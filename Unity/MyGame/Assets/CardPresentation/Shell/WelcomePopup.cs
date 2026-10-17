// WelcomePopup.cs — 原版 **`WelcomePopup`**（`WelcomeScreen Alpha` prefab）—— 首启欢迎整屏
//
// ============================ 出处（唯一判据，全部本地现读）============================
// · prefab 树 / 矩形 / 组件 / 文字档位（2026-10-17 现读，可复跑）：
//     `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "WelcomeScreen Alpha" --depth 8 --relative --md`
//   根 = `bundle_menus_assets_all/GameObject/WelcomeScreen Alpha.json`，
//   根 MB = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_5425894111832748395.json`（窗参逐字段实读）：
//     `type = 1`(Popup) · `windowsPlacement = 15`(Popup) · **`closeOnESC = 0`** · `updateNavPanel = 0` ·
//     `extraScaleSmallScreen = 1.0` · `openSound = {0,0}` · `closeSound = {0,0}` · `useDefaultCloseSoundIfNull = 1`
//   字段 PPtr 实读：`warlordImage → Event image` · `trigger → Collider` · `titleText → Texts/TitleText` ·
//   `descriptionText → Texts/DescriptionText`。（**没有** `campaignPointDrawer` —— 这是它与
//   `Tutorial Message Window PopUp` 的唯一结构差别：本窗的 `Texts` 直接挂在根上，中间没有 `Container`。）
//   两个 prefab 共有的四件（压暗 / 红底 / Event image / Texts / Collider）**逐值相同**（两份 dump 亲比）
//   ⇒ 几何常量只留一处（`TutorialMessagePopup` 那几个 `public static readonly`），本文件直接取。
// · 行为（第一权威 = 反编译 `d:/2/tools/decomp_full/`）：
//   · `WelcomePopup__get_IsFirstWelcome.c`：**`!SegmentationController.CheckTag(0)`** —— 而 `PlayerTag`
//     的 **0 = `CollectedFirstDeck`**（`PlayerTag.cs`）⇒ **不是「首次启动」**，是**onboarding 的
//     「还没收集第一副牌」**。
//   · `WelcomePopup__Open.c`：① `GameWindow.Open()` ② 若 `CheckTag(CollectedFirstDeck)` 为真
//     ⇒ `GameAnalytics.SendOnboardingFinished()` + `AddTag(0x32 = 50 = FinishedOnboarding)`
//     ③ `titleText ← Context.Title` · `descriptionText ← Context.Description`（虚表 0x568 / 0x578）
//     ④ `Event image` ← `Context` 的 AssetReference / 活动督军 `fullHeroSprite` / liveop 资产图，
//        末句 **`image.enabled = (sprite != null)`** ⑤ `trigger.onClick.AddListener(Close)`
//   · `WelcomePopup__Close.c`：先 `RemoveListener`，再 `GameWindow.Close()`。
//     ⚠️ `WelcomePopup__OnClose.c` 的**方法体是符号错配**（内容是 `TutorialTipScript__StopTipPermanent`）
//     ⇒ 那一条**读不到**，本窗不实现 `OnClose` 的任何额外行为（如实记）。
//   · 触发：**全库无调用点**（按类名 / `IsFirstWelcome` 直搜只命中自身；组件 PathID 无外部 PPtr 引用；
//     场景里也没有实例）⇒ 本窗由 **FTUE onboarding 流程**开出来，而**我们整条 FTUE 没建**
//     （判据 → `资料/普查产出_1017/UI/R2_暂缓批复核.md` §1·1 与此文 §五·4）。
//
// ============================ 我们侧的对位（逐件写清，不冒充原版）============================
// · **显隐判据**：原版 = `!SegmentationController.CheckTag(CollectedFirstDeck)`（PlayFab 玩家 tag），
//   看完加 `FinishedOnboarding`。🔴 **我们没有任何 tag / onboarding 存档**（`grep Onboarding` 全仓 0 命中）
//   ⇒ `IsFirstWelcome` **恒 false + 不接显隐**（如实标注，⛔ 不拿「第一次启动」冒充它）。
//   见 `IsFirstWelcome` 的 doc。
// · **文案**：默认 `MainMenu/Welcome/Title` / `MainMenu/Welcome/Subtitle` / `Demo/Welcome/TapToContinue`
//   （= prefab 三颗上的 `Localize.mTerm`，与 Tutorial 窗同一套；取法/缺口 → `TutorialMessagePopup.OfficialTerm`）。
// · **`Event image`**：同 Tutorial 窗 —— 原版那一格是「Context 的 AssetReference / 活动督军立绘」，
//   两者都在远端 ⇒ 留 `Context.Image` 口（调用方给就画、不给就整层关掉 + 出声一次）。
// · **`Collider`**：整屏透明 Image + `EverguildButton`；原版 `Open()` 挂 `onClick += Close`
//   ⇒ **点屏幕任何地方 = 关窗**（它是最后一个兄弟、盖在面板与文字之上）。
//   ⚠️ `Menu Dark Background` 的 `BackgroundCloseButton.window` PPtr 实读是 **0** ⇒ 压暗层本身不关窗
//   （别给它加 `ShadeHit`）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `WelcomePopup`（`GameWindow` 子类）—— **首启欢迎整屏：立绘 + 标题 + 长正文 + 点击继续**。
    /// <para>🔴 **它和 `TutorialMessagePopup` 是同一套版面**（两份 prefab 的四件逐值相同），
    /// 差别只有两处：① 本窗**没有** `Container Campaign Points`；② 本窗的 `Texts` 是根的直子（Tutorial 也是直子）。</para>
    /// <para>🔴 **入口缺口（如实记）**：原版触发是 FTUE onboarding（`!CheckTag(CollectedFirstDeck)`），
    /// 而我们没有 tag 存档 / 没有 FTUE ⇒ 本窗**建好并注册，但没有任何生产入口**，
    /// **也不自己判显隐**（见 <see cref="IsFirstWelcome"/>）。</para>
    /// <para>🔴 **层带**：自成一档 **3382–3390**（`TutorialMessagePopup` 3372–3380 之上、`DuelPopupWindow` 3400 之下）。</para></summary>
    public class WelcomePopup : GameWindow
    {
        // ============================================================ 队列档（自成一档 · 3382–3390）
        public const int QShade = 3382,     // `Menu Dark Background`（压暗整屏；**没有命中区**）
                         QPanel = 3383,     // `Generic Window Red Background Big`（九宫底）
                         QArt = 3384,       // `Event image`
                         QText = 3385;      // 全部文案
        /// <summary>窗内**命中区**那一档（整屏 `Collider`）—— 本窗最高。</summary>
        public const int QHit = 3390;

        /// <summary>原版根名（= `WelcomePopup.Create()` 里 `new GameObject(...)` 用的名字，**两处同源**）。</summary>
        public const string RootName = "WelcomeScreen Alpha";

        // ============================================================ 数据
        /// <summary>本窗要铺的一份内容（= 原版 `WelcomePopup.Context`）。
        /// **文本是词条键**（窗里再查表）；`Image` = 原版那个 AssetReference 指到的图。</summary>
        public struct Context
        {
            public string TitleTerm;      // null/空 ⇒ `TutorialMessagePopup.TitleTermDefault`
            public string DescTerm;       // null/空 ⇒ `TutorialMessagePopup.DescTermDefault`
            public Texture2D Image;       // `Event image`（原版 `warlordImage`）；null ⇒ 整层关 + 出声一次
        }

        /// <summary>出厂值 = prefab 里那三颗 TMP 的词条键、无图。</summary>
        public static Context DefContext()
        {
            return new Context
            {
                TitleTerm = TutorialMessagePopup.TitleTermDefault,
                DescTerm = TutorialMessagePopup.DescTermDefault,
                Image = null,
            };
        }

        static Context FillDef(Context? src) { return src.HasValue ? src.Value : DefContext(); }

        /// <summary>最近一次开出来的那一扇（自检读口）。</summary>
        public static WelcomePopup LastOpened { get; private set; }

        Context _ctx = DefContext();
        /// <summary>当前铺的那一份（自检读口）。</summary>
        public Context Ctx { get { return _ctx; } }

        /// <summary>原版 `WelcomePopup.IsFirstWelcome` = `!SegmentationController.CheckTag(CollectedFirstDeck)`。
        /// <para>🔴 **判据在远端 profile（PlayFab 玩家 tag），本地没有对应物**：我们没有
        /// `SegmentationController` / `CollectedFirstDeck` / `FinishedOnboarding` 任何一个
        /// （`grep Onboarding` 全仓 0 命中）⇒ 本属性**恒 false**、**不接任何显隐**。
        /// ⛔ **别把它改成一个本地「第一次启动」flag 去冒充** —— 原版那条判的是
        /// **「还没收集第一副牌」的 onboarding 阶段**，不是「第一次启动」（判据 → 文件头）。
        /// ⛔ 也别据此以为「本窗永远不该出现」：缺的是**开关数据**，不是窗。</para></summary>
        public static bool IsFirstWelcome { get { return false; } }

        // 自检读口
        Transform _dark, _panel, _eventImage, _texts, _collider;
        Label _title, _desc, _tap;
        BlinkGraphic _blink;
        /// <summary>本次 `Build()` 里 `Event image` 是不是**没有贴图**。</summary>
        public bool EventImageIsBlank { get; private set; }
        /// <summary>取不到的图（自检读口）。</summary>
        public readonly System.Collections.Generic.List<string> MissingArt =
            new System.Collections.Generic.List<string>();

        // ============================================================ 建窗
        /// <summary>建一扇（`WindowsManager.OpenWelcome` 的 `create` 那一跳）。
        /// <para>窗参逐字段照根 MB 实读：`type = 1 Popup` · `windowsPlacement = 15 Popup` ·
        /// **`closeOnESC = 0`** · `updateNavPanel = 0` · `extraScaleSmallScreen = 1.0`。</para></summary>
        public static WelcomePopup Create(WindowsManager mgr, Context? ctx = null)
        {
            var go = new GameObject(RootName);
            var win = go.AddComponent<WelcomePopup>();
            win.type = WindowType.Popup;                  // 实读 type = 1
            win.placement = WindowsPlacement.Popup;       // 实读 windowsPlacement = 15
            win.closeOnEsc = false;                       // 实读 closeOnESC = **0**
            win.updateNavPanel = false;                   // 实读 updateNavPanel = 0
            win.extraScaleSmallScreen = 1f;               // 实读 1.0（= 不覆盖）
            win.Manager = mgr;
            win._ctx = FillDef(ctx);
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[Welcome] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            Build();
        }

        /// <summary>换一份内容（复用同一扇窗时用；同 `TutorialMessagePopup.Show`）。</summary>
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
            _dark = _panel = _eventImage = _texts = _collider = null;
            _title = _desc = _tap = null;
            EventImageIsBlank = false;

            // ---- 1) `Menu Dark Background`（压暗整屏；**不挂命中区** —— PPtr 实读为 0）----
            _dark = MenuDraw.Node(transform, "Menu Dark Background", TutorialMessagePopup.DarkBgR);
            MenuDraw.Rect(_dark, CardArt.Solid(), TutorialMessagePopup.DarkBgR, "Image",
                          QShade, TutorialMessagePopup.ShadeTint);

            // ---- 2) `Generic Window Red Background Big`（九宫底）----
            _panel = MenuDraw.Node(transform, "Generic Window Red Background Big", TutorialMessagePopup.RedBgR);
            MenuDraw.Nine(_panel, Tex(TutorialMessagePopup.ArtPanel, "`Generic Window Red Background Big` 的九宫底"),
                          TutorialMessagePopup.RedBgR, TutorialMessagePopup.PanelBorder,
                          TutorialMessagePopup.PanelTexW, TutorialMessagePopup.PanelTexH,
                          QPanel, null, true, "Image");

            // ---- 3) `Event image`（原版 `warlordImage`；没图 ⇒ 整层不画，= `set_enabled(sprite != null)`）----
            _eventImage = MenuDraw.Node(transform, "Event image", TutorialMessagePopup.EventImageR);
            bool imageOn = _ctx.Image != null;
            if (imageOn)
                MenuDraw.Rect(_eventImage, _ctx.Image, TutorialMessagePopup.EventImageR, "Image",
                              QArt, TutorialMessagePopup.EventTint, false);
            else
            {
                EventImageIsBlank = true;
                NoteNoEventImage();
            }
            _eventImage.gameObject.SetActive(imageOn);

            // ---- 4) `Texts`（Title / Description / Tap To continue）----
            _texts = MenuDraw.Node(transform, "Texts", TutorialMessagePopup.TextsR);
            var title = TutorialMessagePopup.OfficialTerm(_ctx.TitleTerm,
                          "Welcome to Warpforge!", "欢迎来到扭曲熔炉！");
            var desc = TutorialMessagePopup.OfficialTerm(_ctx.DescTerm,
                          "War rages across the stars. Ancient civilisations and upstart empires fight endless battles "
                        + "across innumerable worlds. \nHumanity stands alone, beset on all sides by the heretic, "
                        + "the mutant, and the alien. \nThere is no mercy. There is no respite. Choose your faction "
                        + "and get ready for battle!",
                          "战争蔓延星辰。旗手文明和新兴帝国在无数世界中争战。\n人类孤独站立，被异教徒、突变体和外星人从四面八方围攻。\n"
                        + "没有仁慈。没有喘息。选择你的阵营，准备战斗！");

            // `TitleText`：fs 34 · 基准 47.73 · auto[20~34] · 折行 1 · 白 · Center/**Bottom**(1024)
            _title = MenuDraw.Text(_texts, TutorialMessagePopup.TitleR, title, Color.white, "TitleText",
                                   TutorialMessagePopup.TitleFont, QText, TutorialMessagePopup.TitleR.W,
                                   TutorialMessagePopup.TitleFontMin, TutorialMessagePopup.TitleFontMax,
                                   TutorialMessagePopup.TitleFontBase, face: TmpFont.Face.LatinUi);
            MenuDraw.SetVAlign(_title, Label.VAlign.Bottom, TutorialMessagePopup.TitleR);

            // `DescriptionText`：fs 28 · 基准 22.5 · auto[18~28] · 折行 1 · 白 · Center/Middle(512)
            _desc = MenuDraw.TextBox(_texts, TutorialMessagePopup.DescR, desc, Color.white, "DescriptionText",
                                     TutorialMessagePopup.DescFont, TutorialMessagePopup.DescFontMin, QText,
                                     TutorialMessagePopup.DescFontMax, TutorialMessagePopup.DescFontBase, face: TmpFont.Face.LatinCardBody);

            // `Tap To continue`：fs 30 · 基准 60 · auto[18~30] · 折行 1 · 色 (0.627,0.616,0.631) · Center/**Capline**(8192)
            var tapText = Loc.HasEntry(TutorialMessagePopup.TapTerm)
                        ? Loc.T(TutorialMessagePopup.TapTerm)
                        : (Loc.Current == AvailableLanguages.Chinese ? "点击以继续" : "Click to continue");
            _tap = MenuDraw.TextBox(_texts, TutorialMessagePopup.TapR, tapText,
                                    TutorialMessagePopup.TapColor, "Tap To continue",
                                    TutorialMessagePopup.TapFont, TutorialMessagePopup.TapFontMin, QText,
                                    TutorialMessagePopup.TapFontMax, TutorialMessagePopup.TapFontBase, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(_tap, Label.VAlign.Capline, TutorialMessagePopup.TapR);

            // `BlinkGraphic`（原版挂在这颗 TMP 上；`blinkSpeed 1.0` / `colorVariation 0.5` = 出厂值）
            var tap = _tap;
            if (tap != null)
            {
                var b = EnsureBlink();
                b.LogTag = "[Welcome]";
                b.Silent = false;
                b.Bind(tap.gameObject, () => tap.color, c => tap.SetColor(c));
                b.Restart();
            }

            // ---- 5) `Collider`（整屏透明 Image + `EverguildButton`；原版 `Open()` 把 `onClick += Close`）----
            _collider = MenuDraw.Node(transform, "Collider", TutorialMessagePopup.FullR);
            ColliderHit = MenuDraw.Hit(_collider, "Hit", TutorialMessagePopup.FullR, QHit, () => Close());

            if (MissingArt.Count > 0)
                Debug.LogWarning("[Welcome] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray()));
        }

        // ============================================================ 文案 / 时钟 / 取图
        static bool _noEventImageWarned;
        void NoteNoEventImage()
        {
            if (_noEventImageWarned) return;
            _noEventImageWarned = true;
            Debug.Log("[Welcome] `Event image` **没有贴图** —— 原版 `WelcomePopup.Open()` 用 `Context` 的"
                      + " AssetReference / 活动督军 `fullHeroSprite` / liveop 资产图喂它；这两条都在远端"
                      + " ⇒ 节点照建、整层关掉（= 原版 `set_enabled(image, sprite != null)` 的画面结果）。⛔ 不拿别的图顶。");
        }

        void Update() { Tick(Time.deltaTime); }
        /// <summary>批处理下没有帧循环 ⇒ 自检直接调（同 `TutorialMessagePopup.Tick`）。</summary>
        public void Tick(float dt) { if (_blink != null) _blink.Tick(dt); }

        BlinkGraphic EnsureBlink()
        {
            if (_blink == null) _blink = GetComponent<BlinkGraphic>();
            if (_blink == null) _blink = gameObject.AddComponent<BlinkGraphic>();
            return _blink;
        }

        Texture2D Tex(string name, string what)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name))
            {
                MissingArt.Add(name);
                Debug.LogWarning("[Welcome] 图取不到：`" + name + "`（" + what + "）⇒ **这一件没画**"
                               + "（`MenuDraw.Rect/Nine` 对 `tex == null` 是静默返回 null）。"
                               + "导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            }
            return t;
        }

        // ============================================================ 自检读口
        public Transform DarkNode { get { return _dark; } }
        public Transform PanelNode { get { return _panel; } }
        public Transform EventImageNode { get { return _eventImage; } }
        public Transform TextsNode { get { return _texts; } }
        public Transform ColliderNode { get { return _collider; } }
        /// <summary>整屏 `Collider` 那颗命中区（点它 = 关窗）。</summary>
        public Transform ColliderHit { get; private set; }
        public Label TitleLabel { get { return _title; } }
        public Label DescriptionLabel { get { return _desc; } }
        public Label TapLabel { get { return _tap; } }
        public BlinkGraphic Blink { get { return _blink; } }

        public string DebugDump()
        {
            string d = _desc != null && !string.IsNullOrEmpty(_desc.Text)
                     ? _desc.Text.Substring(0, Mathf.Min(24, _desc.Text.Length)) : "-";
            return "title=\"" + (_title != null ? _title.Text : "-") + "\""
                 + " desc=\"" + d + "…\""
                 + " tap=\"" + (_tap != null ? _tap.Text : "-") + "\""
                 + " image=" + (!EventImageIsBlank ? "on" : "blank")
                 + " missingArt=" + MissingArt.Count;
        }
    }
}
