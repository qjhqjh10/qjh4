// WFCamerasConversion.cs — 原版 `CamerasConversionHelper` 的对应物（棋盘相机 ↔ HUD 相机 的换算）
//
// 🔴 判据来源（**全部是机器码级实读**，不是按名字猜）：
//   · `d:/2/tools/decomp_full/CamerasConversionHelper__*.c`（六个方法逐一亲读）
//   · 常量值用 `工具/read_literal.py` 从 `GameAssembly.dll` 读出来：
//       `DAT_1834b2e98` = **57.2958**（180/π）· `DAT_1834b2bb4` = **0.5** ·
//       `DAT_1834b2dc0` = **0.0174533**（π/180）· `DAT_1834b2bb8` = **1.0** ·
//       `DAT_1834b2e60` = `0x7FFFFFFF`（`Mathf.Abs` 的位掩码）
//   · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/CamerasConversionHelper.cs`（交叉；⚠️ 它有失真：
//     入参写成未初始化 `default`、只回写 x/z 不写 y、返回类型元组被当成方法名 —— **以 `.c` 为准**）
//
// ---- 原版的六个方法（**行号是本文件对 ILSpy 那份的**，G1/G2 两轮的错位争议在这里定案）----
//   `:7`   `ConvertPositionAndScaleBetweenCameras(Camera, Camera, float, Vector3)` → BoundingSphere
//   `:75`  `ConvertPositionAndScaleBoardToUI(Vector3)`                            → BoundingSphere
//   `:107` `ConvertPositionAndScaleUIToBoard(Vector3, float)`                     → BoundingSphere
//   `:149` `ConvertPositionBetweenCameras(Camera, Camera, float, Vector3)`        → Vector3
//   `:174` `ConvertPositionBoardToUI(Vector3)`                                    → Vector3
//   `:218` `ConvertPositionUIToBoard(Vector3, float)`                             → Vector3
//   `:270` `GetFovFromCamera(Camera)`  ·  `:278` `GetUIZPosition()`
//   ⇒ **`:7/:75/:107` 是 `AndScale` 那一族**（返回 BoundingSphere），⛔ 不是去掉 `AndScale` 的名字。
//
// ---- 换算本体（`ConvertPositionBetweenCameras`，逐句）----
//   `v = origin.WorldToViewportPoint(p);  v.z = desiredZDistance;  return dest.ViewportToWorldPoint(v);`
//   ⚠️ **`desiredZDistance` 是「沿目的地相机 forward 的视口深度」**，不是世界 z ——
//      `v.z` 直接覆写，不加减任何相机 z。调用点自己把「想要的世界 z」折成深度
//      （`UIToBoard` = `desiredZ − boardCamera.transform.position.z`；`BoardToUI` = `GetUIZPosition()`）。
//
// ---- 半径（三个 `AndScale` 共同的那一句，逐句）----
//   `radius = (tan(halfFov_dest) · 2·desiredZ) / (tan(halfFov_origin) · 2·|p.z − originCam.z|)`
//   其中 `halfFov` 是**半视角**，机器码里的求法是
//     `f = atan(1 / projectionMatrix.m11)` → `× 57.2958` → `× 0.5` → `× 0.0174533` → `tan(f)`
//   ⇒ 三个常数相乘 = **1**，`atan` 与 `tan` 又互为反函数 ⇒ **整条链恒等于 `1 / projectionMatrix.m11`**。
//      （`m11` 的字节偏移在 `projectionMatrix` 里是 `0x14` = 行 1 列 1；透视相机 `m11 = 1/tan(fov/2)`。）
//   ⚠️ 我们**照原版走这一整条链之后的等价值**（`HalfTan`），所以正交相机下也会得到一个「数」——
//      那不是「真半高」（正交的半高不随深度变）。原版也是这么算的，⛔ 别在这里「修正」它。
//
// ---- 相机从哪来 ----
//   原版是 `BattleManager.Instance.hudCamera(+0xA0)` / `.boardCamera(+0xA8)` 两个静态引用。
//   我们的表现层用回调注入（VFX 层不认识相机）：`HudCamera` / `BoardCamera` / `UICanvasZ`。
//   没接 ⇒ **照原版的失败姿势**：`CustomDebug.LogError` + 返回 `Vector3.zero` / `radius = 1`（`GetUIZPosition` 回 0），
//   另外**再记一个计数**（原版只有一条日志、没有计数；计数是给我们自检用的，不改行为）。
using System;
using UnityEngine;

