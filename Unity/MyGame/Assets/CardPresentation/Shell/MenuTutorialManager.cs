// MenuTutorialManager.cs — 原版 **`MainTutorialManager`**（第二套菜单引导系统）的等价实现。
//
// ==================== 出处（第一权威 = 反编译，逐方法对照）====================
// · 类/字段：`D:/2/Warpforge_code/Scripts/Assembly-CSharp/MainTutorialManager.cs`
//   + `level0` 的 `MainTutorialManager` GameObject（组件 167/333/433；**333 = 本类**，script 349）原始字节实读。
//   · 序列化字段：`tutorialPointerMenu`（GUID `593d…` = **TutorialPointerMenu**，与 E01 同一个 prefab）·
//     5 个 `Vector2` offset（**原始字节实读**：(0,0) / (35,−22) / (85,−12) / (75,−48) / (100,5)）·
//     `welcomeAnouncement`（`1_welcome_announcement`，远端）· `ftueSelectDeckWindow`（GUID `d89d…` = 与 E01 同一扇窗）。
// · 逐方法（原版 → 本文件）：
//   `MainTutorialManager__GetState.c`                 → `GetState()`
//   `MainTutorialManager__ChangedWindowState.c`       → `ChangedWindowState()`
//   `MainTutorialManager__AddPointer.c`               → `AddPointer()`
//   `MainTutorialManager._AddPointerWithDelay_d__22__MoveNext.c` → `Update()` 里的**下一帧**应用
//     （⚠️ 那个协程**没有 WaitForSeconds**，只 `yield return null` ⇒ 延迟 = **一帧**，不是 0.1s）
//   `MainTutorialManager__GetPointerOffset.c`         → `OffsetOf()`
//   `MainTutorialManager__EnterLevelUpCard.c`         → `EnterLevelUpCard()`（本地无活动卡数据 ⇒ 只出声）
//   `MainTutorialManager__SetSelectedArmy{,FromMenu}.c` → `SetSelectedArmy{,FromMenu}()`
//   `MainTutorialManager__ShouldGetInitialDeck.c`     → `ShouldGetInitialDeck()`
//   `MainTutorialManager__HasZeroCampaignPoints.c`    → `HasZeroCampaignPoints()`（等价物，见下）
//   `MainTutorialManager__OnDisable.c`                → `OnDisable` 的退订（本类只订阅 sceneLoaded）
// · `buttonObjNames`（13 条）：原版在 `MainTutorialManager.__ctor.c` 里逐条 `Add(枚举, 字符串字面量)`；
//   字符串地址已用 `il2cpp_out/stringliteral.json` 全部解出（表见 `MainTutorialButton` 的注释）。
//   运行期解析 = `AddPointerWithDelay` 里的 **`GameObject.Find(名字)`**（⚠️ 是**按名字**，不是按 id 组件）。
// · ⚠️ `DAT_1842d1a18`（`ChangedWindowState` 里 `ChangeTab` 的第二参）：**不是枚举常量** ——
//   `script.json` 的 `ScriptMetadataMethod` 解出 = **`Method$GameWindowWithTabs.ChangeTab<CampaignWindowTab>()`**
//   ⇒ 语义 = **切到 Campaign 页签**。⛔ 别再猜它是 1/2/3。
//
// ==================== 与原版的差异（全部是「本地没有服务器数据」的等价物）====================
//   · `SegmentationController.CheckTag(CollectedFirstDeck = 0)` → **E01 的 `SelectArmy` 完成位**
//     （`WfMenuTutorial.Done.SelectArmy`，同一份存档口径；见 `HasFirstDeck()`）。
//   · `HasZeroCampaignPoints()` → `!MenuTutorialFlow.HasAnyClaimedMission()`
//     （我们战役点固定 9999 ⇒ 原判据恒假；原版「有战役点」正是领任务发的）。
//   · 排位计数（服务器）→ 本进程观测「排位窗开过 + 离开主菜单场景」。
//   · 活动卡 `CardTier > 0` → 本进程观测「打开过 `CardDetailPopup`」（升级钮被停用）。
//   · `welcomeAnouncement`（远端公告）→ 主菜单挂上时视为已完成（`_welcomeDone`）。
//   · `EnterLevelUpCard` 的活动卡（服务端）**本地没有** ⇒ 不打开任何伪造的卡，只出声一次。
//   · 指针目标：原版按 `GameObject.Find(名字)`；我们优先用**页签/窗口的本地访问器**
//     （`MissionsTab.TutorialClaimButton` 等），查不到的**出声 + 不画**。
//
// ==================== 残差四条 · 最终口径（2026-10-19 第五十六会话收口）====================
// ① `EnterRankedMenuButton`（原版名 **`Multiplayer Button Text`**）—— **原版（本 build）没有这颗节点** ⇒
//    按**铁律 11 例外①** 关账。判据（本轮逐条重核）：
//      · `d:/2/新解包资源/assets_full`（84 个 bundle 全树）grep 该名 **0 命中**；
//        控制组 `Main Menu Navigation Button - Rewards` **1 命中**（`bundle_scenes_scenes_mainmenuwarpforge/
//        GameObject/`）⇒ 不是「搜法坏了」。
//      · 主菜单场景 `Navigation Panel ▸ Buttons Container` 实读 = **Home / Collection / Shop / Rewards / Social**
//        五颗，**没有 Multiplayer**；`bundle_menus_assets_all` 的 16,768 个 GameObject 里也没有该名。
//      · 原版自己找不到时也走「报错 + 不画」：`MainTutorialManager._AddPointerWithDelay_d__22__MoveNext.c`
//        `GameObject.Find(...) == null` ⇒ `CustomDebug.LogError("Object for adding pointer not found")`（stringliteral `0x4261758`）。
//    ⇒ 我们 `Resolve` 返回 null + 出声一次，**与原文等价**；⛔ 别拿模式卡顶（那不是同一个语义）。
// ② `EnterLevelUpCard` —— 活动卡（`GameModes.GetActiveEvent` 的 `CardTier > 0` 那张）是**服务端数据**，
//    本仓没有 ⇒ **保持空态 + 出声**，⛔ 不造伪造卡（`EnterLevelUpCard` 的日志里写明「缺服务端数据」）。
// ③ `LevelUpButton` —— 原版**只在 `CardDisplayWindow` 刚被打开时**才 `AddPointer(10)`
//    （`MainTutorialManager__EnterLevelUpCard.c`：`CardDisplayWindow.ShowCard(...)` → `AddPointer(10)`；
//    调用点只有 `ChangedWindowState` 的 `CampaignRewardsWindow`/`MainMenuWindow` 的 state 4 两支）。
//    ⇒ 我们的条件 = **`CardDetailPopup` 是顶窗、且 `CurrentState == Open`**（下面的 `Resolve` 现核过）。
//    ⚠️ 补一条实读：本 build 里连原版的 `LevelUpButton` **这个 GameObject 名也不存在**
//    （全量解包 `"m_Name": "LevelUpButton"` 0 命中）；原版卡窗里那颗是
//    `Card Options Panel/Panel/Upgrade Panel/Content/Upgrade`（`Card Displayer Menu For Menu` 树实读）
//    ⇒ 原版这一指在本 build 里同样是**死目标**（`GameObject.Find` 找不到 → `LogError`）。我们的 `UpgradeHit` 是
//    按**语义**对应的等价物，不是按名字。
// ④ `returnedToPlay` —— **原版死字段**：全量反编译里唯一写点 = `MainTutorialManager__ChangedWindowState.c:111`
//    （`*(undefined1 *)(param_1 + 0x75) = 1`），**0 个读点**（`GetState` 读的是 +0x74 = `selectedArmy`）。
//    我们同样**只写不读**（见 `_returnedToPlay` 的注释与 `Editor/MainMenuScene.cs` 的源码级断言）。
//
// 🔴 **默认不启动**：与 E01 同一个用户口径（`MenuTutorialFlow.MenuTutorialRequested`，`AutoStart=false` +
//   `-wfftue`）。改回 `MenuTutorialFlow.AutoStart = true` 即恢复原版「首次进菜单就播」。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardPresentation
{
    /// <summary>原版 `MainTutorialManager.MainTutorialState`（**值逐条照抄**）。</summary>
    public enum MainTutorialState
    {
        NoState = 0,
        Start = 1,
        SelectedArmy = 2,
        ClaimedCampaignPoints = 3,
        CollectedCampaignFirstNode = 4,
        ReturnedToPlay = 5,
        FirstRankedPlayed = 6,
        CardLeveledUp = 7,
    }

    /// <summary>原版 `MainTutorialManager.MainTutorialButtons`（**13 个，值与字符串逐条照抄**；
    /// 字符串 = `__ctor` 里 `Add(枚举, 字面量)` 的那一批，已用 `stringliteral.json` 解出）。</summary>
    public enum MainTutorialButton
    {
        RewardsButton = 0,          // "Main Menu Navigation Button - Rewards"
        MissionsButton = 1,         // "MissionsRewardsButton"
        CampaignButton = 2,         // "CampaignRewardsButton"
        OrkArmyInCampaign = 3,      // "CampaignArmySelectorGoff"
        ClaimMissionButton = 4,     // "CollectMissionTutorial_Challenge_0"
        ClaimDeckNode = 5,          // "CampaignNode" + GetCampaignRedeemNodeId()
        CollectDeckButton = 6,      // "Unlock Button"
        PlayButton = 7,             // "Main Menu Navigation Button - Home"
        EnterRankedMenuButton = 8,  // "Multiplayer Button Text"
        PlayRankedButton = 9,       // "Ranked Play Mode Button"
        LevelUpButton = 10,         // "LevelUpButton"
        ForgeMenu = 11,             // "Forge Menu Button"
        DoForgeButton = 12,         // "DoForgeButton"
    }

    /// <summary>原版 `MainTutorialManager` 的等价实现（第二套菜单引导）。
    /// <para>⚠️ 与 `MenuTutorialFlow`（E01 的 `FlowTutorialStage` 5 阶段链）**并存**、各跑各的；
    /// 两者共用 `MenuTutorialFlow.MenuTutorialRequested` 这一个启动闸。</para></summary>
    public class MenuTutorialManager : MonoBehaviour
    {
        /// <summary>原版 `CampaignWindowTab.LockArmySelectorByArmy` 用的教程阵营 = **Goff**
        /// （判据 = `MainTutorialButtons.OrkArmyInCampaign` 的字符串 `CampaignArmySelectorGoff`）。</summary>
        public const string TutorialArmy = "Goff";

        // ============================================================ 原版 5 个 offset（原始字节实读）
        static readonly Vector2 OffsetDefault = Vector2.zero;                 // tutorialPointerOffset
        static readonly Vector2 OffsetArmyIcon = new Vector2(35f, -22f);      // ...ArmyIcon
        static readonly Vector2 OffsetCollectionMission = new Vector2(85f, -12f); // ...CollectionMission
        static readonly Vector2 OffsetCampaignNode = new Vector2(75f, -48f);  // ...CampaignNode
        static readonly Vector2 OffsetCollectDeck = new Vector2(100f, 5f);    // ...CollectDeck

        // ============================================================ 原版 13 个节点名（`__ctor` 的字符串字面量，逐条实读）
        // 判据 = `MainTutorialManager__.ctor.c` 的 13 个 `Add(枚举, DAT_...)` + `il2cpp_out/stringliteral.json`
        //        地址 → 值（0x42bc920 / 0x4237188 / 0x42a79c0 / 0x42a76d8 / 0x42389d8 / 0x42a78c8 /
        //        0x4265208 / 0x42bc820 / 0x423c578 / 0x42a6d30 / 0x42aacd0 / 0x42cc0a0 / 0x4283050）。
        // 自检断它 = 拿**原版字面量**当期望值，不是拿我们的实现自证（见 `Editor/MainMenuScene.cs`）。
        static readonly string[] OriginalNodeNames =
        {
            "Main Menu Navigation Button - Rewards",   // 0 RewardsButton
            "MissionsRewardsButton",                   // 1 MissionsButton
            "CampaignRewardsButton",                   // 2 CampaignButton
            "CampaignArmySelectorGoff",                // 3 OrkArmyInCampaign
            "CollectMissionTutorial_Challenge_0",      // 4 ClaimMissionButton
            "CampaignNode",                            // 5 ClaimDeckNode（原版再拼 `GetCampaignRedeemNodeId()`）
            "Unlock Button",                           // 6 CollectDeckButton
            "Main Menu Navigation Button - Home",      // 7 PlayButton
            "Multiplayer Button Text",                 // 8 EnterRankedMenuButton（🔴 本 build 无此 GameObject）
            "Ranked Play Mode Button",                 // 9 PlayRankedButton
            "LevelUpButton",                           // 10 LevelUpButton（🔴 本 build 同名 GameObject 也不存在）
            "Forge Menu Button",                       // 11 ForgeMenu
            "DoForgeButton",                           // 12 DoForgeButton
        };

        /// <summary>原版 `buttonObjNames[按钮]` 的**字面量**（= `GameObject.Find` 的入参）。
        /// 自检口：诊断/断言用，⛔ 不参与运行期解析。</summary>
        public static string OriginalNodeName(MainTutorialButton b)
        {
            int i = (int)b;
            return i >= 0 && i < OriginalNodeNames.Length ? OriginalNodeNames[i] : null;
        }

        /// <summary>🆕 2026-10-19：该 id 的目标名在**本地这一版原版**里是否**不存在**（0 命中）。
        /// <para>判据（第五十六会话逐条重核）：`assets_full` **84 个 bundle 全树** grep `"m_Name": "&lt;名字&gt;"`：
        /// `Multiplayer Button Text` **0** · `LevelUpButton` **0**；
        /// 控制组 `Main Menu Navigation Button - Rewards` / `Ranked Play Mode Button` 各 **1**（搜法有效）。
        /// ⇒ 原版自己走到这两颗时也是 `GameObject.Find == null` → `CustomDebug.LogError` + 不画
        /// （`MainTutorialManager._AddPointerWithDelay_d__22__MoveNext.c`）。</para>
        /// <para>⛔ 这不是「我们漏建」：主菜单 `Navigation Panel ▸ Buttons Container` 实读只有
        /// Home / Collection / Shop / Rewards / Social 五颗；原版卡窗里那颗升级钮叫 `Upgrade`
        /// （`Card Displayer Menu For Menu/Card Options Panel/Panel/Upgrade Panel/Content/Upgrade`），不叫 `LevelUpButton`。</para></summary>
        public static bool OriginalNodeMissingInLocalBuild(MainTutorialButton b)
        {
            return b == MainTutorialButton.EnterRankedMenuButton || b == MainTutorialButton.LevelUpButton;
        }

        // ============================================================ 状态（原版字段；静态 = 跨场景存活）
        static MenuTutorialManager _instance;
        static bool _selectedArmy;        // 原版 +0x74
        // 原版 +0x75。🔴 **原版死字段**：唯一写点 = `MainTutorialManager__ChangedWindowState.c:111`
        //   （`MainMenuWindow` 且 `GetState()==CollectedCampaignFirstNode(4)` 那一支）；
        //   全量反编译对该偏移 **0 个读点**（同文件其它方法读的是 +0x74 = `selectedArmy`）。
        //   ⇒ 我们**照抄保留、只写不读**（写完没人消费）—— 源码级断言见 `Editor/MainMenuScene.cs`。
        static bool _returnedToPlay;
        static bool _rankedPlayed;        // 等价物（原版 = PlayerData 排位计数）
        static bool _cardDetailSeen;      // 等价物（原版 = 活动卡 CardTier>0）
        static bool _welcomeDone;         // 等价物（原版 = welcomeAnouncement 远端进度）
        static bool _changedToCampaign;   // 原版 +0x88
        static bool _rankedSeen;          // 本地观测：排位窗开过
        static string _selectedArmyId;
        static readonly HashSet<MainTutorialButton> _missingLogged = new HashSet<MainTutorialButton>();

        MainMenuRuntime _menu;
        TutorialPointerUi _pointer;
        GameWindow _lastTop;
        WindowTabType _lastTab = WindowTabType.None;
        bool _active;
        bool _pendingApply;
        MainTutorialButton _pending;

        // ---- 诊断读口（自检用）
        public static bool IsRunning { get { return _instance != null && _instance._active; } }
        public static string SelectedArmyId { get { return _selectedArmyId; } }
        public static bool RankedPlayed { get { return _rankedPlayed; } }
        public static bool CardDetailSeen { get { return _cardDetailSeen; } }
        public TutorialPointerUi Pointer { get { return _pointer; } }

        // ============================================================ 入口
        /// <summary>主菜单建完之后调一次（`MainMenuRuntime.Build()` 末尾，排在 `MenuTutorialFlow.Attach` 之后）。</summary>
        public static MenuTutorialManager Attach(MainMenuRuntime menu)
        {
            if (!Application.isPlaying) return null;
            if (menu == null) return null;
            if (_instance == null)
            {
                var go = new GameObject("Menu Tutorial Manager");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<MenuTutorialManager>();
            }
            _instance._menu = menu;
            bool on = MenuTutorialFlow.MenuTutorialRequested;
            _instance._active = on;
            if (!on)
            {
                _instance.DestroyPointer();
                return _instance;
            }
            _welcomeDone = true;
            _instance._lastTop = null;
            _instance._lastTab = WindowTabType.None;
            _instance.ChangedWindowState();
            Debug.Log("[MenuTutorialMgr] 原版 `MainTutorialManager` 的等价实现已启动（`-wfftue`）。"
                    + "本地等价物：首次卡组=E01 的 SelectArmy 完成位 · 战役点=领过任务 · 排位/升级卡=本进程观测。");
            return _instance;
        }

        void Awake()
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            _pointer = TutorialPointerUi.Create(transform, TutorialPointerUi.Tier.Menu);
            _pointer.Hide();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene s, LoadSceneMode mode)
        {
            if (!_active) return;
            DestroyPointer();
            _lastTop = null;
            _lastTab = WindowTabType.None;
            // 原版 `GetState` 读服务器排位计数；我们观测「从排位窗那一侧离开主菜单」。
            if (_rankedSeen && s.IsValid() && !string.IsNullOrEmpty(s.name) && s.name != "MainMenu")
            {
                _rankedPlayed = true;
                Debug.Log("[MenuTutorialMgr] 观测到离开主菜单进入场景 `" + s.name + "`（排位窗已开过）"
                        + " ⇒ `FirstRankedPlayed` 的本地等价信号成立");
            }
        }

        void Update()
        {
            if (!_active) return;
            if (_menu == null) return;                 // 跨场景存活；离开主菜单就停手（同 E01 的判据）

            var wm = WindowsManager.Get();
            var top = wm != null ? wm.TopWindow : null;
            var tab = top is GameWindowWithTabs ? ((GameWindowWithTabs)top).CurrentTab : WindowTabType.None;

            // ---- 观测（原版从服务器读的两条，我们只能观测）
            if (top is RankedEventWindow) _rankedSeen = true;
            if (top is CardDetailPopup) _cardDetailSeen = true;

            // ---- 窗口/页签变了 ⇒ 重算指针（原版由 WindowsManager 的窗口事件触发）
            if (top != _lastTop || tab != _lastTab)
            {
                _lastTop = top;
                _lastTab = tab;
                ChangedWindowState();
            }

            // ---- `AddPointerWithDelay` = **下一帧**应用（原版协程只 `yield return null`）
            if (_pendingApply)
            {
                _pendingApply = false;
                ApplyPointer();
            }
            if (_pointer != null) _pointer.Tick(Time.deltaTime);
        }

        // ============================================================ 状态机（= `MainTutorialManager.GetState.c`）
        /// <summary>原版 `MainTutorialManager.GetState()`。**逐句照抄它的分支结构**，
        /// 只把四个服务器判据换成下面的本地等价物（见文件头）。</summary>
        public static MainTutorialState GetState()
        {
            bool hasDeck = HasFirstDeck();                       // 原版 `CheckTag(CollectedFirstDeck = 0)`
            bool pointFlag = !HasZeroCampaignPoints();           // 原版的 bVar8（= 有战役点）
            bool selected = _selectedArmy;
            bool noDeck = !hasDeck;                              // 原版的 bVar7

            if (hasDeck || pointFlag || selected)
            {
                if (_rankedPlayed) return MainTutorialState.FirstRankedPlayed;      // 6
                if (noDeck || pointFlag || !selected)
                {
                    if (pointFlag && noDeck) return MainTutorialState.ClaimedCampaignPoints; // 3
                    if (hasDeck)
                    {
                        if (_cardDetailSeen) return MainTutorialState.CardLeveledUp; // 7
                    }
                    if (CampaignData.ClaimedCount >= 1) return MainTutorialState.CollectedCampaignFirstNode; // 4
                    return MainTutorialState.ReturnedToPlay;                        // 5
                }
                return MainTutorialState.SelectedArmy;                              // 2
            }
            return _welcomeDone ? MainTutorialState.Start : MainTutorialState.NoState; // 1 / 0
        }

        /// <summary>原版 `HasZeroCampaignPoints()`：所有战役 `AvailablePoints == 0`。
        /// 🔴 我们战役点固定 9999 ⇒ 原判据恒假 ⇒ 等价物 = **没领过任何任务**（见文件头）。</summary>
        static bool HasZeroCampaignPoints() { return !MenuTutorialFlow.HasAnyClaimedMission(); }

        /// <summary>原版 `ShouldGetInitialDeck()` = `!SegmentationController.CheckTag(CollectedFirstDeck=0)`。</summary>
        public static bool ShouldGetInitialDeck() { return !HasFirstDeck(); }

        /// <summary>原版 `PlayerTag.CollectedFirstDeck = 0` 的本地等价物：E01 `SelectArmy` 阶段完成位。
        /// 键与 E01 共用 `WfMenuTutorial.Done.<stageID>`（⛔ 别另开一份存档）。</summary>
        static bool HasFirstDeck()
        {
            for (int i = 0; i < MenuTutorialStages.Count; i++)
            {
                var d = MenuTutorialStages.At(i);
                if (d == null || d.Id != MenuTutorialStageId.SelectArmy) continue;
                if (string.IsNullOrEmpty(d.StageId)) return false;
                return MenuTutorialFlow.IsStageCompleted(d);   // 完成位 2026-10-29 起住在 `DemoData`（旧 Prefs 兜底）
            }
            return false;
        }

        /// <summary>= 原版 `SetSelectedArmy(int army)`（`selectedArmy=true` + `selectedArmyInForge=army`）。</summary>
        public static void SetSelectedArmy(string army)
        {
            _selectedArmy = true;
            _selectedArmyId = army;
        }

        /// <summary>= 原版 `SetSelectedArmyFromMenu(int army)`：置标志 + 立刻重算指针。</summary>
        public static void SetSelectedArmyFromMenu(string army)
        {
            SetSelectedArmy(army);
            if (_instance != null) _instance.ChangedWindowState();
        }

        /// <summary>自检/调试口：清掉本进程观测（**不动** `PlayerPrefs` / E01 的进度）。</summary>
        public static void ResetObservations()
        {
            _selectedArmy = false; _selectedArmyId = null; _returnedToPlay = false;
            _rankedPlayed = false; _rankedSeen = false; _cardDetailSeen = false;
            _changedToCampaign = false; _missingLogged.Clear();
            if (_instance != null) _instance.DestroyPointer();
        }

        // ============================================================ 指针驱动（= `ChangedWindowState.c`）
        /// <summary>原版 `MainTutorialManager.ChangedWindowState()`：按 **窗口类型 × GetState()** 选一颗按钮挂指针。
        /// 结构逐句照抄；类型映射：`MainMenuRewardsWindow`→`RewardsWindow` ·
        /// `CampaignRewardsWindow`→`CampaignRewardWindow` · `MainMenuWindow`→`MainMenuWindow` ·
        /// `ShopWindow`→`ShopWindow` · `CollectionScreen`→`CollectionWindow` · `RankedEventWindow`→`RankedEventWindow`。</summary>
        void ChangedWindowState()
        {
            DestroyPointer();
            var state = GetState();
            var top = TopWindow();
            if (top == null) return;
            var tabs = top as GameWindowWithTabs;
            var tab = tabs != null ? tabs.CurrentTab : WindowTabType.None;

            if (!(top is RewardsWindow))
            {
                if (top is CampaignRewardWindow)
                {
                    if (state == MainTutorialState.ClaimedCampaignPoints) { AddPointer(MainTutorialButton.CollectDeckButton); return; }
                    if (state == MainTutorialState.CollectedCampaignFirstNode) { EnterLevelUpCard(); return; }
                    return;
                }
                if (!(top is MainMenuWindow) && !(top is ShopWindow) && !(top is CollectionWindow))
                {
                    if (top is RankedEventWindow && state == MainTutorialState.SelectedArmy)
                    {
                        AddPointer(MainTutorialButton.PlayRankedButton);
                    }
                    return;
                }
                _changedToCampaign = false;
                if (!(top is MainMenuWindow))
                {
                    // ShopWindow / CollectionScreen：原版只有 state 1/2/3 才指回 Rewards；0 与 >3 不画。
                    if (state > MainTutorialState.CollectedCampaignFirstNode || state == MainTutorialState.NoState) return;
                    AddPointer(MainTutorialButton.RewardsButton);
                    return;
                }
                // ---- MainMenuWindow
                if (state == MainTutorialState.CollectedCampaignFirstNode)
                {
                    _returnedToPlay = true;
                    EnterLevelUpCard();
                    return;
                }
                if (state == MainTutorialState.Start)
                {
                    // 原版：`WindowsManager.OpenWindow(ftueSelectDeckWindow)`（= E01 的 Choose Army Deck FTUE 窗）。
                    WindowsManager.OpenChooseArmyDeck();
                    return;
                }
                if (state == MainTutorialState.FirstRankedPlayed) { AddPointer(MainTutorialButton.RewardsButton); return; }
                if (state == MainTutorialState.SelectedArmy) { AddPointer(MainTutorialButton.EnterRankedMenuButton); return; }
                if (state > MainTutorialState.CollectedCampaignFirstNode || state == MainTutorialState.NoState) return;
                AddPointer(MainTutorialButton.RewardsButton);
                return;
            }

            // ---- 主奖励窗（原版 `MainMenuRewardsWindow`）
            if (state == MainTutorialState.Start)
            {
                if (tab != WindowTabType.Campaign)
                {
                    if (_changedToCampaign) return;
                    _changedToCampaign = true;
                    // `DAT_1842d1a18` = `ChangeTab<CampaignWindowTab>()`（不是枚举常量，见文件头）
                    tabs.ChangeTab(WindowTabType.Campaign);
                    return;
                }
                // 战役页签：原版先 `LockArmySelectorByArmy(CurrentArmy)`；state==1 之后**不挂指针**。
                LockCampaignArmySelector();
                return;
            }

            if (tab == WindowTabType.Campaign)
            {
                LockCampaignArmySelector();     // 原版这一句对**所有** state 都执行
                if (state == MainTutorialState.ClaimedCampaignPoints) { AddPointer(MainTutorialButton.ClaimDeckNode); return; }
                if (state == MainTutorialState.SelectedArmy) { AddPointer(MainTutorialButton.MissionsButton); return; }
            }
            else if (tab == WindowTabType.Missions)
            {
                if (state == MainTutorialState.Start || state == MainTutorialState.ClaimedCampaignPoints)
                {
                    AddPointer(MainTutorialButton.CampaignButton);
                    return;
                }
                if (state == MainTutorialState.SelectedArmy) { AddPointer(MainTutorialButton.ClaimMissionButton); return; }
            }
            else if (tab == WindowTabType.Forge)
            {
                if (state == MainTutorialState.SelectedArmy) { AddPointer(MainTutorialButton.MissionsButton); return; }
                if (state < MainTutorialState.CollectedCampaignFirstNode) { AddPointer(MainTutorialButton.CampaignButton); return; }
            }
            if (state == MainTutorialState.CollectedCampaignFirstNode) { AddPointer(MainTutorialButton.PlayButton); return; }
        }

        /// <summary>= 原版 `CampaignWindowTab.LockArmySelectorByArmy(CurrentArmy)`：只让教程阵营（Goff）可点。</summary>
        static void LockCampaignArmySelector()
        {
            var tab = MenuTutorialFlow.TabOfType<CampaignTab>(TopWindow());
            if (tab != null) tab.LockArmySelector(TutorialArmy);
        }

        /// <summary>原版 `MainTutorialManager.EnterLevelUpCard()`：取活动 `GameModes` 卡（服务端）→
        /// `CardDisplayWindow.ShowCard` → `AddPointer(LevelUpButton)`。
        /// 🔴 本地没有 live-ops 活动卡 ⇒ **不打开伪造的卡**，如实出声（判据在远端）。
        /// 残差②的最终口径：**保持空态 + 出声**，⛔ 不造伪造卡（见文件头）。</summary>
        static void EnterLevelUpCard()
        {
            _lastLevelUpNote = "缺服务端数据：原版取活动 `GameModes` 卡（`CardTier > 0`）→ "
                             + "`CardDisplayWindow.ShowCard(...)` → `AddPointer(LevelUpButton)`；"
                             + "本地没有活动卡 ⇒ **不打开任何伪造卡、也不挂指针**（判据在远端）。";
            if (_missingLogged.Add(MainTutorialButton.LevelUpButton))
                Debug.LogWarning("[MenuTutorialMgr] `EnterLevelUpCard`：" + _lastLevelUpNote);
        }

        /// <summary>最近一次 `EnterLevelUpCard` 给出的结论（自检读；诊断用）。</summary>
        public static string LastLevelUpCardNote { get { return _lastLevelUpNote; } }
        static string _lastLevelUpNote;

        /// <summary>自检口：跑一次 `EnterLevelUpCard` 并返回它给出的结论。
        /// ⚠️ 它**不打开任何窗、不改观测状态**（实现只写一行结论 + 出声一次）—— 断言用它钉「缺数据时不伪造」。</summary>
        public static string EnterLevelUpCardForTest()
        {
            EnterLevelUpCard();
            return _lastLevelUpNote;
        }

        // ============================================================ 指针（= `AddPointer.c` + `AddPointerWithDelay`）
        /// <summary>原版 `AddPointer(button)`：起协程，**下一帧**解析目标并挂指。</summary>
        static void AddPointer(MainTutorialButton button)
        {
            if (_instance == null) return;
            _instance._pending = button;
            _instance._pendingApply = true;
        }

        void ApplyPointer()
        {
            DestroyPointer();
            var t = Resolve(_pending);
            if (t == null)
            {
                if (_missingLogged.Add(_pending))
                {
                    // ① 原版节点名在本地这一版**根本不存在** ⇒ 原版走到这里同样是 `GameObject.Find` 失败 →
                    //    `CustomDebug.LogError("Object for adding pointer not found")` + 不画。
                    //    我们照抄「出声 + 不画」，并把「原版无」写进出声里（铁律 11 例外①：不是我们漏建）。
                    if (_pending == MainTutorialButton.EnterRankedMenuButton)
                        Debug.LogWarning("[MenuTutorialMgr] 指针 `EnterRankedMenuButton`（原版名 `"
                                + OriginalNodeName(_pending) + "`）—— 🔴 **原版（本 build）没有这颗节点**"
                                + "（`assets_full` 84 个 bundle 全树 0 命中，按铁律 11 例外①关账）⇒ **不画**。"
                                + "⛔ 别拿模式卡 / `Social` 钮顶。");
                    else if (_pending == MainTutorialButton.LevelUpButton)
                        Debug.LogWarning("[MenuTutorialMgr] 指针 `LevelUpButton` —— 原版在 `CardDisplayWindow` 打开后指它；"
                                + "本地这一版**没有** `LevelUpButton` 这个 GameObject（0 命中），语义等价物 = "
                                + "打开着的 `CardDetailPopup.UpgradeHit` ⇒ 当前没有打开的详情窗 ⇒ **不画**"
                                + "（活动卡是服务端数据，`EnterLevelUpCard` 不会代为打开 —— 见残差②）。");
                    else
                        Debug.LogWarning("[MenuTutorialMgr] 指针目标找不到：`" + _pending + "` ⇒ **这一颗不画**。"
                                + "（原版是 `GameObject.Find(buttonObjNames[..])`；本地没有对应节点或不在当前窗/页签）");
                }
                return;
            }
            var p = MenuTutorialFlow.CenterPx(t);
            var off = OffsetOf(_pending);
            // 原版 `Transform.localPosition = offset`（目标本地像素、y 向上）⇒ 我们的设计像素 y 向下取反。
            if (off != Vector2.zero) p += new Vector2(off.x, -off.y);
            _pointer.Show(p, null, true);
        }

        /// <summary>原版 `GetPointerOffset(MainTutorialButtons)`：**5 个 offset、其余走 default (0,0)**（逐条实读）。</summary>
        static Vector2 OffsetOf(MainTutorialButton b)
        {
            switch (b)
            {
                case MainTutorialButton.OrkArmyInCampaign: return OffsetArmyIcon;
                case MainTutorialButton.ClaimMissionButton: return OffsetCollectionMission;
                case MainTutorialButton.ClaimDeckNode: return OffsetCampaignNode;
                case MainTutorialButton.CollectDeckButton: return OffsetCollectDeck;
                default: return OffsetDefault;
            }
        }

        void DestroyPointer()
        {
            _pendingApply = false;
            if (_pointer != null) _pointer.Hide();
        }

        static GameWindow TopWindow()
        {
            var wm = WindowsManager.Get();
            return wm != null ? wm.TopWindow : null;
        }

        /// <summary>13 个 id → 本地节点。原版是 `GameObject.Find(名字)`；我们优先用各窗/页的访问器（见文件头）。
        /// ⛔ 查不到就返回 null（调用点出声一次）—— 不拿近似的钮顶。</summary>
        static Transform Resolve(MainTutorialButton b)
        {
            var top = TopWindow();
            switch (b)
            {
                case MainTutorialButton.RewardsButton: return FindScene("Main Menu Navigation Button - Rewards");
                case MainTutorialButton.PlayButton: return FindScene("Main Menu Navigation Button - Home");

                case MainTutorialButton.MissionsButton: return MenuTutorialFlow.TabButton(top, WindowTabType.Missions);
                case MainTutorialButton.CampaignButton: return MenuTutorialFlow.TabButton(top, WindowTabType.Campaign);
                case MainTutorialButton.ForgeMenu: return MenuTutorialFlow.TabButton(top, WindowTabType.Forge);

                case MainTutorialButton.OrkArmyInCampaign:
                {
                    var tab = MenuTutorialFlow.TabOfType<CampaignTab>(top);
                    return tab != null ? tab.TutorialArmyItem : null;
                }
                case MainTutorialButton.ClaimMissionButton:
                {
                    var tab = MenuTutorialFlow.TabOfType<MissionsTab>(top);
                    return tab != null ? tab.TutorialClaimButton : null;
                }
                case MainTutorialButton.ClaimDeckNode:
                {
                    // 原版 = `GameObject.Find("CampaignNode" + GetCampaignRedeemNodeId())`（第一个可领节点）。
                    var tab = MenuTutorialFlow.TabOfType<CampaignTab>(top);
                    return tab != null ? tab.TutorialRedeemNode : null;
                }
                case MainTutorialButton.CollectDeckButton:
                {
                    var w = top as CampaignRewardWindow;
                    return w != null ? w.TutorialClaimButton : null;
                }
                case MainTutorialButton.EnterRankedMenuButton:
                    // 原版 id 8 = `Multiplayer Button Text` —— 🔴 **原版（本 build）没有这颗节点**
                    //   （`assets_full` 84 个 bundle 全树 0 命中；主菜单导航栏实读 = Home/Collection/Shop/Rewards/Social）。
                    //   ⇒ 按铁律 11 例外①**关账**：这里**永远返回 null**（调用点出声一次、不画）。
                    //   ⛔ 别拿模式卡 / `Social` 钮顶 —— 那不是同一个语义（见文件头 ①）。
                    return null;
                case MainTutorialButton.PlayRankedButton:
                    // 原版 id `Ranked Play Mode Button` —— 本地排位窗的等价物 = `To Battle Button`。
                    return top is RankedEventWindow ? MenuTutorialFlow.FindUnder(top.transform, "To Battle Button") : null;
                case MainTutorialButton.LevelUpButton:
                {
                    // 原版 id 10 `LevelUpButton` —— 原版**只在 `CardDisplayWindow` 刚被 `EnterLevelUpCard` 打开时**
                    //   才 `AddPointer(10)`（`MainTutorialManager__EnterLevelUpCard.c`），且 `GameObject.Find` 找的是
                    //   卡窗里的升级钮（本 build 的实名 = `Card Options Panel/…/Upgrade`；`LevelUpButton` 这个名字 0 命中）。
                    //   我们的等价物 = `CardDetailPopup.UpgradeHit` ⇒ 条件同时要求：
                    //     ① 它**是顶窗**（被别的窗盖住时不该画到别人身上）；② `CurrentState == Open`
                    //     （`Hide()`/`ToBackground()` 之后 `Find` 仍能摸到子树 —— 只判 `top as` 不够）。
                    var cd = top as CardDetailPopup;
                    return cd != null && cd.IsOpen() ? cd.UpgradeHit : null;
                }
                case MainTutorialButton.DoForgeButton:
                {
                    // 原版 id `DoForgeButton` —— 本地等价物 = Forge 页第一格可领钮。
                    var tab = MenuTutorialFlow.TabOfType<ForgeTab>(top);
                    return tab != null ? tab.TutorialClaimButton : null;
                }
            }
            return null;
        }

        /// <summary>🆕 2026-10-19（第五十六会话）：把 `Resolve` 暴露成**只读自检口**（⛔ 不改生产行为）。
        /// 断言用它钉两条：「`EnterRankedMenuButton` 恒 null」·「`LevelUpButton` 只在详情窗**打开**时可指」。
        /// ⚠️ 它读的是**当前顶窗**（`WindowsManager.TopWindow`）—— 自检里要自己开/关窗来验两种状态。</summary>
        public static Transform ResolveTargetForTest(MainTutorialButton b) { return Resolve(b); }

        static Transform FindScene(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var go = GameObject.Find(name);
            return go != null ? go.transform : null;
        }
    }
}
