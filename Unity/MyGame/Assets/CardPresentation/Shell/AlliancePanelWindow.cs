// AlliancePanelWindow.cs — A985③：原版 `FrontCanvas/Alliance Panel`（组件类 `BattleAlliancePanel`）整扇窗
//
// ============================ 这是什么 ============================
// 对局里点 HUD 上那颗联盟钮（原版 `BattleHud.AlliancePanelOpenButton`）弹出来的**对手档案**窗：
// 头像框 + `Name:` / 名字 + `Title:` / 称号 + `Alliance:` / 联盟名 + 徽章 + 评级；
// **对手没有联盟时**改成一行「这个玩家还没有加入任何联盟」的提示。
//
// ============================ 判据（第一权威：解包资源逐字段亲读）============================
// 包 = `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/`（13 个战场各一份；**逐字段比对过
// arena1 vs arena2**：根框五元组、子件 rect、贴图 pid **逐位相同** ⇒ 13 场共用同一份 prefab）。
//
//   · 根  `GameObject/Alliance Panel.json`（GO 1130，`m_IsActive = 0`）+ `RectTransform_2859.json`：
//         `m_AnchorMin = m_AnchorMax = (0,1)` · `m_Pivot = (0.13, 0.825)` ·
//         `m_AnchoredPosition = (-1.044435, -25.271729)` · `m_SizeDelta = (815.044006, 475.469971)` ·
//         `m_LocalScale = 0.9999718070030212`
//         父链 = `Canvas(RT 2759) → FrontCanvas(2900) → Safe area FrontCanvas(2862)`（两条 stretch、零偏移）
//         ⇒ **绝对矩形 = x[-107.00, 708.02] y[-57.93, 417.52]**（**顶出屏 57.9 px —— 原版就长这样**）。
//         ⚠️ 父链三级的 `m_SizeDelta` 都是 `0`（stretch 到 1920×1080）⇒ 按 1920×1080 解；
//            `Canvas` 自己 `m_LocalScale = 0` 是**序列化残留**（ScreenSpaceOverlay 的 Canvas 由引擎驱动）
//            ⇒ 按 `1` 解，⛔ 别拿那个 0 去乘。根自己那个 `0.9999718` 同理按 `1` 解（差 0.0028%）。
//   · 控制器 `MonoBehaviour_4117.json`（`m_Script = BattleAlliancePanel`）的 12 个字段，按偏移顺序：
//     0x20 `avatarDisplay` · 0x28 `playerName` · 0x30 `playerTitle` · 0x38 `playerTitleHolder` ·
//     0x40 `playerScore` · 0x48 `allianceGO` · 0x50 `playerAllianceName` · 0x58 `allianceRatingDisplay` ·
//     0x60 `backgroundCloseButton` · 0x68 `canvasGroup` · 0x70 `allianceBadgeDrawer` ·
//     0x78 `notInAnAllianceMessage`（= GO 762 / RT 2660 / `MonoBehaviour_4475`）
//   · 方法体（`d:/2/tools/decomp_full/`，第一权威）：
//     `BattleAlliancePanel__Awake.c`  ⇒ **开局 `SetActive(false)`**，并给 `backgroundCloseButton` 挂
//                                       「点了就 `CloseButtonClick`」；`…__CloseButtonClick.c` = `SetActive(false)`。
//     `BattleAlliancePanel__Open.c`   ⇒ 显隐/赋值全在里面（逐句等价见 `Apply`）。
//     `BattleHud__AlliancePanelOpenButton.c` ⇒ HUD 取 `BattleManager.matchData` 的
//                                      `+0x10` 名 / `+0x50` 称号 / `+0x38` 评级 / `+0x40`,`+0x48` 头像 id /
//                                      `+0x68` 联盟名 / `+0x7c` 联盟评级 / `+0x80` 徽章 id 调 `Open(...)`。
//   · 节点树逐行核对表：`资料/战斗规格/战斗重建_0827/子代理读报_front弹层_0827.md` §五
//     （⚠️ 它给的是「**未乘 `localScale`** 的 rect」；本文件用**乘过的实绘矩形**，两者中心相同）。
//
// ============================ 🔴 三个「一个值 ≠ 全部情况」的坑 ============================
//  ① **`PlayerInfo` 身上有 `VerticalLayoutGroup`**（`MonoBehaviour_4213`：`m_Spacing = 0` ·
//     `m_ChildAlignment = 0`(UpperLeft) · `m_ChildControlWidth = 1` · **`m_ChildControlHeight = 0`**）⇒
//     `NameHolder` / `TitleHolder` / `Player Rating Display` 三个 holder 的序列化 rect **全是 0×0、
//     且都落在 `PlayerInfo` 的锚点上 = 「布局跑之前的模板位」** ⇒ 照抄会让三行叠成一坨。
//     ✅ 正确值按 `Core/UguiRect.cs` 的 `UguiLayout.VerticalChild`（那一条刚好就是本组合）重算 —— 见 `Build`。
//     ⚠️ 与 CLAUDE.md §三「权威坐标表里的 0×0 / 越界 rect 是模板位」是同一条，本件是**第二个实例**。
//  ② **文字那一族的行内 `m_LocalScale = 0.8`**（头像框 1.25 · 头像图 2.0）⇒ 字号与框都要乘它。
//     序列化 `m_fontSize` 是**没乘的 em**。
//  ③ **`Close Background` 的 rect 是 11668×8012**（远超屏幕、中心在面板右上）⇒ 那是原版「铺满全屏的
//     熄灯层」，⛔ 别当它是排版错误。
//
// ============================ 🔴 分层：只用渲染队列 ============================
//  全部件在**透明队列 3000**（`ImageQuad` 材质的默认档）⇒ 同队列下谁压谁由「到相机的 3D 距离」定，
//  而 `Close Background` 中心在 x≈+650 px（**离相机比面板本体近**）⇒ **靠 z 排会把熄灯层画到面板上面**。
//  ⇒ 逐层显式写队列（`QShade`…`QText`），并把「压暗层档 < 内容最低档」这条交给 `MenuDraw.ShadeHit` 自己校验。
//
// ============================ 🆕 相对原版的**已知缺口**（如实记，⛔ 不静默）============================
//  ① **头像立绘不画**：`Avatar Item Small/Image Container/Image` 的 `m_Sprite` 是 `0`（运行期由
//     `AvatarDisplay.Initialize` 灌）⇒ **原版出厂态本来就不画**（同本仓名牌那两处的口径）；
//     我们单机的 bot 也没有头像数据源 ⇒ 这一格保持原版出厂态（**只摆框**）。
//  ② **两处评级不建**：`Player Rating Display` / `Alliance Rating Display` 里的 `Main Icon` /
//     `Individual rating value` 序列化都是 **0×0**，尺寸由 `RatingDisplay` 脚本运行时算
//     （两个节点身上各挂一个 `HorizontalLayoutGroup`、`m_ChildControlWidth/Height = 1`
//     ⇒ 尺寸 =「TMP/LayoutElement 首选宽」× 容器高，**静态取不到**）；而且我们单机**没有对手评级数据**。
//     ⇒ 不建那两格（要补：先有一个真评级数据源 + 一次运行期读数）。
//  ③ **徽章 `Frame` / `Badge` 不建**：两张 `m_Sprite` 也是 `0`（`AllianceBadgeDrawer.Draw(id)` 运行期灌）
//     ⇒ 同样是「出厂态就没有」；且我们单机恒走「无联盟」那一支，那两格根本不显。
//  ④ **出场动画（`DOFade` + `DOScale`）没做**：原版 `Open` 末尾是 `CanvasGroup.alpha = 0` → `DOFade`
//     + 「先缩到 `×DAT_1834b2e84` 再 `DOScale` 弹回」。本件**直接显/隐**（本仓其它窗同此）；
//     ⚠️ 三个 DOTween 常量（目标 alpha / 时长 / 缩放系数）**没读**（要在 `GameAssembly.dll` 里取值）。
//  ⑤ ~~**HUD 上那颗钮我们还没有**（见文件末「入口」一节）~~ ⇒ ✅ **2026-10-18（A985③）已接**：
//     `Battle/BattleDriver.cs` 的 `BuildHudExtras` 里建了那颗钮，点击入口 = `HandleAlliancePanel`。
//     🔴 **那颗钮【没有贴图】** —— 原版它的 `m_TargetGraphic` 类是 `NonDrawingGraphic`
//     （`OnPopulateMesh` 方法体为空）、节点就是 `LeftArea/EnemyInfo` **本身** ⇒ 纯命中区。
//     判据全文（rect / padding / 13 场同构 / 点击沿）**只写在那一处**，本文件不重复。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `FrontCanvas/Alliance Panel`（`BattleAlliancePanel`）—— 对局内的**对手档案**窗。
    /// <para>⚠️ **它是「面板」不是「窗」**：根组件是普通 `MonoBehaviour`、**不是 `GameWindow`**
    /// ⇒ 没有 `type` / `placement` / `closeOnESC` 可填、也⛔ **不注册进 `WindowsManager`**
    /// （同 `Shell/AllianceEventScorePanel.cs` 的口径；别为了「凑一条注册」给它编一个窗身份）。</para></summary>
    public class AlliancePanelWindow : MonoBehaviour
    {
        // ============================================================ 分层（渲染队列，⛔ 不是 z）
        //  全部 ≥ 3980 ⇒ 都在战斗 HUD（默认档 3000）之上；`QShade` 是唯一低于内容的档。
        public const int QShade = 3980;     // 熄灯层 `Close Background`（**必须严格低于 `QContentMin`**）
        public const int QBg = 4000;        // `Background` / `BG`
        public const int QFrame = 4010;     // `BGFrame` 四块
        public const int QAvatar = 4020;    // 头像框 `Border`
        public const int QText = 4030;      // 全部文字
        /// <summary>传给 `MenuDraw.ShadeHit` / `MenuDraw.Absorb` 的「本窗内容最低档」——
        /// 那两个口靠它算「压暗/吸收命中区用哪一档」并**当场校验**（不合法会告警）。</summary>
        public const int QContentMin = QBg;

        // ============================================================ 根框（画布像素 · 左上原点 · 1920×1080）
        static readonly PxRect RootR = new PxRect(-107.00f, -57.93f, 708.02f, 417.52f);

        // ============================================================ 逐件几何（**全 = 实绘矩形**，已乘 localScale）
        //  命名 = 原版节点名；括号里的 pid = `RectTransform` 的 PathID（可逐条复查）。
        //  ⚠️ 这一组**不是从报告里抄的**，是按 `RectTransform_*.json` 的五元组 + 父链 + `m_LocalScale` 现算的
        //     （算式 = 文件头那三条；与 `子代理读报_front弹层_0827.md` §五 那张表**中心逐一吻合**，
        //      差别只在「它没乘 `localScale`」）。

        // 熄灯层 `Close Background`（RT 2698）：图 = 同族底图 + 纯黑 α0.4627；`m_RaycastTarget = 1`
        static readonly PxRect ShadeR = new PxRect(-5181.741f, -3867.769f, 6486.258f, 4144.042f);
        const float ShadeAlpha = 0.4627f;                       // 实读 `m_Color = (0,0,0,0.4627)`

        // 面板本体 `Background`（RT 3166）。⚠️ 它自己**没有** Image（只有 `BG` 那张有）—— 见 `Build` 吸收层那条
        static readonly PxRect BackgroundR = new PxRect(-97.327f, 9.970f, 625.312f, 413.089f);
        // 内容底 `BG`（RT 3225）：贴图 `40K_shop_offer_bg_Sororitas_0`、染色 (0.349,0.349,0.349,1)、Type=Simple
        static readonly PxRect BgR = new PxRect(14.158f, 55.246f, 571.457f, 356.583f);
        static readonly Color BgTint = new Color(0.349f, 0.349f, 0.349f, 1f);
        // `BGFrame`（RT 3339）与它四块（RT 2734/3407/3414/3122）；图 = `40k_UnitChat_Background_*`
        static readonly PxRect FrameR = new PxRect(-92.217f, 13.557f, 624.645f, 412.646f);
        static readonly PxRect FrameLeftR = new PxRect(-92.673f, 25.022f, 24.645f, 411.994f);
        static readonly PxRect FrameRightR = new PxRect(541.475f, 13.985f, 625.123f, 385.038f);
        static readonly PxRect FrameTopR = new PxRect(23.750f, 24.840f, 541.422f, 58.762f);
        static readonly PxRect FrameBottomR = new PxRect(24.487f, 348.664f, 542.159f, 385.195f);

        // 头像框 `Border`（RT 3040）：图 `Player Profile Border` 256×286、`m_PreserveAspect = 1` ⇒ 等比内接
        static readonly PxRect AvatarBorderR = new PxRect(-11.283f, 60.573f, 201.246f, 209.590f);

        // `Name:` / 名字（`NameHolder` = VLG 第 0 格，高 42.573 ⇒ y[62.498, 105.071]）
        static readonly PxRect NameRowR = new PxRect(180.000f, 62.498f, 556.959f, 105.071f);
        static readonly PxRect NameLabelR = new PxRect(180.000f, 63.436f, 273.941f, 103.636f);
        static readonly PxRect NameTextR = new PxRect(276.220f, 63.436f, 555.228f, 103.636f);
        // `Title:` / 称号（`TitleHolder` = VLG 第 1 格，高 38.442 ⇒ y[105.071, 143.513]）
        static readonly PxRect TitleRowR = new PxRect(180.000f, 105.071f, 556.959f, 143.513f);
        static readonly PxRect TitleLabelR = new PxRect(179.995f, 104.586f, 274.429f, 145.386f);
        static readonly PxRect TitleTextR = new PxRect(276.215f, 104.102f, 555.223f, 145.166f);
        // `Alliance` 组（RT 3495）与它那两行字（组里**没有**布局组 ⇒ 用它自己的 rect）
        static readonly PxRect AllianceGroupR = new PxRect(23.094f, 220.581f, 574.688f, 356.530f);
        static readonly PxRect AllianceLabelR = new PxRect(183.090f, 244.780f, 278.002f, 285.579f);
        static readonly PxRect AllianceNameR = new PxRect(278.940f, 244.780f, 535.827f, 285.579f);
        // `NotInaAllianceText`（RT 2660）—— 🔴 原版那句 `is is` 双 is 是笔误，照抄勿改
        static readonly PxRect NotInAllianceR = new PxRect(55.765f, 257.013f, 529.877f, 293.972f);
        const float NotInAllianceAlpha = 0.772549f;             // 实读 `m_fontColor.a`

        // 字号 = 原版 `m_fontSize` **× 该节点累计 localScale**（这一族全是 0.8）。
        //  ⚠️ 这几颗都 `m_enableAutoSizing = 1`，但序列化 `m_fontSize` **就是**上一次算出来的拟合值
        //     ⇒ 直接用它（`MenuDraw.Text` 走 `SetGlyphHeight`，不做二次 autosize —— 见 `Text` 的注释）。
        const float NameLabelPx = 28.00f;      // 35   × 0.8
        const float NameTextPx = 32.00f;       // 40   × 0.8
        const float TitleLabelPx = 28.00f;     // 35   × 0.8
        const float TitleTextPx = 28.00f;      // 35   × 0.8
        const float AllianceLabelPx = 26.72f;  // 33.4 × 0.8
        const float AllianceNamePx = 28.00f;   // 35   × 0.8
        const float NotInAlliancePx = 26.32f;  // 32.9 × 0.8

        // 原版那 4 颗 TMP 的**静态原文**（= prefab 出厂值，也是运行期 `GetTranslation` 的兜底）。
        //  🔴 运行期那 4 条真值在**远端 I2 表**（本仓没有，见 `资料/普查产出_1018/G2b_战斗本地化收口.md`）
        //     ⇒ 先查 `Loc` 表；`Loc` 里没这一条就用 prefab 原文（= 原版出厂就是那句英文）。
        const string NameLabelTerm = "Battle/AlliancePanel/PlayerLabel";
        const string TitleLabelTerm = "Battle/AlliancePanel/TitleLabel";
        const string AllianceLabelTerm = "Battle/AlliancePanel/AllianceLabel";
        const string NotInAllianceTerm = "Battle/AlliancePanel/NotInAnAlliance";
        const string NameLabelEn = "Name:";
        const string TitleLabelEn = "Title:";
        const string AllianceLabelEn = "Alliance:";
        const string NotInAllianceEn = "This player is is still not part of an Alliance";

        // ============================================================ 建出来的东西
        Transform _root, _background, _allianceGo, _titleHolder, _notInAlliance;
        Label _nameLb, _titleLb, _allianceLb;
        readonly List<string> _missArt = new List<string>();
        bool _visible;

        public bool Visible { get { return _visible; } }
        public Transform Root { get { return _root; } }
        /// <summary>取不到的美术名（`MenuDraw.Rect` 取不到图会**静默 return null** ⇒ 整层消失，所以自己记账）。</summary>
        public List<string> MissingArt { get { return _missArt; } }
        /// <summary>`NotInaAllianceText` 那个节点（自检：无联盟那一档该显、有联盟时该藏）。</summary>
        public Transform NotInAllianceNode { get { return _notInAlliance; } }
        /// <summary>`Alliance:` 那一组（原版字段 `allianceGO`）。</summary>
        public Transform AllianceGroupNode { get { return _allianceGo; } }
        /// <summary>`Title:` 那一组（原版字段 `playerTitleHolder`）。</summary>
        public Transform TitleHolderNode { get { return _titleHolder; } }
        /// <summary>名字那一段（原版字段 `playerName`）。</summary>
        public Label NameLabel { get { return _nameLb; } }

        // ============================================================ 建
        /// <summary>在 <paramref name="parent"/> 下建出整扇窗（**默认关着**，照原版 `Awake` 的
        /// `SetActive(false)`）。根框位置/尺寸 = 原版绝对矩形（含「顶出屏 57.9 px」那一条）。</summary>
        public static AlliancePanelWindow Create(Transform parent, string name = "Alliance Panel")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<AlliancePanelWindow>();
            c._root = go.transform;
            MenuDraw.ApplyPxRect(go.transform, parent, RootR);
            c.Build();
            c.Close();
            return c;
        }

        void Build()
        {
            _missArt.Clear();

            // ---- ① 熄灯层（`Close Background`）：铺满全屏的黑 + **点它 = 关窗** ----
            // 原版：那颗 `Image` 的 `m_RaycastTarget = 1`，而「关窗」是 `Awake` 给它的 `Button.m_OnClick`
            // 挂上 `CloseButtonClick`（`BattleAlliancePanel__Awake.c` 逐句）⇒ 语义 = 「点窗外关」。
            // 走公共件 `MenuDraw.ShadeHit`（它自己校验档 < `QContentMin`，不合法当场告警）。
            // ⚠️ 原版那张图挂了贴图，但染色是**纯黑 α0.4627** ⇒ 乘出来 = 「黑，透明度 = 贴图 α × 0.4627」，
            //    而那张底图的 α 满幅 ⇒ 用**实底**画等价（这是**我们这处的做法**，如实标）。
            var shade = MenuDraw.Rect(_root, CardArt.Solid(), ShadeR, "Close Background", QShade,
                                      new Color(0f, 0f, 0f, ShadeAlpha));
            if (shade != null)
                MenuDraw.ShadeHit(shade.transform, ShadeR, QShade, QContentMin, Close, "CloseHit");

            // ---- ② 面板本体：`Background`（空节点）+ `BG`（内容底）+ `BGFrame` 四块 ----
            _background = MenuDraw.Node(_root, "Background", BackgroundR);
            MenuDraw.Rect(_background, Art("40K_shop_offer_bg_Sororitas_0"), BgR, "BG", QBg, BgTint);
            var frame = MenuDraw.Node(_background, "BGFrame", FrameR);
            MenuDraw.Rect(frame, Art("40k_UnitChat_Background_Left"), FrameLeftR, "BGFrame Left", QFrame);
            MenuDraw.Rect(frame, Art("40k_UnitChat_Background_Right"), FrameRightR, "BGFrame Right", QFrame);
            MenuDraw.Rect(frame, Art("40k_UnitChat_Background_Top"), FrameTopR, "BGFrame Top", QFrame);
            MenuDraw.Rect(frame, Art("40k_UnitChat_Background_Bottom"), FrameBottomR, "BGFrame Bottom", QFrame);

            // ---- ③ 吸收层：点窗内空白处**什么都不该发生** ----
            // 原版：`BG` 那张图 `m_RaycastTarget = 1`，而它的父链上没有 `IPointerClickHandler`
            // （「关窗」那颗挂在**兄弟** `Close Background` 上）⇒ 射线打到面板自己 ⇒ 无事发生。
            // 我们这边命中候选只收 `WindowButton` ⇒ 不补这一层，鼠标会**穿过面板**落到熄灯层上，
            // 点窗内任意空白都会关窗。判据与三条红线 → `Shell/MenuDraw.cs` 的 `Absorb` 段。
            // ⚠️ **吸收的是 `Background` 整块**（不是原版 `BG` 那一张图的框）—— 这是**我们挑的**：
            //    原版 `BGFrame` 四块各自也是 Image（`m_RaycastTarget` 没逐块核）⇒ 取整块更保守。
            MenuDraw.Absorb(_root, "BackgroundAbsorb", BackgroundR, QShade, QContentMin);

            // ---- ④ 头像框（`Avatar Item Small` → `Image Container` → `Border`）----
            // `Avatar Item Small` 自己是个 `Button` 但 `m_interactable = 0` ⇒ 不可点、不加命中区。
            // ⚠️ `Image`（立绘）`m_Sprite = 0`（运行期灌）· `Highlight` `m_Enabled = 0` · `Raycast Target` `act=F`
            //    ⇒ **原版出厂态就都不画** ⇒ 本件只摆框（缺口①）。
            MenuDraw.Rect(_root, Art("Player_Profile_Border"), AvatarBorderR, "Border", QAvatar,
                          keepAspect: true);

            // ---- ⑤ `EnemyInfo`：`Name:` / 名字 · `Title:` / 称号 ----
            //  🔴 那三个 holder 在 prefab 里是 **0×0 且都落在 `PlayerInfo` 的锚点上** = `VerticalLayoutGroup`
            //     **跑之前的模板位**（`MonoBehaviour_4213`：spacing 0 · UpperLeft · `childControlWidth = 1` ·
            //      **`childControlHeight = 0`**）⇒ 按 `UguiLayout.VerticalChild` 重算：
            //       `PlayerInfo` 实绘框 = x[180.000, 556.959] y[62.498, 213.723]（RT 3363）
            //       第 0 格 `NameHolder`  高 42.573 ⇒ y[62.498, 105.071]
            //       第 1 格 `TitleHolder` 高 38.442 ⇒ y[105.071, 143.513]
            //       第 2 格 `Player Rating Display` 高 60.539 ⇒ y[143.513, 204.052]（**不建**，缺口②）
            //     ⛔ 别照抄报告里那三个重叠的 y（那是模板位）。
            var nameRow = MenuDraw.Node(_root, "NameHolder", NameRowR);
            Txt(nameRow, NameLabelR, LocOr(NameLabelTerm, NameLabelEn), Color.white, "PlayerLabel",
                NameLabelPx, Label.VAlign.Capline);
            _nameLb = Txt(nameRow, NameTextR, "", Color.white, "Name Text", NameTextPx, Label.VAlign.Capline);

            _titleHolder = MenuDraw.Node(_root, "TitleHolder", TitleRowR);
            Txt(_titleHolder, TitleLabelR, LocOr(TitleLabelTerm, TitleLabelEn), Color.white, "TitleLabel",
                TitleLabelPx, Label.VAlign.Capline);
            _titleLb = Txt(_titleHolder, TitleTextR, "", Color.white, "Title Text", TitleTextPx,
                           Label.VAlign.Midline);

            // ---- ⑥ `Alliance` 组：`Alliance:` / 联盟名 ----
            //  `allianceGO`（原版字段 0x48）—— **无联盟时整组关掉**（`BattleAlliancePanel__Open.c`）。
            //  ⚠️ 组里的 `Alliance Badge Drawer/{Frame,Badge}` 两张图 `m_Sprite` 也是 `0`（`AllianceBadgeDrawer.Draw`
            //     运行期灌）⇒ 出厂态不画；`Alliance Rating Display` 同缺口② ⇒ 本件只建两行字。
            _allianceGo = MenuDraw.Node(_root, "Alliance", AllianceGroupR);
            Txt(_allianceGo, AllianceLabelR, LocOr(AllianceLabelTerm, AllianceLabelEn), Color.white,
                "Alliance Label", AllianceLabelPx, Label.VAlign.Capline);
            _allianceLb = Txt(_allianceGo, AllianceNameR, "", Color.white, "Alliance Name",
                              AllianceNamePx, Label.VAlign.Capline);

            // ---- ⑦ `NotInaAllianceText`：无联盟那一行提示（**原文含 `is is` 双 is = 原版笔误，勿改**）----
            var msg = Txt(_root, NotInAllianceR, LocOr(NotInAllianceTerm, NotInAllianceEn),
                          new Color(1f, 1f, 1f, NotInAllianceAlpha), "NotInaAllianceText",
                          NotInAlliancePx, Label.VAlign.Midline);
            _notInAlliance = msg != null ? msg.transform : null;

            Apply("", "", "");      // 出厂态 = 原版 `Awake` 那一支（`Create` 末尾再关一次）
        }

        // ============================================================ 状态 → 参数（原版 `Open` 的逐句等价物）
        /// <summary>原版 `BattleAlliancePanel.Open(name, title, rank, avatarItem, avatarItem2, allianceName, …)`
        /// 的**等价物**（`BattleAlliancePanel__Open.c` 逐句读过；头像/评级/徽章那三条见文件头缺口①②）。
        /// <para>**显隐三支**（原版就是这三条 `SetActive`）：</para>
        /// · `playerTitleHolder` —— **称号为空 ⇒ 关**；<br/>
        /// · `allianceGO` —— **联盟名为空 ⇒ 关**；<br/>
        /// · `notInAnAllianceMessage`（那个 TMP 的 `gameObject`）—— **联盟名为空 ⇒ 开**（并把文案设成
        ///   `GetTranslation("Battle/AlliancePanel/NotInAnAlliance")`），否则关。<br/>
        /// ⚠️ 原版判「称号空不空」用的是**称号**那一个实参（`param_3`）、判「有没有联盟」用的是**联盟名**
        /// （`param_7`）—— 本方法逐条照抄，⛔ 别把两者合并成一个「有没有档案」。</summary>
        public void Open(string playerName, string title, string allianceName)
        {
            gameObject.SetActive(true);
            Apply(playerName, title, allianceName);
            _visible = true;
        }

        /// <summary>原版 `BattleAlliancePanel.CloseButtonClick`（= `Close Background` 那颗钮的 `onClick`）：
        /// **只 `SetActive(false)`** —— 不清数据、不动别的窗。</summary>
        public void Close()
        {
            gameObject.SetActive(false);
            _visible = false;
        }

        /// <summary>世界坐标落在**面板本体**上吗（**不含**那张铺满全屏的熄灯层）。
        /// <para>判据 = `Build` ③ 那层**吸收区**用的**同一个矩形** `BackgroundR`（⛔ 别在这儿另写一份；
        /// 原版那边是 `BG` / `BGFrame` 几张 Image 的 `m_RaycastTarget = 1` 把它们上面的射线吃掉、
        /// 传不到底下那颗 `Close Background`；而**吸收范围取整块 `Background` 是我们挑的**，
        /// 见 `Build` ③ 那条注释）。</para>
        /// <para>用途：战斗侧 `BattleDriver.HandleAlliancePanel` 靠它分「点窗内（什么都不做）」与
        /// 「点窗外（= 原版那颗 `Close Background` ⇒ 关窗）」。⚠️ 那扇窗在 `Build` 里挂的
        /// `MenuDraw.ShadeHit` 上是个 `WindowButton`，**只有外壳那套 `PointerLayer` 会派发它**
        /// —— 战斗里不走那条路 ⇒ 关窗那一手由调用方接。</para></summary>
        public bool HitBody(Vector3 world)
        {
            var p = LayoutSpace.ToPixel(world);
            return p.x >= BackgroundR.x1 && p.x <= BackgroundR.x2
                && p.y >= BackgroundR.y1 && p.y <= BackgroundR.y2;
        }

        void Apply(string playerName, string title, string allianceName)
        {
            if (_nameLb != null) _nameLb.SetText(playerName ?? "");
            bool hasTitle = !string.IsNullOrEmpty(title);
            if (_titleHolder != null) _titleHolder.gameObject.SetActive(hasTitle);
            if (_titleLb != null) _titleLb.SetText(title ?? "");

            bool hasAlliance = !string.IsNullOrEmpty(allianceName);
            if (_allianceGo != null) _allianceGo.gameObject.SetActive(hasAlliance);
            if (_allianceLb != null) _allianceLb.SetText(allianceName ?? "");
            if (_notInAlliance != null) _notInAlliance.gameObject.SetActive(!hasAlliance);

            // 🔴 换过字就要**重新左对齐一次**：`Label.SetText` 会重排，而 `RefreshBounds` 把字形块
            //    **居中**到锚点上（见 `Battle/CardDisplayWindow.cs` 的 `PlaceLeft` 那条同族教训）。
            //    ⚠️ 本句**只在窗口已激活时**才有效（TMP 在未激活时量不出宽 ⇒ `AlignLeftOn` 会自己早退）
            //    ⇒ `Open` 先 `SetActive(true)` 再调 `Apply`，顺序是死的。
            if (gameObject.activeInHierarchy)
            {
                MenuDraw.AlignLeft(_nameLb, NameTextR);
                MenuDraw.AlignLeft(_titleLb, TitleTextR);
                MenuDraw.AlignLeft(_allianceLb, AllianceNameR);
            }
        }

        // ============================================================ 助手
        /// <summary>一段**左对齐**的文字（原版这几颗全是 `m_HorizontalAlignment = 1`=Left）。
        /// 字号 = `px`（**已乘过 localScale 的实绘 em**）；垂直档 = 原版 `m_VerticalAlignment`。</summary>
        static Label Txt(Transform parent, PxRect r, string text, Color col, string name,
                         float px, Label.VAlign v)
        {
            var lb = MenuDraw.Text(parent, r, text, col, name, px, QText);
            if (lb == null) return null;
            lb.SetAlignLeft();                  // ① 逐行左对齐（真折行时才看得见）
            MenuDraw.SetVAlign(lb, v, r);       // ② 垂直档（⚠️ 必须排在 ① 后面：`Left` 自带 V=Middle）
            MenuDraw.AlignLeft(lb, r);          // ③ 整块左缘贴框左缘（⚠️ 只调 ① 不够，见 `Apply` 那条注释）
            return lb;
        }

        /// <summary>词条 → 文案。**远端 I2 表本地没有** ⇒ `Loc` 里没这一条时用 prefab 的静态原文
        /// （= 原版出厂就是那句英文）。⛔ **别把「查不到」写成猜测** —— 这里显式回落到原版原文。</summary>
        static string LocOr(string term, string prefabEn)
            => Loc.HasEntry(term) ? Loc.T(term) : prefabEn;

        /// <summary>取图 + 记账（`MenuDraw.Rect` 取不到图会**静默 return null** ⇒ 整层消失）。</summary>
        Texture2D Art(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            // 三级兜底 `ui/ → ui_menu/ → ui_deck/`（与 `Shell/AllianceEventScorePanel.cs` 同一条路）
            var t = CardArt.Ui(name) ?? CardArt.MenuUi(name) ?? CardArt.DeckUi(name);
            if (t == null && !_missArt.Contains(name))
            {
                _missArt.Add(name);
                Debug.LogWarning($"[AlliancePanel] 取不到美术 `{name}` ⇒ 那一层**不画**（⛔ 不静默："
                               + "缺的是哪张见本行；名单也记在 `MissingArt` 里）。");
            }
            return t;
        }
    }

    // ============================================================ 入口（✅ 2026-10-18 · A985③ 已接）
    //  原版：HUD 那颗 `BattleHud.alliancePanelOpenButton` 点了 → `BattleAlliancePanel.Open(...)`。
    //  🔴 **那颗钮就是敌方名牌本身**（`BackCanvas/…/LeftArea/EnemyInfo`，13 场同构）——
    //     判据（rect · `m_RaycastPadding` 外扩 · 「它没有贴图」· 抬起沿）**全部写在**
    //     `Battle/BattleDriver.cs` 的 `BuildHudExtras` 建它那一段 + `HandleAlliancePanel`。
    //  ✅ 我们这边：钮建在 `BattleDriver.BuildHudExtras`、窗建在同一处（`AlliancePanelWindow.Create(root)`）、
    //     点击链 = `BattleDriver.HandleAlliancePanel`（模态，接在 `Update` 的 `HandleBattleLog` 之后）。
    //     名字那一格 = `BattleDriver.AlliancePanelName`（联机取对端真名 / 单机取名牌上那一串；
    //     ⛔ 不编造 · 称号与联盟名恒空 = **原版单机形态**）。
    //  ⚠️ **战斗里不走 `PointerLayer`** ⇒ 本窗 `Build` 里那两个 `MenuDraw` 命中节点在战场里**不会被派发**，
    //     所以「点窗外关窗」由 `BattleDriver` 那一侧接（它用本类的 `HitBody` 分窗内 / 窗外）。
}