namespace WarpforgeVFX
{
    public static class WFCamerasConversion
    {
        // ==================================================================
        //  下游钩子（表现层挂；⛔ 都不挂 = 每条换算都会报错并回零值，不会静默给个错数）
        // ==================================================================

        /// <summary>原版 `BattleManager.Instance.hudCamera`（字段偏移 `+0xA0`）—— **UI/手牌**那台相机。</summary>
        public static Func<Camera> HudCamera;

        /// <summary>原版 `BattleManager.Instance.boardCamera`（字段偏移 `+0xA8`）—— **3D 棋盘**那台相机。</summary>
        public static Func<Camera> BoardCamera;

        /// <summary>原版 `GetUIZPosition()` 里 `BattleHud` 的 `GetComponentInChildren&lt;Canvas&gt;().transform.position.z`。
        /// 我们把它拆成一个「报 UI 画布世界 z」的回调（`GetUIZPosition` 再减 HUD 相机 z）。</summary>
        public static Func<float> UICanvasZ;

        // ---- 诊断计数（自检用；**不改行为**）----
        /// <summary>`GetUIZPosition` / `BoardToUI` 走到「HudCamera 没接」那一支的次数。</summary>
        public static int MissingHudCamera;
        /// <summary>`BoardToUI` / `UIToBoard` 走到「BoardCamera 没接」那一支的次数。</summary>
        public static int MissingBoardCamera;
        /// <summary>`UICanvasZ` 没接的次数（原版这是 `BattleHud` 里找不到 Canvas ⇒ NRE）。</summary>
        public static int MissingUICanvas;
        /// <summary>相机投影矩阵读到 `m11 == 0`（除零）的次数。</summary>
        public static int ZeroProjectionM11;

        static bool _warnHud, _warnBoard, _warnCanvas, _warnM11;

        public static void ResetDiagnostics()
        {
            MissingHudCamera = MissingBoardCamera = MissingUICanvas = ZeroProjectionM11 = 0;
            _warnHud = _warnBoard = _warnCanvas = _warnM11 = false;
        }

        // ==================================================================
        //  原版 `GetUIZPosition()`
        // ==================================================================
        /// <summary>原版：`BattleHud` 里那个 `Canvas` 的 `transform.position.z` **减去** `hudCamera.transform.position.z`。
        /// <para>找不到（`BattleHud.Instance` 为空 / 没有 Canvas / 没有 hudCamera）⇒ `CustomDebug.LogError` 后 **返回 0**。</para></summary>
        public static float GetUIZPosition()
        {
            var hud = HudCamera != null ? HudCamera() : null;
            if (hud == null)
            {
                MissingHudCamera++;
                if (!_warnHud)
                {
                    _warnHud = true;
                    Debug.LogError("[WarpforgeVFX] `WFCamerasConversion.HudCamera` 没接（原版 `BattleManager.hudCamera`）" +
                                   " ⇒ `GetUIZPosition()` 按原版回 **0**（手牌→棋盘那一跳的深度会错）。");
                }
                return 0f;
            }
            if (UICanvasZ == null)
            {
                MissingUICanvas++;
                if (!_warnCanvas)
                {
                    _warnCanvas = true;
                    Debug.LogError("[WarpforgeVFX] `WFCamerasConversion.UICanvasZ` 没接（原版 `BattleHud` 里的 " +
                                   "`Canvas.transform.position.z`）⇒ `GetUIZPosition()` 按原版回 **0**。");
                }
                return 0f;
            }
            return UICanvasZ() - hud.transform.position.z;
        }

