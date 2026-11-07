# B168 · D3 奖励页「活动」黑屏 —— filter / 背景 / 事件类型

> 日期：2026-11-07 · 写手 D3 · **未起客户端、未跑 Unity、未动 git**
> 只改：`E:/warpforge/05_工具/server/tools/add_all_configs.py` +
> 生成出的 `responses/ExecuteCloudScript.InitializeLiveOps.json` 与 `responses/fragments/playfab_profile.json`
> 未改：`server.py`、mod 源码、游戏、任何解包资源。

---

## 一 结论速览

| 项 | 结果 |
|---|---|
| 主因①：`OfflineCampaign` 的 `$type` 是**短名**（类是 `Everguild.LiveOps.CampaignEventData`） | ✅ 已修为全名。短名会退化成基类 `LiveOpsEventData`（concrete）⇒ 事件被**静默丢弃** |
| 主因②：`Data` 缺 `filter` ⇒ `army` 默认 `Neutral(0)` ≠ `SelectedArmy(10)` | ✅ 已补 `filter`（9 个键，形状照 `Ultramarines Campaign.json` 逐字抄） |
| `Data.Background.m_AssetGUID` 为空 ⇒ 补完 filter 仍是纯黑 | ✅ 已补 `2cca2c1f18e963f48ace4a407a19d626`（`Campaign_Faction_Bck_Ultramarines`，离线 catalog 里已确认） |
| 全库其他 liveops 事件有没有同类 `$type` 短名问题 | ✅ **没有**：本轮把 42 处（26 种）逐个对 `dump.cs` 的 `// Namespace:` 核过，**只有 Campaign 这一处** |
| `PointsConfig` 加 `CampaignPoints`（可选项） | ⛔ **本轮不做**，理由见 §五·1（判据链已查清，不是遗漏） |
| 改完是否需要重启服务端 | 🔴 **是**。`server.py` 不热重载，必须 kill 旧 python 再起（见 §四） |

---

## 二 改动清单

### 2·1 `tools/add_all_configs.py`（3 处）

1. **`CAMPAIGN_EVENT.Data["$type"]`**
   `CampaignEventData, Assembly-CSharp` → **`Everguild.LiveOps.CampaignEventData, Assembly-CSharp`**
   - 判据：`d:/2/tools/il2cpp_out/dump.cs:124993` 的 `// Namespace: Everguild.LiveOps`、`:124995` 的声明。
   - 失败形状（R2 §3·5 已实测过一次）：`Type.GetType("CampaignEventData, Assembly-CSharp")` 解析不到
     ⇒ `TypedSerializer.ReadJson` 把 `type1` 退回**声明类型** `LiveOpsEventData`
     ⇒ 它是 **concrete**（`dump.cs:126492`）⇒ 走「可赋值」分支、**反序列化成功且零日志**
     ⇒ 运行时类型是基类 ⇒ `LiveopsHelper.TryGetEventFromData` 查不到包装类 ⇒ `CreateEvent` 返回 null
     ⇒ **事件既不进 live 也不进 blocked，直接消失**。
   - ⚠️ 这一条其实**比 filter 更早把页面废掉**：事件根本没被创建，
     `CampaignHandler.GetEvents()` 里没有 `OfflineCampaign` —— 这正解释了判据报告 §七 那条
     「诚实边界：不能证明 OfflineCampaign 一定进了 GetEvents()」。本轮把这条边界**实锤掉**。

