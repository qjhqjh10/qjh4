# B120-OFF · 离线对战参数溯源（只读审计）

> 子代理 `B120-OFF` · 2026-11-02 · **只读**（未改任何 `.cs` / `.asset` / 账 / 正本，未跑 Unity、未跑 typecheck、未动 git，`d:/2` 只读）。
> 起因 = 用户 2026-11-02 原话：「**原版是有离线对战的，应该是所有相关的代码都有，参数也有，但是需要认真审查，然后梳理出符合原本的设计。**」
> 本文**不重复** `报告/B120_E_引擎数值.md` 的「原版 vs 我们」逐值对照；本文只回答**「这个值原版从哪儿拿到」**，并据此重判「哪些真的只能自定」。
> 判据优先级：`decomp_full/*.c`（首行函数名已核）→ `cpp2il/out_il2csharp`（交叉）→ `out_cpp2il_dev_ilspy`（第三源）→ `il2cpp_out/dump.cs`（字段/属性）→ `assets_full`（**按字段名**搜）→ `assetripper_out/ExportedProject`（`.asset` / `.unity` / `.prefab`）→ 关服前存档。

---

## §〇 一句话结论

**原版没有「不连服务器也能开打」的离线对战** —— 它有一个**模式名叫 `OfflinePractice`（6）/ `OwnDeckTraining`（12）**，指「单人打 AI、不需要对手」，但**参数（`GameplayVariablesData` 那 18 格）仍然由服务器下发**：`InitializeLiveOps` 云脚本 → `HandlerData` → `PracticeEventData.gameplayVariables` → `MatchData.GameplayData`。**本地一个字节都没有**（`assets_full` 24.7 万对象按字段名全 0 命中；Android 1.35.0 的 87 个远端 bundle 里也没有事件数据）。

**但是**：原版**自己在客户端里印着这两套模式的规则文案**（`Event_Description/Classic/Help` · `Event_Description/Skirmish/Help`，**在 1.35.0 安卓客户的 `I2Languages` 里逐字可读**），另有 5 条 `Tips/*` 文案 + 13 场场景里序列化的 `MinionManager.slotsPerSide=4` + `VarsGlobal.deckSize=30`。**这 11 格能落到确定值**（不是猜，也不是我们挑的），加上 AI 牌池 224 副本地 SO —— 「只能自定」从 **19 条 → 7~8 条**。

---

## §一 五个核心假设：逐条证实 / 证伪

### 假设 1 · `PlayEventData` 是 ScriptableObject，且「事件 SO 只由服务器下发」——**两条都成立，但要把「0 命中」的说法换掉**

| 子命题 | 判定 | 证据 |
|---|---|---|
| `PlayEventData : LiveOpsEventData : IdentifiableSO : ScriptableObject` | ✅**成立** | `dump.cs:130221`（`PlayEventData : LiveOpsEventData`）· `dump.cs:126492`（`LiveOpsEventData : IdentifiableSO`）· `decomp_full/IdentifiableSO__.ctor.c` 首行虽被打成 `AddressablesBuildNumber___ctor`，体是 `UnityEngine_ScriptableObject___ctor(param_1,0)` ⇒ 基类是 SO |
| 本地 `assets_full` 按字段名 `gameplayVariables` / `skullsRewards` / `admisionPrices` 搜 = 0 | ✅**复核成立（我自己重跑了一遍）** | `grep -rl "\"gameplayVariables\"" --include=*.json` = 0；`skullsRewards` / `admisionPrices` / `alternativeTraitLogic` / `warlordLifeChange` / `manaAccumulation` / `assignDefensiveCardsInEditMode` / `handLimit` / `drawCardsPerTurn` / `manaPerTurn` / `overtimeTurn` / `shouldReshuffle` / `showMulligan` / `deckSize` / `maxLegendaries` / `numberOfCopies` **全部 0 命中**。⚠️ 这里**字段名搜索是有效否证**——`assets_full` 的 MonoBehaviour JSON **确实带序列化字段名**（抽样 `bundle_menus_assets_all/MonoBehaviour/Booster Pack Legendary Ultramarines.json` 有 `itemDescriptionKey` / `dropTableItems` 等） |
| 换导出目录 / 换 AssetRipper 工程再搜 | ✅**仍然 0** | `assetripper_out/ExportedProject/Assets` 全树 `grep -rl "gameplayVariables"` 只命中 **6 个 `.cs` 脚本**（`PlayEventData.cs` + 5 个 UI 类），**没有一个 `.asset`** |
| 换大小写 / 换关键词 | ✅ 已换：`startingManaSecond` / `startingMana` / `slotsPerSide`。**只有 `slotsPerSide` 命中 26 个文件**，但那是 **`MinionManager` 的同名字段**（见 §二 #4），**不是** `GameplayVariablesData` | `grep -rl "\"slotsPerSide\"" --include=*.json` = 26 个文件，全部落在 `bundle_scenes_scenes_battlearena*`；逐条读 `MonoBehaviour_4372.json` 的键 = `leftSlotPosNormal` / `minionConversionHeight` / `slotsPerSide` / `heroScale` ⇒ 是 `MinionManager` |
| 「事件 SO 只由服务器下发」 | ✅**成立，而且能给出正面的数量证据** | 全库**只有 16 个 `LiveOpsEventData` 实例**（判据 = 有 `activationType` 字段）：`Dark Zealots Parent Event` / `Expansion Orks1/UM1 Parent·Scoring·Premium Item Offer` / `Release Space Wolves·Emperors Children 同三件` / `Premium Campaign Store` / `RewardsMenu` / `Ultramarines Campaign`。**其中没有一个是 `PlayEventData` 的子类**（`Practice` / `Tutorial` / `Ranked` / `FastMode` 一个都没有）⇒ Classic / Skirmish / Practice / Tutorial / Ranked 五种事件的**数据从来没进过任何客户端包** |
| 会不会在远端包里 | ✅**也不在** | Android 1.35.0 的 Addressables 目录（`_tmp_view/apk135/catalog_dump/catalog_bundles.tsv`）**87 个 bundle**，逐条看：卡图 / 卡动画 / 场景 / 菜单 / 图标 / 本地化 / 预组卡组 / shader / 音效 / 视频 —— **没有 liveops 数据包**（只有 `liveopsicons` / `liveopsmenuimages` 两张美术图） |

