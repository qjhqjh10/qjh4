// PlayerProfileWindow.cs — 多人界面第 2 件：玩家档案窗（原版 `Player Profile Window`，6 个页签）
//
// ============================ 出处（唯一正本） ============================
// · 骨架（根 / 压暗 / `Menu Area` / `Tab Buttons` / `Tab  Area` / `Tab Content` / 6 个页签根）
//   → `资料/阶段二_多人界面_原版规格.md` **§2·1**（2026-09-27 回原始 JSON 换的真值）
// · **页签键内部**那一层（`button_bg` / `Icon` / `Label` / `Tab Toggle Title`）→ **本文件头**，
//   2026-09-27 用 `工具/menu_dump.py` 取的逐键真值（下面 `Tabs` 表里逐条标了出处）。
// 工具：`python 工具/menu_dump.py bundle_menus_assets_all --rt <pid> --depth N --md`
// 自检：`python 工具/menu_dump.py --verify-layout` —— 布局算法对着 §2·1 **手算过的 6 个键**逐位核；
//       ⚠️ 必须走**整棵树**：只测 `Tab Buttons` 自己的话，`Menu Area` 上那个 `m_Enabled=0` 的布局件
//          把父级挪歪的错误**测不出来**（2026-09-27 就这么漏过一次，值差 7.0）。
//
// ---- 🔴 窗口参数（`PlayerProfileMenu` 的 MB 原文；`MonoBehaviour_-4653119414410905038.json`）----
//   `type = 1 (Popup)` · `windowsPlacement = 15 (Popup)` · `closeOnESC = 1` · `updateNavPanel = 0` ·
//   `useDefaultCloseSoundIfNull = 1` · `extraScaleSmallScreen = 1.075`。
//   ⚠️ **1.075 不是 1.07**（`PracticeModePopup` 才是 1.07）—— 逐窗实测，别互推。
//
// ---- 🔴 这一件最要紧的三条判据（**读反编译定的，不是照字段猜的**）----
// ① **页签的选中态靠「换 sprite」，不是「换色」**：
//    · `EverguildToggle.colorTintOnValueChange = 0` ⇒ `onColor/offColor` 是**死值**（原版根本不采）；
//    · `changeSpriteOnValueChange = 1` ⇒ 换的是 `spriteToChange`（= 键里的 `button_bg` 那张 `Image`）；
//    · 选中 = `onSprite` = `40K_settings_button_hover`；未选 = `offSprite` = `40K_settings_button`。
//    · **互证**：`m_SpriteState.m_SelectedSprite` 与 `onSprite` 是**同一张**（PathID `-2307655919992762574`）。
//    出处：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/EverguildToggle.cs`（字段 + `ToggleTint` 的 `ShowIf` 门）
//          + 逐键取值 `MonoBehaviour_-3406534988526421454.json`（Profile 键）等 6 份。
//    ⇒ **我们照这条做**：`RefreshHighlights()` 只换 `button_bg` 的贴图，**不碰颜色**。
// ② **键上「没有名字条底图」** —— 别照 `MainMenuSubmenuWindow` 那套左栏（那是奖励窗/商店/收藏窗的画法，
//    它们每个键都有一张 `40k_main_bt_nametag`）。这里 `Label` 是**裸文字**，底下**没有**底板。
// ③ **出厂打开的是 `Title` 页**（第 3 个键），不是第一页 —— 唯一信号 = `Title Tab` 是**唯一**
//    `m_IsActive=true` 的页签根；`m_IsOn` 六个键全 0，从数据里看不出「哪个键亮着」（正本 §2·1）。
//
// ---- 🔴 一条如实记下的存疑（不是事实，是「查不到」）----
//   第 1 个键（`Profile Button`）的 `button_bg` 有**两处**与其余 5 个不同（其余 5 个彼此完全一致）：
//     · `m_AnchoredPosition.y` = 0（其余 −0.796906）
//     · `m_Color.a` = 1.0（其余 0.7098039388656616；rgb 六个键都是 (1, 0.5723676681518555, 0)）
//   本文件**照原样逐键复刻**（不去「对齐」它们）—— 这是**原版 JSON 的字面值**，可复查。
//   ⚠️ 但我们**不知道原版运行期会不会覆盖**（`EverguildButtonMaterialModifier` 会改**材质**，
//      不写 `Image.m_Color`；`colorTintOnValueChange=0` 说明 toggle 也不写）
//      ⇒ 进原版实况看一眼就知道，**列进 `资料/真Play待验清单.md`**。
//      ⚠️ 同一模式在**设置窗**那棵树上也有（键 0 与其余差 0.8px）⇒ 这大概是**预制体的共性**，不是笔误。
//
// ---- 我们挑的（原版取不到，逐条出声）----
//   · `m_PixelsPerUnitMultiplier = 0.92`（六个键的 `button_bg` 都是）：**我们这套 `ImageQuad` 没有这个属性**
//     ⇒ 没实现，如实记着（它是 UGUI 对九宫格/平铺的像素密度修正，Simple 图上影响很小）。
//   · 六个页签**内容**的数据（玩家 id / 连登天数 / 战绩 / 成就 / 段位）**全在服务器** ⇒ 按用户口径
//     「**有什么复刻什么，具体的数据和排名这些可以空着**」⇒ 照结构建、**留空态，不编数字**。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>一个页签键的真值。**逐键不同**（不是同一套参数的拷贝）—— 见文件头与 `Tabs` 表。</summary>
    public struct PpTabSpec
    {
        public readonly string Node, Label, Icon;
        /// <summary>原版 TMP 的 `m_fontSize`（画布像素）。⚠️ 第 3 键 33.65 / 第 5 键 32.75 是 **autosize 缩下来的**，别当笔误。</summary>
        public readonly float FontPx, AutoMin, AutoMax;
        /// <summary>`Icon` 的**绝对矩形**（原版 `Image + PreserveAspect`，四个键的框各不相同）。</summary>
        public readonly float IconL, IconT, IconR, IconB;
        /// <summary>`Label` 的绝对上下边（左右六个键恒为 113.48…268.48）。</summary>
        public readonly float LabT, LabB;
        /// <summary>`button_bg` 的 `m_AnchoredPosition.y`（第 1 键 0，其余 −0.796906）。</summary>
        public readonly float BgDy;
        /// <summary>`button_bg` 的 `m_Color.a`（第 1 键 1.0，其余 0.7098039388656616；rgb 恒 (1, 0.5723677, 0)）。</summary>
        public readonly float BgA;
        public PpTabSpec(string node, string label, string icon, float fontPx, float autoMin, float autoMax,
                         float iconL, float iconT, float iconR, float iconB,
                         float labT, float labB, float bgDy, float bgA)
        {
            Node = node; Label = label; Icon = icon; FontPx = fontPx; AutoMin = autoMin; AutoMax = autoMax;
            IconL = iconL; IconT = iconT; IconR = iconR; IconB = iconB;
            LabT = labT; LabB = labB; BgDy = bgDy; BgA = bgA;
        }
    }

    /// <summary>玩家档案窗。原版 `PlayerProfileMenu : GameWindowWithTabs`（+ 根上另挂 `TabbedWindowComponents`）。</summary>
    public class PlayerProfileWindow : GameWindowWithTabs
    {
        // ============================================================ 队列档
        //
        // 这一扇是**全屏的 Popup**（压暗 + 整屏面板），所以自成一档，**不占**页那一档（3005–3064）与
        // 别的弹窗（3100–3144）。取 **3150 起**（`grep "const int Q" Shell/*.cs` 看过：3150 以上没人用）。
        // 🔴 分层用**渲染队列**、不用 z（见 CLAUDE.md §三：透明物体按到相机的 3D 距离排序，屏中间的反而更近）。
        public const int QShade = 3150, QPanel = 3151, QChrome = 3152, QIcon = 3153, QText = 3154, QHit = 3155;
        /// <summary>六个页的内容从那之上起（各页自己再细分）。</summary>
        public const int QPageBase = 3160;

        // ============================================================ 骨架真值（正本 §2·1 · 绝对矩形）

        /// <summary>`Menu Dark Background`：纯色 **(0,0,0,0.7725)**、**原版 sprite = 0（就没图）**、无九宫格。
        /// 身上挂 `BackgroundCloseButton` ⇒ **点背景关窗**。</summary>
        public const float ShadeL = -1327.3f, ShadeT = -746.18f, ShadeR = 3247.3f, ShadeB = 1826.18f;
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.7725f);

        /// <summary>`Tab Buttons`：`a=(0,1)-(0,1) p=(1,.5) pos=(273.48,−533) sz=(178,705.512)`
        /// ⇒ **x 95.48..273.48 · y 180.24..885.752**。自带 `VerticalLayoutGroup`（**en=1**）：
        /// `spacing 10` · `align 5 (MiddleRight)` · `ctrlH=1` · `expandH=1` · `ctrlW=0` ⇒ 每键 **165 × 109.252**、
        /// y 从 **180.24** 步进 **119.252**、x **108.48..273.48**（横向由 MiddleRight 居中左缩 13）。</summary>
        public const float BarL = 95.48f, BarT = 180.24f, BarR = 273.48f, BarB = 885.752f;
        public const float KeyL = 108.48f, KeyR = 273.48f, KeyW = 165f, KeyH = 109.252f, KeyStep = 119.252f;
        /// <summary>键里 `button_bg` 的**原版贴图尺寸**（`40K_settings_button`，168×156）。</summary>
        public const float BgTexW = 168f, BgTexH = 156f;
        public const float LabL = 113.48f, LabR = 268.48f;

        public const string ArtTabOff = "40K_settings_button";        // offSprite（未选）
        public const string ArtTabOn = "40K_settings_button_hover";   // onSprite / m_SelectedSprite（选中）

        /// <summary>`Tab  Area`（⚠️ **名字里是两个空格**）：`pos=(1049,−540) sz=(1551.04,844)`
        /// ⇒ **273.48,118 .. 1824.52,962**。</summary>
        public const float AreaL = 273.48f, AreaT = 118f, AreaR = 1824.52f, AreaB = 962f;
        /// <summary>`Generic Window Red Background Big`：`a=(0,0)-(1,1) pos=(0,−0.459) sz=(0,−0.916)`
        /// ⇒ **273.48,118.92 .. 1824.52,962**。**Sliced**，九宫格 border = **(L42, B363, R655, T81)**，原图 **1100×701**。</summary>
        public const float RedT = 118.917f;
        public const string ArtRedBg = "UI_Deck_Information_Back";
        public static readonly Vector4 RedBorder = new Vector4(42f, 363f, 655f, 81f);

        /// <summary>`Generic Close Button Orange`：`a=(1,1)-(1,1) p=(.5,.5) pos=(−26.527,−38.719) sz=(74.386,75.605)`
        /// ⇒ **1760.80,118.92 .. 1835.19,194.52**（⚠️ **右溢出窗框 ~10.7px** —— 原版就这么摆，**别「对齐」掉**）。
        /// 图 `UI_Button_Round_background`（237×237，**Simple + preserveAspect**）。</summary>
        public const float CloseL = 1760.80f, CloseT = 118.92f, CloseR = 1835.19f, CloseB = 194.52f;
        public const string ArtClose = "UI_Button_Round_background";
        /// <summary>关闭钮的两个子件（**同一个矩形**，`Icon` 压在 `Background` 上）。</summary>
        public const float CloseInL = 1769.0f, CloseInT = 126.9f, CloseInR = 1825.8f, CloseInB = 185.0f;
        public const string ArtCloseBg = "40k_general_bt_yellow", ArtCloseIcon = "40k_general_bt_yellow_close";

        /// <summary>`Tab Content`：`a=(.05,0)-(.95,1) p=(0,.5) pos=(0,−0.459) sz=(0,−0.916)`
        /// ⇒ **351.03,118.92 .. 1746.97,962**（= `TabbedWindowComponents.tabHolder`）。</summary>
        public const float ContentL = 351.03f, ContentR = 1746.97f;
        /// <summary>`Avatar Tab` / `Title Tab` 两个页根**比内容区宽 33**（**两侧各溢 16.33**）—— 这是**真值，不是取整**，
        /// 照抄时**别顺手「对齐」掉**（正本 §2·1 / §2·3）。</summary>
        public const float WideL = 318.37f;

        // ============================================================ 六个键（逐键真值）
        //
        // 出处：`工具/menu_dump.py bundle_menus_assets_all --rt 1299990831746480690 --depth 3 --md`
        //       （`Tab Buttons` 的 RT pid = 1299990831746480690）· 2026-09-27。
        // ⚠️ `Icon` 的框**六个键各不相同**（`141.41×94.15` / `155×96.11` / `140.30×85.44` / …）
        //     —— 原版没挂 AspectRatioFitter，就是**各自摆的**，**别按一个尺寸统一**。
        // ⚠️ 文案与键名**不一致**：「`Trophies`」键的文案是 **`Achievements`**、「`Ranked`」键的文案是 **`Ranking`**。
        public static readonly PpTabSpec[] Tabs =
        {
            // Node              Label          Icon                            fs      autoMin autoMax  Icon 框(绝对)                       Label 上下  BgDy        BgA
            new PpTabSpec("Profile Button",  "Profile",     "40K_Profile_icon_profile",        35.0f,  10f, 35f, 120.28f,175.20f,261.68f,269.34f, 246.57f,286.57f,  0f,        1.0f),
            new PpTabSpec("Avatar Button",   "Avatar",      "40K_Profile_icon_avatar",         35.0f,  10f, 35f, 113.48f,293.46f,268.48f,389.58f, 364.12f,404.12f, -0.796906f, 0.7098039388656616f),
            new PpTabSpec("Title Button",    "Title",       "40K_Profile_icon_title",          33.65f, 10f, 35f, 120.83f,407.62f,261.13f,493.06f, 493.06f,524.98f, -0.796906f, 0.7098039388656616f),
            new PpTabSpec("Battle Log Button","Battle Log","40K_Profile_icon_battlelog",       35.0f,  10f, 35f, 120.28f,532.86f,261.68f,626.20f, 602.63f,642.63f, -0.796906f, 0.7098039388656616f),
            new PpTabSpec("Trophies",        "Achievements","40K_Profile_icon_Trophies",       32.75f, 10f, 35f, 120.28f,653.41f,261.68f,746.75f, 721.88f,761.88f, -0.796906f, 0.7098039388656616f),
            new PpTabSpec("Ranked",          "Ranking",     "40k_UI_icon_ranked_Skirmish",     35.0f,  10f, 35f, 120.28f,777.30f,261.68f,864.41f, 841.13f,881.13f, -0.796906f, 0.7098039388656616f),
        };

        /// <summary>`button_bg` 的 `m_Color`（六个键 rgb 相同、只差 a）。</summary>
        public static readonly Color BgTint = new Color(1f, 0.5723676681518555f, 0f);

        /// <summary>出厂打开的那一页（**`Title`，第 3 个键**）—— 判据见文件头 ③。</summary>
        public const int DefaultTabIndex = 2;

        // ============================================================ 建的

        public readonly List<string> MissingArt = new List<string>();
        readonly ImageQuad[] _btnBg = new ImageQuad[Tabs.Length];
        readonly WindowTabBase[] _pages = new WindowTabBase[Tabs.Length];
        Transform _content;

        /// <summary>原始页签根（`Tab Content` 下的六个），调试/自检用。</summary>
        public Transform[] PageRoots { get { return _pageRoots; } }
        readonly Transform[] _pageRoots = new Transform[Tabs.Length];
        /// <summary>六个键的 `button_bg`（自检要按它断选中态）。</summary>
        public ImageQuad[] ButtonBgs { get { return _btnBg; } }

        /// <summary>按页签类型取那一页的实例（自检用 —— 页是 `MonoBehaviour`，`FindChild` 只能拿到节点）。</summary>
        public WindowTabBase Page(WindowTabType t)
        {
            for (int i = 0; i < _pages.Length; i++)
                if (_pages[i] != null && _pages[i].Type == t) return _pages[i];
            return null;
        }

        public PlayerProfileWindow()
        {
            // 左栏的**视觉顺序** = 六个键的节点顺序（与原版 `TabbedWindowComponents.tabs[]` 同序：
            // Profile→Avatar→Title→Battle Log→Trophies→Ranking，正本 §2·1）。
            // ⚠️ 这里**没有母版键**：原版 `Tab Buttons` 就 6 个子节点，母版在 `TabbedWindowComponents.tabPrefab`
            //    那个**独立资产**里（不是子节点）⇒ `visualTypes` 六项全是真页签，没有 `None`。
            visualTypes.Clear();
            visualTypes.Add(WindowTabType.ProfileInfo);
            visualTypes.Add(WindowTabType.ProfileAvatar);
            visualTypes.Add(WindowTabType.ProfileTitle);
            visualTypes.Add(WindowTabType.ProfileBattleLog);
            visualTypes.Add(WindowTabType.ProfileTrophies);
            visualTypes.Add(WindowTabType.ProfileRanking);
        }

        public static PlayerProfileWindow Create(WindowsManager mgr)
        {
            var go = new GameObject("Player Profile Window");
            var win = go.AddComponent<PlayerProfileWindow>();
            win.type = WindowType.Popup;                       // 实证 type=1
            win.placement = WindowsPlacement.Popup;            // 实证 windowsPlacement=15
            win.closeOnEsc = true;                             // 实证 closeOnESC=1
            win.extraScaleSmallScreen = 1.075f;                // 实证 —— ⚠️ 不是 1.07
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            LastOpened = win;                                  // 自检用（同 `PracticeModePopup` 那几扇的写法）
            return win;
        }

        /// <summary>最近一次 `Create` 出来的那扇（自检断言「点头像 ⇒ 真的开了这扇窗」）。</summary>
        public static PlayerProfileWindow LastOpened;

        /// <summary>🆕 2026-10-03（§三第 29 条 A3②）：**正在看谁的档案**。
        /// `null` = 自己那一份（原版默认）；非空 = **别人的**（从排行榜的行点进来的）。
        /// 🔴 **别人的资料全在服务器**（那个玩家的存档）⇒ 这时六个页**都不铺本地数据**，
        /// 只画一行如实说明 —— **绝不拿我们自己的数据冒充他**（红线：不许静默失败 / 不许假装）。</summary>
        public string ViewedPlayer;

        /// <summary>开「别人那一份」—— 原版行被点时开的就是这一扇（`profileButton`）。</summary>
        public static PlayerProfileWindow CreateFor(WindowsManager mgr, string playerName)
        {
            var w = Create(mgr);
            w.ViewedPlayer = playerName;
            return w;
        }

        public override void Open()
        {
            Build();
            // 出厂落在 **Title** 页（照原版；不是第一页）
            if (tabButtons != null) tabButtons.Click(DefaultTabIndex);
            RefreshHighlights();
        }

        // ============================================================ 建

        void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) DestroyNow(root.GetChild(i).gameObject);
            MissingArt.Clear();

            // 1) 压暗 + 点背景关窗（原版 `Menu Dark Background` 上挂的就是 `BackgroundCloseButton`）
            MenuDraw.Rect(root, CardArt.Solid(), new PxRect(ShadeL, ShadeT, ShadeR, ShadeB),
                          "Menu Dark Background", QShade, ShadeColor);
            MenuDraw.Hit(root, "BackgroundHit", new PxRect(0f, 0f, 1920f, 1080f), QShade + 1, () => Close());

            // 2) `Menu Area`（原版身上挂了个布局件但 **`m_Enabled=0` ⇒ 不跑**，所以它就是个普通满屏容器）
            var area = MenuDraw.Node(root, "Menu Area", new PxRect(0f, 0f, 1920f, 1080f));

            // 3) 左栏六个键
            BuildTabBar(area);

            // 4) `Tab  Area`：红底 + 关闭钮
            var tabArea = MenuDraw.Node(area, "Tab  Area", new PxRect(AreaL, AreaT, AreaR, AreaB));
            {
                var tex = ArtInternal(ArtRedBg);
                if (tex != null)
                    MenuDraw.Nine(tabArea, tex, new PxRect(AreaL, RedT, AreaR, AreaB), RedBorder,
                                  tex.width, tex.height, QPanel, null, true, "Generic Window Red Background Big");
                // ⚠️ Sliced 的九宫格 border 按**贴图原始像素**给（42,363,655,81），不是按显示尺寸缩过的
            }
            BuildCloseButton(tabArea);

            // 5) `Tab Content` + 六个页根（**顺序照原版 `tabs[]`**）
            _content = MenuDraw.Node(tabArea, "Tab Content", new PxRect(ContentL, RedT, ContentR, AreaB));
            tabHolder = _content;
            BuildPages();
        }

        void BuildTabBar(Transform area)
        {
            var bar = MenuDraw.Node(area, "Tab Buttons", new PxRect(BarL, BarT, BarR, BarB));
            var tb = bar.gameObject.AddComponent<TabButtons>();
            tb.options.Clear();
            tabButtons = tb;

            for (int i = 0; i < Tabs.Length; i++)
            {
                // 🔴 **布局后的真值**：每键 165 × 109.252，y 从 180.24 步进 119.252（正本 §2·1；
                //    算法已由 `menu_dump.py --verify-layout` 对着手算值逐位核过）。
                float top = BarT + KeyStep * i, bot = top + KeyH;
                var b = MenuDraw.Node(bar, Tabs[i].Node, new PxRect(KeyL, top, KeyR, bot));

                // `button_bg`：`a=(0,0)-(1,1) sz=(0,0)` ⇒ 铺满键；`m_AnchoredPosition.y` **逐键不同**（见文件头存疑）
                var bgRect = new PxRect(KeyL, top + Tabs[i].BgDy, KeyR, bot + Tabs[i].BgDy);
                var c = BgTint; c.a = Tabs[i].BgA;
                _btnBg[i] = MenuDraw.Rect(b, ArtInternal(ArtTabOff), bgRect, "button_bg", QChrome, c);

                // `Icon`：原版 `Simple + PreserveAspect`，**逐键各自的框**
                var t = Tabs[i];
                MenuDraw.Rect(b, ArtInternal(t.Icon), new PxRect(t.IconL, t.IconT, t.IconR, t.IconB),
                              "Icon", QIcon, null, true);

                // `Label`（155 宽、裸文字，**底下没有底板**）> `Tab Toggle Title`
                // 🔴 TMP 原文：`m_TextWrappingMode = 0`（**不折行**）· `m_overflowMode = 0` ·
                //    `m_enableAutoSizing = 1` + `m_fontSizeMin/Max = 10/35`（**六个键的 max 都是 35**）·
                //    对齐 `2/512`（水平居中 / 垂直居中）· 色 (1,1,1,1) · `m_margin = 0`。
                //    `m_fontSize` 六个键是 35/35/**33.65**/35/**32.75**/35 —— 后两个是**编辑器里 autosize 缩过的**
                //    ⇒ 我们把**那个值当基准**、`max` 仍给 35，让 TMP 自己缩（与原版同一套设置）。
                var labRect = new PxRect(LabL, t.LabT, LabR, t.LabB);
                var lbl = MenuDraw.Text(b, labRect, t.Label, Color.white, "Tab Toggle Title", t.FontPx, QText);
                if (lbl != null && t.AutoMax > t.AutoMin)
                    lbl.SetAutoFitBox(LayoutSpace.Px(labRect.W), LayoutSpace.Px(labRect.H), t.AutoMin, t.AutoMax);

                // 点击区：整键（原版是 `EverguildToggle`，我们只用它的「点一下切页」语义）
                int captured = i;
                MenuDraw.Hit(b, "Hit", new PxRect(KeyL, top, KeyR, bot), QHit,
                             () => { if (tabButtons != null) tabButtons.Click(captured); });

                tb.options.Add(new TabButtons.Option
                {
                    type = visualTypes[i],
                    button = b.GetComponentInChildren<WindowButton>(true),
                });
            }
            tb.tabButtonPrefab = null;   // 原版母版是独立资产、**不是子节点** ⇒ 这里没有要关的母版
            tb.Initialize(this);
        }

        void BuildCloseButton(Transform tabArea)
        {
            var c = MenuDraw.Node(tabArea, "Generic Close Button Orange",
                                  new PxRect(CloseL, CloseT, CloseR, CloseB));
            MenuDraw.Rect(c, ArtInternal(ArtClose), new PxRect(CloseL, CloseT, CloseR, CloseB), "Image", QChrome, null, true);
            MenuDraw.Rect(c, ArtInternal(ArtCloseBg), new PxRect(CloseInL, CloseInT, CloseInR, CloseInB),
                          "Background", QChrome + 1, null, true);
            MenuDraw.Rect(c, ArtInternal(ArtCloseIcon), new PxRect(CloseInL, CloseInT, CloseInR, CloseInB),
                          "Icon", QChrome + 2, null, true);
            MenuDraw.Hit(c, "Hit", new PxRect(CloseL, CloseT, CloseR, CloseB), QHit, () => Close());
        }

        /// <summary>六个页根。**出厂 active 状态照原版**：只有 `Title Tab` 是 true，其余五个 false
        /// —— `GameWindowWithTabs.ChangeTab` 会按当前页重设，这里先摆成出厂的样子（自检要断它）。</summary>
        void BuildPages()
        {
            tabs.Clear();
            for (int i = 0; i < Tabs.Length; i++)
            {
                // ⚠️ `Avatar Tab` / `Title Tab` 的根**两侧各溢 16.33**（真值）
                float l = (i == 1 || i == 2) ? WideL : ContentL;
                var r = new PxRect(l, RedT, ContentR, AreaB);
                var page = MenuDraw.Node(_content, Tabs[i].Node.Replace(" Button", "") + " Tab", r);
                _pageRoots[i] = page;
                // ⚠️ `AddComponent(Type)` 返回的是 `Component` —— 必须显式转（2026-09-27 第一次编译就栽在这）
                // 🆕 2026-10-03（A3②）：看**别人**的档案时六页都换成占位（数据在服务器，本地没有）
                System.Type comp = ViewedPlayer != null ? typeof(StrangerProfilePage) : PageComponentOf(i);
                var mb = page.gameObject.AddComponent(comp) as ProfilePage;
                if (mb != null) { mb.Win = this; mb.PageIdx = i; }
                _pages[i] = mb;
                if (mb != null) tabs.Add(mb);
                page.gameObject.SetActive(i == DefaultTabIndex);
            }
            for (int i = 0; i < _pages.Length; i++) if (_pages[i] != null) _pages[i].Setup();
        }

        /// <summary>哪个键对应哪个页组件类（**类名照原版**：`ProfileTab` / `AvatarTab` / `TitleTab` /
        /// `BattleLogTab` / `AchievementsMenu` / `RankedTab`）。</summary>
        static System.Type PageComponentOf(int i)
        {
            switch (i)
            {
                case 0: return typeof(ProfileTab);
                case 1: return typeof(AvatarTab);
                case 2: return typeof(TitleTab);
                case 3: return typeof(BattleLogTab);
                case 4: return typeof(AchievementsMenu);
                default: return typeof(RankedTab);
            }
        }

        /// <summary>页下标 → 页类型（六页各自那句 `override WindowTabType Type` 的**同一份**）。
        /// 🆕 2026-10-03：`StrangerProfilePage` 一个类顶六页，靠它按 `PageIdx` 取。</summary>
        public static WindowTabType TabTypeOf(int i)
        {
            switch (i)
            {
                case 0: return WindowTabType.ProfileInfo;
                case 1: return WindowTabType.ProfileAvatar;
                case 2: return WindowTabType.ProfileTitle;
                case 3: return WindowTabType.ProfileBattleLog;
                case 4: return WindowTabType.ProfileTrophies;
                default: return WindowTabType.ProfileRanking;
            }
        }

        /// <summary>左栏选中态：**只换贴图、不碰颜色**（判据见文件头 ①）。
        /// 基类在 `ChangeTab` 末尾会调它 ⇒ 谁都不必再记着调。</summary>
        public override void RefreshHighlights()
        {
            for (int i = 0; i < _btnBg.Length; i++)
            {
                if (_btnBg[i] == null) continue;
                bool on = visualTypes[i] == CurrentTab;
                _btnBg[i].SetTexture(ArtInternal(on ? ArtTabOn : ArtTabOff));
            }
        }

        // ============================================================ 小工具

        /// <summary>取图（`CardArt.MenuUi` 会依次找 `ui_menu/ → ui_deck/ → ui/`）。取不到记进 `MissingArt`。
        /// 🔴 `CardArt.MenuUi` **不做「空格 → 下划线」转换** —— 传的必须是**导入后的文件名**。
        /// （页也从这里取图 ⇒ 只此一个入口，缺图不会两处各记一份。）</summary>
        public Texture2D ArtInternal(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name)) MissingArt.Add(name);
            return t;
        }

        static void DestroyNow(GameObject go)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) { Object.DestroyImmediate(go); return; }
