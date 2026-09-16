# 待修 · `Accursed Helbrute` 与 `Litany of Despair`（2026-09-16）

> **只读调查，未改任何代码。** `Core/EffectText.cs` 正被主对话编辑 ⇒ **行号会漂，判据一律以函数名给**。
> 证据四层：① 卡池 `RuleEngine/Resources/cards_engine.json`（1126 张）＋原始转录 `数据/游戏数据/card_stats_raw.json`
> ② 引擎源码 `Core/{EffectText,CardDef,EffectResolver,GivePayload,RuleCore}.cs` ③ `d:/warpforge/scripts/rule_core.gd`
> ＋签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/` ④ 成品卡图 `d:/2/Warpforge部队卡片/`（铁律 7）＋逐张看图抄录
> `资料/卡表核对_卡图提取/{_还原效果文字,Chaos__1,Orks__2}.md` ＋会话留下的探针快照 `_tmp_view/probe_after.txt`（1097 张）。
> ⚠️ **本轮没跑 Unity**（同工程第二个实例起不来）⇒ 凡「op 长什么样」的结论，要么引用任务里已实测的 dump，要么标**源码推导**。

---

## 一 · `Accursed Helbrute`（`BL74` · unit · 8 费 9/7/9 · BlackLegion）

**卡面**（`Chaos/Dark Zealots/Warpforge_10_Accursed-Helbrute.png` → 抄录 `_还原效果文字.md:181`）：
`When a friendly troop receives a ✦Dark Pact, this troop gains it as well` —— 与 `desc` 逐字一致（只少一个图标词）。**数据这层没问题，错全在引擎。**
**根因（两处；① 是看得见的症状，② 是修完 ① 才会暴露的静默错打）**
① **载荷 `it as well` 认不出** —— `GivePayload.ParseInto` 五条判据（能量 / 嵌入 / `,`+`and` 多属性拆分 / `GiveKw` 前缀 / `ReAttr`＋裸 `±N`）**一条都不命中** ⇒ 返回 `false`；`EffectResolver.DoGive` 开头 `payload == null` ⇒ 日志「载荷…本版不认识 —— 这条没生效」＋`unresolved.Add`，**整条 op 空过**。
② **目标 `this troop` 指到别人身上** —— `EffectText.ParseTarget` 把 `this troop` 交给 `IsPronoun` ⇒ `Side/Kind = prev`；事件路 `EffectResolver.ResolveOps` 的 `prime = seed`，seed 来自 `BroadcastWhen(..., subject)` ＝**拿到契约的那个单位** ⇒ 修好 ① 之后「同样获得」会加在**事件主语**身上，不是印刷这张卡的那个单位。
⚠️ **更正任务书一句**：这不是「不报错」—— 日志每次两行都在。真正丢的是**汇总口径**：`EffectResolver.ResolveOps` 里那个 `unresolved` **建了、传了、从来没读**（循环之后直接丢弃）⇒ 只进 `ctx.Log`、不进任何报表。
**引擎现有机制能不能表达 —— 能表达一大半，只缺一个 op 形状**
黑暗契约＝**单位身上的关键词**（`KeywordTable.DarkPact`），**种类**在旁表 `EffectResolver._keywordsPact`，已有公开取值口 `EffectResolver.PactOf(UnitState)`。**事件把契约带过来了**：`EffectResolver.GrantDarkPact` 里**先**写好种类、**再** `BroadcastWhen(ctx, WhenEventKind.GetsDarkPact, pactOwner, u.Card, u)` ⇒ 监听器结算时 `PactOf(事件主语)` 一定拿得到那份契约。缺的只是「取事件里那份契约」的载荷形状。**`rule_core.gd` 全文 `grep "as well"` = 0 命中** ⇒ 这张卡参考实现也没做，**没有权威形态可抄**（如实记：这是自定，不是照抄）。
🔴 顺手查出一处**死代码**（别再对着它改）：`GrantDarkPact` ③ 那个「扫己方棋盘找第一个监听者」的循环取的是 `hv.Card.Effect("when a friendly troop receives a dark pact")`，而 `CardDef.Effect` 查的是按**关键词名**建的 `_effects` 字典 ⇒ **恒为 null，这一支从来没跑过**。真正在响的是 ① 那句 `BroadcastWhen`。
**最小修法（两处，都是三行级）**
**A 载荷**：`GivePayload.ParseInto` 加**最窄**一条 —— 剥完噪声后整段**恰好等于 `it as well`** 时产出 `PayloadOp { CopyEventPact = true }`（照 `PayloadOp.Embedded` 的先例加标志位）；`EffectResolver.DoGive` 在 `p.Keyword == KeywordTable.DarkPact` 那一支**旁边**加 `if (p.CopyEventPact) { GrantDarkPact(ctx, owner, t, PactOf(ctx.LastTarget), by); continue; }`。`GivePayload.Mechanized` **不用改**（新标志位既非 keyword 也无 `Attr`，天然走 `if (!op.IsKeyword) continue;`）。判据必须收到「整段恰好相等」：全池 11 处 `as well` 里只有这一处是代词，其余都是 `give it <具体载荷> as well`。
**B 目标**：`EffectText.ParseTarget` 把 `this troop`/`this unit` 从 `prev/prev` 改判成 `Subjectless`（`Side="own", Auto=true, Count=1, Subjectless=true`）—— `EffectResolver.ResolveTargets` 的 `Subjectless` 支**已经是**「有施放者就是施放者自己」，事件路的 `source` 就是监听者；同一文件 `EffectText.TryDouble`（`this troop's …` 那一支）**早就这么写了** ⇒ 这是把同一句话的两半对齐。
**落点**　载荷认词 → `GivePayload.cs` : `GivePayload.ParseInto`（＋`PayloadOp` 加 `CopyEventPact`）· 结算 → `EffectResolver.cs` : `DoGive`（DarkPact 那一支旁，复用现成的 `PactOf`/`GrantDarkPact`）· 目标 → `EffectText.cs` : `ParseTarget`（`IsPronoun(t)` 那一支）。
**影响面**　**同形卡：全池 1 张**（就是它）—— `desc` 含 `as well` 的共 11 张，只有它的载荷是代词。**B 会波及**：`desc` 含 `this troop/this unit/that troop/that unit` 的共 **17 张**；探针里真正落成 `prev/prev 「this troop」` 的只有 **2 处**（`Warp Spider` 的 `Strike: Return this troop…`、`Mucolid Spore` 的 `Strike: Destroy this troop`）—— 它们走 `FireTriggerAt`（`seed = null` ⇒ `prime = source` ＝单位自己），**行为不变**；`Hunta Rig` 的 `This troop attacks it` 落成 `forceattack` 且 `Target` 为空，**不经 `ParseTarget`**；唯一在**事件路**被 seed 盖掉的是 `Immortals Phalanx`，而它的事件主语恰好就是它自己。⇒ **真实行为变化只有 Helbrute 一张**（探针覆盖 1097/1126，剩 29 张未逐张核）。
**风险与推荐度**　🔴 **递归（改 A 必须一并处理）**：Helbrute **自己**收到契约时，`GrantDarkPact` **先广播（①）再给自己加（②）** ⇒ 监听器会「再给自己一份」→ `GrantDarkPact` 替换旧契约并**再广播一次** ⟳，直到 `BattleContext.MaxEffectChain = 8` 截断。最小办法：A 那一支里 `if (ReferenceEquals(t, ctx.LastTarget)) continue;`（它已经拿到了）。
**A 推荐**（小、语义确凿、全池唯一）；**B 推荐，但必须先跑 `RuleEngineTest.Run`** —— 它动的是代词口径，靠自检确认那 17 张没被改坏；若自检出现别的差异，就把 B 收窄成「只在事件路生效」。❌ **不推荐**在 `GrantDarkPact` 里按**卡名**特判 —— 按卡名写死是本工程一直避免的形状。

