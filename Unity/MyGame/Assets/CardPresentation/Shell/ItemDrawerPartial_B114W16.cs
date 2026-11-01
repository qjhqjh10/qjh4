// ItemDrawerPartial_B114W16.cs — 第 114 会话写手 `W-16`（2026-11-01）· 账 `B-249`：`Shop` 档 Avatar 变体
//
// 【为什么单开这一节】原版 `Avatar Drawer Shop Variant` 与基础档 `Avatar Drawer` 是
//   **同一个类 `AvatarDrawer`、两套 prefab**（`ItemTypeSets` 里 `PlayerAvatar` 的 Shop(20) 列仍写 `AvatarDrawer`，
//   20 条记录里**只有 2 条**写了 Shop 档：`PlayerAvatar` / `CosmeticItemAvatarBorder`）。
//   `Dispatch` 早就把 `DrawerOverride ov` 收进来了，但 26 个 `case` 里原先**只有 `GenericArmy` 把 `ov` 往下传**
//   ⇒ 这一档永远按基础档几何画。施工方式与 `B114_WC` 的 Icon 档**同一套**：
//   `Dispatch` 的第一行 `{` 里**同物理行**加一句 `if (ov == Shop && ShopAvatarVariant(...)) return;`
//   （零行数变化，账的 A 落点不动）；实现全在本文件。
//
// 【判据（现行 build 现读 · 与 `ItemDrawerLayout.cs` 里那批常量**同一条读法**）】
//   `python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Avatar Drawer Shop Variant" --depth 4 --relative --no-sprite --no-ancestor-scale`
//   （出处 `d:/2/新解包资源/assets_full/bundle_menus_assets_all`）逐行：
//     · 根：`AspectRatioFitter` 高控宽 0.66 ⇒ 712.8×1080 —— **与基础档逐值相同**
//     · `Content`：锚 (0.5,0.5)-(0.5,0.5) · `sizeDelta` 712×1080 —— **也与基础档逐值相同**
//       ⇒ ⛔ 别被「两套 prefab」这四个字骗成「连 Content 都换了」（`B-249` 施工单里那句「`Content` 也分叉」**不成立**，
//          两份 prefab 的 `Content` 锚点/尺寸逐字节相同；真正的分叉是下面 5 条）
//     · `Background`：**GameObject `m_IsActive = 0`**（基础档是 **1**）
//       ⇒ 原版这一档**没有**那张 `40k_Rewards_icon_base card` 底板。
//         判据链：`ItemDrawer<T>.Draw`（`d:/2/tools/decomp_generic_trial/out/ItemDrawer_object___Draw.c`）
//         全程**没碰过** `background` 那格（它只 SetActive 了 `label` 与 `quantity` 两颗）⇒ 序列化态说了算。
//     · `avatarBG`：锚 (0,0.38)-(1,0.95) ⇒ 设计 px (0.4,54)-(712.4,669.6)
//       （基础档 (139.8,233.3)-(571.0,712.3)）—— 我们**不建**这一层（原版 `Draw` 也不碰它），
//       它只用来推 `avatar` 的落点。
//     · `avatar`（= `ItemDrawerComponents.image` 槽）：锚 **-0.475..1.52 × -0.175..1.35**（相对 `avatarBG`，
//       `sizeDelta` ≈0）⇒ 设计 px **(-337.8,-161.46)-(1082.64,777.33)** = **1420.44×938.79**
//       （基础档 `avatar` 是 (-65.0,65.6)-(795.2,796.1) = 860.21×730.48）⇒ **这一格是整份 prefab 差得最大的一处**。
//     · `Army`：锚 **(0,0.26)-(1,0.35)** · `sizeDelta` 0 ⇒ 设计 px **(0.4,702)-(712.4,799.2)** = 712×97.2，
//       **`m_IsActive = 1`**（基础档 / Icon 档都是 0）。
//       TMP：`m_fontSize 90` · `m_fontSizeMax 90` · `m_fontSizeMin 12` · `m_fontSizeBase 36` ·
//            色 **(1,1,1,1)（白；通用可见档那批是 (0.689,0.689,0.689,0.851)）** · 对齐 Center/**Middle** · `m_TextWrappingMode 0`（不折行）。
//     · `Label`：锚 (0,0.14)-(1,0.25) · `sizeDelta` **(4,0)** ⇒ 设计 px **(-1.6,810)-(714.4,928.8)** = 716×118.8
//       （基础档 (36.0,810)-(676.8,918.0) = 640.8×108）。
//       `HorizontalLayoutGroup` pad L/R 25 ⇒ 子件 `Name` 设计 px **(23.4,810)-(689.4,928.8)** = 666×118.8，
//       TMP `m_fontSize 90`（基础档 75）· 白 · Center/Middle · 不折行。
//     · `quantity` 槽 = null（与基础档同）⇒ 这一档**永不画数量**。
//     · `Premium Highlight` / `Converted Drawer` / `Ephemeral Drawer` / `Collected Badge` 出厂 inactive（与基础档同，
//       由 `ResetDecorationLayers` 那一族负责）。
//
// 【两条「两源打架」的记录（铁律 5 · 反证一起写在这儿）】
//   ① 🔴 **字号 90 vs 75**：`d:/2/tools/assetripper_out/ExportedProject/Assets/Avatar Drawer Shop Variant.prefab`
//      里这两颗 TMP 的 `m_fontSize` 都还是 **75**（和基础档逐值相同）。**本实现取 90**，理由三条：
//      (a) 真包才是现行 build，且本工程 `ItemDrawerLayout.cs` 里**全部**版面常量都是从这个工具/这条读法出来的；
//      (b) 真包 `MonoBehaviour/` 里 `m_fontSize == 90` 的 `'Goff'` / `'Legendary Wildcard'` **全库各只有一颗**，
//          而且 `'Goff'` 那颗的锚点逐值等于本 prefab 的 `Army`（(0,0.26)-(1,0.35)）⇒ 它**就是**这一档的 Army，不是别处的；
//      (c) AssetRipper 那份导出里两份 prefab 的 TMP 用的是**同一个 pathID**（`114694121525821480`），
//          而它们的 RectTransform 又确实不同 ⇒ 那份导出是「prefab variant 的 override 只合进去了一部分」的产物
//          （矩形合进去了、TMP 覆盖没合进去），不能当这两个字段的判据。
//   ② 🔴 **`Content` 没有分叉**（见上面第二条）：`B-249` 施工单与 `B114_W11` 报告 §五·2 都写着「`Content`/`avatar`/
//      `Army`/`Label` 的锚点分叉」——`Content` 与 `avatar` 自身的锚点**逐值相同**；`avatar` 的落点变是因为**父件
//      `avatarBG` 换了**（继承，不是它自己改），`Content` 是**一点没变**。
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        // ============================================================ B114_W16 · B-249：Shop 档 Avatar 变体（2026-11-01）
        //
        //  设计 px（根 712.8×1080 · 左上原点 · y 向下）—— 出处见文件头，⛔ 别拿基础档那批常量顶替：
        /// <summary>`Shop` 档 `avatar`（`image` 槽）那一格 = prefab 锚 -0.475..1.52 × -0.175..1.35（相对 `avatarBG`）。</summary>
        const float ShopAvatarX1 = -337.8f, ShopAvatarY1 = -161.46f, ShopAvatarX2 = 1082.64f, ShopAvatarY2 = 777.33f;
        /// <summary>`Shop` 档 `Army` 那一行 = prefab 锚 (0,0.26)-(1,0.35) of `Content` 712×1080。</summary>
        const float ShopArmyX1 = 0.4f, ShopArmyY1 = 702f, ShopArmyX2 = 712.4f, ShopArmyY2 = 799.2f;
        /// <summary>`Shop` 档 `Label` 那一行 = prefab 锚 (0,0.14)-(1,0.25) + `sizeDelta.x 4`（左右各外扩 2）。</summary>
        const float ShopLabelX1 = -1.6f, ShopLabelY1 = 810f, ShopLabelX2 = 714.4f, ShopLabelY2 = 928.8f;
        /// <summary>`Shop` 档 `Label/Name` = `Label` 左右各收 `HorizontalLayoutGroup` 的 pad 25。</summary>
        const float ShopNameX1 = 23.4f, ShopNameX2 = 689.4f;
        /// <summary>`Shop` 档两颗 TMP 的 `m_fontSize` / `m_fontSizeMax`（基础档是 75）。</summary>
        const float ShopTextPx = 90f, ShopTextMinPx = 12f, ShopTextBasePx = 36f;
        /// <summary>`Shop` 档 `Content/Army` 的 `m_fontColor` —— **白**（通用可见档那批是 `DrArmyInk` 的灰）。</summary>
        static readonly Color ShopArmyInk = new Color(1f, 1f, 1f, 1f);

        /// <summary>`Dispatch` 的 Shop 档分流：命中返回 `true`（调用方 `return`），否则 `false`（照旧走 switch）。
        /// <para>⚠️ **只认 `DrawerOverride.Shop`**。`Horizontal`(15)/`OfferPopups`(30) 两档的变体几何**没查过**
        /// ⇒ ⛔ 别顺手接上（`ItemTypeSets` 里那两列本族都是 `null` = 回落主档，真包也没有对应的 Avatar 变体 prefab）。</para></summary>
        static bool ShopAvatarVariant(string drawer, Transform node, PxRect box, ItemSpec item, int quantity,
                                      ItemDrawerStyle style, ref ItemDrawResult res)
        {
            switch (drawer)
            {
                case DrawerAvatar: ShopAvatar(node, box, item, quantity, style, ref res); return true;
                default: return false;
            }
        }

        /// <summary>原版 `Avatar Drawer Shop Variant` 上的 `AvatarDrawer.Draw`（**同一个类，只是换了套 prefab**）。
        /// <para>与基础档 `ItemDrawerAvatar.Avatar(...)` 的四点差别（其余逐句相同）：
        /// ① 主图那格换成 `ShopAvatarXxx`；② **不建** `Background`（这一档的 GameObject 出厂 inactive）；
        /// ③ `Army` **建出来且保持激活**（这一档 `m_IsActive = 1`）+ 白字 + 字号 90 + Center/Middle + 不折行；
        /// ④ `Label` / `Name` 换成 `ShopLabelXxx` / `ShopNameXxx` + 字号 90。
        /// `quantity` 槽仍是 null ⇒ 与基础档一样**永不画数量**。</para></summary>
        static void ShopAvatar(Transform node, PxRect box, ItemSpec item, int quantity, ItemDrawerStyle st,
                               ref ItemDrawResult res)
        {
            var fr = FrameOf(box, fit: RootFit.HeightControlsWidth);   // ARF `m_AspectMode 2` · 0.66 —— 与基础档同一档（B-260）
            var content = DrawerContent(node, fr);          // `Content` 712×1080 —— 与基础档逐值相同

            // ⛔ 不调 `DrawerBackground`：这一档的 `Content/Background` 是 `m_IsActive: 0`
            //    ⇒ 原版屏幕上没有那张底板（`ItemDrawer<T>.Draw` 全程不碰 `background` 那格，见文件头）。

            // ① `image.sprite = item.Image` —— 落点换成 `avatarBG` 换成新框之后的 `avatar` 那一格
            var tex = CardArt.Cosmetics(item.Art);
            if (!DrawerImage(content, fr.Map(ShopAvatarX1, ShopAvatarY1, ShopAvatarX2, ShopAvatarY2), tex, item.Art,
                             st, ref res))
                Note("avatar|art|" + (item.Id ?? "?"),
                     "头像 `" + (item.Id ?? "?") + "` 的图名 `" + (item.Art ?? "<空>") + "` 取不到"
                     + "（`CardArt.Cosmetics` 的 `Resources/Art/avatars/`）⇒ 这一层**不画**（⛔ 不拿别的图顶替）");

            // ② 阵营名 —— **这一档原版真显示**（`Army` 的 `m_IsActive = 1`），⛔ 别照基础档那句 `SetActive(false)`
            DrawShopArmyName(content, fr, item.CardArmy, st);

            // ③ `Label/Name`（`quantity` 槽 null ⇒ 不画数量）
            DrawShopName(content, fr, item, st);
        }

        /// <summary>`Shop` 档的 `Content/Army`：白字 · 字号 90 · Center/Middle · **不折行** · 保持激活。
        /// <para>⚠️ ⛔ 别转调 `DrawerArmyName` —— 那一份是通用可见档的尺（(0.4,702)-(712.4,810)、灰 `DrArmyInk`、
        /// 字号 75、折行**开**）。本档与它**只有 x1/x2/y1 三个数恰好相同**（0.4 / 712.4 / 702），
        /// **y2（799.2 vs 810）、墨色、字号、折行**四处都不同 ⇒ 不能复用。</para>
        /// <para>`Neutral`（`LocalizedName` 空串）⇒ 不建节点（与原版「写空串进一棵没显示的树」在屏幕上同义，
        /// 这一档那棵树是**显示**的 ⇒ 空串也画不出东西）。</para></summary>
        static void DrawShopArmyName(Transform content, DrawerFrame fr, int army, ItemDrawerStyle st)
        {
            if (st.NamePx <= 0f) return;                    // 调用方不要文字层（与 `DrawerArmyName` 同一条闸）
            string name = ArmyIcons.LocalizedName(army);
            if (string.IsNullOrEmpty(name)) return;
            var r = fr.Map(ShopArmyX1, ShopArmyY1, ShopArmyX2, ShopArmyY2);
            ClippedText(content, r, name, ShopArmyInk, NodeArmy,
                        ShopTextPx * fr.K, st.QText, r.W, st, 0,
                        ShopTextMinPx * fr.K, ShopTextPx * fr.K, ShopTextBasePx * fr.K, true);
        }

        /// <summary>`Shop` 档的 `Content/Label/Name`（`Label` 716×118.8（左右外扩 2）· `Name` 666×118.8 ·
        /// 字号 90 · 白 · 不折行）。
        /// <para>⚠️ ⛔ 别转调 `DrawerNameQuantity` —— 它的 `Label` / `Name` 用的是基础档那两格（640.8 宽 / 字号 75）。</para></summary>
        static void DrawShopName(Transform content, DrawerFrame fr, ItemSpec item, ItemDrawerStyle st)
        {
            if (st.NamePx <= 0f) return;
            var rowNode = MenuDraw.Node(content, "Label", fr.Map(ShopLabelX1, ShopLabelY1, ShopLabelX2, ShopLabelY2));
            if (rowNode == null || string.IsNullOrEmpty(item.Label)) return;
            var nr = fr.Map(ShopNameX1, ShopLabelY1, ShopNameX2, ShopLabelY2);
            ClippedText(rowNode, nr, item.Label, Color.white, NodeName,
                        ShopTextPx * fr.K, st.QText, nr.W, st, 0,
                        ShopTextMinPx * fr.K, ShopTextPx * fr.K, ShopTextBasePx * fr.K, true);
        }
    }
}
