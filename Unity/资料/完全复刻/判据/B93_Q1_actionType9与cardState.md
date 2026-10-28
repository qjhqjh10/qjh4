# B93_Q1 · `BattleActionType` 91 项 vs `ResolveAction` 82 case 的 9 个差集 ＋ `cardState` 三档复核

> 只读查判据代理 `B93_Q1`，2026-10-28。**未改任何源码 / 账 / 正本**。
> 判据源：`d:/2/tools/decomp_full`（第一）/ `out_cpp2il_dev_ilspy`（第二）/ `out_il2csharp`（第三）/ `d:/2/新解包资源/assets_full`（资产侧）/ `dump.cs`（签名与枚举）。
> 本机快照时间限定：**本 build / 2025-07-30 资源快照 / 安卓 1.35.0**。

---

## §0 前置：差集复算（比 B10 多一条结论）

**复算命令照 `报告/B92_B10.md` §6.5 跑，结果一致**：

```text
cases: 82   enum items: 92 nonzero: 91
diff: [(2,'createMinion'), (6,'showEnemyCard'), (8,'doDamage'), (12,'playHeroPower'),
       (24,'replaceHeroPower'), (32,'mulligan'), (33,'returnToDeck'), (40,'growthAnim'), (73,'reanimate')]
```

- `switch` 位置 = `decomp_full/BattleManager__ResolveAction.c:688`（`switch(local_228[0])`）。
- 枚举 = `il2cpp_out/dump.cs:45306`（`BattleActionType // TypeDefIndex: 917`）。

**新增结论（B10 没写）**：**`undefined(0)` 也没有 case**，但它**在 switch 之前就被拦掉**——
`out_il2csharp/BattleManager.cs:5925-5935`：`actionType == 0` ⇒ `CustomDebug.LogWarning("Trying to resolve undefined battleAction")` + 复位 `resolvingActionFlag` + `return`。
⇒ 「缺 case」的实数是 **10 型（含 0）**，B10 的「9」是**非 0** 的口径，两个数都对，**引用时要说清口径**。

**这 9 型真被塞进队列时会怎样**（`BattleManager__ResolveAction.c:749-756`）：
`default:` ⇒ `Enum.ToString()` ⇒ **`CustomDebug.LogError("Trying to resolve battleAction with unknown type " + 类型名)`** ⇒ `return`。
（同一句在 ILSpy `out_cpp2il_dev_ilspy/BattleManager.cs:22446-22451`。）
⇒ **不是静默忽略，是 LogError 出声**；`待接线与排队项.md:348` 那句「入队也无人处理」需按此改写法。

---

## §1 生产者普查（先定「有没有人产」）

四条独立口径，全指向**同一结论：这 9 型在本 build 里没有任何生产者**。

| 口径 | 做法 | 结果 |
|---|---|---|
| ① `decomp_full` 全树（25,096 个 `.c`） | 正则 `BattleAction___ctor(<_, 类型>` 抽第二实参 | **75 处 / 65 个不同类型**；9 型各 **0** 次（0 型也 0） |
| ② `out_cpp2il_dev_ilspy` 全树 | `BattleActionType.<名>` 与 `new BattleAction(...)` | `BattleActionType.` 共 109 处，**这 9 个名字只出现在 `BattleActionType.cs` 的枚举定义里**；75 处 `new BattleAction(` 用的是另外 64 个名字 |
| ③ `out_il2csharp` | `(BattleActionType)N` 数值强转 | 2/6/8/12/24/32/33/40/73 **各 0** |
| ④ `assets_full` | `ReplaceHeroPower` / `HeroPowerState` / `playHeroPower` / `growthAnim` | **0 文件命中**（UnityEvent `m_MethodName` / AssetReference 全无） |

**联机入口实测**（这是唯一还能把任意类型喂进来的一条路）：
`BattleManager.ParseEnemyAction`（`out_il2csharp/BattleManager.cs:2338`）第 3 句就是
`__ret.actionType = playerAction.typ;`（`:2349`，**网络字节原样拷进 BattleAction**），但紧接着的 if-链
**只接受 7 种**：`1 attack` / `5 playCardFromHand` / `14 endTurn` / `30 playActiveAbility` / `54 resign` / `55 chatMessage` / `76 useWaystone`
（`:2351-2512`）。**其余 84 型一律**落 `L_1809704ed`：

```csharp
// out_il2csharp/BattleManager.cs:2517-2534（同构 C：BattleManager__ParsePlayerAction.c）
CustomDebug.LogError("Trying to cancel match with victory: ParseError: " + typeof(BattleActionType));
... matchData.desyncCancelledGame = true; alreadyCancelledMatch = true;
... PlayerDataManager.RecordMatchWonByCancel(...);   // ← 记我方胜
__ret.actionType = BattleActionType.undefined;
```

⇒ **原版把「对手发来没见过的动作类型」当作弊/掉同步处理：判我方胜 + 取消对局**，不是"忽略"。
⇒ 这 9 型**既没有本地生产者，也进不了正常入队路径**。

**普查的边界（必读）**：① 的 75 处是**显式调用点**；构造函数可能被**内联**进调用方（ILSpy 会把内联后的 ctor 显示成 `Unsafe.Write(..., new BattleAction(...))`，仍会显形，所以 ② 是补这一刀）——但若某个方法**两棵树都没解出体**（全量 724 条无方法体那一档），理论上仍可能藏着生产者。**要 100% 闭合需 VA 级反汇编扫 `BattleAction___ctor` 的全部调用点**，本次没做（见 §5）。

---

## §2 差集 9 条逐条

