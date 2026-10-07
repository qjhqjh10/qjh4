# REV_W4 · 引擎手牌族 独立审查（只读）

> 审查代理 `REV_W4` · 2026-10-18 · **只找错、只交报告，一个字都没改**
> 只读手段：`git diff`（6 个文件）· 现读源码 · `d:/2/tools/decomp_full/` · `d:/2/Warpforge_code/Scripts/Assembly-CSharp/` · `cards_engine.json`（现扫）· `PlayerHand__*` / `CardScript__*` / `AbilityLogic__PlayAbility.c` 方法体
> ⛔ 没跑 Unity / `typecheck.sh` · ⛔ 没动 git · ⛔ 没改任何工程文件与正本

---

## 1. 结论一句话

**真问题 7 条 · 存疑 5 条 · 作者自陈不成立 3 条。**

五笔账的**方向全部正确、判据引用基本准确**（`ProcessCompanion` 三点全反 · 「`N` = 链长不是张数」这个读法**核过、成立** · 到期位逐条 · `0x82` 那条判据我只补名不推翻）。
但有三处**硬伤**：① `A885②` 的消费者**漏掉了最热的那一个进手牌入口**（`EffectResolver.cs:1917` 的 `create … in hand`）；② **三条带 `Companion` 的卡在输入侧就是死的**（两条关键字表里没有 `Companion` 条目 ⇒ 静默不触发；一条 `N` 被读成 1 而卡面是 2）；③ `extrinsic` 的生产者近似**方向性地偏向误摘**。

---

## 2. 逐条问题表

