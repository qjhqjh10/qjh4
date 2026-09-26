// SettingsWindow.cs — 主菜单设置窗（原版 `Main Menu Settings Window`，类 `SettingsMenu : GameWindowWithTabs`）
//
// ============================ 为什么这个文件现在才出现 ============================
// 它一直属于「阶段二**第 4 层**及以后，还没开」。后果是：**主菜单右上角那颗齿轮点了没反应**
//   （`MainMenuRuntime.cs:359` 只建了图、没接点击）= **静默失败**（本工程红线）。
// 2026-09-26 联机线开工，用户拍板「**联机页挂在设置窗里**，设置窗照原版建，先做 图像 / 音频 / 联机 三页」
//   ⇒ 连同窗壳一起建。判据全文 → `资料/联机P2P_设计与交接.md` §3·5。
//
// ============================ 照原版的部分（逐条有出处） ============================
// 出处 = `工具/menu_rect.py bundle_menus_assets_all "Main Menu Settings Window"` + 原始 JSON 实读。
//   · 根 `RectTransform_-7066813013973172314.json`：**`m_LocalScale = (0.9,0.9,0.9)`** ——
//     🔴 **只这一扇窗是 0.9**（`Rewards Base Submenu Variant` / `Shop Menu Variant` 都是 1）
//     ⇒ 子节点按**未缩放的矩形**摆，世界里的位置 = 屏幕中心 + 0.9 ×（矩形中心 − 屏幕中心）。
//     断言要用 `SettingsWindow.Screen(...)` 换算（别直接拿原版矩形当世界坐标 —— 那会差 11%）。
//   · `Menu Dark Background` 4574.60×2572.36，色 **(0,0,0,0.7725)**（无 sprite）。
//   · `Menu Area` [328.10,123.11]–[1602.50,966.19]：底图 `40k_popup`（Sliced，九宫格 169,160,169,160）
//     ```Mask``` [338.50,132.55]–[1592.62,956.39]；`Background fill` [510.62,132.55]–[1592.72,956.39]
//     = `40k_popup_texture` **Tiled**（`m_Type=2`，`ppuMultiplier=2.0` ⇒ 128/2 = **64 px 一格**）。
//   · `Generic Close Button` [1559.00,91.61] 75×75（`UI_Button_Round_background`）
//     + `Icon` [1568.31,101.86]–[1624.68,156.35]（`40k_bt_close`）。
//   · `Tab Buttons` 列 [328.10,123.10]–[506.52,966.19]（178.42 宽）：
//     VLG **padTop 30** · spacing 0 · align **UpperCenter** · childControlWidth 1 · childControlHeight 0
//     ⇒ 每个键 **178.42 × 157.68**，从 y = 123.10+30 = **153.10** 起往下排（第 k 个的顶边 = 153.10 + k×157.68）。
//   · `Separators` [505.07,103.11]–[507.97,986.19]（`40k_Separator_Fade_Sides_Vertical`）。
//   · 内容区 `Tab Content` [506.52,123.11]–[1538.78,966.19]；页标题 `Tab Title` [596.52,189.24]–[1538.78,259.24]
//     （**fs 55 · 左对齐**，实测 `m_HorizontalAlignment=1`）。
//   · 音频页（原版 `Media Tab`）：`Audio Settings` [596.52,280.15]–[1280.71,630.95]，**3 行 × 105 高**，从顶往下。
//   · 图像页（原版 `Graphics Tab`）：`Content` [506.52,267.71]–[1538.89,966.19]（VLG）；
//     `Quality Selector` 行 [551.52,267.71]–[1386.37,327.10]（下拉框到 x=952.18、**文字在框右边** [978.86…]）；
//     勾选行在 `Scroll View` [551.52,432.50]–[1538.89,954.00] 里，`Content` x 563.52–1018.73、**每行 76 高、步长 80.6**。
//   · `Debug Buttons` 那一排**不建** —— 全量反编译 `SettingsMenu__Awake.c` 里对它 `SetActive(false)`（调试层）。
//
// ============================ 🔴 我们挑的 / 查不到的（不许冒充原版） ============================
//   ① **「联机」这一页原版没有**（搜过 Online/Network/Server/Connect/Region/Ping/Multiplayer/Matchmak，
//      设置窗里一个都没有）⇒ 这一页是**新增设计**（用户 2026-09-26 的规格）。
//   ② **页签文案**：解包里那几个 `Tab Toggle Title` 的 `m_text` 是**西班牙语**（`Gráficos`/`Soporte`/`Cuenta`，
//      第 2 页是 `Multimedia`）、TMP **没挂 I2 词条** ⇒ 英文正式文案本地拿不到。我们写
//      **Graphics / Audio / Online**（英文，与工程别处「先用英文」的口径一致），**这是我们的选择**。
//   ③ **只建 5 页里的 3 页**（General / Account / Support 不建 —— 账号页整页走服务器、支持页是外链、
//      General 页是语言/退出游戏）。切到没建的页要**出声**，不静默。
//   ④ **联机页的页面标题、说明行、状态行**都是我们写的文案。
//   ⑤ 原版勾选图与下拉框底的 **sprite PathID 没解出名字**（`-5728790147372056906` / `4411787853012002210`
//      / `4198243566598287219`，UnityPy 按 PathID 反查没查到）⇒ 勾选改用原版**别处**在用的
//      `40K_toggle_on/off`，下拉框底用 `40K_button`。**这是我们的选择**，记在正本 §九。
using System;
using System.Collections.Generic;
using UnityEngine;
using CardPresentation.Net;

namespace CardPresentation
{
    /// <summary>设置窗的三个页（原版有五个，我们只建这三个 —— 见文件头 ③）。</summary>
    public enum SettingsTab { Graphics = 0, Audio = 1, Online = 2 }

    public class SettingsWindow : GameWindow
    {
        // ============================================================ 常量（出处见文件头）

        /// <summary>根 `m_LocalScale`。**只这一扇窗是 0.9**，别抄到别的窗上。</summary>
        public const float RootScale = 0.9f;

