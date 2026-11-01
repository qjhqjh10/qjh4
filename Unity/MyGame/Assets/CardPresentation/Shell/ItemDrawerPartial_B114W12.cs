// ItemDrawerPartial_B114W12.cs — 第一百一十四会话 · 写手 `W-12`（2026-11-01）· 账 `B-243`
//
// ============================ 它补的是什么 ============================
//  `Expansion Pass Points` 这一族抽屉的 `Content` 下有一层**恒显**的静态件 `Campaign Glow`
//  （它**不在** `ItemDrawerComponents` 的任何一个槽里 —— `background` 也是 `{fileID:0}` ⇒
//   原版**不由代码开关**，`m_IsActive = 1` 就是出厂态）。我们**两档都没画**。
//
//  判据（prefab 现读 · `d:/2/tools/assetripper_out/ExportedProject/Assets/`）：
//
//  ① Icon 档 `Icon Expansion Pass Points Drawer Variant.prefab`
//     · `Content.m_Children[0]` = `Campaign Glow`（排在 `Image` **之前** ⇒ 画在它**下面**）、`m_IsActive = 1`
//     · `!u!224 &224161607090078389`：`anchorMin (0,0)` · `anchorMax (1,1)` ·
//       `m_SizeDelta (-360.3243,-360.3243)` · `m_LocalScale 1.4999999` · `m_Pivot (0.5,0.5)` ·
//       parent = `Content`（**1080×1080**）
//       ⇒ 框 = 180.16215..899.83785（719.6757²），绕中心 ×1.5
//       ⇒ **视觉 (0.243225,0.243225)-(1079.756775,1079.756775)**
//     · `!u!114 &114470112387431263`：`m_Sprite` guid `6c77cbd6b01d7c549952437eb07de765`
//       = `Sprite/40K_general_icon_Expansion Pass XP`（我们落盘名 `40K_general_icon_Expansion_Pass_XP`，
//       `Resources/Art/ui_menu/` 已有）· `m_PreserveAspect = 1` · `m_Type 0`(Simple) · `m_Color` 白
//
//  ② 基础档 `Expansion Pass Points Drawer.prefab`
//     🔴 **与 Icon 档不是同一张图、也不是同一组数**（施工单 `报告/B114_WC_B223_Icon几何.md` §顺手发现 2
//     只写了 Icon 那一组；本波回源核了基础档）：
//     · `Content.m_Children[1]` = `Campaign Glow`（在 `Background` **之后**、`Image` **之前**）、`m_IsActive = 1`
//     · `!u!224 &224371939378208658`：`anchorMin (0,0.4)` · `anchorMax (1,0.8)` ·
//       `m_SizeDelta (229.31067,139.08057)` · `m_AnchoredPosition (-0.00012207031,0)` ·
//       `m_LocalScale 1` · `m_Pivot (0.5,0.5)` · parent = `Content`（**712×1080**）
//       ⇒ 框 = **(-114.6555,146.4597)-(826.6552,717.5403)**（941.3107 × 571.0806，中心 (356,432)）
//     · `!u!114 &114320256170494724`：`m_Sprite` guid `d2dc932d3e8aec642a97a91df78b78c1`
//       = `40k_campaign_Expansion Pass`（落盘名 `40k_campaign_Expansion_Pass`，源 sprite 126×126）·
//       `m_PreserveAspect = 1` · `m_Type 0`(Simple) · `m_Color` 白
//
//  ── 坐标口径 ────────────────────────────────────────────────────────────────
//    上面两组框都是「**相对 `Content` 左上角**的设计 px · 左上原点 · y 向下」（prefab 是 uGUI 的
//    左下原点 y 向上 ⇒ 已按 `1080 − y` 翻过）。Icon 档的 `Content` 左沿在设计 px `0`
//    （`DrawerIconContent` = `fr.Map(0,0,1080,1080)`）；基础档的 `Content` 左沿在 `DrCx1`（= 0.4）
//    （`DrawerContent` = `fr.Map(DrCx1,0,DrCx2,DrRootH)`）⇒ 基础档那组要**加上 `DrCx1`** 再 `fr.Map`。
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        // ============================================================ 常量（照 prefab 逐字）
        //  ⚠️ 节点名 `Campaign Glow` 的常量**已经存在**（`ItemDrawerPremium.cs:43` 的 `NodeCampaignGlow`，
        //     那一族的 `background` 槽正好也叫这个名字 —— 同一个字符串、两份不同 prefab）⇒ 本文件**复用**它，
        //     ⛔ 别在此再声明一份（`ItemDrawer` 是同一个 partial 类 ⇒ 会 `CS0102`）。
        /// <summary>Icon 档那张（sprite guid `6c77cbd6b01d7c549952437eb07de765`；落盘名）。</summary>
        public const string ArtCampaignGlowIcon = "40K_general_icon_Expansion_Pass_XP";
        /// <summary>基础档那张（sprite guid `d2dc932d3e8aec642a97a91df78b78c1`；落盘名）。⚠️ **与 Icon 档不是同一张**。</summary>
        public const string ArtCampaignGlowBase = "40k_campaign_Expansion_Pass";

        // ---- 视觉框（设计 px · 左上原点 · y 向下 · **相对 `Content` 左上角**；出处见文件头）----
        /// <summary>Icon 档 `Campaign Glow` 的视觉框（框 180.16215..899.83785 绕中心 ×1.5）。</summary>
        const float GlowIconX1 = 0.243225f, GlowIconY1 = 0.243225f;
        const float GlowIconX2 = 1079.756775f, GlowIconY2 = 1079.756775f;
        /// <summary>基础档 `Campaign Glow` 的视觉框（锚 `(0,0.4)-(1,0.8)` of 712×1080 + sizeDelta
        /// `(229.31067,139.08057)` ⇒ 941.3107 × 571.0806，中心 (356,432)；`m_LocalScale = 1`）。</summary>
        const float GlowBaseX1 = -114.655457f, GlowBaseY1 = 146.459715f;
        const float GlowBaseX2 = 826.655212f, GlowBaseY2 = 717.540285f;

        // ============================================================ 渲染队列（这一层为什么要单独一档）
        //
        //  原版靠 **sibling 序**（`Content.m_Children` = [Campaign Glow, Image] / [Background, Campaign Glow, Image]）
        //  表达「后面的压前面的」。本项目定死了「**分层用渲染队列，不用 z**」（`CLAUDE.md` §三）⇒
        //  必须把这三种次序翻译成**三个互不相同**的队列号，否则同号的透明件按「到相机的距离」排序，前后不可靠。
        //
        //  🔴 为什么不能复用 `QBoard` / `QArt`：好几个调用点传的是 `ItemDrawerStyle.Default(q, q, q)`
        //     （`CampaignTab.cs:696` 节点奖励 = `QNodeItem` 三个同号 · `ExpansionPassEventWindow.cs:2444`
        //     = `QCardReward, QCardReward, QCardText`）⇒ `QBoard == QArt`，**这两档正是本站点**。
        //     复用它们的话，光圈与底板/主图同号 ⇒ 那块近不透明的金属环会随机压住主图（实测过同号排序不可靠）。
        //
        //  🔶 自己设计（非原版）：`CampaignGlowQueue = QBoard + 1` · `DrawerArtQueue = max(QArt, QBoard + 2)`
        //     · 为什么必须自己定：原版用 sibling 序，我们的引擎没有 uGUI 运行时、只能靠 renderQueue 表达次序，
        //       而「在两个已有号之间插一层」需要空档 —— 调用点给的号常常是**连号** ⇒ 只能把主图往上让一档
        //     · 谁定的 + 日期：调度台 2026-11-01 指派（`W-12` / 账 `B-243`）落地
        //     ⚠️ 代价（如实记）：调用点的 `QArt` 距 `QBoard` 不足 2 时，**主图会上移 1~2 档**；
        //        已分开 ≥2 的调用点**逐值不变**（`Mathf.Max` 取原值）。

        /// <summary>`Campaign Glow` 的渲染队列 = 底板之上、主图之下（原版 sibling 序 `Background < Campaign Glow < Image`）。</summary>
        internal static int CampaignGlowQueue(ItemDrawerStyle st) { return st.QBoard + 1; }

        /// <summary>**本抽屉**主图的渲染队列：恒高于 <see cref="CampaignGlowQueue"/>（见上面那段「为什么」）。</summary>
        internal static int DrawerArtQueue(ItemDrawerStyle st) { return Mathf.Max(st.QArt, st.QBoard + 2); }

        // ============================================================ 画这一层
        /// <summary>Icon 档（`Icon Expansion Pass Points Drawer Variant`）：在调用方刚建好的 `Content` 下补 `Campaign Glow`。
        /// <para>⚠️ 必须在主图**之前**调（语义上是「先画下面那层」）；实际次序由队列号保证，与建树先后无关。</para></summary>
        internal static bool DrawCampaignGlowIcon(Transform content, DrawerFrame fr, ItemDrawerStyle st)
        {
            return DrawExpPassGlow(content, fr, st, ArtCampaignGlowIcon,
                                    GlowIconX1, GlowIconY1, GlowIconX2, GlowIconY2);
        }

        /// <summary>基础档（`Expansion Pass Points Drawer`）：`Content` 已由 `PointsForArmy` 建好 ⇒ 从抽屉根找回来再补这一层。
        /// <para>`null` 返回 = 没画成（`Content` 不在 —— 走了占位板那一支；或图取不到，那时已 `Note` 出声）。</para>
        /// <para>⚠️ 调用点必须**先用 <see cref="DrawerArtQueue"/> 抬过 `st.QArt`** 再画主图，否则主图与光圈同号
        /// （`Default(q,q,q)` 那类调用点就是这样）。</para></summary>
        internal static bool DrawCampaignGlowBase(Transform drawerRoot, PxRect box, ItemDrawerStyle st)
        {
            if (drawerRoot == null) return false;
            var content = drawerRoot.Find(NodeContent);   // ⚠️ `Transform.Find` 只找**直接子件**；`DrawerContent` 建的就是直接子件
            if (content == null) return false;            // 没有 `Content`（占位板那一支）⇒ 没有这层可画
            var fr = FrameOf(box, fit: RootFit.HeightControlsWidth);   // 与 `PointsForArmy` 同一套（默认根 712.8×1080）· `Expansion Pass Points Drawer` 是 ARF 2（B-260）
            return DrawExpPassGlow(content, fr, st, ArtCampaignGlowBase,
                                    DrCx1 + GlowBaseX1, GlowBaseY1, DrCx1 + GlowBaseX2, GlowBaseY2);
        }

        /// <summary>两档共用的那一句建层（`m_PreserveAspect = 1` ⇒ `keepAspect: true`）。</summary>
        static bool DrawExpPassGlow(Transform content, DrawerFrame fr, ItemDrawerStyle st, string art,
                                     float x1, float y1, float x2, float y2)
        {
            if (content == null) return false;
            var tex = Tex(art);
            if (tex == null)
            {
                Note("campaignGlow|" + art,
                     "扩张通行证点数抽屉的 `Campaign Glow` 图 `" + art + "` 在 `Resources/Art/ui_menu/` 里取不到"
                     + " ⇒ 这一层**不画**（⛔ 不拿别的图顶替；导入口 = `工具/import_original_art.py` 的 `MENU_IMAGES`）");
                return false;
            }
            MenuDraw.Rect(content, tex, fr.Map(x1, y1, x2, y2), NodeCampaignGlow, CampaignGlowQueue(st),
                          null, true, st.Clip, st.ClipSoftness);
            return true;
        }
    }
}
