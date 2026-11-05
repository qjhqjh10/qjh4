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
//   🔴 2026-10-29 订正（`B97_UI2` 现核）：`BattleDriver.HitSlot`（`BattleDriver.cs:33200`）—— ⛔ **不是** `CardInteraction.Frontmost`（那套是**手牌/换牌区**，`A1668`；账原先错锚就是这句）。断言在 `B97_UI2_Assert.cs`。
//
// 本文件只钉 **2026-10-29 这一轮在 `Battle/TargetReticle.cs` 里对齐的三件事**：
//   A. 2D 兜底（无战场相机）⇒ `ResolveAim` 原样返回（🔶 自己设计：原版此处会 NRE）；
//   B. **双平面都打空 ⇒ 落点 = 世界原点**（原版双哨兵 `fVar9 <= fVar10` 相等 ⇒ 取第二平面默认值 `Vector3.zero`；
//      ⛔ 不是「保持指针点」——旧实现返回 `reticleWorld`，本轮已照抄原版怪值）；
//   C. **准星本体跟原始指针 / 弧线终点走求交**（原版是两个不同的点：`MoveCrosshair` 的 set_position vs `UpdateTrail` 的 desiredWorldPosition）。
//
//   🔴 **2026-11-05 订正（`B148-W9`）**：本行原来写的是「2026-11-01（`B162`）起 `ResolveAim` 是 `hc`→`hc` 同一台相机往返，
//      B/C 夹具摆成『两个平面穿过相机』」—— **那套已作废**：`B148-W7` 照 `.c:58/70` 把出射改回 **`boardCam`**（`hc` 只投屏一次）
//      ⇒ 射线原点在 **bc**，「平面穿过 hc」不再保证打空。B / C 两段已改成「bc 朝上 + 背对敌兵平面」，并补 B0b/B0c（结构断言）。
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
//   · 🧨 **B1/B2/B3 · C4（2026-11-05 `B148-W9` 补）**：把 `boardCam.ScreenPointToRay` 改回 `hudCam.ScreenPointToRay` ⇒ 同一屏幕点下
//      `hc` 的射线**会命中敌兵平面**（B0b 已用几何独立算出这条）⇒ 落点 ≠ 世界原点、`AimMisses` 不涨、`LastAimOnPlane = true` ⇒ B1/B2/B3/C4 红。
//   · 🧨 **B2/C4（同上，第二种回退）**：把 `screen` 改成用 `boardCam.WorldToScreenPoint` 算 ⇒ 指针落在 bc 背后（`screen.z < 0`）⇒ 走早退、落点 = 原指针 ⇒ B2/C4 红。
//   · 🧨 **B0c（同上）**：把 bc 的摆法改回「斜着看原点」或与 hc 做成同构 ⇒ bc 出射不再双打空 ⇒ B0c 红（此时 B1/B2/B3 已失去鉴别力，它们测的仍是恒真量）。
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

            // 战场相机（原版 `boardCamera +0xA8`）= **出射那一台**（2026-11-05 `B148-W7` 起；`hc` 只负责投屏）。
            // 🔴 **2026-11-05 订正（`B148-W9`）**：本段原来写「bc 只是『有 3D 战场』的标记、`ResolveAim` 拿 `hc` 往返求交」——
            //   那套已作废。现在**射线由 bc 出** ⇒ 「双打空」必须拿 **bc 的射线**去凑，把平面摆成「穿过 hc」不再有任何保证。
            //   摆法：bc 在 y=8、朝**上且 +z**（`LookAt(0,16,6)` ⇒ forward = `(0, .894, .447)`）：
            //     · 地板 y=0 在它**下方**，射线朝上 ⇒ 永远打不中（走「背对平面」`t<0`，不靠残差、不靠平行分支）；
            //     · 敌兵平面 z=−15 在 **−z 侧**，射线朝 **+z** 走 ⇒ 同样永远打不中。
            //   ⛔ 别改回「两个平面穿过相机 ⇒ 恒打空」那种摆法：那条推导（`enter = −nearClip`）只对**出射相机自己**成立，
            //      而现在出射的是 bc、不是 hc（2026-10-24 那次的教训也一样：靠「射线与法线恰好不垂直」会在 4e7 处误判命中）。
            var bcGo = new GameObject("x2_bc");
            bc = bcGo.AddComponent<Camera>();
            bc.transform.position = new Vector3(0f, 8f, 2f);
            bc.transform.LookAt(new Vector3(0f, 16f, 6f));               // forward = (0, .894, .447)：朝上 + 朝 +z
            bc.fieldOfView = 46.4f;

            // HUD 相机：**只负责投屏**（`hc.WorldToScreenPoint`，原版 `.c:58`）。朝 +z 摆，指针放它正前方 10 单位
            //   ⇒ 投出来的 `screen` 恰是屏幕中心、`screen.z = 10 > 0`（过掉「在相机背后」那道闸）。
            var hcGo = new GameObject("x2_hc");
            hc = hcGo.AddComponent<Camera>();
            hc.transform.position = new Vector3(0f, 5f, -20f);
            hc.transform.rotation = Quaternion.identity;                 // forward = (0,0,1)（**精确**：不做任何旋转运算）
            hc.fieldOfView = 60f;

            ret.boardCam = bc; ret.hudCam = hc;
            ret.floorY    = 0f;                                          // 地面 y=0：在 bc **下方**、bc 朝上 ⇒ 打不中
            ret.enemyRowZ = -15f;                                        // 敌兵平面在 **−z 侧**、bc 朝 +z ⇒ 打不中

            Vector3 pointer = hc.transform.position + hc.transform.forward * 10f;   // (0,5,−10)：在 hc 正前方（`screen.z = 10`，过掉「在相机背后」那道闸）
            Tr(pointer.sqrMagnitude > 1f, "B0（前提）原始指针点非零（否则 B2 分不出「原点」与「原样返回」）");

            // ---- 结构断言（**不碰被测实现**：只用两台相机 + 两个平面，几何自己算）。同一屏幕点：
            //      `hc` 出射必**中**敌兵平面、`bc` 出射必**双打空**。它挡的是「夹具退化」——
            //      哪天有人把相机摆法改回旧摆法／把两台做成同构，双打空就不成立了，而 B1/B2/B3 会**照样绿**
            //      （它们测的本来就是同一个恒真量）⇒ 这一段就彻底没有鉴别力了。2026-11-05 `B148-W9` 补。
            Vector3 screenPt = hc.WorldToScreenPoint(pointer);
            var floorP = new Plane(Vector3.up, new Vector3(0f, ret.floorY, 0f));
            var enemyP = new Plane(Vector3.back, new Vector3(0f, 0f, ret.enemyRowZ));
            float dHud, dBf, dBe;
            bool hudHits = enemyP.Raycast(hc.ScreenPointToRay(screenPt), out dHud);
            bool bcHitsFloor = floorP.Raycast(bc.ScreenPointToRay(screenPt), out dBf);
            bool bcHitsEnemy = enemyP.Raycast(bc.ScreenPointToRay(screenPt), out dBe);
            Tr(hudHits, "B0b（结构）同一屏幕点、用 `hc` 出射 ⇒ **命中敌兵平面**（t=" + dHud.ToString("F2") + "）"
                      + " —— 这就是「出射改回 hc」时会踩中的那条路（🧨 它若不再命中，本段也测不出「混成一台」）");
            Tr(!bcHitsFloor && !bcHitsEnemy,
                "B0c（结构）**`bc` 出射两个平面都打空**（地板命中=" + bcHitsFloor + "、敌兵命中=" + bcHitsEnemy + "）"
              + " —— 这是 B1/B2/B3 的前提｜🧨 相机改回斜着看 ⇒ 本行先红，别等 B1/B2 一起变绿了才发现夹具失效");

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

            // 复用 B 的「双平面都打空」摆法 ⇒ 求交结果 = Vector3.zero，而原始指针是个**非零**点
            // ⇒ 两者必不同，足以分辨「准星跟谁」。
            // 🔴 **2026-11-05 订正（`B148-W9`）**：这里原来写「两个平面**穿过相机** ⇒ 恒打空」—— ⚠️ 那是 2026-11-01（`B162`）
            //   「`hc`→`hc`」时期的摆法；`B148-W7` 把出射改回 `boardCam` 之后，射线原点在 **bc**、平面却穿过 hc
            //   ⇒ 不再保证打空（实测会命中敌兵平面）。现改成与 B 段**同一套**「bc 朝上 + 背对敌兵平面」，机理见 B 段注释。
            var bcGo = new GameObject("x2_bc_C");
            bc = bcGo.AddComponent<Camera>();
            bc.transform.position = new Vector3(0f, 8f, 2f);
            bc.transform.LookAt(new Vector3(0f, 16f, 6f));    // 出射方：forward = (0, .894, .447)，朝上 + 朝 +z
            bc.fieldOfView = 46.4f;

            var hcGo = new GameObject("x2_hc_C");
            hc = hcGo.AddComponent<Camera>();
            hc.transform.position = new Vector3(0f, 5f, -20f);
            hc.transform.rotation = Quaternion.identity;      // forward = (0,0,1)（**精确**）；只投屏、不出射
            hc.fieldOfView = 60f;

            ret.boardCam = bc; ret.hudCam = hc;
            ret.floorY    = 0f;                               // 地面 y=0：在 bc 下方、bc 朝上 ⇒ 打不中
            ret.enemyRowZ = -15f;                             // 敌兵平面在 −z 侧、bc 朝 +z ⇒ 打不中

            var anchor = new Vector3(-2f, 0f, -6f);
            Vector3 pointer = hc.transform.position + hc.transform.forward * 10f;   // (0,5,−10)：从同一台 hc 出发
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