| # | 枚举 | 原版真实处理路径 | 我们现状 | 结论 |
|---|---|---|---|---|
| 2 | `createMinion` | 就是 **`summonUnit(20)`** | `RuleCore.DeployFree` | 原版无此型，**不用补** |
| 6 | `showEnemyCard` | **联机消息** `PUNReceiveCardSeen` → `ShowEnemyCardSeen` → `CardScript.ShowEnemyCardInHand()` 协程 | `BattleDriver.NetRemoteCardSeen` | 原版无此型，**不用补** |
| 8 | `doDamage` | 就是 **`doMultiDamage(9)`**（＋`doMultiDamageAndDestroySelf(75)`、`attack(1)` 的战斗伤害） | `RuleCore.ApplyDamage` | 原版无此型，**不用补** |
| 12 | `playHeroPower` | **死档**：整套 HeroPower 机制在本 build 只剩枚举 | 无（`TutorialScript` 只是枚举镜像） | **不用补**，原版也无人处理 |
| 24 | `replaceHeroPower` | **死档**：`ReplaceHeroPower` 0 调用点 | 无 | **不用补**（与账里 D 档一致） |
| 32 | `mulligan` | **专门流程**（联机消息 → `FinishMulliganFinalPhase` → `PlayerHand.ReplaceMulliganCard` → `ReturnToDeckAnim`） | `RuleCore.Mulligan` + `NetBattle` | 原版无此型，**不用补** |
| 33 | `returnToDeck` | 表现 = `CardScript.ReturnToDeckAnim`；引擎 = **`recallToDeck(51)`** | `EffectResolver` 回牌库那一支 | 原版无此型，**不用补** |
| 40 | `growthAnim` | **死档**：`AbilityTrigger.Growth=100` 全树 0 引用 | 无 | **不用补**，原版也无人处理 |
| 73 | `reanimate` | 就是 **`transformToRemnant(72)` 的 from-remnant 支** | `EffectResolver.DoReanimate` | 原版无此型，**不用补** |

### 2·1 `2 createMinion` → 真实路径 = `summonUnit(20)`

- **原版没有任何叫 `CreateMinion` 的方法**（`dump.cs` 全表 0 命中），单位创建全部走召唤：
  `BattleManager__AddSummonUnit.c:46` `BattleAction___ctor(&local_218, 0x14 /*20 summonUnit*/, …)` → `:95-111` 入队
  → `ResolveAction.c:2178` 的 `case 0x14`（状态机句柄行 `:2183`，句柄 `DAT_18426dd40` ＝ `BattleManager__ResolveSummonUnit.c` 的那个，
  **逐字交叉验证过**）→ 状态机真体 `BattleManager._ResolveSummonUnit_d__510__MoveNext.c`
  `:87 BattleCardManager__CreateCard` · `:101 CardScript__SetAsUnitInPlay` · `:135/:274 MinionManager__SummonMinion`。
- **我们现状**：`RuleEngine/Core/RuleCore.cs:4759`（`DeployFree`，出队 `summonUnit` → `SummonMinion`，不付费）· `:4980` 重载。
- **要改什么**：**无**。原版没这个型，我们也不需要为它造语义。

### 2·2 `6 showEnemyCard` → 真实路径 = 联机消息 → `ShowEnemyCardInHand()` 协程

- **原版**：`out_cpp2il_dev_ilspy/BattleCommsManager.cs:1359-1370` `PUNReceiveCardSeen(uniqueInGameID, mi)`
  （先校验 `sender == matchData.opponentId`）→ `BattleManager.ShowEnemyCardSeen`（`BattleManager.cs:18283-18298`，
  按 `uniqueInGameID` 扫敌手手牌，命中才 `StartCoroutine(cardScript.ShowEnemyCardInHand())`）
  → `CardScript.ShowEnemyCardInHand()`（`CardScript.cs:10668`，kickoff；真体在 `CardScript._ShowEnemyCardInHand_d__286__MoveNext.c`）。
- **我们现状**：`CardPresentation/Battle/BattleDriver.cs:2139` `NetRemoteCardSeen(int uniqueInGameId)`（账 `CardScript.ShowEnemyCardInHand` 行 = A）：
  找不到实例**出声**，找到就 `_cardDisplay.Show(...)`。
- **要改什么**：**无**。

### 2·3 `8 doDamage` → 真实路径 = `doMultiDamage(9)`

- **原版**：伤害只有三条入口
  ① `BattleManager.DealMultiDamage` → `BattleManager__DealMultiDamage.c:40` `BattleAction___ctor(…, 9, …)`（`DealMultiDamageVariable.c:35` 同）
  ② `DealMultiDamageAndDestroySelf` → 型 **75**
  ③ 攻击/反击的战斗伤害（`ResolveAttackDamage`，含在 `attack(1)` 那条 action 里）
  → `ResolveAction.c:994` 的 `case 9`（句柄行 `:999`，`DAT_18426d170` ＝ `BattleManager__ResolveMultiDamage.c`，**交叉验证过**）。
  另：`AddDamageStat`（`BattleManager.cs:24526`）**只写战报统计，不建动作**。
- **我们现状**：`RuleEngine/Core/RuleCore.cs:8453` `ApplyDamage` / `:8464` `ApplyDamageCore`；攻击伤害在 `DeclareAttackExecute`。
- **要改什么**：**无**。

### 2·4 `12 playHeroPower` / **2·5 `24 replaceHeroPower`** → 两档都是死档

四源全扫，HeroPower 这套机制在本 build 只剩**枚举空壳**：