| # | 结论 | 证据（文件:行 + 片段） | 该照哪个判据 |
|---|---|---|---|
| 1 | **真问题** | `RuleEngine/Core/EffectResolver.cs:1917` `ctx.Players[who].Hand.AddRange(made);` `:1919 EnforceHandLimit(ctx, who);` —— **没有** `SetupCardInHand`。这是 `create N … 进手牌（/对手手牌）` 那条路（`op.Dest == "hand"/"enemyhand"`） | 原版：`_ResolveCreateHandCard_d__512:135` 造出来的卡走 `PlayerHand.AddCardNotDrawnToHand`，而它内部 `:34` 就调 `SetupCardInHand`。作者自己在 `RuleCore.cs:1419`（潮涌）与 `:1538`（伴生）都插了 —— 这一处是**漏插**，不是决定 |
| 2 | **真问题** | `cards_engine.json`：`TAU38 Coldstar Battlesuit` `keywords=['Flying','Armour 1']`、`desc='…Companion 2: Marker Drone'`；`TAU28 Strike Team` `keywords=[]`、`desc='Companion: DS8 Support Turret'` ⇒ `CardDef.cs:98`（`_keywords` 只由 `keywords` 数组建）⇒ `Has(Companion)` **恒假** ⇒ `RuleCore.cs:1525 else return;` **一句日志都没有** | 卡面 `desc` 印着 `Companion`；原版闸门是 `ShouldTriggerCompanion.c:12-29`（`HasCurrentTrait(0x49c)` **或** counter>0）。同族先例已经补过：`CardDef.cs:1175-1180`（`Ecstasy` 的 `keywords` 数组里没有它 ⇒ 从 `desc` 收正文时顺手登记关键词）—— `Companion` 没照做 |
| 3 | **真问题** | `TAU13 Pathfinder`：`keywords=['Rally','Companion']`（**裸 `Companion`，无数字无名字**）、`desc='Rally: …\nCompanion 2: Marker Drone'` ⇒ `KwValue(Companion)` 走 `CardDef.cs:2524 return 1;` 兜底 ⇒ `RuleCore.cs:1514 n = x - 1 = 0` ⇒ **链长 1，卡面是 2** | 卡面 `Companion 2`（原版 `companionCounter` = `CardSetup.c:256-259` ← `get_CurrentCompanion` ← 词条值 = **2**）。名字侧 `ExtractCompanionName(Desc)` 是对的，**只有 N 读错** |
| 4 | **真问题** | `RuleEngine/Core/RuleCore.cs:1525` `else return;` —— 无词条、无计数时**静默返回**（红线）。上面两条（名字读不出 / 池里查不到 / `CardPool == null`）都出声了，**唯独这一支不出声**；而第 2、3 条那种数据缺陷正好都落进来 | `d:/4/CLAUDE.md` 红线「不许静默失败」。原版这一支是**真的没有事可做**（`ProcessCompanion.c:29` 返回），但**我们的**「没有词条」可能是解析层漏了 ⇒ 必须出声 |
| 5 | **真问题** | `RuleEngine/Core/EffectResolver.cs:2826` 那句 `Extrinsic` 赋值是「形参 **或** `ctx.ActingUnit != null`」（即「**结算这一刻有个在场单位**」）—— 只要那一刻有单位就把这条手牌效果标成 extrinsic；`RuleCore.cs:3152` 又按「extrinsic 且施放者不在场」摘 ⇒ 那个单位一离场，**这条效果被摘**；原版旗标来自 `AbilityData` 的 `whileInPlay`（`+0x40`）/ `whileInPlayVariable`（`+0x41`）—— 判据 `AbilityLogic__PlayAbility.c:1195-1198` 与 `:1352-1355` 里的 `*(cardEffect+0x30) = (abilityData+0x40) ‖ (+0x41)`，**是「这条能力是不是持续型」**，不是「此刻有没有单位」。⇒ 一次性能力（`Deploy:` / `Battlecry:` 那一族）给的手牌加成会被我们**误摘**。具体嫌疑卡：`GOF84 Snakebite Nob`（unit，正文无触发前缀 = 一次性投递）、`GOF81 Beast Snagga Nob`（unit 的回合末触发） | 待实现物 = `AbilityData.whileInPlay*`。⚠️ 拿不到时**不能**用「此刻有没有单位」顶替 —— 该机制**要做**（铁律 11），只是判据未齐，见 §5 |
| 6 | **真问题** | `RuleEngine/Core/RuleCore.cs:3238-3241` 去重键 = `r[k].Source == e.Source && r[k].Op.Payload == e.Op.Payload`（**字符串**键）；原版 `CardEffect__IsSameEffect` 比的是**整条记录**（`+0x30/+0x31/+0x32..0x35/+0x48/+0x4c/+0x50/+0x54/+0x60/+0x64/+0x68/+0x6c/+0x70/+0xa4` + `enchantingCard` 引用） | `CardScript__AlreadyContainsEffect.c` → `CardEffect__IsSameEffect.c`。**后果**：同一来源连挂 N 次（`Beast Snagga Nob` 撑三个回合 = 3 条，全池唯一已知的叠加族）被压成 **1 条** ⇒ 后进手牌的牌只吃 +1，而一直在手里的牌打出时吃 +3 |
| 7 | **真问题**（文档） | `RuleEngine/Core/CardDef.cs:2241-2242` 仍写「**从手牌打出时带出至多 X 张同名伴生部队**」+「原版是「**可**打出」（玩家选），我们**自动带满** —— 近似」 | 现判据 `CardScript__ProcessCompanion.c:26-29/70/85-89` + `BattleManager__AddNewCardToHand.c:65/:97` + `_ResolveCreateHandCard_d__512:135`：**按卡定义造一张新卡 · 进手牌 · 零移除 · 不碰手牌**。作者自陈未改（不在白名单）⇒ 主对话落盘 |
| 8 | 存疑 | `RuleEngine/Core/RuleCore.cs:3227-3246` 登记表 = **现场扫手牌实例**（`HandEffectRegistry`） | 原版是 `PlayerHand.activeEffects // +0x48` 一张**登记表**。⇒ **某条效果的载体全部离开手牌之后，我们在登记表上就看不见它了** ⇒ 它本该补给后来的牌（原版会），我们不会。判据 = `PlayerHand__SetupCardInHand.c:38`（遍历的是 `activeEffects`，不是手牌） |
| 9 | 存疑 | `RuleEngine/Core/RuleCore.cs:3251-3253` `if (card.Type != "unit") return false; … return crit == null \|\| crit.Matches(card);` | 判不出来时**放行任何单位卡**（不是「不放行」）⇒ 会把 `GOF81`「给**手牌里的野兽** +1」发给非野兽的部队。运行时**会出声**（`EffectResolver.cs:3278` 那句「进手牌：…吃上了…」），但不说自己走的是退路档。判据 = `PlayerHand__SetupCardInHand.c:58` 的 `FilterMethods.CheckIfMeetsCriteria` |
| 10 | 存疑 | `RuleEngine/Core/RuleCore.cs:3013` `var src = inst.HandEffects[0].Source;`（下 6 行却写着 `if (e == null) continue;`） | 同一次改动里**两个口径**：这里假定 `[0]` 非空、那里防 `null`。今天进不去（两条 Add 路径都塞非空），但这是一处**没护栏**的读 |
| 11 | 存疑 | `RuleEngine/Core/RuleCore.cs:1086-1099` `EnforceHandLimit` 是 `while (Count > limit)` **丢表尾**；`PlayCompanions` 靠「刚 Add 在末尾」对齐原版 | 原版 `_AddCardNotDrawnToHand_d__39:26-30/63-65`：**先判 `Count < MaxCardsInHand` 再 Add**，满了就 `SendToCemetery(刚造那张)`。手牌本来就在上限时**等价**；**手牌已超上限时我们多丢**（`SpawnTideCopies` 不调 `EnforceHandLimit`，`Hand` 可以超）。近似成立，但注释里「语义一致」这句话**过强** |
| 12 | 存疑 | 21 条新断言 + 12 条改口期望值**全部未被执行**（作者 §6 自陈没跑） | 我按静态判据逐条核过（§4），但**静态核不能代替跑**。主对话收口时必须跑 `RuleEngineTest.Run` |

