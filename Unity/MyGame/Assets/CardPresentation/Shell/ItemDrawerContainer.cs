// ============================================================================ 商品容器抽屉（原版 `ContainerDrawer`）
//
//  判据（施工单 B5；方法体 = `cpp2il/out_cpp2il_dev_ilspy/ContainerDrawer.cs`）：
//    `protected override void Draw(ShopContainerBase item, int quantity = 1)`
//      `{ Image image = base.image; image.sprite = item.PreviewImage.Load();`
//      `  if (armyIcon != null) {`
//      `      armyIcon.gameObject.SetActive(item.cardArmy != CardArmy.Neutral);`     ← 只看「是不是中立」
//      `      armyIcon.sprite = SingletonBehaviour<ArmyUtilities>.Instance.GetArmyIcon(item.cardArmy); } }`
//  ⇒ 两件：① 主图 = `PreviewImage`（`AssetReferenceTyped<Sprite>`，**抽象虚拟属性**：真值在具体子类
//     `ShopContainer` / `DlcBundle` 上，我们这边数据层已把它压成一个图名 = `ItemSpec.Art`；
//     卡包那例的图名实读 = `40K_shop_offer_booster_UM`，见 `Shell/ItemDrawer.cs` 文件头）
//     ② 阵营徽记 = **中立才关**（不是「认不出才关」—— 与原版逐字相同）
//
//  ── 版面（prefab 实读 · `Container Drawer`）────────────────────────────────
//    根 712.8×1080（ARF 0.66）· `Content/Background` 铺满 · `Content/Image` **铺满 `Content`（712×1080）**
//    且 `m_LocalScale` 让**视觉框 = ×1.2 → 854.40×1296.00**（prefab 故意的「铺满 + 溢出」）·
//    `Content/Army Icon` **(0.4,939.6)-(712.4,1134)**（**落在 `Content` 下沿之外**，prefab 实读原样）·
//    `Content/Label` 有 `Quantity` + `Name` 两颗子件（fs 各 75/auto[12~75]）
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        /// <summary>原版 `ContainerDrawer : ItemDrawer&lt;ShopContainerBase&gt;`。</summary>
        public const string DrawerContainer = "ContainerDrawer";

        /// <summary>`Content/Image` 的视觉放大倍数（prefab 的 `m_LocalScale` 实读 ⇒ ×1.2）。</summary>
        const float ContainerImgScale = 1.2f;

        /// <summary>原版 `ContainerDrawer.Draw` 的等价实现。</summary>
        static void Container(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                              ref ItemDrawResult res)
        {
            var fr = FrameOf(box);
            var content = DrawerContent(node, fr);
            DrawerBackground(content, fr, st);

            // ① `image.sprite = item.PreviewImage.Load()` —— 铺满 `Content` 再 ×1.2（prefab 的视觉框）
            var tex = Tex(item.Art);
            var imgR = DrawerFrame.Scale(fr.Map(DrCx1, 0f, DrCx2, DrRootH), ContainerImgScale);
            if (!DrawerImage(content, imgR, tex, item.Art, st, ref res))
                Note("container|art|" + (item.Id ?? "?"),
                     "商品容器 `" + (item.Id ?? "?") + "` 的预览图 `" + (item.Art ?? "<空>") + "` 取不到"
                     + "（`ContainerDrawer` 画的是 `ShopContainerBase.PreviewImage`）⇒ 这一层**不画**"
                     + "（⛔ 不拿别的图顶替）");

            // ② 阵营徽记：**只有中立才关**（`armyIcon.SetActive(item.cardArmy != Neutral)`）
            if (item.CardArmy != 0)
            {
                var aR = fr.Map(DrCx1, 939.6f, DrCx2, 1134f);
                var aTex = ArmyIcons.Texture(item.CardArmy);
                if (aTex != null)
                    MenuDraw.Rect(content, aTex, aR, NodeArmyIcon, st.QArt + 2, null, true, st.Clip, st.ClipSoftness);
                else
                    Note("container|army|" + item.CardArmy,
                         "商品容器 `" + (item.Id ?? "?") + "` 的阵营 " + item.CardArmy + " 没有徽记图"
                         + "（`ArmyIcons.Texture`）⇒ 原版 `GetArmyIcon` 返 null、`armyIcon.sprite` 被赋空 ⇒ 这一层空着");
            }

            DrawerNameQuantity(content, fr, item, qty, st, hasName: true, hasQuantity: true);
        }
    }
}