| 符号 | 出现地 | 引用 |
|---|---|---|
| `BattleActionType.playHeroPower = 12` | `BattleActionType.cs:17` | 仅定义 |
| `BattleActionType.replaceHeroPower = 24` | `BattleActionType.cs:29` | 仅定义 |
| `PlayerActions.useHeroPower = 3` | `PlayerActions.cs:6` | **仅定义**（`GetAvailableActions` 里没有这一支；我们 `SimpleAI.cs:49` 的注释也写了「原版从不产出」） |
| `HeroPowerStateOptions`（inactive/active/used/waitingToResolve） | `HeroPowerStateOptions.cs:1` | **全树无人引用** |
| `BattleManager.ReplaceHeroPower(RawCardScript, bool, int)` | `out_cpp2il_dev_ilspy/BattleManager.cs:39644` · `out_il2csharp/BattleManager.cs:18631` | **0 调用点**（三棵树一致） |
| `assets_full` 字符串引用 | — | **0 文件命中**（本次现扫） |

- **我们现状**：**无**（`RuleEngine/Core/TutorialScript.cs:110/:122` 只是我们抄的 `BattleActionType` 镜像枚举，用来对照原版闸门，**不作入队**）。
- **要改什么**：**不用补**——这不是"我们没做"，是**原版本身就没有可用实现**（铁律 11 例外①：判据在原版侧为空）。
  账里 `ReplaceHeroPower` 已判 **D**（B91_L4），本次三源复核**与它一致**。

### 2·6 `32 mulligan` → 真实路径 = 专门换牌流程（不入动作队列）

- **原版**：`BattleCommsManager` 一族消息（`SendMulliganCards` / `PUNReceiveEnemyMulligan` / `PUNReceiveMulliganAck` / `RequestMulliganResend`）
  → `BattleManager.ReceiveMulliganAck` / `ReceiveEnemyMulliganAndEnvEffect`（账 A）
  → `BattleManager.FinishMulliganFinalPhase`（`BattleManager.cs:18651` kickoff，真体 `_FinishMulliganFinalPhase_d__351__MoveNext.c`；`:25853-25876` 是两阶段收口）
  → `PlayerHand.ReplaceMulliganCard`（`PlayerHand.cs:2223`）/ `CompleteMulliganPhase`（`:2264`）
  → `CardScript.ReturnToDeckAnim(globalVars.timeToDrawEnemyCard)`（`PlayerHand.cs:1177` 起协程）。
  **全程没有 `BattleAction` 入队**。
- **我们现状**：`RuleCore.Mulligan`（`RuleEngine/Core/RuleCore.cs:444`，含 `MulliganCollectIndices` 整批校验）＋ `NetBattle.OnLocalMulligan/HostResolveMulligan`（账 A/B）。
- **要改什么**：**无**（`mulligan(32)` 这个型不用补）。

### 2·7 `33 returnToDeck` → 真实路径 = 表现动画 + `recallToDeck(51)`

- **表现层**：`CardScript.ReturnToDeckAnim(float timeToMove)`（`CardScript.cs:9005` kickoff，真体 `_ReturnToDeckAnim_d__248__MoveNext.c`）——
  **全树唯一调用点** = `PlayerHand.cs:1177`（换牌替换那一支）。
- **引擎层**：「回牌库」这条动作是 **`recallToDeck(51)`**：`BattleManager__AddRecallToDeck.c:38` `BattleAction___ctor(…, 0x33 /*51*/, …)`
  → `ResolveAction.c:4341` 的 `case 51`（句柄行 `:4346`，`DAT_18426d658` ＝ `BattleManager__ResolveRecallToDeck.c`，**交叉验证过**）
  → 真体 `_ResolveRecallToDeck_d__543__MoveNext.c`（`:108/:177/:276` 三处 `ResetToValuesInHand(…,1,…)`）。
- **我们现状**：回牌库在 `RuleEngine/Core/EffectResolver.cs:6737-6742` 那一支（跟 `_ResolveRecallToDeck_d__543__MoveNext.c` 对齐，2026-10-28 `B92_B4c` 已按 `:108/:177/:276` 补了当场复位）；账 `AddRecallToDeck` = A。
- **要改什么**：**无**。（`BattleDriver` 侧那条"飞回牌库"的**表现动画**本次没逐行核，见 §5。）

### 2·8 `40 growthAnim` → 死档

- `AbilityTrigger.Growth = 100`（`AbilityTrigger.cs:20`）—— **全树（decomp_full / ILSpy / il2csharp）0 引用**；
  `growthAnim` 只出现在枚举定义里。
- 卡表里 4 处 "Growth" 是**动画 clip 名**（`"Hive Fleet Ground Growth_warlord"` / `animTimes[].n = "Hive Fleet Ground Growth"`，Talos/泰伦督军那条），
  **不是** `AbilityTrigger.Growth`，走的是普通 `animTimes` 管线。
- **我们现状**：无。
- **要改什么**：**不用补**（铁律 11 例外①：原版无写点、无读者、无生产者）。

### 2·9 `73 reanimate` → 真实路径 = `transformToRemnant(72)` 的 **from-remnant 支**

这是 9 条里最容易误判的一条（「有个叫 reanimate 的型，那复活一定走它」——**不是**）：