2. **`CAMPAIGN_EVENT.Data` 新增 `filter`**（放在 `location` 之后、`drawData` 之前）
   ```json
   {"army": 10, "expansion": 0, "hasDuration": false, "eventDuration": 24,
    "hasCooldown": false, "eventCooldown": 168, "eventGroups": [],
    "Triggers": {"list": []}, "Conditions": {"list": []}}
   ```
   - 形状来源（⛔ 不是自己编的）：编辑器资产
     `02_解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/Ultramarines Campaign.json` 的 `filter` **逐字段照抄**。
   - 字段真值：`EventFilter`（`dump.cs:125811-125841`）：`army`(CardArmy) · `expansion`(CardExpansionType) ·
     `hasDuration`(bool) · `eventDuration`(int) · `hasCooldown`(bool) · `eventCooldown`(int) ·
     `eventGroups`(List) · `Triggers`(`DynamicList<EventTrigger>`，序列化键 `list`) · `Conditions`(同)。
   - `army = 10` = `CardArmy.Ultramarines`（`dump.cs:45634-45652`），
     与 `CampaignHandler.SelectedArmy` 默认值一致；`server.py:_inject_liveops` 也会把
     `Campaign.SharedData.selectedArmy = 10` 注进去（本波实测两口都是 10）。
   - 消费链：`CampaignWindowTab.OnOpen` → `ChangeArmy(SelectedArmy)` →
     `campaigns.FirstOrDefault(c => c.Army == army)` → 命中才 `SetActiveCampaign`
     （`CampaignWindowTab.cs:600-674`）。不命中就**什么都不做**（没有 `First()` 兜底，同窗 Forge 页有）。

3. **`CAMPAIGN_EVENT.Data["Background"]["m_AssetGUID"]`** 空串 → **`2cca2c1f18e963f48ace4a407a19d626`**
   - `CampaignEventData.Background` 是 `AssetReferenceTyped<Sprite>`（`dump.cs:125004`）；
     guid = `Assets/Campaign_Faction_Bck_Ultramarines.asset` 的 `m_RenderDataKey`，
     贴图在 `campaignrewardbackgrounds_assets_all.bundle`，guid **已确认在离线 catalog 里**。
   - 不补这一条：`CampaignUIBackground.LoadImage` 不会打开 `Background Image` ⇒ 页面仍是 prefab 那颗黑底。

4. **`REFRESH_EVENT_IDS` 增加 `"OfflineCampaign"`**（这一条不改，上面三条**一条都落不了盘**）
   - 写入循环对「已存在的 eventId」默认 **跳过**；`OfflineCampaign` 原本不在白名单里。
   - 实测：加进去之后 `--write` 才会打印 `[+事件] Campaign.OfflineCampaign（覆盖）`。

> 注释里写全了上面每条的判据出处，并标了
> `🔶 自己设计` 只用在「我们没有真·线上活动数据」这一点上（沿用原文件口径），
> **filter 的字段值不是自己设计** —— 那是编辑器资产里的原版形状。

### 2·2 生成产物（已实际落盘）

| 文件 | 变化 |
|---|---|
| `responses/ExecuteCloudScript.InitializeLiveOps.json` | **语义差异只有 3 处**（`$type` / `Background.m_AssetGUID` / 新增 `filter`）；raw diff 24 行。其余 handler 条数、`TitleData` 一字未动 |
| `responses/fragments/playfab_profile.json` | **重写后逐字节不变**（md5 前后同为 `d6766eeb…`）—— 无并发漂移 |

改前备份（本轮新建，未被脚本的 `.bak_before_b160` 覆盖逻辑影响）：
`responses/ExecuteCloudScript.InitializeLiveOps.json.bak_before_d3` ·
`responses/fragments/playfab_profile.json.bak_before_d3`

现读 `Campaign.RawEvents[0].Data` 关键片段：

```json
"$type": "Everguild.LiveOps.CampaignEventData, Assembly-CSharp",
"filter": {"army": 10, "expansion": 0, "hasDuration": false, "eventDuration": 24,
           "hasCooldown": false, "eventCooldown": 168, "eventGroups": [],
           "Triggers": {"list": []}, "Conditions": {"list": []}},
"Background": {"m_AssetGUID": "2cca2c1f18e963f48ace4a407a19d626"}
```

---

## 三 `$type` 全量审计（26 种 / 42 处，逐个对 `dump.cs` 现核）

