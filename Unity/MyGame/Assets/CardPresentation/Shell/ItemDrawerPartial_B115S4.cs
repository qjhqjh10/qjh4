// ItemDrawerPartial_B115S4.cs — 第 115 会话 · 代理 `B115_S4`（2026-11-01）· 账 `B-293` 的第 1 项：`Wildcard Drawer`
//
// 判据：`资料/完全复刻/判据/B115_S4_三份Drawer规格.md` §四（`Wildcard Drawer.prefab` 逐节点现读，
//       根 = `ExportedProject/Assets/Wildcard Drawer.prefab` 里 `m_Father: {fileID: 0}` 的那个）。
//
// 本文件只放 `Wildcard()` 重写之后**新增**的两件（`Wildcard()` 本体留在 `Shell/ItemDrawer.cs`：
// 它是既有成员，且那块区域的行数被账落点钉死 ⇒ 只能整段同物理行替换，新成员不能塞进去）：
//   ① `WildcardEmblemRect(fr)` —— `Content/Image` 那条**下沿横带**（锚 (0,−0.05)-(1,0.13)）。
//      ⚠️ 这条矩形与 `Shell/ItemDrawerContainer.cs` 里 `Army Icon` 用的是**同一条**（同族共用矩形）；
//      ⛔ 那边不在本次白名单，所以这次**没有**把它抽成共用常量，两处各留一份（已在 §七-3 记为待收口）。
//   ② `WildcardLabelRow(...)` —— `Content/Label` 那一行（`40k_main_bt_nametag` 底板 + `Quantity` + `Name`）。
//
//  ⛔ 为什么不复用 `ItemDrawerLayout.cs` 的 `DrawerNameQuantity`：几何**逐位相同**（`DrLb*`/`DrQty*`/`DrName*`
//     那三组常量就是这一族的），但**水平对齐不同** —— 原版这一档 `Name` 是 `m_HorizontalAlignment = 1`（**Left**），
//     而 `DrawerNameQuantity` 给的是 Center（它按 `Avatar Drawer` 那一族写的）。共用件不在本次白名单
//     ⇒ 本档自己建这一行，**数字仍照共用常量**（⛔ 别在这里再抄一份几何）。
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        /// <summary>`Content/Label` 的那张底板（原版 `Label` 节点自己就是一颗 `Image`）。
        /// 出处 = `Wildcard Drawer.prefab` 的 `Content/Label`：`m_Sprite` 指到这名字、`m_PreserveAspect 0`（拉伸）。
        /// ⚠️ 落盘在 `Resources/Art/ui/`（`CardArt.MenuUi` 的第三级），现读存在。</summary>
        internal const string WildcardNametagSprite = "40k_main_bt_nametag";

        /// <summary>`Content/Image`（阵营徽记）的矩形。左右沿 = `Content` 的两沿（`DrCx1/DrCx2`，不是整根 0…712.8），
        /// 上下沿 = **共用常量** `DrEmblemY1/Y2`（= prefab 的 `m_AnchorMin (0,−0.05)` / `m_AnchorMax (1,0.13)` × 1080）。
        /// <para>🔴 **2026-11-01（`B-315`）收口**：这条横带原来是本文件与 `Shell/ItemDrawerContainer.cs` **各写一份字面量**，
        /// 现两处同引 `ItemDrawerLayout.DrEmblemY1/Y2`（唯一来源）—— 数值逐位不变（`939.6f` / `1134f` 同两个字面量）。</para>
        /// <para>⚠️ `m_PreserveAspect = 1` ⇒ 画的时候必须 `keepAspect: true`（徽记 256² 内接到这条 712×194.4 的横带里）。</para></summary>
        internal static PxRect WildcardEmblemRect(DrawerFrame fr)
        {
            return fr.Map(DrCx1, DrEmblemY1, DrCx2, DrEmblemY2);
        }

        /// <summary>`Wildcard Drawer` 的 `Content/Label` 那一行。
        /// <para>结构照 prefab：`Label`（`40k_main_bt_nametag` 底板，**静态层、恒 active**）
        /// → `Quantity`（左半 · `H = Right`）+ `Name`（右半 · `H = Left`）。两颗 TMP 的出厂值逐格现读：
        /// `m_fontSize 75` / `m_fontSizeBase 36` / `m_fontSizeMin/Max 12/75` / `m_VerticalAlignment 512 = Middle` / 色白。</para>
        /// <para>⚠️ `Label` 上的 `HorizontalLayoutGroup`（padding L/R 25 · spacing 10 · forceExpand W/H = 1）
        /// 把两颗子件各摊到 `(640.8 − 50 − 10) / 2 = 290.4` 宽 —— 那三个数字**就是** `DrQtyX1…DrNameX2`
        /// 那组常量的来源，所以这里直接用常量映射，⛔ 不重新算一遍。</para>
        /// <para>🔴 与原版一致的两件「不画」：① 调用方把 `QuantityPx`/`NamePx` 置 0 时，对应那颗不建，
        /// **但另一半的矩形不变**（原版 prefab 两颗都在、布局不会因为我们的取舍而重排）；
        /// ② 底板取不到图时**只少画底板**（名字/数量照画），并出声 —— ⛔ 不拿别的图顶。</para></summary>
        internal static void WildcardLabelRow(Transform content, DrawerFrame fr, ItemSpec item, int qty, ItemDrawerStyle st)
        {
            var labelR = fr.Map(DrLbX1, DrLbY1, DrLbX2, DrLbY2);
            var tag = Tex(WildcardNametagSprite);
            Transform row;
            if (tag != null)
            {
                // 🔴 **`MenuDraw.Rect` 除 `tex == null` 外还有两条合法返 null 的路**（判据 = `MenuDraw.cs:1624-1700`）：
                //    ① `ClipRect(...)` 判「整块落在裁切区外」⇒ **直接 return null**（滚动区里滚出视口的格子**天天**命中它）；
                //    ② `ImageQuad.Create` 建不出（退化矩形）⇒ `if (quad == null) return null;`。
                //    ⇒ ⛔ **绝不许在返回值上 `.transform` 硬解** —— 2026-11-01 `RewardsScene.Run` 那条
                //    `NullReferenceException`（`ForgeTab.BuildRewardIcon` → 本函数）就是把这条当成「恒非 null」了。
                //    这里与全库同口径：**整块在视口外 ⇒ 连节点一起不建**（`Rect`/`Nine`/`Tiled`/`Hit`/`Text` 五条画路都这样）。
                var labelQuad = MenuDraw.Rect(content, tag, labelR, "Label", st.QBoard, null, false,
                                              st.Clip, st.ClipSoftness);
                if (labelQuad == null) return;      // 这一行三层（底板/数量/名字）共用同一个 `labelR` ⇒ 一起不建
                row = labelQuad.transform;
            }
            else
            {
                Note("wildcard|nametag|" + WildcardNametagSprite,
                     "`Wildcard Drawer` 的名条底板 `" + WildcardNametagSprite + "` 在 `Resources/` 里取不到"
                     + "（`CardArt.MenuUi` 三级全空）⇒ 这一层不画（名字与数量照画）");
                row = MenuDraw.Node(content, "Label", labelR);
            }

            float px = 75f * fr.K;      // 原版 `m_fontSize`（设计 px）→ 本格 px；三格 auto 同乘 `fr.K`
            if (st.QuantityPx > 0f && ItemDrawerOptions.Stackable(item))
            {
                var qr = fr.Map(DrQtyX1, DrLbY1, DrQtyX2, DrLbY2);
                ClippedText(row, qr, qty.ToString(), Color.white, NodeQuantity, px, st.QText, qr.W, st, 2,
                            12f * fr.K, 75f * fr.K, 36f * fr.K, true, TmpFont.Face.LatinCardBody);
            }
            if (st.NamePx > 0f && !string.IsNullOrEmpty(item.Label))
            {
                var nr = fr.Map(DrNameX1, DrLbY1, DrNameX2, DrLbY2);
                // 文本 = 原版 `WildcardDrawer.GetItemName`（`WildcardName`，判据见它在 ItemDrawer.cs 里的 doc）；
                // `align: 1` = Left —— 就是本档与原版对齐、而共用件给不出的那一格（见文件头）。
                ClippedText(row, nr, WildcardName(item), Color.white, "Name", px, st.QText, nr.W, st, 1,
                            12f * fr.K, 75f * fr.K, 36f * fr.K, true);
            }
        }

        // ══════════════════════════════════════════════════════════════════════════════════════════
        //  `Deck Drawer`（`B-293` 第 2 项）：主图那一层的**裁切矩形**
        //  判据 → `判据/B115_S4_三份Drawer规格.md` §五（`Deck Drawer.prefab` 的 `Deck Image(mask)` + 过扫的
        //  `Deck Image`）。`Deck()` 本体仍在 `Shell/ItemDrawer.cs`（既有成员 + 账落点钉死行号）。
        // ══════════════════════════════════════════════════════════════════════════════════════════

        /// <summary>`Deck Drawer` 的**主图**节点名 —— **按 prefab 逐字**（`Deck Image(mask)/Deck Image`）。
        /// <para>🔴 为什么不复用 `NodeIcon`：那个是**全库共用**常量（其它抽屉的主图都叫 `Icon`），改它会波及其它档
        /// ⇒ deck 这一档单开一个（`B-316`，2026-11-01）。</para></summary>
        internal const string NodeDeckImage = "Deck Image";

        /// <summary>`Deck Drawer` 的**名字**节点名 —— **按 prefab 逐字**（`Content/Label` 本身就是 `Deck Name`，
        /// 也就是 `ItemDrawerComponents.label` 指的那颗 TMP）。
        /// <para>🔴 为什么不复用 `NodeItemName`：同上，那个是全库共用的（`Placeholder` 等在用）⇒ deck 单开。</para></summary>
        internal const string NodeDeckName = "Deck Name";

        /// <summary>`Deck Drawer` 主图的**裁切矩形** = `Deck Image(mask)` 的矩形 ∩ 外层视口。
        /// <para>判据：原版那层是 `RectMask2D`（`m_Padding (0,0,0,0)`）套在**过扫**的 `Deck Image` 外面
        /// ⇒ 可见范围 = mask 矩形，而画的那张图是「拉伸到过扫框」的（`m_PreserveAspect 0`）。</para>
        /// <para>🔴 **为什么不直接把 `maskR` 传给 `MenuDraw.Rect` 的 `clip`**：那个形参是
        /// **显式覆盖**（`ViewportClip.Resolve` 第 ① 条：形参非空 ⇒ 形参赢）⇒ 直接传会把
        /// **外层滚动视口的裁切丢掉**（抽屉一旦挂进滚动区就会画到视口外面）。所以先 `Resolve` 出父链那一份、
        /// 与 `maskR` **求交**再传 —— 没有外层裁切时结果就是 `maskR`。</para>
        /// <para>⚠️ 交集为空 ⇒ 返回**退化矩形**（宽高 0）：`MenuDraw.Rect` 的 `ClipRect` 守卫会判「不可见」、
        /// 连节点都不建（与其它四条画路同口径）。</para></summary>
        internal static PxRect? DeckMaskClip(Transform parent, PxRect maskR, ItemDrawerStyle st)
        {
            var outer = ViewportClip.Resolve(parent, null, st.ClipSoftness, default(Vector4));
            if (!outer.RenderClip.HasValue) return maskR;
            var o = outer.RenderClip.Value;
            float x1 = Mathf.Max(o.x1, maskR.x1), y1 = Mathf.Max(o.y1, maskR.y1);
            float x2 = Mathf.Min(o.x2, maskR.x2), y2 = Mathf.Min(o.y2, maskR.y2);
            if (x2 <= x1 || y2 <= y1) return new PxRect(maskR.CX, maskR.CY, maskR.CX, maskR.CY);
            return new PxRect(x1, y1, x2, y2);
        }
    }
}
