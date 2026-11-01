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
//      `Content/Background` 铺满 · sprite = **`40k_Rewards_icon_title card`**（329×499）—— 🔴 **不是**别档共享的
//        `40k_Rewards_icon_base card`（2026-11-01 `B114_W35` 真包现读，`B-275`；见 `ItemDrawerLayout.cs` 文件头 ②）·
//      `Content/Image` **(191.5,76.5)-(520.5,422.3)**（`Image Top` 是它的子件、
//      出厂 alpha 0.729 的一层静态件，**不是 `Draw` 产物 ⇒ 不建**）·
//      `Content/Army` (0.4,702)-(712.4,810) **fs 35.099998** · `Content/Title` **(64.7,432.1)-(648.2,680.5)** fs **27.05**
//      auto[15~80] 居中 折行 1 · `Content/Label/Name` (61,810)-(651.8,918) fs **35.1**（**没有 Quantity 子件**）
//      ⚠️ 括号里那四个数 = 原 dump 的 `⇲ls=0.2444` 行 ÷ 0.244367 还原到设计空间（逐项算过）。
//      🔴 **2026-11-01 就地订正（铁律 5 · `B114_W35`，`B-276`）**：本行原来写 `Army` **fs75** —— **错**，
//         真包现读是 **35.099998**（auto 仍是 [12,75]、基准仍 36、色同）。对照 `Avatar Drawer` 的 `Army`
//         **确实是 75.0** ⇒ **75 是通则、`Title Drawer` 是例外**（⛔ 别把通则也改掉）。
//         ✅ **2026-11-01 `B114_W38`（`B-280`）已补完纵向档**：原版这颗 `Army` 是 **Center/Bottom**（我们原先是出厂
//         `Middle`）⇒ `Title(...)` 拿到 `DrawerArmyName` 的返回值后调 `MenuDraw.SetVAlign(lb, Label.VAlign.Bottom, r)`。
//         ✅ 同一份 prefab 的 `Label/Name` 是 **Center/Midline** ⇒ 2026-11-01 `B114_W43`（`B-285`）已补（对照 `Avatar Drawer` = `Middle`）。
//         🔴 `Content/Background` 的 `m_PreserveAspect` 也是 **1**（我们原先拉伸）⇒ `DrawerBackground(..., true)`（`B-282①`）。
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
        /// <summary>`Content/Background` 那张图 —— **这一份 prefab 专属**（真包现读 `m_Sprite` = `40k_Rewards_icon_title card`，
        /// 落盘名空格→下划线；资源在 `Resources/Art/ui_menu/40k_Rewards_icon_title_card.png`，329×499）。
        /// ⛔ **不是** `ItemDrawerLayout.DrBackgroundSprite`（`40k_Rewards_icon_base card`）—— 2026-11-01 `B-275`。</summary>
        const string TitleBgSprite = "40k_Rewards_icon_title_card";
        /// <summary>`Content/Army` 的 `m_fontSize`（**真包现读 35.099998**；auto 仍 [12,75] · 基准 36）。
        /// 🔴 与通则（`DrArmyFontPx` = 75，`Avatar Drawer` 实读）**不同** —— 2026-11-01 `B-276`。</summary>
        const float TitleArmyFontPx = 35.1f;

        /// <summary>原版 `TitleDrawer.Draw` 的等价实现（**只服务竖卡 `Title Drawer`**）。
        /// <para>🔴 **2026-11-01 就地订正（铁律 5 · `B114_W35`）**：本行原来写「`TitleDrawerHorizontal` **共用这一份** ——
        /// 原版就是继承」—— 方法体的继承关系仍然成立，但**横条档已经不走这里了**：`B114_W32` 把
        /// `TitleDrawerHorizontal` 按类名分流到 `ItemDrawerPartial_B114W32.cs` 的 `TitleHorizontal(...)`
        /// （两份 prefab 的版面一个数都不共用：`Content` 712×1080 vs 1028×370）⇒ **本函数今天只服务竖卡**，
        /// 所以下面这两处「Title 专属」的例外值不会污染横条档。</para>
        /// <para>⚠️ 两条如实记：① 原版第 3 句把 `image` 的 sprite **抄给** `additionalArmyIconForEffect`
        /// （同一张图、叠一层）⇒ 我们按同一矩形再画一张（视觉相同；那一颗的真实矩形在 prefab 里没单独给，
        /// 它挂在 `image` 下面 ⇒ 按父矩形画）；② `armyText` 在 `Icon Title Drawer Variant` 上是 null ⇒ 那一档
        /// **不画阵营名**（那是 `TitleIcon(...)` 的事）。</para></summary>
        static void Title(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                          ref ItemDrawResult res)
        {
            var fr = FrameOf(box, fit: RootFit.HeightControlsWidth);   // `Title Drawer`（竖卡）：ARF `m_AspectMode 2` · 0.66（B-260）
            var content = DrawerContent(node, fr);
            // 底板 = 这一份 prefab 自己那张（`40k_Rewards_icon_title card`），⛔ 不是共享的 `..._base_card`
            //   第 5 个实参 = `m_PreserveAspect`：真包现读这一颗是 **1**（B-282①）⇒ 传 `true`。
            DrawerBackground(content, fr, st, TitleBgSprite, true);

            // `armyText.text = CardArmyToString(cardArmy)`
            //   字号 = 本档专属的 35.1（真包现读），⛔ 别用通则 75（`Avatar Drawer` 才是 75）
            var armyLb = DrawerArmyName(content, fr, item.CardArmy, st, TitleArmyFontPx);
            //   垂直档 = **`Center/Bottom`**（真包现读；对照 `Avatar Drawer` 是 `Center/Middle`）—— B-280。
            //   矩形与 `DrawerArmyName` 内部那一格逐值相同（(0.4,702)-(712.4,810) ⇒ 真包 (0.1,171.5)-(174.1,197.9) ÷ 0.244367）。
            //   ⚠️ 拿 `null` 调是安全的（`MenuDraw.SetVAlign` 自己判空）—— 阵营认不出时那颗本来就不建。
            MenuDraw.SetVAlign(armyLb, Label.VAlign.Bottom, fr.Map(DrCx1, DrArmyY1, DrCx2, DrArmyY2));

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
            //   垂直档 = **`Center/Midline`**（真包现读；对照 `Avatar Drawer` 的 `Label/Name` 是 `Center/Middle`）—— B-285。
            if (st.NamePx > 0f && !string.IsNullOrEmpty(item.Label))
            {
                var nr = fr.Map(61f, 810f, 651.8f, 918f);
                var nameLb = ClippedText(content, nr, item.Label, Color.white, NodeName, 35.1f * fr.K, st.QText, nr.W, st, 0,
                                         12f * fr.K, 75f * fr.K, 36f * fr.K, true);
                //   拿 `null` 调是安全的（`MenuDraw.SetVAlign` 自己判空）—— 整块在视口外时那颗本来就不建。
                MenuDraw.SetVAlign(nameLb, Label.VAlign.Midline, nr);
            }
        }

        /// <summary>原版 `TitleIconDrawer.Draw` —— **空方法体**。这一档唯一的可见件是 prefab 出厂那张固定图
        /// `40K_Profile_icon_title`（根 1080×1080、视觉 ×1.25）。⛔ 别给它编阵营名/名字（那四颗槽全是 null）。</summary>
        static void TitleIcon(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                              ref ItemDrawResult res)
        {
            var fr = FrameOf(box, DrIconRoot, DrIconRoot, RootFit.HeightControlsWidth);   // `Icon Title Drawer Variant`：ARF 2 · 1（B-260）
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