判法：取 `$type` 里的类型全名 →
① 带命名空间的，必须等于 `dump.cs` 里该类上方 `// Namespace:` 的值；
② **短名只在「类在全局命名空间」时才合法**（`// Namespace:` 为空行）。
⚠️ 脚本必须排除编译器的嵌套声明（`CampaignEventData.<>c` 那种在全局命名空间，会造出**假阳性 OK**）——
本轮第一版检查器就栽在这上面，加 `(?![\w.])` 之后才抓出真凶。

| # | 事件类型（现写法） | 原写法 | dump.cs 真值 | 判定 | 用在哪些事件 |
|---|---|---|---|---|---|
| 1 | `CampaignPersistence, Assembly-CSharp` | 同 | `(global)` · `:52289` | ✅ | Campaign/OfflineCampaign |
| 2 | `Everguild.LiveOps.CampaignEventData, Assembly-CSharp` | **`CampaignEventData, Assembly-CSharp`** | `Everguild.LiveOps` · `:124995` | 🔴 **已修** | Campaign/OfflineCampaign |
| 3 | `Everguild.LiveOps.DraftEventData, Assembly-CSharp` | 同 | `Everguild.LiveOps` · `:127940` | ✅ | GameModes/OfflineDraft |
| 4 | `Everguild.LiveOps.DraftEventSaveGame, Assembly-CSharp` | 同 | `Everguild.LiveOps` · `:126817` | ✅ | GameModes/OfflineDraft |
| 5 | `Everguild.LiveOps.ExpansionPassData, Assembly-CSharp` | 同 | `Everguild.LiveOps` · `:132241` | ✅ | ScoringEvent × 4 |
| 6 | `Everguild.LiveOps.ExpansionPassSave, Assembly-CSharp` | 同 | `Everguild.LiveOps` · `:132456` | ✅ | ScoringEvent × 4 |
| 7 | `Everguild.LiveOps.FixedStoreData, Assembly-CSharp` | 同 | `Everguild.LiveOps` · `:134122` | ✅ | Shops × 3 |
| 8 | `Everguild.LiveOps.ForgeItemData, Assembly-CSharp` | 同 | `Everguild.LiveOps` · `:128264` | ✅ | Forge/OfflineForge |
| 9 | `Everguild.LiveOps.GameMessageData, Assembly-CSharp` | 同 | `Everguild.LiveOps` · `:128764` | ✅ | Inbox |
| 10 | `Everguild.LiveOps.GameMessagePersistence, Assembly-CSharp` | 同 | `Everguild.LiveOps` · `:128828` | ✅ | Inbox |
| 11 | `Everguild.LiveOps.PracticeEventData, Assembly-CSharp` | 同 | `Everguild.LiveOps` · `:130255` | ✅ | GameModes/PracticeEvent |
| 12 | `Everguild.LiveOps.PracticeEventSaveGame, Assembly-CSharp` | 同 | `Everguild.LiveOps` · `:130267` | ✅ | GameModes/PracticeEvent |
| 13 | `Everguild.LiveOps.RankedV2Save, Assembly-CSharp` | 同 | `Everguild.LiveOps` · `:131309` | ✅ | GameModes/OfflineClassic |
| 14 | `Everguild.LiveOps.ShopItemPersistence, Assembly-CSharp` | 同 | `Everguild.LiveOps` · `:133508` | ✅ | Shops/OfflineOfferStarter |
| 15 | `Everguild.LiveOps.ShopStorePersistence, Assembly-CSharp` | 同 | `Everguild.LiveOps` · `:134405` | ✅ | Shops × 3 |
| 16 | `Everguild.LiveOps.TutorialEventData, Assembly-CSharp` | 同 | `Everguild.LiveOps` · `:130320` | ✅ | GameModes/TutorialEvent |
| 17 | `Everguild.LiveOps.TutorialEventSaveGame, Assembly-CSharp` | 同 | `Everguild.LiveOps` · `:130329` | ✅ | GameModes/TutorialEvent |
| 18 | `FastModeSave, Assembly-CSharp` | 同 | `(global)` · `:57579` | ✅ | GameModes/OfflineSkirmish |
| 19 | `ForgePersistence, Assembly-CSharp` | 同 | `(global)` · `:57841` | ✅ | Forge/OfflineForge |
| 20 | `MainMenuMissionData, Assembly-CSharp` | 同 | `(global)` · `:60393` | ✅ | Missions × 4 |
| 21 | `MissionPersistence, Assembly-CSharp` | 同 | `(global)` · `:61024` | ✅ | Missions × 4 |
| 22 | `ParentEventData, Assembly-CSharp` | 同 | `(global)` · `:62661` | ✅ | ParentEvents/RewardsMenu |
| 23 | `ParentEventPersistence, Assembly-CSharp` | 同 | `(global)` · `:62725` | ✅ | ParentEvents/RewardsMenu |
| 24 | `RankedFastModeData, Assembly-CSharp` | 同 | `(global)` · `:57782` | ✅ | GameModes/OfflineSkirmish |
| 25 | `RankedV2Data, Assembly-CSharp` | 同 | `(global)` · `:64030` | ✅ | GameModes/OfflineClassic |
| 26 | `ShopOfferDataV2, Assembly-CSharp` | 同 | `(global)` · `:66074` | ✅ | Shops/OfflineOfferStarter |

