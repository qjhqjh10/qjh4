// ItemDrawerPartial_B114W27.cs — 第一百一十四会话 · 写手 `W-27`（2026-11-01）· 账 `B-258`
//
// ============================ 它补的是什么 ============================
//  `Campaign Points` 这一族抽屉的 `Content` 下有一层**恒显**的静态件 `Campaign Glow`
//  （它**不在** `ItemDrawerComponents` 的任何槽里 ⇒ 原版**不由代码开关**，`m_IsActive = 1` 就是出厂态）。
//  与 `B-243`（`Expansion Pass Points`）同族，但**不同 prefab**；我们两档都没画。
//
//  判据（prefab 现读 · `d:/2/tools/assetripper_out/ExportedProject/Assets/`）：
//
//  ① 基础档 `Campaign Points Drawer.prefab`
//     · `Content.m_Children[1]` = `Campaign Glow`（在 `Background` **之后**、`Image` **之前**）
//     · `!u!224 &224371939378208658`：`anchorMin (0,0.4)` · `anchorMax (1,0.8)` ·
//       `m_SizeDelta (62.5253,37.8941)` · `m_AnchoredPosition (0,0)` · `m_LocalScale 1` · `m_Pivot (0.5,0.5)`
//       parent = `Content`（**712×1080**，居中在 712.8 宽的根里 ⇒ 左沿 `DrCx1` = 0.4）
//       ⇒ 本地框 `(-31.26265, 197.05295)-(743.26265, 666.94705)`（左上原点 · y 向下）
//       ⇒ **根设计 px `(-30.86265, 197.05295)-(743.66265, 666.94705)`**（774.5253 × 469.8941）
//     · `!u!114 &114320256170494724`：`m_Sprite` guid `d2b2111cc38ea6542aa8031e1578f84f`
//       = `40K_genearl_icon_Campaign points_big`（256×256，落盘名 `40K_genearl_icon_Campaign_points_big`）
//       · `m_PreserveAspect = 1` · `m_Type 0`(Simple) · `m_Color` 白 · `m_RaycastTarget = 1`
//
//  ② Icon 档 `Icon Campaign Points Drawer Variant.prefab`（= `Campaign Tab.prefab` 内嵌的那一份）
//     · `Content.m_Children[0]` = `Campaign Glow`（排在 `Image` **之前** ⇒ 画在它**下面**）
//     · `!u!224 &224161607090078389`：`anchorMin (0,0)` · `anchorMax (1,1)` · `m_SizeDelta (0,0)` ·
//       **`m_LocalScale 1.4999999`** · `m_Pivot (0.5,0.5)` · parent = `Content`（**1080×1080**）
//       ⇒ 本地 0..1080 绕中心 ×1.5 = **`(-270,-270)-(1350,1350)`**（1620² 过扫，整棵 prefab 无 `Mask`）
//     · `!u!114 &114470112387431263`：**同一张图**（同 guid `d2b2111c…`）· `m_PreserveAspect = 1` ·
//       `m_Type 0` · `m_Color` 白 · `m_RaycastTarget = 1`
//
//  ── 坐标口径 ────────────────────────────────────────────────────────────────
//    两组的框都是「根设计 px · 左上原点 · y 向下」（prefab 是 uGUI 左下原点 y 向上 ⇒ 已按 `1080 − y` 翻过）。
//    基础档走 `FrameOf(box)`（根 712.8×1080）；Icon 档走 `FrameOf(box, DrIconRoot, DrIconRoot)`（根 1080²）。
//
//  ── 谁调它 ────────────────────────────────────────────────────────────────
//    `ItemDrawer.CampaignPoint`（`ItemDrawer.cs:2179`）—— 这一族**同一个类、两套 prefab**，
//    变体判据 = `ItemDrawerStyle.CampaignPointsGlowIcon`（见本文件开头那个 partial struct；⛔ 不是按 `ov`，
//    因为 `ItemDrawerView.Refresh()` 重画只带 `_style`）。
//    · 真 ⇒ <see cref="DrawCampaignPointsGlowIcon"/>（= `Campaign Tab` 高级面板的点数格；
//      `Shell/ItemDrawer.cs` 的 `DrawCampaignPointsReward` 把它置真）
//    · 假（默认）⇒ <see cref="DrawCampaignPointsGlowBase"/>（= 战役奖励窗奖励格那条基础档）
using UnityEngine;

