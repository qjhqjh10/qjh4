# B8_R · 引擎广播事件种类（`EvtKind` 20 vs 原版 35）—— 查证与补全

> 执行代理 **B8_R**（完全复刻批次 8）· 2026-10-26 · 判据 = `d:/2/tools/decomp_full/`（🥇）+ `AbilityTrigger.cs` 枚举真值 + `cards_engine.json`（1,128 张）可达性现读。
> 白名单：`RuleEngine/Core/BattleEvent.cs` · `WhenEvent.cs` · `EffectText.cs` · 账补丁 `_parts/B8_R.tsv` · 本报告。

---

## ① 结论（三句）

1. 🔴 **「`EvtKind` 20 个成员 vs 原版广播 35 种」是【两条不同的轴】，条数无可比性 —— 不是缺口。**
   原版根本没有「事件种类枚举」这种对象：`BattleManagerSupport__Broadcast*`（**43 个**）+ `BattleManager__Broadcast*`（**30 个**，其中 **24 个是纯转调**）+ `MinionManager__BroadcastActivateMinion`（1 个）是**监听者派发器**，最终由 `RawCardScript.OnTrigger(id, …)` 带出 **57 个不同的 `AbilityTrigger` id**。
   我们的对应物是 **`WhenEventKind`（29 种 + `triggers:<kw>` 家族）＋ `EffectResolver.BroadcastWhen`（60 余个调用点）**；`EvtKind` 是**表现层词汇**（原版没有对应枚举）。
2. ✅ **原版 49 条 `Broadcast*` 逐条对下来：44 条已有等价实现 / 5 条真缺口 / 0 条「原版也没有」。**
   5 条真缺口 = `TurnSetup(155)` · `TurnStarted(160)` · `UnitPoisoned(290)` · `CardCreatedInHand(210)` · `TrapResolved(270)`。
3. 🔑 **这 5 条的可达性全部是 0 张**（`cards_engine.json` 1,128 张现读）⇒ **今天没有任何一张卡会因为补上它们而从『不响』变『响』**。它们是**休眠机制**（铁律 11 仍要做），但**不构成本账升 A 的判据** ⇒ 没有为它们新增 `EvtKind` 成员（加了就是死代码）。

**另外查到一条真差异（不是「种类缺」，是「跳数多」）**：原版有 **6 条**广播**只有棋盘一跳、完全没有手牌跳**（`CollectFaith` / `CollectQuestPoints` / `CollectSpiritStone` / `SabotageCreated` / `SecretCreated` / `EarnedSkull`）；我们只有 `EarnedSkull` 收窄了（F2 已做），**另外 5 条仍走通用四跳** ⇒ 手牌里躺着的监听者会被**多叫醒一次并真的结算**。可达监听卡 = **1 + 3 + 4 + 1 + 2 = 11 张**。修法 = 给这 5 处传 `boardAndPersistentOnly: true`（一行一处，落在 `EffectResolver`，非本代理白名单）。

---

## ② 逐条证据：原版 49 条广播的完整去向

### ②·0 取证方法（可复跑）

```bash
ls D:/2/tools/decomp_full/ | grep -i "BattleManager.*__Broadcast"     # 75 个（含编译闭包 1 个）
# 每条广播 → 它调的 CardScript 接收函数 → 接收函数里的 OnTrigger(id, …) 常量
# → 用 D:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs 解名字
# → 用 MyGame/Assets/RuleEngine/Resources/cards_engine.json 的 abilityTriggers 数可达卡
```
- 接收端 trigger 号解码样例（逐条核过）：`0x78`=120 Slay · `0x1ae`=430 Requiem · `0x1e0`=480 · `0x1d6`=470 · `0x299`=665 · `0x2b7`=695 · `0x262`=610 · `0x2bd`=701 · `700` · `0x29`=41 · `0x2b`=43 · `0x28`=40 · `0x2d`=45 · `0x9b`=155 · `0x1e`=30 · `0x1f`=31 · `0xe6`=230 · `0x2e`=46 · `0x1b8`=440 · `0xa0`=160 · `0x32`=50 · `0x244`=580 · `300` · `0x12d`=301 · `0x28a`=650 · `0x82`=130 · `0x2d0`=720 · `0x2cb`=715 · `0x3c`=60 · `0xb9`=185 · `0xbb`=187 · `0x295`=661 · `0x2d6`=726 · `0x46`=70 · `0x1c2`=450 · `0x287`=647 · `0x285`=645 · `0x208`=520 · `0x1fe`=510 · `0x203`=515 · `0x122`=290 · `0x294`=660 · `0x23a`=570 · `0x286`=646 · `0x1b3`=435 · `0x1c7`=455 · `0x123`=291 · `0xbe`=190 · `0xc3`=195 · `0x25d`=605 · `0x226`=550 · `0x2a8`=680 · `0x26c`=620 · `0x276`=630 · `0xe1`=225 · `0x104`=260 · `0xfa`=250 · `0x230`=560 · `0x10e`=270 · `0x1ea`=490 · `0x24e`=590 · `0xd2`=210。