```text
AbilityEffect.reanimate = 430            (AbilityEffect.cs:75)
 → AbilityLogic.PlayAbility 的 reanimate 支
   out_il2csharp/AbilityLogic.cs:2858  BattleManager.AddTransformFromRemnant(manager, card, waitToTransform, …)
 → BattleManager__AddTransformFromRemnant.c:38   BattleAction___ctor(…, 0x48 /*72 transformToRemnant*/, …)
 → ResolveAction.c:6370  case 0x48   ← 句柄行 :6426，DAT_18426a330 = BattleManager__ResolveTransformFromRemnant.c（交叉验证过）
       if (local_252 == 0) {                       // ← waitToTransform 位 = 方向
           Log("…") ; CardScript__SetAsUnitTransforming(card)      (ResolveAction.c:6482)
           CardScript__TransformFromRemnant(card, callback)
       } else { …ResolveTransformIntoRemnant 协程（d__554）… }
 → onComplete（out_cpp2il_dev_ilspy/BattleManager.cs:30386-30400）
       ResetToValuesInHand → BroadcastUnitReanimated → SetAsUnitInPlay → ResetSummonSickness
       → ActivateTraitsOnSummonOrEnchantment(onSummon:1) → UpdateWhileInPlay(unitJustSummoned:1) → CheckStun
```

- **要点**：`AddTransformFromRemnant` 与 `AddTransformIntoRemnant` **共用 actionType 72**，方向由 `waitToTransform` 这个 bool 决定
  （`BattleManager__AddTransformIntoRemnant.c:35` 也是 `0x48`）⇒ **对账时 ⛔ 别按 actionType 区分"变残骸/复活"**。
- **我们现状**：`RuleEngine/Core/EffectResolver.cs:15506` `DoReanimate`（`"reanimate"` 派发在 `:1144`）
  ＋ `:16627` `ReanimateRemnantInPlace` ＋ 账 `ResolveTransformFromRemnant` = B / `BroadcastUnitReanimated` = A。
- **要改什么**：**无**（行为已复刻；缺的只是 `reanimate(73)` 这个常量本身，而原版这里用的是 72 —— 按 B-10 裁定「actionType 由能力自带」，⛔ 别为了"补上 73"去改入队号）。

---

## §3 `cardState` 三档（3 / 15 / 17）现状复核 —— 两份材料谁对

### 结论一览

| 档 | 原版写点 | 我们有没有接 | 判定 |
|---|---|---|---|
| **3 `inPlayAttacking`** | `CardScript__SetupActiveAbility.c:5` | ✅ **已接**：`RuleCore.UseAbilityCore`（方法起 `RuleCore.cs:11587`）里 `:11663` `MarkCardState(inPlayAttacking)` ＋ `:11665` `finally{ClearCardStateMark();}` | **B2b 对，续推计划过期** |
| **17 `inPlayAminingAttack`** | `CardScript__SetupAttack.c:12` | ✅ **已接**：`RuleCore.DeclareAttackCore`（方法起 `RuleCore.cs:6282`）执行层 `:6603` `MarkCardState(inPlayAminingAttack)` ＋ `:6605` `finally{ClearCardStateMark();}` | **B2b 对，续推计划过期** |
| **15 `inPlayTransforming`** | `CardScript.SetAsUnitTransforming()` 末句（`CardScript.cs:15656`）＋ `ResolveWeaponKilled`（`:15509`） | ❌ **未接**，而且**原版可达** | **B2b 的「零可达」错；续推计划「15 没接」对** |

⇒ **两份材料各对一半**：
- `重同步点后的续推计划.md` §2·1「17 / 3 / 15 的写点还没接」= **对 17/3 已过期**（它 19:53 写，晚于 B2b 的 19:44，但沿用了旧结论），**只对 15 成立**。
- `报告/B92_B2b.md` §2·4「17 与 3 已经接了」= **对**；「15 是零可达」= **错**（下面给反证）。

### 3·1 15 可达的**三条**链（B2b 只查了 `ResolveWeaponKilled` 一条）

`inPlayTransforming(15)` 的**唯一通用写口**是 `CardScript.SetAsUnitTransforming()`，其末句
`cardState__BackingField = CardStateOptions.inPlayTransforming;`（`out_cpp2il_dev_ilspy/CardScript.cs:15656`，方法起 `:15606`）。
它有三个调用者 —— **三个都对应真实动作类型**：

| 调用者 | 位置 | 触发的动作 | 生产链 |
|---|---|---|---|
| `ResolveTransformUnit` | `BattleManager.cs:29086` | **`transformUnit(27)`** | `ResolveAction.c:2931` `case 27` → `BattleManager__ResolveTransformUnit(…)` |
| `ResolveTransformIntoRemnant` | `BattleManager.cs:12097` | **`transformToRemnant(72)` 的 into 支** | `CardScript.CheckIfDead`（`CardScript.cs:15183`，调用点 `:15297`）→ `AddTransformIntoRemnant` → 型 72 |
| `ResolveAction` case 0x48 **from-remnant 支（内联）** | `decomp_full/BattleManager__ResolveAction.c:6482` | **`transformToRemnant(72)` 的 from 支（= 复活）** | `AbilityEffect.reanimate(430)` → `AbilityLogic.cs:2858` → `AddTransformFromRemnant` → 型 72 |

第 4 个写点 `ResolveWeaponKilled`（`CardScript.cs:15509`）确实是死的（Weapons 卡种本 build 不存在，B2b 已证）——
**但 B2b 的错因是把"4 个写点里死了 1 个"当成了"这档不可达"**：它漏了另外三条，其中两条（死亡翻面 / 复活）在**本 build 是常见事件**
—— 卡池侧实证：`cards_engine.json` 里 `Remnant` **2,418 处** / `reanimate` **7 处**（残骸翻面与复活那一族卡确实在池内）。

**我们侧同样可达**：这些行为我们都做了 —— `EffectResolver.DoReanimate`（`EffectResolver.cs:15506`）、
`ReanimateRemnantInPlace`（`:16627`）、死亡翻面 `RuleCore.CleanupDeaths` 那一支（`RuleCore.cs:9863` 附近 `IsRemnant = true`）。
⇒ **15 不是"我们没做也不必做"，是"原版会置、我们没置"**。