### 作者自陈不成立（3 条）

| # | 它写的 | 实际 | 证据 |
|---|---|---|---|
| a | §8-5「`0x82` 是哪个词条**没查清**」 | **查得到**：`0x82 = 130 = DefinedTrait.jam`。而且**它自己白名单里的 `BattleContext.cs` 早就记着**同一句话 | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs:13` `jam = 130,`；`RuleEngine/Core/BattleContext.cs:374`「`jam` = `DefinedTrait.jam = 130`。**不建模**：全池 0 张卡提到它」 |
| b | §8-4「两个非单位手牌入口（`RuleCore.cs:331/392` 防御卡）没插消费者调用（`HandEffectFits` 对非单位卡恒 false，插了也是空过）」 | **理由不成立**：原版那条链（`_AddCardNotDrawnToHand` / `_AddDrawnCardToHand`）**不看卡类型**，一律调 `SetupCardInHand`。真正的漏点也不在这两处，在 **`EffectResolver.cs:1917`**（见问题 1） | `PlayerHand__SetupCardInHand.c:31/34/35/118` 四个调用点 |
| c | §2 与 §6 的「**11 个进手牌入口**」 | 实测 **12 个 `SetupCardInHand` 调用点**（`EffectResolver` 461/2384/2451/2465/3492/3583/3639 + `RuleCore` 568/1066/1419/1538/4143），**外加 1 个漏点**（`EffectResolver:1917`）与 2 个未插（`RuleCore:331/392`）。口径不实 | 同上；`grep -rn "Hand.Add\|Hand.AddRange" RuleEngine/Core/` 全量比对 |

（另：§3 里 `BattleDriver.cs:2599` 这个指针**指偏 8 行**，实际是 `:2607` `RuleCore.NewBattle(… cardPool: pool, …)`。）

---

## 3. 必查 1/2/3/4 逐条结论

### 必查 1 · `A920 的续`（伴生）

- **与 P1 三条裁定对得上，逐条核过**：源 = **卡定义**（`ctx.NewInstance(def)`，`RuleCore.cs:1532`）· 去向 = **手牌**（`ps.Hand.Add(made)`，`:1535`）· **零移除**（`DeployFree`/`Hand.RemoveAt` 两支在 diff 里已删）。**成立**。
- **`N` = 链长不是张数 —— 这个读法成立**。`ProcessCompanion.c` 方法体亲读：`:26` 带词条 ⇒ `:35,74` 源 = `relatedCard1`；`:29/:70` 不带词条且 `+0x60 ≥ 1` ⇒ 源 = **自己的 `rawCardData`**（造一张自己）；`:89` `new +0x60 = param_1 +0x60 - 1`。⇒ `Companion N` 的链 = 宿主 → 1 张(计数 N−1) → 1 张(N−2) → … 计数 0 停。**总张数 = N，但一次只给一张**。作者 §3 的读法**正确**。
  ⚠️ 但 **一个边界它写错了口径**：`Companion:`（无数字）在原版**照样造一张**（闸门在词条 + `relatedCard1`，不看计数），新卡计数 = −1。我们 `KwValue` 兜底 **1** ⇒ `n = 0` ⇒ 结果**一致**（都只造一张）；但 `P1 §5b` 写的「无数字 = 0」与我们 `CardDef.cs:2524` 的兜底 1 **是两个口径**，别再混。
- **`CreatePool.FindByName` 作 `relatedCard1` 的替身**：全池**确有 3 组同名归一化**（`Terminator Champion`(BL44/EC33) · `Maulerfiend`(BL46/EC39) · `Terminator`(EC22/UM82)）⇒ `FindByName` 返回**池序第一个**、**会张冠李戴**。今天**不咬人**：8 张带 `Companion` 的卡名的伴生名全是 Tau Drone（`Missile/Marker/Guardian/Stealth/Gun Drone`、`DS8 Support Turret`），**无一撞名**。查不到时**真出声**（`RuleCore.cs:1515-1520`，自检 ④ 也钉了那句日志）。⇒ **存疑（潜伏）**，建议替身加一条「同名多份 ⇒ 出声」。
- **`EnforceHandLimit` 是等价近似吗 —— 基本等价，但注释过强**：见问题 11。

### 必查 2 · `A885④` 到期位

- **三段断言「老结构下必红」—— 成立**（逐段按老结构推演）：
  · A（先 `turn` 后无时长）：老结构到期标量被第二条覆盖成 `Never` ⇒ 两条都留 ⇒ 实得 2，断言要 1 ⇒ **红**；
  · B（先无时长后 `turn`）：标量 = `EndOfTurn` ⇒ 两条一起摘 ⇒ 实得 0，断言要 1 ⇒ **红**；
  · C（`nextturn` + `turn`）：标量 = `EndOfTurn` ⇒ 一起摘 ⇒ 实得 0，断言要 1 ⇒ **红**。
  三段**各自独立红**，所以「结构上不可能在老结构下绿」这句**属实**。
- **旧 5 个名字的兼容视图 —— grep 全仓结果**：**生产代码零读取**（`Core/` 下全是注释）；**判据侧 12 处读取**，逐处核过**读取时刻那一份实例身上不超过 1 条** ⇒ **今天不会读错**：`RuleEngineTest.cs:3585`(`HandBuffSource`) · `:3616/:3618`(`HandBuffExpire`，各只挂一条) · `:3718/3719/3723/3735/3737/3755`(`HandBuffUsesRef`) · `:8145` 是 **dump 字符串**不是判据。⇒ 没有「两套并存」的判据。
  ⚠️ 但**没有任何东西挡着以后读错**：新写的 ②′ 就往同一份实例上挂两条；建议在 5 个兼容 getter 的注释里把「读的时候必须保证 ≤1 条」写成一句，或直接给它们加一条「>1 条时打警告」。

### 必查 3 · `A887 的续`（期望值怎么推的）

- **判定：从原版判据推的，不是从实现反推的。** 三条理由：① 规则本身（acting card 是那张手牌牌自己）来自原版，不是我们的形状；② 6 处改动的**增量**恰好等于「手牌那一跳原先洒到被观察单位上的那一档」（`+1/+1/+2/+1/+2/+3`），是**同一条规则的一次次应用**，不是逐个凑数；③ 若是反推，会直接抄实得值，**不会**在同一处同时改棋盘值**并**补 `HandEffectCountOn` 判别式。
- **夹具的「原版不可能态」会不会让那 5 处新期望值在别的夹具下不成立 —— 会（但规则不受影响）**：原版「打出一张牌 = 同一个 `CardScript` 从手牌搬到棋盘」⇒ **同一张牌不可能同时在手又在棋盘**。所以那些**绝对值**（3/4/5/…）只在夹具下成立；它们**验证的是规则**，不是「原版这一幕的数字」。作者如实标了，**可以接受**。
- **但它们有隐含前提，断言本身没钉**：`HandEffectCountOn(ctx,p,name)` 在「手牌里找不到那份同名实例」时**返回 0** ⇒ 每条 `== 1` 的前提是「那份真在手里」。我逐处核了夹具：`W3`（`Battle(new[]{watcher},…)`）· `W4` · `W6` · `W8` · `FixtureWatcher` ②-b（`BattlePool(new[]{watcher,own},…)`）· `FixtureHuntWatch` 反例（`ProbeBattle(new[]{hunter},…)`）· `FixtureKwListen` ④-b（`ProbeBattle(new[]{watcher},…)`）· `FixtureNonEph`（`Battle(new[]{watcher,plain,eph},…)`）—— **手牌里那份都在**（这些助手都把第一副牌库抽上手）。⇒ 今天不空过。⚠️ 但 5 条 `== 0` 的**反例**没有这条前提就**恒绿**（`0 == 0`）⇒ 建议每处补一条 `CheckTrue(手牌里那份找得到)`（**这是本轮唯一一条「将来会变假断言」的隐患**）。

### 必查 4 · `A885②`（消费者 + extrinsic）

- **① 现场扫手牌代替手牌级登记表 —— 不等价，两处**：见问题 6（去重键比原版粗 ⇒ 少补 N−1 条）与问题 8（载体全走光 ⇒ 登记表上这条效果消失）。**顺序**也不等价：原版按 `activeEffects` 的插入序，我们按**手牌序** ⇒ 多条效果同时兑现时 `Ops` 的应用次序可能不同（今天无数据能让它显形）。
- **② extrinsic 的生产者是近似，而且偏向误摘**：见问题 5。**该不该做**：**要做**（铁律 11）。判据明确（`AbilityData.whileInPlay +0x40` / `whileInPlayVariable +0x41`），拿不到的是**输入**不是**规则**；`ctx.ActingUnit != null` **不是**这个旗标的近似物 —— 它是「此刻有没有单位在行动」，与「这条能力是不是持续型」是两件事，一次性能力会被误标。
- **③ `expireHandBuffs` 里那条没做的（`extrinsic && HasCurrentTrait(enchantingCard, 0x82)`）—— 要做**
  · `0x82` = **130** = **`jam`**（判据 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs:13`；同一条已在 `RuleEngine/Core/BattleContext.cs:374` 记着）。
  · 判据行 = `PlayerHand__UpdateCardEffects.c:235` `HasCurrentTrait(lVar4,0x82,0)`，其中 `lVar4 = *(entry.cardEffect + 0x18)` = **`enchantingCard`（施放者）** ⇒ **作者写的语义（「施放者身上带 `jam`」）对**，不是「那张牌自己」。
  · **判据级意见**：**先做前置、再做这一条**。否则 `HasCurrentTrait(x, jam)` 在我们的词表上**恒假** = 死代码（`KeywordTable` 里也没有 `jam`；全池 1126 张**没有一张**带 `jam`），⇒ 建议按 `Ecstasy` 的先例先把 `jam` 进 `KeywordTable`（判据 = `DefinedTrait.cs:13`），再把这一支写进 `RuleCore.HandEffectExpired`。