**结论：42 处 `$type` 里，只有 `CampaignEventData` 一处写错；其余 25 种全部正确**
（「短名」的 10 种都在全局命名空间，短名即全名，`Type.GetType` 能解析）。
`Rewards` / `RankedScoreBoost` 两个 handler 现读 `RawEvents` 为 0 条，没有类型可审。

顺带把**生成器**里全部 21 个 `$type` 字面量（不止事件，含 `PointsConfig` / `LeaderboardConfig` /
`Templates` / `AlliancesConfig` 等）也逐个核过 —— **全部正确**（`DailyLoginChallenge` 是赋值语句形态，
单独核过：`dump.cs:59143` 段 `// Namespace:` 为空 = 全局）。
两份文件的 `TitleData` 里嵌的 JSON 也扫过：11 种 `$type`，**0 错**。

---

## 四 自检（全部现跑，未起游戏）

### 4·1 静态自检 —— `27 PASS / 0 FAIL`

脚本：`E:/warpforge/_tmp_view/d3_selftest/verify_d3.py`

| 检查 | 期望 | 实测 |
|---|---|---|
| `Campaign.RawEvents` 条数 | 1 | ✅ 1 |
| `Data.$type` | `Everguild.LiveOps.CampaignEventData, Assembly-CSharp` | ✅ |
| `filter.army` | 10 | ✅ 10（其余 8 个键逐个断言通过） |
| `Background.m_AssetGUID` | `2cca2c1f18e963f48ace4a407a19d626` | ✅ |
| `PointsPerLevel` / `Nodes` / `Persistence` 未被误改 | 10 / 3 条 / IsActive=true | ✅ |
| 响应里所有 `$type`（26 种） | 与 `dump.cs` 一致 | ✅ 0 bad |
| `GameModes` | 5 条、无 V3 | ✅ `TutorialEvent / OfflineClassic / OfflineSkirmish / OfflineDraft / PracticeEvent` |
| `ScoringEvent` | 4 条 | ✅ |
| `UserInventory` | 2590 | ✅ 2590 |
| 两份 JSON 共有 `TitleData` | 同值 | ✅ 20/20（`playfab_profile` 另有 3 个独有 key，改动前就如此） |
| 行尾 | 纯 LF | ✅ 两份 CRLF=0 / 裸 CR=0 |

`HandlerData` 各 handler 条数（未变）：
`ParentEvents 1 · Shops 4 · GameModes 5 · Inbox 1 · Missions 4 · Forge 1 · Campaign 1 · Rewards 0 · ScoringEvent 4 · RankedScoreBoost 0`