> **表述订正（铁律 5 口径）**：不要把结论写成「按字段名 0 命中所以没有」。正确表述 = **①字段名搜索（有效）②`LiveOpsEventData` 实例枚举（16 个，无 PlayEventData）③安卓 1.35.0 远端包目录 87 条（无事件数据包）——三条独立证据同向**。

---

### 假设 2 · `ConfigProvider<T>` 的 `config` 可能在 prefab 里带默认值 —— **❌ 证伪（三条都断了）**

| 子命题 | 判定 | 证据 |
|---|---|---|
| `config` 是 `[SerializeField]` | ❌**不是** | `dump.cs:135135` 只写 `protected T config; // 0x0`，**没有 `[SerializeField]`**（同文件里 `AIBotsConfig.BotList` 那类字段**是**带 `[SerializeField]` 的，所以「有没有」是能区分的，不是属性普遍缺失） |
| provider 挂在本地 prefab / 场景上 | ✅**挂上了**（但见下条） | `assetripper_out/ExportedProject/Assets/Data/Scenes/Intro.unity:7356` 里 `AIBotsConfigProvider`（guid `171dec6fc5563ebb980e22a9bd60d08f`）确实是 `!u!114` 组件；同一场景里 131 个脚本组件，含 `ConfigManager` / `LiveOpsManager` / `GameModes` 与 **16 个 `*ConfigProvider`** |
| 该组件序列化了 `config` 载荷 | ❌**空** | `Intro.unity` 那个 `!u!114 &389` 块只有 `m_Script` / `m_Name` / `m_EditorClassIdentifier` —— **一个业务字段都没有** |
| titleData 缺键时是否保留序列化默认值 | ❌**不保留：它先把 config 换成 `new T()`** | `decomp_generic_trial/out_b94/ConfigProvider_object_.UnpackInternal__1682730.c`：开头 `uVar2 = ReflectionHelper__New<object>(…)` → `*(param_1 + 0x20) = uVar2`（**先无条件 new 一个**），然后才 `Dictionary.TryGetValue(key, …)`；命中才 `DeserializeObjectWithType` 覆盖，不命中就 `Debug.Log` 后**直接返回那个 new 出来的空对象**。`get_Config__4c68a0.c` = `return *(param_1+0x20)`（同一格）。`Initialize__16826b0.c` = `param_1[4] = 虚调用(param_2)`（同上） |
| `LiveOpsHandler.handlerCache` 是不是本地默认值 | ❌**不是** | `dump.cs:53379` 起：`events` / `blockedEvents` / `shared` / `handlerActions` / `handlerCache` **全都没有 `[SerializeField]`**；`Intro.unity` 的 `GameModes` 组件（guid `915931d9…`）也是**零业务字段**（`m_Script` / `m_Name` / `m_EditorClassIdentifier`） |

> ⇒ **结论**：`ConfigProvider` 系（`AIBotsConfig` 等）的默认值 **= `new T()` 的字段初始值**，不是 prefab。而 `GameplayVariablesData` **连这条路都没有** —— 它是普通 `[Serializable]` 类、**构造器是空的**（`out_il2csharp/Everguild/LiveOps/GameplayVariablesData.cs:60-62`：`public GameplayVariablesData() : base() {}`；`decomp_full/GameplayVariablesData__.ctor.c` 内容其实是 `<>f__AnonymousType31___ctor`，**文件名与内容不符**，别拿它当否证），**没有字段初始值** ⇒ 服务器 JSON 少一个键，那一格就是 **0**。

---

### 假设 3 · `MatchData` 构造时 `currentEvent == null` 会怎样 —— **没有回退，`GameplayData` 保持 null，第一处使用就 NRE**

`out_il2csharp/Assembly-CSharp/MatchData.cs:131-153`：

```csharp
public MatchData(PlayModes playMode, PlayerBattleData playerBattleData, CardDeck playerDeck,
                 CardDeck enemyDeck, DeckDifficultyLevel botDifficulty = (DeckDifficultyLevel)10,
                 IPlayEvent currentEvent = default)
{
    this.playerStartMana = 2; this.enemyStartMana = 2;
    ...
    this.playMode = playMode;
    if (currentEvent != null) { this.__field_CurrentEvent = currentEvent; }   // ← null 就什么都不做
    ...
}
```

- **构造器从不写 `GameplayData`**（全文件只有三处写：`:111` setter、`:351` 拷贝构造、`:766` `SetMatchRecording`）。
- 唯一的生产者 = `Everguild/MatchMakerManager.cs`：`FindMatch` `:183` 与 `StartMatch` `:260`，都是 `matchData.GameplayData = <事件>.GetBaseData().gameplayVariables;` —— **在构造器之外**。
- 紧接着 `:204` / `:271` / `:290` 就读 `gameplayVariablesData1.alternativeTraitLogic` ⇒ **`gameplayVariables` 为 null 时，`StartMatch` 自己先炸**，根本走不到战斗。
- 第一个地图侧的消费者 `MinionManager.Awake`（`MinionManager.cs:98-107`）**只判 `PlayerDataManager != null`，不判 `matchData` 也不判 `GameplayData`** ⇒ 必然 NRE。
- 拷贝构造（`:281` 起）**会**把 `+0xa0 CurrentEvent` / `+0xa8 GameplayData` 一起拷过去（`:351`），所以回放/重开那条路是继承原局的。
- ⚠️ 顺手发现：`MatchData.playerStartMana`(0x10) / `enemyStartMana`(0x14) 是**只写死字段**（全仓只有 `MatchData.cs:14,15,135,136,285,286` 的声明与赋值，**0 个读点**）。真正的起始能量走 `GameplayData.startingMana(+0x2C)` / `startingManaSecond(+0x30)`。

---

### 假设 4 · 各模式的 `IPlayEvent` 从哪来 —— **只有两个入口：牌组的 `CustomGameModeEvent`，或全局的 `GameModes.CurrentPlayingEvent`**

`Everguild/MatchMakerManager.cs:224-241`（`StartMatch`，Bot / 单机路）：

