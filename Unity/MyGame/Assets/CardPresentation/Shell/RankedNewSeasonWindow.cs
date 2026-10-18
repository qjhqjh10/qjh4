// RankedNewSeasonWindow.cs — 原版 **`RankedEventStartSeasonWindow`**
// （prefab 根 `Ranked New Season WIndow` · 排位新赛季开赛窗：NEW RANKED SEASON + 倒计时）
//
// ============================ 出处（唯一判据，全部本地现读）============================
// · prefab 树 / 矩形 / 组件 / 文字档位（2026-10-17 现读，可复跑）：
//     `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Ranked New Season WIndow" --depth 9`
//   根 = `bundle_menus_assets_all/GameObject/Ranked New Season WIndow.json`，
//   根 MB = `MonoBehaviour_7393637103769976266.json`（窗参逐字段实读）：
//     `type = 1`(Popup) · `windowsPlacement = 15`(Popup) · `closeOnESC = 1` · `updateNavPanel = 0` ·
//     `extraScaleSmallScreen = 1.0` · `openSound` / `closeSound` = 空 PPtr · `useDefaultCloseSoundIfNull = 1`
//   ⇒ 关窗音走家族默认（`GameWindow.Close()` 的既有实现），本窗**不赋任何 clip**。
//   字段 PPtr 实读：`backgroundCloseButton.window = 0`（**点暗底不关窗**）· `timeToEnd` → `Timer` 那颗 TMP。
// · 行为（第一权威 = 反编译 `d:/2/tools/decomp_full/RankedEventStartSeasonWindow__Initialize.c`）：
//   `timeToEnd - PlayerDataManager.CurrentServerDateTime()` → `SupportMethods.GetTimeString(span, 1)` →
//   `string.Format(I2("Ends in: {0}" 那一族), timeString)` → 写进 `timeToEnd.setText(...)`。
//   ⇒ **截止时间是服务端数据**（本地没有）⇒ 出厂显示 `--` + 出声一次，⛔ 不编一个日期。
//   `OnEnable` / `OnDisable` 只是给 `backgroundCloseButton.onClick` 挂/摘监听（那颗的 `window` PPtr 是 0，
//   ⇒ 原版那一刻点暗底也**不**关窗 —— 与 `backgroundCloseButton.window == 0` 自洽）。
//
// ============================ 我们侧的对位（逐件写清，不冒充原版）============================
// · **文案**：标题走 `Localize.mTerm = MainMenu/RankedWindow/NewRankedSeason`；倒计时走
//   `Event_Description/General/EndsInPlusTime`（= `Ends in: {0}`，官方 13 语言表里逐字有）。
//   取词条统一走 `TutorialMessagePopup.OfficialTerm`（全工程**唯一一份**取法，⛔ 别抄第二份）。
// · **倒计时**：`Initialize(DateTime? endUtc)` 的入口；`null` ⇒ `--`。给了时刻就按
//   「`endUtc - DateTime.UtcNow`」算 `Xd Yh`（原版 `SupportMethods.GetTimeString` 的格式；
//   ⚠️ 原版是拿**服务器时间**算的，我们只有本机时钟 —— 如实标注，别声称等价）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `RankedEventStartSeasonWindow`（`GameWindow` 子类）——
    /// **新排位赛季开始窗**：`NEW RANKED SEASON` + 剩余时间。
    /// <para>🔴 **数据缺口（如实记，⛔ 不编）**：赛季结束时刻来自服务端 ⇒ 出厂显示 `--`。</para>
    /// <para>🔴 **入口缺口**：原版由排位事件链开，我们这一侧没有那个状态机 ⇒ 本窗**建好并注册**，
    /// 入口由调用方自己定（与 T18 同口径）。</para></summary>
    public class RankedNewSeasonWindow : GameWindow
    {
        // ============================================================ 队列档（自成一档 · 3420–3429）
        // ⚠️ 层带不许与别的窗重叠（现扫 `Shell/*.cs` 的 `const int Q*`；相邻带 = 3410–3419 / 3430–3439）。
        public const int QShade = 3420,      // `Back Background`
                         QTitle = 3421,      // `Title`
                         QTimer = 3422,      // `Timer` 容器 + 图标 + 文字
                         QIcon = 3423;
        public const int QHit = 3429;

        // ============================================================ 几何（dump 现读 · 1920×1080 画布 px）
        public const string RootName = "Ranked New Season WIndow";   // ⚠️ 原版拼写就是 `WIndow`（大写 I）

        public static readonly PxRect BackR = new PxRect(-761.30f, -427.10f, 2681.30f, 1507.10f);   // 3442.55×1934.17
        public static readonly PxRect TitleR = new PxRect(53.20f, 0f, 1866.80f, 207.69f);           // 1813.58×207.69
        public static readonly PxRect TimerR = new PxRect(211.50f, 977.00f, 1708.50f, 1080.00f);    // 1496.98×102.96
        public static readonly PxRect TimerIconR = new PxRect(920.20f, 996.20f, 984.70f, 1060.80f); // 64.555²
        /// <summary>`Timer` 文字：CSF 把宽度收成「文本自身宽」（prefab `sizeDelta.x = 0`），
        /// 布局组居中放置后**左沿 = 999.8**。我们按「左沿 999.8、右沿取容器右沿」建，再 `AlignLeft` ——
        /// 逐像素等价于「自身宽 + 居中」的那一档（字的左沿落在同一处）。</summary>
        public static readonly PxRect TimerTextR = new PxRect(999.80f, 977.00f, 1708.50f, 1080.00f);

        public static readonly Color ShadeTint = new Color(0f, 0f, 0f, 0.7725490f);

        public const string ArtClock = "WF_icon_clock";

        // 文字档位（prefab 逐颗实读）
        /// <summary>🔴 **2026-10-18（W3）就地订正（铁律 5）**：MB `-517081153138623030` 实读
        /// `m_fontSize = 87.01` · `m_fontSizeBase = 87.01` · min **18** · max **72** · **`m_enableAutoSizing = 0`**
        /// ⇒ 原版**渲染 87.01**（min/max 是**死字段**，不参与）。
        /// ⚠️ 我们原来照 min/max 传了自适应（`autoMinPx = TitleFontMin`）⇒ `SetAutoFitBox` 把字号**夹到 ≤72**
        /// （标题小 17%）。判据 → `资料/普查产出_1018/P2_弹窗排版_通用活动奖励.md` §11。</summary>
        public const float TitleFont = 87.01f, TitleFontMin = 18f, TitleFontMax = 72f, TitleFontBase = 87.01f;
        public const float TimerFont = 50f, TimerFontMin = 18f, TimerFontMax = 72f, TimerFontBase = 50f;

        // 官方词条
        public const string TitleTerm = "MainMenu/RankedWindow/NewRankedSeason";
        public const string TimerTerm = "Event_Description/General/EndsInPlusTime";

        // 兜底原文（官方 13 语言表取不到时用）
        const string TitleEn = "NEW RANKED SEASON", TitleZh = "新的排位赛季";
        const string TimerEn = "Ends in: {0}", TimerZh = "剩余时间：{0}";

        // ============================================================ 数据
        public struct Context
        {
            /// <summary>赛季结束的 UTC 时刻（原版 `timeToEnd` 那一格由服务端给）；null ⇒ 显示 `--`。</summary>
            public System.DateTime? EndUtc;
        }
        public static Context DefContext() { return new Context { EndUtc = null }; }
        static Context FillDef(Context? src) { return src.HasValue ? src.Value : DefContext(); }

        public static RankedNewSeasonWindow LastOpened { get; private set; }

        Context _ctx = DefContext();
        public Context Ctx { get { return _ctx; } }

        Transform _back, _title, _timer, _timerIcon;
        Label _titleLabel, _timerLabel;
        public readonly List<string> MissingArt = new List<string>();

        // ============================================================ 建窗
        public static RankedNewSeasonWindow Create(WindowsManager mgr, Context? ctx = null)
        {
            var go = new GameObject(RootName);
            var win = go.AddComponent<RankedNewSeasonWindow>();
            win.type = WindowType.Popup;                  // 实读 type = 1
            win.placement = WindowsPlacement.Popup;       // 实读 windowsPlacement = 15
            win.closeOnEsc = true;                        // 实读 closeOnESC = 1
            win.updateNavPanel = false;                   // 实读 updateNavPanel = 0
            win.extraScaleSmallScreen = 1f;               // 实读 1.0（= 不覆盖）
            win.Manager = mgr;
            win._ctx = FillDef(ctx);
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[RankedNewSeason] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            Build();
        }

        public void Show(Context? ctx = null)
        {
            _ctx = FillDef(ctx);
            Build();
        }

        /// <summary>原版 `RankedEventStartSeasonWindow.Initialize(DateTime timeToEnd)` —— 只刷新倒计时那一颗。</summary>
        public void Initialize(System.DateTime? endUtc)
        {
            _ctx.EndUtc = endUtc;
            if (_timerLabel != null) _timerLabel.SetText(TimerText());
        }

        // ============================================================ 建整棵树（顺序 = 原版兄弟序）
        public void Build()
        {
            PointerLayer.UnregisterOwnedBy(gameObject);
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();
            _back = _title = _timer = _timerIcon = null;
            _titleLabel = _timerLabel = null;

            // ---- 1) `Back Background`（`BackgroundCloseButton.window = 0` ⇒ 点暗底**不**关窗）----
            _back = MenuDraw.Node(transform, "Back Background", BackR);
            MenuDraw.Rect(_back, CardArt.Solid(), BackR, "Image", QShade, ShadeTint);

            // ---- 2) `Title`：fs 87.01 · base 87.01 · **AUTO=0**（min18/max72 是死字段）· Center/Middle ----
            // 🔴 `autoMinPx` 传 **0** ⇒ **不开自适应**（`MenuDraw.TextCore` 的闸 = `autoMinPx > 0 && fontPx > autoMinPx`），
            //    只保留 `wrapPx` = 框宽（原版 `m_TextWrappingMode = 1`）⇒ 字号恒 **87.01**（= 原版渲染值）。
            //    ⛔ 别把 auto 三格改成 `[0, 72, base]` 那种半套 —— `autoMinPx = 0` 就够，后两格是死实参。
            _title = MenuDraw.Node(transform, "Title", TitleR);
            _titleLabel = MenuDraw.Text(_title, TitleR, TutorialMessagePopup.OfficialTerm(TitleTerm, TitleEn, TitleZh),
                                        Color.white, "EverguildTextMeshPro", TitleFont, QTitle, TitleR.W,
                                        face: TmpFont.Face.LatinCardBody);

            // ---- 3) `Timer`（HorizontalLayoutGroup spacing 15.1 · MiddleCenter）----
            _timer = MenuDraw.Node(transform, "Timer", TimerR);
            _timerIcon = MenuDraw.Node(_timer, "Timer Icon", TimerIconR);
            MenuDraw.Rect(_timerIcon, Tex(ArtClock, "`Timer Icon` 的时钟图"), TimerIconR, "Image", QIcon, null, true);
            // `Timer` 文字：fs 50 · base 50 · auto[18~72] · 白；左沿 = 布局组算出的 999.8
            _timerLabel = MenuDraw.Text(_timer, TimerTextR, TimerText(), Color.white, "Timer",
                                        TimerFont, QTimer, TimerTextR.W, TimerFontMin, TimerFontMax, TimerFontBase, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(_timerLabel, TimerTextR);

            if (MissingArt.Count > 0)
                Debug.LogWarning("[RankedNewSeason] ⚠️ 有 " + MissingArt.Count + " 张图取不到："
                                 + string.Join("、", MissingArt.ToArray()));
        }

        /// <summary>倒计时那一段文案。原版 = `string.Format(I2(EndsInPlusTime), SupportMethods.GetTimeString(span))`。
        /// 🔴 截止时刻是服务端数据；没有 ⇒ `--` + 出声一次。⚠️ **本机时钟** ≠ 服务器时间（如实标注）。</summary>
        string TimerText()
        {
            var fmt = TutorialMessagePopup.OfficialTerm(TimerTerm, TimerEn, TimerZh);
            if (!_ctx.EndUtc.HasValue)
            {
                NoteOnce(ref _warnedNoTime,
                         "[RankedNewSeason] `Timer` **没有赛季结束时刻** —— 原版 = `timeToEnd - PlayerDataManager."
                       + "CurrentServerDateTime()`（服务端数据，本机没有）⇒ 显示 `--`，⛔ 不编日期。");
                return string.Format(fmt, "--");
            }
            var span = _ctx.EndUtc.Value - System.DateTime.UtcNow;
            if (span.TotalSeconds < 0) span = System.TimeSpan.Zero;
            return string.Format(fmt, span.Days + "d " + span.Hours + "h");   // = `GetTimeString` 的 `Xd Yh` 形状
        }
        static bool _warnedNoTime;

        // ============================================================ 取图 / 出声（不静默）
        Texture2D Tex(string name, string what)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name))
            {
                MissingArt.Add(name);
                Debug.LogWarning("[RankedNewSeason] 图取不到：`" + name + "`（" + what + "）⇒ **这一件没画**"
                               + "（`MenuDraw.Rect/Nine` 对 `tex == null` 是静默返回 null）。"
                               + "导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            }
            return t;
        }
        static void NoteOnce(ref bool flag, string msg)
        {
            if (flag) return;
            flag = true;
            Debug.LogWarning(msg);
        }

        // ============================================================ 自检读口
        public Transform BackNode { get { return _back; } }
        public Transform TitleNode { get { return _title; } }
        public Transform TimerNode { get { return _timer; } }
        public Transform TimerIconNode { get { return _timerIcon; } }
        public Label TimerLabel { get { return _timerLabel; } }

        public Label TitleLabel { get { return _titleLabel; } }

        public string DebugDump()
        {
            return "title=\"" + (_titleLabel != null ? _titleLabel.Text : "-") + "\""
                 + " timer=\"" + (_timerLabel != null ? _timerLabel.Text : "-") + "\""
                 + " end=" + (_ctx.EndUtc.HasValue ? _ctx.EndUtc.Value.ToString("u") : "<none>");
        }
    }
}
