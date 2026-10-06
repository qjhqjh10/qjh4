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
//     🔴 **2026-10-03 就地更正（铁律 5 · A86）**：本行原来写「**那个 SO 与那批抽屉 prefab 本地全都没有**
//     （全库搜 `ItemDrawer*` 资产 **0 命中**）」—— **两半都是假的**：① 那份 SO 在 `sharedassets0.assets`
//     （**没有 type tree** ⇒ 按字段名 grep 无效），**2026-10-04 已整张解出**（`资料/普查产出_1004/
//     ItemDrawerConfig_映射表.md`；脚本 `工具/read_itemdrawerconfig.py` 可复现）；② 那批抽屉 prefab
//     **也 dump 得出来**（`工具/menu_dump.py bundle_menus_assets_all "Deck Drawer"` 摊得出整棵子树）。
//     **错因** = 把「grep 不到」当成了「本地没有」。
//     ✅ **2026-10-03：抽屉收口到 `Shell/ItemDrawer.cs`（抽屉库，A12-P3 建）** —— 本文件里那一格现在**是真的抽屉**
//     （`ItemDrawer.Draw` 那四步），不再是本地手搓的一段。仍**是我们挑的**部分：抽屉**内部每层的版式**
//     （图标占框 0.7 / 数量钉底 / 阵营名条）与**格子尺寸** `ItemW×ItemH`（见那两条常量的注释）。
//     **物品图标**照 `CampaignData.ItemIcon`（数据层那张表）+ 抽屉库自己的野牌判据
//     （`WildcardDrawer` 那张 `40k_general_wildcard_*`）；**判据空的那 27 个 id 仍然画占位板 + 逐条打日志**。
//   · **`Tap To Continue`**（出厂 INACT、全库无脚本引用）⇒ **不建**（照本工程纪律①）。
//   · **`Scroll View` / `Viewport` 的裁切**：✅ **2026-10-08（A182）已建** —— `Viewport` 那颗
//     `RectMask2D` 的 `m_Softness = (200,0)` / `m_Padding = (0,0,0,0)` 照原版字段接上（见 `BuildColumn`）。
//     ⚠️ **滚动范围恒 = 0**（推导在 `Build()` ⑨）⇒ `MenuScroll` **不建**：建了也只能滚 0px = 假件。
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
        /// <summary>0x18 `OnCollect` —— 原版是 **`Action&lt;RewardTier&gt;`、没有返回值**
        /// （签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CampaignRewardsWindowContext.cs`）。
        /// 🆕 **2026-10-12（A448）收窄成 `Func&lt;int, bool&gt;`**，多出来的那个 `bool` = **本次领取成不成**。
        /// 为什么需要它：原版「领取成功 ⇒ 关窗」这个决定**不在窗里**，而在**领取成功回调**那一拍
        /// （判据逐句见 `OnUnlock` 的方法头）—— 原版的成功信号走**服务端回包**；
        /// 我们这一侧的领取是**同步**的（`Shell/CampaignTab.cs:838` `CampaignData.Claim(i, tier, out why) == true`）
        /// ⇒ 把同一个信号沿这条回调**原样带回来**，窗才能在**同一拍**里按它决定关不关。
        /// ⛔ 现有调用点 `Shell/CampaignTab.cs:808` 的 `OnCollect = tier =&gt; ClaimForTest(i, tier)`
        /// **一个字符都不用改**（表达式 lambda 对 `Action&lt;int&gt;` 与 `Func&lt;int, bool&gt;` 都成立）。
        /// ⚠️ **这不是「原版就是这样」，是我们的收窄**（如实标注，铁律 3）。</summary>
        public System.Func<int, bool> OnCollect;
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

        /// <summary>🆕 **2026-10-11（A272）**：`Scroll View/Viewport/Content` 那个**零宽点** ——
        /// 原版 = **960,285 → 960,935**（宽 **0**、两边由两列各自的锚点展开；它自己带
        /// `ContentSizeFitter m_HorizontalFit = 2(MinSize)`；判据 → 波 C1 报告 §一 那张表的第 3 层）。
        /// <para>⚠️ **我们建的是 `MenuDraw.Node`，它只吃这个矩形的中心**（只写 `localPosition`）⇒ 与本件原来那个
        /// 「与 `Viewport` 同矩形的容器」**行为等价**（两者的中心都是 960,610）—— 改过来是为了让**代码说的与原版
        /// 数据一致**：原来「同一个内容矩形在两处各表达一次」（波 C1 报告 §四·2）。</para>
        /// <para>🔴 它同时是「本窗**要不要**接 `MenuScroll`」的判据之一：内容宽 **0**
        /// ⇒ `contentW − viewW = 0` ⇒ 滚不动（推导见 `Build()` ⑨）。</para></summary>
        public static readonly PxRect InnerContent = new PxRect(960f, 285f, 960f, 935f);

        /// <summary>`Title`（`N(2, .5,1, .5,1, .5,1, 0,−28.3, 600,75)`）= 660,193.30 → 1260,268.30。</summary>
        public static readonly PxRect Title = new PxRect(660f, 193.30f, 1260f, 268.30f);

        /// <summary>`Scroll View`（`N(2, 0,0, 1,1, 0,.5, 0,−45, 0,−150)`）= 0,285 → 1920,935。</summary>
        public static readonly PxRect ScrollView = new PxRect(0f, 285f, 1920f, 935f);

        // ---------------- 🔴 `Scroll View` / `Viewport`（2026-10-08 · A182 建的）
        // 原版结构（`menu_dump.py bundle_menus_assets_all "Campaign Reward Window" --depth 5` 实读）：
        //   `Content` → `Scroll View`(`Image,ScrollRect` · **h=1 v=0 mode=1(Elastic) inertia=1**
        //   · elasticity 0.1 · decel 0.135) → **`Viewport`(`Image,RectMask2D`)** → `Content`(CSF h:MinSize=0)
        //   → `Base Rewards` / `Premium Rewards`；`Scroll View` 与 `Viewport` **同矩形** = `ScrollView`。
        // `RectMask2D` 的字段（逐处表 `d:/4/_tmp_view/q1_rm2d.txt` 第 272 行那一区）：
        //   `soft=(200,0) pad=(0.0,0.0,0.0,0.0) en=1`。
        /// <summary>原版 `RectMask2D.m_Softness` = **(200,0)**（x 管左右、y 管上下 ⇒ **只左右渐隐**）。
        /// 🔴🔴 **2026-10-13（A435 甲 · A198② 阶段 2）：类型从 `Vector2` 改成 `Vector2Int`** ——
        ///   它现在**就是**喂给 `ViewportClip.softness` 的那个值，而原版那个字段的类型逐字是
        ///   `RectMask2D.m_Softness: Vector2Int`（判据 → `Shell/ViewportClip.cs` 的字段注释）。</summary>
        public static readonly Vector2Int ScrollSoft = new Vector2Int(200, 0);
        /// <summary>原版 `RectMask2D.m_Padding` = **(0,0,0,0)**（UGUI 的 `(L,B,R,T)`；正 = 缩小）。</summary>
        public static readonly Vector4 ScrollPad = Vector4.zero;

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

        /// <summary>`Unlock Button` 自己的尺寸（`N(7, .5,0, .5,0, .5,0, 0,15, 245,45)`）——**245×45**。
        /// 🔴 **2026-10-13（A537）**：它同时是「**锚点驱动**」那一族的五元组 ——
        /// `anchorMin = anchorMax = (0.5, 0)` · `pivot = (0.5, 0)` · `pos = (0, 15)` · `sizeDelta = (245,45)`
        /// ⇒ **落在 holder 的底部居中**、**底边离 holder 底 15px**。⛔ 它**不参与** holder 的
        /// `HorizontalLayoutGroup`（判据见 `BuildColumn` 与 `BtnBottom`）。</summary>
        public const float UnlockW = 245f, UnlockH = 45f;
        /// <summary>`Unlock Button` 与 `Warning` **共用的** `m_AnchoredPosition.y`（prefab 原文 = **15**）。
        /// 两者都是 `anchorMin = anchorMax = (·, 0)` + `pivot = (·, 0)` ⇒ **底边离 holder 底 15px**
        /// （`pos.y` 在 uGUI 里**向上为正**，本仓坐标系 y 向下 ⇒ 实算是「holder 底 − 15」）。</summary>
        public const float BtnBottom = 15f;
        /// <summary>`Unlock Button/Icon Campaign Points Drawer Variant` —— 它的框 = 按钮左半格减 77.5 ⇒ **45×45**；
        /// 该节点 `localScale = 1.5` ⇒ **实显 67.5²**（缩放烘进矩形，见 `BuildUnlockButton` 的注释）。
        /// 出厂 INACT，只有「付点解锁」态由 `ToggleTexts(true)` 打开。</summary>
        public const float PtIconBox = 45f, PtIconScale = 1.5f;
        /// <summary>`Warning` 的高（prefab 原文 `m_SizeDelta.y = 45`）。
        /// 🔴 **2026-10-13（A537）**：它也是「**锚点驱动**」那一族 —— 宽**不是常量**：
        /// `anchorMin.x 0.1 / anchorMax.x 0.9` ⇒ **显示宽 = 0.8 × 父宽 + `WarnSizeDX`(30)**、
        /// 父 = holder（宽由内容决定），见 `WarnAnchorMin` 与 `BuildColumn`。</summary>
        public const float WarnH = 45f;
        /// <summary>`Warning` 的锚点跨度与 `m_SizeDelta.x`（prefab 原文 `0.1` / `0.9` / `30`）。
        /// 🔴 **2026-10-13（A537）就地更正（铁律 5）**：这里原来是 `const float WarnW = 300f`，
        /// 注释写着「原版取不到 —— 父宽 = 内容宽、而它自己又是内容的一员 ⇒ **循环** ⇒ 取一个够放文案的
        /// 宽度，并标明**这是我们挑的**」。**那个「循环」是假矛盾**：它成立的唯一前提是
        /// 「`Warning` **参与** holder 的布局组」；而 prefab 里它挂着 `LayoutElement.m_IgnoreLayout = 1`
        /// ⇒ **一格都不占** ⇒ 父宽 = 纯内容宽、**精确值算得出**。⇒ 现在这三个数就是原版字段本身，
        /// **不再是「我们挑的」**（判据 = `BuildColumn` 里那三条互证）。</summary>
        public const float WarnAnchorMin = 0.1f, WarnAnchorMax = 0.9f, WarnSizeDX = 30f;
        /// <summary>`Badge`（`N(7, 0,1, 0,1, 0,1, 0,0, 100,100)`，`40k_campaign_Premium-icon`）。
        /// 🔴 **2026-10-13（A537）**：`anchorMin = anchorMax = (0,1)` · `pivot = (0,1)` · `pos = (0,0)`
        /// ⇒ 它落在 **holder 的左上角**（**连 `PadL`(30) 都没加** —— 布局组永远摆不出这个位置）。</summary>
        public const float BadgeSize = 100f;

        /// <summary>🔴 **我们挑的**：一个奖励物品格的尺寸。原版这里是 `ItemDrawer` 从
        /// `ItemDrawerConfig` 里取的抽屉 prefab ⇒ **尺寸在 prefab 里**，我们没照它量（照 dump 出来的
        /// 抽屉几何改版式 = **一件待做的活**，见 `Shell/ItemDrawer.cs` 文件头 ②）⇒ 这个值是我们定的，
        /// 依据只有一条：**两列各最多 2 个物品**（89 条奖励里基础档最多 2、高级档最多 1），
        /// 取 200 才让「2 物品 + 按钮 + 徽标」在 960 宽的列里放得下。
        /// <para>🔴 **2026-10-03 就地更正（铁律 5 · A86）**：本行原来把理由写成「抽屉 prefab（**本地没有**）」
        /// —— **假的**：那张 SO 2026-10-04 已整张解出、抽屉 prefab 也 dump 得出来；**但「这个尺寸是我们挑的」
        /// 这件事没变**（倒掉的是**前提**，不是**事实**）。</para></summary>
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
        /// <summary>🔴 **A406（2026-10-12）逐站实读**：`Campaign Reward Window/Content/Title/Glow {Get,Preview} reward/
        /// `Text {Get,Preview} Reward` 两颗原版都是 `auto[25.0~**50.0**] 基准=**36.0**`
        /// ⇒ 上限 **50.0**（= `TitleFont`，**恰好相等**）· base **36.0**。
        /// 判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Campaign Reward Window" --depth 10 --md`
        /// 里那两行的 `auto[25.0~50.0] 基准=36.0`。⚠️ 同树另两颗上限**不是** 50
        /// （`Unlock Button/Point Count` = 32.0 · `Claimed Text` = 35.0）—— 逐颗读，别按窗统一。</summary>
        public const float TitleAutoMax = 50f, TitleAutoBase = 36f;

        /// <summary>按钮文案。⚠️ **原版是 I2 词条**（`Rewards Menu/Claimed` · `MainMenu/General/Claim`），
        /// 词条表在**远端 CCD、本地没有** ⇒ 下表是**我们按 term 末段填的**（不是原版字符串）。</summary>
        public const string TxtClaimed = "Claimed", TxtClaim = "Claim";
        /// <summary>⚠️ 同上：结论是 `Rewards Menu/PremiumUnlockWarning` / `Rewards Menu/PremiumExtraTip`，
        /// **真值本地没有** ⇒ 这两句是**我们填的占位**（而且我们的口径下高级轨恒解锁 ⇒ 一般不显示）。</summary>
        public const string TxtPremWarning = "Premium campaign required",
                                  TxtPremTip = "Unlock the Premium track to claim";
        public const float ClaimedFont = 35f, ClaimedAutoMin = 10f, CostFont = 32.3f, CostAutoMin = 10f;
        public const float WarnFont = 47.5f, WarnAutoMin = 18f;

        /// <summary>🔴 **2026-10-12（A446）逐站实读**：下面三站的 `autoMax` / `base`
        /// （= 原版 `m_fontSizeMax` / `m_fontSizeBase`，画布 px）。**标称与 `autoMin` 本来就是对的**
        /// （A413 那轮逐处核过）—— 欠的就是这两档：它们走的共同基类 `GameWindow.Text` 当时**没有这两个形参**
        /// （A446 已补并透传，见 `Shell/WindowsManager.cs` 的 `Text`）。
        /// 判据命令（读数取各行的 `字号=` / `基准=` / `auto[…]` 三段）：
        /// <code>python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Campaign Reward Window" --depth 10 --md</code>
        /// 实读（2026-10-12 本件亲跑，读数逐位抄自输出）：
        /// · `…/{Base,Premium} Rewards/Rewards/Warning` = `字号=47.5 基准=36.0 auto[18.0~30.0]` ⇒ **30 / 36**
        /// · 同上 `/Unlock Button/Point Count`          = `字号=32.3 基准=12.0 auto[10.0~32.0]` ⇒ **32 / 12**
        /// · 同上 `/Unlock Button/Claimed Text`         = `字号=35.0 基准=12.0 auto[10.0~35.0]` ⇒ **35 / 12**
        /// ⚠️ **逐颗读、别按窗统一**：同树的 `Text {Get,Preview} Reward` 是 `50.0 基准=36.0 auto[25.0~50.0]`
        /// （= 上面的 `TitleAutoMax` / `TitleAutoBase`），三处各不相同。
        /// ⚠️ `Warning` 那一颗 **`max(30) &lt; 字号(47.5)`**：**原版就是这样**（`max` 管的是自适应那一档的
        /// 上限，见 `MenuDraw.Text` 的 A333 注），⛔ 别「顺手校正」成 47.5。</summary>
        public const float WarnAutoMax = 30f, WarnAutoBase = 36f;
        public const float CostAutoMax = 32f, CostAutoBase = 12f;
        public const float ClaimedAutoMax = 35f, ClaimedAutoBase = 12f;

        // ============================================================ 运行时状态

        public readonly List<string> MissingArt = new List<string>();
        /// <summary>本地查不到图标的物品 id（**逐条出声**，自检也钉它）。</summary>
        public readonly List<string> NoIconItems = new List<string>();
        /// <summary>🆕 抽屉里那张**判据图**本地取不到、退了 `_small` 档的物品 id（**出声** —— 退化不是静默）。
        /// 判据：原版野牌用的是 `40k_general_wildcard_&lt;rarity&gt;`（328×497 的平铺卡面），
        /// 我们 `Resources/` 下只有 `_small` 档（斜置小卡），见 `ItemDrawer.WildcardTex`。</summary>
        public readonly List<string> FallbackArtItems = new List<string>();

        CampaignRewardsContext _ctx;
        Transform _root, _baseHolder, _premHolder, _baseBtn, _premBtn, _warn, _badge;
        /// <summary>🆕 **2026-10-12（A481）**：`Content/Reward Claim` 那棵子树（= 原版 prefab 里那一份；
        /// **与 `Reward Window` 共用同一个实现** `Shell/RewardWindow.cs` 的 `RewardClaimFx`）。
        /// 🔴 **本窗里它【原版就没有人驱动】⇒ 我们建出来但恒不激活**（判据见 `Build()` 里那一段）。
        /// 字段留着只为自检能读到它（`ClaimFx`）。</summary>
        GameObject _claimFx;
        /// <summary>`Content/Reward Claim` 那棵子树（自检读它；`null` = 没建）。</summary>
        public GameObject ClaimFx { get { return _claimFx; } }
        PxRect _baseHolderR, _premHolderR, _baseColR, _premColR;
        Label _baseClaimed, _premClaimed, _baseCost, _premCost;
        bool _isPreview;

        /// <summary>两列 holder 的实算矩形（自检直接量它）。</summary>
        public PxRect BaseHolderRect { get { return _baseHolderR; } }
        public PxRect PremHolderRect { get { return _premHolderR; } }
        public bool IsPreview { get { return _isPreview; } }

        /// <summary>🆕 **2026-10-05（A81）**：压暗层的**命中区**节点（「点窗外关窗」）—— 自检用
        /// （`MenuDraw.ShadeRuleOk(darkHit, qShade, qContentMin, out why)` 的 `darkHit`）。
        /// 🔴 **本窗没有关窗钮 ⇒ 这一颗是唯一的关窗路径** ⇒ 它不在 = 玩家关不掉这扇窗。</summary>
        public Transform ShadeHit { get { return transform.Find("BackgroundHit"); } }

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
            _claimFx = null;         // A481：整棵子树随 `root` 的子件一起被删了 ⇒ 记账也清掉
            var ctx = _ctx ?? new CampaignRewardsContext { Rewards = new CampaignData.RewardSpec[0] };

            var full = new PxRect(0f, 0f, 1920f, 1080f);

            // ① `Menu Dark Background`（sprite 为空 ⇒ 纯色块）
            MenuDraw.Rect(root, CardArt.Solid(), Shade, "Menu Dark Background", QShade, ShadeColor);
            // 🔴 **2026-10-05（A81）**：压暗层的**点击区**（「点窗外关窗」）—— **原来一处命中区都没有**。
            //   🔴 **这扇窗没有独立的关窗钮**（该 prefab 的组件列逐颗读过，§A81 裁定）⇒
            //   **这条命中区是它【唯一】的关窗路径**：漏了 = 点哪都关不掉这扇窗。
            //   判据：原版是压在 `Menu Dark Background` **自身节点**上的 `BackgroundCloseButton`
            //   （签名桩里字段名就叫 **`closeButton`** —— ⚠️ 字段名各窗不同，别按名字 grep），
            //   由窗口类自己挂/摘（`CampaignRewardsWindow__OnEnable.c:12-19` 挂 · `OnDisable.c:12-19` 摘）；
            //   挂的处理函数是那条虚调用槽 `+0x1b8/+0x1c0` = **`GameWindow.Close`（`Slot: 8`）**
            //   （工程里已有这条实读：`资料/日常_调用链_DailyStreak.md:75`；`dump.cs` 的 `GameWindow`
            //   一节里 `Close()` 正是 `Slot: 8`）—— 与另外四扇窗那两颗用的是同一槽。
            //   档 = **压暗层自己那一档 `QShade`(3110)**，**严格低于**本窗内容命中区档 `QUnlockBg`(3118)
            //   —— `Unlock Button` 那颗的 `HitBox` 用的就是 `QUnlockBg` ⇒ 档不拉开就会**抢走领奖钮的点击**
            //   （症状是「点不动的钮看着像正常工作」）。
            //   出处 → `资料/待办判据_阶段二与联机.md` §A81 · 公共件规矩 → `MenuDraw.ShadeHit` 的注释。
            MenuDraw.ShadeHit(root, Shade, QShade, QUnlockBg, () => Close(), "BackgroundHit");

            // ③ `Content`
            var content = MenuDraw.Node(root, "Content", Content);
            // `Reward Background Get Reward`（红）/ `Reward Background Preview Reward`（灰）—— 出厂 active = 后者
            MenuDraw.Rect(content, Art(ArtBgGet), Content, "Reward Background Get Reward", QContentBg)
                ?.gameObject.SetActive(false);
            MenuDraw.Rect(content, Art(ArtBgPreview), Content, "Reward Background Preview Reward", QContentBg);
            // 🆕 **2026-10-06（A94）：那一整块面板底图吸收点击**。判据 = 原版 prefab
            //   `Campaign Reward Window > Content > Reward Background*` 那颗 `Image` 的
            //   **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读；整块盖满 0,165→1920,965，
            //   出厂 active 的是 `Preview Reward` 那张）—— 射线打到它自己、父链上没有点击处理器
            //   （关窗那颗 `BackgroundCloseButton` 在压暗层上）⇒ 原版点这里**什么都不做**。
            MenuDraw.Absorb(root, "AbsorbHit", Content, QShade, QUnlockBg);

            // ③·b 🆕 **2026-10-12（A481）**：`Content/Reward Claim` 那棵子树 —— **与 `Reward Window`
            //   共用同一个实现**（`Shell/RewardWindow.cs` 的 `RewardClaimFx`，⛔ 别各写一份；那两个宿主
            //   prefab 里这一份的**参数逐位相同**：`m_IsActive=false` · `pos (0,−32,0)` · `scale 684.33`）。
            //   🔴 **原版在这扇窗里【没有任何驱动】**（本件实读）：`CampaignRewardsWindow` 的字段表
            //   （`dump.cs` `// 0x70 … 0xF0`）**没有** `claimRewardParticles` 那一项，而
            //   `CampaignRewardsWindow__Open.c` 里那四句 `SetActive` 打的是 `0xC8/0xD0/0xD8/0xE0`
            //   （四个 Preview/Get 件）—— **一处都不碰 `Reward Claim`** ⇒ 那棵子树**恒为出厂那个 `false`**。
            //   ⇒ 我们**照建、不激活**（`SetVisible(_claimFx, false)`）—— 这不是「忘了接」，是判据如此
            //   （⚖️ 与 `Reward Window` 那一份的区别**是原版的区别**：那边 `Open()` 会按 `!IsPreview` 开）。
            _claimFx = RewardClaimFx.Build(content);
            RewardClaimFx.SetVisible(_claimFx, false);

            // ④ `Scroll View` → `Viewport` → `Content`（两列的父）
            var sv = MenuDraw.Node(content, "Scroll View", ScrollView);
            // ⚠️ `Scroll View` 自己那个 `Image` 是 `Background` 图但 `m_Color` 的 **alpha = 0** ⇒ 画了也看不见，
            //    照「不画不可见的件」的纪律**不建**（`Viewport` 的 `UIMask` 也是 alpha 0）。
            // 🔴🔴 **2026-10-13（A435 甲 · A198② 阶段 2）**：这一颗是**视口节点**，裁切状态挂在它身上
            //   （= 原版 `RectMask2D` 挂 `Campaign Reward Window/Content/Scroll View/Viewport`；
            //   契约 → `Shell/ViewportClip.cs` 文件头）。走 `ViewportClip.Hang` ⇒
            //   **框（节点自己的 rect）· `padding` · `softness` 三样一次写死**：
            //   `ScrollPad = (0,0,0,0)` · `ScrollSoft = (200,0)`（原版实读，`q1_rm2d.txt` 第 272 行那一区）。
            //   ⛔ **别改回 `Node(...)`** —— 那样节点上就没有状态了（本窗那些列底/按钮/抽屉全靠它裁）。
            var vp = ViewportClip.Hang(sv, "Viewport", ScrollView, ScrollPad, ScrollSoft).transform;
            // 🆕 **2026-10-11（A272）**：原版这一层是「960,285 → 960,935 的**零宽点**」（CSF `MinSize` 撑出来的），
            //   原来建的是与 `Viewport` 同矩形的容器 —— 两者**中心相同**（`MenuDraw.Node` 只吃中心）⇒ 行为等价，
            //   改的是**表达**（同一个内容矩形不再在两处各写一遍）。见 `InnerContent` 的注释。
            var inner = MenuDraw.Node(vp, "Content", InnerContent);

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
                                   TitleFont, QTitleText, glowGetR.W, TitleAutoMin, TitleAutoMax, TitleAutoBase) : null;
            if (gt != null) MenuDraw.AlignRight(gt, glowGetR);
            var gp = MenuDraw.Rect(title, Art(ArtGlow), glowGetR, "Glow Preview reward", QTitleBg, GlowPreview, true);
            var pt = gp != null ? MenuDraw.Text(gp.transform, glowGetR, TxtPreview, Color.white, "Text Preview Reward",
                                   TitleFont, QTitleText, glowGetR.W, TitleAutoMin, TitleAutoMax, TitleAutoBase) : null;
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
                                 + " —— 原版走 `ItemDrawer`；🔴 2026-10-03 更正（A86）：本行原来接着说「那个 `ItemDrawerConfig` SO"
                                 + "与那批抽屉 prefab 本地都没有」，**假的** —— SO 已整张解出、prefab 也 dump 得出来；"
                                 + "**真正缺的是这几个 id 的图标**（那批 id 连 SO 都没导出 ⇒ 判据空）"
                                 + "（铁律 11 第①种：原版本身取不到 ⇒ 占位板 + 出声；见 `Shell/ItemDrawer.cs` 文件头）");
            if (FallbackArtItems.Count > 0)
                Debug.Log("[CampaignReward] 有 " + FallbackArtItems.Count + " 个野牌物品用的是**退档图**"
                          + "（原版那张 `40k_general_wildcard_<rarity>` 不在 `Resources/` 下 ⇒ 退了 `_small`）："
                          + string.Join("、", FallbackArtItems.ToArray()) + " —— 见 `ItemDrawer.WildcardTex`");

            // ⑨ 🔴 **2026-10-08（A182）：`Scroll View`/`Viewport` 的裁切已接**（原来这里是那句
            //   「滚动与 `RectMask2D` 裁剪**本轮没做**」的出声日志 —— 出声日志按规矩随「没做」一起撤掉）。
            //   **为什么仍然不建 `MenuScroll`（= 滚动范围恒 0，不是「不做滚动」）**：
            //     · 原版 `Content` 的矩形 = **960,285 → 960,935**（`menu_dump` 实读：宽 **0**，
            //       它是 CSF(`m_HorizontalFit = 2 = MinSize`) 撑出来的零宽点，两列由锚点各自展开）；
            //     · UGUI 的滚动范围只认 **`m_Content` 自己那四个世界角**（`ScrollRect.GetBounds()` →
            //       `m_Content.GetWorldCorners(m_Corners)`，本地源码
            //       `PackageCache/com.unity.ugui@27635d171b1a/…/ScrollRect.cs:1354-1361`），
            //       **不是**子件的并集；且 `AdjustBounds` 会把比视口小的内容补到视口大小；
            //     · ⇒ `contentBounds.size.x (0→1920) − viewBounds.size.x (1920) = 0`
            //       ⇒ `InternalCalculateOffset`（同文件 `:1386-1416`）两个方向都取不到偏移
            //       ⇒ **原版这扇窗的 `ScrollRect` 也一个字都滚不动**（它是 `h=1 v=0 mode=1` 的活件，
            //       只是范围恰好是 0）。我们两列的内容宽（A537 之后 = **只有物品**：1 件 = 260、
            //       真数据最多 2 件 = 485 < 960 一列）也放得下。
            //       🔴 **2026-10-13（A537）就地更正（铁律 5）**：本行原文写「最多 **655+225 = 880**」
            //       —— 那是把按钮/徽标也算进内容宽的旧模型的数（现在那三颗件一颗都不进表，见 `BuildColumn`）。
            //   ⇒**这一扇要的是「视口裁切」**（`Clip` + 软边 `(200,0)`，见 `BuildColumn`），不需要滚动件。
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

        /// <summary>一列。子件次序照原版子节点序：**物品（运行时插列首）→ `Unlock Button` → [`Warning` → `Badge`，高级列才有]**。
        /// 🔴 **2026-10-08（A182）**：整列（列底九宫格 + 物品抽屉 + 按钮 + 警告 + 徽标）都长在
        /// `Content/Scroll View/Viewport` 那颗 `RectMask2D` 之下 ⇒ 内部每一件走带裁切的那条路
        /// （`DrawRect` / 本文件 `Text` / `ItemDrawer` 那两条守卫）。
        /// 🔴🔴 **2026-10-13（A435 甲 · A198② 阶段 2）：裁切状态【改了载体】** —— 原来这里「成对拿捏
        /// 三件套」（存 → 设 `Clip` = 视口矩形 / `ClipSoftness` = **(200,0)** / `ClipPad` = **(0,0,0,0)**
        /// → 建完还原）；现在那三样**长在视口节点上**（`Build()` 里 `ViewportClip.Hang(sv, "Viewport",
        /// ScrollView, ScrollPad, ScrollSoft)` 一次写死）⇒ 本方法**一个字都不设**，
        /// 每一件沿父链解析到那颗节点（`ViewportClip.Resolve` 第 2 支）。
        /// ⚠️ 拿捏范围**只包列本身**：`Title` / `Menu Vignette` / 压暗层都在视口**之外**（原版也如此
        /// —— 它们是 `Content` 的兄弟，不在 `Scroll View` 里）⇒ **别把节点挂到窗根上**（那会把它们一起裁掉）。</summary>
        void BuildColumn(Transform holder, PxRect col, CampaignData.RewardSpec[] items, bool show,
                         bool centered, float centerX, bool isBase, CampaignRewardsContext ctx)
        {
            for (int i = holder.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(holder.GetChild(i).gameObject);
            if (!show) return;

            // 🔴🔴 **2026-10-13（A435 甲 · A198② 阶段 2）：那三行没了。** 旧写法 = 「设 `Clip` = 视口矩形 /
            //   `ClipSoftness` = `ScrollSoft` / `ClipPad` = `ScrollPad`，建完成对还原」。
            //   现在那三样长在**视口节点**上（`Build()` 里那句 `ViewportClip.Hang(…)` 一次写死）⇒
            //   本方法**一个字都不设**、每一件沿父链解析到那颗节点
            //   （⛔ 别再把 `Clip = ScrollView;` 加回来 —— 那会让 `Editor/RewardsScene.cs` 的 A489
            //      `NodeShadowedByParam == 0` 红，而画面**看不出任何差别**）。

            // ============================================================ 子件宽度表（A537）
            // 🔴🔴 **2026-10-13（A537）：这张表里【只有物品抽屉】—— 那三颗件一颗都不进表。**
            //   判据 = **三条互证**（本件逐条亲读，不是转述）：
            //   ① **prefab 字段**（`d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/`）：
            //      三颗件各自挂着 `LayoutElement`、且 **`m_IgnoreLayout = 1`**
            //      （六个 `m_Min/m_Preferred/m_Flexible*` 全 `-1`）：
            //        · `…/Premium Rewards/Rewards/Unlock Button`  → `LayoutElement_-6614172172701557626`
            //          （基础列那颗同：`LayoutElement_8356385346601655430`）
            //        · `…/Premium Rewards/Rewards/Warning`        → `LayoutElement_-2770069720850916218`
            //        · `…/Premium Rewards/Rewards/Badge`          → `LayoutElement_-4015934712974893946`
            //   ② **uGUI 源码**（本地包）：`LayoutGroup.CalculateLayoutInputHorizontal()` 建 `m_RectChildren`
            //      时**跳过** `ILayoutIgnorer.ignoreLayout == true` 的子件
            //      （`MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/
            //       Layout/LayoutGroup.cs:52-79`；`LayoutElement : ILayoutIgnorer`）——
            //      而 `m_RectChildren` 是**算尺寸与摆位置唯一用的那张表**。
            //   ③ **几何**（同一份 prefab，逐字段现读；两个数都摆不出来 ⇒ 只能是锚点驱动）：
            //        · holder 自己 `sizeDelta.x = 0` + CSF `h:MinSize` ⇒ 出厂宽 = **60**
            //          （= `PadL 30 + PadR 30 + 0 个物品` ⇒ **三颗件一格都没贡献**）；
            //        · `Unlock Button` = `anchor/pivot (0.5,0)` · `pos (0,15)` · `245×45`
            //          ⇒ 以 **holder 的中心**为心、**底边离 holder 底 15px** —— 布局组**永远摆不出水平居中**；
            //        · `Badge` = `anchor/pivot (0,1)` · `pos (0,0)` · `100×100` ⇒ holder 的**左上角**
            //          （**连 `PadL`(30) 都没加**）。
            //   ⇒ holder 内容宽 = `PadL + Σ物品宽 + Spacing×(n−1) + PadR`
            //     （**0 物品 = 60**（= prefab 实读值）· **1 物品 = 260**（= 30+200+30））。
            //   ⛔ **别再往这张表里塞那三颗件**（A537 之前就是那么写的）：holder 会从 260 涨到
            //     505/760（`UnlockW 245` / `+BadgeSize 100`），按钮与徽标被挤到物品右边、
            //     **静默**（矩形断言全绿、画面上整列是错的）。
            //   ⚠️ **A402③ 那一半已被本条吸收**：W-C1 当时改成「锁定支不收 `UnlockW`」，
            //     方向对、但只对锁定支成立；真判据是**任何支都不收**（`m_IgnoreLayout` 与锁定与否无关）。
            bool lockedHere = !isBase && PremiumLocked;      // 仍要用：它驱动「警告开 / 按钮整颗关」
            float[] ws = new float[items.Length];
            for (int i = 0; i < ws.Length; i++) ws[i] = ItemW;
            // 🔴 **2026-10-12（A239）：这张表**同时**喂「内容宽」与「每一件的位置」** ——
            //    两处各算一遍 = 「容器宽度」与「子件落点」迟早对不上（CLAUDE.md §三）。
            float holderW = UguiLayout.HorizontalContentW(ws, PadL, PadR, Spacing);

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
            // 🔴 **必须走九宫格**：这张图只有 **69×63**、四边 `m_Border` 各 **18px** —— 拉成 260×615 的
            //    单块会把那个「八角框」整个抻变形（2026-09-23 实拍发现：画出来是个被拉长的八边形）。
            // 🔴 A182：改走 `DrawNine`（= `MenuDraw.Nine` + 本窗的 `RenderClip`/`ClipSoftness`）——
            //    压在视口边上的那一列底要跟着渐隐（原版 `RectMask2D` 一视同仁）。
            DrawNine(holder, Art(ArtColumnBg), hr, new Vector4(18f, 18f, 18f, 18f), 69f, 63f, QColumnBg);

            // ⚠️ **物品的槽号是「反序」的 `k`（= `n−1−i`），⛔ 别顺手换成 `i`** —— 原版每画一条就
            //    `SetAsFirstSibling()` 一次（`CampaignRewardsWindow__Open.c:135-145`；正本 §十四 `:598`）⇒
            //    效果是**列表里最后一条被挤到最左**。我们按**反序建节点**复现同一个树序
            //    ⇒ 第 `i` 条落在槽 `n−1−i`。
            //    换成 `i` 会把整列奖励**左右颠倒**（静默：矩形断言全绿）。
            //    （**2026-10-03 订正留档**：更早那版注释写「先画物品、后画按钮 ⇒ 同样的次序」——
            //     对**一条**奖励成立、**两条以上不成立**；错因 = 照「物品挤到列首」这一点推的，
            //     没去读 `SetAsFirstSibling` 的语义。）
            // 🔴 **2026-10-13（A537）**：**物品是 holder 的 `HorizontalLayoutGroup` 里【唯一】的子件**
            //    （那三颗件 `m_IgnoreLayout = 1`，见上面那条）⇒ 槽号就是 `ws` 的下标、逐件从
            //    `hr.x1 + PadL` 起排 + `Spacing`。**竖向仍是 `MiddleCenter`**（`ctrlH = 1` 用子件自己的
            //    首选高）—— 这一半以前就是对的，没动。
            //    ⚠️ **A239（2026-10-12）那两半的原因都随本条一起消失**（列**不再混宽**了：表 = `[ItemW]×n`）；
            //    这里保留「两个数从同一张 `ws` 派生」的写法，是因为它仍是**唯一**的宽度来源
            //    （⛔ 别改成各自手写一遍算式）。
            int k = 0;
            for (int i = items.Length - 1; i >= 0; i--, k++)
            {
                var r = UguiLayout.HorizontalChildOwnHeight(hr, ws, ItemH, k, PadL, Spacing);
                r = VertCenter(hr, r, ItemH);
                BuildItem(holder, r, items[i]);
            }

            // ============================================================ 三颗件（A537）：**锚点驱动**
            // 🔴 它们**不参与** holder 的 `HorizontalLayoutGroup`（`LayoutElement.m_IgnoreLayout = 1`，
            //   判据见上面那三条互证）⇒ 落点一律由 **prefab 自己的锚点五元组**算，
            //   算式只此一处 = `UguiRect.Child`（⛔ 别在这里手写一遍「中心 ± 半个宽」——
            //   `Core/UguiRect.cs` 那份是照原版公式逐条抄的，也是 `工具/menu_rect.py` 的对照实现）。
            // `Unlock Button`：`N(7, .5,0, .5,0, .5,0, 0,15, 245,45)`
            //   ⇒ `anchorMin = anchorMax = (0.5,0)` · `pivot (0.5,0)` · `pos (0,15)` · `245×45`
            //   ⇒ **holder 底部居中**、**底边离 holder 底 15px**。
            var br = UguiRect.Child(hr, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), UguiRect.P50,
                                    new Vector2(0f, BtnBottom), new Vector2(UnlockW, UnlockH));
            var btn = MenuDraw.Node(holder, "Unlock Button", br);
            // A17：原版 `Campaign Reward Window>…>Unlock Button` 是 SpriteSwap（普查 §块 2 第 8 行；高级列同 prefab 二次实例）
            var bgQ = DrawRect(btn, Art(ArtUnlockBtn), br, "bg", QUnlockBg);   // A182：吃视口裁切
            var btn2 = BuildUnlockButton(btn, br, isBase, ctx, bgQ);
            if (isBase) { _baseBtn = btn; _baseClaimed = btn2.claimed; _baseCost = btn2.cost; }
            else { _premBtn = btn; _premClaimed = btn2.claimed; _premCost = btn2.cost; }

            // 🔴 **2026-10-13（A402 ②）：锁定支 —— 整颗按钮【不在】**（原版 `SetPremiumButton.c:85`）。
            //   ⚠️ **这一句不是「变灰」的等价物，两句都必须在**：本句管「不在」（`SetActive(0)`），
            //   `BuildUnlockButton` 里那句 `wb.Interactable = clickable` 管「在但点不动」
            //   （= 原版另外两支里的 `set_interactable`）。⛔ 别用其中一句顶替另一句：
            //     · 只变灰不隐藏 ⇒ 锁定支下屏幕上还立着一颗灰钮（原版那一支里它整个不在）；
            //     · 只隐藏不设 `Interactable` ⇒ 节点一旦被人打开它就是可点的（两半都得有）。
            //   ✅ 顺带正确：`Shell/PointerLayer.cs:488` 的 `CollectHits` 只挑 `WindowButton.isActiveAndEnabled`
            //      ⇒ 藏起来之后这一颗**不再吃点击**（= 原版 INACT 子件不进射线）。
            //   📌 **A537 之后它与布局表脱钩**：那句「锁定支不收 `UnlockW`」（A402③）已并入上面那条
            //      —— 按钮**在哪种支下都不进表**，这里的 `SetActive(false)` 只影响「画不画 / 吃不吃点击」。
            if (lockedHere) btn.gameObject.SetActive(false);

            if (!isBase)
            {
                // `Warning`：`N(7, .1,0, .9,0, .5,0, 0,15, 30,45)`
                //   ⇒ `anchor (0.1,0)-(0.9,0)`（横拉 80%）· `pivot (0.5,0)` · `pos (0,15)` · `sizeDelta (30,45)`
                //   ⇒ **宽 = 0.8 × holder 宽 + 30**（父 = holder）· **底部居中**、离底同样 15px。
                //   ⚠️ 那个「父宽 = 内容宽 ⇒ 循环」是**假矛盾**（它一格都不占，见上），见 `WarnAnchorMin` 的注释。
                var wr = UguiRect.Child(hr, new Vector2(WarnAnchorMin, 0f), new Vector2(WarnAnchorMax, 0f),
                                        UguiRect.P50, new Vector2(0f, BtnBottom),
                                        new Vector2(WarnSizeDX, WarnH));
                _warn = MenuDraw.Node(holder, "Warning", wr);
                // ⚠️ 原来是「建完再 `MenuDraw.AlignRight(wl, wr)`」—— A182 接上裁切之后那样**顺序反了**
                //    （`ClipText` 夹的是世界坐标的顶点，先裁再挪 = 把裁好的块挪出框）⇒ 改成走 `align` 参数。
                // 🔴 **2026-10-13（A635）就地订正（铁律 5）**：上面那次改道**顺手传了 `align = 2`**，理由是
                //    「原版 `Warning` 就是 `Right`」—— **那个理由是错的**：它把 **TMP 的枚举**当成了**我们自己的**。
                //    两套枚举不是一套：
                //      · 原版 TMP `HorizontalAlignmentOptions`：`Left = 1` · **`Center = 2`** · `Right = 4`；
                //      · 本仓 `GameWindow.Text` 的 `align`（`Shell/WindowsManager.cs:346`）：`0 = 居中 · 1 = 左 · 2 = 右`
                //        ⇒ **只有 `1` 与 TMP 同义**；`2` 恰好是「原版的**居中** / 我们的**右**」⇒ 文字被推到框最右。
                //    🔴 让这件事**从「看不出来」变成「看得出来」的是 A537**：改前那个框是「按钮右边那一格」，
                //      右对齐尚且像故意的；改后框**居中在列里**（宽 238）⇒ 右对齐的字明显偏出。
                //    判据（现读，⛔ 不是抄表；本窗这一层只有**高级列**有 `Warning`）：
                //      `python 工具/menu_dump.py bundle_menus_assets_all "Campaign Reward Window" --depth 12`
                //      ⇒ `Warning      … 'Warning or Tip' 字号=47.5 对齐=Center/Middle` ⇒ **Center**
                //         `Point Count  … '100'           字号=32.3 对齐=Left/Midline`   ⇒ **Left**（见下面 `align: 1`）
                //         `Claimed Text … 'Change Deck'   字号=35.0 对齐=Center/Middle` ⇒ **Center**（该行没传 align，正好对）
                //    ⇒ 删掉那个 `2`（`align = 0` = 居中 = 原版）。
                Text(_warn, wr, TxtPremWarning, Color.white, "WarningText", WarnFont, QUnlockText,
                     wr.W, WarnAutoMin, autoMaxPx: WarnAutoMax, autoBasePx: WarnAutoBase);   // A446：补原版 30/36
                // 🔴 **2026-10-13（A402 ①）：这一句原来是无条件 `SetActive(false)`** —— 全仓没有第二处开它
                //   ⇒ 锁定支下 `Warning` **永远不显示**，而原版那一支正是**唯一**显示它的地方。
                //   判据（`SetPremiumButton.c` 三支逐句）：`:81` 锁支 `SetActive(premiumWarning, **1**)` ·
                //   `:32` 不锁支 `SetActive(…, 0)` · `:120` 已领支 `SetActive(…, 0)`。
                //   ⚠️ 锁支里紧跟的 `set_text(premiumWarning, premiumUnlockWarning)`（`:97-107`，只有锁支设）
                //   用的就是 `TxtPremWarning` 那一条词条的占位（见它的注释）⇒ 文案不用改。
                _warn.gameObject.SetActive(lockedHere);
                // `Badge`：`N(7, 0,1, 0,1, 0,1, 0,0, 100,100)`
                //   ⇒ `anchorMin = anchorMax = (0,1)` · `pivot (0,1)` · `pos (0,0)` · `100×100`
                //   ⇒ **holder 的左上角**（**连 `PadL`(30) 都没加** —— 布局组摆不出这个位置）。
                var gr = UguiRect.Child(hr, UguiRect.A01, UguiRect.A01, UguiRect.P01,
                                        Vector2.zero, new Vector2(BadgeSize, BadgeSize));
                _badge = MenuDraw.Node(holder, "Badge", gr);
                DrawRect(_badge, Art(ArtBadge), gr, "img", QBadge, null, true);   // A182：吃视口裁切
            }
        }

        // ⚠️ `Warning` 的宽**不是常量**（`= 0.8 × holder 宽 + 30`，锚点算出来）——
        //    原来这里是一个 `const float WarnW = 300f`，A537 已删（判据与更正的留档见 `WarnAnchorMin`）。

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
            DrawRect(pi, Art(ArtPointIcon), ptRect, "Icon", QUnlockPt, null, true);   // A182：吃视口裁切
            pi.gameObject.SetActive(false);

            // `Point Count`：N(8, .5,0, 1,1, 0,.5, 5,0, −5,−14.3902)
            var costR = UguiRect.Child(r, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
                                       new Vector2(5f, 0f), new Vector2(-5f, -14.3902f));
            // A446：`autoMax`/`base` = 原版 `Point Count` 那颗的 `32 / 12`（判据见上面那组常量）
            // 🔴 **2026-10-13（A635）**：补 `align: 1`（**左对齐**）—— 原版那一颗的 TMP **`对齐=Left/Midline`**
            //    （`m_HorizontalAlignment = 1` = `HorizontalAlignmentOptions.Left`），我们原来没传
            //    ⇒ `Label` 默认把文字**居中**在框心（= `align = 0`）。判据 / 两套枚举的对照见上一段。
            //    ⚠️ 用**命名实参** `align:`（⛔ 别把它插到 `autoMaxPx` 前面当位置实参）——
            //    见 `WindowsManager.Text` 的形参次序那一段：`int` 字面量能隐式转 `float`，位置一错就**静默**绑错。
            var cost = Text(btn, costR, "", Color.white, "Point Count", CostFont, QUnlockText, costR.W, CostAutoMin,
                            autoMaxPx: CostAutoMax, autoBasePx: CostAutoBase, align: 1);
            // `Claimed Text`：N(8, 0,0, 0,0, .5,.5, 0,0, 0,0) —— 零尺寸锚点，文字自己撑开 ⇒ 用整个按钮当框
            // （`Label` 建出来就是 pivot (.5,.5) 居中在锚点上 ⇒ 「居中」不用再调对齐）
            // A446：`autoMax`/`base` = 原版 `Claimed Text` 那颗的 `35 / 12`（判据见上面那组常量）
            var claimed = Text(btn, r, "", Color.white, "Claimed Text", ClaimedFont, QUnlockText, r.W, ClaimedAutoMin,
                               autoMaxPx: ClaimedAutoMax, autoBasePx: ClaimedAutoBase);

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
            // 🔴 A182：命中区也吃视口裁切（原版那颗 `RectMask2D` 是 `ICanvasRaycastFilter` ⇒ **框外的点
            //   判不中它**）。⚠️ 整块在视口外 ⇒ `DrawRect` 返回 null ⇒ 这颗 `WindowButton` 上就没有 quad，
            //   `PointerLayer` 自己会跳过它（`HitQuad` 拿不到 quad ⇒ 这一件点不动）——与「画不出来」一致。
            DrawRect(hit, CardArt.Solid(), r, "HitBox", QUnlockBg, new Color(0f, 0f, 0f, 0f));
            var wb = hit.gameObject.AddComponent<WindowButton>();
            // 🔴 **2026-10-12（A466）：可点性补上「付点解锁」那一支的 `ctx.Claimable`**（原来**恒定可点**）。
            // 判据 = 反编译方法体 `CampaignRewardsWindow__SetBaseButton.c`（**本件亲读**，两条支各一句）：
            //   · `*(int *)(ctx + 0x30) < 1`（= `PointCost < 1`，`:38`）⇒ `SetAsFreeClaim(btn, **1**)`（`:40`）
            //     —— 那个 `1` 直落 `Selectable.set_interactable`（`CampaignUnlockButton__SetAsFreeClaim.c:15`：
            //     第 2 个形参 `param_2` 就是 `interactable`）⇒ **「免费领」那一支硬编码真、不看 `Claimable`**；
            //   · `else`（付点那一支，`:68-84`）⇒ `set_interactable(*(btn + 0x38), *(ctx + **0x2a**))`
            //     —— `0x2a` 就是 `Claimable`（`CampaignRewardsWindowContext__get_Claimable.c:4`）。
            // ⇒ 照原文写成 `(ctx.PointCost < 1 || ctx.Claimable)`。⛔ **别简化成「一律 `&& ctx.Claimable`」** ——
            //    那会把免费领那一支也改掉，而原版那一支**根本不看这个字段**。
            // ⚠️ **影响面如实说**：本战役 47 个节点的 `Cost` 全在 **100…1100**（`Shell/CampaignData.cs` 的 `Nodes` 表）
            //    ⇒ 免费那一支**今天在真数据下走不到**（只有自检夹具会喂 `PointCost = 0`）；真正被这一改影响的是
            //    付点那一支：`Claimable == false`（点数不够 / 状态已是 20/30）时**不再点得动**。
            // ⚠️ **原版【高级】那颗钮是另一套**（`SetPremiumButton.c:40`：`SetAsFreeClaim(btn, ctx.BaseCollected)`）
            //    —— ⛔ **不是「原版没有」**（出处：`资料/普查产出_1012/H31_收件箱一族.md` §顺手发现）。
            //    🔴 **2026-10-13（A472）就地更正**：这里原来接着写「**那一条本件没做**」—— **已过期**，
            //    那一条**就是下面这一段**（当天落的地）。
            // 🔴 **2026-10-13（A472）：高级列那颗钮是【另一套门】—— `BaseCollected`（「基础档领了才点得动」）。**
            //   判据（第一权威 = 反编译方法体）：
            //   · `CampaignRewardsWindow__SetPremiumButton.c:22-40`（「未领 ∧ 未锁」那一支）末句 =
            //     `CampaignUnlockButton__SetAsFreeClaim(*(param_1 + 0x88), *(undefined1 *)(ctx + 0x28))`；
            //     `0x28` = **`BaseCollected`**（getter `CampaignRewardsWindowContext__get_BaseCollected.c`；
            //     字段偏移与 `dump.cs` 的 `CampaignRewardsWindow` 一节逐位对上）。
            //   · `CampaignUnlockButton__SetAsFreeClaim.c:15`：第 2 个形参直落
            //     `UnityEngine_UI_Selectable__set_interactable(btn + 0x38, param_2)` ⇒ **它 = `interactable`**。
            //   ⚠️ 那一支里 **`ctx+0x30`(PointCost) 与 `ctx+0x2a`(Claimable) 一次都没被读**（本件逐字段核过
            //     `SetPremiumButton.c` 全文：只出现 `0x29`/`0x10`/`0x28`/`0x88`/`0x90`/`0x98`/`0xb0–0xc0`）
            //     ⇒ 高级列**只看 `BaseCollected`**，⛔ 别把 A466 那套（基础列的 `PointCost`/`Claimable`）搬过来。
            //   ⚠️ 对照基础列那颗：`SetBaseButton.c:40` 是**硬编码 `1`**（免费支，不看 `Claimable`）、`:84` 才是
            //     `ctx.Claimable`（付点支）—— **两列的门本来就不同**，这是原版的区别、不是笔误。
            //   📌 **原版那条张力的解释**（查清，⛔ 别顺手改 `CampaignData.StateOf` / `Claim` 的建模）：
            //     非 Repeatable 节点领完基础档后 state = 20，此时高级钮**可点**、而`CampaignNode.Collect` 的闸
            //     只放行 `state ∈ {10,40}`（`CampaignNode__Collect.c:29-33`）⇒ **点了领不到**（原版那一支只打一条
            //     `Debug.Log`）。47 个节点里只有 UM47 是 `IsRepeatable` ⇒ **原版对这 46 格就是这个行为**。
            //     我们这一侧同形：`CampaignData.Claim` 也要求 `Claimable`（= `StateOf ∈ {10,40}`）⇒ 点了会
            //     出声「这一格的基础档已经领过了」且**不关窗**（`OnUnlock` 只在成功时 `Close()`）。
            bool clickable = !premiumLocked && !claimedState
                             && (isBase ? (ctx.PointCost < 1 || ctx.Claimable) : ctx.BaseCollected);
            wb.onClick = () => { if (clickable) OnUnlock(isBase ? CampaignData.TierBasic : CampaignData.TierPremium); };
            wb.Bind(bgQ, ArtUnlockBtn);   // A17：悬停换图（常态图 `UI_Button_Mulligan` → `_hover`）
            // 🔴 **2026-10-12（A466）：「`interactable`」那一半（变灰 + 挡派发）也落地** —— 原版那一句是
            //   `Selectable.set_interactable`，**两半**一起生效（`DoStateTransition(Disabled)` 换灰材质 +
            //   `Selectable.OnPointerClick` 头一句直接返回）；我们这套的最小等价物 = `WindowButton.Interactable`
            //   （`Shell/PromptPopup.cs:939`：setter 走 `RefreshGray()`、`Click()` 头一句
            //   `if (!_interactable) { 出声; return; }`）。⇒ 「该灰的时候不灰」那条症状（A 表原话）修在这一句上。
            //   ⛔ **必须排在 `Bind` 【之后】**：`GrayTargets()` = `target` + 子树（`Shell/PromptPopup.cs:968`），
            //     而 `target` 是 `Bind` 才设成那颗**可见的**底图 `bgQ` 的 —— 排在 `Bind` 之前的话，头一次
            //     `RefreshGray()` 只灰到那颗**透明的** `HitBox`，而 `GrayedForTest` 随即为真 ⇒ 后面再设也只会早退
            //     ⇒ **画面上一像素都不变**（`RefreshGray` 自己的注释就把「先灰过、`Bind` 之后又灰」列为踩过的坑）。
            wb.Interactable = clickable;
            return new BtnLabels { claimed = claimed, cost = cost };
        }

        static void SetText(Label lb, string s) { if (lb != null) lb.SetText(s ?? ""); }

        /// <summary>`UnlockClicked(RewardTier)` → `context.OnCollect(tier)`（原版原样透传）。
        /// <para>🆕 **2026-10-12（A448）：「关窗三条路」的第 ① 条 —— 领到 ⇒ 关窗**。
        /// 正本 = `资料/阶段二_锻造厂与战役页_原版规格.md:646`「① 领取成功后 `CloseWindow&lt;CampaignRewardsWindow&gt;()`
        /// （唯一主动关）· ② ESC · ③ 点暗底」—— 本件之前**只做了 ② 与 ③**
        /// （本文件唯一的 `Close()` 在压暗层那颗 `BackgroundHit` 上）。**现在 ① 也做上了。**</para>
        /// <para>判据（**第一权威 = 反编译方法体**，本件亲读）：
        /// · **`CampaignRewardsWindow` 自己不关** —— `d:/2/tools/decomp_full/CampaignRewardsWindow__{Open,OnEnable,
        ///   OnDisable,ESCPressed,UnlockClicked,SetBaseButton,SetPremiumButton,ConfigureIsPreviewState,CenterHolder}.c`
        ///   里 **`CloseWindow` 零命中**；`UnlockClicked` 只把 tier 透传给 `context+0x18` 那个委托（`Action&lt;int&gt;`）。
        /// · **关窗在领取成功回调那一拍** = `CampaignWindowTab.&lt;TryCollect&gt;g__Refresh_0`
        ///   （`decomp_full/CampaignWindowTab.__c__DisplayClass24_0___TryCollect_g__Refresh_0.c`，顺序照原文逐句）：
        ///   `CampaignHeaderDisplay.Initialize` → `ArmySelector.Refresh` → **`CampaignNode.Collect(id, tier)`**
        ///   → **`WindowsManager.CloseWindow&lt;CampaignRewardsWindow&gt;()`**。</para>
        /// <para>⇒ 两条口径**照实读**，别顺手加条件：
        /// ① **不是「点了就关」** —— 失败那一支不关（`Everguild.LiveOps.Campaign.__c__DisplayClass11_0__
        ///   _CollectRewards_g__OnFail_11_1.c` 里没有关窗这一跳）⇒ 用回调带回来的 `bool` 把关门。
        /// ② **不是「两档全领完才关」** —— 上面那个回调里**没有任何**「另一档也领了」的判据
        ///   ⇒ **每领到一档就关一次**（另一档要再点一次节点把窗开回来；`CampaignNode.Collect` 会把节点从
        ///   `10/40` 改写成 `20/30`，我们的 `CampaignData.StateOf` 同形 ⇒ `Claimable` 随之为假）。</para>
        /// <para>⚠️ **落点在窗里（如实标注，⛔ 不是「原版就是这样」）**：原版那一句在**调用方**
        /// （`CampaignTab` 的领取回调）里；我们把它放在窗里，因为 `Shell/CampaignTab.cs` **不在本件白名单**
        /// （本件只能碰三扇窗）。两者**可观测行为一致**。若将来有人接手 `CampaignTab.cs`，
        /// **更贴原版结构**的等价写法是在 `ClaimForTest` 的 `if (…Claim…)` 支里补一句
        /// `_win.Manager.CloseWindow&lt;CampaignRewardWindow&gt;()`，并把这个 `Func` 收回 `Action`。</para>
        /// <para>**关窗次序**（原版同此）：原版是 `RewardService.Collect(...)` **先**开领奖窗
        /// （调用点 `Everguild.LiveOps.Campaign.__c__DisplayClass11_0___CollectRewards_g__OnSuccess_0.c:27`），
        /// **再**由 `onCollected` 回调关本窗 ⇒ 这里也是先 `OnCollect`（内含 A438 的
        /// `RewardWindow.ShowCollected`）**再** `Close()`。那一刻本窗已被新开的领奖窗压到 `Background`
        /// ⇒ `Close()` 走的**不是**「带上一扇回来」那一支（`WindowsManager.NotifyClosed` 只在
        /// 「关掉的正是最上面那扇」时才调 `ShowPreviousWindow`）—— 与 `Editor/RewardsScene.cs` 里
        /// 那两条「关掉领奖窗 ⇒ 底窗回 `Open`」的夹具**不冲突**（那时关的是领奖窗、不是本窗）。</para></summary>
        void OnUnlock(int tier)
        {
            if (_ctx == null || _ctx.OnCollect == null) return;
            // ← 原版 `g__Refresh_0` 末尾那一跳；`bool` = 「这一次真的领到了」（判据见方法头）。
            if (_ctx.OnCollect(tier)) Close();
        }

        // ============================================================ 物品格

        /// <summary>一个奖励物品格。**走抽屉库**（`Shell/ItemDrawer.cs`）——
        /// 原版这一句就是 `ItemDrawer.Draw(holder, item, quantity, DrawerOverride.Default)`
        /// （`CampaignRewardsWindow__Open.c:134`；那 4 步见抽屉库的文件头）。
        /// <para>抽屉库里**有判据的**：野牌那四层（卡面底图 + 阵营徽记 + 阵营名 + 数量）·
        /// 「取不到图就不画」·「判据空 ⇒ 占位板」。
        /// **我们挑的**：格子尺寸（`ItemW/ItemH`）· 抽屉内部每层的版式（🔴 **2026-10-03 更正（A86）**：
        /// 原来括注「抽屉 prefab 本地没有」—— **假的**，prefab 整棵 dump 得出来；
        /// 真实原因是**我们还没照 dump 出来的几何改**，见 `Shell/ItemDrawer.cs` 文件头 ②）·
        /// 占位板的底色与短名。</para>
        /// <para>数量的值用原版的 `quantity`（`RewardInfo.quantity`），**不是我们编的**。</para></summary>
        void BuildItem(Transform parent, PxRect r, CampaignData.RewardSpec spec)
        {
            var item = ItemDrawer.Spec(spec.Id, CampaignData.ItemIcon(spec.Id), CampaignData.ItemShortName(spec.Id));
            var st = ItemDrawerStyle.Default(QItem, QItemIcon, QItemIcon);
            st.IconFill = ItemIconPx / Mathf.Min(ItemW, ItemH);            // = 0.7（值只有这一处，见 `ItemIconPx` 的注释）
            st.NodeName = "Item_" + CampaignData.ItemShortName(spec.Id);   // 自检按 `Item_` 前缀数格子
            // 🔴 **2026-10-08（A182）**：原来这里是 `st.Clip = null`（配着那句「本窗的 `Scroll View` 没做裁剪」）。
            //   现在给 **`RenderClip`**（= 视口矩形按 `ClipPad` 内缩；本处 pad = 0 ⇒ 就是视口）——
            //   抽屉里那四层（卡面底/阵营徽记/阵营名条/数量）里凡是整块落在视口外的**不建**、压在边上的**截**。
            //   🆕 **2026-10-11（A238）：软边也接上了** —— `ItemDrawerStyle` 加了 `ClipSoftness`，
            //   抽屉那四条画路（`Shell/ItemDrawer.cs` 的 `:793/:816/:831/:843`）逐条透传给
            //   `MenuDraw.Rect` 的 `clipSoftness`。原来只有 `Clip` ⇒ **同一个视口里列底/按钮是 `(200,0)` 渐隐、
            //   抽屉四层却是硬边截**（波 C1 报告 §四·1）。这里给的就是本窗那一份 `ClipSoftness`
            //   （`BuildColumn` 拿捏期间 = `ScrollSoft` = **(200,0)**，原版 `RectMask2D.m_Softness` 实读值）
            //   —— 与 `st.Clip` 取同一时刻的窗口状态，**成对**。
            //   ⚠️ **2026-10-11 就地订正（W-A · A314 · 铁律 5）**：这两行原写「**仍不吃裁切的**：抽屉里那三层
            //   **文字**（`MenuDraw.Text` 没有裁切形参，本库也没走 `MenuDraw.ClipText`）—— 那是**另一件欠账**」
            //   —— **已过期**：那一件（**A302**）**2026-10-11 就做完了** ⇒ 抽屉里那三层文字现在**与图吃同一份**
            //   `Clip` / `ClipSoftness`（`Shell/ItemDrawer.cs` 新增唯一一份 `ClippedText`，三个调用点全改走它；
            //   数量那处顺带把「先裁再挪」的次序倒了过来）。错因：这句是 **2026-10-08（A182）** 写 `RenderClip`
            //   时留的「本件没顺手加」，A302 落地后**没人回来销它**（改按内容认 ⇒ `Shell/ItemDrawer.cs` 里
            //   `ClipSoftness` 那段 doc 与 `ClippedText` 的方法头，⛔ 不写行号）。
            //   🔴 **2026-10-14（A797）再补一笔（铁律 5）**：上面引的那句话里还有半句**双料过期** ——
            //   「`MenuDraw.Text` **没有裁切形参**」。其中「A302 绕开它」只是**一半**：A781（**2026-10-13**）
            //   **把形参本身补上了** —— `MenuDraw.Text` / `TextBox` 现在各带 `clip` / `clipSoftness`、
            //   走同一份 `ViewportClip.Resolve`（A798 起还多一道「整块在框外 ⇒ 连节点一起不建」的闸）。
            //   ⇒ 本窗那几条文字走的仍是 `ItemDrawer.ClippedText`（它自己那层收口没变），
            //   但**判据的出处**从此是那两处共用件，⛔ 别再照抄「没有裁切形参」这句。
            // 🔴🔴 **2026-10-13（A435 甲 · A198② 阶段 2 · B8）：这两行【派生写入】没了。**
            //   旧写法 = `st.Clip = RenderClip; st.ClipSoftness = ClipSoftness;` —— 把**窗级**已解析的
            //   状态**再喂给**下层载体（`ItemDrawerStyle`）。迁到「状态长在视口节点上」之后：抽屉那几条
            //   画路自己会 `ViewportClip.Resolve(parent, st.Clip, st.ClipSoftness, …)`，而 `st.Clip`
            //   缺省就是 `null` ⇒ **沿父链解析**到 `Content/Scroll View/Viewport` 那颗节点
            //   （这些格挂在 `Base Rewards/Rewards` / `Premium Rewards/Rewards` 之下）⇒ 拿到的是
            //   **同一份** pad/soft（`(0,0,0,0)` / `(200,0)`）。
            //   ⚠️ 同 B9/B10：`ItemDrawerStyle.Clip` 这个字段**留着**（= 显式覆盖的口子，
            //   同 `MenuDraw.Rect` 的 `clip` 形参）—— 唯一还用它的是 `Editor/RewardsScene.cs` 的 A302 探针。
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
        /// ⇒ 走的是「不锁」那条。
        /// 🔴 **2026-10-13（A402）就地更正（铁律 5）**：这句原来接着写「**两条都实现了**」—— **是半假的**：
        /// 锁定支当时既没显 `Warning`（那句是**无条件** `SetActive(false)`）、按钮也只是**变灰**。
        /// **现在两条真的都实现了**（判据逐句 = `SetPremiumButton.c:81` 显警告 · `:85` 藏整颗按钮 ·
        /// `:97-107` 设警告文案），落点在 `BuildColumn` 的 `lockedHere` 那**两**处（`Warning` 开合 / 按钮 `SetActive`）。
        /// 🔴 **2026-10-13（A537）就地更正（铁律 5）**：本行原文写的是「那**三**处（**宽度表** / `Warning` 开合 /
        /// 按钮 `SetActive`）」—— **「宽度表」那一处已不存在**：A537 查出那三颗件全都
        /// `LayoutElement.m_IgnoreLayout = 1` ⇒ **任何支下都不进 holder 的布局表**（判据见 `BuildColumn`）。
        /// 📌 真数据下仍走「不锁」那条（`IsPremiumLocked` 恒 `false`）⇒ 锁定支**只能靠自检夹具到达**
        /// （这正是它必须有断言咬着的理由，见 `WC1_战役奖励窗.md` §五）。</summary>
        public bool PremiumLocked { get { return _ctx != null && _ctx.IsPremiumLocked; } }

        // ============================================================ 小工具

        void SetRect(Transform t, PxRect r)
        {
            t.localPosition = MenuDraw.Local(t.parent, r.x1, r.y1, r.x2, r.y2);
        }

        /// <summary>`MiddleCenter`：子件在 holder 的**内容区**里垂直居中（pad T25 B85）。
        /// 🔴 **2026-10-13（A537）：只有物品走它了** —— 那三颗件改走锚点（见 `BuildColumn`）。
        /// ⚠️ 「内容区」= `[holder.y1 + PadT, holder.y2 − PadB]` = **320..825**；三颗件**不在**这个区间里
        /// （按钮/警告底边在 895、徽标顶边在 295）—— 这正是「物品竖向居中那一半本来就是对的、
        /// 另三颗件全错」的由来。</summary>
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

        // ⚠️ **本窗原来在这里就地抄了一份 `Text(...)` 薄包装**（收「整块在视口外 ⇒ 不建 / 压在边上 ⇒ 裁 /
        //    对齐要在裁之前」那三步）。🆕 **2026-10-11（A241）已上移到共同基类 `GameWindow.Text`**
        //    （`Shell/WindowsManager.cs`）—— 本文件的**调用点一个都没改**（包括 `WarningText` 那条传 `align`
        //    的：那个形参现在就在基类签名里）。⛔ 别在本文件再抄回来（判据 → `资料/待办判据_1008.md` §A241）。

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