### 必查 6 · 静默失败逐支

| 分支 | 出声吗 |
|---|---|
| 伴生名字读不出（`RuleCore.cs:1494-1500`） | 出声 ✅ |
| `ctx.CardPool == null`（`:1502-1507`） | 出声 ✅ |
| 卡池里按名字查不到（`:1508-1521`） | 出声 ✅（自检 ④ 钉住） |
| **没词条也没计数（`:1525`）** | ✗ **静默** ⇒ 问题 4 |
| `criteria` 判不出、退回「任何单位卡」（`:3251-3253`） | 会打「吃上了」但**不说**自己走退路 ⇒ 问题 9 |
| 非单位卡不吃（`:3251`） | 静默（可接受，作者已标注） |
| 建场期 `ctx.Players[owner] == null`（`:3208`） | 静默（可接受，作者已标注） |

### 必查 7 · `CardDef.cs:2241`

**那句注释现在是错的**（问题 7）。**新判据**：
- 机制 = 「打出带 `Companion` 词条的卡（或 `companionCounter > 0` 的卡）时，**按卡定义造一张新卡**（带词条 ⇒ `rawCardData.relatedCard1`；不带 ⇒ 它自己的 `rawCardData`）**放进手牌**，并把 `companionCounter = 源 − 1` 回填到新卡上形成递归链；**全程零移除、不上场、不读手牌**」。
- 出处：`CardScript__ProcessCompanion.c:26/29/35/70/74/85/88-89` · `CardScript__ShouldTriggerCompanion.c:12-29` · `BattleManager__AddNewCardToHand.c:65/:97` · `BattleManager._ResolveCreateHandCard_d__512:135` · `BattleManager__BroadcastCardCreated.c`。
- 「原版是**可**打出（玩家选）」这句**也不成立**（原版就是无条件造一张，手牌满时 `SendToCemetery` 且**仍发** `BroadcastCardCreated`）。
- `CardDef.cs:2243`（「没有卡面正文 ⇒ 不进 `RoutableTriggers`」）**仍然成立**，别一起改掉。