### 3·2 15 的观测面（为什么它今天的可观察差异 ≈ 0，但仍要接）

- 15 与 2 在下列读点里**同真**：`IsInPlay()`（`CardScript.cs:20755-20770`）、`IsInPlayOrDying()`、`ActivateTraitsOnSummonOrEnchantment`（`:11741-11766` 三档放行）。
- **唯一**把 15 单独拎出来判的读点是 `SetAsUnitTransforming` 自己的幂等闸（`CardScript.cs:15617`）。
- 我们 `CardStateOf` 的消费方只有 `IsInPlayState*` 两个（`UnitStateCardState.cs:197/:200`）⇒ 接上 15 **不改任何一张卡的结算读数**（与 B2b 对 3/17 的判断同构）。
- **但按铁律 11「全部都要做、只有先后」**：这是真缺口，只是**排后面**，⛔ 不能判"不做"。

### 3·3 15 的施工落点（给执行代理）

| 写点 | 落点（现读行号，改动前请复核 —— `RuleCore.cs`/`EffectResolver.cs` 另有写手） | 复位 |
|---|---|---|
| 复活（from-remnant） | `EffectResolver.DoReanimate`（`EffectResolver.cs:15506`）里 `ReanimateRemnantInPlace` 前后那一段（`:15540-15560`）——用 `try/finally` 包住 `back = ReanimateRemnantInPlace(rem)` 及其后续（含 `Auras.Recompose`），`finally { rem.ClearCardStateMark(); }`（注意：翻回来换的是**同一个 `CardInstance` 的新 `UnitState`**，标记要打在**新的那个**上，或按原版打在"翻的过程中那个对象"上并保证 `SetAsUnitInPlay` 之后是 2） | 翻完后 `ClearCardStateMark()`（＝原版 `SetAsUnitInPlay` 置 2） |
| 变残骸（into-remnant） | `RuleCore.CleanupDeaths` 残骸那一段（`RuleCore.cs:9863` 附近 `IsRemnant = true` 的新建 `rem`） | 同上 |
| （可选，同族）`transformUnit(27)` | 我们 `EffectResolver.DoBecome`（`EffectResolver.cs:3491`）是**手牌战略卡变身**，不是在场单位变身 ⇒ **这里没有真实落点**（与 `EffectResolverPartial_B88E2.cs:521` 的说明一致）；在场单位变身（W8）尚未开 ⇒ 记"等 W8" | — |

---

## §4 对账建议（⛔ 我没动账，交调度台合并）

1. **`战斗逻辑驱动.tsv` 的 `ResolveAction` 行（状态 B）**：`差异` 列建议补一句
   「9 个非 0 差集（`2/6/8/12/24/32/33/40/73`）**均无生产者**（四源普查）且**无 case**；入队则 `default:` ⇒
   `LogError("Trying to resolve battleAction with unknown type …")`；其中 2/6/8/32/33/73 的真实路径分别是
   `summonUnit(20)` / 联机 `PUNReceiveCardSeen` / `doMultiDamage(9)` / 专用换牌流程 / `recallToDeck(51)` / `transformToRemnant(72)` 的 from 支；
   12/24/40 为原版死档。判据 → `判据/B93_Q1_actionType9与cardState.md`」
2. **`cardState` 相关的三行**（若账里有 `cardState` 落点行）：17/3 已接，需从"未接"改为 A；
   **15 保持缺口**，落点写 §3·3 那两处。
3. **`报告/B92_B2b.md` §2·4** 与 **§四·1** 的「15 零可达」建议就地订正（铁律 5：保留更正痕迹）：
   「⚠️ 2026-10-28 `B93_Q1` 更正：原写"本 build 零可达"，实际 `SetAsUnitTransforming` 有 **3 个活调用者**
   （case 27 / case 0x48 into 支 / case 0x48 from 支），其中 from 支由 `AbilityEffect.reanimate(430)` 驱动 ⇒ **15 可达**；
   只有 `ResolveWeaponKilled` 那一条是死的。」
4. **`重同步点后的续推计划.md` §2·1** 建议订正为：「17 / 3 已由 `B92_B2b` 接（`RuleCore.cs:6603` / `:11663`）；
   **只剩 15**，且 15 原版可达（见 `判据/B93_Q1_…` §3）」。

---

## §5 没查清的部分（如实标）

1. **内联构造的 producer 未穷尽**：`decomp_full` 只露 75 处显式 `BattleAction___ctor` 调用；构造函数被内联后**第二实参可能变成裸常量**，
   我的正则抓不到。**还差**：VA 级反汇编扫 `BattleAction` 构造（IL `newobj`/`stfld actionType`）的全部调用点。
   缓解证据：ILSpy 树里 `new BattleAction(...)` 75 处 + `(BattleActionType)N` 强转 0 命中 ⇒ 概率很低。
2. **我们侧回牌库的"表现动画"**（`returnToDeck(33)` 的视觉那半边）没逐行核：`CardPresentation/Battle/BattleDriver` 里
   "卡飞回牌库"那一段与 `CardScript.ReturnToDeckAnim` 的对应关系**未确认**。
3. **`ParseEnemyAction` 的 desync→判我方胜 那条原版行为，我们 `NetBattle` 有没有等价闸未核**（见 §6·3）。
4. **`out_cpp2il_dev_ilspy` 有若干 method body 缺失**（全量 724 条无方法体那一档）；若生产者恰好落在里面，§1 的普查会漏。

