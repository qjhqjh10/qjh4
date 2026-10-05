// RewardWindow.cs — 原版 **`Reward Window`**（独立的全屏「领奖 / 预览」窗；类 `RewardWindow : GameWindow`）
//
// ============================ 出处（唯一正本） ============================
//  · 树 / 矩形 / 图 / 字号 / 对齐 / 颜色：`工具/menu_dump.py bundle_menus_assets_all "Reward Window" --depth 8`
//    （本文件里**每一个数**都是它实读的）+ 按 **PathID** 逐节点复核（`GameObject/*.json` 的 `m_Component`
//    ↔ `RectTransform_<pid>.json` ↔ `Transform` 父链 —— 名字在解包目录里**不是唯一的**：
//    `Content` 有 436 份、`Button Text` 447 份、`Tap To Continue` 4 份 ⇒ **按名字取一定会取错一份**）。
//  · 根组件 19 个字段：`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-1684643839753787023.json`
//    （`type=1(Popup)` · `windowsPlacement=15(Popup)` · `closeOnESC=1` · `updateNavPanel=0` ·
//     `extraScaleSmallScreen=1.0` · `animTime=0.8` · `openSound`/`closeSound` = null + `useDefaultCloseSoundIfNull=1`）。
//  · 驱动（`d:/2/tools/decomp_full/`）：`RewardWindow__{Open,Close,OnCollectClicked,ConfigureIsPreviewState,
//    FixLayoutSize,DoRewardAnimation,DelayedParticlePlay}.c` · `RewardWindowContext__*.c` ·
//    `RewardService__{Collect,Preview}.c` · 字段表 `d:/2/tools/il2cpp_out/dump.cs:94553-94686`。
//  · 数值字面量：`工具/read_literal.py d:/2/unity_run_ref/GameAssembly.dll <地址>`（见 `MaskPadWide` 那一组）。
//
// ---- 🔴 字段 → 节点（19 个 `[SerializeField]` 逐个按 PathID 解出来，⛔ 不是按名字猜的）----
//   `listHolder`             0x70 = `Content/Scroll View/Viewport/Content`（**物品抽屉挂它下面**）
//   `closeButton`            0x78 = `Menu Dark Background`（那颗 `BackgroundCloseButton` 在**压暗层**上）
//   `collectButton`          0x80 = `Collect Button`          `premiumWarning`   0x88 = `Premium Disclaimer`
//   `tapToContinueText`      0x90 = `Tap To Continue`         `getRewardBackground`0x98 = `Reward Background Get Reward`
//   `getRewardText`          0xA0 = `Glow Get reward`         `previewRewardBackground` 0xA8 = `Reward Background Preview Reward`
//   `previewRewardText`      0xB0 = `Glow Preview reward`     `claimRewardParticles`    0xB8 = `Reward Claim`（3D 粒子）
//   `mask2D`                 0xC0 = `Viewport` 的 `RectMask2D`（**soft (200,0) · pad (0,0,0,0) · en=1**）
//   `scrollRect`             0xD0 = `Scroll View` 的 `ScrollRect`（**h=1 v=0 mode=1(Elastic) inertia=1 · 0.1 · 0.135**）
//   `particleOnAppear`       0xD8 = `RewardAppearParticle`（**不在窗口树上** —— 运行期实例化的模板）
//   `animTime`               0xC8 = **0.8**；`soundOnAppear` 0xE0 = 外链 AudioCue；`drawers` 0xE8 = 运行期 List
//   🔴 `getRewardText` / `previewRewardText` 名字像「文字」、其实是**那团底光**（`Text *` 是它的子件、跟着开关）
//
// ---- 🔴 入口（谁开这扇窗）—— 查到了 ----
//   `RewardService.Preview(IReadOnlyList<RewardInfo> rewards, Action<…> onClose,
//                          IReadOnlyList<RewardInfo> collectedRewards, bool isPremiumLocked = true,
//                          string customTitle)` → `new RewardWindowContext(…, isPreview: 1)` →
//                          `WindowsManager.OpenWindow<RewardWindow>(ctx)`（`RewardService__Preview.c` 全文）。
//   `RewardService.Collect(rewards, showAnimation = true, onCollected, collectedRewards,
//                          isPremiumLocked = true, customTitle, showXpToast = true)` 同理
//   （领奖流程的**公共出口**；`isPreview: 0`）。
//   🔴 **2026-10-11（批次1 · W1）就地订正（铁律 5）**：原先这一行把两个签名都写错了 ——
//     · `Preview` 的第 2 个形参是 **`onClose`**（不是 `onCollect`），第 5 个是 **`customTitle`**（不是 `onPremiumUpgrade`）；
//     · `Collect` 多一个 `showAnimation`（**为假就不开窗**）与 `showXpToast`。
//     依据 = `d:/2/tools/il2cpp_out/dump.cs` 的 `RewardService` 方法表 + 两个 `.c` 里 ctor 的实参逐位对齐。
//   🆕 **接线做了（A309）**：本类现在提供 `Show(ctx)` / `ShowCollected(...)` / `ShowPreview(...)` 三个静态入口
//     （= 那两个服务方法的等价物，判据与「为什么落在窗类上」见 `Show` 上面那一段）。
//     真调用点：**日常线领奖** —— `Shell/DailyData.cs` 的 `CollectReward` / `CollectStreak` 走 `ShowCollected`。
//     ⚠️ 还欠别处：开包（`BoosterPackOpenWindow`）/ 锻造 / 战役节点 在原版也走 `RewardService.Collect`
//     （调用点清单见报告），那几处**不在本件白名单**里 ⇒ 如实记着（不是静默省略）。
//
// ---- 🔴 一个值 ≠ 全部情况（铁律 5·c）：本窗有 **三组** 状态，任一都别当默认 ----
//   ① **`IsPreview`**（`ctx` 的 `0x19`）：显 `Preview` 那套底图/底光、**隐** `Tap To Continue`、
//      **不播**开场揭示动画、粒子不播（`RewardWindow__Open.c` 的 `:189` / `:205-217` / `:280` / `:283` 读的全是 `0x19`）；
//      🔴 **2026-10-11（批次1 · F1）就地订正（铁律 5）**：这半句原来写「**显** `Tap To Continue`」—— **写反了**。
//      判据 = `RewardWindow__Open.c:189` 实读 `SetActive(tapToContinueText, *(char *)(ctx + 0x19) == '\0')`
//      ⇒ **只有非预览态**（`0x19 == 0`）才 `SetActive(true)`；本文件 `Build()` 第 ⑦ 段（`_tap.SetActive(!ctx.IsPreview)`）
//      与它一致 —— 原话是「`Open()` 按 `!IsPreview` 打开」，写反的只是这两处注释。
//   ② **`IsPremiumLocked`**（`0x18`，ctor 默认 **true**）：**只多管一件** —— `Premium Disclaimer` 的显隐
//      （`IsPremiumLocked && 有高级档奖励`，`:164-185`）；
//   ③ `OnCollect != null`：`Collect Button` 整颗建不建（`:194`）。
//   🔴 **2026-10-11（批次1 · F7）就地订正（铁律 5）**：①② 原来**写反了**（① 写成 `IsPremiumLocked`、
//      ② 写成 `IsPreview` 只多管 `Premium Disclaimer`）—— 判据逐条：
//      · `RewardWindowContext__.ctor.c:51-52`：**`0x19 ← param_9 = isPreview`**、**`0x18 ← param_3 = isPremiumLocked`**
//        （形参表 = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/RewardWindowContext.cs` 的 ctor 签名，逐位对上）；
//      · `RewardWindow__Open.c` 里每个字段各自读的偏移（上面每个件都标了行号）。
//      ⇒ 两个 flag 在自检的三档夹具里**恒等**（`false,false` / `false,false` / `true,true`）⇒ 反了也**看不出来**，
//        一到「locked 但非 preview」就会**静默画错**（判别档见 `Editor/RewardsScene.cs` 第 ⑧·b 段）。
//   ⚠️ **出厂 prefab 的 active** 是第三组之外的另一个状态：`Reward Background Get Reward` / `Glow Get reward` 出厂 ACT、
//   `Reward Background Preview Reward` / `Glow Preview reward` 出厂 INACT —— 但 `Open()` **一定会重设这四个**
//   ⇒ 出厂值只在「没人调 Open」时有意义。
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>开这一窗要带的东西 —— **照原版 `RewardWindowContext` 的 9 个字段**（名字与顺序逐条对过 `dump.cs`）。
    /// 五件**我们这边不画**的如实标注在各自字段上。</summary>
    public class RewardWindowContext
    {
        /// <summary>0x38 `Rewards` —— 要展示的奖励。原版 `Open()` 里**按 `RewardTier` 升序**排一遍再逐个画
        /// （**2026-10-11（批次1 · W1）已坐实**：那个 LINQ 助手 = `Enumerable.OrderBy&lt;object,Int32Enum&gt;`，
        /// 判据见 `OrderByTier`）；⛔ **本字段本身保持原序**（原版两个回调带出去的就是它）。</summary>
        public CampaignData.RewardSpec[] Rewards;
        /// <summary>0x18 `IsPremiumLocked`（**原版 ctor 的默认值就是 `true`**）—— 「高级轨拿不到」那一态。
        /// 🔴 **它只管一件**：`Premium Disclaimer` 的显隐（`IsPremiumLocked && 有高级档奖励`，
        /// `RewardWindow__Open.c:164-185` 读 `0x18`）。
        /// ⛔ **别拿它当「预览态」的开关**（2026-10-11（批次1 · F7）按反编译订正 —— 原来这里写「拿不到那一态」，
        /// 读起来像是它管着整套 Preview 底图/`Tap To Continue`，那全是 `0x19` 的事）。</summary>
        public bool IsPremiumLocked = true;
        /// <summary>0x19 `IsPreview` —— **预览态**：显 `Preview` 那套底图/底光、**隐** `Tap To Continue`、
        /// **不播**开场揭示与粒子（`RewardWindow__Open.c:189/205-217/280/283` 读的全是 `0x19`）。
        /// ⛔ `Premium Disclaimer` **不归它管**（那一条读 `0x18` = `IsPremiumLocked`）。
        /// 🔴 **2026-10-11（批次1 · F1）就地订正（铁律 5）**：上面那半句原来写「**显** `Tap To Continue`」—— **写反了**。
        /// 判据 = `RewardWindow__Open.c:189` `SetActive(tapToContinueText, ctx+0x19 == '\0')`
        /// （字段名与偏移：`dump.cs:94565` `tapToContinueText // 0x90`）⇒ 预览态**不显**它。</summary>
        public bool IsPreview;
        /// <summary>0x10 `CustomTitle` —— 非空时**覆盖**标题那两段 TMP 的本地化词条（原版走 `I2_Loc.Localize.set_Term`）。</summary>
        public string CustomTitle;
        /// <summary>0x20 `OnPremiumUpgrade` —— 高级轨解锁回调。⚠️ **我们不接**（我们口径下高级轨恒解锁，
        /// 同 `CampaignRewardWindow.PremiumLocked` 那条）；字段留着，⛔ 别把它当成「做完了」。</summary>
        public System.Action OnPremiumUpgrade;
        /// <summary>0x28 `OnCollect` —— 点 `Collect Button` 的回调（参数 = `Rewards`）。
        /// 🔴 **为 null ⇒ 那颗按钮整颗 `SetActive(false)`**（原版 `Open()` 里那一句）。</summary>
        public System.Action<CampaignData.RewardSpec[]> OnCollect;
        /// <summary>0x30 `OnClose` —— 关窗回调（参数 = `Rewards`，**不是** `CollectedRewards`；`Close()` 里那两句逐字读的）。</summary>
        public System.Action<CampaignData.RewardSpec[]> OnClose;
        /// <summary>0x48 `CratePrefab` —— 3D 箱子 prefab。⚠️ **本工程这条线不建 3D 体**（同其它窗的口径）。</summary>
        public GameObject CratePrefab;
        /// <summary>0x40 `CollectedRewards` —— ⚠️ 原版**画法上不读它**（只有 `OnClose` 的签名带 `IReadOnlyList` 那条路）；
        /// 我们留字段记账，⛔ 别以为「没实现」= 漏了。</summary>
        public CampaignData.RewardSpec[] CollectedRewards;
    }

    /// <summary>原版 `Reward Window`（pid **13701**；普查产出记的那个 pid）—— **独立的全屏领奖窗**。
    /// 🔴 **别与 `Shell/RewardsWindow.cs` 混**：那一扇是 `Rewards Base Submenu Variant`（日常左栏页签那套宿主），
    /// 两扇窗**没有父子关系**（判据 → `资料/普查产出_1008/波C1_A182_四扇窗裁切.md` §0·1）。</summary>
    public class RewardWindow : GameWindow
    {
        // ============================================================ 矩形（原版 RT 字段 + 锚点链算出来的绝对值）

        /// <summary>`Menu Dark Background`（`MenuWindowBase`/`GameWindow` 家族那层压暗层，比屏幕大得多）。</summary>
        public static readonly PxRect Shade = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);
        /// <summary>`m_Color` 原文 `(0,0,0,0.77254904)`。</summary>
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.772549f);

        /// <summary>`Content`（`N(0,0.5 → 1,0.5) sizeDelta (0,800) pos (0,−25)`）= **0,165 → 1920,965**。
        /// 两态底图 / `Scroll View` / `Title` / `Collect Button` / `Premium Disclaimer` / `Tap To Continue` 全在它下面。</summary>
        public static readonly PxRect Content = new PxRect(0f, 165f, 1920f, 965f);

        /// <summary>`Scroll View`（`N(0,0 → 1,1) sizeDelta (0,−167.469) pos (0,−51.265) pivot (0,0.5)`）
        /// = **0,300 → 1920,932.5**（1920×632.53）。`Viewport` 与它**同矩形**。</summary>
        public static readonly PxRect ScrollView = new PxRect(0f, 300f, 1920f, 932.5f);

        /// <summary>原版 `RectMask2D.m_Softness` = **(200,0)**（x 管左右 ⇒ **只左右渐隐**）· `m_Padding` = **(0,0,0,0)**。</summary>
        public static readonly Vector2 ScrollSoft = new Vector2(200f, 0f);
        public static readonly Vector4 ScrollPad = Vector4.zero;

        /// <summary>`Title`（`N(.5,1) size 600×75 pos y −28.3 pivot (.5,1)`）= **660,193.30 → 1260,268.30**。</summary>
        public static readonly PxRect Title = new PxRect(660f, 193.30f, 1260f, 268.30f);
        /// <summary>两团底光（`Glow *`，`N(.5,.5) size 600×75 pos y **+5.92**`）= **660,187.38 → 1260,262.38**。
        /// ⚠️ 比 `Title` **高 5.92px**（原版就是这么摆的）。</summary>
        public static readonly PxRect Glow = new PxRect(660f, 187.38f, 1260f, 262.38f);

        /// <summary>`Collect Button`（`N(.5,0) size 245×75 pos y **+25**`）= **837.5,902.5 → 1082.5,977.5**
        /// （下沿 977.5 **超出** `Content` 的 965 —— 原版如此）。</summary>
        public static readonly PxRect CollectBtn = new PxRect(837.5f, 902.5f, 1082.5f, 977.5f);
        /// <summary>`Button Text` 的**布局后**矩形（原版带 `AspectRatioFitter(宽控高 3.83864)` +
        /// 锚 `0.0355→0.9613` × `sizeDelta −6` ⇒ 宽 **220.81**、高 **57.52`）；
        /// 中心与按钮中心重合（差 0.06px）。⚠️ **ARF 我们不跑** ⇒ 这个矩形是照 dump 写死的（不是算的）。</summary>
        public static readonly PxRect ButtonText = new PxRect(849.19f, 911.18f, 1070.00f, 968.70f);

        /// <summary>`Premium Disclaimer`（`N(.89,.05) size (0,80) pivot (1,.5)`）= 右沿 **1708.8** ·
        /// 竖中 **925**（即 885..965）。它自己带 `ContentSizeFitter(h:PreferredSize)` ⇒ **宽由文字撑**（我们右对齐代替）。</summary>
        public const float DisclaimerRight = 1708.8f, DisclaimerTop = 885f, DisclaimerBottom = 965f;
        /// <summary>`Premium Icon`（`N(0,.5) size 78.545² pos (−39.273,+0.7275)` 在 Disclaimer 里）
        /// = **1630.33,885.00 → 1708.88,963.55**。</summary>
        public static readonly PxRect PremiumIcon = new PxRect(1630.33f, 885f, 1708.88f, 963.55f);

        /// <summary>`Tap To Continue`（`N(.5,0) size 768×80 pivot (.5,1)`）= **576,965 → 1344,1045**。</summary>
        public static readonly PxRect TapToContinue = new PxRect(576f, 965f, 1344f, 1045f);

        /// <summary>`Menu Vignette` —— 铺满整屏的暗角（`m_Color = (0,0,0,0.5803922)`、**没有 sprite**）。</summary>
        public static readonly PxRect Full = new PxRect(0f, 0f, 1920f, 1080f);
        public static readonly Color VignetteColor = new Color(0f, 0f, 0f, 0.5803922f);

        // ---- `Viewport/Content`（= `listHolder`）：**拉伸锚 + `ContentSizeFitter(h:MinSize=120)`**
        //      ⇒ 宽 = `max(120, HLG 的首选宽)`、**水平居中在视口里**（pivot .5 + `anchoredPosition 0`）。
        //      HLG 原文：`m_Spacing 40` · `m_ChildAlignment 4(MiddleCenter)` ·
        //      `pad = (L60, R60, T30, B50)` · `childControlWidth 0 / childControlHeight 1`。
        public const float PadL = 60f, PadR = 60f, PadT = 30f, PadB = 50f, ItemSpacing = 40f;
        /// <summary>`ContentSizeFitter.m_HorizontalFit = 1 (MinSize)` · `m_VerticalFit = 0` ⇒ **最小宽 120**。</summary>
        public const float MinContentW = 120f;

        /// <summary>🔴 **我们挑的**：一个奖励物品格的尺寸。原版这里是 `ItemDrawer.Draw` 从 `ItemDrawerConfig`
        /// 取的**抽屉 prefab**，尺寸在 prefab 里（那批 prefab dump 得出来，但**照它改版式**是另一件待做的活
        /// —— 见 `Shell/ItemDrawer.cs` 文件头 ②）⇒ 与我们别处同一口径取 **200×300**
        /// （判据只有「视口 632.53 高、`padT 30 / padB 50` ⇒ 可用 552.53，300 放得下」）。
        /// ⚠️ **这不是原版值**，别当判据用。</summary>
        public const float ItemW = 200f, ItemH = 300f;
        /// <summary>主图边长（**我们挑的**，同上）= `ItemDrawerStyle.IconFill` 的出处：140 ÷ min(200,300) = **0.7**。</summary>
        public const float ItemIconPx = 140f;

        // ============================================================ 渲染队列
        // 🔴 三组约束（都在 `Editor/RewardsScene.cs` 里有断言）：
        //   ① **高于所有「页」**（`RewardsWindow`/`ForgeTab`/`CampaignTab` 用到 3005…3064）；
        //   ② 压暗层命中区档 **严格低于**本窗内容命中区档（否则「点不动的钮看着像正常工作」）；
        //   ③ 本窗是弹窗、画在别人上面 ⇒ 从 **3130** 起（`CampaignRewardWindow` 占 3110…3123 · `PromptPopup` 3018…3023）。
        public const int QShade = 3130, QContentBg = 3131, QScroll = 3132, QViewport = 3133,
                         QItem = 3134, QItemIcon = 3135, QTitleBg = 3136, QTitleText = 3137,
                         QCollectBg = 3138, QCollectText = 3139, QPremIcon = 3140, QPremText = 3141,
                         QTapContinue = 3142, QVignette = 3143;

        /// <summary>🆕 **2026-10-11（A311）**：抽屉里那三个装饰层（premium 的 `Highlight`/`Blackout`/`Badge`、
        /// converted/ephemeral 的条与字）的**起始队列** —— 依次 `QDecor + 0/+1/+2/+3`（条）+`/ +4`（条里的字）
        /// `/+5`（`AlreadyOwned`）。
        /// 原版靠 **sibling 序**（那几件排在抽屉内容之后 ⇒ 画在上面）；我们照本仓口径用渲染队列分层
        /// （`CLAUDE.md` §三「分层要用渲染队列，不能用 z」）。
        /// 🔴 **上下界是两头夹的**：① **必须 &gt; `QItemIcon`(3135)** —— 否则装饰层被抽屉自己的图盖住；
        /// ② **必须 &lt; `QVignette`(3143)** —— 原版那层暗角是 `Content` 的**下一个兄弟**（画在所有内容之上），
        /// 把装饰层放到它上面会让高亮/角标在整屏压暗里**比周围亮一截**（看得见的错）。
        /// ⇒ 取 **3136**（占 3136…3141）。⚠️ 3136…3141 另几个号是 `QTitleBg`/`QTitleText`/`QCollectBg`/
        /// `QCollectText`/`QPremIcon`/`QPremText` —— 那几颗件与物品行**在屏幕上不重叠**（标题 y 193–268、
        /// 物品行 y ≈465–765），同队列不产生可见冲突（吸收点击那颗 `AbsorbHit` 本来就在 `QCollectBg`，
        /// 而今天物品抽屉的 3134/3135 **已经在它下面**、照样看得见 ⇒ 那颗不遮挡）。</summary>
        public const int QDecor = 3136;

        // ============================================================ 图（名字 = 导入后的文件名；原版原名见括号）
        public const string ArtBgGet = "40k_general_popup_simple_red";             // 原名 `40k_general_popup_simple red`
        public const string ArtBgPreview = "40k_general_popup_simple_greyscale";   // 原名 `… greyscale`
        public const string ArtGlow = "40k_bt_underbutton";                        // 两团底光同一张（Sliced 240,0,240,0 + PA）
        public const string ArtCollectBtn = "UI_Button_Mulligan";                  // Simple；HL/Pressed 见 `WindowButton.Bind`
        public const string ArtPremIcon = "40k_campaign_Premium-icon";             // 301×305

        /// <summary>两态底光的 `m_Color` 原文（**只有色差、同一张图**）。</summary>
        public static readonly Color GlowGet = new Color(0.8018868f, 0.0794322f, 0.0794322f, 1f);
        public static readonly Color GlowPreview = new Color(0.5283019f, 0.3563546f, 0.3563546f, 1f);

        /// <summary>标题两态**的 TMP 原文**（`m_text` 实读；`CustomTitle` 非空时被它覆盖）。</summary>
        public const string TxtGet = "Rewards claimed", TxtPreview = "You will get";
        public const float TitleFont = 50f, TitleAutoMin = 25f;      // auto[25~50]
        /// <summary>`Button Text` 的 `m_text` 与字号（auto[10~40]，`Center/Capline`，折行 0）。</summary>
        public const string TxtCollect = "Collect";
        public const float CollectFont = 40f, CollectAutoMin = 10f;
        /// <summary>`Premium Disclaimer` 的 `m_text`（36px · Right/Middle · 折行 0 · **autosize 0**）。
        /// ⚠️ 原版这一串是 I2 词条（`Localize`），**词条表在远端 CCD、本地没有** ⇒ 我们画的就是 prefab 里这串英文。</summary>
        public const string TxtPremium = "Upgrade to premium to unlock";
        public const float PremFont = 36f;
        /// <summary>`Tap To Continue` 的 `m_text`（55px · Center/**Bottom** · 折行 0 · raycastTarget **0**）。
        /// ⚠️ 原版 `m_fontSizeMin 55 / m_fontSizeMax 11`（**倒过来的**，出厂如此）⇒ 我们不给自适应、就画 55。</summary>
        public const string TxtTap = "Click to continue";
        public const float TapFont = 55f;

        // ============================================================ 开场揭示动画（`DoRewardAnimation`）
        // 逐条照反编译读出来的（**不是我们的近似**）：
        //   `mask2D.padding` 从 `(950,0,950,0)` **线性**收到 `(0,0,0,0)`、时长 = `animTime = **0.8**` 秒
        //   （`DOTween.To(getter=mask2D.padding, setter, endValue=Vector4.zero, animTime)` +
        //    `SetEase(…, 1)` = `Ease.Linear`；OnUpdate 里 `RectMask2D.set_padding`）。
        //   宽屏再加一档：`aspect > 1.77778` 时初始值是 `950 + clamp01((aspect−1.77778)/0.555555) × 300`。
        //   🔴 三个字面量都是**读出来的**（`工具/read_literal.py`）：0x1834b32e0/…e8/…e8/…ec = (950,0,950,0) ·
        //      0x1834b32d0 = 1.77778 · 0x1834b32cc = 0.555555 · 0x1834b326c = 300 · 0x1834b2bb8 = 1。
        /// <summary>`animTime`（prefab 字段 0xC8）= **0.8** 秒。</summary>
        public const float AnimTime = 0.8f;
        /// <summary>非宽屏时遮罩的初始 padding（四个字面量）。</summary>
        public static readonly Vector4 MaskPadWide = new Vector4(950f, 0f, 950f, 0f);
        public const float WideAspect = 1.77778f, WideSlope = 0.555555f, WidePerAspect = 300f;

        // ============================================================ 逐件 punch（`DoRewardAnimation` 的后半段）
        // 🆕 **2026-10-11（批次1 · W1 · A310①）**：这一段原来**没做**（判据齐、只在报告里记着）。
        // 出处 = `d:/2/tools/decomp_full/RewardWindow__DoRewardAnimation.c:110-174`，四个字面量**全是读出来的**
        // （`工具/read_literal.py d:/2/unity_run_ref/GameAssembly.dll …`，**4 字节 float**）：
        //   🔴 **2026-10-11（批次1 · F1）现读复核**：范围原记 `:111-174` —— `uVar8 = DAT_1834b3158`（0.4）在 **`:110`**、
        //      被漏在外面（`:111` 是 0.75、`:113` 才是 0.1、`:114` 是 0.2，与下表逐条对得上），已按实读改正。
        //   · `DAT_1834b3158` = **0.4**（**`:110`**）⇒ `DOPunchScale` 的 **duration**
        //     （同一个地址在 `CardScript__DoPushBack.c:17` 也是 punch 时长 ⇒ 互相印证）
        //   · 立即数 **5**（**`:159`**）⇒ **vibrato**（`DoPushBack` 那处硬编码的是 8；两处都不是字面量池里的）
        //   · `DAT_1834b2bb0` = **0.2**（**`:114`**）⇒ **elasticity**（`DoPushBack` 用的是 `…2dc8` = 0.3）
        //   · `DAT_1834b2dc4` = **0.1**（**`:113`**）⇒ punch 向量的**倍率**；基准向量 = `DAT_1842da2a8` 的
        //     `static_fields + 0xC` 那个静态 `Vector3` ⇒ **(1,1,1)**。
        //     🆕 **2026-10-11（批次1 · F1）已定案**：这里原来写「**结构推断**（静态字段的身份），要钉死得跑真 Play 读那个 static」
        //     —— **不用真 Play，静态就钉得死**，判据两条（都在本地、都实读过）：
        //     ① `d:/2/tools/il2cpp_out/dump.cs:537034-537035` —— `UnityEngine.Vector3` 的**静态字段表带偏移**：
        //        `private static readonly Vector3 zeroVector; // 0x0` · **`oneVector; // 0xC`**
        //        ⇒ `static_fields + 0xC` = **`oneVector` = (1,1,1)**；
        //     ② **反编译那一句本身就写死在 `+0xc` 上**：`RewardWindow__DoRewardAnimation.c:153-156`
        //        `lVar11 = *(longlong *)(DAT_1842da2a8 + 0xb8)`（`+0xb8` = `static_fields` 指针）之后
        //        读的正是 **`+0xc` / `+0x10` / `+0x14`** 三个 float（= 那个 Vector3 的 x/y/z；
        //        x 落在**低半**，与同处 `CONCAT44(...)` + `local_d0 = z` 的写法一致）⇒ punch = **0.1 × one** ✓ 实现一致。
        //     旁证（原来那两条，真）：`AbilityLogic__CreateTempAbilityTransform.c:73-81` 读同一地址三个 float 后
        //     `Transform.set_localScale`；`AnimFXModuleTransformModifier__Update.c:34-40` 在没有父节点时用同一地址兜底缩放
        //     （拿 `zeroVector` 当兜底缩放会得 0 ⇒ 反证它是 one）。类身份：`CustomTypes__DeserializeVector3.c:18,53`
        //     把 12 字节的向量装箱进 `DAT_1842da2a8` ⇒ 那个 Il2CppClass 就是 `UnityEngine.Vector3`。
        //   · `DAT_1834b2e84` = **0.75** ⇒ 延迟系数：`SetDelay(归一化距离 × 0.75)`
        //   · 归一化距离 = `clamp01(|drawer.localPosition.x| ÷ <参照>.localPosition.x)`
        //     （`DAT_1834b2e60` = `0x7FFFFFFF` ⇒ 那一位就是 `Mathf.Abs` 的位掩码；除零恒走 `0` 那支守卫）
        // 🔴 **2026-10-11（批次1 · W1）就地订正（铁律 5）**：`<参照>` 那件**不是 `content`** ——
        //    反编译里读的是 **`scrollRect + 0x40`**，按 `d:/2/tools/il2cpp_out/dump.cs` 的 `ScrollRect` 字段表
        //    （`m_Content` = **0x20** · `m_Viewport` = **0x40**）⇒ **是 `Viewport`**。
        //    🔴 **2026-10-11（批次1 · F1）就地订正（铁律 5）**：这一行原来写「`资料/普查产出_1010/戊2_RewardWindow_A302.md`
        //    §五·3 记的「`scrollRect.content`」作废」—— **指错了目标**：那份报告里**没有**「`scrollRect.content`」
        //    这几个字（审查代理逐字 grep 过整个 `资料/`，只有 W1 自己那两处提过）。它 §五·3 真正写的是
        //    「延迟 ∝ **到内容左沿**的归一化距离」（`戊2_RewardWindow_A302.md:101`）⇒ 该改的是那半句，
        //    **已就地订正**为参照 `m_Viewport`（那份报告与本文件两处口径现在一致）。
        //    ⚠️ 换算成「我们能写的量」：`Scroll View` 的 pivot 在**左沿**（0,0.5）、`Viewport` 的在**中心**
        //    （0.5,0.5）⇒ 在 `Scroll View` 的局部坐标里它 = **960**（= 视口半宽）；
        //    而**我们**这边两件的 `localPosition` 都是 0（`MenuDraw.Node` 一律「矩形中心对矩形中心」，
        //    pivot 不是 (0.5,0.5) 的语义我们不表达）⇒ **直接抄字段会恒得 0、静默丢掉整条错峰**
        //    ⇒ 取**等价量** `ScrollView.W × 0.5`（见 `PunchRefPx`）。
        //    ⇒ 语义 = 离**内容中心**（内容在视口里居中 ⇒ 也就是视口中心）越远、弹得越晚，
        //    最外圈（±960）满 `0.75s` —— 与「揭示从中间往两边拉开」正好配套。
        public const float PunchTime = 0.4f;
        public const int PunchVibrato = 5;
        public const float PunchElasticity = 0.2f;
        public const float PunchAmount = 0.1f;
        public const float PunchDelayMax = 0.75f;
        /// <summary>归一化距离的分母（原版 = `Viewport.localPosition.x`，= 视口半宽 960）。</summary>
        public static float PunchRefPx { get { return ScrollView.W * 0.5f; } }

        /// <summary>第 `i` 个抽屉的 punch 延迟（秒）—— **同一个式子**供自检直调（⛔ 别在自检里再写一份）。
        /// `count` = 奖励条数；抽屉的**内容坐标系**中心（未加滚动位移 —— 原版那句读的就是抽屉相对**内容**的
        /// `localPosition`，滚动改的是内容节点自己、不改这个相对量）。</summary>
        public static float PunchDelayAt(int i, int count)
        {
            if (count <= 0) return 0f;
            float contentW = Mathf.Max(MinContentW, PadL + PadR + count * ItemW
                                                   + Mathf.Max(0, count - 1) * ItemSpacing);
            // 内容在视口里**居中**（`CenteredContent`）⇒ 内容中心 = 视口中心；抽屉中心 − 内容中心：
            float drawerCx = -(contentW * 0.5f) + PadL + i * (ItemW + ItemSpacing) + ItemW * 0.5f;
            return PunchNorm(drawerCx, PunchRefPx) * PunchDelayMax;
        }

        /// <summary>`DoRewardAnimation` 里那条归一化距离，**逐字**（含 `|x|` 与 `[0,1]` 夹取、
        /// 含「分母为 0 ⇒ 判 0」那道守卫）。分母**不是**写死的 —— 由调用方给（见 `PunchRefPx` 的注释）。</summary>
        public static float PunchNorm(float drawerLocalX, float refX)
        {
            if (refX == 0f) return 0f;                       // = 反编译里 `if (fVar17 == 0.0)`
            float v = Mathf.Abs(drawerLocalX) / refX;        // = `(float)(bits & 0x7FFFFFFF) / (refX - 0)`
            return Mathf.Clamp01(v);
        }

        // ============================================================ `Tap To Continue` 的 `BlinkGraphic`
        // 🆕 **2026-10-11（批次1 · W1 · A310⑤）**：原来是一段**静止**的提示字。
        // 出处 = `d:/2/tools/decomp_full/BlinkGraphic__{Update,Start,ctor}.c`（三个方法逐句读）
        //   + 解包资源里那两个字段的值：`bundle_menus_assets_all` 的 **36 个 `BlinkGraphic` 实例逐个实读**
        //     —— `blinkSpeed` **全是 1.0**、`colorVariation` **全是 0.5**，与 ctor 的两个立即数
        //     （`0x3f800000` / `0x3f000000`）**逐位相同** ⇒ **一个 prefab 都没覆盖过**。
        // 行为：`Start()` 把 `graphic.color` 存成 `originalColor`；`Update()` 每帧
        //   `t = Clamp01(|**Mathf.Cos**(blinkSpeed × currentTime)|)`（`FUN_180488130` = `Mathf.Cos`）
        //   → **只改 alpha 那一位**：`a = Lerp(original.a, original.a × colorVariation, t)`
        //     （反编译里新值写在 `Color` 的**高 32 位** = `a`；低 32 位原样 `CONCAT44` 带过去）；
        //   → `currentTime += Time.deltaTime`（**scaled**：`BlinkGraphic__Update.c:31` 实读
        //     `UnityEngine_Time__get_deltaTime`，⛔ 不是 `unscaledDeltaTime`）。
        // 🔴 **2026-10-11（批次1 · F1）就地订正（铁律 5）**：这一行原来写的是 `Mathf.Sin`，还附了一条「反证」
        //    （「同族 `UIGradientUtils__RotationDir.c` 用同一对助手返回 `(sin, cos)`」）—— **两条都错，都撤**：
        //    · **`FUN_180488130` = `Mathf.Cos`，三条独立判据**：
        //      ① `Easing__<Get>g__inOutSine_0_30.c` = `(1 − F130(π·t)) × 0.5` —— 任何 easing 库的 InOutSine
        //         都必须 `g(0) = 0` ⇒ **`F130(0)` 必须是 1** ⇒ 只能是 **cos**（`sin(0)=0` 会得 0.5，那就不是曲线了）；
        //      ② 同族 `…g__outSine_0_29.c` = **`F6e0`**(t × π/2)、`…g__inSine_0_28.c` = `F6e0((t−1) × π/2) + 1`
        //         ⇒ **那支才是 sin**（`F6e0 ≠ F130`，两支各自与 in/outSine 的定义逐项对得上）；
        //      ③ 不依赖任何库的一条：`UIFlippableAndRotableUVs__RotatePointAroundPivotUVs.c` 里
        //         `x' = ox + (px−ox)·F130 − (py−oy)·F6e0` · `y' = oy + (py−oy)·F130 + (px−ox)·F6e0`
        //         = **标准逆时针旋转** ⇒ `F130 = cos θ`、`F6e0 = sin θ` 逐项对上。
        //      ⇒ 差别是**相位差 90°**：**开窗第一刻原版 α = `Lerp(a, 0.5a, |cos 0| = 1)` = `0.5a`（最暗）**，
        //        按 sin 实现则是 `1.0a`（最亮）—— 看得见（那颗字上**没有别的组件写颜色**：
        //        `EverguildTextController__Awake.c` 全文只 `set_fontSize`/`set_fontSizeMin`/`set_fontSizeMax`）。
        //    · **那条「反证」错在两处**：`UIGradientUtils__RotationDir.c:9-11` 实读 `uVar1 = F130(θ)`（= **cos**）、
        //      `uVar2 = F6e0(θ)`（= sin）、`return CONCAT44(uVar2, uVar1)` ⇒ Vector2 的 **x = uVar1 = cos**、
        //      **y = uVar2 = sin** ⇒ 返回 **`(cos θ, sin θ)`** —— **正是它说它不是的那一种**
        //      （同一约定在 `UIGradient__ModifyMesh.c:77-78` 的内联处也看得到：cos 写 x、sin 写 y）。
        //      而且**就算它真是 `(sin, cos)` 也证不了哪一支是 sin** —— 它压根不是 `FUN_180488130` 身份的判据。
        //    ⚠️ 顺带（⛔ 本件**没改**、留给调度台裁决）：`资料/主菜单_原版规格.md:270-271` 与记忆文件里挂着同一句错解释。
        //      **但它那条实测结论仍然成立** —— 有效渐变轴确实是 `(sin θ, cos θ)`，理由在**消费者**那一侧：
        //      `UIGradient__ModifyMesh.c:148-149` 把 `dir.x` 配 **y**、`dir.y` 配 **x**
        //      （`pos.y·(cos/size.y) + pos.x·(sin/size.x) + m5`）⇒ **结论对、理由错**，两者不冲突。
        public const float BlinkSpeed = 1f, BlinkVariation = 0.5f;

        // ============================================================ 运行时状态

        public readonly List<string> MissingArt = new List<string>();
        /// <summary>本地查不到图标的物品 id（**逐条出声** —— 判据空不是静默）。</summary>
        public readonly List<string> NoIconItems = new List<string>();
        /// <summary>抽屉里那张**判据图**取不到、退了 `_small` 档的物品 id（出声；同 `CampaignRewardWindow`）。</summary>
        public readonly List<string> FallbackArtItems = new List<string>();

        RewardWindowContext _ctx;
        Transform _listHolder;
        MenuScroll _scroll;
        Label _titleGet, _titlePreview;
        GameObject _bgGet, _bgPrev, _glowGet, _glowPrev, _collect, _premium, _tap;
        float _reveal = 1f;              // 1 = 动画结束（出厂「没在动」—— `Open()` 才会把它置 0 再来一遍）
        /// <summary>`Tap To Continue` 那段字（= 原版 `BlinkGraphic.graphic` 的落点）。</summary>
        Label _tapLabel;
        /// <summary>= 原版 `BlinkGraphic.Start()` 存下的 `originalColor`（那件是 TMP 自己的 `m_Color`）。</summary>
        Color _tapBase = Color.white;
        /// <summary>= 原版 `BlinkGraphic.currentTime`（每帧 `+= deltaTime`）。</summary>
        float _blinkT;
        /// <summary>「`Tap Text` 没建出来」那件事**只出声一次**（`BlinkTick` 每帧都调 ⇒ 不能每帧刷屏；
        /// 本仓既有同款：`ItemDrawer.Note` / `Core/CardIcons` 的 `_warned`）。</summary>
        bool _blinkNullWarned;
        /// <summary>逐件 punch 的**当前值**。⚠️ tween **打在这个数组上**，不是打在抽屉的 `Transform` 上 ——
        /// 见 `StartPunches()` 的注释（我们的物品每帧都要重建，tween 绑 Transform 会被销毁打断）。</summary>
        Vector3[] _punchScale = new Vector3[0];
        readonly List<Tween> _punches = new List<Tween>();
        /// <summary>刚建出来的那一批抽屉节点（按奖励下标排；`ApplyPunchScales` 按它对位贴 punch 值）。</summary>
        readonly List<Transform> _itemNodes = new List<Transform>();
        /// <summary>本窗自己的动画时钟（揭示 + punch 共用）与「最后一件 punch 何时结束」。</summary>
        float _animT, _punchEnd;
        /// <summary>**按 `RewardTier` 排过序**的那份奖励表（= 原版 `Open()` 里那句
        /// `context.Rewards.OrderBy(r =&gt; r.RewardTier).ToList()`）。`BuildItems` 与 `StartPunches` 都走它 ——
        /// ⛔ **`context.Rewards` 本身不动**（原版 `OnCollect` / `OnClose` 回调带出去的仍是 `+0x38` 那一份原序）。</summary>
        CampaignData.RewardSpec[] _ordered = new CampaignData.RewardSpec[0];

        /// <summary>一格抽屉 —— **= 原版 `RewardWindow.drawers` 那个 `List&lt;ItemDrawer&gt;`（字段 `+0xE8`）里的一项**。
        /// ⚠️ **一格 ≠ 一条奖励**：第 4 跳（`!Options.Stackable`）会把一条奖励展开成 `quantity` 格
        /// （`RewardWindow__Open.c:133-147`，每格 `quantity = 1`）⇒ 那之后「格」才是排版的单位
        /// （原版也是：`DoRewardAnimation` 遍历的就是这个 `drawers` 列表，**展开出来的那几格也参与 punch**）。</summary>
        struct Cell
        {
            /// <summary>这一格属于 `_ordered` 里的哪一条（第 4 跳展开出来的那几格与它的首格同号）。</summary>
            public int Reward;
            /// <summary>这一格画的 `quantity`（首格 = 该奖励的数量；展开出来的那些 = **1**）。</summary>
            public int Quantity;
        }

        /// <summary>第 4 跳展开后的格表（`Build()` 里跟着 `_ordered` 一起算）。</summary>
        Cell[] _cells;

        /// <summary>物品抽屉的宿主（= 原版 `listHolder`）。自检按它数格子。</summary>
        public Transform ListHolder { get { return _listHolder; } }
        /// <summary>视口滚动区（整扇窗**唯一**一份滚动实现 = `MenuScroll`；原版那颗 `ScrollRect` 是 h=1 弹性）。</summary>
        public MenuScroll Scroll { get { return _scroll; } }
        /// <summary>揭示动画的进度（**0 = 刚开、遮罩收拢；1 = 全开**）。自检直接读它。</summary>
        public float RevealProgress { get { return _reveal; } }
        /// <summary>此刻遮罩的 padding（= 原版 `RectMask2D.m_Padding` 的那个值）。</summary>
        public Vector4 MaskPad { get { return MaskPadAt(1f - _reveal); } }
        public bool IsPreview { get { return _ctx != null && _ctx.IsPreview; } }
        public bool IsPremiumLocked { get { return _ctx == null || _ctx.IsPremiumLocked; } }
        /// <summary>开这一窗时带的那个 context（自检按它核「带来了哪几条奖励 / 哪个 flag」）。</summary>
        public RewardWindowContext Context { get { return _ctx; } }

        /// <summary>第 `i` 个抽屉**此刻**的 punch 缩放（没在弹 = `(1,1,1)`）。自检读它。</summary>
        public Vector3 PunchScaleOf(int i)
        {
            return (i >= 0 && i < _punchScale.Length) ? _punchScale[i] : Vector3.one;
        }
        /// <summary>punch 那一段**还该不该推进**（= 有奖励、非预览态、时钟还没跑过 `PunchEnd`）。</summary>
        public bool PunchRunning { get { return !IsPreview && _punchEnd > 0f && _animT <= _punchEnd; } }
        /// <summary>本窗全部 punch 跑完的时刻（秒；从 `Build()` 那一刻起算）。</summary>
        public float PunchEnd { get { return _punchEnd; } }
        /// <summary>本窗的动画时钟（揭示 + punch 共用；由 `Tick(dt)` 推）。</summary>
        public float AnimT { get { return _animT; } }

        /// <summary>`BlinkGraphic` 的 `t` = `Clamp01(|cos(blinkSpeed × currentTime)|)`（原版 `Update` 逐句）。
        /// 🔴 **是 `cos` 不是 `sin`**（2026-10-11（批次1 · F1）按反编译订正 —— 三条独立判据见上面那一段）。</summary>
        public float BlinkT { get { return Mathf.Clamp01(Mathf.Abs(Mathf.Cos(BlinkSpeed * _blinkT))); } }
        /// <summary>此刻 `Tap To Continue` 的 alpha = `Lerp(原色.a, 原色.a × colorVariation, BlinkT)`。</summary>
        public float BlinkAlpha { get { return Mathf.Lerp(_tapBase.a, _tapBase.a * BlinkVariation, BlinkT); } }
        /// <summary>= `BlinkGraphic.currentTime`（自检用它确认时钟真在走）。</summary>
        public float BlinkClock { get { return _blinkT; } }

        public static RewardWindow Create(WindowsManager mgr)
        {
            var go = new GameObject("Reward Window");
            var win = go.AddComponent<RewardWindow>();
            win.type = WindowType.Popup;                    // 实证 type=1
            win.placement = WindowsPlacement.Popup;         // 实证 windowsPlacement=15
            win.closeOnEsc = true;                          // 实证 closeOnESC=1
            win.extraScaleSmallScreen = 1f;                 // 实证 1.0
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        // ============================================================ 入口（= 原版 `RewardService.Collect` / `Preview`）
        //
        // 🆕 **2026-10-11（批次1 · W1 · A309）**：下面这三个静态方法就是「本工程还没有调用点」那一条的接线。
        // 判据（`RewardService__{Collect,Preview}.c` 逐句 + `d:/2/tools/il2cpp_out/dump.cs` 的方法签名）：
        //   · `Collect(IReadOnlyList<RewardInfo> rewards, bool showAnimation = true, Action<…> onCollected,
        //      IReadOnlyList<RewardInfo> collectedRewards, bool isPremiumLocked = **true**, string customTitle,
        //      bool showXpToast = true)` —— 发完奖之后
        //      `new RewardWindowContext(rewards, isPremiumLocked, onCollect: **null**, onClose: onCollected,
        //       … , isPreview: **0**)` ⇒ **「Rewards claimed」那一态、`Collect Button` 整颗不建**，
        //      点窗外 / ESC 关掉时回调 `onCollected`（`RewardWindow__Close.c` 读 `0x30` 发 `Rewards`）。
        //      ⚠️ `showAnimation == false` 时**根本不开窗**（`RewardService__Collect.c:170` 那一支直接跳去回调）。
        //   · `Preview(rewards, onClose, collectedRewards, isPremiumLocked = **true**, customTitle)` ⇒
        //     同一个 ctor，但 `isPreview: **1**` ⇒ **「You will get」预览态**。
        //      它的触发者是「**一个抽屉里装着多条奖励**」的那些格子 —— `DailyRewardDrawerController` /
        //      `DailyStreakItemContainer` 的 `<Initialize>b__0`，而且**只在 `rewards.Count > 1` 时才挂**
        //      （两处 `Initialize.c` 里 `if (1 < Count)` 那一支；那个 lambda 与
        //       `MissionMilestoneStep.<DisplayCheckmark>b__0` 等 **10 个**是同一段字节 —— ICF 折叠，函数体一致）。
        //      ⇒ **我们这边暂时没有那种格子**（日常两扇窗每个格子只有一条奖励）⇒ `ShowPreview` **暂无调用点**，
        //      如实记在报告里（⛔ 不是「做完了」）。
        // 🔴 **`RewardService` 这个类我们没有**（它是服务端流程那一套），所以这两个入口落在窗类上 ——
        //    **这是「我们挑的落点」**（同族先例 = `WindowsManager.ShowPopUp` 那种「服务方法住在管理器上」，
        //    但那样会把 `RewardWindow` 的细节漏进共用件）；⛔ 不是「原版就长在窗上」。

        /// <summary>开一扇领奖窗；**场上已经开着一扇就先关掉它**（= 原版那两个入口开头那一跳
        /// `GetOpenWindow&lt;RewardWindow&gt;() != null ⇒ Close()`）。返回那扇窗（没建成 = `null`）。</summary>
        public static RewardWindow Show(RewardWindowContext ctx)
        {
            if (ctx == null) return null;
            var wm = WindowsManager.EnsureHost();
            if (wm == null) return null;
            RewardWindow win = null;
            for (int i = wm.openWindows.Count - 1; i >= 0; i--)
            {
                var w = wm.openWindows[i];
                if (w == null) continue;
                var rw = w as RewardWindow;
                if (rw == null) continue;
                if (rw.CurrentState != WindowState.Closed) rw.Close();   // = 原版那一跳
                win = rw;             // 我们的 `Close()` 只 `SetActive(false)`（老账 A123）⇒ 实例还在，直接复用
                break;
            }
            if (win == null)
            {   // 关过的实例仍挂在 Popup 锚点下 ⇒ 找出来复用，别每领一次就多一个物体（原版是 `Destroy`，没这问题）
                var anchor = WindowsManager.GetWindowAnchor(WindowsPlacement.Popup);
                if (anchor != null)
                {
                    var all = anchor.GetComponentsInChildren<RewardWindow>(true);
                    if (all.Length > 0) win = all[0];
                }
            }
            if (win == null) win = Create(wm);
            wm.OpenWindow(win, ctx);
            return win;
        }

        /// <summary>= 原版 `RewardService.Collect(...)` 的**开窗那一半**（发奖不在这里 —— 我们的发奖口只有一个：
        /// `Shell/DailyData.cs` 的 `Wallet.Grant`）。默认值与原版签名逐个对齐：`isPremiumLocked = true`、
        /// `onCollect = null`（⇒ `Collect Button` 整颗不建）、`isPreview = false`。</summary>
        public static RewardWindow ShowCollected(CampaignData.RewardSpec[] rewards,
                                                 System.Action<CampaignData.RewardSpec[]> onClose = null,
                                                 bool isPremiumLocked = true, string customTitle = null)
        {
            return Show(new RewardWindowContext
            {
                Rewards = rewards ?? new CampaignData.RewardSpec[0],
                IsPremiumLocked = isPremiumLocked, IsPreview = false,
                OnCollect = null, OnClose = onClose, CustomTitle = customTitle,
            });
        }

        /// <summary>= 原版 `RewardService.Preview(...)`。⚠️ **本工程还没有调用方**（判据见上面那段：
        /// 原版那条路的触发者是「一个抽屉装多条奖励」的格子，我们暂时没有）。
        /// 留着它，是因为 `Preview` 也是这扇窗**真实的**第二个人口 —— 接线时只差一个调用点。</summary>
        public static RewardWindow ShowPreview(CampaignData.RewardSpec[] rewards,
                                               System.Action<CampaignData.RewardSpec[]> onClose = null,
                                               bool isPremiumLocked = true, string customTitle = null)
        {
            return Show(new RewardWindowContext
            {
                Rewards = rewards ?? new CampaignData.RewardSpec[0],
                IsPremiumLocked = isPremiumLocked, IsPreview = true,
                OnCollect = null, OnClose = onClose, CustomTitle = customTitle,
            });
        }

        protected override void SetupData(object data) { base.SetupData(data); _ctx = data as RewardWindowContext; }

        public override void Open() { Build(); }

        /// <summary>关窗：先发 `OnClose(Rewards)`（**参数是 `Rewards`**，反编译里那两个 `+0x38` 实读），再走基类。</summary>
        public override void Close()
        {
            var cb = _ctx != null ? _ctx.OnClose : null;
            if (cb != null) cb(_ctx.Rewards ?? new CampaignData.RewardSpec[0]);
            base.Close();
        }

        /// <summary>点 `Collect Button`：发 `OnCollect(Rewards)`，之后这一颗**不再响应**（原版那两句里的
        /// `Selectable.interactable = false`）。⚠️ 我们这套 `WindowButton` 没有 `interactable` 字段
        /// ⇒ 用**同一句门在 `onClick` 里**（`CampaignRewardWindow.BuildUnlockButton` 的 `clickable` 是同一口径）。
        /// 重新 `Build()` 会把门放回去（`_collectArmed = true`）。</summary>
        bool _collectArmed = true;

        void OnCollectClicked()
        {
            if (!_collectArmed) return;
            _collectArmed = false;
            var cb = _ctx != null ? _ctx.OnCollect : null;
            if (cb != null) cb(_ctx.Rewards ?? new CampaignData.RewardSpec[0]);
        }

        // ============================================================ 揭示动画（帧循环那条路 + 自检直调）

        /// <summary>🔴 批处理下**没有帧循环** ⇒ 自检直接调它（同 `MenuScroll` 由 `PointerLayer.Update` 驱动、
        /// 自检直调那条约定）。`dt` 秒推进一次；走完 `AnimTime` 就停在 1。
        /// <para>🆕 **2026-10-11（批次1 · W1 · A310①⑤）**：本方法现在还推两样东西 ——
        /// **逐件 punch 的时钟**（`_animT`）与 **`BlinkGraphic` 的时钟**（`_blinkT`）。</para></summary>
        public void Tick(float dt)
        {
            _animT += dt;
            BlinkTick(dt);
            if (_reveal < 1f)
            {
                _reveal = Mathf.Clamp01(_reveal + dt / AnimTime);
                BuildItems();                       // 遮罩变了 ⇒ 视口里那批件要**按新带口重切**
                return;                             // 重建那一路顺手把 punch 的当前值贴上去了
            }
            // 揭示跑完（0.8s）之后 punch 可能还在飞（最晚 0.75 + 0.4 = **1.15s**）⇒ 这一支**不重建**、
            // 只把数组里此刻的值重贴到已经建好的那批节点上（少了这一支，最后那一批会**冻在半路**）。
            if (PunchRunning) ApplyPunchScales();
        }

        /// <summary>帧循环里的入口。🔴 **时钟基 = `Time.deltaTime`（scaled）** —— 2026-10-11（批次1 · F1）就地订正（铁律 5）：
        /// 原来用的是 `Time.unscaledDeltaTime`（那是从别处**搬**过来的写法）。原版这一支读的是
        /// **`UnityEngine.Time.get_deltaTime`**（`BlinkGraphic__Update.c:31`，逐句实读）⇒ `timeScale = 0` 时**原版停、我们不能停**。
        /// 另两条同向的旁证（都在本文件里）：① 开场揭示在原版是 **DOTween** 的默认时钟（= scaled）；
        /// ② 逐件 punch 那些 tween 走 `CardTween.Mode` = `UpdateType.Normal`（也是 scaled）——
        /// 三样东西同在 `Tick` 里推，统一成 scaled 之后**只有一种时间基**，不会一半走一半停。</summary>
        void Update() { Tick(Time.deltaTime); }

        // ---- 逐件 punch（`DoRewardAnimation` 后半段那一圈 `DOPunchScale`）

        /// <summary>起逐件 punch。参数与调用形态照原版**逐字**（`DOTween.Punch` 就是 `DOPunchScale` 内部那一条；
        /// 不给 `SetEase` —— 原版对这支 tween 也没给）。
        /// 🔴 **为什么 tween 打在 `_punchScale` 这个数组上、而不是抽屉的 `Transform` 上**：
        /// 我们的物品格**每次 `Tick` 都要重建**（裁切是在建的时候切进矩形/uv 的；原版靠 `RectMask2D` 在 GPU 上裁、
        /// 建一次就够）—— tween 若绑在 `Transform` 上，第一次重建就把它跟那个节点一起毁掉
        /// ⇒ punch **静默地一帧都没播**（而且不会有任何报错）。打在数组上之后：tween 自己活到 `PunchEnd`，
        /// `BuildItems` / `ApplyPunchScales` 把当前值贴给**当时存在**的那个节点。
        /// 唯一被换掉的是「被驱动的对象」，四个字面量与 `SetDelay` 的算式都与原版相同。</summary>
        void StartPunches()
        {
            // 🆕 **2026-10-11（A311）**：单位是**格**不是「奖励条数」—— 原版 `DoRewardAnimation` 遍历的是
            //   `drawers`（`+0xE8`）那个列表，而第 4 跳展开出来的那几格**也在里面**
            //   （`RewardWindow__Open.c:139-147` 的循环体里 `FUN_180002430(drawers, …)` 把它们逐个加进去）。
            //   无展开时 `_cells.Length == _ordered.Length`，与原来逐位相同。
            var list = _ordered ?? new CampaignData.RewardSpec[0];
            int count = (_cells != null && _cells.Length > 0) ? _cells.Length : list.Length;
            _punchScale = new Vector3[count];
            for (int i = 0; i < count; i++) _punchScale[i] = Vector3.one;
            _punches.Clear();
            _punchEnd = 0f;
            if (IsPreview || count == 0) return;   // 预览态不播（`RewardWindow__Open.c:283` 读 `0x19`）
            float end = 0f;
            for (int i = 0; i < count; i++)
            {
                float delay = PunchDelayAt(i, count);
                if (delay > end) end = delay;
                int k = i;                                  // ⛔ 别直接捕循环变量（闭包要的是**这一格自己**的下标）
                var tw = DOTween.Punch(() => _punchScale[k], v => _punchScale[k] = v,
                                       Vector3.one * PunchAmount, PunchTime, PunchVibrato, PunchElasticity);
                if (tw == null) continue;
                tw.SetDelay(delay);                         // 原版无条件 `SetDelay(归一化距离 × 0.75)`
                tw.SetUpdate(CardTween.Mode);               // 批处理里没有帧循环 ⇒ 自检用 `CardTween.Advance` 推
                if (CardTween.LinkEnabled) tw.SetLink(gameObject);   // 窗被销毁时把 tween 一起带走（同 `CardTween.Use` 的理由）
                _punches.Add(tw);
            }
            _punchEnd = end + PunchTime;
        }

        /// <summary>把每一格此刻的 punch 值贴到它的节点上（`BuildItems` 之后、以及揭示结束后每帧都走）。</summary>
        void ApplyPunchScales()
        {
            for (int i = 0; i < _itemNodes.Count; i++)
            {
                var n = _itemNodes[i];
                if (n == null) continue;
                n.localScale = PunchScaleOf(i);
            }
        }

        // ---- `Tap To Continue` 的 `BlinkGraphic`

        /// <summary>= 原版 `BlinkGraphic.Update()`（`d:/2/tools/decomp_full/BlinkGraphic__Update.c` 逐句）。
        /// <para>🔴 **2026-10-11（批次2 · A355）已按原版落地（铁律 5 订正）**：本行这段时间线原来写的是
        /// 「A325 现读核过、但**这一轮没落地**（卡在 `Editor/RewardsScene.cs` 不在白名单）」
        /// ＋「下一手：先把 §九(f) 的采样改成「先 `Tick` 到目标时钟、再 `Tick(0f)` 取色」，再把这里两句倒过来」——
        /// **那两件这一批都做了**：`RewardsScene` §九(f) 的采样先改（**五条期望值一个都没动**），
        /// 本方法后改（下面两句已按原版次序）。⇒ 那段「没落地」的记录**不再成立**，就地改掉，判据留在这里。</para>
        /// <para>判据（反编译逐句）原有**两处次序差**，**两处现在都对齐了**：
        /// · ① 原版**先按此刻的 `currentTime` 算颜色、算完才** `currentTime += deltaTime`
        /// （`:17` 读 `+0x30` 算 `t` → `:24-26` 写色 → **`:31` 才** `*(param_1 + 0x30) = deltaTime + 旧的`）
        /// —— ✅ 我们现在逐字同序（`BlinkAlpha` → `SetColor` → `_blinkT += dt`）；
        /// · ② 原版那句推进**写在 `if (graphic != null)` 之内**：`null` 那一支走 `FUN_1803f47a0()`
        /// （**抛 NRE**，反编译里标着 `/* WARNING: Subroutine does not return */`）⇒ **时钟一次都不会走**；
        /// —— ✅ 我们现在 `null` 那一支**直接 return、不推时钟**（⛔ 不抛 NRE：批处理里抛了就整条自检没了；
        /// 但也**不静默** —— 出一声日志，见方法体）。</para>
        /// <para>⚠️ 时钟**推进量**与 `BlinkT` / `BlinkAlpha` / `BlinkClock` 的读数在两版之下**完全相同**（都是 `Σdt`）
        /// ⇒ 除「写进 `Label` 的颜色」那一组（§九(f)），别的断言一条都不受影响。</para>
        /// <remarks>**改坏法**：① 把 `_blinkT += dt;` 挪回写色**之前** ⇒ 自检那条「**次序**」断言红
        /// （`Editor/RewardsScene.cs` §九(f)：`Tick(π/2)` 那一拍写进标签的必须是**推进前**那一档 0.5；
        /// ⚠️ **只有那一条**分得出两种次序 —— `Tick(T)` + `Tick(0f)` 这一对在新旧次序下最后一笔相同）；
        /// ② 在 `_tapLabel == null` 那一支也推时钟 ⇒ 自检那条「`_tapLabel == null` 时 `BlinkClock` 停在 0」红
        /// （夹具 = 把 `Tap Text` 那颗 `DestroyImmediate` 掉，模拟原版 `graphic == null` 那一支）。</remarks></summary>
        void BlinkTick(float dt)
        {
            // 🔴 **2026-10-11（批次2 · A355）：次序照原版** —— 判据 `BlinkGraphic__Update.c:17-32`（见上）。
            if (_tapLabel == null)
            {
                // 原版这一支**抛 NRE**（`FUN_1803f47a0`，反编译标着「不返回」）⇒ **时钟一次都不走**。
                // 我们**不抛**（批处理里抛一下整条自检就没了），但**同样不推时钟**；⛔ 不静默 ⇒ 出声一次。
                if (!_blinkNullWarned)
                {
                    _blinkNullWarned = true;
                    Debug.LogWarning("[RewardWindow] `Tap To Continue` 那段字没建出来（= 原版 `BlinkGraphic.graphic == null`）"
                                     + "⇒ 原版在这一支**抛 NRE、时钟一次都不走**（`BlinkGraphic__Update.c:20-30` 的推进写在"
                                     + " `if (graphic != null)` 之内）；我们**不抛**、**也不推时钟、不写色**（逐字对齐那一句的语义）。");
                }
                return;
            }
            var c = _tapBase;
            c.a = BlinkAlpha;               // 原版 `:17` 读 `+0x30` 算 `t` ⇒ `:24-26` 只改 alpha 那一位
            _tapLabel.SetColor(c);
            _blinkT += dt;                  // **算完才推**（原版 `:31`）—— ⛔ 别挪回写色之前（自检那条「次序」断言盯着）
        }

        /// <summary>`k = 1` ⇒ 遮罩收到最拢（初始）· `k = 0` ⇒ 全开。宽屏那一档见上面那组字面量。</summary>
        static Vector4 MaskPadAt(float k)
        {
            float w = MaskPadWide.x;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : WideAspect;
            if (aspect > WideAspect)
                w += Mathf.Clamp01((aspect - WideAspect) / WideSlope) * WidePerAspect;
            return new Vector4(w * k, 0f, w * k, 0f);
        }

        // ============================================================ 建

        public void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);
            MissingArt.Clear(); NoIconItems.Clear(); FallbackArtItems.Clear();
            _titleGet = _titlePreview = null;
            _bgGet = _bgPrev = _glowGet = _glowPrev = _collect = _premium = _tap = null;
            _scroll = null; _listHolder = null;
            _tapLabel = null; _tapBase = Color.white;
            _itemNodes.Clear();
            // 重开一遍 = 时钟归零；上一轮那批 punch 的 tween 先杀掉
            // （它们读写的是 `this._punchScale`，留着会跟新一轮的 tween 抢同一个数组）
            for (int i = 0; i < _punches.Count; i++) if (_punches[i] != null) _punches[i].Kill();
            _punches.Clear();
            _animT = 0f; _blinkT = 0f; _punchEnd = 0f;
            _collectArmed = true;      // 重新建 ⇒ 那颗按钮重新可点（原版 `Open()` 里也重设 `interactable = 1`）
            var ctx = _ctx ?? new RewardWindowContext { Rewards = new CampaignData.RewardSpec[0] };
            // 🆕 **2026-10-11（批次1 · W1 · A310②）**：原版 `Open()` 画之前先
            // `context.Rewards.OrderBy(r => r.RewardTier).ToList()`（**已坐实**，见 `OrderByTier` 的注释）。
            // `BuildItems` / `StartPunches` 都走这一份；⛔ `context.Rewards` 本身保持原序
            //（原版那两个回调带出去的仍是 `+0x38` 那一份原序）。
            _ordered = OrderByTier(ctx.Rewards);
            // 🆕 **2026-10-11（批次2 · A311）第 4 跳**：`!Options.Stackable` 的那几条要**按数量展开成多格**
            //   （原版 `RewardWindow__Open.c:133-147`；`BuildItems` 按这个格表排版、`StartPunches` 按它数 tween）。
            _cells = ExpandCells(_ordered);

            // ① `Menu Dark Background`（sprite 为空 ⇒ 纯色块）+ 它的点击区（原版 `closeButton` 就在这一层上）
            MenuDraw.Rect(root, CardArt.Solid(), Shade, "Menu Dark Background", QShade, ShadeColor);
            MenuDraw.ShadeHit(root, Shade, QShade, QCollectBg, () => Close(), "BackgroundHit");

            // ② `Content`
            var content = MenuDraw.Node(root, "Content", Content);
            // 两态底图（「Get」红 / 「Preview」灰）—— 出场都建，由 `ConfigureIsPreviewState` 决定开哪一张
            _bgGet = RectGo(content, Art(ArtBgGet), Content, "Reward Background Get Reward", QContentBg);
            _bgPrev = RectGo(content, Art(ArtBgPreview), Content, "Reward Background Preview Reward", QContentBg);
            // 面板底图吸收点击（判据 = 原版那颗 `Image.m_RaycastTarget = 1`；层 = 内容档 − 1，同 A94 的口径）
            MenuDraw.Absorb(root, "AbsorbHit", Content, QShade, QCollectBg);

            // ③ `Scroll View` → `Viewport` → `Content`（= `listHolder`）
            //    ⚠️ `Scroll View` 自己那张 `Background` 图 `m_Color.a = **0**`、`Viewport` 的 `UIMask` 同样 a=0
            //    ⇒ 照「不画不可见的件」的纪律**两张都不建**（同 `CampaignRewardWindow`）。
            var sv = MenuDraw.Node(content, "Scroll View", ScrollView);
            var vp = MenuDraw.Node(sv, "Viewport", ScrollView);
            // `Viewport/Content` 的矩形：**拉伸锚 + CSF MinSize** ⇒ 宽 = max(120, 内容宽)、**水平居中**。
            // 这里先按「还没有内容」那个最小宽建点（真正的位置在 `BuildItems()` 里按内容宽重摆）。
            _listHolder = MenuDraw.Node(vp, "Content", CenteredContent(MinContentW));
            _scroll = new MenuScroll(ScrollView, ScrollView.CX - MinContentW * 0.5f, ScrollView.CX + MinContentW * 0.5f)
            { Elastic = true, Inertia = true, Owner = gameObject };
            _scroll.OnChanged = BuildItems;                 // 滚动 ⇒ 内容按新偏移重建（同锻造/战役轨道那条规矩）
            PointerLayer.RegisterScroll(_scroll);           // ⚠️ 本窗**能滚**（内容比视口宽时；`AdjustBounds` 已管住两头）

            // ④ `Title` 两态（底光 + 文字；`Text *` 是 `Glow *` 的**子件**，挂错父会让「开关底光」管不到字）
            var title = MenuDraw.Node(content, "Title", Title);
            _glowGet = RectGo(title, Art(ArtGlow), Glow, "Glow Get reward", QTitleBg, GlowGet, true);
            _titleGet = GlowText(_glowGet, "Text Get Reward", TxtGet);
            _glowPrev = RectGo(title, Art(ArtGlow), Glow, "Glow Preview reward", QTitleBg, GlowPreview, true);
            _titlePreview = GlowText(_glowPrev, "Text Preview Reward", TxtPreview);
            if (!string.IsNullOrEmpty(ctx.CustomTitle))
            {   // 原版：非空时给**可见那一份**的 TMP 设 `Localize.Term`（我们两份都设，语义等价）
                if (_titleGet != null) _titleGet.SetText(ctx.CustomTitle);
                if (_titlePreview != null) _titlePreview.SetText(ctx.CustomTitle);
            }

            // ⑤ `Collect Button`（原版 `Image` = Simple `UI_Button_Mulligan`；HL/Pressed 两张走 SpriteSwap）
            var btn = MenuDraw.Node(content, "Collect Button", CollectBtn);
            var btnQ = MenuDraw.Rect(btn, Art(ArtCollectBtn), CollectBtn, "bg", QCollectBg);
            // `Button Text`：原版 `Center/Capline`（`Label` 天然水平居中 ⇒ 水平那一半不用动）
            Text(btn, ButtonText, TxtCollect, Color.white, "Button Text",
                 CollectFont, QCollectText, 0f, 0f, 0);
            // 🔴 **2026-10-11（批次1 · F8）就地订正（铁律 5）**：这一段原来在 `MenuDraw.Hit` 的返回值上
            //    **又 `AddComponent<WindowButton>()` 挂了一颗** —— 而 `MenuDraw.Hit` 的尾段本来就是
            //    「建命中 quad + `AddComponent<WindowButton>` + `onClick` + `Bind`」
            //    （`Shell/MenuDraw.cs:1452-1458`，`AddComponent` 那句是**无条件**的），
            //    而且这里那两句（`onClick` = `OnCollectClicked`、`Bind(btnQ, ArtCollectBtn)`）
            //    与 `MenuDraw.Hit` 收到的实参**逐字同源** ⇒ **那第二颗是纯冗余**。
            //
            //    代价（**本轮实测，不是推演**）：同一个 `Hit` 节点上两颗同型 `WindowButton`、
            //    共用**同一颗**命中 quad（`HitQuad` = `GetComponentInChildren<ImageQuad>()`）
            //    ⇒ 队列与 z 逐位相同 ⇒ `PointerLayer.HitButton` 那条「队列大的先、同队列 z 小的先」
            //    **分不出这两颗**，赢家退化成 `FindObjectsByType` 的**枚举顺序**；
            //    而自检那一侧取件走的是 `GetComponentInChildren<WindowButton>(true)`（= 组件表里的**第一颗**）
            //    —— **两把尺子各挑一颗** ⇒ `Editor/RewardsScene.cs` 那条「引用相等（真鼠标点得到）」的前提
            //    出红，**而场上其实点得到**（两颗的 `onClick` 都绑着 `OnCollectClicked`，
            //    所以同一段的 `OnCollect` 计数那条照样绿 —— 症状极具误导性）。
            //    ⛔ **别再往 `MenuDraw.Hit` / `AddHit` 的返回值上挂第二颗 `WindowButton`**：
            //    全工程 `grep` 过，**只有这一处**那么写（其余每一处 `AddComponent<WindowButton>` 都建的是自己的节点）。
            //    **改坏法**：把那两行加回来 ⇒ `RewardsScene` §⑦ 那条「`Collect Button` 上**只有一颗**
            //    `WindowButton`」立刻红（不必等「引用相等」那条去撞枚举顺序）。
            MenuDraw.Hit(btn, "Hit", CollectBtn, QCollectBg, OnCollectClicked, btnQ, ArtCollectBtn);
            _collect = btn.gameObject;

            // ⑥ `Premium Disclaimer`（右对齐到 1708.8）+ 它的子件 `Premium Icon`
            //    ⚠️ 原版**文字就在 `Premium Disclaimer` 这个节点上**（TMP + `ContentSizeFitter`）；
            //    我们这套 `Label` 是**自己的 GameObject** ⇒ 多一层容器（名字照原版，文字叫 `Disclaimer Text`）。
            var dis = MenuDraw.Node(content, "Premium Disclaimer", new PxRect(
                DisclaimerRight - 600f, DisclaimerTop, DisclaimerRight, DisclaimerBottom));
            Text(dis, new PxRect(DisclaimerRight - 600f, DisclaimerTop, DisclaimerRight, DisclaimerBottom),
                 TxtPremium, Color.white, "Disclaimer Text", PremFont, QPremText, 0f, 0f, 2);
            MenuDraw.Rect(dis, Art(ArtPremIcon), PremiumIcon, "Premium Icon", QPremIcon);
            _premium = dis.gameObject;

            // ⑦ `Tap To Continue`（出厂 INACT，`Open()` 按 `!IsPreview` 打开 —— `RewardWindow__Open.c:189` 读 `0x19`）
            var tapNode = MenuDraw.Node(content, "Tap To Continue", TapToContinue);
            var tapLb = Text(tapNode, TapToContinue, TxtTap, Color.white, "Tap Text",
                             TapFont, QTapContinue, 0f, 0f, 0);
            // 🔴 原版这一段的 `m_VerticalAlignment = 1024 (**Bottom**)`，而我们的 `Label` **只有水平对齐**
            //    （`Battle/Label.cs` 只有 `AlignLeftOn` / `AlignRightOn`）⇒ 按**量出来的行高**把它贴到框底，
            //    等价于 Bottom（⛔ 不是「换个中心点」那种近似：贴底那一条 = 原版渲出来的那一条）。
            if (tapLb != null)
            {
                float h = tapLb.WorldH * 108f;
                if (h > 0.5f)
                    tapLb.transform.localPosition = MenuDraw.Local(tapNode, TapToContinue.x1,
                        TapToContinue.y2 - h, TapToContinue.x2, TapToContinue.y2);
            }
            _tap = tapNode.gameObject;
            // 🆕 **2026-10-11（批次1 · W1 · A310⑤）**：这一段字是原版 `BlinkGraphic` 的落点 ——
            //   记下它 + 它出厂那一位 `originalColor`（= `BlinkGraphic.Start()` 干的那件事）。
            _tapLabel = tapLb;
            if (tapLb != null) _tapBase = tapLb.color;

            // ⑧ `Menu Vignette` 最后画（原版它是 `Content` 的**下一个兄弟** ⇒ 在所有内容之上）
            MenuDraw.Rect(root, CardArt.Solid(), Full, "Menu Vignette", QVignette, VignetteColor);

            // ⑨ **`Open()` 那五句可见性**（逐个字段对上反编译；期望值见文件头「三组状态」）
            // 🔴 **2026-10-11（批次1 · F7）就地订正（铁律 5）**：这四句原来**全读 `IsPremiumLocked`** ——
            //    **偏移读错了**：`d:/2/tools/decomp_full/RewardWindow__Open.c` 里只有 `Premium Disclaimer`
            //    读 `0x18`（`:164-185`），**其余全部**读 **`0x19` = `IsPreview`**：
            //      两态底图/底光 `:205-217` · `Tap To Continue` `:189` · `claimRewardParticles` `:280` ·
            //      播不播揭示动画 `:283`。
            //    ⚠️ 两个 flag 在既有三档夹具里恒等 ⇒ 反了**看不出来**；判别档见 `Editor/RewardsScene.cs`。
            ConfigureIsPreviewState(ctx.IsPreview);
            _premium.SetActive(ctx.IsPremiumLocked && HasPremium(ctx.Rewards));
            _tap.SetActive(!ctx.IsPreview);
            _collect.SetActive(ctx.OnCollect != null);

            // ⑩ 物品 + 揭示动画（`!IsPreview` 才播；预览态遮罩恒开 —— `RewardWindow__Open.c:283`）
            _reveal = ctx.IsPreview ? 1f : 0f;
            BuildItems();
            StartPunches();     // = `DoRewardAnimation` 后半段那一圈 `DOPunchScale`（预览态自己会早退）

            if (MissingArt.Count > 0)
                Debug.LogWarning("[RewardWindow] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray())
                                 + " —— 导入器：`工具/import_original_art.py`");
            if (NoIconItems.Count > 0)
                Debug.LogWarning("[RewardWindow] ⚠️ 有 " + NoIconItems.Count + " 个物品**本地没有图标**"
                                 + "（画的是占位板）：" + string.Join("、", NoIconItems.ToArray()));
            // 🔴 **没做的要出声**（`CLAUDE.md` §三「不许静默失败」）。
            // 🆕 **2026-10-11（批次1 · W1）就地订正（铁律 5）**：这一段原来报「四处没做」，其中两条已经不成立：
            //   · **逐件 punch 做了**（`StartPunches`，判据齐 —— 见那段注释的四个字面量）；
            //   · **入口接上了**（`ShowCollected` ← `DailyData.CollectReward`/`CollectStreak`，那条路玩家走得到）；
            //   · 而原来写的「素材/插件不在本工程」**是假的**：那**六套**粒子（`Wave left` / `Wave right` /
            //     两个 `Wave Shine` / 两个 `Trails`）的 `ParticleSystem` 与 `ParticleSystemRenderer`
            //     **都在 `d:/2/新解包资源/assets_full/bundle_menus_assets_all/` 里**（逐节点实读），
            //     `particleOnAppear` 那个模板也在同一个包里 —— 缺的是**把这套粒子重建出来**（属特效还原线）。
            //     🔴 **2026-10-11（批次2 · F2）就地订正（铁律 5）**：这里原来写「**五**套」（与括号里那 6 个名字
            //     自相矛盾），运行期那句 `Debug.Log` 也跟着写「5 个」——**两处都错**。按**组件 PathID 反查**重数
            //     了一遍（⛔ 别走「拿 GameObject 的 PathID 去找文件名」那条路：名字唯一的对象只叫 `<名字>.json`，
            //     那条路对 `Wave right` / `Trails` 恒找不到 ⇒ 会少算）：`Reward Claim` 子树 =
            //     **6 个 `ParticleSystem` ＋ 6 个 `ParticleSystemRenderer`**，形状 =
            //     `Reward Claim` → `Wave left` → {`Wave Shine`、`Trails`、`Wave right` → {`Wave Shine`、`Trails`}}。
            Debug.Log("[RewardWindow] 本窗还有 **2 处**原版有、我们没做（判据齐，别当已实现）："
                      + "① **两处 3D 粒子**，只 `SetActive` 不播放 —— `Reward Claim`（挂在 `Content` 下，"
                      + "下面 6 个 `ParticleSystem`/6 个 `ParticleSystemRenderer`，另有 1 个带 `sounds` 的模块）"
                      + "与 `particleOnAppear`（运行期 `Instantiate` 到**每个抽屉的 transform 下**、"
                      + "延迟 = 同一个归一化距离 × 0.75 —— 见 `RewardWindow__DelayedParticlePlay`）；"
                      + "② `soundOnAppear`（`0xE0`）**本窗没播** —— 反编译里读不到它的调用点，判据没查清。"
                      + "⚠️ 而**开场揭示（0.8s 遮罩线性收缩）+ 逐件 punch + `Tap To Continue` 的闪烁"
                      + "＋抽屉层那三跳（`TogglePremiumHighlight` / `SetEphemeralDisplay` / `SetConvertedItem`）"
                      + "＋ `!Stackable` 按数量展开成 N 格都做了**（后两件 = 2026-10-11 A311，在 `ItemDrawer` 层）；"
                      + "`Tick(dt)` 由 `Update` 驱动，批处理里自检直调。");
        }

        /// <summary>按奖励表重建物品格（滚动 / 揭示动画每次推进都走它 —— 与 `MenuScroll.OnChanged` 是同一件事）。</summary>
        void BuildItems()
        {
            if (_listHolder == null) return;
            for (int i = _listHolder.childCount - 1; i >= 0; i--)
                RewardsWindow.DestroySafe(_listHolder.GetChild(i).gameObject);

            var list = _ordered ?? new CampaignData.RewardSpec[0];
            // 🆕 **2026-10-11（A311）**：排版的单位是**格**（第 4 跳展开之后的那个表），不是「奖励条数」。
            //   无展开时 `_cells.Length == list.Length`、`_cells[i].Reward == i` ⇒ 一切照旧。
            if (_cells == null) _cells = ExpandCells(list);
            var cells = _cells;

            // 内容宽 = padL + Σ格 + 间隔 + padR（HLG 的首选宽，`ContentSizeFitter(MinSize)` 取它与 120 的较大者）
            float contentW = Mathf.Max(MinContentW, PadL + PadR + cells.Length * ItemW
                                                    + Mathf.Max(0, cells.Length - 1) * ItemSpacing);
            var cr = CenteredContent(contentW);
            _listHolder.localPosition = MenuDraw.Local(_listHolder.parent, cr.x1, cr.y1, cr.x2, cr.y2);
            if (_scroll != null)
            {   // 两端跟着内容走 ⇒ 范围 = `[−Δ/2, +Δ/2]`（内容比视口窄时两头都是 0 = 滚不动，`AdjustBounds` 那条）
                // 🔴 **这里【直接写 `Offset` 字段】、不走 `SetOffset`** —— 后者变了就发 `OnChanged`
                //    （= 又调回本函数）⇒ 内层那趟会把外层的格子删掉重建成两套（静默）。写字段后**只夹一次**。
                _scroll.ContentX1 = cr.x1; _scroll.ContentX2 = cr.x2;
                _scroll.Offset = Mathf.Clamp(_scroll.Offset, _scroll.ClampLo, _scroll.ClampHi);
            }
            float shift = _scroll != null ? _scroll.Offset : 0f;
            float cy = (cr.y1 + PadT + cr.y2 - PadB) * 0.5f;         // `MiddleCenter` 的竖向中心

            // 🔴 **三件套成对拿捏**（`Clip` / `ClipSoftness` / `ClipPad`）—— 只有这一段在视口里：
            var prevClip = Clip; var prevSoft = ClipSoftness; var prevPad = ClipPad;
            Clip = ScrollView; ClipSoftness = ScrollSoft;
            ClipPad = MaskPad;                     // 揭示动画那一刀（静止时 = (0,0,0,0)）
            _itemNodes.Clear();
            for (int i = 0; i < cells.Length; i++)
            {
                var spec = list[cells[i].Reward];
                float x1 = cr.x1 + PadL + i * (ItemW + ItemSpacing) - shift;
                var box = new PxRect(x1, cy - ItemH * 0.5f, x1 + ItemW, cy + ItemH * 0.5f);
                // 「原位那一格」= 这一条奖励的**第一格** —— 原版的第 2/3 跳**只对它调**
                // （展开出来的那几格原版**只调第 1 跳**，见 `RewardWindow__Open.c:133-147`）。
                bool first = (i == 0) || (cells[i - 1].Reward != cells[i].Reward);
                var node = BuildItem(_listHolder, box, spec, cells[i].Quantity, first);
                _itemNodes.Add(node);
                // 🆕 A310①：这一格**此刻**的 punch 值（重建之后要把它的缩放补回去 —— 见 `StartPunches`）
                if (node != null) node.localScale = PunchScaleOf(i);
            }
            Clip = prevClip; ClipSoftness = prevSoft; ClipPad = prevPad;
        }

        /// <summary>一格奖励的**图**：先走数据层那张「id → 图」表（`CampaignData.ItemIcon`，
        /// 战役 / 卡包那些 id 靠它）；
        /// 🔴 **2026-10-11（批次1 · W1）补的兜底（**我们挑的**，不是原版口径）**：日常线那边**没有服务端 item id**，
        /// 奖励是按**菜单图名**记的（`Wallet.Grant(art, n)` ⇒ 例如 `40k_topmarquee_currency_gold`）。
        /// 那张表认不出时，再认一次「id 本身就是一张菜单图名」——
        /// ⛔ 别把这条兜底当成「`RewardInfo.targetId` 可以随便填」：它只补我们自己的数据模型缺口，
        /// 认不出的仍然走占位板 + 出声（红线：不许静默）。</summary>
        static string ArtOf(string id)
        {
            string art = CampaignData.ItemIcon(id);
            if (art != null) return art;
            return CardArt.MenuUi(id) != null ? id : null;
        }

        /// <summary>一格奖励 —— **走抽屉库**（`Shell/ItemDrawer.cs`，判据只此一份）。
        /// 原版那句是 `ItemDrawer.Draw(listHolder, r.Item, r.Quantity, DrawerOverride.Default)`（`__Open.c` 实读）。
        /// <para>🆕 **2026-10-11（批次2 · A311）**：建完之后**紧接着调那三跳** —— 原版 `RewardWindow.Open` 的次序是
        /// `ItemDrawer.Draw(...)` → ① `TogglePremiumHighlight(tier == 10)` →
        /// ②/③ `SetEphemeralDisplay` **或** `SetConvertedItem`（**互斥**：`convertedInto != null` 才走 ③，
        /// 否则才判 `IsEphemeral`）—— 判据 = `RewardWindow__Open.c:104-132` 逐句 + 现读的指令流。</para>
        /// <param name="quantity">这一格画的 `quantity`（第 4 跳展开出来的那几格 = 1）。</param>
        /// <param name="first">这一格是不是该奖励的**第一格** —— 三跳里 **②③ 只对第一格调**（原版如此）。</param>
        /// <returns>刚建出来的抽屉节点（`null` = 这一格没建出来 —— `ItemDrawer.Draw` 的空结果那一支）。</returns>
        Transform BuildItem(Transform parent, PxRect box, CampaignData.RewardSpec spec, int quantity, bool first)
        {
            var item = ItemDrawer.Spec(spec.Id, ArtOf(spec.Id), CampaignData.ItemShortName(spec.Id));
            var st = ItemDrawerStyle.Default(QItem, QItemIcon, QItemIcon);
            st.IconFill = ItemIconPx / Mathf.Min(ItemW, ItemH);           // = 0.7（出处只有 `ItemIconPx` 一条）
            st.NodeName = "Item_" + CampaignData.ItemShortName(spec.Id);  // 自检按 `Item_` 前缀数格子
            st.Clip = RenderClip;                 // = 视口按 `ClipPad` 内缩（揭示动画期间会被收窄）
            st.ClipSoftness = ClipSoftness;       // (200,0) —— 与图/字**同一份**软边（A238 / A302）
            st.QDecor = QDecor;                   // 三个装饰层的队列（见 `QDecor` 那条注释）
            // ---- 第 1 跳：`TogglePremiumHighlight(spec.Tier == 10)`（**每一格都调**，展开出来的那几格也调）
            st.Premium = (spec.Tier == CampaignData.TierPremium);
            // ---- 第 2/3 跳（**只对第一格**）：`convertedInto != null` ⇒ ③，否则 `IsEphemeral` ⇒ ②
            bool conv = first && spec.ConvertedInto.HasValue;
            bool eph = first && !spec.ConvertedInto.HasValue && spec.IsEphemeral;
            if (conv)
            {
                st.Converted = true;
                st.ConvertedQuantity = spec.ConvertedInto.Value.Quantity;
                st.ConvertedArt = ArtOf(spec.ConvertedInto.Value.Id);
            }
            else if (eph) { st.Ephemeral = true; st.EphemeralMs = spec.EphemeralMs; }

            var res = ItemDrawer.Draw(parent, box, item, quantity, DrawerOverride.Default, st);

            if (res.Placeholder && !NoIconItems.Contains(spec.Id)) NoIconItems.Add(spec.Id);
            if (res.FallbackArt && !FallbackArtItems.Contains(spec.Id)) FallbackArtItems.Add(spec.Id);
            if (res.Node == null) return null;
            if (st.Premium) ItemDrawer.SetPremium(res.Node, box, st);
            if (conv) ItemDrawer.SetConverted(res.Node, box, st);
            else if (eph) ItemDrawer.SetEphemeral(res.Node, box, st);
            return res.Node;
        }

        /// <summary>**第 4 跳**：`!Options.Stackable` **且 `convertedInto == null`** 时，一条奖励要
        /// **再画 `quantity − 1` 格**（`RewardWindow__Open.c:133-147`：循环上界 `reward.Quantity − 1`，
        /// 循环体 `ItemDrawer.Draw(parent, item, **1**, Default)` + **每个都照样 `TogglePremiumHighlight`**）。
        /// 返回**展开后的格表**（没展开的奖励就是它自己那一格）。
        /// <para>⛔ 判据（`stackable`）**不住在这里** —— 它是 `ItemDrawer` 层的事
        /// （`ItemDrawer.IsStackable`，表来源 = `ItemDrawerConfig` 的 `options`），这边只按它排版。
        /// ⚠️ 原版那个条件里**还有一半我们做不到**：`stackable` 是从**那个抽屉的 `options`** 读的，
        /// 而我们的 `ItemDrawerStyle` 不表达 `options` ⇒ 见 `ItemDrawer.ExpandsByQuantity` 的注释。</para>
        /// <remarks>**改坏法**：把 `spec.Quantity &gt; 1` 那道守卫删掉 ⇒ `IsStackable` 会被逐格调用（那些格
        /// `Quantity == 1`）⇒ 「`quantity 1` 的奖励不展开」那条自检红；把 `spec.ConvertedInto.HasValue`
        /// 那个否定删掉 ⇒ 自检「converted 的那条**不**展开」红。</remarks>
        static Cell[] ExpandCells(CampaignData.RewardSpec[] list)
        {
            var cells = new List<Cell>();
            if (list == null) return cells.ToArray();
            for (int i = 0; i < list.Length; i++)
            {
                var spec = list[i];
                int n = 1;
                if (spec.Quantity > 1 && !spec.ConvertedInto.HasValue)
                {
                    var item = ItemDrawer.Spec(spec.Id, ArtOf(spec.Id), CampaignData.ItemShortName(spec.Id));
                    if (ItemDrawer.ExpandsByQuantity(item, DrawerOverride.Default)) n = spec.Quantity;
                }
                for (int k = 0; k < n; k++)
                    cells.Add(new Cell { Reward = i, Quantity = n == 1 ? spec.Quantity : 1 });
            }
            return cells.ToArray();
        }

        /// <summary>照 `ConfigureIsPreviewState(bool)`：**四个件**（两态底图 + 两团底光）二选一。
        /// 参数是 **`IsPreview`**（`RewardWindow__Open.c:205` 读的是 `0x19`，那四句 `SetActive` 全由它派生）。
        /// 🔴 **2026-10-11（批次1 · F7）就地订正（铁律 5）**：这里原来写「参数是 `IsPremiumLocked`
        /// （`Open()` 里那四句 SetActive 的入参实读）」—— **偏移读错了**，实读的是 `0x19` = `IsPreview`。</summary>
        public void ConfigureIsPreviewState(bool isPreview)
        {
            SetOn(_bgGet, !isPreview); SetOn(_glowGet, !isPreview);
            SetOn(_bgPrev, isPreview); SetOn(_glowPrev, isPreview);
        }

        static void SetOn(GameObject go, bool on) { if (go != null) go.SetActive(on); }

        /// <summary>= 原版 `Open()` 里那句 `context.Rewards.OrderBy(r =&gt; r.RewardTier).ToList()`
        /// （`RewardWindow__Open.c:69`：`FUN_180c99af0(source, keySelector, mi)` → `Enumerable.ToList`）。
        /// <para>🔴 **2026-10-11（批次1 · W1 · A310②）**：这条原来只是**推断**（上一轮的普查报告 §五·4 记着
        /// 「那个泛型 LINQ 助手符号丢了，分不出是不是 `OrderBy`」）。**现在坐实了** —— 判据 = **那个助手的地址**：
        /// `FUN_180c99af0` 的 RVA = `0xC99AF0`，在 `d:/2/tools/il2cpp_out/dump.cs` 的 `GenericInstMethod` 表里
        /// **正落在 `|-Enumerable.OrderBy&lt;object, Int32Enum&gt;` 那一行**（邻居一目了然：
        /// `OrderBy&lt;object,bool&gt;` @0xC99A60 · `OrderBy&lt;object,int&gt;` @0xC99B80 · `OrderBy&lt;object,float&gt;` @0xC99D30）。
        /// 键选择器那一支的签名是 `RewardInfo → RewardTier`（`dump.cs` 里 `RewardWindow.&lt;&gt;c.&lt;&gt;9__20_0`）
        /// —— `RewardTier` 正是 `Int32Enum` ⇒ **`OrderBy(r =&gt; r.RewardTier)`，升序**。</para>
        /// <para>⚠️ 稳定性：LINQ `OrderBy` 是**稳定排序**（键相等时保持原相对次序）⇒ 这里也手工写成稳定排序
        /// （`Array.Sort` 不保证稳定）。奖励条数是个位数，代价无关紧要。</para></summary>
        public static CampaignData.RewardSpec[] OrderByTier(CampaignData.RewardSpec[] src)
        {
            var a = (src != null) ? (CampaignData.RewardSpec[])src.Clone() : new CampaignData.RewardSpec[0];
            for (int i = 1; i < a.Length; i++)          // 插入排序：只在前面的键**严格更大**时才后移 ⇒ 稳定
            {
                var v = a[i];
                int j = i - 1;
                while (j >= 0 && a[j].Tier > v.Tier) { a[j + 1] = a[j]; j--; }
                a[j + 1] = v;
            }
            return a;
        }

        /// <summary>`premiumWarning` 那一句的谓词（`<>c__<Open>b__20_1`：`r.RewardTier == 10`）。</summary>
        static bool HasPremium(CampaignData.RewardSpec[] rw)
        {
            if (rw == null) return false;
            for (int i = 0; i < rw.Length; i++) if (rw[i].Tier == CampaignData.TierPremium) return true;
            return false;
        }

        // ============================================================ 小工具

        /// <summary>`Viewport/Content` 的矩形：宽 = 内容宽、**水平居中在视口里**（拉伸锚 + pivot .5 + CSF）。</summary>
        static PxRect CenteredContent(float w)
        {
            float cx = (ScrollView.x1 + ScrollView.x2) * 0.5f;
            return new PxRect(cx - w * 0.5f, ScrollView.y1, cx + w * 0.5f, ScrollView.y2);
        }

        GameObject RectGo(Transform parent, Texture2D tex, PxRect r, string name, int q,
                          Color? tint = null, bool keepAspect = false)
        {
            var quad = MenuDraw.Rect(parent, tex, r, name, q, tint, keepAspect);
            return quad != null ? quad.gameObject : null;
        }

        /// <summary>底光里那一段字（`Text *` 是 `Glow *` 的子件；文字盒 = 底光盒）。</summary>
        Label GlowText(GameObject glow, string name, string s)
        {
            if (glow == null) return null;
            return Text(glow.transform, Glow, s, Color.white, name, TitleFont, QTitleText, Glow.W, TitleAutoMin);
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
            var ctx = _ctx;
            sb.Append("RewardWindow：");
            sb.Append(ctx == null ? "**没有 context** " : ((ctx.Rewards != null ? ctx.Rewards.Length : 0) + " 条奖励"));
            sb.Append(" · 态 ").Append(IsPremiumLocked ? "PremiumLocked" : "Ok")
              .Append(IsPreview ? "/Preview" : "/Collect")
              .Append(" · 揭示 ").Append(_reveal.ToString("F2"))
              .Append(" · pad ").Append(MaskPad.ToString())
              .Append(" · 滚动 [").Append(_scroll != null ? _scroll.ClampLo.ToString("F1") : "-")
              .Append(", ").Append(_scroll != null ? _scroll.ClampHi.ToString("F1") : "-").Append("]")
              .Append(" · 无图标物品 ").Append(NoIconItems.Count)
              .Append(" · 缺图 ").Append(MissingArt.Count).Append(" 张")
              .Append(" · 退档图 ").Append(FallbackArtItems.Count).Append(" 个");
            return sb.ToString();
        }
    }
}