#endif
            Object.Destroy(go);
        }

        public override void NotifyNotBuilt(string what)
        {
            Debug.Log("[Profile] `" + what + "` 还没实现");
        }
    }

    // ============================================================ 六个页的公共底座
    //
    // ⚠️ 名字**照原版类名**：`Profile Tab`→`ProfileTab` · `Avatar Tab`→`AvatarTab` · `Title Tab`→`TitleTab` ·
    //    `Battle Log Tab`→`BattleLogTab` · `Trophies Tab`→**`AchievementsMenu`**（原版就叫这个：
    //    键名叫 Trophies、文案是 Achievements）· `Ranking Tab`→`RankedTab`。字段名也照原版
    //    （出处：各页自己的 MonoBehaviour JSON，2026-09-27 用 `menu_dump.py` 读的 `字段: …` 那一列）。

    /// <summary>档案窗一页的公共底座。原版是 `WindowTabBase&lt;PlayerProfileMenu&gt;`；
    /// 我们沿用 `WindowTabBase`（它的 `Type` 是 `WindowTabType` ⇒ 六个档案页的值在**30–35** 段）。</summary>
    public abstract class ProfilePage : WindowTabBase
    {
        /// <summary>宿主窗（`BuildPages` 建完就赋）。</summary>
        public PlayerProfileWindow Win;

        /// <summary>本页的根矩形（原版**逐页不同**：`Avatar`/`Title` 两侧各溢 16.33）。</summary>
        public abstract PxRect PageRect { get; }

        protected Transform Root { get { return transform; } }

        /// <summary>本页的队列起档（六页各占一段 10 个号，互不重叠）。</summary>
        protected int Q { get { return PlayerProfileWindow.QPageBase + PageIndex * 10; } }

        /// <summary>本页在 `Tabs` 表里的下标（0–5）。
        /// 🆕 2026-10-03：改成 **`virtual`**（原来 `abstract`）—— 六个具体页仍各自覆写成常量，
        /// 而 `StrangerProfilePage` **一个类要顶六页** ⇒ 靠建窗那一处统一赋的 `PageIdx`。</summary>
        protected virtual int PageIndex { get { return PageIdx; } }
        /// <summary>建窗那一处赋的页下标（`StrangerProfilePage` 用它算队列档与类型）。</summary>
        public int PageIdx;

        /// <summary>**裁切边界**（画布像素）。等价于原版 `Viewport` 上那个 `RectMask2D`。
        /// 滚动区在画内容**之前**设一次、画完清掉（照 `ForgeTab.BuildRewardCells` 的用法）。
        /// 🆕 **2026-10-03：横纵两轴都真被截了** —— `MenuDraw.Rect` 补了纵向 uv 裁剪。
        /// （原来那句「只有横轴真被截、纵轴靠 `MenuScroll.Intersects` 整块不建兜着」**已作废**：
        ///  `Intersects` 只管「整块在视口外」，**部分越界的件**它是放行的 ⇒ 得靠这里的逐 quad 裁剪。）</summary>
        protected PxRect? Clip;

        /// <summary>取图（走宿主窗那一个入口，取不到会记进 `MissingArt`）。</summary>
        protected Texture2D Art(string name) { return Win != null ? Win.ArtInternal(name) : null; }

        // ---- 绘图转发（都用本页的队列档；`MenuDraw` 是唯一的画图层，别在这再写一份）----
        protected static Transform Node(Transform parent, string name, PxRect r) { return MenuDraw.Node(parent, name, r); }
        protected Transform Node(string name, PxRect r) { return MenuDraw.Node(Root, name, r); }

        /// <summary>按矩形摆一张图。`art == null` ⇒ 纯色块（原版那种「没 sprite、只有 m_Color」的件）。</summary>
        protected ImageQuad Rect(Transform parent, string art, PxRect r, string name, int qOff,
                                 Color? tint = null, bool keepAspect = false)
        {
            var tex = art == null ? CardArt.Solid() : Art(art);
            return MenuDraw.Rect(parent, tex, r, name, Q + qOff, tint, keepAspect, Clip);
        }
        protected ImageQuad Rect(string art, PxRect r, string name, int qOff, Color? tint = null, bool keepAspect = false)
        { return Rect(Root, art, r, name, qOff, tint, keepAspect); }

        /// <summary>纯色块。</summary>
        protected ImageQuad Solid(Transform parent, PxRect r, string name, int qOff, Color tint)
        { return MenuDraw.Rect(parent, CardArt.Solid(), r, name, Q + qOff, tint); }

        /// <summary>🆕 **装饰品头像**那一批的图（`Resources/Art/avatars/`）。
        /// 🔴 走 `CardArt.Cosmetics` 而**不是** `MenuUi` —— 那一批在**独立目录**，而且
        /// **名字含空格、原样传**（`Avatar_UM_Attack Bike`，别换下划线）。
        /// 取不到时记进 `MissingArt`（**出声**，不静默画个白块）。</summary>
        protected ImageQuad CosmeticRect(Transform parent, string art, PxRect r, string name, int qOff,
                                         bool keepAspect = true)
        {
            Texture2D tex = null;
            if (!string.IsNullOrEmpty(art))
            {
                tex = CardArt.Cosmetics(art);
                if (tex == null && Win != null && !Win.MissingArt.Contains(art)) Win.MissingArt.Add(art);
            }
            return MenuDraw.Rect(parent, tex, r, name, Q + qOff, null, keepAspect, Clip);
        }

        /// <summary>九宫格（原版 `Image.Type = Sliced`）。`border` 按**贴图原始像素**给。</summary>
        protected GameObject Nine(Transform parent, string art, PxRect r, Vector4 border, string name, int qOff,
                                  Color? tint = null, bool fillCenter = true)
        {
            var tex = Art(art);
            if (tex == null) return null;
            return MenuDraw.Nine(parent, tex, r, border, tex.width, tex.height, Q + qOff, tint, fillCenter, name);
        }

        /// <summary>TMP 规矩：`m_TextWrappingMode = 1`（限宽换行）/ `0`（不折行）。
        /// 🔴 **别用 `SetFontSize(px/108)`** —— 那会大 2.7 倍；走 `SetGlyphHeight`（`MenuDraw.Text` 已经做了）。</summary>
        protected Label Text(Transform parent, string text, PxRect r, Color color, string name, float fontPx,
                             int qOff, bool autoFit = false, float autoMinPx = 0f, bool alignLeft = false,
                             bool wrap = false)
        {
            // 裁切：文字没法截 uv ⇒ 与 `MenuWindowBase.Text` 同一条规矩：**整块在视口外就不建**
            if (Clip.HasValue && (r.x2 <= Clip.Value.x1 || r.x1 >= Clip.Value.x2)) return null;
            var lb = MenuDraw.Text(parent, r, text, color, name, fontPx, Q + qOff);
            if (lb != null)
            {
                if (wrap) lb.SetWrapWidth(LayoutSpace.Px(r.W));
                if (autoFit && fontPx > autoMinPx)
                    lb.SetAutoFitBox(LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), autoMinPx, fontPx);
                if (alignLeft) MenuDraw.AlignLeft(lb, r);
            }
            return lb;
        }

        protected Transform Hit(Transform parent, string name, PxRect r, int qOff, System.Action onClick)
        { return MenuDraw.Hit(parent, name, r, Q + qOff, onClick); }

        /// <summary>本页的滚动区。**全壳只有 `MenuScroll` 这一份滚动实现**（别在这再写一套偏移+夹取）。
        /// `vertical = true` 用 `TopAligned`（原版 `m_Vertical 1` 那种）。</summary>
        protected MenuScroll NewScroll(PxRect viewport, float contentW, float contentH, bool vertical)
        {
            var s = vertical ? MenuScroll.TopAligned(viewport, contentH)
                             : MenuScroll.LeftAligned(viewport, contentW);
            s.Owner = Root.gameObject;     // 切走的页签里那些滚动区靠它被跳过
            PointerLayer.RegisterScroll(s);
            return s;
        }

        /// <summary>本页内容。**只在第一次开窗时调一次**（换页只切 `activeSelf`，不重建）。</summary>
        public sealed override void Setup()
        {
            // 🔴 **先把自己名下那些滚动区撤掉**（2026-09-27 补）：下面会「把 Root 的子节点全删了重建」，
            //   而重建出来的是**新 `MenuScroll` 对象** —— 旧的那些 `Owner` 是这个 **Root**（重建时它不会死）
            //   ⇒ 光靠「宿主销毁」判不出它们已经没用 ⇒ 登记表**每开一次窗涨一批**、而且**旧条目还能被滚轮命中**
            //   （`OnChanged` 指向已经销毁的节点）。判据 → `项目任务.md` §〇 A ②。
            PointerLayer.UnregisterOwnedBy(Root.gameObject);
            for (int i = Root.childCount - 1; i >= 0; i--) DestroyNow(Root.GetChild(i).gameObject);
            Clip = null;
            Build();
            if (Win != null && Win.MissingArt.Count > 0)
                Debug.LogWarning("[Profile] `" + Tag + "` 页有 " + Win.MissingArt.Count
                                 + " 张图取不到（**这些件没画**）：" + string.Join("、", Win.MissingArt.ToArray())
                                 + " —— 导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
        }

        /// <summary>本页的代号（日志用；照原版类名）。</summary>
        public string Tag { get { return GetType().Name; } }

        protected abstract void Build();

        /// <summary>立刻销毁（批处理下没有帧循环 ⇒ `Object.Destroy` 不生效）。
        /// 🔴 `protected`：各页的**滚动重建**（`MenuScroll.OnChanged`）也要用它 —— 别各页再抄一份。</summary>
        protected static void DestroyNow(GameObject go)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) { Object.DestroyImmediate(go); return; }