```
if (playMode == OwnDeckTraining(12))  evt = playerDeck.CustomGameModeEvent;
else if (playMode == OfflinePractice(6)) evt = playerDeck.CustomGameModeEvent;
else evt = LiveOpsManager.GetHandler<GameModes>().CurrentPlayingEvent;
```

`CardDeck.CustomGameModeEvent`（`CardDeck.cs:33-42`）= `GameModes.GetActiveEvent(this.gameMode)`；
`GameModes.GetActiveEvent(PlayModes)`（`GameModes.cs:329-356`）= `GetEvents(false)` 里筛 **`g.EventPlayMode == playMode`（`:586`）且 `g.EventState == 2`（Active，`:543`）**，取 `priority` 最高（`OrderByDescending`）的一条，**全不命中返回 null**。
`GetEvents()` 返回的就是 `LiveOpsHandler.events` —— 由 `LiveOpsManager.BuildHandler` → `LiveOpsEventFactory.CreateEvent(RawEvent.Data, …)` 从**服务器 `HandlerData`** 造出来的（`LiveOpsManager._BuildHandler_d__17__MoveNext.c:178`）。

> ⚠️ 缺口：`GameModes.SetAsCurrentlyPlayingEvent` / `set_CurrentPlayingEvent` 在**全量反编译里找不到调用方**（`il2csharp` / `out_cpp2il_dev_ilspy` / `decomp_full` 三源都只有定义与 `CombatFinished` 里的 `= null`）。⇒ **「谁在进模式窗时把它设进去」这一跳读不到**，只能从 `CurrentPlayingEvent` 的 5 个读点（`BattleNetworkManager.cs:984`、`ChallengeLogMgr.cs:964`、`MatchMakerManager.cs:181/238/256`）反推它确实被设过。

| 模式（`PlayModes`） | `MatchType` | 事件从哪来 | `EventPlayMode` 判据 |
|---|---|---|---|
| 经典 Classic(0) | Ranked(10) | 全局 `CurrentPlayingEvent` | `RankedV2Event.cs:26-31` / `RankedV3Event.cs:24-29` **=`return 0;`** |
| 排位友好 RankedFriendly(11) | Unranked(170) | 全局 | 同 0（同一批 Ranked 事件）⇒ **经典与排位友好共用同一份 gameplayVariables** |
| 练习 OfflinePractice(6) | PracticeOffline(50) | **`playerDeck.CustomGameModeEvent`** | `PracticeEvent.cs:12-16` **=`return 6;`**（硬编码） |
| 自己卡组训练 OwnDeckTraining(12) | PracticeOffline(50) | **`playerDeck.CustomGameModeEvent`**（`CardDeck.gameMode` 为 null ⇒ `GetActiveEvent(null)` 走 default = Classic 0 的筛法，永远不命中 ⇒ 必须能空过）| — |
| 遭遇 / 闪击 Skirmish(13) | FastMode(200) | 全局 | `out_cpp2il_dev_ilspy/FastModeBaseEvent.cs:407` **=`PlayModes.Skirmish`** |
| 教程 Tutorial(4) | Tutorial(100) | 全局（`StartMatch(PlayModes.Tutorial, …)`，`BattleManager.cs:26769` / `GameBootController.cs:63`） | `TutorialEvent.cs:11-16` **=`return 4;`** |
| 轮抽 ClosedDeck(7) | ClosedDeck(110) | 全局 | `DraftEventBase.EventPlayMode` 体未恢复（判据缺） |
| 回放 Replay(10) | Replay(160) | `SetMatchRecording` 里先 `GetActiveEvent(录制的 gameMode)`，**不命中就回落 `GetActiveEvent(default)` = Classic(0)** | `MatchData.cs:752-766` |
| 战役 Campaign(8) | Campaign(130) | 🔴 **不走 `StartMatch`**（全仓 0 个 `PlayModes.Campaign` 调用点），且 `CampaignEventData` **继承 `LiveOpsEventData` 而不是 `PlayEventData`**（`dump.cs:124995`）⇒ **根本没有 `gameplayVariables`** | 这一路 `GameplayData` 由谁填 = **读不到（缺口）** |

---

### 假设 5 · `ScenarioVariables` 与 `GameplayVariablesData` 的 18 格重叠情况 —— **同名只有 1 格，而且那格在原版是死的**

原版 `ScenarioVariables`（`dump.cs:119936-119949`）10 个字段，**本地三份资产全在且带真值**：

| 文件 | startingMana | startingHand | secondExtraCards | maxMana | maxCardsInHand | QPT | QPTAlt | clockTimeLimit | reduced | countdown |
|---|---|---|---|---|---|---|---|---|---|---|
| **`DefaultScenario.json`**（`bundle_duplicateassetisolationso_assets_all`，另有 `assetripper…/Assets/DefaultScenario.asset`） | **1** | **3** | **1** | 10 | 10 | **3** | **4** | **60** | **10** | **15** |
| `TutorialScenario.json` | 1 | 3 | **0** | 10 | 10 | 3 | 4 | **1000** | **1000** | **10** |
| `EditorScenario.json` | 1 | 3 | 1 | 10 | 10 | 3 | 4 | **150** | **30** | **15** |

⚠️ **不要拿 `ScenarioVariables..ctor` 当值**（`ScenarioVariables__.ctor.c` 写的是 3/5/1/10/10/3/4/60/30/10）—— 序列化资产**覆盖**了它。这与 `MinionManager..ctor` 写 `slotsPerSide=3` 而场景序列化 4 是同一个模式：**SO / MonoBehaviour 以资产为准，ctor 只是回退**。

**读点实测（`il2csharp` 全仓 grep）**：

