// ============================================================================ 货币抽屉（原版 `CurrencyDrawer`）
//
//  为什么单独一件：货币是**唯一一个「主图 + 名条行 + 小图标 + 数量」四件套**的抽屉
//  （`CurrencyDrawer.Draw` + `GetItemName` + `if ((bool)icon) icon.sprite = item.GetIcon(Small)`），
//  而我们原来的通用 `Icon` 抽屉只有「主图 + 数量」。硬把这两颗塞进 `Icon` 的版式 = **编一个原版没有的排布**
//  ⇒ 照 prefab 的**整棵子树**实现这一件，`Icon` 那一行只做「认出是货币就转交」。
//
//  ── 几何（逐项，⛔ 没有一个数是我目测调的）──────────────────────────────────────
//  判据 = `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Currency Drawer"`
//  （**prefab 局部坐标 · 1920×1080 设计基准 · 左上原点 · y 向下**，与我们的 `PxRect` 同一口径 ⇒ **不需要换算**）：
//     Currency Drawer   (603.6, 0) → (1316.4, 1080)   712.8×1080 · `AspectRatioFitter` **高控宽 0.66**
//       Content         (604.0, 0) → (1316.0, 1080)
//         Background    (604.0, 0) → (1316.0, 1080)    sprite `40k_Rewards_icon_base card`（Simple · 非 preserveAspect）
//         Image         (604.0, 0) → (1316.0,  864)    主图槽；**`×0.9`（绕中心）⇒ 视觉 640.8×777.6** · preserveAspect
//         Label         (639.6,810) → (1280.4, 918)    名条底 `40k_main_bt_nametag`（Simple × 拉伸）
//                       ＋ `HorizontalLayoutGroup`：**spacing 10 · padding (25,25,0,0) · align 4 (MiddleCenter)**
//           Icon        (901.0,810) → (1009.0, 918)    **小图标槽 = 108²**（preserveAspect）
//           Quantity    (1019.0,810) → （宽 0，由布局给） 数量：`fs 100` · 基准 `36` · auto `12~100` · **Left/Midline**
//     （同一份 dump 里还有 `Converted Drawer` / `Ephemeral Drawer` / `Collected Badge` / `Premium Highlight`，
//      那几个出厂 `m_IsActive = 0`，且**不是** `Draw` 这一跳的件 ⇒ 本件不建，⛔ 别顺手补上。）
//
//  ── 三条如实记的边界 ────────────────────────────────────────────────────────────
//  ① 🔴 **2026-10-30 就地订正（铁律 5 · `B101_E3`）**：本行原来写「背景图**不在** `Resources/`」—— **已过期**：
//     `B100_F3` 已把这族导进 `Resources/Art/ui_menu/`（现读 6 张全在：`40k_Rewards_icon_base_card` + `_common/_rare/_epic/_legendary` + `_SDF`）
//     ⇒ `CardArt.MenuUi` 取得到、背景层**会画**；下面那段「取不到 ⇒ 不画 + 出声一次」的兜底**保留**（图上真缺时仍不静默、⛔ 不拿别的图冒充）。
//     ⚠️ 原句「源图在 `CardPresentation/Art/原版/0_mainmenu/`、没跑导入腿」是**当时**的事实，只留作订正痕迹。
//  ② **ARF 的兜底是 TODO（还没查清，⛔ 不是「自己设计」）**：原版「高控宽 0.66」= 高度定宽度；容器比抽屉
//     **更窄**时 ARF 怎么缩**没读过**（`AspectRatioFitter` 是 Unity 内置组件、行为必可查）⇒ 暂按
//     `min(box.H, box.W / 0.66)` 近似（见下面 `ar` 那两行）。
//  ③ **HLG 的居中是真做的**（不是按 dump 的静态位贴）：先建数量量出渲染宽、再算整组起点、再摆小图标
//     并把数量左对齐到「起点 + 图标宽 + spacing」。⇒ 数量串变长时**小图标会跟着往左移**，与原版一致。
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        // ---- 节点名：**照 prefab 逐字**（`menu_dump` 的树里就是这几个名字；自检按它们取件）----
        public const string NodeCurrencyContent = "Content";
        public const string NodeCurrencyBackground = "Background";
        public const string NodeCurrencyImage = "Image";
        public const string NodeCurrencyLabel = "Label";
        /// <summary>小图标槽（原版 `Content/Label/Icon`）—— 与通用抽屉的 `NodeIcon` **同名不同层**。</summary>
        public const string NodeCurrencySmallIcon = NodeIcon;

        // ---- 几何常量（prefab 局部 · 设计 px · y 向下；出处见文件头那张表）----
        const float CurRootX1 = 603.6f, CurRootX2 = 1316.4f, CurRootW = CurRootX2 - CurRootX1;   // 712.8
        const float CurRootH = 1080f;                                                            // `AspectRatioFitter` 0.66
        const float CurContentX1 = 604f, CurContentX2 = 1316f;
        const float CurImageY2 = 864f;                                                           // 主图槽底边
        const float CurImageScale = 0.9f;                                                        // ×0.9（绕中心）
        const float CurLabelX1 = 639.6f, CurLabelX2 = 1280.4f, CurLabelY1 = 810f, CurLabelY2 = 918f;
        const float CurLabelPadL = 25f, CurLabelPadR = 25f;                                      // HLG padding (25,25,0,0)
        const float CurHlgSpacing = 10f;                                                         // HLG spacing
        const float CurSmallIconSide = 108f;                                                     // `Content/Label/Icon` = 108²
        const float CurQtyX1 = 1019f;                                                            // 数量起点（= 图标右边 + spacing）
        const float CurQtyFontPx = 100f, CurQtyBasePx = 36f, CurQtyMinPx = 12f, CurQtyMaxPx = 100f;
        /// <summary>名条底与背景的 sprite 名（原版 `m_Sprite` 实读）。</summary>
        const string CurNametagSprite = "40k_main_bt_nametag", CurBackgroundSprite = "40k_Rewards_icon_base_card";

        /// <summary>最近一次画货币抽屉时**实测出来的五件 + 三个矩形** —— 自检读口（宿主按它断「哪件有没有、位置对不对」）。
        /// <para>⚠️ 全是**建完之后实测**的值（小图标/数量那两格是 HLG 排完的结果，不是常量）⇒ 自检拿它比绝对坐标才有意义。</para></summary>
        public struct CurrencyParts
        {
            public bool Background, MainImage, Nametag, SmallIcon, Quantity;
            public PxRect LabelRow, SmallIconRect, QuantityRect;
        }
        /// <summary>读口：最近一次 `DrawCurrency` 的实测结果（静态 ⇒ 批处理里连画多件时读最后一次）。</summary>
        public static CurrencyParts LastCurrency;

        /// <summary>货币抽屉一整件（原版 `CurrencyDrawer.Draw`）。由 <see cref="Icon"/> 按「item.Id 是货币 code」转交。</summary>
        static void DrawCurrency(Transform node, PxRect box, ItemSpec item, int quantity,
                                 ItemDrawerStyle st, ref ItemDrawResult res)
        {
            var tex = Tex(item.Art);
            if (tex == null)                                   // 主图都取不到 ⇒ 与通用 `Icon` 一样退占位板
            {
                Placeholder(node, box, item, quantity, st, ref res);
                res.MissingArt = true;
                return;
            }
            res.Art = item.Art;
            LastCurrency = new CurrencyParts();

            // `AspectRatioFitter` 高控宽 0.66：k = 每一设计 px 对应多少实际 px
            // TODO（还没查清，⛔ 不是「自己设计」）：容器比抽屉**更窄**时按宽缩 —— 原版 ARF 在这种容器下的行为**未读**，
            //   这里取 min(高, 宽/0.66) 作兜底；容器够宽时与原版逐像素相同（这才是常态）。
            float ar = CurRootW / CurRootH;                    // 0.66
            float k = Mathf.Min(box.H, box.W / ar) / CurRootH;
            float w = CurRootW * k, h = CurRootH * k;
            float ox = box.CX - w * 0.5f, oy = box.CY - h * 0.5f;

            // prefab 局部 → 实际像素（x 先减根原点，再乘尺度）
            System.Func<float, float, float, float, PxRect> M = (x1, y1, x2, y2) =>
                new PxRect(ox + (x1 - CurRootX1) * k, oy + y1 * k, ox + (x2 - CurRootX1) * k, oy + y2 * k);

            var content = MenuDraw.Node(node, NodeCurrencyContent, M(CurContentX1, 0f, CurContentX2, CurRootH));

            // ① 背景（`40k_Rewards_icon_base card`，Simple 非 preserveAspect）—— 图不在 `Resources/` ⇒ 出声一次、不画
            {
                var bgTex = CardArt.MenuUi(CurBackgroundSprite);
                if (bgTex != null)
                {
                    MenuDraw.Rect(content, bgTex, M(CurContentX1, 0f, CurContentX2, CurRootH),
                                  NodeCurrencyBackground, st.QBoard, null, false, st.Clip, st.ClipSoftness);
                    LastCurrency.Background = true;
                }
                else
                    Note("currency|bg", "货币抽屉的背景图 `" + CurBackgroundSprite + "` **不在 `Resources/`**"
                        + "（源图在 `CardPresentation/Art/原版/0_mainmenu/`，要跑 `工具/import_original_art.py --only-menu`）"
                        + "⇒ 这一层**不画**、不拿别的图冒充");
            }

            // ② 主图：槽 (604,0)-(1316,864)，**×0.9 绕中心**
            {
                var r = M(CurContentX1, 0f, CurContentX2, CurImageY2);
                float iw = r.W * CurImageScale, ih = r.H * CurImageScale;
                var vis = new PxRect(r.CX - iw * 0.5f, r.CY - ih * 0.5f, r.CX + iw * 0.5f, r.CY + ih * 0.5f);
                MenuDraw.Rect(content, tex, vis, NodeCurrencyImage, st.QArt, null, true, st.Clip, st.ClipSoftness);
                LastCurrency.MainImage = true;
            }

            // ③ 名条行（底图 `40k_main_bt_nametag` 拉伸铺满）+ HLG 里那两件
            var labelR = M(CurLabelX1, CurLabelY1, CurLabelX2, CurLabelY2);
            var row = MenuDraw.Node(content, NodeCurrencyLabel, labelR);
            LastCurrency.LabelRow = labelR;
            {
                var tag = CardArt.MenuUi(CurNametagSprite);
                if (tag != null)
                {
                    MenuDraw.Rect(row, tag, labelR, "Nametag", st.QBoard, null, false, st.Clip, st.ClipSoftness);
                    LastCurrency.Nametag = true;
                }
                else
                    Note("currency|tag", "名条底 `" + CurNametagSprite + "` 取不到 ⇒ 名条只有字/图标（`CardArt.MenuUi` 三档都空）");
            }

            // 数量：先按 dump 的静态位建出来**量渲染宽**（HLG 的 align=4 是「整组居中」⇒ 得先知道数量有多宽）
            float side = CurSmallIconSide * k;
            float innerW = labelR.W - (CurLabelPadL + CurLabelPadR) * k;
            var qtyR0 = M(CurQtyX1, CurLabelY1, math_MaxQtyX2(CurLabelX2), CurLabelY2);
            Label qtyLb = null;
            float qtyW = 0f;
            if (st.QuantityPx > 0f && ItemDrawerOptions.Stackable(item))
            {
                qtyLb = ClippedText(row, qtyR0, quantity.ToString(), Color.white, NodeQuantity,
                                    CurQtyFontPx * k, st.QText, qtyR0.W, st, align: 1,
                                    autoMinPx: CurQtyMinPx * k, autoMaxPx: CurQtyMaxPx * k, autoBasePx: CurQtyBasePx * k,
                                    wrapOff: true, face: TmpFont.Face.LatinCardBody);
                if (qtyLb != null) qtyW = qtyLb.WorldW * 108f;
            }

            // HLG：整组 [小图标][spacing][数量] 在名条内宽里居中（`align = 4 MiddleCenter`）
            float groupW = side + (qtyLb != null ? CurHlgSpacing * k + qtyW : 0f);
            float startX = labelR.x1 + CurLabelPadL * k + Mathf.Max(0f, (innerW - groupW) * 0.5f);

            // ④ 小图标槽（108²，竖向铺满名条行）：图名来自 `CurrencyNames.Small`（guid 链，见那个文件）
            {
                var small = CurrencyNames.Small(item.Id);
                var smallTex = small != null ? CardArt.MenuUi(small) : null;
                if (smallTex != null)
                {
                    var ir = new PxRect(startX, labelR.y1, startX + side, labelR.y1 + side);
                    MenuDraw.Rect(row, smallTex, ir, NodeCurrencySmallIcon, st.QArt, null, true, st.Clip, st.ClipSoftness);
                    LastCurrency.SmallIcon = true;
                    LastCurrency.SmallIconRect = ir;
                }
                else
                    Note("currency|small|" + (item.Id ?? "?"),
                         "货币 `" + (item.Id ?? "?") + "` 的小图标取不到（`CurrencyNames.Small` = "
                         + (small ?? "判据空") + "）⇒ 这一颗**不画**（⛔ 不拿主图顶替）");
            }

            // ⑤ 数量挪到「图标右边 + spacing」（HLG 的排布结果；量完才知道起点 ⇒ 只能建完再挪）
            if (qtyLb != null)
            {
                float qx = startX + side + CurHlgSpacing * k;
                MenuDraw.AlignLeft(qtyLb, new PxRect(qx, labelR.y1, qx + Mathf.Max(qtyW, 1f), labelR.y2));
                LastCurrency.Quantity = true;
                LastCurrency.QuantityRect = new PxRect(qx, labelR.y1, qx + Mathf.Max(qtyW, 1f), labelR.y2);
            }
        }

        /// <summary>数量那颗的**测量用**右边界：名条内宽右端（= `Label.x2 − paddingRight`）。
        /// 单独拎出来只为一件事：`MenuDraw.Text` 的 `wrapPx` 必须是**正数**，否则 auto-fit 整条闸不生效。</summary>
        static float math_MaxQtyX2(float labelX2) { return labelX2 - CurLabelPadR; }

        /// <summary>原版抽屉类名 `CurrencyDrawer`（= `ItemTypeSets` 里 `Currency` 那一条的主档与 `Icon` 档）。
        /// <para>🔴 **2026-10-30（`B101_E3`）补登记**（施工单 §一-3 / 表 A6）：这一档的实现早就在本文件里，
        /// 但 `DrawerImpls` 里**没有它** ⇒ `PickDrawer` 走兜底（落到 `IconDrawer`）再在 `Icon()` 里转交
        /// `DrawCurrency`；功能到得了、但路上会打一条「这个抽屉我们没实现」的**假告警**
        /// （`Note("impl|" + cls)`，key = `impl|CurrencyDrawer`）。现在三处（`DrawerImpls` / `Draw` 的 switch /
        /// `Dispatch` 的 switch）都登记了 ⇒ 直接命中，且 `Drawer` 字段报的是**原版类名**。</para></summary>
        public const string DrawerCurrency = "CurrencyDrawer";
    }
}