        public const float PopL = 328.10f, PopT = 123.11f, PopR = 1602.50f, PopB = 966.19f;
        public const float MaskL = 338.50f, MaskT = 132.55f, MaskR = 1592.62f, MaskB = 956.39f;
        public const float FillL = 510.62f, FillT = 132.55f, FillR = 1592.72f, FillB = 956.39f;
        public const float ShadeW = 4574.60f, ShadeH = 2572.36f;
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.7725f);

        public const float CloseL = 1559.00f, CloseT = 91.61f, CloseR = 1634.00f, CloseB = 166.61f;
        public const float CloseIconL = 1568.31f, CloseIconT = 101.86f, CloseIconR = 1624.68f, CloseIconB = 156.35f;

        public const float BarL = 328.10f, BarT = 123.10f, BarR = 506.52f, BarB = 966.19f;
        public const float BarPadTop = 30f;             // VLG `m_Padding.m_Top`
        public const float TabBtnH = 157.68f;           // childControlHeight = 0 ⇒ 用各自的 sizeDelta
        public const float BarSepL = 505.07f, BarSepT = 103.11f, BarSepR = 507.97f, BarSepB = 986.19f;

        public const float TabsL = 506.52f, TabsT = 123.11f, TabsR = 1538.78f, TabsB = 966.19f;
        public const float TitleL = 596.52f, TitleT = 189.24f, TitleR = 1538.78f, TitleB = 259.24f;
        public const float PageTitleFontPx = 55f;       // 实测 `Tab Title` fs=55

        // 音频页
        public const float AuL = 596.52f, AuT = 280.15f, AuR = 1280.71f, AuB = 630.95f;
        public const float AuRowH = 105f;
        /// <summary>滑块轨道宽 —— 取**本页容器宽**（原版 Media 页容器撑满 684.19）。
        /// ⚠️ `WfSlider` 自己的常量 561.08 来自**战斗内**那根，别混。</summary>
        public const float AuTrackW = 684.19f;

        // 图像页
        public const float GfxL = 506.52f, GfxT = 267.71f;
        public const float QualL = 551.52f, QualT = 267.71f, QualR = 1386.37f, QualB = 327.10f;
        public const float QualBoxR = 952.18f;          // 下拉框右边缘
        public const float QualTextL = 978.86f;         // 框右边那行字
        public const float ChkL = 563.52f, ChkT = 432.50f, ChkR = 1018.73f;
        public const float ChkRowH = 76f, ChkRowStep = 80.6f;
        /// <summary>勾选行在原版 `Scroll View` 里的**第几行**（树里实测）：Vsync 第 6 行、FPS 第 7 行。</summary>
        public const int VsyncRow = 5, FpsRow = 6;

        // 联机页（**这一页是我们设计的**，见文件头 ①）
        public const float OnRoleT = 280f, OnRoleB = 340f, OnRoleW = 300f, OnRoleGap = 20f;
        public const float OnLabelT = 370f, OnFieldT = 400f, OnFieldH = 60f, OnFieldW = 500f;
        public const float OnPassLabelT = 480f, OnPassT = 510f;
        public const float OnBtnT = 610f, OnBtnH = 80f, OnBtnW = 300f;
        public const float OnFillT = 610f, OnFillW = 120f;   // 【刷新】钮（IP 框右边）
        public const float OnStatusT = 730f;

        public const float FontLabel = 40f, FontSmall = 34f, FontButton = 40f;

        // 图（名字都是导入后的文件名 —— `CardArt.MenuUi` **不做空格→下划线转换**）
        public const string ArtPopup = "40k_popup";
        public const string ArtFill = "40k_popup_texture";
        public const string ArtCloseBg = "UI_Button_Round_background";
        public const string ArtCloseIcon = "40k_bt_close";
        public const string ArtSep = "40k_Separator_Fade_Sides_Vertical";
        public const string ArtTabBg = "40K_settings_button";
        public const string ArtTabBgSel = "40K_settings_button_selected";
        public const string ArtButton = "40K_button";
        public const string ArtToggleOn = "40K_toggle_on";
        public const string ArtToggleOff = "40K_toggle_off";
        public const string ArtInputBg = "InputFieldBackground";
        public const float PopupTexW = 359f, PopupTexH = 336f;
        public static readonly Vector4 PopupBorder = new Vector4(169f, 160f, 169f, 160f);
        /// <summary>`40k_popup_texture` 的平铺格 = 128 ÷ `ppuMultiplier 2`。</summary>
        public const float FillTilePx = 64f;
        /// <summary>`40K_button` 的九宫格 border（贴图 px）—— 同 `SettingsPanel` 投降钮那份实测值。</summary>
        public static readonly Vector4 BtnBorder = new Vector4(234f, 46f, 234f, 46f);
        public const float BtnTexW = 489f, BtnTexH = 107f;
        /// <summary>原版按钮绿（`Resign` 钮 / `OkButton` 同一支色）。</summary>
        public static readonly Color BtnGreen = new Color(0.3686f, 0.8941f, 0.5874f, 1f);
        public static readonly Color BtnGrey = new Color(0.62f, 0.62f, 0.66f, 1f);

        // 渲染队列：整窗一档（设置窗是弹窗，要盖住所有页；`Tooltip` 更高）
        public const int QShade = 3130, QPanel = 3131, QFill = 3132, QContent = 3133, QText = 3134, QOverlay = 3135;

        // ============================================================ 状态

        public SettingsTab Current { get; private set; }
        public static SettingsWindow Instance { get; private set; }

        // ---- 自检/将来接线要用的只读口（**别为了好看把它们藏起来**：自检拿不到就只能瞎猜）----
        public WfSlider[] AudioSliders { get { return _audioSliders; } }
        public Label QualityLabel { get { return _qualityLabel; } }
        public Label StatusLabel { get { return _statusLabel; } }
        public NetRole Role { get { return _role; } }
        public string Flash { get { return _flash; } }
        public Transform HostBlock { get { return _hostBlock; } }
        public Transform ClientBlock { get { return _clientBlock; } }
        /// <summary>当前显示的那一对输入框（主机块 / 客机块）。</summary>
        public MenuInputField IpField { get { return _role == NetRole.Host ? _ipField : _ipField2; } }
        public MenuInputField PwdField { get { return _role == NetRole.Host ? _pwdField : _pwdField2; } }

