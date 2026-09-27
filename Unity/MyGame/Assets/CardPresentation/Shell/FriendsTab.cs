// FriendsTab.cs — 社交窗第 2 页：**好友**（原版 `FriendsTab`，`WindowTabBase<SocialMenuWindow>`）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/社交_联盟与好友页.md` —— §A·1 的 **380–397 行**（主树里这一页那 18 个节点）·
// §A·2·4 `Friend Info Item` 独立根（好友行真 prefab，RT `9146413356635407152`）· §B·9/§B·18 · §C。
//
// ---- 🔴 这一页要照做的四条（读原始 JSON 定的）----
// ① **出厂 act = F**（`Friends Tab` 的 `m_IsActive = 0`，§A·1 第 380 行）—— 默认停在 Alliances 页，
//    点左栏第 2 键才切过来。`SocialWindow.BuildTabContents` 里 Setup 时它还是 active 的（TMP 量得到尺寸），
//    随后 `Click(0)` 才把它关掉。
// ② **`Header` 比窗框宽**：`332.17,70.94→**2085.00**,300.54`（§A·1 第 381 行）—— 原版就这么摆
//    （它是个容器，真正画出来的东西都在 1920 以内）⇒ **别「对齐」掉**（同 `BattleLogTab` 的 `Matches` 那条）。
// ③ **好友行 100% 是运行期生成的**：`Friends Container>Viewport>Content` 出厂 **0 子节点**（§B·18 末条），
//    行由 `FriendsTab.friendElementPrefab` → `Instantiate` 出来 ⇒ 我们同形：**按数据条数逐行建**，0 条就什么都不建。
// ④ **这一页的搜索框只看得见、打不了字**（我们这套外壳没有文字输入）⇒ 点它**出声**，不静默。
//
// ---- 数据（用户 2026-09-26 口径：「具体的数据和排名这些可以空着」）----
// 好友表在服务器 ⇒ 本地无源 ⇒ **恒空**。行模板照建（`BuildFriendRow`），有数据那天直接长出来。
// 空间三颗钮（加好友 / 立即决斗 / 行内挑战·删友·看档案）**全部要服务器** ⇒ 一律出声。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `FriendsTab`。</summary>
    public class FriendsTab : SocialPage
    {
        public override WindowTabType Type { get { return WindowTabType.SocialFriends; } }
        protected override int PageIndex { get { return 1; } }

        // ============================================================ 队列档（页内分层）
        const int L_Panel = 0;   // 面板/行底板
        const int L_Btn = 1;     // 钮底
        const int L_Art = 2;     // 钮上的图标 / 状态点
        const int L_Text = 3;    // 主要文字
        const int L_Line = 4;    // 分隔线（压在字之下无所谓，单列一号便于自检）
        const int L_Hit = 7;     // 命中区

        // ============================================================ 真值（绝对画布像素 · §A·1 380–397 行）
        // 页根：`167.17,70.94→1920.00,1080.00`（= `Tabs` 矩形，由 `SocialWindow.BuildTabContents` 给）。
        /// <summary>`Header`：`332.17,70.94→2085.00,300.54` —— ⚠️ **右边界超出窗框**（判据 ②）。</summary>
        static readonly PxRect HeaderR = new PxRect(332.17f, 70.94f, 2085.00f, 300.54f);
        /// <summary>`Find players panel`（空容器，只有位置）：`357.37,147.14→1100.86,256.53`。</summary>
        static readonly PxRect FindPanelR = new PxRect(357.37f, 147.14f, 1100.86f, 256.53f);
        /// <summary>`Search Field`（`EverguildInputField` · 底图 `InputFieldBackground` · Sliced 九宫 (10,10,10,10)
        /// · 染色 **(0.0627,0,0,1)**）。占位文本见 `PlaceholderText`。</summary>
        static readonly PxRect SearchFieldR = new PxRect(398.77f, 173.35f, 925.42f, 230.31f);
        static readonly Vector4 SearchBorder = new Vector4(10f, 10f, 10f, 10f);
        static readonly Color SearchTint = new Color(0.0627f, 0f, 0f, 1f);
        /// <summary>`Placeholder`：`Enter player name` · 40px · `auto[18,40]` · Left/Midline · 色 (1,1,1,**0.58**)。
        /// ⚠️ 它和 `Text` 都铺在 `Text Area`（`RectMask2D`）里 —— 我们**没有掩码体系**（整套都没有），
        /// 这里只画占位、不裁（如实记着，同档案窗 `ChooseNameWindow` 那条）。</summary>
        static readonly PxRect PlaceholderR = new PxRect(403.34f, 173.36f, 885.42f, 230.31f);
        const string PlaceholderText = "Enter player name";
        const float PlaceholderPx = 40f, PlaceholderAutoMin = 18f;
        static readonly Color PlaceholderCol = new Color(1f, 1f, 1f, 0.58f);

        /// <summary>`Add Friend Button`（80.05×68.41 · 底 `UI_Button_Organe_Square_Normal` 九宫 (75,51,63,57)）
        /// —— ⚠️ **它没有 `Button Text`**（纯图标钮，§B·9）。</summary>
        static readonly PxRect AddFriendR = new PxRect(953.12f, 167.63f, 1033.12f, 236.04f);
        static readonly Vector4 SquareBtnBorder = new Vector4(75f, 51f, 63f, 57f);
        /// <summary>`Add Friend Button/Icon`（`40K_bt_addFriend` 103×76 · preserveAspect）。</summary>
        static readonly PxRect AddFriendIconR = new PxRect(966.46f, 169.37f, 1019.78f, 234.30f);
        /// <summary>`Instant duel Button`（同底图同尺寸）。</summary>
        static readonly PxRect InstantDuelR = new PxRect(1045.70f, 167.63f, 1125.70f, 236.04f);
        /// <summary>`Instant duel Button/Icon`（`40K_bt_challenge1` 88×89 · preserveAspect）。</summary>
        static readonly PxRect InstantDuelIconR = new PxRect(1062.58f, 170.27f, 1108.81f, 233.40f);
        /// <summary>`Search Player`（**在面板上方**：`382.36,125.65→1077.85,178.99`）· 38.27px · Left/Middle · 折行。</summary>
        static readonly PxRect SearchPlayerR = new PxRect(382.36f, 125.65f, 1077.85f, 178.99f);
        const float HeadPx = 38.27f;

        /// <summary>`Friends List` 容器：`332.15,254.26→1920.00,1080.06`。</summary>
        static readonly PxRect ListR = new PxRect(332.15f, 254.26f, 1920.00f, 1080.06f);
        /// <summary>`Friends Title`（`Your friends:`）。</summary>
        static readonly PxRect TitleR = new PxRect(383.41f, 261.33f, 1078.90f, 314.80f);
        /// <summary>`Divisor line`（`40k_Separator Fade Sides Horizontal` 128×4 · 九宫 (63,0,63,0) ·
        /// 染色 (0.875,0.552,0.286,1)；原版还有 `ppuMul=1.64` —— 我们的画图不做 ppu 缩放，照矩形摆）。</summary>
        static readonly PxRect DivisorR = new PxRect(343.14f, 311.12f, 1898.89f, 314.80f);
        static readonly Vector4 DivisorBorder = new Vector4(63f, 0f, 63f, 0f);
        static readonly Color DivisorTint = new Color(0.875f, 0.552f, 0.286f, 1f);
        /// <summary>`Friends Container`（`ScrollRect` h=0 v=1 mode=1 Elastic inertia=1 elasticity=0.1
        /// decel=0.135）—— 我们拿它的矩形当滚动区，手感走 `MenuScroll`（同其它页）。</summary>
        static readonly PxRect ContainerR = new PxRect(332.15f, 314.80f, 1875.80f, 1080.06f);

        /// <summary>🔴 **好友行的尺寸 = 网格 cell**（`Friends Container>Viewport>Content` 的
        /// `GridLayoutGroup`：cellSize **721.3×84.82** · spacing (11.2,12.7) · pad (16,0,20,0) · align 0 UpperLeft）。
        /// ⚠️ **这一格是本页唯一「表里两个数打架」的地方**：独立根 `Friend Info Item` 自己序列化的
        /// `sizeDelta` 是 **670.16×58.46**，而网格 cell 是 721.3×84.82。我们取 **cell**，三条理由：
        /// ① 树里那份真实例（`Alliance Member Entry`，另一个网格）的 `sizeDelta` = **750×100 = 它那个 cell**；
        /// ② 行底板是 `a=(0,0)-(1,1)` 全拉伸 —— 取 670.16 会在列表里**露出缝**；
        /// ③ 作者把 cell 设得比行内容大，正是为了「行填满格子」。
        /// 📌 行 builder 收**矩形参数**（不写死尺寸）⇒ 真被证伪时只改这一个常量。</summary>
        static readonly Vector2 CellSize = new Vector2(721.3f, 84.82f);
        static readonly Vector2 CellGap = new Vector2(11.2f, 12.7f);
        const float CellPadL = 16f, CellPadT = 20f;

        // ⚠️ **没有 `_scroll`**：好友表恒空（服务器源）⇒ 没有可滚的内容。有数据那天照 `BattleLogTab`
        //    那条路接 `MenuScroll`（`ScrollRect` 的真值本页已经记在 `ContainerR` 的注释里了）。
        Transform _content;
        public int BuiltRows { get; private set; }

        // ============================================================ 建

        public void SetHost(SocialWindow win) { Win = win; }

        public override void Setup()
        {
            var header = Node(Root, "Header", HeaderR);
            var panel = Node(header, "Find players panel", FindPanelR);

            // `Search Field`（九宫格底）+ 占位文字
            var sf = Node(panel, "Search Field", SearchFieldR);
            Nine(sf, "InputFieldBackground", SearchFieldR, SearchBorder, "Background", L_Panel, SearchTint);
            Text(sf, PlaceholderR, PlaceholderText, PlaceholderCol, "Placeholder",
                 PlaceholderPx, L_Text, PlaceholderAutoMin);
            // ⚠️ `Text Area`（`RectMask2D`）与 `Text`（内容是 U+200B 零宽空格 = 空输入）**都不画**：
            //    整棵原版树里 `Text` 静态就是零宽字符、`Text Area` 只挂了个掩码 ⇒ 画出来是空的。
            //    我们**不建假节点**（纪律③的同一精神），只留这条注释。
            Hit(sf, "SearchFieldHit", SearchFieldR, L_Hit,
                () => Say("`Search Field` **输入框打不了字** —— 我们这套外壳没有文字输入系统"
                        + "（原版 `EverguildInputField` 走 uGUI 输入）。**没有静默**，点了就报这一句。"));

            var add = Node(panel, "Add Friend Button", AddFriendR);
            Nine(add, "UI_Button_Organe_Square_Normal", AddFriendR, SquareBtnBorder, "Image", L_Btn);
            Rect(add, "40K_bt_addFriend", AddFriendIconR, "Icon", L_Art, null, true);
            Hit(add, "Hit", AddFriendR, L_Hit,
                () => Say("`Add Friend Button`：加好友要**服务器**（原版发一条好友请求）—— 本地没有那个源。"));
            _addFriendHit = add.Find("Hit");

            var duel = Node(panel, "Instant duel Button", InstantDuelR);
            Nine(duel, "UI_Button_Organe_Square_Normal", InstantDuelR, SquareBtnBorder, "Image", L_Btn);
            Rect(duel, "40K_bt_challenge1", InstantDuelIconR, "Icon", L_Art, null, true);
            Hit(duel, "Hit", InstantDuelR, L_Hit,
                () => Say("`Instant duel Button`：立即决斗要**先有好友**（原版拿选中的好友去开局）—— "
                        + "本地好友表恒空 ⇒ 现在没得选。"));
            _instantDuelHit = duel.Find("Hit");

            Text(panel, SearchPlayerR, "Search player", Color.white, "Search Player", HeadPx, L_Text, 0f);

            var list = Node(Root, "Friends List", ListR);
            Text(list, TitleR, "Your friends:", Color.white, "Friends Title", HeadPx, L_Text, 0f);
            Nine(list, "40k_Separator_Fade_Sides_Horizontal", DivisorR, DivisorBorder, "Divisor line", L_Line, DivisorTint);

            var container = Node(list, "Friends Container", ContainerR);
            var vp = Node(container, "Viewport", ContainerR);
            _content = Node(vp, "Content", new PxRect(ContainerR.x1, ContainerR.y1, ContainerR.x2, ContainerR.y1 + 7.3f));
            BuildRows();
        }

        /// <summary>两处「要服务器」的命中区（自检要读它们证明**点了会出声**）。</summary>
        Transform _addFriendHit, _instantDuelHit;
        public Transform AddFriendHit { get { return _addFriendHit; } }
        public Transform InstantDuelHit { get { return _instantDuelHit; } }

        public override void OnOpen()
        {
            // 原版 `FriendsTab.OnOpen` 会拿好友列表重填容器（服务器）⇒ 我们每次进来重算一遍（本地恒 0 条）
            BuildRows();
        }

        /// <summary>自检用：喂了数据之后重画（= `OnOpen` 那条路）。**只给自检**。</summary>
        public void RebuildForTest() { BuildRows(); }

        /// <summary>按数据条数逐行建（原版 `Instantiate(friendElementPrefab, friendsContainer)`）。
        /// 行位按 `GridLayoutGroup` 的规矩自己推（`menu_dump` 的布局算法只做 V/H、不做 grid，§B·12）。</summary>
        void BuildRows()
        {
            if (_content == null) return;
            for (int i = _content.childCount - 1; i >= 0; i--) SocialWindow.DestroySafe(_content.GetChild(i).gameObject);
            BuiltRows = 0;

            var all = SocialData.Friends;
            int n = all.Count;

            // 🔴 **先把 `Content` 摆到位、再建行** —— 行是按「绝对画布坐标」算 `localPosition` 的
            //    （`MenuDraw.Local(parent, …)` = 绝对中心 − **父节点的世界位置**）⇒ 建完行再挪父节点，
            //    整排会跟着父节点一起偏（2026-09-27 实测：偏 48.76px，而**每一行的相对关系还是对的**，
            //    所以只看「行距/行高」的断言一条都抓不到）。
            float h = n == 0 ? 7.3f : CellPadT + n * CellSize.y + (n - 1) * CellGap.y;
            _content.localPosition = MenuDraw.Local(_content.parent, ContainerR.x1, ContainerR.y1,
                                                    ContainerR.x2, ContainerR.y1 + h);

            int cols = Mathf.Max(1, Mathf.FloorToInt((ContainerR.W - CellPadL * 2f + CellGap.x) / (CellSize.x + CellGap.x)));
            for (int i = 0; i < n; i++)
            {
                int col = i % cols, row = i / cols;
                float x1 = ContainerR.x1 + CellPadL + col * (CellSize.x + CellGap.x);
                float y1 = ContainerR.y1 + CellPadT + row * (CellSize.y + CellGap.y);
                BuildFriendRow(all[i], new PxRect(x1, y1, x1 + CellSize.x, y1 + CellSize.y));
                BuiltRows++;
            }
        }

        /// <summary>好友行 = 原版 `Friend Info Item` 那棵树（§A·2·4，9 个节点）。
        /// 行内是**半透明底板 + 状态点 + 名字 + 三颗图标钮**；三颗钮全要服务器 ⇒ 一律出声。</summary>
        void BuildFriendRow(SocialData.Friend f, PxRect r)
        {
            var row = Node(_content, "Friend Info Item", r);
            // `background`：`a=(0,0)-(1,1)` 全拉伸 · `40K_dropdown_bg` 九宫 (23,20,23,20) · 染色 (0.981,0.469,0.356,1)
            var bg = Node(row, "background", r);
            Nine(bg, "40K_dropdown_bg", r, new Vector4(23f, 20f, 23f, 20f), "Bg", L_Panel,
                 new Color(0.981f, 0.469f, 0.356f, 1f));

            // `Connection Status`（25.9 从左边起 · 25.2²）—— 里面两个状态点**只有一个亮**：
            // 在线 `Connected Image`（原版 act F ⇒ 我们按「在线才建」）、离线 `Disconnected`（act T）
            var st = Node(row, "Connection Status", new PxRect(r.x1 + 13.30f, r.y1 + 16.63f, r.x1 + 38.50f, r.y1 + 41.83f));
            bool online = f != null && f.Online;
            if (online)
                Rect(st, "40K_icon_status_online", new PxRect(r.x1 + 13.30f, r.y1 + 16.63f, r.x1 + 38.50f, r.y1 + 41.83f),
                     "Connected Image", L_Art, new Color(0f, 1f, 0.0736f, 1f), true);
            else
                Rect(st, "40K_icon_status_offline", new PxRect(r.x1 + 13.30f, r.y1 + 16.63f, r.x1 + 38.50f, r.y1 + 41.83f),
                     "Disconnected", L_Art, new Color(0.84f, 0.494f, 0.44f, 1f), true);

            // `Friend name`：`a=(0.072,0)-(1,1) p=(0,.5) pos=(0.5,0) sz=(−264.559,−4.96)` ⇒ 从 x=0.072W 拉到右边 −264.56
            float nl = r.x1 + r.W * 0.072f + 0.5f, nr = r.x2 - 264.559f;
            Text(row, new PxRect(nl, r.y1 + 2.48f, nr, r.y2 - 2.48f), f != null ? f.Name : "",
                 Color.white, "Friend name", 45f, L_Text, 18f);

            // 三颗右对齐的图标钮（`a=(1,.5)`，从右往左 −224.771 / −133.8 / −42.829，各 68.644×69.315）
            Btn(row, r, -224.771f, "Show Profile Button", "40K_bt_View_Friend",
                () => Say("`Show Profile Button`：原版开**他的**玩家档案窗 —— 本地没有好友数据 ⇒ 开不了别人的档案。"));
            Btn(row, r, -133.8f, "Challenge button", "40K_bt_challenge2",
                () => Say("`Challenge button`：原版开**好友挑战弹窗**（`MessagePopupWindowDuel`）—— "
                        + "入口本身已经建好了，但**得有对手**才有意义（本地好友表恒空）。"));
            Btn(row, r, -42.829f, "Delete friend button", "40K_bt_deleteFriend",
                () => Say("`Delete friend button`：删好友要**服务器**（原版 `RemoveFriend`）。"));
        }

        /// <summary>好友行里的一颗图标钮（原版三颗几何完全一样，只有 x 与图不同）：
        /// `a=(1,.5) p=(.5,.5) pos=(dx,0)` ⇒ **从行的右边沿往里数** `|dx|`，竖直居中。</summary>
        void Btn(Transform row, PxRect r, float dxFromRight, string name, string art, System.Action onClick)
        {
            float cx = r.x2 + dxFromRight, cy = (r.y1 + r.y2) * 0.5f;
            var br = new PxRect(cx - 34.322f, cy - 34.6575f, cx + 34.322f, cy + 34.6575f);
            var n = Node(row, name, br);
            Rect(n, art, br, "Image", L_Art, new Color(0.906f, 0.906f, 0.906f, 1f), true);
            Hit(n, "Hit", br, L_Hit, onClick);
        }
    }
}