### ②·1 43 条 `BattleManagerSupport__Broadcast*`

> 「可达」= `cards_engine.json` 里 `abilityTriggers` 含该 id 的卡数（现读 1,128 张）。
> 「跳数」= 预调（对事件主体自己先跑一遍） / 棋盘（`+0x470` allCardsInPlay，多数带 `IsInPlay` 过滤） / 手牌（`GetCardsInHandRef`，两跳 = 双方各一次）。

| # | 原版广播 | 接收端 → trigger id（可达） | 跳数 | 我们的落点 | 判 |
|---|---|---|---|---|---|
| 1 | `BroadcastCardDrawn` | `CardDrawn` → 260 OtherCardDrawn（2）· 250 ThisCardDrawnAsTrap（0） | 预调 + 棋盘 + 手牌×2 | `RuleCore.cs:2981`（`BroadcastWhen(Draw)` → `EffectResolver.cs:7277` TN_OtherCardDrawn） | 已有 |
| 2 | `BroadcastCardPlayed` | `ResolveCardPlayed` → 10 OtherCardPlayed（7）· 0 ThisCardPlayed（586）· 590 Combo（0）；另转 `ResolveUnitSummoned`（见 #39） | 预调 + 棋盘 + 手牌×2 | `RuleCore.cs:4044` + `EffectResolver.cs:2511`（`Play`）· `ActionPump.cs:1099`（rally → `ThisCardPlayed`） | 已有（Combo 590 池内 0 张） |
| 3 | `BroadcastCardReturnedDeck` | `CardReturnedDeck` → `RawCardScript.OnCardReturnedDeck` → 480（0） | 预调 + 棋盘 + 手牌×2 | `EffectResolver.cs:15458-15463`（`RecordCardReturnedEvent`）+ `:7318` | 已有 |
| 4 | `BroadcastCardReturnedHand` | 同上 → 470（0） | 同上 | 同上 + `:7317` | 已有 |
| 5 | `BroadcastCollectFaith` | `ReactToCollectFaith` → 665（**1**） | **预调 + 棋盘，无手牌跳** | `EffectResolver.cs:12633-12638` · `RuleCore.cs:1632` · `EffectResolver.cs:12696` | 已有，**跳数多两跳** ⇒ 见 ③ |
| 6 | `BroadcastCollectQuestPoints` | `ReactToCollectQuestPoints` → 695（**3**） | **预调 + 棋盘，无手牌跳** | `EffectResolver.cs:12627` | 同上 |
| 7 | `BroadcastCollectSpiritStone` | `ReactToCollectSpiritStone` → 610（**4**） | **预调 + 棋盘，无手牌跳** | `RuleCore.cs:12065`（`CollectWaystoneCore`） | 同上 |
| 8 | `BroadcastDeadUnit` | `ResolveDeadCard` → 120 Slay（16）· 430 Requiem（0） | 预调×2 + 棋盘 + 手牌×2 | `RuleCore.cs:6163`（`Kills`→`Slay`，`WhenEvent.cs:2087`）· `RuleCore.cs:9754`（`Die`） | 已有（430 休眠） |
| 9 | `BroadcastEarnedSkull` | `TriggeredEarnSkull` → 640（0） | **只有棋盘一跳** | `EffectResolver.cs:15528-15535`（**已用 `boardAndPersistentOnly: true`**） | 已有（唯一已收窄的） |
| 10 | `BroadcastRemoveEffectsFrom` | `RemoveExtrinsicEffectsFrom` → **无 OnTrigger** | 棋盘 + 手牌×2 | `UnitState.RemoveExtrinsicEffectsFrom` + `Aura.Recompose`（账 :714 B） | 已有（等价） |
| 11 | `BroadcastRemoveFromGame` | `RemoveFromGameCard` → **无 OnTrigger** | 棋盘 + 手牌×2（9 条 `Ineq`） | 账 :715 已记 **C** | 已有（等价，另账） |
| 12 | `BroadcastSabotageCreated` | `ReactToSabotageCreated` → 701（**1**） | **预调 + 棋盘，无手牌跳** | `EffectResolver.cs:4008` | 已有，**跳数多两跳** ⇒ 见 ③ |
| 13 | `BroadcastSecretCreated` | `ReactToSecretCreated` → 700（**2**） | **预调 + 棋盘，无手牌跳** | `EffectResolver.cs:4000` / `:4823` / `:12933` | 同上 |
| 14 | `BroadcastTurnBeforeEnd` | `OnTurnBeforeEnd` → 41（**1**） | 棋盘 + 手牌×2 | `EffectResolver.cs:8269-8286`（BeforeEnd→End 两遍） | 已有 |
| 15 | `BroadcastTurnEnd` | `OnTurnEnd` → 43 Courtesy（0）· 40 TurnEnd（**11**）· 45 Resolution（0） | 棋盘 + 手牌×2 | `EffectResolver.cs:8269-8345` | 已有 |
| 16 | `BroadcastTurnSetup` | `OnTurnSetup` → 155（**0**） | 棋盘 + 手牌×2 | **缺**（`RuleCore.cs:1917-1923` 自认没有钩子） | 🔴 **真缺口（休眠）** |
| 17 | `BroadcastTurnStart` | `OnTurnStart` → 30（5）· 31（8）· 230 Relentless（0）· 46 Talent（96）· 440 Landing（0） | 棋盘 + 手牌×2 | `EffectResolver.cs:8280-8345`（`TurnTriggerNameOf` 30/31 二选一）· 46 走 `Talent` 关键词 | 已有 |
| 18 | `BroadcastTurnStarted` | `OnTurnStarted` → 160（**0**） | 棋盘 + 手牌×2 | **缺**（`TurnTriggerNameOf` 没有这一档） | 🔴 **真缺口（休眠）** |
| 19 | `BroadcastUnitAttacked` | `ResolveUnitAttacked` → 50（13）· 580 Strike（30）· 300 Mob（16）· 301 Regiment（13） | 预调×2（被打者→攻击者）+ 棋盘 + 手牌×2 | `RuleCore.cs:6773`（`Attack`，`preCalled`/`preCalled2`） | 已有 |
| 20 | `BroadcastUnitCodex` | `TriggerOtherCardCodex` → 650（1） | 棋盘 + 手牌×2 | `RuleCore.cs:12433/12548` → `EffectResolver.cs:7371` | 已有 |
| 21 | `BroadcastUnitDamaged` | `ResolveDamageDealt` → 130 Rage（5）· 720（9）· 715（10）· 60（3）· 185（0）· 187（0） | 预调 + 棋盘 + 手牌×2 | `RuleCore.cs:9054-9097`（`EmitHit`） | 已有（130=`penitence`；185/187 休眠） |
| 22 | `BroadcastUnitDuty` | `TriggeredDuty` → 661（1） | 棋盘 + 手牌×2 | `RuleCore.cs:12433/12548` → `EffectResolver.cs:7346` | 已有 |
| 23 | `BroadcastUnitFerocity` | `TriggeredFerocity` → 726（2） | 棋盘 + 手牌×2（**0 条 `Ineq`**） | 同上 → `EffectResolver.cs:7344` | 已有 |
| 24 | `BroadcastUnitHealed` | `ReactToUnitHealed` → 70（**0**） | 棋盘 + 手牌×2（**无预调**） | `EffectResolver.cs:15502-15512` | 已有（休眠） |
| 25 | `BroadcastUnitJammed` | `ReactToUnitJammed` → **无 OnTrigger** | 棋盘 + 手牌×2 | `EffectResolver.cs:15655-15724`（`ResolveJamUnit`） | 已有（等价） |
| 26 | `BroadcastUnitLanded` | `TriggerOtherCardLanding` → 450（0） | 棋盘 + 手牌×2 | `RuleCore.cs:7673` → `EffectResolver.cs:7388` | 已有 |
| 27 | `BroadcastUnitLoseStealth` | `TriggerLoseStealth` → 647（**1**） | 棋盘 + 手牌×2 | `EffectResolver.cs:10727` + `WhenEvent.cs:2087`（B8_C 已补映射） | 已有 |
| 28 | `BroadcastUnitMob` | `TriggerOtherCardMob` → 645（**1**） | 棋盘 + 手牌×2 | `RuleCore.cs:6673/10195` → `EffectResolver.cs:7370` | 已有 |
| 29 | `BroadcastUnitObtainedHuntMark` | `ReactToUnitHuntMarked` → 520（2） | 预调 + 棋盘 + 手牌×2 | `EffectResolver.cs:10727-10757`（`BroadcastKeywordEvent`） | 已有 |
| 30 | `BroadcastUnitObtainedMark` | `ReactToUnitMarked` → 510（6） | 预调 + 棋盘 + 手牌×2 | 同上 + `EffectResolver.cs:12075` | 已有 |
| 31 | `BroadcastUnitObtainedMarkerlight` | `ReactToUnitMarkerlight` → 515（1） | 预调 + 棋盘 + 手牌×2 | 同上 | 已有 |
| 32 | `BroadcastUnitPoisoned` | `ReactToUnitPoisoned` → 290（**0**） | 棋盘 + 手牌×2 | **缺**（只有毒支回合末摧毁） | 🔴 **真缺口（休眠）** |
| 33 | `BroadcastUnitPray` | `TriggeredPray` → 660（3） | 棋盘 + 手牌×2 | `RuleCore.cs:11775` → `EffectResolver.cs:7345` | 已有 |
| 34 | `BroadcastUnitReanimated` | `ReactToUnitReanimated` → 570（5） | 预调 + 棋盘 + 手牌×2 | `EffectResolver.cs:15187/15229` | 已有 |
| 35 | `BroadcastUnitRegiment` | `TriggerOtherCardRegiment` → 646（**0**） | 棋盘 + 手牌×2 | `RuleCore.cs:6695` → `EffectResolver.cs:7372` | 已有（休眠） |
| 36 | `BroadcastUnitRequiem` | `TriggerOtherCardRequiem` → 435（**0**） | 棋盘 + 手牌×2 | `EffectResolver.cs:15558-15565` | 已有（休眠） |
| 37 | `BroadcastUnitSpiritStone` | `TriggerOtherCardSpiritStone` → 455（**1**） | 棋盘 + 手牌×2 | `EffectResolver.cs:961` + `:7295` | 已有 |
| 38 | `BroadcastUnitStunned` | `ReactToUnitStunned` → 291（2） | 预调 + 棋盘 + 手牌×2 | `EffectResolver.cs:10727` + `:7309` | 已有 |
| 39 | `BroadcastUnitSummoned` | `ResolveUnitSummoned` → 190（11）· 195（1）· 605（0 张同时命中）· 550（0）· 680（9） | 预调 + 棋盘 + 手牌×2 | `RuleCore.cs:4076/4855`（`Deploy`）· `:4105`（`FireTriggerOnSide(Uprising)` = 原版 `:82-105` 那支） | 已有 |
| 40 | `BroadcastUnitSwarm` | `TriggeredSwarm` → 620（2） | 棋盘 + 手牌×2 | `RuleCore.cs:4538-4560` / `:4653` → `EffectResolver.cs:7347` | 已有 |
| 41 | `BroadcastUnitSynapse` | `TriggeredSynapse` → 630（3） | 棋盘 + 手牌×2 | `EffectResolver.cs:2746` → `:7353` | 已有 |
| 42 | `BroadcastUnitUsedSacrifice` | `ReactToUnitUsedSacrifice` → 225（**0**） | 预调 + 棋盘 + 手牌×2 | `RuleCore.cs:9351` + `WhenEvent.cs:1886-1901` + `EffectResolver.cs:7386` | 已有（休眠） |
| 43 | `BroadcastWhileInPlay` | `UpdateWhileInPlay` → **无 OnTrigger** | 棋盘 + 手牌×2（6 条 `Ineq`） | `EffectResolver.EmitWhileInPlayPass`（`EffectResolver.cs:7913`）；账 :747 已 C | 已有（另账 C） |