| 字段 | 读点 | 我们该不该用 |
|---|---|---|
| `startingHand` | `PlayerHand.cs:286,1632` · `BattleManager.cs:39902,39907` | ✅ **在用**（起手 3 张的唯一真源） |
| `secondExtraCards` | `PlayerHand.cs:299,1642` | ✅ **在用**（后手 +1，教程 0） |
| `questPointsForTrigger` / `Alt` | `PlayerManager.cs:125,130,701,706,1184,1188` | ✅ **在用**（由 `GameplayVariablesData.alternativeTraitLogic` 二选一） |
| `clockTimeLimit` / `Reduced` / `clockCountdownSec` | `ClockManager.cs:173,252,263,462-466,598…729` · `BattleManager.cs:30650` | ✅ **在用** |
| **`startingMana`** | **0 处**（只有 `ScenarioVariables.cs:20` 的 ctor 赋值） | ⛔ **死字段** —— 与 `GameplayVariablesData.startingMana` **同名但不同类**，`BattleManager.SetupInitialMana` 只读后者 |
| **`maxMana`** | **0 处**（`AbilityLogic.cs:3621` 那个是 `TargetsAffected.maxMana` 枚举，不是它） | ⛔ **死字段**（我们不设能量上限 = 与原版同形） |
| **`maxCardsInHand`** | **0 处** | ⛔ **死字段** —— 手牌上限只读 `GameplayVariablesData.handLimit`（`BattleManager.MaxCardsInHand` `:21962` 读 `+0x44`），**不读** `+0x28` |

⇒ **重叠结论**：
1. **字段名真重叠 = 1 格（`startingMana`），且 `ScenarioVariables` 那一格原版是死的**，所以它的 `1` **不能**当 `GameplayVariablesData.startingMana` 判据（但见 §三 #8，它**恰好与官方文案反推出来的 1 吻合**，可当**旁证**）。
2. **语义重叠 = 1 格**（`ScenarioVariables.maxCardsInHand` ↔ `GameplayVariablesData.handLimit`），前者死，值 10 —— 也**只能当旁证**。
3. **跨类依赖 = 1 处**：`alternativeTraitLogic` 决定 `PlayerManager.AddQuestPoints` 取 3 还是 4；**3/4 这两个数本身在 `ScenarioVariables`（有真值），且官方文案 `Tips/Hud/PlayerQPCount(Alt)` 逐字写着**。
4. **`startingHand` / `secondExtraCards` 在 `GameplayVariablesData` 里根本没有** —— 这是「我们另加三格」的原因（`GameplayVariables.cs:384-396` 已如实标注）。

---

## §二 18 格 + AI 难度表 · 逐条溯源表

> 表头里的「本地真值」**分级**：
> `✅官方文案`= 1.35.0 客户端自带的规则文案（`I2Languages`，见 §附一）·
> `✅序列化资产`= 场景/SO 里读出来的数 ·
> `⚠️旁证`= 同概念但原版不读的字段 / 弱文案 ·
> `❌`= 无。
> 「离线/无事件时行为」一栏对**全部 18 格都一样**：`GameplayData == null` ⇒ `MatchMakerManager.StartMatch` 就在 `:271` 炸掉，**走不到战斗**；下面不再逐行重复，只写**如果这一格缺失/为 0 会出什么病**。

