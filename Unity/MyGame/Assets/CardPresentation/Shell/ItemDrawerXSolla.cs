// ============================================================================ XSolla 礼包抽屉（原版 `XSollaOfferDrawer`）
//
//  判据（施工单 B19；方法体 = `cpp2il/out_cpp2il_dev_ilspy/XSollaOfferDrawer.cs`）—— **全文三句**：
//    `ItemDrawerComponents c = Components;`
//    `c.convertedItem.gameObject.SetActive(false);`                        ← ① 关掉「折算价」那一格
//    `c.conversionDisplay.SetActive(true);`                                ← ② 打开「折算条」
//    `c.convertedLabel.text = LocalizationManager.GetTranslation("MainMenu/RewardWindow/SentToInbox");`  ← ③
//  ⇒ 它画的是「**已发到收件箱**」那条状态，不是商品图。
//    我们的等价物：`convertedItem`（价格那一行）**根本不建**（= 原版关掉它）；`conversionDisplay` = `Converted Drawer`
//    容器（`NodeConvertedDrawer`）；`convertedLabel` = 容器下面的 `AlreadyOwned` 那颗 TMP
//    （prefab 实读：出厂文本就是 `Sent to your inbox` ⇒ 与 ③ 那条词条互证）。
//    词条 `MainMenu/RewardWindow/SentToInbox` 本机**有**（`Resources/official_loc_13lang.json` 的 keys 里命中；
//    英文 = `Sent to your inbox`）⇒ 走 `Loc.T`（缺键会出声并回键名，不静默）。
//  ⚠️ 这档另有 `public override void SetConvertedItem(RewardInfo convertedTo) { }` —— **空覆写**
//    ⇒ 原版对 XSolla 这一档，「折算」那条调用被吞掉（我们的 `SetConverted` 是调用方按状态调的，
//      这一档同样不会被调 —— 语义相同）。
//
//  ── 版面（prefab 实读 · `XSolla Offer Drawer`）────────────────────────────────
//    根 712.8×1080 · `Content/Image` **(0.4,162)-(712.4,810)**（比别档高 216 —— 这一档主图占比更大）·
//    `Content/Label`（`Quantity` 出厂 INACT + `Name`）· `Content/Converted Drawer` **(102.9,947.3)-(609.9,1032.7)**
//    （**出厂就是激活的**，与其它档的 `INACT` 相反）· 其下 `AlreadyOwned` **(102.9,936.0)-(609.9,1044.0)**
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        /// <summary>原版 `XSollaOfferDrawer : ItemDrawer&lt;XSollaBundleItem&gt;`。</summary>
        public const string DrawerXSolla = "XSollaOfferDrawer";

        /// <summary>原版第 ③ 句那个词条 key（`LocalizationManager.GetTranslation(...)` 的字面量）。</summary>
        const string XSollaSentTerm = "MainMenu/RewardWindow/SentToInbox";

        /// <summary>原版 `XSollaOfferDrawer.Draw`（三句）。</summary>
        static void XSollaOffer(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                                ref ItemDrawResult res)
        {
            var fr = FrameOf(box);
            var content = DrawerContent(node, fr);
            DrawerBackground(content, fr, st);

            // 主图（`Components.image` 由基类那一步画；XSolla 的 `Draw` 自己不碰它）
            var tex = Tex(item.Art);
            if (!DrawerImage(content, fr.Map(DrCx1, 162f, DrCx2, 810f), tex, item.Art, st, ref res))
                Note("xsolla|art|" + (item.Id ?? "?"),
                     "XSolla 礼包 `" + (item.Id ?? "?") + "` 的图名 `" + (item.Art ?? "<空>") + "` 取不到"
                     + "（这一档的 `Draw` 只写折算条那三句，主图靠基类 `image`）⇒ 这一层不画");

            DrawerNameQuantity(content, fr, item, qty, st, hasName: true, hasQuantity: true);

            // ② `conversionDisplay.SetActive(true)` + ③ `convertedLabel.text = I2(...)`
            //   （① `convertedItem.SetActive(false)` = 我们不建那一行 ⇒ 等价）
            var conv = MenuDraw.Node(content, NodeConvertedDrawer, fr.Map(102.9f, 947.3f, 609.9f, 1032.7f));
            var r = fr.Map(102.9f, 936f, 609.9f, 1044f);
            ClippedText(conv, r, Loc.T(XSollaSentTerm), AlreadyOwnedInk, NodeAlreadyOwned,
                        75f * fr.K, st.QDecor + 5, r.W, st, 0, 12f * fr.K, 75f * fr.K, 36f * fr.K, true,
                        TmpFont.Face.LatinCardBody);
        }
    }
}