### ②·2 `BattleManager` 自己实现的 6 条（其余 24 条是纯转调）

| # | 原版广播 | 接收端 → trigger id（可达） | 跳数 | 我们的落点 | 判 |
|---|---|---|---|---|---|
| 44 | `BroadcastActivateMinions(bool)` | `MinionManager.BroadcastActivateMinion` + `CardScript.ActivateMinion`（**无 OnTrigger**）+ `PlayerHand.LightPlayableHand` | 逐卡 | `RuleCore.cs:3488-3497`（`RecomputeActAndAttack`）· `RuleCore.cs:1675-2135`（`BeginTurnCore`）· `BattleDriver.cs:26810-26820`（手牌高亮） | 已有 |
| 45 | `BroadcastCardCreated(CardScript)` | `ReactToCardCreated` → 210（**0**） | 棋盘 + **手牌×1**（`GetCardsInHandList` 七个 true；**无预调、无自排除**） | **缺** | 🔴 **真缺口（休眠）** |
| 46 | `BroadcastChangedLanguage()` | `Component.BroadcastMessage("ChangedLanguage")`（**纯表现，无 OnTrigger**） | — | `BattleHudLanguage.cs` + `BattleDriver.cs:32613-32640` | 已有（表现层） |
| 47 | `BroadcastQuestResolved(CardScript)` | `QuestResolved` → `RawCardScript.OnQuestResolved` → 490（**4**） | 棋盘 + **手牌×1**（排除 questCard） | `EffectResolver.cs:12848-12940`（`QuestPointThreshold` = **结果层**等价） | 已有（机制不同，见 ④） |
| 48 | `BroadcastTacticPlayed(CardScript, CardScript)` | `TacticPlayed` → `RawCardScript.OnTacticPlayed` → 560 Artifice（**19**） | **只有棋盘一跳**（0 处 `GetCardsInHandRef`、无预调） | `EffectResolver.cs:2659`（`FireTriggerOnSide(Artifice)`）· `:7579`（SAU11） | 已有 |
| 49 | `BroadcastTrapResolved(CardScript)` | `TrapResolved` → 270（**0**） | 棋盘 + **手牌×1**（排除 trapCard） | **缺** | 🔴 **真缺口（休眠）** |
| — | `MinionManager.BroadcastActivateMinion(bool)` | `CardScript.ActivateMinion`（无 OnTrigger） | — | `RuleCore.cs:2047-2051`；账 :1245 **已 A** | 已有 |

