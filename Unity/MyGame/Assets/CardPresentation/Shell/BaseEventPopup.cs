// BaseEventPopup.cs — 原版 **`BaseEventPopup`（事件说明弹窗族）**：6 个 prefab 变体全部落在这一个类里
//
// ============================ 出处（唯一正本） ============================
//  · **prefab（第一权威）**：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/GameObject/` 下那六个根 ——
//      `Base Event Popup` · `Event Help Popup Variant` · `Event Help Popup Small Image Variant` ·
//      `Liveop Event Help Variant` · `Full Screen Event Announcement Variant` · `Base Expiration Popup`。
//    量具（可复跑）：
//      `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "<根名>" --depth 8 --md`
//    六个根的窗口参数 / 五个字段的 PPtr 与 tag 值，是从各自的 `BaseEventPopup` 组件 JSON 里**逐字段实读**的
//    （例：`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-4960665813637071397.json` = `Base Event Popup` 那颗）。
//  · **反编译（行为）**：`d:/2/tools/decomp_full/BaseEventPopup__{Open,Close}.c` ·
//      `BaseEventPopup__get_liveOps.c` · `Everguild.LiveOps.LiveOpsAssetUtility__{GetLabel,GetEventLabel,LoadAsset_object_}.c`
//  · **入口**：`SkirmishEventWindow__HelpButtonClick.c` / `RankedEventWindowV2__HelpButtonClick.c`（逐句见 §Help 接线）
//  · 枚举：`EventLabelReferenceType`（Title=0 / Description=1 / SubTitle=2 / Badge=3 / NavigationBarTitle=4 /
//      WelcomeTitle=5 / EndTitle=10 / EndDescription=11 / Help=12）·
//      `EventAssetReferenceType`（Icon=0 / BackgroundImage=1 / **ForegroundImage=2** / HeaderImage=3 / FooterImage=4 /
//      ArmyImage=5 / RankingIcon=6 / **FullScreenImage=7** / **OpenSound=8** / OverIcon=9 / …）
//      —— 两处都在 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/Everguild/LiveOps/` 的签名桩里。
//
// ============================ 六个变体 ============================
// 结构**完全相同**（压暗 + 红底 + Event image + Texts(Title/Description/Tap To continue) + 整屏 Collider），
// 差别只有五处：Event image 的框/染色、`Texts` 里三颗的框与对齐、红底显不显、`Texts` 显不显、`extraScale`。逐值见下。
//
//   变体（原版根名）                            Event image                         title tag / desc tag / image tag
//   `Base Event Popup`                    281.62,34.12→1140.38,892.88 (858.75²)   Title(0) / Description(1) / ForegroundImage(2)
//   `Event Help Popup Variant`            同上                                      Title(0) / Help(12) / ForegroundImage(2)
//   `Event Help Popup Small Image Variant`470.67,236.67→1037.33,803.33 (566.67)    Title(0) / Help(12) / ForegroundImage(2)
//   `Liveop Event Help Variant`           430.89,165.49→1103.11,837.72 (672.23²)   WelcomeTitle(5) / Help(12) / ForegroundImage(2)
//   `Full Screen Event Announcement Var.` 0,0→1920,1080（铺满·白）                  Title(0) / Description(1) / FullScreenImage(7)
//   `Base Expiration Popup`               281.62,34.12→1140.38,892.88（prefab 里烤着 Uriel 立绘）EndTitle(10) / EndDescription(11) / ForegroundImage(2)
//
//   🔴 **全屏宣告那一份**：`Menu Dark Background` 的 alpha = **0.9059**（其余 0.7725）·
//      `Generic Window Red Background Big` 与 `Texts` **出厂 `m_IsActive = false`**（`menu_dump` 标 `F` / `ANC✗`）·
//      `extraScaleSmallScreen = **1.0**`（其余 **1.2**）。
//
// ============================ 运行期行为（`BaseEventPopup__Open.c` 逐句）============================
//   ① `GameWindow.Open()`（本类在 `Open` 里走我们那一份 `Build()`）；
//   ② `warlordImage` 非空 ⇒ `LoadAsset(liveOps, warlordImageTag)` 一路 `set_sprite`；
//   ③ `titleText` / `descriptionText` 非空 ⇒ `GetLabel(liveOps, tag, Array.Empty())` → `set_text`；
//   ④ `trigger`（那颗整屏 `Collider` 的 `EverguildButton`）的 `onClick += Close`；
//   ⑤ `HasAsset(liveOps, 8 /*OpenSound*/)` 才 `AudioCue.Play2D`（prefab 里 `openSound` 是 null）。
//   🔴 **拿不到标签时 `set_text` 写的是空串**（`GetLabel`：`GetEventLabel` 返回 0 ⇒ 回
//      `DAT_184557100 + 0xb8` 那个静态空串）—— **不是**保留 prefab 里烤的那句样例文案。
//      ⇒ 我们本地没有 liveops 事件数据（远端 CCD）时，标题/正文**留空**是**照原版**，不是我们的取舍。
//   🔴 `Tap To continue` **不在这条链上**（Open 只碰 image/title/description）⇒ 它照自己的 `Localize`
//      词条 `Demo/Welcome/TapToContinue` 显示（官方表：`Click to continue` / `点击以继续`）。
//   ⚠️ `closeSound` / `useDefaultCloseSoundIfNull` 我们**没有建模**（`GameWindow` 的关窗音整条链不存在，
//      见 `GameWindow.Close` 的注释）—— 如实记。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`BaseEventPopup` 的六个 prefab 变体（顺序 = prefab 在包里的族谱，⛔ 别当等级）。</summary>
    public enum EventPopupVariant
    {
        /// <summary>`Base Event Popup`。</summary>
        BaseEvent = 0,
        /// <summary>`Event Help Popup Variant`（大图）。</summary>
        EventHelp = 1,
        /// <summary>`Event Help Popup Small Image Variant`（小图）。</summary>
        EventHelpSmallImage = 2,
        /// <summary>`Liveop Event Help Variant`（活动帮助：标题 Capline、正文左对齐）。</summary>
        LiveopEventHelp = 3,
        /// <summary>`Full Screen Event Announcement Variant`（整屏宣告：无红底、无文字、压暗更深）。</summary>
        FullScreenAnnouncement = 4,
        /// <summary>`Base Expiration Popup`（活动结束：EndTitle / EndDescription 标签）。</summary>
        BaseExpiration = 5,
    }

    /// <summary>原版 `BaseEventPopup` —— 事件说明弹窗。**一个类、六份变体数据**（原版就是同一个类挂六个 prefab）。
    /// <para>🔴 本类**不自己找事件数据**：原版那三样（图 / 标题 / 正文）都从 `ILiveOps` 的
    /// `EventLabel` / asset 里取，而 liveops 事件的 JSON 在**远端 CCD**（本地 84 个 bundle 里没有）——
    /// 见 `资料/普查产出_1017/UI/U3_弹窗与小界面族.md` §五·6。⇒ 调用方要给就把词条键传进来，
    /// 不给就**留空 + 出声**（照原版 `GetLabel` 缺标签的写法；⛔ 不编文案、⛔ 不拿别的键顶）。</para></summary>
    public class BaseEventPopup : GameWindow
    {
        // ============================================================ 队列
        // 梯子：压暗 < 面板 < 图/装饰 < 文字 < 命中。
        // 🔴 必须**严格高于活动窗整族**（`LiveOpsEventWindow.QBg..QHit` = 3104–3116 —— 这扇窗就是被它开出来的），
        //    且**低于** `SearchingMatchPopup`（3130）与 `PromptPopup`（3140）。
        //    3124–3129 这一段今天没有别的窗用（3125–3128 属 `DeckSelectionPopup`、但两者不会同屏：
        //    选卡组窗关掉之后才可能点 Help）。同档时谁盖谁不可控（透明队列按到相机的距离排）⇒ 每层必须不同档。
        public const int QShade = 3124;
        public const int QPanel = 3125;
        public const int QArt = 3126;
        public const int QText = 3127;
        /// <summary>整屏 `Collider` 那一档（本窗最高；压暗层命中区必须严格低于它）。</summary>
        public const int QHit = 3129;

        // ============================================================ 共有几何（六个变体逐值相同）
        /// <summary>`Menu Dark Background`：比屏幕大一圈（4574.60 × 2572.36）。</summary>
        static readonly PxRect DarkBgR = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);
        /// <summary>`Generic Window Red Background Big`（1052.74 × 733.35）。</summary>
        static readonly PxRect RedBgR = new PxRect(442.63f, 146.33f, 1495.37f, 879.67f);
        /// <summary>`UI_Deck_Information_Back`：**1100×701 · `m_Border = (L 42, B 363, R 655, T 81)`**
        /// （实读 `bundle_atlasindividual_assets_0_mainmenu/Sprite/UI_Deck_Information_Back.json`；
        /// 我们 `Resources/Art/ui_deck/` 那张 PNG 也是 1100×701，逐字节同尺寸）。`m_Type = Sliced`。</summary>
        static readonly Vector4 RedBorder = new Vector4(42f, 363f, 655f, 81f);
        const float RedTexW = 1100f, RedTexH = 701f;
        /// <summary>`Texts` 容器（450 × 580）—— 六个变体同值；子件可以溢出它（Liveop 那一份就溢）。</summary>
        static readonly PxRect TextsR = new PxRect(1005f, 190f, 1455f, 770f);
        /// <summary>整屏 `Collider`。</summary>
        static readonly PxRect FullR = new PxRect(0f, 0f, 1920f, 1080f);

        /// <summary>`Tap To continue` 的色（实读 `(0.627451, 0.6156863, 0.6313726, 1)`）。</summary>
        static readonly Color TapColor = new Color(0.627451f, 0.6156863f, 0.6313726f, 1f);

        // ============================================================ 变体数据
        /// <summary>一个变体的差异面（其余四件（压暗/红底/Texts 容器/Collider）逐值相同，见上面的常量）。</summary>
        struct Geo
        {
            public PxRect Image;
            public Color ImageTint;
            public float DarkAlpha;
            public bool RedBgActive;      // `Full Screen` 那一份红底出厂关着
            public bool TextsActive;      // `Full Screen` 那一份 Texts 出厂关着
            public PxRect Title, Desc, Tap;
            public Label.VAlign TitleVAlign;
            public bool DescLeft;         // `Liveop Event Help` 的正文是 Left，其余 Center
        }

        static Geo GeometryOf(EventPopupVariant v)
        {
            // 三个「大图」变体（Base / Event Help / Expiration）共用的那一套
            var g = new Geo
            {
                Image = new PxRect(281.62f, 34.12f, 1140.38f, 892.88f),      // 858.75²
                ImageTint = new Color(0.8018868f, 0.8018868f, 0.8018868f, 1f),
                DarkAlpha = 0.7725490f,
                RedBgActive = true,
                TextsActive = true,
                Title = new PxRect(1005f, 190f, 1455f, 295f),                // 450 × 105
                Desc = new PxRect(1025f, 308.58f, 1435f, 733.58f),           // 410 × 425
                Tap = new PxRect(1005f, 730.35f, 1455f, 809.65f),            // 450 × 79.29
                TitleVAlign = Label.VAlign.Bottom,                           // m_VerticalAlignment = 1024
                DescLeft = false,                                            // Center/Middle
            };
            switch (v)
            {
                case EventPopupVariant.EventHelpSmallImage:
                    g.Image = new PxRect(470.67f, 236.67f, 1037.33f, 803.33f);   // 566.67 × 566.66
                    break;
                case EventPopupVariant.LiveopEventHelp:
                    g.Image = new PxRect(430.89f, 165.49f, 1103.11f, 837.72f);   // 672.23²
                    g.ImageTint = new Color(0.8679245f, 0.8679245f, 0.8679245f, 1f);
                    g.Title = new PxRect(924.14f, 190f, 1455f, 240.31f);         // 530.86 × 50.31（锚点 (0,1)-(1,1)）
                    g.Desc = new PxRect(1005f, 244.46f, 1467.28f, 770f);         // 462.28 × 525.54（溢到容器右边 12.28）
                    g.Tap = new PxRect(1005f, 770f, 1455f, 824.75f);             // 450 × 54.75
                    g.TitleVAlign = Label.VAlign.Capline;                        // m_VerticalAlignment = 8192
                    g.DescLeft = true;                                           // m_HorizontalAlignment = 1 (Left)
                    break;
                case EventPopupVariant.FullScreenAnnouncement:
                    g.Image = FullR;
                    g.ImageTint = Color.white;
                    g.DarkAlpha = 0.9058824f;
                    g.RedBgActive = false;
                    g.TextsActive = false;
                    break;
                case EventPopupVariant.BaseExpiration:
                    // 与 Base 逐值相同；prefab 里那张 `UI_Deck_Warlord_Uriel Ventris` 是**烤着的样例**，
                    // 运行期一样被 `LoadAsset(...)` 覆盖（`Open.c` 那一跳无条件 `set_sprite`）⇒ 不照它画死。
                    break;
            }
            return g;
        }

        /// <summary>原版根名（六个 prefab 各一个；我们的窗口 GO 照抄，自检/日志按名字认）。</summary>
        public static string RootNameOf(EventPopupVariant v)
        {
            switch (v)
            {
                case EventPopupVariant.EventHelp: return "Event Help Popup Variant";
                case EventPopupVariant.EventHelpSmallImage: return "Event Help Popup Small Image Variant";
                case EventPopupVariant.LiveopEventHelp: return "Liveop Event Help Variant";
                case EventPopupVariant.FullScreenAnnouncement: return "Full Screen Event Announcement Variant";
                case EventPopupVariant.BaseExpiration: return "Base Expiration Popup";
                default: return "Base Event Popup";
            }
        }

        // ============================================================ 状态（自检/调用方读）
        public EventPopupVariant Variant { get; private set; }
        /// <summary>标题的官方词条键（= liveops `WelcomeTitle` / `Title` 那一条的 `EventLabel.RawValue`）。
        /// **null = 本地没有**（事件数据在远端）⇒ 标题留空 + 出声。</summary>
        public string TitleTerm { get; private set; }
        /// <summary>正文的官方词条键（= liveops `Help` / `Description` / `EndDescription` 那一条）。null 同上。</summary>
        public string DescriptionTerm { get; private set; }
        /// <summary>「这扇窗是哪个事件要出来的」—— 只用于出声（原版 `GetEventLabel` 那条告警里带的也是标签类型）。</summary>
        public string EventNote { get; private set; }

        public Label TitleLabel { get; private set; }
        public Label DescriptionLabel { get; private set; }
        public Label TapLabel { get; private set; }
        /// <summary>`Event image` 那个节点（我们本地没有事件图 ⇒ 它**在、但没有贴图** —— 原版拿不到 asset 时也是这样）。</summary>
        public Transform EventImageNode { get; private set; }
        /// <summary>整屏 `Collider` 那颗命中区（点它 = 关窗）。</summary>
        public Transform ColliderHit { get; private set; }
        public BlinkGraphic Blink { get { return _blink; } }
        /// <summary>这一次 `Build()` 里 `Event image` 是不是**没有贴图**（= 本地没有 liveops 事件图）。</summary>
        public bool EventImageIsBlank { get; private set; }
        /// <summary>取不到的图（照同族 `LiveOpsEventWindow.Tex` 那条「不静默」的做法）。</summary>
        public readonly List<string> MissingArt = new List<string>();

        BlinkGraphic _blink;
        static readonly HashSet<string> _warnedNoEventData = new HashSet<string>();

        // ============================================================ 建窗
        /// <summary>建一扇事件弹窗。
        /// <para>窗口参数（六个 prefab 逐字段实读，见文件头那张表）：`type = 1 Popup` ·
        /// `windowsPlacement = 15 Popup` · `closeOnESC = 1` · `updateNavPanel = 0` ·
        /// `extraScaleSmallScreen = 1.2`（**全屏宣告 1.0**）。</para>
        /// <para><paramref name="titleTerm"/> / <paramref name="descriptionTerm"/> = 官方词条键
        /// （`Event_Description/*` 那一族在 `Resources/official_names.json` 的 bulk 表里，走 `Loc.HasBulk`）；
        /// 传 null = 本地拿不到，**留空 + 出声**。</para></summary>
        public static BaseEventPopup Create(WindowsManager mgr, EventPopupVariant variant,
                                            string titleTerm = null, string descriptionTerm = null,
                                            string eventNote = null)
        {
            var go = new GameObject(RootNameOf(variant));
            var win = go.AddComponent<BaseEventPopup>();
            win.Variant = variant;
            win.TitleTerm = titleTerm;
            win.DescriptionTerm = descriptionTerm;
            win.EventNote = eventNote;
            win.type = WindowType.Popup;                                        // 实证 type = 1
            win.placement = WindowsPlacement.Popup;                             // 实证 windowsPlacement = 15
            win.closeOnEsc = true;                                              // 实证 closeOnESC = 1
            win.updateNavPanel = false;                                         // 实证 updateNavPanel = 0
            win.extraScaleSmallScreen = variant == EventPopupVariant.FullScreenAnnouncement ? 1f : 1.2f;
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        /// <summary>最近一次开出来的那扇（自检用，同族先例 `LiveOpsEventWindow.LastOpened`）。</summary>
        public static BaseEventPopup LastOpened { get; private set; }

        public override void Open()
        {
            LastOpened = this;
            Build();
        }

        // ============================================================ 建整棵树（顺序 = 原版兄弟序）
        public void Build()
        {
            // 🔴 同族教训（`LiveOpsEventWindow.Build` 那一整段）：重建前先把本窗名下的滚动区撤掉。
            //    本窗没有滚动区，但**这一句与那条纪律同形**、成本为零 —— 将来加视口时不用再想一遍。
            PointerLayer.UnregisterOwnedBy(gameObject);
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();
            TitleLabel = DescriptionLabel = TapLabel = null;
            EventImageNode = null;
            ColliderHit = null;
            EventImageIsBlank = false;

            var g = GeometryOf(Variant);

            // ① `Menu Dark Background`（纯色压暗 + 原版那颗 `BackgroundCloseButton` ⇒ 点它关窗）
            //    ⚠️ 命中的是压暗层自己那一档（`QShade`），**严格低于**本窗内容命中区 `QHit` ——
            //    判据与踩过的坑 → `MenuDraw.ShadeHit` 的注释（同队列时点内容会被判成点背景）。
            var dark = MenuDraw.Node(transform, "Menu Dark Background", DarkBgR);
            MenuDraw.Rect(dark, CardArt.Solid(), DarkBgR, "Image", QShade, new Color(0f, 0f, 0f, g.DarkAlpha));
            MenuDraw.ShadeHit(dark, DarkBgR, QShade, QHit, () => Close(), "BackgroundCloseButton");

            // ② `Generic Window Red Background Big`（九宫格；全屏宣告那份出厂 `m_IsActive = false`）
            var red = MenuDraw.Node(transform, "Generic Window Red Background Big", RedBgR);
            MenuDraw.Nine(red, Tex("UI_Deck_Information_Back"), RedBgR, RedBorder, RedTexW, RedTexH,
                          QPanel, null, true, "Image");
            red.gameObject.SetActive(g.RedBgActive);

            // ③ `Event image`（原版 `sprite = null`，由 `Open()` 按 `ForegroundImage`/`FullScreenImage` 喂）
            //    🔴 我们本地**没有事件图**（liveops 远端）⇒ 节点照建、贴图空着 + 出声一次。
            //    这与原版「`LoadAsset` 拿不到 ⇒ `set_sprite(空)`」的**画面结果一致**（那张 Image 不画）。
            EventImageNode = MenuDraw.Node(transform, "Event image", g.Image);
            if (EventImage != null)
                MenuDraw.Rect(EventImageNode, EventImage, g.Image, "Image", QArt, g.ImageTint, false);
            else
            {
                EventImageIsBlank = true;
                NoteNoEventImage();
            }

            // ④ `Texts`（Title / Description / Tap To continue）
            var texts = MenuDraw.Node(transform, "Texts", TextsR);
            BuildTexts(texts, g);
            texts.gameObject.SetActive(g.TextsActive);

            // ⑤ `Collider`（整屏透明 Image + `EverguildButton`；原版 `Open()` 把 `onClick += Close`）
            var col = MenuDraw.Node(transform, "Collider", FullR);
            ColliderHit = MenuDraw.Hit(col, "Hit", FullR, QHit, () => Close());

            if (MissingArt.Count > 0)
                Debug.LogWarning("[EventPopup] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray()));
        }

        /// <summary>`Texts` 那三颗。逐颗参数 = 原版 prefab 实读（判据见文件头那张表 + 下面每颗的注释）。</summary>
        void BuildTexts(Transform texts, Geo g)
        {
            // `TitleText`：fs **34** · 基准 **47.73** · auto[**20**~**34**] · 折行 1 · 白
            TitleLabel = MenuDraw.Text(texts, g.Title, ResolveTerm(TitleTerm, "TitleText"), Color.white,
                                       "TitleText", 34f, QText, g.Title.W, 20f, 34f, 47.73f);
            // 纵向：Base 系 = **Bottom**(1024) · Liveop = **Capline**(8192)；横向都是 Center（不调 `Align*`）
            MenuDraw.SetVAlign(TitleLabel, g.TitleVAlign, g.Title);

            // `DescriptionText`：fs **28** · 基准 **22.5** · auto[**18**~**28**] · 折行 1 · 白
            DescriptionLabel = MenuDraw.TextBox(texts, g.Desc, ResolveTerm(DescriptionTerm, "DescriptionText"),
                                                Color.white, "DescriptionText", 28f, 18f, QText, 28f, 22.5f);
            // 横向：Liveop = **Left**(1)，其余 = Center(2)（`Label` 出厂就是居中 ⇒ Center 这一档不调）
            if (g.DescLeft) MenuDraw.AlignLeft(DescriptionLabel, g.Desc);
            // 纵向：六个变体都是 **Middle**(512) = `Label` 出厂档 ⇒ 不调

            // `Tap To continue`：fs **30** · 基准 **60** · auto[**18**~**30**] · 折行 1 · 色 (0.627,0.616,0.631)
            // 词条 = `Demo/Welcome/TapToContinue`（官方表 en `Click to continue` / zh `点击以继续`）。
            TapLabel = MenuDraw.TextBox(texts, g.Tap, Loc.T("Demo/Welcome/TapToContinue"), TapColor,
                                        "Tap To continue", 30f, 18f, QText, 30f, 60f);
            MenuDraw.SetVAlign(TapLabel, Label.VAlign.Capline, g.Tap);

            // `BlinkGraphic`（原版挂在这颗 TMP 上；`blinkSpeed 1.0` / `colorVariation 0.5` = 出厂值，
            // 36 个实例逐个实读、一个都没覆盖过）。公共件的时钟由本窗 `Tick` 推（批处理没有帧循环）。
            // 🔴 **`Texts` 关着的那一份（全屏宣告）不绑目标、`Silent = true`** —— 原版那颗组件跟着
            //   `Texts` 一起 inactive、`Update` 压根不跑；我们照「时钟不推、也不出声」办（`BlinkGraphic.Tick`
            //   在没目标时只出声、不推时钟；`Silent` 挡掉那条「本不该有目标」的告警）。
            var tap = TapLabel;
            if (tap != null && g.TextsActive)
            {
                var b = EnsureBlink();
                b.LogTag = "[EventPopup]";
                b.Silent = false;
                b.Bind(tap.gameObject, () => tap.color, c => tap.SetColor(c));
                b.Restart();
            }
            else if (EnsureBlink() != null)
            {
                _blink.Silent = true;
                _blink.Bind(null, null, null);
                _blink.Restart();
            }
        }

        /// <summary>🆕 允许调用方喂一张事件图（原版 = `LoadAsset(liveOps, warlordImageTag)` 那张）。
        /// 🔴 **本地没有任何调用方会填它** —— 事件图在远端 liveops 数据里（见文件头）；这个口是给
        /// 「将来真接上事件数据」那一轮准备的，⛔ 别拿别的图顶进来。
        /// ⚠️ 必须在 `Open()`/`Build()` **之前**赋值。</summary>
        public Texture2D EventImage;

        BlinkGraphic EnsureBlink()
        {
            if (_blink == null) _blink = GetComponent<BlinkGraphic>();
            if (_blink == null) _blink = gameObject.AddComponent<BlinkGraphic>();
            return _blink;
        }

        // ============================================================ 文案
        /// <summary>取一条官方词条；**取不到 ⇒ 空串 + 出声**（⛔ 不编文案、⛔ 不拿别的键顶）。
        /// <para>两处来源都要试：`Resources/official_names.json` 的 **bulk** 表（`Event_Description/*` 111 条在
        /// 那一份里 —— 见 `Core/Loc.cs` 的「数据面词条（bulk）」那一节）与 `Loc.Table`。</para>
        /// <para>⚠️ 原版这一格是 `LiveOpsAssetUtility.GetLabel(liveOps, tag)`：从**事件自己的** `EventLabel`
        /// 列表里按键取，取不到就 `CustomDebug.LogWarning` + 回空串 —— 我们这条警告与它同义。</para></summary>
        string ResolveTerm(string term, string which)
        {
            if (string.IsNullOrEmpty(term))
            {
                string tag = which == "TitleText" ? "标题" : "正文";
                string key = (EventNote ?? RootNameOf(Variant)) + "/" + which;
                if (_warnedNoEventData.Add(key))
                    Debug.LogWarning("[EventPopup] 「" + (EventNote ?? RootNameOf(Variant)) + "」的" + tag
                        + "**没有官方词条键** —— 原版从 liveops 事件的 `EventLabel` 里取"
                        + "（`LiveOpsAssetUtility.GetLabel`，键在事件 JSON 里、事件数据在远端 CCD，本地没有）"
                        + " ⇒ " + tag + "**留空**（= 原版「取不到标签 ⇒ `set_text(空)`」的画面结果）"
                        + "。⛔ 不编文案、⛔ 不拿别的键顶。");
                return "";
            }
            if (Loc.HasBulk(term)) return Loc.BulkOr(term, "");
            if (Loc.HasEntry(term)) return Loc.T(term);
            Debug.LogWarning("[EventPopup] `" + which + "` 的词条键 `" + term + "` 在本地两处表里都**没有**"
                             + "（`Resources/official_names.json` 的 bulk 表 · `Loc.Table`）⇒ 留空"
                             + "（⛔ 不拿别的键顶）。");
            return "";
        }

        static bool _noEventImageWarned;
        void NoteNoEventImage()
        {
            if (_noEventImageWarned) return;
            _noEventImageWarned = true;
            Debug.Log("[EventPopup] `Event image` **没有贴图** —— 原版 `BaseEventPopup.Open()` 用"
                      + " `LoadAsset(liveOps, ForegroundImage/FullScreenImage)` 喂它（tag 见文件头那张表）；"
                      + "**liveops 事件数据与图都在远端 CCD，本地没有** ⇒ 节点照建、贴图空着"
                      + "（= 原版 `LoadAsset` 取不到时 `set_sprite(空)` 的画面结果）。"
                      + "⛔ 不拿别的活动图顶（那会静默画错一张）。");
        }

        // ============================================================ 时钟（`BlinkGraphic`）
        /// <summary>帧循环里的入口。⚠️ 原版 `BlinkGraphic.Update` 读的是 **`Time.deltaTime`（scaled）**，
        /// 本仓公共件也是（判据 → `Shell/BlinkGraphic.cs` 文件头）。</summary>
        void Update() { Tick(Time.deltaTime); }

        /// <summary>批处理下没有帧循环 ⇒ 自检直接调（同 `RewardWindow.Tick` 那条约定）。</summary>
        public void Tick(float dt) { if (_blink != null) _blink.Tick(dt); }

        // ============================================================ 取图（不静默）
        Texture2D Tex(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name)) MissingArt.Add(name);
            return t;
        }
    }
}