        readonly List<ImageQuad> _tabBgs = new List<ImageQuad>();
        readonly List<Transform> _pages = new List<Transform>();
        readonly List<string> MissingArt = new List<string>();

        // 联机页的控件
        MenuInputField _ipField, _pwdField;
        ImageQuad _roleHostBg, _roleClientBg;
        Transform _hostBlock, _clientBlock;
        Label _statusLabel;
        NetRole _role = NetRole.Host;
        string _flash;                                   // 最后一次操作的结果（人话）

        public static SettingsWindow Create(WindowsManager mgr)
        {
            var go = new GameObject("Main Menu Settings Window");
            var win = go.AddComponent<SettingsWindow>();
            win.type = WindowType.Popup;
            win.placement = WindowsPlacement.Popup;
            win.closeOnEsc = true;
            win.extraScaleSmallScreen = 1f;
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            Instance = win;
            return win;
        }

        /// <summary>扳手：**原版未缩放的矩形** → 世界里的实际矩形（根 `m_LocalScale = 0.9`）。
        ///
        /// 🔴 **2026-09-26 实测**：**不能**用「把根节点 `localScale` 设成 0.9」来复刻这个 0.9 ——
        ///   我们的 `ImageQuad` 是**按世界尺寸**画的（`Create` 给的 `h` 是 world 单位），
        ///   父节点的缩放**对它不起作用**（实测：75px 的关闭钮挂在 0.9 的根下，渲出来仍是 **75.0**，
        ///   而位置却按 0.9 移了 ⇒ 图会「站对地方、尺寸偏大 11%」）。
        ///   ⇒ **把 0.9 烘进每一个矩形**（本文件的画图包装函数统一先过 `Screen()`），根节点保持 scale 1。
        ///   ⚠️ 这条与工程里那句「分层要用渲染队列不能用 z」同族：**别跟渲染管线的实际语义较劲**。</summary>
        public static PxRect Screen(float x1, float y1, float x2, float y2)
        {
            const float cx = 960f, cy = 540f;
            float S(float v, float c) { return c + (v - c) * RootScale; }
            return new PxRect(S(x1, cx), S(y1, cy), S(x2, cx), S(y2, cy));
        }

        public override void Open()
        {
            Build();
            OpenTab(Current);
            // 会话还没起来就先建一台（主机【保存】/ 客机【检查连接】都要它）
            NetRuntime.Ensure(transform.root);
        }

        public void CloseWindow() { Close(); }

        // ============================================================ 建

        void Build()
        {
            var root = transform;
            // ⚠️ **根节点保持 scale 1** —— 原版那个 0.9 由 `Screen()` 烘进坐标（见 `Screen` 的注释）
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);
            _tabBgs.Clear(); _pages.Clear(); MissingArt.Clear();

            // 1) 压暗整屏（`Menu Dark Background`：无 sprite 的纯色块）
            Node(root, "Menu Dark Background", 960f - ShadeW * 0.5f, 540f - ShadeH * 0.5f,
                 960f + ShadeW * 0.5f, 540f + ShadeH * 0.5f);
            Solid(root, "Menu Dark Background", 960f, 540f, ShadeW, ShadeH, ShadeColor, QShade);

            // 2) 弹窗本体
            var area = Node(root, "Menu Area", PopL, PopT, PopR, PopB);
            Nine(area, "Generic Popup Background", PopL, PopT, PopR, PopB, ArtPopup, PopupTexW, PopupTexH,
                 PopupBorder, QPanel, Color.white);
            Node(area, "Mask", MaskL, MaskT, MaskR, MaskB);
            Tiled(area, "Background fill", FillL, FillT, FillR, FillB, ArtFill, FillTilePx, QFill);
            Rect(area, "Separators", BarSepL, BarSepT, BarSepR, BarSepB, ArtSep, QPanel);

            // 3) 关闭钮（圆底 + 图标；**图标是钮的子节点** —— 原版就是这么套的）
            var closeN = Node(area, "Generic Close Button", CloseL, CloseT, CloseR, CloseB);
            Rect(closeN, "bg", CloseL, CloseT, CloseR, CloseB, ArtCloseBg, QContent);
            Rect(closeN, "Icon", CloseIconL, CloseIconT, CloseIconR, CloseIconB, ArtCloseIcon, QOverlay);
            Hit(closeN, "Hit", CloseL, CloseT, CloseR, CloseB, QOverlay, () =>
            {
                Debug.Log("[Settings] 关闭钮");
                Close();
            });