**计数：44 已有 · 5 真缺口（全部 0 张可达）· 0「原版也没有」。**

### ②·3 「原版也没有」的三条（**不补**，沿用 B8_C 的裁定）

| 我们的 `WhenEventKind` | 为什么不补（判据） |
|---|---|
| `getsshield` | `AbilityTrigger.cs` 全 97 条 `grep -i shield` = **0 行**；`CardScript` 的 15 个 `ReactTo*` 里没有 shield。⚠️ `BroadcastUnitObtainedMark` 的 `Mark` 是 **Mark of Chaos**（510），不是盾 |
| `becomesremnant` | 枚举无 `Remnant`；`CardScript.TransformIntoRemnant()` 体内 0 处 `OnTrigger`（🥈 `out_cpp2il_dev_ilspy/CardScript.cs:14973-15008`） |
| `unitjammed` | `CardScript__ReactToUnitJammed.c` 体内 **0 处 `OnTrigger`**（本轮复验：全文 `grep OnTrigger` = 0）——这与 `BroadcastUnitJammed` **存在**并不矛盾：它是「清理型广播」（撤效果 + `UpdateWhileInPlay`），不带 trigger 号 |

---

## ③ 真差异：6 条广播「没有手牌跳」，我们只收窄了 1 条

**判据**（`grep -c GetCardsInHandRef`，逐份全文）：

