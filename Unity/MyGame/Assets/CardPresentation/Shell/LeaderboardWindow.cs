// LeaderboardWindow.cs — 多人界面那一批 第 3 件：**四个排行榜**（`RankedRankingWindow` 那一族）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/排行榜_遭遇战.md` · `排行榜_经典.md` · `排行榜_轮抽.md` ·
// `排行榜_嵌入版与行族.md`（**行**那一段在 `Shell/LeaderboardRow.cs`）· `排行榜_入口与调用.md`（入口）。
// 骨架真值 → `资料/阶段二_多人界面_原版规格.md` §三（⚠️ 那一节原来只有**模板位**，本轮换成指针 + 真值摘要）。
//
// ---- 🔴 七条判据（读原始 JSON / 反编译定的）----
// ① **四个根**：`RankedSkirmishLeaderboardPopup`（遭遇，**3** 个页签）· `RankedClassicLeaderboardPopup Variant`
//    （经典，**2** 个）· `DraftLeaderboardPopup`（轮抽，**2** 个）· `Ranked Leaderboard Display`（**嵌入版**：
//    无页签 / 无关闭键 / 无暗底 / 无 `Army Selector`）。前三扇的根组件都是 **`RankedRankingWindow`**。
// ② 🔴 **页签互斥是【代码】接的，不是 `ToggleGroup`** —— 三个 toggle 的 `m_Group` 序列化全是空，
//    是 `RankedRankingWindow.Start` 逐个挂 `onValueChanged`；`ToggleTab` 里再手工把别的 `set_isOn(false)`。
//    而 `EverguildToggle.Awake` **不调** `Toggle.Awake` ⇒ **别照搬 uGUI 常识**。我们的页签自己管单选。
// ③ 🔴 **页签数按 `tabDefinitions` 算，不按树上的钮数** —— 轮抽树上有 `Armies`/`Alliances` 两个钮，
//    但 `tabDefinitions` **只登记了 `Alliances`**（`Armies` 那个在**原版里点不动**）⇒ 我们照原版：
//    钮建出来、点了**如实出声**、不切页（红线：不许静默失败）。
// ④ 🔴 **`Timer` 不建**（赛季倒计时：服务器数据 + 用户 2026-09-26 明确不要赛季倒计时）。
//    `Generic Simplified UI Button_updated`（'Last season'）与 `Last Season Text` **建**，
//    但**照原版的运行期规则关掉**：`UIRankingListController.Initialize` 末尾传 `showCurrent = true`
//    ⇒ `lastSeasonText.SetActive(!showCurrent)` = **关**；`AllowChangeSeason(有上一赛季榜?)`
//    ⇒ 本地**没有**上一赛季榜 ⇒ **按钮也关**（判据 → 正本 §A·1 的 `Initialize`/`ChangeSeasonRankingView`/`AllowChangeSeason`）。
// ⑤ **四棵榜都没有空态节点**（正本 §3·1 第 3 条）⇒ 本地没数据时**照原版留空**，
//    **不自己造一个空态**（要造就得先查 `战斗UI_原版对账表.md` §六「我们加的」）。
// ⑥ **入口**（`排行榜_入口与调用.md`）：`RankedEventWindowV2` 上**两颗** —— `rankingPrefab`（遭遇）与
//    `rankingPrefabClassic`（经典），按 `playMode == Classic(0)` 二选一；轮抽那扇由
//    `AlliancesEventScorePanel.LeaderboardButtonClick` / `DraftExpiringContent.OpenLeaderboard` 开。
// ⑦ 🔴 **嵌入版 `Ranked Leaderboard Display` 在原版里【零引用】**（84 个 bundle 解压后逐 SerializedFile 搜 guid，
//    只命中它自己的 `AssetBundle.m_Container`）⇒ **它没有入口**。任务文件把它列进「四个排行榜」⇒ 我们**建**它，
//    但**不编入口**（自检直接开它验）—— 同 `BattleLogPopup` 那条先例。
//
// ⚠️ **哪些是我们挑的（不是原版）**：嵌入版的 `Scroll View` 序列化高是 **0**（它没有 `LayoutElement`，
//    另三扇都有 `preferredHeight = 790`）⇒ 真实高取不到，我们**按父 `Content` 撑满**（见 `EmbListR`）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>四个排行榜。**枚举值是我们的**（原版没有「第几个榜」这种枚举，只有四个独立的 prefab）。</summary>
    public enum LeaderboardKind { Skirmish = 0, Classic = 1, Draft = 2, Embedded = 3 }

    /// <summary>页签。原版 `RankedRankingWindow.LeaderboardTab` 是**每格一个 provider** 的数据结构，
    /// 不是枚举；我们这三个名字取自三棵树上的钮名（`Player` / `Armies` / `Alliances`）。</summary>
    public enum LeaderboardTab { Player = 0, Armies = 1, Alliances = 2 }

    /// <summary>`RankedRankingWindow` 那一族 —— 排行榜。四扇共用一个类，差异只有页签表与行族。</summary>
    public class LeaderboardWindow : GameWindow
    {
        // 队列档：**弹窗那一档**（`BattleLogPopup` 用 3450；这一族再往上一段，互不重叠）
        public const int QBase = 3500;
        const int QPanel = QBase, QBg = QBase + 1, QContent = QBase + 3, QRow = QBase + 5,
                  QText = QBase + 15, QHit = QBase + 20;

        public static LeaderboardWindow LastOpened { get; private set; }

        public LeaderboardKind Kind { get; private set; }
        public LeaderboardTab CurrentTab { get; private set; }
        /// <summary>这一页实际建了几行（自检用）。</summary>
        public int BuiltRows { get; private set; }
        /// <summary>这一棵建了几个页签钮（自检用）。</summary>
        public int TabCount { get; private set; }
        /// <summary>`Last season` 那颗钮**现在可不可见**（照原版规则算的，自检用）。</summary>
        public bool SeasonButtonVisible { get; private set; }
        public readonly List<string> MissingArt = new List<string>();

        Transform _listContent;
        MenuScroll _scroll;
        readonly RowCtx _rowCtx = new RowCtx();

        // ============================================================ 真值（绝对画布像素）
        // ---- 三扇全屏弹窗共用那一套（三份普查逐位相同）----
        static readonly PxRect DarkBgR = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);
        static readonly Color DarkBgTint = new Color(0f, 0f, 0f, 0.773f);

        static readonly PxRect TabBarR = new PxRect(38.62f, 104.70f, 210.59f, 928.48f);
        const float TabL = 45.59f, TabR = 210.59f, TabTop = 104.70f, TabH = 157.684f;
        const float TabIconL = 45.59f, TabIconR = 205.59f, TabIconDy = 17.34f, TabIconH = 125.335f;
        /// <summary>`button_bg` 的色（正本 §A：`(1,0.427,0,1)`）。**选中/未选中是换图不换色**：
        /// `onSprite = 40K_settings_button_hover` · `offSprite = 40K_settings_button`。</summary>
        static readonly Color TabBgTint = new Color(1f, 0.427f, 0f, 1f);
        const string ArtTabOn = "40K_settings_button_hover", ArtTabOff = "40K_settings_button";

        static readonly PxRect PanelR = new PxRect(202.40f, 16.32f, 1717.60f, 1006.93f);
        static readonly Vector4 PanelBorder = new Vector4(42f, 363f, 655f, 81f);
        const string ArtPanel = "UI_Deck_Information_Back";
        static readonly PxRect TitleR = new PxRect(643.55f, 36.95f, 1276.45f, 146.52f);
        static readonly PxRect TopBarR = new PxRect(282.95f, 141.63f, 1637.05f, 147.63f);
        static readonly Vector4 LineBorder = new Vector4(80f, 0f, 80f, 0f);
        const string ArtLine = "40k_main_line";

        static readonly PxRect ContentR = new PxRect(248.99f, 147.64f, 1671.01f, 937.83f);
        static readonly PxRect ArmySelR = new PxRect(248.99f, 147.64f, 1671.01f, 258.59f);
        static readonly PxRect SepLineR = new PxRect(248.99f, 260.37f, 1671.01f, 266.37f);
        static readonly PxRect ScrollR = new PxRect(248.99f, 288.59f, 1671.01f, 937.83f);
        /// <summary>行容器（内层 `Content`）：**1200 宽**（360→1560，左右各留 111）。</summary>
        const float ListL = 360f, ListR = 1560f;

        static readonly PxRect CloseR = new PxRect(1656.81f, 9.19f, 1731.19f, 84.80f);
        static readonly PxRect CloseInnerR = new PxRect(1664.96f, 17.17f, 1721.82f, 75.30f);
        const string ArtCloseBg = "UI_Button_Round_background", ArtCloseCircle = "40k_general_bt_yellow",
                     ArtCloseIcon = "40k_general_bt_yellow_close";

        static readonly PxRect SeasonBtnR = new PxRect(264.74f, 62.21f, 509.74f, 121.26f);
        static readonly PxRect SeasonBtnTxR = new PxRect(276.44f, 68.00f, 497.25f, 115.48f);
        static readonly Vector4 SeasonBtnBorder = new Vector4(333f, 96f, 333f, 96f);
        const string ArtSeasonBtn = "UI_Button_Mulligan";
        static readonly PxRect SeasonTextR = new PxRect(1251.60f, 51.43f, 1660.40f, 132.04f);

        // ---- 嵌入版那一套（`Ranked Leaderboard Display`，根 rect 与三扇不同）----
        static readonly PxRect EmbPanelR = new PxRect(208.72f, 47.60f, 1727.48f, 1054.86f);
        static readonly PxRect EmbTitleR = new PxRect(643.55f, 73.06f, 1276.45f, 174.90f);
        static readonly PxRect EmbTopBarR = new PxRect(282.95f, 170.00f, 1637.05f, 176.00f);
        static readonly PxRect EmbBtnR = new PxRect(275.50f, 90.73f, 520.50f, 158.37f);
        static readonly PxRect EmbBtnTxR = new PxRect(287.20f, 97.35f, 508.01f, 151.74f);
        static readonly PxRect EmbSeasonTextR = new PxRect(1265.70f, 73.06f, 1674.50f, 176.03f);
        /// <summary>🔴 **推算值**（不是原版）：嵌入版的 `Scroll View` 序列化高是 **0**（没有 `LayoutElement`）
        /// ⇒ 我们按它的父 `Content`（`248.99,176.01→1671.01,1006.93`）撑满。见文件头「哪些是我们挑的」。</summary>
        static readonly PxRect EmbListR = new PxRect(248.99f, 176.01f, 1671.01f, 1006.93f);

        // ============================================================ 建

        /// <summary>开一扇。`kind` 选哪一棵（四个 prefab，一棵一个实例）。</summary>
        public static LeaderboardWindow Create(WindowsManager mgr, LeaderboardKind kind)
        {
            var go = new GameObject(NameOf(kind));
            var win = go.AddComponent<LeaderboardWindow>();
            win.Kind = kind;
            // 原版窗口字段（三扇全屏榜的根组件 `RankedRankingWindow` 是 `GameWindow` 子类）：
            // `type=1 Popup` · `windowsPlacement=15 Popup` · `closeOnESC=1` · `extraScaleSmallScreen=1.0`
            win.type = WindowType.Popup;
            win.placement = WindowsPlacement.Popup;
            win.closeOnEsc = true;
            win.extraScaleSmallScreen = 1f;
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public static string NameOf(LeaderboardKind k)
        {
            switch (k)
            {
                case LeaderboardKind.Skirmish: return "RankedSkirmishLeaderboardPopup";
                case LeaderboardKind.Classic: return "RankedClassicLeaderboardPopup Variant";
                case LeaderboardKind.Draft: return "DraftLeaderboardPopup";
                default: return "Ranked Leaderboard Display";
            }
        }

        /// <summary>一个页签钮（**照各自的树**；`Wired` = 原版 `tabDefinitions` 有没有登记它）。</summary>
        struct TabEntry
        {
            public LeaderboardTab Tab; public string Name; public string Art; public bool Wired;
            public TabEntry(LeaderboardTab t, string n, string a, bool wired = true)
            { Tab = t; Name = n; Art = a; Wired = wired; }
        }

        static TabEntry[] TabsOf(LeaderboardKind k)
        {
            switch (k)
            {
                case LeaderboardKind.Skirmish:
                    return new[]
                    {
                        new TabEntry(LeaderboardTab.Player, "Player", "40K_Chat_icon_Global"),
                        new TabEntry(LeaderboardTab.Armies, "Armies", "40K_Profile_icon_title"),
                        new TabEntry(LeaderboardTab.Alliances, "Alliances", "40K_Chat_icon_Alliance_v2"),
                    };
                case LeaderboardKind.Classic:
                    return new[]
                    {
                        new TabEntry(LeaderboardTab.Player, "Player", "40K_Chat_icon_Global"),
                        new TabEntry(LeaderboardTab.Armies, "Armies", "40K_Profile_icon_title"),
                    };
                case LeaderboardKind.Draft:
                    // 🔴 判据 ③：原版 `tabDefinitions` **只登记了 `Alliances`** ⇒ `Armies` 那格点不动
                    return new[]
                    {
                        new TabEntry(LeaderboardTab.Armies, "Armies", "40K_Profile_icon_title", false),
                        new TabEntry(LeaderboardTab.Alliances, "Alliances", "40K_Chat_icon_Alliance_v2"),
                    };
                default:
                    return new TabEntry[0];      // 嵌入版没有页签
            }
        }

        /// <summary>页签 → 行族（正本 §A·4：每格一个 provider，各自的 `rankingRowPrefab`）。
        /// 遭遇的 Alliances 用 `AllianceRankingRow Variant`、轮抽的用 `Skulls Variant`（判据 ①）。</summary>
        public static LeaderboardRowFamily FamilyOf(LeaderboardKind k, LeaderboardTab t)
        {
            if (t == LeaderboardTab.Alliances)
                return k == LeaderboardKind.Draft ? LeaderboardRowFamily.AllianceSkulls : LeaderboardRowFamily.Alliance;
            if (t == LeaderboardTab.Armies) return LeaderboardRowFamily.PlayerForArmy;
            return LeaderboardRowFamily.Player;
        }

        /// <summary>开窗时默认选哪一格：**原版 `Open()` 开的是 `tabDefinitions[0]`**（经典榜实证
        /// `ToggleTab(tabDefinitions[0], true)`）。轮抽的 `tabDefinitions` 只有 `Alliances` ⇒ 以它为准。</summary>
        public static LeaderboardTab DefaultTabOf(LeaderboardKind k)
        {
            if (k == LeaderboardKind.Draft) return LeaderboardTab.Alliances;
            return LeaderboardTab.Player;      // 嵌入版的 `PlayerRankingDataProvider.playMode = 0 (Classic)`
        }

        public override void Open()
        {
            LastOpened = this;
            CurrentTab = DefaultTabOf(Kind);
            Build();
        }

        /// <summary>自检用：喂了数据之后重画（`LeaderboardData.InjectForTest` → 这个 → 断言 → `ClearForTest`）。</summary>
        public void RebuildForTest() { Build(); }

        Texture2D Art(string n)
        {
            if (string.IsNullOrEmpty(n)) return null;
            var t = CardArt.MenuUi(n);
            if (t == null && !MissingArt.Contains(n)) MissingArt.Add(n);
            return t;
        }

        static Transform Node(Transform p, string n, PxRect r) { return MenuDraw.Node(p, n, r); }
        ImageQuad Rect(Transform p, string art, PxRect r, string n, int q, Color? tint = null, bool keepAspect = false)
        { return MenuDraw.Rect(p, art == null ? CardArt.Solid() : Art(art), r, n, q, tint, keepAspect); }
        GameObject Nine(Transform p, string art, PxRect r, Vector4 b, string n, int q)
        { var t = Art(art); return t == null ? null : MenuDraw.Nine(p, t, r, b, t.width, t.height, q, null, true, n); }

        /// <summary>切页签（**唯一入口** —— 页签钮与自检都走它）。</summary>
        public void SelectTab(LeaderboardTab tab)
        {
            if (tab == CurrentTab) return;
            CurrentTab = tab;
            Build();
        }

        /// <summary>点了「原版没接线」的那一格（轮抽的 `Armies`）—— **如实出声，不假装切换**。</summary>
        void OnDeadTab(LeaderboardTab tab)
        {
            Debug.LogWarning("[Leaderboard] `" + NameOf(Kind) + "` 的 `" + tab + "` 那一格**点了不切换** —— "
                             + "这不是漏做：原版 `tabDefinitions` **只登记了 `Alliances`**（树上那个钮没接线）"
                             + "⇒ 我们照原版（判据 → 资料/普查产出_0927/排行榜_入口与调用.md §1）");
        }

        void Build()
        {
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();
            BuiltRows = 0;
            TabCount = 0;

            if (Kind == LeaderboardKind.Embedded) BuildEmbedded();
            else BuildPopup();

            if (MissingArt.Count > 0)
                Debug.LogWarning("[Leaderboard] `" + NameOf(Kind) + "` ⚠️ 有 " + MissingArt.Count
                                 + " 张图取不到（**这些件没画**）：" + string.Join("、", MissingArt.ToArray()));
        }

        // ---------------------------------------------------------- 三扇全屏弹窗

        void BuildPopup()
        {
            var dark = Node(transform, "Menu Dark Background", DarkBgR);
            Rect(dark, null, DarkBgR, "Image", QPanel, DarkBgTint);
            MenuDraw.Hit(dark, "CloseHit", DarkBgR, QPanel, () => Close());

            BuildTabs();

            var panel = Node(transform, "Ranking Display", PanelR);
            Nine(panel, ArtPanel, PanelR, PanelBorder, "Generic Window Red Background Big", QBg);
            MenuDraw.Text(panel, TitleR, "TOP PLAYERS", Color.white, "Title", 55f, QText, TitleR.W, 18f);
            MenuDraw.Rect(panel, Art(ArtLine), TopBarR, "TopBar", QContent, null, false);

            // `Content`（VLG：`Army Selector` + `Scroll View` 两块）
            var content = Node(panel, "Content", ContentR);

            // `Army Selector`：出厂**只有壳**（`Army Content` 0 个子节点，运行期由 provider 填）
            var sel = Node(content, "Army Selector", ArmySelR);
            Node(sel, "Viewport", ArmySelR);
            Node(sel, "Army Content", new PxRect(ArmySelR.CX, ArmySelR.CY, ArmySelR.CX, ArmySelR.CY));
            MenuDraw.Rect(sel, Art(ArtLine), SepLineR, "Separator Line", QContent, null, false);
            Debug.Log("[Leaderboard] `Army Selector` 的 `Army Content` **出厂 0 个子节点**"
                      + "（原版运行期由 provider 按阵营填）⇒ 我们只建壳、不造条目"
                      + "（本地没有「每个阵营的榜」这种数据）。");

            var sv = Node(content, "Scroll View", ScrollR);
            var vp = Node(sv, "Viewport", ScrollR);      // 原版是 `UIMask`(a=0) + `RectMask2D` ⇒ 不画
            _scroll = MenuScroll.TopAligned(ScrollR, 0f);
            _scroll.Owner = gameObject;
            _scroll.OnChanged = () => RebuildRows();
            PointerLayer.RegisterScroll(_scroll);
            _listContent = Node(vp, "Content", new PxRect(ListL, ScrollR.y1, ListR, ScrollR.y1));
            RebuildRows();

            BuildSeasonPieces(false);
            BuildCloseButton();
        }

        // ---------------------------------------------------------- 嵌入版

        void BuildEmbedded()
        {
            // 🔴 它**不是一扇窗**（原版当嵌件用，而且全库零引用）—— 没有暗底、没有页签、没有关闭键。
            //    ⚠️ 它的根上**没有 `RankedRankingWindow`**（只有 `UIRankingListController` + 三个 provider）
            //    ⇒ 窗口字段（`type`/`placement`/…）**原版没有可抄** —— 我们按弹窗开它**只是为了能看/能验**，
            //    **这一条是我们挑的**（自检直接 `Create()`）。
            Debug.Log("[Leaderboard] `Ranked Leaderboard Display` 是**嵌入版**（原版零引用、没有任何窗口字段）——"
                      + "我们建它、但**不编入口**；`type/placement` 取弹窗值**是我们挑的**。");
            var content = Node(transform, "Content", EmbListR);
            Nine(transform, ArtPanel, EmbPanelR, PanelBorder, "Generic Window Red Background Big", QBg);
            MenuDraw.Text(transform, EmbTitleR, "TOP PLAYERS", Color.white, "Title", 55f, QText, EmbTitleR.W, 18f);
            MenuDraw.Rect(transform, Art(ArtLine), EmbTopBarR, "TopBar", QContent, null, false);
            BuildSeasonPieces(true);

            var sv = Node(content, "Scroll View", EmbListR);
            var vp = Node(sv, "Viewport", EmbListR);
            _scroll = MenuScroll.TopAligned(EmbListR, 0f);
            _scroll.Owner = gameObject;
            _scroll.OnChanged = () => RebuildRows();
            PointerLayer.RegisterScroll(_scroll);
            _listContent = Node(vp, "Content", new PxRect(ListL, EmbListR.y1, ListR, EmbListR.y1));
            RebuildRows();
        }

        // ---------------------------------------------------------- 页签 / 关闭键 / 赛季件

        void BuildTabs()
        {
            var bar = Node(transform, "Tab Buttons", TabBarR);
            var specs = TabsOf(Kind);
            for (int i = 0; i < specs.Length; i++)
            {
                float t = TabTop + TabH * i, b = t + TabH;
                var r = new PxRect(TabL, t, TabR, b);
                var node = Node(bar, specs[i].Name, r);

                bool on = specs[i].Tab == CurrentTab;
                // `button_bg`：**换图不换色**（`onSprite`/`offSprite`），色恒 `(1,0.427,0,1)`
                Rect(node, on ? ArtTabOn : ArtTabOff, r, "button_bg", QBg, TabBgTint);
                var iconR = new PxRect(TabIconL, t + TabIconDy, TabIconR, t + TabIconDy + TabIconH);
                Rect(node, specs[i].Art, iconR, "Icon", QContent, null, true);
                // `Label`（页签文字层）**出厂就 inactive、而且全子树没有一处引用它**（正本 §A·1）
                // ⇒ 照纪律**不建**。页签是**纯图标**的。

                var tab = specs[i].Tab;
                bool wired = specs[i].Wired;
                MenuDraw.Hit(node, "Hit", r, QHit,
                             () => { if (wired) SelectTab(tab); else OnDeadTab(tab); });
                TabCount++;
            }
        }

        void BuildCloseButton()
        {
            var close = Node(transform, "Generic Close Button Orange", CloseR);
            // 三件**都是 `preserveAspect`**（正本 §A·4·7）；`Image` 自己的底图 **m_Enabled 是开的**
            //（与 `BattleLogPopup` 那颗不同 —— 那边底图 m_Enabled=0、只画两个子件）。
            Rect(close, ArtCloseBg, CloseR, "Image", QBg, null, true);
            Rect(close, ArtCloseCircle, CloseInnerR, "Background", QContent, null, true);
            Rect(close, ArtCloseIcon, CloseInnerR, "Icon", QContent, null, true);
            MenuDraw.Hit(close, "Hit", CloseR, QHit, () => Close());
        }

        /// <summary>`Generic Simplified UI Button_updated`（'Last season'）+ `Last Season Text`。
        /// 🔴 **两件都建、但都关着**（判据 ④）。`Timer` **整块不建**。</summary>
        void BuildSeasonPieces(bool embedded)
        {
            // 照原版的运行期规则算出来（**不是我们挑的显隐**）：
            //   `AllowChangeSeason(!string.IsNullOrWhiteSpace(previousSeasonLeaderboardKey))`，本地没有上一赛季榜
            SeasonButtonVisible = false;

            var btnR = embedded ? EmbBtnR : SeasonBtnR;
            var txR = embedded ? EmbBtnTxR : SeasonBtnTxR;
            var textR = embedded ? EmbSeasonTextR : SeasonTextR;

            var btn = Node(transform, "Generic Simplified UI Button_updated", btnR);
            Nine(btn, ArtSeasonBtn, btnR, SeasonBtnBorder, "Image", QBg);
            MenuDraw.Text(btn, txR, "Last season", Color.white, "Button Text", 36f, QText, txR.W, 10f);
            MenuDraw.Hit(btn, "Hit", btnR, QHit, OnSeasonButton);
            btn.gameObject.SetActive(SeasonButtonVisible);

            var st = Node(transform, "Last Season Text", textR);
            var lb = MenuDraw.Text(st, textR, "Last season", Color.white, "Last Season Text", 40f, QText,
                                   textR.W, 18f);
            if (lb != null) MenuDraw.AlignRight(lb, textR);
            st.gameObject.SetActive(false);     // 原版 `ChangeSeasonRankingView(true)` 把它关掉
        }

        void OnSeasonButton()
        {
            Debug.Log("[Leaderboard] `Last season` 键 —— 原版切到**上一赛季**的榜（服务器数据）。"
                      + "本地没有赛季、也没有上一赛季榜 ⇒ 如实说明。");
            if (Manager != null)
                Manager.ShowPopUp("上一赛季的榜单在服务器上。\n本地版没有赛季数据，所以这里只能看看界面。",
                                  "知道了", null);
        }

        // ---------------------------------------------------------- 列表

        void RebuildRows()
        {
            if (_listContent == null) return;
            MenuDraw.ClearChildren(_listContent);
            BuiltRows = 0;

            var rows = LeaderboardData.Rows(Kind, CurrentTab);
            if (rows.Count == 0)
            {
                Debug.Log("[Leaderboard] `" + NameOf(Kind) + "` / `" + CurrentTab + "` —— 本地**没有榜单数据**"
                          + "（原版读服务器：`LeaderboardManager` / 各 `RankingDataProvider`）⇒ **照原版留空**"
                          + "（四棵榜都没有空态节点，我们**不自己造**）。");
                return;
            }

            float top = _scroll != null ? _scroll.Viewport.y1 : 0f;
            _rowCtx.Art = Art;
            _rowCtx.Q = QRow;
            _rowCtx.Clip = _scroll != null ? _scroll.Viewport : (PxRect?)null;
            var fam = FamilyOf(Kind, CurrentTab);
            for (int i = 0; i < rows.Count; i++)
            {
                float y = top + i * (LeaderboardRow.RowH + LeaderboardRow.RowGap);
                var rr = new PxRect(ListL, y, ListR, y + LeaderboardRow.RowH);
                if (_scroll != null) rr = _scroll.Shift(rr);
                LeaderboardRow.Build(_rowCtx, _listContent, rr, rows[i], fam);
                BuiltRows++;
            }
            _rowCtx.Clip = null;
        }
    }
}