---

## §6 顺手发现（⛔ 我没改任何东西，交调度台分流）

1. 🔴 **`B92_战斗判据_B1.md:104` 的「缺的 15 型」清单与 `.c` 直接冲突**。
   它写「缺的 15 型（`0/1–0x0a/0x0c/0x18/0x20/0x21/0x28/0x49`）由别的入口处理」，但现读 `ResolveAction.c` 的 case 表：
   **1 / 3 / 4 / 5 / 7 / 9 / 10 / 11 全都在 switch 里**（`case 1` 与 `0x34/0x35/0x4a` 共用 fall-through 体；
   行号：`case 1:689` · `3:757` · `4:816` · `5:875` · `7:934` · `9:994` · `10:1053` · `11:1109`）。
   而它自己那段枚举名单还**自相矛盾**：`0x18/0x20/0x21`（24/32/33）落在它上句写的「`0x0d–0x27` 已有 case」区间里。
   交叉验证：`case 3` 的状态机句柄 `DAT_18426ca88` **就是** `BattleManager__ResolveDrawCard.c` 用的那一个（逐字相同）。
   真缺口 = **9 个非 0 + `undefined(0)`**（0 由 switch 前的闸处理）。
   ⇒ 那条施工单若被照抄，会有人去"补 case 3/9/20"——**那是重复实现**。建议按本条订正。
   **实测量化**：**附录 A 只有 75 型**（表里第一列 = 型号），未覆盖 16 型
   `{1,2,3,4,5,6,7,8,9,10,12,24,32,33,40,73}` —— 其中 **1/3/4/5/7/9/10 这 7 型 `.c` 里【真有 case】**
   （只有 2/6/8/12/24/32/33/40/73 这 9 型才真没有）⇒ **附录 A 缺 7 行，施工单要补**（补齐后 75+7=82 行，与 `.c` 的 82 个标签一致）。
   **同一句错话还复制在 7 处**（铁律 5 要求一起改，⛔ 我只改了 `B92_战斗判据_B1.md` 这一份）：
   `判据/B92_战斗判据_B1.tsv` · `判据/_gen_b1.py:194/:410` · `_gen_b1b.py:65` · `_gen_b1c.py:33` · `_gen_b1d.py:45` · `报告/B92_B1.md` ·
   `账/_parts/_merged_B92/B92_B1{,b,c}.tsv`（**最后这三个是归档补丁，⛔ 别改**）。
   ⚠️ **`_gen_b1*.py` 是那几份 TSV 的生成器** ⇒ 只改 `.md` / 手改 TSV，重跑生成器会把旧话写回去；要彻底修得改生成器里的字符串。
2. **`待接线与排队项.md:348`**「枚举里有、分发里没有 = 入队也无人处理」需改：实为 `default:` ⇒ **LogError 出声**，不是静默；
   且本地**入不了队**、联机入口会把它转成 `undefined` 并**判胜/取消对局**。
3. 🔴 **原版有一条"防作弊/掉同步"闸，值得单独立账**：`ParseEnemyAction` 只接受 `{1,5,14,30,54,55,76}`，
   其余一律 `desyncCancelledGame = true` + `RecordMatchWonByCancel(…)` + `SendDesync()`（`out_il2csharp/BattleManager.cs:2517-2534`）。
   ⇒ **"对手发来本 build 不认识的动作类型"在原版里 = 我方判胜 + 取消对局**。
   我们 P2P 是不是也这么处理**未核**（建议派一条只读账到联机线）。
4. **`AddTransformFromRemnant` / `AddTransformIntoRemnant` 共用 actionType 72**，方向靠 `waitToTransform` 位
   （`BattleManager__AddTransformFromRemnant.c:38` 与 `…IntoRemnant.c:35` 都是 `0x48`）。
   ⛔ 以后对账别按 actionType 反推"这是变残骸还是复活"。
5. **`AbilityTrigger.Growth = 100` 是一个 0 引用的枚举成员**（三棵树全 0），而卡数据里 4 处 "Growth" 是
   **动画 clip 名**（`Hive Fleet Ground Growth`）。别把两者混成一个东西。
6. **`ResolveAction` 的 `default:` 用的日志文案是 `"Trying to resolve battleAction with unknown type " + 类型名`**
   （`out_cpp2il_dev_ilspy/BattleManager.cs:22446-22451`）。我们 `ActionPump` 那边如果要仿，用同一句便于对日志。

---

## §7 施工单：`cardState = 15` 的两个写点（可直接派写手 · 2026-10-28 追加）

> 派工前置（⛔ 三条硬约束）：
> ① **零行数变化** —— `RuleCore.cs` 有 365 处账落点、`EffectResolver.cs` 同类 ⇒ `try {` / `} finally { … }` 的括号**必须并进已有行**（这是 `RuleCore.cs:9858` 那行注释自己写的纪律）。改完立刻跑
> `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py`（纯 python，不跑 Unity）确认 **0 漂移**，再跑 `bash 工具/typecheck.sh`。
> ② **只加写点，⛔ 不动任何读点** —— 特别是 ⛔ 别改 `UnitState.IsInPlayState` / `IsInPlayOrDyingState` / `IsInPlayStateOf`（把 15 排除掉 = 错映射，会立刻改变一大批卡）。
> ③ **只标「原版真有这条路径」的那两支** —— 复活效果的**墓地退路**（`DoReanimate` 候选 ②）是**我们自己的旧口径**，原版没有这条，⛔ 不标 15（要标就按铁律 18 打 `🔶 自己设计`）。