| 文件 | `GetCardsInHandRef` | `GetCardsInHandList` |
|---|---|---|
| `BroadcastCollectFaith.c` | **0** | 0 |
| `BroadcastCollectQuestPoints.c` | **0** | 0 |
| `BroadcastCollectSpiritStone.c` | **0** | 0 |
| `BroadcastSabotageCreated.c` | **0** | 0 |
| `BroadcastSecretCreated.c` | **0** | 0 |
| `BroadcastEarnedSkull.c` | **0** | 0 |
| （对照）`BroadcastUnitHealed.c` / `UnitDuty.c` / `UnitMob.c` | 2 / 2 / 2 | — |

我们这一侧：`BroadcastEarnedSkull` **已经**用 `boardAndPersistentOnly: true`（F2 做的，`EffectResolver.cs:15528-15535`）；**另外 5 处仍是通用四跳**：

| 事件 | 我们发出点 | 可达监听卡（`abilityTriggers` 含该 id 的卡） |
|---|---|---|
| `GainFaith`(665) | `EffectResolver.cs:12633-12638` · `RuleCore.cs:1632` · `EffectResolver.cs:12696` | **1**（SOR38 Paragon Warsuit） |
| `GainQuest`(695) | `EffectResolver.cs:12627` | **3**（DA5 / DA12 / DA6） |
| `GainSpirit`(610) | `RuleCore.cs:12065` | **4**（ASH80 / ASH28 / ASH78 / ASH25） |
| `CreatesSabotage`(701) | `EffectResolver.cs:4008` | **1**（GSC68 Atalan Jackal） |
| `CreatesSecret`(700) | `EffectResolver.cs:4000` / `:4823` / `:12933` | **2**（DA15 / DA66） |

