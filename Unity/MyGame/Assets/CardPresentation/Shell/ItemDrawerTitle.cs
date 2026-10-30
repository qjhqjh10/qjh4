// ============================================================================ 称号三档（`TitleDrawer` / `TitleIconDrawer` / `TitleDrawerHorizontal`）
//
//  判据（施工单 B9 / B10 / B11；方法体 = `cpp2il/out_cpp2il_dev_ilspy/{TitleDrawer,TitleIconDrawer,TitleDrawerHorizontal}.cs`）：
//    · `TitleDrawer.Draw(CosmeticItemTitle item, int quantity = 1)` 逐句：
//        `armyText.text      = GameStaticData.CardArmyToString(item.cardArmy);`
//        `base.image.sprite  = ArmyUtilities.GetArmyIcon(item.cardArmy);`
//        `additionalArmyIconForEffect.sprite = base.image.m_Sprite;`        ← 第二颗**同一张图**
//        `if (base.image.m_Sprite == null) { image.gameObject.SetActive(false);
//                                            additionalArmyIconForEffect.gameObject.SetActive(false); }`
//        `title.text         = item.LocalizedName;`
//    · `TitleIconDrawer.Draw` —— **真空体**（ILSpy `TitleIconDrawer.cs:3-6` = `{ }`；`.c` 落在共享空桩）
//      ⇒ 这一档**一点逻辑都没有**，只有 prefab 出厂那张固定图（施工单 §〇「空体本身就是判据」）。
//    · `TitleDrawerHorizontal : TitleDrawer` —— **`Draw` 无覆写**（`ReDraw` 只是 `base.Draw`）；
//      额外只有 `ToggleLock` → `lockImage.SetActive`、`ToggleHighlight` → `highlight.SetActive`、以及
//      `button.m_OnClick`（ILSpy `:114-160`）⇒ 这三件由 `ItemDrawerView` 那三个「画完之后再调」的口承担
//      （`ToggleLock`→`ToggleClaimedWarning` / `ToggleHighlight`→`TogglePremiumHighlight`，见 `:1523-1526`）。
//
//  ── 版面（prefab 实读）──────────────────────────────────────────────────────
//    `Title Drawer`（根 174.18×263.92 · `Content` 被 `UIScaleToFit` ×0.2444 —— **设计空间仍是 712×1080**）：
//      `Content/Background` 铺满 · `Content/Image` **(191.5,76.5)-(520.5,422.3)**（`Image Top` 是它的子件、
//      出厂 alpha 0.729 的一层静态件，**不是 `Draw` 产物 ⇒ 不建**）·
//      `Content/Army` (0.4,702)-(712.4,810) fs75 · `Content/Title` **(64.7,432.1)-(648.2,680.5)** fs **27.05**
//      auto[15~80] 居中 折行 1 · `Content/Label/Name` (61,810)-(651.8,918) fs **35.1**（**没有 Quantity 子件**）
//      ⚠️ 括号里那四个数 = 原 dump 的 `⇲ls=0.2444` 行 ÷ 0.244367 还原到设计空间（逐项算过）。
//    `Icon Title Drawer Variant`（根 **1080×1080** · ARF 1）：`Content/Image` 铺满 1080²、**视觉 ×1.25**、
//      出厂 sprite = **`40K_Profile_icon_title`**（固定）⇒ 这一档只画那一张。
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        /// <summary>原版 `TitleDrawer : ItemDrawer&lt;CosmeticItemTitle&gt;`。</summary>
        public const string DrawerTitle = "TitleDrawer";
        /// <summary>原版 `TitleIconDrawer : ItemDrawer&lt;CosmeticItemTitle&gt;`（`Icon` 档；**空方法体**）。</summary>
        public const string DrawerTitleIcon = "TitleIconDrawer";
        /// <summary>原版 `TitleDrawerHorizontal : TitleDrawer`（`Horizontal`(15) 与 `OfferPopups`(30) 两档）。</summary>
        public const string DrawerTitleHorizontal = "TitleDrawerHorizontal";

        /// <summary>`Icon Title Drawer Variant` 那张出厂图（prefab 的 `Content/Image.m_Sprite` 实读）。</summary>
        const string TitleIconArt = "40K_Profile_icon_title";
        /// <summary>`Content/Image` 那一格（设计 px；Title 这一档与其它档**不同**，见文件头）。</summary>
        const float TitleImgX1 = 191.5f, TitleImgY1 = 76.5f, TitleImgX2 = 520.5f, TitleImgY2 = 422.3f;
        /// <summary>`Content/Title` 那一格（称号正文）。</summary>
        const float TitleTextX1 = 64.7f, TitleTextY1 = 432.1f, TitleTextX2 = 648.2f, TitleTextY2 = 680.5f;

        /// <summary>原版 `TitleDrawer.Draw` 的等价实现（`TitleDrawerHorizontal` **共用这一份** —— 原版就是继承）。
        /// <para>⚠️ 两条如实记：① 原版第 3 句把 `image` 的 sprite **抄给** `additionalArmyIconForEffect`
        /// （同一张图、叠一层）⇒ 我们按同一矩形再画一张（视觉相同；那一颗的真实矩形在 prefab 里没单独给，
        /// 它挂在 `image` 下面 ⇒ 按父矩形画）；② `armyText` 在 Icon 变体上是 null ⇒ 那一档**不画阵营名**
        /// （本函数只服务 `TitleDrawer` / `TitleDrawerHorizontal` 两份，两份都有 `armyText`）。</para></summary>
        static void Title(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                          ref ItemDrawResult res)
        {
            var fr = FrameOf(box);
            var content = DrawerContent(node, fr);
            DrawerBackground(content, fr, st);

            // `armyText.text = CardArmyToString(cardArmy)`
            DrawerArmyName(content, fr, item.CardArmy, st);

            // `base.image.sprite = GetArmyIcon(cardArmy)` + `additionalArmyIconForEffect.sprite = image.m_Sprite`
            var tex = ArmyIcons.Texture(item.CardArmy);
            if (tex == null)
                Note("title|army|" + item.Id,
                     "称号 `" + (item.Id ?? "?") + "` 的阵营 " + item.CardArmy + " 没有徽记图"
                     + "（`ArmyIcons.Texture`；`Neutral` 也没有 —— 原版 `GetArmyIcon(0)` 就是 null）"
                     + " ⇒ 原版把 `image` 与 `additionalArmyIconForEffect` **两层都关掉**，我们两层都不画");
            var imgR = fr.Map(TitleImgX1, TitleImgY1, TitleImgX2, TitleImgY2);
            if (DrawerImage(content, imgR, tex, ArmyIcons.IconName(item.CardArmy), st, ref res))
                MenuDraw.Rect(content, tex, imgR, NodeArmyIconExtra, st.QArt + 1, null, true, st.Clip, st.ClipSoftness);

            // `title.text = item.LocalizedName`
            //   我们的等价物 = `ItemSpec.Title`（没有就回落 `Label`）；空 ⇒ 不画（原版那颗 TMP 的出厂串是占位）。
            string title = string.IsNullOrEmpty(item.Title) ? item.Label : item.Title;
            if (st.NamePx > 0f && !string.IsNullOrEmpty(title))
            {
                var r = fr.Map(TitleTextX1, TitleTextY1, TitleTextX2, TitleTextY2);
                ClippedText(content, r, title, Color.white, NodeTitleText, 27.05f * fr.K, st.QText, r.W, st, 0,
                            15f * fr.K, 80f * fr.K, 36f * fr.K);
            }

            // `Label/Name`（fs 35.1 居中；没有 `Quantity` 子件 ⇒ 不画数量）
            if (st.NamePx > 0f && !string.IsNullOrEmpty(item.Label))
            {
                var nr = fr.Map(61f, 810f, 651.8f, 918f);
                ClippedText(content, nr, item.Label, Color.white, NodeName, 35.1f * fr.K, st.QText, nr.W, st, 0,
                            12f * fr.K, 75f * fr.K, 36f * fr.K, true);
            }
        }

        /// <summary>原版 `TitleIconDrawer.Draw` —— **空方法体**。这一档唯一的可见件是 prefab 出厂那张固定图
        /// `40K_Profile_icon_title`（根 1080×1080、视觉 ×1.25）。⛔ 别给它编阵营名/名字（那四颗槽全是 null）。</summary>
        static void TitleIcon(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                              ref ItemDrawResult res)
        {
            var fr = FrameOf(box, DrIconRoot, DrIconRoot);
            var content = DrawerIconContent(node, fr);
            var r = DrawerFrame.Scale(fr.Map(0f, 0f, DrIconRoot, DrIconRoot), 1.25f);   // 视觉 ×1.25（绕中心）
            var tex = Tex(TitleIconArt);
            if (!DrawerImage(content, r, tex, TitleIconArt, st, ref res))
                Note("titleIcon|art|" + TitleIconArt,
                     "称号图标档的固定图 `" + TitleIconArt + "` 取不到（`Resources/Art/ui_menu/`）⇒ 这一格空着"
                     + "（原版那图是 prefab 出厂 sprite，`Draw` 是空体、什么都不写）");
        }
    }
}
