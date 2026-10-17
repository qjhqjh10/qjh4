// SettingsPanel.cs — 对局内的**设置面板**（原版 `BattleSettingsPanel` / `BattleSettingsWindow`）
//
// 为什么要有它：**投降按钮就在这里面** —— 原版 `BattleSettingsWindow.cs:9` 字段
// `resignButton` / `resignButtonText`（还有 closeButton 和三根音量滑块），
// 入口是右上角那个 `SettingsBtn`（`BattleSettingsWindow` 由 `SettingsBtn` 打开）。
// 之前只找到 `BattleResult.Forfeit` 这个枚举、找不到按钮，就是漏了「它在设置面板里」。
//
// 数值出处（`资料/战斗规格/战斗重建_0827/战斗界面JSON权威表_0827.md` + 运行时 dump）：
//   · 面板本体 743.2 × 758.6（`BattleSettingsPanel` 自己的 rect）
//   · 关闭钮 75 × 75（`Generic Close Button`，图 `UI_Button_Round_background` + 子图 `40k_bt_close`）
//   · 面板底 图 `40k_popup_texture`、边框 图 `40k_popup`
//   · 🆕 **`Auto Zoom` 那一行**（**A424**，原版 `BattleSettingsPanel/Auto Zoom Toggle`，字段
//     `BattleSettingsWindow.autoZoom`（`EverguildToggle`））—— 逐值亲读
//     `bundle_scenes_scenes_battlearena1`：`RectTransform_3099`(行) / `_2654`(勾选框) / `_3268`(文字)
//     + `MonoBehaviour_4356`(Toggle) / `_3977`(TMP) / `_5032`(I2 `Localize`)；几何见下面那组 `Az*` 常量。
// ⚠️ **2026-09-18 更正：投降按钮的 rect 一直都拿得到，原来那句「取不到 ⇒ 位置是我们挑的」是错误否定。**
//    原来写「dump 里 `BattleSettingsPanel` 那一支只列到 Auto Zoom / 关闭钮 / Debug 那排，没有 resignButton 节点」，
//    实际是当年**漏了它挂在 `Bottom buttons` 子节点下**、也漏了运行时 dump 里本来就有这颗
//    （`runtime_ui_dump_Battle_Arena_1.tsv` 路径 `.../BattleSettingsPanel/Bottom buttons/Resign Button`）。
//    **原版真值**（`assets_full/bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_3211.json` 亲读）：
//    面板内（原点=面板中心，y 向上）中心 **(−171.7, −310.5)** px、尺寸 **300×90** px（= 面板左下角，纵向 90.9% 处），
//    底图 `40K_button`（9-slice border 234,46,234,46）、染色 **(0.3686,0.8941,0.5874,1) 绿**、文字 `Resign` fs38。
//    ⚠️ 13 个战场各存一份副本，**只有 `battlearena2` 不同**（anchor(0,0)/ap(0,0)）—— 我们照多数那份做。
//    ✅ **2026-09-19 已照真值改完代码**：九宫格 `40K_button`（Sliced，border 234/46）+ 绿染 (0.3686,0.8941,0.5874,1)
//      + 白字 fs38。完整规格与出处见 `资料/普查产出_0918/第18行_UI三小条_规格.md` §①。
// ⚠️ 三根音量滑块：见 §②（原版写 `AudioMixer.SetFloat("Volume"+MixerType 枚举名)`）。
//
// ═══════════════════════════════════════════════════════════════════════════════════════
// 🔴 **2026-10-16（`T09` 批 2 · `PopUps` 组）：本面板已从【世界空间 quad】迁到【uGUI 子画布】**
//   判据与 8 步配方 → `资料/任务线/战斗线/任务/T09_整层换uGUI.md` §五·补⑨/⑩；
//   宿主映射 → `资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一（`SettingsPanel` 那一行）。
//   · **子画布** = `HudCanvas.EnsureSubCanvas(null, "BattleSettingsPanel Canvas",
//     HudCanvas.SortingLayer.PopUps, 0)`。原版宿主 = `BattleSettingsPanel` @
//     `BattleHud/Canvas/FrontCanvas/Safe area FrontCanvas`，**`PopUps` 层 / order 0 / 兄弟序 10/14**
//     （`CV_2763` · `RT_3535` · T09b §A·2#8）。
//   · **单位**：uGUI 档 **1 单位 = 1 设计 px**（见 `HudCanvas` 文件头那条推导）⇒ 本文件那个
//     `U(px)` 现在**恒等于 `px`**（⛔ 不再是 `px/108`）；`SetAutoFitBox` / `SetVAlign` /
//     `SetScriptHeight` 收的也都是 px（最后一处传 `pxPerUnit = 1`）。
//   · **坐标**：图/字一律**中心锚 + 面板中心为原点**（y 向上）—— 与 `Label.CreateUgui` /
//     `HudCanvas.MakeSlicedPiece` 同一个口径 ⇒ 本文件那几组「面板内 px」常量
//     （`Az*` / `Mute*` / `Lang*` / `Lst*` / `Skip*` / `Resign*`）**原样**搬进去。
//   · **面板帧 = `_panel`**（画布下那个 743.2×758.6 px 的节点，中心 = 设计中心 (960,540)）；
//     命中判定凡是要「面板内 px」的，一律在**它**的局部空间里算（⛔ 不是 `transform` —— 见下）。
//   · 🔴 **例外（如实记）**：三根 `WfSlider`（`Battle/WfSlider.cs`）**2026-10-16 批 2 时还在世界空间**
//     —— 那一整族（几何 / 命中 / 九宫格）内部全按「世界单位 = px ÷ 108」写死，要迁它得先给它加一个
//     uGUI 档，而它**不在本件白名单**（当时已写进报告，交调度台裁）。
//     ⇒ 它们当时仍挂在**世界空间那层壳**（本组件自己的宿主 `transform`）上，⛔ **没有**挂进画布。
//     ⚠️ **两条代价**（当时如实记的；**2026-10-16 块 6 迁完之后两条都消失**，见下面那段）：
//       ① 子画布在 `PopUps` 层 ⇒ **面板底会盖住那三根滑块**（它们当时仍画在 `Default` 层）；
//       ② 它们当时仍跟着 `HudRoot` 的震屏走，而本面板其余件（已在画布上）不再跟。
//     报告 → `资料/普查产出_1016并行/T09批2_SettingsPanel.md`。
// ═══════════════════════════════════════════════════════════════════════════════════════
// 🔴🔴 **2026-10-16（`T09` 块 6 · 第二件）就地改：上面那条「例外」【已不存在】—— 三根已迁到画布。**
//   `WfSlider.Create` 加了双模式（`uguiHost:` 形参；宿主为 `null` ⇒ **世界档、逐位不变**），本面板的
//   `Build()` 传 `uguiHost: _panel` ⇒ 三根与面板其余件**同挂那块 `PopUps`/0 的子画布**
//   （⛔ 不新增排序层、不进 `SubCanvasRank` —— 它们是**面板内部件**，判据 → `Battle/WfSlider.cs` 文件头）。
//   ⇒ **本组件自己的宿主 `transform` 现在成了一个【不装任何件】的壳**（只剩那个 `sizeDelta`）。
//   ⚠️ **那个壳【必须留着】**：`Editor/BattleScene.cs` §A218 的「设置面板**世界壳** = 743.2×758.6」
//      那条断言读的就是它（同节另一条读本件的 `PanelRect`）⇒ **删了会把那条断言带红**。
//   🔴 **仍与原版不同的一处（如实记，⛔ 不是漏了）**：这层「世界壳」是**我们自己的**（原版没有中间这一级），
//      它与 `T09` §五·补⑩ 二 那条「我们的嵌套结构未必等于原版」是同一族 —— **本笔不动它**。
// ═══════════════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;      // 🆕 2026-10-18（A921）：语言下拉那 12 行（`List<LangRow>`）
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    public class SettingsPanel : MonoBehaviour
    {
        /// <summary>面板是不是开着。开着时**吃掉点击**，别让底下的棋盘也响应。</summary>
        public bool Visible { get; private set; }
        /// <summary>点「投降」时回调（驱动层接 `BattleDriver.Forfeit`）</summary>
        public Action OnResign;
        /// <summary>点「对手难度」时回调（驱动层换下一档，再用 <see cref="SetDifficulty"/> 回写文字）</summary>
        public Action OnCycleDifficulty;

        // 🔴 **2026-10-16（`T09` 批 2）**：这一族从 `ImageQuad`（世界空间 quad）换成 `HudPiece`
        //   —— 现在**全是 uGUI 件**（`RectTransform + Image`，建在子画布上）。
        //   `HudPiece` 的适配面（`SetTint` / `SetTexture` / `SetAspect` / `Contains` / `WorldW/H` /
        //   `transform` / `gameObject`）把下游用法挡在类型后面，见 `Battle/HudPiece.cs` 文件头。
        HudPiece _bg, _close, _shade;
        Label _title, _resignText, _closeText;
        HudPiece _closeIcon;
        HudPiece _diffBtn;
        Label _diffLabel, _diffValue;
        /// <summary>投降钮的根。⚠️ **不是 `ImageQuad`** —— 原版底图 `40K_button` 是
        /// `Image.Type = Sliced`（border 234,46,234,46），要九宫格；单个 quad 拉不出那个形。
        /// 🔴 **2026-10-16（`T09` 批 2）**：现在是 **一张 `Image (Sliced)`**（`HudCanvas.MakeSlicedPiece`
        /// —— uGUI 里九宫格就是这一件事，比九块 quad 更贴原版）。</summary>
        HudPiece _resignBtn;
        Texture _resignTex;
        /// <summary>🆕 2026-10-18（A991）「跳过教程」钮的根。⚠️ 同 `_resignBtn`：**不是 `ImageQuad`**
        /// （底图 `40K_button` 是九宫格）⇒ uGUI 一张 `Image (Sliced)`。</summary>
        HudPiece _skipBtn;
        /// <summary>那颗钮上的字（原版 `Button Text`，fs38，白，居中）。</summary>
        Label _skipText;

        // ---- 🆕 2026-10-12（A424）：「Auto Zoom」那一行（原版 `Auto Zoom Toggle`）----
        /// <summary>勾选框底图（原版 `Toggle.targetGraphic`，图 `40K_dropdown_bg`）。</summary>
        HudPiece _azBox;
        /// <summary>勾（原版 `Toggle.graphic`，图 `40K_settings_icon_checkmark`）。</summary>
        HudPiece _azCheck;
        /// <summary>那一行的文字（原版 `Label`，TMP `m_text = "Auto zoom"`）。</summary>
        Label _azLabel;

        // ---- 🆕 2026-10-16（`T01`）：`ChatToggle`「静音对手」那一行（原版 `Volume Sliders/ChatToggle`）----
        /// <summary>OFF 那半的底图（原版 `Background`，图 `40K_toggle_off`）—— ⚠️ **原版这半恒 active**
        /// （`SetActive` 只打 ON 那半，见 `RefreshMuteChat` 那段注释）。</summary>
        HudPiece _muteOffBg;
        /// <summary>ON 那半的底图（原版 `Background On`，图 `40K_toggle_on`）—— 原版就是**整块盖上去**的
        /// （两块同 rect、ON 那块不透明 ⇒ 盖住 OFF 那半的底图 + 勾 + `OFF` 字）。</summary>
        HudPiece _muteOnBg;
        HudPiece _muteOffChk, _muteOnChk;
        /// <summary>左边那句说明（原版 `Label`，TMP `"Mute opponent"`，词条 `MuteLabelTermKey`）。</summary>
        Label _muteLabel;
        /// <summary>`OFF` / `ON` 两颗字（原版 `OffText` / `OnText`，各自挂在**自己那半的勾**下面）。</summary>
        Label _muteOffText, _muteOnText;
        /// <summary>开关当前是不是 ON（原版 `BattleSettingsWindow` 上那颗 `EverguildToggle.isOn`）。
        /// 🔴 **本局内存**（原版 `VoiceLinesController.chatMuted` 是 `[SerializeField]` 的一个 bool、
        /// `Initialize` 无条件写 0）—— **不是 `PlayerPrefs`、不是服务器**。</summary>
        bool _muteChatOn;

        // ---- 🆕 2026-10-17：「Language Selector」那一行（原版 `LanguageSelector.languagesDropdown`）----
        /// <summary>下拉框底图（原版 `LanguagesDropdown` 那颗 `Image`，图 `40K_dropdown_field_closed`）。</summary>
        HudPiece _langField;
        /// <summary>框右端那个箭头（原版 `Arrow`）。</summary>
        HudPiece _langArrow;
        /// <summary>框里那行**当前语言名**（原版 `LanguagesDropdown > Label`）。</summary>
        Label _langCap;
        /// <summary>左边那颗标签（原版 `SelectLanguageText`，`'Select Language'`）。</summary>
        Label _langSelText;
        /// <summary>上一次 `PointerFrame` 收到的是不是「按住」—— 本面板自己判**抬起**那一帧用（见 `LangPointerFrame`）。</summary>
        bool _wasDown;
        /// <summary>这一轮「按住」是**从语言那一行**开始的（抬起时只有它还为真才算点它）。</summary>
        bool _langArmed;

        // ---- 🆕 2026-10-18（A921）：语言下拉那 12 行列表的状态 ----
        /// <summary>`LanguagesDropdown > Template` 原型（**恒 inactive** —— 原版出厂 `m_IsActive = 0`）。
        /// 建一份（关着），只为「照原版结构」与自检能定位它，⛔ 它不参与显示。</summary>
        Transform _langTemplate;
        /// <summary>真正显示的那份（原版 `TMP_Dropdown.Show()` 的 `Instantiate` 克隆体，运行时名字 = `"Dropdown List"`）。
        /// **首开时才建**（= 原版 `CreateDropdownList`）；关掉**不销毁**（原版淡出后 `Destroy` —— 我们不做淡出，
        /// 见 `ShowLangList`；这不影响任何一个可观察值）。</summary>
        Transform _langList;
        /// <summary>列表里 12 行那三件 + 行号。</summary>
        readonly List<LangRow> _langRows = new List<LangRow>();
        struct LangRow { public HudPiece Bg, Chk; public Label Lb; public int Idx; }
        /// <summary>按下那一刻记下「这一下按在哪」：`-1` = 不在行上。抬起来还落在同一行才算点它
        /// （uGUI `IPointerClickHandler` 的语义，与 `LangPointerFrame` 原有那条同形）。</summary>
        int _langRowArmed = -1;
        /// <summary>按下那一刻指针落在**列表的空白处**（列表开着、又不在任何一行上）——
        /// 原版那颗铺满全屏的透明 `Blocker` 就是干这个的（`CreateBlocker:1009` + `onClick → Hide`）。</summary>
        bool _langBlankArmed;
        /// <summary>自检专用：把「ESC 按下了」钉死（`null` = 走真实键盘 —— **生产恒为 null**）。
        /// 形状照 `BattleDriver.EscapePressedForTest`（批处理里 `Keyboard.current == null`，见 `ESC` 那段）。</summary>
        public static bool? EscapePressedForTest;
        // ⚠️ A424 曾有一格 `bool _autoZoomHeld`（按下那一帧的边沿 latch）—— **A445 删掉了**：
        //    那一行挪回了「抬起」的点击链（`BattleDriver.SettingsClickAt`），原版那颗 `Toggle` 的
        //    `IPointerClickHandler` 自带「一次抬起点一次」的语义，不需要我们再 latch。

        // ---- 三根音量滑块（原版 `BattleSettingsWindow` 的 music/soundFX/voiceOver）----
        // 位置逐条来自解包（`资料/普查产出_0918/第18行_UI三小条_规格.md` §② + 2026-09-19 逐级解父链）：
        //   滑块中心（面板内，原点=面板中心，y 向上）：Music (−2.00, 120.07) · FX (−2.00, 3.44) · VoiceOver (−2.00, −113.18)
        //   标签：左边缘 x = −282.54（= 轨道左边缘）· **框 631.21×55.00** · 框心 y = 178.17 / 61.57 / −55.13
        //         · 对齐 `Left/**Bottom**` · 折行=1 · fs42 · auto[10~42] · base 36
        //   🔴 **2026-10-16（A712 阶段 2）就地订正（铁律 5）**：这里原来写「**矩形中心** y = 183.74 / 67.11 /
        //   −49.51、盒高 64.14 ⇒ 文字底边 = 盒中心 − 32.07 = 151.67 / 35.04 / −81.58」——**那一组数是错的**
        //   （来历没查清：本笔扫 `Unity/资料/**/*.md` 的 `183.74` / `64.14` / `67.11` / `49.51` **零命中**，
        //   只在 `SettingsPanel.cs` 自己这里找到它）。真值本笔现读（**只读 dump、没改原版**）：
        //   `python 工具/menu_dump.py bundle_scenes_scenes_battlearena1 "Volume Sliders" --depth 3 --no-sprite`
        //   ⇒ 三颗 `Text` 的绝对矩形 `677.5,332.5→1308.7,387.5` · `677.5,449.1→1308.7,504.1` ·
        //   `677.5,565.8→1308.7,620.8`（1920×1080 左上原点）⇒ **631.21×55.00**；
        //   同一份 dump 的三根滑块中心（`418.10 / 534.70 / 651.40`）配我们的 `SliderCy` 反解出面板中心
        //   y = **538.17** ⇒ 框心（面板内、y 向上）= **178.17 / 61.57 / −55.13**、框高 = **55.00**。
        //   ⚠️ 旧值同时**偏高 1.00px**（旧「文字底边」151.67 vs 真值 538.17−387.5 = 150.67）—— 一并改正。
        //   三者共用祖父 `Volume Sliders`（在面板内 (−2.00,−19.89)，701.35×441.40），**没有 Media Tab**
        WfSlider _musicSlider, _fxSlider, _voiceSlider;
        Label _musicLabel, _fxLabel, _voiceLabel;
        // 原版标签是 `Music` / `Sound Effects` / `Voice-overs`（fs42 白，左对齐 / **VAlign Bottom**）。
        // 🔴 **2026-10-18（第十二轮 · W6）改**：原来这里是一个写死的英文 `const string[] SliderNames`
        //    （2026-09-19 的口径「这些地方先用英文」）⇒ **现在走原版词条**（三条键逐条判据 → `Core/Loc.cs`）：
        //      · `MainMenu/Settings/SettingLabel/Music`   → TMP 原文 `Music`
        //      · `MainMenu/Settings/SettingLabel/SoundFx` → TMP 原文 `Sound Effects`
        //      · `Settings/Media/VoiceOvers`              → TMP 原文 `Voice-overs`
        //    英文那一列与原来那三个写死串**逐字相同**（所以英文档零变化），中文档从「印英文」变成印中文。
        //    ⚠️ 字号**必须按语种选**（`ApplyLangFont`）—— 见下面 `SliderLabel` 里那条注释。
        const float SliderCx = -2.00f;
        static readonly float[] SliderCy = { 120.07f, 3.44f, -113.18f };
        static readonly float[] LabelBoxCy = { 178.17f, 61.57f, -55.13f };   // 原版那三颗 `Text` 的**框心**（面板内，y 向上）
        /// <summary>原版那三颗 `Text` 的**框高** —— 现读 `menu_dump.py … "Volume Sliders" --depth 3 --no-sprite`
        /// 的 `高` 列（三颗同值）。`Bottom` 那一档要它（`Label.SetVAlign` 的框高）。</summary>
        const float LabelBoxH = 55.00f;
        /// <summary>原版那三颗 `Text` 的**框宽** —— 同一份 dump 的矩形是 `677.5,332.5→1308.7,387.5`
        /// （1920×1080 左上原点）⇒ 宽 = `1308.7 − 677.5 = 631.21`（三颗同值）。
        /// 🆕 **2026-10-10（A1212 块 9）新加** —— 它是 `Label.SetAutoFitBox` 的**框宽**（`wrapPx`）那一格，
        /// 出处 = 本文件 `:107/:115` 那条注释里逐字写着的「框 631.21×55.00」（同 `LabelBoxH` 一份 dump）。
        /// ⚠️ 原来只有 `LabelBoxH`（`SetVAlign` 那一档只要高）；接自适应**必须同时给宽**
        /// （`MenuDraw.TextCore` 那条路的闸第一条就是 `wrapPx > 0`）。</summary>
        const float LabelBoxW = 631.21f;
        const float LabelLeftX = -282.54f;
        const float LabelFontPx = 42f;                       // 原版 fs42（autoSizing 10–42）
        /// <summary>原版那三颗 `Text` 的 `m_fontSizeMin` / `m_fontSizeBase`
        /// （上限那一格 = <see cref="LabelFontPx"/> 本身 —— 原版 `m_fontSizeMax = 42 = m_fontSize`）。
        /// 🆕 **2026-10-10（A1212 块 9）**：出处 = `资料/普查产出_第十会话/R6_包装层战斗与其余.md` §2·D
        /// （`SettingsPanel.cs:664/:665/:666` 那三处，原版四格 `10 / 42 / 36 / 1`）+
        /// 本文件 `:108` 那条实读注释「折行=1 · fs42 · auto[10~42] · base 36」；归组 =
        /// `资料/普查产出_第十一会话/RA1212_切块表.md` 块 9。
        /// ⚠️ `max = 42 = m_fontSize` ⇒ 自适应**只能缩、不能涨**（与 `Title` / `Button Text` 那两颗同一条口径）。
        /// <para>🔴 **2026-10-10 如实登记一条「本族特有的量纲隐患」（本处是全仓第一处「`Cap` 路 + `SetAutoFitBox`」，⛔ 没改口径、只登记）**：
        /// 本面板这三颗走 <see cref="ApplyLangFont"/> ⇒ <see cref="Label.SetScriptHeight"/> ——
        /// **汉字档走 `SetGlyphHeight`、拉丁档走 `SetCapHeight`**（`Loc.HasCjk` 那条唯一判据）。
        /// 而 `Label.SetAutoFitBox` 内部把 `maxPx` 折成 `fontSize` 时用的分母是 `NominalPx()` =
        /// `FontSizeToPx(TmpFontSize())` —— 对 `SetGlyphHeight` 路，`NominalPx == 调用方那个 px`（`42`）；
        /// 对 `SetCapHeight` 路，它量出来的是**汉字墨高口径**的值（`42 × 0.72 × (0.0948/0.0779) ≈ 36.8`）
        /// ⇒ 同一个 `maxPx = 42` 在**英文档**下写进的 `m_fontSizeMax` 会**比 `cur` 大 ≈ 1.14 倍**。
        /// 判据 = `Battle/Label.cs` 的 `NominalPx()` 那段就地订正（它自己写着「哪天有人给 Cap 那一路接上
        /// 自适应，就会静默大 1.22 倍」—— 本次就是那一档，比例按现读的两个实测值算是 1.14）。
        /// ⛔ **不在这里乘补偿系数**（那是自己发明口径）；如实记着、交调度台裁。</para></summary>
        const float LabelAutoMin = 10f, LabelAutoBase = 36f;
        /// <summary>三根音量滑块标签的**原版词条键**（顺序 = `SliderCy` / `LabelBoxCy` 的顺序）。</summary>
        static readonly string[] SliderNameTerms =
        {
            "MainMenu/Settings/SettingLabel/Music",
            "MainMenu/Settings/SettingLabel/SoundFx",
            "Settings/Media/VoiceOvers",
        };
        /// <summary>正在被拖动的那根（拖动期间不把指针当点击）。</summary>
        WfSlider _dragSlider;

        // 面板尺寸（原版 743.2×758.6 px，1080p 下 108 px/世界单位）
        const float PanelW = 743.2f, PanelH = 758.6f;
        const float ClosePx = 75f;

        /// <summary>🔴 **2026-10-19（A1301②）**：那颗叉图（原版 `BattleSettingsPanel/Generic Close Button/
        /// Close Button`）自己的 `m_SizeDelta`（**设计 px**）—— 出处
        /// `bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_3460.json`：stretch 锚
        /// `x 0.12663→0.87337` / `y 0.13672→0.86328` × 父件 `Generic Close Button` 的 75×75，
        /// 再加它自己的 `m_SizeDelta (0.364, 0.008)` ⇒ **56.37 × 54.50**
        /// （`工具/menu_dump.py bundle_scenes_scenes_battlearena1 "BattleSettingsPanel"` 的 `Close Button`
        /// 那行逐位相同）。
        /// ⚠️ 我们**画出来**的那颗叉是 `ClosePx × 0.42` = 31.5 的正方（`ImageQuad` 按贴图比例）——
        /// 比原版的框小，所以命中框**不能**拿它当基准（那是 `A964②` §四·5 那一族：实绘 ≠ 框），
        /// 一律用**原版框**这一对字面量。</summary>
        const float CloseIconRectW = 56.37f, CloseIconRectH = 54.5f;

        /// <summary>🔴 **2026-10-19（A1301②）**：同一颗的 **`m_RaycastPadding`**（分量序 **L,B,R,T**；
        /// **负值 = 外扩**）。实据 = `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_5056.json`
        /// （`m_RaycastTarget = 1`、四分量全是 **−20**）⇒ 原版命中框 **96.37 × 94.50**。
        /// <para>🔴 **圆底那颗不算**：`Generic Close Button` 自己的 `Image`（`MB_4346`）**`m_RaycastTarget = 0`**
        /// ⇒ 原版整颗钮的命中区**只由这颗叉图定**（我们那条 `_close.Contains` 是多的 —— 但它那块 75² 被
        /// 96.37×94.50 完全包住，「多的」不改变结果；`HitClose` 里两个半支都留着，见那里的注释）。</para></summary>
        const float CloseIconPadPx = -20f;

        /// <summary>本面板的**渲染队列**。这一族件原来一次都没显式设过队列 = `Sprites/Default` 的默认档
        /// （**3000**，同 `WaitBanner.BattleQChrome` / `CardDisplayWindow.QChrome`）。
        /// 🔴 走 `MenuDraw.Nine` 的地方**必须显式传**这个数 —— 那个助手会写队列，
        /// 不传就等于在战斗现场顺手换了一次排序（2026-10-04 · A50③ 收口时补的）。
        /// 🔴🔴 **2026-10-16（`T09` 批 2）**：本面板整层迁到 uGUI 子画布之后，**这个数在本件里只剩一个
        /// 调用点**（三根 `WfSlider` 的 `queue:` 形参）。
        /// 面板自己的图/字**全部不问队列**了（层级归画布的兄弟序，见 `Img` 的 doc）。
        /// <para>🔴 **2026-10-16（`T09` 块 6）**：那三根迁到画布之后，`queue` 在本件里**不再是「渲染队列」**
        /// —— 画布上没有队列这回事，它变成 `WfSlider` 内部**三层兄弟序的基准**（取负 ⇒ 轨道在最下、手柄在最上）。
        /// **值不动**（仍是 3000），只是含义随档变 —— 见 `Battle/WfSlider.cs` 文件头那条「三层先后」。</para></summary>
        const int QPanel = 3000;

        // ---- 投降钮：**原版真值**（`资料/普查产出_0918/第18行_UI三小条_规格.md` §①）----
        // 面板内（原点=面板中心，y 向上）中心 (−171.7,−310.5) px、尺寸 300×90 px = 面板左下角。
        // 出处：`assets_full/bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_3211.json`
        //       （`ap(171.7,−50)` `sd(300,90)` anchorMin=anchorMax`(0,1)` pivot`.5,.5`）
        const float ResignCxPx = -171.7f, ResignCyPx = -310.5f;
        const float ResignWPx = 300f, ResignHPx = 90f;
        /// <summary>底图 `40K_button` 的九宫格 border（贴图 px，x/y/z/w = 左/下/右/上）。
        /// 出处：同一张 Sprite 的 `m_Border`。⚠️ 左右各 234 而目标只有 300 宽 ⇒ 中间那段会被压没，
        /// 这是**原版 Sliced 在目标小于 border 时的退化行为**，不是我们挑的。</summary>
        const float ResignBorderL = 234f, ResignBorderB = 46f, ResignBorderR = 234f, ResignBorderT = 46f;
        /// <summary>原版文字字号 fs38（`MonoBehaviour_3799.json` 的 TMP `m_fontSize`）。</summary>
        const float ResignFontPx = 38f;
        /// <summary>原版按钮染色（`MonoBehaviour_4755.json` 那个 Image 的 `m_Color`）。</summary>
        static readonly Color ResignTint = new Color(0.3686f, 0.8941f, 0.5874f, 1f);
        /// <summary>投降钮那颗字的**原版词条键** —— **只此一份**（`Create` 与 `RefreshTexts` 都取它）。
        /// 判据（TMP 原文 `Resign`、挂在哪颗 `Localize` 上）→ `Core/Loc.cs` 那一块，⛔ 别在这儿抄第二份。</summary>
        public const string ResignTerm = "Battle/Settings/ResignButton";

        // ---- 🆕 2026-10-18（A991）：「跳过教程」那颗钮 —— **原版真值，逐值现读** ----
        // 判据 = `工具/menu_dump.py bundle_scenes_scenes_battlearena1 "BattleSettingsPanel" --depth 3 --no-sprite`
        //        （绝对矩形，1920×1080 左上原点）⇒ 面板绝对框 `588.4,190.0 → 1331.6,948.6`（743.20×758.63）
        //        ⇒ **框心 = (960.0, 569.3)**（与本文件 `Lang*` 那一族用的是同一个反算口径）：
        //   · `Bottom buttons`       `616.6,829.8 → 1303.4,929.8`（686.80×100 · `HorizontalLayoutGroup`）
        //     ├ `Resign Button`       `638.3,834.8 →  938.3,924.8`（300×90）
        //     └ `SkipTutorial Button` `981.7,834.8 → 1281.7,924.8`（300×90）
        //   ⇒ 面板内（原点 = 面板中心、**y 向上**）：Resign **( −171.7, −310.5)** · Skip **(+171.7, −310.5)**
        //     —— 与 `ResignCxPx/ResignCyPx` **逐值互为镜像**（两颗同尺寸 300×90，一左一右）。
        //     🔴 这一条**推翻了**原来那句「`1370/1375 > 1080` 疑似模板位 ⇒ 最终屏幕偏移仍未证出」——
        //        在**面板坐标系**里它位置清清楚楚（同族的 `Resign` 早就这么算出来了）。
        //   · 底图：`MonoBehaviour_4721.json`（那颗 uGUI `Image`）的 `m_Sprite` = **`40K_button`**
        //     （与 `Resign` **同一张**）· `m_Type = 0(Simple)` + **`m_PreserveAspect = 1`** ·
        //     `m_Color = (0.369,0.894,0.587,1)`（**同一个绿**）。
        //   · 文字：`Button Text` 框 `994.4,852.5 → 1268.4,905.8`（274×53.3）· TMP 原文 **`Skip tutorial`** ·
        //     `mTerm = Battle/Settings/SkipTutorial`（`MonoBehaviour_4307.json`）· fs **38**（base 12 ·
        //     auto 12~38）· `Center/Midline` · 折行 0 · 白 —— **与 `Resign` 那颗逐字段同形**。
        // 🔴 **它建在【本面板】里，不在 HUD 的教程覆盖层上**（2026-10-18 A991 搬过来的）：
        //    原版那颗钮的父链 = `SkipTutorial Button → Bottom buttons → **BattleSettingsPanel**`
        //    （13/13 个战场场景同构，每包恰 1 份）；我们原来建在 `TutorialOverlay` 那棵 HUD 子树上、
        //    摆位还标着「我们挑的」——**那是落点错了**（铁律 11：查出来就照原版改）。
        // ⚠️ **非教程局它也照样在**（原版那颗是 prefab 里常驻的；`BattleManager.ClickSkip` 在非教程局
        //    只 `LogError`）⇒ 我们**不加「只在教程局才建/才显示」的闸**，行为差异由 `ApplyTutorialSkip` 出声。
        const float SkipCxPx = 171.7f, SkipCyPx = -310.5f;
        const float SkipWPx = 300f, SkipHPx = 90f;
        /// <summary>那颗字的**原版词条键** —— **只此一份**（`Build` 与 `RefreshTexts` 都取它）。
        /// 判据（TMP 原文 `Skip tutorial`、`mTerm` 在哪颗 `Localize` 上）→ `Core/Loc.cs` 那一块，⛔ 别抄第二份。</summary>
        public const string SkipTutorialTerm = "Battle/Settings/SkipTutorial";

        // ---- 🆕 「Auto Zoom」那一行：**原版真值**（A424）----
        // 面板内（原点 = 面板中心，y **向上**）px。出处 = `bundle_scenes_scenes_battlearena1` 亲读：
        //   · 行 `RectTransform_3099.json`：anchorMin = anchorMax = (0.5,0.5) ·
        //     `m_AnchoredPosition = (−282.4200134277344, 249.47999572753906)` ·
        //     `m_SizeDelta = (472.4599914550781, 75.64099884033203)` · `m_Pivot = (0, 0.5)`
        //     ⇒ x ∈ [−282.42, 190.04] · y ∈ [211.66, 287.30]（中心 y = 249.48）
        //   · 勾选框 `RectTransform_2654.json`（`Toggle` 子节点）：anchor (0,1) ·
        //     ap (37.03099822998047, −37.820499420166016) · sd (74.06159973144531, 57.66559982299805) ·
        //     pivot (0.5,0.5) ⇒ **中心 = 行左上 + (37.031, −37.8205) = (−245.389, 249.4795)**
        //   · 文字 `RectTransform_3268.json`：anchor (0,1) · ap (79.0, −37.820499420166016) ·
        //     sd (229.29100036621094, 75.64099884033203) · pivot (0, 0.5) ⇒ **左中 = (−203.42, 249.4795)**
        //   · TMP `MonoBehaviour_3977.json`：`m_text = "Auto zoom"` · fs **42**（autoSizing 29–42 ·
        //     base 36）· `m_HorizontalAlignment = 1`(Left) · `m_VerticalAlignment = 512`(Middle) · 白
        //   · I2 `MonoBehaviour_5032.json`：`mTerm = "Settings/Graphics/AutoZoom"`（+ `LocalizeOnAwake`）
        //   · Toggle `MonoBehaviour_4356.json`：`m_Transition = 1`(ColorTint) ·
        //     `m_Colors.m_NormalColor = (0.2862745, 0.9647059, 0.6862745, 1)` · `toggleTransition = 1`(Fade) ·
        //     `m_IsOn = 0` · `colorTintOnValueChange = 0` / `changeSpriteOnValueChange = 0`（都不生效）
        //   · 两个图：`40K_dropdown_bg`（119×102，`m_Type=Simple` + `preserveAspect=1`）·
        //     `40K_settings_icon_checkmark`（66×51，同）
        const float AzRowCxPx = -46.19f;                 // = −282.42 + 472.46/2
        const float AzRowCyPx = 249.48f;
        const float AzRowWPx  = 472.46f, AzRowHPx = 75.641f;
        const float AzBoxCxPx = -245.389f, AzBoxCyPx = 249.4795f;
        const float AzBoxWPx  = 74.0616f,  AzBoxHPx  = 57.6656f;
        const float AzLabelLeftPx = -203.42f, AzLabelCyPx = 249.4795f;
        /// <summary>文字块宽（原版 `Label.m_SizeDelta.x`；自检用它算「文字有没有越出原版那一格」）。</summary>
        const float AzLabelWPx = 229.291f;
        /// <summary>这一行的**命中区**（面板内 px，y 向上；每项 = 中心 x · 中心 y · 宽 · 高）。
        /// = 原版那一行里**两个可被射线命中**的矩形：① 勾选框那格（`Toggle` 的 Image，`m_RaycastTarget = 1`）
        /// ② 文字那一格（TMP，`m_RaycastTarget = 1`）。原版 `Toggle` 组件挂在**行根**上、
        /// 而 uGUI 的 `ExecuteEvents.ExecuteHierarchy` 会从被点中的那个 GO 往上冒泡到它
        /// ⇒ **点这两块都算点那颗开关**。
        /// ⚠️ 两块**中间有 4.94px 的缝**（勾选框右沿 −208.359 → 文字左沿 −203.42）—— 原版那时打到的是面板自己
        /// （不触发任何东西）⇒ 我们**照原版留着缝、不补**（补了就是「原版点不到的地方我们能点到」那种偏离）。</summary>
        static readonly float[][] AzHitPx = {
            new[] { AzBoxCxPx, AzBoxCyPx, AzBoxWPx, AzBoxHPx },
            new[] { -203.42f + AzLabelWPx * 0.5f, AzLabelCyPx, AzLabelWPx, AzRowHPx },
        };
        /// <summary>原版 `m_Colors.m_NormalColor`（`Selectable` ColorTint 把 `targetGraphic`（那格 `40K_dropdown_bg`）
        /// 染成这个绿）。⚠️ 我们只做**常态**这一档：hover / pressed / selected 三档原版各有一个色，
        /// 而本面板其余按钮**一个都没有**做过悬停/按下态 ⇒ 不单独给这一行加一套（如实记）。</summary>
        static readonly Color AzBoxTint = new Color(0.2862745f, 0.9647059f, 0.6862745f, 1f);
        /// <summary>原版 TMP `m_fontSize = 42`（autoSizing 上限；`Auto zoom` 在 229.29×75.64 里放得下 ⇒ 实绘就是 42）。</summary>
        const float AzFontPx = 42f;
        /// <summary>原版 TMP 印的英文原文（**小写 z**，逐字符照抄 `MonoBehaviour_3977.json` 的 `m_text`）。</summary>
        public const string AutoZoomLabelEn = "Auto zoom";
        /// <summary>原版那一格的 I2 词条键（`MonoBehaviour_5032.json` 的 `mTerm`）。
        /// 🔴 **客户端没有本地 I2 词条表**（246,807 个文件扫中文串零命中、84 个 bundle 无本地化包，
        /// 词条表在远端 CCD）—— ⚠️ **这半句仍然成立**（说的是「**不在客户端安装目录**里」）；
        /// 🔴 **2026-10-14 更正（铁律 5）**：它后面原来接着「⇒ 正式译文**拿不到**」—— **该结论已不成立**。
        ///   原版**有**官方中文表，由**远端 Addressables 包 `localization_assets_all`**（2025-07-30 快照）下发，
        ///   **2026-10-14 已解开**并落至 `Unity/数据/游戏数据/官方本地化_en+zh-CN.json`；
        ///   **现读确认那张表里就有本键**：`Settings/Graphics/AutoZoom` = `Auto zoom` / **`自动缩放`**。
        ///   **错因** = 把「不在离线包体里」读成了「原版根本没有」。判据全文 → `CLAUDE.md` §一·7 的
        ///   「2026-10-14 更正」段（⛔ 别在这儿抄第二份）。
        ///   ⇒ **本工程【已经接了这颗键】**：`Core/Loc.cs` 登记 `Settings/Graphics/AutoZoom`，
        ///     调用点见本文件 `Loc.T(AutoZoomTermKey)`（建行那一处 + `RefreshTexts()` 那一处）与
        ///     本文件下面那段 2026-10-18 的订正 ⇒ 「只当键 + 兜底记着」是**过期描述**，
        ///     `AutoZoomLabelEn` 今天只在**英文档**（或键掉了时）兜底。</summary>
        public const string AutoZoomTermKey = "Settings/Graphics/AutoZoom";

        // ---- 🆕 2026-10-16（`T01` · **`ChatToggle`「静音对手」**，原版 `BattleSettingsPanel/Volume Sliders/ChatToggle`）----
        // 为什么补它：原版战斗内这扇窗里就有它（`T01` 的账），我们一直**没建**（`A2109`）。
        // 🔴 **下面全是【面板内】坐标**（原点 = 面板中心、**y 向上**、px；与 `Az*` / `Lang*` 同一口径）。
        //    出处 = 2026-10-16 亲读 `bundle_scenes_scenes_battlearena2`（⚠️ **不是 arena1** —— 那份的
        //    2D 全树是截断版、缺的正好在 HUD，见本线 `准则与做法.md` §六·1）：
        //      · 本行 `RectTransform_3142.json`：锚 `(0,1)`、piv `(0.5,0.5)`、`ap(350.675,−454.87)`、
        //        `sd(701.35, 54.90)`；父 `Volume Sliders`（`RT_3516`：面板内 `(−1.998, −19.891)`、701.35×441.398）
        //        ⇒ 本行中心 = 面板内 **(−1.998, −254.062)**、701.35 × 54.90。
        //        ⚠️ **本文件 `:622` 那句「原版 `ChatToggle`（−174.94）」是旧的错值**（来路没查清；
        //        `−254.06` 才是现读 —— 用 `工具/_probe_node_rect.py bundle_scenes_scenes_battlearena2 ChatToggle
        //        BattleSettingsPanel` 复核过：它沿 `m_Father` 逐跳算到面板中心）。
        //      · 子件（逐条 = `menu_rect.py … "ChatToggle" --depth 3` 的矩形之差，**相对本行中心**）：
        //        `Background`（图 `40K_toggle_off`）框心 **(+190.00, 0)**、180.76×56.80；
        //        `Background/Checkmark`（图 `Volume_button`）=(+157.14, −0.82)、57.93×46.22；
        //        `…/OffText` `'OFF'` =(+211.32, −1.32)、49.47×49.21；
        //        `Background On`（图 `40K_toggle_on`）**与 `Background` 同框同尺寸**；
        //        `Background On/Checkmark` =(+224.10, −0.82)、57.93×46.22；`…/OnText` `'ON'` =(+171.02, −1.32)、48.92×49.21；
        //        `Label 'Mute opponent'` =(−73.48, 0)、346.20×53.00。
        //        🔴 那两块拼起来**严丝合缝**：标签右沿 `−75.478+346.20/2 = 97.622` = 开关框左沿 `188.002−90.38`。
        const float MuteCxPx = -1.998f, MuteCyPx = -254.062f;
        const float MuteWPx  = 701.35f, MuteHPx = 54.90f;
        const float MuteBgOffCxPx = 190.00f, MuteBgCxPx = 190.00f;   // 两半的底图**同框**（原版两个 GO 同 rect）
        const float MuteBgWPx = 180.76f, MuteBgHPx = 56.80f;
        const float MuteChkOffCxPx = 157.14f, MuteChkOnCxPx = 224.10f;
        const float MuteChkCyPx = -0.82f, MuteChkWPx = 57.93f, MuteChkHPx = 46.22f;
        const float MuteTxtOffCxPx = 211.32f, MuteTxtOnCxPx = 171.02f, MuteTxtCyPx = -1.32f;
        const float MuteTxtOffWPx = 49.47f, MuteTxtOnWPx = 48.92f, MuteTxtHPx = 49.21f;
        const float MuteLabelLeftPx = -248.578f, MuteLabelCyPx = -254.062f;
        const float MuteLabelWPx = 346.20f, MuteLabelHPx = 53.00f;
        /// <summary>`'Mute opponent'` 那颗 TMP 的 `m_fontSize` = 42 · `autoSizing = 1` · `[18, 42]` · `base 36`
        /// （`…/MonoBehaviour_4200.json` 逐字段实读）· 对齐 `Left/Middle` · 折行 = 1 · 白。</summary>
        const float MuteFontPx = 42f, MuteAutoMin = 18f, MuteAutoBase = 36f;
        /// <summary>`'ON'` / `'OFF'` 那两颗：`m_fontSize = 30` · `autoSizing = 1` · `[1, 30]` ·
        /// 对齐 `Center/Middle`（`MonoBehaviour_4156` / `_3939` 实读）。⚠️ 原版 `m_fontSizeBase` 也是 36
        /// （三颗同一份 prefab 模板），但**上限只有 30** ⇒ 实绘 ≤ 30。</summary>
        const float MuteOnOffFontPx = 30f;
        /// <summary>这三颗的**原版词条键**（各自的 `Localize.mTerm` 原文，逐颗现读）：
        /// `Label`=`…/MonoBehaviour_5295.json` · `OnText`=`…_5163` · `OffText`=`…_5396`。
        /// ⚠️ 两列值都是**官方值**（`官方本地化_en+zh-CN.json` 逐字命中），见 `Core/Loc.cs` 那三条登记。</summary>
        public const string MuteLabelTermKey = "MainMenu/Settings/ButtonLabel/MuteChatLabel";
        public const string MuteOnTermKey    = "MainMenu/Settings/ButtonLabel/MuteChat_On";
        public const string MuteOffTermKey   = "MainMenu/Settings/ButtonLabel/MuteChat_Off";
        /// <summary>这一行的**命中区**（面板内 px，y 向上；每项 = 中心 x · 中心 y · 宽 · 高）。
        /// = 原版那一行里**两个可被射线命中**的矩形（同一套口径 = `AzHitPx` 那条）：① 开关那半（`Image`,
        /// `m_RaycastTarget = 1`）② `'Mute opponent'` 那颗 TMP（`m_RaycastTarget = 1`）——
        /// 原版 `EverguildToggle`/`Toggle` 挂在**行根**上，uGUI 的 `ExecuteEvents.ExecuteHierarchy`
        /// 会从被点中的 GO 往上冒泡到它 ⇒ **点这两块都算点那颗开关**。
        /// 🔴 **两块严丝合缝、没有缝**（同上那两行数）⇒ 等价于整行，但**照原版按两块写**
        /// （将来若查出原版某一块 `m_RaycastTarget=0`，改一处即可）。
        /// ⚠️ **`m_RaycastPadding` 没查**（原版 `Toggle`/TMP 那两格是几 —— 没读）。
        ///    本工程的口径是「可点区 = 可射线件的**并集** + 各自 padding」（`CLAUDE.md` 那条）——
        ///    这里**先用并集**（= 与 `HitAutoZoom` 同一条口径），⛔ 不许凭印象给它外扩几像素。</summary>
        static readonly float[][] MuteHitPx = {
            new[] { MuteCxPx + MuteBgCxPx, MuteCyPx, MuteBgWPx, MuteBgHPx },
            new[] { MuteCxPx + (-73.48f), MuteLabelCyPx, MuteLabelWPx, MuteLabelHPx },
        };

        // ---- 🆕 2026-10-17：「Language Selector」那一行（原版 `BattleSettingsPanel/Language Selector`，
        //      组件 `LanguageSelector`，字段 `languagesDropdown`）----
        // 为什么补它：**原版战斗内这扇设置窗里就有语言选择**（用户 2026-09-28：「请你检查游戏主界面右上角的
        // 设置按钮和**对战里的设置按钮** ……这些设置按钮里面就有语言选择」；判据 = `资料/说明书/01_战斗_对战/
        // 2D层_battlearena1全树.md:672-745`）。
        // 🔴 **下面全是【面板内】坐标**（原点 = 面板中心，**y 向上**，px）—— 与 `Az*` 那一族同一个口径。
        //    出处 = 2026-10-17 亲读 `bundle_scenes_scenes_battlearena1`
        //    （`python 工具/menu_dump.py bundle_scenes_scenes_battlearena1 "BattleSettingsPanel" --depth 4 --no-sprite`
        //     的**绝对矩形**，再用面板中心 `y = 569.3` / `x = 960` 反算回面板内）：
        //      · 行        `677.6,222.6 → 1257.3,282.0`（579.70×59.40）⇒ x ∈ [−282.4, 297.3] · y ∈ [287.3, 346.7]
        //      · 下拉框    `1007.3,222.6 → 1257.3,282.0`（250.00×59.40）⇒ x ∈ [47.3, 297.3]（右沿与行右沿齐）
        //      · 那行标签  `677.6,223.6 → 1012.6,281.1`（335.00×57.53）⇒ x ∈ [−282.4, 52.6]
        //      · 框内 `Label` / `Arrow` = 树 dump 的相对值（`Label [57,770 230x46]` / `Arrow [272,782 20x20]`，
        //        父 = 下拉框 `[47,763]`）⇒ 框内偏移 (10, 7) 与 (225, 19)。
        const float LangRowX1Px = -282.4f;                 // 那一行的左沿（= 标签的左沿）
        const float LangRowTopPx = 346.7f, LangRowBotPx = 287.3f;
        const float LangFieldX1Px = 47.3f, LangFieldX2Px = 297.3f;
        /// <summary>左边那颗标签的框（原版 `SelectLanguageText`：`335×57.53`、顶 `345.7`、底 `288.2`
        /// ⇒ 框心 y = **316.95**）—— `Label` 的左中锚要的就是框心（同 `SliderLabel` 那条口径）。</summary>
        const float LangSelCyPx = 316.95f;
        /// <summary>框内那行**当前语言名**（原版 `LanguagesDropdown > Label`）：框内偏移 (10,7)、230×46、
        /// TMP `fs 18`（`base 14`、auto 18~40）、`Left/Middle`、色 `(0.67,0.67,0.67,1)`。
        /// 🔴 **两个设置窗那一颗是同一个值**（主菜单 `General Tab` 那颗也是 fs18 + 同样那个灰）；
        /// ⚠️ 框宽 230 与「左右各内缩 10」自洽（10 + 230 + 10 = 250 = 框宽 250）⇒ **我们只落内缩 10 这一条**，
        /// 不另立一个「框宽」（`Label` 收的是「节点位 + 字号」，没有框）。</summary>
        const float LangCapInsetPx = 10f, LangCapTopPx = 7f, LangCapHPx = 46f;
        /// <summary>框内右端那个箭头：框内偏移 (225,19)、20×20、**preserveAspect**、色 `(0.0196,0.353,0.192,1)`。
        /// ⚠️ 图名 `40K_dropdown_arrow_closed` 是**我们的选择**（原版那颗 PathID `-1891211968353393973`
        /// 本地没解出名字 —— 同 `Shell/SettingsWindow.cs` 的 `ArtLangArrow` 那条）。</summary>
        const float LangArrowOffXPx = 225f, LangArrowOffYPx = 19f, LangArrowPx = 20f;
        static readonly Color LangArrowTint = new Color(0.0196f, 0.353f, 0.192f, 1f);
        /// <summary>下拉框那颗 `Image` 的 `m_Color`（原版实读；⛔ 与主菜单那扇的 `(0.286,0.965,0.686,1)`
        /// **不是同一个值** —— 两处各抄各的）。</summary>
        static readonly Color LangFieldTint = new Color(0.122f, 0.973f, 0.537f, 1f);
        /// <summary>那一行左边的标签（原版 `SelectLanguageText`）：TMP `'Select Language'` · **fs42**
        /// （base 36、auto 10~42）· `Left/Middle` · 白。词条 = `MainMenu/Settings/ButtonLabel/SelectLanguage`
        /// （与主菜单那扇**共用同一条** —— bundle 里这条 mTerm 出现 43 次，两族各挂一遍）。</summary>
        const float LangSelFontPx = 42f, LangCapFontPx = 18f;
        /// <summary>框里那行语言名的颜色（原版那颗 TMP 的 `m_Color` = `(0.67,0.67,0.67,1)` ——
        /// 主菜单那扇同一颗也是这个灰，两处各自照抄）。</summary>
        static readonly Color LangCapColor = new Color(0.67f, 0.67f, 0.67f, 1f);
        public const string LangFieldArt = "40K_dropdown_field_closed", LangArrowArt = "40K_dropdown_arrow_closed";
        /// <summary>那一行标签的词条键（= 原版 `Localize.mTerm`；⭐ 与 `Shell/SettingsWindow.cs` 的
        /// `lkSelectLang` **同一个字符串** —— 两处各是各的常量，但值必须一致）。</summary>
        public const string LangLabelTermKey = "MainMenu/Settings/ButtonLabel/SelectLanguage";

        // ---- 🆕 2026-10-18（A921）：语言下拉的【12 行列表】—— 原版 `LanguagesDropdown > Template` 子树 ----
        // 判据 = **战斗内**这棵树逐字段实读（`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/`，
        // 2026-10-18 亲读；pid 见每条）：
        //   `LanguagesDropdown`(RT **3546**) → `Template`(RT **3553**) → `Viewport`(RT **2642**) → `Content`(RT **2761**)
        //   → `Item`(RT **2589**) → { `Item Background`(3092) / `Item Checkmark`(2591) / `Item Label`(2624) }；
        //   另有 `Template > Scrollbar`(RT 2794) → `Sliding Area`(2925) → `Handle`(3127)。
        //   · `Template` RT：`aMin(0,0.5) aMax(1,0.5) ap(-2.5,-22) sd(-4.9998, 573.96) pivot(0.5,1)`
        //     ⇒ 宽 = 框宽 250 − 4.9998 = **245.0002**、高 **573.96**；**顶 = 框心 −22**（`ap.y` 是**向下**）。
        //   · `Viewport`：`aMin(0,0) aMax(1,1) sd(-17,0) pivot(0,1)` ⇒ **右沿 −17**（让给滚动条）；
        //     它挂 `Mask`（`m_ShowMaskGraphic = 0`）+ `Image`（内置 `UIMask`）⇒ **不画**、只当裁切框。
        //   · `Content`：`aMin(0,1) aMax(1,1) sd(0, 41.7226) pivot(0.5,1)`（41.7226 = 模板位一行；运行时按行数重算）。
        //     ⚠️ 本件的 `Content` 高给 `rows × 40.8707`：我们这棵树的行是自己摆的 ⇒ 这个高度**不驱动任何东西**
        //     （那 0.85 的差只落在**关着的**原型件上，⛔ 别照它去改行高）。
        //   · `Item`：`aMin(0,0.5) aMax(1,0.5) sd(0, **40.8707**)` + `Toggle`（`MB 4939`：`m_Transition = 2`(SpriteSwap) ·
        //     `toggleTransition = 1`(Fade) · `graphic` = `Item Checkmark` · `m_IsOn = 1`）。
        //   · `Item Checkmark`：`ap(10,0) sd(20,20)` ⇒ **中轴 = 行左起 10**、20×20、`m_Type = 0`(Simple，**拉伸**)。
        //   · `Item Label`：`ap(5,-0.5) sd(-30,-3)` ⇒ 行内边距 **左 20 · 右 10 · 上 2 · 下 1**；
        //     TMP（`MB 3842`）：`fs 30`（`m_fontSizeBase 14` · auto **18~40**）· `Left/Middle` ·
        //     `m_fontColor` **(0.783,0.783,0.783)**（`m_fontColor32` = `0xFFC8C8C8`，两者一致）。
        //   · `ScrollRect`（`MB 4335`）：`m_MovementType 2`(Clamped) · `m_VerticalScrollbarVisibility **2**`(AutoHide)
        //     · spacing **−3.0** · `m_ScrollSensitivity 1.0`。⚠️ **12 × 40.8707 = 490.45 < 视口 573.96 ⇒ 装得下、
        //     滚不动** ⇒ 原版那条 AutoHide 会把整根 `Scrollbar` `SetActive(false)`；本件**照办**（建出来但恒关着）。
        // 🔴 **与主菜单那扇【同构、不同宽】**：那一扇挂在 400.666 宽的框上 ⇒ 设计 **395.666**；本扇挂在 **250**
        //    宽的框上 ⇒ **245**。其余每个字段两边**逐值相同**（573.96 / 40.8707 / ap(-2.5,-22) / sd(-4.9998) /
        //    viewport −17 / Checkmark 20@x10 / Label `ap(5,-0.5) sd(-30,-3)` / fs30 / 灰 0.783）。
        //    ⛔ **别照抄 `Shell/SettingsWindow.cs` 的 `LstL/LstR`**（596.556/992.222 是**那扇窗**的绝对坐标）。
        //    ✅ 两处**唯一的数值差异** = 行底图的染色：本扇 `Item Background.m_Color = **(0, 0.8314, 0.5255)**`，
        //    主菜单那扇是 (0.2863, 0.9647, 0.6863) —— 两处各照各的原版件。
        const float LstInsetX = 2.4999f;             // `sd.x = -4.9998` 在「左右各贴一条边」的锚上 ⇒ **每边**缩 2.4999
        const float LstHPx = 573.96f;
        const float LstTopBelowFieldCyPx = 22f;      // `ap.y = -22`（顶 = **框心**往下 22）
        const float LstVpInsetR = 17f;               // `Viewport.sd.x = -17`
        const float LstItemH = 40.8707f;             // 行高（`Item.sd.y`）
        const float LstSbW = 20f;                    // `Scrollbar.sd.x`（右沿一颗 20 宽的竖条，恒关着）
        const float LstChkS = 20f, LstChkCx = 10f;   // 勾：20×20、**中轴在行左起 10**
        /// <summary>`Item Label` 的行内边距（由 `ap(5,-0.5) sd(-30,-3)` 在拉伸锚上解出来：左 20 右 10 上 2 下 1）。</summary>
        const float LstLblPadL = 20f, LstLblPadR = 10f, LstLblPadT = 2f, LstLblPadB = 1f;
        const float LstLblFontPx = 30f;              // TMP `m_fontSize`（base 14 · auto 18~40）
        static readonly Color LstLblColor = new Color(0.783f, 0.783f, 0.783f, 1f);
        /// <summary>行底图那颗 `Image.m_Color`（**本扇自己的值**，与主菜单那扇不同 —— 见上面那段）。</summary>
        static readonly Color LstItemTint = new Color(0f, 0.8314f, 0.5255f, 1f);
        /// <summary>`Template` 那颗 `Image.m_Color`（两扇同值）。</summary>
        static readonly Color LstPanelTint = new Color(0.2863f, 0.9647f, 0.6863f, 1f);
        /// <summary>列表底板 = `40k_dropdown_bg`（119×102 · 九宫 (23,20,23,20) · `m_Type = 1`(Sliced) ·
        /// **`m_PixelsPerUnitMultiplier = 1.09`**）—— 与本扇 Auto Zoom 那颗勾选框底图**同一张**
        /// （同一个 PathID `-5728790147372056906`）。端帽实画 = `23 ÷ 1.09` / `20 ÷ 1.09`
        /// （uGUI `Image.GenerateSlicedSprite` 的 `multipliedPixelsPerUnit`）；本面板**父链无缩放**
        /// ⇒ 不再乘任何根缩放（主菜单那扇要 ×0.9，见它的 `LstPanelBorderOut`）。</summary>
        const string LstPanelArt = "40k_dropdown_bg";
        /// <summary>原版那张贴图的**贴图 px 尺寸**。🔴 **2026-10-16（`T09` 批 2）起本件不再用它**
        /// （旧那套是 `MenuDraw.Nine` 切 uv 要；uGUI 的 `Sprite` 自己带尺寸）—— 留着当**原版值**的记录。</summary>
        const float LstPanelTexW = 119f, LstPanelTexH = 102f;
        const float LstPanelPpuMul = 1.09f;
        /// <summary>原版 `Sprite.m_Border`（贴图 px）。🔴 **2026-10-16（`T09` 批 2）起本件不直接传它** ——
        /// `HudCanvas.MakeSlicedPiece` 的 `pixelsPerUnitMultiplier` 恒 1 ⇒ 传 `LstPanelBorderOut`
        /// （= 除以 1.09）才能画出与旧那套**同值**的端帽（21.1 / 18.35）。留它当**原版值**的记录。</summary>
        static readonly Vector4 LstPanelBorder = new Vector4(23f, 20f, 23f, 20f);
        /// <summary>真正传给 `MakeSlicedPiece` 的那个 border（= `m_Border ÷ ppuMul`，见 `LstPanelBorder` 那条）。</summary>
        static readonly Vector4 LstPanelBorderOut =
            new Vector4(23f / LstPanelPpuMul, 20f / LstPanelPpuMul, 23f / LstPanelPpuMul, 20f / LstPanelPpuMul);
        /// <summary>语言那一行**那颗框**在面板里的 z 档（= `PanelZ − 0.01`，与 `BuildLanguageRow` 传进去的一致）。
        /// 🔴 **2026-10-16（`T09` 批 2）**：`ShowLangList` / `HideLangList` 要把这颗框在**最前**与**本档**之间
        /// 来回挪（理由见 `ShowLangList` 那段）⇒ 这个数必须**只有一份**（⛔ 别在两处各写 `− 0.01f`）。</summary>
        const float LangFieldZ = PanelZ - 0.01f;
        /// <summary>列表开着时，那把框挪到的 z 档 —— **比本面板任何一个件都靠前**
        /// （本件各件的 z ∈ [`PanelZ − 0.06`, `PanelZ + 0.02`] = [−0.51, −0.43] ⇒ 取 −0.60 留余量）。</summary>
        const float LangListFrontZ = -0.60f;
        /// <summary>行底图 —— 原版 `Item Background` 那颗 `Image.m_Sprite`：**与主菜单那扇同一个 PathID
        /// `5175970378912652380`**（= 同一张 `40K_dropdown_item`，只有 `m_Color` 两扇不同）。
        /// `Toggle.m_SpriteState` 另给三档：`_hover` / `_press` / `_selected`（同一族四个切片）。
        /// ⚠️ **`40K_dropdown_item_press` 本地 `Resources/` 里没有**（只在 `Art/原版/去重资源/` 当源图）
        /// ⇒ **按下那一档没落**（如实标注；主菜单那扇的清单里也是同一张缺）；**悬停**那一档用了 `_hover`。</summary>
        const string LstItemArt = "40K_dropdown_item", LstItemHiArt = "40K_dropdown_item_hover";
        /// <summary>勾那一层 —— 原版是 Unity **内置** `Checkmark`（同一 PathID `6419449077939965772`，两扇同一颗），
        /// 本地 `Resources/` 里没有导入 ⇒ 用**本扇自己那颗** `40K_settings_icon_checkmark`（Auto Zoom 那行的勾就是它）
        /// —— **这是我们的选择**，与 `Shell/SettingsWindow` 同一条口径。</summary>
        const string LstChkArt = "40K_settings_icon_checkmark";
        /// <summary>列表这一族的**渲染队列**（旧那套的口径）—— 本工程当时没有 uGUI 排序层
        /// （原版 `Template` 上那颗 `Canvas.overrideSorting` + `DropdownList.OnEnable` 把
        /// `sortingLayerName` 顶到 `"PopUps"`），等价物 = 渲染队列（`CLAUDE.md` §三：分层要用队列、别靠 z）。
        /// 序：本面板其余件（队列默认档 **3000**）&lt; 列表底 **3141** &lt; 行底 **3142** &lt; 行里的字/勾 **3143**。
        /// ⚠️ 主菜单那扇还有一档 `QBlocker 3140`（那颗铺满全屏的透明 `Blocker`）；本扇的「点空白收起」是
        /// **几何判定**（见 `HitLangBlank`）⇒ **不建那颗透明块**，但**序照留**（列表仍从 3141 起）。
        /// 🔴🔴 **2026-10-16（`T09` 批 2）：这三个数在本件里【已经是死值】** —— 本面板整层迁到
        /// **`PopUps` 子画布**之后，谁压谁由**兄弟序**（`HudCanvas.Add` 的 z）+ 子画布的层决定，
        /// **不再看渲染队列**（`ImageQuad.SetRenderQueue` / `Label.SetRenderQueue` 在画布上都是空操作）。
        /// ⚠️ 现在它们**只剩两个用处**：① 喂 `Img(..., q, ...)` 那个「有注释的空操作」形参（留个来历）；
        /// ② 让下面 `QLangList` 那条注释的**序**可读。⛔ 别按它们去推层级 —— 真判据是创建顺序（见 `Img`）。</summary>
        const int QLangList = 3141, QLangItem = 3142, QLangText = 3143;

        /// <summary>面板内 **px** → 本件局部坐标的**本地单位**。
        /// <para>🔴 **2026-10-16（`T09` 批 2）**：本面板整层迁到 uGUI 子画布之后，
        /// **1 局部单位 = 1 设计 px**（推导 → `HudCanvas` 文件头）⇒ 这个函数**恒等于 `px`**。
        /// 保留它（而不是把上百处调用点改写成裸数）是为了让每一处都看得出**这些数就是 px** ——
        /// ⛔ 别把它写回 `px / 108f`：那是**世界档**的换算，写回去整扇面板会缩成 **1/108**、
        /// 而画面只是「小得看不见」、**一个字都不报**（`T09` §五·补⑩ 那道硬关卡一）。</summary>
        static float U(float px) { return px; }

        /// <summary>建一个**纯分组节点**（只有 `RectTransform`、不画东西）。
        /// `cx/cy` = **面板内 px、y 向上**（相对父件的中心）；`z` = 旧那套的层深度 ⇒ 本函数据它
        /// **登记兄弟序**（`HudCanvas.Add`：z 大的排在前面 = 画在**底下**，与旧那套同向）。</summary>
        static Transform Node(Transform parent, string name, float cx, float cy, float z)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);    // 中心锚 + 居中 pivot
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.localPosition = new Vector3(U(cx), U(cy), 0f);         // z ⛔ 不进 localPosition（画布上的 z 无意义）
            HudCanvas.Add(rt, z);
            return rt;
        }

        /// <summary>建一块图（uGUI `Image`，锚点/枢轴恒在中心）：`w`×`h` 是**面板内 px**；
        /// `tint` 传 `null` = 不染。
        /// <para>🔴 `SetAspect(w/h)` —— 原版那几颗都是 `Simple`（**拉伸**，只有 `Arrow` 是 `preserveAspect`）
        /// ⇒ 按框给比值（uGUI 档的 `HudPiece.SetAspect` 就是把 `sizeDelta.x` 定成 `h × 比值`，
        /// 与旧 quad 那一侧逐字同义）。</para>
        /// <para>⚠️ `q`（旧那套的渲染队列）在 uGUI 档是**有注释的空操作** —— 层级归画布的兄弟序
        /// （同 `Label.SetRenderQueue` 的先例，⛔ 不是「悄悄没做」）。</para>
        /// <para>⚠️ 贴图取不到 ⇒ 出声、返回 `null`（⛔ 不静默建一块白图 / 也不退回世界空间 quad）。</para></summary>
        static HudPiece Img(Transform parent, string name, Texture2D tex, float cx, float cy,
                            float w, float h, float z, int q, Color? tint)
        {
            if (parent == null || tex == null)
            {
                Debug.LogWarning($"[Settings] 设置面板那张 `{name}` 的**贴图取不到** ⇒ 这一件不建"
                               + "（跑 `工具/` 那套取图脚本把图放进 `Resources/Art/`；⛔ 不摆白块）");
                return null;
            }
            // 一张 `Image (Sliced)`、border 传 0 ⇒ 与 `Simple` 拉伸**像素等价**（原版这几颗就是 `Simple`）；
            // 走这个口是为了：① 锚点/枢轴恒在中心（= 本面板的面板帧口径）② 不必给本文件加 `using UnityEngine.UI`。
            var piece = HudCanvas.MakeSlicedPiece(parent, tex, name, w, h, Vector4.zero, tint ?? Color.white);
            if (piece == null) return null;
            var rt = piece.Rect;
            rt.localPosition = new Vector3(U(cx), U(cy), 0f);
            HudCanvas.Add(rt, z);
            return piece;
        }

        /// <summary>同上面那个 `Img`，但**只给高**：宽 = `h × 贴图比例`
        /// （= `ImageQuad.Create(parent, tex, pos, worldH, anchor, name)` 的语义 ——
        /// 见 `T09` §五·补④ 第一行「**按【高】定、宽由贴图比例来**」）。
        /// `q` 取 0（uGUI 档不看队列）。</summary>
        static HudPiece Img(Transform parent, string name, Texture2D tex, float cx, float cy, float h, float z)
        {
            if (tex == null)
            {
                Debug.LogWarning($"[Settings] 设置面板那张 `{name}` 的**贴图取不到** ⇒ 这一件不建"
                               + "（跑 `工具/` 那套取图脚本把图放进 `Resources/Art/`）");
                return null;
            }
            float w = h * tex.width / Mathf.Max(1f, tex.height);
            return Img(parent, name, tex, cx, cy, w, h, z, 0, null);
        }

        /// <summary>世界坐标是不是落在**某个件自己的矩形**里（`RectTransform` 的中心锚 + 居中枢轴）。
        /// <para>🔴 **2026-10-16（`T09` 批 2）**：本体**一个字没改** —— 它本来就是纯几何，
        /// 只要求「`w/h` 与 `InverseTransformPoint` 出来的是**同一把尺**」。在本面板里那把尺 = **px**：
        /// 世界空间那档传 `U(px)` = 世界单位、uGUI 那档传 px，各自自洽。
        /// ⚠️ **2026-10-16（`T09` 块 6）**：本面板**已经没有世界空间那档了**（三根 `WfSlider` 也迁上了画布）
        /// ⇒ 现在所有调用点都走 px 那一支（`WfSlider` 自己那三条交互口不走这里，见它的 `Contains`）。</para></summary>
        static bool RectContains(Transform root, Vector3 world, float w, float h)
        {
            if (root == null) return false;
            var l = root.InverseTransformPoint(world);
            return Mathf.Abs(l.x) <= w * 0.5f && Mathf.Abs(l.y) <= h * 0.5f;
        }

        /// <summary>一张**按给定宽高比**的纯白贴图（配 `SetTint` 画任意半透明色块）。
        /// 和 `EndPanel.SolidTex` 同一个做法 —— 纯色块不该去借原版图（那些是带花纹/半透明的）。
        /// 🆕 2026-09-17：开成 `internal` —— 同目录的 `WaitBanner`（原版 `WaitText`）要画同一套
        /// 「整屏压暗 + 实底条」，**别再抄第三份**（`EndPanel` 那份是历史遗留，下次谁碰它顺手并过来）。</summary>
        internal static UnityEngine.Texture2D SolidTex(float aspect)
        {
            int h = 8, w = Mathf.Max(1, Mathf.RoundToInt(8f * aspect));
            var t = new UnityEngine.Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            t.SetPixels32(px);
            t.Apply();
            t.name = $"Solid_{w}x{h}";
            return t;
        }

        /// <summary>本面板各层的**层深度**（旧那套是世界 z：**大的在后**、先画）。
        /// 🔴 **2026-10-16（`T09` 批 2）**：uGUI 档里它**只用来登记兄弟序**（`HudCanvas.Add`，
        /// 语义同向）—— ⛔ 别再写进 `localPosition.z`（画布上的 z 无意义；写了会把件推离画布平面）。
        /// ⚠️ 它**只由本件自己决定**（本件各层的**原版 z 关系没有判据** —— `A276` 那条口径就是这么定的）。</summary>
        const float PanelZ = -0.45f;

        /// <summary>🔴 **2026-10-16（`T09` 批 2）**：本面板那块 **uGUI 子画布**的名字
        /// （= 原版宿主名 + ` Canvas`，见 `T09` §五·补⑩ 第 1 步）。⛔ **只此一份**。
        /// 🔴 **2026-10-16（修波 `FD` · 账 `F10`）就地订正（铁律 5）**：本行原来接着写
        /// **「`EnsureSubCanvas` 不会去重，所以 `Create` 里先按名字找一次（自检/探针会把面板建好几遍）」** ——
        /// **那句【已过期】**：它早已改成**按名字幂等**（复用路径还会对齐层/order + 重排次）⇒
        /// `Create` 里那个「先查再建」的包装**已删**、直接调它；自检/探针把面板建好几遍也**不会再建出
        /// 第二块同名画布**（这正是当初写那个包装要防的事，现在由 `EnsureSubCanvas` 自己保证）。
        /// 判据 = `T09c` §一 那一行：`BattleSettingsPanel` @ `…/Safe area FrontCanvas`，`PopUps` / order **0**。</summary>
        const string SubCanvasName = "BattleSettingsPanel Canvas";

        /// <summary>**面板帧** = 画布下那个 743.2×758.6 px 的节点（`BattleSettingsPanel`）——
        /// **「面板内 px」的原点就是它的中心**（y 向上），与本文件那几组 `Az*` / `Mute*` / `Lang*` 常量同口径。
        /// 🔴 命中判定凡是要「面板内 px」的，一律用**它**的局部空间（⛔ 不是 `transform` ——
        /// 那是**世界空间那层壳**，2026-10-16 块 6 之后**一个件都不装**，见文件头那段）。</summary>
        RectTransform _panel;

        /// <summary>本面板那块 **uGUI 子画布**的 `RectTransform`（`PopUps` / order 0）——
        /// 面板帧 `_panel` 与整屏压暗 `_shade` 都挂在它下面（两者是兄弟，先后由 `HudCanvas.Add` 的 z 定）。</summary>
        RectTransform _cvRt;
        /// <summary>子画布的 `RectTransform`（`_cvRt` 的只读别名；建不出来时 `null`）。</summary>
        RectTransform CvRt { get { return _cvRt; } }

        /// <summary>自检用：本面板那层 **uGUI 帧**（画布下那个 743.2×758.6 px 的节点）。
        /// 🔴 给它的理由：`transform`（= 世界空间那层壳）**不是**面板内容的父节点
        /// ⇒ 自检要**量面板帧**、以及**跨档找面板内的件**（含三根 `WfSlider` 的子件）都要走这个口。</summary>
        public RectTransform PanelRect { get { return _panel; } }

        public static SettingsPanel Create(Transform parent, Action onResign, Action onCycleDifficulty)
        {
            // 🔴 **2026-10-11（A218）**：根节点是 `RectTransform` + 写 `sizeDelta`。
            //    判据 = 原版 `BattleSettingsPanel` 实读（`bundle_scenes_scenes_battlearena1`，2026-10-11 现读）：
            //    `RectTransform` · `anchor (0.5,0.5) 重合` · `pivot (0.5,0.5)` ·
            //    **`m_SizeDelta = (743.202, 758.6345)`** · `ap (0, −29.317)` —— 与本文件头那句
            //    「面板本体 743.2 × 758.6（`BattleSettingsPanel` 自己的 rect）」同源（取整那一份）。
            //    改坏法：删掉 `SetPxSize` ⇒ `Editor/BattleScene.cs` §A218「设置面板根 = 743.2×758.6」红。
            // 🔴 **2026-10-16（`T09` 批 2）**：这一层在本批时**只装还没迁的三根 `WfSlider`**（文件头那条「例外」）
            //    ⇒ 它仍留在**世界空间**、`MenuDraw.SetPxSize`（世界单位）**原样不动**（`A218` 那条读的就是它）。
            //    面板自己的图/字**全部建在 `_panel`**（画布下那个同尺寸节点）上。
            // 🔴 **2026-10-16（`T09` 块 6）就地改**：那三根**也迁到 `_panel` 上了** ⇒ 这一层**一个件都不装**
            //    （空壳）。**`MenuDraw.SetPxSize` 那一行【原样留着】** —— `A218` 的「世界壳 = 743.2×758.6」
            //    读的就是它，⛔ 删了整个节点会把那条断言带红。⚠️ 别顺手「清理」这个壳（那是另一笔账）。
            var go = new GameObject("SettingsPanel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            MenuDraw.SetPxSize(go.transform, PanelW, PanelH);
            var p = go.AddComponent<SettingsPanel>();
            p.OnResign = onResign;
            p.OnCycleDifficulty = onCycleDifficulty;

            // ---- 子画布：`PopUps` / order 0（判据 → `T09c` §一 `SettingsPanel` 那一行）----------------
            // 🔴 **子 Canvas 一个 `CanvasScaler` 都不许加**（原版全场景只有一个、在根上 ⇒ 加了会二次缩放）。
            HudCanvas.Ensure(LayoutSpace.Cam);          // 幂等（`HudImageTex` 那一族会先建；这里兜底）
            // 🔴 **2026-10-16（修波 `FD` · 账 `F10`）**：这里原来的「先 `SubCanvas` 查、没有再建」两行包装
            //   **已删**（配套那句「`EnsureSubCanvas` 不会去重」已过期 —— 它现在按名字幂等）。
            var cvRt = HudCanvas.EnsureSubCanvas(null, SubCanvasName,
                                                 HudCanvas.SortingLayer.PopUps, 0);
            if (cvRt == null)
            {
                // ⛔ **不许静默退回世界空间 quad** —— 半迁移态（一半跟画布、一半跟世界）在 16:9 下看不出来
                //    （同 `HudCanvas.Ensure` / `Label.CreateUgui` 那两条红线）。
                UnityEngine.Object.DestroyImmediate(go);
                Debug.LogError("[Settings] `PopUps` 子画布建不起来（根 canvas 没有？）⇒ **本面板不建**");
                return null;
            }
            // 面板帧：中心锚 + 743.2×758.6 px，摆在**设计中心 (960,540)** ⇒ 局部原点 = 面板中心。
            var pgo = new GameObject("BattleSettingsPanel", typeof(RectTransform));
            pgo.transform.SetParent(cvRt, false);
            p._cvRt = cvRt;
            p._panel = (RectTransform)pgo.transform;
            HudCanvas.SetPxSize(p._panel, PanelW, PanelH);     // ⛔ 别用 `MenuDraw.SetPxSize`（那个 ÷108）
            p._panel.localPosition = HudCanvas.PosPx(HudCanvas.ReferenceResolution.x * 0.5f,
                                                     HudCanvas.ReferenceResolution.y * 0.5f);
            // 面板帧自己也要进兄弟序的账（整屏压暗 `_shade` 与它是兄弟 ⇒ 谁先画由 z 定，见 `Build`）。
            HudCanvas.Add(p._panel, PanelZ);
            p.Build();
            p.Hide();
            return p;
        }

        void Build()
        {
            // ---- 底板：原版是 `Mask`+`Background fill`（图 `40k_popup_texture`）----
            // z 都要**比棋盘/HUD 靠前**（面板盖住它们）：`PanelZ`(−0.45) 比结算面板(−0.50)靠后、比任何卡靠前。
            // 🔴 **2026-10-16（`T09` 批 2）**：uGUI 档里 z **不再进 `localPosition`** —— 它只用来
            //    **登记兄弟序**（`HudCanvas.Add`；z 大的排在前面 = 画在底下，与旧那套同向）。
            const float Z = PanelZ;
            // 挡在背后的整屏压暗 —— 原版 `BattleSettingsPanel` 里就有一个 `Dark Shade`（3963.5×2693）。
            // 🔴 **2026-10-16（`T09` 批 2）**：整屏覆盖层改成**铺满子画布**（`FillParent`）——
            //    ⛔ **别写死 `1920×1080`**：`Expand` 档下屏比画布宽时（21:9 等）画布 rect 比 1920 宽，
            //    写死的矩形**盖不满两边**（`HudCanvas.FillParent` 的 doc）。
            //    · 父 = **子画布**（⛔ 不是 `_panel` —— 那个只有 743×758，铺满它盖不住整屏）；
            //    · 贴图比例**无意义**（会被拉满）⇒ `SolidTex(1f)`；旧那套按可见宽高比开贴图是为了
            //      那条「超大 quad + 队列 4100」的土办法，它随本批整族退场。
            //    · z = `Z + 0.02`（比面板底更靠后）⇒ 兄弟序排在 `_panel` 之前 = 先画 = 压在底下 ✓。
            _shade = HudCanvas.MakeImagePiece(CvRt, SolidTex(1f), "settings_shade", 0f, 0f, 1f, 1f,
                                              new Color(0f, 0f, 0f, 0.62f));
            if (_shade != null)
            {
                HudCanvas.FillParent(_shade.Rect);
                // ⚠️ `MakeImagePiece` 内部**已经**登记过一次（`MakeImage` 的默认 z）⇒ 这里要挪档就得走
                //    `Reorder`（它会先摘掉再插）—— 用 `Add` 会在那一组的账里留下**两条**同一个件的记录。
                HudCanvas.Reorder(_shade.Rect, Z + 0.02f);
            }
            else Debug.LogWarning("[Settings] 整屏压暗（`settings_shade`）没建起来");

            // 面板底：原版是 `Mask` + `Background fill`（图 `40k_popup_texture`，128×128 的九宫格填充）。
            // ⚠️ **不能直接用那张图**（试过）：它是**半透明填充**，拉大到 743×758 之后板子几乎全透、
            //    棋盘从面板里透出来；而且我们没有九宫格，拉大边缘也糊。
            //    所以这里用**自建实底**（深色 + 不透明）+ 上面那层压暗 —— 标记成「我们的做法」。
            // 🔴 **2026-10-16（`T09` 批 2）就地改一处（铁律 5）**：旧那套是「按 8×8 的纯白贴图比例铺」
            //    ⇒ `SolidTex(PanelW/PanelH)` 取整成 **8×8**（比例 1）⇒ 实画是 **758.6×758.6**、
            //    比面板**宽 15.4 px**。uGUI 档能精确给 ⇒ 现在按面板 rect 给 **743.2×758.6** ✓。
            _bg = Img(_panel, "settings_bg", SolidTex(PanelW / PanelH), 0f, 0f, PanelW, PanelH, Z, 0,
                      new Color(0.13f, 0.13f, 0.16f, 0.98f));

            // 🔴 2026-09-19 用户口径：面板上这些**先用英文**（之后做彻底的完全翻译）。
            // ⚠️ 原版这个面板**有没有标题查不到**（规格文档 §① 里没记标题这一项）⇒ `Settings` 是**我们起的**。
            // 🔴 **2026-10-18（第十二轮 · W6）补判据（结论不变）**：本轮把「查不到」升级成**否定证据**——
            //    `battlearena1` 那棵树**逐文本节点**走了一遍（**137 个**有字或有词条的节点），
            //    `BattleSettingsPanel` 子树里**没有一个标题节点**；而且 `Battle/` 前缀的
            //    **93 条代码字面量**（`d:/2/tools/il2cpp_out/stringliteral.json`）里也**没有**标题键
            //    （`Battle/Settings/` 那一族只有 `Exit` / `ResignButton` / `SkipTutorial` 三条）。
            //    ⇒ **原版确实没有这一格**（铁律 11 例外①）⇒ 保留英文/中文自拟，⛔ 别去替它编一条原版键。
            _title = HudCanvas.MakeText(_panel, "Settings", 0f, U(PanelH * 0.5f) - U(70f),
                                        4, new Color(0.95f, 0.93f, 0.88f), new Vector2(0.5f, 0.5f),
                                        Z - 0.01f, "settings_title");

            // ---- 关闭钮：75×75，圆底 + 叉（原版 `Generic Close Button`）----
            // 🔴 **2026-10-16（`T09` 批 2）**：两张图都改走 `Img`（uGUI `Image`、中心锚、px）；
            //    ⚠️ 圆底那颗仍然**不按原版那格 75×75 的内接规则** —— 它按**贴图比例**铺（`Img` 的「只给高」
            //    那一档 = `ImageQuad.Create` 的原语义）；叉那颗按 `ClosePx × 0.42` 的高、宽由贴图比例来
            //    （`A1301②` 记着「实绘 ≠ 框」，所以命中框一律用**原版框**那两个字面量，⛔ 不拿它当基准）。
            float cx = U(PanelW * 0.5f) - U(ClosePx * 0.6f);
            float cy = U(PanelH * 0.5f) - U(ClosePx * 0.6f);
            _close = Img(_panel, "settings_close", CardArt.Ui("UI_Button_Round_background"),
                         cx, cy, U(ClosePx), Z - 0.01f);
            _closeIcon = Img(_panel, "settings_close_icon", CardArt.Ui("40k_bt_close"),
                             cx, cy, U(ClosePx * 0.42f), Z - 0.02f);

            // ---- 投降：原版 `Resign`，面板左下角 ----
            // 🔴 **2026-09-19 照原版真值重做** —— 原来这里是 (0,−U(120)) / 宽 249 / 橙字，
            //    三样都是我们挑的（旧注释写的「rect 取不到」是**错误否定**，值一直都在，
            //    见文件头那段的更正与 `资料/普查产出_0918/第18行_UI三小条_规格.md` §①）。
            var resignTex = CardArt.Ui("40K_button");
            _resignTex = resignTex;
            // 🔴 **2026-10-16（`T09` 批 2）**：原来这两行 `resignTexW/H` 只喂 `MenuDraw.Nine` 的
            //    `texW/texH`（切 uv 用）—— uGUI 那一侧由 `Sprite` 自己带尺寸 ⇒ **删掉**（`diffH` 那条
            //    算式本来就直接读 `resignTex`，有自己的 null 兜底）。
            // 🔴 **2026-10-04（A50③）：投降钮收口到公共件 `MenuDraw.Nine`** —— 原来直调
            //    `ImageQuad.CreateNineSlice`（= 绕开公共件的那条路，拿不到 `clip`）。两处**逐项等价**，别改：
            //    ① **矩形 = 投降钮的画布 px 矩形** —— 本面板挂在战斗 HUD 根下、`localPosition = 0`
            //       ⇒ 面板中心 = 屏幕中心 (960,540)，钮心相对面板中心是 (−171.7,−310.5)（y **向上**）
            //       ⇒ 画布 px 的 y 要翻：`540 − (−310.5) = 850.5`（y 向下）；
            //    ② **渲染队列 = 3000**（`QPanel`）—— 本面板原来一次都没显式设过队列（= 材质默认档 3000），
            //       而 `MenuDraw.Nine` **会写**队列 ⇒ 必须显式传同一个数，否则等于在战斗现场换一次排序。
            //    ⚠️ **落位仍按本面板的 `U(px)` 口径显式给**（下面那行）：本面板的子件一律
            //       `U(px) = px/108` 摆，而 `MenuDraw.Local` 走的是「按可见宽拉伸」的画布映射
            //       （16:9 相同、非 16:9 不同）—— 不覆盖的话这颗钮在窄/宽屏上会**跑到面板底板外面**。
            //       `MenuDraw.Nine` 的 z 恒 = `0 − 父件 z`（`RectCenter` 的 z 恒 0）⇒ z 也必须补，
            //       而且补的是**裸局部 z**（A276：与同父的 `_close`（`Z − 0.01`）· `_resignText`（`Z − 0.02`）
            //       同一套口径）。⚠️ 这是**收口径、不是对齐原版** —— 本件各层的**原版 z 关系没有判据**
            //       （`Z` 这个常量本身是我们挑的）；改前那种写法（`Z − 0.01 − transform.position.z`）
            //       今天逐位相同（`transform.position.z ≡ 0`），它只是「把世界 z 落到 Z − 0.01」的另一套口径。
            //       改坏法：写回带 `- transform.position.z` 的那版 ⇒ `Editor/BattleScene.cs` 的 A276 探针断言红。
            // 🔴 **2026-10-16（`T09` 批 2）**：整段换成 **uGUI 一张 `Image (Sliced)`**
            //    （`HudCanvas.MakeSlicedPiece` —— 原版那颗 `40K_button` 本来就是 `Image.Type = Sliced`，
            //     border 234/46 就是我们传的那个 `border`；`MakeSlicedPiece` 的 `ppuMul = 1` ⇒ **画出来的
            //     端帽 = 原版那对 border**，与旧那套 `MenuDraw.Nine` 不传 `borderOutPx` 时**逐值相同**）。
            //    · 矩形 = `ResignWPx × ResignHPx`（300×90）**px**、落位 = `(ResignCxPx, ResignCyPx)` px
            //      （相对**面板中心**）⇒ 与旧那套「按面板中心算出来的画布矩形」**逐值等价**；
            //    · `QPanel`(3000) 这个形参**不再需要**（画布上的层级归兄弟序，见 `Img` 的 doc）；
            //    · 染色 = 同一个 `ResignTint`（原来走 `TintAll` 逐块染，现在**一张**图直接给）。
            //    · `A276` 那条「裸局部 z」口径：uGUI 档没有局部 z ⇒ 那条断言**要改**
            //      （见报告；本件 `ResignLocalZ` 现在报的是**登记的兄弟序 z**）。
            _resignBtn = HudCanvas.MakeSlicedPiece(_panel, resignTex, "settings_resign",
                                                  ResignWPx, ResignHPx,
                                                  new Vector4(ResignBorderL, ResignBorderB, ResignBorderR, ResignBorderT),
                                                  ResignTint);
            if (_resignBtn != null)
            {
                _resignBtn.Rect.localPosition = new Vector3(U(ResignCxPx), U(ResignCyPx), 0f);
                HudCanvas.Add(_resignBtn.Rect, Z - 0.01f);
            }
            else Debug.LogWarning("[Settings] 投降钮（`settings_resign`）没建起来"
                                + "（`40K_button` 取不到？⇒ 本节 `HasArt` 会先红）");
            // 文字：原版 `Resign`（本地化 key `Battle/Settings/ResignButton`）。
            // 🔴 **2026-09-19 用户口径：先用英文**（原版就是英文；中文**查不到** —— 客户端没有 I2 语言表），
            //    之后再做彻底的完全翻译。字号 fs38 按**拉丁大写高度**定（见 `SliderLabel` 那条注释）。
            _resignText = HudCanvas.MakeText(_panel, Loc.T(ResignTerm), U(ResignCxPx), U(ResignCyPx),
                                             4, Color.white, new Vector2(0.5f, 0.5f), Z - 0.02f, "settings_resign_text");
            ApplyLangFont(_resignText, Loc.T(ResignTerm), ResignFontPx);

            // ---- 🆕 2026-10-18（A991）：**「跳过教程」**（原版 `SkipTutorial Button`）----
            // 与 `Resign` 同一颗 `Bottom buttons` 下的**镜像位**（+171.7, −310.5）· 同尺寸 300×90 ·
            // **同一张 `40K_button`** · 同一个绿染 · 同一档字号（fs38）—— 逐项照抄上面那一段，
            // 判据（面板内坐标 / 底图 / 染色 / 文字框 / fs）全在文件头那组 `Skip*` 常量的注释里。
            // 🔴 **行为**（原版 `BattleSettingsWindow__SkipTutorialButtonOnClick.c`，方法体亲读）：
            //    `WindowsManager.CloseWindow(<设置窗>)` **然后** `BattleManager.ClickSkip(bm, 0)`
            //    ⇒ 「**先关设置窗、再跳过**」—— 那一半在驱动层（`BattleDriver.SettingsClickAt`），
            //    本件只负责**把这颗钮摆出来 + 给命中区**（同 `HitResign` 的分工）。
            // 🔴 **2026-10-16（`T09` 批 2）**：与 `_resignBtn` **逐字同一条处置**（uGUI 一张 `Image (Sliced)`、
            //    300×90 px、落位 `(+171.7, −310.5)` px、同一个绿染、兄弟序 z = `Z − 0.01`）。
            _skipBtn = HudCanvas.MakeSlicedPiece(_panel, resignTex, "settings_skip_tutorial",
                                                SkipWPx, SkipHPx,
                                                new Vector4(ResignBorderL, ResignBorderB, ResignBorderR, ResignBorderT),
                                                ResignTint);
            if (_skipBtn != null)
            {
                _skipBtn.Rect.localPosition = new Vector3(U(SkipCxPx), U(SkipCyPx), 0f);
                HudCanvas.Add(_skipBtn.Rect, Z - 0.01f);
            }
            else Debug.LogWarning("[Settings] 「跳过教程」那颗钮（`settings_skip_tutorial`）没建起来");
            // 文字：原版 `Button Text` 的 TMP 原文就是 `Skip tutorial`（`mTerm` 见 `SkipTutorialTerm`）。
            _skipText = HudCanvas.MakeText(_panel, Loc.T(SkipTutorialTerm), U(SkipCxPx), U(SkipCyPx),
                                           4, Color.white, new Vector2(0.5f, 0.5f), Z - 0.02f, "settings_skip_tutorial_text");
            ApplyLangFont(_skipText, Loc.T(SkipTutorialTerm), ResignFontPx);

            // ---- 对手难度（🆕 2026-09-17）----
            // 用的是**现成的两样东西**：投降那颗钮同一张原版按钮图 `40K_button`（同宽 249 px，
            // 免得压扁），以及 `Label`。位置我们挑的 —— 原版**没有这个入口**
            // （那个旋钮在 `AIBotsConfig` 里、按排位/连败自动挑配置，玩家改不了；
            // 见 `资料/AI_原版反编译_0917.md` §三末）。所以这一行**是我们加的**，如实标着。
            // ⚠️ 它**不跟着投降钮改成九宫格** —— 那颗钮的形制是原版的，这一行是我们自己的，
            //    按贴图宽高比铺就够了（2026-09-19：投降钮改真值时这行原样保留）。
            float diffH = resignTex != null ? 249f * resignTex.height / resignTex.width : 100f;
            // ⚠️ **2026-09-19 从 y=+120 挪到 y=−220**：原来那格正好压在**音乐滑块**（y=120.07）上。
            //    原版没有这一行（那个旋钮在 `AIBotsConfig` 里、玩家改不了），所以**让位的是我们**。
            // 🔴 **2026-10-16（`T01`）再挪一次：−220 → −155。** 原来那句「−220 落在原版 `ChatToggle`
            //    （−174.94）与投降钮（−310.5）之间，不撞任何原版元素」**两处都不成立**：
            //    ① 原版 `ChatToggle` 的中心现读 = **−254.062**（不是 −174.94，那是旧错值）；
            //    ② 本行框高 ≈ 54.5 ⇒ y=−220 那一档占 **−247.3…−192.7**，与 `ChatToggle` 的
            //       **−281.5…−226.6** **重叠** ⇒ 我们有义务让开（本行是我们加的，原版没有它）。
            //    新档 **−155**：它在**第三根滑块标签框底边**（−55.13 − 55/2 = **−82.63**）与
            //    `ChatToggle` 顶边（**−226.6**）之间那段空带里居中（空带中心 = −154.6）。
            //    ⚠️ 这一行**仍然是我们加的**，⛔ 别拿它当原版件去对账。
            float diffY = U(-155f);
            _diffLabel = HudCanvas.MakeText(_panel, "AI Difficulty", -U(190f), diffY,
                                            4, new Color(0.95f, 0.93f, 0.88f), new Vector2(0.5f, 0.5f),
                                            Z - 0.02f, "settings_diff_label");
            _diffBtn = Img(_panel, "settings_diff_btn", resignTex, U(70f), diffY, U(diffH), Z - 0.01f);
            _diffValue = HudCanvas.MakeText(_panel, "", U(70f), diffY,
                                            4, new Color(1f, 0.86f, 0.55f), new Vector2(0.5f, 0.5f),
                                            Z - 0.02f, "settings_diff_value");

            // ---- 🆕 2026-10-12（A424）：`Auto Zoom` 那一行（原版 `Auto Zoom Toggle`）----
            // 为什么要有它：原版**战斗内这一扇**设置窗（`BattleSettingsWindow`）本来就有这一行，
            // 而「点了立刻重算」那一跳（`BattleSettingsWindow__OnAutoZoomChanged`）的家**正是它**——
            // 我们一直只有主菜单「图像」页那颗开关（= 原版 `GraphicsTab.autoZoom`，它**不**重算），
            // A175 那次是把那一跳「挂到菜单那颗上」权宜的 ⇒ A424 把这一行补回来。
            // 🔴 两条路（菜单那颗 / 战斗内这一行）**同源**：都走 `Shell.SettingsWindow` 那套 `AutoZoom.Set`
            //    + `FindFirstObjectByType<CombatAutoZoom>().ForceRefresh()`（见 `ToggleAutoZoomFromPanel`）。
            // 图：底图 `40K_dropdown_bg`（原版 `Toggle.targetGraphic`，`m_Type = Simple` + `preserveAspect = 1`）
            //     + 勾 `40K_settings_icon_checkmark`（原版 `Toggle.graphic`）。
            //     🔴 **两张都走 `CardArt.MenuUi`（= `Art/ui_menu/` 那批）**：它们本来就与菜单那批**共目录**
            //     （`MenuUi` 的注释里写明了「有些件是两边共用的：战斗里也在画」），而 `Art/ui/`（战斗那批）
            //     里没有这两张。导入器 = `工具/import_original_art.py` 的 `MENU_FROM_ART`
            //     （`40K_settings_icon_checkmark` 是 A424 新加进去的那一条；`40k_dropdown_bg` 早就在）。
            // 两张都按原版那两格的**框**内接（`ImageQuad.FitHeight` = uGUI `preserveAspect` 的语义）：
            // 框 = 74.0616 × 57.6656（原版 `Toggle` 那格的 `m_SizeDelta`）。
            var azBoxTex = CardArt.MenuUi("40k_dropdown_bg");
            float azBoxH = azBoxTex != null
                         ? ImageQuad.FitHeight(U(AzBoxWPx), U(AzBoxHPx), azBoxTex.width / (float)azBoxTex.height)
                         : U(AzBoxHPx);
            // 🔴 **2026-10-16（`T09` 批 2）**：改走 `Img`（uGUI，中心锚、px）—— `FitHeight` 那条**内接**算式
            //    **一个字没改**（它给的就是「按贴图比例内接之后的高」，px；width 由 `Img` 按贴图比例补）。
            _azBox = Img(_panel, "settings_autozoom_box", azBoxTex, U(AzBoxCxPx), U(AzBoxCyPx), azBoxH, Z - 0.01f);
            // ⚠️ 那个绿是 `Selectable` 的 `m_NormalColor`（ColorTint 常态档），**不是** Image 自己的 `m_Color`
            //    （那个是白）。`EverguildToggle.colorTintOnValueChange = 0` ⇒ 开关翻动**不**改色。
            if (_azBox != null) _azBox.SetTint(AzBoxTint);

            var azChkTex = CardArt.MenuUi("40K_settings_icon_checkmark");
            float azChkH = azChkTex != null
                         ? ImageQuad.FitHeight(U(AzBoxWPx), U(AzBoxHPx), azChkTex.width / (float)azChkTex.height)
                         : U(AzBoxHPx);
            _azCheck = Img(_panel, "settings_autozoom_check", azChkTex, U(AzBoxCxPx), U(AzBoxCyPx), azChkH, Z - 0.02f);

            // 文字：原版 TMP 印的就是英文 `"Auto zoom"`（**小写 z**，逐字符照抄）；I2 词条 = `Settings/Graphics/AutoZoom`
            //   （客户端**没有**本地 I2 词条表 ⇒ 正式译文拿不到）。
            //   ⚠️ 上面括号那句是**旧话**：🔴 **2026-10-14 更正（铁律 5）**「⇒ 正式译文拿不到」**已不成立**
            //      —— 官方 zh-CN 表已在本地、且**就有本键**；**判据与「仍然成立的那半句」在本文件
            //      `AutoZoomTermKey` 的 `<summary>` 里**（⛔ 别在这儿抄第二份）。
            // 🔴 **2026-10-18（双语③ 波 1 · P4）就地改（铁律 5）**：原来这里直接印 `AutoZoomLabelEn`
            //   ——**写死英文**⇒ 英文档对、**中文档也印英文**。现在走上面那条键（`:254` 已经记着它）：
            //   EN 列逐字照抄原版 TMP ⇒ **英文档零变化**；ZH 列「自动缩放」= `zh_CN.csv:57`（`Auto Zoom`，
            //   大写 Z ⇒ **近邻**、非精确命中，见 `Core/Loc.cs` 那条注释）。
            // ⚠️ **字号跟着「这一段文本的语种」走**（`ApplyLangFont` → `Label.SetScriptHeight` →
            //   `Loc.HasCjk` 那条唯一判据）。原来写死 `SetCapHeight(px × 0.72)`：那是**拉丁大写**那条
            //   （原版 fs42），中文照它算会**小 28%**（本文件 `SliderLabel` / `ApplyLangFont` 的
            //   doc 里早把这条判据写全了）。EN 走 `ApplyLangFont` 的取值与原来**逐字相同**
            //   （`0.72 / 108`），⇒ 英文档连尺寸都不变。
            // ⛔ 换语言时要跟着刷 ⇒ 同一条也进了 `RefreshTexts()`（本面板**自己**就能换语言）。
            string azText = Loc.T(AutoZoomTermKey);
            _azLabel = HudCanvas.MakeText(_panel, azText, U(AzLabelLeftPx), U(AzLabelCyPx),
                                          4, Color.white, new Vector2(0f, 0.5f), Z - 0.02f, "settings_autozoom_label");
            ApplyLangFont(_azLabel, azText, AzFontPx);

            // ---- 🆕 2026-10-16（`T01`）：`ChatToggle`「静音对手」那一行 ----
            // 位置 = `Volume Sliders` 下面那一行（原版父链就是这么挂的：`BattleSettingsPanel/
            // Volume Sliders/ChatToggle`），几何/字段判据全在上面那组 `Mute*` 常量的注释里。
            BuildMuteChatRow(Z);

            // ---- 三根音量滑块（原版 `BattleSettingsWindow` 的 music / soundFX / voiceOver）----
            // 每一根都按「标签在上、滑块在下」；数值一路走到 `AudioMixer.SetFloat("Volume"+组名, dB)`
            // （见 `Core/WarpforgeAudio.cs`，那里面写清了原版的 dB 公式与「只存音乐」那条）。
            // 🔴 **2026-10-07（A169）**：修 `WfSlider` 那三处硬编码时改的调用点 —— 三样现在**必传**
            //   （原来是本件内部写死的，逐实例不同的东西写死 = 真缺陷，见 `Battle/WfSlider.cs` 文件头）：
            //   · `queue: QPanel` = 本面板那一档（3000；`WfSlider` 内部按 `queue`/`+1`/`+2` 铺轨道/填条/手柄）；
            //   · `handlePx: 34.406` = 原版这九根手柄的**实画边长** = **滑区高 12 + 序列化框高 22.406**
            //     （🔴 **2026-10-07 波 8 · A197 改的就是这个数**：原来传 `WfSlider.HandlePx` = 22.406
            //       —— 那是手柄**序列化**的 `m_SizeDelta.y`、不是实画边长。判据 = uGUI
            //       `Slider.UpdateVisuals` 把手柄的 `anchorMin.y/anchorMax.y` 写成 **0 / 1**
            //       （本机 `…/com.unity.ugui/Runtime/UGUI/UI/Core/Slider.cs:616-623`）⇒ 运行时框高 =
            //       `Handle Slide Area` 高（= 滑块根 12）+ 22.406；110×110 方图 + `preserveAspect` 取短边
            //       ⇒ 实画就是 34.406。原来那版**小 35%**，而设置窗那三根现在传 35.406 × 0.9 = 31.87）；
            //   · `handleOffset: 11.99988` = 原版 `Handle.m_AnchoredPosition.x` 的字面量；
            //   · `capScale: 1` = 本面板父链**无缩放** ⇒ 设计 px = 画布 px
            //     （端帽 `184÷2 / 30÷2` ⇒ **92 / 15**；**滑区右端的让位 10** ⇒ 滑区宽 551.08）。
            //   ⛔ 别在这儿乘什么 0.9 —— 那是主菜单设置窗那三个调用点的事（那边烘 `RootScale`）。
            // 🔴🔴 **2026-10-07（波 8）：`handleOffset` 保持原版的 `11.99988`（= 12），⛔ 不要改成 17。**
            //   A169 报告附录写过「原版手柄起点 = 轨道左 + 17（`Handle Slide Area` 左右各让 5 再加 12）」——
            //   **那是算错的**：那条 RT 的 `m_AnchoredPosition.x = −4.99988` 在拉伸轴上是从**锚矩形中心**
            //   量起的 ⇒ 滑区 rect = [轨道左 **+0**, 轨道右 − 10]（左沿与轨道左沿重合，只有右边让 10）。
            //   两条独立核（本机 uGUI `DefaultControls.CreateSlider` 四个字段互相自洽 · 原版运行时 dump 里
            //   FPS 那行的滑区左沿与滑块左沿同为 266）都在 `Battle/WfSlider.cs` 的文件头里。
            //   ⇒ 手柄中心 = 轨道左 **+12** + 值 × 551.08（这就是原版），⛔ 别在这儿加 5。
            // 🔴🔴 **2026-10-16（`T09` 批 2）：这三根当时【没迁】—— 见文件头那条「例外」。**
            //   它们仍挂在**世界空间那层壳**（本组件的宿主 `transform`）上 ⇒ 位置必须走
            //   **世界单位**（`LayoutSpace.Px(px)` = `px ÷ 108`），⛔ **不能用 `U(px)`** ——
            //   那个现在是 px（面板帧那一档的换算），喂进去这三根会**偏 108 倍**（静默）。
            //   `WfSlider` 内部（`_w = trackPx/108`、`Contains`/`SetFromPointer` 的局部单位、
            //   `MenuDraw.Nine` 的九宫格）**全是世界单位** ⇒ 要迁它得先给 `WfSlider` 加 uGUI 档。
            // ══════════════════════════════════════════════════════════════════════════════════
            // 🔴🔴 **2026-10-16（`T09` 块 6 · 第二件）就地改：这三根【已迁到画布】—— 上面那段「没迁」已过期。**
            //   `WfSlider` 那一侧加了**双模式**（`Create(..., uguiHost:)`，宿主为空 = 世界档、逐位不变），
            //   这里传 `uguiHost: _panel`（= **本面板的画布帧**，见它的 doc）⇒ 三根现在是 uGUI 件。
            //   ⛔ **不新增排序层、不进 `HudCanvas.SubCanvasRank`** —— 它们是**面板内部件**，
            //      跟着面板那块 `PopUps`/0 的子画布走（落点判据 → `Battle/WfSlider.cs` 文件头那条「落点」）。
            //   · **位置改交 px**：`center` 的 x/y 在**画布档 = 设计 px**（世界档才是世界单位）⇒
            //     与 `SliderCx / SliderCy`（**面板内 px、y 向上、原点 = 面板中心**，见那两条常量的注释）
            //     **同一个数**，⛔ **不要再套 `LayoutSpace.Px(...)`**（套了会偏 108 倍、**而画面不报错**）。
            //     坐标系对得上：`_panel` 是**中心锚 + 中心枢轴**、`localPosition = (0,0)`（= 设计中心）
            //     ⇒ 它的局部原点就是面板中心 —— 与 `SliderLabel` 喂 `U(LabelLeftX)` 那一套**同一把尺**。
            //   · `center.z` 在画布档**不进 `localPosition`**，只当兄弟序档（z 大 = 画在底下）——
            //     沿用 `Z − 0.01`：面板底 `_bg` 是 `Z`(−0.45) ⇒ −0.46 比它小 ⇒ 三根画在面板**底之上** ✓。
            //   · `queue: QPanel` / `handlePx` / `handleOffset` / `capScale: 1f` **四样都不动**：
            //     画布档把 `queue` 当兄弟序基准（取负）、另外三样本来就是设计 px 口径。
            _musicSlider = WfSlider.Create(transform, "music",
                                           new Vector3(SliderCx, SliderCy[0], Z - 0.01f),
                                           WarpforgeAudio.Music, WarpforgeAudio.SetMusic,
                                           queue: QPanel, handlePx: 34.406f,
                                           handleOffset: WfSlider.HandleOffsetPx, capScale: 1f,
                                           uguiHost: _panel);
            _fxSlider = WfSlider.Create(transform, "fx",
                                        new Vector3(SliderCx, SliderCy[1], Z - 0.01f),
                                        WarpforgeAudio.SoundFx, WarpforgeAudio.SetSoundFx,
                                        queue: QPanel, handlePx: 34.406f,
                                        handleOffset: WfSlider.HandleOffsetPx, capScale: 1f,
                                        uguiHost: _panel);
            _voiceSlider = WfSlider.Create(transform, "voice",
                                           new Vector3(SliderCx, SliderCy[2], Z - 0.01f),
                                           WarpforgeAudio.VoiceOver, WarpforgeAudio.SetVoiceOver,
                                           queue: QPanel, handlePx: 34.406f,
                                           handleOffset: WfSlider.HandleOffsetPx, capScale: 1f,
                                           uguiHost: _panel);

            _musicLabel = SliderLabel(SliderNameTerms[0], 0, Z);
            _fxLabel    = SliderLabel(SliderNameTerms[1], 1, Z);
            _voiceLabel = SliderLabel(SliderNameTerms[2], 2, Z);

            // 🆕 2026-10-17：语言那一行（原版 `BattleSettingsPanel/Language Selector`）
            BuildLanguageRow(Z);
        }

        /// <summary>🆕 2026-10-17：原版战斗内这扇窗的 **`Language Selector`** 那一行 ——
        /// 下拉框（底图 + 当前语言名 + 箭头）+ 左边的 `Select Language` 标签。
        /// 几何/染色/字号**逐值见本文件那组 `Lang*` 常量**（面板内 px、y 向上）；这一段只讲落地三件：
        /// <list type="number">
        /// <item>三张图都是 uGUI `Image`（底图带绿染、箭头 `preserveAspect` 内接；🔴 **2026-10-16（`T09` 批 2）
        ///   起改走 `Img`**）—— 本面板的 `Z` 一族是**登记兄弟序**的 z（同 `_azBox`/`_azCheck`）
        ///   ⇒ 底图 `Z − 0.01`、字/箭头 `Z − 0.02`；</item>
        /// <item>文字只有两颗：框里的**当前语言名**（fs18，走 `Loc.LanguageName`）与左边那颗标签
        ///   （fs42，走 `Loc.T(词条)`）—— **字号按语种选**（`ApplyLangFont`）；</item>
        /// <item>命中区 = **下拉框那一块**（原版 `TMP_Dropdown` 挂在 `LanguagesDropdown` 上；左边那颗标签
        ///   点下去原版什么也不发生 ⇒ 不接）。判定 = `HitLanguage`，**真实输入与自检走同一条**。</item>
        /// </list>
        /// ✅ **2026-10-18（A921）订正（铁律 5）**：这里原来写「原版那颗点开是个 12 行的滚动列表 ——
        /// **本批没建**，改成『点一下换下一个』」——**现在建了**：`BuildLangTemplate` / `ShowLangList` /
        /// `ChooseLanguage` / `ESC` 四件（判据 = **战斗内**那棵子树逐字段实读，见上面那组 `Lst*` 常量；
        /// ⚠️ **原版两扇窗的这颗下拉不是同一尺寸**，本件照**战斗那扇**的 245 宽建，⛔ 别抄主菜单的 395.666）。
        /// 于是 `CycleLanguage`（点一下换下一个）**0 调用点 ⇒ 已删**（本仓规矩：死代码删）。
        /// ✅ **2026-10-18（A991）**：`Skip tutorial` 那颗钮**已经建了**（`Build` 的 `SkipCxPx` 那一段）
        /// —— 它原版就在本面板的 `Bottom buttons` 下（见文件头那组 `Skip*` 常量）。下面那行是旧口径。
        /// ⚠️ 本面板**没建**的原版件还有：`ChatToggle`（'Mute opponent'）· `Debug Buttons`
        /// （那不在这条活的范围里，别顺手加）。</summary>
        void BuildLanguageRow(float z)
        {
            float cx = (LangFieldX1Px + LangFieldX2Px) * 0.5f;
            float cy = (LangRowTopPx + LangRowBotPx) * 0.5f;
            float w = LangFieldX2Px - LangFieldX1Px, h = LangRowTopPx - LangRowBotPx;

            // 🔴 **2026-10-16（`T09` 批 2）**：底下这四件（框 / 框里的语言名 / 箭头 / 左边那颗标签）
            //    全部改走 uGUI 口（`Img` / `HudCanvas.MakeText`）—— 坐标/尺寸/染色**逐值不动**（px）。
            _langField = Img(_panel, "settings_lang_field", CardArt.MenuUi(LangFieldArt), U(cx), U(cy),
                             w, h, LangFieldZ, QPanel, LangFieldTint);   // 原版那颗 `Image` 是 `Simple`（拉伸）⇒ 按框给 (w,h)

            // 框内那行当前语言名（原版 `LanguagesDropdown > Label`）—— 左中锚，x = 框左 + 10
            _langCap = HudCanvas.MakeText(_panel, Loc.LanguageName(Loc.Current),
                                          U(LangFieldX1Px + LangCapInsetPx),
                                          U(LangRowTopPx - LangCapTopPx - LangCapHPx * 0.5f),
                                          4, LangCapColor, new Vector2(0f, 0.5f), z - 0.02f, "settings_lang_caption");
            ApplyLangFont(_langCap, Loc.LanguageName(Loc.Current), LangCapFontPx);

            // 右端那个箭头（原版 `Arrow`：20×20 + `preserveAspect` ⇒ 取短边内接）
            // 框内偏移 (225, 19) 是**到它自己的左上角** ⇒ 中心 = 框左 + 225 + 10、框顶 − 19 − 10
            var arrowTex = CardArt.MenuUi(LangArrowArt);
            float arrowH = arrowTex != null
                         ? ImageQuad.FitHeight(U(LangArrowPx), U(LangArrowPx), arrowTex.width / (float)arrowTex.height)
                         : U(LangArrowPx);
            _langArrow = Img(_panel, "settings_lang_arrow", arrowTex,
                             U(LangFieldX1Px + LangArrowOffXPx + LangArrowPx * 0.5f),
                             U(LangRowTopPx - LangArrowOffYPx - LangArrowPx * 0.5f), arrowH, z - 0.02f);
            if (_langArrow != null) _langArrow.SetTint(LangArrowTint);

            // 左边那颗标签（原版 `SelectLanguageText`，fs42 / Left/Middle / 白；框 335×57.53 ⇒ 框中 y = 316.95）
            _langSelText = HudCanvas.MakeText(_panel, Loc.T(LangLabelTermKey), U(LangRowX1Px), U(LangSelCyPx),
                                              4, Color.white, new Vector2(0f, 0.5f), z - 0.02f, "settings_lang_label");
            ApplyLangFont(_langSelText, Loc.T(LangLabelTermKey), LangSelFontPx);

            // 🆕 2026-10-18（A921）：`Template` 子树 —— 挂在**那颗框**下面（原版 `Template` 的父就是
            //   `LanguagesDropdown`：`TMP_Dropdown.Show()` 那句 `SetParent(m_Template.transform.parent, false)`
            //   给的就是它）⇒ 列表里所有坐标都是**相对框**的 px（y 向上）。
            // ⚠️ z 比本面板其余件再靠前一档（`Z − 0.03`）：列表内部那一族的排序走**渲染队列**（见 `QLangList`），
            //   这一档只是让「列表 vs 面板其余件」在 z 上也自洽（⛔ 别靠 z 代替队列）。
            // 🔴 **2026-10-16（`T09` 批 2）**：兜底父件从 `transform`（**世界空间那层壳**）改成 `_panel`
            //   —— 画布上的件**绝不能**挂到世界空间节点下（那样 `RectTransform` 链断掉、锚点也没有父矩形）。
            BuildLangTemplate(_langField != null ? _langField.transform : _panel, z - 0.03f);
        }

        /// <summary>按**这段文本的语种**定字号：汉字 ≈ **1 em**、拉丁大写 ≈ **0.72 em**
        /// （判据 = `Battle/Label.SetCapHeight` / `SetGlyphHeight` 的 doc；`Loc.HasCjk` 是那条判据的**唯一一份**）。
        /// 🔴 **为什么必须有它**：这一行现在**跟着语言变**（英文 `Select Language` / 中文 `选择语言`）——
        /// 写死 `SetCapHeight(px × 0.72)` 的话中文会**小 28%**，写死 `SetGlyphHeight(px)` 的话英文会**大 39%**
        /// （本文件 `SliderLabel` 那条注释里早写着「彻底翻译成中文时这里要跟着换」—— 就是这一处）。</summary>
        static void ApplyLangFont(Label l, string text, float px)
        {
            // 🔴 **2026-10-18（W6）收口**：语种→字号的判据**只留一份**（`Label.SetScriptHeight`，
            //    它内部接的是 `Loc.HasCjk` 那条唯一判据）。这里只是把本件的 px→本档单位换算传进去。
            // 🔴 **2026-10-16（`T09` 批 2）就地改一个数（铁律 5）**：`pxPerUnit` 从 **108 → 1** ——
            //    uGUI 档的「1 局部单位 = 1 px」（`Label.IsUgui` 那条判据），传 108 的话字号会**小 108 倍**
            //    （而画面只是「小得看不见」、不报错）。⛔ **这条只对本件成立**：世界档的调用点照旧传 108。
            if (l == null) return;
            l.SetScriptHeight(text, px, 1f);
        }

        /// <summary>把本面板**跟着语言走**的那两行字重设一遍（框里的语言名 + 左边那颗标签）。
        /// 🔴 调用点 = `ChooseLanguage`（选中某一行那一刻）+ `RefreshLangRows` 那条刷新路。⛔ 不走 `Loc` 的静态事件
        /// （同 `Shell/SettingsWindow.RefreshTexts`）。</summary>
        public void RefreshTexts()
        {
            // 🔴 **2026-10-18（W6）扩**：本面板**自己就有**语言下拉（`BuildLanguageRow`）⇒ 在同一扇窗里换语言时，
            //    跟着语言走的那几行字**必须当场重设**（否则要等下次开窗才对）。
            //    ⛔ 别把它做成静态事件广播（同 `Shell/SettingsWindow.RefreshTexts`、`Loc` 不发事件那条）。
            if (_resignText != null)
            {
                string r = Loc.T(ResignTerm);
                _resignText.SetText(r);
                ApplyLangFont(_resignText, r, ResignFontPx);
            }
            // 🆕 2026-10-18（A991）：跳过钮那颗字**也跟语言走**（词条在 `Core/Loc.cs` 那张表里）。
            if (_skipText != null)
            {
                string s = Loc.T(SkipTutorialTerm);
                _skipText.SetText(s);
                ApplyLangFont(_skipText, s, ResignFontPx);
            }
            var sliders = new[] { _musicLabel, _fxLabel, _voiceLabel };
            for (int i = 0; i < sliders.Length; i++)
            {
                if (sliders[i] == null) continue;
                string t = Loc.T(SliderNameTerms[i]);
                sliders[i].SetText(t);
                ApplyLangFont(sliders[i], t, LabelFontPx);
            }
            if (_langSelText != null)
            {
                string t = Loc.T(LangLabelTermKey);
                _langSelText.SetText(t);
                ApplyLangFont(_langSelText, t, LangSelFontPx);
            }
            if (_langCap != null)
            {
                string c = Loc.LanguageName(Loc.Current);
                _langCap.SetText(c);
                ApplyLangFont(_langCap, c, LangCapFontPx);
            }
            // 🆕 2026-10-18（双语③ 波 1 · P4）：`Auto Zoom` 那一行的字**改成跟语言走了**
            //   （`Build` 那一次只是**初值**）⇒ 在同一扇窗里换语言时必须当场重设，否则要等下次开窗。
            //   ⛔ 别做成静态事件广播（同 `Shell/SettingsWindow.RefreshTexts`、`Loc` 不发事件那条）。
            if (_azLabel != null)
            {
                string az = Loc.T(AutoZoomTermKey);
                _azLabel.SetText(az);
                ApplyLangFont(_azLabel, az, AzFontPx);
            }
            // 🆕 2026-10-16（T01）：静音那一行的三颗字（说明 + ON/OFF）同一条口径 —— 在同一扇窗里换语言时当场重设。
            if (_muteLabel != null)
            {
                string ml = Loc.T(MuteLabelTermKey);
                _muteLabel.SetText(ml);
                ApplyLangFont(_muteLabel, ml, MuteFontPx);
            }
            if (_muteOffText != null)
            {
                string ot = Loc.T(MuteOffTermKey);
                _muteOffText.SetText(ot);
                ApplyLangFont(_muteOffText, ot, MuteOnOffFontPx);
            }
            if (_muteOnText != null)
            {
                string nt = Loc.T(MuteOnTermKey);
                _muteOnText.SetText(nt);
                ApplyLangFont(_muteOnText, nt, MuteOnOffFontPx);
            }
        }

        // ==================================================================
        //  🆕 2026-10-18（A921）：语言下拉那 12 行列表（原版 `LanguagesDropdown > Template`）
        //  判据逐字段 = 上面那组 `Lst*` 常量（**战斗内**那棵树，2026-10-18 亲读）。
        //  行为判据 = 官方 `TMP_Dropdown`（本 build 用的就是它，两条旁证见
        //  `资料/普查产出_1017/R_下拉Template子树.md` §三·3）：
        //    `Show():778` → `Instantiate(Template)`、改名 `"Dropdown List"`、`SetParent(m_Template.transform.parent)`
        //    → `CreateBlocker()`；点行 ⇒ `OnSelectItem:1247`（行号 = 兄弟序）= `value = i` + **末尾 `Hide()`**；
        //    **点空白** ⇒ 那颗铺满全屏的透明 `Blocker` 的 `onClick → Hide`（`CreateBlocker:1073-1074`）；
        //    **ESC** ⇒ `OnCancel:763 → Hide()`（列表开着时那颗 `Blocker` 先吃到 `Cancel`）。
        //  —— 与主菜单那扇（`Shell/SettingsWindow` 的 `ShowLangList`/`HideLangList`/`ChooseLanguage`/`ESCPressed`）
        //     **同一套语义**，只有几何/染色按本扇自己的原版值。⛔ 别在两边各写一套行为。
        // ==================================================================

        /// <summary>列表此刻开着没有（= 原版 `TMP_Dropdown.IsExpanded`，`m_Dropdown != null`）。</summary>
        public bool LangListOpen { get { return _langList != null && _langList.gameObject.activeSelf; } }

        /// <summary>建 `Template` 子树（**一份**，`_langTemplate`）。⚠️ 它**恒 inactive**（原版出厂值）——
        /// 但**子节点照样建**：原版 `Show()` 要 `Instantiate` 它。</summary>
        void BuildLangTemplate(Transform fld, float z)
        {
            _langRowsZ = z;                              // 克隆体那一族与原型同一层（排序靠队列，见 `QLangList`）
            _langTemplate = BuildLangListSubtree(fld, "Template", z, 1);
            _langTemplate.gameObject.SetActive(false);
            Debug.Log("[Settings] 语言下拉的 `Template` 子树建好了（**关着** —— 原版出厂 `m_IsActive = 0`；"
                    + "运行时显示的那份叫 `Dropdown List`，见 `TMP_Dropdown.Show():820`）");
        }

        /// <summary>建列表的**一件** —— `Template` 原型与真正显示那份（`Dropdown List`）**共用这一份**
        /// （两处写同一条规则 = 迟早不一致）。`rows` = 铺几行（原型那件 = **1 行** = 原版 `Content` 的模板位，
        /// 文字 `'Option A'`；克隆体 = `Loc.Languages.Length`）。
        /// <para>几何（**相对那颗语言框的中心**，px、y 向上；`h` = 框高的一半 = 29.7）：
        /// 列表顶 = `h − 22`（原版 `ap.y = −22`）、高 **573.96**、宽 = 框宽 − 4.9998（左右各让 2.4999）；
        /// `Viewport` 右沿再让 **17**；`Content` 顶 = 列表顶、高 = `rows × 40.8707`；
        /// 第 `i` 行 = `Content` 顶往下 `i × 40.8707`。⚠️ 本件**不建裁切层** —— 12 行 490.45 &lt; 视口 573.96
        /// ⇒ 一行都不会被切到（原版那条 `ScrollbarVisibility = 2`(AutoHide) 也正是因为这个才把它关掉）。</para></summary>
        Transform BuildLangListSubtree(Transform parent, string name, float z, int rows)
        {
            // ⚠️ **坐标全是相对父件的**（`Node`/`Rect`/`Label` 的 `cx/cy` 都按「相对它自己的父件」给）——
            //    下面每一层都把「框中心系」里的值减去父件的中心再传下去。改层级时**必须**重算这几行。
            var list = Node(parent, name, 0f, 0f, z);                                   // = 框中心
            float halfW = (LangFieldX2Px - LangFieldX1Px) * 0.5f;                       // 125
            float halfH = (LangRowTopPx - LangRowBotPx) * 0.5f;                         // 29.7
            float w = halfW * 2f - LstInsetX * 2f;                                      // 245.0002
            float top = halfH - LstTopBelowFieldCyPx;                                   // 7.7
            float bot = top - LstHPx;
            float l = -w * 0.5f, r = w * 0.5f;
            float vpR = r - LstVpInsetR;
            float vpCx = (l + vpR) * 0.5f, vpCy = (top + bot) * 0.5f;                    // Viewport / Content 的框心（x、y）

            // 底板（原版 `Template` 自己那颗 `Image`：`40k_dropdown_bg` **Sliced** + ppuMul 1.09 + 绿染）
            // 🔴 **2026-10-16（`T09` 批 2）**：`MenuDraw.Nine`（九块世界空间 quad）→ **一张 `Image (Sliced)`**
            //    （`HudCanvas.MakeSlicedPiece`，原版本来就是这一件事）。
            //    ⚠️ **border 传 `LstPanelBorderOut`（= `m_Border ÷ ppuMul 1.09`）而不是原版那个 `m_Border`** ——
            //    因为 `MakeSlicedPiece` 的 `pixelsPerUnitMultiplier` 恒 1（它的 doc 写着「原版那颗这个字段
            //    主对话没读到 ⇒ 不猜」）⇒ 要**画出来的端帽**与旧那套（用的就是 `borderOutPx`）**同值 21.1/18.35**，
            //    就得把那个 1.09 预先折进 border。代价 = **切 uv 的那三刀落在 21.1 贴图 px 处**（原版是 23），
            //    差别 ≤2 贴图 px、且只在底板那条边上 —— 如实记着（要逐位对齐得让 `MakeSlicedPiece` 收 ppuMul，
            //    那是 `HudCanvas` 的事、不在本件白名单）。
            _langListBg = HudCanvas.MakeSlicedPiece(list, CardArt.MenuUi(LstPanelArt), "list_bg",
                                                    w, LstHPx, LstPanelBorderOut, LstPanelTint);
            if (_langListBg != null)
            {
                _langListBg.Rect.localPosition = new Vector3(0f, U(vpCy), 0f);
                HudCanvas.Add(_langListBg.Rect, z);
            }
            else Debug.LogWarning("[Settings] 语言下拉的底板（`list_bg`）没建起来"
                                + "（`40k_dropdown_bg` 取不到？）");

            // `Viewport`（原版 `Mask` + `Image(UIMask, m_ShowMaskGraphic = 0)` ⇒ **不画**、只当裁切框）
            var vp = Node(list, "Viewport", vpCx, vpCy, z);
            float ctCy = top - rows * LstItemH * 0.5f;                                   // `Content` 框心（框中心系）
            var content = Node(vp, "Content", 0f, ctCy - vpCy, z);
            // `Scrollbar`：**建出来但恒关着**（原版 `m_VerticalScrollbarVisibility = 2`(AutoHide) + 内容 490.45
            // < 视口 573.96 ⇒ `SetVerticalScrollbarVisibility` 会 `SetActive(false)`）。见上面那段。
            {
                float sbL = r - LstSbW;
                var sb = Node(list, "Scrollbar", (sbL + r) * 0.5f, vpCy, z);
                Img(sb, "scroll_bg", CardArt.MenuUi("Background"), 0f, 0f, LstSbW, LstHPx, z, QLangText, null);
                var slide = Node(sb, "Sliding Area", 0f, 0f, z);
                // `Handle`：`aMin(0,0) aMax(1,0.9273) sd(20,20)` ⇒ 高 = 滑区高 × 0.9273 + 20，贴滑区**底**
                float slideH = LstHPx - 20f, handleH = slideH * 0.9273074865341187f + 20f;
                Img(slide, "Handle", CardArt.MenuUi("40k_menu_scroll_bar_fill"), 0f,
                     (bot + 10f + handleH * 0.5f) - vpCy, LstSbW, handleH, z, QLangText, null);
                sb.gameObject.SetActive(false);
            }
            for (int i = 0; i < rows; i++)
                BuildLangItem(content, i, rows > 1, top, z, l, vpR, ctCy);
            return list;
        }

        /// <summary>列表里的一行（原版 `Content > Item`：`Toggle` + 三个子件）。
        /// 行顶 = `Content` 顶 + `i × 40.8707`（原版 `Show():840-844` 把第 0 项摆在**最上**）。
        /// ⚠️ 行里那三件的矩形由原版字段推出来，见 `LstChkCx` / `LstLblPad*` 那两条注释；
        ///    `ctCy` = `Content` 的框心（框中心系）—— 行节点要按它换算成**相对 `Content`** 的坐标。</summary>
        void BuildLangItem(Transform content, int i, bool real, float contentTop, float z, float l, float r, float ctCy)
        {
            float t = contentTop - i * LstItemH, b = t - LstItemH;
            float rowCx = (l + r) * 0.5f, rowCy = (t + b) * 0.5f;
            // ⚠️ 行节点名 = **运行时按语言拼的**（原版 `AddItem:1148`：`"Item " + 序号 + ": " + 文案`）——
            //    ⛔ 别拿它当稳定标识（换一次语言名字就变）；自检走 `LangRowBg(i)` / `LangRowWorldPos(i)`。
            var item = Node(content, real ? ("Item " + i + ": " + Loc.LanguageName(Loc.Languages[i])) : "Item",
                            0f, rowCy - ctCy, z);                 // x 恒 0：行心与 `Content` 框心同列
            var bg = Img(item, "Item Background", CardArt.MenuUi(LstItemArt), 0f, 0f,
                          r - l, LstItemH, z, QLangItem, LstItemTint);
            // 勾（原版 `Item Checkmark`：`ap(10,0)` ⇒ **中轴 = 行左起 10**、20×20、`m_Type = 0`(Simple，**拉伸**)）
            var chk = Img(item, "Item Checkmark", CardArt.MenuUi(LstChkArt), (l + LstChkCx) - rowCx, 0f,
                           LstChkS, LstChkS, z, QLangText, Color.white);
            // 字（原版 `Item Label`：行内边距 左 20 / 右 10 / 上 2 / 下 1 · `Left/Middle` · 灰 0.783）
            float bl = l + LstLblPadL, bt = t - LstLblPadT, bb = b + LstLblPadB;   // 右内边距 = `r − LstLblPadR`（只影响框宽，字是左中锚）
            string txt = real ? Loc.LanguageName(Loc.Languages[i]) : "Option A";
            var lb = HudCanvas.MakeText(item, txt, U(bl - rowCx), U((bt + bb) * 0.5f - rowCy),
                                        4, LstLblColor, new Vector2(0f, 0.5f), z, "Item Label");
            // 🔴 **队列（旧那套）**：行底图在 `QLangItem`(3142)、字在 `QLangText`(3143) —— uGUI 档这一句
            //   **是有注释的空操作**（层级归画布的兄弟序：三件都按同一个 z 登记 ⇒ 创建顺序 = 底图 → 勾 → 字 ✓，
            //   见 `Img`/`Node`/`MakeText` 里那次 `HudCanvas.Add`）。
            if (lb != null) lb.SetRenderQueue(QLangText);
            // ⚠️ 原版那颗 TMP 开了 autosize（18~40），本工程 `Label` 没有这一档 ⇒ 按 `fs 30` 定字号、
            //    **按语种选**（汉字 ≈ 1 em / 拉丁大写 ≈ 0.72 em，同 `ApplyLangFont` 那条判据）。
            //    实测最长那条（`Portuguese` ≈ 165px、`葡萄牙语` ≈ 120px）都放得进 198 宽的框（自检有断言）。
            ApplyLangFont(lb, txt, LstLblFontPx);
            if (real) _langRows.Add(new LangRow { Bg = bg, Chk = chk, Lb = lb, Idx = i });
        }

        /// <summary>**点开 / 收起**（原版 `TMP_Dropdown.OnPointerClick → Show()` / `Blocker.onClick → Hide()`）。
        /// ⚠️ 原版 `Show()` **不是 toggle**：已经开着再点框会**直接 return**（`m_Dropdown != null`）；
        /// 而框上方那一块此刻被 `Blocker` 盖住 ⇒ 真实交互里「再点一下框 = 收起」。
        /// 我们没有 UGUI 那套射线优先级，所以这里显式写成 toggle —— **这一处是对齐行为、不是照抄实现**
        /// （同 `Shell/SettingsWindow.ToggleLangList` 那条）。</summary>
        public void ToggleLangList() { if (LangListOpen) HideLangList(); else ShowLangList(); }

        /// <summary>开列表（= 原版 `Show()`）：首开时**建克隆体**（`Instantiate(Template)` + 改名 `"Dropdown List"`
        /// + 挂到 `Template` 的父下），然后 `SetActive(true)`。
        /// ⚠️ 原版还会 `AlphaFadeList(0.15, 0→1)` 淡入 + 建那颗全屏 `Blocker`：
        /// **淡入不做**（本工程的 `ImageQuad`/`Label` 没有 alpha 动画口，批处理下也没有帧循环）；
        /// **`Blocker` 不建**（原版那颗是 `Color.clear` 的**透明**块，作用只是「吃掉落到别处的点击」
        /// —— 本件的命中是**几何判定**（`HitLangRow` / `HitLangBlank`），不吃射线 ⇒ 建一块看不见的图没有意义）。</summary>
        public void ShowLangList()
        {
            if (_langField == null) return;
            if (_langList == null)
            {
                // 原版：`m_Dropdown = CreateDropdownList(m_Template.gameObject)` → `name = "Dropdown List"`
                //   → `dropdownRectTransform.SetParent(m_Template.transform.parent, false)`
                _langList = BuildLangListSubtree(_langField.transform, "Dropdown List",
                                                 _langRowsZ, Loc.Languages.Length);
            }
            _langList.gameObject.SetActive(true);
            // 🔴 **2026-10-16（`T09` 批 2）**：把**那颗框**（连同列表子树）挪到面板子件的**最前面** ——
            //    这是旧那套 `QLangList/QLangItem/QLangText`（3141/3142/3143）三档队列的**等价物**：
            //    那三档的全部作用就是「把整棵列表压在面板其余件之上」（见 `QLangList` 的 doc）。
            //    迁到画布后队列没了、先后**只由兄弟序**定，而列表挂在**那颗框下面**（`Show()` 的父链 +
            //    自检那条 `LangListNode.parent == LangFieldNode` 都要求如此）⇒ 光靠创建顺序，
            //    列表会被**排在框后面的件**盖住 —— 实测几何：列表底边伸到面板 y = −249.3，
            //    而静音行占 y ∈ [−281.5, −226.6] ⇒ **重叠 22.7 px**（不给这一句就是「被静音行压掉一条」）。
            //    ⚠️ 那颗框本身跟着挪到最前**无害**：它与任何同层件都不重叠（y ≈ 317、x ∈ [47.3, 297.3]）。
            //    ⚠️ **与原版的差别（如实记）**：原版那颗 `Dropdown List` 是**自己的 canvas**
            //       （`Canvas.overrideSorting` + `sortingLayerName = "PopUps"`）—— 要逐位对齐得给它建一层
            //       子画布；本件**先用兄弟序顶**（判据与两条可选修法都写在报告里）。
            HudCanvas.Reorder(_langField.Rect, LangListFrontZ);
            RefreshLangRows();
            _langHoverRow = -1;
            Debug.Log($"[Settings] 语言下拉**点开**（{_langRows.Count} 行 —— 原版 `TMP_Dropdown.Show()`："
                    + "`Instantiate(Template)` → `\"Dropdown List\"` → `SetActive(true)` → 建全屏 `Blocker`）");
        }

        /// <summary>收起（= 原版 `Hide()` + `DestroyBlocker`）。⚠️ **不销毁克隆体**（原版会淡出后 `Destroy`）——
        /// 我们不做淡出 ⇒ 留着复用，不影响任何一个可观察值（它关着、也不在命中里）。</summary>
        public void HideLangList()
        {
            bool was = LangListOpen;
            if (_langList != null) _langList.gameObject.SetActive(false);
            // 🔴 **2026-10-16（`T09` 批 2）**：把那颗框挪回**它本来的那一档**（开列表时挪到了最前，
            //    见 `ShowLangList` 那段）。幂等 —— 没开过列表时也挪回原位（值本来就相同）。
            if (_langField != null) HudCanvas.Reorder(_langField.Rect, LangFieldZ);
            _langRowArmed = -1; _langBlankArmed = false; _langHoverRow = -1;
            if (was) Debug.Log("[Settings] 语言下拉**收起**（原版 `Hide()` → `AlphaFadeList(0.15, 0)` → "
                             + "`DelayedDestroyDropdownList(0.15)` → `DestroyBlocker`；本件不做淡出）");
        }

        /// <summary>点某一行 ⇒ 选中（= 原版 `OnSelectItem:1247`：按行号定 `value` → **末尾 `Hide()`**）。
        /// ⚠️ 原版那条链的「行号 → 语言」映射 = `LanguageSelector.ResetLanguagesDropdown` 遍历的那个静态
        /// `string[]` 的**下标** —— 我们的等价物 = `Loc.Languages` 的**声明序**（`Core/Loc.cs` 已按枚举取值核过）。</summary>
        public void ChooseLanguage(int i)
        {
            if (i < 0 || i >= Loc.Languages.Length) { Debug.LogWarning($"[Settings] 语言下拉：行号 {i} 越界"); return; }
            HideLangList();
            var v = Loc.Languages[i];
            if (Loc.Current == v) { Debug.Log($"[Settings] 语言已经是「{Loc.LanguageName(v)}」—— 不变"); return; }
            bool changed = Loc.SetLanguage(v);
            RefreshTexts();
            Debug.Log("[Settings] 语言 → " + Loc.LanguageName(Loc.Current) + $"（{Loc.Current}）"
                    + (Loc.HasOwnText(Loc.Current) ? "" : "　⚠️ 本地没有这一套文案 ⇒ 界面文字**回退英文**（见 `Loc.T`）")
                    + (changed ? "" : "（值没变）"));
        }

        /// <summary>把 12 行的**勾**那一层按当前语言重挑（= 原版 `Toggle.isOn = (value == i)` →
        /// `graphic`（那层勾）的 alpha；本件用 `SetActive`，见 `AutoZoom` 那条同族说明）。
        /// 同时把行底图**复位成常态那张**（原版 `Show()` 每次都重设 `item.toggle.isOn`）。</summary>
        void RefreshLangRows()
        {
            for (int i = 0; i < _langRows.Count; i++)
            {
                var row = _langRows[i];
                if (row.Chk != null) row.Chk.gameObject.SetActive(Loc.Languages[row.Idx] == Loc.Current);
                if (row.Lb != null)
                {
                    string t = Loc.LanguageName(Loc.Languages[row.Idx]);
                    row.Lb.SetText(t);
                    ApplyLangFont(row.Lb, t, LstLblFontPx);
                }
                // 🔴 **2026-10-16（`T09` 批 2）**：`HudPiece.SetTexture` 只有**一个**形参 ——
                //   旧那套的 `keepAspect: true`（「比例由行框定，别让新图的自身比例覆盖它」）在 uGUI 那一侧
                //   **天然成立**（换 `Sprite` 不动 `sizeDelta`）⇒ 那个实参退场。
                if (row.Bg != null) row.Bg.SetTexture(CardArt.MenuUi(LstItemArt));
            }
            _langHoverRow = -1;
        }

        /// <summary>指针落在**第几行**上（`-1` = 不在任何一行上）。判据 = 那一行的底图那块**矩形**
        /// （原版那一行整块都可点：`Toggle.targetGraphic` = `Item Background` 铺满整行）。
        /// 🔴 **2026-10-16（`T09` 批 2）**：`Bg.Contains`（走 `GetWorldCorners`）→ **`RectContains`**
        /// （同 `HitResign`/`HitLanguage` 那一套写法）；几何仍是那颗行底图自己的矩形 ⇒ 判定结果不变。</summary>
        public bool HitLangRow(Vector3 world, out int idx)
        {
            idx = -1;
            if (!Visible || !LangListOpen) return false;
            for (int i = 0; i < _langRows.Count; i++)
            {
                var bg = _langRows[i].Bg;
                if (bg == null) continue;
                var s = HudCanvas.SizePx(bg.Rect);
                if (RectContains(bg.transform, world, s.x, s.y)) { idx = _langRows[i].Idx; return true; }
            }
            return false;
        }

        /// <summary>列表开着时，指针落在**空白处**（不在任何一行上）—— 原版那颗全屏 `Blocker` 覆盖的范围。
        /// ⚠️ 它**也包括那颗框自己**（`Blocker` 在框之上）⇒ 「再点一下框 = 收起」这条行为就是它给的。</summary>
        bool HitLangBlank(Vector3 world)
        {
            if (!LangListOpen) return false;
            int i;
            return !HitLangRow(world, out i);
        }

        /// <summary>ESC —— **列表开着先收列表**（原版 `TMP_Dropdown.OnCancel:763 → Hide()`：UGUI 把 `Cancel`
        /// 事件按「当前选中对象 → 父链」派发，列表开着时那颗全屏 `Blocker`（内部类 `DropdownBlocker`）先吃到它）。
        /// 返回 `true` = 这一下**被用掉了**（⛔ 窗不关、也不做别的事）—— 与主菜单那扇
        /// （`Shell/SettingsWindow.ESCPressed`：抢在 `base` 之前判一次、返回 true 就不再往下走）**同一条口径**。
        /// <para>🔴 **接线**：本件每帧都被驱动层调一次 `PointerFrame`（`BattleDriver.HandleSettings` 里那句
        /// `_settingsPanel.PointerFrame(...)`，面板开着时恒走）⇒ **ESC 就在那个每帧入口读一次**。
        /// ⛔ 别把这句挪进「按下/抬起」那两支 —— 它是**帧级**边沿、与指针无关。
        /// ⚠️ 本件不在 `BattleDriver.cs` 的白名单里（那份文件另有写手）⇒ **没有**在驱动层加一行。</para></summary>
        public bool EscPressed()
        {
            if (!Visible) return false;
            if (LangListOpen)
            {
                HideLangList();
                Debug.Log("[Settings] ESC：**先收语言下拉列表**（原版 `TMP_Dropdown.OnCancel → Hide()`）—— 窗不关");
                return true;
            }
            return false;    // 列表没开 ⇒ 本件不吃（原版那一路归 `GameWindow.ESCPressed`；本件不是 `GameWindow`）
        }

        static bool EscDown()
        {
            if (EscapePressedForTest.HasValue) return EscapePressedForTest.Value;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
        }

        /// <summary>悬停那一档（原版 `Item` 那颗 `Toggle` 的 `m_Transition = 2`(SpriteSwap) 的
        /// `m_SpriteState.m_HighlightedSprite` = `40K_dropdown_item_hover`）。
        /// ⚠️ **按下那一档没落**：`40K_dropdown_item_press` 本地 `Resources/` 里没有（见 `LstItemArt` 那条）。
        /// 只在**换行**时才动贴图（`HudPiece.SetTexture` 在 uGUI 档**不动 `sizeDelta`** ⇒ 比例仍由行框定 ✓）。</summary>
        void RefreshLangHover(Vector3 world)
        {
            if (!LangListOpen) { _langHoverRow = -1; return; }
            int i;
            HitLangRow(world, out i);
            if (i == _langHoverRow) return;
            _langHoverRow = i;
            for (int k = 0; k < _langRows.Count; k++)
            {
                var row = _langRows[k];
                if (row.Bg == null) continue;
                bool hot = row.Idx == i;
                row.Bg.SetTexture(CardArt.MenuUi(hot ? LstItemHiArt : LstItemArt));
            }
        }
        int _langHoverRow = -1;
        /// <summary>列表底板的九宫格根（自检量「建出来了没有」；⛔ 只读）。
        /// 🔴 **2026-10-16（`T09` 批 2）**：类型从 `GameObject` 换成 `HudPiece` —— 现在是一张
        /// `Image (Sliced)`（`HudCanvas.MakeSlicedPiece`），自检口 `LangListBgNode` 仍返回 `GameObject`。</summary>
        HudPiece _langListBg;
        /// <summary>克隆体那一族的 z（与原型同一层；🔴 **2026-10-16（`T09` 批 2）**：uGUI 档的先后由
        /// **登记的兄弟序**定（`HudCanvas.Add` 的 z，见 `QLangList` 那条注释），这个 z 只是跟着面板走）。</summary>
        float _langRowsZ;

        // ---- 自检口（⛔ 只读，不给生产用）----
        /// <summary>`Template` 原型节点（**恒 inactive**）。</summary>
        public Transform LangTemplateNode { get { return _langTemplate; } }
        /// <summary>真正显示的那份（`"Dropdown List"`）；没开过 = `null`。</summary>
        public Transform LangListNode { get { return _langList; } }
        /// <summary>列表的父（= 那颗语言框）—— 自检核「列表挂在哪儿」用。</summary>
        public Transform LangFieldNode { get { return _langField != null ? _langField.transform : null; } }
        /// <summary>列表底板的九宫格根（自检量它的实画矩形）。</summary>
        public GameObject LangListBgNode { get { return _langListBg != null ? _langListBg.gameObject : null; } }
        public int LangRowCount { get { return _langRows.Count; } }
        /// <summary>🔴 **2026-10-16（`T09` 批 2）**：返回类型 `ImageQuad` → **`HudPiece`**
        /// （这一族现在是 uGUI 件）。自检那侧用的是 `var` + `.transform.position` / `.WorldW` ⇒ **零改动**
        /// （`HudPiece` 那两个口都有，且 `WorldW` 两种模式的语义一致 —— 见 `HudPiece` 文件头）。</summary>
        public HudPiece LangRowBg(int i) { return (i >= 0 && i < _langRows.Count) ? _langRows[i].Bg : null; }
        public Label LangRowLabel(int i) { return (i >= 0 && i < _langRows.Count) ? _langRows[i].Lb : null; }
        public HudPiece LangRowCheck(int i) { return (i >= 0 && i < _langRows.Count) ? _langRows[i].Chk : null; }
        /// <summary>🆕 **2026-10-17（`A1212` 块 9 的断言口）**：那三颗音量滑条标签
        /// （0=`Music` · 1=`Sound Effects` · 2=`Voice-overs`）。**只给自检读**。
        /// <para>⚠️ 为什么不按节点名找：画布档下它们挂在**共用子画布**（`BattleSettingsPanel Canvas`），
        /// 面板被建过好几次时同名件会有多份 ⇒ 按名找会拿到已销毁/别人的那一颗。</para></summary>
        public Label SliderLabelForTest(int i)
        {
            switch (i)
            {
                case 0: return _musicLabel;
                case 1: return _fxLabel;
                case 2: return _voiceLabel;
            }
            return null;
        }
        /// <summary>第 `i` 行**底图**的世界中心（自检照着它点 —— 走与真实输入**同一条**判定）。
        /// 🔴 为什么给这个口：行节点名是**运行时按语言拼的**（换一次语言就变）⇒ `transform.Find(名字)` 不可靠。</summary>
        public Vector3 LangRowWorldPos(int i)
        {
            var bg = LangRowBg(i);
            return bg != null ? bg.transform.position : Vector3.zero;
        }
        /// <summary>第 `i` 行的矩形（**相对那颗语言框的中心**，px、y 向上）—— 自检量版面用。
        /// ⚠️ 给的是**行**（= 原版 `Item` 那格），不是卡位那一族的东西。
        /// 🔴 必须走 `InverseTransformPoint`：行是挂在 `Content > Item` 下面的（`localPosition` 那一层
        /// 只有一个「相对行心」的偏移，读它永远得 0）。</summary>
        public bool LangRowRectPx(int i, out float cx, out float cy, out float w, out float h)
        {
            cx = cy = w = h = 0f;
            var bg = LangRowBg(i);
            if (bg == null || _langField == null) return false;
            // 🔴 **2026-10-16（`T09` 批 2）**：两个因子都改了，但**返回值仍是 px**（自检那几条期望值不动）：
            //   ① `InverseTransformPoint` 出来的**已经是 px**（uGUI 档的局部单位 = 设计 px）⇒ 那个 `× 108` 退场；
            //   ② 尺寸改走 `HudCanvas.SizePx`（= `sizeDelta`，**成对的那个逆**）而不是 `WorldW`
            //      —— 后者是**世界尺寸**（非 16:9 下 ×108 已经不是 px）。
            var local = _langField.transform.InverseTransformPoint(bg.transform.position);
            cx = local.x; cy = local.y;
            var s = HudCanvas.SizePx(bg.Rect);
            w = s.x; h = s.y;
            return true;
        }

        // ==================================================================
        //  🆕 2026-10-16（`T01`）：`ChatToggle`「静音对手」那一行
        // ==================================================================

        /// <summary>建那一行（几何 / 图 / 三颗字 / 词条 —— 判据全在上面那组 `Mute*` 常量）。
        ///
        /// <para>🔴 **为什么两半的底图都要建**（而不是「一张图 + 换 sprite」）：原版就是**两个 GO**
        /// （`Background` / `Background On`，同 rect、同尺寸），`OnMuteChatChanged` 只对 **ON 那半**
        /// 做 `SetActive(v)`；OFF 那半**恒 active**。ON 那块不透明、正好整块盖住 OFF 那半的
        /// 底图 + 勾 + `OFF` 字（两块同 rect ⇒ 这就是原版「选中态」的做法）。
        /// ⛔ 别改成「一张图换 sprite」：那会丢掉原版两个 GO 的可见性语义（自检也量不到）。</para>
        ///
        /// <para>⚠️ **颜色 = 白**，⛔ 别照 `offColor` 那格把它染灰：那颗 `EverguildToggle` 的
        /// `colorTintOnValueChange = 0` ⇒ 那条 on/off 染色的路**原版恒不走**（`EverguildToggle.RefreshVisuals`
        /// 里那段被 `+0x158` 那道闸挡着）—— 序列化里那个 `offColor (0.75,0.75,0.75,1)` 是**死值**。
        /// （这正是本线 `准则与做法.md` §三·3「一个值 ≠ 全部情况」那一类：字段有值 ≠ 它会生效。）</para></summary>
        void BuildMuteChatRow(float z)
        {
            // 取图口 = **三级兜底** `ui/ → ui_menu/ → ui_deck/`（先例 = 同族 `Shell/AlliancePanelWindow.cs`
            //   的 `Art()`；`MenuUi` 内部已含 `ui_menu/ → ui_deck/ → ui/`）。
            // 🔴 这两张 `40K_toggle_*` 属**去重资源**批次，只导在 `Resources/Art/ui_deck/`；而 `CardArt.Ui`
            //   只查 `Art/ui/`（`Core/CardArt.cs:424-428`）⇒ 裸 `Ui` 恒 null ⇒ 两块底图根本不建
            //   （日志 `[Settings]…贴图取不到 ⇒ 这一件不建`）。`Volume_button` 在 `ui/` 下、裸 `Ui` 取得到，不动。
            var offTex = CardArt.Ui("40K_toggle_off") ?? CardArt.MenuUi("40K_toggle_off") ?? CardArt.DeckUi("40K_toggle_off");
            var onTex  = CardArt.Ui("40K_toggle_on")  ?? CardArt.MenuUi("40K_toggle_on")  ?? CardArt.DeckUi("40K_toggle_on");
            var chkTex = CardArt.Ui("Volume_button");

            // 两块底图（原版 `Image` + `preserveAspect = 1` ⇒ 按框内接；图 301×120、框 180.76×56.80）
            // 🔴 **2026-10-16（`T09` 批 2）**：这一行四张图 + 三颗字全部改走 uGUI 口（`Img` / `MakeText`）
            //    —— `FitHeight` 那两条**内接**算式、坐标、尺寸、z **逐值不动**（px）。
            float bgH = offTex != null
                      ? ImageQuad.FitHeight(U(MuteBgWPx), U(MuteBgHPx), offTex.width / (float)offTex.height)
                      : U(MuteBgHPx);
            _muteOffBg = Img(_panel, "settings_mute_bg_off", offTex,
                             U(MuteCxPx + MuteBgOffCxPx), U(MuteCyPx), bgH, z - 0.05f);
            float bgHOn = onTex != null
                        ? ImageQuad.FitHeight(U(MuteBgWPx), U(MuteBgHPx), onTex.width / (float)onTex.height)
                        : U(MuteBgHPx);
            _muteOnBg = Img(_panel, "settings_mute_bg_on", onTex,
                            U(MuteCxPx + MuteBgCxPx), U(MuteCyPx), bgHOn, z - 0.06f);

            // 两颗勾（原版 `Checkmark`，图 `Volume_button` 110×110 方图 + `preserveAspect` ⇒ 取短边内接）
            float chkH = chkTex != null
                       ? ImageQuad.FitHeight(U(MuteChkWPx), U(MuteChkHPx), chkTex.width / (float)chkTex.height)
                       : U(MuteChkHPx);
            _muteOffChk = Img(_panel, "settings_mute_check_off", chkTex,
                              U(MuteCxPx + MuteChkOffCxPx), U(MuteCyPx + MuteChkCyPx), chkH, z - 0.051f);
            _muteOnChk = Img(_panel, "settings_mute_check_on", chkTex,
                             U(MuteCxPx + MuteChkOnCxPx), U(MuteCyPx + MuteChkCyPx), chkH, z - 0.061f);

            // 两颗字：原版 `OffText` / `OnText`（fs30、auto[1,30]、base 36、居中、词条见上面那两条键）
            string offTxt = Loc.T(MuteOffTermKey), onTxt = Loc.T(MuteOnTermKey);
            _muteOffText = HudCanvas.MakeText(_panel, offTxt,
                                              U(MuteCxPx + MuteTxtOffCxPx), U(MuteCyPx + MuteTxtCyPx),
                                              4, Color.white, new Vector2(0.5f, 0.5f), z - 0.052f,
                                              "settings_mute_text_off");
            ApplyLangFont(_muteOffText, offTxt, MuteOnOffFontPx);
            if (_muteOffText != null)
                _muteOffText.SetAutoFitBox(U(MuteTxtOffWPx), U(MuteTxtHPx), 1f, MuteOnOffFontPx, MuteAutoBase);
            _muteOnText = HudCanvas.MakeText(_panel, onTxt,
                                             U(MuteCxPx + MuteTxtOnCxPx), U(MuteCyPx + MuteTxtCyPx),
                                             4, Color.white, new Vector2(0.5f, 0.5f), z - 0.062f,
                                             "settings_mute_text_on");
            ApplyLangFont(_muteOnText, onTxt, MuteOnOffFontPx);
            if (_muteOnText != null)
                _muteOnText.SetAutoFitBox(U(MuteTxtOnWPx), U(MuteTxtHPx), 1f, MuteOnOffFontPx, MuteAutoBase);

            // 左边那句：原版 `Label`（TMP `"Mute opponent"`、fs42、auto[18,42]、base 36、Left/**Middle**、白）
            string lab = Loc.T(MuteLabelTermKey);
            _muteLabel = HudCanvas.MakeText(_panel, lab, U(MuteLabelLeftPx), U(MuteLabelCyPx),
                                            4, Color.white, new Vector2(0f, 0.5f), z - 0.05f,
                                            "settings_mute_label");
            // 字号按语种选（`ApplyLangFont` → `Label.SetScriptHeight` → `Loc.HasCjk` 那条唯一判据），
            // ⚠️ 顺序照 `SliderLabel`：**先**定字体高度、**再** `SetAutoFitBox`（后者会重排 mesh）。
            ApplyLangFont(_muteLabel, lab, MuteFontPx);
            // ⚠️ 这一处**同时命中**本文件 `LabelAutoBase` 那段登记的量纲隐患（`Cap` 路下
            //    `SetAutoFitBox` 的 `maxPx` 会比 `cur` 大 ≈1.14 倍）—— ✅ **本轮登记、不补偿**
            //    （判据与理由在那段注释里，⛔ 别在这儿乘系数）。
            if (_muteLabel != null)
                _muteLabel.SetAutoFitBox(U(MuteLabelWPx), U(MuteLabelHPx), MuteAutoMin, MuteFontPx, MuteAutoBase);

            RefreshMuteChat();
        }

        /// <summary>按当前状态刷**可见性**（原版 `BattleSettingsWindow.OnMuteChatChanged` 中间那一句
        /// `muteChatToggleOn.SetActive(v)`，以及 `Open()` 里的回填）。
        /// ⚠️ **ON 那半只看 `_muteChatOn`、OFF 那半恒显示**（原版就是这两个 GO 的默认状态）。</summary>
        void RefreshMuteChat()
        {
            bool on = Visible && _muteChatOn;
            if (_muteOnBg != null) _muteOnBg.gameObject.SetActive(on);
            if (_muteOnChk != null) _muteOnChk.gameObject.SetActive(on);
            if (_muteOnText != null) _muteOnText.gameObject.SetActive(on);
        }

        /// <summary>这一下点在「静音对手」那一行上吗。判据 = 上面 `MuteHitPx` 那两块**原版矩形**，
        /// ⛔ 不是「整行框」。真实输入与自检走的是**同一条**判定（自检拿 `MuteChatWorldPos()` 喂进来）。
        /// 🔴 **2026-10-16（`T09` 批 2）**：`transform`（世界空间那层壳）→ **`_panel`**（面板帧）——
        /// 只有面板帧的局部单位是 **px**，而那组 `MuteHitPx` 常量就是面板内 px。到命中的那条判定**没变**。</summary>
        public bool HitMuteChat(Vector3 world)
        {
            if (!Visible || _panel == null) return false;
            var l = _panel.InverseTransformPoint(world);         // 面板局部（= px、y 向上）
            foreach (var r in MuteHitPx)
                if (Mathf.Abs(l.x - U(r[0])) <= U(r[2]) * 0.5f && Mathf.Abs(l.y - U(r[1])) <= U(r[3]) * 0.5f)
                    return true;
            return false;
        }

        /// <summary>点那颗开关（原版 `BattleSettingsWindow.OnMuteChatChanged` 整支的等价物）：
        /// ① 翻 `isOn` ② **只 `SetActive` ON 那半** ③ 写「对手聊天静音」那一位。
        ///
        /// <para>🔴 **如实声明：我们这边点了没有可见效果**（除了开关自己的滑块动一下）。判据（2026-10-16 实核）：
        /// 它那一位挡的是**一条链** —— 对手在 `ChatPopup` 选的那 6 句 → `PUNReceiveChatMessage`（`isPlayer=0`）
        /// → 被拦 ⇒ **气泡 + 语音 + 「对手在聊天」提示三样一起没**；而 `CardPresentation/Net/*.cs`（8 个文件）
        /// 里 `chat` **0 命中** ⇒ **我们没有那条链**。⛔ 别把敌方单位语音条绑上来（原版不受它影响 ——
        /// 三条判据见 `资料/普查产出_1015战斗判据/T01b_三处判据缺口.md`）。将来做联机对手聊天时**必须**进这个开关。</para></summary>
        public void ToggleMuteChatFromPanel()
        {
            if (!Visible) return;
            SetMuteChat(!_muteChatOn);
        }

        /// <summary>把开关设成 `v`（原版 `Open()` 用 `chatMuted` 回填那一条）。</summary>
        public void SetMuteChat(bool v)
        {
            _muteChatOn = v;
            RefreshMuteChat();
            Debug.Log($"[Battle] 「静音对手」→ {(v ? "ON" : "OFF")}（原版 `BattleSettingsWindow.OnMuteChatChanged`："
                    + "`isOn = v` + `muteChatToggleOn.SetActive(v)` + `voiceLinesController.chatMuted = v`）。"
                    + "⚠️ **我们这边没有可见效果** —— 那条链（对手聊天）我们还没做，见 `ToggleMuteChatFromPanel` 的 doc。");
        }

        /// <summary>自检用：开关当前状态（原版那颗 `Toggle.isOn`）。</summary>
        public bool MuteChatOn { get { return _muteChatOn; } }
        /// <summary>自检用：**ON 那半**（原版 `muteChatToggleOn` 那个 GO）现在画不画。
        /// 🔴 这是「面板可见 + 开关为真」两个条件的合取 —— 报 `false` **不一定**是没开。</summary>
        public bool MuteChatOnHalfVisible { get { return _muteOnBg != null && _muteOnBg.gameObject.activeSelf; } }
        /// <summary>自检用：OFF 那半（原版 `Background`）现在画不画（面板可见时**恒真**）。</summary>
        public bool MuteChatOffHalfVisible { get { return _muteOffBg != null && _muteOffBg.gameObject.activeSelf; } }
        /// <summary>自检照着它点 —— 走 `HitMuteChat` **同一条**判定，⛔ 不是自己另算一个坐标。</summary>
        public Vector3 MuteChatWorldPos()
        {
            return _muteOffBg != null ? _muteOffBg.transform.position : Vector3.zero;
        }
        /// <summary>自检量版面用：这一行的矩形（**相对面板中心**，px、y 向上）。
        /// ⚠️ 量的是 **ON 那半的底图**（两块同 rect ⇒ 等价），走 `InverseTransformPoint`（同 `LangRowRectPx`）。
        /// 🔴 **2026-10-16（`T09` 批 2）**：两个因子都改了、**返回值仍是 px**（自检那几条期望值不动）：
        /// ① 参考帧 `transform` → **`_panel`**（只有它是 px）；② 尺寸走 `HudCanvas.SizePx`（成对的那个逆）。</summary>
        public bool MuteChatRectPx(out float cx, out float cy, out float w, out float h)
        {
            cx = cy = w = h = 0f;
            var b = _muteOnBg != null ? _muteOnBg : _muteOffBg;
            if (b == null || _panel == null) return false;
            var local = _panel.InverseTransformPoint(b.transform.position);
            cx = local.x; cy = local.y;
            var s = HudCanvas.SizePx(b.Rect);
            w = s.x; h = s.y;
            return true;
        }

        /// <summary>滑块标签：左对齐（锚点 `(0, 0.5)` = **文字块的左缘 + 行盒心**落在给定坐标上）。
        /// 🔴 **2026-10-16（A712 阶段 2）：节点从「块底」挪到「原版那一颗的框心」，锚点 `(0,0)` → `(0,0.5)`。**
        ///    理由 = `Label.SetVAlign` 那条模型的**前提是「节点位 = 原版那一颗的**框心**」**（见它的 doc 与
        ///    `WA712` §三 —— 那条 doc 原来把本处列为「不在这个前提里」的两处之一）。
        ///    `Bottom` 这一档要的也是「框心 − 框高/2」⇒ 不给定框心就没法精确落档。
        ///    ⚠️ 锚点换档**只动纵向**（横向还是「文字块左缘 = 节点 x」，`anchor.x` 仍是 0）。</summary>
        Label SliderLabel(string term, int i, float z)
        {
            string text = Loc.T(term);                                  // 原版词条（见 `SliderNameTerms`）
            // 🔴 **2026-10-16（`T09` 批 2）**：这两颗标签**在画布上**（⇒ uGUI 档、单位 = px）。
            //    ⚠️ **2026-10-16（`T09` 块 6）**：这里原来接着写「它们与那三根滑块是**两族**：滑块留在世界空间」
            //    —— **那句已过期**：三根滑块现在**同挂 `_panel`**（`uguiHost: _panel`）⇒ 标签与滑块
            //    **同一把尺（px）、同一个父节点**（先后由 `HudCanvas.Add` 的 z 定）。
            var l = HudCanvas.MakeText(_panel, text, U(LabelLeftX), U(LabelBoxCy[i]),
                                       4, Color.white, new Vector2(0f, 0.5f), z - 0.01f,
                                       "settings_slider_label_" + i);
            // 🔴 **字号按语种选**（唯一一份实现 = `ApplyLangFont`）：fs42 是 TMP 的 font size，
            //    拉丁大写只占约 **0.72 em**、汉字约占 **1 em** —— 写死一种就会让另一档差 28%/39%。
            //    （这两条标签自 2026-10-18 起**跟着语言变**，所以这里不能再写死 `SetCapHeight(0.72em)`。）
            ApplyLangFont(l, text, LabelFontPx);
            // 🆕 **2026-10-10（A1212 块 9）接上自适应**：原版那三颗 `Text` 四格 = `10 / 42 / 36 / 1`
            //   （出处 = `R6_包装层战斗与其余.md` §2·D 的 `:664/:665/:666` 三处 + 本文件 `:108` 那条实读；
            //   归组 = `RA1212_切块表.md` 块 9）。**走 `Label.SetAutoFitBox` 那条路 ⇒ 没有那三道闸。**
            //   · 量纲（`Label.SetAutoFitBox(worldW, worldH, minPx, maxPx, basePx)` 的 doc 写着）：
            //     **框宽/框高走世界单位** ⇒ 过 `U()`（本文件的 px→世界单位换算，= `/108`）；
            //     **`min/max/base` 走原版画布 px**（画布 1920×1080，与我们同一把尺）⇒ **原样传、⛔ 不乘任何系数**。
            //   · ⚠️ **必须在 `SetVAlign` 之前调** —— `SetWrapWidth` / `SetAutoFitBox` 都会重排 mesh，
            //     而 `Bottom` 那一档要按框高重算（`Label.SetVAlign` 的 doc 那句「排在重排之后才稳」）。
            //   · ⚠️ 折行 = **1** ⇒ 不用还原（`SetAutoFitBox` 内部顺带开的就是 `Normal(1)`）。
            if (l != null) l.SetAutoFitBox(U(LabelBoxW), U(LabelBoxH), LabelAutoMin, LabelFontPx, LabelAutoBase);
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向档 = 原版那三颗 `Text` 的 **`VAlign Bottom`**
            //   （判据 = 本文件 `:48` 引的那条实读「标签文字 Music / Sound Effects / Voice-overs，
            //    fs42 白，左对齐 / **VAlign Bottom**」+ `menu_dump.py` 现读的 `对齐=Left/Bottom`）。
            //   `Bottom` 吃框高 ⇒ 传原版那一颗的框高（55.00px，见 `LabelBoxH`）。
            //   ⚠️ **这一落是可见的**：旧版面（档 `Middle` + 全局字墨校正 + 那个假盒底）的**墨心**落在
            //   *旧假设的*框心**上 6.55px**，而原版 `Bottom` 的真目标是框心**下 5.39px**（= −55/2 +
            //   (20+60/2)/95×42）⇒ 按同一个盒算**下移 11.94px**；再叠旧 `LabelBottomY` 那 1.00px 的偏高，
            //   合计 **≈12.94px** —— 这正是 A712 要修的那一类（同 W16 那处 `Bottom` 的 11px 量级）。
            //   ⚠️ 文案恒单行（`Music` / `Sound Effects` / `Voice-overs`）+ `Label` 恒 `NoWrap`
            //   ⇒ 不踩 W16 报告 §五·1 那条「多行 + `Top`/`Bottom`」的坑（原版那三颗 `折行=1`，
            //      但本工程的 `Label` 不折行，两者在**这几个串**上等价）。
            if (l != null) l.SetVAlign(Label.VAlign.Bottom, U(LabelBoxH));
            return l;
        }

        // ==================================================================
        //  滑块交互 —— **真实输入与自检走同一条判定**
        // ==================================================================

        /// <summary>指针这一帧的状态喂进来。`down` = 按住。返回「**滑块接住了这一下**」
        /// （驱动层据此决定要不要再把它当点击转给按钮）。
        /// ⚠️ 「Auto Zoom」那一行**不在这里**（A445 起它走抬起的点击链），见方法体里的注释。</summary>
        public bool PointerFrame(Vector3 world, bool down)
        {
            if (!Visible)
            {
                _dragSlider = null; _wasDown = false; _langArmed = false;
                _langRowArmed = -1; _langBlankArmed = false; _langHoverRow = -1;
                return false;
            }
            // 🆕 2026-10-18（A921）：**ESC —— 先收列表、窗不关**（原版 `TMP_Dropdown.OnCancel → Hide()`，
            //   见 `EscPressed` 那段）。🔴 为什么在这条路上读键盘：本件**每帧**都被驱动层调一次
            //   （`BattleDriver.HandleSettings` 里那句 `_settingsPanel.PointerFrame(...)`），而
            //   `BattleDriver.cs` 不在本件的白名单里 ⇒ 这里是本件唯一「每帧必到」的入口。
            //   ⛔ 别把这句挪进下面那两支（它是**帧级**边沿、与指针无关）。
            if (EscDown()) EscPressed();
            RefreshLangHover(world);
            // 🆕 2026-10-17：语言那一行的**按下 → 抬起**这一对边沿（原版那颗 `TMP_Dropdown` 是 `Selectable`
            //   ⇒ `IPointerClickHandler`，**抬起**那一帧才算点它）。⚠️ 这一段必须在下面那句
            //   `if (!down) … return false;` **之前** —— 抬起那一帧 `down == false`，走不到底下。
            bool langClick = LangPointerFrame(world, down);
            if (!down) { _dragSlider = null; return langClick; }

            // 🆕 2026-10-18（A921）：**列表开着** ⇒ 这一帧的按下归那颗全屏 `Blocker`
            //   （`LangPointerFrame` 已经返回 true）—— ⛔ 别再往下走：原版那颗 `Blocker` 盖住整根滑块，
            //   按在列表上不该把底下的滑轨拖走。
            if (LangListOpen) return true;

            // 🔴 **2026-10-12（A445）：「Auto Zoom」那一行不在本方法里判。**
            //    原版那颗开关（`BattleSettingsPanel/Auto Zoom Toggle`，组件 `EverguildToggle`）继承
            //    `Toggle`/`Selectable` ⇒ 它走 `IPointerClickHandler`，**抬起**那一帧才触发。
            //    所以它的指针入口与 `HitResign/HitDifficulty/HitClose` 同一条链 =
            //    **`BattleDriver.SettingsClickAt`**（那里现在有那一行）。
            //    ⚠️ A424 当初把它落在「按下那一帧」是**文件所有权逼出来的权宜**（那时 `BattleDriver.cs`
            //    不在那件活的白名单里）—— 现在挪回去了，保留这段痕迹免得下一个会话再挪回来。
            //    本方法仍然只负责**滑块**：拖动是持续状态，必须在**按下**那一帧接住。
            if (_dragSlider != null) { _dragSlider.SetFromPointer(world); return true; }

            foreach (var s in Sliders)
                if (s != null && s.Contains(world))
                {
                    _dragSlider = s;
                    s.SetFromPointer(world);       // **按下即定位**（🔴 **照原版** —— `Slider.OnPointerDown` 的
                                                   // else 支就是 `UpdateDrag`；原来这里写「`m_TargetGraphic` 只认手柄、
                                                   // 这条是我们挑的」= 读错了对象，订正见 `Battle/WfSlider.cs` 文件头）
                    return true;
                }
            return false;
        }

        WfSlider[] Sliders { get { return new[] { _musicSlider, _fxSlider, _voiceSlider }; } }

        /// <summary>三根滑块（自检用）。</summary>
        public WfSlider SliderAt(int i) { return Sliders[i]; }
        public int SliderCount { get { return 3; } }

        /// <summary>三根滑块的标签文字（自检用 —— 空标签 = 玩家看到一根没有说明的条）。</summary>
        public string SliderLabelAt(int i)
        {
            var ls = new[] { _musicLabel, _fxLabel, _voiceLabel };
            return ls[i] != null ? ls[i].Text : null;
        }

        /// <summary>难度显示名。🔴 **2026-09-19 改回英文** —— 用的就是**原版的枚举名**
        /// `SuperEasy / Easy / Normal / Hard`（原版没有玩家可见的难度开关，所以没有「原版文案」可抄；
        /// 枚举名是原版的）。之前那套中文是我们起的。（彻底的完全翻译留到后面统一做。）</summary>
        public static string DifficultyName(AiDifficulty d)
        {
            switch (d)
            {
                case AiDifficulty.SuperEasy: return "SuperEasy";
                case AiDifficulty.Easy: return "Easy";
                case AiDifficulty.Hard: return "Hard";
                default: return "Normal";
            }
        }

        /// <summary>按下一档（很简单 → 简单 → 普通 → 困难 → 很简单）。</summary>
        public static AiDifficulty NextDifficulty(AiDifficulty d)
        {
            switch (d)
            {
                case AiDifficulty.SuperEasy: return AiDifficulty.Easy;
                case AiDifficulty.Easy: return AiDifficulty.Normal;
                case AiDifficulty.Normal: return AiDifficulty.Hard;
                default: return AiDifficulty.SuperEasy;
            }
        }

        /// <summary>把当前难度写到按钮上（驱动层改完 `aiDifficulty` 就调它）。</summary>
        public void SetDifficulty(AiDifficulty d)
        {
            if (_diffValue != null) _diffValue.SetText(DifficultyName(d));
        }

        // ==================================================================
        //  🆕 2026-10-12（A424）：「Auto Zoom」那一行的三条链
        // ==================================================================

        /// <summary>点那一行**做的那件事** —— 与主菜单「图像」页那颗开关（`Shell/SettingsWindow.ToggleAutoZoom`）
        /// **逐字同一条链**，别在面板里另写一套：
        /// <list type="number">
        /// <item>写值：`AutoZoom.Set(!AutoZoom.Enabled)`（= 原版 `GraphicsTab.AutoZoomClick(bool)`
        /// ⇒ 同时写 `GameStaticData.useCombatAutoZoom`(+0x125) 与 `autoCombatChosenManually`(+0x12f)）；</item>
        /// <item>立刻重算：`FindFirstObjectByType&lt;CombatAutoZoom&gt;().ForceRefresh()` ——
        /// 取法与 `BattleSettingsWindow__OnAutoZoomChanged.c` **逐字相同**（`Object.FindObjectOfType&lt;T&gt;()`
        /// → `CombatAutoZoom.ResetCameraZoomUIAction()`）。**战斗内这一颗才是那条路的家**，
        /// 主菜单那颗原版**不**重算（A175 把这一跳挂过去只是权宜）。</item>
        /// </list>
        /// <para>⚠️ 场上没有那个组件（不在战斗里）⇒ 什么都不用做：**值已经写下了**，下一局开局就读得到
        /// （原版那台组件住在战场场景里）。**不许静默** —— 这条日志说清是哪种情况。</para></summary>
        public void ToggleAutoZoomFromPanel()
        {
            AutoZoom.Set(!AutoZoom.Enabled);
            var zoom = UnityEngine.Object.FindFirstObjectByType<CombatAutoZoom>();
            if (zoom != null) zoom.ForceRefresh();
            RefreshAutoZoomCheck();
            Debug.Log("[Settings] Auto Zoom → " + (AutoZoom.Enabled ? "开" : "关")
                    + "（战斗内那一行：原版 `BattleSettingsWindow__OnAutoZoomChanged` —— 写 "
                    + "`GameStaticData.useCombatAutoZoom`(+0x125) 后 `FindObjectOfType<CombatAutoZoom>()"
                    + ".ResetCameraZoomUIAction()`）"
                    + (zoom != null ? "：本局那个组件在，**已经当场重算**" : "：本局没有那个组件 ⇒ 值已存下"));
        }

        /// <summary>勾那一层的显隐 —— **两件事合起来**：面板开着（`Visible`）**且**开关是开的。
        /// 原版 `Toggle.graphic` 由 `Toggle.UpdateVisuals` 按 `isOn` 控制（`toggleTransition = 1` = 淡入淡出），
        /// ⚠️ 我们**用 `SetActive` 而不是淡入淡出**（本面板没有补间系统，如实记）。</summary>
        void RefreshAutoZoomCheck()
        {
            if (_azCheck != null) _azCheck.gameObject.SetActive(Visible && AutoZoom.Enabled);
        }

        /// <summary>这一下点在「Auto Zoom」那一行上吗。判据 = 上面那两块**原版矩形**（见 <see cref="AzHitPx"/>），
        /// ⛔ 不是「行框整个」。真实输入与自检走的是同一条判定（自检拿 `AutoZoomRowWorldPos` 喂进来）。
        /// 🔴 **2026-10-16（`T09` 批 2）**：参考帧 `transform` → **`_panel`**（只有它是 px，见 `HitMuteChat` 那条）。</summary>
        public bool HitAutoZoom(Vector3 world)
        {
            if (!Visible || _panel == null) return false;
            var l = _panel.InverseTransformPoint(world);         // 面板局部（= px、y 向上）
            foreach (var r in AzHitPx)
                if (Mathf.Abs(l.x - U(r[0])) <= U(r[2]) * 0.5f && Mathf.Abs(l.y - U(r[1])) <= U(r[3]) * 0.5f)
                    return true;
            return false;
        }

        // ==================================================================
        //  🆕 2026-10-17：语言那一行的命中与边沿
        // ==================================================================

        /// <summary>这一下点在**语言下拉框**上吗（原版那颗 `TMP_Dropdown` 挂在 `LanguagesDropdown` 上，
        /// 左边那颗 `SelectLanguageText` 点下去什么也不发生 ⇒ 只认框那一块）。
        /// ⚠️ 判定与命中区**同一份几何**（那颗框自己的矩形）——
        /// 与 `HitDifficulty` / `HitResign` 同一套写法（`RectContains`，⛔ 别在这儿另写一份手算矩形）。</summary>
        public bool HitLanguage(Vector3 world)
        {
            if (!Visible || _langField == null) return false;
            // 🔴 **2026-10-16（`T09` 批 2）**：`_langField.Contains`（走 `GetWorldCorners`）→ **`RectContains`**
            //    —— 与 `HitResign`/`HitDifficulty` **同一套写法**（那条注释要求的正是这一条），
            //    几何仍是那颗框自己的矩形（= 原版那颗 `Image` 的 250×59.4）⇒ 判定结果不变。
            var s = HudCanvas.SizePx(_langField.Rect);
            return RectContains(_langField.transform, world, s.x, s.y);
        }

        /// <summary>语言那一行的**按下 / 抬起**边沿 —— 返回「这一帧这一次抬起算不算点到了它」。
        ///
        /// <para>🔴 **为什么在这一层收边沿，而不是并进 `BattleDriver.SettingsClickAt`**：
        /// 本面板其余四颗（Resign / Difficulty / Auto Zoom / Close）的入口**都在驱动层**那条链上
        /// （`BattleDriver.SettingsClickAt`，A462/A445 把它们统一到「抬起那一帧」）—— 而**本件（双语这条活）
        /// 的白名单里没有 `BattleDriver.cs`** ⇒ 在本面板内部自己收**同一对边沿**，行为等价：
        /// 按下那一帧记「这一下是从哪开始的」、抬起那一帧（且两帧都落在框上）才触发 ——
        /// 这正是 uGUI `IPointerClickHandler` 的语义。
        /// 📌 **将来收口**：把它并进 `SettingsClickAt`（与兄弟三颗摆在一起）更整齐，判据也是那一处。
        /// ⛔ **别改成「按下就触发」**：A445 明确把上一颗（Auto Zoom）从按下挪回了抬起。</para>
        ///
        /// <para>⚠️ 自检可以直接喂这一对（`PointerFrame(pos, true)` → `PointerFrame(pos, false)`），
        /// 走的是**与真实输入同一条**判定（批处理里 `Mouse.current` 是 null、`Update` 也不跑）。</para></summary>
        bool LangPointerFrame(Vector3 world, bool down)
        {
            bool wasDown = _wasDown;
            _wasDown = down;
            if (down && !wasDown)
            {
                // 按下：记下这一下「从哪开始」。
                // 🆕 2026-10-18（A921）：**列表开着时**，原版那颗铺满全屏的透明 `Blocker` 在最上层
                //   （`CreateBlocker:1009`）⇒ 这一帧一律**吃掉**（滑块/别的按钮都不许起拖、不许响应）：
                //   按在**行**上 = 待会儿选它；按在**别处**（**含那颗框自己** —— `Blocker` 压在它上面）
                //   = 待会儿收起。这两条合起来就是原版「再点一下框 = 收起」的来路。
                if (LangListOpen)
                {
                    int row; _langArmed = false;
                    _langRowArmed = HitLangRow(world, out row) ? row : -1;
                    _langBlankArmed = _langRowArmed < 0;
                    return true;
                }
                _langArmed = HitLanguage(world);
                return false;
            }
            if (!down && wasDown)                                                      // 抬起：原版就是这一帧触发
            {
                // ① 点行 ⇒ 选中 + 收起（原版 `OnSelectItem:1247` = `value = i` + 末尾 `Hide()`）
                if (_langRowArmed >= 0)
                {
                    int armed = _langRowArmed; _langRowArmed = -1; _langBlankArmed = false;
                    int hit;
                    if (HitLangRow(world, out hit) && hit == armed) { ChooseLanguage(armed); return true; }
                    return true;      // 按在行上、抬到别处 ⇒ **不选**（但这一下仍归 `Blocker` 吃）
                }
                // ② 点空白 ⇒ 收起（原版那颗 `Blocker` 的 `onClick → Hide`）
                if (_langBlankArmed) { _langBlankArmed = false; HideLangList(); return true; }
                // ③ 点框 ⇒ 开/收（原版 `OnPointerClick → Show()`；开着时这一下会被 ① 吃掉，见上面）
                bool fire = _langArmed && HitLanguage(world);
                _langArmed = false;
                if (fire) { ToggleLangList(); return true; }
            }
            return false;
        }

        public void Show() { Visible = true; SetActive(true); }
        public void Hide() { Visible = false; SetActive(false); }
        void SetActive(bool on)
        {
            foreach (var go in new[] { _shade, _bg, _close, _closeIcon, _diffBtn })
                if (go != null) go.gameObject.SetActive(on);
            if (_resignBtn != null) _resignBtn.gameObject.SetActive(on);   // 一张 `Image (Sliced)`（不是 `ImageQuad`）
            if (_skipBtn != null) _skipBtn.gameObject.SetActive(on);       // 🆕 A991：同上（那颗也是九宫格 ⇒ uGUI 一张）
            foreach (var l in new[] { _title, _resignText, _skipText, _diffLabel, _diffValue,
                                      _musicLabel, _fxLabel, _voiceLabel, _azLabel,
                                      // 🆕 2026-10-17：语言那一行的两颗字（框里的语言名 + 左边的标签）
                                      _langCap, _langSelText,
                                      // 🆕 2026-10-16（T01）：静音那一行的三颗字。⚠️ 那两颗
                                      // ON/OFF 字**挂在各自那半的勾下面**（原版父链如此），但在这里
                                      // 先随面板开关走 —— ON 那半的**额外**闸在 `RefreshMuteChat()`（本方法末尾调）。
                                      _muteLabel, _muteOffText, _muteOnText })
                if (l != null) l.gameObject.SetActive(on);
            if (_azBox != null) _azBox.gameObject.SetActive(on);
            // 🆕 2026-10-16（T01）：静音那一行的三张图（OFF 那半恒显示、ON 那半看开关 —— 见 `RefreshMuteChat`）
            if (_muteOffBg != null) _muteOffBg.gameObject.SetActive(on);
            if (_muteOffChk != null) _muteOffChk.gameObject.SetActive(on);
            // 🆕 2026-10-17：语言那一行那两张图
            if (_langField != null) _langField.gameObject.SetActive(on);
            if (_langArrow != null) _langArrow.gameObject.SetActive(on);
            // 勾那一层**不跟着面板开关走**：它还要看 `AutoZoom.Enabled`（见 `RefreshAutoZoomCheck`）
            RefreshAutoZoomCheck();
            // 🆕 2026-10-16（T01）：静音那一行 ON 那半同理 —— 它还要看**开关本身**（`_muteChatOn`）。
            RefreshMuteChat();
            // 🆕 三根音量滑块。⚠️ `WfSlider` **不是** `MonoBehaviour`，没有 `.gameObject` ——
            //    要它自己的 `SetVisible`（第一次写漏了会在这里编译不过）。
            foreach (var s in Sliders) if (s != null) s.SetVisible(on);
            if (!on)
            {
                _dragSlider = null; _wasDown = false; _langArmed = false;
                // 🆕 2026-10-18（A921）：关面板时**把列表也收掉** —— 列表挂在**那颗语言框**下面，框停用会让它
                //   在层级上跟着不显示，可 `_langList.gameObject.activeSelf` **仍是真** ⇒ `LangListOpen` 会
                //   谎报「开着」（下次开面板一进来列表就露着）。⛔ 别只靠父件的 `SetActive`。
                HideLangList();
            }
        }

        /// <summary>指针是不是落在面板上（开着的时候**吃掉**点击，别穿到棋盘）。
        /// 🔴 **2026-10-16（`T09` 批 2）**：`HudPiece.Contains`（走 `GetWorldCorners`）→ **`RectContains`**
        /// （本件自己的局部坐标系 + 尺寸）—— 与 `HitResign`/`HitLangRow` **同一套写法**，
        /// 而且不依赖「画布有没有被引擎摆过」（批处理下 `GetWorldCorners` 的可用性没验过）。
        /// 几何上两者**同一块矩形**（`_bg` 就是 743.2×758.6、中心锚）。</summary>
        public bool Contains(Vector3 world)
        {
            if (_bg == null) return false;
            var s = HudCanvas.SizePx(_bg.Rect);
            return RectContains(_bg.transform, world, s.x, s.y);
        }

        /// <summary>这一下点在「投降」上吗</summary>
        public bool HitResign(Vector3 world)
        {
            if (!Visible || _resignBtn == null) return false;
            return RectContains(_resignBtn.transform, world, U(ResignWPx), U(ResignHPx));
        }

        /// <summary>🆕 **2026-10-18（A991）**：这一下点在**「跳过教程」**上吗。
        /// 判据与 <see cref="HitResign"/> **逐字同形** —— 同一颗 `Bottom buttons` 下的镜像位、
        /// 同一套九宫格命中算法（`RectContains`，因为根节点不是 `ImageQuad`）。
        /// 🔴 **真实输入与自检走的就是这一条**（驱动层 `BattleDriver.SettingsClickAt`）。</summary>
        public bool HitSkipTutorial(Vector3 world)
        {
            if (!Visible || _skipBtn == null) return false;
            return RectContains(_skipBtn.transform, world, U(SkipWPx), U(SkipHPx));
        }

        /// <summary>这一下点在「关闭」上吗。
        /// <para>① 圆底那颗（`_close`，75²）—— ⚠️ 原版那颗 **`m_RaycastTarget = 0`**（收不到射线），
        /// 这是**我们多的**一档；结果无害（它被 ② 那块完全包住），留着当「叉图取不到」时的兜底。</para>
        /// <para>② 🔴 **2026-10-19（A1301②）**：叉图那颗**改走原版命中框** —— `rect` 56.37×54.50 按
        /// `m_RaycastPadding(−20,−20,−20,−20)` **外扩** ⇒ **96.37×94.50**（原来走 `_closeIcon.Contains`，
        /// 量的是**画出来那个 31.5 的正方** ⇒ 原版那一圈**全丢**）。</para></summary>
        public bool HitClose(Vector3 world)
        {
            if (!Visible || _close == null) return false;
            // 🔴 **2026-10-16（`T09` 批 2）**：`_close.Contains` → `RectContains`（同 `Contains` 那条口径）。
            var s = HudCanvas.SizePx(_close.Rect);
            if (RectContains(_close.transform, world, s.x, s.y)) return true;
            return CloseIconHit(world);
        }

        /// <summary>🆕 **2026-10-19（A1301②）**：叉图那颗 `Image` 的**原版命中框**里吗。
        /// <para>🔴 算式**只此一份** —— 转发 `MenuDraw.PaddedHitRect`（它再转发 `PaddedRect`；
        /// 符号口径与退化守卫都在那一边）。⛔ 本件一个字都不写 `±pad`。
        /// 形状同 `BattleDriver.HitPaddedRect`（那一颗是 A964② 的先例）。</para>
        /// <para>⚠️ 坐标系要跳一下：quad 局部 **y 向上**，`PxRect` 是**左上原点、y 向下**。</para></summary>
        bool CloseIconHit(Vector3 world)
        {
            if (_closeIcon == null) return false;
            var l = _closeIcon.transform.InverseTransformPoint(world);
            // 🔴 **2026-10-16（`T09` 批 2）**：`U(1f)` 现在是 **1**（面板帧的单位 = px）⇒ 这一句
            //    **自己就跟着档走**（旧世界档 `U(1f)` = 1/108 ⇒ ppu = 108）。⛔ 别把它写死成 108。
            float ppu = 1f / U(1f);                   // = 1：取本件唯一那个换算的倒数 ⇒ 不写第二份换算
            var hit = MenuDraw.PaddedHitRect(
                new PxRect(-CloseIconRectW * 0.5f, -CloseIconRectH * 0.5f,
                            CloseIconRectW * 0.5f,  CloseIconRectH * 0.5f),
                new Vector4(CloseIconPadPx, CloseIconPadPx, CloseIconPadPx, CloseIconPadPx));
            float px = l.x * ppu, py = -l.y * ppu;    // 本件局部 px（左上原点、y 向下）
            return px >= hit.x1 && px <= hit.x2 && py >= hit.y1 && py <= hit.y2;
        }

        /// <summary>🆕 **2026-10-19（A1301②）自检用**：叉图那颗的**原版命中框**尺寸（**画布 px**）
        /// = 56.37×54.50 四边外扩 20 ⇒ **96.37×94.50**。算式同 `CloseIconHit`（唯一一份 `PaddedRect`）。</summary>
        public static Vector2 CloseHitPx
        {
            get
            {
                var r = MenuDraw.PaddedRect(new PxRect(0f, 0f, CloseIconRectW, CloseIconRectH),
                                            new Vector4(CloseIconPadPx, CloseIconPadPx, CloseIconPadPx, CloseIconPadPx));
                return new Vector2(r.W, r.H);
            }
        }

        /// <summary>这一下点在「对手难度」上吗
        /// （🔴 **2026-10-16（`T09` 批 2）**：`Contains` → `RectContains`，同 `HitClose` 那条口径）。</summary>
        public bool HitDifficulty(Vector3 world)
        {
            if (!Visible || _diffBtn == null) return false;
            var s = HudCanvas.SizePx(_diffBtn.Rect);
            return RectContains(_diffBtn.transform, world, s.x, s.y);
        }

        /// <summary>点面板的空白处 —— 吃掉，但不做事（原版有 `Dark Shade` 挡住背后）</summary>
        public bool HitBlank(Vector3 world) { return Visible && Contains(world); }

        // ---- 自检用 ----
        public string ResignText { get { return _resignText != null ? _resignText.Text : null; } }
        /// <summary>投降按钮的世界坐标（自检照着它点 —— 走的是和真实点击同一条命中判定）</summary>
        public Vector3 ResignWorldPos { get { return _resignBtn != null ? _resignBtn.transform.position : Vector3.zero; } }

        // ---- 🆕 2026-10-18（A964① ③）：关闭钮的只读口（⛔ 只给自检，不给生产用）----
        //  补它的理由：`HitResign` / `HitDifficulty` / `HitAutoZoom` **各有一颗中心读口**，
        //  只有「关闭」这颗没有 ⇒ `E2` 探针（命中区 ⊇ 实绘矩形）量不到它，只能整个跳过。
        //  形状照 `BattleDriver.CameraResetButtonDrawnPx`（中心 + 实绘尺寸一对）。

        /// <summary>关闭钮的世界坐标（自检照着它点 —— 走的是和真实点击同一条 <see cref="HitClose"/>）。
        /// 取**圆底那颗** `_close`（同族的 `Resign/Difficulty/AutoZoom` 也都是取底图那颗，
        /// 字/图标是压在它上面的、中心同一个）。</summary>
        public Vector3 CloseWorldPos { get { return _close != null ? _close.transform.position : Vector3.zero; } }

        /// <summary>关闭钮**实绘**的宽 × 高（**设计 px**）。
        /// 判据 = 那颗 `Image` 的 `sizeDelta`（= 贴图比例 × 建时给的高 `U(ClosePx)`），
        /// ⛔ 不是「按 75×75 那个框反推」—— 两者不一致就是 `E2` 要报的东西。
        /// 🔴 **2026-10-16（`T09` 批 2）**：改读 **px**（`HudCanvas.SizePx` = `sizeDelta` 的成对逆）
        /// —— 旧那套读 `WorldW/H`（**世界单位**，自检再 ×108）⇒ **自检那几条要跟着去掉 `× 108f`**。</summary>
        public Vector2 CloseDrawnSize
        {
            get { return _close != null ? HudCanvas.SizePx(_close.Rect) : Vector2.zero; }
        }

        // ---- 🆕 2026-10-18（A991）「跳过教程」钮的自检口（⛔ 只读，不给生产用）----
        /// <summary>那颗钮（九宫格根 + 字）**都建出来了**没有。</summary>
        public bool SkipTutorialBuilt { get { return _skipBtn != null && _skipText != null; } }
        /// <summary>那颗钮**现在画不画**（跟着面板开关走 —— 原版那颗在 `BattleSettingsPanel` 里、
        /// 面板在场景里是 `(inactive)`，运行期由设置按钮打开）。</summary>
        public bool SkipTutorialShown { get { return _skipBtn != null && _skipBtn.gameObject.activeSelf; } }
        /// <summary>那颗钮（九宫格根）的**世界坐标** —— 自检照着它点（走与真实输入同一条判定）。</summary>
        public Vector3 SkipTutorialWorldPos { get { return _skipBtn != null ? _skipBtn.transform.position : Vector3.zero; } }
        /// <summary>那颗字上写的是什么（原版 TMP 原文 `Skip tutorial`）。</summary>
        public string SkipTutorialText { get { return _skipText != null ? _skipText.Text : null; } }
        /// <summary>难度按钮上写着的字（自检用）</summary>
        public string DifficultyText { get { return _diffValue != null ? _diffValue.Text : null; } }
        /// <summary>难度按钮的世界坐标（自检照着它点）</summary>
        public Vector3 DifficultyWorldPos { get { return _diffBtn != null ? _diffBtn.transform.position : Vector3.zero; } }
        /// <summary>自检用（**A276**）：投降钮那一层占的 **z 档**（没建出来 ⇒ `NaN`）。
        /// <para>判据 = 它**只由本件的 `PanelZ` 那一套决定**，不许随本面板的世界 z 变。</para>
        /// <para>🔴 **2026-10-16（`T09` 批 2）语义换了（铁律 5，如实记）**：uGUI 档里**根本没有局部 z**
        /// （画布上的件靠兄弟序/排序层）⇒ 原来那句 `transform.localPosition.z` 恒 `0`、**断言会变成恒真**
        /// （两颗面板都比 0 ⇒ 永远绿）。现在报的是**登记的兄弟序 z**（`HudCanvas.OrderZOf`）——
        /// 它才是「同一父节点下只该有一套 z 口径」这条判据在 uGUI 档的**对应物**（口径全文 →
        /// `资料/普查产出_1010/V4b_三件口径.md` §Q2）。⚠️ **自检那条断言要同批改**（见报告）。</para></summary>
        public float ResignLocalZ
        {
            get { return _resignBtn != null ? HudCanvas.OrderZOf(_resignBtn.Rect) : float.NaN; }
        }
        public bool HasArt
        {
            get
            {
                return _bg != null && _bg.Texture != null
                    && _close != null && _close.Texture != null
                    && _resignBtn != null && _resignTex != null
                    && _diffBtn != null && _diffBtn.Texture != null
                    // 🆕 A424：Auto Zoom 那一行的两张图（少一张 = 那一行画不出来 ⇒ 这条要红）
                    && _azBox != null && _azBox.Texture != null
                    && _azCheck != null && _azCheck.Texture != null;
            }
        }

        // ---- 🆕 A424 自检口（⛔ 只读，不给生产用）----

        /// <summary>`Auto Zoom` 那一行三件（底图 / 勾 / 文字）**都建出来了**没有。</summary>
        public bool AutoZoomRowBuilt { get { return _azBox != null && _azCheck != null && _azLabel != null; } }
        /// <summary>勾那一层**现在画不画**（原版那一格 = `Toggle.graphic` 的 `enabled`）。</summary>
        public bool AutoZoomCheckShown { get { return _azCheck != null && _azCheck.gameObject.activeSelf; } }
        /// <summary>那行文字上写的是什么（原版 TMP `m_text`）。</summary>
        public string AutoZoomLabelText { get { return _azLabel != null ? _azLabel.Text : null; } }
        /// <summary>勾选框（`Toggle` 那格）的世界坐标 —— 自检照着它点（走与真实输入同一条判定）。</summary>
        public Vector3 AutoZoomBoxWorldPos { get { return _azBox != null ? _azBox.transform.position : Vector3.zero; } }
        /// <summary>文字那格的世界坐标（= 原版 `Label` 的**左中**）—— 自检照它点。</summary>
        public Vector3 AutoZoomLabelWorldPos { get { return _azLabel != null ? _azLabel.transform.position : Vector3.zero; } }
        /// <summary>勾选框**实画的**宽 × 高（**设计 px**；判据 = uGUI `preserveAspect` 内接，见 `ImageQuad.FitHeight`）。
        /// 🔴 **2026-10-16（`T09` 批 2）**：改读 **px**（`HudCanvas.SizePx`）⇒ **自检那两条要跟着去掉 `× 108f`**。</summary>
        public Vector2 AutoZoomBoxDrawnSize
        {
            get { return _azBox != null ? HudCanvas.SizePx(_azBox.Rect) : Vector2.zero; }
        }
        /// <summary>勾**实画的**宽 × 高（**设计 px**，同上）。</summary>
        public Vector2 AutoZoomCheckDrawnSize
        {
            get { return _azCheck != null ? HudCanvas.SizePx(_azCheck.Rect) : Vector2.zero; }
        }
        /// <summary>勾选框那个绿（自检钉「染对色了没有」；⛔ 值本身来自原版 `m_Colors.m_NormalColor`）。</summary>
        public Color AutoZoomBoxTint { get { return AzBoxTint; } }

        // ---- 🆕 2026-10-17 语言那一行的自检口（⛔ 只读，不给生产用）----

        /// <summary>那一行的四件（框 / 箭头 / 框里的语言名 / 左边那颗标签）**都建出来了**没有。</summary>
        public bool LanguageRowBuilt
        { get { return _langField != null && _langArrow != null && _langCap != null && _langSelText != null; } }
        /// <summary>框里那行字写的是什么（= 当前语言名）。</summary>
        public string LanguageCaptionText { get { return _langCap != null ? _langCap.Text : null; } }
        /// <summary>左边那颗标签上写的是什么（词条 `MainMenu/Settings/ButtonLabel/SelectLanguage`）。</summary>
        public string LanguageLabelText { get { return _langSelText != null ? _langSelText.Text : null; } }
        /// <summary>下拉框那颗 quad 的世界坐标（自检照着它点 —— 走与真实输入同一条判定）。</summary>
        public Vector3 LanguageFieldWorldPos { get { return _langField != null ? _langField.transform.position : Vector3.zero; } }
        /// <summary>框**实画的**宽 × 高（**设计 px**；原版那颗是 `Simple`（拉伸）⇒ 应逐值等于 250×59.4）。
        /// 🔴 **2026-10-16（`T09` 批 2）**：改读 px ⇒ **自检那条要跟着去掉 `× 108f`**。</summary>
        public Vector2 LanguageFieldDrawnSize
        {
            get { return _langField != null ? HudCanvas.SizePx(_langField.Rect) : Vector2.zero; }
        }
        /// <summary>箭头**实画的**宽 × 高（**设计 px**；`preserveAspect`：46×19 内接进 20×20 ⇒ 高 20×19/46）。
        /// 🔴 **2026-10-16（`T09` 批 2）**：改读 px ⇒ **自检那条要跟着去掉 `× 108f`**。</summary>
        public Vector2 LanguageArrowDrawnSize
        {
            get { return _langArrow != null ? HudCanvas.SizePx(_langArrow.Rect) : Vector2.zero; }
        }
        /// <summary>箭头那颗 quad 的世界坐标（自检量它有没有落进框右端那一格）。</summary>
        public Vector3 LanguageArrowWorldPos { get { return _langArrow != null ? _langArrow.transform.position : Vector3.zero; } }
        /// <summary>左边那颗标签的世界坐标（= 原版 `SelectLanguageText` 框的**左中**）。</summary>
        public Vector3 LanguageLabelWorldPos { get { return _langSelText != null ? _langSelText.transform.position : Vector3.zero; } }
    }
}