namespace CardPresentation
{
    public partial struct ItemDrawerStyle
    {
        /// <summary>`Campaign Points` 抽屉的变体档：**true = `Icon Campaign Points Drawer Variant`（根 1080²）** ·
        /// false（默认）= 基础档 `Campaign Points Drawer`（根 712.8×1080）。
        /// <para>🔶 自己设计（非原版）：原版靠「这一格的 `ItemDrawerReference` 指向哪份 prefab」表达，我们没有那张配置；
        /// 而本项目既有的等价口有两条 —— `DrawerOverride.Icon`（`B-223` 那 7 族）与 `ItemDrawerStyle` 上的变体字段
        /// （`PremiumLayers` / `IconFillBox`）。这里取**后者**：`ItemDrawerView.Refresh()` 重画时**只带 `_style`**
        /// （`ItemDrawerPartial_B109E11.cs:33` 那条已知限制），挂 `ov` 的话一刷新就退回基础档几何。</para>
        /// <para>谁定的 + 日期：调度台 2026-11-01 指派（`W-27` / 账 `B-258`）落地。默认 false ⇒ 既有调用点逐字节不变。</para></summary>
        public bool CampaignPointsGlowIcon;
    }

    public static partial class ItemDrawer
    {
        // ============================================================ 常量（照 prefab 逐字）
        //  ⚠️ 节点名 `Campaign Glow` 的常量**已经存在**（`ItemDrawerPremium.cs:43` 的 `NodeCampaignGlow`）⇒ 本文件**复用**它，
        //     ⛔ 别在此再声明一份（`ItemDrawer` 是同一个 partial 类 ⇒ 会 `CS0102`）。
        //  ⚠️ 图也**复用** `PremiumGlowSprite`（`ItemDrawerPremium.cs:233`，同一串 `40K_genearl_icon_Campaign_points_big`）
        //     —— 两档 prefab 的 `Campaign Glow.m_Sprite` guid 都是 `d2b2111cc38ea6542aa8031e1578f84f`，
        //     与 `Premium Drawer` 那一族的出厂横带**是同一张**。⛔ 别再声明第二个同串常量。

        // ---- 视觉框（根设计 px · 左上原点 · y 向下；出处见文件头）----
        /// <summary>基础档 `Campaign Glow` 的框（`Content` 712×1080 的锚 `(0,0.4)-(1,0.8)` + sizeDelta
        /// `(62.5253,37.8941)`，`m_LocalScale = 1`；再 + `DrCx1`（`Content` 在根里的左沿））。</summary>
        const float CpGlowBaseX1 = -30.86265f, CpGlowBaseY1 = 197.05295f;
        const float CpGlowBaseX2 = 743.66265f, CpGlowBaseY2 = 666.94705f;

        /// <summary>Icon 档 `Campaign Glow` 的框（`Content` 1080² 的锚 `(0,0)-(1,1)` + sizeDelta 0，
        /// 绕中心 `m_LocalScale 1.4999999` ⇒ 1620² 过扫）。</summary>
        const float CpGlowIconX1 = -270f, CpGlowIconY1 = -270f;
        const float CpGlowIconX2 = 1350f, CpGlowIconY2 = 1350f;