---

## 4. 断言鉴别力抽查（含「删掉实现它还绿吗」）

| 抽查对象 | 结论 |
|---|---|
| ②′ 三段（`RuleEngineTest.cs:3629-3701`） | **强**。删掉逐条判（换回整份一个到期位）⇒ A 得 2 / B 得 0 / C 得 0，**三处都红**。「再补一条 `HandEffects[0].Expire` 档位」也钉住了「留的是哪一条」而不是「留了几条」——不是同义反复 |
| 伴生 ①/②/③（`:10953-10994`） | **强**。删 `IsCompanion/CompanionCounter` ⇒ `made == null` 红；删递归支（`CompanionCounter > 0`）⇒ ③ 红；换回 `DeployFree + RemoveAt` ⇒ `dronesOnBoard == 0` 与「手牌张数不变」同时红。`Check(Hand.Count, hand0)` 单看是弱式（净 0 变化可由多种实现凑出），但被前两条夹住了 |
| 6 条 `HandEffectCountOn == 1` | **中**：删掉 `HandListenerSelfOp` 分流（全部走 `ResolveOps`）⇒ 实得 0 ⇒ 红，**有鉴别力**。⚠️ 它量的是**内部表**不是「打出后真的 +N」；本次**没有任何一条**新断言验证这条手牌效果**兑现**成攻值。建议补一条「把那张手牌打出来 ⇒ 攻值确实涨」的端到端断言 |
| 5 条 `HandEffectCountOn == 0` | **中偏弱**：靠「手牌里那份存在」这个**未钉住的前提**（§3 必查 3）。今天夹具都在，但另一条腿一改就变恒绿 |
| `AttachHandEffectCopy` 的日志断言 | 无（只有 ③ 的「出声」断言那一族是别的账）。建议 `SetupCardInHand` 至少给「补了几条」留一条可断言的东西 |
| 「兼容视图零改动」 | 已逐处核（§3 必查 2）——**没有**假绿 |