### 7·1 写点 A · 变残骸（原版 `case 0x48` 的 **into 支** → `ResolveTransformIntoRemnant`）

**原版链路（现读）**：`CardScript.CheckIfDead`（`out_cpp2il_dev_ilspy/CardScript.cs:15183`，调用点 `:15297`）
→ `BattleManager.AddTransformIntoRemnant`（型 **72**，`BattleManager__AddTransformIntoRemnant.c:35`）
→ `ResolveAction.c:6370` 的 `case 0x48` **else 支** → `ResolveTransformIntoRemnant`（协程 `d__554`）
→ 其内 `CardScript.SetAsUnitTransforming()`（`BattleManager.cs:12097`）→ **`cardState(+0x228) = 15`**（`CardScript.cs:15656`）
→ 残骸定形后由 `SetAsUnitInPlay` 一类回 **2**。

**我们落点（现读）**：`RuleEngine/Core/RuleCore.cs:9857`
`if ((u.Has(KeywordTable.Remnant) || u.Has(KeywordTable.Waystone)) && !u.IsRemnant)`，体从 `:9858` 起（首句 `BroadcastRemoveFromGame(ctx, u, null);`，`:9862-9863` 新建 `rem`）。

**伪代码（括号并进已有行，行数不变）**：

```csharp
// :9857 —— 15 档窗口 = 原版 ResolveTransformIntoRemnant 里那次 SetAsUnitTransforming（case 0x48 into 支）
if ((u.Has(KeywordTable.Remnant) || u.Has(KeywordTable.Waystone)) && !u.IsRemnant)
{   u.MarkCardState(CardStateOptions.inPlayTransforming); try {          // ← 全部并进 :9858 那一行（该行现在是 `{ BroadcastRemoveFromGame(...); ...`）
        … 现有分支体一字不动（建 rem / 入格位 / 记账）…
    } finally { u.ClearCardStateMark(); } }                              // ← 并进该 if 分支原本的收尾 `}` 那一行（现读要执行代理自己定位，别按行号硬改）
```

> 备选（更贴本仓惯用写法，同样零行数变化）：把 `{` 那行改成 `using (UnitState.CardStateScope(u, CardStateOptions.inPlayTransforming)) {`，
> 作用域类型（`struct` + `IDisposable`，`Dispose` 里 `ClearCardStateMark()`）**追加到 `RuleEngine/Core/UnitStateCardState.cs` 类尾**
> （该文件是 2026-10-28 新建、账落点少，追加类尾安全；与 `using (BeginAction(ctx))` 同一套写法）。
> ⚠️ 二选一，**别两种都做**。

### 7·2 写点 B · 复活（原版 `case 0x48` 的 **from 支** → `ResolveTransformFromRemnant`）

**原版链路（现读）**：`AbilityEffect.reanimate(430)` → `AbilityLogic.PlayAbility`（`out_il2csharp/AbilityLogic.cs:2858`）
→ `AddTransformFromRemnant`（型 **72**，`…AddTransformFromRemnant.c:38`）→ `ResolveAction.c:6370 case 0x48` **from 支**
→ `CardScript__SetAsUnitTransforming`（`ResolveAction.c:6482`，**内联**）→ `CardScript.TransformFromRemnant(onComplete)`
→ onComplete（`out_cpp2il_dev_ilspy/BattleManager.cs:30386-30400`）：`ResetToValuesInHand` → `BroadcastUnitReanimated`
→ **`SetAsUnitInPlay`（= 回 2）** → `ResetSummonSickness` → `ActivateTraitsOnSummonOrEnchantment` → `UpdateWhileInPlay` → `CheckStun`。

**我们落点（现读）**：`RuleEngine/Core/EffectResolver.cs` `DoReanimate`（`:15506`）的**候选 ①（场上残骸）**那一支：
`:15543 var back = ReanimateRemnantInPlace(rem);` … `Auras.Recompose` … `ApplyDeployTurnState` … `CheckStun` … `ctx.Emit(EvtKind.Deploy, …)` … `BroadcastWhen(…Reanimated…)` … `ctx.Tally.AddReanimated(owner);` → **`:15600 附近 got++;`**。

**伪代码（括号并进已有行，行数不变）**：

```csharp
int slot = s;                                                       // :15541（原样）
using (UnitState.CardStateScope(rem, CardStateOptions.inPlayTransforming)) {   // ← 并进 :15543 那一行开头（原版窗口 = SetAsUnitTransforming ⇒ SetAsUnitInPlay）
    var back = ReanimateRemnantInPlace(rem);                        // 原样
    … 现有 15544-15599 一字不动（DeployedTurn / PlayOrder / Auras / ApplyDeployTurnState / CheckStun /
      Emit(Deploy) / BroadcastWhen(Reanimated) / Tally）…
    got++;                                                          // :15600（原样）
}                                                                   // ← 并进该循环体原本的收尾 `}` 行
```

> ⚠️ **窗口的终点照原版放在 `BroadcastWhen(Reanimated)` / `got++` 之后**（那一段对应原版 onComplete 的
> `BroadcastUnitReanimated` → `SetAsUnitInPlay`）；⛔ 别把 `got++` 留在 `finally` 之外——它已经在外层循环里，按上面写法不动即可。
> ⚠️ `ReanimateRemnantInPlace` 是**原地**改同一个对象（`rem` 翻回来还是它）⇒ 标在 `rem` 上、用 `rem.ClearCardStateMark()` 复位都对；
> 若执行代理发现有 `rem` 被换对象的分支，改成标/清 `back`。

### 7·3 断言（三条一组，⛔ 不许只断谓词）