| # | 参数 | 原版消费点（`文件:行`） | 原版取值来源 | 缺省/为 0 时 | 本地是否有真值（证据） | 新结论 | 建议设计值 + 依据 |
|---|---|---|---|---|---|---|---|
| 1 | `maxLegendaries` | `DeckUtility.CanAddCard.c:105` 闸 = `if (0 < maxLegendaries)`；`CardDeck.cs:644,659,665` | 服务器 `PlayEventData.gameplayVariables` | 0 = **关掉整条闸** = 无上限（**这是合法值，不是"缺"**） | **✅官方文案**：`Event_Description/Classic/Help` = `No Legendary card limit`；`Skirmish/Help` = `Up to 4 Legendaries (Warlord not included)` | **本地有真值**（经典 0 / 遭遇 4） | 经典 **0**（哨兵=无上限）· 遭遇 **4** —— 与现码 `GameplayVariables.cs:112,118` 一致 |
| 2 | `numberOfCopiesOtherRarities` | `GameplayVariablesData.GetMaxCopiesInDeck(+0x14)`（`.c:11`），经 `DeckUtility` | 服务器 | 非传同名卡**一张都不许加** | **❌**（只有非官方规则书 `:53` 的 2 + 商店文案 `ShopOffers/Descriptions/GenericReinforcementDrop` = `2 copies of non-legendary ones` 的弱旁证） | **只能自定** | 保持 **2**（弱旁证方向一致） |
| 3 | `numberOfCopiesLegendary` | 同上 `+0x18`（`.c:8`） | 服务器 | 同上 | **✅官方文案**：`MenuShop/ExtraLegendaryWarning` = `You can only use one copy of each legendary card in your deck` | **本地有真值 = 1**（比原来只靠规则书强） | **1**，与现码一致 |
| 4 | `slotsPerSide` | `MinionManager.Awake`（`MinionManager.cs:98-107`）：先 `this.slotsPerSide = GameplayData.slotsPerSide` **再**按它重建两条槽位数组 | 服务器（`+0x1C`） | 0 ⇒ 两条槽位数组长度为 0 ⇒ **棋盘一格都没有** | **✅序列化资产**：13 场 × 2 个 `MinionManager` 实例全 = **4**（例 `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4372.json` `"slotsPerSide": 4`）；`MinionManager..ctor` 回退值 = 3 | **本地有真值（回退值） = 4** | 每侧 **4**（= 每边 9 格含督军），与 `BoardSpec.cs:33` 一致。⚠️ 但这是我们**结构上的 9**，与原版 `+0x1C` 同名不同义（`GameplayVariables.cs:124` doc 已写明） |
| 5 | `warlordLifeChange` | `BattleManager.SetupWarlordsLifeAndEnchantment.c:34-58` → `ForceChangeMaxHealth(督军, lifeChange + 当前最大生命)` | 服务器（`+0x20`） | 0 = 用卡面原值 | **✅官方文案 + 实拍**：`Classic/Help` `Warlords start with 10 more Health` · `Skirmish/Help` `Warlords start with 10 less Health`（**两句互为参照**）＋ 用户真机 Aeldari 40/35/35 vs SO 30/25/25 | **本地有真值**（经典 +10 / 遭遇 **0**，因为 SO 基准就是"少 10"那一档） | 经典 **+10** · 遭遇 **0** —— 与现码 `GameplayVariables.cs:112,117` 一致（2026-11-02 的改动是对的） |
| 6 | `deckSize` | `CardDeck.MaxDeckSize`（`CardDeck.cs:107`）· `DeckUtility.cs:136,173` · `DeckInfoPopup.cs:178` · `DeckEditingPanel.cs:126,311` | 服务器（`+0x24`）；另有 `VarsGlobal.deckSize=30`（展示层 `BattleManager.cs:1269`） | 0 ⇒ 一副牌都装不进、计数器显示 `0/0` | **✅官方文案 + ✅序列化资产**：`Classic/Help` `30-card decks` · `Skirmish/Help` `12-card decks`；`MenuDeck/Error/IncompleteDeck` = `You need to add 30 cards`；`VarsGlobal.deckSize=30`（`资料/VarsGlobal_原版数值.md`，`sharedassets0.assets` 两份安装逐字节一致） | **本地有真值**（经典 30 / 遭遇 12） | **30 / 12**，与现码一致 |
| 7 | `overtimeTurn` | `BattleManager._NextTurn_d__395.MoveNext.c:191-207`（`overtimeTurn <= turnCounter`，**可空**） | 服务器（`+0x28`） | **null ⇒ 这个模式永不进加时** | **❌**（只有相对比较：`Classic/Help` `Overtime begins later` vs `Skirmish/Help` `Overtime begins earlier`，**没有绝对值**） | **只能自定**（只能保证「遭遇 < 经典」） | 保持经典 **10** · 遭遇 **10**（现码两档同值，⚠️ 与"更早/更晚"文案不符 —— 见 §六 悬案 1） |
| 8 | `startingMana` | `BattleManager.SetupInitialMana.c:20-46`（先手取 `+0x2C`，`playerGoesFirst` 那个标志 XOR 换 `+0x30`）；`PlayerManager.TurnStart` 每回合 `manaPerTurn` 次 `AddNewMana(1)` | 服务器（`+0x2C`） | 0 ⇒ 首回合**只有** `manaPerTurn` 点 | **✅官方文案反推**：`Classic/Help` `Both players start at 2 energy`，而 `TurnStart`（`PlayerManager.cs:618-629`）在**首回合也跑 `manaPerTurn` 次** ⇒ `startingMana = 2 − manaPerTurn(1) = **1**`。交叉校验：`Skirmish/Help` `Player 1 starts at 3 Energy`，`3 − manaPerTurn(2) = 1` ⇒ **两模式同为 1**（若按"回合开始前"读，会得到 2 和 3 两个不同值 —— 那不合理） | **本地有真值（反推，两条文案互为校验）** | 经典 **1** · 遭遇 **1** —— 与现码 `ClassicStartingMana=1` 一致 |
| 9 | `startingManaSecond` | 同上 `+0x30` | 服务器（`+0x30`） | 同上 | **✅官方文案反推**：经典 `Both players start at 2` ⇒ `2−1 = **1**`；遭遇 `Player 2 at 3+1 Energy` ⇒ `4−2 = **2**` | **本地有真值** | 经典 **1** · 遭遇 **2** —— 与现码一致（`SkirmishStartingManaSecond=2`） |
| 10 | `addedManaSecond` | `BattleManager.cs:1055,1056`（`SetAccumulationMana(>0)` 是 **HUD 能量累积组唯一显隐闸**）· `_NextTurn_d395:296-352`（`turnCounter==1 且 !=0 ⇒ SetSavedMana(对方, it)`，只加 `unusedMana`） | 服务器（`+0x34`） | 0 ⇒ 累积 HUD 组不显示、后手不额外留能量 | **❌**（`Skirmish/Help` 那句 `Player 2 at 3+1 Energy` **可以**读成它，但也会把 P2 首回合能量顶到 5，与文案 4 冲突；见 `B88_J1` 的算术矛盾） | **只能自定** | 经典/遭遇 **0** · 教程 **1**（现码 `GameplayVariables.cs:254,483`，已带 `🔶 自己设计`） |
| 11 | `manaPerTurn` | `PlayerManager.cs:618-629`（`for i < manaPerTurn: AddNewMana(1)`，只抬 `totalMana`） | 服务器（`+0x38`） | 0 ⇒ **能量永不增长** | **✅官方文案**：`Classic/Help` `Gain +1 Maximum Energy each turn` · `Skirmish/Help` `Gain +2 Maximum Energy each turn` · 另有 `Tips/CostTip` / `PCTips/Battle/Energy` / `TutorialData/Tutorial1/Turn6/Tip1(fast)` 四处冗余印证 | **本地有真值**（经典 1 / 遭遇 2） | **1 / 2**，与现码一致 |
| 12 | `drawCardsPerTurn` | `BattleManager.cs:5002`（`for i < drawCardsPerTurn: AddDrawCard`） | 服务器（`+0x3C`） | 0 ⇒ **每回合不抽牌** | **✅官方文案**（间接但闭合）：`Tips/OvertimeTip` = `Draw 2 cards per turn`，而加时那一支是**额外 +1**（`TurnStartCardDraw.c:64-70`）⇒ 基础 = **1** | **本地有真值 = 1**（两模式同值，无反证） | **1**，与现码一致 |
| 13 | `manaAccumulation` | `BattleManager.cs:4970-4975`（`SetSavedMana(min(it, GetUnusedMana()))`，只在 `turnCounter > 1` 调） | 服务器（`+0x40`） | 0 ⇒ 不累积（回合末未花能量清零） | **✅官方文案（遭遇）**：`Skirmish/Help` `accumulate 1 unused point` ⇒ **1**；另有 `TutorialData/Tutorial1/Turn6/Tip2fast` = `You can accumulate 1 Energy per turn…` ⚠️ **经典没有任何文案说它累积** ⇒ 经典 = **0** 是"文案沉默"，不是"文案支持" | 遭遇 **本地有真值 = 1** · 经典 **⚠️旁证 = 0** | 经典 **0** · 遭遇 **1**，与现码一致 |
| 14 | `handLimit` | `BattleManager.cs:21962`（`MaxCardsInHand` **只读 `+0x44`**，全量反编译扫 `get_scenarioVariables` 那 16 个调用点，`+0x28` 读 **0** 次） | 服务器（`+0x44`） | 0 ⇒ **一张牌都抽不进手**（会立刻卡死）；文案 `Battle/Tips/HandFull` 证明这条闸存在但**不给数** | **⚠️旁证 = 10**：`ScenarioVariables.maxCardsInHand`（`DefaultScenario` / `TutorialScenario` / `EditorScenario` 三份**全是 10**）—— 同概念、原版不读 | **只能自定**（10 是三个独立资产都指向的同一个值，但**不是**这一格的读点） | 保持经典 **10** · 遭遇 **8**（遭遇的 8 只有非官方规则书） · 教程 **10** |
| 15 | `shouldReshuffle` | `BattleManager.cs:35032`（牌库空时：真 ⇒ 洗回墓地，假 ⇒ 走疲劳） | 服务器（`+0x48`） | false ⇒ 走疲劳 | **❌（弱旁证）**：`Battle/Tips/DamageFatigue` / `…FatigueEnemy` 两条疲劳文案存在 ⇒ 至少有一个模式是 false；不能反推全部 | **只能自定** | 保持 **false**（用户 2026-10-20 拍板「空了就只走疲劳」，与文案同向） |
| 16 | `showMulligan` | `BattleManager.cs:933,9247,39745,39863,39897` · `PlayerHand.cs:577`（假 ⇒ 连弹窗都不弹，直接 `ClickMulliganDone()`） | 服务器（`+0x49`） | false ⇒ 直接开打 | **✅官方文案**：`Classic/Help` `Both players get to mulligan their starting hand` · `Skirmish/Help` `No mulligan` | **本地有真值**（经典 true / 遭遇 false） | **true / false**，与现码一致 |
| 17 | `assignDefensiveCardsInEditMode` | `DeckEditingWindow.cs:841`（编辑器卡池筛选谓词，真 = 保留防御卡） | 服务器（`+0x4A`） | false ⇒ 编辑器卡池里没有防御卡可选 | **❌**（只能从 `MenuDeck/Error/HasNotAssignedDefensiveCard`「你有可用防御卡但没装」反推"池里确实有" ⇒ 更像 true，但**不是**这一格的判据） | **只能自定**（⚠️ **我们 0 读点**，选哪个都不影响行为） | 保持 经典 false / 遭遇 true（占位），并在 doc 里注明"零行为影响，等编辑器那条路做起来再定" |
| 18 | `alternativeTraitLogic` | `CardScript.cs:6456`（祈祷结束 +1 信仰）· `PlayerManager.AddQuestPoints`（阈值 3/4）· `PlayerManager.ResetMana` | 服务器（`+0x4B`） | false ⇒ 用 3 触发、祈祷不加信仰 | **❌（值）／✅（语义真值）**：`Tips/Hud/PlayerQPCount` = `for each **3** Quest Progress` · `PlayerQPCountAlt` = `for each **4**` ⇒ **"它到底切什么"有了官方判据**；两模式各取哪一支 = 无文案 | **只能自定（值）**，但**语义已钉死** | 经典 **false** · 遭遇 **true**（现码，用户 2026-10-25 口径；3/4 两数与 `ScenarioVariables` + 官方文案双重吻合） |
| 19 | **AI 难度表**（`AIBotsConfig.BotList` 分档） | `MatchData.GetNormalBotConfig`（`decomp_full/MatchData__GetNormalBotConfig.c:60-78`）：`lossStreak ∈ [minLossStreak,maxLossStreak]` 且 `rank ∈ [minRank,maxRank]` 取第一条命中，**全不命中取 `BotList[0]`** | **TitleData**：`ConfigManager.GetConfig<AIBotsConfig>()`（`AIBotsConfig` 有 8 个 `[SerializeField]` 字段、`new T()` 为空 ⇒ **没有它 = 空列表 = NRE**） | 无 AI 牌组可选 | **分档规则 ❌**（`grep "\"BotList\"" / "\"botNames\"" / "\"minLossStreak\"" / "\"skipChance\"" / "\"card_level_rank_offset\""` 全 0）；**牌池 ✅序列化资产**：`bundle_prebuiltdecks_assets_all` **236 副** `PrebuiltDeck`（12 副名字含 `Tutorial` 被 `PrebuiltDeckCollection.AddDeck` 跳过 ⇒ 有效 **224**） | **分档规则只能自定 · 牌池本地有真值** | 见 §二·B（牌池交叉表 + `GetDeckByDifficulty` 的等概率取法） |

