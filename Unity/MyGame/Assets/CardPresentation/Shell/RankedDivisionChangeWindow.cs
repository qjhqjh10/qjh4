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
// · **段位大图 / Rank Icon / Rank Number Glow / 两颗模式图标**：`m_Sprite = 0` ⇒ 节点照建、**拿到号才画**
//   （`Rank Icon` 自 2026-10-30 `B99_G3` 起由 `FeedRankIcon()` 喂；其余三件本机仍不画）+ 各出声一次。
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
            Build(); PointerLayer.SetInputEnabled(false);   // 🆕 B90-N1：原版 `Open()` 第一件事 = `EventSystemController.Toggle(false)`（判据见类尾 `SafetyInputEnabler`）
            StartSequence();     // = 原版 `Open()` 尾：`Play("Division Config")` + 起 promote 计时
        }

        /// <summary>换一份内容（复用同一扇窗时用；`GameWindow.TryOpen` 在 `Open` 态照原版早退 ⇒ 必须显式调本方法）。
        /// ⚠️ 换内容**不重播**开场演出 ⇒ 只在序列**已过 promote 那一拍**时才停播（病史与判据 → 类尾 `B90-F2` 那段）。</summary>
        public void Show(Context? ctx = null)
        {
            _ctx = FillDef(ctx);
            Build();
            if (_promoteDelay < 0f) StopAnimation();   // 🔴 B90-F2：promote 未起播时⛔不许掐序列（否则演出整段不播、输入只能等 10.4s 兜底）
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

            // ---- 1) `Menu Dark Background` —— 🔴 2026-10-27（B88_E6）就地订正（铁律 5）：原写「`BackgroundCloseButton.window = 0` ⇒ 点暗底**不**关窗」不成立 —— `window=0` 只关**基类**那条自动关窗，派生窗自己加了监听（`RankedEventDivisionChangeWindow[NewSystem]__OnEnable.c:15-22` = `backgroundCloseButton(+0x78/+0x70).onClick.AddListener(CloseButton)`；`…__CloseButton.c:9` 首句 = 虚表 `+0x1b8` = `Close()`）⇒ **点暗底会关窗**。
            _dark = MenuDraw.Node(transform, "Menu Dark Background", DarkR);
            MenuDraw.Rect(_dark, CardArt.Solid(), DarkR, "Image", QShade, ShadeTint); MenuDraw.ShadeHit(_dark, DarkR, QShade, QTitle, () => Close(), "BackgroundCloseButton");

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

            // ---- 3) `Division Image`（段位大图）----
            _division = BuildDivisionImage();
            DivisionArtUsed = DivisionArtOf();     // 段位号 → 图名（表 = `RankedDivisionInfo.DivisionArt`，7 条字节证据）
            if (string.IsNullOrEmpty(DivisionArtUsed))
            {
                DivisionArtBlank = true;
                // 🔴 2026-10-27（B88_E10）就地订正（铁律 5）：原写「本机没有段位号→图名对照表」**已过期** ——
                //   表就在 `RankedDivisionInfo.DivisionArt`（判据 → 那份文件的 doc：`RankedDivisionsSO.asset` 7 条 + Sprite GUID 逐条相等）。
                //   现在只剩这一种情况不画：**本窗没拿到段位号**（原版由服务端 `RankedDivisions.GetDivisionData` 给）。
                NoteNoArt("Division Image", "本窗没拿到段位号（`Context.DivisionLabel` 为空且 `DivisionNumber` 未设）"
                                          + "⇒ 段位大图不画（节点照建，`_Blend` 挂载点保留）");
            }
            // ⚠️ 原版同一处（`ChangeParticleEffectAndSpriteRank`）还换 `ParticleSystem` —— 粒子那半归特效线（容器 `Particles DivisionUp`/`Rank Up` 仍是空壳）。
            DivisionArtBlank = string.IsNullOrEmpty(DivisionArtUsed);

            // ---- 4) `Division Image/Rank Icon`（+ 出厂关着的 `Rank Icon Glow`）----
            //   原版：`Rank Icon` 布局 67.418²、`m_LocalScale = 1.090329` ⇒ 视觉 73.51²。
            //   🔴 这里按**视觉框**建、`localScale` 保持 1（理由见 `RankIconR` 的注释）。
            _rankIcon = BuildRankIcon();
            _rankGlow = BuildRankIconGlow();
            _rankGlow.gameObject.SetActive(false);   // 出厂 `m_IsActive = false`
            FeedRankIcon();   // 名次号 → 图（原版 `GetRankData` 那条链；实现见类尾 —— 没号就不画 + 出声，与原话同一句）

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
            var pfx = RankedDivisionParticles.Build(transform);   // 🆕 账 `221`：两棵粒子树按原版建（判据 JSON + 生成器见那个文件头）
            _particlesDivision = pfx.Division;   _sparksBurnDivision = pfx.SparksBurnDivision;   _divisionPs = pfx.DivisionPs;
            _particlesRank = pfx.Rank;   _rankPs = pfx.RankPs;
            // 🆕 账 `221`（2026-10-29）：这一条 `NoteNoArt` 已**删掉** —— 粒子不再是空壳（原来写的是
            //   「只建容器节点 + 取不到 `ParticleSystem` 时逐条出声一次」）。判据 JSON 取不到 / 材质取不到时由
            //   `RankedDivisionParticles.Build` 自己**出声一次**（⛔ 不静默失败）。
            //   两棵树合计 **14 颗 `ParticleSystem`**（`Particles DivisionUp` 6 + `Particles Rank Up` 8）。

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
            // 🔴 原版 `MilestonesStepDisplay.Initialize(currentPoints, totalPoints, …)` 会按 `totalPoints` 建**那么多**颗
            //   （`stepPrefab[i]`，越界用 `Last()`）；我们只烘了 prefab 里的 4 颗模板（`StepR.Length`）⇒ 总数更多时
            //   **摆不出多出来的那几颗**（原版靠 `steps` 上的 VLG 自动排）。⛔ 不许静默少画：出声一次。
            if (_ctx.SealTotal.HasValue && _ctx.SealTotal.Value > n && !_warnedSealTotal)
            {
                _warnedSealTotal = true;
                Debug.LogWarning("[RankedDivChange] 印玺总数 " + _ctx.SealTotal.Value + " > prefab 的 " + n
                               + " 颗模板 ⇒ **多出来的那几颗不建**（原版 `MilestonesStepDisplay.Initialize` 按 "
                               + "`totalPoints` 实例化、由 `steps` 的 VLG 排版；我们的 4 个矩形是烘死的绝对框）。"
                               + "本地拿不到 `TotalSeals`（服务端）⇒ 这条分支平时走不到。");
            }
            // 出厂（`SealCurrent == null`）= 全空（prefab 的 `Fill` alpha 0）
            int cur = _ctx.SealCurrent.HasValue ? Mathf.Clamp(_ctx.SealCurrent.Value, 0, n) : 0;
            MilestonesStepDisplay.Initialize<ImageQuad>(cur, n, _sealFills.ToArray(), null, null, q => q, (q, filled, i, ps, pa) => ToggleSeal(i, filled), null, false, false);   // 🆕 2026-10-28 B92-N4c：逐颗 `Toggle(currentPoints > i)` 改走原版同名件的算法核（`stepPrefab` = 已建好的 Fill 句柄，`instantiate` 恒等；销毁/建件仍在 `Build()`）
        }

        /// <summary>`ApplySeals` 那条「总数超出模板」的告警只出声一次（同 `_noArtWarned` 那套口径）。</summary>
        static bool _warnedSealTotal;

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
        //      `AnimateUIMaterialProperty.floatProperty1` 上 —— 🔴 逐帧求值后写 `WarpforgeVFX.AnimateUIMaterialProperty`（2026-10-27 真驱动）
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
            public MatTrack[] materialPaths;
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
            var matList = new List<MatTrack>();

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
                    case "materialFloat": matList.Add(new MatTrack { path = c.path, a = BuildCurve(c) }); break;
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
            _promoteDelay = PromoteDelay(StepR.Length); SafetyInputEnabler();   // 🆕 B90-N1：原版 `Open()` 在 `Toggle(false)` 之后紧接 `StartCoroutine(SafetyInputEnabler())`
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
            if (dt < 0f) dt = 0f; TickInputSafety(dt);   // 🆕 B90-N1：8/9 秒兜底（= 原版 `SafetyInputEnabler` 那条协程；本窗的 `Tick` 就是我们的协程泵）
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
                    ApplyParticleAndGlow(); PlayClip(_ctx.IsDivisionChange ? ClipKeyDivisionUp : ClipKeyRankUp);
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
                _lastFinishedKey = _playingKey; DispatchClipEvents(_rt);   // 🆕 B90-N1：原版 clip 里的 `AnimationFinished` 动画事件（我们两条 clip 的事件时刻都 == clip 长度 ⇒ 落在这一跳）
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

            // ⑥ `AnimateUIMaterialProperty.floatProperty1`（原版那是一个改材质 shader 浮点的组件；2026-10-27 起真驱动）
            if (_rt.materialPaths != null)
                for (int i = 0; i < _rt.materialPaths.Length; i++)
                {
                    var m = _rt.materialPaths[i];
                    var n = NodeAt(m.path);
                    if (n == null) continue;
                    var amp = n.GetComponent<WarpforgeVFX.AnimateUIMaterialProperty>();
                    if (amp == null) { WarnNoMaterialTarget(m.path); continue; }
                    var s1 = amp.floatProperty1; s1.value = m.a != null ? m.a.Evaluate(t) : s1.value; amp.floatProperty1 = s1;
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

        // ============================================================ `MilestoneStep.Toggle`（原版 · 印玺步点的两态 + 入场弹性缩放）
        //
        // 【判据】方法体 = `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/MilestoneStep.cs:53-113`（整类方法体，第一权威）。
        //   逐句对应（原版 → 我们）：
        //     `checkMark.enabled = option`                      → `Fill` 那颗 quad 的 `SetActive(on)`
        //     `emptyMark.enabled = option ? alwaysShowEmptyMark : true` → `Empty` 那颗 quad
        //     `if (!(doEnterAnimation & option)) return;`       → 同一个闸（**动的是 `doEnterAnimation`，⛔ 不是入参 `playAnim`**）
        //     `checkMarkImage.color` 的 alpha 先置 0、`OnStart` 里还原 → `Tint` 的 alpha（**原版没有 DOFade**，别加）
        //     `DOScale(localScale × animationScaleInitialMultiplier, animationTime).SetEase(OutElastic)
        //        .SetDelay(index × between + start).From()`     → 同一串（`.From()` ⇒ **起点 = 传进去那个大值、终点 = 当前 1 倍**）
        //     `OnStart` 里 `if (playSound) cue.Play2D()`        → 本类**不发声**：三个调用点一律 `playSounds: false`
        //        （`RankedEventDivisionChangeWindowNewSystem.cs:421` · `RankedMenuContainer.cs:197` · `RankingDisplay.cs:182`）
        //        ⇒ 原版这条也在**不响**的那一支；cue（`soundOnAppear`）本地未解出，如实留空。
        //
        // 【出厂值】逐 prefab 实读（`D:/2/tools/assetripper_out/ExportedProject/Assets/Ranked Division Change Window.prefab`
        //   的 4 个 `MilestoneStep` 块；另见 `RankedSealStep.prefab` / `RankedSealStep Division Change Variant.prefab`）：
        //     第 0 颗（= `RankedSealStep Division Change Variant` 那一档）：
        //        alwaysShowEmptyMark **1** · doEnterAnimation **1** · scaleMultiplier **3** · time **0.6** · between **0.2** · start **0.25**
        //     第 1–3 颗（= `RankedSealStep` 那一档）：
        //        alwaysShowEmptyMark **0** · doEnterAnimation **1** · **3** · **0.8** · **0.3** · **0.5**
        //   ⇒ 延迟逐颗不同（0.25 / 0.80 / 1.10 / 1.40），而 `GetTotalAnimationTime()` 只读**第 0 颗**
        //     （`MilestonesStepDisplay.cs:62-75`）⇒ `(n−1)×0.2 + 1.05` —— 与 `PromoteDelay` 那条公式同源。
        //
        // 【我们侧的形状】`Fill` / `Empty` 是 `MenuDraw.Rect` 建出来的两颗**叶 quad**
        //   （子树里按名字找：`Fill` 由 `_sealFills` 直接持有、`Empty` 走 `step.Find("Empty")`）；
        //   `ImageQuad` 的尺寸由网格参数定、`localScale` 恒 1（同 `BuildStageMarkToggleTween` 那条 ⚠️）
        //   ⇒ 缩它只影响画面、不会把几何毁掉。
        /// <summary>一颗 `MilestoneStep` 的出厂参数（字段名照原版；值 = prefab 序列化实读，见上面那段）。</summary>
        public struct MilestoneStepCfg
        {
            public bool AlwaysShowEmptyMark;
            public bool DoEnterAnimation;
            public float ScaleMultiplier;
            public float Time;
            public float BetweenStepsDelay;
            public float StartDelay;
        }

        /// <summary>`Ranked Division Change Window.prefab` 那 4 颗的出厂参数（下标 = 步点序号；越界取最后一档）。</summary>
        public static readonly MilestoneStepCfg[] SealCfgs =
        {
            // 第 0 颗 = `RankedSealStep Division Change Variant`
            new MilestoneStepCfg { AlwaysShowEmptyMark = true,  DoEnterAnimation = true,
                                   ScaleMultiplier = 3f, Time = 0.6f, BetweenStepsDelay = 0.2f, StartDelay = 0.25f },
            // 第 1–3 颗 = `RankedSealStep`
            new MilestoneStepCfg { AlwaysShowEmptyMark = false, DoEnterAnimation = true,
                                   ScaleMultiplier = 3f, Time = 0.8f, BetweenStepsDelay = 0.3f, StartDelay = 0.5f },
            new MilestoneStepCfg { AlwaysShowEmptyMark = false, DoEnterAnimation = true,
                                   ScaleMultiplier = 3f, Time = 0.8f, BetweenStepsDelay = 0.3f, StartDelay = 0.5f },
            new MilestoneStepCfg { AlwaysShowEmptyMark = false, DoEnterAnimation = true,
                                   ScaleMultiplier = 3f, Time = 0.8f, BetweenStepsDelay = 0.3f, StartDelay = 0.5f },
        };

        /// <summary>步点 <paramref name="i"/> 的出厂参数（越界 ⇒ 最后一档，⛔ 不返回 default）。</summary>
        public static MilestoneStepCfg SealCfgOf(int i)
        {
            if (SealCfgs.Length == 0) return new MilestoneStepCfg { DoEnterAnimation = false, ScaleMultiplier = 1f };
            return SealCfgs[Mathf.Clamp(i, 0, SealCfgs.Length - 1)];
        }

        /// <summary>= 原版 `MilestoneStep.Toggle(option, index, playSound, playAnim)`（**只做两态 + 入场缩放**）。
        /// 步点序号 = <paramref name="i"/>（= 原版那个 `index`，既是参数表下标也是延迟倍数）。
        /// <para>⚠️ 原版的 `playAnim` 形参**在方法体里根本没被读**（读的是序列化字段 `doEnterAnimation`）
        /// ⇒ 本方法**不带**那个形参，⛔ 别把它当成「要不要播动画」的开关。</para></summary>
        public void ToggleSeal(int i, bool on)
        {
            if (i < 0 || i >= _sealFills.Count) return;
            var fill = _sealFills[i];
            var cfg = SealCfgOf(i);
            var step = fill != null ? fill.transform.parent : null;
            var empty = step != null ? step.Find("Empty") : null;

            // ① `checkMark.enabled = option`
            if (fill != null) fill.gameObject.SetActive(on);
            // ② `emptyMark.enabled = option ? alwaysShowEmptyMark : true`
            if (empty != null) empty.gameObject.SetActive(!on || cfg.AlwaysShowEmptyMark);
            // ③ 入场动画：`doEnterAnimation && option`（⛔ 不是 playAnim）
            if (!on || !cfg.DoEnterAnimation || fill == null) return;
            if (!Application.isPlaying)
            {
                // 批处理没有帧循环（DOTween 不推进）⇒ 直接落到终态，⛔ 不起一条永远停在起点的补间
                //（同 `DraftModeWindow.PlayPackOpenAnim` / `CollectionWindow.PlayCardAnimateIn` 的闸）
                fill.SetTint(Color.white);
                return;
            }
            BuildSealEnterTween(fill, i, cfg);
        }

        /// <summary>`MilestoneStep.Toggle` 的那条入场补间（拆出来给自检用 `CardTween.Mode = Manual` 手动推）。
        /// <para>⚠️ 缓动必须经 <see cref="CardTween.Use"/> 的 `ease` 形参传（它在里面 `SetEase`；
        /// 自己先 `SetEase(OutElastic)` 再交给它会**被覆盖成 Linear** —— 实测过）。</para></summary>
        public static DG.Tweening.Tween BuildSealEnterTween(ImageQuad fill, int index, MilestoneStepCfg cfg)
        {
            if (fill == null) return null;
            var tr = fill.transform;
            // 原版先把 `checkMarkImage.color` 的 alpha 置 0（**不是** DOFade），`OnStart` 里还原成原色
            var on = Color.white;
            fill.SetTint(new Color(on.r, on.g, on.b, 0f));
            // `DOScale(当前 × multiplier, time).SetEase(OutElastic).SetDelay(index×between + start).From()`
            //   `.From()` ⇒ **起点 = 传进去那个大值**（当前缩放 × 3）、**终点 = 当前缩放**（= 1 倍）
            DG.Tweening.Tweener tw = DG.Tweening.ShortcutExtensions.DOScale(tr, Vector3.one * cfg.ScaleMultiplier, cfg.Time);
            tw = DG.Tweening.TweenSettingsExtensions.SetDelay(tw, index * cfg.BetweenStepsDelay + cfg.StartDelay);
            tw = DG.Tweening.TweenSettingsExtensions.From(tw);
            // 本文件没有 `using DG.Tweening;`（⚠️ 别为这两行加 —— 会和 `Ease`/`Sequence` 之类撞名）⇒ 一律走静态全名
            DG.Tweening.TweenSettingsExtensions.OnStart(tw, () => { if (fill != null) fill.SetTint(on); });
            // 与 `BuildStageMarkToggleTween` 同一条纪律：走完 / 被打断都把缩放显式复位
            //（批处理下 `OnComplete` 不一定跑得到；这颗的缩放本来恒 1）
            DG.Tweening.TweenSettingsExtensions.OnComplete(tw, () => { if (tr != null) tr.localScale = Vector3.one; });
            DG.Tweening.TweenSettingsExtensions.OnKill(tw, () => { if (tr != null) tr.localScale = Vector3.one; });
            return CardTween.Use(tw, DG.Tweening.Ease.OutElastic, fill);
        }

        /// <summary>`materialFloat` 通道的曲线（`floatProperty1.value`，即原版喂给材质的那一个浮点）。
        /// 🔴 2026-10-27（`B9_W20WIRE`）由「一条没接的 `string[]`」升级成「路径 + 曲线」——
        /// 原来那一版只存 path、只无条件出声，等于这一族曲线**从来没被驱动过**。</summary>
        struct MatTrack { public string path; public AnimationCurve a; }

        /// <summary>本窗那两个节点上**还没有** `AnimateUIMaterialProperty` 时出声（每个 path 只说一次）。
        /// <para>🔴 2026-10-27 就地订正（铁律 5）：这里原来写「那个组件我们工程里没有」—— **过期**。
        /// 组件 **2026-10-26 就建好了**：`WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs`
        /// （逐句对齐原版，`B8_AS` 建的）。现在缺的是**挂载目标**：`Division Image` / `Rank Icon` 这两颗
        /// 我们**没有画**（原版 `m_Sprite = 0`，图由服务端喂；见本文件 `Build()` 第 3/4 段与 `NoteNoArt`）
        /// ⇒ 它们身上没有 `MeshRenderer`，`Attach(Renderer, …)` 无处可挂。</para>
        /// <para>还差什么：等这两颗有图（有 `ImageQuad`）时，在 `Build()` 里补
        /// `WarpforgeVFX.AnimateUIMaterialProperty.Attach(quad.GetComponent<MeshRenderer>(), Slot("_Blend", 0f))`
        /// 即可 —— 本方法这段驱动会自动接上（它只认组件在不在）。</para></summary>
        void WarnNoMaterialTarget(string path)
        {
            WarnChannel("mat:" + path,
                        "`" + path + "` 上没有 `AnimateUIMaterialProperty` ⇒ 原版 `floatProperty1.value`（`_Blend`）"
                      + "那条曲线**没接**：组件早就在 `WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs`，"
                      + "🔴 2026-10-27 就地订正：`Division Image`/`Rank Icon` 已由 `BuildDivisionImage`/`BuildRankIcon` 补上载体；走到这里说明该 path 的节点上没有 `ImageQuad`。");
        }

        // ============================================================ 材质挂载载体（2026-10-27 · B9_W28MOUNT）
        //
        // 判据（现读 `bundle_menus_assets_all`，逐字段）：
        //   · `GameObject/Division Image.json` 的 `m_Component` 序 =
        //       `RectTransform → CanvasRenderer → Image(pid -7044926665261084338) →
        //        AnimateUIMaterialProperty(pid 4555674787176354126) → Canvas`
        //   · `GameObject/Rank Icon.json` 同形（Image pid -691165018575761074 · AMP pid -6557930150585593522）
        //   · 两个 AMP 的字段逐字相同：`image` PPtr 指向**同一个 GameObject** 上的 `Image` ·
        //     `initializeOnEnable = 1` · `floatProperty1 = { _Blend, 0 }` · `floatProperty2` 空槽 ·
        //     `changeMaterialOnAnimation = null`
        // ⇒ AMP 的挂载目标是「这颗节点自己的 `Image`」。我们这边那颗 `Image` 的等价物 =
        //   同一个 GameObject 上的 `ImageQuad`（它自带 `MeshFilter/MeshRenderer`，见 `Battle/ImageQuad.cs:127-137`）。
        //
        // ⚠️ **图仍然是空的、这是对的**：原版两颗 `Image.m_Sprite = 0`（本机没有「段位号→图名」对照表），
        //   图由服务端 `RankedDivisions.GetDivisionData/GetRankData` 的 `AssetReferenceTyped` 在运行期喂，
        //   而 `Open()` 在 `sprite == null` 时 `SetActive(false)` ⇒ 屏幕上什么都没有。
        //   `m_Sprite = 0` 在 uGUI 里本来是「全白贴图 × `m_Color`」的**纯白块**，照画会是一块 851×851 的白板
        //   ⇒ 载体 quad 的 tint **α = 0**：画不出东西，但材质通道挂得上（⛔ 不是 `MenuDraw.Node` 那种空节点）。
        //   服务端那一半 = P3（`资料/完全复刻/判据/服务器与账户_不复刻清单_第2轮.md`），⛔ 本机不编图。
        //
        // ⚠️ 这两颗**没有** `UIBorderGlow`（与卡片展示窗那两件不同）⇒ 不存在「两个组件抢材质」。
        //
        // 🔴 账里 `RankedDivisionChangeWindow.cs` 的落点是 `:475` / `:1163` ⇒ 所以这两件只在 `Build()` 里
        //   占**一行**（原来是 `MenuDraw.Node(...)` 那一行），实现全部追加在类尾，行号零漂。

        /// <summary>建 `Division Image`（+ 挂载载体 + 材质组件）。失败时退回空节点并**出声**。</summary>
        Transform BuildDivisionImage()
        {
            // 🆕 2026-10-27（B88_E10 · 账 `个人资料_成就_排行:221`）：段位大图**真的画出来**。
            //   判据 = 原版 `ChangeParticleEffectAndSpriteRank(Image, ParticleSystem, Sprite)` 的**第一件事**
            //   （`decomp_full/RankedEventDivisionChangeWindowNewSystem__ChangeParticleEffectAndSpriteRank.c`：
            //    `if (rankSprite != 0) Image.set_sprite(rankSprite)`）—— 我们这一侧 = 把 `MenuDraw.Rect` 的
            //    **纹理**从纯色换成那张段位图、**alpha 从 0 提到 1**（原来那两格是「没有图」的占位）。
            //   ⚠️ 图名来自 `RankedDivisionInfo.DivisionArtName`（7 条字节证据，⛔ 不在这里另编一张表）。
            string art = DivisionArtOf();
            var tex = string.IsNullOrEmpty(art) ? null : Tex(art, "`Division Image` 的段位大图");
            bool drawn = tex != null;
            var quad = MenuDraw.Rect(transform, drawn ? tex : CardArt.Solid(), DivisionR, "Division Image", QDivision,
                                     new Color(1f, 1f, 1f, drawn ? 1f : 0f));
            if (quad == null)
            {
                // `MenuDraw.Rect` 整块落在裁切框外 ⇒ 不建（既有语义）。这一档丢的是**材质挂载点**，不是图。
                WarnChannel("carrier:Division Image",
                            "`Division Image` 的载体 quad 没建出来（整块落在裁切框外）⇒ 原版挂在它上面的 "
                          + "`AnimateUIMaterialProperty`（`_Blend`）**没有挂载目标**");
                return MenuDraw.Node(transform, "Division Image", DivisionR);
            }
            WarpforgeVFX.AnimateUIMaterialProperty.Attach(
                quad.GetComponent<MeshRenderer>(),
                WarpforgeVFX.AnimateUIMaterialProperty.Slot("_Blend", 0f));
            return quad.transform;
        }

        // ============================================================ 段位大图（2026-10-27 · B88_E10 · 账 `221`）
        /// <summary>`Division Image` 上现在画的是哪张图（= `RankedDivisionInfo.DivisionArtName` 的返回值；
        /// 空串 = 没画）。自检读口。</summary>
        public string DivisionArtUsed { get; private set; }

        /// <summary>段位号（原版由服务端 `RankedDivisions.GetDivisionData` 给；本地没有 ⇒ 调用方自己设）。
        /// ⚠️ 追加在本窗**类尾之后**（`Build()` 之前不动）—— `Context` 是 struct，往里加字段会让
        /// 本文件 `:475/:753/:764/:774/:823/:1163` 那几条账落点整体下移，按铁律 18·b 一律不动它。</summary>
        public int? DivisionNumber;

        /// <summary>段位号 → 图名：`DivisionNumber` 给了就用它；否则拿 `Context.DivisionLabel` 在
        /// `RankedDivisionInfo` 的官方段位名表里**反查**（`Ranked/Division/1..7` = Iron..Legendary，现读全在
        /// `official_loc_13lang.json`）。两路都取不到 ⇒ 空串（调用方 `NoteNoArt` 出声、不画）。
        /// ⛔ 这里**不编**段位号、也不退回演示值 —— 那会画出一张原版此刻不会画的图。</summary>
        string DivisionArtOf()
        {
            if (DivisionNumber.HasValue) return RankedDivisionInfo.DivisionArtName(DivisionNumber.Value) ?? "";
            if (!string.IsNullOrEmpty(_ctx.DivisionLabel))
                for (int n = 1; n <= RankedDivisionInfo.DivisionArtCount; n++)
                {
                    string key = "Ranked/Division/" + n;
                    string nm = Loc.T(key);
                    if (!string.IsNullOrEmpty(nm) && nm != key && nm == _ctx.DivisionLabel)
                        return RankedDivisionInfo.DivisionArtName(n) ?? "";
                }
            return "";
        }

        /// <summary>建 `Rank Icon`（+ 挂载载体 + 材质组件）。失败时退回空节点并**出声**。</summary>
        Transform BuildRankIcon()
        {
            var quad = MenuDraw.Rect(_division, CardArt.Solid(), RankIconR, "Rank Icon", QRank,
                                     new Color(1f, 1f, 1f, 0f));
            if (quad == null)
            {
                WarnChannel("carrier:Rank Icon",
                            "`Rank Icon` 的载体 quad 没建出来（整块落在裁切框外）⇒ 原版挂在它上面的 "
                          + "`AnimateUIMaterialProperty`（`_Blend`）**没有挂载目标**");
                return MenuDraw.Node(_division, "Rank Icon", RankIconR);
            }
            WarpforgeVFX.AnimateUIMaterialProperty.Attach(
                quad.GetComponent<MeshRenderer>(),
                WarpforgeVFX.AnimateUIMaterialProperty.Slot("_Blend", 0f));
            return quad.transform;
        }

        // ============================================================ Rank Icon Glow 载体（2026-10-27 · B9_W39MAT）
        /// <summary>建 `Rank Icon Glow` 的载体（原版是一颗 uGUI `Image`，不是纯节点）。
        /// 判据（`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-8635674026998525618.json` 实读）：
        /// `m_Script` = uGUI `Image` · `m_Sprite = 0`（白图）· `m_Color = GlowTint`（黄 (1,0.9168,0,1)）·
        /// `m_Material` = `Material_-8464978790139598553.json`（`m_Name = UI Additive`，shader `Everguild/UI/Additive` = **加法**）。
        /// 🔴 就地订正（铁律 5 · 推翻 W28 的猜测）：那张材质 `m_SavedProperties` 里的 `_ColorMain 337.79…` /
        ///   `_Reveal 0.723` / `_Noise_Strength_X_Speed_Y_Scale_Z` **是上一颗 shader 的残留**
        ///   （`Everguild/UI/Additive` 的 `m_PropInfo` 只有 `_MainTex/_Color/_Stencil*`）⇒ 它**不是**
        ///   「靠 `_Reveal` 揭开的黄辉光」，而是「白图 × 黄 `m_Color` × 加法」的**加色块**，动画走
        ///   `Image.m_Color.a`（= 本文件 `ApplyChannels` 的 `alpha` 通道，已驱动 `ImageQuad.SetTint`）。
        /// 载体 = `ImageQuad` + 加法材质（`Resources/Art/vfx/UI Additive.mat`；工程等价 shader
        ///   `WarpforgeVFX/Sprites/Additive`，原版 `Everguild/UI/Additive` 本机未单独重建 —— 无 mask 的纯色块两者一致）。
        /// 出厂 `m_IsActive = false` 由调用点关（见 `Build()` 紧接的一行）。取不到材质 ⇒ 退回 `Sprites/Default` 并**出声**（⛔ 不静默）。</summary>
        Transform BuildRankIconGlow()
        {
            var quad = MenuDraw.Rect(_rankIcon, CardArt.Solid(), RankIconGlowR, "Rank Icon Glow", QRank, GlowTint);
            if (quad == null)
            {
                WarnChannel("carrier:Rank Icon Glow",
                            "`Rank Icon Glow` 的载体 quad 没建出来（整块落在裁切框外）⇒ 退回空节点；"
                          + "原版 `Image.m_Color.a` 那条曲线**没有挂载目标**");
                return MenuDraw.Node(_rankIcon, "Rank Icon Glow", RankIconGlowR);
            }
            var srcMat = Resources.Load<Material>("Art/vfx/UI Additive");
            if (srcMat == null)
            {
                WarnChannel("mat:Rank Icon Glow",
                            "取不到 `Resources/Art/vfx/UI Additive` 材质 ⇒ `Rank Icon Glow` 走 `Sprites/Default`（**非加法**）。"
                          + " 导入入口 = `工具/import_mainmenu_effect_mats.py --apply`");
            }
            else
            {
                quad.SetMaterial(new Material(srcMat));   // 每窗一份（SetTint/AMP 会改 `_Color`，别污染共享资产）
                quad.SetTint(GlowTint);                    // `SetMaterial` 不复制色 ⇒ 重设成黄
            }
            return quad.transform;
        }

        // ============================================================================================
        //  🆕 2026-10-28（B90-N1）：**弹窗期间禁输入 / 关窗·演完·超时后解禁**那条横切链
        //
        //  【判据 —— 第一权威，逐句现读 2026-10-28】`d:/2/tools/decomp_full/`：
        //   · `RankedEventDivisionChangeWindow__Open.c:38-52` / `…NewSystem__Open.c:228-240`：
        //       `GameWindow.Open()` → `EventSystemController.Toggle(false)` → `StartCoroutine(SafetyInputEnabler())`
        //   · `…__CloseButton.c:9-14` / `…NewSystem__CloseButton.c:9-14`：`Close()` → `Toggle(true)`
        //   · `…__AnimationFinished.c` / `…NewSystem__AnimationFinished.c`：`Toggle(true)`（clip 的动画事件）
        //   · `…_SafetyInputEnabler_d__15__MoveNext.c`：`WaitForSeconds(8f)` → `Toggle(true)`（**旧系统 = 8 秒定值**）
        //     `…NewSystem._SafetyInputEnabler_d__29__MoveNext.c`：`WaitForSeconds(<总动画时长 − 0.25f> + 9f)` → `Toggle(true)`
        //       （`0x1834b3208` = **9.0** · `0x1834b2e7c` = **0.25**，前者由 `工具/read_literal.py` 现读；
        //        `<总动画时长 − 0.25>` 正是本窗的 `_promoteDelay`（`PromoteDelayTrim = 0.25f` 同源））
        //   🔴 **反编译里那个调用显示成 `ChatPreviewMessage__Enable(this, v)` —— 那是 ICF 误标，不是聊天预览**：
        //      `ChatPreviewMessage : MonoBehaviour`（`dump.cs:44782`，**没有 `Instance`**），而这里取的是
        //      `SingletonBehaviour<object>.get_Instance(DAT_1842bdc50)` ⇒ 那个单例只能是
        //      `EventSystemController : SingletonBehaviour<EventSystemController>`（`dump.cs:104632`）；
        //      两个类的 `+0x20` 都是 `Behaviour`（`TextMeshProUGUI message` / `EventSystem eventSystem`）
        //      ⇒ 方法体逐字相同 ⇒ 符号被折叠成先导出的那个名字。
        //      旁证：`Shell/PointerLayer.cs` 的 `InputEnabled` doc 早就写着「`EventSystemController.Toggle` 与
        //      `ChatPreviewMessage.Enable` 同体 ⇒ ICF」。
        //   🔴 **范围也是现读出来的**：`RankedEventSeasonEndedWindow` / `…Old` / `RankedEventStartSeasonWindow`
        //      **三扇都没有这条链**（`__Open.c` 里没有 `Toggle/Enable` 调用，`__CloseButton.c` 的体只有 `Close()`）
        //      ⇒ 只有 `RankedEventDivisionChangeWindow` 与 `…NewSystem` 这两扇要禁输入。⛔ 别给另三扇也加上。
        //
        //  【我们的等价物】`PointerLayer.SetInputEnabled(bool)` = 原版 `EventSystemController.Toggle(bool)`
        //   （本仓没有 UGUI `EventSystem`，那一位就是「ESC 第一道门槛 + 唯一输入路」，见 `PointerLayer.cs` 那段 doc）。
        //  【形状差】原版是协程 + `WaitForSeconds`；我们没有帧循环以外的等待 ⇒ 用**同一个 `Tick` 里的倒计时**
        //   （`SafetyInputEnabler()` 只负责算出秒数，`TickInputSafety(dt)` 负责推进）。
        //  ⚠️ 两套系统的兜底时长不同（旧 8f 定值 / 新 `_promoteDelay + 9f`），本窗**一扇窗同时承载两套判据**
        //   ⇒ 取**新系统那一档**（更晚的那个，绝不会在演出途中解禁）。旧系统的 8f 记在这里备查。

        /// <summary>新系统兜底里的那个常数（判据 = `0x1834b3208` 现读 = 9.0）。</summary>
        public const float SafetyExtraDelay = 9f;
        /// <summary>旧系统兜底的定值（判据 = `…_SafetyInputEnabler_d__15__MoveNext.c` 的 `WaitForSeconds(8f)`）。</summary>
        public const float SafetyFlatDelayOld = 8f;

        float _inputSafetyLeft;

        /// <summary>= 原版 `Open()` 尾那句 `StartCoroutine(SafetyInputEnabler())`：**起兜底倒计时**并返回秒数。
        /// <para>时长 = `_promoteDelay + 9f`（新系统那一档；见类尾那段注释的两套值）。</para></summary>
        public float SafetyInputEnabler()
        {
            float d = Mathf.Max(0f, _promoteDelay) + SafetyExtraDelay;
            _inputSafetyLeft = d;
            return d;
        }

        /// <summary>每帧推进兜底倒计时（由 `TickAnimation` 第一句调 ⇒ 与演出同一口时钟）。</summary>
        void TickInputSafety(float dt)
        {
            if (_inputSafetyLeft <= 0f) return;
            _inputSafetyLeft -= dt;
            if (_inputSafetyLeft > 0f) return;
            _inputSafetyLeft = 0f;
            AnimationFinished();
            Debug.LogWarning("[RankedDivChange] 演出**没有在超时前报完成** ⇒ 走**兜底解禁**"
                           + "（= 原版 `SafetyInputEnabler` 那条 `WaitForSeconds`）；若每窗都出现这条，"
                           + "说明 clip 的 `AnimationFinished` 事件没跑到 —— 先查 `Resources/division_clips.json` 的 `events`");
        }

        /// <summary>原版 `AnimationFinished()`（clip 动画事件的接收器）= **只做一件事：解禁输入**。
        /// <para>🔴 账 `214`/`220` 原写「体 = `ChatPreviewMessage.Enable(true)`、我们从不关聊天预览 ⇒ 无需重开」——
        /// **两句都过期**（铁律 5）：真身是 `EventSystemController.Toggle(true)`，而且我们**现在就关**（见 `Open()`）。</para></summary>
        public void AnimationFinished()
        {
            PointerLayer.SetInputEnabled(true);
        }

        /// <summary>把 clip 的动画事件按时刻派发（= 原版 `AnimationEvent.functionName` 那一跳）。
        /// <para>本窗两条 clip 的事件都是 `(t = length, "AnimationFinished")`（`Resources/division_clips.json` 现读）
        /// ⇒ 等价落点 = clip 播完那一跳。遇到**不认识**的函数名 ⇒ 出声一次（⛔ 不静默）。</para></summary>
        void DispatchClipEvents(RtClip rc)
        {
            if (rc == null || rc.data == null || rc.data.events == null) return;
            for (int i = 0; i < rc.data.events.Length; i++)
            {
                var e = rc.data.events[i];
                if (e == null || string.IsNullOrEmpty(e.function)) continue;
                if (e.function == "AnimationFinished") { AnimationFinished(); continue; }
                WarnChannel("clipEvent:" + e.function,
                            "clip `" + rc.clip.name + "` 里的动画事件 `" + e.function + "` 我们**没有实现** ⇒ 这一拍什么都没做"
                          + "（原版 `AnimationEvent` 会调同名的 MonoBehaviour 方法）。");
            }
        }

        /// <summary>关窗 ⇒ 解禁（= 原版 `CloseButton()` 的第二句 `EventSystemController.Toggle(true)`）。
        /// <para>⚠️ 挂在 `Close()` 覆写上、⛔ 不只在 `CloseButton()` 里：原版 `CloseButton` 只是「点暗底 / 点关钮」那条路，
        /// 而本窗还能被 ESC / `WindowsManager.CloseWindow` 关掉 —— 任一条路漏接都会把**输入层永久留在停用态**。
        /// 原版在 `Close()` 返回后**无条件** `Toggle(true)`（哪怕首句 `!activeSelf` 早退了）⇒ 我们照抄这个次序。</para></summary>
        public override void Close()
        {
            base.Close();
            PointerLayer.SetInputEnabled(true);
        }

        /// <summary>自检读口：兜底倒计时还剩几秒（0 = 已解禁）。</summary>
        public float InputSafetyLeftForTest { get { return _inputSafetyLeft; } }

        // ============================================================================================
        //  🆕 2026-10-28（B90-F2）：**`Show()` 为什么不能无条件 `StopAnimation()`** —— 一次真·产品缺陷
        //
        //  【病史（现读证据）】生产入口只有一条：`Shell/WindowsManager.cs:1209` 的 `OpenRankedDivisionChange`
        //   = `OpenByRef(...)` **之后紧跟** `win.Show(ctx)`（`:1212`）。而新建那一支 `OpenByRef` 内部已经跑完了
        //   「`Create()` → `mgr.OpenWindow(win)` → `GameWindow.TryOpen` → `Open()`」（`WindowsManager.cs:914` / `:330`），
        //   而 `Open()`（本文件 `:220`）= `Build(); PointerLayer.SetInputEnabled(false); StartSequence();`，
        //   `StartSequence()`（`:753`）刚把 `_promoteDelay` 置成 `PromoteDelay(4) = 1.40s`、兜底 `_inputSafetyLeft = 10.40s`。
        //   ⇒ 紧跟的 `Show()` 原来那句**无条件** `StopAnimation()` 把 `_promoteDelay` 抹成 `-1`（`:768`），
        //   而它**不动 `_inputSafetyLeft`** ⇒ `TickAnimation` 的「① 等 promote 那一拍」那一支（`:827`）永远进不去
        //   ⇒ `PlayClip(rankUp / divisionUp)` **一次都不播**，只剩 `TickInputSafety` 在跑 ⇒
        //   **演出整段不播 + 输入要等 10.40 秒兜底才解禁**。
        //   实测：`_tmp_view/menu.log` 有 `[RankedDivChange] 演出**没有在超时前报完成** ⇒ 走**兜底解禁**`；
        //   自检实测「兜底还剩 0 秒 · 动画时间 0 / 时长 0.217 · 推了 521 步」——
        //   521 × 0.02 = 10.42 ≈ 10.40，而 `0.217` 正是 `Division Config` 的长度（`Resources/division_clips.json` 现读）
        //   ⇒ 证据链闭合：**停在了 config 那一条、promote 根本没起播**。
        //
        //  【判据为什么站在「保序列」这边】原版 `Open()`（`RankedEventDivisionChangeWindowNewSystem__Open.c:228-240`）
        //   之后**没有任何「再关一次动画」的入口**；`StopAnimation` 这一跳**是我们为「复用换内容」补的**
        //   （原版没有 `Show(ctx)` 这条入口，见该方法 doc；原版 `WindowsManager.OpenWindowCO` 只走 `TryOpen`）。
        //   ⇒「换内容时停在出厂姿态」这条意图**只在序列已播完 / 已过 promote 时有意义**；
        //   序列还没走到 promote 就掐掉 = **把原版会播的那段演出整个吞掉**（不是「停得更早」，是「一步都不走」）。
        //
        //  【改法（二选一里选了②）】只给那一句加闸：`if (_promoteDelay < 0f) StopAnimation();`
        //   ⛔ 没有选方案①「`OpenRankedDivisionChange` 干脆不调 `Show(ctx)`」—— 那会**连带砍掉复用支的内容刷新**：
        //   复用支的 `TryOpen` 在原版与本仓都是「一个字段都不写就 return」的早退（`GameWindow.cs:516-545`），
        //   `Show(ctx)` 是那一路**唯一**把新 `ctx` 铺到树上的入口（`WindowsManager.cs:901-906` 那段注释正是为它写的）
        //   ⇒ 删掉它 = 拿一个真缺陷换另一个真缺陷。
        //   ✅ `Build()` 照旧每次都跑（换内容必须重建）；重建**不影响进行中的序列** ——
        //   `_nodeCache` 由 `Build()` 清空（`:257`），`SampleAnimation` 与 `ApplyChannels`/`NodeAt` 都按 **path 每帧重解析**，
        //   而原生 `Animation` 那份只是旁路（批处理里它本来就推不动，见 `PlayClip` 的 `WarnChannel`）。
        //
        //  【谁还会走到这里（`grep -rn "\.Show(\|StopAnimation()" Shell/ Editor/` 现读全量）】
        //   · `Show(Context?)` 的**唯一**调用方 = `Shell/WindowsManager.cs:1212`。
        //   · `StopAnimation()` 的调用方只有两处、都在本文件：`Show()`（本闸）与 `StartSequence()` 首句（`:755`）
        //     —— 后者**必须**停：那是「重开一条新序列」的复位，不是「掐断一条没起播的演出」。
        //   · 同族 `OpenTutorialMessage` / `OpenWelcome` / `OpenRankedNewSeason` / `OpenRankedSeasonEnded` /
        //     `OpenGiveFeedback` 也是「`OpenByRef` 之后紧跟 `Show(ctx)`」，但那五扇的 `Show()` **里没有动画**（只 `Build()`）
        //     ⇒ 本缺陷**只在升段窗这一扇**成立，⛔ 别顺手去改那五扇。
        //
        //  【还欠什么】① 反编译里**没有查到**与原版「重开时停播」对应的方法（`dump.cs` 里 `StopAnimation` 只在
        //   `GameWindow` 家族），所以本闸的上界只能取「本仓自己的序列语义」—— 若将来查到原版另有一跳，要回来对齐；
        //   ② 生产入口 `OpenRankedDivisionChange` 目前**还没接进活界面**（只有 `Editor/MainMenuScene.cs` 在调），
        //   接上之后要真机看一次「`Division Change` 音效 → promote 演出 → 解禁」的次序。
        // ============================================================================================

        // ============================================================ 账 `221`（2026-10-29 · B97_PROFILE）
        //  `RankedEventDivisionChangeWindowNewSystem.ChangeParticleEffectAndSpriteRank(Image, ParticleSystem, Sprite)`
        //  的 ③④ 两跳。**全部成员追加在类尾**（本文件 `:475/:753/:764/:774/:823/:1163/:1410/:1433/:1459` 还有账落点，
        //  往中间插一行会让它们整体下移 ⇒ 铁律「新成员追加类尾」）。
        //
        //  【判据（逐句现读）】`decomp_full/RankedEventDivisionChangeWindowNewSystem__ChangeParticleEffectAndSpriteRank.c`
        //   ① `targetImage.sprite = rankIcon(+0x98).m_Sprite(+0xe0)`
        //   ② `targetImage.gameObject.SetActive(op_Implicit(rankSprite))`（= `rankSprite != null`）
        //   ③ `textureSheetAnimation.SetSprite(0, rankIcon.m_Sprite)`
        //   ④ `GetComponent<Renderer>().enabled = op_Implicit(rankSprite)`
        //  【调用侧 —— 谁是谁】`…__DoDivisionChangeAnimations.c:54-56` 传
        //   `(rankNumberGlow, divisionParticleSystem(+0xa8), rankIcon.m_Sprite)`；`…__DoRankChangeAnimations.c:54-56` 传
        //   `(rankNumberGlow, rankParticleSystem(+0xb0), rankIcon.m_Sprite)`。
        //   两个字段的真身（`MonoBehaviour_-4938671765695524530.json` 的 PPtr 现读）：
        //     · `divisionParticleSystem` = ps `1201952928742936910` = **DivisionUp ▸ Sparks Burn ▸ Promote Letter Effect**
        //     · `rankParticleSystem`     = ps `-6284612905696259762` = **Rank Up ▸ Sparks Burn ▸ Promote Letter Effect**
        //     · `rankNumberGlow`         = `-8635674026998525618`（= 我们的 `_rankGlow` 那颗加色块）
        //   ⇒ **第三参 `rankSprite` 与图源 `rankIcon.m_Sprite` 是同一个值**（调用侧传的就是它）——
        //     `B90-N1` 那句「第一件事 `if (rankSprite != 0) set_sprite`」是错读，已被 `B94-R-L` 推翻。
        //
        //  【本机会不会变】**不会**：`rankIcon.m_Sprite` 由服务端段位图喂（P3）⇒ 本机恒 `null`
        //   ⇒ ② `Rank Icon Glow` 保持 `SetActive(false)`、④ `Renderer.enabled = false`，与今天「空壳」的可观测结果**相同**
        //   （只多了一条：粒子现在真的建出来了，但它们的 renderer 是关的）。注入 `RankIconSprite` 后才可见。

        ParticleSystem _divisionPs, _rankPs;
        Sprite _rankIconSprite;
        bool _warnedNoGlowQuad;

        /// <summary>`rankIcon.m_Sprite` 的等价物（原版由服务端段位图喂 ⇒ 本机恒 `null`；自检可注入）。
        /// ⚠️ 我们的 `Rank Icon` 是 `CardArt.Solid()` 的纯色 quad（**没有 `Sprite` 概念**）⇒ 只能由外部注入。</summary>
        public Sprite RankIconSprite
        {
            get { return _rankIconSprite; }
            set { _rankIconSprite = value; }
        }

        /// <summary>自检读口：`Particles DivisionUp ▸ Sparks Burn ▸ Promote Letter Effect` 的 `Renderer.enabled`（= 原版 ④）。</summary>
        public bool DivisionParticleEnabled { get { return RankedDivisionParticles.RendererEnabled(_divisionPs); } }
        /// <summary>自检读口：`Particles Rank Up ▸ Sparks Burn ▸ Promote Letter Effect` 的 `Renderer.enabled`。</summary>
        public bool RankParticleEnabled { get { return RankedDivisionParticles.RendererEnabled(_rankPs); } }
        /// <summary>自检读口：两棵树的 `Promote Letter Effect` 各自那个 `ParticleSystem`（原版 ③ 的目标）。</summary>
        public ParticleSystem DivisionParticleForTest { get { return _divisionPs; } }
        /// <summary>自检读口，见 <see cref="DivisionParticleForTest"/>。</summary>
        public ParticleSystem RankParticleForTest { get { return _rankPs; } }

        /// <summary>③④ 的落地口。**由 `TickAnimation` 的 promote 那一拍调用**（原版次序 = 先 `Change…` 再 `Play(name)`）。</summary>
        void ApplyParticleAndGlow()
        {
            var sprite = _rankIconSprite;
            bool visible = sprite != null;

            // ① `targetImage.sprite = rankIcon.m_Sprite`：我们的 `targetImage`（`Rank Icon Glow`）是 `ImageQuad`
            //   （`CardArt.Solid()` 的白块 + 加法材质），没有 uGUI `Image.sprite` 这一位 ⇒ 等价物 = 换贴图。
            //   `sprite == null`（本机常态）⇒ 什么都不做（原版这时也会把 sprite 设成 null = 白图）。
            var quad = _rankGlow == null ? null : _rankGlow.GetComponent<ImageQuad>();
            if (sprite != null)
            {
                if (quad != null) quad.SetTexture(sprite.texture);
                else if (!_warnedNoGlowQuad)
                {
                    _warnedNoGlowQuad = true;
                    WarnChannel("carrier:Rank Icon Glow",
                                "`Rank Icon Glow` 上没有 `ImageQuad`（载体退回空节点）⇒ 原版 ①（`targetImage.sprite = rankIcon.m_Sprite`）"
                              + "**没有落点**；②④ 仍按 `rankSprite != null` 生效");
                }
            }

            // ② `targetImage.gameObject.SetActive(rankSprite != null)`
            if (_rankGlow != null) _rankGlow.gameObject.SetActive(visible);

            // ③ `textureSheetAnimation.SetSprite(0, rankIcon.m_Sprite)` + ④ `Renderer.enabled = (rankSprite != null)`
            var ps = _ctx.IsDivisionChange ? _divisionPs : _rankPs;
            if (ps == null)
            {
                WarnChannel("ps:" + (_ctx.IsDivisionChange ? "DivisionUp" : "RankUp"),
                            "本窗没建出 `Promote Letter Effect` 那颗 `ParticleSystem`（判据 JSON / 材质缺失）"
                          + "⇒ 原版 ③④ **没有落点**（`Renderer.enabled` 保持出厂 true，但那个 renderer 根本不存在）");
                return;
            }
            if (visible)
            {
                var uv = ps.textureSheetAnimation;
                uv.enabled = true;
                uv.SetSprite(0, sprite);
            }
            var r = ps.GetComponent<Renderer>();
            if (r != null) r.enabled = visible;   // ④
        }

        // ============================================================ 名次图（`Rank Icon` 的图源 · 2026-10-29 · B99_G3 · 账 `221` 同族）
        //  【原版链（第一手）】`decomp_full/RankedEventDivisionChangeWindowNewSystem__Open.c` 现读 `:172-197`：
        //    `rankData = RankedDivisions.GetRankData(divisions, last.+0x14)`   ← 名次号取自赛季段位配表
        //    `this.+0xe0 = rankData?.+0x18`（`AssetReferenceTyped<Sprite>`）
        //    `Image(+0x98).sprite = (this.+0xe0 != null) ? Load(this.+0xe0) : null`
        //    `SetActive(Image(+0x98).gameObject, op_Implicit(Image.sprite))`   ← **显隐 = 图有没有**
        //    之后 `Do{Rank,Division}ChangeAnimations` 把它交给 `ChangeParticleEffectAndSpriteRank`（= 本文件
        //    `ApplyParticleAndGlow` 的 ①②③④）⇒ 名次图一喂进去，`Rank Icon Glow` 与粒子那两跳自己成立。
        //    `GetRankData(n)` = `Math.Max(n,1)` + 按 `divisionNumber == n` 找，**找不到就是 null（没有 `Last()`）**
        //    （`decomp_full/RankedDivisions__GetRankData.c:25-30`）⇒ ≥7 不画；号本身来自服务器，本机没有那张表。
        //  🔴 本机拿得到的那一半 = **号 → 图名**（`RankedDivisionInfo.RankArt` 6 条，字节证据在那份文件的 doc）；
        //    拿不到的那一半 = **名次号本身**（服务器 `LeaderboardManager` / `rankedRanks` TitleData）
        //    ⇒ 只接线、⛔ **不编演示名次号**（口径同 `RankedDivisionInfo.RankArt` 的 doc 末段）。
        //  ⚠️ 全部成员追加在**类尾**：本文件 `:475/:753/:764/:774/:823/:1163` 还有账落点，插在中间会让它们整体下移。

        /// <summary>名次号（原版 `rankIcon` 的来源：`RankedDivisions.GetRankData(last.`+0x14`)` 的入参）。
        /// 1..6 对应 `RankedDivisionInfo.RankArt`（`Roman_I`…`Roman_VI`）。
        /// 服务器数据（`LeaderboardManager.GetCachedLeaderboard` / `rankedRanks` TitleData）⇒ 本地 default = `null`。</summary>
        public int? RankNumber;

        /// <summary>`Rank Icon` 上现在画的是哪张图（= `RankedDivisionInfo.RankArtName` 的返回值；空串 = 没画）。自检读口。</summary>
        public string RankArtUsed { get; private set; }

        /// <summary>名次号 → 图名（与 `DivisionArtOf` 同构）。两路依次：`RankNumber` →（本地真有阈值表时）
        /// `RankedData.CurrentDivision.RankNumber`。两路都取不到 ⇒ 空串（调用方出声、不画）。
        /// ⛔ 这里**不编**名次号、也不退回演示值 —— 那会画出一张原版此刻不会画的图。
        /// <para>🔴 号本身的两条边界照**机器码**（`decomp_full/RankedDivisions__GetRankData.c:25-30` 现读）：
        /// ① `param_2 = Math.Max(param_2, 1)` ⇒ **≤0 夹到 1**（= 画 `Roman_I`），不是「不画」；
        /// ② 之后只有 `FirstOrDefault(divisionNumber == n)`、**没有** `Last()` 兜底（`GetDivisionData.c:31-33`
        ///    才有那句 `if (lVar3 == 0) Last(...)`）⇒ **≥7 查不到 = `null` = 不画**。
        /// ⚠️ 施工判据 §3·1 写的「`GetRankData` 查不到退 `Last()`」是把 `GetDivisionData` 的那句错记到这边，
        ///    本实现按机器码走（只夹下限、不兜 `Last()`），勘误已写进报告。</para></summary>
        string RankArtOf()
        {
            int? n = RankNumber;
            if (!n.HasValue)
            {
                // `Known == false` = 本地没有 `rankedRanks` 阈值表（`RankedData.WhyNoRankTable`）⇒ 它的 `RankNumber`
                // 是 `default(int)` = 0，**不是**「第 0 档」⇒ 必须靠 `Known` 挡掉，⛔ 不能直接读。
                var d = RankedData.CurrentDivision;
                if (d.Known) n = d.RankNumber;
            }
            // ① `Math.Max(n,1)`（机器码原样）：0 / 负数 ⇒ 第 1 档，**不是**「不画」。
            return n.HasValue ? (RankedDivisionInfo.RankArtName(Math.Max(n.Value, 1)) ?? "") : "";
        }

        /// <summary>`Build()` 第 4 段的落点（原 `NoteNoArt` 那一行的替代）：按 <see cref="RankArtOf"/> 取图 →
        /// 换纹理 + **α 从 0 提到 1**（`BuildRankIcon` 建的是 α=0 的占位）→ 同时喂 `RankIconSprite`
        /// （`ApplyParticleAndGlow` 的 ①②③④ 全靠它判 `sprite != null`）。
        /// 拿不到图 ⇒ 保持原样 + `NoteNoArt`（**原话照旧**，可观测行为与改前一致，只是多了一条「有号就画」）。</summary>
        void FeedRankIcon()
        {
            string art = RankArtOf();
            RankArtUsed = art ?? "";
            var tex = string.IsNullOrEmpty(art)
                    ? null
                    : Tex(art, "`Rank Icon` 的名次图（`RankedDivisionInfo.RankArt`；名次号 1..6 ↔ `Roman_I`…`Roman_VI`）");
            if (tex == null)
            {
                // 🔴 原版这一支是 `Image(+0x98).sprite = null` + `SetActive(false)` ⇒ **图与 `Sprite` 都要清空**。
                //   不清就会「复用的窗把上一次的名次图/辉光留着」（`Show()` 走的是同一个实例，`_rankIconSprite`
                //   是字段、不属于 `Build()` 重建的那棵树）。⛔ 别只清 quad、不清 `RankIconSprite`（`ApplyParticleAndGlow`
                //   判的就是它）。
                var prev = _rankIcon == null ? null : _rankIcon.GetComponent<ImageQuad>();
                if (prev != null) { prev.SetTexture(CardArt.Solid()); prev.SetTint(new Color(1f, 1f, 1f, 0f)); }
                RankIconSprite = null;
                // 原话照旧（`NoteNoArt` 按 `what` 去重 ⇒ 与改前打的是同一句、同样只打一次）。
                NoteNoArt("Rank Icon", "`rankIcon.m_Sprite = 0`（运行期 `RankedDivisions.GetRankData(...)`）");
                return;
            }

            var quad = _rankIcon == null ? null : _rankIcon.GetComponent<ImageQuad>();
            if (quad == null)
            {
                // 载体落在裁切框外那一档（`BuildRankIcon` 退回空节点）⇒ 图没地方画。与 `carrier:Rank Icon` 分开记，
                // 免得被前面那条已经打过的 warn 吃掉。
                WarnChannel("art:Rank Icon",
                            "名次图 `" + art + "` 取到了，但 `Rank Icon` 上没有 `ImageQuad`（载体退回空节点）⇒ **没画**");
                return;
            }

            quad.SetTexture(tex);
            quad.SetTint(Color.white);          // α 0 → 1（原版在这一拍是 `Image.sprite = <图>`，`m_Color` 不动、仍是白）
            // `ApplyParticleAndGlow` 的 ①②③④ 要的是 `Sprite`（原版 `Image.sprite`）。同一张图复用同一个 `Sprite`，
            // 免得重复 `Build()` 每次 new 一个（那是托管 + native 两侧各一份）。
            if (_rankIconSprite == null || _rankIconSprite.texture != tex)
                RankIconSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
    }
}
