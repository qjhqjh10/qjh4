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

        ImageQuad _bg, _close, _shade;
        Label _title, _resignText, _closeText;
        ImageQuad _closeIcon;
        ImageQuad _diffBtn;
        Label _diffLabel, _diffValue;
        /// <summary>投降钮的根节点。⚠️ **不是 `ImageQuad`** —— 原版底图 `40K_button` 是
        /// `Image.Type = Sliced`（border 234,46,234,46），要九宫格；单个 quad 拉不出那个形。</summary>
        GameObject _resignBtn;
        Texture _resignTex;

        // ---- 🆕 2026-10-12（A424）：「Auto Zoom」那一行（原版 `Auto Zoom Toggle`）----
        /// <summary>勾选框底图（原版 `Toggle.targetGraphic`，图 `40K_dropdown_bg`）。</summary>
        ImageQuad _azBox;
        /// <summary>勾（原版 `Toggle.graphic`，图 `40K_settings_icon_checkmark`）。</summary>
        ImageQuad _azCheck;
        /// <summary>那一行的文字（原版 `Label`，TMP `m_text = "Auto zoom"`）。</summary>
        Label _azLabel;

        // ---- 🆕 2026-10-17：「Language Selector」那一行（原版 `LanguageSelector.languagesDropdown`）----
        /// <summary>下拉框底图（原版 `LanguagesDropdown` 那颗 `Image`，图 `40K_dropdown_field_closed`）。</summary>
        ImageQuad _langField;
        /// <summary>框右端那个箭头（原版 `Arrow`）。</summary>
        ImageQuad _langArrow;
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
        struct LangRow { public ImageQuad Bg, Chk; public Label Lb; public int Idx; }
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
        const float LabelLeftX = -282.54f;
        const float LabelFontPx = 42f;                       // 原版 fs42（autoSizing 10–42）
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

        /// <summary>本面板的**渲染队列**。这一族件原来一次都没显式设过队列 = `Sprites/Default` 的默认档
        /// （**3000**，同 `WaitBanner.BattleQChrome` / `CardDisplayWindow.QChrome`）。
        /// 🔴 走 `MenuDraw.Nine` 的地方**必须显式传**这个数 —— 那个助手会写队列，
        /// 不传就等于在战斗现场顺手换了一次排序（2026-10-04 · A50③ 收口时补的）。</summary>
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
        /// 词条表在远端 CCD）⇒ 正式译文**拿不到**，只当「键 + 兜底」里的那个键记着（同 `Resign` 那条口径）。</summary>
        public const string AutoZoomTermKey = "Settings/Graphics/AutoZoom";

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
        const float LstPanelTexW = 119f, LstPanelTexH = 102f;
        const float LstPanelPpuMul = 1.09f;
        static readonly Vector4 LstPanelBorder = new Vector4(23f, 20f, 23f, 20f);
        static readonly Vector4 LstPanelBorderOut =
            new Vector4(23f / LstPanelPpuMul, 20f / LstPanelPpuMul, 23f / LstPanelPpuMul, 20f / LstPanelPpuMul);
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
        /// <summary>列表这一族的**渲染队列** —— 本工程没有 UGUI `Canvas` 排序层（原版 `Template` 上那颗
        /// `Canvas.overrideSorting` + `DropdownList.OnEnable` 把 `sortingLayerName` 顶到 `"PopUps"`），
        /// 等价物 = 渲染队列（`CLAUDE.md` §三：分层要用队列、别靠 z）。
        /// 序：本面板其余件（队列默认档 **3000**）&lt; 列表底 **3141** &lt; 行底 **3142** &lt; 行里的字/勾 **3143**。
        /// ⚠️ 主菜单那扇还有一档 `QBlocker 3140`（那颗铺满全屏的透明 `Blocker`）；本扇的「点空白收起」是
        /// **几何判定**（见 `HitLangBlank`）⇒ **不建那颗透明块**，但**序照留**（列表仍从 3141 起）。</summary>
        const int QLangList = 3141, QLangItem = 3142, QLangText = 3143;

        static float U(float px) { return px / 108f; }

        /// <summary>建一个**纯分组节点**（只有 `RectTransform`、不画东西）。坐标 = **面板内 px（y 向上）相对父件**。</summary>
        static Transform Node(Transform parent, string name, float cx, float cy, float z)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(U(cx), U(cy), z);
            return go.transform;
        }

        /// <summary>建一块图（`ImageQuad`，锚点恒 (0.5,0.5)）：`w`×`h` 是**面板内 px**；`tint` 传 `null` = 不染。
        /// 🔴 `SetAspect(w/h)` —— 原版那几颗都是 `Simple`（**拉伸**，只有 `Arrow` 是 `preserveAspect`）⇒ 按框给比值。</summary>
        static ImageQuad Img(Transform parent, string name, Texture tex, float cx, float cy,
                              float w, float h, float z, int q, Color? tint)
        {
            var quad = ImageQuad.Create(parent, tex, new Vector3(U(cx), U(cy), z), U(h),
                                        new Vector2(0.5f, 0.5f), name);
            if (quad != null)
            {
                quad.SetAspect(w / h);
                if (tint.HasValue) quad.SetTint(tint.Value);
                quad.SetRenderQueue(q);
            }
            return quad;
        }

        /// <summary>把九宫格根节点下所有块染成同一个色（根节点自己没有 `ImageQuad` 组件）。</summary>
        static void TintAll(GameObject root, Color c)
        {
            if (root == null) return;
            foreach (var q in root.GetComponentsInChildren<ImageQuad>(true)) q.SetTint(c);
        }

        /// <summary>世界坐标是不是落在九宫格根节点的矩形里（它没有 `ImageQuad.Contains`，命中测试自己算）。
        /// 锚点固定 0.5,0.5（`CreateNineSlice` 就是这么摆的）。</summary>
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

        public static SettingsPanel Create(Transform parent, Action onResign, Action onCycleDifficulty)
        {
            // 🔴 **2026-10-11（A218）**：根节点是 `RectTransform` + 写 `sizeDelta`。
            //    判据 = 原版 `BattleSettingsPanel` 实读（`bundle_scenes_scenes_battlearena1`，2026-10-11 现读）：
            //    `RectTransform` · `anchor (0.5,0.5) 重合` · `pivot (0.5,0.5)` ·
            //    **`m_SizeDelta = (743.202, 758.6345)`** · `ap (0, −29.317)` —— 与本文件头那句
            //    「面板本体 743.2 × 758.6（`BattleSettingsPanel` 自己的 rect）」同源（取整那一份）。
            //    改坏法：删掉 `SetPxSize` ⇒ `Editor/BattleScene.cs` §A218「设置面板根 = 743.2×758.6」红。
            var go = new GameObject("SettingsPanel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            MenuDraw.SetPxSize(go.transform, PanelW, PanelH);
            var p = go.AddComponent<SettingsPanel>();
            p.OnResign = onResign;
            p.OnCycleDifficulty = onCycleDifficulty;
            p.Build();
            p.Hide();
            return p;
        }

        void Build()
        {
            // ---- 底板：原版是 `Mask`+`Background fill`（图 `40k_popup_texture`）----
            // z 都要**比棋盘/HUD 靠前**（面板盖住它们）：-0.45 比结算面板(−0.50)靠后、比任何卡靠前
            const float Z = -0.45f;
            // 挡在背后的整屏压暗 —— 原版 `BattleSettingsPanel` 里就有一个 `Dark Shade`（3963.5×2693）
            _shade = ImageQuad.Create(transform, SolidTex(LayoutSpace.VisibleWidth / LayoutSpace.DesignHeight),
                                      new Vector3(0f, 0f, Z + 0.02f),
                                      LayoutSpace.DesignHeight * 1.05f, new Vector2(0.5f, 0.5f), "settings_shade");
            if (_shade != null) _shade.SetTint(new Color(0f, 0f, 0f, 0.62f));

            // 面板底：原版是 `Mask` + `Background fill`（图 `40k_popup_texture`，128×128 的九宫格填充）。
            // ⚠️ **不能直接用那张图**（试过）：它是**半透明填充**，拉大到 743×758 之后板子几乎全透、
            //    棋盘从面板里透出来；而且我们没有九宫格，拉大边缘也糊。
            //    所以这里用**自建实底**（深色 + 不透明）+ 上面那层压暗 —— 标记成「我们的做法」。
            _bg = ImageQuad.Create(transform, SolidTex(PanelW / PanelH), new Vector3(0f, 0f, Z),
                                   U(PanelH), new Vector2(0.5f, 0.5f), "settings_bg");
            if (_bg != null) _bg.SetTint(new Color(0.13f, 0.13f, 0.16f, 0.98f));

            // 🔴 2026-09-19 用户口径：面板上这些**先用英文**（之后做彻底的完全翻译）。
            // ⚠️ 原版这个面板**有没有标题查不到**（规格文档 §① 里没记标题这一项）⇒ `Settings` 是**我们起的**。
            // 🔴 **2026-10-18（第十二轮 · W6）补判据（结论不变）**：本轮把「查不到」升级成**否定证据**——
            //    `battlearena1` 那棵树**逐文本节点**走了一遍（**137 个**有字或有词条的节点），
            //    `BattleSettingsPanel` 子树里**没有一个标题节点**；而且 `Battle/` 前缀的
            //    **93 条代码字面量**（`d:/2/tools/il2cpp_out/stringliteral.json`）里也**没有**标题键
            //    （`Battle/Settings/` 那一族只有 `Exit` / `ResignButton` / `SkipTutorial` 三条）。
            //    ⇒ **原版确实没有这一格**（铁律 11 例外①）⇒ 保留英文/中文自拟，⛔ 别去替它编一条原版键。
            _title = Label.Create(transform, "Settings", new Vector3(0f, U(PanelH * 0.5f) - U(70f), Z - 0.01f),
                                  4, new Color(0.95f, 0.93f, 0.88f), new Vector2(0.5f, 0.5f), "settings_title");

            // ---- 关闭钮：75×75，圆底 + 叉（原版 `Generic Close Button`）----
            float cx = U(PanelW * 0.5f) - U(ClosePx * 0.6f);
            float cy = U(PanelH * 0.5f) - U(ClosePx * 0.6f);
            _close = ImageQuad.Create(transform, CardArt.Ui("UI_Button_Round_background"),
                                      new Vector3(cx, cy, Z - 0.01f), U(ClosePx), new Vector2(0.5f, 0.5f), "settings_close");
            _closeIcon = ImageQuad.Create(transform, CardArt.Ui("40k_bt_close"),
                                          new Vector3(cx, cy, Z - 0.02f), U(ClosePx * 0.42f), new Vector2(0.5f, 0.5f), "settings_close_icon");

            // ---- 投降：原版 `Resign`，面板左下角 ----
            // 🔴 **2026-09-19 照原版真值重做** —— 原来这里是 (0,−U(120)) / 宽 249 / 橙字，
            //    三样都是我们挑的（旧注释写的「rect 取不到」是**错误否定**，值一直都在，
            //    见文件头那段的更正与 `资料/普查产出_0918/第18行_UI三小条_规格.md` §①）。
            var resignTex = CardArt.Ui("40K_button");
            _resignTex = resignTex;
            float resignTexW = resignTex != null ? resignTex.width : 489f;
            float resignTexH = resignTex != null ? resignTex.height : 107f;
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
            _resignBtn = MenuDraw.Nine(transform, resignTex,
                                       new PxRect(LayoutSpace.DesignPxW * 0.5f + ResignCxPx - ResignWPx * 0.5f,
                                                  LayoutSpace.DesignPxH * 0.5f - ResignCyPx - ResignHPx * 0.5f,
                                                  LayoutSpace.DesignPxW * 0.5f + ResignCxPx + ResignWPx * 0.5f,
                                                  LayoutSpace.DesignPxH * 0.5f - ResignCyPx + ResignHPx * 0.5f),
                                       new Vector4(ResignBorderL, ResignBorderB, ResignBorderR, ResignBorderT),
                                       resignTexW, resignTexH, QPanel, name: "settings_resign");
            if (_resignBtn != null)
                _resignBtn.transform.localPosition =
                    new Vector3(U(ResignCxPx), U(ResignCyPx), Z - 0.01f);
            TintAll(_resignBtn, ResignTint);
            // 文字：原版 `Resign`（本地化 key `Battle/Settings/ResignButton`）。
            // 🔴 **2026-09-19 用户口径：先用英文**（原版就是英文；中文**查不到** —— 客户端没有 I2 语言表），
            //    之后再做彻底的完全翻译。字号 fs38 按**拉丁大写高度**定（见 `SliderLabel` 那条注释）。
            _resignText = Label.Create(transform, Loc.T(ResignTerm), new Vector3(U(ResignCxPx), U(ResignCyPx), Z - 0.02f),
                                       4, Color.white, new Vector2(0.5f, 0.5f), "settings_resign_text");
            ApplyLangFont(_resignText, Loc.T(ResignTerm), ResignFontPx);

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
            //    −220 落在原版 `ChatToggle`（−174.94，我们没做）与投降钮（−310.5）之间，不撞任何原版元素。
            float diffY = U(-220f);
            _diffLabel = Label.Create(transform, "AI Difficulty", new Vector3(-U(190f), diffY, Z - 0.02f),
                                      4, new Color(0.95f, 0.93f, 0.88f), new Vector2(0.5f, 0.5f), "settings_diff_label");
            _diffBtn = ImageQuad.Create(transform, resignTex,
                                        new Vector3(U(70f), diffY, Z - 0.01f), U(diffH), new Vector2(0.5f, 0.5f), "settings_diff_btn");
            _diffValue = Label.Create(transform, "", new Vector3(U(70f), diffY, Z - 0.02f),
                                      4, new Color(1f, 0.86f, 0.55f), new Vector2(0.5f, 0.5f), "settings_diff_value");

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
            _azBox = ImageQuad.Create(transform, azBoxTex,
                                      new Vector3(U(AzBoxCxPx), U(AzBoxCyPx), Z - 0.01f),
                                      azBoxH, new Vector2(0.5f, 0.5f), "settings_autozoom_box");
            // ⚠️ 那个绿是 `Selectable` 的 `m_NormalColor`（ColorTint 常态档），**不是** Image 自己的 `m_Color`
            //    （那个是白）。`EverguildToggle.colorTintOnValueChange = 0` ⇒ 开关翻动**不**改色。
            if (_azBox != null) _azBox.SetTint(AzBoxTint);

            var azChkTex = CardArt.MenuUi("40K_settings_icon_checkmark");
            float azChkH = azChkTex != null
                         ? ImageQuad.FitHeight(U(AzBoxWPx), U(AzBoxHPx), azChkTex.width / (float)azChkTex.height)
                         : U(AzBoxHPx);
            _azCheck = ImageQuad.Create(transform, azChkTex,
                                        new Vector3(U(AzBoxCxPx), U(AzBoxCyPx), Z - 0.02f),
                                        azChkH, new Vector2(0.5f, 0.5f), "settings_autozoom_check");

            // 文字：原版 TMP 印的就是英文 `"Auto zoom"`（**小写 z**，逐字符照抄）；I2 词条 = `Settings/Graphics/AutoZoom`
            //   （客户端**没有**本地 I2 词条表 ⇒ 正式译文拿不到，同 `Resign` 那条口径：**先用原版英文**）。
            _azLabel = Label.Create(transform, AutoZoomLabelEn, new Vector3(U(AzLabelLeftPx), U(AzLabelCyPx), Z - 0.02f),
                                    4, Color.white, new Vector2(0f, 0.5f), "settings_autozoom_label");
            // ⚠️ 英文用「拉丁大写高度」定字号（fs42 是 TMP 的 font size，拉丁大写只占约 0.72 em）—— 同 `SliderLabel`。
            if (_azLabel != null) _azLabel.SetCapHeight(U(AzFontPx * 0.72f));

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
            _musicSlider = WfSlider.Create(transform, "music", new Vector3(U(SliderCx), U(SliderCy[0]), Z - 0.01f),
                                           WarpforgeAudio.Music, WarpforgeAudio.SetMusic,
                                           queue: QPanel, handlePx: 34.406f,
                                           handleOffset: WfSlider.HandleOffsetPx, capScale: 1f);
            _fxSlider = WfSlider.Create(transform, "fx", new Vector3(U(SliderCx), U(SliderCy[1]), Z - 0.01f),
                                        WarpforgeAudio.SoundFx, WarpforgeAudio.SetSoundFx,
                                        queue: QPanel, handlePx: 34.406f,
                                        handleOffset: WfSlider.HandleOffsetPx, capScale: 1f);
            _voiceSlider = WfSlider.Create(transform, "voice", new Vector3(U(SliderCx), U(SliderCy[2]), Z - 0.01f),
                                           WarpforgeAudio.VoiceOver, WarpforgeAudio.SetVoiceOver,
                                           queue: QPanel, handlePx: 34.406f,
                                           handleOffset: WfSlider.HandleOffsetPx, capScale: 1f);

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
        /// <item>三张图都是 `ImageQuad`（底图带绿染、箭头 `preserveAspect` 内接）——
        ///   本面板的 `Z` 一族是**裸局部 z**（同 `_azBox`/`_azCheck`）⇒ 底图 `Z − 0.01`、字/箭头 `Z − 0.02`；</item>
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
        /// ⚠️ 本面板**没建**的原版件还有：`ChatToggle`（'Mute opponent'）· `Skip tutorial` · `Debug Buttons`
        /// （那不在这条活的范围里，别顺手加）。</summary>
        void BuildLanguageRow(float z)
        {
            float cx = (LangFieldX1Px + LangFieldX2Px) * 0.5f;
            float cy = (LangRowTopPx + LangRowBotPx) * 0.5f;
            float w = LangFieldX2Px - LangFieldX1Px, h = LangRowTopPx - LangRowBotPx;

            _langField = ImageQuad.Create(transform, CardArt.MenuUi(LangFieldArt),
                                          new Vector3(U(cx), U(cy), z - 0.01f), U(h),
                                          new Vector2(0.5f, 0.5f), "settings_lang_field");
            if (_langField != null)
            {
                _langField.SetAspect(w / h);              // 原版那颗 `Image` 是 `Simple`（拉伸）⇒ 按框给比值
                _langField.SetTint(LangFieldTint);
            }

            // 框内那行当前语言名（原版 `LanguagesDropdown > Label`）—— 左中锚，x = 框左 + 10
            _langCap = Label.Create(transform, Loc.LanguageName(Loc.Current),
                                    new Vector3(U(LangFieldX1Px + LangCapInsetPx), U(LangRowTopPx - LangCapTopPx - LangCapHPx * 0.5f), z - 0.02f),
                                    4, LangCapColor, new Vector2(0f, 0.5f), "settings_lang_caption");
            ApplyLangFont(_langCap, Loc.LanguageName(Loc.Current), LangCapFontPx);

            // 右端那个箭头（原版 `Arrow`：20×20 + `preserveAspect` ⇒ 取短边内接）
            // 框内偏移 (225, 19) 是**到它自己的左上角** ⇒ 中心 = 框左 + 225 + 10、框顶 − 19 − 10
            var arrowTex = CardArt.MenuUi(LangArrowArt);
            float arrowH = arrowTex != null
                         ? ImageQuad.FitHeight(U(LangArrowPx), U(LangArrowPx), arrowTex.width / (float)arrowTex.height)
                         : U(LangArrowPx);
            _langArrow = ImageQuad.Create(transform, arrowTex,
                                          new Vector3(U(LangFieldX1Px + LangArrowOffXPx + LangArrowPx * 0.5f),
                                                      U(LangRowTopPx - LangArrowOffYPx - LangArrowPx * 0.5f), z - 0.02f),
                                          arrowH, new Vector2(0.5f, 0.5f), "settings_lang_arrow");
            if (_langArrow != null) _langArrow.SetTint(LangArrowTint);

            // 左边那颗标签（原版 `SelectLanguageText`，fs42 / Left/Middle / 白；框 335×57.53 ⇒ 框中 y = 316.95）
            _langSelText = Label.Create(transform, Loc.T(LangLabelTermKey),
                                        new Vector3(U(LangRowX1Px), U(LangSelCyPx), z - 0.02f),
                                        4, Color.white, new Vector2(0f, 0.5f), "settings_lang_label");
            ApplyLangFont(_langSelText, Loc.T(LangLabelTermKey), LangSelFontPx);

            // 🆕 2026-10-18（A921）：`Template` 子树 —— 挂在**那颗框**下面（原版 `Template` 的父就是
            //   `LanguagesDropdown`：`TMP_Dropdown.Show()` 那句 `SetParent(m_Template.transform.parent, false)`
            //   给的就是它）⇒ 列表里所有坐标都是**相对框**的 px（y 向上）。
            // ⚠️ z 比本面板其余件再靠前一档（`Z − 0.03`）：列表内部那一族的排序走**渲染队列**（见 `QLangList`），
            //   这一档只是让「列表 vs 面板其余件」在 z 上也自洽（⛔ 别靠 z 代替队列）。
            BuildLangTemplate(_langField != null ? _langField.transform : transform, z - 0.03f);
        }

        /// <summary>按**这段文本的语种**定字号：汉字 ≈ **1 em**、拉丁大写 ≈ **0.72 em**
        /// （判据 = `Battle/Label.SetCapHeight` / `SetGlyphHeight` 的 doc；`Loc.HasCjk` 是那条判据的**唯一一份**）。
        /// 🔴 **为什么必须有它**：这一行现在**跟着语言变**（英文 `Select Language` / 中文 `选择语言`）——
        /// 写死 `SetCapHeight(px × 0.72)` 的话中文会**小 28%**，写死 `SetGlyphHeight(px)` 的话英文会**大 39%**
        /// （本文件 `SliderLabel` 那条注释里早写着「彻底翻译成中文时这里要跟着换」—— 就是这一处）。</summary>
        static void ApplyLangFont(Label l, string text, float px)
        {
            // 🔴 **2026-10-18（W6）收口**：语种→字号的判据**只留一份**（`Label.SetScriptHeight`，
            //    它内部接的是 `Loc.HasCjk` 那条唯一判据）。这里只是把本件的 px→世界单位换算（`U()` = /108）传进去。
            if (l == null) return;
            l.SetScriptHeight(text, px, 108f);
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
        /// 第 `i` 行 = `Content` 顶往下 `i × 40.8707`。⚠️ 本件**不建裁切层** —— 12 行 490.45 < 视口 573.96
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
            _langListBg = MenuDraw.Nine(list, CardArt.MenuUi(LstPanelArt),
                                        new PxRect(0f, 0f, w, LstHPx), LstPanelBorder, LstPanelTexW, LstPanelTexH,
                                        QLangList, LstPanelTint, true, "list_bg", LstPanelBorderOut);
            // ⚠️ `MenuDraw.Nine` 按**画布 px** 算落位（`MenuDraw.Local`），本面板的子件一律 `U(px)` 口径
            //    ⇒ 摆位**显式覆盖**（同 `_resignBtn` 那条注释里的做法；`Nine` 的 z 恒 0 ⇒ 也要补）。
            if (_langListBg != null)
                _langListBg.transform.localPosition = new Vector3(0f, U(vpCy), z);

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
            var lb = Label.Create(item, txt, new Vector3(U(bl - rowCx), U((bt + bb) * 0.5f - rowCy), z),
                                  4, LstLblColor, new Vector2(0f, 0.5f), "Item Label");
            // 🔴 **队列必须显式给**：行底图在 `QLangItem`(3142)，而 `Label` 默认留在材质默认档（3000）
            //   ⇒ 不给的话**行底图会把字盖住**（本面板其余几颗字靠 z 分层、那些件同队列，这一族不同）。
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
                if (row.Bg != null) row.Bg.SetTexture(CardArt.MenuUi(LstItemArt), true);
            }
            _langHoverRow = -1;
        }

        /// <summary>指针落在**第几行**上（`-1` = 不在任何一行上）。判据 = 那一行的底图先个 quad 的**渲染矩形**
        /// （原版那一行整块都可点：`Toggle.targetGraphic` = `Item Background` 铺满整行）。</summary>
        public bool HitLangRow(Vector3 world, out int idx)
        {
            idx = -1;
            if (!Visible || !LangListOpen) return false;
            for (int i = 0; i < _langRows.Count; i++)
                if (_langRows[i].Bg != null && _langRows[i].Bg.Contains(world)) { idx = _langRows[i].Idx; return true; }
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
        /// 只在**换行**时才动贴图（`SetTexture(..., keepAspect: true)` —— 比例由行框定，别让新图的自身比例覆盖它）。</summary>
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
                row.Bg.SetTexture(CardArt.MenuUi(hot ? LstItemHiArt : LstItemArt), true);
            }
        }
        int _langHoverRow = -1;
        /// <summary>列表底板的九宫格根节点（自检量「建出来了没有」；⛔ 只读）。</summary>
        GameObject _langListBg;
        /// <summary>克隆体那一族的 z（与原型同一层 —— 排序靠**渲染队列**，见 `QLangList`；z 只是跟着面板走）。</summary>
        float _langRowsZ;

        // ---- 自检口（⛔ 只读，不给生产用）----
        /// <summary>`Template` 原型节点（**恒 inactive**）。</summary>
        public Transform LangTemplateNode { get { return _langTemplate; } }
        /// <summary>真正显示的那份（`"Dropdown List"`）；没开过 = `null`。</summary>
        public Transform LangListNode { get { return _langList; } }
        /// <summary>列表的父（= 那颗语言框）—— 自检核「列表挂在哪儿」用。</summary>
        public Transform LangFieldNode { get { return _langField != null ? _langField.transform : null; } }
        /// <summary>列表底板的九宫格根（自检量它的实画矩形）。</summary>
        public GameObject LangListBgNode { get { return _langListBg; } }
        public int LangRowCount { get { return _langRows.Count; } }
        public ImageQuad LangRowBg(int i) { return (i >= 0 && i < _langRows.Count) ? _langRows[i].Bg : null; }
        public Label LangRowLabel(int i) { return (i >= 0 && i < _langRows.Count) ? _langRows[i].Lb : null; }
        public ImageQuad LangRowCheck(int i) { return (i >= 0 && i < _langRows.Count) ? _langRows[i].Chk : null; }
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
            var local = _langField.transform.InverseTransformPoint(bg.transform.position);
            cx = local.x * 108f; cy = local.y * 108f;
            w = bg.WorldW * 108f; h = bg.WorldH * 108f;
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
            var l = Label.Create(transform, text, new Vector3(U(LabelLeftX), U(LabelBoxCy[i]), z - 0.01f),
                                 4, Color.white, new Vector2(0f, 0.5f), "settings_slider_label_" + i);
            // 🔴 **字号按语种选**（唯一一份实现 = `ApplyLangFont`）：fs42 是 TMP 的 font size，
            //    拉丁大写只占约 **0.72 em**、汉字约占 **1 em** —— 写死一种就会让另一档差 28%/39%。
            //    （这两条标签自 2026-10-18 起**跟着语言变**，所以这里不能再写死 `SetCapHeight(0.72em)`。）
            ApplyLangFont(l, text, LabelFontPx);
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
                    s.SetFromPointer(world);       // **按下即定位**（原版 `m_TargetGraphic` 只认手柄，这条是我们挑的）
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
        /// ⛔ 不是「行框整个」。真实输入与自检走的是同一条判定（自检拿 `AutoZoomRowWorldPos` 喂进来）。</summary>
        public bool HitAutoZoom(Vector3 world)
        {
            if (!Visible) return false;
            var l = transform.InverseTransformPoint(world);      // 面板局部（= px/108、y 向上）
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
        /// ⚠️ 判定与命中区**同一份几何**（`_langField.Contains` = 那颗 quad 的渲染矩形）——
        /// 与 `HitDifficulty` / `HitResign` 同一套写法（⛔ 别在这儿另写一份手算矩形）。</summary>
        public bool HitLanguage(Vector3 world)
        {
            if (!Visible) return false;
            return _langField != null && _langField.Contains(world);
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
            if (_resignBtn != null) _resignBtn.SetActive(on);   // 九宫格根节点（不是 ImageQuad）
            foreach (var l in new[] { _title, _resignText, _diffLabel, _diffValue,
                                      _musicLabel, _fxLabel, _voiceLabel, _azLabel,
                                      // 🆕 2026-10-17：语言那一行的两颗字（框里的语言名 + 左边的标签）
                                      _langCap, _langSelText })
                if (l != null) l.gameObject.SetActive(on);
            if (_azBox != null) _azBox.gameObject.SetActive(on);
            // 🆕 2026-10-17：语言那一行那两张图
            if (_langField != null) _langField.gameObject.SetActive(on);
            if (_langArrow != null) _langArrow.gameObject.SetActive(on);
            // 勾那一层**不跟着面板开关走**：它还要看 `AutoZoom.Enabled`（见 `RefreshAutoZoomCheck`）
            RefreshAutoZoomCheck();
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

        /// <summary>指针是不是落在面板上（开着的时候**吃掉**点击，别穿到棋盘）</summary>
        public bool Contains(Vector3 world)
        {
            return _bg != null && _bg.Contains(world);
        }

        /// <summary>这一下点在「投降」上吗</summary>
        public bool HitResign(Vector3 world)
        {
            if (!Visible || _resignBtn == null) return false;
            return RectContains(_resignBtn.transform, world, U(ResignWPx), U(ResignHPx));
        }

        /// <summary>这一下点在「关闭」上吗</summary>
        public bool HitClose(Vector3 world)
        {
            if (!Visible) return false;
            if (_close != null && _close.Contains(world)) return true;
            if (_closeIcon != null && _closeIcon.Contains(world)) return true;
            return false;
        }

        /// <summary>这一下点在「对手难度」上吗</summary>
        public bool HitDifficulty(Vector3 world)
        {
            return Visible && _diffBtn != null && _diffBtn.Contains(world);
        }

        /// <summary>点面板的空白处 —— 吃掉，但不做事（原版有 `Dark Shade` 挡住背后）</summary>
        public bool HitBlank(Vector3 world) { return Visible && Contains(world); }

        // ---- 自检用 ----
        public string ResignText { get { return _resignText != null ? _resignText.Text : null; } }
        /// <summary>投降按钮的世界坐标（自检照着它点 —— 走的是和真实点击同一条命中判定）</summary>
        public Vector3 ResignWorldPos { get { return _resignBtn != null ? _resignBtn.transform.position : Vector3.zero; } }
        /// <summary>难度按钮上写着的字（自检用）</summary>
        public string DifficultyText { get { return _diffValue != null ? _diffValue.Text : null; } }
        /// <summary>难度按钮的世界坐标（自检照着它点）</summary>
        public Vector3 DifficultyWorldPos { get { return _diffBtn != null ? _diffBtn.transform.position : Vector3.zero; } }
        /// <summary>自检用（**A276**）：投降钮写进去的**局部 z**（没建出来 ⇒ `NaN`）。
        /// 判据 = 它**只由本件的 `Z` 决定**，不许随本面板的世界 z 变 —— 同父的 `_close`/`_resignText`
        /// 都是裸局部 z ⇒ 同一父节点下只该有一套口径（判据全文 → `资料/普查产出_1010/V4b_三件口径.md` §Q2）。</summary>
        public float ResignLocalZ { get { return _resignBtn != null ? _resignBtn.transform.localPosition.z : float.NaN; } }
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
        /// <summary>勾选框**实画的**世界宽 × 世界高（判据 = uGUI `preserveAspect` 内接，见 `ImageQuad.FitHeight`）。</summary>
        public Vector2 AutoZoomBoxDrawnSize
        {
            get { return _azBox != null ? new Vector2(_azBox.WorldW, _azBox.WorldH) : Vector2.zero; }
        }
        /// <summary>勾**实画的**世界宽 × 世界高。</summary>
        public Vector2 AutoZoomCheckDrawnSize
        {
            get { return _azCheck != null ? new Vector2(_azCheck.WorldW, _azCheck.WorldH) : Vector2.zero; }
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
        /// <summary>框**实画的**世界宽 × 世界高（原版那颗是 `Simple`（拉伸）⇒ 应逐值等于 250×59.4 px ÷ 108）。</summary>
        public Vector2 LanguageFieldDrawnSize
        { get { return _langField != null ? new Vector2(_langField.WorldW, _langField.WorldH) : Vector2.zero; } }
        /// <summary>箭头**实画的**世界宽 × 世界高（`preserveAspect`：46×19 内接进 20×20 ⇒ 高 20×19/46）。</summary>
        public Vector2 LanguageArrowDrawnSize
        { get { return _langArrow != null ? new Vector2(_langArrow.WorldW, _langArrow.WorldH) : Vector2.zero; } }
        /// <summary>箭头那颗 quad 的世界坐标（自检量它有没有落进框右端那一格）。</summary>
        public Vector3 LanguageArrowWorldPos { get { return _langArrow != null ? _langArrow.transform.position : Vector3.zero; } }
        /// <summary>左边那颗标签的世界坐标（= 原版 `SelectLanguageText` 框的**左中**）。</summary>
        public Vector3 LanguageLabelWorldPos { get { return _langSelText != null ? _langSelText.transform.position : Vector3.zero; } }
    }
}
