// MenuTutorialStageDef.cs — **菜单侧 FTUE（新手引导）的 7 个阶段表**。
//
// ============================ 出处（第一权威 = 反编译，全部本地现读）============================
// · 阶段类（7 个 `FlowTutorialStage` 子类，类桩 + 方法体逐条读过）：
//     `D:/2/Warpforge_code/Scripts/Assembly-CSharp/FlowTutorialStage.cs`（抽象基类：`stageID` / `completed` /
//     `isFUTE` / `TryExecute` / `Execute` / `DoUpdate` / `Complete` / `CheckForCompletion` / `WindowsChange` / `Reset`）
//     `TutorialStageSelectArmy / GetMission / GetCampaignNode / PlayRanked / LevelUpCard /
//      ClaimForgeFirstLevel / TutorialFinished`
//   · 执行器 = `TutorialManager`（`TutorialManager.cs` + `d:/2/tools/decomp_full/TutorialManager__*.c`）：
//       `Initialize` → 每个 stage `Reset()` + `TutorialV2SaveData.LoadStage()`；
//       找**第一个 `completed == false` 且 `CheckForCompletion() == false`** 的 stage → `currentStage = it` → `Execute()`；
//       `Complete()`（`FlowTutorialStage__Complete.c`）→ `completed = true` + `SaveStage` + 重启 `DelayedPLayTutorial`；
//       `Update` 遍历 `stagesToUpdate` 调 `DoUpdate`；`WindowChange` 先 `DisablePointer` 再 `currentStage.WindowsChange(...)`。
//   · 阶段次序：🔴 **本机解不出序列化数组**（`TutorialManager.stages` 在 prefab/SO 里，而本地 dump 里
//     `stageID` / `isFUTE` / `stages` / `persistScriptablesInEditorForDebug` **全库 0 命中**；
//     `level0` 的 `MainTutorialManager` MonoBehaviour **没有 type tree**（只有 m_GameObject/m_Enabled/m_Script））。
//     ⇒ 本表次序 = **推断**，四条判据：
//       ① `MainTutorialManager.MainTutorialState` 枚举次序（`Start=1 → SelectedArmy=2 → ClaimedCampaignPoints=3 →
//          CollectedCampaignFirstNode=4 → ReturnedToPlay=5 → FirstRankedPlayed=6 → CardLeveledUp=7`）；
//       ② `MainTutorialManager.GetState`（`MainTutorialManager__GetState.c`）的判据次序：
//          先 `FirstRankedPlayed`（返回 6）→ `CardLeveledUp`（7）→ `CollectedCampaignFirstNode`（4）→ `ReturnedToPlay`（5）；
//       ③ 每个 stage 的 `CheckForCompletion` 说的是什么（见下逐条）；
//       ④ 🆕 **官方本地化表 `TutorialData/TutorialMessage/*` 的键名按流程排**（12 条，`官方本地化_en+zh-CN.json`
//          与 `Assets/CardPresentation/Resources/official_loc_13lang.json` 两边都在，逐条现读）：
//          `GoToMissions*` → `CheckMissions*` → `GoToCampaign*` → `InCampaign*` → `LevelUpCard*` →
//          `GoToForge*` → `InForge*`。**这条把「任务 → 战役 → 升级卡 → 锻造」四段的先后钉死了**；
//          `PlayRanked`（没有文案）夹在战役与升级卡之间 —— 依据 ① 的枚举里 `FirstRankedPlayed` 在
//          `CardLeveledUp` 之前、`CollectedCampaignFirstNode` 之后。
//     ⛔ **别把本表当成「原版序列化顺序」**——它标的是**推断**，真值要等 `TutorialManager.stages` 的资产解出来。
//
// · 指针 prefab（`bundle_generalgamewindows_assets_all/GameObject/TutorialPointer*.json`，逐 prefab 现读）：
//     | prefab | useLocalPositions | x/yOffset | moveDuration | glow | tutorialText | 谁用 |
//     |---|---|---|---|---|---|---|
//     | TutorialPointerMenu | 1 | (0,0) | 0.0 | 有 | 有 | ✅ **菜单侧全部 stage**（`TutorialManager.tutorialPointerMenu`） |
//     | TutorialPointerWildcards | 1 | (0,0) | 0.0 | 有 | 有 | `WildcardTutorialManager`（过滤器/已拥有那两条，**不是**本链） |
//     | TutorialPointerCombat | 0 | (2,−2) | 0.7 | 无 | 无 | 战斗内（`battlearena*/GameObject/TutorialPointerCombat.json` 有实例） |
//     | TutorialPointerBase | 0 | (2,−2) | 0.7 | 无 | 无 | **未确证**（全库没有序列化字段指向它） |
//   ⇒ **本链 7 个 stage 全用 `TutorialPointerMenu`** —— 判据 = `TutorialManager.AddPointerAction` 里
//     `AssetReferenceTyped<GameObject> tutorialPointerMenu`（字段偏移 +0x28）是**唯一**被加载的 prefab，
//     而 7 个 stage 的 `DoMenuActions` 调的全是 `TutorialManager.AddPointer(...)`。
//   · `TutorialPointer.SetTutorialText(text, showPointer)` = 气泡的正路（`bgTutorialText.SetActive(true)` +
//     `tutorialText.text = text`）。⚠️ **菜单侧那两条 `AddPointer` 重载都不调它**
//     （`TutorialManager__AddPointerAction.c` 里一次都没有）⇒ 原版菜单链**只用手指、不挂气泡**；
//     文案走**整屏简报窗**（`TutorialManager.ShowTutorialMessageWindow` → `TutorialMessageWindowPopUp`）。
//
// · 文案（✅ **官方词条，本地全在**）：`TutorialData/TutorialMessage/*` —— 12 条，逐条在这个文件下面引用。
//   来源 = `D:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json` 的 `terms` +
//   `Assets/CardPresentation/Resources/official_loc_13lang.json`（`keys/en/zh` 三条平行数组，下标同一份）。
//   🔴 **不是我们写的** —— 这就是原版 stage SO 里 `TutorialMessageData.titleKey / messageKey` 指的那批词条
//   （键名 `TutorialData/TutorialMessage/…` 与 `TutorialStage*` 的 `goTo…Texts / explain…` 字段一一对得上）。
//   `TutorialStageTutorialFinished.tutorialFinishedTexts` 的键**没查到**（12 条里没有 finished 那一对）
//   ⇒ 用 `MainMenu/Welcome/TitleAfterTutorial` / `SubtitleAfterTutorial`（= 教程通关后那张欢迎屏，
//   判据：这两条的英文原文就是「War calls for you! / You are now ready to plunge yourself into…」）——
//   **这是推断**，如实标。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>7 个 `FlowTutorialStage` 子类（**次序见文件头的「推断」说明**）。</summary>
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
        /// <summary>主菜单模式卡 `Base Game Mode Container 1x2 - Ranked`。</summary>
        ModeCardRanked,
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
        /// <summary>指针 tier（`Menu` = `TutorialPointerMenu`；见文件头那张映射表）。</summary>
        public string PointerTier;
        /// <summary>第一颗要指的控件。`None` = 本 stage 不显示指针（原版 `TutorialStageSelectArmy` 就是这一档）。</summary>
        public MenuTutorialTargetKind Target;
        /// <summary>第二颗控件（进窗之后才出现的那一颗）。`None` = 没有。</summary>
        public MenuTutorialTargetKind TargetInWindow;
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

    /// <summary>阶段表正本（顺序 = **推断**，判据见文件头）。</summary>
    public static class MenuTutorialStages
    {
        public static readonly MenuTutorialStageDef[] All =
        {
            new MenuTutorialStageDef
            {
                Id = MenuTutorialStageId.SelectArmy, ClassName = "TutorialStageSelectArmy",
                PointerTier = null, Target = MenuTutorialTargetKind.None,
                OnEnter = MenuTutorialActionKind.OpenSelectArmyWindow,
                // 原版 `TutorialStageSelectArmy` **没有 `TutorialMessageData` 字段**（类桩只有
                //   `ftueSelectArmyWindow` + 运行时实例）⇒ 本阶段**没有整屏简报**（`TutorialData/TutorialMessage/*`
                //   12 条里也没有选阵营那一对 —— 与这条吻合）。
                TitleKey = null, BodyKey = null,
                CompletionEvidence = "Inventory.DistinctCount > 0 —— 即「起始卡组已入库」",
            },
            new MenuTutorialStageDef
            {
                Id = MenuTutorialStageId.GetMission, ClassName = "TutorialStageGetMission",
                PointerTier = "Menu", Target = MenuTutorialTargetKind.NavRewards,
                TargetInWindow = MenuTutorialTargetKind.RewardsTabMissions,
                TitleKey = "TutorialData/TutorialMessage/GoToMissionsTitle",
                BodyKey = "TutorialData/TutorialMessage/GoToMissionsDescription",
                ExplainTitleKey = "TutorialData/TutorialMessage/CheckMissionsTitle",
                ExplainBodyKey = "TutorialData/TutorialMessage/CheckMissionsDescription",
                // 原版：`HasCampaignPoints()` = `CampaignHandler` 的任一战役 `AvailablePoints > 0`。
                // 🔴 我们这一版**战役点固定 9999**（`CampaignData.Points = 9999`，用户「资源固定 9999」口径）
                //   ⇒ 原判据**恒真**、阶段会一秒跳过 ⇒ 用「真领过一个任务」作等价物（见 `CheckCompletion`）。
                CompletionEvidence = "HasCampaignPoints —— 我们等价物 = 领过任一 Missions 奖励",
            },
            new MenuTutorialStageDef
            {
                Id = MenuTutorialStageId.GetCampaignNode, ClassName = "TutorialStageGetCampaignNode",
                PointerTier = "Menu", Target = MenuTutorialTargetKind.NavRewards,
                TargetInWindow = MenuTutorialTargetKind.RewardsTabCampaign,
                TitleKey = "TutorialData/TutorialMessage/CampaignTitle",
                BodyKey = "TutorialData/TutorialMessage/GoToCampaignDescription",
                ExplainTitleKey = "TutorialData/TutorialMessage/CampaignTitle",
                ExplainBodyKey = "TutorialData/TutorialMessage/InCampaignDescription",
                // 原版：`HasTwoNodesCompleted()`（`TutorialStageGetCampaignNode__HasTwoNodesCompleted.c`）
                //   谓词是 `1 < <某个 List 的 count>`。🔴 **两处口径打架**，本表按**官方产品文案**取：
                //   `InCampaignDescription` 明写「Let's unlock your **first** reward node!」+
                //   `MainTutorialState.CollectedCampaignFirstNode`（第一格）⇒ 完成 = **领到第一格**。
                //   那 `1 < count` 的解释：原版那个 List 里**根节点本来就算一格**（已领），
                //   所以「> 1」= 根 + 第一格 ⇒ 与「第一格」是同一件事。⚠️ 该解释**是推断**，如实标。
                CompletionEvidence = "HasTwoNodesCompleted（含根节点）/ 官方文案「first reward node」—— 我们等价物 = CampaignData.ClaimedCount >= 1",
            },
            new MenuTutorialStageDef
            {
                Id = MenuTutorialStageId.PlayRanked, ClassName = "TutorialStagePlayRanked",
                PointerTier = "Menu", Target = MenuTutorialTargetKind.ModeCardRanked,
                // 类桩只有 `enterRankedButton` / `playRankedButton` 两个 `TutorialPointerData`，
                // **没有 `TutorialMessageData`** ⇒ 本阶段没有整屏简报（12 条文案里也没有排位那一对）。
                CompletionEvidence = "PlayerData 的排位计数 Sum > 0 —— 我们等价物 = 从排位入口进过战斗场景",
            },
            new MenuTutorialStageDef
            {
                Id = MenuTutorialStageId.LevelUpCard, ClassName = "TutorialStageLevelUpCard",
                PointerTier = "Menu", Target = MenuTutorialTargetKind.NavCollection,
                TargetInWindow = MenuTutorialTargetKind.CollectionTabCards,
                TitleKey = "TutorialData/TutorialMessage/LevelUpCardTitle",
                BodyKey = "TutorialData/TutorialMessage/LevelUpCardDescription",
                // 原版：`HasAlreadyUpgradedCard()` = 卡库里 `CardTier > 1` 的卡存在。
                // 🔴 我们这一版 `CardProgress.Level` 缺省 = `MaxLevel`、且**唯一的 `SetLevel` 调用点
                //   `DoUpgrade()` 自 2026-10-12 起 `Interactable = false`**（判据写在 `Shell/CardDetailPopup.cs` 的
                //   `CardProgress.Level` 注释里）⇒「升过级」这个状态**永远为假** ⇒ 等价物 = 打开过卡牌详情窗。
                CompletionEvidence = "Any(card.CardTier > 1) —— 我们等价物 = 打开过 CardDetailPopup（升级钮当前被停用）",
            },
            new MenuTutorialStageDef
            {
                Id = MenuTutorialStageId.ClaimForgeFirstLevel, ClassName = "TutorialStageClaimForgeFirstLevel",
                PointerTier = "Menu", Target = MenuTutorialTargetKind.NavRewards,
                TargetInWindow = MenuTutorialTargetKind.RewardsTabForge,
                TitleKey = "TutorialData/TutorialMessage/GoToForgeTitle",
                BodyKey = "TutorialData/TutorialMessage/GoToForgeDescription",
                ExplainTitleKey = "TutorialData/TutorialMessage/GoToForgeTitle",
                ExplainBodyKey = "TutorialData/TutorialMessage/InForgeDescription",
                // 原版：`HasUsedForge()` = `ForgeHandler.HasAnyArmyAnyRewardCollected()`；
                //   另有 `HasNoForgeableRewards()`（所有阵营都没有可领项）⇒ 走「跳过后直接 Complete」。
                // ✅ 我们**有等价状态**，但要看**变化**：`ForgeData` 的初值故意造得非零（见 `_forgeAtEnter`）。
                CompletionEvidence = "ForgeHandler.HasAnyArmyAnyRewardCollected —— 等价物 = 本阶段内 ForgeData.LevelOf 涨了",
            },
            new MenuTutorialStageDef
            {
                Id = MenuTutorialStageId.TutorialFinished, ClassName = "TutorialStageTutorialFinished",
                PointerTier = "Menu", Target = MenuTutorialTargetKind.NavHome,
                // 🔴 `tutorialFinishedTexts` 的键**本地没查到**（`TutorialData/TutorialMessage/*` 12 条里没有
                //   finished 那一对）⇒ 用教程通关后的欢迎屏那两条（**推断**，见文件头「文案」最后一段）。
                TitleKey = "MainMenu/Welcome/TitleAfterTutorial",
                BodyKey = "MainMenu/Welcome/SubtitleAfterTutorial",
                CompletionEvidence = "Execute 里直接 Complete()（玩家点了 Home、且已在主菜单）",
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