**为什么这真的会错（不只是「多醒一次」）**：`BroadcastHandWhen`（`EffectResolver.cs:10536+`）对手牌里的单位卡会走 `c.FireWhen(...)` → `ResolveOps`，**真的结算**。即：`SOR38 Paragon Warsuit` 躺在手里、你获得 1 点信仰 ⇒ **我们会对敌方督军造成 4 点伤害**，原版不会（原版这条广播根本不扫手牌）。

**修法**：这 5 处 `BroadcastWhen` 加 `boardAndPersistentOnly: true`（与 `BroadcastEarnedSkull` 逐字同形）。落在 `EffectResolver.cs`（**非本代理白名单**）⇒ 见 ⑥ 请求。
⚠️ 注意：`boardAndPersistentOnly` **仍保留** `BroadcastPersistentWhen`（「弃牌堆里那张战术卡在听」那一跳）—— 原版没有这一跳，但那是**我们自己的**机制（`EffectResolver.cs` 的 ⓪-b 注释已如实标注「没有判据」），改它会牵动别的账，本代理**不动**。

---

## ④ 没查清的部分（如实标着，⛔ 不当成已对齐）

1. **`BroadcastUnitDamaged` 的棋盘跳是否排除被打者** —— `.c` 把 `op_Inequality` 的**第二实参丢了**（`BattleManagerSupport__BroadcastUnitDamaged.c:38` 等处只剩 1 个实参），而同族 `Obtained*` 那几份里**显式写出来了**。⇒ 只能靠 **VA 级反汇编**判。我们按「预调 + 排除」实现（`RuleCore.cs:9093-9096`）；若原版**不**排除，则被打者自己会被叫醒两次（池内 60 = 3 张，其中 `Pray Enchantment` 那条是「受伤即撤」⇒ 双响无害但仍是差异）。
2. **`BroadcastUnitSummoned` → `OnTrigger(0x25d = 605 UseFaith)`** —— 原版在「**这张单位卡自己被召唤**」（`param_2 == param_1` ∧ `cardType == 0`）时，紧接着 195 再发一次 **605**。语义上讲不通（605 是「用了信仰能力」），而且**没有任何一张卡同时带 195 与 605**（现读 1,128 张）⇒ 今天零可观测。**没查清**：是枚举值在这份 build 里被重编号，还是真有一条「召唤即视为用信仰」的隐藏口径。⇒ ⛔ 不补、也不据此改 605 的语义。
3. **`BroadcastTacticPlayed` 的 `HasCurrentTrait(0x410)`** —— `0x410` = 1040 这个 `DefinedTrait` 与我们的 `KeywordTable.Artifice` 是不是逐一对应，**没做池内逐张对账**（19 张）。
4. **`BroadcastQuestResolved` 的机制差异边界** —— 我们走结果层（「督军阵营 == DarkAngels」当判据）。⇒ 4 张 DA 督军结果相同，但「别的阵营以后加 490 能力」这一档表示不出来。原版是**数据驱动**的（`createCardInDeck = 270`），`QuestTriggered` / `createCardInDeck` 两个符号在 `Assets/` 仍 **0 命中**。
5. **`EvtKind` 到底该不该按「表现时机」补** —— 本轮只证明了「不能按原版 `Broadcast*` 的条数补」。**表现层**那边（`VfxMap.cs`）有自己的口径「⛔ 不许照着原版 trigger 名字表补齐，加了 = 永远等不到」；要补哪一种，得由 `战斗界面_HUD` / `特效动画` 那两本账提需求，**不是从广播族推**。

---

## ⑤ 顺手发现（⛔ 都不是我该改的文件）

