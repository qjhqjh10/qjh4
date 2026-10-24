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
        // ✅ **2026-10-11（A252）可见性收窄**：这行原是 2026-10-04（A47 接线批）**整行**放宽成 `public` 的；
        //   留 `public` 的两个各有实测引用（脚本扫全工程 301 个 `.cs`、剔注释、剔本文件）：
        //   `QShade` 1 处 · `QHit` 2 处（都在 `Editor/MainMenuScene.cs`：`CheckShadeRule` / `CheckAbsorbRule`
        //   与「档案窗 vs 顶栏」那类跨窗比较）；`QPanel`/`QChrome`/`QIcon`/`QText` **外部引用 = 0** ⇒ 回 `const`。
        //   ⚠️ 另核过：同文件里的两个页类（`ProfilePage` / `StrangerProfilePage`）用的是 `QPageBase` 与那几个
        //   几何常量，**不用**被收的这四个；`class X : PlayerProfileWindow` 全 0。
        //   ⚠️ `QPageBase`（下面那个）**不在本件范围**：页类**在别的类里**非限定用它 ⇒ 必须留 `public`。
        public const int QShade = 3150;     // ✅ 留 `public`：`Editor/MainMenuScene.cs` 引用（1 处）
        const int QPanel = 3151;            // 3151 红底那一层内容（`Generic Window Red Background Big`）
        const int QChrome = 3152;           // 3152 窗框 / 左栏
        const int QIcon = 3153;             // 3153 图标层
        const int QText = 3154;             // 3154 文字层
        public const int QHit = 3155;       // ✅ 留 `public`：`Editor/MainMenuScene.cs` 引用（2 处）
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
        /// <summary>🔴 **2026-10-18（A1053）**：关窗钮那两颗子件（`Background` / `Icon`，同矩形
        /// `CloseInL..CloseInB`）**自己的** `m_RaycastPadding`（原版实读 `(-20)⁴`；L,B,R,T · **负 = 外扩**）
        /// ⇒ 命中区 = 子件矩形外扩 20 = **96.86 × 98.13**（⛔ 不是根矩形 `CloseL..CloseB` 的 74.39×75.60）。
        /// 算式只走 `MenuDraw.PaddedRect`；口径 → `普查_全仓命中区与关闭键族.md` §〇-1。</summary>
        static readonly Vector4 ClosePad = new Vector4(-20f, -20f, -20f, -20f);

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
        // 🆕 **2026-10-22（P1）**：下面这六个 `Label` 只是**英文原文 / 词条取不到时的兜底** ——
        //    上屏一律走 <see cref="TabLabel"/>（官方词条，见 <see cref="TabTerms"/>）。
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

        /// <summary>🔴 **2026-10-22（P1）**：六个键的**官方词条键** —— 判据 = 每颗
        /// `Tab Toggle Title` 上那个 `Localize.mTerm`（`资料/普查产出_1022/N1_账户界面与数据普查.md` §一 逐键实读）。
        /// ⚠️ 节点名与词条**不同源**：`Trophies` 键 → `Achievements/Achievements` · `Ranked` 键 → `Ranked/RankingTab`。
        /// 官方两列（`数据/游戏数据/官方本地化_en+zh-CN.json`）：
        /// `Profile`/个人资料 · `Avatar`/头像 · `Title`/头衔 · `Battle Log`/战斗记录 · `Achievements`/成就 · `Ranking`/排名。</summary>
        public static readonly string[] TabTerms =
        {
            "PlayerProfile/Profile", "PlayerProfile/Avatar", "PlayerProfile/Title",
            "PlayerProfile/BattleLog", "Achievements/Achievements", "Ranked/RankingTab",
        };

        /// <summary>键上屏文案：**走官方词条**（取不到才回 <see cref="Tabs"/> 里那份英文原文）。
        /// 🆕 2026-10-22（P1）：改前直接印 `Tabs[i].Label` ⇒ 中文档下六个键全是英文（参考图是中文）。</summary>
        public static string TabLabel(int i)
        {
            string v = Loc.T(TabTerms[i]);
            return string.IsNullOrEmpty(v) || v == TabTerms[i] ? Tabs[i].Label : v;
        }

        /// <summary>出厂打开的那一页（**`Title`，第 3 个键**）—— 判据见文件头 ③。</summary>
        public const int DefaultTabIndex = 2;

        // ============================================================ 建的

        public readonly List<string> MissingArt = new List<string>();
        readonly ImageQuad[] _btnBg = new ImageQuad[Tabs.Length];
        /// <summary>A17 换图用：每个页签底图对应的按钮（选中态由 `RefreshHighlights` 换底图 ⇒ 要同步常态图）。</summary>
        readonly WindowButton[] _btnWb = new WindowButton[Tabs.Length];
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
            // 🔴 **2026-10-04（A47 接线批）订正档号：`QShade + 1`(3151) → `QShade`(3150)** ——
            //   规矩 = 「压暗层的命中区落在**压暗层自己那一档**，且严格低于本窗任何内容命中区档」
            //   （判据 → `MenuDraw.ShadeHit` 的注释 · `资料/待办判据_阶段二与联机.md` §A25·补（一））。
            //   ⚠️ 3151 恰好是本窗 `QPanel`（红底那**一层内容**）—— 压暗层的命中区压在内容层上是不合规矩的。
            MenuDraw.ShadeHit(root, new PxRect(0f, 0f, 1920f, 1080f), QShade, QHit, () => Close(), "BackgroundHit");

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
            // 🆕 **2026-10-06（A94）：红底那块面板吸收点击**。判据 = 原版 prefab
            //   `Player Profile Window > Menu Area > Tab  Area > Generic Window Red Background Big`
            //   那颗 `Image` 的 **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读），
            //   rect = 273.48,118.917→1824.52,962（**比 `Tab Area` 容器低 0.917px** —— 吸收层照 `Image` 那颗给）。
            MenuDraw.Absorb(root, "AbsorbHit", new PxRect(AreaL, RedT, AreaR, AreaB), QShade, QHit);
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

                // 🔴 **2026-10-24（用户拍板 · `U2` 判据）**：本 build 的 prefab 有 **6 颗**页签
                //   （`Profile/Avatar/Title/Battle Log/Trophies/Ranked`），但**用户自己的原版实拍只有 5 颗**、
                //   第 5 颗下方是**空带** ⇒ 这是**版本差**，用户明确说「最下面多了一个按钮及其界面」⇒ **隐藏第 6 颗**。
                //    ⛔ **页面与 `Tabs`/`visualTypes` 表都保留**（删掉这一行即可复原）；`Tab Buttons` 不是真
                //    `VerticalLayoutGroup` ⇒ 前 5 颗的位置**逐位不变**。
                //    ⚠️ `continue` 必须早于 `tb.options.Add`（否则仍会被算进页签索引）。
                if (Tabs[i].Node == "Ranked") { b.gameObject.SetActive(false); continue; }

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
                var lbl = MenuDraw.Text(b, labRect, TabLabel(i), Color.white, "Tab Toggle Title", t.FontPx, QText, face: TmpFont.Face.LatinCardBody);
                // 🔴 **2026-10-11（A305①）**：第 5 个实参 = 原版 `Tab Toggle Title` 的 `m_fontSizeBase` **原文**。
                //    **六颗页签全同 = `35.0`**（原版实读：`/Player Profile Window/Menu Area/Tab Buttons/*/Label/Tab Toggle Title`
                //    `m_fontSize 35/35/33.65/35/32.75/35` · `auto[10~35]` · **base 六颗都是 35.0** —— 逐站表 §二·3 #21），
                //    ⇒ 这一格**可以写死 35**（不是「按每颗算」）。
                //    ⚠️ 与标称的关系：`m_fontSize` 有两颗是编辑器里缩过的（33.65 / 32.75）而 base 仍是 35 ⇒
                //      「base == 标称」在这两颗上**不成立**，这就是它必须显式传的原因。
                if (lbl != null && t.AutoMax > t.AutoMin)
                    lbl.SetAutoFitBox(LayoutSpace.Px(labRect.W), LayoutSpace.Px(labRect.H), t.AutoMin, t.AutoMax, 35f);
                // 🔴 **2026-10-08（A212）**：上面那句 `SetAutoFitBox` 内部会 `SetWrapWidth` ⇒ **无条件开折行**，
                //    而原版这六颗是 `m_TextWrappingMode = 0`（**不折行**，见上面那行 TMP 原文）——
                //    判据（现读）= `python 工具/menu_dump.py bundle_menus_assets_all "Player Profile Window"
                //    --depth 25 --md` 的 `折行=` 列：`Tab Buttons/*/Label/Tab Toggle Title` **6/6 全 `0`**
                //    （`Profile`/`Avatar`/`Title`/`Battle Log`/`Achievements`/`Ranking`）。
                //    ⚠️ 这一句**必须**在 `SetAutoFitBox` 之后（A205：`textWrappingMode` 的 setter 只标脏、
                //    **批处理没有帧循环** ⇒ 不显式重排的话「字段说了 0、画面还是折行的」）。
                if (lbl != null) lbl.SetWrapping(false);

                // 点击区：整键（原版是 `EverguildToggle`，我们只用它的「点一下切页」语义）
                int captured = i;
                // 🆕 A17：原版 `Tab Buttons>Menu Area>Player Profile Window` 是 Toggle，悬停换 `…_selected`
                //（`onSprite` 的 `_hover` 是选中态，见普查 §一）
                // 🔴🔴 **2026-10-18（W18）命中区补满 10px 间隙**：六个键是 `VerticalLayoutGroup`
                //   `spacing 10` 摆出来的（`KeyStep 119.252 − KeyH 109.252 = 10`），原来每键的 `Hit`
                //   **只盖自己的键框** ⇒ 那 10px 空档落到窗根的 `BackgroundHit`（= **关窗**）。
                //   实测（`_tmp_view/click_log.txt` 第五轮）：画布 `(189,297)` 与 `(201,768.8)` 两点正好落在
                //   键 0/1 与键 4/5 的空档里 ⇒ 日志是 `BackgroundHit … Player Profile Window` + 窗被关掉；
                //   而同一次点在键内的 `(201,300.8)` 正常切到 Avatar 页。
                //   ⇒ 每键的命中区**上下各吃半个间隙**（首键上沿 = 条顶、末键下沿 = 条底，正好铺满整条，
                //   相邻两键的命中区**相接不重叠**）。⛔ 别去改键框本身（那是布局真值 165×109.252）。
                const float KeyGapHalf = (KeyStep - KeyH) * 0.5f;
                float hitTop = top - (i == 0 ? 0f : KeyGapHalf);
                float hitBot = bot + (i == Tabs.Length - 1 ? 0f : KeyGapHalf);
                var tabHit = MenuDraw.Hit(b, "Hit", new PxRect(KeyL, hitTop, KeyR, hitBot), QHit,
                             () => { if (tabButtons != null) tabButtons.Click(captured); }, _btnBg[i], ArtTabOff,
                             clickCue: WindowSounds.SelectToggleCue);
                if (tabHit != null) _btnWb[i] = tabHit.GetComponent<WindowButton>();
                // 🆕 **2026-10-18（音效线缺口 `AUD` §三 C9）**：原版这六颗页签是 `EverguildToggle` +
                //   **`allowClickWhenAlreadySelected = 0`** ⇒ 再点当前页时连按钮音一起跳过。
                if (_btnWb[i] != null)
                {
                    _btnWb[i].allowClickWhenAlreadySelected = false;
                    _btnWb[i].isOn = () => tabButtons != null && tabButtons.CurrentVisualIndex == captured;
                }

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
            // 🔴🔴 **2026-10-18（A1149 第一半）圆底盘【挂点】归真**（本件 = 第九会话 P6）：
            //   改前圆底盘画在一颗**自造子件 `Image`** 上、而 `Generic Close Button Orange` 是颗**裸节点**；
            //   原版那颗 `Image` **就长在根节点自己身上**（无独立子件名）。
            //   判据（逐字段直读）=
            //   `python -I d:/tmp/wf_b4probe/pa.py bundle_menus_assets_all "Player Profile Window" 8`：
            //   根 `Generic Close Button Orange`（在 `Menu Area/Tab  Area/` 下）自己带 `Image`：
            //   `UI_Button_Round_background` · `m_Type=0`(Simple) · **`m_PreserveAspect=1`** ·
            //   `m_PixelsPerUnitMultiplier=1.0` · `m_RaycastTarget=0` · 矩形 **74.39×75.61**；
            //   贴图 `m_Rect` = **237×237 正方** ⇒ 实绘 **74.39×74.39**。
            //   ⇒ 照兄弟窗先例 `Shell/BaseOfferPopup.cs` / `Shell/ReferralPopupWindow.cs` 的形状
            //   （`Rect` 画在根节点上、图取不到才退回 `Node`）。矩形与 `keepAspect` **改前就是对的**（本处只改挂点）。
            // 🆕 **2026-10-18（A1058 · 第六会话批 2）**：换图那一层 = **子件 `Background`**（`closeFaceQ`，
            //   画的是 `ArtCloseBg` = `40k_general_bt_yellow`），⛔ **不是圆底盘 `closeBaseQ`**。
            //   判据（原版 prefab 亲读）=
            //   `python -I d:/tmp/wf_hit/rcunion.py bundle_menus_assets_all "Player Profile Window" --depth 8`：
            //   根 `Menu Area/Tab  Area/Generic Close Button Orange` 那颗 `EverguildButton` 的
            //   **`m_TargetGraphic` = pid-981292653871465934**；解该 pid ⇒ **所属 GO 名 = `Background`**、
            //   贴图 pid `5693181797853584851` → `40k_general_bt_yellow`（`d:/4/_tmp_view/sprite_pids_ALL.json`）。
            //   ⚠️ **2026-10-18 更正（铁律 5）**：原注释写「`trans=2` 换的是它自己的 Image = `UI_Button_Round_background`」
            //   —— **那句是错的**（错因 = 只读 `m_Transition`、没读 `m_TargetGraphic`）。
            var closeBaseQ = MenuDraw.Rect(tabArea, ArtInternal(ArtClose), new PxRect(CloseL, CloseT, CloseR, CloseB),
                                           "Generic Close Button Orange", QChrome, null, true);
            var c = closeBaseQ != null ? closeBaseQ.transform
                                       : MenuDraw.Node(tabArea, "Generic Close Button Orange",
                                                       new PxRect(CloseL, CloseT, CloseR, CloseB));
            var closeFaceQ = MenuDraw.Rect(c, ArtInternal(ArtCloseBg), new PxRect(CloseInL, CloseInT, CloseInR, CloseInB),
                                           "Background", QChrome + 1, null, true);
            MenuDraw.Rect(c, ArtInternal(ArtCloseIcon), new PxRect(CloseInL, CloseInT, CloseInR, CloseInB),
                          "Icon", QChrome + 2, null, true);
            // 🆕 A17：原版 `… > Player Profile Window` 的 `Generic Close Button Orange` 是 SpriteSwap
            //（HL = `40k_general_bt_yellow_hover`）
            // 🆕 **2026-10-18（A1053 · 第六会话批 2）**：**命中区**归真值 —— 子件（56.86×58.13）按
            //   自己的 `m_RaycastPadding (-20)⁴` 外扩 = **96.86 × 98.13**；改前传根矩形
            //   `CloseL..CloseB`（74.39×75.60）⇒ 每边小 11.2。
            //   判据 = `python -I d:/tmp/wf_hit/rcpad.py bundle_menus_assets_all "Player Profile Window"
            //   --depth 8 --substr "Tab  Area/Generic Close Button"`（实读 `96.86 x 98.13`）。
            MenuDraw.Hit(c, "Hit", MenuDraw.PaddedRect(new PxRect(CloseInL, CloseInT, CloseInR, CloseInB), ClosePad),
                         QHit, () => Close(), closeFaceQ, ArtCloseBg, "40k_general_bt_yellow_hover",
                         clickCue: WindowSounds.DefaultCloseCue);
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
                // 🔴 **2026-10-24（`U2` 现核 · 铁律 5）**：原版第 6 页的**页根名是 `Ranking Tab`**
                //   （资产文件名 `GameObject/**Ranking Tab.json**`、`menu_dump` 同；类名才是 `RankedTab`），
                //   而「键名去 ` Button` 再拼 ` Tab`」会得到 **`Ranked Tab`** ⇒ **只有这一颗要显式映射**。
                string pageName = Tabs[i].Node == "Ranked" ? "Ranking Tab"
                                : Tabs[i].Node.Replace(" Button", "") + " Tab";
                var page = MenuDraw.Node(_content, pageName, r);
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
                var t = ArtInternal(on ? ArtTabOn : ArtTabOff);
                _btnBg[i].SetTexture(t);
                // 🔴 `SetTexture` 会把 `_aspect` 冲成贴图自己的比值 ⇒ 拉回「按原版矩形定的」那个
                //（同族先例 `BattleLogPanel:532` · `AlliancesTab:189`）
                _btnBg[i].SetAspect((KeyR - KeyL) / KeyH);
                // A17：选中态是**这里**换的底图 ⇒ 同步给按钮记的「常态图」（否则悬停退出会还原成未选中的图）
                if (_btnWb[i] != null) _btnWb[i].SetNormalTex(t);
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

}
