// AvatarTab.cs — 玩家档案窗第 2 页：`Avatar Tab`（原版类名 `AvatarTab`）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/档案窗_Avatar与Title页.md` —— §A1 是**层 × 参数**表（工具逐行），
// §A-补 1/2/3 是工具给不了的那部分（`activeSelf` 条件 / 图参数 / **格子怎么排**）。
// 表 = `python 工具/menu_dump.py bundle_menus_assets_all --rt -4150415914498688462 --depth 8 --md`
//
// ---- 🔴 这一页与 `Title Tab` 的差别（同构的那一半**共用一套画法**）----
//   · `Selected Item Panel` 里**不是空的**：装的是 `Avatar Menu Item`（大图预览 + 名字），
//     而 `Title Tab` 那份装的是 `ProfileItemDisplay`（只有图 + 字）。
//   · 多了 `Toggle borde` 这个**活**节点（`AvatarTab.toggleAvatarBorderIcon`）—— 切「显不显示头像边框」。
//     ⚠️ 同一个名字在 `Title Tab` 里是**死节点**（出厂 F、全包无引用）⇒ 别当成一份。
//   · 网格格子 = **`Avatar Item Small`（180×180）**，而 `Title Tab` 用的是 325.9×130 的横条。
//
// ---- 🔴 格子的版式**是真值**（不像 Title 那页得自己挑）----
//   预制体里 `Item Drawer` 底下有**1 个实例** `Avatar Item Small_Ref`（180×180），**它就是 `itemPrefab`**
//   —— 运行期先 `SetActive(false)` 再逐格 `Instantiate`（`AvatarTab__InitializeGameObjects.c:60/152/206`）
//   ⇒ 它的内部结构可以直接照抄（下面 `CellOffsets` 那几行）。出处：普查文件 §A-补 3。
//
// ---- 数据 ----
//   清单来自**原版资产**（469 张可选头像 SO → `Resources/profile_cosmetics.json`），**不是玩家存档**
//   （判据 → `Shell/ProfileData.cs` 文件头）。**显示名是我们从资源名反推的**。
//   ⚠️ 原版预制体里那两个**占位值**记在这里备查（**不画**）：`Avatar Name` 的 `m_text = 'TEST NAME'`。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `Avatar Tab`（`AvatarTab`）。</summary>
    public class AvatarTab : ProfilePage
    {
        public override WindowTabType Type { get { return WindowTabType.ProfileAvatar; } }
        protected override int PageIndex { get { return 1; } }
        public override PxRect PageRect
        {
            get { return new PxRect(PlayerProfileWindow.WideL, PlayerProfileWindow.RedT,
                                    PlayerProfileWindow.ContentR, PlayerProfileWindow.AreaB); }
        }

        // ============================================================ 真值（绝对画布像素 · 左上原点）
        // ⚠️ 页根**两侧各溢 16.33**（真值，正本 §2·1）。

        /// <summary>`Selected Item Panel`：`318.37,292.02 → 587.32,758.89`。</summary>
        const float SelL = 318.37f, SelT = 292.02f, SelR = 587.32f, SelB = 758.89f;
        /// <summary>`Selected Item Panel/Avatar Menu Item`：`318.37,360.15 → 585.45,633.73`。
        /// 挂 `EverguildButton + AvatarDisplay + ItemDrawerComponents`（`AvatarDisplay` 字段
        /// `avatarHolder/avatarImage/avatarName/button/highlight/...`）。</summary>
        const float MiL = 318.37f, MiT = 360.15f, MiR = 585.45f, MiB = 633.73f;
        /// <summary>`Avatar Menu Item/Image Container`：`318.37,360.15 → 585.45,573.73`（267.08×213.58）。</summary>
        const float IcL = 318.37f, IcT = 360.15f, IcR = 585.45f, IcB = 573.73f;
        /// <summary>`Image Container/Highlight`（**选中高亮**）：`318.37,355.60 → 588.79,571.03`。
        /// 图 `Player_Avatar_selected`（256×256）· `preserveAspect`。
        /// 🔴 原版**这一份（大图那份）实际不会亮** —— 两个调用点都写死 `highlight = false`
        /// （`AvatarTab__RefreshDisplays.c:47` 传 `0,0`、`AvatarTab__OnAvatarClick.c:38` 第 4 参 `0`）
        /// ⇒ 我们照实：**只给网格格子用**，大图那份恒关。</summary>
        const float HlL = 318.37f, HlT = 355.60f, HlR = 588.79f, HlB = 571.03f;
        public const string ArtHighlight = "Player_Avatar_selected";
        /// <summary>`Image Container/Border`（**头像边框**）：`318.37,381.51 → 585.45,595.09`。
        /// 图 `Player Profile Border`（256×286）· `preserveAspect`。
        /// ⚠️ **传的必须是导入后的文件名**（下划线版）—— `CardArt.MenuUi` **不做「空格→下划线」转换**，
        /// 写成精灵名 `Player Profile Border` 会**静默取不到**（2026-09-27 就这么红过一条）。
        /// 它由 `Toggle borde` 那个钮控制显隐（`AvatarTab.ToggleAvatarBorder` / `CheckEnableAvatarBorder`）。</summary>
        const float BdL = 318.37f, BdT = 381.51f, BdR = 585.45f, BdB = 595.09f;
        public const string ArtBorder = "Player_Profile_Border";
        /// <summary>`Image Container/Image`（**头像立绘那一层**）：`318.37,357.45 → 585.45,571.03`
        /// —— 这是**布局矩形**（267.08×213.58），**实绘还要 ×2.0**（`ScaleImage`，绕中心放大 ⇒ 实绘 534.16×427.16，
        /// `preserveAspect` 后 427.16²）。🔴 判据 = prefab 的 `m_LocalScale = 2.0`（S5 §①，12/12 实例同值）。
        /// ⚠️ 预制体里这件的 `m_Sprite = 0`（原版运行期由 `AvatarDisplay.avatarImage` 喂）。</summary>
        const float ImL = 318.37f, ImT = 357.45f, ImR = 585.45f, ImB = 571.03f;
        /// <summary>`Avatar Menu Item/Avatar Name`：`303.37,253.13 → 600.45,321.03` ·
        /// TMP 字号 **35** · auto **[15~35]** · 色 **(1, 0.693, 0.00784)**。
        /// ⚠️ 它比父件宽（303.37..600.45 vs 318.37..585.45）—— 真值。</summary>
        const float AnL = 303.37f, AnT = 253.13f, AnR = 600.45f, AnB = 321.03f;
        const float AnPx = 35f, AnAutoMin = 15f;
        static readonly Color NameColor = new Color(1f, 0.693f, 0.00784f, 1f);

        /// <summary>`Select Avatar Button`：`343.49,670.43 → 562.21,736.48` · `UI_Button_Mulligan`（Simple）。</summary>
        const float BtnL = 343.49f, BtnT = 670.43f, BtnR = 562.21f, BtnB = 736.48f;
        const float BtL = 354.25f, BtT = 677.86f, BtR = 550.74f, BtB = 729.05f;
        /// <summary>`Toggle borde`：`343.49,771.43 → 562.21,837.48`（**这一份是活的**）。
        /// ⚠️ 节点名原版就拼成 `borde`（不是 `border`）。文案 `'Toggle Border'`（36 · auto[10~36]，**有 Localize**）。</summary>
        const float TbL = 343.49f, TbT = 771.43f, TbR = 562.21f, TbB = 837.48f;
        const float TbtL = 354.25f, TbtT = 778.86f, TbtR = 550.74f, TbtB = 830.05f;
        public const string ArtButton = "UI_Button_Mulligan";
        /// <summary>词条取样；**`Core/Loc.cs` 表里还没有这个键时就地兜底**（两列 = 官方原文）。
        /// <para>🔴 **2026-10-17（`N37①`）为什么需要它**：`Demo/AvatarMenu/SelectAvatarButton`
        /// **不在 `Core/Loc.cs` 的手写表里**，而本批白名单只允许动 `MenuDeck/CantImportDeck` 那一行
        /// ⇒ 先走本地兜底（中/英两列照官方表；其余 10 语言走 `Loc.Lang13Current` 取官方列）。
        /// 等主对话把这条键补进 `Loc`，本函数**自动改用 `Loc.T`**（判据 = `Loc.HasEntry`）。
        /// ⚠️ **2026-10-19（`N71` 现核）就地订正（铁律 5）**：本段原写「`Loc.T` 只在非中英档才查
        /// 13 语言表」—— **自 2026-10-18（`N80`）起不成立**：`Loc.T` 的第三档现在对**中/英档**也兜
        /// 官方全表（`Core/Loc.cs` 现读 = `Table → bulk → 官方全表 → 缺键`）⇒ 直接 `Loc.T(键)` 也能取到。
        /// 本函数**保留**（缺键时仍有英文兜底；已有调用点行为不变），⛔ 不必改回直接 `Loc.T`。</para></summary>
        static string AvatarButtonTerm(string term, string en, string zh)
        {
            if (Loc.HasEntry(term)) return Loc.T(term);
            if (Loc.Current == AvailableLanguages.Chinese) return zh;
            if (Loc.Current == AvailableLanguages.English) return en;
            string v13 = Loc.Lang13Current(term);      // 其余 10 语言：官方列（缺列回退英文）
            return string.IsNullOrEmpty(v13) ? en : v13;
        }

        /// <summary>`Select Avatar Button/Button Text` 的**上屏文案** —— 🆕 **2026-10-17（`N37①`）起走官方词条**
        /// （上屏经 `AvatarButtonTerm`：Loc 表里有键走 `Loc.T`、没有就照官方两列兜底）。
        /// <para>🔴 **原版 prefab 这颗没有 `Localize`/`mTerm`**（2026-10-17 逐组件现读：三个
        /// `m_text = "Selecionar"` 的节点上只有 TMP + `EverguildTextMeshPro` + `EverguildButtonMaterialModifier`，
        /// **没有任何带词条字段的组件**）⇒ 原版 prefab 里占位就是葡语 `Selecionar`、没有本地化接线。</para>
        /// <para>✅ **2026-10-19（`W57`）全包普查复核（独立重做，铁律 5·c）**：`bundle_menus_assets_all`
        /// 里 `m_text = 'Selecionar'` 的 TMP 共 **3 颗**（`Player Profile Window/…/Avatar Tab/Selected Item
        /// Panel/Select Avatar Button/Button Text` · `…/Title Tab/…` 同名节点 · `Profile Cosmetic Tab/…`），
        /// 三颗的 GameObject 上 `Localize` 组件数 = **0**；同包 `mTerm = Demo/AvatarMenu/SelectAvatarButton`
        /// 的 `Localize` = **0 条** ⇒ 「prefab 无 mTerm」坐实（不是「翻了一个包没找到」）。</para>
        /// <para>官方表里有键 `Demo/AvatarMenu/SelectAvatarButton` = `Select` / `选择` ⇒ **接它**
        /// （比葡语占位更接近玩家看到的成品）。⚠️ **「原版用的就是这条键」是【推断】**：
        /// prefab 无 `mTerm` 可依 —— ⛔ 别写成「原版就是这条键」。</para></summary>
        public const string BtnLabelTerm = "Demo/AvatarMenu/SelectAvatarButton";
        /// <summary>原版 prefab 出厂原文（葡语占位 `Selecionar`）—— ⛔ 不上屏，只留作判据。</summary>
        public const string BtnLabelFactory = "Selecionar";
        /// <summary>🔴 **2026-10-18（`W22`）**：`Toggle borde/Button Text` 的**官方词条键** ——
        /// **判据是 prefab 自己**（不是猜的）：`Avatar Tab/Selected Item Panel/Toggle borde/Button Text`
        /// 那件 GameObject 上挂的 `Localize` 组件 `mTerm = "Demo/AvatarMenu/ToggleBorder"`。
        /// <para>读法/实例（三颗同族 `Toggle borde` 逐颗按 pid 现读，`mTerm` **全同**）：
        /// `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-6561044466830837198.json`（**Avatar Tab** · 本页用的这颗）·
        /// `MonoBehaviour_-2955158983638475911.json`（`Profile Cosmetic Tab`）·
        /// `MonoBehaviour_-9170721748395918798.json`（`Title Tab` —— 那页的 `Toggle borde` 是**死节点**，我们不建）。
        /// 三颗的 `m_Script.m_PathID = 8610481073976370760`（= `Localize`），组件表里与 TMP 并列。</para>
        /// <para>官方 13 语言表（`Resources/official_loc_13lang.json` 现读，键在索引 **5907**）=
        /// `Toggle Border` / **`切换边框`**（13 列**全非空**：es `Alternar Borde` · ru `Переключить рамку` · …）。</para>
        /// <para>⚠️ 这条键**不在手写 `Loc.Table` 里**（`Loc.HasEntry` = false，本批白名单也不允许动 `Core/**`），
        /// 但 `Loc.T` 自 **2026-10-18（`N71`）** 起在「`Table` → bulk → **官方全表**」的第三档会兜到它
        /// ⇒ 中/英/其余 10 语言**四档都能取到**（⛔ 别写死中文、⛔ 别在本文件再抄一份本地兜底）。</para></summary>
        public const string ToggleBorderTerm = "Demo/AvatarMenu/ToggleBorder";
        /// <summary>原版 prefab 那颗 TMP 的**出厂原文**（`'Toggle Border'`）—— ⛔ 不上屏，只留作判据
        /// （与 `TtlLabelFactory` / `BtnLabelFactory` 同口径；上屏走 `Loc.T(ToggleBorderTerm)`）。</summary>
        public const string ToggleBorderLabel = "Toggle Border";

        /// <summary>`Item Display Panel` + 它的三件（与 `Title Tab` 逐位相同 —— 同一套壳）。</summary>
        const float DisL = 632.79f, DisT = 210.69f, DisR = 1701.49f, DisB = 868.61f;
        const float TtlL = 654.16f, TtlT = 147.51f, TtlR = 1166.61f, TtlB = 210.70f;
        const float TtlPx = 35f, TtlAutoMin = 18f;
        /// <summary>`Select Item` 的**出厂原文**（原版 prefab 那颗 TMP 的 `m_text = "Select your avatar"`）。
        /// 🔴 **2026-10-17（`A1756`）起上屏走词条** `Demo/AvatarMenu/SelectAvatar`
        /// （官方两列 = `Select your avatar` / `选择你的头像`；同值键还有 `PlayerProfile/SelectAvatarTip`）
        /// ⇒ ⛔ 别再把这个常量直接印到界面上。
        /// ✅ **2026-10-18（W18）判据已坐实**（不再是「同值推断」）：该件 GameObject 上的 `Localize`
        /// 组件 `mTerm = "Demo/AvatarMenu/SelectAvatar"`（读法同 `TitleTab.TtlLabelTerm` 那段注释；
        /// 文件 = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_3351280201059105330.json`）。</summary>
        public const string TtlLabelFactory = "Select your avatar";
        public const string ArtPanel = "UI_Deck_Information_submenu_Back";
        public static readonly Vector4 PanelBorder = new Vector4(18f, 18f, 18f, 18f);
        const float VpL = 654.16f, VpT = 210.69f, VpR = 1680.12f, VpB = 855.46f;
        /// <summary>🆕 **2026-10-04：这个 `Scroll Rect` 上 `RectMask2D.m_Softness` 的原版真值 = (0,50)**
        /// —— **纵向** 50px 渐隐带（x 是硬边）。
        /// 🔴 判据（`d:/4/_tmp_view/q1_rm2d.txt:221-222`）：
        /// `Player Profile Window/Menu Area/Tab  Area/Tab Content/Avatar Tab/Item Display Panel/Scroll Rect`
        /// soft = **(0,50)**。⚠️ 同一扇窗的 `Trophies Tab/Scroll/Viewport` 与
        /// `Ranking Tab/AllFactions/scroll rect/viewport` 是 **(0,0)** —— **逐页不同，别互推**（铁律 5·c）。
        /// ⚠️ 机制与代价 → `MenuDraw.ApplySoftEdges`。</summary>
        static readonly Vector2 VpSoft = new Vector2(0f, 50f);
        /// <summary>`Item Drawer`（内容容器）：宽 **1044.65**（比视口宽 18.69 ⇒ 两侧各溢 9.35）。
        /// `GridLayoutGroup`：`cellSize **180×180**` · `spacing **(25,50)**` · `padding L13 T40` ·
        /// `UpperLeft` 起、水平优先。**5 列**（`floor((1044.65−13+25)÷205)`）。</summary>
        const float GridL = 654.16f, GridT = 210.70f, GridW = 1044.65f;
        const float CellW = 180f, CellH = 180f, GapX = 25f, GapY = 50f, PadL = 13f, PadT = 40f;

        // ---- 格子内部（**相对格子左上角**的偏移；出处：普查文件 §A-补 3 的 `Avatar Item Small_Ref` 那几行）----
        // 🔴 `Avatar Name` 落在**格子之外**（y 180..221.31）—— 正好插进 `GridLayoutGroup` 留的 **50px 行距**里。
        //    别把它「修正」回格子内。
        const float CImgL = 0f, CImgT = 0f, CImgR = 180f, CImgB = 142.63f;          // Image Container（布局矩形）
        // ⚠️ **2026-10-24（E18）就地订正（铁律 5）**：`CHlB` 原来写的是 **144.49**，那是这一件的
        //   **高**、不是「距格子顶的下边偏移」（错因：抄表时把 `尺寸` 列当成了 `底边` 列）。
        //   实据 = `资料/普查产出_0927/档案窗_Avatar与Title页.md:57` 的绝对框
        //   `667.16,247.17→850.50,391.65`，减去格子左上 `667.16,250.70` ⇒ **0,−3.53 · 183.34,140.95**
        //   （高 = 140.95 + 3.53 = **144.48** ≈ 那条表的 144.49 ✓ 自洽）。
        //   ⇒ 改前那面**选中金框比原版高 3.53px**（上下各多 1.77），放大 ×1.9 之后会变成 6.7px —— 顺手改掉。
        const float CHlL = 0f, CHlT = -3.53f, CHlR = 183.34f, CHlB = 140.95f;       // Highlight（**出厂 T**；布局框，实绘 ×1.9）
        const float CBdL = 0f, CBdT = 14.26f, CBdR = 180f, CBdB = 156.89f;          // Border（布局框，实绘 ×1.25）
        const float CArtL = 0f, CArtT = -2.70f, CArtR = 180f, CArtB = 139.93f;      // Image 立绘（布局框，实绘 ×2.0）
        const float CNmL = 0f, CNmT = 180f, CNmR = 180f, CNmB = 221.31f;            // Avatar Name（**出厂 F**）
        const float CNmPx = 36f, CNmAutoMin = 12f;

        // ============================================================ 🔴 三层的 `m_LocalScale`（实绘放大）
        // **上面那四组是【布局矩形】**（`RectTransform` 的 anchor/pos/sizeDelta 算出来的框，一个像素都不动）；
        // 原版真正画出来的框 = **布局矩形绕自己的 pivot (0.5,0.5) 再乘下面这三个倍数**。
        //
        // 🔴 **判据（第一权威 = prefab 的 `RectTransform.m_LocalScale`，2026-10-24 由只读代理 S5 逐颗读出）**：
        //   `资料/普查产出_1024b/S5_头像组装.md` §① 的表 + 「**12/12 个 `AvatarDisplay` 实例全同** ⇒ 系统级真值」。
        //   · 头像 `Image`（= `AvatarDisplay.avatarImage`）  × **2.0**
        //   · 框  `Border`（= `AvatarDisplay.avatarHolder`） × **1.25**
        //   · 高亮 `Highlight`（选中光晕，图 `Player_Avatar_selected`） × **1.9**
        //   ⚠️ `AvatarDisplay__ChangeAvatar.c:57` / `__ChangeBorder.c:50` **只 `set_sprite`、不碰 scale**
        //     ⇒ 倍数**全在 prefab 里**，运行期不会变（不是「运行时按状态改」的那一类，铁律 5·c）。
        //   ⚠️ 同族的第二份独立证据（**小号 `Avatar Item Small`**，即 `ProfileTab` / `RankedTab` 那两格）：
        //     `Editor/BattleScene.cs:24532`（A1512）逐字写着「`Border`（自己 `m_LocalScale 1.25`）」
        //     与「`Image`（自己 `m_LocalScale 2.0`）」—— 与这里同值。
        //   ⚠️ 第三份（**顶栏那颗 `Avatar Item Small`**，另一个 build 点）：`Shell/TopBar.cs` 的 `BuildTopAvatar`
        //     从**场景** prefab 独立推出同一个结论（`Image m_LocalScale = 2.0` / `Border ×1.25`），
        //     且按它与原版实拍逐点比对过（2026-09-27）—— 三处同值，⛔ 别再「按截图缩小它」。
        //
        // 🔴 **为什么必须放大（用户 2026-10-24 的原话：「头像框里面的头像应该更大，甚至超出头像框」）**：
        //   头像 sprite 是 **512² 画布 + 自带透明边**（≈ 主流款 alpha bbox 288×384，左右各留 108px）⇒
        //   放大 2.0 之后**可见内容宽 285.26×288/512 = 160.46**，而框（PA 内接后）宽 **159.59**
        //   ⇒ **1.006×（几乎等宽）**、头顶**戳出框顶**（原版 y −40.59 vs 框顶 −3.57 ≈ 37px）。
        //   ⛔ **不放大**时可见内容只有 **80.23** 宽 = 框宽的 **0.628×** —— 正是用户说的「缩小在里面」。
        //   ⛔ **别改用 `localScale`**：本工程惯例是**把倍数乘进矩形**（先例 `Shell/BoosterInfoPopup.cs:626`
        //     的 `51.88f * iconScl`），`MenuDraw.SetPxSize` 那条路不写 `localScale`。
        public const float ScaleImage = 2.0f, ScaleBorder = 1.25f, ScaleHighlight = 1.9f;

        /// <summary>把一个**布局矩形**绕它自己的中心放大 —— 原版 `m_LocalScale`（pivot = `(0.5,0.5)`）的几何效果。
        /// <para>🔴 **本批唯一实现**（`ProfileTab` / `RankedTab` 那两格也转调这里）：本批白名单只允许动
        /// `AvatarTab` / `ProfileTab` / `RankedTab` 三份 ⇒ 先寄在 `AvatarTab` 上；等 `ProfilePage`（三页的公共底座）
        /// 可写时应收口到那里，⛔ 别在第三处再抄一份（同规则两处写 = 迟早不一致）。</para>
        /// <para>判据 = uGUI：`pivot` 处的点不动、其余按 `scale` 线性摊开（`Transform.localScale` 的语义）。</para></summary>
        public static PxRect ScaleAboutCenter(PxRect r, float s)
        {
            if (Mathf.Approximately(s, 1f)) return r;
            float cx = (r.x1 + r.x2) * 0.5f, cy = (r.y1 + r.y2) * 0.5f;
            return new PxRect(cx + (r.x1 - cx) * s, cy + (r.y1 - cy) * s,
                              cx + (r.x2 - cx) * s, cy + (r.y2 - cy) * s);
        }

        MenuScroll _scroll;
        Transform _grid;
        Label _bigName;          // 大图那份的 `Avatar Name`
        ImageQuad _bigArt, _bigHighlight;
        Label _btnLabel;         // `Select Avatar Button > Button Text`
        WindowButton _btnHit;
        readonly List<ImageQuad> _cellArt = new List<ImageQuad>();
        readonly List<ImageQuad> _cellHl = new List<ImageQuad>();
        /// <summary>每个建出来的格子对应**清单里的第几条**（换选中时要按它点亮对的那一格）。</summary>
        readonly List<int> _cellIdx = new List<int>();

        /// <summary>🆕 2026-10-22（P1）：**预览**的那一张（点格子只改它；`Select` 钮才写 `ProfileData.AvatarIndex`）。
        /// 原版 = `AvatarTab.currentItem`（`OnAvatarClick` 写）+ `OnOpen` 里用 `PlayerAvatarItem` 重设
        /// （`AvatarTab__RefreshDisplays.c` 末尾那一路）。</summary>
        public int PreviewIndex = 0;
        /// <summary>已装备的那一张（= `ProfileData.AvatarIndex`，全工程唯一一份）。</summary>
        public int Equipped { get { return ProfileData.AvatarIndex; } }

        /// <summary>格子左上（**内容坐标**，未加滚动偏移）。`GridLayoutGroup` 的 UpperLeft + 水平优先。</summary>
        public static void CellXY(int col, int row, out float x, out float y)
        {
            x = GridL + PadL + col * (CellW + GapX);
            y = GridT + PadT + row * (CellH + GapY);
        }

        /// <summary>一屏列数（5）—— 自检要断它。</summary>
        public static int Columns
        {
            get { return Mathf.Max(1, Mathf.FloorToInt((GridW - PadL + GapX) / (CellW + GapX))); }
        }

        protected override void Build()
        {
            // 🆕 2026-10-22（P1）：**先**定预览项（原版 `RefreshDisplays` 也是先取 `PlayerAvatarItem` 再刷大图）
            PreviewIndex = Mathf.Clamp(ProfileData.AvatarIndex, 0, Mathf.Max(0, Avatars.Count - 1));
            // ---- ① `Item Display Panel`（与 `Title Tab` 逐位相同的那套壳）----
            var disp = Node("Item Display Panel", new PxRect(DisL, DisT, DisR, DisB));
            Nine(disp, ArtPanel, new PxRect(DisL, DisT, DisR, DisB), PanelBorder, "Background", 0);

            var vp = new PxRect(VpL, VpT, VpR, VpB);
            var scrollNode = Node(disp, "Scroll Rect", vp);
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：裁切状态**长在视口节点上**（= 原版那个 `RectMask2D`）。
            //    ⚠️ **本页没有 `Viewport` 节点** —— 视口矩形是建在 `Scroll Rect` 这一层上的
            //    （原版树是 `Item Display Panel/Scroll Rect/Viewport/Item Drawer`；我们少了中间那层，
            //    见 A435 迁移表 §二·A 注①）。⇒ 走注①那条**【低风险】路**：把 `ViewportClip` **直接挂在
            //    现成的 `Scroll Rect` 节点上**（它的 rect 本来就是视口矩形，`FindAbove` 从 `Item Drawer`
            //    走一级就命中）—— **零结构改动**。
            //    ⛔ **别顺手插一层 `Viewport`**：那会挪 `Item Drawer` 的父节点（`Editor/MainMenuScene.cs`
            //    有一条量**世界坐标**的断言，插层后要逐字复核）—— 那是另立的账，不是本次迁移。
            //    参数：`padding = (0,0,0,0)`（原版 `Scroll Rect` 那个 `RectMask2D` 实读）
            //    · `softness = VpSoft = (0,50)`（同一份判据 → 上面 `VpSoft` 那段注释）。
            var vc = scrollNode.gameObject.AddComponent<ViewportClip>();
            vc.padding = Vector4.zero;
            vc.softness = new Vector2Int((int)VpSoft.x, (int)VpSoft.y);
            // 🔴 **2026-10-18（A840 · S1）**：补一句 `CaptureNow()` —— 把**刚写进这个节点的那个矩形**记成基准
            //   （= 框的中心那一帧，口径 → `ViewportClip` 文件头 §①）。⛔ 少了它，这个视口**逐位回落到旧写法**
            //   （实时反推 + `LiveDerivations`）：本页整块面板被挪过之后，框与被比矩形就**不在同一帧**。
            //   ⚠️ **必须在这里调**（`AddComponent` 之后、`BuildRows` 之前）：`MenuDraw.ApplyPxRect` 写矩形时
            //   组件还不存在（那一句的穿透写在 `ApplyPxRect` 尾）—— 同 `ViewportClip.Hang` 里那句
            //   `vc.SetBaseRect(r)` 的位置理由（`CardPresentation/Shell/ViewportClip.cs`）。⛔ 别改成 `OnEnable` 里自动抓（文件头 `CardPresentation/Shell/ViewportClip.cs` 明令）。
            vc.CaptureNow();
            _scroll = NewScroll(vp, GridW, 0f, true, 50f);   // 原版 `Avatar Tab/Item Display Panel/Scroll Rect` = 50（D_N29）
            // 🔴 **2026-10-13（A465 · W-A435己）**：构建循环那一行（`BuildRows` 里
            //   `if (!_scroll.Intersects(r)) continue;`）从今天起读**同一颗节点**的状态
            //   （`MenuScroll.Intersects` 走 `ClipNode.State.RenderClip`）—— 那颗节点就是上面
            //   `scrollNode`（本页没有 `Viewport` 层，`ViewportClip` 直接挂在 `Scroll Rect` 上，见上）。
            //   ⚠️ 今天两值同（节点框 = `vp`、`padding` 全 0）⇒ **逐个位不变**。
            _scroll.ClipNode = vc;
            // 🔴 **滚轮要能重画**：`MenuScroll` 只改 `Offset`，**画是调用方的事** ——
            //    不接这个回调 = 滚轮转了、格子一个都不动（**静默失败**，2026-09-27 修）。
            //    接法照 `ForgeTab.BuildRewardCells` / `CollectionWindow.RebuildCardsCells` 那条已有的路。
            _scroll.OnChanged = RebuildRows;
            _grid = Node(scrollNode, "Item Drawer", new PxRect(GridL, GridT, GridL + GridW, GridT));

            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：原来这里是「`Clip = vp; ClipSoftness = VpSoft;`
            //    → `BuildRows()` → `Clip = null; ClipSoftness = Vector2.zero;`」那一对 —— **整对删掉**
            //    （裁切状态已迁到 `Scroll Rect` 节点上，见上面那一段）。留着它 = 形参永远非空 ⇒
            //    `Resolve` 走第 1 支 ⇒ **节点一个像素都不生效**（静默，唯一痕迹 = `NodeShadowedByParam`）。
            BuildRows();

            // 🔴 **A406（2026-10-12）逐站实读**：`Avatar Tab/Item Display Panel/Select Item`
            //   原版 `auto[18.0~35.0] 基准=26.1` ⇒ 上限 35（= 标称）· base **26.1**。判据 =
            //   `python 工具/menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 25 --md`。
            // 🔴 **A1024③（2026-10-17）换面**：本页只有**大图那颗** `Avatar Name` 是 `Asar-Regular SDF`
            //    （判据 = `menu_dump … "Player Profile Window"` 的 `Selected Item Panel ▸ Avatar Menu Item ▸ Avatar Name`）；
            //    `Select Item` / 两个 `Button Text` / 格子里的 `Avatar Name` 全是 **Pragati** ⇒ `LatinCardBody`。
            Text(disp, Loc.T("Demo/AvatarMenu/SelectAvatar"), new PxRect(TtlL, TtlT, TtlR, TtlB), Color.white, "Select Item",
                 TtlPx, 3, autoFit: true, autoMinPx: TtlAutoMin, alignLeft: true, wrap: true,
                 autoMaxPx: 35f, basePx: 26.1f, face: TmpFont.Face.LatinCardBody);

            // ---- ② `Selected Item Panel`：当前选中的头像（大图 + 名 + 两个钮）----
            var sel = Node("Selected Item Panel", new PxRect(SelL, SelT, SelR, SelB));
            var big = Node(sel, "Avatar Menu Item", new PxRect(MiL, MiT, MiR, MiB));

            var ic = Node(big, "Image Container", new PxRect(IcL, IcT, IcR, IcB));
            // 🔴 `Highlight` 挂在 **`Image Container`** 下（不是 `Avatar Menu Item` 下）—— 照原版树。
            //    大图这份**恒关**（见常量注释）。
            // 🔴 **2026-10-24（E18）**：三层都按原版 `m_LocalScale` **绕各自布局矩形的中心放大**
            //    （头像 ×2.0 / 框 ×1.25 / 高亮 ×1.9 —— 判据与理由见 `ScaleImage` 那一段常量注释）。
            _bigHighlight = Rect(ic, ArtHighlight, ScaleAboutCenter(new PxRect(HlL, HlT, HlR, HlB), ScaleHighlight),
                                 "Highlight", 4, null, true);
            if (_bigHighlight != null) _bigHighlight.gameObject.SetActive(false);
            // ⚠️ **别用 `Rect(ic, null, …)`** —— `CardArt.Solid()` 是**不透明白块**，会变成面板正中一大块白。
            //    没有图就**不建**（`CosmeticRect` 取不到时返回 null）。
            // 🔴🔴 **2026-10-18（W18）画序订正（铁律 5）**：原来是 `Highlight 4 → Image 4 → Border 5`
            //    —— **立绘被边框压住**，而 `Player_Profile_Border` 的中心是**不透明黑**
            //    （实测 RGBA `(0,0,0,255)`）⇒ 大图那块整片黑、头像一张看不见（用户 2026-10-18 真机点名）。
            //    原版兄弟序 = **`Highlight → Border → Image`**（`Image` 最后 = 画在最上面）
            //    ⇒ 队列必须是 `Highlight 4 < Border 5 < Image 6`。同族先例（已收口）：
            //    `ProfileTab.L_ArtOverFrame` / `RankedTab.L_ArtOverFrame` / `TopBar.QBarAvatarFrame<QBarAvatar`。
            WarnIfAvatarMissing(Avatars.Count > 0 ? Avatars[PreviewIndex].Art : null);
            _bigArt = CosmeticRect(ic, Avatars.Count > 0 ? Avatars[PreviewIndex].Art : null,
                                   ScaleAboutCenter(new PxRect(ImL, ImT, ImR, ImB), ScaleImage), "Image", 6);
            Rect(ic, ArtBorder, ScaleAboutCenter(new PxRect(BdL, BdT, BdR, BdB), ScaleBorder), "Border", 5, null, true);
            // 🔴 **2026-10-07（A62 子表 A · A28）**：原版 `Avatar Tab > Selected Item Panel > Avatar Name`
            //   （`'TEST NAME'`）是 **`折行=0 auto[15~35]`**（判据 = `md "Player Profile Window" --depth 25 --md`）
            //   ⇒ `SetAutoFitBox` 内部会无条件开折行 ⇒ 显式关掉（A205：关这一下顺带把版面推下去）。
            // 🔴 **A406**：`Avatar Tab/Selected Item Panel/Avatar Menu Item/Avatar Name`
            //   原版 `auto[15.0~35.0] 基准=36.0` ⇒ 上限 35（= 标称）· base **36.0**（同一条 dump）。
            _bigName = Text(big, "", new PxRect(AnL, AnT, AnR, AnB), NameColor, "Avatar Name",
                            AnPx, 6, autoFit: true, autoMinPx: AnAutoMin, alignLeft: false, wrap: false,
                            autoMaxPx: 35f, basePx: 36f, face: TmpFont.Face.LatinUi);
            if (_bigName != null) _bigName.SetWrapping(false);

            // 两个钮：**图和文字都挂在钮节点【里面】**（照原版树：`Select Avatar Button > Button Text`）
            var btn = Node(sel, "Select Avatar Button", new PxRect(BtnL, BtnT, BtnR, BtnB));
            var selQ = Rect(btn, ArtButton, new PxRect(BtnL, BtnT, BtnR, BtnB), "Image", 7);
            // 🔴 **A62 · A29**：原版 `Button Text`（`'Selecionar'`）是 **`折行=0 auto[10~36]`** ⇒ 关掉。
            // 🔴 **A406**：`Avatar Tab/Selected Item Panel/Select Avatar Button/Button Text`
            //   原版 `auto[10.0~36.0] 基准=12.0` ⇒ 上限 36（= 标称）· base **12.0**。
            // 🔴🔴 **2026-10-23（P5）画序订正：字必须**严格**排在底图之上（`Q+8` > 底图 `Q+7`）** ——
            //   实拍（`_tmp_view/menu/11_档案窗_头像页.png`）那颗钮**一个字都没有**，而同页的
            //   `Toggle borde`（同一套写法）中文正常。两者的唯一差别 = 这颗钮此刻是 **`interactable = false`**
            //   ⇒ `WindowButton.RefreshGray` 把底图材质换成 `Everguild/UI/Greyscale`（一份**新材质**）。
            //   改前字与底图**同为 `Q+7`**：同队列的透明件按「到相机距离」排（CLAUDE.md §三），
            //   距离相等时由材质当决胜键 —— 换灰材质那一下就把顺序翻了过来 ⇒ 底图盖住字。
            //   **判据**：① 同一颗钮在**头衔页**（那颗钮当时**没变灰**）中文正常（`_p5_title_btn.png`）；
            //            ② 参考图 `账户-头像参考图.png` 里原版**灰钮上照样印白字「正在使用」**；
            //            ③ 原版 prefab 的 `Button Text` 是 `Select Avatar Button` 的**子件**（永远画在 Image 之上）。
            //   ⛔ 别改回同队列：那等于把「字在不在」交给材质决胜键（静默、且只在变灰时才现形）。
            var selTx = Text(btn, ButtonLabel(), new PxRect(BtL, BtT, BtR, BtB), Color.white, "Button Text",
                             CNmPx, 8, autoFit: true, autoMinPx: 10f, alignLeft: false, wrap: false,
                             autoMaxPx: 36f, basePx: 12f, face: TmpFont.Face.LatinCardBody);
            if (selTx != null) selTx.SetWrapping(false);
            _btnLabel = selTx;
            // 🆕 A17：原版 `Avatar Tab>Selected Item Panel>Select Avatar Button` 是 SpriteSwap（普查 §块 5 第 8 行）
            // ⚠️ 命中区顺延到 `Q+9`（原 `Q+8` 与字同号）—— 那颗 quad 是**透明**的，抬一号只为不留新的同号并列。
            var selHit = Hit(sel, "SelectHit", new PxRect(BtnL, BtnT, BtnR, BtnB), 9, OnSelectClicked, selQ, ArtButton,
                clickCue: WindowSounds.SelectAvatarCue);
            _btnHit = selHit != null ? selHit.GetComponent<WindowButton>() : null;

            var tbn = Node(sel, "Toggle borde", new PxRect(TbL, TbT, TbR, TbB));
            var tbQ = Rect(tbn, ArtButton, new PxRect(TbL, TbT, TbR, TbB), "Image", 7);
            // 🔴 **A62 · A30**：原版 `Toggle borde > Button Text`（`'Toggle Border'`）**`折行=0 auto[10~36]`** ⇒ 关掉。
            // 🔴 **A406**：`Avatar Tab/Selected Item Panel/Toggle borde/Button Text`
            //   原版 `auto[10.0~36.0] 基准=12.0` ⇒ 上限 36 · base **12.0**（与上面那颗**同值但是两棵、各读过一次**）。
            // 🔴 **2026-10-18（`W22`）**：文案**走官方词条**（原版这颗 `Localize.mTerm = "Demo/AvatarMenu/ToggleBorder"`，
            //   官方两列 `Toggle Border` / `切换边框`）—— 改前写死 `ToggleBorderLabel` 英文 ⇒ **中文档下这颗仍是英文**
            //   （用户 2026-10-18 第六轮真机截图点名）。⛔ 别再直接印常量。
            // 🔴 **2026-10-23（P5）**：与上面那颗钮同一条画序纪律 —— 字 `Q+8` **严格**在底图 `Q+7` 之上
            //   （这颗钮今天不变灰、所以改前「碰巧对」；它的队列关系与上面那颗**逐位同形**，
            //    留成同号就是留一颗同类的雷）。理由与实拍判据 → 上面 `Select Avatar Button` 那段。
            var tbTx = Text(tbn, Loc.T(ToggleBorderTerm), new PxRect(TbtL, TbtT, TbtR, TbtB), Color.white, "Button Text",
                            CNmPx, 8, autoFit: true, autoMinPx: 10f, alignLeft: false, wrap: false,
                            autoMaxPx: 36f, basePx: 12f, face: TmpFont.Face.LatinCardBody);
            if (tbTx != null) tbTx.SetWrapping(false);
            // 🆕 A17：`Toggle borde` 同族（普查 §块 5 第 9 行）；命中区 `Q+9`（同上面那条理由）
            Hit(sel, "BordeHit", new PxRect(TbL, TbT, TbR, TbB), 9, ToggleBorder, tbQ, ArtButton,
                clickCue: WindowSounds.SelectAvatarCue);

            Refresh();
        }

        /// <summary>格子。原版 = `Instantiate(itemPrefab, contentHolder)` 逐张生成
        /// （`AvatarTab__InitializeGameObjects.c:152`），**一屏只建看得见的那些**（视口外的整格不建）。</summary>
        void BuildRows()
        {
            var items = Avatars;
            int n = items.Count, cols = Columns;
            int rows = n == 0 ? 0 : (n + cols - 1) / cols;
            float contentH = rows == 0 ? 0f : PadT + rows * CellH + (rows - 1) * GapY;
            _scroll.ContentX2 = GridT + contentH;

            _cellArt.Clear(); _cellHl.Clear(); _cellIdx.Clear();
            for (int i = 0; i < n; i++)
            {
                int col = i % cols, row = i / cols;
                float x, y; CellXY(col, row, out x, out y);
                var r = _scroll.Shift(new PxRect(x, y, x + CellW, y + CellH));
                if (!_scroll.Intersects(r)) continue;
                // 🔴 **子件一律从偏移后的 `r` 起算**（`ox/oy` = 这一格**在屏幕上**的左上角）。
                //    拿未偏移的 `x/y` 摆子件 = 一滚就「底板走了、图与字没走」（2026-09-27 修，同 `ForgeTab` 的写法）。
                float ox = r.x1, oy = r.y1;

                var cell = Node(_grid, "Avatar Item Small_" + i, r);
                // 子件按**格子内偏移**摆（`Shift` 已经把整个格子搬到屏幕上了）
                // 🔴 **2026-10-24（E18）**：`Image Container` 是**布局框**（不放大）；它底下三层各自放大
                //    （头像 ×2.0 / 框 ×1.25 / 高亮 ×1.9，见 `ScaleImage` 那段常量注释）。
                //    ⚠️ **放大后的框会溢出 `Image Container` / 格子**（原版也溢出：头像实绘 360×285.26
                //    比容器 180×142.63 大一倍）—— 父链上**没有任何 `Mask`**，溢出不裁（判据 = S5 §①
                //    「无任何遮罩」）；**只有滚动视口那层 `RectMask2D` 照常裁边**，与原版一致。
                var ic = Node(cell, "Image Container",
                              new PxRect(ox + CImgL, oy + CImgT, ox + CImgR, oy + CImgB));
                var hl = Rect(ic, ArtHighlight,
                              ScaleAboutCenter(new PxRect(ox + CHlL, oy + CHlT, ox + CHlR, oy + CHlB),
                                               ScaleHighlight),
                              "Highlight", 1, null, true);
                if (hl != null) hl.gameObject.SetActive(i == Selected);   // 出厂 F，选中的那一格才亮
                // 🔴🔴 **2026-10-18（W18）画序订正（铁律 5）** —— **这一页 469 格全黑**的根因：
                //   原来 `Highlight 1 → Image 2 → Border 3` ⇒ **边框压在立绘上面**，而
                //   `Player_Profile_Border` 的中心是**不透明黑**（实测中心像素 `(0,0,0,255)`，
                //   透明只占 15.5%）⇒ 每格的立绘被一整块黑盖死（用户 2026-10-18 只看到 469 个空盾）。
                //   原版兄弟序 = **`Highlight → Border → Image`**（`Image` 最后 = 画在最上面；
                //   判据 = `资料/普查产出_0927/档案窗_Avatar与Title页.md` §A1 第 41–43 行，
                //   同族先例 = `ProfileTab.L_ArtOverFrame` 那段注释，2026-09-27 已在别的页修过同一条病）。
                //   ⇒ 队列改成 `Highlight 1 < Border 2 < Image 3`（Name 4 / Hit 5 顺延，互不重叠）。
                // 🔴 **2026-10-24（E18）**：框与立绘各自**绕布局框中心放大**（×1.25 / ×2.0）——
                //   放大后立绘仍然**整体压在框之上**（画序没动，只是两者都比布局框大）。
                Rect(ic, ArtBorder,
                     ScaleAboutCenter(new PxRect(ox + CBdL, oy + CBdT, ox + CBdR, oy + CBdB), ScaleBorder),
                     "Border", 2, null, true);
                WarnIfAvatarMissing(items[i].Art);     // ⛔ 取不到图必须出声（不许静默留白）
                var art = CosmeticRect(ic, items[i].Art,
                                       ScaleAboutCenter(new PxRect(ox + CArtL, oy + CArtT, ox + CArtR, oy + CArtB),
                                                        ScaleImage), "Image", 3);
                // `Avatar Name` 在**格子外**（oy+CNmT..oy+CNmB），落在行距里 —— 真值
                // 🔴 **2026-10-07（A62 子表 A · A31 / 判据 §③「碰巧对」）**：原版这一件是 **`折行=1`**，
                //   我们原来**没显式声明**（靠 `SetAutoFitBox` 顺带打开）⇒ 补 `wrap: true` 钉死。
                // 🔴 **A406（2026-10-12）逐站实读**：`Avatar Tab/Item Display Panel/Scroll Rect/Item Drawer/
                //   Avatar Item Small_Ref/Avatar Name` 原版 `auto[12.0~36.0] 基准=36.0`
                //   ⇒ 上限 36（= 标称）· base **36.0**（⚠️ 与上面大图那份的 base 同、上限同，但**两棵各读一次**）。
                // 🔴🔴 **2026-10-24（E12 · P1-7）：建成之后立刻 `SetActive(false)`** —— 原版这一件**不上屏**：
                //   ① prefab 里它 `m_IsActive = **F**`（`资料/普查产出_0927/档案窗_Avatar与Title页.md` §A，
                //      `Avatar Name` 那一行的「激活」列 = F）；
                //   ② 运行期只有 `AvatarDisplay.ChangeAvatar` 对它 `set_text`，**没有任何 `SetActive`**
                //      （`decomp_full/AvatarDisplay__ChangeAvatar.c` / `AvatarTab__OnAvatarClick.c`）；
                //   ③ 用户参考图 `账户-头像参考图.png` 的格子里**只有立绘、没有名字**。
                //   ⇒ **建、但关着**（同 `ProfileTab` 的 `Player Info/Avatar Item Small/Avatar Name` 那条处置）。
                //   ⛔ 别改成「不建」：节点留着，哪天原版那一格真打开就有东西可显示（本工程纪律：
                //     结构照原版建、显隐照原版字段）。
                var cellNameLb = Text(cell, items[i].Name, new PxRect(ox + CNmL, oy + CNmT, ox + CNmR, oy + CNmB),
                     Color.white, "Avatar Name", CNmPx, 4, autoFit: true, autoMinPx: CNmAutoMin, alignLeft: false, wrap: true,
                     autoMaxPx: 36f, basePx: 36f, face: TmpFont.Face.LatinCardBody);
                if (cellNameLb != null) cellNameLb.gameObject.SetActive(false);
                int captured = i;
                Hit(cell, "Hit", r, 5, () => Select(captured));
                _cellArt.Add(art); _cellHl.Add(hl); _cellIdx.Add(i);
            }
        }

        /// <summary>🆕 **2026-10-18（W18）**：头像贴图取不到时**出声**（⛔ 不许静默留白 —— 上一版整页
        /// 469 格全黑，运行期连一行日志都没有，只能靠真机截图发现）。
        /// 每个图名只报一次（469 格 × 每次滚动重建，不设闸会刷屏）。
        /// ⚠️ 这里**只是探一下**（`CardArt.Cosmetics` 内部有缓存，第二次是字典命中，不重复 IO）；
        /// 真正的绘制仍走 `CosmeticRect`（它取不到会记进宿主的 `MissingArt`，自检读那份）。</summary>
        static void WarnIfAvatarMissing(string art)
        {
            if (string.IsNullOrEmpty(art)) return;
            if (CardArt.Cosmetics(art) != null) return;
            if (!_warnedMissingArt.Add(art)) return;
            Debug.LogWarning("[Profile] 头像图**取不到**：`Resources/Art/avatars/" + art + "` ⇒ 这一格只剩盾框。"
                           + "清单来自 `Resources/profile_cosmetics.json`（生成器 `工具/gen_profile_cosmetics.py`），"
                           + "图的导入器 = `工具/import_original_art.py` 的 `COSMETIC_AVATAR_SRC`。");
        }
        static readonly HashSet<string> _warnedMissingArt = new HashSet<string>();

        /// <summary>这一屏建出来的格子数（自检用：469 条全建会卡，必须只建看得见的）。</summary>
        public int BuiltCells { get { return _cellArt.Count; } }

        /// <summary>🆕 **2026-10-24（E12 · P1-7）**：格子里**一共有几颗 `Avatar Name`**
        /// （证明是「**建了但关着**」而不是「根本没建」—— 这两件事在截图上一模一样，必须分开断）。</summary>
        public int CellNameCount
        {
            get
            {
                if (_grid == null) return 0;
                int n = 0;
                for (int i = 0; i < _grid.childCount; i++)
                    if (_grid.GetChild(i).Find("Avatar Name") != null) n++;
                return n;
            }
        }

        /// <summary>🆕 **2026-10-24（E12 · P1-7）**：格子那些 `Avatar Name` 里**现在有几颗是激活的**
        /// （原版恒 **0** —— prefab `m_IsActive = F`、运行期只 `set_text` 不 `SetActive`）。</summary>
        public int ActiveCellNameCount
        {
            get
            {
                if (_grid == null) return 0;
                int n = 0;
                for (int i = 0; i < _grid.childCount; i++)
                {
                    var nm = _grid.GetChild(i).Find("Avatar Name");
                    if (nm != null && nm.gameObject.activeSelf) n++;
                }
                return n;
            }
        }

        /// <summary>🆕 **2026-10-13（A465 · W-A435己）**：这一页那个**纵向**滚动区（**自检用** ——
        /// 同 `LeaderboardWindow.RowsScroll` / `BattleLogTab.RowsScroll` 那条理由：断言要能读到
        /// `ClipNode` 与 `Viewport` 两态）。⛔ 生产代码不用它。</summary>
        public MenuScroll RowsScroll { get { return _scroll; } }

        /// <summary>滚轮改了偏移 ⇒ 重画格子（挂在 `MenuScroll.OnChanged` 上）。
        /// 🔴 **必须先清**：回调会重入这里，不清的话每滚一格就叠一层（同 `ForgeTab.BuildRewardCells`
        /// 那条「幂等」注释）。
        /// 🔴 **2026-10-13（A435 阶段 2 · 丙）**：原来这里还重设了一次 `Clip`/`ClipSoftness`
        /// （`Build()` 那次是在 `BuildRows` 外面给的）—— **那一对删掉了**：裁切状态现在长在
        /// `Scroll Rect` 那个节点上，重画时**自动继续生效**，不需要重设。</summary>
        void RebuildRows()
        {
            if (_grid == null) return;
            for (int i = _grid.childCount - 1; i >= 0; i--) DestroyNow(_grid.GetChild(i).gameObject);
            BuildRows();
        }

        // ============================================================ 数据
        //
        // 🔴 清单来自**原版资产**（469 张可选头像 SO），不是玩家存档 —— 判据 → `Shell/ProfileData.cs` 文件头。
        //    ⚠️ 显示名是我们从资源名反推的（`Avatar_UM_Attack Bike` → `Attack Bike`）。
        public static List<ProfileData.Item> Avatars { get { return ProfileData.Avatars; } }

        /// <summary>「正在使用」词条（原版 `AvatarTab.ToggleSelectButton` 里「选中的那张 == 当前装备」那一支；
        /// 官方表 `PlayerProfile/InUse` = `In use` / `正在使用`；参考图两颗钮都印这个）。</summary>
        public const string InUseTerm = "PlayerProfile/InUse";

        /// <summary>当前**预览**的下标。🔴 **2026-09-27 收口：已装备那一份读写都走 `ProfileData.AvatarIndex`**
        /// （原来它是这个页签实例上的字段 ⇒ 关掉档案窗就没了，主菜单顶栏那块头像读不到它）。
        /// 🆕 **2026-10-22（P1）**：本属性从「已装备」改成「**预览**」（原版 `AvatarTab.currentItem` 语义）——
        /// 点格子只换预览；**装备**在 `Select Avatar Button`（`ConfirmSelect`）。
        /// ⚠️ **默认 0 是我们挑的**（原版的默认头像靠一个解不出来的常量挑，见 `ProfileData.AvatarIndex` 注释）。</summary>
        public int Selected { get { return PreviewIndex; } set { PreviewIndex = value; } }
        /// <summary>要不要画头像边框（`Toggle borde` 那个钮）。原版由 `PlayerAvatarDataManager` 的边框字段驱动；
        /// 🆕 2026-10-22 起**落盘**（`ProfileData.AvatarBorderOn`）。</summary>
        public bool BorderOn
        {
            get { return ProfileData.AvatarBorderOn; }
            set { ProfileData.AvatarBorderOn = value; }
        }

        /// <summary>点格子 = **只预览**（原版 `AvatarTab.OnAvatarClick` → `AvatarDisplay.Initialize` +
        /// `ToggleSelectButton`，**不**写 `PlayerAvatarDataManager`；装备在那颗钮上）。</summary>
        public void Select(int index)
        {
            if (index < 0 || index >= Avatars.Count) { Debug.Log("[Profile] 头像预览越界：" + index); return; }
            PreviewIndex = index;
            Refresh();
        }

        /// <summary>装备当前预览的那张 —— 原版 `AvatarTab.OnChoseAvatar/SetAvatar`（`SetCurrentItem` + 上传）。
        /// 我们的等价物 = `ProfileData.AvatarIndex`（唯一一份，会落盘）。</summary>
        public void ConfirmSelect()
        {
            if (PreviewIndex < 0 || PreviewIndex >= Avatars.Count)
            {
                Debug.Log("[Profile] `Select`（头像页）：**还没预览任何一张**");
                return;
            }
            ProfileData.AvatarIndex = PreviewIndex;
            Refresh();
            Debug.Log("[Profile] 已装备头像「" + Avatars[PreviewIndex].Name + "」（# " + PreviewIndex
                    + "）—— 写进 `ProfileData.AvatarIndex`（顶栏读它），并落盘到 "
                    + "`persistentDataPath/WarpforgeProfile/profile.json`");
        }

        /// <summary>每次切到本页：预览回到**已装备的那张**（原版 `RefreshDisplays` 末尾那条）。</summary>
        public override void OnOpen()
        {
            PreviewIndex = Mathf.Clamp(ProfileData.AvatarIndex, 0, Mathf.Max(0, Avatars.Count - 1));
            Refresh();
        }

        /// <summary>切「显不显示头像边框」。原版 `AvatarTab.ToggleAvatarBorder` → `SetCurrentItem(border, cb)`。
        /// 🆕 2026-10-22：结果落盘（`ProfileData.AvatarBorderOn`）。</summary>
        public void ToggleBorder()
        {
            BorderOn = !BorderOn;
            Refresh();
            Debug.Log("[Profile] 头像边框 = " + (BorderOn ? "显示" : "隐藏")
                    + "（已落盘：`ProfileData.AvatarBorderOn`；原版这一步是 `PlayerAvatarDataManager.SaveAvatarBorder`）");
        }

        /// <summary>把「当前预览的头像」刷一遍：大图那块（立绘 + 名）+ 格子上的选中高亮 + 边框显隐 + 那颗钮。
        /// 出处：原版 `AvatarTab.RefreshDisplays` → `AvatarDisplay.Initialize(...)`；
        /// 「旧选 `ToggleHighlight(false)` + 新选 `true`」那条链在 `AvatarTab__OnAvatarClick.c:25/28`。
        /// ⚠️ **不重建格子**（原版也只切 `Highlight` 的 active）—— 所以换预览**不需要重跑 `BuildRows`**。</summary>
        public void Refresh()
        {
            bool has = PreviewIndex >= 0 && PreviewIndex < Avatars.Count;
            if (_bigName != null) _bigName.SetText(has ? Avatars[PreviewIndex].Name : "");
            if (_bigArt != null)
            {
                var tex = has ? CardArt.Cosmetics(Avatars[PreviewIndex].Art) : null;
                if (tex != null) { _bigArt.SetTexture(tex); _bigArt.gameObject.SetActive(true); }
                else _bigArt.gameObject.SetActive(false);
            }
            if (_bigHighlight != null) _bigHighlight.gameObject.SetActive(false);   // 大图那份恒关（见常量注释）
            for (int k = 0; k < _cellHl.Count; k++)
                if (_cellHl[k] != null) _cellHl[k].gameObject.SetActive(k < _cellIdx.Count && _cellIdx[k] == PreviewIndex);
            // 边框：`Toggle borde` 那个钮管的（`AvatarTab.CheckEnableAvatarBorder`）。
            // ⚠️ 原版是按 `FindActiveAvatarBorder() != null` 决定**这个钮能不能点**，不是直接藏边框图；
            //    我们这里**简化为「开关边框那一层」** —— 这是**我们的做法**，如实标着。
            for (int k = 0; k < _grid.childCount; k++)
            {
                var bd = _grid.GetChild(k).Find("Image Container/Border");
                if (bd != null) bd.gameObject.SetActive(BorderOn);
            }
            // 钮：预览 == 已装备 ⇒ `PlayerProfile/InUse`（不可点）；否则 ⇒ Select（可点）。
            //   判据 = `AvatarTab__ToggleSelectButton.c` 的三分支（相等 / 未解锁 / 其他 ⇒ InUse / Locked / Select）。
            if (_btnLabel != null) _btnLabel.SetText(ButtonLabel());
            if (_btnHit != null) _btnHit.Interactable = has && PreviewIndex != ProfileData.AvatarIndex;
        }

        /// <summary>那颗钮当前该印什么（自检也读它）。</summary>
        public string ButtonLabel()
        {
            bool has = PreviewIndex >= 0 && PreviewIndex < Avatars.Count;
            return has && PreviewIndex == ProfileData.AvatarIndex
                 ? AvatarButtonTerm(InUseTerm, "In use", "正在使用")
                 : AvatarButtonTerm(BtnLabelTerm, "Select", "选择");
        }

        void OnSelectClicked() { ConfirmSelect(); }
    }
}