1. 🔴 **`WhenEvent.cs:2093` 的 `KindsAbsentInOriginal` 与 `EffectResolver.cs:15811` 的提示文字里写着「三种」** —— 现状确实 3 个（`GetsShield` / `BecomesRemnant` / `UnitJammed`）✓ 一致，**无需改**（查过一遍免得下次当成不一致）。
2. ⚠️ **账 `:707` / `:708` 的落点写成 `EffectResolver.cs:15458-15451`（起 > 止，是个笔误）** —— 真身是 `15458-15463`（`RecordCardReturnedEvent`）。这两行不在本代理的 30 行里，**未改**，留给下一次碰这两行的代理。
3. ⚠️ **账 `:709` 的落点 `EffectResolver.cs:12445-12446 · 12508-12509` 已经漂了** —— 现读 `GainFaith` 的实发点是 `RuleCore.cs:1632` / `EffectResolver.cs:12633-12638` / `:12696`。（`:12445` 现在是 `HealUnit` 附近。）本代理在 `B8_R.tsv` 的「`BroadcastCollectedFaith`」那一行（即 `BattleManager.CollectedFaith`）里给了新落点，但 `BattleManagerSupport.CollectFaith` 那一行**未改**。
4. 🟢 **`abilityTriggers` 含 0（`ThisCardPlayed`）的 586 张里，有 481 张卡面没有 `Rally`** —— 一度怀疑是缺口，**核对后不是**：那 481 张**全部是 `tactic` / `defence` 卡**（「打出即结算」= trigger 0 就是它自己的正文），走 `spellType` / 主动能力那条路。**记下来免得下一个人重踩这个假警报**。
5. 🟢 **`EarnedSkull(640)` 的可达性是 0**（`cards_engine.json`），与 F2 报告的「0 张」**互相印证**；但 `cards_truth.json`（1,351 张 SO 全集）里含 640 的有 12 张 ⇒ **两个张数口径不同，⛔ 别互相校验**（正是 CLAUDE.md 那条「三个张数口径别互相校验」）。

---

## ⑥ 给调度台的请求

### ⑥·1 需要别的写手做的（都在 `EffectResolver.cs` / `RuleCore.cs`，不在我的白名单）

| 优先级 | 改什么 | 判据 |
|---|---|---|
| **P0** | 给 5 处 `BroadcastWhen` 加 `boardAndPersistentOnly: true`：`EffectResolver.cs:12633`（GainFaith）、`:12627`（GainQuest）、`RuleCore.cs:12065`（GainSpirit）、`EffectResolver.cs:4008`（CreatesSabotage）、`:4000`/`:4823`/`:12933`（CreatesSecret） | 原版那 5 份 `Broadcast*` 全文 `GetCardsInHandRef` = 0（见 ③ 表）；同形先例 = `EffectResolver.cs:15528-15535` |
| P1 | `BroadcastCardCreated`(210) 发射口 | `BattleManager__BroadcastCardCreated.c:24-41`；0 张可达 |
| P1 | `BroadcastTrapResolved`(270) 发射口 | `BattleManager__BroadcastTrapResolved.c:26-50`；0 张可达 |
| P1 | `TurnSetup(155)` / `TurnStarted(160)` 相位钩子 | `BattleManagerSupport__BroadcastTurnSetup.c` / `…TurnStarted.c`；各 0 张可达 |
| P1 | `UnitPoisoned(290)` 发射口 | `BroadcastUnitPoisoned.c` + `ReactToUnitPoisoned.c:7`；0 张可达 |
| P2 | `UnitDamagedByAbility(185)` / `UnitDamagedByAttack(187)` | `ResolveDamageDealt.c` 的 `0xb9`/`0xbb` 两支；各 0 张可达 |

### ⑥·2 `play check` 指纹

- **本代理零行为变化**：唯一改的代码文件 `BattleEvent.cs` 的 `git diff --numstat` = **`41 0`**（41 行纯插入、0 行删除），插入位置在**文件末尾（原 719 行之后）**，全是 `//` 注释 ⇒ 结构上不可能改行为。
- **实测（时间顺序，可复核）**：

  | 时刻 | 命令 | 读数 |
  |---|---|---|
  | 07:15:05 | `play check` | 指纹 `a93ffb8f344517a9ac611ef06de17a3a` **不变**、**rc=0** |
  | 07:18:25 | （我改 `BattleEvent.cs`，纯注释） | — |
  | 07:22:28 / 07:22:58 | `play check` | 指纹 `350c16b2761e9a662b1f29ebaf69500e`、**rc=1**（两次一致 ⇒ 是稳定态，不是抖动） |