        // ============================================================ 渲染队列（这一层为什么要单独一档）
        //
        //  原版靠 **sibling 序**（`Content.m_Children` = [Campaign Glow, Image] / [Background, Campaign Glow, Image]）
        //  表达「后面的压前面的」。本项目定死了「**分层用渲染队列，不用 z**」（`CLAUDE.md` §三）⇒
        //  必须把它们翻成**互不相同**的队列号。
        //
        //  🔴 为什么不能复用 `QBoard` / `QArt`：这一族的调用点传的是 `ItemDrawerStyle.Default(QPanelPts, QPanelPts, QPanelPts)`
        //     （`CampaignTab.cs:1897` 的 `CampaignPointStyle()`）⇒ **`QBoard == QArt`** 就是本档的常态；
        //     光圈与主图同号 ⇒ 透明件按「到相机的距离」排序，那块近不透明的金属盘会随机压住主图。
        //
        //  🔶 自己设计（非原版）：`CampaignPointsGlowQueue = QBoard + 1` · `CampaignPointsArtQueue = max(QArt, QBoard + 2)`
        //     · 为什么必须自己定：原版用 sibling 序，我们的引擎没有 uGUI 运行时、只能靠 renderQueue 表达次序，
        //       而「在两个已有号之间插一层」需要空档 —— 调用点给的号常常是**连号** ⇒ 只能把主图往上让一档
        //     · 谁定的 + 日期：调度台 2026-11-01 指派（`W-27` / 账 `B-258`）落地；
        //       口径与 `B-243` 的 `CampaignGlowQueue` / `DrawerArtQueue`（`ItemDrawerPartial_B114W12.cs`）逐条同构
        //     ⚠️ 代价（如实记）：调用点的 `QArt` 距 `QBoard` 不足 2 时，**主图会上移 1~2 档**；
        //        已分开 ≥2 的调用点**逐值不变**（`Mathf.Max` 取原值）。

        /// <summary>`Campaign Glow` 的渲染队列 = 底板之上、主图之下（原版 sibling 序 `Background < Campaign Glow < Image`）。</summary>
        internal static int CampaignPointsGlowQueue(ItemDrawerStyle st) { return st.QBoard + 1; }

        /// <summary>**本抽屉**主图的渲染队列：恒高于 <see cref="CampaignPointsGlowQueue"/>（见上面那段「为什么」）。</summary>
        internal static int CampaignPointsArtQueue(ItemDrawerStyle st) { return Mathf.Max(st.QArt, st.QBoard + 2); }

        // ============================================================ 画这一层
        /// <summary>基础档（`Campaign Points Drawer`）：把 `Campaign Glow` 画在抽屉根下（= `Content` 那一层的屏幕位置，
        /// 见文件头「坐标口径」）。<para>`false` = 没画成（图取不到，那时已 `Note` 出声）。</para></summary>
        internal static bool DrawCampaignPointsGlowBase(Transform node, PxRect box, ItemDrawerStyle st)
        {
            return DrawCampaignPointsGlow(node, FrameOf(box, fit: RootFit.HeightControlsWidth), st,
                                          CpGlowBaseX1, CpGlowBaseY1, CpGlowBaseX2, CpGlowBaseY2);   // 基础档 `Campaign Points Drawer` 是 ARF `m_AspectMode 2`（0.66）（B-260）
        }

        /// <summary>Icon 档（`Icon Campaign Points Drawer Variant`）：根 1080² ⇒ 框是 1620² 的过扫（绕中心 ×1.5）。
        /// <para>`false` = 没画成（图取不到，那时已 `Note` 出声）。</para></summary>
        internal static bool DrawCampaignPointsGlowIcon(Transform node, PxRect box, ItemDrawerStyle st)
        {
            return DrawCampaignPointsGlow(node, FrameOf(box, DrIconRoot, DrIconRoot), st,
                                          CpGlowIconX1, CpGlowIconY1, CpGlowIconX2, CpGlowIconY2);
        }

        /// <summary>两档共用的那一句建层（`m_PreserveAspect = 1` ⇒ `keepAspect: true`）。</summary>
        static bool DrawCampaignPointsGlow(Transform node, DrawerFrame fr, ItemDrawerStyle st,
                                           float x1, float y1, float x2, float y2)
        {
            if (node == null) return false;
            var tex = Tex(PremiumGlowSprite);
            if (tex == null)
            {
                Note("campaignPointsGlow|" + PremiumGlowSprite,
                     "战役点抽屉的 `Campaign Glow` 图 `" + PremiumGlowSprite + "` 在 `Resources/Art/ui_menu/` 里取不到"
                     + " ⇒ 这一层**不画**（⛔ 不拿别的图顶替；导入口 = `工具/import_original_art.py` 的 `MENU_IMAGES`）");
                return false;
            }
            return MenuDraw.Rect(node, tex, fr.Map(x1, y1, x2, y2), NodeCampaignGlow,
                                 CampaignPointsGlowQueue(st), null, true, st.Clip, st.ClipSoftness) != null;
        }
    }
}