---

## 5. 要新开的账（铁律 11：全部要做，只分先后）

| # | 做什么 | 判据 | 落点 |
|---|---|---|---|
| 1 | `create N … 进手牌/对手手牌` 也补消费者调用 | `_ResolveCreateHandCard_d__512:135` → `AddCardNotDrawnToHand:34` `SetupCardInHand` | `RuleEngine/Core/EffectResolver.cs:1917` |
| 2 | 登记表去重键换成「整条记录」口径（至少 `Source + Payload + Duration + Target + Expire`） | `CardEffect__IsSameEffect.c`（逐字段比对） | `RuleEngine/Core/RuleCore.cs:3238-3241` |
| 3 | 登记表「载体全走光」的缺口：改成「效果记录本身」活着而不是「扫手牌」 | `PlayerHand__SetupCardInHand.c:38`（遍历 `activeEffects`）；⛔ 不许复活对局级的 `HandBuffTable` | `RuleEngine/Core/RuleCore.cs:3227-3246` |
| 4 | extrinsic 生产者换成真旗标（`AbilityData.whileInPlay*`）；拿不到就先**只对「持续型」标注**，⛔ 不用「此刻有单位」顶替 | `AbilityLogic__PlayAbility.c:1195-1198 / :1352-1355` | `RuleEngine/Core/EffectResolver.cs:2826` |
| 5 | `UpdateCardEffects` 第二条无条件支线：`extrinsic && HasCurrentTrait(enchantingCard, jam)` ⇒ 摘。**前置**：先把 `jam` 进 `KeywordTable` | `PlayerHand__UpdateCardEffects.c:235` + `DefinedTrait.cs:13` | `RuleEngine/Core/RuleCore.cs:3140 HandEffectExpired` |
| 6 | 三条伴生卡的数据缺陷：`keywords` 数组缺 `Companion` 条目时，从 `desc` 登记关键词 + 从 `desc` 的 `Companion N:` 取 N | `CardScript__ShouldTriggerCompanion.c:12-29`；卡面 `TAU38/TAU28/TAU13` 的 `desc` | `RuleEngine/Core/CardDef.cs`（`CollectCompanionName` 一带，照 `:1175-1180` 的 `Ecstasy` 先例） |
| 7 | `PlayCompanions` 的静默支出声（`else return;` 前面打一句日志） | 红线「不许静默失败」 | `RuleEngine/Core/RuleCore.cs:1525` |
| 8 | `CardDef.cs:2241-2242` 的注释就地订正（保留更正痕迹） | §3 必查 7 的新判据 | `RuleEngine/Core/CardDef.cs:2241` |
| 9 | `criteria` 退路出声（说明自己是退路档）+ 考虑默认**不放行** | `PlayerHand__SetupCardInHand.c:58` | `RuleEngine/Core/RuleCore.cs:3251-3253` |
| 10 | `inst.HandEffects[0]` 加护栏（或改读 `LastHandEffect`） | 同函数下的 `if (e == null) continue;` 口径 | `RuleEngine/Core/RuleCore.cs:3013` |
| 11 | `EnforceHandLimit` 的注释订正（「语义一致」→「手牌未超上限时一致；已超上限时会多丢」），或改成原版口径（先判再 Add） | `_AddCardNotDrawnToHand_d__39:26-30/63-65` | `RuleEngine/Core/RuleCore.cs:1086-1099`、`:1541-1542` |
| 12 | （顺手）`BattleDriver.cs:2599` 这个指针改成 `:2607` | 现读 | 文档 | 

