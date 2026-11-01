// ItemDrawerPartial_B114W39.cs — 第一百一十四会话 · 写手 `W-39`（2026-11-01）· 账 `B-265` / `B-266`
//
// ============================ 它补的是什么 ============================
//  `Campaign Points` 这一族的**基础档**（`Campaign Points Drawer.prefab`）有两处与原版不符：
//
//  ① `B-265` —— **主图几何**。原版 `Content/Image`（RectTransform `!u!224 &224017697238300914`）：
//       `m_AnchorMin (0, 0.4)` · `m_AnchorMax (1, 0.8)` · `m_SizeDelta (0,0)` · `m_LocalScale (1,1,1)` ·
//       `m_Pivot (0.5,0.5)` · `m_PreserveAspect 1`
//     ⇒ 它**铺满抽屉的整条横带**（根设计 px 的 y = `1080 − 864 .. 1080 − 432` = **216..648**，
//       x = `Content` 的 `0.4..712.4`），再按 PA 把 256² 的方图内接成 **432²**（居中在 `(356.4, 432)`）。
//     我们原来两档共用 `Square(box, st.ArmyFill)`（居中方块）—— **只有 Icon 档恰好等价**：
//     Icon 档的 `Content/Image`（`!u!224 &224313026266067498`）锚是 `(0,0)-(1,1)` + `m_SizeDelta (0,0)` + PA
//     ⇒ 铺满 1080² 的 `Content` 再按 PA 内接 = `min(W,H)` 方框 = `Square(box, 1)`。
//     ⚠️ 基础档那格用 `ArmyFill` 是**我们挑的**（`CampaignTab.CampaignPointStyle` 给 `1f`，
//        `ItemDrawerStyle.Default` 给 `0.34f`）⇒ 同一个抽屉在两条调用路径上主图**大小都不一样**，
//        而且没有一条等于原版的 432²。本文件把两档各自照 prefab 的锚点算回来。
//
//  ② `B-266` —— **底板层**。原版 `ItemDrawerComponents`（`!u!114 &114302127217294661`）的
//     `background` = `{fileID: 114875609422105747}` = **`Content/Background` 的真实节点**
//     （`m_IsActive 1` · `m_Enabled 1` · `m_PreserveAspect 1` · sprite `40k_Rewards_icon_base card`
//     guid `c8f9b1a41360ac845919c316ddae577b`），而 `CampaignPoint` **从不画底板** ⇒ 基础档少一层。
//     ⚠️ **与 `B-256` 的区别**：那条是「**有槽，但 `m_IsActive: 0`**」（原版**不画**）；
//        本条是「**有槽、且原版真的画**」（`m_IsActive 1` + `m_Enabled 1`）。
//     ⚠️ **Icon 档的背景槽是 `{fileID: 0}`**（`!u!114 &114985305603660343`：`background: {fileID: 0}`）
//        ⇒ 那一档**不画底板**（同族先例：`Editor/B114_C_Assert.cs:121` 对 `PlayerAvatar` 的 Icon 变体
//        断的就是「`background` 槽 null ⇒ 不建 `Background`」）。
//
// ── 判据出处 ────────────────────────────────────────────────────────────────
//    `d:/2/tools/assetripper_out/ExportedProject/Assets/Campaign Points Drawer.prefab`
//    · GameObject `Image`（`&1703437598541787`）→ RT `&224017697238300914` / Image `&114217368724061974`
//    · `Content/Background` → Image `&114875609422105747`（在 `ItemDrawerComponents` 的 `background` 槽里）
//    复核（现读，同一份表：`Image 0.4 216.0 712.4 648.0 712.00 432.00 … preserveAspect`）：
//      python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Campaign Points Drawer" --depth 2 --relative
//
// ── 坐标口径 ────────────────────────────────────────────────────────────────
//    两个矩形都是「根设计 px · 左上原点 · y 向下」，与 `FrameOf/Map` 同一套（见 `ItemDrawerLayout.cs` 文件头）。
//    基础档走 `FrameOf(box)`（根 712.8×1080）；Icon 档走 `FrameOf(box, DrIconRoot, DrIconRoot)`（根 1080²）。
//    ⚠️ `m_PreserveAspect = 1` 那一档由调用点的 `MenuDraw.Rect(..., keepAspect: true)` 负责，
//       本文件**只负责给对框**（给错了 PA 也救不回来：宽高比一样时它什么也不做）。
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        // ============================================================ B-265：主图矩形（照 prefab 的锚点）
        //
        //  🔴 为什么不能继续用 `Square(box, st.ArmyFill)`：
        //     · 基础档的 `Content/Image` 是 **(0,0.4)-(1,0.8)** 的**横带**（不是正方形），PA 之后才是方图；
        //     · `ArmyFill` 在两处调用点取值不同（`CampaignPointStyle()` = 1、`Default()` = 0.34）
        //       ⇒ 同一个抽屉的主图大小**随调用方变**，而原版只由 prefab 决定。
        //  🔴 `DrImgY1/DrImgY2`（216 / 648）**不是巧合**：`Content/Image` 的默认矩形就是按
        //     `(0,0.4)-(1,0.8) × 1080` 算出来的（`1080 − 0.8×1080 = 216` · `1080 − 0.4×1080 = 648`）
        //     ⇒ 这里复用它 = 复用同一条规则（`CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」）。
        /// <summary>`Campaign Points` 抽屉主图（`Content/Image`）的矩形 —— **两档照各自的 prefab 锚点**。
        /// <para>基础档 `Campaign Points Drawer`：锚 `(0,0.4)-(1,0.8)` + `m_SizeDelta 0` + `m_LocalScale 1`
        /// ⇒ 根设计 px 的 `(0.4,216)-(712.4,648)`（712×432 的横带；`m_PreserveAspect 1` + 256² 方图 ⇒ 画成 432²）。</para>
        /// <para>Icon 档 `Icon Campaign Points Drawer Variant`：锚 `(0,0)-(1,1)` + `m_SizeDelta 0`
        /// ⇒ 铺满 1080² 的 `Content`（PA 后方图 = 1080²）。</para>
        /// <para>⚠️ 这个框**只是布局框**：方图要画成 432² / 1080² 靠调用点的 `keepAspect: true`（`CardbackFace.Fit`）。</para></summary>
        internal static PxRect CampaignPointsImageRect(PxRect box, ItemDrawerStyle st)
        {
            if (st.CampaignPointsGlowIcon)
                return FrameOf(box, DrIconRoot, DrIconRoot).Map(0f, 0f, DrIconRoot, DrIconRoot);
            var fr = FrameOf(box, fit: RootFit.HeightControlsWidth);   // 基础档 `Campaign Points Drawer`：ARF 2 · 0.66（B-260）
            return fr.Map(DrCx1, DrImgY1, DrCx2, DrImgY2);
        }

        // ============================================================ B-266：底板层（只有基础档有）
        /// <summary>原版 `Content/Background` 那一层（`ItemDrawerComponents.background` 槽指向的真实节点）。
        /// <para>**只有基础档画** —— Icon 档的 `background` 槽是 `{fileID: 0}`（见文件头 ②）。</para>
        /// <para>走既有的 <see cref="DrawerBackground"/>（取不到图 ⇒ `Note` 出声一次、**不画**，⛔ 不拿别的图顶替）：
        /// 那一层画的就是 `Content` 整块（`fr.Map(DrCx1, 0, DrCx2, DrRootH)`），与 `Content` 自身同尺寸，
        /// 而我们的 `FrameOf` 已经把 `Content` 的偏移/缩放算进 `K/OX/OY` ⇒ 直接画在抽屉根 `node` 上等价。</para>
        /// <para>✅ **2026-11-01（`B-284` · `B114_W43`）已与原版对齐**：原版这颗 `Image` 的 `m_PreserveAspect = 1`，
        /// 而 `DrawerBackground` 的 `keepAspect` 形参默认值已于同日由 `false` 改成 **`true`**（全族一起，判据见 `ItemDrawerLayout.cs` 那颗形参的 doc）。
        /// ⇒ 这一档现在按 328×498 的**自身比例**画（`1080 × 328/498 = 711.325` 宽），与 `Content` 712×1080 的差是 **−0.675 px**。
        /// ⚠️ 订正痕迹（铁律 5）：本段原来写「`DrawerBackground` 走 `keepAspect: false`…要改就得动共用件、⛔ 不在本账范围内」——**已过期**。</para></summary>
        internal static bool DrawCampaignPointsBackground(Transform node, PxRect box, ItemDrawerStyle st)
        {
            if (node == null) return false;
            if (st.CampaignPointsGlowIcon) return false;     // Icon 档 `background: {fileID: 0}` ⇒ 原版不画
            return DrawerBackground(node, FrameOf(box, fit: RootFit.HeightControlsWidth), st);   // 基础档 ARF 2 · 0.66（B-260）
        }
    }
}
