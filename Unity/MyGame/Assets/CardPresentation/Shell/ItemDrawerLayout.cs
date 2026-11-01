// ============================================================================ 抽屉共用版面尺（`ItemDrawerLayout`）
//
//  为什么单开一份：本轮补齐的 17 档抽屉（判据 → `资料/完全复刻/判据/B100_D1_抽屉18档_施工单.md` 表 B）
//  共用**同一套 prefab 版面**：根 712.8×1080（`AspectRatioFitter` 高控宽 0.66）+ `Content` 712×1080
//  + `Content/Background` 铺满（⚠️ `Cardback Drawer` 例外：`background` 槽 = null、Content 下无 Background 节点 —— 2026-10-31 `B108_R6` 现读）+ `Content/Image`（多数档 (0.4,216)-(712.4,648)）+ `Content/Army`
//  + `Content/Label`（`HorizontalLayoutGroup`：`Quantity` 右对齐在左半、`Name` 左对齐在右半）。
//  13 份 prefab 逐份 `menu_dump` 现读（2026-10-30 · B101_E3），值**逐份相同**才写进这里；
//  ⛔ 别在任何一档里再抄一遍（`CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」）。
//
//  ── 判据（`python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "<prefab>" --depth 4 --relative --no-sprite --no-ancestor-scale`）──
//    · 根 712.8×1080 / ARF 0.66：`Avatar Drawer` · `Container Drawer` · `Premium Drawer` ·
//      `Expansion Pass Premium Drawer` · `Forge Points Drawer` · `Expansion Pass Points Drawer` ·
//      `Alliance Badge Drawer` · `XSolla Offer Drawer` · `Avatar Border Drawer` · `Random Card Drawer`（10 份）
//    · 🔴 **Icon 变体【不是一个统一的根比例】—— 按 prefab 分三档**（⚠️ **2026-11-01 `B114_W48` 就地订正（铁律 5）**：
//      本处原来写「Icon 变体（`Icon Title Drawer Variant` / `Icon VIP Premium Campaign Drawer Variant` /
//      `Icon Forge Points Drawer Variant`）：根 **1080×1080**、ARF **1**，`Content` 1080×1080 ⇒ 用 `FrameOf(box, 1080, 1080)`」
//      —— **对一半以上不成立**：① 只点了 13 份里的 3 份；② 根比例不止一种；③ 那 7 份「比例 1」的 `Content`
//      **序列化并不是 1080×1080**（是锚 0..1 **拉伸**到正方形根上，`1080` 只是我们取的设计边长）。
//      判据 = `d:/2/tools/assetripper_out/ExportedProject/Assets/<prefab>.prefab` 里**根**那个 GameObject
//      （`m_Father: {fileID: 0}` 的 Transform 所指）上的 `!u!114`；⛔ **别读 `assets_full` 的同名 GameObject** ——
//      那一份有 **30+ 个嵌套副本**、根不是它（`Icon Campaign Points Drawer Variant` 就有 **36 份同名文件**：
//      34 份 mode 3 + 2 份 mode 2，根不在其中）。
//      13 份 `Icon … Drawer Variant` 现读 = **三档**：
//      (a) **ARF 高控宽 2 · 比例 1**（根 = 父高 × 1 ⇒ 正方形；`Content` 锚 0..1 铺满它，设计空间边长取 1080）**7 份** ——
//          `Icon Avatar` · `Icon Container` · `Icon Currency` · `Icon Forge Points` · `Icon Title` ·
//          `Icon VIP Premium Campaign` · `Icon Wildcard`
//          ⇒ `FrameOf(box, DrIconRoot, DrIconRoot, RootFit.HeightControlsWidth)`；
//      (b) **ARF 高控宽 2 · 比例 0.66**（根 712.8×1080 = **基础档同一套设计空间**）**4 份** ——
//          `Icon Avatar Border` · `Icon Expansion Pass Premium` · `Icon Premium Campaign` · `Icon Random Card`
//          ⇒ **默认根** `FrameOf(box, fit: RootFit.HeightControlsWidth)`（⛔ 别传 1080）；
//      (c) **ARF FitInParent 3 · 比例 1**（根锚 `(0,0)` + `sizeDelta 0×0`；`Content` 序列化 1080×1080、锚 (0.5,0.5)；
//          原版那一刻 = **等比内接**）**2 份** —— `Icon Campaign Points` · `Icon Expansion Pass Points`
//          ⇒ `FrameOf(box, DrIconRoot, DrIconRoot)`（= `Contain`，⛔ 别迁）。
//      ⇒ **谁要按「Icon = 1080²」写代码，先按 prefab 名查这张三档表**；逐处落点 →
//        `资料/完全复刻/报告/B114_W47_ARF迁移.md` §一（那 7+4 份怎么传）与 §二（那 2 份为什么留 `Contain`）。
//    · `Cardback Drawer`：根 235.09×356.19（**根 `m_LocalRotation` = identity；`-7.27°` 是 `General Basic Offer Popup` 实例上的装饰，不在 prefab 根** —— 2026-10-31 `B108_R6` 现读），`Content` 仍是 712×1080 —— 它走 `FrameOf` 的默认根
//    · `Title Drawer`：根 174.18×263.92（`Content` 被 `UIScaleToFit` ×0.2444 缩到框里）—— 设计空间仍是 712×1080
//
//  ── 三处「如实记」（⛔ 别把它们当成原版值）─────────────────────────────────────
//  ① **`K` 的算式**（✅ **2026-11-01 `B114_W32` 已读完，不再是 TODO**）：
//     原版根上挂 `AspectRatioFitter`，`m_AspectMode 2` = **HeightControlsWidth**。UGUI 那一档的实现只有一句
//     `rectTransform.SetSizeWithCurrentAnchors(Horizontal, rect.height * m_AspectRatio)`
//     （判据 = `D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/UGUI/UI/Core/Layout/AspectRatioFitter.cs:151-156`）
//     ⇒ **宽恒等于「高 × 比例」，完全不看父框的宽** ⇒ 父框比 `高×比例` 更窄时，根是**横向溢出**（不是按宽缩）。
//     根锚是 `(0,0)-(1,1)` + `sizeDelta (0,0)`（逐份 prefab 实读）⇒ 根**高 = 框高**、上下铺满、左右居中。
//     ⇒ 它的 `Content` 缩放 `UIScaleToFit(fitInParent)` = `min(根宽/Content宽, 根高/Content高)`，
//        对基础档（根 0.66·H × H、Content 712×1080）恒等于 **`H / 1080`**（宽那一支永远不更小：0.66/712 > 1/1080）。
//     ⇒ 原版口径 = **`K = box.H / 根高`**（`RootFit.HeightControlsWidth`）。
//     我们的默认仍是 `K = min(box.H/根高, box.W/根宽)`（= 等比内接，`RootFit.Contain`）——
//     两者**只在「框比根更窄」时不同**（框宽/框高 < 根宽/根高，基础档即 0.66）：那时原版溢出、我们缩进去。
//     ⚠️ **按 prefab 分档**：`m_AspectMode 2` 的用 `HeightControlsWidth`；`m_AspectMode 3`（FitInParent，
//        `Icon Campaign Points Drawer Variant` / `Icon Expansion Pass Points Drawer Variant`）与
//        **根上没有 ARF、但 `Content` 走 `UIScaleToFit`** 的（`Title Drawer Horizontal Variant`）原版就是**等比内接**
//        ⇒ 必须继续用 `Contain`。⚠️ `Alliance Badge Drawer Icon` 是**第三种**：根无 ARF 且锚死 `(0.5,0.5)`、
//        `sizeDelta 165.95×155.74` + `m_AnchoredPosition.y 44.6322` ⇒ **固定尺寸、根本不跟框走**
//        ⇒ `FrameOf` 的**两支都不适用**。✅ **2026-11-01 `B114_W35`（`B-277`）已落地**：
//        `AllianceBadgeIconRootOf(box)`（`Shell/ItemDrawerPartial_B114W35.cs`）—— 尺寸只由 prefab 常量决定、
//        框只决定它的**中心**在哪；断言 `Editor/B114_W35_Assert.cs` §③。
//        逐份清单 → `资料/完全复刻/报告/B114_W32_ARF与两档几何.md` §一。
//     ⛔ 别一步把默认改成 `HeightControlsWidth`（那会让 mode 3 / 无 ARF 的那四族反过来溢出）——
//        迁移要按 prefab 逐个显式传，清单见报告。
//     ✅ **2026-11-01 `B114_W47` 已按清单逐个迁完**（`m_AspectMode 2` 的 34 份 prefab ⇒ **26 处 `FrameOf` 调用点**显式传
//        `RootFit.HeightControlsWidth`，另有 1 处**内联的同一算式**（`DrawCurrency`）改走 `FitK`；mode 3 的两族 / 横条档 /
//        固定尺寸的 `Alliance Badge Drawer Icon` 继续走各自那一支）。`Base Drawer` / `Deck Drawer` / `Wildcard Drawer`
//        这三份**本来就没有帧模型**（占位板 / 我们自己挑的方框），不在本次迁移面内。
//        逐处对照表 → `资料/完全复刻/报告/B114_W47_ARF迁移.md`。默认仍是 `Contain`（⛔ 别再改默认）。
//  ② **`Background` 那张图**：**默认档**是 `40k_Rewards_icon_base card`（落盘名空格→下划线 = `40k_Rewards_icon_base_card`）。
//     📌 **2026-10-30 `B100_F3` 已把它 + `_common/_rare/_epic/_legendary` 导进 `Resources/Art/ui_menu/`**
//     （现读：6 张全在）⇒ `ItemDrawerCurrency.cs` 文件头 ① 那条「不在 `Resources/`」**已解除**。
//     🔴 **2026-11-01 就地订正（铁律 5 · `B114_W35` 真包现读）**：本行原来写「**每一档**都是这张」——
//        **不成立**，它是**默认值不是通则**：
//        · `Title Drawer`（竖卡）= **`40k_Rewards_icon_title card`**（329×499，落盘 `40k_Rewards_icon_title_card`）；
//        · `Title Drawer Horizontal Variant` = `40k_Profile_display_title`（322×116，`B114_W32` 已接）；
//        · 其余基础档 = `40k_Rewards_icon_base card`（328×498）。
//        ⇒ `DrawerBackground` 的图名改成**形参**（默认值 = 共享那张），各档按自己 prefab 显式传。
//  ③ **`Army` 那一行**：**默认档** 字号 **75** / 基准 36 / auto[12~75] / 居中 / 折行 1 / 色 (0.689,0.689,0.689,0.851)。
//     🔴 **2026-11-01 就地订正（铁律 5 · `B114_W35` 真包现读）**：本行原来写「逐份 prefab 实读，值相同」——
//        **不成立**：`Title Drawer`（竖卡）的 `Content/Army` 是 **35.099998**（auto 仍是 [12~75]、基准仍 36、
//        对齐 `Center/**Bottom**`）⇒ **75 是通则，`Title Drawer` 是例外**。
//        ⚠️ 对照组 = `Avatar Drawer` 的 `Army` 确确实实是 **75.0**（同法现读）⇒ ⛔ 别把通则也改成 35.1。
//        ⇒ `DrawerArmyName` 多一个 `fontPx` 形参（默认 = `DrArmyFontPx` = 75），`Title` 显式传 35.1。
//     ✅ **2026-11-01 `B114_W38`（`B-280`）补完这一条的「垂直档」**：原版 `Title Drawer` 的 `Army` 是
//        **`Center/Bottom`**（同一份 prefab 的 `Label/Name` 是 `Center/Midline`），而 `Avatar Drawer` 是 `Center/Middle`
//        ⇒ `DrawerArmyName` 改成**返回 `Label`**（对既有调用点源码兼容），`Title(...)` 拿到它之后显式调
//        `MenuDraw.SetVAlign(lb, Label.VAlign.Bottom, r)`。⛔ 别把 `Middle` 的通则改掉。
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        // ---- 节点名：**照 prefab 逐字**（自检按它们取件；`NodeIcon` 那一套是我们旧名，两套并存）----
        /// <summary>`Content` 容器（每份抽屉 prefab 都有这一层）。</summary>
        public const string NodeContent = "Content";
        /// <summary>`ItemDrawerComponents.background` 那件（**默认档**的 sprite = `40k_Rewards_icon_base card`；
        /// ⚠️ `Title Drawer` 是 `40k_Rewards_icon_title card`、横条档是 `40k_Profile_display_title` —— 见文件头 ②）。</summary>
        public const string NodeBackground = "Background";
        /// <summary>`ItemDrawerComponents.image` 那件（各档各自的「主图」）。</summary>
        public const string NodeImage = "Image";
        /// <summary>`Content/Army`（阵营名 TMP）。</summary>
        public const string NodeArmy = "Army";
        /// <summary>`Content/Label/Name`（物品名 TMP）。</summary>
        public const string NodeName = "Name";
        /// <summary>`Content/Frame`（`Alliance Badge Drawer` 的档位框）。</summary>
        public const string NodeTrophyFrame = "Frame";
        /// <summary>`Content/Frame/Trophy Icon`（**基础档** `Alliance Badge Drawer` 的徽章本体）。
        /// ⚠️ **Icon 档 `Alliance Badge Drawer Icon` 那颗叫 `Badge`**（见 <see cref="NodeIconBadge"/>）——
        /// 2026-11-01 `B114_W38`（`B-282②`）真包现读 · `B-277` 的 `IconAllianceBadgeFixedDraw` 建完当场改名。</summary>
        public const string NodeTrophyIcon = "Trophy Icon";
        /// <summary>`Alliance Badge Drawer Icon` **根的直接子件**那颗徽章本体 —— 真包现读就叫 **`Badge`**
        /// （`&224709867586078768`，锚 (0,0)-(1,1) 满根）。基础档同位置那颗叫 `Trophy Icon`（见上）。
        /// ⇒ **只服务 Icon 档**；⛔ 别拿它去查基础档（那里没有这个名字）。判据 → `B114_W38` 报告 §活3②。</summary>
        public const string NodeIconBadge = "Badge";
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
        /// <summary>`Content` 的左右沿（0.4 / 712.4 —— **逐份相同**）。⚠️ `0.4` **不是误差、也不是我们挑的**：prefab 根挂 `AspectRatioFitter`（`m_AspectMode 2` = HeightControlsWidth · `m_AspectRatio 0.66`）⇒ 根宽 = 1080×0.66 = **712.8**，而 `Content` 是 `m_SizeDelta (712,1080)` + 锚 `(0.5,0.5)` + `m_AnchoredPosition (0,0)` ⇒ **左右各留 (712.8−712)/2 = 0.4**（`Content` 自己那份 `m_LocalScale` 序列化是 `0`，由它身上的 `UIScaleToFit`（`fitInParent:1` · `targetRect` = 根 · `padding 0`）运行期算成 **1.0** ⇒ 框不动）。判据 = 2026-11-01 `B114_W20` 现读 `Expansion Pass Points Drawer.prefab`（`!u!114 &114711914853252429` 的 `fitInParent/targetRect` + 根 `AspectRatioFitter`）⇒ ⛔ **别再把它改成 0**：那条「原版 `Content` 是 0..712」是**把 `Content` 的本地坐标当成了根的坐标**（全文 → `资料/完全复刻/报告/B114_W20_抽屉0.4与三档光圈.md` §一）。</summary>
        const float DrCx1 = 0.4f, DrCx2 = 712.4f;
        /// <summary>`Content/Image` 的默认矩形（`Avatar` / `Container` / `XSolla` / `Title` 例外，见各自文件）。</summary>
        const float DrImgY1 = 216f, DrImgY2 = 648f;
        /// <summary>`Content/Image` 那条**下沿横带**（锚 `(0,−0.05)-(1,0.13)` × `Content` 的 1080 高 ⇒
        /// `1080−0.13×1080 = 939.6` · `1080+0.05×1080 = 1134`）。**越过 `Content` 下沿 54 px —— 原版就是溢出**。
        /// <para>🔴 2026-11-01（`B-315`）：`Wildcard Drawer` 的 `Content/Image` 与 `Container Drawer` 的
        /// `Content/Army Icon` **是同一条矩形**（判据 → `判据/B115_S4_三份Drawer规格.md` §四·2 / §五·2）。
        /// 收口前两处各写一份字面量 ⇒ 这里成为**唯一来源**，⛔ 别在任何抽屉里再抄一份数字。</para></summary>
        const float DrEmblemY1 = 939.6f, DrEmblemY2 = 1134f;
        /// <summary>`Content/Army` 那一行。</summary>
        const float DrArmyY1 = 702f, DrArmyY2 = 810f;
        /// <summary>`Content/Label` 那一行（`HorizontalLayoutGroup`：padding L/R 25 · spacing 10）。</summary>
        const float DrLbY1 = 810f, DrLbY2 = 918f, DrLbX1 = 36f, DrLbX2 = 676.8f;
        /// <summary>`Label/Quantity`（左半、右对齐）与 `Label/Name`（右半、左对齐）—— 两份子件的布局位。</summary>
        const float DrQtyX1 = 61f, DrQtyX2 = 351.4f, DrNameX1 = 361.4f, DrNameX2 = 651.8f;
        /// <summary>**默认**底图（落盘名：空格→下划线）。出处 = 各 prefab 的 `Content/Background.m_Sprite`。
        /// ⚠️ 这是**默认值不是通则**（铁律 5·c）：`Title Drawer` 的 prefab 写的是 `40k_Rewards_icon_title card`
        /// ⇒ 那一档必须显式传，⛔ 别用本常量顶（见文件头 ② 与 `Shell/ItemDrawerTitle.cs`）。</summary>
        const string DrBackgroundSprite = "40k_Rewards_icon_base_card";
        /// <summary>**默认** `Content/Army` 字号（设计 px · 原版 `m_fontSize 75`）。
        /// ⚠️ 同样是**默认值不是通则**：`Title Drawer` 的 `m_fontSize = 35.099998`（见文件头 ③）。</summary>
        const float DrArmyFontPx = 75f;
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

        /// <summary>原版根上那颗 `AspectRatioFitter` 在**哪种模式**下工作 —— 决定「框不是根那比例」时往哪边让。
        /// <para>判据 = `AspectRatioFitter.cs:140-162`（UGUI 包源码，见文件头 ①）：</para>
        /// <para>· `HeightControlsWidth`（`m_AspectMode 2`）—— 只有一句
        /// `SetSizeWithCurrentAnchors(Horizontal, rect.height * m_AspectRatio)`，**不看父框宽**
        /// ⇒ 框太窄时根**横向溢出**（左右各溢出一半，根仍居中）。</para>
        /// <para>· `Contain` —— 对应 `m_AspectMode 3`（FitInParent）与「根本身没有 ARF」两种：
        /// 原版那一刻是**等比内接**（FitInParent 的算式 / 子件 `UIScaleToFit(fitInParent)` 的 `min`）。</para></summary>
        public enum RootFit
        {
            /// <summary>`m_AspectMode 3` / 根无 ARF ⇒ 等比内接：`K = min(框高/根高, 框宽/根宽)`（= 本库的旧口径）。</summary>
            Contain = 0,
            /// <summary>`m_AspectMode 2` ⇒ 高说了算：`K = 框高/根高`（框不够宽时**原版是溢出**，⛔ 不缩）。</summary>
            HeightControlsWidth = 1,
        }

        /// <summary>按「抽屉根盖在这一格框里」算出 <see cref="DrawerFrame"/>。
        /// 默认根 = 712.8×1080（ARF 0.66）；Icon 变体传 (1080,1080)。
        /// <paramref name="fit"/> 默认 <see cref="RootFit.Contain"/> = **旧口径逐位不变**（20 多处调用点一个字节没动）；
        /// `m_AspectMode 2` 的那一批要显式传 <see cref="RootFit.HeightControlsWidth"/>（迁移清单 → 文件头 ① / 报告 §一）。</summary>
        internal static DrawerFrame FrameOf(PxRect box, float rootW = DrRootW, float rootH = DrRootH,
                                            RootFit fit = RootFit.Contain)
        {
            float k = FitK(box, rootW, rootH, fit);
            var fr = new DrawerFrame { K = k };
            fr.OX = box.CX - rootW * k * 0.5f;
            fr.OY = box.CY - rootH * k * 0.5f;
            return fr;
        }

        /// <summary>`FrameOf` 与 <see cref="RootRectOf"/> 共用的那一条算式（只此一份，⛔ 别在两处各写一遍）。
        /// 判据见 <see cref="RootFit"/> 与文件头 ①。</summary>
        public static float FitK(PxRect box, float rootW, float rootH, RootFit fit)
        {
            if (fit == RootFit.HeightControlsWidth) return box.H / rootH;      // 原版 ARF：宽 = 高 × 比例，不看框宽
            return Mathf.Min(box.H / rootH, box.W / rootW);                   // FitInParent / UIScaleToFit：等比内接
        }

        /// <summary>原版**根**在本格 px 里的矩形（左上原点 · y 向下）。
        /// <para>⚠️ 不是画什么东西的位置，而是「原版那一刻根占多大」—— 用来算溢出量 / 给断言拿一个**独立于
        /// `DrawerFrame` 内部字段**的读数口（`K`/`OX`/`OY` 三个数拼回来的话，断言的期望值就和实现对上了 = 自证）。</para></summary>
        public static PxRect RootRectOf(PxRect box, float rootW = DrRootW, float rootH = DrRootH,
                                        RootFit fit = RootFit.Contain)
        {
            float k = FitK(box, rootW, rootH, fit);
            float w = rootW * k, h = rootH * k;
            return new PxRect(box.CX - w * 0.5f, box.CY - h * 0.5f, box.CX + w * 0.5f, box.CY + h * 0.5f);
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

        /// <summary>画 `Content/Background`（**每一档都有**；`sprite` 默认 = 共享那张 `40k_Rewards_icon_base card`，
        /// ⚠️ `Title Drawer` 是 `40k_Rewards_icon_title card` —— **那一档必须显式传**，见文件头 ②）。
        /// 取不到图 ⇒ 出声一次、**不画**（⛔ 不拿别的图顶替）。</summary>
        /// <param name="keepAspect">原版那一颗 `Image.m_PreserveAspect`。
        /// 🔴 **默认 `true` = 按 sprite 自己的比例内接**：2026-11-01 `B114_W43`（`B-284`）真包**逐份**现读，
        /// **全族各档的 `Content/Background` 都是 `m_PreserveAspect 1`**（那一行末逐份印 `preserveAspect`）——
        /// `Avatar` · `Container` · `Premium` · `Forge Points` · `Alliance Badge` · `XSolla Offer` ·
        /// `Avatar Border` · `Random Card` · `Currency` · `Title` · `Campaign Points` 共 **11 份**。
        /// 原来默认是 `false`（拉伸）⇒ 全族都偏离原版；`B114_W38` 先按施工单只给 `Title` 显式传了 `true`（`B-282①`），
        /// 本行把它升成**全族默认**（`B-284`）。⚠️ 几何差 **&lt;1 px**（基础档 `328×498` 放进 `712×1080` 的框 ⇒ 高差 0.68 px；
        /// `Random Card` `328×523` 进 `712×1134` ⇒ 宽差 0.83 px）⇒ 拉伸与 PA **视觉等价**；
        /// 改它是**为了换图/换框时不再露馅**，不是修正一个看得见的偏差。
        /// ⚠️ **两档不走本函数**（各自实现里画底板）：`Random Card` 走 `DrawerImage`（那里恒 `keepAspect: true`，本来就对）；
        /// `Currency` 在 `Shell/ItemDrawerCurrency.cs` 的 `DrawCurrency` 里自己调 `MenuDraw.Rect(…, false, …)` —— **未改**，
        /// 见 `报告/B114_W43_全族PA与TitleName垂直档.md` §四（本活白名单不含那个文件）。</param>
        internal static bool DrawerBackground(Transform content, DrawerFrame fr, ItemDrawerStyle st,
                                              string sprite = DrBackgroundSprite, bool keepAspect = true)
        {
            var tex = Tex(sprite);
            if (tex == null)
            {
                Note("drawerbgsprite|" + sprite,
                     "抽屉底图 `" + sprite + "` 在 `Resources/` 里取不到"
                     + "（`CardArt.MenuUi` 三级全空；导入口 = `工具/sync_battle_ui_art.py`）⇒ 这一层**不画**");
                return false;
            }
            MenuDraw.Rect(content, tex, fr.Map(DrCx1, 0f, DrCx2, DrRootH), NodeBackground,
                          st.QBoard, null, keepAspect, st.Clip, st.ClipSoftness);
            return true;
        }

        /// <summary>画 `Content/Army`（原版 `armyText.text = GameStaticData.CardArmyToString(army)`）。
        /// 我们的等价物 = `ArmyIcons.LocalizedName`（同一句 `Armies/&lt;枚举名&gt;` 词条，见 `Core/ArmyIcons.cs`）。
        /// `Neutral` / 认不出 ⇒ 原版那串是空、这一层不画（⛔ 不拿默认值顶）。</summary>
        /// <param name="fontPx">原版那一颗 `m_fontSize`（设计 px）。**默认 = <see cref="DrArmyFontPx"/> = 75**
        /// （= 通则，`Avatar Drawer` 现读就是 75.0）；⚠️ **`Title Drawer` 是例外 = 35.1**，
        /// 那一档由 `ItemDrawerTitle.Title(...)` 显式传（判据 → 文件头 ③）。</param>
        /// <returns>画出来的那颗 <see cref="Label"/>（**没画 = `null`**：调用方不要文字层 / 阵营认不出 / 整块在视口外）。
        /// 🆕 2026-11-01 `B114_W38`（`B-280`）：原来返回 `void` —— 改成返回实例之后，需要**按档补垂直档**的调用点
        /// （今天只有 `Title Drawer`：原版 `Center/Bottom`）才能拿到它去调 `MenuDraw.SetVAlign`。
        /// ⚠️ **源码兼容**：把「调一个 `void` 方法」写成语句，与「调一个有返回值的方法但不要返回值」在 C# 里是同一件事
        /// ⇒ 其余 9 个既有调用点**一个字节都不用改**（⛔ 别顺手给它们加垂直档）。</returns>
        internal static Label DrawerArmyName(Transform content, DrawerFrame fr, int army, ItemDrawerStyle st,
                                             float fontPx = DrArmyFontPx)
        {
            if (st.NamePx <= 0f) return null;                 // 调用方不要文字层
            string name = ArmyIcons.LocalizedName(army);
            if (string.IsNullOrEmpty(name)) return null;
            var r = fr.Map(DrCx1, DrArmyY1, DrCx2, DrArmyY2);
            // auto 区间 [12,75] 与 base 36 **逐份相同**（`Title Drawer` 也是这两个数）⇒ 只有标称字号按档传。
            return ClippedText(content, r, name, DrArmyInk, NodeArmy, fontPx * fr.K, st.QText, r.W, st, 0,
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

        // ============================================================ 自检读口（2026-10-31 · `B109_E11`）
        //
        //  为什么要有它：这一整套版面常量是 **prefab 逐份实读**出来的（出处见文件头），但它们全是 `private const`
        //  ⇒ 断言看不见「底图名 / 尺寸 / 文本位」这几个值，只能靠人眼比 dump。
        //  这里**只做转发、不复制数值**（数值仍只有上面那一份 —— `CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」）。

        /// <summary>版面读口：`Content` 左右沿 / `Image` 默认矩形 / `Label` 行 / `Quantity`·`Name` 分栏（设计 px · 左上原点）。</summary>
        public struct LayoutMetrics
        {
            public float ContentX1, ContentX2, ImageY1, ImageY2;
            public float LabelX1, LabelX2, LabelY1, LabelY2;
            public float QtyX1, QtyX2, NameX1, NameX2;
            /// <summary>`Content/Background` 的 `m_Sprite`（落盘名：空格→下划线）。</summary>
            public string BackgroundSprite;
        }

        /// <summary>读口：见 <see cref="LayoutMetrics"/>。</summary>
        public static LayoutMetrics Metrics()
        {
            return new LayoutMetrics
            {
                ContentX1 = DrCx1, ContentX2 = DrCx2, ImageY1 = DrImgY1, ImageY2 = DrImgY2,
                LabelX1 = DrLbX1, LabelX2 = DrLbX2, LabelY1 = DrLbY1, LabelY2 = DrLbY2,
                QtyX1 = DrQtyX1, QtyX2 = DrQtyX2, NameX1 = DrNameX1, NameX2 = DrNameX2,
                BackgroundSprite = DrBackgroundSprite,
            };
        }
    }
}