#endif
            Object.Destroy(go);
        }
    }

    // ============================================================ 别人那一份（A3②）

    /// <summary>🆕 2026-10-03（§三 第 29 条 A3②）：**看别人的档案**时，六个页都换成这一个。
    ///
    /// 原版：排行榜的行被点 ⇒ `profileButton` ⇒ 开**那个玩家**的档案窗，六页都由服务器填。
    /// 我们：**没有别人的数据**（全在服务器），也**不拿本地自己那一份冒充他**（那会是假信息）
    /// ⇒ 照红线「不许静默失败」：开**同一扇窗**（外壳/左栏/关闭钮一模一样），
    /// 每页画两行**如实说明**。判据（原版行为）→ `LeaderboardRow.OnRowClicked` 的注释。</summary>
    public class StrangerProfilePage : ProfilePage
    {
        public override WindowTabType Type { get { return PlayerProfileWindow.TabTypeOf(PageIdx); } }

        public override PxRect PageRect
        {
            get
            {
                // 与六个具体页同值：`Avatar`/`Title` 两页两侧各溢 16.33（判据 → `BuildPages` 那段）
                float l = (PageIdx == 1 || PageIdx == 2) ? PlayerProfileWindow.WideL : PlayerProfileWindow.ContentL;
                return new PxRect(l, PlayerProfileWindow.RedT, PlayerProfileWindow.ContentR, PlayerProfileWindow.AreaB);
            }
        }

        protected override void Build()
        {
            string who = Win != null ? Win.ViewedPlayer : null;
            var r = PageRect;
            float cx = r.CX, cy = r.CY;

            var l1 = MenuDraw.Text(Root, new PxRect(cx - 700f, cy - 70f, cx + 700f, cy + 10f),
                                   "「" + (who ?? "?") + "」的档案", new Color(0.98f, 0.686f, 0.169f, 1f),
                                   "Stranger Name", 45f, Q, 1400f, 23f);
            if (l1 != null) l1.SetRenderQueue(Q);
            var l2 = MenuDraw.Text(Root, new PxRect(cx - 700f, cy + 20f, cx + 700f, cy + 120f),
                                   "服务器数据 —— 本地版只有你自己那一份（原版这一页由服务器填）",
                                   Color.white, "Stranger Note", 32f, Q + 1, 1400f, 18f);
            if (l2 != null) l2.SetRenderQueue(Q + 1);

            Debug.Log("[Profile] 看**别人**的档案：「" + (who ?? "?") + "」的 " + Tabs[PageIdx]
                      + " 页 —— 原版这一页的数据**全在服务器**（那个玩家的存档）"
                      + "⇒ 我们**不拿本地自己那一份冒充他**，如实画一行说明（§三第29条 A3②）");
        }

        public static readonly string[] Tabs =
        { "Profile", "Avatar", "Title", "Battle Log", "Trophies", "Ranking" };
    }
}