---

## 6. 没查清的部分

1. **`AbilityData.whileInPlay` 的实际取值** —— `+0x40/+0x41` 由谁写、哪些能力是持续型，本地没有那张描述符表（作者的「没查清」我**确认成立**，但**不是**它说的那条）。⇒ 问题 5 的「哪几张卡会误摘」只能列出**嫌疑**（`GOF84` / `GOF81`），**不能**给确定清单。
2. **`CardEffect +0x38` 是什么** —— `IsSameEffect` 先比它再比一堆旗标；它若是「每条实例一个唯一号」，则原版对**每次触发**都算不同记录（与我们的叠加口径一致）；若是模板号，则原版的叠加语义需要重审。反编译里没找到写点。⇒ `A885②` 去重该照哪个口径，**取证不足**。
3. **`jam` 词条在我们词表上的落点** —— 我判「要做 + 先登记关键词」，但**没有**核对 `KeywordTable.Prefixes` 里加 `jam` 会不会与别的前缀冲突（本笔未做）。
4. **21 条新断言 + 12 条改口期望值一律没被跑过** —— 我只能静态核判据链，**没有执行**（红线：不跑 Unity）。全部「看起来会绿」，但**不是结论**。
5. **`EffectResolver.cs:1917` 那一支的 `made` 是否可能已经带过消费者** —— 我按 `op.Dest == "hand"/"enemyhand"` 的造牌路径判「没有」，但**没有**逐行走完 `CreatePool` 那条链的每一跳来排除别的插入点。