### 4·2 端到端自测（临时端口 8901，临时 state/log，跑完自清）

脚本：`E:/warpforge/_tmp_view/d3_selftest/run_selftest.py`

```
LoginWithSteam OK, keys: ['code', 'data', 'status']
Campaign RawEvents: ['OfflineCampaign']
Campaign SharedData: {'selectedArmy': 10}
GameModes eventIds: ['TutorialEvent', 'OfflineClassic', 'OfflineSkirmish', 'OfflineDraft', 'PracticeEvent']
ScoringEvent count: 4
FAILS: []
ALL ASSERTIONS PASSED
```

⇒ 客户端**实际会收到**的那一份里：`filter.army == 10`、`Background` guid 正确、`$type` 全名、
且 `Campaign.SharedData.selectedArmy == 10` —— **两边都是 10，`ChangeArmy` 这次能命中**。

### 4·3 幂等

`--write --write-profile` 连跑两次，两份文件 md5 不变：
`ExecuteCloudScript.InitializeLiveOps.json = 4ea10a2f620956e994f7a48aee5f6d92` ·
`playfab_profile.json = d6766eeb868dd4b459a2d6bd67f7a35a`

### 4·4 🔴 生效前提：**重启服务端**

`server.py` **不热重载**，响应模板只在启动时读一次。调度台需要 **kill 旧 python 进程再起**
（⛔ 本代理没有起/停服务端，只在临时端口上自测过，真环境 `server/logs`、`server/state` 未被写）。

---

## 五 没做 / 判据待查（⛔ 都不是「猜着做了」）

### 5·1 `PointsConfig` 加 `CampaignPoints`（可选项）—— **本轮不做**

判据链已查清：

- 活动页底部那个点数由 `Missions.GetPointsPerArmyOnDailyLogin(army)` 算，谓词是
  **`x.Item is CampaignPoints && item.cardArmy == army`**
  （`cpp2il/out_cpp2il_dev_ilspy/Missions.cs:713-745` + `:2067`）。
- 我们的 `Daily_Login` 奖励是 `{"item": "campaignPoints", "quantity": 50}` ——
  那是 `CurrencyConfig` 里 `code="campaignPoints"`(type=71) 的 **`Currency`**，`is CampaignPoints` = **假**
  ⇒ 现读恒定返回 0 ⇒ 面板显示 `0`。
- 要变成 50 得**两步一起做**：① `PointsConfig.points` 加一条 `CampaignPoints`（`cardArmy:10` + 两张图标）；
  ② 把 `Daily_Login` 的 reward item 改成那条的 id（`"Campaign Points Ultramarines"` 一类，不是 `campaignPoints`）。
- **不做的理由**（不是成本，是回归风险）：②会把**已经验收通过的奖励页**（3 张任务卡 + 图标 + 300 点）
  从「走 Currency 的真图标链」切到「走 Points 链」；而 `CampaignPoints.GetIcon` 在两张图标为空时**直接 throw**
  （`decomp_full/CampaignPoints__GetIcon.c` 末尾），顶栏 `ResourcesBarController.GetCounter` 也在同一条风险面上。
  `B168_S3_改名联盟榜单.md` §七 上一轮已复核并决定**不加**；本波沿用，⛔ 没有推翻。
- **只影响那个数字，不影响页面是否显示**（判据报告 §五·3 原文）。⇒ 判 **第二批**，
  单独一轮做（备好两张真图标 + 改完奖励页/顶栏各复验一次）。

### 5·2 ⚠️ 顺手发现：`Nodes` 的三条节点 `UnlockedBy` **全是空数组**

