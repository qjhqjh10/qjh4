// TutorialMessagePopup.cs — 原版 **`TutorialMessageWindowPopUp`**（教程消息窗 / 「点击以继续」整屏简报）
//
// ============================ 出处（唯一判据，全部本地现读）============================
// · prefab 树 / 矩形 / 组件 / 文字档位（2026-10-17 现读，可复跑）：
//     `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Tutorial Message Window PopUp" --depth 8 --relative --md`
//   根 = `bundle_menus_assets_all/GameObject/Tutorial Message Window PopUp.json`，
//   根 MB = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-1703418497440718640.json`（窗参逐字段实读）：
//     `type = 1`(Popup) · `windowsPlacement = 15`(Popup) · **`closeOnESC = 0`** · `updateNavPanel = 0` ·
//     `extraScaleSmallScreen = 1.0` · `openSound = {0,0}` · `closeSound = {0,0}` · `useDefaultCloseSoundIfNull = 1`
//   ⇒ 关窗音走家族默认（`GameWindow.useDefaultCloseSoundIfNull` 的既有实现），本窗**不赋任何 clip**。
//   字段 PPtr 实读：`warlordImage → Event image`（`MonoBehaviour_-7889800045111779120`）·
//   `trigger → Collider`（`MonoBehaviour_-2784585674942750512`）· `campaignPointDrawer → Icon Campaign Points Drawer Variant`
//   （`MonoBehaviour_6650960178031780048`）· `titleText → Texts/TitleText` · `descriptionText → Texts/DescriptionText`。
// · 行为（第一权威 = 反编译 `d:/2/tools/decomp_full/`）：
//   · `TutorialManager__ShowTutorialMessageWindow.c`：`data+0x28 = 1`（"已展示"标记）后
//     `WindowsManager.OpenWindow(this+0x30 /* tutorialMessageWindow */, data)`。
//   · `TutorialMessageWindowPopUp__Open.c` 逐句：
//       ① `GameWindow.Open()`
//       ② **`campaignPointDrawer.gameObject.SetActive(false)`** —— 出厂**就关掉战役点数抽屉**
//       ③ `data = this+0x40`；`titleText.setText(I2(data+0x10))` · `descriptionText.setText(I2(data+0x18))`
//       ④ `data+0x20`（`AssetReference`）**非空且有效** ⇒ `Load` → `warlordImage.sprite`；
//          否则查当前活动 → `CardDeck` 的督军 `RawCardScript.fullHeroSprite` → `warlordImage.sprite`，
//          最后 **`warlordImage.enabled = (sprite != null)`**（没图 ⇒ 整层不画，不是留空框）
//       ⑤ `trigger.onClick.AddListener(...)` → `RaycastFilter.ClearRaycastFilters()`
//   · `TutorialMessageWindowPopUp__Close.c`：先 `RemoveListener`，再 `GameWindow.Close()`，
//     最后把 `this+0x98` 那个 `AssetReferenceTyped` **Release**（= ④ 加载进来的那张图）。
//   · `TutorialMessageWindowPopUp__ShowCampaignPointsDrawer.c`：`warlordImage.enabled = false` →
//     `drawer.gameObject.SetActive(true)` → `CampaignPointDrawer.DrawForArmy(army)`。
//     🔴 **`ShowCampaignPointsDrawer` 在全量反编译里 0 调用者**（`grep -rl` 只命中它自己那个 `.c`）
//     ⇒ 这一支在原版当前 build 里**也走不到**；我们照建、但**默认按 `Open()` 那一句关着**。
//   · `CampaignPointDrawer__DrawForArmy.c`：两件事 —— ① `+0x58` 那颗 TMP ← `GameStaticData.CardArmyToString(army)`；
//     ② `ItemDrawer.image.sprite ← ArmyUtilities.GetArmyIcon(army)`、`enabled = (sprite != null)`。
//     ⚠️ 本 prefab 的 `Content` 下**没有任何 TMP 子件**（树里只有 `Campaign Glow` / `Image` / 两个抽屉）
//     ⇒ ① 那一跳在这个 prefab 上是空转（`+0x58` 为 0 时 `DrawForArmy` 首句守卫就跳过了）。
//
// ============================ 我们侧的对位（逐件写清，不冒充原版）============================
// · **文案**：原版走 `I2(data+0x10 / +0x18)`（`TutorialMessageData` 的两条词条键）。
//   本 prefab 的 `TitleText` / `DescriptionText` 上那颗 `Localize` **`m_Enabled = 0`**（组件级禁用），
//   但 `mTerm` 仍在：`MainMenu/Welcome/Title` · `MainMenu/Welcome/Subtitle`（= WelcomePopup 同两条）。
//   ⇒ 默认值用这两条官方词条；调用方可另给 `Context.TitleTerm / DescTerm`。
//   🔴 **这两条不在 `Core/Loc.cs` 的手写 `Table` 里**（`Loc.HasEntry` = false），而 `Loc.cs` 本轮归
//   T08 写手独占（不能加）⇒ 走 `Loc.Lang13Of` 的公开口读 `Resources/official_loc_13lang.json`
//   （9447 键，这两条都在），中/英各取各的列；取不到才回本文件里那份官方原文兜底 + 出声。
//   ⚠️ **待 T08 收编**：这两条一旦进 `Loc.Table`，本文件的 `OfficialTerm` 就自动走 `Loc.T`（无需改代码）。
// · **`Event image`（=`warlordImage`）**：原版那张图来自 `TutorialMessageData` 的 AssetReference 或活动督军立绘 ——
//   两者都在远端 / 需要教程 stage 数据 ⇒ 本窗**留一个 `Context.Image` 口**（调用方给就画、不给就空着 + 出声一次），
//   与 `BaseEventPopup.EventImage` 同一处置。⛔ 不拿别的图顶（那会静默画错一张）。
// · **`Container Campaign Points`**：照 prefab 建（`Campaign Glow` = `40K_genearl_icon_Campaign points_big`，
//   视觉 481.42²，压在 320.95² 的抽屉框下面）；出厂 **`Icon Campaign Points Drawer Variant` 整颗关着**
//   （= `Open()` 那一句）。`ShowCampaignPointsDrawer(armyIcon)` 是那一支的入口，**默认不会被调用**
//   （原版同样 0 调用者），照建只是为了把 prefab 结构补齐。
// · **`Collider`**（整屏透明 `Image` + `EverguildButton`）：原版 `Open()` 给它挂 `onClick` ⇒
//   **点屏幕任何地方 = 关窗**（它是最后一个兄弟 ⇒ 盖在面板/文字之上）。我们照做：
//   整屏命中区 `Collider/Hit` 挂 `Close()`。
//   ⚠️ `Menu Dark Background` 自己那颗 `BackgroundCloseButton.window` PPtr 实读是 **0**（= 空）
//   ⇒ **压暗层本身不关窗**（与 `ItemDisplayWindow` 那种「OnEnable 补挂」不同）—— 别给它加 `ShadeHit`。
// · **`Tap To continue` 的闪烁**：`BlinkGraphic`（`blinkSpeed 1.0` / `colorVariation 0.5` = 出厂值，
//   prefab 里 7 个实例逐个实读、一个都没覆盖过）—— 与 `BaseEventPopup.BuildTexts` 同一写法。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `TutorialMessageWindowPopUp`（`GameWindow` 子类）—— **整屏简报：立绘 + 标题 + 长正文 + 点击继续**。
    /// <para>🔴 **入口缺口（如实记，⛔ 不伪造调用点）**：原版的调用链是
    /// `TutorialStageClaimForgeFirstLevel / TutorialStageGetCampaignNode / TutorialStageGetMission /
    /// TutorialStageLevelUpCard` 的 `DoMenuActions` → `TutorialManager.ShowTutorialMessageWindow(data)`。
    /// **我们这一侧没有菜单版的教程 stage 状态机**（全仓 `FlowTutorial` / `TutorialStage.DoMenuActions`
    /// 零命中；`TutorialStage*` 只存在于**战斗内** `Battle/TutorialScript` 那一族）⇒ 本窗**建好并注册**，
    /// 但**没有生产入口**。判据 → `资料/普查产出_1017/UI/R2_暂缓批复核.md` §1·1。</para>
    /// <para>🔴 **它不在任何一页的层带里**：自成一档 **3372–3380**
    /// （`ItemDisplayWindow` 3362–3370 之上、`DuelPopupWindow` 3400 之下 —— 层带不许重叠）。</para></summary>
    public class TutorialMessagePopup : GameWindow
    {
        // ============================================================ 队列档（自成一档 · 3372–3380）
        public const int QShade = 3372,     // `Menu Dark Background`（压暗整屏；**它的命中区不存在**，见文件头）
                         QPanel = 3373,     // `Generic Window Red Background Big`（九宫底）
                         QArt = 3374,       // `Event image`
                         QGlow = 3375,      // `Container Campaign Points/Campaign Glow`（抽屉底下的光晕）
                         QPoints = 3376,    // 抽屉里的 `Image`（运行期阵营图标，原版兄弟序在 Glow **之后**）
                         QText = 3377;      // 全部文案
        /// <summary>窗内**命中区**那一档（整屏 `Collider`）—— 本窗最高。</summary>
        public const int QHit = 3380;

        // ============================================================ 几何（dump 现读 · 1920×1080 画布 px）
        /// <summary>原版根名（= 我们 `Create()` 里 `new GameObject(...)` 用的名字，**两处同源**）。</summary>
        public const string RootName = "Tutorial Message Window PopUp";

        /// <summary>`Menu Dark Background`：4574.60×2572.36，**以屏幕中心为中心**（−1327.30,−746.18 → 3247.30,1826.18）。</summary>
        public static readonly PxRect DarkBgR = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);
        /// <summary>`Generic Window Red Background Big`（1052.74×733.35）。</summary>
        public static readonly PxRect RedBgR = new PxRect(442.63f, 146.33f, 1495.37f, 879.67f);
        /// <summary>`Event image`（858.75×858.75）。</summary>
        public static readonly PxRect EventImageR = new PxRect(281.62f, 34.12f, 1140.38f, 892.88f);
        /// <summary>`Container Campaign Points`（320.95×323.74）。</summary>
        public static readonly PxRect PointsR = new PxRect(566.53f, 340.13f, 887.47f, 663.87f);
        /// <summary>`Icon Campaign Points Drawer Variant`（320.95×320.95，`AspectRatioFitter` 1:1 FitInParent）。</summary>
        public static readonly PxRect DrawerR = new PxRect(566.53f, 341.53f, 887.47f, 662.47f);
        /// <summary>`Campaign Glow` 的**视觉**框：布局 320.95² × `localScale 1.5` ⇒ **481.42²**、居中于抽屉框。
        /// 🔴 原版那颗的 `localScale` 是**序列化值 1.5**（不是运行期算的），照乘。</summary>
        public static readonly PxRect GlowR = CenteredOn(DrawerR, 481.42f);
        /// <summary>`Texts` 容器（450×580）；子件可以溢出它（`Tap To continue` 就溢到 809.65）。</summary>
        public static readonly PxRect TextsR = new PxRect(1005f, 190f, 1455f, 770f);
        public static readonly PxRect TitleR = new PxRect(1005f, 190f, 1455f, 295f);        // 450×105
        public static readonly PxRect DescR = new PxRect(1025f, 308.58f, 1435f, 733.58f);    // 410×425
        public static readonly PxRect TapR = new PxRect(1005f, 730.35f, 1455f, 809.65f);     // 450×79.293
        /// <summary>整屏 `Collider`。</summary>
        public static readonly PxRect FullR = new PxRect(0f, 0f, 1920f, 1080f);

        /// <summary>`Menu Dark Background` 的 `Image.m_Color`（实读 (0,0,0,0.7725490)）。</summary>
        public static readonly Color ShadeTint = new Color(0f, 0f, 0f, 0.7725490f);
        /// <summary>`Event image` 的 `Image.m_Color`（实读 (0.8018868,0.8018868,0.8018868,1)）。</summary>
        public static readonly Color EventTint = new Color(0.8018868f, 0.8018868f, 0.8018868f, 1f);
        /// <summary>`Tap To continue` 的色（实读 (0.627451,0.6156863,0.6313726,1)）。</summary>
        public static readonly Color TapColor = new Color(0.627451f, 0.6156863f, 0.6313726f, 1f);

        /// <summary>`Generic Window Red Background Big` 的底图 = `UI_Deck_Information_Back`
        /// （1100×701 · `m_Border = (L 42, B 363, R 655, T 81)` · `m_Type = Sliced`）。</summary>
        public const string ArtPanel = "UI_Deck_Information_Back";
        public const float PanelTexW = 1100f, PanelTexH = 701f;
        public static readonly Vector4 PanelBorder = new Vector4(42f, 363f, 655f, 81f);

        /// <summary>`Campaign Glow` 的图（`40K_genearl_icon_Campaign points_big`；⚠️ 原版拼写就是 `genearl`）。</summary>
        public const string ArtCampaignGlow = "40K_genearl_icon_Campaign points_big";

        // 文字档位（prefab 逐颗实读）
        public const float TitleFont = 34f, TitleFontMin = 20f, TitleFontMax = 34f, TitleFontBase = 47.73f;
        public const float DescFont = 28f, DescFontMin = 18f, DescFontMax = 28f, DescFontBase = 22.5f;
        /// <summary>⚠️ `DescriptionText` 的 `m_lineSpacing = 5` —— 我们的 `MenuDraw` 没有这个形参
        /// （同族 `BaseEventPopup` 也没接），**如实记**为未复刻的一处排版细节。</summary>
        public const float DescLineSpacing = 5f;
        public const float TapFont = 30f, TapFontMin = 18f, TapFontMax = 30f, TapFontBase = 60f;

        // ============================================================ 官方词条（默认那几条）
        /// <summary>标题词条键（= prefab `TitleText` 上那颗 `Localize.mTerm`）。</summary>
        public const string TitleTermDefault = "MainMenu/Welcome/Title";
        /// <summary>正文词条键（= prefab `DescriptionText` 上那颗 `Localize.mTerm`）。</summary>
        public const string DescTermDefault = "MainMenu/Welcome/Subtitle";
        /// <summary>`Tap To continue` 的词条键（在 `Loc.Table` 里，直接 `Loc.T`）。</summary>
        public const string TapTerm = "Demo/Welcome/TapToContinue";

        // 官方表原文兜底（`官方本地化_en+zh-CN.json` 的 `terms` 逐字，2026-10-17 现读）
        const string TitleEn = "Welcome to Warpforge!", TitleZh = "欢迎来到扭曲熔炉！";
        const string DescEn = "War rages across the stars. Ancient civilisations and upstart empires fight endless battles "
                            + "across innumerable worlds. \nHumanity stands alone, beset on all sides by the heretic, "
                            + "the mutant, and the alien. \nThere is no mercy. There is no respite. Choose your faction "
                            + "and get ready for battle!";
        const string DescZh = "战争蔓延星辰。旗手文明和新兴帝国在无数世界中争战。\n人类孤独站立，被异教徒、突变体和外星人从四面八方围攻。\n"
                            + "没有仁慈。没有喘息。选择你的阵营，准备战斗！";
        const string TapEn = "Click to continue", TapZh = "点击以继续";

        // ============================================================ 数据
        /// <summary>本窗要铺的一份内容。**文本已经是"词条键"**（窗里再查表）。
        /// 建法：默认 `DefContext()`（= prefab 出厂那一份）。</summary>
        public struct Context
        {
            /// <summary>标题词条键；null/空 ⇒ `TitleTermDefault`。</summary>
            public string TitleTerm;
            /// <summary>正文词条键；null/空 ⇒ `DescTermDefault`。</summary>
            public string DescTerm;
            /// <summary>`Event image`（= 原版 `warlordImage`）那张图；null ⇒ 节点照建、不画（+ 出声一次）。</summary>
            public Texture2D Image;
            /// <summary>true ⇒ 走 `ShowCampaignPointsDrawer` 那一支（关 Event image、开战役点数抽屉）。
            /// 🔴 原版那条链 **0 调用者** ⇒ 默认 false。</summary>
            public bool PointsMode;
            /// <summary>`PointsMode` 下画在抽屉 `Image` 上的阵营图标（原版 `ArmyUtilities.GetArmyIcon(army)`）；
            /// null ⇒ 留空 + 出声（⛔ 不编一张）。</summary>
            public Texture2D ArmyIcon;
        }

        /// <summary>出厂值 = prefab 里那三颗 TMP 的词条键 + 空白图 + 立绘模式。</summary>
        public static Context DefContext()
        {
            return new Context
            {
                TitleTerm = TitleTermDefault, DescTerm = DescTermDefault,
                Image = null, PointsMode = false, ArmyIcon = null,
            };
        }

        static Context FillDef(Context? src) { return src.HasValue ? src.Value : DefContext(); }

        /// <summary>最近一次开出来的那一扇（自检读口，同族先例 `ItemDisplayWindow.LastOpened`）。</summary>
        public static TutorialMessagePopup LastOpened { get; private set; }

        Context _ctx = DefContext();
        /// <summary>当前铺的那一份（自检读口）。</summary>
        public Context Ctx { get { return _ctx; } }

        // 自检读口（节点引用）
        Transform _dark, _panel, _eventImage, _points, _drawer, _texts, _collider;
        Label _title, _desc, _tap;
        BlinkGraphic _blink;
        /// <summary>本次 `Build()` 里 `Event image` 是不是**没有贴图**。</summary>
        public bool EventImageIsBlank { get; private set; }
        /// <summary>取不到的图（自检读口 —— 非空就是「有件根本没画」）。</summary>
        public readonly List<string> MissingArt = new List<string>();

        // ============================================================ 建窗
        /// <summary>建一扇（`WindowsManager.OpenTutorialMessage` 的 `create` 那一跳）。
        /// <para>窗参逐字段照根 MB 实读：`type = 1 Popup` · `windowsPlacement = 15 Popup` ·
        /// **`closeOnESC = 0`**（ESC 不关！）· `updateNavPanel = 0` · `extraScaleSmallScreen = 1.0`。</para></summary>
        public static TutorialMessagePopup Create(WindowsManager mgr, Context? ctx = null)
        {
            var go = new GameObject(RootName);
            var win = go.AddComponent<TutorialMessagePopup>();
            win.type = WindowType.Popup;                  // 实读 type = 1
            win.placement = WindowsPlacement.Popup;       // 实读 windowsPlacement = 15
            win.closeOnEsc = false;                       // 实读 closeOnESC = **0**
            win.updateNavPanel = false;                   // 实读 updateNavPanel = 0
            win.extraScaleSmallScreen = 1f;               // 实读 1.0（= 不覆盖）
            // `openSound` / `closeSound` 实读都是空 PPtr、`useDefaultCloseSoundIfNull = 1`
            //   ⇒ 一个 clip 都不赋，关窗音走 `GameWindow.Close()` 的家族默认。
            win.Manager = mgr;
            win._ctx = FillDef(ctx);
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[TutorialMsg] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            Build();
        }

        /// <summary>换一份内容（复用同一扇窗时用；同 `ItemDisplayWindow.Show`）。
        /// <para>⚠️ `GameWindow.TryOpen` 在 `CurrentState == Open` 时照原版**早退**（不重建），
        /// 所以复用那一支必须显式调本方法。</para></summary>
        public void Show(Context? ctx = null)
        {
            _ctx = FillDef(ctx);
            Build();
        }

        // ============================================================ 建整棵树（顺序 = 原版兄弟序）
        /// <summary>照 prefab 逐节点搭（**先清空重来** —— 换内容就是重建，同 `ItemDisplayWindow.Build`）。</summary>
        public void Build()
        {
            PointerLayer.UnregisterOwnedBy(gameObject);
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();
            _dark = _panel = _eventImage = _points = _drawer = _texts = _collider = null;
            _title = _desc = _tap = null;
            EventImageIsBlank = false;

            // ---- 1) `Menu Dark Background`（压暗整屏；**不挂命中区** —— 见文件头那条 PPtr 实读）----
            _dark = MenuDraw.Node(transform, "Menu Dark Background", DarkBgR);
            MenuDraw.Rect(_dark, CardArt.Solid(), DarkBgR, "Image", QShade, ShadeTint);

            // ---- 2) `Generic Window Red Background Big`（九宫底）----
            _panel = MenuDraw.Node(transform, "Generic Window Red Background Big", RedBgR);
            MenuDraw.Nine(_panel, Tex(ArtPanel, "`Generic Window Red Background Big` 的九宫底"),
                          RedBgR, PanelBorder, PanelTexW, PanelTexH, QPanel, null, true, "Image");

            // ---- 3) `Event image`（原版 `warlordImage`；没图 ⇒ 整层不画，= `set_enabled(sprite != null)`）----
            _eventImage = MenuDraw.Node(transform, "Event image", EventImageR);
            bool imageOn = !_ctx.PointsMode && _ctx.Image != null;
            if (imageOn)
                MenuDraw.Rect(_eventImage, _ctx.Image, EventImageR, "Image", QArt, EventTint, false);
            else if (!_ctx.PointsMode)
            {
                EventImageIsBlank = true;
                NoteNoEventImage();
            }
            _eventImage.gameObject.SetActive(imageOn);

            // ---- 4) `Container Campaign Points`（出厂整颗关着 = `Open()` 那一句 SetActive(false)）----
            //   ⚠️ 原版 `Content` 是 **1080×1080 再 ×0.2972**（`UIScaleToFit` 运行期改写 `localScale`）；
            //      我们直接按**换算后的 320.95²** 建这一层，并把 `Campaign Glow` 摆到它的**绝对视觉框**
            //      （481.42²）— 结果与「1080 × 0.2972 × 1.5」逐值相同，只是不经过那两级缩放。
            _points = MenuDraw.Node(transform, "Container Campaign Points", PointsR);
            _drawer = MenuDraw.Node(_points, "Icon Campaign Points Drawer Variant", DrawerR);
            var content = MenuDraw.Node(_drawer, "Content", DrawerR);
            // `Campaign Glow`：布局 320.95² × `localScale 1.5` ⇒ 视觉 481.42²（序列化值，照乘）
            MenuDraw.Rect(content, Tex(ArtCampaignGlow, "`Campaign Glow`"), GlowR, "Campaign Glow", QGlow, null, true);
            // `Image`：出厂 `<无图>`；`PointsMode` 下由 `DrawForArmy` 换成阵营图标（`enabled = sprite != null`）
            if (_ctx.PointsMode)
            {
                if (_ctx.ArmyIcon != null)
                    MenuDraw.Rect(content, _ctx.ArmyIcon, DrawerR, "Image", QPoints, null, true);
                else
                    Debug.LogWarning("[TutorialMsg] `PointsMode` 打开了战役点数抽屉，但**没有阵营图标**"
                                   + "（原版 = `CampaignPointDrawer.DrawForArmy` → `ArmyUtilities.GetArmyIcon(army)`；"
                                   + "我们这一侧没给）⇒ 那一格**留空**。⛔ 不编一张。");
            }
            _drawer.gameObject.SetActive(_ctx.PointsMode);   // = 原版 `Open()` 的 SetActive(false)（默认）

            // ---- 5) `Texts`（Title / Description / Tap To continue）----
            _texts = MenuDraw.Node(transform, "Texts", TextsR);
            var title = OfficialTerm(_ctx.TitleTerm, TitleEn, TitleZh);
            var desc = OfficialTerm(_ctx.DescTerm, DescEn, DescZh);
            // `TitleText`：fs 34 · 基准 47.73 · auto[20~34] · 折行 1 · 白 · Center/**Bottom**(1024)
            _title = MenuDraw.Text(_texts, TitleR, title, Color.white, "TitleText",
                                   TitleFont, QText, TitleR.W, TitleFontMin, TitleFontMax, TitleFontBase, face: TmpFont.Face.LatinUi);
            MenuDraw.SetVAlign(_title, Label.VAlign.Bottom, TitleR);
            // `DescriptionText`：fs 28 · 基准 22.5 · auto[18~28] · 折行 1 · 白 · Center/Middle(512)
            _desc = MenuDraw.TextBox(_texts, DescR, desc, Color.white, "DescriptionText",
                                     DescFont, DescFontMin, QText, DescFontMax, DescFontBase, face: TmpFont.Face.LatinCardBody);
            // `Tap To continue`：fs 30 · 基准 60 · auto[18~30] · 折行 1 · 色 (0.627,0.616,0.631) · Center/**Capline**(8192)
            var tapText = Loc.HasEntry(TapTerm) ? Loc.T(TapTerm)
                                               : (Loc.Current == AvailableLanguages.Chinese ? TapZh : TapEn);
            _tap = MenuDraw.TextBox(_texts, TapR, tapText, TapColor, "Tap To continue",
                                    TapFont, TapFontMin, QText, TapFontMax, TapFontBase, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(_tap, Label.VAlign.Capline, TapR);

            // `BlinkGraphic`（原版挂在这颗 TMP 上；`blinkSpeed 1.0` / `colorVariation 0.5` = 出厂值）
            var tap = _tap;
            if (tap != null)
            {
                var b = EnsureBlink();
                b.LogTag = "[TutorialMsg]";
                b.Silent = false;
                b.Bind(tap.gameObject, () => tap.color, c => tap.SetColor(c));
                b.Restart();
            }

            // ---- 6) `Collider`（整屏透明 Image + `EverguildButton`；原版 `Open()` 把 `onClick += Close`）----
            //   它是**最后一个兄弟** ⇒ 盖在面板/文字之上 ⇒ **点屏幕任何地方 = 关窗**（同原版）。
            _collider = MenuDraw.Node(transform, "Collider", FullR);
            ColliderHit = MenuDraw.Hit(_collider, "Hit", FullR, QHit, () => Close());

            if (MissingArt.Count > 0)
                Debug.LogWarning("[TutorialMsg] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray()));
        }

        // ============================================================ 战役点数抽屉（原版那一支，0 调用者）
        /// <summary>原版 `TutorialMessageWindowPopUp.ShowCampaignPointsDrawer(army)`：
        /// 关 `Event image`、开抽屉、画阵营图标。
        /// 🔴 原版这个方法**全量反编译 0 调用者** —— 我们照建是为了「prefab 结构不缺件」，
        /// ⛔ 别把它当成「教程链会走到」的入口。</summary>
        public void ShowCampaignPointsDrawer(Texture2D armyIcon)
        {
            var c = _ctx;
            c.PointsMode = true;
            c.ArmyIcon = armyIcon;
            Show(c);
        }

        // ============================================================ 文案
        static readonly HashSet<string> _warnedTerm = new HashSet<string>();

        /// <summary>取一条官方词条：`Loc.Table` 优先；不在表里则走 `Resources/official_loc_13lang.json`（公开口 `Lang13Of`）。
        /// <para>🔴 **为什么不是直接用 `Loc.T`**：`MainMenu/Welcome/Title` / `MainMenu/Welcome/Subtitle`
        /// 这两条**不在** `Loc.Table` 的手写表里（`Loc.HasEntry` = false），而 `Core/Loc.cs` 本轮归 T08 写手独占
        /// ⇒ 走公开的 `Loc.Lang13Of(key, "zh"/"en")`（该表 9447 条，两条都在，2026-10-17 现读）。
        /// ⚠️ **待 T08 把这两条收进 `Loc.Table`** ⇒ 本函数第一句就自动改走 `Loc.T`，**代码不用动**。</para>
        /// <para>取不到 ⇒ 回本文件里那份官方原文兜底 + 出声一次（⛔ 不印键名）。</para></summary>
        internal static string OfficialTerm(string key, string en, string zh)
        {
            if (string.IsNullOrEmpty(key)) return "";
            if (Loc.HasEntry(key)) return Loc.T(key);
            bool zhUi = Loc.Current == AvailableLanguages.Chinese;
            string v = Loc.Lang13Of(key, zhUi ? "zh" : "en");
            if (!string.IsNullOrEmpty(v)) return v;
            if (_warnedTerm.Add(key))
                Debug.LogWarning("[TutorialMsg] 官方词条 `" + key + "` 在 `Loc.Table` 与官方 13 语言表里都取不到"
                               + " ⇒ 回本文件里那份官方原文兜底（en/zh）。生成器：`工具/gen_official_loc.py`");
            return zhUi ? zh : en;
        }

        static bool _noEventImageWarned;
        void NoteNoEventImage()
        {
            if (_noEventImageWarned) return;
            _noEventImageWarned = true;
            Debug.Log("[TutorialMsg] `Event image` **没有贴图** —— 原版 `TutorialMessageWindowPopUp.Open()` 用"
                      + " `TutorialMessageData` 的 AssetReference 或活动督军的 `fullHeroSprite` 喂它"
                      + "（两条都在远端 / 需要教程 stage 数据，本地没有）⇒ 节点照建、整层关掉"
                      + "（= 原版 `set_enabled(image, sprite != null)` 的画面结果）。⛔ 不拿别的图顶。");
        }

        // ============================================================ 时钟（`BlinkGraphic`）
        void Update() { Tick(Time.deltaTime); }
        /// <summary>批处理下没有帧循环 ⇒ 自检直接调（同 `BaseEventPopup.Tick`）。</summary>
        public void Tick(float dt) { if (_blink != null) _blink.Tick(dt); }

        BlinkGraphic EnsureBlink()
        {
            if (_blink == null) _blink = GetComponent<BlinkGraphic>();
            if (_blink == null) _blink = gameObject.AddComponent<BlinkGraphic>();
            return _blink;
        }

        // ============================================================ 取图（不静默）
        Texture2D Tex(string name, string what)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name))
            {
                MissingArt.Add(name);
                Debug.LogWarning("[TutorialMsg] 图取不到：`" + name + "`（" + what + "）⇒ **这一件没画**"
                               + "（`MenuDraw.Rect/Nine` 对 `tex == null` 是静默返回 null）。"
                               + "导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            }
            return t;
        }

        // ============================================================ 自检读口
        public Transform DarkNode { get { return _dark; } }
        public Transform PanelNode { get { return _panel; } }
        public Transform EventImageNode { get { return _eventImage; } }
        public Transform PointsNode { get { return _points; } }
        public Transform DrawerNode { get { return _drawer; } }
        public Transform TextsNode { get { return _texts; } }
        public Transform ColliderNode { get { return _collider; } }
        /// <summary>整屏 `Collider` 那颗命中区（点它 = 关窗）。</summary>
        public Transform ColliderHit { get; private set; }
        public Label TitleLabel { get { return _title; } }
        public Label DescriptionLabel { get { return _desc; } }
        public Label TapLabel { get { return _tap; } }
        public BlinkGraphic Blink { get { return _blink; } }

        /// <summary>自检用：把当前铺出来的东西摊开。</summary>
        public string DebugDump()
        {
            string d = _desc != null && !string.IsNullOrEmpty(_desc.Text)
                     ? _desc.Text.Substring(0, Mathf.Min(24, _desc.Text.Length)) : "-";
            return "title=\"" + (_title != null ? _title.Text : "-") + "\""
                 + " desc=\"" + d + "…\""
                 + " tap=\"" + (_tap != null ? _tap.Text : "-") + "\""
                 + " image=" + (!EventImageIsBlank && _ctx.Image != null ? _ctx.Image.name : "<blank>")
                 + " points=" + (_drawer != null && _drawer.gameObject.activeSelf)
                 + " missingArt=" + MissingArt.Count;
        }

        // ============================================================ 小工具
        /// <summary>以 <paramref name="box"/> 的中心为中心、边长 <paramref name="side"/> 的正方形。</summary>
        internal static PxRect CenteredOn(PxRect box, float side)
        {
            float cx = (box.x1 + box.x2) * 0.5f, cy = (box.y1 + box.y2) * 0.5f;
            return new PxRect(cx - side * 0.5f, cy - side * 0.5f, cx + side * 0.5f, cy + side * 0.5f);
        }
    }
}
