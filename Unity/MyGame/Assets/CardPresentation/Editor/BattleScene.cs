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
    static string ScenePath
    {
        get
        {
            if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("WF_ARENA")))
                return "Assets/CardPresentation/Scenes/Battle.unity";
            return "Assets/CardPresentation/Scenes/Battle_" + ArenaBuilder.ArenaFromEnv() + ".unity";
        }
    }

    /// <summary>本局用哪个原版战场。**判据 = `ArenaByArmy.SceneFor(督军阵营)`**（运行时那张表），
    /// 建场时由 `WF_ARENA` 选（转发 `ArenaBuilder.ArenaFromEnv`）。
    /// 表本体与判据 → `资料/普查产出_0920/场景光照与后处理_原版规格.md` §六。</summary>
    static string BoardArena { get { return ArenaBuilder.ArenaFromEnv(); } }

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
        foreach (var s in list) if (s.path == path) { Debug.Log(P + $"  已在 Build Settings：{System.IO.Path.GetFileName(path)}"); return; }
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log(P + $"  已加进 Build Settings：{System.IO.Path.GetFileName(path)}");
    }

    // ==================================================================
    //  自检
    // ==================================================================

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

        Camera cam = BuildScene(out BattleDriver driver, out BoardLayout pBoard,
                                out BoardLayout eBoard, out CardInteraction it);
        // 批处理下 `AddComponent` **不会**触发 `Awake`（那是 Play 模式的事），
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
            Check(Mathf.Abs(EventTiming.DurationOf(EvtKind.Deploy) - 1.0f) < 1e-3f,
                  "登场 1.0s（原版 `Summon Troop Tween` 的 DelayTween duration=1.0）");

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

            // ② 角标：只有**卡面上带数值**的关键词才画（出处：规则书关键词表的「带数值」列）
            Check(Badges.CarriesValue("Armour 2") && Badges.CarriesValue("Hunt Mark 1"),
                  "带数值的（Armour / Hunt Mark）画角标");
            Check(!Badges.CarriesValue("Flying") && !Badges.CarriesValue("Rally"),
                  "不带数值的（Flying / Rally）不画角标");

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
                Shot(cam, "28a_徽标_四枚");
                Check(probe.SetBadges(null) == 0 && probe.BadgesShown == 0,
                      "★ 清空后一位都不剩（原版每轮重算前也是先全部 Toggle(false)）");
                var kept = probe.BadgeTexture(0);     // 清空只关层、不销毁
                Check(kept != null, "清空只是关掉层，贴图还在（下次复用）");
                Shot(cam, "28b_徽标_清空");
                Object.DestroyImmediate(probe.gameObject);
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
                    Check(flickWant == 0 ? fxs.Length == 0 : fxs.Length == flickAttached,
                          $"★ 闪烁族：本场原版 **{flickWant}** 个 · 我们挂上 **{flickAttached}** 个 · **{flickMissing}** 个因对象没搬而没挂"
                          + $"（建出来的场景里实际 {fxs.Length} 个）{missList}");
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
                        Check(names.Count == 13,
                              $"★ 13 个战场各有一份 `Battle_<场>.unity`（当前不同场景名 {names.Count} 个，目标 13；"
                              + " —— 建法：`WF_ARENA=<场> BattleScene.BuildAndSaveScene`；"
                              + "**没建的会回落 `Battle` 并在日志里出声**（不是静默）");
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
                var cvS = driver.HandViewAt(0);
                var shLayer = cvS != null ? cvS.transform.Find("shadow") : null;
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
            if (drv != null)
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
            }
        }

        // ---- 1f. 四小件（2026-09-13 补）：牌库张数底板 / 手牌数底板 / 本回合已出牌数 / 里程碑骷髅 ----
        // ⚠️ 这四样的毛病**截图都看不出来**：半透明板画成实心、板压根没画、出牌灯该亮不亮、
        //    骷髅摆到名牌外面 —— 画面都「看着挺满」。所以一律按数值断言。
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            if (drv != null)
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
                    Check(true, "⚠️ 这一局我方阵营**没有默认卡背**（`Art/cards/back_<阵营>.png` 只有 4 张）"
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
                it.SimulateRelease(slotPos);
                // 落位动画走完才会触发 `OnDeployed`（引擎调用在回调里）——
                // 推进到「引擎里真有这个单位」为止，别写死一个时长（踩过：0.8s 不够，断言全挂）
                int landSteps = 0;
                int atSlotStep = -1;
                for (int i = 0; i < 90 && ctx.Players[0].Board[freeSlot] == null; i++)
                {
                    Step(1f / 30f);
                    landSteps++;
                    if (atSlotStep < 0 && Vector3.Distance(view.transform.position, slotPos) < 0.02f)
                        atSlotStep = landSteps;      // 卡**到格位**的那一帧（对比「引擎里那一格填上」的那一帧）
                }
                float landSec = landSteps / 30f;
                Step(0.2f);
                Debug.Log(P + $"   落位：卡到格位用了 {atSlotStep / 30f:F2}s，引擎那一格填上用了 {landSec:F2}s"
                            + $"（原版 `minionToConversionPointTime` = {DeploySequence.MoveTime:F2}s，"
                            + "差值 = DOTween 起步那一帧 + Step 粒度）");

                Check(ctx.Players[0].Board[freeSlot] != null,
                      $"拖到槽 {freeSlot} 后引擎里那一格有单位了（{ctx.Players[0].Board[freeSlot]?.Name}）");
                // 落位时长要**跟着原版字段走**：`MinionManager.minionToConversionPointTime = 0.3`
                // ⚠️ 2026-09-13 之前这里是 **0.92s** —— 那是把 `Card Hand To Board`（「2D 卡→3D 身体」的
                //    交接动画）的长度当成了「手牌飞到场位」的时长，认错了来源。
                Check(landSec <= DeploySequence.MoveTime + 0.25f,
                      $"落位 {landSec:F2}s 完成（原版 `minionToConversionPointTime` = {DeploySequence.MoveTime:F2}s；"
                      + $"卡实际在第 {atSlotStep} 帧到位，余量是 DOTween 起步帧 + Step 粒度）");
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
            if (drv != null)
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
                //   ⚠️ 四条原版都是 `Simple + preserveAspect` 且贴图比例≈框比例 ⇒ 按长边定高、宽由贴图比例出。
                float lL = log.FrameWorldH("40k_battlelog_frame_Left") * 108f;
                float lR = log.FrameWorldH("40k_battlelog_frame_Right") * 108f;
                float wT = log.FrameWorldW("40k_battlelog_frame_TOP") * 108f;
                float wB = log.FrameWorldW("40k_battlelog_frame_Bottom") * 108f;
                Check(log != null && Mathf.Abs(lL - 571.24f) < 1.5f && Mathf.Abs(lR - 578.65f) < 1.5f,
                      $"★ 日志**左右竖边框长度不同**（原版 左 571.24 / 右 578.65 —— 不是面板高 654.5）—— 实测 {lL:F1} / {lR:F1}");
                Check(log != null && Mathf.Abs(wT - 707.9f) < 8f && Mathf.Abs(wB - 706.6f) < 8f,
                      $"★ 日志上下**横边框宽 ≈ 707**（原版 707.30 —— 不是面板全宽 794.1）—— 实测 {wT:F1} / {wB:F1}");
                Check(log != null && log.FilledRows > 0, $"日志里有内容（{log.FilledRows} 行有字）");
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
            Check(wait.BarFillPieces >= 1,
                  $"★ 填充层平铺了 {wait.BarFillPieces} 块（原版是 `Tiled`，128 px 一块）");
            Check(Mathf.Abs(wait.ShadeTint.a - 0.6118f) < 0.001f,
                  $"★ 整屏压暗的 α = 原版 0.6118（实得 {wait.ShadeTint.a:F4}）");
            Shot(cam, "02b_等待提示");
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

                // 指针压在按钮上 → 放大 1.3 倍 + 亮黄圈（原版 scaleMultiplierWhenSelected）
                var rp = driver.Selector.ButtonWorld(AttackKind.Ranged);
                Check(rp.HasValue, "拿得到「远程」按钮的世界坐标");
                if (rp.HasValue)
                {
                    var before = driver.Selector.ButtonScale(AttackKind.Ranged);
                    driver.SimulatePointerAt(rp.Value);
                    Check(driver.HoveredCommand == AttackKind.Ranged,
                          $"指针压到「远程」上 → 就是它（{driver.SelectorDescription}）");
                    Step(0.15f);
                    Check(driver.Selector.ButtonScale(AttackKind.Ranged) > before,
                          $"压着的按钮放大了（{before:F2} → {driver.Selector.ButtonScale(AttackKind.Ranged):F2}，原版 1.3 倍）");
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
                  $"拖拽阈值 = 原版的 0.085 屏高（{AttackSelector.DragThresholdWorld:F2} 世界单位）");
            Debug.Log(P + $"   拖出 {AttackSelector.DragThresholdWorld:F2} 世界单位（1080p ≈ "
                        + $"{AttackSelector.DragThreshold01 * 1080f:F0} px）才弹出选择器");
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
            driver.SimulateCommand(AttackKind.Melee);
            Check(!driver.SelectorOpen, "定下打法 → 选择器收起");

            // ① 指针压在**合法**目标上 → 准星出现、压在目标身上、弧线拱起来
            driver.SimulatePointerAt(foeGo.transform.position);
            Check(driver.ReticleVisible, "指针压在合法目标上 → 准星出现");
            Vector3 cp;
            Check(driver.reticle.CrossPosition(out cp) &&
                  Vector3.Distance(new Vector3(cp.x, cp.y, 0f), new Vector3(foeGo.transform.position.x, foeGo.transform.position.y, 0f)) < 0.05f,
                  "准星**压在目标身上**（不是飘在别处）");
            Check(driver.reticle.ArcPointCount == 10,
                  $"弧线 {driver.reticle.ArcPointCount} 段（原版 `curvePoints` = 10）");
            float meleeBulge = driver.reticle.ArcBulge;
            Check(meleeBulge > 0.05f, $"近战的弧线拱起来了（离弦 {meleeBulge:F3} 世界单位）");
            var mc = driver.reticle.CrossColor;
            Check(mc.r > 0.8f && mc.g < 0.2f && mc.b < 0.1f,
                  $"近战 → 准星红（{mc.r:F2},{mc.g:F2},{mc.b:F2}，原版 `colorPresets` attackType=1）");
            // 合法目标的**底光**：原版 `Highlight`（红），锚在卡体那颗**近战**数值格上
            Check(driver.FoeUnits[foeSlot].CurrentGem == TargetGem.Melee,
                  "合法目标 → 卡面**近战**那颗数值格的底光点亮（原版 `Highlight` / `40K_melee_glow`）");
            Shot(cam, "09_选目标_准星");

            // ② 指针挪开 → 收起来。**不能显示「你正指着一个打不了的人」**
            driver.SimulatePointerAt(LayoutSpace.ToWorld(0.02f, 0.06f));
            Check(!driver.ReticleVisible, "指针离开合法目标 → 准星收起（不显示打不了的目标）");

            // ③ 换成远程：准星变紫，弧线**明显比近战平**（原版两条 profile 曲线差一个数量级）
            ctx.Players[0].Board[retAtk] =
                new UnitState(CardByName(StarterCards.Tide(), "Ballista"), false) { Exhausted = false };
            driver.RefreshAll();
            driver.SimulateOpenCommand(retAtk);
            driver.SimulateCommand(AttackKind.Ranged);
            driver.SimulatePointerAt(driver.FoeUnits[foeSlot].transform.position);
            Check(driver.ReticleVisible, "远程也能出准星");
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
            Check(!driver.ReticleVisible, "取消指挥 → 准星收起");

            // 靶场还原
            ctx.Players[0].Board[retAtk] = null;
            for (int t = 0; t < BoardSpec.Size; t++) ctx.Players[1].Board[t] = foeBackup[t];
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
                    Check(sp.ShownTargets == 1,
                          $"面板上的「可选目标数」= 合法目标数（{sp.ShownTargets}）");
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
                float bigH = cdw.Card == null ? 0f
                           : CardView.Height * cdw.Card.transform.localScale.y * 108f;
                Check(Mathf.Abs(bigH - CardDisplayWindow.CardHeightPx) < 2f,
                      $"放大卡高 {bigH:F0} px = 原版的 {CardDisplayWindow.CardHeightPx:F0} px");
                Shot(cam, "11_卡牌展示窗");

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
                        c2.Signals.Add(new BattleEvent { Kind = EvtKind.Death, Player = 0, Slot = free[0] });
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
                        c2.Signals.Add(new BattleEvent { Kind = EvtKind.Death, Player = 0, Slot = free[0] });
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
                int handExpect = startHandSk + 1 + conjSk + (meFirst ? 0 : 1);
                Check(handSk == handExpect,
                      $"遭遇：手牌 = 起手 {startHandSk} + 首回合抽 1"
                    + (conjSk > 0 ? $" + 天赋生成 {conjSk}" : "")
                    + (meFirst ? "（我方**先手** ⇒ **没有防御卡**）" : " + **防御卡 1**（我方是**后手**）")
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
            if (drv != null)
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
            if (drv != null)
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
            if (drv != null)
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

                // ---- 14c. 三根音量滑块（🆕 2026-09-19；原版 `BattleSettingsWindow` 的 music/SoundFX/voiceOver）----
                // 版面来源：解包逐级解父链（滑块中心 (∓2.00, 120.07/3.44/−113.18) px，轨道 561.08×14）
                // 数值来源：`AudioMixer.SetFloat("Volume"+组名, dB)`，见 `Core/WarpforgeAudio.cs`。
                {
                    Check(sp.SliderCount == 3, "设置面板上有三根音量滑块");
                    for (int i = 0; i < sp.SliderCount; i++)
                        Check(sp.SliderAt(i) != null && sp.SliderAt(i).HasArt,
                              $"第 {i + 1} 根滑块的三张图都取到了（轨道 / 填充 / 手柄）");
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
                    Check(s0 != null && s0.Contains(s0.HandleWorldPos),
                          "★ 指针落在音乐滑块的手柄上 → 命中判定打得中");
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
            if (drv != null)
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
                    var restAtk = pBoard.SlotPosition(probe);
                    Check(atkView != null, "攻击者的视图在场上");
                    Check(drv.SimulateOpenCommand(probe), "点自己的单位 → 攻击方式选择器弹出");
                    drv.SimulateCommand(AttackKind.Melee);
                    Check(drv.SimulateResolve(victim) == RuleCodes.OK, $"朝槽 {victim} 打出去");
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

                    AdvanceTo(hitAt - 0.05f);                // 出手事件已发、**位移正走到一半**
                    Debug.Log(P + $"   [probe] t={drv.Clock:F3} 待播 {drv.TimelinePending} "
                                + $"对面槽{victim}视图 {(drv.FoeUnits.ContainsKey(victim) ? "在" : "没了")} "
                                + $"消散中 {drv.DyingCount}");
                    float moved = Vector3.Distance(atkView.transform.position, restAtk);
                    Check(moved > 0.02f, $"② 攻击位移：攻击者**离开了静止位** {moved:F3} 世界单位（前冲在动）");
                    Shot(cam, "20_出手");

                    AdvanceTo(hitAt + 0.03f);               // **刚过命中那一刻**
                    var vicView = drv.FoeUnits.ContainsKey(victim) ? drv.FoeUnits[victim] : null;
                    Debug.Log(P + $"   攻击者 {atkView.name}@槽{probe}　受击者 {(vicView == null ? "无" : vicView.name)}@槽{victim}"
                                + $"　对面场上：{drv.BoardViewNames(false)}");
                    Debug.Log(P + $"   命中帧：攻击者离位 {Vector3.Distance(atkView.transform.position, restAtk):F3}"
                                + $"（角 {Mathf.DeltaAngle(atkView.transform.eulerAngles.z, 0f):F2}°）"
                                + $"　受击者离位 {(vicView == null ? -1f : Vector3.Distance(vicView.transform.position, eBoard.SlotPosition(victim))):F3}"
                                + $"（角 {(vicView == null ? 0f : Mathf.DeltaAngle(vicView.transform.eulerAngles.z, 0f)):F2}°）");
                    Check(vicView != null, "③ 受击者的视图还在（阵亡是排在时间线上的，这一刻还没轮到）");
                    if (vicView != null)
                    {
                        var restVic = eBoard.SlotPosition(victim);
                        float mv = Vector3.Distance(vicView.transform.position, restVic);
                        Check(mv > 0.02f, $"③ 命中抖动：受击者被弹开了 {mv:F3} 世界单位");
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
                        var slotPos = eBoard.SlotPosition(victim);
                        float dPop = Vector3.Distance(drv.LastPop.transform.position, slotPos);
                        Check(dPop < 0.9f && drv.LastPop.transform.position.y > slotPos.y,
                              $"……而且飘在**挨打那张卡上**（离格位中心 {dPop:F3} 世界单位、在上方）");
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
                    Check(dying != null && dying.Alpha < 1f,
                          $"……而且它**正在变淡**（alpha {(dying == null ? -1f : dying.Alpha):F2}，不是「啪」一下没了）");
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
                At("EnergyAccumulation_Me", 1785.6f, 555.65f);
                At("OvertimeIndicator", 1753.2f, 377.0f);       // x[1718.9,1787.5] y[341.5,412.5]

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
                Check(!string.IsNullOrEmpty(mp.PromptText), $"提示行写着「{mp.PromptText}」");
                // 🆕 2026-09-26：**「你先手 / 你后手」那一行**（原版 `MulliganText/TurnText`）。
                //   🔴 **它是原版唯一一处「先手/后手」的表现** —— 原版没有硬币资产/动画/音效（判据 → §2.8）。
                Check(mp.TurnText == (drv.Ctx.FirstSeat == 0 ? MulliganPanel.TurnFirst : MulliganPanel.TurnSecond),
                      $"★ 换牌面板那行「你先手 / 你后手」跟**这一局谁先手**对得上：「{mp.TurnText}」"
                    + $"（先手 = {drv.Ctx.Players[drv.Ctx.FirstSeat].Name}）");
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
        //  🆕 2026-09-26（N4）：**联机客机的视图方向**（`_me = 1`）
        //
        //  为什么单开一段：联机对局两端跑的是**同一套绝对座位**（主机 0 / 客机 1），
        //  客机那边靠 `SetMySeat(1)` 把视图翻过来 —— 而这一路**批处理里只能这样验**
        //  （同一工程不能同时跑两个 Unity 实例，两台机器真连一次是「真 Play」的事）。
        //  验的是：**自己那侧的东西必须画在屏幕下半**（手牌 / 上场单位），对面那侧在上半。
        // ==================================================================
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
                    // 名牌那一行是 `阵营 + 生命 + 数值`（**文案本地化** ⇒ 别按 "HP" 去匹配，按数字判方向）
                    Check(driver.MyPlateText != null && driver.MyPlateText.Contains("27")
                          && driver.FoePlateText != null && driver.FoePlateText.Contains("12"),
                          $"★ 客机视角：**两块名牌认的也是 `_me` 那一方**（我 `{driver.MyPlateText}` / 敌 `{driver.FoePlateText}`）");
                    driver.SetMySeat(0); driver.RefreshAll();
                    Check(driver.MyPlateText != null && driver.MyPlateText.Contains("12")
                          && driver.FoePlateText != null && driver.FoePlateText.Contains("27"),
                          $"（对照）主机视角：同一个局面下两块名牌**反过来了**（我 `{driver.MyPlateText}` / 敌 `{driver.FoePlateText}`）");
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
            }

            // ② ★★ **正面：放刚打完的那一局**（`BeginFromPendingCore` 重建 + 全量灌动作，最后比指纹）
            var playRec = string.IsNullOrEmpty(playable) ? null : ReplayStore.Load(playable);
            Check(playRec != null, $"刚打完那一局的录像读得回来（`{playable}`）");
            if (playRec != null)
            {
                Debug.Log(P + $"   【放】刚打完那局：{playRec.actions.Count} 条动作 · 座位 {playRec.mySeat}");
                bool same = driver.PlayReplay(playRec);
                Check(same, "★★ **回放演完，指纹与录制时【相等】⇒ 演的是同一局**"
                          + "（这是「录全了没有」唯一的尺子 —— 不等就说明有动作没录到）");
                Check(driver.Ctx != null && driver.Ctx.IsOver, "回放演到终局（这一局演完了）");
                Check(playRec.trace != null && playRec.trace.Count == playRec.actions.Count,
                      $"逐条轨迹与动作**一一对应**（{playRec.trace.Count} 条）—— 少一条就少对一个分叉点");
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
        var arena = GameObject.Find("Arena3D");
        int arenaRend = arena != null ? arena.GetComponentsInChildren<Renderer>(true).Length : 0;
        Check(arenaRend > 10, arena == null
              ? "存档里没有 Arena3D（按「退回烘图」那一支验）"
              : $"存档里有 Arena3D（可渲染件 {arenaRend} 个，跟着场景一起存下来了）");

        Camera bcam = null, hcam = null;
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (c.depth < 0) bcam = c; else if (hcam == null) hcam = c;
        }
        Check((bcam != null) == (arenaRend > 10),
              "3D 场地与那台透视相机**成对**存在（有场地没相机 / 有相机没场地都是错的）");
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
            Light dl = null;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
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
                Check(arena != null && dl.transform.IsChildOf(arena.transform),
                      "灯挂在 `Arena3D` 下（跟着战场一起走，不留在场景根）");
            }

            Check(RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Flat,
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
                var c = RenderSettings.ambientLight;
                Check(Mathf.Abs(c.r - E[0]) < 0.004f && Mathf.Abs(c.g - E[1]) < 0.004f
                      && Mathf.Abs(c.b - E[2]) < 0.004f,
                      $"★ 环境光色 = 默认环境 SO 的 `ambientColor` ({E[0]:F5},{E[1]:F5},{E[2]:F5})"
                      + $" —— 来自 `{mfEnv.defaultEnv.so}`（**运行时真值**；场景里的 `m_AmbientSkyColor` 是"
                      + $" ({mfEnv.ambient.sky[0]:F4},{mfEnv.ambient.sky[1]:F4},{mfEnv.ambient.sky[2]:F4})，"
                      + "原版运行时会把它覆盖掉）");

                // 「值真的到了渲染器」：URP 读的是 `RenderSettings.ambientProbe`（SH），不是 `ambientLight` 本身。
                // 实况实测（原版 arena3）probe 的 DC = **sRGB→linear** 之后的颜色 ——
                // (0.8066,0.9522,1.0) → 实测 (0.61508,0.89479,1.0)，逐位验算吻合；arena1 (1,1,1)→(1,1,1)。
                var sh = RenderSettings.ambientProbe;
                var lin = new Vector3(Mathf.GammaToLinearSpace(c.r), Mathf.GammaToLinearSpace(c.g),
                                      Mathf.GammaToLinearSpace(c.b));
                Check(Mathf.Abs(sh[0, 0] - lin.x) < 0.02f && Mathf.Abs(sh[1, 0] - lin.y) < 0.02f
                      && Mathf.Abs(sh[2, 0] - lin.z) < 0.02f,
                      $"★ 环境光**真的到了渲染器**：`ambientProbe` DC = ({sh[0, 0]:F4},{sh[1, 0]:F4},{sh[2, 0]:F4})"
                      + $" = sRGB→linear(环境光色) = ({lin.x:F4},{lin.y:F4},{lin.z:F4})");
            }
            else Check(false, "★ 清单里有 `defaultEnv`（原版运行时环境光的真源）—— 缺了就只能退回场景值");

            Check(!RenderSettings.fog, "雾关着（原版 13 场的默认环境 `fogDensity` 都是 0）");

            // 🔴 反例：战场上**不能有「材质没贴图」的粒子** —— 那会渲成**不透明白方块**。
            //    踩过：blacklegion 的 4 个 `Heat Distortion`（扭曲类，原版材质本来就没有贴图）被我们
            //    建成了 `URP/Particles/Unlit` + 空贴图 ⇒ 画面中间横着 4 条白板；和原版真渲图一比就露。
            //    现在的做法是**不建 + 报警**（`ArenaBuilder.BuildContent`），这条断言盯着它别再回来。
            int whitePs = 0;
            if (arena != null)
            {
                foreach (var r in arena.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
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
            if (arena != null && mfEnv != null && mfEnv.particles != null)
            {
                var built = new System.Collections.Generic.List<ParticleSystem>();
                foreach (var p in arena.GetComponentsInChildren<ParticleSystem>(true))
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
                    if (!pe.active || pe.renderMode == 5 || string.IsNullOrEmpty(pe.texFile)) continue;
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
                foreach (var c in arena.GetComponentsInChildren<WarpforgeVFX.ArenaParticleIndex>(true))
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
        // · Vignette 黑 / 中心 (0.5,0.5) / 强度 0.297。ColorLookup 实测是 **identity**（不接）。
        {
            var mfP = ArenaBuilder.LoadManifest(BoardArena);
            Volume gv = null;
            foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
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
                    else if (c.type == "Vignette")
                    {
                        Vignette v;
                        bool ok = gv.sharedProfile.TryGet(out v) && v != null && v.active;
                        Check(ok && Mathf.Abs(v.intensity.value - c.intensity) < 0.001f
                              && Mathf.Abs(v.center.value.x - c.center[0]) < 0.001f,
                              $"★ Vignette = 清单值 强度 {c.intensity:F3} / 中心 ({c.center[0]:F1},{c.center[1]:F1})"
                              + "（原版就是拿它压四角，我们原来一点都没接）");
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
        if (arenaRend <= 10)
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
        if (bd != null && bd.transform.Find("Backdrop") != null && arenaRend > 10)
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
        const float K = 0.5f;                      // DAT_1834b2bb4 —— 取 [minY,maxY] 的**中点**
        const float RefH = 1080f;
        const float PlayerHelperTopPx = 249.9f;    // 我方 helper 上沿（pivot 在下 ⇒ 往上长）
        const float EnemyHelperBottomPx = 1080f - 96.7f;   // 敌方 helper 下沿（pivot 在上 ⇒ 往下长）

        static AnimationCurve Curve(float[] t, float[] v, float[] ins, float[] outs)
        {
            var keys = new Keyframe[t.Length];
            for (int i = 0; i < t.Length; i++)
                keys[i] = new Keyframe(t[i], v[i], ins[i], outs[i]);
            return new AnimationCurve(keys);
        }

        // viewShiftModifier —— **输出就是 `lensShift.y`**（7 帧，实读）
        static readonly AnimationCurve ViewShift = Curve(
            new[] { 0.3450f, 0.38875f, 0.4170f, 0.5000f, 0.5710f, 0.6514f, 0.7067f },
            new[] { -0.09650f, -0.15226f, -0.16415f, -0.22012f, -0.25977f, -0.33297f, -0.42161f },
            new[] { 0f, -1.274542f, -0.420809f, -0.972000f, -0.593500f, -0.997100f, -1.129900f },
            new[] { -1.274542f, -0.420809f, -0.972000f, -0.593500f, -0.997100f, -1.129900f, 0f });

        // verticalPaddingByZoom：0.0030534→−0.0320131 · 1.0→−0.1710815
        static readonly AnimationCurve PadByZoom = Curve(
            new[] { 0.0030534f, 1.0f }, new[] { -0.0320131f, -0.1710815f },
            new[] { -0.1394944f, -0.1394944f }, new[] { -0.1394944f, -0.1394944f });

        // verticalPaddingModifierByAspectRatio：≤1.77 恒 1.0；2.333→0.6519；2.44→0.6508
        static readonly AnimationCurve PadModByAspect = Curve(
            new[] { 1.333f, 1.6f, 1.77f, 2.333f, 2.44f },
            new[] { 1.0f, 1.0f, 1.0f, 0.6519f, 0.6508f },
            new[] { 0.027326f, 0f, 0f, 0.051700f, -0.009200f },
            new[] { 0f, 0f, 0.051700f, -0.009200f, -0.009200f });

        /// <summary>这个宽高比下原版会用的 `lensShift.y`。</summary>
        public static float LensShiftY(float aspect)
        {
            float minY = PlayerHelperTopPx / RefH;
            float maxY = EnemyHelperBottomPx / RefH + PadModByAspect.Evaluate(aspect) * PadByZoom.Evaluate(Zoom);
            return ViewShift.Evaluate((maxY + minY) * K);
        }
    }

    /// <summary>3D 战场相机的 `lensShift.y`（历史注释留在下面，现在的真值来自 `BoardFramer`）。
    /// 🔴 **公开**：独立战场场景（`ArenaBuilder.BuildSceneTail`）也用这一份 ——
    /// 两台相机必须是**同一套取景判据**，不然预览图和战斗画面不一样（2026-09-20 踩过：
    /// 独立场景那台**没开物理相机** ⇒ `lensShift` 被忽略 ⇒ 预览比原版**亮 42%**、多出半屏天空）。</summary>
    public static float BoardLensShiftY() { return BoardFramer.LensShiftY(16f / 9f); }

    /// <summary>把原版战场（网格 + 粒子）建到 `root` 下，返回**可渲染件数**（0 = 建不出来，调用方要兜底）。
    /// 参数与做法**全在 `ArenaBuilder.BuildContent`**（与独立场景模式共用同一段，判据只留一处）。</summary>
    static int BuildArena3D(GameObject root)
    {
        var mf = ArenaBuilder.LoadManifest(BoardArena);
        if (mf == null) return 0;
        var go = ArenaBuilder.BuildContent(root.transform, mf);
        int n = go.GetComponentsInChildren<Renderer>(true).Length;
        Debug.Log(P + $"   3D 战场：清单 网格 {mf.meshes?.Length ?? 0} 条 / 粒子 {mf.particles?.Length ?? 0} 条，"
                    + $"实际可渲染件 {n} 个");
        return n;
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
            SetLayerRecursive(arenaGo, ArenaLayer);
            // 🆕 2026-09-20：把后处理那台 Volume 还原到 **Default 层**。
            // 原版的 Volume 在 Default 层，而原版 BoardCamera 的 `m_VolumeLayerMask = 1`（只有 Default 层）
            // ⇒ 上面那行会把 Arena3D **整棵树**刷成 `ArenaLayer`，不还原的话相机够不着那个 Volume，
            //    **后处理会静默失效**（不报错、只是没效果 —— 正是本项目最怕的那类坑）。
            foreach (var v in arenaGo.GetComponentsInChildren<Volume>(true)) v.gameObject.layer = 0;
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
