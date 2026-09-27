// AlliancesTab.cs — 社交窗第 1 页：**联盟**（原版 `AlliancesTab`，`WindowTabBase<SocialMenuWindow>`）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/社交_联盟与好友页.md` §A·1（主树的 **47–379 行**）· §A·2·1/§A·2·2（两款行族的独立根）·
// §B·4/§B·6/§B·7/§B·11/§B·12/§B·13 · §C。**入口/调用/监听** → `资料/普查产出_0927/多人界面_入口与调用.md` §③。
//
// ---- 🔴 这一页的结构（读原始 JSON 定的，别按印象改）----
// `Alliances Tab`
//   ├ `AllianceNotMemberVariant`（`AllianceSearchTab`）**act = T** ← 默认就是这一支
//   │    ├ `Alliance Header Buttons` → `Divisor line` + `Tab buttons`（**两个 `EverguildToggle`**，
//   │    │     文案 `Join` / `Create` —— ⚠️ 它们**不是 `Button`**，是页签键，`AllianceSearchTab`
//   │    │     的两个字段 `joinButton`/`createButton` 指它们，点了切下面两个视图）
//   │    ├ `List View`（`JoinAllianceMenu`）—— 搜索框 + `Invitations` 列表 + `Open Alliances` 列表
//   │    └ `Create Alliance View`（`CreateAllianceMenu`）**act = F** ← 点 `Create` 才亮
//   └ `AllianceMemberVariant`（`AllianceMemberTab`）**act = F** ← 已在盟里那一支（见 `AllianceMemberTab.cs`）
//
// 🔴 **两个 `GeneralDetails` 不是同一份**（§B·5）：`AllianceMemberVariant>GeneralDetails`
//    （RT `-4327119531760820061`，act T）与 `AllianceNotMemberVariant>GeneralDetails`
//    （RT `-8680982849342087005`，act **F**），同名不同 pid，靠 `AllianceMemberTab.generalView` /
//    `AllianceSearchTab.allianceView` 两个字段分清 —— **合并成一份会同时弄错两态**。
//    我们的做法：**一份 builder、建两棵独立的树**（`BuildGeneralDetails`，与 `AllianceMemberTab` 共用）——
//    这跟原版「两个 prefab 实例」是同一件事，不是把两份合成一份。
//
// ---- 出厂 act=F 的件：**按「可切到的状态 / 装饰」二分**（别一刀切）----
//   · 可切到的状态（点一下就会亮）⇒ **建**：`Create Alliance View`（点 `Create` 键）；
//   · 只是「编辑态/未激活态」的件 ⇒ **不建**，逐条列在下面（每处都写明为什么）：
//     `Config fields` 下的 `LanguagesDropdown` / `Privacy Dropdown` / `Edit` / `Confirm` / `Cancel`
//     （五个全是 act F —— 它们是**改盟设置**那一态，改设置要服务器）、两处 `Secondary Icon`（act F）、
//     `GeneralDetails>DEBUG_TEXTS`（原档调试残留，§C·2 明说**别照抄**）、
//     两个 `TMP_Dropdown` 的 `Template`（**Unity 内置模板**，`Item Label = 'Option A'` —— §B·13 明说别当业务节点）。
//
// ---- 数据 ----
// 邀请 / 公开联盟 / 成员**全在服务器** ⇒ `SocialData` 三张表**默认恒空** ⇒ 三个列表**一行都不建**
// （原版那几个行实例是美术原位参照，运行期 `FillGroupInvitations`/`FillOpenAlliances` 会清掉重填，
//   判据 → `多人界面_入口与调用.md` §③ 社交那一条）。留白、不编。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `AlliancesTab`。持有**两支**（`allianceNotMemberVariant` / `allianceMemberVariant`）。</summary>
    public class AlliancesTab : SocialPage
    {
        public override WindowTabType Type { get { return WindowTabType.SocialAlliances; } }
        protected override int PageIndex { get { return 0; } }

        // 队列档：本页 10 个号里，**两支各占 5 个**（未入盟支 0–4、入盟支 5–9）——
        // 两支互斥，但这样即使同时亮也不会打架（入盟支整片盖住未入盟支）。
        public const int QBandSearch = 0, QBandMember = 5;

        /// <summary>`AllianceNotMemberVariant`（`AllianceSearchTab`，**act T**）—— 默认这一支。</summary>
        public AllianceSearchTab Search { get; private set; }
        /// <summary>`AllianceMemberVariant`（`AllianceMemberTab`，**act F**）—— 已在盟里那一支。</summary>
        public AllianceMemberTab Member { get; private set; }

        public void SetHost(SocialWindow win) { Win = win; }

        public override void Setup()
        {
            // ---- `AllianceNotMemberVariant`：332.17,70.90→1919.50,1080.02（§A·1 第 48 行）----
            var nm = Node(Root, "AllianceNotMemberVariant", new PxRect(332.17f, 70.90f, 1919.50f, 1080.02f));
            Search = nm.gameObject.AddComponent<AllianceSearchTab>();
            Search.Page = this;
            Search.Build();

            // ---- `AllianceMemberVariant`：332.67,162.04→1919.00,1080.02（§A·1 第 302 行附近）----
            //   ⚠️ 出厂 act=F ⇒ 建完就关掉（它的内容仍然建出来，「可切到的状态」那一类）。
            var mv = Node(Root, "AllianceMemberVariant", new PxRect(332.67f, 162.04f, 1919.00f, 1080.02f));
            Member = mv.gameObject.AddComponent<AllianceMemberTab>();
            Member.Page = this;
            Member.Build();
            Member.gameObject.SetActive(false);

            Debug.Log("[Social] 联盟页：默认 `AllianceNotMemberVariant`（未入盟）。"
                    + "⚠️ 原版按 `AlliancesManager.CurrentGroupCached`（服务器）二选一 ⇒ **本地恒走未入盟支**，"
                    + "`AllianceMemberVariant` 建了但**走不到**（如实记着，不是漏做）。");
        }

        /// <summary>联盟名（`Alliance name text`）—— 两支共用一份数据源时用得上；本地恒空。</summary>
        public string AllianceName { get { return SocialData.AllianceName; } }
    }

    // ==================================================================
    //  `AllianceSearchTab` —— 未入盟那一支
    // ==================================================================

    /// <summary>原版 `AllianceSearchTab`。字段：`allianceView` / `createAllianceMenu` / `createButton` /
    /// `joinAllianceMenu` / `joinButton` / `joinButtonText`。</summary>
    public class AllianceSearchTab : SocialView
    {
        protected override int QOff { get { return AlliancesTab.QBandSearch; } }

        // 队列档（本视图内 0–4；命中区 3–4）
        const int L_Panel = 0, L_Btn = 1, L_Art = 2, L_Text = 3, L_Line = 1, L_Hit = 3;

        // ---- 真值（§A·1 第 49–67、104–120 行）----
        static readonly PxRect HeaderBtnR = new PxRect(331.67f, 89.87f, 1920.00f, 162.04f);
        static readonly PxRect HeadDivR = new PxRect(331.17f, 158.36f, 1920.50f, 162.04f);
        /// <summary>`Join` 键：`360.47,90.30→620.47,157.94`（`40K_tab_button_overwindow` 489×97 ·
        /// 九宫 (188,0,99,30) · ppuMul 1.5）。文案 `Join`，**字号 60**、`auto[12,60]`。</summary>
        static readonly PxRect JoinBtnR = new PxRect(360.47f, 90.30f, 620.47f, 157.94f);
        /// <summary>`Create` 键：`632.92,90.30→892.92,157.94`（同底图）。</summary>
        static readonly PxRect CreateBtnR = new PxRect(632.92f, 90.30f, 892.92f, 157.94f);
        static readonly Vector4 TabBtnBorder = new Vector4(188f, 0f, 99f, 30f);
        /// <summary>两个键的**选中态换图**：`onSprite = 40K_tab_button` · `offSprite = 40K_tab_button_overwindow`
        /// （由 Atlas `SpriteAtlas_4765312961718699286` 的 `m_PackedSprites ↔ names` 解出）。
        /// 染色 `onColor = (1,0.631,0,1)` · `offColor = (1,0.544,0,1)`。</summary>
        const string ArtTabOn = "40K_tab_button", ArtTabOff = "40K_tab_button_overwindow";
        static readonly Color TabOnCol = new Color(1f, 0.631f, 0f, 1f), TabOffCol = new Color(1f, 0.544f, 0f, 1f);
        const float TabBtnPx = 60f, TabBtnAutoMin = 12f;

        static readonly PxRect ListViewR = new PxRect(360.99f, 162.04f, 1902.59f, 1080.02f);
        static readonly PxRect CreateViewR = new PxRect(368.48f, 165.12f, 1882.38f, 1080.02f);
        static readonly PxRect SearchFieldR = new PxRect(1402.00f, 172.90f, 1798.57f, 229.86f);
        static readonly PxRect SearchPhR = new PxRect(1406.57f, 172.90f, 1758.57f, 229.86f);
        static readonly PxRect SearchGoR = new PxRect(1800.78f, 171.38f, 1860.78f, 231.38f);
        static readonly PxRect SearchGoIconR = new PxRect(1812.42f, 183.02f, 1849.14f, 219.74f);
        static readonly PxRect InvTitleR = new PxRect(360.99f, 252.29f, 1865.99f, 297.29f);
        static readonly PxRect InvListR = new PxRect(360.99f, 312.29f, 1865.99f, 312.29f);
        static readonly PxRect OpenTitleR = new PxRect(360.99f, 277.29f, 1874.89f, 322.29f);
        static readonly PxRect OpenViewportR = new PxRect(360.99f, 337.29f, 1874.90f, 1079.77f);
        static readonly PxRect OpenListR = new PxRect(360.99f, 337.29f, 1865.99f, 447.29f);
        /// <summary>一款行族的行高 = **110**（`Invitation List Entry` / `Alliance List Entry` 都是），
        /// 列表间距 **10**（§A·2·1/§A·2·2 的根 `sizeDelta=(…,110)`；`Invitations>List` 的 VLG `spacing=10`）。</summary>
        const float RowH = 110f, RowGap = 10f;

        Transform _listView, _createView, _invList, _openList;
        ImageQuad _joinBg, _createBg;
        public GameObject CreateAllianceView { get { return _createView != null ? _createView.gameObject : null; } }
        public GameObject JoinAllianceView { get { return _listView != null ? _listView.gameObject : null; } }

        public void Build()
        {
            BuildHeader();
            BuildListView();
            BuildCreateView();
            ShowJoin();          // 出厂：`Create Alliance View` act F ⇒ 默认是 Join 那一支
        }

        // ---------------------------------------------------------- 顶部两个页签键

        void BuildHeader()
        {
            var head = Node(Root, "Alliance Header Buttons", HeaderBtnR);
            Nine(head, "40k_Separator_Fade_Sides_Horizontal", HeadDivR, new Vector4(63f, 0f, 63f, 0f),
                 "Divisor line", L_Line, new Color(0.875f, 0.552f, 0.286f, 1f));
            _joinBg = TabToggle(head, JoinBtnR, "Generic Tab UI Button Search", "Join", true);
            _createBg = TabToggle(head, CreateBtnR, "Generic Tab UI Button Create", "Create", false);
            Hit(head, "JoinHit", JoinBtnR, L_Hit, ShowJoin);
            // ⚠️ 原版这一颗是 `EverguildToggle`，点了**不是去建盟**而是切到建盟那张表 —— 我们照做，
            //    并在切过去的视图里出声（建盟本身要服务器）。
            Hit(head, "CreateHit", CreateBtnR, L_Hit, ShowCreate);
        }

        /// <summary>一个页签键（底图 + 文案）。返回底图 quad 供选中态换图/换色。</summary>
        ImageQuad TabToggle(Transform parent, PxRect r, string name, string text, bool on)
        {
            var n = Node(parent, name, r);
            var q = Rect(n, on ? ArtTabOn : ArtTabOff, r, "Image", L_Btn, on ? TabOnCol : TabOffCol);
            var tr = new PxRect(r.x1 + 9.66f, r.y1 + 4.66f, r.x2 - 9.66f, r.y2 + 4.66f);   // `Button Text` 的实测矩形
            Text(n, tr, text, Color.white, "Button Text", TabBtnPx, L_Text, TabBtnAutoMin);
            return q;
        }

        /// <summary>`ShowJoin`（原版 `AllianceSearchTab.ShowJoinAllianceMenu`）—— 切到 `List View`。</summary>
        public void ShowJoin()
        {
            if (_listView != null) _listView.gameObject.SetActive(true);
            if (_createView != null) _createView.gameObject.SetActive(false);
            SwapTabArt(false);
        }

        /// <summary>`ShowCreateAllianceMenu` —— 切到 `Create Alliance View`（并**出声**：建盟要服务器）。</summary>
        public void ShowCreate()
        {
            if (_listView != null) _listView.gameObject.SetActive(false);
            if (_createView != null) _createView.gameObject.SetActive(true);
            SwapTabArt(true);
            Say("`Create` 页：原版这一步是向服务器**建一个联盟**（`CreateAllianceMenu` 的 `createButton`）——"
              + "本地没有服务器 ⇒ **表填了也建不了**，按下去的钮会如实出声。");
        }

        /// <summary>选中态：**换图 + 换色**（原版 `EverguildToggle` 的 `onSprite/offSprite` + `onColor/offColor`，
        /// 值全部实读）。0/1 = Join/Create。</summary>
        void SwapTabArt(bool createOn)
        {
            var onTex = Win.Art(ArtTabOn);
            if (_joinBg != null && onTex != null)
            {
                _joinBg.SetTexture(onTex);
                _joinBg.SetAspect(JoinBtnR.W / JoinBtnR.H);   // ⚠️ `SetTexture` 会把 aspect 冲成贴图自己的比值
                _joinBg.SetTint(createOn ? TabOffCol : TabOnCol);
            }
            if (_createBg != null && onTex != null)
            {
                _createBg.SetTexture(onTex);
                _createBg.SetAspect(CreateBtnR.W / CreateBtnR.H);
                _createBg.SetTint(createOn ? TabOnCol : TabOffCol);
            }
        }

        // ---------------------------------------------------------- `List View`（`JoinAllianceMenu`）

        void BuildListView()
        {
            _listView = Node(Root, "List View", ListViewR);

            // 搜索框（在右边）：`InputFieldBackground` 九宫 + 占位 `Search` + 那颗圆形搜索钮
            var sf = Node(_listView, "Search Field", SearchFieldR);
            Nine(sf, "InputFieldBackground", SearchFieldR, new Vector4(10f, 10f, 10f, 10f), "Background",
                 L_Panel, new Color(0.0627f, 0f, 0f, 1f));
            Text(sf, SearchPhR, "Search", new Color(1f, 1f, 1f, 0.58f), "Placeholder", 50f, L_Text, 18f);
            Hit(sf, "SearchHit", SearchFieldR, L_Hit, () => Say(
                "`Search Field`（找联盟）**输入框打不了字** —— 我们这套外壳没有文字输入系统；"
              + "而且**搜索本身也要服务器**。"));

            var go = Node(_listView, "Generic Round Button Variant", SearchGoR);
            Rect(go, "40k_general_bt_yellow", SearchGoR, "Bg", L_Btn, null, true);
            Rect(go, "40k_icon_search", SearchGoIconR, "Image", L_Art, null, true);
            // ⚠️ 原版这个钮里那个 `Button Text = 'X'`（act **F**）是调试残留 ⇒ 不建。
            Hit(go, "GoHit", SearchGoR, L_Hit, () => Say("搜索联盟：要**服务器**（原版走 `JoinAllianceMenu.searchButton`）。"));

            // `List Area`（VLG spacing 25）→ `Invitations` / `Open Alliances`
            var area = Node(_listView, "List Area", new PxRect(361.00f, 252.29f, 1874.90f, 1079.77f));

            var inv = Node(area, "Invitations", new PxRect(360.99f, 252.29f, 1874.90f, 252.29f));
            Text(inv, InvTitleR, "Alliances invitations:", Color.white, "Title", 47.5f, L_Text, 18f);
            _invList = Node(inv, "List", InvListR);
            BuildRows(_invList, InvListR, SocialData.Invitations.Count, BuildInvitationRow);

            var open = Node(area, "Open Alliances", new PxRect(360.99f, 277.29f, 1874.90f, 1079.77f));
            Text(open, OpenTitleR, "Open alliances:", Color.white, "Title", 47.5f, L_Text, 18f);
            var vp = Node(open, "Viewport", OpenViewportR);
            _openList = Node(vp, "List", OpenListR);
            BuildRows(_openList, OpenListR, SocialData.OpenAlliances.Count, BuildAllianceListRow);
        }

        /// <summary>逐行建（原版 `FillGroupInvitations` / `FillOpenAlliances` 会**清空重填**）。
        /// 行高 110 / 间距 10，从列表顶边往下排 —— 表里那两个行实例的 y 正是这么摆的
        /// （312.29→422.29、432.29→542.29）。</summary>
        void BuildRows(Transform list, PxRect listRect, int count, System.Action<Transform, PxRect, int> build)
        {
            for (int i = list.childCount - 1; i >= 0; i--) SocialWindow.DestroySafe(list.GetChild(i).gameObject);
            for (int i = 0; i < count; i++)
            {
                float y = listRect.y1 + i * (RowH + RowGap);
                build(list, new PxRect(listRect.x1, y, listRect.x2, y + RowH), i);
            }
        }

        // ---------------------------------------------------------- 行模板一：`AllianceInvitationEntry`

        /// <summary>邀请行（原版 `AllianceInvitationEntry`，§A·2·1 的独立根 `-5586301021970909275`）。
        /// 尾部两个钮的文案是 **`Join` + `Dismiss`**（节点名却叫 `Reject`）。</summary>
        void BuildInvitationRow(Transform list, PxRect r, int i)
        {
            var m = SocialData.Invitations[i];
            var row = Node(list, "Invitation List Entry", r);
            RowBase(row, r, "background");
            var bd = Node(row, "BadgeDrawer", new PxRect(r.x1 + 18.80f, r.y1 + 10.00f, r.x1 + 108.80f, r.y1 + 100.00f));
            Node(bd, "Frame", new PxRect(r.x1 + 18.80f, r.y1 + 10.00f, r.x1 + 108.80f, r.y1 + 100.00f));
            Node(bd, "Badge", new PxRect(r.x1 + 18.80f, r.y1 + 10.00f, r.x1 + 108.80f, r.y1 + 100.00f));
            RowTexts(row, r, m.Name, m.Region, m.Members, m.MemberMax, m.Rating, 4.50f);
            // 两个钮（我方 `+393.8` / 邀请方 `+603.8`，都 200×57）
            RowButton(row, r, 1046.30f, 24.46f, "Join", "Join", () => Say(
                "`Join`（接受邀请）：要**服务器**（原版 `AllianceInvitationEntry.HandleJoin`）。"));
            RowButton(row, r, 1256.30f, 23.70f, "Reject", "Dismiss", () => Say(
                "`Dismiss`（拒绝邀请）：要**服务器**（原版 `HandleDismiss`）。"));
            Hit(row, "InfoHit", r, L_Hit, () => Say("点这一行看联盟信息：原版 `HandleInfo` 开的是**服务器**上的详情。"));
        }

        // ---------------------------------------------------------- 行模板二：`AllianceListEntry`

        /// <summary>公开联盟行（原版 `AllianceListEntry`，§A·2·2 的独立根 `471114273799851884`）。
        /// 与邀请行同构，只是尾部换成**一个** `Join`。</summary>
        void BuildAllianceListRow(Transform list, PxRect r, int i)
        {
            var m = SocialData.OpenAlliances[i];
            var row = Node(list, "Entry", r);
            RowBase(row, r, "background");
            var bd = Node(row, "BadgeDrawer", new PxRect(r.x1 + 18.80f, r.y1 + 10.00f, r.x1 + 108.80f, r.y1 + 100.00f));
            Node(bd, "Frame", new PxRect(r.x1 + 18.80f, r.y1 + 10.00f, r.x1 + 108.80f, r.y1 + 100.00f));
            Node(bd, "Badge", new PxRect(r.x1 + 18.80f, r.y1 + 10.00f, r.x1 + 108.80f, r.y1 + 100.00f));
            RowTexts(row, r, m.Name, m.Region, m.Members, m.MemberMax, m.Rating, 4.50f);
            RowButton(row, r, 1188.00f, 21.18f, "Generic UI Button", "Join", () => Say(
                "`Join`（加入这个联盟）：要**服务器**（原版 `AllianceListEntry.TryJoin`）。"));
            Hit(row, "InfoHit", r, L_Hit, () => Say("点这一行：原版 `HandleInfo` 看联盟详情（服务器）。"));
        }

        // ---------------------------------------------------------- 两款行族共用的零件
        //
        // 🔴 两款行的**内件几何逐值相同**（把 §A·2·1 与 §A·2·2 两张表并排看：Title/Region/
        //    Members Header/Member Count/Ranking Header 全是同一组绝对偏移，只有尾部那个钮不同）
        //    ⇒ 收口成下面三个函数（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。

        void RowBase(Transform row, PxRect r, string name)
        {
            Nine(row, "40K_dropdown_bg", r, new Vector4(23f, 20f, 23f, 20f), name, L_Panel,
                 new Color(0.981f, 0.469f, 0.356f, 1f));
        }

        /// <summary>行里的五段字。⚠️ **两款行的 `Members Header` / `Member Count` / `Ranking Header`
        /// 的 y 差 4px**（邀请行 `-31/-81.508/-31` vs 公开行 `-35/-83.645/-35`，§A·2·1 vs §A·2·2）
        /// —— 这里按**各自表**里的绝对 rect 走，`dy` 那个参数就是给这个差用的。</summary>
        void RowTexts(Transform row, PxRect r, string name, string region, int members, int max,
                      string rating, float dy)
        {
            Text(row, new PxRect(r.x1 + 133.55f, r.y1 + 4.50f, r.x1 + 639.03f, r.y1 + 57.50f), name ?? "",
                 Color.white, "Title", 55.9f, L_Text, 18f);
            Text(row, new PxRect(r.x1 + 133.55f, r.y1 + 58.36f, r.x1 + 639.03f, r.y1 + 102.94f), region ?? "",
                 new Color(0.906f, 0.906f, 0.906f, 1f), "Region", 47.05f, L_Text, 18f);
            Text(row, new PxRect(r.x1 + 639.04f, r.y1 + 12.82f + dy, r.x1 + 816.14f, r.y1 + 49.18f + dy), "Members:",
                 Color.white, "Members Header", 38.35f, L_Text, 18f);
            Text(row, new PxRect(r.x1 + 639.04f, r.y1 + 55.31f + dy, r.x1 + 816.15f, r.y1 + 107.70f + dy),
                 members + "/" + max, Color.white, "Member Count", 50f, L_Text, 18f);
            Text(row, new PxRect(r.x1 + 847.45f, r.y1 + 12.82f + dy, r.x1 + 1024.55f, r.y1 + 49.18f + dy), "Ranking:",
                 Color.white, "Ranking Header", 38.35f, L_Text, 18f);
            // `Ranking`：图标（段位）+ 数值。原版是 `HorizontalLayoutGroup` 排的（图标 53.6/55.4 见方）
            Rect(row, "40k_UI_icon_ranked_Skirmish",
                 new PxRect(r.x1 + 888.23f, r.y1 + 51.50f, r.x1 + 941.86f, r.y1 + 106.90f),
                 "Icon", L_Art, null, true);
            Text(row, new PxRect(r.x1 + 941.86f, r.y1 + 53.00f, r.x1 + 1024.55f, r.y1 + 106.90f),
                 rating ?? "", Color.white, "Ranking Value", 50f, L_Text, 18f);
        }

        /// <summary>行尾那颗钮（`40K_button` 489×107 · 九宫 (234,46,234,46) · preserveAspect）。</summary>
        void RowButton(Transform row, PxRect r, float x1, float y1, string name, string text, System.Action onClick)
        {
            var br = new PxRect(x1, r.y1 + y1, x1 + 200f, r.y1 + y1 + 57f);
            var n = Node(row, name, br);
            Nine(n, "40K_button", br, new Vector4(234f, 46f, 234f, 46f), "Bg", L_Btn);
            Text(n, new PxRect(br.x1 + 13f, br.y1, br.x2 - 13f, br.y2), text, Color.white, "Button Text",
                 36.65f, L_Text, 12f);
            Hit(n, "Hit", br, L_Hit, onClick);
        }

        // ---------------------------------------------------------- `Create Alliance View`（`CreateAllianceMenu`）

        /// <summary>建盟表（原版 `CreateAllianceMenu`，**act F**）。⚠️ **五项操作全要服务器**：
        /// 名字/描述（要打字）、语言/隐私（两个 `TMP_Dropdown`）、`Continue`（花 1000 水晶建盟）
        /// ⇒ 我们把**看得见的版面照建**，点下去一律出声。
        /// 两个下拉的 `Template` 是 **Unity 内置模板**（`Item Label = 'Option A'`）⇒ **不建**（§B·13）。</summary>
        void BuildCreateView()
        {
            _createView = Node(Root, "Create Alliance View", CreateViewR);

            Field(_createView, "Name input title", "Alliance Name",
                  new PxRect(432.47f, 257.65f, 1125.42f, 307.65f),
                  new PxRect(432.47f, 307.95f, 1332.47f, 367.35f), "Name Input");
            Field(_createView, "Desc input title", "Alliance Description",
                  new PxRect(432.48f, 402.99f, 1125.43f, 452.99f),
                  new PxRect(432.48f, 454.55f, 1332.48f, 659.00f), "Desc Input");

            // `Create Alliance Text`（标题）+ `Price Display Button`（`Continue` + 1000 水晶）
            Text(_createView, new PxRect(428.10f, 666.82f, 678.10f, 723.60f), "Create alliance", Color.white,
                 "Create Alliance Text", 40f, L_Text, 18f);
            var price = new PxRect(428.09f, 714.21f, 678.14f, 792.99f);
            var pb = Node(_createView, "Price Display Button", price);
            Nine(pb, "40K_button", price, new Vector4(234f, 46f, 234f, 46f), "Generic UI Button", L_Btn);
            // `Price Display`：水晶图标 + 价格（`1000`）
            Rect(pb, "40k_general_icon_currency_crystal",
                 new PxRect(495.15f, 730.73f, 542.06f, 777.65f), "icon", L_Art, null, true);
            Text(pb, new PxRect(542.06f, 730.73f, 609.12f, 777.65f), "1000", Color.white, "text", 40f, L_Text, 13.46f);
            Hit(pb, "Hit", price, L_Hit, () => Say(
                "`Continue`（花 1000 建盟）：要**服务器** —— 本地没有联盟系统，资源也花不掉。"));

            // 语言 / 隐私两个下拉（只建「合上的那一面」：底图 + 空 Label + 箭头）
            Dropdown(_createView, "Select Language", "Select language",
                     new PxRect(1440.43f, 250.87f, 1690.43f, 307.65f),
                     new PxRect(1440.43f, 307.64f, 1690.43f, 367.04f), "LanguagesDropdown");
            Dropdown(_createView, "Select Privacy", "Select privacy",
                     new PxRect(1440.43f, 397.50f, 1690.43f, 454.28f),
                     new PxRect(1440.43f, 454.28f, 1690.43f, 513.68f), "Privacy Dropdown");
        }

        /// <summary>一个「标题 + 输入框」组（建盟页那两组）。输入框是 `40K_dropdown_bg` 九宫。⚠️ 打不了字。</summary>
        void Field(Transform parent, string titleName, string title, PxRect titleR, PxRect boxR, string boxName)
        {
            Text(parent, titleR, title, Color.white, titleName, 40f, L_Text, 18f);
            var box = Node(parent, boxName, boxR);
            Nine(box, "40K_dropdown_bg", boxR, new Vector4(23f, 20f, 23f, 20f), "Bg", L_Panel,
                 new Color(1f, 0.475f, 0.098f, 1f));
            Hit(box, "Hit", boxR, L_Hit, () => Say(
                $"`{boxName}` **输入框打不了字** —— 我们这套外壳没有文字输入系统。"));
        }

        /// <summary>一个下拉的合上面（`40K_dropdown_field_closed` 727×102 · 九宫 (60,35,60,35) + 箭头
        /// `40K_dropdown_arrow_closed`）。⚠️ 点开要 `Template`，那是 Unity 内置模板 ⇒ 我们**不建**。</summary>
        void Dropdown(Transform parent, string name, string title, PxRect titleR, PxRect fieldR, string fieldName)
        {
            Text(parent, titleR, title, Color.white, name, 40f, L_Text, 18f);
            var f = Node(parent, fieldName, fieldR);
            Nine(f, "40K_dropdown_field_closed", fieldR, new Vector4(60f, 35f, 60f, 35f), "Bg", L_Panel,
                 new Color(1f, 0.475f, 0.098f, 1f));
            Rect(f, "40K_dropdown_arrow_closed",
                 new PxRect(fieldR.x1 + 225f, fieldR.y1 + 19.7f, fieldR.x1 + 245f, fieldR.y1 + 39.7f),
                 "Arrow", L_Art, null, true);
            Hit(f, "Hit", fieldR, L_Hit, () => Say(
                $"`{fieldName}` 下拉：**没接**（选了也没用 —— 建盟要服务器；原版的展开靠 Unity 内置 `Template`）。"));
        }
    }
}
