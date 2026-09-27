// AllianceMemberTab.cs — 社交窗「联盟」页的**第二支**：已在盟里那一态
//   （原版 `AllianceMemberTab`，挂在 `AllianceNotMemberVariant` 的**兄弟**节点 `AllianceMemberVariant` 下，
//     **出厂 act = F**）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/社交_联盟与好友页.md` §B·5（**两个 `GeneralDetails` 不是同一份**）· §B·11（二级页签）·
// §A·1 第 **255–379 行**（这一支那 120 个节点）· §A·2·3（成员行 `AllianceMemberEntry` 独立根）。
//
// ---- 🔴 三条判据 ----
// ① **这一支本地【走不到】**：原版 `AlliancesTab.ToFocus` 按 `AlliancesManager.CurrentGroupCached`（服务器）
//    在 `AllianceSearchTab`(0x30) 与 `AllianceMemberTab`(0x38) 之间二选一 —— 本地那个值恒空
//    ⇒ **永远停在未入盟支**（判据 → `多人界面_入口与调用.md` §③ 社交那一条）。
//    **我们照样把它建出来**（用户口径「有什么复刻什么」），只是**永远不亮** —— 这是**忠实地走不到**，
//    不是漏做。自检里我们**手动 `SetActive(true)`** 把它验一遍（`MainMenuScene` 那一段）。
// ② **`GeneralDetails` 有两份、同名不同 pid**（§B·5）：这一支的是 RT `-4327119531760820061`（act T），
//    未入盟支那份是 RT `-8680982849342087005`（act F）。
//    ⇒ 我们的做法是**一份 builder、建两棵独立的树**（跟原版两个 prefab 实例是同一件事），
//      **不是把两份合成一份**。
// ③ **二级页签 `General` / `Trophies` 只在这一支里有**（§B·11）：`AllianceMemberTab.generalButton` →
//    `Generic Tab UI Button Info`（文案 **`General`**，节点名却是 `Info`）/ `trophiesButton` → `…Trophies`。
//    ⚠️ `TrophiesWindow` 出厂 act **F** ⇒ 点 `Trophies` 才亮（这个切换是纯本地的，**能用**）。
//
// ---- 出厂 act=F 的件（**不建**，逐条写明为什么）----
//   · `Config fields` 下的 `LanguagesDropdown` / `Privacy Dropdown` / `Edit` / `Confirm` / `Cancel`
//     （改盟设置那一态，要服务器）；
//   · 两处 `Secondary Icon`（评级旁边的小图标，act F）；
//   · 成员行里的 `Avatar Name`（act F）与 `Raycast Target`（act F，那是命中层，我们另建 `Hit`）；
//   · `GeneralDetails>DEBUG_TEXTS`（原档调试残留，§C·2 明说别照抄）；
//   · 两个下拉的 `Template`（Unity 内置模板）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `AllianceMemberTab`。</summary>
    public class AllianceMemberTab : SocialView
    {
        protected override int QOff { get { return AlliancesTab.QBandMember; } }

        // 队列档（本视图内 0–4）
        const int L_Panel = 0, L_Btn = 1, L_Art = 2, L_Text = 3, L_Hit = 3;
        const int L_Line = 1;

        // ---- 真值 ----
        static readonly PxRect HeaderBtnR = new PxRect(331.17f, 116.65f, 1920.00f, 188.83f);
        static readonly PxRect HeadDivR = new PxRect(330.67f, 185.15f, 1920.50f, 188.83f);
        static readonly PxRect TabsRowR = new PxRect(359.97f, 116.65f, 1417.27f, 185.15f);
        static readonly PxRect InfoBtnR = new PxRect(359.97f, 117.08f, 619.97f, 184.72f);
        static readonly PxRect TrophiesBtnR = new PxRect(632.42f, 117.08f, 892.42f, 184.72f);
        static readonly Vector4 TabBtnBorder = new Vector4(188f, 0f, 99f, 30f);
        static readonly Color TabOnCol = new Color(1f, 0.631f, 0f, 1f), TabOffCol = new Color(1f, 0.544f, 0f, 1f);
        const string ArtTabOn = "40K_tab_button", ArtTabOff = "40K_tab_button_overwindow";

        static readonly PxRect ContentR = new PxRect(331.17f, 188.83f, 1920.00f, 1080.05f);
        public static readonly PxRect GeneralR = new PxRect(332.67f, 162.04f, 1919.00f, 1080.02f);
        public static readonly PxRect TrophiesR = new PxRect(366.97f, 189.04f, 1921.00f, 1079.84f);
        static readonly PxRect ChatPreviewR = new PxRect(1479.80f, 109.80f, 1879.80f, 169.80f);

        Transform _general, _trophies;
        ImageQuad _infoBg, _trophiesBg;

        public GameObject GeneralView { get { return _general != null ? _general.gameObject : null; } }
        public GameObject TrophiesView { get { return _trophies != null ? _trophies.gameObject : null; } }

        public void Build()
        {
            BuildHeader();
            _general = Node(Root, "GeneralDetails", GeneralR);
            AllianceGeneralDetails.Build(this, _general, GeneralR);
            _trophies = Node(Root, "TrophiesWindow", TrophiesR);
            BuildTrophies(_trophies);
            BuildChatPreview();
            ShowGeneral();       // 出厂：`TrophiesWindow` act F ⇒ 默认 General 那一支
        }

        // ---------------------------------------------------------- 二级页签

        void BuildHeader()
        {
            var head = Node(Root, "Alliance Header Buttons (1)", HeaderBtnR);
            Nine(head, "40k_Separator_Fade_Sides_Horizontal", HeadDivR, new Vector4(63f, 0f, 63f, 0f),
                 "Divisor line", L_Line, new Color(0.875f, 0.552f, 0.286f, 1f));
            var row = Node(head, "Tab buttons", TabsRowR);
            _infoBg = Toggle(row, InfoBtnR, "Generic Tab UI Button Info", "General", true);
            _trophiesBg = Toggle(row, TrophiesBtnR, "Generic Tab UI Button Trophies", "Trophies", false);
            Hit(row, "InfoHit", InfoBtnR, L_Hit, ShowGeneral);
            Hit(row, "TrophiesHit", TrophiesBtnR, L_Hit, ShowTrophies);
        }

        ImageQuad Toggle(Transform parent, PxRect r, string name, string text, bool on)
        {
            var n = Node(parent, name, r);
            var q = Rect(n, on ? ArtTabOn : ArtTabOff, r, "Image", L_Btn, on ? TabOnCol : TabOffCol);
            Text(n, new PxRect(r.x1 + 9.66f, r.y1 + 4.66f, r.x2 - 9.66f, r.y2 + 4.66f), text, Color.white,
                 "Button Text", 60f, L_Text, 12f);
            return q;
        }

        public void ShowGeneral()
        {
            if (_general != null) _general.gameObject.SetActive(true);
            if (_trophies != null) _trophies.gameObject.SetActive(false);
            SwapTabArt(false);
        }

        public void ShowTrophies()
        {
            if (_general != null) _general.gameObject.SetActive(false);
            if (_trophies != null) _trophies.gameObject.SetActive(true);
            SwapTabArt(true);
        }

        void SwapTabArt(bool trophiesOn)
        {
            var onTex = Page.ArtOf(ArtTabOn);
            if (_infoBg != null && onTex != null)
            {
                _infoBg.SetTexture(onTex);
                _infoBg.SetAspect(InfoBtnR.W / InfoBtnR.H);
                _infoBg.SetTint(trophiesOn ? TabOffCol : TabOnCol);
            }
            if (_trophiesBg != null && onTex != null)
            {
                _trophiesBg.SetTexture(onTex);
                _trophiesBg.SetAspect(TrophiesBtnR.W / TrophiesBtnR.H);
                _trophiesBg.SetTint(trophiesOn ? TabOnCol : TabOffCol);
            }
        }

        // ---------------------------------------------------------- `TrophiesWindow`（`AllianceTrophiesView`）

        /// <summary>联盟奖杯页。⚠️ **两个标题用的就是 prefab 里的字面样例串**
        /// （`Featured: Trophy Name` / `45 Trophies Achieved!` —— 都是资产里的真字符串，不是我们编的）；
        /// 奖杯行是 `TrophyDisplay`（`Item Drawer` 里的一个，运行期按数量生成）。
        /// 🔴 这一支**本地走不到**（见文件头 ①）⇒ 我们只把**看得见的骨架**照建。</summary>
        void BuildTrophies(Transform root)
        {
            var nm = Node(root, "CurrentActiveBadge Name",
                          new PxRect(598.41f, 217.39f, 1493.50f, 267.22f));
            Text(nm, new PxRect(598.41f, 217.39f, 1493.50f, 267.22f), "Featured: Trophy Name",
                 Color.white, "Text", 50f, L_Text, 12f);
            var cnt = Node(root, "CurrentActiveBadge Count",
                           new PxRect(597.94f, 267.62f, 1504.44f, 318.73f));
            Text(cnt, new PxRect(597.94f, 267.62f, 1504.44f, 318.73f), "45 Trophies Achieved!",
                 Color.white, "Text", 40f, L_Text, 12f);
            // `CurrentActiveBadge`：那一枚大徽标（`AllianceBadgeDrawer`）—— 徽标图在**服务器**上（盟自己没有存档）
            var badge = Node(root, "CurrentActiveBadge", new PxRect(414.00f, 204.89f, 579.95f, 360.62f));
            Node(badge, "Frame", new PxRect(414.00f, 204.89f, 579.95f, 360.62f));
            Nine(root, "40k_Separator_Fade_Sides_Horizontal", new PxRect(331.07f, 376.71f, 1920.10f, 380.39f),
                 new Vector4(63f, 0f, 63f, 0f), "Divisor line Trophies", L_Line,
                 new Color(0.875f, 0.552f, 0.286f, 1f));
            var scroll = Node(root, "Scroll Rect", new PxRect(366.97f, 378.51f, 1921.00f, 1079.84f));
            var drawer = Node(scroll, "Item Drawer", new PxRect(366.97f, 378.51f, 1921.00f, 755.51f));
            BuildTrophyRows(drawer);
        }

        /// <summary>奖杯格（原版 `TrophyDisplay`，一屏三格：`366.97,401.51→674.97,755.51` 那个是第一个）。
        /// 我们按**奖杯数**生成；本地恒 0 ⇒ 一格都不建。</summary>
        void BuildTrophyRows(Transform drawer)
        {
            for (int i = drawer.childCount - 1; i >= 0; i--) SocialWindow.DestroySafe(drawer.GetChild(i).gameObject);
            int n = SocialData.AllianceTrophies;
            for (int i = 0; i < n; i++)
            {
                float x = 366.97f + i * 307f;
                var cell = Node(drawer, "TrophyDisplay", new PxRect(x, 401.51f, x + 298f, 755.51f));
                var prog = Node(cell, "Progress", new PxRect(x + 35f, 700.58f, x + 283f, 726.05f));
                var bar = Node(prog, "ProgressBar", new PxRect(x + 33.75f, 684.41f, x + 283f, 731.97f));
                Nine(bar, "40k_campaign_bar_bg", new PxRect(x + 33.75f, 684.41f, x + 283f, 731.97f),
                     new Vector4(20f, 0f, 20f, 0f), "Background", L_Panel);
                Text(prog, new PxRect(x + 47.59f, 696.38f, x + 271.61f, 721.94f), "0/0", Color.white,
                     "counter", 26.95f, L_Text, 12f);
            }
        }

        // ---------------------------------------------------------- 联盟聊天预览（属于**这一支**）

        /// <summary>`ChatPreview`（`1479.80,109.80→1879.80,169.80`）—— ⚠️ **它是联盟这一支的孩子、
        /// 不属于好友页**（§B·10）。两条消息行是 `ChatPreviewMessage`；底下那颗钮开的是 `ChatPanel`。</summary>
        void BuildChatPreview()
        {
            var cp = Node(Root, "ChatPreview", ChatPreviewR);
            var box = Node(cp, "Container", new PxRect(1479.50f, 109.80f, 1851.80f, 169.80f));
            Nine(box, "Closed-Chat_background", new PxRect(1479.50f, 109.80f, 1851.80f, 169.80f),
                 Vector4.zero, "Image", L_Panel);
            MsgRow(box, new PxRect(1494.50f, 112.80f, 1851.80f, 139.80f), "Message Preview");
            MsgRow(box, new PxRect(1494.50f, 139.80f, 1851.80f, 166.80f), "Message Preview (1)");
            var btn = Node(cp, "Button", new PxRect(1851.80f, 109.80f, 1879.80f, 169.80f));
            Rect(btn, "40k_alliances_icon_chat", new PxRect(1851.80f, 109.80f, 1879.80f, 169.80f),
                 "Icon", L_Art, null, true);
            Hit(btn, "Hit", new PxRect(1851.80f, 109.80f, 1879.80f, 169.80f), L_Hit, () =>
            {
                Say("`ChatPreview` 那颗钮：原版开**聊天窗**（`ChatPreview.OpenChat` → `WindowsManager`）。");
                // 🔴 聊天窗本身已建（`Shell/ChatPanelWindow.cs`）⇒ 这里**真的开它**，不是只出声。
                if (Win != null) Win.OpenChat();
            });
        }

        void MsgRow(Transform parent, PxRect r, string name)
        {
            var row = Node(parent, name, r);
            // 原版两条预览的文案是富文本（`<color=#00FF20>Player Name:</color> Message`），字号 23
            Text(row, r, "<color=#00FF20>Player Name:</color> Message", Color.white, "text", 23f, L_Text, 0f);
        }
    }

    // ==================================================================
    //  `GeneralDetails`（= 原版 `AllianceView`）—— **一份 builder，两棵树**
    // ==================================================================

    /// <summary>原版 `AllianceView`（节点名 `GeneralDetails`），两支各一份实例。
    /// 只建 **act=T** 的那些件（判据见 `AllianceMemberTab` 文件头那段）。</summary>
    public static class AllianceGeneralDetails
    {
        public static void Build(AllianceMemberTab v, Transform root, PxRect r)
        {
            // `BadgeDrawer`（盟徽：Frame + Badge）—— 徽标图的来源在服务器（盟自己的存档）
            var bd = Node(root, "BadgeDrawer", new PxRect(360.40f, 170.48f, 610.95f, 405.60f));
            Node(bd, "Frame", new PxRect(360.40f, 170.48f, 610.95f, 405.60f));
            Node(bd, "Badge", new PxRect(360.40f, 170.48f, 610.95f, 405.60f));

            // 盟名（原版静态样例是 `Alliance Name bla bla`；我们读数据源，空就空着）
            var nm = Node(root, "Alliance name text", new PxRect(614.88f, 177.48f, 1114.24f, 248.56f));
            v.Text(nm, new PxRect(614.88f, 177.48f, 1114.24f, 248.56f), SocialData.AllianceName ?? "",
                   Color.white, "Text", 50f, 3, 18f);

            // 两个评级块：`Alliance Rating Display`（段位图标）/ `Draft Rating Display`（骷髅图标）
            // 🔴 **两块的 `Main Icon` 不是同一张图**：一个 `40k_UI_icon_ranked_Skirmish`、
            //    一个 `40k_battle_Win Skull`（§A·1 第 276/280 行附近）。
            RatingRow(v, root, 237.38f, "Alliance Rating Display", "40k_UI_icon_ranked_Skirmish", "");
            RatingRow(v, root, 318.21f, "Draft Rating Display", "40k_battle_Win_Skull", "");

            // `Config fields` 里那一行**看得见**的：`extra_info`（语言 / 隐私摘要，40px Right/Middle）
            var cf = Node(root, "Config fields", new PxRect(1079.54f, 183.02f, 1862.35f, 243.02f));
            var ei = Node(cf, "extra_info", new PxRect(1408.66f, 183.02f, 1862.35f, 243.02f));
            v.Text(ei, new PxRect(1408.66f, 183.02f, 1862.35f, 243.02f), "English / Private",
                   Color.white, "Text", 40f, 3, 18f, false);

            // 联盟简介（`Description input text`；原版样例是一串 Aliance Description，属调试残留 §C·2）
            var desc = Node(root, "Description input text", new PxRect(1130.44f, 288.85f, 1879.21f, 482.08f));
            var dtx = Node(desc, "description text", new PxRect(1146.32f, 291.64f, 1863.33f, 481.71f));
            v.Text(dtx, new PxRect(1146.32f, 291.64f, 1863.33f, 481.71f), "", new Color(1f, 1f, 1f, 1f),
                   "Text", 38f, 3, 0f);

            // 成员区分隔线 + `MemberList`
            v.Nine(root, "40k_Separator_Fade_Sides_Horizontal", new PxRect(330.67f, 489.85f, 1920.50f, 493.53f),
                   new Vector4(63f, 0f, 63f, 0f), "Divisor line members", 1,
                   new Color(0.875f, 0.552f, 0.286f, 1f));
            var ml = Node(root, "MemberList", new PxRect(369.42f, 446.55f, 1880.92f, 1080.05f));
            var lbl = Node(ml, "members label", new PxRect(369.42f, 447.01f, 964.25f, 488.44f));
            v.Text(lbl, new PxRect(369.42f, 447.01f, 964.25f, 488.44f), "Members: --/20", Color.white,
                   "Text", 38.35f, 3, 18f);
            var sv = Node(ml, "Scroll View", new PxRect(369.67f, 493.63f, 1880.67f, 1080.05f));
            var vp = Node(sv, "Viewport", new PxRect(369.67f, 493.63f, 1880.67f, 1080.05f));
            var content = Node(vp, "Content", new PxRect(369.67f, 493.63f, 1880.67f, 677.63f));
            AllianceMemberRow.BuildAll(v, content);
        }

        /// <summary>一个评级块：`Main Icon` + `Individual rating value`（右对齐）。
        /// ⚠️ 原版还有 `Secondary Icon`（act **F**）⇒ 不建。</summary>
        static void RatingRow(AllianceMemberTab v, Transform root, float y, string name, string art, string value)
        {
            var row = Node(root, name, new PxRect(614.88f, y, 1101.46f, y + 80.83f));
            v.Rect(row, art, new PxRect(614.88f, y, 674.88f, y + 80.83f), "Main Icon", 2, null, true);
            var val = Node(row, "Individual rating value", new PxRect(674.88f, y, 1101.46f, y + 80.83f));
            v.Text(val, new PxRect(674.88f, y, 1101.46f, y + 80.83f), value ?? "", Color.white,
                   "Text", 45f, 3, 18f, false);
        }

        static Transform Node(Transform parent, string name, PxRect r) { return MenuDraw.Node(parent, name, r); }
    }

    // ==================================================================
    //  成员行（原版 `AllianceMemberEntry`，§A·2·3 的独立根 `6030421012472178610`）
    // ==================================================================

    /// <summary>成员行（750×100 —— 与 `MemberList>Scroll View>Viewport>Content` 的
    /// `GridLayoutGroup` cellSize **逐值相同**，所以直接吃 cell 尺寸即可，不用再推）。
    /// ⚠️ 行里**两处评级圆**用的图不同：`Draft Rating` = **骷髅** · `Ranked Rating` = **段位图标**。</summary>
    public static class AllianceMemberRow
    {
        /// <summary>按数据条数逐行建（原版 `AllianceMemberList` 用 `entryPrefab` 逐条 Instantiate）。
        /// 行位按 `GridLayoutGroup`（cell **750×100** · spacing (10,7.22) · pad (0,0,9,75)）推。</summary>
        public static void BuildAll(AllianceMemberTab v, Transform content)
        {
            for (int i = content.childCount - 1; i >= 0; i--) SocialWindow.DestroySafe(content.GetChild(i).gameObject);
            var all = SocialData.Members;
            for (int i = 0; i < all.Count; i++)
            {
                float x = 369.67f + 9f;                         // padLeft 0 + padTop 9（原版那个 pad 是 (0,0,9,75)）
                float y = 493.63f + 9f + i * (100f + 7.22f);
                Build(v, content, all[i], new PxRect(x, y, x + 750f, y + 100f));
            }
        }

        /// <summary>一行（坐标都是**行内相对**，逐值照 §A·2·3 的表 —— 那份表用的根尺寸正好是 750×100）。</summary>
        public static void Build(AllianceMemberTab v, Transform content, SocialData.Member m, PxRect r)
        {
            var row = MenuDraw.Node(content, "Alliance Member Entry", r);
            v.Nine(row, "40K_dropdown_bg", r, new Vector4(23f, 20f, 23f, 20f), "background", 0,
                   new Color(1f, 0.36f, 0f, 1f));
            // `background/Image`（左侧那条深色竖带）+ 名次数字
            v.Rect(row, null, new PxRect(r.x1 + 2.24f, r.y1 + 2.52f, r.x1 + 47.54f, r.y1 + 97.59f),
                   "Image", 0, new Color(0.481f, 0.182f, 0f, 1f));
            v.Text(row, new PxRect(r.x1 + 2.52f, r.y1 + 2.56f, r.x1 + 47.04f, r.y1 + 97.34f),
                   m.Index.ToString(), Color.white, "member index", 40f, 3, 18f);
            // 头像（`Avatar Item Small`：Highlight + Border + Image）—— 立绘走 `CardArt.Cosmetics`
            var av = new PxRect(r.x1 + 50.57f, r.y1 + 11.87f, r.x1 + 149.44f, r.y1 + 114.93f);
            var avn = MenuDraw.Node(row, "Avatar Item Small", av);
            v.Rect(avn, "Player_Avatar_selected", new PxRect(av.x1, av.y1 - 3.53f, av.x2 + 3.34f, av.y2 - 36.37f),
                   "Highlight", 2, null, true);
            var imgC = MenuDraw.Node(avn, "Image Container", new PxRect(av.x1, av.y1, av.x2, av.y1 + 65.69f));
            v.Cosmetic(imgC, m.AvatarArt, new PxRect(av.x1, av.y1 - 2.70f, av.x2, av.y1 + 62.99f), "Image", 2);
            v.Rect(avn, "Player_Profile_Border", new PxRect(av.x1, av.y1 + 6.57f, av.x2, av.y1 + 72.26f),
                   "Border", 2, null, true);
            // 在线状态点
            if (m.Online)
                v.Rect(row, "40K_icon_status_online", new PxRect(r.x1 + 52.90f, r.y1 + 68.33f, r.x1 + 75.46f, r.y1 + 97.35f),
                       "connection status", 2, new Color(0f, 1f, 0.0736f, 1f), true);
            else
                v.Rect(row, "40K_icon_status_offline", new PxRect(r.x1 + 52.90f, r.y1 + 68.33f, r.x1 + 75.46f, r.y1 + 97.35f),
                       "connection status", 2, new Color(0.84f, 0.494f, 0.44f, 1f), true);
            v.Text(row, new PxRect(r.x1 + 149.44f, r.y1 + 11.87f, r.x1 + 694.75f, r.y1 + 59.26f), m.Name ?? "",
                   Color.white, "member name", 50f, 3, 18f);
            v.Text(row, new PxRect(r.x1 + 149.44f, r.y1 + 59.26f, r.x1 + 495.52f, r.y1 + 98.56f), m.Role ?? "",
                   new Color(0.887f, 0.887f, 0.887f, 1f), "member role", 41.45f, 3, 18f);
            // 两处评级（`VerticalLayoutGroup` 里上下两行，各 46.5 高）
            Rating(v, row, r, 3.36f, "Draft Rating", "40k_battle_Win_Skull", m.DraftRating, false);
            Rating(v, row, r, 49.86f, "Ranked Rating", "40k_UI_icon_ranked_Skirmish", m.RankedRating, true);
            v.Hit(row, "Hit", r, 3, () =>
                SocialPage.Say("点成员那一行：原版是 `AllianceMemberEntry` 的 `button`（开成员选项弹窗 "
                             + "`AllianceMemberOptionsPopup`，8 个钮全要服务器 —— 判据 → `多人界面_入口与调用.md` §③）。"));
        }

        /// <summary>一处评级：`Main Icon` + `Individual rating value`（**右对齐**）。
        /// ⚠️ 上下两块的 `Main Icon` 的 pivot **不同**（上 `(.5,.5)`、下 `(1,.5)` ⇒ 下那块是贴右的），
        /// 逐值照 §A·2·3 第 474/478 行。</summary>
        static void Rating(AllianceMemberTab v, Transform row, PxRect r, float dy, string name, string art,
                           string value, bool rightPivot)
        {
            float x1 = rightPivot ? r.x1 + 610.18f : r.x1 + 675.18f;
            v.Rect(row, art, new PxRect(x1, r.y1 + dy, x1 + 65f, r.y1 + dy + 46.5f), name + "/Main Icon", 2, null, true);
            v.Text(row, new PxRect(r.x1 + 610.18f, r.y1 + dy, r.x1 + 740.18f, r.y1 + dy + 46.5f), value ?? "",
                   Color.white, name + "/Individual rating value", 42f, 3, 18f, false);
        }
    }
}
