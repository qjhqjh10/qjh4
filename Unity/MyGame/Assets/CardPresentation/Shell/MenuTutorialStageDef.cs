// MenuTutorialStageDef.cs — **菜单侧 FTUE（新手引导）的阶段表**。
//
// ==================== 出处（第一权威 = 原版序列化资产 + 反编译，全部本地现读）====================
// 🔴 **2026-10-19（E01 收尾）就地订正：阶段【顺序】已从原版序列化资产里解出，不再是推断。**
//   原版 `TutorialManager` 的唯一实例 = `level0` 的 `MainTutorialManager` GameObject
//   （comps = Transform 167 / `MainTutorialManager` 333 / `TutorialManager` 433）。
//   ⛔ `assets_full` 的 JSON **读不到**这份数据（level0 没有 type tree，导出器只留了 m_GameObject/m_Enabled/
//   m_Script）—— 本文件头这张表是**直接读原始字节**解出来的（`D:/2/unity_run_ref/Warpforge_Data/level0`，
//   UnityPy + 按字段序手工解；`TutorialManager` 组件 pid 433，`m_Enabled` 后依次是
//   `priority/status`（来自 `InitializableBehaviour`）→ `tutorialPointerMenu`(GUID) →
//   `tutorialMessageWindow`(GUID) → `stages[]` → `tutorialFirstdecks[]` → `persistScriptablesInEditorForDebug`）。
//   · `stages[]` 实读 = **count 5** + 5 个 `{fileID 2, pathID 535..539}`（fileID 2 = `sharedassets0.assets`）。
//   · 5 个 stage SO 的 `m_Name` / `stageID` **逐字实读**：
//     | 数组序 | pid | SO 资产名 | `stageID` | 脚本类 |
//     |---|---|---|---|---|
//     | 1 | 535 | `1_TutorialStageSelectArmy`       | `SelectArmy`           | `TutorialStageSelectArmy` |
//     | 2 | 536 | `2_TutorialStageSelectRanked`     | `PlayRanked`           | `TutorialStagePlayRanked` |
//     | 3 | 537 | `3_TutorialStageGetMission`       | `GetMission`           | `TutorialStageGetMission` |
//     | 4 | 538 | `4_TutorialStageGetCampaignNode`  | `GetCampaignFirstNode` | `TutorialStageGetCampaignNode` |
//     | 5 | 539 | `7_TutorialStageTutorialFinished` | `TutorialFinished`     | `TutorialStageTutorialFinished` |
//     ⇒ **本地这一版原版只挂 5 个 stage，且 `PlayRanked` 是【第 2 个】**（在领任务之前）——
//       旧版报告把它排在 `GetCampaignNode` 之后，已被序列化真值推翻。
//   · `TutorialManager.Initialize` / `_DelayedPLayTutorial_d__13__MoveNext` / `ForceCompleteAll` 三处都**按数组序**
//     遍历（`param_1 + 0x38` = 数组、`+0x18` = count），代码里**没有排序** ⇒ 顺序 = 资产数组序。
// · 🔴 **`LevelUpCard` / `ClaimForgeFirstLevel` 的 SO 在本地原版里【不存在】**：资产名编号是
//   1,2,3,4,**7**（缺 5、6），`sharedassets0` / `resources` / 全部 bundle 里都搜不到 `5_TutorialStage*` /
//   `6_TutorialStage*`（只有 MonoScript 与本地化文案）。⇒ 我们把这两个 def 放在 **5、6 位**：
//   位置依据 = **资产名编号缺口**（5/6 空着、7 = Finished）+ **官方文案键序**（`LevelUpCard*` 在 `GoToForge*` 之前）。
//   **这一小段仍是推断**（⛔ 别写成已证实）；若只要「本地原版那一版」，把这两个 def 从 `All` 删掉即可。
//   保留它们 = 铁律 11（类与文案都在，属于原版有、本地资产缺的那一类）。
//
// · 执行器 = `TutorialManager`（`TutorialManager.cs` + `d:/2/tools/decomp_full/TutorialManager__*.c`）：
//     `Initialize` → 每个 stage `Reset()` + `TutorialV2SaveData.LoadStage()`；
//     `_DelayedPLayTutorial_d__13__MoveNext` 找**第一个 `completed == false` 且 `CheckForCompletion() == false`**
//     的 stage → `currentStage = it` → `Execute()`（只跑一个）；`Complete()` → `completed = true` +
//     `TutorialV2SaveData.SaveStage` + 重启 `DelayedPLayTutorial`（= 从数组头再找下一个）；
//     `WindowChange` 先 `DisablePointer()` 再 `currentStage.WindowsChange(...)`。
//
// · **指针目标**（同一份 SO 里的 `TutorialPointerData`：`id` / `offset` / `filterRaycast`，逐字节实读）：
//   解析 = `TutorialPointerData.get_Target` → `GameObjectUniqueIdentifier.Find(id)` —— **按唯一 id 找，不是按名字**。
//   id ↔ GameObject 本机已解出：
//     `RewardsMenuButton`  → `Main Menu Navigation Button - Rewards`（mainmenuwarpforge 场景 pid 175）
//     `RewardsMenu_MissionsButton` → `MissionsRewardsButton`（menus 包，挂在 Tab Buttons 下）
//     `RewardsMenu_CampaignButton` → `CampaignRewardsButton`
//     `RankedModeMenuButton` → `Skirmish Ranked Game Mode Container 1x2`（offset **(0,−240)**）
//     `PlayRankedButton` → `To Battle Button`（menus 包；⚠️ 这个 id 在 6 个 GO 上都有，是通用 id）
//     `CampaignRewardUnlockBaseReward` → `Unlock Button`（offset **(0,20)**）
//     `HomeMenuButton` → `Main Menu Navigation Button - Home`（场景 pid 48）
//     `CollectMissionTutorial_Challenge_0` = 代码常量（`TutorialStageGetMission.TUTORIAL_STAGE_MISSION_CLAIM_MISSION_BUTTON_ID`）
//   `offset` 的去向（`TutorialManager__AddPointerAction.c` 逐句）：指针根 `SetParent(target.transform)` →
//   `localPosition = offset` ⇒ **指尖 = 目标 pivot + offset（目标本地像素，y 向上）**。
//
// · 指针 prefab（`bundle_generalgamewindows_assets_all/GameObject/TutorialPointer*.json`，逐 prefab 现读）：
//     | prefab | GUID | useLocalPositions | x/yOffset | moveDuration | 谁用 |
//     |---|---|---|---|---|---|
//     | TutorialPointerMenu | `593d3960…fd543` | 1 | (0,0) | 0.0 | ✅ **本链全部 stage**（`TutorialManager.tutorialPointerMenu`，SO 实读就是这个 GUID） |
//     | TutorialPointerWildcards | `06991d70…e4148e` | 1 | (0,0) | 0.0 | `WildcardTutorialManager.tutorialPointerMenu`（`level0` MB 412 实读同一个 GUID） |
//     | TutorialPointerCombat | `fa5ca67b…c3dc21a4` | 0 | (2,−2) | 0.7 | 战斗内（13 个 `battlearena*` 场景各有实例） |
//     | TutorialPointerBase | `85161f5d…f02cae` | 0 | (2,−2) | 0.7 | **无任何序列化引用**（全库只有它自己的 AssetBundle container 条目） |
//   · `TutorialPointerBase` = **prefab 名，不是基类**：4 个 prefab 的组件脚本**全是同一个 `TutorialPointer`**
//     （MonoScript `-1274404763354297598` = `bundle_Waprforge_monoscripts/MonoScript/MonoScript_-1274404763354297598.json`）。
//     Base 的字段与 Combat **逐值相同**、树也相同（`Pointer/light`，无 `Glow`、无气泡）⇒ 它是
//     `generalgamewindows` 包里**与 Combat 同参数的备用/母版 prefab**（很可能是 prefab variant 母体或遗留资产），
//     **运行期无引用**。⛔ 不能把它叫「基类」。
//   · 菜单链**不挂气泡**：`AddPointerAction` 从不调 `SetTutorialText`（`TutorialPointer__SetTutorialText.c`
//     只在 `Wildcards` 那条链上被调用）⇒ 文案走整屏简报（`ShowTutorialMessageWindow` → `TutorialMessageWindowPopUp`）。
//
// · 文案（✅ **官方词条，本地全在**）：`TutorialData/TutorialMessage/*` —— 12 条。
//   来源 = `D:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json` 的 `terms` +
//   `Assets/CardPresentation/Resources/official_loc_13lang.json`。
//   🔴 **不是我们写的** —— 这就是 stage SO 里 `TutorialMessageData.titleKey/messageKey` 指的那批词条；
//   `TutorialFinished` 的两条已从 SO 实读证实（`MainMenu/Welcome/TitleAfterTutorial` / `SubtitleAfterTutorial`）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>7 个 `FlowTutorialStage` 子类。⚠️ **枚举值只用于旧存档位图，不表示顺序** ——
    /// 真正的顺序 = `MenuTutorialStages.All[]`（原版序列化数组序，见文件头）。</summary>
    public enum MenuTutorialStageId
    {
        SelectArmy = 0,
        GetMission = 1,
        GetCampaignNode = 2,
        PlayRanked = 3,
        LevelUpCard = 4,
        ClaimForgeFirstLevel = 5,
        TutorialFinished = 6,
    }

    /// <summary>指针要指向哪一颗控件（**存的是「哪一颗」，不是坐标** —— 目标窗会动）。</summary>
    public enum MenuTutorialTargetKind
    {
        None = 0,
        /// <summary>左竖导航 `Main Menu Navigation Button - Rewards`。</summary>
        NavRewards,
        /// <summary>左竖导航 `Main Menu Navigation Button - Collection`。</summary>
        NavCollection,
        /// <summary>左竖导航 `Main Menu Navigation Button - Home`。</summary>
        NavHome,
        /// <summary>奖励窗左栏 `Missions` 页签（`TabButtons.options` 里 `WindowTabType.Missions` 那一颗）。</summary>
        RewardsTabMissions,
        /// <summary>奖励窗左栏 `Campaign` 页签。</summary>
        RewardsTabCampaign,
        /// <summary>奖励窗左栏 `Forge` 页签。</summary>
        RewardsTabForge,
        /// <summary>主菜单模式卡 `Base Game Mode Container 1x2 - Ranked`。
        /// <para>⚠️ 原版这一颗的 `GameObjectUniqueIdentifier.id` = `RankedModeMenuButton`，挂的 GO 名是
        /// `Skirmish Ranked Game Mode Container 1x2`；我们壳里的对应节点叫
        /// `Base Game Mode Container 1x2 - Ranked`（我们自己建的卡，见 `MainMenuRuntime._placeAllCards`）。</para></summary>
        ModeCardRanked,
        /// <summary>排位窗（`RankedEventWindow`）里的 `To Battle Button` ——
        /// = 原版 `TutorialPointerData.id = PlayRankedButton`（`TutorialStagePlayRanked.playRankedButton`）。</summary>
        RankedPlayButton,
        /// <summary>Missions 页签里任务行的 `Collect` —— 原版第三步指针 id
        /// `CollectMissionTutorial_Challenge_0`（代码常量；`filterRaycast=1`）。
        /// <para>本地等价物 = `MissionsTab.TutorialClaimButton`（第一条可领 / 未领的行）。</para></summary>
        MissionClaimButton,
        /// <summary>Forge 页签里可领格的 `Claim` —— 原版第三步 `ForgeWindowTab.GetForgeItem(...)` 那颗
        /// `Generic UI Button`（`AddPointer(..., filterRaycast=1)`）。</summary>
        ForgeClaimButton,
        /// <summary>战役窗基础列的 `Unlock Button` —— 原版 id `CampaignRewardUnlockBaseReward`，offset (0,20)。</summary>
        CampaignClaimButton,
        /// <summary>战役页（`CampaignWindowTab`）里的**教程轨道节点** —— 原版
        /// `CampaignWindowTab.GetTutorialTrackArmyNode(tab, 2)`（= 节点 id 前缀 + `2`，我们 = `CampaignNode_2`），
        /// `DoMenuActions` 传的是 stage 的 `capmaignNodeCursorOffset` **(40,0)**。</summary>
        CampaignTrackNode,
        /// <summary>收藏窗左栏 `Cards` 页签。</summary>
        CollectionTabCards,
        /// <summary>当前那扇窗的根（兜底：控制件已经关掉时，指向窗本身）。</summary>
        CurrentWindow,
    }

    /// <summary>一个菜单引导阶段。字段全部可选 —— 没有的写 `null` / `None`，⛔ 不编。</summary>
    public sealed class MenuTutorialStageDef
    {
        public MenuTutorialStageId Id;
        /// <summary>原版类名（= 本表最硬的判据；`stageID` 字符串本地取不到）。</summary>
        public string ClassName;
        /// <summary>🔴 **原版 stage SO 里的 `FlowTutorialStage.stageID` 字符串**（`sharedassets0.assets` 逐字节实读）。
        /// <para>= 原版存档 `TutorialV2SaveData.StageSaveData.stageId` 的键；我们的 `PlayerPrefs` 等价物也用它
        /// （见 `MenuTutorialFlow.DoneKey`）。`null` = **本地原版没有这份 SO**（`LevelUpCard`/`ClaimForgeFirstLevel`），
        /// 这时回退到类名当键（**我们的键，不是原版值**）。</para></summary>
        public string StageId;
        /// <summary>指针 tier（`Menu` = `TutorialPointerMenu`；见文件头那张映射表）。</summary>
        public string PointerTier;
        /// <summary>第一颗要指的控件。`None` = 本 stage 不显示指针（原版 `TutorialStageSelectArmy` 就是这一档）。</summary>
        public MenuTutorialTargetKind Target;
        /// <summary>第二颗控件（进窗之后才出现的那一颗）。`None` = 没有。</summary>
        public MenuTutorialTargetKind TargetInWindow;
        /// <summary>🔴 **原版 `TutorialPointerData.offset`**（目标本地像素，**y 向上**；见文件头）。
        /// `= (0,0)` 表示原版这个目标就是 0 偏移。⛔ 不从截图量，只照序列化值。</summary>
        public Vector2 PointerOffset;
        /// <summary>第三颗控件（= 原版 `DoMenuActions` 的**第三步**：`explain…` 简报弹过之后指的那一颗）。
        /// `None` = 没有第三步。判定见 `MenuTutorialFlow.RefreshPointer`。</summary>
        public MenuTutorialTargetKind DeepTarget;
        /// <summary>第三颗控件的原版 offset（同 <see cref="PointerOffset"/> 的坐标系）。</summary>
        public Vector2 DeepOffset;
        /// <summary>第三步的**另一支**（原版同一步在不同上下文指不同件：例如 `GetCampaignNode` 在战役页签里
        /// 指 `GetTutorialTrackArmyNode(tab,2)`、在战役窗里指 `Unlock Button`）。`None` = 没有。</summary>
        public MenuTutorialTargetKind DeepTarget2;
        /// <summary>第二支的 offset。</summary>
        public Vector2 DeepOffset2;
        /// <summary>要在阶段开始时替玩家打开哪扇窗（`None` = 不主动开）。</summary>
        public MenuTutorialActionKind OnEnter;

        /// <summary>整屏简报的标题词条（= 原版 `TutorialMessageData.titleKey`）。`null` = 这一阶段没有简报。</summary>
        public string TitleKey;
        /// <summary>整屏简报的正文词条（= 原版 `TutorialMessageData.messageKey`）。</summary>
        public string BodyKey;
        /// <summary>第二个简报（= 原版 `explain…` 那一份），进窗/换页之后再弹一次。`null` = 没有。</summary>
        public string ExplainTitleKey;
        public string ExplainBodyKey;

        /// <summary>完成条件的判据（写给人看，代码里的实现在 `MenuTutorialFlow.CheckCompletion`）。</summary>
        public string CompletionEvidence;

        public bool HasMessage { get { return !string.IsNullOrEmpty(TitleKey) || !string.IsNullOrEmpty(BodyKey); } }
        public bool HasExplain { get { return !string.IsNullOrEmpty(ExplainTitleKey) || !string.IsNullOrEmpty(ExplainBodyKey); } }
    }

    /// <summary>阶段开始时主动做的那一跳。</summary>
    public enum MenuTutorialActionKind
    {
        None = 0,
        /// <summary>打开 `Choose Army Deck FTUE Window`（= 原版 `TutorialStageSelectArmy.Execute` 那句 `OpenWindow`）。</summary>
        OpenSelectArmyWindow,
    }

    /// <summary>阶段表正本（**顺序 = 原版 `TutorialManager.stages[]` 序列化数组序**，见文件头）。
    /// <para>🔴 1–4 与 7 是**实读真值**；5–6 是**推断位置**（本地原版没有这两份 SO，见文件头）。</para></summary>
    public static class MenuTutorialStages
    {
        public static readonly MenuTutorialStageDef[] All =
        {
            // ===== 1/5（原版 stages[0]，pid 535，SO `1_TutorialStageSelectArmy`）=====
            new MenuTutorialStageDef
            {
                Id = MenuTutorialStageId.SelectArmy, ClassName = "TutorialStageSelectArmy",
                StageId = "SelectArmy",
                PointerTier = null, Target = MenuTutorialTargetKind.None,
                OnEnter = MenuTutorialActionKind.OpenSelectArmyWindow,
                // 原版 SO 实读：只有 `ftueSelectArmyWindow`（ComponentReference，GUID `d89d6a15…`）——
                //   **没有 TutorialPointerData、没有 TutorialMessageData** ⇒ 本阶段既无指针也无简报。
                //   Execute 里 `WindowsManager.OpenWindow(ftueSelectArmyWindow)` 并订阅它的关闭事件。
                TitleKey = null, BodyKey = null,
                CompletionEvidence = "Inventory.DistinctCount > 0 —— 即「起始卡组已入库」",
            },
            // ===== 2/5（原版 stages[1]，pid 536，SO `2_TutorialStageSelectRanked`）=====
            new MenuTutorialStageDef
            {
                Id = MenuTutorialStageId.PlayRanked, ClassName = "TutorialStagePlayRanked",
                StageId = "PlayRanked",
                PointerTier = "Menu", Target = MenuTutorialTargetKind.ModeCardRanked,
                TargetInWindow = MenuTutorialTargetKind.RankedPlayButton,
                // 原版 SO 实读：`enterRankedButton.id = RankedModeMenuButton`（offset **(0,−240)**）+
                // `playRankedButton.id = PlayRankedButton` → GO `To Battle Button`（offset (0,0)）。
                // 类桩没有 `TutorialMessageData` ⇒ 本阶段**没有整屏简报**（12 条文案里也没有排位那一对）。
                // `ShowCursor`：主菜单上指模式卡；进 `RankedEventWindow(V2)` 后指 `To Battle Button`。
                PointerOffset = new Vector2(0f, -240f),
                CompletionEvidence = "PlayerData 排位计数 Sum > 0（**且回到 MainMenuWindow 才置完成**）",
            },
            // ===== 3/5（原版 stages[2]，pid 537，SO `3_TutorialStageGetMission`）=====
            new MenuTutorialStageDef
            {
                Id = MenuTutorialStageId.GetMission, ClassName = "TutorialStageGetMission",
                StageId = "GetMission",
                PointerTier = "Menu", Target = MenuTutorialTargetKind.NavRewards,
                TargetInWindow = MenuTutorialTargetKind.RewardsTabMissions,
                // 原版 SO 实读：`RewardsMenuButton` + `RewardsMenu_MissionsButton`（两者 offset 均 (0,0)、
                //   filterRaycast=1），文案两对（`GoToMissions*` / `CheckMissions*`）逐字实读。
                // ✅ 第三步（2026-10-19 接上）：停在 Missions 页签且第二份简报已弹过之后，
                //   指针换指该任务行的 `Collect` 钮（原版 id = 代码常量 `CollectMissionTutorial_Challenge_0`，
                //   offset (0,0)、filterRaycast=1）；本地等价物 = `MissionsTab.TutorialClaimButton`。
                DeepTarget = MenuTutorialTargetKind.MissionClaimButton,
                TitleKey = "TutorialData/TutorialMessage/GoToMissionsTitle",
                BodyKey = "TutorialData/TutorialMessage/GoToMissionsDescription",
                ExplainTitleKey = "TutorialData/TutorialMessage/CheckMissionsTitle",
                ExplainBodyKey = "TutorialData/TutorialMessage/CheckMissionsDescription",
                // 原版：`HasCampaignPoints()` = 任一战役 `AvailablePoints > 0`（服务器下发），
                //   且**当前窗不是 `RewardWindow`（领奖弹窗）**才 Complete。
                // 🔴 我们这一版战役点固定 9999（用户「资源固定 9999」口径）⇒ 原判据恒真
                //   ⇒ 等价物 = 「真领过一条 Missions 奖励」（原版里战役点正是领任务发的 ⇒ 语义同一件事）。
                CompletionEvidence = "HasCampaignPoints（任一战役 AvailablePoints>0 且不在 RewardWindow 里）—— 我们等价物 = 领过任一 Missions 奖励",
            },
            // ===== 4/5（原版 stages[3]，pid 538，SO `4_TutorialStageGetCampaignNode`）=====
            new MenuTutorialStageDef
            {
                Id = MenuTutorialStageId.GetCampaignNode, ClassName = "TutorialStageGetCampaignNode",
                StageId = "GetCampaignFirstNode",
                PointerTier = "Menu", Target = MenuTutorialTargetKind.NavRewards,
                TargetInWindow = MenuTutorialTargetKind.RewardsTabCampaign,
                // 原版 SO 实读：`RewardsMenuButton` + `RewardsMenu_CampaignButton`（offset (0,0)）
                //   + `CampaignRewardUnlockBaseReward`（offset **(0,20)**），文案两对逐字实读。
                // ✅ 第三步（2026-10-19 接上）：进战役窗之后指基础列 `Unlock Button`。
                //   原版 offset (0,20) 是**从按钮底边量**（按钮 245×45、pivot (0.5,0)）⇒ 相对**中心** =
                //   `20 − 45/2 = −2.5`（y 向上）；我们的节点是中心枢轴，所以 DeepOffset 写 −2.5。
                DeepTarget = MenuTutorialTargetKind.CampaignClaimButton,
                DeepOffset = new Vector2(0f, -2.5f),
                // ✅ 第三步的另一支（2026-10-19 接上）：停在**战役页签**（`CampaignWindowTab`）里时，原版指
                //   `GetTutorialTrackArmyNode(tab, 2)` = 节点 id 前缀 + "2"（我们 = `CampaignNode_2`），
                //   offset = `TutorialStageGetCampaignNode.capmaignNodeCursorOffset` **实读 (40,0)**。
                DeepTarget2 = MenuTutorialTargetKind.CampaignTrackNode,
                DeepOffset2 = new Vector2(40f, 0f),
                TitleKey = "TutorialData/TutorialMessage/CampaignTitle",
                BodyKey = "TutorialData/TutorialMessage/GoToCampaignDescription",
                ExplainTitleKey = "TutorialData/TutorialMessage/CampaignTitle",
                ExplainBodyKey = "TutorialData/TutorialMessage/InCampaignDescription",
                // 原版：`HasTwoNodesCompleted()`（SO 实读 stageID 就是 `GetCampaignFirstNode`）——
                //   谓词 = 某个 List 的 `count > 1`；**且当前窗不是 `CampaignRewardsWindow`/`RewardWindow`**。
                //   `InCampaignDescription` 官方文案写「unlock your **first** reward node」+
                //   `MainTutorialState.CollectedCampaignFirstNode` ⇒ 我们取「领到第一格」。
                //   那 `1 < count` 的解释：原版那个 List 里**根节点本来就算一格**（已领）⇒ 与「第一格」同一件事。
                //   ⚠️ 该解释**仍是推断**（本地没有战役服务器数据可对照）。
                CompletionEvidence = "HasTwoNodesCompleted（count>1，含根节点）/ 官方文案「first reward node」—— 我们等价物 = CampaignData.ClaimedCount >= 1",
            },
            // ===== 5（**推断位**：本地原版没有这份 SO；资产名编号缺 5）=====
            new MenuTutorialStageDef
            {
                Id = MenuTutorialStageId.LevelUpCard, ClassName = "TutorialStageLevelUpCard",
                StageId = null,                       // 本地没有 SO ⇒ 原版 stageID 取不到
                PointerTier = "Menu", Target = MenuTutorialTargetKind.NavCollection,
                TargetInWindow = MenuTutorialTargetKind.CollectionTabCards,
                // 类桩字段：`collectionMenuButton` / `cardsButton` / `selectWarlordButton` / `levelUpCardButton`
                //   + `goToLevelUpCardTexts`。SO 不在本地 ⇒ **id 与文案键名都是按字段名/本地化族对出来的**
                //   （`TutorialData/TutorialMessage/LevelUpCardTitle|Description`），⛔ 不是实读。
                TitleKey = "TutorialData/TutorialMessage/LevelUpCardTitle",
                BodyKey = "TutorialData/TutorialMessage/LevelUpCardDescription",
                // 原版：`HasAlreadyUpgradedCard()` = 卡库里 `CardTier > 1` 的卡存在；
                //   另有 `HasNoCardToUpgrade()`（所有卡都不可升级）⇒ 也 Complete。
                // 🔴 我们这一版 `CardProgress.Level` 缺省 = `MaxLevel`、且唯一的 `SetLevel` 调用点
                //   `DoUpgrade()` 自 2026-10-12 起 `Interactable = false`
                //   ⇒ `CardTier > 1` 永远为假、`HasNoCardToUpgrade` 永远为真（原版口径会**开局即完成**）
                //   ⇒ 等价物 = 打开过卡牌详情窗（玩家能走到的最接近的一步）。
                CompletionEvidence = "Any(card.CardTier > 1) 或 All(不可升级) —— 我们等价物 = 打开过 CardDetailPopup（升级钮当前被停用）",
            },
            // ===== 6（**推断位**：本地原版没有这份 SO；资产名编号缺 6）=====
            new MenuTutorialStageDef
            {
                Id = MenuTutorialStageId.ClaimForgeFirstLevel, ClassName = "TutorialStageClaimForgeFirstLevel",
                StageId = null,                       // 本地没有 SO ⇒ 原版 stageID 取不到
                PointerTier = "Menu", Target = MenuTutorialTargetKind.NavRewards,
                TargetInWindow = MenuTutorialTargetKind.RewardsTabForge,
                // ✅ 第三步（2026-10-19 接上）：停在 Forge 页签且 `forgeHelpTexts` 弹过之后，指针换指
                //   第一格**可领**的 `Claim` 钮（原版 `ForgeWindowTab.GetForgeItem` 那颗）。
                //   ⚠️ 原版那个 AddPointer 的 offset 传的是运行时鼠标位置 ⇒ 我们取 (0,0)，等价实现。
                DeepTarget = MenuTutorialTargetKind.ForgeClaimButton,
                // 类桩字段：`rewardsMenuButton` / `forgeButton` + `goToForgeTexts` / `forgeHelpTexts`。
                // 文案键名按本地化族对出（`GoToForge*` / `InForge*`），⛔ 不是实读。
                TitleKey = "TutorialData/TutorialMessage/GoToForgeTitle",
                BodyKey = "TutorialData/TutorialMessage/GoToForgeDescription",
                ExplainTitleKey = "TutorialData/TutorialMessage/GoToForgeTitle",
                ExplainBodyKey = "TutorialData/TutorialMessage/InForgeDescription",
                // 原版：`HasUsedForge()` = `ForgeHandler.HasAnyArmyAnyRewardCollected()`（且不在 `RewardWindow`）；
                //   另有 `HasNoForgeableRewards()`（所有阵营都没有可领项）⇒ 直接 Complete。
                // ✅ 我们**有等价状态**，但要看**变化**：`ForgeData` 的初值故意造得非零（见 `_forgeAtEnter`）。
                CompletionEvidence = "ForgeHandler.HasAnyArmyAnyRewardCollected（且不在 RewardWindow）—— 等价物 = 本阶段内 ForgeData.LevelOf 涨了",
            },
            // ===== 7/5（原版 stages[4]，pid 539，SO `7_TutorialStageTutorialFinished`）=====
            new MenuTutorialStageDef
            {
                Id = MenuTutorialStageId.TutorialFinished, ClassName = "TutorialStageTutorialFinished",
                StageId = "TutorialFinished",
                PointerTier = "Menu", Target = MenuTutorialTargetKind.NavHome,
                PointerOffset = new Vector2(0f, 0f),
                // ✅ **SO 实读证实**（不再是推断）：`tutorialFinishedTexts.titleKey = MainMenu/Welcome/TitleAfterTutorial`、
                //   `messageKey = MainMenu/Welcome/SubtitleAfterTutorial`；`homeButtonPointer.id = HomeMenuButton`。
                // Execute：若当前不在 MainMenuWindow ⇒ 指 Home 钮 + 排队一次「回主菜单」动作；
                //   若已在主菜单 ⇒ `ShowMainMenu()` + 开这份 finished 简报（`alreadyShown=1`）→ `Complete()`。
                TitleKey = "MainMenu/Welcome/TitleAfterTutorial",
                BodyKey = "MainMenu/Welcome/SubtitleAfterTutorial",
                CompletionEvidence = "SegmentationController.CheckTag(0x14) —— 我们等价物 = 回到主菜单",
            },
        };

        public static int Count { get { return All.Length; } }
        public static MenuTutorialStageDef At(int i)
        {
            if (i < 0 || i >= All.Length) return null;
            return All[i];
        }
    }
}
