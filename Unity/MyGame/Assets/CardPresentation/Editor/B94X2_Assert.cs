// B94X2_Assert.cs — 执行代理 X2（第九十四会话 · 波 1.5，2026-10-29）
//
// ============================ 它盯的是什么 ============================
// 账 `战斗逻辑驱动:381`（原版 `BattleManager.GetTargetInPos`）的**落点错配订正**：
//   那一条的 _我们的落点_ 原来指 `TargetReticle.ResolveAim` —— 但 `ResolveAim` 的真判据
//   **不是** `GetTargetInPos`，而是另一个原版类 `TargetReticleController`：
//     · `TargetReticleController__UpdateTrail.c`  —— 弧线终点：双平面求交（floorPlane / enemyMinionPlane 取近）
//     · `TargetReticleController__MoveCrosshair.c` —— 准星本体：`crosshair.set_position(targetPositionUISpace)`（原样指针）
//   `GetTargetInPos` 自己那条 2D 射线实参 `DAT_1834b2bc8` = **-1f**（`read_literal` 实读）⇒ 恒 0 命中 = 死支；
//   它真生效的是 3D 回退支（`Physics.RaycastNonAlloc(..., 200f, BoardElements)`），对应物是
//   `CardInteraction.Frontmost`（不在这里重测，那一条已由 `BattleScene` 的 `A1668` 族断言覆盖）。
//
// 本文件只钉 **2026-10-29 这一轮在 `Battle/TargetReticle.cs` 里对齐的三件事**：
//   A. 2D 兜底（无战场相机）⇒ `ResolveAim` 原样返回（🔶 自己设计：原版此处会 NRE）；
//   B. **双平面都打空 ⇒ 落点 = 世界原点**（原版双哨兵 `fVar9 <= fVar10` 相等 ⇒ 取第二平面默认值 `Vector3.zero`；
//      ⛔ 不是「保持指针点」——旧实现返回 `reticleWorld`，本轮已照抄原版怪值）；
//   C. **准星本体跟原始指针 / 弧线终点走求交**（原版是两个不同的点：`MoveCrosshair` 的 set_position vs `UpdateTrail` 的 desiredWorldPosition）。
//
// ============================ 怎么用（宿主·由调度台接）============================
//   `B94X2_Assert.Run();`
//   `_sink.Pass += B94X2_Assert.Pass; _sink.Fail += B94X2_Assert.Fail;`
//   · **不跑 Unity 批处理**、不建持久资产；建的 GameObject（含临时 Camera）在收尾 `DestroyImmediate`。
//   · **不依赖 `BattleDriver`**（`TargetReticle` 直接 `new GameObject().AddComponent<>().Build()`）。
//   · 建议宿主：`Editor/BattleScene.cs`（准星的主场；那里 `CardArt` 已 `Load()` 过 ⇒ `Build()` 能建出准星）。
//
// ============================ 🔴 灭自证（改哪一处会让哪一条红）============================
//   · B2：把 ResolveAim 的双打空分支从 `Vector3.zero` 改回 `reticleWorld` ⇒ B2 红（`pointer` 非零点）。
//   · C3：把 `Aim` 里准星那一句改回 `aim`（求交点）⇒ 准星落到 (0,0,0)、x/y 对不上 `pointer` ⇒ C3 红。
//   · C5：若「准星本体」与「弧线终点」又被合成同一个点（旧的 3 参 Show 口径）⇒ C5 红。
//   · A1：把 2D 兜底那条 `boardCam == null` 早退删掉 ⇒ A1 试图对 null 相机求交 ⇒ 抛异常/返回值变 ⇒ 红。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 X2：准星/瞄准线的真判据（`TargetReticleController.UpdateTrail` / `MoveCrosshair`）。
/// <para>⛔ **期望值全是字面量 / 独立算出来的量**，不从被测实现里读 —— 否则就是「拿我们的实现证明我们的实现」。</para></summary>
public static class B94X2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[X2] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "X2 准星/瞄准线：TargetReticle ← TargetReticleController.UpdateTrail / MoveCrosshair（账 战斗逻辑驱动:381）");

        SectionA_2D兜底原样返回();
        SectionB_双平面都打空落点为零();
        SectionC_准星原始指针_弧线求交();

        Debug.Log(_sink.P + "X2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ A 2D 兜底
    static void SectionA_2D兜底原样返回()
    {
        MenuCheck.Section(_sink, "A 2D 兜底（无战场相机 ⇒ 不求交；🔶 自己设计，原版此处 NRE）");
        var go = new GameObject("x2_reticle_A");
        try
        {
            var ret = go.AddComponent<TargetReticle>();
            ret.Build();
            Tr(ret.boardCam == null && ret.hudCam == null, "A0（前提）没有绑战场/HUD 相机");
            var p = new Vector3(1.23f, -4.56f, 7.89f);
            int miss0 = ret.AimMisses;
            var r = ret.ResolveAim(p);
            C(r, p, "A1 ★ 无战场相机 ⇒ `ResolveAim` **原样返回**（不做平面求交）");
            Tr(!ret.LastAimOnPlane, "A2 …并且 `LastAimOnPlane = false`（不欺骗下游说「求到平面上了」）");
            C(ret.AimMisses, miss0, "A3 …这条 2D 兜底路**不**计 `AimMisses`（它不是「打空」）");
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ------------------------------------------------------------ B 双平面都打空
    static void SectionB_双平面都打空落点为零()
    {
        MenuCheck.Section(_sink, "B 双平面都打空 ⇒ 落点 = 世界原点（原版 `fVar9<=fVar10` 双哨兵取 `Vector3.zero`）");
        var go = new GameObject("x2_reticle_B");
        Camera bc = null, hc = null;
        try
        {
            var ret = go.AddComponent<TargetReticle>();
            ret.Build();

            // 战场相机：正常朝前；点在它正前方 ⇒ 投到**屏幕中心**（z>0，过掉「在相机背后」那道闸）。
            var bcGo = new GameObject("x2_bc");
            bc = bcGo.AddComponent<Camera>();
            bc.transform.position = new Vector3(5f, 10f, -16f);
            bc.transform.LookAt(new Vector3(0f, 0f, 0f));
            bc.fieldOfView = 46.4f;

            // HUD 相机：forward = (0, +1, +1)/√2 —— **两条平面都结构性打空**，不靠浮点巧合。
            //   · 地面平面 (y=0, 法线 up)：origin 在 y=5（在地面之上）且 dir.y = +0.707 ⇒ enter < 0 ⇒ **打空**；
            //   · 敌兵平面 (z=enemyRowZ, 法线 -z)：dir.z = +0.707 ⇒ vdot = −0.707 < 0 ⇒ enter < 0 ⇒ **打空**。
            //   ⛔ 别改回 `Quaternion.Euler(-90,0,0)`：它的 forward 是 `(0, 0.99999996, ±2e-8)`（float 精度残差），
            //      而 Unity `Plane.Raycast` 的闸是 `vdot == 0`（**精确零**、不是 Approximately）⇒ 敌兵平面
            //      `vdot ≈ 8e-8 ≠ 0` ⇒ 在 4/8e-8 ≈ **4.7e7** 处判「命中」（实测落点 `(0, 47171730, −4)`，z=−4 就是敌方那面）。
            var hcGo = new GameObject("x2_hc");
            hc = hcGo.AddComponent<Camera>();
            hc.transform.position = new Vector3(0f, 5f, 0f);
            hc.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1f, 1f).normalized);
            hc.fieldOfView = 60f;

            ret.boardCam = bc; ret.hudCam = hc; ret.floorY = 0f; ret.enemyRowZ = -4f;

            Vector3 pointer = bc.transform.position + bc.transform.forward * 10f;   // 非零点、在战场相机正前方
            Tr(pointer.sqrMagnitude > 1f, "B0（前提）原始指针点非零（否则 B2 分不出「原点」与「原样返回」）");

            int miss0 = ret.AimMisses;
            var r = ret.ResolveAim(pointer);
            C(ret.AimMisses, miss0 + 1, "B1（前提）这条射线确实**两个平面都打空**（`AimMisses` +1）");
            C(r, Vector3.zero,
                "B2 ★ 双平面都打空 ⇒ 落点 = **世界原点 (0,0,0)**（照抄原版 `fVar9<=fVar10` 取 `Vector3.zero`；"
              + "⛔ 不是 `pointer` —— 旧实现返回 `reticleWorld` 是偏离）");
            Tr(!ret.LastAimOnPlane, "B3 …`LastAimOnPlane = false`（打空不冒充落在平面上）");
        }
        finally
        {
            if (bc != null) Object.DestroyImmediate(bc.gameObject);
            if (hc != null) Object.DestroyImmediate(hc.gameObject);
            Object.DestroyImmediate(go);
        }
    }

    // ------------------------------------------------------------ C 准星 vs 弧线
    static void SectionC_准星原始指针_弧线求交()
    {
        MenuCheck.Section(_sink, "C 准星本体跟【原始指针】· 弧线终点走求交（原版 MoveCrosshair vs UpdateTrail 是两个点）");
        var go = new GameObject("x2_reticle_C");
        Camera bc = null, hc = null;
        try
        {
            var ret = go.AddComponent<TargetReticle>();
            ret.Build();

            // 复用 B 的「双平面都打空」相机 ⇒ 求交结果 = Vector3.zero，而原始指针是个**非零**点
            // ⇒ 两者必不同，足以分辨「准星跟谁」。
            var bcGo = new GameObject("x2_bc_C");
            bc = bcGo.AddComponent<Camera>();
            bc.transform.position = new Vector3(5f, 10f, -16f);
            bc.transform.LookAt(new Vector3(0f, 0f, 0f));
            bc.fieldOfView = 46.4f;

            var hcGo = new GameObject("x2_hc_C");
            hc = hcGo.AddComponent<Camera>();
            hc.transform.position = new Vector3(0f, 5f, 0f);
            // 与 B 段**同一套**「结构性双打空」朝向 —— forward = (0,+1,+1)/√2（⛔ 别改回 `Euler(-90,0,0)`，
            // 那条的 float 残差会让敌兵平面在 4.7e7 处命中；机理与实测见 B 段那段注释）。
            hc.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1f, 1f).normalized);
            hc.fieldOfView = 60f;

            ret.boardCam = bc; ret.hudCam = hc; ret.floorY = 0f; ret.enemyRowZ = -4f;

            var anchor = new Vector3(-2f, 0f, -6f);
            Vector3 pointer = bc.transform.position + bc.transform.forward * 10f;
            Tr(pointer.sqrMagnitude > 1f, "C0（前提）原始指针点非零");

            // Show 先把状态点亮（`Visible = true`），再每帧 Aim 喂原始指针。
            ret.Show(anchor, ret.ResolveAim(pointer), AttackKind.Melee);
            ret.Aim(anchor, pointer, AttackKind.Melee);

            Vector3 cross;
            Tr(ret.CrossPosition(out cross), "C2（前提）准星建出来了且可见");
            Tr(Mathf.Abs(cross.x - pointer.x) < 1e-2f && Mathf.Abs(cross.y - pointer.y) < 1e-2f,
                $"C3 ★ 准星本体的 x/y = **原始指针**（实测 {cross.x:F3},{cross.y:F3} ← 指针 {pointer.x:F3},{pointer.y:F3}）；"
              + "判据 = `MoveCrosshair.c` 第一句 `crosshair.set_position(targetPositionUISpace)`"
              + "｜🧨 改回 `aim`（求交点）⇒ 落到 (0,0) 附近 ⇒ 本行红");
            C(ret.LastAim, Vector3.zero,
                "C4 弧线终点 = 求交结果（此夹具双平面都打空 ⇒ 世界原点）—— 与准星本体是**不同的点**");
            Tr(Vector3.Distance(cross, ret.LastAim) > 0.5f,
                $"C5 ★ 「准星本体 ≠ 弧线终点」（相距 {Vector3.Distance(cross, ret.LastAim):F2}）"
              + " —— 原版是两个对象各喂各的（`MoveCrosshair` 喂原指针 / `UpdateTrail` 喂求交点）"
              + "｜🧨 把两者又合成一个点（旧 3 参 Show 口径）⇒ 本行红");
        }
        finally
        {
            if (bc != null) Object.DestroyImmediate(bc.gameObject);
            if (hc != null) Object.DestroyImmediate(hc.gameObject);
            Object.DestroyImmediate(go);
        }
    }
}
