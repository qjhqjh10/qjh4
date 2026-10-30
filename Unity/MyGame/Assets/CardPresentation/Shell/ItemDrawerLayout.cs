// ============================================================================ 抽屉共用版面尺（`ItemDrawerLayout`）
//
//  为什么单开一份：本轮补齐的 17 档抽屉（判据 → `资料/完全复刻/判据/B100_D1_抽屉18档_施工单.md` 表 B）
//  共用**同一套 prefab 版面**：根 712.8×1080（`AspectRatioFitter` 高控宽 0.66）+ `Content` 712×1080
//  + `Content/Background` 铺满 + `Content/Image`（多数档 (0.4,216)-(712.4,648)）+ `Content/Army`
//  + `Content/Label`（`HorizontalLayoutGroup`：`Quantity` 右对齐在左半、`Name` 左对齐在右半）。
//  13 份 prefab 逐份 `menu_dump` 现读（2026-10-30 · B101_E3），值**逐份相同**才写进这里；
//  ⛔ 别在任何一档里再抄一遍（`CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」）。
//
//  ── 判据（`python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "<prefab>" --depth 4 --relative --no-sprite --no-ancestor-scale`）──
//    · 根 712.8×1080 / ARF 0.66：`Avatar Drawer` · `Container Drawer` · `Premium Drawer` ·
//      `Expansion Pass Premium Drawer` · `Forge Points Drawer` · `Expansion Pass Points Drawer` ·
//      `Alliance Badge Drawer` · `XSolla Offer Drawer` · `Avatar Border Drawer` · `Random Card Drawer`（10 份）
//    · Icon 变体（`Icon Title Drawer Variant` / `Icon VIP Premium Campaign Drawer Variant` /
//      `Icon Forge Points Drawer Variant`）：根 **1080×1080**、ARF **1**，`Content` 1080×1080 ⇒ 用 `FrameOf(box, 1080, 1080)`
//    · `Cardback Drawer`：根 235.09×356.19 + **自带 `↻-7.27°`**（旋转），`Content` 仍是 712×1080 —— 它走 `FrameOf` 的默认根
//    · `Title Drawer`：根 174.18×263.92（`Content` 被 `UIScaleToFit` ×0.2444 缩到框里）—— 设计空间仍是 712×1080
//
//  ── 三处「如实记」（⛔ 别把它们当成原版值）─────────────────────────────────────
//  ① **`K` 的算式**：原版是 `AspectRatioFitter`（高控宽 0.66），我们按
//     `K = min(box.H / 1080, box.W / 712.8)`（= 等比内接）折算。原版 ARF 在**容器更窄**时的行为
//     （`m_AspectMode = HeightControlsWidth` 在宽不够时是**溢出**还是**按宽缩**）**没读过**
//     —— 与 `ItemDrawerCurrency.cs` 文件头 ② 同一条 TODO，⛔ 不是「自己设计」、是**还没查清**。
//  ② **`Background` 那张图**：`40k_Rewards_icon_base card`（落盘名空格→下划线 = `40k_Rewards_icon_base_card`）。
//     📌 **2026-10-30 `B100_F3` 已把它 + `_common/_rare/_epic/_legendary` 导进 `Resources/Art/ui_menu/`**
//     （现读：6 张全在）⇒ `ItemDrawerCurrency.cs` 文件头 ① 那条「不在 `Resources/`」**已解除**。
//  ③ **`Army` 那一行**：字号 75 / 基准 36 / auto[12~75] / 居中 / 折行 1 / 色 (0.689,0.689,0.689,0.851)
//     —— 逐份 prefab 实读，值相同。
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        // ---- 节点名：**照 prefab 逐字**（自检按它们取件；`NodeIcon` 那一套是我们旧名，两套并存）----
        /// <summary>`Content` 容器（每份抽屉 prefab 都有这一层）。</summary>
        public const string NodeContent = "Content";
        /// <summary>`ItemDrawerComponents.background` 那件（sprite 恒 `40k_Rewards_icon_base card`）。</summary>
        public const string NodeBackground = "Background";
        /// <summary>`ItemDrawerComponents.image` 那件（各档各自的「主图」）。</summary>
        public const string NodeImage = "Image";
        /// <summary>`Content/Army`（阵营名 TMP）。</summary>
        public const string NodeArmy = "Army";
        /// <summary>`Content/Label/Name`（物品名 TMP）。</summary>
        public const string NodeName = "Name";
        /// <summary>`Content/Frame`（`Alliance Badge Drawer` 的档位框）。</summary>
        public const string NodeTrophyFrame = "Frame";
        /// <summary>`Content/Frame/Trophy Icon`（`Alliance Badge Drawer` 的徽章本体）。</summary>
        public const string NodeTrophyIcon = "Trophy Icon";
        /// <summary>`Content/Title`（`Title Drawer` 的称号正文）。</summary>
        public const string NodeTitleText = "Title";
        /// <summary>`additionalArmyIconForEffect`（`Title Drawer` 的第二颗阵营徽记，画在 `image` 之上）。</summary>
        public const string NodeArmyIconExtra = "Army Icon Extra";
        /// <summary>`Content/Collected Badge`（`claimedWarning` 那件；原版 prefab 里是**空节点**）。</summary>
        public const string NodeCollectedBadge = "Collected Badge";

        // ---- 版面常量（prefab 设计 px · 左上原点 · y 向下；出处见文件头）----
        /// <summary>抽屉根的宽（`AspectRatioFitter` 0.66 的那一侧）。</summary>
        public const float DrRootW = 712.8f;
        /// <summary>抽屉根的高（每一份基础抽屉都一样）。</summary>
        public const float DrRootH = 1080f;
        /// <summary>Icon 变体（ARF 1）的根边长。</summary>
        public const float DrIconRoot = 1080f;
        /// <summary>`Content` 的左右沿（0.4 / 712.4 —— 与根差 0.4 px，逐份相同）。</summary>
        const float DrCx1 = 0.4f, DrCx2 = 712.4f;
        /// <summary>`Content/Image` 的默认矩形（`Avatar` / `Container` / `XSolla` / `Title` 例外，见各自文件）。</summary>
        const float DrImgY1 = 216f, DrImgY2 = 648f;
        /// <summary>`Content/Army` 那一行。</summary>
        const float DrArmyY1 = 702f, DrArmyY2 = 810f;
        /// <summary>`Content/Label` 那一行（`HorizontalLayoutGroup`：padding L/R 25 · spacing 10）。</summary>
        const float DrLbY1 = 810f, DrLbY2 = 918f, DrLbX1 = 36f, DrLbX2 = 676.8f;
        /// <summary>`Label/Quantity`（左半、右对齐）与 `Label/Name`（右半、左对齐）—— 两份子件的布局位。</summary>
        const float DrQtyX1 = 61f, DrQtyX2 = 351.4f, DrNameX1 = 361.4f, DrNameX2 = 651.8f;
        /// <summary>共享底图（落盘名：空格→下划线）。出处 = 各 prefab 的 `Content/Background.m_Sprite`。</summary>
        const string DrBackgroundSprite = "40k_Rewards_icon_base_card";
        /// <summary>`Content/Army` 的 `m_Color`（prefab 实读 · 逐份相同）。</summary>
        internal static readonly Color DrArmyInk = new Color(0.689f, 0.689f, 0.689f, 0.851f);

        /// <summary>「设计 px → 本格 px」的换算（根矩形 + 缩放）。</summary>
        internal struct DrawerFrame
        {
            /// <summary>每个设计 px 对应多少实际 px。</summary>
            public float K;
            /// <summary>根矩形左上角的屏上位置。</summary>
            public float OX, OY;
            /// <summary>设计 px 的矩形 → 本格 px（左上原点 · y 向下，与本工程 `PxRect` 同一口径）。</summary>
            public PxRect Map(float x1, float y1, float x2, float y2)
            {
                return new PxRect(OX + x1 * K, OY + y1 * K, OX + x2 * K, OY + y2 * K);
            }
            /// <summary>把一个矩形**绕自己的中心**放大 `s` 倍（原版 `m_LocalScale` 那一档，如 `×1.2` / `×1.25`）。</summary>
            public static PxRect Scale(PxRect r, float s)
            {
                float w = r.W * s, h = r.H * s;
                return new PxRect(r.CX - w * 0.5f, r.CY - h * 0.5f, r.CX + w * 0.5f, r.CY + h * 0.5f);
            }
        }

        /// <summary>按「抽屉根盖在这一格框里」算出 <see cref="DrawerFrame"/>。
        /// 默认根 = 712.8×1080（ARF 0.66）；Icon 变体传 (1080,1080)。</summary>
        internal static DrawerFrame FrameOf(PxRect box, float rootW = DrRootW, float rootH = DrRootH)
        {
            float k = Mathf.Min(box.H / rootH, box.W / rootW);      // TODO（见文件头 ①）：ARF 在窄容器下的行为未读
            var fr = new DrawerFrame { K = k };
            fr.OX = box.CX - rootW * k * 0.5f;
            fr.OY = box.CY - rootH * k * 0.5f;
            return fr;
        }

        /// <summary>画 `Content` 容器（原版每一份抽屉都有；我们建出来当子件的父亲，节点名逐字）。
        /// ⚠️ 原版 `Content` 带 `UIScaleToFit`（把 712×1080 缩进整根框）—— 我们的 `FrameOf` 已经把缩放算进 `K`
        /// ⇒ 这里只建一个**同尺寸的纯容器**，不再叠一层缩放。</summary>
        internal static Transform DrawerContent(Transform node, DrawerFrame fr)
        {
            return MenuDraw.Node(node, NodeContent, fr.Map(DrCx1, 0f, DrCx2, DrRootH));
        }

        /// <summary>Icon 变体（根 1080×1080）的 `Content` —— 那一族的 `Content` 是 **1080² 铺满**，
        /// 与基础档的 712×1080 **不同**（`Icon Title Drawer Variant` / `Icon VIP Premium Campaign Drawer Variant` /
        /// `Icon Forge Points Drawer Variant` 三份逐份实读）。</summary>
        internal static Transform DrawerIconContent(Transform node, DrawerFrame fr)
        {
            return MenuDraw.Node(node, NodeContent, fr.Map(0f, 0f, DrIconRoot, DrIconRoot));
        }

        /// <summary>画 `Content/Background`（**每一档都有、sprite 恒 `40k_Rewards_icon_base card`**）。
        /// 取不到图 ⇒ 出声一次、**不画**（⛔ 不拿别的图顶替）。</summary>
        internal static bool DrawerBackground(Transform content, DrawerFrame fr, ItemDrawerStyle st)
        {
            var tex = Tex(DrBackgroundSprite);
            if (tex == null)
            {
                Note("drawerbgsprite|" + DrBackgroundSprite,
                     "抽屉共享底图 `" + DrBackgroundSprite + "` 在 `Resources/` 里取不到"
                     + "（`CardArt.MenuUi` 三级全空；导入口 = `工具/sync_battle_ui_art.py`）⇒ 这一层**不画**");
                return false;
            }
            MenuDraw.Rect(content, tex, fr.Map(DrCx1, 0f, DrCx2, DrRootH), NodeBackground,
                          st.QBoard, null, false, st.Clip, st.ClipSoftness);
            return true;
        }

        /// <summary>画 `Content/Army`（原版 `armyText.text = GameStaticData.CardArmyToString(army)`）。
        /// 我们的等价物 = `ArmyIcons.LocalizedName`（同一句 `Armies/&lt;枚举名&gt;` 词条，见 `Core/ArmyIcons.cs`）。
        /// `Neutral` / 认不出 ⇒ 原版那串是空、这一层不画（⛔ 不拿默认值顶）。</summary>
        internal static void DrawerArmyName(Transform content, DrawerFrame fr, int army, ItemDrawerStyle st)
        {
            if (st.NamePx <= 0f) return;                      // 调用方不要文字层
            string name = ArmyIcons.LocalizedName(army);
            if (string.IsNullOrEmpty(name)) return;
            var r = fr.Map(DrCx1, DrArmyY1, DrCx2, DrArmyY2);
            ClippedText(content, r, name, DrArmyInk, NodeArmy, 75f * fr.K, st.QText, r.W, st, 0,
                        12f * fr.K, 75f * fr.K, 36f * fr.K);
        }

        /// <summary>画 `Content/Label` 那一行（`HorizontalLayoutGroup` 里的 `Quantity` + `Name`）。
        /// <paramref name="hasName"/> / <paramref name="hasQuantity"/> = **这一段 prefab 上那颗槽是不是 null**
        /// （判据 → 施工单 §三-2 的 null 表；`Avatar` / `Premium` / `Title` 等档没有 `Quantity` 子件）。
        /// ⚠️ 两个槽都在时按 prefab 的左右半排（左半 `Quantity` 右对齐、右半 `Name` 左对齐）；
        /// 只有 `Name` 时铺满（prefab 实测：`Name` 从 61 起）。</summary>
        internal static void DrawerNameQuantity(Transform content, DrawerFrame fr, ItemSpec item, int qty,
                                                ItemDrawerStyle st, bool hasName, bool hasQuantity)
        {
            if (!hasName && !hasQuantity) return;          // 两颗槽都 null（`Cardback` / `TitleIcon` / `PremiumIcon`）⇒ 连容器都不建
            var rowNode = MenuDraw.Node(content, "Label", fr.Map(DrLbX1, DrLbY1, DrLbX2, DrLbY2));
            float px = 75f * fr.K;
            if (hasQuantity && st.QuantityPx > 0f && ItemDrawerOptions.Stackable(item))
            {
                var qr = fr.Map(DrQtyX1, DrLbY1, DrQtyX2, DrLbY2);
                ClippedText(rowNode, qr, qty.ToString(), Color.white, NodeQuantity, px, st.QText, qr.W, st, 2,
                            12f * fr.K, 75f * fr.K, 36f * fr.K, true, TmpFont.Face.LatinCardBody);
            }
            if (hasName && st.NamePx > 0f && !string.IsNullOrEmpty(item.Label))
            {
                var nr = hasQuantity ? fr.Map(DrNameX1, DrLbY1, DrNameX2, DrLbY2)
                                     : fr.Map(DrQtyX1, DrLbY1, DrNameX2, DrLbY2);
                ClippedText(rowNode, nr, item.Label, Color.white, NodeName, px, st.QText, nr.W, st, 0,
                            12f * fr.K, 75f * fr.K, 36f * fr.K, true);
            }
        }

        /// <summary>把 `ItemDrawerComponents.image` 那一层画出来（取不到 ⇒ 出声 + `res.MissingArt`，不画）。</summary>
        internal static bool DrawerImage(Transform content, PxRect r, Texture2D tex, string artName,
                                         ItemDrawerStyle st, ref ItemDrawResult res, string name = NodeImage,
                                         int qAdd = 0)
        {
            if (tex == null) { res.MissingArt = true; return false; }
            MenuDraw.Rect(content, tex, r, name, st.QArt + qAdd, null, true, st.Clip, st.ClipSoftness);
            res.Art = artName;
            return true;
        }

        // ============================================================ B0：基类那四句「复位」（施工单 §一-2 / §四-3）
        //
        //  原版 `ItemDrawer<T>.Draw`（`decomp_generic_trial/out/ItemDrawer_object___Draw.c:20-21,116-117`）
        //  **进门先复位四个装饰/转换状态**：
        //    `SetConvertedItem(null)`（虚表槽 10）· `SetEphemeralDisplay(null)`（槽 11）·
        //    `ToggleClaimedWarning(false)`（槽 7）· `TogglePremiumHighlight(false)`（槽 8）。
        //  我们这四个状态**不挂在抽屉组件上**，而是「调用方事后按需叠」⇒ 唯一会留下陈旧装饰的路径是
        //  「同一个节点重画」；而 `ItemDrawerView.Refresh()` 走的是 `MenuDraw.ClearChildren(Node)`
        //  （**更彻底**：那四层直接被删掉）⇒ 复位这一步在今天的实现里**恒为无副作用的空操作**，
        //  保留它是为了**保持与原版相同的调用形状**（顺序 + 次数），将来若改成「复用子树」不会漏。
        //  ⚠️ 呼点插在 `Draw` 的「③ Instantiate」与「④ Draw」之间（照施工单原文），**同一物理行**追加。
        internal static void ResetDecorationLayers(Transform node)
        {
            if (node == null) return;
            DeactivateChild(node, NodeConvertedDrawer);
            DeactivateChild(node, NodeEphemeralDrawer);
            DeactivateChild(node, NodeCollectedBadge);
            DeactivateChild(node, NodePremiumHighlight);
        }

        static void DeactivateChild(Transform node, string name)
        {
            var t = node.Find(name);
            if (t != null) t.gameObject.SetActive(false);
        }
    }
}
