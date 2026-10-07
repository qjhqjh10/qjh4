# W_B3 事件层与常驻（战斗/引擎侧）—— 交接报告

> 批：**B3** · 2026-10-17 · 写手代理 · 唯一判据文件 = `资料/事件层_数据与设计.md` · `资料/常驻效果_数据与设计.md`
> 白名单四个文件：`Core/WhenEvent.cs` · `Core/Aura.cs` · `Core/RuleCore.cs`（**只为 oath 那一小段**）· `Editor/RuleEngineTest.cs`
> ⚠️ **本轮没跑 Unity**（一支自检都没跑）—— 断言写好待主对话在同步点统一跑。
> ⛔ 没动过的：`EffectResolver.cs` · `EffectText.cs` · `CardDef.cs` · `CreatePool.cs`（都不在白名单）—— 涉及它们的活见 ③。

## ① 结论（逐条：原版 / 我们 / 结果 / 判据出处）

| # | 主题 | 原版长什么样 | 我们原来 | 结果 |
|---|---|---|---|---|
| 1 | **`When played` 收不到** | 「打出一张牌」有两条广播：`BattleManagerSupport__BroadcastCardPlayed.c`（由 `BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:1426` 调；听众 = **场上每张牌** `+0x470` + 当前回合方手牌 + 另一方手牌）· `BattleManager__BroadcastTacticPlayed.c`（由 `CardScript__NonTargetSpellPlayed.c` / `CardScript__TargetedSpellPlayed.c` 调；听众 = 场上 `IsInPlay` 的牌）。🔴 **但它们对「打出那张牌自己」的意义 = 结算我自己的正文**：`CardScript__ResolveCardPlayed.c` 里对自身那一次调用传的 `cardPlayed` **就是它自己**（虚表槽 `0x308` = `OnCardPlayedWithTarget(manager, thisCard, cardPlayed, targetCard)`）⇒ **没有「另一个监听者被叫醒」这回事** | 战术卡不注册监听器（`CardDef.cs:1689-1713` 的 `CanListenForEvents => IsUnit`）；`When played, gain 3 Quest Points` 那半句**早就**由 `EffectText` 当普通句子结算（`CardDef.cs:1700-1704`） | ➖ **不改行为**（我们的做法与原版**等价**）。只把上述查证写进 `WhenEvent.SelfOnly` 的注释（原文只说「不是事件层的事」，没写原版长什么样） |
| 2 | **单案四条** | —— | 四条**全都已经接完并有断言**：`this unit attacks an enemy with Hunt Mark`（解析 `WhenEvent.cs` 的 `attacks <宾语>` 支 → 广播 `RuleCore.cs:1710` 带 `target:`）· `this unit kills an enemy`（`RuleCore.cs:1908` 带 `actor:`）· `you create or play a secret`（`WhenEvents.TrySplitAlternative` + `EffectResolver.cs:1874` 广播）· `played`（= 第 1 条） | ✅ **本轮只核，未改**。端到端断言已在 `RuleEngineTest.TestWhenEvents` ⑭-a/b/c（长牙）、⑯-a/b/c（击杀）、⑩（拆两条）、⑫（造破坏）、⑬（解析层） |
| 3 | **事件名铺宽（④）** | —— | 最近一次**全套实跑**（`_tmp_view/ruleengine.log`，3198/3198 全过）里那两行原话：`` `When <事件>`：带它的卡 **78** 张 · 真的点亮 **82** 张（共 83 条监听器）· 事件触发式降费 **4** 张 `` / `` 认不出的**事件短语** 0 种 ``；清单 `_tmp_view/when_unparsed.md` **只剩表头、零行** | ✅ **无可铺宽项（0 种）**。⚠️ 口径：这个桶只装**单位卡**的 `When` 句（非单位卡在 `CardDef.AddWhenTrigger` 就被 `CanListenForEvents` 挡住、根本不进桶） |
| 4 | **条件层缺 `they are battlesuits`** | —— | 🔴 **条件层 2026-09-14 就做完了**（commit `9e88674`「A6 族 B」）：`EffectText.cs:7268` 的 `ClauseKeywords → targethaskw` + `EffectResolver.cs:5120-5191` 真去判（`CreatePool.IsKnownKind("battlesuits")` 走 `Singular` 命中 `battlesuits`）。`资料/回手与指代_数据与设计.md:118` 那一行**是过期记录**。**仍然打不出来的是后半句**，见 ③-1 | ➖ **不是条件层缺** —— 换口径，见 ③-1 |
| 5 | **`TurnBeforeEnd = 41`** | `AbilityTrigger.TurnBeforeEnd = 41`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs:15`）；时机 = `BattleManager__ResolveEndTurn.c:385` **先** `BroadcastTurnBeforeEnd`、`:433` **后** `BroadcastTurnEnd`（两者背靠背，中间无别的步骤）；`CardScript__OnTurnBeforeEnd.c` 对每张牌发 `OnTrigger(0x29 = 41, …)`；全库**只有** `ResolveEndTurn` 这一个调用点 | 我们只有 `turn_start` / `turn_end` 两档（`EffectText.AtTurnClauses` 出来的 `Phase` 只有这两个值；`RuleCore.cs:827` 调 `ResolveAtTurn(ctx, "turn_end")`） | ⛔ **停手 —— 要改的文件不在白名单**，见 ③-2 |
| 6 | **8.9「同数换位」** | ✅ **逐行核过**（`d:/2/tools/decomp_full/CardScript__UpdateWhileInPlay.c`）：取 `MinionManager.GetUnitList()` 的条数（`get_cardType()==10` 取 `Count`、否则取 `Count-1`）存 `+0x358`，然后 `if (*(int *)(param_1 + 0x358) == iVar2) return;` —— **只比数量**；真变了才 `RemovePackBuffs` + `AddEffect`（带 `0x262 = DefinedTrait.pack`）。⇒ 「把 A 挪到 B、张数不变」这条路上原版**一条都不重算** | 我们每次棋盘变动整份重算 ⇒ 同数换位**会**刷新 | ➖ **行为一字未动**（派活点名）—— 把上面那几行判据补进 `Aura.Recompose` 的注释，并写明「要跟原版得先跑实况」 |
| 7 | **`oath` 的 `Emit` 排在付费之前** | 原版三段**闸 → 付费 → 结算**：`CardScript__CanUseOathAbility.c` → **`BattleManager__PayActiveAbilityCostOath.c`**（代价读该卡 trait `0x4fb`，调 `PlayerManager.UseMana(cost)`）→ `CardScript__ResolveActiveAbilityPlayed.c`（到这一步才 `+0x50 += 1`、`+0x4C = 1`）。**没有任何一步在付费之前宣布「能力发动了」** | 🔴 **真缺陷**：`ctx.Emit(EvtKind.Ability, …)` 排在 `ResolveOathAbility` **之前** ⇒ 付不起那一次也发过一条「能力发动了」（监听方都在表现层：战斗日志按它写一行「发动技能」`BattleDriver.cs:4510` · VFX 映射 `BattleDriver.cs:6661` · trait 粒子 `BattleDriver.cs:6825` —— `EvtKind.Ability` 那一档对应的正是原版 `UsedActiveAbility`(0x4f1 = ferocity)） | ✅ **落地**：整段（`Emit` + 日志）挪到**付费成功之后**；另删掉一个死掉的局部 `int before` |

## ② 改动清单（含断言）

**`Core/RuleCore.cs`** · `UseOathAbility`（唯一改动点）：`Emit` + 日志移到 `ResolveOathAbility` 返回 true **之后**；`if (!ok)` 分支补一句「一条 `EvtKind.Ability` 都不发」。判据（三段原版顺序 + 行号）写在方法头注释里。

**`Core/WhenEvent.cs`** · 三处**过期注释就地更正**（铁律 5，不动一行代码）：
- `SelfOnly`：补第 1 条那份原版查证（两条广播 + 自身那一次调用的语义），并写明 ⛔ 别给 `played` 单开解析分支。
- `SetWho`：原文写「省主语的这几条**正确语义是「就是它自己」**，我们现在按「任何单位」处理 —— **这是我们挑的近似**……**留到下一轮**」—— **整段过期**：`SelfOnly` 2026-09-13 第三十四轮就做完了。改成「自指只能由各分支**显式**设，不是从『省主语』推出来的」。
- `StripFirst`：原文写「`When deployed` / `When played` 这几条**暂时收不到**」—— 前半句过期（`deployed` / `reanimated` 早收了，走各自的直接分支、**不走** `StripFirst`）。保留 `When played` 那半。

**`Core/Aura.cs`** · `Recompose` 的注释：把第 6 条的逐行判据补上（含 `Count-1` 这个细节），并显式写「本文件行为不许改」。

**`Editor/RuleEngineTest.cs`** · `TestOathAbility` 两条新断言 + 一个新助手 `OathAbilitySignals(ctx)`（数 `Signals` 里 `Kind==Ability && Keyword=="oath"` 的条数）：
- ③（成功）：`Check(OathAbilitySignals(ctx), 1, …)` —— 0 条说明事件没接、2 条说明结算跑了两遍。
- ⑤（付不起）：**先 `ctx.ClearSignals()`**，再 `Check(OathAbilitySignals(ctx), 0, "★ 付不起那一次一条 Ability 事件都没发 —— 判别式：把 Emit 挪回 ResolveOathAbility 之前，这里会实得 1")`。
  🔴 **这就是判别式**：把 `Emit` 挪回付费之前 ⇒ 这条**必变红**。（③ 那条只挡「事件整个没接」，挡不住「挪回去」—— 因为成功那次两边都发 1 条。）
  ⚠️ `ClearSignals` 不能省：③ 成功那次留下的一条不清掉就永远数不出 0（反例会假通过）。

## ③ 没查清 / 停手的部分

1. **`Dynamic Offensive`（TAU54）后半句仍然打不出来 —— 卡在「卡实例身份」，不是条件层**。
   `Draw 2 troops from your deck. If they are Battlesuits, give them Flank.` —— 条件认得出、也真会判，但：
   · `DoDrawType` 把抽到的牌记进 `ctx.DrawnThisResolve`、**没有**记 `ctx.LastTargets`（`EffectResolver.cs:432-437`）；
   · 而 `targethaskw` 的目标只从 `chosen` / `ctx.LastTargets` / `ctx.LastTarget` 取（`EffectResolver.cs:5135-5139`）⇒ `ts.Count == 0 ⇒ return false`（**判不了**，调用点会出声记进 `unresolved`，**不静默**）。
   · 而且**即便记了也没用**：`LastTargets` 是 `List<UnitState>`，抽到的是**手牌里的 `CardInstance`** —— 给「手里的卡」挂关键词要的是 `SetupCardInHand` 那一套，那是 `资料/卡实例身份_爆炸半径.md` §六 第 6 条那一笔（**开着**：那一节原引 `EffectResolver.cs:3464`「手牌里那些等着被 `SetupCardInHand` 挂效果的还没做」—— ⚠️ **该行号已被后来的改动推移**，引用时按那句注释文本找，别按行号）。
   ⇒ **归到「卡实例身份」**。要动 `EffectResolver.cs`（**不在白名单**），请主对话并账或另派。
2. **`TurnBeforeEnd = 41`（第 5 条）落地** —— 判据已齐（见①），但**两个落点都在白名单外**：
   · `RuleCore.cs` 的 `EndTurn`（我的白名单只到「oath 那一小段」）—— 要在 `ResolveAtTurn(ctx, "turn_end")`（`RuleCore.cs:827`）**之前**加一句 `ResolveAtTurn(ctx, "turn_before_end")`；
   · `EffectText.cs` 的 `AtTurnClauses` / `TryAtTurn` —— 要有**生产者**才会有人产出 `turn_before_end`。
   ⚠️ **本地没有卡面短语能映射到这一档**：全池 grep `before the end` / `before end` / `end of the turn` **0 命中**；而**哪张卡配哪个 `AbilityTrigger` 在远端 CCD 的卡预制体上**（`资料/常驻效果_数据与设计.md` §二·⑤ 已记「本地没有原版卡技能的字段级数据」）⇒ **「`At the end of each turn` 那几张里有没有该走 41 的、`Poisoned Supplies` 那类手牌陷阱算 40 还是 41」都证不了**。
   ⇒ 建议：**先只加相位骨架**（`ResolveAtTurn` 对未知相位本来就是安全的，`RuleCore.cs:663-666`），等有判据再挂生产者；⛔ **别凭猜测把哪句卡面挪到 41**（那是拿猜测填空）。
3. **`_tmp_view/when_unparsed.md` 的「0 种」口径**：它只覆盖**走到 `WhenEvents.ParseAll`** 的短语（= 单位卡）。**非单位卡的 `When <事件>` 句**（如 `Reconnaissance Mission` 的 `When played`）从不进桶 —— 这不是缺陷（那一族要么已被 `EffectText` 接手、要么由 `SplitHandTrapWhen` 单管），但**「0 种」不能读成「所有卡面的 `When` 都认得出」**。
4. Aura 那 33 张的机制账（`_tmp_view/aura_mechanism.md`）本轮**没复核**：只看过它一次，没跑 Unity 重生成。

## ④ 类型检查（原样贴）

最后一次（收工态，原样贴）：
```
$ TMPDIR=/tmp/wf_b3e bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
⚠️ 中途有**两次不干净**，两次的报错**全部集中在别人的文件**上（并发写手正在改 `CardDisplayWindow.cs` / `DeckScene.cs`，符号还在变：`_eyeBtn` · `DeckRuntime.UiRowRarityTint/UiQuadTex/QLowest/QSep`）—— 按简报名点名的判据「错误全集中在不是你负责的文件上 ⇒ 那不是你的问题」，**没有去碰**；等了一会儿重跑即 0/0。

（加跑了一次 `WF_DOC=1`：我改的四个文件 **XML doc 格式警告 0**；仓里其它文件的 CS1570 是别人的活，未碰。）
