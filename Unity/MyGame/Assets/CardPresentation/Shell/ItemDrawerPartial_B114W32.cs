// ItemDrawerPartial_B114W32.cs — 第一百一十四会话 · 写手 `W-32`（2026-11-01）· 账 `B-263`：`Horizontal`(15) / `OfferPopups`(30) 的称号抽屉变体几何
//
// 【这一条是什么】`CosmeticItemTitle` 的 `Horizontal`(15) 与 `OfferPopups`(30) 两档指的是
//   **`Title Drawer Horizontal Variant`**（`ItemTypeSets` 里那两列都写 `TitleDrawerHorizontal`；
//   判据 = `资料/普查产出_1004/ItemDrawerConfig_映射表.md` §② 的两行 `TitleDrawerHorizontal`，
//   GUID `b9799d0d3bc9161499d7d546a31646f9`）。原版 `TitleDrawerHorizontal : TitleDrawer` **不覆写 `Draw`**
//   ⇒ 方法体逐句相同，**但 prefab 是另一个人**：`Content` 是 **1028×370 的横条**（不是 712×1080 的竖卡）。
//   我们原来把 `case DrawerTitle: case DrawerTitleHorizontal:` 合成一支、都走 `Title(...)` ⇒
//   这一档**整套版面都是基础档的**（竖卡几何 + 阵营名 + 名字行），在横条框里画出来完全是另一个东西。
//
// 【判据（现行 build 现读：`python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all
//   "Title Drawer Horizontal Variant" --depth 4 --relative`，出处 `d:/2/新解包资源/assets_full/bundle_menus_assets_all`）】
//   · 根：**没有 `AspectRatioFitter`**（`m_Component` 里只有 `ItemDrawerComponents` / `TitleDrawerHorizontal` /
//     `EverguildButton`），锚 `(0,0)-(1,1)` + `sizeDelta (0,0)` ⇒ **根 = 父框本身**（任何比例都照铺）。
//   · `Content`：锚 `(0.5,0.5)` · `sizeDelta` **1028×370** · `UIScaleToFit(fitInParent:1)` ⇒
//     缩放 = `min(根宽/1028, 根高/370)` —— dump 实测那一份是 `×0.300024`（根 315.41×111.01，宽 0.3068 / 高 0.3000 取小）
//     ⇒ **等比内接**，与 `FrameOf(box, 1028, 370)` 的 `Contain` 口径**逐位同一条算式**。
//   · `Content/Background`：(0,0)-(1028,370)（铺满）· sprite = **`40k_Profile_display_title`**（322×116 · **preserveAspect**；2026-11-01 就地订正：原写「PA 关」与真包相反）
//     ⚠️ **不是**别档共用的 `40k_Rewards_icon_base card`（`ItemDrawerLayout.DrBackgroundSprite`）——
//        这一份 prefab 的 `background` 槽指向的就是另一张（⛔ 别拿共享常量顶）。
//   · `Content/Image`（`image` 槽）：**(786.7,141.5)-(1004.0,353.0)** = 217.33×211.49（PA=1）· 出厂无图
//     （`TitleDrawer.Draw` 那句 `image.sprite = GetArmyIcon(cardArmy)` 才给图）。
//   · `Content/Title`（`title` 槽）：**(51.4,64.4)-(976.6,306.6)** = 925.2×242.27 ·
//     TMP `m_fontSize 120` · auto `[15,120]` · base 36 · **Center/Middle** · 折行 **1** · 白。
//   · `Content/Army`（`armyText` 槽）：**`m_IsActive = 0`** ⇒ 原版屏幕上没有阵营名那一行
//     （`TitleDrawer.Draw` 只写 `armyText.text`、一次 `SetActive` 都没调 ⇒ 序列化态说了算）。
//   · `Content/Label`（`label` / `quantity` 槽的父件）：**`m_IsActive = 0`** ⇒ 名字与数量**都不出现**。
//   · `Content/Lock` / `Content/highlight`：出厂 `m_IsActive = 0`，由 `ToggleLock` / `ToggleHighlight` 那两口负责（不在本波）。
//
// 【两条「两个数据源打架」的记录（铁律 5 · 反证一起写在这儿）】
//   ① 🔴 **`Title` 的字号/高度：真包 120 / 242.27，AssetRipper 导出 90 / 232.70**（矩形四沿一致、只差字号与高）。
//      **取真包**，理由与 `B114_W16` 文件头 ① 同一条：`assetripper_out` 那份 prefab variant 的 **TMP override 没合进去**
//      （枚举、`m_fontSize`、`m_fontSizeMax` 这类覆盖会漏），而 `menu_dump` 读的是**现行 build 的 bundle 本体**。
//      ⇒ 凡本文件里带「字号 / auto 区间」的数，出处一律是 `menu_dump`；⛔ 别拿那份导出去改回来。
//   ② 🔴 **根那 `↻2.45°` 不是这份 prefab 的属性**：`Title Drawer Horizontal Variant.prefab` 的根
//      `m_LocalRotation = (0,0,0,1)` / `m_LocalEulerAnglesHint (0,0,0)`（AssetRipper 直读）。
//      `menu_dump` 打出来的是**某一份实例**（挂在一扇窗里）的旋转 ⇒ 与 `Cardback Drawer` 的 `-7.27°` 同族
//      （由调用点/实例负责，⛔ 别画进抽屉本体）。
//
// 【为什么单开一节】与 `B114_W16`（Shop Avatar）/ `B114_W24`（Shop AvatarBorder）/ `B114_WC`（Icon 七族）同一套：
//   `Dispatch` 的第一行 `{` 里**同物理行**加一句分流（零行数变化，账的 A 落点不动）；实现全在本文件。
//   ⚠️ 分流**按抽屉类名**判（`TitleDrawerHorizontal`），不按 `ov` —— 那个类名**只**由 `Horizontal` /
//   `OfferPopups` 两档选中（`ItemTypeSets` 里 `CosmeticItemTitle` 那两列），而 `ItemDrawerView.Refresh()`
//   回头调 `Dispatch` 时**不传 `ov`**（`Shell/ItemDrawer.cs:2030`）⇒ 按 `ov` 判会让重画掉回竖卡几何。
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        // ============================================================ B114_W32 · B-263：`Title Drawer Horizontal Variant`（2026-11-01）
        //
        //  设计 px（**`Content` 空间**：左上原点 · y 向下 · `Content` = 1028×370）—— 出处见文件头，⛔ 别拿竖卡那批顶替：
        /// <summary>`Title Drawer Horizontal Variant` 的 `Content` 尺寸（`m_SizeDelta (1028,370)`）。</summary>
        const float DrHTitleW = 1028f, DrHTitleH = 370f;
        /// <summary>`Content/Image`（`image` 槽）那一格：锚 (0.7653,0.046)-(0.9767,0.6176) ⇒ 设计 px (786.7,141.5)-(1004.0,353.0)。</summary>
        const float DrHTitleImgX1 = 786.7f, DrHTitleImgY1 = 141.5f, DrHTitleImgX2 = 1004.0f, DrHTitleImgY2 = 353.0f;
        /// <summary>`Content/Title`（`title` 槽）：锚 (0.05,0.1713)-(0.95,0.826) ⇒ 设计 px (51.4,64.4)-(976.6,306.6)。</summary>
        const float DrHTitleTextX1 = 51.4f, DrHTitleTextY1 = 64.4f, DrHTitleTextX2 = 976.6f, DrHTitleTextY2 = 306.6f;
        /// <summary>`Content/Background` 那张图（**这一份 prefab 专属**，⛔ 不是共享的 `40k_Rewards_icon_base card`）。</summary>
        const string DrHTitleBgSprite = "40k_Profile_display_title";
        /// <summary>`Content/Title` 的 TMP 刻度：`m_fontSize 120` · auto `[15,120]` · base 36（真包现读）。</summary>
        const float DrHTitleTextPx = 120f, DrHTitleTextMinPx = 15f, DrHTitleTextBasePx = 36f;

        /// <summary>`Dispatch` 的横条称号档分流：命中返回 `true`（调用方 `return`），否则 `false`（照旧走 switch）。
        /// <para>⚠️ **只认 `TitleDrawerHorizontal` 这个类名**（理由见文件头末段：`Refresh()` 不传 `ov`）。</para></summary>
        static bool HorizontalTitleVariant(string drawer, Transform node, PxRect box, ItemSpec item, int quantity,
                                           ItemDrawerStyle style, ref ItemDrawResult res)
        {
            switch (drawer)
            {
                case DrawerTitleHorizontal: TitleHorizontal(node, box, item, quantity, style, ref res); return true;
                default: return false;
            }
        }

        /// <summary>原版 `Title Drawer Horizontal Variant` 上的 `TitleDrawer.Draw`（**同一个方法体**，换了一套 prefab）。
        /// <para>与基础档 `Title(...)` 的差别（其余逐句相同：都是「底板 + 阵营徽记 + 称号正文」这三层）：
        /// ① `Content` 换成 **1028×370**（根无 ARF ⇒ 根 = 框，`UIScaleToFit` 等比内接 ⇒ `FrameOf(box, 1028, 370)`）；
        /// ② 底板图换成 `40k_Profile_display_title`；
        /// ③ 徽记那格换成右侧的 (786.7,141.5)-(1004.0,353.0)；
        /// ④ 称号正文那一格 + 字号 120；
        /// ⑤ **不建** `Army` / `Label`（这两颗在 prefab 里 `m_IsActive = 0`，原版 `Draw` 也不开）。
        /// `quantity` 槽虽非 null，但它爹 `Label` 关着 ⇒ 与原版一致**永不画数量**。</para></summary>
        static void TitleHorizontal(Transform node, PxRect box, ItemSpec item, int quantity, ItemDrawerStyle st,
                                    ref ItemDrawResult res)
        {
            // 根 = 框本身（这份 prefab 的根没有 `AspectRatioFitter`）；`Content` 由 `UIScaleToFit` 等比内接 ⇒ Contain。
            var fr = FrameOf(box, DrHTitleW, DrHTitleH);
            var content = MenuDraw.Node(node, NodeContent, fr.Map(0f, 0f, DrHTitleW, DrHTitleH));

            // ① 底板（`Content/Background`，铺满 1028×370）
            var bg = Tex(DrHTitleBgSprite);
            if (bg == null)
                Note("drawerbgsprite|" + DrHTitleBgSprite,
                     "横条称号档的底板 `" + DrHTitleBgSprite + "` 在 `Resources/` 里取不到"
                     + "（`CardArt.MenuUi` 三级全空）⇒ 这一层**不画**（⛔ 不拿别的档那张 `40k_Rewards_icon_base card` 顶替）");
            else
                MenuDraw.Rect(content, bg, fr.Map(0f, 0f, DrHTitleW, DrHTitleH), NodeBackground,
                              st.QBoard, null, true, st.Clip, st.ClipSoftness);

            // ② `image.sprite = GetArmyIcon(cardArmy)` + `additionalArmyIconForEffect.sprite = image.m_Sprite`
            //    （`additionalArmyIconForEffect` 在这份 prefab 里 = `Content/Image/Image Top`，矩形与 `Image` 逐值相同）
            var tex = ArmyIcons.Texture(item.CardArmy);
            if (tex == null)
                Note("titleh|army|" + (item.Id ?? "?"),
                     "横条称号 `" + (item.Id ?? "?") + "` 的阵营 " + item.CardArmy + " 没有徽记图（`ArmyIcons.Texture`）"
                     + " ⇒ 原版把 `image` 与 `additionalArmyIconForEffect` **两层都关掉**，我们两层都不画");
            var imgR = fr.Map(DrHTitleImgX1, DrHTitleImgY1, DrHTitleImgX2, DrHTitleImgY2);
            if (DrawerImage(content, imgR, tex, ArmyIcons.IconName(item.CardArmy), st, ref res))
                MenuDraw.Rect(content, tex, imgR, NodeArmyIconExtra, st.QArt + 1, null, true, st.Clip, st.ClipSoftness);

            // ③ `title.text = item.LocalizedName`（空 ⇒ 不画：原版那颗 TMP 的出厂串是占位）
            string title = string.IsNullOrEmpty(item.Title) ? item.Label : item.Title;
            if (st.NamePx > 0f && !string.IsNullOrEmpty(title))
            {
                var r = fr.Map(DrHTitleTextX1, DrHTitleTextY1, DrHTitleTextX2, DrHTitleTextY2);
                ClippedText(content, r, title, Color.white, NodeTitleText, DrHTitleTextPx * fr.K, st.QText, r.W, st, 0,
                            DrHTitleTextMinPx * fr.K, DrHTitleTextPx * fr.K, DrHTitleTextBasePx * fr.K);
            }

            // ④ ⛔ 不建 `Army` / `Label`：这一份 prefab 里那两颗 `m_IsActive = 0`，而原版 `Draw` 只写文本
            //    （`armyText.text` / `label.setText`），**一次 `SetActive` 都没调** ⇒ 序列化态说了算 ⇒ 屏幕上没有。
        }
    }
}
