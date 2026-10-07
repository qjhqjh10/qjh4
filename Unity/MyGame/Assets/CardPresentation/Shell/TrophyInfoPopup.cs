// TrophyInfoPopup.cs — 联盟奖杯格的**详情弹窗**（原版 `TrophyInfoPopup : GameWindow`）
//
// ============================ 出处（唯一正本） ============================
// `d:/2/新解包资源/assets_full/bundle_menus_assets_all/`：
//   · 根 GameObject `Alliance Trophy Info Popup` + 它身上那颗 MB（`MonoBehaviour_-3823665489305576323`）
//     （另一颗 `-3185861090812863363` 是 `TransformScalerBySmallScreenUI`）；
//   · 逐节点的矩形 / 图名 / 九宫 / 染色 / 字号 / 对齐 = **`menu_dump.py` 实读**
//     （`python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Alliance Trophy Info Popup" --md --depth 12`
//      —— ⚠️ **要比默认深度多挖两层**：`Fill Area` / `Fill` / `end` / `CheckMark` 那几件在默认深度下
//      **一个都不印出来**，照默认输出搭就会少一层）；
// 行为 = 反编译 `d:/2/tools/decomp_full/TrophyInfoPopup__{Start,Open,HandleFeatureTrophy}.c`；
// 触发 = `AllianceTrophiesView__HandleTrophyClick.c`（`WindowsManager.OpenWindow(trophyInfoPopup, 那一枚奖杯)`）。
//
// ---- 🔴 三条判据（实读，别推翻）----
// ① **窗参**（MB 逐字段实读）：`type = 1`(Popup) · `windowsPlacement = 15`(Popup) · `closeOnESC = 1` ·
//    `updateNavPanel = 0` · `extraScaleSmallScreen = 1.0` · `menuScale = 1.35`（小屏缩放那一颗 —— ✅ 2026-10-06
//    **A165 起真接上了**：`Open()` 里给窗体根 GO 挂 `TransformScalerBySmallScreenUI` + `TrophyMenuScale`）。
// ② **七个字段各指哪个节点**（pid 反查，7/7 逐个对上）：
//    `badgeDrawer → BadgeDrawer` · `trophyName → Title` · `trophyDescription → Descripton`（原版就这么拼）·
//    `progressHolder → Progress`（GO）· `progressBar → ProgressBar`（组件）· `toggle → Checkbox` ·
//    `closeButton → Generic Close Button Orange`。
// ③ **`Category` 出厂 act = F** ⇒ **不建**（同族口径：出厂关着的件不建，见 `MainMenuRuntime` 纪律③）。
//
// ---- 🔴 三处如实标注（本地拿不到的东西）----
// ① **奖杯数据全在服务器**：`AllianceTrophy` 由 `AlliancesTrophyController` 从**远端 LiveOps 配置**建
//    （`AllianceTrophyAssetProvider__GetAssets.c` → `ConfigManager.GetConfig`），
//    `AllianceTrophyData` 的名字 / 描述 / `Challenge` 全在那份配置里（本地没有它的副本）。
//    ⇒ **名字 / 描述 / 进度 / 徽标本地一条都拿不到** ⇒ 出厂按**零值**摆（`""` / `0/0`），
//    ⛔ **不拿 prefab 里的样例串**（`Trophy Name` / `Reach milestones Ultramarines Forge points…` / `100/200`）
//    充数（口径 = `SocialData` 文件头「别为了好看塞假数据」）。数据路本身是**接好的**（`SetTrophy`）——
//    自检用「喂一条数据」证明它真的铺得上去。
// ② **整条链本地走不到**：`SocialData.AllianceTrophies` 恒 0 ⇒ 奖杯格**一格都不建** ⇒ 没有可点的格子
//    ⇒ 这扇窗在正常路径上**开不出来**（**忠实地走不到**，不是漏做 —— 同 `AllianceMemberTab` 文件头 ①）。
//    自检里直接 `TrophyInfoPopup.Open(...)` 把它验一遍。
// ③ **掩码没做**：原版 `Background` / `Fill` / `Outline` 三个 `Image` 上挂着 `Mask(showGraphic=1)`，
//    进度 0 时 `Fill` 宽 0 ⇒ `end` 端帽本该被整个裁掉，我们**没有掩码体系**（同 `AllianceMemberTab`
//    奖杯格那条已知偏离）⇒ 端帽会画出来。判据与代价同那边 ①。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `TrophyInfoPopup`（`GameWindow` 子类 · 点奖杯格开的那扇窗）。
    /// <para>🔴 **它不在任何一页的层带里**：自成一档 **3310–3326**（社交页 3200–3209 与奖杯格 3210–3299
    /// 之上、聊天窗 3300–3308 之上；挑战弹窗 3400 / 战斗日志 3450 / 排行榜 3500 之下 —— 层带不许重叠）</para>
    /// <para>⚠️ **2026-10-11（A307 现读订正，铁律 5）**：这里原写「它同其它每一扇窗一样留在主菜单顶栏
    /// （**3600–3604**）**之下**……**用户 2026-09-28 按实拍拍板『顶栏压住窗』**」—— **号码、结论、理由三处全过期**：
    /// ① 顶栏**现在是 `2986–2998`**（同日 A283 先降到 `2994–2998`、2026-10-17 F5 又把下沿放宽到 `2986`；
    /// 用户 2026-10-11 裁定「照原版」）；
    /// ⚠️ **2026-10-18 订正**：本句原来写「顶栏**现在是 `2994–2998`**」—— 那个号段**已过期**
    /// （**唯一出处 = `Shell/TopBar.cs` 那张表**）；本句的**方向**没变（②③ 照旧）。
    /// ② 方向**反了** —— 本窗（3310–3326）如今**在顶栏之上**；
    /// ③ 那正是**原版**的排法（判据 = 兄弟序 + `Canvas_1075` 的 `PopUps` 排序层，
    ///    见 `Shell/MainMenuRuntime.cs` 的 `QBarPanel` 那段 · `资料/普查产出_1010/V4b_三件口径.md` §Q3）
    ///    ⇒ ✅ **本窗不再偏离原版**（⛔ 别再把「顶栏压在它上面」当待办）。</para></summary>
    public class TrophyInfoPopup : GameWindow
    {
        // ============================================================ 队列档（本窗自成一档 · 3310–3326）
        //   逐层的顺序 = **原版兄弟序**（uGUI 按兄弟序画 ⇒ 后面的压前面的；判据 = 各父节点的 `m_Children`）：
        //     根 = [Menu Dark Background, window]；
        //     window = [Generic Window Red Background Big, BadgeDrawer, Generic Close Button Orange, RightSide]；
        //     RightSide = [Title, Category(act F ⇒ 不建), Descripton, Controls]；
        //     Controls(VLG) = [Progress, selectButton]；
        //     Progress = [ProgressBar, **Next Tier**]（⇒ 原版里 `Next Tier` 排在 `ProgressBar` **之后**）；
        //     ProgressBar = [Background, Outline, counter]，而 `Background` = [Fill Area]、`Fill Area` = [Fill]、
        //     `Fill` = [end]（⇒ 槽底 < end < 描边 < 字）。
        //   🔴 **2026-10-05 订正 + 已照原版改回**：原来这里写「`Controls` = [Progress, **Next Tier**, selectButton]」
        //     —— **错的**。原始 `m_Children` 实读（`工具/menu_dump.py … --depth 12` 的缩进 +
        //     `工具/menu_rect.py` 爬的父链，两处一致）：
        //     `Controls` 只有 **2** 个子（`Progress` / `selectButton`），**`Next Tier` 是 `Progress` 的子**
        //     （`Progress` 的第 **2** 个、也是最后一个子 —— 排在 `ProgressBar` 后面）。
        //   ⚠️ **我们原来把它建在 `RightSide` 下**（且创建顺序早于 `Progress`）—— **父错**。
        //     这个错能活到今天，靠的是两件事的叠加：
        //       ① `FindChild` 走 `GetComponentsInChildren`（= **整棵子树**）⇒ 「在 `RightSide` 底下」照样捞得到；
        //       ② `MenuDraw.Local` / `Label.AlignLeftOn` 都是「**世界坐标 − 父的世界坐标**」⇒ 换父**世界矩形逐位不变**。
        //     ⇒ 断言全绿、画面全对，**只有层级是错的**。修法见 `Build` 里 `5a·2` 那一段；
        //     自检那两条「**直系子**」断言见 `MainMenuScene.Run` 的「进度条那一叠」一节
        //     （⛔ `FindChild` 走整棵子树，**看不见**这个错）。
        public const int QShade = 3310,        // 压暗整屏（`Menu Dark Background`）
                         QWinBg = 3311,        // 面板底（`Generic Window Red Background Big`）
                         QBadge = 3312,        // `BadgeDrawer`（节点，无图）
                         QBadgeArt = 3313,     // └ `Frame` / `Badge`（图在服务器 ⇒ 今天不画）
                         QText = 3314,         // `Title` / `Descripton`（`Next Tier` 原来也借这一档 —— 2026-10-06 A95 起挪去 `QNextTier`）
                         QBarSlot = 3315,      // `ProgressBar>Background`（槽底）
                         QBarFill = 3316,      // `Fill Area>Fill`
                         QBarEnd = 3317,       // `Fill>end`（端帽，压槽底）
                         QBarFrame = 3318,     // `ProgressBar>Outline`（压 `end`，两者重叠 ≈ 5.7px）
                         QBarText = 3319,      // `counter`（`ProgressBar` 的最后一颗子件 ⇒ 压 `Outline`）
                         QNextTier = 3320,     // `Progress`> **`Next Tier`**（🆕 2026-10-06 A95：原版兄弟序 = `ProgressBar` **之后** ⇒ 必须压在 `QBar*` 之上）
                         QCheckBox = 3321,     // `Checkbox>Toggle`（那个方框）
                         QCheckMark = 3322,    // └ `CheckMark`（勾）
                         QCheckText = 3323,    // `Checkbox>Label`
                         QClose = 3324,        // 关窗圆钮的底（`UI_Button_Round_background`）
                         QCloseIcon = 3325;    // └ `Icon`（`40k_general_bt_yellow_close`）
        /// <summary>本窗**内容命中区**那一档（压暗层命中区必须**严格低于**它 —— `MenuDraw.ShadeHit` 现场核）。
        /// 两个内容命中区（关窗钮 / 勾选行）**在屏幕上不重叠** ⇒ 同号无害。</summary>
        public const int QHit = 3326;

        /// <summary>🆕 **2026-10-06（A165）**：**烤在 prefab 里**的那颗小屏缩放器带的倍数
        /// （原版 `TransformScalerBySmallScreenUI.menuScale`）。
        /// <para>判据 = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-3185861090812863363.json`
        /// —— 挂在**窗体根 GO** `Alliance Trophy Info Popup` 上（`m_GameObject.m_PathID = -811541866307429251`）、
        /// `m_Enabled: 1`、`m_Script.m_PathID = 3361136396530371980`，**`menuScale = 1.350000023841858`**。</para>
        /// 🔴 **它 ≠ `extraScaleSmallScreen`**（窗参那颗 MB 写的是 **1.0**，含义是「**不覆盖**」）——
        /// 小屏下真正生效的是这个 **1.35**；只读 `extra` 的实现在这一扇上会**静默不放大**
        /// （判据全文 → `Shell/TransformScalerBySmallScreenUI.cs` 文件头 ③）。</summary>
        public const float TrophyMenuScale = 1.35f;

        // ============================================================ 真值（dump 实读 · 绝对画布像素）
        // 🔴 **2026-10-05 重取（A88）**：`工具/menu_dump.py` 的 `_child_sizes` 补成**完整 uGUI**之后，
        //   `Controls`(VLG) 的两个子件跑后值变了 —— **`selectButton` 与 `Checkbox` 各 +39.54px**
        //   （`Progress` 不动：它是**第一格**，起点 = `padding.top`，撑开量落在它**下面**那一段）。
        //   算式（本地 uGUI 源码 `HorizontalOrVerticalLayoutGroup.cs:186-216`）：
        //     组高 187.926、两格各 `sizeDelta.y = 54.4217` ⇒ `总首选 = 108.8434`（`spacing = 0`）
        //     ⇒ `surplus = 79.0826`、`总 flexible = 2` ⇒ `fmul = 39.5413`
        //     ⇒ 每格 `childSize = 54.4217 + 39.5413 = 93.963`（**步进按它算**）；
        //     `align = 0`(UpperLeft) ⇒ `alignmentOnAxis = 0` ⇒ `offsetInCell = 0` ⇒ 第一格位置不变。
        //   ⇒ `selectButton` 起点 `569.04 + 93.963 = 663.00`（旧值 623.46 = W1/W2 那两版的输出，**已作废**）。
        //   重取命令：`python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Alliance Trophy Info Popup" --depth 12 --md`
        //   ⚠️ **`Next Tier` 的父是 `Progress`（原始 `m_Children` 实读），不是 `Controls`**（订正见上「队列档」那节）。
        static readonly PxRect ShadeR    = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);  // 全库统一那个压暗矩形
        static readonly Color  ShadeCol  = new Color(0f, 0f, 0f, 0.773f);                       // Simple (0,0,0,0.773)
        static readonly PxRect WinR      = new PxRect(395.72f, 188.35f, 1524.28f, 851.65f);     // `window`
        static readonly PxRect WinBgR    = new PxRect(395.70f, 178.35f, 1547.30f, 895.80f);     // `Generic Window Red Background Big`
        static readonly PxRect BadgeR    = new PxRect(435.87f, 267.52f, 944.13f, 744.48f);      // `BadgeDrawer`（= `Frame`/`Badge` 同矩形）
        static readonly PxRect CloseR    = new PxRect(1487.08f, 159.85f, 1561.47f, 235.45f);    // `Generic Close Button Orange`
        static readonly PxRect CloseBgR  = new PxRect(1495.24f, 167.83f, 1552.10f, 225.96f);    // └ `Background` / `Icon`（同矩形）
        static readonly PxRect RightR    = new PxRect(960.00f, 204.35f, 1508.28f, 835.65f);     // `RightSide`
        static readonly PxRect TitleR    = new PxRect(976.12f, 307.85f, 1492.84f, 359.85f);     // `Title`
        static readonly PxRect DescR     = new PxRect(976.00f, 352.85f, 1477.24f, 532.89f);     // `Descripton`（原版拼写）
        static readonly PxRect NextTierR = new PxRect(976.12f, 532.80f, 1477.12f, 569.04f);     // `Next Tier`
        static readonly PxRect ControlsR = new PxRect(976.12f, 569.04f, 1477.12f, 756.96f);     // `Controls`（VLG）
        static readonly PxRect ProgressR = new PxRect(976.12f, 569.04f, 1477.12f, 623.46f);     // └ `Progress`（= `progressHolder`）
        static readonly PxRect BarR      = new PxRect(976.12f, 560.78f, 1477.12f, 629.38f);     //   └ `ProgressBar`
        static readonly PxRect BarBgR    = new PxRect(976.12f, 574.50f, 1477.12f, 615.66f);     //     └ `Background`
        static readonly PxRect FillAreaR = new PxRect(976.12f, 579.45f, 1477.12f, 610.71f);     //       └ `Fill Area`
        static readonly PxRect EndR      = new PxRect(952.38f, 595.78f, 981.82f, 627.64f);      //         └ `Fill>end`（**出厂位**）
        static readonly PxRect CounterR  = new PxRect(1076.32f, 581.36f, 1376.92f, 615.66f);    //     └ `counter`
        static readonly PxRect SelectR   = new PxRect(976.12f, 663.00f, 1477.12f, 717.42f);     // └ `selectButton`（`toggle` 字段那颗的父）
        static readonly PxRect CheckR    = new PxRect(990.39f, 669.83f, 1477.12f, 728.03f);     //   └ `Checkbox`

        // 九宫：`Generic Window Red Background Big` = `UI_Deck_Information_Back`
        //   （1100×701 · `m_Border = (42,363,655,81)` —— **与 `BattleLogPopup` 那张面板同一张图同一组 border**）
        static readonly Vector4 WinBgBorder = new Vector4(42f, 363f, 655f, 81f);
        const float WinBgTexW = 1100f, WinBgTexH = 701f;
        // 进度条四件（`m_Border` 与 `ppuMul` 与奖杯格那一套**逐值相同** —— 同一批图；判据见 `AllianceMemberTab.NineSoft`）
        static readonly Vector4 BarBorder   = new Vector4(20f, 0f, 20f, 0f);
        static readonly Vector4 BarBorder90 = new Vector4(20f / 0.9f, 0f, 20f / 0.9f, 0f);
        static readonly Vector4 FillBorder   = new Vector4(10f, 0f, 10f, 0f);
        static readonly Vector4 FillBorder90 = new Vector4(10f / 0.9f, 0f, 10f / 0.9f, 0f);
        static readonly Color SlotCol = new Color(0.299f, 0.289f, 0.689f, 1f);
        static readonly Color FillCol = new Color(1f, 0.509f, 0f, 1f);
        static readonly Color EndCol  = new Color(1f, 1f, 1f, 0.698f);
        static readonly Color OutCol  = new Color(1f, 0.841f, 0f, 1f);
        static readonly Color MarkCol = new Color(0.575f, 0.209f, 0.209f, 1f);

        /// <summary>🔴 **2026-10-18（第十一轮）：`Next Tier` 那一行走词条**。
        /// <para>键 = 原版那颗 `Next Tier` 的 `Localize.mTerm` 原文 **`SocialMenu/Alliances/Trophies/NextTier`**
        /// （本批按 pid 亲读；父链 = `Next Tier < Progress < Controls < RightSide < window < Alliance Trophy Info Popup`）。
        /// 英文列 = 那颗 TMP 的 `m_text` 原文 **`Next Tier:`（带冒号）**；中文列「下一档：」**我们自拟**（`zh_CN.csv` 无该串）。</para>
        /// <para>⚠️ **2026-10-18（第十一轮）就地订正（铁律 5）**：下面这段原来写「它身上带 `Localize` ⇒ 原版走 I2 词条，
        /// 而**词条表在远端 CCD** ⇒ 只能照抄 prefab 里那个串本身」—— **后半不成立**：
        /// 那颗 `Localize` 的 **`mTerm` 就在 prefab 上**（键名本地读得到），取不到的是**译文**（远端 I2），
        /// 而我们的**中文那一列本来就是我们自己译的**（本表通用口径）⇒ **完全可以接词条**，不必只照抄英文串。
        /// 同族那次同类错（`AlliancesTab.LabelBack`）另见 §15。</para>
        /// <para>⛔ **别改回 `const`** —— 这扇窗可以反复开（`Open()` 每次都会重建那一行），
        /// 常量会在第一次求值后就冻住（换语言不再变）。消费点只有 `:382` 那一处。</para></summary>
        public static string NextTierText { get { return Loc.T("SocialMenu/Alliances/Trophies/NextTier"); } }
        /// <summary>勾选行那句 —— 同上（键 = 原版那颗 `Label` 的 `Localize.mTerm` 原文
        /// **`SocialMenu/Alliances/Trophies/FeaturedTrophyLabel`**；父链 = `Label < Checkbox < selectButton
        /// < Controls < RightSide < window`）。英文列 = TMP 原文 `Alliance featured trophy`；
        /// 中文列「联盟精选奖杯」= **`zh_CN.csv:53` 精确命中**。⛔ 别改回 `const`（理由同上）。</summary>
        public static string FeatureLabel { get { return Loc.T("SocialMenu/Alliances/Trophies/FeaturedTrophyLabel"); } }

        /// <summary>最近一次开出来的那一扇（自检用，同 `SocialWindow.LastOpened` 那条先例）。</summary>
        public static TrophyInfoPopup LastOpened { get; private set; }

        // ============================================================ 数据（本地无源 ⇒ 零值态）
        /// <summary>一枚奖杯要显示的东西。**原版是从服务器的 `AllianceTrophy` 上读的**
        /// （`TrophyInfoPopup__Open.c`：`Data.LocalizedName` / `Data.LocalizedDescription` /
        /// `Save.CurrentValue` / `GetTargetMilestone()` / `IsFeatured`）—— 本地没有那个源 ⇒ 出厂全零。</summary>
        public struct TrophyView
        {
            public string Name, Description;
            public int Value, Max;
            public bool Featured;
            /// <summary>原版：`progressHolder.SetActive(!progressBar.IsFilled &amp;&amp; !Data.DontShowProgress)`。
            /// 那两个值本地都判不出来（在服务器）⇒ 走 prefab 出厂态（亮）。</summary>
            public bool ShowProgress;
            public static TrophyView Empty
            {
                get
                {
                    return new TrophyView { Name = "", Description = "", Value = 0, Max = 0,
                                            Featured = false, ShowProgress = true };
                }
            }
        }

        TrophyView _view = TrophyView.Empty;
        /// <summary>当前铺的那一份（自检读口）。</summary>
        public TrophyView View { get { return _view; } }
        /// <summary>`toggle` 字段那一颗（`Checkbox` 上的 `EverguildToggle`）的 `isOn`。
        /// 🔴 我们**只存状态、没有 `EverguildToggle` 组件**（本工程没有 uGUI 事件），视觉 = 勾那颗的 alpha。</summary>
        public bool ToggleIsOn { get; private set; }

        // 建出来的件（`Apply` 要改它们）
        Label _title, _desc, _nextTier, _counter, _checkLabel;
        Transform _progressHolder, _fillArea;
        ImageQuad _fill, _endCap;
        readonly List<ImageQuad> _checkMarks = new List<ImageQuad>();

        // ============================================================ 开 / 关

        /// <summary>建 + 开（原版那条路：`WindowsManager.OpenWindow(trophyInfoPopup, 那一枚奖杯)`）。</summary>
        public static TrophyInfoPopup Open(WindowsManager mgr, TrophyView? view = null)
        {
            var go = new GameObject("Alliance Trophy Info Popup");     // 节点名照原版（自检也要按它找）
            var win = go.AddComponent<TrophyInfoPopup>();
            win.type = WindowType.Popup;                  // 实证 type = 1
            win.placement = WindowsPlacement.Popup;       // 实证 windowsPlacement = 15
            win.closeOnEsc = true;                        // 实证 closeOnESC = 1
            win.extraScaleSmallScreen = 1f;               // 实证 1.0
            // 🆕 **2026-10-06（A165）**：把原版**烤在 prefab 里**的那颗小屏缩放器补上 —— 原版窗体根 GO 上除了
            //   `TrophyInfoPopup` 那颗 MB（`-3823665489305576323`）还有一颗 `TransformScalerBySmallScreenUI`
            //   （`MonoBehaviour_-3185861090812863363.json`，`menuScale = 1.35`，见 `TrophyMenuScale` 的注释）。
            //   🔴 **为什么必须补**：`extraScaleSmallScreen = 1.0` 是「**不覆盖**」语义 ⇒ 小屏下起作用的是烤着的 1.35。
            //   （窗口根上带成品的另外两扇 `AllianceMemberOptionsPopup` / `GenericOptionsPanel` **我们没建** ——
            //    将来建它们时同样要烤 1.35；窗口根上带成品的**全库只有这 3 个**。）
            //   ⚠️ 我们是**代码建窗**：`AddComponent` 那一刻 `OnEnable` 就跑过了（那时 `menuScale` 还是 1）
            //      ⇒ 赋完值必须**显式 `Initialize()`**（同族先例：`WindowHolder.RegisterNow`）。
            var smallScreenScaler = go.AddComponent<TransformScalerBySmallScreenUI>();
            smallScreenScaler.menuScale = TrophyMenuScale;
            smallScreenScaler.Initialize();
            win.Manager = mgr;
            win._view = view ?? TrophyView.Empty;
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[TrophyPopup] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            Build();                 // `Build` 末尾会 `Apply()`（建 + 铺一次到位）
            // 🔴 两条如实出声（红线：不许静默失败）—— 数据在服务器 + 整条链本地走不到。
            Debug.Log("[TrophyPopup] 开了 `TrophyInfoPopup`（原版 `AllianceTrophiesView.HandleTrophyClick` → "
                    + "`WindowsManager.OpenWindow(trophyInfoPopup, 那一枚奖杯)`）。"
                    + "⚠️ 这一扇**本地走不到**（`SocialData.AllianceTrophies` 恒 0 ⇒ 奖杯格一格都不建 ⇒ 没有可点的格子）；"
                    + "⚠️ 名字 / 描述 / 进度 / 徽标全在服务器 ⇒ 现在是**零值态**（⛔ 没拿 prefab 的样例串充数）。");
        }

        /// <summary>铺一条数据（**自检用**，也是数据路唯一的入口；产品路径上那份数据来自服务器）。</summary>
        public void SetTrophy(TrophyView v) { _view = v; Apply(); }

        // ============================================================ 建

        /// <summary>照原版 prefab 逐节点搭（**自检与运行时同一条路**）。末尾调 `Apply()` 把 `_view` 铺上去。</summary>
        public void Build()
        {
            var root = transform;
            MenuDraw.ClearChildren(root);

            // ---- 1) `Menu Dark Background`：压暗整屏 + **点背景就关**（原版 `BackgroundCloseButton`）----
            MenuDraw.Rect(root, CardArt.Solid(), ShadeR, "Menu Dark Background", QShade, ShadeCol);
            // 🔴 压暗层命中区用**压暗层自己那一档**（`QShade`），且**严格低于**本窗内容命中区档（`QHit`）
            //    —— `MenuDraw.ShadeHit` 现场核这条不变量（同 `ImportDeckPopup` / `BoosterInfoPopup` 那条口径）。
            MenuDraw.ShadeHit(root, ShadeR, QShade, QHit, () => Close(), "BackgroundHit");

            // ---- 2) `window` + 面板底（九宫 `UI_Deck_Information_Back`）----
            var win = MenuDraw.Node(root, "window", WinR);
            MenuDraw.Nine(win, Tex("UI_Deck_Information_Back"), WinBgR, WinBgBorder,
                          WinBgTexW, WinBgTexH, QWinBg, null, true, "Generic Window Red Background Big");
            // 🆕 **2026-10-06（A94）：窗底那块红底吸收点击**。判据 = 原版 prefab
            //   `Alliance Trophy Info Popup > window > Generic Window Red Background Big` 那颗 `Image` 的
            //   **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读），rect = 395.70,178.35→1547.30,895.80。
            //   ⚠️ 用 `WinBgR`（那颗 `Image` 自己的 rect，**比 `window` 容器大一圈**）。
            MenuDraw.Absorb(root, "AbsorbHit", WinBgR, QShade, QHit);

            // ---- 3) `BadgeDrawer`：盟徽（`Frame` + `Badge` 两个节点）----
            //   原版这两颗是 `Image` 但 `m_Sprite = <无图>` —— 图由 `AllianceBadgeDrawer.Draw(GroupBadge)` /
            //   `SetTierFrame(tier)` 在运行期从**服务器**给的 `GroupBadge` 上贴 ⇒ 我们**只建节点、不画图**
            //   （同 `AllianceMemberTab` 奖杯格里 `BadgeDrawer` 那一段的口径）。
            var badge = MenuDraw.Node(win, "BadgeDrawer", BadgeR);
            MenuDraw.Node(badge, "Frame", BadgeR);
            MenuDraw.Node(badge, "Badge", BadgeR);

            // ---- 4) `Generic Close Button Orange`：圆底 + 关窗图标 + 悬停/按下换图 ----
            //   底 `UI_Button_Round_background`（237×237 · Simple · preserveAspect）；
            //   里面两颗 `40k_general_bt_yellow` / `40k_general_bt_yellow_close`，**同矩形**；
            //   `m_SpriteState` = `HL 40k_general_bt_yellow_hover` · `P 40k_general_bt_yellow_pressed`
            //   （dump 那一列 `HL=… P=…` 实读 —— ⭐ **是原版值，不是按命名猜的**；⚠️ 库里**没有**
            //    `40k_general_bt_yellow_close_hover`，所以必须显式传这两个名，别让 `Bind` 去推）。
            //   🔴 **2026-10-04（修红）结构订正**：原来把这三颗都建成 `window` 的**兄弟**（命名对、层级错）
            //   —— 原版 `Generic Close Button Orange` 的 `m_Children` = `[Background, Icon]`（dump 的缩进
            //   也写着它们是**子件**）。⛔ 层级错了会**静默**：节点按名字找得到、`FindChild(父, "Background")`
            //   却找不到（同步点实跑就是这么红的：6 条「节点不在」）。
            //   ⚠️ 我们这颗钮的**自己那张图**（原版是钮身上那颗 `Image`）只能另开一层 —— 本工程一个
            //   `ImageQuad` 占一个 GameObject ⇒ 命名成 `Image`（照本仓「节点自己那颗图」的先例）。
            var closeNode = MenuDraw.Node(win, "Generic Close Button Orange", CloseR);
            var closeBg = MenuDraw.Rect(closeNode, Tex("UI_Button_Round_background"), CloseR,
                                        "Image", QClose, null, true);
            MenuDraw.Rect(closeNode, Tex("40k_general_bt_yellow"), CloseBgR,
                          "Background", QClose, null, true);
            MenuDraw.Rect(closeNode, Tex("40k_general_bt_yellow_close"), CloseBgR,
                          "Icon", QCloseIcon, null, true);
            var closeHit = MenuDraw.Hit(closeNode, "CloseHit", CloseR, QHit, () => Close(),
                                        closeBg, "UI_Button_Round_background",
                                        "40k_general_bt_yellow_hover", "40k_general_bt_yellow_pressed");
            if (closeHit == null)
                Debug.LogWarning("[TrophyPopup] 关窗钮的命中区没建出来（`MenuDraw.Hit` 返回 null）"
                               + " ⇒ **点它关不了窗**（只能点背景或 ESC）。");

            // ---- 5) `RightSide`：标题 / 描述 / 下一级 / 进度 / 勾选行 ----
            var right = MenuDraw.Node(win, "RightSide", RightR);
            // ⛔ `Category`（`976.00,307.85→1492.72,352.85`）**不建** —— 出厂 act F（判据③）。
            // 🔴 **`Title` 的字号：`fontPx = 40` 是「上限档」，不是设计者填的输入**（2026-10-04 修红时读原始 MB 才看清）：
            //    `MonoBehaviour_2410309707926101117.json`（= `trophyName` 字段指的那一颗）实读 ——
            //    `m_text 'Trophy Name'` · **`m_fontSize 40`（= 自适应**收敛结果**，恰好顶到上限）** ·
            //    **`m_fontSizeBase 36`（设计者填的输入档）** · `m_fontSizeMin/Max 3 / 40` · `m_enableAutoSizing 1` ·
            //    `m_HorizontalAlignment 1 (Left)` · `m_TextWrappingMode 1 (Normal)`。
            //    ⇒ 我们按本仓既有口径传 `m_fontSize`(=40) 当 `fontPx`（它同时就是 `m_fontSizeMax`）✓；
            //    ⛔ **别拿运行时读回来的字号去断「等于 40」** —— 汉字行盒比拉丁高，TMP 会让一档（实测 34.29）。
            //    🆕 **2026-10-12（A333 + A336③）**：第 4/5 个实参 = 原版 `m_fontSizeMax` / `m_fontSizeBase`
            //    = **40 / 36**（上限 = 标称 ⇒ A333 上本来就对；base 是本窗口第一份逐颗实读）。
            _title = MenuDraw.TextBox(right, TitleR, "", Color.white, "Title", 40f, 3f, QText, 40f, 36f);
            if (_title != null) MenuDraw.AlignLeft(_title, TitleR);
            // 🆕 **2026-10-12（A336③）**：`Alliance Trophy Info Popup/window/RightSide/Descripton` 实读
            //    = `fs 35 · auto[15~35] · base **36.0**`（上限 = 标称 ⇒ A333 本来就对）。
            _desc = MenuDraw.TextBox(right, DescR, "", Color.white, "Descripton", 35f, 15f, QText, 35f, 36f);
            if (_desc != null) MenuDraw.AlignLeft(_desc, DescR);
            // 🔴 **`Next Tier` 原来就建在这一行**（父 = `right` = `RightSide`）—— **层级错**：
            //    原版它是 **`Progress` 的第 2 个子**（`Progress` = [`ProgressBar`, `Next Tier`]）。
            //    2026-10-05 已挪进 `Progress` 下，见下面 `5a·2` 那一段（那里写了为什么世界矩形不变、
            //    以及为什么这个错一直没人报警）。

            // `Controls`（`VerticalLayoutGroup`）—— 三个子件的矩形**都是布局跑之后的值**（dump 给的就是后值）
            var controls = MenuDraw.Node(right, "Controls", ControlsR);

            // ---- 5a) `Progress`（= `progressHolder`）> `ProgressBar` > { Background > Fill Area > Fill > end, Outline, counter } ----
            _progressHolder = MenuDraw.Node(controls, "Progress", ProgressR);
            var bar = MenuDraw.Node(_progressHolder, "ProgressBar", BarR);
            var bgTex = Tex("40k_campaign_bar_bg");
            var slot = bgTex != null
                ? MenuDraw.Nine(bar, bgTex, BarBgR, BarBorder, bgTex.width, bgTex.height, QBarSlot,
                                SlotCol, true, "Background", Border90(BarBorder))
                : null;
            var fillParent = slot != null ? slot.transform : bar;   // 取不到图时退回挂 `ProgressBar`（不静默丢节点）
            // `Fill Area`（父 = `Background` —— 原版树就是这层嵌套，⚠️ **不是**挂在 `ProgressBar` 下）
            _fillArea = MenuDraw.Node(fillParent, "Fill Area", FillAreaR);
            var outTex = Tex("40k_campaign_bar_outline");
            if (outTex != null)
                MenuDraw.Nine(bar, outTex, BarBgR, BarBorder, outTex.width, outTex.height, QBarFrame,
                              OutCol, true, "Outline", Border90(BarBorder));
            // 🆕 **2026-10-12（A336③）**：`…/Controls/Progress/ProgressBar/counter` 实读
            //    = `fs 35 · auto[12~35] · base **36.0**`（上限 = 标称）。
            _counter = MenuDraw.TextBox(bar, CounterR, "0/0", Color.white, "counter", 35f, 12f, QBarText, 35f, 36f);

            // ---- 5a·2) `Next Tier`（**原版是 `Progress` 的第 2 个子**，排在 `ProgressBar` 之后）----
            //   🔴 **2026-10-05 结构订正（父错 → 照原版改回）**：原来它建在 `right`（= `RightSide`）下、
            //     而且创建顺序还早于 `Progress`。原始 `m_Children` 实读（两处一致）：
            //       · `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Alliance Trophy Info Popup" --depth 12`
            //         —— `Next Tier` 缩进在 **`Progress` 之下一层**（`Controls` › `Progress` › `Next Tier`）；
            //       · `工具/menu_rect.py` 爬父链：`Controls`(rt 707947216465321085) 的 `m_Children` = **2 个**
            //         （`Progress` / `selectButton`），而 `Progress`(rt -4716729376017064835) 的 `m_Children`
            //         = [`ProgressBar`, **`Next Tier`**]（后者 rt 1053000965615148157）。
            //   ⚠️ **这个错为什么一直没人报警**（两件事叠加，缺一不可）：
            //     ① 断言用的是 `FindChild` = `GetComponentsInChildren`（**整棵子树**）⇒ 挂在 `RightSide` 下
            //        照样捞得到、量的矩形也照样对（正是 `MainMenuScene.cs` 那条「`FindChild` 走整棵子树
            //        ⇒ 那样写恒真」的同一个坑）；
            //     ② `MenuDraw.Local`（`Shell/MenuDraw.cs:25-26`）= `RectCenter − parent.position`，
            //        而 `Label.AlignLeftOn`（`Battle/Label.cs:519-527`）也是「**世界 x − 父的世界 x**」
            //        ⇒ 换父时局部坐标**按新父重算** ⇒ **世界矩形逐位不变**。
            //   ⇒ 只有**层级**是错的：`FindChild(<RightSide 的子树>, "Next Tier")` 恒非 null，
            //      `NthChild(<RightSide>, "Next Tier", 0)`（**直系子**）才分得出来。
            //     自检那两条直系子断言见 `Editor/MainMenuScene.cs` 的「进度条那一叠」一节。
            //   🔴 **2026-10-06（A95）：档位已照原版兄弟序重排** —— `Next Tier` 现在自成一档 `QNextTier`(3320)，
            //     严压在 `ProgressBar` 那一叠（`QBarSlot` 3315 … `QBarText` 3319）**之上**。
            //     改之前它借的是 `QText`(3314)（= 与 `Title` / `Descripton` 同档），与原版兄弟序**不符**：
            //     原版 `Progress` 的 `m_Children` = [`ProgressBar`, `Next Tier`] ⇒ `Next Tier` 画在 `ProgressBar` 之上。
            //   ⚠️ **改之前实测过「零可观测差异」**（`Next Tier` 的框底 = `569.04`，而 `ProgressBar` 那几件
            //     **真画得出像素**的全在其下 —— `Background` / `Outline` 从 `574.50` 起、`counter` 从 `581.36` 起、
            //     `end` 从 `595.78` 起；`ProgressBar` 自己那颗节点没有 `Image` ⇒ 两者连一个像素都不重叠）——
            //     那**只是当时那一个数据点**，说的是「这一帧画面上看不出差别」，
            //     ⛔ **不构成「可以不做」的理由**（铁律 11：与原版不符 ⇒ 照做，只有先后之分，没有做不做）。
            //     照兄弟序重排档位就得给整条带子重编号（连带 `MainMenuScene.Run` 里那几条队列断言）——
            //     2026-10-06 **已经做了**：带子从 3310–3325 变成 **3310–3326**，一个空号都没有。
            //   ⚠️ 挂进来时**别打乱 `ProgressBar` 那一支的兄弟序**：本句在最末 ⇒ `Progress` 的子件
            //     恰好是 [`ProgressBar`, `Next Tier`]，与 `m_Children` 逐位一致。
            // 🆕 **2026-10-12（A336③）**：`…/Controls/Progress/Next Tier` 实读
            //    = `fs 35 · auto[3~35] · base **39.0**` —— ⚠️ **与同一棵树上另外三颗不同值**
            //    （`Title`/`Descripton`/`counter` 是 36）⇒ 逐个读、⛔ 别拿一个顶一片（铁律 5·c）。
            _nextTier = MenuDraw.TextBox(_progressHolder, NextTierR, NextTierText, Color.white, "Next Tier", 35f, 3f,
                                         QNextTier, 35f, 39f);
            if (_nextTier != null) MenuDraw.AlignLeft(_nextTier, NextTierR);

            // ---- 5b) `selectButton` > `Checkbox`（`EverguildToggle`：方框 + 勾 + 文字）----
            var select = MenuDraw.Node(controls, "selectButton", SelectR);
            var box = MenuDraw.Node(select, "Checkbox", CheckR);
            //   ⚠️ **方框与文字的位置是【我们挑的】**：dump 里那两个子件是**退化值**（都 0 宽、贴在右端）——
            //      因为那层 `HorizontalLayoutGroup` 的首选宽度要 Unity 的字体度量，dump 算不出来
            //      （它的告警原文：「下面这些布局组的主轴尺寸算不准 …… 表里那几个子节点的值**别照抄**」）。
            //      按本仓先在的约定摆（`SettingsWindow.BuildCheckRow`）：**方框在左、文字在右**。
            var boxR = new PxRect(CheckR.x1, CheckR.y1, CheckR.x1 + CheckR.H, CheckR.y2);
            var labelR = new PxRect(boxR.x2 + 10f, CheckR.y1, CheckR.x2, CheckR.y2);
            // 🔴 **2026-10-04（修红）结构订正**：`CheckMark` 原版是 **`Toggle` 的子件**（dump 的缩进 +
            //   `RectTransform` 的 `m_Children`），原来建成了 `Checkbox` 的兄弟 ⇒ 与关窗钮那三颗同一个错
            //   （层级错了**静默**：`FindChild(Toggle, "CheckMark")` 找不到）。现在照原版：
            //   `Checkbox` > `Toggle`（钮自己那颗图 = `40K_dropdown_bg`，本仓命名成 `Image`）> `CheckMark`。
            var tgl = MenuDraw.Node(box, "Toggle", boxR);
            MenuDraw.Rect(tgl, Tex("40K_dropdown_bg"), boxR, "Image", QCheckBox, null, true);
            // 勾：`40k_general_bt_yellow_confirm`（Simple + preserveAspect · 染色 `(0.575,0.209,0.209,1)`）
            //   原版显隐走 `Toggle.graphic` 的 **alpha**（那颗 `EverguildToggle` 的 `colorTintOnValueChange=0` /
            //   `changeSpriteOnValueChange=0` 排除了它自己的 tint/sprite 那条路）⇒ 我们同一做法：
            //   **未选中 = alpha 0**（⛔ 不是 `SetActive(false)` —— 那会让「点亮」变成重建）。
            _checkMarks.Clear();
            var mark = MenuDraw.Rect(tgl, Tex("40k_general_bt_yellow_confirm"), boxR,
                                     "CheckMark", QCheckMark, new Color(MarkCol.r, MarkCol.g, MarkCol.b, 0f), true);
            if (mark != null) _checkMarks.Add(mark);
            // 🆕 **2026-10-12（A336③）**：`…/selectButton/Checkbox/Label` 实读
            //    = `fs 32 · auto[29~32] · base **36.0**`（上限 = 标称）。
            _checkLabel = MenuDraw.TextBox(box, labelR, FeatureLabel, Color.white, "Label", 32f, 29f, QCheckText,
                                           32f, 36f);
            if (_checkLabel != null) MenuDraw.AlignLeft(_checkLabel, labelR);
            // 点整行切换（原版那颗 `EverguildToggle` 就在 `Checkbox` 上、命中区 = 整个 `Checkbox`）
            MenuDraw.Hit(box, "CheckHit", CheckR, QHit, () => HandleFeatureTrophy(!ToggleIsOn));

            Apply();
        }

        /// <summary>`border ÷ m_PixelsPerUnitMultiplier`（原版那颗 `Image` 的角块会按它缩放，见 `MenuDraw.Nine`）。</summary>
        static Vector4 Border90(Vector4 b) { return new Vector4(b.x / 0.9f, b.y / 0.9f, b.z / 0.9f, b.w / 0.9f); }

        // ============================================================ 取图（**取不到必须出声**）
        readonly List<string> _missArt = new List<string>();
        /// <summary>本窗**取不到的图**（自检读口 —— 非空就是「有件根本没画」，同 `MainMenuSubmenuWindow.MissingArt`
        /// 那条口径，那几个窗都有 `Check(win.MissingArt.Count, 0, …)` 的断言）。</summary>
        public List<string> MissingArt { get { return _missArt; } }

        /// <summary>取图 + **取不到就喊一声**。
        /// 🔴 **为什么不能直接写 `CardArt.MenuUi(x)`**：`MenuDraw.Rect` / `MenuDraw.Nine` 对 `tex == null`
        /// 是**静默返回 null**（`Shell/MenuDraw.cs` 的 `Rect`（`tex == null` 第一句） 第一句）⇒ 那一件**连节点都不会建**，而画面上只是「少了个东西」
        /// —— 正是本工程最恨的那类静默失败（同步点实跑就吃过一次：6 条断言报「节点不在」，而根因是
        /// **层级建错**、图其实好好的）。
        /// ⚠️ 原来是「一张**手写清单**在 `Build` 末尾统一查一遍」—— 那种写法迟早跟实现脱节（新加一件、忘了进清单
        /// 就永远查不到）⇒ 改成**谁取谁报**（清单由调用点自动长出来）。</summary>
        Texture2D Tex(string art)
        {
            var t = CardArt.MenuUi(art);
            if (t == null && !_missArt.Contains(art))
            {
                _missArt.Add(art);
                Debug.LogWarning($"[TrophyPopup] 图取不到：`{art}` ⇒ **这一件没画**"
                               + "（`MenuDraw.Rect/Nine` 对 `tex == null` 是静默返回 null）"
                               + " —— 导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            }
            return t;
        }

        // ============================================================ 铺数据

        /// <summary>把 `_view` 铺到已建好的件上（= 原版 `TrophyInfoPopup__Open.c` 那一段：
        /// `badgeDrawer.Draw(status)` → `trophyName.SetText(Data.LocalizedName)` →
        /// `trophyDescription.SetText(Data.LocalizedDescription)` →
        /// `progressBar.SetProgress(Save.CurrentValue, GetTargetMilestone(), "{0}/{1}", true)` →
        /// `progressHolder.SetActive(...)` → `toggle.SetIsOnWithoutNotify(IsFeatured)`）。
        /// <para>⚠️ 徽标那一颗（`badgeDrawer.Draw`）**铺不了** —— 它要一个服务器来的 `GroupBadge`（文件头 ①）。
        /// ⚠️ `toggle.interactable`（原版在「这一枚已经是默认奖杯」时把勾置灰）也判不了：那要
        /// `AlliancesTrophyController.DefaultTrophy` 的状态，同样在服务器。</para></summary>
        public void Apply()
        {
            // 🔴 **2026-10-04（修红）**：`SetText` 之后**必须重新对齐** —— `MenuDraw.AlignLeft` 是按**渲染宽度**
            //    把节点挪到「文字左边缘 = 框左沿」（`Label.AlignLeftOn`），字一换宽度就变
            //    （`Build` 里那一次对齐是给**出厂那一段文本**做的）。空串时 `AlignLeftOn` 自己**不挪**
            //    （守卫见 `Label.HasMeasuredWidth`）⇒ 零值态停在框心（什么都不画），有数据才对齐。
            if (_title != null) { _title.SetText(_view.Name ?? ""); MenuDraw.AlignLeft(_title, TitleR); }
            if (_desc != null)  { _desc.SetText(_view.Description ?? ""); MenuDraw.AlignLeft(_desc, DescR); }
            if (_counter != null) _counter.SetText(_view.Value + "/" + _view.Max);   // 原版格式串 `{0}/{1}`
            if (_progressHolder != null) _progressHolder.gameObject.SetActive(_view.ShowProgress);
            RebuildProgress();
            SetToggle(_view.Featured);
        }

        /// <summary>进度条那一叠（`Fill` + `end`）—— **按 `_view` 重建**。
        /// 原版由 `Slider` 在运行期改 `Fill` 的 anchors（出厂 `value = 0` ⇒ 宽 0），
        /// 而我们的九宫格是「整块一次建好、尺寸一变就得重排那九块」⇒ 只能在宽度变时重建。
        /// 🔴 零宽那一支**只占位、不画图**（`MenuDraw.Nine` 对退化矩形本来也不建 —— 同 `AllianceMemberTab` 那条）。</summary>
        void RebuildProgress()
        {
            if (_fillArea == null) return;
            float w = _view.Max > 0 ? FillAreaR.W * Mathf.Clamp01(_view.Value / (float)_view.Max) : 0f;
            var old = _fillArea.Find("Fill");
            if (old != null) MenuFrameDestroy(old.gameObject);
            _fill = null;
            Transform fillT;
            if (w > 0.01f)
            {
                var tex = Tex("40k_campaign_bar_fill");
                var go = tex != null
                    ? MenuDraw.Nine(_fillArea, tex, new PxRect(FillAreaR.x1, FillAreaR.y1, FillAreaR.x1 + w, FillAreaR.y2),
                                    FillBorder, tex.width, tex.height, QBarFill, FillCol, true, "Fill",
                                    borderOutPx: FillBorder90)
                    : null;
                if (go != null) { fillT = go.transform; _fill = go.GetComponentInChildren<ImageQuad>(); }
                else fillT = MenuDraw.Node(_fillArea, "Fill", new PxRect(FillAreaR.x1, FillAreaR.y1, FillAreaR.x1 + w, FillAreaR.y2));
            }
            else
            {
                fillT = MenuDraw.Node(_fillArea, "Fill",
                                      new PxRect(FillAreaR.x1, FillAreaR.y1, FillAreaR.x1, FillAreaR.y2));   // 出厂：零宽
            }
            // `end` 端帽跟着 `Fill` 的右端走（原版 `end` 是 `Fill` 的子 · pivot (1,0.5) · pos (5.7,−1)）
            var er = new PxRect(EndR.x1 + w, EndR.y1, EndR.x2 + w, EndR.y2);
            _endCap = MenuDraw.Rect(fillT, Tex("40k_campaign_bar_end"), er, "end", QBarEnd, EndCol, true);
        }

        /// <summary>批处理下 `Destroy` 不生效（没有帧循环）⇒ 走 `MainMenuSubmenuWindow.DestroySafe`
        /// 那一份（全工程唯一一份，别在这儿再写一遍 `isPlaying` 判断）。</summary>
        static void MenuFrameDestroy(GameObject go) { MainMenuSubmenuWindow.DestroySafe(go); }

        /// <summary>勾选态（原版 `toggle.SetIsOnWithoutNotify(IsFeatured)`）。
        /// 视觉 = 那颗勾的 **alpha**（1 / 0），⛔ 不是 `SetActive`（判据见 `Build` 里那一行）。</summary>
        void SetToggle(bool on)
        {
            ToggleIsOn = on;
            foreach (var q in _checkMarks)
                if (q != null) q.SetTint(new Color(MarkCol.r, MarkCol.g, MarkCol.b, on ? 1f : 0f));
        }

        /// <summary>自检用：直接切勾选态（等同点一下那一行）。产品路径上只有 `CheckHit` 那个回调会调它。</summary>
        public void SetToggleForTest(bool on) { SetToggle(on); }

        /// <summary>原版 `TrophyInfoPopup.HandleFeatureTrophy(bool)`：把这一枚（`true`）或**默认那一枚**
        /// （`false`）设成联盟展示奖杯 —— 它走 `PlayFabWrapper.GenericCloudScriptHandler(0x439, …)`，
        /// **是一次服务器写**。
        /// 🔴 我们没有服务器 ⇒ **不改状态、如实出声**，并把勾**退回原状**（⛔ 不让界面说谎：
        /// 「点了没反应」与「点了看着成了、其实没成」，后者更坏 —— 红线：不许静默失败）。</summary>
        public void HandleFeatureTrophy(bool value)
        {
            Debug.Log($"[TrophyPopup] 点了勾选行（`HandleFeatureTrophy({value})`）：原版发一条 "
                    + "`PlayFabWrapper.GenericCloudScriptHandler(0x439, { trophy, status, callback })` "
                    + "把展示奖杯写到服务器上（`TrophyInfoPopup__HandleFeatureTrophy.c`）—— "
                    + "**本地没有服务器** ⇒ 状态**没有改**（勾退回原状）。"
                    + "⚠️ 判据里还有一条我们判不了：`toggle.interactable` 取决于"
                    + "`AlliancesTrophyController.DefaultTrophy` 的状态（服务器）。");
            SetToggle(_view.Featured);      // 退回真实状态（没变）
        }

        /// <summary>自检用：把当前铺出来的东西摊开（同 `Tooltip.DebugDump` 那条先例）。</summary>
        public string DebugDump()
        {
            return "name=\"" + (_title != null ? _title.Text : "-") + "\" desc=\""
                 + (_desc != null ? _desc.Text : "-") + "\" counter="
                 + (_counter != null ? _counter.Text : "-")
                 + " progressActive=" + (_progressHolder != null && _progressHolder.gameObject.activeSelf)
                 + " fillActive=" + (_fill != null && _fill.gameObject.activeSelf)
                 + " fillW=" + (_fill != null ? (_fill.WorldW * 108f).ToString("F1") : "0.0") + "px"
                 + " endX=" + (_endCap != null ? LayoutSpace.PxX(_endCap.transform.position.x).ToString("F2") : "-")
                 + " on=" + ToggleIsOn;
        }
    }
}
