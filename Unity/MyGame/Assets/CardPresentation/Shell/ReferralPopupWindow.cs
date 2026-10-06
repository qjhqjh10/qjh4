// ReferralPopupWindow.cs — **推荐人窗**（原版 `ReferralPopupWindow : GameWindow`）
//
// ⚠️ **它不在 `menus` 包里** —— 原版 prefab 在 **`bundle_generalgamewindows_assets_all`**
//    （派活/复查时最容易踩的一格：`bundle_menus_assets_all` 里**没有**它）。
//
// ============================ 出处（唯一正本）============================
// prefab：`d:/2/新解包资源/assets_full/bundle_generalgamewindows_assets_all/` 根 GO `Referral Popup`
//   · 窗口参数 MB = `MonoBehaviour_8365794629712173860` 逐字段实读（见下面「窗参」）；
//   · 逐节点几何 / 字号 / 九宫 / 出厂显隐 =
//     `python d:/4/Unity/工具/menu_dump.py bundle_generalgamewindows_assets_all "Referral Popup" --depth 12 --relative --md`
//     🔴 **本扇的根是「拉伸」根**（锚 `(0,0)-(1,1)`、`sizeDelta (0,0)`）⇒ `--relative` 与绝对框**同一套数**
//     （根自己就是 `0,0→1920,1080`），两档无需换算。
//   · 逐节点**原读副本**（我自己的读法，独立于 menu_dump）：
//     `d:/4/_tmp_view/wl3/prefab_read.py` → `d:/4/_tmp_view/wl3/recon_read.json`
//     —— **78 个节点**、50 颗 `marker`（兄弟序照 `m_Children`，⛔ 不是按 RectTransform 目录扫）。
//   · 可复现对账：`PYTHONIOENCODING=utf-8 python d:/4/_tmp_view/wl3/check_table.py`
// 行为 = 反编译 `d:/2/tools/decomp_full/ReferralPopupWindow__{Start,Open,Refresh,OnSetReferrer}.c`
//   ＋ 两个闭包 `<Start>b__10_0` / `<OnSetReferrer>b__13_0`
//   ＋ 字段偏移表 `d:/2/tools/il2cpp_out/dump.cs`（`ReferralPopupWindow`，TypeDefIndex 2358）。
//
// ============================ 窗参（MB 逐字段实读）============================
// `type = 1`(Popup) · `windowsPlacement = 15`(Popup) · `openSound/closeSound = null` ·
// `useDefaultCloseSoundIfNull = 1` · `closeOnESC = 1` · `updateNavPanel = 0` ·
// 🔴 **`extraScaleSmallScreen = 1.350000023841858`**（不是 1.0 —— 这一扇在小屏真会被放大 1.35；
//    逐扇实读，同族 `GenericOptionsPanel` 那个 1.0 是「不覆盖」的另一档）。
// 根 GO 上**只有一颗组件**（`RectTransform` + `ReferralPopupWindow`）—— ⛔ 没有烤着的
// `TransformScalerBySmallScreenUI`（「窗口根上带成品的 3 扇」不含它）⇒ 放大那一支由基类
// `GameWindow.ApplySmallScreenScale()` 走 `AddComponent` 补（判据 → `Shell/WindowsManager.cs` 的 `GameWindow.ApplySmallScreenScale`）。
// 字段 → 节点（`dump.cs` 的偏移表 + 逐个 pid 反查，8/8 对得上）：
//   `+0x70 referralCounter → counter number`      · `+0x78 referralCounterSteps → counter 下 50 颗 marker` ·
//   `+0x80 inputView → Input View`                · `+0x88 referredView → Referred View` ·
//   `+0x90 referredPlayerName → Referred View/Name` · `+0x98 referredPlayerInput → Input View/input` ·
//   `+0xA0 errorMessage → Input View/error`       · `+0xA8 referButton → Input View/Generic Simplified UI Button` ·
//   `+0xB0 defaultColor` · `+0xC0 achievedColor`（两个 `Color` 字段，值是下面那两个常量）。
//
// ============================ 🔴 原版的显隐/颜色模型（`Refresh()` 逐句读出来的）============================
// `referrer = ReferralManager.GetReferrer()`、`has = (referrer != null)`：
//   ① `inputView.SetActive(!has)` · `referredView.SetActive(has)`；
//   ② `has` ⇒ `referredPlayerName.SetText(referrer.Name)`（`ReferralEntry` 的 `+0x18`）；
//   ③ `referButton.interactable = !has`；
//   ④ `count = ReferralManager.GetReferrals().Count(x => !x.IsRedeemed)`
//      （predicate = `ReferralPopupWindow.<>c.<Refresh>b__12_0`，逐字是 `!x.IsRedeemed`）；
//   ⑤ `max = ConfigManager.GetConfig<...>()` 的 `+0x18`（**远端 LiveOps 配置**）：
//      `count < max` ⇒ `string.Format(GetTranslation("MenuShop/referral/counter"), count)`；
//      否则 ⇒ `GetTranslation("MenuShop/referral/counterMaximum")`（**不带占位**）；
//   ⑥ `errorMessage.SetText("")`（**清空** —— 那个字面量我从 `stringliteral.json` 读出来了：
//      `DAT_1842b80e0` → RVA `0x42B80E0` → **空串**）；⑦ 逐颗 `marker` 的 `Outline.effectColor`：
//      `i < count` ⇒ `achievedColor`，否则 ⇒ `defaultColor`（**50 颗**全刷一遍）。
// `Start()`（**两条监听，逐句读**）：
//   · `referButton.onClick`（= uGUI `Button.m_OnClick`，`EverguildButton : Button` 的 `+0x100`）
//     → `{{OnSetReferrer}}`（`UIGenericEventCatcher.SourceDelegate.Invoke()` **无参** ⇒ 只能是它）；
//   · `referredPlayerInput.m_OnValueChanged`（`TMP_InputField +0x1D0`，`OnChangeEvent : UnityEvent<string>`）
//     → `{{<Start>b__10_0(string _)}}` ⇒ **改字就清掉错误提示**（参数丢弃）。
//   ⚠️ 那两个 `___ctor` 的**委托类名**（`UIGenericEventCatcher.SourceDelegate` /
//      `EverguildDropfield.DropDelegate`）有一处与签名对不上（`DropDelegate.Invoke(PointerEventData)` 收的不是
//      `string`）—— 我按**事件字段的类型**认（`m_OnValueChanged` 是 `UnityEvent<string>`，与 `b__10_0(string)`
//      严丝合缝），如实标在报告 §八。
// `OnSetReferrer()`：`referButton.interactable = false` → `ReferralManager.SetReferrer(input.text, 成功、失败两跳)`；
//   失败 ⇒ `<OnSetReferrer>b__13_0(err)`：`errorMessage.SetText(GetTranslation("CustomErrors/" + err))`
//   ＋ `referButton.interactable = true`（`"CustomErrors/"` 那个前缀也是从 `stringliteral.json` 读的，RVA `0x425CCA8`）。
//
// ============================ 🔴 两处「原版自己就是这样」（不是我们的近似）============================
// ① **`counter` 出厂就是关着的，而且运行期没有任何代码把它打开**（现读三条证据，缺一不可）：
//    · prefab 里 `counter` 的 `m_IsActive = 0`（`menu_dump` 印 `INACT` / `F`，`prefab_read.py` 印 `act=0`）；
//    · 窗口的十个序列化字段**没有一个是它**（偏移表见上），`Refresh()` 只 `SetActive` 了 `Input View`/`Referred View`；
//    · 全包按 pid 搜：`counter` 的 GO（`-7485178439993369820`）除了**它自己的** `HorizontalLayoutGroup`
//      与 AssetBundle 清单之外**零引用** ⇒ 父链上也没有 Animator（那棵树上一个 `Animation`/`Animator` 都没有）。
//    ⇒ **原版成品里那 50 颗 `marker` 一个像素都不画**。我们**照建**（节点数必须与 prefab 一致）＋**建成关着**
//      ＝ 与原版成品同一档（同 `debug_buttons` / `DebugEventTogglerWindow` 那条「不可达 ⇒ 照原版不画」的先例）。
// ② **`Referred View` 出厂也是关着的**（`m_IsActive = 0`）—— 但**它有**一组运行期开它的代码（`Refresh()` ①），
//    所以它是**两态**的那一件（`Apply()` 会开它）。两件别混：一件可开、一件不可开。
//
// ============================ 🔴 文本：prefab 出厂串**是俄文**（照抄，不换）============================
// 这扇窗的 `Localize.mTerm` 逐条我读出来了（下面 `Txt*` 常量旁的注释）—— 原版运行期由 I2 词条覆盖，
// 而**词条表在远端 CCD、本地一个 value 都没有**（同全仓口径）⇒ 只能照抄 prefab 里印着的那串。
// 🔴 **它印的是俄文**（`Реферальная программа` / `Пригласите своих друзей!` / …），
//    只有 `Input View/error` 与 `counter number` 印的是英文。**这是资产里的真字符串**，不是我编的。
//    ✅ **俄文字形能画出来**：本工程字体 = `Resources/Fonts/NotoSerifCJK-Regular SDF`（`m_AtlasPopulationMode = 1`
//    = **Dynamic**，字符表为空、按需光栅化），源字体 `Assets/CardPresentation/Fonts/NotoSerifCJK-Regular.ttf`
//    的 `cmap` 里 **U+041F / U+0435 都在**（本件现读，见报告 §九）。
//
// ============================ 🔴 两处「我们算的，不是照抄」（铁律 3）============================
// ① **`counter` 与它下面 50 颗 `marker` 的矩形**：`counter` 是**关着**的 ⇒ uGUI **不会**跑它那颗
//    `HorizontalLayoutGroup`，序列化值就是运行期值 ⇒ 取 prefab 原值算出来的绝对框（`counter` 那一格
//    与 `menu_dump` 印的**逐位相同**，可反证）。`marker` 的 50 个 `ap.x` 逐位读出来是 **`8.59 + 17.18 i`**
//    （float32 序列化噪声最大 **0.00027px**）⇒ 我用**一条算式**生成，⛔ 没有抄 50 行 —— 这是本文件唯一
//    用算式代替逐格的那一处，对账脚本会把 50 格逐格比一遍（不符必须是 0）。
// ② **`Referred View` 两个子件的框**：那一颗 `HorizontalLayoutGroup` 也是**关着**的
//    （`counter` 同理），而 `childControlWidth = 1` ⇒ **宽由布局写**、而布局要 TMP 的首选宽度（字体度量）
//    ⇒ `menu_dump` 在那一格印的是 **0.00 宽**（它模拟跑了一遍、量不出字宽）。我取的是 **prefab 序列化值**
//    （`Label` 211.14 × 72.697 · `Name` 155.35 × 72.697，逐位实读）—— 那也是原版**那一刻**的真值。
//    ⚠️ 一旦原版把 `Referred View` 打开，uGUI 会把这两个孩子的框**重写一遍**（宽按各自字宽）⇒
//    「有数据」那一档的精确框**算不出来**（要 Unity 的字体度量）⇒ 我们那一档**保留序列化框**并出声。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `ReferralPopupWindow`（`GameWindow` 子类）—— 推荐人窗
    /// （输入推荐人名字 / 看自己的推荐人 / 50 颗进度点 / 「我收集了 N 份奖励」）。
    /// <para>🔴 **它不在任何一页的层带里**：自成一档 **3348–3361**（A251 那一族里
    /// `AllianceMemberOptionsPopup` 3340–3347 之上、挑战弹窗 `DuelPopupWindow` 3400–3405 之下 —— 层带不许重叠）。</para>
    /// <para>⚠️ **它不在 `menus` 包里** —— 原版 prefab 在 `bundle_generalgamewindows_assets_all`。</para></summary>
    public class ReferralPopupWindow : GameWindow
    {
        // ============================================================ 队列档（本窗自成一档 · 3348–3361）
        //   逐层顺序 = **原版兄弟序**（uGUI 按兄弟序画 ⇒ 后面的压前面的；判据 = prefab 的 `m_Children`）：
        //   根 = [Menu Dark Background, window]；
        //   window = [Generic Window Red Background Big, Generic Close Button Orange, content]；
        //   content = [Referral Title, Input View, Referred View, Divisor line members, spacing (1),
        //              Title, Descripton, counter number, counter]。
        public const int QShade = 3348,          // 压暗整屏（`Menu Dark Background`）
                         // ⚠️ **压暗层的命中区【与它同档】**（`MenuDraw.ShadeHit` 的 `qShade` 实参就是 `QShade`）——
                         //    `MenuDraw.ShadeRuleOk` 现场比的就是「命中 quad 的档 == 视觉压暗层那颗 quad 的档」
                         //    ⇒ ⛔ 别为它另立一个 `QShade+1` 的常量（那样这条不变量**必红**）。
                         QWinBg = 3349,          // `window/Generic Window Red Background Big`
                         QClose = 3350,          // `Generic Close Button Orange`（底 + 它的 `Background`/`Icon`）
                         QText = 3351,           // `content` 下那一列字（逐个不重叠 ⇒ 同档）
                         QInputBg = 3352,        // `input`
                         QPlaceholder = 3353,    // └ `Text Area/Placeholder`
                         QInputText = 3354,      // └ `Text Area/Text`
                         QBtn = 3355,            // `Generic Simplified UI Button`
                         QBtnText = 3356,        // └ `Button Text`
                         QError = 3357,          // `error`
                         QMarkerOutline = 3358,  // `marker` 的 4 份 `Outline` 副本（uGUI 的 `Shadow` 画在**本体之前**）
                         QMarker = 3359;         // `marker` 本体（黑方块）
        /// <summary>本窗**内容命中区**那一档（压暗层命中区**严格低于**它 —— `MenuDraw.ShadeHit` 现场核）。
        /// ⚠️ 面板的吸收层 = `QHit − 1` = **3360**（`MenuDraw.Absorb` **自己算**，调用方不许挑）——
        /// 它比 `marker` 高、比命中区低，**三段互不同档**（同档时谁吃到命中退化成枚举顺序）。</summary>
        public const int QHit = 3361;

        // ============================================================ 几何（绝对 = 现读 · 画布 px）
        //   根是拉伸根（`(0,0)-(1,1)`、`sizeDelta (0,0)`）⇒ 整屏 0,0→1920,1080。
        static readonly PxRect ShadeR   = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);
        static readonly PxRect WinR     = new PxRect(364.03f, 287.11f, 1555.97f, 900.68f);      // 1191.9448×613.5716
        static readonly PxRect PaneR    = new PxRect(503.93f, 287.11f, 1434.17f, 900.68f);      // 930.236×613.5716
        static readonly PxRect CloseR   = new PxRect(1361.78f, 287.11f, 1436.17f, 362.72f);     // 74.386×75.605
        static readonly PxRect CloseArtR= new PxRect(1369.93f, 295.09f, 1426.79f, 353.22f);     // 56.86×58.13
        static readonly PxRect ContentR = new PxRect(526.04f, 327.79f, 1397.11f, 839.83f);      // 871.068×512.032
        static readonly PxRect RefTitleR= new PxRect(526.04f, 327.79f, 1397.11f, 380.93f);      // 871.068×53.1405
        static readonly PxRect InputViewR = new PxRect(526.04f, 385.93f, 1397.11f, 560.93f);    // 871.068×175
        static readonly PxRect InLabelR = new PxRect(560.37f, 385.93f, 1362.79f, 438.16f);      // 802.42×52.2205
        static readonly PxRect InputR   = new PxRect(560.37f, 438.16f, 1362.79f, 483.16f);      // 802.425×45
        static readonly PxRect TextAreaR= new PxRect(570.37f, 445.16f, 1352.79f, 477.16f);      // 782.43×32
        static readonly PxRect InSpacingR = new PxRect(781.94f, 483.16f, 1141.21f, 496.13f);    // 359.27×12.97
        static readonly PxRect BtnR     = new PxRect(871.53f, 496.13f, 1051.62f, 544.86f);      // 180.086×48.7317
        static readonly PxRect BtnTextR = new PxRect(877.74f, 498.64f, 1045.59f, 542.37f);      // 167.85×43.73（ARF 跑完）
        static readonly PxRect ErrorR   = new PxRect(757.27f, 544.86f, 1165.89f, 569.07f);      // 408.626×24.2125
        static readonly PxRect DivisorR = new PxRect(526.04f, 565.93f, 1397.11f, 569.70f);      // 871.068×3.77
        static readonly PxRect Spacing1R= new PxRect(526.04f, 574.70f, 1397.11f, 587.67f);      // 871.068×12.97
        static readonly PxRect TitleR   = new PxRect(526.04f, 592.67f, 1397.11f, 644.67f);      // 871.068×52
        static readonly PxRect DescrR   = new PxRect(526.04f, 649.67f, 1397.11f, 789.67f);      // 871.068×140
        static readonly PxRect CntNumR  = new PxRect(526.04f, 794.67f, 1397.11f, 820.17f);      // 871.068×25.4988
        static readonly PxRect CounterR = new PxRect(532.08f, 481.45f, 1391.08f, 543.64f);      // 859×62.1965（关着）
        static readonly PxRect RefViewR = new PxRect(526.04f, 588.43f, 1397.11f, 763.43f);      // 871.068×175（出厂关着）
        /// <summary>`Referred View/Label` —— **prefab 序列化值**（那颗 HLG 关着 ⇒ 布局不跑；
        /// 见文件头「两处我们算的」②。`menu_dump` 在这一格印的是模拟布局的 0 宽，**不取**）。</summary>
        static readonly PxRect RefLabelR = new PxRect(770.87f, 647.08f, 982.01f, 719.78f);      // 锚(0,1) piv(0,0.5) sd 211.14×72.697 ap 244.829,−95
        /// <summary>`Referred View/Name` —— 同上（序列化值）。</summary>
        static readonly PxRect RefNameR = new PxRect(996.93f, 647.08f, 1152.28f, 719.78f);      // 锚(0,1) piv(0,0.5) sd 155.35×72.697 ap 470.889,−95
        /// <summary>`counter/marker` 的 **50 格网格**：第 1 格 `x1 = 535.67`、步长 **17.18**（prefab 的
        /// `m_AnchoredPosition.x = 8.59 + 17.18 i` 换算到画布 —— 见文件头「两处我们算的」①）；
        /// `y = 507.54→517.54`（10×10）**50 格全同**（逐位实读）。</summary>
        public const float MarkerX0 = 535.67f, MarkerDx = 17.18f, MarkerY1 = 507.54f, MarkerSize = 10f;
        /// <summary>`marker` 的颗数（= prefab 的 `referralCounterSteps` 数组长，**现读 = 50**）。</summary>
        public const int MarkerCount = 50;
        /// <summary>`Text Area` 那颗 `RectMask2D` 的 `m_Padding = (-8,-5,-8,-5)`（**负 = 往外扩**）——
        /// 我们建 `Clip` 时按它外扩（`MenuDraw` 的 `clip` 是**裸框**，没有 pad 那一层）。</summary>
        public const float TextClipPadX = 8f, TextClipPadY = 5f;
        /// <summary>`marker` 那颗 `Outline` 的 `m_EffectDistance = (2.5, 2.5)`（**画布 px** —— 节点 `localScale = 1`）。
        /// uGUI 的 `Outline` = 往**四个斜角**各画一份副本（`Shadow.ApplyShadow` 四次），副本在**本体之前**。</summary>
        public const float MarkerOutlineOff = 2.5f;

        // ============================================================ 图 / 色 / 字号（逐条 MB 实读）
        public const string ArtPaneBg = "UI_Deck_Information_Back";   // 1100×701 · 九宫 42,363,655,81 · Sliced · ppuMul 1
        public const string ArtCloseBase = "UI_Button_Round_background";  // 237² · Simple · PA
        public const string ArtCloseBg = "40k_general_bt_yellow";         // 71² · Simple · PA
        public const string ArtCloseIcon = "40k_general_bt_yellow_close"; // 71² · Simple · PA（同框，画在上面）
        public const string ArtCloseHover = "40k_general_bt_yellow_hover";
        public const string ArtClosePressed = "40k_general_bt_yellow_pressed";
        /// <summary>`input` 的底：`40K_dropdown_bg`（119×102 · 九宫 23,20,23,20 · Sliced ·
        /// 🔴 `ppuMul = 1.6399999856948853` ⇒ 画出来的角块 = `border ÷ 1.64`，走 `Nine` 的 `borderOutPx`）。</summary>
        public const string ArtInputBg = "40K_dropdown_bg";
        /// <summary>`Divisor line members`：`40k_Separator Fade Sides Horizontal`（128×4 · 九宫 63,0,63,0 ·
        /// Sliced · `ppuMul = 1.64` ⇒ 角块 63 ÷ 1.64 = **38.4146**）。</summary>
        public const string ArtDivisor = "40k_Separator_Fade_Sides_Horizontal";
        /// <summary>`Generic Simplified UI Button` 的底（`UI_Button_Mulligan` 410×124 · **Simple + PA** ·
        /// `m_SpriteState` 实读 HL/P 两张）。</summary>
        public const string ArtBtn = "UI_Button_Mulligan",
                            ArtBtnHover = "UI_Button_Mulligan_hover",
                            ArtBtnPressed = "UI_Button_Mulligan_Pressed";
        /// <summary>`m_PixelsPerUnitMultiplier = 1.6399999856948853` 的那两张（`input` / `Divisor`）。</summary>
        public const float PpuMul164 = 1.6399999856948853f;

        public static readonly Color ShadeTint = new Color(0f, 0f, 0f, 0.772549f);   // 实读 (0,0,0,0.77254903)
        public static readonly Color InputTint = new Color(1f, 0.31733924f, 0.0039215686f, 1f); // 实读
        public static readonly Color DivisorTint = new Color(0.8745098f, 0.55244786f, 0.28627455f, 1f); // 实读
        public static readonly Color PlaceholderTint = new Color(0.9716981f, 0.9716981f, 0.9716981f, 0.5f); // 实读
        public static readonly Color ErrorTint = new Color(0.8207547f, 0.042586334f, 0.042586334f, 1f);     // 实读
        public static readonly Color CntNumTint = new Color(1f, 1f, 1f, 0.6862745f); // 实读 (1,1,1,0.686274528503418)
        /// <summary>`marker` 本体那颗 `Image.m_Color`（实读 **纯黑 (0,0,0,1)**）—— 不变色。</summary>
        public static readonly Color MarkerTint = new Color(0f, 0f, 0f, 1f);
        /// <summary>原版 `defaultColor` 字段（MB 实读 `(0.91764706, 0.76862746, 0.48235294, 1)`）——
        /// **没达成**的 `marker` 描边色。</summary>
        public static readonly Color DefDefaultColor = new Color(0.91764706f, 0.76862746f, 0.48235294f, 1f);
        /// <summary>原版 `achievedColor` 字段（MB 实读 `(0.29293314, 0.8396226, 0.19406372, 1)`）——
        /// **已达成**的 `marker` 描边色。⚠️ prefab 里 50 颗 `Outline` 的 `m_EffectColor` 出厂就是这个值
        /// （= 出厂那一档「50 颗全达成」）。</summary>
        public static readonly Color DefAchievedColor = new Color(0.29293314f, 0.8396226f, 0.19406372f, 1f);

        // 字号四元组 = prefab 的 `m_fontSize` / `m_fontSizeBase` / `m_fontSizeMin` / `m_fontSizeMax`（逐个实读）
        public const float RefTitleFont = 40f, RefTitleBase = 45.2f, RefTitleMin = 3f, RefTitleMax = 40f;
        public const float InLabelFont = 35f, InLabelBase = 39f, InLabelMin = 3f, InLabelMax = 35f;
        public const float PlaceholderFont = 40f, PlaceholderBase = 14f, PlaceholderMin = 18f, PlaceholderMax = 40f;
        public const float InputTextFont = 45f, InputTextBase = 14f, InputTextMin = 18f, InputTextMax = 45f;
        public const float BtnTextFont = 26.35000038f, BtnTextBase = 12f, BtnTextMin = 10f, BtnTextMax = 30f;
        public const float ErrorFont = 16.75f, ErrorBase = 36f, ErrorMin = 3f, ErrorMax = 35f;
        public const float RefLabelFont = 35f, RefLabelBase = 39f, RefLabelMin = 3f, RefLabelMax = 35f;
        public const float NameFont = 35f, NameBase = 36f, NameMin = 3f, NameMax = 35f;
        public const float TitleFont = 39.95000076f, TitleBase = 45.2f, TitleMin = 3f, TitleMax = 40f;
        public const float DescrFont = 32.20000076f, DescrBase = 39f, DescrMin = 3f, DescrMax = 35f;
        public const float CntNumFont = 26.89999962f, CntNumBase = 36f, CntNumMin = 3f, CntNumMax = 32f;

        // ============================================================ 出厂文本（prefab 原文，逐字）
        //   ⚠️ 每一行后面那个 `term=` 是那颗 `Localize` 组件的 `mTerm`（本件逐颗实读，6/6 拿到）——
        //      原版运行期拿它去 I2 词条表取值，而**词条表在远端 CCD**（本地一个 value 都没有）
        //      ⇒ 我们用 prefab 里印着的那一串（**俄文**，见文件头最后一段）。⛔ 别拿别的语言的串顶。
        public const string TxtRefTitle = "Реферальная программа";              // term=MenuShop/referral/mainTitle
        public const string TxtInLabel = "Введите имя вашего реферера в игре:";  // term=MenuShop/referral/inputLabel
        public const string TxtBtn = "Подтвердить";                              // term=MainMenu/General/Confirm
        public const string TxtError = "AN ERROR HAS OCURRED";                   // 无 Localize（原文；运行期被清空/替换）
        public const string TxtRefLabel = "Your referrer was:";                  // term=MenuShop/referral/referrerLabel
        public const string TxtName = "José Bezerra";                            // prefab 出厂样例串（运行期 = referrer.Name）
        public const string TxtTitle = "Пригласите своих друзей!";               // term=MenuShop/referral/title
        public const string TxtDescr = "Если они присоединятся к игре и введут ваше имя пользователя как реферера,"
                                     + " вы оба получите подарок! Вы можете проверить свои ожидающие подарки в"
                                     + " вашем почтовом ящике.";                  // term=MenuShop/referral/description
        /// <summary>`counter number` 的出厂串 = I2 词条 **`MenuShop/referral/counter`** 的原文
        /// （它带 `{0}`；`Refresh()` 走 `string.Format` 填的就是这一位）。</summary>
        public const string TxtCounter = "You have collected {0} referral rewards!";
        /// <summary>`Placeholder` 的内容：prefab 里就是**一个换行**（画出来是空的）。</summary>
        public const string TxtPlaceholder = "\n";
        /// <summary>`Text Area/Text` 的内容：prefab 里是 **U+200B 零宽空格**（= 空输入那一档）。</summary>
        public const string TxtInputEmpty = "\u200B";

        // ============================================================ 数据（原版 `ReferralManager` 三个查询）
        /// <summary>一份推荐关系的公开面（原版那三处的等价物：`GetReferrer()` / `GetReferrals().Count(!IsRedeemed)` /
        /// `referrer.Name`）。**本地一条都没有**（`ReferralManager` 走 PlayFab 服务端）。
        /// <para>`MaxRewards` 对应原版那个 **远端 LiveOps 配置**的 `+0x18`（`count &lt; max` 决定用哪条词条）——
        /// 🔴 **它的原值我们读不到**（配置在远端 CCD）⇒ 缺省取 `MarkerCount`（= 50，进度点有几颗就是几档，
        /// 这是**我们挑的**、不是原版的做法，如实标）。</para></summary>
        public struct ReferralView
        {
            /// <summary>`GetReferrer() != null` —— 有推荐人 ⇒ **关输入框、开「你的推荐人是」**。</summary>
            public bool HasReferrer;
            /// <summary>`referrer.Name`（`ReferralEntry` 的 `+0x18`）⇒ `Referred View/Name`。</summary>
            public string ReferrerName;
            /// <summary>`GetReferrals().Count(x =&gt; !x.IsRedeemed)` ⇒ 进度点的点亮数 + 「我收集了 N 份」。</summary>
            public int RewardCount;
            /// <summary>远端配置的 `max`（见结构体文档；缺省 50）。</summary>
            public int MaxRewards;
            /// <summary>输入框里的字（原版 `referredPlayerInput.text`）。</summary>
            public string InputText;
        }

        /// <summary>原版 `Refresh()` 那一档**没有数据**时的等价物（`referrer == null` 分支）。</summary>
        public static ReferralView DefView()
        {
            return new ReferralView { HasReferrer = false, ReferrerName = "", RewardCount = 0,
                                      MaxRewards = MarkerCount, InputText = "" };
        }

        /// <summary>最近一次开出来的那一扇（自检用，同 `RankedRewardEventWindow.LastOpened` 那条先例）。</summary>
        public static ReferralPopupWindow LastOpened { get; private set; }

        ReferralView _view = DefView();
        /// <summary>真的喂过数据吗（`false` = **出厂那一档**：prefab 的序列化状态一字未改）。</summary>
        public bool HasData { get; private set; }

        Transform _window, _content, _inputView, _referredView, _counter, _input;
        Label _refTitle, _inLabel, _placeholder, _inputText, _error, _refLabel, _name, _title, _descr, _cntNum;
        readonly System.Collections.Generic.List<Transform> _markers = new System.Collections.Generic.List<Transform>();
        readonly System.Collections.Generic.List<ImageQuad[]> _markerOutlines = new System.Collections.Generic.List<ImageQuad[]>();
        bool _warnedCounterMax;

        // ============================================================ 开

        /// <summary>建一扇。`view == null` ⇒ **出厂那一档**（prefab 的序列化显隐一字未改）。</summary>
        public static ReferralPopupWindow Create(WindowsManager mgr, ReferralView? view = null)
        {
            var go = new GameObject("Referral Popup");     // 节点名照原版（`WindowsManager` 复用的键同源）
            var win = go.AddComponent<ReferralPopupWindow>();
            win.type = WindowType.Popup;               // 实读 `type = 1`
            win.placement = WindowsPlacement.Popup;    // 实读 `windowsPlacement = 15`
            win.closeOnEsc = true;                     // 实读 `closeOnESC = 1`
            win.extraScaleSmallScreen = 1.35f;         // 实读 1.350000023841858（**不是 1.0** —— 逐扇实读）
            win.Manager = mgr;
            if (view.HasValue) { win._view = view.Value; win.HasData = true; }
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[Referral] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            Build();
            Debug.Log("[Referral] 开了 `Referral Popup`（原版 `ReferralPopupWindow`）。"
                    + "⚠️ **推荐数据在服务端**（`ReferralManager` 走 PlayFab）⇒ 本地走的是"
                    + "**原版自己的 no-data 分支**（`GetReferrer() == null`）：`Input View` 开着、"
                    + "`Referred View` 关着、`referButton.interactable = true`、计数串 = prefab 原文。"
                    + "🔴 **`counter`（50 颗进度点）出厂就是关着的、原版运行期也没有任何代码开它**"
                    + "（prefab `m_IsActive = 0` + 十个字段没一个是它 + 全包按 pid 搜零引用）"
                    + "⇒ **原版成品里那 50 颗一个像素都不画**，我们照原版建成关着。"
                    + "入口：原版由 `ReferralContainer.OpenPopup()` 那一条 LiveOps 容器链开（**我们没建那个容器**）。");
        }

        /// <summary>喂一份数据（**自检用**，也是数据路唯一的入口）。= 原版 `Refresh()` 的一次调用。</summary>
        public void SetReferral(ReferralView v) { _view = FillDef(v); HasData = true; Apply(); }

        /// <summary>逐格补出厂值（只补**空**的那些格，已赋的一个字不动）。</summary>
        public static ReferralView FillDef(ReferralView v)
        {
            if (v.ReferrerName == null) v.ReferrerName = "";
            if (v.InputText == null) v.InputText = "";
            if (v.MaxRewards <= 0) v.MaxRewards = MarkerCount;
            if (v.RewardCount < 0) v.RewardCount = 0;
            return v;
        }

        // ============================================================ 几何助手

        /// <summary>第 `i` 颗 `marker` 的矩形（**唯一一份算式** —— 50 格由它生成，见文件头「两处我们算的」①）。
        /// `x1 = 535.67 + 17.18 i`、`y = 507.54→517.54`、10×10。</summary>
        public static PxRect MarkerRect(int i)
        {
            float x1 = MarkerX0 + MarkerDx * i;
            return new PxRect(x1, MarkerY1, x1 + MarkerSize, MarkerY1 + MarkerSize);
        }

        /// <summary>`Text Area` 的裁切框（= 它自己那颗 `RectMask2D` 的矩形 **按 `m_Padding` 往外扩**）。
        /// `m_Padding = (-8,-5,-8,-5)`（x/y/z/w = 左/下/右/上），**负 = 扩**。</summary>
        public static PxRect TextClipRect()
        {
            return new PxRect(TextAreaR.x1 - TextClipPadX, TextAreaR.y1 - TextClipPadY,
                              TextAreaR.x2 + TextClipPadX, TextAreaR.y2 + TextClipPadY);
        }

        // ============================================================ 建

        public void Build()
        {
            var root = transform;
            MenuDraw.ClearChildren(root);
            MissingArt.Clear();
            _markers.Clear(); _markerOutlines.Clear();
            _refTitle = _inLabel = _placeholder = _inputText = _error = _refLabel = null;
            _name = _title = _descr = _cntNum = null;
            _inputView = _referredView = _counter = _input = null;

            // ---- ① `Menu Dark Background`：压暗整屏 + **点它关窗**（原版那颗 `BackgroundCloseButton` 挂在它身上）----
            MenuDraw.Rect(root, CardArt.Solid(), ShadeR, "Menu Dark Background", QShade, ShadeTint);
            // ⚠️ `qShade` 传 `QShade`（**与视觉压暗层同档**）—— `MenuDraw.ShadeRuleOk` 现场比的就是这一条。
            MenuDraw.ShadeHit(root, ShadeR, QShade, QHit, () => Close(), "BackgroundHit");
            // 面板吸收点击（原版那颗 `Image` 的 `m_RaycastTarget = 1`、父链上没有点击处理器 ⇒ 原版点面板什么都不做）
            // ⚠️ 摆在 `window` **之前**建 ⇒ 根的直接子件序 = [Menu Dark Background, BackgroundHit, AbsorbHit, window]
            //    （两颗我们加的命中层挨在一起；层档由 `Absorb` 自己算 = `QHit − 1`，与兄弟序无关）。
            MenuDraw.Absorb(root, "AbsorbHit", PaneR, QShade, QHit);

            // ---- ② `window` + 窗底（`UI_Deck_Information_Back` 九宫 42,363,655,81 · ppuMul 1）----
            _window = MenuDraw.Node(root, "window", WinR);
            var paneTex = Tex(ArtPaneBg, "窗体底 `Generic Window Red Background Big`");
            if (paneTex != null)
                MenuDraw.Nine(_window, paneTex, PaneR, new Vector4(42f, 363f, 655f, 81f), 1100f, 701f, QWinBg,
                              null, true, "Generic Window Red Background Big");
            else
                MenuDraw.Node(_window, "Generic Window Red Background Big", PaneR);

            // ---- ③ 关窗钮：**一颗节点带 Image + 两个孩子**（原版就是三层；`Background` 与 `Icon` 同框）----
            var closeQ = Rect(_window, ArtCloseBase, CloseR, "Generic Close Button Orange", QClose, null, true);
            var close = closeQ != null ? closeQ.transform : MenuDraw.Node(_window, "Generic Close Button Orange", CloseR);
            Rect(close, ArtCloseBg, CloseArtR, "Background", QClose, null, true);
            Rect(close, ArtCloseIcon, CloseArtR, "Icon", QClose, null, true);
            // 换图落在**圆底那一层**（原版 `m_TargetGraphic` = 它自己那颗 `Image`；HL/P 逐颗实读）。
            MenuDraw.Hit(close, "Hit", CloseR, QHit, () => Close(), closeQ,
                         ArtCloseBase, ArtCloseHover, ArtClosePressed);

            // ---- ④ `content` 那一列（**兄弟序照 `m_Children`**）----
            _content = MenuDraw.Node(_window, "content", ContentR);

            // `Referral Title`（fs40 · base 45.2 · auto[3~40] · Center/Middle · 折行=1）
            _refTitle = MenuDraw.TextBox(_content, RefTitleR, TxtRefTitle, Color.white, "Referral Title",
                                         RefTitleFont, RefTitleMin, QText, RefTitleMax, RefTitleBase);

            // `Input View`（那一列的输入区）
            _inputView = MenuDraw.Node(_content, "Input View", InputViewR);
            _inLabel = MenuDraw.TextBox(_inputView, InLabelR, TxtInLabel, Color.white, "Label",
                                        InLabelFont, InLabelMin, QText, InLabelMax, InLabelBase);
            BuildInput();
            // `spacing`：**空节点**（原版就是一根占位，没有 Image）
            MenuDraw.Node(_inputView, "spacing", InSpacingR);
            BuildReferButton();
            _error = MenuDraw.TextBox(_inputView, ErrorR, TxtError, ErrorTint, "error",
                                      ErrorFont, ErrorMin, QError, ErrorMax, ErrorBase);

            // `Referred View`（**出厂关着**；两态的那一件 —— `Refresh()` 在 `has` 时开它）
            _referredView = MenuDraw.Node(_content, "Referred View", RefViewR);
            _refLabel = MenuDraw.TextBox(_referredView, RefLabelR, TxtRefLabel, Color.white, "Label",
                                         RefLabelFont, RefLabelMin, QText, RefLabelMax, RefLabelBase);
            _name = MenuDraw.TextBox(_referredView, RefNameR, TxtName, Color.white, "Name",
                                     NameFont, NameMin, QText, NameMax, NameBase);

            // `Divisor line members`（分隔条 · 九宫 63,0,63,0 · ppuMul 1.64 ⇒ 角块 38.4146）
            var divTex = Tex(ArtDivisor, "分隔条 `Divisor line members`");
            if (divTex != null)
                MenuDraw.Nine(_content, divTex, DivisorR, new Vector4(63f, 0f, 63f, 0f), 128f, 4f, QText,
                              DivisorTint, true, "Divisor line members",
                              new Vector4(63f / PpuMul164, 0f, 63f / PpuMul164, 0f));
            else
                MenuDraw.Node(_content, "Divisor line members", DivisorR);

            // `spacing (1)`：空节点
            MenuDraw.Node(_content, "spacing (1)", Spacing1R);
            _title = MenuDraw.TextBox(_content, TitleR, TxtTitle, Color.white, "Title",
                                      TitleFont, TitleMin, QText, TitleMax, TitleBase);
            // 🔴 **2026-10-16（A712 阶段 2）就地订正（铁律 5）**：这段原来写着
            //    「`Descripton` 的 `m_VerticalAlignment = 256 (Top)` —— **我们这套 `Label` 没有纵向对齐那一层**
            //    （只有 `AlignLeft`/`AlignRight`）⇒ 建成**居中**并出声」。**「没有那一层」这半句现在过期了**：
            //    阶段 1（2026-10-15）已把 `Label.SetVAlign` + 逐档算式落进 `Battle/Label.cs`。
            // 🔴 **本行现在是真调用**（`W16` 当时判「档案对、但不落」，理由是**多行串没有口径**，见 `W16_A712阶段2.md` §⑤·1）：
            //    那个缺口的根治件（W25，2026-10-16）已把 `Label.VOffsetWorldNow` 的位移**按块高**算
            //    （新增 `VOffsetBlockWorld()`：`Top` 折**首行** / `Bottom` 折**末行**；`Middle` 那三档**短路成 0**
            //    ⇒ 出厂档逐位不变）。**所以才敢打开这一行。**
            //    · 落之前的老病（W16 实测推算）：只按**一行**算 ⇒ 整串被推高 `(n−1)/2 × 行盒高`
            //      （本窗这串俄文 ≈150 字符 / 框宽 871 / fs32.2 实渲 ≈ 3 行、行盒 48.8px ⇒ 偏 **≈49px**、
            //      顶出这个 140px 高的框 —— **比不改更错**，所以当时停手是对的）。
            //    · 现在：位移 = `原版 Top 那一格(框高 140) − 我们那一行的墨心 − 块心到首行那一截`
            //      ⇒ **首行的字墨**落在 `框上角 − ascent` 该在的地方（= 原版 `Top` 的语义），整串随行数自己折。
            //    · ⛔ 别把这一行删了退回「居中」；也别给它传别的档 —— 判据是 prefab 里的
            //      `m_VerticalAlignment = 256`（= `Top`，见本文件 `:427` 与 `W16` §二）。
            _descr = MenuDraw.TextBox(_content, DescrR, TxtDescr, Color.white, "Descripton",
                                      DescrFont, DescrMin, QText, DescrMax, DescrBase);
            // ⚠️ 排在 `TextBox` **之后**：`SetWrapWidth` / `SetAutoFitBox` 各自会重排一次，
            //    而框高那一份同时由 `SetAutoFitBox` 记进 `Label` ⇒ 先设档会拿不到框高（`SetVAlign` 的 doc）。
            MenuDraw.SetVAlign(_descr, Label.VAlign.Top, DescrR);
            _cntNum = MenuDraw.TextBox(_content, CntNumR, TxtCounter, CntNumTint, "counter number",
                                       CntNumFont, CntNumMin, QText, CntNumMax, CntNumBase);

            // `counter`（**出厂关着、运行期也没有代码开它** —— 见文件头①）+ 50 颗 `marker`
            _counter = MenuDraw.Node(_content, "counter", CounterR);
            for (int i = 0; i < MarkerCount; i++) BuildMarker(i);

            Apply();
            if (MissingArt.Count > 0)
                Debug.LogWarning("[Referral] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray()));
        }

        /// <summary>`Input View` 里的输入框：底（九宫 · ppuMul 1.64）+ `Text Area`（裁切）+ `Placeholder` + `Text`。
        /// <para>🔴 **原版这一件是活的**（`EverguildInputField : TMP_InputField`）—— 我们接的是本壳**现成的**
        /// 文本输入口 `PointerLayer.BeginText(...)`（同 `CollectionWindow` 的筛选框 / `ProfileTab` 改名 /
        /// `SettingsWindow` 那一族的用法），⛔ 不另立第二套。</para>
        /// <para>⚠️ `Text Area` 那颗 `RectMask2D` 的 `m_Padding = (-8,-5,-8,-5)`（**负 = 往外扩**）——
        /// 我们的 `Clip` 是裸框 ⇒ 按 pad 外扩一份（`TextClipRect()`）。文字那一层用 `MenuDraw.ClipText`
        /// 逐字裁（本壳既有口子；打进来的字不会溢出输入框）。</para></summary>
        void BuildInput()
        {
            var bgTex = Tex(ArtInputBg, "输入框底 `input`");
            // 画出来的角块 = `border ÷ ppuMul`（判据 → `MenuDraw.Nine` 的 `borderOutPx` 形参）
            var borderOut = new Vector4(23f / PpuMul164, 20f / PpuMul164, 23f / PpuMul164, 20f / PpuMul164);
            if (bgTex != null)
            {
                var go = MenuDraw.Nine(_inputView, bgTex, InputR, new Vector4(23f, 20f, 23f, 20f), 119f, 102f,
                                       QInputBg, InputTint, true, "input", borderOut);
                if (go == null) MenuDraw.Node(_inputView, "input", InputR);
                else _input = go.transform;
            }
            else _input = MenuDraw.Node(_inputView, "input", InputR);

            var ta = MenuDraw.Node(_input, "Text Area", TextAreaR);
            // `Placeholder`：fs40 · base 14 · auto[18~40] · Center/Middle · **折行=0** · 色 (0.972,0.972,0.972,0.5)
            _placeholder = MenuDraw.TextBox(ta, TextAreaR, TxtPlaceholder, PlaceholderTint, "Placeholder",
                                            PlaceholderFont, PlaceholderMin, QPlaceholder, PlaceholderMax, PlaceholderBase);
            // ⚠️ `TextBox` 会**无条件**把换行开成 `Normal`（`SetAutoFitBox` → `SetWrapWidth`）——
            //    原版这一格是 `m_TextWrappingMode = 0 (NoWrap)` ⇒ 建完显式关掉（同 `BaseOfferPopup` 那几处）。
            if (_placeholder != null) _placeholder.SetWrapping(false);
            // `Text`：fs45 · base 14 · auto[18~45] · Center/Middle · **折行=3**（`PreserveWhitespaceNoWrap`）· 白
            _inputText = MenuDraw.TextBox(ta, TextAreaR, TxtInputEmpty, Color.white, "Text",
                                          InputTextFont, InputTextMin, QInputText, InputTextMax, InputTextBase);
            if (_inputText != null) { _inputText.SetWrappingMode(3); MenuDraw.ClipText(_inputText, TextClipRect(), Vector2.zero); }

            // 命中区：原版 `m_OnValueChanged` / 点一下进编辑 —— 我们走 `PointerLayer.BeginText`
            // （`EverguildInputField.m_CharacterLimit = 0` = **不限长**；我们这一侧给 64 的上限并出声，见 `BeginInput`）。
            MenuDraw.Hit(_input, "Hit", InputR, QHit, BeginInput);
        }

        /// <summary>`Input View/Generic Simplified UI Button`（`UI_Button_Mulligan` · Simple + PA ·
        /// `Button` 那颗是 `trans = 2 (SpriteSwap)`：HL/P 两张逐字实读）。
        /// <para>⚠️ 这颗上**还叠了一颗 `EverguildButton`（`m_Transition = 1` ColorTint）** —— 两个 `Selectable`
        /// 挂同一件上，原版到底哪一个生效我没查到判据 ⇒ 照**同族既成做法**（`GenericOptionsPanel` 的
        /// `Template` 是**同一颗资产、同一套组件**，那边接的是换图）只接换图，如实记（报告 §九）。</para></summary>
        void BuildReferButton()
        {
            var btnNode = MenuDraw.Node(_inputView, "Generic Simplified UI Button", BtnR);
            var btnTex = Tex(ArtBtn, "确认钮底 `Generic Simplified UI Button`");
            ImageQuad btnQ = null;
            if (btnTex != null) btnQ = MenuDraw.Rect(btnNode, btnTex, BtnR, "Image", QBtn, null, true);
            // `Button Text`：fs26.35 · base 12 · auto[10~30] · Center/**Capline** · **折行=0**
            var tx = MenuDraw.TextBox(btnNode, BtnTextR, TxtBtn, Color.white, "Button Text",
                                      BtnTextFont, BtnTextMin, QBtnText, BtnTextMax, BtnTextBase);
            if (tx != null) tx.SetWrapping(false);
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版那颗 = `Center/**Capline** · 折行=0`
            //   （判据 = 上一行那句读数原文）。
            MenuDraw.SetVAlign(tx, Label.VAlign.Capline, BtnTextR);
            var hit = MenuDraw.Hit(btnNode, "Hit", BtnR, QHit, SubmitReferrer, btnQ, ArtBtn, ArtBtnHover, ArtBtnPressed);
            if (hit != null && btnQ == null)
                Debug.LogWarning("[Referral] 确认钮的底图没建出来 ⇒ 这一颗**没有图可换**（悬停/按下看不出来）。");
        }

        /// <summary>一颗 `marker`：**1 个黑方块 + 4 份 `Outline` 副本**。
        /// <para>判据 = 那颗 `Outline` 组件的三个字段实读：`m_EffectColor`（出厂 = `achievedColor`）·
        /// `m_EffectDistance = (2.5, 2.5)` · `m_UseGraphicAlpha = 1`。uGUI 的 `Outline` 就是
        /// 「往四个斜角各画一份副色副本、副本排在**本体之前**」（`Shadow.ApplyShadow` 四次）。</para>
        /// <para>🔴 本壳**没有 uGUI 的 `Outline`/`Shadow` 那一层** ⇒ 逐份建成 4 个 `ImageQuad` 子件
        /// （名字 `Outline`），`Apply()` 刷的就是这四颗的 tint（= 原版刷 `Outline.effectColor` 那一跳）。
        /// ⚠️ 它们是**我们这套渲染的产物**（原版那 4 份在 mesh 里、不是节点）—— 同 `MenuDraw.Nine` 的 9 个子块。</para></summary>
        void BuildMarker(int i)
        {
            var r = MarkerRect(i);
            var mk = MenuDraw.Rect(_counter, CardArt.Solid(), r, "marker", QMarker, MarkerTint);
            var node = mk != null ? mk.transform : MenuDraw.Node(_counter, "marker", r);
            var arr = new ImageQuad[4];
            for (int k = 0; k < 4; k++)
            {
                float dx = (k < 2 ? -MarkerOutlineOff : MarkerOutlineOff);
                float dy = (k % 2 == 0 ? -MarkerOutlineOff : MarkerOutlineOff);
                var o = new PxRect(r.x1 + dx, r.y1 + dy, r.x2 + dx, r.y2 + dy);
                arr[k] = MenuDraw.Rect(node, CardArt.Solid(), o, "Outline", QMarkerOutline, DefAchievedColor);
                if (arr[k] == null)
                    Debug.LogWarning("[Referral] `marker` #" + i + " 的 `Outline` 副本没建出来（`CardArt.Solid()` 取不到？）。");
            }
            _markers.Add(node);
            _markerOutlines.Add(arr);
        }

        // ============================================================ 铺数据（= 原版 `Refresh()` 那七跳）

        /// <summary>把 `_view` 铺到已建好的件上。**逐跳对位原版 `Refresh()`**（见文件头）。
        /// ⚠️ 出厂那一档（`HasData == false`）**什么都不改** —— 建的就已经是 prefab 的序列化状态，
        /// 而原版此刻也一个字都不写（`Open()` 只在拿到 `ReferralManager` 时才调 `Refresh()`）。</summary>
        public void Apply()
        {
            if (_inputView == null) return;              // 还没建
            if (!HasData)
            {
                // 出厂那一档：照 prefab 的序列化显隐（`Input View` 开 / `Referred View` 关 / `counter` 关）
                _inputView.gameObject.SetActive(true);
                _referredView.gameObject.SetActive(false);
                _counter.gameObject.SetActive(false);
                return;
            }
            var v = _view;
            // ① 两扇视图互斥
            _inputView.gameObject.SetActive(!v.HasReferrer);
            _referredView.gameObject.SetActive(v.HasReferrer);
            // ② 有推荐人 ⇒ 填名字（原版 `referrer.Name`）
            if (_name != null && v.HasReferrer) _name.SetText(v.ReferrerName ?? "");
            // ⑧ 输入框的显示 = 这一份数据里的字（原版这一格装的是**用户敲进去的**那一串；我们收在
            //    `ReferralView.InputText` ⇒ 铺数据时把它同步回显示，`BeginInput` 也拿它当初值）。
            //    ⚠️ 走 `ShowTyped`（不是裸 `SetText`）—— 那条路会**重裁一次**（契约见 `ShowTyped` 的注释）。
            ShowTyped(v.InputText ?? "");
            if (v.HasReferrer && _referredView != null)
                Debug.Log("[Referral] `Referred View` 开了 —— ⚠️ 原版此刻 uGUI 会把它那颗 `HorizontalLayoutGroup`"
                        + "跑一遍（`childControlWidth = 1`），两个孩子的**宽按各自字宽重写**；"
                        + "「字宽」要 Unity 的字体度量 ⇒ **我们量不出来**，这两个孩子**保留 prefab 序列化框**"
                        + "（`Label` 211.14 / `Name` 155.35）。最坏后果 = 长名字会被框挤着换行。");
            // ③ 按钮可用性（原版 `interactable = !has`）
            SetReferButtonInteractable(!v.HasReferrer);
            // ④⑤ 计数串（`count < max` 用带 `{0}` 的那条词条，否则用 `counterMaximum`）
            if (_cntNum != null)
            {
                if (v.RewardCount < v.MaxRewards)
                    _cntNum.SetText(string.Format(TxtCounter, v.RewardCount));
                else
                {
                    // 🔴 `MenuShop/referral/counterMaximum` 那条词条的**正文本地拿不到**（词条表在远端 CCD）
                    //    ⇒ 出声 + 退回 prefab 里那条带占位的串（**不假装知道**）。
                    if (!_warnedCounterMax)
                    {
                        _warnedCounterMax = true;
                        Debug.LogWarning("[Referral] `RewardCount >= MaxRewards` ⇒ 原版换用词条 "
                                       + "`MenuShop/referral/counterMaximum`（不带占位），"
                                       + "而**那条词条的正文在远端 CCD、本地一个 value 都没有** ⇒ "
                                       + "这一档**照抄不到**：退回 prefab 里那条 `" + TxtCounter + "`。");
                    }
                    _cntNum.SetText(string.Format(TxtCounter, v.RewardCount));
                }
            }
            // ⑥ 错误提示清空（原版 `Refresh()` 尾段那一句 `SetText("")` —— 字面量实读 = 空串）
            if (_error != null) _error.SetText("");
            // ⑦ 50 颗进度点的描边色（`i < count` ⇒ `achievedColor`，否则 `defaultColor`）
            RefreshMarkerColors();
            // ⚠️ `counter` **不在**上面任何一跳里（原版就是没有）⇒ 它的开关只由 prefab 决定
            //    （**恒关**）。这里显式写一次并出声，免得读代码的人以为漏了。
            if (_counter != null && !_counter.gameObject.activeSelf)
                Debug.Log("[Referral] （如实记）`counter` 那一栏**没有被打开** —— 原版 `Refresh()` 里没有它。");
        }

        /// <summary>逐颗刷 `marker` 的描边色（= 原版 `Refresh()` 尾段那个 `for` 循环：
        /// `markers[i].effectColor = i &lt; count ? achievedColor : defaultColor`）。</summary>
        public void RefreshMarkerColors()
        {
            int n = _view.RewardCount;
            for (int i = 0; i < _markerOutlines.Count; i++)
            {
                var col = i < n ? DefAchievedColor : DefDefaultColor;
                var arr = _markerOutlines[i];
                if (arr == null) continue;
                for (int k = 0; k < arr.Length; k++) if (arr[k] != null) arr[k].SetTint(col);
            }
        }

        /// <summary>第 `i` 颗 `marker` 现在那 4 份 `Outline` 副本的 tint（自检读口；`null` = 没建出来）。</summary>
        public ImageQuad[] MarkerOutlineQuads(int i)
        {
            return (i >= 0 && i < _markerOutlines.Count) ? _markerOutlines[i] : null;
        }

        /// <summary>原版 `referButton.interactable`。⚠️ 我们**没有 uGUI 的 `interactable`** ⇒
        /// 置 `false` 时把命中区**关掉**（点不动）+ 出声，让「点了没反应」看得出来（红线：不许静默失败）。
        /// 判据 = 原版 `OnSetReferrer()` 首句与失败回调各写一次这个字段。</summary>
        void SetReferButtonInteractable(bool on)
        {
            _referEnabled = on;
            var hit = FindChildByName(_inputView, "Generic Simplified UI Button");
            if (hit == null) return;
            var n = hit.Find("Hit");
            if (n != null) n.gameObject.SetActive(on);
        }

        bool _referEnabled = true;
        /// <summary>`referButton` 现在可用吗（自检读口）。</summary>
        public bool ReferButtonEnabled { get { return _referEnabled; } }

        // ============================================================ 两条交互（对位原版那两条监听）

        /// <summary>`referButton.onClick` → 原版 `OnSetReferrer()`：先 `interactable = false`，
        /// 再 `ReferralManager.SetReferrer(input.text, 成功→Refresh / 失败→&lt;OnSetReferrer>b__13_0)`。
        /// <para>🔴 **本地没有 `ReferralManager`**（服务端）⇒ 这一跳**没有真结果**：如实出声、
        /// 并把按钮**恢复成可用**（不留下一个「按了就死」的钮）。⛔ 不假装提交成功。</para></summary>
        public void SubmitReferrer()
        {
            string who = _inputText != null ? _inputText.Text : "";
            if (string.IsNullOrEmpty(who) || who == TxtInputEmpty)
                who = _view.InputText ?? "";
            SetReferButtonInteractable(false);
            Debug.LogWarning("[Referral] 按了确认（原版 `OnSetReferrer()` → `ReferralManager.SetReferrer(\"" + who + "\", …)`）。"
                    + "🔴 **本地没有 `ReferralManager`**（PlayFab 服务端）⇒ 没有真结果可报；"
                    + "原版两条回调的对应物：成功 ⇒ `Refresh()` · 失败 ⇒ "
                    + "`errorMessage = GetTranslation(\"CustomErrors/\" + err)`。我们**只出声、不假装成功**，"
                    + "并把按钮恢复成可用。");
            SetReferButtonInteractable(true);
        }

        /// <summary>点输入框 ⇒ 进编辑。走本壳现成的 `PointerLayer.BeginText`（同 `CollectionWindow` 那几处）。
        /// 每次改动 ⇒ 刷 `Text Area/Text` 的显示 + **清掉错误提示**（= 原版
        /// `referredPlayerInput.m_OnValueChanged → &lt;Start&gt;b__10_0` 那一跳，参数被丢弃）。
        /// <para>⚠️ 上限：原版 `m_CharacterLimit = 0`（**不限长**）；我们这一侧给 **64** 并出声
        /// （本壳 `BeginText` 要一个上限，见 `PointerLayer.BeginText`；⛔ 不静默截断 —— 到顶就不收字并出声）。</para></summary>
        public void BeginInput()
        {
            var pl = PointerLayer.Instance;
            if (pl == null)
            {
                Debug.LogWarning("[Referral] 批处理里没有 `PointerLayer` ⇒ 输入框敲不了字（Play 里可以）。"
                               + "**没有静默** —— 原版这一件是活的 `EverguildInputField`。");
                return;
            }
            string started = _view.InputText ?? "";
            if (string.IsNullOrEmpty(started) || started == TxtInputEmpty) started = "";
            pl.BeginText(started, InputCharLimit,
                s => { _view.InputText = s ?? ""; ShowTyped(_view.InputText); ClearError(); },
                () => ShowTyped(_view.InputText),
                s => ShowTyped(s ?? ""));
            Debug.Log("[Referral] 输入推荐人名字：回车确认，ESC 取消（原版 `EverguildInputField`，"
                    + "`m_CharacterLimit = 0` = 不限长；我们这一侧上限 " + InputCharLimit + "）。");
        }

        /// <summary>本壳输入上限（**我们挑的** —— 原版是 0 = 不限长，如实标）。</summary>
        public const int InputCharLimit = 64;

        /// <summary>把字画进 `Text Area/Text`（**逐次重裁** —— 打进去的字不会溢出输入框）。
        /// 🔴 **文字没变就直接返回**：`MenuDraw.ClipText` 的契约是「**在没夹过的网格上夹一次**」
        /// （见 `MenuDraw.ClipTmpMesh` 的③：在已经夹过的网格上再夹一次 = 几何夹第二次而 uv 只按第一次走
        /// —— **越裁越错，且静默**）⇒ 只有真改了字（`Label.SetText` 会 `ForceMeshUpdate` 出一份新网格）
        /// 才重裁。</summary>
        void ShowTyped(string s)
        {
            if (_inputText == null) return;
            string show = string.IsNullOrEmpty(s) ? TxtInputEmpty : s;
            if (_inputText.Text == show) return;      // 没变 ⇒ 别重裁（见上面那条契约）
            _inputText.SetText(show);
            MenuDraw.ClipText(_inputText, TextClipRect(), Vector2.zero);
        }

        /// <summary>清掉错误提示（= 原版 `&lt;Start&gt;b__10_0`：`errorMessage.SetText("")`）。</summary>
        public void ClearError()
        {
            if (_error != null && _error.Text != "") _error.SetText("");
        }

        /// <summary>`Input View/error` 现在那一串（自检读口）。</summary>
        public string ErrorText { get { return _error != null ? _error.Text : null; } }
        /// <summary>`Input View/input/Text Area/Text` 现在那一串（自检读口）。</summary>
        public string InputTextShown { get { return _inputText != null ? _inputText.Text : null; } }
        /// <summary>`counter number` 现在那一串（自检读口）。</summary>
        public string CounterText { get { return _cntNum != null ? _cntNum.Text : null; } }
        /// <summary>`Referred View/Name` 现在那一串（自检读口）。</summary>
        public string ReferrerNameText { get { return _name != null ? _name.Text : null; } }

        // ============================================================ 取图（**取不到必须出声**）
        readonly System.Collections.Generic.List<string> _missArt = new System.Collections.Generic.List<string>();
        /// <summary>本窗**取不到的图**（自检读口 —— 非空就是「有件根本没画」，同 `RankedRewardEventWindow.MissingArt`）。
        /// ⚠️ `MenuDraw.Rect/Nine` 对 `tex == null` 是**静默返回 null** ⇒ 必须谁取谁报。</summary>
        public System.Collections.Generic.List<string> MissingArt { get { return _missArt; } }

        Texture2D Tex(string art, string what)
        {
            var t = CardArt.MenuUi(art);
            if (t == null && !_missArt.Contains(art))
            {
                _missArt.Add(art);
                Debug.LogWarning("[Referral] 图取不到：`" + art + "`（" + what + "）⇒ **这一件没画**"
                               + "（`MenuDraw.Rect/Nine` 对 `tex == null` 是静默返回 null）。"
                               + "导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            }
            return t;
        }

        /// <summary>`art` 为空 ⇒ `CardArt.Solid()`（纯色件）；否则取图（取不到会出声，返回 `null`）。</summary>
        ImageQuad Rect(Transform p, string art, PxRect r, string n, int q, Color? tint = null, bool keepAspect = false)
        {
            var t = string.IsNullOrEmpty(art) ? CardArt.Solid() : Tex(art, n);
            return MenuDraw.Rect(p, t, r, n, q, tint, keepAspect);
        }

        static Transform FindChildByName(Transform t, string name)
        {
            if (t == null) return null;
            for (int i = 0; i < t.childCount; i++) if (t.GetChild(i).name == name) return t.GetChild(i);
            return null;
        }

        // ============================================================ 自检读口

        /// <summary>自检用：把当前铺出来的东西摊开（同 `RankedRewardEventWindow.DebugDump` 那条先例）。</summary>
        public string DebugDump()
        {
            return "hasData=" + HasData
                 + " inputView=" + (_inputView != null && _inputView.gameObject.activeSelf)
                 + " referredView=" + (_referredView != null && _referredView.gameObject.activeSelf)
                 + " counter=" + (_counter != null && _counter.gameObject.activeSelf)
                 + " markers=" + _markers.Count
                 + " count=" + _view.RewardCount + "/" + _view.MaxRewards
                 + " referEnabled=" + _referEnabled
                 + " missingArt=" + _missArt.Count;
        }

        /// <summary>`counter` 那一栏的节点（**出厂恒关**；自检读口）。</summary>
        public Transform CounterNode { get { return _counter; } }
        /// <summary>`window` 节点（自检读口）。</summary>
        public Transform WindowNode { get { return _window; } }
        /// <summary>`Input View` 节点（自检读口）。</summary>
        public Transform InputViewNode { get { return _inputView; } }
        /// <summary>`Referred View` 节点（自检读口）。</summary>
        public Transform ReferredViewNode { get { return _referredView; } }
        /// <summary>`input` 节点（自检读口）。</summary>
        public Transform InputNode { get { return _input; } }

        // ============================================================ 对账表（**纯数据 · 不参与渲染**）
        /// <summary>一个节点的**冻结事实**：路径 / **相对根左上角的框** / 出厂 `activeSelf`。
        /// <para>🔴 **本表是「照抄」那一侧的记录**：每一条都逐格对着
        /// `python d:/4/Unity/工具/menu_dump.py bundle_generalgamewindows_assets_all "Referral Popup" --depth 12 --relative --md`
        /// 的现读值抄（**相对框**那一档 —— 本扇根是拉伸根 ⇒ 相对框 == 绝对框）。</para>
        /// <para>⚠️ **两行的来源不是 `menu_dump`**（那是**我们算的**，见文件头「两处我们算的」）：
        /// `window/content/Referred View/Label` 与 `…/Name` —— 取 **prefab 序列化值**（那颗 HLG 关着 ⇒ 布局不跑，
        /// 而 `childControlWidth = 1` 让 `menu_dump` 在那一格量不出宽）。对账脚本对这两条走**原读**那一侧。</para>
        /// <para>⚠️ **`marker` 那 50 行由 `MarkerRect(i)` 生成**（一条算式；见文件头①）—— 对账脚本逐格比。</para></summary>
        public struct ReconRow
        {
            public string Path;
            public PxRect R;
            public bool On;
            public ReconRow(string p, PxRect r, bool on = true) { Path = p; R = r; On = on; }
        }

        /// <summary>逐节点对账表（**78 条 = 原版 78 个节点**）。</summary>
        public static readonly ReconRow[] Recon = BuildRecon();

        static ReconRow[] BuildRecon()
        {
            var l = new System.Collections.Generic.List<ReconRow>();
            l.Add(new ReconRow("",                           new PxRect(0f, 0f, 1920f, 1080f)));
            l.Add(new ReconRow("Menu Dark Background",       ShadeR));
            l.Add(new ReconRow("window",                     WinR));
            l.Add(new ReconRow("window/Generic Window Red Background Big", PaneR));
            l.Add(new ReconRow("window/Generic Close Button Orange",       CloseR));
            l.Add(new ReconRow("window/Generic Close Button Orange/Background", CloseArtR));
            l.Add(new ReconRow("window/Generic Close Button Orange/Icon",       CloseArtR));
            l.Add(new ReconRow("window/content",             ContentR));
            l.Add(new ReconRow("window/content/Referral Title", RefTitleR));
            l.Add(new ReconRow("window/content/Input View",  InputViewR));
            l.Add(new ReconRow("window/content/Input View/Label", InLabelR));
            l.Add(new ReconRow("window/content/Input View/input", InputR));
            l.Add(new ReconRow("window/content/Input View/input/Text Area", TextAreaR));
            l.Add(new ReconRow("window/content/Input View/input/Text Area/Placeholder", TextAreaR));
            l.Add(new ReconRow("window/content/Input View/input/Text Area/Text",        TextAreaR));
            l.Add(new ReconRow("window/content/Input View/spacing", InSpacingR));
            l.Add(new ReconRow("window/content/Input View/Generic Simplified UI Button", BtnR));
            l.Add(new ReconRow("window/content/Input View/Generic Simplified UI Button/Button Text", BtnTextR));
            l.Add(new ReconRow("window/content/Input View/error", ErrorR));
            l.Add(new ReconRow("window/content/Referred View", RefViewR, false));      // 出厂 act = F
            l.Add(new ReconRow("window/content/Referred View/Label", RefLabelR));      // ← 原读（序列化值）
            l.Add(new ReconRow("window/content/Referred View/Name",  RefNameR));       // ← 原读（序列化值）
            l.Add(new ReconRow("window/content/Divisor line members", DivisorR));
            l.Add(new ReconRow("window/content/spacing (1)", Spacing1R));
            l.Add(new ReconRow("window/content/Title",       TitleR));
            l.Add(new ReconRow("window/content/Descripton",  DescrR));
            l.Add(new ReconRow("window/content/counter number", CntNumR));
            l.Add(new ReconRow("window/content/counter", CounterR, false));            // 出厂 act = F
            for (int i = 0; i < MarkerCount; i++)
                l.Add(new ReconRow("window/content/counter/marker#" + i, MarkerRect(i)));
            return l.ToArray();
        }
    }
}
