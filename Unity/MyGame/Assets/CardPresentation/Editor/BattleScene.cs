// BattleScene.cs — 可玩的对战场景 + 自检
//
// 两件事：
//   1. **建场景**（菜单 / CLI）—— 建好一个能点的对局：下方自己的 9 格、上方对手的 9 格、
//      底部手牌、HUD。存成 `Assets/CardPresentation/Scenes/Battle.unity`，
//      打开按 Play 就能拖牌、点单位打人、点 END TURN。
//   2. **自检**（CLI）—— 批处理下没有 play 循环、也没有真实输入，所以走 `Simulate*` 那套
//      （**和真实输入同一份逻辑**，不是另写一份），逐步断言 + 截图。
//
// 用法（菜单）：Tools > CardPresentation > 生成对战场景 / 对战自检
// 用法（CLI）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \
//     -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod BattleScene.Run \
//     -logFile "d:/4/_tmp_view/battle.log"
//   筛输出：grep "^BT "
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class BattleScene
{
    const string P = "BT ";
    const string OutDir = @"d:/4/_tmp_view/battle";
    /// <summary>对战场景的落盘路径。🆕 **2026-09-25 起按 `WF_ARENA` 参数化**：
    /// · **不设** `WF_ARENA` ⇒ `Battle.unity`（**缺省行为一字不变** —— 八条自检那条照旧走它）；
    /// · 设 `WF_ARENA=<场>` ⇒ `Battle_<场>.unity`（**该场那份**）。
    ///
    /// **为什么要多份**：原版是**按督军阵营查表选战场**（`ArenaByArmy.SceneFor`，判据 → `资料/普查产出_0920/
    /// 场景光照与后处理_原版规格.md` §六），而战场几何是**建场时烘进场景**的（`BuildArena3D` →
    /// `ArenaBuilder.BuildContent`，根节点 `"Warpforge_" + mf.scene`）⇒ **「一局一个战场」= 一场一份 Battle 场景**。
    /// ⚠️ **场景在 `.gitignore` 里**（`Assets/CardPresentation/Scenes/`）⇒ 多份**不占仓库、不算源码重复**；
    /// 源码始终只有**一段** `BuildScene()`，跑 N 次而已。
    /// **判据只此一处**：`BuildAndSaveScene` 存它、自检开它。名字不认识时**回退 `Battle.unity` 并出声**
    /// （判定转发 `ArenaBuilder.ArenaFromEnv`，别在别处再写一套）。</summary>
    /// <summary>🆕 **2026-09-30（§27 架构）起只有一份**：`Battle.unity`。
    /// 原来按 `WF_ARENA` 分叉成 13 份 `Battle_<场>.unity` —— 那套把战场**烘进每份场景**；
    /// 现在战场是**运行时实例化**（`ArenaRuntimeLoader` + `Resources/ArenaPrefabs/`）⇒
    /// **一份场景服务 13 个战场**。`WF_ARENA` 对存盘路径不再有影响
    /// （它仍然决定「自检/出图这一轮看哪一场」，那个判据在 `ArenaRuntimeLoader.ResolveArenaKey`）。
    /// ⚠️ 场景在 `.gitignore` 里。</summary>
    static string ScenePath
    {
        get { return "Assets/CardPresentation/Scenes/Battle.unity"; }
    }

    /// <summary>本局用哪个原版战场。**判据 = `ArenaByArmy.SceneFor(督军阵营)`**（运行时那张表），
    /// 建场时由 `WF_ARENA` 选（转发 `ArenaBuilder.ArenaFromEnv`）。
    /// 表本体与判据 → `资料/普查产出_0920/场景光照与后处理_原版规格.md` §六。</summary>
    static string BoardArena { get { return ArenaBuilder.ArenaFromEnv(); } }

    /// <summary>🆕 **2026-10-14（A555）** 自检用：现读一次 `Resources/EnvBlendables.json` 的
    /// **`sceneStandaloneBuild[]`**（只取这一节）。
    /// **为什么另起一个局部 DTO**：`CardPresentation.ScenarioBlendableFactory` 里那份
    /// `SceneStandaloneFile` 是 **internal**（跨程序集看不见，而且 `_sceneStanBuild` 是 private static）；
    /// 而 `JsonUtility` **忽略**目标类型里没有的键 ⇒ 照 `Battle/ScenarioBlendables.cs:1975-1978` 那个先例
    /// **只声明自己要的那一节**，并且**复用它的 `AnimFxBuildGroup`**（⛔ 不另定义一套「节点」类型）。</summary>
    [System.Serializable]
    class EnvBuildProbeFile
    {
        public CardPresentation.ScenarioBlendableFactory.AnimFxBuildGroup[] sceneStandaloneBuild;
    }

    /// <summary>🆕 **2026-10-14（A555）** 自检用：每场在旁挂 `sceneStandaloneBuild[]` 里
    /// **`nodes[]` 的条数**（`root` 对不上 / 读不到 ⇒ 0）；`found` = 按 `root` 真找到了几节。
    /// 🔴 **要它是因为那个数不能再写死**：那一节是 `工具/gen_env_blendables.py` 的产物，而生成器里
    /// 有一条「与建场侧已建的路径重合就跳过」的守卫（`:942-943 if paths[i] in groups_paths: continue`）
    /// ⇒ **重跑一次就会把与建场侧重合的那些条目全跳掉**（13 场里正好 8 条）⇒ 写死 `3 / 4 / 1`
    /// 会把**合法重生成**误报成红。取法照生产那条（`Battle/ScenarioBlendables.cs:2304-2305`：
    /// 按 `root` 线性找一节）—— ⛔ **不按数组下标**（顺序是生成器给的，别假设）。
    /// ⚠️ 读不到时**静默回 0** ⇒ 调用侧必须再断一条「三节都找到了」的前提（否则「没读到」会被
    /// 伪装成「旁挂本来就是空的」= 弱断言分不出两种状态）。</summary>
    static int[] EnvBuildNodeCounts(string[] arenaKeys, out int found, out int[] reparen)
    {
        var counts = new int[arenaKeys.Length];
        reparen = new int[arenaKeys.Length];
        found = 0;
        var ta = Resources.Load<TextAsset>("EnvBlendables");
        if (ta == null) return counts;
        var f = JsonUtility.FromJson<EnvBuildProbeFile>(ta.text);
        var groups = f != null ? f.sceneStandaloneBuild : null;
        if (groups == null) return counts;
        for (int i = 0; i < arenaKeys.Length; i++)
            for (int j = 0; j < groups.Length; j++)
                if (groups[j] != null && groups[j].root == arenaKeys[i])
                {
                    counts[i] = groups[j].nodes != null ? groups[j].nodes.Length : 0;
                    // 🆕 **2026-10-14（A418②）**：`reparent[]` 也从这个**同一次加载**里取
                    // （它与 `nodes[]` 联动，见调用点那段注释）。
                    reparen[i] = groups[j].reparent != null ? groups[j].reparent.Length : 0;
                    found++;
                    break;
                }
        return counts;
    }

    // ---- 版面（归一化，y 从底部算）----
    //
    // **全部来自原版实测**，不是拍的。原版 1920×1080 下量到的（出处：
    // `d:/warpforge/scripts/battle.gd` 的场卡尺寸体系 + `资料/对战排版_原版数值与改造方案.md`）：
    //     玩家行中心 y = 708 px  →  1 - 708/1080 = 0.3444
    //     敌方行中心 y = 466 px  →  1 - 466/1080 = 0.5685
    //     场卡 137.2 × 218.4 px，相邻中心距 149.3 px（= MinionSeparation 0.82 × 182.14）
    //     手牌卡 165 × 263 px，中心行 y≈950（= 玩家槽底 + 卡半高 + 9 px 隙）
    //
    // 换算：这套布局「可见高恒 10 世界单位」，1080p 下 **108 px / 世界单位**，
    //       所以 px → 归一化 = px/1920（横）、px/1080（纵）；
    //       px → 卡缩放   = px / (CardView 的尺寸 × 108)。
    //  ⚠️ **2026-09-12 撤掉了「两行整体上移 0.05」那个补丁**。它的由来是：我们的卡当时是
    //     1.45×2.03（比原版**矮 12%**），照搬 708 的话手牌顶不到战场、中间那条弧会盖住
    //     前排督军卡底部的数值 —— 于是把两行一起上移 54 px 躲开。
    //     现在卡本体改成原版的 2.0927×3.3313，两个尺寸都**精确对上**了
    //     （场卡 137.2×218.4、手牌 165.0×262.7，见 `Run()` 里的断言），补丁就该撤：
    //     按原版数值，手牌上沿正好贴住玩家行下沿（差 1.4 px）—— **原版就是这么贴着的**。
    const float EnemyLineY = 0.5685f;                                   // 原版 466/1080
    const float PlayerLineY = 0.3444f;                                  // 原版 708/1080
    // 🔴 判据**只此一处**（在 `BoardLayout`）：这里原来各写了一份同样的算式，
    //    而 `BoardLayout` 的**默认值**写着另一个数（0.876，错的）—— 两份并存迟早不一致，已经出过事。
    const float BoardSpacing = BoardLayout.OriginalSlotPitchPx / 1920f;   // 149.3/1920 = 0.0778
    const float BoardScale = BoardLayout.DefaultPlacedScale;              // 137.2/(2.0927×108) = 0.607
    // 手牌中心行：原版 y≈950 px（0.1204）—— 卡底正好压在屏幕下沿上。
    const float HandBaselineY = HandLayout.DefaultBaselineY;            // 0.1204（判据收在 HandLayout 一处）
    const float HandScale = HandLayout.DefaultCardScale;                // 165/(2.0927×108) = 0.730

    /// <summary>原版场卡的屏幕宽度占比（137.2/1920）—— 自检拿它当基准</summary>
    const float OriginalCardWidthRatio = 137.2f / 1920f;
    /// <summary>原版 9 槽整排跨度占比（8×149.3+137.2 = 1331.6 / 1920）</summary>
    const float OriginalBoardSpanRatio = 1331.6f / 1920f;
    /// <summary>原版场卡高度 px（`3.3313 × 0.36 × 182.14`，见 `审查更正清单_0827.md:136`）</summary>
    const float BoardCardHeightPx = 218.4f;
    /// <summary>原版手牌卡高度 px（`3.3313 × 0.73 × 108`）</summary>
    const float HandCardHeightPx = 262.6f;

    [MenuItem("Tools/CardPresentation/生成对战场景")]
    public static void BuildAndSaveScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera cam = BuildScene(out BattleDriver driver, out _, out _, out _);
        // 玩的时候手牌让位要走补间（不然拖拽时整排牌瞬移）。
        // **只在存场景这一路打开** —— 批处理自检要当场精确的位置，见 HandLayout.animateRelayout
        var hand = PlayerHand();
        if (hand != null) hand.animateRelayout = true;
        // 手感补间（攻击位移 / 命中抖动 / 阵亡消散 / 发牌入场）同理，见 `BattleDriver.animateFeel`
        if (driver != null) driver.animateFeel = true;
        // 开局换牌（原版单机是进的；批处理自检默认跳过，见 `BattleDriver.mulliganEnabled`）
        if (driver != null) driver.mulliganEnabled = true;
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        EnsureInBuildSettings(ScenePath);
        AssetDatabase.Refresh();
        Debug.Log(P + $"对战场景已存：{ScenePath} —— 打开按 Play 就能玩");
    }

    /// <summary>把对战场景加进 `EditorBuildSettings`（幂等）—— **否则 `SceneManager.LoadScene` 在 Play 模式下会抛**
    /// （`Scene '…' couldn't be loaded because it has not been added to the build settings`）。
    /// 判据与 `MainMenuScene` / `DeckScene` / `ShellScene` 那三个登记器**同形**。
    ///
    /// 🔴 **2026-09-25 查出：`Battle.unity` 以前从来没被登记过** —— 全工程三个登记器（`MainMenuScene` /
    /// `DeckScene` / `ShellScene`）里**没有对战场景这一份**；而 `PlayerBuild` 走的是
    /// 「直接塞 `BuildPlayerOptions.scenes`、不动这个文件」那条路 ⇒ **编辑器里「从外壳进战斗」那一下
    /// 一直没被真正执行过**（外壳那两处 `LoadScene("Battle")` 在批处理下会提前 return，
    /// 所以八条自检也照不到它）。这里补上 —— 缺它的话，**一场一份 Battle 场景**这条路根本走不通。</summary>
    static void EnsureInBuildSettings(string path)
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        // 🆕 2026-09-30（§27）：顺手**清掉指向已删场景的死条目** ——
        //   13 份 `Battle_<场>.unity` 现在不再生成（战场改成运行时实例化），
        //   而 Build Settings 里还留着它们 ⇒ 出 player 时会报「场景文件不存在」。
        int dead = 0;
        for (int i = list.Count - 1; i >= 0; i--)
            if (!System.IO.File.Exists(list[i].path)) { list.RemoveAt(i); dead++; }
        if (dead > 0)
        {
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log(P + $"  清掉 Build Settings 里 {dead} 个**指向已删场景**的死条目（§27 之后 13 份 Battle_<场> 不再生成）");
        }
        foreach (var s in list) if (s.path == path) { Debug.Log(P + $"  已在 Build Settings：{System.IO.Path.GetFileName(path)}"); return; }
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log(P + $"  已加进 Build Settings：{System.IO.Path.GetFileName(path)}");
    }

    // ==================================================================
    //  自检
    // ==================================================================

    /// <summary>读 <paramref name="rt"/> 上一个像素（**自检专用**；LUT 那条链要真把像素读回来
    /// 才证明得了 `lerp` 的语义 —— 只验「接上了」什么都证明不了）。</summary>
    static Color ReadRtPixel(RenderTexture rt, int x, int y)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tmp = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tmp.ReadPixels(new Rect(x, y, 1, 1), 0, 0);
        tmp.Apply();
        var c = tmp.GetPixel(0, 0);
        RenderTexture.active = prev;
        UnityEngine.Object.DestroyImmediate(tmp);
        return c;
    }

    /// <summary>`m` 的每个通道是否都落在 `a`/`b` 之间（容差 <paramref name="eps"/>）。
    /// 🔴 用「区间」而不是「逐值相等」是因为 RT 上可能有一次 sRGB 写转换，而**单调**变换
    /// 保区间、不保值 —— 逐值比会假红。</summary>
    static bool Between3(Color m, Color a, Color b, float eps)
    {
        return In(m.r, a.r, b.r, eps) && In(m.g, a.g, b.g, eps) && In(m.b, a.b, b.b, eps);
        bool In(float v, float p, float q, float e)
        { return v >= Mathf.Min(p, q) - e && v <= Mathf.Max(p, q) + e; }
    }

    /// <summary>`m` 是否比 `b` **更靠近** `a`（同样是转换无关的判据：`|m−a| < |m−b|`）。</summary>
    static bool CloserTo(Color m, Color a, Color b)
    {
        float da = Mathf.Abs(m.r - a.r) + Mathf.Abs(m.g - a.g) + Mathf.Abs(m.b - a.b);
        float db = Mathf.Abs(m.r - b.r) + Mathf.Abs(m.g - b.g) + Mathf.Abs(m.b - b.b);
        return da < db;
    }

    [MenuItem("Tools/CardPresentation/对战自检")]
    public static void Run()
    {
        Directory.CreateDirectory(OutDir);
        Debug.Log(P + "=== 对战自检 开始 ===");

        // 🔴 **补间的推进方式必须在建场景之前就定死**（2026-09-19 踩到）。
        //    它原来只在「拖拽上场」那一节（本文件后面）设 —— 而**建场景时就会建一批补间**
        //    （手牌第一次同步时 `SetHighlightScale` / `SetOutline` 各建一条）。
        //    那些补间出生时 `Mode` 还是 `Normal` ⇒ 批处理下**永远不推进**（`Elapsed()` 恒 0.000），
        //    而后来建的补间是 Manual ⇒ 推进。症状极像「这条判据没生效」：颜色、材质、目标全对，
        //    就是不动。判据只有一处 —— 所以放在入口的第一行。
        CardTween.Mode = DG.Tweening.UpdateType.Manual;

        // 🆕 2026-09-27：**录像自检用临时目录** —— 绝不写进玩家的真录像夹（`persistentDataPath`）。
        //    与 `NetConfig.OverridePath` / `DeckStore.OverridePath` 同一套路。
        ReplayStore.OverrideDir = @"d:/4/_tmp_view/replays_selftest";
        ReplayStore.VerboseTrace = true;   // 自检开黑匣子（真打时关：一局 23 KB → 9 KB）
        ReplayStore.ResetForTest();

        // 🆕 **DOTween 补间报错的计数器**（2026-09-16 加）。
        //
        // 为什么要有它：构建后 player 验证在真包里抓到 **22 条**
        // `DOTWEEN ► Target or field is missing/null`（补间打在一个**已经销毁的 Transform** 上），
        // 而**编辑器自检一条都没有**。加这个计数器，是要把「编辑器 0 条」从**假设**变成**量出来的数** ——
        // 顺手也是一把**永久的尺子**：这条断言哪天变红，就说明编辑器这条路也看得见了。
        // （成因与两轮实验见 `资料/特效还原_进度与交接.md` §七。）
        int dotween = 0;
        Application.LogCallback dwCounter =
            (cond, stack, type) => { if (cond != null && cond.Contains("DOTWEEN")) dotween++; };
        Application.logMessageReceived += dwCounter;

        // ---- 🆕 2026-10-12（A175）：把两格**会改战场相机取景**的玩家设置压成确定的一档（关），收尾按原值放回 ----
        // 🔴 为什么必须压（两格同一个理由）：
        //   ① `AutoZoom.Enabled`（原版 `GameStaticData.useCombatAutoZoom` +0x125）—— 从 A175 起它**真的会改**
        //      战场相机的取景（`lensShift.y`）：关着 = 不缩放、开着 = 按原版曲线缩；
        //   ② `SmallScreenUI.Enabled`（+0x11c）—— 原版 `CombatCameraZoom.set_TargetZoomLevel` 里那道
        //      `if (smallScreenUI)` 把 zoom 上界压到 ~0.305（16:9），`ResetForBattle`/每次写目标值都要过它。
        //   不压的话，同一份自检在「玩家开着」与「玩家关着」两台机器上会量出**两个取景**
        //   ⇒ 13d 那组「卡在屏内 / 卡底边压在手牌之上」的断言就成了**随玩家偏好变红变绿**的
        //   （本工程红线：断言不许有隐藏状态）。
        // ⛔ **不许动玩家的真设置** ⇒ 全程 `PersistOverride = true`（只改内存、一个字节都不写盘），
        //   收尾按原值放回（`Auto Zoom` 在 `=== 结束` 之前那段，`Small Screen UI` 紧挨着它）。
        // ⚠️ 位置：**必须在 `BuildScene`（它会 `Begin`）之前** —— 那是一局的开头，取景就是在那一刻定下来的。
        bool azWasOn      = AutoZoom.Enabled;
        bool azWasChosen  = AutoZoom.ChosenManually;
        bool azWasPersist = AutoZoom.PersistOverride;
        AutoZoom.PersistOverride = true;
        AutoZoom.RestoreForTest(false, false);
        bool ssWasOn      = SmallScreenUI.Enabled;
        bool ssWasChosen  = SmallScreenUI.ChosenManually;
        bool ssWasPersist = SmallScreenUI.PersistOverride;
        SmallScreenUI.PersistOverride = true;
        SmallScreenUI.Set(false);

        Camera cam = BuildScene(out BattleDriver driver, out BoardLayout pBoard,
                                out BoardLayout eBoard, out CardInteraction it);
        // 🔴 2026-10-11（A321）**订正**：这一句原来写的是「批处理下 `AddComponent` **不会**触发 `Awake`
        //   （那是 Play 模式的事）」—— 那是**把局部推广成一般**，会让读者以为「批处理里 `SetActive(true)`
        //   也不跑生命周期」。实测到的是**两档**（2026-10-11 W3 三步坐实，判据 →
        //   `资料/普查产出_1011/W3_子2.md` §二·4·③ / §五·4）：
        //     · **不跑**那一档 = 组件建在**未激活的父链**下（A260 那条：`WindowsManager.Instance` 只在
        //       `Awake` 里赋 ⇒ 自检里恒 null ⇒ 「模式卡 `OpenMode` 报没有 WindowsManager」）；
        //     · **会跑**那一档 = 宿主**激活**时（日常页那批 TMP 的字真渲出来了，而 `MeshRenderer`/
        //       `MeshFilter`/`m_mesh` 只在它的 `Awake()` 里建）。
        // ⚠️ **订正到这里为止，剩下一条如实记「未查清」**：上面「会跑」那一档实测的宿主是 **TMP**
        //   （带 `[ExecuteAlways]`），而本程这三个宿主（`AttackSelector`/`TargetReticle`/`SkillPanel`，
        //   见 `BuildScene` 里那三处 `AddComponent`）建在**激活的** `sceneRoot` 下、**都没有**
        //   `[ExecuteAlways]` ⇒ 「两档的分界到底是**父链激活**、还是**类型带 `[ExecuteAlways]`**」没定论。
        //   ⛔ 别把「激活就会跑」当一般结论写进注释；这里**照旧显式补 `Build()`**（两种解释下都安全）。
        // 而选择器的按钮/底板、准星的 sprite/弧线都是运行时建在 `Build()` 里的 —— 这里显式补一次
        if (driver.selector != null) driver.selector.Build();
        if (driver.reticle != null) driver.reticle.Build();
        if (driver.skillPanel != null) driver.skillPanel.Build();
        driver.Begin(StarterCards.EmberFaction, StarterCards.TideFaction, 20260911);
        Step(0.3f);

        int pass = 0, fail = 0;

        void Check(bool ok, string msg)
        {
            if (ok) { pass++; Debug.Log(P + $"   ✓ {msg}"); }
            else { fail++; Debug.LogError(P + $"   ✗ {msg}"); }
        }

        var ctx = driver.Ctx;

        // ---- 1. 开局 ----
        Debug.Log(P + "--- 开局 ---");
        // 起手 3 张 + 第 1 回合抽 1 张（`Begin()` 里已经 BeginTurn 过了）= 4 张
        Check(ctx.Players[0].Hand.Count == RuleCore.StartHand + 1,
              $"手牌 {ctx.Players[0].Hand.Count} 张（起手 {RuleCore.StartHand} + 首回合抽 1）");
        Check(ctx.Players[0].Energy == 2, $"第 1 回合能量 {ctx.Players[0].Energy}（应 2）");
        Check(driver.HandCount == ctx.Players[0].Hand.Count, $"画面上的手牌 {driver.HandCount} 张 == 引擎的 {ctx.Players[0].Hand.Count} 张");
        Debug.Log(P + $"   画面上手牌：{driver.HandViewNames()}");
        Debug.Log(P + $"   引擎手牌　：{string.Join("/", HandNames(ctx, 0))}");
        Check(driver.MyUnits.Count == 1, "自己场上只有督军 1 个");
        Check(driver.FoeUnits.Count == 1, "对手场上也只有督军 1 个");
        Check(eBoard.SlotPosition(0).y > pBoard.SlotPosition(0).y, "对手的半场在自己的上面");
        Debug.Log(P + $"   我的阵营 {StarterCards.EmberFaction}，卡组 {DeckBuilder.ClassicDeckSize} 张，"
                    + $"手牌 {string.Join("/", HandNames(ctx, 0))}");
        Shot(cam, "01_开局");

        // ---- 🆕 2026-10-12（A457）：**实况宽高比的前置总闸**（= `16:9`）----
        // 🔴 **为什么单开这一条**：本文件里有一大批期望值**是按宽高比算出来的**（最刺眼的一条 =
        //   A175 那两条 `lensShift.y`：算式里带 `PadMod(aspect) = 0.9951903`，而那个 `0.9951903`
        //   只在 **16:9** 上成立）；它们的「前提」原来**只是注释里的一句话**，没有断言钉住。
        //   H18 查出过实据：A175 原来那条前提写的是 `aspect ∈ [1.333, 1.77]`，而**实况是 16:9 = 1.7777778
        //   —— 恰好落在那个区间之外** ⇒ 那条前提**当场就是死的**（它自己永远不会红，它守的那条期望值
        //   却是按 16:9 算的）。本行把那个隐含前提**显式化、并前移到整轮的第一个现场**。
        //   出处：`资料/普查产出_1012/H18_曲线订正与A417断言.md` §七·2（「同一族的风险」那段）。
        // 🔑 **宽高比只有一个写点** = 本文件的 `Shot()`（`cam.aspect = 1920f / 1080f`，3D 相机走同一句
        //   的那一支）；`ArenaBuilder.ConfigureBoardCamera` **不设** `aspect`（H18 §三·3 现读核过）
        //   ⇒ 这一条放在**第一次 `Shot` 之后**，钉的就是整轮里恒定的那一档。
        //   ⚠️ 第二句「`driver.boardCam` 就是 `Shot` 写的那一台」**不是同义反复**：`Shot` 是先
        //   `FindBoardCamera()` 找到相机再写它的 `aspect`（按 `depth < 0` 找），万一多出一台 `depth < 0`
        //   的相机，写与读就会分家 —— 那一档只有这一条能看出来。
        if (driver.boardCam == null)
        {
            Check(false, "（A457 前置总闸）`driver.boardCam` 在 —— 3D 战场相机不在就没法钉宽高比，"
                       + "下面所有按宽高比算出来的期望值都失去前提（⛔ 不静默跳过）");
        }
        else
        {
            Check(driver.boardCam == FindBoardCamera(),
                  "（A457 前置总闸）`driver.boardCam` **就是 `Shot()` 写 `aspect` 的那一台**"
                + "（`Shot` 按 `FindBoardCamera()`（`depth < 0`）找相机 ⇒ 两边必须是同一台）");
            Check(Mathf.Abs(driver.boardCam.aspect - 16f / 9f) < 1e-4f,
                  $"★ A457 实况宽高比 = **16:9**（`driver.boardCam.aspect` 实得 {driver.boardCam.aspect:F6}，"
                + $"16/9 = {(16f / 9f):F6}）—— 整轮按 16:9 算的期望值（A175 那两条里就带着 "
                + "`PadMod(16:9) = 0.9951903`）**全靠这一条当前提**；改坏法：把 `Shot()` 里那句 "
                + "`cam.aspect = (float)W / H` 改掉（或改成别的 W/H）⇒ 红");
        }
        Check(Mathf.Abs(cam.aspect - 16f / 9f) < 1e-4f,
              $"★ A457 …HUD 那台相机（= `Shot(cam, …)` 传进去的那个实参）同样是 16:9（实得 {cam.aspect:F6}）"
            + " —— 两台相机用的是同一句里的同一个式子，`aspect` 的写点只有 `Shot()` 一处");

        // ---- 1d. 特效：事件表里的名字在特效库里都找得到 ----
        Debug.Log(P + "--- 特效 ---");
        {
            Debug.Log(P + "   事件表 " + VfxMap.Describe(StarterCards.EmberFaction));
            Debug.Log(P + "   事件表 " + VfxMap.Describe(StarterCards.TideFaction));
            if (!WarpforgeVFX.WarpforgeEffectLibrary.Available)
            {
                Debug.Log(P + "   （特效库没加载 —— 跳过名字校验；卡牌流程不受影响）");
            }
            else
            {
                int n = 0, miss = 0;
                foreach (var name in VfxMap.AllNames())
                {
                    n++;
                    WarpforgeVFX.WFEffectEntry e;
                    if (WarpforgeVFX.WarpforgeEffectLibrary.Instance.TryGet(name, out e)) continue;
                    miss++;
                    Debug.LogWarning(P + "   库里没有这个特效：" + name);
                }
                Check(miss == 0, $"VfxMap 里 {n} 个特效名在库里都找得到（缺 {miss}）");
            }

            // 🔴 **每个事件都必须能解析出特效名**（2026-09-15 加）。
            //    判据是 `VfxMap.Events`（**唯一一份**）。原来 `PlaySignal` 的 switch 漏了 4 个 kind
            //    —— `Return` 与三个阵营资源事件 —— 它们**结构上永远不播**，
            //    而且**当时没有任何断言会红**（名字都在库里，只是永远查不到）。
            //    这条就是给那个形状上的锁。
            {
                int unmapped = 0;
                foreach (var ev in VfxMap.Unmapped())
                {
                    unmapped++;
                    Debug.LogWarning(P + "   这个事件没配特效（结构上永远不播）：" + ev);
                }
                Check(unmapped == 0, $"事件表里每个事件都配了特效（没配的 {unmapped} 个）");
            }

            // ---- 事件时序表（`EventTiming`）：数错一位整段动作的节奏就全乱，**截图看不出来** ----
            // 🔴 2026-09-18 改口径（全量反编译查实，规格见 `资料/普查产出_0918/第18行_UI三小条_规格.md` §③）：
            //    **出手那一下的判据是 `attackStepTime`（0.1），不是 `timeToChargeAttack`（0.35 没消费点）**；
            //    而且**近战/远程是两档**（0.10 / 0.20）⇒ `DurationOf` 加了带事件的重载。
            // 🆕 2026-09-29：**落位之后还有一拍**（原版 `_ResolvePlayCardFromHand` 的 state 4→5 =
            //    `WaitForSeconds(DAT_1834b2dc8 = 0.3)`），所以是 `1.0 + DeployLandHold` 而不是 1.0。
            //    判据 → `EventTiming.DeployLandHold` 的注释（那里也记了它与 §26 原文「0.6 全加在 Play 上」的出入）。
            Check(Mathf.Abs(EventTiming.DurationOf(EvtKind.Deploy) - (1.0f + EventTiming.DeployLandHold)) < 1e-3f,
                  $"登场 1.0 + {EventTiming.DeployLandHold}（`Summon Troop Tween` 的 DelayTween duration=1.0"
                + " + 落位之后那一拍）");

            var eMeleeAtk = new BattleEvent { Kind = EvtKind.Attack, Ranged = false };
            var eRangedAtk = new BattleEvent { Kind = EvtKind.Attack, Ranged = true };
            var eHit = new BattleEvent { Kind = EvtKind.Hit };

            Check(Mathf.Abs(EventTiming.DurationOf(eMeleeAtk) - EventTiming.AttackStepMelee) < 1e-3f,
                  $"近战出手 {EventTiming.AttackStepMelee}s（`attackStepTime`=0.1，位移完成即命中帧）");
            Check(Mathf.Abs(EventTiming.DurationOf(eRangedAtk) - EventTiming.AttackStepRanged) < 1e-3f,
                  $"远程出手 {EventTiming.AttackStepRanged}s（`ResolveAttackRangedAnim` 那一档）");
            Check(Mathf.Abs(EventTiming.DurationOf(EvtKind.Hit) - 0.75f) < 1e-3f,
                  $"挨打 {EventTiming.DurationOf(EvtKind.Hit)}s（`Impact Light Tween`：0.5 的旋转 Punch + 0.25 复位）");
            Check(Mathf.Abs(EventTiming.DurationOf(EvtKind.Ability) - 1.0f) < 1e-3f,
                  "技能 1.0s（原版 Mutation/Execution_BL/Vanguard/Hammer Slam 取中）");

            // 抬刀：出处是真反编译的 `_ResolveAttack_d__438__MoveNext.c:842`
            // `WaitForSeconds(attackStepTime × 2.0)`，`attackStepTime` 卡预制体实测 0.1
            Check(Mathf.Abs(EventTiming.DelayBetween(null, eMeleeAtk) - 0.2f) < 1e-3f,
                  $"一串里的第一条若是出手，先等**抬刀** {EventTiming.AttackWindUp}s（`attackStepTime 0.1 × 2.0`）");
            // 🔴 **2026-09-19 更正**：这里原来断言「近战命中 → 扣血再等 0.30s」，引的是
            //    `_AttackMeleeAnim…:258,260` —— 实读那两行是**命中之后**的 `AppendInterval(0.1)` + 归位 0.2。
            //    命中本身与**段1 位移的 OnComplete 同帧**（`…b__1.c:16-27`）⇒ 近战这一档的等待是 **0**。
            Check(Mathf.Abs(EventTiming.DelayBetween(eMeleeAtk, eHit) - EventTiming.MeleeImpactLag) < 1e-3f,
                  $"近战命中与出手**同一拍**收尾，不再等待（{EventTiming.MeleeImpactLag}s；"
                  + "原版 `__c__DisplayClass357_0___AttackMeleeAnim_b__1.c:16-27` 与位移完成同帧扣血）");
            Check(Mathf.Abs(EventTiming.DelayBetween(eRangedAtk, eHit) - EventTiming.RangedFlight) < 1e-3f,
                  $"远程命中 → 扣血等该 VFX 的时长 {EventTiming.RangedFlight}s"
                  + "（`_ResolveAttackRangedAnim…:122-124`；无全局常数，填死众数 1.0）");
            // **出手 → 扣血那一刻**：近战 0.10（命中帧）/ 远程 1.20。
            // 近战之后的收招（停 0.1 + 归位 0.2 = 0.3）**不在这一档里**，它由挨打动画的时长覆盖：
            // `DurationOf(Hit)` = `HitRotDuration 0.5 + ResetDuration 0.25 = 0.75` ≥ 0.3 ⇒ **序列总长仍是 0.4s**。
            Check(Mathf.Abs(EventTiming.DurationOf(eMeleeAtk) + EventTiming.DelayBetween(eMeleeAtk, eHit) - 0.10f) < 1e-3f,
                  "★ 近战 出手→扣血 = 0.10s（= `attackStepTime`，原版的命中帧）");
            Check(Mathf.Abs(EventTiming.DurationOf(eRangedAtk) + EventTiming.DelayBetween(eRangedAtk, eHit) - 1.20f) < 1e-3f,
                  "★ 远程 Attack→扣血 = 1.20s（0.20 + 1.00）");
            Check(EventTiming.DurationOf(EvtKind.Hit) >= 0.3f - 1e-3f,
                  $"★ 命中后的收招 0.3s（`:258-259` 停 0.1 + `:260,264-268` 归位 0.2）被挨打动画 "
                  + $"{EventTiming.DurationOf(EvtKind.Hit)}s 覆盖 ⇒ 整条近战序列仍是 0.4s");
            Check(Mathf.Abs(EventTiming.DelayBetween(null, eHit)) < 1e-3f, "一串里的第一条若不是出手，不等");

            int unsourced = 0;
            foreach (EvtKind k in System.Enum.GetValues(typeof(EvtKind)))
                if (!EventTiming.IsSourced(k)) unsourced++;
            // 🔴 2026-09-18 改口径：`VarsGlobal` 整表解出来之后，**阵亡也有出处了**
            //    （小兵 `deathTimeMinionDuration 0.2` / 督军 `deathTimeWarlordDuration 0.5`）
            //    ⇒ 「拍的」那一条**归零**。这一格原来断言 `unsourced == 1`，那条老断言正是
            //      把「查不到」钉死的形状（资产早就解出来了）。
            Check(unsourced == 0, $"**每一条事件时长都有原版出处**（没出处的 {unsourced} 条）"
                  + "—— 阵亡那一条 2026-09-18 从 `VarsGlobal` 补上了");
            Debug.Log(P + "   事件时序（出处）：");
            foreach (EvtKind k in System.Enum.GetValues(typeof(EvtKind)))
                Debug.Log(P + $"     {k,-8} {EventTiming.DurationOf(k):F2}s　← {EventTiming.SourceOf(k)}");
        }

        // ---- 1e. 「临时卡（Ephemeral）」角标 ----
        // 规则书 `:183`/`:229` 说临时卡「回合结束若在手牌则移除，**从游戏中移除（非弃置）**」——
        // 玩家得**看得出哪张是临时的**，否则牌忽然没了就是「静默失败」。
        // ⚠️ 这一段只验**表现层这一侧**（引擎侧的机制在 `RuleEngineTest.TestEphemeral` 里）：
        //    图标导进来了没有、角标画不画得出来。
        Debug.Log(P + "--- 临时卡（Ephemeral）角标 ---");
        {
            var ephIcon = CardArt.Trait("ephemeral");
            Check(ephIcon != null,
                  "原版关键词图标 `Atlas_trait_icon_ephemeral` 已经导进 `Resources/Art/traits/`"
                  + "（跑 `工具/import_original_art.py`）");
            if (ephIcon != null)
                Debug.Log(P + $"   图标 {ephIcon.width}×{ephIcon.height}");

            // 拿**手牌里那张真实的卡**当数据源，另建一个视图来试「开/关」两条路。
            // ⚠️ 不直接改手牌里那张视图 —— `ShowEphemeral` 的状态由 `SyncHand` 每轮重刷，
            //    在这儿改了会被下一轮覆盖，测出来的东西不可信。
            var sample = driver.HandViewAt(0);
            Check(sample != null, "手牌里有牌可以当数据源");
            if (sample != null && ephIcon != null)
            {
                var probe = CardView.Create(driver.transform, sample.Data, "EphProbe");
                // 摆到可见区中央偏上，拍两张**只差角标**的图 —— 光看一张分不出「画没画」。
                // ⚠️ **放大 1.6 倍**：手牌尺寸下（165×263 px）80×80 的图标只有 24 px 左右，
                //    截图上几乎看不出差别 —— 尺子太小会把「画了」读成「没画」。
                probe.transform.localPosition = new Vector3(0f, 0.9f, -0.5f);
                probe.transform.localScale = Vector3.one * 1.6f;
                Check(!probe.EphemeralShown, "新造的卡**默认不带**角标（临时卡是少数）");
                Shot(cam, "27a_临时卡角标_关");

                probe.ShowEphemeral(true);
                Check(probe.EphemeralShown, "★ 打开之后 `EphemeralShown` 为真（角标真的画出来了）");
                Shot(cam, "27b_临时卡角标_开");

                probe.ShowEphemeral(false);
                Check(!probe.EphemeralShown, "★ 关掉之后为假 —— 不然一张临时卡打出去后，"
                      + "接手它那个视图的普通卡会**一直带着角标**");
                Object.DestroyImmediate(probe.gameObject);
            }
        }

        // ---- 1e·2. 🆕 状态环照原版（2026-09-29：原来是自造羽化图 + 整卡染黄）----
        // 判据 → `资料/待办判据_战场与战斗视图.md` §8b：原版那层是 `MinionLight` 上的 SpriteRenderer，
        // sprite `Card board frame SDF` · 材质 `Card board Frame SDF` · shader
        // `Everguild/FX/Card Highlight And Shadow`；**6 态**，`regular` 的 alpha 是 0（不亮）。
        // 🔴 抓的就是那处**误标**：「打得出去」在原版走 SDF `_Outline`，**不是**这层黄环。
        Debug.Log(P + "--- 状态环（原版 `FrameHighlight`）---");
        {
            var src = driver.HandViewAt(0);
            if (src != null)
            {
                var probe = CardView.Create(driver.transform, src.Data, "RimProbe");
                probe.SetHighlight(CardHighlightState.Playable);
                Check(!probe.RimVisible,
                      "★ 「打得出去」（`Playable`）**不点亮状态环** —— 原版那层黄色是「正在展示主动技能」，"
                    + "playable 走 SDF `_Outline`（原来我们这儿是整卡染黄 + 黄环，两层混成一层）");
                probe.SetHighlight(CardHighlightState.ValidTarget);
                Check(probe.RimVisible, "合法目标 → 状态环亮起来");
                // 🔴 **2026-09-29：那圈现在是【补间】的**（原版 `ChangeFrameColor` 0.1s）⇒ 读色之前必须把
                //   补间推完；原来这里直接读，读到的是材质初始值 (1,1,1)（自检实测就是这么红的）。
                Step(CardHighlight.AnimTime + 0.05f);
                var rc = probe.RimColor;
                Check(Mathf.Abs(rc.r - 0f) < 0.02f && Mathf.Abs(rc.g - 1f) < 0.02f && Mathf.Abs(rc.b - 0.1294f) < 0.02f,
                      $"…而且是**原版那个绿 `#00FF21`**（实得 ({rc.r:F3}, {rc.g:F3}, {rc.b:F3})）");
                // 🆕 2026-09-29：**补间** —— 原版 `ChangeFrameColor` 补 0.1s（`CardHighlightAnimTime`）。
                //   ⚠️ 必须验「途中」那一档：只验首尾的话，「瞬变」也能过。
                probe.SetHighlight(CardHighlightState.Normal);      // 先回到关
                Step(CardHighlight.AnimTime + 0.05f);
                probe.SetHighlight(CardHighlightState.ValidTarget);
                Step(0.02f);                                        // 补间刚起步
                var rcMid = probe.RimColor;
                // 起点是**透明黑**（上一档 `Normal` 走完留下的 (0,0,0,0)）、终点是**绿** ⇒ 看 **g 分量**：
                // 补间途中应在 0~1 之间；**瞬变**会直接是 1 ⇒ 用「< 0.98」把它排除掉。
                Check(rcMid.g > 0.02f && rcMid.g < 0.98f,
                      $"★ 那圈状态色**是补间出来的**（t=0.02s 时 g={rcMid.g:F3}，还在 0 → 绿的半路上；瞬变会是 1）");
                Step(CardHighlight.AnimTime + 0.05f);
                var rcEnd = probe.RimColor;
                Check(Mathf.Abs(rcEnd.g - 1f) < 0.02f && Mathf.Abs(rcEnd.a - CardHighlight.RimAlphaScale) < 0.02f,
                      $"…走完 = 原版那个绿 + 材质常量 alpha（a={rcEnd.a:F3}，原版材质 `_Outline.a` = 0.447）");
                // 另一档：原版 **state 2 `selected` 传的时长是 0 = 瞬切**（`ChangeState` case 2）
                probe.SetHighlight(CardHighlightState.Selected);
                var rcIm = probe.RimColor;
                Check(Mathf.Abs(rcIm.r - 1f) < 0.02f && Mathf.Abs(rcIm.g - 1f) < 0.02f
                      && Mathf.Abs(rcIm.a - CardHighlight.RimAlphaScale) < 0.02f,
                      $"★ `selected` 是**瞬切**（原版 `ChangeState` case 2 走的是 `uVar2 = 0` 那一支）"
                    + $"：一置上就是白（实得 ({rcIm.r:F2},{rcIm.g:F2},{rcIm.b:F2},a={rcIm.a:F3})）");
                probe.SetHighlight(CardHighlightState.Normal);
                // 🆕 2026-09-29：原版是**淡出**（`ChangeFrameColor` 补 0.1s，alpha 到 0 之后才
                //   `ToggleFrames(false)`）⇒ 关的那一帧还亮着，要先把补间推完再判
                Step(CardHighlight.AnimTime + 0.05f);
                Check(!probe.RimVisible,
                      "常规态 → 环**淡出之后**关掉（原版 `regular` 的 alpha 就是 0；补间走完才 `ToggleFrames(false)`）");
                Check(probe.RimShaderName == "Everguild/FX/Card Highlight And Shadow",
                      $"★ 这层用的是**原版 shader**（现在 `{probe.RimShaderName}`）"
                    + "—— 原来是我们自造的 `Sprites/Default` + 那张程序生成的羽化图");
                Check(probe.RimTexName != null && probe.RimTexName.Contains("Card_board_frame_SDF"),
                      $"…贴的是**原版那张图**（`{probe.RimTexName}`，79×107 @(25,11)）");
                // 🆕 2026-09-29：**摆位也照原版**（`3DBody/MinionLight`：pos (−0.001, 1.326, 0) ·
                //   scale (3.1555, 3.0920) × sprite 0.79×1.07 ⇒ 实绘 **2.4928 × 3.3084**）。
                //   ⚠️ 环圈的是**看得见的那张卡**：手牌是 2D 卡（居中）、场上是 3D 卡体（中心在卡中心下方 0.3397）
                //   ⇒ 两档几何不同，而**换档入口只有 `SetFace` 这一处**（铁律 10 第 5 条）。
                var rimHand = probe.RimSize;
                Check(Mathf.Abs(rimHand.x - 1.09f * 2.0927f) < 0.01f && Mathf.Abs(rimHand.y - 1.06f * 3.3313f) < 0.01f,
                      $"手牌那一档：环仍居中、尺寸沿用 1.09×1.06（实测 {rimHand.x:F3} × {rimHand.y:F3}）");
                probe.SetFace(CardFace.Board);
                var rimBoard = probe.RimSize;
                Check(Mathf.Abs(rimBoard.x - 2.4928f) < 0.01f && Mathf.Abs(rimBoard.y - 3.3084f) < 0.01f,
                      $"★ 切到场上 ⇒ 环 = **原版 `MinionLight` 的实绘尺寸 2.4928 × 3.3084**"
                    + $"（实测 {rimBoard.x:F4} × {rimBoard.y:F4}）");
                Check(Mathf.Abs(probe.RimLocalPos.y - (1.326f - CardView.Height * 0.5f)) < 0.01f,
                      $"…而且落在 **3D 卡体的中心**（卡中心下方 0.3397，实得 {probe.RimLocalPos.y:F4}；"
                    + "与网格中心 −0.343 差 0.003 —— 原版节点坐标与我们的网格包围盒**两条独立路径互证**）");
                Check(probe.RimLocalPos.z > 0f,
                      $"…并且贴在卡体**后面**（z={probe.RimLocalPos.z:F3}；原版靠 `SortingOrder = −1` + `zTest LEqual`"
                    + " 达到同一件事：环被卡体挡住中间、只在轮廓外沿露出来）");
                Object.DestroyImmediate(probe.gameObject);
            }
            else Debug.Log(P + "   （没有手牌视图，跳过状态环用例）");
        }

        // ---- 1f. 🆕 棋盘单位卡的 buff/debuff 徽标（原版 `BattleCardUI.boardTraitIcons`）----
        // 出处与「哪些是我们挑的」全在 `Core/Badges.cs` 的文件头；这里只验它真画出来了、
        // 画的是不是**该画的那一枚**（截图看不出这个 —— 得断言贴图）。
        Debug.Log(P + "--- 棋盘徽标（buff/debuff）---");
        {
            Check(Badges.MaxSlots == 7, "7 个位（原版左 3 + 右 4，第 8 个关键词不再显示）");

            // ① 判据：关键词 → 图（**大小写/空格不统一**，靠归一后查表）
            Check(Badges.SpriteOf("Armour 2") == "armour", "`Armour 2` → `armour`（剥掉数值）");
            Check(Badges.SpriteOf("Blast 2.") == "blast", "`Blast 2.` → `blast`（剥掉数值与句点）");
            Check(Badges.SpriteOf("Flying") == "flying", "`Flying` → `flying`");
            Check(Badges.SpriteOf("Blood Thirst") == "bloodThirst", "`Blood Thirst` → `bloodThirst`（空格 + 驼峰）");
            Check(Badges.SpriteOf("Destroyer") == "frenzied",
                  "★ `Destroyer` → `frenzied` —— **图集里没有 `destroyer.png`**，按 token 名猜会画不出来");
            Check(Badges.SpriteOf("Talent: Skyborne Deployment") == "talent",
                  "`Talent: xxx` 取冒号前的词 → 图集里**真有** `talent.png`");
            Check(Badges.SpriteOf("Lord Commander") == null,
                  "★ 认不出就是 null（**不猜**）—— `Lord Commander` 这种图集里没有");
            Check(Badges.SpriteOf(null) == null, "空关键词不炸");

            // ② 角标：判据 = **原版那条「引擎值 ≥ 1 就画」**在我们数据里的等价物
            //    （①卡面原文里有数字 ②引擎值 ≥ 2 ③规则书那张表 —— 见 `Badges.CarriesValue` 的注释）。
            //    🔴 **2026-09-29 改口径**：原版 `BoardTraitIcon.Initialize` 是 `counter < 1` 才换
            //    「Without counter」那一支，而基值来自 `CardTrait.GetNewTrait(..., defaultValue = 0)`
            //    ⇒ **卡面没写数字的词条基值就是 0、不画**。不能直接照抄「值 ≥ 1」——
            //    我们的引擎对没有数字的关键词**兜底给 1**（`KeywordTable.FirstNumber`）。
            Check(Badges.CarriesValue("Armour 2") && Badges.CarriesValue("Hunt Mark 1"),
                  "带数值的（Armour / Hunt Mark）画角标");
            Check(!Badges.CarriesValue("Flying") && !Badges.CarriesValue("Rally"),
                  "不带数值的（Flying / Rally）不画角标（引擎里它们的值也是 1 —— **兜底值**，不是真数值）");
            Check(Badges.CarriesValue("Flying", 2),
                  "★ 引擎值 ≥ 2 时照样画（被 modifier 抬上去的 —— 原版这时也会画）");
            Check(Badges.CarriesValue("Sentry", 1, new System.Collections.Generic.List<string> { "sentry" }),
                  "★ 卡面原文里有数字（`CardDef.NumericKeywords`）时也画 —— 这条是数据侧的判据");

            // ③ 位子就是原版预制体的那 7 个（`TraitIconContainer*.json` **实读**，见 `Badges.BodyY`）
            // 🔴 **2026-09-20 更正**：这条原来钉的是 **+0.99** —— 那是按
            //    `ourY = (origY/2.96 − 0.5) × 3.3313` 这个**错式子**算出来的（既乘了 1.125 又按 3.3313 拉伸，
            //    而 `TraitIcons` 是 `3DBody` 的孩子、和 `Card 3D` **平级**，不该乘 0.88586）。
            //    真值 = `TraitIcons.y 1.533` + 容器 y `0.826` − 半卡高 `3.3313/2` = **+0.6934**。
            //    判据链与实测过程 = `资料/3DBody_原版场上卡体规格.md` §四之二。
            Check(Mathf.Abs(Badges.SlotAt(0).x + 0.563f) < 0.002f && Mathf.Abs(Badges.SlotAt(0).y - 0.6934f) < 0.01f,
                  $"左 1 的**容器**在 (−0.563, +0.6934)（实为 {Badges.SlotAt(0).x:F4}, {Badges.SlotAt(0).y:F4}）"
                  + " —— 原版 `TraitIcons`(0.296,1.533) + `TraitIconContainer 1`(−0.859,0.826) − 半卡高");
            Check(Mathf.Abs(Badges.SlotAt(3).y - 0.6934f) < 0.01f && Mathf.Abs(Badges.SlotAt(6).y + 0.6117f) < 0.01f,
                  $"右列也在真值上（右 1 {Badges.SlotAt(3).y:F4} / 右 4 {Badges.SlotAt(6).y:F4}；应 +0.6934 / −0.6117）");
            // 🔴 **容器 ≠ 图标**：图标是容器下的 `Container` 子节点，还要往卡外偏 `∓0.287`（容器 scale 0.750）
            //    ⇒ 0.21525。底板 `IconBackground` 偏 `∓0.300` ⇒ 0.225、并下移 0.015。
            //    原来图标**直接画在容器位置上**（整体偏内 ≈ 卡宽 10%），底板被设成「正后方」（= 0）——
            //    那条的理由是「我们是平面 2D 卡」，随 3D 卡体一起作废了。
            Check(Mathf.Abs(Badges.IconOutward - 0.21525f) < 0.001f && Mathf.Abs(Badges.PlateOutward - 0.225f) < 0.001f
                  && Mathf.Abs(Badges.PlateDy + 0.015f) < 0.001f,
                  "★ 图标/底板相对容器的偏移 = **原版真值**（0.21525 / 0.225 / −0.015）—— 不是 0");
            Check(Mathf.Abs(Badges.SlotAt(6).x - 0.563f) < 0.002f && Badges.SlotAt(6).y < Badges.SlotAt(5).y,
                  "右 4 在右下（原版右列比左列多一个位）");

            // ④ 全卡池覆盖：**认不出图标的报数**（红线：不许静默少画）
            {
                int cards = 0, withBadges = 0, unresolved = 0;
                var missing = new System.Collections.Generic.SortedSet<string>();
                foreach (var c in RuleEngine.CardDatabase.Load())
                {
                    cards++;
                    var b = Badges.For(c.Keywords, c.Desc);
                    if (b.Count > 0) withBadges++;
                    foreach (var kv in c.Keywords)
                        if (Badges.SpriteOf(kv.Key) == null) { unresolved++; missing.Add(kv.Key); }
                }
                Debug.Log(P + $"   全卡池 {cards} 张：能画出徽标的 {withBadges} 张，"
                            + $"认不出图的关键词 {unresolved} 处 / {missing.Count} 种");
                foreach (var m in missing) Debug.Log(P + $"     认不出：`{m}`");
                Check(cards > 1000, "卡池读到了");
                Check(withBadges > 400, "过半数的卡至少有一枚徽标（原版这些卡身上就是有关键词的）");
            }

            // ④·B 🆕 2026-09-21：**卡面「关键词段」必须带图标**（原版卡面印的是「图标 + 词」）。
            //   判据与画法**只在** `CardText.KeywordSegment` 一处；这里只验它的输出。
            //   正例 `Howling Banshee`（`ASH20`）：`keywords` 是 `Waystone`/`Flank`，而 `desc` 只有
            //   `Rally: Stun an enemy` ⇒ **这两个字在正文里一个字都没有**，只能从这一段印出来。
            //   PnP 成品卡面印的正是 `◈Waystone.  ⬇Flank.`
            //   （`Aeldari/3部队/Warpforge_20_Howling-Banshee.png`，主对话逐张核过，铁律 7）。
            //   原版写法出处（全量反编译）：`GameStaticData__TraitNameToString.c:75-76`
            //   = `<nobr>` + `<sprite name=…>` + 词 + `</nobr>`，**图标与词之间不留空格**。
            {
                var all = RuleEngine.CardDatabase.Load();
                var banshee = CreatePool.FindByName(all, "Howling Banshee");
                Check(banshee != null, "卡池里有 `Howling Banshee`（关键词段的验收卡）");
                if (banshee != null)
                {
                    string seg = CardText.KeywordSegment(banshee.Keywords, false, banshee.Desc);
                    Check(seg.Contains("<sprite name=\"waystone\">") && seg.Contains("<sprite name=\"flank\">"),
                          "★ 关键词段带图标（`Waystone`/`Flank` 都不在 desc 里）—— 实得「" + seg + "」");
                    Check(seg.Contains("Waystone") && seg.Contains("Flank"),
                          "★ ……而且**词还印着**（不能只剩图标）—— 实得「" + seg + "」");
                    Check(seg.Contains("<nobr>") && seg.Contains("</nobr>"),
                          "★ 每一项包了 `<nobr>`（不包的话 TMP 会把图标与词拆到两行）—— 实得「" + seg + "」");

                    // 反例①：`desc` 本身就等于关键词列表的 ⇒ **一个字都不补**（`Lychguard` 实测
                    //         `desc` = `Remnant. Armour 2. Vanguard`，与 `keywords` 一字不差）
                    var lych = CreatePool.FindByName(all, "Lychguard");
                    Check(lych != null && CardText.KeywordSegment(lych.Keywords, false, lych.Desc) == "",
                          "★ 反例：`Lychguard` 的 desc 等于关键词列表 ⇒ 关键词段为空");

                    // 反例②：**表外词不进这一段**。⚠️ 只有**中文**这一侧会挡 —— `KeywordEn` 对任何键
                    //        都有兜底（首字母大写），`KeywordZh` 才是查表、查不到返回 null。
                    //        （原注释写「`lord commander` 这类表外词不补」，说的就是中文这条路。）
                    var fake = new Dictionary<string, int> { { "lord commander", 1 } };
                    Check(CardText.KeywordZh("lord commander") == null && CardText.KeywordEn("lord commander") != null,
                          "表外词：中文名查不到（null）、英文名有兜底 —— 判据就在这里分叉");
                    Check(CardText.KeywordSegment(fake, true, "") == "",
                          "★ 反例：表外词在**中文**卡面上根本不进这一段");
                }
            }

            // ⑤ 真渲染：造一张**带 4 枚徽标**的卡，拍「有 / 无」两张图（只差徽标那几层）
            {
                var badges = new System.Collections.Generic.List<Badge>
                {
                    new Badge { sprite = "armour",   counter = 2, active = true },
                    new Badge { sprite = "blast",    counter = 3, active = true },
                    new Badge { sprite = "flying",   counter = 0, active = true },
                    new Badge { sprite = "vulnerable", counter = 1, active = true },
                };
                var d = CardData.Simple("BadgeProbe", 3, 3, 4);
                d.artId = "UM_Heavy_Intercessor";  // 借一张有立绘的卡，截图里好看（立绘按 **id** 取名）
                d.badges = badges;
                var probe = CardView.Create(driver.transform, d, "BadgeProbe");
                probe.transform.localPosition = new Vector3(-0.62f, 0.86f, -0.5f);
                probe.transform.localScale = Vector3.one * 1.9f;
                Check(probe.BadgesShown == 4, $"4 枚都画出来了（实际 {probe.BadgesShown}）");
                Check(probe.BadgeTexture(0) != null && probe.BadgeTexture(0).name.ToLower().Contains("armour"),
                      "★ 第 1 枚画的**就是** `armour` 那张图（不是随便一张）");
                Check(probe.BadgeCounter(0) == "2" && probe.BadgeCounter(1) == "3",
                      "角标数字 = 关键词的值（护甲 2 / 爆裂 3）");
                Check(probe.BadgeCounter(2) == "", "不带数值的关键词（Flying）没有角标");
                // 🆕 2026-09-29：**未激活态**（原版 `BoardTraitIcon.Initialize` 的 `traitEnabled == false`
                //    ⇒ 换 `disabledMaterial` = `Sprite Greyscale`，**位置和大小都不变**）。
                Check(!probe.BadgeGreyed(0), "激活态的徽标不灰");
                badges[0] = new Badge { sprite = "armour", counter = 2, active = false };
                // ⚠️ **卡变了要重建视图**（`CardView` 只更新已存在的层，不会补缺的）
                probe.SetData(d);
                Check(probe.BadgeGreyed(0),
                      "★ 未激活态 = **同一张图换灰化材质**（原版 `disabledMaterial` / `_GreyScale`）——"
                    + " 不是换图、也不是缩小");
                Shot(cam, "28a2_徽标_未激活态");
                badges[0] = new Badge { sprite = "armour", counter = 2, active = true };
                probe.SetData(d);
                // 🆕 2026-09-29：**脉冲高亮**（原版 `BoardTraitIcon.HighlightIcon`——
                //    `DOScale(原始 × 1.6, 0.5s)`，`SetLoops(2, Yoyo)`，`OutCubic`）
                int pulsesBefore = probe.PulseCount;
                probe.PulseBadge(0);
                Check(probe.PulseCount == pulsesBefore + 1, "★ 脉冲高亮播了一次（原版 `HighlightIcon` 的 `DOScale ×1.6`）");
                probe.PulseBadgeByKeyword("does-not-exist");
                Check(probe.PulseCount == pulsesBefore + 1, "认不出的关键词**不脉冲**（不猜哪一位）");
                // 🆕 2026-09-29：**整组淡入淡出**（原版 `FadeAllTraitsIcons(1.0, 0.3)`，
                //    全量反编译里唯一的调用点是 `CardScript.<HeroLandIntoField>`）
                probe.SetBadgeAlpha(0f);
                Check(probe.BadgeAlpha < 0.01f, "★ 整组徽标能单独淡到 0（原版 `NestedFadeGroup.alpha`）");
                probe.FadeBadges(1f, 0.3f);
                Step(0.4f);
                Check(probe.BadgeAlpha > 0.99f, $"★ 0.3 s 之后淡回实心（实测 {probe.BadgeAlpha:F3}）");
                probe.SetBadgeAlpha(1f);
                Shot(cam, "28a_徽标_四枚");
                Check(probe.SetBadges(null) == 0 && probe.BadgesShown == 0,
                      "★ 清空后一位都不剩（原版每轮重算前也是先全部 Toggle(false)）");
                var kept = probe.BadgeTexture(0);     // 清空只关层、不销毁
                Check(kept != null, "清空只是关掉层，贴图还在（下次复用）");
                Shot(cam, "28b_徽标_清空");
                Object.DestroyImmediate(probe.gameObject);
            }

            // ⑤·b 🆕 2026-09-29：**场上四个数值各占一个图层** + 涨落变色 + bump
            //      （原版 `CardTextCountersController.DoColorChange` / `cardTextBumpSize`，
            //       判据 → `资料/待办判据_战场与战斗视图.md` §26 第 3 条）
            {
                var sd = CardData.Simple("StatProbe", 3, 4, 5);
                sd.melee = 3; sd.ranged = 2; sd.armor = 1; sd.health = 5; sd.isUnit = true;
                sd.artId = "UM_Heavy_Intercessor";
                var sv = CardView.Create(driver.transform, sd, "StatProbe", CardFace.Board);
                sv.transform.localPosition = new Vector3(-1.5f, 0.86f, -0.5f);
                sv.transform.localScale = Vector3.one * 1.9f;
                Check(sv.StatShown(CardView.StatMelee) == 3 && sv.StatShown(CardView.StatRanged) == 2
                      && sv.StatShown(CardView.StatArmour) == 1 && sv.StatShown(CardView.StatHealth) == 5,
                      "★ 场上四个数值**各占一层**、画的都是自己那个数（原来烘在一张图里，单个动不了）"
                    + $" —— 实测 [{sv.StatShown(CardView.StatMelee)}, {sv.StatShown(CardView.StatRanged)},"
                    + $" {sv.StatShown(CardView.StatArmour)}, {sv.StatShown(CardView.StatHealth)}]"
                    + $"（isUnit={sd.isUnit} · Pragati={PragatiDigits.Available} · face={sv.Face}）");
                Check(sv.StatFlash(CardView.StatHealth) == Color.white, "刚建出来没有闪现色");

                // 掉 2 点血 → 生命那一格**变落色 + bump**
                int bumps = sv.StatBumps;
                sd.health = 3;
                sv.SetData(sd);
                Check(sv.StatFlash(CardView.StatHealth) == CardFeel.StatDownColor,
                      "★ 掉血 ⇒ 只**生命那一格**变落色（原版 `DoColorChange`：`new < old` 取 +0x28 那一组）");
                Check(sv.StatFlash(CardView.StatMelee) == Color.white, "别的格不受影响（这正是要拆层的原因）");
                Check(sv.StatBumps == bumps + 1, "★ bump 播了一次（原版 `cardTextBumpSize 0.75` / `Time 0.45`）");
                Shot(cam, "28c_数值_掉血变色");

                // 同值再刷一次 → 颜色刷回原色（原版 `old == new` 那一支）
                sv.SetData(sd);
                Check(sv.StatFlash(CardView.StatHealth) == Color.white,
                      "★ 同值刷新后刷回原色（原版那句 `if (param_2 == param_3)`）");

                // 加 1 点攻 → 涨色
                sd.melee = 4;
                sv.SetData(sd);
                Check(sv.StatFlash(CardView.StatMelee) == CardFeel.StatUpColor, "涨 ⇒ 涨色");
                Object.DestroyImmediate(sv.gameObject);
            }

            // ⑤·c 🆕 2026-10-05（A90②）：**换卡面材质不许把显式分好的渲染队列抹掉**
            //      （= A85 那条工程级不变量的**漏网处**：`Core/CardView.cs:969-975` 的修 2026-10-03 就落了，
            //       但**此前没有任何断言盯着它**，所以这一块单开）
            //  为什么盯在这里：分层**只靠渲染队列**（`CardFan.SetCardQueue`），而 `SetData` 那条兜底支路
            //  用 `FaceMaterial(d)` = `new Material(_quadMat)` 换掉 `_face` 的材质，基材 `Sprites/Default`
            //  的 SubShader 标签是 `QUEUE: Transparent` = **3000** ⇒ 不写回旧队列就**静默**掉档。
            //  谁真会踩到（判据在 `CardView.cs:939-968`）：**删掉 `Resources/Art/` 那一档** —— 那时每张卡都走
            //  这条支路，被整块日志面板（4000）盖住 = 画面上「日志里点出来的那张卡是空的」，
            //  而灰化那类断言**只看 shader 名** ⇒ 掉了也全绿。
            //  🔴 **改坏就红**：把 `CardView.cs` 那句 `if (qFace >= 0) fm.renderQueue = qFace;` 删掉
            //     （或改成无条件写 3000）⇒ 新材质带回 SubShader 的 3000 ⇒ 下面那条断 4444 必红。
            {
                var qd = CardData.Simple("QueueProbe", 3, 3, 4);   // `faction = null` ⇒ `frameTex == null`
                var qv = CardView.Create(driver.transform, qd, "QueueProbe");
                var qmr = qv.GetComponent<MeshRenderer>();   // 兜底支路的 `_face` = 根节点这个渲染器（`CardView.cs:1485`）
                Check(qmr != null && qmr.enabled,
                      "前提：这张卡确实走了**兜底卡面**支路（`CardData.Simple` 的 `faction = null`；"
                    + "有卡框时 `Build` 会把这个根渲染器 `enabled = false`，`CardView.cs:1343`）"
                    + " —— 这条不成立，下面两条就是空转");
                CardFan.SetCardQueue(qv, 4444);             // 显式分层（卡内所有层一起平移）
                var matBefore = qmr.sharedMaterial;
                qv.SetData(qd);                             // 同一份数据换一次 ⇒ 走 `_face` 换材质那一段
                Check(qmr.sharedMaterial != matBefore,
                      "`SetData` 确实**换了一份新材质**（否则下面那条是在空转路径上断的）");
                Check(qmr.sharedMaterial.renderQueue == 4444,
                      $"★ 换卡面材质**保留了**显式分好的渲染队列 4444（实测 {qmr.sharedMaterial.renderQueue}）"
                    + " —— `Core/CardView.cs:969-975`；删掉那句写回就退回 3000、这条红");
                Object.DestroyImmediate(qv.gameObject);
            }

            // ⑥ 场上真单位：**每张卡「画出来的数量」必须等于「它拿到的徽标数」**
            //    （这一条不依赖「此刻场上正好有带关键词的卡」，是条恒等式，什么时候都成立）
            {
                int total = 0, withBadges = 0, bad = 0;
                foreach (var kv in driver.MyUnits)
                {
                    total++;
                    if (kv.Value == null) continue;
                    int want = kv.Value.Data.badges != null ? kv.Value.Data.badges.Count : 0;
                    if (kv.Value.BadgesShown != want) bad++;
                    if (want > 0) withBadges++;
                }
                foreach (var kv in driver.FoeUnits)
                {
                    total++;
                    if (kv.Value == null) continue;
                    int want = kv.Value.Data.badges != null ? kv.Value.Data.badges.Count : 0;
                    if (kv.Value.BadgesShown != want) bad++;
                    if (want > 0) withBadges++;
                }
                Debug.Log(P + $"   场上有卡 {total} 张，其中带徽标的 {withBadges} 张");
                Check(total > 0, "场上有卡");
                Check(bad == 0, "★ 每张场上卡「画出来的徽标数 == 数据里的徽标数」（这张不等就是漏画/多画）");
                // ⚠️ 这一刻**可能一张带关键词的卡都没有**（测试脚本刚开局）—— 所以这条只记数不判死，
                //    真渲染那条路由上面第 ⑤ 步的探针卡 + 整局截图盯着（`28a_徽标_四枚.png`）。
                Debug.Log(P + (withBadges > 0 ? "   本刻场上有带徽标的卡 ✓" : "   本刻场上没有带徽标的卡（不是失败）"));
            }
        }

        // ---- 1b. 版面：原版实测数值对不对得上 ----
        Debug.Log(P + "--- 版面 ---");
        {
            float visW = LayoutSpace.VisibleWidth;
            var hand = PlayerHand();

            // ① 棋盘：9 槽跨度 / 场卡宽 —— 都是「占可见宽度的比例」，换分辨率也该成立
            float cardW = CardView.Width * pBoard.placedScale * LayoutSpace.Scale;
            float step = Mathf.Abs(pBoard.SlotPosition(1).x - pBoard.SlotPosition(0).x);
            float span = step * 8f + cardW;
            float spanRatio = span / visW, cardRatio = cardW / visW;
            float cardH = CardView.Height * pBoard.placedScale * LayoutSpace.Scale;
            Debug.Log(P + $"   棋盘：9 槽跨度 {spanRatio * 1920f:F1} px 占比 {spanRatio:P1}（原版 1331.6 / {OriginalBoardSpanRatio:P1}）"
                        + $"　场卡 {cardRatio * 1920f:F1}×{cardH / visW * 1920f:F1} px（原版 137.2×218.4）"
                        + $"　中心距 {step / visW * 1920f:F1} px（原版 149.3）");
            Check(Mathf.Abs(spanRatio - OriginalBoardSpanRatio) < 0.01f, "9 槽跨度 = 原版的 69.4% 可见宽");
            Check(Mathf.Abs(cardRatio - OriginalCardWidthRatio) < 0.004f, "场卡宽 = 原版的 137.2 px");
            // 🆕 2026-09-12：**高度**这条原来没有 —— 宽一直是对的，高矮了 26 px（192.4 vs 218.4），
            //    根因是卡本体比例被我们改成 1.45×2.03（0.714）而不是原版的 2.0927×3.3313（0.628）。
            //    现在两样都断言，改坏哪一样都会红。
            Check(Mathf.Abs(cardH / visW * 1920f - BoardCardHeightPx) < 2f,
                  $"场卡高 = 原版的 {BoardCardHeightPx} px");

            // 手牌卡同一条道理：宽对了不算数，**高**也要对（原来只有宽是对的）
            float handW = CardView.Width * HandScale * LayoutSpace.Scale;
            float handH = CardView.Height * HandScale * LayoutSpace.Scale;
            Debug.Log(P + $"   手牌卡 {handW / visW * 1920f:F1}×{handH / visW * 1920f:F1} px（原版 165.0×{HandCardHeightPx}）");
            Check(Mathf.Abs(handW / visW * 1920f - 165f) < 2f, "手牌卡宽 = 原版的 165.0 px");
            Check(Mathf.Abs(handH / visW * 1920f - HandCardHeightPx) < 2f,
                  $"手牌卡高 = 原版的 {HandCardHeightPx} px");
            Check(step > cardW, $"相邻两格不叠（空档 {(step - cardW) / visW * 1920f:F1} px）");

            // ② 两行不叠：我的行上沿 < 对手行下沿
            float pTop = pBoard.SlotPosition(0).y + CardView.Height * pBoard.placedScale * LayoutSpace.Scale * 0.5f;
            float pBot = pBoard.SlotPosition(0).y - CardView.Height * pBoard.placedScale * LayoutSpace.Scale * 0.5f;
            float eBot = eBoard.SlotPosition(0).y - CardView.Height * eBoard.placedScale * LayoutSpace.Scale * 0.5f;
            Check(eBot > pTop, $"两行不叠（我的上沿 {pTop:F2} < 对手下沿 {eBot:F2}，空档 {eBot - pTop:F2} 世界）");

            // ③ 敌方镜像：槽 0 在右边，且和自己这边左右对称
            var my0 = pBoard.SlotPosition(0); var foe0 = eBoard.SlotPosition(0);
            Check(foe0.x > eBoard.SlotPosition(8).x, $"敌方是镜像的（槽 0 在 x={foe0.x:F2}，槽 8 在 x={eBoard.SlotPosition(8).x:F2}）");
            Check(Mathf.Abs(foe0.x + my0.x) < 1e-4f, "两边的 0 号位左右对称（面对面）");

            // ④ 手牌：弧线、张角、间距
            float handTop = hand.SlotPosition(hand.numberOfCardsForMaxHeight / 2,
                                              hand.numberOfCardsForMaxHeight).y
                          + CardView.Height * hand.cardScale * LayoutSpace.Scale * 0.5f;
            float handBottom = hand.SlotPosition(0, hand.numberOfCardsForMaxHeight).y
                             - CardView.Height * hand.cardScale * LayoutSpace.Scale * 0.5f;
            Debug.Log(P + $"   手牌：{hand.Describe(4)}");
            Debug.Log(P + $"         {hand.Describe(12)}");
            // ⚠️ 2026-09-12：这两条原来写的是「不压到战场 / 不沉出屏幕底」。卡本体改成原版的
            //    2.0927×3.3313 之后，按**原版数值**摆，这两件事**本来就会各差一点点**：
            //      · 原版手牌卡 263 px 高、中心行 950 px → 卡底在 **1081.3 px**（屏幕下沿是 1080）；
            //      · 中列那张因为弧高（`ArcHeightFull`，来自原版布局曲线）还要往上抬 ~30 px，
            //        会探进前排卡底部一点。
            //    所以改成**和原版几何对账**，而不是「绝对不许碰」—— 后者当初成立是因为我们的卡
            //    偏矮 12%、两行被整体上移了 54 px，那个补丁已经撤掉（见文件头）。
            float handBottomPx = (LayoutSpace.VisibleHeight * 0.5f - handBottom) * 108f;
            float handTopPx    = (LayoutSpace.VisibleHeight * 0.5f - handTop) * 108f;
            float rowBottomPx  = (LayoutSpace.VisibleHeight * 0.5f - pBot) * 108f;
            Debug.Log(P + $"   手牌卡底 {handBottomPx:F1} px（原版 1081.3）　中列卡顶 {handTopPx:F1} px"
                        + $"　我的行下沿 {rowBottomPx:F1} px（原版 817.2）　叠 {rowBottomPx - handTopPx:F1} px");
            Check(Mathf.Abs(handBottomPx - 1081.3f) < 3f,
                  "手牌卡底压着屏幕下沿 = 原版的 1081.3 px（卡底本来就会出屏 1 px）");
            Check(rowBottomPx - handTopPx < 40f,
                  $"中列手牌探进前排的深度很小（{rowBottomPx - handTopPx:F1} px ≤ 40；原版同样会探进去）");

            // 弧线：中间高两头低；张角：两头朝外撇、中缝是正的
            float yMid = hand.SlotPosition(6, 12).y, yEnd = hand.SlotPosition(0, 12).y;
            Check(yMid > yEnd + 0.05f, $"弧线中间比两端高 {yMid - yEnd:F3} 世界（原版曲线：端低中高，不是抛物线）");
            float rotL = hand.RotationAt(0, 12), rotM1 = hand.RotationAt(5, 12), rotM2 = hand.RotationAt(6, 12);
            float rotR = hand.RotationAt(11, 12);
            Check(rotL > 0.5f && rotR < -0.5f, $"两端朝外撇（左 {rotL:F2}° / 右 {rotR:F2}°）");
            // 12 张时正中没有「那一张」，看的是**中缝两侧符号相反、各自都接近 0**
            Check(rotM1 > 0f && rotM2 < 0f && Mathf.Abs(rotM1) < 1f && Mathf.Abs(rotM2) < 1f,
                  $"中缝两侧几乎不正转（+{rotM1:F2}° / {rotM2:F2}°）");
            Check(Mathf.Abs(rotL + hand.RotationAt(11, 12)) < 1e-3f, "张角左右对称");

            // 间距自适应：牌少时不压缩、牌多时压在 maxLayoutSize 之内
            float s4 = hand.SpanWorld(4), s12 = hand.SpanWorld(12);
            float cap = hand.maxLayoutSize * visW;      // 16:9 下还要加宽高比修正，这里只看下界
            Check(s12 <= visW * 0.65f, $"12 张手牌跨度 {s12:F2} 世界 ≤ 可见宽的 65%（原版 60.25% 上限）");
            Check(Mathf.Abs(s4 - 3f * 1.45f * LayoutSpace.Scale) < 0.01f,
                  $"4 张时用原版间距 1.45 不压缩（实测 {s4 / 3f:F3} 世界/张）");
            Debug.Log(P + $"   手牌张数 → 跨度（世界）：4 张 {s4:F2}，12 张 {s12:F2}，上限 {cap:F2}");

            // ⑤ 原版美术：背景 + 卡框接上了没
            //    **装了才断言** —— 删掉 `Resources/Art/` 之后这两条自动跳过，
            //    自检照样全绿（「换自己的美术」这条路得能跑）
            Debug.Log(P + "   " + CardArt.Describe());
            var backdrop = driver.backdrop;
            if (CardArt.Available)
            {
                // 🆕 2026-09-19：战场现在是**真 3D**（`Arena3D` 29 网格 + 34 粒子 + 透视 `BoardCamera`）
                //    ⇒ 这条分两支。3D 那支的**逐值判据**（FOV / lensShift / 图层 / 相机成对）在
                //    `CheckSavedScene` 里，这里只验「场地在不在、兜底图有没有被同时挂上」。
                if (driver.boardCam != null)
                {
                    var arena = GameObject.Find("Arena3D");
                    int nArena = arena != null ? arena.GetComponentsInChildren<Renderer>(true).Length : 0;
                    Check(nArena > 10, $"战场是**真 3D**（Arena3D 可渲染件 {nArena} 个，不再是一张烘平的图）");
                    Check(backdrop == null, "3D 场地在的时候**没有**同时挂兜底背景图（两条路只留一条）");

                    // ★ 环境光**真的到了渲染器**：URP 读的是 `RenderSettings.ambientProbe`（SH），不是 `ambientLight` 本身。
                    //   实况实测（原版 arena3）probe 的 DC = **sRGB→linear** 之后的颜色 ——
                    //   (0.8066,0.9522,1.0) → 实测 (0.61508,0.89479,1.0) 逐位验算吻合；arena1 (1,1,1)→(1,1,1)。
                    //   🔴 **2026-09-30（§27）从 `CheckSavedScene` 挪到这里** —— 那些值现在存在 prefab 里、
                    //   由 `ArenaRuntimeLoader.Load → ArenaSceneState.Apply` 在**运行时**灌进 RenderSettings
                    //   （存档场景里那几项已经空了）⇒ 只能在这里量。
                    {
                        var ld = UnityEngine.Object.FindFirstObjectByType<CardPresentation.ArenaRuntimeLoader>();
                        var stA = (ld != null && ld.Current != null)
                                ? ld.Current.GetComponentInChildren<CardPresentation.ArenaPrefabData>() : null;
                        if (stA != null)
                        {
                            var col = stA.state.ambientSky;
                            var lin = new Vector3(Mathf.GammaToLinearSpace(col.r), Mathf.GammaToLinearSpace(col.g),
                                                  Mathf.GammaToLinearSpace(col.b));
                            var sh = RenderSettings.ambientProbe;
                            Check(Mathf.Abs(sh[0, 0] - lin.x) < 0.02f && Mathf.Abs(sh[1, 0] - lin.y) < 0.02f
                                  && Mathf.Abs(sh[2, 0] - lin.z) < 0.02f,
                                  "★ 环境光**真的到了渲染器**（运行时那份）：`ambientProbe` DC = "
                                + $"({sh[0, 0]:F4},{sh[1, 0]:F4},{sh[2, 0]:F4}) = sRGB→linear(环境光色) = ({lin.x:F4},{lin.y:F4},{lin.z:F4})");
                        }
                        else Check(false, "★ §27：运行时取不到 `ArenaPrefabData`（战场没实例化？）");
                    }

                    // 🆕 2026-09-22 闪烁族（原版 `MaterialFlickerEffect`，13 场 38 个）：
                    //    **判据必须按【该件自己的原版参数】算**，不能拿某一场的实测区间去套所有场 ——
                    //    🔴 **2026-09-25 踩过**：老断言写死「区间必须盖过 0.20~2.03」（那是 arena1 两个光斑的实测），
                    //    而 aeldari 的 `Dynamic Lights 8` 的 `amplitude` 只有 **0.36**（arena1 是 0.834）
                    //    ⇒ 它的**正确**区间就是 **0.62~1.48**，被判红。**错的是断言，不是产品。**
                    //    **算式**（`WFMaterialFlicker.Tick`，逐字照 `MaterialFlickerEffect__Update.c`）：
                    //      `alpha = (noise + 1) × 原α × (fade−0.25)/0.75`，`noise` 的四项正弦权重和 = 1.39
                    //    ⇒ **摆幅 ÷ (amplitude × 原α) ≈ 2.4**（四个样本实测 2.39 / 2.43 / 2.51 / 2.63），**均值 = 原α**。
                    //    ⇒ 判据：**逐件**看 ① 均值 ≈ 原α ② 摆幅 ≈ 2.4×(amplitude×原α)。
                    //    这样「常量抄错 / 四个权重丢了」仍然会红（摆幅会塌向 0），而换场不会误伤。
                    var fxs = UnityEngine.Object.FindObjectsByType<WarpforgeVFX.WFMaterialFlicker>(FindObjectsSortMode.None);
                    // 🔴 **判据按【该场自己的原版数据】来**，不能拿 arena1 的情况套所有场（**2026-09-25 踩**）：
                    //    `FlickerData.Specs` 只覆盖 **7 个场**（arena1/2/3 · aeldari · astramilitarum ·
                    //    spacewolves · tauviorla），**其余 6 场（含 `battlearenasororitas`）原版就没有闪烁件**
                    //    ⇒ 原来那句「本局战场 ≥1 个」换一场必红。**错的是断言，不是产品。**
                    //    对账口径三样：**want**（本场原版有几个）· **attached**（我们挂上了几个）·
                    //    **missing**（原版有、但对象没搬进来 —— 例如 astramilitarum 那 9 个挂在 `SpriteRenderer` 上的，
                    //    见 `项目任务.md` §三 第 3 条 第 9 项）。
                    int flickWant = 0;
                    foreach (var sp in FlickerData.Specs) if (sp.arena == BoardArena) flickWant++;
                    int flickAttached = ArenaBuilder.LastFlickerAttached;
                    int flickMissing = ArenaBuilder.LastFlickerMissing.Count;
                    string missList = flickMissing > 0
                        ? "；**没挂上的是**（原版有、对象没搬）：" + string.Join(" / ", ArenaBuilder.LastFlickerMissing.ToArray())
                        : "";
                    // 🔴 **2026-09-30（§27）判据改了口径**：原来比的是 `ArenaBuilder.LastFlickerAttached`
                    //    —— 那是**建场期**的静态计数，而 §27 之后战场是在 **`BuildArenaPrefabs`** 那一趟建的
                    //    ⇒ 与「跑自检」**不是同一个进程**，这个计数**恒为 0**（假红；实测 2026-09-30 撞上）。
                    //    现在**按原版数据判**：本场原版没有闪烁件 ⇒ 场上必须 0 个；有 ⇒ 场上必须 > 0 且**不超过**原版数。
                    Check(flickWant == 0 ? fxs.Length == 0 : (fxs.Length > 0 && fxs.Length <= flickWant),
                          $"★ 闪烁族：本场原版 **{flickWant}** 个 · 场上实际 **{fxs.Length}** 个"
                          + $"（按原版数据判；`LastFlickerAttached`={flickAttached} 是**建 prefab 那一趟**的计数、这里不可比）{missList}");
                    if (fxs.Length > 0)
                    {
                        int badMean = 0, badSwing = 0, frozen = 0;
                        float worstMeanErr = 0f, worstRatio = float.MaxValue;
                        string worstName = "";
                        foreach (var fx in fxs)
                        {
                            float sum = 0f, mn = float.MaxValue, mx = float.MinValue;
                            for (int i = 0; i < 200; i++)
                            {
                                float a = fx.SampleAlpha(i * 0.05f);
                                sum += a; if (a < mn) mn = a; if (a > mx) mx = a;
                            }
                            float mean = sum / 200f;
                            float expect = fx.OrigAlpha * fx.FadeFactor;      // 均值应当就是它
                            float swing = (mx - mn) / Mathf.Max(fx.amplitude * expect, 1e-4f);
                            if (fx.amplitude < 0.01f) frozen++;
                            else if (Mathf.Abs(mean - expect) > 0.12f * Mathf.Max(expect, 1e-3f)) badMean++;
                            if (swing < 1.5f || swing > 3.5f) badSwing++;
                            if (Mathf.Abs(mean - expect) > worstMeanErr) { worstMeanErr = Mathf.Abs(mean - expect); worstName = fx.name; }
                            if (swing < worstRatio) worstRatio = swing;
                        }
                        Check(frozen == 0, $"★ 每一件的 `amplitude` 都 > 0（不能闪的件 {frozen} 个）");
                        Check(badMean == 0,
                              $"★ 每一件的 alpha 均值都 ≈ 它自己的 `原α`（偏的 {badMean}/{fxs.Length} 个，最大偏差 {worstMeanErr:F3}）"
                              + " —— 算式常量与四个正弦权重照抄 `MaterialFlickerEffect__Update.c`");
                        Check(badSwing == 0,
                              $"★ ……而且**真的在闪**：摆幅 ÷ (amplitude×原α) 应落在 1.5~3.5（四个样本实测 2.39~2.63）；"
                              + $"偏的 {badSwing}/{fxs.Length} 个，最差 {worstRatio:F2}；最差的是「{worstName}」");
                    }

                    // 🆕 2026-09-25「一局一个战场」那条链（表 = 运行时那份 `ArenaByArmy`，
                    //    判据 → `资料/普查产出_0920/场景光照与后处理_原版规格.md` §六）。
                    //    **逐个阵营查一遍**，看两件事：
                    //    ① 返回的场景名**必须真的能被 `SceneManager.LoadScene` 载入** ——
                    //       ⚠️ 判据走 `ArenaByArmy.CanLoadScene`（**扫 Build Settings 的场景表**）：
                    //       **场景文件在磁盘上、但没进 Build Settings 时 `LoadScene` 照样会抛**
                    //       （`File.Exists` 判不出来，踩过：`Battle.unity` 以前从没被登记过，
                    //       而批处理下 `LoadScene` 提前 return ⇒ 没人发现）。
                    //       🔴 **别用 `Application.CanStreamedLevelBeLoaded`** —— 2026-09-25 实测：
                    //       在 `-batchmode -executeMethod` 下它**连已登记的 `Battle` 都判成 false**，
                    //       14 个阵营全回落、这两条自检直接红。
                    //    ② `SceneFor` 与表里那一行的 `Scene` **不许自相矛盾**。
                    //    ⚠️ 某场那份没建时**回落 `Battle`**（那也「载得入」）⇒ 这一条**不会**因为没建齐而红；
                    //       「建齐了没有」由下一条按**不同场景名的个数**定量判（13 个战场 ⇒ 13 个名字）。
                    {
                        int badL = 0, badT = 0; var detail = "";
                        var names = new System.Collections.Generic.HashSet<string>();
                        foreach (var row in ArenaByArmy.Rows)
                        {
                            var nm = ArenaByArmy.BattleSceneNameFor(row.Army);
                            names.Add(nm);
                            if (!ArenaByArmy.CanLoadScene(nm)) { badL++; detail += $"「{row.Army}」→`{nm}` "; }
                            if (ArenaByArmy.SceneFor(row.Army) != row.Scene) badT++;
                        }
                        Check(badL == 0 && badT == 0,
                              $"★ 逐个阵营查表，返回的对战场景名**都载得入**（载不入的 {badL} 个 · 自相矛盾的 {badT} 个）"
                              + (detail.Length > 0 ? "：" + detail : "")
                              + " —— `LoadScene` 要的是「**在 Build Settings 里**」，光有 `.unity` 文件不够");
                        // 🔴 **2026-09-30（§27 架构）判据换掉了**：原来这里要求「13 个不同的场景名」
                        //    （13 份 `Battle_<场>.unity`）。现在**只有一份 `Battle.unity`**，
                        //    13 个战场由 `Resources/ArenaPrefabs/<场>.prefab` 在**运行时**提供
                        //    ⇒ 这里改判**那 13 件 prefab 取不取得到**（取不到就是 `Load` 时会出声的那个坑）。
                        int nPrefab = 0; var missPrefab = new System.Text.StringBuilder();
                        foreach (var row in ArenaByArmy.Rows)
                        {
                            if (row.Scene == "battlearena1" && nPrefab > 0) continue;   // 同一场的多行（Neutral/Ultramarines）只数一次
                            if (Resources.Load<GameObject>("ArenaPrefabs/" + row.Scene) != null) nPrefab++;
                            else missPrefab.Append(row.Scene).Append(' ');
                        }
                        Check(names.Count == 1 && nPrefab == 13,
                              $"★ §27：**一份 `Battle.unity`** + **13 件战场 prefab**（当前场景名 {names.Count} 个（目标 1）、"
                            + $"prefab 取到 {nPrefab} 件（目标 13）"
                            + (missPrefab.Length > 0 ? "；缺：" + missPrefab : "")
                            + "）—— 建法：`-executeMethod ArenaBuilder.BuildArenaPrefabs`；"
                            + "**缺的会在 `ArenaRuntimeLoader.Load` 里出声**（不是静默）");
                    }
                }
                else
                {
                    Check(backdrop != null && backdrop.Ready, "战场背景接上了（ArtBaker 烘的 arena1_bg）");
                    if (backdrop != null && backdrop.Ready)
                    {
                        var r = backdrop.ScreenRect();      // (图宽, 图高, 可见宽, 可见高)
                        Check(r.x >= r.z - 1e-3f && r.y >= r.w - 1e-3f,
                              $"背景「铺满」可见区（图 {r.x:F2}×{r.y:F2} ≥ 可见 {r.z:F2}×{r.w:F2}，不变形）");
                    }
                }

                // 软光/影那层（原版 `Card Highlight And Shadow`，4.4281² @ y −0.0126）—— 2026-09-19 接上
                // ⚠️ **2026-10-07 改（A111 连带）**：卡面各层现在挂在卡根下面新增的那层 `2DCard` 上
                //    （原版就是两级 `CardUI Reference / 2DCard`，见 `CardView.Build`）⇒
                //    `Transform.Find("shadow")`（**只找直接子件**）找不到它了。改成**按名字递归找**。
                //    ⛔ 别改成写死路径 `Find("2DCard/shadow")` —— 那是把结构名抄进断言，改个名就静默失效。
                var cvS = driver.HandViewAt(0);
                var shLayer = cvS != null
                    ? System.Array.Find(cvS.GetComponentsInChildren<Transform>(true), t => t.name == "shadow")
                    : null;
                Check(shLayer != null, "卡面**最底层**有软光/影那层（原版 `Card Highlight And Shadow`）");
                if (shLayer != null)
                {
                    var shMf = shLayer.GetComponent<MeshFilter>();
                    var shMr = shLayer.GetComponent<MeshRenderer>();
                    var sz = shMf != null && shMf.sharedMesh != null ? shMf.sharedMesh.bounds.size : Vector3.zero;
                    // ⚠️ 量**网格**不是世界包围盒：手牌里的卡是缩放过的（0.73），世界尺寸会跟着缩
                    Check(Mathf.Abs(sz.x - 4.4281f) < 0.01f && Mathf.Abs(sz.y - 4.4281f) < 0.01f,
                          $"那层是原版的 **4.4281²**（实测 {sz.x:F4}×{sz.y:F4}，比卡本体 2.09×3.33 大得多 ⇒ 露在卡外那圈就是软影）");
                    var shMat = shMr != null ? shMr.sharedMaterial : null;
                    Check(shMat != null && shMat.shader != null &&
                          shMat.shader.name == "Everguild/FX/Card Highlight And Shadow",
                          $"用的是**原版 shader** `Everguild/FX/Card Highlight And Shadow`（实测 {shMat?.shader?.name}）");
                    // 🔴 这条是踩出来的：shader 默认的 `_Outline` 是**不透明的白** ⇒ 每张卡会多出一圈白框；
                    //    原版**材质**把它的 alpha 设成 0（平时不描边）。改成非 0 之前先想清楚。
                    // 🔴 这条是踩出来的：shader 默认的 `_Outline` 是**不透明的白** ⇒ 每张卡会多出一圈白框；
                    //    原版**材质**把它的 alpha 设成 0（平时不描边）。改成非 0 之前先想清楚。
                    //    ⚠️ 看的是**材质模板**（`SdfTemplateOutlineAlpha`）—— 卡上那个值现在会被状态驱动
                    //    （手牌打得出去就点亮，见下面第 ⑦ 条）。
                    Check(CardView.SdfTemplateOutlineAlpha >= -0.5f && CardView.SdfTemplateOutlineAlpha < 1e-3f,
                          $"`_Outline` 的 alpha **模板默认值**是 0（实测 {CardView.SdfTemplateOutlineAlpha:F3}；"
                          + "不是 0 的话卡会多一圈白框）");
                }
                var frameTex = CardArt.Frame(StarterCards.EmberFaction);
                Check(frameTex != null, $"卡框图加载到了（frame_{StarterCards.EmberFaction.ToLowerInvariant()}.png）");
                if (frameTex != null)
                {
                    var uv = CardView.FrameUv(frameTex);
                    Debug.Log(P + $"   卡框 {frameTex.width}×{frameTex.height}，卡本体 UV "
                                + $"u {uv.xMin:F3}..{uv.xMax:F3}  v {uv.yMin:F3}..{uv.yMax:F3}"
                                + $"（占比 {uv.width:P0}×{uv.height:P0}）");
                    Check(uv.width < 0.95f && uv.height < 0.95f,
                          "卡框 UV 裁到了卡本体上（不是把整张 1024² 留白一起铺上去）");
                }
            }
            else
            {
                Debug.Log(P + "   （`Resources/Art/` 是空的 —— 跳过美术那几条断言，"
                            + "用程序生成的占位卡面/纯色背景）");
            }

            Shot(cam, "01b_版面");
        }

        // ---- 1e. 右侧能量区：敌方水晶 / 底板 / 任务点（2026-09-12 补的一批原版元素）----
        // ⚠️ 这几件的毛病**截图看不出来** —— 少一颗水晶、底板图拿错、任务点在水晶内侧，
        //    画面都「看着挺满」。所以按数值断言。
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            // 🔴 **A195**：原来是 `if (drv != null)`、**没有 `else`** ⇒ 找不到 driver 时下面整节（含 ★ 判据）静默跳过、
            //    section 照样绿。形状照同族那几处（§15 ⑧ / §15b / §15c / §17 早就是这写法）：条件不成立**当场红**。
            if (drv == null) Check(false, "找不到 BattleDriver");
            else
            {
                // ① 敌方水晶：原版 `EnemyMana` 是有的，我们原来一颗都没画
                Check(drv.FoeEnergyGemTex.StartsWith("40k_battle_energy_"),
                      $"敌方能量水晶用的是原版两张图（现在 `{drv.FoeEnergyGemTex}`）");
                var fp = drv.FoeEnergyPos01;
                Check(Mathf.Abs(fp.x - 0.97060f) < 0.002f && Mathf.Abs(fp.y - 0.73278f) < 0.002f,
                      $"敌方水晶在 x01 {fp.x:F5} / y01 {fp.y:F5}（原版权威表 0.97060 / 0.73278）");
                Check(drv.FoeEnergyText != null && drv.FoeEnergyText.Contains("/"),
                      $"敌方能量数字读得出来（`{drv.FoeEnergyText}`）");

                // ② 两块底板：`Card Frame Cost Icon`
                Check(drv.MyEnergyPlateTex == "Card_Frame_Cost_Icon" && drv.FoeEnergyPlateTex == "Card_Frame_Cost_Icon",
                      $"两块能量底板都是 `Card_Frame_Cost_Icon`（我 `{drv.MyEnergyPlateTex}` / 敌 `{drv.FoeEnergyPlateTex}`）");

                // ③ 任务点：**我方在水晶下方、敌方在上方**（原来两个都摆在水晶内侧，是接片的偏移）
                var mp = drv.QuestIconPos(true);
                var qp = drv.QuestIconPos(false);
                var myE = drv.MyEnergyPos01;
                Check(Mathf.Abs(mp.x - 0.97180f) < 0.002f && Mathf.Abs(mp.y - 0.40347f) < 0.002f,
                      $"我方任务点在 x01 {mp.x:F5} / y01 {mp.y:F5}（原版 0.97180 / 0.40347）");
                Check(Mathf.Abs(qp.x - 0.97133f) < 0.002f && Mathf.Abs(qp.y - 0.81569f) < 0.002f,
                      $"敌方任务点在 x01 {qp.x:F5} / y01 {qp.y:F5}（原版 0.97133 / 0.81569）");
                Check(mp.y < myE.y && qp.y > fp.y,
                      $"任务点在水晶**外侧**（我 {mp.y:F3} < 水晶 {myE.y:F3}；敌 {qp.y:F3} > {fp.y:F3}）");

                // ④ 🆕 2026-09-29：**能量座上那 6 枚常亮小光点**（原版 `Energy And turn holder/Lights`）。
                //    判据 = 原版那 6 个 RT 的**绝对屏幕矩形**（13 个战场包逐场核过、完全同构）——
                //    逐枚比中心与尺寸，**不是**拿我们自己的常量自证。
                var lights = drv.EnergyLights;
                Check(lights != null && lights.Count == 6,
                      $"能量座上有 **6 枚**光点（原版是 6 枚不是 5 枚；实得 {(lights == null ? 0 : lights.Count)}）");
                if (lights != null && lights.Count == 6)
                {
                    float[] ex = { 1839.19f, 1803.22f, 1829.97f, 1840.15f, 1832.02f, 1855.89f };
                    float[] ey = { 370.51f, 346.63f, 346.82f, 353.45f, 361.72f, 359.85f };
                    float[] ew = { 19.32f, 12.77f, 10.00f, 8.73f, 8.24f, 10.10f };
                    float[] eh = { 18.78f, 12.65f, 9.30f, 8.17f, 6.55f, 8.83f };
                    int bad = 0; string worst = "";
                    for (int i = 0; i < 6; i++)
                    {
                        var wp = lights[i].transform.position;
                        float cx = LayoutSpace.PxX(wp.x), cy = LayoutSpace.PxY(wp.y);
                        float w = LayoutSpace.PxX(lights[i].WorldW) - LayoutSpace.PxX(0f);
                        float h = LayoutSpace.PxX(lights[i].WorldH) - LayoutSpace.PxX(0f);
                        float wx = ex[i] + ew[i] * 0.5f, wy = ey[i] + eh[i] * 0.5f;
                        if (Mathf.Abs(cx - wx) > 1.5f || Mathf.Abs(cy - wy) > 1.5f
                            || Mathf.Abs(w - ew[i]) > 1f || Mathf.Abs(h - eh[i]) > 1f)
                        {
                            bad++;
                            worst = $"#{i + 1} 中心({cx:F1},{cy:F1}) 期望({wx:F1},{wy:F1})"
                                  + $" 尺寸 {w:F1}×{h:F1} 期望 {ew[i]}×{eh[i]}";
                        }
                    }
                    Check(bad == 0, $"…而且**逐枚落在原版那个矩形里**（{6 - bad}/6 对得上）；{worst}");
                }
            }
        }

        // ---- 1f. 四小件（2026-09-13 补）：牌库张数底板 / 手牌数底板 / 本回合已出牌数 / 里程碑骷髅 ----
        // ⚠️ 这四样的毛病**截图都看不出来**：半透明板画成实心、板压根没画、出牌灯该亮不亮、
        //    骷髅摆到名牌外面 —— 画面都「看着挺满」。所以一律按数值断言。
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            // 🔴 **A195**：原来是 `if (drv != null)`、**没有 `else`** ⇒ 找不到 driver 时下面整节（含 ★ 判据）静默跳过、
            //    section 照样绿。形状照同族那几处（§15 ⑧ / §15b / §15c / §17 早就是这写法）：条件不成立**当场红**。
            if (drv == null) Check(false, "找不到 BattleDriver");
            else
            {
                // ① 牌库张数底板（原版 `PlayerDeck/Player Deck Size Container`，敌方那个小一号）
                Check(drv.DeckSizePlateTex == "40K_display",
                      $"张数底板用的是 `40K_display`（现在 `{drv.DeckSizePlateTex}`）");

                // ①b 🆕 2026-09-26：牌堆卡背底下那层 **SDF**（原版 `Cardback Shadow SDF`）。
                //   逐值 = 原版 `Cardback Container` 下两个兄弟节点自己的 `sizeDelta`（实读）：
                //     `Cardback` 2.1739×3.1364 · `Cardback Shadow SDF` **2.9212×3.8122**（@scale100 = 292.12×381.22 px）
                //     —— 比值 **1.34376 / 1.21548**，两节点 anchoredPos 都是 (0,0) ⇒ **同心**。
                var pile = drv.MyPileQuad;
                var dsdf = drv.MyDeckSdfQuad;
                if (pile == null || pile.Texture == null)
                    Check(true, "⚠️ 这一局我方阵营**没有默认卡背**（查不到 `CardbackTable.DefaultFor(阵营)`、"
                              + "且旧那 4 张 `Art/cards/back_<阵营>.png` 里也没有）"
                              + "⇒ 牌堆 SDF 那条**验不到**（不是失败；换个有卡背的阵营才验得了）");
                else Check(dsdf != null && dsdf.Texture != null,
                      "★ 我方牌堆底下有 **`Cardback Shadow SDF`** 那一层（原版 `DeckManager.cardbackShadow`）");
                if (pile != null && pile.Texture != null && dsdf != null)
                {
                    float rw = dsdf.WorldW / pile.WorldW, rh = dsdf.WorldH / pile.WorldH;
                    Check(Mathf.Abs(rw - 2.9212f / 2.1739f) < 0.02f && Mathf.Abs(rh - 3.8122f / 3.1364f) < 0.02f,
                          $"★ SDF 比卡背大 **1.34376 × 1.21548**（原版两个 sizeDelta 之比）—— 实得 {rw:F5} × {rh:F5}");
                    Check(dsdf.Texture.name.EndsWith("_sdf"),
                          $"★ SDF 贴的是**这张牌堆卡背自己的掩码**（`{dsdf.Texture.name}`）");
                    var mr = dsdf.GetComponent<MeshRenderer>();
                    Check(mr != null && mr.sharedMaterial != null && mr.sharedMaterial.shader != null
                          && mr.sharedMaterial.shader.name == "Everguild/FX/Card Highlight And Shadow",
                          "★ SDF 层用**原版 shader** `Everguild/FX/Card Highlight And Shadow`"
                        + "（不是 `Sprites/Default` —— 那会把灰掩码当图直接画出来）");
                    // 层次：这块 HUD 用 z 排（相机看 +Z、z 越大越远）⇒ SDF 必须**更远**才在卡背底下
                    Check(dsdf.transform.localPosition.z > pile.transform.localPosition.z,
                          $"★ SDF 在卡背**底下**（z {dsdf.transform.localPosition.z:F3} > 卡背 {pile.transform.localPosition.z:F3}）");
                }
                Check(Mathf.Abs(drv.MyDeckSizePlateWorldH - 59.11f / 108f) < 0.002f,
                      $"我方张数底板高 {drv.MyDeckSizePlateWorldH:F4}（原版 59.11 px = 0.5473，"
                    + "是 `AspectRatioFitter` 4.0346 按父宽 238.5 算出来的，不是 sizeDelta）");
                Check(Mathf.Abs(drv.FoeDeckSizePlateWorldH - 52.05f / 108f) < 0.002f,
                      $"敌方张数底板高 {drv.FoeDeckSizePlateWorldH:F4}（原版 52.05 px = 0.4819，敌方小一号）");
                Check(Mathf.Abs(drv.DeckSizePlateAlpha - 0.6941177f) < 0.01f,
                      $"张数底板是**半透明**的（α {drv.DeckSizePlateAlpha:F4}，原版 m_Color.a = 0.6941）");
                Check(drv.PileLabelWorldW > 0.01f && drv.PileLabelWorldW < drv.MyDeckSizePlateWorldH * (442f / 112f) - 0.05f,
                      $"张数文字宽 {drv.PileLabelWorldW:F3} < 底板宽 "
                    + $"{drv.MyDeckSizePlateWorldH * (442f / 112f):F3}（原版文字是 TMP 自动缩字号塞进去的）");

                // 🔴 **2026-09-27（PA 普查 §三 第 1 条）：牌堆底板是 `preserveAspect` —— 我们原来只给「高」。**
                //   原版 `RightArea/PlayerDeck/DeckAndEnergyImage`：PA=1 · 框 230×229.85（方）·
                //   图 `UI_Deck_Background` 364×346（横）⇒ **按宽定** ⇒ 实绘 **230×218.63**；
                //   我们原来按高给 230 ⇒ 实绘 241.96×230（**宽出框 11.96px、高出 11.4px = +5.2%**）。
                Check(Mathf.Abs(drv.DeckPlateWorldW * 108f - 230f) < 1.5f
                   && Mathf.Abs(drv.DeckPlateWorldH * 108f - 218.63f) < 1.5f,
                      $"★ 我方牌堆底板 = 原版 **230×218.63**（PA=1 内接，图比框宽 ⇒ 按宽定）—— 实测 "
                    + $"{drv.DeckPlateWorldW * 108f:F2}×{drv.DeckPlateWorldH * 108f:F2}");
                Check(Mathf.Abs(drv.FoeDeckPlateWorldW * 108f - 200f) < 1.5f
                   && Mathf.Abs(drv.FoeDeckPlateWorldH * 108f - 190.11f) < 1.5f,
                      $"★ 敌方牌堆底板 = 原版 **200×190.11**（同一条判据，敌方小一号）—— 实测 "
                    + $"{drv.FoeDeckPlateWorldW * 108f:F2}×{drv.FoeDeckPlateWorldH * 108f:F2}");

                // ② 手牌数底板（原版 `CardsInHandText/Bg (1)`，图**也是** `40K_display`）
                Check(drv.HandPlateTex == "40K_display",
                      $"手牌数底板用的是 `40K_display`（现在 `{drv.HandPlateTex}`）");
                Check(Mathf.Abs(drv.HandPlateWorldH - 65.7f / 108f) < 0.004f,
                      $"手牌数底板高 {drv.HandPlateWorldH:F4}（原版缩放链 108×0.9259×0.009 = 0.9 → 259.3×65.7 px）");
                // 板子 259 px 宽，贴在屏幕左缘就会被切掉一半（**第一版就是这样**，截图里只剩右半边）
                {
                    var hp = drv.HandPlatePos01;
                    float halfW01 = drv.HandPlateWorldH * (442f / 112f) * 0.5f / LayoutSpace.VisibleWidth;
                    Check(hp.x - halfW01 >= -0.002f,
                          $"手牌数底板整块在屏幕内（中心 x01 {hp.x:F4} − 半宽 {halfW01:F4} = {hp.x - halfW01:F4} ≥ 0）");
                }

                // ③ 本回合已出牌数（原版 `LeftArea/CardsPlayedInTurnHolder` 里的三枚）
                Check(drv.PlayedPipTex(0) == "40k_general_bt_yellow" && drv.PlayedPipTex(2) == "40k_general_bt_yellow",
                      $"三枚「已出牌数」用的是 `40k_general_bt_yellow`（现在 `{drv.PlayedPipTex(0)}`）");
                Check(drv.PlayedPipsOn == Mathf.Min(3, drv.CardsPlayedThisTurn),
                      $"亮的枚数 = min(3, 本回合已出牌数)（现在亮 {drv.PlayedPipsOn} / 出了 {drv.CardsPlayedThisTurn}）"
                    + " —— 第 2 节打出牌之后还会再验一次「真的会亮」");

                // ---- 🆕 2026-09-29（§25）：**进攻卡（环境效果卡）** 的数据与 HUD 钮 ----
                //   判据 → `资料/加时与冲突模式_原版规格.md` 的进攻卡那一节 + 判据文件 §三 第 25 条。
                //   ⚠️ 这一节只验「数据在位 + 那颗钮的显隐判据」；**换环境那一半归【战场场景线】**
                //      （`项目任务.md` §三 第 30 条），这边只记「该切到哪条 SO」。
                Check(CardPresentation.OffensiveCards.Available && CardPresentation.OffensiveCards.All.Length == 13,
                      $"★ 进攻卡数据在位：{CardPresentation.OffensiveCards.All.Length} 个阵营"
                    + "（`Resources/OffensiveCards.json` ← `工具/gen_offensive_cards_flat.py`）");
                {
                    var choices = CardPresentation.OffensiveCards.Choices("Ultramarines");
                    Check(choices.Count == 4, $"★ 先手方的候选 = **4 张**（现在 {choices.Count}）");
                    Check(choices.Count > 0 && CardPresentation.OffensiveCards.IsEmpty(choices[0]),
                          "★ **「不使用进攻卡」排在下标 0**（原版 `GetEnvEffectCards` 先 Add 空卡、再 AddRange）");
                    Check(CardArt.OffensiveFace("Ultramarines", 0) != null
                       && CardArt.OffensiveFace("Ultramarines", -1) != null,
                          "★ 进攻卡的卡面插画**导进来了**（三张 + 空卡那张都取得到；"
                        + "`Resources/Art/offensive/`，由 `工具/import_offensive_faces.py` 导，50/52）");
                }
                Check(!drv.OffensiveButtonVisible,
                      "★ 开局时那颗**进攻卡钮是关着的**（原版 `BattleHud.Initialize` 先 SetActive(false)，"
                    + "之后只有「进攻卡生效」那一步会按「选定卡 ≠ 空卡」打开它）");
                {
                    // 空卡那一路 ⇒ 钮**不出现**（原版 `isEmptyOffensiveCard` 那条判据）
                    int slotWas = ctx.OffensiveSlotIdx;
                    RuleCore.ChooseOffensiveCard(ctx, 0, -1, "");
                    drv.SimulateApplyOffensiveEnv();
                    Check(!drv.OffensiveButtonVisible, "…选了「不使用进攻卡」⇒ 钮**仍然不出现**（不是变灰）");
                    // 真卡那一路 ⇒ 钮出现
                    var c0 = CardPresentation.OffensiveCards.Choices("Ultramarines")[1];
                    RuleCore.ChooseOffensiveCard(ctx, 0, c0.idx, c0.envSO);
                    bool fogWas = RenderSettings.fog; float denWas = RenderSettings.fogDensity;
                    Color fcWas = RenderSettings.fogColor; Color alWas = RenderSettings.ambientLight;
                    float blendWas = Shader.GetGlobalFloat("_AmbientColorBlend");
                    drv.SimulateApplyOffensiveEnv();
                    Check(drv.OffensiveButtonVisible, $"…选了真卡（槽 {c0.idx}）⇒ 钮**出现**");

                    // 🆕 2026-10-01：**展示窗那一步**（原来只「如实出声」）—— 原版 `BattleManager.DisplayOffensiveCard()`
                    //   → `CardDisplayWindow.ShowCard(card, tier:null, showOptions:false, …)`：**纯展示**，
                    //   不改战场状态、不发网络包（判据 → `项目任务.md` §三 第 25 条）。
                    Check(drv.CardDisplay != null, "（前提）卡牌展示窗那一套在位");
                    if (drv.CardDisplay != null)
                    {
                        drv.CardDisplay.Hide();
                        bool opened = drv.OpenOffensiveCardWindow();
                        Check(opened && drv.CardDisplay.Visible && drv.CardDisplay.ShownTitle == c0.name,
                              $"★ 那颗钮按下 ⇒ **弹的是展示窗**（现展示 `{drv.CardDisplay.ShownTitle}`）—— "
                            + "原版那条链是**纯展示**（`showOptions:false`），**不换环境、不发网络包**");
                        drv.CardDisplay.Hide();
                    }

                    // 🆕 2026-10-01：**进攻卡池用的是【后手那一方】的阵营**（原版 `GetEnvEffectCards` 的 army
                    //   恒取后手那一边：`playerGoesFirst ? enemyManager(+0xc8) : playerManager(+0xc0)`；
                    //   判据全文 → `资料/普查产出_0930/进攻防御卡_面板语义.md`）。**这条在改之前会红。**
                    Check(!string.IsNullOrEmpty(drv.OffensivePoolFaction)
                       && drv.OffensivePoolFaction != drv.MyFactionForTest,
                          $"★ 进攻卡池用的是**后手那一方**的阵营（`{drv.OffensivePoolFaction}`），"
                        + $"不是本机自己的（`{drv.MyFactionForTest}`）");

                    // 🆕 2026-10-01：**后手方那 3 张防御卡 + 选完换进手牌**
                    //   （原版 `EnviromentalEffectCardsSO.defensiveCards` → `ClickChosenCardDone` → 进后手方手牌）
                    var defList = drv.DefensiveChoicesForTest("Ultramarines");
                    Check(defList.Count == 3, $"★ 后手方那 3 张防御卡取到了（现在 {defList.Count}）");
                    bool allDef = defList.Count > 0;
                    foreach (var df in defList) if (df == null || df.Type != "defence") allDef = false;
                    Check(allDef, "…而且它们都是 `defence` 类型（判据：`数据/游戏数据/defensive_cards_39.json` "
                                + "13×3 与我们卡池同阵营的 `defence` 卡**逐张对上 38/39**）");
                    if (defList.Count > 0)
                    {
                        int seat = ctx.SecondSeat;
                        int before = 0;
                        foreach (var h in ctx.Players[seat].Hand)
                            if (h != null && h.Card != null && h.Card.Type == "defence") before++;
                        RuleCore.SetDefensiveCard(ctx, seat, defList[0]);
                        int after = 0; CardDef lastDef = null;
                        foreach (var h in ctx.Players[seat].Hand)
                            if (h != null && h.Card != null && h.Card.Type == "defence") { after++; lastDef = h.Card; }
                        Check(after == 1 && lastDef == defList[0],
                              $"★ 选完那张**换进后手方手牌**（防御卡 {before} → {after} 张，现在是「{lastDef?.Name}」；"
                            + "原版 `ClickChosenCardDone` 那条链就是这么落的）");
                    }

                    // 🆕 2026-10-01（§三 第 8 条 · 判据 `资料/全量反编译复核_靠推断的清单.md` §2.1）：
                    //   **标题那条链照原版搭好了**：`Battle/ChooseCard/Instructions-<uniqueId>` → 无后缀那条 → 调用方兜底
                    //   （原版 `ChooseCardMenu__GetTittleText(string uniqueId)`，`dump.cs:37575`）。
                    //   ⚠️ **本地没有语言表**（词条在远端 CCD）⇒ 实际跑起来必然落在最后一档
                    //   ⇒ 这里**临时往表里塞两条**证明链本身是通的（塞完清掉，别污染后面）。
                    // ⚠️ **A195 核过：这个门是安全的**（没改）—— `drv.Choose == null` 时，§20「选牌面板」那句
                    //    `Check(panel != null, "选牌面板建出来了")` 会在**同一次 `Run` 内**当场红。
                    if (drv.Choose != null)
                    {
                        var terms = ChoosePanel.Terms;
                        Check(terms.Count == 0, "（前提）`Terms` 出厂是空的 —— 所以这条链现在必然走第三级");
                        terms["Battle/ChooseCard/Instructions"] = "无后缀词条";
                        terms["Battle/ChooseCard/Instructions-ABC"] = "带后缀词条";
                        drv.Choose.Open(new List<CardView>(), null, "ABC");
                        Check(drv.Choose.TitleText == "带后缀词条" && drv.Choose.LastUniqueId == "ABC",
                              $"★ 标题链第一级：`uniqueId` 拼进 key、**命中了**（现在「{drv.Choose.TitleText}」）");
                        drv.Choose.Open(new List<CardView>(), null, "没有这张卡");
                        Check(drv.Choose.TitleText == "无后缀词条",
                              "★ 第二级：后缀查不到 ⇒ **回落无后缀那条**（这是原版自己的回落，不是我们加的）");
                        terms.Clear();
                        drv.Choose.Open(new List<CardView>(), null, "ABC");
                        Check(drv.Choose.TitleText == ChoosePanel.DefaultTitle,
                              $"★ 第三级：表空（= **本地的实际状态**）⇒ 落到调用方兜底「{ChoosePanel.DefaultTitle}」"
                            + "（🔴 不自己编词条 —— 词条正文在远端本地化表，本地一张都没有）");
                        drv.Choose.Close();
                    }

                    // 🆕 2026-09-30（§三 第 30 条 · 4 环境的**战场侧**）：执行器真的把环境换了吗
                    //   判据 → `资料/加时与冲突模式_原版规格.md` 的进攻卡那一节（补间雾 / 环境光 / 实例化 prefab）。
                    var envIt = CardPresentation.EnvironmentConditions.Find(c0.envSO);
                    Check(envIt != null, $"★ 环境数据在位：`{c0.envSO}` 在 `Resources/EnvironmentConditions.json` 里查得到");
                    Check(drv.EnvApplierForTest != null && drv.EnvApplierForTest.CurrentSO == c0.envSO,
                          $"★ 环境执行器记下了这一条（现在 `{(drv.EnvApplierForTest == null ? "null" : drv.EnvApplierForTest.CurrentSO)}`）");
                    Check(drv.EnvApplierForTest != null && drv.EnvApplierForTest.BlendT >= 1f,
                          "★ 补间推到底了（批处理没有帧循环 ⇒ 靠 `Advance` 手推，与 `SimulateApplyOffensiveEnv` 同一条理由）");
                    if (envIt != null)
                    {
                        Check(Mathf.Abs(RenderSettings.fogDensity - envIt.fogDensity) < 1e-4f,
                              $"★ 雾密度到位：现在是 {RenderSettings.fogDensity:F4}、表里是 {envIt.fogDensity:F4}"
                            + "（判据：`ScenarioEnvironmentConditionSO__EnableEnvironment` 的 `ApplyFog`）");
                        Check(Mathf.Abs(Shader.GetGlobalFloat("_AmbientColorBlend") - envIt.ambientBlend) < 1e-3f,
                              $"★ 环境光混合到位：现在是 {Shader.GetGlobalFloat("_AmbientColorBlend"):F3}、表里是 {envIt.ambientBlend:F3}");
                        Check(CardPresentation.EnvironmentConditions.HasPrefab(envIt)
                              == (drv.EnvApplierForTest.CurrentInstance != null),
                              "★ 「这一条有没有物件」⇔「实例建出来了」（原版 `scenarioObjects` 有值才 `Instantiate`）");
                    }
                    // 🆕 2026-09-30 晚：**blendable 那两半**（判据 → `资料/加时与冲突模式_原版规格.md` 的 2026-09-30 那一节；
                    //   方法体在 `d:/2/tools/decomp_full/Scenario{...}__*.c`）：
                    //   ① 环境 prefab 实例里那批（原版 `EnableEnvironment` ③ 那段）
                    //   ② **战场【自己】挂的那批**（原版 `SetRegisteredBlendeablesState(lVar8)`，
                    //      方向 = `SO.defaultScenarioObjectsState`，实测 38/55 条 = 0）—— 上一轮整条没做
                    CardPresentation.EnvBlendables.Counts(out int nbPf, out int nbSc);
                    Check(nbPf == 42 && nbSc == 13,
                          $"★ blendable 旁挂在位：{nbPf} 件 prefab + {nbSc} 场（判据 = `工具/gen_env_blendables.py` 的两侧自检）");
                    var ap0 = drv.EnvApplierForTest;
                    Check(ap0 != null && ap0.InstanceBlendableCount > 0,
                          $"★ 环境实例上挂上了 blendable（{ap0?.InstanceBlendableCount ?? -1} 个）"
                        + " —— 原版 `EnableEnvironment` ③ 那段");
                    Check(ap0 != null && ap0.SceneBlendableCount > 0,
                          $"★ 战场【自己】那批 blendable 也挂上了（{ap0?.SceneBlendableCount ?? -1} 个）"
                        + " —— 原版 `SetRegisteredBlendeablesState(lVar8)` 那条路");

                    // ============================================================
                    // 🆕 2026-10-11（A191 + A201）**独立一节**：原版的【分组节点】＋ `AnimationClipByGuid` 那条接通
                    //
                    // 为什么单开一节（两件账都**不能算「已验收」**）：
                    //  · **A191**：原版有 4 个「宿主对象」我们工程里根本不存在（旁挂 `_missingTargets` 逐条记着），
                    //    真因是战场一直**平铺**建 ⇒ 那几个组件永远解析不到；
                    //  · **A201**：`ScenarioAnimationBlend` 的 `clipLoader`（= `ScenarioBlendableFactory.AnimationClipByGuid`）
                    //    那一跳 2026-10-07 落地后**一次都没在 Unity 里跑过** —— 而 A191 没做时
                    //    `myAnimation` 恒 null、`DoScenarioBlend` 第一道门就 return ⇒ **`clipLoader` 一次都不会被调到**，
                    //    任何「clipLoader 通过」的断言都是**空跑**。
                    //
                    // 判据（两条都不是我们自己的常量）：
                    //  · A191 = 旁挂 `arenas/<场>/<场>_groups.json`（由 `工具/gen_arena_groups.py` **直读原版场景包**产出，
                    //    含每个节点「原版是否带 `Animation`」）—— **逐条对着我们建出来的 prefab 核**；
                    //  · A201 = 原版那两条 clip 的 **assetGUID 来自 SO**（`EnvironmentConditions.All[].animationsToChange[].clip`），
                    //    取它们的是**生产那份** `clipLoader`。
                    //
                    // ⚠️ 本段**只读 + 一次性实例化**，末尾 `DestroyImmediate` 收干净（批处理下没有帧循环，
                    //    `Destroy` 不生效 —— CLAUDE §三）。编辑模式**不跑 `Awake/OnEnable`**（这一族组件都没标
                    //    `[ExecuteAlways]`，实测 grep 过）⇒ 实例化**不写任何全局状态**。
                    // ⚠️ **如实记一条没做到的**：本段**没有**驱动 `ScenarioAnimationBlend.DoScenarioBlend` 本体
                    //    —— 它读的是 `manager.CurrentItem`，而 `EnvironmentApplier.CurrentItem` 是 `get; private set;`，
                    //    编辑器侧**没有注入口**；要注入就得给生产类开一个测试专用的口子，而那正是
                    //    A185 那条裁定否掉的做法。⇒ 这里验到「**生产那份 `clipLoader` 真的把 clip 取回来了、
                    //    并且 `Animation` 真的收得下它**」为止。
                    // ============================================================
                    {
                        var dkPf = Resources.Load<GameObject>("ArenaPrefabs/battlearenadarkangels");
                        var tvPf = Resources.Load<GameObject>("ArenaPrefabs/battlearenatauviorla");
                        Check(dkPf != null && tvPf != null,
                              "★ 前置：A191 那两件 arena prefab 取得到（`Resources/ArenaPrefabs/…`）"
                            + $"（darkangels {(dkPf != null ? "✓" : "✗")} · tauviorla {(tvPf != null ? "✓" : "✗")}）");
                        if (dkPf != null && tvPf != null)
                        {
                            // 本地的两个**探测用**小工具（不是判据）：按 `/` 路径逐段下沉、以及反着算一条路径。
                            // ⚠️ 名字比较一律走 `CardPresentation.EnvironmentApplier.Norm`（**Trim** ——
                            //   原版真有带尾随空格的名字 `'Railgun Turret 1 Target '`）。
                            System.Func<Transform, string, Transform> findPath = (rt, path) =>
                            {
                                if (rt == null || string.IsNullOrEmpty(path)) return null;
                                var segs = path.Split('/');
                                // 🔴 2026-10-11（R1）**订正**：原来写的是
                                //   `Transform cur = 根名相等 ? rt : null;` + `for (int i = (cur != null ? 1 : 0); cur != null && …)`
                                //   —— 一旦**首段不等于根名**，`cur` 就是 null，而 for 的继续条件正是 `cur != null`
                                //   ⇒ **循环一次都不进、直接 `return null`** ⇒ 这个助手**只能找到「以 prefab 根名开头」
                                //   的路径**。而旁挂（`arenas/<场>/<场>_groups.json`）里每条路径都是**相对 prefab 根**的
                                //   （`Scenario/…`），prefab 根名实测 = `<场>`（13/13）⇒ 原来在本段里**每一次调用
                                //   都返回 null**（7 条红的共同发端：节点、clip 路径、tau 那两件、`adds` 全中）。
                                //   ✅ 正确语义 = 「首段就是根名」与「路径相对根」**两种写法都支持**。
                                int i = CardPresentation.EnvironmentApplier.Norm(rt.name)
                                        == CardPresentation.EnvironmentApplier.Norm(segs[0]) ? 1 : 0;
                                Transform cur = rt;
                                for (; cur != null && i < segs.Length; i++)
                                {
                                    Transform nx = null;
                                    for (int c = 0; c < cur.childCount; c++)
                                        if (CardPresentation.EnvironmentApplier.Norm(cur.GetChild(c).name)
                                            == CardPresentation.EnvironmentApplier.Norm(segs[i]))
                                        { nx = cur.GetChild(c); break; }
                                    cur = nx;
                                }
                                return cur;
                            };
                            System.Func<Transform, Transform, string> pathOf = (t, rt) =>
                            {
                                var s = t.name.Trim();
                                for (var p = t.parent; p != null && p != rt; p = p.parent)
                                    s = p.name.Trim() + "/" + s;
                                return s;
                            };
                            // 旁挂 `parent` 那条字段的含义 = **父的路径**，而 `pathOf` 给的是**对象自己的路径**
                            // （含自己的名字）⇒ 拿它去比 `parent` **父子恒差一段**。
                            // 🔴 2026-10-11（R2）**订正**：改挂那两段原来用的就是 `pathOf(tr, root)` ⇒
                            //   「凡是找得到的对象一律判错」，而不是「位置真的不对」（实测 83 条不匹配里
                            //   **63 条**的形状恰恰是 `want + "/" + name`）。
                            //   ⚠️ 对象的父**就是 prefab 根**时回空串，与旁挂里 `parent = ""`（根）**同义**。
                            System.Func<Transform, Transform, string> parentPathOf =
                                (t, rt) => (t == null || t == rt) ? "" : pathOf(t, rt);
                            // 路径比较**按段 Trim**（与 `findPath` 的逐段 `Norm` 同一套语义）：`pathOf` 每段都
                            // `Trim()`，而旁挂是原样字符串 ⇒ 某一段带尾随空格时会**静默假红**（旁挂里真有这种
                            // 名字：`'Railgun Turret 1 Target '`；实测今天两场的 `parent` 段没有带空格的，0/121）。
                            System.Func<string, string> normPath = p =>
                            {
                                if (string.IsNullOrEmpty(p)) return "";
                                var ss = p.Split('/');
                                var sb = new System.Text.StringBuilder();
                                for (int k = 0; k < ss.Length; k++)
                                {
                                    if (k > 0) sb.Append('/');
                                    sb.Append(CardPresentation.EnvironmentApplier.Norm(ss[k]));
                                }
                                return sb.ToString();
                            };
                            // 「旁挂要求的那个父**在树里存在**吗」—— 用来把「我们摆错了」（存在却不是它）
                            // 与「上游闸门挡掉的」（树里没有）分开。⚠️ 空串 = 父就是 prefab 根（当然在，
                            // 而 `findPath` 遇空串按约定回 null，所以要在这里单独放行）。
                            System.Func<Transform, string, bool> parentInTree =
                                (rt, p) => string.IsNullOrEmpty(p) || findPath(rt, p) != null;
                            // 把一个旁挂目标翻成对象 —— **走生产那份解析器**（`SceneResolver`），
                            // 免得在 Editor 里把「按名字 + 最近位置找对象」这条判据写第二遍（铁律 6）。
                            System.Func<CardPresentation.IEnvTargetResolver, string, float[], GameObject> findTarget =
                                (res, name, pos) => res.GoOf(new CardPresentation.EnvBlendables.Target
                                { path = name, leaf = name, pos = pos, kind = "go" });

                            var dkInst = UnityEngine.Object.Instantiate(dkPf);
                            var tvInst = UnityEngine.Object.Instantiate(tvPf);
                            try
                            {
                                // ---- A191 ①：darkangels 那一棵子树 ----
                                var dkSc = ArenaBuilder.LoadGroups("battlearenadarkangels");
                                // 🔴 2026-10-11（R4）**三档拆开** —— 原来只有一个 `dkBad`，分母还是「**旁挂写的条数**」
                                //   （不是「应当发生的条数」）⇒ `节点 0/2 · 改挂 0/61` 读起来像 0% 完成，
                                //   实际是 51/61 早就位（D5 §六·2 那条）。三档：
                                //     · `dkMove`     = 找得到 + 父路径**逐条对上**
                                //     · `dkBad`      = **必须 0 的那一档**：父路径**在树里存在却不是它** ⇒ 我们摆错了
                                //                      （外加「该带 `Animation` 的没带」）
                                //     · `dkNotBuilt` = 旁挂里有、**树里没有**（自身对象缺，或它要求的**父节点**缺）
                                //                      ⇒ 闸门挡掉 / prefab 没随旁挂重建，**逐条点名**、不算我们摆错
                                //   ⛔ **一个数都别写死**：`工具/gen_arena_groups.py` 补上那四道闸门（A343）之后，
                                //   被挡掉的对象会转成 `nodes[]`（建成**空节点**）⇒ 节点数升、`notBuilt` 降到 0（D4 §三）。
                                //    ⚠️ 2026-10-11 本件写报告时实测：**旁挂那半边已经改了**（darkangels `nodes[]`
                                //    **2 → 12**、tau **8 → 36**），而 `Resources/ArenaPrefabs/*.prefab` **还没重建**
                                //    ⇒ `dkNode == nodes.Length` 会**正确地红**（`旁挂 12 个节点、prefab 里只有 2 个`）
                                //    直到跑一次 `ArenaBuilder.BuildArenaPrefabs`。**这条红是「不同步」，不是本断言坏了。**
                                int dkNode = 0, dkAnim = 0, dkMove = 0, dkBad = 0, dkNotBuilt = 0, dkMissNode = 0;
                                string dkBadWhat = "", dkNotBuiltWhat = "", dkMissNodeWhat = "";
                                if (dkSc != null)
                                {
                                    for (int i = 0; i < dkSc.nodes.Length; i++)
                                    {
                                        var n = dkSc.nodes[i];
                                        var tr = findPath(dkInst.transform, n.path);
                                        if (tr == null) { dkMissNode++; dkMissNodeWhat += $"`{n.path}` "; continue; }
                                        dkNode++;
                                        bool hasAnim = tr.GetComponent<Animation>() != null;
                                        if (n.animation != 0 && hasAnim) dkAnim++;
                                        if (n.animation != 0 && !hasAnim) { dkBad++; dkBadWhat += $"`{n.path}` 没带 `Animation` "; }
                                    }
                                    var resDk = new CardPresentation.EnvironmentApplier.SceneResolver(dkInst.transform);
                                    for (int i = 0; i < dkSc.targets.Length; i++)
                                    {
                                        var t = dkSc.targets[i];
                                        var tr = findTarget(resDk, t.name, t.pos);
                                        if (tr == null) { dkNotBuilt++; dkNotBuiltWhat += $"目标 `{t.name}`（自身不在树里）"; continue; }
                                        // 🔴 2026-10-11（R2）：比的是**父的路径**（`parentPathOf`），不是对象自己的路径
                                        var got = parentPathOf(tr.transform.parent, dkInst.transform);
                                        if (normPath(got) != normPath(t.parent))
                                        {
                                            // 要求的父**在树里存在** ⇒ 那是我们摆错了（必须 0）；
                                            // 父**不在树里** ⇒ 上游闸门挡掉的那一批（D4 §二），不算我们摆错
                                            if (parentInTree(dkInst.transform, t.parent))
                                            { dkBad++; dkBadWhat += $"`{t.name}` 的父是 `{got}`、旁挂要求 `{t.parent}` "; }
                                            else
                                            { dkNotBuilt++; dkNotBuiltWhat += $"`{t.name}` 要求的父 `{t.parent}` 不在树里 "; }
                                            continue;
                                        }
                                        // 🔴 2026-10-14（#1 · A191）：**targets 循环也要数 `Animation`** —— 与 `adds`
                                        //   循环同形。计数口径本来是**三张表之和**（`_stats.animation` 的算法 =
                                        //   `工具/gen_arena_groups.py:651-653`），而这里原来只数 nodes + adds。
                                        //   本轮重算把 `Directional Light`（`animation = 1`）从 `adds[]` 挪进了
                                        //   `targets[]` ⇒ 只数两张表时 `dkAnim` 从 2 掉成 1。
                                        //   实据（现读 `Resources/ArenaPrefabs/battlearenadarkangels.prefab`）：
                                        //   classID `111`（`Animation`）恰好 **2** 个 —— 在 `Directional Light`
                                        //   与 `Battle Arena Dark Angels baked` 上，与旁挂逐条对上。
                                        //   ⛔ 别把上面那条改成 `dkAnim >= 1`（那是**弱化**：放掉了「`Directional Light`
                                        //   到底有没有补上 `Animation`」）。
                                        if (t.animation != 0)
                                        {
                                            if (tr.GetComponent<Animation>() != null) dkAnim++;
                                            else { dkBad++; dkBadWhat += $"`{t.name}` 没补上 `Animation` "; }
                                        }
                                        dkMove++;
                                    }
                                    for (int i = 0; i < dkSc.adds.Length; i++)
                                    {
                                        var t = dkSc.adds[i];
                                        // 🔴 2026-10-11（R3）：原来用 `findPath(root, t.name)` 查一个**单段名字** ——
                                        //   单段路径要求它**等于根名** ⇒ 恒 null（prefab 根名实测是 `<场>`）。
                                        //   `adds[]` 只带 `name`/`pos`（**没有 path**）⇒ 走**生产那份解析器**（名字 + 最近位置）。
                                        var tr = findTarget(resDk, t.name, t.pos);
                                        if (tr == null) { dkNotBuilt++; dkNotBuiltWhat += $"`adds` 里的 `{t.name}` 不在树里 "; continue; }
                                        if (t.animation != 0)
                                        {
                                            if (tr.GetComponent<Animation>() != null) dkAnim++;
                                            else { dkBad++; dkBadWhat += $"`{t.name}` 没补上 `Animation` "; }
                                        }
                                    }
                                }
                                // 🔴 **这一条是「分组节点」这四个字的全部意义**：原版 `Dark Angels Void Combat animations`
                                //    （assetGUID `aac3fe87…`）那条 clip 的曲线 `m_Path` 是**相对 `Animation` 组件那个
                                //    GameObject** 的，实读 **13 条曲线 / 4 条唯一路径**（`wf_prefabs_extra.bundle` 直读）：
                                //    `Turret 1 barrel`（Position+Rotation+Scale）· `Turret 2 barrel`（Position+Rotation+Scale）·
                                //    `Turret missile joint`（Rotation+Scale）·
                                //    `Turret 1 barrel/Lance Fire (5)` 与 `Turret 2 barrel/Lance Fire (5)`（Float 各一条）。
                                //    ⚠️ 2026-10-11（D5）**订正**：这里原来只抄了 1 号炮塔那条路径，**漏了 2 号炮塔那条**。
                                //    ⇒ 只建节点、不把子件挂回去（空壳）在这里**当场红**。
                                //    🧨 **改坏法**：把 `ArenaBuilder.BuildContent` 里那句 `ApplyGroupNodes(mf, root)`
                                //    注释掉（或只建节点、把 `targets` 那一段删掉）⇒ 下面这条红。
                                //    ⚠️ 门槛只压在**前三条**上（它们才是我方应当建出来的）：后两条（`…/Lance Fire (5)`）
                                //    是**上游闸门挡掉**的（原版自己 `m_IsActive=False` + `renderMode=5` ⇒
                                //    `ArenaBuilder.cs:1997-2016` 第一道闸门；D4 §二 族 1）—— **prefab 重建前**
                                //    应当在树里**找不到**它俩，而 `gen_arena_groups.py` 把它们收进 `nodes[]` 之后
                                //    （空节点）**重建 prefab 就会找得到** ⇒ 这条断言**两种状态都绿**（不必改）。
                                string[] clipPaths =
                                {
                                    "Scenario/Battle Arena Dark Angels baked/Turret 1 barrel",
                                    "Scenario/Battle Arena Dark Angels baked/Turret 2 barrel",
                                    "Scenario/Battle Arena Dark Angels baked/Turret missile joint",
                                    "Scenario/Battle Arena Dark Angels baked/Turret 1 barrel/Lance Fire (5)",
                                    "Scenario/Battle Arena Dark Angels baked/Turret 2 barrel/Lance Fire (5)",
                                };
                                int clipHit = 0, clipFirst3 = 0;
                                for (int i = 0; i < clipPaths.Length; i++)
                                    if (findPath(dkInst.transform, clipPaths[i]) != null)
                                    { clipHit++; if (i < 3) clipFirst3++; }
                                Check(clipFirst3 == 3,
                                      $"★ A191：分组节点**真的当父节点**了 —— 那条 clip 的路径 {clipHit}/{clipPaths.Length} 条解析得到"
                                    + "（前三条**必须全中**：`Turret 1 barrel` / `Turret 2 barrel` / `Turret missile joint`；"
                                    + "`…/Lance Fire (5)` 两条 = 原版自己 `m_IsActive=False`+`renderMode=5` 被闸门挡掉，"
                                    + "prefab 重建前找不到 / 重建后找得到，见 D4 §二 族 1）");
                                Check(dkSc != null && dkBad == 0 && dkNode == dkSc.nodes.Length && dkAnim >= 2,
                                      $"★ A191：darkangels 的旁挂**结构逐条对上**（节点 {dkNode}/{dkSc?.nodes.Length ?? -1} · "
                                    + $"带 `Animation` 的 {dkAnim} 个 —— 原版那个分组节点 + `Directional Light` 共 2 个）"
                                    + $"（改挂 {dkMove} 条、父路径全对"
                                    + (dkBad > 0 ? $"；**{dkBad} 条摆错了**：{dkBadWhat}" : "")
                                    + (dkMissNode > 0 ? $"；**旁挂要的 {dkMissNode} 个节点 prefab 里没有**"
                                                     + "（旁挂与 prefab **不同步** ⇒ 跑一次 `ArenaBuilder.BuildArenaPrefabs`）："
                                                     + dkMissNodeWhat : "")
                                    + (dkNotBuilt > 0 ? $"；**{dkNotBuilt} 条树里没有**（闸门挡掉 / prefab 没重建，"
                                                     + $"见 D4 §二）：{dkNotBuiltWhat}" : "")
                                    + "）");

                                // ---- A191 ②：tauviorla 那三个炮塔族（`_missingTargets` 里 4 条中有 3 条在这一场）----
                                var tvSc = ArenaBuilder.LoadGroups("battlearenatauviorla");
                                // 同 #2 的三档（R4）。⚠️ 2026-10-11 本件写报告时实测：旁挂那半边**已经**把被闸门挡掉的
                                // 对象收进 `nodes[]` 了（darkangels 2→12 · tau 8→36），而 prefab **还没重建**
                                // ⇒ 节点那一档现在会正确地报「不同步」。⛔ 一个数都别写死。
                                int tvNode = 0, tvAnim = 0, tvMove = 0, tvBad = 0, tvNotBuilt = 0, tvMissNode = 0;
                                string tvBadWhat = "", tvNotBuiltWhat = "", tvMissNodeWhat = "";
                                if (tvSc != null)
                                {
                                    for (int i = 0; i < tvSc.nodes.Length; i++)
                                    {
                                        var n = tvSc.nodes[i];
                                        var tr = findPath(tvInst.transform, n.path);
                                        if (tr == null) { tvMissNode++; tvMissNodeWhat += $"`{n.path}` "; continue; }
                                        tvNode++;
                                        if (n.animation != 0)
                                        {
                                            if (tr.GetComponent<Animation>() != null) tvAnim++;
                                            else { tvBad++; tvBadWhat += $"`{n.path}` 没带 `Animation` "; }
                                        }
                                    }
                                    var resTv = new CardPresentation.EnvironmentApplier.SceneResolver(tvInst.transform);
                                    for (int i = 0; i < tvSc.targets.Length; i++)
                                    {
                                        var t = tvSc.targets[i];
                                        var tr = findTarget(resTv, t.name, t.pos);
                                        if (tr == null) { tvNotBuilt++; tvNotBuiltWhat += $"目标 `{t.name}`（自身不在树里）"; continue; }
                                        // 🔴 2026-10-11（R2）：比的是**父的路径**（`parentPathOf`），不是对象自己的路径
                                        var got = parentPathOf(tr.transform.parent, tvInst.transform);
                                        if (normPath(got) != normPath(t.parent))
                                        {
                                            // 父**在树里存在**却不在它下面 ⇒ 我们摆错了（必须 0）；
                                            // 父**不在树里** ⇒ 上游闸门挡掉 / prefab 没随旁挂重建（见 D4 §二）
                                            if (parentInTree(tvInst.transform, t.parent))
                                            { tvBad++; tvBadWhat += $"`{t.name}` 的父是 `{got}`、旁挂要求 `{t.parent}` "; }
                                            else
                                            { tvNotBuilt++; tvNotBuiltWhat += $"`{t.name}` 要求的父 `{t.parent}` 不在树里 "; }
                                            continue;
                                        }
                                        tvMove++;
                                    }
                                }
                                // `_missingTargets` 里那 4 条 = `Battle Arena Dark Angels baked` + `Railgun Turret 1/2`
                                // + 两个 `Railgun turret`（同一 leaf 两条）+ **`Railgun Turret N Target` 两件**
                                // （🔴 那两件**从来不在** `_missingTargets` 里 —— 那张表只查 blendable 自己的 target、
                                //  不查「target 组件自己的字段」；判据 → `资料/普查产出_1011/W9_A196_A210_A211.md` §五·4）
                                var tvTgt1 = findPath(tvInst.transform,
                                    "Scenario/Battle Arena Tau Viorla Baked/Railgun Turret 1/Railgun Turret 1 Target ");
                                var tvTurret = findPath(tvInst.transform,
                                    "Scenario/Battle Arena Tau Viorla Baked/Railgun Turret 1/Railgun Turret Base.001/Cylinder.001/Railgun turret");
                                // ⚠️ 这两件**必须**用 `findPath` 查（不是 `findTarget`）：判据是「它俩在**树里的那一条
                                //    父链**上」（`Railgun Turret 1/` 那一段带不带、带得对不对，正是本条要验的东西）。
                                //    ⚠️ 2026-10-11：本条原来红**只因 R1 那个坏助手**（两件实测都在，父链逐字对得上）。
                                Check(tvTgt1 != null && tvTurret != null,
                                      "★ A191：tauviorla 那两个「清单外的」也建出来了 —— `Railgun Turret 1 Target `"
                                    + "（**名字原版就带尾随空格**，照抄 —— `findPath` 按 `Norm` 逐段比，比得上）"
                                    + "与 `…/Cylinder.001/Railgun turret`"
                                    + $"（{(tvTgt1 != null ? "✓" : "✗")} / {(tvTurret != null ? "✓" : "✗")}）"
                                    + " —— 前者是 `LookAtConstrainWIP.target` 指的对象、后者是 `AnimFXController` 的宿主");
                                Check(tvSc != null && tvBad == 0 && tvNode == tvSc.nodes.Length && tvAnim >= 2,
                                      $"★ A191：tauviorla 的旁挂**结构逐条对上**（节点 {tvNode}/{tvSc?.nodes.Length ?? -1} · "
                                    + $"带 `Animation` 的 {tvAnim} 个 = 两个炮塔节点）"
                                    + $"（改挂 {tvMove} 条、父路径全对"
                                    + (tvBad > 0 ? $"；**{tvBad} 条摆错了**：{tvBadWhat}" : "")
                                    + (tvMissNode > 0 ? $"；**旁挂要的 {tvMissNode} 个节点 prefab 里没有**"
                                                     + "（旁挂与 prefab **不同步** ⇒ 跑一次 `ArenaBuilder.BuildArenaPrefabs`）："
                                                     + tvMissNodeWhat : "")
                                    + (tvNotBuilt > 0 ? $"；**{tvNotBuilt} 条树里没有**（闸门挡掉 / prefab 没重建，"
                                                     + $"见 D4 §二）：{tvNotBuiltWhat}" : "")
                                    + "）");

                                // ---- 🆕 2026-10-12（A340）：旁挂那三层 → 组件这一跳（`sounds` / `exitSounds` / `modules`）----
                                //   判据 = **原版包直读**（`gen_env_blendables.py` 的 `pack_animfx_defs` 新收这三层；
                                //   逐条 → `资料/普查产出_1012/W3_AnimFX旁挂.md` §二）：两个
                                //   `TauCannonAnimationStopper` 各 **1 条 `sounds`**、`exitSounds` / `modules` **空**。
                                //   🔴 探的是**生产那一跳**（`ScenarioBlendableFactory.Create` + `SceneResolver.GoOf`），
                                //   ⛔ 别在 Editor 里另写一套「按名字 + 最近位置找对象」（铁律 6）。
                                //   🧨 **改坏法（A340）**：把 `gen_env_blendables.py` 的 `target_fields()` 里
                                //   `if cn == 'AnimFXController'` 那两行去掉、重生成一次旁挂 ⇒ `sounds.count` 缺键
                                //   ⇒ 工厂**出声**且 `sounds.Length == 0` ⇒ 下面第一条红（改前这里恒 0 条）。
                                {
                                    var resTv2 = new CardPresentation.EnvironmentApplier.SceneResolver(tvInst.transform);
                                    int nFx = 0, nWired = 0, nCue = 0;
                                    string fxWhat = "";
                                    // ⚠️ 循环变量**不许叫 `it`** —— 本方法的 `it`（`CardInteraction`）在
                                    //    `BuildScene` 那行 `out` 参数上，同名会 CS0136（本仓踩过）。
                                    foreach (var itFx in CardPresentation.EnvBlendables.ForArena("battlearenatauviorla"))
                                    {
                                        if (itFx == null || itFx.cls != "TauCannonAnimationStopper") continue;
                                        CardPresentation.EnvBlendables.Target at = null;
                                        foreach (var t in itFx.targets)
                                            if (t != null && t.kind == "animfx") { at = t; break; }
                                        if (at == null) continue;
                                        nFx++;
                                        // ①「三层在位」的证书：三个 `count` 键**都得在**（缺 = 旁挂是旧版 / packer 没接上）
                                        bool layers = at.GetF("sounds.count", -1f) >= 0f
                                                   && at.GetF("exitSounds.count", -1f) >= 0f
                                                   && at.GetF("modules.count", -1f) >= 0f;
                                        // ② 原版那一条 cue 的名字 —— **写死**（判据是原版包，不是我们的实现）。
                                        //    ⚠️ 别按名字直觉猜反：**近的那个是 `Far`**（W3 §五·5.1）。
                                        string wantCue = at.path.Contains("Railgun Turret 2") ? "Railgun Turret"
                                                       : at.path.Contains("Railgun Turret 1") ? "Railgun Turret Far"
                                                       : null;
                                        var host = resTv2.GoOf(at);
                                        var st = host != null
                                               ? CardPresentation.ScenarioBlendableFactory.Create(itFx, host, resTv2, true)
                                                 as CardPresentation.TauCannonAnimationStopper
                                               : null;
                                        var fa = st != null ? st.animFXController : null;
                                        var s0 = (fa != null && fa.sounds != null && fa.sounds.Length == 1) ? fa.sounds[0] : null;
                                        bool cueOk = s0 != null && wantCue != null && s0.sound == wantCue;
                                        // 5 个数值逐字段对（原版那一条：`time = 0` · 3D · 重复 5 次 · 间隔 0.75s）
                                        bool fieldsOk = s0 != null && s0.time == 0f && !s0.is2d && s0.repeat
                                                     && s0.loops == 5 && Mathf.Abs(s0.timeInterval - 0.75f) < 1e-4f;
                                        // 两层**原版就是空的**（写 `0`）= 数据，不是「没收」
                                        bool emptyOk = fa != null && fa.exitSounds != null && fa.exitSounds.Length == 0
                                                    && fa.modules != null && fa.modules.Count == 0;
                                        if (cueOk && WarpforgeVFX.WFSoundBank.HasCue(s0.sound)) nCue++;
                                        if (layers && cueOk && fieldsOk && emptyOk) nWired++;
                                        else fxWhat += $"[{at.path}：三层键 {(layers ? "✓" : "✗")} · "
                                                     + $"宿主 {(host != null ? "✓" : "✗")} · 组件 {(st != null ? "✓" : "✗")} · "
                                                     + $"`sounds` {(fa != null && fa.sounds != null ? fa.sounds.Length : -1)} 条 · "
                                                     + $"cue `{(s0 != null ? s0.sound : "<无>")}`（期望 `{wantCue}`） · "
                                                     + $"`exitSounds`/`modules` {(fa != null && fa.exitSounds != null ? fa.exitSounds.Length : -1)}"
                                                     + $"/{(fa != null && fa.modules != null ? fa.modules.Count : -1)}] ";
                                    }
                                    Check(nFx == 2 && nWired == 2,
                                          "★ A340：两个 `TauCannonAnimationStopper.animFXController` 的 `sounds` 层"
                                        + "**照旁挂建出来了**（原版那 2 个实例各有 1 条；改前这里恒 0）"
                                        + $"（{nWired}/{nFx} 条逐字段对上"
                                        + (fxWhat.Length > 0 ? $"，没对上的：{fxWhat}" : "") + "）");
                                    // 跨文件那一条：这两条 cue 在 `Resources/animfx_sounds.json` 里**真解得到**
                                    //   （`HasCue` 只查表、不播音、不计 `BadCues`）。⛔ 没这条的话
                                    //   「组件字段对上了但表里没这个 cue」会静默不播（红线）。
                                    Check(nCue == 2,
                                          $"★ A340（跨文件）：这两条 cue 在 `Resources/animfx_sounds.json` 里**都解得到**"
                                        + $"（{nCue}/2；判据 = `WarpforgeVFX.WFSoundBank.HasCue`）—— 🧨 让 "
                                        + "`工具/import_original_sfx.py` 少收这两条 ⇒ 红");
                                }

                                // ---- 🆕 2026-10-12（A341）：`preventDestroy = false` 那条支路（工厂补排自毁）----
                                //   🔴 **今天数据走不到**（能走到工厂的那两个实例 `preventDestroy` 都是 `1`；全库另 3 个
                                //   `= 0` 的实例**不归任何 blendable 管**，见 `W3` §七·1）⇒ **合成一条目标**去点它。
                                //   ⛔ 不合成就写断言 = **恒真的假断言**（本仓明令禁止）。
                                //   ⚠️ `SelfDestroyScheduled` 是**累积**计数、没有重置 ⇒ 一律**读差**，别写绝对值。
                                //   ⚠️ 探针宿主用**独立根对象**（不挂在 tvInst 里 ⇒ 不扰动别的断言、也不被 A191 那棵树遍历到）；
                                //   解析器用 `SceneResolver(null)` —— `FindNearest` 在 `root == null` 时改走**全场景按名找**
                                //   （`EnvironmentApplier.cs:743-762`）。
                                //   🧨 **改坏法（A341）**：删掉 `MakeAnimFx` 里
                                //   `if (!c.preventDestroy && c.destroyTime > 0f) { … }` 那一段 ⇒ 第一条红（负对照仍绿）。
                                //   ⚠️ 与 `W3` §五·5.2 那段**唯一**的差别：断言里**没有** `stopper != null` ——
                                //   `DestroyImmediate` 之后那个组件引用**按 Unity 的 `==` 就是 null**（宿主都没了）
                                //   ⇒ 拿它当「建出来了」的判据会**恒假**。要判的是「支路执行了 + 宿主当场没了」这两件事。
                                {
                                    CardPresentation.EnvBlendables.Item ProbeItem(string probeName, float pd, float dt)
                                        => new CardPresentation.EnvBlendables.Item
                                        {
                                            cls = "TauCannonAnimationStopper",
                                            owner = probeName, ownerLeaf = probeName,
                                            targets = new[]
                                            {
                                                new CardPresentation.EnvBlendables.Target
                                                {
                                                    leaf = probeName, path = probeName, kind = "animfx",
                                                    pos = new float[] { 0f, 0f, 0f },
                                                    // 三个 `count` 键也给上（缺键 = 「旁挂是旧版」那一档会出声，
                                                    //   这里要探的是**自毁**那条支路，别让它被别的出声淹掉）
                                                    fields = new[]
                                                    {
                                                        new CardPresentation.EnvBlendables.TargetField { k = "preventDestroy",   f = pd },
                                                        new CardPresentation.EnvBlendables.TargetField { k = "destroyTime",      f = dt },
                                                        new CardPresentation.EnvBlendables.TargetField { k = "sounds.count",     f = 0f },
                                                        new CardPresentation.EnvBlendables.TargetField { k = "exitSounds.count", f = 0f },
                                                        new CardPresentation.EnvBlendables.TargetField { k = "modules.count",    f = 0f },
                                                    },
                                                },
                                            },
                                        };
                                    var resProbe = new CardPresentation.EnvironmentApplier.SceneResolver(null);

                                    var probe = new GameObject("A341 Probe Host");
                                    try
                                    {
                                        int before341 = CardPresentation.ScenarioBlendableFactory.SelfDestroyScheduled;
                                        CardPresentation.ScenarioBlendableFactory.Create(
                                            ProbeItem("A341 Probe Host", 0f, 1.8f), probe, resProbe, true);
                                        int now341 = CardPresentation.ScenarioBlendableFactory.SelfDestroyScheduled;
                                        Check(now341 == before341 + 1,
                                              "★ A341：`preventDestroy = false` 那条支路**真的执行了**（工厂补排了自毁）"
                                            + $"（计数 {before341} → {now341}）—— 🧨 删掉 `MakeAnimFx` 里那一段 ⇒ 本条红");
                                        // ⚠️ 批处理下 `Destroy`（延时销毁）不生效 ⇒ 编辑模式那一档工厂走
                                        //   `DestroyImmediate` ⇒ 宿主**当场**没了；运行时那一档是
                                        //   `Destroy(go, destroyTime)`（与原版 `OnEnable` 第二句逐字同路）——
                                        //   两档**判据同一句**，差别只在「排定」vs「当场」。
                                        //   只断计数不断宿主的话，「排了但没销」这一档分不出来。
                                        Check(probe == null,
                                              "★ A341：批处理这一档（`Application.isPlaying == false`）走的是 "
                                            + "`DestroyImmediate` ⇒ 探针宿主**当场没了**"
                                            + "（⚠️ Play 模式下这一档不成立 —— 那时是 `Destroy(go, 1.8s)`、"
                                            + "要等下一次销毁才没；本自检只在批处理 / 编辑模式跑）");
                                    }
                                    finally { if (probe != null) UnityEngine.Object.DestroyImmediate(probe); }

                                    // 负对照：`preventDestroy = 1`（= 数据里能走到工厂的那两个实例那一档）
                                    //   ⇒ **不许**排定、也**不许**销毁。没有这条的话「计数 +1」可能只是「谁都加」。
                                    var probe2 = new GameObject("A341 Probe Host 2");
                                    try
                                    {
                                        int before2 = CardPresentation.ScenarioBlendableFactory.SelfDestroyScheduled;
                                        CardPresentation.ScenarioBlendableFactory.Create(
                                            ProbeItem("A341 Probe Host 2", 1f, 1.8f), probe2, resProbe, true);
                                        Check(CardPresentation.ScenarioBlendableFactory.SelfDestroyScheduled == before2
                                              && probe2 != null,
                                              "★ A341（负对照）：`preventDestroy = true` ⇒ **不排自毁、宿主也还在**"
                                            + $"(计数仍是 {CardPresentation.ScenarioBlendableFactory.SelfDestroyScheduled}）");
                                    }
                                    finally { if (probe2 != null) UnityEngine.Object.DestroyImmediate(probe2); }
                                }

                                // ---- A201：`clipLoader` 那条接通**真的跑一遍** ----
                                var guids = new System.Collections.Generic.List<string>();
                                foreach (var soItem in CardPresentation.EnvironmentConditions.All)
                                {
                                    if (soItem == null || soItem.animationsToChange == null) continue;
                                    foreach (var atc in soItem.animationsToChange)
                                        if (atc != null && !string.IsNullOrEmpty(atc.clip) && !guids.Contains(atc.clip))
                                            guids.Add(atc.clip);
                                }
                                Check(guids.Count == 2,
                                      $"★ 前置：SO 的 `animationsToChange[]` 里有 2 条 clip 的 assetGUID（现在 {guids.Count} 条）"
                                    + " —— 判据 = `数据/游戏数据/environment_conditions.json`（原版 SO 直读）");

                                CardPresentation.EnvBlendables.Item it2 = null;
                                foreach (var x in CardPresentation.EnvBlendables.ForArena("battlearenadarkangels"))
                                    if (x != null && x.cls == "ScenarioAnimationBlend"
                                        && CardPresentation.EnvironmentApplier.Norm(x.ownerLeaf)
                                           == "Battle Arena Dark Angels baked")
                                    { it2 = x; break; }
                                var resA = new CardPresentation.EnvironmentApplier.SceneResolver(dkInst.transform);
                                // 🔴 2026-10-11（R5 / D5 §二 #5）：宿主改用**生产那份解析器** ——
                                //   `SceneResolver.GoOf`（名字 + 最近位置），与运行时的 `PickSceneHost` /
                                //   `EnvironmentApplier.FindAnimationInScene` **同一条路**。原来这里用
                                //   `findPath(…, "Scenario/Battle Arena Dark Angels baked")` **自己又实现了一次**
                                //   「按路径找宿主」（同一件事两处实现，铁律 6），而它正是 R1 那个坏助手
                                //   ⇒ 恒 null ⇒ `Create` **一次都没被调用**（`bl == null`，
                                //   日志里那句 `组件 ✗` 的真正含义 = 「组件根本没建」，**不是**「建了但属性空」）。
                                var hostA = (it2 != null && it2.targets != null && it2.targets.Length > 0)
                                          ? resA.GoOf(it2.targets[0]) : null;
                                var bl = hostA != null
                                       ? CardPresentation.ScenarioBlendableFactory.Create(it2, hostA, resA, true)
                                         as CardPresentation.ScenarioAnimationBlend
                                       : null;
                                // 🧨 **改坏法**：把 `ArenaBuilder.ApplyGroupNodes` 里补 `Animation` 那两句去掉
                                //    （或让 `gen_arena_groups.py` 不写 `animation` 标记）⇒ 下面这条红
                                //    （`myAnimation` 恒 null、`DoScenarioBlend` 第一道门就 return、`clipLoader` 空跑）。
                                Check(bl != null && bl.myAnimation != null,
                                      "★ A201：`ScenarioAnimationBlend` 建出来了、**`myAnimation` 非空**"
                                    + "（这一条正是 A191 的意义所在：宿主不建 + `Animation` 不挂 ⇒ 它恒 null、"
                                    + "`DoScenarioBlend` 第一道门就 return ⇒ `clipLoader` 永远跑不到）"
                                    + $"（组件 {(bl != null ? "✓" : "✗")} · `myAnimation` {(bl != null && bl.myAnimation != null ? bl.myAnimation.name : "null")}）");

                                int clipOk = 0; string clipWhat = "";
                                bool clipRan = (bl != null && bl.clipLoader != null && bl.myAnimation != null);
                                if (clipRan)
                                    foreach (var g in guids)
                                    {
                                        var clip = bl.clipLoader(g);              // ← **生产那份** `AnimationClipByGuid`
                                        if (clip == null) { clipWhat += $"{g} 取不到 "; continue; }
                                        bl.AddAnimation(clip, false);             // 原版 `AddClip(clip, clip.name)`
                                        if (bl.myAnimation.GetClip(clip.name) == null)
                                        { clipWhat += $"{g}（`{clip.name}`）`AddClip` 之后取不回 "; continue; }
                                        clipOk++;
                                        clipWhat += $"{clip.name} ✓ ";
                                    }
                                // 🧨 **改坏法**：把 `资源/Resources/StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle`
                                //    挪走（或把 `extract_missing_shaders.py` 登记的 GUID 容器别名去掉）⇒ 这条红。
                                // 🔴 2026-10-11（R5）：**把「循环一次都没跑」如实说出来** —— 原来这句是
                                //   `（{clipOk}/{guids.Count}：{clipWhat}）`，前置不满足时 `clipWhat` 是空串，
                                //   日志写出来就是 `0/2：`，**读起来像「跑了 2 条都失败」**（实际一次都没执行）。
                                // ⚠️ 这一条是**取 clip 那一跳的探针**，**不是**「这两条 clip 会播」的证据 ——
                                //   生产路径 `DoScenarioBlend` 先按 `filterCode` 过滤（`ScenarioBlendables.cs:935`），
                                //   这里把两条不同 SO 的 clip 都 `AddClip` 到了同一个 `Animation` 上。
                                Check(clipOk == guids.Count && guids.Count > 0,
                                      $"★ A201：**GUID → `LoadAsset<AnimationClip>` → `Animation.AddClip` 这条链真的通**"
                                    + $"（{clipOk}/{guids.Count}："
                                    + (clipRan ? clipWhat
                                               : "**没跑到**（前置未满足：组件 / `clipLoader` / `myAnimation` 有一处是 null）")
                                    + "）—— 走的包 = `StreamingAssets/WarpforgeVFX/"
                                    + "wf_prefabs_extra.bundle`（原版源包的容器键**就是 GUID** ⇒ 这一跳与原版同路）");
                                // ⚠️ 2026-10-11：下面这半句原来**写死**「`Directional Light` 那颗是
                                //   `LightAnimationOrbital`、SO 写的是 `LightAnimationOrbit`」—— 改成**现读旁挂**
                                //   （那一颗的 `ownerLeaf` / `filterCode` 从 `env_blendables.json` 取，
                                //   SO 侧有哪些 `filterCode` 也从 `environment_conditions.json` 取），
                                //   免得它哪天变了没人知道（铁律 5：文档/断言里的判据要能被复核）。
                                string otherOwner = "", otherFilter = "";
                                foreach (var x in CardPresentation.EnvBlendables.ForArena("battlearenadarkangels"))
                                {
                                    if (x == null || x.cls != "ScenarioAnimationBlend" || x == it2) continue;
                                    otherOwner = x.ownerLeaf; otherFilter = x.GetS("filterCode"); break;
                                }
                                string soFilters = ""; bool otherMatched = false;
                                foreach (var soItem in CardPresentation.EnvironmentConditions.All)
                                {
                                    if (soItem == null || soItem.animationsToChange == null) continue;
                                    foreach (var atc in soItem.animationsToChange)
                                    {
                                        if (atc == null || string.IsNullOrEmpty(atc.filterCode)) continue;
                                        if (soFilters.IndexOf(atc.filterCode) < 0)
                                            soFilters += (soFilters.Length > 0 ? " · " : "")
                                                       + $"`{atc.filterCode}`（SO `{soItem.so}`）";
                                        if (atc.filterCode == otherFilter) otherMatched = true;
                                    }
                                }
                                Check(bl != null && bl.filterCode == "VoidCombatAnimations",
                                      "★ A201（**照抄原版数据，别去「修」**）：那颗的 `filterCode` = "
                                    + (bl != null ? $"`{bl.filterCode}`" : "**组件没建出来 ⇒ 读不到**")
                                    + "；SO `Dark Angels Void Combat` 那条 `animationsToChange[].filterCode` 写的就是"
                                    + " `VoidCombatAnimations` ⇒ 这一对**配得上**；而另一颗（旁挂 `ownerLeaf` = "
                                    + $"`{otherOwner}`）的 `filterCode` = `{otherFilter}`，在 SO 侧现读到的 "
                                    + $"{soFilters} 里" + (otherMatched ? "**能**" : "**一个都配不上**（差一个 `al`）")
                                    + " ⇒ **原版自己那一对永远配不上**，照抄（⛔ 别给它做模糊匹配）");
                            }
                            finally
                            {
                                UnityEngine.Object.DestroyImmediate(dkInst);
                                UnityEngine.Object.DestroyImmediate(tvInst);
                            }
                        }
                    }

                    // ============================================================
                    // 🆕 2026-10-07 波9批二（A137）**独立一节**：环境 prefab 里那批**常驻生成器**
                    //   （`ParticleSystemAreaSpawner` / `…Controller`；**不是** blendable，旁挂单开 `standalone` 一节）。
                    // 判据 = **原版 bundle 直读**（`工具/gen_env_blendables.py` 的 `collect_standalone`，逐条清单
                    //   在 `资料/普查产出_1007/波9批二_A137_自启spawner.md`）：
                    //   · 全库 **28 + 3** 个实例、**全在** `battleprefabs_vfxandmisc`（场景侧 0 个）；
                    //     被 `ScenarioParticleSpawnerBlender` 引用的只有 4 个 ⇒ 这一节 = **24 + 3**（其中
                    //     **16** 条 `useAutomaticSpawn=1` 自启、**8** 条 `=0` **只由 controller 驱动**）。
                    //   · 本程这一条 = `EnvironmentalCondition Ultramarines Bombardment`
                    //     （`OffensiveCards.Choices("Ultramarines")[1]` = 卡槽 0）：standalone =
                    //     **3 个 spawner（`Orbital 3/4/5 spawner`，三条都 `useAutomaticSpawn=0`）
                    //     + 1 个 controller**（3 条定义、weight 0.33 / chances 0.3）。
                    //   · ⚠️ 这 3 条的模板落在**同一 bundle 里另一个 prefab 根**上（`Orbital N repeat`），
                    //     **不在**环境 prefab 子树里 —— 专门盯「外部模板」那条兜底。
                    //   ⚠️ 本段**唯一**对场景的写 = 最后那句显式 `SpawnParticle()`（验池子链），
                    //     它生出来的副本随本实例在下面那次换环境时一起销毁。
                    // ============================================================
                    {
                        int nStan = CardPresentation.EnvironmentApplier.StandaloneDataCount();
                        Check(nStan == 27,
                              $"★ 旁挂 `standalone` 一节在位：{nStan} 条（原版直读 = 24 spawner + 3 controller；"
                            + "-1 = 那一节没读到 / 解析失败）—— 判据 → `工具/gen_env_blendables.py`");
                        if (ap0 != null)
                        {
                            Check(ap0.InstanceSpawnerCount == 3 && ap0.InstanceControllerCount == 1,
                                  $"★ `EnvironmentalCondition Ultramarines Bombardment` 实例上建出了 "
                                + $"3 spawner + 1 controller（现在 {ap0.InstanceSpawnerCount} + "
                                + $"{ap0.InstanceControllerCount}）—— 旁挂那 4 条**一条都不许少建**");
                            Check(ap0.StandaloneMissedCount == 0,
                                  $"★ 一条都没漏（没建出来的 = {ap0.StandaloneMissedCount}"
                                + (ap0.StandaloneMissedCount > 0 ? $"：{ap0.StandaloneMissed}" : "")
                                + "）—— 建不出时**点名**，不静默跳过");

                            ParticleSystemAreaSpawner[] bSp = ap0.CurrentInstance != null
                                ? ap0.CurrentInstance.GetComponentsInChildren<ParticleSystemAreaSpawner>(true)
                                : new ParticleSystemAreaSpawner[0];
                            ParticleSystemAreaSpawner sp3 = null, sp4 = null, sp5 = null;
                            for (int i = 0; i < bSp.Length; i++)
                            {
                                if (bSp[i] == null) continue;
                                string nn = bSp[i].name.Trim();
                                if (nn == "Orbital 3 spawner") sp3 = bSp[i];
                                else if (nn == "Orbital 4 spawner") sp4 = bSp[i];
                                else if (nn == "Orbital 5 spawner") sp5 = bSp[i];
                            }
                            ParticleSystemAreaSpawnerController ctl = ap0.CurrentInstance != null
                                ? ap0.CurrentInstance.GetComponentInChildren<ParticleSystemAreaSpawnerController>(true)
                                : null;

                            Check(bSp.Length == 3 && sp3 != null && sp4 != null && sp5 != null,
                                  $"★ 三条 `Orbital N spawner` 都建在正确的宿主上（数到 {bSp.Length} 个组件；"
                                + $"3/4/5 分别 {(sp3 != null ? "✓" : "✗")}{(sp4 != null ? "✓" : "✗")}"
                                + $"{(sp5 != null ? "✓" : "✗")}）");
                            Check(sp3 != null && sp3.maxPoolSize == 5 && !sp3.useAutomaticSpawn
                               && Mathf.Abs(sp3.boxSize.x - 250f) < 1e-3f
                               && Mathf.Abs(sp3.boxSize.y - 1f) < 1e-3f
                               && Mathf.Abs(sp3.boxSize.z - 50f) < 1e-3f
                               && Mathf.Abs(sp3.spawnRate - 0.73f) < 1e-3f
                               && Mathf.Abs(sp3.chances - 0.5f) < 1e-3f,
                                  "★ `Orbital 3 spawner` 的 6 个字段 = **原版实读值**（boxSize (250,1,50) · "
                                + $"spawnRate 0.73 · chances 0.5 · maxPool 5 · auto=false）—— 现在是 box=("
                                + $"{sp3?.boxSize.x},{sp3?.boxSize.y},{sp3?.boxSize.z}) rate={sp3?.spawnRate} "
                                + $"chances={sp3?.chances} pool={sp3?.maxPoolSize} auto={sp3?.useAutomaticSpawn}");
                            Check(sp4 != null && !sp4.useAutomaticSpawn && sp5 != null && !sp5.useAutomaticSpawn,
                                  "★ 另两条也是 `useAutomaticSpawn=false` —— 原版这 8 条**由 controller 驱动**，"
                                + "**不能**自己起循环（把 0 抄成 1 会让它们多出一份独立发射）");
                            Check(sp3 != null && sp3.particleSystemPrefab != null,
                                  "★ **外部模板解出来了**：`particleSystemPrefab` 指的是**同一 bundle 里另一个 "
                                + "prefab 根**（`Orbital 3 repeat`）⇒ 前两条路（层级路径 / 子树按叶子名）**必然落空**，"
                                + "靠效果库那条兜底（原版 10/24 条是这种）");
                            Check(sp3 != null && sp3.particleSystemPrefab != null
                               && !sp3.particleSystemPrefab.gameObject.activeSelf,
                                  "★ 模板按原版那句 `SetActive(false)` 关着（判据 = 反汇编 `RVA 0x675E10`："
                                + "「起循环之前、无条件」）—— 它**不该**在场上自己渲");

                            Check(ctl != null && ctl.startOnEnable,
                                  "★ controller 建出来了、`startOnEnable` 是**真**（原版 3/3 都是 1 ⇒ 自启）");
                            bool defsOk = ctl != null && ctl.particleSystemAreaSpawners != null
                                       && ctl.particleSystemAreaSpawners.Length == 3;
                            if (defsOk)
                                for (int i = 0; i < 3; i++)
                                {
                                    var d0 = ctl.particleSystemAreaSpawners[i];
                                    if (d0 == null || d0.particleSystemAreaSpawner == null
                                     || Mathf.Abs(d0.weight - 0.33f) > 1e-3f
                                     || Mathf.Abs(d0.chances - 0.3f) > 1e-3f) { defsOk = false; break; }
                                }
                            Check(defsOk, "★ controller 的 `particleSystemAreaSpawners[]` **这一层也收了**：3 条定义，"
                                        + "每条都指到**实例里那颗** spawner 组件、weight 0.33 / chances 0.3（原版实读值）"
                                        + $"（现在 {ctl?.particleSystemAreaSpawners?.Length ?? -1} 条）"
                                        + " —— 不收这层的话，那 8 条 `auto=0` 的 spawner 永远不出粒子（**静默**）");

                            if (sp3 != null)
                            {
                                // 池子的公开面只有 `CountInactive`（`IObjectPool<T>`），拿它判不出「新建了一颗」——
                                // 直接数 **spawner 的子件**：`CreatePooledItem` 是 `Instantiate(prefab, transform)`，
                                // 所以「多出一个子件」= 池子真的把模板实例化了。（`sp3` 自己那个模板副本原本就是 1 个子件。）
                                int was = sp3.transform.childCount;
                                sp3.SpawnParticle();
                                Check(sp3.transform.childCount == was + 1,
                                      $"★ `SpawnParticle()` 真的把模板 `Instantiate` 出来（子件 {was} → "
                                    + $"{sp3.transform.childCount}）—— `particleSystemPrefab` 为 null 时这一步会"
                                    + "**静默什么都不做**，所以这条盯的是池子那条链真的通");
                            }

                            // ② **同一个 GameObject 上两条 `ParticleSystemAreaSpawner`** —— 原版真有这种：
                            //   `EnvironmentalCondition Sororitas Shrine Bombardment/Psychic_Lightning_down` 上
                            //   一条被 `ScenarioParticleSpawnerBlender` 引用（**0.13 / 0.7**）、一条是 standalone
                            //   （**0.1 / 0.6**），两条共用一个 GameObject。
                            //   `MakeSpawner` 若写成 `GetComponent() ?? AddComponent()`，后建那条会把先建那条的
                            //   6 个字段**静默覆盖**（只在同 object 双组件时现形）—— 这条就是盯它。
                            //   ⚠️ 验它要**临时换一次环境再换回来**（这一程原本该停在 c0 上）；
                            //     换回来后必须把中间那两次淡出推完，否则下面那条 `FadingCount == 1` 会变成 2。
                            var sh = CardPresentation.EnvironmentConditions.Find(
                                "EnvironmentalCondition Sororitas Shrine Bombardment");
                            // 🔴 **A195**：原来这个门**没有 `else`** —— 那条环境 prefab 取不到（或 `Find` 不到）时，
                            //    下面那条 ★ 静默跳过、section 照样绿。前提断言 + `if`：条件不成立**当场红**。
                            //    （`ap0 != null` 那半另有前置断言，见本节上面 `Check(ap0 != null && …)`。）
                            bool shOk = sh != null && CardPresentation.EnvironmentConditions.HasPrefab(sh)
                                     && ap0 != null;
                            Check(shOk,
                                  "（前提）`Sororitas Shrine Bombardment` 找得到**且它的 prefab 取得到**"
                                + " —— 不成立时下面那条 ★（同 GO 两条 spawner 不互相覆盖）等于没验（A195）");
                            if (shOk)
                            {
                                ap0.Apply(sh, true);
                                var dup = ap0.CurrentInstance != null
                                    ? ap0.CurrentInstance.GetComponentsInChildren<ParticleSystemAreaSpawner>(true)
                                    : new ParticleSystemAreaSpawner[0];
                                bool was013 = false, was010 = false;
                                for (int i = 0; i < dup.Length; i++)
                                {
                                    if (dup[i] == null) continue;
                                    if (Mathf.Abs(dup[i].spawnRate - 0.13f) < 1e-3f
                                     && Mathf.Abs(dup[i].chances - 0.7f) < 1e-3f) was013 = true;
                                    if (Mathf.Abs(dup[i].spawnRate - 0.1f) < 1e-3f
                                     && Mathf.Abs(dup[i].chances - 0.6f) < 1e-3f) was010 = true;
                                }
                                var bl = ap0.CurrentInstance != null
                                    ? ap0.CurrentInstance.GetComponentInChildren<ScenarioParticleSpawnerBlender>(true)
                                    : null;
                                Check(dup.Length == 2 && dup[0].transform == dup[1].transform && was013 && was010
                                   && bl != null && bl.areaSpawners != null && bl.areaSpawners.Length == 1
                                   && bl.areaSpawners[0] != null
                                   && Mathf.Abs(bl.areaSpawners[0].spawnRate - 0.13f) < 1e-3f,
                                      "★ 同一个 GameObject 上那两条 **各是各的组件、字段没互相覆盖**"
                                    + $"（原版 `Sororitas Shrine Bombardment/Psychic_Lightning_down`：blender 那条 "
                                    + $"0.13/0.7、standalone 那条 0.1/0.6；现在建出 {dup.Length} 条，"
                                    + $"blender 那条的 rate={bl?.areaSpawners?[0]?.spawnRate}）"
                                    + " —— `MakeSpawner` 一旦复用已有组件，先建那条会被**静默覆盖**");
                                ap0.Apply(envIt, true);                       // 回到这一程原本那条（c0）
                                ap0.AdvanceBlendables(1000f);                 // 把中间那两次换场产生的淡出推完
                                Check(ap0.FadingCount == 0 && ap0.CurrentSO == c0.envSO
                                   && ap0.InstanceSpawnerCount == 3,
                                      $"★ 验完还原：回到 `{c0.envSO}`（现在 `{ap0.CurrentSO}`）· 淡出清空"
                                    + $"（FadingCount={ap0.FadingCount}）· 新实例上又是 3 个 spawner"
                                    + $"（{ap0.InstanceSpawnerCount}）");
                            }
                        }
                    }

                    // ⚠️ **A195 核过：这个门是安全的**（没改）—— 两个合取项各自**已有前置断言**：
                    //    `envIt` 见本节上面 `Check(envIt != null, "★ 环境数据在位…")`、`ap0` 见 `Check(ap0 != null && …)`。
                    if (envIt != null && ap0 != null)
                    {
                        // 行为断言：**本条 `defaultScenarioObjectsState` 决定战场自己的粒子开还是关**
                        //   （原版 = 战场 FX ↔ 环境 prefab FX 的交叉换场；判据同上）
                        int off = CountEmittersOff();
                        if (envIt.defaultScenarioObjectsState == 0)
                            Check(off > 0, $"★ 本条 state=0 ⇒ **战场自己那批粒子被关掉了**（实测 {off} 个 emission 关着）");
                        else
                            Check(off == 0, $"…本条 state=1 ⇒ 不关战场自己的粒子（实测关着 {off} 个，应为 0）");
                        // 换一条环境 ⇒ 旧实例**先进淡出等回调**、推够时间才销毁（原版 `DisableEnvironment` 的计数回调那套）
                        var c1 = CardPresentation.OffensiveCards.Choices("Ultramarines")[2];
                        RuleCore.ChooseOffensiveCard(ctx, 0, c1.idx, c1.envSO);
                        drv.SimulateApplyOffensiveEnv();
                        Check(ap0.FadingCount == 1,
                              $"★ 换环境时旧实例先进入淡出（FadingCount={ap0.FadingCount}）—— 不是立刻销毁");
                        ap0.AdvanceBlendables(600f);
                        Check(ap0.FadingCount == 0, $"★ 回调到齐后旧实例被销毁（FadingCount={ap0.FadingCount}）");
                    }

                    // 🆕 2026-09-30（§25）：**reveal 动画那四段**（原版 `_ApplyOffensiveAndDefensiveEffects` 的 state 0~3）
                    //   判据（逐行读过三段协程）→ `BattleDriver.BeginOffensiveRevealOrApply` 上面那段注释。
                    //   ⚠️ 上面那几条走的是 `SimulateApplyOffensiveEnv()`（**绕过 reveal 直接生效**）——
                    //      那正是「自检要当场精确状态」的那条路子；reveal 这条单独走一次。
                    {
                        RuleCore.ChooseOffensiveCard(ctx, 0, c0.idx, c0.envSO);   // 选一张真卡（非空）
                        var durs = BattleDriver.RevealDurationsForTest;
                        Check(durs.Length == 4 && Mathf.Abs(durs[0] - 2.0f) < 1e-4f
                           && Mathf.Abs(durs[3] - 1.0f) < 1e-4f
                           && Mathf.Abs(durs[1] - 0.5f) < 1e-4f
                           && Mathf.Abs(durs[2] - 3.0f) < 1e-4f,
                              $"★ reveal 四段 = **原版实读值**：等 2.0 s（`manager+0x488`）· "
                            + $"`DestroyCardOffensive` 的 t=1.0（`DOMove` **补间**时长）· 外层只等 t×0.5=0.5 · "
                            + $"非空卡再等 3.0 s（`manager+0x4a0`）—— 现在是 {durs[0]}/{durs[3]}/{durs[1]}/{durs[2]}");
                        Check(drv.OffensiveIsNonEmpty, "（前提）这一局选的是**真卡**（不是「不使用进攻卡」）");
                        drv.BeginOffensiveRevealForTest();
                        Check(drv.RevealStageForTest == 0 && drv.RevealCardForTest != null,
                              "★ reveal 起手 ⇒ 第 0 段、**揭示卡建出来了**（原版 `DisplayRevealedCard`；"
                            + "起始缩放 = 原版 `localScale = 场上值 × 0.01`）");
                        // 第 0 段：等满 2.0 s
                        drv.AdvanceOffensiveRevealForTest(1.9f);
                        Check(drv.RevealStageForTest == 0, "…不到 2.0 s 还停在第 0 段");
                        drv.AdvanceOffensiveRevealForTest(0.2f);
                        Check(drv.RevealStageForTest == 1,
                              "★ 2.0 s 到 ⇒ 进第 1 段（**HUD 钮的显隐排在 2.0 s 之后**—— 原版如此，"
                            + "我们原来把它放在 `ApplyOffensiveEnvOnce` 里、比原版早）");
                        // 第 1 段：外层**只等 0.5 s**（那 1.0 是 DOMove 补间，不阻塞）
                        drv.AdvanceOffensiveRevealForTest(0.45f);
                        Check(drv.RevealStageForTest == 1, "…0.5 s 之前还在第 1 段（1.0 那条是补间、不是等待）");
                        drv.AdvanceOffensiveRevealForTest(0.1f);
                        Check(drv.RevealStageForTest == 2, "★ 0.5 s 到 ⇒ 进第 2 段");
                        Check(drv.EnvApplierForTest != null && drv.EnvApplierForTest.CurrentSO == c0.envSO,
                              $"…第 2 段确实把环境应用了（原版 state 2 的 `ApplyEnvEffect`；现在 "
                            + $"`{(drv.EnvApplierForTest == null ? "null" : drv.EnvApplierForTest.CurrentSO)}`）");
                        Check(drv.RevealCardForTest == null, "…那张揭示卡这时已经被收掉（原版 `DestroyCardOffensive`）");
                        // 第 2 段：非空卡要等满 3.0 s
                        drv.AdvanceOffensiveRevealForTest(2.9f);
                        Check(drv.RevealStageForTest == 2, "…第 2 段要等满 3.0 s（原版 `manager+0x4a0`，不是 4）");
                        drv.AdvanceOffensiveRevealForTest(0.2f);
                        Check(drv.RevealStageForTest == -1, "★ 3.0 s 到 ⇒ 结束（原版 state 3）");
                    }

                    // 还原（别把后面的用例带跑偏）：环境这一条是**真的改了全局状态**的。
                    // ⚠️ 顺序：**先**让执行器撤掉环境（它会写一遍全局量），**再**把全局量复位。
                    if (drv.EnvApplierForTest != null) drv.EnvApplierForTest.Apply(null, true);
                    RenderSettings.fog = fogWas; RenderSettings.fogDensity = denWas; RenderSettings.fogColor = fcWas;
                    RenderSettings.ambientLight = alWas; Shader.SetGlobalFloat("_AmbientColorBlend", blendWas);
                    // 还原（别把后面的用例带跑偏）
                    RuleCore.ChooseOffensiveCard(ctx, 0, slotWas, "");
                    ctx.OffensiveChosen = false;
                }

                // ④ 里程碑骷髅（原版 `LeftArea/PlayerInfo/Milestones`）
                Check(drv.SkullIconTex == "40k_battle_Win_Skull",
                      $"里程碑骷髅用的是 `40k_battle_Win_Skull`（现在 `{drv.SkullIconTex}`）");
                var sp = drv.SkullIconPos01;
                Check(Mathf.Abs(sp.x - 0.10073f) < 0.002f && Mathf.Abs(sp.y - 0.11426f) < 0.002f,
                      $"骷髅在 x01 {sp.x:F5} / y01 {sp.y:F5}"
                    + "（原版绝对中心 (193.4, 956.6) → 0.10073 / 0.11426；权威表那两行是错的，见留档）");
                Check(!string.IsNullOrEmpty(drv.SkullScoreText) && drv.SkullScoreText[0] == 'x',
                      $"里程碑分数是 `x N` 那种写法（现在 `{drv.SkullScoreText}`）");
                // 骷髅和名牌几乎重叠：同 z 会被名牌整个盖住（**第一版就是这么丢的** ——
                // 断言查不到「画没画出来」，但能钉住「它在名牌前面」这个必要条件）
                Check(drv.SkullIconZ < 0.3f,
                      $"骷髅的 z = {drv.SkullIconZ:F3} 比 HUD 图默认的 0.3 更近（同 z 就被名牌盖住了）");
                // ⑤ 名牌位置 —— 2026-09-13 更正：原来抄的是 `FrontCanvas/Alliance Panel` 底下那份
                //    **inactive** 实例的坐标（`EnemyInfo (157,108)` / `PlayerInfo (32,977)`），
                //    不是 `LeftArea` 下 HUD 那一份 → 我方偏右 44 px / 偏高 37 px、敌方偏右 ~168 px。
                //    顺带把「骷髅压住名字」也治好了（名牌下移 37 px 之后骷髅正好在文字上方）。
                var mp = drv.MyPlatePos01;
                var ep = drv.EnemyPlatePos01;
                Check(Mathf.Abs(mp.x - (-0.005938f)) < 0.002f && Mathf.Abs(mp.y - 0.060602f) < 0.002f,
                      $"我方名牌左缘中点 x01 {mp.x:F6} / y01 {mp.y:F6}（原版 −0.005938 / 0.060602 —— 贴屏幕左缘、出血 11 px）");
                Check(Mathf.Abs(ep.x - (-0.005833f)) < 0.002f && Mathf.Abs(ep.y - 0.927083f) < 0.002f,
                      $"敌方名牌左缘中点 x01 {ep.x:F6} / y01 {ep.y:F6}（原版 −0.005833 / 0.927083）");
                Check(drv.SkullBottomY01 - drv.MyPlateTextCenterY01 > 0.005f,
                      $"里程碑骷髅在名牌文字的**上方**、不压字（下沿 {drv.SkullBottomY01:F5} − 文字中心 "
                    + $"{drv.MyPlateTextCenterY01:F5} = {drv.SkullBottomY01 - drv.MyPlateTextCenterY01:F5} > 0.005）");
                // ⑥ 牌堆上的回合灯 —— 2026-09-13 更正：原来只按锚点矩形折算、**漏了 `anchoredPosition (7.9,65.8)`**，
                //    灯被摆到牌堆右下角（偏低 66 px / 偏左 8 px）。原版绝对中心 (1813.46, 985.2)。
                var lp = drv.MyDeckLightPos01;
                Check(Mathf.Abs(lp.x - 0.94451f) < 0.002f && Mathf.Abs(lp.y - 0.08778f) < 0.002f,
                      $"牌堆回合灯在 x01 {lp.x:F5} / y01 {lp.y:F5}（原版 0.94451 / 0.08778 —— 锚点矩形 + `anchoredPosition`）");
            }
        }

        // ---- 1c. 满编手牌长什么样（12 张，专门看一眼扇形）----
        {
            var hand = PlayerHand();
            var driverRef = Object.FindObjectOfType<BattleDriver>();
            var root = new GameObject("HandPreview");
            var preview = new List<CardView>();
            for (int i = 0; i < 12; i++)
                preview.Add(CardView.Create(root.transform, CardData.Placeholder(i), $"Preview_{i:00}"));

            // 先把真手牌藏起来 —— 不然预览的 12 张会叠在真实的那几张上，截图上就成 16 张了
            int handN = driverRef != null ? driverRef.HandCount : 0;
            for (int i = 0; i < handN; i++)
            {
                var v = driverRef.HandViewAt(i);
                if (v != null) v.gameObject.SetActive(false);
            }

            hand.Refresh(preview);
            Step(0.05f);
            Shot(cam, "01c_满手12张");

            for (int i = 0; i < handN; i++)
            {
                var v = driverRef.HandViewAt(i);
                if (v != null) v.gameObject.SetActive(true);
            }
            Object.DestroyImmediate(root);
            Debug.Log(P + "   [图] 满手 12 张已截图（临时视图已销毁）");
        }

        // ---- 2. 拖拽上场（走真实的鼠标路径，不是 SimulatePlay）----
        Debug.Log(P + "--- 拖拽上场 ---");
        {
            CardTween.Mode = DG.Tweening.UpdateType.Manual;      // 批处理下补间要手动推进

            // ⑥ 高亮那一下的**放大 + 补间** —— 原版 `ScaleFactor` 1.05 / `AnimTime` 0.1s（2026-09-19 接上）
            //    判据 = 三个时刻的缩放：起始 → 补间**途中**（必须介于两者之间，这才叫补间）→ 到位（=基础×1.05）。
            //    ⚠️ 必须验「途中」那一档：只验首尾的话，「瞬变到 1.05」也能过。
            //    ⚠️ 位置有讲究：**必须放在 `CardTween.Mode = Manual` 之后** —— 在这之前补间不推进，
            //       量出来纹丝不动（第一版就摆错了地方，三条断言全红而代码是对的）。
            {
                var hv = driver.HandViewAt(0);
                Check(hv != null, "手牌第 1 张拿得到视图（验高亮缩放用）");
                if (hv != null)
                {
                    float s0 = hv.transform.localScale.x;
                    hv.SetHighlight(CardHighlightState.Hover);
                    Step(0.02f);                                   // 补间刚起步
                    float sMid = hv.transform.localScale.x;
                    Step(CardHighlight.AnimTime + 0.05f);           // 走完
                    float s1 = hv.transform.localScale.x;
                    Check(sMid > s0 + 1e-4f && sMid < s0 * CardHighlight.ScaleFactor - 1e-4f,
                          $"高亮放大**是补间**（t=0.02s 时 {sMid:F4}，介于 {s0:F4} 与 {s0 * CardHighlight.ScaleFactor:F4} 之间）");
                    Check(Mathf.Abs(s1 - s0 * CardHighlight.ScaleFactor) < 1e-3f,
                          $"高亮到位 = 基础缩放 × {CardHighlight.ScaleFactor}（{s1:F4}）");
                    hv.SetHighlight(CardHighlightState.Normal);
                    Step(CardHighlight.AnimTime + 0.05f);
                    Check(Mathf.Abs(hv.transform.localScale.x - s0) < 1e-3f,
                          $"取消高亮回到 {s0:F4}（`localScale` 与布局不打架）");
                }
            }

            // ⑦ 原版那圈**高亮描边**（SDF 层的 `_Outline`）：打得出去才有、打不出去 alpha 0、补间 0.2s
            //    判据 = `CardHighlight.OutlineOf`（**只此一处**）；这里只验「上屏跟不跟得上判据」。
            {
                // ⚠️ **分小步推**（一帧一步，跟真实播放一致）：一次推 0.25s 的话，那条补间是在这一推的
                //    **过程中**才被建出来的，整推都轮不到它 —— 量出来 elapsed 恒 0.000（踩过）。
                for (int k = 0; k < 24; k++) Step(1f / 60f);         // ≈0.4s，够 0.2s 的补间走完
                int badOutline = 0, on = 0, off = 0;
                var det = new System.Text.StringBuilder();
                for (int i = 0; i < driver.HandCount; i++)
                {
                    var hv2 = driver.HandViewAt(i);
                    if (hv2 == null) continue;
                    // 我们这边「打不出去」= `Unplayable`（置灰）；其余状态都算「能打」⇒ 该有描边
                    bool wantOn = hv2.State != CardHighlightState.Unplayable;
                    bool isOn = hv2.OutlineColor.a > 0.5f;
                    det.Append($"[{i}]{hv2.State}/{(isOn ? "亮" : "灭")}({hv2.OutlineDebug}) ");
                    if (wantOn == isOn) { if (wantOn) on++; else off++; }
                    else badOutline++;
                }
                Check(badOutline == 0 && on > 0,
                      $"★ 手牌那圈**描边**跟着「打不打得出去」走（该亮 {on} 张 / 该灭 {off} 张，不符 {badOutline} 张）"
                      + $" ｜ {det}");
            }

            int dragIdx = -1;
            for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
                if (ctx.Players[0].Hand[i].Card.Cost <= ctx.Players[0].Energy) { dragIdx = i; break; }

            if (dragIdx >= 0)
            {
                var view = driver.HandViewAt(dragIdx);
                int freeSlot = SimpleAI.FirstFreeSlot(ctx.Players[0]);
                // 🔴 2026-09-20：拖拽的**落点**用 `DropTargetWorld` —— 真 3D 时卡画在透视层，
                //    屏幕位置与老的 `SlotPosition` 差 ≈150 px（拖动测试必须照着**看得见的那个位置**拖）。
                var slotPos = pBoard.DropTargetWorld(freeSlot);
                int handBefore = ctx.Players[0].Hand.Count;
                int energyBefore = ctx.Players[0].Energy;
                string cardName = ctx.Players[0].Hand[dragIdx].Card.Name;

                it.SimulateHover(view.transform.position);
                Step(0.05f);
                it.SimulatePress(view.transform.position);
                Step(0.2f);                                       // 拿起来

                // ---- 拖拽插槽：手牌边拖边让位（原版 GetClosestInHandSlot）----
                // 挑**最后一张**当探针（别挑到被拖的那张）：它排在空位右边，空位往右挪时它会左移一格
                int others = driver.HandCount - 1;
                int probeIdx = dragIdx >= others ? others - 1 : others;
                var probe = probeIdx >= 0 ? driver.HandViewAt(probeIdx) : null;
                float probeX0 = probe != null ? probe.transform.position.x : 0f;

                var farLeft = LayoutSpace.ToWorld(0.10f, HandBaselineY);
                for (int i = 0; i < 10; i++) { it.SimulateDrag(farLeft, 1f / 30f); Step(1f / 30f); }
                Check(it.InsertIndex == 0, $"拖到手牌最左 → 插槽 {it.InsertIndex}（应 0）");

                var farRight = LayoutSpace.ToWorld(0.90f, HandBaselineY);
                for (int i = 0; i < 10; i++) { it.SimulateDrag(farRight, 1f / 30f); Step(1f / 30f); }
                Check(it.InsertIndex == others, $"拖到手牌最右 → 插槽 {it.InsertIndex}（应 {others}）");
                if (probe != null)
                    Check(probe.transform.position.x < probeX0 - 0.1f,
                          $"空位右边的牌让位了（{probe.name} x {probeX0:F2} → {probe.transform.position.x:F2}）");

                for (int i = 0; i < 24; i++) { it.SimulateDrag(slotPos, 1f / 30f); Step(1f / 30f); }

                // 🆕 2026-10-01（§〇 第 14 条 (b)）：**落点指示只点一格**（原版
                //   `MinionManager.ReassembleMinionsWhilePlayingUnit` 里那句 `SetCardShadow(GetLeft/RightSlotPos(落点))`；
                //   判据 → `资料/待办判据_战场与战斗视图.md` §8b）。**换掉了原来那个「九格一起亮」的 `SetDragHighlight`。**
                //   ⚠️ 这条**必须趁松手之前量** —— 松手时（`Release`）就全灭了。
                bool dragIsUnit = ctx.Players[0].Hand[dragIdx].Card.IsUnit;
                if (dragIsUnit)
                    Check(pBoard.LitSlotCount == 1 && pBoard.LitSlot == freeSlot,
                          $"★ 拖到棋盘上时**只有落点那一格亮着**（亮 {pBoard.LitSlotCount} 格、第 {pBoard.LitSlot} 格；"
                        + $"它会落在第 {freeSlot} 格）—— 不再是「九格全亮」");
                else
                    Check(pBoard.LitSlotCount == 0,
                          $"战术卡不落格位 ⇒ 一格都不点（原版那条链叫 `…WhilePlayingUnit`，只管单位牌；现在亮 {pBoard.LitSlotCount} 格）");
                Check(eBoard == null || eBoard.LitSlotCount == 0, "…对面那块棋盘一格都不亮");
                // 🔴 **只验「材质亮着」不够，还要验「看得见」** —— 2026-10-01 实拍 + 像素探针抓到过：
                //    指示器**被拖拽中的卡整块盖住**（卡周围 380×340 里只剩 706 个绿像素），
                //    而三条状态断言**全绿**。现在钉**三件**：位置（像素对得上被拖那张卡）· 画序（队列）· 层。
                //    ⚠️ 位置那三套相机换算**都不通**（逐张实拍看的，别再试）⇒ 改成**直接摆到指针**上，
                //      「它落在哪一格」由 `slot` 说了算（见 `BoardLayout.SetDropSlot` 那条长注释）。
                {
                    // 🔴 **「看得见」的判据（2026-10-01 打了七轮，四层原因见 `资料/已知的坑.md`）**：
                    //   结论 = **它得挂在被拖那张卡的底下** —— 卡自己的子物体必然跟着卡画
                    //   （实拍里那张卡的 blob 阴影就在它底下 ✓）。四套「屏幕↔世界」换算全错，只有这条稳。
                    var mr = pBoard.SlotMarkerRenderer(freeSlot);
                    Check(mr != null && mr.transform.parent == view.transform,
                          $"★ 落点指示**挂在被拖那张卡底下**（父 = "
                        + $"{(mr != null && mr.transform.parent != null ? mr.transform.parent.name : "<无>")}）");
                    Check(mr != null && mr.gameObject.layer == view.gameObject.layer,
                          $"★ …而且是**同一层**（{(mr != null ? mr.gameObject.layer : -1)} / {view.gameObject.layer}）");
                    Check(mr != null && mr.transform.localScale.x > 0f
                                    && Mathf.Abs(mr.transform.localScale.y / mr.transform.localScale.x
                                                 - CardView.Height / CardView.Width) < 0.01f,
                          "★ …长宽比照**卡本体**（`CardView.Width : Height` = 2.0927 : 3.3313）");
                    int mq = pBoard.SlotMarkerQueue(freeSlot);
                    Check(mq > 3000,
                          $"★ 落点指示的**渲染队列 {mq} > 卡的 3000**（原版材质的 `m_CustomRenderQueue`）"
                        + " —— 同队列时它会被拖拽中的卡整块盖住（实拍抓过）");
                    if (mr != null)
                        Debug.Log(P + $"   [落点指示] 父={mr.transform.parent?.name} · 层={mr.gameObject.layer}"
                                    + $" · localPos={mr.transform.localPosition} · localScale={mr.transform.localScale}"
                                    + $" · isVisible={mr.isVisible} · 队列={mr.sharedMaterial.renderQueue}");
                }
                Shot(cam, "2c_落点指示_拖拽中");     // 实拍证据：改版面必看截图（这一帧就是「一格亮着」的样子）

                it.SimulateRelease(slotPos);
                Check(pBoard.LitSlotCount == 0,
                      $"★ 松手后落点指示**全灭**（现在还剩 {pBoard.LitSlotCount} 格亮着）");
                // 落位动画走完才会触发 `OnDeployed`（引擎调用在回调里）——
                // 推进到「引擎里真有这个单位」为止，别写死一个时长（踩过：0.8s 不够，断言全挂）
                int landSteps = 0;
                int atSlotStep = -1;
                Vector2 arriveXy = new Vector2(float.NaN, float.NaN);   // A337：到达那一帧卡在哪儿（灭自证用）
                // 🔴🔴 **2026-10-11（A304 + A337 · 这是本条自检里唯一一条「静默量错」）**：
                //    **A304 记的是「靶取错了」**：原来这句比的是 `Vector3.Distance(view.transform.position, slotPos)`，
                //    两处都错：① `slotPos` 是**拖拽用的那个点**（`DropTargetWorld`），而落位补间飞向的是
                //    `which.SlotPosition(land)`（`Hand/CardInteraction.cs:442`）⇒ 两者在 3D 下差
                //    **0.73 世界单位**（≈79 px：日志 `Release：指针世界 (…)` 实测 `DropTargetWorld(0) =
                //    (−5.84, −0.89)` vs `SlotPosition(0) = (−5.53, −1.56)`）；
                //    ② **跨了两个空间** —— 3D 下 `DropTargetWorld` 走 `LayoutSpace.ScreenToWorld`，
                //    那个函数**末尾强制 `w.z = 0f`**（`Core/LayoutSpace.cs:86`），而卡视图摆在
                //    `ArenaSlots.RootPosition(…)` 上（`z = PlayerZ = −6.655`，`Board/ArenaSlots.cs:58`）
                //    ⇒ 距离恒 ≫ 0.02。
                //    ⇒ `atSlotStep` 从 3D 落地那天起一次都没生效过，而它**不报错**、只打一个负数：
                //    四份实测日志（`_tmp_view/{x,y,z,z2}_BattleScene*.log`，2026-09-29）全打 **`-0.03s`**
                //    （= 哨兵 −1 ÷ 30）。2D 时代（2026-09-16 之前）它打的是 **0.27s** —— 那是真的。
                //    **A304 当时只把靶对齐到「补间的终点」**（那是 `SlotPosition`）—— 对齐的是**错的那一半**：
                //    真缺陷在**终点本身**。**A337（同一天晚些）把终点改正了**
                //    （`CardInteraction.cs:442` = `which.DropTargetWorld(land)`，理由与改坏法见那里）⇒
                //    本检测器跟着**换成同一个口**（铁律 6：判据只此一处，⛔ 别在这里再算一次投影）。
                //    ✅ 仍然**只比 x/y**（照抄本文件 `:6880` / `:6955` 已有的正确写法：z 是层次 ——
                //    补间中段那句 `lift = tr.position + (0, 0.35, −0.2)` 会把 z 拉到 −0.8，末段才收回来）。
                //    ⚠️ `land == freeSlot` 由本用例的前提保证（这一步棋盘上还没有单位 ⇒
                //    `LandingSlot` 不推人）；真跑出岔子时下面那条新断言会**当场红**（不静默）。
                var arrivePos = pBoard.DropTargetWorld(freeSlot);
                // 🔴 **A337：两个候选靶在这里【必须真的分得开】，否则下面那条「到达」断言是空断言**
                //    （弱断言 —— 「有人把终点改回 `SlotPosition`」它也照样绿）。实测差 **0.47~0.74 世界单位**
                //    （督军位 51 px / 最外格 72 px，两个数都是真的：透视下投影点的 y **逐格不同**，
                //    见 `资料/普查产出_1011/WB3_A337.md` §二·3/§五·1）⇒ 阈值只取**检测容差本身**（0.02），
                //    ⛔ 不另编一个魔数（也别拿某个固定 px 当靶）。
                var linePos = pBoard.SlotPosition(freeSlot);
                float sep3D = new Vector2(arrivePos.x - linePos.x, arrivePos.y - linePos.y).magnitude;
                if (pBoard.use3D && pBoard.boardCam != null)
                    Check(sep3D > 0.02f,
                          $"★ 3D 下「投影点」与「2D 行线」**确实不是同一处**（差 {sep3D:F3} 世界单位，实测 0.47~0.74）"
                        + " —— 没有它，下面那条「到达」断言对「终点改回 `SlotPosition`」**无感**");
                else
                    Check(sep3D == 0f,
                          $"★ 非 3D 档 `DropTargetWorld` **逐位等于** `SlotPosition`（差 {sep3D:F6}）——"
                        + " 2D 那条路一个像素都不许动（`BoardLayout.cs:416` 的兜底就是 `return SlotPosition(slot)`）");
                // 🔴 **采样步长从 1/30 收到 1/60**（`landSteps` 的换算跟着改成 /60f，`landSec` 语义**不变**）：
                //    落位补间末段是 `OutCubic`（`DeploySequence.cs:62`）—— 它**在到达前一帧就已经收敛到
                //    0.02 以内**，而 30Hz 采样下「收敛的那一刻」**贴着补间完成的那一帧**，晚一步就会看见
                //    已经交接完的位姿（`SyncBoard` 在补间 `OnComplete` 里把卡摆到 `ArenaSlots.RootPosition`，
                //    见 `BattleDriver.cs` 的 `OnCardDeployed`）⇒ 检测器会**时有时无**。
                //    收到 1/60 之后，到达前至少还有一整帧落在「`DropTargetWorld` 附近、还没交接」的窗口里
                //    （末帧进度 ≈ 99.9% ⇒ 距离 ≈ 0.001 世界单位，阈值 0.02 有 20 倍余量）。
                //    ⚠️ 只影响本循环的采样密度；`landSec` = 引擎那一格填上的时刻，与步长无关（都是 0.30s）。
                for (int i = 0; i < 180 && ctx.Players[0].Board[freeSlot] == null; i++)
                {
                    Step(1f / 60f);
                    landSteps++;
                    var pNow = view.transform.position;      // ⚠️ `view` 是 `CardView`（MonoBehaviour）⇒ 走 `.transform`
                    if (atSlotStep < 0
                        && new Vector2(pNow.x - arrivePos.x, pNow.y - arrivePos.y).magnitude < 0.02f)
                    {                                          // 卡**到格位**的那一帧（对比「引擎里那一格填上」的那一帧）
                        atSlotStep = landSteps;
                        arriveXy = new Vector2(pNow.x, pNow.y);   // A337：那一刻的坐标，给下面那条「不在 2D 行线上」用
                    }
                }
                float landSec = landSteps / 60f;
                Step(0.2f);
                // 🔴 **这条断言就是 A304 的那颗牙**（上面那条读数一直打 −0.03s 而没人发现，因为它不报红）：
                //    🧨 **改坏法（A337 之后【反过来】了 —— 以前「换成 `SlotPosition`」才是改坏法）**：
                //    把 `arrivePos` 换成 `pBoard.SlotPosition(freeSlot)`（= 与 `CardInteraction.cs:442`
                //    的终点**不一致**）⇒ 靶差 0.73 世界单位 ⇒ 卡一辈子到不了 ⇒ `atSlotStep` 永远停在
                //    哨兵 −1 ⇒ **这里当场红**；
                //    把**终点与这里【一起】**改回 `SlotPosition` ⇒ 这条会绿 —— 那是上面那条 `sep3D` 与
                //    下面那条「不在 2D 行线上」的活（这一条只管「检测器真的触发过」，管不了「靶是哪一个」）；
                //    把 `new Vector2(…).magnitude` 换回 `Vector3.Distance` ⇒ 3D 下 z 差 6.655 ⇒ 同样红；
                //    把步长退回 `1f / 30f`（而 `landSec` 仍除 60）⇒ 采样跨过收敛窗口 ⇒ 这条也可能红。
                //    （⛔ 别把它改成「比 `landSteps`」—— 那会把读数钉死成同义反复，见 `已知的坑.md`。
                //      这条只断「检测器**真的触发过**」，具体的帧号留给下面那条日志。）
                Check(atSlotStep >= 0,
                      $"★ 卡到格位那一刻**真的被检测到**（`atSlotStep` = {atSlotStep}，不是哨兵 −1）"
                    + " —— ⚠️ 它原来比的是**拖拽用的那个点**（`DropTargetWorld`）、而落位补间当时飞向的是"
                    + " `SlotPosition`（**A337 已把终点改成同一个口**：`arrivePos` 与上面 `slotPos` 现在是"
                    + " 同一个函数、同一组实参）且**跨了 z 层次** ⇒ 从 3D 落地起一次都没生效过"
                    + "（日志里那串 `-0.03s` 就是这个）");
                // 🔴 **A337 的灭自证那条**：`atSlotStep >= 0` 只证明「卡到达了**某个**点」，不证明**是哪一个**。
                //    这里钉「到达的那一帧**不在 2D 行线上**」—— 它才是「把终点与检测器**一起**改回
                //    `SlotPosition`」那种回归的挡板（那一档上面那条哨兵检查**照样绿**）。
                if (atSlotStep >= 0 && pBoard.use3D && pBoard.boardCam != null)
                {
                    float dLine = new Vector2(arriveXy.x - linePos.x, arriveXy.y - linePos.y).magnitude;
                    Check(dLine > 0.02f,
                          $"★ 到达那一帧**确实不在 2D 行线上**（离 `SlotPosition` {dLine:F3} 世界单位，"
                        + "实测 0.47~0.74）—— 🧨 改坏法：终点与检测器**一起**改回 `SlotPosition` ⇒ 这里当场红");
                }
                Debug.Log(P + $"   落位：卡到格位用了 {atSlotStep / 60f:F2}s，引擎那一格填上用了 {landSec:F2}s"
                            + $"（60Hz 采样 · 原版 `minionToConversionPointTime` = {DeploySequence.MoveTime:F2}s，"
                            + "差值 = DOTween 起步那一帧 + Step 粒度）");

                Check(ctx.Players[0].Board[freeSlot] != null,
                      $"拖到槽 {freeSlot} 后引擎里那一格有单位了（{ctx.Players[0].Board[freeSlot]?.Name}）");
                // 落位时长要**跟着原版字段走**：`MinionManager.minionToConversionPointTime = 0.3`
                // ⚠️ 2026-09-13 之前这里是 **0.92s** —— 那是把 `Card Hand To Board`（「2D 卡→3D 身体」的
                //    交接动画）的长度当成了「手牌飞到场位」的时长，认错了来源。
                Check(landSec <= DeploySequence.MoveTime + 0.25f,
                      $"落位 {landSec:F2}s 完成（原版 `minionToConversionPointTime` = {DeploySequence.MoveTime:F2}s；"
                      + $"卡实际在第 {atSlotStep} 帧（60Hz）到位，余量是 DOTween 起步帧 + Step 粒度）");
                Check(ctx.Players[0].Board[freeSlot] != null && ctx.Players[0].Board[freeSlot].Name == cardName,
                      $"上去的正是拖的那张「{cardName}」");
                Check(ctx.Players[0].Hand.Count == handBefore - 1,
                      $"手牌 {handBefore} → {ctx.Players[0].Hand.Count}");
                Check(ctx.Players[0].Energy < energyBefore,
                      $"能量 {energyBefore} → {ctx.Players[0].Energy}");
                Check(driver.HandCount == ctx.Players[0].Hand.Count,
                      $"画面手牌 {driver.HandCount} == 引擎手牌 {ctx.Players[0].Hand.Count}（旧视图销毁了）");

                // 本回合已出牌数：**真打出一张之后第一枚灯要亮**（原版 `CardsPlayedInTurn1..3`）
                Check(driver.CardsPlayedThisTurn >= 1,
                      $"打出这张牌后本回合已出牌数 = {driver.CardsPlayedThisTurn}");
                Check(driver.PlayedPipsOn == Mathf.Min(3, driver.CardsPlayedThisTurn),
                      $"已出牌数的灯亮了 {driver.PlayedPipsOn} 枚（应 = min(3, {driver.CardsPlayedThisTurn})）");
            }
            else Debug.Log(P + "   （没有付得起的牌，跳过拖拽用例）");
        }
        Step(0.3f);
        Shot(cam, "02a_拖拽上场");

        // ---- 2b. 墓地/战斗日志（原版 `CemeteryLogPanel` + `ShowCemeteryBtn`，2026-09-13）----
        // 起因：用户点名要 ① 墓地/战斗日志。引擎那边**留档**（`Ctx.ActionLog`）+ 面板这一层。
        // ⚠️ 这几条的毛病截图也看不出来：面板打不开、行是空的、日志里没有刚发生的事。
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            // 🔴 **A195**：原来是 `if (drv != null)`、**没有 `else`** ⇒ 找不到 driver 时下面整节（含 ★ 判据）静默跳过、
            //    section 照样绿。形状照同族那几处（§15 ⑧ / §15b / §15c / §17 早就是这写法）：条件不成立**当场红**。
            if (drv == null) Check(false, "找不到 BattleDriver");
            else
            {
                Check(drv.CemeteryBtnTex == "40k_UI_bt_battlelog",
                      $"敌方名牌上的「看日志」按钮用的是原版那张图（现在 `{drv.CemeteryBtnTex}`）");
                var log = drv.BattleLog;
                Check(log != null && !log.Visible, "日志面板**默认是关着的**（原版要点了才划出来）");

                drv.ShowBattleLog();
                Check(log != null && log.Visible && log.RootActive, "点开之后面板真的显示出来了");
                Check(log != null && log.ShadeActive, "压暗层也跟着出来了");
                Check(log != null && log.RowsInFrontOfBg,
                      "日志的行画在底板**前面**（z 顺序反了的话底板会把行全盖住 —— 第一版就是这样）");
                // 🔴 **2026-09-27（PA 普查 §三 第 2 条）：四条边框原来【全取错了件】。**
                //   判据 = 原版 `CemeteryLogPanel/Frame/{Left,Right,Top,Bottom}` 的框（`menu_dump` 实读 `battlearena1`）：
                //     Left 97.65×**571.24** · Right 63.05×**578.65** · Top 707.30×65.06 · Bottom 707.30×46.79
                //   原来：**竖条**取「面板锚高 654.5」（多 14%）、**横条**取「面板全宽 794.1」（多 12%）。
                //   🆕 **2026-10-07（A113）：把「框」与「实绘」分开量。** 四条原版都是
                //   `Simple + m_PreserveAspect = 1` ⇒ 实绘 = **等比内接进框**（不是拉伸、也不一定等于框）：
                //     Top **宽受限** ⇒ 707.302×**64.995**（比框高 65.056 略矮）· 其余三条**高受限**
                //     ⇒ 高 = 框高、宽 = 框高 × 贴图比例。
                //   ⚠️ 上一版这两条盯的是「≈707.9 / ≈706.6」—— 那是**我们自己按框高算出来的值**，
                //   等于拿我们的常量证明我们的常量（而且正好放过了 Top 宽出框 0.69 px 那一条）。
                //   现在期望值全部是**原版实绘**。完整字段链 → `BattleLogPanel.cs` 常量区那段注释。
                float fTw = log.FrameWorldW("40k_battlelog_frame_TOP") * 108f;
                float fTh = log.FrameWorldH("40k_battlelog_frame_TOP") * 108f;
                float fBw = log.FrameWorldW("40k_battlelog_frame_Bottom") * 108f;
                float fBh = log.FrameWorldH("40k_battlelog_frame_Bottom") * 108f;
                float fLw = log.FrameWorldW("40k_battlelog_frame_Left") * 108f;
                float fLh = log.FrameWorldH("40k_battlelog_frame_Left") * 108f;
                float fRw = log.FrameWorldW("40k_battlelog_frame_Right") * 108f;
                float fRh = log.FrameWorldH("40k_battlelog_frame_Right") * 108f;
                Check(Mathf.Abs(fTw - 707.302f) < 0.5f && Mathf.Abs(fTh - 64.995f) < 0.5f,
                      $"★ 上边框实绘 **707.302×64.995**（原版框 707.302×65.056，**宽受限**内接 ⇒ 高略矮；"
                    + $"不是面板全宽 794.1）—— 实测 {fTw:F2}×{fTh:F2}");
                Check(Mathf.Abs(fBw - 706.700f) < 0.5f && Mathf.Abs(fBh - 46.795f) < 0.5f,
                      $"★ 下边框实绘 **706.700×46.795**（高受限）—— 实测 {fBw:F2}×{fBh:F2}");
                Check(Mathf.Abs(fLw - 93.147f) < 0.5f && Mathf.Abs(fLh - 571.236f) < 0.5f,
                      $"★ 左边框实绘 **93.147×571.236**（高受限；高 = 框高，**不是面板锚高 654.5**）"
                    + $"—— 实测 {fLw:F2}×{fLh:F2}");
                Check(Mathf.Abs(fRw - 62.814f) < 0.5f && Mathf.Abs(fRh - 578.654f) < 0.5f,
                      $"★ 右边框实绘 **62.814×578.654**（高受限；**与左边框不同长**）—— 实测 {fRw:F2}×{fRh:F2}");
                Check(log != null && log.FilledRows > 0, $"日志里有内容（{log.FilledRows} 行有字）");
                // 🆕 2026-09-28：行几何照原版订正（原来是我们按「八行」自己配的 53.92）
                Check(Mathf.Abs(BattleLogPanel.RowHeightPx - 43.134f) < 0.01f,
                      $"★ 日志行高 = **43.134**（原版 `CemeteryActions` 748.006×431.344 ÷ **10** 行 ·"
                      + " 行底图 `40k_battlelog_display_neutral` 原生 653×43）");
                Check(BattleLogPanel.RowTotal == 10,
                      "★ 行数 = **10**（原版 `cemeteryActions` 数组长度硬编码、场景里就是 10 个 `CemeterySliderUI`）");

                // 🆕 2026-09-29：**行底板按「谁做的动作」换** —— 原版有三张同名变体
                //   （`40k_battlelog_display_{player,enemy,neutral}`，三张都是 653×43），
                //   我们原来**三张都画 neutral**。判据 = `BattleEvent.Player`。
                {
                    var names = new System.Collections.Generic.HashSet<string>();
                    string bgMine = null, bgFoe = null;
                    for (int i = 0; i < BattleLogPanel.RowTotal; i++)
                    {
                        var bg = log.RowBgName(i);
                        if (string.IsNullOrEmpty(bg)) continue;
                        names.Add(bg);
                        var t = log.RowText(i);
                        if (string.IsNullOrEmpty(t)) continue;
                        if (t.Contains("我方")) bgMine = bg;
                        if (t.Contains("敌方")) bgFoe = bg;
                    }
                    foreach (var n in names)
                        Check(n == "40k_battlelog_display_player" || n == "40k_battlelog_display_enemy"
                           || n == "40k_battlelog_display_neutral",
                              $"行底板是原版三张变体之一（实测 `{n}`）");
                    if (bgMine != null && bgFoe != null)
                        Check(bgMine != bgFoe,
                              $"★ 我方那行与敌方那行**底板不是同一张**（{bgMine} vs {bgFoe}）");
                    else
                        Debug.Log(P + $"   （日志里暂时只有单向的行：我方 `{bgMine}` / 敌方 `{bgFoe}` ——"
                                    + " 三张变体的**映射**另有断言盯着，见下一行）");
                    Check(BattleLogPanel.RowArt(BattleLogPanel.RowSide.Player) == "40k_battlelog_display_player",
                          "★ 我方 → `…_player`");
                    Check(BattleLogPanel.RowArt(BattleLogPanel.RowSide.Enemy) == "40k_battlelog_display_enemy",
                          "★ 敌方 → `…_enemy`");
                    Check(BattleLogPanel.RowArt(BattleLogPanel.RowSide.Neutral) == "40k_battlelog_display_neutral",
                          "★ 无归属 → `…_neutral`");
                }
                var rb0 = log.RowBg(0);
                if (rb0 != null)
                    Check(Mathf.Abs(rb0.WorldW * EndPanel.PxPerUnit - 748f) < 2f,
                          $"★ 行底图**画满整行宽 748**（原版那张是 `Simple + preserveAspect=0` ⇒ 拉伸）"
                          + $"—— 实测 {rb0.WorldW * EndPanel.PxPerUnit:F1}");
                Check(log != null && log.RowText(0) != null && log.RowText(0).Contains("回合"),
                      $"最新一行带着回合号：`{log.RowText(0)}`");
                // 日志里必须**真的有刚刚那一步**（上一步是真拖了一张牌上场）
                bool sawPlay = false;
                for (int i = 0; i < 8 && !sawPlay; i++)
                {
                    var t = log.RowText(i);
                    if (!string.IsNullOrEmpty(t) && (t.Contains("打出") || t.Contains("进入格位"))) sawPlay = true;
                }
                Check(sawPlay, "日志里能找到刚打出的那张牌（不是一份空壳）");
                // 字**真的画出来了**没有 —— 只看 `RowText(0)` 有值是不够的（见 `RowTextWidth` 的注释）
                Check(log.RowTextWidth(0) > 0.01f,
                      $"最新那行的文字真有宽度（{log.RowTextWidth(0):F3} 世界单位 > 0.01）—— TMP 建出字形了");
                // 🆕 2026-10-01（§〇 第 15 条 (b)）：**面板是「划出来」的，不是切显隐**。
                //   判据（逐行读 `CemeteryManager__ShowCemeteryLogBtn.c`）：开 = `:17`、合 = `:51`，
                //   两条都是 `DOAnchorPosX(rt, initialX(−1200) / finalX(87), DAT_1834b2dc8)`；时长 0.3f
                //   是 `GameAssembly.dll` 浮点池实读；没有 `SetEase` ⇒ 默认 OutQuad。
                Check(Mathf.Abs(log.PanelLeftX - (-1200f)) < 1f,
                      $"★ 刚点开时面板**还收在屏幕外**（左缘 {log.PanelLeftX:F1} px —— 原版 `initialX = −1200`）"
                    + $"—— 是划进来的，不是切显隐（进度 {log.SlideT:F2}）");
                log.AdvanceSlideForTest(0.15f);          // 推半程（0.3s 的一半）
                float halfX = log.PanelLeftX;
                Check(halfX > -1199f && halfX < 86f,
                      $"★ 滑到半程时左缘落在两端之间（{halfX:F1} px）");
                log.FinishSlideForTest();                // = 原版那个 `TweenExtensions.Complete()`
                Check(Mathf.Abs(log.PanelLeftX - 87f) < 0.01f,
                      $"★ 滑完停在**原版 finalX = 87**（实测 {log.PanelLeftX:F2} px）");
                // 🆕 **2026-10-07（A113 追加）：面板拉开后，三件东西各自锚自己那条原版 rect** ——
                //   原来它们**全被摆在「面板正中」**，三处都偏。判据双份且一致：
                //     运行时 `…/panel_0914/runtime_ui_dump_drive.tsv:136/142/144`
                //     + 场景序列化 `RectTransform_2944.json`（BG）/ `_2935.json`（CemeteryActions）/ `_3429.json`（ActionText）。
                //     · `BG`            中心 面板局部 (365.98679, 11.696)  ⇒ 屏幕 **(452.99, 539.07)**
                //     · 行容器          中心 面板局部 (367.76898, 24.95)   ⇒ 第一行中心 **(454.77, 331.71)**
                //     · 行内文字左缘    面板局部  8.766（= 行内 15.0）     ⇒ 屏幕 **95.77**
                //   ⚠️ 这里原来量的是「行中心 x ≈ 484.05」= **我们摆错时的值**（面板中心 397.05 + 87），
                //      已按原版改成 454.77；「面板左缘 = 87」另有 `PanelLeftX` 一条盯着，不靠行来证。
                var row0Px = LayoutSpace.ToPixel(log.RowBg(0).transform.position);
                Check(Mathf.Abs(row0Px.x - 454.77f) < 1f && Mathf.Abs(row0Px.y - 331.71f) < 1f,
                      $"★ 第一行中心 = 原版 (454.77, 331.71)（= 面板左缘 87 + 行容器中心 367.769 − 行宽/2 + 行高/2）"
                    + $"—— 实测 ({row0Px.x:F2}, {row0Px.y:F2})（原来摆面板正中 ⇒ x 偏右 29.3 / y 偏上 10.5）");
                var bgPx = log.BgCenterPx;
                Check(Mathf.Abs(bgPx.x - 452.99f) < 1f && Mathf.Abs(bgPx.y - 539.07f) < 1f,
                      $"★ 底板 `BG` 中心 = 原版 (452.99, 539.07)（**不在面板中线上**：比面板中心偏左 31.05 / 偏上 11.70）"
                    + $"—— 实测 ({bgPx.x:F2}, {bgPx.y:F2})");
                var text0Px = LayoutSpace.ToPixel(log.RowAnchor(0));
                Check(Mathf.Abs(text0Px.x - 95.77f) < 1f,
                      $"★ 行内文字左缘 = 原版 x **95.77**（面板局部 8.766 · 行内 15.0 —— `ActionText` 的 `offsetMin.x`）"
                    + $"—— 实测 {text0Px.x:F2}（原来按「面板居中 + 行内 8.8」摆 ⇒ 偏右 23.1 px）");

                // 🆕 2026-10-07（A113）：**四条边框的【位置】也逐条量**（上一版只量了尺寸 ——
                //   于是「两条竖框整整偏左 87 px、两条横框偏右 16.6 px」一直没人发现，截图也看不出）。
                //   判据 = prefab 字段链算出的**屏幕 px**（`PanelOpenX 87` + 面板局部 px）：
                //     Top (467.42, 278.67) · Bottom (467.43, 762.82)
                //     Left (68.60, 542.20)「在面板左缘**外** 18.4 px」· Right (850.75, 541.27)
                //   要点：**量的是 quad 的真实 transform**（`FrameCenterPx`），不是我们存下来的常量；
                //   且必须在**滑到位之后**问（面板在飞的时候它们跟着动）。完整链 → `BattleLogPanel.cs` 常量区。
                {
                    var fT = log.FrameCenterPx("40k_battlelog_frame_TOP");
                    var fB = log.FrameCenterPx("40k_battlelog_frame_Bottom");
                    var fL = log.FrameCenterPx("40k_battlelog_frame_Left");
                    var fR = log.FrameCenterPx("40k_battlelog_frame_Right");
                    Check(Mathf.Abs(fT.x - 467.42f) < 1f && Mathf.Abs(fT.y - 278.67f) < 1f,
                          $"★ 上边框中心 = 原版 (467.42, 278.67) —— 实测 ({fT.x:F2}, {fT.y:F2})");
                    Check(Mathf.Abs(fB.x - 467.43f) < 1f && Mathf.Abs(fB.y - 762.82f) < 1f,
                          $"★ 下边框中心 = 原版 (467.43, 762.82) —— 实测 ({fB.x:F2}, {fB.y:F2})");
                    Check(Mathf.Abs(fL.x - 68.60f) < 1f && Mathf.Abs(fL.y - 542.20f) < 1f,
                          $"★ 左边框中心 = 原版 (68.60, 542.20)（**在面板左缘外 18.4 px**）"
                        + $"—— 实测 ({fL.x:F2}, {fL.y:F2})");
                    Check(Mathf.Abs(fR.x - 850.75f) < 1f && Mathf.Abs(fR.y - 541.27f) < 1f,
                          $"★ 右边框中心 = 原版 (850.75, 541.27) —— 实测 ({fR.x:F2}, {fR.y:F2})");
                    // 横条的中心**不在面板中线上**（原版 380.43 vs 面板中心 397.03）—— 这正是
                    // 「`Frame` 容器左中锚点」那条字段链的指纹。单独钉一条（比的是**面板中心**这个几何量，
                    // **不用行中心** —— 行是另一件事、位置将来可能挪，别把两条账绑在一起）。
                    float panelCxPx = 87f + 794.069f * 0.5f;
                    Check(fT.x < panelCxPx - 10f,
                          $"★ 两条横边框的中心比面板中心**偏左 16.6 px**（原版 380.43 vs 397.03 —— 不是居中！）"
                        + $"—— 上框 {fT.x:F2} vs 面板中心 {panelCxPx:F2}");
                }

                // 🆕 2026-09-29：**行内卡名是链接**（原版包成 `<b><link="…"><u>名字</u></link></b>`）——
                //   悬停它要弹一张卡（`CemeteryManager.CheckCardLink`）。这两条是那件事的**全部前置**。
                int linkRow = -1;
                for (int i = 0; i < 8 && linkRow < 0; i++)
                    if (!string.IsNullOrEmpty(log.RowLinkKey(i))) linkRow = i;
                Check(linkRow >= 0, "日志行里挂着**卡名链接**（没有它就谈不上悬停弹卡）");
                if (linkRow >= 0)
                {
                    Check(!log.RowText(linkRow).Contains("<"),
                          $"…而 `RowText` 给的是**可见文字**（富文本标签已剥）：`{log.RowText(linkRow)}`");
                    Vector3 lp;
                    Check(log.FindLinkProbe(linkRow, cam, out lp),
                          $"…能扫到一个**压在链接字形上**的点（第 {linkRow} 行）—— 批处理没鼠标，得自己找");
                    Check(drv.SimulateLogHover(lp),
                          $"★ 悬停到卡名上 ⇒ **弹卡**（{drv.LogHoverCardKey}）");
                    Check(drv.LogHoverCardKey == log.RowLinkKey(linkRow),
                          "…弹的就是那一行链接指向的卡");
                    Step(0.2f); Shot(cam, "02b2_日志悬停弹卡");
                    Check(!drv.SimulateLogHover(LayoutSpace.FromPixel(1500f, 1000f)),
                          "指针移到别处 ⇒ **收卡**（不是一直挂着）");
                }
                Step(0.2f);
                Shot(cam, "02b_战斗日志");
                log.Hide();
                Check(!log.Visible, "关掉之后面板收起来了");
            }
        }

        // ---- 3. 费用约束：付不起的牌落不下去 ----
        Debug.Log(P + "--- 落点受费用约束 ---");
        int pricey = -1;
        for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
            if (ctx.Players[0].Hand[i].Card.Cost > ctx.Players[0].Energy) { pricey = i; break; }
        if (pricey >= 0)
        {
            int code = RuleCore.CanPlayCard(ctx, 0, pricey, 1);
            Check(code == RuleCodes.ErrCost,
                  $"付不起的「{ctx.Players[0].Hand[pricey].Card.Name}」({ctx.Players[0].Hand[pricey].Card.Cost} 费)"
                + $"在 {ctx.Players[0].Energy} 能时被拒绝（{RuleCodes.Describe(code)}）");
        }
        else Debug.Log(P + "   （这局起手都付得起，跳过费用拒绝用例）");

        // ---- 4. 出牌（走引擎直通车，验数值账）----
        Debug.Log(P + "--- 出牌 ---");
        int played = 0;
        int unitsBefore = driver.MyUnits.Count;
        for (int guard = 0; guard < 8; guard++)
        {
            int card, slot;
            if (!SimpleAI.NextPlay(ctx, out card, out slot)) break;   // 单位卡和战术卡都算
            int before = ctx.Players[0].Energy;
            if (driver.SimulatePlay(card, slot) != RuleCodes.OK) break;
            played++;
            Check(ctx.Players[0].Energy < before, $"打出后能量下降（{before} → {ctx.Players[0].Energy}）");
        }
        Check(driver.MyUnits.Count == unitsBefore + played,
              $"场上单位从 {unitsBefore} 涨到 {driver.MyUnits.Count}（出了 {played} 张）");
        // 前一步拖拽可能已经把能量花光了，所以「一张没出」也是正常结果 —— 关键是别剩下能出的牌
        Check(played > 0 || ctx.Players[0].Energy == 0,
              $"出牌阶段收尾：直通出了 {played} 张，能量剩 {ctx.Players[0].Energy}"
            + (played == 0 ? "（能量已被拖拽那步花光，符合预期）" : ""));
        Step(0.4f);
        Shot(cam, "02_我的回合出场");

        // ---- 5. 攻击 ----
        Debug.Log(P + "--- 攻击 ---");
        driver.SimulateEndTurn();                 // 交给对手
        // ---- 4.5 等待提示（原版 `WaitText`）----
        // 位置/尺寸照原版字段：锚 (0.5,1) + (7,−175.1)、**1344×79.4 px**、压暗 α **0.6118**
        //（出处 `runtime_ui_dump_drive_0912.tsv:350-355`）。⚠️ **触发时机与文案是我们挑的**
        //（原版查不到），理由见 `Battle/WaitBanner.cs` 文件头。
        driver.RefreshAll();                      // 批处理里没有 Update() 循环，HUD 得手动刷
        var wait = driver.Wait;
        Check(wait != null, "等待提示建起来了（原版 `WaitText`）");
        if (wait != null)
        {
            Check(wait.Visible, "★ 对手回合 → 等待提示**显示**");
            string want = CardText.Phrase("WAITING FOR OPPONENT");
            Check(wait.ShownText == want,
                  $"★ 条上写的字：「{wait.ShownText}」=「{want}」"
                + "（⚠️ 这一句是**我们加的**：原版 `Text` 节点是空的、本地化 key 没解出来）");
            // ⚠️ 尺寸断言的是 **`Generic Popup Background` 的 1323×90**（真正画出来的那个子节点），
            //    不是父容器 `WaitText` 的 1344×79.4 —— 2026-09-17 照场景 JSON 更正。
            Check(Mathf.Abs(wait.BarWorldW - 1323f / 108f) < 0.01f,
                  $"★ 提示条宽 = 原版 `Generic Popup Background` 1323 px ÷ 108 = {1323f / 108f:F3} 世界单位（实得 {wait.BarWorldW:F3}）");
            Check(Mathf.Abs(wait.BarWorldH - 90f / 108f) < 0.01f,
                  $"★ 提示条高 = 原版 90 px ÷ 108 = {90f / 108f:F3} 世界单位（实得 {wait.BarWorldH:F3}）");
            // ⚠️ **不能断言「= 9 块」**：原版这张 `40k_popup` 的上下边框各 **160 px**，而提示条只有 **90 px** 高
            //    ⇒ 中间那一行被挤没（顶/底边框按比例压扁）—— **这正是 Unity `Sliced` 的退化行为**，
            //    原版自己也是这么画的。所以判据是「角块都在（≥6）」。
            Check(wait.BarFramePieces >= 6,
                  $"★ 底板九宫格建了 {wait.BarFramePieces} 块（`40k_popup` 是 `Sliced`；提示条比它的上下边框还矮 ⇒ 6 是正常的退化）");
            // 🔴 **2026-10-04（A50④）把这条断死**：原来是 `>= 1`，而 `BarFillPieces` 当时数的是
            //    `_fillRoot.childCount`（= `MenuDraw.Tiled` 返回的那个根节点，**恒 1**）⇒ 那条断言**恒真**，
            //    改坏块数一条都不会红。现在两边都修了：读数数真块、断言断**原版那个数**。
            // 🔴 **2026-10-05 就地订正：块数 11 → 42。** 原来这里按**节距 128** 算（= 贴图 `m_Rect.width`），
            //    **漏了那颗 Image 的 `m_PixelsPerUnitMultiplier = 2.0`**。uGUI 的格宽 =
            //    `(m_Rect.width − 左右 border) ÷ (ppu ÷ refPPU × ppuMul)`（`Image.cs:756` `multipliedPixelsPerUnit`
            //    · `:1232` `tileWidth`；本地源码在 `Library/PackageCache/com.unity.ugui@27635d171b1a/`），
            //    参考分辨率下 `ppu ÷ refPPU = 100 ÷ 100 = 1` ⇒ **128 ÷ 2 = 64 画布 px**。
            //    ⇒ 1323 ÷ 64 = 20.67 → **21 列**；90 ÷ 64 = 1.41 → **2 行** ⇒ **21 × 2 = 42**。
            //    （本工程另有 5 处同一张图的先例都传 64：`PromptPopup.FillTilePx` · `ImportDeckPopup.FillTilePx` ·
            //      `MissionRerollPopup` · `ProfileTab` · `DuelPopupWindow`；`WaitBanner` 是全工程唯一的例外。）
            //    ⚠️ 42 是**算出来的**，不是量的：`ImageQuad.CreateTiled` 按 `PixelsPerUnit = 108` 折世界尺寸
            //    （与本件 `U()` 同一换算）⇒ 12.25 ÷ 0.5926 = 20.67 → 21 列，和上面那条整除式同源。
            Check(wait.BarFillPieces == 42,
                  $"★ 填充层平铺了 **42** 块（原版 `Tiled`：节距 128÷ppuMul 2 = **64** ⇒ 1323÷64 → 21 列 × 90÷64 → 2 行；"
                + $"实得 {wait.BarFillPieces} —— ⚠️ 这条以前是 `>= 1`，等于没查）");
            Check(Mathf.Abs(wait.ShadeTint.a - 0.6118f) < 0.001f,
                  $"★ 整屏压暗的 α = 原版 0.6118（实得 {wait.ShadeTint.a:F4}）");
            Shot(cam, "02b_等待提示");
        }

        // ---- 4.5c **A276**：同一父节点下的三层只该有**一套 z 口径**（裸局部 z）----
        // 现场：`Battle/WaitBanner.cs` 的框那一层原来写的是 `Z − transform.position.z`
        //   （= 「把**世界** z 落到 Z」那套口径），而同父的压暗层（`Z+0.02`）/ 文字（`Z−0.01`）都是裸局部 z。
        // ⚖️ 调度台裁定 = **收口径**（选项 (a)；判据全文 → `资料/普查产出_1010/V4b_三件口径.md` §Q2）：
        //   改成裸局部 z ⇒ **不是「对齐原版」**（这三层原版的 z 关系**没有判据**，`Z` 本身是我们挑的）。
        // 判据（两态，且**不读被断实现的常量**）：**同一个件建两遍**，只把**父节点的世界 z** 挪 3.3 ——
        //   裸局部 z 那版**一个数都不变**；带 `− transform.position.z` 那版会整体偏 3.3 ⇒ 当场红。
        //   （今天父件世界 z ≡ 0 ⇒ 两种写法**逐位相同**，所以只有「挪父件」这个夹具分得开两态。）
        // 改坏法：把那行写回 `Z - transform.position.z` ⇒ 下面这条红（`zB` 会比 `zA` 小 3.3）。
        {
            var zr0 = new GameObject("A276Probe_wait_z0");
            var zr1 = new GameObject("A276Probe_wait_z33");
            zr1.transform.position = new Vector3(0f, 0f, 3.3f);
            var popTexP = CardArt.DeckUi("40k_popup");
            var filTexP = CardArt.Ui("40k_popup_texture");     // 两张都给（走**正常那一支**，本条不掺「图缺」那一档）
            var wA = WaitBanner.CreateWithArt(zr0.transform, popTexP, filTexP);
            var wB = WaitBanner.CreateWithArt(zr1.transform, popTexP, filTexP);
            float zA = wA != null ? wA.PopupLocalZ : float.NaN;
            float zB = wB != null ? wB.PopupLocalZ : float.NaN;
            if (float.IsNaN(zA) || float.IsNaN(zB))
                // ⚠️ **出声**跳过（不是静默门）：这一档 = `40k_popup` 取不到、框压根没建 ——
                //    上面 §4.5 的 `BarFramePieces == 42` 与 §4.5b 那两条会先红（同一条件在同一次 Run 里有别处会红）。
                Debug.Log(P + "   （A276：框那一层没建出来（`40k_popup` 取不到）⇒ 等待提示这条 z 口径不判；"
                            + "§4.5 那条 `BarFramePieces == 42` 会先红）");
            else
                Check(Mathf.Abs(zA - zB) < 1e-5f,
                      $"★ 等待提示那三层**同一套 z 口径**（裸局部 z，不随父件的世界 z 变）—— "
                    + $"父件世界 z = 0 时框 z = {zA:F4}；父件挪到 +3.3 时 = {zB:F4}");
            Object.DestroyImmediate(zr0);
            Object.DestroyImmediate(zr1);
        }

        // ---- 4.5b 美术取不到那一档：必须「出声 + 退化」，⛔ **不许抛** ----
        // 判据 = 「不许静默失败」那条红线的**反向**（那一条管「别不出声」，这一条管「别炸」）。
        // 这一档**原来会抛 `NullReferenceException`**：框这一层改用公共件 `MenuDraw.Nine` 之后，
        // `tex == null` ⇒ 它**返回 null**（`Shell/MenuDraw.cs:758`）；而旧写法 `ImageQuad.CreateNineSlice`
        // **从不返回 null**（`Battle/ImageQuad.cs:316`：只打警告、**仍返回 root**）⇒
        // 建填充那几行里的 `_popup.transform` 从「死码、永远安全」变成一条真 NRE 路径。
        // ⚠️ 期望值全是**外部事实**、不从被测实现读：图传 null ⇒ 该建的件**一块都不该有**、条子不该显示；
        //    **「出声」那一半 = 一条警告同时说明两层**（框没建成 ⇒ 填充一并跳过）。
        // ⛔ 把 `_fillRoot.transform.SetParent(_popup.transform, …)` 挪回 `if` 外面 ⇒ 下面第 1 条立刻红。
        {
            var noArtRoot = new GameObject("WaitBannerNoArtProbe");

            // ① **两张图都取不到**（= 「美术目录被删」那一档，本工程明确支持的退路）
            int warns = 0;
            string lastWarn = "";
            Application.LogCallback hWarn = (cond, st, type) =>
            {
                if (type == LogType.Warning && cond != null && cond.Contains("[WaitBanner]"))
                { warns++; lastWarn = cond; }
            };
            WaitBanner noArt = null;
            string threw = null;
            Application.logMessageReceived += hWarn;
            try { noArt = WaitBanner.CreateWithArt(noArtRoot.transform, null, null); }
            catch (System.Exception e) { threw = e.GetType().Name + "：" + e.Message; }
            Application.logMessageReceived -= hWarn;
            Check(threw == null,
                  $"★★ 两张图都取不到时**不许抛**（实测 {(threw == null ? "没抛" : "抛了 —— " + threw)}）");
            Check(noArt != null, "★ 那一档仍然返回一具 `WaitBanner`（不是半路中断）");
            if (noArt != null)
            {
                Check(noArt.BarFramePieces == 0, $"★ 框那层退化成空（实得 {noArt.BarFramePieces} 块）");
                Check(noArt.BarFillPieces == 0, $"★ 填充那层也跳过（实得 {noArt.BarFillPieces} 块）");
                Check(!noArt.Visible, "★ 整条提示不显示（`Visible` 假 —— 不是半截挂着一块压暗）");
            }
            Check(warns == 1, $"★ 而且**要出声**：一条 `[WaitBanner]` 警告**同时说明两层**（框没建成 ⇒ 填充一并跳过）（实测 {warns} 条"
                              + (lastWarn.Length > 0 ? "，末条：" + lastWarn : "") + "）");

            // ② **框在、填充图缺**：走的是另一支 —— 框照建（≥6 块，同上面那条 `Sliced` 退化说明）、
            //    填充出声跳过；这一支也必须不抛。
            int warns2 = 0;
            Application.LogCallback hWarn2 = (cond, st, type) =>
            { if (type == LogType.Warning && cond != null && cond.Contains("[WaitBanner]")) warns2++; };
            var popTex = CardArt.DeckUi("40k_popup");
            WaitBanner halfArt = null;
            string threw2 = null;
            Application.logMessageReceived += hWarn2;
            try { halfArt = WaitBanner.CreateWithArt(noArtRoot.transform, popTex, null); }
            catch (System.Exception e) { threw2 = e.GetType().Name + "：" + e.Message; }
            Application.logMessageReceived -= hWarn2;
            Check(threw2 == null, $"★ 只有填充图缺时**也不许抛**（实测 {(threw2 == null ? "没抛" : "抛了 —— " + threw2)}）");
            Check(halfArt != null && halfArt.BarFramePieces >= 6,
                  $"★ 框照建（≥6 块，实得 {(halfArt != null ? halfArt.BarFramePieces : -1)}）"
                + "（⚠️ 这几条要用 `40k_popup` 建框 ⇒ 那张图取不到时它们会和上面那条 `BarFramePieces` **同因红**）");
            Check(halfArt != null && halfArt.BarFillPieces == 0,
                  $"★ 填充退化成 **0** 块（实得 {(halfArt != null ? halfArt.BarFillPieces : -1)}）");
            Check(warns2 == 1, $"★ 这一支**只响一条**警告（实测 {warns2} 条）");

            Object.DestroyImmediate(noArtRoot);
        }

        // ---- 4.6 共用件：`Label.SetAutoFitBox` 的 min/max **口径**（A50①）----
        // 判据 = **原版 `Timer Text` 的字段原文**（商店 19 变体里 `TypeFs=34` 的那 9 份）：
        //   `m_fontSize = 30.6` · `m_fontSizeMin = 10` · `m_fontSizeMax = 32`
        //   出处 `资料/待办判据_审查发现_1004.md` §A34-F2；矩形/字号那一份表 → `Shell/OfferContainer.cs`
        //   的 `TimerTextFit` + `TimerText = PxRect(40,69,261.6,98)`（= 221.6 × 29）。
        // 🔴 **上限 32 ≠ 字号 30.6** —— 这就是挂住 A50① 的那 1.4px：自适应是**在 `[min,max]` 里二分**
        //    （`TextMeshPro.cs:4139-4149` 只涨到 `m_fontSizeMax` 为止），把上限设成「调用方字号」= 天花板矮 1.4px。
        //    ⇒ 把 `Battle/Label.cs` 的 `fontSizeMax` 改回 `cur`，下面第 2、4 条立刻红（第 2 条变 30.6）。
        {
            // 探针摆在**画面外**（y = 99），免得进后面那几张截图；量完立刻销毁（批处理里没有帧循环 ⇒ `DestroyImmediate`）
            var probe = Label.Create(driver.transform, "5d 20h 15m", new Vector3(0f, 99f, 0f), 4,
                                     Color.white, new Vector2(0.5f, 0.5f), "AutoFitProbe");
            Check(probe.CanRenderChinese,
                  "探针 label 走的是 **TMP** 后端（`_tmp != null`）—— 点阵后端没有自适应这回事，"
                + "这条不成立时下面就无效（子句：字体资产没加载）");
            probe.SetGlyphHeight(30.6f / 108f);                          // 原版 `m_fontSize = 30.6`
            probe.SetAutoFitBox(221.6f / 108f, 29f / 108f, 10f, 32f);    // 原版 `m_fontSizeMin/Max = 10/32`
            float fitMax = Label.FontSizeToPx(probe.FontSizeMax);
            float fitMin = Label.FontSizeToPx(probe.FontSizeMin);
            Check(Mathf.Abs(fitMax - 32f) < 0.05f,
                  $"★ `Label.SetAutoFitBox` 的**上限** = 原版 `m_fontSizeMax` **32px**（实得 {fitMax:F2}px —— "
                + "⚠️ 这个值 **≠** 本件的字号 30.6：旧写法拿字号当上限，短文案就永远画小 1.4px）");
            Check(Mathf.Abs(fitMin - 10f) < 0.05f,
                  $"★ …**下限** = 原版 `m_fontSizeMin` **10px**（实得 {fitMin:F2}px）");
            // 🔴 A57③：**同一个 label 调第二次**（`_tmp.fontSize = cur` 原来设在 `enableAutoSizing = true`
            //    之前，而 TMP 只在 `!m_enableAutoSizing` 时回写 `m_fontSizeBase`（`TMP_Text.cs:467`），
            //    起点又是 base（`TextMeshPro.cs:2149`））—— 再调一次，区间不许漂。
            probe.SetAutoFitBox(221.6f / 108f, 29f / 108f, 10f, 32f);
            float fitMax2 = Label.FontSizeToPx(probe.FontSizeMax);
            float fitMin2 = Label.FontSizeToPx(probe.FontSizeMin);
            Check(Mathf.Abs(fitMax2 - 32f) < 0.05f && Mathf.Abs(fitMin2 - 10f) < 0.05f,
                  $"★ **调第二次**区间不漂：仍是 10/32px（实得 {fitMin2:F2}/{fitMax2:F2}px）");
            Debug.Log(P + "   " + probe.DumpSizes());
            Object.DestroyImmediate(probe.gameObject);

            // 🔴 **2026-10-05 加：`NominalPx()` 的两条 `Set*` 路必须落到同一个 px** ——
            //    上面那两条量的是 `SetGlyphHeight` 那一路（`NominalPx()` 恰好等于 `FontSizeToPx(cur)`）；
            //    走 `SetCapHeight` 的件原来直接拿**大写高**当 px，而本工程「px」的标称口径是**汉字墨高**
            //    ⇒ 差 `Wglyph ÷ Wcap`（实测 0.0948/0.0779 = **1.2169 倍**），同一对 `minPx/maxPx`
            //    会因为调用方用哪个 `Set*` 而量出两个意思（`fontSizeMax` 偏大 1.2169 倍）。
            //    ⚠️ 期望值 32 / 10 是**原版 `Timer Text` 字段的原文**、换算常数取自**字体度量**（不是取自被测实现）。
            //    ⛔ 把 `NominalPx()` 改回 `world × 108` ⇒ 下面这条立刻红（会量成 ≈38.9px）。
            {
                var capProbe = Label.Create(driver.transform, "5d 20h 15m", new Vector3(0f, 99f, 0f), 4,
                                            Color.white, new Vector2(0.5f, 0.5f), "CapFitProbe");
                // 🔴 **A195**：这一段原来是**没有 `else` 的 `if`** —— `CanRenderChinese` 为假时下面那条 ★
                //    **一条都不跑**、section 照样绿（红线「断言不许有能整段静默不跑的写法」；同族 F10 已按这个形状修好）。
                //    形状 = **前提断言 + `if`**（与本函数上面 §4.6 那条 `probe` 的写法一致）：条件不成立 ⇒ **当场红**。
                Check(capProbe.CanRenderChinese,
                      "（前提）capProbe 走的是 **TMP** 后端（`_tmp != null`）—— 点阵后端没有自适应这回事，"
                    + "这条不成立时下面那条 ★ 等于没验（A195：原来这个 `if` 没有 `else`，整段会静默空转）");
                if (capProbe.CanRenderChinese)
                {
                    capProbe.SetCapHeight(30.6f / 108f);                    // 同一个原版字号，**换成大写那一路**
                    capProbe.SetAutoFitBox(221.6f / 108f, 29f / 108f, 10f, 32f);
                    float capMax = Label.FontSizeToPx(capProbe.FontSizeMax);
                    float capMin = Label.FontSizeToPx(capProbe.FontSizeMin);
                    float kGlyphOverCap = TmpFont.WorldGlyphPerFontSize / TmpFont.WorldCapPerFontSize;
                    Check(Mathf.Abs(capMax - 32f) < 0.05f && Mathf.Abs(capMin - 10f) < 0.05f,
                          $"★ **走 `SetCapHeight` 那一路**，同一对 min/max 也落到 10/32px"
                        + $"（实得 {capMin:F2}/{capMax:F2}px —— ⚠️ 旧写法按「大写高 = px」算，"
                        + $"会量成 {32f * kGlyphOverCap:F2}px，偏大 {kGlyphOverCap:F4} 倍）");
                }
                Object.DestroyImmediate(capProbe.gameObject);
            }

            // ---- 4.6b 🆕 2026-10-06（A80①）：`SetAutoFitBox` 的 **fontSize 时序**（A57③）----
            // 🔴 **为什么单开一条**：上面两条读的是 `FontSizeMin/Max`，而那两个数是**我们自己写进去的**
            //    （`cur × (maxPx|minPx)/nomPx`，`Battle/Label.cs:372-373`）—— 把 A57③ 的时序改坏
            //    （删掉 `enableAutoSizing = false;`、或把 `fontSize = cur;` 挪到 `enableAutoSizing = true;` 之后）
            //    **它们逐位同结果、一条都不红**；`Editor/ChatBoxProbe.cs` 那几条又是**同一名义字号**的重复调用 ⇒ 同样抓不到。
            // 判据 = TMP 源码两条事实（正是 `Label.SetAutoFitBox` 那两行注释引的同一对）：
            //    ① `TMP_Text.cs:467`：`fontSize` 的 setter **只在 `!m_enableAutoSizing` 时**才回写
            //       `m_fontSizeBase`；② `TextMeshPro.cs:2148-2149`：每次重排的**起点** =
            //       `Clamp(m_fontSizeBase, m_fontSizeMin, m_fontSizeMax)`
            //    ⇒ 同一个 label 被**第二次**调时自适应还开着 ⇒ base 停在**第一次**那个值。
            // ⚠️ **只比 `FontPxNow` 抓不到它**：自适应是**二分**（`TextMeshPro.cs:3073-3089` 缩 / `:4136-4155` 涨），
            //    起点不同、终点相同（都收在「装得下的最大号」）⇒ 被修坏的其实只有 `m_fontSizeBase` 这个**字段**，
            //    而它没有公开口（`protected`，读 `fontSize` 得到的是**自适应结果**不是 base）。所以本块两条都断：
            //      ① 复用过 vs 新建，`FontPxNow` 一致（断「历史不改变结果」—— 棘轮那一族的兜底）；
            //      ② **直读 `m_fontSizeBase`**（反射）两者一致 —— **这一条才是真辨别 A57③ 的**。
            // ⛔ 本块**不改** `Battle/Label.cs`（共用件全工程在用）：现写法（`:370-371` 先关自适应、再写字号）
            //    就是对的，这里只把「改坏了会红」这颗牙补上（A80② 的判据：红了才去动那个共用件）。
            {
                // TMP 的 `m_fontSizeBase` 是 `protected`（`TMP_Text.cs:473`）⇒ 判「base 有没有被刷成第二次那个字号」
                // 只能直读字段。（本工程读非公开字段有先例：`RuleEngineTest.cs:7823`。）
                var baseFld = typeof(TMPro.TMP_Text).GetField("m_fontSizeBase",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Check(baseFld != null,
                      "（前提）反射拿得到 TMP 的 `m_fontSizeBase` —— 拿不到时下面第 ② 条等于没验");

                // 复用那条：**先 A 再 B**，两步都走完整的一对 `SetGlyphHeight` + `SetAutoFitBox`
                var reuse = Label.Create(driver.transform, "5d 20h 15m", new Vector3(0f, 99f, 0f), 4,
                                         Color.white, new Vector2(0.5f, 0.5f), "AutoFitReuseProbe");
                // 对照：**新建一条，只走 B 那一次**
                var fresh = Label.Create(driver.transform, "5d 20h 15m", new Vector3(0f, 99f, 0f), 4,
                                         Color.white, new Vector2(0.5f, 0.5f), "AutoFitFreshProbe");
                bool tmpUp = reuse.CanRenderChinese && fresh.CanRenderChinese;
                Check(tmpUp, "（前提）两条探针都走 **TMP** 后端 —— 点阵后端没有「自适应」这回事，这条不成立时下面就无效");
                if (tmpUp)
                {
                    const float aPx = 18f;      // 第一次那个名义字号 —— 只要 **≠** 第二次那个就行
                    const float bPx = 30.6f;    // 原版 `Timer Text` 的 `m_fontSize`（与 §4.6 同源）
                    // 框 / min / max 一律取原版 `Timer Text` 的字段原文（221.6 × 29 / 10 / 32，见 §4.6 头）
                    reuse.SetGlyphHeight(aPx / 108f);
                    reuse.SetAutoFitBox(221.6f / 108f, 29f / 108f, 10f, 32f);
                    reuse.SetGlyphHeight(bPx / 108f);
                    reuse.SetAutoFitBox(221.6f / 108f, 29f / 108f, 10f, 32f);
                    fresh.SetGlyphHeight(bPx / 108f);
                    fresh.SetAutoFitBox(221.6f / 108f, 29f / 108f, 10f, 32f);

                    // ① 正本写的判别性写法：复用过 vs 新建，**实际渲染出来的字号**必须一致
                    //    （历史不得改变结果。⛔ 把 `SetSizes` 里那次强制重排去掉、或让 `_glyphHeight` 反过来
                    //      去读 TMP 的当前字号，两条路量出来的数就会分叉 ⇒ 这里红）
                    Check(Mathf.Abs(reuse.FontPxNow - fresh.FontPxNow) < 0.05f,
                          $"★ 复用过的 label（{aPx:F0}px → {bPx}px）与**新建**的（只走 {bPx}px）量出来是同一个字号"
                        + $"（复用 {reuse.FontPxNow:F2}px vs 新建 {fresh.FontPxNow:F2}px）");
                    // 顺带把「字号落在原版 auto 区间里」断出来（`已知的坑.md`：只断「渲染宽 ≤ 框宽」的话，
                    // **把字缩到看不见也能全绿** —— §4.6 那两条断的是区间两端，这里断**实际值落在区间内**）
                    Check(reuse.FontPxNow >= 9.95f && reuse.FontPxNow <= 32.05f,
                          $"★ 复用那条的实际字号落在原版区间 `[m_fontSizeMin, m_fontSizeMax]` = [10, 32]px 里"
                        + $"（实得 {reuse.FontPxNow:F2}px）");

                    // ② 🔴 **真辨别 A57③ 的那一条**：直读 `m_fontSizeBase`
                    var reuseTmp = reuse.GetComponentInChildren<TMPro.TMP_Text>(true);
                    var freshTmp = fresh.GetComponentInChildren<TMPro.TMP_Text>(true);
                    if (baseFld != null && reuseTmp != null && freshTmp != null)
                    {
                        float baseReuse = (float)baseFld.GetValue(reuseTmp);
                        float baseFresh = (float)baseFld.GetValue(freshTmp);
                        Check(Mathf.Abs(baseReuse - baseFresh) < 1e-3f,
                              $"★★ 复用那条的 `m_fontSizeBase` 与新建的**一致**（复用 {baseReuse:F4} vs 新建 {baseFresh:F4}"
                            + $" = {Label.FontSizeToPx(baseReuse):F2}px / {Label.FontSizeToPx(baseFresh):F2}px）"
                            + " —— ⛔ 把 `Battle/Label.cs` 的 `SetAutoFitBox` 里那句 `_tmp.enableAutoSizing = false;` 删掉"
                            + "（或把 `_tmp.fontSize = …;` 挪到 `enableAutoSizing = true;` 之后）⇒"
                            + "复用的那份停在**第一次**那档、新建的是第二次的 ⇒ 这里当场红");
                        Check(Mathf.Abs(Label.FontSizeToPx(baseReuse) - bPx) < 0.05f,
                              $"★ …而且 base 就是**第二次要的那个字号** {bPx}px（实得 {Label.FontSizeToPx(baseReuse):F2}px）");
                    }
                    Debug.Log(P + "   时序探针 复用：" + reuse.DumpSizes());
                    Debug.Log(P + "   时序探针 新建：" + fresh.DumpSizes());
                }
                Object.DestroyImmediate(reuse.gameObject);
                Object.DestroyImmediate(fresh.gameObject);
            }

            // ---- 4.6c 🆕 2026-10-11（A305①）：`SetAutoFitBox` 的**第 5 个实参**（原版 `m_fontSizeBase`）----
            // 🔴 **为什么必须有这一条**：第 5 个实参**只是自适应二分的起点**（`TextMeshPro.cs:2148-2149`），
            //    终点照样收敛 ⇒ **只比 `FontPxNow`/`WorldW` 抓不到它**（两者都会全绿）；它也没有公开读口
            //    （`m_fontSizeBase` 是 `protected`，`fontSize` getter 给的是**收敛结果**）——
            //    同 §4.6b 那两条的处境，所以同样**反射直读真字段**。
            // 判据 = **原版资产字段的实读值**（⛔ 不是我们自己的常量 —— 那会变成自证）：
            //    · `Booster Info Popup/window/Text/Title` 的 `m_fontSizeBase = **45.2**`（标称 40）
            //      （逐站表 §二·3 #6；我们那一处的调用点 = `Shell/BoosterInfoPopup.cs` 的 `title`）
            //    · TMP 的序列化默认 **36**（`TMP_Text.cs:473`）—— 「多数站是 36」就是它。
            // 🧨 **改坏法**：把 `Battle/Label.cs` 里 `baseCur` 那一行改回 `float baseCur = cur;`
            //    （或删掉 `_tmp.fontSize = baseCur;`）⇒ 第 ① 条**当场红**（量出来是 40 而不是 45.2）；
            //    把 `basePx > 0f` 那个三元写成 `basePx >= 0f ? … : cur`（0 也当真值用）⇒ 第 ② 条红
            //    （base 被写成 0，而 0 是个合法值 —— 这正是「缺省 0」必须与「真的 0」分开的原因）。
            {
                var baseProbe = Label.Create(driver.transform, "X", new Vector3(0f, 99f, 0f), 4,
                                             Color.white, new Vector2(0.5f, 0.5f), "BaseProbe");
                Check(baseProbe.CanRenderChinese,
                      "（前提）baseProbe 走的是 **TMP** 后端（`_tmp != null`）—— 点阵后端没有自适应，"
                    + "下面两条等于没验");
                if (baseProbe.CanRenderChinese)
                {
                    // ① 传了第 5 个实参 ⇒ 写进去的**就是它**（px 口径逐值相等，与标称不同）
                    baseProbe.SetGlyphHeight(40f / 108f);                 // 标称 40px（原版 Title 的 `m_fontSize`）
                    baseProbe.SetAutoFitBox(221.6f / 108f, 29f / 108f, 3f, 40f, 45.2f);
                    float gotBase = Label.FontSizeToPx(baseProbe.FontSizeBase);
                    Check(Mathf.Abs(gotBase - 45.2f) < 0.05f,
                          $"★ `SetAutoFitBox(…, basePx: 45.2)` 把 **`m_fontSizeBase` 写成 45.2px**（实得 {gotBase:F2}px）"
                        + " —— 反射直读 TMP 的真字段；⚠️ 它 **≠ 标称 40**（这正是这一格存在的意义）");
                    // ② 不传 ⇒ **旧行为**（base = 调用方那一档）一字不变（A57③/A80① 那两条的前提）
                    var baseProbe2 = Label.Create(driver.transform, "X", new Vector3(0f, 99f, 0f), 4,
                                                  Color.white, new Vector2(0.5f, 0.5f), "BaseProbe2");
                    baseProbe2.SetGlyphHeight(30.6f / 108f);              // 原版 `Timer Text` 的 `m_fontSize`
                    baseProbe2.SetAutoFitBox(221.6f / 108f, 29f / 108f, 10f, 32f);
                    float gotBase2 = Label.FontSizeToPx(baseProbe2.FontSizeBase);
                    Check(Mathf.Abs(gotBase2 - 30.6f) < 0.05f,
                          $"★ **不传第 5 个实参**时 base 仍是**调用方那一档** 30.6px（实得 {gotBase2:F2}px）"
                        + " —— 逐位等于旧行为（46 个调用点里没接的那些、以及本文件 §4.6/§4.6b 的探针都靠这一档）");
                    Object.DestroyImmediate(baseProbe2.gameObject);
                }
                Object.DestroyImmediate(baseProbe.gameObject);
            }
        }

        driver.SimulateAiTurn();                  // 对手出牌 + 攻击 + 交回来
        Step(0.3f);
        Check(ctx.Active == 0, "对手走完，回合回到我这里");
        if (wait != null) Check(!wait.Visible, "★ 回到我的回合 → 等待提示**关掉**（不是一直挂着）");
        // （回放条的断言在**第 14b 节** —— 它建在后面的 HUD 段里，这里只提一句，别在别处再断一遍）
        Debug.Log(P + $"   对手场上 {driver.FoeUnits.Count} 个  我场上 {driver.MyUnits.Count} 个");
        Debug.Log(P + $"   画面上手牌：{driver.HandViewNames()}");
        Debug.Log(P + $"   引擎手牌　：{string.Join("/", HandNames(ctx, 0))}");
        Debug.Log(P + $"   我场上　　：{driver.BoardViewNames(true)}");
        Shot(cam, "03_对手回合之后");

        // 找一对能打的
        int atkSlot = -1, tgtSlot = -1;
        for (int s = 0; s < RuleEngine.BoardSpec.Size && atkSlot < 0; s++)
        {
            var u = ctx.Players[0].Board[s];
            if (u == null || u.Exhausted) continue;
            for (int t = 0; t < RuleEngine.BoardSpec.Size; t++)
            {
                if (ctx.Players[1].Board[t] == null) continue;
                if (RuleCore.IsValidTarget(ctx, 0, s, 1, t, false) == RuleCodes.OK) { atkSlot = s; tgtSlot = t; break; }
            }
        }

        if (atkSlot >= 0)
        {
            // **原版的三步**：点自己的单位 → 弹选择器 → 选打法 → 点目标
            Check(driver.SimulateOpenCommand(atkSlot), $"点自己的槽 {atkSlot} → 弹出攻击方式选择器");
            Check(driver.SelectedSlot == atkSlot, "选中的就是那个单位");
            Check(driver.SelectorOpen, $"选择器开着（{driver.SelectorDescription}）");
            Check(driver.HasCommand(AttackKind.Melee), "有近战这一项");
            Step(0.25f);
            Shot(cam, "03b_攻击方式选择器");

            driver.SimulateCommand(AttackKind.Melee);
            Check(!driver.SelectorOpen, "选完打法，选择器收起");
            Check(driver.Command == AttackKind.Melee, "定下来的是近战");

            int foeBefore = ctx.Players[1].Board[tgtSlot].Health;
            Check(driver.SimulateResolve(tgtSlot) == RuleCodes.OK, $"打槽 {tgtSlot} 成功");
            var after = ctx.Players[1].Board[tgtSlot];
            Check(after == null || after.Health < foeBefore,
                  $"目标掉血了（{foeBefore} → {(after == null ? "阵亡" : after.Health.ToString())}）");
            Step(0.3f);
            Shot(cam, "04_攻击后");
        }
        else Debug.Log(P + "   （这一回合没有合法攻击，跳过）");

        // ---- 5b. 选择器：只有该有的项 / 拖出阈值 ----
        Debug.Log(P + "--- 攻击方式选择器（原版 Drag Attack Selector）---");
        ClearEffects();
        {
            // 远程单位 + 近战单位的表现应当不同：`Ballista` 近战 0 / 远程 4 —— 只该有「远程」那一项
            int probeSlot = FreeSlot(ctx, 0);
            if (probeSlot >= 0)
            {
                ctx.Players[0].Board[probeSlot] =
                    new UnitState(CardByName(StarterCards.Tide(), "Ballista"), false) { Exhausted = false };
                driver.RefreshAll();

                if (driver.SimulateOpenCommand(probeSlot))
                {
                    Check(driver.HasCommand(AttackKind.Ranged), "Ballista（近战 0 / 远程 4）有「远程」");
                    Check(!driver.HasCommand(AttackKind.Melee), "…而且**没有**「近战」—— 0 攻打不了近战");
                    Debug.Log(P + $"   选择器：{driver.SelectorDescription}");
                }

                // 指针压在按钮上 → **决定"点下去打哪个"**（`HoveredCommand`）。
                // 🔴 **2026-09-29 更正**：那圈 + 1.3 倍**不再挂在 hover 上** —— 原版挂的是
                //   `unit.attackType`（`HighlightSelectedAttackTypeButtons`）⇒ 悬停**不改变按钮大小**。
                //   这一格原来按「我们挂在 hover 上」写，跟着实现一起改了。
                var rp = driver.Selector.ButtonWorld(AttackKind.Ranged);
                Check(rp.HasValue, "拿得到「远程」按钮的世界坐标");
                if (rp.HasValue)
                {
                    var before = driver.Selector.ButtonScale(AttackKind.Ranged);
                    driver.SimulatePointerAt(rp.Value);
                    Check(driver.HoveredCommand == AttackKind.Ranged,
                          $"指针压到「远程」上 → 就是它（{driver.SelectorDescription}）");
                    Step(0.15f);
                    Check(Mathf.Abs(driver.Selector.ButtonScale(AttackKind.Ranged) - before) < 0.01f,
                          $"悬停**不再**改变按钮大小（{before:F2} → {driver.Selector.ButtonScale(AttackKind.Ranged):F2}）"
                        + " —— 原版那圈只挂「已选打法」那一格（判据 → `AttackSelector.RefreshPicked`）");
                    Shot(cam, "03c_按钮高亮");
                }

                driver.SimulateDeselect();
                Check(!driver.SelectorOpen, "点槽外 → 选择器收起、取消指挥");
                ctx.Players[0].Board[probeSlot] = null;
                driver.RefreshAll();
            }
            else Debug.Log(P + "   （自己场上满了，跳过选择器用例）");

            // 拖拽阈值：0.085 × 屏高来自原版 `accumulatedDragForMinDistance`
            Check(Mathf.Abs(AttackSelector.DragThreshold01 - 0.085f) < 1e-6f,
                  $"拖拽阈值 = 0.085 屏高（{AttackSelector.DragThresholdWorld:F2} 世界单位）");
            Debug.Log(P + $"   拖出 {AttackSelector.DragThresholdWorld:F2} 世界单位（1080p ≈ "
                        + $"{AttackSelector.DragThreshold01 * 1080f:F0} px）才弹出选择器"
                        + " —— ⚠️ **如实标注的偏离**：原版那个字段其实是**入场动画的分母**，"
                        + "真正的弹出阈值常量没定死（判据 → `待办判据_战场与战斗视图.md` Q7）");

            // 🆕 2026-09-29（Q7 第 4 条）：**三钮入场动画** —— 公式逐句照
            // `CardDisplayAttackTypeButton__MoveButton.c`：`pos = lerp(归位位 + 方向×外扩距, 归位位, t)`；
            // `directionToPivotPoint` 三个都朝下（近战 -0.5,-1 / 技能 0,-1.1 / 远程 0.5,-1），
            // `furtherPointDistance` = 45 / 45 / 80 px。
            // ⚠️ **A195 核过：这两个门都安全**（没改）—— `probeSlot < 0` 时上面那个 `if/else` 已经**出声**
            //    （「自己场上满了，跳过选择器用例」）；`SimulateOpenCommand` 为假 = 选择器没打开，
            //    而**同一节的第一次调用**（上面那段 `if` 的**外**面）紧跟着就是
            //    `Check(rp.HasValue, "拿得到「远程」按钮的世界坐标")` —— `AttackSelector.ButtonWorld` 在
            //    `!Visible` 时返回 null ⇒ 那一档会红（两处调用是同一个夹具、同一个 API）。
            if (probeSlot >= 0)
            {
                ClearEffects();
                ctx.Players[0].Board[probeSlot] =
                    new UnitState(CardByName(StarterCards.Tide(), "Ballista"), false) { Exhausted = false };
                driver.RefreshAll();
                if (driver.SimulateOpenCommand(probeSlot))
                {
                    var p0 = driver.Selector.ButtonWorld(AttackKind.Ranged);
                    Check(driver.Selector.EnterProgress <= 1e-3f,
                          $"刚弹出时入场进度 = 0（实得 {driver.Selector.EnterProgress:F3}）");
                    // 🔴 **2026-10-13（A658）就地更正**：这两句原来是 `Tick(0.04f)` / `Tick(1f)`
                    //    （「按时间推进 0.04s = 半程，时长 0.085s」）。判据补上了 ——
                    //    `AttackTypesButtonsController__MoveButtons.c` 里驱动量**是累计的【视口拖拽位移】**：
                    //    `accumulatedDrag(+0x50) += TouchInputManager.TouchDragDeltaViewport(+0x18)`，
                    //    再 `t = clamp01(−accumulatedDrag.y ÷ accumulatedDragForMinDistance(+0x30))`
                    //    ⇒ 改喂位移（0.0425 = 0.085 的一半；批处理里没有输入层，只能自己喂）。
                    driver.Selector.TickForTest(0.04f, new Vector2(0f, -0.0425f));   // 拖了半程
                    var pMid = driver.Selector.ButtonWorld(AttackKind.Ranged);
                    Check(driver.Selector.EnterProgress > 0.2f && driver.Selector.EnterProgress < 0.9f,
                          $"拖了半个阈值（0.0425 屏高）⇒ 进度在半程（实得 {driver.Selector.EnterProgress:F3}）");
                    driver.Selector.TickForTest(1f, new Vector2(0f, -1f));           // 拖够（超了会被夹到 1）
                    var p1 = driver.Selector.ButtonWorld(AttackKind.Ranged);
                    Check(driver.Selector.EnterProgress >= 1f, "拖够 ⇒ 入场进度 = 1（夹在 1）");
                    Check(p0.HasValue && pMid.HasValue && p1.HasValue, "三个阶段都拿得到按钮坐标");
                    if (p0.HasValue && p1.HasValue)
                    {
                        Check((p0.Value - p1.Value).magnitude > 0.01f,
                              $"★ 入场时按钮**真的从外扩位飞回来**（起点距归位 {(p0.Value - p1.Value).magnitude:F3} 世界单位）");
                        Check(p0.Value.y < p1.Value.y - 0.001f,
                              "★ 外扩方向朝【下】（原版 `directionToPivotPoint` 三个的 y 都是负的）");
                    }
                    driver.SimulateDeselect();
                }
                ctx.Players[0].Board[probeSlot] = null;
                driver.RefreshAll();
            }
        }

        // ---- 5b·2. 棋盘上的「轻点 / 拖拽」分工（🔴 **2026-09-28 照原版改的**）----
        //      原版：**轻点 = 开大卡展示窗**（我方**与对手**都给开 —— 棋盘段没有 `isPlayer` 守卫）·
        //            **拖够 = 弹三选一**（只对我方 —— 敌方 `OnTouchDrag` 有 `isPlayer` 闸，拖不动）。
        //      判据 → `资料/待办判据_战场与战斗视图.md` §8b；分流那一段 = `BattleDriver.BoardPress`（只此一处）。
        Debug.Log(P + "--- 棋盘轻点 vs 拖拽（原版分工）---");
        ClearEffects();
        {
            int mine = FreeSlot(ctx, 0), foe = FreeSlot(ctx, 1);
            if (mine >= 0 && foe >= 0)
            {
                ctx.Players[0].Board[mine] =
                    new UnitState(CardByName(StarterCards.Tide(), "Ballista"), false) { Exhausted = false };
                ctx.Players[1].Board[foe] =
                    new UnitState(CardByName(StarterCards.Tide(), "Ballista"), false) { Exhausted = false };
                driver.RefreshAll();

                // ① 轻点**我方**单位 ⇒ 开大卡窗，而且**不**弹三选一（原版：轻点不承担选中职责）
                Check(driver.SimulateTapUnit(0, mine), "轻点我方单位 → 开大卡展示窗");
                Check(!driver.SelectorOpen, "…而且**没有**弹攻击三选一（原版轻点不选中）");
                Debug.Log(P + $"   窗里显示的是：「{driver.CardDisplay.ShownTitle}」");
                Check(driver.CardDisplay.EffectRowCount == 0,
                      "没 buff 的单位 → 「谁给我加的 buff」整组**不出现**（原版 `DisplayCardEffects`：有 effect 才露）");
                Step(0.2f); Shot(cam, "03d_棋盘轻点开大卡窗");
                driver.SimulateTapUnit(0, mine);
                Check(!driver.CardDisplay.Visible, "再轻点一次 → 关窗");

                // ①′ 有 buff 的单位 ⇒ 那块露出来（原版同一条链：`ShowBattleCard` → `DisplayCardEffects`）
                ctx.Players[0].Board[mine].AddTempBuff(new UnitState.TempBuff
                {
                    Name = "attack", Value = 2, Owner = 0, UntilMyNextTurn = true,
                    Src = "战术卡", SourceCard = "Blind Librarian",
                });
                ctx.Players[0].Board[mine].AddTempBuff(new UnitState.TempBuff
                {
                    IsKeyword = true, Name = RuleEngine.KeywordTable.Vanguard, Value = 1, Owner = 0,
                    UntilMyNextTurn = true, Src = "战术卡", SourceCard = "Blind Librarian",
                });
                Check(driver.SimulateTapUnit(0, mine), "有 buff 的单位 → 照样开窗");
                Check(driver.CardDisplay.EffectRowCount == 2,
                      $"…而且效果清单露了 2 行（实际 {driver.CardDisplay.EffectRowCount}）");
                Check(driver.CardDisplay.EffectWho(0) == "Blind Librarian",
                      $"第 1 行「谁给的」= **施加者真卡名**（`SourceCard`，不是恒为「战术卡」的 `Src`）：{driver.CardDisplay.EffectWho(0)}");
                Check(driver.CardDisplay.EffectWhat(0) == "+2 近战", $"第 1 行「给了什么」：{driver.CardDisplay.EffectWhat(0)}");
                Check(driver.CardDisplay.EffectWhat(1) == "先锋",
                      $"第 2 行是**关键词**（走 `CardText.KeywordZh`）：{driver.CardDisplay.EffectWhat(1)}");
                Debug.Log(P + $"   效果清单：{driver.CardDisplay.EffectWho(0)} / {driver.CardDisplay.EffectWhat(0)}"
                            + $" · {driver.CardDisplay.EffectWho(1)} / {driver.CardDisplay.EffectWhat(1)}");
                // 🔴 **左对齐**（原版 `m_HorizontalAlignment = 1`）—— 这条专门抓「短句被居中」那个 bug：
                //    它**肉眼才看得见**、别的断言全绿（2026-09-28 就是这么漏过去一次的）。
                float wantLeft = CardWinBox.EffCx - CardWinBox.EffWhoW * 0.5f;
                float l0 = driver.CardDisplay.EffectWhatLeftPx(0), l1 = driver.CardDisplay.EffectWhatLeftPx(1);
                Check(Mathf.Abs(l0 - wantLeft) < 1.5f, $"两行字都**贴框左缘**（原版 Left 对齐）：{l0:F1} vs 期望 {wantLeft:F1}");
                Check(Mathf.Abs(l0 - l1) < 1.5f, $"…而且短句与长句**左缘同一条线**（{l0:F1} / {l1:F1}）—— 不是居中");
                Check(driver.CardDisplay.EffectTextInFrontOfBg(0),
                      "行内文字**在底板前面**（z 更小）—— 反了会被压暗（2026-09-28 踩过：图上只是变暗，断言全绿）");
                Step(0.25f); Shot(cam, "03e_效果清单");
                driver.SimulateTapUnit(0, mine);
                Check(!driver.CardDisplay.Visible, "…再点一次关掉");

                // ② 轻点**对手**单位 ⇒ 也给开（原版棋盘段没有任何敌我判断）
                Check(driver.SimulateTapUnit(1, foe), "轻点对手单位 → **也**开大卡窗（原版棋盘段无 `isPlayer` 守卫）");
                driver.SimulateTapUnit(1, foe);
                Check(!driver.CardDisplay.Visible, "…再点一次关上");

                // ③ 拖够阈值 ⇒ 弹三选一（原版打开它的**唯一**入口是拖拽）
                Check(driver.SimulateDragUnit(0, mine), "拖够阈值 → 弹三选一（原版 `TryDraggingFromBoard`）");
                Check(!driver.CardDisplay.Visible, "…这时**不**开大卡窗（拖过的松手不算轻点）");
                driver.SimulateDeselect();

                // ④ 对手的单位**拖不动**（原版 `OnTouchDrag` 的 `isPlayer` 闸）
                Check(!driver.SimulateDragUnit(1, foe), "对手单位拖不动（拖够也不弹三选一）");

                ctx.Players[0].Board[mine] = null;
                ctx.Players[1].Board[foe] = null;
                driver.RefreshAll();
            }
            else Debug.Log(P + "   （场上位置不够，跳过棋盘轻点用例）");
        }

        // ---- 5c. 选目标反馈：准星 + 弧线（原版 `NoCanvas2D/Attack Target Reticle`）----
        Debug.Log(P + "--- 选目标反馈：准星 + 弧线 ---");
        ClearEffects();
        {
            // 这一节要个干净的靶场，但**别把后面的用例饿着** —— 用完原样放回去
            var foeBackup = new UnitState[BoardSpec.Size];
            for (int t = 0; t < BoardSpec.Size; t++)
            {
                foeBackup[t] = ctx.Players[1].Board[t];
                if (t != BoardSpec.WarlordSlot) ctx.Players[1].Board[t] = null;
            }

            int retAtk = FreeSlot(ctx, 0);
            const int foeSlot = 1;                      // 离督军位远一点
            ctx.Players[0].Board[retAtk] =
                new UnitState(CardByName(StarterCards.Ember(), "Veteran"), false) { Exhausted = false };
            ctx.Players[1].Board[foeSlot] =
                new UnitState(CardByName(StarterCards.Tide(), "Tide Minion"), false);
            driver.RefreshAll();

            // 准星尺寸：原版 8.694 世界单位 **在 hudCamera 画布平面帧** ⇒ ×14.84 px/单位 ÷108 = **1.195**
            // 🔴 2026-09-18 更正：旧值 4.006 是用「原版槽距 3.0 世界单位」推的 —— **3.0 是编辑器占位值**
            //    （运行时被 `MinionSeparation 0.82` 换掉）⇒ 那个 49.77 px/单位 是错的。见 `TargetReticle` 的常量注释。
            var cs = TargetReticle.CrosshairWorldSize;
            Check(Mathf.Abs(cs.x - 1.195f) < 0.02f && Mathf.Abs(cs.y - 1.208f) < 0.02f,
                  $"准星世界尺寸 {cs.x:F3} × {cs.y:F3}（原版 8.694×8.791 **按 HUD 画布平面帧** 14.84 px/单位 换算）");
            // 🔴 **判据要能区分两个帧**：准星在 **HUD 画布平面帧**（14.84 px/单位）、弧线在**棋盘帧**（182.14），
            //    同一个量按哪个帧换算差 **12.3 倍**。⚠️ 别写成 `cs.x*108 == 8.694*14.84` —— 那是**同义反复**
            //    （`CrosshairWorldSize` 就是 `CrossW/108`），永远为真、等于没断言。
            Check(Mathf.Abs(cs.x * 108f - 8.694f * 182.14f) > 100f,
                  $"★ 准星换算用的是 **HUD 画布平面帧**（{cs.x * 108f:F1} px），**不是**棋盘帧"
                  + $"（那样会是 {8.694f * 182.14f:F1} px）—— 差 12.3 倍");

            // 弧线材质：**必须是原版那个 shader** —— 截图上看不出「用的是不是它」
            Check(driver.reticle.LineShaderName == "Everguild/FX/Unlit UV scroll",
                  $"弧线用的是**原版材质**（`CroshairTrail` 的 shader = {driver.reticle.LineShaderName}）");
            Check(driver.reticle.LineTextureName == "CrosshairTrail",
                  $"弧线的贴图是原版那张（`_MainTex` = {driver.reticle.LineTextureName}）");

            var foeGo = driver.FoeUnits[foeSlot];
            Check(driver.SimulateOpenCommand(retAtk), $"点自己的槽 {retAtk} → 弹选择器");
            // 🆕 2026-09-29 攻击选择器三条照原版改（判据 → `资料/待办判据_战场与战斗视图.md` Q7）
            {
                var sel = driver.selector;
                var selUnit = driver.BoardViewAt(retAtk);
                Check(sel != null && selUnit != null
                      && Vector3.Distance(sel.AnchorWorld, selUnit.transform.position) < 0.01f,
                      "★ 整条挂在**被拖的那个单位身上**（原版展开时 `set_position(Get2DWorldPosFromBoardPos(单位))`；"
                    + $"实测偏差 {(sel != null && selUnit != null ? Vector3.Distance(sel.AnchorWorld, selUnit.transform.position) : -1f):F4}）"
                    + " —— 原来固定在屏幕中心");
                var bg = sel.BgSizePx;
                Check(Mathf.Abs(bg.x - 514.8f) < 0.5f && Mathf.Abs(bg.y - 125.4f) < 0.5f,
                      $"★ 底板 = 原版那条尺寸 **514.8 × 125.4**（实测 {bg.x:F1} × {bg.y:F1}；原来是个 125.4² 的方块）");
                Check(sel.BgTint.r > 0.99f && sel.BgTint.g > 0.99f && sel.BgTint.b > 0.99f && sel.BgTint.a > 0.99f,
                      "…而且是**白色实心**（原版那个 Image `m_Sprite` 空 + `m_Color=(1,1,1,1)`；"
                    + $"实测 ({sel.BgTint.r:F2},{sel.BgTint.g:F2},{sel.BgTint.b:F2},{sel.BgTint.a:F2})）"
                    + " —— 原来我们画的是深色半透明");
                Check(sel.ButtonScale(AttackKind.Melee) < 1.01f && sel.ButtonScale(AttackKind.Ranged) < 1.01f,
                      "★ 这个单位**还没打过** ⇒ 各格都不放大（原版那圈挂的是 `unit.attackType`，0 = 不亮）");
                // 「已选打法」那一格：挑一个**这一手真有的**打法（不同单位的可选格不同：近战/远程/技能），
                // 给它一个 `LastAttackType` 再开一次 —— 原版那圈（+ 1.3 倍）挂的就是它。
                var pickKind = AttackKind.Melee;
                foreach (var o in sel.Options) if (o.Enabled) { pickKind = o.Kind; break; }
                ctx.Players[0].Board[retAtk].LastAttackType =
                    pickKind == AttackKind.Melee ? 1 : (pickKind == AttackKind.Ranged ? 2 : 4);
                driver.SimulateOpenCommand(retAtk);
                Check(sel.ButtonScale(pickKind) > 1.2f,
                      $"★ 那圈 + 1.3 倍挂在**已选打法那一格**（原版 `unit.attackType`；"
                    + $"给它 {pickKind} 时实测 {sel.ButtonScale(pickKind):F2}）"
                    + " —— 我们原来挂在**指针悬停**上");
                ctx.Players[0].Board[retAtk].LastAttackType = 0;
                driver.SimulateOpenCommand(retAtk);
            }
            driver.SimulateCommand(AttackKind.Melee);
            Check(!driver.SelectorOpen, "定下打法 → 选择器收起");
            // 🆕 2026-09-29 照原版改：**进入选目标状态就亮准星**，不需要指针先压到合法目标上
            // （原版六个 `ToggleCrosshair(true,false)` 调用点全在「开始选目标」那几支；
            //  我们原来是「指针压在合法目标上才亮」—— 与原版相反。判据 → `资料/待办判据_战场与战斗视图.md` Q7 第 6 条）。
            Check(driver.ReticleVisible, "★ 定下打法（进入选目标）→ **准星立刻亮**（原版 `ToggleCrosshair(true)` 那六支的语义）");

            // ① 指针移到合法目标上 → 准星跟着走、弧线拱起来
            driver.SimulatePointerAt(foeGo.transform.position);
            // 🆕 2026-09-29：**必须先 Step 再读/再拍** —— 准星现在是**淡入**的（原版 `colorChangeSpeed 8.0`，
            //   0→1 用 0.125s），不推的话它停在 alpha 0 上（截图里看不见准星）。
            Step(0.2f);
            Check(driver.ReticleVisible, "指针压在合法目标上 → 准星还在（状态没结束）");
            Check(driver.reticle.FadeAlpha >= 0.999f,
                  $"★ 淡入推完 ⇒ 准星完全不透明（实测 {driver.reticle.FadeAlpha:F3}；原版 ramp 速率 8.0/秒）");
            // 🆕 2026-09-29 照原版改：准星**不再压在卡上**，而是落在
            //   「指针射线 ∩ (地板平面 ∪ 敌兵平面)，取近的那个命中点」（原版 `UpdateTrail` 的①～④）。
            //   ⇒ 断言改断**落点在不在这两个平面上** —— 断「压在卡身上」是旧口径。
            Check(driver.reticle.LastAimOnPlane,
                  "★ 准星落点 = **双平面求交**的结果（原版 `UpdateTrail`：`floorPlane` / `enemyMinionPlane` 取近）");
            Check(driver.reticle.AimMisses == 0,
                  $"★ 求交一次都没打空（实测 {driver.reticle.AimMisses} 次；打空 = 射线背对或没有相机）");
            Vector3 aimFoe;
            Check(driver.reticle.CrossPosition(out aimFoe)
                  && Mathf.Abs(aimFoe.z - (TargetReticle.Z - 0.02f)) < 1e-3f,
                  "准星节点画在 HUD 那一层（z = `TargetReticle.Z` − 0.02，与弧线同一平面）");
            // 🔴 **2026-09-29 更正**：这里原来断的是 `== 10`，并把 10 说成「段」——
            //   原版 `CrosshairLineEffect.curvePoints = 10` 是**段数**，循环 `0..curvePoints`（含两端）
            //   ⇒ **点数 = 11**（`CrosshairLineEffect__SetPoints.c`）。
            Check(driver.reticle.ArcPointCount == 11,
                  $"弧线 {driver.reticle.ArcPointCount} 个点（原版 `curvePoints` = 10 ⇒ **11 个点**）");
            Check(driver.reticle.ArcUseWorldSpace,
                  "★ 弧线用**世界空间**（原版 `CrosshairLine 3D` 的 `m_UseWorldSpace = true`）");
            Check(!Cursor.visible,
                  "★ 准星亮着时**藏掉系统光标**（原版 `TargetReticleController__ToggleCrosshair`：`Cursor.set_visible(show ^ 1)`）");
            float meleeBulge = driver.reticle.ArcBulge;
            Check(meleeBulge > 0.05f, $"近战的弧线拱起来了（离弦 {meleeBulge:F3} 世界单位）");
            var mc = driver.reticle.CrossColor;
            Check(mc.r > 0.8f && mc.g < 0.2f && mc.b < 0.1f,
                  $"近战 → 准星红（{mc.r:F2},{mc.g:F2},{mc.b:F2}，原版 `colorPresets` attackType=1）");
            // 合法目标的**底光**：原版 `Highlight`（红），锚在卡体那颗**近战**数值格上
            Check(driver.FoeUnits[foeSlot].CurrentGem == TargetGem.Melee,
                  "合法目标 → 卡面**近战**那颗数值格的底光点亮（原版 `Highlight` / `40K_melee_glow`）");
            Shot(cam, "09_选目标_准星");

            // ② 指针挪开 → 🔴 **准星不灭**（2026-09-29 照原版改）。
            //    原版「灭」的时机是**选目标状态结束**（`StopTracking` / `CancelActionStates` / `ResolveEndTurn`），
            //    不是「指针离开目标」—— 准星本来就是**跟着指针**走的那条线。
            //    这一档要收的是**悬停反馈**（合法目标底光 + `selectedTargetInBoard` 橙 + 「会打死它」图标）。
            driver.SimulatePointerAt(LayoutSpace.ToWorld(0.02f, 0.06f));
            Check(driver.ReticleVisible, "★ 指针离开合法目标 → 准星**仍然亮着**（原版只在状态结束时才灭）");
            Check(!Cursor.visible, "★ 准星亮着 ⇒ 系统光标一直是藏的（`Cursor.set_visible(show ^ 1)` 的方向没翻）");
            Check(driver.ReticleTargetView == null, "指针不在任何合法目标上 → 悬停那一档收起（准星与它是两件事）");

            // ②·b 🆕 2026-09-29：指针**压着**的那一个 = 原版 `selectedTargetInBoard`（橙 #FF8400 ×1.05）
            //      **外加**「这一下会打死它」那层（原版 `Minion Death Icon`）。
            //      判据 → `资料/待办判据_战场与战斗视图.md` §8b / Q7。
            //      ⚠️ 这一档和 `ValidTarget` 是**同一时刻的两档**：合法的都绿，**压着的那一个**橙。
            var foeUnit = ctx.Players[1].Board[foeSlot];
            int savedHp = foeUnit.Health;
            foeUnit.Health = 1;                        // 逼出「够致死」那一支（近战至少 1 点）
            driver.SimulatePointerAt(driver.FoeUnits[foeSlot].transform.position);
            var fv = driver.BoardViewAt(foeSlot, false);
            Check(driver.ReticleTargetView == fv, "准星压着的目标被记下来了（离开时要能收掉）");
            Check(fv.State == CardHighlightState.SelectedTargetInBoard,
                  $"指针压着的那一个 → `selectedTargetInBoard`（实测 {fv.State}）");
            Step(0.2f);                                // `CardTween.Mode = Manual`：手动把补间推到头
            // ⚠️ 必须**先 Step 再读色** —— 那圈现在是补间的（`selectedTargetInBoard` 属 0.1s 那一档）
            Check(Mathf.Abs(fv.RimColor.r - 1f) < 0.02f && Mathf.Abs(fv.RimColor.g - 0.5176f) < 0.02f
                  && fv.RimColor.b < 0.01f,
                  $"…它那圈状态环是**橙 #FF8400**（实测 {fv.RimColor.r:F3},{fv.RimColor.g:F3},{fv.RimColor.b:F3}）");
            Check(Mathf.Abs(fv.HighlightMul - 1.05f) < 0.01f,
                  $"…而且**放大 1.05**（原版 `selectedTargetInBoard` 那一档就是 ×1.05；实测 {fv.HighlightMul:F3}）"
                + " —— ⚠️ 同族的 `selected`（白、瞬切）**不缩放**，别照抄这一条");
            Check(fv.WillDieVisible, "预览伤害**够打死**它 → 「这一下会打死它」那层亮起来");
            Check(fv.WillDieTexName == "Minion_Death_Icon",
                  $"…贴的是原版那张 `Minion Death Icon`（实测 {fv.WillDieTexName}）");
            Check(Mathf.Abs(fv.WillDieLocalScale.x - 1.27f) < 1e-4f && Mathf.Abs(fv.WillDieLocalScale.y - 1.8f) < 1e-4f,
                  $"…尺寸 = 原版 `m_Size` 1.27 × 1.8（实测 {fv.WillDieLocalScale.x:F3} × {fv.WillDieLocalScale.y:F3}）");
            Step(0.35f);                               // 原版那条 clip 的曲线只走到 0.25 s
            Check(Mathf.Abs(fv.WillDieAlpha - 1f) < 0.02f, $"…0.25 s 内淡入到 1（实测 {fv.WillDieAlpha:F3}）");
            Check(Vector3.Distance(fv.WillDieLocalPos, CardView.WillDieToForCheck) < 1e-3f,
                  $"…同时**上浮**到原版 clip 的终点（y {fv.WillDieLocalPos.y:F3} ← 起点 {CardView.WillDieFromForCheck.y:F3}）"
                  + " —— 那条 clip 是「淡入 + 上浮」，不是纯淡入");
            Shot(cam, "09b_选目标_会打死它");

            // 另一半：打不死 → **不亮**（原版 `ToggleCombatPreviewHighlight` 的 else 支）
            foeUnit.Health = 99;
            driver.SimulatePointerAt(driver.FoeUnits[foeSlot].transform.position);
            Check(!fv.WillDieVisible, "预览伤害**打不死**它 → 那层不亮（不是一直挂在那儿）");
            // 指针离开 → 两样一起收：图标关掉、状态退回「只是合法目标」
            driver.SimulatePointerAt(LayoutSpace.ToWorld(0.02f, 0.06f));
            Check(!fv.WillDieVisible && fv.State == CardHighlightState.ValidTarget,
                  $"指针移开 → 图标收掉、只留「合法目标」那一档（实测 {fv.State}）");
            foeUnit.Health = savedHp;
            driver.RefreshAll();

            // ③ 换成远程：准星变紫，弧线**明显比近战平**（原版两条 profile 曲线差一个数量级）
            ctx.Players[0].Board[retAtk] =
                new UnitState(CardByName(StarterCards.Tide(), "Ballista"), false) { Exhausted = false };
            driver.RefreshAll();
            driver.SimulateOpenCommand(retAtk);
            driver.SimulateCommand(AttackKind.Ranged);
            driver.SimulatePointerAt(driver.FoeUnits[foeSlot].transform.position);

            // 🔴 **2026-10-12（A354-a）：补一条能分辨【曲线形状】的断言**（原来只有下面的「触发次数」）。
            //   判据 = `DG.Tweening.DOTween.Punch` 的段数公式 `count = (int)(vibrato × duration)`、
            //          **`< 2` 钳成 2**；关键帧 `end[0] = direction`、`end[count-1] = 0`，
            //          中间奇数格 `end[i] = −ClampMagnitude(direction, mag × elasticity)`。
            //          （两条独立路径读出来的：我们 `Assets/Plugins/Demigiant/DOTween/DOTween.dll` 的 IL
            //           ＋ 原版 `GameAssembly.dll` 的同名函数反汇编，逐句对位 —— 见 `资料/已知的坑.md`
            //           2026-10-11 那条。）
            //   ⇒ 准星这条 `(int)(0 × 0.5) = 0` **钳成 2** ⇒ `end = [0.2·s₀, 0]`
            //   ⇒ 轨迹 `s₀ → s₀+0.2s₀ → s₀`：**单峰、构造性不向下穿零**。
            //      ⚠️ 与 `CardFeel` 挨打那条 `(int)(8 × 0.4) = 3`（**会反向过冲**）**不是一回事** ——
            //      那一条在同文件「后坐 punch = 0.4s / vibrato 8 / 弹性 0.3」那组常量下面（`ProbePunchBack` 那一节）。
            //   采样法：**只泵补间、不推时钟**（`CardTween.Advance` = `DOTween.ManualUpdate`，
            //   不碰 `AdvanceTimeline` / 粒子）⇒ 后面那几条读状态 / 拍图的断言看到的时刻**一格没变**
            //   （淡入照旧由下面的 `Step(0.2f)` 推）。
            //   🧨 改坏法 ①：把 punch 拿掉（或 `ScaleOnChangeModifier` 写 0）⇒ 峰值差 ≈ 0 ⇒ 第一条红；
            //   🧨 改坏法 ②：把 `vibrato` 调到 ≥ 6（`(int)(6 × 0.5) = 3`）⇒ 第 2 段压到原始 scale
            //      以下（反向过冲）⇒ 第二条红。⛔ 「触发次数」那条对这两种改动**都无感**（这就是它弱的地方）。
            {
                float sMin = float.MaxValue, sMax = float.MinValue;
                const int NSample = 36;                 // 36 × (1/60) = 0.6 s > punch 的 0.5 s：整条曲线 + 回原位都盖住
                for (int k = 0; k < NSample; k++)
                {
                    CardTween.Advance(1f / 60f);        // 1/60 一采 ⇒ 段界那一拍不会漏
                    float s = driver.reticle.CrossScaleX;
                    if (s < sMin) sMin = s;
                    if (s > sMax) sMax = s;
                }
                float sRest = driver.reticle.CrossScaleX;   // 补间已走完（`end[last] = 0`）⇒ 这个值 = 原始 scale
                Check(sMax - sRest > 0.02f * sRest,
                      $"★ 换打法那一下**真的弹了**：峰值 {sMax:F4} = 原始 {sRest:F4} + 约 0.2×原始"
                    + "（原版那次 `DOPunchScale(原始scale × 0.2, 0.5s, vibrato 0, elasticity 1)`）——"
                    + " 🧨 不弹 / 幅度写 0 的实现恒为 0 ⇒ 这里红");
                Check(sMin >= sRest - 1e-3f,
                      $"★ ……而且**单峰、不向下穿零**（最低 {sMin:F4}，原始 {sRest:F4}）——"
                    + " `count = (int)(0 × 0.5) = 0` **钳成 2** ⇒ `end = [0.2s₀, 0]`、只涨不跌。"
                    + " 🧨 改坏法：`vibrato` 调到 ≥ 6（`(int)(6 × 0.5) = 3`）"
                    + " ⇒ `end[1] = −ClampMagnitude(dir, mag × elasticity)` 把第 2 段压到原始 scale 以下 ⇒ 这里红");
            }

            Step(0.2f);                       // 淡入推完再读/再拍（同上）
            Check(driver.ReticleVisible, "远程也能出准星");
            // 🆕 2026-09-29：**换打法那一下的 scale punch**（原版 `SetAttackType` 里那次
            //   `DOPunchScale(原始scale × 0.2, 0.5s, vibrato 0, elasticity 1.0)`）。
            //   🔴 **2026-10-12（A354-a）就地订正（铁律 5）**：这条原来写着「判据用『punch 触发次数』，
            //   因为 punch 会**来回振荡再回到原位**」—— **那个前提是错的**：`vibrato = 0` 时
            //   `count` 被钳成 **2**（公式见上），轨迹是**单峰**、构造性**不会**来回穿零
            //   （同一族在 `Shell/RewardWindow` 那次也踩过：`资料/普查产出_1011/DIAG-B_Rewards十一条红.md` §二·#3）。
            //   ⇒ 曲线形状由上面那两条新断言管；这一条只留「**触发过**」（= 只在换打法那一下 punch，
            //   不是每次显示）—— 它对「弹成什么形状」无感，⚠️ 别再拿它当形状的判据。
            Check(driver.reticle.PunchCount >= 2,
                  $"★ 换打法（近战→远程）触发了 punch（累计 {driver.reticle.PunchCount} 次；" +
                  "原版只在 `SetAttackType` 里 punch，不是每次显示）");
            float rangedBulge = driver.reticle.ArcBulge;
            Check(rangedBulge < meleeBulge * 0.5f,
                  $"远程的弧线比近战平得多（{rangedBulge:F3} vs {meleeBulge:F3}）—— 原版 " +
                  "`curveProfileMelee` 0→1→0 对 `curveProfileRange` 0→0.099→0");
            var rc = driver.reticle.CrossColor;
            Check(rc.b > 0.8f && rc.r > 0.7f && rc.g < 0.5f,
                  $"远程 → 准星紫（{rc.r:F2},{rc.g:F2},{rc.b:F2}，原版 attackType=2）");
            Check(driver.FoeUnits[foeSlot].CurrentGem == TargetGem.Ranged,
                  "远程 → 点亮的是**远程**那颗数值格（原版 `Highlight ranged` / `40K_ranged_glow`）");
            Shot(cam, "10_选目标_远程准星");
            driver.SimulateDeselect();
            Check(driver.FoeUnits[foeSlot].CurrentGem == TargetGem.None,
                  "取消指挥 → 数值格底光也熄了");

            // ④ 主动技能：金色（原版 attackType=3/4）
            ctx.Players[0].Board[retAtk] =
                new UnitState(CardByName(StarterCards.Ember(), "Ironclad"), false) { Exhausted = false };
            driver.RefreshAll();
            if (driver.SimulateOpenCommand(retAtk) && driver.HasCommand(AttackKind.Ability))
            {
                driver.SimulateCommand(AttackKind.Ability);
                driver.SimulatePointerAt(driver.FoeUnits[foeSlot].transform.position);
                Check(driver.ReticleVisible, "技能（要选目标的那种）也出准星");
                var ac = driver.reticle.CrossColor;
                Check(ac.r > 0.8f && ac.g > 0.6f && ac.b < 0.4f,
                      $"技能 → 准星金（{ac.r:F2},{ac.g:F2},{ac.b:F2}，原版 attackType=3/4）");
            }
            else Debug.Log(P + "   （Ironclad 这轮放不出技能，跳过金色那条）");

            driver.SimulateDeselect();
            Check(!driver.ReticleVisible, "取消指挥 → 准星收起（原版 `StopTracking` / `CancelActionStates` 那一档）");
            Check(Cursor.visible, "★ 准星收起后**系统光标还回来**（原版 `set_visible(show ^ 1)` 的两个方向都对）");

            // 靶场还原
            ctx.Players[0].Board[retAtk] = null;
            for (int t = 0; t < BoardSpec.Size; t++) ctx.Players[1].Board[t] = foeBackup[t];
            driver.RefreshAll();
        }

        // ---- 🆕 2026-09-29：**AI 那条准星演出**（原版 `BattleManager.EnemyTargetingAnim`）----
        //   判据（2026-09-29 逐句读过那 61 行）：起点 = 施法者位置（**先瞬移过去**，`PrepareMovement`）·
        //   终点 = 目标的 2D 位置 · 时长 `VarsGlobal.targettingAnimTime`（偏移 **0x88**，实读 **0.5**）·
        //   `DoCrosshairMove` **不带 `SetEase`** ⇒ DOTween 默认 **OutQuad**（不是线性）· 演完才结算 ·
        //   **只有 AI 那侧有**（三个调用点的守卫都是「施法方 `isPlayer == false`」）。
        // 🔴 我们原来**完全没有**这条（`项目任务.md` §〇 第 9 条那条「另记一条」）—— 这一节把它钉死。
        Debug.Log(P + "--- AI 准星演出（原版 EnemyTargetingAnim）---");
        {
            // 🔴🔴 **必须先打开 `animateFeel`** —— `BattleDriver.animateFeel` **默认 `false`**
            //    （批处理自检要「当场精确」），而这条演出**只在真机路径上开**
            //    （它靠 `AdvanceTimeline` 泵推进；关着的时候 `StartAiTargetingAnim` 直接返回 false，
            //     于是这一节会全部走空、还看不出为什么）。同 `PlayDeathFeel` 那一节的规矩。
            bool feelWas = driver.animateFeel;
            driver.animateFeel = true;

            var foeBackup = new UnitState[BoardSpec.Size];
            for (int t = 0; t < BoardSpec.Size; t++)
            {
                foeBackup[t] = ctx.Players[1].Board[t];
                if (t != BoardSpec.WarlordSlot) ctx.Players[1].Board[t] = null;
            }
            const int foeAtkSlot = 1;
            int vicSlot = FreeSlot(ctx, 0);
            ctx.Players[1].Board[foeAtkSlot] =
                new UnitState(CardByName(StarterCards.Tide(), "Tide Minion"), false) { Exhausted = false };
            ctx.Players[0].Board[vicSlot] =
                new UnitState(CardByName(StarterCards.Ember(), "Veteran"), false);
            // 🔴 演出演完会**真的执行那条动作**（`ExecuteAction` 认 `ctx.Active`）⇒ 这一节要让 AI 当行动方，
            //    否则动作会以真人的身份执行、多半被引擎拒掉（那验的就不是这条链了）。用完还原。
            int activeWas = ctx.Active;
            ctx.Active = 1 - driver.MySeat;
            driver.RefreshAll();

            // ① 反例：目标在 **AI 自己那一侧** ⇒ 不演（原版那条 `IsAgainstHuman()` 守卫）
            Check(!driver.SimulateStartEnemyTargetingAnim(new AiAction
                  { Kind = AiActionKind.AttackMelee, Slot = foeAtkSlot, TargetP = 1, TargetSlot = foeAtkSlot }),
                  "★ 目标不是真人那一侧 ⇒ **不起演出**（原版三个调用点的守卫都是 `isPlayer == false`）");
            Check(!driver.AiTargetingAnimActive, "…而且什么都没留下（状态没被置上）");

            // ② 正面：打真人 ⇒ 起演出；准星先**瞬移**到施法者那儿（原版 `PrepareMovement` 是 `set_position`）
            var actA = new AiAction
            { Kind = AiActionKind.AttackMelee, Slot = foeAtkSlot, TargetP = 0, TargetSlot = vicSlot };
            Check(driver.SimulateStartEnemyTargetingAnim(actA), "目标在真人那一侧 ⇒ 起演出");
            Check(driver.ReticleVisible, "★ 演出把准星点亮（原版 `ToggleCrosshair(true, false)` 那一支）");
            Vector3 fromW = driver.FoeUnits[foeAtkSlot].transform.position;
            Vector3 p0;
            bool gotP0 = driver.reticle.CrossPosition(out p0);
            Check(gotP0 && Mathf.Abs(p0.x - fromW.x) < 0.05f && Mathf.Abs(p0.y - fromW.y) < 0.05f,
                  "★ 准星**起点 = 施法者**（原版 `PrepareMovement` 先瞬移过去，无补间；"
                + $"实测 ({p0.x:F2},{p0.y:F2}) vs 施法者 ({fromW.x:F2},{fromW.y:F2})）");
            var mc = driver.reticle.CrossColor;
            Check(mc.r > 0.8f && mc.g < 0.5f, $"近战 ⇒ 准星红（{mc.r:F2},{mc.g:F2},{mc.b:F2}）");

            // ③ 推进 1/4 时长 ⇒ 应当已经走了 **43.75%**（OutQuad = 1 − 0.75²）——
            //    **这一条就是「不是线性」的判据**（线性只会走 25%）。原版 `DoCrosshairMove` 没有 `SetEase`。
            Vector3 toW = driver.reticle.ResolveAim(driver.MyUnits[vicSlot].transform.position);
            driver.SimulateTickAiTargetingAnim(CardFeel.EnemyTargetingAnimTime * 0.25f);
            Vector3 p1;
            driver.reticle.CrossPosition(out p1);
            float span = Mathf.Abs(toW.x - p0.x) > 1e-3f ? Mathf.Abs(toW.x - p0.x) : Mathf.Abs(toW.y - p0.y);
            float moved = Mathf.Abs(toW.x - p0.x) > 1e-3f ? Mathf.Abs(p1.x - p0.x) : Mathf.Abs(p1.y - p0.y);
            float frac = moved / Mathf.Max(1e-4f, span);
            Check(frac > 0.35f && frac < 0.95f,
                  $"★ 走完 1/4 时长时已推进 **{frac:P1}**（OutQuad 理论 **43.75%**；**线性只有 25%**）"
                + " —— 原版 `DoCrosshairMove` 不带 `SetEase`，走的是 DOTween 默认缓动");

            // ④ 演完（0.5 s）⇒ 收尾：准星灭 + **那条动作真的执行了**（原版：演出 → 等 0.5 s → 结算）
            int hpBefore = ctx.Players[0].Board[vicSlot] != null ? ctx.Players[0].Board[vicSlot].Health : -1;
            driver.SimulateTickAiTargetingAnim(CardFeel.EnemyTargetingAnimTime);
            Check(!driver.AiTargetingAnimActive, "★ 演完（0.5 s）⇒ 演出状态清掉");
            Check(!driver.ReticleVisible, "…准星也收起（原版 `OnComplete → ToggleCrosshairOff`；⚠️ 那两个回调"
                                        + "在 `.c` 里解不出方法名 ⇒ **属推断**，见 `BattleDriver` 那段注释）");
            var vicAfter = ctx.Players[0].Board[vicSlot];
            var atkAfter = ctx.Players[1].Board[foeAtkSlot];
            bool ranAct = atkAfter == null || atkAfter.Exhausted
                       || vicAfter == null || vicAfter.Health != hpBefore;
            Check(ranAct, "★ 演完**那条动作真的执行了**（攻击方 Exhausted / 目标掉血或阵亡）"
                        + " —— 演出不是死路，演完必须接着结算");

            // 靶场还原（别把后面的用例饿着）
            ctx.Active = activeWas;
            driver.animateFeel = feelWas;               // 关回去（后面靠「当场精确」）
            for (int t = 0; t < BoardSpec.Size; t++) ctx.Players[1].Board[t] = foeBackup[t];
            ctx.Players[0].Board[vicSlot] = null;
            driver.RefreshAll();
        }

        // ---- 🆕 2026-09-29：**回手动画 = 倒放 `Card Hand To Board`**（原版 `PlayBackToHandAnimation`）----
        //   判据（读全文 68 行 + clip 关键帧）：`speed = −1`、`time = AnimationState.length`；
        //   反向时间轴上 —— 2D 卡面在 **0.2167** 打开、**[0.25, 0.5833]** 淡回 1；
        //   3D 体 `_DissolveAmount` 在 **[0.2167, 0.75]** 0→1；同时 `DOMove(up × localScale.x × 3.0,
        //   **0.208 s**, 线性)`。我们原来**当场把视图摘掉**（一帧动画都没有）。
        Debug.Log(P + "--- 回手动画（倒放 Card Hand To Board）---");
        {
            // 🔴 **必须先打开 `animateFeel`**（默认 `false` —— 批处理自检要「当场精确」，
            //    见 `BattleDriver.animateFeel`）。关着的时候 `PlayReturnFeel` 走的是「当场摘视图」那条，
            //    这条动画一帧都不会演（而且**断言失败之后还会去碰已销毁的视图** ⇒ 直接崩掉整条自检）。
            bool feelWas = driver.animateFeel;
            driver.animateFeel = true;

            var foeBackup = new UnitState[BoardSpec.Size];
            for (int t = 0; t < BoardSpec.Size; t++)
            {
                foeBackup[t] = ctx.Players[1].Board[t];
                if (t != BoardSpec.WarlordSlot) ctx.Players[1].Board[t] = null;
            }
            const int retSlot = 1;
            ctx.Players[1].Board[retSlot] = new UnitState(CardByName(StarterCards.Tide(), "Tide Minion"), false);
            driver.RefreshAll();
            var rv = driver.FoeUnits[retSlot];
            Check(rv != null, "（前提）先摆一张场上的卡");
            if (rv != null)
            {
                Vector3 p0 = rv.transform.position;
                float liftUp = rv.transform.localScale.x * CardFeel.ReturnLiftScale;

                // 引擎侧先把它从棋盘上拿走，再发那条 `Return`（＝原版 `ResolveRecallToHand` 的形状）
                ctx.Players[1].Board[retSlot] = null;
                ctx.Signals.Add(new BattleEvent { Kind = EvtKind.Return, Player = 1, Slot = retSlot });
                driver.RefreshAll();

                Check(driver.ReturningCount == 1,
                      "★ 回手 ⇒ 视图**不当场销毁**，进「正在回手」那一档（原版倒放一条 0.9167 s 的 clip）");
                Check(rv.OnDissolveMaterial || !rv.DissolveSupported,
                      $"★ 3D 卡体换上了**溶解材质**（原版 `SetCardMaterial(minion3DRenderer, dissolveMaterial)`；"
                    + $"支持溶解={rv.DissolveSupported}）");
                Check(!rv.ArtVisible, "t=0：2D 卡面还**没**回来（原版 `2DCard.m_IsActive` 反向 0.2167 才打开）");

                // 走到 0.35 s：溶解应当刚过 1/4 窗、2D 卡面刚开始淡回来
                Step(0.35f);
                if (rv == null)
                {
                    Check(false, "★ 回手走到 0.35 s 时视图**不该已经没了**（那条倒放要 0.9167 s）");
                }
                else
                {
                    Check(Mathf.Abs(rv.DissolveAmount - 0.25f) < 0.06f,
                          $"★ 3D 体 `_DissolveAmount` = **{rv.DissolveAmount:F3}**（反向 [0.2167, 0.75] 这一段，"
                        + "0.35 s 处理论 0.25）—— 原版是 clip 的材质曲线，我们按同一条时间轴喂值");
                    Check(rv.ArtVisible && Mathf.Abs(rv.ArtAlpha - 0.30f) < 0.08f,
                          $"★ 2D 卡面淡回来了（alpha **{rv.ArtAlpha:F2}**，反向 [0.25, 0.5833] 这一段，理论 0.30；"
                        + "原版 `2DCard` 的 CanvasGroup 那条曲线）");
                    Check(Mathf.Abs((rv.transform.position.y - p0.y) - liftUp) < 0.02f,
                          $"★ 抬升到位：+**{rv.transform.position.y - p0.y:F3}** 世界单位 = `localScale.x × 3.0`"
                        + $"（{rv.transform.localScale.x:F3} × 3.0 = {liftUp:F3}；原版 `DOMove(… + Vector3.up × …)`）");
                }

                // 再走完剩下的（总 0.9167 s）⇒ 视图销毁
                Step(0.70f);
                Check(driver.ReturningCount == 0, "★ 倒放走完（0.9167 s）⇒ 视图销毁（交给手牌那套重建）");
                Check(rv == null, "…而且那个视图**真的被销毁了**（不是只从名单里摘掉）");
            }

            for (int t = 0; t < BoardSpec.Size; t++) ctx.Players[1].Board[t] = foeBackup[t];
            driver.animateFeel = feelWas;               // 关回去（后面靠「当场精确」）
            driver.RefreshAll();
        }

        // ---- 🆕 2026-09-29：**状态框**（原版 `BattleCardUI` 两本字典那一套）----
        //   判据 → `Core/TraitFrames.cs` 的文件头（两本字典 · OnPlay(2) / Trigger(3)+TriggerOnPlay(6) 分工 ·
        //   框挂在卡的 `effectsAnchor` 下 · 「已有 ⇒ 不重播」· `Clean` 只清「已经不再持有的」）。
        Debug.Log(P + "--- 状态框（trait frame）---");
        {
            // 🔴 **必须先打开 `animateFeel`**：`EvtKind.Trigger` 那条链走的是 `PlayFeel`，
            //    而 `PlayFeel` 只在 `animateFeel` 打开时才调（`BattleDriver: if (animateFeel) PlayFeel(e);`）
            //    ⇒ 关着的时候「触发类那一本」永远不会被写到（2026-09-29 实测：`TriggerCount` 恒 0）。
            bool feelWas = driver.animateFeel;
            driver.animateFeel = true;

            var foeBackup = new UnitState[BoardSpec.Size];
            for (int t = 0; t < BoardSpec.Size; t++)
            {
                foeBackup[t] = ctx.Players[1].Board[t];
                if (t != BoardSpec.WarlordSlot) ctx.Players[1].Board[t] = null;
            }
            const int tfSlot = 1;
            ctx.Players[1].Board[tfSlot] =
                new UnitState(CardByName(StarterCards.Tide(), "Tide Minion"), false);
            driver.RefreshAll();
            var tv = driver.FoeUnits[tfSlot];
            Check(tv != null, "（前提）先摆一张场上的卡");
            if (tv != null)
            {
                var tf = tv.GetComponent<TraitFrames>();
                Check(tf != null && tf.Anchor != null, "★ 卡视图上挂了状态框那一套（含 `effectsAnchor`）");

                // ① 拿到一个**有绑定**的 trait（`stealth` → `StealthEffect`）⇒ 挂上一张框
                Check(TraitFrames.PrefabFor("stealth", false) != null, "（前提）`stealth` 有绑定的框 prefab");
                ctx.Players[1].Board[tfSlot].AddKeyword("stealth", 1);
                driver.RefreshAll();
                Check(tf != null && tf.HasPlayFrame("stealth"),
                      $"★ 关键词「stealth」加上来 ⇒ 那一本里有了它（现有 {tf.PlayCount} 张）");
                // ② 「已有 ⇒ 不重播」（原版 `DisplayStatusAnim` 那条已存在的分支）
                int before = tf.PlayCount;
                driver.RefreshAll();
                Check(tf.PlayCount == before, "★ 再同步一次不会重复挂（原版「已在字典里 ⇒ 只归位、不重播」）");
                // ③ 关键词没了 ⇒ `CleanStatusAnims(false)` 把它清掉
                //   ⚠️ **别整个换掉 `UnitState`**（`ctx.…Board[slot] = new UnitState(…)`）——
                //      `SyncBoard` 会因此**重建视图**，手里那个 `tf` 就变成指向**已销毁组件**的引用，
                //      后面几条断言会假红（2026-09-29 实测：`TriggerCount` 恒 0）。
                ctx.Players[1].Board[tfSlot].RemoveKeyword("stealth", 1);
                driver.RefreshAll();
                Check(tf.PlayCount == 0, "★ trait 不再持有 ⇒ 那张框被清掉（原版 `CleanStatusAnims` 的谓词）");

                // ③·b 🆕 2026-10-01：**`vanguard` 也接上了** —— 原版那个具名字段是 `vanguardFrame(+0x278)`
                //   （写方 `SetVanguardStealth`），它原来挂在 `MissingFrames` 里，卡点是那件 prefab
                //   **没进效果库**（不是 addressable）⇒ 靠下面这条导入路补进来：
                //   `工具/extract_missing_shaders.py --prefabs` → `EffectExporter.RunListed`
                //   → `EffectLibraryBuilder.Run`。判据 → `资料/已知的坑.md`。
                Check(TraitFrames.PrefabFor("vanguard", false) == "Vanguard Frame Animated VAT",
                      "★ `vanguard` 有绑定的框（按名字硬绑 —— 与 stealth/swarm 同一个口径，文件头如实标了）");
                Check(WarpforgeVFX.WarpforgeEffectLibrary.Available
                   && WarpforgeVFX.WarpforgeEffectLibrary.Instance.TryGet("Vanguard Frame Animated VAT", out _),
                      "★ `Vanguard Frame Animated VAT` **真的在效果库里**（这条就是那条导入路的验收）");
                ctx.Players[1].Board[tfSlot].AddKeyword("vanguard", 1);
                driver.RefreshAll();
                Check(tf != null && tf.HasPlayFrame("vanguard"),
                      $"★ 关键词「vanguard」加上来 ⇒ 那一本里有了它（现有 {tf.PlayCount} 张）");
                ctx.Players[1].Board[tfSlot].RemoveKeyword("vanguard", 1);
                driver.RefreshAll();
                Check(tf.PlayCount == 0, "★ vanguard 撤掉 ⇒ 那张框也被清掉（同一条谓词）");

                // ④ 触发类那一本：`EvtKind.Trigger` 带着关键词进来 ⇒ 挂上（原版 `DisplayTriggerAnim`）
                //   ⚠️ **那个关键词得真在这张卡上** —— 原版 `CleanStatusAnims` 会把「卡上已经不再持有的 trait」
                //      的框清掉，而且**两本字典走同一条谓词**（触发类那本也不例外）⇒ 拿一个卡上没有的词去演，
                //      下一帧同步就被清掉了（2026-09-29 实测：日志里「状态框上场」打了、`TriggerCount` 还是 0）。
                //   ⚠️ 也重新取一次视图/组件（上一步万一真重建过视图，旧引用就废了）
                ctx.Players[1].Board[tfSlot].AddKeyword("oath", 1);
                var tv2 = driver.FoeUnits[tfSlot];
                var tf2 = tv2 != null ? tv2.GetComponent<TraitFrames>() : null;
                Check(tf2 != null, "（前提）视图还在、状态框那一套还挂着");
                ctx.Signals.Add(new BattleEvent
                { Kind = EvtKind.Trigger, Player = 1, Slot = tfSlot, Keyword = "oath" });
                driver.RefreshAll();
                // ⚠️ **要推一下时间线** —— 触发那一条是**排期**播的（`EventTiming` 给它一个 hold），
                //    不像 `Return` 那样当场演；不推的话断言量到的是「还没轮到」。
                Step(0.6f);
                Check(tf2 != null && tf2.TriggerCount == 1,
                      $"★ 「oath」触发 ⇒ 触发类那一本里有了它（{(tf2 != null ? tf2.TriggerCount : -1)} 张）");
                // ⑤ 反例：没有绑定的关键词**不许**挂、也不许报错（原版没有那个框）
                ctx.Signals.Add(new BattleEvent
                { Kind = EvtKind.Trigger, Player = 1, Slot = tfSlot, Keyword = "flying" });
                driver.RefreshAll();
                Step(0.6f);
                Check(tf2 != null && tf2.TriggerCount == 1,
                      $"★ 没绑定的关键词（flying）⇒ 不挂框（还是 {tf2?.TriggerCount} 张；原版也没有这个框）");
                // ⑥ 🆕 2026-09-30：**`bloodthirst` 接上了**（原来挂在 `MissingFrames` 里）。
                //   判据 = 地址表 `数据/索引/anim_address_map.json` 的 **`BloodThirstTraitTrigger` → `BloodThirstEffect`**
                //   （`…TraitTrigger` 那一族；原版 trait id `0xdc`）。
                //   ⚠️ **同一批还改了引擎**：`RuleCore.EmitBloodThirst` 现在会在「带嗜血的单位本回合计数变成 1」
                //      那一刻发 `EvtKind.Trigger`（判据 = 原版 `CardScript.ActivateBloodThirst` 的 `+0x48 == 1`）
                //      —— 那一天之前**引擎从不发这个事件**，所以框永远上不了场。
                ctx.Players[1].Board[tfSlot].AddKeyword(KeywordTable.BloodThirst, 1);
                var tv3 = driver.FoeUnits[tfSlot];
                var tf3 = tv3 != null ? tv3.GetComponent<TraitFrames>() : null;
                Check(tf3 != null, "（前提）视图还在、状态框那一套还挂着");
                ctx.Signals.Add(new BattleEvent
                { Kind = EvtKind.Trigger, Player = 1, Slot = tfSlot, Keyword = KeywordTable.BloodThirst });
                driver.RefreshAll();
                Step(0.6f);
                Check(tf3 != null && tf3.TriggerCount == 2,
                      $"★ 「bloodthirst」触发 ⇒ 触发类那一本里有了它（{(tf3 != null ? tf3.TriggerCount : -1)} 张）"
                    + " —— 原版 `BloodThirstTraitTrigger → BloodThirstEffect`，2026-09-30 接上");
                ctx.Players[1].Board[tfSlot].RemoveKeyword(KeywordTable.BloodThirst, 1);
                driver.RefreshAll();
            }

            for (int t = 0; t < BoardSpec.Size; t++) ctx.Players[1].Board[t] = foeBackup[t];
            driver.animateFeel = feelWas;               // 关回去
            driver.RefreshAll();
        }

        // ---- 🆕 2026-09-29：**阵亡照原版重做**（原版 `CardScript.UnitDeath`）----
        //   判据：**不是把场上的卡溶解掉**（原文那条 2026-09-29 查实是错的）——
        //   ① 关掉卡的 3D 体（`body3D.SetActive(false)`）② 在卡位生成死亡爆散体
        //   ③ 卡抖一下（0.2/0.5 s、(0.3,0.05,0)、vibrato 10、randomness 90、fadeOut）。
        //   ✅ 2026-10-01 起 ② 那件 `Card 3D Death Explosion` **已经在效果库里了**
        //      （原来取不到：它不是 addressable，两条枚举路都拿不到；现在靠
        //       `工具/extract_missing_shaders.py --prefabs` 重打包 → `EffectExporter.RunListed`
        //       → `EffectLibraryBuilder.Run` 进来，判据 → `资料/已知的坑.md`）。
        //      ⇒ 这一节现在**连「真的生成了那个爆散体」一起判**（取不到时退回分支并如实出声）。
        Debug.Log(P + "--- 阵亡（原版 UnitDeath）---");
        {
            bool feelWas = driver.animateFeel;
            driver.animateFeel = true;

            var foeBackup = new UnitState[BoardSpec.Size];
            for (int t = 0; t < BoardSpec.Size; t++)
            {
                foeBackup[t] = ctx.Players[1].Board[t];
                if (t != BoardSpec.WarlordSlot) ctx.Players[1].Board[t] = null;
            }
            const int deadSlot = 1;
            ctx.Players[1].Board[deadSlot] =
                new UnitState(CardByName(StarterCards.Tide(), "Tide Minion"), false);
            driver.RefreshAll();
            var dv = driver.FoeUnits[deadSlot];
            Check(dv != null, "（前提）先摆一张场上的卡");

            bool bodyInLib = WarpforgeVFX.WarpforgeEffectLibrary.Available
                          && WarpforgeVFX.WarpforgeEffectLibrary.Instance.TryGet(CardFeel.DeathBodyFx, out _);

            ctx.Players[1].Board[deadSlot] = null;
            ctx.Signals.Add(new BattleEvent { Kind = EvtKind.Death, Player = 1, Slot = deadSlot });
            driver.RefreshAll();
            Check(driver.DyingCount == 1, "★ 阵亡那个视图进「正在消散」那一档（没被当场销毁）");
            if (dv != null && bodyInLib)
                Check(!dv.Body3DVisible,
                      "★ **场上卡的 3D 体被关掉**（原版 `body3D.SetActive(false)`）—— 阵亡不是把这张卡溶掉，"
                    + "是换成**卡位另生成的那个爆散体**（`Card 3D Death Explosion`）");
            else
                // 🔴 **A246：这两句原来会把原因说反** —— `dv == null`（视图没取到）那一档也会说
                //    「效果库里没有 `Card 3D Death Explosion`」。今天**不可达**（20 行前那条
                //    `Check(dv != null, "（前提）先摆一张场上的卡")` 已经先红了）⇒ **低危**
                //    ⇒ 只改文案、⛔ 不动逻辑（真取不到视图时上面那条 ★ 本来就该红）。
                Debug.Log(P + "   （" + (dv == null
                            ? "这张场上的卡**没取到视图**（上面「（前提）先摆一张场上的卡」那条已经红过）⇒ "
                            : "效果库里没有 `" + CardFeel.DeathBodyFx + "` ⇒ ")
                          + "走的是**退回分支**，这一条不判 3D 体；"
                          + "补它的两步见 `CardFeel.SpawnDeathBody` 的注释）");

            // 🆕 2026-10-01：**连「那件爆散体真的生成了」一起判** —— 这是「导入路打通了没有」的判据
            //   （2026-09-29 那会儿它取不到，这一条只能挂空）。
            // ⚠️ **A195 核过：这个门是安全的**（没改）—— `bodyInLib` 为假时**上面那个 `else` 已经出声**
            //    （「效果库里没有 `Card 3D Death Explosion` ⇒ 走的是退回分支…」），不是静默跳过。
            if (bodyInLib)
            {
                bool spawned = false;
                foreach (var ep in WarpforgeVFX.WarpforgeEffectPlayer.ActivePlayers)
                    if (ep != null && ep.EffectName == CardFeel.DeathBodyFx) spawned = true;
                Check(spawned, "★ 卡位真的生成了那个**死亡爆散体**（`Card 3D Death Explosion`，"
                             + "原版 `Instantiate(cardDestroyFX, 卡位置, 3D体旋转)`）—— "
                             + "2026-09-29 时它取不到、走的是退回分支");
            }

            // 🆕 2026-10-01 晚：**爆散体那个 Animator 的动画到底通没通** —— 三层判据，缺一层都能假绿。
            //
            // 背景：那件 prefab 的 `m_Controller` 原来指向**空 GUID**（bundle 资产落不了盘 ⇒ 运行时 null）。
            //   控制器本身现已按 `数据/游戏数据/animator_controllers.json` 建成工程资产；
            //   但**包里那份 clip 是 muscle/streamed 格式、曲线数据根本没打进包** ⇒ 工程那个 `.anim` 是空的，
            //   真正播的动画由 `WarpforgeAnimatorBridge` 在运行时从重打的小包里取**原件**换上
            //   （与原版 shader 同一条路子）。三条落盘路径的实测 → `AnimClipProbe.Run` 的日志头。
            {
                var animPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/WarpforgeVFX/Prefabs/" + CardFeel.DeathBodyFx + ".prefab");
                Check(animPrefab != null, $"（前提）`{CardFeel.DeathBodyFx}` 的 prefab 在工程里");
                if (animPrefab != null)
                {
                    var an0 = animPrefab.GetComponent<Animator>();
                    Check(an0 != null && an0.runtimeAnimatorController != null,
                          "★ 爆散体的 `Animator.m_Controller` **不是空**（原来这里是 guid 全 0 的伪引用）");
                    var abinder = animPrefab.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
                    Check(abinder != null && abinder.animatorController == "Card 3D WH40K Explosion",
                          "★ binder 上记着**原版控制器名**（运行时按它从 `wf_prefabs_extra.bundle` 取原件）");

                    // ⚠️ **`Apply()` 要调【实例上】那个 binder，不是 preab 资产上那个**（第一版写错过一次：
                    //   调资产上的 ⇒ 挂上去的是资产的 Animator，实例上那份还是工程里的空壳 clip
                    //   ⇒ 断言读到 `clip.length = 1`、动画也没驱动，看着像桥坏了）。
                    //   顺带：在资产上调 `Apply()` 会**把运行时材质/控制器写进 prefab 资产**（脏数据）。
                    var ainst = UnityEngine.Object.Instantiate(animPrefab);
                    var ainstBinder = ainst.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
                    if (ainstBinder != null) ainstBinder.Apply();
                    var an = ainst.GetComponent<Animator>();
                    var rc = an != null ? an.runtimeAnimatorController : null;
                    // 原版真值 = `AnimationClip.m_StopTime = 0.8166667`（`assets_full` 那份 JSON 实读）。
                    // ⚠️ 判据得用**这个数**才能分出「原件」与「工程那份空壳」—— **空壳的 length 是 1**（Unity 默认值）。
                    const float OrigLen = 0.8166667f;
                    float alen = rc != null && rc.animationClips.Length > 0 ? rc.animationClips[0].length : -1f;
                    Check(rc != null && rc.animationClips.Length == 1 && Mathf.Abs(alen - OrigLen) < 0.001f,
                          $"★ 运行时挂上的是**原版那份**控制器（clip `Card Explosion` 长 {alen:0.####} s = 原版 "
                          + $"`m_StopTime` {OrigLen:0.####}；工程里那个空壳是 1 s）");
                    // 真的在动：原版 clip 在 t=0 把子物体 `Minion Death` 关掉、≈0.1 s 又打开（探针实测）。
                    var md = ainst.transform.Find("Minion Death");
                    bool abefore = md != null && md.gameObject.activeSelf;
                    if (an != null) { an.Play(0, 0, 0f); an.Update(0f); }
                    bool aafter = md != null && md.gameObject.activeSelf;
                    Check(md != null && abefore != aafter,
                          $"★ 动画**真的驱动了这个 prefab**（`Minion Death` 的 activeSelf {abefore} → {aafter}）");
                    UnityEngine.Object.DestroyImmediate(ainst);
                }
            }
            Step(0.8f);
            Check(driver.DyingCount == 0, "…演完（小兵 0.2 s / 督军 0.5 s）视图销毁");

            // 🆕 2026-10-01：**连续棋盘上的阵亡** —— 补位上来的那张**绝不能**被当成「这一格空了」销毁。
            //   棋盘改成连续无洞之后（`RuleEngine/Core/BoardSlots.cs`），打死**贴督军那格**会让
            //   外侧那张**补位**进来 ⇒ 同一格里会先后站着两个人，而阵亡演出还要晚 0.85 s 才播。
            //   这一条钉的就是那一刻的两件事：① 活人那张**还是原来那张视图**（按身份搬格）；
            //   ② 死者那张**进「正在消散」那一档**（它的格号已经被顶掉了，不能再去 `views[slot]` 找它）。
            {
                for (int t = 0; t < BoardSpec.Size; t++)
                    if (t != BoardSpec.WarlordSlot) ctx.Players[1].Board[t] = null;
                var doomed = new UnitState(CardByName(StarterCards.Tide(), "Tide Minion"), false);
                var stay = new UnitState(CardByName(StarterCards.Tide(), "Reef Guard"), false);
                ctx.Players[1].Board[5] = doomed;      // 贴督军那格（右侧下标 0）
                ctx.Players[1].Board[6] = stay;        // 它外侧那一格
                driver.RefreshAll();
                var stayView = driver.FoeUnits.ContainsKey(6) ? driver.FoeUnits[6] : null;
                Check(stayView != null && driver.FoeUnits.ContainsKey(5), "（前提）5、6 号格各有一张视图");

                // 引擎那一侧发生的事：5 号格那张死了 ⇒ 6 号格那张**补位到 5**（`BoardSlots.RemoveAt`）
                ctx.Players[1].Board[6] = null;
                ctx.Players[1].Board[5] = stay;
                ctx.Signals.Add(new BattleEvent { Kind = EvtKind.Death, Player = 1, Slot = 5 });
                driver.RefreshAll();

                Check(driver.FoeUnits.ContainsKey(5) && driver.FoeUnits[5] == stayView,
                      "★ **补位上来那张还是原来那张视图**（按身份搬格，不是销毁重建）");
                Check(!driver.FoeUnits.ContainsKey(6), "6 号格的键让出来了（没有幽灵挂在旧格上）");
                // ⚠️ 阵亡演出走哪一档看**时间线**：`Death` 前面没有别的事件时延迟 0 ⇒ 这一帧就播
                //    （进 `DyingCount`）；前面排着别的（攻击→命中→阵亡）时它会先被摘进 `_fading`
                //    —— 两种都算对，这里按**这条夹具实际走的那一档**判。
                Check(driver.DyingCount == 1 || driver.FadingCount == 1,
                      "★ 被打死那张**进「等演出 / 正在消散」那一档** —— "
                    + $"它的格号被补位的人顶掉了，照样找得到它（消散 {driver.DyingCount} / 待演 {driver.FadingCount}）");
                Step(0.05f);      // ⚠️ 别推太久：小兵消散只要 0.2 s，推过头就看不到它在消散档里了
                Check(driver.DyingCount == 1, "★ （阵亡事件轮到播）它在「正在消散」那一档里");
                Check(driver.FoeUnits.ContainsKey(5) && driver.FoeUnits[5] == stayView,
                      "…补位那张**仍然活着**（没被当成它溶掉）");
                Step(0.8f);
                Check(driver.DyingCount == 0, "…演完销毁（补位那一张不受影响）");
            }

            for (int t = 0; t < BoardSpec.Size; t++) ctx.Players[1].Board[t] = foeBackup[t];
            driver.animateFeel = feelWas;
            driver.RefreshAll();
        }

        // ---- 🆕 2026-10-01：**trait 的 FromCode 粒子**（原版 `CardScript.ActivateTraitParticlesFromCode`）----
        //   判据（8 个调用点 + trait id 从指令流实读）→ `资料/待办判据_战场与战斗视图.md` 的「trait 粒子」段；
        //   绑定表与「和那两本字典不是一条链」的说明 → `Core/TraitParticles.cs` 的文件头。
        Debug.Log(P + "--- trait 的 FromCode 粒子 ---");
        {
            // ① 绑定表里**每一件**都必须在效果库里（名字写错 / 没导出 = 静默不播，这条就是防它的）
            int bound = 0, missing = 0;
            foreach (var t in TraitParticles.Traits)
            {
                bound++;
                var n = TraitParticles.PrefabFor(t);
                bool ok = WarpforgeVFX.WarpforgeEffectLibrary.Available
                       && WarpforgeVFX.WarpforgeEffectLibrary.Instance.TryGet(n, out _);
                if (!ok) { missing++; Debug.LogWarning(P + $"   🔴 trait `{t}` 绑的 `{n}` **不在效果库里**"); }
            }
            Check(bound >= 7 && missing == 0,
                  $"★ FromCode 绑定表 {bound} 条，**每一件的 prefab 都在效果库里**（不在的 {missing} 件）");
            Check(TraitParticles.PrefabFor("huntmark") == null,
                  "★ `huntMark` **故意留白**（那条调用点是 `…InTarget`，而映射表里没有 `…InTarget` 结尾的 CardAnim "
                + "⇒ 按铁律 3 宁可留白、不猜；判据 → `Core/TraitParticles.cs`）");

            // ③ 🆕 2026-10-01（§三 第 26 条 · ⑥ 的「接线那半」）：**`AnimFXModuleTween` 那条链接上了**。
            //   判据 → `Core/UnitTweenTable.cs` 文件头（逐行读的 `UnitTweenSO.BuildSequence` +
            //   `AnimFXModuleTween.PlayAnimCoroutine` + 六个子类的 `GetTween`）。
            //   原来 `WFModuleTween.OnInvoke` **全仓没人赋值** ⇒ 138 个模块的请求全落在 `DroppedRequests` 里。
            {
                Check(CardPresentation.UnitTweenRuntime.Installed,
                      "★ 补间钩子 `WFModuleTween.OnInvoke` **挂上了**（原来全仓没人赋值 ⇒ 138 个模块全部空转）");
                Check(CardPresentation.UnitTweenTable.Loaded && CardPresentation.UnitTweenTable.Count >= 24,
                      $"★ 补间表载进来了：{CardPresentation.UnitTweenTable.Count} 串"
                    + $"（`Resources/UnitTweens.json` ← `工具/gen_unit_tweens.py`；"
                    + $"工程里被 `tweenAnims` 引用的是 24 个，覆盖自检 = `工具/_verify_unit_tweens.py`）"
                    + (CardPresentation.UnitTweenTable.Loaded ? "" : " ⚠️ " + CardPresentation.UnitTweenTable.LastError));
                Check(WarpforgeVFX.WFModuleTween.CardResolver != null,
                      "★ 补间模块的卡上下文钩子挂上了（原版 `BuildSequence(tweenAnims[i], actingCard, targetCard)`）");

                // 🆕 2026-10-01（§三 第 9 条 · ①）**粒子碰撞平面那 7 个替身**（原版 `BattleParticleColliderManager`）：
                //   判据 = `资料/AnimFX_实现与接线.md` §11.6 d)（位置）+ 2026-10-01 补读的旋转
                //   （`07_场景/battlearena1/Transform/*.json`）。量的是**渲染/几何真值**，不是拿我自己的常量自证。
                Check(WarpforgeVFX.WFModuleCollisions.ColliderLookup != null,
                      "★ 碰撞平面的钩子 `WFModuleCollisions.ColliderLookup` 挂上了（原来恒 null ⇒ 平面一条也加不上）");
                {
                    Vector3 pp, pu, ep, eu;
                    bool okP = driver.ParticleColliderAt(5, out pp, out pu);      // Player
                    bool okE = driver.ParticleColliderAt(10, out ep, out eu);     // Enemy
                    Check(okP && okE, $"★ 两个替身物体按 id 查得到（Player=5 {okP} / Enemy=10 {okE}）");
                    // 两条兵线的距离 —— 用**战场真值**（`ArenaSlots` 那一排的 z）当独立参照，不用桥自己的中间量
                    float want = Mathf.Abs(ArenaSlots.Position(BoardLayout.WarlordSlot + 1, true).z
                                         - ArenaSlots.Position(BoardLayout.WarlordSlot + 1, false).z);
                    Check(Mathf.Abs((ep - pp).magnitude - want) < 0.02f,
                          $"★ 两条兵线之间的距离对得上（战场真值 {want:F3} / 两个平面之间 {(ep - pp).magnitude:F3}）");
                    Vector3 fp, fu; driver.ParticleColliderAt(0, out fp, out fu);
                    Check(Vector3.Angle(fu, Vector3.up) < 1f,
                          $"★ `Floor` 那面是**水平**的（法线 +Y；实测夹角 {Vector3.Angle(fu, Vector3.up):F1}°）");
                    Check(Vector3.Angle(pu, ep - pp) < 1f,
                          $"★ 我方那面朝**敌方**（法线 +forward；实测 {Vector3.Angle(pu, ep - pp):F1}°）");
                    Check(Vector3.Angle(eu, pp - ep) < 1f,
                          $"★ 敌方那面朝**我方**（法线 −forward；实测 {Vector3.Angle(eu, pp - ep):F1}°）");
                    Vector3 w1p, w1u; driver.ParticleColliderAt(8, out w1p, out w1u);   // PWF
                    Check(Vector3.Angle(w1u, pp - ep) < 1f,
                          "★ `PWF`（From Camera）**与敌方同朝向**（原版那两行记录都这么说：它是用途名、不是算法）");
                    Vector3 g15p, g15u; driver.ParticleColliderAt(15, out g15p, out g15u);
                    Check(Mathf.Abs(g15p.z - (pp.z + (ep.z - pp.z) * (1.463f / 1.621f))) < 0.02f,
                          "★ `GenericTarget` 落在那条比值线上（1.463 / 1.621 —— 与 `EnemyWarlord` 同一档）");

                    // 🔴 **2026-10-12（A368）**：**同一个「兵线」还有第二个消费方** —— 锥角修正那一跳
                    //   （`WFModuleScaleByTarget.MinionLines`）。它原来**写死 2D**（`playerBoard.SlotPosition`），
                    //   而消费方 `ChangeShapeAngle` 算的是**比值** `lineD ÷ cardD`，其中两张卡的位置是
                    //   **战场世界坐标** ⇒ 3D 下兵线与卡**不在同一个世界系**、`lineD` 一路偏小（差 3.4 倍，静默）。
                    //   ⇒ 这条问的是**模块实际用的那个委托**（`MinionLinesForTest` 转发 `TryMinionLines`，
                    //   不是在这儿重写一遍判据）；期望值 `want` 就是上面那条用的**战场真值**（同一处口径，铁律 6）。
                    //   🧨 **改坏法**：把 `BattleDriver.cs` 的 `MinionLines = TryMinionLines;` 换回旧 lambda
                    //   （只取 HUD 正交平面那两个点）⇒ 实得 **2.241**、与 `want` 差 **5.457** ⇒ 任何容差都分得开。
                    //   ⚠️ 这一条只在**建了 3D 战场**的宿主里成立（`CardBaseDemo` 那类宿主恒走 2D 支，⛔ 别抄过去）。
                    {
                        Vector3 mlA, mlB;
                        bool mlOk = driver.MinionLinesForTest(out mlA, out mlB);
                        float mlD = Vector3.Distance(mlA, mlB);
                        Check(mlOk && Mathf.Abs(mlD - want) < 1e-3f,
                              $"★ A368：锥角那一跳拿到的兵线是**战场世界系**那两个点"
                            + $"（期望 {want:F3} = |EnemyZ − PlayerZ|；实得 {mlD:F3}）"
                            + $" —— 退回 2D 的 2.241（HUD 正交平面，差 {want - 2.241f:F3}）就红"
                            + $"（3D 相机 {(driver.boardCam != null ? "在" : "**不在**")}）");
                    }
                }

                // 🆕 2026-10-01（§三 第 9 条 · ⑤）**灵石吸附**：目标 = 那一侧的灵石图标（HUD 空间），
                //   粒子在棋盘相机的空间里 ⇒ 中间必须过 `ConvertPositionBetweenCameras` 那座桥。
                Check(WarpforgeVFX.WFModuleMoveParticlesToTarget.ResolveTarget != null
                   && WarpforgeVFX.WFModuleMoveParticlesToTarget.FromCamera != null
                   && WarpforgeVFX.WFModuleMoveParticlesToTarget.ToCamera != null,
                      "★ 灵石吸附那三个钩子（目标 / 两台相机）都接上了");
                {
                    var stone = driver.StoneIconForTest(true);
                    Check(stone != null, "（前提）我方灵石图标在位（吸附目标就是它）");
                    Vector3 bp = stone != null ? stone.position : Vector3.zero;
                    bool conv = stone != null && WarpforgeVFX.WFModuleMoveParticlesToTarget
                                     .TryConvertFromHud(stone.position, -8f, out bp);
                    Check(conv, "★ 相机换算那条式子跑得通（`ConvertPositionBetweenCameras`）");
                    if (conv)
                    {
                        // **定义性质**：换算前（HUD 相机）与换算后（棋盘相机）**视口坐标一致** —— 这才是
                        //   「屏幕上看着是往那枚灵石吸」；只验「不抛异常」是没用的。
                        var v0 = driver.cam.WorldToViewportPoint(stone.position);
                        var v1 = driver.boardCam.WorldToViewportPoint(bp);
                        Check(Mathf.Abs(v0.x - v1.x) < 0.002f && Mathf.Abs(v0.y - v1.y) < 0.002f,
                              $"★ 换算后**在屏幕上指着同一处**（视口差 {Mathf.Abs(v0.x - v1.x):F4}, {Mathf.Abs(v0.y - v1.y):F4}）");
                    }
                }
                // 🆕 2026-10-01（§三 第 31 条 · 第 2 件）：**相邻特效**
                //   （`AnimFXInstanceParticleAdjacent`，全库 **1 实例 / 1 效果**）。
                // 🔴 判据链（硬判据，不是自证）：`BattleManager__GetAnimTransform.c` 的 `case 1/2`
                //    取**第二个**传入的 Transform（`case 0` 取第一个），而这份 CardAnim 是
                //    `startPosOption = endPosOption = 1`（`Deathspinner Slice Card Target.json`）
                //    ⇒ **起终点都是那个相邻单位** ⇒ 挂在每个相邻单位身上、不做位移
                //    （`shouldMoveVFX = 0` 与 `timeAtStartPos = 0.5` 自洽）。
                // 这里量两件事：① 那条「cardAnim GUID → prefab 名」**真的落进库了**；
                //              ② 「挑哪些单位」按 `BoardSpec.AdjacentSlots` **真的挑对了**。
                Check(WarpforgeVFX.WFModuleInstanceParticleAdjacent.OnExecute != null,
                      "★ 相邻特效的钩子 `WFModuleInstanceParticleAdjacent.OnExecute` 挂上了"
                    + "（原来恒 null ⇒ 每次只 LogWarning + 计数，一个单位都不播）");
                {
                    const string adjFx = "BulletImpact_deathspinner_arc_alt";
                    WarpforgeVFX.WFEffectEntry ent = null;
                    bool hasAdj = WarpforgeVFX.WarpforgeEffectLibrary.Available
                               && WarpforgeVFX.WarpforgeEffectLibrary.Instance.TryGet(adjFx, out ent);
                    Check(hasAdj, $"（前提）库里有效果 `{adjFx}`");
                    string pn = null;
                    if (hasAdj && ent.modules != null)
                        foreach (var md in ent.modules)
                            if (md != null && md.kind == "AnimFXInstanceParticleAdjacent")
                                pn = md.GetString("prefabName");
                    Check(pn == "Deathspinner Cut Effect",
                          "★ `cardAnim.m_AssetGUID` 换出的 prefab 名**进了库**（实测 `" + pn + "`）"
                        + " —— 判据 = `数据/索引/anim_address_map.json` 的 `cardanim_guid_to_name`"
                        + "（这一跳是 2026-10-01 补的，原来记成「要建导入路」）");
                    Check(WarpforgeVFX.WarpforgeEffectLibrary.Available
                       && WarpforgeVFX.WarpforgeEffectLibrary.Instance.TryGet("Deathspinner Cut Effect", out _),
                          "★ 要播的那件在库里（`Deathspinner Cut Effect`）—— 缺的从来不是资产、是这一跳");

                    // ② 「挑哪些单位」：**拿棋盘自己当独立参照**，不看实现里的中间量
                    var wantAdj = new List<CardView>();
                    var gotAdj = new List<CardView>();
                    var probeAdj = new List<int>();
                    int nProbe = 0;
                    for (int s = 0; s < BoardSpec.Size && nProbe < 3; s++)
                    {
                        for (int side = 0; side < 2 && nProbe < 3; side++)
                        {
                            bool mine = side == 0;
                            if (driver.BoardViewAt(s, mine) == null) continue;
                            nProbe++;
                            wantAdj.Clear();
                            BoardSpec.AdjacentSlots(s, probeAdj);
                            foreach (int a in probeAdj)
                            {
                                var w = driver.BoardViewAt(a, mine);
                                if (w != null) wantAdj.Add(w);
                            }
                            driver.AdjacentUnitViews(mine, s, gotAdj);
                            bool sameAdj = gotAdj.Count == wantAdj.Count;
                            for (int i = 0; sameAdj && i < wantAdj.Count; i++)
                                if (gotAdj[i] != wantAdj[i]) sameAdj = false;
                            Check(sameAdj, "★ 槽 " + s + "（" + (mine ? "我" : "敌") + "）的相邻单位挑对了："
                                         + "实现 " + gotAdj.Count + " 个 / 独立算 " + wantAdj.Count + " 个");
                        }
                    }
                    Check(nProbe > 0, "（前提）棋盘上至少有一个单位可供探测");
                }

                // 🆕 2026-10-01（§三 第 31 条 · 第 1 件）：**只被模块字段引用的那 3 张材质进 binder 了**。
                //   判据链 → `资料/普查产出_1001/资产导入路三件_侦察.md` §①，其中最关键的一条是探针实测的：
                //   🔴 **本 build 的 `AssetBundle.LoadAsset<Material>(名字)` 恒为 null**
                //      （`GetAllAssetNames()` 吐的是**容器键**不是资产名）⇒ 必须按键或按 `LoadAllAssets` 取。
                //   这里量两件事：① 那张 `WFMatDef` 真在 binder 里；② **它的 shader 真解析得出来**
                //   （造 Material 造得出来才算落地 —— 只验「名字在不在」证明不了这一点）。
                {
                    var wantMats = new (string fx, string mat)[]
                    {
                        ("AmbushEffect", "Card 3d Dissolve Blend Image Ambush"),
                        ("StealthEffect", "Card 3d Stealth"),
                        ("VanguardIdleEffect", "Vanguard_Frame VAT Dissolve"),
                    };
                    foreach (var w in wantMats)
                    {
                        var pf = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                            "Assets/WarpforgeVFX/Prefabs/" + w.fx + ".prefab");
                        Check(pf != null, "（前提）有 `" + w.fx + ".prefab`");
                        var bd = pf != null ? pf.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>() : null;
                        WarpforgeVFX.WFMatDef hit = null;
                        if (bd != null && bd.materials != null)
                            foreach (var d in bd.materials)
                                if (d != null && d.name == w.mat) hit = d;
                        Check(hit != null, "★ `" + w.fx + "` 的 binder 里有 `" + w.mat + "`"
                                         + "（这张原来只被模块字段引用 ⇒ 挂在渲染器上看不见 ⇒ 从没过导出器）");
                        if (hit != null)
                        {
                            var built = WarpforgeVFX.WarpforgeEffectBinder.BuildForProbe(hit);
                            Check(built != null, "★ `" + w.mat + "` **造得出来**（原 shader `" + hit.shader
                                               + "` 解析得到）—— 只验「名字在不在」证明不了这一条");
                        }
                    }
                }
                // 🆕 2026-10-01（§三 第 31 条 · 第 1 件 · **卡材质那一半**）：三个回调挂上了、
                //   而且**真的换得动**。判据（原版逐行读过）：`BattleCardUI__SetCardMaterial.c` /
                //   `…__RestoreOriginalMaterial.c` —— 全在**卡 3D 体那一个 Renderer** 上（原版 `+0x180`）。
                //   ⚠️ 只验「钩子非 null」证明不了任何事 ⇒ 这里**拿一张真卡走一遍**：
                //   换上去 → 断言换了；恢复 → 断言恢复原样。
                Check(WarpforgeVFX.WFModuleChangeMaterial.SetCardMaterial != null
                   && WarpforgeVFX.WFModuleChangeMaterial.RestoreOriginalMaterial != null
                   && WarpforgeVFX.WFModuleChangeMaterial.CardTexture != null,
                      "★ 换材质的三个回调（`SetCardMaterial` / `RestoreOriginalMaterial` / `CardTexture`）都挂上了");
                {
                    CardView probeCard = null;
                    for (int s = 0; s < BoardSpec.Size && probeCard == null; s++)
                    {
                        probeCard = driver.BoardViewAt(s, true) ?? driver.BoardViewAt(s, false);
                    }
                    Check(probeCard != null, "（前提）棋盘上有一张卡可用来试换材质");
                    if (probeCard != null)
                    {
                        var t = probeCard.transform;
                        var before = probeCard.Body3DMaterial;
                        Check(before != null, "（前提）那张卡有 3D 卡体材质（平面模式 / 未建体就没有）");
                        var tex = WarpforgeVFX.WFModuleChangeMaterial.CardTexture(t);
                        Check(tex != null, "★ `CardTexture` 取得到那张卡的立绘（原版 `rawCard.cardSprite.texture`）");
                        var probe = new Material(before != null ? before.shader : null);
                        // 原版那条路的形状：`SetCardMaterial` 返回「实际用的那份材质」
                        var back = WarpforgeVFX.WFModuleChangeMaterial.SetCardMaterial(t, probe, false);
                        Check(back == probe && probeCard.Body3DMaterial == probe,
                              "★ `SetCardMaterial` 真把 3D 卡体换成了给的那份（原版 `Renderer.SetMaterial`）");
                        Check(!probeCard.OnOriginalBodyMaterial, "★ 换完确实**不是**原来那份了");
                        WarpforgeVFX.WFModuleChangeMaterial.RestoreOriginalMaterial(t);
                        Check(probeCard.OnOriginalBodyMaterial,
                              "★ `RestoreOriginalMaterial` 换回了建体时那份（原版 `+0x2c8` 那份的对应物）");
                        UnityEngine.Object.DestroyImmediate(probe);
                    }
                }

                // 🆕 2026-10-01（§三 第 31 条 · 第 3 件）：**后期（LUT / Bloom）下游**。
                //   判据 → `CardPresentation/Battle/BattlePostFx.cs` 文件头 + 侦察正本 §③
                //          （shader 算式是**反汇编**读出来的：`o = lerp(_LUT1,_LUT2,_Blend)`）。
                //   量四件事：① 钩子挂上；② 链就绪；③ **14 张 LUT 都按名字取得到**（模块给下游的是名字）；
                //   ④ **真跑一遍 Blit 并把像素读回来** —— 只验「接上了」证明不了公式对。
                Check(WarpforgeVFX.WFModulePostProcess.OnPostFx != null,
                      "★ 后期钩子 `WFModulePostProcess.OnPostFx` 挂上了（原来 0 订阅者 ⇒ 52 个效果整段不生效）");
                var pfx = driver.PostFxForTest;
                Check(pfx != null && pfx.Ready, "★ 后期下游就绪（LUTBlender 材质 + `ColorLookup` 都在位）");
                Check(WarpforgeVFX.WarpforgeShaderMap.TryResolve("Hidden/LUTBlender", out _, out _),
                      "★ `Hidden/LUTBlender` 解析得到（映射到自建的 `WarpforgeVFX/LUTBlender`）");
                if (pfx != null && pfx.Ready)
                {
                    // ③ 14 张 LUT：模块传到下游的是**资产名**字符串 ⇒ 必须落在 `Resources/` 下才取得到
                    //    （导入器 `工具/import_original_luts.py`；名单是数据驱动的）
                    string[] lutNames = { "LUT Red Tint", "LUT Dimensional Breach", "LUT Red Hell",
                        "LUT Pink Emperors Children", "LUT Overexpose High", "LUT Blizzard", "LUT Blind",
                        "LUT_Dark", "LUT Blue Tint", "LUT Nuclear", "LUT Invert", "LUT Poster",
                        "LUT Pink Emperors Children Extreme", "LUT Normal" };
                    int missLut = 0; string firstMiss = null;
                    foreach (var n in lutNames)
                        if (Resources.Load<Texture2D>("WarpforgeVFX/LUT/" + n) == null)
                        { missLut++; if (firstMiss == null) firstMiss = n; }
                    Check(missLut == 0, "★ 14 张 LUT 都按名字取得到（缺 " + missLut + " 张"
                                      + (firstMiss != null ? "，第一张 `" + firstMiss + "`" : "") + "）");

                    // ④ 真跑：同一张 RT、同一个像素，分别在 `_Blend` = 0 / 1 / 0.25 下读回来。
                    //    🔴 **判据是「单调关系」而不是「逐值相等」** —— RT 上可能有一次 sRGB 写转换，
                    //       逐值比会假红；而 sRGB 是**单调**的 ⇒ 「0.25 的结果落在 A、B 之间、且更靠近 A」
                    //       这个关系在转换前后**都成立**（这正是我们要证明的 `lerp` 语义）。
                    var req = new WarpforgeVFX.PostFxRequest
                    {
                        op = WarpforgeVFX.PostFxOp.LutBlend,
                        setTextures = true,
                        lut1 = "LUT Red Tint",
                        lut2 = "LUT Invert",
                        mergeLuts = false,
                    };
                    pfx.Handle(req); pfx.BlendTo(0f);
                    var A = ReadRtPixel(pfx.Combined, 128, 8);
                    pfx.BlendTo(1f);
                    var B = ReadRtPixel(pfx.Combined, 128, 8);
                    pfx.BlendTo(0.25f);
                    var M = ReadRtPixel(pfx.Combined, 128, 8);
                    Check(Mathf.Abs(A.r - B.r) + Mathf.Abs(A.g - B.g) + Mathf.Abs(A.b - B.b) > 0.05f,
                          "（前提）两张 LUT 在那个像素上**确实不一样**（否则这条断言什么都没验）");
                    Check(Between3(M, A, B, 0.02f),
                          $"★ `_Blend=0.25` 的结果落在 A 与 B 之间（A={A.r:F3},{A.g:F3},{A.b:F3} "
                        + $"B={B.r:F3},{B.g:F3},{B.b:F3} M={M.r:F3},{M.g:F3},{M.b:F3}）—— 这就是 `lerp` 的语义");
                    Check(CloserTo(M, A, B),
                          "★ 而且**更靠近 A**（0.25 < 0.5）—— 「落在中间」还不够，这条才排得掉「取平均/乱插」");
                    Check(pfx.ColorLookupRef != null
                       && ReferenceEquals(pfx.ColorLookupRef.texture.value, pfx.Combined),
                          "★ `ColorLookup.texture` 真的指向了那张合并 RT（原版 `LUTBlender.DoBlend` 的最后一步）");
                    // Reset：交还给**进场时那张静态 LUT**（原版 `PostFXController.ResetLUT`）
                    pfx.Handle(new WarpforgeVFX.PostFxRequest { op = WarpforgeVFX.PostFxOp.Reset, instant = true });
                    Check(ReferenceEquals(pfx.ColorLookupRef.texture.value, pfx.OriginalLut),
                          "★ `Reset` 把 `ColorLookup.texture` 交还给了进场时那张（= 该战场的静态 LUT）");
                    Check(pfx.MissingLutTextures.Count == 0,
                          "★ 刚才那条链**一张 LUT 都没缺**（缺了会记名字，只有真缺才非空）");
                }
                // 真建一串出来（判据是「建得出来」而不是「名字查得到」—— 后者只证明表在）
                var e0 = CardPresentation.UnitTweenTable.Get("AeldariRecallTween");
                Check(e0 != null && e0.tweens.Length == 2,
                      $"★ 抽一串来建：`AeldariRecallTween` 有 {e0?.tweens.Length} 条补间（表里应是 2）");
                var seq0 = CardPresentation.UnitTweenTable.Build(e0, driver.transform, driver.transform, "自检");
                // ⚠️ 这里**不 `using DG.Tweening`**（本文件用不着，加了怕撞名字）⇒ 扩展方法走全名
                float seqDur = seq0 != null ? DG.Tweening.TweenExtensions.Duration(seq0) : -1f;
                Check(seq0 != null && seqDur > 0f,
                      $"★ **真的建出了 DOTween 序列**（时长 {seqDur:F2}s —— 原版这串是 1.0+0.1 两条）");
                if (seq0 != null) DG.Tweening.TweenExtensions.Kill(seq0, false);
                // 反例：表里没有的名字 ⇒ **建不出来**（不是静默给个空序列）
                Check(CardPresentation.UnitTweenTable.Build(null, driver.transform, driver.transform, "自检") == null,
                      "★ 反例：空的 entry ⇒ 建不出来（返回 null，调用方出声）");
                Check(CardPresentation.UnitTweenTable.Get("这个补间不存在_自检用") == null,
                      "★ 反例：表里没有的名字 ⇒ `Get` 返回 null（调用方会**出声**跳过，不静默）");
            }

            // ② 真的播得出来：摆一张带 `ferocity` 的卡，发一条 `EvtKind.Ability`
            //   （原版 `CardScript__UsedActiveAbility.c:64` = `ActivateTraitParticlesFromCode(self, 0x4f1, self, 0)`）
            bool feelWas = driver.animateFeel;
            driver.animateFeel = true;
            var foeBackup = new UnitState[BoardSpec.Size];
            for (int t = 0; t < BoardSpec.Size; t++)
            {
                foeBackup[t] = ctx.Players[1].Board[t];
                if (t != BoardSpec.WarlordSlot) ctx.Players[1].Board[t] = null;
            }
            const int tpSlot = 2;
            ctx.Players[1].Board[tpSlot] = new UnitState(CardByName(StarterCards.Tide(), "Tide Minion"), false);
            ctx.Players[1].Board[tpSlot].AddKeyword(KeywordTable.Ferocity, 1);
            driver.RefreshAll();
            ctx.Signals.Add(new BattleEvent { Kind = EvtKind.Ability, Player = 1, Slot = tpSlot });
            driver.RefreshAll();
            bool spun = false;
            foreach (var ep in WarpforgeVFX.WarpforgeEffectPlayer.ActivePlayers)
                if (ep != null && ep.EffectName == "FerocityEffect") spun = true;
            Check(spun, "★ 带 `ferocity` 的单位发动主动能力 ⇒ 卡上播了它的 FromCode 粒子（`FerocityEffect` —— "
                      + "原版 `UsedActiveAbility` 里点名调的那一条）");

            for (int t = 0; t < BoardSpec.Size; t++) ctx.Players[1].Board[t] = foeBackup[t];
            driver.animateFeel = feelWas;
            driver.RefreshAll();
        }

        // ---- 🆕 替代行动（原版 `Duty` / `Pray` / `Ferocity` / `Agenda`）在**界面上**真的能用 ----
        //   2026-09-13 A2：这四个关键词以前既没有引擎实现、也没有入口（只有我们自定的 `Ability:`）；
        //   现在引擎逐关键词做完（`RuleCore.UseAlternative`），界面上**就是那一格主动技能按钮**
        //   （原版本来也只有一格 —— 这四个在原版就是单位的主动能力）。
        Debug.Log(P + "--- 替代行动（议程 …）走界面这条路 ---");
        {
            var cAlt = driver.Ctx;
            var bikes = CardByName(CardDatabase.Load(), "Ravenwing Bikes");
            Check(bikes != null && bikes.TriggerOps("agenda") != null,
                  "卡池里有 `Ravenwing Bikes`，它的 `Agenda:` 正文收得下来");
            if (bikes != null)
            {
                int slotA = 2;
                var backup = cAlt.Players[0].Board[slotA];
                cAlt.Players[0].Board[slotA] = new UnitState(bikes, false) { Exhausted = false };
                driver.RefreshAll();

                driver.SimulateOpenCommand(slotA);
                Check(driver.HasCommand(AttackKind.Ability),
                      "★ 带 `Agenda` 的单位**给出了主动技能那一格**"
                      + "（没有引擎实现的话这里根本不会亮）");
                int questBefore = cAlt.Players[0].QuestPoints;
                driver.SimulateCommand(AttackKind.Ability);
                Check(cAlt.Players[0].QuestPoints == questBefore + 1,
                      "★ 点下去**真的结算了议程的正文**"
                      + $"（任务点 {questBefore} → {cAlt.Players[0].QuestPoints}）");
                Check(cAlt.Players[0].Board[slotA].Exhausted, "花掉了这个单位本回合的行动");

                driver.RefreshAll();
                driver.SimulateOpenCommand(slotA);
                Check(!driver.HasCommand(AttackKind.Ability),
                      "★ 同一回合不能再点第二次（行动额度只有一次，规则书 `:150`）");

                cAlt.Players[0].Board[slotA] = backup;      // 靶场还原
                driver.RefreshAll();
                driver.SimulateDeselect();
            }
        }

        // ---- 5. 护卫限制 ----
        Debug.Log(P + "--- 护卫（Vanguard）限制目标 ---");
        {
            var probe = RuleCore.NewBattle(
                DeckBuilder.StarterDeck(StarterCards.Of(StarterCards.EmberFaction), StarterCards.EmberFaction, 30, new System.Random(5)),
                DeckBuilder.StarterDeck(StarterCards.Of(StarterCards.TideFaction), StarterCards.TideFaction, 30, new System.Random(6)),
                7);
            RuleCore.BeginTurn(probe);
            // 手搓一个场景：对手场上放个护卫 + 一个普通单位，我放个攻击者
            probe.Players[1].Board[1] = new UnitState(CardByName(StarterCards.Tide(), "Reef Guard"), false) { Exhausted = false };
            probe.Players[1].Board[2] = new UnitState(CardByName(StarterCards.Tide(), "Siren"), false) { Exhausted = false };
            probe.Players[0].Board[1] = new UnitState(CardByName(StarterCards.Ember(), "Veteran"), false) { Exhausted = false };

            int noGuard = RuleCore.IsValidTarget(probe, 0, 1, 1, 2, false);
            int guard = RuleCore.IsValidTarget(probe, 0, 1, 1, 1, false);
            Check(noGuard == RuleCodes.ErrTarget, $"场上有护卫时打不了普通单位（{RuleCodes.Describe(noGuard)}）");
            Check(guard == RuleCodes.OK, "场上有护卫时可以打护卫");
        }

        // ---- 6. 技能与触发：引擎的**事件流** → 特效 ----
        //
        // 这两类特效以前**接不上** —— 不是特效的问题，是引擎里没有这两种事件。
        // 2026-09-12 引擎补上了 `EvtKind.Ability` / `EvtKind.Trigger`，这里验「真的变成画面」。
        Debug.Log(P + "--- 技能与触发（事件流 → 特效）---");
        {
            var ember = StarterCards.Ember();
            var tide = StarterCards.Tide();

            // 确保轮到我（上一小节打完可能已经自动交回合了）
            if (ctx.Active != 0) { driver.SimulateEndTurn(); driver.SimulateAiTurn(); }
            Check(ctx.Active == 0, "轮到我了（技能用例要有自己的回合）");

            ClearEffects();     // 前面几小节的火线/烟会盖住画面，截图前清一次

            // ---- ① 主动技能：走**和真实点击同一条路** ----
            //      路径是「点自己的单位 → 弹出选择器 → 选主动技能 → 点目标」，不是直接调引擎
            int casterSlot = FreeSlot(ctx, 0);
            int preySlot = FreeSlot(ctx, 1);
            Check(casterSlot >= 0 && preySlot >= 0, $"有空位摆这一对（我 {casterSlot} / 对手 {preySlot}）");

            if (casterSlot >= 0 && preySlot >= 0)
            {
                ctx.Players[0].Board[casterSlot] =
                    new UnitState(CardByName(ember, "Ironclad"), false) { Exhausted = false };
                ctx.Players[1].Board[preySlot] =
                    new UnitState(CardByName(tide, "Shellback"), false) { Exhausted = false };
                driver.RefreshAll();
                Step(0.3f);

                // 卡面上要**写出来**这个单位有技能 —— 不然玩家不知道有这回事
                // ⚠️ 期望值从 `CardText` 取，**不要写死 "ABILITY"** ——
                //    文案会跟着语言变（有中文字体就是「技能」），写死的话改文案就断一次
                var casterView = driver.MyUnits.ContainsKey(casterSlot) ? driver.MyUnits[casterSlot] : null;
                Check(casterView != null && casterView.Data.keywords != null
                      && casterView.Data.keywords.Contains(CardText.Keyword(KeywordTable.Ability)),
                      $"卡面上写明了主动技能（「{(casterView == null ? "没视图" : casterView.Data.keywords)}」）");
                var preyView = driver.FoeUnits.ContainsKey(preySlot) ? driver.FoeUnits[preySlot] : null;
                Check(preyView != null && preyView.Data.keywords != null
                      && preyView.Data.keywords.Contains(CardText.Keyword(KeywordTable.Backlash)),
                      $"对手单位的触发关键词也写在卡面上（「{(preyView == null ? "没视图" : preyView.Data.keywords)}」）");

                _fired.Clear();

                // 第一步：点自己的单位 → 弹选择器（**这时还不该播技能特效**）
                Check(driver.SimulateOpenCommand(casterSlot), $"点自己的槽 {casterSlot} → 弹选择器");
                Check(driver.SelectedSlot == casterSlot, "选中的就是 Ironclad");
                Check(driver.HasCommand(AttackKind.Ability), "选择器里有「主动技能」那一项");
                Check(driver.HasCommand(AttackKind.Melee), "…也有「近战」（Ironclad 2 攻）");
                Check(FiredCount(VfxMap.Resolve(VfxMap.Ability)) == 0, "只是弹了个选择器，还没放技能");
                Step(0.2f);
                Shot(cam, "06_攻击方式选择器");

                // 第二步：点「主动技能」→ 点亮合法目标 + 弹技能卡面板
                int startCode = driver.SimulateUseAbility(casterSlot, -1);
                Check(startCode == RuleCodes.OK,
                      $"选「主动技能」→ 进入选目标（{RuleCodes.Describe(startCode)}）");
                Check(driver.Command == AttackKind.Ability, "定下来的打法是主动技能");
                Step(0.3f);                       // 推完那 0.2s 的淡入
                Shot(cam, "06b_技能_选目标");

                // ---- 技能卡面板（原版 `ActiveSkillDesc`）----
                var sp = driver.skillPanel;
                Check(sp != null && sp.Visible, "放技能时弹出技能卡面板");
                if (sp != null && sp.Visible)
                {
                    Check(sp.ShownName == CardText.Name("Ironclad"),
                          $"面板上写的是施放者那张卡（「{sp.ShownName}」）");
                    // ⚠️ **2026-09-29 改成「跟引擎比」**（原来写死 `== 1`）：面板上那个数**本来就是**
                    //   `HighlightTargets() → RuleCore.CanUseAbility` 数出来的 ⇒ 写死一个常数等于在赌
                    //   「场上只有一个可打目标」，而 AI（后手）开局多抽一张之后打法会变、场上会多一个单位。
                    //   这里用**引擎自己的判据**再数一遍（不另写一条规则），面板对不上就报。
                    int wantT = 0;
                    for (int t = 0; t < RuleEngine.BoardSpec.Size; t++)
                        if (RuleCore.CanUseAbility(ctx, 0, casterSlot, t) == RuleCodes.OK) wantT++;
                    Check(sp.ShownTargets == wantT && wantT >= 1,
                          $"面板上的「可选目标数」= 合法目标数（面板 {sp.ShownTargets} / 引擎 {wantT}）");
                    Check(sp.Alpha > 0.9f, $"淡入推完了（alpha {sp.Alpha:F2}）");
                    Check(sp.CurrentLight == SkillPanel.Light.Available,
                          $"有合法目标 → 铺黄绿那层（原版 `LightAvailable`，{sp.CurrentLight}）");
                    Debug.Log(P + "   " + sp.Describe());

                    // 指针按在面板上 → 铺蓝那层（原版 `LightPressed`）
                    sp.SetPointer(sp.transform.position, true);
                    Check(sp.CurrentLight == SkillPanel.Light.Pressed,
                          $"指针按在面板上 → 铺蓝那层（原版 `LightPressed`，{sp.CurrentLight}）");
                    sp.SetPointer(sp.transform.position, false);
                    Check(sp.CurrentLight == SkillPanel.Light.Available, "松开 → 回到黄绿");
                    Shot(cam, "06c_技能卡面板");
                }

                // 第三步：点目标 → 结算
                int hp1 = ctx.Players[1].Board[preySlot].Health;
                int useCode = driver.SimulateUseAbility(casterSlot, preySlot);
                Check(useCode == RuleCodes.OK, $"点敌方目标 → 放技能（{RuleCodes.Describe(useCode)}）");
                Check(sp == null || sp.CurrentLight == SkillPanel.Light.Acting,
                      $"技能结算时面板铺白那层（原版 `ShowActingLight`，{(sp == null ? "无面板" : sp.CurrentLight.ToString())}）");
                var after = ctx.Players[1].Board[preySlot];
                Check(after == null || after.Health < hp1,
                      $"技能打中了（{hp1} → {(after == null ? "阵亡" : after.Health.ToString())}）");
                Check(driver.Command == AttackKind.None, "打完了，指挥状态收干净");
                // 事件排了时间线（`EventTiming`），得推到播完才断言 —— 以前是同一帧全播的
                StepThrough(driver);
                Check(FiredCount(VfxMap.Resolve(VfxMap.Ability)) > 0,
                      $"**引擎的 Ability 事件变成了特效**（{VfxMap.Resolve(VfxMap.Ability)}）");
                Check(FiredCount(VfxMap.Resolve(VfxMap.Hit)) > 0, "挨伤害也播了（Hit 事件）");
                Check(ctx.Players[0].Board[casterSlot].Exhausted, "放技能花掉了这个单位的行动");
                Step(0.3f);
                Shot(cam, "06b_技能_命中");
            }

            // ---- ② 触发效果：把带 Rally 的卡**正常打出去** ----
            ClearEffects();
            _fired.Clear();
            ctx.Players[0].Hand.Insert(0, ctx.NewInstance(CardByName(ember, "Flamecaller")));   // 第 7 行第 2 步：手牌存实例
            ctx.Players[0].Energy = 10;              // 这一段验的是触发，不是费用，别让能量挡路
            driver.RefreshAll();
            Step(0.2f);

            int rallySlot = FreeSlot(ctx, 0);
            Check(rallySlot >= 0, $"有空格部署（{rallySlot}）");
            if (rallySlot >= 0)
            {
                Check(driver.SimulatePlay(0, rallySlot) == RuleCodes.OK, "把 Flamecaller 打出去");

                var rallyView = driver.MyUnits.ContainsKey(rallySlot) ? driver.MyUnits[rallySlot] : null;
                Check(rallyView != null && rallyView.Data.keywords != null
                      && rallyView.Data.keywords.Contains(CardText.Keyword(KeywordTable.Rally)),
                      $"卡面写出了触发关键词（「{(rallyView == null ? "没视图" : rallyView.Data.keywords)}」）");

                StepThrough(driver);      // 事件时间线推到播完（Deploy 1.0s → Trigger）
                Check(FiredCount(VfxMap.Resolve(VfxMap.Deploy)) > 0,
                      $"**引擎的 Deploy 事件变成了特效**（{VfxMap.Resolve(VfxMap.Deploy)}）");
                Check(FiredCount(VfxMap.Resolve(VfxMap.Trigger)) > 0,
                      $"**引擎的 Trigger 事件变成了特效**（{VfxMap.Resolve(VfxMap.Trigger)}）");
                Step(0.35f);
                Shot(cam, "07_触发效果_Rally");
            }

            // ---- ③ 对手那边发动技能，画面同样要播（事件流不分敌我）----
            ClearEffects();
            _fired.Clear();
            driver.SimulateEndTurn();                // 交给对手
            int guardSlot = FreeSlot(ctx, 1);
            if (guardSlot >= 0)
            {
                // 珊瑚卫 0 攻、只会治疗 —— AI 的规则里它一定会放技能（打不了人就只能放技能）
                ctx.Players[1].Board[guardSlot] =
                    new UnitState(CardByName(tide, "Reef Guard"), false) { Exhausted = false };
                ctx.Players[1].Warlord.Health -= 5;  // 督军掉点血，治疗才有理由放
                driver.RefreshAll();

                driver.SimulateAiTurn();             // AI 出牌 + 放技能 + 攻击，然后交回来
                StepThrough(driver);                 // 事件时间线推到播完
                Check(FiredCount(VfxMap.Resolve(VfxMap.Ability)) > 0,
                      "对手单位放技能，画面照样播（同一个事件流，不区分敌我）");
                Step(0.3f);
                Shot(cam, "08_对手发动技能");
            }
            else Debug.Log(P + "   （对手场上满了，跳过「对手放技能」用例）");
        }

        // ---- 7. 打完一整局 ----
        Debug.Log(P + "--- 打完一整局 ---");
        // 🆕 2026-09-27：**结算要写一条本地对局记录**（`Shell/BattleLogData.cs`，用户在结算处拍板接的）。
        // 先记下打之前有几条 —— 这样「有没有记上」「有没有**重复**记」（结算那段每帧都跑，
        // 全靠 `!_endPanel.Visible` 那道闸只放一次）两件事才验得出来。
        int logBefore = BattleLogData.Count;
        int turnGuard = 0;
        while (!ctx.IsOver && turnGuard < 120)
        {
            SimpleAI.PlayTurn(ctx);
            // 这一段是**规则**的批量压力测试（120 回合），不是表现层走的那条路 ——
            // 一次 drain 会把上千个特效同时点着，批处理下既慢又看不出什么。
            // 特效的断言在上面几小节已经做过了，这里只把事件倒掉。
            driver.DropSignals();
            if (ctx.IsOver) break;
            RuleCore.EndTurn(ctx);
            RuleCore.BeginTurn(ctx);
            turnGuard++;
        }
        driver.RefreshAll();
        Step(0.2f);
        // 特效诊断：谁大得离谱一眼就能看出来（尺寸失控的效果会把整屏盖住）
        foreach (var pl in WarpforgeVFX.WarpforgeEffectPlayer.ActivePlayers)
        {
            if (pl == null) continue;
            var rs = pl.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) { Debug.Log(P + $"   [特效诊断] {pl.name}：没有渲染器"); continue; }
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            Debug.Log(P + $"   [特效诊断] {pl.name,-28} 位置 {pl.transform.position} "
                        + $"包围盒 {b.size}（{rs.Length} 个渲染器）");
        }
        Check(ctx.IsOver, $"分出胜负了（共 {ctx.Turn} 回合，{turnGuard} 轮循环）");
        Check(ctx.Winner >= 1 && ctx.Winner <= 3, $"赢家 = {ctx.Winner}（1/2 胜，3 平局）");

        // ---- 对局结算界面（原版 `EndBattlePanel`）----
        var end = driver.End;
        Check(end != null, "结算面板建出来了");
        if (end != null)
        {
            Check(end.Visible, "打完之后结算面板是显示的");
            Check(end.ResultText == "胜利" || end.ResultText == "失败" || end.ResultText == "平局",
                  $"结果文字是三种之一（实际「{end.ResultText}」）");
            Check(end.ShownSkulls >= 0 && end.ShownSkulls <= 3,
                  $"骷髅数在 0..3（实际 {end.ShownSkulls}）");
            // 🔴 **2026-09-27（PA 普查）**：原版 `EndBattlePanel/AllRewardsHolder/SkullsHolder` 是 **PA=0**（Simple）
            //   ⇒ `40k_main_bt_nametag`（109×41）**拉满 648.1×52.4**；`ImageQuad` 默认按贴图比例定宽
            //   ⇒ 补 `SetAspect` 之前我们只画出 **139.3 宽（窄 508.8px、只剩 21%）**。
            Check(Mathf.Abs(end.SkullPlateWorldW * 108f - 648.1f) < 2f
               && Mathf.Abs(end.SkullPlateWorldH * 108f - 52.4f) < 1.5f,
                  $"★ 骷髅底条渲染 = 原版 **648.1×52.4**（PA=0 拉满）—— 实测 "
                + $"{end.SkullPlateWorldW * 108f:F1}×{end.SkullPlateWorldH * 108f:F1}");
            // 骷髅判据只有一处（`DeckRules.SkullsFor`）；这里**独立算一遍对账** ——
            // 终局血量是「降到过的最低生命」的上界，所以面板那个数只会 ≥ 它
            int foeNow = Mathf.Max(0, ctx.Players[1 - driver.MyIndex].Warlord.Health);
            int floor = RuleEngine.DeckRules.SkullsFor(foeNow);
            Check(end.ShownSkulls >= floor,
                  $"骷髅数 ≥ 按终局血量算的下界（面板 {end.ShownSkulls} / 终局算 {floor}）—— 中途降得更低过就该更多");
            // 名牌上那行 `x N` 和结算面板必须是**同一份判据**（`DeckRules.SkullsFor`）——
            // 两处各算各的迟早不一致，而「名板一个数、结算另一个数」是最难被发现的那种错
            Check(driver.SkullScoreText == "x" + end.ShownSkulls,
                  $"名牌里程碑 `{driver.SkullScoreText}` == 结算面板的 {end.ShownSkulls} 个骷髅");
            // 🆕 2026-09-26（用户点名问的）：**点亮的是不是「最左边那几个」**。
            //    原来只验总数 ⇒ 「亮哪几个」这件事**没有任何尺子**（`EndPanel` 里 code 是 `i < ShownSkulls`、
            //    index 0 在最左，但换个写法谁也发现不了）。⚠️ 同一个地方还有个地雷：
            //    `SkullThresholds = {20,10,0}` 是**降序**、图标是升序 —— 将来做「第 k 档 ↔ 第 k 个图标」会左右颠倒。
            var sk = end.SkullQuads;
            Check(sk != null && sk.Length == 3 && sk[0] != null && sk[1] != null && sk[2] != null,
                  "三个骷髅都建出来了（`skull_0..2`）");
            if (sk != null && sk.Length == 3 && sk[0] != null && sk[1] != null && sk[2] != null)
            {
                Check(sk[0].transform.position.x < sk[1].transform.position.x
                      && sk[1].transform.position.x < sk[2].transform.position.x,
                      "`skull_0` 在**最左**（index 递增 = 从左到右）");
                int lit = 0;
                // 🔴 **2026-09-27 改判据**：原版未点亮的骷髅是 **`SetActive(false)`（根本不显示）**、
                //    不是半透明（`EndBattleDoors__ShowRewards.c:58-68`）⇒ 这里**按 `activeSelf` 数**，
                //    不再按 alpha 数（原来那版数的是 `Tint.a > 0.5`，那是「我们挑的」半透明做法）。
                for (int i = 0; i < 3; i++) if (sk[i].gameObject.activeSelf) lit++;
                Check(lit == end.ShownSkulls, $"亮的**恰好** {end.ShownSkulls} 个（按 activeSelf 数，实得 {lit}）");
                bool leftToRight = true;
                for (int i = 0; i < 3; i++)
                    if (sk[i].gameObject.activeSelf != (i < end.ShownSkulls)) leftToRight = false;
                Check(leftToRight,
                      $"★ 点亮的正是**最左边那 {end.ShownSkulls} 个**（从左往右依次亮，不是从右往左/从中间）");
                // 🆕 **防雷：`SkullThresholds[k]` ↔ `skull_k` 一一对应**（这条原来是**没有**的 ——
                //    `SkullThresholds` 是**降序** `{20,10,0}` 而图标从左到右是**升序**，两者靠 index 对齐）。
                //    有人把它改成升序 ⇒ 会**静默**变成「第 1 个图标 = 最后达成的那一档」。
                var th = DeckRules.SkullThresholds;
                Check(th.Length == sk.Length, $"阈值个数 == 图标个数（{th.Length} / {sk.Length}）");
                Check(th.Length == 3 && th[0] == 20 && th[1] == 10 && th[2] == 0,
                      $"★ 阈值就是原版那三个 **20 / 10 / 0**（实得 {string.Join("/", System.Array.ConvertAll(th, x => x.ToString()))}）"
                    + " —— 判据 `MatchData__GetMilestones.c:45-83`（把敌方督军削到 ≤20 / ≤10 / ≤0 各得 1 个）");
                for (int i = 1; i < th.Length; i++)
                    Check(th[i - 1] > th[i],
                          $"★ 阈值必须**严格降序**（第 {i} 个 {th[i]} ＜ 第 {i - 1} 个 {th[i - 1]}）—— "
                        + "改成升序 = 「第 k 档 ↔ 第 k 个图标」静默左右颠倒");
            }
            // 🔴 用户 2026-09-17 定：**奖励行不做**（奖励红水晶与评分都在服务器，单机用不上）⇒ 只留骷髅。
            //    按**节点名**数（字段已删，数不到才说明真去干净了）
            Check(end.RewardRowPieces == 0,
                  $"★ 奖励行一块都不建（`RewardsHolder` / 奖杯 / 评分文字；实得 {end.RewardRowPieces} 块）");
        }

        // ---- 7b. 对局历史：结算那处写下的**本地记录**（🆕 2026-09-27）----
        // 🔴 这是**加功能、不是复刻** —— 原版这一步在服务器（每局结束写 `PlayerDataManager.battleLogData`），
        // 用户 2026-09-27 拍板由我们在结算处记（判据 → `Shell/BattleLogData.cs` 文件头）。
        // 断言盯两件最容易出事、又最难发现的事：① **只写一条**（结算那段代码每帧都跑，
        // 全靠 `!_endPanel.Visible` 那道闸放一次）② **数值与结算面板同源**（两处各算各的迟早不一致）。
        {
            Check(BattleLogData.Count == logBefore + 1,
                  $"★ 结算**恰好**写了一条对局记录（打之前 {logBefore} → 打完 {BattleLogData.Count}）");
            var rec = BattleLogData.Count > 0 ? BattleLogData.All[0] : null;
            Check(rec != null, "记录取得到（`All[0]` 是最新那条）");
            if (rec != null)
            {
                var want = ctx.Winner == 3 ? BattleLogData.Outcome.Draw
                         : ctx.Winner == driver.MyIndex + 1 ? BattleLogData.Outcome.Victory
                         : BattleLogData.Outcome.Defeat;
                Check(rec.Result == want,
                      $"结果与 `Ctx.Winner`={ctx.Winner} 同源（记录 {rec.Result} / 期望 {want}）");
                // 面板那行字是**另一处独立算出来的** ⇒ 拿它交叉对账（胜/负/平三态一一对上）
                if (end != null)
                {
                    var fromPanel = end.ResultText == "胜利" ? BattleLogData.Outcome.Victory
                                  : end.ResultText == "失败" ? BattleLogData.Outcome.Defeat
                                  : BattleLogData.Outcome.Draw;
                    Check(rec.Result == fromPanel,
                          $"记录的结果与结算面板那行字一致（面板「{end.ResultText}」/ 记录 {rec.Result}）");
                    // ★ 骷髅：**必须**是同一份判据（`DeckRules.SkullsFor`）—— 面板显示几颗，记录里就是几颗
                    Check(rec.OwnSkulls == end.ShownSkulls,
                          $"★ 我方骷髅数与结算面板一致（面板 {end.ShownSkulls} / 记录 {rec.OwnSkulls}）");
                }
                Check(rec.OwnName == ProfileData.PlayerName,
                      $"我方玩家名走**唯一那一处**（`ProfileData.PlayerName` = 「{ProfileData.PlayerName}」，记录「{rec.OwnName}」）");
                Check(rec.OwnHeroName.Length > 0, $"我方督军名非空（「{rec.OwnHeroName}」）");
                Check(rec.Mode == (ctx.Vars.IsSkirmish ? "Skirmish" : "Classic"),
                      $"模式写的是本局那个（记录「{rec.Mode}」）");
                // 单机打 bot：原版那格是**服务端账号 id**，本地没有对等物 ⇒ **留空，不编一个名字**
                Check(string.IsNullOrEmpty(rec.EnemyName),
                      $"单机局的对手名**留空**（不编；实得「{rec.EnemyName}」）");
                // 回放还没做（§三 第 18 条 第 6 件）⇒ 没有编号可比，记 -1
                Check(rec.RecordingIndex == -1, $"回放编号 = -1（回放整条链还没做）");
            }
        }
        // ---- 结算「开门」视频（原版 `EndBattleDoors`；资产在 `Resources/Art/videos/`）----
        // 反编译 `SetupDoor` 返回 `VideoClip.length`、调用方拿它 WaitForSeconds —— 这里就对这条约定对账：
        // **推够片长之前内容层不能揭晓，推够了必须揭晓**。
        var doors = end == null ? null : end.Doors;
        Check(doors != null, "开门那层建出来了");
        if (doors != null)
        {
            if (doors.HasVideo)
            {
                Check(doors.Playing, $"结算时视频在播（`{doors.Clip}`，片长 {doors.Length:F2}s）");
                Check(doors.ScreenVisible, "视频那层是显示着的");
                // 判据：`EndBattleDoors.SetupDoor` 自己就调了 `ShowRewards` —— **奖励跟视频同时出**，
                // 不是播完才揭晓（第一版按「播完揭晓」做的，读了反编译之后改掉了）
                Check(end.ContentVisible, "奖励和视频**同时**出现（原版 SetupDoor 里就是直接 ShowRewards）");
                Check(!end.TitleVisible, "结果文字藏着 —— 那段视频自己带 VICTORY/DEFEAT/DRAW 字样，原版也没有结果文字节点");
                // 🆕 2026-09-27：**开门音效**（原版每档一支 `AudioCue`，走 `MixerType.Jingles`）。
                //    原版那三条片子**没有音轨**（实测 audioTrackCount=0）⇒ 音效是**另播**的；
                //    我们原来**一支都没接**。这里断「三支都在」—— 视频在就说明 `Art/` 导过了，那音效也该在。
                {
                    var rr = new[] { BattleDoors.Result.Victory, BattleDoors.Result.Defeat, BattleDoors.Result.Draw };
                    foreach (var r1 in rr)
                        Check(BattleDoors.HasCue(r1),
                              $"开门音效 `{r1}`（`Art/audio/sfx/Match{r1}.ogg`）加载得到 —— 原版走 `SoundManager.Play2D(cue, Jingles)`");
                }

                float t = 0f;
                while (doors.Playing && t < 20f) { Step(1f / 30f); t += 1f / 30f; }

                Check(!doors.Playing, $"视频播完了（这一轮又推了 {t:F2}s）");
                // `Elapsed` 会被 `Advance` 夹在片长上 —— 正好等于片长才说明「等够了、也没多等」。
                // ⚠️ 别拿循环里的 t 和片长比：`Show` 之后到这儿之间已经推过别的 dt 了（这里 0.2s），
                //    第一版就是这么写的，差 0.19s 蒙混过关。
                Check(Mathf.Abs(doors.Elapsed - doors.Length) < 0.001f,
                      $"播到的位置正好 = 片长（{doors.Elapsed:F2}s / {doors.Length:F2}s）");
                Check(doors.ScreenVisible, "播完视频那层还在（留最后一帧当底图，不是播完就撤）");
            }
            else
            {
                // 走到这里说明 `Resources/Art/videos/` 没放视频（那目录 gitignore，删了就是这样）
                Check(end.ContentVisible, "没有视频资产 → 内容照常显示（不卡住、不静默等）");
                Check(end.TitleVisible, "没有视频时结果文字是我们自己画的（有视频时那是视频里的字）");
                Debug.Log(P + "   ⚠️ 没放开门视频（`Resources/Art/videos/`），这条只验了「没有也能走通」");
            }
        }

        string who = ctx.Winner == 3 ? "平局" : (ctx.Winner == 1 ? "我（Ember）胜" : "对手（Tide）胜");
        Debug.Log(P + $"   {who}；我督军剩 {Mathf.Max(0, ctx.Players[0].Warlord.Health)}，"
                    + $"对手督军剩 {Mathf.Max(0, ctx.Players[1].Warlord.Health)}");
        ClearEffects();
        Shot(cam, "05_对局结束");

        // ---- 8. 原版卡牌接进对战（Ultramarines vs Goff，2026-09-12）----
        // ⚠️ 上面几节跑的都还是**我们自己设计**的那 26 张（`StarterCards`）—— 那套是专门为覆盖
        //    关键词/触发设计的，`RuleEngineTest` 里那批规则用例靠它们，所以**留着**（而且它是
        //    「卡池可以换」这件事的活证明）。这一节才是「原版 1131 张真的打进一局」：
        //    **不带参数** `Begin()` = 默认两个原版阵营 + 从原版卡池自动凑牌。
        Debug.Log(P + "--- 原版卡牌接进对战（Ultramarines vs Goff）---");
        {
            // ⚠️ 这里必须**显式传**阵营：`Begin` 只在参数非 null 时覆盖字段（那是对的行为 ——
            //    重开一局不该把阵营悄悄改回默认），而上一节已经把字段设成 Ember/Tide 了。
            //    「按 Play 时用哪两个」是另一回事：`Start()` 调的是无参 `Begin()`，
            //    那个用的是字段初值 = 下面这两条常量（新实例才会走初值）。
            Check(BattleDriver.DefaultFactionA == "Ultramarines"
                  && BattleDriver.DefaultFactionB == "Goff",
                  $"无参 Begin() 的默认阵营 = {BattleDriver.DefaultFactionA} vs {BattleDriver.DefaultFactionB}"
                  + "（打开 Battle.unity 按 Play 打的就是这两个）");

            driver.Begin(BattleDriver.DefaultFactionA, BattleDriver.DefaultFactionB, 20260912);
            Step(0.3f);
            var c2 = driver.Ctx;

            Check(driver.MyFaction == BattleDriver.DefaultFactionA
                  && driver.FoeFaction == BattleDriver.DefaultFactionB,
                  $"阵营 = {driver.MyFaction} vs {driver.FoeFaction}");
            Check(c2.Players[0].Warlord.Card.FromOriginalPool,
                  $"我方督军「{c2.Players[0].Warlord.Name}」来自原版卡池");
            Check(c2.Players[0].Warlord.Card.Faction == driver.MyFaction, "督军阵营和写的一致");

            // ---- 卡背：`WFModuleCardback.CardbackResolver` **真的接上了**（2026-09-19 补）----
            // 这条线原来是**死的**：全工程只有声明、没有赋值点 ⇒ `ResolveCardback()` 恒 null
            // ⇒ `Initialize` 早退（原版「拿不到卡背」那条路）⇒ `CreateCard*` 家族那 18 个效果 /
            // 30 个实例**一个卡背都画不出来，而且不报错**（正本
            // `资料/普查产出_0918/孤儿待办_五条查证.md` §一）。
            // 判据取「**双方各解析得出一张 sprite**」—— 只验「回调非 null」的话，
            // 回调恒返回 null 也照样绿（这是本工程记过的「尺子的假象」）。
            // ⚠️ 卡背来源 = **阵营**（`CardArt.CardBack`，和牌堆/敌方手牌**同源**）——
            //    原版是玩家档案里的装饰品，我们单机没有档案。目前只有 4 个阵营有图
            //    （`back_{ember,goff,tide,ultramarines}.png`），其余阵营按「拿不到卡背」如实报。
            var cbResolver = WarpforgeVFX.WFModuleCardback.CardbackResolver;
            Check(cbResolver != null,
                  "卡背：`CardbackResolver` 已由 `BattleDriver.HookAnimFxCards` 接上"
                  + "（没接的话卡背那 18 个效果全空、而且不报错）");
            if (cbResolver != null)
            {
                var cbMine = cbResolver(true);
                var cbFoe = cbResolver(false);
                Check(cbMine != null && cbFoe != null,
                      $"卡背：双方各**解析得出一张 sprite**"
                      + $"（我 {driver.MyFaction} →「{(cbMine == null ? "null" : cbMine.name)}」/ "
                      + $"对手 {driver.FoeFaction} →「{(cbFoe == null ? "null" : cbFoe.name)}」）");
                Check(cbMine == cbResolver(true),
                      "卡背：同一方两次拿到的是**同一张**（按阵营缓存，不是每次 `Sprite.Create`）");
            }

            int handPool = 0, deckPool = 0, foeDeckPool = 0;
            foreach (var card in c2.Players[0].Hand) if (card.Card.FromOriginalPool) handPool++;
            foreach (var card in c2.Players[0].Deck) if (card.Card.FromOriginalPool) deckPool++;
            foreach (var card in c2.Players[1].Deck) if (card.Card.FromOriginalPool) foeDeckPool++;
            Check(handPool == c2.Players[0].Hand.Count,
                  $"手牌 {handPool}/{c2.Players[0].Hand.Count} 张来自原版卡池");
            Check(deckPool == c2.Players[0].Deck.Count && foeDeckPool == c2.Players[1].Deck.Count,
                  $"双方抽牌堆全部来自原版卡池（我 {deckPool} / 对手 {foeDeckPool}）");

            // 卡面：中文名 + 卡自己的效果原文（原版卡面就是这两样）
            var v0 = driver.HandViewAt(0);
            var h0 = c2.Players[0].Hand[0].Card;      // 第 7 行第 2 步：手牌存实例
            Check(v0 != null && v0.Data.title == h0.NameZh,
                  $"手牌第一张卡面用**中文名**「{(v0 == null ? "?" : v0.Data.title)}」"
                  + $"（英文 {h0.Name}）");
            Check(v0 != null && !string.IsNullOrEmpty(v0.Data.keywords),
                  $"卡面效果行：「{Short(v0 == null ? "" : v0.Data.keywords, 34)}」");
            Debug.Log(P + "   手牌：" + driver.HandViewNames());

            // ---- 卡牌放大展示窗（原版 `CardDisplayWindow`；轻点卡牌开关）----
            // 判据来自反编译 `BasicCardUI.ToggleOpenCardDisplayOnTouch` —— **轻点**就是开关。
            {
                var cdw = driver.CardDisplay;
                Check(cdw != null, "卡牌展示窗建出来了");
                var view = driver.HandViewAt(0);

                void Tap()
                {
                    it.SimulateHover(view.transform.position);
                    Step(0.05f);
                    it.SimulatePress(view.transform.position);
                    Step(0.05f);
                    it.SimulateRelease(view.transform.position);      // 原地松开 = 轻点
                    Step(0.1f);
                }

                Tap();
                Check(cdw.Visible, "轻点手牌 → 展示窗打开（原版 `ToggleOpenCardDisplayOnTouch`）");
                Check(cdw.ShownTitle == h0.NameZh, $"窗里就是这张卡：{cdw.ShownTitle}");
                Check(!string.IsNullOrEmpty(cdw.ShownBody),
                      $"下面写着它的效果：「{Short(cdw.ShownBody, 26)}」");
                // 🔴 **卡体尺寸**：原版卡槽 `m_LocalScale = 250` × 卡体 `2.0927×3.3313`
                //    ⇒ **832.825 px**（**不是 743** —— 那是**多卡展示窗**模板 `CardUI Reference` 的 223.14 档；
                //    2026-09-28 订正，判据 → `资料/阶段二_卡片详情窗_原版规格.md` §十·4）
                float bigH = cdw.Card == null ? 0f
                           : CardView.Height * cdw.Card.transform.localScale.y * 108f;
                Check(Mathf.Abs(bigH - CardFan.FrontHpx) < 2f,
                      $"放大卡高 {bigH:F0} px = 原版 {CardFan.FrontHpx:F1}（槽 scale 250 × 卡体 3.3313）");

                // ---- 卡片那一叠（1 主卡 + 相关卡 · 扇形）----
                // 判据与真值 → `Core/CardFan.cs`（原版那条 legacy clip `Card Display Open`）。
                // ⚠️ 相关卡**算得出几张取决于这张牌**（点名/池子），所以只断「≥1 格」；
                //    但**位姿 / 缩放比 / 分层 / 压暗**那几条是原版 clip 的常量，对每一格都成立。
                Check(cdw.SlotCount >= 1, $"卡片那一叠 **{cdw.SlotCount} 格**（1 主卡 + {cdw.SlotCount - 1} 相关卡）");
                Check(Mathf.Abs(cdw.SlotView(0).Tint.r - 1f) < 0.01f,
                      "★ 前台那张**不压暗**（原版前台色 = (1,1,1,1)，`_DAT_1834b2e50` 当场解出来的）");
                if (cdw.SlotCount >= 2)
                {
                    var s1 = cdw.SlotView(1);
                    var c1 = LayoutSpace.ToPixel(s1.transform.position);
                    Check(Mathf.Abs(c1.x - (960f - 121f)) < 2.5f,
                          $"槽 1 中心 x = {c1.x:F1}（原版 clip `anchoredPosition.x = −121`）");
                    Check(Mathf.Abs(c1.y - (540f - 53f)) < 2.5f,
                          $"槽 1 中心 y = {c1.y:F1}（原版 `anchoredPosition.y = 53`）");
                    Check(Mathf.Abs(s1.transform.eulerAngles.z - 2.510f) < 0.05f,
                          $"槽 1 转角 = {s1.transform.eulerAngles.z:F3}°（原版 clip 2.510°）");
                    Check(Mathf.Abs(s1.transform.localScale.x / cdw.SlotView(0).transform.localScale.x
                                    - 232.9537f / 250f) < 0.005f,
                          "槽 1 缩放比 = 232.954/250（原版 clip）");
                    Check(Mathf.Abs(s1.Tint.r - CardFan.BackTint.r) < 0.01f,
                          $"★ 相关卡压暗到 {s1.Tint.r:F2} = 原版 `cardInBackGroundColorTint`(0.65)");
                    int q0 = CardQueue(cdw.SlotView(0)), q1 = CardQueue(s1);
                    Check(q0 > q1, $"★ 前台那张的渲染队列（{q0}）**高于**相关卡（{q1}）"
                                 + " —— 前后靠队列、不靠 z（`CardView` 每层写死 3000，多格叠加会错乱）");
                }
                Shot(cam, "11_卡牌展示窗");

                // ---- 🆕 A156：点击区（原版 `CardUI/2DCard/UI Collider`）的**双轴比例 + 偏置** ----
                // 🔴 判据 = 解包原件字段（2026-10-07 现读 · 第一权威）：
                //   `bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_2801.json`
                //     `m_AnchorMin(0,0)` · `m_AnchorMax(1,1)`（**拉伸锚**）· `m_Pivot(0.5,0.5)`
                //     · `m_AnchoredPosition(0, −0.02)` · `m_SizeDelta(−0.2, −0.44)`
                //   父件 `2DCard`（`RectTransform_2876`）= `m_SizeDelta 2.0927 × 3.3313`（卡单位 · scale 250）
                //   ⇒ 点击区 **473.175 × 722.825** px（卡体 523.175 × 832.825 · 卡心 (960,480)）
                //   ⇒ 四沿 = **723.412 / 123.587 / 1196.588 / 846.413**
                //     （左右各内缩 **25** · **上缩 60 · 下缩 50** —— 中心比卡心**低 5px**）
                //   ⚠️ **两轴比例不同**（x = 1.8927/2.0927 = 0.90443 · y = 2.8913/3.3313 = 0.86792）。
                //      改前我们用的是「**一个 0.9043 双轴同用 + 居中**」⇒ 上沿 103.438 / 下沿 856.562，
                //      **上多吃 20.149px · 下多吃 10.149px**（那两条窄带原版不吃）。本段四条边一改就红。
                {
                    Check(cdw.Visible, "（前提）展示窗开着 —— 下面四条边要拿 `HitSlot` 二分出来");
                    // 二分夹逼：`inside`（命中槽 0）→ `outside`（不命中）。回传收敛到的那个点。
                    //  ⚠️ 探针一律取 **x=960 / y=480** 这两条线：槽 1–8 的矩形在这两条线上都不覆盖
                    //     被夹逼的那一段（槽 1 命中区 y∈[154.9, 828.4]、x∈[618.5, 1059.5]）⇒ 边界唯一。
                    Vector2 Bisect(float ix, float iy, float ox, float oy)
                    {
                        float lo = 0f, hi = 1f;
                        for (int k = 0; k < 32; k++)
                        {
                            float t = (lo + hi) * 0.5f;
                            var p = EndPanel.Pos(ix + (ox - ix) * t, iy + (oy - iy) * t, 0f);
                            if (cdw.HitSlot(p) == 0) lo = t; else hi = t;
                        }
                        return new Vector2(ix + (ox - ix) * lo, iy + (oy - iy) * lo);
                    }
                    const float OL = 723.412f, OT = 123.587f, OR = 1196.588f, OB = 846.413f;
                    float eT = Bisect(960f, 480f, 960f, 0f).y;
                    float eB = Bisect(960f, 480f, 960f, 1000f).y;
                    float eL = Bisect(960f, 480f, 600f, 480f).x;
                    float eR = Bisect(960f, 480f, 1320f, 480f).x;
                    Check(Mathf.Abs(eT - OT) < 0.5f,
                          $"★ 点击区**上沿** = {eT:F3}（原版 `sd(-0.2,-0.44)`+`ap(0,-0.02)` 复算 **123.587**"
                          + " ⇒ **上缩 60**；改前是 103.438 = 双轴同用一个 0.9043 的错）");
                    Check(Mathf.Abs(eB - OB) < 0.5f,
                          $"★ 点击区**下沿** = {eB:F3}（原版 **846.413** ⇒ **下缩 50**；改前是 856.562）");
                    Check(Mathf.Abs(eL - OL) < 0.5f,
                          $"★ 点击区**左沿** = {eL:F3}（原版 **723.412** ⇒ 左缩 25 = 0.2 卡单位 × 250）");
                    Check(Mathf.Abs(eR - OR) < 0.5f,
                          $"★ 点击区**右沿** = {eR:F3}（原版 **1196.588** ⇒ 右缩 25）");
                    // 行为面：两条「改前被我们多吃」的窄带（取那条带的中线，两边各留 ≈9px 余量）
                    Check(cdw.HitSlot(EndPanel.Pos(960f, 113.5f, 0f)) == -1,
                          "★ 卡面**上缘那条窄带**（103.4–123.6）⇒ 现在**不命中**（原版不吃这一下）");
                    Check(cdw.HitSlot(EndPanel.Pos(960f, 133.5f, 0f)) == 0,
                          "…而它下面 10px（123.6 以内）⇒ 仍然**判给前台那张**（别把整块都关掉）");
                    Check(cdw.HitSlot(EndPanel.Pos(960f, 851.5f, 0f)) == -1,
                          "★ 卡面**下缘那条窄带**（846.4–856.6）⇒ 现在**不命中**（原版不吃这一下）");
                    Check(cdw.HitSlot(EndPanel.Pos(960f, 836.5f, 0f)) == 0,
                          "…而它上面 10px（846.4 以内）⇒ 仍然**判给前台那张**");
                    Check(cdw.HitSlot(EndPanel.Pos(960f, 480f, 0f)) == 0,
                          "★ 卡心 ⇒ 照旧判给前台那张（别改成一个永远 −1 的实现）");
                }

                // ---- 换位（原版 `ChangeCardPosition` + `CardSwapFinished`）----
                if (cdw.SlotCount >= 2)
                {
                    var front0 = cdw.FrontDef;
                    string title0 = cdw.ShownTitle;
                    var posFront = cdw.SlotView(0).transform.position;
                    var posOther = cdw.SlotView(1).transform.position;
                    cdw.SwapToFront(1);
                    Check(cdw.IsSwapping, "点相关卡 ⇒ 换位在播（原版闸① `swappingCards` 置上）");
                    cdw.SwapToFront(2);      // 播到一半再点 ⇒ 该被闸①挡掉
                    Check(cdw.FrontDef == front0 && cdw.ShownTitle == title0,
                          "★ 换位播到一半再点 ⇒ **什么都不做**（原版闸①）");
                    Step(0.3f);              // `CardTween.Mode = Manual` 已由本自检置好
                    Check(!cdw.IsSwapping, "0.25s（原版 `relatedCardSwapTime`）之后换位收尾、开闸");
                    Check(cdw.FrontDef != front0,
                          $"★ 被点那张换到了前台（现在是「{cdw.ShownTitle}」）"
                          + " —— 收尾照原版 `CardSwapFinished` 重设了 lore / 语音 / 眼睛钮");
                    Check(Vector3.Distance(cdw.SlotView(0).transform.position, posFront) < 0.01f,
                          "★ 它站在**原来的前台位**（两两互换，不是「把谁提到最前」）");
                    Check(Vector3.Distance(cdw.SlotView(1).transform.position, posOther) < 0.01f,
                          "★ 原来那张前台让到了**被点卡的槽位**（同上）");
                    Check(CardQueue(cdw.SlotView(0)) > CardQueue(cdw.SlotView(1)),
                          "★ 换位后队列**跟着重排**（新前台画在最上面 —— 不重排就会被身后的卡盖住）");
                    Check(Mathf.Abs(cdw.SlotView(0).Tint.r - 1f) < 0.01f
                          && Mathf.Abs(cdw.SlotView(1).Tint.r - CardFan.BackTint.r) < 0.01f,
                          "★ 着色也跟着换（新前台变白、让位那张压暗 —— 原版那两条 tween）");
                    Check(Mathf.Abs(cdw.SlotView(1).transform.localScale.x / cdw.SlotView(0).transform.localScale.x
                                    - 232.9537f / 250f) < 0.005f,
                          "★ …连**缩放比**也对调过来了（前台永远是位姿槽 0）");
                    Shot(cam, "11c_展示窗_换位后");
                }

                // ---- 点击路由（运行时是 `BattleDriver.Update` → `HandleDisplayWindowClick`；批处理里直接调它）----
                // 🔴 **重叠区归前台**（原版：每格一张卡自己的 `UI Collider`，射线取最上面那张）
                //    ⇒ 要验「点相关卡」必须点在**它露出前卡之外的那一条**上。
                {
                    if (cdw.SlotCount >= 2)
                    {
                        var s1 = cdw.SlotView(1);
                        // 槽 1 的命中矩形 x ≈ 618.6…1059.4、槽 0 的 ≈ 723.4…1196.6 ⇒ 660 落在**只属于槽 1** 的那条
                        var w1 = EndPanel.Pos(660f, 487f, 0f);
                        Check(cdw.HitSlot(w1) == 1,
                              "★ 点在「槽 1 露出前卡之外」的那一条 ⇒ 判给**槽 1**（重叠区归前台 —— 原版同此）");
                        Check(cdw.HitSlot(s1.transform.position) == 0,
                              "…而点在**重叠区**（槽 1 的中心就在前卡上）⇒ 判给**前台那张**");
                        Check(driver.HandleDisplayWindowClick(w1),
                              "★ 那一下**被窗吃掉**并拿去换位（`BattleDriver.HandleDisplayWindowClick`）");
                        Check(cdw.IsSwapping, "…换位真的开始了（原版闸① `swappingCards` 置上）");
                        Step(0.3f);
                        Check(driver.HandleDisplayWindowClick(cdw.SlotView(0).transform.position),
                              "★ 点**前台那张**也被吃掉（原版闸②：什么都不做，但**不能落给遮罩**）");
                        // 🆕 2026-09-29：**点遮罩空白 = 关窗**（原版 `BackgroundCloseButton.OnPointerClick`
                        //   → `OnBackgroundClick` → `Close`；这一格原来记的是「我们没接」）。
                        //   ⚠️ 两条「同帧」守卫（刚开窗那一帧不判遮罩 / 同帧不再开）在**批处理里恒不生效**
                        //   —— `BattleDriver.SameFrame` 里排除了 `Application.isBatchMode`（`Time.frameCount`
                        //   在自检里不推进，不排除的话那两条路永远走不到）。
                        Check(driver.HandleDisplayWindowClick(EndPanel.Pos(960f, 1026f, 0f)),
                              "★ 点遮罩空白 ⇒ **被窗吃掉并关窗**（原版 `BackgroundCloseButton`）");
                        Check(!cdw.Visible, "…窗真的关了（原来这里是「不拦截」）");
                        Tap();                          // 后面几条还要用窗 ⇒ 再点开
                        Check(cdw.Visible, "（重开）再轻点同一张手牌 → 窗又开了");

                        // 🆕 2026-09-29：**指针移开就关**（原版 `CardCollider.OnPointerExit` → `CardScript.OnTouchExit`）
                        Check(!driver.TickCardWinPointerExit(view.transform.position),
                              "指针**还压在**那张牌上 ⇒ 不关（原版唯一守卫 `displayingCardFlag` 就是它）");
                        Check(!driver.TickCardWinPointerExit(cdw.SlotView(0).transform.position),
                              "…压在**窗里那个卡格**上 ⇒ 也不关（这两个钮在卡外的下缘，见 `ContainsPointer` 的注释）");
                        Check(driver.TickCardWinPointerExit(EndPanel.Pos(60f, 60f, 0f)),
                              "★ 指针**移开那张牌** ⇒ 窗自己关掉（原版 `OnTouchExit`；不用再点一下）");
                        Check(!cdw.Visible, "…窗关了");
                        Tap();
                    }
                    else
                    {
                        Check(driver.HandleDisplayWindowClick(EndPanel.Pos(960f, 1026f, 0f)),
                              "★ 只有 1 格时点遮罩空白 ⇒ 同样关窗（这条路和格数无关）");
                        Check(!cdw.Visible, "…窗关了");
                        Tap();
                    }
                }

                // ---- 语音按钮（原版 `Voices Over Button`，`x[1650.0,1738.7] y[945.5,1034.2]`）----
                // 位置/大小来自权威表；点它播**正在展示那张卡**的单位语音（`VoiceLines`）
                var voiceWorld = new Vector3((1694.35f - 960f) / EndPanel.PxPerUnit,
                                             (540f - 989.85f) / EndPanel.PxPerUnit, 0f);
                Check(cdw.HitVoice(voiceWorld), "★ 语音按钮**算命中**（原版那个 88.66² 的圆钮位置）");
                Check(!cdw.HitVoice(Vector3.zero), "★ 屏幕正中**不算**（不会到处都是按钮）");
                cdw.PlayVoice();
                Check(cdw.LastVoiceFile != null || !VoiceLines.Has(h0.Id),
                      $"★ 点语音按钮 → 播了「{cdw.LastVoiceFile}」"
                      + "（这张卡没有语音时**明说**、不静默 —— 判据是 `VoiceLines.Has`）");

                // ---- 相关卡那一叠 + 换位：**换一张一定有相关卡的卡再验一遍** ----
                // 上面那张手牌常常一张相关卡都没有（`SlotCount == 1`）⇒ 扇形/换位/命中那几条会整段跳过。
                // 样卡用 `Master of Arcana`：它的天赋是个 **4 张的池子** ⇒ 一定有相关卡（`CollectionScene` 用的也是它）。
                // 判据与真值 → `Core/CardFan.cs`（原版那条 legacy clip `Card Display Open`）+ 正本 §十。
                {
                    var moa = FindPoolCard("Master of Arcana");
                    Check(moa != null, "（前提）卡池里有 `Master of Arcana`（天赋是个 4 张的池子）");
                    if (moa != null)
                    {
                        cdw.Show(BattleDriver.ToCardData(moa, moa.Faction), moa);
                        Check(cdw.SlotCount >= 2, $"`Master of Arcana` ⇒ 那一叠 **{cdw.SlotCount} 格**（主卡 + 相关卡）");
                        var s1 = cdw.SlotView(1);
                        var c1 = LayoutSpace.ToPixel(s1.transform.position);
                        Check(Mathf.Abs(c1.x - (960f - 121f)) < 2.5f,
                              $"槽 1 中心 x = {c1.x:F1}（原版 clip `anchoredPosition.x = −121`）");
                        Check(Mathf.Abs(c1.y - (540f - 53f)) < 2.5f,
                              $"槽 1 中心 y = {c1.y:F1}（原版 `anchoredPosition.y = 53`）");
                        Check(Mathf.Abs(s1.transform.eulerAngles.z - 2.510f) < 0.05f,
                              $"槽 1 转角 = {s1.transform.eulerAngles.z:F3}°（原版 clip 2.510°）");
                        Check(Mathf.Abs(s1.transform.localScale.x / cdw.SlotView(0).transform.localScale.x
                                        - 232.9537f / 250f) < 0.005f,
                              "槽 1 缩放比 = 232.954/250（原版 clip）");
                        Check(Mathf.Abs(s1.Tint.r - CardFan.BackTint.r) < 0.01f,
                              $"★ 相关卡压暗到 {s1.Tint.r:F2} = 原版 `cardInBackGroundColorTint`(0.65)");
                        int q0 = CardQueue(cdw.SlotView(0)), q1 = CardQueue(s1);
                        Check(q0 > q1, $"★ 前台那张的渲染队列（{q0}）**高于**相关卡（{q1}）"
                                     + " —— 前后靠队列、不靠 z（`CardView` 每层写死 3000，多格叠加会错乱）");
                        // 命中：**重叠区归前台**（原版每格一张自己的 `UI Collider`，射线取最上面那张）
                        var w1 = EndPanel.Pos(660f, 487f, 0f);   // 槽 1 露出前卡之外的那一条
                        Check(cdw.HitSlot(w1) == 1, "★ 点在「槽 1 露出前卡之外」的那一条 ⇒ 判给**槽 1**");
                        Check(cdw.HitSlot(s1.transform.position) == 0, "…点在**重叠区** ⇒ 判给**前台那张**");
                        // 🆕 2026-09-29：点遮罩空白 = 关窗（同上面那条；同帧守卫在批处理里恒不生效，见 `SameFrame`）
                        Check(driver.HandleDisplayWindowClick(EndPanel.Pos(960f, 1026f, 0f)),
                              "★ 点遮罩空白 ⇒ 关窗");
                        Check(!cdw.Visible, "…窗关了");
                        // ⚠️ `Show` 会**重建**卡格 ⇒ 上面抓的 `s1` 变成**已销毁对象**
                        //    （实测：这里踩过一次 `MissingReferenceException`）⇒ 重开后**必须重新取一次**。
                        cdw.Show(BattleDriver.ToCardData(moa, moa.Faction), moa);
                        s1 = cdw.SlotView(1);
                        Shot(cam, "11e_展示窗_相关卡");
                        // 换位（原版 `ChangeCardPosition` + `CardSwapFinished`）
                        var front0 = cdw.FrontDef;
                        var posFront = cdw.SlotView(0).transform.position;
                        var posOther = s1.transform.position;
                        Check(driver.HandleDisplayWindowClick(w1), "★ 那一击**被窗吃掉**并拿去换位");
                        Check(cdw.IsSwapping, "…换位真的开始了（原版闸① `swappingCards` 置上）");
                        cdw.SwapToFront(2);      // 播到一半再点 ⇒ 该被闸①挡掉
                        Check(cdw.FrontDef == front0, "★ 换位播到一半再点 ⇒ **什么都不做**（原版闸①）");
                        Step(0.3f);              // `CardTween.Mode = Manual` 已由本自检置好
                        Check(!cdw.IsSwapping, "0.25s（原版 `relatedCardSwapTime`）之后收尾、开闸");
                        Check(cdw.FrontDef != front0, $"★ 被点那张换到了前台（现在是「{cdw.ShownTitle}」）");
                        Check(Vector3.Distance(cdw.SlotView(0).transform.position, posFront) < 0.01f,
                              "★ 它站在**原来的前台位**（两两互换，不是「把谁提到最前」）");
                        Check(Vector3.Distance(cdw.SlotView(1).transform.position, posOther) < 0.01f,
                              "★ 原来那张前台让到了**被点卡的槽位**（同上）");
                        Check(CardQueue(cdw.SlotView(0)) > CardQueue(cdw.SlotView(1)),
                              "★ 换位后队列**跟着重排**（不重排的话新前台会被身后的卡盖住）");
                        Check(Mathf.Abs(cdw.SlotView(0).Tint.r - 1f) < 0.01f
                              && Mathf.Abs(cdw.SlotView(1).Tint.r - CardFan.BackTint.r) < 0.01f,
                              "★ 着色也跟着换（新前台变白、让位那张压暗 —— 原版那两条 tween）");
                        Check(driver.HandleDisplayWindowClick(cdw.SlotView(0).transform.position),
                              "★ 点**前台那张**也被吃掉（原版闸②：什么都不做，但**不能落给遮罩**）");
                        Shot(cam, "11f_展示窗_换位后");
                    }
                }

                // ---- 风味底图（原版 `FlavourTextSO.GetClanFlavorBackground`，按阵营选）----
                // 13 张图 2026-09-28 才导进工程（`Resources/Art/ui/flavourbg_<阵营小写>.png`）。
                {
                    var moa2 = FindPoolCard("Master of Arcana");
                    var fac = moa2 != null ? moa2.Faction : h0.Faction;
                    Check(CardArt.FlavorBg(fac) != null,
                          $"★ 阵营「{fac}」的风味底图取得到（原版那 13 张按阵营的 `40K_display_Flavortext *`）");
                    Check(CardArt.FlavorBg(null) == null, "…阵营为空时**取不到**（不许静默给一张错的）");
                }

                Tap();
                Check(!cdw.Visible, "再轻点一次 → 关掉");
            }

            // ---- 多张一起看（原版 `UIMultiCardDisplay` / `Generic Multi Card Display Combat`）----
            // 版面数值全部来自**逐字段权威表**（`资料/战斗规格/战斗重建_0827/子代理读报_front弹层_0827.md` §二）：
            // 窗口带 `y[131,949]`（818.04 高）· 标题 1192.37×63.204 / fs38 / 白 · 遮罩 α0.7725 ·
            // Continue 条 577.5×63.84 + 圆钮 80.47（纵向凸出）。
            // ⚠️ **入口（点我方牌堆）是我们挑的** —— 原版从 `BattleManager.ResolveAction` 打开（环境卡/战绩卡组）。
            {
                var mcd = driver.MultiCards;
                Check(mcd != null, "多张展示窗建出来了（原版 `UIMultiCardDisplay`）");
                Check(mcd != null && !mcd.Visible, "……平时是关着的");

                if (mcd != null)
                {
                    driver.ShowMyDeck(true);
                    Step(0.05f);
                    Check(mcd.Visible, "★ 点牌堆 → **摊开牌库**");
                    Check(mcd.HeaderShown == "你的牌库", $"★ 标题：「{mcd.HeaderShown}」");
                    Check(mcd.CardCount > 0 && mcd.CardCount == c2.Players[0].Deck.Count,
                          $"★ 摊开 {mcd.CardCount} 张 = 牌库剩的 {c2.Players[0].Deck.Count} 张");
                    // 一排的宽度：装得下就不缩（可用宽 = 1920 − 两侧各 60），缩过也不许小于下限
                    float availPx = 1920f - 120f;
                    Check(mcd.ContentWidthPx > 0f, $"一排宽 {mcd.ContentWidthPx:F0} px（可用 {availPx:F0}）");
                    Check(mcd.CardCount <= 1 || mcd.ContentWidthPx <= availPx + 1f
                          || mcd.UsedScale > 0f, "……装不下时是**等比缩**（原版这里是横向滚动 —— 我们挑的）");
                    // 卡中心 y 落在窗口带里（标题下面那一截的中点）
                    Check(mcd.CardCypx > MultiCardDisplay.BandTopPx + 63f && mcd.CardCypx < MultiCardDisplay.BandBottomPx,
                          $"卡中心 y = {mcd.CardCypx:F0} px（窗口带 [{MultiCardDisplay.BandTopPx:F0},{MultiCardDisplay.BandBottomPx:F0}] 之内）");
                    Shot(cam, "11b_多张展示窗");
                    driver.ShowMyDeck(false);
                    Check(!mcd.Visible, "关掉");
                }

                // 入口判据：牌堆那一块算命中，屏幕别处不算（`MyDeckX01/MyDeckY01` 是 `BattleDriver` 的常量）
                var deckWorld = LayoutSpace.ToWorld(0.90104f, 0.10648f);
                Check(BattleDriver.HitMyDeckPile(deckWorld), "★ 牌堆中心**算命中**（入口判据）");
                Check(!BattleDriver.HitMyDeckPile(Vector3.zero), "★ 屏幕正中**不算**（不会到处都被当成牌堆）");
            }

            // 真拖一张上场（和上面那条一样的鼠标路径，不是直接调 SimulatePlay）
            int dragIdx = -1;
            for (int i = 0; i < c2.Players[0].Hand.Count; i++)
                if (c2.Players[0].Hand[i].Card.Cost <= c2.Players[0].Energy) { dragIdx = i; break; }

            if (dragIdx >= 0)
            {
                var view = driver.HandViewAt(dragIdx);
                int freeSlot = SimpleAI.FirstFreeSlot(c2.Players[0]);
                // 🔴 2026-09-20：拖拽的**落点**用 `DropTargetWorld` —— 真 3D 时卡画在透视层，
                //    屏幕位置与老的 `SlotPosition` 差 ≈150 px（拖动测试必须照着**看得见的那个位置**拖）。
                var slotPos = pBoard.DropTargetWorld(freeSlot);
                it.SimulateHover(view.transform.position);
                Step(0.05f);
                it.SimulatePress(view.transform.position);
                Step(0.2f);
                for (int i = 0; i < 24; i++) { it.SimulateDrag(slotPos, 1f / 30f); Step(1f / 30f); }
                it.SimulateRelease(slotPos);
                for (int i = 0; i < 90 && c2.Players[0].Board[freeSlot] == null; i++) Step(1f / 30f);
                Step(0.2f);
                Check(c2.Players[0].Board[freeSlot] != null,
                      $"原版卡拖上场成功：{c2.Players[0].Board[freeSlot]?.Name} → 槽 {freeSlot}");
                // ⚠️ 这条同时是 `CardInteraction._placed` 那个坑的回归测试：
                //    上面第 2 节已经往场上摆过牌，`_placed` 里还留着那些槽号。
                //    `Begin()` 不清它的话，这一张会**弹回手上**（2026-09-12 就是这么撞出来的）。
                Check(it.Placed.Count >= 1, $"落位登记被新一局接管了（Placed {it.Placed.Count} 个）");

                // 🔴 **2026-09-20 新增 —— 一条真 bug 的回归断言：手牌打出去的卡必须换出 3D 卡体。**
                //    「出牌是搬视图、不是重建视图」⇒ `CardView.SetFace(Board)` 是**唯一**的换场景入口，
                //    3D 卡体原本只在 `Build` 里建（= 只有「出生就在场上」那条路有）⇒
                //    **自己打出去的兵在场上是一张平面贴纸**（没有 3D 体、还露着 2D 立绘层），
                //    而督军 / AI 出的牌走 `SyncBoard` 新建、本来就有。
                //    ⚠️ **通检会被蒙过去**：下面 `13d` 那条 `badBody` 断言遍历的是「场上所有卡」，
                //    而当时场上那些卡恰好全是督军/AI 出的 ⇒ 它一直是绿的。所以这里必须**盯住刚打出去的这一张**。
                //    （同一形状见 `CLAUDE.md` 铁律 10 第 5 条：多入口的东西每个入口都要断言。）
                {
                    var dv = driver.BoardViewAt(freeSlot);
                    Check(dv != null, "刚打出去的那张卡在场上找得到（按槽位）");
                    Check(dv != null && dv.Face == CardFace.Board,
                          $"★ 换上场的这张展示场景是 `Board`（实为 {dv?.Face}）");
                    Check(dv != null && dv.Body3DVisible,
                          "★ **手牌打出去的卡也有 3D 卡体**（`SetFace` 里补建 —— 漏了就是一张平面贴纸）");
                    Check(dv != null && !dv.ArtVisible && !dv.FrameVisible,
                          "★ 换上场的这张**不再露 2D 立绘层 / 卡框**（3D 体与 2D 立绘二选一）");
                }

                // 🆕 2026-09-25 **替代行动那五张专属按钮图**（`Attack type button {Pray,Duty,Ferocity,Agenda,Oath}`）
                //    —— 原版是**运行时**给主动技能按钮的 `buttonIcon` 赋的（场景里那格 `m_Sprite` 是空的），
                //    我们原来只有一张自己挑的占位图。五张已导进 `Resources/Art/ui/`。
                //    ⚠️ **缺一张只会静默回落到占位图**（`ApplyOptionIcons` 判空就保持原样）⇒ 这里必须钉住。
                {
                    CardArt.Load();
                    var iconPairs = new[]
                    {
                        new { Kw = KeywordTable.Duty,     Oath = false, Tex = "Attack_type_button_Duty" },
                        new { Kw = KeywordTable.Pray,     Oath = false, Tex = "Attack_type_button_Pray" },
                        new { Kw = KeywordTable.Ferocity, Oath = false, Tex = "Attack_type_button_Ferocity" },
                        new { Kw = KeywordTable.Agenda,   Oath = false, Tex = "Attack_type_button_Agenda" },
                        new { Kw = (string)null,          Oath = true,  Tex = "Attack_type_button_Oath" },
                    };
                    foreach (var pr in iconPairs)
                    {
                        Check(AttackSelector.AltIconFor(pr.Kw, pr.Oath) == pr.Tex,
                              $"★ `{pr.Tex}` 的映射对得上（实得 '{AttackSelector.AltIconFor(pr.Kw, pr.Oath)}'）");
                        Check(CardArt.Ui(pr.Tex) != null,
                              $"★ `{pr.Tex}` 的图**在工程里**（缺了会静默回落到占位图，画面上看不出来）");
                    }
                    Check(AttackSelector.AltIconFor(null, false) == null,
                          "★ 没有替代行动的卡 ⇒ 返回 null（落回占位图），不是随便给一张");
                }

                // 🆕 2026-09-25 **真的换到按钮上了吗** —— 摆一个带 `Duty` 的单位、弹一次选择器，
                //    技能那一格必须是那张职责图。
                //    ⚠️ 这一段和上面那段是**两件事**：上面只验「映射表 + 图在不在」，
                //      这一段验「`Show` 有没有把图刷上去」—— 漏掉 `ApplyOptionIcons` 时上面照样全绿。
                {
                    var dutyCard = new CardDef("Probe_Duty", "Probe_Duty", "unit",
                                               "Duty: Deal 3 damage to an enemy troop",
                                               null, "Test", 1, 2, 3, 0,
                                               new[] { KeywordTable.Duty });
                    int ds = -1;
                    for (int s = 0; s < BoardSpec.Size; s++)
                        if (s != BoardSpec.WarlordSlot && c2.Players[0].Board[s] == null) { ds = s; break; }
                    if (ds >= 0)
                    {
                        c2.Players[0].Board[ds] = new UnitState(dutyCard, false) { Exhausted = false };
                        driver.RefreshAll();
                        bool opened = driver.SimulateOpenCommand(ds);
                        Check(opened, "★ 带 `Duty` 的探针单位点得开攻击方式选择器");
                        string got = null;
                        if (opened && driver.selector != null)
                            foreach (var o in driver.selector.Options)
                                if (o.Kind == AttackKind.Ability) got = o.IconName;
                        Check(got == "Attack_type_button_Duty",
                              $"★ 技能那一格**真换成了职责那张专属图**（实得 '{got}'）"
                            + " —— 只在映射表里对不算，得真刷到按钮上");
                        // 📸 留一张图：断言管「用的是哪张」，这张管「印出来什么样」
                        Step(0.10f);
                        Shot(cam, "31_替代行动按钮_职责");
                        driver.SimulateDeselect();
                        c2.Players[0].Board[ds] = null;
                        driver.RefreshAll();
                    }
                    else Debug.Log(P + "   （没有空位摆 `Duty` 探针，跳过按钮换图检查）");
                }

                // 🆕 2026-09-25 **残骸体**（原版 `RemnantBody3D <阵营>`）——
                //    引擎里那一格是残骸时，场上要**盖一具残骸体**（灵族 = 一枚漂浮的灵魂石 /
                //    死灵 = 一张碎裂的卡）并把**原卡卡身关掉**（原版 `BattleCardUI.CreateRemnantBody`
                //    → `RemnantBody.BodyVisibilityToggle` → `ToggleBody3D(false)`）。
                //    ⚠️ 引擎那一半（死亡 → 留残骸 → 点击收集 → +1 颗）在 `RuleEngineTest` 里另有 **25 条**
                //      （2026-09-25 实跑：2966 → 2991；含 AI 收集那 5 条与两条事件约定），这里只管**画没画出来**。
                //    ⚠️ **不能拿场上一张现成的卡改 `IsRemnant`** —— `RemnantPrefabOf` 的判据是
                //      **卡上的关键词**（`Waystone.` → 灵族那具 · `Remnant.` → 死灵那具），
                //      随便一张兵两个都没有 ⇒ 一具都盖不出来（第一版就是这么红的）。
                //      所以这里**造两具合成残骸**，一具一个阵营，正好把「关键词 → 哪一具」也钉住。
                {
                    var free = new List<int>();
                    for (int s = 0; s < BoardSpec.Size; s++)
                        if (s != BoardSpec.WarlordSlot && c2.Players[0].Board[s] == null) free.Add(s);

                    if (free.Count >= 2)
                    {
                        var vA = ProbeRemnantView(driver, c2, free[0], KeywordTable.Waystone,
                                                 "RemnantBody3D Aeldari");
                        Check(vA != null && vA.RemnantBodyVisible, "★ 残骸那一格**盖上了残骸体**");
                        Check(vA != null && !vA.Body3DVisible,
                              "★ 残骸那一格**原卡卡身被关掉**（原版 `ToggleBody3D(false)`）");
                        if (vA != null && vA.RemnantBodyRoot != null)
                        {
                            Check(vA.RemnantBodyRoot.name == "RemnantBody3D Aeldari",
                                  $"★ 路标石残骸盖的是**灵族那具**（实得 '{vA.RemnantBodyRoot.name}'）");
                            // 🔴 **归位**：那两个 prefab 的根停在 `x = 100`（原版那套「后台位置」，
                            //    同族见 `WarpforgeEffectPlayer.Play` 的注释）⇒ 不把 localPosition 归零
                            //    就是**整具残骸体画在场景外面**，而且一声不响。量的是**渲染真值**。
                            float d = Vector3.Distance(vA.RemnantBodyRoot.transform.position,
                                                       vA.transform.position);
                            Check(d < 0.001f,
                                  $"★ 残骸体**归位到卡上**（偏差 {d:F5}；不归零会是 100 上下）");

                            // 🔴 **缩放 = 复制原卡卡身的**（原版 `CreateRemnantBody` 末尾那段：
                            //    取 `BattleCardUI.minion3DRenderer`（= `Card 3D` 节点）的
                            //    `transform.localScale` 再 `set_localScale(remnantRoot, 它)`）。
                            //    量的是**世界缩放**，比的是卡身自己那个值 —— 不是我们自己的常量。
                            var ws = vA.RemnantBodyRoot.transform.lossyScale;
                            var bs = vA.Body3DWorldScale;
                            Check(bs != Vector3.zero && (ws - bs).magnitude < 1e-4f,
                                  $"★ 残骸体的缩放 = **原卡卡身那个值**（残骸体 {ws.x:F5} vs 卡身 {bs.x:F5}）");
                        }
                        else Check(false, "★ 残骸体建出来了（拿得到根节点）");

                        // 🔴 **材质真的绑上了吗** —— 2026-09-25 实拍踩过：第一版是裸 `Instantiate`，
                        //    没走 `WarpforgeEffectPlayer`（= 没跑 `WarpforgeEffectBinder`），
                        //    原版 shader 没绑上 ⇒ **整具渲成黑块**，而「在不在」那几条照样绿。
                        //    判据：每一层都要有材质、有 shader，且**不能**是 Unity 的报错 shader。
                        int mb, mbBad; string mbWhy;
                        RemnantMaterialCount(vA, out mb, out mbBad, out mbWhy);
                        Check(mb > 0 && mbBad == 0,
                              $"★ 路标石残骸体的材质**全绑上了**（{mb} 层；没绑 {mbBad} 层{mbWhy}）—— "
                            + "裸 `Instantiate`（不走 `WarpforgeEffectPlayer`）会整具渲成黑块");

                        // 🆕 2026-09-29：残骸体那圈光（原版 `FrameHighlightRemnant` = prefab 里的 `RemnantLight`）
                        //   —— 原来这层**永远是黑的**（prefab 的 `m_Sprite` 空着，而且没人驱动它）。
                        //   判据（反编译实读 `CardHighlight__SetRemnantHighlight.c`）：把**主状态环的颜色原样拷过去**。
                        Check(vA.RemnantLightSpriteName == "Glow UI W40K",
                              $"★ 残骸体那圈的 sprite 挂上了（实得 '{vA.RemnantLightSpriteName}'；原版 = `Glow UI W40K`"
                            + " 123×123 @PPU100 —— prefab 里 `m_Sprite` 原本是空的）");
                        vA.SetHighlight(CardHighlightState.ValidTarget);
                        Check(vA.RemnantLightColor.r < 0.01f && Mathf.Abs(vA.RemnantLightColor.g - 1f) < 0.01f
                              && vA.RemnantLightColor.a > 0.9f,
                              $"★ …而且**跟着状态色走**（合法目标 ⇒ 绿；实得 "
                            + $"{vA.RemnantLightColor.r:F2},{vA.RemnantLightColor.g:F2},{vA.RemnantLightColor.b:F2},{vA.RemnantLightColor.a:F2}）");
                        vA.SetHighlight(CardHighlightState.Normal);
                        Check(vA.RemnantLightColor.a < 0.01f,
                              $"…退回常规档 ⇒ **灭掉**（原版 `RegularColor` 的 alpha 就是 0；实得 {vA.RemnantLightColor.a:F3}）");

                        // 🆕 2026-09-29（Q 批第 6 条）：**残骸体那三条原版音效**（出现 / 收集 / 被打掉）。
                        //   判据 → `RemnantSfx` 文件头那张表（六条 cue ↔ 五条 clip）。
                        //   ① 表本身（含两条反直觉的：灵族「出现/收集」**共用一条 clip**；
                        //      死灵的 cue 名 `NecronsCardReanimate` **≠** clip 名 `CardReanimate`）
                        Check(RemnantSfx.ClipOf(RemnantSfx.Moment.ToRemnant, true) == "Aeldari To Waystone Death",
                              "★ 灵族 出现 → `Aeldari To Waystone Death`");
                        Check(RemnantSfx.ClipOf(RemnantSfx.Moment.Collect, true) == "Aeldari To Waystone Death",
                              "★ 灵族 收集 → **同一条 clip**（原版两条 cue 共用 ⇒ 别按 cue 名去 Resources 里找）");
                        Check(RemnantSfx.ClipOf(RemnantSfx.Moment.Death, true) == "Aeldari Waystone Destruction",
                              "★ 灵族 被打掉 → `Aeldari Waystone Destruction`");
                        Check(RemnantSfx.ClipOf(RemnantSfx.Moment.ToRemnant, false) == "CardShatter",
                              "★ 死灵 出现 → `CardShatter`");
                        Check(RemnantSfx.ClipOf(RemnantSfx.Moment.Collect, false) == "CardReanimate",
                              "★ 死灵 收集 → `CardReanimate`（**cue 名 `NecronsCardReanimate` ≠ clip 名**）");
                        Check(RemnantSfx.ClipOf(RemnantSfx.Moment.Death, false) == "RemnantsDestroyed",
                              "★ 死灵 被打掉 → `RemnantsDestroyed`");

                        //   ② 五条 clip **真能加载**（`Resources/Art/` 是 gitignore 的 ⇒ 新克隆要跑导入器）
                        var sfxNames = new System.Collections.Generic.HashSet<string>();
                        foreach (RemnantSfx.Moment mm in new[] { RemnantSfx.Moment.ToRemnant,
                                                                  RemnantSfx.Moment.Collect,
                                                                  RemnantSfx.Moment.Death })
                            foreach (bool ae in new[] { true, false }) sfxNames.Add(RemnantSfx.ClipOf(mm, ae));
                        Check(sfxNames.Count == 5, $"★ 六条 cue 去重后 = **5** 条 clip（实得 {sfxNames.Count}）");
                        foreach (var n in sfxNames)
                            Check(Resources.Load<AudioClip>("Art/audio/sfx/" + n) != null,
                                  $"★ 音效 `{n}` 加载得到（取不到就跑 `工具/import_remnant_sfx.py`）");

                        //   ③ **钩子行为**：合成一条 `Death`，而这一格**现在立着残骸** ⇒ 判成「出现」
                        RemnantSfx.ResetCounters();
                        driver.PlayRemnantSfx(new RuleEngine.BattleEvent
                        {
                            Kind = RuleEngine.EvtKind.Death, Player = 0, Slot = free[0], CardId = "Probe_Waystone"
                        });
                        Check(RemnantSfx.Played == 1 && RemnantSfx.LastMoment == RemnantSfx.Moment.ToRemnant,
                              $"★ 「这一格现在是残骸」的 Death ⇒ 播**出现**（实得 {RemnantSfx.LastMoment}）");
                        Check(RemnantSfx.LastClip == "Aeldari To Waystone Death",
                              $"★ …而且挑的是**灵族**那条（实得 `{RemnantSfx.LastClip}`）");

                        RemnantSfx.ResetCounters();
                        driver.PlayRemnantSfx(new RuleEngine.BattleEvent
                        {
                            Kind = RuleEngine.EvtKind.CollectWaystone, Player = 0, Slot = free[0]
                        });
                        Check(RemnantSfx.Played == 1 && RemnantSfx.LastMoment == RemnantSfx.Moment.Collect,
                              "★ `CollectWaystone` 事件 ⇒ 播**收集**");

                        //   ③·b 🆕 2026-09-29 **原版 `AudioCue` 的随机区间**（音高 / 音量 / 重播间隔）
                        //        数据 = `Resources/Art/audio/sfx/remnant_cue_props.json`
                        //        （`工具/import_remnant_sfx.py` 从 `soundcollection` 包抽的）。
                        //        ⚠️ 这半条**截图与耳朵都验不了**（批处理没有音频设备）⇒ 只能断言数值。
                        Check(RemnantSfx.PropsLoaded == 6,
                              $"★ 六条 cue 的随机区间表读到了（实得 {RemnantSfx.PropsLoaded} 张；"
                            + "取不到就跑 `工具/import_remnant_sfx.py`）");
                        var cueAe = RemnantSfx.PropsOf(RemnantSfx.Moment.Death, true);
                        Check(cueAe != null && Mathf.Abs(cueAe.minPitch - 0.8f) < 0.01f
                              && Mathf.Abs(cueAe.maxPitch - 1.24f) < 0.01f
                              && Mathf.Abs(cueAe.maxVolume - 0.3f) < 0.01f,
                              "★ 灵族那三档的区间 = 原版 `AudioCue` 的 0.8~1.24 / 音量 0.3");
                        Check(RemnantSfx.LastPitch >= 0.8f && RemnantSfx.LastPitch <= 1.24f,
                              $"★ 播的时候音高落在原版给的区间里（实得 {RemnantSfx.LastPitch:F3}，应在 0.8~1.24）");
                        Check(RemnantSfx.LastVolume > 0f, "★ 音量也是从区间里取的");

                        //   `timeToPlayAgain`：同一条 cue 在间隔内**再点一次要被挡掉**（原版就是这么节流的）
                        RemnantSfx.ResetCounters();
                        RemnantSfx.ResetThrottle();
                        RemnantSfx.Play(RemnantSfx.Moment.Death, true);
                        bool again = RemnantSfx.Play(RemnantSfx.Moment.Death, true);
                        Check(!again && RemnantSfx.Throttled == 1,
                              $"★ 同一条 cue 在 `timeToPlayAgain`(0.1s) 内再播被挡（实得 Throttled={RemnantSfx.Throttled}）");
                        RemnantSfx.ResetThrottle();
                        RemnantSfx.ResetCounters();

                        //   反例：**不是残骸的死亡一条都不该响**（不然每死一个兵都播残骸音效）
                        RemnantSfx.ResetCounters();
                        driver.PlayRemnantSfx(new RuleEngine.BattleEvent
                        {
                            Kind = RuleEngine.EvtKind.Death, Player = 0, Slot = -1, CardId = "NoSuchCard_XYZ"
                        });
                        Check(RemnantSfx.Played == 0, "★ 非残骸的死亡**不播**（反例）");
                        RemnantSfx.ResetCounters();

                        // 第二具：**死灵**那种 —— 同一个机制的另一张皮（碎裂的卡）
                        var vN = ProbeRemnantView(driver, c2, free[1], KeywordTable.Remnant,
                                                 "RemnantBody3D Necrons");
                        Check(vN != null && vN.RemnantBodyVisible,
                              "★ 死灵的残骸也盖出来了（同一个机制的另一张皮）");
                        Check(vN != null && vN.RemnantBodyRoot != null
                              && vN.RemnantBodyRoot.name == "RemnantBody3D Necrons",
                              "★ 死灵那具是 `RemnantBody3D Necrons`（**按关键词挑**，不是写死一个）");
                        {
                            int nb, nbad; string nwhy;
                            RemnantMaterialCount(vN, out nb, out nbad, out nwhy);   // 顺带把清单打进日志
                            Check(nb > 0 && nbad == 0,
                                  $"★ 死灵残骸体的材质也**全绑上了**（{nb} 层；没绑 {nbad} 层{nwhy}）");
                        }

                        // 📸 **两具并排留一张图** —— 断言管「在不在」，这张管「像不像」
                        //    （`CLAUDE.md` 铁律：改完表现层至少抽一张真值图看一眼）。
                        // ⚠️ **先 `Step` 再拍**：残骸体那一大坨几乎全是**粒子**（`Spirit Stone Idle`
                        //    外面还挂着 Appear / Embers / Ground Glow / GhostTrails…），而批处理
                        //    **没有帧循环** ⇒ 不手动 `Simulate` 的话它们停在第 0 帧，图上是「几乎空的」，
                        //    看着像残骸体没建出来（`Step` 里已经统一 `Simulate` 了）。
                        Step(0.35f);
                        Shot(cam, "29_残骸体_灵族与死灵");

                        // 📸 再留一张**收集那一下**的实拍 —— 原版那件（`WaystoneCollect`）长什么样，
                        //    断言答不了，只能看。这是「挑特效要看试片」那条规矩的落地。
                        CardEffects.FireEvent(VfxMap.CollectWaystone, vA != null ? vA.transform.position
                                                                                : Vector3.zero);
                        Step(0.30f);
                        Shot(cam, "30_收集灵魂石_特效");

                        // 🔴 **「引擎说它死了、可那一格还站着人」的接缝**（守卫在 `PlayDeathFeel` 开头）——
                        //    引擎在「变成残骸」那条路上**也会发 `EvtKind.Death`**（`CleanupDeaths` 两个分支都发）；
                        //    若播放器照常把视图溶掉，刚盖上的残骸体就**闪一下没了**。
                        //    这里**直接往 `Signals` 里塞一条 Death**（不跑伤害链，最小可复现）。
                        //
                        // 🔴🔴 **必须先打开 `animateFeel`**（`BattleDriver.animateFeel` **默认 `false`**，
                        //    是批处理自检为「当场精确的坐标」关掉的）—— 关着的时候 `PlayFeel` 整段不跑、
                        //    `PlayDeathFeel` **一次都不会被调**，这两条断言就会**测了个寂寞**
                        //    （2026-09-25 实测：正向那条**静默通过**、反例那条红 —— 正是这个症状）。
                        //    跑完**关回去**：后面那些断言依赖「当场精确」。
                        bool feelWas = driver.animateFeel;
                        driver.animateFeel = true;

                        // ⚠️ 两条都**必须在 `Step` 之前量** —— `Step` 会把消散补间也推完、
                        //    `_dying` 那时已经清空、视图也重建过了 ⇒ 量出来两边一样。
                        // ⚠️ 判据取**视图对象身份**：守卫没生效时 `PlayDeathFeel` 会 `views.Remove`，
                        //    紧接着 `SyncBoard` 会**新建一个视图** —— `RemnantBodyVisible` 照样是 true，
                        //    只有「还是不是原来那个对象」能分辨。
                        int dyingBefore = driver.DyingCount;
                        // ⚠️ **`CardId` 必须填** —— 引擎就是这么发的（`RuleCore.CleanupDeaths` 发的是 `u.Name`）。
                        //    🔴 2026-10-01 起守卫按**身份**判「那一格站着的到底是不是**同一张卡**翻面的残骸」
                        //    （`PlayDeathFeel`：连续棋盘下那一格随时会被补位上来的**别人**占掉），
                        //    不填名字会被判成「不是同一张卡」而照常消散。
                        string remnantName = c2.Players[0].Board[free[0]] != null
                                           ? c2.Players[0].Board[free[0]].Name : null;
                        c2.Signals.Add(new BattleEvent { Kind = EvtKind.Death, Player = 0, Slot = free[0],
                                                         CardId = remnantName });
                        driver.RefreshAll();
                        Check(driver.DyingCount == dyingBefore,
                              "★ **那一格还站着人（残骸）⇒ 不播阵亡消散**（否则残骸会闪一下没了）");
                        Check(ReferenceEquals(vA, driver.BoardViewAt(free[0])),
                              "★ 而且**还是原来那个视图**（被换成新建的就说明它被溶掉重建过）");
                        Step(1.5f);

                        // 撤掉：残骸被收走 / 被摧毁之后，那一格要**恢复成正常卡**
                        c2.Players[0].Board[free[0]].IsRemnant = false;
                        driver.RefreshAll();
                        Check(vA != null && !vA.RemnantBodyVisible && vA.Body3DVisible,
                              "★ 不再是残骸时**撤掉残骸体、原卡卡身回来**");

                        // 🔴 反例：**那一格真空了**时，同一条 Death 事件必须照常消散
                        //    （守卫「用格子上还有没有东西」当判据，而不是 `IsRemnant` —— 反过来会把正常阵亡也吃掉）
                        //    ⚠️ **顺序要紧**：**先把 Death 塞进队列、再清空格子**。
                        //       反过来的话 `SyncBoard` 会先把那个视图 `Kill` 掉（格子空了、又没有待播的阵亡事件）
                        //       ⇒ 那条 Death 轮到播时 `_myUnits` 里已经没有视图 ⇒ **什么都不发生**。
                        //    ⚠️ 这一步之后**不能再碰 `vA`**（视图会被销毁）。
                        int dyingBefore2 = driver.DyingCount;
                        string deadName = c2.Players[0].Board[free[0]] != null
                                        ? c2.Players[0].Board[free[0]].Name : null;
                        c2.Signals.Add(new BattleEvent { Kind = EvtKind.Death, Player = 0, Slot = free[0],
                                                         CardId = deadName });
                        c2.Players[0].Board[free[0]] = null;
                        driver.RefreshAll();
                        Check(driver.DyingCount > dyingBefore2,
                              "★ 反例：那一格**真空了** ⇒ 照常阵亡消散（守卫不能把正常阵亡也吃掉）");
                        Step(1.5f);

                        driver.animateFeel = feelWas;      // 关回去（后面靠「当场精确」）

                        // 🆕 2026-09-25 **收集那一下的接线**（`EvtKind.CollectWaystone` → 特效名 → 库里真有）：
                        //    原版收走一颗石头是**独立的一条链**（`RemnantAeldari.CollectWaystoneEffect`：
                        //    播收集音 + 在残骸原位实例化收集粒子 + 销毁残骸体），**不是**阵亡消散。
                        //    这里只验「名字接得上、库里真有这件」——播出来好不好看要看实拍
                        //    （本工程规矩：挑特效要看 `Editor/VfxPicker.cs` 的试片）。
                        string cx = VfxMap.Resolve(VfxMap.CollectWaystone);
                        Check(cx == "WaystoneCollect",
                              $"★ 收集灵魂石接的是原版同名那件 `WaystoneCollect`（实得 '{cx}'）");
                        var lib2 = WarpforgeVFX.WarpforgeEffectLibrary.Instance;
                        WarpforgeVFX.WFEffectEntry ce;
                        Check(lib2 != null && lib2.TryGet(cx, out ce) && ce.prefab != null,
                              "★ 效果库里**真有**这件（没有的话收集那一下是静默的 —— 点了没反应）");
                    }
                    else Debug.Log(P + "   （场上空位不足 2 个，跳过残骸体检查）");
                }
            }
            else Debug.Log(P + "   （开局手牌都付不起，跳过拖拽）");

            // 「按 R 再来一局」—— 面板上写着这句话，原来**没有代码接**。这里验它真的能重开。
            var beforeCtx = c2;
            driver.Restart();
            Step(0.3f);
            var c3 = driver.Ctx;
            Check(!ReferenceEquals(c3, beforeCtx), "重开一局：引擎上下文换新的了");
            // ⚠️ 第三十四轮：回合 1 的 `BeginTurn` 里**天赋**会从卡池现拿一张塞进手牌
            //    （`RuleCore.SpawnTalents`）—— 它**不属于起手**，要单独算进来，不然这里差 1 张。
            Check(c3.Turn == 1
                  && c3.Players[0].Hand.Count == RuleCore.StartHand + 1 + Conjured(c3, 0),
                  $"重开一局回到第 1 回合、手牌 {c3.Players[0].Hand.Count} 张"
                  + $"（起手 {RuleCore.StartHand} + 抽 1 + 天赋生成的 {Conjured(c3, 0)} 张）");
            Check(c3.Players[0].Warlord.Card.FromOriginalPool, "重开之后还是原版卡池");

            // ⚠️ 重开**不能叠份**：`BuildHud` 每调一次就建一套，HUD 会叠两层、
            //    而且旧的那个 `EndPanel` 会成孤儿（`_endPanel` 指向新建的那个，旧的永远不 Hide）。
            //    这条是截图抓出来的 —— 新一局已经开打，上一局的「对局结束/骷髅/按R再来一局」还压着。
            int turnLabels = 0;
            foreach (var t in driver.GetComponentsInChildren<Transform>(true))
                if (t.name == "TurnLabel") turnLabels++;
            Check(turnLabels == 1, $"重开之后 HUD 只有一份（TurnLabel ×{turnLabels}）");
            Check(driver.End != null && !driver.End.Visible, "重开之后上一局的结算面板收掉了");
            Shot(cam, "09_原版卡_开局");

            // 对手用**同一套卡池**连打几轮 —— 只打一回合的话对手可能手牌全付不起，看不出东西
            int foeUnits = 0;
            for (int round = 0; round < 4 && !c3.IsOver; round++)
            {
                driver.SimulateAiTurn();
                StepThrough(driver);
                Step(0.3f);
                foeUnits = 0;
                for (int s = 0; s < BoardSpec.Size; s++)
                    if (c3.Players[1].Board[s] != null && !c3.Players[1].Board[s].IsWarlord) foeUnits++;
                Debug.Log(P + $"   AI 第 {round + 1} 轮打完：场上单位 {foeUnits} 个，"
                            + $"能量 {c3.Players[1].Energy}，手牌 {c3.Players[1].Hand.Count} 张");
                if (foeUnits > 0) break;
            }
            Check(foeUnits > 0, $"AI 真的用原版卡池出牌了（场上 {foeUnits} 个单位）");
            Shot(cam, "10_原版卡_AI回合");
            ClearEffects();
        }


        // ---- 8b. 卡框档（🔴 2026-09-27：判据整个换了）----
        // 🔴 **原来这里是「卡框按【稀有度】分档」—— 那条映射在原版里根本不存在，整段作废。**
        //   反编译一手证据：原版 **「升级档」就是「卡框档」**（`CardTier{Tier1=0..Tier4=3}`），
        //   `CardTierUIController.SetTier` 转发到 `CardFramesSO.GetClanFrame(army, **tier**, cardType)`
        //   —— **入参里没有稀有度**；稀有度驱动的是**底部菱形宝石**（`CardRarityHolderController`），与框**并存**。
        //   ⚠️ 原那段断言（common→tier1 / legendary→tier4 / special 按阵营分档）是**照着错映射写的** ⇒
        //      **「自检替错值背书」的又一实例**（同 `资料/已知的坑.md`）；而且代码与文档早就打架
        //      （坑表已把这条映射标成「不存在」，代码只改了 `special`）。
        // **用户 2026-09-27 拍板**：本作全解锁、不做升级/合成 ⇒ **所有卡一律取该阵营的【最高档】框**。
        Debug.Log(P + "--- 卡框档（一律最高档）---");
        {
            var f1 = CardArt.Frame("Ultramarines", "common");
            var f4 = CardArt.Frame("Ultramarines", "legendary");
            Check(f1 != null && f1.name.Contains("tier" + CardArt.MaxTier),
                  $"★ common 也取**最高档**框（`{f1?.name}`）—— 稀有度不再影响卡框");
            Check(f1 == f4, $"★ common 与 legendary 拿到**同一张**框图（`{f1?.name}`）—— 这是有意的，不是漏配");
            Check(CardArt.Frame("Ultramarines", null) == f1
                  && CardArt.Frame("Ultramarines", "没这个稀有度") == f1,
                  "稀有度为空 / 不认识，也拿同一张（「不认识的退回 tier1」那条老路已废）");
            // 抽 5 个阵营 × {部队, 战术} 复核**最高档一个都不缺**（每档导了 60 份）
            int miss = 0;
            foreach (var fac in new[] { "Ultramarines", "Sororitas", "Sautekh", "AstraMilitarum", "DarkAngels" })
                foreach (var tactic in new[] { false, true })
                {
                    var t = CardArt.Frame(fac, "common", tactic);
                    if (t == null || !t.name.Contains("tier" + CardArt.MaxTier)) miss++;
                }
            Check(miss == 0, $"抽 5 阵营 × 部队/战术 ⇒ 最高档框**一个都不缺**（缺 {miss} 个）");
            // 战术卡**另一套框**（原版 troop / stratagem 分开；战术卡那张下半截是大片文字区）
            var ft = CardArt.Frame("Ultramarines", "common", true);
            Check(ft != null && ft.name.Contains("strat") && ft != f1,
                  $"战术卡用的是**另一套框**（`{ft?.name}` ≠ `{f1?.name}`）");

            // 真造一张卡，验**卡面确实按最高档取了框**（不是只有 `CardArt` 会取）
            var probe = CardView.Create(cam.transform, new CardData
            {
                id = "Aggressor Sergeant", title = "TEST", cost = 1, melee = 1, ranged = 0,
                // ⚠️ 立绘按**引擎卡 id** 取名（2026-09-15 起）—— 这里必须给 artId，
                //    只给 `id`（卡名）会取不到图（`CardArt.Portrait` 的键是 id）。
                artId = "UM_Aggressor_Sergeant",
                health = 1, armor = 0, keywords = "", isUnit = true,
                frame = BattleDriver.FactionColor("Ultramarines"), faction = "Ultramarines",
                rarity = "legendary",     // ⚠️ 与下面那颗 `GemTierProbe`(common) 成对，用来验「宝石按稀有度变」
            }, "FrameTierProbe");
            Check(probe.FrameTexture != null && probe.FrameTexture.name.Contains("tier" + CardArt.MaxTier),
                  $"★ **legendary** 的卡面用了最高档框（{probe.FrameTexture?.name}）");
            Check(probe.ArtLayerTexture != null && probe.ArtLayerTexture.name.StartsWith("art_"),
                  $"立绘那层用的是**真插图**（{probe.ArtLayerTexture?.name}）");
            // 底部那颗**稀有度宝石**：卡框档改的是框的形制，**稀有度只在这颗宝石上**（原版 `Rarity` 节点）
            Check(probe.GemTexture != null && probe.GemTexture.name.Contains("legendary"),
                  $"★ 底部稀有度宝石是 **legendary** 那张（{probe.GemTexture?.name}）—— 框与宝石是**两层**");
            var probe2 = CardView.Create(cam.transform, new CardData
            {
                id = "Aggressor Sergeant", title = "TEST2", cost = 1, melee = 1, ranged = 0,
                health = 1, armor = 0, keywords = "", isUnit = true,
                frame = BattleDriver.FactionColor("Ultramarines"), faction = "Ultramarines",
                rarity = "common",
            }, "GemTierProbe");
            Check(probe2.GemTexture != null && probe2.GemTexture.name.Contains("common")
                  && probe2.GemTexture != probe.GemTexture,
                  $"common 用的是另一张（{probe2.GemTexture?.name}）");
            // 🔴 **新增（2026-09-27）**：`probe2` 的稀有度是 **common** —— 它的**框**也必须是最**高档**
            //    （「框按升级档、稀有度只管宝石」这条新判据，拿一张 common 的卡来钉）。
            Check(probe2.FrameTexture != null && probe2.FrameTexture.name.Contains("tier" + CardArt.MaxTier),
                  $"★ **common** 的卡面也用了最高档框（{probe2.FrameTexture?.name}）—— 与 legendary **同档**，稀有度不影响框");
            if (Application.isPlaying) Object.Destroy(probe2.gameObject);
            else Object.DestroyImmediate(probe2.gameObject);
            if (Application.isPlaying) Object.Destroy(probe.gameObject);
            else Object.DestroyImmediate(probe.gameObject);
        }

        // ---- 9. 卡组编辑器编的那套牌 → 对战（2026-09-12 接的最后一环）----
        // 第 8 节验的是「自动凑一副原版牌也能打」；这一节验**玩家自己编的那副**能不能真的打进对局 ——
        // 也就是 `DeckLibrary` → `Begin(myDeck:)` 这条调用点，以及「不许静默替换」这条红线。
        // ⚠️ 存档必须**隔离**（`DeckStore.OverridePath` 指到临时文件），绝不碰玩家的真存档 ——
        //    做法和 `DeckScene.cs` 那一段一样。
        Debug.Log(P + "--- 卡组编辑器编的那套牌 ---");
        {
            string tmpStore = System.IO.Path.Combine(OutDir, "decks_selftest.json");
            var pool9 = CardDatabase.Load();
            string savedOverride = DeckStore.OverridePath;
            DeckStore.OverridePath = tmpStore;
            try
            {
                if (File.Exists(tmpStore)) File.Delete(tmpStore);

                // ① 一副都没编过 → 走自动凑，且**没什么要交代的**（那是默认行为）
                var none = BattleDriver.PickSavedDeck(out _);
                Check(none == null, "存档里一副卡组都没有 → PickSavedDeck 给 null（走自动凑）");
                driver.BeginFromDeckLibrary();          // 按 Play 走的就是这一条
                Step(0.3f);
                Check(driver.DeckNotice == "" && driver.HintText == "",
                      "自动凑的时候提示行是空的（默认行为不用跟玩家交代）");

                // ② 合法卡组：**真的用上了**；战术卡 2026-09-12 起**能打了** ——
                //    能解析干净的收下，解析不了的照样丢并在提示行说清张数
                const int tactics = 12;         // 故意塞 12 张战术卡
                var legal = MakeDeck(pool9, "Ultramarines", tactics, "自检·极限战士");
                var libA = DeckLibrary.Load();
                libA.Add(legal);                // 落盘，并设为「当前选中」
                Check(BattleDriver.PickSavedDeck(out _)?.Name == legal.Name,
                      $"从卡组库里读出当前选中那套：「{legal.Name}」");

                driver.BeginFromDeckLibrary();  // ★ 走**按 Play 时那条路**，不是手工传卡组
                Step(0.3f);
                var c9 = driver.Ctx;
                var src9 = driver.MyDeckSource;
                var warlord9 = CardDatabase.FindById(pool9, legal.WarlordId);
                Check(src9 != null && src9.Name == legal.Name,
                      $"开局真的读了卡组库：「{(src9 == null ? "null" : src9.Name)}」");
                Check(driver.MyFaction == "Ultramarines" && warlord9 != null
                      && warlord9.Faction == driver.MyFaction,
                      $"我方阵营跟着**督军**走 = {driver.MyFaction}（督军 {warlord9?.Name}）");
                Check(c9.Players[0].Warlord.Name == warlord9.Name,
                      $"带队的督军就是编的那张：「{c9.Players[0].Warlord.Name}」");

                // 塞进去的那 12 张里，几张能解析？（能解析的会被收下 → 上场牌数跟着变）
                int tacKept = 0, tacDropped = 0;
                foreach (var id in legal.CardIds)
                {
                    var cc = CardDatabase.FindById(pool9, id);
                    if (cc == null || cc.Type != "tactic") continue;
                    if (DeckBuilder.TacticPlayable(cc)) tacKept++; else tacDropped++;
                }
                // ⚠️ 第三十四轮：天赋从卡池现拿的那几张**不属于卡组** ⇒ 账要扣掉（见 `Conjured`）。
                int conj9 = Conjured(c9, 0);
                int inPlay = c9.Players[0].Hand.Count + c9.Players[0].Deck.Count - conj9;
                // ⚠️ 2026-09-13 第三十三轮：**防御卡现在也上场**（开局就在手里，见 `RuleCore.BuildPlayer`）
                //    ⇒ 账要多一张。这张断言以前是 `CardCount - tacDropped`，防御卡进来后就成了 30-2 而不是 29。
                // 🔴 2026-09-26：**防御卡只发【后手】**（用户指出 —— 我们原来两边都发是错的）
                //    ⇒ 这一张的账**按角色**：我方是后手才有它。
                bool iAmSecond9 = c9.FirstSeat != 0;
                int wantUnits = DeckRules.CardCount(false) - tacDropped + (iAmSecond9 ? 1 : 0);
                Check(inPlay == wantUnits,
                      $"上场的牌 = 编的 30 张 − {tacDropped} 张解析不了的战术"
                      + (iAmSecond9 ? " + 1 防御" : "（我方**先手** ⇒ 防御卡不入手）")
                      + $" = {inPlay} 张（应 {wantUnits}）"
                      + (conj9 > 0 ? $"（已扣掉天赋凭空生成的 {conj9} 张）" : ""));
                Check(c9.Players[0].Hand.Exists(x => x != null && x.Card.Type == "defence") == iAmSecond9,
                      iAmSecond9 ? "防御卡在手里（第三十三轮起它上场了；**我方是后手**）"
                                 : "★ 我方**先手** ⇒ **防御卡不在手里**（防御卡是后手的补偿，判据 → §2.8）");
                Check(tacKept > 0, $"战术卡留下了 {tacKept} 张（能解析的现在能打了，不是全丢）");
                int tacInPlay = 0;
                foreach (var card in c9.Players[0].Hand) if (card.Card.Type == "tactic") tacInPlay++;
                foreach (var card in c9.Players[0].Deck) if (card.Card.Type == "tactic") tacInPlay++;
                // ⚠️ **天赋生成的也是 `tactic`** ⇒ 按**份数**减掉。
                //    别改成「逐张判 `MarkedEphemeralCount(card) == 0`」——
                //    `CardDef` 是**共享模板**，卡组里那张同名卡会被**一起**判成生成的（多减一张）。
                tacInPlay -= conj9;
                Check(tacInPlay == tacKept,
                      $"收下的战术卡真的在牌里（手牌 + 牌库 {tacInPlay + conj9} 张 − 天赋 {conj9} = "
                      + $"{tacInPlay} 张，应 {tacKept}）");

                Check(driver.DeckNotice.Contains(legal.Name),
                      $"提示行说了用的是哪副牌：「{Short(driver.DeckNotice, 44)}」");
                // ⚠️ **2026-09-16 修：这条断言原来写的是 `Contains(tacDropped + " 张")` —— 脆的。**
                //    它靠两个巧合活着：① `tacDropped = 0` 时靠提示行里别的数字（`10 张`/`20 张`）
                //    **含子串 `0 张`** 蒙混通过；② 张数凑巧不是别人的子串。
                //    实测：把 `Angels of Death` / `Self-Destruction` 修好之后**掉 0 张**，
                //    提示行正确地**不再带那一句**，这条立刻变红 —— 而行为是**变好**了。
                //    ⇒ 改成按分支精确比对**那句话本身**。
                Check(tacDropped == 0
                        ? !driver.DeckNotice.Contains("没上场")
                        : driver.DeckNotice.Contains($"{tacDropped} 张（效果本版解析不了的战术卡）没上场"),
                      $"丢掉的张数也说清了（{tacDropped} 张解析不了的战术 —— 防御卡**不再**算丢）");
                // ⚠️ 2026-09-13 加：**提示行不许再提「防御卡」**。
                //    上一次改 `FromDeck` 收防御卡时忘了改这句文案 —— 截图里防御卡明明在手上，
                //    提示行还在说「防御卡…没上场」（**对玩家说错话比不说更糟**）。
                //    这条断言把「文案」和「取舍」钉在一起，以后再改就不会漏。
                Check(!driver.DeckNotice.Contains("防御卡"),
                      $"提示行**不再**说防御卡没上场（「{Short(driver.DeckNotice, 40)}」）");
                Check(driver.HintText == driver.DeckNotice,
                      "那句就写在提示行上 —— 开局就看得见，不用去翻日志");
                Shot(cam, "12_玩家编的卡组");

                // ③ 「按 R 再来一局」**不能把卡组弄丢**（原来 Restart 不带卡组，重开就变自动凑了）
                driver.Restart();
                Step(0.3f);
                var c9b = driver.Ctx;
                Check(ReferenceEquals(driver.MyDeckSource, src9), "重开一局：还是这副牌");
                Check(c9b.Players[0].Warlord.Name == warlord9.Name
                      && c9b.Players[0].Hand.Count + c9b.Players[0].Deck.Count
                         == wantUnits + Conjured(c9b, 0),
                      "重开一局：引擎里也还是编的那副（没悄悄退回自动凑）"
                      + $"，牌数 {wantUnits} + 天赋 {Conjured(c9b, 0)} 对得上");

                // ④ 不合法的卡组 → **明说**原因再退回，不能装作打的就是你那副
                // 🔴 **2026-09-26 换了「让它不合法」的办法**：原来靠 `bad.DefensiveId = null`
                //    （我们当年要求「恰好 1 张防御卡」），而**原版根本不校验防御卡**、我们已经改成可选
                //    （判据 → `资料/加时与冲突模式_原版规格.md` §2.7c）⇒ 那副牌现在是**合法**的。
                //    改用**超张数**（`TooManyCards`）—— 同样是我们真会拦的一种，而且下面那句
                //    「不是编的那 31 张」**从此才是真话**（原来那副其实只有 30 张，那句话是错的）。
                var bad = MakeDeck(pool9, "Ultramarines", tactics, "自检·张数超了");
                bad.CardIds.Add(bad.CardIds[0]);              // 31 张 ⇒ 超过经典上限 30
                driver.Begin(seed: 20260915, myDeck: bad);
                Step(0.3f);
                Check(driver.DeckNotice.Contains("不合法") && driver.DeckNotice.Contains("退回自动凑"),
                      $"不合法的卡组明说再退回：「{Short(driver.DeckNotice, 44)}」");
                Check(!driver.DeckNotice.Contains("本局用你编的"),
                      "退回时**不会**说成「用你编的」（说了就是骗玩家）");
                Check(driver.Ctx.Players[0].Hand.Count + driver.Ctx.Players[0].Deck.Count
                      == DeckBuilder.ClassicDeckSize - 1 + Conjured(driver.Ctx, 0),
                      $"退回的确实是自动凑的一副"
                      + $"（{driver.Ctx.Players[0].Hand.Count + driver.Ctx.Players[0].Deck.Count}"
                      + $" 张 = {DeckBuilder.ClassicDeckSize} − 督军 + 天赋生成的 {Conjured(driver.Ctx, 0)} 张，"
                      + $"不是编的那 {wantUnits} 张）");

                // ⑤ 全是战术卡 → 2026-09-12 起**能解析的都收下**（不再「只剩督军」）；
                //    解析不了的照样丢并说清张数。顺带用**超长卡组名**试截断
                //   （`Label` 是 NoWrap 的，名字长了会横着铺出屏幕）
                var longName = "自检·一副名字特别长的、全是战术卡的卡组（专门用来试截断的）";
                var allTactic = MakeDeck(pool9, "Ultramarines", DeckRules.CardCount(false), longName);
                int allDropped = 0;
                foreach (var id in allTactic.CardIds)
                {
                    var cc = CardDatabase.FindById(pool9, id);
                    if (cc != null && cc.Type == "tactic" && !DeckBuilder.TacticPlayable(cc)) allDropped++;
                }
                driver.Begin(seed: 20260916, myDeck: allTactic);
                Step(0.3f);
                // ⚠️ 2026-09-16 一并改成按分支精确比对（同上一条：`Contains(allDropped + " 张")`
                //    靠子串巧合通过，掉 0 张时提示行合理地没有那一句 ⇒ 断言误报成红）。
                Check(driver.DeckNotice.Contains("本局用你编的")
                      && (allDropped == 0
                            ? !driver.DeckNotice.Contains("没上场")
                            : driver.DeckNotice.Contains($"{allDropped} 张（效果本版解析不了的战术卡）没上场")),
                      $"全是战术卡：能解析的收下、{allDropped} 张解析不了的说明白"
                      + "（防御卡第三十三轮起不再算丢）"
                      + $"（「{Short(driver.DeckNotice, 44)}」）");
                Check(driver.DeckNotice.Contains("…") && !driver.DeckNotice.Contains(longName),
                      "超长卡组名被截断了（没整段塞进提示行）");
                Check(driver.HintWidth > 0f && driver.HintWidth < LayoutSpace.VisibleWidth * 0.9f,
                      $"那句没铺出屏幕（宽 {driver.HintWidth:F2} / 可见 {LayoutSpace.VisibleWidth:F2} 世界单位）");

                // ⑥ 玩家自己选的就是 Goff → 对手让开，别开局先打一场内战
                //   （要先显式把对手字段压成 Goff，不然验不到「撞上了」这个分支）
                var goff = MakeDeck(pool9, "Goff", 4, "自检·兽人");
                driver.Begin("Goff", "Goff", 20260917);
                driver.Begin(seed: 20260918, myDeck: goff);
                Step(0.3f);
                Check(driver.MyFaction == "Goff" && driver.FoeFaction != "Goff",
                      $"玩家选 Goff 时对手换人（不是内战）：{driver.MyFaction} vs {driver.FoeFaction}");

                // ⑦ 读的是**当前选中**的那套，不是第一套
                var libB = DeckLibrary.Load();
                var second = MakeDeck(pool9, "Goff", 2, "自检·第二套");
                libB.Add(second);                              // Add 会把它设为当前选中
                Check(BattleDriver.PickSavedDeck(out _)?.Name == second.Name,
                      $"读的是当前选中那套：「{second.Name}」");
                libB.Select(0);
                libB.Save();                                   // `Select` 自己不落盘（改的是内存里的下标）
                Check(BattleDriver.PickSavedDeck(out _)?.Name == legal.Name,
                      $"换选一套之后读到的也换了：「{legal.Name}」");

                // ⑧ 存档坏掉 → **不能装作没事**（说不出卡组就直说，别让玩家以为打的是自己那副）
                File.WriteAllText(tmpStore, "{ 这不是 json");
                string note2;
                var broken = BattleDriver.PickSavedDeck(out note2);
                Check(broken == null && !string.IsNullOrEmpty(note2),
                      $"存档坏了：读不出卡组，但带回一句人话「{Short(note2, 26)}」");
                driver.Begin(seed: 20260919, myDeck: broken, deckNote: note2);
                Step(0.3f);
                Check(driver.DeckNotice.Contains("读不出来"),
                      $"存档坏了会在提示行上说：「{Short(driver.DeckNotice, 44)}」");
                ClearEffects();
            }
            finally
            {
                DeckStore.OverridePath = savedOverride;        // 玩家的真存档路径还回去
                if (File.Exists(tmpStore)) File.Delete(tmpStore);
            }
        }

        // ---- 9b. 🆕 2026-09-26：**遭遇模式（Skirmish）真能开一局** ----
        // 起因：预组那一池 103 副里 **54 副是 12 张的遭遇牌**，在此之前点它们开不了局
        // ⇒ 预组页只敢列经典那 29 副。现在引擎侧支持了，这节验的就是**这条链通到底**：
        // 一副真预组 → `SetPendingBattleDeck` → `BeginFromDeckLibrary` → 按 `gameMode` 建局。
        // 判据（值本身的来源）→ `RuleEngine/Core/GameplayVariables.cs`（唯一出处）。
        {
            PrebuiltDecks.Deck sk = null;
            foreach (var d in PrebuiltDecks.Tab)
                if (d != null && d.gameMode == (int)GameMode.Skirmish && d.complete) { sk = d; break; }
            Check(sk != null, "预组那一池里**有遭遇模式（12 张）的牌**—— 这正是原来不敢列的那一批");
            if (sk != null)
            {
                Check(sk.cardIds != null && sk.cardIds.Length == 12,
                      $"逮到的这副遭遇预组是 **12 张**（`{sk.deckId}`，实得 "
                    + $"{(sk.cardIds == null ? -1 : sk.cardIds.Length)} 张）");
                // **走真入口**：把预组放进那条通道，然后按 Play 的入口开一局
                PrebuiltDecks.SetPendingBattleDeck(sk);
                // 🆕 2026-09-26：这一节**把「我方先手」那个钉子拔掉** ⇒ 真去走 `BattleDriver.Begin` 里那枚硬币，
                //   然后**按角色**断（下面两条都是）。判据 → `资料/加时与冲突模式_原版规格.md` §2.8。
                //   ⚠️ 同时**钉住种子**：`BeginFromDeckLibrary` 真 Play 是每局换种子的（否则硬币永远同一面），
                //      自检要可复现 ⇒ 这一节钉成「**我方先手**」，9c 钉成「**我方后手**」，
                //      于是两条路**都被确定性地覆盖**（比随硬币落在哪一面强）。
                driver.ForceFirstSeat = null;
                driver.ForceSeed = SeedForFirstSeat(0);
                driver.BeginFromDeckLibrary();
                Step(0.3f);

                Check(driver.Vars.IsSkirmish,
                      "**模式跟着预组走进了对局**（`BattleDriver.Vars.IsSkirmish`）"
                    + "—— 判据是预组数据里的 `gameMode`，不是另设一个开关");
                Check(driver.Ctx.Vars.deckSize == 12,
                      $"引擎拿到的是**遭遇那套参数**（卡组 {driver.Ctx.Vars.deckSize} 张，应为 12）");
                int hpSk = driver.Ctx.Players[0].Warlord.MaxHealth;
                int handSk = driver.Ctx.Players[0].Hand.Count;
                int enSk = driver.Ctx.Players[0].MaxEnergy;
                bool mullSk = driver.Ctx.MulliganOpen;
                int startHandSk = driver.Ctx.Vars.startingHand;   // ⚠️ **必须在 `Begin` 之前存下来**
                // 督军生命：判据是 **−10**（原版文案 `Warlords start with 10 less Health`），
                // ⚠️ **不是「等于 20」** —— 督军底血各卡不同（这一副是 35）⇒ **拿卡面血量当基准**。
                // ⚠️ 基准要取**对局自己那个督军的 `CardDef`**（`UnitState.Card`）——
                //    拿 `heroId` 回卡池查会**查到另一张卡**（实测：查出来 40 血、实际用的是 35 血的），
                //    那是「按 id 反查」在卡池里撞了同名/同 id 的坑，**别用**（这一版踩过）。
                var wlCard = driver.Ctx.Players[0].Warlord.Card;
                Check(wlCard != null && hpSk == wlCard.Health - 10,
                      $"遭遇：督军（{wlCard.Name}）生命 = **卡面 {wlCard.Health} − 10 = {wlCard.Health - 10}**（实得 {hpSk}）"
                    + "—— 原版文案 `Warlords start with 10 less Health`，**是「少 10」不是「等于某个固定值」**");
                // 🔴 **2026-09-26：防御卡现在只发【后手】**（用户指出 —— 我们原来两边都发是错的），
                //   而**谁先手是投硬币决定的**（`BattleDriver.Begin` 里的 `firstSeat`；
                //   用户 2026-09-26 拍板「一律投硬币」，等价于「所有督军的 `initiative` 相同」）⇒ **这两条断言必须按角色写**，
                //   不能再写死「+2 张 / 3 点」。
                bool meFirst = driver.Ctx.FirstSeat == 0;                 // 我方（座位 0）是不是先手
                int conjSk = Conjured(driver.Ctx, 0);                     // 天赋「凭空生成」的那几张（开局就在手/牌库里）
                int extraSk = driver.Ctx.Vars.secondExtraCards;           // 后手补偿（`ScenarioVariables.secondExtraCards`，默认 1）
                int handExpect = startHandSk + 1 + conjSk + (meFirst ? 0 : 1 + extraSk);
                Check(handSk == handExpect,
                      $"遭遇：手牌 = 起手 {startHandSk} + 首回合抽 1"
                    + (conjSk > 0 ? $" + 天赋生成 {conjSk}" : "")
                    + (meFirst ? "（我方**先手** ⇒ **没有防御卡**、也没有后手补偿）"
                               : $" + **防御卡 1** + **后手补偿 {extraSk}**（我方是**后手**）")
                    + $" = {handExpect}（实得 {handSk}）"
                    + "—— 判据 → `资料/加时与冲突模式_原版规格.md` §2.8");
                Check(!mullSk, "遭遇：**没有换牌阶段**（原版文案 `No mulligan`）—— 弹窗与引擎两边都得压住");
                var firstSk = driver.Ctx.Players[driver.Ctx.FirstSeat];
                Check(firstSk.MaxEnergy == 3,
                      $"遭遇：**先手**第 1 回合 **3 点**能量（原版文案 `P1 3 Energy P2 4 Energy` 里先手那个数），"
                    + $"实得 {firstSk.MaxEnergy}（先手 = {firstSk.Name}）");

                // 回到经典，免得把后面那些节留在遭遇模式下（它们是按 30 张的账写的）
                driver.Begin(BattleDriver.DefaultFactionA, BattleDriver.DefaultFactionB, 20260926);
                Step(0.3f);
                ClearEffects();
                Check(!driver.Vars.IsSkirmish, "验完**退回经典**（后面的自检按经典那套账写）");
            }
        }

        // ---- 9b-2. 🆕 2026-10-06（A147）：**遭遇局开局里程碑 = `x0`**（原版是**事件驱动**、不是按生命反推）----
        // 判据（`d:/2/tools/decomp_full/`，三个方法体都在）：
        //   · `BattleScoreUiManager__Initialize.c` 收尾 `UpdateMilestonesCount(0xffffffff)` ⇒ 文案
        //     `System_String__Format("x{0}", index + 1)` = **`x0`**（`__UpdateMilestonesCount.c:13`）；
        //   · 之后只有 `BattleScoreManager__CheckThresholds.c:22`（跟着「敌方督军生命变化」那条信号）
        //     才会把它往上调，且 `AlreadyAccomplished` **只置位、不清零**（已达成不回退）。
        // 🔴 **为什么要专门开一节**：**经典局盖不住这一格** —— 56 个督军起始生命 ∈ {25,30,35,40}，
        //   `SkullsFor` 全给 0，两种口径**同值**；只有**遭遇局**（`SkirmishWarlordLifeChange = -10`）
        //   才有起始生命 ≤ 20 的督军，旧实现那时会显示 `x1`。所以这一节的**前提本身也是断言**
        //   （找不到那样的督军 ⇒ 当场红，不静默跳过）。
        {
            var pool147 = CardDatabase.Load();
            // 找「自动凑牌时挑中的那个督军」起始生命 ≤ 30 的阵营（−10 之后 ≤ 20，才踩得到那一格）。
            // ⚠️ 判据与 `MakeDeck` **逐字一致**：取**该阵营在卡池里出现的第一个 `hero`** ——
            //    换一种挑法就可能挑到 35/40 血的那个，前提就不成立了。
            string lowFoe = null; CardDef lowFoeHero = null;
            {
                var seenHeroF = new HashSet<string>();
                foreach (var c in pool147)
                {
                    if (c == null || c.Type != "hero" || seenHeroF.Contains(c.Faction)) continue;
                    seenHeroF.Add(c.Faction);
                    if (lowFoe == null && c.Health - 10 <= 20) { lowFoe = c.Faction; lowFoeHero = c; }
                }
            }
            Check(lowFoe != null && lowFoeHero != null,
                  "（前提）卡池里有一个「遭遇局起始生命 ≤ 20」的督军阵营 —— 没有的话这一节验不到 A147 那一格");
            if (lowFoe != null && lowFoeHero != null)
            {
                // 两边都用这一副（镜像局）—— 只为把**敌方督军**钉成那个低血督军；
                // `foeFaction:` 必须显式传（`ResolveDeck` 是按 `_foeFaction` 那副池子校验卡组的，
                // 不传就会**静默退回自动凑**、前提当场落空）。
                var sk147 = MakeDeck(pool147, lowFoe, 0, "自检·A147 遭遇", (int)GameMode.Skirmish);
                Check(sk147 != null && sk147.CardIds.Count == 12,
                      $"（前提）凑出一副 12 张的遭遇牌（阵营 {lowFoe}）");
                if (sk147 != null)
                {
                    driver.Begin(seed: 20261006, myDeck: sk147, foeDeck: sk147, foeFaction: lowFoe,
                                 vars: GameplayVariables.For(GameMode.Skirmish));
                    Step(0.3f);
                    // ⚠️ **HUD 的刷新在 `RefreshAll()` 里**（`AdvanceTimeline` 不刷 —— 见 `RefreshAll` 里那句
                    //   「批处理里没有 Update() 循环，HUD 得在这里刷」）⇒ 读 `SkullScoreText` 前必须先刷一次，
                    //   否则读到的是**建标签时的初始文本**（那会让这条断言变成恒真/恒假的假尺子）。
                    driver.RefreshAll();
                    int foeStartHp = driver.Ctx.Players[1 - driver.MyIndex].Warlord.MaxHealth;
                    int byHp = DeckRules.SkullsFor(foeStartHp);
                    Check(driver.Vars.IsSkirmish && foeStartHp == lowFoeHero.Health - 10,
                          $"（前提）遭遇局：敌方督军（{lowFoeHero.Name}）起始生命 = {lowFoeHero.Health} − 10 "
                        + $"= {foeStartHp} ≤ 20 —— 正是「按生命反推」会算出 ≥1 颗的那一档");
                    Check(byHp >= 1,
                          $"（前提）按**旧口径**（`SkullsFor(最低生命)`）这里本该显示 `x{byHp}` —— 所以下面那条**不是恒真**");
                    Check(driver.SkullScoreText == "x0",
                          $"★ **开局 = `x0`**（原版 `Initialize` 无条件写 `x0`；实得「{driver.SkullScoreText}」）"
                        + " —— 一次都还没打到敌方督军 ⇒ **0 颗**（事件驱动 + 已达成不回退，不是按当前生命反推）");

                    // 🔴 **A148 的那一半**：结算面板与对局记录**必须拿同一个数**（不是各自再按生命反推一遍）。
                    //   开局就投降（双方督军一点血都没掉）⇒ 面板/记录都是 **0 颗**；旧口径按最低生命反推会给
                    //   `SkullsFor({foeStartHp})` = **1 颗** ⇒ 这条在旧实现下必红。
                    //   ⚠️ 记数必须在 `Forfeit()` **之前** —— 那一下自己就走 `UpdateHud` 把记录写了。
                    int logBefore147 = BattleLogData.Count;
                    // 🆕 **2026-10-11（批次 · A375）**：日常那个计数也要在 `Forfeit()` **之前**记 ——
                    //   那一下会走结算那一处把骷髅送进日常（`DailyData.OnBattleEnd`）。
                    int skullDaily147 = DailyData.SkullsCountValue();
                    driver.Forfeit();                       // 走真入口（`RecRaw` 记账那一处，不是直调 `RuleCore`）
                    Check(driver.End != null && driver.End.Visible && driver.End.ShownSkulls == 0,
                          $"★ 遭遇局**开局就结束** ⇒ 结算面板 **0 颗**（实得 "
                        + $"{(driver.End == null ? -1 : driver.End.ShownSkulls)}；旧口径按生命反推会给 {byHp} 颗）"
                        + " —— 面板拿的是**已达成档数**，与 HUD 同一格字段");
                    Check(BattleLogData.Count == logBefore147 + 1
                          && BattleLogData.All[0] != null && BattleLogData.All[0].OwnSkulls == 0,
                          "★ ……对局记录里我方骷髅也是 **0**（与面板同源；旧口径会记 1）");
                    // 🆕 **2026-10-11（A375）**：**送到日常那一份也必须是 0**。
                    //   🔴 **这一条才是分开「新口径 / 旧口径」的那条**：9b-3 那节用的是经典局
                    //   （起始血 25~40），`_foeSkullCount` 与 `SkullsFor(最低生命)` **同值** ⇒ 那边分不出来；
                    //   只有「遭遇局起点血 ≤ 20」这一格，两种口径才**分道**（旧口径这时会送 {byHp} 颗过去）。
                    //   改坏法：把 `BattleDriver` 结算处那个实参从 `_foeSkullCount` 换成
                    //   `DeckRules.SkullsFor(_foeWarlordMinHp)` ⇒ 本条红（本条也是**唯一**会红的那条）。
                    Check(DailyData.SkullsCountValue() == skullDaily147,
                          $"★ ……**日常骷髅计数也一颗不加**（{skullDaily147} → {DailyData.SkullsCountValue()}；"
                        + $"旧口径这时会 +{byHp}）—— 与上面两条合起来，把「已达成档数」这个口径钉到**四处同源**"
                        + "（HUD 的 `x N` / 结算面板 / 对局记录 / **日常计数**）");

                    // 回到经典，免得把后面那些节留在遭遇模式下（它们是按 30 张的账写的）
                    driver.Begin(BattleDriver.DefaultFactionA, BattleDriver.DefaultFactionB, 20261006);
                    Step(0.3f);
                    ClearEffects();
                    Check(!driver.Vars.IsSkirmish, "验完**退回经典**（后面的自检按经典那套账写）");
                }
            }
        }

        // ---- 9b-3. 🆕 2026-10-11（批次 · A375）：**一局打完 ⇒ 骷髅进日常计数**
        //   —— 这条线**原来整条是断的**（日常那个计数是个出厂 mock，没有「一局结束 → 累加」这条路）。
        // 原版链路（`d:/2/tools/decomp_full/`，逐环亲读；全文 → `资料/普查产出_1011/R1_每日骷髅与登录卡.md` §二）：
        //   `ChallengeLogMgr__LogMatchEnd.c` —— `BattleEndSignal___ctor(signal, matchData, gameMode,
        //   **BattleScoreManager__GetSkullCount(manager + 0xF8)**, isWin)`，那个 int 落在 `SkullsCount`（`@0x18`）；
        //   → `SkullsCount__OnBattleEnd.c` 末句 `MissionChallenge__UpdateProgress(this, signal.SkullsCount, **0**, 0)`；
        //   → `…DisplayClass35_0___UpdateProgress_b__0.c`：`shouldOverride == false ⇒ value + currentValue`（**累加**）。
        // ⚠️ **面板/HUD 那个「几颗」就是该进日常的那一个数** —— 它是 `_foeSkullCount`
        //   = 原版 `GetSkullCount()` 的同一格字段（已达成档数），而它自己是用 `DeckRules.SkullsFor` 算的
        //   ⇒ 本节**两样都断**：① 面板显示几颗（按生命算出来的档数）② 日常计数**恰好**涨这么多。
        //   ⛔ 两边**不许各算各的**（那正是 9b-2 那节 · A148 修掉的那个病）。
        // 🔴 **两态 + 断的是【累加后】的值**：`≤20 ⇒ 1 颗` 与 `≤10 ⇒ 2 颗` 各一条，
        //   记账一律写成「这一节的起点 + N」—— 只断「变没变」分不出「+1 / +2 / +3」，
        //   只断「+1」也分不出「累加」与「每局都置成 1」。
        // 🔴 第 ③ 段是**边界那一格**：`21 ⇒ 0 颗`（与 ① 的 `20 ⇒ 1 颗` 合起来才钉得住原版是
        //   `health <= threshold` 而不是 `<`）；它同时是「0 个那一局计数不动」的那一态。
        // **改坏法**（三条）：① 把 `BattleDriver` 结算处那两个实参删回去（仍调 3 参版）⇒ 编译不过（签名变了）；
        //   ② 把那句里的 `_foeSkullCount` 换成写死的 `0` ⇒ 本节「+1 / +2」两条红；
        //   ③ 换成 `_foeWarlordMinHp`（A147 之前的**旧口径** `SkullsFor(最低生命)`）——
        //   ⚠️ **本节三段照样全绿**（经典局起始血 25~40，两种口径在这些值上同值）⇒ 分开这两种口径的
        //   **不是本节**，而是**上面 9b-2 那节新加的那条**（遭遇局开局 `x0` ⇒ 日常计数也 +0；旧口径那时会 +1）。
        {
            int baseA375 = DailyData.SkullsCountValue();      // 本节起点（收工还原）
            // ⚠️ 座位**每局 `Begin` 之后重取一次** —— `MyIndex` 就是 `_me`，而它是在 `Begin` 里定下来的
            //   （`BattleDriver.cs:1625`）。在 `Begin` 之前读，拿到的是**上一局**留下的那个座位。
            int foeSeatA375 = 0;

            // ---------------- ① 削到 **20** ⇒ 过第 1 档 ⇒ 1 颗
            driver.Begin(BattleDriver.DefaultFactionA, BattleDriver.DefaultFactionB, 20261011);
            foeSeatA375 = 1 - driver.MyIndex;
            Step(0.3f);
            driver.RefreshAll();      // 原版 `Initialize` 用**当前生命**播种缓存值（这一步不算「变化」）
            Check(driver.Ctx.Players[foeSeatA375].Warlord.Health > 20,
                  $"（前提）敌方督军起始生命 {driver.Ctx.Players[foeSeatA375].Warlord.Health} > 20 —— "
                + "经典局没有生命增减（25~40），所以下面「削到 20」是一次**真的下降**");
            driver.Ctx.Players[foeSeatA375].Warlord.Health = 20;
            driver.RefreshAll();      // 生命**变了** ⇒ 走原版那条 `CheckHealth` 信号
            Check(driver.SkullScoreText == "x1",
                  $"（前提）HUD 里程碑 = `x1`（实得「{driver.SkullScoreText}」）—— 20 ≤ 阈值 20");
            driver.Forfeit();         // 走真入口结算（`Forfeit` → `RefreshAll` → `UpdateHud` → 结算那一处）
            Check(driver.End != null && driver.End.ShownSkulls == 1,
                  $"（前提）结算面板 1 颗（实得 {(driver.End == null ? -1 : driver.End.ShownSkulls)}）");
            Check(DailyData.SkullsCountValue() - baseA375 == 1,
                  "★ 一局打完 ⇒ **日常骷髅计数 +1**（敌方督军削到 20 ⇒ 过第 1 档；阈值 = `{20,10,0}`）"
                + $" —— 计数 {baseA375} → {DailyData.SkullsCountValue()}。A375 之前这条链**根本没接**"
                + "（那边只会推三条每日任务，骷髅计数一个数都不动 ⇒ 骷髅卡恒 `x160`）");

            // ---------------- ② 削到 **10** ⇒ 过两档 ⇒ 再 +2（累加，不是「每局都置 1」）
            int base2A375 = DailyData.SkullsCountValue();
            driver.Begin(BattleDriver.DefaultFactionA, BattleDriver.DefaultFactionB, 20261012);
            foeSeatA375 = 1 - driver.MyIndex;
            Step(0.3f);
            driver.RefreshAll();
            driver.Ctx.Players[foeSeatA375].Warlord.Health = 10;
            driver.RefreshAll();
            Check(driver.SkullScoreText == "x2",
                  $"（前提）HUD 里程碑 = `x2`（实得「{driver.SkullScoreText}」）—— 10 ≤ 20 且 10 ≤ 10");
            driver.Forfeit();
            Check(driver.End != null && driver.End.ShownSkulls == 2,
                  $"（前提）结算面板 2 颗（实得 {(driver.End == null ? -1 : driver.End.ShownSkulls)}）");
            Check(DailyData.SkullsCountValue() - base2A375 == 2,
                  "★ 第二局削到 10 ⇒ 过两档 ⇒ 计数 **+2**（**是 2，不是 1、也不是 3** —— "
                + "`+1` 分不出「+1 与 +2」，`+3` 分不出「两档与三档」）"
                + $" —— 计数 {base2A375} → {DailyData.SkullsCountValue()}");

            // ---------------- ③ 边界 + 0 那一态：削到 **21** ⇒ 一档都不过 ⇒ +0
            int base3A375 = DailyData.SkullsCountValue();
            driver.Begin(BattleDriver.DefaultFactionA, BattleDriver.DefaultFactionB, 20261013);
            foeSeatA375 = 1 - driver.MyIndex;
            Step(0.3f);
            driver.RefreshAll();
            Check(driver.Ctx.Players[foeSeatA375].Warlord.Health > 21,
                  "（前提）敌方督军起始生命 > 21（否则「削到 21」那一下不算下降）");
            driver.Ctx.Players[foeSeatA375].Warlord.Health = 21;
            driver.RefreshAll();
            Check(driver.SkullScoreText == "x0",
                  $"★ 21 只比阈值 20 **大 1** ⇒ **一档都不过**（实得「{driver.SkullScoreText}」）—— "
                + "与 ① 的 `20 ⇒ x1` 合起来钉住原版是 **`health <= threshold`**，不是 `<`");
            driver.Forfeit();
            Check(driver.End != null && driver.End.ShownSkulls == 0,
                  $"（前提）结算面板 0 颗（实得 {(driver.End == null ? -1 : driver.End.ShownSkulls)}）");
            Check(DailyData.SkullsCountValue() - base3A375 == 0,
                  "★ 这一局一颗都没拿到 ⇒ 日常计数**一个数都不加**（0 个也走同一条链，不是「不加就跳过」）"
                + $" —— 计数仍是 {DailyData.SkullsCountValue()}");

            // ---------------- 还原（别把本节打出来的账留在工作区 —— 后面几节按经典那套账写）
            DailyData.ForceSkullsCountForTest(baseA375);
            Check(DailyData.SkullsCountValue() == baseA375, "（还原）骷髅计数回到本节起点那个值");
            driver.Begin(BattleDriver.DefaultFactionA, BattleDriver.DefaultFactionB, 20261006);
            Step(0.3f);
            ClearEffects();
        }

        // ---- 9c. 🆕 2026-09-26：**玩家自建的遭遇卡组也能开一局** ----
        // 9b 验的是**预组**那条路（模式从预组数据的 `gameMode` 来）。这一节验的是**玩家自己的卡组**：
        // 「编辑器存 → `PlayerDeck.GameMode` **落盘** → 从磁盘读回来 → 按这副牌的模式开局」。
        // 在此之前**这条路是断的**：`RecentDecks` 里没有模式字段，`BeginFromDeckLibrary` 对手编卡组
        // **恒按经典**开（12 张的牌会被当成 30 张开，牌库两回合抽干）。
        // 判据（模式为什么挂在卡组上）→ `资料/加时与冲突模式_原版规格.md` §2.7。
        {
            string tmpSk = System.IO.Path.Combine(OutDir, "decks_skirmish_selftest.json");
            var poolSk = CardDatabase.Load();
            string savedOverrideSk = DeckStore.OverridePath;
            // 🆕 2026-09-26：这一节也**拔掉「我方先手」那个钉子** ⇒ 走真硬币，然后按角色断（见下面 ②/③）。
            //   ⚠️ 种子**钉成「我方后手」** —— 于是「先手没有防御卡 / 后手有」这条规则的两面都被覆盖到
            //      （9b 是「我方先手」，这一节是「我方后手」）。
            driver.ForceFirstSeat = null;
            driver.ForceSeed = SeedForFirstSeat(1);
            DeckStore.OverridePath = tmpSk;
            try
            {
                if (File.Exists(tmpSk)) File.Delete(tmpSk);

                // 找一个**同时有督军和防御卡**的阵营（`MakeDeck` 两样都要，缺一样这副牌就不合法）
                string fac = null;
                {
                    var heroF = new HashSet<string>();
                    var defF = new HashSet<string>();
                    foreach (var c in poolSk)
                    {
                        if (c == null) continue;
                        if (c.Type == "hero") heroF.Add(c.Faction);
                        else if (c.Type == "defence") defF.Add(c.Faction);
                    }
                    foreach (var c in poolSk)
                        if (c != null && c.Type == "hero" && defF.Contains(c.Faction)) { fac = c.Faction; break; }
                }
                Check(fac != null, "找一个同时有督军和防御卡的阵营（凑一副合法的遭遇牌要用）");

                var skDeck = fac != null ? MakeDeck(poolSk, fac, 0, "自检·自建遭遇牌", (int)GameMode.Skirmish) : null;
                Check(skDeck != null && skDeck.CardIds.Count == 12,
                      $"凑出来的是一副 **12 张**的遭遇牌（实得 {(skDeck == null ? -1 : skDeck.CardIds.Count)} 张，"
                    + $"阵营 {fac}）—— `MakeDeck` 现在**按模式算张数**，不是写死 30");
                Check(skDeck != null && skDeck.IsSkirmish,
                      "这副牌的 `PlayerDeck.GameMode` = 13（遭遇）—— **模式在建组那一刻就打上去**");
                if (skDeck != null)
                {
                    Check(DeckRules.Validate(skDeck, id => CardDatabase.FindById(poolSk, id), true) == DeckError.None,
                          "它是**照遭遇规则合法**的（12 张 · 1 督军 · 1 防御 · 同名上限）");

                    // ⚠️ 存档必须隔离（绝不碰玩家的真存档）—— 与第 9 节同一条规矩
                    var libSk = DeckLibrary.Load();
                    libSk.Add(skDeck);            // 落盘 + 设成「当前选中」

                    // ① **落盘这一环单独验**：再从磁盘读一份回来，模式还在不在
                    var backSk = DeckLibrary.Load();
                    Check(backSk.Current != null && backSk.Current.IsSkirmish
                          && backSk.Current.GameMode == (int)GameMode.Skirmish,
                          "**模式落盘了**：重新从磁盘读回来，这副牌还是遭遇"
                        + $"（`GameMode` = {(backSk.Current == null ? -999 : backSk.Current.GameMode)}，应为 13）");

                    // ② 走**按 Play 时那条真路**：预组通道空着 ⇒ `PickSavedDeck` ⇒ 按牌自己带的模式开
                    PrebuiltDecks.ClearPendingBattleDeck();
                    driver.BeginFromDeckLibrary();
                    Step(0.3f);
                    Check(driver.Vars.IsSkirmish,
                          "**玩家自建的遭遇卡组也能开出遭遇局**（`BattleDriver.Vars.IsSkirmish`）"
                        + "—— 判据是**这副牌自己**的 `GameMode`，不是另设的开关");
                    Check(driver.Ctx.Vars.deckSize == 12,
                          $"引擎拿到的是遭遇那套参数（卡组 {driver.Ctx.Vars.deckSize} 张，应为 12）"
                        + "—— 在此之前这条路恒按经典 30 张开");

                    // ②b 🔴 **「打的是不是这副牌」** —— 这一条是补的，而且**非补不可**：
                    //     上面 ② 那两条只量了**模式**和**参数**，量不到「用的是哪副牌」。
                    //     实况（2026-09-26 抓到）：`ResolveDeck` 校验时**没传模式** ⇒ 这副 12 张的遭遇牌
                    //     被按经典的 30 张判 ⇒ `TooFewCards` ⇒ **静默换成自动凑的 30 张**，
                    //     而上面两条断言**照样全绿**。判据 → `资料/加时与冲突模式_原版规格.md` §2.7。
                    {
                        string probe = skDeck.CardIds[0];
                        bool usedIt = false;
                        foreach (var ci in driver.Ctx.Players[0].Hand)
                            if (ci != null && ci.Card != null && ci.Card.Id == probe) usedIt = true;
                        foreach (var ci in driver.Ctx.Players[0].Deck)
                            if (ci != null && ci.Card != null && ci.Card.Id == probe) usedIt = true;
                        Check(usedIt,
                              "★ **打的就是这副牌**（牌库里找得到它带的卡）—— 只验模式/参数是**不够**的："
                            + "那副 12 张牌曾经被按经典判、悄悄换成自动凑的 30 张，而模式那两条照样绿");
                    }

                    // ③ 🆕 2026-09-26：**卡组没带防御卡 ⇒ 本局补一张 + 提示行说清楚**
                    //    判据（唯一）→ `资料/加时与冲突模式_原版规格.md` §2.7c：
                    //    原版 `DeckUtility.ValidateDeck` **不校验防御卡**（0 张合法），
                    //    兜底在 `BattleManager.AddGoesSecondCardToDeck`（从防御卡池随机抽一张进手牌，
                    //    **卡组带了就用卡组那张**）。
                    {
                        var lib2 = DeckLibrary.Load();
                        Check(lib2.Current != null && !string.IsNullOrEmpty(lib2.Current.DefensiveId),
                              "（前提）这副自建遭遇牌**原本带着**防御卡");
                        lib2.Current.DefensiveId = null;          // 故意拿掉
                        lib2.Save();

                        driver.BeginFromDeckLibrary();
                        Step(0.3f);
                        Check(driver.DeckNotice != null && driver.DeckNotice.Contains("没带防御卡"),
                              "★ 没带防御卡的卡组：**提示行告诉玩家「补了一张」**（手里多一张没编过的牌，"
                            + "不说清楚他会以为是 bug）—— 实得「" + driver.DeckNotice + "」");
                        // ⚠️ **补的那张进不进手牌，看「我方是不是后手」**（防御卡只发后手，见 `RuleCore.BuildPlayer`）
                        //   ⇒ 这里两条断言都按角色写：**后手 ⇒ 手牌里正好 1 张**；**先手 ⇒ 手牌里没有**
                        //     （但那张卡**确实被补进来了** —— 靠上面的提示行 + `FromDeck` 的日志证明）。
                        int defInHand = 0;
                        bool defSameFaction = false;
                        foreach (var ci in driver.Ctx.Players[0].Hand)
                        {
                            if (ci == null || ci.Card == null || ci.Card.Type != "defence") continue;
                            defInHand++;
                            if (DeckRules.SameFaction(ci.Card.Faction, driver.MyFaction)) defSameFaction = true;
                        }
                        bool meSecond = driver.Ctx.FirstSeat != 0;
                        if (meSecond)
                        {
                            Check(defInHand == 1,
                                  $"★ ……而且那张防御卡**真的进了手牌**（实得 {defInHand} 张 · "
                                + "我方是后手 ⇒ 应该拿到；`RuleCore.BuildPlayer` 的防御卡分流）");
                            Check(defSameFaction,
                                  "★ ……补的那张是**本阵营**的（跨阵营的牌在我们引擎里上不了场）");
                        }
                        else
                        {
                            // ⚠️ 先手那一侧**整张都不进对局**（原版叫「分离防御卡」）——
                            //    补进来的那张照旧属于这副牌，只是**这一局它是先手，所以不持有**。
                            Check(defInHand == 0,
                                  $"★ **我方是先手 ⇒ 手牌里【没有】防御卡**（实得 {defInHand} 张）"
                                + "—— 防御卡是**后手的补偿**，先手不该有（用户 2026-09-26 指出我们原来两边都发是错的）");
                            Check(driver.DeckNotice != null && driver.DeckNotice.Contains("补了一张"),
                                  "……而提示行照旧说清了「本局补了一张」（补的是**这副牌**，谁持有看谁后手）");
                        }
                    }

                    // 回到经典，免得把后面那些节留在遭遇模式下
                    // ⚠️ 同时**把两个钉子装回去** —— 这一节拔掉了它们（9c 开头），
                    //   后面的「回合流程」自检都写死了「我方在第 1 回合行动」，不装回去会连锁红。
                    driver.ForceFirstSeat = 0;
                    driver.ForceSeed = null;
                    driver.Begin(BattleDriver.DefaultFactionA, BattleDriver.DefaultFactionB, 20260926);
                    Step(0.3f);
                    ClearEffects();
                    Check(!driver.Vars.IsSkirmish, "验完**退回经典**（后面的自检按经典那套账写）");
                }
            }
            finally
            {
                DeckStore.OverridePath = savedOverrideSk;
                try { if (File.Exists(tmpSk)) File.Delete(tmpSk); } catch { /* 自检里删不掉无所谓 */ }
            }
        }

        // ---- 10. 战术卡：**真的用鼠标拖出去打**（端到端）----
        // 第 9 节验的是「战术卡进得了牌组」、`RuleEngineTest` 验的是「引擎打得出去」——
        // 中间那段**拖拽路径**（`CardInteraction.ResolveDrop` 认不认敌方半场、
        // `OnCardDeployed` 走不走战术分支）只有真拖一次才算验过。
        Debug.Log(P + "--- 战术卡：鼠标拖出去打 ---");
        {
            var poolT = CardDatabase.Load();
            CardDef pickT = null;
            foreach (var c in poolT)
            {
                if (c == null || c.Type != "tactic" || c.Cost > 3) continue;
                if (!DeckBuilder.TacticPlayable(c)) continue;
                var opsT = EffectText.Parse(c.Desc, out _, out _);
                if (EffectText.PickSide(opsT) != "enemy") continue;
                bool allDeal = opsT.Count > 0;
                foreach (var o in opsT) if (o.Verb != "deal") allDeal = false;
                if (!allDeal) continue;
                pickT = c; break;
            }
            Check(pickT != null, "找到一张「只打敌方单位」的低费原版战术卡");

            var dkT = pickT == null ? null : DeckWithTactic(poolT, pickT.Faction, pickT.Name);
            Check(dkT != null, pickT == null ? "（没挑到卡）" : $"凑出一副带「{pickT.Name}」的合法卡组");
            if (dkT != null)
            {
                CardTween.Mode = DG.Tweening.UpdateType.Manual;
                // ⚠️ `NewBattle` 默认**洗牌**（种子定死 → 同一 seed 可复现，但那张战术卡**第几回合**
                //    抽到是跟着 seed 变的）。所以这里**逐个种子试**：抽到手 + 能量够就开打。
                //    不写死一个 seed 是因为牌序随卡组内容变 —— 写死了下次改卡池这条就红。
                bool ready = false;
                int tacIdx = -1, tgt = -1;
                BattleContext cT = null;
                for (int attempt = 0; attempt < 8 && !ready; attempt++)
                {
                    driver.Begin(seed: 20260921 + attempt, myDeck: dkT);
                    Step(0.3f);
                    cT = driver.Ctx;
                    for (int round = 0; round < 14 && !cT.IsOver; round++)
                    {
                        tacIdx = -1;
                        for (int i = 0; i < cT.Players[0].Hand.Count; i++)
                            if (cT.Players[0].Hand[i].Card.Name == pickT.Name) { tacIdx = i; break; }
                        if (tacIdx >= 0 && cT.Players[0].Energy >= pickT.Cost) { ready = true; break; }
                        driver.SimulateEndTurn();
                        driver.SimulateAiTurn();
                        Step(0.3f);
                    }
                }
                Check(ready, $"试了几个种子之后，「{pickT.Name}」到手且付得起（第 {cT.Turn} 回合）");

                if (ready)
                {
                    // 挑个敌方目标：优先部队；没有就督军（`an enemy` **含督军** —— 引擎自检里钉过）
                    for (int s = 0; s < BoardSpec.Size; s++)
                    {
                        var u = cT.Players[1].Board[s];
                        if (u != null && !u.IsWarlord) { tgt = s; break; }
                    }
                    if (tgt < 0) tgt = BoardSpec.WarlordSlot;
                    var tgtUnit = cT.Players[1].Board[tgt];
                    int hpBefore = tgtUnit.Health;
                    int handBefore = cT.Players[0].Hand.Count;
                    int energyBefore = cT.Players[0].Energy;

                    var view = driver.HandViewAt(tacIdx);
                    var tgtPos = eBoard.DropTargetWorld(tgt);
                    it.SimulateHover(view.transform.position);
                    Step(0.05f);
                    it.SimulatePress(view.transform.position);
                    Step(0.2f);
                    for (int i = 0; i < 24; i++) { it.SimulateDrag(tgtPos, 1f / 30f); Step(1f / 30f); }
                    it.SimulateRelease(tgtPos);
                    // 打出动画走完才触发 `OnDeployed`（引擎调用在回调里）—— 推到「手牌真的少一张」
                    for (int i = 0; i < 90 && cT.Players[0].Hand.Count >= handBefore; i++) Step(1f / 30f);
                    Step(0.2f);

                    Check(cT.Players[0].Hand.Count == handBefore - 1,
                          $"战术卡离开了手牌（{handBefore} → {cT.Players[0].Hand.Count}）");
                    Check(cT.Players[0].Energy < energyBefore,
                          $"扣了费（{energyBefore} → {cT.Players[0].Energy}）");
                    Check(cT.Players[0].Discard.Count > 0, "进了弃牌堆");
                    Check(tgtUnit.Health < hpBefore,
                          $"目标真的挨了打（{tgtUnit.Name} {hpBefore} → {tgtUnit.Health}）");
                    Check(driver.HandCount == cT.Players[0].Hand.Count,
                          $"画面手牌 {driver.HandCount} == 引擎手牌 {cT.Players[0].Hand.Count}（视图销毁了）");
                    Shot(cam, "13_战术卡打出去");
                }
            }
        }

        // ---- 11. 造牌 / 免费部署：**画面真的跟上了没有**（定点驱动）----
        // 第 10 节验的是「拖得出去、引擎结算了」；引擎那边 `RuleEngineTest` 查得很密
        // （手牌张数 / 场上槽位 / 池子内容 / 同种子可复现）。**中间那段一直没人验**：
        // 造出来的牌**画面会不会给它建视图**（`SyncHand` 按名字配、配不上的新建）、
        // 部署出来的单位**画面上有没有那一格**。这一节就是补这一段 ——
        // 判据是 `driver.HandCount == 引擎手牌张数`（视图和引擎不同步时这两个数会分叉）。
        Debug.Log(P + "--- 造牌 / 免费部署：画面跟上没有 ---");
        {
            var poolC = CardDatabase.Load();

            // ① 造牌：`Mercurial Host`（帝皇之子 4 费）→ `Create 3 random Combat Elixir in your hand`
            DriveTacticAndCheck(driver, it, cam, poolC, "Mercurial Host", pBoard, eBoard, "14_造牌_3张战斗药剂",
                Check, (c, before) =>
                {
                    // ⚠️ **手牌上限**（`RuleCore.HandMax` = 10）：超出的直接进弃牌堆。
                    //    2026-09-13 第三十三轮防御卡进手牌之后起手多一张 ⇒ 这条链**正好撞上限**，
                    //    所以净增不是 +2 而是「夹到上限」，3 张药剂里有 1 张直接进了弃牌堆。
                    int expectHand = System.Math.Min(RuleCore.HandMax, before.Hand + 2);
                    Check(c.Players[0].Hand.Count == expectHand,
                          $"造牌：手牌 −1 打出去 +3 造出来 → {c.Players[0].Hand.Count}"
                          + $"（{before.Hand} + 2 被上限 {RuleCore.HandMax} 夹住）");
                    int elixirs = 0, elixirsOut = 0;
                    foreach (var h in c.Players[0].Hand)
                        if (h.Card.Subtype == "Combat Elixir" || h.Card.Subtype == "Elixir") elixirs++;
                    foreach (var h in c.Players[0].Discard)
                        if (h.Card.Subtype == "Combat Elixir" || h.Card.Subtype == "Elixir") elixirsOut++;
                    Check(elixirs + elixirsOut == 3,
                          $"3 张战斗药剂**都造出来了**（手牌 {elixirs} + 弃牌堆 {elixirsOut} —— 溢出的那张进弃牌堆）");
                    Check(driver.HandCount == c.Players[0].Hand.Count,
                          $"**画面手牌 {driver.HandCount} == 引擎手牌 {c.Players[0].Hand.Count}**"
                          + "（造出来的牌画面也建了视图）");
                });

            // ② 免费部署：`Skyborne Deployment`（赛姆汉 1 费）→ `Deploy two Storm Guardian`
            DriveTacticAndCheck(driver, it, cam, poolC, "Skyborne Deployment", pBoard, eBoard, "15_部署_两个风暴守卫",
                Check, (c, before) =>
                {
                    int now = 0, occupied = 0;
                    for (int s = 0; s < BoardSpec.Size; s++)
                    {
                        var u = c.Players[0].Board[s];
                        if (u != null) occupied++;
                        if (u != null && u.Name == "Storm Guardian") now++;
                    }
                    Check(now == 2, $"部署：场上多了 2 个风暴守卫（原本占 {before.OnBoard} 格，现在 {occupied} 格）");
                    Check(occupied == before.OnBoard + 2, "……而且真的占了 2 个新格位");
                    Check(driver.HandCount == c.Players[0].Hand.Count,
                          $"画面手牌 {driver.HandCount} == 引擎手牌 {c.Players[0].Hand.Count}");
                });
        }

        // ---- 12. 投降（原版 `BattleResult.Forfeit`；2026-09-12 用户点名要的）----
        // ⚠️ 走的是**公开方法** `BattleDriver.Forfeit()`（按键那条路只是临时入口，
        //    原版没找到投降按钮）。断言四件事：判负、记下是谁投的、结算面板出得来、副标题说清是投降。
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            // 🔴 **A195**：原来是 `if (drv != null)`、**没有 `else`** ⇒ 找不到 driver 时下面整节（含 ★ 判据）静默跳过、
            //    section 照样绿。形状照同族那几处（§15 ⑧ / §15b / §15c / §17 早就是这写法）：条件不成立**当场红**。
            if (drv == null) Check(false, "找不到 BattleDriver");
            else
            {
                drv.Begin("Ultramarines", "Goff", 20260912);
                var c = drv.Ctx;
                Check(c.Winner == 0, "（投降前）对局进行中");
                c.Players[drv.MyIndex].Warlord.Health = 30;

                drv.Forfeit();

                Check(c.ForfeitedBy == drv.MyIndex, "记下是「我」投降的");
                Check(c.Winner == 1 - drv.MyIndex + 1, "投降 → **对手**胜（不看我方督军血量）");
                Check(c.Players[drv.MyIndex].Warlord.Health > 0,
                      "投降时我方督军还活着（不是被打死的）");
                var end2 = drv.End;
                Check(end2 != null && end2.Visible, "结算面板弹出来了");
                Check(end2 != null && end2.ResultText == "失败", "结算标题是「失败」");
                Check(end2 != null && end2.SubText != null && end2.SubText.Contains("投降"),
                      $"副标题写明是投降（现在：`{(end2 == null ? "<无面板>" : end2.SubText)}`）");
                // 结算面板的**层序**：内容要盖在卡上面、压暗要盖住所有卡（含手牌）。
                // ⚠️ 这两条都踩过：压暗原来在 z=0，而手牌在 z 0~0.24（更远）→ 手牌整排没被压暗、
                //    亮着从面板底下透出来。截图一眼能看出来，断言能防它再犯。
                var ep2 = Object.FindObjectOfType<EndPanel>();
                if (ep2 != null)
                {
                    var dimT = ep2.transform.Find("dim");
                    float dimZ = dimT != null ? dimT.position.z : 0f;
                    float handMaxZ = float.MinValue;
                    foreach (var cv in Object.FindObjectsOfType<CardView>())
                        if (cv.name.StartsWith("Hand"))
                            handMaxZ = Mathf.Max(handMaxZ, cv.transform.position.z);
                    Check(dimT != null && dimZ < handMaxZ,
                          $"压暗层在最靠前（dim z={dimZ:F2} < 手牌最远 z={handMaxZ:F2}）");
                    var subT = ep2.transform.Find("content/end_sub");
                    Check(subT != null && subT.position.z < handMaxZ,
                          $"结算副标题盖在手牌上面（z={(subT != null ? subT.position.z : 0f):F2} < {handMaxZ:F2}）");
                }
                Shot(cam, "16_投降结算");

                // 🆕 2026-09-27：**客机视角（`_me = 1`）同一条链要整体翻过来** —— 结算面板用 `_me` 判胜负
                //   （`BattleDriver._endPanel.Show(Ctx.Winner, _me, …)`；`EndPanel` 里
                //   `ResultText = winner == myIndex + 1 ? "胜利" : "失败"`）。
                //   批处理里验不到「两个 Unity 真连」，但**这条映射**必须钉住：
                //   否则客机那边赢了会显示「失败」（而且只有真连一次才看得出）。
                {
                    drv.SetMySeat(1);
                    drv.Begin("Ultramarines", "Goff", 20260914);
                    var c1 = drv.Ctx;
                    Check(c1.Winner == 0, "（客机那一局·投降前）对局进行中");
                    c1.Players[1].Warlord.Health = 30;

                    drv.Forfeit();

                    Check(c1.ForfeitedBy == 1, "★ 客机视角：投降记的是**座位 1**（`_me` 那一方）");
                    Check(c1.Winner == 1, "★ 客机视角：座位 0 胜（绝对座位，不翻）");
                    Check(drv.End != null && drv.End.ResultText == "失败",
                          $"★ 客机视角：我（座位 1）投降 ⇒ 面板显示**「失败」**（实得「{(drv.End == null ? "<无面板>" : drv.End.ResultText)}」）");
                    Check(drv.End != null && drv.End.SubText != null && drv.End.SubText.Contains("我方"),
                          $"★ 副标题说的是**我方**投降（现在：`{(drv.End == null ? "<无面板>" : drv.End.SubText)}`）"
                          + "（`EndPanel` 按 `forfeitedBy == myIndex` 判的，所以这一条同时在验座位没翻错）");
                    Shot(cam, "16b_投降结算_客机视角");

                    drv.SetMySeat(0);        // 🔴 **还原**（后面每一节都按座位 0 写）
                }
            }
        }

        // ---- 13. 回合时钟（原版 `ClockManager`；2026-09-12 用户点名要的）----
        // 数值出处：`DefaultScenario.json:19-21`（60 / 10 / 15 秒）、`ClockManager__GetTotalTime.c`、
        // `ClockManager__Update.c:110-141`（超时 → 15 秒倒计时 → `EndTurnClick(true)` 自动结束回合）。
        // 批处理下没有帧循环，所以**手动按秒推表**（`TickClockForTest`），不是等真实时间。
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            // 🔴 **A195**：原来是 `if (drv != null)`、**没有 `else`** ⇒ 找不到 driver 时下面整节（含 ★ 判据）静默跳过、
            //    section 照样绿。形状照同族那几处（§15 ⑧ / §15b / §15c / §17 早就是这写法）：条件不成立**当场红**。
            if (drv == null) Check(false, "找不到 BattleDriver");
            else
            {
                drv.Begin("Ultramarines", "Goff", 20260913);
                // 🔴 2026-09-17：时长**照原版的三步结构算**（`TurnSecondsForThisMatch` =
                // ScenarioVariables 默认 → 缩时标志 → **matchType 覆盖**）。本局 matchType=80(EventAI)
                // ⇒ 覆盖成 **240 s**（`ClockManager__GetTotalTime.c`；常量 `0x1834b31c4` 复核过 = 240.0）。
                float total = drv.TurnSecondsForThisMatch;
                Check(Mathf.Abs(total - 240f) < 0.01f,
                      $"本局时长 = 原版 GetTotalTime 算出来的 {total:F0} s（matchType {drv.matchType} = EventAI ⇒ 240）");
                Check(drv.ClockCountingDown == false, "开局不在倒计时那一段");
                Check(Mathf.Abs(drv.ClockLeft - total) < 0.01f, $"开局表是满的（{drv.ClockLeft:F1} s）");
                string mss = $"{Mathf.FloorToInt(total) / 60}:{Mathf.FloorToInt(total) % 60:00}";
                Check(drv.ClockText == mss, $"时钟写着 `{drv.ClockText}`（m:ss）");

                drv.TickClockForTest(total - 35f);  // 走到剩 35 s（原版 `timeToHurryUp`）
                Check(Mathf.Abs(drv.ClockLeft - 35f) < 0.01f, $"推 {(int)(total - 35f)} s 后剩 {drv.ClockLeft:F1} s");
                Check(drv.ClockText == "0:35", $"时钟写着 `{drv.ClockText}`");

                drv.TickClockForTest(34f);          // 只剩 1 s
                drv.TickClockForTest(2f);           // 越过 0 → 进那 15 秒倒计时
                Check(drv.ClockCountingDown, "总时长走完 → 进了倒计时那一段");
                Check(Mathf.Abs(drv.ClockLeft - 15f) < 0.01f, $"倒计时从 15 s 起（现在 {drv.ClockLeft:F1}）");
                Check(drv.Ctx.Active == drv.MyIndex, "……这时**还没**结束回合（倒计时那 15 秒还在玩家手里）");

                drv.TickClockForTest(16f);          // 倒计时走完 → 自动结束回合
                Check(drv.Ctx.Active != drv.MyIndex, "倒计时走完 → **自动结束回合**（原版 EndTurnClick(true)）");
                Check(drv.Ctx.Winner == 0, "……但没有额外惩罚：对局还在打");
                Shot(cam, "17_回合时钟");
            }
        }

        // ---- 14. 设置面板 → 投降（**原版就是这么进的**）----
        // 出处：`BattleSettingsWindow.cs:9` `resignButton`；入口 `SettingsBtn`（权威表 x[1808.0,1871.9] y[9.2,73.1]）。
        // 第 12 节验的是「投降这个动作」，这一节验的是「**从哪个门进去**」—— 少一个都不算还原。
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            // 🔴 **A195**：原来是 `if (drv != null)`、**没有 `else`** ⇒ 找不到 driver 时下面整节（含 ★ 判据）静默跳过、
            //    section 照样绿。形状照同族那几处（§15 ⑧ / §15b / §15c / §17 早就是这写法）：条件不成立**当场红**。
            if (drv == null) Check(false, "找不到 BattleDriver");
            else
            {
                drv.Begin("Ultramarines", "Goff", 20260914);
                var sp = drv.Settings;
                Check(sp != null && !sp.Visible, "设置面板平时是关着的");
                Check(sp != null && sp.HasArt, "面板的图都取到了（底 / 圆形关闭钮 / 投降钮 / 难度钮）");
                // 🔴 2026-09-19 用户口径：**先用英文**（原版就是 `Resign`；中文查不到），彻底翻译留到后面统一做。
                Check(sp != null && sp.ResignText == "Resign", $"投降按钮上写着「{(sp == null ? "" : sp.ResignText)}」");
                // ⚠️ 战斗日志与结算副标题（`RuleCore.Forfeit` / `EndPanel`）**仍然是中文** ——
                //    它们不属于这次改的这处 UI，等「彻底的完全翻译」那一轮一起过。

                Check(drv.SettingsBtnReady, "右上角设置按钮：贴图是 `UI_Settings_Icon`、命中矩形认自己");
                Check(drv.SimulateOpenSettings() && sp.Visible, "点设置按钮 → 面板打开");

                // ---- 对手难度（🆕 2026-09-17：原版没有这个入口，旋钮在 `AIBotsConfig` 里）----
                // 取值照原版 `DeckDifficultyLevel`（SuperEasy=0 / Easy=5 / Normal=10 / Hard=15）。
                Check(sp.DifficultyText == SettingsPanel.DifficultyName(drv.aiDifficulty),
                      $"难度按钮上写着当前档「{sp.DifficultyText}」（默认 {drv.aiDifficulty}）");
                Check((int)AiDifficulty.Normal == 10 && (int)AiDifficulty.Hard == 15,
                      "四档取值照原版 `DeckDifficultyLevel`（Normal=10 / Hard=15）");
                var dw = sp.DifficultyWorldPos;
                Check(sp.HitDifficulty(dw), "难度按钮的命中判定打得中");
                var diffBefore = drv.aiDifficulty;
                drv.SettingsClickAt(dw);
                Check(drv.aiDifficulty != diffBefore,
                      $"★ 点一下 → 换了一档（{diffBefore} → {drv.aiDifficulty}）");
                Check(sp.DifficultyText == SettingsPanel.DifficultyName(drv.aiDifficulty),
                      $"按钮上的字跟着换了（现在是「{sp.DifficultyText}」）");
                // 转一圈回到原档（四档刚好一圈）—— 证明是**循环**而不是只往一个方向走
                for (int i = 0; i < 3; i++) drv.SettingsClickAt(dw);
                Check(drv.aiDifficulty == diffBefore, "★ 再点三下 → 转一圈回到原档（四档循环）");
                Shot(cam, "18_设置面板");

                // ---- 14b2. **A276**：投降钮那一层的 z 口径（与同父的关闭钮/投降文字**同一套**）----
                // 现场：`Battle/SettingsPanel.cs` 原来写的是 `Z − 0.01 − transform.position.z`
                //   （= 「把**世界** z 落到 Z−0.01」那套口径），而同父的 `_close`（`Z−0.01`）·
                //   `_resignText`（`Z−0.02`）都是裸局部 z ⇒ 同一父节点下两套口径并存。
                // ⚖️ 调度台裁定 = **收口径**（选项 (a)；判据全文 → `资料/普查产出_1010/V4b_三件口径.md` §Q2），
                //   改成裸局部 z ⇒ **不是「对齐原版」**（本件各层的原版 z 关系没有判据）。
                // 判据（两态，不读被断实现的常量）：同一具面板建两遍、只挪**父节点的世界 z** 3.3 ——
                //   裸局部 z 那版不变；带 `− transform.position.z` 那版整体偏 3.3 ⇒ 红。
                // 改坏法：把那行写回带 `- transform.position.z` 的版本 ⇒ 下面这条红。
                {
                    var zr2 = new GameObject("A276Probe_set_z0");
                    var zr3 = new GameObject("A276Probe_set_z33");
                    zr3.transform.position = new Vector3(0f, 0f, 3.3f);
                    var pA = SettingsPanel.Create(zr2.transform, null, null);
                    var pB = SettingsPanel.Create(zr3.transform, null, null);
                    float rA = pA != null ? pA.ResignLocalZ : float.NaN;
                    float rB = pB != null ? pB.ResignLocalZ : float.NaN;
                    if (float.IsNaN(rA) || float.IsNaN(rB))
                        // ⚠️ **出声**跳过：投降钮那一层靠 `CardArt.Ui("40K_button")`，图缺就没建 ——
                        //    同一条件在同一次 Run 里有别处会红（本节的 `sp.HasArt`）。
                        Debug.Log(P + "   （A276：投降钮那一层没建出来（`40K_button` 取不到）⇒ 这条 z 口径不判；"
                                    + "本节 `sp.HasArt` 会先红）");
                    else
                        Check(Mathf.Abs(rA - rB) < 1e-5f,
                              $"★ 投降钮那一层与同父件**同一套 z 口径**（裸局部 z，不随面板的世界 z 变）—— "
                            + $"父件世界 z = 0 时 = {rA:F4}；挪到 +3.3 时 = {rB:F4}");
                    Object.DestroyImmediate(zr2);
                    Object.DestroyImmediate(zr3);
                }

                // ---- 14b3. 🆕 **2026-10-12（A424）**：`Auto Zoom` 那一行（原版 `BattleSettingsPanel/Auto Zoom Toggle`）----
                // 判据（全是实读 `bundle_scenes_scenes_battlearena1`）：`RectTransform_3099/2654/3268`（几何）·
                //   `MonoBehaviour_3977`（TMP `m_text = "Auto zoom"` fs42 HAlign=Left VAlign=Middle）·
                //   `_4356`（`EverguildToggle`：`m_Transition = ColorTint`、`m_Colors.m_NormalColor` = 那个绿）·
                //   `_5032`（I2 `mTerm = "Settings/Graphics/AutoZoom"`）。
                //   **期望值全是原版字面量**（面板内 px，见 `Battle/SettingsPanel.cs` 那组 `Az*` 常量），⛔ 不读被测实现。
                // 🧨 改坏法：① 勾选框中心少乘/多乘一个偏移 ⇒ 第 2 条红；② 命中区写成「整行」⇒ 第 4 条的**缝**那条红；
                //   ③ **`SettingsClickAt` 里不接这一行 ⇒ 第 6 条红（翻不动）**
                //      —— 🔴 **2026-10-12（A460）改过这里**：原来写的是「`PointerFrame` 里不接这一行」，
                //      而 A445 之后那一行**根本不走 `PointerFrame`**（它的指针入口在 `BattleDriver.SettingsClickAt`，
                //      与 Resign/Difficulty/Close 同一条）⇒ 照旧文改坏，第 6 条**不会红**（那正是 A460 修的假断言）；
                //   ④ 那一行不走 `ForceRefresh()`（只写值、不重算）⇒ 第 7 条红（= A424 的**命门**：
                //      原版战斗内这一颗是「点了立刻重算」那条路的家）；
                //   ⑤ 把那一行搬回 `SettingsPanel.PointerFrame` ⇒ 第 5 条（A445 形状那条负向断言）红。
                {
                    var azComp = drv.AutoZoom;
                    bool azWasOn424 = AutoZoom.Enabled, azWasChosen424 = AutoZoom.ChosenManually;

                    Check(sp.AutoZoomRowBuilt && sp.AutoZoomLabelText == SettingsPanel.AutoZoomLabelEn
                       && sp.AutoZoomLabelText == "Auto zoom",
                          $"★ A424：`Auto Zoom` 那一行建出来了、文字就是原版 TMP 印的那句"
                        + $"（「{sp.AutoZoomLabelText}」，**小写 z**；判据 = `MonoBehaviour_3977.json` 的 `m_text`）");

                    // ② 几何：面板内 px 原值（`RectTransform_3094…` 见 `SettingsPanel` 那组 `Az*` 常量）。
                    //    ⚠️ 只比**面板局部**的 x/y（z 是本工程自己的层序口径，没有原版判据）。
                    var pcn = sp.transform;
                    Vector3 boxWant = pcn.position + new Vector3(-245.389f / 108f, 249.4795f / 108f, 0f);
                    Vector3 labWant = pcn.position + new Vector3(-203.42f / 108f, 249.4795f / 108f, 0f);
                    var dA = sp.AutoZoomBoxWorldPos - boxWant;
                    var dB = sp.AutoZoomLabelWorldPos - labWant;
                    Check(Mathf.Abs(dA.x) < 1e-4f && Mathf.Abs(dA.y) < 1e-4f
                       && Mathf.Abs(dB.x) < 1e-4f && Mathf.Abs(dB.y) < 1e-4f,
                          $"★ A424：那一行的两块都落在**原版那个位置**上 —— 勾选框中心偏 ({dA.x:F4},{dA.y:F4}) 世界单位、"
                        + $"文字左中偏 ({dB.x:F4},{dB.y:F4})（期望 = 面板内 px (−245.389, 249.4795) / (−203.42, 249.4795)）");

                    // ③ 两张图按 `preserveAspect` **内接**进原版那格 74.0616 × 57.6656（「放得进 + 至少贴满一边」）。
                    var bd = sp.AutoZoomBoxDrawnSize * 108f;
                    var cd = sp.AutoZoomCheckDrawnSize * 108f;
                    bool Inscribe(Vector2 s) { return s.x <= 74.0616f + 0.05f && s.y <= 57.6656f + 0.05f; }
                    bool Touch(Vector2 s)
                    {
                        return Mathf.Abs(s.x - 74.0616f) < 0.05f || Mathf.Abs(s.y - 57.6656f) < 0.05f;
                    }
                    Check(Inscribe(bd) && Touch(bd) && Inscribe(cd) && Touch(cd),
                          $"★ A424：两张图都**内接**进原版那一格 74.0616 × 57.6656（= uGUI `preserveAspect`）——"
                        + $"底图实画 {bd.x:F2}×{bd.y:F2} · 勾实画 {cd.x:F2}×{cd.y:F2}（px）");

                    // ④ 命中区 = **原版那两个矩形**（勾选框 + 文字块），**两块中间那道缝不命中**。
                    Check(sp.HitAutoZoom(sp.AutoZoomBoxWorldPos) && sp.HitAutoZoom(sp.AutoZoomLabelWorldPos),
                          "★ A424：勾选框与文字那两块的命中判定都打得中（原版两块 `m_RaycastTarget = 1`）");
                    Vector3 gap = pcn.position + new Vector3(-205.8895f / 108f, 249.4795f / 108f, 0f);
                    Check(!sp.HitAutoZoom(gap),
                          "★ A424：两块**中间那道 4.94px 的缝**（原版那时打到的是面板自己）**不命中**"
                        + " —— 把命中区写成「整行」就会红");

                    // ⑤ 点一下 ⇒ 翻值 + 勾跟着亮/灭 + **当场重算**（= A424 的命门）。
                    //    🔴 **2026-10-12（A445/A460）**：那一行的指针入口已挪到**抬起**那条链
                    //    （`BattleDriver.SettingsClickAt`，与 Resign / Difficulty / Close 同一条；
                    //      原版那颗 `Auto Zoom Toggle` 是 `EverguildToggle`（继承 `Toggle`/`Selectable`）
                    //      ⇒ 走 `IPointerClickHandler`，**抬起**那一帧才触发）
                    //    ⇒ 这三条改走 `drv.SettingsClickAt(...)`（**真路**，与真鼠标同一条判定）。
                    //    ⚠️ **原来这里用 `sp.PointerFrame(…, true)` 点它 —— A445 之后那条路不再认这一行
                    //    ⇒ `capA/capB` 恒 `false` ⇒ 这条当场红**（这正是 A460 修的那条）。
                    AutoZoom.RestoreForTest(false, false);          // 开关关（内存态，⛔ 不落盘）
                    sp.Hide(); sp.Show();                           // 让勾那一层按新状态重算一次
                    Check(!sp.AutoZoomCheckShown, "（前提）开关关着 ⇒ 勾那一层不画（= 原版 `Toggle.graphic` 的显隐）");
                    int applyBefore424 = azComp != null ? azComp.FramingApplyCount : -1;
                    // ⚠️ **A445 的「形状」负向断言**：那一行**已经不归** `PointerFrame`（滑块那条）管。
                    //    把那一行搬回 `PointerFrame` ⇒ 这一条红。
                    Check(!sp.PointerFrame(sp.AutoZoomBoxWorldPos, true),
                          "★ A445：`Auto Zoom` 那一行的指针入口**不在** `SettingsPanel.PointerFrame` 里"
                        + "（它走抬起的 `BattleDriver.SettingsClickAt`，与 Resign/Difficulty/Close 同一条）"
                        + " —— 搬回 `PointerFrame` 就红");
                    bool hitA = drv.SettingsClickAt(sp.AutoZoomBoxWorldPos);   // ← 抬起那一下（一次点击 = 一次调用）
                    Check(hitA && AutoZoom.Enabled,
                          $"★ A424：点那一行 ⇒ 开关翻成**开**（指针被接住 {hitA}；实得 {AutoZoom.Enabled}）");
                    Check(sp.AutoZoomCheckShown, "★ A424：……勾跟着亮起来");
                    Check(azComp != null && azComp.FramingApplyCount > applyBefore424,
                          "★ A424：……**并且当场重算了一次**（`FindFirstObjectByType<CombatAutoZoom>().ForceRefresh()`，"
                        + $"= 原版 `BattleSettingsWindow__OnAutoZoomChanged` 那一条链；写入次数 {applyBefore424} → "
                        + $"{(azComp == null ? "无组件" : azComp.FramingApplyCount.ToString())}）"
                        + " —— 只写值不重算 ⇒ 这条红");
                    // 点第二下 ⇒ 翻回去（两态真的翻得动）
                    drv.SettingsClickAt(sp.AutoZoomBoxWorldPos);
                    Check(!AutoZoom.Enabled && !sp.AutoZoomCheckShown,
                          "★ A424：再点一下 ⇒ 翻回**关**、勾跟着灭（两态真的翻得动）");
                    // 🔴 **A445 之后「按住不放不重复翻」那半从这一层【断不出来】—— 如实记，不假称验过**：
                    //    旧断言用两次 `PointerFrame(…, true)` 模拟「按住不放」，而那一半的语义已经搬到
                    //    **驱动层的 latch** 上：`BattleDriver.HandleSettings` 是
                    //    `if (ClickedThisFrame() && !captured) SettingsClickAt(...)`，而 `ClickedThisFrame()`
                    //    是**按下沿** latch（`bool down = PointerHeld(); if (!down) { _clickLatch = false; return false; }
                    //    if (_clickLatch) return false; _clickLatch = true; … return true;`）⇒ 按住不放时它只在
                    //    第一帧为真。**`SettingsClickAt` 本身是一次点击一次调用**（它自己不带 latch）
                    //    ⇒ 从这一层**测不到**「按住不放」。`ClickedThisFrame()` / `PointerHeld()` 都不是 public
                    //    ⇒ 要保住这一条得**新增一个自检口**（本件如实记，没替它发明一个）。
                    //    🧨 改坏法（那一半，**在驱动层**）：把那句的 `ClickedThisFrame()` 换成 `PointerHeld()`
                    //    ⇒ 按住不放会每帧翻一次 —— 但**本文件这几条断言看不出来**，只能代码审查 / 真 Play。

                    // ⑥ 收尾：把玩家那一格放回去（本节的 `AutoZoom.Set` 全程 `PersistOverride` ⇒ 一个字节都没写盘）
                    //    🔴 **必须再 `ForceRefresh()` 一次**：上面那一跳真的改过战场相机的取景（`lensShift.y` +
                    //    `sensorSize.x`），而本节后面还有一大堆量**同一台相机**的断言（13d 那组「卡在屏内」）
                    //    ⇒ 不这儿放回去，那些会随「这次点到哪一档」红绿。
                    AutoZoom.RestoreForTest(azWasOn424, azWasChosen424);
                    if (azComp != null) azComp.ForceRefresh();
                    sp.Hide(); sp.Show();
                }

                // ---- 14c. 三根音量滑块（🆕 2026-09-19；原版 `BattleSettingsWindow` 的 music/SoundFX/voiceOver）----
                // 版面来源：解包逐级解父链（滑块中心 (∓2.00, 120.07/3.44/−113.18) px，轨道 561.08×12）
                //   🔴 **2026-10-05（A96）就地更正**：这里原来写「561.08×**14**」—— 那是错的（`WfSlider`
                //   当年那个 `TrackH = 14f` 也一起改了）。原版解析高 = (0.45−0.33) × 容器高 100 = **12.00**，判据见下面那条断言。
                // 数值来源：`AudioMixer.SetFloat("Volume"+组名, dB)`，见 `Core/WarpforgeAudio.cs`。
                {
                    Check(sp.SliderCount == 3, "设置面板上有三根音量滑块");
                    for (int i = 0; i < sp.SliderCount; i++)
                        Check(sp.SliderAt(i) != null && sp.SliderAt(i).HasArt,
                              $"第 {i + 1} 根滑块的三张图都取到了（轨道 / 填充 / 手柄）");

                    // ---- 🆕 A96（2026-10-05）：轨道高 = **原版解析高 12.00** ----
                    // 判据（解包实读）：`bundle_scenes_scenes_battlearena1` / `BattleSettingsPanel/Volume Sliders/
                    //   {Music,FX,Voiceover} Container`（RT `3417`/`2865`/`3297`：`m_SizeDelta=(0,100)`、
                    //   `m_LocalScale=(1,1)`）＋ 滑块 RT（`2943`/`2631`/`3329`）锚 y `0.33→0.45`
                    //   ⇒ 高 = (0.45−0.33) × 100 = **12.00**；父链无缩放 ⇒ 屏幕上就是 12.00。
                    //   复核：`python 工具/menu_dump.py bundle_scenes_scenes_battlearena1 "Volume Sliders"
                    //   --depth 2 --no-sprite` → `Music Slider … 561.08 **12.00**`（三根同值）。
                    // ⚠️ 我们原来画的 14 **两边都不是**（主菜单设置窗那三根是 13 × 0.9 = 11.7，另一个数）。
                    // 🔴 量的是**渲出来的轨道高**（`TrackWorldH` = Background 那几块 quad 的**并集高**），
                    //   不是把参数念一遍 —— 常量改了、或 `TrackRectPx` 忘了跟着 `trackH` 走，这条都会红。
                    for (int i = 0; i < sp.SliderCount; i++)
                    {
                        var sq = sp.SliderAt(i);
                        float thpx = sq == null ? 0f : sq.TrackWorldH * 108f;
                        Check(sq != null && Mathf.Abs(thpx - 12f) <= 0.3f,
                              $"★ 第 {i + 1} 根滑块的**轨道高** = {thpx:F2}px（原版 **12.00** = 行高 100 × 锚高 0.12）");

                        // ---- 🆕 A130（2026-10-06）：**`Fill` 那一层也量**（外壳侧那半见 `Editor/SettingsScene.cs`）----
                        // 🔴 补它的原因：原来**两处宿主都只量 Background**（`TrackWorldH`），而 `Battle/WfSlider.cs`
                        //   里 bg（`name: "slider_bg"`）与 fill（`name: "slider_fill"`）**同源于同一个
                        //   `TrackRectPx(trackW, trackH)`**（那两处 `MenuDraw.Nine`，入参一模一样）⇒
                        //   **只改其中一层不会红**（独立审查报出来的欠断言，与 A125② 是同一件）。
                        // 判据（两层为什么必须同高）：原版那三根 `… Slider`（`bundle_scenes_scenes_battlearena1`）
                        //   的子件 `Background` / `Fill` / `Handle` **同父同高**；而 uGUI `Slider.UpdateVisuals`
                        //   （`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Slider.cs`
                        //   的 `UpdateVisuals`）会把 `Fill` 的 `anchorMin=(0,0)`/`anchorMax=(value,1)`
                        //   ⇒ **纵向铺满容器** ⇒ 与 `Background` 同高。（原版 `Fill` 的**序列化** rect 是个
                        //   0×0 的退化值，运行时才被覆盖 —— 见 A125② 报告二·3。）
                        // ⚠️ 战斗侧的**树和外壳侧不是同一条路**：三根挂在 `SettingsPanel` 下
                        //   （`WfSlider.Create(transform, "music"/"fx"/"voice", …)`，见 `Battle/SettingsPanel.cs`）
                        //   ⇒ 按**层名** `slider_<SliderName>` → `slider_fill` 取节点。`WfSlider` 上**没有**
                        //   `Fill` 的对外访问器（`HasArt` 只判「非空 + 有子节点」、拿不到几何；`TrackWorldH`
                        //   只管 Background），而 `Battle/WfSlider.cs` **不在本件白名单** ⇒ **不改它**，用名字取
                        //   （与 A125② 在外壳侧的做法一致）。改层名会让这条红 —— 那算**可接受的契约**
                        //   （层名同样是实现的一部分），照实出声、不静默。
                        // 量法照 Background 那条：取该层九宫格子 quad 的**并集高**（`ImageQuad.WorldH`），
                        //   ⛔ 不是把参数念一遍；期望值写**字面量 `12f`**（原版解析高 = 0.12 × 容器高 100，
                        //   父链无缩放 ⇒ 屏幕上就是 12.00），⛔ 别引用 `WfSlider.TrackH`（那样常量错了也不红）。
                        Transform fillN = null;
                        if (sq != null)
                        {
                            var sroot = sp.transform.Find("slider_" + sq.SliderName);
                            if (sroot != null) fillN = sroot.Find("slider_fill");
                        }
                        if (fillN == null)
                        {
                            // ⛔ 不是 `if` 没有 `else`：找不到就当场红，并说明下面两条等于没验
                            Check(false, $"第 {i + 1} 根滑块的 `Fill` 那一层在（`slider_fill`）"
                                       + " —— 不在 = 下面两条等于没验");
                        }
                        else
                        {
                            float fh = 0f;
                            var fqs = fillN.GetComponentsInChildren<ImageQuad>(true);
                            for (int k = 0; k < fqs.Length; k++)
                                if (fqs[k] != null) fh = Mathf.Max(fh, fqs[k].WorldH);
                            Check(Mathf.Abs(fh * 108f - 12f) <= 0.3f,
                                  $"★ 第 {i + 1} 根滑块的 **`Fill` 层高** = {fh * 108f:F2}px（原版 **12.00**"
                                + "，与 `Background` 同源 ⇒ 两层必须同高）");
                            Check(Mathf.Abs(fh * 108f - thpx) <= 0.05f,
                                  $"★ 第 {i + 1} 根滑块**两层同高**（`Fill` {fh * 108f:F3}px vs `Background` "
                                + $"{thpx:F3}px，容差 0.05）—— 只改其中一层这条就红");
                        }
                    }
                    Check(WarpforgeAudio.Ready && WarpforgeAudio.ParamsOk,
                          "音频 mixer 加载得到、四个暴露参数（VolumeFX/Music/Voices/Jingles）都认得");
                    for (int i = 0; i < sp.SliderCount; i++)
                        Check(!string.IsNullOrEmpty(sp.SliderLabelAt(i)),
                              $"第 {i + 1} 根滑块有标签：「{sp.SliderLabelAt(i)}」");

                    var s0 = sp.SliderAt(0);
                    // ⚠️ 初值必须**跟着总线**（0 是错的）：`WarpforgeAudio` 那三个静态属性在 `Ensure()`
                    //    之前是 0 而不是 1 —— 建面板时若读到 0，三根滑块会全画在最左边。
                    Check(Mathf.Abs(s0.Value - WarpforgeAudio.Music) < 1e-3f
                          && Mathf.Abs(sp.SliderAt(1).Value - WarpforgeAudio.SoundFx) < 1e-3f
                          && Mathf.Abs(sp.SliderAt(2).Value - WarpforgeAudio.VoiceOver) < 1e-3f,
                          "★ 三根滑块的初值 = 总线当前值（不是恒 0）");
                    // 🔴🔴 **2026-10-07（波 8）：这条原来是「只靠 bug 才绿」的【弱断言】—— 改成两态都测得出来。**
                    //   · 它原来拿**滑块当时的值**（= 本机 `PlayerPrefs` 里存的音量，默认 1）当被测点。手柄中心
                    //     = 滑区左沿 + `m_AnchoredPosition.x`(11.99988) + 值 × 滑区宽(551.08) ⇒ **值 1 时手柄中心
                    //     落在轨道右端【之外】2 画布 px**，这时「打得中」靠的是 `WfSlider.Contains` 的**手柄那一块**
                    //     （A169 补的）、而不是轨道那一段；值 ≈ 0.5 时手柄中心落在轨道**里** ⇒ 有没有手柄那一块
                    //     都成立（**恒真**）。⇒ 换了机器 / 玩家改过音量，这条就什么都验不出来。
                    //   · ⇒ **显式把值定到 1 再测**（`fire: false` —— 自检不许改总线/存档），并补一条**负例**
                    //     （手柄右缘之外 1 画布 px ⇒ 必须打不中）——「打得中 / 打不中」两态都分辨得出来。
                    //     ⚠️ 负例的探针按**量出来的**手柄宽算（`ImageQuad.WorldW`），⛔ 不写死 22.406：
                    //     手柄边长将来若按原版改成「框被轨道撑开后的高」（34.4 / 35.4），这条仍成立。
                    s0.SetValue(1f, false);
                    Check(s0.Contains(s0.HandleWorldPos),
                          "★ 值 = 1 时指针落在音乐滑块的**手柄**上 → 命中判定打得中"
                        + "（那一刻手柄中心在轨道右端**之外** 2 画布 px ⇒ 认的正是手柄那一块）");
                    {
                        // 1 画布 px 的世界向量：**轨道的世界宽 ÷ 原版轨道宽 561.08**（自校准，别 `*108f` 硬折）
                        Vector3 perPx = (s0.RightWorld - s0.LeftWorld) / 561.08f;
                        // 手柄自己那一层（⛔ `transform.Find` 只找直接子件 ⇒ 名字写全 `slider_<名>/slider_handle`）
                        var hNode = sp.transform.Find("slider_" + s0.SliderName + "/slider_handle");
                        var hQuad = hNode != null ? hNode.GetComponent<ImageQuad>() : null;
                        Check(hQuad != null,
                              "（前提）音乐滑块的手柄那一层找得到（`slider_handle`）—— 找不到 = 下面那条负例等于没验");
                        Check(hQuad != null
                              && !s0.Contains(s0.HandleWorldPos
                                              + perPx.normalized * (hQuad.WorldW * 0.5f + perPx.magnitude)),
                              "★ 手柄右缘**之外 1 画布 px** ⇒ 打不中（负例 —— 上面那条「打得中」不是恒真；"
                            + "世界距离按手柄自己量出来的半宽 + 1 画布 px 算）");
                    }
                    bool cap = sp.PointerFrame(s0.HandleWorldPos, true);
                    Check(cap, "★ 滑块**接住了**这一下（驱动层就不会再把它当点击转给按钮）");

                    sp.PointerFrame(s0.LeftWorld, true);
                    Check(s0.Value <= 0.02f, $"★ 拖到最左 → 值 = {s0.Value:F3}（≈0）");
                    sp.PointerFrame(s0.RightWorld, true);
                    Check(s0.Value >= 0.98f, $"★ 拖到最右 → 值 = {s0.Value:F3}（≈1）");
                    Check(Mathf.Abs(WarpforgeAudio.Music - s0.Value) < 0.02f,
                          $"★ 滑块的改动**进了总线**（`WarpforgeAudio.Music` = {WarpforgeAudio.Music:F3}）");
                    sp.PointerFrame(Vector3.zero, false);        // ⚠️ **松手**再换下一根 —— 不松的话
                                                                 //    `PointerFrame` 会继续喂给**上一根**（第一次就是这么写错的）

                    // 中间一根（音效）拉一半 —— 顺带验「三根是各自独立的」
                    var s1 = sp.SliderAt(1);
                    float fxBefore = s1.Value;
                    float musicBefore = s0.Value;        // ⚠️ 先记下来 —— 拿 `s0.Value` 跟它自己比是**恒真**的
                    sp.PointerFrame(Vector3.Lerp(s1.LeftWorld, s1.RightWorld, 0.5f), true);
                    Check(Mathf.Abs(s1.Value - 0.5f) < 0.05f, $"★ 音效滑块拉到中间 → 值 = {s1.Value:F3}（≈0.5）");
                    Check(Mathf.Abs(s0.Value - musicBefore) < 1e-3f, "三根**各自独立**（动音效不影响音乐）");
                    sp.PointerFrame(Vector3.zero, false);        // 松手
                    // ⚠️ 别写 `Check(x || true, …)` 这种**恒真**的断言 —— 一直绿的断言等于没有断言。
                    //    这里要验的是「松手之后回到没在拖的状态」：拿一个**面板外**的点按下，应当接不住。
                    Check(!sp.PointerFrame(new Vector3(100f, 100f, 0f), true),
                          "松手之后不再处于拖动状态（面板外的点接不住）");

                    // 还原（自检不该改玩家的存档 —— 音乐是唯一落盘的那个）
                    sp.SliderAt(0).SetValue(1f, true);
                    s1.SetValue(fxBefore, true);
                    sp.SliderAt(2).SetValue(1f, true);
                    sp.PointerFrame(Vector3.zero, false);
                    Check(Mathf.Abs(WarpforgeAudio.Music - 1f) < 0.01f && Mathf.Abs(WarpforgeAudio.SoundFx - 1f) < 0.01f,
                          "自检结束把三档还原成满音量（不污染存档）");

                    // ---------------- 🆕 2026-10-07（波 8）：手柄的**起点** + 「指针 → 值」的映射 ----------------
                    // 🔴 补它的原因：这两样此前**一条断言都没有** —— 而 A169 报告附录把起点算成了「轨道左 + 17」
                    //   （照那个数改就会**偏离原版 5 设计 px**）。判据逐条出处 → `Battle/WfSlider.cs` 文件头：
                    //   · 手柄中心 = **滑区左沿** + `m_AnchoredPosition.x`(11.99988) + 值 × 滑区宽；
                    //   · `Handle Slide Area`：锚 (0,0)-(1,1)、`m_SizeDelta.x = −10`、`m_AnchoredPosition.x =
                    //     −4.99988`、pivot (.5,.5)。拉伸轴上的 `anchoredPosition` 从**锚矩形的中心**量起 ⇒
                    //     rect = [轨道左 + 0, 轨道右 − 10]（那个 −5 把「居中」正好抵消成「贴左」）
                    //     ⇒ 值 0 时手柄中心 = 轨道左 + **12** 设计 px（本条父链无缩放 ⇒ 画布 px 同值）。
                    //   · 取值：原版 `Slider.UpdateDrag` 的 `clickRect` 用的是 `m_HandleContainerRect`
                    //     （= 滑区）⇒ 0 在**滑区左沿**、1 在**滑区右沿**、分母 = 滑区宽 **551.08**。
                    // 期望值一律是**原版字面量**（12 / 0.25 / 1 / 551.08 / 561.08），⛔ 不引用 `WfSlider.*` 的常量。
                    {
                        var ms = sp.SliderAt(0);
                        // 1 画布 px 的世界向量 = 轨道的世界宽 ÷ 原版轨道宽 561.08（自校准；⛔ 别 `*108f` 硬折）
                        Vector3 perPx = (ms.RightWorld - ms.LeftWorld) / 561.08f;
                        if (Mathf.Abs(perPx.x) < 1e-7f)
                        {
                            Check(false, "（前提）轨道的世界 x 方向量得出来 —— 量不到 = 下面三条等于没验");
                        }
                        else
                        {
                            float before = ms.Value;
                            ms.SetValue(0f, false);            // ⚠️ `fire:false` —— 自检不改总线/存档
                            float hx0 = (ms.HandleWorldPos.x - ms.LeftWorld.x) / perPx.x;
                            Check(Mathf.Abs(hx0 - 12f) <= 0.1f,
                                  $"★ 值 0 时手柄中心 = 轨道左端 + **{hx0:F2}** 画布 px（原版 = 滑区左沿 +"
                                + " `m_AnchoredPosition.x` 11.99988 —— 滑区左沿**就是**轨道左沿，⛔ 不是 +17）");
                            // 起点与分母**一起**钉住：探针放在「滑区左沿 + 1/4 滑区宽」，值应**恰为 0.25**
                            //（起点若挪成 +5 ⇒ 0.2409；分母若换成整根轨道 561.08 ⇒ 0.2455 —— 都 > 0.002 ⇒ 红）
                            sp.PointerFrame(ms.LeftWorld + perPx * (0.25f * 551.08f), true);
                            Check(Mathf.Abs(ms.Value - 0.25f) <= 0.002f,
                                  $"★ 点在滑区 **1/4** 处 ⇒ 值 = {ms.Value:F4}（原版 `Slider.UpdateDrag`："
                                + "起点 = 滑区左沿、分母 = **滑区宽 551.08**；⛔ 不是整根轨道）");
                            sp.PointerFrame(Vector3.zero, false);                    // 松手（不松 = 下一帧还在拖它）
                            sp.PointerFrame(ms.LeftWorld + perPx * 551.08f, true);    // 滑区**右沿**
                            Check(Mathf.Abs(ms.Value - 1f) <= 0.002f,
                                  $"★ 点在滑区**右沿**（= 轨道右端 − 10 画布 px）⇒ 值 = {ms.Value:F4}（≈1）");
                            sp.PointerFrame(Vector3.zero, false);
                            ms.SetValue(before, false);                              // 还原（不 fire）
                            Check(Mathf.Abs(ms.Value - before) < 1e-4f,
                                  "（本组收尾）音乐滑块的值还原成进这一组之前的值");
                        }
                    }

                    // ---------------- 🆕 2026-10-07（波 8 · A197）：三根音量滑块的**手柄实画边长** ----------------
                    // 🔴 原来那两个调用点传的是手柄**序列化**的框高 22.406 —— 而那**不是**运行时画出来的尺寸：
                    //   uGUI `Slider.UpdateVisuals` 把手柄的 `anchorMin.y/anchorMax.y` 写成 **0 / 1**
                    //   （本机 `D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Resources/PackageManager/
                    //   BuiltInPackages/com.unity.ugui/Runtime/UGUI/UI/Core/Slider.cs:616-623`，只改**轴**那一维的锚值）
                    //   ⇒ 运行时框高 = `Handle Slide Area` 高 + `m_SizeDelta.y`(22.406)；本面板父链无缩放、
                    //   滑区高 = 轨道高 **12** ⇒ 框 12 + 22.406 = **34.406**；110×110 方图 + `preserveAspect`
                    //   取短边 ⇒ **实画边长就是 34.406**（A197 前画 22.406 ⇒ 小 35%）。
                    //   出处：`bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_2979.json`
                    //   （手柄 `m_SizeDelta = 46.811 × 22.406`）+ 上面那条 `Slider.cs` 的行号。
                    //   ⚠️ 同批的外壳侧（主菜单设置窗音频三根）是 35.406 × 0.9 = 31.87 —— **同一个算式**、
                    //   只是那边滑区高 13。期望值写字面量 **34.406**（⛔ 不引用 `WfSlider.*`：
                    //   拿被测常量当期望 = 同义反复，常量错了也不红）。
                    for (int i = 0; i < sp.SliderCount; i++)
                    {
                        var sq = sp.SliderAt(i);
                        var hn = sq != null ? sp.transform.Find("slider_" + sq.SliderName + "/slider_handle") : null;
                        var hq = hn != null ? hn.GetComponent<ImageQuad>() : null;
                        if (hq == null)
                        {
                            Check(false, $"（A197）第 {i + 1} 根滑块的 `slider_handle` 找得到"
                                       + " —— 不在 = 下面两条等于没验");
                            continue;
                        }
                        Check(Mathf.Abs(hq.WorldW * 108f - 34.406f) <= 0.3f,
                              $"★（A197）第 {i + 1} 根滑块手柄的**实画宽** = {hq.WorldW * 108f:F3}px"
                            + "（原版 = 滑区高 12 + 序列化框高 22.406 = **34.406**；"
                            + "改坏法：退回 `WfSlider.HandlePx` 那个序列化值 ⇒ 22.406 ⇒ 红）");
                        Check(Mathf.Abs(hq.WorldH * 108f - 34.406f) <= 0.3f,
                              $"★（A197）第 {i + 1} 根滑块手柄的**实画高** = {hq.WorldH * 108f:F3}px"
                            + "（110×110 方图 + `preserveAspect` ⇒ 宽高相等）");
                    }
                }

                // 走**和真实点击同一条判定**（`HitResign` → `Forfeit`），不是直接叫 Forfeit
                var w = sp.ResignWorldPos;
                Check(sp.HitResign(w), "投降按钮的命中判定打得中");
                drv.SettingsClickAt(w);
                Check(drv.Ctx.ForfeitedBy == drv.MyIndex, "从设置面板点「投降」→ 真的判了投降");
                Check(!sp.Visible, "投完面板自己收起来了");
                Shot(cam, "18b_投降之后");
            }
        }

        // ---- 13d. 场上卡按展示场景分层（🆕 2026-09-19）----
        // 🔴 原版在 inPlay 类状态把**整张 `2DCard` 关掉**（`Card2DController__Toggle.c:5-8`，
        //    由 `BattleCardUI.SetObjectVisibility` 调）⇒ 场上只剩「立绘 + 攻/血/甲 + 关键词徽标」，
        //    **没有卡框 / 费用 / 稀有度宝石 / 卡名 / 阵营行 / 兵种行 / 效果底板 / 效果文字**。
        //    三场景对照表与出处见 `CardView.CardFace` 的注释。
        // ⚠️ 这条断言特别值：**出牌是「搬视图」不是「重建视图」**（为了不丢落位动画），
        //    所以「换场景」那一步很容易漏 —— 漏了的话**督军是对的、自己打出去的兵带着卡框**，
        //    连截图都不容易一眼看出来是同一个 bug。
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            // 🔴 **A195**：原来是 `if (drv != null)`、**没有 `else`** ⇒ 找不到 driver 时下面整节（含 ★ 判据）静默跳过、
            //    section 照样绿。形状照同族那几处（§15 ⑧ / §15b / §15c / §17 早就是这写法）：条件不成立**当场红**。
            if (drv == null) Check(false, "找不到 BattleDriver");
            else
            {
                // ⚠️ 用 `drv.BoardViews()`（引擎的场上单位表），**不是** `boardRoot.GetComponentsInChildren` ——
                //    自检场景里 `boardRoot` 是整个场景根，手牌也在下面。
                var onBoard = new List<CardView>();
                foreach (var x in drv.BoardViews()) if (x != null) onBoard.Add(x);
                Check(onBoard.Count > 0, $"场上找得到卡（{onBoard.Count} 张）");
                int badFace = 0, badFrame = 0, badCost = 0, badGem = 0, badText = 0, badBody = 0, badArt = 0, withBody = 0;
                foreach (var v in onBoard)
                {
                    if (v == null) continue;
                    if (v.Face != CardFace.Board) badFace++;
                    if (v.FrameVisible) badFrame++;
                    if (v.CostVisible) badCost++;
                    if (v.GemVisible) badGem++;
                    if (v.TextBgVisible) badText++;
                    // 🔴 **2026-09-20 新增不变量：3D 体与 2D 立绘二选一**（口径同 `Build` / `SetFace`）。
                    //    两个同时露 = 同一张卡两张脸叠着；而「已经有 3D 体了却还露着 2D 立绘」
                    //    正是**手牌打出去那条路漏了换场景**的症状（`SetFace` 里补建之前就是这样）。
                    //    ⚠️ 反过来（没 3D 体 ⇒ 露 2D 立绘）**不算错** —— 那是网格/shader 取不到时的正当退回。
                    if (v.Body3DVisible && v.ArtVisible) badArt++;
                    // 🆕 2026-09-19：场上那张是**原版 3D 卡体**（`3DBody`），不是平面立绘
                    var body = v.transform.Find("body3D");
                    if (body == null) { badBody++; continue; }
                    withBody++;
                    var mr = body.GetComponent<MeshRenderer>();
                    var mf = body.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null || mr == null || mr.sharedMaterial == null
                        || mr.sharedMaterial.shader == null
                        || mr.sharedMaterial.shader.name != "CardPresentation/Card3D")
                        badBody++;
                    else
                    {
                        // 网格的三套 UV 都得在（UV1 是立绘那套 —— 丢了就贴不出立绘，而**不会报错**）
                        var mesh = mf.sharedMesh;
                        if (mesh.uv == null || mesh.uv.Length == 0 || mesh.uv2 == null || mesh.uv2.Length == 0)
                            badBody++;
                    }
                }
                Check(badFace == 0, $"★ 场上每张卡都是 `CardFace.Board`（不符 {badFace} 张）");
                Check(badFrame == 0, $"★ 场上**没有卡框**（不符 {badFrame} 张）");
                Check(badCost == 0, $"★ 场上**没有费用六边形**（不符 {badCost} 张）");
                Check(badGem == 0, $"★ 场上**没有稀有度宝石**（不符 {badGem} 张）");
                Check(badText == 0, $"★ 场上**没有效果文字底板**（不符 {badText} 张）");
                Check(badArt == 0, $"★ 场上**3D 体与 2D 立绘不同时出现**（两个都露的有 {badArt} 张）");
                // 🆕 3D 卡体（原版 `3DBody` → `Card 3D`）：在场每张卡都得有，且用的是原版网格 + 我们的 Card3D shader，
                //    而且**网格的 UV1 得在**（立绘吃它；丢了不会报错、只会贴不出立绘）
                Check(withBody == onBoard.Count && badBody == 0,
                      $"★ 场上每张卡都是**原版 3D 卡体**（有 {withBody}/{onBoard.Count} 张，其中不合格 {badBody} 张；"
                      + "网格+UV1+`CardPresentation/Card3D`）");
                // 🆕 2026-09-19：场上卡**不许**挂名字/技能/兵种行那几层 TMP ——
                //    `SetData` 原来**无条件**重建文字层，把 `SetFace(Board)` 的隐藏覆盖掉了
                //    （症状：卡下面露出一行名字，3D 卡体变矮之后才看出来）。判据同 `Build` 那条。
                int strayText = 0;
                foreach (var v in onBoard)
                    foreach (var t in v.GetComponentsInChildren<TMPro.TextMeshPro>(true))
                        if (t.gameObject.activeInHierarchy && !string.IsNullOrEmpty(t.text)) strayText++;
                Check(strayText == 0, $"★ 场上卡上没有多余的文字层（实测 {strayText} 处 —— 名字/技能/兵种行都该关着）");

                // 🔴 **2026-09-20 新加：场上的卡在不在真 3D 那一层、落点对不对**（第 12 行 B 段）。
                //    逐值判据 = `ArenaSlots`（原版 `MinionManager`）。
                if (drv.use3DBoard)
                {
                    var bcam = FindBoardCamera();
                    Check(bcam != null && (bcam.cullingMask & (1 << ArenaSlots.ArenaLayer)) != 0
                          && (cam.cullingMask & (1 << ArenaSlots.ArenaLayer)) == 0,
                          "★ 3D 那一层：**透视相机画、正交相机不画**（卡换层后两边不能都不画 —— 那是静默消失）");
                    int badLayer = 0, badPos = 0, seen = 0;
                    // 🆕 2026-09-25：**卡底那枚软阴影**（`项目任务.md` §三 第 12 条 第 3 项）。
                    // 期望的世界直径 = 三个**原版值**串起来 × 这张卡的缩放：
                    //   (125/100) 精灵      —— 原版 sprite `Card blob shadow` 是 128×128 @ `m_PixelsToUnits = 100`
                    //                          ⇒ 整块 **1.28**（`Sprite/Card blob shadow.json`）；
                    //                          我们导的是**裁掉透明边**的内容（原版 `textureRect` = 124.848²），
                    //                          存成 125² ⇒ 按 PPU 100 建出来是 **1.25**，与原版**可见内容**差 0.12%
                    //   × 2.0567584         —— 影子自己的 `localScale`（`Transform_6339896688388119488.json`）
                    //   × 0.88586           —— `Card 3D` 的 `localScale`
                    //   颜色 `m_Color = (0,0,0,0.36078429)` · `floorY = 0` · `yOffsetForSnap = 0.01`
                    //   （四个参数都在 `MonoBehaviour_2235663227690458048.json`）
                    int nShadow = 0, missShadow = 0, badDia = 0, badCol = 0, badShadowPos = 0;
                    for (int s = 0; s < BoardLayout.SlotCount; s++)
                        for (int e = 0; e < 2; e++)
                        {
                            bool foe = e == 1;
                            var v = drv.BoardViewAt(s, !foe);
                            if (v == null) continue;
                            seen++;
                            if (v.gameObject.layer != ArenaSlots.ArenaLayer) badLayer++;
                            // x / z 与缩放无关 ⇒ 可以精确比；y 加过「半卡高 × 缩放」，只判「在**地面之上**」
                            var p = v.transform.localPosition;
                            var g = ArenaSlots.Position(s, foe);
                            if (Mathf.Abs(p.x - g.x) > 0.01f || Mathf.Abs(p.z - g.z) > 0.01f || p.y <= 0.1f) badPos++;

                            var bs = v.Shadow;
                            if (bs == null) { missShadow++; continue; }
                            nShadow++;
                            float sc2 = s == BoardLayout.WarlordSlot ? ArenaSlots.HeroScale(foe)
                                                                    : ArenaSlots.CardScale(foe);
                            float wantDia = 1.25f * 2.0567584f * 0.88586f * sc2;
                            if (Mathf.Abs(bs.WorldDiameter - wantDia) > wantDia * 0.02f) badDia++;
                            // 贴地时：纯黑、α = 0.36078429 × 整卡不透明度；世界 y = floorY + 0.01
                            var bc = bs.CurrentColor;
                            if (bc.r > 0.001f || bc.g > 0.001f || bc.b > 0.001f
                                || Mathf.Abs(bc.a - 0.36078429f * v.Alpha) > 0.01f) badCol++;
                            if (Mathf.Abs(bs.transform.position.y - 0.01f) > 0.002f) badShadowPos++;
                        }
                    Check(seen > 0, $"3D 那一层数到场上卡 {seen} 张");
                    Check(missShadow == 0 && nShadow == seen,
                          $"★ 每张场卡底下都有一枚**原版软阴影**（有 {nShadow}/{seen} 张，缺 {missShadow} 张）");
                    Check(badDia == 0,
                          $"★ 软阴影的世界直径 = (125/100)×**2.0567584**×**0.88586**×卡缩放（不符 {badDia} 张）");
                    Check(badCol == 0,
                          $"★ 软阴影是**纯黑 α0.36078429**（贴地那档；整卡淡出时跟着淡）—— 不符 {badCol} 张");
                    Check(badShadowPos == 0,
                          $"★ 软阴影**贴在地上**（世界 y = `floorY 0` + `yOffsetForSnap 0.01`）—— 不符 {badShadowPos} 张");
                    Check(badLayer == 0, $"★ 场上的卡都在 `ArenaLayer`（不符 {badLayer} 张）");
                    Check(badPos == 0, $"★ 场上的卡都落在**原版落点**上、且站在地面之上（不符 {badPos} 张）");

                    // ============================================================
                    //  🆕 2026-09-26：**「未行动」绿光**（原版 `CardScript.ActivateMinion`
                    //  → `SetActive(CanAttackNow())` → `BattleCardUI.canAttackAnim`(+0x140)）
                    //  原版资产 = `bundle_battleprefabs_vfxandmisc_assets_all` 的
                    //  `3DBody/CanActParticles` + 子节点 `RotatingRing`。
                    //  🔴 **逐值都盯原版**（`项目任务.md` §三 第 12 条第 3 项），不是盯我们自己的常量。
                    // ============================================================
                    int nCanAct = 0, missCanAct = 0, wantLit = 0, badLit = 0, badParent = 0;
                    CardView probe = null;
                    for (int s = 0; s < BoardLayout.SlotCount && probe == null; s++)
                        for (int e = 0; e < 2 && probe == null; e++)
                        {
                            var v = drv.BoardViewAt(s, e == 0);
                            if (v != null && v.CanActBuilt) probe = v;
                        }
                    for (int s = 0; s < BoardLayout.SlotCount; s++)
                        for (int e = 0; e < 2; e++)
                        {
                            bool foe = e == 1;
                            var v = drv.BoardViewAt(s, !foe);
                            if (v == null) continue;
                            if (!v.CanActBuilt) { missCanAct++; continue; }
                            nCanAct++;
                            // **父节点必须是卡根** —— 挂在 `body3D`(=`Card 3D`) 底下会白乘一次 0.88586、
                            // 还会被那个 yaw 180° 带着转（这是本条最容易写错的地方，专门钉一条）。
                            if (v.CanActRoot.transform.parent != v.transform) badParent++;
                            bool want = RuleEngine.RuleCore.CanActNow(drv.Ctx, foe ? 1 - drv.MyIndex : drv.MyIndex, s);
                            if (want) wantLit++;
                            if (v.CanActVisible != want) badLit++;
                        }
                    Check(seen == 0 || missCanAct == 0,
                          $"★ 每张场卡都建出了「未行动」绿光那一层（建出的 {nCanAct}/{seen}，缺 {missCanAct}）");
                    Check(badParent == 0,
                          $"★ 绿光挂在**卡根**下（= 原版 `3DBody`，**不是** `Card 3D`）—— 挂错的 {badParent} 张");
                    Check(badLit == 0,
                          $"★ 绿光「亮/灭」= `RuleCore.CanActNow`（该亮 {wantLit} 张，亮错了 {badLit} 张）");
                    //  🔴 **正向也要验一次** —— 上面那条在「一个都不该亮」的局面里是 **0 == 0 的空过**
                    //  （这工程的老账：「自检绿不等于口径对」）。这里**人为**把一格改成「能行动」，
                    //  视图必须跟着亮；改回去必须灭。
                    if (seen > 0)
                    {
                        // 🔴 **不依赖棋局当时的状态** —— 这一段的**目的只是验接线**
                        //    （判据说「能打」⇒ 视图亮；说不「能打」⇒ 灭）。
                        //    所以把引擎里那三样临时掰成「有一格一定能打」：`Winner`（`IsOver` 的来源）、
                        //    `Active`（谁的回合）、那一格自己的疲劳/眩晕/已攻击次数/攻防值。
                        //    验完全部还原（这工程的老账：「自检绿不等于口径对」—— 一个都不亮的局面里 0==0 是空过）。
                        int svWinner = drv.Ctx.Winner, svActive = drv.Ctx.Active;
                        drv.Ctx.Winner = 0;
                        int slot = -1, owner = -1; bool svEx = false, svStun = false;
                        int svAtk = 0, svA = 0, svR = 0;
                        string why = "";
                        for (int side = 0; side < 2 && slot < 0; side++)
                        {
                            owner = side == 0 ? drv.MyIndex : 1 - drv.MyIndex;
                            drv.Ctx.Active = owner;
                            var pl = drv.Ctx.Players[owner];
                            for (int s = 0; s < BoardLayout.SlotCount && slot < 0; s++)
                            {
                                var u = pl.Board[s];
                                if (u == null) continue;
                                if (u.IsRemnant) continue;
                                svEx = u.Exhausted; svStun = u.IsStunned; svAtk = u.AttacksThisTurn;
                                svA = u.Attack; svR = u.RangedAttack;
                                // **把「能打」人为造出来** —— 攻击力本身由 `RuleEngineTest` 盯着，这里只验接线
                                u.Exhausted = false; u.IsStunned = false; u.AttacksThisTurn = 0;
                                u.Attack = Mathf.Max(1, u.Attack); u.RangedAttack = Mathf.Max(1, u.RangedAttack);
                                if (RuleEngine.RuleCore.CanActNow(drv.Ctx, owner, s)) slot = s;
                                else
                                {
                                    why += $" {side}/{s}({u.Name},blind={u.IsBlind},pin={u.Has("pindown")})"
                                         + $"={RuleEngine.RuleCore.CanAttackNow(drv.Ctx, owner, s, true)};";
                                    u.Exhausted = svEx; u.IsStunned = svStun; u.AttacksThisTurn = svAtk;
                                    u.Attack = svA; u.RangedAttack = svR;
                                }
                            }
                        }
                        Check(slot >= 0, "★ 正向：棋盘上存在可造出「能行动」的一格（找不到就验不了正向那条）：" + why);
                        if (slot >= 0)
                        {
                            drv.RefreshAll();
                            var v1 = drv.BoardViewAt(slot, owner == drv.MyIndex);
                            bool lit = v1 != null && v1.CanActVisible;
                            var u = drv.Ctx.Players[owner].Board[slot];
                            u.Exhausted = svEx; u.IsStunned = svStun; u.AttacksThisTurn = svAtk;
                            u.Attack = svA; u.RangedAttack = svR;
                            drv.Ctx.Winner = svWinner;
                            drv.RefreshAll();
                            var v2 = drv.BoardViewAt(slot, owner == drv.MyIndex);
                            bool lit2 = v2 != null && v2.CanActVisible;
                            Check(lit && !lit2,
                                  $"★ 正向：第 {slot} 格（{u.Name}）改成「能行动」⇒ 绿光**亮**（{lit}）；还原 ⇒ **灭**（{lit2}）");
                        }
                        else { drv.Ctx.Winner = svWinner; drv.RefreshAll(); }
                        drv.Ctx.Active = svActive;
                    }
                    if (probe != null)
                    {
                        var go = probe.CanActRoot;
                        var tr = go.transform;
                        // ① 姿态：`3DBody` 坐标 (0, 0.030168533, 0.006052971) 换算到我们的卡根（y −= Height/2）
                        float wantY = 0.030168533f - CardView.Height * 0.5f;
                        Check(Mathf.Abs(tr.localPosition.x) < 1e-4f
                              && Mathf.Abs(tr.localPosition.y - wantY) < 1e-3f
                              && Mathf.Abs(tr.localPosition.z - 0.006052971f) < 1e-5f,
                              $"★ `CanActParticles` 位置 = 原版 3DBody (0,0.030169,0.006053) ⇒ 卡根 (0,{wantY:F5},0.006053)"
                            + $"（实得 {tr.localPosition.x:F5},{tr.localPosition.y:F5},{tr.localPosition.z:F5}）");
                        var eu = tr.localEulerAngles;
                        Check(Mathf.Abs(Mathf.DeltaAngle(eu.x, 90f)) < 0.01f
                              && Mathf.Abs(Mathf.DeltaAngle(eu.y, 0f)) < 0.01f
                              && Mathf.Abs(Mathf.DeltaAngle(eu.z, 0f)) < 0.01f,
                              $"★ 绿光**平躺**（原版四元数 (0.7071068,0,0,0.7071068) = 绕 X 90°）—— 实得 {eu}");
                        Check(Mathf.Abs(tr.localScale.x - 1.7355630f) < 1e-3f
                              && Mathf.Abs(tr.localScale.y - 1.7355632f) < 1e-3f,
                              $"★ 绿光缩放 = **原版 1.735563**（未缩放 3DBody 空间；**不许**再乘 Card 3D 的 0.88586）"
                            + $"—— 实得 {tr.localScale}");
                        var ring = tr.Find("RotatingRing");
                        Check(ring != null, "★ `CanActParticles` 底下有 `RotatingRing` 子节点");
                        if (ring != null)
                        {
                            Check(Mathf.Abs(ring.localPosition.z + 0.04743f) < 1e-5f
                                  && Mathf.Abs(ring.localScale.x - 0.59292f) < 1e-4f
                                  && Mathf.Abs(ring.localScale.z - 0.05929f) < 1e-5f,
                                  $"★ `RotatingRing` 逐值：pos (0,0,−0.04743) · scale (0.59292,0.59292,0.05929)"
                                + $"—— 实得 {ring.localPosition} / {ring.localScale}");
                            var prB = ring.GetComponent<ParticleSystemRenderer>();
                            Check(prB != null && prB.renderMode == ParticleSystemRenderMode.Mesh
                                  && prB.mesh != null && prB.mesh.name.StartsWith("FxObject_cylinder_short"),
                                  $"★ `RotatingRing` 网格 = **`FxObject_cylinder_short`**（UnityPy 实读 pathID 4959531874643241410；"
                                + $"**不是 `Cylinder_Ring`**）—— 实得 `{(prB != null && prB.mesh != null ? prB.mesh.name : "<null>")}`");
                        }
                        var psA = go.GetComponent<ParticleSystem>();
                        var m = psA.main;
                        Check(m.maxParticles == 2 && Mathf.Abs(m.startSize.constant - 1.9f) < 1e-4f
                              && Mathf.Abs(m.simulationSpeed - 0.5f) < 1e-4f
                              && m.ringBufferMode == ParticleSystemRingBufferMode.LoopUntilReplaced
                              && Mathf.Abs(m.ringBufferLoopRange.x - 0.1f) < 1e-4f
                              && Mathf.Abs(m.ringBufferLoopRange.y - 0.9f) < 1e-4f,
                              "★ `CanActParticles` 主模块 = 原版：maxNumParticles **2** · startSize **1.9** · "
                            + "simulationSpeed **0.5** · **`looping=false` 但 `ringBufferMode=Loop`(0.1~0.9)**（常亮的真因）");
                        var c0 = m.startColor.color;
                        Check(Mathf.Abs(c0.r - 0.30103764f) < 1e-3f && Mathf.Abs(c0.g - 0.94509804f) < 1e-3f
                              && Mathf.Abs(c0.b - 0.09019607f) < 1e-3f && Mathf.Abs(c0.a - 0.22745098f) < 1e-3f,
                              $"★ `startColor` = 原版 (0.30104,0.94510,0.09020,**a 0.22745**) —— 实得 {c0}");
                        var prA = go.GetComponent<ParticleSystemRenderer>();
                        Check(prA != null && prA.renderMode == ParticleSystemRenderMode.Mesh
                              && prA.mesh != null && prA.mesh.name == "Quad",
                              "★ `CanActParticles` 网格 = **Unity 内置 `Quad`**（原版 `m_Mesh` pathID **10210**，`FileID 5`）");
                        Check(prA != null && prA.sharedMaterial != null
                              && prA.sharedMaterial.shader != null
                              && prA.sharedMaterial.shader.name == "Universal Render Pipeline/Particles/Unlit"
                              && prA.sharedMaterial.renderQueue == 3000,
                              "★ 绿光材质 = 原版 `Circle_Hoop Additive`（shader 就是 **URP 自带的 Particles/Unlit**，队列 3000）");
                        // ② **真的会出粒子吗** —— `shapeType = 6 (Mesh)` 而 `m_Mesh` 是空的（原版就这样），
                        //    这一步是唯一能证伪「形状不发射」的办法。批处理没有帧循环 ⇒ 手动 Simulate。
                        probe.SetCanAct(true);
                        probe.SimulateCanAct(0.3f);
                        int cnt = go.GetComponent<ParticleSystem>().particleCount;
                        var ringCnt = ring != null ? ring.GetComponent<ParticleSystem>().particleCount : -1;
                        Check(cnt > 0, $"★ 绿光**真的吐粒子**（`Simulate(0.3)` 之后 particleCount = {cnt}；"
                                     + "原版 `shapeType=6(Mesh)` 而 `m_Mesh` 空 —— 若长期是 0 说明这条形状不发射，要改判）");
                        Check(ringCnt > 0, $"★ `RotatingRing` 也吐粒子（particleCount = {ringCnt}）");
                        drv.RefreshAll();   // 把上面那次人为点亮还原成真实状态
                    }
                    // 纯函数判据（不依赖场上有没有卡）
                    Check(Mathf.Abs(ArenaSlots.CardScale(false) - 0.36f) < 1e-4f
                          && Mathf.Abs(ArenaSlots.CardScale(true) - 0.69f) < 1e-4f,
                          "★ 原版 `desiredScale`：玩家 **0.36** / 敌 **0.69**（两份各用各的，不是同一份）");
                    Check(Mathf.Abs(ArenaSlots.Position(0, false).x + (4 * 0.82f + 0.09f)) < 1e-4f
                          && Mathf.Abs(ArenaSlots.Position(5, false).x - (1 * 0.82f + 0.09f)) < 1e-4f
                          && Mathf.Abs(ArenaSlots.Position(BoardLayout.WarlordSlot, false).x) < 1e-4f,
                          "★ 原版 `FillMinionPositions`：槽 0 x=−3.37、槽 5 x=+0.91、督军槽在正中");
                    Check(Mathf.Abs(ArenaSlots.Position(0, true).z - 1.043f) < 1e-4f
                          && Mathf.Abs(ArenaSlots.Position(0, false).z + 6.655f) < 1e-4f,
                          "★ 两行 z：玩家 −6.655 / 敌 +1.043（`MinionArea` 的两份 local z）");
                    // 🆕 2026-09-25（`项目任务.md` §三 第 12 条 第 3 项）—— 下面两个数**实读**自
                    // `MonoBehaviour_4372.json`（我）/ `_4373.json`（敌），硬写在这里当靶子：
                    //   · 敌方 `minionExtraDistanceFromHero` = **0.13**（原版；我们原来按玩家侧取 0.09，注释自承没实读到）
                    //   · 督军位 `heroExtraOffset` = (0,0,**−0.42**) / (0,0,**−0.75**)（原版；**我们原来一条都没实现**）
                    // ⚠️ 敌方 x 要**镜像**（`Position` 最后那一步），所以槽 0 的敌行 x 是 **+**6.25。
                    Check(Mathf.Abs(ArenaSlots.Position(0, true).x - (4 * 1.53f + 0.13f)) < 1e-4f,
                          "★ 敌行落点 x = +(4×1.53 + **0.13**)（原版 `minionExtraDistanceFromHero` 实读）");
                    Check(Mathf.Abs(ArenaSlots.Position(BoardLayout.WarlordSlot, false).z + 7.075f) < 1e-4f
                          && Mathf.Abs(ArenaSlots.Position(BoardLayout.WarlordSlot, true).z - 0.293f) < 1e-4f,
                          "★ 督军位另有原版 `heroExtraOffset`：我 z = −6.655−0.42 = **−7.075** / 敌 z = 1.043−0.75 = **+0.293**");
                    Check(Mathf.Abs(ArenaSlots.Position(5, false).z + 6.655f) < 1e-4f,
                          "★ 那个 z 偏移**只管督军位**（槽 5 仍是 −6.655，没被带偏）");

                    // 🔴 **往返一致性**（2026-09-20）：把每一格的「该往哪儿拖」（`DropTargetWorld`，
                    //    已经按**透视投影**换算过）再喂回落点判定（`TryResolveSlot`），必须**解回自己**。
                    //    这一条同时挡住两类错：投影/镜像写反 · 容差写太大（互相咬）或太小（判不中）。
                    int badRt = 0, rtN = 0;
                    foreach (var bd in new[] { drv.playerBoard, drv.enemyBoard })
                    {
                        if (bd == null || !bd.use3D) continue;
                        for (int s = 0; s < BoardLayout.SlotCount; s++)
                        {
                            rtN++;
                            var p = bd.transform.TransformPoint(bd.DropTargetWorld(s));
                            int back;
                            if (!bd.TryResolveSlot(p, out back) || back != s) badRt++;
                        }
                    }
                    Check(rtN > 0 && badRt == 0,
                          $"★ 落点**往返一致**：每格「该往哪儿拖」都能解回自己（{rtN - badRt}/{rtN} 格）");
                    // 反例：拖到**另一行**的位置不该被这一行接住（两行的容差不能互相咬）
                    {
                        var mine = drv.playerBoard;
                        var foe = drv.enemyBoard;
                        if (mine != null && foe != null && mine.use3D && foe.use3D)
                        {
                            int s2;
                            var pFoeOnMine = mine.transform.TransformPoint(foe.DropTargetWorld(BoardLayout.WarlordSlot));
                            Check(!mine.TryResolveSlot(pFoeOnMine, out s2),
                                  "★ 反例：把敌方那一行的位置丢给**我方**判定，不该被接住（两行容差不许互相咬）");
                        }
                    }

                    // 🔴 **诊断（不是断言）：把两行卡心投到屏幕上，跟原版的两行实测值对一对。**
                    //    原版 `2D层_battlearena1全树.md` 里**UI 层**那两行的行心是
                    //    **708 px（玩家）/ 466 px（敌）** @1080。而 3D 卡是按 `MinionArea` 摆的 ——
                    //    两者是不是同一处，**从没验证过**（原版关服 ⇒ 拿不到「棋盘上有卡」的实拍）。
                    //    这条日志就是为了把那个问号变成一个数：**下次做战场取景时先读它**。
                    {
                        var bcam2 = FindBoardCamera();
                        // ⚠️ **A195 核过：这个门是安全的**（没改）—— 同一个 `if (drv.use3DBoard)` 块的上头那句
                        //    `Check(bcam != null && …)` 断的就是**同一个 `FindBoardCamera()`** ⇒ 它为空这里早红了。
                        if (bcam2 != null)
                        {
                            // 用 **viewport**（0..1）而不是 screen px —— 批处理下 `pixelHeight` 会变，
                            // 同一个点会被算成两个百分比（实测 62.1% / 66.0%）。
                            float bodyMid = (0.9683f - 1.6550f) * 0.5f;
                            float myTop = -1f, myBottom = -1f, foeTop = -1f;
                            foreach (bool foe in new[] { false, true })
                            {
                                float sc = ArenaSlots.CardScale(foe);
                                var root = ArenaSlots.RootPosition(BoardLayout.WarlordSlot, foe, sc);
                                var mid = root + Vector3.up * (bodyMid * sc);
                                float fromTop = 1f - bcam2.WorldToViewportPoint(mid).y;
                                Debug.Log(P + $"   [投影诊断] {(foe ? "敌" : "我")}方行卡心 → "
                                            + $"{fromTop * 100f:F2}% 距顶（= {fromTop * 1080f:F0} px @1080）");
                                if (!foe)
                                {
                                    myTop = fromTop;
                                    // 卡身**底边**（身体在卡坐标里 y −1.6550…+0.9683）
                                    myBottom = 1f - bcam2.WorldToViewportPoint(root + Vector3.up * (-1.6550f * sc)).y;
                                }
                                else foeTop = fromTop;
                            }
                            // 🔴 **取景的判据（2026-09-20 换成这三条）**：不再拿 `708 / 466` 当靶 ——
                            //    那两个数是 `battle.gd` 时代**由旧投影算出来的**（`审查更正清单_0827.md:95`
                            //    自己标着「由投影/旧校准·待核」），而现在 `lensShift` 由**原版算法**
                            //    （`BoardFramer`，曲线与常量逐值实读）给出 ⇒ 拿旧值当靶会**逼着算法跑偏**。
                            //    改成三条不依赖那个数、但一定要成立的：
                            Check(myTop > foeTop, $"★ 我方行在敌方行**下面**（{myTop * 100f:F1}% vs {foeTop * 100f:F1}% 距顶）");
                            Check(myTop < 0.90f && foeTop > 0.10f,
                                  $"★ 两行都在屏内（我方 {myTop * 100f:F1}% · 敌方 {foeTop * 100f:F1}% 距顶）");
                            // 手牌**上沿** ≈ 0.758 距顶（`HandLayout.DefaultBaselineY 0.1204` 从**下**算 + 半卡高）
                            // —— 我方卡**底边**必须在它上面，否则自己的兵被手牌盖住（B 段踩过的那件事）。
                            Check(myBottom < 0.758f,
                                  $"★ 我方卡**底边**在 {myBottom * 100f:F1}% 距顶，**压在手牌上沿（≈75.8%）之上**"
                                  + " —— 不然自己的兵会被手牌盖住");
                            Debug.Log(P + $"   [投影诊断] 相机 aspect={bcam2.aspect:F4} "
                                        + $"pixel={bcam2.pixelWidth}x{bcam2.pixelHeight} "
                                        + $"fov={bcam2.fieldOfView:F4} lens={bcam2.lensShift} "
                                        + $"pos={bcam2.transform.position}");
                            // 单独把 3D 那层 dump 出来（不带 HUD）—— 量「卡到底画在哪」用这张，别用合成图
                            DumpCam(bcam2, 1920, 1080, "d:/4/_tmp_view/battle/_3donly_13d.png");
                        }
                    }
                }
                // ⚠️ 跳过时**打日志而不是加一条恒真断言** —— 一直绿的断言等于没有断言（本工程踩过）。
                else Debug.Log(P + "   （这次没建 3D 战场 ⇒ 跳过「卡在 3D 那一层」那组断言）");
            }
        }

        // ---- 14b. 回放条（原版 `ReplayButtons`；2026-09-17）----
        // 坐标悬案已复核（`ReplayBar.cs` 文件头）：**坑 38 对、坑 35 错** —— 它在**屏内顶部**
        // x[410.2,703.8] y[37.3,94.7]，和 `LeftArea` **平级**（都挂在 `Safe area BackCanvas` 下）。
        // ✅ **2026-09-27：当年那两句「查不到」现在都查到了**（判据 → `资料/普查产出_0927/回放_界面真值.md`）：
        //   ① **显示时机** = `ReplayHud.Setup()` = `objHolder.SetActive(matchType == 0xA0)` —— **只有回放局**；
        //   ② **四个钮的原版语义** = `ClickRestartReplay` / `ClickPlayReplay` / `ClickPauseReplay` /
        //      `ClickNextStepReplay`（**回放的播放控制**）。
        //   ⚠️ 下面这些交互断言走的**仍是我们那套「本局时间控制」**（原版那四个动作要回放播放端，我们还没有）
        //   —— 它**不是复刻**，代码与 `ReplayBar.cs` 文件头都标着；**界面入口已挪到键盘**（`Space` / `.`）。
        Debug.Log(P + "--- 回放条（原版 ReplayButtons）---");
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            var rb = drv != null ? drv.Replay : null;
            Check(rb != null, "回放条建起来了");
            if (drv != null && rb != null)
            {
                drv.Begin("Ultramarines", "Goff", 20260917);
                Check(rb.HasArt, "4 枚图都取到了（restart / play / pause / next）");
                // 🔴 **2026-09-27：显隐照原版改对了** —— 原版 `ReplayHud.Setup()` =
                //    `objHolder.SetActive(matchType == 0xA0)`（**只有回放局**才出现）。
                //    我们从不进回放局 ⇒ `Begin()` 之后**整条是关的**（原来一直摆着，是错的）。
                Check(!rb.HolderVisible, "★ 普通对局 ⇒ 整条**不显示**（原版 `matchType == 0xA0` 才亮）");
                Check(!drv.ReplayClickAt(rb.PauseWorldPos),
                      "★ 不显示时**点它也不接**（`ReplayClickAt` 那道闸 —— 否则会看不见地触发暂停）");
                rb.Setup(true);                    // 下面的交互断言要在「回放局」这个前提下走
                Check(rb.HolderVisible, "★ 摆成回放局 ⇒ 整条亮起来（开关在 `Holder` 那一层，不是根节点）");
                Check(rb.VisibleCount == 3, $"同屏 3 枚（Play/Pause 互斥），实得 {rb.VisibleCount}");
                Check(rb.PauseShown && !rb.PlayShown,
                      "★ 开局在播 ⇒ 亮的是「暂停」那枚（与原版 `ReplayHud.Initialize()` **一致**，不是我们挑的）");

                // 位置 / 尺寸照原版（1920×1080，y 从上算）。⚠️ **只比 x/y** —— 我们这些 quad
                // 各自有 z（层次），比 3D 距离会被 z 差带跑（2026-09-17 第一版就是这么红的）
                var want1 = LayoutSpace.ToWorld((410.2f + 9.42f + 79.80f / 2f) / 1920f,
                                                1f - (37.3f + 4.4f + 48.57f / 2f) / 1080f);
                SettleShake();
                var got1 = rb.ReplayWorldPos;
                float d = new Vector2(got1.x - want1.x, got1.y - want1.y).magnitude;
                Check(d < 0.001f, $"★ 第 1 枚落在原版坐标上（偏差 {d:F4} 世界单位）"
                                  + $"｜实得 ({got1.x:F4},{got1.y:F4}) 期望 ({want1.x:F4},{want1.y:F4})");
                var sz = rb.BtnWorldSize;
                Check(Mathf.Abs(sz.x * 108f - 79.80f) < 0.3f && Mathf.Abs(sz.y * 108f - 48.57f) < 0.3f,
                      $"★ 一枚 79.80×48.57 px（实得 {sz.x * 108f:F2}×{sz.y * 108f:F2}）");
                Check((rb.PlayWorldPos - rb.PauseWorldPos).magnitude < 0.0001f,
                      "Play 与 Pause **同座标**（原版就是互斥的两张图）");

                // 暂停 / 继续 —— 走**和真实点击同一条判定**
                Check(drv.ReplayClickAt(rb.PauseWorldPos) && drv.ReplayPaused, "★ 点一下 → 进入暂停");
                Check(rb.PlayShown, "停住时亮的是「播放」那枚（点它继续）");
                Check(drv.ReplayClickAt(rb.PlayWorldPos) && !drv.ReplayPaused, "再点一下 → 继续播");

                // 单步：先打一刀造出待播事件（攻击的「抬刀」那一段是有延迟的）
                int a2, tp2, ts2;
                bool ranged2;
                if (SimpleAI.NextAttack(drv.Ctx, out a2, out tp2, out ts2, out ranged2))
                    RuleCore.DeclareAttack(drv.Ctx, drv.MyIndex, a2, tp2, ts2, ranged2);
                drv.RefreshAll();
                int queued = drv.TimelinePending;
                Check(queued > 0, $"打一刀之后有待播事件（{queued} 条 —— 「抬刀」那一段）");
                int after = queued;
                Check(drv.ReplayClickAt(rb.StepWorldPos) && drv.TimelinePending == after - 1,
                      $"★ 单步推掉一条（{after} → {drv.TimelinePending}）");
                Check(drv.ReplayPaused, "单步会**先停下**（和视频编辑器一个习惯）");

                // 重开：换一局，且**暂停态不跨局带过去**
                var old = drv.Ctx;
                Check(drv.ReplayClickAt(rb.ReplayWorldPos) && drv.Ctx != old, "★ 点重开 → 换了一局");
                Check(!drv.ReplayPaused, "重开之后回到「正在播」（暂停态不带过去）");
                Shot(cam, "19_回放条");
            }
        }

        // ---- 14c. 单位语音条（原版 `Unit Chat`；2026-09-17）----
        // 形状/数值/「哪些是我们挑的」→ `Battle/UnitChatPanel.cs` 文件头；
        // 数据（**张数/条数不写死，下面直接数源文件**）→ `Resources/voice_lines.json`，
        // 由 `工具/import_original_audio.py` 从原版解包资源生成。
        Debug.Log(P + "--- 单位语音条（原版 Unit Chat）---");
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            var chat = drv != null ? drv.UnitChat : null;
            Check(chat != null && chat.Ready, "语音条建起来了");
            Check(VoiceLines.Ready, $"语音表读进来了（{VoiceLines.CardCount} 张卡 / {VoiceLines.LineCount} 条台词）"
                                    + (VoiceLines.LoadError ?? ""));
            // ⚠️ **这里不写死数字**。原来写的是「595 张 / 1787 条」——
            //    2026-09-18 导入管道补全（1787 → 1844）之后，它立刻变成**假红**：
            //    表本身是对的，红的是这条过期断言。**写死的历史测量值 = 定时炸弹**（项目反复踩）。
            //    改法：直接数**源文件**（`Resources/voice_lines.json`）自己有多少张卡/多少条，
            //    再和运行时表比 —— 这样「管道生成对了、加载器却吞了行」才分得出来。
            int srcCards = 0, srcLines = 0;
            var _ta = Resources.Load<TextAsset>("voice_lines");
            if (_ta != null)
            {
                var _db = JsonUtility.FromJson<SrcVoiceDb>(_ta.text);
                if (_db != null && _db.cards != null)
                    foreach (var _c in _db.cards)
                    {
                        if (_c == null || string.IsNullOrEmpty(_c.id)) continue;
                        srcCards++;
                        if (_c.lines != null) srcLines += _c.lines.Length;
                    }
            }
            Check(srcCards > 0 && VoiceLines.CardCount == srcCards && VoiceLines.LineCount == srcLines,
                  $"★ 运行时表逐条等于源文件（源 {srcCards} 张 / {srcLines} 条，实得 {VoiceLines.CardCount}/{VoiceLines.LineCount}）");

            if (drv != null && chat != null)
            {
                drv.Begin("Ultramarines", "Goff", 20260918);
                Check(chat.HasArt, "气泡的图都取到了（`40k_voicelines_radio` / 波形图 / 卡图位）");
                var want = LayoutSpace.ToWorld((12.61f + 648.77f / 2f) / 1920f,
                                               1f - (643.50f + 236.50f / 2f) / 1080f);
                SettleShake();          // 见 `SettleShake` 的说明：别让没跑完的震动把 HUD 留在偏移位上
                var got = chat.BubbleCenterWorld(0);
                float d = new Vector2(got.x - want.x, got.y - want.y).magnitude;   // 只比 x/y（z 是层次）
                Check(d < 0.001f, $"★ 我方气泡落在原版矩形上（偏差 {d:F4} 世界单位）");
                var sz = chat.BubbleWorldSize;
                Check(Mathf.Abs(sz.x * 108f - 648.77f) < 0.6f && Mathf.Abs(sz.y * 108f - 236.50f) < 0.6f,
                      $"★ 气泡 648.77×236.50 px（实得 {sz.x * 108f:F2}×{sz.y * 108f:F2}）");
                var enemy = chat.BubbleCenterWorld(1);
                Check(enemy.y > chat.BubbleCenterWorld(0).y, "敌方的气泡在上、我方的在下（原版如此）");

                // 打一张**有语音、又付得起**的牌 → 气泡应当跟着引擎事件自己冒出来
                var hand = drv.Ctx.Players[drv.MyIndex].Hand;
                int voiced = -1;
                for (int i = 0; i < hand.Count; i++)
                {
                    if (!VoiceLines.Has(hand[i].Card.Id)) continue;   // ⚠️ 要**卡 id（字符串）**，不是实例号（int）
                    if (RuleCore.CostOf(drv.Ctx, drv.MyIndex, hand[i]) > drv.Ctx.Players[drv.MyIndex].Energy) continue;   // 第 7 行第 3 步：按**那一份**算
                    voiced = i;
                    break;
                }
                if (voiced < 0)
                {
                    Debug.Log(P + "   （这一手没有带语音的卡，跳过「事件驱动」那两条断言）");
                }
                else
                {
                    var card = hand[voiced];
                    int slot = SimpleAI.FirstFreeSlot(drv.Ctx.Players[drv.MyIndex]);
                    int code = RuleCore.PlayCard(drv.Ctx, drv.MyIndex, voiced, slot);
                    Check(code == RuleCodes.OK, $"打出一张有语音的卡（{card.Card.Name}）");
                    if (code == RuleCodes.OK)
                    {
                        drv.RefreshAll();
                        bool seen = false;
                        for (float t = 0f; t < 3f && !seen; t += 1f / 30f) { Step(1f / 30f); seen = chat.ShownSide == 0; }
                        Check(seen, "★ 那个单位一上场 → 语音条自己冒出来（走的是引擎事件那条路）");
                        Check(chat.LastCardId == card.Card.Id, $"★ 播的是它（{card.Card.Name} / {card.Card.Id}）");
                        Check(chat.LastEvent == "Deploy", $"事件是部署（实得 {chat.LastEvent}）");
                        Check(chat.LastClipLoaded, $"音频取到了（`{chat.LastClipName}`）");
                        Check(!string.IsNullOrEmpty(chat.ShownText) && chat.ShownText != "<无>",
                              $"条上有字：「{chat.ShownText}」");
                        Shot(cam, "20_单位语音条");      // ⚠️ 趁它还亮着拍（拍完再验「自己收起来」那一条）

                        Step(5f);        // 停留时间走完
                        Check(chat.ShownSide == -1, "★ 停留时间走完 → 气泡自己收起来");
                    }
                }

                // ---- 🆕 2026-09-25：**台词不许溢出**台词框（`项目任务.md` §三 第 12 条 第 2 项）----
                // 原版 `ChatText` 的出处：`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_3894.json`
                //   （TMP 组件；RT 3566 / GO 306）：RT 绝对矩形 **x[201.2,642.1] y[738.2,819.9] = 440.9×81.7 px** ·
                //   `m_TextWrappingMode = 1` · `m_enableAutoSizing = 1` · `m_fontSizeMin/Max = 10/33` ·
                //   `m_HorizontalAlignment = 1 (Left)` · `m_VerticalAlignment = 512`。
                // 🔴 **下面这三个数硬写在测试里、带出处** —— 拿 `UnitChatPanel.TextW` 去断言就是自证
                //    （常量哪天被改错，断言跟着一起错，这就是「自检替错值背书」）。全项目反复踩过这一条。
                {
                    // 挑**最长的那一条台词**试：用户当初看到的就是它出框（实测最长那条超出 ≈648 px = 框宽 1.47 倍）
                    string longest = null, longId = null, longName = null;
                    var db = _ta != null ? JsonUtility.FromJson<SrcVoiceDb>(_ta.text) : null;
                    if (db != null && db.cards != null)
                        foreach (var c in db.cards)
                        {
                            if (c == null || c.lines == null) continue;
                            foreach (var L in c.lines)
                            {
                                if (L == null || string.IsNullOrEmpty(L.text)) continue;
                                if (longest == null || L.text.Length > longest.Length)
                                { longest = L.text; longId = c.id; longName = c.name; }
                            }
                        }
                    if (string.IsNullOrEmpty(longest)) Check(false, "★ 从语音表里挑得出最长的一句（表是空的）");
                    else
                    {
                        chat.Speak(0, longId, "Test", null, longName, longest, null);
                        var lb = chat.TextLabel;
                        Check(lb != null, "★ 拿得到台词那块文字本体（量渲染真值要用它）");
                        if (lb != null)
                        {
                            float wPx = lb.WorldW * 108f, hPx = lb.WorldH * 108f;
                            Check(lb.Wrapping, "★ 台词是**折行**模式（原版 `m_TextWrappingMode = 1`）");
                            Check(lb.LineCount >= 2,
                                  $"★ 最长那句**真折了行**（实得 {lb.LineCount} 行；只有 1 行说明又变回 `NoWrap` 了）");
                            Check(wPx <= 440.9f + 0.6f,
                                  $"★ 渲染宽度 ≤ 原版框宽 **440.9 px**（实得 {wPx:F1}，这句 {longest.Length} 字）");
                            Check(hPx <= 81.7f + 0.6f,
                                  $"★ 渲染高度 ≤ 原版框高 **81.7 px**（实得 {hPx:F1}）");
                            Check(lb.FontPxNow >= 10f - 0.5f && lb.FontPxNow <= 33f + 0.5f,
                                  $"★ 字号落在原版 `m_fontSizeMin/Max` = **[10, 33]** 内（实得 {lb.FontPxNow:F1}）");
                            Shot(cam, "20b_单位语音条_最长台词");   // 实拍一眼：它现在应当**在框里折成几行**
                        }
                        chat.HideAll();
                    }
                }
                // ---- 🆕 2026-09-18：`cantdo`（原版 `ChatMessage.ICantDoThat` = 枚举 3）----
                // 原版：玩家做**非法操作**时督军说一句「我不能这么做」；闸门 = **正在播语音时不插播**。
                // 14 个触发点与逐条判据见 `资料/语音线_原版规格与ASR管道.md` §1.3。
                {
                    // ① 表本身：`ForCantDo` 必须**只有 `cantdo` 一条**。
                    //    原版取不到词条时落回 `defaultChatSound`（我们没有那个字段）；
                    //    回落成 `line` 会播「出场台词」，**那不是原版行为** ⇒ 这条断言把它钉住。
                    Check(VoiceLines.ForCantDo.Length == 1 && VoiceLines.ForCantDo[0] == "cantdo",
                          "★ `ForCantDo` 只有 `cantdo` 一条（**不回落 `line`**）");

                    // ② 「拖回手牌」不算非法操作 —— **纯函数判据**，不受音频时序影响。
                    //    这是本节最要紧的一条：分不清这两者的话，玩家每次**取消拖拽**都会被训一句。
                    var K = CardPresentation.CardInteraction.DropRejectKind.EngineRefused;
                    Check(CardPresentation.CardInteraction.IsIllegalAction(K)
                          && CardPresentation.CardInteraction.IsIllegalAction(
                                 CardPresentation.CardInteraction.DropRejectKind.SlotUsed),
                          "★ 「引擎拒绝」「格位已用」判为**非法操作**");
                    Check(!CardPresentation.CardInteraction.IsIllegalAction(
                                 CardPresentation.CardInteraction.DropRejectKind.MissedSlot)
                          && !CardPresentation.CardInteraction.IsIllegalAction(
                                 CardPresentation.CardInteraction.DropRejectKind.NoBoard),
                          "★ 「**拖回手牌/空白处**」「没有棋盘」**不算**非法操作（= 不播 cantdo）");

                    // ③ 督军那一族取得到台词与音频（`cantdo` 全池 54 条 —— **只有督军有**）
                    var myWarlord = drv.Ctx.Players[drv.MyIndex].Warlord;
                    // ⚠️ 必须先初始化：下面那句 `&&` 一短路，`out` 就不会被赋值（CS0165）
                    string cf = null, ct = null;
                    bool gotCantDo = myWarlord != null
                        && VoiceLines.TryPick(myWarlord.Card.Id, VoiceLines.ForCantDo, null, out cf, out ct);
                    Check(gotCantDo, $"★ 我方督军（{(myWarlord != null ? myWarlord.Card.Name : "无")}）有 `cantdo` 台词");
                    if (gotCantDo)
                        Check(VoiceLines.Clip(cf) != null, $"`cantdo` 的音频也在（`{cf}`）");
                    // ④ 真播一次 —— 但要**先确认没有语音在播**（原版闸门：不打断正在说的）。
                    //    上一条断言可能刚让某张牌说过话，所以这里只断言「闸门没坏」，
                    //    **不断言一定播出来了**（那是时序，会 flaky）。
                    bool busy = chat.IsSpeaking;
                    drv.SpeakCantDo();
                    Check(busy || chat.LastEvent == "CantDo",
                          busy ? "★ 闸门：正在播语音时**不插播** `cantdo`（原版行为）"
                               : $"★ `SpeakCantDo()` 让督军说了话（实得 `{chat.LastEvent}`）");
                }
                // ---- 🆕 2026-09-21：`hurry`（原版 `ChatMessage.Bored` = 枚举 2）----
                // 触发链一手证据（反编译）：`ClockManager__Update.c:44-51` —— `timeToHurryUp(35.0) + elapsed > 总时长`
                // 时触发**一次**（`latch_0xb8`），由 `StartTimer.c:28` 每回合复位；说话人**固定是我方督军**
                // （`DisplayWarlordRegularChatMessage(idx, isPlayer:true)`）。逐条见 `VoiceLines.ForHurry` 的注释。
                {
                    // ① 表本身：只有 `hurry` 一条、**不回落 `line`**（同 `cantdo` 的理由）
                    Check(VoiceLines.ForHurry.Length == 1 && VoiceLines.ForHurry[0] == "hurry",
                          "★ `ForHurry` 只有 `hurry` 一条（**不回落 `line`**，同 `cantdo`）");

                    // ② 我方督军取得到台词与音频
                    var wH = drv.Ctx.Players[drv.MyIndex].Warlord;
                    string hf = null, ht = null;
                    bool gotHurry = wH != null && VoiceLines.TryPick(wH.Card.Id, VoiceLines.ForHurry, null, out hf, out ht);
                    Check(gotHurry, $"★ 我方督军（{(wH != null ? wH.Card.Name : "无")}）有 `hurry` 台词");
                    if (gotHurry) Check(VoiceLines.Clip(hf) != null, $"`hurry` 的音频也在（`{hf}`）");

                    // ③ **每回合只播一次的 latch**（原版 `latch_0xb8`）。
                    //    只在「现在正好轮到玩家」时驱动钟 —— 否则 `TickClock` 会直接 return，
                    //    那会变成一条**空转的假绿**，不如**明说跳过**。
                    if (drv.Ctx.Active == drv.MyIndex && drv.ClockLeft > 0f)
                    {
                        Check(drv.HurrySaidThisTurn == false,
                              "★ 本回合还没到 35 秒时 latch 是**没触发**的（`HurrySaidThisTurn=False`）");
                        float toThreshold = drv.ClockLeft - drv.hurryUpSeconds;
                        if (toThreshold > 0f) drv.TickClockForTest(toThreshold - 0.5f);
                        Check(drv.HurrySaidThisTurn == false,
                              "★ 差 0.5 秒时**还没播**（阈值是「剩 ≤ 35 秒」）");
                        drv.TickClockForTest(1.0f);
                        Check(drv.HurrySaidThisTurn,
                              "★ 越过 35 秒 → `hurry` 触发（原版 `ClockManager__Update` 那条）");
                    }
                    else
                    {
                        Debug.Log(P + "   （现在不是玩家回合 / 钟已停 ⇒ 跳过 hurry 的 latch 那三条，"
                                    + "**不是通过、是没测**）");
                    }
                }
                // ---- 🆕 2026-09-18：`vs*`（打特定对手的开场白）的对照表 ----
                // 表与判据见 `VoiceLines.VsFactionTokens` 的注释；99 个 token 的逐条裁定与
                // 「认不出的 17 个」见 `资料/语音线_原版规格与ASR管道.md` §1.5.1。
                {
                    // ① 表里的**阵营名**必须都是真实存在的 —— 抄错一个字母 = **静默失效**
                    var facs = new System.Collections.Generic.HashSet<string>();
                    foreach (var c in drv.Ctx.CardPool) if (c != null) facs.Add(c.Faction);
                    var badFac = new System.Collections.Generic.List<string>();
                    int nFac = 0;
                    foreach (var kv in VoiceLines.VsFactionTable)
                    {
                        nFac++;
                        if (!facs.Contains(kv.Key)) badFac.Add(kv.Key);
                    }
                    Check(badFac.Count == 0,
                          badFac.Count == 0
                            ? $"★ `vs` 表的 {nFac} 个阵营名都真实存在（对着卡池的 `Faction` 校过）"
                            : $"**`vs` 表里有不存在的阵营名**：{string.Join(" / ", badFac)}");

                    // ② 每个 token 都得是 `vs` 开头、全小写（素材侧就是这么拼的）
                    var badTok = new System.Collections.Generic.List<string>();
                    foreach (var kv in VoiceLines.VsFactionTable)
                        foreach (var t in kv.Value)
                            if (!t.StartsWith("vs") || t != t.ToLowerInvariant()) badTok.Add(t);
                    Check(badTok.Count == 0,
                          badTok.Count == 0 ? "★ `vs` 表的 token 拼法合法（`vs` 开头、全小写）"
                                            : $"**拼法不对**：{string.Join(" / ", badTok)}");

                    // ③ 顺序 = 原版回落链：**先对手督军 → 再对手阵营 → 最后普通 `intro`**
                    var v1 = VoiceLines.ForVersus("Marneus Calgar", "Ultramarines");
                    Check(v1.Length >= 2 && v1[0] == "vs~marneuscalgar" && v1[v1.Length - 1] == "intro",
                          $"★ `ForVersus` 顺序对：先人名 → … → 最后 `intro`"
                          + $"（实得 [0]=`{v1[0]}` / 末=`{v1[v1.Length - 1]}`）");
                    // ④ **多对一的父军团词要展开到每个子阵营** —— 这是「集合模型」的核心
                    Check(System.Array.IndexOf(v1, "vssm") >= 0 && System.Array.IndexOf(v1, "vsum") >= 0,
                          "★ 父军团词展开到每个子阵营（Ultramarines 同时拿到 `vsum` **和** `vssm`）");
                    var vSW = VoiceLines.ForVersus(null, "SpaceWolves");
                    Check(System.Array.IndexOf(vSW, "vssm") >= 0 && System.Array.IndexOf(vSW, "vsum") < 0,
                          "★ 同一个 `vssm` 也挂在 SpaceWolves 下、但 **`vsum` 不在**（子阵营各拿各的）");
                    // ⑤ **不猜**：表里没配阵营级词的阵营只回落 `intro`
                    var v2 = VoiceLines.ForVersus(null, "Genestealers");
                    Check(v2.Length == 1 && v2[0] == "intro",
                          "★ 表里没配阵营级词的阵营（Genestealers）只回落 `intro` —— **宁可认不出**");

                    // ⑥ 端到端：真拿卡池里的卡试一遍「对泰伦说什么」
                    // 🔴 **这条断言我写错过两次，两次都是「判据没指向要证的事」**：
                    //    · 第一版只数了「`TryPick` 返回 true 的卡」—— 而 `ForVersus` **末尾永远带 `intro`**
                    //      ⇒ 哪怕 `vs` 表整张失效也照样绿（**假绿**）。
                    //    · 第二版改成 `vf.StartsWith("vs")` —— 可 `out clipName` 给的是**文件名**
                    //      （`VO_AM_Ursula Creed_vsTyranids.ogg`）**不是 ev** ⇒ 恒假（**假红**）。
                    //    ⇒ 第三版用 `TryPick(..., out ev)` 那个重载，**拿真 ev 判**。
                    //    教训：判据要指向要证的那件事，**而且断言消息里要打印实测值**（前两次都是靠它露的馅）。
                    int vsReal = 0, vsOnlyIntro = 0; string vsHit = null;
                    foreach (var c in drv.Ctx.CardPool)
                    {
                        if (c == null || !VoiceLines.Has(c.Id)) continue;
                        string vf, vt, vev;
                        if (!VoiceLines.TryPick(c.Id, VoiceLines.ForVersus(null, "Leviathan"), null,
                                                out vf, out vt, out vev)) continue;
                        if (vev != null && vev.StartsWith("vs"))
                        { vsReal++; if (vsHit == null) vsHit = c.Name + " → " + vev + "（" + vf + "）"; }
                        else vsOnlyIntro++;
                    }
                    Check(vsReal > 0,
                          $"★ **真的**有卡拿到 `vs*` 行（不是回落 `intro`）：{vsReal} 张；例：`{vsHit}`"
                          + $"（另有 {vsOnlyIntro} 张只回落到 `intro` —— 那些是**泰伦没给它们录 vs 行**的）");
                }
                // ---- 🆕 2026-09-18：开局独白（原版 `ShowHeroesIntroMessage`）----
                // 规格见 `资料/语音线_原版规格与ASR管道.md` §1.5：**严格先手→后手串行**、
                // 各自等语音播完、无额外秒数。判据落在「**后手在先手播完之前不开口**」。
                {
                    Check(VoiceLines.ForIntro.Length == 1 && VoiceLines.ForIntro[0] == "intro",
                          "★ `ForIntro` 只有 `intro`（普通单位没这一族，拿不到是**对的**）");
                    Check(VoiceLines.ForMirror.Length == 2 && VoiceLines.ForMirror[0] == "mirror"
                          && VoiceLines.ForMirror[1] == "intro",
                          "★ `ForMirror` = `mirror` → 回落 `intro`（同督军对局用它替换 intro）");

                    // 🔴 **先把上一节还挂着的气泡放完**（气泡有**最短 2 秒**）。
                    //    不排空的话，下面的 1 秒预算会整段花在「等上一条说完」上，
                    //    测出来是「先手没开口」—— 那是**假红**（独白没坏，是前一节还没收尾）。
                    //    2026-09-18 实测：上一节 `SpeakCantDo()` 说完后气泡还在，就踩了这个。
                    for (float t = 0f; t < 10f && (chat.IsSpeaking || chat.ShownSide != -1); t += 1f / 30f)
                        Step(1f / 30f);
                    drv.StartIntroMonologue();
                    int firstSide = drv.Ctx.Active;
                    Check(drv.IntroStage == 0,
                          $"★ 触发后状态机在「该先手说」（实得 {drv.IntroStage}）");

                    // ① 先手开口
                    bool firstSpoke = false;
                    for (float t = 0f; t < 1f && !firstSpoke; t += 1f / 30f)
                    { Step(1f / 30f); firstSpoke = chat.LastEvent == "Intro" && chat.ShownSide == firstSide; }
                    Check(firstSpoke,
                          $"★ 先手（{drv.Ctx.Players[firstSide].Warlord.Card.Name}）先开口");
                    // ② **串行的核心判据**：这时候后手**还没开口**，状态机停在「等先手播完」
                    Check(drv.IntroStage == 1 && chat.ShownSide != 1 - firstSide,
                          $"★ **后手没有插队**（状态机 =1 表示在等先手播完；实得 {drv.IntroStage}）");

                    // ③ 先手播完 → 后手开口
                    bool secondSpoke = false;
                    for (float t = 0f; t < 10f && !secondSpoke; t += 1f / 30f)
                    { Step(1f / 30f); secondSpoke = chat.LastEvent == "Intro" && chat.ShownSide == 1 - firstSide; }
                    Check(secondSpoke, "★ 先手播完之后轮到后手（**串行**，不是同时播）");

                    // ④ 两条都放完 → 状态机自己收尾
                    for (float t = 0f; t < 10f && drv.IntroStage >= 0; t += 1f / 30f) Step(1f / 30f);
                    Check(drv.IntroStage < 0,
                          $"★ 两条都播完 → 独白自己收尾（实得 {drv.IntroStage}）");
                }
                // ---- 🆕 2026-09-18：ChatPopup（原版 `VoiceLinesPopupSelector`）----
                // 规格与逐节点坐标 → `资料/语音线_原版规格与ASR管道.md` §1.7 / §1.7.1；
                // 实现 → `Battle/ChatPopupPanel.cs`。判据落在**三件容易做错的事**上：
                //   ① 6 个钮的位置（序列化态全是 (0,0)，真位置是布局组排出来的）
                //   ② 点面板外要关（原版那条全屏关闭区）
                //   ③ 说完进 4 秒冷却
                {
                    var pdrv = Object.FindObjectOfType<BattleDriver>();
                    var pop = pdrv != null ? pdrv.ChatPopup : null;
                    Check(pop != null && pop.Ready, "ChatPopup 建起来了（7 张图都取到了）");
                    if (pop != null)
                    {
                        Check(!pop.Visible, "默认是**关**的（原版 `m_IsActive = false`）");

                        // ① 版面：第 1 个与第 6 个钮的绝对矩形（原版 §1.7.1）
                        var r0 = pop.ButtonRect(0);
                        Check(Mathf.Abs(r0.x - 77.20f) < 0.01f && Mathf.Abs(r0.y - 481.89f) < 0.01f
                              && Mathf.Abs(r0.width - 603.60f) < 0.01f && Mathf.Abs(r0.height - 48f) < 0.01f,
                              $"★ 第 1 个钮在原版矩形上（实得 x={r0.x:F2} y={r0.y:F2} {r0.width:F2}×{r0.height:F2}；"
                              + "期望 77.20 / 481.89 / 603.60×48）");
                        var r5 = pop.ButtonRect(5);
                        Check(Mathf.Abs(r5.y - 729.29f) < 0.01f,
                              $"★ 6 个钮按布局组排开（第 6 个 y 实得 {r5.y:F2}，期望 729.29 = 481.89 + 5×49.48）");
                        Check(pop.ButtonRect(3).y - pop.ButtonRect(2).y > 49f,
                              "★ 钮与钮之间有间距（**不是**序列化态那种全叠在左下角）");

                        // ② 6 个钮点得动
                        pop.Show();
                        Check(pop.Visible, "开得起来");
                        int spoke = 0; var evs = new System.Text.StringBuilder();
                        for (int i = 0; i < 6; i++)
                        {
                            var r = pop.ButtonRect(i);
                            var w = LayoutSpace.ToWorld((r.x + r.width * 0.5f) / 1920f,
                                                        1f - (r.y + r.height * 0.5f) / 1080f);
                            if (pdrv.ChatClickAt(w)) { spoke++; evs.Append(VoiceLines.ForChatButton[i]).Append(' '); }
                        }
                        Check(spoke == 6, $"★ 6 个钮都收下了点击（实得 {spoke}/6：{evs}）");
                        Check(pop.LastClicked == 5, $"★ 最后点的是第 6 个钮（实得 {pop.LastClicked}）");

                        // ③ 冷却 = 原版 4 秒
                        Check(pdrv.ChatCooldownLeft > 3.9f,
                              $"★ 说完进 4 秒冷却（实得 {pdrv.ChatCooldownLeft:F2}s）");

                        // ④ 点面板外 → 关（0.5 s 淡出之后才不可见）
                        var outside = LayoutSpace.ToWorld(0.9f, 0.5f);
                        pdrv.ChatClickAt(outside);
                        for (float t = 0f; t < 2f && pop.Visible; t += 1f / 30f) Step(1f / 30f);
                        Check(!pop.Visible,
                              $"★ 点面板外 → 面板关掉（原版那条全屏关闭区；淡出 {ChatPopupPanel.FadeTime}s）");
                    }
                }
                // ⚠️ 本节可能把对局按了暂停（单步会先停）—— **收尾一定要恢复**，
                //    不然后面几节的对局全停在原地（它们各自 `Begin` 也救不回来）
                drv.SetReplayPaused(false);
            }
        }

        // ---- 15. 手感补间（用户 2026-09-13 点名的六件）----
        // 参数全在 `CardFeel.cs`，**每一条都有出处**（原版 AnimationClip / UnitTweenSO /
        // 卡预制体序列化字段 / 反编译常量 + 从 `GameAssembly.dll` 读出的 `_DAT_`）。
        // ⚠️ 这一段**显式打开** `animateFeel` —— 前面那十几节的断言要的是「当场精确的坐标」，
        //    和 `HandLayout.animateRelayout` 是同一条规矩（见 `BuildAndSaveScene`）。
        Debug.Log(P + "--- 手感补间（发牌 / 攻击 / 命中 / 阵亡 / 数值 / 重排）---");
        {
            // ① 先把「参数表」本身钉住 —— 抄错一位整套手感就不对，而**截图看不出来**
            // ② **出处登记表**：每个常量都必须登记自己是「原版字段 / 原版推导 / 我们挑的」三档里的哪一档
            //    （用户 2026-09-13 追问过「这些都是原版资料里的参数吧」—— 答案是**大部分是、有七处不是**，
            //     所以把这件事做成会被自检打出来的清单，而不是散在注释里）
            {
                var miss = CardFeel.Unclassified();
                Check(miss.Count == 0, miss.Count == 0
                      ? $"手感常量 {CardFeel.Catalog.Length} 条**全部登记了出处**"
                      : $"有 {miss.Count} 个手感常量没登记出处：{string.Join("/", miss)}");
                Debug.Log(P + $"   出处分档：原版字段 {CardFeel.CountOf(CardFeel.Src.Field)} 条 · "
                            + $"原版推导 {CardFeel.CountOf(CardFeel.Src.Derived)} 条 · "
                            + $"**我们挑的 {CardFeel.CountOf(CardFeel.Src.Ours)} 条**");
                Debug.Log(P + "   出处登记表：\n" + CardFeel.ProvenanceReport());
            }

            // ③ **每条序列的「真实长度」必须等于它标的那个时长。**
            //    ⚠️ 这条是 2026-09-13 加上的，因为踩了一个很阴的坑：DOTween 的 `Join` 接的是
            //    **当前游标**（上一次 `Append` 的结束处）而不是序列开头 —— 写错的话那一段
            //    从中间起算、整条序列被拖长，而**截图和别的断言都看不出来**（就是慢一点）。
            //    实测：落位那条 0.30s 被拖成 0.405s（旧版更离谱：0.92 → 1.10）。
            {
                var hv0 = driver.HandViewAt(0);
                CardData probeData = hv0 != null ? hv0.Data : CardData.Placeholder(1);
                var probe = CardView.Create(driver.transform, probeData, "ProbeFeel");
                if (probe == null) Debug.Log(P + "   （建不出探针卡，跳过序列长度检查）");
                else
                {
                    float dDeploy = Dur(DeploySequence.Play(probe, Vector3.zero, 1f));
                    Check(Mathf.Abs(dDeploy - DeploySequence.MoveTime) < 1e-3f,
                          $"落位序列真实长度 {dDeploy:F3}s == 原版 `minionToConversionPointTime` {DeploySequence.MoveTime:F2}s");
                    // 🔴 2026-09-18 改口径：阵亡消散**照原版分档**（小兵 0.2 / 督军 0.5），
                    //    不再是「从出战 clip 对称借来的 0.5333」（那条老断言把错口径钉死了 —— 见下）
                    float dDissolve = Dur(CardFeel.Dissolve(probe));
                    Check(Mathf.Abs(dDissolve - CardFeel.DeathDissolveMinion) < 1e-3f,
                          $"小兵消散序列 {dDissolve:F3}s == 原版 `deathTimeMinionDuration` {CardFeel.DeathDissolveMinion:F2}s");
                    float dDissolveW = Dur(CardFeel.Dissolve(probe, 0f, null, isWarlord: true));
                    Check(Mathf.Abs(dDissolveW - CardFeel.DeathDissolveWarlord) < 1e-3f,
                          $"督军消散 {dDissolveW:F3}s == 原版 `deathTimeWarlordDuration` {CardFeel.DeathDissolveWarlord:F2}s（**分档**，不是一个常量）");
                    float dDeal = Dur(CardFeel.DealIn(probe, Vector3.zero));
                    Check(Mathf.Abs(dDeal - CardFeel.DealDuration) < 1e-3f,
                          $"发牌序列 {dDeal:F3}s == {CardFeel.DealDuration:F2}s");
                    float dLunge = Dur(CardFeel.Lunge(probe.transform, Vector3.right));
                    Check(Mathf.Abs(dLunge - CardFeel.AttackPunchDuration) < 1e-3f,
                          $"前冲序列 {dLunge:F3}s == {CardFeel.AttackPunchDuration:F2}s（`Recoil Normal Tween`）");
                    float dCharge = Dur(CardFeel.Charge(probe.transform, Vector3.right));
                    Check(Mathf.Abs(dCharge - CardFeel.ChargeTime) < 1e-3f,
                          $"蓄力序列 {dCharge:F3}s == {CardFeel.ChargeTime:F2}s（卡预制体 `timeToChargeAttack`）");
                    // 飘字那条：时间轴必须**严格照 clip**（0.117 进 / 0.2 缩完 / 停到 1.667 / 1.833 消失）
                    CardFeel.PopNumber(driver.transform, Vector3.zero, 1, false);
                    float dPop = Dur(CardFeel.LastPopTween);
                    Check(Mathf.Abs(dPop - CardFeel.PopOut) < 1e-3f,
                          $"飘字序列 {dPop:F3}s == {CardFeel.PopOut:F3}s（`InBattleDamageCounter Variation 1`）");

                    if (Application.isPlaying) Object.Destroy(probe.gameObject);
                    else Object.DestroyImmediate(probe.gameObject);
                }
            }

            Check(Mathf.Abs(CardFeel.PushBackDuration - 0.4f) < 1e-4f && CardFeel.PushBackVibrato == 8
                  && Mathf.Abs(CardFeel.PushBackElasticity - 0.3f) < 1e-4f,
                  "后坐 punch = 0.4s / vibrato 8 / 弹性 0.3（`CardScript__DoPushBack.c` + 从 DLL 读的两个 `_DAT_`）");

            // 🔴 **2026-10-12（A354-b）：上面那条只管【常量】，常量对 ≠ 曲线对** —— 曲线形状在这里单列。
            //   判据 = `DG.Tweening.DOTween.Punch` 的段数公式（见 `资料/已知的坑.md` 2026-10-11 那条；
            //          两条独立路径：我们 DLL 的 IL ＋ 原版 `GameAssembly.dll` 的同名函数反汇编）：
            //     `count = (int)(vibrato × duration)`、**`< 2` 钳成 2**；关键帧
            //     `end[0] = direction` · `end[count-1] = 0` · 中间奇数格
            //     `end[i] = −ClampMagnitude(direction, mag × elasticity)`（⇒ **`elasticity` 只在 `count ≥ 3` 时被读到**）。
            //   ⇒ 挨打这条 `(int)(8 × 0.4) = **3**` ⇒ `end = [+punch, ≈−0.2×punch, 0]`
            //     （负那一下的上界 = `mag × elasticity`，而 `mag` 每轮按 `|direction|/count` 递减
            //      ⇒ `0.3 × (1 − 1/3) × |punch| = 0.2×|punch|`，再被 `ClampMagnitude` 夹住）
            //   ⇒ 轨迹**会穿零**：弹出去 → **反向过冲** → 归位。
            //      ⚠️ 与「`count = 2`」那一族（`RewardWindow` 的 5×0.4；准星那条 `vibrato 0` ⇒ 钳 2）
            //      **不是一回事** —— 那两条是**单峰、构造性不穿零**。
            //   采样法：直接调**生产入口** `CardFeel.HitReact`（`BattleDriver.PlayHitFeel` 走的就是它，
            //   实参与生产一字不差），然后 `CardTween.Advance` **只泵补间、不推时钟**，在 punch 的
            //   作用轴上采 36 个点（1/60 一采 ⇒ 两个关键帧都落在采样点上）。
            //   🧨 改坏法 ①：`PushBackVibrato` 调到 ≤ 5（5×0.4 = 2）⇒ 段数掉回 2 ⇒ 不再过冲 ⇒ 第二条红；
            //   🧨 改坏法 ②：`PushBackElasticity` 写 0 ⇒ `ClampMagnitude(dir, mag×0)` 恒 0 ⇒ 第 2 段回 0 ⇒ 同样红；
            //   🧨 改坏法 ③：把 `DOPunchPosition` 换成 `DOMove` 那类单调补间 ⇒ 两条都红。
            {
                var punchGo = new GameObject("ProbePunchBack");
                var ptr = punchGo.transform;
                ptr.position = new Vector3(1.5f, -2.25f, -0.5f);   // 随便挑一个非零点：punch 是**相对**位移，靶不重要
                Vector3 axis = Vector3.right;                      // `HitReact` 里 `Flat(away)` = (1,0,0) ⇒ punch 沿它来
                Vector3 start = ptr.position;
                CardFeel.HitReact(ptr, axis, false, 0f, 5);        // 5 ⇒ `PushBackMagnitude` 取 1.0 原版单位那一档
                float lo = 0f, hi = 0f;
                for (int k = 0; k < 36; k++)                       // 36 × (1/60) = 0.6 s > punch 的 0.4 s
                {
                    CardTween.Advance(1f / 60f);
                    float dev = Vector3.Dot(ptr.position - start, axis);   // **带符号**的偏离（穿零要看符号）
                    if (dev < lo) lo = dev;
                    if (dev > hi) hi = dev;
                }
                Check(hi > 0.05f,
                      $"★ 挨打后坐**真的弹出去了**（正向峰值 {hi:F3} 世界单位）——"
                    + " 🧨 不弹 / 幅度写 0 的实现恒为 0 ⇒ 这里红（没有它，下面那条「穿零」就是空断言）");
                Check(lo < -0.05f * hi,
                      $"★ ……而且**反向过冲（穿零）**：负向最低 {lo:F3}（正向 {hi:F3} 的 {Mathf.Abs(lo / Mathf.Max(hi, 1e-6f)):P0}）——"
                    + " `(int)(8 × 0.4) = 3` ⇒ `end = [+punch, ≈−0.2×punch, 0]`。"
                    + " 🧨 改坏法：`vibrato` ≤ 5（段数掉回 2）或 `elasticity` 写 0 ⇒ 曲线变单峰、负向恒 0 ⇒ 这里红");
                if (Application.isPlaying) Object.Destroy(punchGo); else Object.DestroyImmediate(punchGo);
            }
            Check(Mathf.Abs(CardFeel.AttackPunchUnits - 0.1f) < 1e-4f
                  && Mathf.Abs(CardFeel.AttackPunchDuration - 0.3f) < 1e-4f
                  && CardFeel.AttackPunchVibrato == 10,
                  "攻击 punch = 0.1 原版单位 / 0.3s / vibrato 10（`Recoil Normal Tween`）");
            Check(Mathf.Abs(CardFeel.ToOurs(0.1f) - 0.16865f) < 1e-3f,
                  $"原版 0.1 世界单位 → 我们 {CardFeel.ToOurs(0.1f):F4}（棋盘 182.14 px/单位 ÷ 本工程 108 px/单位）");
            Check(Mathf.Abs(CardFeel.ChargeTime - 0.35f) < 1e-4f && Mathf.Abs(CardFeel.ChargeAngleDeg + 10f) < 1e-4f
                  && Mathf.Abs(CardFeel.ChargeBackModifier - 0.5f) < 1e-4f,
                  "蓄力 0.35s / 后仰 10° / 后撤系数 0.5（卡预制体 `timeToChargeAttack` 等三个字段）");
            Check(Mathf.Abs(CardFeel.HitRotLightDeg - 3f) < 1e-4f
                  && Mathf.Abs(CardFeel.HitRotDuration - 0.5f) < 1e-4f && CardFeel.HitRotVibrato == 7,
                  "挨打的旋转 punch = 3° / 0.5s / vibrato 7（`Impact Light Tween`）");
            Check(Mathf.Abs(CardFeel.DeathDissolveMinion - 0.2f) < 1e-4f
                  && Mathf.Abs(CardFeel.DeathDissolveWarlord - 0.5f) < 1e-4f
                  && Mathf.Abs(CardFeel.DeathDissolve(false) - 0.2f) < 1e-4f
                  && Mathf.Abs(CardFeel.DeathDissolve(true) - 0.5f) < 1e-4f,
                  "★ 阵亡消散**分档**：小兵 0.2s（`deathTimeMinionDuration`）/ 督军 0.5s（`deathTimeWarlordDuration`）"
                  + " —— 改之前两边都是 0.5333（从出战 clip 对称借来的，不是原版值）");
            Check(Mathf.Abs(CardFeel.PopHoldUntil - 1.6667f) < 1e-3f && Mathf.Abs(CardFeel.PopOut - 1.8333f) < 1e-3f
                  && Mathf.Abs(CardFeel.PopIn - 0.1167f) < 1e-3f,
                  "飘字 0.117s 进 / 停到 1.667s / 1.833s 消失（`InBattleDamageCounter Variation 1`）");

            var drv = Object.FindObjectOfType<BattleDriver>();
            var hand = PlayerHand();
            if (drv == null || hand == null) Check(false, "找不到 BattleDriver / HandLayout");
            else
            {
                CardTween.Mode = DG.Tweening.UpdateType.Manual;
                drv.animateFeel = true;
                drv.Begin("Ultramarines", "Goff", 20260915);

                // ---- ① 发牌入场 ----
                Check(drv.DealtCount > 0, $"发牌入场：开局这一轮 {drv.DealtCount} 张是**新进手牌**的");
                var dv = drv.DealtView(0);
                if (dv != null)
                {
                    float fromDeck = Vector3.Distance(dv.transform.position, drv.DeckWorld);
                    Check(fromDeck < 0.4f, $"……第一张此刻还压在**牌堆**上（离牌堆中心 {fromDeck:F3} 世界单位）");
                    Check(dv.Alpha < 1f, $"……而且是**淡入**中（alpha {dv.Alpha:F2} < 1）");

                    int idx = drv.HandIndex(dv);
                    CardTween.Advance(CardFeel.DealDuration + 0.1f);
                    var want = hand.SlotPosition(Mathf.Max(0, idx), drv.HandCount);
                    float off = Mathf.Abs(dv.transform.position.x - want.x)
                              + Mathf.Abs(dv.transform.position.y - want.y);
                    Check(off < 0.02f, $"推出 {CardFeel.DealDuration:F2}s → 它**落到了手牌位**上（偏差 {off:F4}）");
                    Check(dv.Alpha > 0.99f, $"……而且已经**不透明**了（alpha {dv.Alpha:F2}）");
                    Shot(cam, "19_发牌入场之后");
                }

                // ---- ②③④⑤ 一次攻击同时验：攻击位移 / 命中抖动 / 阵亡消散 / 数值过渡 ----
                // 局面是**自己摆的**（照 `5b` 的做法），不靠抽牌运气：
                // 我方督军解除疲劳当攻击者，对面放一个 1 血靶子保证一刀砍死。
                int probe = RuleEngine.BoardSpec.WarlordSlot;
                var mine = drv.Ctx.Players[0].Board[probe];
                int victim = FreeSlot(drv.Ctx, 1);
                CardDef dummy = null;
                foreach (var c in drv.Ctx.CardPool)
                    if (dummy == null && c != null && c.Type == "unit" && c.Faction == "Goff"
                        && !c.Has(KeywordTable.Stealth) && !c.Has(KeywordTable.Flying)) dummy = c;
                if (mine == null || victim < 0 || dummy == null)
                    Debug.Log(P + "   （摆不出攻击用例，跳过 ②③④⑤）");
                else
                {
                    mine.Exhausted = false;
                    drv.Ctx.Players[1].Board[victim] = new UnitState(dummy, false) { Health = 1 };
                    drv.RefreshAll();

                    var atkView = drv.MyUnits[probe];
                    Check(atkView != null, "攻击者的视图在场上");
                    // 🔴 **2026-10-12（A360）：两条「静止位」的靶都在下面 `AdvanceTo(hitAt - 0.05f)` 之前取**
                    //   —— 原来是 `pBoard.SlotPosition(probe)`（攻击者）/ `eBoard.SlotPosition(victim)`（受击者），
                    //   理由与改坏法见那一段的注释。
                    Check(drv.SimulateOpenCommand(probe), "点自己的单位 → 攻击方式选择器弹出");
                    drv.SimulateCommand(AttackKind.Melee);
                    Check(drv.SimulateResolve(victim) == RuleCodes.OK, $"朝槽 {victim} 打出去");
                    // 🆕 2026-09-29：**引擎那一侧真的记下了这一档打法**（原版 `EntityScript.currentAttackType`）
                    // —— 攻击选择器那圈「已选打法」高亮读的就是它（判据 → `UnitState.LastAttackType`）。
                    Check(drv.Ctx.Players[0].Board[probe].LastAttackType == 1,
                          "★ 打出一记近战之后 `LastAttackType` = 1（原版 `currentAttackType`；"
                        + $"实测 {drv.Ctx.Players[0].Board[probe].LastAttackType}）");
                    Debug.Log(P + $"   [probe] 打完 t={drv.Clock:F3} 待播 {drv.TimelinePending} "
                                + $"对面槽{victim}视图 {(drv.FoeUnits.ContainsKey(victim) ? "在" : "没了")}");
                    Debug.Log(P + "   [probe] 时间线：\n" + drv.TimelineDump());

                    // 时间轴（`PlaySignals` 排的）—— **全部由事件表推，别写死**：
                    //   抬刀 `AttackWindUp` 0.2 → **出手事件** → 出手 `DurationOf(Attack)` 0.1 →
                    //   `MeleeImpactLag` → **命中事件** → 挨打 `DurationOf(Hit)` 0.75 → 阵亡。
                    // 🔴 **2026-09-19 更正**：命中帧从「抬刀 + 0.65」正回 **0.30**
                    //   （`MeleeImpactLag` 0.3→0 —— 那 0.3 实读是命中**之后**的收招 `:258-259`+`:260,264-268`，
                    //    不是前摇；见 `Core/EventTiming.cs` 那条注释）⇒ 下面两个采样点**必须跟着一起前移**：
                    //   原来写死 0.87（按「命中 0.85」算的），改动之后它落在弹跳**衰减完之后**，
                    //   量出来位移恒为 0.000（断言红、画面却看不出差别）。
                    //
                    // ⚠️ 采样必须**细推**（1/60 一步）：`Step(dt)` 是先推事件、再把补间推 `dt` 秒，
                    //    一次推 0.45 s 就等于「命中那一下整段弹跳被跳过去了」——
                    //    第一版这么写，量出来位移恒为 0.000（而截图上看不出差别）。
                    float hitAt = EventTiming.AttackWindUp + EventTiming.AttackStepMelee + EventTiming.MeleeImpactLag;
                    float t0 = drv.Clock;
                    float At(float rel) { return t0 + rel; }
                    void AdvanceTo(float rel)
                    {
                        int guard = 0;
                        while (drv.Clock < At(rel) && guard++ < 2000) Step(1f / 60f);
                    }

                    // 🔴 **2026-10-12（A360 修）：两条「静止位」的靶换成【这张卡自己的位姿】。**
                    //   改之前：攻击者取 `pBoard.SlotPosition(probe)`、受击者取 `eBoard.SlotPosition(victim)`
                    //   —— 那是 **HUD 正交平面**上的 2D 行线点（`LayoutSpace.ToWorld`，z 恒 0），
                    //   而场上的卡活在 **3D 竞技场**里（`ArenaSlots.RootPosition`：我方 z = −6.655 / 敌方 z = +1.043）
                    //   ⇒ 两个世界系**光 z 就隔 6~7 个世界单位** ⇒ `Vector3.Distance(...) > 0.02f` **恒真**
                    //   ⇒ 「② 离开了静止位」「③ 被弹开了」分不出「抖动了」和「没抖动」= 弱断言
                    //   （与 A337 / A359 同族；普查见 `资料/普查产出_1012/S2_战斗侧_开账现核.md` §一 A360）。
                    //   ✅ 现在两边都取**卡自己**：此刻**时间线一格都还没推**（`t0` 刚取完、下面是第一个
                    //      `AdvanceTo`），而 `RefreshAll` 把卡摆到位是**立即** `SetPose`（不是补间）
                    //      ⇒ 这一刻的位姿**就是**它的静止位。两个量**同一个世界系**，容差才有意义。
                    //   🧨 改坏法：把靶换回 `SlotPosition` ⇒ 距离恒 > 2 ⇒ 下面两条**永远绿**（那正是修之前的样子）；
                    //     把 `CardFeel.MeleeAttack` / `CardFeel.HitReact` 拿掉（或幅度写 0、补间不推进）
                    //     ⇒ 卡一动不动 ⇒ 那两条当场红。
                    var restAtk = atkView.transform.position;
                    var vicAtRest = drv.FoeUnits.ContainsKey(victim) ? drv.FoeUnits[victim] : null;
                    Check(vicAtRest != null, "受击者的视图在场上（挨打之前那一刻）");
                    var restVic = vicAtRest != null ? vicAtRest.transform.position : Vector3.zero;
                    // ★ **灭自证**（与 A337 那条 `sep3D`、A359 那条 `dLinePop` 同型）：钉住「卡自己的位姿」与
                    //   「2D 行线 `SlotPosition`」在 3D 下**确实不是同一处** —— 没有它，把靶换回 `SlotPosition`
                    //   这件事在读数上**看不出来**（两条主断言照样绿）。阈值只取**检测容差本身**（0.02），
                    //   ⛔ 不另编魔数（同 `:2222` 那条的口径）。
                    if (pBoard.use3D && pBoard.boardCam != null)
                    {
                        float sepLine = Vector3.Distance(restAtk, pBoard.SlotPosition(probe));
                        Check(sepLine > 0.02f,
                              $"★ 3D 下「卡自己的静止位」离「2D 行线 `SlotPosition`」**{sepLine:F2} 世界单位**"
                            + "（z 上就差一整个竞技场深度）—— 这条是**灭自证**：拿 `SlotPosition` 当静止位 = 恒真");
                    }

                    AdvanceTo(hitAt - 0.05f);                // 出手事件已发、**位移正走到一半**
                    Debug.Log(P + $"   [probe] t={drv.Clock:F3} 待播 {drv.TimelinePending} "
                                + $"对面槽{victim}视图 {(drv.FoeUnits.ContainsKey(victim) ? "在" : "没了")} "
                                + $"消散中 {drv.DyingCount}");
                    float moved = Vector3.Distance(atkView.transform.position, restAtk);
                    Check(moved > 0.02f,
                          $"② 攻击位移：攻击者**离开了静止位** {moved:F3} 世界单位（前冲在动；"
                        + $"静止位 = 它自己出手前那一刻的位姿 {restAtk.x:F2},{restAtk.y:F2},{restAtk.z:F2}）"
                        + " —— 🧨 拿 `SlotPosition` 当静止位 ⇒ 恒真（见上面那条灭自证）");
                    // ★ ② 的**方向**（A360 顺手补）：这一段是**朝受击者冲的**（`CardFeel.MeleeEndPos`
                    //   算的就是「受击者位置往回退一个 `playerAttackMargin`」）⇒ 「冲了，但冲反了 /
                    //   冲到别处去了」这种实现要能红 —— 那正是 2026-09-29 修掉的那个缺陷类
                    //   （`PlayAttackFeel` 的 `dir` 原来是**两个世界系相减**出来的）。
                    if (vicAtRest != null)
                    {
                        Vector3 toFoe = vicAtRest.transform.position - restAtk;
                        float align = toFoe.sqrMagnitude > 1e-6f
                            ? Vector3.Dot((atkView.transform.position - restAtk).normalized, toFoe.normalized)
                            : 1f;
                        Check(align > 0.3f,
                              $"★ ……而且**是朝受击者冲的**（位移方向 · 受击者方向 = {align:F3}）"
                            + " —— 🧨 改坏法：`MeleeEndPos` 的目标点算错（比如又跨两个世界系相减）⇒ 冲到别处 ⇒ 这里红");
                    }
                    Shot(cam, "20_出手");

                    AdvanceTo(hitAt + 0.03f);               // **刚过命中那一刻**
                    var vicView = drv.FoeUnits.ContainsKey(victim) ? drv.FoeUnits[victim] : null;
                    Debug.Log(P + $"   攻击者 {atkView.name}@槽{probe}　受击者 {(vicView == null ? "无" : vicView.name)}@槽{victim}"
                                + $"　对面场上：{drv.BoardViewNames(false)}");
                    Debug.Log(P + $"   命中帧：攻击者离位 {Vector3.Distance(atkView.transform.position, restAtk):F3}"
                                + $"（角 {Mathf.DeltaAngle(atkView.transform.eulerAngles.z, 0f):F2}°）"
                                + $"　受击者离位 {(vicView == null ? -1f : Vector3.Distance(vicView.transform.position, restVic)):F3}"
                                + $"（角 {(vicView == null ? 0f : Mathf.DeltaAngle(vicView.transform.eulerAngles.z, 0f)):F2}°）");
                    Check(vicView != null, "③ 受击者的视图还在（阵亡是排在时间线上的，这一刻还没轮到）");
                    if (vicView != null)
                    {
                        float mv = Vector3.Distance(vicView.transform.position, restVic);
                        Check(mv > 0.02f,
                              $"③ 命中抖动：受击者**离开了静止位** {mv:F3} 世界单位"
                            + $"（静止位 = 它自己挨打前的位姿 {restVic.x:F2},{restVic.y:F2},{restVic.z:F2}）"
                            + " —— 🧨 改坏法：`CardFeel.HitReact` 拿掉 / 幅度写 0 ⇒ 它一动不动 ⇒ 红；"
                            + "拿 `SlotPosition` 当静止位 ⇒ 恒真");
                        Check(Mathf.Abs(Mathf.DeltaAngle(vicView.transform.eulerAngles.z, 0f)) > 0.3f,
                              $"……而且**转了一下**（z 偏了 {Mathf.DeltaAngle(vicView.transform.eulerAngles.z, 0f):F2}°）");
                    }

                    Check(drv.LastPop != null, "⑤ 数值过渡：伤害数值飘出来了");
                    if (drv.LastPop != null)
                    {
                        Debug.Log(P + $"   飘字：「{drv.LastPop.Text}」");
                        Check(drv.LastPop.Text.StartsWith("-"), $"……是**伤害**（负数）：「{drv.LastPop.Text}」");
                        Check(drv.LastPop.color.a > 0.05f, $"……而且开始显影了（alpha {drv.LastPop.color.a:F2}）");
                        // ⚠️ 位置也要断言：飘字挂在驱动层自己的节点下，父节点一旦不在原点，
                        //    它会飘到别的地方去（而**截图上看不出来** —— 那一下 alpha 才 0.17、还被烟盖着）
                        // 🔴 **2026-10-11（A359）：靶换成 `DropTargetWorld`** —— 与 `BattleDriver.PlayHitFeel`
                        //    的是**同一个口**（铁律 6；那一边的改坏法与理由见它的注释）。偏移读
                        //    **同一个常量** `BattleDriver.PopOffset`（原来内联在驱动层的实参里）。
                        //    ⚠️ 容差**从 0.9 收到 0.05** —— `0.9` 是**弱断言**：受击方在我方还是敌方那一行、
                        //    投影点与 2D 行线差多少，都可能让它**照样绿**（分不出「靶是哪一处」）。
                        //    🧨 **改坏法**：把 `PlayHitFeel` 的 `at` 换回 `layout.SlotPosition(e.Slot)`
                        //    ⇒ 基准差 0.47~0.74 世界单位 ⇒ 这里当场红。
                        var popBase = eBoard.DropTargetWorld(victim);
                        var popAt = popBase + BattleDriver.PopOffset;
                        float dPop = new Vector2(drv.LastPop.transform.position.x - popAt.x,
                                                 drv.LastPop.transform.position.y - popAt.y).magnitude;
                        Check(dPop < 0.05f && drv.LastPop.transform.position.y > popBase.y,
                              $"……而且飘在**挨打那张卡上**（基准 = 那一格的 3D 投影点，偏差 {dPop:F4} 世界单位、在上方）");
                        // 🔴 **A359 的灭自证那条**（与 A337 同型）：上面那条只证明「飘在**某个**基准点上」，
                        //    不证明**是哪一个**。这里钉「它不是落在 2D 行线上」—— 挡的是「驱动层与这里的靶
                        //    **一起**改回 `SlotPosition`」那种回归（那一档上面那条**照样绿**）。
                        if (eBoard.use3D && eBoard.boardCam != null)
                        {
                            var linePop = eBoard.SlotPosition(victim) + BattleDriver.PopOffset;
                            float dLinePop = new Vector2(drv.LastPop.transform.position.x - linePop.x,
                                                         drv.LastPop.transform.position.y - linePop.y).magnitude;
                            Check(dLinePop > 0.05f,
                                  $"★ ……而且**不是**落在 2D 行线上（离 `SlotPosition` 基准 {dLinePop:F4} 世界单位）"
                                + " —— 🧨 改坏法：驱动层与这里的靶**一起**改回 `SlotPosition` ⇒ 这里当场红");
                        }
                    }
                    Shot(cam, "21_命中与飘字");

                    // ⚠️ **2026-09-13 改：这里原来是写死的 `1.72`**，注释还写着「刚过阵亡那一刻（1.70）」。
                    //    引擎改成「伤害**同时结算** → 死亡触发排在其后」（规则书 :145 + :238）之后，
                    //    时间线上**多了一条** `Hit` —— 被攻击者的**反击**（原版 `rule_core.gd:4310`
                    //    修正过「近战击杀免反」那条规则偏差，所以目标死了也照样反击，反击是一次真伤害、
                    //    要占 `DurationOf(Hit)` 0.75 s）。⇒ 阵亡时刻从 1.70 推到 **2.45**。
                    //    **不再写死**：按事件表推出的时刻 = 命中那一刻 + 两次 Hit 的时长 + DeathHold。
                    //    （下次谁动了 `EventTiming`，这条会自己跟上，不会再变成一条骗人的断言。）
                    //    🔴 **2026-09-18 再改：那个 `0.85f` 也是个写死的数** —— 它 = 抬刀 0.2 + **旧的出手 0.65**。
                    //       出手时长按原版改成 `attackStepTime`(0.10) 之后它就不对了
                    //       ⇒ 现在**全部由事件表推**，一个魔数都不留。
                    //    🔴 **2026-09-19**：`MeleeImpactLag` 由 0.30 改成 **0**（那 0.3 是命中**之后**的收招，
                    //       不是前摇）⇒ 这一步推出的阵亡时刻**整体前移 0.3 s**，不用改判据（它本来就是推出来的）。
                    float deathAt = EventTiming.AttackWindUp                        // 抬刀
                                  + EventTiming.AttackStepMelee                     // 出手 → 命中（近战档）
                                  + EventTiming.MeleeImpactLag                      // 命中 → 扣血
                                  + 2f * EventTiming.DurationOf(EvtKind.Hit)        // 挨打 + 目标的反击
                                  + EventTiming.DeathHold;
                    AdvanceTo(deathAt + 0.02f);             // 刚过阵亡那一刻
                    Check(drv.DyingCount == 1, $"④ 阵亡消散：有 {drv.DyingCount} 张卡正在消散（视图已被从场上摘掉）"
                          + $"（推到 t0+{deathAt + 0.02f:F2}s）");
                    var dying = drv.DyingView(0);
                    // ⚠️ 断言要**连着非空一起判** —— 第一版写成 `dying == null || dying.Alpha < 1f`，
                    //    「视图根本没进消散表」反而让它通过了（那一版 `DyingCount` 就是 0）
                    // 🔴 **2026-10-01 改判据**：原来判的是 `dying.Alpha < 1f`（「正在变淡」）——
                    //    那是**旧表现**（`CardFeel.Dissolve` 淡出，取不到爆散体时的退回分支）留下的。
                    //    现在走原版那条：**卡不淡出**，而是 ① 关 3D 体 ② 卡位生成爆散体 ③ 卡抖一下
                    //    （`CardFeel.DeathExplosion`；判据 → `资料/待办判据_战场与战斗视图.md` 末节第 9 条）。
                    bool deathBodyInLib = WarpforgeVFX.WarpforgeEffectLibrary.Available
                        && WarpforgeVFX.WarpforgeEffectLibrary.Instance.TryGet(CardFeel.DeathBodyFx, out _);
                    if (deathBodyInLib)
                        Check(dying != null && !dying.Body3DVisible,
                              "……而且它是**原版那种阵亡**（3D 体已关、卡还在原地抖），不是「啪」一下没了"
                            + (dying == null ? "（视图没进消散表）" : ""));
                    else
                        Check(dying != null && dying.Alpha < 1f,
                              $"……而且它**正在变淡**（效果库里没有那件爆散体 ⇒ 走的是退回分支；"
                            + $"alpha {(dying == null ? -1f : dying.Alpha):F2}）");
                    Shot(cam, "22_阵亡消散");

                    while (drv.DyingCount > 0 && drv.Clock < At(deathAt + 0.02f) + CardFeel.DissolveTime + 0.2f) Step(1f / 30f);
                    Check(drv.DyingCount == 0, "……推完 0.53s → 消散结束、视图销毁");
                }

                // ---- ⑥ 手牌重排 ----
                // 🔴 2026-09-17 更正：这条原来写「时长**原版查不到**、0.18s 是我们挑的」—— **两半都不成立**。
                //    原版值 = `VarsGlobal.timeToPositionCard = 0.2`（`CardTween.RelayoutDuration` 已换成它），
                //    调用方 `PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c:76-80` 也已反编译出来。
                //    出处：`资料/VarsGlobal_原版数值.md` §一。
                //    （断言推进量是**符号引用** `CardTween.RelayoutDuration + 0.05f`，换值自动跟着走。）
                bool savedAnim = hand.animateRelayout;
                hand.animateRelayout = true;
                var hv = drv.HandViewAt(0);
                // 🔴 **A195**：这个门原来**没有 `else`** —— 手牌不足 2 张时下面那三条 ⑥ 断言**一条都不跑**、
                //    section 照样绿。前提断言 + `if`（同 A52-F10 的形状）：条件不成立**当场红**。
                Check(hv != null && drv.HandCount >= 2,
                      $"（前提）手牌 ≥ 2 张、且第 0 张的真视图取得到（实得 HandCount={drv.HandCount}）"
                    + " —— 不成立时下面那三条 ⑥ 断言等于没验（A195）");
                if (hv != null && drv.HandCount >= 2)
                {
                    var list = new List<CardView>();
                    for (int i = 0; i < drv.HandCount; i++) list.Add(drv.HandViewAt(i));

                    float restY = hv.transform.position.y;
                    hand.Refresh(list, 0);                       // 悬停第 0 张 → 它该抬起来
                    Step(1f / 60f);
                    Check(hv.transform.position.y - restY < hand.hoverLift * 0.5f,
                          "⑥ 手牌重排走的是**补间**：刷新后的第一帧还没抬到位");
                    CardTween.Advance(CardTween.RelayoutDuration + 0.05f);
                    Check(Mathf.Abs(hv.transform.position.y - (restY + hand.hoverLift)) < 0.01f,
                          $"……推完 {CardTween.RelayoutDuration:F2}s → 抬到位（{hand.hoverLift:F2} 世界单位）");

                    hand.Refresh(list);                          // 收工：把悬停撤掉
                    CardTween.Advance(CardTween.RelayoutDuration + 0.05f);
                    Check(Mathf.Abs(hv.transform.position.y - restY) < 0.01f, "……松开后回到原位");
                }
                hand.animateRelayout = savedAnim;
                drv.animateFeel = false;

                // ---- ⑥b 手牌**排列**：按张数分档 + 选中让位（2026-09-25 照原版逐句解出后补）----
                // 🔴 断言比的是**原版实读值**（不是我们的字段 —— 拿我们的常量断言我们的常量 = 自证）。
                //    原版出处：`资料/手牌布局_原版算法与参数.md`（`GetPosition`/`GetRotation` 逐句 + VarsDevice 实读）。
                {
                    bool savedAnim2 = hand.animateRelayout;
                    hand.animateRelayout = false;      // 这一段比的是**当场**的位置，不走补间

                    // (1) 三个原来「自造」的量，现在都有原版出处
                    Check(Mathf.Abs(hand.hoverLift - 30f / 108f) < 1e-4f,
                          $"⑥b 悬停抬起 = 原版 `VarsDevice.cardInHandShownYOffset` **30 px**（我们 {hand.hoverLift * 108f:F1} px）");
                    Check(Mathf.Abs(hand.hoverScale - 1.3f) < 1e-4f,
                          $"⑥b 悬停缩放 = 原版 `cardInHandShownScale` **1.3**（我们 {hand.hoverScale:F2}）");
                    Check(Mathf.Abs(hand.selectedCardExtra - 2.0f * HandLayout.OurUnitsPerWorldUnit) < 1e-5f,
                          $"⑥b 选中让位 = 原版 `extraSpaceOnSelectedCard` **2.0 世界单位** = 29.7 px"
                        + $"（我们 {hand.selectedCardExtra * 108f:F1} px）");

                    // (2) **按张数分档的边界**：原版 `n × 间距 > 0.6026 × 屏宽` ⇒ n ≥ 8 起压缩间距、
                    //     且压缩时**选中也不让位**（原版在那一支里把 extra 归零）。
                    Check(!hand.IsCompressed(7), "⑥b 7 张：装得下 ⇒ 不压缩、选中会让位");
                    Check(hand.IsCompressed(8), "⑥b 8 张：装不下 ⇒ 压缩间距、**选中不让位**（原版口径）");
                    Check(hand.SpacingFor(7) > hand.SpacingFor(12),
                          $"⑥b 压缩是真的生效：7 张间距 {hand.SpacingFor(7) * LayoutSpace.VisibleWidth * 108f:F1} px"
                        + $" > 12 张 {hand.SpacingFor(12) * LayoutSpace.VisibleWidth * 108f:F1} px");

                    // (3) **弧高按张数长**（原版 `m_heightModifierBasedOnTotalCards`；x = (n−1)/(12−1)）
                    //     原版值：5 张 ≈ 10.0 px · 12 张 ≈ 31.4 px（= 3.02 × 0.70 × hMod × 14.835）
                    float arc5 = (hand.SlotPosition(2, 5).y - hand.SlotPosition(0, 5).y) * 108f;
                    float arc12 = (hand.SlotPosition(6, 12).y - hand.SlotPosition(0, 12).y) * 108f;
                    Check(Mathf.Abs(arc5 - 10.0f) < 0.6f, $"⑥b 5 张弧高 = 原版算出的 **10.0 px**（我们 {arc5:F1} px）");
                    Check(Mathf.Abs(arc12 - 31.4f) < 1.2f, $"⑥b 12 张弧高 = **31.4 px**（我们 {arc12:F1} px）⇒ 牌越多弧越大");

                    // (4) **张角**：原版 `atan2(dx, |lookTo| + 弧高) × 张数修正`
                    //     5 张外缘 = atan2(313 px, 4896 px) × 0.948 = **3.47°**
                    float tilt5 = hand.RotationAt(0, 5);
                    Check(Mathf.Abs(tilt5 - 3.47f) < 0.12f,
                          $"⑥b 5 张外缘张角 = 原版算出的 **3.47°**（我们 {tilt5:F2}°）");
                    Check(hand.RotationAt(4, 5) * tilt5 < 0f, "⑥b 两侧反向撇（左负右正）");

                    // (5) **选中让位真的动**：5 张悬停中间那张 ⇒ 左边两张整体左移、右边两张整体右移
                    if (hv != null && drv.HandCount >= 3)
                    {
                        var list = new List<CardView>();
                        for (int i = 0; i < drv.HandCount; i++) list.Add(drv.HandViewAt(i));
                        int sel = drv.HandCount / 2;
                        var leftView = drv.HandViewAt(0);
                        var selView = drv.HandViewAt(sel);
                        float leftX0 = leftView.transform.position.x;
                        float selX0 = selView.transform.position.x;
                        hand.Refresh(list, sel, -1);
                        float dLeft = leftView.transform.position.x - leftX0;
                        float dSel = selView.transform.position.x - selX0;
                        Check(dLeft < -hand.selectedCardExtra * 0.5f,
                              $"⑥b 选中第 {sel} 张 ⇒ 左侧的牌**整体左让** {dLeft * 108f:F1} px（原版 29.7 px 量级）");
                        Check(Mathf.Abs(dSel) < 0.01f, "⑥b ……而被选中的那张自己不左右移（只抬起/放大）");
                        hand.Refresh(list);
                    }
                    hand.animateRelayout = savedAnim2;
                }

                // ---- ⑦ 挨打震镜头 ----
                // 原版：卡预制体 `meleeHitCameraShakePreset` → preset `Shake Hit Small` → Cinemachine Impulse
                // → **震主相机**（那三件套挂在 "Cinemachine Vcam" 上驱动 BoardCamera），而独立 `UI Camera`
                //   没挂、`useCanvasShake: 0` ⇒ **原版不震 UI**。
                // 我们：只有一台正交相机、HUD 是世界空间 quad ⇒ 落地方式是
                //   「**相机与 HUD 根同向等量平移**」，HUD 在屏幕上就纹丝不动。见 `CardFeel.ShakeCamera`。
                {
                    var hudRootT = drv.hudRoot;
                    // ⚠️ **先收掉没跑完的那次震动，再取「原位」**（2026-09-19）：
                    //    `ShakeCamera` 现在开头会自己 `SettleShake()`（把上一次震动复位）——
                    //    如果这里在它之前就把**歪着的位置**当成了 `camHome`，量出来的满幅会是
                    //    「旧位移 + 新位移」的合成（实测 0.2428 而不是 0.04），后面两条「放回原位」也会跟着红。
                    SettleShake();
                    Vector3 camHome = cam.transform.position;
                    Vector3 hudHome = hudRootT != null ? hudRootT.localPosition : Vector3.zero;

                    var shake = CardFeel.ShakeCamera(cam, hudRootT, CardFeel.ShakeWorldAmplitude);
                    Check(Mathf.Abs(Dur(shake) - (CardFeel.ShakeSustainTime + CardFeel.ShakeDecayTime)) < 1e-3f,
                          $"震镜头序列 {Dur(shake):F3}s == `Shake Hit Small` 的 sustain {CardFeel.ShakeSustainTime}"
                          + $" + decay {CardFeel.ShakeDecayTime}（attackTime 0 不占时长）");

                    // `attackTime = 0` ⇒ **一帧到满**（不是「没有起振」）：刚建出来就该在满幅
                    float full = (cam.transform.position - camHome).magnitude;
                    Check(Mathf.Abs(full - CardFeel.ShakeWorldAmplitude) < 1e-4f,
                          $"……`attackTime 0` ⇒ 当场满幅（{full:F4} = {CardFeel.ShakeWorldAmplitude} 世界单位，由原版值推导）");

                    // 🔴 **HUD 根必须跟相机「同向等量」走** —— 正交相机下屏幕坐标 ∝ (world − cam)，
                    //    两者同向等量 ⇒ HUD 在屏幕上不动，晃的只有棋盘/卡牌/特效。（反向就错了：那是加倍晃。）
                    Check(hudRootT != null
                          && Mathf.Abs((hudRootT.localPosition - hudHome).y
                                       - (cam.transform.position - camHome).y) < 1e-5f,
                          "……HUD 根与相机**同向等量**平移 ⇒ 屏幕上 HUD 不动、只有战场在晃");

                    // 噪声是**三条带**叠出来的：最快那条（55.54 Hz × `m_FrequencyGain 0.05` = 2.777 Hz）
                    // 走半个周期时落到谷底（t ≈ 0.18 s），此时总位移该明显**低于**峰值
                    // —— 即它是「抖」而不是一路单调衰减。（t=0 三条带同相 ⇒ 恰好是峰值。）
                    CardTween.Advance(0.18f);
                    float dip = (cam.transform.position - camHome).y;
                    Check(dip > 0f && dip < CardFeel.ShakeWorldAmplitude * 0.45f,
                          $"……噪声三条带叠出**起伏**：t=0.18 s 落到 {dip:F4}（峰值 {CardFeel.ShakeWorldAmplitude:F2}）");

                    CardTween.Advance(0.25f);                    // 0.18 + 0.25 > 0.4 ⇒ 序列跑完
                    Check((cam.transform.position - camHome).magnitude < 1e-4f,
                          "……跑完把**相机放回原位**（不复位镜头会永久歪着，后面对局全跟着偏）");
                    Check(hudRootT != null && (hudRootT.localPosition - hudHome).magnitude < 1e-4f,
                          "……HUD 根也放回原位");
                }

                // ---- ⑧ 敌方手牌（2026-09-17 新加）----
                // 原版 `PlayerHand` MB 4350 + `CardsHorizontalLayout` MB 4053，**显示的是卡背**
                // （`PlayerHand__SetupCardInHand.c:45` → `ShowCardBack(!isPlayer)`）。
                // 规格正本 = `资料/敌方手牌_原版规格.md`；8 个字段 + 3 条曲线在 `HandLayout.ConfigureForEnemy()`。
                {
                    Check(drv.FoeHandCount > 0, $"敌方手牌建出来了（**{drv.FoeHandCount}** 张卡背）");
                    var qFirst = drv.FoeHandViewAt(0);
                    var qLast = drv.FoeHandViewAt(drv.FoeHandCount - 1);
                    Check(qFirst != null && qLast != null
                          && qFirst.transform.position.y > 0f && qLast.transform.position.y > 0f,
                          qFirst == null ? "（建不出敌方手牌视图）"
                          : $"……那排牌在**屏幕上半**（y {qFirst.transform.position.y:F2} / {qLast.transform.position.y:F2} > 0）"
                            + " —— 原版 `HandAnchor` 挂在**顶边**（距顶 0.32 px）");
                    if (qFirst != null && drv.FoeHandCount >= 3)
                    {
                        var qMid = drv.FoeHandViewAt(drv.FoeHandCount / 2);
                        if (qMid != null)
                            Check(qMid.transform.position.y < qFirst.transform.position.y,
                                  $"……弧线**中间往下凹**（中 {qMid.transform.position.y:F2} < 端 {qFirst.transform.position.y:F2}）"
                                  + " —— 这就是「从上方垂下来」，根因是原版 `m_maxHeight = -0.78`（**负的**）");
                    }
                    if (qFirst != null)
                    {
                        float wpx = qFirst.WorldW / LayoutSpace.VisibleWidth * 1920f;
                        float want = CardView.Width * HandLayout.EnemyCardScale * 108f;
                        Check(Mathf.Abs(wpx - want) < 2f,
                              $"……卡背宽 {wpx:F1} px = 原版的 {want:F1} px（`m_scale 0.54`；我方是 165 px）");
                    }
                    Check(Mathf.Abs(CardFeel.DealSeconds(true) - 0.3f) < 1e-6f
                          && Mathf.Abs(CardFeel.DealSeconds(false) - 0.15f) < 1e-6f,
                          $"敌我发牌用时**分开**：我方 {CardFeel.DealSeconds(true)} s / 敌方 {CardFeel.DealSeconds(false)} s"
                          + "（`timeToDrawPlayerCard` / `timeToDrawEnemyCard`）");
                }
            }
        }

        // ---- 15b. 悬停信息层（tooltip，2026-09-20 新建）----
        // 判据逐条照全量反编译（正本 `CardPresentation/Core/Tooltip.cs` 头部那一段）：
        //   enter 立刻显示 / exit 立刻隐藏 / 位置 = **触发器自己的位置** + offset（**不跟鼠标**）/
        //   按下鼠标收起 / **护甲那个容器原版就没有 tooltip** ⇒ 我们也不给。
        // 批处理没有鼠标 ⇒ 走 `TickTooltipAt(world)`（**和鼠标那条路同一个函数**）。
        Debug.Log(P + "--- 悬停信息层（tooltip）---");
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            if (drv == null) Check(false, "找不到 BattleDriver");
            else
            {
                CardView cv = null;
                for (int s = 0; s < 12 && cv == null; s++) cv = drv.BoardViewAt(s, true);
                Check(cv != null, $"场上有自己的卡可以悬停（{(cv != null ? cv.name : "没有")}）");
                if (cv != null)
                {
                    Tooltip.Hide(); Tooltip.FinishFade();
                    Check(!Tooltip.Visible, "先是没显示的");

                    int n0 = Tooltip.ShowCount;
                    bool hit = drv.TickTooltipAt(cv.StatWorld(CardView.StatMelee));
                    Check(hit && Tooltip.Visible, "把指针放到**近战数值**上 → tooltip 立刻显示（enter 立刻显示，无延迟）");
                    Check(Tooltip.ShownBody == TipText.Melee, "显示的是近战那一条");
                    Check(Tooltip.ShowCount == n0 + 1, "……而且只来了一条");
                    Tooltip.FinishFade();
                    Debug.Log(P + "  [DBG] tooltip: " + Tooltip.DebugDump());
                    Shot(Camera.main, "29_悬停tooltip");

                    drv.TickTooltipAt(cv.StatWorld(CardView.StatRanged));
                    Check(Tooltip.ShownBody == TipText.Ranged, "挪到**远程数值**上 → 换成远程那条");
                    drv.TickTooltipAt(cv.StatWorld(CardView.StatHealth));
                    Check(Tooltip.ShownBody == TipText.Health, "挪到**生命数值**上 → 换成生命那条");
                    drv.TickTooltipAt(cv.StatWorld(CardView.StatCost));
                    Check(Tooltip.ShownBody == TipText.Cost, "挪到**费用**上 → 换成费用那条");

                    // 🔴 原版**护甲容器没有 tooltip**（`子代理读报_2dcard_0827.md:95`）⇒ 我们也不给
                    Tooltip.Hide(); Tooltip.FinishFade();
                    drv.TickTooltipAt(cv.StatWorld(CardView.StatArmour));
                    Check(!Tooltip.Visible, "**护甲上没有 tooltip**（原版那个容器就没有触发器 —— 照原版）");

                    // 空白处：什么都不显示
                    drv.TickTooltipAt(new Vector3(60f, 60f, 0f));
                    Check(!Tooltip.Visible, "挪到空白处 → 立刻隐藏（exit 立刻隐藏）");

                    // 位置口径：**锚点那一套**（原版 `GetPivotPosition` 9 项表）——
                    // 同一张卡、同一个点，anchor 10(左中) 与 15(右中) 必须**落在位置的两侧**
                    Vector3 at = cv.StatWorld(CardView.StatHealth);
                    Tooltip.Show("x", at, 10);
                    var c10 = Tooltip.PanelCenter;
                    Tooltip.Show("x", at, 15);
                    var c15 = Tooltip.PanelCenter;
                    float wWorld = Tooltip.PanelSizePx.x / 108f;
                    // `pivot` 的语义（uGUI）：**pivot 那个点落在锚点上**。
                    // anchor 10 = MiddleLeft ⇒ pivot (0,0.5) = 面板**左**边贴着锚点 ⇒ 面板往**右**长；
                    // anchor 15 = MiddleRight ⇒ 往**左**长。**别凭直觉反着写**（第一版就是这样写反的）。
                    Check(c10.x > at.x && c15.x < at.x,
                              $"锚点真的在起作用：anchor 10(左中) 让面板往**右**长（{c10.x:F2} > {at.x:F2}）、"
                              + $"anchor 15(右中) 往**左**长（{c15.x:F2} < {at.x:F2}）");
                    Check(Mathf.Abs(Mathf.Abs(c15.x - c10.x) - wWorld) < 0.02f,
                              $"两侧相差正好一个面板宽（{Mathf.Abs(c15.x - c10.x):F2} ≈ {wWorld:F2}）");
                    Check(Mathf.Abs(c10.y - at.y) < 0.01f, "y 上不偏（MiddleLeft/MiddleRight 都是垂直居中）");
                    Tooltip.Hide(); Tooltip.FinishFade();
                    Check(!Tooltip.Visible, "收起来了");
                }
            }
        }

        // ---- 15c. 🆕 2026-09-21：**关键词（trait）的 tooltip** ----
        // **原版这条链**（全量反编译）：关键词段整项套 `<link=<DefinedTrait枚举名>>`
        //   （`GameStaticData__TraitNameToString.c:84-109`）→ `TextTooltipController` 每帧
        //   `TMP_TextUtilities.FindIntersectingLink` 命中（`…GetTraitTooltip.c:30,35-36`）
        //   → `EverguildTraitTooltipItem`（比基础版多 **图标 + 标题**）。
        // 我们这条：`CardText.KeywordSegment` 套 `<link=规范键>` → `CardView.LinkAt`
        //   → `TmpFont.LinkAt` → 文案 `TipText.Trait`（表由 `工具/gen_trait_tips.py` 从规则书生成）。
        Debug.Log(P + "--- 关键词 tooltip（trait）---");
        {
            // ① 文案表本身：**从规则书 61 条生成的**，不是手抄进 C# 的
            Check(TipText.TraitCount == 61,
                  $"trait 文案表 61 条（实际 {TipText.TraitCount}）—— 规则书 :161-225 就是 61 个关键词");
            string arm = TipText.Trait("armour");
            Check(!string.IsNullOrEmpty(arm) && arm.Contains("护甲") && arm.Contains(":167"),
                  "★ `armour` 的 tooltip = 标题（护甲/Armour）+ 规则书原文 + 出处 :167 —— 实得「" + arm + "」");
            Check(arm != null && arm.Contains("<sprite name=\"armour\">"),
                  "★ 标题行**带图标**（原版 `EverguildTraitTooltipItem` 比基础版多的就是图标 + 标题）");
            // 反例：规则书 61 条里没有的词 —— **只出名字 + 如实说明「没有解释」**，**不编一句解释**
            string ab = TipText.Trait("ability");
            Check(ab != null && ab.Contains("技能") && ab.Contains("规则书里没有这个词的条目"),
                  "★ 反例：自造词 `ability` 不在规则书 61 条里 ⇒ 只出名字 + **如实说「规则书里没有这个词的条目」**"
                  + "（不编解释）—— 实得「" + ab + "」");
            Check(TipText.Trait(null) == null && TipText.Trait("") == null,
                  "空键返回 null ⇒ 调用方**不弹面板**（连名字都凑不出来就什么都不显示）");

            // ② 剥标签那条兜底路：`<nobr>` / `<link>` **别原样印到点阵画面上**
            //    （2026-09-21 踩到：`StripTags` 原来只剥 `<sprite>`，而关键词段现在还带这两种）
            Check(CardIcons.StripTags("<link=armour><nobr><sprite name=\"armour\">Armour 2</nobr></link>") == "Armour 2",
                  "★ `StripTags` 把 `<sprite>`/`<nobr>`/`<link>` **一起**剥掉（点阵兜底那条路靠它）");

            // ②·B 效果**正文**里的行内图标也带 link（`CardIcons.Rewrite` 那一层，
            //      原版那 7 个显式 key 走的就是这条路 —— 不用做词形识别）
            Check(Badges.KeyOf("frenzied") == "destroyer" && Badges.KeyOf("rage") == "penitence"
                  && Badges.KeyOf("markOfChaos") == "darkpact" && Badges.KeyOf("concussive") == "concussion",
                  "★ 图名 → 规范键：**别名那 4 个转得回来**（`frenzied`→`destroyer` …）");
            Check(Badges.KeyOf("rally") == "rally" && Badges.KeyOf("SpiritStone_3") == "spiritstone"
                  && Badges.KeyOf("questPoints2") == "questpoints" && Badges.KeyOf("Melee") == "melee",
                  "★ ……其余小写化即规范键；`SpiritStone_3` / `questPoints2` 的**档位数字要剥掉**");
            {
                int scanned = 0, withLink = 0;
                foreach (var c in RuleEngine.CardDatabase.Load())
                {
                    if (c == null || !c.FromOriginalPool || string.IsNullOrEmpty(c.Desc)) continue;
                    scanned++;
                    if (CardIcons.Rewrite(c.Id, "desc", c.Desc).IndexOf("<link=", System.StringComparison.Ordinal) >= 0)
                        withLink++;
                }
                Check(scanned > 1000, $"扫过 {scanned} 张原版卡的 `desc`");
                Check(withLink > 100, $"★ 效果正文里带 `<link>` 的卡 **{withLink} 张**"
                                    + "（行内图标那条路接了 link ⇒ 悬停图标能出解释）");
                // 反例：正文里**什么都没有**的卡不该被塞进 link
                Check(CardIcons.Rewrite("UM_Light_Cover", "desc", "Hello world") == "Hello world",
                      "★ 反例：正文里没有记号 ⇒ `Rewrite` 原样返回、不塞 link");
            }

            // ②·C 🔴 **折行不能把标签掐断**（2026-09-21 踩到：`Wrap` 原来是**逐字符**折的，
            //      行满时正好落在标签中间就会在 `<sprite name="codex">` **里面**插一个 `\n`，
            //      TMP 认不出半个标签 ⇒ 把标签原样印出来）。
            //      不变量：折行**只插入 `\n`、不改一个字** ⇒ 去掉 `\n` 必须一字不差还原。
            {
                string longBody = TipText.Trait("armour") + " " + TipText.Trait("rally") + " "
                                + TipText.Trait("concussive") + " 这一行故意写长，逼它折几次行。";
                string wrapped = Tooltip.WrapForTest(longBody);
                Check(wrapped.IndexOf('\n') >= 0, "……先确认**确实折了行**（不然这条断言是空转）");
                Check(wrapped.Replace("\n", "") == longBody.Replace("\n", ""),
                      "★ 折行只插 `\\n`、不改字（标签不会被掐断）—— 实得「"
                      + wrapped.Replace("\n", "⏎") + "」");
                Check(wrapped.Contains("<sprite name=\"armour\">"),
                      "★ ……而且 `<sprite name=\"armour\">` 整段还在（没被拆成两半）");
            }

            // ③ 端到端：**手牌**上真的能命中（手牌走完整 `2DCard`；场上那套把关键词层关掉了，
            //    所以这条只能在手牌上验）。扫的是 TMP 自己报的包围盒 —— 不赌某一个点的对齐。
            var drv = Object.FindObjectOfType<BattleDriver>();
            if (drv == null) Check(false, "找不到 BattleDriver");
            else
            {
                string foundKey = null; Vector3 foundAt = Vector3.zero;
                int cards = 0, probes = 0;
                for (int i = 0; i < drv.HandCount && foundKey == null; i++)
                {
                    var v = drv.HandViewAt(i);
                    if (v == null || !v.gameObject.activeSelf) continue;
                    Vector3 c; Vector2 h;
                    if (!v.KeywordRect(out c, out h)) continue;
                    cards++;
                    for (int a = 1; a <= 9 && foundKey == null; a++)
                        for (int b = 1; b <= 5 && foundKey == null; b++)
                        {
                            Vector3 p = c + new Vector3((a / 10f - 0.5f) * 2f * h.x,
                                                        (b / 6f - 0.5f) * 2f * h.y, 0f);
                            probes++;
                            string k = v.LinkAt(p, cam);
                            if (!string.IsNullOrEmpty(k)) { foundKey = k; foundAt = p; }
                        }
                }
                Check(foundKey != null,
                      $"★ 手牌的关键词层里能命中的 `<link>` 找到了（扫了 {cards} 张卡的包围盒 / {probes} 个点）"
                      + (foundKey != null ? $"：`{foundKey}`" : ""));
                if (foundKey != null)
                {
                    Tooltip.Hide(); Tooltip.FinishFade();
                    int n0 = Tooltip.ShowCount;
                    bool hit = drv.TickTooltipAt(foundAt);
                    Check(hit && Tooltip.Visible, "★ 悬停关键词 → trait tooltip 弹出来");
                    Check(Tooltip.ShownBody == TipText.Trait(foundKey),
                          "★ 弹的是**这个词**那一条（实得「" + Tooltip.ShownBody + "」）");
                    Check(Tooltip.ShowCount == n0 + 1, "……而且只来了一条");
                    Tooltip.FinishFade();
                    Debug.Log(P + "  [DBG] trait tooltip: " + Tooltip.DebugDump());
                    Shot(cam, "30_悬停关键词tooltip");
                    Tooltip.Hide(); Tooltip.FinishFade();
                }
            }
        }

        // ---- 16. 「原版有、我们原来缺」的 HUD 件（2026-09-13 补摆）----
        // 清单与绝对坐标出自 `资料/战斗UI_原版对账表.md` §三点五③；逐件出处写在 `BuildHudExtras` 里。
        // 这一节验的是「**摆上去了、图取得到、位置和资料对得上**」—— 那批件全是显示件，没有行为可验。
        Debug.Log(P + "--- 原版有、我们原来缺的 HUD 件 ---");
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            if (drv == null) Check(false, "找不到 BattleDriver");
            else
            {
                drv.Begin("Ultramarines", "Goff", 20260916);
                Step(0.3f);

                Check(drv.HudExtraCount == 10, $"补摆的图 {drv.HudExtraCount} 件（应有 10：头衔底条×2 / 头像块×2 / 三个按钮 / 能量累积×2 / 加时标记）");
                // ⚠️ 这条是「不许静默失败」：图名字写错、资源没同步进来，都会在这里红
                Check(drv.HudExtrasMissingArt() == 0, $"这 10 件的贴图**都取到了**（缺图 {drv.HudExtrasMissingArt()} 件）");
                Debug.Log(P + "   补摆清单：\n" + drv.HudExtraReport());

                // 位置：和资料里的绝对矩形**中心**比（每件都在 1.5 px 内）
                void At(string name, float wantX, float wantY)
                {
                    var p = drv.HudExtraPosPx(name);
                    Check(Mathf.Abs(p.x - wantX) < 1.5f && Mathf.Abs(p.y - wantY) < 1.5f,
                          $"{name} 中心 ({p.x:F1},{p.y:F1}) ≈ 资料 ({wantX},{wantY})");
                }
                At("TitleBackground_Me", 209.7f, 1049.5f);      // 我 x[54.2,365.2] y[1028.5,1070.5]
                At("TitleBackground_Foe", 210.0f, 113.8f);      // 敌 x[54.5,365.5] y[92.8,134.8]
                // 🔴 2026-09-24 改：这两件原来钉的是**错值**（容器中心 58.15,997.65 ⇒ 偏高 11.9 px），
                //    而且只钉了我方。真判据（见 `BattleDriver.BuildHudExtras` 那一大段注释）：
                //    `Border` 自己的 rect 我 y[960.0,1059.1] / 敌 y[24.5,123.6]，
                //    再 ×`m_LocalScale 1.25`（绕 pivot 中心，中心不变）、按 256×286 等比 ⇒ 实绘 110.88×123.88。
                At("AvatarItemSmall_Me", 57.15f, 1009.55f);     // 中心 (57.15,1009.55) · 实绘 110.88×123.88
                At("AvatarItemSmall_Foe", 58.45f, 74.05f);      // 中心 (58.45,74.05) —— **敌方那个原来没建**
                At("ChatButton", 83.15f, 911.1f);               // x[50.9,115.4] y[880.2,942.0]
                At("CenterCameraButton", 50.15f, 599.1f);       // x[17.9,82.4] y[568.2,630.0]
                At("OffensiveButton", 54.5f, 500.35f);          // x[0,109] y[446.9,553.8]
                At("EnergyAccumulation_Foe", 1785.55f, 287.95f);
                // 🔴 **2026-10-14 订正**：本行原来钉的是**我们自己的错值**（`1785.6 / 555.65`）——
                //    我方那盏能量灯原来**照抄了敌方的 x**（`1746.7`），真值 `1748.5645137293218`
                //    （两证：`子代理读报_back右区_0827.md:162` 印的 x[1748.6,1826.4] + **实况 dump**
                //     `runtime_ui_dump_drive_0912.tsv:287` 我 `pos(-80.2,0.5)` / `:258` 敌 `pos(-81.3,0.5)`）
                //    ⇒ 修完之后实测中心 = **1787.4575**（改前 1785.6 差 1.86 px = **真偏离**）。
                //    全过程 → `资料/普查产出_1014/Block12_BattleDriver族.md` §2.3 / §2.6。
                //    ⛔ **别反过来把实现改回 1746.7**（那是错的，就是这一条断言当年把它钉死的）。
                At("EnergyAccumulation_Me", 1787.46f, 555.6f);  // 我 x[1748.6,1826.4] y[515.5,595.7]
                At("OvertimeIndicator", 1753.2f, 377.0f);       // x[1718.9,1787.5] y[341.5,412.5]

                // ---- 🆕 2026-10-13（A513）：`ChatButton` 的**细口径**（上面那行 At(…) 是粗口径 1.5 px，两档并存）----
                // 判据（原版直读 · 父链走完）→ `BattleDriver.BuildHudExtras` 里 `_chatBtn` 那一大段：
                //   `RectTransform_2984.json`（`ChatButton`，挂 GO 85）的 `anchoredPosition (19.219999313354492, 110.0)`
                //   + `sizeDelta (64.44300079345703, 61.84600067138672)` + 父链（`PlayerInfo` 260×75 @(31.7001953125,28)
                //   → 四层 stretch、零偏移 → 裸 `Transform`）⇒ 绝对 x[50.9201953125,115.36319610595703]
                //   y[880.15399932861328,942.0] ⇒ **中心 = (83.14169570922852, 911.0769996643066)**（y 从上）。
                //   `LayoutSpace.ToWorld` 收的 y01 是**从下**的 ⇒ 168.9230003356934 = 1080 − 911.0769996643066。
                //   ⚠️ 期望值是**原版字面量**换算出来的（⛔ 不读 `BuildHud` 传的那四个数）。
                // 🧨 **改坏法**：`BuildHudExtras` 里那四个 px 写成取整值（`50.9 / 880.2 / 64.44 / 61.85`）
                //   ⇒ 中心 (83.1200, 911.1250) 偏 0.0217 / 0.048 px = **2.0e-4 / 4.4e-4 世界单位** ⇒ 本条红
                //   （2026-10-13 之前就是这个状态 —— 与 A423 那颗钮同一个病）。⛔ 别放宽阈值：0.011 px 是
                //   「照原版精确值写」的提醒。
                {
                    Vector3 want513 = drv.hudRoot != null
                        ? drv.hudRoot.TransformPoint(LayoutSpace.ToWorld(83.14169570922852f / 1920f, 168.9230003356934f / 1080f))
                        : LayoutSpace.ToWorld(83.14169570922852f / 1920f, 168.9230003356934f / 1080f);
                    var p513 = drv.HudExtraWorldPos("ChatButton");
                    var d513 = p513 - want513;
                    Check(Mathf.Abs(d513.x) < 1e-4f && Mathf.Abs(d513.y) < 1e-4f,
                          "★ A513：`ChatButton` 落在**原版那个位置**上（中心 = (83.14169570922852, 911.0769996643066) px，y 从上）"
                        + $" —— 实得偏 ({d513.x:F4}, {d513.y:F4}) 世界单位（阈值 1e-4 世界单位 ≈ 0.011 px；"
                        + "同一件在上面还有一条粗口径 1.5 px 的）");
                }

                // ---- 🆕 2026-10-14（A531 = A513 的另一半）：上面那 10 行 `At(…)` 是**粗口径**（1.5 px），
                //      这里把剩下这 **8 件**也用**原版精确值**再钉一遍（阈值 1e-4 世界单位 ≈ 0.0108 px，
                //      与 `ChatButton` 那条同档；`ChatButton` / `CenterCameraButton` 两件上一轮已各自收口）。
                // 判据（原版直读 · 父链走完）逐件印在 `BattleDriver.BuildHudExtras` 对应那一段的注释里
                //   （`RectTransform_<pid>.json` 的 `anchoredPosition` + `sizeDelta` + 父链 ⇒ 绝对矩形 ⇒ 中心）——
                //   ⚠️ 期望值是**原版字面量**换算出来的（⛔ 不读 `BuildHudExtras` 传的那四个数，那是我们这一侧）。
                //   ⚠️ 换算式与 `HudAbs` **逐字同一条**：中心 = (`x + w/2`, `y + h/2`)，y 从**上**往下数。
                // 逐件「旧值 → 新值 · 旧值偏多少」→ `资料/普查产出_1014/Block12_BattleDriver族.md` §2.2 / §2.5。
                // 🧨 **改坏法**：`BuildHudExtras` 里那四个 px 写回 `子代理读报_*_0827.md` 那种**取整到 0.1**
                //   的写法 ⇒ 最大一件偏 **1.86 px**（`EnergyAccumulation_Me` 的 x，= 当年那处真偏离），
                //   另有 8 件偏 0.012~0.047 px（**都超过本档 0.011 px 的阈值**）⇒ 下面这一片红
                //   （2026-10-14 之前就是这个状态 —— 与 A423/A513 那两颗钮同一个病）。
                void AtExact(string exName, float exCx, float exCy)
                {
                    Vector3 wantEx = drv.hudRoot != null
                        ? drv.hudRoot.TransformPoint(LayoutSpace.ToWorld(exCx / 1920f, (1080f - exCy) / 1080f))
                        : LayoutSpace.ToWorld(exCx / 1920f, (1080f - exCy) / 1080f);
                    var pEx = drv.HudExtraWorldPos(exName);
                    var dEx = pEx - wantEx;
                    Check(Mathf.Abs(dEx.x) < 1e-4f && Mathf.Abs(dEx.y) < 1e-4f,
                          $"★ A531：`{exName}` 落在**原版那个位置**上（中心 = ({exCx}, {exCy}) px，y 从上）"
                        + $" —— 实得偏 ({dEx.x:F4}, {dEx.y:F4}) 世界单位（阈值 1e-4 世界单位 ≈ 0.011 px；"
                        + "同一件在上面还有一条粗口径 1.5 px 的）");
                }
                AtExact("TitleBackground_Me",     209.7001953125f,   1049.5051536560059f);
                AtExact("TitleBackground_Foe",    210.0f,            113.75257110595703f);
                AtExact("AvatarItemSmall_Me",     57.15119171142578f, 1009.5327754654946f);
                AtExact("AvatarItemSmall_Foe",    58.45099639892578f, 74.08019215250636f);
                AtExact("OffensiveButton",        54.50400161743164f, 500.32849979400635f);
                AtExact("EnergyAccumulation_Foe", 1785.550192279f,   287.961250148f);
                AtExact("EnergyAccumulation_Me",  1787.457517193f,   555.596562371f);
                AtExact("OvertimeIndicator",      1753.1948928833008f, 377.0f);

                // 任务点：🔴 **只属于暗黑天使**（2026-09-13 更正 —— 原来无条件摆给全部 13 个阵营，
                // 是**张冠李戴**；机器码级出处见 `BattleDriver.ShowsQuestPoints` 的注释）。
                // 做法照原版：**物件照建、`SetActive` 切显隐** ⇒ 数字与坐标仍然量得到，只是藏着。
                // ⚠️ 这一局的双方都不是暗黑天使 ⇒ **一件都不该可见**。
                //    （原来这条断言只写 `QpText == "0/3"` —— 它只证明「建出来了」，
                //     证明不了「该不该显示」，所以这个错一直没被抓到。）
                Check(!drv.QuestPointsVisible(true) && !drv.QuestPointsVisible(false),
                      "双方都不是暗黑天使 → 任务点那一组**都不显示**（原来无条件摆给所有阵营，是张冠李戴）");
                // ✅ 2026-09-13 第三十三轮：**任务点机制有了**（`PlayerState.QuestPoints`）——
                //    那批 DarkAngels 卡的卡面图标就是任务点徽记（OCR 把它丢了，只留 `Gain 1`）。
                //    这一局双方都是 0，所以显示仍是 `0/3`，但**它现在是引擎算出来的真值**了。
                Check(drv.QpText == "0/3",
                      $"任务点数字「{drv.QpText}」= 引擎真值 `X/3`（这一局双方都是 0）");
                Check(!BattleDriver.ShowsQuestPoints("Ultramarines")
                      && !BattleDriver.ShowsQuestPoints("Goff")
                      && BattleDriver.ShowsQuestPoints("DarkAngels"),
                      "任务点**只给暗黑天使**（原版按督军阵营开关：`cmp [督军+0x2c],0x6e`）");

                // ---- 阵营资源那两件（信仰 / 灵魂石，2026-09-13 第三十三轮）----
                // ✅ **2026-09-18 判据定案：按阵营**（原版 `RawCardScript.Uses*` 就是
                //    `督军卡 + 0x2c` 跟 30/80/110 比 —— 见 `ShowsSpiritStone` 的注释）。
                //    本局是 Ultramarines vs Goff，两个都不在这三个阵营里 ⇒ 两组都不显示。
                Check(!drv.FaithVisible(true) && !drv.FaithVisible(false)
                      && !drv.SpiritStoneVisible(true) && !drv.SpiritStoneVisible(false),
                      "本局双方（Ultramarines / Goff）→ 信仰与灵魂石**都不显示**（按阵营）");
                // 🔴 **正例与反例必须成对** —— 只验「不显示」的话，「永远不显示」也能过。
                Check(BattleDriver.ShowsFaith(BattleDriver.FaithFaction)
                      && !BattleDriver.ShowsFaith("Ultramarines"),
                      "判据 `ShowsFaith`：修女 → 显示 · 别的阵营 → 不显示");
                Check(BattleDriver.ShowsSpiritStone(BattleDriver.SpiritStoneFaction)
                      && !BattleDriver.ShowsSpiritStone("Ultramarines"),
                      "判据 `ShowsSpiritStone`：灵族 → 显示 · 别的阵营 → 不显示");
                // 🆕 **这次改动的要害：判据与「当前有没有值」解耦**（原版就是这样）。
                //    改之前 `ShowsFactionResource(value) = value > 0` ⇒ 这条会红（0 值整组不显示）。
                Check(BattleDriver.ShowsFaith("Sororitas") && BattleDriver.ShowsSpiritStone("SaimHann"),
                      "★ **计数为 0 也照样显示**（判据是阵营、不是值 —— 改之前这条会红）");
                Check(drv.FaithTex == "40k_Battle_Display_Faith",
                      $"信仰那张图取到了：{drv.FaithTex}（取不到 = 美术没同步进来）");
                Check(drv.StoneGemTex == "UI_Gem_Eldar",
                      $"灵魂石那颗宝石取到了：{drv.StoneGemTex}");
                var qPos = drv.HudExtraPosPx("__none__");        // 只为确认找不到时返回 (-1,-1)
                Check(qPos.x < 0f, "查不到的名字返回 (-1,-1)（自检自己的哨兵值）");

                // **z 序**（同 z 的两张图谁压谁不确定，只能靠断言钉）：
                //   🔴 2026-09-24 改：判据是**原版同级顺序**（直读 `RectTransform_3189.json` 的 `m_Children`）
                //   = `[NameBackground, TitleBackground, Avatar Item Small, PlayerNameText]`
                //   —— UGUI 后出现的兄弟画在上面 ⇒ 底条**压名牌**、头像块**压底条**。
                //   ⛔ 旧断言钉的是「底条在名牌后面」，那是**没有出处**的写法（真事故是「同 z 不确定」）。
                Check(drv.HudExtraZDelta("TitleBackground_Me") > 0f,
                      $"头衔底条在名牌**前面**（z 差 {drv.HudExtraZDelta("TitleBackground_Me"):F2}，原版它排在 NameBackground 之后）");
                Check(drv.HudExtraZDelta("AvatarItemSmall_Me") > drv.HudExtraZDelta("TitleBackground_Me"),
                      "头像块又压在头衔底条**前面**（原版同类里它排在 TitleBackground 之后）");
                Check(drv.HudExtraZDelta("AvatarItemSmall_Foe") > 0f,
                      $"敌方头像块也在名牌**前面**（z 差 {drv.HudExtraZDelta("AvatarItemSmall_Foe"):F2}）");

                // ---- 称号（2026-09-24 新接：原来是**常显的空底条**）----
                // 🔴 判据 = 原版 `PlayerProfileUIController.SetProfileTitle`：
                //    `SetActive(titleGO, !IsNullOrEmpty(title))` —— 没称号 ⇒ **底条连同文字一起关**。
                //    实况 dump 里 `TitleBackground.activeSelf = False`（原版关服、玩家没称号）。
                // ⚠️ **正例与反例必须成对** —— 只验「不显示」的话，「永远不显示」也能过。
                Check(!drv.TitleVisible(true) && !drv.TitleVisible(false)
                      && !drv.TitleBgVisible(true) && !drv.TitleBgVisible(false),
                      "单机没有玩家资料 ⇒ 称号**整块不显示**（底条 + 文字一起关，与原版实况一致）");
                drv.SetTitle("测试称号", null);              // 正例：给一个称号
                Check(drv.TitleVisible(true) && drv.TitleBgVisible(true)
                      && drv.TitleTextOf(true) == "测试称号"
                      && !drv.TitleVisible(false) && !drv.TitleBgVisible(false),
                      $"★ 有称号 ⇒ 底条 + 文字**一起亮**，且只亮给了的那一侧（文本「{drv.TitleTextOf(true)}」）");
                drv.SetTitle(null, null);                    // 复位（后面的截图要的是原版实况那副样子）
                Check(!drv.TitleVisible(true) && !drv.TitleBgVisible(true),
                      "清掉称号 ⇒ 又整块关回去（判据跟着数据走，不是一次性开关）");
                // 字号与位置（**别拿常量自证**：这里比的是 TMP 渲出来的实际字号 `FontPxNow`）
                Check(Mathf.Abs(drv.TitleFontPxNow(true) - BattleDriver.TitleFontPx) < 0.6f
                      && Mathf.Abs(drv.TitleFontPxNow(false) - BattleDriver.TitleFontPx) < 0.6f,
                      $"称号字号实测 {drv.TitleFontPxNow(true):F2} / {drv.TitleFontPxNow(false):F2} px ≈ 原版 m_fontSize {BattleDriver.TitleFontPx}");
                var tpM = drv.TitlePosPx(true); var tpF = drv.TitlePosPx(false);
                Check(Mathf.Abs(tpM.x - 227.1f) < 1.5f && Mathf.Abs(tpM.y - 1044.05f) < 1.5f
                      && Mathf.Abs(tpF.x - 227.4f) < 1.5f && Mathf.Abs(tpF.y - 108.3f) < 1.5f,
                      $"称号文字中心 我({tpM.x:F1},{tpM.y:F1}) 敌({tpF.x:F1},{tpF.y:F1}) ≈ 原版 (227.1,1044.05)/(227.4,108.3)");

                // 加时标记：**默认关着**，图要在（原版也只在加时里出现；🆕 2026-09-20 起机制接上了）
                Check(!drv.OvertimeVisible, "加时标记默认**不显示**（原版 `OvertimeUi.Awake` 也是关着的）");
                Check(drv.OvertimeTex == "40k_icon_overtime", $"……但图已经接好了：{drv.OvertimeTex}");

                // 🆕 2026-09-20 加时 splash：手动播一次，逐帧推淡入/停/淡出，并拍一张图
                //    （原版 `OvertimeUi.DisplayOvertime`：淡入 1.0 / 停 1.0 / 淡出 1.0，`fadeTime` = 资产值 1.0）
                drv.ShowOvertime();
                // 🆕 2026-09-22 加时**音效**：原版 `OvertimeUi.enteringOvertimeSound` = AudioCue `OvertimeStart`
                //    （`bundle_soundcollection_assets_all/MonoBehaviour/OvertimeStart.json`：pitch/volume 全 1.0）。
                //    音频本地原本是**缺 setup 头的 FSB5 裸流**（播不了）⇒ 已重建，见 `工具/rebuild_overtime_start_ogg.py`。
                //    判据 = **clip 真的加载得到**（只调 `Play` 不算 —— 加载不到时 `WFSoundBank.Clip` 只打警告）。
                var otClip = WarpforgeVFX.WFSoundBank.Clip("OvertimeStart");
                Check(otClip != null, "★ 加时音效 `OvertimeStart` 加载得到"
                      + "（丢了就跑 `python 工具/rebuild_overtime_start_ogg.py` 重建）");
                if (otClip != null)
                    Check(Mathf.Abs(otClip.length - 5.4211f) < 0.01f && otClip.frequency == 48000,
                          $"★ ……而且是对的这份：{otClip.length:F4} s @ {otClip.frequency} Hz（原版 `m_Length = 5.421083` s / `m_Frequency = 48000`）");
                drv.TickOvertime(0f);
                Check(drv.OvertimeSplashVisible, "★ 播加时 splash 后**它亮起来了**");
                // 🔴 反例：**字号**。踩过：把原版的 `m_fontSize = 80` 当本工程的单位用
                //    （`SetCapHeight(80 * WorldCapPerFontSize)`）⇒ 一个字母占了大半屏 ≈335 px。
                //    跨工程能对的只有**渲染高度**：原版 80 pt ⇒ 大写高 0.72 em ≈ 57.6 px ≈ 0.533 世界单位。
                var ts = drv.OvertimeSplashTextSize;
                Check(ts.x > 0.5f && ts.x < 9f && ts.y > 0.2f && ts.y < 2.5f,
                      $"★ splash 字号合理：`OVERTIME!` 渲染成 {ts.x:F2}×{ts.y:F2} 世界单位"
                      + "（屏宽 17.78 世界单位；照抄原版 80 当本工程单位会让它铺满整屏）");
                Check(drv.OvertimeSplashAlpha < 0.01f, $"……从 α≈0 开始淡入（现在 {drv.OvertimeSplashAlpha:F3}）");
                drv.TickOvertime(1.0f);                       // 淡入结束
                Check(Mathf.Abs(drv.OvertimeSplashAlpha - 1f) < 0.01f,
                      $"★ 1.0 s 后淡入到满（原版 fadeTime = 1.0，现在 α={drv.OvertimeSplashAlpha:F3}）");
                Shot(cam, "33_加时splash");
                drv.TickOvertime(1.0f);                       // 停留结束
                Check(Mathf.Abs(drv.OvertimeSplashAlpha - 1f) < 0.01f, "再停 1.0 s 期间保持满");
                drv.TickOvertime(1.0f);                       // 淡出结束
                Check(drv.OvertimeSplashAlpha < 0.01f, $"★ 再 1.0 s 淡出到 0（现在 {drv.OvertimeSplashAlpha:F3}）");
                Check(!drv.OvertimeSplashVisible && !drv.OvertimeVisible,
                      "★ 播完**两个都收起来**（原版 `AppendCallback` 那条路 —— 它是「闪一下」，不是常亮）");

                Shot(cam, "23_HUD补摆件");
            }
        }

        // ---- 17. 开局换牌（原版 `Mulligan` 子树 / 规则书 :46）----
        // 引擎侧的规则账在 `RuleEngineTest.TestMulligan` 里（15 条）；这一节验的是**画面这一侧**：
        // 换牌在 BeginTurn **之前**、面板的件都在、点「换」真的标记、点「完成」真的换掉并开打。
        Debug.Log(P + "--- 开局换牌（Mulligan）---");
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            if (drv == null) Check(false, "找不到 BattleDriver");
            else
            {
                string Sig(BattleContext c)
                {
                    var l = new List<string>();
                    // ⚠️ **凭空生成的牌不算**（第三十四轮）：换牌结束时 `BeginTurn` 里的**天赋**
                    //    会从卡池现拿一张塞进手牌，而这张签名比的是「**卡组自己的牌有没有多/少**」。
                    // 🔴 2026-09-18 第 7 行第 3 步：标记住在**每一份**上 ⇒ 直接看这一份的
                    //    `EphemeralMarked`。那个「按份数扣、防同名被一起跳过」的绕法**删掉了** ——
                    //    它当年存在的唯一理由就是「卡模板共享、同名那两张分不开」，现在分得开了。
                    foreach (var x in c.Players[0].Hand)
                        if (!x.EphemeralMarked) l.Add(x.Card.Name);
                    foreach (var x in c.Players[0].Deck) l.Add(x.Card.Name);
                    foreach (var x in c.Players[0].Discard) l.Add(x.Card.Name);
                    l.Sort();
                    return string.Join(",", l.ToArray());
                }

                bool saved = drv.mulliganEnabled;
                drv.mulliganEnabled = true;
                // 🆕 2026-09-29（§25）：**这一段也要把「选进攻卡」关掉** —— 换牌一完成，原版就会弹
                //   `SetupEnviromentalEffectPhase` 那个面板并**停住**（等玩家点「继续」）；
                //   这一节验的是换牌本身，不停住的话「这时才发能量 / 抽第 1 张」那些断言会假红
                //   （2026-09-29 实测：三条红都是这么来的）。
                bool savedOff = drv.offensivePhaseEnabled;
                drv.offensivePhaseEnabled = false;
                drv.Begin("Ultramarines", "Goff", 20260917);

                Check(drv.InMulligan, "Begin 之后**先进换牌阶段**");
                // 这两条是「换牌发生在 BeginTurn 之前」的硬证据
                Check(drv.Ctx.Players[0].Energy == 0, $"……这时**还没发能量**（{drv.Ctx.Players[0].Energy}）");
                Check(drv.Ctx.Players[0].Hand.Count == RuleCore.StartHand,
                      $"……也**还没抽第 1 张**（手牌 {drv.Ctx.Players[0].Hand.Count} = 起手 {RuleCore.StartHand}）");

                var mp = drv.Mulligan;
                Check(mp != null && mp.Visible, "换牌面板开着");
                Check(mp.BarHasArt && mp.PlayHasArt && mp.EyeHasArt,
                      $"面板的件都取到图了（底「{mp.BarTex}」/ 眼睛「{mp.EyeTex}」）");
                Check(mp.CardButtonCount == drv.HandCount,
                      $"每张起手牌上都贴了「换」按钮（{mp.CardButtonCount} 个 == 手牌 {drv.HandCount} 张）");
                // 🔴 **A245：这条原来没有判别力** —— `PromptText` 在 `_prompt == null` 时返 `"<无>"`
                //    （**非空串**）⇒ 「提示行压根没建出来」那一档**照样通过**（两态分不开 = 等于没查）。
                //    现在那个 getter 返 `null`（见 `Battle/MulliganPanel.cs` 的 `PromptText`）⇒ 两态可分了。
                //    ⚠️ 今天**不可达**（`Label.Create` 恒不返回 null）⇒ 这是**潜在**缺口、不是现患。
                //    改坏法：把 `MulliganPanel.Create` 里建 `_prompt` 那一句删掉（或让它为 null）⇒
                //    下面这条会印出 `(null …)` 并**当场红**。
                Check(!string.IsNullOrEmpty(mp.PromptText),
                      $"提示行写着「{mp.PromptText ?? "(null —— 这一行没建出来)"}」");
                // 🆕 2026-09-26：**「你先手 / 你后手」那一行**（原版 `MulliganText/TurnText`）。
                //   🔴 **它是原版唯一一处「先手/后手」的表现** —— 原版没有硬币资产/动画/音效（判据 → §2.8）。
                Check(mp.TurnText == (drv.Ctx.FirstSeat == 0 ? MulliganPanel.TurnFirst : MulliganPanel.TurnSecond),
                      $"★ 换牌面板那行「你先手 / 你后手」跟**这一局谁先手**对得上：「{mp.TurnText}」"
                    + $"（先手 = {drv.Ctx.Players[drv.Ctx.FirstSeat].Name}）");
                // 🔴 **2026-09-30 新加**：那两行的**颜色**照原版 —— **纯白**。
                //   判据（亲读原版资产）= `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_3731.json`
                //   （`Choose cards to replace in first hand`）与 `MonoBehaviour_3856.json`（`You go second`）的
                //   **`m_fontColor32` 都是 `4294967295`（= `0xFFFFFFFF` 纯白）**、`m_fontColor` 都是 `(1,1,1,1)`。
                //   ⚠️ 原来**两行都是暖色 `(1, 0.94, 0.82)`**（我们挑的，没有出处）⇒ 已改成纯白。
                // ⚠️ **A195 核过：这个 `&&` 门是安全的**（没改）—— 它**不可能为假**：
                //    `Label.Create` **从不返回 null**（`Battle/Label.cs:64` 无条件 `return l`；TMP 不在时退**点阵**
                //    后端、`_tmp` 为 null 但 Label 对象照样在），而两行 label 是 `MulliganPanel.Create` 里无条件建的
                //    ⇒ 只有「整具面板没建出来」才会为空，那一档上面 `Check(mp != null && mp.Visible, "换牌面板开着")` 已经红了。
                if (mp.PromptLabel != null && mp.TurnLabel != null)
                {
                    var c1 = mp.PromptLabel.color; var c2 = mp.TurnLabel.color;
                    Check(c1.r > 0.999f && c1.g > 0.999f && c1.b > 0.999f
                       && c2.r > 0.999f && c2.g > 0.999f && c2.b > 0.999f,
                          $"★ 换牌那两行的颜色 = **原版纯白**（`m_fontColor32 = 0xFFFFFFFF`）—— 实测"
                        + $" 提示行 ({c1.r:F3},{c1.g:F3},{c1.b:F3}) · 先后手行 ({c2.r:F3},{c2.g:F3},{c2.b:F3})"
                        + "（原来两行都是暖色 (1,0.94,0.82)，无出处）");
                }
                // 🔴 **2026-09-29 新加**：这一行**照原版 rect 摆**（原来是我们自己放在其下方 21.5 px 处）。
                //   判据 = `bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_3403.json`：
                //   原版 `MulliganText/TurnText` 左上 (312.50, 129.42) · 1307.06×54.17 ⇒ 中心 **(966.03, 156.50)**。
                //   ⚠️ 量的是**渲染出来那个 label 的实际位置**（不是拿常量跟自己比 —— 那是自证）。
                // ⚠️ **A195 核过：这个门是安全的**（没改）—— 上面刚断过 `mp.TurnLabel != null`；
                //    真为空时 `mp.TurnText` 返 `null`（**A245 起**；原来返 `"<无>"`）⇒ 两种取值都 ≠
                //    那两个字面量 ⇒ `Check(mp.TurnText == …)` 当场红（判别力不变）。
                if (mp.TurnLabel != null)
                {
                    var tw = mp.TurnLabel.transform.position;
                    float tcx = LayoutSpace.PxX(tw.x), tcy = LayoutSpace.PxY(tw.y);
                    Check(Mathf.Abs(tcx - 966.03f) < 1.5f && Mathf.Abs(tcy - 156.5f) < 1.5f,
                          $"★ 换牌那行的位置 = **原版 rect**（实得中心 ({tcx:F1}, {tcy:F1})，原版 (966.0, 156.5)）"
                        + " —— 原来我们自己放在 y=178（低 21.5 px、高多 5.8 px）");
                }
                Check(!drv.TurnLabelVisible, "换牌阶段**不显示回合行**（对局还没开始，写「第 0 回合」是误导）");
                // 🔴 **2026-09-27（PA 普查）**：原版 `MulliganContinueButton/Button` 是 **PA=0**（`m_Type=0` Simple）
                //   ⇒ `40k_bt_underbutton`（485×83）**拉满 577.5×63.84**；而 `ImageQuad` 默认按**贴图比例**定宽
                //   ⇒ 补 `SetAspect` 之前我们只画出 **372.8 宽（窄 204.7px）**。
                //   ⚠️ 量的是**渲染尺寸**、不是框 —— 框一直是对的，错的是往里画多大。
                Check(Mathf.Abs(mp.BarWorldW * 108f - 577.5f) < 1.5f
                   && Mathf.Abs(mp.BarWorldH * 108f - 63.84f) < 1.5f,
                      $"★ 换牌底条渲染 = 原版 **577.5×63.84**（PA=0 拉满）—— 实测 "
                    + $"{mp.BarWorldW * 108f:F1}×{mp.BarWorldH * 108f:F1}");
                Debug.Log(P + "   " + mp.Describe());
                Shot(cam, "24_开局换牌");

                // ---- 17b. **A276**：换牌按钮那一层的 z 口径（**不许跟卡的世界 z 走**）----
                // 现场：`Battle/MulliganPanel.cs` 原来是 `c.transform.position + (0, −h, Z − c…z)` ——
                //   那个 `− c.transform.position.z` 项**自相消**（`+ c…z` 与 `− c…z` 恰好抵消）⇒
                //   这一处**改不改都一样**（恒等于 `Z`）。但它**形状上**很容易被后人「顺手化简」成
                //   `c.transform.position`（那一行里 x/y 本来就是拿卡的世界坐标当局部坐标用的）——
                //   那样按钮的 z 就会跟着卡走。现在 z 由本面板的 `Z` 单独给（与同父的 `_shade` 同口径）。
                // ⚖️ 调度台裁定 = 收口径（选项 (a)；判据全文 → `资料/普查产出_1010/V4b_三件口径.md` §Q2）；同一处
                //   两套口径并存（这一处的「另一套」= 卡的世界坐标）⇒ 收成面板的局部 z。
                // 判据（两态）：把第 1 张手牌的**世界 z** 挪 0.4 再重建按钮 ⇒ 局部 z **一动不动**。
                // 改坏法：把 z 写成 `c.transform.position.z`（或整个 `c.transform.position`）⇒ 下面这条红。
                {
                    var hv0 = drv.HandViewAt(0);
                    if (hv0 == null) Check(false, "（A276 前提）取得到第 1 张手牌的视图");
                    else
                    {
                        var handForZ = new List<CardView>();
                        for (int i = 0; i < drv.HandCount; i++)
                        { var v = drv.HandViewAt(i); if (v != null) handForZ.Add(v); }
                        mp.Open(handForZ);
                        float zc0 = mp.CardButtonLocalZ(0);
                        var cardP0 = hv0.transform.position;
                        hv0.transform.position = new Vector3(cardP0.x, cardP0.y, cardP0.z + 0.4f);
                        mp.Open(handForZ);                       // 用**同一份**手牌重建，只让卡的 z 不同
                        float zc1 = mp.CardButtonLocalZ(0);
                        hv0.transform.position = cardP0;         // 还原卡的位置…
                        mp.Open(handForZ);                       // …并把按钮重建回原位那一版
                        Check(!float.IsNaN(zc0) && Mathf.Abs(zc0 - zc1) < 1e-5f,
                              $"★ 「换」按钮那一层的 z 是**面板的局部 z**、不跟卡的世界 z 走 —— "
                            + $"卡 z {cardP0.z:F2} 时按钮 z = {zc0:F4}；卡挪到 {cardP0.z + 0.4f:F2} 时 = {zc1:F4}");
                    }
                }

                // 点「换」→ 标记；再点一次 → 取消（走的是面板的命中判定，不是直接改标记）
                Check(drv.SimulateMulliganToggle(0) && mp.IsMarked(0), "点第 1 张牌的「换」→ 标记上了");
                Check(mp.Marked.Count == 1, "……而且只标记了 1 张");
                Check(mp.CardBtnTex(0) == "UI_Button_Mulligan_Pressed",
                      $"……按钮换成按下态那张图（{mp.CardBtnTex(0)}）");
                Check(drv.SimulateMulliganToggle(0) && !mp.IsMarked(0), "再点一次 → 取消标记");
                Check(drv.SimulateMulliganToggle(0) && drv.SimulateMulliganToggle(2) && mp.Marked.Count == 2,
                      "标记第 1、3 张（`Marked` 应当是升序的 [0,2]）");
                Shot(cam, "25_换牌标记");

                // 🔴 「眼睛」那颗钮（原版 `HideMulliganButton` → `MulliganManager.ToggleMulliganVisibility`
                //    → `ShowMulliganElements`）。**2026-09-17 照反编译核实**：它是**开关**（按 activeInHierarchy
                //    取反），且一次收**四样** —— 两个 GameObject + 每张卡的换牌按钮 + **压暗层**
                //    （`Shade.SwitchShade`）。我们原来只收按钮、压暗还盖着 —— 那是**漏做**：玩家按眼睛
                //    就是为了看战场，原版那时屏幕是亮的。
                SettleShake();      // 点「眼睛」是**按世界坐标点的**，HUD 偏了就会点空（见 `SettleShake`）
                Check(drv.SimulateMulliganEye() && !mp.CardButtonsShown && !mp.ShadeActive,
                      "点「眼睛」→ 换牌按钮**和压暗层**一起收起（原版 `ShowMulliganElements(false)`）");
                // ⚠️ 图要拍在**两次点击之间** —— 拍在后面那一次之后，画面是「又都回来了」，
                //    和文件名对不上（第一次写的时候就是这么错的，图上按钮还在）。
                Shot(cam, "25b_换牌收起");
                Check(drv.SimulateMulliganEye() && mp.CardButtonsShown && mp.ShadeActive,
                      "再点一次 → 按钮和压暗都回来（**是开关，不是按住**）");

                string sigBefore = Sig(drv.Ctx);
                int handBefore = drv.Ctx.Players[0].Hand.Count;
                Check(drv.SimulateMulliganDone(), "点「完成换牌」");

                Check(!drv.InMulligan && !mp.Visible, "……换牌阶段结束、面板收起");
                Check(drv.Ctx.Players[0].Energy == 2, $"……这时才发能量（{drv.Ctx.Players[0].Energy} = 回合 1）");
                // ⚠️ 第三十四轮：换牌结束后 `BeginTurn` 里的**天赋**也会塞一张进来 ⇒ 单算。
                Check(drv.Ctx.Players[0].Hand.Count == handBefore + 1 + Conjured(drv.Ctx, 0),
                      $"……也才抽第 1 张（手牌 {handBefore} → {drv.Ctx.Players[0].Hand.Count}，"
                      + $"其中天赋生成的 {Conjured(drv.Ctx, 0)} 张）");
                // 换牌 + 抽牌的净效果：牌**一张不多一张不少**（换掉的回牌库、补抽回来）
                Check(Sig(drv.Ctx) == sigBefore, "牌一张不多一张不少（换掉 2 张 → 回牌库重洗 → 补抽 2 张）");
                Debug.Log(P + $"   换完手牌：{string.Join("/", HandNames(drv.Ctx, 0))}");
                Shot(cam, "26_换牌之后");

                // ---- 换牌倒计时（原版 `BattleManager._MulliganCountdown`）------------------
                // 原版：逐秒 -1 → **< 10 s** 把剩余秒数写到「完成换牌」那颗钮上（`SetMulliganTimer`）
                //       → **< 1 s** 自动完成（`ProcessMulliganDone` = 等价玩家点完成）。
                // 总秒数字段 = `VarsGlobal.mulliganTimeLimit`（**值拿不到，默认是我们挑的**，见字段注释）。
                // 上一段已经把换牌走完了 ⇒ 重开一局来验。
                drv.Begin("Ultramarines", "Goff", 20260919);
                Check(drv.InMulligan && mp.Visible, "重开一局 → 又进换牌阶段（下面验倒计时）");
                Check(Mathf.Abs(drv.MulliganSecondsLeft - drv.mulliganSeconds) < 0.01f,
                      $"倒计时从总秒数起（{drv.MulliganSecondsLeft:F1} = mulliganSeconds {drv.mulliganSeconds:F1}）");
                Check(mp.DoneText == MulliganPanel.DoneLabel,
                      $"开局时按钮上写的是「{MulliganPanel.DoneLabel}」（还没进最后 10 秒）");
                int toGo = Mathf.CeilToInt(drv.mulliganSeconds) - 9;
                for (int i = 0; i < toGo; i++) drv.TickMulliganForTest(1f);
                Check(mp.DoneText == "0:09", $"剩 9 s 时按钮上写着「0:09」（实得「{mp.DoneText}」）");
                Check(drv.InMulligan, "……这时**还没**自动完成（原版只在 <1 s 才自动完成）");
                Shot(cam, "25c_换牌倒计时");
                for (int i = 0; i < 12 && drv.InMulligan; i++) drv.TickMulliganForTest(1f);
                Check(!drv.InMulligan, "走到 0 → **自动完成换牌**（= 玩家点「完成换牌」同一条路）");
                Check(mp.DoneText == MulliganPanel.DoneLabel,
                      $"……收尾时按钮的字复原成「{MulliganPanel.DoneLabel}」（实得「{mp.DoneText}」）");

                // 关掉开关 → 回到默认路径（自检里那十几节用的就是这条）
                drv.mulliganEnabled = false;
                drv.Begin("Ultramarines", "Goff", 20260918);
                Check(!drv.InMulligan && drv.Ctx.Players[0].Energy == 2,
                      "不开换牌 → Begin 之后直接就是回合 1（老路径不变）");
                drv.mulliganEnabled = saved;
                drv.offensivePhaseEnabled = savedOff;
            }
        }


        // ---- 20. 选牌面板（原版 `ChooseCardMenu`）：引擎不再替玩家挑 ----
        //   引擎那半在 `RuleEngineTest.TestPlayerChoice`（含「不同下标 → 不同卡」那条硬判据）；
        //   这一节验的是**表现层那半**：面板真的弹出来、点得动、选完关得掉。
        //   ⚠️ 必须走 `SimulatePlayViaPanel` —— **不是** `SimulatePlay`（那个直接调引擎、绕过面板）。
        Debug.Log(P + "--- 选牌面板 ---");
        {
            var pool = CardDatabase.Load();
            var rd = CardDatabase.Find(pool, "Rapid Deployment");   // `Choose a troop in your hand. Lower its cost by 2`
            Check(rd != null, "卡池里有 `Rapid Deployment`（`Choose a troop in your hand`）");
            var panel = driver != null ? driver.Choose : null;
            Check(panel != null, "选牌面板建出来了");
            Check(panel != null && !panel.Visible, "平时是关着的");

            if (rd != null && panel != null)
            {
                ctx = driver.Ctx;                       // 上面那节重建过对局，这里重新取一次
                ctx.Players[0].Hand.Add(ctx.NewInstance(rd));
                ctx.Players[0].Energy = 9;              // 保证付得起（费 3）
                driver.RefreshAll();
                Step(0.05f);

                int idx = -1;
                for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
                    if (ReferenceEquals(ctx.Players[0].Hand[i].Card, rd)) { idx = i; break; }   // 第 7 行第 3 步：手牌存实例
                Check(idx >= 0, "注入的那张牌在手牌里");

                Check(driver.SimulatePlayViaPanel(idx, SimpleAI.FirstFreeSlot(ctx.Players[0])),
                      "走**面板那条路**出牌（`BeginPlay` 返回成功）");
                Step(0.05f);

                Check(panel.Visible, "★ **面板真的弹出来了**（不用面板的话这一步直接就打出去了）");
                Check(driver.ChooseOptionCount >= 2,
                      $"★ 上面摆着 **{driver.ChooseOptionCount}** 张候选（应 ≥2 —— 手牌里的部队）");
                Check(panel.TitleText == "选择一张牌",
                      $"★ 标题「{panel.TitleText}」= 运行时实测那句"
                      + "（**不是**静态兜底的英文 `Choose one card` —— 铁律 4）");
                Check(panel.ConfirmText == "继续", $"★ 确认文案「{panel.ConfirmText}」");
                Check(panel.BarHasArt && panel.PlayHasArt && panel.EyeHasArt,
                      "★ 底条 / 圆钮 / 眼睛三张图都在"
                      + $"（`{panel.BarTex}` · `40k_UI_bt_play` · `{panel.EyeTex}`）");
                Shot(cam, "20_选牌面板");

                Check(driver.SimulateChoosePick(0), "点第 1 张候选（走面板的命中判定，真路径）");
                Check(driver.SimulateChooseDone(), "点「继续」");
                Check(!panel.Visible, "★ 选完**面板关掉了**");
                Check(ctx.LastChosenCard != null, "★ 引擎真的按面板选了一张（`LastChosenCard` 非空）");

                // 收尾：收尾：这一节是全自检**最后**跑的一节 —— 播出来的特效要清掉再交棒
                // （批处理下没有帧循环，`WarpforgeEffectPlayer` 不会自毁，见 `ClearEffects`）
                ClearEffects();
                Step(0.2f);
            }
        }

        // ---- 21. 🆕 2026-09-14：三选一 / 选效果 / 变身 —— **同一个面板**，候选不是卡池里的卡 ----
        //   为什么是同一个面板：原版 `ChooseCardMenu` 全树**只有两个调用点**（`ChoiceOfCardPlayer`
        //   与 `_SetupEnviromentalEffectPhase`），**没有第三个** —— 「choose one」走的也是它
        //   （`CardScript.NeedsToChooseFromPool()` → `BattleManager.ChooseCardMethod`
        //     → `ChoiceOfCardPlayer` → `ChooseCardMenu.Setup`）。
        //   候选在数据侧是 `TargetCriteria.filterSpecificCards`（`List<RawCardScript>`）——**就是真卡**，
        //   而且面板**带 Done 钮**（`chooseButton` / `ClickChooseDone`）。
        //   ⇒ 我们复用同一个面板 + 同一个「选完点继续」；**不是卡**的那几项由
        //     `BattleDriver.MakeChoiceCard` **合成一张卡**（源卡的插图 + 选项文字当效果文字，
        //     `cost = -1` ⇒ 不画费用六边形）。
        Debug.Log(P + "--- 三选一 / 选效果 / 变身面板 ---");
        {
            var pool = CardDatabase.Load();
            var panel = driver != null ? driver.Choose : null;
            Check(panel != null, "面板还是那一个（三族共用，没另开第三份克隆）");

            // ---- 21-a 三选一：`The Fang` = `Choose one: Deploy a Grey Hunter; Heal 4 … or Draw 2 cards` ----
            var fang = CardDatabase.Find(pool, "The Fang");
            Check(fang != null, "卡池里有 `The Fang`（`chooseone`，费 2）");
            if (fang != null && panel != null)
            {
                ctx = driver.Ctx;
                ctx.Players[0].Hand.Add(ctx.NewInstance(fang));
                ctx.Players[0].Energy = 9;
                driver.RefreshAll();
                Step(0.05f);

                int idx = -1;
                for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
                    if (ReferenceEquals(ctx.Players[0].Hand[i].Card, fang)) { idx = i; break; }
                Check(idx >= 0, "注入的那张牌在手牌里");

                int before = UnitsOnBoard(ctx, 0);
                Check(driver.SimulatePlayViaPanel(idx, SimpleAI.FirstFreeSlot(ctx.Players[0])),
                      "走**面板那条路**打出 `The Fang`");
                Step(0.05f);

                Check(panel.Visible, "★ **三选一面板弹出来了**（不用面板的话这一步直接就打出去了）");
                Check(panel.TitleText == ChoosePanel.DefaultTitle,
                      $"★ 标题「{panel.TitleText}」= 原版那唯一一个（原版只有一个 `ChooseText` 对象，"
                      + "运行时**换词条**而不是换标题；原来这里断言的是我们自造的「选择一项」，2026-09-18 删）");
                Check(driver.ChooseOptionCount == 3,
                      $"★ 上面摆着 **{driver.ChooseOptionCount}** 张候选（卡面就是三项）");
                Check(driver.ChooseOptionName(0) == "Deploy a Grey Hunter",
                      $"★ 第 1 张候选的卡名「{driver.ChooseOptionName(0)}」= 卡面原文"
                      + "（**不是**小写的 `deploy a grey hunter` —— 解析层 `TryChooseOne` 存的是原文大小写）");
                Check(driver.ChooseOptionName(2) == "Draw 2 cards",
                      $"★ 第 3 张候选「{driver.ChooseOptionName(2)}」");
                Shot(cam, "21a_三选一面板");

                Check(driver.SimulateChoosePick(0), "点第 1 张候选");
                Check(driver.SimulateChooseDone(), "点「继续」");
                Check(!panel.Visible, "★ 选完**面板关掉了**");
                Check(UnitsOnBoard(ctx, 0) > before,
                      $"★ 选第 1 项真的**部署了 `Grey Hunter`**（场上 {before} → {UnitsOnBoard(ctx, 0)} 个单位）");
                ClearEffects();
                Step(0.15f);

            }

            // ---- 21-b 选效果（池子是**真卡**）：`Exemplary Warrior` 的三项就是三张真卡 ----
            var ew = CardDatabase.Find(pool, "Exemplary Warrior");
            Check(ew != null, "卡池里有 `Exemplary Warrior`（`chooseeffect`，费 2）");
            if (ew != null && panel != null)
            {
                ctx = driver.Ctx;
                ctx.Players[0].Hand.Add(ctx.NewInstance(ew));
                ctx.Players[0].Energy = 9;
                driver.RefreshAll();
                Step(0.05f);

                int idx = -1;
                for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
                    if (ReferenceEquals(ctx.Players[0].Hand[i].Card, ew)) { idx = i; break; }

                Check(driver.SimulatePlayViaPanel(idx, SimpleAI.FirstFreeSlot(ctx.Players[0])),
                      "走面板那条路打出 `Exemplary Warrior`");
                Step(0.05f);

                Check(panel.Visible, "★ **选效果面板弹出来了**");
                Check(panel.TitleText == ChoosePanel.DefaultTitle,
                      $"★ 标题「{panel.TitleText}」= 同上（原版**不分叉标题**）");
                Check(driver.ChooseOptionCount == 3, $"★ 摆着 {driver.ChooseOptionCount} 张候选");
                Check(driver.ChooseOptionId(0) == "Righteous Fury"
                      && driver.ChooseOptionId(2) == "Paragon of Ultramar",
                      $"★ 三项都是**真卡**（`{driver.ChooseOptionId(0)}` / "
                      + $"`{driver.ChooseOptionId(1)}` / `{driver.ChooseOptionId(2)}`；卡面标题是中文："
                      + $"`{driver.ChooseOptionName(0)}`）"
                      + " —— 池子里的条目本来就是卡名（`ChooseEffectPools`）");
                Shot(cam, "21b_选效果面板");

                Check(driver.SimulateChoosePick(0), "点第 1 张候选（`Righteous Fury`）");
                Check(driver.SimulateChooseDone(), "点「继续」");
                Check(!panel.Visible, "★ 选完**面板关掉了**");
                ClearEffects();
                Step(0.15f);
            }

            // ---- 21-c 选效果（池子是**载荷**）：合成卡 —— 源卡的插图 + 选项文字 ----
            var ha = CardDatabase.Find(pool, "Hyper-adaptation");
            Check(ha != null, "卡池里有 `Hyper-adaptation`（`Choose an effect and give it to a friendly troop`）");
            if (ha != null && panel != null)
            {
                ctx = driver.Ctx;
                ctx.Players[0].Hand.Add(ctx.NewInstance(ha));
                ctx.Players[0].Energy = 9;
                driver.RefreshAll();
                Step(0.05f);

                int idx = -1;
                for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
                    if (ReferenceEquals(ctx.Players[0].Hand[i].Card, ha)) { idx = i; break; }

                Check(driver.SimulatePlayViaPanel(idx, SimpleAI.FirstFreeSlot(ctx.Players[0])),
                      "走面板那条路打出 `Hyper-adaptation`");
                Step(0.05f);

                Check(panel.Visible, "★ 面板弹出来了");
                Check(driver.ChooseOptionCount == 3, $"★ 摆着 {driver.ChooseOptionCount} 张候选");
                Check(driver.ChooseOptionName(0) == "+1 Armour"
                      && driver.ChooseOptionName(1) == "+2 Melee Attack",
                      $"★ 三项是**载荷文字**（`{driver.ChooseOptionName(0)}` / "
                      + $"`{driver.ChooseOptionName(1)}` / `{driver.ChooseOptionName(2)}`）"
                      + " —— 合成卡，插图用**这张卡自己的**（用户 2026-09-13 的界面线索）");
                Shot(cam, "21c_选效果_合成卡");

                Check(driver.SimulateChoosePick(0), "点第 1 张候选");
                Check(driver.SimulateChooseDone(), "点「继续」");
                Check(!panel.Visible, "★ 选完**面板关掉了**");
                ClearEffects();
                Step(0.15f);
            }

            // ---- 21-d 变身：`Hrolf the Ironhowl` 的 `become A or B` ----
            // 🔴 **2026-09-14 用户裁决翻了这条的口径**：卡面 `Stratagems in your hand become a
            //    Hunting Wolf or Fenrisian Wolf` **没有 `choose` 字样** ⇒ 按规则书英文版 `:475`
            //    的反面（只有写了 `choose` 才轮到玩家），**引擎随机挑、不开面板**。
            //    ⇒ 这条从「**面板弹出来、摆两只狼**」翻面成「**面板不弹、手牌里的战略卡直接变成一只**」。
            var hrolf = CardDatabase.Find(pool, "Hrolf the Ironhowl");
            Check(hrolf != null, "卡池里有 `Hrolf the Ironhowl`");
            if (hrolf != null && panel != null)
            {
                ctx = driver.Ctx;
                // 手牌里先塞一张**战略卡**（`become` 换的就是手牌里的战略卡）
                var strat = CardDatabase.Find(pool, "Fenrisian Wolfpack");
                Check(strat != null, "卡池里有 `Fenrisian Wolfpack`（拿它当手牌里的战略卡）");
                if (strat != null) ctx.Players[0].Hand.Add(ctx.NewInstance(strat));
                ctx.Players[0].Hand.Add(ctx.NewInstance(hrolf));
                ctx.Players[0].Energy = 9;
                driver.RefreshAll();
                Step(0.05f);

                int idx = -1;
                for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
                    if (ReferenceEquals(ctx.Players[0].Hand[i].Card, hrolf)) { idx = i; break; }

                Check(driver.SimulatePlayViaPanel(idx, SimpleAI.FirstFreeSlot(ctx.Players[0])),
                      "走面板那条路部署 `Hrolf the Ironhowl`");
                Step(0.05f);

                Check(!panel.Visible,
                      "★ 变身**不再弹面板** —— 卡面没有 `choose`，按用户口径该**随机**"
                      + "（原来这里钉的是「面板弹出来了」，已推翻）");

                string becomeLog = null;
                foreach (string e in ctx.Events)
                    if (e != null && e.Contains("变身")) becomeLog = e;
                Check(becomeLog != null,
                      "★ 日志里留下了变身那一笔（**不静默**）—— " + (becomeLog ?? "<无>"));

                bool anyWolf = false;
                foreach (var c in ctx.Players[0].Hand)
                    if (c.Card != null && (c.Card.Name == "Hunting Wolf" || c.Card.Name == "Fenrisian Wolf")) anyWolf = true;
                Check(anyWolf, "★ 手牌里的战略卡真的变成了狼（`Fenrisian Wolfpack` 那只不在了）");
                ClearEffects();
                Step(0.15f);
            }
        }

        Application.logMessageReceived -= dwCounter;
        Check(dotween == 0, $"全程没有 DOTween 补间报错（实测 **{dotween}** 条；"
                          + "真包里这一族曾经有 22 条，成因与修法见 `资料/特效还原_进度与交接.md` §七）");

        // ==================================================================
        //  🆕 2026-10-11（A218）：「空节点工厂」那一族的 **`sizeDelta`**
        //
        //  为什么单开一段：A92 把那一族的**类型**补成了 `RectTransform`，但 `sizeDelta` 仍是默认值 ⇒
        //  「空节点 + 原版像素矩形」的**宽高验收不了**（判据原文 = `资料/待办判据_1007.md` §A218 的 ①）。
        //  判据 = **原版那一件自己的 `rect`**（2026-10-11 逐个现读 `bundle_scenes_scenes_battlearena1`，
        //  出处逐条写在断言里），⛔ 不读被测实现、也不拿我们的常量当期望值。
        //  验收口径 = `rect.width/height` == 原版那对 px（经**唯一那一份**换算 `LayoutSpace.Px`）。
        //  ⚠️ 这一段在 `logMessageReceived` 摘掉**之后** —— 探针建/毁不掺进上面那条 DOTween 计数。
        Debug.Log(P + "--- §A218 空节点工厂的 `sizeDelta`（`rect` 的宽高 = 原版矩形）---");
        {
            // 一具「断这个节点的 `rect` = 原版那对 px」的小尺子（只本段用）。
            // 🔴 断的是 **`rect`** 而不是 `sizeDelta` —— 那正是本件的验收口径（写 `sizeDelta` 只是手段）。
            // 🔴 **父链缩放核查**：`rect` 与「设计 px」同量纲**只在父链 `lossyScale == 1` 时成立** ⇒ 每条都先断这条前置
            //    （本工程今天全部成立：`WindowsManager.AttachToAnchor` 把窗根写成 `localScale = one`；战斗侧这些件挂在
            //     `HudRoot` 下、链上无缩放；⛔ 非 1 的档不能直接比设计 px，要按 `MenuDraw.SetPxSize` 的注释单独算）。
            System.Action<Transform, string, float, float, string> box = (t, what, wPx, hPx, src) =>
            {
                if (t == null) { Check(false, $"（前提）{what} 没建出来"); return; }
                var rt = t.GetComponent<RectTransform>();
                Check(rt != null, $"（前提）{what} 是 `RectTransform`（A92 那半）");
                if (rt == null) return;
                var ls = t.lossyScale;
                Check(Mathf.Abs(ls.x - 1f) < 1e-3f && Mathf.Abs(ls.y - 1f) < 1e-3f,
                      $"（前提·父链缩放）{what} 的 `lossyScale` = ({ls.x:F3},{ls.y:F3})，应为 1（= 本件验收口径成立的那一档）");
                Check(rt.anchorMin == rt.anchorMax && rt.pivot == new Vector2(0.5f, 0.5f),
                      $"（前提）{what} 的锚点重合、pivot 居中（`rect` 只由 `sizeDelta` 决定的前提）");
                Check(Mathf.Abs(rt.rect.width - LayoutSpace.Px(wPx)) < 0.01f
                      && Mathf.Abs(rt.rect.height - LayoutSpace.Px(hPx)) < 0.01f,
                      $"★ {what} 的 `rect` = **{rt.rect.width * EndPanel.PxPerUnit:F1} × {rt.rect.height * EndPanel.PxPerUnit:F1} px**"
                      + $"（原版 {wPx:F2}×{hPx:F2} —— {src}）");
            };

            var a218root = new GameObject("A218ProbeRoot");
            // ① 整屏那几扇：原版 `anchor (0,0)-(1,1)` + `sizeDelta (0,0)` ⇒ 绝对矩形 **(0,0)-(1920,1080)**
            var a218End = EndPanel.Create(a218root.transform);
            box(a218End.transform, "结算面板根 `EndPanel`", 1920f, 1080f, "原版 `EndBattlePanel`（stretch）");
            box(a218End.transform.Find("content"), "结算内容层 `content`", 1920f, 1080f,
                "⚠️ **本层是我们自己的**（原版 `EndBattlePanel` 下没有这一级）⇒ 取它真正占的那块 = 屏矩形");
            box(a218End.transform.Find("content/SkullsHolder"), "骷髅行 `SkullsHolder`", 648.1f, 52.4f,
                "原版 `SkullsHolder` 的 `m_SizeDelta (648.1, 52.38)`（末位 0.02px 取本文件一直在用的 52.4）");
            box(MulliganPanel.Create(a218root.transform).transform, "换牌面板根 `MulliganPanel`", 1920f, 1080f,
                "原版 `Mulligan`（stretch ⇒ 整屏）");
            box(CardDisplayWindow.Create(a218root.transform).transform, "卡牌展示窗根", 1920f, 1080f,
                "原版 `Card Display Window`（stretch ⇒ 整屏）");
            box(UnitChatPanel.Create(a218root.transform).transform, "单位语音条根", 1920f, 1080f,
                "原版 `Unit Chat`（stretch ⇒ 整屏）");
            box(ChoosePanel.Create(a218root.transform).transform, "选牌面板根", 1920f, 1080f,
                "原版 `ChooseCardMenu`（stretch ⇒ 整屏）");
            var a218Log = BattleLogPanel.Create(a218root.transform);
            box(a218Log.transform, "日志面板根", 1920f, 1080f,
                "⚠️ **本层是我们自己的**（原版那一级是 `Safe area BackCanvas/LeftArea`，实读 RT · `sizeDelta (0,0)` ⇒ 铺满）");
            // ⚠️ `CemeteryLogPanel` 是**根节点的【兄弟】**不是子件（`Build(root)` 里 `SetParent(root)` ——
            //    `root` 是**传进来的那个**参数，`BattleLogPanel` 那层只收 shade 之外的东西）⇒ 从 `a218root` 往下找。
            box(a218root.transform.Find("CemeteryLogPanel"), "日志面板 `CemeteryLogPanel`", 794.1f, 653.7998f,
                "原版 `CemeteryLogPanel`：`m_SizeDelta (794.06897, **0**)` + 竖直 stretch ⇒ 高 = 0.60537014×1080");
            // ② 非整屏那几扇（原版那对数值逐条现读）
            box(MultiCardDisplay.Create(a218root.transform).transform, "多卡展示窗根", 1920f, 818f,
                "原版 `Generic Multi Card Display Combat`（横向 stretch + `sizeDelta (0, 818.04)`）");
            box(SettingsPanel.Create(a218root.transform, null, null).transform, "对局内设置面板根", 743.2f, 758.6f,
                "原版 `BattleSettingsPanel` 的 `m_SizeDelta (743.202, 758.6345)`");
            box(ChatPopupPanel.Create(a218root.transform).transform, "聊天弹幕根 `ChatPopup`", 815.04f, 475.47f,
                "原版 `ChatPopup` 的 `m_SizeDelta (815.044, 475.470)`");
            var a218Replay = ReplayBar.Create(a218root.transform);
            box(a218Replay.transform, "回放条根", 293.6f, 57.41f,
                "原版 `ReplayButtons` 的 `m_SizeDelta (293.6, 57.406)`");
            box(a218Replay.transform.Find("Holder"), "回放条 `Holder`", 258.54f, 59.43f,
                "原版 `Holder`（= 字段 `objHolder`）绝对 rect `x[416.3,674.8] y[36.3,95.7]`");
            var a218Doors = BattleDoors.Create(a218root.transform);
            box(a218Doors.transform, "开门视频根 `BattleDoors`", 1920.1199f, 1118.981f,
                "原版 `BattleDoors` 的 `m_SizeDelta`（它自己 `localScale 1.0665` ⇒ 视觉 2048×1193，缩放**不复刻**）");
            box(a218Doors.transform.Find("video_player"), "视频组件宿主 `video_player`", 1920f, 1080f,
                "原版 `Video Image`（`VideoPlayer` 组件的宿主就是它）");
            // ③ 等待提示（原版 `WaitText` 与它下面那两级，矩形**各不相同**）
            var a218Wait = WaitBanner.CreateWithArt(a218root.transform, CardArt.DeckUi("40k_popup"),
                                                    CardArt.Ui("40k_popup_texture"));
            box(a218Wait.transform, "等待提示根（原版 `WaitText`）", 1344f, 79.44f,
                "原版 `WaitText` 的 `m_SizeDelta (1344.0, 79.44)`");
            box(a218Wait.transform.Find("wait_popup/wait_fillRoot"), "等待提示填充层根", 1323f, 90f,
                "原版 `Mask`（stretch ⇒ 矩形 = 父件 `Generic Popup Background` 的 1323×90）");
            // ④ 滑块根：战斗那三根原版实读 **561.08 × 12.00**（见 `WfSlider` 文件头那段）
            WfSlider.Create(a218root.transform, "A218Probe", Vector3.zero, 0.5f, null,
                            queue: 3000, handlePx: 34.406f, handleOffset: WfSlider.HandleOffsetPx, capScale: 1f);
            box(a218root.transform.Find("slider_A218Probe"), "音量滑块根", 561.08f, 12f,
                "原版战斗那三根 `Slider`：`Container sizeDelta (0,100)` + 锚 y `0.33→0.45` ⇒ 高 12 · 宽 561.08");

            // ------------------------------------------------------------------
            //  🔴 **反向那一半**：原版**本来就是裸 `Transform`** 的几处**不许被顺手补齐**
            //     （弱断言分不出两态 ⇒ 必须成对断：上面那批**有** `RectTransform`、这几处**没有**）
            // ------------------------------------------------------------------
            if (driver != null && driver.hudRoot != null)
            {
                Check(driver.hudRoot.GetComponent<RectTransform>() == null,
                      "🔴 `HudRoot` **没有** `RectTransform`（判据 = 原版 `BattleHud` 本尊就是裸 `Transform`"
                      + " —— `bundle_scenes_scenes_battlearena1` 实读 go_pid 239；它的**孙辈**才是 uGUI 的 `Canvas`）");
                box(driver.hudRoot.Find("OvertimeSplashText"), "加时 splash 根", 1920f, 1080f,
                    "原版 `OvertimeSplashText`（stretch + `sizeDelta (0,0)` ⇒ 整屏）");
            }
            else Check(false, "（前提）拿得到 `driver.hudRoot`（`HudRoot` / 加时 splash 那两条要靠它）");

            // 这几处**原版就是裸 `Transform`**（判据逐条写在 `Battle/*.cs` 的注释里），但它们的创建是**惰性**的
            // ⇒ 拿不到就**出声跳过**（不是静默门）：`Particle colliders` 只在第一次粒子碰撞时要、`Slots` 只在棋盘铺标记时建。
            var a218Pc = driver != null ? driver.transform.Find("Particle colliders") : null;
            if (a218Pc != null)
                Check(a218Pc.GetComponent<RectTransform>() == null && a218Pc.childCount > 0
                      && a218Pc.GetChild(0).GetComponent<RectTransform>() == null,
                      "🔴 粒子碰撞平面 `Particle colliders` 及它的 `Generic Target` 子件**都没有** `RectTransform`"
                      + "（判据 = 原版 go_pid 575 / 138 实读就是裸 `Transform`）");
            else Debug.Log(P + "   （A218：`Particle colliders` 这一轮**没建出来**（惰性）⇒ 那两条不判；"
                             + "判据在 `Battle/BattleDriver.cs` 的注释里）");
            var a218Slots = driver != null && driver.playerBoard != null
                          ? driver.playerBoard.transform.Find("Slots") : null;
            if (a218Slots != null)
                Check(a218Slots.GetComponent<RectTransform>() == null,
                      "🔴 落点标记根 `Slots` **没有** `RectTransform`（判据 = 原版 `Board Center` / `MinionArea` /"
                      + " `HandArea` 那一族实读全是裸 `Transform`）");
            else Debug.Log(P + "   （A218：`Slots` 这一轮**没建出来**（惰性）⇒ 那一条不判；判据在 `Board/BoardLayout.cs` 的注释里）");
            var a218Line = driver != null && driver.reticle != null
                         ? driver.reticle.transform.Find("CrosshairLine") : null;
            if (a218Line != null)
                Check(a218Line.GetComponent<RectTransform>() == null,
                      "🔴 准星弧线 `CrosshairLine` **没有** `RectTransform`（判据 = 原版 `CrosshairLine 3D`"
                      + " go_pid 848 实读裸 `Transform`，同一批 7 个 `LineRenderer` 宿主全是）");
            else Debug.Log(P + "   （A218：`CrosshairLine` 这一轮**没建出来** ⇒ 那一条不判；判据在 `Battle/TargetReticle.cs` 的注释里）");

            Object.DestroyImmediate(a218root);      // 批处理下 `Destroy` 不生效
        }

        // ==================================================================
        //  🆕 2026-09-26（N4）：**联机客机的视图方向**（`_me = 1`）
        //
        //  为什么单开一段：联机对局两端跑的是**同一套绝对座位**（主机 0 / 客机 1），
        //  客机那边靠 `SetMySeat(1)` 把视图翻过来 —— 而这一路**批处理里只能这样验**
        //  （同一工程不能同时跑两个 Unity 实例，两台机器真连一次是「真 Play」的事）。
        //  验的是：**自己那侧的东西必须画在屏幕下半**（手牌 / 上场单位），对面那侧在上半。
        // ==================================================================
        // ⚠️ **A195 核过：这个门是安全的**（没改）—— 它查的是 `Run` 里那两个**局部**（不是 `FindObjectOfType` 重查）：
        //    `driver` 由 `BuildScene` 直接给出、紧接着就被解引用（`driver.selector` / `driver.Begin`）；
        //    `ctx` 取自 `driver.Ctx`，之后每次重取（§20/§21 那几处）**紧接着**就是 `ctx.Players[0]…`
        //    ⇒ 任一为 null 的话**前面早就 NRE 了**（大声，不是静默），根本走不到这一行。
        if (driver != null && ctx != null)
        {
            // 🔴 **联机局：对面那一侧不许跑 AI**（否则 AI 和网络会给对面同时出招 ⇒ 立刻打岔）。
            //    挂上联机层再问那个判据 —— 单机那一路必须原样不变。
            Debug.Log(P + "--- 联机：对面那一侧该由网络驱动，不许跑 AI ---");
            Check(driver.AiShouldDriveOpponent, "单机：**AI 照旧驱动对面**（联机没把单机改坏）");
            var nbProbe = CardPresentation.Net.NetBattle.Attach((CardPresentation.Net.INetBattleHost)driver,
                                                                null, isHost: true);
            driver.AttachNet(nbProbe);
            Check(!driver.AiShouldDriveOpponent,
                  "★ 挂上联机层之后：**AI 不再驱动对面那一侧**（对面由 `NetTick()` 落地网络动作）");
            driver.AttachNet(null);
            Check(driver.AiShouldDriveOpponent, "拆掉联机层 ⇒ 又回到单机那一路（幂等，不留后遗症）");

            Debug.Log(P + "--- 联机客机视图（`_me = 1`）：自己那侧该在屏幕下半 ---");
            driver.SetMySeat(1);
            Check(driver.MyIndex == 1, "`SetMySeat(1)` ⇒ `MyIndex = 1`（引擎那边一切照旧，只是视图翻过来）");
            // 引擎侧不变：`Ctx.Players[1]` 就是「我」那一方
            Check(driver.Ctx == ctx, "换座位**不动引擎**（同一个 `Ctx`，只是「哪一侧画在下面」变了）");
            driver.RefreshAll();
            var myHand1 = driver.HandViewAt(0);
            var foeHand1 = driver.FoeHandViewAt(0);
            if (myHand1 != null && foeHand1 != null)
            {
                Check(myHand1.transform.position.y < 0f,
                      $"客机视角：**我的手牌在下半屏**（y={myHand1.transform.position.y:F2} < 0）");
                Check(foeHand1.transform.position.y > 0f,
                      $"客机视角：**对面手牌在上半屏**（y={foeHand1.transform.position.y:F2} > 0）");
            }
            else Check(false, "客机视角：两边的手牌视图都在（拿到 `HandViewAt(0)` / `FoeHandViewAt(0)`）");

            // 场上单位同理：两边各摆一个，看谁在下半屏。
            // ⚠️ **单位的位置是「竞技场世界坐标」**（3D 落点，`ArenaSlots.RootPosition`），
            //    **不是 UI 的屏幕坐标** ⇒ 不能假设「y < 0 就是下面」。改成 **A/B 比**：
            //    先记下座位 0 那副占的是哪一行，再把座位翻成 1，看两副牌是不是**换了行**。
            int mineSlot = -1, foeSlot = -1;
            var card1 = FirstUnit(driver.Ctx, 1);
            var card0 = FirstUnit(driver.Ctx, 0);
            if (driver.Ctx.IsOver || card1 == null || card0 == null)
            {
                // 如实说「这一段没验」，不装作验过（本项目红线：不许静默）
                Debug.LogWarning(P + "  ⚠️ **场上单位那一层没验**（这一局已经打完 / 手牌里没有单位卡）—— "
                                 + "「客机视角下 `MyUnits` 认的是不是座位 1 那一方」留给"
                                 + "**两台机器真连一次**那次（`资料/真Play待验清单.md`）");
            }
            else if (RuleCore.DeployFree(driver.Ctx, 1, card1, out mineSlot)
                  && RuleCore.DeployFree(driver.Ctx, 0, card0, out foeSlot))
            {
                // 🔴 **量「映射」而不是量「坐标」**：单位落在哪一行是竞技场 3D 的事
                //    （`ArenaSlots.RootPosition` 是**竞技场世界坐标**，不是屏幕坐标，别拿 y 的正负判上下）。
                //    这里要钉的是：**`MyUnits` 认的必须是 `_me` 那一方的棋子、`FoeUnits` 认另一方**。
                driver.SetMySeat(1); driver.RefreshAll();
                var vm = Lookup(driver.MyUnits, mineSlot);
                var vf = Lookup(driver.FoeUnits, foeSlot);
                CardView vm0 = null, vf0 = null;
                if (vm != null && vf != null)
                {
                    Check(vm.Data.id == card1.Name,
                          $"★ 客机视角（`_me = 1`）：`MyUnits` 里那个**是座位 1 的「{card1.Name}」**（本机的棋子）");
                    Check(vf.Data.id == card0.Name,
                          $"★ 客机视角（`_me = 1`）：`FoeUnits` 里那个**是座位 0 的「{card0.Name}」**（对面的棋子）");
                }
                else Check(false, "客机视角：两边的场上视图都建出来了");

                // 反向对照：座位 0 视角下**两个字典要认另一边**
                driver.SetMySeat(0); driver.RefreshAll();
                vm0 = Lookup(driver.MyUnits, foeSlot);
                vf0 = Lookup(driver.FoeUnits, mineSlot);
                if (vm0 != null && vf0 != null)
                {
                    Check(vm0.Data.id == card0.Name,
                          $"★ 主机视角（`_me = 0`）：`MyUnits` 里那个是座位 0 的「{card0.Name}」（同一批棋子，认的方反过来了）");
                    Check(vf0.Data.id == card1.Name,
                          $"★ 主机视角（`_me = 0`）：`FoeUnits` 里那个是座位 1 的「{card1.Name}」");
                }
                else Check(false, "主机视角：两边的场上视图都建出来了");

                // ---- 19b. 客机视角：**其余 HUD 认的是不是 `_me` 那一方** ----
                //  判据：`BattleDriver.UpdateHud` 里 `me = Ctx.Players[_me]` / `foe = Ctx.Players[1 - _me]`
                //  ⇒ 翻座位之后，**能量 / 牌堆 / 名牌**三处的两侧内容要整体对调。
                //  ⚠️ 为了让「对调」看得出来，先把两边能量**改成不一样的**（改完还原 —— 后面几节还要用这个局面）。
                {
                    int e0 = ctx.Players[0].Energy, e1 = ctx.Players[1].Energy;
                    ctx.Players[0].Energy = 3; ctx.Players[1].Energy = 5;
                    //  名牌那一行是 `阵营 + 生命/HP + 数值`（**文案是本地化的** ⇒ 别按 "HP" 匹配）
                    //  ⇒ 要让两边的**数值**不一样才好判方向（一样的话「换没换」看不出来）。
                    int h0 = ctx.Players[0].Warlord.Health, h1 = ctx.Players[1].Warlord.Health;
                    ctx.Players[0].Warlord.Health = 12; ctx.Players[1].Warlord.Health = 27;
                    string WantE(int seat) { return ctx.Players[seat].Energy + "/" + ctx.Players[seat].MaxEnergy; }
                    string WantP(int seat)
                    {
                        return CardText.Phrase("DECK") + " " + ctx.Players[seat].Deck.Count + "  "
                             + CardText.Phrase("DISC") + " " + ctx.Players[seat].Discard.Count;
                    }
                    driver.SetMySeat(0); driver.RefreshAll();
                    Check(driver.MyEnergyText == WantE(0) && driver.FoeEnergyText == WantE(1),
                          $"（对照）主机视角：能量条两侧各认自己的（{driver.MyEnergyText} / {driver.FoeEnergyText}）");
                    driver.SetMySeat(1); driver.RefreshAll();
                    Check(driver.MyEnergyText == WantE(1),
                          $"★ 客机视角：**我方能量 = 座位 1 的**（实得 {driver.MyEnergyText}，应为 {WantE(1)}）");
                    Check(driver.FoeEnergyText == WantE(0),
                          $"★ 客机视角：**对面能量 = 座位 0 的**（实得 {driver.FoeEnergyText}，应为 {WantE(0)}）");
                    Check(driver.MyPileText == WantP(1) && driver.FoePileText == WantP(0),
                          $"★ 客机视角：**牌堆/弃牌计数也跟着翻**（我 {driver.MyPileText} / 敌 {driver.FoePileText}）");
                    // 🔴 **2026-09-28 改**：这块名牌原来印「阵营 + 生命」，靠 12/27 两个血量判方向；
                    //    现在**照原版印名字**（那个节点就叫 `EnemyNameText`）⇒ 名字**不随座位变**
                    //    （我就是我、对手就是对手）⇒ 它**不再能判「翻座位」**（那是它原本的用途要说清的事）。
                    //    座位方向由上面**能量 / 牌堆**两条盯（判据本来就更硬、也更直接）。
                    Check(driver.MyPlateText != null && driver.MyPlateText.StartsWith(ProfileData.PlayerName),
                          $"★ 名牌第一段 = **玩家名**（原版那格是名字节点；实得「{driver.MyPlateText}」）");
                    Check(driver.FoePlateText != null && !driver.FoePlateText.Contains(ProfileData.PlayerName),
                          "★ 对面那块名牌**不印玩家名**（单机局对面没有名字来源 ⇒ **不编**，"
                          + $"只剩阵营；实得「{driver.FoePlateText}」）");
                    // ⚠️ **必须把座位还原成 0** —— 原来这行夹在那两条断言中间，改断言时**差点丢掉**
                    //    （丢了后面那批「同一局面」的用例就全在客机视角下跑，会静默改变语义）。
                    driver.SetMySeat(0); driver.RefreshAll();
                    ctx.Players[0].Energy = e0; ctx.Players[1].Energy = e1;
                    ctx.Players[0].Warlord.Health = h0; ctx.Players[1].Warlord.Health = h1;

                    // ---- 攻击选择器：**同一个槽号在两侧指向不同棋子** ----
                    //  判据：`BattleDriver.OpenCommand(slot)` 首行读的是 `Ctx.Players[_me].Board[slot]`。
                    //  最硬的证法是**拿一个「座位 0 有兵、座位 1 空着」的槽**：
                    //  座位 1 视角下点它**必须点不开**（那边是空的），座位 0 视角下点得开。
                    //  ⚠️ **刚部署的单位是 `Exhausted = true`**（`UnitState` 那条：部署当回合不能动）
                    //     ⇒ 对照那一半会「点不开」而**理由不是座位**，所以先把行动权还给它。
                    if (ctx.Players[0].Board[foeSlot] != null) ctx.Players[0].Board[foeSlot].Exhausted = false;
                    bool foeSlotEmptyForSeat1 = ctx.Players[1].Board[foeSlot] == null;
                    if (foeSlotEmptyForSeat1)
                    {
                        driver.SetMySeat(1); driver.RefreshAll();
                        Check(!driver.SimulateOpenCommand(foeSlot),
                              $"★ 客机视角：点槽 {foeSlot}（**座位 1 那边是空的**）⇒ **弹不出选择器**"
                              + "（`OpenCommand` 读的是 `Players[_me]` ⇒ 对面那个兵不算我的）");
                        driver.SetMySeat(0); driver.RefreshAll();
                        Check(driver.SimulateOpenCommand(foeSlot),
                              $"（对照）主机视角：同一个槽 {foeSlot} 是**我自己的兵**（且已解行动） ⇒ 弹得出选择器");
                        if (driver.SelectorOpen) driver.SimulateCommand(AttackKind.Melee);   // 收起，别留给下一节
                    }
                    else
                        Debug.LogWarning(P + "  ⚠️ **选择器那一层没验**：两个座位在同一个槽上都有兵"
                                         + "（拿不到「一侧空着」的槽）—— 如实说没验，不装作验过");
                }

                driver.SetMySeat(0); driver.RefreshAll();
            }
            else Check(false, "客机视角：能给两边各摆一个单位（`DeployFree`）");

            driver.SetMySeat(0);        // 🔴 **还原**（后面 `CheckSavedScene` 那一节还要用）
            driver.RefreshAll();
        }

        // ---- 20. 本地录像：录了一局 → 量它多大 → 放一遍 → 指纹对得上吗 🆕 2026-09-27 ----
        // 判据全文 → `Battle/ReplayStore.cs` 文件头 · `BattleDriver.RecFinish` / `PlayReplay`。
        // 🔴 用户 2026-09-27 拍板「做，我们需要录像」，并问了两件事：**专门的文件夹**（= `ReplayStore.Dir`）
        //    与 **「留最近 50 局、占多大」** ⇒ 这一节把「一局多大」**量出来**，50 局的占用就是它 ×50。
        Debug.Log(P + "--- 本地录像（录 → 量大小 → 放 → 对指纹）---");
        {
            // 🔴 自检里那一局是**用 `SimulateAiTurn` 走的**（`SimpleAI.PlayTurn` + 回合推进）——
            //    它**现在也走同一条录制路**（`SimpleAI.Executed` 挂在引擎边界上；
            //    回合推进收口在 `EndTurnAndAdvance`）⇒ **自检这一局是完整录下来的**。
            //    ⚠️ 若哪天这里变成「指纹对不上」，先查那两个钩子还在不在（别急着改判据）。
            // 先**真打一局完整的**（两边都由 AI 代走），再拿它量大小 —— 别拿前面那种一两步就结束的局当样本。
            // 🆕 2026-10-12（A381）：这一句只为**下面那条骷髅断言**留一个「不是在断空气」的凭据
            //   （本节这局自己拿了多少颗 —— 0 颗时那条骷髅断言就没在断东西，日志里要说出来）。
            int skullBeforeSampleA381 = DailyData.SkullsCountValue();
            driver.Begin("Ultramarines", "Goff", 20260927);
            if (driver.InMulligan) driver.SimulateMulliganDone();
            int rg = 0;
            while (driver.Ctx != null && !driver.Ctx.IsOver && rg++ < 400) driver.SimulateAiTurn();
            Debug.Log(P + $"   录了一局完整的：{driver.Ctx.Turn} 回合 · 循环 {rg} 次");

            int n = ReplayStore.Count;
            Check(n > 0, $"★ 打过的局**都自动录了**（夹里现在 {n} 份）");
            // 取**动作最多的那一份**来量（最新那份可能是一两步就结束的局，不代表性）
            ReplayRecord rec = null; string newest = null;
            foreach (var f in ReplayStore.List())
            {
                var r = ReplayStore.Load(f);
                if (r == null) continue;
                if (rec == null || r.actions.Count > rec.actions.Count) { rec = r; newest = f; }
            }
            Check(rec != null, $"读得回来（动作最多的那份 `{newest}`）");

            // 🔴🔴 **2026-09-27 更正：要放的是「刚打完的这局」，不是「动作最多的那一份」。**
            //
            // 原来拿「最多的那一份」去放 ⇒ **必红**（2026-09-27 实测：分叉点稳稳在第 6 条）。
            // 而根因**不是回放坏了**，是**那一局根本不可回放**：
            //   录像的契约是「起始条件 + 动作流」 —— 只有**由动作流产生**的局面才复现得出来。
            //   而自检前面那些「靶场」小节会**直接手写引擎状态**，例如
            //   `BattleScene.cs:1398` `cAlt.Players[0].Board[2] = new UnitState(bikes, false)`
            //   （验完议程再在 `:1417` 还原）⇒ 那一步**永远不可能**由动作流复现。
            //   实测就是它：录的那一条 `kind=5 slot=2 alt=agenda` 打的是**手放上去的
            //   `Ravenwing Bikes`**，回放时那一格是空的 ⇒ `UseAlternative` 返回 `ErrNotUnit`
            //   （日志：「第 6 条……被拒：该格没有单位」）⇒ 从此整局错开、终局指纹不等。
            //   ⚠️ 判据是 `rec.traceState` 那两行（★ 新加的局面速写），不是猜的。
            // ⇒ **放本节刚打的那一局**（`Begin` + 换牌 + `SimulateAiTurn`，全程只有动作流）。
            //   它的路径就在 `LastReplayFile` 上（`RecFinish` 落盘时记的）。
            string playable = driver.LastReplayFile;

            // ⚠️ **A195 核过：这个门是安全的**（没改）—— 上面 `Check(rec != null, "读得回来（…）")` 就是这条前提。
            if (rec != null)
            {
                long bytes = new System.IO.FileInfo(System.IO.Path.Combine(ReplayStore.Dir, newest)).Length;
                // 黑匣子关掉之后是多大（= **真打时一局的大小**）——直接把那两份速写清空再序列化一次
                var t1 = rec.traceLogTail; var t2 = rec.traceState;
                rec.traceLogTail = new System.Collections.Generic.List<string>();
                rec.traceState = new System.Collections.Generic.List<string>();
                long plain = System.Text.Encoding.UTF8.GetByteCount(UnityEngine.JsonUtility.ToJson(rec));
                rec.traceLogTail = t1; rec.traceState = t2;
                long per = rec.actions.Count > 0 ? (plain - 500) / rec.actions.Count : 0;
                Debug.Log(P + $"   【量】刚打完那一局：**{rec.actions.Count} 条动作** ——"
                            + $" **开黑匣子 {bytes} 字节（{bytes / 1024f:F1} KB）** ／ **关黑匣子 {plain} 字节（{plain / 1024f:F1} KB）**"
                            + $" ⇒ 每条动作约 {per} 字节（净荷；头部约 500 字节）");
                Debug.Log(P + $"   【算】按 `ReplayStore.MaxKept = {ReplayStore.MaxKept}` 局："
                            + $"**真打（关黑匣子）满仓 ≈ {plain * ReplayStore.MaxKept / 1024f / 1024f:F2} MB**"
                            + $"（= {plain / 1024f:F1} KB × {ReplayStore.MaxKept}；带黑匣子约 {bytes * ReplayStore.MaxKept / 1024f / 1024f:F2} MB）");
                Check(bytes > 0 && plain > 0 && plain <= bytes, $"两个尺寸都量得出来（开 {bytes} / 关 {plain} 字节）");
                Check(rec.actions.Count > 0, $"动作流非空（{rec.actions.Count} 条）");
                Check(rec.finalHash != 0, "记下了终局指纹（放的时候拿它对账）");

                // 诊断：把**录的那一局**的最后几条引擎事件与动作表打出来（回放侧会在分叉点打它自己的）
                var alog = driver.Ctx != null ? driver.Ctx.ActionLog : null;
                if (alog != null)
                    for (int i = Mathf.Max(0, alog.Count - 4); i < alog.Count; i++)
                        Debug.Log(P + $"   【录】事件{i}: {alog[i].Kind}#p{alog[i].Player}s{alog[i].Slot}<-p{alog[i].TargetPlayer}s{alog[i].TargetSlot}:{alog[i].CardId}");
                for (int i = 0; i < Mathf.Min(8, rec.actions.Count); i++)
                {
                    var mm2 = rec.actions[i];
                    string pk = mm2.picks == null ? "null" : mm2.picks.Length.ToString();
                    Debug.Log(P + $"   【录】动作{i}: kind={mm2.kind} actor={mm2.actor} handIdx={mm2.handIdx} slot={mm2.slot} "
                                + $"tgtP={mm2.targetP} tgtSlot={mm2.targetSlot} alt={mm2.altKeyword} picks={pk}");
                }

                // ③ ★ **反面用例**（先做 —— 下面那次正面重建会把它覆盖掉）：
                //    靶场手改过局面那一局**放不出来**，而且必须**当场报出来**（红线：不许静默失败）。
                //    没有这一条的话，「回放悄悄演成另一局」这种事就没人管了。
                //    ⚠️ 只在「最多的那一份 ≠ 刚打完那份」时跑 —— 相等时说明这局也是干净的，跳过。
                if (newest != playable)
                {
                    Debug.Log(P + "   ★ 反面用例：下面这几行**红字是预期的**（靶场局放不出来 ⇒ 必须出声）");
                    bool bad = driver.PlayReplay(rec);
                    Check(!bad, "★★ **靶场手改过局面的那局放不出来，而且【报出来了】**"
                              + "（不是静默演成另一局 —— 「不许静默失败」的落地）");
                }
                // 🔴 **A195**：原来这一支**连出声都没有** —— 两份相等时那条 ★★ 就静默不跑。
                //    这里**不加断言**：相等是**合法情形**（说明「动作最多那一份」就是刚打完的干净局），
                //    真红了反而是假警报 ⇒ 按红线取「出声」那一半（同 §1f 那句「没建 3D 战场 ⇒ 跳过」的做法）。
                else Debug.Log(P + "   （「动作最多那一份」就是刚打完那份 ⇒ 上面那条反面用例**跳过**"
                                  + " —— 不是失败，但这一档下那条 ★★ 等于没验；A195）");
            }

            // ② ★★ **正面：放刚打完的那一局**（`BeginFromPendingCore` 重建 + 全量灌动作，最后比指纹）
            var playRec = string.IsNullOrEmpty(playable) ? null : ReplayStore.Load(playable);
            Check(playRec != null, $"刚打完那一局的录像读得回来（`{playable}`）");
            if (playRec != null)
            {
                // 🔴 **A381**（2026-10-12）：放一局录像 **一笔记账都不许有** —— 判据 = 原版
                //   `ChallengeLogMgr.LogMatchEnd` **只在真打完时叫**（回放不是「真打完」）。
                //   闸在驱动侧（`BattleDriver.UpdateHud` 那个结算块，字段 `_replaySession`），这里读它的自检口。
                //   ⚠️ 三个基线**必须在 `PlayReplay` 之前**记：那一趟会把动作**一口气**灌完、`Ctx.IsOver`
                //   在那趟结束时就为真，而画面靠 `_timeline` 慢慢演 ⇒ 闸要是只在那一趟为真，
                //   下一帧 `Update()` 就复发（所以它必须**整场回放期间**都留着）。
                //   🧨 **改坏法**：删掉 `PlayReplay` 里置 `_replaySession` 那一句、或删掉结算块里那个分支
                //   ⇒ 下面四条**同时红**（结算账 +1 / 对局记录 +1 / 日常骷髅 +N / 结算面板弹出来）。
                int logBeforeA381   = BattleLogData.Count;
                int skullBeforeA381 = DailyData.SkullsCountValue();
                int settleBefore381 = driver.SettleCount;
                Debug.Log(P + $"   【放】刚打完那局：{playRec.actions.Count} 条动作 · 座位 {playRec.mySeat}");
                // 🆕 **2026-10-13（A410）**：这一趟顺手把日志抓下来 —— 回放走的就是 `BeginFromPendingCore`
                //   （`PlayReplay` 传的是 `attachNet: false`），而那里面那句开局日志原来**写死**「联机开局」
                //   ⇒ 回放局在日志里也自称联机局。抓法 = 本仓现成的那一套（`Application.logMessageReceived`，
                //   同 `Editor/SettingsScene.cs` 的 `CaptureErrors`）。
                var log410 = new List<string>();
                Application.LogCallback h410 = (string m, string st, LogType ty) => log410.Add(m);
                Application.logMessageReceived += h410;
                bool same;
                try { same = driver.PlayReplay(playRec); }
                finally { Application.logMessageReceived -= h410; }
                Check(same, "★★ **回放演完，指纹与录制时【相等】⇒ 演的是同一局**"
                          + "（这是「录全了没有」唯一的尺子 —— 不等就说明有动作没录到）");
                // 🆕 **2026-10-13（A410）**：两句一起才咬得住「**照实情说**」——
                //   ① 不许再自称「联机开局」（判据 = 原版 `MatchType.Replay = 160` 是**独立的一档**，同 A387）；
                //   ② 也不许干脆不出声（红线：不许静默失败）⇒ 它得说清自己是「回放开局」。
                // 🧨 **改坏法**：把 `BeginFromPendingCore` 那句日志改回写死的「联机开局」⇒ 第 1 条红；
                //   把整句删掉（或只说状态、不再说这是哪一种局）⇒ 第 2 条红。
                //   ⚠️ 这一趟是**单机放录像**（上面 `driver.AttachNet(null)` 已经摘掉联机）⇒ 驱动侧判据落到
                //      `attachNet == false && _net == null` 那一档；联机 / 重连那两档的措辞这里**不验**。
                string log410txt = string.Join("\n", log410.ToArray());
                Check(!log410txt.Contains("联机开局"),
                      $"★ A410：回放局的日志**不再自称「联机开局」**（这一趟抓了 {log410.Count} 行日志）"
                    + " —— 回放与联机走的是同一个 `BeginFromPendingCore`，可**回放不是联机局**"
                    + "（原版 `MatchType.Replay = 160` 是独立的一档；同 A387）");
                Check(log410txt.Contains("回放开局"),
                      $"★ A410：那一趟**说清了自己是哪一档**（日志里有「回放开局」；共抓 {log410.Count} 行）"
                    + " —— 只把「联机开局」删掉、什么也不说同样是错（红线：不许静默失败）");
                // 🆕 **2026-10-12（A387，W4 备好的原文照贴）**：回放局**不再借「联机局」那张标签**。
                //   账的落点先订正：`deckNote` **根本不进「对局记录」**（`RecordBattleLog` 不收它、
                //   `BattleLogData.Match` 也没这一格）—— 它唯一去处是**提示行**（`Begin` → `_deckNotice` → `SetHint`）。
                //   而原版**没有我们这条提示行**（`SetHint` 文件头自己写着）⇒ 「回放该写哪句话」**没有判据**
                //   ⇒ 按「不发明文案」把它传 `null`。原版能给到的判据只到「**回放是独立的一档**」
                //   （`MatchType.Replay = 160`）。
                //   🧨 **改坏法**：把 `PlayReplay` 的 `deckNote: null` 换回 `"联机局"`（或任何非 null）⇒
                //   下面第 1 条**必红**（它比的是**传进去的原样值**，与「录的那局有没有带卡组」**无关**
                //   —— 这正是为什么不比成品句子 `DeckNotice`）。
                Check(driver.RawDeckNoteForTest == null,
                      "★ A387：回放局**不再借「联机局」那张标签**（`deckNote` 实得 "
                    + (driver.RawDeckNoteForTest == null ? "`null`" : "「" + driver.RawDeckNoteForTest + "」")
                    + "）—— 回放不是联机局（原版 `MatchType.Replay = 160` 是独立的一档）");
                // ⚠️ 第 2 条是**哨兵、不是主力**（如实标）：只有「录的那一局本来就没带卡组」时，旧实现才会
                //   拼出这两句 ⇒ 那一档下它红；别的档它**恒绿**，**单独不构成判据**。要两条都硬，
                //   就得让自检里那局录像本身不带卡组 —— 那是录像节的写法问题，不在 A387 这一笔账里。
                Check(!driver.DeckNotice.Contains("联机局") && !driver.DeckNotice.Contains("读不出来"),
                      $"★ ……提示行也不再说这两句（实得「{Short(driver.DeckNotice, 44)}」）");
                Check(driver.Ctx != null && driver.Ctx.IsOver, "回放演到终局（这一局演完了）");
                // ① 驱动侧那笔账（**自检口**）。
                Check(driver.SettleCount == settleBefore381,
                      $"★ A381：放一局录像 ⇒ 结算账**一笔都没多**（{settleBefore381} → {driver.SettleCount}）"
                    + " —— 原版 `LogMatchEnd` 只在真打完时叫");
                // ② 三个**独立于驱动**的可观测（这三条才是「真的没记」的判据，别只信驱动那个计数器）。
                Check(BattleLogData.Count == logBeforeA381,
                      $"★ ……对局记录**没多**（{logBeforeA381} → {BattleLogData.Count}）");
                Check(DailyData.SkullsCountValue() == skullBeforeA381,
                      $"★ ……日常骷髅**没多**（{skullBeforeA381} → {DailyData.SkullsCountValue()}；"
                    + $"本节那局自己拿了 {skullBeforeA381 - skullBeforeSampleA381} 颗 ⇒ 这条"
                    + (skullBeforeA381 - skullBeforeSampleA381 > 0 ? "**真的在断东西**）" : "**是恒真的**（如实说））"));
                Check(driver.End == null || !driver.End.Visible, "★ ……结算面板与开门视频**没弹**");
                // ③ 闸的前提（机制那一格）：它是「整场回放期间」为真、不是「灌动作那一瞬」。
                //    ⚠️ 与上面几条**分开断**：这一条红了是「闸的形态变了」，上面几条红了才是「账记多了」。
                Check(driver.ReplaySession,
                      "★ A381：回放局标记**整场都在**（`ReplaySession`；清掉的唯一入口是 `Begin()` = 新开一局）");
                Check(playRec.trace != null && playRec.trace.Count == playRec.actions.Count,
                      $"逐条轨迹与动作**一一对应**（{playRec.trace.Count} 条）—— 少一条就少对一个分叉点");
            }

            // ---- 🆕 2026-10-12（A382）：结算那一块的**闩**是 `_settled`（一局一张账），⛔ 不是面板可见性 ----
            //   旧实现 `if (_endPanel != null && !_endPanel.Visible)` 有两层病：
            //     ① 面板为 `null` ⇒ 四件事（面板 / 日常 / 对局记录 / 录像收尾）**全静默跳过**、没人出声；
            //     ② 那个「可见性」本来也不是闩 —— 它靠的是「新一局一开局，`UpdateHud` 的 `else` 支顺手
            //        `Hide()`」这个**副作用**。
            //   ⚠️ **「面板为 `null`」那一半自检里造不出来**（`EndPanel.Create` 恒建、不返回 `null`，
            //      见 `S2` §二.6）⇒ 「出声且账照记」那一半**只能靠代码审查** —— **如实记，不假称验过**。
            //   能验的那一半 = **闩不依赖面板可见性**：把面板 `Hide()` 掉再刷一帧。
            //   🧨 **改坏法**：把驱动侧 `if (!_settled) { _settled = true; … }` 换回
            //   `if (_endPanel != null && !_endPanel.Visible)` ⇒ 本段第二条红（面板被 `Hide()` 过 ⇒ 又记一笔）。
            {
                int settleBefore382 = driver.SettleCount;
                int logBefore382    = BattleLogData.Count;
                driver.Begin("Ultramarines", "Goff", 20260913);
                if (driver.InMulligan) driver.SimulateMulliganDone();
                driver.Forfeit();                      // 走真入口（与 §12 投降那一节同一条路）
                Check(driver.Settled && driver.SettleCount == settleBefore382 + 1
                      && BattleLogData.Count == logBefore382 + 1,
                      $"★ A382：一局**恰好**记一笔账（结算账 {settleBefore382} → {driver.SettleCount}；"
                    + $"对局记录 {logBefore382} → {BattleLogData.Count}）");
                if (driver.End != null) driver.End.Hide();   // 闩若是「面板可见性」，这一句之后就会**再记一笔**
                driver.RefreshAll();                        // 再刷一帧（`RefreshAll` 末尾就是 `UpdateHud`）
                Check(driver.SettleCount == settleBefore382 + 1 && BattleLogData.Count == logBefore382 + 1,
                      $"★ A382：把面板 `Hide()` 掉再刷一帧也**不会**再记一笔"
                    + $"（结算账 {driver.SettleCount}、记录 {BattleLogData.Count}）"
                    + " —— 闩是 `_settled`、不是面板可见性");
            }

            // 上限修剪：多存几份 ⇒ 只留最新的，旧的自动删
            int before = ReplayStore.Count;
            for (int i = 0; i < 3; i++)
                ReplayStore.Save(new ReplayRecord { savedAt = "2000-01-0" + (i + 1) + " 00:00:00", myHero = "Old" + i, foeHero = "X" });
            Check(ReplayStore.Count == before + 3, $"又塞了 3 份 ⇒ 一共 {before + 3} 份");
            int removed = ReplayStore.TrimToLast(before);
            Check(removed == 3, "按上限修剪 ⇒ **删掉 3 份最旧的**（不是删新的）");
            Check(ReplayStore.Count == before, $"修完剩 {before} 份");
            ReplayStore.ResetForTest();
            Check(ReplayStore.Count == 0, "自检收尾：清空临时目录");
        }

        // ---------------- 🆕 2026-10-12（A175）：`Auto Zoom` 的消费者（原版 `CombatAutoZoom`）----------------
        // 判据（全部实读，全文 → `Battle/CombatAutoZoom.cs` 文件头）：
        //  · 方法体 = `d:/2/tools/decomp_full/CombatAutoZoom__{ctor,Initialize,OnEnable,OnDisable,
        //    OnMinionNumberChanged,SetZoomLevel,ResetCameraZoomUIAction,ForceRefresh}.c`（`ForceRefresh` 与
        //    `ResetCameraZoomUIAction` **同一个 RVA 0x60CA80** = 逐字节同体）；
        //  · `SetZoomLevel` 里被反编译器吃掉的那两个操作数 = **指令流实读**（`工具/disasm_va.py … 0x18060ce40`）：
        //    开关真 ⇒ `Evaluate(人数)`、假 ⇒ 常量 `DAT_1834b2bb8` = **1.0**；上夹取也是这个 1.0；
        //  · 曲线 `unitsZoomCurve` = `MonoBehaviour_5231.json` 等 **13 份逐字节相同**（md5 `ca085afd`）；
        //  · 触发 = `MinionManager__RefreshOccupationSlots.c`（全反编译里 `OnMinionAddedOrRemoved` 的**唯一 Invoke 点**）。
        //  · 期望的 `lensShift.y` 两个数（−0.210542 / −0.250836）= 拿**原版那几条曲线**逐段 Hermite 手算出来的
        //    （`zoom = 1` / `zoom = 0`；算式就写在下面 `OffFrameA175` 那几行），⛔ **不是**从
        //    `CombatAutoZoom.LensShiftY` 读的 —— 从被测实现里读期望值 = 自证（本仓红线）。
        //    🔴 **2026-10-12（A444）改过一次**：原来那对是 **−0.207943 / −0.250551**，是照 `CombatAutoZoom.cs` 里
        //    **抄错的**三条取景曲线（`ViewShift` 的四段切线 + `PadModByAspect` 的 `+0.0517`）算出来的
        //    —— 形式上像「手算自原版」、实际**钉的是实现里那一份错值**。曲线订正后重算见下面那几行。
        // ⚠️ **放在这里（整轮最靠后）的理由**：这一块会真的改那台战场相机的取景，而 13d 那组
        //    「卡在屏内 / 卡底边压在手牌之上」的断言量的是**同一台相机** ⇒ 必须在它们全跑完之后。
        // 🧨 **主改坏法**：① 把 `SetZoomLevel` 里「开关假 ⇒ 1.0」那一支删掉（关着也套曲线）⇒ 第 3 条红；
        //    ② 把 `if (!manualCamera)` 那道闸删掉 ⇒ 第 7 条红；③ 把 `TargetZoomLevel` 的 setter 里
        //    `ApplyFraming()` 删掉（只算不落）⇒ 第 4/6 条红（`FramingApplyCount` 也钉着这件事）。
        {
            var az = driver.AutoZoom;
            Check(az != null && az.boardCamera == driver.boardCam && driver.boardCam != null,
                  "★ A175：3D 战场这一档下**消费者建出来了**，而且拿着的是**那台战场相机**"
                + $"（`BattleDriver.AutoZoom`；原版 `combatCameraZoom.targetCamera` = 场景里的 `BoardCamera`）"
                + $" —— 实得 {(az == null ? "`null`" : (az.boardCamera == null ? "相机为 null" : "对上了"))}");

            if (az != null && driver.boardCam != null)
            {
                // ① 曲线 = 原版那 5 个键（期望值是**原版字面量**，⛔ 不读 `az` 里那份）。
                Check(Mathf.Abs(CombatAutoZoom.EvaluateUnitsZoom(0f)) < 1e-6f
                   && Mathf.Abs(CombatAutoZoom.EvaluateUnitsZoom(2f)) < 1e-6f
                   && Mathf.Abs(CombatAutoZoom.EvaluateUnitsZoom(3f) - 0.105257757f) < 1e-5f
                   && Mathf.Abs(CombatAutoZoom.EvaluateUnitsZoom(4f) - 1f) < 1e-6f
                   && Mathf.Abs(CombatAutoZoom.EvaluateUnitsZoom(9f) - 1f) < 1e-6f,
                      "★ A175：人数→zoom 的曲线就是原版那 5 个键（**≤2 ⇒ 0 · 3 ⇒ 0.1052578 · ≥4 ⇒ 1**；"
                    + "判据 = `MonoBehaviour_5231.json`，13/13 场逐字节相同）");

                // ② 前提：宽高比就是 **16:9**（批处理里 `Shot()` 定死 `cam.aspect = (float)1920 / 1080`）——
                //    下面两个期望值就是按**这个**宽高比算出来的。
                //    🔴 **2026-10-12（A444）订正**：这里原来断的是「宽高比在 [1.333, 1.77]（只有这一档
                //    `PadMod ≡ 1`）」，而**实况是 16:9 = 1.7777778，比 1.77 高** ⇒ 那条前提**当场就是红的**；
                //    而且 `verticalPaddingModifierByAspectRatio(16:9) = 0.9951903 ≠ 1`（它在第 3 键 1.77
                //    之后才开始掉，16:9 已经进了下降段）⇒ 两个期望值必须把这一项带上。
                //    ⚠️ 本工程踩过同款坑（13d 那条「判据要在判据成立的条件下量」），所以把前提也断出来。
                float aspect = driver.boardCam.aspect;
                Check(Mathf.Abs(aspect - 16f / 9f) < 1e-4f,
                      $"（前提）战场相机宽高比 = **16:9**（{aspect:F6}；`Shot()` 里 `cam.aspect = (float)1920 / 1080`）"
                    + " —— 下面两个 `lensShift.y` 期望值就是按这个宽高比算的"
                    + "（`verticalPaddingModifierByAspectRatio(16:9) = 0.9951903`，**不是** 1.0）");

                // 这一局重开一把干净的（前面那些段落把棋盘/对局都折腾过）。
                driver.Begin("Ultramarines", "Goff", 20260914);
                if (driver.InMulligan) driver.SimulateMulliganDone();
                driver.RefreshAll();

                // 两个期望值 = 拿**原版那三条曲线**逐段 Hermite **手算**出来的（⛔ 不是从被测实现里读的 —— 那是自证）：
                //   曲线逐值照抄 `MonoBehaviour_4697.json`（判据资产，13/13 场逐字节相同）；
                //   ★ **2026-10-12（A444）：曲线订正后重算过**（旧值 −0.207943 / −0.250551 是照**抄错的**那三条算的）。
                //   `minY = 249.9 / 1080 = 0.2313889`；
                //   `maxY(zoom) = (1080 − 96.7) / 1080 + PadMod(aspect) × PadByZoom(zoom)`
                //                = `0.9104630 + 0.9951903 × PadByZoom(zoom)`（宽高比 = 16:9 ⇒ `PadMod = 0.9951903`，见上面的前提）
                //   `arg = (maxY + minY) × 0.5`；`lensShift.y = ViewShift(arg)`
                //   · `zoom = 1`：`PadByZoom(1) = −0.1710815` ⇒ `arg = 0.4857966` ⇒ **−0.210542**
                //   · `zoom = 0`：`PadByZoom(0) = −0.0320131`（曲线左端**取端点值**）⇒ `arg = 0.5549963` ⇒ **−0.250836**
                //   ⚠️ 宽高比 ≤ 1.77 时（`PadMod ≡ 1`）这两个数是 **−0.210265 / −0.250793** —— A444 报告 §5·1 给的
                //      就是这一对（它把 `PadMod ≡ 1` 当成了前提）；**实况是 16:9，必须用上面那一对**。
                //   ⚠️ 订正前后差 **0.0026 / 0.00029**（前者 > 容差 0.001）⇒ 曲线被改回旧值这条会红。
                const float OffFrameA175 = -0.210542f;   // zoom = 1（= 烘进场景那一档，也是「关着」那一档）
                const float OnFrameA175  = -0.250836f;   // zoom = 0（= 原版曲线在人少那一档给的取景）
                float offFrame = OffFrameA175, onFrame = OnFrameA175;
                int evBefore   = az.ZoomEventCount;
                int applyBefore = az.FramingApplyCount;

                // ③ **关着 ⇒ 不缩放**。触发条件走**真那条路**（棋盘变 → 消费者收到人数事件）。
                AutoZoom.RestoreForTest(false, false);                        // 开关关（内存态，⛔ 不落盘）
                driver.Ctx.Players[0].Board[BoardSlots.SlotOf(BoardSlots.Left, 0)] =
                    new UnitState(CardByName(StarterCards.Ember(), "Veteran"), false);
                driver.RefreshAll();                                         // 棋盘一变 ⇒ `TickAutoZoom` 抬事件
                Check(az.ZoomEventCount > evBefore,
                      "★ A175：棋盘一变 ⇒ 消费者**真的收到人数事件**（原版 `MinionManager.OnMinionAddedOrRemoved` →"
                    + $" `OnMinionNumberChanged`；累计 {evBefore} → {az.ZoomEventCount}）");
                Check(Mathf.Abs(az.ZoomLevel - 1f) < 1e-6f,
                      $"★ A175：**开关关着 ⇒ zoom = 1（不缩放）**（实得 {az.ZoomLevel:F6}；"
                    + "原版 `SetZoomLevel` 那一支取常量 1.0，读指令流实得）");
                Check(Mathf.Abs(driver.boardCam.lensShift.y - offFrame) < 0.001f,
                      $"★ ……取景**一字不差**停在烘出来那一档（`lensShift.y` 实得 {driver.boardCam.lensShift.y:F6}，"
                    + $"本局零点 {offFrame:F6}）—— 关着的时候这一格**什么都不该改**");

                // ④ **开着 + 人少 ⇒ zoom = 0**，而且取景真的动了。触发走**菜单那颗开关同一条路**
                //    （`FindObjectByType<CombatAutoZoom>().ForceRefresh()` = 原版 `BattleSettingsWindow__OnAutoZoomChanged`）。
                AutoZoom.Set(true);
                az.ForceRefresh();
                Check(Mathf.Abs(az.ZoomLevel) < 1e-6f,
                      $"★ A175：**开着 + 较忙那侧 1 人（≤2）⇒ zoom = 0**（实得 {az.ZoomLevel:F6}，"
                    + "原版曲线第一档就是 0）");
                Check(Mathf.Abs(driver.boardCam.lensShift.y - onFrame) < 0.001f
                   && Mathf.Abs(onFrame - offFrame) > 0.02f,
                      $"★ ……取景**真的动了**：`lensShift.y` {offFrame:F6} → {driver.boardCam.lensShift.y:F6}"
                    + $"（期望 {onFrame:F6}；两档差 {Mathf.Abs(onFrame - offFrame):F4} ⇒ 这条分得开开/关两种状态）");
                Check(az.FramingApplyCount > applyBefore,
                      $"★ ……而且是**真写进相机**的，不只是算了个数（写入次数 {applyBefore} → {az.FramingApplyCount}）");

                // ⑤ **开着 + 人多 ⇒ zoom = 1**（曲线给满）⇒ 取景回到不缩放那一档。**两态真的翻得动**。
                for (int i = 0; i < BoardSpec.SlotsPerSide; i++)
                    driver.Ctx.Players[0].Board[BoardSlots.SlotOf(BoardSlots.Left, i)] =
                        new UnitState(CardByName(StarterCards.Ember(), "Veteran"), false);
                driver.RefreshAll();
                Check(Mathf.Abs(az.ZoomLevel - 1f) < 1e-6f
                   && Mathf.Abs(driver.boardCam.lensShift.y - offFrame) < 0.001f,
                      $"★ A175：**开着 + 较忙那侧 4 人（≥4）⇒ zoom = 1** ⇒ 取景回到不缩放那一档"
                    + $"（zoom {az.ZoomLevel:F6} · `lensShift.y` {driver.boardCam.lensShift.y:F6} vs {offFrame:F6}）");

                // ⑥ `manualCamera` 那道闸（原版 `if (!combatCameraZoom.manualCamera) set_TargetZoomLevel(…)`）
                //    —— **能分辨的那一半**：先置真，再把棋盘改成「该给 zoom 0」的样子（人少），
                //    走**非 force** 的事件那条路 ⇒ **不该动**。
                az.manualCamera = true;
                for (int i = 0; i < BoardSpec.SlotsPerSide; i++)
                    driver.Ctx.Players[0].Board[BoardSlots.SlotOf(BoardSlots.Left, i)] = null;
                driver.Ctx.Players[0].Board[BoardSlots.SlotOf(BoardSlots.Left, 0)] =
                    new UnitState(CardByName(StarterCards.Ember(), "Veteran"), false);
                driver.RefreshAll();
                Check(az.manualCamera && Mathf.Abs(az.ZoomLevel - 1f) < 1e-6f
                   && Mathf.Abs(driver.boardCam.lensShift.y - offFrame) < 0.001f,
                      "★ A175：`manualCamera` 为真时**人数事件不动取景**（现在较忙那侧只有 1 人 ⇒ 本该给 0，"
                    + $"实得 zoom {az.ZoomLevel:F6} · `lensShift.y` {driver.boardCam.lensShift.y:F6}）"
                    + " —— 原版那一道 `if (!manualCamera)` 就是它");

                // ⑦ **`force` 那条路会把手动档清掉**（原版 `if (force) { … *(combatCameraZoom + 0x30) = 0; }`）。
                az.ForceRefresh();
                Check(!az.manualCamera && Mathf.Abs(az.ZoomLevel) < 1e-6f
                   && Mathf.Abs(driver.boardCam.lensShift.y - onFrame) < 0.001f,
                      $"★ A175：**`ForceRefresh()`（force 那条路）清掉手动档并重算**"
                    + $"（`manualCamera` {az.manualCamera} · zoom {az.ZoomLevel:F6} · "
                    + $"`lensShift.y` {driver.boardCam.lensShift.y:F6} vs {onFrame:F6}）");

                // ⑧ 🆕 **2026-10-12（A421）**：`sensorSize.x` 那一半（原版 `CameraVerticalFramer.cameraSizeXTable`）。
                //   判据（全是实读）：曲线 6 键 = `bundle_scenes_scenes_battlearena1` 的 `MonoBehaviour_4697.json`
                //   （13/13 场逐字节相同）· `maxVerticalSizeInViewPort`（`framer+0x78`）= **7.0** ·
                //   那一段的指令流 = `CameraVerticalFramer$$CalculateFraming` VA 0x1806085D0 的
                //   `0x180608B4F`(abs 掩码) → `0x180608B69`(Math.Min) → `0x180608BB0`(取 `+0x38` 求值)
                //   → `0x180608C02-C0E`(`(origX − curve) × clamp01(zoom) + curve`)。
                //   **期望值全是原版字面量**，插值在本文件里**自己算一遍 Hermite**（⛔ 不读被测实现的静态口）。
                //   🧨 改坏法：① 曲线抄错一个键 ⇒ ①/③ 红；② 把 `Mathf.Min(…, 7)` 那个上夹去掉 ⇒ ②/④ 红；
                //   ③ `zoom = 1` 那支不恒等（比如忘了夹 zoom）⇒ ③ 红；④ `ApplyFraming` 里不写 `sensorSize.x`
                //   （只写 lensShift）⇒ ⑤ 红。
                {
                    // 原版 `cameraSizeXTable` 的 6 个键 / 值 / 段切线（原版字面量，抄自 `MonoBehaviour_4697.json`；
                    // 原版这 6 键满足 `outSlope[i] == inSlope[i+1]` ⇒ 一段只有一个切线）
                    float[] kt = { 4.930829048156738f, 5.591578006744385f, 5.856215000152588f,
                                   7.6655964851379395f, 8.940309524536133f, 11.45475959777832f };
                    float[] kv = { 41.084930419921875f, 35.6795539855957f, 33.34006881713867f,
                                   25.721040725708008f, 22.336490631103516f, 17.468660354614258f };
                    float[] km = { -8.180681228637695f, -8.84035587310791f, -4.210846900939941f,
                                   -2.655146598815918f, -1.93594229221344f, -2.3610286712646484f };
                    // Unity `AnimationCurve.Evaluate`（`m_PreInfinity/m_PostInfinity = 2` = Clamp ⇒ 两端取端点值）
                    // 在这几条键上的**逐段三次 Hermite**（`weightedMode = 0` ⇒ 权重不参与，切线就是上面那些）。
                    float wantCamX(float t)
                    {
                        if (t <= kt[0]) return kv[0];
                        if (t >= kt[5]) return kv[5];
                        for (int i = 0; i < 5; i++)
                            if (t >= kt[i] && t <= kt[i + 1])
                            {
                                float h = kt[i + 1] - kt[i], s = (t - kt[i]) / h, s2 = s * s, s3 = s2 * s;
                                return (2f * s3 - 3f * s2 + 1f) * kv[i] + (s3 - 2f * s2 + s) * h * km[i]
                                     + (-2f * s3 + 3f * s2) * kv[i + 1] + (s3 - s2) * h * km[i];
                            }
                        return kv[5];
                    }

                    // ① 曲线 = 原版那 6 个键（逐键比原版字面量）+ 两端 Clamp。
                    bool keysOk = true;
                    for (int i = 0; i < 6; i++)
                        if (Mathf.Abs(CombatAutoZoom.EvaluateCameraSizeX(kt[i]) - kv[i]) > 1e-3f) keysOk = false;
                    Check(keysOk
                       && Mathf.Abs(CombatAutoZoom.EvaluateCameraSizeX(0f) - 41.084930419921875f) < 1e-3f
                       && Mathf.Abs(CombatAutoZoom.EvaluateCameraSizeX(20f) - 17.468660354614258f) < 1e-3f
                       && Mathf.Abs(CombatAutoZoom.MaxVerticalSizeInViewPort - 7f) < 1e-6f,
                          "★ A421：`cameraSizeXTable` 就是原版那 **6 个键**、两端 Clamp，"
                        + "`maxVerticalSizeInViewPort` = **7.0**（判据 = `MonoBehaviour_4697.json`，13/13 场逐字节相同）");

                    // ② `Mathf.Min(|Δ世界 Y|, maxVerticalSizeInViewPort)` 那一段（**上夹真的在**）。
                    Check(Mathf.Abs(CombatAutoZoom.BoundsVerticalSizeInViewport(7.6837f) - 7f) < 1e-5f
                       && Mathf.Abs(CombatAutoZoom.BoundsVerticalSizeInViewport(-3.2f) - 3.2f) < 1e-5f
                       && Mathf.Abs(CombatAutoZoom.BoundsVerticalSizeInViewport(7f) - 7f) < 1e-5f,
                          "★ A421：`bounds` = `Mathf.Min(Mathf.Abs(Δ世界 Y), 7.0)`（上夹 + 取绝对值两半都在）");

                    // ③ `sensorSize.x = (origX − curve) × clamp01(zoom) + curve`：`zoom = 1` **恒等**、`zoom = 0` 取曲线值、
                    //    zoom 超出 [0,1] 被夹（原版那两条 `comiss`）。期望值 = 上面那个**独立 Hermite**。
                    float wantC7 = wantCamX(7f);          // = 28.523765f（手算：原版第 3–4 键之间）
                    Check(Mathf.Abs(CombatAutoZoom.FrameSensorSizeX(41.5f, 7f, 1f) - 41.5f) < 1e-4f
                       && Mathf.Abs(CombatAutoZoom.FrameSensorSizeX(41.5f, 7f, 0f) - wantC7) < 1e-3f
                       && Mathf.Abs(wantC7 - 28.523765f) < 1e-3f
                       && Mathf.Abs(CombatAutoZoom.FrameSensorSizeX(41.5f, 7f, 2f)
                                  - CombatAutoZoom.FrameSensorSizeX(41.5f, 7f, 1f)) < 1e-4f
                       && Mathf.Abs(CombatAutoZoom.FrameSensorSizeX(41.5f, 7f, -1f)
                                  - CombatAutoZoom.FrameSensorSizeX(41.5f, 7f, 0f)) < 1e-4f,
                          $"★ A421：`sensorSize.x` 的 lerp —— `zoom = 1` **恒等**（41.5 → "
                        + $"{CombatAutoZoom.FrameSensorSizeX(41.5f, 7f, 1f):F4}）· `zoom = 0` = 曲线值 "
                        + $"{wantC7:F6}（手算自原版那 6 个键 = 28.523765）· zoom 被 `Clamp01`");

                    // ④ **现量**：`bounds` 的自变量 = 「敌方区下沿 ↔ 我方手牌区上沿」在战场相机世界系里的高差。
                    //    期望值 = 原版那一条链在本文件里**独立写一遍**（⛔ 不读 `CombatAutoZoom` 任何静态口）：
                    //      z  = `boardCamera.transform.InverseTransformPoint(场地根).z`（原版那一格字面量 = 100.0
                    //           = 原版场地根 x；我们整体平移 −100 ⇒ 等价点 = `Vector3.zero`，z 恒等）
                    //      Δvp.y = (敌方下沿 − 手牌上沿) 的**视口** y 差（`LayoutSpace` 可见高 = 10 世界单位）
                    //      K  = `2 · z · tan(vFOV/2)`，`tan(vFOV/2) = (sensorSize.x / aspect) / 2 / focal`
                    //           （原版 `gateFit = Horizontal`：竖直画幅由 `sensorSize.x / aspect` 定）
                    {
                        var bcam4 = driver.boardCam;
                        // `sensorSize.x` **逐场两个取值**（41.5 / 37.2）—— 从**清单**读原版值（⛔ 不从相机现读，
                        // 那时相机上可能正挂着上一次算出来的曲线值），同 §存档检查那条的取法。
                        var camMf4 = ArenaBuilder.LoadManifest(BoardArena);
                        float sx4 = (camMf4 != null && camMf4.camera != null && camMf4.camera.sensorSizeX > 0f)
                                  ? camMf4.camera.sensorSizeX : 41.5f;
                        float zz = bcam4.transform.InverseTransformPoint(Vector3.zero).z;
                        float halfPx = LayoutSpace.DesignPxH * 0.5f;                       // 540
                        float enemyY4 = ((1080f - 96.7f) - halfPx) / 108f;                // 原版敌方 helper 下沿（从屏底 983.3 px）
                        float playerY4 = (249.9f - halfPx) / 108f;                        // 原版手牌 helper 上沿（249.9 px · M = 1）
                        float tanHalf = (sx4 / bcam4.aspect) * 0.5f / 28f;                // focal 28（原版 `Camera_1461.json`）
                        float wantDelta = 2f * zz * tanHalf * (enemyY4 - playerY4) / LayoutSpace.DesignHeight;
                        // ⚠️ 上面用的是 `sx4`（原版那个 sensorSize.x）—— **量的时候相机上必须是它**
                        //    （原版那一句 `set_sensorSize(+0x98)` 就是把相机换成它再量；不还原会自激）。
                        // ⚠️ **不许断「它一定 > 7」**：`sensorSize.x` 逐场两个取值（41.5 / 37.2）而 `z` 也随场，
                        //    16:9 下窄的那三场算出来是 **6.888**（< 7，上夹**不**咬）⇒ 断死了会把那三场判红。
                        Check(Mathf.Abs(Mathf.Abs(az.MeasuredWorldDeltaY) - wantDelta) < 0.02f
                           && Mathf.Abs(az.BoundsVerticalSize - Mathf.Min(Mathf.Abs(az.MeasuredWorldDeltaY), 7f)) < 1e-5f
                           && az.BoundsVerticalSize <= 7f + 1e-5f,
                              $"★ A421：现量的「敌我卡区高差」= {az.MeasuredWorldDeltaY:F4} 世界单位"
                            + $"（本文件独立算一遍 = {wantDelta:F4}；`z` {zz:F4} · 宽高比 {bcam4.aspect:F4}"
                            + $" · 原版 `sensorSize.x` {sx4:F3}）"
                            + $" ⇒ `bounds` = {az.BoundsVerticalSize:F4} = `Min(|高差|, 7.0)`"
                            + $"（{(Mathf.Abs(az.MeasuredWorldDeltaY) > 7f ? "上夹咬住了" : "上夹没咬、取的就是它")}）"
                            + "；⚠️ 实现返回的是 `y(手牌上沿) − y(敌方下沿)`（原版那个相减次序，**负**数）"
                            + " —— 所以左边先取一次绝对值");
                    }

                    // ⑤ **两态**：开关开着 + 人少（此刻 zoom = 0）⇒ 相机 `sensorSize.x` **真的被写成曲线值**；
                    //    关掉 ⇒ **回到原样**。原样那两个数从**这一局真相机**上取（关着的时候写的就是它），
                    //    不是从被测实现里读的。
                    {
                        var bcam5 = driver.boardCam;
                        // 先把开关关掉、重算一次 ⇒ 相机上就是「原版那一档」
                        AutoZoom.RestoreForTest(false, false);
                        az.ForceRefresh();
                        float origSx = bcam5.sensorSize.x;
                        float wantOff = az.BoundsVerticalSize;                 // 与开关无关（只跟几何/相机走）
                        float wantOn = wantCamX(wantOff);                      // ⛔ 不被测实现，自己算的那一份
                        Check(Mathf.Abs(origSx - 41.5f) < 0.05f || Mathf.Abs(origSx - 37.2f) < 0.05f,
                              $"（前提）关着的时候相机 `sensorSize.x` = 原版那一档（{origSx:F3}；"
                            + "原版 13 台里两个取值 41.5 / 37.2）");
                        Check(Mathf.Abs(bcam5.sensorSize.y - 24f) < 0.05f,
                              $"（前提）`sensorSize.y` **没被我们碰过**（{bcam5.sensorSize.y:F3}，原版序列化值 24）");

                        AutoZoom.Set(true);                                   // 内存态（`PersistOverride` 已开，⛔ 不落盘）
                        az.ForceRefresh();                                    // = 战斗内那颗开关那条路
                        Check(Mathf.Abs(bcam5.sensorSize.x - wantOn) < 1e-3f
                           && Mathf.Abs(bcam5.sensorSize.x - origSx) > 0.5f,
                              $"★ A421：**开关开着 + 人少 ⇒ `sensorSize.x` 真的被写成曲线值**"
                            + $"（{origSx:F3} → {bcam5.sensorSize.x:F3}，期望 {wantOn:F3} = "
                            + $"`(origX − cameraSizeXTable({wantOff:F3})) × 0 + 曲线值`）"
                            + " —— 只算不落（`ApplyFraming` 里不写这一格）就会红");

                        AutoZoom.RestoreForTest(false, false);
                        az.ForceRefresh();
                        Check(Mathf.Abs(bcam5.sensorSize.x - origSx) < 1e-3f,
                              $"★ A421：**关掉 ⇒ `sensorSize.x` 回到原版那一档**（{bcam5.sensorSize.x:F3} vs {origSx:F3}）"
                            + " —— 两态真的翻得动（`zoom = 1` 时那个 lerp 恒等）");
                    }

                }

                // 收尾：把取景放回**不缩放**那一档（后面只剩 A388 那一段，别再让相机停在歪的地方）。
                AutoZoom.RestoreForTest(false, false);
                az.ForceRefresh();
                Check(Mathf.Abs(driver.boardCam.lensShift.y - offFrame) < 0.001f,
                      $"★ A175 收尾：开关放回**关** ⇒ 取景回到不缩放那一档（{driver.boardCam.lensShift.y:F6}）"
                    + " —— 玩家的真设置在整轮末尾由 `AutoZoom.RestoreForTest` 放回（见下面那段）");
            }
        }

        // ---------------- ★ A422（2026-10-12）：原版 `CombatCameraZoom` 那一件（手动缩放 / 拖拽 / 世界边界 / 平滑）----------------
        // 判据（全部实读，全文 → `Battle/CombatCameraZoom.cs` 文件头）：
        //  · 26 个方法体 = `d:/2/tools/decomp_full/CombatCameraZoom__*.c`（+ 逐 VA 复核了 6 处**反编译器认错的操作数**）；
        //  · 常量 = `.rdata` 直读（`工具/read_literal.py`）；序列化值 = `MonoBehaviour_4404.json`（13/13 场逐字节相同）；
        //  · 期望值**一律在本文件里另行推/算**（⛔ 不读被测实现的静态口 —— 那是自证）。
        // ⚠️ **三个前提**（不设的话下面几条恒真或恒假；判据 → H20 报告 §三·A）：
        //   ① `PointerOverUi = () => false`（原版 = `IsPointerOverUIObject(fingerId)`；本仓没有 UGUI `EventSystem`）
        //      —— 不接的话第一次出声一次、之后按「不在 UI 上」放行（结果一样，但会多一行日志）；
        //   ② 🔴 **`FocusedOverride = true`** —— **批处理里 `Application.isFocused` 恒 `false`**，
        //      而那道门（原版 `EventSystem.current.m_HasFocus`）会把手动那一路**整条挡掉**；
        //   ③ `PointerViewportOverride` —— 不钉住的话 `GetWorldPositionUnderMouse` 跟着一个假鼠标坐标走。
        // ⚠️ **③、⑤～⑫ 跑在一个临时 rig 上**（自建相机 + 自建 `CombatAutoZoom`）：几何由本段钉死 ⇒
        //   世界边界那一档**可算**，而且**不碰真战场相机**（后面还有别的段落量那一台）。
        //   **这不是「换一条实现」** —— rig 上跑的是同一个 `CombatCameraZoom` / `CombatAutoZoom` 类。
        // 🧨 主改坏法：① `DragSign` 改回 `+1` ⇒ 第 9 条红；② 序列化值抄成 ctor 默认（`zoomSpeed 0.1` /
        //   `invertMouseScrollWheel false` / `dragSensitivity 0.005`）⇒ 第 2 条红；③ `SmoothDamp` 换成直接赋值 ⇒ 第 10 条红；
        //   ④ `ClampShiftLimits` 两条 `if` 改成 `else if` ⇒ 第 7 条（后者覆盖前者那一半）红；
        //   ⑤ 删掉 `if (manualCamera) return false;` ⇒ 第 11 条红；⑥ 删掉 `manualCamera = false` ⇒ 第 12 条红。
        {
            var az422 = driver.AutoZoom;
            var cz422 = az422 != null ? az422.CameraZoomForTest : null;
            if (az422 == null || cz422 == null)
            {
                // 不许静默：下面十几条全依赖它（`SetupAutoZoom` 只在有 3D 战场相机时才建它）
                Check(false, "★ A422：原版 `CombatCameraZoom` 那一件**没建出来** ⇒ 十几条一条都验不了"
                           + "（HUD 建了但没有 3D 战场相机那一档；原因见 `SetupAutoZoom` 的日志）");
            }
            else
            {
                Check(cz422.targetCamera == driver.boardCam && cz422.framer == az422,
                      "（前提）★ A422：那一件拿着的是**这台**战场相机 + **这个**取景器"
                    + "（原版 `CombatCameraZoom.targetCamera`(+0x20) = 场景里的 `BoardCamera`；`cameraVerticalFramer`(+0x50)）");
                Check(!SmallScreenUI.Enabled,
                      "（前提）★ A422：`smallScreenUI` **关着** —— 开着时 `CombatCameraZoom.TargetZoomLevel` 的 setter"
                    + " 会把上界压到 ~0.305（16:9），下面几条期望值按「上界 = 1」算的");

                // ① 常量 = 原版字面量（`.rdata` 直读：`0x1834b2bb8`/`0x1834b2bb4`/`0x1834b2bc8`/`0x1834b2da8`/
                //    `0x1834b3174`/`0x1834b2ba8`/`0x1834b2db8`；那个 `120` 是 Windows 一格滚轮）
                Check(Mathf.Abs(CombatCameraZoom.OffZoom - 1f) < 1e-9f
                   && Mathf.Abs(CombatCameraZoom.DragSign - (-1f)) < 1e-9f
                   && Mathf.Abs(CombatCameraZoom.ScrollUnitsPerNotch - 120f) < 1e-9f
                   && Mathf.Abs(CombatCameraZoom.OriginalLensShiftSpeed - 10f) < 1e-9f
                   && Mathf.Abs(CombatCameraZoom.LensShiftSnapEpsilon - 1e-7f) < 1e-16f
                   && Mathf.Abs(CombatCameraZoom.ShiftCorrectionEpsilon - 1e-10f) < 1e-19f
                   && Mathf.Abs(CombatCameraZoom.CameraForwardEpsilon - 1e-6f) < 1e-13f,
                      "★ A422：七个常量逐个 = 原版 `.rdata` 里读出来的字面量"
                    + "（`OffZoom 1` · `DragSign −1` · `ScrollUnitsPerNotch 120` · `OriginalLensShiftSpeed 10` ·"
                    + " `LensShiftSnapEpsilon 1e-7` · `ShiftCorrectionEpsilon 1e-10` · `CameraForwardEpsilon 1e-6`）"
                    + " —— `DragSign` 写回 `+1` 这一条就红（拖拽方向会反）");

                // ② 序列化值 = **场景那一档**（判据 = `MonoBehaviour_4404.json`，13/13 场逐字节相同）。
                //    🔴 这条专门盯那**三个「ctor 默认 ≠ 场景值」**：`zoomSpeed`(0.1/12.5) ·
                //       `invertMouseScrollWheel`(false/true) · `dragSensitivity`(0.005/0.01)。
                Check(Mathf.Abs(cz422.ZoomSpeedTest - 12.5f) < 1e-6f
                   && Mathf.Abs(cz422.DragSensitivityTest - 0.01f) < 1e-8f
                   && Mathf.Abs(cz422.SnapBackSpeedTest - 5f) < 1e-6f
                   && Mathf.Abs(cz422.ZoomSensitivityTest - 0.1f) < 1e-8f
                   && Mathf.Abs(cz422.ZoomSensitivityMobileTest - 0.005f) < 1e-9f
                   && cz422.InvertMouseScrollWheelTest,
                      $"★ A422：六个参数取的是**场景那一档**（`zoomSpeed 12.5` · `dragSensitivity 0.01` ·"
                    + $" `snapBackSpeed 5` · `zoomSensitivity 0.1` · `zoomSensitivityMobile 0.005` ·"
                    + $" `invertMouseScrollWheel true`）—— 实得 {cz422.ZoomSpeedTest} / {cz422.DragSensitivityTest} /"
                    + $" {cz422.SnapBackSpeedTest} / {cz422.ZoomSensitivityTest} / {cz422.ZoomSensitivityMobileTest} /"
                    + $" {cz422.InvertMouseScrollWheelTest}；**照 ctor 默认抄（0.1 / 0.005 / false）这一条就红**");
                var wb422 = cz422.WorldBoundsTest;
                Check(Mathf.Abs(wb422.x - (-10.05f)) < 1e-4f && Mathf.Abs(wb422.y - (-8f)) < 1e-4f
                   && Mathf.Abs(wb422.width - 20.115f) < 1e-4f && Mathf.Abs(wb422.height - 15.75f) < 1e-4f,
                      $"★ A422：`worldBounds` = 原版 `(89.95, −8.0, 20.115, 15.75)` 经**我们那次 −100 平移**之后那一档"
                    + $"（实得 ({wb422.x:F3}, {wb422.y:F3}, {wb422.width:F3}, {wb422.height:F3})；"
                    + "平移的判据 = `Editor/BattleScene.cs` 的 `ArenaOriginX = 100` + 场地根 `localPosition.x = −100`）");

                // ---- 临时 rig：几何由本段钉死。用完 `DestroyImmediate`。----
                // ⚠️ 必须在 `try/finally` 外面先把引用拿齐（`UnityEngine.Object` 的「假 null」那一套）
                GameObject rigGo = new GameObject("A422Probe_rig");
                try
                {
                    var rigCamGo = new GameObject("A422Probe_cam");
                    rigCamGo.transform.SetParent(rigGo.transform, false);
                    rigCamGo.transform.localPosition = new Vector3(0f, 0f, -5f);   // t = 5 ⇒ 视野宽 = 41.5×5/28 = 7.411
                    rigCamGo.transform.localRotation = Quaternion.identity;
                    var rigCam = rigCamGo.AddComponent<Camera>();
                    // 光学参数照 `Battle/ArenaSceneState.cs:111-117` 那一组（原版 `BoardCamera` 的那一档）
                    rigCam.orthographic = false;
                    rigCam.usePhysicalProperties = true;
                    rigCam.focalLength = 28f;
                    rigCam.sensorSize = new Vector2(41.5f, 24f);
                    rigCam.gateFit = Camera.GateFitMode.Horizontal;
                    rigCam.aspect = 16f / 9f;

                    var rigAz = rigGo.AddComponent<CombatAutoZoom>();
                    rigAz.boardCamera = rigCam;
                    rigAz.ResetForBattle();          // 出厂档（zoom 回 1、manualCamera 清、vcam 位移放回不缩放那一档）
                    var rigCz = rigAz.CameraZoomForTest;
                    if (rigCz == null)
                    {
                        Check(false, "★ A422：临时 rig 上那件组件**没建出来** ⇒ ③、⑤～⑫ 全验不了");
                    }
                    else
                    {
                        rigAz.Initialize();          // 那三句前提没写进去的那一句：让 `CombatCameraZoom.Initialize` 真的跑一次
                        rigCz.PointerOverUi = () => false;                 // 前提 ①
                        rigCz.FocusedOverride = true;                      // 前提 ②（批处理里必须钉）
                        rigCz.PointerViewportOverride = new Vector2(0.5f, 0.5f);   // 前提 ③

                        // ③ `Initialize` 的**反解**：相机当前那个 `sensorSize.x` 应当反解回 zoom = 1
                        //    （判据 = `CombatCameraZoom__Initialize.c`：`t = Clamp01((camSS.x − ss0.x)/(ss1.x − ss0.x))`，
                        //     而 `FrameSensorSizeX(…, zoom:1)` **恒等** ⇒ `ss1.x == camSS.x` ⇒ `t = 1`）。
                        //    🧨 改坏法：`ss0`/`ss1` 对调 ⇒ `t = 0`（分母变成 `ss0.x − camSS.x`）⇒ 红。
                        Check(Mathf.Abs(rigCz.TargetZoomLevel - 1f) < 1e-6f
                           && Mathf.Abs(rigCz.CurrentZoomLevel - 1f) < 1e-6f
                           && !rigCz.ManualCamera,
                              $"★ A422：`Initialize()` 把开局那个 zoom **反解回 1**（= 我们烘进场景那一档）"
                            + $"（实得 target {rigCz.TargetZoomLevel:F6} · current {rigCz.CurrentZoomLevel:F6} ·"
                            + $" manualCamera {rigCz.ManualCamera}）—— 反解公式里 `ss0`/`ss1` 对调就红");

                        // ④ `CorrectLensShift` **纯函数**（期望值在本文件里自己算：`1 / ((ss × |z|) / focal)`）
                        {
                            float kx = (rigCam.sensorSize.x * Mathf.Abs(rigCamGo.transform.position.z)) / rigCam.focalLength;
                            float ky = (rigCam.sensorSize.y * Mathf.Abs(rigCamGo.transform.position.z)) / rigCam.focalLength;
                            var cs = rigCz.CorrectLensShift(Vector2.zero, new Vector2(1f, 0f));
                            Check(Mathf.Abs(cs.x - 1f / kx) < 1e-7f && Mathf.Abs(cs.y) < 1e-12f
                               && Mathf.Abs(kx - 7.4107143f) < 1e-4f,
                                  $"★ A422：`CorrectLensShift((0,0),(1,0))` = `(1 / ((sensorSize.x × |pos.z|) / focal), 0)`"
                                + $"（本文件独立算：kx = 41.5×5/28 = {kx:F6} ⇒ 期望 {1f / kx:F6}，实得 ({cs.x:F6}, {cs.y:F6})）"
                                + " —— 把 x/y 两个分母对调 ⇒ 红");
                        }

                        // ⑤ `GetCameraFrustumWorldBoundsWithShift(zero)` = 「相机在 `z = 0` 平面上的视野」
                        //    期望矩形在本文件里由**公开的 Unity 相机属性**独立算：
                        //      t = (0 − pos.z)/forward.z · 宽 = sensorSize.x × t / focal ·
                        //      高 = 那个宽 / aspect · 中心 = (pos.x + forward.x·t, pos.y + forward.y·t)
                        //    （本 rig 的 forward = (0,0,1)、right = (1,0,0)、up = (0,1,0) ⇒ 轴对齐矩形）
                        {
                            var ct = rigCamGo.transform;
                            float t5 = (0f - ct.position.z) / ct.forward.z;
                            float w5 = rigCam.sensorSize.x * t5 / rigCam.focalLength;
                            float h5 = w5 / rigCam.aspect;
                            var r5 = rigCz.GetCameraFrustumWorldBoundsWithShift(Vector2.zero);
                            var wantC5 = new Vector2(ct.position.x + ct.forward.x * t5,
                                                     ct.position.y + ct.forward.y * t5);
                            Check(Mathf.Abs(r5.width - w5) < 1e-4f && Mathf.Abs(r5.height - h5) < 1e-4f
                               && Mathf.Abs(r5.center.x - wantC5.x) < 1e-4f && Mathf.Abs(r5.center.y - wantC5.y) < 1e-4f,
                                  $"★ A422：`GetCameraFrustumWorldBoundsWithShift(0)` = 相机在 `z = 0` 平面上的视野矩形"
                                + $"（实得 {r5.width:F4}×{r5.height:F4} @中心 ({r5.center.x:F4},{r5.center.y:F4})；"
                                + $" 独立算 {w5:F4}×{h5:F4} @({wantC5.x:F4},{wantC5.y:F4})）"
                                + " —— 半宽少乘/多乘那个 0.5、或把 `Vector3.forward` 换成别的静态（如 `up`）⇒ 红");
                        }

                        // ⑥ `ClampShiftLimits` 的**早退**：视野已在 `worldBounds` 内 ⇒ **原样返回**。
                        //    🧨 改坏法：把两条判断的**方向**写反（`r.xMin > bounds.xMin` 这种）⇒ 界内时 nx 会被算成
                        //       非零 ⇒ 返回值不再等于入参 ⇒ 红。
                        //    ⚠️ **如实记**：把那个 `1e-10` 的早退**整个删掉**，这一条**不会红**
                        //       （`CorrectLensShift(desired, (0,0))` 恒等于 `desired`，0/kx 就是 0）
                        //       ⇒ `1e-10` 只能靠第 ⑦ 条间接盯（那一档 nx 是真非零）。
                        {
                            var want6 = new Vector2(0.001f, -0.002f);
                            var got6 = rigCz.ClampShiftLimits(want6);
                            var r6 = rigCz.GetCameraFrustumWorldBoundsWithShift(want6);
                            var wb6 = cz422.WorldBoundsTest;
                            bool inside6 = r6.xMin > wb6.xMin && r6.xMax < wb6.xMax
                                        && r6.yMin > wb6.yMin && r6.yMax < wb6.yMax;
                            Check(inside6 && Mathf.Abs(got6.x - want6.x) < 1e-7f && Mathf.Abs(got6.y - want6.y) < 1e-7f,
                                  $"★ A422：`ClampShiftLimits` 的**早退** —— 视野完全在界内时**一个字节都不动**"
                                + $"（入参 ({want6.x},{want6.y}) ⇒ 实得 ({got6.x:F8},{got6.y:F8})；"
                                + $" 视野 x[{r6.xMin:F3},{r6.xMax:F3}] ⊂ 界 x[{wb6.xMin:F3},{wb6.xMax:F3}]）");
                        }

                        // ⑦a `worldBounds` **真夹**（视野比界窄 ⇒ 只一侧出界）：把 `desired.x` 喂大
                        //    ⇒ 返回值的 `.x` **小于**它，且新视野矩形的 **xMax 落回 `worldBounds.xMax`**（1e-4）。
                        {
                            var want7 = new Vector2(1.0f, 0f);
                            var got7 = rigCz.ClampShiftLimits(want7);
                            var r7 = rigCz.GetCameraFrustumWorldBoundsWithShift(got7);
                            var wb7 = cz422.WorldBoundsTest;
                            Check(got7.x < want7.x - 1e-3f && Mathf.Abs(got7.y) < 1e-9f
                               && Mathf.Abs(r7.xMax - wb7.xMax) < 1e-3f
                               && r7.xMin > wb7.xMin && r7.xMax < wb7.xMax + 1e-4f,
                                  $"★ A422：`ClampShiftLimits` **真的在夹** —— `desired.x = {want7.x}` ⇒ 实得 {got7.x:F6}（变小），"
                                + $" 夹完的视野 x[{r7.xMin:F3},{r7.xMax:F3}] 落回界 x[{wb7.xMin:F3},{wb7.xMax:F3}] 内"
                                + " —— 删掉那两条 `if`（不夹）这一条就红；`x/y` 分母对调也红");
                        }

                        // ⑦b 🔴 **那两条 `if` 不是 `else if`**（后者可以覆盖前者）：把视野拉**宽过界**（相机挪到 t = 20
                        //     ⇒ 视野宽 29.64 > 界宽 20.115）⇒ 正负两侧**同时**出界 ⇒ `nx` 最终取的是**后一条**
                        //     （`bounds.xMax − r.xMax`，负）⇒ 修正方向 = **左移**、夹完的 `xMax` 贴回界右沿而 `xMin` 仍在外。
                        //    🧨 改坏法：两条 `if` 改成 `else if` ⇒ 取的是**前一条**（正）⇒ 修正方向反过来 ⇒ 红。
                        {
                            rigCamGo.transform.localPosition = new Vector3(0f, 0f, -20f);   // t = 20 ⇒ 视野宽 29.643
                            var want7b = Vector2.zero;
                            var got7b = rigCz.ClampShiftLimits(want7b);
                            var r7b = rigCz.GetCameraFrustumWorldBoundsWithShift(got7b);
                            var wb7b = cz422.WorldBoundsTest;
                            Check(Mathf.Abs(r7b.width - 29.6429f) < 1e-3f          // 前提：这一档确实是「宽过界」
                               && got7b.x < want7b.x - 1e-3f                        // 修正 = 左移（后一条 if 赢了）
                               && Mathf.Abs(r7b.xMax - wb7b.xMax) < 1e-4f           // 右沿贴回界
                               && r7b.xMin < wb7b.xMin,                             // 左沿仍在外（宽过界的必然）
                                  $"★ A422：`ClampShiftLimits` 两条 `if` **不是 `else if`**（后者覆盖前者）—— "
                                + $"视野宽 {r7b.width:F3} > 界宽 {wb7b.width:F3} ⇒ 两侧同时出界 ⇒ 修正取**后一条**（左移），"
                                + $" 实得 `desired.x` {want7b.x} → {got7b.x:F6}，夹完 xMax = {r7b.xMax:F4} 贴回界右沿 {wb7b.xMax:F4}"
                                + $"、xMin {r7b.xMin:F3} 仍在外 —— **改成 `else if` 这一条就红**（方向会反过来）");
                            rigCamGo.transform.localPosition = new Vector3(0f, 0f, -5f);    // 挪回去
                        }

                        // ⑧ **滚轮改 zoom**（手动档）：`invertMouseScrollWheel = true` ⇒ 符号翻转 ⇒ **减**
                        //    期望：`TargetZoomLevel` 从 1 减到 `1 − zoomSensitivity × 1`（`zoomSensitivity` = 0.1）
                        //    🧨 改坏法：删掉 `invertMouseScrollWheel` 那一跳 ⇒ 方向反 ⇒ 值被上夹在 1 ⇒ 红
                        {
                            rigAz.ResetForBattle();                      // zoom 回 1、manual 回 false
                            rigCz.ManualCamera = true;
                            float z0 = rigCz.TargetZoomLevel;
                            rigCz.InjectPointerSource(+1f, false, Vector2.zero);   // 一格滚轮（原版 legacy 刻度 = ±1）
                            rigCz.TickInjected(0.02f);
                            float z1 = rigCz.TargetZoomLevel;
                            Check(Mathf.Abs(z0 - 1f) < 1e-6f && Mathf.Abs(z1 - (1f - 0.1f)) < 1e-4f
                               && z1 < z0 - 1e-3f,
                                  $"★ A422：滚轮一格 ⇒ zoom **减** `zoomSensitivity × 1 = 0.1`"
                                + $"（{z0:F6} → {z1:F6}；`invertMouseScrollWheel = true` ⇒ 原版那一句 `xorps` 求负）"
                                + " —— 把求负那一跳删掉 ⇒ 方向反、上夹在 1 ⇒ 红");
                        }

                        // ⑨ **右键拖拽平移**：`HandleDrag() = TouchDragDelta × dragSensitivity × (−1)`
                        //    ⇒ 向左拖 10 px ⇒ `(+0.1, 0)`；整帧之后 `virtualCameraLensShift.x` **变大**。
                        //    ⚠️ **量级别想当然**：拖那一帧里 `targetLensShift = vcam + drag = (0.1, …)`，而
                        //    `Vector2.SmoothDamp` 的 `smoothTime = dt × zoomSpeed = 0.02 × 12.5 = 0.25` ⇒
                        //    **一帧只走约 1.1%**（≈ +0.0011，本文件按 Unity 那条公式手算过）⇒ 阈值取 1e-4。
                        //    🧨 改坏法：`DragSign` 改 `+1` ⇒ 两条都红。
                        {
                            rigCz.ManualCamera = true;
                            rigCz.InjectPointerSource(0f, true, new Vector2(-10f, 0f));
                            var drag9 = rigCz.HandleDrag();
                            Check(rigCz.TouchPressedSecondary
                               && Mathf.Abs(drag9.x - 0.1f) < 1e-6f && Mathf.Abs(drag9.y) < 1e-9f,
                                  $"★ A422：右键按着时 `HandleDrag()` = `TouchDragDelta × dragSensitivity × (−1)`"
                                + $"（拖 (−10,0) ⇒ 期望 (0.1, 0)，实得 ({drag9.x:F6}, {drag9.y:F6})）"
                                + " —— 那个 `−1` 是 `.rdata` 直读（`0x1834B2BC8`），改成 `+1` 就红");
                            float x9 = rigCz.virtualCameraLensShift.x;
                            rigCz.TickInjected(0.02f);                   // 整帧：`ApplyZoom` 的手动档那条
                            float x9b = rigCz.virtualCameraLensShift.x;
                            Check(x9b > x9 + 1e-4f,
                                  $"★ A422：整帧之后镜头**真的往正走**（`virtualCameraLensShift.x` {x9:F6} → {x9b:F6}；"
                                + "方向 = 「向左拖 ⇒ 镜头右移」，同 `HandleDrag` 的 `−1`；量级 ≈ +0.0011 = 0.1 × 一帧平滑系数）"
                                + " —— 这一档几何在界内（视野宽 7.41 ≪ 界宽 20.115）⇒ **世界边界不会把结果吃掉**");
                        }

                        // ⑩ **平滑真的在追**（不是一步到位）：先让 target ≠ current（`SetZoomLevel(instant:false)`
                        //    那一档**只写 target**），再连跑两帧 ⇒ current 既不等于 target、又逐帧靠近。
                        //    🧨 改坏法：把 `Mathf.SmoothDamp` 换成直接赋值 ⇒ 第一帧就到位 ⇒ 红。
                        {
                            rigAz.ResetForBattle();       // ⚠️ **先把 current 也放回 1**（`SetZoomLevel(instant:false)` 只写 target，
                                                          //    而上面两帧已经让 current 动过了 —— 不重置的话「前提」那条会红）
                            rigCz.ManualCamera = false;
                            rigCz.SetZoomLevel(0.5f, false, false);
                            bool prem10 = Mathf.Abs(rigCz.TargetZoomLevel - 0.5f) < 1e-6f
                                       && Mathf.Abs(rigCz.CurrentZoomLevel - 1f) < 1e-5f;
                            rigCz.InjectPointerSource(0f, false, Vector2.zero);
                            rigCz.TickInjected(0.016f);
                            float c1 = rigCz.CurrentZoomLevel;
                            rigCz.TickInjected(0.016f);
                            float c2 = rigCz.CurrentZoomLevel;
                            Check(prem10 && Mathf.Abs(c1 - 0.5f) > 1e-3f && Mathf.Abs(c2 - 0.5f) > 1e-3f
                               && Mathf.Abs(c2 - 0.5f) < Mathf.Abs(c1 - 0.5f),
                                  $"（前提）目标 0.5 / 当前还是 1（`instant:false` 只写目标）={prem10}；"
                                + $"★ A422：`SmoothDamp` **真的在追** —— 第一帧 {c1:F6}、第二帧 {c2:F6}（都在朝 0.5 走，"
                                + "两帧都没到位）—— 换成直接赋值 ⇒ 第一帧就 0.5 ⇒ 红");
                        }

                        // ⑪ **手动档 `SettleFraming()` 什么都不写**（原版那一道 `if (manualCamera) return false;`）
                        //    🧨 改坏法：删掉那一句 ⇒ 返回 true 且写相机 ⇒ 红。
                        {
                            rigCz.ManualCamera = true;
                            var ls11 = rigCam.lensShift; var ss11 = rigCam.sensorSize;
                            int ap11 = rigCz.FramingApplyCount;
                            bool wrote11 = rigCz.SettleFraming();
                            Check(!wrote11
                               && Mathf.Abs(rigCam.lensShift.x - ls11.x) < 1e-9f
                               && Mathf.Abs(rigCam.lensShift.y - ls11.y) < 1e-9f
                               && Mathf.Abs(rigCam.sensorSize.x - ss11.x) < 1e-9f
                               && Mathf.Abs(rigCam.sensorSize.y - ss11.y) < 1e-9f
                               && rigCz.FramingApplyCount == ap11,
                                  $"★ A422：手动档下 `SettleFraming()` 返回 {wrote11}、相机 `lensShift`/`sensorSize` **一个字节都没变**、"
                                + $" `FramingApplyCount` 也没涨（{ap11}）—— 原版那道 `if (manualCamera) return false;`"
                                + " —— 删掉它这一条就红");
                        }

                        // ⑫ `SetZoomLevel(force:true)` **清手动档**（原版 `manualCamera = false`），
                        //    而且清完之后 `SettleFraming()` 会真的写（两件叠起来的落点）。
                        //    🧨 改坏法：删掉 `manualCamera = false` ⇒ 两条都红。
                        {
                            rigCz.ManualCamera = true;
                            rigCz.SetZoomLevel(0.25f, false, true);
                            Check(!rigCz.ManualCamera && Mathf.Abs(rigCz.TargetZoomLevel - 0.25f) < 1e-6f,
                                  $"★ A422：`SetZoomLevel(force:true)` 清掉手动档并写新的目标"
                                + $"（manualCamera {rigCz.ManualCamera} · zoom {rigCz.TargetZoomLevel:F6}）");
                            int ap12 = rigCz.FramingApplyCount;
                            Check(rigCz.SettleFraming() && rigCz.FramingApplyCount > ap12,
                                  $"★ A422：……清完之后 `SettleFraming()` **真的会写**（`FramingApplyCount` {ap12} → "
                                + $"{rigCz.FramingApplyCount}）—— 手动档那道闸 + 落点那一路**两件叠起来**才对得上原版");
                        }
                    }
                }
                finally
                {
                    // ⚠️ 先把静默事件摘掉（`OnEnable` 在批处理里跑不跑本工程没定论，摘一次是幂等的），再销毁
                    var rigAzRef = rigGo.GetComponent<CombatAutoZoom>();
                    if (rigAzRef != null) rigAzRef.DetachMinionEvent();
                    // 🔴 2026-10-14（#8 顺手堵住 = **A809**）：**那条静态分辨率信号也必须摘**。
                    //   `CombatAutoZoom.Initialize()` 会建出 `BattleCameraSreenSize` **并**注册那条静态信号
                    //   （调用点 `Battle/CombatCameraZoom.cs:360` → 方法 `Battle/BattleCameraSreenSize.cs:172`）
                    //   —— 这里不摘
                    //   ⇒ 静态事件上留一个**指向已销毁组件**的委托：抬一次信号就冒一条
                    //   `[BattleCameraSreenSize] \`Initialize\` 的 combatCameraZoom 与 cameraVerticalFramer
                    //   两个都 没接上…`（`battle.log:40233` 就是这么来的），而且 `_refsMissingNoted` 把它闩住
                    //   ⇒ 之后再也看不见（**静默污染**，`D1013` §五·1 与本段 A463 各自独立查到）。
                    var rigCzRef = rigAzRef != null ? rigAzRef.CameraZoomForTest : null;
                    if (rigCzRef != null && rigCzRef.battleCameraScreenSize != null)
                        rigCzRef.battleCameraScreenSize.UnregisterResolutionSignal();
                    UnityEngine.Object.DestroyImmediate(rigGo);
                }

                // 收尾：把真那一件放回**不缩放**那一档（后面只剩 A388/A423，别再让相机停在歪的地方）。
                AutoZoom.RestoreForTest(false, false);
                az422.ForceRefresh();
            }
        }

        // ---------------- ★ A423（2026-10-12）：HUD 那颗「重置自动镜头」钮 ----------------
        // 判据（全是实读）→ `BattleDriver.ToggleCameraResetButton` 的注释（节点 `CenterCameraButton` ·
        //   `BattleHud.resetCameraZoomButton`(+0xa8) · 显隐三处调用点 · 点击链 · `.rdata` 三个 punch 常量）。
        // 🧨 改坏法：① 图名/尺寸写错 ⇒ 第 1/2/3 条红；② 建完不 `SetActive(false)` ⇒ 第 4 条红；
        //   ③ `SetupAutoZoom` 里不接 `ToggleResetCameraZoomUi` ⇒ 第 5/6 条红（钮永不出现）；
        //   ④ 命中区拿 `ImageQuad.Contains`（画出来的 61.846）顶替原版那个 48.443×45.846 ⇒ 第 7 条红；
        //   ⑤ 点击不 Invoke 那条 `Action`（自己另写一次重算）⇒ 第 8 条红。
        {
            Check(driver.CameraResetButtonBuilt && driver.CameraResetButtonArt == "40k_UI_bt_center_camera",
                  $"★ A423：HUD 上那颗「重置自动镜头」钮建出来了，而且就是原版那张图"
                + $"（原版 `BattleHud.resetCameraZoomButton`(+0xa8) / 节点 `CenterCameraButton` / sprite `40k_UI_bt_center_camera`；"
                + $" 实得 `{driver.CameraResetButtonArt}`）");
            var bd423 = driver.CameraResetButtonDrawnPx;
            Check(Mathf.Abs(bd423.x - 61.846f) < 0.05f && Mathf.Abs(bd423.y - 61.846f) < 0.05f
               && bd423.x < 64.4429f,
                  $"★ A423：画出来的是**内接**进原版那个 rect 的 61.846×61.846（原版 `m_PreserveAspect = 1`）——"
                + $" 实画 {bd423.x:F3}×{bd423.y:F3}（rect = 64.443×61.846）"
                + " —— 丢掉 PA 那一档（按 rect 拉伸成 64.443×61.846）这一条就红");

            // 几何：px 原值（判据 = `RectTransform_3487` 的 `anchoredPosition (0.150757, −59.097)`
            //   + 父 `Left Anchor` 那宽 100 的整列 ⇒ 中心在 1920×1080 画布上的 **(50.1508, 599.097)** 处
            //   —— y **从上往下**数（即 `HudAbs` 的口径），换算成 `HudAbs` 的左上角口径 = **(17.9293, 568.174)**）。
            //   🔴 **2026-10-12 订正（A423 收红那轮）**：这句原来把画布中心写成 `(50.1508, 480.903)` ——
            //     **值对、标签错**：`480.903 = 1080 − 599.097` 是**翻转后**的那个数，而 `LayoutSpace.ToWorld`
            //     的 `y01`（相对可见区、y 从**下**）收的恰好就是它（`BattleDriver.HudAbs` 里
            //     `cy = 1 − (y + h/2)/1080`）⇒ 下面 `480.903f / 1080f` **一个字都不能改**，
            //     改了文案就够（照 480.903 去「修」代码会把方向改反）。
            //   期望值 = 原版字面量换算出来的（⛔ 不读 `BuildHud` 传的那几个数）。
            //   ⚠️ 过 `hudRoot.TransformPoint` 与建它那一路**逐字同一条换算**（`ImageQuad.Create` 写的也是
            //     `root` 下的 `localPosition`）⇒ 不假设 `HudRoot` 自己在原点。
            {
                Vector3 want423 = driver.hudRoot != null
                    ? driver.hudRoot.TransformPoint(LayoutSpace.ToWorld(50.1508f / 1920f, 480.903f / 1080f))
                    : LayoutSpace.ToWorld(50.1508f / 1920f, 480.903f / 1080f);
                var d423 = driver.CameraResetButtonWorldPos - want423;
                Check(Mathf.Abs(d423.x) < 1e-4f && Mathf.Abs(d423.y) < 1e-4f,
                      $"★ A423：那颗钮落在**原版那个位置**上（中心 = `LeftAnchor` 中心 + (0.150757, −59.097) px"
                    + $" ⇒ 画布 (50.1508, 599.097)，y 从上往下数）—— 实得偏 ({d423.x:F4}, {d423.y:F4}) 世界单位"
                    + "（阈值 1e-4 世界单位 ≈ 0.011 px；同一件在 16 那一节还有一条「中心 ≈ (50.15, 599.1)」的粗口径）"
                    + " —— 🧨 改坏法：`BattleDriver.BuildHudExtras` 里那四个 px 写成取整值"
                    + "（`17.9 / 568.2 / 64.44 / 61.85`）⇒ 中心偏 0.0308 / 0.028 px = 2.9e-4 / 2.6e-4 世界单位"
                    + " ⇒ 本条红（2026-10-12 之前就是这个状态）");
            }

            Check(!driver.CameraResetButtonVisible,
                  "★ A423：**开局它是关着的**（原版 `BattleHud.Initialize` 里那句 `SetActive(false)`）"
                + " —— 建完不关（让它一直亮着）这一条就红");
            Check(driver.CameraResetHookWired,
                  "★ A423：显隐钩子接上了（原版 `BattleHud.ToggleResetAutoCameraZoom` 那一条 = "
                + "`BattleDriver.SetupAutoZoom` 里接的 `ToggleCameraResetZoomUi`）—— 不接的话那三处只会出声、钮永不出现");
            Check(!driver.CameraResetButtonHit(driver.CameraResetButtonWorldPos),
                  "★ A423：**关着的时候点不着**（原版 `SetActive(false)` 的节点收不到射线）");

            // 走**真那条路**让它出现：原版 `CombatCameraZoom.LateUpdate` 里
            //   `if (ScrollDelta != 0 || TouchPressedSecondary) BattleHud.Instance.ToggleResetAutoCameraZoom(allowManualControl);`
            var cz423 = driver.AutoZoom != null ? driver.AutoZoom.CameraZoomForTest : null;
            if (cz423 == null)
            {
                Check(false, "★ A423：拿不到 `CombatCameraZoom` ⇒ 「玩家一动镜头 ⇒ 钮出现」那两条验不了");
            }
            else
            {
                cz423.ToggleAllowManualControl(true);
                cz423.ManualCamera = false;
                cz423.PointerOverUi = () => false;
                cz423.FocusedOverride = true;
                cz423.PointerViewportOverride = new Vector2(0.5f, 0.5f);
                int punch423 = driver.CameraResetPunchCount;
                cz423.InjectPointerSource(+1f, false, Vector2.zero);      // 滚一格
                cz423.TickInjected(0.02f);
                Check(driver.CameraResetButtonVisible && cz423.ManualCamera,
                      "★ A423：**玩家一滚轮 ⇒ 那颗钮就出现**，而且镜头进了手动档"
                    + $"（原版同一条 `if` 里两句挨着：`ToggleResetAutoCameraZoom(allowManualControl)` + `manualCamera = allowManualControl`；"
                    + $" 实得 显示 {driver.CameraResetButtonVisible} · manualCamera {cz423.ManualCamera}）");
                Check(driver.CameraResetPunchCount == punch423 + 1,
                      $"★ A423：出现那一下**弹了一次**（原版 `DOPunchScale(Vector3.one × 0.2, 0.5s, vibrato 10, elasticity 1.0)`；"
                    + $" 计数 {punch423} → {driver.CameraResetPunchCount}）");

                // 命中区 = 原版那个**带 `m_RaycastPadding` 的**矩形（48.443×45.846），⛔ 不是画出来的 61.846 正方形
                var c423 = driver.CameraResetButtonWorldPos;
                Check(driver.CameraResetButtonHit(c423),
                      "★ A423：钮中心那一击**打得中**（真实输入与自检走同一条判定）");
                float off423 = 25f / 108f;      // 25 px：在画出来的 61.846 里、在命中的 45.846 里**都算内**，只有原版那个更窄的矩形能分辨
                var near423 = c423 + new Vector3(off423, 0f, 0f);
                Check(!driver.CameraResetButtonHit(near423),
                      $"★ A423：偏中心 25 px（x 向）那一点**打不中** —— 原版 `Image.m_RaycastPadding = (−8,−8,−8,−8)`"
                    + $"（负 = 往里缩）⇒ 命中矩形是 **48.443×45.846**（半宽 24.22 px），不是画出来的那个 61.846 正方形"
                    + $"（拿 `ImageQuad.Contains` 顶替 ⇒ 这一条红）");

                // 点它 ⇒ 原版那条链：`BattleHud.DoResetCameraZoom()` → Invoke `ResetCameraZoom`(+0xc0)
                //   → `CombatAutoZoom.ResetCameraZoomUIAction()` → `SetZoomLevel(Max(敌,我), force:true)`
                //   ⇒ 清手动档 + 重算取景 + **把本钮自己收起来**。
                int apply423 = driver.AutoZoom.FramingApplyCount;
                bool ok423 = driver.ResetCameraZoomClick();
                Check(ok423 && !cz423.ManualCamera && !driver.CameraResetButtonVisible
                   && driver.AutoZoom.FramingApplyCount > apply423,
                      $"★ A423：点它 ⇒ 镜头**被重置**（手动档清掉 {cz423.ManualCamera} · 钮收回 {driver.CameraResetButtonVisible} ·"
                    + $" 取景重算了 {apply423} → {driver.AutoZoom.FramingApplyCount}）"
                    + " —— `force:true` 那一支不调 `RaiseToggleResetCameraZoomUi(false)` 钮就收不回去 ⇒ 红");

                // 收尾：开关放回关、取景回到不缩放那一档（同 A175 那一段的收尾口径），
                //   并把那三个自检口放开（不让它们留在生产语义上）
                AutoZoom.RestoreForTest(false, false);
                driver.AutoZoom.ForceRefresh();
                cz423.FocusedOverride = null;
                cz423.PointerViewportOverride = null;
                cz423.PointerOverUi = null;
            }
        }

        // ---------------- 🆕 2026-10-12（A388，W4 备好的原文照贴）：离场把静态钩子摘干净 ----------------
        // ⚠️ **本段测的是「摘」这个方法本身**（`DetachStaticHooks` + `StaticHookCount` 两个自检口）。
        //    🔴 **把 `OnDestroy` 里那句 `DetachStaticHooks();` 删掉，本段(a)照样全绿** ⇒ 「`OnDestroy` 有没有调它」
        //    这半**只能靠代码审查**（`BattleDriver.OnDestroy` 里就一句，`Editor` 侧看一眼即可）——
        //    **如实记，不假称验过**；下面 (b) 那半（真 `DestroyImmediate`）能盖住「生命周期没走到」那一类。
        // 🆕 **2026-10-13（A515）**：这个基线提到块外 —— 下面 (b) 段要拿它当**同一次运行的基线**
        //   （断言 = 「二次 `Begin()` 接回的条数 == 首次 `Begin()` 之后的条数」）。
        //   🔴 **为什么不能写死 19**：那 19 槽里 `WFModulePostProcess.OnPostFx` 只在「载进来的战场
        //   prefab 里有 `Volume`」时挂得上（`AttachPostFx` 取不到 Volume 就**出声并保持 null**）
        //   ⇒ 换个环境它可能少一条，写死数会把那种环境误报成红。用同一次运行的实测基线就没有这个问题。
        int hookedBefore388 = BattleDriver.StaticHookCount;
        {
            // 🔴 **2026-10-14（A532）给下面那个 `16` 补出处**：`Begin()` 之后 **现读是 19 条**
            //   （`Unity/_tmp_view/battle.log:39110` 逐字：「A388：开局后静态钩子挂着 19 条」——
            //    与上面 A515 那段注释里说的「那 19 槽」一致）。这里写 **16 只是下限**、**故意不写 19**，
            //   理由见上一段：`AttachPostFx` 取不到 `Volume` 时 `OnPostFx` 会**保持 null**
            //   ⇒「钩子数」是**环境相关**的，写死真值会把「换了个战场 / 换了个环境」误报成红。
            //   （原来那个 16 离真值更远，读起来更像「实测值」，所以补这一句把它标成下限。）
            // 🔑 **通则（以后写这类断言一律照它）**：先问「这一组数是**谁挂的**、有没有哪个挂点**带条件**」——
            //   只要有一个带条件（这里是 `Volume`），就 ⛔ **别写死数**：要么用**同一次运行的基线**
            //   （如 A515 的 `hookedBeforeB == hookedBefore388`），要么用**下限 + 点名那几条环境无关的**
            //   （如紧下面那条「账上点名的三条在」）。出处 = `资料/普查产出_1013/批次计划_1013.md` §六·B（A532）。
            Check(hookedBefore388 >= 16,
                  $"A388：开局后静态钩子挂着 {hookedBefore388} 条"
                + "（挂点 = `HookAnimFxShake/Cards` · `BuildHud` 的两个补间口 · 后期 · 录像）");
            Check(WarpforgeVFX.WFEffectCards.Resolver != null
                  && WarpforgeVFX.WFModuleCollisions.ColliderLookup != null
                  && WarpforgeVFX.WFModuleScaleByTarget.MinionLines != null,
                  "A388：账上点名的那三条在（`WFEffectCards.Resolver` / `Collisions.ColliderLookup` / `ScaleByTarget.MinionLines`）");
            int removed388 = driver.DetachStaticHooks();
            Check(removed388 == hookedBefore388 && BattleDriver.StaticHookCount == 0,
                  $"★ A388：离场把静态钩子**摘干净**（摘前 {hookedBefore388} → 摘掉 {removed388} → "
                + $"现存 {BattleDriver.StaticHookCount}）—— 旧代码一条都不摘（现存 ≥ 3，场景重载后还指着已销毁的 driver）");
            Check(WarpforgeVFX.WFEffectCards.Resolver == null
                  && WarpforgeVFX.WFModuleCollisions.ColliderLookup == null
                  && WarpforgeVFX.WFModuleScaleByTarget.MinionLines == null,
                  "★ A388：账上点名的那三条也在其中（都成 null 了）");
            // ⚠️ 这一段**会把钩子摘光**（上面 A175 那一段已经用完 VFX 了，所以放在它之后没问题）；
            //    若将来还要在这之后用 VFX，得先 `driver.Begin(...)` 把这一场接回来。
        }

        // ---------------- 🆕 2026-10-12（A388 的 b 半）：**真卸载**那一路（更硬）----------------
        // 🔴 为什么在 `CheckSavedScene` **之前**（W4 原文说「放整轮自检的最后」—— 这里必须把它读准）：
        //    `CheckSavedScene` 里第一句就是 `EditorSceneManager.OpenScene(...)`，那会把**当场建的**这份场景
        //    整个卸掉 ⇒ 之后 `driver` 已经没了（本条要的 `driver.Begin(...)` 与 `DestroyImmediate(driver)`
        //    两件都做不到）。所以「整轮的最后」= **打开存档场景之前**。
        //    ⚠️ **2026-10-12 订正**：这里原来还写着「⇒ `driver` 的 `OnDestroy` **已经跑过了**
        //    （`StaticHookCount` 早就是 0）⇒ 再断一次就是**恒真的假断言**」—— **那个前提是错的**
        //    （编辑模式不派 `OnDestroy`，判据见下面 (b) 段）：放它之后**不会**恒真，而会**红**。
        //    结论不变，理由是上面那句。
        {
            // 🔴 **先把这一场接回来，否则这一条是恒真的假断言**：上面 (a) 已经把计数清成 0
            //    ⇒ 不接回来就是「0 → 销毁 → 还是 0」，什么都没验。`Begin()` 里会重新挂
            //    `HookAnimFxShake/HookAnimFxCards` 那一串（W4 §三 的注脚也是这么写的）。
            driver.Begin("Ultramarines", "Goff", 20260915);
            if (driver.InMulligan) driver.SimulateMulliganDone();
            // ⬇⬇ 2026-10-14（#9 · A462 搬家）：这一段原来在整轮末尾（`DestroyImmediate(driver)` **之后**）
            //    ⇒ 进段时 `Object.FindObjectOfType<BattleDriver>()` 恒为 null（**前提不成立**）⇒ 整段一条都验不了。
            //    搬到这儿（`driver.Begin(...)` + `SimulateMulliganDone()` 之后）：driver 活着、HUD 已建、开局已走完。
            //    ⚠️ 本段自己 try/finally 收尾、**一处 `Hook*` 调用都没有**（只用 `FindObjectOfType` 与 `RefreshAll()`；
            //    挂/摘静态钩子的口只在 `BattleDriver.Begin()` / `DetachStaticHooks()` 里）⇒ 下面 `hookedBeforeB` 不受影响。

            // ---------------- 🆕 2026-10-13（A462 + A658）：输入的【两条沿】· 选择器的入场驱动量 ----------------
            // 逐处判据（15 处代码调用，一处一档）→ `资料/普查产出_1013/WA462_输入入口分类.md` §三 / §四。
            // 组件级判据（悬停那三格状态机）→ `d:/2/tools/decomp_full/`：
            //   `CardDisplayAttackTypeButton__{Update,Toggle,MoveButton}.c` +
            //   `...__UnityEngine.EventSystems.IPointerEnter/ExitHandler.{OnPointerEnter,OnPointerExit}.c` +
            //   `..._<StartSafeTouch>d__37__MoveNext.c` · `AttackTypesButtonsController__{MoveButtons,OnEnable}.c`
            // uGUI 那一侧的**根判据**（本机自带源码 `com.unity.ugui`）：
            //   `UI/Core/Button.cs:110-116`（`OnPointerClick` → `m_OnClick`）·
            //   `UI/Core/Selectable.cs:1201-1212`（`OnPointerDown` **不**触发 `onClick`）·
            //   `EventSystem/InputModules/StandaloneInputModule.cs:208-217`（`IPointerClickHandler` 在
            //   `ReleaseMouse()` = 松手那一帧里执行）· `EventSystem/EventTriggerType.cs:24`（`PointerDown = 2`）
            // 🧨 主改坏法（逐条附在断言上）：① 两条沿合并回一个「按下沿」latch；
            //   ② 悬停那条去掉 0.1s 安全窗；③ `_pressCaptured` / `_swallowNextRelease` 删掉
            //   （滑块拖完 / 模态被按下关掉之后会**穿透**）；④ 入场动画改回按时间驱动。
            {
                var wb4 = Object.FindObjectOfType<BattleDriver>();
                Check(wb4 != null, "★ A462：（前提）场上有一台 `BattleDriver` —— 两条沿的判据全挂在它身上");
                if (wb4 != null)
                {
                    // 收尾状态（本段动过的东西一律放回去）
                    var sp4 = wb4.Settings;
                    bool spWasOpen4 = sp4 != null && sp4.Visible;
                    bool azWas4 = AutoZoom.Enabled, azChosen4 = AutoZoom.ChosenManually;
                    try
                    {
                        // ============================================================
                        //  一、两条沿本身（12 处「改抬起」共用的那**一个**机制）
                        // ============================================================
                        // 手法：`PointerHeldForTest` 钉死「按住 / 松手」，`PollInputEdgesForTest()`
                        //       走一次「一帧」。⚠️ 批处理里 `Mouse.current == null` ⇒ 不钉死的话
                        //       `PointerHeld()` 恒 false、**两条沿一条都验不了**。
                        BattleDriver.PointerHeldForTest = true;
                        wb4.PollInputEdgesForTest();                      // 第 1 帧：按下
                        Check(!wb4.ReleasedThisFrameForTest(),
                              "★ A462：（按下那一帧）**不给松手沿** —— 12 处改抬起之后，按住那一下不许触发"
                            + "（改坏法：把 `_upEdge` 换成「按住」⇒ 这条红）");
                        Check(wb4.ClickedThisFrameForTest(),
                              "★ A462：（同一帧）按下沿照旧给 —— `ClickedThisFrame` 的语义一个字没改");
                        Check(!wb4.ClickedThisFrameForTest(),
                              "★ A462：按下沿**一次按住只给一次**（原来的 latch 语义；改坏法：去掉 `_downEdge = false`）");

                        wb4.PollInputEdgesForTest();                      // 第 2 帧：还按着
                        Check(!wb4.ClickedThisFrameForTest() && !wb4.ReleasedThisFrameForTest(),
                              "★ A462：（按住不放的第 2 帧）**两条沿都不给** —— 按住不放不会每帧触发");

                        BattleDriver.PointerHeldForTest = false;
                        wb4.PollInputEdgesForTest();                      // 第 3 帧：松手
                        Check(!wb4.ClickedThisFrameForTest(),
                              "★ A462：（松手那一帧）**不给按下沿** ← 这就是「按下 vs 抬起」两态的分界线");
                        Check(wb4.ReleasedThisFrameForTest(),
                              "★ A462：（松手那一帧）**给松手沿** ← 12 处改的就是它");
                        Check(!wb4.ReleasedThisFrameForTest(),
                              "★ A462：松手沿**一次按住只给一次**（改坏法：去掉 `_upEdge = false`）");
                        wb4.PollInputEdgesForTest();                      // 第 4 帧：还松着
                        Check(!wb4.ReleasedThisFrameForTest(),
                              "★ A462：松手之后**不会补出第二次松手沿**（边沿是帧的状态、不滞留）");

                        // ============================================================
                        //  一·b、`ClickLog`：**两条沿各记一次**（用户 2026-09-24 要的「真实点击记录」）
                        //     判据 = 纪律 1：`ClickedThisFrame` 原来兼着 `ClickLog.Begin/Hit`，
                        //     改沿时**两支都要记**，否则改到抬起的 12 处**一条记录都不会有**。
                        //     ⚠️ 探针写 `_tmp_view/battle/` 下的临时文件（⛔ 不碰玩家那份真日志），
                        //        做法同 `Editor/ShellScene.cs:3274-3288`。
                        // ============================================================
                        {
                            var probePath4 = Path.Combine(OutDir, "_wb4_click_probe.txt");
                            if (File.Exists(probePath4)) File.Delete(probePath4);
                            ClickLog.OverridePath = probePath4;
                            BattleDriver.PointerWorldForTest = Vector3.zero;
                            BattleDriver.PointerHeldForTest = true;
                            wb4.PollInputEdgesForTest();
                            wb4.ClickedThisFrameForTest();
                            ClickLog.End();
                            string blkDown4 = ClickLog.LastBlock ?? "";
                            BattleDriver.PointerHeldForTest = false;
                            wb4.PollInputEdgesForTest();
                            wb4.ReleasedThisFrameForTest();
                            ClickLog.End();
                            string blkUp4 = ClickLog.LastBlock ?? "";
                            ClickLog.OverridePath = null;
                            Check(blkDown4.Contains("来源 BattleDriver"),
                                  "★ A462：**按下沿**那一下进了「真实点击记录」（`ClickLog.Begin/Hit` 还在）");
                            Check(blkUp4.Contains("来源 BattleDriver"),
                                  "★ A462：**松手沿**那一下**也**进了同一份记录"
                                + "（改坏法：只在按下那一支调 `ClickLog` ⇒ 改到抬起的 12 处全都查不到记录，"
                                + "而画面看上去一切正常 = 典型静默）");
                        }

                        // ============================================================
                        //  二、`_pressCaptured`：被滑块接住的这一次按住，松手那一帧**不算点击**
                        //     （原版 uGUI 靠 `StandaloneInputModule.ProcessDrag` 把 `eligibleForClick`
                        //      清掉来做同一件事 —— 拖完滑块不许顺带按到别的钮）
                        // ============================================================
                        if (sp4 != null)
                        {
                            sp4.Show();
                            var ms4 = sp4.SliderAt(0);
                            Check(ms4 != null, "★ A462：（前提）设置面板上第 1 根滑块在（不然下面这条等于没验）");
                            if (ms4 != null)
                            {
                                // 探针取**手柄当前的位置**（一定是命中区里的一点）；值可能被这一按挪动，
                                // 收尾用 `SetValue(..., fire:false)` 放回去（同 14c 那组的做法）。
                                float mv0 = ms4.Value;
                                BattleDriver.PointerWorldForTest = ms4.HandleWorldPos;
                                BattleDriver.PointerHeldForTest = true;
                                wb4.PollInputEdgesForTest();
                                wb4.TickSettingsInputForTest();            // 按下：滑块接住这一按
                                Check(wb4.PressCapturedForTest,
                                      "★ A462：（前提）滑块接住了这一次按住（`SettingsPanel.PointerFrame` 返回 true）");
                                BattleDriver.PointerHeldForTest = false;
                                wb4.PollInputEdgesForTest();
                                wb4.TickSettingsInputForTest();            // 松手
                                Check(!wb4.ReleasedThisFrameForTest(),
                                      "★ A462：被滑块接住的这一次按住 —— **松手那一帧不算点击**"
                                    + "（改坏法：删掉 `_pressCaptured` ⇒ 拖完音量条会在松手那一下顺带点掉别的钮）");
                                ms4.SetValue(mv0, false);                  // 还原（不 fire）
                            }
                            sp4.Hide();
                        }

                        // ============================================================
                        //  三、设置钮 / 设置面板里那颗 Auto Zoom：#5 #6（按下不动、松手才动）
                        // ============================================================
                        if (sp4 != null)
                        {
                            sp4.Hide();
                            BattleDriver.PointerWorldForTest = wb4.HudButtonWorldPosForTest("settings");
                            Check(wb4.HudButtonWorldPosForTest("settings") != Vector3.zero,
                                  "★ A462：（前提）拿得到设置钮的世界坐标（拿不到 `WorldPointer()` 就压不着它）");
                            BattleDriver.PointerHeldForTest = true;
                            wb4.PollInputEdgesForTest();
                            wb4.TickSettingsInputForTest();
                            Check(!sp4.Visible,
                                  "★ A462：设置钮**按下那一帧不弹面板**"
                                + "（原版 `BattleHud.settingsButton` = `EverguildButton`，`BattleHud__Awake.c:48-55` 绑 `m_OnClick`）");
                            BattleDriver.PointerHeldForTest = false;
                            wb4.PollInputEdgesForTest();
                            wb4.TickSettingsInputForTest();
                            Check(sp4.Visible, "★ A462：……**松手那一帧才弹**（改坏法：换回 `ClickedThisFrame()` ⇒ 前一条红）");

                            // 面板里那颗 `Auto Zoom`（原版 `EverguildToggle` ⇒ `IPointerClickHandler`）
                            Check(sp4.AutoZoomRowBuilt, "★ A462：（前提）`Auto Zoom` 那一行建出来了（不然下面两条等于没验）");
                            bool az0 = AutoZoom.Enabled;
                            BattleDriver.PointerWorldForTest = sp4.AutoZoomBoxWorldPos;
                            BattleDriver.PointerHeldForTest = true;
                            wb4.PollInputEdgesForTest();
                            wb4.TickSettingsInputForTest();
                            Check(AutoZoom.Enabled == az0,
                                  $"★ A462：面板里那颗 `Auto Zoom` —— **按下那一帧不翻**（实得 {AutoZoom.Enabled}，期望还是 {az0}）");
                            BattleDriver.PointerHeldForTest = false;
                            wb4.PollInputEdgesForTest();
                            wb4.TickSettingsInputForTest();
                            Check(AutoZoom.Enabled != az0,
                                  $"★ A462：……**松手那一帧才翻**（实得 {AutoZoom.Enabled}）—— 与 Resign/Difficulty/Close 同一条链");
                            AutoZoom.RestoreForTest(azWas4, azChosen4);         // 内存态放回去（本段全程 PersistOverride）
                            sp4.Hide();
                        }

                        // ============================================================
                        //  四、`#11` 与 `#12`：**同一次实验里两条沿并存**（日志面板 / 日志钮）
                        //     · 关面板 = 按下沿（原版 `shade` 的 `EventTrigger`，`eventID 2 = PointerDown`）
                        //     · 开面板 = 松手沿（原版 `ShowCemeteryBtn` = `EverguildButton`）
                        // ============================================================
                        var log4 = wb4.BattleLog;
                        Check(log4 != null, "★ A462：（前提）战斗日志面板建出来了");
                        if (log4 != null)
                        {
                            if (log4.Visible) log4.Hide();
                            var cemeteryW = wb4.HudButtonWorldPosForTest("cemetery");
                            BattleDriver.PointerWorldForTest = cemeteryW;
                            Check(cemeteryW != Vector3.zero, "★ A462：（前提）拿得到日志钮的世界坐标");
                            BattleDriver.PointerHeldForTest = true;
                            wb4.PollInputEdgesForTest();
                            wb4.TickLogInputForTest();
                            Check(!log4.Visible,
                                  "★ A462：日志钮**按下那一帧不开**（原版 `ShowCemeteryBtn` 的 `m_OnClick → ShowCemeteryLogBtn`）");
                            BattleDriver.PointerHeldForTest = false;
                            wb4.PollInputEdgesForTest();
                            wb4.TickLogInputForTest();
                            Check(log4.Visible,
                                  "★ A462：……**松手那一帧才开**（改坏法：换回 `ClickedThisFrame()` ⇒ 前一条红）");
                            // 面板开着 ⇒ 同一处（`shade` 盖满全屏）那一下**仍然按按下沿关**
                            BattleDriver.PointerHeldForTest = true;
                            wb4.PollInputEdgesForTest();
                            wb4.TickLogInputForTest();
                            Check(!log4.Visible,
                                  "★ A462：日志面板**按下那一帧就收**（原版 `shade` = `EventTrigger`，"
                                + "`eventID 2 = PointerDown`；判据 `EventTriggerType.cs:24`）"
                                + " —— ⚠️ 换成松手沿 = 与原版不符，也**红**");
                            // 吞并自证：**同一次按住**的松手沿不许把它又打开（指针就在日志钮上）
                            BattleDriver.PointerHeldForTest = false;
                            wb4.PollInputEdgesForTest();
                            wb4.TickLogInputForTest();
                            Check(!log4.Visible,
                                  "★ A462：……同一次按住的**松手沿不会把它又打开**（改坏法：删掉 `_swallowNextRelease`"
                                + " ⇒ 画面闪一下、看着像「点了没反应」）");
                        }

                        // ============================================================
                        //  五、`#10`：`ChatPopup` 那一处的**两半**（条外关闭按「按下」/ 选台词按「松手」）
                        // ============================================================
                        var pop4 = wb4.ChatPopup;
                        Check(pop4 != null && pop4.Ready, "★ A462：（前提）`ChatPopup` 建起来了");
                        if (pop4 != null && pop4.Ready)
                        {
                            // ① 条外关闭：拿「`ChatButton` 那一颗」当条外的一点（实测它在面板矩形外 ——
                            //    面板 x 77.2..680.8 / y 481.9..777.3，那颗钮在 (50.9, 880)）
                            pop4.Show();
                            BattleDriver.PointerWorldForTest = wb4.HudButtonWorldPosForTest("chat");
                            BattleDriver.PointerHeldForTest = true;
                            wb4.PollInputEdgesForTest();
                            wb4.TickChatInputForTest();
                            Check(!pop4.Visible,
                                  "★ A462：`ChatPopup` 的**条外关闭**判的是**按下那一帧**"
                                + "（原版 `CloseChatPopup` = `EventTrigger` `eventID 2 = PointerDown`）");
                            BattleDriver.PointerHeldForTest = false;
                            wb4.PollInputEdgesForTest();
                            wb4.TickChatInputForTest();
                            Check(!pop4.Visible,
                                  "★ A462：……同一次按住的**松手沿不会把它又打开**（指针就压在那颗 `ChatButton` 上；"
                                + "改坏法：删掉 `_swallowNextRelease` ⇒ 这条红）");

                            // ② 选台词：按在钮上**不说话**，松手才说
                            pop4.Show();
                            var r4 = pop4.ButtonRect(0);
                            BattleDriver.PointerWorldForTest =
                                LayoutSpace.ToWorld((r4.x + r4.width * 0.5f) / 1920f,
                                                    1f - (r4.y + r4.height * 0.5f) / 1080f);
                            int clicked0 = pop4.LastClicked;
                            BattleDriver.PointerHeldForTest = true;
                            wb4.PollInputEdgesForTest();
                            wb4.TickChatInputForTest();
                            // ⚠️ 比的是「**没有变成 0**」而不是「等于上一帧那个值」—— 按下那一帧
                            //    `PointerDownAt` 会把 `LastClicked` 归 -1（一次新的点击开始了），
                            //    那是它本来就有的语义（见 `ChatPopupPanel.PointerDownAt`）。
                            Check(pop4.Visible && pop4.LastClicked != 0,
                                  $"★ A462：按在台词钮上 —— **按下那一帧不说那句**（`LastClicked` 实得 {pop4.LastClicked}，"
                                + $"期望不是 0；上一帧是 {clicked0}）"
                                + "（原版 6 颗 = `ChatPopupButton : EverguildButton` ⇒ `onClick` 在松手那一帧）");
                            BattleDriver.PointerHeldForTest = false;
                            wb4.PollInputEdgesForTest();
                            wb4.TickChatInputForTest();
                            Check(pop4.LastClicked == 0,
                                  $"★ A462：……**松手那一帧才说那一句**（`LastClicked` 实得 {pop4.LastClicked}，期望 0）");
                            if (pop4.Visible) pop4.Hide();
                        }

                        // ============================================================
                        //  六、`#1` 多卡摊开窗：按下不关、松手才关
                        // ============================================================
                        Check(wb4.Ctx != null, "★ A462：（前提）这一局还有 `Ctx`（多卡窗靠牌库内容才开得起来）");
                        if (wb4.Ctx != null)
                        {
                            wb4.ShowMyDeck(true);
                            Check(wb4.MultiCardsVisibleForTest, "★ A462：（前提）多卡摊开窗开得起来");
                            BattleDriver.PointerWorldForTest = LayoutSpace.ToWorld(0.5f, 0.5f);   // 随便一点（不是牌堆）
                            BattleDriver.PointerHeldForTest = true;
                            wb4.PollInputEdgesForTest();
                            wb4.TickMultiCardsForTest();
                            Check(wb4.MultiCardsVisibleForTest,
                                  "★ A462：多卡窗**按下那一帧不关**（原版 `BackgroundCloseButton` / `Close` 钮都是 uGUI 点击）");
                            BattleDriver.PointerHeldForTest = false;
                            wb4.PollInputEdgesForTest();
                            wb4.TickMultiCardsForTest();
                            Check(!wb4.MultiCardsVisibleForTest,
                                  "★ A462：……**松手那一帧才关**（改坏法：换回 `ClickedThisFrame()` ⇒ 前一条红）");
                        }

                        // ============================================================
                        //  七、`#3` 攻击选择器【悬停即选中】+ 0.1s 安全窗（原版根本不是点击）
                        //     `CardDisplayAttackTypeButton` 只实现 `IPointerEnter/ExitHandler`；
                        //     `__Update.c:20-30` 要 `inputOverButton(+0x92) && sendInput(+0x90)
                        //     && !isInputOverSent(+0x91)` 三格一齐才发。
                        // ============================================================
                        // ⚠️ **只验触发沿，不碰拖拽流程**（WA462 §五·2 明说时序没验全）。
                        var sel4 = wb4.Selector;
                        Check(sel4 != null, "★ A462：（前提）攻击选择器在");
                        if (sel4 != null && wb4.Ctx != null)
                        {
                            // 摆一个**新鲜的**探针单位（`Ballista`：近战 0 / 远程 4 ⇒ 只有「远程」那一格。
                            // 同 5b 那段的做法，收尾要把它清掉）。
                            int side4 = wb4.MySideForTest;
                            int probe4 = FreeSlot(wb4.Ctx, side4);
                            Check(probe4 >= 0, "★ A462：（前提）我方棋盘上有一个空格能摆探针单位");
                            if (probe4 >= 0)
                            {
                                ClearEffects();
                                wb4.Ctx.Players[side4].Board[probe4] =
                                    new UnitState(CardByName(StarterCards.Tide(), "Ballista"), false) { Exhausted = false };
                                wb4.RefreshAll();
                                Check(wb4.SimulateOpenCommand(probe4) && wb4.HasCommand(AttackKind.Ranged),
                                      "★ A462：（前提）探针 `Ballista` 的选择器开起来了、且有「远程」那一格");
                                var bw4 = sel4.ButtonWorld(AttackKind.Ranged);
                                Check(bw4.HasValue, "★ A462：（前提）拿得到「远程」那格的世界坐标");
                                if (bw4.HasValue)
                                {
                                    BattleDriver.PointerWorldForTest = bw4.Value;
                                    // 安全窗还在（刚弹出，`Show()` 打了 0.1s）⇒ **悬停上去也不发**
                                    sel4.UpdatePointer(bw4.Value);
                                    Check(sel4.Hovered == AttackKind.Ranged && !sel4.HoverPickReady,
                                          $"★ A462：刚弹出时指针就压在上面 —— **安全窗没走完 ⇒ 不发**"
                                        + $"（`sendInput(+0x90)` 那一格；实得 safeLeft={sel4.HoverSafeLeft:F3}s、"
                                        + $"ready={sel4.HoverPickReady}）");
                                    wb4.DrivePlayerTurnForTest();
                                    Check(wb4.SelectedSlot >= 0 && wb4.SelectorOpen,
                                          "★ A462：……这一帧**没有选中任何打法**（指针压着也不动 —— 安全窗之内）");
                                    // 「换格 ⇒ 重开安全窗」那一格（原版 `OnPointerExit` → `DisableTemporary(其它钮)`）：
                                    // 指针**刚进入**这一格 ⇒ 安全窗从头算（0.1s 是「进入之后」才起算的）
                                    sel4.TickForTest(0f, Vector2.zero);
                                    Check(!sel4.HoverPickReady,
                                          $"★ A462：指针**刚进入**那一格 ⇒ 安全窗重开（原版 `OnPointerExit` 会给别的钮"
                                        + $" `DisableTemporary`），实得 safeLeft={sel4.HoverSafeLeft:F3}s");
                                    // 安全窗走完（喂 0.11s）⇒ 同一格**发**
                                    sel4.TickForTest(0.11f, Vector2.zero);
                                    Check(sel4.HoverPickReady,
                                          "★ A462：安全窗走完（0.1s，原版 `disableTimeAfterPointerExit` 三颗都是这个数）"
                                        + $" ⇒ 可以发（实得 {sel4.HoverSafeLeft:F3}s）");
                                    wb4.DrivePlayerTurnForTest();
                                    Check(!wb4.SelectorOpen && wb4.Command == AttackKind.Ranged,
                                          $"★ A462：**悬停即选中** —— 指针一直压着「远程」就定下来了"
                                        + $"（选择器收起 {!wb4.SelectorOpen} / 打法 {wb4.Command}）"
                                        + "（改坏法：改回 `if (ClickedThisFrame())` ⇒ 这两条红）");
                                    wb4.SimulateDeselect();
                                }
                                wb4.Ctx.Players[side4].Board[probe4] = null;      // 探针清掉（同 5b 那段）
                                wb4.RefreshAll();
                            }
                        }

                        // ============================================================
                        //  八、`#4` 的 ④ 支：**按下不是选目标，松手才是**（③ 与 ④ 同一行，判据逐字相同）
                        //     ⚠️ 另一半（⑤ 记 `_pressSlot`）必须留在按下那一帧 —— 见 `DrivePlayerTurn`
                        //     里那条注释；这里验的是「按下那一帧**没有**把目标打掉」。
                        // ============================================================
                        if (wb4.Ctx != null && wb4.FoeUnits.Count > 0)
                        {
                            int mySide4 = wb4.MySideForTest;
                            int mySlot4 = FreeSlot(wb4.Ctx, mySide4);
                            int foeSlot4 = -1;
                            CardView foeV4 = null;
                            foreach (var kv in wb4.FoeUnits)
                                if (kv.Value != null) { foeSlot4 = kv.Key; foeV4 = kv.Value; break; }
                            Check(mySlot4 >= 0 && foeSlot4 >= 0,
                                  "★ A462：（前提）我方有空格摆探针、对面场上也有一个单位（④ 那组要这两样）");
                            if (mySlot4 >= 0 && foeSlot4 >= 0)
                            {
                                ClearEffects();
                                wb4.Ctx.Players[mySide4].Board[mySlot4] =
                                    new UnitState(CardByName(StarterCards.Tide(), "Ballista"), false) { Exhausted = false };
                                wb4.RefreshAll();
                                // ⚠️ `Ballista` 只有远程 ⇒ 用 `Ranged` 起手（近战 0 开不出「近战」那一格）
                                Check(wb4.SimulateOpenCommand(mySlot4) && wb4.HasCommand(AttackKind.Ranged),
                                      "★ A462：（前提）探针的选择器开起来了、有「远程」那一格");
                                wb4.SimulateCommand(AttackKind.Ranged);          // 定好打法 ⇒ 进「选目标」
                                Check(wb4.SelectedSlot == mySlot4, "★ A462：（前提）已经进「选目标」状态");
                                if (wb4.SelectedSlot == mySlot4)
                                {
                                    BattleDriver.PointerWorldForTest = foeV4.transform.position;
                                    BattleDriver.PointerHeldForTest = true;
                                    wb4.PollInputEdgesForTest();
                                    wb4.DrivePlayerTurnForTest();
                                    Check(wb4.SelectedSlot >= 0,
                                          "★ A462：选目标时**按下那一帧不结算**（原版点棋盘单位 = `CardCollider.IPointerClickHandler`）"
                                        + " —— 改坏法：把 ④ 那一支挪回按下沿 ⇒ 这条红");
                                    BattleDriver.PointerHeldForTest = false;
                                    wb4.PollInputEdgesForTest();
                                    wb4.DrivePlayerTurnForTest();
                                    Check(wb4.SelectedSlot < 0,
                                          "★ A462：……**松手那一帧才结算**（`Resolve` 自己会 `ClearSelection`；"
                                        + "改坏法：换回 `ClickedThisFrame()` ⇒ 前一条红）");
                                }
                                wb4.Ctx.Players[mySide4].Board[mySlot4] = null;   // 探针清掉
                                wb4.RefreshAll();
                            }
                        }
                        else Debug.Log(P + "   （A462：对面场上没有单位 ⇒ ④ 那一组没验 —— 出声）");

                        // ============================================================
                        //  九、A658：三钮入场动画的**驱动量 = 累计的视口拖拽位移**（不是时间）
                        //     `AttackTypesButtonsController__MoveButtons.c`：
                        //     `accumulatedDrag += TouchInputManager.TouchDragDeltaViewport` →
                        //     `t = clamp01(−accumulatedDrag.y ÷ accumulatedDragForMinDistance(+0x30))`
                        //     `OnEnable` 把累计量清零（`__OnEnable.c`）。
                        // ============================================================
                        if (wb4.Ctx != null)
                        {
                            // 再摆一个**新鲜**探针（上面那组可能已经把上一个用掉了 —— `OpenCommand` 对
                            // `Exhausted` 的单位直接返回）
                            int side5 = wb4.MySideForTest;
                            int probe5 = FreeSlot(wb4.Ctx, side5);
                            Check(probe5 >= 0, "★ A658：（前提）我方棋盘上还有空格能摆第二个探针单位");
                            if (probe5 >= 0)
                            {
                                ClearEffects();
                                wb4.Ctx.Players[side5].Board[probe5] =
                                    new UnitState(CardByName(StarterCards.Tide(), "Ballista"), false) { Exhausted = false };
                                wb4.RefreshAll();
                                Check(wb4.SimulateOpenCommand(probe5), "★ A658：（前提）探针的选择器开起来了");
                                var s5 = wb4.Selector;
                                Check(s5 != null && s5.Visible, "★ A658：（前提）选择器可见（下面量的是它的入场进度）");
                                if (s5 != null && s5.Visible)
                                {
                                    Check(s5.EnterProgress <= 1e-3f && s5.AccumulatedDrag == Vector2.zero,
                                          "★ A658：弹出时累计量清零（原版 `OnEnable` 从静态零向量写 `+0x50/+0x54`）");
                                    // 停住不拖 ⇒ **冻住**（原来那个按时间推进的版本会自己走完）
                                    bool moved5 = s5.TickForTest(0.5f, Vector2.zero);
                                    Check(!moved5 && s5.EnterProgress <= 1e-3f,
                                          $"★ A658：**停住不拖 ⇒ 动画冻在原地**（喂 0.5s 时间 + 零位移，进度实得 {s5.EnterProgress:F3}）"
                                        + " —— 改坏法：换回 `_enterT += dt / 0.085f` ⇒ 这条红");
                                    // 往下拖（视口 y 正向）⇒ 进度不涨（原版取的是 −accumulatedDrag.y）
                                    s5.TickForTest(0f, new Vector2(0f, 0.05f));
                                    Check(s5.EnterProgress <= 1e-3f,
                                          $"★ A658：**往下拖不推进**（原版 `t = −accumulatedDrag.y ÷ …`；实得 {s5.EnterProgress:F3}）");
                                    // 往上拖 0.0425 = 半程
                                    // 🔴 **2026-10-14 就地订正**：累计量是**从开窗起一直在加**的
                                    //   （`PushDrag`：`_accumulatedDrag += delta`，只有 `OnEnable` 那一下清零）
                                    //   ⇒ 上一步那 +0.05 还挂着 ⇒ 要净 −0.0425 就得喂 **−0.0925**
                                    //   （原来写 −0.0425 ⇒ 净 +0.0075 ⇒ 进度 0，本条**从未被执行过**所以一直没露）。
                                    //   ⛔ 别改成「反过来把上一步的 0.05 删掉」—— 那一步是「往下拖不推进」的判据。
                                    s5.TickForTest(0f, new Vector2(0f, -0.0925f));
                                    Check(Mathf.Abs(s5.EnterProgress - 0.5f) < 0.02f,
                                          $"★ A658：往上拖半个阈值（净 −0.0425 屏高 = `DragThreshold01` 的一半）⇒ 进度 ≈ 0.5（实得 {s5.EnterProgress:F3}）");
                                    s5.TickForTest(0f, new Vector2(0f, -0.5f));
                                    Check(s5.EnterProgress >= 1f,
                                          $"★ A658：拖够 ⇒ 夹在 1（实得 {s5.EnterProgress:F3}）");
                                    wb4.SimulateDeselect();
                                }
                                wb4.Ctx.Players[side5].Board[probe5] = null;      // 探针清掉
                                wb4.RefreshAll();
                            }
                        }

                        // ============================================================
                        //  十、**源级**（结构）断言：`BattleDriver.cs` 里还剩哪几处用**按下沿**
                        //     15 处代码调用一处不能漏 —— 这一条把「哪一行换成了哪条沿」整体钉死，
                        //     补上「面板开不起来 ⇒ 那一处验不了」的那几处（#2 放大窗 / #13 回放条 /
                        //     #14 换牌 / #15 选牌）。⛔ 它不是行为断言，别拿它顶替上面那几条。
                        // ============================================================
                        {
                            string src4 = System.IO.Path.Combine(Application.dataPath,
                                                                 "CardPresentation/Battle/BattleDriver.cs");
                            if (!System.IO.File.Exists(src4))
                            {
                                Check(false, $"★ A462：读得到 `BattleDriver.cs`（路径 {src4}）—— 读不到时"
                                           + "下面那条源级断言**等于没验**，所以这里当场红");
                            }
                            else
                            {
                                var lines4 = System.IO.File.ReadAllLines(src4);
                                var downCallSites = new List<string>();
                                for (int i = 0; i < lines4.Length; i++)
                                {
                                    string L = lines4[i].TrimStart();
                                    if (L.StartsWith("//")) continue;                 // 注释不算（含 `///`）
                                    if (L.IndexOf("ClickedThisFrame()") < 0) continue;
                                    if (L.StartsWith("bool ClickedThisFrame")
                                        || L.StartsWith("public bool ClickedThisFrameForTest")) continue;   // 定义 / 自检口
                                    downCallSites.Add("行" + (i + 1) + ":" + L.Substring(0, Mathf.Min(52, L.Length)));
                                }
                                // **按下沿**允许留下的调用点（逐处判据 → WA462 §三 / §四）：
                                //  ① 日志面板背板 `shade`（原版 `EventTrigger` `eventID 2 = PointerDown`）
                                //  ② `ChatPopup` 条外关闭 `CloseChatPopup`（同上）
                                //  ③ 攻击选择器的**槽外取消**（原版没有对应物 —— 保持现状，WA462 §四·3）
                                //  ④ `DrivePlayerTurn` ⑤ 记 `_pressSlot`（我们自己的中间态，纪律 3）
                                string joined4 = string.Join(" | ", downCallSites.ToArray());
                                Check(downCallSites.Count == 4,
                                      $"★ A462：`BattleDriver.cs` 里用**按下沿**的代码行**只剩 4 处**"
                                    + $"（实得 {downCallSites.Count} 处：{joined4}）"
                                    + " —— 多一处 = 有一条该改抬起的没改；少一处 = 把一个本来就该按下的改掉了"
                                    + "（这一条同时覆盖 #2 放大窗 / #13 回放条 / #14 换牌 / #15 选牌："
                                    + "那几处的面板本段开不起来 ⇒ 行为那半边没验，这里补「那一行到底调的是哪条沿」）");
                            }
                        }
                    }
                    finally
                    {
                        BattleDriver.PointerHeldForTest = null;
                        BattleDriver.PointerWorldForTest = null;
                        AutoZoom.RestoreForTest(azWas4, azChosen4);
                        if (sp4 != null) { if (spWasOpen4) sp4.Show(); else sp4.Hide(); }
                    }
                }
            }
            Debug.Log(P + "--- A462 / A658 段结束 ---");

            int hookedBeforeB = BattleDriver.StaticHookCount;
            Check(hookedBeforeB > 0,
                  $"（前提）新开一局把静态钩子**接回来**了（现存 {hookedBeforeB} 条）—— 有这个前提，(b) 才不是恒真");

            // ---------------- 🆕 2026-10-13（A515）：**二次 `Begin()` 也要把钩子一条不少地接回来** ----------------
            // 🔴 上面那次 `driver.Begin(...)` 是**同一个 driver 的第 N 次**（`_hudBuilt` 早就闩住了 ——
            //    `HudExtraCount` 那 10 件补摆件就是证据）⇒ 正好在这儿钉「静态钩子**每次 `Begin` 都重挂**」。
            //    基线 = `hookedBefore388`（**同一次运行**里「一次 `Begin` 之后」的实测条数，见它上面那段注；
            //    清单槽数 `StaticHookSlots` 只打印出来当参照，⛔ 不拿它当期望值 —— 理由同那段注）。
            //    毛病原来是：`UnitTweenRuntime.HeroBySeat` / `SeatOf` 那两个补间解析口**只在 `BuildHud()` 里赋值**，
            //    而 `BuildHud` 被 `_hudBuilt` 闩住 ⇒ 二次 `Begin` 时它们**不跑**（可 `DetachStaticHooks()`
            //    已经把这两格置 null 了 ⇒ 静默少两条，下游只表现为「补间定位不到督军」）。
            // 🧨 **改坏法**：把那两句挪回 `BuildHud()` ⇒ 二次 `Begin` 只接回 **17/19** ⇒ 第 1 条红
            //    （2026-10-13 之前就是这个状态，实测日志 `d:/4/_tmp_view/battle.log` 卸前 17）；
            //    把 `Begin()` 里那句 `HookUnitTweenResolvers()` 删掉 ⇒ 两条一起红。
            // ⚠️ 它与上面 (a)/(b) 两段**不冲突**：(b) 用的是动态量 `hookedBeforeB`（⛔ 没有写死 17），
            //    所以这条修完不会把 (b) 弄红。
            {
                int slots515 = BattleDriver.StaticHookSlots;
                Check(slots515 > 0 && driver.HudExtraCount > 0 && hookedBeforeB == hookedBefore388,
                      $"★ A515：**二次 `Begin()` 之后静态钩子一条不少**（首次 `Begin` 后 {hookedBefore388} 条 → "
                    + $"二次 `Begin` 后 {hookedBeforeB} 条；清单共 {slots515} 槽）"
                    + $"，且 HUD 早建过了（`HudExtraCount = {driver.HudExtraCount}` ⇒ `_hudBuilt` 为真）"
                    + " —— 钩子绑的是**这个 driver 实例**，所以 `Begin` 每走一次都要重挂；少两条就是"
                    + "`UnitTweenRuntime.HeroBySeat` / `SeatOf` 又挂回 `BuildHud()` 里去了");
                Check(UnitTweenRuntime.HeroBySeat != null && UnitTweenRuntime.SeatOf != null,
                      "★ A515：那两个**补间解析口**在二次 `Begin()` 之后仍挂着"
                    + $"（`HeroBySeat` = {(UnitTweenRuntime.HeroBySeat != null ? "非 null" : "**null**")} · "
                    + $"`SeatOf` = {(UnitTweenRuntime.SeatOf != null ? "非 null" : "**null**")}）"
                    + " —— 它们为 null 时 `UnitTweenRuntime.ResolveHero` **直接返回 null**（补间定位不到督军，静默）");
            }
            UnityEngine.Object.DestroyImmediate(driver);

            // 🔴 **2026-10-12（A388 收红那轮）：这一段按 `Application.isPlaying` 分两档。**
            //    原来那句「批处理里 `DestroyImmediate` **同步**调 `OnDestroy`」**前提是错的**：
            //    `BattleDriver` **不带 `[ExecuteAlways]`**（`Battle/BattleDriver.cs:22` 那一行），而自检跑在
            //    **编辑模式**（`BattleScene.cs:97` 的 `EditorSceneManager.NewScene`；同一轮 A341 那条断言
            //    自己盖过章 `Application.isPlaying == false`）⇒ **编辑器不派生命周期消息** ⇒
            //    `DestroyImmediate(组件)` **不会**调 `OnDestroy` ⇒ `DetachStaticHooks()` 压根没被调过。
            //    **实测**（`d:/4/_tmp_view/battle.log:39055`）：卸前 17 → 现存 **17**；而 (a) 段**直接调**
            //    `DetachStaticHooks()` 是 **19 → 0**（`:38912`）⇒ **那个方法本身没毛病**，是这一档的前提不成立。
            //    ⛔ **不许把这一档写成绿、也不许删掉**（铁律 11 + 「弱断言分不出两种状态 = 没断」）：
            //      编辑模式这半分**改断「环境事实」**（组件真没了 ∧ 钩子**原封不动**，两件都是实读）。
            //    🔴 **真路径（真卸载 ⇒ 清零）批处理里验不了** ⇒ 归 `资料/真Play待验清单.md` **D39**
            //      （「离开战场场景 → 再进一次」，走真卸载而不是 `DestroyImmediate`）。
            bool realUnload388 = Application.isPlaying;
            int hookedAfterB = BattleDriver.StaticHookCount;
            if (realUnload388)
            {
                Check(hookedAfterB == 0,
                      $"★ A388：**组件真被卸载**之后一条静态钩子都不剩（卸前 {hookedBeforeB} → 现存 {hookedAfterB}）"
                    + " —— 这一档 `DestroyImmediate` 会派 `OnDestroy`（它第一句就是 `DetachStaticHooks()`）"
                    + "；🧨 改坏法：`OnDestroy` 不摘（或摘漏一条）⇒ 现存 ≥ 1 ⇒ 红");
            }
            else
            {
                Debug.LogWarning($"[A388(b)] 编辑模式（`Application.isPlaying == false`）：`DestroyImmediate(组件)` "
                               + "**不派 `OnDestroy`**（`BattleDriver` 不带 `[ExecuteAlways]`）⇒ 钩子仍挂着 "
                               + $"{hookedAfterB} 条、且指着**已销毁**的 driver。这一档**验不了**「真卸载 ⇒ 清零」"
                               + "—— 真路径 = 离开战场场景 → 再进一次，只有真 Play 能跑到"
                               + "（`资料/真Play待验清单.md` D39）。");
                Check(driver == null && hookedAfterB == hookedBeforeB,
                      $"★ A388（编辑模式这一档）：组件**真没了**（`== null`）而钩子**原封不动**"
                    + $"（{hookedBeforeB} → {hookedAfterB}）—— 这是「编辑器不派生命周期消息」那条环境事实，"
                    + "**不是**「实现了清零」（清零那半只有真 Play 能验，D39）"
                    + "；🧨 改坏法：给 `BattleDriver` 加 `[ExecuteAlways]`（或让这一路真派上 `OnDestroy`）"
                    + " ⇒ 现存变 0 ⇒ 本条红 —— 那是「环境变了、好消息」，把上面 Play 那一档提成无条件即可");
            }
            // ⚠️ 销毁的是**组件**（不是 `driver.gameObject` —— 那是 `sceneRoot`）。
            // ⚠️ **仍然没验的那条真路径**：离开战场场景 → 再进一次（场景卸载 → 新 driver）—— 那要真 Play（D39）。
        }

        // ---------------- 🆕 2026-10-12（A431）：A417 接线 + A393 那 5 条场景侧 `AnimFXController` 真建出来了 ----------------
        // 这一段 = **验收**（生产那一跳在 `Battle/ArenaRuntimeLoader.cs:112-114`，H7 已接 —— 本件**只写断言**）。
        // 判据（全是实读；全文 → `资料/普查产出_1012/H2_场景侧AnimFX.md`）：
        //  · **数据侧**：`Resources/EnvBlendables.json` 的 `sceneStandalone` 一节**总条数 = 5**（原版直读；`-1` = 那节没读到）；
        //  · **生产那一跳**：三场各自的「建出几个组件 / `nodes[]` 几条 / 改挂几个 / 没对上几个」= 原版包直读那几格
        //    （`battlearena2` **1/3/5/0** · `battlearena3` **2/4/0/0** · `battlearenatauviorla` **2/1/0/0** ⇒ 合计 **5 条**）；
        //    🔴 2026-10-14（#2–#5）：`nodes[]` 那几格现在读的是「**新建 + 复用** == 这一格」——prefab 重建后
        //    它们**已经在树里**，走的是「复用」那一支；另有单配的一条钉「新建 == 0」（见 `wantNodes` / `wantReused`）。
        //  · **接线点**：`SceneAnimFxCalls` 每 `Load()` 一次 +1（⛔ 恒 0 = 「数据在、没人调」那一档）。
        // ⚠️ **落在整轮最后**（`A388` 那两段之后）：本段会 `Instantiate` 一整棵战场，谁也别再依赖现场。
        // 🔴 **纪律（2026-10-14 补）**：**本轮收尾这几段（A431 / A514 / A463）不许依赖 `driver`** ——
        //    `driver` 已在上面那句 `DestroyImmediate(driver)` 卸掉（编辑模式不派 `OnDestroy`，所以别指望它
        //    自己收尾）。A462/A658 那一整段原来就落在这批收尾段的**后面** ⇒ 进段时
        //    `Object.FindObjectOfType<BattleDriver>()` 恒 null、**整段一条都验不了**（已搬到 `Begin()` 之后）。
        // ⚠️ **不碰任何既有现场**：用**独立探针根**（⛔ 不挂进 13d 那组的 `tvInst`），收工 `DestroyImmediate`。
        // ⚠️ `enabled` / 宿主 `activeSelf` 两个开关**全部来自旁挂字段**（`enabled` / `goActive`），不是我们挑的。
        // 🧨 **改坏法**：① 删 `ArenaRuntimeLoader.cs` 里那句 `BuildSceneAnimFx(...)` ⇒ `SceneAnimFxCalls` 不涨 ⇒ 第 2 条红；
        //   ② `gen_env_blendables.py` 的 `main()` 去掉 `sb.collect_scene_standalone(scripts)` 重生成旁挂 ⇒
        //      `SceneStandaloneDataCount()` 变 `-1` ⇒ 第 1 条红、且三场都会「建出 0 条」⇒ 第 3 条红；
        //   ③ 把 `BuildSceneAnimFx` 里那句 `_sceneAnimFxRoot == arenaRoot` 去重删掉 ⇒ 同一个根再调一次会**再建一遍**
        //      （**幂等那条**：第二次返回 1 而不是 0）⇒ 红；
        //   ④ 把 `nodes[]` 的 `SetParent(..., false)` 改成 `true`（或改坏 `reparent` 那一跳）⇒ 节点位姿/父子关系变 ⇒
        //      第 4 条（`Find` 到 RocketTrail 底下挂着的粒子数）红。
        //   ⑤ **把 `BuildSceneAnimFx` 里 `MakeAnimFx(..., /*simulateSelfDestroyInEditor:*/ false)` 那个 `false`
        //      删掉/改成 `true`** ⇒ `battlearena2` 的 `RocketTrail` 宿主在编辑模式**当场被 `DestroyImmediate`**
        //      ⇒ 组件被误记成「建不出来」、③ 改挂被 `continue` 跳过 ⇒ **本段四条一起红**
        //      （= 2026-10-12 之前的状态；根因 → `D9_Battle六红诊断.md` §二·3）。
        // 🔴 **2026-10-14（A554）：下面这几张表【只留这一份】，A431 与后面的 A514 ① 共用** ——
        //   （原来两处各写一遍同样的字面量：这里 `3/4/1`，A514 ① 那边 `3 / 0 / 1 / 1 / 5 / 0`。
        //    🆕 其中 `wantNodes` / `wantReused` 当天又按 **A555** 改成**从旁挂现读**了 —— 见下面那段。）
        //   两段测的是**同一份事实**：`battlearena2` 要的那几个分组节点**已经烘在 prefab 里**
        //   ⇒ 走「有同名子件就复用」那一支（新建 **0** · 复用 = 旁挂 `nodes[]` 的条数）。
        //   ⛔ **别退回「两处各写一份」**：`CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」——
        //   提到块外之后这条耦合是**编译期**的：哪天 prefab 里那几个节点被拿掉/改名、或建场口径变了
        //   （`Created` 会跟着掉或涨），A431 与 A514 ① **一起红**，不用靠人去记。
        //   （先例 = 上面那条「`Embers` 预告」：当时预告了 `reparent 5 → 6`，靠的是注释提醒，
        //     结果那个预告根本没发生、期望值一个没动 ⇒ 注释挡不住漂移，共用一份才挡得住。）
        //   ⚠️ A514 ① 自己那条**前提断言**（「`battlearena2.prefab` 自带一颗根级 `Scenario` 子件」）仍留在它那边
        //   —— 它咬的是「夹具前提在不在」，不是期望值。
        string[] keys     = { "battlearena2", "battlearena3", "battlearenatauviorla" };
        int[] wantBuilt   = { 1, 2, 2 };
        // 🔴 **2026-10-14（A555 · A418②）：`wantNodes` / `wantReused` 改成「由旁挂条数驱动」** ——
        //   这两个数**不是常量**，它们就是 `Resources/EnvBlendables.json` 的
        //   `sceneStandaloneBuild[].nodes.Length`（现读 = `3 / 4 / 1`；**重跑生成器之后会变 `0 / 0 / 0`**，
        //   原因见 `EnvBuildNodeCounts` 的 doc）⇒ 写死任何一个都会在**另一档**红。
        //   期望值 = **全部走复用**（新建恒 0）：那几条路径建场侧已经各建过一颗、且烘进了 prefab。
        int envGroupsFound;
        int[] wantReparen;
        int[] wantNodes   = EnvBuildNodeCounts(keys, out envGroupsFound, out wantReparen);
        int[] wantReused  = (int[])wantNodes.Clone();   // 期望：一条都不新建 ⇒ 复用 == 旁挂条数
        // 🔴 **2026-10-14（A418②）：`wantReparen` 也改成由旁挂驱动** —— 原来写死 `{5,0,0}`。
        //   这一格**与 `nodes[]` 联动**：`nodes[]` 非空时，运行时先建那几颗节点、再把子件改挂进去；
        //   `nodes[]` 变成空表之后（建场侧已烘进 prefab），原来挂在「新节点」下的那一件也要**直接改挂**
        //   ⇒ `reparent` 条数会**变多**（battlearena2 实测 5 → **6**）。写死 5 就会把**合法重生成**误报成红。
        //   值直接读旁挂 `sceneStandaloneBuild[].reparent.Length`（与 `nodes` 同一次加载，⛔ 不另开第二处口径）。
        // ⚠️ **本批新增的前提**（A555）：期望值现在是**从旁挂现读**的 ⇒「读不到」必须**当场红**，
        //   否则 `EnvBuildNodeCounts` 会静默回 `0 / 0 / 0`，把「没读到」伪装成「旁挂本来就是空的」
        //   （= 「弱断言分不出两种状态」那一族）。
        Check(envGroupsFound == keys.Length,
              $"（前提）★ A431：三场都要能在旁挂 `sceneStandaloneBuild[]` 里按 `root` 找到一节"
            + $"（实得 {envGroupsFound} / {keys.Length}）—— 少了它，下面那条的期望值会静默变成 0/0/0");
        {
            int stan = CardPresentation.ScenarioBlendableFactory.SceneStandaloneDataCount();
            Check(stan == 5,
                  $"★ A431：旁挂里 `sceneStandalone` 一节 = **5 条**（实得 {stan}；原版直读 = 5 ——"
                + " `battlearena2` 1 · `battlearena3` 2 · `battlearenatauviorla` 2）"
                + " —— `-1` / 0 就是「那一节没读到」（跑 `python 工具/gen_env_blendables.py` 重生成）");

            int callsBefore = CardPresentation.ScenarioBlendableFactory.SceneAnimFxCalls;
            Check(callsBefore > 0,
                  $"★ A431：**接线点真的有人到**（`SceneAnimFxCalls` = {callsBefore}；它每 `Load()` 一次 +1，"
                + "恒 0 = 「数据在、但没人调」那一档 —— 生产那一跳在 `ArenaRuntimeLoader.Load()` 里）");

            // 期望值（判据 = 原版包；三场都走**生产那份 prefab**，⛔ 不手搓）
            // 🔴 **2026-10-14 订正（#2–#5）**：这一段原来写着「若跑 A418 那三步，`battlearena2` 的 `reparent[]`
            //   会 **5 → 6**（多出 `Embers`）⇒ 期望值要一起改成 6」——**那个预告没有发生**（现读
            //   `Resources/EnvBlendables.json` 的 `sceneStandaloneBuild`：三场仍是 `reparent = 5 / 0 / 0`）。
            //   **本轮真正变了的是另一件事**：`ArenaBuilder.BuildArenaPrefabs` **重建了 13 场 prefab**
            //   ⇒ 旁挂 `nodes[]` 要建的那几个分组节点**已经烘在 prefab 里**了 ⇒ `SceneAnimFxNodesCreated`
            //   **3/4/1 → 0**、`SceneAnimFxNodesReused` **→ 3/4/1**（`Battle/ScenarioBlendables.cs:2327-2345`
            //   那条「有同名子件就**复用**、不建两份」——两份同名之后 `SceneResolver` 是「名字 + 最近位置」，
            //   命中哪一份**不确定**）⇒ **这是本仓 A514 本批故意要的行为，不是缺陷**，是期望值过时。
            //   ⇒ 每条改成「**新建 + 复用 == 旁挂 `nodes[]` 的条数**」，并**另单配一条**钉「新建必须是 0」
            //      （将来谁把那些节点从 prefab 里拿掉 ⇒ 红的是那条**点名**的断言，而它上面的和式仍绿）。
            // 期望值表（`keys` / `wantBuilt` / `wantNodes` / `wantReused` / `wantReparen`）在**本段块外**
            // —— 🆕 2026-10-14（A554）提到那里了，**与后面 A514 ① 共用一份**（⛔ 别在块里再抄一份）。
            int totalBuilt = 0, totalNodes = 0, totalReused = 0, totalReparen = 0, totalMissed = 0;
            var p2 = Resources.Load<GameObject>("ArenaPrefabs/" + keys[0]);
            Check(p2 != null,
                  "（前提）`Resources/ArenaPrefabs/battlearena2.prefab` 在 —— 少了就跑"
                + " `-executeMethod ArenaBuilder.BuildArenaPrefabs`");
            if (p2 != null)
            {
                var probe = UnityEngine.Object.Instantiate(p2);
                probe.name = "A431 探针（battlearena2）";
                int n = CardPresentation.ScenarioBlendableFactory.BuildSceneAnimFx(probe.transform, keys[0]);
                int nodesA  = CardPresentation.ScenarioBlendableFactory.SceneAnimFxNodesCreated;
                int reusedA = CardPresentation.ScenarioBlendableFactory.SceneAnimFxNodesReused;
                totalBuilt += n; totalNodes += nodesA; totalReused += reusedA;
                totalReparen += CardPresentation.ScenarioBlendableFactory.SceneAnimFxReparented;
                totalMissed += CardPresentation.ScenarioBlendableFactory.SceneAnimFxMissed;
                Check(n == wantBuilt[0]
                   && nodesA + reusedA == wantNodes[0]
                   && CardPresentation.ScenarioBlendableFactory.SceneAnimFxReparented == wantReparen[0]
                   && CardPresentation.ScenarioBlendableFactory.SceneAnimFxMissed == 0
                   && CardPresentation.ScenarioBlendableFactory.SceneAnimFxCalls == callsBefore + 1,
                      $"★ A431：`battlearena2` 的 `RocketTrail` 那一族**真建出来了**（组件 {n} · 新建节点 "
                    + $"{nodesA} · 复用 {reusedA} · 改挂 "
                    + $"{CardPresentation.ScenarioBlendableFactory.SceneAnimFxReparented} · 没对上 "
                    + $"{CardPresentation.ScenarioBlendableFactory.SceneAnimFxMissed}"
                    + (CardPresentation.ScenarioBlendableFactory.SceneAnimFxMissed > 0
                       ? $"（{CardPresentation.ScenarioBlendableFactory.SceneAnimFxMissedWhat}）" : "")
                    + $"；期望 **{wantBuilt[0]} / {wantNodes[0]}（= 新建 0 + 复用 {wantReused[0]}）/ {wantReparen[0]} / 0**）"
                    + "—— 原版那颗 `destroyTime = 6` 一销毁要连带这 5 个子件粒子");
                // ⛔ 这一条**不是上面那条的重复**：它钉的是「**新建必须是 0**」——
                //   旁挂 `nodes[]` 要的那 3 个节点**已经烘在 prefab 里**（本轮 `BuildArenaPrefabs` 重建的）
                //   ⇒ 走的是「复用」那一支。🧨 改坏法：把 prefab 里那 3 个分组节点删掉（或
                //   `gen_env_blendables.py` 不再收它们）⇒ **这一条红**，而上面那条（和式）仍然绿。
                Check(nodesA == 0 && reusedA == wantReused[0],
                      $"★ A431：`battlearena2` 那 {wantNodes[0]} 个分组节点是**复用**来的（新建 {nodesA}（期望 **0**）· "
                    + $"复用 {reusedA}（期望 **{wantReused[0]}**））—— prefab 重建后它们已经在树里，"
                    + "`ScenarioBlendables.cs` 那条「有同名子件就复用、不建两份」正是本仓要的行为");

                // 比数字更硬的一条：**父子关系真的建立了**（那 5 个改挂对象的父 = 新建的那颗 `RocketTrail`）。
                var rk = probe.transform.Find("Scenario/Battle Arena 2 Particles/RocketTrail");
                int rkPs = rk != null ? rk.GetComponentsInChildren<ParticleSystem>(true).Length : -1;
                Check(rk != null && rkPs >= 5,
                      $"★ A431：`Scenario/Battle Arena 2 Particles/RocketTrail` 底下**挂着 ≥ 5 颗粒子**（实得 {rkPs}）"
                    + " —— 改挂那一跳是 `SetParent(..., true)`（世界位姿逐字不变），只改了归属；"
                    + "这几个名字（`BigExplosion` / `Smoke` / `Twinkle` / `Fire Small` / `Launch Smoke`）也一起钉住了");

                // 两个开关**照旁挂**（`enabled` / `goActive`）—— `battlearena2` 那颗原版是两个都开。
                var c2 = rk != null ? rk.GetComponent<AnimFXController>() : null;
                Check(c2 != null && c2.enabled && c2.gameObject.activeSelf,
                      $"★ A431：`RocketTrail` 那颗组件 `enabled` + 宿主 `activeSelf` 都按旁挂开着"
                    + $"（实得 enabled={c2 != null && c2.enabled} · activeSelf={c2 != null && c2.gameObject.activeSelf}）");

                // **幂等**：同一个战场实例再调一次 ⇒ 0（判据 = 传进来的那个 root，见 `BuildSceneAnimFx` 的类注释）。
                int n2 = CardPresentation.ScenarioBlendableFactory.BuildSceneAnimFx(probe.transform, keys[0]);
                Check(n2 == 0,
                      $"★ A431：**同一个战场实例**再调一次 ⇒ 不重复建（实得 {n2}）—— 去重判据是「传进来的那个 root」"
                    + "（原版那些组件随场景只出现一次；换场新实例会重新建，那是另一支）");

                UnityEngine.Object.DestroyImmediate(probe);
            }

            // 另外两场：只数数字（各自一份**独立探针根**；`enabled=false` 那几颗照样算「建出来了」）。
            for (int i = 1; i < keys.Length; i++)
            {
                var pf = Resources.Load<GameObject>("ArenaPrefabs/" + keys[i]);
                Check(pf != null, $"（前提）`Resources/ArenaPrefabs/{keys[i]}.prefab` 在");
                if (pf == null) continue;
                var probe = UnityEngine.Object.Instantiate(pf);
                probe.name = "A431 探针（" + keys[i] + "）";
                int n = CardPresentation.ScenarioBlendableFactory.BuildSceneAnimFx(probe.transform, keys[i]);
                int nodes = CardPresentation.ScenarioBlendableFactory.SceneAnimFxNodesCreated;
                int reused = CardPresentation.ScenarioBlendableFactory.SceneAnimFxNodesReused;
                int rep   = CardPresentation.ScenarioBlendableFactory.SceneAnimFxReparented;
                int miss  = CardPresentation.ScenarioBlendableFactory.SceneAnimFxMissed;
                totalBuilt += n; totalNodes += nodes; totalReused += reused;
                totalReparen += rep; totalMissed += miss;
                Check(n == wantBuilt[i] && nodes + reused == wantNodes[i] && rep == wantReparen[i] && miss == 0,
                      $"★ A431：`{keys[i]}` 那一族也真建出来了（组件 {n} · 新建节点 {nodes} · 复用 {reused} · "
                    + $"改挂 {rep} · 没对上 {miss}"
                    + (miss > 0 ? $"（{CardPresentation.ScenarioBlendableFactory.SceneAnimFxMissedWhat}）" : "")
                    + $"；期望 {wantBuilt[i]} / {wantNodes[i]}（= 新建 0 + 复用 {wantReused[i]}）"
                    + $" / {wantReparen[i]} / 0）");
                // 「新建必须是 0」那条（同 `battlearena2`）—— 判据与改坏法见上面那一段的注释。
                Check(nodes == 0 && reused == wantReused[i],
                      $"★ A431：`{keys[i]}` 那 {wantNodes[i]} 个分组节点是**复用**来的（新建 {nodes}（期望 **0**）· "
                    + $"复用 {reused}（期望 **{wantReused[i]}**））—— 与 `battlearena2` 同因："
                    + "prefab 重建后它们已经在树里，走「有同名子件就复用」那一支");

                // 两个开关也逐场钉一遍（原版就是这几档：✓ 全开 / ✓ 组件关而宿主开 / ✓ 宿主关而组件开）。
                // ⚠️ 这里按**名字**取、⛔ 不按路径写死：那几颗的父是**已有的**对象，路径是 `SceneResolver`
                //    （名字 + 最近位置）当场解出来的 ⇒ 写死路径只会在无关的地方红。
                string leaf = keys[i] == "battlearena3" ? "Lightning_Green" : "Big Gun Effect";
                AnimFXController pick = null;
                foreach (var c in probe.GetComponentsInChildren<AnimFXController>(true))
                    if (c.gameObject.name == leaf) { pick = c; break; }
                bool wantEnabled = keys[i] != "battlearena3";              // arena3 两颗原版就是关的
                bool wantActive  = keys[i] != "battlearenatauviorla";      // tau 那颗宿主 GO 原版就是关的
                Check(pick != null && pick.enabled == wantEnabled && pick.gameObject.activeSelf == wantActive,
                      $"★ A431：`{leaf}` 那颗组件真挂在树里，且 `enabled` / 宿主 `activeSelf` **与旁挂逐值一致**"
                    + $"（实得 {pick != null && pick.enabled} / {pick != null && pick.gameObject.activeSelf}，"
                    + $"期望 {wantEnabled} / {wantActive}）—— 判据 = 旁挂的 `enabled` / `goActive`"
                    + "（原版这几个值本来就不一样：arena3 那两颗组件是关的、tau 那颗宿主 GO 是关的）");

                UnityEngine.Object.DestroyImmediate(probe);
            }

            int totalWant = wantNodes[0] + wantNodes[1] + wantNodes[2];
            // 🔴 **2026-10-14（A418②）**：`totalReparen` 的期望也**由旁挂驱动**（原来写死 `5`）——
            //   它与 `nodes[]` 联动（见 `wantReparen` 那段注释）：`nodes[]` 变空之后，原来挂在「新节点」下
            //   的那一件也要直接改挂 ⇒ 合计 5 → **6**。写死 5 会把合法重生成误报成红。
            int totalReparenWant = wantReparen[0] + wantReparen[1] + wantReparen[2];
            // 🔴 **2026-10-14（A555 · A418②）：下面那条的期望值改成「由旁挂条数驱动」** ——
            //   重跑 `工具/gen_env_blendables.py`（= A418② 那条 Unity 腿）之后，
            //   `sceneStandaloneBuild[].nodes[]` 会从 `3 / 4 / 1` 变成 **`0 / 0 / 0`**
            //   （生成器会把与建场侧已建的那 8 条全跳过）⇒ 旧版这里写死的 `totalReused == 8`
            //   会把**合法重生成**误报成红。现在两种状态都**真**验，⛔ 不是放宽。
            Check(totalBuilt == 5 && totalNodes == 0 && totalReused == totalWant
               && totalReparen == totalReparenWant && totalMissed == 0,
                  $"★ A431：**三场合计**建出 {totalBuilt} 个组件（期望 **5** = 旁挂里那 5 条，一条不漏）·"
                + $" 新建节点 {totalNodes}（**0** —— 一条都不许重复建）·"
                + $" 复用 {totalReused}（期望 **{totalWant}** = 旁挂 `nodes[]` 合计，现读 "
                + $"`{wantNodes[0]} / {wantNodes[1]} / {wantNodes[2]}`）· 改挂 {totalReparen}（{totalReparenWant}）· 没对上 {totalMissed}（0）");

            // 🔴🆕 **2026-10-14（A555 · A418②）：「旁挂 `nodes[]` == 0」那一档也必须绿** —— 本条专为它写。
            //   那一档（= 重跑生成器之后）最容易被踩的坑是：把「`nodes[]` 是空的」当成「**这一节没数据**」
            //   ⇒ 连 `sceneStandalone` 那半边的 5 个组件也一起不建（生产代码里那两条判据是**分开**的：
            //   看的是 `bg` / 两节在不在，⛔ **不是** `nodes.Length > 0`）⇒ 这 5 个组件是死数、与 `nodes[]` 无关。
            //   而「节点」那两格必须**随数据在 `0` ↔ 旁挂条数之间变**：空表就是**一颗都不建**。
            //   🧨 改坏法：① 见上（整节跳过 ⇒ `totalBuilt` 红）；② 不看数据、见到空表仍按旧清单**多建**
            //   ⇒ `totalNodes == 0` 红；③ 把 `EnvBuildNodeCounts` 的读法改回「按下标取」⇒ 前面那条
            //   「三节都按 `root` 找到」的**前提断言**先红。
            Check(totalBuilt == 5 && totalNodes + totalReused == totalWant,
                  $"★ A431：**`nodes[]` == 0 那一档也必须绿**（= A418② 重跑 `gen_env_blendables.py` 之后）——"
                + $" 组件 {totalBuilt}（期望 **5**：来自 `sceneStandalone`，与 `nodes[]` 空不空**无关**）·"
                + $" 新建 + 复用 = {totalNodes} + {totalReused} = {totalNodes + totalReused}"
                + $"（期望 **{totalWant}**，现读）—— ⛔ 这不是「没比」：条数为 0 时这两格必须**真的是 0 / 0**，"
                + "多建一颗、或整节跳过，这里都会红");
        }

        // ---------------- 🆕 2026-10-13（A514）：场景侧 AnimFX 的**静默口**全部改成出声 ----------------
        // 判据 = **红线「不许静默失败」**（`CLAUDE.md` §三）+ **本文件族既有的出声范本**
        //   （`MakeAnimFx` 自己那两处：宿主对象解析不到 / 旁挂里缺 `preventDestroy`；
        //    外加 `BuildSceneAnimFx` 的汇总日志 `SceneAnimFxMissedWhat`）。⛔ 没有新造一套通道。
        // 本段**只验两件事**：① 该只建一份的**只建一份** ② 该出声的**真出声** —— ⛔ 不复验 A431 那段数字
        //   （那段管「建得对不对」）；⛔ 也不碰 13d 那组既有现场（另起独立探针、收工 `DestroyImmediate`）。
        // ⚠️ **落点必须在这**（A431 之后、整轮收尾之前）：本段会**临时把 `sceneStandalone` 两节按掉再恢复**
        //   ⇒ 放前面会把 A431 那一段的数字带偏。恢复是否成功，本段自己**回读一次「5 条」自证**。
        // ⚠️ **如实标一条**：`MakeAnimFx` 里第 3 条（`AddComponent` 回 null）**合成不出来** ⇒ 那一条
        //   **没有断言咬**，只有日志（见 `ScenarioBlendables.cs` 该处的注释）；下面 ④ 只咬得住另两条。
        {
            var prefab514 = Resources.Load<GameObject>("ArenaPrefabs/battlearena2");
            Check(prefab514 != null,
                  "（前提）`Resources/ArenaPrefabs/battlearena2.prefab` 在（A514 段要用它）");

            // ---- ① 节点「已存在就复用」（原来是无条件 `new GameObject`）----
            // 判据：旁挂那句话的前提是「原版有、**我们工程里没有**」，可我们这边后来**可能已经有了**
            //   （建场侧也照原版建分组节点 / prefab 重烘 / 两个调用点用**不同 root** 各调一次）。
            //   无条件建 ⇒ 同一个父底下**两份同名**，而 `SceneResolver` 是「名字 + 最近位置」
            //   ⇒ 之后 `GoOf` 命中哪一份**不确定**（静默错）。
            // 期望值（数据直读）：`battlearena2` 的 `nodes[]` = **3** 条（`Scenario` / `Battle Arena 2 Particles`
            //   / `RocketTrail`，浅→深）；本轮 prefab 重建后这 3 条**全都在 prefab 里** ⇒ 新建 **0**、复用 **3**。
            // 🔴 **2026-10-14（#6）订正**：这里原来写着「本段**先自己摆一颗** `Scenario` ⇒ 新建 = 3 − 1 = 2、复用 = 1」
            //   —— 那会造出「**同一个父底下两份同名**」，而**生产路径不会这样**（传进来的 root 本来就是 prefab
            //   自带的那一份）⇒ 夹具那三行删掉，期望改成「复用 3 / 新建 0」，判据对象换成 **prefab 自带的那颗**。
            // 🧨 **改坏法**：把 `BuildSceneAnimFx` 里那段 `FindChildByName` 判空删掉（回到无条件建）
            //   ⇒ 复用 **0**（不是 3）· 新建 **3**（不是 0）· 场根底下叫 `Scenario` 的子件 **2** 个（不是 1）
            //   ⇒ 下面第 1 条**一处三红**。
            if (prefab514 != null)
            {
                var probeA = UnityEngine.Object.Instantiate(prefab514);
                probeA.name = "A514 探针（节点复用）";
                // 🔴 夹具**不再手工摆那颗 `Scenario`**（旧夹具那三行：`new GameObject("Scenario")` +
                //   `SetParent` + `localPosition = (1,2,3)`）—— 判据对象换成 prefab 自带的那一颗：
                //   调用**前后**逐位比它的 `localPosition`（语义不变：仍然是「只出声、不动我们的树」）。
                var pre = probeA.transform.Find("Scenario");
                Check(pre != null,
                      "（前提）`battlearena2.prefab` 自带一颗根级 `Scenario` 子件"
                    + "（下面「复用不改写」比的就是它；⛔ 少了它下面那条等于没验，所以这里当场红）");
                Vector3 preLp = pre != null ? pre.transform.localPosition : Vector3.zero;
                var gotA = new List<string>();
                Application.LogCallback hA = (string m, string st, LogType ty) => gotA.Add(m);
                Application.logMessageReceived += hA;
                int builtA;
                try { builtA = CardPresentation.ScenarioBlendableFactory.BuildSceneAnimFx(probeA.transform, "battlearena2"); }
                finally { Application.logMessageReceived -= hA; }
                int dupA = 0;
                for (int i = 0; i < probeA.transform.childCount; i++)
                    if (probeA.transform.GetChild(i).name == "Scenario") dupA++;
                // ⚠️ 期望值 = **本段块外那张共用表**（A431 与 A514 ① 共用一份，见它上面那段注释；`[0]` = `battlearena2`）
                //   —— ⛔ 别在这里又写一遍字面量（A554：两处写同一条规则 = 迟早不一致）。
                Check(CardPresentation.ScenarioBlendableFactory.SceneAnimFxNodesReused == wantReused[0]
                   && CardPresentation.ScenarioBlendableFactory.SceneAnimFxNodesCreated == 0
                   && dupA == 1 && builtA == wantBuilt[0]
                   && CardPresentation.ScenarioBlendableFactory.SceneAnimFxReparented == wantReparen[0]
                   && CardPresentation.ScenarioBlendableFactory.SceneAnimFxMissed == 0,
                      $"★ A514：节点**已存在就复用**（复用 {CardPresentation.ScenarioBlendableFactory.SceneAnimFxNodesReused}"
                    + $" · 新建 {CardPresentation.ScenarioBlendableFactory.SceneAnimFxNodesCreated} · 场根底下叫 `Scenario`"
                    + $" 的子件 {dupA} 个 · 组件 {builtA} · 改挂 {CardPresentation.ScenarioBlendableFactory.SceneAnimFxReparented}"
                    + $" · 没对上 {CardPresentation.ScenarioBlendableFactory.SceneAnimFxMissed}）"
                    + $" —— 期望 **{wantReused[0]} / 0 / 1 / {wantBuilt[0]} / {wantReparen[0]} / 0**"
                    + "（旁挂 `nodes[]` 那几条**全都在 prefab 里**了 ⇒ 一个都不新建；"
                    + "场根底下只有 prefab 自带的那**一颗** `Scenario`。两份同名之后 `SceneResolver` 命中哪一份就不确定了）");
                Check(pre != null && (pre.transform.localPosition - preLp).sqrMagnitude < 1e-8f,
                      $"★ A514：复用**不改写**已有的那颗节点（`localPosition` 实得 "
                    + (pre != null ? pre.transform.localPosition.ToString() : "（没了）")
                    + $"，与**调用前**那一档 {preLp} 逐位相同）—— 树与旁挂不一致时**只出声、不动我们的树**"
                    + "（⚠️ 如实：本场树里那颗与旁挂**是**一致的 ⇒ 这条只挡「复用时把它挪走/改写」那一类，"
                    + "不覆盖「不一致时出声」那半 —— 那半由下面那条日志断言咬）");
                bool saidA = false;
                foreach (var m in gotA) if (m != null && m.Contains("已经有一个同名子件")) saidA = true;
                // 🔴 **2026-10-14（A418②）：`nodes[]` 空 ⇒ 这一段**根本走不到** ⇒ 本条改成【按数据分两态】**
                //   （⛔ 不是放宽）：旁挂给了节点 ⇒ **必须出声**（原判据）；旁挂没给（重跑生成器之后就是这档）
                //   ⇒ 那就不该出声、也不该有任何一次复用。两态都真验，且**说清是「没这一档」而不是「没验」**。
                if (wantReused[0] > 0)
                    Check(saidA,
                          $"★ A514：复用那一档**出声**（{gotA.Count} 行日志里找「已经有一个同名子件」= "
                        + (saidA ? "有" : "没有") + "）—— ⛔ 不是静默跳过、也不是静默重建一份");
                else
                    Check(!saidA
                       && CardPresentation.ScenarioBlendableFactory.SceneAnimFxNodesReused == 0
                       && CardPresentation.ScenarioBlendableFactory.SceneAnimFxNodesCreated == 0,
                          "★ A514：旁挂 `nodes[]` **空**（重跑生成器之后的那一档）⇒ **没有可复用的对象**："
                        + $"复用 {CardPresentation.ScenarioBlendableFactory.SceneAnimFxNodesReused}（期望 0）·"
                        + $" 新建 {CardPresentation.ScenarioBlendableFactory.SceneAnimFxNodesCreated}（期望 0）·"
                        + $" 「已经有一个同名子件」出声 {saidA}（期望 False —— 没有那一档就不该出声）"
                        + " —— 判据 = 旁挂这一节本来就不再有节点（它们已经烘在 prefab 里）");
                UnityEngine.Object.DestroyImmediate(probeA);
            }

            // ---- ② 「旁挂没读到」⇒ **出声** + **不写 `_sceneAnimFxRoot`**（不谎报「已经建过」）----
            // 判据 = 同一实例**连调两次**：两次都必须说「一条都没建」；⛔ 第二次**不许**变成
            //   「这个战场实例已经建过」（那是**谎报** —— 事实是一条都没建）。
            // ⚠️ 这一档自检里造不出真环境（要 `Resources/EnvBlendables.json` 取不到）⇒ 用
            //   `ForceSceneStandaloneMissingForTest` 把**已经读到的结果**按掉（生产代码不读任何测试标志）。
            // 🧨 **改坏法**：把那一支里的 `_sceneAnimFxRoot = arenaRoot;` 加回去（= 原来那句「先写后失败」）
            //   ⇒ 第二次撞去重支 ⇒ 「一条都没建」只剩 **1** 条、且日志里出现「已经建过」⇒ 第 1 条红。
            if (prefab514 != null)
            {
                var probeB = UnityEngine.Object.Instantiate(prefab514);
                probeB.name = "A514 探针（旁挂缺失）";
                var gotB = new List<string>();
                Application.LogCallback hB = (string m, string st, LogType ty) => gotB.Add(m);
                int aB, bB;
                CardPresentation.ScenarioBlendableFactory.ForceSceneStandaloneMissingForTest(true);
                Application.logMessageReceived += hB;
                try
                {
                    aB = CardPresentation.ScenarioBlendableFactory.BuildSceneAnimFx(probeB.transform, "battlearena2");
                    bB = CardPresentation.ScenarioBlendableFactory.BuildSceneAnimFx(probeB.transform, "battlearena2");
                }
                finally
                {
                    Application.logMessageReceived -= hB;
                    CardPresentation.ScenarioBlendableFactory.ForceSceneStandaloneMissingForTest(false);
                }
                int missB = 0; bool claimB = false;
                foreach (var m in gotB)
                {
                    if (m == null) continue;
                    if (m.Contains("**一条都没建**")) missB++;
                    // 🔴 2026-10-14（#7 · A514②）：检测串**必须是「谎报」那句的原话** ——
                    //   `ScenarioBlendables.cs:2256` 的真谎报文案是「**这个战场实例已经建过** ⇒ 不重复建」；
                    //   而「一条都没建」那条警告的**正文自己**含「再进来时谎报「已经建过」」
                    //   （`ScenarioBlendables.cs:2277-2278`）⇒ 只搜「已经建过」会命中**说明文字**（自撞）。
                    if (m.Contains("这个战场实例已经建过")) claimB = true;
                }
                Check(aB == 0 && bB == 0 && missB == 2 && !claimB,
                      $"★ A514：旁挂没读到那一档**每次都出声、且不谎报**（两次各回 {aB} / {bB}；"
                    + $"「一条都没建」出现 {missB} 次（期望 **2**）；日志里有「已经建过」= {claimB}（期望 **False**））"
                    + " —— 原来那一支**先写 `_sceneAnimFxRoot` 再失败** ⇒ 第二次会打「这个战场实例已经建过」；"
                    + "且 `LoadSceneStandalone` 那两句 `LogError` 只在第一次读时打一遍 ⇒ 第二次起**一声不响**");
                // 恢复自证：把「没读到」关掉之后数据要能读回来（否则本段会把后面所有调用带偏）
                int backB = CardPresentation.ScenarioBlendableFactory.SceneStandaloneDataCount();
                Check(backB == 5,
                      $"★ A514：（自证）本段收工时旁挂**恢复**成 5 条（实得 {backB}）"
                    + " —— 这一条红了 = `ForceSceneStandaloneMissingForTest(false)` 没把状态还回去");
                UnityEngine.Object.DestroyImmediate(probeB);
            }

            // ---- ③ 「这一场旁挂里没有条目」⇒ **不是失败，但必须出声**（数据 / 缺口分开）----
            // 判据 = 原版那 7 个场景侧实例只在 `battlearena2` / `battlearena3` / `battlearenatauviorla` 三场
            //   ⇒ 拿 `battlearena1` 真调一次：它**一条都不该建**，但**必须说一句**（静默就分不出
            //   「这场本来没有」与「旁挂旧了」）。同族先例 = `BuildSoundTrack` 那句「原版这层本来就是空的」。
            // 🧨 **改坏法**：把那一支改回静默 `return 0`（删掉那句 `Debug.Log`）⇒ 抓不到 ⇒ 红。
            var p1 = Resources.Load<GameObject>("ArenaPrefabs/battlearena1");
            Check(p1 != null, "（前提）`Resources/ArenaPrefabs/battlearena1.prefab` 在（A514 段要用它）");
            if (p1 != null)
            {
                var probeC = UnityEngine.Object.Instantiate(p1);
                probeC.name = "A514 探针（这场没有条目）";
                var gotC = new List<string>();
                Application.LogCallback hC = (string m, string st, LogType ty) => gotC.Add(m);
                Application.logMessageReceived += hC;
                int cC;
                try { cC = CardPresentation.ScenarioBlendableFactory.BuildSceneAnimFx(probeC.transform, "battlearena1"); }
                finally { Application.logMessageReceived -= hC; }
                bool saidC = false;
                foreach (var m in gotC) if (m != null && m.Contains("这一场没有条目")) saidC = true;
                Check(cC == 0 && CardPresentation.ScenarioBlendableFactory.SceneAnimFxBuilt == 0 && saidC,
                      $"★ A514：旁挂里**没有这一场**时 ⇒ 一条不建（实得 {cC}）**但要说出来**"
                    + $"（{gotC.Count} 行日志里找「这一场没有条目」= {(saidC ? "有" : "没有")}）"
                    + " —— `battlearena1` 本来就没有（原版只在 arena2/3/tau 三场）⇒ 这一档**不是失败，"
                    + "但也不能静默**：静默就分不出「这场本来没有」与「旁挂旧了」");
                UnityEngine.Object.DestroyImmediate(probeC);
            }

            // ---- ④ `MakeAnimFx` 的「不建」支路**逐条出声**（原来三条都是静默 `return null`）----
            // 两条生产数据走不到（空 `targets` / 整条 `targets[]` 没有 `animfx`）⇒ 用**合成的** `item` 现造；
            //   走的仍是**同一份实现**（`MakeAnimFxForTest` = 那个 `private` 方法的一层壳，⛔ 不是复制一份逻辑）。
            // ⚠️ 第 3 条（`AddComponent` 回 null）**合成不出来**（要「组件加不上」那种环境）⇒ 见本段开头那条如实标。
            // 🧨 **改坏法**：把那两处 `Debug.LogWarning` 删掉（回到 `return null`）⇒ 抓到的警告数 **0**（期望 1）⇒ 红。
            var rootD = new GameObject("A514 MakeAnimFx 探针根");
            var resD = new EnvironmentApplier.SceneResolver(rootD.transform);
            int[] WarnD(System.Action act, params string[] frags)
            {
                var n = new int[frags.Length];
                Application.LogCallback h = (string m, string st, LogType ty) =>
                {
                    if (ty != LogType.Warning || m == null) return;
                    for (int i = 0; i < frags.Length; i++) if (m.Contains(frags[i])) n[i]++;
                };
                Application.logMessageReceived += h;
                try { act(); } finally { Application.logMessageReceived -= h; }
                return n;
            }
            const string FRAG_ARG = "收到**不全的参数**";      // 支路一：参数不全
            const string FRAG_NOFX = "的目标都没有";           // 支路二：`targets[]` 里没有 animfx
            const string FRAG_ADDC = "**回了 null**";          // 支路三：`AddComponent` 回 null（合成不出来，只当正例的对照）
            var wArg = WarnD(() => CardPresentation.ScenarioBlendableFactory.MakeAnimFxForTest(null, resD), FRAG_ARG);
            Check(wArg[0] == 1,
                  $"★ A514：`MakeAnimFx` 收到**不全的参数** ⇒ 出声（抓到 {wArg[0]} 条，期望 1）"
                + " —— 这一档原来静默 `return null`（而且 `item == null` 时还会**先 NRE**："
                + "`it.targets` 解引用空对象 ⇒ 连「不建」都说不出口）");
            var itEmpty = new EnvBlendables.Item
            { cls = "A514合成", owner = "(合成)", ownerLeaf = "(合成)", targets = new EnvBlendables.Target[0] };
            var wNoFx = WarnD(() => CardPresentation.ScenarioBlendableFactory.MakeAnimFxForTest(itEmpty, resD), FRAG_NOFX);
            Check(wNoFx[0] == 1,
                  $"★ A514：`MakeAnimFx` 的 `targets[]` 里**一条 `kind == \"animfx\"` 都没有** ⇒ 出声"
                + $"（抓到 {wNoFx[0]} 条，期望 1）—— 这一档原来静默 `return null`，调用方只会看到"
                + "「`MakeAnimFx` 建不出来」，而**原因不是「宿主缺」**（不点名就查不出是旁挂目标种类变了还是抄错了）");
            // 正例（⛔ 不是装饰）：真给一条**能建**的目标 —— 它必须**建出来**、且上面两条警告**一条都不许出现**。
            //   作用有二：① 证明 `MakeAnimFxForTest` 这层壳真的调到了 `MakeAnimFx`（不是恒 null 的假绿）
            //   ② 证明上面两条**不是**「反正抓不到警告」那种恒真判据。
            var hostGo = new GameObject("A514Host");
            hostGo.transform.SetParent(rootD.transform, false);
            var itOk = new EnvBlendables.Item
            {
                cls = "A514合成", owner = "(合成)", ownerLeaf = "A514Host",
                targets = new[]
                {
                    new EnvBlendables.Target { kind = "animfx", leaf = "A514Host",
                                               fields = new[] { new EnvBlendables.TargetField { k = "preventDestroy", f = 1f } } },
                },
            };
            AnimFXController madeD = null;
            var wOk = WarnD(() => { madeD = CardPresentation.ScenarioBlendableFactory.MakeAnimFxForTest(itOk, resD); },
                            FRAG_ARG, FRAG_NOFX, FRAG_ADDC);
            Check(madeD != null && wOk[0] == 0 && wOk[1] == 0 && wOk[2] == 0,
                  $"★ A514：（正例）给一条**能建**的 `animfx` 目标 ⇒ 真建出来（`{(madeD != null ? madeD.name : "null")}`）、"
                + $"且三条「不建」的出声**一条都没响**（{wOk[0]} / {wOk[1]} / {wOk[2]}，期望 0/0/0）"
                + " —— 这一条同时挡住「探针根本没调到 `MakeAnimFx`」与「断言恒真」两种假绿"
                + "（⚠️ 它自己会带出两条**别的**警告：`sounds.count` / `modules.count` 旁挂缺 —— 那是既有的出声口，不在本判据里）");
            UnityEngine.Object.DestroyImmediate(rootD);
        }

        // ---------------- 🆕 2026-10-13（A553 · 本段由 A606 落到宿主里）：场景侧重试闩 —— 「只在成功时置位」 ----------------
        // 判据 = 红线「不许静默失败」+「失败可自愈」：`LoadSceneStandalone()` 的 `_sceneStanTried`
        //   原来在【第二条语句】就置位 ⇒ 读失败也记成「读过了」⇒ 同一个进程内**再也不会重读**
        //   （重生成旁挂 / `AssetDatabase.Refresh` 都救不回来，只能重进编辑器 = 域重载）。
        // ⚠️ **为什么非要造一只新开关**：`ForceSceneStandaloneMissingForTest(false)` 只按掉两节 ——
        //   那样下一次调用会**真读成功**，「改前 / 改后」读数**完全一样**（都是 5）⇒ **分不出两种状态**。
        //   必须让失败**真的发生**（`ForceSceneStandaloneLoadFailForTest`）才验得了「可重试」。
        // 🧨 **改坏法**：① 把 `LoadSceneStandalone()` 里那句 `_sceneStanTried = true;` **挪回第二行**
        //   （= 改前）⇒ 真读次数 **+1**（不是 4）、关掉开关后 `SceneStandaloneDataCount()` 仍是 **-1**（不是 5）⇒ 红；
        //   ② 删掉 `NoteSceneStanFail` 里那一句 `if (!_sceneStanNoted.Add(key)) return;` ⇒ 失败出声 **3 条**（不是 1）⇒ 红。
        // ⚠️ **落点**（A606 点名的两条）：**A514 段之后**（本段**不复验** A431 那些数字，也⛔不依赖 `driver`）·
        //   **整轮收尾之前** —— 收尾那段要 `OpenScene`，本段全程只读静态缓存 + 日志窗口，不碰任何现场。
        //   ✅ 自带自证：本段最后一步必须把状态**还原成「已读到 5 条」**（`backE == 5` 那条就是它）。
        //   ⛔ 开关必须放 `finally` 里关掉（写不写对决定后面所有 `BuildSceneAnimFx` 走不走失败路）。
        // 出处 = `资料/普查产出_1013/WSmall2_场景重试闩.md` §五（成品代码逐字照贴，⛔ 一个字没改）。
        {
            int a0 = CardPresentation.ScenarioBlendableFactory.SceneStandaloneLoadAttempts;
            var gotE = new List<string>();
            Application.LogCallback hE = (string m, string st, LogType ty) => { if (m != null) gotE.Add(m); };
            int r1 = 0, r2 = 0, r3 = 0, backE = -99;
            CardPresentation.ScenarioBlendableFactory.ForceSceneStandaloneMissingForTest(false);   // 清闩 + 按掉两节
            CardPresentation.ScenarioBlendableFactory.ForceSceneStandaloneLoadFailForTest(true);  // 让读**真的**失败
            Application.logMessageReceived += hE;
            try
            {
                r1 = CardPresentation.ScenarioBlendableFactory.SceneStandaloneDataCount();
                r2 = CardPresentation.ScenarioBlendableFactory.SceneStandaloneDataCount();
                r3 = CardPresentation.ScenarioBlendableFactory.SceneStandaloneDataCount();
                CardPresentation.ScenarioBlendableFactory.ForceSceneStandaloneLoadFailForTest(false);
                backE = CardPresentation.ScenarioBlendableFactory.SceneStandaloneDataCount();      // 关掉开关 ⇒ 该能读回来
            }
            finally
            {
                Application.logMessageReceived -= hE;
                CardPresentation.ScenarioBlendableFactory.ForceSceneStandaloneLoadFailForTest(false);
            }
            int failE = 0, healE = 0;
            foreach (var m in gotE)
            {
                if (m.Contains("**失败（资源取不到）**")) failE++;
                if (m.Contains("**重试成功**")) healE++;
            }
            int dE = CardPresentation.ScenarioBlendableFactory.SceneStandaloneLoadAttempts - a0;
            Check(r1 == -1 && r2 == -1 && r3 == -1 && dE == 4 && failE == 1 && backE == 5,
                  $"★ A553：场景侧旁挂**失败可重试**（三次各回 {r1} / {r2} / {r3}；**真读次数 +{dE}**（期望 **4**）· "
                + $"「失败（资源取不到）」出声 **{failE}** 条（期望 **1** = 去重）· 关掉开关后读到 **{backE}** 条（期望 **5**））"
                + " —— 改前那一版**进了函数就置位** ⇒ 第 2 次起不再重读（+1）、关掉开关后仍是 **-1**（**永久放弃**）");
            Check(healE == 1,
                  $"★ A553：「**重试成功**」那一句**出声一次**（抓到 {healE} 条，期望 1）—— 自愈必须**看得见**"
                + "（⛔ 不是「悄悄好了」，那也算静默失败）；它只在**失败过之后**第一次读成功时响");
        }

        // ---------------- 🆕 2026-10-13（A463）：两件原版组件（`TouchInputManager` + `BattleCameraSreenSize`）----------------
        // 判据 = **`d:/2/tools/decomp_full/` 里两件各自的方法体**（逐句）：
        //   · `TouchInputManager__{Awake, Update, UpdateDrag, Toggle}`（4 个）
        //   · `BattleCameraSreenSize__{Start, ResolutionHasChanged, Initialize, DoLensShift}` + 四个闭包（8 个）
        // 旁证 = `d:/2/tools/il2cpp_out/dump.cs`（字段名/偏移/ctor 值）·
        //        `assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4011.json`（`forceMobileInput: 0`）
        //        与 `MonoBehaviour_4208.json`（三个序列化值）·
        //        `script.json` 的 `ScriptMetadataMethod`（那几个 `DAT_` 解出来是**方法指针**：
        //        `Signal.Register<ScreenResolutionChangeSignal>()` / `ResolutionHasChanged()` / 四个闭包）。
        // ⚠️ **两件各自起一个临时探针**（一个 GO + 一个相机 rig），收工全部 `DestroyImmediate`
        //   + 把静态事件/静态格还回去，并**回读自证**（见每小节末尾那条）。
        // 🧨 主改坏法（逐条附在各自的断言上）：① 透镜位移的 x 不归零；② `instant` 那支不再抬 Action；
        //   ③ `TouchDragDelta` 取 `touch.position` 而不是 `deltaPosition`（= 本件最大的那处反编译坑）；
        //   ④ 捏合那一格符号取反；⑤ `vcam` 的 setter 不推给真相机；⑥ `UpdateDrag` 的 viewport 分母对调。
        {
            // ================================================================
            //  一、`TouchInputManager`（原版 TypeDefIndex 2299）—— 原版输入层唯一的真相源
            // ================================================================
            // ⚠️ 探针自建：`Ensure()` 那条路建的是**另一个** GO ⇒ 本段先清静态格、再自己 `AddComponent`。
            TouchInputManager.ResetStaticsForTest();
            var timGo = new GameObject("A463Probe_TouchInputManager");
            var tim = timGo.AddComponent<TouchInputManager>();
            try
            {
                // ⚠️ `AddComponent` 跑不跑 `Awake` 本工程**没有定论**（A321 订正）⇒ 显式补一次
                //    （同族先例 = 那条订正的「一律显式补一次 `Build()`」）。跑没跑如实记在消息里。
                bool awakeRan = TouchInputManager.Current == tim;
                if (!awakeRan) tim.Bootstrap();
                Check(TouchInputManager.Current == tim,
                      "（前提）★ A463：静态格清干净之后 `AddComponent` ⇒ `Instance` 就是它（原版 `+0x30`）"
                    + $"（`Awake` 自己跑过没有：{awakeRan} —— 没跑的话本段显式补了一次 `Bootstrap()`）");

                // ① 单例守卫：后来者**抢不走** `Instance`（原版 `Awake`：`if (Instance != null && Instance != this)
                //    { Destroy(gameObject); return; }`）。
                //    🧨 改坏法：把那两行守卫删掉 ⇒ `Instance` 变成第二个 ⇒ 红。
                //    ⚠️ 原版那句 `Object.Destroy` 在批处理下**不生效**（本仓已知：没有帧循环）⇒ 我们**只断
                //       「`Instance` 没被抢走」**（那正是守卫的语义）；「那个 GO 真没了」要帧循环才验得了，如实标。
                var timGoDup = new GameObject("A463Probe_TouchInputManager_dup");
                var timDup = timGoDup.AddComponent<TouchInputManager>();
                // 同样的显式补一次 —— ⛔ 不补的话这一条在「`Awake` 没跑」那一档会**恒真**（假绿）。
                if (TouchInputManager.Current != timDup) timDup.Bootstrap();
                Check(TouchInputManager.Current == tim && timDup != tim,
                      "★ A463：第二个实例**抢不走** `Instance` —— 原版那一支是 `Destroy(gameObject); return;`；"
                    + "🧨 删掉守卫 ⇒ 红");
                UnityEngine.Object.DestroyImmediate(timGoDup);

                // ② `Toggle(bool)` = 一句 `Behaviour.set_enabled` —— **它不清那 8 格**（见那件文件头 E）。
                //    原版拿它**停摆**：`BattleSettingsWindow.Open` 传 `false` ⇒ `Update` 不跑 ⇒ 8 格**冻在上一帧**。
                //    🧨 改坏法：在 `Toggle` 里「顺手」加清零 —— ③ 那条的对照就没了（这一条本身验不出来，
                //       如实标：它只钉 `enabled` 被真的翻动）。
                tim.Toggle(false);
                bool toggleOff = !tim.enabled;
                tim.Toggle(true);
                Check(toggleOff && tim.enabled,
                      "★ A463：`Toggle(false/true)` 就是 `enabled = false/true`（原版逐句一行）"
                    + " —— ⛔ 原版**不清零**那 8 格（`Update` 只是停跑）");

                // ③ `UpdateDrag` 纯算式（原版 `private` ⇒ 走 `UpdateDragForTest`，**同一个体**）。
                //    期望值**在本文件里独立算**（⛔ 不读实现）。
                //    🧨 改坏法：`TouchDragDeltaViewport` 的两个分母对调（x 除高、y 除宽）⇒ 红
                //      （前提：`Screen.width != Screen.height`，下面那条前提就断它）。
                Check(Screen.width != Screen.height,
                      $"（前提）★ A463：`Screen.width({Screen.width}) != Screen.height({Screen.height})`"
                    + " —— 相等的话「viewport 分母对调」那条就分不出来了");
                tim.LastTouchPositionForTest = new Vector2(100f, 50f);
                tim.UpdateDragForTest(new Vector2(130f, 90f));
                var wantVp = new Vector2(30f / Screen.width, 40f / Screen.height);
                Check(Mathf.Abs(TouchInputManager.TouchDragDelta.x - 30f) < 1e-3f
                   && Mathf.Abs(TouchInputManager.TouchDragDelta.y - 40f) < 1e-3f
                   && TouchInputManager.IsDragging
                   && Vector2.Distance(TouchInputManager.TouchDragDeltaViewport, wantVp) < 1e-6f
                   && Vector2.Distance(tim.LastTouchPositionForTest, new Vector2(130f, 90f)) < 1e-4f,
                      $"★ A463：`UpdateDrag` = 差分 + `IsDragging` + **viewport 那一除** + 基准前移"
                    + $"（实得 delta {TouchInputManager.TouchDragDelta} · viewport {TouchInputManager.TouchDragDeltaViewport}"
                    + $" · 期望 {wantVp} · 基准 {tim.LastTouchPositionForTest}）"
                    + " —— 🧨 两个分母对调 ⇒ 红");
                tim.UpdateDragForTest(new Vector2(130f, 90f));
                Check(!TouchInputManager.IsDragging && TouchInputManager.TouchDragDelta == Vector2.zero,
                      "★ A463：原地不动 ⇒ `IsDragging == false`（原版 = `0 < |delta|`）");

                // ④ `Tick` 的**桌面路**（原版 `if (!forceMobileInput && !isMobilePlatform && (TouchPressed || TouchPressedSecondary))
                //    UpdateDrag(TouchPosition);`）：delta = 本帧指针 − 上一帧基准。
                //    🧨 改坏法：把 `if (TouchPressed || TouchPressedSecondary)` 那道门删掉 ⇒ 下面第二段（没人按）红。
                TouchInputManager.MobilePlatformOverride = false;
                tim.ForceMobileInputForTest = false;
                tim.LastTouchPositionForTest = new Vector2(150f, 250f);
                TouchInputManager.RawOverride = new TouchInputManager.RawInput
                { PointerPos = new Vector2(200f, 300f), LeftHeld = true };
                tim.Tick();
                Check(TouchInputManager.TouchPressed && !TouchInputManager.TouchPressedSecondary
                   && Vector2.Distance(TouchInputManager.TouchPosition, new Vector2(200f, 300f)) < 1e-4f
                   && Vector2.Distance(TouchInputManager.TouchDragDelta, new Vector2(50f, 50f)) < 1e-4f
                   && TouchInputManager.IsDragging,
                      $"★ A463：桌面路 —— 左键按着 ⇒ `TouchPressed` 真、`TouchPosition` = 指针、"
                    + $" `TouchDragDelta` = 指针 − 上一帧基准（实得 {TouchInputManager.TouchDragDelta}，期望 (50,50)）");
                tim.LastTouchPositionForTest = new Vector2(200f, 300f);       // 上一条已经把它推到 (200,300)
                TouchInputManager.RawOverride = new TouchInputManager.RawInput
                { PointerPos = new Vector2(999f, 999f) };                     // 一个键都没按
                tim.Tick();
                Check(!TouchInputManager.TouchPressed && TouchInputManager.TouchDragDelta == Vector2.zero
                   && !TouchInputManager.IsDragging
                   && Vector2.Distance(TouchInputManager.TouchPosition, new Vector2(999f, 999f)) < 1e-4f,
                      "★ A463：**没人按键 ⇒ 不更新拖拽**（`TouchPosition` 照更新、delta 归零）"
                    + " —— 🧨 把那道 `if (TouchPressed || TouchPressedSecondary)` 删掉 ⇒ 红");

                // ⑤ `forceMobileInput` 那两层门：真时**桌面路整段被跳过**（原版是
                //    `if (!forceMobileInput) { if (!isMobilePlatform) { … } }`，**不是**「两条都走」）。
                //    判据：`forceMobileInput = true` + 指针按着 + **零根手指** ⇒ delta 必须是 0（桌面路没跑），
                //    而 `TouchPosition` 照样是本帧指针（那是第 ⑧ 句，无条件）。
                //    🧨 改坏法：去掉 `!forceMobileInput` 那层 ⇒ 桌面路跑起来 ⇒ delta = (10,10) − 基准 ⇒ 红。
                tim.ForceMobileInputForTest = true;
                tim.LastTouchPositionForTest = new Vector2(0f, 0f);
                TouchInputManager.RawOverride = new TouchInputManager.RawInput
                { PointerPos = new Vector2(10f, 10f), LeftHeld = true, TouchCount = 0 };
                tim.Tick();
                Check(TouchInputManager.TouchDragDelta == Vector2.zero
                   && Vector2.Distance(TouchInputManager.TouchPosition, new Vector2(10f, 10f)) < 1e-4f,
                      $"★ A463：`forceMobileInput` 真 ⇒ **桌面路整段跳过**（实得 delta {TouchInputManager.TouchDragDelta}，"
                    + "期望 (0,0)；`TouchPosition` 照样更新）—— 🧨 去掉那层门 ⇒ 红");
                tim.ForceMobileInputForTest = false;

                // ⑥ `ScrollDelta` 那一格取的是 **`.y`**（不是 `.x`）。
                //    判据链：`.c` 那一句是 `*(statics+0x20) = extraout_var`（看不出取的是哪一半），
                //    而**指令流** `0x79BDDC` 是 `movss xmm0,[rbp+0x124]` —— `mouseScrollDelta` 的 x/y 分别落在
                //    `[rbp+0x120]`/`[rbp+0x124]` ⇒ 拿的是 **y**。这里喂一个 x≠y 的读数来钉它。
                //    ⚠️ 如实标：**「`.y` 是从新输入系统的哪个字段来的」那一跳在本轮验不了**
                //      （批处理里 `Mouse.current == null` ⇒ 走的永远是注入那条）⇒ 只有代码审查 / 真 Play 能验。
                //    🧨 改坏法：写成 `raw.Scroll.x` ⇒ 得 5 ⇒ 红。
                TouchInputManager.RawOverride = new TouchInputManager.RawInput { Scroll = new Vector2(5f, 7f) };
                tim.Tick();
                Check(Mathf.Abs(TouchInputManager.ScrollDelta - 7f) < 1e-6f,
                      $"★ A463：`ScrollDelta` = **`.y`**（喂的是 (x=5, y=7)，实得 {TouchInputManager.ScrollDelta}，期望 7）"
                    + " —— 🧨 换成 `.x` ⇒ 得 5 ⇒ 红");

                // ⑦ 滚轮量纲（Windows 一格 = 120）**两处口径一致**。
                //    ⚠️ 如实标：`CombatCameraZoom.ScrollUnitsPerNotch` 是 `const` 转发 —— 这一条**分不出**
                //      「转发」与「各写一份字面量」（两种写法在这一格上等价）；它只钉「两处的值仍然一致」。
                //    🧨 改坏法：把任一处改成 100 ⇒ 红。
                Check(Mathf.Abs(TouchInputManager.ScrollUnitsPerNotch - 120f) < 1e-9f
                   && Mathf.Abs(CombatCameraZoom.ScrollUnitsPerNotch - TouchInputManager.ScrollUnitsPerNotch) < 1e-9f,
                      $"★ A463：滚轮量纲 = 120，且 `CombatCameraZoom.ScrollUnitsPerNotch` 与它一致"
                    + $"（{TouchInputManager.ScrollUnitsPerNotch} / {CombatCameraZoom.ScrollUnitsPerNotch}）"
                    + " —— 🧨 任一处改成别的数 ⇒ 红");

                // ⑧ 双指 ⇒ `ScrollDelta = −(本帧两指距离 − 上一帧距离)`。
                //    🔴 这一条的**存在理由**：原版那两句在 `.c` 里长成 `FUN_180789ad0(…)`（**看着像「调了个 void、
                //       结果丢了」**），实际是 `set_ScrollDelta`（按指令流认出来的，见那件文件头 ③）
                //       ⇒ **照 `.c` 抄会把整段捏合缩放丢掉**（一个静默的输入缺口）。
                //    期望值：两指 (0,0)/(3,4) ⇒ 距离 5；上一帧 2 ⇒ `ScrollDelta = −3`。
                //    🧨 改坏法：① 写成 `last − dist` 或 `PinchSign` 改 +1 ⇒ 符号反 ⇒ 红。
                TouchInputManager.MobilePlatformOverride = true;
                tim.LastTwoFingerDistanceForTest = 2f;
                TouchInputManager.RawOverride = new TouchInputManager.RawInput
                {
                    TouchCount = 2,
                    Touch0Pos = new Vector2(0f, 0f), Touch0Phase = 1,
                    Touch1Pos = new Vector2(3f, 4f), Touch1Phase = 1,
                };
                tim.Tick();
                Check(Mathf.Abs(TouchInputManager.ScrollDelta - (-3f)) < 1e-3f
                   && Vector2.Distance(TouchInputManager.TwoFingerMidPoint, new Vector2(1.5f, 2f)) < 1e-4f
                   && Vector2.Distance(TouchInputManager.TouchPosition, new Vector2(1.5f, 2f)) < 1e-4f
                   && TouchInputManager.TouchPressedSecondary
                   && Mathf.Abs(tim.LastTwoFingerDistanceForTest - 5f) < 1e-3f,
                      $"★ A463：双指张开 ⇒ `ScrollDelta` = **−(距离差)** = −3（实得 {TouchInputManager.ScrollDelta}）·"
                    + $" 中点 `(1.5, 2)`（两指中点同时当 `TouchPosition`）· `TouchPressedSecondary` 真 ·"
                    + $" 基准更新成 5（实得 {tim.LastTwoFingerDistanceForTest}）"
                    + " —— 🧨 符号取反 ⇒ 红；照 `.c` 把它当「void 调用」整段丢掉 ⇒ 得 0 ⇒ 红");

                // ⑨ 基准还是 0（第一帧双指）⇒ **不产** `ScrollDelta`（原版 `if (0 &lt; lastTwoFingerDistance) { … }`）。
                //    🧨 改坏法：把那道门删掉 ⇒ 第一次双指就产一个假的大 delta ⇒ 红。
                tim.LastTwoFingerDistanceForTest = 0f;
                TouchInputManager.RawOverride = new TouchInputManager.RawInput
                {
                    TouchCount = 2,
                    Touch0Pos = new Vector2(0f, 0f), Touch0Phase = 1,
                    Touch1Pos = new Vector2(3f, 4f), Touch1Phase = 1,
                };
                tim.Tick();
                Check(Mathf.Abs(TouchInputManager.ScrollDelta) < 1e-6f
                   && Mathf.Abs(tim.LastTwoFingerDistanceForTest - 5f) < 1e-3f,
                      $"★ A463：基准是 0（第一帧双指）⇒ **不产 `ScrollDelta`**（实得 {TouchInputManager.ScrollDelta}，期望 0），"
                    + $" 但基准照样记成 5（实得 {tim.LastTwoFingerDistanceForTest}）"
                    + " —— 🧨 删掉 `if (0f < lastTwoFingerDistance)` 那道门 ⇒ 红");

                // ⑩ 单指 ⇒ `TouchDragDelta` 取的是 **`Touch.deltaPosition`**，`TouchPosition` 才是 `touch.position`。
                //    🔴 这一条咬的正是那件文件头的 ④：`.c` 里两个读取点被 Ghidra 分别认成
                //       `System.Nullable&lt;Vector2&gt;.GetValueOrDefault` 与 `NativeArray&lt;Vector2&gt;.Enumerator.get_Current`
                //       （**两个都不是真的**）；按**指令流** `0x79C598` 的 `call 0x183a330` 才是判据
                //       —— `all_methods.txt` 里那是 `UnityEngine.Touch$$get_deltaPosition`。
                //       ⇒ **照 `.c` 抄会把绝对位置当位移**（静默错）。
                //    🧨 改坏法：把 `TouchDragDelta = raw.Touch0Delta` 换成 `raw.Touch0Pos` ⇒ 红。
                TouchInputManager.RawOverride = new TouchInputManager.RawInput
                {
                    TouchCount = 1,
                    Touch0Pos = new Vector2(400f, 300f), Touch0Delta = new Vector2(7f, -5f), Touch0Phase = 1,
                };
                tim.Tick();
                var wantVp2 = new Vector2(7f / Screen.width, -5f / Screen.height);
                Check(Vector2.Distance(TouchInputManager.TouchDragDelta, new Vector2(7f, -5f)) < 1e-4f
                   && Vector2.Distance(TouchInputManager.TouchPosition, new Vector2(400f, 300f)) < 1e-4f
                   && TouchInputManager.TouchPressed
                   && Vector2.Distance(TouchInputManager.TouchDragDeltaViewport, wantVp2) < 1e-6f,
                      $"★ A463：单指 ⇒ `TouchDragDelta` 取的是 **`Touch.deltaPosition`**（实得 {TouchInputManager.TouchDragDelta}，"
                    + $"期望 (7,−5)），`TouchPosition` 才是 `touch.position`（实得 {TouchInputManager.TouchPosition}，期望 (400,300)）"
                    + " —— 🔴 这一条挡住「照 `.c` 把绝对位置当位移」（Ghidra 把那两处认反了，见文件头 ④）");
            }
            finally
            {
                // 收工：**先**把注入与静态格还回去，再销毁探针（⛔ 不留一个指向已销毁组件的 `Instance`）。
                TouchInputManager.RawOverride = null;
                TouchInputManager.MobilePlatformOverride = null;
                TouchInputManager.ResetStaticsForTest();
                UnityEngine.Object.DestroyImmediate(timGo);
            }
            // 回读自证（⛔ 不是装饰）：本段把两个**静态**东西改过（注入口 + 8 个静态格）——
            //   这一条红了 = 本段会把后面 / 下一次调用带偏。
            Check(TouchInputManager.Current == null
               && TouchInputManager.TouchDragDelta == Vector2.zero
               && !TouchInputManager.IsDragging
               && Mathf.Abs(TouchInputManager.ScrollDelta) < 1e-9f
               && TouchInputManager.RawOverride == null
               && TouchInputManager.MobilePlatformOverride == null,
                  "★ A463：（自证）`TouchInputManager` 的静态格与两个注入口**全部还回出厂**、`Instance` 归 null");

            // ================================================================
            //  二、`BattleCameraSreenSize`（原版 TypeDefIndex 493）—— 「分辨率变了 ⇒ 重新取景」
            // ================================================================
            // ⚠️ 跑在**临时 rig** 上（自建相机 + 自建 `CombatAutoZoom`），几何由本段钉死 ⇒ 取景可算；
            //    而且**不碰真战场相机**（A422 那一段后面还要用它）。
            //    **这不是「换一条实现」**：rig 上跑的是同一个 `CombatCameraZoom` / `BattleCameraSreenSize`。
            // 🔴 2026-10-14（#8）：**基线 = 同一次运行的增量**（收工断「与本段进来之前一样」）。
            //   ⛔ 别写死 `== 0`：场上**还有主战场那台合法订阅者** —— `BattleDriver` 的 `CombatAutoZoom`
            //   由 `gameObject.AddComponent` 加在 **`sceneRoot`** 上、`Initialize` 里注册过
            //   （`Battle/BattleDriver.cs:6125` → `CombatAutoZoom.Initialize()`），它**不随**
            //   `DestroyImmediate(driver)`（销毁的是**组件**）消失 ⇒ 本段进来时它已经注册着 1 台。
            int sigBefore463 = BattleCameraSreenSize.ResolutionSignalSubscriberCountForTest;
            GameObject rig463 = new GameObject("A463Probe_rig");
            BattleCameraSreenSize rig463Ss = null;
            try
            {
                var rig463CamGo = new GameObject("A463Probe_cam");
                rig463CamGo.transform.SetParent(rig463.transform, false);
                rig463CamGo.transform.localPosition = new Vector3(0f, 0f, -5f);
                rig463CamGo.transform.localRotation = Quaternion.identity;
                var rig463Cam = rig463CamGo.AddComponent<Camera>();
                // 光学参数照 `Battle/ArenaSceneState.cs:111-117` 那一组（与 A422 的 rig 同一档）
                rig463Cam.orthographic = false;
                rig463Cam.usePhysicalProperties = true;
                rig463Cam.focalLength = 28f;
                rig463Cam.sensorSize = new Vector2(41.5f, 24f);
                rig463Cam.gateFit = Camera.GateFitMode.Horizontal;
                rig463Cam.aspect = 16f / 9f;

                var rig463Az = rig463.AddComponent<CombatAutoZoom>();
                rig463Az.boardCamera = rig463Cam;
                rig463Az.ResetForBattle();
                rig463Az.Initialize();          // ← 这一句会建出 `CombatCameraZoom` **并** `BattleCameraSreenSize`
                var rig463Cz = rig463Az.CameraZoomForTest;
                var rig463SsLocal = rig463Cz != null ? rig463Cz.battleCameraScreenSize : null;
                rig463Ss = rig463SsLocal;
                if (rig463SsLocal == null)
                {
                    // 不许静默：下面十来条一条都验不了。
                    Check(false, "★ A463：`BattleCameraSreenSize` **没建出来** ⇒ 本段后面十几条一条都验不了"
                               + "（原版它是场景里序列化好的一件；我们由 `CombatCameraZoom.Initialize` 补建 —— 见那里注释）");
                }
                else
                {
                    // ㈠ 三个序列化字段取的是**场景那一档**（判据 = `MonoBehaviour_4208.json`）。
                    //    🔴 特意咬那处 **ctor ≠ 场景**：`sensorSizeXBigScreen` 的 ctor 是 **41.5**（`0x42260000`）、
                    //       而 13 个战场里序列化的都是 **41.0**。
                    //    🧨 改坏法：照 ctor 那一档抄（41.5）⇒ 红。
                    Check(Mathf.Abs(rig463SsLocal.SensorSizeXSmallTest - 37f) < 1e-4f
                       && Mathf.Abs(rig463SsLocal.SensorSizeXBigScreenTest - 41f) < 1e-4f
                       && Mathf.Abs(rig463SsLocal.AnimTimeTest - 3f) < 1e-4f,
                          $"★ A463：那三个序列化字段取的是**场景那一档**（实得 {rig463SsLocal.SensorSizeXSmallTest} /"
                        + $" {rig463SsLocal.SensorSizeXBigScreenTest} / {rig463SsLocal.AnimTimeTest}，期望 37 / **41** / 3）"
                        + " —— `sensorSizeXBigScreen` 的 **ctor 是 41.5**（`0x42260000`）⇒ 照 ctor 抄 ⇒ 红"
                        + "（⚠️ 如实：这三个字段在**本 build 里一个方法都不读** —— 我们保留只为字段表对齐，不是它们在起作用）");

                    // ㈡ 接线：本件由 `CombatCameraZoom` 建出来、四格引用逐格接齐、两个 `Action` 都被订上
                    //    （订法 = 原版 `Awake` 那两段 `Delegate.Combine`：`+0x58` 配 `OnCameraSensorSizeChanged`、
                    //     `+0x50` 配 `OnCameraShiftChanged`）。
                    //    🧨 改坏法：`SubscribeScreenSizeSource` 里少订一条 / 少接一格引用 ⇒ 红。
                    Check(rig463Cz.battleCameraScreenSize == rig463SsLocal
                       && rig463SsLocal.combatCameraZoom == rig463Cz
                       && rig463SsLocal.boardCamera == rig463Cam
                       && rig463SsLocal.cameraVerticalFramer == rig463Az
                       && rig463SsLocal.vcamAsCombatCameraZoom == rig463Cz
                       && rig463SsLocal.OnCameraSensorSizeChanged != null
                       && rig463SsLocal.OnCameraShiftChanged != null,
                          "★ A463：那件组件由 `CombatCameraZoom.Initialize` 建出来、**四格引用逐格接齐**"
                        + "（`boardCamera` · `cameraVerticalFramer` · `combatCameraZoom` · `vcam`），"
                        + "且 `CombatCameraZoom` **真的订到了它那两个 `Action` 上**（= 原版 `Awake` 的两段 `Delegate.Combine`）"
                        + " —— 🧨 少接一格 / 少订一条 ⇒ 红");

                    // ㈢ `Initialize(instant: true)`：抬 `OnCameraShiftChanged`，且 **shift.x 被强制成 0**
                    //    （原版两处终值都是 `(ulonglong)y << 0x20`，低 32 位 = 0）。
                    //    期望值**在断言里独立算一遍**：`CalculateFraming(GetMaxZoomLevel(1))` 给的 `desiredLensShift`
                    //    只留 `.y`。🔴 为了让「x 归零」这条**真的可分**，先把相机的 `lensShift.x` 摆成**非 0**
                    //    （取景器会把当前 x 原样带出来 —— 见 `CombatAutoZoom.CalculateFraming` 那条注释）
                    //    ⇒ 下面另起一条**前提**断它确实非 0（不然「x 归零」恒真）。
                    //    🧨 改坏法：写成 `new Vector2(y, y)` ⇒ `.x` 不再是 0 ⇒ 红。
                    rig463Cam.lensShift = new Vector2(0.5f, 0f);
                    rig463Cam.sensorSize = new Vector2(41.5f, 24f);
                    Vector2 wantSs463, wantShift463;
                    bool framing463 = rig463Az.CalculateFraming(rig463Cz.GetMaxZoomLevel(1f),
                                                               out wantSs463, out wantShift463);
                    Check(framing463 && Mathf.Abs(wantShift463.x - 0.5f) < 1e-4f,
                          $"（前提）★ A463：取景器把相机当前的 `lensShift.x = 0.5` **原样带出来**（实得 {wantShift463.x:F4}）"
                        + " —— 它是下面「x 被强制成 0」那条的**可分性前提**（不摆这个非 0 值，那一条恒真）");

                    Vector2 capShift = new Vector2(999f, 999f), capSs = new Vector2(999f, 999f);
                    int shiftHits = 0, ssHits = 0;
                    System.Action<Vector2> onShift = v => { capShift = v; shiftHits++; };
                    System.Action<Vector2> onSs = v => { capSs = v; ssHits++; };
                    rig463SsLocal.OnCameraShiftChanged += onShift;
                    rig463SsLocal.OnCameraSensorSizeChanged += onSs;
                    try
                    {
                        rig463SsLocal.Initialize(true);
                        Check(Mathf.Abs(capShift.x) < 1e-6f
                           && Mathf.Abs(capShift.y - wantShift463.y) < 1e-4f
                           && shiftHits == 1,
                              $"★ A463：`Initialize(instant: true)` 抬 `OnCameraShiftChanged(shift)`，"
                            + $" 且 **shift.x 被强制成 0**（实得 ({capShift.x:F6}, {capShift.y:F6})，"
                            + $" 期望 (0, {wantShift463.y:F6})；抬了 {shiftHits} 次）"
                            + " —— 判据 = 原版那两处终值都是 `(ulonglong)y << 0x20`；🧨 写成 `new Vector2(y, y)` ⇒ 红");
                        Check(Mathf.Abs(capSs.x - wantSs463.x) < 1e-3f
                           && Mathf.Abs(capSs.y - wantSs463.y) < 1e-3f
                           && ssHits == 1,
                              $"★ A463：同一条路也抬 `OnCameraSensorSizeChanged(newSensorSize)`，载荷 = 取景器算出来的那个"
                            + $"（实得 {capSs}，期望 {wantSs463}；抬了 {ssHits} 次）—— 🧨 删掉那一句 Invoke ⇒ 红");

                        // ㈣ 信号那条路：`ResolutionHasChanged()` **就是** `Initialize(instant: true)`
                        //    （原版那个方法**整整两行**：`BattleCameraSreenSize__Initialize(param_1, 1, 0)`），
                        //    而**实况走的就是它**（`Start` 把 `ResolutionHasChanged` 注册到
                        //    `ScreenResolutionChangeSignal` 上；全反编译里 `Initialize` **只有这一个调用点**）。
                        //    判据：抬一次信号 ⇒ `InitializeCount` +1 **且** Action 又抬一次。
                        //    🧨 改坏法：把 `ResolutionHasChanged()` 改成 `Initialize(false)`（补间那条）⇒
                        //       `InitializeCount` 照样 +1，但 Action **不抬**（补间那条路不抬）⇒ 红。
                        int cnt0 = rig463SsLocal.InitializeCount, hit0 = shiftHits;
                        rig463SsLocal.RegisterResolutionSignal();        // 幂等（批处理下 `Start` 跑不跑未定论）
                        BattleCameraSreenSize.NotifyScreenResolutionChanged();
                        Check(rig463SsLocal.InitializeCount == cnt0 + 1 && shiftHits == hit0 + 1,
                              $"★ A463：`ResolutionHasChanged()` = `Initialize(instant: true)` —— 抬一次那条信号 ⇒"
                            + $" `Initialize` 跑到第 {rig463SsLocal.InitializeCount} 次、Action 也抬到第 {shiftHits} 次"
                            + "（**两条都 +1**）—— 🧨 改成 `Initialize(false)` ⇒ 第二条不涨 ⇒ 红");

                        // ㈤ 负例（⛔ 不是装饰）：`instant: false` 那条路**不抬 Action**、但**真建了补间**
                        //    （原版那两个 `if/else` 只走一支）。这一条同时挡住「上面那条是恒真」。
                        //    🧨 改坏法：把 `if (instant)` 两支写反 ⇒ 红。
                        rig463SsLocal.KillTweensForTest();
                        int hit1 = shiftHits;
                        // 摆一个**明显不等于终值**的起点 ⇒ 下面「补间真的落了终值」那一条才**可分**
                        //   （起点≈终值的话，补间一步没跑也会绿 —— 那是弱断言）。
                        rig463SsLocal.VcamLensShift = new Vector2(0.9f, 0.9f);
                        rig463SsLocal.Initialize(false);
                        Check(shiftHits == hit1 && rig463SsLocal.TweenCreatedForTest,
                              $"★ A463：（负例）`instant: false` ⇒ **不抬 Action**（抬了 {shiftHits - hit1} 次，期望 0）、"
                            + $" 但真的建了补间（`TweenCreated` = {rig463SsLocal.TweenCreatedForTest}"
                            + $" · `HasLive` 参考量 = {rig463SsLocal.HasLiveTweenForTest}）"
                            + " —— 🧨 两支写反 ⇒ 红");

                        // ㈥ 补间那条路的**终值**：批处理里 DOTween 不会自己推进 ⇒ 用 `CompleteTweensForTest()`
                        //    推到终点（手法 = `SetUpdate(Manual)` + `DOTween.ManualUpdate`，判据 = `Editor/DOTweenSmokeTest.cs`）。
                        //    判据：`vcam` 等价物（= `CombatCameraZoom.virtualCameraLensShift`）到 `(0, want.y)`，
                        //    **并且真相机也到**（那一跳 = `ApplyVirtualCameraLensShift`）。
                        //    🧨 改坏法：`VcamLensShift` 的 setter 里删掉 `ApplyVirtualCameraLensShift()` ⇒ 后半段红。
                        rig463SsLocal.CompleteTweensForTest();
                        Check(Vector2.Distance(rig463Cz.virtualCameraLensShift, new Vector2(0f, wantShift463.y)) < 1e-3f
                           && Vector2.Distance(rig463Cam.lensShift, new Vector2(0f, wantShift463.y)) < 1e-3f,
                              $"★ A463：补间跑到终点 ⇒ 虚拟镜头位移 = (0, {wantShift463.y:F4})"
                            + $"（实得 {rig463Cz.virtualCameraLensShift}）**并且推到了真相机**"
                            + $"（`Camera.lensShift` 实得 {rig463Cam.lensShift}）"
                            + " —— 起点摆的是 (0.9, 0.9)（⛔ 不是终值）⇒ 补间一步没跑就红"
                            + "；🧨 setter 里删掉 `ApplyVirtualCameraLensShift()` ⇒ 后半段红");

                        // ㈦ `vcam` 那一格的等价物：`BattleCameraSreenSize.VcamLensShift`
                        //    ←→ `CombatCameraZoom.virtualCameraLensShift`（**同一个存储**，见那件文件头那一整段）。
                        //    🧨 改坏法：setter 少写一半（只写自己的字段 / 不推给真相机）⇒ 红。
                        rig463SsLocal.VcamLensShift = new Vector2(0.123f, -0.456f);
                        Check(Vector2.Distance(rig463Cz.virtualCameraLensShift, new Vector2(0.123f, -0.456f)) < 1e-5f
                           && Vector2.Distance(rig463Cam.lensShift, new Vector2(0.123f, -0.456f)) < 1e-5f
                           && Vector2.Distance(rig463SsLocal.VcamLensShift, new Vector2(0.123f, -0.456f)) < 1e-5f,
                              "★ A463：`vcam` 那一格的读写都落在 `CombatCameraZoom.virtualCameraLensShift`"
                            + "（**同一个存储** —— 原版是 `vcam.m_Lens.LensShift`，对象偏移 `+0xD0`），"
                            + "且写完真的推到真相机（`Camera.lensShift`）—— 🧨 setter 少写一半 ⇒ 红");

                        // ㈧ `DoLensShift(float, bool)` —— **本 build 零调用点**（原版它的体被内联进了
                        //    `Initialize` 后半段），但公开接口在。判据：它抬出来的载荷同样是 `(0, y)`。
                        //    🧨 改坏法：写成 `new Vector2(y, 0)` ⇒ `.y` 不是 0.25 ⇒ 红。
                        capShift = new Vector2(999f, 999f);
                        int hit2 = shiftHits;
                        rig463SsLocal.DoLensShift(0.25f, true);
                        Check(shiftHits == hit2 + 1
                           && Mathf.Abs(capShift.x) < 1e-6f && Mathf.Abs(capShift.y - 0.25f) < 1e-6f,
                              $"★ A463：`DoLensShift(y, true)` 抬的也是 `(0, y)`（实得 {capShift}，期望 (0, 0.25)）"
                            + " —— 它与 `Initialize` 后半段是原版里**两份同样的体**（本 build 里它零调用点，照原版留着）"
                            + "；🧨 写成 `new Vector2(y, 0)` ⇒ 红");

                        // ㈨ **等价物 A**（⛔ 不是原版行为）：原版那条 `ScreenResolutionChangeSignal` 由别处发，
                        //    发动者在全反编译里**查不到**（`grep -rl ScreenResolutionChangeSignal decomp_full` = 0）
                        //    ⇒ 我们让自己的 `Update` 比屏宽高。判据：基线没变 ⇒ 不抬；改一个像素 ⇒ 抬一次。
                        //    🧨 改坏法：把 `Tick` 里那句 `NotifyScreenResolutionChanged()` 删掉 ⇒ 第二条红。
                        int c9a = rig463SsLocal.InitializeCount;
                        rig463SsLocal.SetScreenSizeBaselineForTest(Screen.width, Screen.height);
                        rig463SsLocal.Tick();
                        int c9b = rig463SsLocal.InitializeCount;
                        rig463SsLocal.SetScreenSizeBaselineForTest(Screen.width + 1, Screen.height);
                        rig463SsLocal.Tick();
                        Check(c9b == c9a && rig463SsLocal.InitializeCount == c9a + 1,
                              $"★ A463：**等价物 A** —— 屏宽高没变 ⇒ 不抬（{c9a} → {c9b}）；变一格 ⇒ 抬一次"
                            + $"（→ {rig463SsLocal.InitializeCount}）"
                            + " —— ⚠️ 这一条验的是**我们的等价物**（原版那条信号源查不到），⛔ 不是原版行为");
                        rig463SsLocal.SetScreenSizeBaselineForTest(Screen.width, Screen.height);
                    }
                    finally
                    {
                        rig463SsLocal.OnCameraShiftChanged -= onShift;
                        rig463SsLocal.OnCameraSensorSizeChanged -= onSs;
                    }
                }
            }
            finally
            {
                if (rig463Ss != null)
                {
                    // ⛔ 必须摘：那是个**静态**事件，留一个指向已销毁组件的委托 ⇒ 下一次抬信号就 NRE。
                    rig463Ss.UnregisterResolutionSignal();
                    rig463Ss.KillTweensForTest();
                }
                UnityEngine.Object.DestroyImmediate(rig463);
            }
            // 回读自证：静态信号上的订阅者数**回到进本段之前那一档**（这一条红了 = 本段会把后面 / 下一次调用带偏）。
            // 🔴 2026-10-14（#8）**订正**：原来断的是 `== 0`，前提是「场上只有我这一台注册过」——
            //   **那个前提不成立**：主战场那台（`sceneRoot` 上那件 `CombatAutoZoom` 的
            //   `BattleCameraSreenSize`）是**合法订阅者**，所以收工恒 ≥ 1。
            //   本 run 实得 **2** = 它 + `A422` rig 那台**已销毁**的泄漏（泄漏那半已在本轮于 A422 的
            //   `finally` 里堵住 ⇒ 基线从 3 回到 2，仍 ≠ 0）⇒ 只能改成**增量**自证。
            //   ⛔ **别写死 `<= 2`**（那种死数字换个环境就会误报/漏报）。
            Check(BattleCameraSreenSize.ResolutionSignalSubscriberCountForTest == sigBefore463,
                  $"★ A463：（自证）那条静态分辨率信号上的订阅者数**回到进本段之前那一档**"
                + $"（进本段前 {sigBefore463} → 收工 {BattleCameraSreenSize.ResolutionSignalSubscriberCountForTest}）"
                + " —— 场上还有主战场那台**合法订阅者**，所以基线不是 0；本段自己那台摘干净了 ⇒ 两个读数就该相等");

            Debug.Log(P + "--- A463 段结束（下面还有别的段）---");

        }

        // 收尾：把玩家的 `Auto Zoom` / `Small Screen UI` 两格真设置**放回原样**
        // （本函数开头把它们压成了「关」，全程 `PersistOverride` ⇒ 只改内存、一个字节都没写盘）。
        AutoZoom.RestoreForTest(azWasOn, azWasChosen);
        AutoZoom.PersistOverride = azWasPersist;
        // ⚠️ `SmallScreenUI` 那边**没有** `RestoreForTest`（那个类在 `Shell/TransformScalerBySmallScreenUI.cs`，
        //    **不在本件白名单**）⇒ 用它的公开口拼：先 `ResetForTest()` 把两格清成出厂，再按原值 `Set`。
        //    **唯一的残留**（如实记）：原本「`Enabled = true` 但 `ChosenManually = false`」那一档，收尾会变成
        //    `ChosenManually = true`。它**不落盘**，而读它的只有 `SettingsScene.Run`—— 那是**另一个进程**的自检
        //    （而且它自己开头就 `ResetForTest`），所以不产生任何可观测后果。
        if (!ssWasChosen) SmallScreenUI.ResetForTest();
        if (ssWasOn) SmallScreenUI.Set(true);
        SmallScreenUI.PersistOverride = ssWasPersist;

        Debug.Log(P + $"=== 结束：{pass} 通过 / {fail} 失败 ===");

        // 最后验一下**存下来的那个场景**（自检上面的场景是当场建的，不是存的那份）
        var tail = new int[2] { pass, fail };
        CheckSavedScene(tail);
        pass = tail[0]; fail = tail[1];
        Debug.Log(P + $"=== 合计：{pass} 通过 / {fail} 失败 ===");

        if (Application.isBatchMode) EditorApplication.Exit(fail == 0 ? 0 : 1);
    }


    /// <summary>
    /// 打开存好的 `Battle.unity` 检查关键件还在不在。
    /// ⚠️ 这一步不能省：`BattleBackdrop` 的 `_mr/_tex` 是**私有字段、不进序列化**，
    ///    存场景之后再打开时它们是 null —— `Build()` 里那段「从子节点重新找回来」
    ///    就是为这个写的，必须真的验一次（不然只有进 Play 才会发现背景没了）。
    /// </summary>
    /// <summary>从视图表里取一个（拿不到给 null，别抛）—— 联机客机视图那一段用。</summary>
    static CardView Lookup(IReadOnlyDictionary<int, CardView> d, int slot)
    {
        CardView v = null;
        if (d != null) d.TryGetValue(slot, out v);
        return v;
    }

    /// <summary>某一方**手牌里第一张单位卡**（联机客机视图那一段要摆一个单位上去用的）。</summary>
    static RuleEngine.CardDef FirstUnit(BattleContext c, int owner)
    {
        if (c == null || owner < 0 || owner > 1) return null;
        foreach (var inst in c.Players[owner].Hand)
            if (inst != null && inst.Card != null && inst.Card.Type == "unit") return inst.Card;
        return null;
    }

    /// <summary>战场上「`emission.enabled` 关着」的粒子系统个数 —— 自检用来判
    /// **「放进攻卡有没有把战场自己那批关掉」**（原版 `SO.defaultScenarioObjectsState = 0` 那条，
    /// 判据 → `资料/加时与冲突模式_原版规格.md` 的 2026-09-30 那一节 + `Battle/ScenarioBlendables.cs`）。</summary>
    static int CountEmittersOff()
    {
        int n = 0;
        foreach (var ps in UnityEngine.Object.FindObjectsOfType<ParticleSystem>(true))
            if (ps != null && !ps.emission.enabled) n++;
        return n;
    }

    static void CheckSavedScene(int[] tally)    {
        if (!File.Exists(ScenePath)) { Debug.LogWarning(P + "还没存过场景，跳过存档检查"); return; }

        EditorSceneManager.OpenScene(ScenePath);
        void Check(bool ok, string msg)
        {
            if (ok) { tally[0]++; Debug.Log(P + $"   ✓ {msg}"); }
            else { tally[1]++; Debug.LogError(P + $"   ✗ {msg}"); }
        }
        // 🆕 2026-09-22：给「粒子那批新字段」用的三个小判据。
        //    ⚠️ 「这条曲线有没有数据」**只能看键**，不能看 `!= null`（`JsonUtility` 会把 JSON 的
        //    `null` 物化成默认空对象 ⇒ `!= null` 恒真，2026-09-21 因此数出过假绿）。
        bool NonZeroCurve(ArenaBuilder.CurveData c)
        {
            if (c == null) return false;
            if (c.isConst) return Mathf.Abs(c.c) > 1e-4f || Mathf.Abs(c.cMin) > 1e-4f;
            return ArenaBuilder.HasKeys(c);
        }
        bool NonZeroVec(float[] v)
            => v != null && v.Length >= 3
               && (Mathf.Abs(v[0]) > 1e-4f || Mathf.Abs(v[1]) > 1e-4f || Mathf.Abs(v[2]) > 1e-4f);
        bool ApproxVec(float[] v, Vector3 expect)
            => v == null || v.Length < 3
               || (Mathf.Abs(v[0] - expect.x) < 1e-4f && Mathf.Abs(v[1] - expect.y) < 1e-4f
                   && Mathf.Abs(v[2] - expect.z) < 1e-4f);

        // 🆕 2026-09-19：战场现在是**真 3D** —— `Arena3D`（29 网格 + 34 粒子）由一台**透视**的
        //    `BoardCamera` 画，HUD 相机只清深度、把 3D 那层叠在下面。烘好的背景图**只在 3D 资产缺失时**兜底，
        //    所以这里两条路都要验（有场地 ⇒ 必须有那台透视相机；没场地 ⇒ 才看背景图）。
        // 🔴 **2026-09-30（§27 架构）**：战场内容**不再烘进场景**了 —— 它在
        //    `Resources/ArenaPrefabs/<场>.prefab` 里（`ArenaBuilder.BuildArenaPrefabs` 建），
        //    进局时由 `ArenaRuntimeLoader` 实例化。⇒ 这一段**分两半查**：
        //      · **场景里**：`Arena3D` 存在、**可渲染件 0 个**（刻意留空）、且挂着 `ArenaRuntimeLoader`；
        //      · **内容与逐场值**：查 **prefab 资产**（编辑期 `Resources.Load` 拿到的就是资产本身，
        //        只读统计没问题 —— 但它**不是**「场上有东西」的证据）。
        //    施工图 → `资料/§27架构_施工图.md`
        var arena = GameObject.Find("Arena3D");
        int arenaRend = arena != null ? arena.GetComponentsInChildren<Renderer>(true).Length : 0;
        var loaderInScene = arena != null ? arena.GetComponent<CardPresentation.ArenaRuntimeLoader>() : null;
        var arenaPrefab = Resources.Load<GameObject>("ArenaPrefabs/" + BoardArena);
        var arenaData = arenaPrefab != null ? arenaPrefab.GetComponentInChildren<CardPresentation.ArenaPrefabData>() : null;
        Check(arena != null && arenaRend == 0 && loaderInScene != null,
              arena == null ? "存档里没有 Arena3D（§27 要求它必须在，只是**空着**）"
                            : $"★ 存档里的 `Arena3D` 是**空的**（可渲染件 {arenaRend}）＋ 挂着 `ArenaRuntimeLoader`"
                            + $"（loader {(loaderInScene != null ? "在" : "**不在**")}）—— §27：战场改为运行时实例化");
        Check(arenaPrefab != null && arenaData != null,
              arenaPrefab == null
                ? $"🔴 `Resources/ArenaPrefabs/{BoardArena}.prefab` **取不到** ⇒ 这一局没有战场"
                  + "（跑 `-executeMethod ArenaBuilder.BuildArenaPrefabs`）"
                : $"★ 战场 prefab 在位：`{BoardArena}`（ArenaPrefabData {(arenaData != null ? "有" : "**没有**")}）");
        var arenaContent = arenaPrefab != null ? arenaPrefab : arena;   // 下面那些「战场里有什么」的断言都用它

        Camera bcam = null, hcam = null;
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (c.depth < 0) bcam = c; else if (hcam == null) hcam = c;
        }
        Check((bcam != null) == (loaderInScene != null),
              "★ 战场装载器与那台透视相机**成对**存在（§27：场景里留的是 loader，不是内容）");
        if (bcam != null)
        {
            Check(!bcam.orthographic, "战场相机是**透视**（不是正交）");
            // 🔴 **2026-09-20 改判据**：原来这两条只比「从原版抄来的两个数」（FOV 46.397 / lensShift −0.205），
            //    而 `lensShift` **根本没生效**（没开物理相机）⇒ **数字全对、取景全错** ——
            //    典型的「断言钉住了错的东西」。现在改成：① 物理相机那几项 ② **投影出来的行位**。
            // 🔴 **2026-09-27 改判据：`sensorSize.x` 是【逐场】的，不许再比写死的 41.5。**
            //    原版 13 台 `BoardCamera` 里只有两个取值：**EC / genestealers / spacewolves = 37.2**，其余十台 = 41.5；
            //    `gateFit = Horizontal` 下 x 直接决定 hFOV（vFOV 按画幅联动）⇒ 写死会把那三场**等比放大 10.36%**。
            //    **这正是文档里记了很久的「这三场的原版实拍图不能用、与原版授权取景差 ~10%」的真因 ——
            //    那条结论已作废：实拍图是好的，是我们的相机写错了。**
            var camMf = ArenaBuilder.LoadManifest(BoardArena);
            float wantSx = (camMf != null && camMf.camera != null && camMf.camera.sensorSizeX > 0f)
                         ? camMf.camera.sensorSizeX : 41.5f;
            Check(bcam.usePhysicalProperties
                  && Mathf.Abs(bcam.focalLength - 28f) < 0.01f
                  && Mathf.Abs(bcam.sensorSize.x - wantSx) < 0.01f
                  && Mathf.Abs(bcam.sensorSize.y - 24f) < 0.01f
                  && bcam.gateFit == Camera.GateFitMode.Horizontal,
                  $"★ 战场相机是**物理相机**（focal 28 / sensor {wantSx:0.##}×24 / gateFit Horizontal）"
                  + " —— 不开这个，`lensShift` 会被 Unity **静默忽略**（踩过）");
            // 🔴 **那三场单独钉一条**（钉的是「旁挂没接错」）—— 值就是原版 `Camera_*.json` 的 `m_SensorSize.x`。
            string[] narrow = { "battlearenaemperorschildren", "battlearenagenestealers", "battlearenaspacewolves" };
            bool isNarrow = System.Array.IndexOf(narrow, BoardArena) >= 0;
            Check(Mathf.Abs(wantSx - (isNarrow ? 37.2f : 41.5f)) < 0.01f,
                  $"★ `{BoardArena}` 的 sensorSize.x = **{(isNarrow ? "37.2" : "41.5")}**"
                  + $"（原版那三场窄一档 / 其余十场 41.5）—— 现在 {wantSx:0.###}");
            Check(Mathf.Abs(bcam.lensShift.y - BoardLensShiftY()) < 0.001f,
                  $"战场相机 lensShift.y {bcam.lensShift.y:F4}（判据 = 原版算法 `BoardFramer.LensShiftY` "
                  + $"{BoardLensShiftY():F4}）");
            // ⚠️ 「取景对不对」那条判据**挪到 13d 了** —— 它必须在一个 **aspect 已知是 16:9** 的时刻跑：
            //    物理相机 + `gateFit = Horizontal` 下，**竖直取景是跟宽高比走的**，
            //    而这里（任何 `Shot` 之前）相机的 aspect 还是默认值，量出来会是另一个数。
            //    🔴 教训：**判据要在判据成立的条件下量** —— 这条断言第一版就是在这儿量的，
            //    同一个点被算成 62.1%，而 13d 那边（aspect 已是 1.7778）是 66.08%。
            Check(bcam.cullingMask == (1 << ArenaLayer), "战场相机只渲 3D 战场那一层");
            Check(bcam.depth < 0, $"战场相机先画（depth {bcam.depth}）");
        }

        // 🆕 2026-09-20：**灯光与环境光**（第 12 行「光照」那一件）。
        //    🔴 原来战斗场景里**一盏灯都没有** —— `ArenaBuilder.BuildContent` 只建网格/粒子，
        //    灯与环境光住在**只给独立场景用的** `BuildSceneTail` 里（`BattleScene.cs` 全篇
        //    没有 `AddComponent<Light>`、也没有 `RenderSettings`）⇒ 战场全靠环境光。
        //    判据全部来自**清单**（= 原版实读），不是这里写死的数。
        {
            var mfEnv = ArenaBuilder.LoadManifest(BoardArena);
            // §27：灯属于**战场内容** ⇒ 在 prefab 里找（原来在场景里）
            Light dl = null;
            if (arenaContent != null)
                foreach (var l in arenaContent.GetComponentsInChildren<Light>(true))
                    if (l.type == LightType.Directional) { dl = l; break; }

            Check(dl != null && mfEnv != null && mfEnv.light != null,
                  "★ 战场有那盏原版平行光（原版 13 场每场**恰好 1 盏** `m_Type==1`）");
            if (dl != null && mfEnv != null && mfEnv.light != null)
            {
                var L = mfEnv.light;
                Check(Mathf.Abs(dl.color.r - L.color[0]) < 0.004f
                      && Mathf.Abs(dl.color.g - L.color[1]) < 0.004f
                      && Mathf.Abs(dl.color.b - L.color[2]) < 0.004f,
                      $"★ 灯色 = 清单值 ({L.color[0]:F5},{L.color[1]:F5},{L.color[2]:F5})"
                      + " —— **不是白色**（原版 arena1 是暖白；写死 `Color.white` 这条就红）");
                Check(Mathf.Abs(dl.intensity - L.intensity) < 0.001f,
                      $"灯强度 = 清单值 {L.intensity:F3}");
                Check(dl.shadows == (LightShadows)Mathf.Clamp(L.shadowType, 0, 2),
                      $"阴影类型 = 清单值 `{(LightShadows)Mathf.Clamp(L.shadowType, 0, 2)}`");
                Check(Mathf.Abs(dl.shadowStrength - L.shadowStrength) < 0.001f,
                      $"★ 阴影强度 = 清单值 {L.shadowStrength:F3}（原版 13 场分 4 档：0.591/0.65/0.725/1.0，"
                      + "原来从没搬过、只写死 Soft）");
                Check(arenaContent != null && dl.transform.IsChildOf(arenaContent.transform),
                      "灯在**战场 prefab 根之下**（跟着战场一起走；§27 后它跟着 prefab 实例化）");
            }

            // §27：场景级值现在存在 **prefab 的 `ArenaPrefabData`** 里（运行时由 `ArenaSceneState.Apply` 灌进 RenderSettings）
            Check(arenaData != null && arenaData.state.ambientMode == (int)UnityEngine.Rendering.AmbientMode.Flat,
                  "★ 环境光模式 = **Flat**（原版 13 场 `m_AmbientMode` **全是 3**，不是 1）"
                  + " —— 写成 `Trilight` 这条就红（踩过：模式错了，抄对颜色也没用）");

            // 🔴 颜色判据 = `defaultEnv.ambientColor`（**运行时真值**），不是场景里的 `m_AmbientSkyColor`。
            //    原版 `ScenarioEnvironmentConditionsManager.Awake()` → `ApplyAmbientColor` →
            //    `RenderSettings.ambientLight = <SO>.ambientColor`。实况探针（2026-09-20）双场实测：
            //    arena1 得 (1,1,1,α0)（SO α=0、场景 α=1）· arena3 得 (0.80660,0.95225,1)
            //    而 arena3 的场景值其实是 (0.6840,0.9229,1) ⇒ **跟 SO，不跟场景**。
            if (mfEnv != null && mfEnv.defaultEnv != null && mfEnv.defaultEnv.ambientColor != null)
            {
                var E = mfEnv.defaultEnv.ambientColor;
                // §27：量的是 **prefab 里那份数据**（运行时由 `ArenaSceneState.Apply` 灌进 RenderSettings）
                var c = arenaData != null ? arenaData.state.ambientSky : RenderSettings.ambientLight;
                Check(Mathf.Abs(c.r - E[0]) < 0.004f && Mathf.Abs(c.g - E[1]) < 0.004f
                      && Mathf.Abs(c.b - E[2]) < 0.004f,
                      $"★ 环境光色 = 默认环境 SO 的 `ambientColor` ({E[0]:F5},{E[1]:F5},{E[2]:F5})"
                      + $" —— 来自 `{mfEnv.defaultEnv.so}`（**运行时真值**；场景里的 `m_AmbientSkyColor` 是"
                      + $" ({mfEnv.ambient.sky[0]:F4},{mfEnv.ambient.sky[1]:F4},{mfEnv.ambient.sky[2]:F4})，"
                      + "原版运行时会把它覆盖掉）");

                // ⚠️ **2026-09-30（§27）：「环境光真的到了渲染器」（`ambientProbe`）这条挪去了运行时阶段**
                //    —— 存档场景里已经**不烘**这些值了（在 prefab 里、运行时才灌）。
                //    新位置：`Run` 里 `driver.Begin` 之后那段（与「战场是真 3D」同处）。
            }
            else Check(false, "★ 清单里有 `defaultEnv`（原版运行时环境光的真源）—— 缺了就只能退回场景值");

            Check(!RenderSettings.fog, "雾关着（原版 13 场的默认环境 `fogDensity` 都是 0）");

            // 🔴 反例：战场上**不能有「材质没贴图」的粒子** —— 那会渲成**不透明白方块**。
            //    踩过：blacklegion 的 4 个 `Heat Distortion`（扭曲类，原版材质本来就没有贴图）被我们
            //    建成了 `URP/Particles/Unlit` + 空贴图 ⇒ 画面中间横着 4 条白板；和原版真渲图一比就露。
            //    现在的做法是**不建 + 报警**（`ArenaBuilder.BuildContent`），这条断言盯着它别再回来。
            int whitePs = 0;
            if (arenaContent != null)
            {
                foreach (var r in arenaContent.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    // 🆕 2026-09-28：**Mesh 模式的不算** —— 它靠网格 + shader 出效果、不吃贴图，
                    //    不会被渲成白方块（就是上面那条豁免口放行的 `Close Monolith Rays` 一族）。
                    //    ⚠️ 判据与构建侧同源：`ArenaBuilder.NoTexMeshModeOk`（构建侧那道闸门）。
                    if (r.renderMode == ParticleSystemRenderMode.Mesh) continue;
                    var m = r.sharedMaterial;
                    if (m == null) { whitePs++; continue; }
                    Texture t = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                    if (t == null && m.HasProperty("_MainTex")) t = m.GetTexture("_MainTex");
                    if (t == null && m.mainTexture != null) t = m.mainTexture;
                    if (t == null) whitePs++;
                }
            }
            Check(whitePs == 0, $"★ 战场里没有「材质没贴图」的粒子（有 {whitePs} 个 ⇒ 会渲成不透明白方块）");

            // 🆕 2026-09-21：粒子**三个「原来根本没建」的东西**各来一条断言。
            //    判据 = 清单里有多少个、场景里就必须有多少个（清单是原版真值）。
            //    为什么非要断言：这三条**全是静默的** —— 少建一个模块不报错、只是画面上烟更大更实，
            //    肉眼在 13 张缩略图上分不出来（发现它靠的是「逐对象参数对账 + 并排渲图」）。
            if (arenaContent != null && mfEnv != null && mfEnv.particles != null)
            {
                var built = new System.Collections.Generic.List<ParticleSystem>();
                foreach (var p in arenaContent.GetComponentsInChildren<ParticleSystem>(true))
                    built.Add(p);
                int wantSol = 0, gotSol = 0, wantCol = 0, gotCol = 0, wantRate = 0, gotRate = 0;
                int wantVel = 0, gotVel = 0, wantClamp = 0, gotClamp = 0, wantNoise = 0, gotNoise = 0;
                int wantRot = 0, gotRot = 0, wantSub = 0, gotSub = 0;
                // 🆕 2026-09-25：**清单侧的计数必须和构建侧用同一套「该不该建」的判据** ——
                //    原版按画质档关着的（`ObjectTogglerByQuality`，见 `项目任务.md` §三 第 3 条 第 4 项）
                //    现在是**故意不建**的；漏掉这一条 ⇒ `aeldari` 那条 colorOverLifetime 断言报
                //    「清单要 21、实得 20」的**假红**（2026-09-25 实测）。
                var qOff = ArenaBuilder.LoadInactiveByQuality(mfEnv.scene);
                foreach (var pe in mfEnv.particles)
                {
                    if (!pe.active || pe.renderMode == 5) continue;
                    // 🔴 2026-09-28：**这一条必须与构建侧共用同一份判据** —— 无贴图的粒子一律不建，
                    //    **但 `renderMode = 4 (Mesh)` 且网格抽得出来的那批要建**（`arena3` 的 `Close Monolith Rays`）。
                    //    漏掉这个豁免 ⇒ 下面三条计数各报一次「清单要 0、实得 2/2/4」的**假红**（实测过）。
                    if (string.IsNullOrEmpty(pe.texFile) && !ArenaBuilder.NoTexMeshModeOk(pe, mfEnv.scene)) continue;
                    if (qOff.Contains(pe.go)) continue;
                    // 🔴 **判据必须看「有没有曲线键」，不能看 `!= null`** ——
                    //    `JsonUtility` 会把 JSON 的 `null` **物化成空对象**，`!= null` 恒真
                    //    （2026-09-21 踩过：这条断言按 `!= null` 数出「34 要 34 有」的**假绿**，
                    //    实际清单里只有 22 条真有 size 曲线、1 条真有 emissionRate 曲线）。
                    if (ArenaBuilder.HasSizeCurve(pe.sizeOverLifetime)) wantSol++;
                    if (pe.colorOverLifetime != null
                        && ((pe.colorOverLifetime.colors != null && pe.colorOverLifetime.colors.Length >= 2)
                            || (pe.colorOverLifetime.alphas != null && pe.colorOverLifetime.alphas.Length >= 2))) wantCol++;
                    if (ArenaBuilder.HasKeys(pe.emissionRateCurve)) wantRate++;
                    // 🆕 2026-09-21 下半场：VFX 那 5 个模块（原来一个都没建）
                    // 🔴 **2026-09-22 修：这四条原来按 `!= null` 计数 —— 那是恒真的**
                    //    （`JsonUtility` 把清单里的 `null` 物化成空对象），所以原来数出来的
                    //    「清单要 34 个」是**假绿**（原版实测 leviathan 是 velocity 26 / clamp 10 /
                    //    noise 6 / rotation 7）。判据改成 `ArenaBuilder.Has*()` —— 与构建侧**共用一份**。
                    if (pe.hasVelocity) wantVel++;
                    if (pe.hasClampVelocity) wantClamp++;
                    if (pe.hasNoise) wantNoise++;
                    if (pe.hasRotation) wantRot++;
                    if (pe.subEmitters != null && pe.subEmitters.Length > 0) wantSub += pe.subEmitters.Length;
                }
                foreach (var p in built)
                {
                    if (p.sizeOverLifetime.enabled) gotSol++;
                    if (p.colorOverLifetime.enabled) gotCol++;
                    if (p.emission.rateOverTime.mode != ParticleSystemCurveMode.Constant) gotRate++;
                    if (p.velocityOverLifetime.enabled) gotVel++;
                    if (p.limitVelocityOverLifetime.enabled) gotClamp++;
                    if (p.noise.enabled) gotNoise++;
                    if (p.rotationOverLifetime.enabled) gotRot++;
                    if (p.subEmitters.enabled) gotSub += p.subEmitters.subEmittersCount;
                }
                Check(gotSol == wantSol, $"★ 粒子的 sizeOverLifetime 建全了（清单要 {wantSol} 个，实得 {gotSol}）"
                      + " —— 缺了 ⇒ 全程满尺寸（原版 Gas 前段只有 23%）");
                Check(gotCol == wantCol, $"★ 粒子的 colorOverLifetime（出生淡入/死亡淡出）建全了"
                      + $"（清单要 {wantCol} 个，实得 {gotCol}）—— 缺了 ⇒ 整条命实心播、比原版更不透明");
                Check(gotRate == wantRate, $"★ 粒子的 emissionRate 曲线建全了（清单要 {wantRate} 个，实得 {gotRate}）"
                      + " —— 拍成峰值常数会让存活粒子多 2~3 倍");
                // 🆕 2026-09-21 下半场：VFX 那 5 个模块（原来一个都没建）
                Check(gotVel == wantVel, $"★ 粒子的 velocityOverLifetime 建全了（清单要 {wantVel} 个，实得 {gotVel}）"
                      + " —— **烟不飘就是一坨浓白**（原版 leviathan 26/71 个对象有它）");
                Check(gotClamp == wantClamp, $"★ 粒子的 limitVelocityOverLifetime 建全了"
                      + $"（清单要 {wantClamp} 个，实得 {gotClamp}）");
                Check(gotNoise == wantNoise, $"★ 粒子的 noise 建全了（清单要 {wantNoise} 个，实得 {gotNoise}）"
                      + " —— 缺了烟雾不会扭，是死板的圆团");
                Check(gotRot == wantRot, $"★ 粒子的 rotationOverLifetime 建全了（清单要 {wantRot} 个，实得 {gotRot}）");
                Check(gotSub == wantSub, $"★ 粒子的**子发射器**连全了（清单要 {wantSub} 个，实得 {gotSub}）"
                      + " —— 「火里蹦火星」就是它");

                // 🆕 2026-09-22：**「原版有、生成器从来没抽」的一批字段**（判据 = `工具/arena_particle_audit.py`）。
                //    ⚠️ 第一版写成「按条数比」，**两条当场红**（`startRotation` 要 14 实得 34、
                //    `startDelay` 要 1 实得 19）—— 条数判据在这种「字段可能有多种编码」的场合不成立。
                //    ⇒ 改成**按下标逐颗配对**：`ArenaParticleIndex` 里存的就是清单 `particles[]` 的下标，
                //    配上了就**直接比值**（判据与构建侧同源，不再各算一套）。
                int paired = 0, badSim = 0, badRot = 0, badDelay = 0, badShape = 0, badRand = 0;
                // ⚠️ `MinMaxCurve.constant` 在 **TwoConstants** 模式下是**无效字段**（读回来是 0）——
                //    第一版按它判，红了 12 条。判据改成按 `(min, max)` 一对比：
                Vector2 GotRange(ParticleSystem.MinMaxCurve c)
                    => c.mode == ParticleSystemCurveMode.Constant
                       ? new Vector2(c.constant, c.constant)
                       : new Vector2(c.constantMin, c.constantMax);
                var idxMap = new System.Collections.Generic.Dictionary<int, ParticleSystem>();
                foreach (var c in arenaContent.GetComponentsInChildren<WarpforgeVFX.ArenaParticleIndex>(true))
                {
                    if (c == null || c.index < 0 || c.index >= mfEnv.particles.Length) continue;
                    var pp = c.GetComponent<ParticleSystem>();
                    if (pp != null) idxMap[c.index] = pp;
                }
                foreach (var kv in idxMap)
                {
                    var pe2 = mfEnv.particles[kv.Key];
                    var m2 = kv.Value.main;
                    paired++;
                    float wantSp = pe2.simulationSpeed > 0f ? pe2.simulationSpeed : 1f;
                    if (Mathf.Abs(m2.simulationSpeed - wantSp) > 1e-3f) badSim++;
                    if (!m2.startRotation3D)
                    {
                        var wr = (pe2.startRotation != null && pe2.startRotation.isConst)
                               ? new Vector2(pe2.startRotation.cMin, pe2.startRotation.c)
                               : Vector2.zero;
                        var gr = GotRange(m2.startRotation);
                        if ((gr - wr).magnitude > 1e-3f) badRot++;
                    }
                    var wd = (pe2.startDelay != null && pe2.startDelay.isConst)
                           ? new Vector2(pe2.startDelay.cMin, pe2.startDelay.c) : Vector2.zero;
                    if ((GotRange(m2.startDelay) - wd).magnitude > 1e-3f) badDelay++;
                    var v = kv.Value.shape;
                    if (NonZeroVec(pe2.shapePos) && (v.position - new Vector3(pe2.shapePos[0], pe2.shapePos[1], pe2.shapePos[2])).magnitude > 1e-3f) badShape++;
                    if (NonZeroVec(pe2.shapeRot) && (v.rotation - new Vector3(pe2.shapeRot[0], pe2.shapeRot[1], pe2.shapeRot[2])).magnitude > 1e-3f) badShape++;
                    if (!ApproxVec(pe2.shapeScale, Vector3.one) && (v.scale - new Vector3(pe2.shapeScale[0], pe2.shapeScale[1], pe2.shapeScale[2])).magnitude > 1e-3f) badShape++;
                    if (Mathf.Abs(m2.randomizeRotationDirection - pe2.randomizeRotationDirection) > 1e-3f) badRand++;
                }
                Check(paired > 0, $"★ 粒子的**下标**配得上（配了 {paired} 颗 / 清单 {mfEnv.particles.Length} 条）");
                Check(badSim == 0, $"★ 每颗粒子的 simulationSpeed 都照清单设了（{badSim}/{paired} 颗不符）"
                      + " —— 烟囱那颗原版是 0.1（慢 10 倍），不设就是全按 1.0 播");
                Check(badRot == 0, $"★ 每颗粒子的 startRotation（**随机朝向**）都照清单设了（{badRot}/{paired} 颗不符）"
                      + " —— 不设 ⇒ 所有烟贴片同朝向、叠成一坨");
                Check(badDelay == 0, $"★ 每颗粒子的 startDelay 都照清单设了（{badDelay}/{paired} 颗不符）");
                Check(badShape == 0, $"★ 每颗粒子的 ShapeModule 位置/欧拉角/缩放都照清单设了（{badShape}/{paired} 颗不符）"
                      + " —— 不设 ⇒ Embers 那族发射方向差 90°");
                Check(badRand == 0, $"★ 每颗粒子的 randomizeRotationDirection 都照清单设了（{badRand}/{paired} 颗不符）");

                // 反例：原版 `m_IsActive=False` 的对象**不许建**（arena3 的两个淡绿 Light 就是它）
                // 🔴 **2026-09-25 修：按【清单下标】判，不按名字** ——
                //    旧写法 `p.gameObject.name == pe.go` 在**重名的场**上会报假幽灵：
                //    `battlearenatauviorla` 的 163 颗里 **67 颗原版关着**，其中 **11 个名字与开着的重名**
                //    （`Muzzle Flash view` ×3、`Muzzle Flash glow` ×2 …）⇒ 实测报出 **28 个假幽灵**，
                //    把这一场的 arena 专项自检打红（而产品是对的：建场日志写「另跳过原版关着 65 个」）。
                //    **判据只能是身份**：我们建出来的每颗都带 `ArenaParticleIndex`（= 清单下标），
                //    拿它回查清单的 `active` 才作数。
                int ghost = 0;
                foreach (var kv in idxMap)
                    if (!mfEnv.particles[kv.Key].active) ghost++;
                Check(ghost == 0, $"★ 原版**关着**的粒子没被建出来（实得 {ghost} 个）"
                      + " —— arena3 的 `TorchEffectNecron/Fire/Light` ×2 就是这么冒出来的");
            }

            // 🔴 反例：**不能有「子网格没材质」的网格** —— Unity 会给它套**默认灰材质**。
            //    踩过：圣女战场的 `Floor` 有 2 个子网格（OBJ 的 `g Floor_0`/`g Floor_1`），
            //    我们只设了 `sharedMaterial`（只作用于子网格 0）⇒ 第 2 个子网格整片灰，
            //    地板变成一块均匀的 (106,101,96)。13 场里 6 场共 29 个网格有多子网格。
            int nullSub = 0;
            if (arena != null)
            {
                foreach (var r in arena.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var ms = r.sharedMaterials;
                    if (ms == null || ms.Length == 0) { nullSub++; continue; }
                    for (int i = 0; i < ms.Length; i++) if (ms[i] == null) nullSub++;
                }
            }
            Check(nullSub == 0, $"★ 没有「子网格没材质」的网格（有 {nullSub} 个 ⇒ Unity 会给它套**默认灰材质**）");
        }

        // 🆕 2026-09-20：**后处理**（原版战场在 `BoardCamera` 上挂的全局 Volume）。
        // 值来自清单（原版实读）：Bloom threshold 1.15 / intensity 5.0 / scatter 1.0 / skipIterations 6
        // · Vignette 黑 / 中心 (0.5,0.5) / 强度 0.297。
        // 🔴 **A243 就地订正（2026-10-11）**：这里原来写「`ColorLookup` 实测是 **identity**（不接）」——
        //    那句是**量错了纹理**得出的误判（量的是 `LUT Normal`，而 7 场各有**自己那张** `LUT <场>`，
        //    与恒等偏差中位 7/255 ⇒ 是**一层温和调色**，不是恒等）。更正 → `ArenaBuilder.ApplyPostFx` 的
        //    `ColorLookup` 分支（2026-09-22 已按 7/6 两档接上）+ `ColorLookupMismatch` 的判据注释。
        {
            var mfP = ArenaBuilder.LoadManifest(BoardArena);
            // §27：后处理 Volume 属于**战场内容** ⇒ 在 prefab 里找
            Volume gv = null;
            if (arenaContent != null)
                foreach (var v in arenaContent.GetComponentsInChildren<Volume>(true))
                    if (v.isGlobal) { gv = v; break; }

            Check(gv != null && gv.sharedProfile != null, "★ 战场有那个**全局 Volume**（原版挂在 BoardCamera 上）");
            if (gv != null && gv.sharedProfile != null && mfP != null && mfP.postFx != null)
            {
                Check(Mathf.Abs(gv.weight - mfP.postFx.weight) < 0.001f
                      && Mathf.Abs(gv.priority - mfP.postFx.priority) < 0.001f,
                      $"Volume weight/priority = 清单值 {mfP.postFx.weight:F2}/{mfP.postFx.priority:F2}");

                foreach (var c in mfP.postFx.components)
                {
                    if (c.type == "Bloom")
                    {
                        Bloom b;
                        bool ok = gv.sharedProfile.TryGet(out b) && b != null && b.active;
                        Check(ok && Mathf.Abs(b.threshold.value - c.threshold) < 0.001f
                              && Mathf.Abs(b.intensity.value - c.intensity) < 0.01f
                              && Mathf.Abs(b.scatter.value - c.scatter) < 0.001f
                              && (c.maxIterations <= 0f
                                  || Mathf.RoundToInt(b.maxIterations.value) == Mathf.RoundToInt(c.maxIterations)),
                              $"★ Bloom = 清单值 threshold {c.threshold:F2} / intensity {c.intensity:F1}"
                              + $" / scatter {c.scatter:F1} / maxIterations {Mathf.RoundToInt(b.maxIterations.value)}"
                              + "（⚠️ 抄的是 `maxIterations` —— URP 只读它，`skipIterations` 是废弃死值）");
                    }
                    // ⚠️ **A195 核过：这是「类型分派」、不是能力/夹具门**（没改）—— 清单里今天只有三种
                    //    （13/13 场：`Bloom` / `Vignette` / **`ColorLookup`**），三种**各有一条 ★**。
                    else if (c.type == "Vignette")
                    {
                        Vignette v;
                        bool ok = gv.sharedProfile.TryGet(out v) && v != null && v.active;
                        Check(ok && Mathf.Abs(v.intensity.value - c.intensity) < 0.001f
                              && Mathf.Abs(v.center.value.x - c.center[0]) < 0.001f,
                              $"★ Vignette = 清单值 强度 {c.intensity:F3} / 中心 ({c.center[0]:F1},{c.center[1]:F1})"
                              + "（原版就是拿它压四角，我们原来一点都没接）");
                    }
                    // 🆕 **A243：这一层原来「没有分支、也没有 `else`」** ⇒ 做了 / 没做 / 变没变，
                    //    自检**一声不吭**（13/13 场清单里都有它）。判据（两态，都来自原版）见
                    //    `ColorLookupMismatch`：7 场各有自己的 `LUT <场>`（该接）· 其余 6 场指的是
                    //    共享的恒等 `LUT Normal`（不接等效）。
                    //    ⚠️ 默认 `BoardArena` = `battlearena1` ⇒ 走到这里的是**「不接」那一档**，
                    //      「7 场该接的接上了没有」由本节后面那条**逐场对账**兜住。
                    else if (c.type == "ColorLookup")
                    {
                        bool own = System.Array.IndexOf(ArenasWithOwnLut, BoardArena) >= 0;
                        string why = ColorLookupMismatch(gv.sharedProfile, BoardArena, mfP);
                        Check(why == null,
                              $"★ `ColorLookup` = 原版那一档（`{BoardArena}`："
                            + (own ? "原版**有**自己的 `LUT <场>` ⇒ 必须接上" : "原版指的是恒等 `LUT Normal` ⇒ 不接等效")
                            + "）" + (why == null ? "" : " —— 🔴 " + why));
                    }
                    else
                    {
                        // 🔴 清单里哪天多出**第四种类型**，⛔ 别让它静默不验（A195 记过这一条）——
                        //    这里出声就够：它不是「能力/夹具门」，只是我们还没写的分派支。
                        Debug.LogWarning(P + $"   （清单里有个后处理组件类型我们没查：`{c.type}` —— 这场没验它）");
                    }
                }

                // 🔴 反例：那台 canvas 相机的 Volume 层掩码必须**够得着**那个 Volume 所在的层。
                //    （踩点：`SetLayerRecursive(Arena3D, ArenaLayer)` 会把 Volume 也刷成 ArenaLayer，
                //      而原版相机的 `m_VolumeLayerMask = 1` 只认 Default 层 ⇒ 不还原就**静默失效**。）
                var bad = 0;
                foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
                    if (v.isGlobal && v.gameObject.layer != 0) bad++;   // `layer` 是**序号**不是掩码
                Check(bad == 0, "★ 全局 Volume 在 **Default 层**（原版相机的 VolumeLayerMask = 1 才够得着它）"
                      + $"（不在 Default 的有 {bad} 个）");

                if (bcam != null)
                {
                    var ad2 = bcam.GetUniversalAdditionalCameraData();
                    Check(ad2 != null && ad2.renderPostProcessing,
                          "★ 战场相机 **`renderPostProcessing = true`**（原版 `m_RenderPostProcessing = 1`）"
                          + " —— 不开这个，上面那些值一个都不会生效");
                }
            }
            else Check(false, "★ 清单里有 `postFx`（原版战场的后处理）");
        }

        // 🆕 **A243（续）：`ColorLookup` 逐场对账** —— 上面那条只看得到**本局这一个战场**
        //   （默认 `BoardArena` = `battlearena1`，恰好是「原版没有专属 LUT」那一档）⇒
        //   「7 场该接的接上了没有」**在默认跑法下还是看不见**。这里把 13 份 `*_PostFx.asset`
        //   一次对完 —— 运行时经 `Resources/ArenaPrefabs/<场>.prefab` 里那个 Volume 用的就是它们。
        //   期望值同样**不从被断的实现里读**：来自原版资源（出处见 `ArenasWithOwnLut`）与清单。
        //   改坏法：把 `ArenaBuilder.ApplyPostFx` 里接 LUT 那一段去掉（或让 `cl.texture` 为空）⇒
        //   那 7 场立刻红；给 `battlearena1` 那 6 场随便接一个 ⇒ 那 6 场红。
        {
            int bad = 0, withLut = 0;
            foreach (var a in ArenaBuilder.AllArenas)
            {
                var mfA = ArenaBuilder.LoadManifest(a);
                string sc = (mfA != null && !string.IsNullOrEmpty(mfA.scene)) ? mfA.scene : a;
                var profA = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                                $"{ArenaBuilder.ProfileDir(sc)}/{sc}_PostFx.asset");
                ColorLookup clA = null;
                if (profA != null && profA.TryGet(out clA) && clA != null && clA.active
                    && clA.texture.value != null) withLut++;
                string why = ColorLookupMismatch(profA, a, mfA);
                if (why != null) { bad++; Debug.LogError(P + $"   ✗ `{a}` 的 ColorLookup：{why}"); }
            }
            Check(bad == 0,
                  $"★ {ArenaBuilder.AllArenas.Length} 场的 `ColorLookup` 都跟原版那一档对得上"
                + $"（实际接上专属 LUT 的 {withLut} 场 = 原版该接的 {ArenasWithOwnLut.Length} 场 · "
                + $"不对的 {bad} 场；逐场理由见上面那些 `✗`）");
        }

        if (hcam != null && arenaRend > 10)
        {
            // 🔴 **2026-09-22 改**：原来这里盯的是 `hcam.clearFlags == Depth`（「HUD 只清深度」那套）。
            //    那套是**两台独立 Base 相机**时代的写法，**真机上战场根本画不出来**（见 §三 第 11 条）。
            //    ⇒ 判据换成「HUD 是 Overlay 且挂在战场相机的 stack 里」。
            var hd = hcam.GetUniversalAdditionalCameraData();
            var bdA = bcam != null ? bcam.GetUniversalAdditionalCameraData() : null;
            Check(hd != null && hd.renderType == CameraRenderType.Overlay,
                  "★ HUD 相机是 **Overlay**（原版做法：base + overlay，后处理作用于合成后整帧）");
            // ⚠️ `cameraStack` 装的是 **`Camera`**，不是 `UniversalAdditionalCameraData`（2026-09-22 编译踩过）
            Check(bdA != null && bdA.renderType == CameraRenderType.Base && bdA.cameraStack.Contains(hcam),
                  "★ HUD 相机挂在**战场相机的 `cameraStack`** 里 —— 不挂的话真机上战场整片画不出来");
            Check((hcam.cullingMask & (1 << ArenaLayer)) == 0, "HUD 相机不重复画 3D 战场那一层");
        }
        // 🔴 §27：兜底支的**开关条件**从「Arena3D 里没内容」换成「**场景里没有 `ArenaRuntimeLoader`**」
        //    —— 现在「内容」本来就为空（要运行时才实例化），拿 `arenaRend` 当条件会**恒为真**。
        if (loaderInScene == null)
        {
            var bd0 = Object.FindObjectOfType<BattleBackdrop>();
            Check(bd0 != null, "（兜底支）存档里有 BattleBackdrop");
            if (bd0 != null)
            {
                bd0.Build();                     // 模拟运行时 Start() 的那一下
                Check(bd0.Ready, $"（兜底支）重新打开后背景能重新绑上（{bd0.Image?.name}）");
            }
        }
        var bd = Object.FindObjectOfType<BattleBackdrop>();
        if (bd != null && bd.transform.Find("Backdrop") != null && loaderInScene != null)
            Debug.LogWarning(P + "   注意：3D 战场和兜底背景图**同时**在场景里（应该只会有一个）");
        Check(Object.FindObjectOfType<BattleDriver>() != null, "存档里有 BattleDriver");

        // ⚠️ 加这条是因为**差点漏掉**：`AttackSelector` 是 `BuildScene` 里新建的节点，
        //    而存档是上一次 `BuildAndSaveScene` 存的 —— 不重建场景，按 Play 就没有选择器，
        //    玩家点自己的单位不会有任何反应。自检里那部分是**当场建的新场景**，验不到存档。
        var sel = Object.FindObjectOfType<BattleDriver>();
        Check(sel != null && sel.selector != null, "存档里的 BattleDriver 接着 AttackSelector");
        Check(Object.FindObjectOfType<AttackSelector>() != null, "存档里有 AttackSelector 节点");
        Check(sel != null && sel.reticle != null, "存档里的 BattleDriver 接着 TargetReticle");
        Check(Object.FindObjectOfType<TargetReticle>() != null, "存档里有 TargetReticle 节点");
        Check(sel != null && sel.skillPanel != null, "存档里的 BattleDriver 接着 SkillPanel");
        Check(Object.FindObjectOfType<SkillPanel>() != null, "存档里有 SkillPanel 节点");
    }

    static string[] HandNames(BattleContext ctx, int p)
    {
        var l = new List<string>();
        foreach (var c in ctx.Players[p].Hand) l.Add($"{c.Card.Name}({c.Card.Cost})");
        return l.ToArray();
    }

    /// <summary>场上**部队**数（**不含督军** —— 它一直占着一格，数进去会把差别抹平）。</summary>
    static int UnitsOnBoard(BattleContext ctx, int p)
    {
        int n = 0;
        var b = ctx.Players[p].Board;
        for (int i = 0; i < b.Length; i++)
            if (i != BoardSpec.WarlordSlot && b[i] != null) n++;
        return n;
    }

    static CardDef CardByName(List<CardDef> pool, string name)
    {
        foreach (var c in pool) if (c.Name == name) return c;
        return pool[0];
    }

    // ==================================================================
    //  建场景
    // ==================================================================

    /// <summary>原版战场的根在 x≈100，我们的一切在 x=0 ⇒ 场地整体平移这么多。</summary>
    const float ArenaOriginX = 100f;

    /// <summary>3D 战场专用图层（空图层 8）—— 透视相机只渲它，HUD 相机把它从 cullingMask 里摘掉。</summary>
    // 判据只此一处 = `ArenaSlots.ArenaLayer`（运行时也要用 —— 场上的卡要挂这一层）
    const int ArenaLayer = ArenaSlots.ArenaLayer;

    /// <summary>原版**运行时**的取景算法 `CameraVerticalFramer.CalculateFraming`（2026-09-20 接上）。
    ///
    /// 🔴 **为什么必须有它**：场景里那个静态 `lensShift.y = −0.205` **不是运行时的值** ——
    /// 原版在 `BattleCameraSreenSize.Initialize` 里按 **屏幕宽高比 + 手牌缩放** 现算一遍，
    /// 再 DOTween 写回相机。照抄静态值 ⇒ 宽高比一变取景就错。
    ///
    /// 逐项出处（**全是实读，没有一个是推的**）：
    /// · 公式 = `D:/2/tools/decomp_full/CameraVerticalFramer__CalculateFraming.c`
    /// · 三条曲线与 `maxVerticalSizeInViewPort` = `07_场景/battlearena1/MonoBehaviour/MonoBehaviour_4697.json`
    /// · `k = 0.5` = `DAT_1834b2bb4`（**从 `GameAssembly.dll` 直读**：VA→RVA→`.rdata`）
    /// · `zoom` 默认 `1.0` = `DAT_1834b2bb8`，也是 `CombatCameraZoom.GetMaxZoomLevel` 的返回
    /// · 两个 helper 的几何 = 运行时 UI dump（`runtime_ui_dump_drive_0912.tsv:340,348`）：
    ///   `EnemyCardAreaSizeHelper Data`（父 `UpperAnchor`，100×**96.7**，pivot **(0.5,1)** ⇒ 下沿）
    ///   `PlayerCardAreaSizeHelper To Use`（父 `BottomAnchor`，100×**249.9**，pivot **(0.5,0)** ⇒ 上沿）；
    ///   而 `UpperAnchor`/`BottomAnchor` 的 anchor 分别贴在 canvas 的**顶 / 底** ⇒ 直接换成 viewport y。
    /// · 角点：max 读 **corner[0]=左下**、min 读 **corner[2]=右上**（`CalculateFraming` 里读的是
    ///   `+0x20` 与 `+0x38`，数组从 `+0x20` 起、每点 12 字节）。
    /// </summary>
    static class BoardFramer
    {
        const float Zoom = 1f;                     // DAT_1834b2bb8

        // 🔴 **2026-10-12（A175，判据收成一处）**：这里的公式、常量与三条曲线**搬进了运行时**
        //    `Battle/CombatAutoZoom.cs` 的 `CombatAutoZoom.LensShiftY(aspect, zoom)` —— 理由：
        //    自动缩放（那个组件）要在**运行时**按 `zoom ≠ 1` 现算取景，而本文件在 `Editor` 程序集里
        //    （`Assets/**/Editor/` ⇒ `Assembly-CSharp-Editor`），运行时那份**引不到它**。
        //    ⇒ 与其抄两份（工程规矩：两处写同一条规则 = 迟早不一致），不如**这一处转调那一处**。
        //    ⚠️ 逐项出处（公式 / 三条曲线 / `k = 0.5` / 两个 helper 的几何 / 角点读法）**原样留在
        //    `CombatAutoZoom.cs` 的取景那一段**，别在这里重抄一遍（两份迟早打架）。
        //    `Zoom = 1f` 保留：**烘场景那份取景永远是「不缩放」那一档**（= 原版 `SetZoomLevel` 在
        //    开关关着时给的那个常量），自动缩放只在运行时改它。

        /// <summary>这个宽高比下原版会用的 `lensShift.y`（**不缩放**那一档，= `CombatAutoZoom` 的 `zoom = 1`）。
        /// 真值来自原版 `CameraVerticalFramer.CalculateFraming`，实现与出处 → `Battle/CombatAutoZoom.cs`。</summary>
        public static float LensShiftY(float aspect)
        {
            return CardPresentation.CombatAutoZoom.LensShiftY(aspect, Zoom);
        }
    }

    /// <summary>3D 战场相机的 `lensShift.y`（历史注释留在下面，现在的真值来自 `BoardFramer`）。
    /// 🔴 **公开**：独立战场场景（`ArenaBuilder.BuildSceneTail`）也用这一份 ——
    /// 两台相机必须是**同一套取景判据**，不然预览图和战斗画面不一样（2026-09-20 踩过：
    /// 独立场景那台**没开物理相机** ⇒ `lensShift` 被忽略 ⇒ 预览比原版**亮 42%**、多出半屏天空）。</summary>
    public static float BoardLensShiftY() { return BoardFramer.LensShiftY(16f / 9f); }

    // ==================================================================
    //  A243 · `ColorLookup` 的判据（原来自检里这一层是**隐形的**：`foreach` 里既没分支也没 `else`）
    // ==================================================================

    /// <summary>原版**各带一张专属 LUT** 的那 7 个战场（其余 6 场指向**共享的** `LUT Normal` —— 严格恒等）。
    /// 🔴 判据 = **逐场亲读原版资源**（⛔ 不是从我们自己的实现里推的）：
    ///   `d:/2/解包整理/07_场景/&lt;场&gt;/MonoBehaviour/ColorLookup_*.json` 的 `texture.m_Value` ——
    ///   `m_FileID **= 0**`（= **本条包内**那张 `LUT &lt;场&gt;`）的正好这 7 场；另外 6 场是
    ///   `m_FileID = 9` + `m_PathID = 382974660631151556`（= `battlesharedresources` 里那份 `LUT Normal`）。
    ///   · 这 7 张：与恒等 LUT **偏差中位 7/255 · p90 11/255**（一层温和调色）⇒ **必须画**；
    ///   · `LUT Normal`：256×16 条带按 16³ 展开后 **4096 个采样点偏差 0/255**（= 恒等）。
    ///     画不画**视觉等价**（只多一趟 3D LUT 采样）⇒ 我们**不接**，下面那条断言钉的就是「没接」。
    /// ⚠️ 名字规则也是原版给的：`&lt;profile 名&gt; − " PostProcessing"` → `LUT &lt;剩下那截&gt;`
    ///   （7 个场景包的 `Texture2D/` 里各只有这一张 LUT，例如 `battlearenasororitas` → `LUT Battle Arena Sororitas`）。</summary>
    static readonly string[] ArenasWithOwnLut = {
        "battlearenaaeldari", "battlearenaemperorschildren", "battlearenagenestealers",
        "battlearenaleviathan", "battlearenasororitas", "battlearenaspacewolves",
        "battlearenatauviorla",
    };

    /// <summary>`ColorLookup` 这一层对不对（**A243**）。`null` = 对；否则返回一句「哪里不对」。
    /// 期望值**全来自原版**：`contribution` 从**清单**读（原版实读，13/13 场都是 1.0）、
    /// 「该不该在」由 `ArenasWithOwnLut` 定（出处见它自己的注释）⇒ ⛔ 不从被断的实现里读常量。
    /// 两态：① 7 场 ⇒ `active` + `texture != null` + 贴的是**这张场的** `LUT &lt;场&gt;` + contribution = 清单值；
    ///         ② 其余 6 场 ⇒ **不该在**（原版那一个是指向恒等 `LUT Normal` 的 override）。</summary>
    static string ColorLookupMismatch(VolumeProfile prof, string arena, ArenaBuilder.Manifest mf)
    {
        ColorLookup cl = null;                                   // ⚠️ 先赋 null：`TryGet` 在短路里 ⇒ 编译器判不了「一定赋过值」
        bool has = prof != null && prof.TryGet(out cl) && cl != null && cl.active;
        if (System.Array.IndexOf(ArenasWithOwnLut, arena) < 0)
            return has ? "这场原版指的是共享的**恒等** `LUT Normal`（16³ 展开后偏差 0/255）⇒ 我们不该接，现在接了一个"
                       : null;
        if (!has) return "这场原版有**自己的** LUT（`LUT <场>`，与恒等偏差中位 7/255）⇒ 该接，现在没有";
        if (cl.texture.value == null) return "接了，但 `texture` 是空的（= 静默不生效，2026-09-25 踩过）";
        // 🔴 `overrideState` **也是承重的**：`VolumeComponent.Override` 只搬 `overrideState == true` 的参数
        //   （本机 `com.unity.render-pipelines.core@…/Runtime/Volume/VolumeComponent.cs:280-296`）
        //   ⇒ 没打勾 = 这一路**根本没进 Volume 栈**、采的还是默认的空纹理（静默、且渲染上看不出来）。
        if (!cl.texture.overrideState || !cl.contribution.overrideState)
            return "`texture`/`contribution` 没打 override ⇒ 这两个值**根本进不了 Volume 栈**（`VolumeComponent.Override` 只搬打勾的）";
        string baseName = (mf != null && mf.postFx != null && mf.postFx.profile != null)
                        ? mf.postFx.profile.Replace(" PostProcessing", "").Trim() : "";
        string want = "LUT " + baseName;
        if (cl.texture.value.name != want)
            return $"贴的不是这张场的 LUT（现在是 `{cl.texture.value.name}`，按原版命名应当是 `{want}`）";
        float contrib = 0f;
        if (mf != null && mf.postFx != null && mf.postFx.components != null)
            foreach (var c in mf.postFx.components)
                if (c != null && c.type == "ColorLookup") contrib = c.contribution;
        if (Mathf.Abs(cl.contribution.value - contrib) > 0.001f)
            return $"contribution {cl.contribution.value:F3} ≠ 清单里的 {contrib:F3}";
        return null;
    }

    /// <summary>把原版战场（网格 + 粒子）建到 `root` 下，返回**可渲染件数**（0 = 建不出来，调用方要兜底）。
    /// 参数与做法**全在 `ArenaBuilder.BuildContent`**（与独立场景模式共用同一段，判据只留一处）。</summary>
    /// <summary>🆕 **2026-09-30（§27 架构）**：战场**不再烘进场景**，改成运行时实例化。
    /// 判据与施工图 → `资料/§27架构_施工图.md`：
    ///   · 13 件 prefab 在 `Assets/Resources/ArenaPrefabs/<场>.prefab`（`ArenaBuilder.BuildArenaPrefabs` 建），
    ///     每件带一个 `ArenaPrefabData`（雾 / 环境光 / 天空盒 / 相机光学那套**场景级**值）；
    ///   · 这里只给 `Arena3D` 挂 `ArenaRuntimeLoader`，**进局时**（`BattleDriver.Begin`）按督军阵营取一件实例化。
    /// ⚠️ **编辑期刻意不实例化** —— 场景里再放一份的话运行时 `Load` 会再建一份（它认不出场景里那份）
    ///   ⇒ 一个场景两份战场。所以打开 `Battle.unity` 看到的是**空 Arena3D**、按 Play 才出战场。</summary>
    static int BuildArena3D(GameObject root)
    {
        root.AddComponent<CardPresentation.ArenaRuntimeLoader>();
        Debug.Log(P + "   3D 战场：§27 —— **运行时实例化**（`ArenaRuntimeLoader`；"
                    + "13 件 prefab 在 `Resources/ArenaPrefabs/`），编辑期不烘内容");
        return 1;      // 1 = 有战场（运行时那条路）
    }

    /// <summary>透视的战场相机 —— **逐值照原版**（`ArenaBuilder.ManifestPath(BoardArena)` 的 `camera`，四处一致的那个）。
    /// 画在 `ArenaLayer` 上、`depth = -1`（先画 3D，HUD 相机再叠上去）。</summary>
    static Camera BuildBoardCamera(Transform parent)
    {
        var mf = ArenaBuilder.LoadManifest(BoardArena);
        var d = mf != null ? mf.camera : null;
        var go = new GameObject("BoardCamera 透视（原版值）");
        go.transform.SetParent(parent, false);

        var c = go.AddComponent<Camera>();
        // 🔴 光学部分**共用一份判据** = `ArenaBuilder.ConfigureBoardCamera`
        //    （独立战场场景的预览相机也调它 —— 两边不一致的话预览图不能当验收靶子）。
        ArenaBuilder.ConfigureBoardCamera(c, d, BoardLensShiftY());
        c.clearFlags     = CameraClearFlags.SolidColor;
        // 🔴 **2026-09-20 修：必须开「物理相机」模式，`lensShift` 才生效。**
        //    原来只设了 `c.lensShift`、**从没设 `usePhysicalProperties`** ⇒ 那个值被 Unity
        //    **静默忽略**（`lensShift` 属于物理相机设置）。症状：整个 3D 战场比原版**低 ≈150 px**
        //    （我方行落在 79%、原版实测 65.6%）—— 而 FOV 与 lensShift 两个数都「照抄对了」。
        //    原版本身就是物理相机：`Camera_1461.json` 的 `m_FocalLength 28.0` /
        //    `m_SensorSize (41.5, 24.0)` / `m_GateFitMode 2`（Horizontal）—— 逐值照抄。
        //    ⚠️ 开了之后 `fieldOfView` 会被忽略，视角由 focal/sensor/gateFit 决定
        //    （16:9 下 vFOV ≈ 45.26°，不是按**竖直** fit 算的 46.397°）。
        //    ⚠️ 原版运行时还有 `CameraVerticalFramer.CalculateFraming` 按宽高比/手牌缩放**重算**
        //    `lensShift` 与 `sensorSize`（曲线 `viewShiftModifier` / `cameraSizeXTable`）——
        //    ⚠️ `lensShift` 那半**已经接了**（`BoardFramer.LensShiftY`，有断言盯着）；
        //    **没接的是 `cameraSizeXTable` 那半** —— 按「垂直视口高」改 `sensorSize.x`，
        //    在 `zoom = 1` 时该式**恒等于不变** ⇒ 16:9 下不生效，**窄屏（zoom<1）才要它**。
        //    详见 `资料/3DBody_原版场上卡体规格.md` §四之五。
        //    ↑ 上面这段值已经搬进 `ArenaBuilder.ConfigureBoardCamera`（与独立场景共用），这里只剩其余设置。
        c.backgroundColor = new Color(0.055f, 0.06f, 0.08f);
        c.depth          = -1;                 // 先画 3D；HUD 相机 depth 0 且只清深度 ⇒ 叠在上面
        c.cullingMask    = 1 << ArenaLayer;    // 只画 3D 战场那一层

        // 🆕 2026-09-20：后处理开关已挪进 `ArenaBuilder.ConfigureBoardCamera`（与独立场景共用一份）。

        // 位置 = 原版相机位置 − 场地平移；朝向 = 单位四元数（原版就是朝 +Z）
        var pos = (d != null && d.pos != null && d.pos.Length >= 3)
                ? d.pos : new[] { 100f, 2.222075f, -13.57198f };
        go.transform.localPosition = new Vector3(pos[0] - ArenaOriginX, pos[1], pos[2]);
        go.transform.localRotation = Quaternion.identity;
        return c;
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
    }

    /// <summary>3D 战场那台透视相机（`depth &lt; 0`）。没有 3D 场地时返回 null。
    /// ⚠️ 批处理下要**显式**把两台相机按顺序渲进同一张 RT，见 `Shot`。</summary>
    static Camera FindBoardCamera()
    {
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (c.depth < 0) return c;
        return null;
    }

    public static Camera BuildScene(out BattleDriver driver, out BoardLayout playerBoard,
                                    out BoardLayout enemyBoard, out CardInteraction interaction)
    {
        var sceneRoot = new GameObject("Battle");

        // 相机（布局基准，见 LayoutSpace）
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.055f, 0.06f, 0.08f);
        LayoutSpace.Apply(cam);                       // ⚠️ 必须在建任何卡之前 —— 坐标换算要用
        // ⚠️ 也必须**先把宽高比定死**再建东西：HUD 文字和格位底片的位置都是建的时候算一次的，
        //    批处理下相机的默认宽高比是 4:3，不先定死的话 16:9 的截图里它们会整片偏左（踩过）
        cam.aspect = LayoutSpace.DesignAspect;
        camGo.transform.position = new Vector3(0f, 0f, -20f);

        // ---- 战场：**真 3D**（原版 battlearena1 重建），不再是一张烘平的图（2026-09-19）----
        //
        // **两层相机**（照原版的结构 —— 原版就是 `BoardCamera` + `UI Camera` 两台）：
        //   · `Arena3D` 下的 29 个网格 + 34 个粒子 → **透视**的 `BoardCamera`，
        //     参数逐值取自台单（= 原版实读，不是我们挑的）：
        //     FOV 46.397182 · near 0.3 · far 300 · lensShift.y −0.205 · pos(100, 2.222075, −13.57198) · 朝 +Z
        //   · 卡牌与 HUD 仍归上面那台**正交**相机 —— 布局坐标全是按它算的，**不能动**
        // 场地整体平移 `−ArenaOriginX`：原版战场在 x≈100、我们的一切在 x=0。平移之后这台相机的
        // 取景与原来的烘图**同一套**（那张图就是这台相机渲的）⇒ 2D 层照样对得上。
        var arenaGo = new GameObject("Arena3D");
        arenaGo.transform.SetParent(sceneRoot.transform, false);
        arenaGo.transform.localPosition = new Vector3(-ArenaOriginX, 0f, 0f);

        // 兜底：3D 资产（Models/Textures 是本地件、不进仓库）不在时退回烘好的背景图。
        // ⚠️ **两条路只留一条**：3D 场地在的时候**不挂** `BattleBackdrop` ——
        //    它的 `Build()` 会自己造一张铺满的 quad，那会把 3D 战场整个盖住（而且 `Start()` 会再 Build 一次）。
        BattleBackdrop backdrop = null;
        Camera boardCam = null;
        int arenaRend = BuildArena3D(arenaGo);
        if (arenaRend > 0)
        {
            // 🔴 **2026-09-30（§27）**：原来这里要 `SetLayerRecursive(arenaGo, ArenaLayer)`、并把 Volume
            //   还原到 Default 层 —— 这两件事现在**烘在 prefab 里**（`ArenaBuilder.BuildOneArenaPrefab`）。
            //   判据一模一样，只是执行时机从「建场」挪到「建 prefab」：
            //   · 整棵树 → `ArenaLayer`（HUD 相机不吃这一层）· Volume 子物体 → `Default`（否则后处理**静默失效**）
            boardCam = BuildBoardCamera(sceneRoot.transform);
            // 🔴🔴 **2026-09-22 修（阻断级）：两台「独立 Base 相机」在真机上把战场整个弄没了。**
            //    实测（真包 `-wfshot` 的 `DumpCameraRenders`）：`BoardCamera` **单独渲**「有内容像素 99% · 均亮 93」
            //    —— 战场画得好好的；而**屏幕上**只剩那台相机的清屏色 ⇒ **后一台 Base 相机的 final blit
            //    把前一台整个盖掉了**。（`Shot()` 里 2026-09-19 那条注释就记过这个现象，
            //    当时判断「只影响 RT 路」—— **屏上路一模一样**。）
            //    ⇒ 照原版改：**base + overlay**（原版就是 base + overlay，后处理作用于**合成后整帧**）。
            //    判据与全部证据链：`项目任务.md` §三 第 11 条。
            cam.cullingMask &= ~(1 << ArenaLayer);          // HUD 不重复画 3D 战场那一层
            var boardData = boardCam.GetUniversalAdditionalCameraData();
            var hudData = cam.GetUniversalAdditionalCameraData();
            boardData.renderType = CameraRenderType.Base;
            hudData.renderType = CameraRenderType.Overlay;
            if (!boardData.cameraStack.Contains(cam)) boardData.cameraStack.Add(cam);
        }
        else
        {
            Debug.LogWarning(P + "   3D 战场建不出来（OBJ/贴图不在？）⇒ 退回烘好的背景图");
            Object.DestroyImmediate(arenaGo);
            backdrop = sceneRoot.AddComponent<BattleBackdrop>();
            backdrop.Build();
        }

        // 对手半场（上）—— **镜像**：原版敌方的 leftSlotPosNormal 是 +x，
        // 所以敌方槽 0 显示在画面**右侧**，两边的 0 号位在各自的左边（面对面）
        var eGo = new GameObject("EnemyBoard");
        eGo.transform.SetParent(sceneRoot.transform, false);
        enemyBoard = eGo.AddComponent<BoardLayout>();
        enemyBoard.lineY = EnemyLineY;
        enemyBoard.spacing = BoardSpacing;
        enemyBoard.placedScale = BoardScale;
        enemyBoard.mirror = true;
        enemyBoard.EnsureMarkers();

        // 我的半场（下）
        var pGo = new GameObject("PlayerBoard");
        pGo.transform.SetParent(sceneRoot.transform, false);
        playerBoard = pGo.AddComponent<BoardLayout>();
        playerBoard.lineY = PlayerLineY;
        playerBoard.spacing = BoardSpacing;
        playerBoard.placedScale = BoardScale;
        playerBoard.EnsureMarkers();

        // 手牌
        var handGo = new GameObject("Hand");
        handGo.transform.SetParent(sceneRoot.transform, false);
        var hand = handGo.AddComponent<HandLayout>();
        hand.baselineY = HandBaselineY;
        hand.cardScale = HandScale;

        // 敌方手牌（原版 `PlayerHand` MB 4350 + `CardsHorizontalLayout` MB 4053）——
        // 🔴 **2026-09-17 新加**：之前整件都没有，所以 `timeToDrawEnemyCard` 一直无处可落。
        //    八个字段 + 三条曲线整套是原版值，`ConfigureForEnemy()` 里逐条注了出处；
        //    规格正本 = `资料/敌方手牌_原版规格.md`。
        var foeHandGo = new GameObject("FoeHand");
        foeHandGo.transform.SetParent(sceneRoot.transform, false);
        var foeHand = foeHandGo.AddComponent<HandLayout>();
        foeHand.ConfigureForEnemy();

        // 交互
        interaction = sceneRoot.AddComponent<CardInteraction>();
        interaction.cam = cam;
        interaction.board = playerBoard;
        interaction.hand = hand;

        // 驱动
        driver = sceneRoot.AddComponent<BattleDriver>();
        // 🆕 2026-09-26：**自检默认钉住「我方先手」** —— 理由见 `BattleDriver.ForceFirstSeat` 那段注释：
        //   这一份自检里有十几条「回合流程」断言写死了「我方在第 1 回合行动」（那是先手才成立的账）。
        //   ⚠️ **掷硬币那半边另有覆盖**：第 9b / 9c 两节会把这里清成 `null`，然后**按角色**断（先手/后手各一套）。
        driver.ForceFirstSeat = 0;
        driver.cam = cam;
        driver.boardCam = boardCam;        // 3D 战场的透视相机（震镜头要两台一起推，见 `CardFeel.ShakeCamera`）
        // 🔴 2026-09-20：**场上的卡搬进 3D 那一层**（站 `MinionArea` 线上、缩放取原版 `desiredScale`）。
        //    判据 = `ArenaSlots`；退化成烘图时（boardCam == null）必须为 false，否则卡没相机画。
        driver.use3DBoard = boardCam != null;
        // 落点判定也得跟着走 3D —— 否则「看着落在卡上、判定落在别处」（两套屏幕位置差 ≈150 px）。
        // ⚠️ 与 `driver.use3DBoard` 是**同一个开关的两个落点**，必须一起设。
        playerBoard.use3D = enemyBoard.use3D = boardCam != null;
        playerBoard.boardCam = enemyBoard.boardCam = boardCam;
        driver.playerBoard = playerBoard;
        driver.enemyBoard = enemyBoard;
        driver.hand = hand;
        driver.foeHand = foeHand;
        driver.interaction = interaction;
        driver.boardRoot = sceneRoot.transform;
        driver.backdrop = backdrop;

        // 攻击方式选择器（原版 `Drag Attack Selector`）—— 单独一个节点，方便整块开关
        //
        // ⚠️ **这里只建空节点，不调 `Build()`** —— 它的按钮/底板/文字都是**运行时生成的**
        //    （和卡牌一样：存进场景的话 `ImageQuad` 的私有 `_tex`/运行时 `Material` 都不进序列化，
        //    重新打开就是一堆没材质的空壳）。`AttackSelector.Awake()` 会在 Play 时建。
        //    批处理自检没有 Awake，所以 `Run()` 里会显式补一次 `Build()`。
        var selGo = new GameObject("AttackSelector");
        selGo.transform.SetParent(sceneRoot.transform, false);
        driver.selector = selGo.AddComponent<AttackSelector>();

        // 选目标反馈：准星 + 弧线（原版 `NoCanvas2D/Attack Target Reticle`）—— 同样只建空节点，
        // 理由和上面选择器一模一样（准星是 `ImageQuad`，弧线是 `LineRenderer` + 运行时 `Material`）
        var retGo = new GameObject("TargetReticle");
        retGo.transform.SetParent(sceneRoot.transform, false);
        driver.reticle = retGo.AddComponent<TargetReticle>();

        // 技能卡面板（原版 `ActiveSkillDesc`）—— 同样只建空节点
        var skGo = new GameObject("SkillPanel");
        skGo.transform.SetParent(sceneRoot.transform, false);
        driver.skillPanel = skGo.AddComponent<SkillPanel>();

        // 特效钩子（特效库不在时只记日志，不影响流程）
        int shot = 0;
        _fired.Clear();
        CardEffects.Play = (name, pos, parent) =>
        {
            // 记名字：断言「引擎发了事件，画面真的播了对应的特效」靠它，
            // 不然只能看截图猜（截图看不出「播的是不是该播的那个」）
            _fired.Add(name);
            var fx = WarpforgeVFX.WarpforgeEffectPlayer.Play(name, null, pos, 1f, -1f);
            shot++;
            Debug.Log(P + $"   [特效] {name} @ {pos} → {(fx == null ? "**没播出来**" : fx.name)}"
                        + $"（活着的播放器 {WarpforgeVFX.WarpforgeEffectPlayer.ActiveCount} 个）");
        };

        return cam;
    }

    /// <summary>自检期间 `CardEffects.Play` 收到过的特效名（按顺序）</summary>
    static readonly List<string> _fired = new List<string>();

    /// <summary>某个特效名在这一次里播过几次</summary>
    static int FiredCount(string effectName)
    {
        if (string.IsNullOrEmpty(effectName)) return 0;
        int n = 0;
        foreach (var s in _fired) if (s == effectName) n++;
        return n;
    }

    /// <summary>某一方第一个空的部署格，满了返回 -1</summary>
    static int FreeSlot(BattleContext ctx, int owner)
    {
        var p = ctx.Players[owner];
        for (int s = 0; s < BoardSpec.Size; s++)
            if (BoardSpec.IsDeployable(s) && p.Board[s] == null) return s;
        return -1;
    }

    /// <summary>
    /// 清掉之前几小节遗留的特效。
    ///
    /// ⚠️ **批处理下没有帧循环** —— `WarpforgeEffectPlayer` 靠 `Update` 自毁，自检里它不会被调，
    ///    于是每小节的效果一直堆着（实测播到第 24 个还在）。特别是阵亡特效 `Explosion_Possession`
    ///    会甩出满屏橙红火线，后面几张截图全被它盖住，人眼验收根本没法看。
    ///    **游戏里不存在这个问题**（有 Update），纯粹是自检环境的账。
    /// </summary>
    static void ClearEffects()
    {
        var alive = new List<WarpforgeVFX.WarpforgeEffectPlayer>();
        foreach (var p in WarpforgeVFX.WarpforgeEffectPlayer.ActivePlayers)
            if (p != null) alive.Add(p);
        foreach (var p in alive) p.Kill();
        Debug.Log(P + $"   （清掉 {alive.Count} 个遗留特效 —— 批处理里没有 Update，它们不会自己消失）");
    }

    /// <summary>诊断用：把一台相机单独渲成 PNG（判「相机没渲东西」还是「被别的相机盖了」）</summary>
    static bool _shot3DDumped;
    static bool _shot3DDumped2;
    static void DumpCam(Camera c, int W, int H, string path)
    {
        var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
        c.targetTexture = rt;
        c.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        c.targetTexture = null;
        var px = tex.GetPixels();
        double sum = 0; int lit = 0;
        var bg = c.backgroundColor;
        foreach (var p in px)
        {
            if (Mathf.Abs(p.r - bg.r) + Mathf.Abs(p.g - bg.g) + Mathf.Abs(p.b - bg.b) > 0.02f) lit++;
            sum += p.r + p.g + p.b;
        }
        Debug.Log(P + $"   [3D诊断] 单独渲染：lit={lit}/{px.Length} sum={sum:F0} → {path}");
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
    }

    static void Shot(Camera cam, string name)
    {
        const int W = 1920, H = 1080;
        cam.aspect = (float)W / H;

        // 🆕 2026-09-19：战场是真 3D 了 ⇒ 截图要**两台相机都渲**（批处理下没有帧循环，
        //    不显式渲第二台就是「只有 HUD、背景一片空」）；合成方式见下面那张 RT 的注释。
        var board3D = FindBoardCamera();
        if (board3D != null)
        {
            board3D.aspect = (float)W / H;

            // 诊断（`WFSHOT_3D=1`）：把 3D 相机**单独**渲一张，用来判「是相机没渲东西」还是「被 HUD 盖了」
            if (System.Environment.GetEnvironmentVariable("WFSHOT_3D") == "1" && !_shot3DDumped)
            {
                _shot3DDumped = true;
                var b = new Bounds();
                bool first = true;
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    if (r.gameObject.layer != ArenaLayer) continue;
                    if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
                }
                Debug.Log(P + $"   [3D诊断] 相机 pos={board3D.transform.position} rot={board3D.transform.rotation.eulerAngles} "
                            + $"mask={board3D.cullingMask} depth={board3D.depth} fov={board3D.fieldOfView} "
                            + $"lens={board3D.lensShift} clear={board3D.clearFlags} enabled={board3D.enabled}");
                Debug.Log(P + $"   [3D诊断] 该层的可渲染件包围盒 center={b.center} size={b.size}（first={!first}）");
                DumpCam(board3D, W, H, "d:/4/_tmp_view/battle/_3donly.png");
            }
        }

        // 🔴 **2026-09-19 实测：URP 下「两台相机渲进同一张 RT」做不到** ——
        //    3D 那层确实进了 RT（中央平均亮度 0.321 实测），但第二台相机一渲，
        //    URP 的最终 blit 就把整张**覆盖**掉（`clearFlags` 设成 `Depth`/`Nothing` 都一样）。
        //    ⇒ 截图这一路改成**两张 RT 手工合成**：3D 一张（不透明）、HUD 一张（**透明底**），
        //    再按 HUD 的 alpha 叠起来。运行时不受影响（那边是 Unity 自己的相机循环，按 depth 顺序合成）。
        var rtA = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);   // 3D 层
        var rtB = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);   // HUD 层（透明底）

        if (board3D != null)
        {
            board3D.targetTexture = rtA;
            board3D.Render();
            board3D.targetTexture = null;
        }

        var prevActive = RenderTexture.active;
        Texture2D tex;
        if (board3D != null)
        {
            // 🔴 **2026-09-22 改**：HUD 现在是战场相机的 **Overlay 子相机** ⇒ `board3D.Render()`
            //    一次就把「3D 战场 + HUD 画布」渲进**同一张 RT**，**不用再手工合成**。
            //    （原来那条「两张 RT 按 alpha 叠」正是为了绕开「两台 base 相机进不了同一张 RT」——
            //      而它同时**把真机上的病盖住了**：真包里战场整片画不出来，自检图却全绿。
            //      判据与证据链见 `项目任务.md` §三 第 11 条。）
            RenderTexture.active = rtA;
            tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
        }
        else
        {
            // 兜底支（3D 战场建不出来 ⇒ 没有相机栈可依附）：HUD 自己渲一张，用透明底
            var savedClear = cam.clearFlags;
            var savedBg = cam.backgroundColor;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.targetTexture = rtB;
            cam.Render();
            cam.targetTexture = null;
            cam.clearFlags = savedClear;
            cam.backgroundColor = savedBg;
            RenderTexture.active = rtB;
            tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
        }
        RenderTexture.active = prevActive;
        RenderTexture.ReleaseTemporary(rtA);
        RenderTexture.ReleaseTemporary(rtB);

        File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log(P + $"   [图] {OutDir}/{name}.png");
    }

    /// <summary>日志里截断长文本用（原版卡的效果文字很长，整条打出来刷屏）</summary>
    static string Short(string s, int n)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= n) return s ?? "";
        return s.Substring(0, n) + "…";
    }

    /// <summary>
    /// **这一方手牌里「凭空生成」的牌有几张** —— 目前只有**天赋**（`RuleCore.SpawnTalents`）生成的战术卡。
    ///
    /// **为什么自检需要它**（2026-09-13 第三十四轮）：天赋卡是**回合开始时从卡池现拿的**，
    /// **不属于那 30 张卡组** ⇒「手牌 + 牌库 = 卡组张数」这类不变量会被它**顶掉一张**，
    /// 于是本轮 `BattleScene` 一次红了 7 条**计数**断言（不是引擎错，是断言写死了老行为）。
    ///
    /// ⚠️ 判据用 `BattleContext.MarkedEphemeralCount`（**只数标记**）——
    ///    **不要**用 `IsEphemeral`：那会把「卡面自带 `Ephemeral`」的 98 张也算进来，
    ///    而那些**本来就是卡组里的牌**，减掉它们就等于把不变量改错了方向。
    /// </summary>
    static int Conjured(BattleContext ctx, int p)
    {
        int n = 0;
        foreach (var c in ctx.Players[p].Hand)
            n += ctx.MarkedCount(c);        // 第 7 行第 3 步：标记住在**每一份**上
        return n;
    }

    /// <summary>🆕 2026-09-26：找一个**让指定座位先手**的对局种子（`0` = 我方先手 · `1` = 我方后手）。
    /// ⚠️ **走 `BattleDriver.FirstSeatForSeed`** —— 那是「谁先手」的唯一算法，**别在这儿把公式再抄一遍**
    /// （两处写同一条规则迟早不一致）。用途：让自检**确定性地**覆盖先手 / 后手两条路（见 9b / 9c）。</summary>
    static int SeedForFirstSeat(int seat)
    {
        for (int s = 1; s < 100000; s++) if (BattleDriver.FirstSeatForSeed(s) == seat) return s;
        return 1;
    }

    /// <summary>
    /// 从真卡池里凑一副**合法**卡组：1 督军 + 1 防御卡 + 30 张同阵营卡。
    /// 其中**前 <paramref name="tactics"/> 张故意放战术卡** —— 战术卡引擎还不支持（`ErrUnimplemented`），
    /// `DeckBuilder.FromDeck` 一定会把它们丢掉，正好用来量「丢了几张」那句话说得对不对。
    /// （同名上限照 `DeckRules.CopyLimit` 走，不然它自己就先不合法的。）
    /// </summary>
    static PlayerDeck MakeDeck(List<CardDef> pool, string faction, int tactics, string name, int gameMode = 0)
    {
        CardDef warlord = null, def = null;
        foreach (var c in pool)
        {
            if (c == null || c.Faction != faction) continue;
            if (warlord == null && c.Type == "hero") warlord = c;
            if (def == null && c.Type == "defence") def = c;
        }
        if (warlord == null) return null;

        // 🆕 2026-09-26：张数**按模式算**（遭遇 12 / 经典 30），模式也**打进这副牌** ——
        //    原来写死 `CardCount(false)`，做遭遇那条链时会**凑出 30 张的「遭遇牌」**（自相矛盾）。
        int want = DeckRules.CardCount(gameMode == (int)GameMode.Skirmish);
        var ids = new List<string>();
        foreach (var c in pool)                       // 战术卡先塞（这些是要被丢掉的那批）
        {
            if (ids.Count >= tactics || c == null || c.Type != "tactic" || c.Faction != faction) continue;
            for (int i = 0; i < DeckRules.CopyLimit(c.Rarity) && ids.Count < tactics; i++) ids.Add(c.Id);
        }
        foreach (var c in pool)                       // 剩下的用单位卡填满
        {
            if (ids.Count >= want || c == null || c.Type != "unit" || c.Faction != faction) continue;
            for (int i = 0; i < DeckRules.CopyLimit(c.Rarity) && ids.Count < want; i++) ids.Add(c.Id);
        }
        return new PlayerDeck(name, warlord.Id, def == null ? null : def.Id, ids, gameMode);
    }

    /// <summary>
    /// 自检用：凑一副**带指定战术卡**的合法卡组（1 督军 + 1 防御 + 28 单位 + 1 张指定战术 = 30）。
    ///
    /// 为什么要单独一个：`MakeDeck` 塞的战术卡是不挑的，拖拽用例需要**确定是这张**
    /// （能完整解析、且只打敌方一个单位 —— 否则 `FromDeck` 会把它丢掉，就没得拖了）。
    /// </summary>
    static PlayerDeck DeckWithTactic(List<CardDef> pool, string faction, string tacticName)
    {
        CardDef warlord = null, def = null, tactic = null;
        foreach (var c in pool)
        {
            if (c == null || c.Faction != faction) continue;
            if (warlord == null && c.Type == "hero") warlord = c;
            if (def == null && c.Type == "defence") def = c;
            if (tactic == null && c.Type == "tactic" && c.Name == tacticName) tactic = c;
        }
        if (warlord == null || def == null || tactic == null) return null;

        int want = DeckRules.CardCount(false);
        var ids = new List<string> { tactic.Id };          // 战术卡排在最前，保证一定在牌里
        foreach (var c in pool)
        {
            if (ids.Count >= want || c == null || c.Type != "unit" || c.Faction != faction) continue;
            for (int i = 0; i < DeckRules.CopyLimit(c.Rarity) && ids.Count < want; i++) ids.Add(c.Id);
        }
        return ids.Count < want ? null : new PlayerDeck("自检·带战术卡", warlord.Id, def.Id, ids);
    }

    /// <summary>驱动一次战术卡的前后快照（造牌/部署那节的断言用）</summary>
    struct TacticBefore
    {
        public int Hand;        // 打出前的手牌张数
        public int OnBoard;     // 打出前自己场上占了几格
    }

    /// <summary>
    /// **定点驱动**：凑一副带指定战术卡的牌组 → 逐种子试到「到手且付得起」→
    /// 用真鼠标把它拖到自己半场打出去 → 等手牌真的变了 → 交给调用方断言 → 截图。
    ///
    /// 为什么要有它：战术卡是随机抽的，`BattleScene` 那条常规驱动路径**碰不到**某一张具体的卡。
    /// 造牌/部署这类效果的验收点又恰恰在「打完那一刻画面跟没跟上」，
    /// 所以必须把那张卡**钉死**再打一次。
    ///
    /// ⚠️ 拖到自己半场就行：不需要选目标的战术卡，`CanPlayTactic` 不要求格位，落哪都算数。
    /// </summary>
    static void DriveTacticAndCheck(BattleDriver driver, CardInteraction it, Camera cam,
                                    List<CardDef> pool, string tacticName,
                                    BoardLayout pBoard, BoardLayout eBoard, string shotName,
                                    System.Action<bool, string> check,
                                    System.Action<BattleContext, TacticBefore> verify)
    {
        CardDef tactic = CardDatabase.Find(pool, tacticName);
        check(tactic != null, $"卡池里有「{tacticName}」");
        if (tactic == null) return;

        var deck = DeckWithTactic(pool, tactic.Faction, tacticName);
        check(deck != null, $"凑出一副带「{tacticName}」的合法卡组（{tactic.Faction}）");
        if (deck == null) return;

        CardTween.Mode = DG.Tweening.UpdateType.Manual;
        bool ready = false;
        int idx = -1;
        BattleContext ctx = null;
        // ⚠️ 牌序随卡组内容变，写死一个 seed 迟早红 —— 逐个试到「到手 + 能量够」
        for (int attempt = 0; attempt < 8 && !ready; attempt++)
        {
            driver.Begin(seed: 20260930 + attempt, myDeck: deck);
            Step(0.3f);
            ctx = driver.Ctx;
            for (int round = 0; round < 14 && !ctx.IsOver; round++)
            {
                idx = HandIdxByName(ctx, tacticName);
                if (idx >= 0 && ctx.Players[0].Energy >= tactic.Cost) { ready = true; break; }
                driver.SimulateEndTurn();
                driver.SimulateAiTurn();
                Step(0.3f);
            }
        }
        check(ready, ready ? $"「{tacticName}」到手且付得起（第 {ctx.Turn} 回合）"
                            : $"「{tacticName}」试了 8 个种子都没到手/付不起");
        if (!ready) return;

        var before = new TacticBefore { Hand = ctx.Players[0].Hand.Count, OnBoard = 0 };
        for (int s = 0; s < BoardSpec.Size; s++)
            if (ctx.Players[0].Board[s] != null) before.OnBoard++;

        // 拖到**自己半场**（不需要目标的战术卡落哪都算数）
        int dropSlot = -1;
        for (int s = 0; s < BoardSpec.Size; s++)
            if (BoardSpec.IsDeployable(s) && ctx.Players[0].Board[s] == null) { dropSlot = s; break; }
        if (dropSlot < 0) dropSlot = BoardSpec.WarlordSlot;      // 满场也行，反正不落格位

        var view = driver.HandViewAt(idx);
        // 🔴 2026-09-20：**这里也必须用 `DropTargetWorld`** —— 上一轮改拖拽目标时漏了这一处
        //    （它变量名是 `dropSlot` 不是 `freeSlot`）⇒ 镜头一改，这几个战术卡用例的落点就落到别处了。
        var dropPos = pBoard.DropTargetWorld(dropSlot);
        it.SimulateHover(view.transform.position);
        Step(0.05f);
        it.SimulatePress(view.transform.position);
        Step(0.2f);
        for (int i = 0; i < 24; i++) { it.SimulateDrag(dropPos, 1f / 30f); Step(1f / 30f); }
        it.SimulateRelease(dropPos);
        // 打出动画走完才触发 `OnDeployed`（引擎调用在回调里）—— 推到「手牌真的变了」
        for (int i = 0; i < 120 && ctx.Players[0].Hand.Count == before.Hand; i++) Step(1f / 30f);
        StepThrough(driver, 2f);

        verify(ctx, before);
        Shot(cam, shotName);
    }

    /// <summary>补间的**真实长度**（秒）。`null` 返回 -1。
    /// ⚠️ 自检用语：DOTween 的 `Join` 接的是当前游标而不是序列开头，写错会把齐序列悄悄拖长 ——
    ///    所以「标的时长」和「真实长度」必须对得上（见第 15 节 ③）。</summary>
    static float Dur(DG.Tweening.Tween t)
    {
        return t == null ? -1f : DG.Tweening.TweenExtensions.Duration(t);
    }

    /// <summary>取**我方**那一台 `HandLayout`。
    ///
    /// 🔴 **别用 `Object.FindObjectOfType&lt;HandLayout&gt;()`** —— 2026-09-17 起对战场景里有**两份**
    /// （我方 `Hand` + 敌方 `FoeHand`），而那个 API 返回哪一份是**任意的**。
    /// **实测后果**：敌方手牌一加进去，六条我方手牌断言一起变红，其中
    /// 「4 张时用原版间距 1.45 不压缩（实测 **0.600** 世界/张）」把马脚露出来了 —— 0.6 是**敌方**的值。
    /// ⇒ 这类「按类型 / 按名字找对象」在这个工程里已经栽过不止一次（另见 `History` 注释里的
    /// `QuestIconPos` 用 `transform.Find`、改 HUD 挂载点那次）。**有引用就用引用。**</summary>
    static HandLayout PlayerHand()
    {
        var drv = Object.FindObjectOfType<BattleDriver>();
        if (drv != null && drv.hand != null) return drv.hand;
        var go = GameObject.Find("Hand");
        return go != null ? go.GetComponent<HandLayout>() : null;
    }

    static int HandIdxByName(BattleContext ctx, string name)    {
        for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
            if (ctx.Players[0].Hand[i].Card.Name == name) return i;
        return -1;
    }

    /// <summary>把**还没跑完的震镜头**收尾（2026-09-19 加）。
    ///
    /// 为什么需要它：`CardFeel.ShakeCamera` 是「**相机 + `HudRoot` 同向等量平移**」——
    /// 位移在**建 tween 的那一刻就已经落到 transform 上**（`attackTime = 0`，见那里的注释），
    /// 靠 tween 跑完/被打断时的 `OnKill` 才复位。而**批处理没有帧循环** ⇒
    /// 一次**由特效触发**的震动（`WFModuleScreenShake.OnShake` → `CardFeel.ShakeCamera`）
    /// 如果在能量绝对世界坐标之前还没推完，`HudRoot` 就**停在偏移位上**，
    /// 于是「量 UI 的世界坐标」那几条断言会**同时**差一个常量。
    /// 🔴 实测（2026-09-19）：接上屏震钩子之后，回放条与换牌气泡两条断言都差 **0.4210** 世界单位
    ///    —— 同一个常量、方向一致，正是这个坑（钩子原来挂在 `Start()`、批处理不走，
    ///    所以这个坑在 2026-09-19 之前**没人踩到过**）。
    /// ⚠️ 真实播放里不需要这一手（DOTween 在 `Update` 里跑，自己会收尾）。</summary>
    static void SettleShake()
    {
        CardFeel.SettleShake();      // 收尾口在 `CardFeel` 那边（工程惯例：别在自检里直接碰 DOTween）
    }

    /// <summary>
    /// 🆕 2026-09-25 自检夹具：往 `slot` 摆一具**合成残骸**（带指定关键词），刷新视图、返回那一格的
    /// `CardView`。**只给自检用** —— 真跑一局时残骸是死亡链（`RuleCore.CleanupDeaths`）造出来的。
    ///
    /// ⚠️ 为什么不能拿场上一张现成的卡改 `IsRemnant`：`BattleDriver.RemnantPrefabOf` 的判据是
    ///    **卡上的关键词**（`Waystone.` → 灵族那具 · `Remnant.` → 死灵那具），
    ///    随便一张兵两个都没有 ⇒ **一具残骸体都盖不出来**（第一版就是这么红的，而且不报错）。
    ///
    /// ⚠️ **不还原棋盘** —— 调用方负责（本处调用点后面紧接着 `driver.Restart()`，那个 `BattleContext`
    ///    马上就要被丢掉）。
    /// </summary>
    static CardView ProbeRemnantView(BattleDriver driver, BattleContext ctx, int slot,
                                     string keyword, string expectPrefab)
    {
        var card = new CardDef($"Probe_{keyword}", $"Probe_{keyword}", "unit", "", null, "Test",
                               1, 0, 1, 0, new[] { keyword });
        ctx.Players[0].Board[slot] = new UnitState(card, false)
        {
            IsRemnant = true, Attack = 0, RangedAttack = 0,
            Health = 1, MaxHealth = 1, Exhausted = true,
        };
        // ⚠️ 白盒改棋盘**不发事件** ⇒ 得自己调一次刷新（正常路径是事件驱动 `RefreshAll`）
        driver.RefreshAll();
        var v = driver.BoardViewAt(slot);
        if (v == null || v.RemnantBodyRoot == null)
            Debug.LogWarning($"[BattleScene] 合成残骸 '{keyword}' 期望盖 '{expectPrefab}'，"
                           + $"但没建出残骸体（视图 {(v == null ? "为 null" : "在")}）");
        return v;
    }

    /// <summary>
    /// 🆕 2026-09-25 数一具残骸体上**绑好材质的层数**与**没绑的层数**（自检用）。
    ///
    /// 🔴 为什么要有它：第一版 `SetRemnantBody` 是裸 `Instantiate`，**没走 `WarpforgeEffectPlayer`**
    ///    ⇒ 也就没跑 `WarpforgeEffectBinder`（原版那批 prefab 的材质是**运行时按名绑**的）
    ///    ⇒ 整具渲成**黑块**，而「残骸体在不在」那几条断言**照样全绿**。实拍才看出来。
    ///    判据：每一层都要有材质、有 shader，且**不能**是 Unity 的报错 shader。
    /// ⚠️ **只数激活的层** —— 原版 prefab 里有一批节点是关着的（如死灵的 `Reanimate Particles`，
    ///    它的材质本来就是 null），关着的层画不出来，算进去就是**假红**
    ///    （2026-09-25 实测：一开这个断言就红了一条，就是它）。
    /// </summary>
    static void RemnantMaterialCount(CardView v, out int bound, out int bad, out string firstBad)
    {
        bound = 0; bad = 0; firstBad = "";
        if (v == null || v.RemnantBodyRoot == null) return;
        var sb = new System.Text.StringBuilder();
        sb.Append("[BattleScene] 残骸体渲染层（只数激活的）：");
        foreach (var r in v.RemnantBodyRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.gameObject.activeInHierarchy) continue;
            var m = r.sharedMaterial;
            sb.Append($"\n  · {r.GetType().Name} '{r.name}' pos={r.transform.position.ToString("F2")} "
                    + $"mat={(m == null ? "<null>" : m.name)} shader={(m == null || m.shader == null ? "<null>" : m.shader.name)} "
                    + $"tex={(m == null || m.mainTexture == null ? "<null>" : m.mainTexture.name)}");
            if (m != null && m.shader != null && !m.shader.name.Contains("InternalErrorShader"))
            {
                bound++;
            }
            else
            {
                bad++;
                if (firstBad.Length == 0)
                    firstBad = $"（头一个：{r.name} → "
                             + (m == null ? "材质为 null" : (m.shader == null ? "shader 为 null" : m.shader.name))
                             + "）";
            }
        }
        Debug.Log(sb.ToString());
    }

    static void Step(float dt)
    {        // 事件时间线也要推 —— 批处理没有帧循环，`BattleDriver.Update` 不会跑。
        // 不推的话事件全卡在队列里，一条特效都不会播（踩过：断言全绿但画面全空）
        var d = Object.FindObjectOfType<BattleDriver>();
        if (d != null) d.AdvanceTimeline(dt);

        CardTween.Advance(dt);
        foreach (var ps in Object.FindObjectsOfType<ParticleSystem>(true))
            if (ps != null) ps.Simulate(dt, withChildren: false, restart: false, fixedTimeStep: true);
    }

    /// <summary>按**英文卡名**从卡池里找一张（自检挑样卡用；找不到返回 null —— 调用方要断「前提」）。</summary>
    static CardDef FindPoolCard(string name)
    {
        var pool = CardDatabase.Load();
        if (pool == null) return null;
        foreach (var c in pool) if (c.Name == name) return c;
        return null;
    }

    /// <summary>一格卡的渲染队列（取卡内所有层里**最小的那个** —— 卡内层序靠 z 偏移、整格一起平移，
    /// 所以最小号就代表这一格；见 `CardFan.SetCardQueue`）。
    /// ⚠️ 读 `sharedMaterial`：`SetCardQueue` 走的是 `.material`（会把实例写回 `sharedMaterial`），
    /// 两边读到的是同一份，且**不会再实例化一次**。</summary>
    static int CardQueue(CardView v)
    {
        if (v == null) return -1;
        int q = int.MaxValue;
        foreach (var mr in v.GetComponentsInChildren<MeshRenderer>(true))
            if (mr.sharedMaterial != null) q = Mathf.Min(q, mr.sharedMaterial.renderQueue);
        return q == int.MaxValue ? -1 : q;
    }

    /// <summary>
    /// 把事件时间线**推到播完**（每段动作有自己的时长，见 `EventTiming`）。
    /// 断言「某个特效播了没有」之前必须走这一步 —— 以前事件是同一帧全播的，`Step(0.3f)` 就够；
    /// 现在排了时间线，得推够。
    /// </summary>
    static void StepThrough(BattleDriver d, float maxSec = 6f)
    {
        float t = 0f;
        const float dt = 1f / 30f;
        while (d != null && d.TimelinePending > 0 && t < maxSec) { Step(dt); t += dt; }
        Step(0.1f);        // 再留一点给刚起来的特效
    }

    /// <summary>**只为数源文件的行数**用的最小形状（`JsonUtility` 只能吃具体类型，
    /// 而 `VoiceLines` 的 `Db` 是私有的）。字段名必须和 json 一致。</summary>
    [System.Serializable] class SrcVoiceDb
    {
        [System.Serializable] public class L { public string ev, file, text; }
        [System.Serializable] public class C { public string id, name, faction; public L[] lines; }
        public C[] cards;
    }
}