- 🔴 **这次的漂移来自【别的代理】，不是本代理**：`Core/` 下 `UnitState.cs`(07:20:59) · `PlayerState.cs`(07:20:37) · `CardDef.cs`(07:19:47) · `EffectResolver.cs`(07:18:52) 全在我那次 `rc=0` 读数（07:15）之后被改；`Resources/cards_engine.json` 的 mtime 是 **01:57（未动）** ⇒ 不是数据侧。⇒ 请调度台在同步点按**那些**改动重算指纹，⛔ 本代理不需要为它负责、也不要据此重钉基线。
- ⛔ **若采纳 ⑥·1 的 P0**（`boardAndPersistentOnly`），指纹**还会再变一次**（手牌监听者被少叫醒 ⇒ 若干卡 `net` 变化）。那一次也**不要单点重钉**，等整批改完一次性重钉。
- 我改完后复跑：`ruleprobe check` = **rc=0**（解析差异 0 行 · 哨兵失败 0 条）；`TMPDIR=/tmp/wf_b8r3 typecheck.sh` = **0 / 0**。
  （中途两次并发假错：`typecheck` 首轮报 `EffectResolver.cs` 的 3 条 `CS0103`；`play check` 一轮构建失败报 `UnitState.cs` 的 4 条 `CS0103 RawKw` —— 都是**别人正在写的文件**，隔一轮重跑即恢复，按铁律 13·3 不当成自己的错。）

### ⑥·3 账的预期

- 补丁 `_parts/B8_R.tsv`：**33 条数据行**（+1 行表头），`--dry` 结果 = **更新 33 · 新增 0**（键全部命中主表）。
- 组成 = `BattleManager.Broadcast*` **30 行**（其中 28 行 B、2 行 C）＋ `BattleManagerSupport.Broadcast{TurnSetup,TurnStarted,UnitPoisoned}` **3 行**（全 B）⇒ **31 行 B + 2 行 C（等级 P0）**。
- ⛔ **零 A** ⇒ `check_a_vs_diff.py` / `verify_annot.py` 的可疑行数**不受本补丁影响**（现读 13 条弱候选全在别的域、缺注解 0）。
- ⚠️ **本补丁只动 `我们的落点` / `判据出处` / `判据要点` / `差异` / `备注` 五列**，`方法` 列**逐字取自主表**（merge 键 = (原版类, 方法)）；`轮次` 写 `B8`。合并请带 `--only B8_R`（已实测 dry-run）。

---

## ⑦ 改了哪些文件哪些行

| 文件 | 改了什么 |
|---|---|
| `RuleEngine/Core/BattleEvent.cs` | **只在【文件末尾】追加 41 行注释块**（719 → 760 行）：说明「`EvtKind` ≠ 原版广播族」这条轴口径 + 44/5/0 的去向计数 + 可达性结论 + 报告指针。⛔ **没有新增任何 `EvtKind` 成员**（理由见 ①·3：原版没有对应枚举；加成员必须有发射口与消费口，否则是死代码；且 5 条缺口命中卡数全为 0）。**零行号漂移**（账引用 `BattleEvent.cs:177` 未受影响）。 |
| `RuleEngine/Core/WhenEvent.cs` | **未改**。逐条核过：`TriggerNameOfWhenKind` 已覆盖全部 29 个 `WhenEventKind` + 11 个 `triggers:<kw>`；`ExtraTriggerNameForKind`（`:2087`）已覆盖 `kills`/`losestealth`；`KindsAbsentInOriginal`（`:2093`）三个成员与原版一致。**没有可补的映射**（`triggers:<kw>` 那族在卡池里只出现 synapse / ferocity / swarm / duty / mob 五个词，全部已映射）。 |
| `RuleEngine/Core/EffectText.cs` | **未改**（无需要新增的文案；`EvtKind` 无新成员 ⇒ 没有新驱动文案）。 |
| `资料/完全复刻/账/_parts/B8_R.tsv` | **新建**，33 行（见 ⑥·3）。 |

**验证**：`TMPDIR=/tmp/wf_b8r bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**；`ruleprobe check` ⇒ **rc=0**。
（首次 typecheck 报了 3 条 `EffectResolver.cs` 的 `CS0103` —— 那是**另一个代理正在写的文件**，隔一轮重跑即 0/0，按铁律 13·3 不当成自己的错。）