            // 4) 左栏三个页签 + 三页内容
            BuildTabs(area);
            _pages.Add(BuildGraphicsPage(area));
            _pages.Add(BuildAudioPage(area));
            _pages.Add(BuildOnlinePage(area));
        }

        void BuildTabs(Transform area)
        {
            var bar = Node(area, "Tab Buttons", BarL, BarT, BarR, BarB);
            // 三个键（原版这一列有 5 个，我们只建 3 个 —— 文件头 ③）
            var specs = new[]
            {
                new { Tab = SettingsTab.Graphics, Label = "Graphics", Icon = "40K_settings_button_graphics" },
                new { Tab = SettingsTab.Audio,    Label = "Audio",    Icon = "40K_settings_button_quality" },   // 原版 Media 页用的就是 quality 那张
                new { Tab = SettingsTab.Online,   Label = "Online",   Icon = "40K_settings_button_account" },   // ⚠️ 我们挑的（联机页原版没有）
            };
            for (int i = 0; i < specs.Length; i++)
            {
                float t = BarT + BarPadTop + i * TabBtnH, b = t + TabBtnH;
                var page = Node(bar, specs[i].Label, BarL, t, BarR, b);
                var bg = Rect(page, "button_bg", BarL, t, BarR, b, ArtTabBg, QContent);
                _tabBgs.Add(bg);
                // 图标 141×107、文字 155×40 —— 模板位是「布局跑之前」的，横向**按居中**摆（我们的推导，文件头）
                float cm = (BarL + BarR) * 0.5f;
                Rect(page, "Icon", cm - 141f * 0.5f, t + 13f, cm + 141f * 0.5f, t + 120f, specs[i].Icon, QOverlay);
                var lb = Text(page, "Tab Toggle Title", specs[i].Label, cm - 155f * 0.5f, cm + 155f * 0.5f,
                              t + 106f, t + 146f, 35f, Color.white, QText);
                var tab = specs[i].Tab;
                Hit(page, "Hit", BarL, t, BarR, b, QOverlay, () => OpenTab(tab));
            }
        }

        /// <summary>换页。**只切 `activeSelf`，不重建**（同 `GameWindowWithTabs.ChangeTab` 的口径）。</summary>
        public void OpenTab(SettingsTab t)
        {
            Current = t;
            for (int i = 0; i < _pages.Count; i++)
                if (_pages[i] != null) _pages[i].gameObject.SetActive(i == (int)t);
            for (int i = 0; i < _tabBgs.Count; i++)
            {
                if (_tabBgs[i] == null) continue;
                // 选中的用 `_selected` 那张（原版 General 默认选中用的其实是 `_hover` —— 两态差得很少，我们用 selected）
                bool on = i == (int)t;
                _tabBgs[i].SetTexture(Tex(on ? ArtTabBgSel : ArtTabBg));
            }
            if (t == SettingsTab.Online) RefreshOnline();
            Debug.Log($"[Settings] 切到 `{t}` 页");
        }

        // ============================================================ 页 1：图像（原版 `Graphics Tab`）

        Transform BuildGraphicsPage(Transform area)
        {
            var page = Node(area, "Graphics Tab", TabsL, TabsT, TabsR, TabsB);
            PageTitle(page, "Graphics");

            // ① `Quality Selector`：下拉框（左） + 说明字（右）
            var row = Node(page, "Quality Selector", QualL, QualT, QualR, QualB);
            Rect(row, "Quality DropDown", QualL, QualT, QualBoxR, QualB, ArtButton, QContent);
            _qualityLabel = Text(row, "Quality Value", QualityName(), QualL + 20f, QualBoxR - 40f, QualT, QualB,
                                 FontLabel, Color.white, QText);
            var lb = Text(row, "Quality selector text", "Quality", QualTextL, QualR, QualT, QualB,
                          FontLabel, Color.white, QText);
            if (lb != null) AlignLeft(lb, new PxRect(QualTextL, QualT, QualR, QualB));
            Hit(row, "QualityHit", QualL, QualT, QualBoxR, QualB, QOverlay, CycleQuality);
            Debug.Log("[Settings] 图像页：`Text In Hand Selector` / `Small Screen Size` / `Auto Zoom` / "
                    + "`Hi FPS` / `super sampling` **没建**（我们这套 UI 没有对应功能 —— 不做假的开关）");

            // ② 勾选行（原版 `Scroll View` 里第 6、7 行）：Vsync 与 FPS 上限
            BuildCheckRow(page, "VSync", VsyncRow, () => QualitySettings.vSyncCount > 0, ToggleVsync);
            BuildCheckRow(page, "FPS limit", FpsRow, () => Application.targetFrameRate > 0, CycleFps,
                          () => "FPS limit: " + FpsText());
            return page;
        }

        Label _qualityLabel;

        void BuildCheckRow(Transform page, string nodeName, int row, Func<bool> state, Action onClick,
                           Func<string> labelText = null)
        {
            float t = ChkT + row * ChkRowStep, b = t + ChkRowH;
            var n = Node(page, nodeName, ChkL, t, ChkR, b);
            var box = Rect(n, "Toggle", ChkL, t, ChkL + 119f, b, state() ? ArtToggleOn : ArtToggleOff, QContent);
            var lb = Text(n, "Label", labelText != null ? labelText() : nodeName, ChkL + 130f, ChkR, t, b,
                          FontLabel, Color.white, QText);
            if (lb != null) AlignLeft(lb, new PxRect(ChkL + 130f, t, ChkR, b));
            Hit(n, "Hit", ChkL, t, ChkR, b, QOverlay, () =>
            {
                onClick();
                // 切完只换这一格的图与字（**别重建 quad** —— 重建会一层层叠上去，旧的那张还在）
                var tex = Tex(state() ? ArtToggleOn : ArtToggleOff);
                if (box != null && tex != null) box.SetTexture(tex);
                if (lb != null && labelText != null) lb.SetText(labelText());
            });
        }

        static string QualityName()
        {
            int lv = QualitySettings.GetQualityLevel();
            var names = QualitySettings.names;
            return (names != null && lv >= 0 && lv < names.Length) ? names[lv] : ("Level " + lv);
        }

        /// <summary>🔴 **自检的注入点**（产品路径下是 null ⇒ 走真 `QualitySettings`）。
        /// 为什么要它：`QualitySettings.SetQualityLevel` 在编辑器里会**把 `QualitySettings.asset` 写脏**
        /// （2026-09-26 实测：跑一次自检，`m_CurrentQuality` 被改了、文件被 Unity 重新序列化），
        /// 而**自检不该改工程设置**（同族规矩：自检不许动玩家的真存档）。</summary>
        public static Action<int> QualitySetterOverride;
        /// <summary>同上，给 `VSync` 那行用（`QualitySettings.vSyncCount` 也会写脏工程设置）。</summary>
        public static Action<int> VSyncSetterOverride;

        static void ApplyQuality(int level)
        {
            if (QualitySetterOverride != null) { QualitySetterOverride(level); return; }
            QualitySettings.SetQualityLevel(level, true);
        }
        static void ApplyVSync(int count)
        {
            if (VSyncSetterOverride != null) { VSyncSetterOverride(count); return; }
            QualitySettings.vSyncCount = count;
        }

        static string FpsText()
        {
            int f = Application.targetFrameRate;
            return f <= 0 ? "unlimited" : (f + " fps");
        }

        void CycleQuality()
        {
            var names = QualitySettings.names;
            if (names == null || names.Length == 0) { Debug.LogWarning("[Settings] 没有画质档可切"); return; }
            int next = (QualitySettings.GetQualityLevel() + 1) % names.Length;
            ApplyQuality(next);
            if (_qualityLabel != null) _qualityLabel.SetText(QualityName());
            _flash = "画质档 → " + QualityName();
            Debug.Log("[Settings] " + _flash);
        }

        void ToggleVsync()
        {
            int next = QualitySettings.vSyncCount > 0 ? 0 : 1;
            ApplyVSync(next);
            _flash = "VSync → " + (next > 0 ? "开" : "关");
            Debug.Log("[Settings] " + _flash);
        }

        void CycleFps()
        {
            int f = Application.targetFrameRate;
            Application.targetFrameRate = f == 60 ? 30 : (f == 30 ? -1 : 60);   // 60 → 30 → 无限 → 60
            _flash = "帧率上限 → " + FpsText();
            Debug.Log("[Settings] " + _flash);
        }

        // ============================================================ 页 2：音频（原版 `Media Tab`）

        Transform BuildAudioPage(Transform area)
        {
            var page = Node(area, "Media Tab", TabsL, TabsT, TabsR, TabsB);
            PageTitle(page, "Audio");

            var box = Node(page, "Audio Settings", AuL, AuT, AuR, AuB);
            var names = new[] { "Music", "Sound Effects", "Voice-overs" };
            var get = new Func<float>[] { () => WarpforgeAudio.Music, () => WarpforgeAudio.SoundFx, () => WarpforgeAudio.VoiceOver };
            var set = new Action<float>[] { WarpforgeAudio.SetMusic, WarpforgeAudio.SetSoundFx, WarpforgeAudio.SetVoiceOver };
            _audioSliders = new WfSlider[3];

            for (int i = 0; i < 3; i++)
            {
                float t = AuT + i * AuRowH, b = t + AuRowH;
                var rowN = Node(box, names[i] + " Container", AuL, t, AuR, b);
                // 标签在上、滑块在下（原版那三行就是这个摆法）
                var lb = Text(rowN, "Label", names[i], AuL, AuR, t, t + 62f, FontLabel, Color.white, QText);
                if (lb != null) AlignLeft(lb, new PxRect(AuL, t, AuR, t + 62f));
                // 🔴 滑块本体：`WfSlider`（**工程里唯一一份**滑块实现，交互/音频接线都在那儿）
                //    ⚠️ 轨道宽与中心都按 **0.9 烘过**的值给（`WfSlider` 也是按世界尺寸画的）
                var rs = Screen(AuL, t, AuR, b);
                float cx = (rs.x1 + rs.x2) * 0.5f;
                float cy = rs.y1 + 63.5f * RootScale;         // 行内局部 y = 57（轨道顶）+ 6.5（半高）
                var center = MainMenuSubmenuWindow.Local(rowN, cx, cy);
                int idx = i;
                _audioSliders[i] = WfSlider.Create(rowN, "vol" + i, center, get[i](), v => set[idx](v),
                                                   trackW: AuTrackW * RootScale);
            }

            var note = Text(page, "Note", "音量走 AudioMixer（与对局内设置面板同一套）", AuL, AuR, AuB + 20f, AuB + 60f,
                            FontSmall, new Color(1f, 1f, 1f, 0.6f), QText);
            if (note != null) AlignLeft(note, new PxRect(AuL, AuB + 20f, AuR, AuB + 60f));
            Debug.Log("[Settings] 音频页：原版这一页还有 `WindowMode Selector`（窗口模式）—— **没建**（不做假开关）");
            return page;
        }

        WfSlider[] _audioSliders;

        // ============================================================ 页 3：联机（**我们新增的设计**）

        Transform BuildOnlinePage(Transform area)
        {
            var page = Node(area, "Online Tab", TabsL, TabsT, TabsR, TabsB);
            PageTitle(page, "Online");

            // ① 角色：主机 / 客机（用户规格：「勾选成为主机或客机」）
            _roleHostBg = RoleButton(page, "Host", 0, NetRole.Host);
            _roleClientBg = RoleButton(page, "Client", 1, NetRole.Client);
            _role = (NetRole)Mathf.Clamp(NetConfig.Current.role, 1, 2);

            // ② 主机块 / 客机块（各自一对 IP+密码 + 自己的那个钮 —— 用户规格逐条）
            _hostBlock = BuildRoleBlock(page, NetRole.Host);
            _clientBlock = BuildRoleBlock(page, NetRole.Client);

            // ③ 状态行（**不许静默**：连接结果、掉线、重连都写在这里）
            _statusLabel = Text(page, "Status", "", TitleL, TabsR - 20f, OnStatusT, OnStatusT + 60f,
                                FontSmall, new Color(1f, 0.9f, 0.6f), QText);
            if (_statusLabel != null) AlignLeft(_statusLabel, new PxRect(TitleL, OnStatusT, TabsR - 20f, OnStatusT + 60f));

            // ④ 说明行 —— ⚠️ **这一页只剩 ~70px 高，六行塞不下** ⇒ 详细的三条路做成**点一下弹窗**。
            //    （用户 2026-09-26：「我们可能是网友需要联机」—— 这件事**必须在界面上说清楚**，
            //     光写一句「请做端口映射」等于没说。）
            var note = Text(page, "Note",
                            "这一页不是原版（原版是联网游戏，没有「当主机」这回事）。\n"
                            + "IP 直连 —— 公网怎么走：点这一行看",
                            TitleL, TabsR - 20f, OnStatusT + 70f, OnStatusT + 140f,
                            FontSmall, new Color(1f, 1f, 1f, 0.55f), QText);
            if (note != null) note.SetWrapWidth(LayoutSpace.Px(TabsR - 20f - TitleL));
            // 点这一行 ⇒ 弹「怎么联机」（三条路写清楚）
            var noteHit = Node(page, "Note Hit", TitleL, OnStatusT + 70f, TabsR - 20f, OnStatusT + 140f);
            Hit(noteHit, "Hit", TitleL, OnStatusT + 70f, TabsR - 20f, OnStatusT + 140f, QOverlay, ShowHowToConnect);

            ApplyRoleVisibility();
            return page;
        }

        ImageQuad RoleButton(Transform page, string label, int idx, NetRole role)
        {
            float x1 = TitleL + idx * (OnRoleW + OnRoleGap), x2 = x1 + OnRoleW;
            var n = Node(page, "Role " + label, x1, OnRoleT, x2, OnRoleB);
            var bg = Rect(n, "bg", x1, OnRoleT, x2, OnRoleB, ArtButton, QContent, BtnGreen);
            var lb = Text(n, "Text", label, x1, x2, OnRoleT, OnRoleB, FontButton, Color.black, QText);
            Hit(n, "Hit", x1, OnRoleT, x2, OnRoleB, QOverlay, () =>
            {
                _role = role;
                NetConfig.Current.role = (int)role;
                NetConfig.Save();
                ApplyRoleVisibility();
                Debug.Log($"[Settings] 联机角色 → {role}");
            });
            return bg;
        }

        Transform BuildRoleBlock(Transform page, NetRole role)
        {
            bool host = role == NetRole.Host;
            float x1 = TitleL, x2 = TitleL + OnFieldW;
            var blk = Node(page, host ? "Host Block" : "Client Block", TitleL, OnLabelT, TabsR, OnBtnT + OnBtnH);

            var ipLb = Text(blk, "IP Label", "IP address", x1, x2, OnLabelT, OnLabelT + 40f, FontLabel, Color.white, QText);
            if (ipLb != null) AlignLeft(ipLb, new PxRect(x1, OnLabelT, x2, OnLabelT + 40f));
            var pwdLb = Text(blk, "Password Label", "Password", x1, x2, OnPassLabelT, OnPassLabelT + 40f, FontLabel, Color.white, QText);
            if (pwdLb != null) AlignLeft(pwdLb, new PxRect(x1, OnPassLabelT, x2, OnPassLabelT + 40f));

            var login = NetConfig.Current;
            var ip = MenuInputField.Create(blk, "IP Field", new PxRect(x1, OnFieldT, x2, OnFieldT + OnFieldH),
                                           login.ip ?? "", "例如 192.168.1.10", ArtInputBg, QContent, QText);
            var pwd = MenuInputField.Create(blk, "Password Field",
                                            new PxRect(x1, OnPassT, x2, OnPassT + OnFieldH),
                                            login.password ?? "", "留空 = 不校验", ArtInputBg, QContent, QText);
            if (host) { _ipField = ip; _pwdField = pwd; }
            else { _ipField2 = ip; _pwdField2 = pwd; }

            if (host)
            {
                // 【刷新】：自动填本机 IP（用户规格：「IP 地址输入框右边有一个刷新按钮」）
                float fx1 = x2 + 20f, fx2 = fx1 + OnFillW;
                var rn = Node(blk, "Refresh", fx1, OnFieldT, fx2, OnFieldT + OnFieldH);
                Rect(rn, "bg", fx1, OnFieldT, fx2, OnFieldT + OnFieldH, ArtButton, QContent, BtnGrey);
                Text(rn, "Text", "Refresh", fx1, fx2, OnFieldT, OnFieldT + OnFieldH, FontSmall, Color.black, QText);
                Hit(rn, "Hit", fx1, OnFieldT, fx2, OnFieldT + OnFieldH, QOverlay, RefreshLocalIp);

                // 【保存】：记住角色/端口/密码并**开始监听**
                ActionButton(blk, "Save", OnBtnT, "Save", () =>
                {
                    var c = NetConfig.Current;
                    NetConfig.SaveAsHost(ip.Text, pwd.Text, c.port);
                    var sess = NetRuntime.Ensure()?.Reset();
                    if (sess != null && sess.StartHost(NetConfig.Current))
                        // 🔴 **把要交给朋友的那串东西直接印出来** —— 主机就这一个任务，
                        //    别让玩家自己去拼 IP 和端口（用户 2026-09-26：「主机点按钮自动填 IP，然后写密码」）
                        _flash = "✅ 主机已就绪，等着对面连进来。\n"
                               + "把这行给朋友 → " + ip.Text + " : " + NetConfig.Current.port
                               + (string.IsNullOrEmpty(pwd.Text) ? "（没设密码）" : "");
                    else _flash = "主机没起来：" + (sess != null ? sess.LastError : "NetRuntime 不在");
                    FlashAndLog();
                });
            }
            else
            {
                // 【检查连接】：点了**自动保存** IP+密码、试连、反馈成败（用户规格逐条）
                ActionButton(blk, "Check", OnBtnT, "Check Connection", () =>
                {
                    var c = NetConfig.Current;
                    NetConfig.SaveAsClient(ip.Text, pwd.Text, c.port);
                    var sess = NetRuntime.Ensure()?.Reset();
                    if (sess == null) { _flash = "NetRuntime 不在（自检里要自己建）"; FlashAndLog(); return; }
                    sess.OnCheckDone = (ok, why) => { _flash = (ok ? "✅ " : "❌ ") + why; FlashAndLog(); };
                    sess.CheckConnection(NetConfig.Current);
                    _flash = sess.StatusText;
                    FlashAndLog();
                });
            }
            return blk;
        }

        MenuInputField _ipField2, _pwdField2;

        void ActionButton(Transform page, string name, float t, string label, Action onClick)
        {
            float x1 = TitleL, x2 = TitleL + OnBtnW, y2 = t + OnBtnH;
            var n = Node(page, name + " Button", x1, t, x2, y2);
            Rect(n, "bg", x1, t, x2, y2, ArtButton, QContent, BtnGreen);
            Text(n, "Text", label, x1, x2, t, y2, FontButton, Color.black, QText);
            Hit(n, "Hit", x1, t, x2, y2, QOverlay, () => { Debug.Log($"[Settings] 点了 `{label}`"); onClick(); });
        }

        int _addrIdx = -1;   // 【刷新】在多网卡之间循环：每点一次换下一个候选

        /// <summary>【刷新】：**每点一次换一个候选地址**。
        /// 🔴 **为什么不做「一次填对」**：多网卡（有线 + 无线 + VPN + 虚拟机）时**机器自己挑不准**，
        ///    而**挑错的代价是朋友连不上**。⇒ 改成**可循环**：flash 里报「第 k/n 个 · 网卡名」，
        ///    玩家一眼就认出哪块是自己的网卡，多点几下就行（输入框也**始终可手改**）。
        /// 📌 2026-09-26 之前这里走 `Dns.GetHostAddresses`，**在批处理里直接抛异常、填不出来**
        ///    —— 已改成枚举网卡（见 `NetConfig.LocalAddresses`）。</summary>
        void RefreshLocalIp()
        {
            // ⚠️ **只在这堆「能用的」里循环** —— `LocalAddresses()` 会把回环（`127.0.0.1` / `::1`）
            //    和 v6 链路本地（`fe80::`）也列出来，那些**填给对面等于没填**，不该出现在循环里。
            var all = NetConfig.LocalAddresses().FindAll(x => x.Usable);
            if (all.Count == 0)
            {
                if (_ipField != null) _ipField.SetText("127.0.0.1");
                _flash = "⚠️ 一块可用网卡都没找到 —— 只能手填地址";
                FlashAndLog();
                return;
            }
            _addrIdx = (_addrIdx + 1) % all.Count;
            var a = all[_addrIdx];
            if (_ipField != null) _ipField.SetText(a.addr);
            _flash = $"本机地址 {_addrIdx + 1}/{all.Count}：{a.Label}"
                   + (a.isV6 ? "（IPv6）" : "")
                   + (all.Count > 1 ? "　—— 再点一下换下一个" : "")
                   + (a.isVirtual
                        ? "\n⚠️ 这是「虚拟网卡」的地址（VPN / 虚拟局域网工具建的那张）。"
                        + "\n　 对面也装了同一个工具的话，直接用这个 —— 穿透由那个工具负责。"
                        : "");
            FlashAndLog();
        }

        /// <summary>「怎么联机」——**三条路写清楚**（用户 2026-09-26：「我们可能是网友需要联机」）。
        /// ⚠️ 我们**不做 NAT 穿透**，所以「公网直连」这一条**必须**借一个外部条件
        /// （公网 IPv6 / 端口映射 / 虚拟局域网工具）。**这不是偷懒** —— 打洞的第一步就要有一台
        /// **公网会合点**，而本项目**没有任何服务器**（判据 → `资料/联机P2P_设计与交接.md` §二·4）。</summary>
        void ShowHowToConnect()
        {
            // 🆕 2026-09-26：**顺带自动识别本机有没有装虚拟局域网工具**
            //    （装了就一定会有那张虚拟网卡 ⇒ 靠它认，不用问玩家）
            string vName = null;
            foreach (var a in NetConfig.LocalAddresses())
                if (a.Usable && a.isVirtual) { vName = a.nic; break; }
            // 🆕 以及**本机有没有公网 IPv6** —— 用户 2026-09-26 定了走「IPv6 直连」那条路，
            //    那他就得一眼看出自己这台够不够条件（不用去命令行敲 ping -6）。
            var v6 = NetConfig.AllLocalIPv6();

            string t =
                "三条路，从最省事开始：\n"
              + "① 同一个局域网 ⇒ 直接填主机那台机器的地址。\n"
              + "② 不在一起 ⇒ 两边装同一个虚拟局域网工具\n"
              + "　（Tailscale / ZeroTier / 蒲公英 之类），填它给的地址。\n"
              + "③ 公网直连 ⇒ 主机有公网 IPv6 就直接填 IPv6 地址；\n"
              + "　否则要在路由器上做端口映射（⚠️ 很多宽带没有公网 IP）。\n\n"
              + "我们不做 NAT 穿透 —— 三条都不走的话，公网连不上。\n\n"
              + "本机检测：\n"
              + "· 公网 IPv6：" + (v6.Length > 0
                    ? "有（" + v6[0] + "）\n  第 ③ 条路能用 —— 只要路由器放行那个 TCP 端口"
                    : "没有 ⇒ 第 ③ 条走不了（很多宽带就是这样），只能走 ① 或 ②")
              + "\n· 虚拟局域网工具：" + (vName != null
                    ? "装了（网卡「" + vName + "」）\n  点【刷新】能切到它给的地址"
                    : "没检测到（想走 ② 就两边各装一个，Tailscale / ZeroTier 都免费）");

            var wm = WindowsManager.Instance;
            if (wm != null) { wm.ShowPopUp(t, "知道了", null); Debug.Log("[Settings] 弹了「怎么联机」"); }
            else Debug.LogWarning("[Settings] 没有 WindowsManager，弹不出「怎么联机」：\n" + t);
        }

        void ApplyRoleVisibility()
        {
            bool host = _role == NetRole.Host;
            if (_hostBlock != null) _hostBlock.gameObject.SetActive(host);
            if (_clientBlock != null) _clientBlock.gameObject.SetActive(!host);
            Debug.Log($"[Settings] 联机页显示的是「{_role}」那一块（{(_role == NetRole.Host ? "IP + 密码 + 保存 + 刷新" : "IP + 密码 + 检查连接")}）");
        }

        void RefreshOnline()
        {
            var sess = NetRuntime.Instance != null ? NetRuntime.Instance.Session : null;
            string s = sess != null ? sess.StatusText : "（会话还没建 —— 点一下 Host 的保存，或 Client 的检查连接）";
            if (_statusLabel != null) _statusLabel.SetText((_flash != null ? _flash + "\n" : "") + s);
        }

        void FlashAndLog() { RefreshOnline(); Debug.Log("[Settings] " + _flash); }

        // ============================================================ 每帧

        void Update()
        {
            // ⚠️ 批处理下 `Update` 不跑（自检自己 `Pump`）—— 这里只服务真 Play。
            if (Current != SettingsTab.Online || _statusLabel == null) return;
            RefreshOnline();
        }

        // ============================================================ 绘图小工具（本窗自用）
        //
        // ⚠️ 不复用 `MainMenuSubmenuWindow` 的那套：那套是**子菜单窗**（`Content Area` 在屏幕左上、
        //    左栏 165 宽贴着屏幕边）；本窗是**居中弹窗**、左栏在弹窗里（178.42 宽）。
        //    两套的矩形语义不同 ⇒ 复用得先把两边都参数化，反而更容易错。判据都在这一个文件里。

        static Transform Node(Transform parent, string name, float x1, float y1, float x2, float y2)
        {
            var s = Screen(x1, y1, x2, y2);
            return MenuDraw.Node(parent, name, new PxRect(s.x1, s.y1, s.x2, s.y2));
        }
        ImageQuad Rect(Transform p, string n, float x1, float y1, float x2, float y2, string art, int q,
                       Color? tint = null)
        {
            var s = Screen(x1, y1, x2, y2);
            return MenuDraw.Rect(p, Tex(art), new PxRect(s.x1, s.y1, s.x2, s.y2), n, q, tint);
        }
        ImageQuad Nine(Transform p, string n, float x1, float y1, float x2, float y2, string art,
                       float tw, float th, Vector4 border, int q, Color? tint)
        {
            var t = Tex(art);
            if (t == null) return null;
            var s = Screen(x1, y1, x2, y2);
            var go = MenuDraw.Nine(p, t, new PxRect(s.x1, s.y1, s.x2, s.y2), border, tw, th, q, tint, true, n);
            return go != null ? go.GetComponentInChildren<ImageQuad>() : null;
        }
        void Tiled(Transform p, string n, float x1, float y1, float x2, float y2, string art, float tile, int q)
        {
            var s = Screen(x1, y1, x2, y2);
            MenuDraw.Tiled(p, Tex(art), new PxRect(s.x1, s.y1, s.x2, s.y2), tile, q, n, null);
        }
        Label Text(Transform p, string n, string s0, float x1, float x2, float y1, float y2, float fs, Color c, int q)
        {
            var s = Screen(x1, y1, x2, y2);
            return MenuDraw.Text(p, new PxRect(s.x1, s.y1, s.x2, s.y2), s0, c, n, fs, q);
        }
        Transform Hit(Transform p, string n, float x1, float y1, float x2, float y2, int q, Action a)
        {
            var s = Screen(x1, y1, x2, y2);
            return MenuDraw.Hit(p, n, new PxRect(s.x1, s.y1, s.x2, s.y2), q, a);
        }
        /// <summary>左对齐到**原版（未缩放）矩形**的左边缘 —— 内部过 `Screen()`。</summary>
        static void AlignLeft(Label lb, PxRect r)
        {
            var s = Screen(r.x1, r.y1, r.x2, r.y2);
            MenuDraw.AlignLeft(lb, new PxRect(s.x1, s.y1, s.x2, s.y2));
        }
        void Solid(Transform p, string n, float cx, float cy, float w, float h, Color c, int q)
        {
            var s = Screen(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);
            MenuDraw.Rect(p, CardArt.Solid(), new PxRect(s.x1, s.y1, s.x2, s.y2), n, q, c);
        }
        void PageTitle(Transform page, string title)
        {
            var lb = Text(page, "Tab Title", title, TitleL, TitleR, TitleT, TitleB, PageTitleFontPx, Color.white, QText);
            if (lb != null) AlignLeft(lb, new PxRect(TitleL, TitleT, TitleR, TitleB));
        }
        Texture2D Tex(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name)) { MissingArt.Add(name); Debug.LogWarning($"[Settings] 图缺了：`{name}`"); }
            return t;
        }
    }

    /// <summary>
    /// 设置窗里的一个**输入框**（原版是 `TMP_InputField`；我们这套没有 uGUI 事件）。
    /// 🔴 **键盘那一层不自己写** —— 走外壳**唯一**那份文本焦点：`PointerLayer.BeginText/TypeChar/EndText`
    ///    （`Shell/PointerLayer.cs`，注释里写明「这份是给外壳/菜单用的」）。两处各写一份 = 迟早不一致。
    /// </summary>
    public class MenuInputField
    {
        public string Text { get; private set; }
        public bool Focused { get { return PointerLayer.Instance != null && PointerLayer.Instance.TextEditing && _mine; } }

        Label _label;
        string _placeholder;
        Transform _root;
        ImageQuad _bg;
        bool _mine;

        public static MenuInputField Create(Transform parent, string name, PxRect r, string initial,
                                            string placeholder, string art, int q, int qText)
        {
            var f = new MenuInputField();
            f.Text = initial ?? "";
            f._placeholder = placeholder;
            f._root = MenuDraw.Node(parent, name, r);
            var tex = CardArt.MenuUi(art);
            f._bg = MenuDraw.Rect(f._root, tex, r, "bg", q);
            f._label = MenuDraw.Text(f._root, new PxRect(r.x1 + 16f, r.y1, r.x2 - 16f, r.y2),
                                     f.Show(), Color.white, "Text", 40f, qText);
            MenuDraw.AlignLeft(f._label, new PxRect(r.x1 + 16f, r.y1, r.x2 - 16f, r.y2));
            MenuDraw.Hit(f._root, "Hit", r, q + 2, () => f.Focus());
            return f;
        }

        string Show() { return string.IsNullOrEmpty(Text) ? (_placeholder ?? "") : Text; }

        void Refresh()
        {
            if (_label == null) return;
            _label.SetText(Focused ? (Text + "_") : Show());
            _label.SetColor(string.IsNullOrEmpty(Text) ? new Color(1f, 1f, 1f, 0.45f) : Color.white);
        }

        public void Focus()
        {
            var pl = PointerLayer.Instance;
            if (pl == null) { Debug.LogWarning("[Settings] 没有 `PointerLayer` ⇒ 输入框收不到键盘"); return; }
            _mine = true;
            pl.BeginText(Text, 64, s => { Text = s; _mine = false; Refresh(); },
                         () => { _mine = false; Refresh(); },
                         s => { Text = s; Refresh(); });
            Refresh();
            Debug.Log("[Settings] 输入框获得焦点（回车确认 / ESC 取消）");
        }

        /// <summary>程序化改文本（【刷新】按钮、自检都用它 —— 批处理里没有键盘）。</summary>
        public void SetText(string s) { Text = s ?? ""; Refresh(); }
    }
}
