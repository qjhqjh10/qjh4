// ItemDrawerPartial_B111R3W.cs — 第 111 会话第 1 波执行代理 `B111_R3W`（2026-11-01）· 账 N138 的 ①（Icon 铺满档）
//
// 判据：`判据/B106_奖励预览链_施工单.md §三 A2` —— 原版 Icon 抽屉的主图是「**铺满 + PA=1**」：
//   本次现读 `d:/2/tools/assetripper_out/ExportedProject/Assets/` 的 Icon 抽屉 prefab（Title / Currency / Random Card 三张），
//   drawer 主图节点 `Image` = `m_AnchorMin (0,0)` · `m_AnchorMax (1,1)` · `m_SizeDelta (0,0)`（整框拉伸）· `m_PreserveAspect 1`（PA=1）。
//   我们的 `Icon(...)` 抽屉原来画 `Square(box, st.IconFill)`（`min(W,H)*k` 居中方形）⇒ 对非方框**不是铺满**。
//
// 本文件只做**加法**（默认行为逐字节不变）：
//   · `ItemDrawerStyle.IconFillBox`（默认 false）。**false ⇒ 走原来的 `Square(box, IconFill)`**（既有 100+ 调用点不受影响）；
//     true ⇒ 主图 = 整个 `box`（= 原版那套拉伸铺满，配合 `MenuDraw.Rect(..., keepAspect: true)` = PA=1）。
//   · `IconRect(box, st)`：`Icon` / `WildcardIcon` / `Placeholder` 三处 `Square(box, st.IconFill)` 改调它（见 ItemDrawer.cs 的那 3 行）。
//   · `DrawIconFill(...)`：给两扇日奖励窗用的便捷口 —— 直接以「Icon 档 + 铺满 + PA=1 + 不画数量/名字」建一格
//     （= 原版 `ItemDrawer.Draw(rewardDrawerHolder, item, qty, Icon)` 的结构等价）。
// ⚠️ 本类是 `partial`（`ItemDrawerStyle` / `ItemDrawer` 在主文件里就是 partial）⇒ 新成员全在这里，**不推主文件任何账落点**。
using UnityEngine;

namespace CardPresentation
{
    public partial struct ItemDrawerStyle
    {
        /// <summary>主图几何档：**false = 居中方形**（`min(box)×IconFill`，既有行为）· **true = 铺满整个 `box`**
        /// （= 原版 Icon 抽屉变体的 `Image` 锚 `(0,0)-(1,1)` + `sizeDelta 0`，见文件头）。默认 false。
        /// <para>🔴 2026-11-01（`B111_R3W` · 账 N138）：新增档，**默认 false ⇒ 既有调用点逐字节不变**。</para></summary>
        public bool IconFillBox;
    }

    public static partial class ItemDrawer
    {
        /// <summary>主图矩形：`IconFillBox` 为真 ⇒ 整个 `box`（铺满）；否则 = 原来的 `Square(box, IconFill)`（居中方形）。
        /// <para>⛔ 三处 `Icon` / `WildcardIcon` / `Placeholder` 必须都走这里 —— 别在两处各写一份分支（CLAUDE.md §三）。</para></summary>
        static PxRect IconRect(PxRect box, ItemDrawerStyle st)
        {
            return st.IconFillBox ? box : Square(box, st.IconFill);
        }

        /// <summary>= 原版 `ItemDrawer.Draw(rewardDrawerHolder, item, qty, Icon)` 的结构等价：**Icon 档 + 铺满 + PA=1 + 不画数量/名字**。
        /// <para>给两扇日奖励窗（`DailyRewardPopup` / `DailyStreakPopup`）的奖格用：它们的主图原先是裸 `MenuDraw.Rect(..., keepAspect: true)`，
        /// 换成本口之后抽屉结构（`Item Drawer` 根 → `Icon` 图）与原版对上，而**几何不变**（icon 仍在 `box` 里铺满 + PA=1）。</para>
        /// <para>`art` = 图名（走 `Tex` = `CardArt.MenuUi`，与两窗各自的 `Art()` 同一解析口）；`null`/空 ⇒ **不画**（返回 null，
        /// 由调用方照旧把缺图记进 `MissingArt` / 出声 —— ⛔ 别静默画个占位板）。</para>
        /// <para>`qArt` = 主图的渲染队列（两窗原先用的是 `QContent`）。数量/名字两档显式置 0 ⇒ 不建那两层
        /// （日奖励那格自己画名字、也不显示数量）。</para></summary>
        public static ItemDrawerView DrawIconFill(Transform parent, PxRect box, string art, string label, int qArt,
                                                  PxRect? clip = null, Vector2 clipSoftness = default(Vector2))
        {
            if (string.IsNullOrEmpty(art)) return null;
            // 明确按 `Generic`（有图、未知原版子类型）建 —— 走 `Icon` 档抽屉（`PickDrawer` 的 `case ItemKind.Generic`）。
            var spec = new ItemSpec { Id = "ItemReward", Kind = ItemKind.Generic, Art = art, Label = label };
            var st = ItemDrawerStyle.Default(qArt, qArt, qArt + 1);
            st.IconFill = 1f; st.IconFillBox = true; st.QuantityPx = 0f; st.NamePx = 0f;
            st.Clip = clip; st.ClipSoftness = clipSoftness;   // 调用方原话：给它原来那条路的裁切（`null` = 沿父链找 `ViewportClip`，与原先裸 `MenuDraw.Rect(..., clip: null)` 同义）
            return DrawView(parent, box, spec, 1, DrawerOverride.Default, st);
        }

#if UNITY_EDITOR
        /// <summary>自检读口（⛔ 非原版件、只许自检调）：把私有 `IconRect` 暴露成只读，供 `Editor/B111_R3WB_Assert.cs` 断几何。
        /// <para>灭自证：谁把 `IconRect` 改回「永远 `Square(...)`」⇒ 铺满那一档的断言红。</para></summary>
        public static PxRect IconRectForTest(PxRect box, ItemDrawerStyle st) { return IconRect(box, st); }
#endif
    }
}
