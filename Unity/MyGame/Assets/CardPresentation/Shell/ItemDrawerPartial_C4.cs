// ============================================================================ C4 partial（2026-10-31 · 第一百零九会话）
//
//  本文件的唯一一件：**`Random Card Drawer ▸ Content/Random Image/Icon` 的 `m_RaycastTarget = 1`**。
//
//  ── 判据（原版）───────────────────────────────────────────────────────────────
//   · `Random Card Drawer.prefab ▸ Content/Random Image/Icon` 的 `Image`：
//     Simple · `m_PreserveAspect 1` · `m_Color` 白 · **`m_RaycastTarget 1`**
//     （2026-10-31 `B109_E18` 现跑 `工具/menu_dump.py bundle_menus_assets_all "Random Card Drawer" --depth 6` 实读；
//      同一份读数见 `资料/完全复刻/报告/B109_E18.md` §一）。
//   · 但**父件 `Random Image` 自己也是一颗会吃射线的 `Image`**（同一份 prefab 读数：它是
//     `UI_Button_Round_background` 那张圆底），而 `Icon` 的框
//     `(142.8,216)-(570.0,540)` **完全落在** 整格的框里 ⇒ 这颗问号**加不加射线，可点区并集不变**
//     （并集判据 → `Shell/MenuDraw.cs` 的 `Union(...)` doc / `Shell/PointerLayer.cs` 的 `UnionOtherQuads`）。
//     所以这一档**只是把原版的射线位补上**，**⛔ 不新增一颗能点的钮**（新增 = 点问号什么都发生，
//     而原版点问号会冒泡到抽屉根、走 `OnPointerClick`）。
//
//  ── 我们怎么表达「吃射线」────────────────────────────────────────────────────
//   本仓这棵树**没有 `Graphic.m_RaycastTarget` 这个位**（`ImageQuad` 是纯渲染件）。`B106_W8` 定下的
//   等价物 = 「一颗钮的命中区 = 它子树里**每一颗在激活链上的 `ImageQuad`** 的外接盒」（`UnionOtherQuads`，
//   全工程唯一实现）。⇒ 补这个位 = **在同一格那颗 `Hit` 节点的子树里**建一颗**透明** `ImageQuad`
//   （与 `MenuDraw.MakeHitQuad` 同一建法：`CardArt.Solid()` + tint `(0,0,0,0)`）。
//   · 放在 `Hit` 节点下（⛔ 不是 `Content` 下）：`PointerLayer` 只扫**钮的子树**；
//   · 矩形取 `Icon` 的**布局框**（不是 `keepAspect` 之后画出来那块）：原版 uGUI 射线读的是
//     `rectTransform` 的 rect（`GraphicRaycaster.cs:327`），**与 `preserveAspect` 无关**；
//   · 裁切用 `_style.Clip`（原版 `RectMask2D` 同样会挡射线）⇒ 与那一格的 `Hit` 吃**同一份**裁切，
//     ⇒ **icon∩clip ⊆ box∩clip**，并集恒不变。
//
//  ⚠️ 只在**这一格可点**（`ShowInfoOnClick` 为真、`ItemDrawerView.AttachHit` 建出了 `Hit` 节点）时才建：
//     `RandomCardDrawer` 的 `showItemInfoOnClick` 是**混合档**（45:7），原版由调用方逐实例给；
//     我们这一棵树里「不可点 = 连命中层都没有」⇒ 那时也没有可挂靠的钮（如实记在报告里，⛔ 不为此另造一颗钮）。
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        /// <summary>`Content/Random Image/Icon` 的**布局框**（设计 px · 左上原点 · y 向下）——
        /// 与 `RandomCard` 画它时用的 `fr.Map(RandomIconX1..)` **同一条算式**（`FrameOf(_box)`）。
        /// <para>⚠️ 这是**布局框**（427.2×324），不是 `keepAspect` 之后画出来那块（171.69×324）——
        /// 原版射线读 `rectTransform`（判据见本文件头）。</para>
        /// <para>🔴 `public` 是给**自检**读的（`Editor/` 在另一个程序集，`internal` 那边看不见 ——
        /// 判据 → `资料/已知的坑.md`「`internal` 在自检里用不了」）。生产侧唯一调用点 = 本文件的 `AttachC4IconRaycast`。</para></summary>
        public static PxRect RandomCardIconRayRect(PxRect box)
        {
            var fr = FrameOf(box, fit: RootFit.HeightControlsWidth);   // 与 `RandomCard` 的 `fr` 同口径（`Random Card Drawer` ARF 2 · 0.66）（B-260）
            return fr.Map(RandomIconX1, RandomIconY1, RandomIconX2, RandomIconY2);
        }
    }

    public sealed partial class ItemDrawerView
    {
        /// <summary>`Content/Random Image/Icon` 的射线靶节点名（原版那颗 `Icon` 的等价物；
        /// 自检按这个名字找它 —— ⛔ 别改成 `Hit`（那是整格那颗，已有主））。</summary>
        public const string NodeIconRaycast = "Icon Raycast";

        /// <summary>补建 `Random Card Drawer` 的问号射线靶（见文件头）。幂等：已经建过就什么都不做。</summary>
        internal void AttachC4IconRaycast()
        {
            if (Drawer != ItemDrawer.DrawerRandomCard) return;
            if (Node == null || HitNode == null) return;              // 不可点那一档：没有可挂靠的钮（见文件头）
            // 🔴 原版这颗只在 `cardArmy == Neutral` 时可见：它是 `Random Image` 的子件，而父件的
            //    `SetActive(item.cardArmy == CardArmy.Neutral)` 那句只在那一档为真（`RandomCardDrawer__Draw.c:37`）
            //    ⇒ 非中立档整件关着 ⇒ 子件**不吃射线**（UGUI：不在激活链上就不进射线表）。
            if (Spec.CardArmy != 0) return;
            if (HitNode.Find(NodeIconRaycast) != null) return;        // `Refresh()` / 重复 `AttachHit` 都会再走到这里

            var r = ItemDrawer.RandomCardIconRayRect(_box);
            // 完全落在裁切框外 ⇒ `MenuDraw.Rect` 返 null。那不是失败：这一颗此刻本来就看不见，
            // 原版 uGUI 也吃不到射线（`RectMask2D` 会挡住）⇒ 不打警告（⛔ 别把「看不见」写成「缺件」）。
            MenuDraw.Rect(HitNode, CardArt.Solid(), r, NodeIconRaycast, _style.QDecor + 6,
                          new Color(0f, 0f, 0f, 0f), false, _style.Clip);
        }
    }
}