---

## 二 · `Litany of Despair`（`BL_Litany_of_Despair` · tactic · 2 费 · legendary）

**卡面**（`Chaos/Dark Zealots/Warpforge_2_Litany-of-Despair.png`，本轮开图复核；抄录 `_还原效果文字.md:201`）：
`Ephemeral. Give Vulnerable 2 to an enemy troop and "💀 Backlash: Give a ✦Dark Pact to a random enemy troop"` —— **卡面上那半句是带引号的**，我们 `desc` / `card_stats_raw.json` 里**引号丢了**。
**根因（三层叠在一起，缺一层都解释不了现状）**
① **数据**：卡面有 `"…"`、两份数据都没有（同批里 `Hunters of Heretics`/`Graceful Avoidance`/`Duty's End` 的引号**都留着** ⇒ 不是全局约定，是这几张转录时丢的）。
② **解析**：就算补回引号，印刷语序是 `Give X to Y and "<Kw>: 正文"` —— 引号段在**目标那侧**，而 `EffectText.TryGiveInner`（`ReGive` 那条路）只把 ` to ` **之前**那段当载荷 ⇒ 引号段整个被收进 `targetText`，嵌入效果**没人接**。
③ **卡片层（症状）**：`keywords` 里有 `Backlash`（那是**引号内那个图标**被当成本卡关键词收下了）⇒ `CardDef.CollectBareKeywordBody` 判据 ①（「已经有 `X:` 前缀」）**认的是段首**、看不见句中 ⇒ 判据 ②（带正文关键词唯一）成立 ⇒ **整条 desc 被登记成 Backlash 的正文**（`_triggerOps` / `_triggerText`）。
🔴 **还有一条语义必须写清楚：「这张卡自己有 Backlash」是误读。** Backlash 是**单位**关键词 —— 触发点只有一处（`RuleCore.KillUnit` 的 `FireTriggerAt(ctx, u, Backlash, …)`）；`rule_core.gd` 同样只在 `kw_has(u,"backlash")` 的**单位**死亡分支触发；`AbilityTrigger` 枚举里**没有 Backlash**。战术卡进弃牌堆时**没有任何东西会去放它的 Backlash** ⇒ 即使把 ③ 修好（切出正文、登记成 `TriggerOps("backlash")`），**这张卡也什么都不会发生**。卡面那句话的真意是「**把那项反噬能力授予那个敌方部队**」。
**引擎现有机制能不能表达 —— 能，而且有同型先例（已经在跑）**
走的是 `GivePayload.ParseInto` 的 **①b 支**（载荷含 `:` 且 `:` 前那截是关键词）⇒ `PayloadOp.Embedded` ⇒ `EffectResolver.DoGive` 见 `p.IsEmbedded` 调 `GrantEmbeddedCore`：拆 `关键词: 正文` → 正文过 `EffectText.Parse` → 挂到目标身上（`UnitState.GrantOps`＋`AddKeyword`）⇒ 那个单位死时 `FireTriggerAt` 自己会找上它（`u.FxOps(keyword)`）。同型先例：`Graceful Avoidance` · `Duty's End` · `Hunters of Heretics`。
**最小修法**
**首选（忠实印次语序，两处小改）**　1）**数据**：在 `数据/游戏数据/cardface_fixes.json` 的 **`desc` 列**补回引号（`"Litany of Despair": "Ephemeral. Give Vulnerable 2 to an enemy troop and \"Backlash: Give a Dark Pact to a random enemy troop\""`）⚠️ **必须走这一列** —— `gen_cards_engine.py` 的 `load_cardface_fixes` 只认它，直接改 `cards_engine.json` 会被重跑静默抹掉。2）**解析**：`EffectText.TryGiveInner` 里，`ReGive` 切完 `payload`/`targetText` **之后**加一条**窄**规则 —— 目标那侧若出现 `and "<关键词>: 正文"`（引号包着、头是 `KeywordTable` 认得出的词），把引号段**折回载荷**（`payload + " and " + 引号段`），目标短语只留前半截。折回后 `GivePayload` 的 ①b 会把它收成第二项 `Embedded`，`DoGive` 把 `vulnerable 2` 与它**一起**给目标 ⇒ 与 `Hunters of Heretics` 完全同型。⚠️ **引号是承重的，别省**：写成 `… and backlash: …` 会被 ①b 的「从后往前找头」（`hitHead` 循环）先吃掉 `backlash` 并把 `vulnerable 2` 一起吞掉（命中即 `return true`）。
**备选（零代码，但改语序）**　只把 `desc` 写成 `Ephemeral. Give Vulnerable 2 and "Backlash: Give a Dark Pact to a random enemy troop" to an enemy troop`（＝`Hunters of Heretics` 语序）。**代价**：数据里得注明「照语义重排，不是照抄卡面语序」—— 本工程以卡面为尺子，这么做要留痕，别让它变成「我们的 `desc` 就是卡面原文」的第二份说法。
**落点**　补引号 → `数据/游戏数据/cardface_fixes.json` 的 `desc` 列（应用点 `工具/gen_cards_engine.py` : `load_cardface_fixes`）· 折回载荷 → `EffectText.cs` : `TryGiveInner`。**不必动** `CardDef.CollectBareKeywordBody`：折回载荷不改变任何 `X:` 前缀的**位置**，③ 的整条吞并**仍会发生但惰性**。
**影响面（可复算：`desc` 按 `.`/换行切段后找非段首的 `<关键词>:`）**
· **引号内（引擎已能收）：全池 13 张** —— `Graceful Avoidance` · `Hunters of Heretics` · `Duty's End` · `Helspear Assault` · `Pledge to the Dark Prince` · `Enhanced Aggression` · `Living Icon` · `Master Outrider` · `Ferocious Rage (Beastboss' Talent)` · `Power of the Waaagh!` · `Uge Choppa` · `Hallowed Martyrs` · `Oath of Moment`（按 19 个带正文关键词扫；`待修_结算层三件.md` 另有一份 13 张名单、成员差 2 张 —— **以那份为准**，本条只要「这个形状全池已能收」这个结论）。
· **裸写（引号丢了）：2 张** —— `Litany of Despair`（Backlash）· **`Beastboss on Squigosaur`（Slay）**。
· **假阳性 3 张** —— `Righteous Repugnance` / `Moment of Grace` 的 `4|5 Energy: …` 是**付费激活前缀**，不是触发关键词，**别一起收**；`Hrolf the Ironhowl` 的 `…⚡ Rally: …` 经 `AddTriggerOp` 的 `StripLeadingIcons` **本来就是段首**。
⚠️ **更正任务书的「8 张里 7 张句首」**：含 `Backlash:` 的 8 张 = **4 张段首（全是单位，走 `CardDef.AddTriggerOp`）** ＋ **3 张引号内（战术卡，走 `GivePayload` ①b）** ＋ Litany（本该是引号内、引号丢了）。段首那 4 张：`Lokhust Lord` · `Grot on Bombsquig` · `Bomb Squig` · `Makari the Grot`。
🔴 **第二张裸写的是 `Beastboss on Squigosaur`（`GOF90` · Ork · unit · legendary），同一根因、修法相反。** 卡面（抄录 `资料/卡表核对_卡图提取/Orks__2.md:16`）：`Stomp. Friendly Beasts cost 1 less and have "💀 Slay: Gain Blood Thirst this turn"` —— **引号也在卡面上**，真意是「**给全体友方 Beast 挂 Slay 能力**」。它现在实测出来的 op（`_tmp_view/probe_after.txt:3178-3181`）是 `gain 载荷「blood thirst」 时长=turn 目标[own/beast ×1 兵种筛=Beast 「friendly beasts cost 1 less and have slay:」]` —— 条件从句＋触发前缀一起被吞进目标引文、`×1` 只给一个 Beast、而且是**立刻加血欲**而不是**授予 Slay**；同时 `CollectBareKeywordBody` 把这条错 op 登记成**它自己的 Slay 正文** ⇒ 它一杀人就会「立刻给一个友方 Beast 加血欲」（**静默错打**）。⇒ **建议单开一条待办**，且**修法与 Litany 相反**（不要切分句中 `Slay:`，要按引号走嵌入/光环那条路）。
**风险与推荐度**　✅ **附带会修好的一个错**：现在 `vulnerable 2` 的目标被标了 **`随机`** —— `EffectText.ParseTarget` 里有 `if (t.Contains("random")) spec.Random = true;`，而 `t` 是**含被吞从句的整段目标短语**（里面有 `a random enemy troop`）。后果双重：`EffectText.PickTarget` 会跳过 `Random` 的目标 ⇒ 这张卡**不让玩家选目标**（`CanPlayTactic` 也不要求格位）；结算时 `EffectResolver.ResolveTargets` 走 `spec.Random` 支**从池子里随机抽一个**。⇒ 卡面写 `to an enemy troop`（**玩家选**）、实况是**随机**，而且卡面不打 `*`。折回载荷后这一条自然消失。
⚠️ **残留（惰性）**：修完之后 `CollectBareKeywordBody` **仍会**把整条 desc 登记成 `_triggerOps["backlash"]` —— 它**不会被执行**（战术卡没有 `UnitState`，`FireTriggerAt` 那条路进不去），只会在覆盖率报表/探针里多一行。想清干净得数据侧把 `Backlash` 从这张卡的 `keywords` 里去掉 —— ⚠️ **动之前要先确认卡面渲染那条路读不读 `keywords`**；**本轮没查这一条，如实标「没查」**（查过的三层：引擎源码 / 卡池数据 / 卡图抄录，都没记这件事）。
**推荐度：首选方案推荐**（数据一处＋解析一处，判据窄、有 3 张同型先例可回归对拍）；备选方案能零代码跑通，但**改语序要留痕**，只在前者来不及做时用。
