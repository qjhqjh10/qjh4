// B117_G10_25_Assert.cs — 执行代理 `B117-G10-25`（战斗域第 25 部分：**相机 · 转场 · 演出**）的断言
//
// ============================ 它盯的是什么 ============================
//   ① `Battle/CameraIntro.cs` —— 13 场开场运镜的 **曲线保真**（端点由 `BattleScene` 的 T12 那组钉过了，
//      本文件补的是**端点之间那段形状**：切线 / 插值 / 中段值）。
//   ② `Battle/CombatAutoZoom.cs` —— `PlayerHand.get_CurrentSizeMultiplier()` 的那一格 `M`（原版 1.365 / 1.0 两档）
//      + `unitsZoomCurve` 三档 + 取景那两处夹取。
//   ③ `Core/AutoCardRotation.cs` —— **两次 `GetRotation` 的相乘顺序**（原版是 x 先、y 后，⛔ 不能交换）。
//   ④ `Battle/CombatCameraZoom.cs` —— 序列化常量取的是**场景值**（不是 ctor 出厂值）那三格。
//
// 判据（⛔ 别在这里抄第二份）→ `Unity/资料/完全复刻/判据/B117-G10-25_相机转场与演出.md`
//   · `d:/2/tools/decomp_full/{AutoCardRotation__*, CombatAutoZoom__*, CameraVerticalFramer__CalculateFraming,
//     PlayerHand__get_CurrentSizeMultiplier}.c`（方法体，第一权威）
//   · `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena*/MonoBehaviour/*.json`（序列化值，13/13 逐场）
//   · 开场运镜的曲线值 = 上面那些 bundle 里的 `AnimationClip/*.json`（生成器的 `--check` 也逐字节核过）。
//   ⚠️ **本文件里的期望值全部手写成原版字面量**（⛔ 不从被测实现里读期望值 —— 那就是自证）。
//
// ============================ 怎么用（宿主）============================
//   `B117G1025Assert.Run();`
//   + 宿主里两行：`_sink.Pass += B117G1025Assert.Pass; _sink.Fail += B117G1025Assert.Fail;`
//   · 推荐宿主 = `Editor/BattleScene.cs`（`BattleScene.Run`）。**本文件不依赖场景设施**（不需要画布 / 相机已经 Apply），
//     只建自己的临时 GameObject，`finally` 里全 `DestroyImmediate`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A4 / A5 是一对**方向相反**的：
//       A4 = `battlearenaemperorschildren` 的 position 首键 `outSlope` **就是割线**（原版两个键一个 ease-out）；
//            把它改成 0 切线（`new Keyframe(t,v)`）⇒ **A4 红**（6.271875 → 7.52625）。
//       A5 = `battlearena1` 的 position 两个键切线**都是 0**（smoothstep）；
//            给它补上割线 ⇒ **A5 红**（−6.024375 → −5.0206）。
//       ⇒ 「把切线整批丢掉」和「把切线整批改成割线」**各红一条**，两条都躲不过。
//   · A8：把 `BuildClip` 里 `flareIntensityMultiplier` 那条 `SetCurve` 删掉 ⇒ 7 场 `HasFlareCurve` 全变 false ⇒ 红。
//   · B3：把 `CurrentSizeMultiplier` 改回恒 `1f` ⇒ 红；改成恒 `1.365f` ⇒ 红（**两档都要对**）。
//   · B5：把 `unitsZoomCurve` 拍平（全 0）⇒ 红。
//   · C2：把两次 `GetRotation` 的相乘顺序对调 ⇒ 红（`qX*qY ≠ qY*qX`）。
//   · C3：把 `Vector3.down` 写成 `Vector3.up` ⇒ 红（y 分量符号反）。
//   · D2：把 `zoomSpeed` / `invertMouseScrollWheel` / `dragSensitivity` 改回 **ctor 出厂值**
//          （0.1 / false / 0.005）⇒ 红 —— 这三格正是「一个值 ≠ 全部情况」踩过的那三处。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B117-G10-25`：开场运镜曲线保真 / 自动缩放的人数倍数与曲线 / 整卡倾摆的乘法顺序 /
/// 相机那三格「场景值 vs ctor 出厂值」。计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B117G1025Assert
{
    static readonly CheckSink _sink = new CheckSink("[G10/25] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    /// <summary>浮点近似（本文件统一 1e-3；切线差的最小判别量是 1.0 量级，1e-3 足够严）。</summary>
    static bool Near(float a, float b, float tol = 1e-3f) { return Mathf.Abs(a - b) <= tol; }

    /// <summary>本文件的临时对象一律即建即毁（⛔ 不在场景里留残骸；批处理 <c>Destroy</c> 不生效 ⇒ 走 `DestroyImmediate`）。</summary>
    static void Kill(GameObject go)
    {
        if (go == null) return;
        if (Application.isPlaying) Object.Destroy(go); else Object.DestroyImmediate(go);
    }

    public static void Run()
    {
        MenuCheck.Section(_sink, "G10-25 相机·转场·演出（开场运镜曲线 · 自动缩放的 M 与曲线 · 整卡倾摆顺序 · 相机场景值）");

        SectionA_开场运镜曲线();
        SectionB_自动缩放();
        SectionC_整卡倾摆();
        SectionD_相机序列化常量();
    }

    // ============================================================ A：开场运镜（`CameraIntro`）
    //
    // 原版判据：13 条 `<场名> Camera Intro` legacy clip（`m_SampleRate 60` / `m_WrapMode 1` = Once）。
    // 端点由 `BattleScene.Run` 的 T12 那组断言钉住了（起点位姿 / 终点本位 / 长度 / legacy 标志）——
    // **本节的独有价值是「端点之间那段形状」**：原版位置曲线**逐场不同**（有的 linear-ish、有的 smoothstep），
    // 只有把 `inSlope/outSlope` 逐键写回才复现得出来；端点断言对这个**完全看不见**。
    static readonly string[] IntroKeys =
    {
        "battlearena1", "battlearena2", "battlearena3", "battlearenaaeldari", "battlearenaastramilitarum",
        "battlearenablacklegion", "battlearenadarkangels", "battlearenaemperorschildren", "battlearenagenestealers",
        "battlearenaleviathan", "battlearenasororitas", "battlearenaspacewolves", "battlearenatauviorla",
    };

    /// <summary>原版有 `flareIntensityMultiplier` 那条 float 曲线的 **7 场**（其余 6 场没有）。</summary>
    static readonly string[] IntroFlareKeys =
    {
        "battlearena1", "battlearena2", "battlearenaaeldari", "battlearenaastramilitarum",
        "battlearenadarkangels", "battlearenaleviathan", "battlearenatauviorla",
    };

    /// <summary>本位 = 战场相机在自己父节点下的局部位姿（13 场同一个；= 原版 `vcam` 相对 `+x100` 父节点的局部值）。</summary>
    static readonly Vector3 IntroHome = new Vector3(0f, 2.222074508666992f, -13.571979522705078f);

    static void SectionA_开场运镜曲线()
    {
        MenuCheck.Section(_sink, "A · 开场运镜：13 场曲线保真（重点 = 端点之间的形状）");

        Tr(CameraIntro.Loaded && CameraIntro.ClipCount == 13,
           $"A0 前提：运镜表载进来了、13 场（实得 {CameraIntro.ClipCount}"
         + (CameraIntro.LastError != null ? " · " + CameraIntro.LastError : "") + "）");

        int miss = 0;
        foreach (var k in IntroKeys) if (!CameraIntro.HasArena(k)) miss++;
        Tr(miss == 0, $"A1 13 个场键都在表里（缺 {miss} 个）");

        var go = new GameObject("G1025_IntroProbe");
        Camera cam = null;
        CameraIntro intro = null;
        try
        {
            cam = go.AddComponent<Camera>();
            cam.enabled = false;                      // 只借 transform 采样，⛔ 别让它参与渲染/截图
            intro = go.AddComponent<CameraIntro>();

            // ---- A2：13 场都能起播，且**各自的 t=0 起点都离本位 >1 单位**（= 表里真的逐场有数据，
            //          不是 13 个场键共用一个占位）----
            int startBad = 0;
            foreach (var k in IntroKeys)
            {
                cam.transform.localPosition = IntroHome;
                cam.transform.localRotation = Quaternion.identity;
                if (!intro.StartForTest(k, cam, null)) { startBad++; continue; }
                if (intro.ClipLength < 2f) startBad++;
                if (Vector3.Distance(intro.PosePositionAtForTest(0f), IntroHome) <= 1f) startBad++;
                if (Vector3.Distance(intro.PosePositionAtForTest(intro.ClipLength), IntroHome) > 1e-2f) startBad++;
            }
            Tr(startBad == 0,
               $"A2 13 场都能按场键起播：**起点离本位 >1 单位、终点回到本位、长度 ≥2 s**（不合格 {startBad} 场）");

            // ---- A4：EC 的 position 首键 `outSlope` = 割线（原版两个键，**两端切线不对称**）----
            // 原版数据（`bundle_scenes_scenes_battlearenaemperorschildren/AnimationClip`）：
            //   t=0   (8.92, 1.56, 0.24)  outSlope = (−3.568, 0.2648298, −5.524792)  ← **= (末−首)/2.5**
            //   t=2.5 (0, 2.2220745, −13.57198)  inSlope = (0, 0, 0)
            // 在 u = 0.25（t = 0.625）处 Hermite 求值：
            //   h00=0.84375  h10=0.140625  h01=0.15625  →  x = 0.84375·8.92 + 0.140625·(−8.92) = **6.271875**
            //   ⛔ 若把切线丢掉（`new Keyframe(t,v)`）：x = 0.84375·8.92 = **7.52625**（差 1.25，一眼分得开）
            float xEC, xA1;
            {
                bool ok = intro.StartForTest("battlearenaemperorschildren", cam, null);
                float x = ok ? intro.PosePositionAtForTest(0.625f).x : float.NaN;
                xEC = x;
                Tr(ok && Near(x, 6.271875f, 1e-2f),
                   $"A4 `battlearenaemperorschildren` 位置曲线的**首键切线 = 割线**被逐键写回了"
                 + $"（t=0.625 处 x 应 = 6.271875，实得 {x:F6}）"
                 + "｜🧨 丢掉切线 ⇒ 7.52625，本条红");
            }

            // ---- A5：arena1 的 position 两个键切线**都是 0**（= smoothstep）----
            // 原版数据：t=0 (−7.14, 1.61, 1.07) si/so 全 0；t=2.5 (0, 2.2220745, −13.57198) si/so 全 0
            //   u = 0.25 ⇒ x = 0.84375·(−7.14) + 0.15625·0 = **−6.024375**
            //   ⛔ 若擅自补上割线：x = **−5.0206**（差 1.0）
            {
                bool ok = intro.StartForTest("battlearena1", cam, null);
                float x = ok ? intro.PosePositionAtForTest(0.625f).x : float.NaN;
                xA1 = x;
                Tr(ok && Near(x, -6.024375f, 1e-2f),
                   $"A5 `battlearena1` 位置曲线的**两个键切线都是 0**（缓入缓出）照原版保留（t=0.625 处 x 应 = −6.024375，实得 {x:F6}）"
                 + "｜🧨 擅自补割线 ⇒ −5.0206，本条红");
            }

            // ---- A6：**A4 与 A5 必须不同** —— 两条曲线在原版里就是两种形状 ----
            // 灭自证的第二重锁：若有人把「所有场都按同一套切线」重建（无论哪一套），这两个数会**同时**对上同一个值。
            Tr(!float.IsNaN(xEC) && !float.IsNaN(xA1) && Mathf.Abs(xEC - xA1) > 1f,
               $"A6（灭自证）EC 与 arena1 在**同一归一化时刻**的实测位移差 {Mathf.Abs(xEC - xA1):F3} > 1"
             + " ⇒ A4/A5 不可能被同一套「全场统一切线」同时满足");

            // ---- A7：起播把相机摆到 clip 的 t=0（不是本位）----
            {
                bool ok = intro.StartForTest("battlearenablacklegion", cam, null);
                var p0 = ok ? intro.PosePositionAtForTest(0f) : Vector3.zero;
                var pEnd = ok ? intro.PosePositionAtForTest(2.5f) : Vector3.zero;
                Tr(ok && Near(Vector3.Distance(p0, new Vector3(3.06f, 2.64f, 9.86f)), 0f, 1e-2f)
                      && Near(Vector3.Distance(pEnd, IntroHome), 0f, 1e-2f),
                   $"A7 blacklegion 的 t=0 位姿 = 原版首键 `(3.06, 2.64, 9.86)`、t=2.5 = 本位"
                 + $"（实得 {p0} → {pEnd}）");
            }

            // ---- A8：光晕那条 float 曲线只在这 7 场存在（其余 6 场原版就没有）----
            int flareBad = 0;
            foreach (var k in IntroKeys)
            {
                if (!intro.StartForTest(k, cam, null)) { flareBad++; continue; }
                bool want = System.Array.IndexOf(IntroFlareKeys, k) >= 0;
                if (intro.HasFlareCurve != want) flareBad++;
            }
            Tr(flareBad == 0,
               $"A8 `flareIntensityMultiplier` 曲线**只在原版那 7 场**存在（其余 6 场没有；不合格 {flareBad} 场）"
             + "｜🧨 删掉 `BuildClip` 里那条 `SetCurve` ⇒ 13 场全变 false ⇒ 红");
        }
        finally
        {
            Kill(go);
        }
    }

    // ============================================================ B：自动缩放（`CombatAutoZoom`）
    static void SectionB_自动缩放()
    {
        MenuCheck.Section(_sink, "B · 自动缩放：`PlayerHand.get_CurrentSizeMultiplier()` 的 M · `unitsZoomCurve` 三档 · 取景夹取");

        // ---- B1：原版 `PlayerHand.smallScreenSizeMultiplier`（字段 `+0x3c`）= 1.365（13/13 场）----
        Tr(Near(CombatAutoZoom.PlayerHandSmallScreenSizeMultiplier, 1.365f, 1e-6f),
           $"B1 原版 `PlayerHand+0x3c smallScreenSizeMultiplier` = **1.365**（13/13 场逐场亲读；实得 "
         + $"{CombatAutoZoom.PlayerHandSmallScreenSizeMultiplier:F6}）");

        // ---- B2 / B3：两档都要对（`smallScreenUI` 假 ⇒ 1.0；真 ⇒ 1.365）----
        // ⚠️ 动全局开关 ⇒ 同本仓自检纪律：`PersistOverride = true`（不写 `PlayerPrefs`）+ `finally` 还原内存态。
        bool wasOn = SmallScreenUI.Enabled;
        bool wasPersist = SmallScreenUI.PersistOverride;
        SmallScreenUI.PersistOverride = true;
        try
        {
            SmallScreenUI.Set(false);
            float off = CombatAutoZoom.CurrentSizeMultiplier;
            Tr(Near(off, 1f, 1e-6f),
               $"B2 `smallScreenUI` **关** ⇒ M = 原版常量 `DAT_1834b2bb8` = **1.0**（实得 {off:F6}）");

            SmallScreenUI.Set(true);
            float on = CombatAutoZoom.CurrentSizeMultiplier;
            Tr(Near(on, 1.365f, 1e-6f),
               $"B3 `smallScreenUI` **开** ⇒ M = `PlayerHand+0x3c` = **1.365**（实得 {on:F6}）"
             + "｜🧨 写回恒 1.0 ⇒ 本条红");

            // ---- B4：两档**真的分得开**（灭自证：恒 1.0 / 恒 1.365 都不可能同时过 B2+B3+B4）----
            Tr(Mathf.Abs(on - off) > 0.3f,
               $"B4（灭自证）两档差 {Mathf.Abs(on - off):F3} > 0.3 ⇒ 「两档都读同一个常量」不可能同时过 B2/B3");
        }
        finally
        {
            SmallScreenUI.Set(wasOn);
            SmallScreenUI.PersistOverride = wasPersist;
            // ⚠️ 如实记：`ChosenManually` 这一格没有写回口（`SmallScreenUI.Set` 恒置 1），
            //    残留与 `BattleScene.Run` 收尾那一档同源（见那边 46,497 的注）—— 不落盘、只影响同进程内的读。
        }

        // ---- B5：`unitsZoomCurve` 三个判据档（原版 5 个键：0→0、3→0.105257757、4→1）----
        float z2 = CombatAutoZoom.EvaluateUnitsZoom(2f);
        float z3 = CombatAutoZoom.EvaluateUnitsZoom(3f);
        float z4 = CombatAutoZoom.EvaluateUnitsZoom(4f);
        Tr(Near(z2, 0f) && Near(z3, 0.10525775700807571f, 1e-6f) && Near(z4, 1f),
           $"B5 人数→zoom 三档 = 原版 `unitsZoomCurve`：2 → **0**、3 → **0.105257757**、4 → **1**"
         + $"（实得 {z2:F6} / {z3:F6} / {z4:F6}）｜🧨 曲线拍平 ⇒ 红");

        // ---- B6：取景那两处夹取（原版 `Mathf.Min(|Δ|, maxVerticalSizeInViewPort = 7.0)`）----
        Tr(Near(CombatAutoZoom.BoundsVerticalSizeInViewport(3f), 3f)
            && Near(CombatAutoZoom.BoundsVerticalSizeInViewport(-3f), 3f)
            && Near(CombatAutoZoom.BoundsVerticalSizeInViewport(12f), 7f),
           "B6 `bounds = Min(|世界高差|, **7.0**)`（原版 `framer+0x78`）：±3 → 3、12 → 7");
        Tr(Near(CombatAutoZoom.MaxVerticalSizeInViewPort, 7f, 1e-6f),
           $"B6′ `maxVerticalSizeInViewPort` = **7.0f**（13/13 场逐字节相同；实得 {CombatAutoZoom.MaxVerticalSizeInViewPort})");

        // ---- B7：`sensorSize.x` 那半 —— zoom = 1 是**恒等**、zoom = 0 取曲线值（两者必须真的不同）----
        float sx1 = CombatAutoZoom.FrameSensorSizeX(41.5f, 7f, 1f);
        float sx0 = CombatAutoZoom.FrameSensorSizeX(41.5f, 7f, 0f);
        Tr(Near(sx1, 41.5f),
           $"B7 `zoom = 1` ⇒ `sensorSize.x` **恒等**于 `originalSensorSize.x`（41.5 → 实得 {sx1:F6}）");
        Tr(Mathf.Abs(sx1 - sx0) > 5f,
           $"B7′ `zoom = 0` ⇒ 取 `cameraSizeXTable` 曲线值、与 zoom=1 **差得开**（{sx1:F4} vs {sx0:F4}）");
    }

    // ============================================================ C：整卡倾摆（`AutoCardRotation`）
    static void SectionC_整卡倾摆()
    {
        MenuCheck.Section(_sink, "C · 整卡倾摆：两次 `GetRotation` 的**相乘顺序**与转轴");

        // ---- C1：原版 `AutoCardRotation.GetRotation` 的单轴 —— 轴不能换 ----
        // 原版 Update 里 x 位移绕 `Vector3.down`（`0x24`）、y 位移绕 `Vector3.right`（`0x3C`）。
        var qDown = AutoCardRotation.GetRotation(1f, 10f, Vector3.down);
        var want  = Quaternion.AngleAxis(10f, Vector3.down);
        Tr(Quaternion.Angle(qDown, want) < 0.01f,
           $"C1 `GetRotation(1, 10, down)` = `AngleAxis(10°, down)`（夹角 {Quaternion.Angle(qDown, want):F4}°）");

        // ---- C2：轴方向（灭自证：写成 `Vector3.up` 就红）----
        // `AngleAxis(10, (0,−1,0))` ⇒ y = −sin5° = −0.087156、w = cos5° = 0.996195；**y 必须是负的**。
        Tr(qDown.y < -0.08f && Mathf.Abs(qDown.w - 0.996195f) < 1e-3f && Mathf.Abs(qDown.x) < 1e-6f,
           $"C2 转轴 = `Vector3.down`（y = −0.087156；实得 {qDown.x:F6}/{qDown.y:F6}/{qDown.z:F6}/{qDown.w:F6}）"
         + "｜🧨 写成 `Vector3.up` ⇒ y 变 +，本条红");

        // ---- C3：夹取对称（原版用「符号位翻转」得到 −maxAngle，不是 0..max 的单边夹）----
        Tr(Quaternion.Angle(AutoCardRotation.GetRotation(100f, 10f, Vector3.down), qDown) < 0.01f
            && Quaternion.Angle(AutoCardRotation.GetRotation(-100f, 10f, Vector3.down),
                                AutoCardRotation.GetRotation(-1f, 10f, Vector3.down)) < 0.01f,
           "C3 角度夹到 **±maxAngle** 两侧对称（+100 ⇒ +10°；−100 与 −1 同一个结果）");

        // ---- C4：**相乘顺序** —— 原版 `GetRotation(dx, maxX, down) * GetRotation(dy, maxY, right)` ----
        // 两个四元数**不可交换** ⇒ 把顺序对调就是另一个姿态。
        var qX = AutoCardRotation.GetRotation(1f, 10f, Vector3.down);
        var qY = AutoCardRotation.GetRotation(1f, 10f, Vector3.right);
        var got = AutoCardRotation.TargetRotationFor(1f, 1f, 10f, 10f);
        var swapped = qY * qX;
        Tr(Quaternion.Angle(got, qX * qY) < 0.01f,
           $"C4 `TargetRotationFor(dx,dy)` = **`Rot(dx, maxX, down) × Rot(dy, maxY, right)`**（先 x 后 y；夹角 {Quaternion.Angle(got, qX * qY):F4}°）");
        Tr(Quaternion.Angle(got, swapped) > 0.01f,
           $"C4′（灭自证）**对调顺序就是另一个姿态**（与 `qY×qX` 差 {Quaternion.Angle(got, swapped):F4}° > 0）"
         + "｜🧨 把 `TargetRotationFor` 里两个因子对调 ⇒ C4 红");

        // ---- C5：位移为 0 ⇒ 目标旋转 = identity（回正那一档）----
        Tr(Quaternion.Angle(AutoCardRotation.TargetRotationFor(0f, 0f, 10f, 10f), Quaternion.identity) < 1e-3f,
           "C5 位移为 0 ⇒ 目标旋转 = identity（停住自己回正）");

        // ---- C6：`EVAL_TIME_AFTER_MOVEMENT` = 2 秒（`.cctor` 的 `0x40000000`）----
        Tr(Near(AutoCardRotation.EvalTimeAfterMovement, 2f, 1e-6f),
           $"C6 `EVAL_TIME_AFTER_MOVEMENT` = **2f**（实读 `0x40000000`；实得 {AutoCardRotation.EvalTimeAfterMovement:F6}）");
    }

    // ============================================================ D：相机序列化常量（`CombatCameraZoom`）
    //
    // 原版这三格**ctor 出厂值 ≠ 13/13 场的场景值**（铁律 5·c 的「一个值 ≠ 全部情况」）——
    // 我们取**场景值**（玩家实况遇到的那一档）。⛔ 别把它改回 ctor 默认。
    static void SectionD_相机序列化常量()
    {
        MenuCheck.Section(_sink, "D · `CombatCameraZoom` 三格「场景值 vs ctor 出厂值」+ 世界边界");

        var go = new GameObject("G1025_CamZoomProbe");
        CombatCameraZoom z = null;
        try
        {
            z = go.AddComponent<CombatCameraZoom>();

            // ---- D1：三格场景值 ----
            Tr(Near(z.ZoomSpeedTest, 12.5f),
               $"D1 `zoomSpeed` = **12.5**（13/13 场序列化值；实得 {z.ZoomSpeedTest:F4}）");
            Tr(z.InvertMouseScrollWheelTest,
               "D2 `invertMouseScrollWheel` = **true**（场景 13/13 为 1，ctor 是 false）");
            Tr(Near(z.DragSensitivityTest, 0.01f),
               $"D3 `dragSensitivity` = **0.01**（场景 13/13，ctor 是 0.005；实得 {z.DragSensitivityTest:F4}）");

            // ---- D4：**灭自证** —— 三格都不能等于 ctor 出厂值 ----
            Tr(!Near(z.ZoomSpeedTest, 0.1f) && !Near(z.DragSensitivityTest, 0.005f) && z.InvertMouseScrollWheelTest,
               "D4（灭自证）三格都**不是** ctor 出厂值（0.1 / 0.005 / false）⇒ 谁把它改回 ctor 默认本条红");

            // ---- D5：其余三格（ctor 与场景一致，照抄）----
            Tr(Near(z.SnapBackSpeedTest, 5f) && Near(z.ZoomSensitivityTest, 0.1f)
                && Near(z.ZoomSensitivityMobileTest, 0.005f),
               $"D5 `snapBackSpeed` 5.0 / `zoomSensitivity` 0.1 / `zoomSensitivityMobile` 0.005"
             + $"（实得 {z.SnapBackSpeedTest:F4} / {z.ZoomSensitivityTest:F4} / {z.ZoomSensitivityMobileTest:F4}）");

            // ---- D6：世界边界 = 原版 `(89.95, −8, 20.115, 15.75)` **再减 100 的 x 平移** ----
            var b = z.WorldBoundsTest;
            Tr(Near(b.x, -10.05f) && Near(b.y, -8f) && Near(b.width, 20.115f) && Near(b.height, 15.75f),
               $"D6 `worldBounds` = 原版 (89.95,−8,20.115,15.75) − 场地平移 100 ⇒ **(−10.05,−8,20.115,15.75)**"
             + $"（实得 ({b.x:F4},{b.y:F4},{b.width:F4},{b.height:F4})）");
        }
        finally
        {
            Kill(go);
        }
    }
}