### §二·B AI 牌池（本地真值，`bundle_prebuiltdecks_assets_all/MonoBehaviour/*.json`）

`PrebuiltDeck` 的序列化字段（首条 `AM_SK_1.json` 实读）：`aiMode` / `gameMode` / `difficulty` / `isPracticeDeck` / `Type` / `cardLibraryIds` / `deckHero` / `deckAvatar` / `deckName` / `uniqueId`。

**交叉表（名字含 `Tutorial` 的 12 副已剔除 —— `PrebuiltDeckCollection.AddDeck` 明确跳过）**：

| `gameMode` | `difficulty=0` SuperEasy | `5` Easy | `10` Normal | `15` Hard | 合计 |
|---|---|---|---|---|---|
| **0 Classic** | 12 | 15 | 74 | 17 | **118** |
| **13 Skirmish** | 0 | 26 | 40 | 40 | **106** |

- `difficulty` 的取值 = `DeckDifficultyLevel` 枚举（`dump.cs:45568-45576`：`SuperEasy=0 / Easy=5 / Normal=10 / Hard=15`）—— ⚠️ **它就是 `MatchData..ctor` 第 5 个参数 `botDifficulty` 的类型**，默认 `Normal(10)`。
- 取牌规则 = `PrebuiltDeckCollection.GetDeckByDifficulty`（`PrebuiltDeckCollection.cs:107-116` + 谓词 `:161-166`）：`d.difficulty == 目标 && d.gameMode == 目标` 过滤后，**`Random.Range(0, count)` 等概率取一张**（`sub_182ffde00(0, count)`）。
- `isPracticeDeck = 1` 的共 **103** 副（这批才是"练习"池：Classic 49 / Skirmish 54，见上表括号分布），另有 `Type=DeckType.Bot(1)` 共 28 副。
- ⚠️ `PrebuiltDeckCollection.AddDeck` 里 `deck.name.Contains("Tutorial")` 用的是 **`name`（Unity 资产名）** 而不是 `m_Name`；首条 `Aeldari_Deck0_Tutorial5_Ghaelyn` 两者相同，可当等价。

