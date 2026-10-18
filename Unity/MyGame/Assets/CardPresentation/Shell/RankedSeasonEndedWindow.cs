// RankedSeasonEndedWindow.cs — 原版 **`RankedEventSeasonEndedWindow`**
// （prefab 根 `Ranked Season Ended Window` · 排位赛季结束窗：SEASON ENDED! + 段位图 + 名次/百分比 + 评分 + 点击继续）
//
// ============================ 出处（唯一判据，全部本地现读）============================
// · prefab 树 / 矩形 / 组件 / 文字档位（2026-10-17 现读，可复跑）：
//     `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Ranked Season Ended Window" --depth 9`
//   根 = `bundle_menus_assets_all/GameObject/Ranked Season Ended Window.json`，
//   根 MB = `MonoBehaviour_-3630938164723221848.json`（窗参逐字段实读）：
//     🔴 **`type = 0`(Fullscreen)**（三分窗里只有它是全屏档）· `windowsPlacement = 15`(Popup) ·
//     `closeOnESC = 1` · `updateNavPanel = 0` · `extraScaleSmallScreen = 1.0` ·
//     `openSound` / `closeSound` = 空 PPtr · `useDefaultCloseSoundIfNull = 1`。
//   另有 `canvasGroup`（`CanvasGroup_-3969323529196569944.json`：`m_Alpha = 1` / 可交互 / 挡射线）。
//   字段 PPtr 实读：`backgroundCloseButton.window = 0`（**点暗底不关窗**）· `divisionIcon` / `rankIcon`
//   的 `Image.m_Sprite = 0`（服务端喂）· `gameModeIcon` = `40k_UI_icon_ranked_Skirmish`。
// · 行为（第一权威 = 反编译 `d:/2/tools/decomp_full/RankedEventSeasonEndedWindow__Open.c`）：
//   `GameWindow.Open()` → 取当前赛季统计 → 段位图/段位图标（`RankedDivisions.GetDivisionData/GetRankData`）；
//   `score`（= `Ranking Points Quantity Text`）← 赛季评分的**整数**；
//   `positionOrPercentageText` ← `I2(EndFinishPercentage)` 或 `I2(EndFinishScore)`（两种，按有没有进前 1000）；
//   `canvasGroup.DOFade(...).From(...)`（淡入）。⇒ **段位 / 名次 / 评分全在服务端** ⇒ 留空/`--` + 出声，
//   ⛔ 不编数字（判据口径同 `RankedDivisionInfo.cs` 文件头 ⑤）。
//
// ============================ 我们侧的对位（逐件写清，不冒充原版）============================
// · **文案**：标题 / `Rating:` / `Tap To Continue` 走 prefab 的 `Localize.mTerm`；
//   名次那一颗原版是 `EndFinishPercentage`（百分位）或 `EndFinishScore`（名次）二选一 ——
//   两个词条都在官方 13 语言表里，取词条统一走 `TutorialMessagePopup.OfficialTerm`（唯一一份取法）。
// · **段位大图 / Rank Icon**：`m_Sprite = 0` ⇒ 节点照建、**不画** + 出声（同 DivisionChange 那一档）。
// · **`Glow below`**：原版是 `ParticleSystem`（scale 250）⇒ 归特效线，本窗只留节点 + 出声一次。
// · **`canvasGroup`**：照 prefab `AddComponent<CanvasGroup>`（`m_Alpha = 1`；原版 `Open()` 的
//   `DOFade` 是动画，本窗不播动画 —— 如实标注）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `RankedEventSeasonEndedWindow`（`GameWindow` 子类）——
    /// **排位赛季结束窗**：`SEASON ENDED!` + 段位图 + 名次百分比 + 评分 + 点击继续。
    /// <para>🔴 **窗参里唯一一个 `type = 0`（Fullscreen）** —— 三分窗中的特例，别照另两扇抄成 Popup。</para>
    /// <para>🔴 **数据缺口（如实记，⛔ 不编）**：段位、名次/百分比、评分全部来自服务端。</para>
    /// <para>🔴 **入口缺口**：原版由排位事件链在赛季结算后开 ⇒ 本窗**建好并注册**，入口由调用方自己定。</para></summary>
    public class RankedSeasonEndedWindow : GameWindow
    {
        // ============================================================ 队列档（自成一档 · 3430–3439）
        // ⚠️ 层带不许与别的窗重叠（现扫 `Shell/*.cs` 的 `const int Q*`；相邻带 = 3420–3429 / 3440–3449）。
        public const int QShade = 3430,      // `Menu Dark Background`
                         QDivision = 3431,   // `Division Image`
                         QRank = 3432,       // `Division Image/Rank Icon`
                         QTitle = 3433,      // `Title`
                         QText = 3434,       // `Percentage Or Score text` / `Tap To Continue` / `Rank Points`
                         QLine = 3435;       // `Rank Points` 三件
        public const int QHit = 3439;

        // ============================================================ 几何（dump 现读 · 1920×1080 画布 px）
        public const string RootName = "Ranked Season Ended Window";

        public static readonly PxRect DarkR = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);
        public static readonly PxRect DivisionR = new PxRect(582.50f, 266.10f, 1337.50f, 1021.10f);   // 754.958²
        /// <summary>`Rank Icon` 的**视觉**框：布局 67.418² × `localScale 1.090329` ⇒ 73.51²、中心不变
        /// （960.6, 526.7）。🔴 按视觉框建、`localScale` 保持 1（理由 → `RankedDivisionChangeWindow.RankIconR`）。</summary>
        public static readonly PxRect RankIconR = new PxRect(923.845f, 489.945f, 997.355f, 563.455f);
        public static readonly PxRect TitleR = new PxRect(53.20f, 0f, 1866.80f, 111.55f);
        public static readonly PxRect PercentR = new PxRect(-0.10f, 200.20f, 1920.10f, 266.10f);
        public static readonly PxRect TapR = new PxRect(-0.10f, 985.50f, 1920.10f, 1080.00f);
        /// <summary>1444.308（dump 的「宽」列；左沿 237.8 ⇒ 右沿 = 1682.108，⛔ 别按 1 位小数写成 1682.2
        /// —— 那样宽度会多 0.09px，自检那条 1444.31 就红）。</summary>
        public static readonly PxRect RankPointsR = new PxRect(237.80f, 100.20f, 1682.108f, 200.20f);
        public static readonly PxRect PointsTitleR = new PxRect(797.90f, 110.50f, 917.30f, 189.80f);  // 119.37×79.335
        public static readonly PxRect GameModeIconR = new PxRect(917.30f, 100.20f, 1017.30f, 200.20f); // 100²
        public static readonly PxRect PointsQtyR = new PxRect(1017.30f, 110.50f, 1122.10f, 189.80f);   // 104.77×79.335
        /// <summary>`Glow below`：布局 100² × `localScale 250`（粒子件，本窗只留节点）。</summary>
        public static readonly PxRect GlowR = new PxRect(909.00f, 543.00f, 1009.00f, 643.00f);

        public static readonly Color ShadeTint = new Color(0f, 0f, 0f, 0.7725490f);

        public const string ArtGameModeIcon = "40k_UI_icon_ranked_Skirmish";

        // 文字档位（prefab 逐颗实读）
        /// <summary>🔴 **2026-10-18（W3）就地订正（铁律 5）**：MB `999434982901775016` 实读
        /// `m_fontSize = 87.01` · `m_fontSizeBase = 87.01` · min **18** · max **72** · **`m_enableAutoSizing = 0`**
        /// ⇒ 原版**渲染 87.01**（min/max 是**死字段**，不参与）。
        /// ⚠️ 我们原来照 min/max 传了自适应 ⇒ `SetAutoFitBox` 把大字标题**夹到 ≤72**。
        /// 判据 → `资料/普查产出_1018/P2_弹窗排版_通用活动奖励.md` §12。</summary>
        public const float TitleFont = 87.01f, TitleFontMin = 18f, TitleFontMax = 72f, TitleFontBase = 87.01f;
        public const float PercentFont = 50f, PercentFontMin = 18f, PercentFontMax = 72f, PercentFontBase = 50f;
        public const float TapFont = 50f, TapFontMin = 18f, TapFontMax = 72f, TapFontBase = 50f;
        public const float PointsTitleFont = 40f, PointsTitleMin = 18f, PointsTitleMax = 72f, PointsTitleBase = 40f;
        public const float QtyFont = 50f, QtyFontMin = 18f, QtyFontMax = 72f, QtyFontBase = 40f;

        // 官方词条（prefab `Localize.mTerm` 实读）
        public const string TitleTerm = "Event_Description/Renked/SeasonEnd";   // ⚠️ 原版拼写就是 `Renked`
        public const string PointsTerm = "Event_Description/Ranked/RankingPoints";
        public const string TapTerm = "Demo/Welcome/TapToContinue";
        public const string PercentTerm = "Event_Description/Ranked/EndFinishPercentage";
        public const string PositionTerm = "Event_Description/Ranked/EndFinishScore";

        // 兜底原文
        const string TitleEn = "SEASON ENDED!", TitleZh = "赛季已结束！";
        const string PointsEn = "Rating:", PointsZh = "评分:";
        const string TapEn = "Click to continue", TapZh = "点击以继续";
        const string PercentEn = "You finished in the top {0}% of players.", PercentZh = "你在玩家中排名前 {0}%。";
        const string PositionEn = "You finished in position {0}", PositionZh = "你最终排名第 {0} 位";

        // ============================================================ 数据
        public struct Context
        {
            /// <summary>赛季评分（原版 `score` 那颗）；null ⇒ `--`。</summary>
            public int? Score;
            /// <summary>百分位（原版 `EndFinishPercentage` 的 `{0}`）；给了就显示「top {0}%」。</summary>
            public int? Percent;
            /// <summary>名次（原版 `EndFinishScore` 的 `{0}`）；`Percent` 为空时才看它。</summary>
            public int? Position;
            /// <summary>段位名（服务端）；null ⇒ 段位图不画。</summary>
            public string DivisionLabel;
        }

        public static Context DefContext()
        {
            return new Context { Score = null, Percent = null, Position = null, DivisionLabel = null };
        }
        static Context FillDef(Context? src) { return src.HasValue ? src.Value : DefContext(); }

        public static RankedSeasonEndedWindow LastOpened { get; private set; }

        Context _ctx = DefContext();
        public Context Ctx { get { return _ctx; } }

        Transform _dark, _division, _rankIcon, _title, _percent, _tap, _rankPoints, _glow;
        Label _titleLabel, _percentLabel, _tapLabel, _pointsTitle, _pointsQty;
        BlinkGraphic _blink;
        CanvasGroup _canvasGroup;
        public bool DivisionArtBlank { get; private set; }
        public readonly List<string> MissingArt = new List<string>();

        // ============================================================ 建窗
        public static RankedSeasonEndedWindow Create(WindowsManager mgr, Context? ctx = null)
        {
            var go = new GameObject(RootName);
            var win = go.AddComponent<RankedSeasonEndedWindow>();
            win.type = WindowType.Fullscreen;             // 🔴 实读 type = **0**（三分窗里唯一一个）
            win.placement = WindowsPlacement.Popup;       // 实读 windowsPlacement = 15
            win.closeOnEsc = true;                        // 实读 closeOnESC = 1
            win.updateNavPanel = false;                   // 实读 updateNavPanel = 0
            win.extraScaleSmallScreen = 1f;               // 实读 1.0（= 不覆盖）
            win.Manager = mgr;
            win._ctx = FillDef(ctx);
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[RankedSeasonEnded] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
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

        // ============================================================ 建整棵树（顺序 = 原版兄弟序）
        public void Build()
        {
            PointerLayer.UnregisterOwnedBy(gameObject);
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();
            _dark = _division = _rankIcon = _title = _percent = _tap = _rankPoints = _glow = null;
            _titleLabel = _percentLabel = _tapLabel = _pointsTitle = _pointsQty = null;
            DivisionArtBlank = false;

            // `canvasGroup`：prefab 根上挂着（`m_Alpha = 1` / 可交互 / 挡射线）
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            // ---- 1) `Menu Dark Background`（`BackgroundCloseButton.window = 0` ⇒ 点暗底**不**关窗）----
            _dark = MenuDraw.Node(transform, "Menu Dark Background", DarkR);
            MenuDraw.Rect(_dark, CardArt.Solid(), DarkR, "Image", QShade, ShadeTint);

            // ---- 2) `Division Image`（服务端段位图；`m_Sprite = 0` ⇒ 不画）----
            _division = MenuDraw.Node(transform, "Division Image", DivisionR);
            if (string.IsNullOrEmpty(_ctx.DivisionLabel))
            {
                DivisionArtBlank = true;
                NoteNoArt("Division Image", "段位大图 = 服务端 `RankedDivisions.GetDivisionData(...).AssetReferenceTyped`，"
                                          + "本机没有「段位号→图名」对照表");
            }
            else
            {
                DivisionArtBlank = true;
                NoteNoArt("Division Image", "给了段位名 `" + _ctx.DivisionLabel + "`，但**段位图名对照表本地没有**");
            }

            // ---- 3) `Division Image/Rank Icon`（服务端；按视觉框建，见 `RankIconR`）----
            _rankIcon = MenuDraw.Node(_division, "Rank Icon", RankIconR);
            NoteNoArt("Rank Icon", "`rankIcon.m_Sprite = 0`（运行期 `RankedDivisions.GetRankData(...)`）");

            // ---- 4) `Title`：fs 87.01 · base 87.01 · **AUTO=0**（min18/max72 是死字段）· Center/**Capline** ----
            // 🔴 `autoMinPx` 传 **0** ⇒ 不开自适应（闸 = `autoMinPx > 0 && fontPx > autoMinPx`），
            //    只保留 `wrapPx` = 框宽 ⇒ 字号恒 **87.01**（= 原版渲染值）。⛔ 别传成 `[0, 72, base]` 半套。
            _title = MenuDraw.Node(transform, "Title", TitleR);
            _titleLabel = MenuDraw.Text(_title, TitleR, TutorialMessagePopup.OfficialTerm(TitleTerm, TitleEn, TitleZh),
                                        Color.white, "EverguildTextMeshPro", TitleFont, QTitle, TitleR.W,
                                        face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(_titleLabel, Label.VAlign.Capline, TitleR);

            // ---- 5) `Percentage Or Score text`：fs 50 · base 50 · Center/**Capline** ----
            _percent = MenuDraw.Node(transform, "Percentage Or Score text", PercentR);
            _percentLabel = MenuDraw.Text(_percent, PercentR, PercentText(), Color.white, "EverguildTextMeshPro",
                                          PercentFont, QText, PercentR.W, PercentFontMin, PercentFontMax, PercentFontBase, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(_percentLabel, Label.VAlign.Capline, PercentR);

            // ---- 6) `Tap To Continue`：fs 50 · base 50 · Center/**Capline** · `BlinkGraphic` ----
            _tap = MenuDraw.Node(transform, "Tap To Continue", TapR);
            _tapLabel = MenuDraw.Text(_tap, TapR, TutorialMessagePopup.OfficialTerm(TapTerm, TapEn, TapZh),
                                      Color.white, "EverguildTextMeshPro", TapFont, QText, TapR.W,
                                      TapFontMin, TapFontMax, TapFontBase, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(_tapLabel, Label.VAlign.Capline, TapR);
            var tap = _tapLabel;
            if (tap != null)
            {
                var b = EnsureBlink();
                b.LogTag = "[RankedSeasonEnded]";
                b.Silent = false;
                b.Bind(tap.gameObject, () => tap.color, c => tap.SetColor(c));
                b.Restart();
            }

            // ---- 7) `Glow below`（粒子件，scale 250；归特效线 ⇒ 节点 + 出声）----
            _glow = MenuDraw.Node(transform, "Glow below", GlowR);
            NoteNoArt("Glow below", "原版是 `ParticleSystem`（`localScale = 250`）；粒子属于特效线，本窗只留节点");

            // ---- 8) `Rank Points`（HorizontalLayoutGroup，MiddleCenter）----
            _rankPoints = MenuDraw.Node(transform, "Rank Points", RankPointsR);
            _pointsTitle = MenuDraw.Text(_rankPoints, PointsTitleR,
                                         TutorialMessagePopup.OfficialTerm(PointsTerm, PointsEn, PointsZh),
                                         Color.white, "Ranking Points Title", PointsTitleFont, QText,
                                         PointsTitleR.W, PointsTitleMin, PointsTitleMax, PointsTitleBase, face: TmpFont.Face.LatinUi);
            var modeIcon = MenuDraw.Node(_rankPoints, "GameMode Icon", GameModeIconR);
            MenuDraw.Rect(modeIcon, Tex(ArtGameModeIcon, "`GameMode Icon` 的排位模式图标"), GameModeIconR,
                          "Image", QLine, null, false);
            _pointsQty = MenuDraw.Text(_rankPoints, PointsQtyR, _ctx.Score.HasValue ? _ctx.Score.Value.ToString() : "--",
                                       Color.white, "Ranking Points Quantity Text", QtyFont, QText, PointsQtyR.W,
                                       QtyFontMin, QtyFontMax, QtyFontBase, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(_pointsQty, PointsQtyR);
            MenuDraw.SetVAlign(_pointsQty, Label.VAlign.Capline, PointsQtyR);

            if (MissingArt.Count > 0)
                Debug.LogWarning("[RankedSeasonEnded] ⚠️ 有 " + MissingArt.Count + " 张图取不到："
                                 + string.Join("、", MissingArt.ToArray()));
        }

        /// <summary>`Percentage Or Score text`：原版二选一（有百分位 ⇒ `EndFinishPercentage`，否则 `EndFinishScore`）。
        /// 两个都没有 ⇒ `--` + 出声一次（⛔ 不编名次）。</summary>
        string PercentText()
        {
            if (_ctx.Percent.HasValue)
                return string.Format(TutorialMessagePopup.OfficialTerm(PercentTerm, PercentEn, PercentZh), _ctx.Percent.Value);
            if (_ctx.Position.HasValue)
                return string.Format(TutorialMessagePopup.OfficialTerm(PositionTerm, PositionEn, PositionZh), _ctx.Position.Value);
            NoteOnce(ref _warnedNoRank,
                     "[RankedSeasonEnded] `Percentage Or Score text` **没有名次/百分位** —— 原版 = 赛季结束后服务端给的"
                   + " `RankedDivisions` 名次统计（本机没有）⇒ 显示 `--`，⛔ 不编名次。");
            return "--";
        }
        static bool _warnedNoRank;

        // ============================================================ 时钟（`BlinkGraphic`）
        void Update() { Tick(Time.deltaTime); }
        public void Tick(float dt) { if (_blink != null) _blink.Tick(dt); }

        BlinkGraphic EnsureBlink()
        {
            if (_blink == null) _blink = GetComponent<BlinkGraphic>();
            if (_blink == null) _blink = gameObject.AddComponent<BlinkGraphic>();
            return _blink;
        }

        // ============================================================ 取图 / 出声（不静默）
        Texture2D Tex(string name, string what)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name))
            {
                MissingArt.Add(name);
                Debug.LogWarning("[RankedSeasonEnded] 图取不到：`" + name + "`（" + what + "）⇒ **这一件没画**"
                               + "（`MenuDraw.Rect/Nine` 对 `tex == null` 是静默返回 null）。"
                               + "导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            }
            return t;
        }
        static readonly HashSet<string> _noArtWarned = new HashSet<string>();
        static void NoteNoArt(string what, string why)
        {
            if (!_noArtWarned.Add(what)) return;
            Debug.Log("[RankedSeasonEnded] 留空：" + what + " —— " + why + " ⇒ 节点照建、**不画**（⛔ 不拿别的图顶）。");
        }
        static void NoteOnce(ref bool flag, string msg)
        {
            if (flag) return;
            flag = true;
            Debug.LogWarning(msg);
        }

        // ============================================================ 自检读口
        public Transform DarkNode { get { return _dark; } }
        public Transform DivisionNode { get { return _division; } }
        public Transform RankIconNode { get { return _rankIcon; } }
        public Transform TitleNode { get { return _title; } }
        public Transform PercentNode { get { return _percent; } }
        public Transform TapNode { get { return _tap; } }
        public Transform RankPointsNode { get { return _rankPoints; } }
        public Label TitleLabel { get { return _titleLabel; } }
        public Label PercentLabel { get { return _percentLabel; } }
        public Label TapLabel { get { return _tapLabel; } }
        public Label PointsTitleLabel { get { return _pointsTitle; } }
        public Label PointsQtyLabel { get { return _pointsQty; } }
        public CanvasGroup Group { get { return _canvasGroup; } }
        public BlinkGraphic Blink { get { return _blink; } }

        public string DebugDump()
        {
            return "title=\"" + (_titleLabel != null ? _titleLabel.Text : "-") + "\""
                 + " percent=\"" + (_percentLabel != null ? _percentLabel.Text : "-") + "\""
                 + " score=\"" + (_pointsQty != null ? _pointsQty.Text : "-") + "\""
                 + " divisionArt=" + (DivisionArtBlank ? "<blank>" : "?")
                 + " missingArt=" + MissingArt.Count;
        }
    }
}
