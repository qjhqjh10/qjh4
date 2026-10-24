// MenuTutorialFlow.cs — **菜单侧新手引导（FTUE）状态机**：`FlowTutorialStage` 链 + 指针 + 聚光灯。
// ⚠️ 原版有 **7 个 `FlowTutorialStage` 子类**，但**本地这一版原版的 `stages[]` 只挂 5 个**
//    （`LevelUpCard` / `ClaimForgeFirstLevel` 的 SO 本地不存在；顺序与证据 → `MenuTutorialStageDef.cs` 文件头）。
//
// ============================ 出处（第一权威 = 反编译，全部本地现读）============================
// · 执行器 = 原版 `TutorialManager`（`SingletonInitializableBehaviour<TutorialManager>`）：
//     `TutorialManager__Initialize.c`：
//       ① 每个 stage `Object.Instantiate`（编辑器调试档除外）→ `Reset()`
//          （`FlowTutorialStage__Reset.c`：`completed = 0` + `ResetTutorialMessageData()`）
//       ② `FlowTutorialStage[] stages` 从存档 `TutorialV2SaveData.LoadAllStages()` 回填 `completed`
//       ③ 若 `SegmentationController.CheckTag()` 为真 ⇒ 从**第一个未完成**的 stage 起把之后全部标 `completed`
//       ④ 找**第一个** `completed == false` 的 stage ⇒ `tutorialEnabled = true` + 订阅
//          `SceneTransitionStarted` / `WindowChange` / `MenuActions`；否则 `enabled = false`（整条链没开始）
//     `TutorialManager._DelayedPLayTutorial_d__13__MoveNext.c`（`PlayTutorial` 起的协程）：
//       `DisablePointer()` → 逐 stage：**`completed == false` 且 `CheckForCompletion() == false`** ⇒
//       `MainTutorialManager.currentStage = it` → `Execute()` → **停**（只跑一个阶段）。
//     `FlowTutorialStage__TryExecute.c`：`completed == false && !CheckForCompletion()` ⇒
//       `currentStage = this` + `Execute()` + 返回 true。
//     `FlowTutorialStage__Complete.c`：`completed = true` → `TutorialV2SaveData.SaveStage(this)` →
//       `StopAllCoroutines` → 重启 `DelayedPLayTutorial`（= **推进到下一个阶段**）。
//     `FlowTutorialStage__TryForceComplete.c`：`isFUTE` 为真时置 `completed = true`（**跳过**）。
//     `TutorialManager__Update.c`：遍历 `stagesToUpdate` 调 `DoUpdate()`（基类空实现）。
//     `TutorialManager__WindowChange.c`：`DisablePointer()` + `currentStage.WindowsChange(isTab, window, tab)`。
//     `FlowTutorialStage__WindowsChange.c`：把 `currentWindow / currentTab` 记下来（基类只做这一件事）。
//   每个 stage 的 `WindowsChange` 会先判「当前窗是不是本阶段已经处理过的那扇」——
//     是 ⇒ 直接 return；不是 ⇒ `if (!CheckForCompletion()) DoMenuActions()`（= **按当前窗重新决定指针**）。
//   `DoMenuActions` 的形状（7 个 stage 一致）：
//     ① 若本阶段的 `TutorialMessageData.alreadyShown == false` ⇒ `ShowTutorialMessageWindow(data)`（整屏简报）
//     ② 否则按「当前窗/页签」选一颗 `TutorialPointerData` ⇒ `TutorialManager.AddPointer(data)`
//        （`AddPointerAction`：清旧指针 → `Instantiate(tutorialPointerMenu)` → 挂到目标下 → `StartPointer`）
//
// · 指针 prefab 映射 / 实读参数 / 气泡几何 → **`TutorialPointerUi.cs`** 文件头（⛔ 不在这里抄第二份）。
// · 聚光灯参数 → **`MenuTutorialHighlight.cs`** 文件头。
// · 🔴 **阶段次序 = 原版 `TutorialManager.stages[]` 序列化数组序**（2026-10-19 从 `level0` 原始字节解出，
//   不再是推断）：`SelectArmy → PlayRanked → GetMission → GetCampaignNode → (LevelUpCard → ClaimForgeFirstLevel
//   ※ 本地原版没有这两份 SO，位置是推断) → TutorialFinished`。逐条证据 → `MenuTutorialStageDef.cs` 文件头。
// · 进度存档：原版是 `TutorialV2SaveData.StageSaveData { string stageId; bool isCompleted; }` 数组，
//   随 `PlayerDataManager.UploadTutorialV2SaveData` 上传到**远端 profile**（`TutorialV2SaveData__SaveStage.c`）。
//   本仓没有后端 ⇒ **等价物 = `PlayerPrefs` 里「每个 stageId 一个完成位」**（键 = `WfMenuTutorial.Done.<stageID>`），
//   **形状照原版**（原版本来就是「按 stageId 记完成」，不是「记一个当前下标」）。
//   🔴 存储位置不是原版的 —— 必须出声（见 `StartFlow` 的 `Debug.Log`）。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CardPresentation
{
    /// <summary>菜单侧 FTUE 状态机（原版 `TutorialManager` + `FlowTutorialStage` 链）。
    /// <para>只在**运行时**挂（`Attach` 里判 `Application.isPlaying`）—— 自检树一个节点都不多。</para></summary>
    public class MenuTutorialFlow : MonoBehaviour
    {
        // ============================================================ 存档键
        // 🔴 **形状照原版 `TutorialV2SaveData.StageSaveData { stageId, isCompleted }`**：
        //   每个 stage **按 stageId 记一个完成位**。原版把它上传到远端 profile；我们落 `PlayerPrefs`
        //   （= 本地等价物，见文件头）。键里用**原版 stageID 字符串**（`MenuTutorialStageDef.StageId`，逐字节实读）；
        //   本地没有 SO 的两个 stage 没有原版 stageID ⇒ 用 `#<类名>` 当键（**我们的键，不是原版值**）。
        public const string PrefDonePrefix = "WfMenuTutorial.Done.";
        public const string PrefSkip = "WfMenuTutorial.Skipped";     // 1 = 玩家/调试口整链跳过（我们自己的标记）
        /// <summary>v1 的存档键（按「当前下标 + 位图」存）。**只用于 `ResetProgress` 清旧档**，⛔ 别再写。</summary>
        public const string PrefStageLegacy = "WfMenuTutorial.Stage";
        /// <summary>v1 的存档键（位图）。**只用于 `ResetProgress` 清旧档**，⛔ 别再写。</summary>
        public const string PrefDoneLegacy = "WfMenuTutorial.DoneMask";

        /// <summary>某一阶段的完成位键（= 原版 `StageSaveData.stageId` 那一格）。</summary>
        static string DoneKey(MenuTutorialStageDef d)
        {
            if (d == null) return null;
            return PrefDonePrefix + (string.IsNullOrEmpty(d.StageId) ? "#" + d.ClassName : d.StageId);
        }

        static bool IsCompleted(int i)
        {
            var d = MenuTutorialStages.At(i);
            var k = DoneKey(d);
            return k != null && PlayerPrefs.GetInt(k, 0) != 0;
        }

        static void MarkCompleted(int i)
        {
            var k = DoneKey(MenuTutorialStages.At(i));
            if (k == null) return;
            PlayerPrefs.SetInt(k, 1);
            PlayerPrefs.Save();
        }

        /// <summary>轮询间隔（原版靠事件；我们这套没有 `WindowsManager` 的窗变更事件 ⇒ 每 0.2s 比一次）。</summary>
        public const float PollInterval = 0.2f;

        /// <summary>🆕 **2026-10-24（真机 F17）：把某一阶段**当场**标成完成（= 原版 `OnSuccess` 里那句
        /// `AddTag(CollectedFirstDeck)` 的本地等价物）。
        /// <para>🔴 **为什么必须在【发放那一刻】写**：状态机自己那条完成链是**每 0.2s 轮询**的
        /// （`Tick` → `CheckCompletion(SelectArmy)`），而 `MenuTutorialManager.Update` 是**每帧**按
        /// 「最上面那扇窗变了」重算 `GetState()` 的。FTUE 窗 `Continue` ⇒ `Close()` 那一拍，
        /// 窗先没、完成位还没写 ⇒ `GetState()` 仍返回 `Start(1)` ⇒ `ChangedWindowState` 里的
        /// **`state == Start ⇒ WindowsManager.OpenChooseArmyDeck()`** 立刻把窗**重新开一遍**
        /// （= 真机 F17 的「点继续后窗被重建、回到选军页」，选择状态当然也没了）。
        /// 原版不存在这个竞态：`AddTag` 与 `CloseWindow` 是同一个回调里的先后两句。</para>
        /// <para>⚠️ **只在运行时写**（`Application.isPlaying`）—— 编辑器/批处理自检不该动玩家的教程存档
        /// （同 `Attach` 那条 `Application.isPlaying` 门槛的口径）。</para>
        /// <para>⚠️ 不改运行中状态机的下标：它自己下一拍 `CheckCompletion` 照样会 `Advance()`（`MarkCompleted` 幂等）。</para></summary>
        public static bool MarkStageDone(MenuTutorialStageId id)
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[MenuTutorial] `MarkStageDone(" + id + ")`：编辑器/批处理里**不写**教程进度（自检不动玩家存档）");
                return false;
            }
            for (int i = 0; i < MenuTutorialStages.Count; i++)
            {
                var d = MenuTutorialStages.At(i);
                if (d == null || d.Id != id) continue;
                MarkCompleted(i);
                Debug.Log("[MenuTutorial] 阶段完成位已写：`" + (d.StageId ?? d.ClassName) + "`"
                        + "（= 原版 `AddTag` 的本地等价物；调用点 = 发放 / 领取那一刻，不是轮询）");
                return true;
            }
            Debug.LogWarning("[MenuTutorial] `MarkStageDone(" + id + ")`：阶段表里没有这一条 ⇒ 没写");
            return false;
        }

        static MenuTutorialFlow _instance;
        public static MenuTutorialFlow Instance { get { return _instance; } }

        MainMenuRuntime _menu;
        TutorialPointerUi _pointer;
        MenuTutorialHighlight _highlight;

        int _index = -1;
        float _poll;
        bool _active;
        bool _wantMessage;        // 本阶段那条整屏简报还没弹（延到第一帧再弹，见 Enter 的注释）
        bool _explainShown;       // 本阶段的第二个简报（原版 `explain…`）已经弹过
        int _messageShownStage = -1;   // 哪一阶段的**第一条**简报已经弹过（= 原版 `TutorialMessageData.alreadyShown`）
        int _explainShownStage = -1;   // 哪一阶段的**第二条**简报已经弹过

        GameWindow _lastTop;
        WindowTabType _lastTab = WindowTabType.None;
        Vector2 _lastPoint = new Vector2(-1f, -1f);

        // 阶段内的观测状态（原版对应的是服务器字段，我们这些是本地等价物 —— 逐条见 CheckCompletion）
        FTUESelectDeckWindow _selectWindow;
        bool _selectSeen;
        bool _rankedSeen, _rankedPlayed;
        bool _cardDetailSeen;
        /// <summary>进入 Forge 阶段那一刻的 `ForgeData.LevelOf` 快照 —— 「领过没有」只能靠**变化**判，
        /// 因为我们的锻造进度**初值故意造得非零**（`ForgeData.Init` 里第 1 个阵营直接满级、
        /// 第 2 个正好可领），直接问 `LevelOf > 0` 会**恒真**。</summary>
        readonly Dictionary<string, int> _forgeAtEnter = new Dictionary<string, int>();

        /// <summary>缺图（= 指针那几张原版图没进 `Resources/`；自检 / 诊断读口）。</summary>
        public List<string> MissingArt { get { return _pointer != null ? _pointer.MissingArt : _empty; } }
        static readonly List<string> _empty = new List<string>();

        // ---- 自检 / 诊断读口
        public bool IsActive { get { return _active; } }
        public int StageIndex { get { return _index; } }
        public MenuTutorialStageDef CurrentStage { get { return MenuTutorialStages.At(_index); } }
        public Vector2 LastPointPx { get { return _lastPoint; } }
        public TutorialPointerUi Pointer { get { return _pointer; } }
        public MenuTutorialHighlight Highlight { get { return _highlight; } }

        // ============================================================ 入口
        /// <summary>🔴 **2026-10-18（用户口径）**：初次进入游戏**不再自动启动**菜单侧引导 ——
        /// 玩家自己会去点对应的地方学习。保留给调试/回归的显式开关 `-wfftue`（命令行，⛔ 不是 UI）。
        /// <para>⚠️ 原版行为（首次进菜单自动播）在 `MenuTutorialStages` 里仍完整保留，改回 true 即恢复。</para></summary>
        public static bool AutoStart = false;

        static bool HasForceStartFlag()
        {
            foreach (var a in System.Environment.GetCommandLineArgs())
                if (string.Equals(a, "-wfftue", System.StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>菜单侧引导**要不要跑**（E01 的 5 阶段链与 `MenuTutorialManager` 共用这一条判断）。
        /// <para>🔴 2026-10-18 用户口径：初次进入**不自动启动**；只有 `-wfftue` 显式启动。改回 `AutoStart=true` 即恢复原版行为。</para></summary>
        public static bool MenuTutorialRequested { get { return AutoStart || HasForceStartFlag(); } }

        /// <summary>主菜单建完之后调一次（`MainMenuRuntime.Build()` 末尾那一跳）。
        /// <para>🔴 **只运行时挂**：本工程的自检是**编辑器非播放**跑同一份 `Build()`，
        /// 挂上去会往自检树里多塞指针/聚光灯节点、并写 `PlayerPrefs` ⇒ 一律不挂（`Application.isPlaying` 判）。</para></summary>
        public static MenuTutorialFlow Attach(MainMenuRuntime menu)
        {
            if (!Application.isPlaying) return null;
            if (menu == null) return null;
            bool start = MenuTutorialRequested;
            // 🔴 **复用跨场景存活的那一份**（`Awake` 里 `DontDestroyOnLoad`）：
            //   `_rankedPlayed` 这类「本进程内观测过的事」必须留着（否则从战斗场景回来就丢了 → 阶段死锁）。
            //   ⚠️ **不挂到主菜单下面**：`DontDestroyOnLoad` 只对根对象生效，挂上去就跟着场景一起没了。
            if (_instance != null)
            {
                _instance._menu = menu;
                if (start) _instance.StartFlow(); else _instance._active = false;
                return _instance;
            }
            var go = new GameObject("Menu Tutorial Flow");
            var f = go.AddComponent<MenuTutorialFlow>();
            f._menu = menu;
            _instance = f;
            if (start) f.StartFlow(); else f._active = false;
            return f;
        }

        void Awake()
        {
            _instance = this;
            // 🔴 **跨场景存活**：`TutorialStagePlayRanked` 的完成信号是「打了一场」（原版读服务器计数）——
            //    我们只能观察「**离开过主菜单场景**」⇒ 必须活过那一次 `LoadScene`，否则回到主菜单时
            //    这个类会重新建一份、把那条观测丢掉（= 阶段死锁）。`DontDestroyOnLoad` 只对**根**对象生效
            //    ⇒ 本对象**不挂到主菜单下面**（`Attach` 也不挂，见那里的注释）。
            DontDestroyOnLoad(gameObject);
            _pointer = TutorialPointerUi.Create(transform, TutorialPointerUi.Tier.Menu);
            _highlight = MenuTutorialHighlight.Create(transform);
            _pointer.Hide();
            if (_highlight != null) _highlight.HideImmediate();
        }

        void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
        void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

        /// <summary>读存档并进入第一个「未完成且条件未满足」的阶段（= 原版 `_DelayedPLayTutorial_d__13__MoveNext`）。</summary>
        public void StartFlow()
        {
            if (PlayerPrefs.GetInt(PrefSkip, 0) != 0)
            {
                _active = false;
                Debug.Log("[MenuTutorial] 整条链已被标记为跳过（`" + PrefSkip + "`）⇒ 不启动");
                return;
            }
            int first = FirstPlayableStage();
            if (first < 0)
            {
                _active = false;
                Debug.Log("[MenuTutorial] 所有 stage 都已完成（或条件已满足）⇒ 整条链不启动");
                return;
            }

            _active = true;
            Enter(first);

            // 🔴 出声，不静默：存档位置是我们的等价物，不是原版的远端 profile。
            Debug.Log("[MenuTutorial] 菜单侧 FTUE 已启动（原版 `TutorialManager` 的 `FlowTutorialStage` 链，"
                    + "顺序 = `stages[]` 序列化数组序）。进度存 `PlayerPrefs[\"" + PrefDonePrefix
                    + "<stageID>\"]` —— 形状照原版 `TutorialV2SaveData.StageSaveData { stageId, isCompleted }`，"
                    + "但原版落**远端 profile**（`PlayerDataManager.UploadTutorialV2SaveData`），本仓没有后端 ⇒ "
                    + "存储位置是**本地等价物**（行为一样：按 stageId 记住哪些做完了）。");
        }

        /// <summary>= 原版 `_DelayedPLayTutorial` 的「找第一个 `completed == false` 且 `CheckForCompletion() == false`」。
        /// <para>⚠️ `TutorialFinished` 例外：它的本地等价判据（「当前在主菜单」）在开局那一刻恒真，
        /// 拿它跳过会把通关那段悄悄吞掉 ⇒ 这一阶段只要没完成就**一定**选中（判据见 `CheckCompletion`）。</para></summary>
        int FirstPlayableStage()
        {
            for (int i = 0; i < MenuTutorialStages.Count; i++)
            {
                if (IsCompleted(i)) continue;
                var def = MenuTutorialStages.At(i);
                if (def != null && def.Id == MenuTutorialStageId.TutorialFinished) return i;
                if (CheckCompletion(i)) continue;      // 条件已满足 ⇒ 跳过但**不标完成**（原版就是这样）
                return i;
            }
            return -1;
        }

        // ============================================================ 阶段推进
        void Enter(int i)
        {
            _index = i;
            _poll = 0f;

            var def = MenuTutorialStages.At(i);
            if (def == null) { FinishAll(); return; }
            Debug.Log("[MenuTutorial] 进入阶段 " + (i + 1) + "/" + MenuTutorialStages.Count
                    + " `" + def.ClassName + "`（原版 stageID = " + (def.StageId ?? "<本地没有该 SO>")
                    + " · 指针 tier = " + (def.PointerTier ?? "<none>") + "）");

            if (def.OnEnter == MenuTutorialActionKind.OpenSelectArmyWindow)
            {
                _selectWindow = WindowsManager.OpenChooseArmyDeck();
                _selectSeen = true;
                if (_selectWindow == null)
                    Debug.LogWarning("[MenuTutorial] `OpenChooseArmyDeck()` 没返回窗（宿主没建起来？）⇒ 本阶段只剩「等它被关掉」那一半，"
                                   + "会卡住 —— 可按 `SkipCurrent()` 跳过");
                else
                    Debug.Log("[MenuTutorial] `TutorialStageSelectArmy.Execute` 的等价物：打开 `Choose Army Deck FTUE Window`");
            }

            // 原版 `DoMenuActions`：`TutorialMessageData.alreadyShown == false` ⇒ 先弹整屏简报，**这一帧不挂指针**。
            // ⚠️ 本方法会在 `MainMenuRuntime.Build()` 里被调到（主菜单还在建）⇒ **延到第一帧再弹**，
            //   避免在建树过程中开窗（`EnsureHost` 会建 WindowsManager，时序上更脏）。
            // `alreadyShown` 按**阶段**记（本对象跨场景存活，见 `Awake` 的 `DontDestroyOnLoad`）——
            // 否则「进战斗再回菜单」会把同一条简报重弹一遍。
            _wantMessage = def.HasMessage && _messageShownStage != i;
            if (_wantMessage) _messageShownStage = i;
            _explainShown = def.HasExplain && _explainShownStage == i;
            if (_highlight != null) _highlight.HideImmediate();
            _pointer.Hide();

            // Forge 阶段：记下进场那一刻的进度（见 `_forgeAtEnter` 的注释）
            _forgeAtEnter.Clear();
            if (def.Id == MenuTutorialStageId.ClaimForgeFirstLevel)
            {
                var armies = ForgeData.Armies;
                for (int k = 0; k < armies.Length; k++) _forgeAtEnter[armies[k]] = ForgeData.LevelOf(armies[k]);
            }

            _lastTop = null;
            _lastTab = WindowTabType.None;
            _lastPoint = new Vector2(-1f, -1f);
        }

        /// <summary>当前阶段完成（= `FlowTutorialStage.Complete`）：按 stageId 置完成位 → 从数组头找下一个。</summary>
        public void Advance()
        {
            if (!_active) return;
            var def = MenuTutorialStages.At(_index);
            if (def != null)
            {
                MarkCompleted(_index);
                Debug.Log("[MenuTutorial] 阶段完成 `" + def.ClassName + "`（stageID = "
                        + (def.StageId ?? "#" + def.ClassName) + "）⇒ `Complete()`（原版：`SaveStage` + 重启 `DelayedPLayTutorial`）");
            }
            // = 原版 `Complete()` 重启 `DelayedPLayTutorial`：**从数组头**再找第一个未完成/未满足的，
            //   ⛔ 不是简单 `_index + 1`（`SkipAll` / 条件已满足的 stage 会让后面出现空洞）。
            int next = FirstPlayableStage();
            if (next < 0) { FinishAll(); return; }
            Enter(next);
        }

        /// <summary>跳过**当前**阶段（= 原版 `FlowTutorialStage.TryForceComplete`：`isFUTE` ⇒ 置 completed）。
        /// <para>⚠️ 原版这个方法**在可读语料里 0 调用点**（只有定义）；这里接的是我们的跳过入口（见 `Update`）。</para></summary>
        public void SkipCurrent()
        {
            if (!_active) return;
            Debug.LogWarning("[MenuTutorial] 跳过当前阶段 `" + (CurrentStage != null ? CurrentStage.ClassName : "-") + "`"
                           + "（= 原版 `TryForceComplete` 的等价物）");
            Advance();
        }

        /// <summary>跳过**整条链**（= 原版 `TutorialManager.ForceCompleteAll`：全部置 completed + `SaveAllStages`）。
        /// <para>⚠️ 原版那个入口只由设置/测试触发，**可读语料里 0 调用点**；我们的入口见 `Update` 的快捷键。</para></summary>
        public void SkipAll()
        {
            PlayerPrefs.SetInt(PrefSkip, 1);
            Debug.LogWarning("[MenuTutorial] 整条 FTUE 被跳过（= 原版 `TutorialManager.ForceCompleteAll` 的等价物）");
            FinishAll();
        }

        /// <summary>清空本地进度（等价于原版「重播教程」那条链：`TutorialStage*` 全部 `Reset()`）。</summary>
        public static void ResetProgress()
        {
            DeleteAllProgress();
            Debug.Log("[MenuTutorial] 本地 FTUE 进度已清空（原版：设置里的 `Replay tutorial`）");
        }

        static void DeleteAllProgress()
        {
            // 新格式：每个 stageId 一个完成位
            for (int i = 0; i < MenuTutorialStages.Count; i++)
            {
                var k = DoneKey(MenuTutorialStages.At(i));
                if (k != null) PlayerPrefs.DeleteKey(k);
            }
            // v1 旧档（按下标 + 位图）—— 清掉，别让旧值影响新逻辑
            PlayerPrefs.DeleteKey(PrefStageLegacy);
            PlayerPrefs.DeleteKey(PrefDoneLegacy);
            PlayerPrefs.DeleteKey(PrefSkip);
            PlayerPrefs.Save();
        }

        /// <summary>🆕 **2026-10-18（调试口，⛔ 不是原版件）**：给 player 命令行用 ——
        /// `-wfskipftue` = 整链标记为已完成/已跳过 · `-wfresetftue` = 清空进度、下次进主菜单从第 1 段重来。
        /// <para>⛔ **不做成原版式设置按钮**：原版 `ForceCompleteAll` / `TryForceComplete` 的调用方
        /// **不在可读语料里**（`d:/2/tools/decomp_full/` 只有定义、0 引用），而唯一的候选 UI 词条
        /// `MainMenu/Settings/ButtonLabel/ReplayTutorial` 在**本地所有预制体里 0 使用**（只有词条本身）
        /// ⇒ 按铁律 11 例外① **不凭空发明一个原版没有的设置项**。玩家侧入口 = `Update` 里的快捷键（等价实现）。</para></summary>
        public static void SetSkippedForDebug(bool skip)
        {
            if (skip)
            {
                PlayerPrefs.SetInt(PrefSkip, 1);
                PlayerPrefs.Save();
                Debug.LogWarning("[MenuTutorial] （调试口 `-wfskipftue`）整条 FTUE 标记为已跳过 —— 以后进主菜单都不再启动。");
            }
            else
            {
                DeleteAllProgress();
                Debug.LogWarning("[MenuTutorial] （调试口 `-wfresetftue`）进度已清空 —— 下次进主菜单从第 1 段重来。");
            }
            if (_instance == null) return;
            if (skip) { _instance._active = false; _instance.FinishAll(); }
            else _instance.StartFlow();
        }

        void FinishAll()
        {
            _active = false;
            // = 原版 `ForceCompleteAll` / `SaveAllStages`：**每个 stage 单独记完成**（不是「存一个终点下标」）
            for (int i = 0; i < MenuTutorialStages.Count; i++)
            {
                var k = DoneKey(MenuTutorialStages.At(i));
                if (k != null) PlayerPrefs.SetInt(k, 1);
            }
            PlayerPrefs.Save();
            if (_pointer != null) _pointer.Hide();
            if (_highlight != null) _highlight.HideImmediate();
        }

        // ============================================================ 每帧
        void Update()
        {
            HandleSkipShortcut();
            Tick(Time.deltaTime);
        }

        /// <summary>🆕 **2026-10-19（E01 收尾）跳过入口** —— `F9` = 跳过当前阶段 · `Shift+F9` = 跳过整条链。
        /// <para>🔴 **这是【等价实现，非原版直证】**：原版菜单侧**没有可读的跳过 UI** ——
        /// `FlowTutorialStage.TryForceComplete` / `TutorialManager.ForceCompleteAll` 在
        /// `d:/2/tools/decomp_full/` 里**只有定义、0 调用点**；唯一沾边的设置词条
        /// `MainMenu/Settings/ButtonLabel/ReplayTutorial` 在**本地全部预制体里 0 使用**（只导出了词条）；
        /// 战斗侧那个 `Battle/Settings/SkipTutorial` + `BattleSettingsWindow.SkipTutorialButtonOnClick`
        /// 是**战斗内教程**的，不是本链。⇒ 不发明原版 UI，改走**引擎已有的输入路径**
        /// （`UnityEngine.InputSystem`，与 `BattleDriver` 的 ESC/Enter 同一条；批处理里 `Keyboard.current == null` ⇒ 自动跳过）。</para></summary>
        void HandleSkipShortcut()
        {
            if (!_active) return;
            var kb = Keyboard.current;
            if (kb == null) return;                      // 批处理 / 无键盘设备
            if (!kb.f9Key.wasPressedThisFrame) return;
            if (kb.shiftKey.isPressed) SkipAll();
            else SkipCurrent();
        }

        /// <summary>批处理 / 自检可直调（本文件没有帧循环也能推进）。</summary>
        public void Tick(float dt)
        {
            if (!_active) return;
            // 🔴 本对象**跨场景存活**（`DontDestroyOnLoad`，为的是 `PlayRanked` 那条观测）——
            //   离开主菜单之后 `_menu` 会被销毁（Unity 假 null）⇒ 这里直接停手。
            //   ⛔ **不能省这一句**：下面第一件事就是 `WindowsManager.Get()`，而它会在**没有宿主**的场景里
            //   走 `EnsureHost()` **建一个**（战斗场景凭空多出一套窗口管理器 = 静默污染）。
            if (_menu == null) return;
            if (_wantMessage)
            {
                _wantMessage = false;
                var d = MenuTutorialStages.At(_index);
                if (d != null) ShowMessage(d.TitleKey, d.BodyKey);
            }
            var wm = WindowsManager.Get();
            var top = wm != null ? wm.TopWindow : null;
            var tab = TabOf(top);
            if (top != _lastTop || tab != _lastTab)
            {
                _lastTop = top;
                _lastTab = tab;
                OnWindowChanged(top, tab);
            }

            // 指针动画（原版是协程 + WaitForSeconds；本仓按 dt 累计 —— 见 TutorialPointerUi.Tick）
            if (_pointer != null) _pointer.Tick(dt);
            if (_highlight != null) _highlight.Tick(dt);

            _poll += dt;
            if (_poll < PollInterval) return;
            _poll = 0f;

            TrackObservations(top);
            if (CheckCompletion(_index)) { Advance(); return; }
            // 目标窗可能动（滚动 / 换页 / 窗被重新摆位）⇒ 每次轮询重算一次手指尖
            RefreshPointer();
        }

        void OnWindowChanged(GameWindow top, WindowTabType tab)
        {
            // = 原版 `TutorialManager.WindowChange`：先撤旧指针，再交给当前 stage 的 `WindowsChange`
            if (_pointer != null) _pointer.Hide();
            if (_highlight != null) _highlight.HideImmediate();
            _lastPoint = new Vector2(-1f, -1f);
            RefreshPointer();
        }

        // ============================================================ 观测（本地等价物）
        void TrackObservations(GameWindow top)
        {
            if (top is FTUESelectDeckWindow) _selectSeen = true;
            if (top is RankedEventWindow) _rankedSeen = true;
            if (top is CardDetailPopup) _cardDetailSeen = true;
        }

        void OnSceneLoaded(Scene s, LoadSceneMode mode)
        {
            // 本对象跨场景存活 ⇒ 进战斗场景时先把指针/聚光灯关掉（它们是给菜单那一套相机画的）
            if (_pointer != null) _pointer.Hide();
            if (_highlight != null) _highlight.HideImmediate();
            _lastTop = null; _lastTab = WindowTabType.None; _lastPoint = new Vector2(-1f, -1f);
            if (!_active) return;

            // 原版：`TutorialStagePlayRanked.CheckForCompletion` 读 `PlayerData` 的排位计数（服务器）。
            // 我们：从**排位模式窗那一侧**进战斗场景 = 等价信号（没有排位存档）。
            if (_rankedSeen && s.IsValid() && !string.IsNullOrEmpty(s.name) && s.name != "MainMenu")
            {
                _rankedPlayed = true;
                Debug.Log("[MenuTutorial] 观测到离开主菜单进入场景 `" + s.name + "`（排位入口已打开过）"
                        + " ⇒ `TutorialStagePlayRanked` 的本地等价信号成立");
            }
        }

        // ============================================================ 完成条件（逐条对原版，见阶段表）
        bool CheckCompletion(int i)
        {
            var def = MenuTutorialStages.At(i);
            if (def == null) return true;
            switch (def.Id)
            {
                case MenuTutorialStageId.SelectArmy:
                    // 原版：`Inventory.DistinctCount > 0`（已拿到起始卡组）。
                    // 🔴 我们这一版 `FTUESelectDeckWindow` 的 `Continue` **不发卡**（没有后端，只出声 + 关窗，
                    //    见该窗文件头）⇒ 「卡库里有卡」这条**永远验不了**；也不拿 `DeckCount > 0` 顶
                    //    （存档为空时那会让阶段**死锁**）⇒ 等价物 = **那扇窗开过、并且已经被玩家关掉**。
                    return _selectSeen
                        && (_selectWindow == null || _selectWindow.CurrentState == WindowState.Closed);

                case MenuTutorialStageId.GetMission:
                    // 原版：`HasCampaignPoints()`（任一战役 `AvailablePoints > 0`）**且当前窗不是 `RewardWindow`**
                    //   （`TutorialStageGetMission__CheckForCompletion.c` 的 `DAT_184284328` = `RewardWindow_TypeInfo`）。
                    // 🔴 我们这一版战役点固定 9999（`CampaignData.Points`）⇒ 原判据恒真 ⇒ 等价物 = 真领过一个任务。
                    if (TopIs<RewardWindow>()) return false;
                    return AnyMissionClaimed();

                case MenuTutorialStageId.GetCampaignNode:
                    // 原版：`HasTwoNodesCompleted()`（`1 < count`）**且当前窗不是
                    //   `CampaignRewardsWindow` / `RewardWindow`**（同一份 .c 里两个 typeinfo 实读）。
                    //   方法名与官方文案「first reward node」的口径见阶段表。
                    if (TopIs<CampaignRewardWindow>() || TopIs<RewardWindow>()) return false;
                    return CampaignData.ClaimedCount >= 1;

                case MenuTutorialStageId.PlayRanked:
                    // 原版：`PlayerData` 排位计数 Sum > 0 **且当前窗是 `MainMenuWindow`** 才 `Complete()`
                    //   （`TutorialStagePlayRanked__CheckForCompletion.c` 的 `DAT_1842365f8` = `MainMenuWindow_TypeInfo`）
                    // ⇒ 打完那一局回到主菜单才置完成（⛔ 不是「一离开主菜单就算」）。
                    return _rankedPlayed && topIsMainMenu;

                case MenuTutorialStageId.LevelUpCard:
                    // 原版：任一卡 `CardTier > 1`，或 `HasNoCardToUpgrade()`（所有卡都不可升级）。
                    //   我们这一版升级钮被停用（`CardProgress.Level` 恒 MaxLevel）⇒ 两条原判据一条恒假、一条恒真
                    //   ⇒ 等价物 = 打开过卡牌详情窗（玩家能走到的最接近的一步）。
                    return _cardDetailSeen;

                case MenuTutorialStageId.ClaimForgeFirstLevel:
                    // 原版：`HasUsedForge()`（且当前窗不是 `RewardWindow`）；另有 `HasNoForgeableRewards()` ⇒ 直接 Complete。
                    // 🔴 我们的 `ForgeData` 初值**故意非零**（第 1 个阵营满级、第 2 个正好可领）⇒
                    //   `LevelOf > 0` 恒真、不能用；改成「**本阶段内** `LevelOf` 涨了」（快照见 `_forgeAtEnter`）。
                    if (TopIs<RewardWindow>()) return false;
                    return ForgeClaimedSinceEnter() || NoForgeableRewards();

                case MenuTutorialStageId.TutorialFinished:
                    // 原版：`CheckForCompletion` = `SegmentationController.CheckTag(0x14)`；
                    //   `Execute` 判「已在主菜单」⇒ `ShowMainMenu()` + 开完成简报 + `Complete()`。
                    // 我们：只要玩家回到主菜单（或一开始就在）就算完成 —— 这一步没有别的状态可等。
                    return topIsMainMenu;
            }
            return false;
        }

        /// <summary>当前最上面那扇窗（没有 `WindowsManager` 时 = null）。</summary>
        static GameWindow TopWindowOrNull()
        {
            var wm = WindowsManager.Get();
            return wm != null ? wm.TopWindow : null;
        }

        /// <summary>原版那几处「`if (currentWindow is X) return;`」的等价物（typeinfo 判据见各 case 注释）。</summary>
        static bool TopIs<T>() where T : GameWindow
        {
            var t = TopWindowOrNull();
            return t != null && t is T;
        }

        bool topIsMainMenu
        {
            get
            {
                var wm = WindowsManager.Get();
                var t = wm != null ? wm.TopWindow : null;
                return t == null || t is MainMenuWindow;
            }
        }

        /// <summary>「领过任一任务」= 原版 `HasCampaignPoints()` 的本地等价物（我们的战役点固定 9999 ⇒ 原判据恒真）。
        /// <para>🔴 **2026-10-19（MainTutorialManager 实现）起公开** —— E01 的 `GetMission` 与
        /// `MenuTutorialManager.GetState` 的 `ClaimedCampaignPoints` 判据**共用这一份**（⛔ 别各写一份）。</para></summary>
        public static bool HasAnyClaimedMission()
        {
            // 三条日/周/登录取其一，再扫日任务表。
            if (DailyData.LoginClaimed() || DailyData.WeeklyClaimed() || DailyData.SkullsClaimed()) return true;
            for (int i = 0; i < 10; i++) if (DailyData.DailyClaimed(i)) return true;
            return false;
        }

        static bool AnyMissionClaimed() { return HasAnyClaimedMission(); }

        /// <summary>进入 Forge 阶段之后，任一军团的已领格数**涨了**没有。</summary>
        bool ForgeClaimedSinceEnter()
        {
            foreach (var kv in _forgeAtEnter)
                if (ForgeData.LevelOf(kv.Key) > kv.Value) return true;
            return false;
        }

        static bool NoForgeableRewards()
        {
            var armies = ForgeData.Armies;
            for (int i = 0; i < armies.Length; i++)
                if (ForgeData.HasToCollect(armies[i])) return false;
            return true;
        }

        // ============================================================ 指针 / 聚光灯
        void RefreshPointer()
        {
            var def = MenuTutorialStages.At(_index);
            if (def == null || _pointer == null) return;
            var wm = WindowsManager.Get();
            var top = wm != null ? wm.TopWindow : null;

            // 整屏简报开着的时候**不挂指针** —— 原版 `DoMenuActions` 在 `ShowTutorialMessageWindow(...)` 之后
            // 直接 `return`（下一跳由窗关掉之后的 `WindowsChange` 触发）。
            if (top is TutorialMessagePopup)
            {
                _pointer.Hide();
                if (_highlight != null) _highlight.Hide();
                _lastPoint = new Vector2(-1f, -1f);
                return;
            }

            Transform target = null;
            Vector2 off = def.PointerOffset;

            // = 原版 `DoMenuActions` 的三步：① explain 简报 → ② 窗里那颗（`TargetInWindow`）→ ③ 页里那颗钮（`DeepTarget`）。
            if (top != null)
            {
                // 第三步（原版 `explain…` 的 `alreadyShown == true` 之后那一路）：
                //   指针从「页签」换成「页里那颗钮」（任务 Collect / 锻造 Claim / 战役 Unlock Button）。
                if (_explainShown && def.DeepTarget != MenuTutorialTargetKind.None)
                {
                    target = Resolve(def.DeepTarget, top);
                    if (target != null) off = def.DeepOffset;
                }
                // 第三步**另一支**（同一步在不同上下文指不同件，例：GetCampaignNode 的战役页签 ↔ 战役窗）
                if (target == null && _explainShown && def.DeepTarget2 != MenuTutorialTargetKind.None)
                {
                    target = Resolve(def.DeepTarget2, top);
                    if (target != null) off = def.DeepOffset2;
                }
                // 第二步：窗里那一颗（`TargetInWindow`），没有再看菜单上那一颗（`Target`）。
                if (target == null && def.TargetInWindow != MenuTutorialTargetKind.None)
                {
                    target = Resolve(def.TargetInWindow, top);
                    if (target != null) off = def.PointerOffset;
                    // 进了窗 ⇒ 弹第二份简报（原版 `explain…`，每阶段只弹一次）。
                    // 🔴 **2026-10-19（E01 收尾）**：原版这一步是**认页签/认窗类型**的 ——
                    //   `GetMission/GetCampaignNode/ClaimForge` 的 `explain…` 只在**对应页签是当前页**时才弹
                    //   （`DoMenuActions` 里先比 `currentTab` 的类型，如 `CampaignWindowTab_TypeInfo`）。
                    //   原来我们只要页签按钮**存在**就弹 ⇒ 在别的页签上也会弹错简报。这里补上那道闸。
                    if (target != null && def.HasExplain && !_explainShown
                        && IsInWindowContext(def.TargetInWindow, top))
                    {
                        _explainShown = true;
                        _explainShownStage = _index;
                        ShowMessage(def.ExplainTitleKey, def.ExplainBodyKey);
                        _pointer.Hide();
                        if (_highlight != null) _highlight.Hide();
                        _lastPoint = new Vector2(-1f, -1f);
                        return;      // 简报盖在上面这一拍不挂指针（下一拍由窗关掉后的 `OnWindowChanged` 接手）
                    }
                }
            }
            if (target == null)
            {
                target = Resolve(def.Target, top);
                off = def.PointerOffset;
            }

            if (target == null)
            {
                // 目标控件还没建/不在（例：窗还没开）⇒ 只撤指针，**不出声**（这是正常中间态，不是失败）
                _pointer.Hide();
                if (_highlight != null) _highlight.Hide();
                _lastPoint = new Vector2(-1f, -1f);
                return;
            }

            var p = CenterPx(target);
            // 🔴 原版 `TutorialPointerData.offset`：指针根 `SetParent(target)` + `localPosition = offset`
            //   （`TutorialManager__AddPointerAction.c`）⇒ 指尖 = 目标 pivot + offset。offset 是目标本地像素、
            //   **y 向上**；我们的设计像素 **y 向下** ⇒ y 取反。⛔ 不从截图量，值照阶段表（序列化实读）。
            //   `off` = 当前用的是哪一颗目标就取哪一份（第二步 = `PointerOffset`、第三步 = `DeepOffset`）。
            if (off != Vector2.zero)
                p += new Vector2(off.x, -off.y);
            if ((p - _lastPoint).sqrMagnitude < 0.25f) return;      // 没动就别重建（重建会打断点击动画）
            _lastPoint = p;
            // 🔴 原版菜单链**不挂气泡**（`AddPointerAction` 里没有 `SetTutorialText`），文案走整屏简报。
            //   兜底仍然保留：万一指针手形图取不到（`Resources/` 缺图），把**同一条官方正文**放进气泡，
            //   别让玩家面对一个只有聚光灯的空屏（如实标注为偏离）。
            string bubble = CardArt.MenuUi(TutorialPointerUi.ArtPointer) == null ? Official(def.BodyKey) : null;
            _pointer.Show(p, bubble, true);
            if (_highlight != null) _highlight.ShowAt(p.x, p.y);
        }

        /// <summary>弹一扇整屏简报（= 原版 `TutorialManager.ShowTutorialMessageWindow`）。
        /// <para>⚠️ `TutorialMessagePopup` 的 `Event image` 会**空着**（原版优先用 `TutorialMessageData.image`、
        /// 退化到活动督军的 `fullHeroSprite`；那两个 AssetReference 都在远端）—— 该窗自己会出声一次。</para></summary>
        static void ShowMessage(string titleKey, string bodyKey)
        {
            if (string.IsNullOrEmpty(titleKey) && string.IsNullOrEmpty(bodyKey)) return;
            var ctx = TutorialMessagePopup.DefContext();
            ctx.TitleTerm = titleKey;
            ctx.DescTerm = bodyKey;
            WindowsManager.OpenTutorialMessage(ctx);
            Debug.Log("[MenuTutorial] 整屏简报：`" + (titleKey ?? "-") + "` / `" + (bodyKey ?? "-") + "`");
        }

        /// <summary>官方词条取值：`Loc.Table` 优先，其次 13 语言表（`official_loc_13lang.json`）。
        /// 取不到 ⇒ `null`（⛔ 不印键名、不编文案）。</summary>
        static string Official(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (Loc.HasEntry(key)) return Loc.T(key);
            return Loc.Lang13Of(key, Loc.Current == AvailableLanguages.Chinese ? "zh" : "en");
        }

        /// <summary>把 `TargetKind` 解成一颗**控件节点**。⛔ 一律按名字/窗类型找，不存坐标。</summary>
        Transform Resolve(MenuTutorialTargetKind kind, GameWindow top)
        {
            switch (kind)
            {
                case MenuTutorialTargetKind.NavRewards: return FindNode("Main Menu Navigation Button - Rewards");
                case MenuTutorialTargetKind.NavCollection: return FindNode("Main Menu Navigation Button - Collection");
                case MenuTutorialTargetKind.NavHome: return FindNode("Main Menu Navigation Button - Home");
                case MenuTutorialTargetKind.ModeCardRanked: return FindNode("Base Game Mode Container 1x2 - Ranked");
                case MenuTutorialTargetKind.RankedPlayButton:
                    // 原版 `playRankedButton.id = PlayRankedButton` → GO `To Battle Button`（menus 包实读）。
                    //   只在排位窗上找（遭遇战窗也有同名钮，但原版那一路只认 `RankedEventWindow(V2)`）。
                    return FindUnder(top != null ? top.transform : null, "To Battle Button");
                case MenuTutorialTargetKind.MissionClaimButton:
                {
                    // 原版第三步 id = `CollectMissionTutorial_Challenge_0`（按唯一 id 找任务行的 Collect）。
                    // 本地等价物 = MissionsTab 记的第一条可领/未领行的 `Collect`（见该属性的注释）。
                    var w = top as GameWindowWithTabs;
                    if (w == null || w.CurrentTab != WindowTabType.Missions) return null;
                    var tab = TabOfType<MissionsTab>(w);
                    return tab != null ? tab.TutorialClaimButton : null;
                }
                case MenuTutorialTargetKind.ForgeClaimButton:
                {
                    // 原版第三步 = `ForgeWindowTab.GetForgeItem(...)` 那颗可领格的 `Generic UI Button`。
                    var w = top as GameWindowWithTabs;
                    if (w == null || w.CurrentTab != WindowTabType.Forge) return null;
                    var tab = TabOfType<ForgeTab>(w);
                    return tab != null ? tab.TutorialClaimButton : null;
                }
                case MenuTutorialTargetKind.CampaignClaimButton:
                {
                    // 原版 id `CampaignRewardUnlockBaseReward` → 基础列 `Unlock Button`（offset (0,20)）。
                    var cw = top as CampaignRewardWindow;
                    return cw != null ? cw.TutorialClaimButton : null;
                }
                case MenuTutorialTargetKind.CampaignTrackNode:
                {
                    // 原版 `CampaignWindowTab.GetTutorialTrackArmyNode(tab, 2)` —— 战役页签里的教程轨道节点。
                    var w = top as GameWindowWithTabs;
                    if (w == null || w.CurrentTab != WindowTabType.Campaign) return null;
                    var tab = TabOfType<CampaignTab>(w);
                    return tab != null ? tab.TutorialTrackNode : null;
                }
                case MenuTutorialTargetKind.RewardsTabMissions: return TabButton(top, WindowTabType.Missions);
                case MenuTutorialTargetKind.RewardsTabCampaign: return TabButton(top, WindowTabType.Campaign);
                case MenuTutorialTargetKind.RewardsTabForge: return TabButton(top, WindowTabType.Forge);
                case MenuTutorialTargetKind.CollectionTabCards: return TabButton(top, WindowTabType.CollectionCards);
                case MenuTutorialTargetKind.CurrentWindow: return top != null ? top.transform : null;
            }
            return null;
        }

        public static Transform TabButton(GameWindow top, WindowTabType type)
        {
            var tabs = top as GameWindowWithTabs;
            if (tabs == null || tabs.tabButtons == null) return null;
            var opts = tabs.tabButtons.options;
            for (int i = 0; i < opts.Count; i++)
            {
                var o = opts[i];
                if (o == null || o.type != type || o.button == null) continue;
                return o.button.transform;
            }
            return null;
        }

        Transform FindNode(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            // ① 先在本棵树里找（`MainMenuRuntime.Find` 是递归的；运行期左栏可能挂在壳的安全区下 ⇒ 会落空）
            if (_menu != null)
            {
                var t = _menu.Find(name);
                if (t != null) return t;
            }
            // ② 再全场景按名字找（= 本工程既有的取件口径，同 `GameObject.Find`）
            var go = GameObject.Find(name);
            return go != null ? go.transform : null;
        }

        /// <summary>那一颗「窗里/页里的第二步目标」现在**在不在正确的上下文**里 ——
        /// 原版 `DoMenuActions` 先比 `currentTab` 的**类型**（如 `MissionsTab` / `CampaignWindowTab` /
        /// `ForgeWindowTab`，typeinfo 见各 stage 的实现）再决定弹不弹 `explain…`。
        /// <para>⚠️ `TargetInWindow` 本身（页签按钮）在**任何页签**都找得到 ⇒ 弹简报必须另加这道闸，
        /// 否则在别的页签上也会弹错那一份。</para></summary>
        static bool IsInWindowContext(MenuTutorialTargetKind kind, GameWindow top)
        {
            if (top == null) return false;
            var w = top as GameWindowWithTabs;
            switch (kind)
            {
                case MenuTutorialTargetKind.RewardsTabMissions: return w != null && w.CurrentTab == WindowTabType.Missions;
                case MenuTutorialTargetKind.RewardsTabCampaign: return w != null && w.CurrentTab == WindowTabType.Campaign;
                case MenuTutorialTargetKind.RewardsTabForge: return w != null && w.CurrentTab == WindowTabType.Forge;
                case MenuTutorialTargetKind.CollectionTabCards: return w != null && w.CurrentTab == WindowTabType.CollectionCards;
                case MenuTutorialTargetKind.RankedPlayButton: return top is RankedEventWindow;
                default: return true;
            }
        }

        /// <summary>在 `GameWindowWithTabs.tabs` 里找某一类页签实例（原版 `GetOpenWindow<MissionsTab>()` 的等价）。
        /// 页签对象**在非当前页时是 `SetActive(false)` 的** ⇒ 调用点必须先判 `CurrentTab`。</summary>
        public static T TabOfType<T>(GameWindow top) where T : WindowTabBase
        {
            var w = top as GameWindowWithTabs;
            if (w == null) return null;
            for (int i = 0; i < w.tabs.Count; i++)
                if (w.tabs[i] is T) return (T)w.tabs[i];
            return null;
        }

        /// <summary>只在**给定根**下面递归找（原版 `GameObjectUniqueIdentifier.Find(id)` 是「按唯一 id 全场景找」；
        /// 我们没有那套 id 组件 ⇒ 窗口内那一颗按子树找，避免撞上同名的别家族件）。</summary>
        public static Transform FindUnder(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var t = FindUnder(root.GetChild(i), name);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>节点中心 → 设计像素（左上原点）。判据 = `MenuDraw.PosInDesignSpace`（全壳唯一一份换算）。</summary>
        public static Vector2 CenterPx(Transform t)
        {
            float k = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;   // = 108 px / 世界单位
            var p = MenuDraw.PosInDesignSpace(t);
            return new Vector2(LayoutSpace.DesignPxW * 0.5f + p.x * k,
                               LayoutSpace.DesignPxH * 0.5f - p.y * k);
        }

        static WindowTabType TabOf(GameWindow w)
        {
            var tabs = w as GameWindowWithTabs;
            return tabs != null ? tabs.CurrentTab : WindowTabType.None;
        }
    }
}