- 现读：`c1 UnlockedBy=[]` · `c2 UnlockedBy=[]` · `c3 UnlockedBy=[]`，而 `Unlocks` 是 `c1→[c2]`、`c2→[c3]`。
- 影响面：`CampaignTrack.GetNodesOffset` 取 **第一条 `UnlockedBy.Count == 0`** 的节点当起点
  （`CampaignTrack.cs:874-920`），三条都为空 ⇒ 起点仍是 `c1`（Position 0,0），**同屏偏移不会错**；
  但 `GetNodeState` 用 `Any(nodeData.UnlockedBy, id == 已收集节点)` 判「是否已解锁」
  （`CampaignTrack.cs:170-215`），`UnlockedBy` 空 ⇒ c2/c3 **永远不会因为 c1 被收集而进入「已解锁」态**。
- **本波没改**：判据为空 —— 参考资产 `Ultramarines Campaign.json` 的 `Nodes` 是 **`null`**
  （节点由 `CampaignGraph` 在编辑器侧生成，`Graph` 字段是 `[JsonIgnore]`），
  本地拿不到「原版 `UnlockedBy` 该填什么」的直接读数。按铁律 2「没查到就说没查到」，⛔ 不猜。
- 下一步（要有人接）：查 `CampaignGraph` / `CampaignNodeData` 的资产，或找一个带 `Nodes` 的活动 SO 作判据；
  查清后 `_campaign_node` 补 `UnlockedBy` 参数即可（一处改动）。

### 5·3 ⚠️ 顺手发现：**所有事件都还没有 `filter`**（判据报告 §八·1 已记）

本波只补 `OfflineCampaign`（它是**唯一会黑屏**的那条：`ChangeArmy` 无兜底）。
其余事件现状与影响：

| 事件 | 现状 | 为什么现在没炸 | 建议 |
|---|---|---|---|
| `OfflineForge` | `filter` 缺 ⇒ army=0 | `ForgeWindowTab.GetForgeEventByArmy` 有 `First(...)` 兜底 | 补 `filter.army=10` 会更贴原版（军队选择器选项会从 Neutral 变 Ultramarines），**本波未改** |
| `OfflineClassic` / `OfflineSkirmish` / `OfflineDraft` / `PracticeEvent` | 同 | `GameModes` 按 `priority` 选，不看 army | 待查（判据：原版 `RankedBaseData.filter` 有值时该是什么） |
| `OfflineStore*` / `OfflineOfferStarter` | 同 | 商店按 `location`/容器取 | 待查 |
| `OfflineInboxWelcome` | 同 | 收件箱不分阵营 | 待查 |
| `OfflineDailyMission` 等 4 条 | 同 | 任务按 `missionType` 分组 | 待查 |

⛔ 没有「顺手全补」：`filter.army` 一旦填错阵营，比空着更容易造成**静默错分流**
（军队选择器会列出错误选项）。每条要有自己的判据。

---

## 六 出处（复核用）

- 判据报告：`d:/4/Unity/资料/完全复刻/报告/B168_判据_奖励页活动黑屏.md` §二 / §五 / §六 / §八
- `$type` 解析链：`B168_R2_头像边框父活动.md` §3·5（`TypedSerializer.ReadJson` → `ReflectionHelper.GetTypeFromString`）
- `dump.cs`（`d:/2/tools/il2cpp_out/dump.cs`）：`124993/124995`（CampaignEventData）·
  `125811-125841`（EventFilter）· `126492`（LiveOpsEventData，concrete）· `45634-45652`（CardArmy）
- 原版形状：`E:/warpforge/02_解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/Ultramarines Campaign.json`
- 消费链：`cpp2il/out_cpp2il_dev_ilspy/CampaignWindowTab.cs:175/189/600/634` ·
  `CampaignTrack.cs:874-920` · `Missions.cs:713-745` + `:2067`
- 自检脚本与产物：`E:/warpforge/_tmp_view/d3_selftest/`
  （`verify_d3.py` · `run_selftest.py` · `check_json_types.py` · `check_type_names.py` · `check_titledata_types.py` · `type_table*.md`）
