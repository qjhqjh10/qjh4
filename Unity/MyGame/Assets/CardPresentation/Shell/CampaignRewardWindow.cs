// CampaignRewardWindow.cs — 阶段二第 3 层：战役奖励窗（原版 `Campaign Reward Window`）
//
// ============================ 出处（唯一正本） ============================
// `资料/阶段二_锻造厂与战役页_原版规格.md` §十四（本窗的落地规格）· §十三（节点 prefab）。
// 本文件里**每一个矩形/字号/颜色/sprite 名都是直读原版资产**，工具与命令：
//   `工具/menu_rect.py bundle_menus_assets_all "Campaign Reward Window" --depth 9 --cs`   ← 矩形 + 锚点五元组
//   `工具/menu_dump.py bundle_menus_assets_all "Campaign Reward Window" --depth 9`        ← sprite / 文本 / 字号 / 颜色 / 组件
//     （`menu_dump.py` 是本轮新写的：sprite 名要按 `m_Sprite.m_PathID` 去**真包**反查 —— 解包目录按名字存文件、**不带 pid**）
// 驱动链（反编译，`d:/2/tools/decomp_full/`）：
//   `CampaignRewardsWindow__Open.c` · `__SetBaseButton.c` · `__SetPremiumButton.c` ·
//   `__ConfigureIsPreviewState.c` · `__UnlockClicked.c` · `__CenterHolder.c` ·
//   `CampaignUnlockButton__{SetAsClaimed,SetAsFreeClaim,SetUnlockCost,ToggleTexts}.c`
//
// ---- 🔴 窗口参数（实证，出自它的 MonoBehaviour 原文 `MonoBehaviour_7664330643585539206.json`）----
//   `type = 1 (Popup)` · `windowsPlacement = 15 (Popup)` · `closeOnESC = 1` · `updateNavPanel = 0` ·
//   `extraScaleSmallScreen = 1.0` · 开/关音效 = null（`useDefaultCloseSoundIfNull = 1`）。
//
// ---- 🔴 本窗的「一个值 ≠ 全部情况」（铁律 5·c）—— 一共 **三套** 状态，任一都别当默认 ----
//   ① **列显隐**：`Rewards.Any(tier == 0)` → 基础列；`Any(tier == 10)` → 高级列。**两列都可能被关掉**。
//   ② **单列时居中**：只有一列有奖励时调 `CenterHolder(那一列)` —— 它把 holder **和它的父**一起改成
//      `anchorMin(0.5,0) / anchorMax(0.5,1) / pivot(0.5,0.5) / anchoredPosition.x = 0` 再强制重排
//      ⇒ **内容在视口里水平居中**（不是留在自己那一半）。
//   ③ **Preview / Get 两态**：`ConfigureIsPreviewState(bool)` 开关四个件（见下）。**出厂态 = Preview**。
//
// ---- Preview / Get 两态到底切哪四个件（`__Open.c` 与 `__ConfigureIsPreviewState.c` 逐字段对上）----
//   字段偏移（由 `CampaignRewardsWindow` 桩文件 + 反编译交叉对齐）：0xC8 `getRewardBackground` ·
//   0xD0 `getRewardText` · 0xD8 `previewRewardBackground` · 0xE0 `previewRewardText`。
//   **pid → 名字是实测的**（`Glow Get reward` 等的 GO pid 直读）：
//     `getRewardBackground`    = `Reward Background Get Reward`（红底 `40k_general_popup_simple red`，出厂 INACT）
//     `getRewardText`          = **`Glow Get reward`**（`40k_bt_underbutton` + 红 `(0.802,0.0794,0.0794)`，出厂 INACT）
//     `previewRewardBackground`= `Reward Background Preview Reward`（灰底 `… greyscale`，出厂 ACT）
//     `previewRewardText`      = **`Glow Preview reward`**（同图 + 灰 `(0.528,0.356,0.356)`，出厂 ACT）
//   ⚠️ `getRewardText` 名字像「文字」、实际是**那团底光**（`Text Get Reward` 是它的子件、跟着一起开关）。
//
// ---- 🔴 领奖按钮的三态（`CampaignUnlockButton`）----
//   `ToggleTexts(isCost)`（`__ToggleTexts.c`）：`claimedText`(0x20) 显 = **!isCost** ·
//   `costText`(0x28) 显 = **isCost** · `pointDrawer`(0x30) 显 = **isCost**。
//   `SetAsClaimed()`   → ToggleTexts(false) + `interactable = false` + claimedText = `claimedKey`
//   `SetAsFreeClaim(b)`→ ToggleTexts(false) + `interactable = b`        + claimedText = `claimKey`
//   `SetUnlockCost(cost, army, b)` → ToggleTexts(true) + `interactable = b` + pointDrawer.DrawForArmy(army)
//                                    + costText = cost.ToString()
//   术语原文（本窗 prefab 里读到的）：`claimedKey = "Rewards Menu/Claimed"` · `claimKey = "MainMenu/General/Claim"`。
//   ⚠️ **这两个词条的**真值**在 I2 语言表里、而 I2 表在远端 CCD（本地没有）** ⇒ 我们画的是
//   **term 末段的英文**（`Claimed` / `Claim`），并在文件里标明**这是推的**（不是原版字符串）。
//
// ---- 🔴 我们挑的（原版取不到，逐条出声）----
//   · **奖励物品的格子**：原版走 `ItemDrawer.Draw(holder, item, quantity)`（`CampaignRewardsWindow__Open.c:134`：
//     覆盖档 = **`Default(0)`**、`quantity` = 该条奖励的 `quantity`），它从一个 **`ItemDrawerConfig` SO** 里
//     按物品类型取**抽屉 prefab** 再 `Instantiate` + `Initialize`（`ItemDrawer__Draw.c` 实证）。
//     **那个 SO 与那批抽屉 prefab 本地全都没有**（全库搜 `ItemDrawer*` 资产 **0 命中**）。
//     ✅ **2026-10-03：抽屉收口到 `Shell/ItemDrawer.cs`（抽屉库，A12-P3 建）** —— 本文件里那一格现在**是真的抽屉**
//     （`ItemDrawer.Draw` 那四步），不再是本地手搓的一段。仍**是我们挑的**部分：抽屉**内部每层的版式**
//     （图标占框 0.7 / 数量钉底 / 阵营名条）与**格子尺寸** `ItemW×ItemH`（见那两条常量的注释）。
//     **物品图标**照 `CampaignData.ItemIcon`（数据层那张表）+ 抽屉库自己的野牌判据
//     （`WildcardDrawer` 那张 `40k_general_wildcard_*`）；**判据空的那 27 个 id 仍然画占位板 + 逐条打日志**。
//   · **`Tap To Continue`**（出厂 INACT、全库无脚本引用）⇒ **不建**（照本工程纪律①）。
//   · **`Scroll View` / `Viewport` 的滚动与裁剪**：两列内容在 1920 宽里放得下 ⇒ 不实现滚动；
//     `RectMask2D` 也没做（与锻造厂页同一个缺口）。**出声**在 `Dump()` 里。
//   · **`premiumUnlockWarning` / `premiumExtraTip` 的文案**同样是 I2 词条（远端，本地没有）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>开这一窗要带的东西。**照原版 `CampaignRewardsWindowContext` 的 8 个字段**
    /// （偏移与驱动见正本 §十四 那张表）。</summary>
    public class CampaignRewardsContext
    {
        public CampaignData.RewardSpec[] Rewards;   // 0x20 → 按 tier 分两列
        public bool BaseCollected;                  // 0x28 → 基础按钮 SetAsClaimed + 高级档「免费领」的入参
        public bool PremiumCollected;               // 0x29 → 高级按钮 SetAsClaimed + 藏 Warning
        public bool Claimable;                      // 0x2a → 按钮 interactable
        public bool IsPremiumLocked;                // 0x10 → 藏高级按钮、显 Warning
        public int PointCost;                       // 0x30 → 按钮上的点数 + pointDrawer
        public int Army;                            // 0x2c → 点数图标的阵营
        public System.Action<int> OnCollect;        // 0x18 → 原样透传给领取
    }

    /// <summary>点战役轨道上的节点之后开的那个窗。原版 `CampaignRewardsWindow : GameWindow`。</summary>
    public class CampaignRewardWindow : GameWindow
    {
        // ============================================================ 矩形（原版 JSON 原文，机械走链）

        /// <summary>`Menu Dark Background` —— 比屏幕大得多的压暗层（4574.6 × 2572.36）。
        /// 与 `DailyRewardPopup` 是**同一个矩形**（两窗都从 prefab 原文读出来，值一样）。</summary>
        public static readonly PxRect Shade = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.773f);   // m_Color 原文 (0,0,0,0.773)

        /// <summary>`Menu Vignette` —— 铺满整屏的暗角（`m_Color = (0,0,0,0.58)`，sprite 为空 ⇒ 纯色块）。</summary>
        public static readonly Color VignetteColor = new Color(0f, 0f, 0f, 0.58f);

        /// <summary>`Content`（`N(1, 0,.5, 1,.5, .5,.5, 0,−25, 0,800)`）= 0,165 → 1920,965。</summary>
        public static readonly PxRect Content = new PxRect(0f, 165f, 1920f, 965f);

        /// <summary>`Title`（`N(2, .5,1, .5,1, .5,1, 0,−28.3, 600,75)`）= 660,193.30 → 1260,268.30。</summary>
        public static readonly PxRect Title = new PxRect(660f, 193.30f, 1260f, 268.30f);

        /// <summary>`Scroll View`（`N(2, 0,0, 1,1, 0,.5, 0,−45, 0,−150)`）= 0,285 → 1920,935。</summary>
        public static readonly PxRect ScrollView = new PxRect(0f, 285f, 1920f, 935f);

        /// <summary>两列。`Base Rewards` 锚 `N(5, 0,0, .5,1, 1,.5, 0,0, 0,0)` ·
        /// `Premium Rewards` 锚 `N(5, .5,0, 1,1, 0,.5, 0,0, 0,0)`。</summary>
        public static readonly PxRect BaseCol = new PxRect(0f, 285f, 960f, 935f);
        public static readonly PxRect PremCol = new PxRect(960f, 285f, 1920f, 935f);

        /// <summary>两列 `Rewards` holder 的「锚点那一角」——基础列是**右边缘**（= 列右 − 65），
        /// 高级列是**左边缘**（= 列左 + 65）（`N(6, 1,0, 1,1, 1,.5, −65,7.5, 0,−35)` 与
        /// `N(6, 0,0, 0,1, 0,.5, 65,7.5, 0,−35)`）。宽**不是 JSON 值**：它带 `ContentSizeFitter`
        /// （`m_HorizontalFit = 1`）⇒ 宽 = 内容宽，见 `UguiLayout.HorizontalContentW`。</summary>
        public const float HolderGap = 65f;
        public const float HolderTop = 295f, HolderBottom = 910f;

        /// <summary>`Rewards` 那个 `HorizontalLayoutGroup` 的参数（原文 `MonoBehaviour_3702268942507023494.json`，
        /// 两列各一份、完全一致）：pad **L30 R30 T25 B85** · spacing **25** · alignment **4 (MiddleCenter)** ·
        /// `childControlWidth = 0` · `childControlHeight = 1`（= 用子件自己的首选高，见 `UguiLayout` 那条注释）。</summary>
        public const float PadL = 30f, PadR = 30f, PadT = 25f, PadB = 85f, Spacing = 25f;

        /// <summary>`Unlock Button` 自己的尺寸（`N(7, .5,0, .5,0, .5,0, 0,15, 245,45)`）——**245×45**。</summary>
        public const float UnlockW = 245f, UnlockH = 45f;
        /// <summary>`Unlock Button/Icon Campaign Points Drawer Variant` —— 它的框 = 按钮左半格减 77.5 ⇒ **45×45**；
        /// 该节点 `localScale = 1.5` ⇒ **实显 67.5²**（缩放烘进矩形，见 `BuildUnlockButton` 的注释）。
        /// 出厂 INACT，只有「付点解锁」态由 `ToggleTexts(true)` 打开。</summary>
        public const float PtIconBox = 45f, PtIconScale = 1.5f;
        /// <summary>`Warning`（出厂 INACT，只有 `IsPremiumLocked` 时由 `SetPremiumButton` 打开）。
        /// ⚠️ 它的宽带 `anchorMin.x 0.1 / anchorMax.x 0.9` ⇒ 是**父宽的 0.8 倍 + 30**，
        /// 而父宽又由 `ContentSizeFitter` 决定（循环）⇒ 这里按「父宽 = 本列内容宽」实算。</summary>
        public const float WarnH = 45f;
        /// <summary>`Badge`（`N(7, 0,1, 0,1, 0,1, 0,0, 100,100)`，`40k_campaign_Premium-icon`）。</summary>
        public const float BadgeSize = 100f;

        /// <summary>🔴 **我们挑的**：一个奖励物品格的尺寸。原版这里是 `ItemDrawer` 从
        /// `ItemDrawerConfig` 里取的抽屉 prefab（**本地没有**）⇒ 尺寸无从查证，这个值是我们定的，
        /// 依据只有一条：**两列各最多 2 个物品**（89 条奖励里基础档最多 2、高级档最多 1），
        /// 取 200 才让「2 物品 + 按钮 + 徽标」在 960 宽的列里放得下。</summary>
        public const float ItemW = 200f, ItemH = 300f;
        /// <summary>物品格里**主图**的边长（**我们挑的**，同上）。它就是抽屉库里那个 `IconFill` 的出处：
        /// `IconFill = ItemIconPx ÷ min(ItemW, ItemH)` = **0.7**（`BuildItem` 里现算，**别在抽屉库里再写死一个 0.7**）。</summary>
        public const float ItemIconPx = 140f;

        // ============================================================ 渲染队列
        // 🔴 **必须高于左栏那些页**：`RewardsWindow`/`ForgeTab`/`CampaignTab` 用到 3005…3064，
        //    而本窗是**弹窗**、画在它们上面。上一版别处的弹窗（`PromptPopup` 3018…3023）就低于
        //    3030+ 的页底板 —— 那种「弹窗被页盖住」**矩形断言量不到**（见 `资料/已知的坑.md`）。
        public const int QShade = 3110, QContentBg = 3111, QScroll = 3112, QViewport = 3113,
                         QColumnBg = 3114, QItem = 3115, QItemIcon = 3116, QBadge = 3117,
                         QUnlockBg = 3118, QUnlockPt = 3119, QUnlockText = 3120,
                         QTitleBg = 3121, QTitleText = 3122, QVignette = 3123;

        // ============================================================ 图（全部直读原版）
        // 🔴 **下列名字一律是「导入后的文件名」（空格 → 下划线）**，不是原版切片的原名 ——
        //    `CardArt.MenuUi` **不做空格转换**（`DailyRewardPopup` 那条注释踩过一次）。
        //    原名 ↔ 文件名的对照见 `工具/import_original_art.py` 的 `MENU_IMAGES`。
        public const string ArtBgGet = "40k_general_popup_simple_red";        // 原名 `40k_general_popup_simple red`
        public const string ArtBgPreview = "40k_general_popup_simple_greyscale"; // 原名 `… greyscale`
        public const string ArtColumnBg = "UI_Deck_Information_submenu_Back"; // 两列 `Rewards` 的底
        public const string ArtUnlockBtn = "UI_Button_Mulligan";              // `Unlock Button`（Simple）
        public const string ArtGlow = "40k_bt_underbutton";                   // 标题那团底光（Sliced + preserveAspect）
        public const string ArtBadge = "40k_campaign_Premium-icon";
        public const string ArtPointIcon = "40K_genearl_icon_Campaign_points"; // 按钮里的战役点图标（⚠️ 原版拼写就是 genearl）

        /// <summary>标题两态的底光颜色（`m_Color` 原文）。
        /// ⚠️ 出厂**没有 Tint 的 `Text *`**：`Text Get Reward` / `Text Preview Reward` 都是纯白。</summary>
        public static readonly Color GlowGet = new Color(0.802f, 0.0794f, 0.0794f, 1f);
        public static readonly Color GlowPreview = new Color(0.528f, 0.356f, 0.356f, 1f);

        /// <summary>标题两态的文案 —— **prefab 里的 TMP 原文**（`ConfigureIsPreviewState` 只开关、
        /// **不改文字** ⇒ 这两个串就是屏幕上印的）。</summary>
        public const string TxtGet = "Campaign Rewards", TxtPreview = "Available rewards";
        public const float TitleFont = 50f, TitleAutoMin = 25f;

        /// <summary>按钮文案。⚠️ **原版是 I2 词条**（`Rewards Menu/Claimed` · `MainMenu/General/Claim`），
        /// 词条表在**远端 CCD、本地没有** ⇒ 下表是**我们按 term 末段填的**（不是原版字符串）。</summary>
        public const string TxtClaimed = "Claimed", TxtClaim = "Claim";
        /// <summary>⚠️ 同上：结论是 `Rewards Menu/PremiumUnlockWarning` / `Rewards Menu/PremiumExtraTip`，
        /// **真值本地没有** ⇒ 这两句是**我们填的占位**（而且我们的口径下高级轨恒解锁 ⇒ 一般不显示）。</summary>
        public const string TxtPremWarning = "Premium campaign required",
                                  TxtPremTip = "Unlock the Premium track to claim";
        public const float ClaimedFont = 35f, ClaimedAutoMin = 10f, CostFont = 32.3f, CostAutoMin = 10f;
        public const float WarnFont = 47.5f, WarnAutoMin = 18f;

        // ============================================================ 运行时状态

        public readonly List<string> MissingArt = new List<string>();
        /// <summary>本地查不到图标的物品 id（**逐条出声**，自检也钉它）。</summary>
        public readonly List<string> NoIconItems = new List<string>();
        /// <summary>🆕 抽屉里那张**判据图**本地取不到、退了 `_small` 档的物品 id（**出声** —— 退化不是静默）。
        /// 判据：原版野牌用的是 `40k_general_wildcard_<rarity>`（328×497 的平铺卡面），
        /// 我们 `Resources/` 下只有 `_small` 档（斜置小卡），见 `ItemDrawer.WildcardTex`。</summary>
        public readonly List<string> FallbackArtItems = new List<string>();

        CampaignRewardsContext _ctx;
        Transform _root, _baseHolder, _premHolder, _baseBtn, _premBtn, _warn, _badge;
        PxRect _baseHolderR, _premHolderR, _baseColR, _premColR;
        Label _baseClaimed, _premClaimed, _baseCost, _premCost;
        bool _isPreview;

        /// <summary>两列 holder 的实算矩形（自检直接量它）。</summary>
        public PxRect BaseHolderRect { get { return _baseHolderR; } }
        public PxRect PremHolderRect { get { return _premHolderR; } }
        public bool IsPreview { get { return _isPreview; } }

        public static CampaignRewardWindow Create(WindowsManager mgr)
        {
            var go = new GameObject("Campaign Reward Window");
            var win = go.AddComponent<CampaignRewardWindow>();
            win.type = WindowType.Popup;                    // 实证 type=1
            win.placement = WindowsPlacement.Popup;         // 实证 windowsPlacement=15
            win.closeOnEsc = true;                          // 实证 closeOnESC=1
            win.extraScaleSmallScreen = 1f;                 // 实证 1.0
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        protected override void SetupData(object data) { base.SetupData(data); _ctx = data as CampaignRewardsContext; }

        public override void Open() { Build(); }

        // ============================================================ 建

        public void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);
            MissingArt.Clear(); NoIconItems.Clear(); FallbackArtItems.Clear();
            _root = root;
            var ctx = _ctx ?? new CampaignRewardsContext { Rewards = new CampaignData.RewardSpec[0] };

            var full = new PxRect(0f, 0f, 1920f, 1080f);

            // ① `Menu Dark Background`（sprite 为空 ⇒ 纯色块）
            MenuDraw.Rect(root, CardArt.Solid(), Shade, "Menu Dark Background", QShade, ShadeColor);

            // ③ `Content`
            var content = MenuDraw.Node(root, "Content", Content);
            // `Reward Background Get Reward`（红）/ `Reward Background Preview Reward`（灰）—— 出厂 active = 后者
            MenuDraw.Rect(content, Art(ArtBgGet), Content, "Reward Background Get Reward", QContentBg)
                ?.gameObject.SetActive(false);
            MenuDraw.Rect(content, Art(ArtBgPreview), Content, "Reward Background Preview Reward", QContentBg);

            // ④ `Scroll View` → `Viewport` → `Content`（两列的父）
            var sv = MenuDraw.Node(content, "Scroll View", ScrollView);
            // ⚠️ `Scroll View` 自己那个 `Image` 是 `Background` 图但 `m_Color` 的 **alpha = 0** ⇒ 画了也看不见，
            //    照「不画不可见的件」的纪律**不建**（`Viewport` 的 `UIMask` 也是 alpha 0）。
            var vp = MenuDraw.Node(sv, "Viewport", ScrollView);
            var inner = MenuDraw.Node(vp, "Content", ScrollView);

            // ⑤ 两列（先建空的 holder，内容在 `LayoutColumns()` 里按「哪一列真的有奖励」定）
            _baseColR = BaseCol; _premColR = PremCol;
            var bcol = MenuDraw.Node(inner, "Base Rewards", _baseColR);
            var pcol = MenuDraw.Node(inner, "Premium Rewards", _premColR);
            _baseHolder = MenuDraw.Node(bcol, "Rewards", new PxRect(0f, 0f, 0f, 0f));
            _premHolder = MenuDraw.Node(pcol, "Rewards", new PxRect(0f, 0f, 0f, 0f));

            // ⑥ `Title`（两态各一套底光 + 文字，出厂 = Preview 那一套）
            var title = MenuDraw.Node(content, "Title", Title);
            var glowGetR = UguiRect.Child(Title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                          new Vector2(0f, 5.92f), new Vector2(600f, 75f));
            var gg = MenuDraw.Rect(title, Art(ArtGlow), glowGetR, "Glow Get reward", QTitleBg, GlowGet, true);
            // 🔴 `Text *` 是 `Glow *` 的**子件**（原版深度 3 → 4）—— 挂错父会让「开关底光」管不到文字，
            //    而且 `FindPath(gPre, "Text Preview Reward")` 也找不到（**实测过一次**）。
            var gt = gg != null ? MenuDraw.Text(gg.transform, glowGetR, TxtGet, Color.white, "Text Get Reward",
                                   TitleFont, QTitleText, glowGetR.W, TitleAutoMin) : null;
            if (gt != null) MenuDraw.AlignRight(gt, glowGetR);
            var gp = MenuDraw.Rect(title, Art(ArtGlow), glowGetR, "Glow Preview reward", QTitleBg, GlowPreview, true);
            var pt = gp != null ? MenuDraw.Text(gp.transform, glowGetR, TxtPreview, Color.white, "Text Preview Reward",
                                   TitleFont, QTitleText, glowGetR.W, TitleAutoMin) : null;
            if (pt != null) MenuDraw.AlignRight(pt, glowGetR);
            if (gg != null) gg.gameObject.SetActive(false);

            // ⑦ `Menu Vignette` 最后画（原版它是 `Content` 的**下一个兄弟** ⇒ 在最上面）
            MenuDraw.Rect(root, CardArt.Solid(), full, "Menu Vignette", QVignette, VignetteColor);

            // ⑧ 按 `Open()` 的逻辑铺两列
            LayoutColumns(ctx);

            if (MissingArt.Count > 0)
                Debug.LogWarning("[CampaignReward] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray())
                                 + " —— 导入器：`工具/import_original_art.py`");
            if (NoIconItems.Count > 0)
                Debug.LogWarning("[CampaignReward] ⚠️ 有 " + NoIconItems.Count + " 个奖励物品**本地没有图标**"
                                 + "（抽屉库里落成占位板 + 短名，**逐条出声**）：" + string.Join("、", NoIconItems.ToArray())
                                 + " —— 原版走 `ItemDrawer`，那个 `ItemDrawerConfig` SO 与那批抽屉 prefab 本地都没有"
                                 + "（铁律 11 第①种：原版本身取不到 ⇒ 占位板 + 出声；见 `Shell/ItemDrawer.cs` 文件头）");
            if (FallbackArtItems.Count > 0)
                Debug.Log("[CampaignReward] 有 " + FallbackArtItems.Count + " 个野牌物品用的是**退档图**"
                          + "（原版那张 `40k_general_wildcard_<rarity>` 不在 `Resources/` 下 ⇒ 退了 `_small`）："
                          + string.Join("、", FallbackArtItems.ToArray()) + " —— 见 `ItemDrawer.WildcardTex`");
            // ⚠️ 只画一次、不静默：视口裁剪与滚动**没实现**
            Debug.Log("[CampaignReward] `Scroll View` 的滚动与 `RectMask2D` 裁剪**本轮没做**"
                      + "（两列内容在 1920 宽里放得下 ⇒ 不影响版面；同锻造厂页那个缺口）");
        }

        // ============================================================ 两列（`Open()` 的判定顺序）

        /// <summary>**照原版 `CampaignRewardsWindow__Open.c` 的判定顺序**（一个都不能改）：
        /// ① 两列**各自**按「这一档有没有奖励」开关；② 只有一列时 `CenterHolder`；
        /// ③ 算 Preview 态；④ 两个按钮各配一次；⑤ 画物品（插在列首）+ 强制重排。</summary>
        void LayoutColumns(CampaignRewardsContext ctx)
        {
            var all = ctx.Rewards ?? new CampaignData.RewardSpec[0];
            var baseItems = Filter(all, CampaignData.TierBasic);
            var premItems = Filter(all, CampaignData.TierPremium);
            bool hasBase = baseItems.Length > 0, hasPrem = premItems.Length > 0;

            // ① 列显隐
            _baseColR = BaseCol; _premColR = PremCol;
            _baseHolder.parent.gameObject.SetActive(hasBase);
            _premHolder.parent.gameObject.SetActive(hasPrem);

            // ② 只有一列时居中（原版 `CenterHolder` 把 holder **和它的父**一起居中）
            bool centered = false;
            float cx = Content.CX;
            if (hasBase && hasPrem) { /* 两列都在 ⇒ 不动 */ }
            else if (hasBase || hasPrem) centered = true;

            // ③ Preview 态：**`__Open.c` 的 bVar9** —— 还有东西没领 ⇒ Preview
            bool preview;
            if (hasPrem && !hasBase) preview = true;                       // 只有高级列 ⇒ 恒 Preview
            else if (hasBase && !hasPrem) preview = !ctx.BaseCollected;    // 只有基础列
            else if (hasBase && hasPrem) preview = !ctx.BaseCollected ? true : !ctx.PremiumCollected;
            else preview = true;                                            // 两列都空
            _isPreview = preview;
            ConfigureIsPreviewState(preview);

            // ④ 两列的内容
            BuildColumn(_baseHolder, BaseCol, baseItems, hasBase, centered, cx, true, ctx);
            BuildColumn(_premHolder, PremCol, premItems, hasPrem, centered, cx, false, ctx);
        }

        static CampaignData.RewardSpec[] Filter(CampaignData.RewardSpec[] all, int tier)
        {
            int n = 0;
            for (int i = 0; i < all.Length; i++) if (all[i].Tier == tier) n++;
            var o = new CampaignData.RewardSpec[n];
            n = 0;
            for (int i = 0; i < all.Length; i++) if (all[i].Tier == tier) o[n++] = all[i];
            return o;
        }

        /// <summary>一列。子件次序照原版子节点序：**物品（运行时插列首）→ `Unlock Button` → [`Warning` → `Badge`，高级列才有]**。</summary>
        void BuildColumn(Transform holder, PxRect col, CampaignData.RewardSpec[] items, bool show,
                         bool centered, float centerX, bool isBase, CampaignRewardsContext ctx)
        {
            for (int i = holder.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(holder.GetChild(i).gameObject);
            if (!show) return;

            // 子件宽度表 → holder 的内容宽（`ContentSizeFitter` 的 `m_HorizontalFit = 1`）
            // 🔴 **`Warning` 只在真显示时才参与布局** —— UGUI 的布局组**跳过 `activeSelf == false` 的子件**
            //    （`Warning` 出厂 INACT、只有 `IsPremiumLocked` 时由 `SetPremiumButton` 打开）。
            //    把它算进去会让高级列的 holder 从 655 宽涨到 980 ⇒ **右边缘 2005 跑出 1920**（静默出屏）。
            var widths = new List<float>();
            for (int i = 0; i < items.Length; i++) widths.Add(ItemW);
            widths.Add(UnlockW);
            if (!isBase)
            {
                if (PremiumLocked) widths.Add(WarnW);
                widths.Add(BadgeSize);
            }
            float holderW = UguiLayout.HorizontalContentW(widths.ToArray(), PadL, PadR, Spacing);

            // holder 自己的矩形：基础列**右边贴**列右 − 65；高级列**左边贴**列左 + 65
            // （两列的 `anchoredPosition.x` 是 ∓65、pivot 也各贴一边 —— 出处见 `HolderGap` 的注释）
            PxRect hr;
            if (centered) hr = new PxRect(centerX - holderW * 0.5f, HolderTop, centerX + holderW * 0.5f, HolderBottom);
            else if (isBase) hr = new PxRect(col.x2 - HolderGap - holderW, HolderTop, col.x2 - HolderGap, HolderBottom);
            else hr = new PxRect(col.x1 + HolderGap, HolderTop, col.x1 + HolderGap + holderW, HolderBottom);
            SetRect(holder, hr);
            if (isBase) _baseHolderR = hr; else _premHolderR = hr;

            // 列底（原版两列 holder 自己带 `Image` = `UI_Deck_Information_submenu_Back`，
            // 组件原文 `m_Type = 1 (**Sliced**)` + `m_PixelsPerUnitMultiplier = 1.0`）
            // 🔴 **必须走九宫格**：这张图只有 **69×63**、四边 `m_Border` 各 **18px** —— 拉成 530×615 的
            //    单块会把那个「八角框」整个抻变形（2026-09-23 实拍发现：画出来是个被拉长的八边形）。
            MenuDraw.Nine(holder, Art(ArtColumnBg), hr, new Vector4(18f, 18f, 18f, 18f), 69f, 63f, QColumnBg);

            int k = 0;
            // 物品。🔴 **原版是「画一条就 `SetAsFirstSibling()` 一次」**（`CampaignRewardsWindow__Open.c:135-145`；
            // 正本 `:598`）⇒ 效果是**列表里最后一条被挤到最左**，而 `Unlock Button`（出厂就在 prefab 里、
            // 位置本来就排在后面）留在末位。这里按**反序**建 —— 与那串 `SetAsFirstSibling` 之后的子节点序**等价**，
            // 槽号仍从左往右递增。
            // ⚠️ **2026-10-03 订正**：原来这里写「先画物品、后画按钮，得到**同样的次序**」——
            //    对**一条**奖励成立、**两条以上不成立**（那两句注释是照「物品挤到列首」这一点推的，
            //    没去读 `SetAsFirstSibling` 的语义）。依据 = `CampaignRewardsWindow__Open.c:138-145` 逐行解。
            for (int i = items.Length - 1; i >= 0; i--, k++)
            {
                var r = UguiLayout.HorizontalChildOwnHeight(hr, ItemW, ItemH, k, PadL, Spacing);
                r = VertCenter(hr, r, ItemH);
                BuildItem(holder, r, items[i]);
            }
            // `Unlock Button`
            var br = UguiLayout.HorizontalChildOwnHeight(hr, UnlockW, UnlockH, k++, PadL, Spacing);
            br = VertCenter(hr, br, UnlockH);
            var btn = MenuDraw.Node(holder, "Unlock Button", br);
            // A17：原版 `Campaign Reward Window>…>Unlock Button` 是 SpriteSwap（普查 §块 2 第 8 行；高级列同 prefab 二次实例）
            var bgQ = MenuDraw.Rect(btn, Art(ArtUnlockBtn), br, "bg", QUnlockBg);
            var btn2 = BuildUnlockButton(btn, br, isBase, ctx, bgQ);
            if (isBase) { _baseBtn = btn; _baseClaimed = btn2.claimed; _baseCost = btn2.cost; }
            else { _premBtn = btn; _premClaimed = btn2.claimed; _premCost = btn2.cost; }

            if (!isBase)
            {
                // `Warning`（出厂 INACT，只有 `IsPremiumLocked` 时由 `SetPremiumButton` 打开）
                var wr = UguiLayout.HorizontalChildOwnHeight(hr, WarnW, WarnH, k++, PadL, Spacing);
                wr = VertCenter(hr, wr, WarnH);
                _warn = MenuDraw.Node(holder, "Warning", wr);
                var wl = MenuDraw.Text(_warn, wr, TxtPremWarning, Color.white, "WarningText",
                                       WarnFont, QUnlockText, wr.W, WarnAutoMin);
                if (wl != null) MenuDraw.AlignRight(wl, wr);
                _warn.gameObject.SetActive(false);
                // `Badge`
                var gr = UguiLayout.HorizontalChildOwnHeight(hr, BadgeSize, BadgeSize, k++, PadL, Spacing);
                gr = VertCenter(hr, gr, BadgeSize);
                _badge = MenuDraw.Node(holder, "Badge", gr);
                MenuDraw.Rect(_badge, Art(ArtBadge), gr, "img", QBadge, null, true);
            }
        }

        /// <summary>`Warning` 的宽。锚是 `0.1 → 0.9` ⇒ `0.8 × 父宽 + 30`，父 = holder。
        /// 🔴 这里算不出精确值（父宽 = 内容宽，而它自己又是内容的一员 ⇒ 循环），**原版也只在
        /// `IsPremiumLocked` 时才显它**，而我们的口径下高级轨恒解锁（见 `PremiumLocked`）
        /// ⇒ 取一个**够放文案的宽度**，并标明**这是我们挑的**。</summary>
        const float WarnW = 300f;

        // ============================================================ `Unlock Button` 的三态

        struct BtnLabels { public Label claimed; public Label cost; }

        /// <summary>**照 `CampaignUnlockButton`**：一个按钮上叠三样 —— `claimedText` / `costText` / `pointDrawer`，
        /// 由 `ToggleTexts(isCost)` 决定显示哪一组；再按状态给文字与 `interactable`。</summary>
        BtnLabels BuildUnlockButton(Transform btn, PxRect r, bool isBase, CampaignRewardsContext ctx, ImageQuad bgQ)
        {
            // `Icon Campaign Points Drawer Variant`：N(8, 0,0, .5,1, 1,.5, 0,0, −77.5,0) + **scl 1.5**
            // 🔴 **缩放要烘进矩形，别给父设 `localScale` 再照常摆子件** —— `MenuDraw.Local` 算的是
            //    「相对父的 localPosition」，父一带 scale 就被**再乘一次**（位置和大小同时偏 1.5×）。
            //    本轮实测：图标画成 150² 且往右偏了半个按钮（同 `ForgeTab` 那条「有 localScale 的子树」）。
            //    算式（照原文实算）：框 = 左半格 minus 77.5 ⇒ **45×45**；再乘 1.5 ⇒ 实显 **67.5²**，
            //    **右边缘钉在 按钮左 + 145**（pivot 在 (1,0.5)、缩放绕 pivot）⇒ 反推左上角。
            const float ptBox = PtIconBox, ptVis = PtIconBox * PtIconScale;
            float ptRight = r.x1 + 145f, ptCY = r.CY;
            var ptRect = new PxRect(ptRight - ptVis, ptCY - ptVis * 0.5f, ptRight, ptCY + ptVis * 0.5f);
            var pi = MenuDraw.Node(btn, "Icon Campaign Points Drawer Variant", ptRect);
            MenuDraw.Rect(pi, Art(ArtPointIcon), ptRect, "Icon", QUnlockPt, null, true);
            pi.gameObject.SetActive(false);

            // `Point Count`：N(8, .5,0, 1,1, 0,.5, 5,0, −5,−14.3902)
            var costR = UguiRect.Child(r, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
                                       new Vector2(5f, 0f), new Vector2(-5f, -14.3902f));
            var cost = MenuDraw.Text(btn, costR, "", Color.white, "Point Count", CostFont, QUnlockText,
                                     costR.W, CostAutoMin);
            // `Claimed Text`：N(8, 0,0, 0,0, .5,.5, 0,0, 0,0) —— 零尺寸锚点，文字自己撑开 ⇒ 用整个按钮当框
            // （`Label` 建出来就是 pivot (.5,.5) 居中在锚点上 ⇒ 「居中」不用再调对齐）
            var claimed = MenuDraw.Text(btn, r, "", Color.white, "Claimed Text", ClaimedFont, QUnlockText,
                                        r.W, ClaimedAutoMin);

            bool claimedState = isBase ? ctx.BaseCollected : ctx.PremiumCollected;
            bool premiumLocked = !isBase && ctx.IsPremiumLocked;
            if (premiumLocked)
            {
                pi.gameObject.SetActive(false);
            }
            else if (claimedState)
            {
                pi.gameObject.SetActive(false);
                SetText(claimed, TxtClaimed); SetText(cost, "");
            }
            else if (ctx.PointCost < 1)
            {
                // 免费领（原版 `SetUnlockCost` 的 `cost < 1` 分支走 `SetAsFreeClaim`）
                pi.gameObject.SetActive(false);
                SetText(claimed, TxtClaim); SetText(cost, "");
            }
            else
            {
                pi.gameObject.SetActive(true);                 // = `ToggleTexts(true)`：开 pointDrawer 与 costText
                SetText(claimed, ""); SetText(cost, ctx.PointCost.ToString());
            }

            var hit = new GameObject("Hit").transform;
            hit.SetParent(btn, false);
            MenuDraw.Rect(hit, CardArt.Solid(), r, "HitBox", QUnlockBg, new Color(0f, 0f, 0f, 0f));
            var wb = hit.gameObject.AddComponent<WindowButton>();
            bool clickable = !premiumLocked && !claimedState;
            wb.onClick = () => { if (clickable) OnUnlock(isBase ? CampaignData.TierBasic : CampaignData.TierPremium); };
            wb.Bind(bgQ, ArtUnlockBtn);   // A17：悬停换图（常态图 `UI_Button_Mulligan` → `_hover`）
            return new BtnLabels { claimed = claimed, cost = cost };
        }

        static void SetText(Label lb, string s) { if (lb != null) lb.SetText(s ?? ""); }

        /// <summary>`UnlockClicked(RewardTier)` → `context.OnCollect(tier)`（原版原样透传）。</summary>
        void OnUnlock(int tier)
        {
            if (_ctx != null && _ctx.OnCollect != null) _ctx.OnCollect(tier);
        }

        // ============================================================ 物品格

        /// <summary>一个奖励物品格。**走抽屉库**（`Shell/ItemDrawer.cs`）——
        /// 原版这一句就是 `ItemDrawer.Draw(holder, item, quantity, DrawerOverride.Default)`
        /// （`CampaignRewardsWindow__Open.c:134`；那 4 步见抽屉库的文件头）。
        /// <para>抽屉库里**有判据的**：野牌那四层（卡面底图 + 阵营徽记 + 阵营名 + 数量）·
        /// 「取不到图就不画」·「判据空 ⇒ 占位板」。
        /// **我们挑的**：格子尺寸（`ItemW/ItemH`）· 抽屉内部每层的版式（抽屉 prefab 本地没有）·
        /// 占位板的底色与短名。</para>
        /// <para>数量的值用原版的 `quantity`（`RewardInfo.quantity`），**不是我们编的**。</para></summary>
        void BuildItem(Transform parent, PxRect r, CampaignData.RewardSpec spec)
        {
            var item = ItemDrawer.Spec(spec.Id, CampaignData.ItemIcon(spec.Id), CampaignData.ItemShortName(spec.Id));
            var st = ItemDrawerStyle.Default(QItem, QItemIcon, QItemIcon);
            st.IconFill = ItemIconPx / Mathf.Min(ItemW, ItemH);            // = 0.7（值只有这一处，见 `ItemIconPx` 的注释）
            st.NodeName = "Item_" + CampaignData.ItemShortName(spec.Id);   // 自检按 `Item_` 前缀数格子
            st.Clip = null;                                                // 本窗的 `Scroll View` 没做裁剪（见文件头）
            var res = ItemDrawer.Draw(parent, r, item, spec.Quantity, DrawerOverride.Default, st);

            if (res.Placeholder && !NoIconItems.Contains(spec.Id)) NoIconItems.Add(spec.Id);
            if (res.FallbackArt && !FallbackArtItems.Contains(spec.Id)) FallbackArtItems.Add(spec.Id);
        }

        // ============================================================ Preview / Get 两态

        /// <summary>**照 `ConfigureIsPreviewState(bool)`**：`getReward*` 显 = **!isPreview**、
        /// `previewReward*` 显 = **isPreview**。四个件与 pid 的对应关系见文件头。</summary>
        public void ConfigureIsPreviewState(bool isPreview)
        {
            _isPreview = isPreview;
            SetActivePath("Content/Reward Background Get Reward", !isPreview);
            SetActivePath("Content/Reward Background Preview Reward", isPreview);
            SetActivePath("Content/Title/Glow Get reward", !isPreview);
            SetActivePath("Content/Title/Glow Preview reward", isPreview);
        }

        void SetActivePath(string path, bool on)
        {
            var t = _root != null ? _root.Find(path) : null;
            if (t != null) t.gameObject.SetActive(on);
        }

        // ============================================================ 状态刷新（领奖之后）

        /// <summary>换一个 context 重铺（领完奖之后 `CampaignTab` 会调这个）。</summary>
        public void Reopen(CampaignRewardsContext ctx) { _ctx = ctx; Build(); }

        // ============================================================ Premium 的两条判据

        /// <summary>`SetPremiumButton` 的两条分支（原版）：`IsPremiumLocked` ⇒ 显 `Warning`、藏按钮；
        /// 否则藏 `Warning`、显按钮。⚠️ **我们的口径下高级轨恒解锁**（用户 2026-09-22 裁决
        /// 「`Campaign Tab` 照建、Premium 轨全解锁」，见 `资料/阶段二外壳_待裁决清单_0922.md` #2）
        /// ⇒ 走的是「不锁」那条。**两条都实现了**（锁的那条留着，将来接真数据时直接用）。</summary>
        public bool PremiumLocked { get { return _ctx != null && _ctx.IsPremiumLocked; } }

        // ============================================================ 小工具

        void SetRect(Transform t, PxRect r)
        {
            t.localPosition = MenuDraw.Local(t.parent, r.x1, r.y1, r.x2, r.y2);
        }

        /// <summary>`MiddleCenter`：子件在 holder 的**内容区**里垂直居中（pad T25 B85）。</summary>
        static PxRect VertCenter(PxRect holder, PxRect child, float h)
        {
            float cy = (holder.y1 + PadT + holder.y2 - PadB) * 0.5f;
            return new PxRect(child.x1, cy - h * 0.5f, child.x2, cy + h * 0.5f);
        }

        Texture2D Art(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name)) MissingArt.Add(name);
            return t;
        }

        public string Dump()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("CampaignReward：");
            if (_ctx == null || _ctx.Rewards == null) sb.Append("**没有 context** ");
            else
            {
                int b = 0, p = 0;
                for (int i = 0; i < _ctx.Rewards.Length; i++)
                    if (_ctx.Rewards[i].Tier == CampaignData.TierPremium) p++; else b++;
                sb.Append("基础档 ").Append(b).Append(" 件 · 高级档 ").Append(p).Append(" 件 · ");
            }
            sb.Append("态 ").Append(_isPreview ? "Preview" : "Get")
              .Append(" · 基础列 x ").Append(_baseHolderR.x1.ToString("F1")).Append("→").Append(_baseHolderR.x2.ToString("F1"))
              .Append(" · 高级列 x ").Append(_premHolderR.x1.ToString("F1")).Append("→").Append(_premHolderR.x2.ToString("F1"))
              .Append(" · 无图标物品 ").Append(NoIconItems.Count).Append(" 个 · 缺图 ").Append(MissingArt.Count).Append(" 张")
              .Append(" · 退档图 ").Append(FallbackArtItems.Count).Append(" 个");
            return sb.ToString();
        }
    }
}
