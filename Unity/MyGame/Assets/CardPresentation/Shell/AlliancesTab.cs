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
//   │    ├ `Create Alliance View`（`CreateAllianceMenu`）**act = F** ← 点 `Create` 才亮
//   │    └ `GeneralDetails`（= 原版 `AllianceView`，**act = F**，RT `-8680982849342087005`）
//   │           ← 🆕 2026-10-04（A55②）：「**点公开列表里某个盟 → 看它的详情**」那一态，
//   │             原版由 `AllianceSearchTab.HandleDisplayAlliance` 点亮 —— 见 `BuildGeneralDetails()`
//   └ `AllianceMemberVariant`（`AllianceMemberTab`）**act = F** ← 已在盟里那一支（见 `AllianceMemberTab.cs`）
//
// 🔴 **两个 `GeneralDetails` 不是同一份**（§B·5）：`AllianceMemberVariant>GeneralDetails`
//    （RT `-4327119531760820061`，act T）与 `AllianceNotMemberVariant>GeneralDetails`
//    （RT `-8680982849342087005`，act **F**），同名不同 pid，靠 `AllianceMemberTab.generalView` /
//    `AllianceSearchTab.allianceView` 两个字段分清 —— **合并成一份会同时弄错两态**。
//    我们的做法：**一份 builder、建两棵**（`AllianceGeneralDetails.Build(..., Variant)`）。
//    ⚠️ **2026-10-04 更正（A35③ 审查查出）**：这一行原来写着「**建两棵独立的树**」—— 当时**不成立，实际只建了一棵**
//    （挂在 `AllianceMemberVariant` 下的那一棵，`AllianceSearchTab` 那一侧**一个 `GeneralDetails` 都没有**）；
//    更要紧的是那**一棵**的几何还是**两份实例各取一半**（上半段 = `AllianceNotMemberVariant` 那份 act F，
//    下半段 = `AllianceMemberVariant` 那份 act T）。✅ **两笔都在 2026-10-04 收掉了**：
//    A29 把那一棵整套换成 act T；**A55②** 把**缺的 act F 那棵**补上（本文件的 `BuildGeneralDetails()`，
//    几何 = `GeoF`、出厂 act F，由 `HandleDisplayAlliance` 点亮）。逐节点清单与两套几何的判据见
//    `AllianceMemberTab.cs` 的 `AllianceGeneralDetails`（`GeoT` / `GeoF` 各一行带普查行号）。
//
// ---- 出厂 act=F 的件：**按「可切到的状态 / 装饰」二分**（别一刀切）----
//   · 可切到的状态（点一下就会亮）⇒ **建**：`Create Alliance View`（点 `Create` 键）；
//     🆕 `AllianceNotMemberVariant>GeneralDetails`（**未入盟支那一棵**，出厂 act F —— 见下面 `BuildGeneralDetails()`）；
//   · 只是「编辑态/未激活态」的件 ⇒ **不建**，逐条列在下面（每处都写明为什么）：
//     `Config fields` 下的 `LanguagesDropdown` / `Privacy Dropdown` / `Edit` / `Confirm` / `Cancel`
//     （它们在**未入盟支那一棵** `GeneralDetails` 里是 act F —— 那五个属于**改盟设置**那一态，改设置要服务器）、
//     两处 `Secondary Icon`（act F）、
//     🔴 **2026-10-04 更正（A55①）** —— 这一条原来写「**五个全是 act F**」，**是把另一份实例的值当成了通用值**：
//        `Config fields` 在 prefab 里**有两份**（同 `GeneralDetails` 一样同名不同 pid，见 §B·5）：
//        · 未入盟支那份（`AllianceNotMemberVariant>GeneralDetails`，普查 `社交_联盟与好友页.md:186-222`）：
//          `extra_info` = **T**、上面那五个 = **F**（`:187` vs `:188,201,214,217,220`）；
//        · 已入盟支那份（`AllianceMemberVariant>GeneralDetails`，普查 `:279-315`）：**正好相反** ——
//          `extra_info` = **F**（`:280`）、那五个 = **T**（`:281,294,307,310,313`）。
//        ⇒ 「五个全是 act F」**只对未入盟支那一棵成立**；`AllianceMemberTab` 那一棵（act T 那份）
//        **五个全建**（A29 已按 act T 接上，见 `AllianceMemberTab.cs` 的 `AllianceGeneralDetails.Build`）。
//        本条留在「不建」这一节，管的是**未入盟支那棵树**（本文件辖内，见下面 `BuildGeneralDetails()`）。
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
        /// <summary>🆕 2026-10-04（A55②）：`AllianceNotMemberVariant>GeneralDetails` 的矩形 ——
        /// **这是 act F 那一份**（RT `-8680982849342087005`，普查 `社交_联盟与好友页.md:171`
        /// `332.67,162.04→1919.00,1080.02`），与 `AllianceMemberTab.GeneralR`
        /// （RT `-4327119531760820061`，act T，`331.17,188.83→1920.00,1080.05`）**同名不同 pid、数值也不同**。
        /// ⚠️ 它不是「同一个节点的另一种状态」—— 原版**两颗都在**，各由一支点亮（§B·5）。</summary>
        public static readonly PxRect GeneralSearchR = new PxRect(332.67f, 162.04f, 1919.00f, 1080.02f);
        static readonly PxRect SearchFieldR = new PxRect(1402.00f, 172.90f, 1798.57f, 229.86f);
        static readonly PxRect SearchPhR = new PxRect(1406.57f, 172.90f, 1758.57f, 229.86f);
        static readonly PxRect SearchGoR = new PxRect(1800.78f, 171.38f, 1860.78f, 231.38f);
        static readonly PxRect SearchGoIconR = new PxRect(1812.42f, 183.02f, 1849.14f, 219.74f);
        static readonly PxRect InvTitleR = new PxRect(360.99f, 252.29f, 1865.99f, 297.29f);
        static readonly PxRect InvListR = new PxRect(360.99f, 312.29f, 1865.99f, 312.29f);
        static readonly PxRect OpenTitleR = new PxRect(360.99f, 277.29f, 1874.89f, 322.29f);
        static readonly PxRect OpenViewportR = new PxRect(360.99f, 337.29f, 1874.90f, 1079.77f);
        static readonly PxRect OpenListR = new PxRect(360.99f, 337.29f, 1865.99f, 447.29f);
        /// <summary>两款行族的**行高 = 110**（`Invitation List Entry` 与 `Alliance List Entry` 的根
        /// `sizeDelta` 都是 `(…,110)`，§A·2·1 / §A·2·2）。</summary>
        const float RowH = 110f;
        /// <summary>`Invitations` 那一列的**行距 = 10**。判据 = `Invitations>List` 那个 `VerticalLayoutGroup`
        /// 的 `spacing=10.0`（普查 `资料/普查产出_0927/社交_联盟与好友页.md:67`）——
        /// 表里两行实例 `312.29→422.29` / `432.29→542.29` 的步进正是 **120 = 110 + 10**。
        /// 🔴 **它与 `Open Alliances` 那一列【不是同一个数】**（铁律 5·c：一个值 ≠ 全部情况）。</summary>
        const float InvRowGap = 10f;
        /// <summary>`Open Alliances` 那一列的**行距 = 5**（🔴 原来两款行族共用 `RowGap = 10` = 照
        /// `Invitations` 抄的 ⇒ 这一列每行多 5px；2026-10-04 审查查出）。
        /// <para>判据（**原版值，不是我们的常量**）：原版这一格是 **`RecyclableScrollRect`
        /// （`PolyAndCode.UI.RecyclableScrollRect : ScrollRect`，MB `MonoBehaviour_-8780120914984378205.json`）**
        /// —— 实读 `_spacingY: 5.0` · `_cellHeight: 110.0` · `IsGrid: 0`；
        /// **行距 = `_spacingY + _cellHeight` = 115**，双份判据：
        /// ① 反汇编 `d:/2/tools/decomp_full/PolyAndCode.UI.VerticalRecyclingSystem__CreateCellPool.c:255,270`
        ///    —— 每下一个格子 `fVar21 −= *(float*)(param_1 + 0x44) + *(float*)(param_1 + 0x50)`
        ///    = `_spacingY + _cellHeight`（字段偏移由 `:226` 的 `set_sizeDelta(…+0x4c)` 那份
        ///    `Vector2(_cellWidth, _cellHeight)` 与网格分支 `:262` 的 `(_spacingX + _cellWidth) × col` 交叉钉死）；
        /// ② `PolyAndCode.UI.VerticalRecyclingSystem._InitCoroutine_d__19__MoveNext.c:57-63`
        ///    —— `Content.sizeDelta.y = (_spacingY + _cellHeight) × 行数 − spacingY`
        ///    （`IsGrid=0` 时末尾那一项 = `_spacingY`）⇒ **内容高 = `N × 110 + (N−1) × 5`**。
        /// ⚠️ 同仓既有结论一致：`资料/普查产出_0923/A3_Cards页.md:78`「行距 = `_spacingY + _cellHeight`」。</para></summary>
        const float OpenRowGap = 5f;

        Transform _listView, _createView, _invList, _openList;
        ImageQuad _joinBg, _createBg;
        /// <summary>🆕 2026-10-04（A55②）：`AllianceNotMemberVariant>GeneralDetails`（**act F** 那一棵）
        /// —— 「点公开列表里某个盟 → 看它的详情」那一态的整棵树（原版 `AllianceSearchTab.allianceView`）。</summary>
        Transform _detail;
        /// <summary>那一棵树里 `Alliance name text` 那个 `Label`（切进详情态时要改字）。</summary>
        Label _detailName;
        /// <summary>`Join` 键上那句文案（🔴 原版在详情态会把它换成**另一个词条**，见 `LabelBack`）。</summary>
        Label _joinLabel;
        public GameObject CreateAllianceView { get { return _createView != null ? _createView.gameObject : null; } }
        public GameObject JoinAllianceView { get { return _listView != null ? _listView.gameObject : null; } }
        /// <summary>自检用：`AllianceNotMemberVariant>GeneralDetails`（act F 那一棵，本地出厂 **act F**）。</summary>
        public GameObject GeneralDetailsView { get { return _detail != null ? _detail.gameObject : null; } }
        /// <summary>自检用：现在是不是「看别的盟」那一态。</summary>
        public bool ShowingDetails { get { return _detail != null && _detail.gameObject.activeSelf; } }
        /// <summary>自检用：详情那一棵的**成员列滚动区**（视口 = act F 那份 `MemberList>Scroll View`，
        /// `371.17,466.85→1882.17,1080.02`）。</summary>
        public MenuScroll DetailScroll { get; private set; }

        /// <summary>🆕 2026-10-03（A25④）：`Open Alliances` 那一格的**纵向滚动区**（全壳唯一一份滚动实现
        /// = `MenuScroll`）。原版 = **`RecyclableScrollRect`**（`PolyAndCode.UI.RecyclableScrollRect : ScrollRect`，
        /// 见 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/PolyAndCode/UI/RecyclableScrollRect.cs:8`）
        /// `h=0 v=1` · **`m_MovementType=1`(Elastic)** · `m_Inertia=1` · `m_Elasticity=0.1` · `decel=0.135`
        /// · `m_ScrollSensitivity=1.0`（原始 JSON 实读：`MonoBehaviour_-8780120914984378205.json`
        /// 的 `m_MovementType: 1` + `m_Content → 3863230360907210915`）。
        /// ⚠️ 视口 = `Open Alliances>Viewport` 那个节点自己的矩形（`OpenViewportR`，原版身上是 `Image + RectMask2D`）
        /// —— **没有另挑一个矩形**（同 `BattleLogPopup`）。⚠️ 滚动灵敏度那一档**不逐处复刻**（全壳一个手感，
        /// 已知自选，记在普查 §D6）。</summary>
        MenuScroll _openScroll;

        /// <summary>自检用：`Open Alliances` 那一格的滚动区（原版 = `RecyclableScrollRect`）。</summary>
        public MenuScroll OpenListScroll { get { return _openScroll; } }

        public void Build()
        {
            BuildHeader();
            BuildListView();
            BuildCreateView();
            BuildGeneralDetails();   // 🆕 A55②：未入盟支那一棵 `GeneralDetails`（出厂 act F）
            ShowJoin();          // 出厂：`Create Alliance View` act F ⇒ 默认是 Join 那一支
        }

        // ---------------------------------------------------------- 顶部两个页签键

        void BuildHeader()
        {
            var head = Node(Root, "Alliance Header Buttons", HeaderBtnR);
            Nine(head, "40k_Separator_Fade_Sides_Horizontal", HeadDivR, new Vector4(63f, 0f, 63f, 0f),
                 "Divisor line", L_Line, new Color(0.875f, 0.552f, 0.286f, 1f));
            _joinBg = TabToggle(head, JoinBtnR, "Generic Tab UI Button Search", "Join", true, out _joinLabel);
            _createBg = TabToggle(head, CreateBtnR, "Generic Tab UI Button Create", "Create", false, out _);
            Hit(head, "JoinHit", JoinBtnR, L_Hit, ShowJoin);
            // ⚠️ 原版这一颗是 `EverguildToggle`，点了**不是去建盟**而是切到建盟那张表 —— 我们照做，
            //    并在切过去的视图里出声（建盟本身要服务器）。
            Hit(head, "CreateHit", CreateBtnR, L_Hit, ShowCreate);
        }

        /// <summary>一个页签键（底图 + 文案）。返回底图 quad 供选中态换图/换色；`label` 交出那句文案
        /// （`Join` 那颗的文案在详情态要换，见 `LabelBack`）。</summary>
        ImageQuad TabToggle(Transform parent, PxRect r, string name, string text, bool on, out Label label)
        {
            var n = Node(parent, name, r);
            var q = Rect(n, on ? ArtTabOn : ArtTabOff, r, "Image", L_Btn, on ? TabOnCol : TabOffCol);
            var tr = new PxRect(r.x1 + 9.66f, r.y1 + 4.66f, r.x2 - 9.66f, r.y2 + 4.66f);   // `Button Text` 的实测矩形
            // 🔴 **2026-10-08（A213）`wrap: false`**：原版这 **2** 颗页签键（`Join` / `Create`）的
            //    `Button Text` 实读 **`折行=0`**（`字号=60 auto[12~60] 对齐=Center/Midline`）——
            //    判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Social Submenu Variant" --depth 16 --md`
            //    ⇒ `Alliance Header Buttons/Tab buttons/Generic Tab UI Button {Search,Create}/Button Text`。
            //    走 `SocialWindow.Text` 时**默认恒折行**（那条路的 `MenuDraw.TextBox` 无条件 `SetWrapWidth`）。
            label = Text(n, tr, text, Color.white, "Button Text", TabBtnPx, L_Text, TabBtnAutoMin, wrap: false);
            return q;
        }

        /// <summary>`ShowJoin`（原版 `AllianceSearchTab.ShowJoinAllianceMenu`）—— 切到 `List View`。
        /// 🆕 A55②：也是「看别的盟」那一态的**退路** —— 原版在那一态把两个 `EverguildToggle` 都置
        /// `isOn = 0`（`HandleDisplayAlliance` 的头两句），而 `ShowJoinAllianceMenu` 的第一句就是
        /// `set_isOn(1)` ⇒ **再点一次 `Join` 键就回到列表**（`Start` 里就是把这两个键的 `onValueChanged`
        /// 接到 `ShowJoinAllianceMenu` / `ShowCreateAllianceMenu`，反编译 `AllianceSearchTab__Start.c`）。</summary>
        public void ShowJoin()
        {
            if (_listView != null) _listView.gameObject.SetActive(true);
            if (_createView != null) _createView.gameObject.SetActive(false);
            if (_detail != null) _detail.gameObject.SetActive(false);
            SetJoinLabel(LabelJoin);
            SwapTabArt(0);
        }

        /// <summary>`ShowCreateAllianceMenu` —— 切到 `Create Alliance View`（并**出声**：建盟要服务器）。
        /// 🆕 A55②：原版那一段也**关掉 `allianceView`**（`SetActive(*(param_1 + 0x48), 0)`，
        /// 就在 `SetActive(createAllianceMenu, 1)` 之前）⇒ 我们照做。</summary>
        public void ShowCreate()
        {
            if (_listView != null) _listView.gameObject.SetActive(false);
            if (_createView != null) _createView.gameObject.SetActive(true);
            if (_detail != null) _detail.gameObject.SetActive(false);
            SetJoinLabel(LabelJoin);
            SwapTabArt(1);
            Say("`Create` 页：原版这一步是向服务器**建一个联盟**（`CreateAllianceMenu` 的 `createButton`）——"
              + "本地没有服务器 ⇒ **表填不了、盟也建不了**，按下去的钮会如实出声。");
        }

        // ---------------------------------------------------------- `GeneralDetails`（未入盟支那一棵，act F）

        /// <summary>`AllianceSearchTab` 那两颗键出厂的字面文案（资产里的 `Button Text`，普查 §A·1 `:49-67`）。</summary>
        const string LabelJoin = "Join";
        /// <summary>🔴 **详情态里 `Join` 那颗会换一句文案** —— 原版 `HandleDisplayAlliance` 把
        /// `joinButtonText` 设成**另一个词条**（`AllianceSearchTab__HandleDisplayAlliance.c` 里那个
        /// `I2_Loc_LocalizationManager__GetTranslation(DAT_1842bfc18)`，与 `Initialize` /
        /// `ShowJoinAllianceMenu` 用的 `DAT_1842532b0` **不是同一条**）⇒ 原版那颗在那一态读的字**变了**。
        /// ⚠️ **词条在远端本地化表，本地一条都取不到**（同 `BattleDriver.cs:2631` 与 `ChoosePanel` 那两处
        /// 「原版词条取不到 ⇒ 落兜底」）⇒ 下面这个串**是我们挑的兜底**，⛔ 不是原版词条。
        /// 语义上是「退回列表」，与 `ShowJoin` 那条真退路自洽。</summary>
        const string LabelBack = "Back";

        void SetJoinLabel(string s) { if (_joinLabel != null) _joinLabel.SetText(s); }

        /// <summary>建 `AllianceNotMemberVariant>GeneralDetails`（**act F** 那一棵）。
        /// 判据（**铁律 4：先解父链/用途字段**）：我们这一支 = `AllianceNotMemberVariant`（组件
        /// `AllianceSearchTab`），原版挂在它下面的 `GeneralDetails` 是 RT `-8680982849342087005`
        /// （act F，普查 `:171`）—— 与 `AllianceMemberTab` 那一棵（RT `-4327119531760820061`，act T）
        /// **同名不同 pid**（§B·5）⇒ **两份都建，各取各的值**（⛔ 别合并、也别拿一份的值套另一份；
        /// 见 `AllianceMemberTab.cs` 的 `AllianceGeneralDetails.Build` 与 `GeoT`/`GeoF` 两套几何）。
        /// ⚠️ **出厂 `act F`** ⇒ 建完就关（它由 `HandleDisplayAlliance` 点亮）。</summary>
        void BuildGeneralDetails()
        {
            _detail = Node(Root, "GeneralDetails", GeneralSearchR);
            _detailName = AllianceGeneralDetails.Build(this, _detail, GeneralSearchR,
                                                       AllianceGeneralDetails.Variant.Search, "",
                                                       out MenuScroll sc);
            DetailScroll = sc;
            _detail.gameObject.SetActive(false);
        }

        /// <summary>原版 `AllianceSearchTab.HandleDisplayAlliance(group)` —— **点公开列表（或邀请行）
        /// 里那个盟 → 看它的详情**。判据（反编译 `d:/2/tools/decomp_full/AllianceSearchTab__HandleDisplayAlliance.c`）：
        /// ① `joinButton.isOn = 0` · ② `joinButtonText ← GetTranslation(另一个词条)` · ③ `createButton.isOn = 0`
        /// · ④ `joinAllianceMenu.SetActive(false)` · ⑤ `createAllianceMenu.SetActive(false)`
        /// · ⑥ `allianceView.SetActive(true)` · ⑦ `allianceView.content.SetActive(false)`
        /// （等 `AllianceView.Draw(group)` 拿到数据再开）· ⑧ `AlliancesController.GetGroupById(id, cb)`。
        /// <para>⚠️ 第 ⑦ 条我们不照做（`content` 建完就开着）：原版关它是为了「数据没到之前不闪一个空面板」，
        /// 而我们**没有那个异步源** —— 关着就永远不开了（那才是静默失败）。</para>
        /// ⚠️ 形参是**盟名字符串**、不是原版的 `Group` —— 原版那个 `Group` 由服务器给，
        /// 我们本地唯一真拿得到的字段就是名字（见下条）。</summary>
        public void HandleDisplayAlliance(string allianceName)
        {
            if (_listView != null) _listView.gameObject.SetActive(false);
            if (_createView != null) _createView.gameObject.SetActive(false);
            if (_detail != null) _detail.gameObject.SetActive(true);
            ShowTabArtNone();
            SetJoinLabel(LabelBack);
            if (_detailName != null) _detailName.SetText(allianceName ?? "");
            Say("看联盟详情：原版走 `AlliancesController.GetGroupById`（**服务器**）拿整个 group，再 "
              + "`AllianceView.Draw(group)` 填徽标 / 两个评级 / 简介 / 成员列。本地**只填得起盟名**"
              + $"（= 你点的那一行：`{allianceName ?? "(空)"}`），其余留白 —— 不是漏做，是没有那个源。"
              + "退回列表：点 `Join` 那一颗（原版在详情态把两颗页签都置 `isOn = 0`，"
              + "`ShowJoinAllianceMenu` 再置回 1 —— 就是这一步回去的）。");
        }

        /// <summary>选中态：**换图 + 换色**（原版 `EverguildToggle` 的 `onSprite/offSprite` + `onColor/offColor`，
        /// 值全部实读）。`onTab`：**0 = `Join`** · **1 = `Create`** · **−1 = 两颗都灭**（详情态 ——
        /// 原版 `HandleDisplayAlliance` 把两个 `EverguildToggle.isOn` 都置 0）。</summary>
        void SwapTabArt(int onTab)
        {
            var onTex = Win.Art(ArtTabOn);
            if (_joinBg != null && onTex != null)
            {
                _joinBg.SetTexture(onTex);
                _joinBg.SetAspect(JoinBtnR.W / JoinBtnR.H);   // ⚠️ `SetTexture` 会把 aspect 冲成贴图自己的比值
                _joinBg.SetTint(onTab == 0 ? TabOnCol : TabOffCol);
            }
            if (_createBg != null && onTex != null)
            {
                _createBg.SetTexture(onTex);
                _createBg.SetAspect(CreateBtnR.W / CreateBtnR.H);
                _createBg.SetTint(onTab == 1 ? TabOnCol : TabOffCol);
            }
        }

        /// <summary>详情态那一档：两颗都灭（`SwapTabArt(-1)`，名字留一个、免得调用处写魔数）。</summary>
        void ShowTabArtNone() { SwapTabArt(-1); }

        // ---------------------------------------------------------- `List View`（`JoinAllianceMenu`）

        void BuildListView()
        {
            _listView = Node(Root, "List View", ListViewR);

            // 搜索框（在右边）：`InputFieldBackground` 九宫 + 占位 `Search` + 那颗圆形搜索钮
            var sf = Node(_listView, "Search Field", SearchFieldR);
            Nine(sf, "InputFieldBackground", SearchFieldR, new Vector4(10f, 10f, 10f, 10f), "Background",
                 L_Panel, new Color(0.0627f, 0f, 0f, 1f));
            // 🔴 **2026-10-08（A213）`wrap: false`**：原版 `List View/Search Field/Text Area/Placeholder`
            //    （文本 `Search`）实读 **`折行=0`**（`字号=50 auto[18~50] 对齐=Left/Midline`，同一条 dump）。
            Text(sf, SearchPhR, "Search", new Color(1f, 1f, 1f, 0.58f), "Placeholder", 50f, L_Text, 18f, wrap: false);
            Hit(sf, "SearchHit", SearchFieldR, L_Hit, () => Say(
                "`Search Field`（找联盟）**输入框打不了字** —— 我们这套外壳没有文字输入系统；"
              + "而且**搜索本身也要服务器**。"));

            var go = Node(_listView, "Generic Round Button Variant", SearchGoR);
            var goQ = Rect(go, "40k_general_bt_yellow", SearchGoR, "Bg", L_Btn, null, true);
            Rect(go, "40k_icon_search", SearchGoIconR, "Image", L_Art, null, true);
            // ⚠️ 原版这个钮里那个 `Button Text = 'X'`（act **F**）是调试残留 ⇒ 不建。
            // 🆕 A17：原版 `Social Submenu Variant>…>Search Field>Generic Round Button Variant` 是 SpriteSwap（普查 §块 5 第 12 行）
            Hit(go, "GoHit", SearchGoR, L_Hit, () => Say("搜索联盟：要**服务器**（原版走 `JoinAllianceMenu.searchButton`）。"),
                goQ, "40k_general_bt_yellow");

            // `List Area`（VLG spacing 25）→ `Invitations` / `Open Alliances`
            var area = Node(_listView, "List Area", new PxRect(361.00f, 252.29f, 1874.90f, 1079.77f));

            var inv = Node(area, "Invitations", new PxRect(360.99f, 252.29f, 1874.90f, 252.29f));
            Text(inv, InvTitleR, "Alliances invitations:", Color.white, "Title", 47.5f, L_Text, 18f);
            _invList = Node(inv, "List", InvListR);
            // ⚠️ `Invitations` 这一列**原版不是滚动区**（树里没有 `ScrollRect`/`Mask`，只有 `List` 的 VLG）
            //    ⇒ 这一处 `sc` 传 **null**：不偏移、不裁（照原版）。
            BuildRows(_invList, InvListR, SocialData.Invitations.Count, BuildInvitationRow, null, InvRowGap);

            var open = Node(area, "Open Alliances", new PxRect(360.99f, 277.29f, 1874.90f, 1079.77f));
            Text(open, OpenTitleR, "Open alliances:", Color.white, "Title", 47.5f, L_Text, 18f);
            var vp = Node(open, "Viewport", OpenViewportR);   // 原版这上面是 `Image + RectMask2D`
            // 🆕 2026-10-03（A25④）：**照原版把滚动区补上**（此前这一格一处滚动都没有 —— 见 `_openScroll` 注释）。
            //   ⚠️ 顺序要紧：**先有滚动区、再让 `SetClip` 生效** —— 只补裁切会把后面的行**藏掉**而不是可滚。
            //   ⚠️ `Owner` 取 `List View`（不是整页）：点 `Create` 键切到建盟表时 `List View` 会被
            //     `SetActive(false)`，这一格必须**一起失去滚轮命中**（`HitScroll` 判的就是 `Owner.activeInHierarchy`）。
            _openScroll = MenuScroll.TopAligned(OpenViewportR, 0f);   // 内容高在 `BuildRows` 里按条数写
            _openScroll.Owner = _listView.gameObject;
            _openScroll.Elastic = true;                               // 原版 `m_MovementType = 1` = Elastic
            _openScroll.OnChanged = RebuildOpenAlliances;             // 滚轮只改 `Offset`、**画是调用方的事**
            SocialPage.RegisterScroll(_openScroll);                    // 指针层要认识它，滚轮才落得到这一格上
            _openList = Node(vp, "List", OpenListR);
            BuildRows(_openList, OpenListR, SocialData.OpenAlliances.Count, BuildAllianceListRow, _openScroll,
                      OpenRowGap);
        }

        /// <summary>滚轮改了偏移 ⇒ 重画公开联盟那一列（**先清再建**，回调会重入 —— 同 `ForgeTab.BuildRewardCells` 那条）。
        /// 数据变了也走它（原版 `FillOpenAlliances` 就是「清空重填」）。**自检的 `RebuildForTest` 也走它** —— 同一条路。</summary>
        void RebuildOpenAlliances()
        {
            BuildRows(_openList, OpenListR, SocialData.OpenAlliances.Count, BuildAllianceListRow, _openScroll,
                      OpenRowGap);
        }

        /// <summary>自检用：喂了数据之后重画（= 原版 `FillOpenAlliances` 那条路）。**只给自检**。</summary>
        public void RebuildForTest() { RebuildOpenAlliances(); }

        /// <summary>逐行建（原版 `FillGroupInvitations` / `FillOpenAlliances` 会**清空重填**）。
        /// 行高恒 110、行距**逐列给**（`rowGap`）—— 因为原版两款行族**不是同一个列表实现**：
        /// `Invitations>List` 是普通 VLG（`spacing=10`）⇒ 步进 120；`Open Alliances` 是 `RecyclableScrollRect`
        /// （`_spacingY 5`）⇒ 步进 **115**（判据见 `InvRowGap` / `OpenRowGap` 两处注释）。
        /// 从列表顶边往下排 —— 表里那两个邀请行实例的 y 正是这么摆的（312.29→422.29、432.29→542.29）。
        /// 🆕 2026-10-03（A25④）：`sc != null` 时行按**滚动偏移之后**的位置摆（`MenuScroll.Shift`）、
        /// 整行滚出视口的**不建**，画之前把 `SetClip(视口)` 设上、画完清掉（= 原版 `Viewport` 的 `RectMask2D`）。
        /// ⚠️ `Invitations` 那一列原版**不是**滚动区 ⇒ 那边传 `null`（行为与接这一批之前一字不差）。</summary>
        void BuildRows(Transform list, PxRect listRect, int count, System.Action<Transform, PxRect, int> build,
                       MenuScroll sc, float rowGap)
        {
            for (int i = list.childCount - 1; i >= 0; i--) SocialWindow.DestroySafe(list.GetChild(i).gameObject);
            // 🔴 内容高写进滚动区（= 原版跑出来的高度）。不写 ⇒ **滚不动**，
            //   而下面「整行滚出视口 ⇒ 不建」那道守卫会把后面的行**彻底藏掉**（同 `BattleLogPopup` 那条）。
            //   `Open Alliances` 这一列的算式 = 原版 `VerticalRecyclingSystem` 的
            //   `(_spacingY + _cellHeight) × N − _spacingY` = `N × RowH + (N−1) × OpenRowGap`（见 `OpenRowGap`）。
            //   ⚠️ 空表那一支也要写（写成 0）—— 否则上一次的内容高留在区里 = 静默的脏值。
            if (sc != null) sc.ContentX2 = listRect.y1 + (count == 0 ? 0f : count * RowH + (count - 1) * rowGap);
            if (sc != null) SetClip(sc.Viewport);
            for (int i = 0; i < count; i++)
            {
                float y = listRect.y1 + i * (RowH + rowGap);
                var r = new PxRect(listRect.x1, y, listRect.x2, y + RowH);
                if (sc != null)
                {
                    r = sc.Shift(r);                                   // 内容坐标 → 屏幕坐标（**只做偏移、不裁**）
                    // 🔴 求交那一份 = `MenuDraw.Visible`（**全工程唯一一份**求交；`ClipRect` 是它「顺带夹出
                    //    可见矩形」的那版，别在这儿再写一遍 `Max/Min`）。⚠️ 2026-10-07 更正（铁律 5 / A12①）：
                    //    原文写「= `MenuDraw.ClipRect`（**全工程唯一一份**）」—— 收口后那两句是**同一份**。
                    if (!MenuDraw.ClipRect(r, sc.Viewport, out _)) continue;
                }
                build(list, r, i);      // `i` 仍然是**数据下标**（不是「第几个建出来的」）—— 行内容取的是 `[i]`
            }
            if (sc != null) SetClip(null);
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
            // 🆕 2026-10-04（A55②）：**这一条真的通到「看详情」那一态了**（不再只出声）。
            // 判据链：`AllianceInvitationEntry.HandleInfo` 发 `OnInfoClick`（反编译
            // `AllianceInvitationEntry__HandleInfo.c`）→ `JoinAllianceMenu.OnDisplayAlliance` →
            // `AllianceSearchTab.HandleDisplayAlliance`（`AllianceSearchTab__Start.c` 里那一条订阅）。
            // ⚠️ **「邀请行也通这一条」是推断**：`JoinAllianceMenu` 只有**一个** `OnDisplayAlliance`
            // 事件、而它给两款行共用同一条 `SetCell`（那个 `RecyclableScrollRect` 的接口），
            // 两条 `HandleInfo` 又各发同一个 `OnInfoClick` ⇒ 两支都该通到这一态。
            // （反编译里 `OnDisplayAlliance` 的**调用点**没解出来 —— 那两个闭包方法体是空的，见报告。）
            Hit(row, "InfoHit", r, L_Hit, () => HandleDisplayAlliance(m.Name));
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
            // 🆕 2026-10-04（A55②）：**点公开列表里的某个盟 → 看它的详情**那一态（原版
            // `AllianceListEntry.HandleInfo` → `OnInfoClick` → `JoinAllianceMenu.OnDisplayAlliance`
            // → `AllianceSearchTab.HandleDisplayAlliance`）。
            Hit(row, "InfoHit", r, L_Hit, () => HandleDisplayAlliance(m.Name));
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
            var bgNine = Nine(n, "40K_button", br, new Vector4(234f, 46f, 234f, 46f), "Bg", L_Btn);
            // 🔴 **2026-10-08（A213）`wrap: false`**：原版行尾那颗 `Join` / `Reject` 的
            //    `Invitation List Entry … Button Text` 实读 **`折行=0`**（`字号=36.65 / 44 auto[12~44]`，
            //    同一条 dump；公开联盟行那颗是 44）—— 走 `SocialWindow.Text` 会**默认折行**。
            Text(n, new PxRect(br.x1 + 13f, br.y1, br.x2 - 13f, br.y2), text, Color.white, "Button Text",
                 36.65f, L_Text, 12f, wrap: false);
            // 🆕 A17：`Invitation List Entry>Invitations>List>…>Join/Reject` 是 SpriteSwap（普查 §块 5 第 13 行）
            // —— 底图是**九宫格** ⇒ 九张一起换
            var h = Hit(n, "Hit", br, L_Hit, onClick);
            var wb = h != null ? h.GetComponent<WindowButton>() : null;
            if (wb != null) wb.BindNine(bgNine, "40K_button");
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
            var pbNine = Nine(pb, "40K_button", price, new Vector4(234f, 46f, 234f, 46f), "Generic UI Button", L_Btn);
            // `Price Display`：水晶图标 + 价格（`1000`）
            // 🔴 **2026-10-07（A77⑫④）`text` 矩形就地重算**：旧值 `542.06 → 609.12`（**= 图标布局框的右沿**起算）。
            //   重算命令（现读）= `python 工具/menu_dump.py bundle_menus_assets_all "Social Submenu Variant" --depth 14 --md`
            //   ⇒ `Price Display Button > Price Display > text` = **546.75,730.73→613.81,777.65**（宽 67.06 不变）。
            //   **为什么变了**：`Price Display` 是 `HorizontalLayoutGroup`（`scaleW=1`）而前一件 `icon` 的
            //   `m_LocalScale = 1.2`（dump 那行标着 `×1.2 → 视觉 56.30×56.30`）⇒ uGUI 的推进量按
            //   `childSize × scaleFactor` 算（`46.91 × 1.2`），而**组内居中**的起始偏移按**乘过缩放**的
            //   requiredSpace 折半 ⇒ 净位移 `46.91 × (1.2 − 1) ÷ 2` = **+4.69**（旧值是旧工具的读数）。
            Rect(pb, "40k_general_icon_currency_crystal",
                 new PxRect(495.15f, 730.73f, 542.06f, 777.65f), "icon", L_Art, null, true);
            Text(pb, new PxRect(546.75f, 730.73f, 613.81f, 777.65f), "1000", Color.white, "text", 40f, L_Text, 13.46f,
                 wrap: false);
            // 🔴 **2026-10-08（A213 · 本件点名的判例）`wrap: false`**：原版
            //    `Create Alliance View/…/Price Display Button/Generic UI Button/Price Display/text`（文本 `1000`）
            //    实读 **`折行=0 · auto[13.46~40] · Center/Capline`**（判据 = `python 工具/menu_dump.py
            //    bundle_menus_assets_all "Social Submenu Variant" --depth 16 --md`）⇒ 走 `SocialWindow.Text`
            //    原来**恒折行** = **真偏离**（A213 普查 §七·C2 顺手读到的真值，本批落地）。
            // 🆕 A17：原版 `Social Submenu Variant>…Create Alliance Text>…` 是 SpriteSwap（普查 §块 5 第 15 行）
            var pbH = Hit(pb, "Hit", price, L_Hit, () => Say(
                "`Continue`（花 1000 建盟）：要**服务器** —— 本地没有联盟系统，资源也花不掉。"));
            var pbWb = pbH != null ? pbH.GetComponent<WindowButton>() : null;
            if (pbWb != null) pbWb.BindNine(pbNine, "40K_button");

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