        // ==================================================================
        //  原版 `ConvertPositionBetweenCameras`（ILSpy `:149`）—— **只换算位置**
        // ==================================================================
        /// <summary>`v = origin.WorldToViewportPoint(p); v.z = desiredZDistance; dest.ViewportToWorldPoint(v)`。
        /// <para>⚠️ 原版对「相机为空」的处置是 `NullReferenceException`（`FUN_1803f47a0` = 抛异常桩）。
        /// 我们改成 **LogError + 原样返回 `p`**（不崩、也不静默 —— 计数 + 一次警告）。</para></summary>
        public static Vector3 ConvertPositionBetweenCameras(Camera origin, Camera dest,
                                                           float desiredZDistance, Vector3 objectCurrentWorldPosition)
        {
            if (origin == null || dest == null)
            {
                Debug.LogError("[WarpforgeVFX] ConvertPositionBetweenCameras 的相机是 null" +
                               "（原版这里直接 NullReferenceException）—— 原样返回入参，不换算。");
                return objectCurrentWorldPosition;
            }
            var v = origin.WorldToViewportPoint(objectCurrentWorldPosition);
            v.z = desiredZDistance;
            return dest.ViewportToWorldPoint(v);
        }

        // ==================================================================
        //  原版 `ConvertPositionAndScaleBetweenCameras`（ILSpy `:7`）
        // ==================================================================
        /// <summary>位置走 <see cref="ConvertPositionBetweenCameras"/>，半径 = 两台相机在该深度上的「投影高度」之比
        /// （算式与三个常数见文件头）。</summary>
        public static BoundingSphere ConvertPositionAndScaleBetweenCameras(
            Camera origin, Camera dest, float desiredZDistance, Vector3 objectCurrentWorldPosition)
        {
            if (origin == null || dest == null)
            {
                Debug.LogError("[WarpforgeVFX] ConvertPositionAndScaleBetweenCameras 的相机是 null" +
                               "（原版这里直接 NullReferenceException）—— 回 (原点, 1)。");
                return new BoundingSphere(Vector3.zero, 1f);
            }
            var pos = ConvertPositionBetweenCameras(origin, dest, desiredZDistance, objectCurrentWorldPosition);
            float tanDest = HalfTan(dest);
            float tanOrigin = HalfTan(origin);
            float dz = Mathf.Abs(objectCurrentWorldPosition.z - origin.transform.position.z);
            // 原版字面：`(tanDest * (desiredZ + desiredZ)) / (tanOrigin * (dz + dz))`
            float radius = tanOrigin == 0f ? 1f : (tanDest * (desiredZDistance + desiredZDistance))
                                              / (tanOrigin * (dz + dz));
            return new BoundingSphere(pos, radius);
        }

        // ==================================================================
        //  原版 `ConvertPositionAndScaleBoardToUI`（ILSpy `:75`）
        // ==================================================================
        /// <summary>棋盘相机 → HUD 相机，深度 = <see cref="GetUIZPosition"/>。
        /// 原版这一支的失败姿势是 `CustomDebug.LogError` + `BoundingSphere(Vector3.zero, 1)`。</summary>
        public static BoundingSphere ConvertPositionAndScaleBoardToUI(Vector3 boardPosition)
        {
            var hud = HudCamera != null ? HudCamera() : null;
            var board = BoardCamera != null ? BoardCamera() : null;
            if (hud == null || board == null) return FailBoardToUI(hud);
            return ConvertPositionAndScaleBetweenCameras(board, hud, GetUIZPosition(), boardPosition);
        }

        // ==================================================================
        //  原版 `ConvertPositionAndScaleUIToBoard`（ILSpy `:107`）
        // ==================================================================
        /// <summary>HUD 相机 → 棋盘相机，深度 = `desiredZDistance − boardCamera.transform.position.z`。
        /// <para>⚠️ 原版这一支**没有** null 检查的兜底（相机为空直接抛），我们照
        /// <see cref="ConvertPositionBetweenCameras"/> 的口径：LogError + 回 `(原点, 1)`。</para></summary>
        public static BoundingSphere ConvertPositionAndScaleUIToBoard(Vector3 uiPosition, float desiredZDistance)
        {
            var hud = HudCamera != null ? HudCamera() : null;
            var board = BoardCamera != null ? BoardCamera() : null;
            if (hud == null || board == null)
            {
                Debug.LogError("[WarpforgeVFX] ConvertPositionAndScaleUIToBoard 的相机是 null" +
                               "（原版这里直接 NullReferenceException）—— 回 (原点, 1)。");
                return new BoundingSphere(Vector3.zero, 1f);
            }
            return ConvertPositionAndScaleBetweenCameras(hud, board,
                                                        desiredZDistance - board.transform.position.z, uiPosition);
        }