**宿主**：`RuleEngineTest.Run`（`Assets/RuleEngine/Editor/RuleEngineTest.cs`，约 3m53s）。
理由：两处写点都在**纯引擎层**（`RuleCore.CleanupDeaths` / `EffectResolver.DoReanimate`），不依赖 `BattleDriver`。
（若还想盖表现层再挂 `BattleScene.Run`，多付 7m56s —— 非必需。）

**现读现状**：`RuleEngineTest.cs` 与 `BattleScene.cs` 里对 `MarkCardState` / `CardStateOf` **一条断言都没有**（grep 只命中注释）
⇒ 3 / 15 / 17 三档**今天全靠人读代码**。这一批建议一次补齐三条。

| # | 断什么 | 怎么写 | 为什么这样写 |
|---|---|---|---|
| 1 | **前提（硬）**：夹具真的走到该支 | 变残骸支：`Check(棋盘上出现了 IsRemnant==true 的新 UnitState)` ＋`Check(原来的 u 已不在 Board)`；复活支：`Check(got==1)` 或 `Check(ps.Board[slot].IsRemnant == false)` | 铁律：`if (前提) Check(结论)` 会让夹具坏掉时**静默跳过**（本项目撞过 3 次）。前提必须是独立一条 `Check` |
| 2 | **窗口内枚举值 == 15** | 复活支：在**窗口内既有的回调点**读 —— `BroadcastWhen(…Reanimated…)`（`EffectResolver.cs:15605` 附近，仍在作用域内）挂一次性 hook，`Check(u.CardStateOf(ctx) == CardStateOptions.inPlayTransforming)`；变残骸支：`BroadcastRemoveFromGame` 之后没有现成回调 ⇒ 加一条**仅测试可见**的探针（`ctx.CardStateProbe?.Invoke(u)`，生产路径恒 null、零影响），在该探针里断 15 | **必须断枚举值本身** —— 断 `IsInPlayState()` 的话 2 和 15 都真 ⇒**恒定绿、等于没断** |
| 3 | **窗口后回 2** | 动作 / `CleanupDeaths` 结束之后，同一张：`Check(CardStateOf(ctx) == CardStateOptions.inPlay)` | 原版窗口的终点是 `SetAsUnitInPlay` 置 2；我们漏掉复位就是**脏标记永久 15**（不报错） |
| 4 | **灭自证（结构上不可能同真）** | ① 断言 2 里**同时**断 `!= CardStateOptions.inPlay`（删掉 `MarkCardState` 那行 ⇒ 必红）；② 照原版幂等闸（`CardScript.cs:15617`：`if (cardState == inPlayTransforming) goto IL_0119`）再补一条：窗口内**再调一次** `MarkCardState(inPlayTransforming)`，`CardStateOf` **仍是 15** | 只断「新写法对」不够：实现和检测器共用 `_cardStateMark` 时，两边一起改回旧写法仍全绿 |

**为什么不会与 `IsInPlayState` 的 2/15 同真撞车**：全仓把 15 与 2 分开判的读点**只有一个** = `SetAsUnitTransforming` 自己的幂等闸
（`CardScript.cs:15617`）；其余（`IsInPlay()` / `IsInPlayOrDying()` / `ActivateTraitsOnSummonOrEnchantment`）里两档同真。
⇒ 断言**只能直接比枚举值**，并**照那条幂等闸**再设计一条「重复标记不改状态」的判别式（上表第 4 行②）。

**顺带**：3 / 17 也照同一形状补 —— 17 的窗口内观测点 = `DeclareAttackCore` 的 `try` 体内（`RuleCore.cs:6604` 那一句的前后），
3 的 = `UseAbilityCore` 的 `try` 体内（`:11664`）；同样「窗口内 == 3/17、窗口后 == 2、删标记则红」。

### 7·4 风险：会不会动 `play check` 指纹？

**理论零变化**，两条现读证据：

- `play check` 的列 = `工具/ruleprobe/PlayProbe.cs:4300` `PfCols` **21 列**（`id/type/warlord/playRc/played/faceOps/selfPlace/mech/other/expectedPay/energyDelta/handCostDelta/dmg/changes/crash/bucket/fixtureFit/injected/changesFull/atkCount/exhausted`）—— **没有 `cardState`**。
- 联机指纹 `NetProtocol.PlayerHash`（`CardPresentation/Net/NetProtocol.cs:627+`）逐格是 `Id/Health/Attack/RangedAttack/Armor/Exhausted/附魔/费用修正/…`，全文件 grep `cardState` **0 命中** ⇒ `StateHash` 也不含它。
- 机制上：`15` 与 `2` 在我们**所有现有读点**里同真（见 §3·2）⇒ 只加写点**不可能**改变任何取值。

**会翻车的写法（⛔ 明令禁止）**：把 `IsInPlayState`/`IsInPlayOrDyingState` 改成排除 15、或让某效果要求 `CardStateOf == inPlay`。

**若仍出现差异，归因候选卡（现读卡池）**：

- **reanimate 族 5 张**：`SAU19` · `SAU20`（Chronomancer：回合末 `reanimate a random friendly Remnant`）· `SAU37` · `SAU40` · `SAU71`（Immortals Phalanx：`When reanimated, …`）。
- **残骸 / 路标石族 53 张**：`keywords` 含 `Remnant` 的 **28** 张（`SAU*`）＋ 含 `Waystone` 的 **25** 张（`ASH*`）；并集 **53**。
  （`RuleCore.cs:9858` 注释写「55 张」= 旧口径；按 `cards_engine.json` 现读是 **53**，差的 2 张可能在 `cardTraits` / 变体上，⛔ 别照抄 55。）

