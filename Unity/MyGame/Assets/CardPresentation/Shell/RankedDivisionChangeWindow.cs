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
//
// ============================ 🆕 2026-10-22：动画（原来一个字都没做）============================
// 出处：`工具/gen_division_clips.py` 文件头（判据链全文）+ `Resources/division_clips.json`。
//   · 原版 `myAnimation` = `Animation_8653451351532409166`（挂在根 GO 上），
//     `m_Animations = [Division Up, Rank Up, Division Config]`、`m_PlayAutomatically = false`。
//   · `Open()`：`Play("Division Config")`（**布局/复位**，不是演出）→ 等
//     `MilestonesStepDisplay.GetTotalAnimationTime() − 0.25` 秒 → `DoPromoteAnimations` 分叉：
//     升段 → `Play("Rank Up")` · 分段变化 → `Play("Division Up")`。
//   ⇒ 本文件按这份数据建三条 legacy clip、挂到 `Animation` 组件上，并按原版时机播。
//   🔴 **两套驱动各管一半**：原生 `Animation` 只能绑 `Transform` / `GameObject.m_IsActive`；
//     原版另外那些 `Image.m_Color.a` / `CanvasGroup.m_Alpha` / TMP `m_fontColor.a` /
//     `ParticleSystem.ShapeModule.m_Scale` / `AnimateUIMaterialProperty.floatProperty1`
//     **我们这套世界空间 UI 没有对应组件**（我们是 `ImageQuad` / `Label`）⇒ 逐帧手动求值写过去。
using System;
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
            /// <summary>🆕 2026-10-22：这一窗是「**分段变化**」还是「**升段**」——
            /// = 原版 `RankedEventDivisionChangeWindowNewSystem.Context.IsDivisionChange`
            /// （`dump.cs` 的 `get_IsDivisionChange`；`Open()` 里由 `previousDivision` 与当前段位号比较得出
            /// `isDivisionChange = (rankId != prevRankId)`，判据 → `RankedEventDivisionChangeWindowNewSystem__Open.c:222`
            /// 的 `*(bool *)(lVar6 + 0x30) = iVar1 != iVar5`）。
            /// <para>`false`（出厂）= **升段** ⇒ 播 `Rank Up`；`true` = **分段变化** ⇒ 播 `Division Up`。</para></summary>
            public bool IsDivisionChange;
        }

        public static Context DefContext()
        {
            return new Context { DivisionLabel = null, Rating = null, SealCurrent = null, SealTotal = null,
                                 IsDivisionChange = false };
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
        // 🆕 2026-10-22：动画要按 **相对路径** 找这三颗（判据 = prefab 树 / clip 里的 path）
        Transform _particlesDivision, _particlesRank, _sparksBurnDivision;
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
            //   ⇒ **开/关窗音**一个 clip 都不赋，关窗音走 `GameWindow.Close()` 的家族默认。
            //   （🆕 2026-10-22：**动画**的三条 clip 是另一码事 —— 见文件头与 `StartSequence()`。）
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
            // 🆕 **2026-10-22（补做动画 · 铁律 5 就地订正）**：`soundOnOpen` = cue **`Division Change`**
            // （`soundOnOpen = PPtr(fileID 6, pid -2236843390636958589)`，ttp 0.01）**不再**在这里播。
            // 原版真正的播放点是反编译 `RankedEventDivisionChangeWindowNewSystem._DoPromoteAnimations_d__22.MoveNext.c`：
            //   `WaitForSeconds(delay)` → `AudioCue.Play2D(cue, 3)`（**MixerType 3 = Jingles**）→ 再 `Play("Rank Up"/"Division Up")`。
            // ⇒ 现在改到 `PlayPromote()` 里、**紧挨着 promote clip 起播**（顺序与原版一致）。
            // ⚠️ 原来这里写「我们的升级动画没建 ⇒ 落在 `Open()`」——**那条理由已经不存在了**（动画已接）。
            Build();
            StartSequence();     // = 原版 `Open()` 尾：`Play("Division Config")` + 起 promote 计时
        }

        /// <summary>换一份内容（复用同一扇窗时用；`GameWindow.TryOpen` 在 `Open` 态照原版早退 ⇒ 必须显式调本方法）。
        /// ⚠️ 换内容**不重播**开场演出（原版没有这条入口）⇒ 只把动画停掉、停在 `Build()` 的出厂姿态。</summary>
        public void Show(Context? ctx = null)
        {
            _ctx = FillDef(ctx);
            Build();
            StopAnimation();
        }

        // ============================================================ 建整棵树（顺序 = 原版兄弟序）
        public void Build()
        {
            PointerLayer.UnregisterOwnedBy(gameObject);
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();
            _sealFills.Clear();
            _dark = _title = _division = _rankIcon = _rankGlow = _textContent = _milestones = _tap = null;
            _particlesDivision = _particlesRank = _sparksBurnDivision = null;
            _reached = _pointsTitle = _pointsQty = _tapLabel = null;
            _titleLabel = null;
            DivisionArtBlank = false;
            // 动画的「原色」缓存按节点实例记 —— 重建后旧的件全没了，必须清（否则表会一直长、且读到伪 null）
            _baseLabel.Clear();
            _baseQuad.Clear();
            _nodeCache.Clear();

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

            // ---- 6) `Particles DivisionUp` / `Particles Rank Up`（粒子归特效线；只建容器 + 出声一次）----
            //   🆕 2026-10-22：**两条 clip 的 path 直接指向这两颗**（`m_IsActive`）以及
            //   `Particles DivisionUp/Sparks Burn` 的 `ShapeModule.m_Scale` ⇒ 节点必须**照 prefab 名建出来**，
            //   否则 `SampleAnimation` 对那几条曲线是**静默空转**（⛔ 不是「差不多就行」）。
            //   判据（prefab 树现走）：`Particles Rank Up` 是**纯 `Transform`**（无 RectTransform）、
            //   `localPosition (0, 88.9, −26.6)` · `localScale 164.237`；子件 `Sparks Burn` 是 RectTransform。
            _particlesDivision = MenuDraw.Node(transform, "Particles DivisionUp", new PxRect(0f, 0f, 1920f, 1080f));
            _sparksBurnDivision = MenuDraw.Node(_particlesDivision, "Sparks Burn", new PxRect(909f, 449f, 1009f, 549f));
            _particlesRank = MenuDraw.Node(transform, "Particles Rank Up", new PxRect(0f, 0f, 1920f, 1080f));
            NoteNoArt("Particles DivisionUp / Particles Rank Up", "原版是 5 个 `ParticleSystem`（Glow below / "
                                            + "Sparks background / Sparks Burn / Glow Big / Promote Letter Effect / Sparks fast）；"
                                            + "粒子属于特效线，本窗只建容器节点（动画的 `m_IsActive` / `ShapeModule.m_Scale` 曲线照建、"
                                            + "取不到 `ParticleSystem` 时逐条出声一次）");

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

        // ============================================================ 时钟
        void Update() { Tick(Time.deltaTime); }

        /// <summary>一帧。⚠️ **顺序有意**：`BlinkGraphic` 先（它每帧都拿 `_base` 重写 `Tap To continue` 的 alpha），
        /// 动画后 —— 这样 `Text Content` 的 `m_Alpha` 才压得住那一闪（原版两者本来就是同一个 `Graphic` 的
        /// alpha 与父 `CanvasGroup` 的 alpha，谁后写谁算）。</summary>
        public void Tick(float dt)
        {
            if (_blink != null) _blink.Tick(dt);
            TickAnimation(dt);
        }

        BlinkGraphic EnsureBlink()
        {
            if (_blink == null) _blink = GetComponent<BlinkGraphic>();
            if (_blink == null) _blink = gameObject.AddComponent<BlinkGraphic>();
            return _blink;
        }

        // ============================================================ 动画（原版 `myAnimation` · 三条 legacy clip）
        //
        // 【判据链 · 全文 → `工具/gen_division_clips.py` 文件头】
        //  · 组件 `Animation_8653451351532409166` 挂在根 GO 上，`m_Animations = [Division Up, Rank Up, Division Config]`，
        //    `m_PlayAutomatically = false`；`m_Animation`（默认 clip）= `Division Config`。
        //  · `Open()`：`Play("Division Config")`（**布局/复位**）→ `MilestonesStepDisplay.Show(...)` →
        //    `delay = GetTotalAnimationTime() - 0.25` → `DoPromoteAnimations(delay, isDivisionChange, prev)`。
        //  · `DoPromoteAnimations`：`WaitForSeconds(delay)` → cue `Division Change`（MixerType 3）→ 分叉：
        //       升段（`isDivisionChange == false`）→ `Play("Rank Up")`
        //       分段变化（`true`）              → `Play("Division Up")`
        //  · 三条全是 legacy 曲线（`m_Legacy=true` · `m_MuscleClipSize=0`）⇒ 数据本地全有、运行期重建。
        //
        // 【🔴 为什么是「两套驱动」】
        //   ① **原生 `Animation`**（`AddClip` + `Play(name)`）：这是原版那条路，`Animation.isPlaying` 可观测。
        //      Unity 自己绑得上的只有 `Transform`（缩放）与 `GameObject.m_IsActive` 两族。
        //   ② **手动通道**（`ApplyChannels`）：原版另外那些曲线绑在 `Image.m_Color.a` / `CanvasGroup.m_Alpha` /
        //      TMP `m_fontColor.a` / `ParticleSystem.ShapeModule.m_Scale` /
        //      `AnimateUIMaterialProperty.floatProperty1` 上 —— **我们这套世界空间 UI 一个这样的组件都没有**
        //      （画图是 `ImageQuad`、画字是 `Label`）⇒ 逐帧 `AnimationCurve.Evaluate` 求值后写我们自己的件。
        //      ⛔ 别把这一半删掉换成 `SetCurve`：那只会得到一条**静默空转**的曲线。
        //   ③ **每帧仍然 `SampleAnimation` 一次**（两个模式都做）：万一原生 `Play` 没起来（`Open` 时还没激活等），
        //      变换曲线照样被写到正确值；同时它也是批处理里唯一的变换驱动（批处理没有帧循环）。
        //      ⛔ 别删掉那条 `if (!isPlaying) 出声` —— 「没起来」必须出声（红线：不许静默失败）。

        public const string ClipsResourcePath = "division_clips";
        public const string ClipKeyDivisionConfig = "divisionConfig";
        public const string ClipKeyRankUp = "rankUp";
        public const string ClipKeyDivisionUp = "divisionUp";

        // `MilestoneStep` 的出厂值（`dump.cs` 字段表 + prefab MB `MonoBehaviour_-1734507135801913010.json` 实读）：
        //   `animationStartDelay 0.25` · `animationTime 0.6` · `animationBetweenStepsDelay 0.2`
        public const float MilestoneAnimStartDelay = 0.25f;
        public const float MilestoneAnimTime = 0.6f;
        public const float MilestoneAnimBetweenSteps = 0.2f;
        /// <summary>`Open()` 里 `delay = GetTotalAnimationTime() - DAT_1834b2e7c`，那个常量按 **4 字节 float** 读 = **0.25**
        /// （`工具/read_literal.py D:/2/unity_run_ref/GameAssembly.dll 0x1834b2e7c`；8 字节那档是 5.72e-06 = 噪声）。</summary>
        public const float PromoteDelayTrim = 0.25f;

        /// <summary>= 原版 `MilestonesStepDisplay.GetTotalAnimationTime() - 0.25`（秒）。
        /// 公式逐句照 `MilestonesStepDisplay__GetTotalAnimationTime.c`：
        /// `(count-1)*between + start + time + between`；再减 `PromoteDelayTrim`。
        /// **4 颗印玺 ⇒ 1.40 s**（原版这个 count 来自服务端 `TotalSeals`，我们只有 prefab 的 4 颗）。</summary>
        public static float PromoteDelay(int steps)
        {
            if (steps < 1) steps = 1;
            return (steps - 1) * MilestoneAnimBetweenSteps
                 + MilestoneAnimStartDelay + MilestoneAnimTime + MilestoneAnimBetweenSteps
                 - PromoteDelayTrim;
        }

        // ---- 数据表（与 `工具/gen_division_clips.py` 的 schema 逐字对应；JSON 里多出来的 `src` 等字段 `JsonUtility` 会忽略）----
        [Serializable] public class DivisionClipKey { public float t, v, si, so; }
        [Serializable] public class DivisionClipCurve
        {
            public string path, type, property, channel, srcAttribute;
            public int classId;
            public DivisionClipKey[] keys;
        }
        [Serializable] public class DivisionClipEvent { public float t; public string function; }
        [Serializable] public class DivisionClip
        {
            public string key, name, wrapMode;
            public bool legacy, loopTime;
            public float sampleRate, length;
            public DivisionClipEvent[] events;
            public DivisionClipCurve[] curves;
        }
        [Serializable] public class DivisionClipFile
        {
            public int schema;
            public string note, source, owner;
            public DivisionClip[] clips = new DivisionClip[0];
        }

        // ---- 表缓存（静态：整份表只读一次）----
        static DivisionClip[] _clipData;
        static Dictionary<string, DivisionClip> _clipByKey;
        static string _clipsError;

        /// <summary>表载进来了没有（没载 = 资源不在；<see cref="ClipsError"/> 有话说）。</summary>
        public static bool ClipsLoaded { get { EnsureClips(); return _clipData != null && _clipData.Length > 0; } }
        /// <summary>表里有几条 clip（预期 3）。</summary>
        public static int ClipCount { get { EnsureClips(); return _clipData != null ? _clipData.Length : 0; } }
        /// <summary>载表/解析失败的原因（无错 = null）。</summary>
        public static string ClipsError { get { EnsureClips(); return _clipsError; } }

        public static DivisionClip FindClipData(string key)
        {
            EnsureClips();
            DivisionClip d;
            return (key != null && _clipByKey != null && _clipByKey.TryGetValue(key, out d)) ? d : null;
        }

        static void EnsureClips()
        {
            if (_clipData != null) return;
            var ta = Resources.Load<TextAsset>(ClipsResourcePath);
            if (ta == null)
            {
                _clipsError = "`Resources/" + ClipsResourcePath + ".json` 载不到 —— 跑一次 "
                            + "`python -I -X utf8 工具/gen_division_clips.py`";
                _clipData = new DivisionClip[0];
                _clipByKey = new Dictionary<string, DivisionClip>();
                return;
            }
            DivisionClipFile f = null;
            try { f = JsonUtility.FromJson<DivisionClipFile>(ta.text); }
            catch (Exception e) { _clipsError = "`" + ClipsResourcePath + ".json` 解析失败：" + e.Message; }
            var items = f != null && f.clips != null ? f.clips : new DivisionClip[0];
            _clipData = items;
            _clipByKey = new Dictionary<string, DivisionClip>(items.Length);
            foreach (var c in items)
            {
                if (c == null || string.IsNullOrEmpty(c.key)) continue;
                if (!_clipByKey.ContainsKey(c.key)) _clipByKey[c.key] = c;
            }
            if (_clipsError == null && items.Length == 0)
                _clipsError = "`" + ClipsResourcePath + ".json` 里一条 clip 都没有";
        }

        // ---- 建 clip（= 原版 JSON 的曲线逐条重放）----
        /// <summary>上一次 <see cref="BuildClip"/> 里**真正交给 `SetCurve`** 的曲线条数。</summary>
        public static int LastNativeCurveCount { get; private set; }
        /// <summary>上一次 <see cref="BuildClip"/> 里**走手动通道**（我们工程没有对应组件）的曲线条数。</summary>
        public static int LastManualCurveCount { get; private set; }
        /// <summary>`SetCurve` 抛异常的次数（正常 = 0；非 0 会逐条出声）。</summary>
        public static int LastCurveErrors { get; private set; }

        /// <summary>按表里一条 clip 建 `AnimationClip`。
        /// 🔴 **只把 Unity 绑得上的两族交给 `SetCurve`**（`Transform` / `GameObject`），其余计入手动通道 ——
        /// 两族条数之和必须等于 `data.curves.Length`（自检那条盯着它，改坏「少写一条曲线」就红）。</summary>
        public static AnimationClip BuildClip(DivisionClip d)
        {
            LastNativeCurveCount = LastManualCurveCount = LastCurveErrors = 0;
            if (d == null) return null;
            var clip = new AnimationClip
            {
                name = d.name,
                legacy = true,
                frameRate = d.sampleRate > 0f ? d.sampleRate : 60f,
                wrapMode = WrapMode.Default,     // 原版 `m_WrapMode = 0`（Default）
            };
            if (d.curves != null)
                for (int i = 0; i < d.curves.Length; i++)
                {
                    var c = d.curves[i];
                    if (c == null || !NativeBindable(c)) { LastManualCurveCount++; continue; }
                    var ac = BuildCurve(c);
                    if (ac == null) { LastManualCurveCount++; continue; }
                    try
                    {
                        clip.SetCurve(c.path, c.type == "GameObject" ? typeof(GameObject) : typeof(Transform),
                                      c.property, ac);
                        LastNativeCurveCount++;
                    }
                    catch (Exception e)
                    {
                        LastCurveErrors++;
                        Debug.LogWarning("[RankedDivChange] `SetCurve` 失败：path=`" + c.path + "` property=`"
                                       + c.property + "` —— " + e.Message);
                    }
                }
            return clip;
        }

        /// <summary>Unity 原生绑得上的两族（其余走手动通道，理由见上面那段）。</summary>
        static bool NativeBindable(DivisionClipCurve c)
        {
            return c.type == "Transform" || c.type == "GameObject";
        }

        /// <summary>把一列关键帧（**含 inSlope/outSlope**）变成 `AnimationCurve`。
        /// 🔴 切线必须逐键写回：`Division Image` 的 5 键缩放里 2.6→2.65 那一记回弹带非零斜率，
        /// 用 `new Keyframe(t,v)` 会变成 ease-in/out（端点仍对得上 ⇒ 端点断言抓不到）。</summary>
        static AnimationCurve BuildCurve(DivisionClipCurve c)
        {
            if (c == null || c.keys == null || c.keys.Length == 0) return null;
            var ks = new Keyframe[c.keys.Length];
            for (int i = 0; i < c.keys.Length; i++)
            {
                var k = c.keys[i];
                ks[i] = new Keyframe(k.t, k.v, k.si, k.so);
            }
            return new AnimationCurve(ks);
        }

        // ---- 逐通道的轨道（按 path 归并；`AnimationCurve` 只建一次，静态缓存）----
        sealed class RtClip
        {
            public DivisionClip data;
            public AnimationClip clip;
            public ScaleTrack[] scales;
            public AlphaTrack[] alphas;
            public GroupTrack[] groups;
            public ActiveTrack[] actives;
            public ParticleTrack[] particles;
            public string[] materialPaths;
        }
        struct ScaleTrack { public string path; public AnimationCurve x, y, z; }
        struct AlphaTrack { public string path; public AnimationCurve a; }
        struct GroupTrack { public string path; public AnimationCurve group, text; }
        struct ActiveTrack { public string path; public AnimationCurve a; }
        struct ParticleTrack { public string path; public AnimationCurve x, y; }

        static readonly Dictionary<string, RtClip> _rtCache = new Dictionary<string, RtClip>();

        static RtClip RuntimeFor(string key)
        {
            EnsureClips();
            RtClip rc;
            if (_rtCache.TryGetValue(key ?? "", out rc)) return rc;
            var d = FindClipData(key);
            if (d == null) return null;
            rc = new RtClip { data = d, clip = BuildClip(d) };
            if (d.curves == null || d.curves.Length == 0)
            {
                WarnChannel("empty:" + key, "clip `" + key + "` 的 `curves` 是空的（JSON 坏了）⇒ 这一段只会空转。");
                _rtCache[key] = rc;
                return rc;
            }

            var scaleOrder = new List<string>();
            var scaleMap = new Dictionary<string, AnimationCurve[]>();
            var alphaList = new List<AlphaTrack>();
            var groupOrder = new List<string>();
            var groupMap = new Dictionary<string, AnimationCurve[]>();
            var activeList = new List<ActiveTrack>();
            var partOrder = new List<string>();
            var partMap = new Dictionary<string, AnimationCurve[]>();
            var matList = new List<string>();

            for (int i = 0; i < d.curves.Length; i++)
            {
                var c = d.curves[i];
                if (c == null) continue;
                switch (c.channel)
                {
                    case "scale":
                    {
                        if (!scaleMap.ContainsKey(c.path)) { scaleMap[c.path] = new AnimationCurve[3]; scaleOrder.Add(c.path); }
                        int idx = c.property != null && c.property.EndsWith(".y", StringComparison.Ordinal) ? 1
                                : c.property != null && c.property.EndsWith(".z", StringComparison.Ordinal) ? 2 : 0;
                        scaleMap[c.path][idx] = BuildCurve(c);
                        break;
                    }
                    case "alpha":   alphaList.Add(new AlphaTrack { path = c.path, a = BuildCurve(c) }); break;
                    case "groupAlpha":
                    case "textColorAlpha":
                    {
                        if (!groupMap.ContainsKey(c.path)) { groupMap[c.path] = new AnimationCurve[2]; groupOrder.Add(c.path); }
                        groupMap[c.path][c.channel == "groupAlpha" ? 0 : 1] = BuildCurve(c);
                        break;
                    }
                    case "active":  activeList.Add(new ActiveTrack { path = c.path, a = BuildCurve(c) }); break;
                    case "particleShapeScale":
                    {
                        if (!partMap.ContainsKey(c.path)) { partMap[c.path] = new AnimationCurve[2]; partOrder.Add(c.path); }
                        partMap[c.path][c.property != null && c.property.EndsWith(".y", StringComparison.Ordinal) ? 1 : 0] = BuildCurve(c);
                        break;
                    }
                    case "materialFloat": matList.Add(c.path); break;
                    default:
                        WarnChannel("chan:" + c.channel,
                                    "未知通道 `" + c.channel + "`（path=`" + c.path + "` property=`" + c.property
                                  + "`）⇒ 这一条**没接**（改坏法：生成器加了新通道而这里没跟着加）。");
                        break;
                }
            }

            rc.scales = new ScaleTrack[scaleOrder.Count];
            for (int i = 0; i < scaleOrder.Count; i++)
            {
                var p = scaleOrder[i];
                var a = scaleMap[p];
                rc.scales[i] = new ScaleTrack { path = p, x = a[0], y = a[1], z = a[2] };
            }
            rc.alphas = alphaList.ToArray();
            rc.groups = new GroupTrack[groupOrder.Count];
            for (int i = 0; i < groupOrder.Count; i++)
            {
                var p = groupOrder[i];
                var a = groupMap[p];
                rc.groups[i] = new GroupTrack { path = p, group = a[0], text = a[1] };
            }
            rc.actives = activeList.ToArray();
            rc.particles = new ParticleTrack[partOrder.Count];
            for (int i = 0; i < partOrder.Count; i++)
            {
                var p = partOrder[i];
                var a = partMap[p];
                rc.particles[i] = new ParticleTrack { path = p, x = a[0], y = a[1] };
            }
            rc.materialPaths = matList.ToArray();
            _rtCache[key] = rc;
            return rc;
        }

        // ---- 播放 ----
        Animation _anim;
        bool _clipsReady;
        bool _playing;
        string _playingKey, _lastFinishedKey;
        float _animT, _animLen;
        float _promoteDelay = -1f;
        int _playCount;
        RtClip _rt;
        AnimationClip _curClip;
        readonly Dictionary<string, Transform> _nodeCache = new Dictionary<string, Transform>();
        readonly Dictionary<Label, Color> _baseLabel = new Dictionary<Label, Color>();
        readonly Dictionary<ImageQuad, Color> _baseQuad = new Dictionary<ImageQuad, Color>();
        static readonly HashSet<string> _chanWarned = new HashSet<string>();

        static void WarnChannel(string key, string msg)
        {
            if (_chanWarned.Count > 128 || !_chanWarned.Add(key)) return;
            Debug.LogWarning("[RankedDivChange] " + msg);
        }

        /// <summary>= 原版 `Open()` 的收尾：`Play("Division Config")` + 起 promote 计时。
        /// 🔴 **`Division Config` 不是演出**：它是「布局/复位」，只有单键（印玺那条 0.2167 s 淡入是唯一的动态）。</summary>
        public void StartSequence()
        {
            StopAnimation();
            PlayClip(ClipKeyDivisionConfig);
            // 印玺颗数一律用 **prefab 的 4 颗**（`StepR.Length`），⛔ 不用 `_sealFills.Count` ——
            //   后者在「图取不到」时会是 0（`MenuDraw.Rect` 对 `tex == null` 返回 null）⇒ 延迟会静默变短。
            //   原版这个 count 来自服务端 `TotalSeals`，我们只有 prefab 这一档（如实标注）。
            _promoteDelay = PromoteDelay(StepR.Length);
        }

        /// <summary>停播（换内容 / 重开时调）。**不还原节点姿态** —— 还原交给下一次 `Build()` 或下一次播放。</summary>
        public void StopAnimation()
        {
            _playing = false;
            _playingKey = null;
            _promoteDelay = -1f;
            _animT = 0f;
            if (_anim != null) _anim.Stop();
        }

        /// <summary>播一条 clip（按 **key**，⛔ 不按名字）。表里没有 / 建不出来 ⇒ 出声 + 返回 false。</summary>
        public bool PlayClip(string key)
        {
            var rc = RuntimeFor(key);
            if (rc == null || rc.clip == null)
            {
                Debug.LogWarning("[RankedDivChange] 动画 clip `" + key + "` 取不到（表里 " + ClipCount + " 条"
                               + (ClipsError != null ? "；载表错误：" + ClipsError : "") + "）⇒ 这一段**不播**。");
                return false;
            }
            EnsureAnim();
            _nodeCache.Clear();
            _rt = rc;
            _curClip = rc.clip;
            _playingKey = key;
            _animT = 0f;
            _animLen = rc.data.length > 0f ? rc.data.length : rc.clip.length;
            _playing = true;
            _playCount++;
            if (_anim != null)
            {
                _anim.Stop();
                _anim.Play(rc.clip.name);
                if (!Application.isBatchMode && !_anim.isPlaying)
                    WarnChannel("native:" + key,
                                "原生 `Animation.Play(\"" + rc.clip.name + "\")` 没起来（`isPlaying` 仍是 false）"
                              + " ⇒ 只剩手动采样在驱动；先查窗口那一刻是不是还没激活。");
            }
            ApplyChannels(0f);
            return true;
        }

        void EnsureAnim()
        {
            if (_anim == null) _anim = GetComponent<Animation>();
            if (_anim == null) _anim = gameObject.AddComponent<Animation>();
            _anim.playAutomatically = false;      // 原版 `m_PlayAutomatically = false`
            _anim.wrapMode = WrapMode.Default;    // 原版 `m_WrapMode = 0`
            if (_clipsReady) return;
            EnsureClips();
            if (_clipData != null)
                for (int i = 0; i < _clipData.Length; i++)
                {
                    var rc = RuntimeFor(_clipData[i].key);
                    if (rc != null && rc.clip != null) _anim.AddClip(rc.clip, rc.clip.name);
                }
            _clipsReady = true;
        }

        /// <summary>每帧推进（真机 `Update` / 自检 `TickForTest`）。</summary>
        void TickAnimation(float dt)
        {
            if (dt < 0f) dt = 0f;
            // ① 等 promote 那一拍（= 原版 `WaitForSeconds(delay)`）
            if (_promoteDelay >= 0f)
            {
                _promoteDelay -= dt;
                if (_promoteDelay <= 0f)
                {
                    _promoteDelay = -1f;
                    // 原版这一拍先响 cue（`AudioCue.Play2D(cue, 3)` = Jingles），再 `Play(name)`。
                    // `soundOnOpen` 指向的就是它（判据 → `Create()` 与文件头）。
                    WindowSounds.PlayJingles(WindowSounds.DivisionChangeCue, "[RankedDivChange] `soundOnOpen`");
                    PlayClip(_ctx.IsDivisionChange ? ClipKeyDivisionUp : ClipKeyRankUp);
                }
            }
            // ② 推进当前 clip
            if (!_playing || _rt == null) return;
            _animT += dt;
            bool done = _animT >= _animLen;
            if (done) _animT = _animLen;
            // 每帧显式采样：批处理没有帧循环（原生 `Animation` 推不动）⇒ 这里是唯一的变换驱动；
            // 真机则与原生那份取同一组值（同一份曲线、同一个 dt）。
            if (_curClip != null) _curClip.SampleAnimation(gameObject, _animT);
            ApplyChannels(_animT);
            if (done)
            {
                _playing = false;
                if (_anim != null) _anim.Stop();
                _lastFinishedKey = _playingKey;
                // **播完就停在这一帧** —— clip 的末键就是原版的静态姿态（⛔ 别回退到 `Build()` 的出厂值）
            }
        }

        /// <summary>把**手动通道**在当前时刻的值写到我们自己的件上（`Transform` / `m_IsActive` 那两族由
        /// `SampleAnimation`/原生动画负责，这里不重复）。</summary>
        void ApplyChannels(float t)
        {
            if (_rt == null) return;

            // ① 缩放：逐 path 一次性写 x/y/z（分成三帧写会让父件先被拉一次）
            if (_rt.scales != null)
                for (int i = 0; i < _rt.scales.Length; i++)
                {
                    var g = _rt.scales[i];
                    var n = NodeAt(g.path);
                    if (n == null) continue;
                    var s = n.localScale;
                    n.localScale = new Vector3(g.x != null ? g.x.Evaluate(t) : s.x,
                                               g.y != null ? g.y.Evaluate(t) : s.y,
                                               g.z != null ? g.z.Evaluate(t) : s.z);
                }

            // ② 组 alpha（原版 `CanvasGroup.m_Alpha` + TMP `m_fontColor.a`）
            if (_rt.groups != null)
                for (int i = 0; i < _rt.groups.Length; i++)
                {
                    var g = _rt.groups[i];
                    var n = NodeAt(g.path);
                    if (n == null) continue;
                    ApplyGroupAlpha(n, g.group != null ? g.group.Evaluate(t) : 1f,
                                       g.text != null ? g.text.Evaluate(t) : 1f);
                }

            // ③ `Image.m_Color.a`：只作用于**该节点自己**的 `ImageQuad`
            if (_rt.alphas != null)
                for (int i = 0; i < _rt.alphas.Length; i++)
                {
                    var g = _rt.alphas[i];
                    var n = NodeAt(g.path);
                    if (n == null) continue;
                    var q = n.GetComponent<ImageQuad>();
                    if (q == null)
                    {
                        WarnChannel("alpha:" + g.path,
                                    "`" + g.path + "` 这一颗没有 `ImageQuad` ⇒ 原版 `Image.m_Color.a` 那条曲线"
                                  + "**画不出来**（那几颗的 `m_Sprite = 0`，图本来就来自服务端，见文件头「数据缺口」）。");
                        continue;
                    }
                    var b = BaseQuad(q);
                    q.SetTint(new Color(b.r, b.g, b.b, b.a * g.a.Evaluate(t)));
                }

            // ④ `GameObject.m_IsActive`
            if (_rt.actives != null)
                for (int i = 0; i < _rt.actives.Length; i++)
                {
                    var g = _rt.actives[i];
                    var n = NodeAt(g.path);
                    if (n == null) continue;
                    bool on = g.a == null || g.a.Evaluate(t) > 0.5f;
                    if (n.gameObject.activeSelf != on) n.gameObject.SetActive(on);
                }

            // ⑤ 粒子 `ShapeModule.m_Scale`（我们只建了容器 ⇒ 没有 `ParticleSystem`）
            //
            // 🔴 **关于原版「显式 Play()」这一拍（如实记）**：
            //   `DoPromoteAnimations_d__22.MoveNext.c` 在 `Play(name)` 之后还有一句虚调用
            //   `(**(code**)(*plVar5 + 0x348))(plVar5, 0, …)`，**看着像** `ParticleSystem.Play(withChildren)`，
            //   但同级字段偏移实读到的是 `+0x90 = divisionIcon` / `+0x98 = rankIcon`（都是 `Image`，
            //   判据 = `dump.cs` 的字段表 + `ChangeParticleEffectAndSpriteRank` 的调用实参）⇒ **判据不足、不定**。
            //   本窗因此**不臆造**这一声：粒子的节拍**只用 clip 里的 `m_IsActive` 键**（`Rank Up` 0.3833 s /
            //   `Division Up` 0.4167 s，见 `Resources/division_clips.json`），那是可复查的数据。
            //   等粒子线把 `ParticleSystem` 建起来，`m_IsActive` 一开就会自己播（默认 `playOnAwake`）。
            if (_rt.particles != null)
                for (int i = 0; i < _rt.particles.Length; i++)
                {
                    var g = _rt.particles[i];
                    var n = NodeAt(g.path);
                    if (n == null) continue;
                    var ps = n.GetComponent<ParticleSystem>();
                    if (ps == null)
                    {
                        WarnChannel("ps:" + g.path,
                                    "`" + g.path + "` 上没有 `ParticleSystem` ⇒ 原版 `ShapeModule.m_Scale` 那两条曲线"
                                  + "**没接**（粒子归特效线：本窗只建容器，见 `Build()` 的注释）。");
                        continue;
                    }
                    var sh = ps.shape;
                    var sc = sh.scale;
                    sh.scale = new Vector3(g.x != null ? g.x.Evaluate(t) : sc.x,
                                           g.y != null ? g.y.Evaluate(t) : sc.y, sc.z);
                }

            // ⑥ `AnimateUIMaterialProperty.floatProperty1`（原版那是一个改材质 shader 浮点的组件）
            if (_rt.materialPaths != null)
                for (int i = 0; i < _rt.materialPaths.Length; i++)
                {
                    var p = _rt.materialPaths[i];
                    if (NodeAt(p) == null) continue;
                    WarnChannel("mat:" + p,
                                "`" + p + "` 的 `AnimateUIMaterialProperty.floatProperty1` **没接** ——"
                              + "那个组件我们工程里没有（`grep class AnimateUIMaterialProperty` 零命中），"
                              + "而且它驱动的是服务端喂的那张段位图的材质。");
                }
        }

        // ---- 组 alpha（原版 `CanvasGroup`：管整棵子树）----
        void ApplyGroupAlpha(Transform node, float groupMul, float textMul)
        {
            var labels = node.GetComponentsInChildren<Label>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                var lb = labels[i];
                if (lb == null) continue;
                var b = BaseLabel(lb);
                lb.SetColor(new Color(b.r, b.g, b.b, b.a * groupMul * textMul));
            }
            var quads = node.GetComponentsInChildren<ImageQuad>(true);
            for (int i = 0; i < quads.Length; i++)
            {
                var q = quads[i];
                if (q == null) continue;
                var b = BaseQuad(q);
                q.SetTint(new Color(b.r, b.g, b.b, b.a * groupMul));
            }
        }

        Color BaseLabel(Label lb)
        {
            Color c;
            if (!_baseLabel.TryGetValue(lb, out c)) { c = lb.color; _baseLabel[lb] = c; }
            return c;
        }
        Color BaseQuad(ImageQuad q)
        {
            Color c;
            if (!_baseQuad.TryGetValue(q, out c)) { c = q.Tint; _baseQuad[q] = c; }
            return c;
        }

        /// <summary>按**相对 owner GO 的路径**取节点（clip 里的 `path` 就是这个口径）。
        /// ⛔ 不用 `transform.Find(整条路径)` 之外的写法 —— 逐段 `Find` 更早暴露「中间某一段没了」。</summary>
        Transform NodeAt(string relPath)
        {
            if (string.IsNullOrEmpty(relPath)) return transform;
            Transform t;
            if (_nodeCache.TryGetValue(relPath, out t) && t != null) return t;
            t = transform;
            var parts = relPath.Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                if (t == null) break;
                var next = t.Find(parts[i]);
                if (next == null)
                {
                    WarnChannel("node:" + relPath,
                                "动画路径 `" + relPath + "` 在窗里找不到（断在第 " + (i + 1) + " 段 `" + parts[i]
                              + "`）⇒ 那几条曲线**画不出来**（原版 prefab 树里是有的）。");
                    return null;
                }
                t = next;
            }
            if (t != null) _nodeCache[relPath] = t;
            return t;
        }

        // ---- 自检读口（动画）----
        public bool IsPlaying { get { return _playing; } }
        public string PlayingClipKey { get { return _playingKey; } }
        public string LastFinishedClipKey { get { return _lastFinishedKey; } }
        public int PlayCount { get { return _playCount; } }
        public float AnimationTime { get { return _animT; } }
        public float AnimationLength { get { return _animLen; } }
        /// <summary>还剩几秒到 promote 那一拍（< 0 = 没有排队的 promote）。</summary>
        public float PromoteDelayRemaining { get { return _promoteDelay; } }
        public Animation NativeAnimation { get { return _anim; } }
        public AnimationClip ClipForTest(string key) { var rc = RuntimeFor(key); return rc != null ? rc.clip : null; }
        public Transform NodeForTest(string relPath) { _nodeCache.Clear(); return NodeAt(relPath); }
        public Vector3 NodeScaleForTest(string relPath) { var n = NodeForTest(relPath); return n != null ? n.localScale : Vector3.zero; }
        public bool NodeActiveForTest(string relPath) { var n = NodeForTest(relPath); return n != null && n.gameObject.activeSelf; }
        /// <summary>节点上第一条 `Label`（没有就看第一块 `ImageQuad`）的当前 alpha；都取不到 = −1。</summary>
        public float NodeAlphaForTest(string relPath)
        {
            var n = NodeForTest(relPath);
            if (n == null) return -1f;
            var lb = n.GetComponentInChildren<Label>(true);
            if (lb != null) return lb.color.a;
            var q = n.GetComponentInChildren<ImageQuad>(true);
            return q != null ? q.Tint.a : -1f;
        }
        /// <summary>自检：显式推一帧（= `Tick` 里动画那一半；不碰 `BlinkGraphic`）。</summary>
        public void TickForTest(float dt) { TickAnimation(dt); }

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
