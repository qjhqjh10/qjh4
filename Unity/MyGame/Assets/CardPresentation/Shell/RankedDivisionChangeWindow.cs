// RankedDivisionChangeWindow.cs — 原版 **`RankedEventDivisionChangeWindowNewSystem`**
// （prefab 根 `Ranked Division Change Window` · 排位升段窗：PROMOTED! + 段位大图 + 印玺里程碑 + 点击继续）
//
// ============================ 出处（唯一判据，全部本地现读）============================
// · prefab 树 / 矩形 / 组件 / 文字档位（2026-10-17 现读，可复跑）：
//     `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Ranked Division Change Window" --depth 9`
//   根 = `bundle_menus_assets_all/GameObject/Ranked Division Change Window.json`，
//   根 MB = `MonoBehaviour_-4938671765695524530.json`（窗参逐字段实读）：
//     `type = 1`(Popup) · `windowsPlacement = 15`(Popup) · `closeOnESC = 1` · `updateNavPanel = 0` ·
//     `extraScaleSmallScreen = 1.0` · `openSound` / `closeSound` = 空 PPtr · `useDefaultCloseSoundIfNull = 1`
//   ⇒ 关窗音走家族默认（`GameWindow.Close()` 的既有实现），本窗**不赋任何 clip**。
//   字段 PPtr 实读：`backgroundCloseButton.window = 0`（**点暗底不关窗**）· `divisionIcon` / `rankIcon` /
//   `rankNumberGlow` 三颗的 `Image.m_Sprite` **全是 0**（运行期由服务端 `RankedDivisions` 的
//   `AssetReferenceTyped` 喂）· `milestonesStepDisplay.stepPrefab` = `RankedSealStep` ·
//   `gameModeIcons` = 两颗（`Game Mode Icon Left` / `Right`，`m_Sprite` 也是 0）。
// · 行为（第一权威 = 反编译 `d:/2/tools/decomp_full/RankedEventDivisionChangeWindowNewSystem__Open.c`）：
//   ① `GameWindow.Open()`；
//   ② `get_windowData()` 为空 ⇒ **整条链早退**（= 没有服务端数据就什么都不填）；
//   ③ 有数据时：段位大图 ← `RankedDivisions.GetDivisionData(...).AssetReferenceTyped.Load()`；
//      `rankIcon` ← `GetRankData(...)`；两者都是 `SetActive(sprite != null)`；
//      `reachedText` ← `I2("You have reached the {0}" 那一族)` 与段位名拼起来；
//      `rankPointsQuantity` ← 评分整数；`milestonesStepDisplay.Show(current, target, …)`；
//      `myAnimation.Play(...)` + 两个协程（升段动画 / 安全输入）。
//   ⇒ **段位名 / 评分 / 印玺数全在服务端**（`RankedDivisionInfo.cs` 文件头 ⑤ 已明写「这一整棵全是服务器数据……
//      留空、不编数字」；`RankedDivisionsSO` 的「段位号→图名」对照表本地没有）。
//      ⇒ 本窗照 prefab 建**窗壳 + 静态文案 + 结构**；数字/图**留空/`--`** + 出声一次，⛔ 不编。
//
// ============================ 我们侧的对位（逐件写清，不冒充原版）============================
// · **文案**：标题 / `Ranking Points` / `Tap To continue` 三颗的 `Localize.mTerm` 原样取（走
//   `TutorialMessagePopup.OfficialTerm` —— 全工程**唯一一份**「Loc.Table → 官方 13 语言表 → 文件内兜底」的取法，
//   ⛔ 别在这儿再抄一份）。「You have reached …」那颗原版是**运行期拼**的（prefab 里的
//   `You have reached the Legenday Division!` 是**拼错的残留**）⇒ 用词条 `Event_Description/Ranked/ReachedText`
//   + 服务端段位名；没有段位名 ⇒ `--` + 出声。
// · **段位大图 / Rank Icon / Rank Number Glow / 两颗模式图标**：`m_Sprite = 0` ⇒ 节点照建、**不画**，
//   各出声一次（同 `TutorialMessagePopup` 的 `Event image` 那一档）。
// · **粒子 `Particles DivisionUp`**：原版是 5 个 `ParticleSystem`（`Glow below` / `Sparks background` /
//   `Sparks Burn` / 双亲下的 `Glow Big` / `Promote Letter Effect` / `Sparks fast`）。
//   粒子属于**战斗表现线 / 特效线**，本窗**只建容器节点**并出声一次（⛔ 不在这里手搓粒子）。
// · **`Mission Milestones Progress`**：prefab 里是 **4 颗** `RankedSealStep` 模板（原版运行期
//   `MilestonesStepDisplay.Show(current, target, …)` 按服务端 `TotalSeals` 改颗数与填充）。
//   我们照 prefab 建 4 颗（`Empty` + `Fill` 两层，`Fill` 出厂 alpha = 0），并留
//   `SetSeals(current, total)` 口：给了才填、不给就是「未推进」。
// · **`Tag To continue` / `Tap To continue` 的闪烁**：`BlinkGraphic`（出厂 `blinkSpeed 1.0` /
//   `colorVariation 0.5`）—— 同 `TutorialMessagePopup` 的写法。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `RankedEventDivisionChangeWindowNewSystem`（`GameWindow` 子类）——
    /// **排位升段 / 降段窗**：`PROMOTED!` + 段位大图 + 评分 + 印玺里程碑 + 点击继续。
    /// <para>🔴 **数据缺口（如实记，⛔ 不编）**：段位名 / 段位图 / 评分 / 印玺数 / 模式图标
    /// **全部来自服务端**（判据 → 文件头）。出厂一档 = 窗壳 + 静态文案，数字 `--`、图不画。</para>
    /// <para>🔴 **入口缺口**：原版由排位事件链在结算后开（`RankedEventWindow` 那一族），
    /// 我们这一侧**没有排位事件结算状态机** ⇒ 本窗**建好并注册**，入口由调用方自己定（与 T18 同口径）。</para></summary>
    public class RankedDivisionChangeWindow : GameWindow
    {
        // ============================================================ 队列档（自成一档 · 3410–3419）
        // ⚠️ 层带不许与别的窗重叠：`TutorialMessagePopup` 3372–3380 · `DuelPopupWindow` 3400 ·
        //   `BattleLogPopup` 3458 · `RankedRewardEventWindow` 3460（2026-10-17 现扫 `Shell/*.cs` 的 `const int Q*`）。
        public const int QShade = 3410,      // `Menu Dark Background`
                         QTitle = 3411,      // `Title` + 两颗模式图标
                         QDivision = 3412,   // `Division Image`
                         QRank = 3413,       // `Division Image/Rank Icon`
                         QText = 3414,       // `Text Content` 全部文案
                         QSeal = 3415;       // `Mission Milestones Progress` 的印玺
        public const int QHit = 3419;

        // ============================================================ 几何（dump 现读 · 1920×1080 画布 px）
        public const string RootName = "Ranked Division Change Window";

        public static readonly PxRect DarkR = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);
        /// <summary>`Title`（`sizeDelta = 0×112`、pivot 顶中 ⇒ 横向取满、纵向 0–112）。</summary>
        public static readonly PxRect TitleR = new PxRect(53.20f, 0f, 1866.80f, 112f);
        public static readonly PxRect ModeIconLeftR = new PxRect(860f, 6f, 960f, 106f);
        public static readonly PxRect ModeIconRightR = new PxRect(960f, 6f, 1060f, 106f);
        public static readonly PxRect DivisionR = new PxRect(534.30f, 149.30f, 1385.70f, 1000.70f);
        /// <summary>`Rank Icon` 的**视觉**框：布局 67.418² × `localScale 1.090329` ⇒ 73.51²、中心不变
        /// （960.6, 458.1）。⚠️ **直接按视觉框建、`localScale` 保持 1** —— 否则子件（`Rank Icon Glow`）
        /// 的 localPosition 会被父级缩放再乘一次（`MenuDraw.PosInDesignSpace` 除的是**父的父**那一级，
        /// 判据见它自己的注释，A294/A60⑨）：那会得到「框对、子件跑偏」的静默错。</summary>
        public static readonly PxRect RankIconR = new PxRect(923.845f, 421.345f, 997.355f, 494.855f);
        /// <summary>`Rank Icon Glow`：**出厂 `m_IsActive = false`**（dump 行首 `INACT`）。
        /// 视觉框 74.14²（= 68 × 1.09），与 Rank Icon 同中心。</summary>
        public static readonly PxRect RankIconGlowR = new PxRect(923.50f, 421.00f, 997.70f, 495.20f);
        public static readonly PxRect ReachedR = new PxRect(257.90f, 105.10f, 1662.10f, 203.20f);
        public static readonly PxRect PointsTitleR = new PxRect(469.30f, 655.70f, 1450.70f, 735.00f);
        public static readonly PxRect RankPointsR = new PxRect(758.20f, 735.00f, 1161.80f, 835.00f);
        public static readonly PxRect PointsBgR = new PxRect(627.30f, 743.50f, 1292.70f, 826.50f);
        public static readonly PxRect PointsIconR = new PxRect(857.60f, 735.00f, 957.60f, 835.00f);
        public static readonly PxRect PointsQtyR = new PxRect(957.60f, 745.40f, 1062.40f, 824.70f);
        public static readonly PxRect TapR = new PxRect(336.90f, 1000.70f, 1583.10f, 1080.00f);
        public static readonly PxRect MilestonesR = new PxRect(292.50f, 418.70f, 1627.50f, 661.30f);
        /// <summary>4 颗 `RankedSealStep` 的**视觉**框（布局 100×242.66 × `localScale 2.5` ⇒ 249.98×606.64）。
        /// 🔴 面包屑：`steps` 容器自己 `localScale 2.5`，四颗的 `m_AnchoredPosition` 是 0 —— dump 报的
        /// 「绝对矩形」已经按父链缩放换算过，这里**照抄换算后的值**（⛔ 别再乘一次）。</summary>
        public static readonly PxRect[] StepR =
        {
            new PxRect(418.50f, 236.70f, 668.50f, 843.30f),
            new PxRect(696.20f, 236.70f, 946.20f, 843.30f),
            new PxRect(973.80f, 236.70f, 1223.80f, 843.30f),
            new PxRect(1251.50f, 236.70f, 1501.50f, 843.30f),
        };

        public static readonly Color ShadeTint = new Color(0f, 0f, 0f, 0.7725490f);
        /// <summary>`Rank Icon Glow` 的 `Image.m_Color` 实读 `(1, 0.9168, 0, 1)`。</summary>
        public static readonly Color GlowTint = new Color(1f, 0.9167996f, 0f, 1f);

        /// <summary>`Rank Icon Glow` 的图（实读 `Sprite` 名字 —— 原版那颗出厂 `m_Sprite = 0`，见文件头）。</summary>
        public const string ArtRankIconGlow = "";
        public const string ArtSealEmpty = "Rank_Skull_Empty";
        public const string ArtSealFill = "Rank_Skull";
        public const string ArtPointsBg = "40K_main_rank_display";

        // 文字档位（prefab 逐颗实读）
        public const float TitleFont = 87f, TitleFontMin = 30f, TitleFontMax = 87f, TitleFontBase = 87f;
        public const float ReachedFont = 51.4f, ReachedFontMin = 18f, ReachedFontMax = 51.4f, ReachedFontBase = 57f;
        public const float PointsFont = 40f, PointsFontMin = 18f, PointsFontMax = 72f, PointsFontBase = 40f;
        public const float QtyFont = 50f, QtyFontMin = 18f, QtyFontMax = 72f, QtyFontBase = 50f;
        public const float TapFont = 40f, TapFontMin = 18f, TapFontMax = 72f, TapFontBase = 40f;

        // 官方词条（prefab `Localize.mTerm` 实读）
        public const string TitleTerm = "Event_Description/Ranked/Promoted";
        public const string ReachedTerm = "Event_Description/Ranked/ReachedText";
        public const string PointsTerm = "Event_Description/Ranked/RankingPoints";
        public const string TapTerm = "Demo/Welcome/TapToContinue";

        // 兜底原文（官方 13 语言表取不到时用；en 逐字 = 官方表，zh = 官方表中文列）
        const string TitleEn = "PROMOTED!", TitleZh = "晋升！";
        const string ReachedEn = "You have reached the {0}", ReachedZh = "你已达到{0}";
        const string PointsEn = "Rating:", PointsZh = "评分:";
        const string TapEn = "Click to continue", TapZh = "点击以继续";

        // ============================================================ 数据
        /// <summary>本窗要铺的一份内容。**全部字段默认 null = prefab 出厂那一档（没有服务端数据）**。</summary>
        public struct Context
        {
            /// <summary>段位名（原版 `RankedDivisions.GetDivisionData(...)` 的本地化名）；null ⇒ `--`。</summary>
            public string DivisionLabel;
            /// <summary>评分整数（原版 `Ranking Points Quantity Text`）；null ⇒ `--`。</summary>
            public int? Rating;
            /// <summary>印玺已填颗数（原版 `MilestonesStepDisplay.Show(current, …)`）；null ⇒ 不填。</summary>
            public int? SealCurrent;
            /// <summary>印玺总颗数（原版服务端 `RankedDivisionV2.TotalSeals`）；null ⇒ 保持 prefab 的 4 颗、全空。</summary>
            public int? SealTotal;
        }

        public static Context DefContext()
        {
            return new Context { DivisionLabel = null, Rating = null, SealCurrent = null, SealTotal = null };
        }
        static Context FillDef(Context? src) { return src.HasValue ? src.Value : DefContext(); }

        /// <summary>最近一次开出来的那一扇（自检读口）。</summary>
        public static RankedDivisionChangeWindow LastOpened { get; private set; }

        Context _ctx = DefContext();
        public Context Ctx { get { return _ctx; } }

        // 自检读口（节点引用）
        Transform _dark, _title, _division, _rankIcon, _rankGlow, _textContent, _milestones, _tap;
        Label _titleLabel, _reached, _pointsTitle, _pointsQty, _tapLabel;        BlinkGraphic _blink;
        Transform _sealSteps;
        readonly List<ImageQuad> _sealFills = new List<ImageQuad>();
        public bool DivisionArtBlank { get; private set; }
        public readonly List<string> MissingArt = new List<string>();

        // ============================================================ 建窗
        public static RankedDivisionChangeWindow Create(WindowsManager mgr, Context? ctx = null)
        {
            var go = new GameObject(RootName);
            var win = go.AddComponent<RankedDivisionChangeWindow>();
            win.type = WindowType.Popup;                  // 实读 type = 1
            win.placement = WindowsPlacement.Popup;       // 实读 windowsPlacement = 15
            win.closeOnEsc = true;                        // 实读 closeOnESC = 1
            win.updateNavPanel = false;                   // 实读 updateNavPanel = 0
            win.extraScaleSmallScreen = 1f;               // 实读 1.0（= 不覆盖）
            // `openSound` / `closeSound` 实读都是空 PPtr、`useDefaultCloseSoundIfNull = 1`
            //   ⇒ 一个 clip 都不赋，关窗音走 `GameWindow.Close()` 的家族默认。
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 A2）**：本窗真正的那一声**不是** `openSound`，是
            //   **`soundOnOpen`** —— 原版 MB `-4938671765695524530`
            //   （`RankedEventDivisionChangeWindowNewSystem : GameWindow`，prefab `Ranked Division Change Window`）
            //   `soundOnOpen = PPtr(fileID 6, pid -2236843390636958589)` = cue **`Division Change`**
            //   （clip `Division Increase`，ttp 0.01）；播放点 = 反编译
            //   `RankedEventDivisionChangeWindowNewSystem._DoPromoteAnimations_d__22.MoveNext.c:32-33`
            //   `AudioCue__Play2D(cue, 3)` ⇒ **MixerType 3 = Jingles**。见 `Open()`。
            win.Manager = mgr;
            win._ctx = FillDef(ctx);
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[RankedDivChange] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 A2）**：`soundOnOpen` = cue **`Division Change`**，
            // **Jingles 组（MixerType 3）** —— 判据见 `Create()` 里那一段（原版 MB pid + 反编译
            // `_DoPromoteAnimations` 的 `Play2D(cue, 3)`）。⛔ 别用 `PlayFx`（`WFSoundPlayer` 写死 FX 组）。
            // ⚠️ **落点是我们的（如实标注）**：原版挂在 `DoPromoteAnimations` 那个协程的起点；
            //   我们的升级动画**没建**（见本文件头）⇒ 落在 `Open()` —— 玩家开窗即听到，顺序上等价于
            //   「动画一开始就响」。
            WindowSounds.PlayJingles(WindowSounds.DivisionChangeCue, "[RankedDivChange] `soundOnOpen`");
            Build();
        }

        /// <summary>换一份内容（复用同一扇窗时用；`GameWindow.TryOpen` 在 `Open` 态照原版早退 ⇒ 必须显式调本方法）。</summary>
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
            _sealFills.Clear();
            _dark = _title = _division = _rankIcon = _rankGlow = _textContent = _milestones = _tap = null;
            _reached = _pointsTitle = _pointsQty = _tapLabel = null;
            _titleLabel = null;
            DivisionArtBlank = false;

            // ---- 1) `Menu Dark Background`（`BackgroundCloseButton.window = 0` ⇒ 点暗底**不**关窗）----
            _dark = MenuDraw.Node(transform, "Menu Dark Background", DarkR);
            MenuDraw.Rect(_dark, CardArt.Solid(), DarkR, "Image", QShade, ShadeTint);

            // ---- 2) `Title`（PROMOTED! + 左右两颗模式图标）----
            _title = MenuDraw.Node(transform, "Title", TitleR);
            var titleText = TutorialMessagePopup.OfficialTerm(TitleTerm, TitleEn, TitleZh);
            // 原版：fs 87 · base 87 · auto[30~87] · 折行 1 · 白 · Center/Midline(4096)
            _titleLabel = MenuDraw.Text(_title, TitleR, titleText, Color.white, "EverguildTextMeshPro",
                                        TitleFont, QTitle, TitleR.W, TitleFontMin, TitleFontMax, TitleFontBase, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(_titleLabel, Label.VAlign.Midline, TitleR);   // 实读 `m_VerticalAlignment = 4096`
            // 两颗模式图标：`m_Sprite = 0`（服务端喂）⇒ 节点照建、不画 + 出声一次（见 NoteNoArt）
            MenuDraw.Node(_title, "Game Mode Icon Left", ModeIconLeftR);
            MenuDraw.Node(_title, "Game Mode Icon Right", ModeIconRightR);
            NoteNoArt("Game Mode Icon Left/Right", "`gameModeIcons` 两颗的 `Image.m_Sprite` 都是 0（运行期由服务端图标喂）");

            // ---- 3) `Division Image`（段位大图；原版 `sprite = 0` ⇒ 不画）----
            _division = MenuDraw.Node(transform, "Division Image", DivisionR);
            if (string.IsNullOrEmpty(_ctx.DivisionLabel))
            {
                DivisionArtBlank = true;
                NoteNoArt("Division Image", "段位大图 = 服务端 `RankedDivisions.GetDivisionData(...).AssetReferenceTyped`，"
                                          + "本机没有「段位号→图名」对照表");
            }
            else
            {
                // 给了段位名也没图（判据仍缺）—— 节点照建、仍不画，出声说明。
                DivisionArtBlank = true;
                NoteNoArt("Division Image", "给了段位名 `" + _ctx.DivisionLabel + "`，但**段位图名对照表本地没有**");
            }

            // ---- 4) `Division Image/Rank Icon`（+ 出厂关着的 `Rank Icon Glow`）----
            //   原版：`Rank Icon` 布局 67.418²、`m_LocalScale = 1.090329` ⇒ 视觉 73.51²。
            //   🔴 这里按**视觉框**建、`localScale` 保持 1（理由见 `RankIconR` 的注释）。
            _rankIcon = MenuDraw.Node(_division, "Rank Icon", RankIconR);
            _rankGlow = MenuDraw.Node(_rankIcon, "Rank Icon Glow", RankIconGlowR);
            _rankGlow.gameObject.SetActive(false);   // 出厂 `m_IsActive = false`
            NoteNoArt("Rank Icon", "`rankIcon.m_Sprite = 0`（运行期 `RankedDivisions.GetRankData(...)`）");

            // ---- 5) `Text Content` ----
            _textContent = MenuDraw.Node(transform, "Text Content", new PxRect(0f, 0f, 1920f, 1080f));
            // 5a `New division reached Text`：fs 51.4 · base 57 · auto[18~51.4] · Center/Middle
            _reached = MenuDraw.Text(_textContent, ReachedR, ReachedText(), Color.white, "New division reached Text",
                                     ReachedFont, QText, ReachedR.W, ReachedFontMin, ReachedFontMax, ReachedFontBase, face: TmpFont.Face.LatinUi);
            // 5b `Ranking Points Title`：**出厂关着**（dump 行首 INACT；`Open()` 里没有任何 SetActive 打开它）
            var pt = MenuDraw.Node(_textContent, "Ranking Points Title", PointsTitleR);
            _pointsTitle = MenuDraw.Text(pt, PointsTitleR, TutorialMessagePopup.OfficialTerm(PointsTerm, PointsEn, PointsZh),
                                         Color.white, "EverguildTextMeshPro", PointsFont, QText, PointsTitleR.W,
                                         PointsFontMin, PointsFontMax, PointsFontBase, face: TmpFont.Face.LatinCardBody);
            pt.gameObject.SetActive(false);
            // 5c `Rank Points`：同样**出厂关着**（父节点 INACT ⇒ 三个子件全是 `ANC✗`）
            var rp = MenuDraw.Node(_textContent, "Rank Points", RankPointsR);
            MenuDraw.Rect(rp, Tex(ArtPointsBg, "`Rank Points/Background`"), PointsBgR, "Background", QText, null, false);
            MenuDraw.Node(rp, "Rank Icon", PointsIconR);
            _pointsQty = MenuDraw.Text(rp, PointsQtyR, _ctx.Rating.HasValue ? _ctx.Rating.Value.ToString() : "--",
                                       Color.white, "Ranking Points Quantity Text", QtyFont, QText, PointsQtyR.W,
                                       QtyFontMin, QtyFontMax, QtyFontBase, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(_pointsQty, Label.VAlign.Capline, PointsQtyR);
            rp.gameObject.SetActive(false);
            // 5d `Tap To continue`：fs 40 · base 40 · Center/Middle · `BlinkGraphic`
            _tap = MenuDraw.Node(_textContent, "Tap To continue", TapR);
            _tapLabel = MenuDraw.Text(_tap, TapR, TutorialMessagePopup.OfficialTerm(TapTerm, TapEn, TapZh),
                                      Color.white, "EverguildTextMeshPro", TapFont, QText, TapR.W,
                                      TapFontMin, TapFontMax, TapFontBase, face: TmpFont.Face.LatinCardBody);
            var tap = _tapLabel;
            if (tap != null)
            {
                var b = EnsureBlink();
                b.LogTag = "[RankedDivChange]";
                b.Silent = false;
                b.Bind(tap.gameObject, () => tap.color, c => tap.SetColor(c));
                b.Restart();
            }

            // ---- 6) `Particles DivisionUp`（粒子归特效线；只建容器 + 出声一次）----
            var particles = MenuDraw.Node(transform, "Particles DivisionUp", new PxRect(0f, 0f, 1920f, 1080f));
            NoteNoArt("Particles DivisionUp", "原版是 5 个 `ParticleSystem`（Glow below / Sparks background / "
                                            + "Sparks Burn / Glow Big / Promote Letter Effect / Sparks fast）；"
                                            + "粒子属于特效线，本窗只建容器节点");

            // ---- 7) `Mission Milestones Progress`（prefab = 4 颗 `RankedSealStep`）----
            _milestones = MenuDraw.Node(transform, "Mission Milestones Progress", MilestonesR);
            _sealSteps = MenuDraw.Node(_milestones, "steps", new PxRect(292.50f, 418.70f, 1627.50f, 661.30f));
            for (int i = 0; i < StepR.Length; i++)
            {
                var step = MenuDraw.Node(_sealSteps, "RankedSealStep" + (i == 0 ? "" : " (" + i + ")"), StepR[i]);
                // 兄弟序 = prefab：`Empty` 在前、`Fill` 在后（Fill 画在 Empty 之上）
                MenuDraw.Rect(step, Tex(ArtSealEmpty, "`RankedSealStep/Empty`"), StepR[i], "Empty", QSeal, null, true);
                // `RankedSealStep` 根上的 `Image` 出厂 `m_Color.a = 0`（= 未填），图 = `Rank Skull`（填充层）
                var fillRoot = MenuDraw.Rect(step, Tex(ArtSealFill, "`RankedSealStep/Fill`"), StepR[i], "Fill",
                                             QSeal + 1, new Color(1f, 1f, 1f, 0f), true);
                if (fillRoot != null) _sealFills.Add(fillRoot);
            }
            ApplySeals();

            if (MissingArt.Count > 0)
                Debug.LogWarning("[RankedDivChange] ⚠️ 有 " + MissingArt.Count + " 件取不到/没画："
                                 + string.Join("、", MissingArt.ToArray()));
        }

        /// <summary>`ReachedText`：原版是**运行期拼**的（段位名来自服务端）。没有段位名 ⇒ `--` + 出声一次。</summary>
        string ReachedText()
        {
            var fmt = TutorialMessagePopup.OfficialTerm(ReachedTerm, ReachedEn, ReachedZh);
            if (string.IsNullOrEmpty(_ctx.DivisionLabel))
            {
                WarnOnce(ref _warnedNoDivision,
                         "[RankedDivChange] `New division reached Text` **没有段位名** —— 原版 = `RankedDivisions."
                       + "GetDivisionData(...)` 的本地化段位名（服务端数据，本机没有）⇒ 显示 `--`，⛔ 不编数字/段位。");
                return "--";
            }
            return string.Format(fmt, _ctx.DivisionLabel);
        }
        static bool _warnedNoDivision;

        /// <summary>印玺填充（`current` / `total`）。`total` 与 prefab 的 4 颗不同 ⇒ 按 prefab 的前 N 颗填，
        /// 多出来的颗数**不做**（prefab 只有 4 颗模板；`MilestonesStepDisplay` 的运行期扩颗在服务端驱动下才会发生）。</summary>
        public void SetSeals(int current, int total)
        {
            _ctx.SealCurrent = current;
            _ctx.SealTotal = total;
            ApplySeals();
        }

        void ApplySeals()
        {
            int n = _sealFills.Count;
            if (n == 0) return;
            if (!_ctx.SealCurrent.HasValue) return;      // 出厂：全空（prefab 的 alpha 0）
            int cur = Mathf.Clamp(_ctx.SealCurrent.Value, 0, n);
            for (int i = 0; i < n; i++)
            {
                bool on = i < cur;
                _sealFills[i].gameObject.SetActive(on);
                if (on) _sealFills[i].SetTint(Color.white);   // 出厂 alpha 0 ⇒ 填上时恢复不透明
            }
        }

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
                Debug.LogWarning("[RankedDivChange] 图取不到：`" + name + "`（" + what + "）⇒ **这一件没画**"
                               + "（`MenuDraw.Rect/Nine` 对 `tex == null` 是静默返回 null）。"
                               + "导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            }
            return t;
        }

        static readonly HashSet<string> _noArtWarned = new HashSet<string>();
        static void NoteNoArt(string what, string why)
        {
            if (!_noArtWarned.Add(what)) return;
            Debug.Log("[RankedDivChange] 留空：" + what + " —— " + why + " ⇒ 节点照建、**不画**（⛔ 不拿别的图顶）。");
        }
        static void WarnOnce(ref bool flag, string msg)
        {
            if (flag) return;
            flag = true;
            Debug.LogWarning(msg);
        }

        // ============================================================ 自检读口
        public Transform DarkNode { get { return _dark; } }
        public Transform TitleNode { get { return _title; } }
        public Transform DivisionNode { get { return _division; } }
        public Transform RankIconNode { get { return _rankIcon; } }
        public Transform RankGlowNode { get { return _rankGlow; } }
        public Transform TextContentNode { get { return _textContent; } }
        public Transform MilestonesNode { get { return _milestones; } }
        public Transform SealStepsNode { get { return _sealSteps; } }
        public Label ReachedLabel { get { return _reached; } }
        public Label TitleLabel { get { return _titleLabel; } }
        public Label PointsTitleLabel { get { return _pointsTitle; } }
        public Label PointsQtyLabel { get { return _pointsQty; } }
        public Label TapLabel { get { return _tapLabel; } }
        public BlinkGraphic Blink { get { return _blink; } }

        public string DebugDump()
        {
            return "reached=\"" + (_reached != null ? _reached.Text : "-") + "\""
                 + " rating=\"" + (_pointsQty != null ? _pointsQty.Text : "-") + "\""
                 + " divisionArt=" + (DivisionArtBlank ? "<blank>" : "?")
                 + " seals=" + _sealFills.Count + " missingArt=" + MissingArt.Count;
        }
    }
}