⇒ **可以照抄的部分**：难度分 4 档、每档 × 每模式有哪些牌、取牌是等概率随机。
⇒ **抄不到的部分**：`minRank / maxRank / minLossStreak / maxLossStreak / skipChance / minSkips / maxSkips`（`AIBotsConfig.BotConfig`，`dump.cs:46520-46532`）以及 `BotCardLevelConfig`（`card_level_rank_offset` / `k_factor` / `card_level_2_chance` 等，`dump.cs:46568+`）。**这 1 条仍是自定**。

---

## §三 判定

### 3.1 「原版离线对战」到底存不存在？

**严格意义上不存在。** 证据链：

1. **开机就要服务器**：`PlayerDataManager._InitializeLiveOpsAndConfig_d__527`（`PlayerDataManager.cs:9822-9833`）直接 `PlayfabWrapper.GenericCloudScriptHandlerAsync<InitializeLiveopsResult>(CloudHandle.InitializeLiveOps, …)`，`_UnpackUserInfo_d__522`（`:10335-10358`）把结果里 `TitleData` / `SharedEventData` / `HandlerData` 分别 `Unpack(...)` 进去 —— **没有 try/catch 兜底、没有本地分支**。
2. **参数只能从这条链来**：`HandlerData → LiveOpsHandler.events → GameModes.GetEvents() → GetActiveEvent(mode) → GetBaseData().gameplayVariables → MatchData.GameplayData`。中间任何一环断了，`StartMatch` 在 `MatchMakerManager.cs:271` 就 NRE。
3. **包里确实没有**：16 个本地 `LiveOpsEventData` 实例无一是 `PlayEventData`；安卓 1.35.0 的 87 个远端 bundle 无事件数据包。
4. **本机存档也没有**：`D:/2/关服前存档/Warpforge/` 里 `eventcache` 是 **Unity Analytics 的事件缓存**（首字节 `UGSEventCache`，内容是 `windowOpen` / `matchFinished` 之类的遥测），**不是** LiveOps；`RemoteConfigCache.json` 只有 `EnableTroopVOs` / `EnableWarlordVOs` 两个布尔；`Player.log` / `Player-prev.log`（18 MB + 6 MB）里 `LiveOps` / `HandlerData` / `playMode` **0 命中**。

**但「原版有单人打 AI 的模式」是真的**，那两个模式叫 `OfflinePractice(6)` 与 `OwnDeckTraining(12)`，**唯一的特殊性是"不用找对手、走本地 bot 牌组"，参数照样吃服务器那一份**。

### 3.2 参数从哪来（分三档）

| 档 | 内容 | 条数 |
|---|---|---|
| **A · 官方客户端自带文案能钉死的** | #1 传说上限 · #3 传说同名 · #5 督军生命 · #6 牌组张数 · #8/#9 起始能量 · #11 每回合能量 · #12 每回合抽牌 · #13 累积（遭遇）· #16 换牌 | **10 格** |
| **B · 序列化资产能钉死的** | #4 每侧格数 = 4（13 场 × 2 个 `MinionManager`）· #14 手牌上限**旁证** 10（三份 `ScenarioVariables`）· AI 牌池 224 副 | **1 格硬 + 1 格旁证 + 1 张牌池** |
| **C · 真的只剩自定** | #2 非传同名上限 · #7 加时回合 · #10 后手累积能量 · #14 手牌上限（严格档）· #15 洗回墓地 · #17 编辑器防御卡 · #18 altTrait 取哪一支 · #19 AI 分档规则 | **7~8 条** |

> ⚠️ **A 档的性质要说清**：这 10 格**不是**「从 `GameplayVariablesData` 二进制里读到的数」，而是**原版自己写给玩家看的规则说明**。用户口径「一切照原版复刻」下，这就是**设计者公开承诺的规则**，比我们的推断强、比非官方规则书强；但它**挡不住服务器在某个版本里偷偷调过值**（文案没改）。所以账里应写成 `来源 = 官方客户端文案（I2Languages），非二进制值`，而**不是** `✅与原版一致`。

### 3.3 关键词：哪些参数本地其实拿得到

**拿得到（11 格 + 1 张牌池）**：
`maxLegendaries`(0/4) · `numberOfCopiesLegendary`(1) · `slotsPerSide`(4) · `warlordLifeChange`(+10/0) · `deckSize`(30/12) · `startingMana`(1) · `startingManaSecond`(1/2) · `manaPerTurn`(1/2) · `drawCardsPerTurn`(1) · `manaAccumulation`(遭遇 1) · `showMulligan`(true/false) · AI 牌池（118 Classic / 106 Skirmish，4 档难度）。

**拿不到（7~8 条）**：`numberOfCopiesOtherRarities` · `overtimeTurn` · `addedManaSecond` · `handLimit` · `shouldReshuffle` · `assignDefensiveCardsInEditMode` · `alternativeTraitLogic`(取哪一支) · `AIBotsConfig` 分档规则。

---

## §四 给调度台的一句话

> **「只能自定」可以从 19 条压到 7~8 条**：其中 **10 格**靠原版客户端自带的 `Event_Description/Classic/Help` · `Skirmish/Help` + 5 条 `Tips/*` 文案**逐值钉死**（0/4、1、4、+10、30/12、1、1/2、1/2、1、1、true/false），**1 格**靠 13 场序列化的 `MinionManager.slotsPerSide=4`，**AI 牌池 224 副**也在本地（只剩分档规则自定）。真正剩下的 7~8 条是：非传同名上限、加时回合、后手累积能量、手牌上限、洗回墓地、编辑器防御卡、altTrait 取哪一支、AI 分档规则。

---

## §五 仍未查清（缺口，⛔ 不是"没有"）

