// PopUpGameWindow.cs — 原版那扇**模态消息窗**（`PopUpGameWindow : GameWindow`）
//
// ============================ 出处（唯一判据） ============================
// ① **两扇 prefab**（本文件两个版面各照一份；都在 `bundle_generalgamewindows_assets_all/GameObject/`）：
//    · `MessagePopupWindow`（**1 按钮**版）—— 窗参 MB `MonoBehaviour_3669898094100658758.json`
//    · `MessagePopupWindow2Buttons`（**2 按钮**版）—— 窗参 MB `MonoBehaviour_2996096449700776559.json`
//    🔴 **全库（4.4 GB / 24.7 万文件）里 `PopUpGameWindow` 只有这 2 个实例**——
//      判据：`bundle_Waprforge_monoscripts/MonoScript/MonoScript_-3977360589382919358.json` 的
//      `m_ClassName = "PopUpGameWindow"`，拿那个 PathID 全库 grep **只命中这 2 颗 MB** + 同包的 `AssetBundle_1.json`
//      ⇒ 它们**就是** `WindowsManager` 那两个字段 `popupWindowOneButton` / `popupWindowTwoButtons` 指的 prefab。
//    · 逐节点参数（层 / 名字 / rect / 锚点 / 字号 / 图名 / 九宫 / 色）= `工具/menu_dump.py` 实读
//      **＋ 原始 JSON 逐字段复核**（表里那几列是**布局跑之后**的值，有一半不是 prefab 原字段 —— 见 `menu_dump` 文件头 ②）：
//        `python d:/4/Unity/工具/menu_dump.py bundle_generalgamewindows_assets_all "MessagePopupWindow" --depth 8 --md`
//        `python d:/4/Unity/工具/menu_dump.py bundle_generalgamewindows_assets_all "MessagePopupWindow2Buttons" --depth 8 --md`
// ② **行为**（`d:/2/tools/decomp_full/` 逐句）：
//    · `WindowsManager__ShowPopUp.c` → `WindowsManager._LoadPopUpAndShow_d__59__MoveNext.c`：给到的按钮
//      **数组长度 > 1 ⇒ 2 按钮版**，否则 1 按钮版；随后 `PopUpGameWindow__ConfigurePopUp(...)` + `WindowsManager.OpenWindow(...)`。
//    · `PopUpGameWindow__ConfigurePopUp.c`：① `SetText(文案, localizeTexts)`；② 逐颗 `LiveButtons`：
//      **超出给定按钮数的那些 `SetActive(false)`**、其余 `SetActive(true)`；每颗按 `buttons[i].Text` 贴标签
//      （`localizeTexts` 为真时过 `LocalizationManager.GetTranslation`）、`OnPress != null` 才挂回调；
//      ③ 末尾把 `closeOnEsc` 写进字段（**所以「1 按钮版」与「2 按钮版」是同一个类的两套 prefab**）。
//    · `PopUpGameWindow__SetText.c`：`localizeTexts != 0` ⇒ 文案先过 `I2.Loc.LocalizationManager.GetTranslation`。
//    · `PopUpGameWindow__PopUpWindowButtonPressed.c` ⇒ 🔴 那个 `.c` 的**函数名被 Ghidra 印成了**
//      `ShopOfferContainer__CallRefreshCallback`（本仓已知的错桩之一）：体 = 调 `+0x18` 那个委托
//      （= `GameWindowButton.OnPress`）⇒ **按下去就是调调用方给的回调**。
//    · `BackgroundCloseButton__UnityEngine.EventSystems.IPointerClickHandler.OnPointerClick.c`：
//      `if (window != null) window.Close(); if (onClick != null) onClick.Invoke();`
//      🔴 **本窗这两颗 `BackgroundCloseButton` 的 `window` 是【空引用】**（原始 JSON 实读 `"window": {"m_PathID": 0}`；
//      全 `bundle_menus_assets_all` 的 82 颗该类实例里**只有 28 颗非空**）⇒ **原版这扇窗点背景【不】关窗**
//      （`onClick` 也是空的）⇒ 压暗层只是一层**吃射线的整屏图**。⇒ 我们用 `MenuDraw.Absorb`，
//      ⛔ **不是** `ShadeHit`（那是「点背景关窗」那一族的工具，同族先例 = `TrophyInfoPopup` /
//      `PlayerProfileWindow` —— 那两扇的 `window` 非空）。
//    · **谁调它**（本窗今天的第一个消费者）= `DeckEditingWindow__TrySaveDeck.c:81-98`：卡组不合法 ⇒
//      `:94 WindowsManager.ShowPopUp(ToRawLocalizationString(err), localizeTexts:1, closeOnEsc:0, 两颗钮)` → `:95 return`；
//      合法那一支 `:103` 反而调 `WindowsManager.HidePopUp`（把还开着的弹窗收掉）。
//      ⚠️ 主按钮那两颗回调**逐句读到了**（`d:/2/tools/decomp_full/DeckEditingWindow___TrySaveDeck_b__42_1.c`
//      与 `DeckEditingWindow.__c___TrySaveDeck_b__42_0.c`）：
//        · 左钮 `MainMenu/General/Discard` = `HidePopUp()` **＋** 虚槽 `0x1b8`（= `GameWindow.Close()`，见
//          `资料/日常_调用链_三窗.md:151` 的槽位↔方法名表 —— 那是个**关掉卡组编辑窗**的动作）；
//        · 右钮 `MainMenu/General/Cancel`  = **只** `HidePopUp()`（留在编辑器里）。
//      （两颗钮用哪句话**不是**从 prefab 读的：prefab 里 `Button Text` 的出厂字面量是 `'Continue'`、
//        `2Buttons` 那颗还带 `Localize.mTerm = "Battle/Mulligan/ButtonDone"` —— 都是**模板默认**，
//        运行期被 `ConfigurePopUp` 按调用方给的 `GameWindowButton.Text` 覆盖。
//        真正的字面量来自 `stringliteral.json` 的地址表：`0x42BE418 → MainMenu/General/Discard`、
//        `0x42BE120 → MainMenu/General/Cancel`，与 `TrySaveDeck.c` 里那两个 `DAT_1842be…` 逐个地址对上。）
// ③ **文案 = I2 术语【键】，不是明文**（这条对**正文与两颗钮**都成立）：
//    `DeckUtility__ToRawLocalizationString.c` 的键 = `MenuDeck/Error/{0}`（`{0}` = 原版 `DeckError` 的号），
//    **查不到词条时兜底仍是另一个键** `MenuDeck/Error/InvalidDeck`（地址表实读，见
//    `资料/普查产出_1011/WB1_A330.md` §2.4 的四行表）。
//
// ---- 🔴 四处如实标注（本地拿不到 / 我们挑的，⛔ 别当成原版）----
//   ① **远端 I2 语言表本地没有**（`assets_full` 全库 `mTerms` 0 命中）⇒ 本类那张 `Terms` **故意留空**，
//      将来拿到远端表只往表里填、**不改任何调用点**（**照 `Battle/ChoosePanel.cs` 那条先例**）。
//      🔴 **2026-10-19（A1012）就地订正（铁律 5）**：这里原来接着写「正文与两颗钮**现在显示的是键本身**
//      （`Term()` 的兜底 = 键）」—— **那句已经不成立**：`Term()` 现在**先查 `Terms`、再转全工程那张
//      `Core/Loc.cs` 语言表** ⇒ 表里**有的**键印**词条**（`MenuDeck/HUD/DiscardChanges` /
//      `MainMenu/General/{Discard,Cancel}` 三条都在 `Loc` 里）、**两处都没有**的才印键名。
//      ⛔ 仍然**不自己编词条**（红线：不许把「查不到」写成猜测）。
//   ② **开/关音效没做**：prefab 的 `openSound` / `closeSound` 都是空 + `useDefaultCloseSoundIfNull = 1`
//      （默认关窗音在**远端音频表**里）⇒ 与全仓其它窗同一条既有缺口（`Shell/RewardWindow.cs:10` 等 6 处已记）。
//   ③ **没有布局系统**：原版 `Buttons` 那两处是 `VerticalLayoutGroup` / `HorizontalLayoutGroup` 在**运行时**
//      算子件位置，序列化的 `Window` 尺寸是**写死的成品值**（`750×420` / `850×430`，两扇都**没有**
//      `ContentSizeFitter`）⇒ 我们按**布局跑之后**的绝对矩形摆死（每条都在下面 `static readonly PxRect` 处注明算式）。
//   ④ **没有掩码体系**：原版 `Window > Mask` 是一颗 `Image,Mask`（**`showGraphic = 0` ⇒ 它自己不画**，只当模板），
//      它的子件 `Background fill` 被**裁成那块圆的形状**。我们**只建节点、不画**（照 `showGraphic = 0`），
//      `Background fill` 直接按 `Mask` 的矩形铺 ⇒ 差的是**四个圆角那一小圈**（如实标，与本仓其它窗同一笔账）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `PopUpGameWindow`（`GameWindow` 子类）—— **唯一的通用模态消息窗**：
    /// 一段话（正文是 I2 **术语键**）+ 1~2 颗按钮（页面正文/钮标都过 `LocalizationManager`）。
    ///
    /// <para>🔴 **与 `Shell/PromptPopup.cs` 是【两扇不同的窗】**（⛔ 别混）：那一扇照的是
    /// `bundle_menus_assets_all` 的 `GenericPromptWindow`（类 `PromptPopup`，带输入框那一族），
    /// 本扇照的是 `bundle_generalgamewindows_assets_all` 的 `MessagePopupWindow{,_2Buttons}`（类 `PopUpGameWindow`）。
    /// 两扇的窗参、面板尺寸、字号、按钮行布局**逐项不同**（900×? 居中 vs 750/850×420/430 死尺寸）。</para>
    ///
    /// <para>🔴 **它不在任何一页的层带里**：自成一档 **3560–3565**（= 全壳**所有窗**之上：
    /// 最高的窗带是排行榜 `LeaderboardWindow.QBase = 3500`…`3520`；**之下**是 `Core/Tooltip.cs` 的
    /// `QTipShadow/QTip/QTipText = 3605/3606/3607` —— 提示面板仍必须最高，那是原版的兄弟序，
    /// 见 `Core/Tooltip.cs:75-84`）⇒ 层带不重叠。</para></summary>
    public class PopUpGameWindow : GameWindow
    {
        // ============================================================ 队列档（本窗自成一档 · 3560–3565）
        // 逐层顺序 = **原版兄弟序**（uGUI 按兄弟序画 ⇒ 后面的压前面的；判据 = 两扇 prefab 原始 JSON 的 `m_Children`）：
        //   根 = [Menu Dark Background, Window]；
        //   Window = [Generic Popup Background, MessageText, Buttons]；
        //   Generic Popup Background = [Mask]、Mask = [Background fill]；
        //   Buttons = [Generic UI Button]（1 按钮版）/ [ButtonLeft, ButtonRight]（2 按钮版）；
        //   → 压暗层 < 面板底 < 面板内填充 < 按钮底 < 文字。
        public const int QShade = 3560,       // 压暗整屏（`Menu Dark Background`，无图纯色）
                         QShadeHit = 3561,    // 压暗层那颗**吃射线的**命中区（原版那颗 `Image.m_RaycastTarget = 1`）
                         QPanel = 3562,       // 面板九宫（`Generic Popup Background` + `Mask` 自己那张同图）
                         QFill = 3563,        // 面板内平铺填充（`Background fill`，`ppuMultiplier = 2`）
                         QContent = 3564,     // 按钮底（`Generic UI Button` / `ButtonLeft` / `ButtonRight`）
                         QText = 3565;        // 文字（`MessageText` / 各 `Button Text`）

        // ============================================================ 图名与真值（`menu_dump` 实读）
        public const string ArtPopup = "40k_popup";               // 359×336 · 九宫格 (169,160,169,160)
        public const string ArtPopupFill = "40k_popup_texture";   // 128×128 · Tiled · `ppuMultiplier 2` ⇒ 64 一格
        public const string ArtButton = "40K_button";             // 489×107 · 九宫格 (234,46,234,46) · Simple
        public const float PopupTexW = 359f, PopupTexH = 336f;
        public const float BtnTexW = 489f, BtnTexH = 107f;
        /// <summary>`40k_popup_texture` 的平铺格（画布 px）= 128 ÷ `m_PixelsPerUnitMultiplier 2.0`。</summary>
        public const float FillTilePx = 64f;
        public static readonly Vector4 PopupBorder = new Vector4(169f, 160f, 169f, 160f);
        /// <summary>`Menu Dark Background` 的 `Image.m_Color` —— **两扇 prefab 逐位同值** `(0,0,0,0.772549033164978)`。
        /// （`PromptPopup` 那扇是 `0.7725`，**不是同一个数**，别互抄。）</summary>
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.772549033164978f);
        /// <summary>三颗钮 `Image.m_Color`（两扇逐位同值）`(0.36862749, 0.89411765, 0.58743727, 1)`。</summary>
        public static readonly Color BtnColor = new Color(0.36862748861312866f, 0.8941176533699036f,
                                                          0.5874372720718384f, 1f);

        /// <summary>压暗整屏那一块（**两扇 prefab 同一个矩形**；全壳 17 处弹窗共用的那个 4574.6×2572.36）。</summary>
        public static readonly PxRect ShadeR = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);

        // ---- 1 按钮版（`MessagePopupWindow`）：`Window` 中心 = 画布中心 + `apos(0,80)`，尺寸 750×420 ----
        public static readonly PxRect Win1R    = new PxRect(585.00f, 250.00f, 1335.00f, 670.00f);
        // `Mask`：锚点 stretch(0,0)-(1,1) + `sizeDelta(-20.268,-19.245)` + `apos(0.261993,0.178986)`
        //   ⇒ 相对面板四边各内缩 10.134 / 9.6225，再按 apos 平移（**画布 y 向下 ⇒ 上移** 0.179）。
        public static readonly PxRect Mask1R   = new PxRect(595.40f, 259.44f, 1325.13f, 660.20f);
        /// <summary>`MessageText`：锚点 `(0,0.5)-(1,0.5)` + `sizeDelta(-80,270)` + `apos(0,50)` ⇒ 宽 = 750−80。</summary>
        public static readonly PxRect Msg1R    = new PxRect(625.00f, 275.00f, 1295.00f, 545.00f);
        /// <summary>`Buttons`：锚点 `(0.5,0)` + `apos(0,70)` + `sizeDelta(733.9,90)`（**贴着面板下沿往上 70**）。</summary>
        public static readonly PxRect Btns1R   = new PxRect(593.05f, 555.00f, 1326.95f, 645.00f);
        /// <summary>`Generic UI Button`：VLG `align=4(MiddleCenter)` `spacing=22.24` `ctrlW/H=0` ⇒ 子件按
        /// **自己那个 `sizeDelta(400,75)`** 居中（`forceExpandW=1` 只影响「格」宽、不改子件本身，
        /// 见本地 uGUI `HorizontalOrVerticalLayoutGroup.cs:205-216` 的 `controlSize=false` 支）⇒ 就是 400×75 居中。</summary>
        public static readonly PxRect Btn1R    = new PxRect(760.00f, 562.50f, 1160.00f, 637.50f);

        // ---- 2 按钮版（`MessagePopupWindow2Buttons`）：`Window` 850×430，同一个 `apos(0,80)` ----
        public static readonly PxRect Win2R    = new PxRect(535.00f, 245.00f, 1385.00f, 675.00f);
        public static readonly PxRect Mask2R   = new PxRect(545.40f, 254.44f, 1375.13f, 665.20f);
        public static readonly PxRect Msg2R    = new PxRect(575.00f, 273.80f, 1345.00f, 546.20f);
        public static readonly PxRect Btns2R   = new PxRect(572.30f, 560.00f, 1347.70f, 650.00f);
        // HLG `align=4` `spacing=0` `forceExpandW=1` `ctrlW=0`：总首选 = 350+350 = 700、组宽 775.4
        // ⇒ 剩 75.4、总 flexible = 2 ⇒ 每格 37.7 的「格」；子件本身仍 350 宽、在格里居中
        // ⇒ 左钮左沿 = 572.30 + 18.85 = 591.15、右钮左沿 = 572.30 + 37.7 + 18.85 + 350 …（下两行就是结果）。
        public static readonly PxRect BtnL2R   = new PxRect(591.15f, 567.00f, 941.15f, 643.00f);
        public static readonly PxRect BtnR2R   = new PxRect(978.85f, 567.00f, 1328.85f, 643.00f);

        /// <summary>`Button Text` 的**框宽** = 按钮宽 − 26（原版那颗锚点 stretch + `sizeDelta.x = −26`）。</summary>
        public const float TextInsetX = 26f;
        /// <summary>`Button Text` 上的 `AspectRatioFitter`：`mode = 1(WidthControlsHeight)` ·
        /// `m_AspectRatio = 5.140573024749756` ⇒ **框高 = 框宽 ÷ 这个比值**（1 按钮版 374÷5.140573 = 72.75 ·
        /// 2 按钮版 324÷5.140573 = 63.03，与 dump 逐位吻合）。</summary>
        public const float TextAspect = 5.140573024749756f;
        /// <summary>1 按钮版那颗 `Button Text` 的 `anchoredPosition = (−0.303955, 0.608994)`
        /// —— **亚像素**（0.3/0.6 px），照原文收进来只为了断言能拿 0.5px 容差比。</summary>
        public const float Btn1TextAposX = -0.303955078125f, Btn1TextAposY = 0.6089935302734375f;

        // ---- 字号（`TextMeshProUGUI` 逐字段实读）----
        /// <summary>正文：`m_fontSize = 40` · `m_enableAutoSizing = 1` · `auto[4, 40]` · **`m_fontSizeBase = 36`** · 居中(Middle/Center) · **折行开**。</summary>
        public const float MsgFontPx = 40f, MsgAutoMinPx = 4f;
        /// <summary>按钮字：1 按钮版 `auto[12,40]`（`m_fontSize = 40`）· 2 按钮版 `auto[12,38]`（`m_fontSize = 38`）。
        /// 两处**折行都关**（`折行=0`，靠自适应缩字号）。**两版 `m_fontSizeBase` 逐值相同 = 12**。</summary>
        public const float Btn1FontPx = 40f, Btn2FontPx = 38f, BtnAutoMinPx = 12f;

        // 🔴 **2026-10-19（A1188）**：`base` 那一格原来**传的是缺省 `0`**（= `SetAutoFitBox` 的
        //   「base 退回调用方那一档」老行为），而原版两颗都**显式设过** `m_fontSizeBase`：
        //   · `MessageText`  = **36**（`m_fontSize 40` ⇒ 比值 0.9）
        //   · `Button Text`  = **12**（1 按钮版 `m_fontSize 40` / 2 按钮版 `38`，**两版都是 12**）
        //   判据 = `工具/menu_dump.py bundle_generalgamewindows_assets_all "MessagePopupWindow"` 与
        //          `… "MessagePopupWindow2Buttons"`（`--no-sprite --no-layout`）实读的 **`基准=`** 那一列
        //          （两扇 prefab 逐颗亲读：`MessageText` `字号=40.0 基准=36.0 auto[4.0~40.0] 折行=1` ·
        //           `Button Text` `字号=40.0/38.0 基准=12.0 auto[12.0~40.0]/[12.0~38.0] 折行=0`）。
        //   ⚠️ **本窗没有把 `localScale` 烘进字号**（`grep localScale` 本文件 0 命中；
        //      `Window` 的父链上也没有缩放：`menu_dump` 那两扇树里 `ls=` 一处都没标）⇒
        //      **不需要像 `AlliancePanelWindow` / `DailyRewardPopup` 那样乘刻度** —— 原文照抄。
        //   ⚠️ `base` **不是**「字号」（`m_fontSize`）、也**不是**上下限：它是 TMP 自适应**二分的起点**
        //      （`Label.SetAutoFitBox` = `cur × basePx/nomPx`，写进 `m_fontSizeBase`）。
        //      base ≠ 正文那一档时，收敛结果可能与原版不同 ⇒ 必须照抄，⛔ 别留缺省 0。
        /// <summary>`MessageText` 的 `m_fontSizeBase`（两扇 prefab 逐值相同 = **36**）。</summary>
        public const float MsgAutoBasePx = 36f;
        /// <summary>`Button Text` 的 `m_fontSizeBase`（**1 按钮版与 2 按钮版逐值相同 = 12**）。</summary>
        public const float BtnAutoBasePx = 12f;

        // ============================================================ 术语（I2 语言表 —— **本地没有**）

        /// <summary>原版词条表在本类的落点（键 → 文字），**照 `Battle/ChoosePanel.cs` 的 `Terms` 先例**建。
        /// 🔴 **本表今天仍是空的**（远端 I2 语言表本地没有；判据 → 本文件头 ① 与
        /// `资料/普查产出_1011/WB1_A330.md` §2.4），将来拿到表就往这里填、**不用改任何调用点**。
        /// ⚠️ **2026-10-19（A1012）订正（铁律 5）**：原来这里写「⇒ 今天**必然查不到**」——
        /// 那句**只对本表成立**；`Term()` 已改成「本表没有就转 `Core/Loc.cs`」⇒「查不到」的判据
        /// **不再是本表**，而是「`Terms` 与 `Loc` 两处都没有」（那时才印键名）。
        /// 填进本表的条目**优先于** `Loc`（见 `Term`）。
        /// 键 = 原版那两类：正文 `MenuDeck/Error/&lt;1..5>`（+ 兜底键 `MenuDeck/Error/InvalidDeck`）·
        /// 按钮 `MainMenu/General/{Discard,Cancel}`。</summary>
        public static readonly Dictionary<string, string> Terms = new Dictionary<string, string>();

        /// <summary>取词条：**先查本类的 `Terms`、再转全工程的 `Core/Loc.cs` 语言表**；
        /// **两处都没有才给键本身**（= 原版 `LocalizationManager.GetTranslation` 查不到时的行为）。
        /// ⛔ **绝不自己编一句中文/英文**（红线：不许把「查不到」写成猜测）。
        ///
        /// <para>🔴 **2026-10-19（`A1012`）就地改掉末句（铁律 5）**：原来这里是 `return key ?? "";`
        /// —— 只查那张**恒空**的 `Terms`、**从不查 `Loc`** ⇒ 明明有词条可印、印出来的却是**键名**
        /// （`Deck/DeckRuntime.cs` 那几处传的就是**键**）。现在转 `Loc.T(key)`。
        /// ⇒ **这是照原版**（原版 `PopUpGameWindow.SetText` 也是拿**键**过
        /// `I2.Loc.LocalizationManager.GetTranslation`，见本文件头 ②），⛔ 不是把调用点改成传明文。</para>
        ///
        /// <para>⚠️ **对传明文的站点行为零变化**：壳侧 20+ 个 `ShowMessagePopUp` 调用点传的都是**明文**
        /// （见 `Shell/WindowsManager.cs:1517`），而 `Loc.T` 对**表里没有的键**同样是
        /// **返回键名本身 + 出声**（`Loc.T` 的 doc）⇒ 明文原样返回，只是会多记一次
        /// `Loc.MissingCount` / 多出一条 `[Loc] 语言表里**没有**这个词条` 的告警（去重后一条键一次）。</para></summary>
        public static string Term(string key)
        {
            string t;
            if (!string.IsNullOrEmpty(key) && Terms.TryGetValue(key, out t) && !string.IsNullOrEmpty(t)) return t;
            // ⚠️ `Loc.T(null)` / `Loc.T("")` 都返回空串 ⇒ 原来那个 `key ?? ""` 的语义由它兜住。
            return Loc.T(key);
        }

        /// <summary>`DeckEditingWindow.TrySaveDeck` 那一支的两颗钮（原版字面量，见文件头 ②：
        /// `0x42BE418` / `0x42BE120`）。**顺序照原版**：`ShowPopUp(…, primaryButton, secondButton)` 里
        /// primary = `LiveButtons[0]` = **`ButtonLeft`** ⇒ **Discard 在左、Cancel 在右**。</summary>
        public const string KeyDiscard = "MainMenu/General/Discard";
        public const string KeyCancel = "MainMenu/General/Cancel";

        // ============================================================ 状态
        string _msgKey, _primaryKey, _secondaryKey;
        System.Action _onPrimary, _onSecondary;
        Label _msgLb, _primaryLb, _secondaryLb;
        Transform _shadeHit;
        /// <summary>两颗钮的**点击接收件**（`WindowButton`，挂在按钮底图那块 quad 上）。
        /// 🔴 **2026-10-12（A454）起由 `RefreshLiveContent` 复用** —— 复用一扇还开着的窗时改它的 `onClick`
        /// （那个字段是**点击那一刻**才读的，见 `WindowButton`）。1 按钮版 / 取图失败 ⇒ 对应那个是 null。</summary>
        WindowButton _primaryHit, _secondaryHit;
        /// <summary>**第二颗钮的那个按钮节点**（`ButtonRight`）。🔴 与 `_secondaryHit` **不是同一个对象**：
        /// `WindowButton` 挂在按钮**底图那块 quad** 上，而原版 `SetActive(false)` 作用于**按钮节点**
        /// （连它的文字一起藏）—— A454 刷新时藏/显的就是本字段。
        /// ⚠️ 1 按钮版**没有**这颗钮（`_secondaryLb` / `_secondaryHit` / 本字段**都是 null**）。</summary>
        Transform _secondaryNode;

        /// <summary>这一扇是照**哪一版 prefab** 建的（`MessagePopupWindow2Buttons` / `MessagePopupWindow`）。
        /// 🔴 **建实例那一刻定死、之后不变**（原版：`_LoadPopUpAndShow` 只在**没有** `popUpWindow` 时按
        /// 「按钮数 > 1」挑 prefab，之后复用同一个实例不再换 prefab）。</summary>
        public bool TwoButtons { get { return _prefabTwo; } }
        bool _prefabTwo;
        /// <summary>这扇窗**头上的 prefab 根名**（自检按它钉「建的是哪一版」）。</summary>
        public string PrefabName { get { return _prefabTwo ? "MessagePopupWindow2Buttons" : "MessagePopupWindow"; } }
        /// <summary>这一次调用给没给第二颗钮（原版 `ShowPopUp(…, primaryButton, secondButton)` 的 `!= null`）。
        /// ⚠️ **与 `TwoButtons` 不是同一件事**：2 按钮版 prefab 的实例被复用去弹一条单钮消息时，
        /// 原版是**把那颗多余的 `LiveButtons[i].SetActive(false)`**（`ConfigurePopUp` 那个循环），
        /// ⛔ 不是把面板换成 1 按钮版（那要 `Destroy` 重建，原版没做）⇒ 本类照它做。</summary>
        public bool HasSecondary { get { return !string.IsNullOrEmpty(_secondaryKey); } }

        public string MessageKey { get { return _msgKey; } }
        public string PrimaryKey { get { return _primaryKey; } }
        public string SecondaryKey { get { return _secondaryKey; } }
        /// <summary>**画出来的**正文（= `Term(MessageKey)`；2026-10-19 `A1012` 起 = `Terms` 有就印它、
        /// 否则 `Loc.T(键)`、**两处都没有才是键名**）。自检量它，⛔ 别拿 `MessageKey` 当渲染结果。</summary>
        public string MessageShown { get { return _msgLb != null ? _msgLb.Text : null; } }
        public string PrimaryShown { get { return _primaryLb != null ? _primaryLb.Text : null; } }
        public string SecondaryShown { get { return _secondaryLb != null ? _secondaryLb.Text : null; } }
        /// <summary>压暗层那颗**吸收**命中区（`MenuDraw.Absorb` 建的；自检拿 `MenuDraw.WasAbsorb` 核它）。</summary>
        public Transform ShadeHitNode { get { return _shadeHit; } }

        // ============================================================ 建 / 开

        /// <summary>`WindowsManager.ShowMessagePopUp` 唯一走的建法（**照原版那条链**：
        /// `ShowPopUp` → `LoadPopUpAndShow` → `ConfigurePopUp` → `OpenWindow`）。
        /// <para>三个入参**照原版都是 I2 术语键**（见文件头 ①）。⚠️ **2026-10-19（`A1012`）就地订正**：
        /// 这里原来写「⛔ 不是明文」—— **实际两套约定并存**：壳侧 20+ 个调用点传的**是明文**
        /// （`Shell/WindowsManager.cs:1517` 自己写着「调用点传的都是【明文】」），只有
        /// `Deck/DeckRuntime.cs` 那几处传的是**键**。两种都能用：`Term()` 先查 `Terms`、再转 `Loc.T`，
        /// 明文不是键 ⇒ 原样返回（见 `Term` 的 doc）。</para>
        /// <para>⚠️ `mgr == null` 时**也把它建出来**、只出声不挂锚点（同 `TrophyInfoPopup.Open` 那条兜底：
        /// 红线「不许静默失败」）。</para></summary>
        public static PopUpGameWindow Create(WindowsManager mgr, string messageKey,
                                             string primaryKey, System.Action onPrimary,
                                             string secondaryKey = null, System.Action onSecondary = null)
        {
            bool two = !string.IsNullOrEmpty(secondaryKey);
            var go = new GameObject(two ? "MessagePopupWindow2Buttons" : "MessagePopupWindow");
            var win = go.AddComponent<PopUpGameWindow>();
            win._prefabTwo = two;                        // 🔴 prefab 版**建实例时定死**（复用不再换，见 `TwoButtons`）
            win.type = WindowType.Popup;                 // 实证 `type = 1`
            win.placement = WindowsPlacement.Popup;      // 实证 `windowsPlacement = 15`
            win.closeOnEsc = false;                      // 实证 `closeOnESC = 0`（**ESC 关不掉它**）
            win.extraScaleSmallScreen = 1f;              // 实证 `extraScaleSmallScreen = 1.0`（=「不覆盖」语义）
            win.Configure(messageKey, primaryKey, onPrimary, secondaryKey, onSecondary);
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            if (mgr == null)
                Debug.LogWarning("[PopUp] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器（锚点也没挂）。");
            return win;
        }

        /// <summary>= 原版 `ConfigurePopUp(文案, localizeTexts, 按钮数组, closeOnEsc)` 那一半**记参数**
        /// —— 两条路（新建 / 复用）都先走它，见下。换文案/换钮 = 再调一次它 + `OpenWindow`
        /// （原版就是 `ConfigurePopUp` + `OpenWindow` 这对）。
        ///
        /// <para>🔴 **2026-10-12（A454）就地改实现（铁律 5）**：本方法原来**只写那 5 个字段**，注释里给的理由是
        /// 「真的建树在 `Open()` 里 —— 我们的窗是**代码建**的，**`Open()` 恒重建** ⇒ 复用同一扇时自然刷新内容」。
        /// **那句前提 A217② 之后就不成立了**：`GameWindow.TryOpen` 已照原版按 `CurrentState` 分三档
        /// （**`Open` 支一个字段都不写就早退**，判据 → `Shell/WindowsManager.cs` 的 `TryOpen`）
        /// ⇒ 复用一扇「还开着」的弹窗时**建树那一步根本不会再跑**，画面上还是**上一条消息**的正文 / 钮标 / 回调
        /// （**静默** —— 画面就是错的，控制台一声不响）。
        /// 原版不是这样：`PopUpGameWindow__ConfigurePopUp.c`（本件亲读，`d:/2/tools/decomp_full/`）是
        /// **直接写活组件** —— ① 先 `PopUpGameWindow__SetText(this, text, localizeTexts, 0)`（写 `MessageText`）；
        /// ② 再逐颗 `LiveButtons`：超出这次按钮数的 `SetActive(false)`、其余 `SetActive(true)`、
        /// 逐颗按 `buttons[i].Text` 贴标签（`localizeTexts` 为真时过 `GetTranslation`）、
        /// 把回调挂到那颗钮的 `onPress` UnityEvent 上（`:85-106` / `:144`）。
        /// ⇒ **复用那一刻内容是会变的。**</para>
        ///
        /// <para>本方法照它：**窗已经建过**（`Build()` 跑过 ⇒ 标签引用非空）时，把正文/钮标/回调/
        /// 「多余那颗钮的显隐」写进**活着的组件**（→ <see cref="RefreshLiveContent"/>）。
        /// ⛔ **一个对象都不重建** —— A217② 要的正是「`Open` 支不重建**内容对象**」：这里只改已有 `Label` 的
        /// 文本与 `WindowButton.onClick`，节点 identity 一个不换（两者互不矛盾：A217② 管的是**重建**，
        /// 本条管的是**把新值写进那几个活着的字段**）。
        /// ⚠️ **还没建过**（`Create()` 那一路：`Configure` 排在 `Open()` 之前）⇒ 只记字段，
        /// `Open() → Build()` 照旧读它们（那一路本来就对）。</para></summary>
        public void Configure(string messageKey, string primaryKey, System.Action onPrimary,
                              string secondaryKey = null, System.Action onSecondary = null)
        {
            _msgKey = messageKey ?? "";
            _primaryKey = primaryKey ?? "";
            _secondaryKey = secondaryKey;
            _onPrimary = onPrimary;
            _onSecondary = onSecondary;
            // ⛔ **不在这里改 `_prefabTwo`** —— prefab 版是建实例那一刻定的（见 `TwoButtons` 的注释）。
            // = 原版 `ConfigurePopUp` 第二段：**窗已经在场就直接写活组件**（A454；
            //   没建过时 `_msgLb`/`_primaryLb` 都是 null ⇒ 自动跳过，交给 `Open() → Build()`）
            if (_msgLb != null || _primaryLb != null) RefreshLiveContent();
        }

        /// <summary>把**当前这几个字段**写进**活着的组件** —— = 原版 `ConfigurePopUp` 第二段（正文那一段
        /// 见 `Configure` 的注释）。
        /// <para>🔴 **只在窗已经建过时调**（`Build()` 跑过 ⇒ 标签引用非空）：那一刻 `Open()` 不会再跑
        /// （A217② 的 `Open` 支），不刷的话画面上还是**上一条消息**。⛔ **不重建任何对象**。</para>
        /// <para>⭐ **改坏法**（自检落点 → 见报告的「待接线」）：删掉 `Configure` 里那句 `RefreshLiveContent()`
        /// ⇒ 「连续弹两条**不同文案**的消息」时第二条**显示的仍是旧文案** ⇒ 那条断言必须红；
        /// 只改正文、不改钮标/回调 ⇒ 钮标与回调那一对断言必须红。</para></summary>
        void RefreshLiveContent()
        {
            SetLabelText(_msgLb, Term(_msgKey));
            SetLabelText(_primaryLb, Term(_primaryKey));
            // ⚠️ 第二颗钮**只在这次真给了它**时才写标签 —— = 原版那个循环的上界 `buttons.Count`
            //    （超出的那几颗**只** `SetActive(false)`、**不**写标签 ⇒ 它们身上留着上一条消息的字）。
            //    那颗钮那时是被藏起来的（下面那句 `SetActive`），所以那截旧字**看不见**。
            if (HasSecondary) SetLabelText(_secondaryLb, Term(_secondaryKey));
            if (_primaryHit != null) _primaryHit.onClick = _onPrimary;
            if (_secondaryHit != null) _secondaryHit.onClick = _onSecondary;
            // 多余的钮：= 原版那个循环里「超出给定按钮数的 `SetActive(false)`」那一支。
            // ⚠️ 藏/显的是**按钮节点**（连它的文字一起），⛔ 不是底图那块 quad —— 见 `_secondaryNode`。
            if (_secondaryNode != null) _secondaryNode.gameObject.SetActive(HasSecondary);
            else if (HasSecondary)
                // 🔴 出声（红线「不许静默失败」）：调用方这次**给了**第二颗钮，而这扇实例是 1 按钮版 prefab
                //    ⇒ 那颗钮**画不出来**、它的回调也永远走不到。⚠️ **原版这里同样是静默的**
                //    （`ConfigurePopUp` 那个循环只遍历本实例的 `LiveButtons`）—— 我们照原版**不改行为**，
                //    但按本仓红线**说出来**（否则调用点会以为「取消」那颗钮在，实际点了等于没点）。
                Debug.LogWarning("[PopUp] 这次给了第二颗钮，但当前这扇是 **1 按钮版** prefab"
                               + "（`MessagePopupWindow`）⇒ 那颗钮与它的回调**都不存在**"
                               + "（原版同此：实例一旦建成就不再换 prefab，见 `TwoButtons`）。");
        }

        /// <summary>改一颗**已经建好**的标签的文本（null / 内容没变 ⇒ 空做）。
        /// ⚠️ 走 `Label.SetText`（它会 `ForceMeshUpdate` + 重跑自适应 + `RefreshBounds`），
        /// ⛔ **不是**新建一个 Label —— 节点 identity 必须保持（A217②）。</summary>
        static void SetLabelText(Label lb, string s) { if (lb != null) lb.SetText(s ?? ""); }

        public override void Open()
        {
            Build();
            // 两条如实出声（红线）：① 词条表本地没有 ⇒ 现在印的是**键**；② 音效没做。
            Debug.Log("[PopUp] 开了 `" + name + "`（" + (TwoButtons ? "2" : "1") + " 按钮版）· 正文键 `"
                    + _msgKey + "` · 左钮 `" + _primaryKey + "`"
                    + (TwoButtons ? " · 右钮 `" + _secondaryKey + "`" : "")
                    + " —— 文案走 `Term()`（本类 `Terms` → `Core/Loc.cs`；2026-10-19 `A1012` 起转 `Loc`）"
                    + "⇒ **表里有就印词条、两处都没有才是键名**；"
                    + "⚠️ 开/关音效没做（`useDefaultCloseSoundIfNull = 1`，默认音在远端音频表）。");
        }

        /// <summary>照两扇 prefab 逐节点搭（**自检与运行时走同一条路**）。</summary>
        public void Build()
        {
            var root = transform;
            MenuDraw.ClearChildren(root);

            var winR = TwoButtons ? Win2R : Win1R;
            var maskR = TwoButtons ? Mask2R : Mask1R;
            var msgR = TwoButtons ? Msg2R : Msg1R;
            var btnsR = TwoButtons ? Btns2R : Btns1R;

            // ---- 1) `Menu Dark Background`：整屏压暗（无图纯色）+ **吃射线的命中区**（原版那颗 `Image`
            //    `m_RaycastTarget = 1`；`BackgroundCloseButton.window` 是空引用 ⇒ **点了什么也不做**）----
            MenuDraw.Rect(root, CardArt.Solid(), ShadeR, "Menu Dark Background", QShade, ShadeColor);
            _shadeHit = MenuDraw.Absorb(root, "AbsorbHit", ShadeR, QShade, QPanel);
            if (_shadeHit == null)
                Debug.LogWarning("[PopUp] 压暗层那颗吃射线的命中区**没建出来**（`MenuDraw.Absorb` 返回 null）"
                               + " —— 模态就挡不住下面的点击了。");

            // ---- 2) `Window`（原版是个**空节点**，只有 RectTransform；尺寸写死在 prefab 里）----
            var winNode = MenuDraw.Node(root, "Window", winR);

            // ---- 3) `Generic Popup Background`（九宫 `40k_popup`，锚点 stretch ⇒ 与 `Window` 同矩形）----
            MenuDraw.Nine(winNode, Tex(ArtPopup), winR, PopupBorder, PopupTexW, PopupTexH,
                          QPanel, null, true, "Generic Popup Background");

            // ---- 4) `Mask`（原版那颗 `Image,Mask` 是 **`showGraphic = 0`** ⇒ **它自己不画**，只当模板）
            //    ⇒ 我们**只建节点**（层级 / 名字照原版：`Window > Mask > Background fill`）。
            //    🔴 **别在这里补一块九宫格**：它那颗 Image 的 sprite / 九宫与 `Generic Popup Background`
            //    完全相同、矩形只差 10px 内缩，而**同档**（都是 `QPanel`）⇒ 补上等于把边框**错位重画一遍**
            //    （`showGraphic = 0` 这条判据就是禁它的）。面板边由上面第 3 步那块画，够了。
            var maskNode = MenuDraw.Node(winNode, "Mask", maskR);

            // ---- 5) `Background fill`（`40k_popup_texture` 平铺，`ppuMultiplier 2` ⇒ 64 一格）。
            //    ⚠️ **我们没有掩码体系**：原版这块是 `Mask` 的子件、被那颗模板裁成圆角形状；
            //    我们直接按 `Mask` 的矩形铺（**矩形与它逐位相同** ⇒ 差的是四个圆角那一小圈，如实标）。----
            MenuDraw.Tiled(maskNode, Tex(ArtPopupFill), maskR, FillTilePx, QFill, "Background fill");

            // ---- 6) `MessageText`：fs 40 · auto[4,40] · **base 36** · 居中 · **折行开**（`折行=1`，别给它关掉）----
            //    ⚠️ `base` = `MsgAutoBasePx`（原版 `m_fontSizeBase`，A1188）—— 原来传缺省 `0`
            //    = 「base 退回调用方那一档」，与原版**不一致**（详见 `MsgAutoBasePx` 的注释）。
            _msgLb = MenuDraw.Text(winNode, msgR, Term(_msgKey), Color.white, "MessageText",
                                   MsgFontPx, QText, msgR.W, MsgAutoMinPx, MsgFontPx, MsgAutoBasePx);
            if (_msgLb == null) Debug.LogWarning("[PopUp] `MessageText` 没建出来（标签建失败）");

            // ---- 7) `Buttons` 行（它是**布局组容器**，子件位置由下面的成品矩形定死）----
            var btnsNode = MenuDraw.Node(winNode, "Buttons", btnsR);

            // ---- 8) 按钮 ----
            // 🔴 照原版 `ConfigurePopUp` 那个循环：**按 prefab 那一版把 `LiveButtons` 全建出来，
            //    超出这次给到的按钮数的那些 `SetActive(false)`**（2 按钮版实例被复用来弹单钮消息时，
            //    原版就是「藏掉右钮」、面板仍是 850×430 —— ⛔ 不是换成 1 按钮版重搭）。
            if (TwoButtons)
            {
                // 左 = **主按钮**（原版 `LiveButtons[0]` = `ButtonLeft`）· 右 = 第二颗
                // 🔴 A454：`out` 那两颗引用（点击件 + 按钮节点）**存下来** —— 复用这扇窗时 `RefreshLiveContent`
                //    要靠它们改回调/藏钮（`Open()` 不会再跑，见 `Configure`）。
                _primaryLb = MakeButton(btnsNode, "ButtonLeft", BtnL2R, Btn2FontPx, 0f, 0f,
                                        _primaryKey, _onPrimary, true, out _primaryHit, out _);
                _secondaryLb = MakeButton(btnsNode, "ButtonRight", BtnR2R, Btn2FontPx, 0f, 0f,
                                          _secondaryKey, _onSecondary, true, out _secondaryHit, out _secondaryNode);
                if (!HasSecondary)
                {
                    // = 原版 `ConfigurePopUp` 循环里「超出给定按钮数的 `SetActive(false)`」那一支
                    if (_secondaryNode != null) _secondaryNode.gameObject.SetActive(false);
                    else Debug.LogWarning("[PopUp] 拿不到 `ButtonRight` 那个按钮节点 ⇒ 那颗多余的钮没藏掉"
                                        + "（`MakeButton` 的 `node` 出参为 null；`ConfigurePopUp` 里那一步是"
                                        + "「超出的 SetActive(false)」）。");
                }
            }
            else
            {
                _primaryLb = MakeButton(btnsNode, "Generic UI Button", Btn1R, Btn1FontPx,
                                        Btn1TextAposX, Btn1TextAposY, _primaryKey, _onPrimary, false,
                                        out _primaryHit, out _);
                // A454：1 按钮版**没有**第二颗 ⇒ 这三样必须显式清掉（否则重建后 `RefreshLiveContent`
                // 会去动上一辈子的对象 —— 那是本类唯一会「静默改到别人身上」的地方）
                _secondaryLb = null; _secondaryHit = null; _secondaryNode = null;
            }
        }

        /// <summary>一颗钮：底图（`40K_button`）+ `Button Text`（stretch + `sizeDelta.x = −26` + ARF 宽控高）。
        /// 返回那颗**画出来的**标签（自检读它 —— ⛔ 不是键变量）。
        /// <para>🆕 **A454 加两个出参**（复用那一路要用）：`hit` = 挂在**底图那块 quad** 上的 `WindowButton`
        /// （改回调要用它）；`node` = **那颗按钮节点**（`ButtonRight` / `ButtonLeft` / `Generic UI Button`，
        /// 藏/显要用它 —— 原版 `SetActive` 作用于它、**不是** quad 那块）。⚠️ 取图失败时 `hit` 为 null。</para></summary>
        /// <param name="keepAspect">原版 `Image.m_PreserveAspect`：**1 按钮版 = 0（拉伸）· 2 按钮版 = 1（等比放进框）**
        /// —— 两扇不一样，逐扇实读，⛔ 别统一。</param>
        Label MakeButton(Transform parent, string nodeName, PxRect r, float fontPx,
                         float aposX, float aposY, string key, System.Action onClick, bool keepAspect,
                         out WindowButton hit, out Transform node)
        {
            var go = MenuDraw.Node(parent, nodeName, r);
            node = go;      // 出参先落一个（下面有 `return null` 那条早退，出参必须在所有路径上都赋过值）
            hit = null;
            var q = MenuDraw.Rect(go, Tex(ArtButton), r, "Image", QContent, BtnColor, keepAspect);
            if (q == null)
                Debug.LogWarning("[PopUp] 取不到 `" + ArtButton + "`（按钮底没画）—— 导入器：`工具/import_original_art.py`");
            else
            {
                hit = q.gameObject.AddComponent<WindowButton>();
                hit.onClick = onClick;
                // 原版 `m_Transition = 2(SpriteSwap)`：HL = `40K_button_hover` · P = `40K_button_pressed`
                // （`m_SpriteState` 实读；`BindSelf` 按「常态图名 + _hover/_pressed」推出来，与 prefab 那两颗逐名相同）。
                hit.BindSelf(ArtButton);
            }

            // `Button Text`：框宽 = 按钮宽 − 26、框高 = 框宽 ÷ 5.140573（ARF 宽控高）·
            // 中心 = 按钮中心 + `anchoredPosition`（**画布 y 向下 ⇒ apos.y 要减**）。
            float tw = r.W - TextInsetX, th = tw / TextAspect;
            float cx = (r.x1 + r.x2) * 0.5f + aposX, cy = (r.y1 + r.y2) * 0.5f - aposY;
            var tr = new PxRect(cx - tw * 0.5f, cy - th * 0.5f, cx + tw * 0.5f, cy + th * 0.5f);
            // 🔴 A1188：`base` = `BtnAutoBasePx`（原版 `m_fontSizeBase = 12`，**1/2 按钮版逐值相同**）——
            //   原来传缺省 `0`（= 退回调用方那一档），与原版不一致。判据见 `BtnAutoBasePx` 的注释。
            var lb = MenuDraw.Text(go, tr, Term(key), Color.white, "Button Text",
                                   fontPx, QText, tw, BtnAutoMinPx, fontPx, BtnAutoBasePx);
            if (lb == null) { Debug.LogWarning("[PopUp] `" + nodeName + "` 的字没建出来"); return null; }
            // 原版那一站 `折行=0`（靠 `auto 12~38/40` 缩字号）⇒ 照它关掉折行。
            // ⚠️ 上面那个 `wrapPx` 只为把**框宽**设上（自适应按框量），模式按原版走。
            lb.SetWrapping(false);
            return lb;
        }

        // 🔴 **2026-10-12（A454）删掉了 `FindChildByName`**：它唯一那个调用点（`Build()` 里按名字找
        //    `ButtonRight` 去 `SetActive(false)`）已换成 `MakeButton` 的 **`node` 出参**。
        //    ⛔ 别再按名字找那颗钮 —— 「按名字找」与「建的时候就把引用留下来」是同一件事的两份写法，
        //    两份迟早不一致（铁律 §三）。`grep -rn FindChildByName` 全仓已零命中。

        /// <summary>取图；**取不到就出声**（红线 —— `MenuDraw.Rect/Nine/Tiled` 对 `tex == null` 是静默返回 null）。</summary>
        static Texture2D Tex(string name)
        {
            var t = CardArt.MenuUi(name);
            if (t == null)
                Debug.LogWarning("[PopUp] 取不到原版图 `" + name + "`（这一层不画）—— 导入器：`工具/import_original_art.py`");
            return t;
        }
    }
}