        // ==================================================================
        //  原版 `ConvertPositionUIToBoard`（ILSpy `:218`）—— **只换算位置**
        // ==================================================================
        /// <summary>HUD 相机 → 棋盘相机，深度 = `desiredZDistance − boardCamera.transform.position.z`。
        /// ⚠️（原版这一支只换算位置、**不带 scale**；带 scale 的是上面那个 `AndScale` 版本。）</summary>
        public static Vector3 ConvertPositionUIToBoard(Vector3 uiPosition, float desiredZDistance)
        {
            var hud = HudCamera != null ? HudCamera() : null;
            var board = BoardCamera != null ? BoardCamera() : null;
            if (hud == null || board == null)
            {
                Debug.LogError("[WarpforgeVFX] ConvertPositionUIToBoard 的相机是 null" +
                               "（原版这里直接 NullReferenceException）—— 原样返回入参。");
                return uiPosition;
            }
            return ConvertPositionBetweenCameras(hud, board,
                                                 desiredZDistance - board.transform.position.z, uiPosition);
        }

        // ==================================================================
        //  原版 `GetFovFromCamera(Camera)`
        // ==================================================================
        /// <summary>原版 `:270`：**半视角的正切**（`1 / projectionMatrix.m11`）。
        /// <para>⚠️ 它是**死入口**（全量反编译里 0 个外部调用点，`AndScale` 那三支都是内联同一条式子）。
        /// 这里留一份公开实现只是为了让「同一条规则只写一处」，⛔ 别新加调用点。</para></summary>
        public static float GetFovFromCamera(Camera cam)
        {
            return HalfTan(cam);
        }

        // ==================================================================
        //  内部
        // ==================================================================

        /// <summary>`tan(半视角)` = `1 / projectionMatrix.m11`。
        /// <para>机器码原样是 `tan( atan(1/m11) × 57.2958 × 0.5 × 0.0174533 )` —— 三个常数相乘 = 1、
        /// `atan`/`tan` 互反 ⇒ 恒等于 `1/m11`（推导见文件头）。**保留推导，别把 `HalfTan` 当「我们挑的」。**</para>
        /// <para>⚠️ 相机为空 / `m11 == 0` ⇒ 记一次 `ZeroProjectionM11` 并按 **1** 处理（原版会除出 ∞/NaN）。</para></summary>
        static float HalfTan(Camera cam)
        {
            if (cam == null) return 1f;
            float m11 = cam.projectionMatrix.m11;
            if (m11 > -1e-9f && m11 < 1e-9f)
            {
                ZeroProjectionM11++;
                if (!_warnM11)
                {
                    _warnM11 = true;
                    Debug.LogWarning("[WarpforgeVFX] 相机 `" + cam.name + "` 的 `projectionMatrix.m11` 是 0 —— " +
                                     "`AndScale` 的半径会除出 ∞/NaN（原版就是这个结果）。按 1 处理并出声。");
                }
                return 1f;
            }
            return 1f / m11;
        }

        /// <summary>原版 `ConvertPositionAndScaleBoardToUI` 的失败姿势：`LogError` + `BoundingSphere(Vector3.zero, 1)`。</summary>
        static BoundingSphere FailBoardToUI(Camera hud)
        {
            if (hud == null) MissingHudCamera++; else MissingBoardCamera++;
            if (!_warnBoard)
            {
                _warnBoard = true;
                Debug.LogError("[WarpforgeVFX] `ConvertPositionAndScaleBoardToUI` 要的两台相机没接全" +
                               "（`WFCamerasConversion.HudCamera` / `.BoardCamera`，原版是 " +
                               "`BattleManager.hudCamera/boardCamera`）—— 按原版回 `BoundingSphere(Vector3.zero, 1)`。");
            }
            return new BoundingSphere(Vector3.zero, 1f);
        }
    }
}