1. **`GameModes.CurrentPlayingEvent` 谁写的** —— 三源反编译都只有定义（`GameModes.cs:32-46` / `:316-322`）与 `CombatFinished` 里的 `= null`（`:310`），**0 个调用方**。"进模式窗时设进去"那一跳读不到。⇒ 我们的模式入口不需要复刻它，但**不能据此说原版"进模式时按 mode 现挑事件"**。
2. **战役（Campaign）的 `GameplayData` 由谁填** —— `CampaignEventData : LiveOpsEventData`（**不是** `PlayEventData`，`dump.cs:124995`）⇒ 它根本没有 `gameplayVariables`；而 `PlayModes.Campaign(8)` 全仓 **0 个调用点**。⇒ 要么战役用的是另一条（读不到的）入口，要么战役也复用 `CurrentPlayingEvent` 里某个 PlayEvent。
3. **`LiveOpsEvent_2.ValidateEventData`** 体未恢复（三源都是 `NotImplementedException` / 乱码）。理论上它可能"补默认值"，但 `GameplayVariablesData` 无字段初始值 ⇒ 即使补也只是零值，**不构成可用回退**。
4. **`DraftEventBase.EventPlayMode`** 未恢复（`dump_full` 无产物、ILSpy 抛 `NotImplementedException`）。
5. **`AIBotsConfig` 的 ctor 是否给 `BotList` 初始值** —— `AIBotsConfig..ctor` 在 `decomp_full` 里的产物我没逐个读；若它有初始值，则在"TitleData 缺键"时会拿到一份内置表。⚠️ 但即使有，本地 `assets_full` 里也**搜不到** `BotList` 字段名，说明它**不来自任何序列化资产**。**建议下一步**：读 `AIBotsConfig..ctor`（RVA `0x64BF40`）的机器码看有没有 `new List<BotConfig>` + `Add`。
6. **`PrebuiltDeckCollection` 的 `decks` 装载顺序** —— `GetDeckByDifficulty` 用 `Random.Range(0, count)` 取"过滤后的第 n 个"，所以**顺序只影响可复现性**，不影响集合。但我们的自检若要复现"原版这一局抽到哪副"，需要原版的 `Random` 序列（`MatchData.randomNumbers` 预烧 100 个那套）。

## §六 顺手发现（不改，只登记）

1. **`GameplayVariablesData__.ctor.c` 文件名与内容不符**：内容其实是 `<>f__AnonymousType31___ctor`（调用 `NetworkingPeer__OnMessage`）。真正的空构造器只存在于 `out_il2csharp`。⇒ 谁按 `.c` 判「ctor 有默认值」都会读错。
2. **`PlayEventData__get_GameplayVariables.c` 内容也是别人**（`ActiveSkillDisplay__get_parentCard`，体 = `return *(param_1 + 0x80)`）。偏移 `0x80` **正好**是 `gameplayVariables` ⇒ 属 ICF 折叠/错位，不是缺产物。同一个错误形状在 `ScenarioVariables__.ctor.c` 上反过来了（那份是**对的**）。
3. **`IdentifiableSO__.ctor.c` 首行是 `AddressablesBuildNumber___ctor`** —— 又一处文件名与内容不符，但体（`UnityEngine_ScriptableObject___ctor`）是对的。
4. **`MatchData.playerStartMana` / `enemyStartMana` 是只写死字段**（§一 假设 3）。谁看到 `= 2` 都别当成"起始能量 2"。
5. **`PrebuiltDeck.aiMode` 236 副全是 0** ⇒ 这个字段在当前 build 是**死值**，AI 行为差异不靠它。
6. **`MatchData.SetMatchRecording` 有 Classic 回落**（`MatchData.cs:752-766`）：录制的 `gameMode` 事件不活跃时，**回落 `GetActiveEvent(default)` = Classic(0)**。⇒ 回放也会读 `GameplayData`，服务器没了同样播不了。
7. **`ScenarioVariables` 三份资产的 ctor 与序列化值不一致**（ctor 3/5/1/60/30/10 vs 资产 1/3/1/60/10/15）—— 提醒：**判 SO 值必须读资产，不能读 ctor**（`MinionManager` 的 `slotsPerSide` 3 vs 4 是同一模式）。
8. **`Battle/Tips/HandFull` 证明 `handLimit` 这道闸存在**，但**任何文案都不给数** —— 这与 `B120_E` 记的"10/8"只能靠规则书完全一致。

## §附一 本地真值的证据坐标（复核用）

| 证据 | 路径 |
|---|---|
| Classic / Skirmish 规则文案（**1.35.0 客户端自带**） | `D:/4/_tmp_view/android_1350/work_A3/i2_android.json`（`mTerms` 10,857 条，`Languages[0]` = 英文）；键 `Event_Description/Classic/Help` · `Event_Description/Skirmish/Help` |
| 同一批文案（2025-07-30 远端包口径，13 语言） | `D:/4/Unity/MyGame/Assets/CardPresentation/Resources/official_loc_13lang.json`（`keys` / `en` / `zh`），10,862 键 |
| `Tips/*` 与 `MenuShop/ExtraLegendaryWarning` | 同上两份，键名见正文 |
| `ScenarioVariables` 三份资产 | `D:/2/新解包资源/assets_full/bundle_duplicateassetisolationso_assets_all/MonoBehaviour/{Default,Editor,Tutorial}Scenario.json`；YAML 版 `D:/2/tools/assetripper_out/ExportedProject/Assets/DefaultScenario.asset` |
| `MinionManager.slotsPerSide = 4` | `D:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena*/MonoBehaviour/*.json`（13 场 × 2 实例） |
| `VarsGlobal.deckSize = 30` | `D:/4/Unity/资料/VarsGlobal_原版数值.md` §一（`sharedassets0.assets` 偏移 `0x7FA8C0` 两份安装逐字节一致） |
| AI 牌池 236 副 | `D:/2/新解包资源/assets_full/bundle_prebuiltdecks_assets_all/MonoBehaviour/*.json` |
| ConfigProvider 无序列化载荷 | `D:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/Intro.unity:7350-7358`（`AIBotsConfigProvider`）与 `:…`（`GameModes`，guid `915931d9d043503477cb677fb373cd5e`） |
| `ConfigProvider.Initialize/UnpackInternal/get_Config` | `D:/2/tools/decomp_generic_trial/out_b94/ConfigProvider_object_.{Initialize__16826b0,UnpackInternal__1682730,get_Config__4c68a0}.c` |
| 事件来源链 | `D:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/Everguild/MatchMakerManager.cs:179-204,241-271` · `PlayerDataManager.cs:9822-9833,10352-10358` · `GameModes.cs:329-356,531-548,586` · `CardDeck.cs:33-42` |
| `MatchData` 构造 / GameplayData 三个写点 | `D:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/MatchData.cs:131-153,351,766` |
