// ============================================================================ 装饰品三档（`AvatarBorderDrawer` / `CardbackDrawer` / `CosmeticDrawer`）
//
//  判据（施工单 `判据/B100_D1_抽屉18档_施工单.md` 表 B 的 B2 / B3 / B4；方法体现读自
//  `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/{AvatarBorderDrawer,CardbackDrawer,CosmeticDrawer}.cs`）：
//    · `AvatarBorderDrawer.Draw(CosmeticItemAvatarBorder)` —— **全篇一句**：
//        `Image image = base.image; image.sprite = item.Image;`
//    · `CardbackDrawer.Draw(CosmeticItemCardback)` —— **全篇一句**：`base.image.sprite = item.CosmeticSprite`
//    · `CosmeticDrawer.Draw(CosmeticItem)` —— **全篇一句**：`base.image.sprite = item.CosmeticSprite`
//      （旁支：只被 `DeckAndCardbackDrawer.cardBackDrawer` 用；配置表不引用它，见施工单 B4）
//  三档都**只碰 `image` 一张图**，不碰阵营名/名字/数量（`Label` 由基类按 `options.showName` 写；
//  `AvatarBorderDrawer` 的 `quantity` 槽是 null ⇒ 原版永不画数量）。
//
//  ── 图从哪来（我们的等价物）──────────────────────────────────────────────────
//    · `CosmeticItem.Image` / `CosmeticItem.CosmeticSprite` 都是 SO 里的 `AssetReferenceTyped<Sprite>`
//      —— 我们的数据层把它压成**一个图名**（`ItemSpec.Art`）。取图**按档分目录**（与 `CardArt` 的三个取口对齐）：
//        边框 ⇒ `CardArt.MenuUi`（`Player_Profile_Border` 在 `Resources/Art/ui/`；
//                          `EquippedBorderArtName` 走的是同一条，见 `Shell/AvatarTab.cs:664-669`）
//        卡背 ⇒ `CardArt.Cosmetic`（`Resources/Art/cardbacks/`，**名字含空格、别 slug 化**）
//    · 取不到 ⇒ **出声 + 不画**（红线：⛔ 不许静默、⛔ 不拿别的图顶替）。
//
//  ── 版面（🔴 2026-10-30 `B101_R8` 就地订正，铁律 5：原来写「原版靠裁切/旋转吃掉溢出 + 根自带 -7.27°」，两条都不成立）
//    ① 两档的 `image` 在 prefab 里都是**过扫**，而且**没有任何 mask 去裁它**（三份 prefab `grep Mask/RectMask2D` = 0）：
//       `avatar` = 860.21×730.48（`avatarBG` 只有 431.19×479；anchors `-0.475..1.52 × -0.175..1.35` = 真过扫）；
//       `Cardback` 的 `Image` = 818.80×1242.00（`Content` = 712×1080；anchors `-0.075..1.075` = 1.15 倍居中过扫）。
//       ⇒ **照过扫矩形画**（`keepAspect`）；溢出是原版行为，⛔ 别缩回可见框。
//    ⚠️ **`-7.27°` 不是 `Cardback Drawer` 的属性**：那份 prefab 的根 `m_LocalRotation` = identity；该角度出现在
//       `General Basic Offer Popup …` 的**实例**上（= `BaseOfferPopup.VariantDrawer.Rot` 那一格）⇒ 由调用点负责。
//    ② 头像框 `avatarBG` 那件（原版 `Player Profile Border`）是 **prefab 自带的第二层**，
//       `Draw` **不碰它**（施工单 B1「还差什么」②）⇒ 我们也不建。
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        /// <summary>原版 `AvatarBorderDrawer : ItemDrawer&lt;CosmeticItemAvatarBorder&gt;`。</summary>
        public const string DrawerAvatarBorder = "AvatarBorderDrawer";
        /// <summary>原版 `CardbackDrawer : ItemDrawer&lt;CosmeticItemCardback&gt;`。</summary>
        public const string DrawerCardback = "CardbackDrawer";
        /// <summary>原版 `CosmeticDrawer : ItemDrawer&lt;CosmeticItem&gt;`（旁支，见文件头）。</summary>
        public const string DrawerCosmetic = "CosmeticDrawer";

        /// <summary>`avatarBG` 那一格的矩形（**设计 px**，prefab 实读 `Avatar Drawer` / `Avatar Border Drawer` 两份相同）：
        /// `(139.8,233.3)-(571.0,712.3)`。见文件头 ①：`image` 的真实节点是它的子件 `avatar`（860×730 过扫），
        /// 我们按这一格画。</summary>
        static PxRect CosmeticAvatarFrame(DrawerFrame fr)
        {
            return fr.Map(139.8f, 233.3f, 571.0f, 712.3f);
        }

        /// <summary>🔴 **头像本体**那一格（`AvatarDrawer` 专用的 `image` 槽）—— **不是** `avatarBG`。
        /// <para>判据（`Avatar Drawer.prefab` 直读）：`ItemDrawerComponents.image` 指向的子件是 **`avatar`**，
        /// 它是 `avatarBG` 的子、anchors `-0.475..1.52 × -0.175..1.35`、`sizeDelta ≈ 0`
        /// ⇒ 设计 px **(−65.0, 65.6)-(795.2, 796.1)** = **860.21 × 730.48**（比 `avatarBG` 宽 1.995× / 高 1.525×）。
        /// 整棵 prefab **没有 `Mask`/`RectMask2D`** ⇒ 这层过扫**不被裁**，是原版本来的样子。
        /// ⚠️ `Avatar Border Drawer` 的 `image` 槽是 **`avatarBG`**（不是 `avatar`）⇒ 那一档**继续用
        /// <see cref="CosmeticAvatarFrame"/>**，⛔ 别两档一起改。</para></summary>
        static PxRect AvatarImageFrame(DrawerFrame fr)
        {
            return fr.Map(-65.0f, 65.6f, 795.2f, 796.1f);
        }

        /// <summary>原版 `AvatarBorderDrawer.Draw`（一句话）。图 = `CosmeticItem.Image`。</summary>
        static void AvatarBorder(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                                 ref ItemDrawResult res)
        {
            var fr = FrameOf(box);
            var content = DrawerContent(node, fr);
            DrawerBackground(content, fr, st);
            var tex = CardArt.MenuUi(item.Art);        // 边框图（`Player_Profile_Border` 一族）
            if (!DrawerImage(content, CosmeticAvatarFrame(fr), tex, item.Art, st, ref res))
                Note("avatarBorder|art|" + (item.Id ?? "?"),
                     "头像边框 `" + (item.Id ?? "?") + "` 的图名 `" + (item.Art ?? "<空>") + "` 在 `Resources/` 里取不到"
                     + "（`CardArt.MenuUi` 三级全空）⇒ 这一层**不画**（⛔ 不拿别的图顶替）");
            DrawerNameQuantity(content, fr, item, qty, st, hasName: true, hasQuantity: false);
        }

        /// <summary>原版 `CardbackDrawer.Draw`（一句话）。图 = `CosmeticItem.CosmeticSprite`。
        /// ⚠️ 这一档的 `Label` 在 prefab 里整组 `INACT`（`⛔GRP-off`）⇒ 原版**不画名字、也不画数量**
        /// ⇒ 我们两格都关（`hasName: false, hasQuantity: false`）。</summary>
        static void Cardback(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                             ref ItemDrawResult res)
        {
            DrawCosmeticOneImage(node, box, item, qty, st, ref res, CardArt.Cosmetic,
                                 "cardback", "卡背");
        }

        /// <summary>原版 `CosmeticDrawer.Draw`（一句话，旁支）。同上（`CosmeticItem.CosmeticSprite`）。</summary>
        static void Cosmetic(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                             ref ItemDrawResult res)
        {
            DrawCosmeticOneImage(node, box, item, qty, st, ref res, CardArt.Cosmetic,
                                 "cosmetic", "装饰品");
        }

        /// <summary>`Cardback Drawer` 的 `Content/Image` 那一格 —— **1.15 倍居中等比过扫**。
        /// <para>判据（prefab 直读）：`Image` 的 anchors 是 **`-0.075..1.075 × -0.075..1.075`**、`sizeDelta ≈ 0`
        /// ⇒ 相对 `Content`（712×1080）= **(−53.4, −81.0)-(765.4, 1161.0)** = 818.8 × 1242，中心不变。
        /// 整棵 prefab 无 `Mask`/`RectMask2D` ⇒ 溢出不被裁（原版就是这样）。</para></summary>
        static PxRect CardbackOverdrawFrame(DrawerFrame fr)
        {
            return fr.Map(-53.4f, -81.0f, 765.4f, 1161.0f);
        }

        /// <summary>`CardbackDrawer` / `CosmeticDrawer` 共用的那一句（两档的方法体**逐字相同**：
        /// `base.image.sprite = item.CosmeticSprite`；差别只有取图口 —— 两者都走 `cardbacks/`）。</summary>
        static void DrawCosmeticOneImage(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                                         ref ItemDrawResult res, System.Func<string, Texture2D> load,
                                         string noteKey, string human)
        {
            var fr = FrameOf(box);
            var content = DrawerContent(node, fr);
            // 🔴 **不画底图**（2026-10-30 `B101_R8` 现读）：`Cardback Drawer.prefab` 的
            //    `ItemDrawerComponents.background` = `{fileID: 0}`（null），`Content` 下也**没有 `Background` 节点**
            //    ⇒ 原版卡背/装饰品抽屉**没有那一层**（原来这里多画了一条 `40k_Rewards_icon_base card`）。
            var tex = load(item.Art);
            // 卡背那张 `Image` 是 **1.15 倍居中等比过扫**（anchors `-0.075..1.075` ⇒ 818.8×1242，`Content` = 712×1080）
            if (!DrawerImage(content, CardbackOverdrawFrame(fr), tex, item.Art, st, ref res))
                Note(noteKey + "|art|" + (item.Id ?? "?"),
                     human + " `" + (item.Id ?? "?") + "` 的图名 `" + (item.Art ?? "<空>") + "` 取不到"
                     + "（`CardArt.Cosmetic` 的 `Resources/Art/cardbacks/`）⇒ 这一层**不画**（⛔ 不拿别的图顶替）");
            DrawerNameQuantity(content, fr, item, qty, st, hasName: false, hasQuantity: false);   // 见 `Cardback` 的 doc
        }
    }
}
