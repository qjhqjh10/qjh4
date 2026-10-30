// AllianceSearchTab.cs —— 从 `Shell/AlliancesTab.cs` 里拆出来的（A1506：MonoBehaviour 的类名【必须】与文件名相同，
// 否则 Unity 建不出 MonoScript ⇒ 存场景时被烘成内联 `!u!115` 存根 ⇒ player 加载即崩）。零行为改动。
using UnityEngine;

namespace CardPresentation
{
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
        // 🔴 **2026-10-18（P1 §八 #3）**：这里原来有一条 `TabBtnBorder = (188,0,99,30)`，**声明了却从未使用**
        //   （底图走 `Rect` = 单块拉伸）。现在九宫格那两套 border（两张 sprite **各一份**）收在
        //   `AllianceMemberTab.cs` 文件尾的 `AllianceTabBg` 里 —— 本文件与 `AllianceMemberTab` **共用那一份**，
        //   ⛔ 别在这里再复制一遍数值（铁律 6）。本文件上面 `JoinBtnR` 的 doc 里那句
        //   「`40K_tab_button_overwindow` 489×97 · 九宫 (188,0,99,30) · ppuMul 1.5」**仍然成立**（= 未选中那张）。
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
        /// <summary>两颗页签的**节点**（九宫格底图挂在它下面、名字仍叫 `Image`）。
        /// 🔴 **2026-10-18（P1 §八 #3）**：类型从 `ImageQuad` 改成 `Transform` —— 换图要重建九宫格
        /// （见 `AllianceTabBg` 的类 doc）。</summary>
        Transform _joinBg, _createBg;
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

        // ---------------------------------------------------------- 🆕 2026-10-27（E9 批 · 账 68/70/410）

        /// <summary>`Invitations` 那一整颗（**标题 + 行容器**）。原版 `JoinAllianceMenu.invitationList`
        /// —— 空表时**整颗关掉**（`FillGroupInvitations` 的头两句 / 回调里的 `SetActive(count != 0)`），
        /// 所以「默认空态多一行 `Alliances invitations:`」那个偏离（账 407①）靠它修。</summary>
        Transform _invNode;
        /// <summary>原版 `JoinAllianceMenu.loading` —— **重入守卫**（`if (loading) return;`）。
        /// 我们两张表都是同步的（`Show`/`Hide` 落在同一个调用栈），没有它也不会有重入 ⇒
        /// 但**必须照原版有这个字段**：将来真接上异步源时它就是那道闸（⛔ 别因为「现在用不上」删掉）。</summary>
        bool _loading;
        /// <summary>原版 `AllianceSearchTab.onCreateTab` —— **上次停在哪张表**（`Initialize` 按它恢复）。
        /// `false` = `Join`（`joinAllianceMenu`）· `true` = `Create`（`createAllianceMenu`）。
        /// 三个写点逐句照原版：`ShowJoinAllianceMenu` ⇒ false · `ShowCreateAllianceMenu` ⇒ true ·
        /// `HandleDisplayAlliance` ⇒ false（`out_cpp2il_dev_ilspy/AllianceSearchTab.cs:150-235`）。</summary>
        bool _onCreateTab;
        /// <summary>`Open Alliances` 那一格「搜索」圆钮的 `WindowButton`（要按原版把
        /// `searchButton.interactable` 在取数期间置假：`JoinAllianceMenu__FillOpenAlliances.c:1189/1203`）。</summary>
        WindowButton _searchBtn;

        /// <summary>自检读口：上次停在哪张表（`false` = `Join` / `true` = `Create`）。</summary>
        public bool OnCreateTab { get { return _onCreateTab; } }

        public void Build()
        {
            BuildHeader();
            BuildListView();
            BuildCreateView();
            BuildGeneralDetails();   // 🆕 A55②：未入盟支那一棵 `GeneralDetails`（出厂 act F）
            ShowJoin();          // 出厂：`Create Alliance View` act F ⇒ 默认是 Join 那一支
        }

        /// <summary>= 原版 `AllianceSearchTab.Initialize()`（**逐句**，判据
        /// `out_cpp2il_dev_ilspy/AllianceSearchTab.cs:150-183`）：`SetActive(true)`，然后**按
        /// `onCreateTab` 恢复上次那张表** —— 假 ⇒ `joinButton.isOn = 1` + 四句 `SetActive` +
        /// `joinAllianceMenu.Initialize()` + `onCreateTab = false`；真 ⇒ `createButton.isOn = 1` + 三句
        /// `SetActive` + `createAllianceMenu.Initialize()` + `onCreateTab = true`。
        /// <para>🔴 **谁调它**：原版是 `AlliancesTab.ToFocus/Refresh`（每次进联盟页都重跑）。我们这一支
        /// 由 <see cref="OnEnable"/> 触发 —— 同一件事（「联盟页被点亮时」），只是挂的钩子不同
        /// （`AlliancesTab.cs` 不在本批白名单内，⛔ 没去改它）。**这就是「页签态记忆」的落地点**（账 68）。</para>
        /// <para>⚠️ 未 `Build()` 之前（`AddComponent` 那一刻 GameObject 已激活 ⇒ `OnEnable` 会先跑一次）
        /// 直接返回 —— 否则会对着 null 的 `_listView` 切显隐。</para></summary>
        public void Initialize()
        {
            if (_listView == null) return;      // 还没 Build（见 <see cref="OnEnable"/>）
            if (!_onCreateTab) ShowJoin();
            else ShowCreate();
        }

        /// <summary>见 <see cref="Initialize"/>：原版由 `AlliancesTab.ToFocus/Refresh` 调，我们挂在这里。
        /// 🔴 **幂等**：`Initialize` 只是「按记忆切一次显隐 + 重填一次」，重复跑不叠加任何东西。</summary>
        void OnEnable() { Initialize(); }

        // ---------------------------------------------------------- 顶部两个页签键

        void BuildHeader()
        {
            var head = Node(Root, "Alliance Header Buttons", HeaderBtnR);
            Nine(head, "40k_Separator_Fade_Sides_Horizontal", HeadDivR, new Vector4(63f, 0f, 63f, 0f),
                 "Divisor line", L_Line, new Color(0.875f, 0.552f, 0.286f, 1f));
            // 🔴 **2026-10-18（第八轮）**：两颗页签的**文案**走词条（只换「显示文案」那一个实参）——
            //   `Join` → `SocialMenu/Alliances/Join`（经 `LabelJoin` 那个属性取，见它的 doc：
            //   这个串运行期还会被 `SetJoinLabel` 重设，所以**必须每次现取**）；
            //   `Create` → `SocialMenu/Alliances/Create`（1 颗，父链 = 兄弟节点
            //   `Generic Tab UI Button Create < Tab buttons < Alliance Header Buttons < …` ⇒ 与我们同一条）。
            //   ⛔ 节点名（`"Generic Tab UI Button Search"` / `"… Create"`）与那串字号/auto*/对齐实参一字未动。
            _joinBg = TabToggle(head, JoinBtnR, "Generic Tab UI Button Search", LabelJoin, true, out _joinLabel);
            _createBg = TabToggle(head, CreateBtnR, "Generic Tab UI Button Create",
                                  Loc.T("SocialMenu/Alliances/Create"), false, out _);
            Hit(head, "JoinHit", JoinBtnR, L_Hit, ShowJoin);
            // ⚠️ 原版这一颗是 `EverguildToggle`，点了**不是去建盟**而是切到建盟那张表 —— 我们照做，
            //    并在切过去的视图里出声（建盟本身要服务器）。
            Hit(head, "CreateHit", CreateBtnR, L_Hit, ShowCreate);
        }

        /// <summary>一个页签键（底图 + 文案）。返回**页签节点**（九宫格底图挂在它下面、名字仍是 `Image`）
        /// 供选中态换图/换色；`label` 交出那句文案（`Join` 那颗的文案在详情态要换，见 `LabelBack`）。
        /// 🔴 **2026-10-18（P1 §八 #3）**：返回类型从 `ImageQuad` 改成 `Transform` —— 九宫格底图是
        /// **九块子件**，换图要走 `AllianceTabBg.Build` 重建（两张 sprite 的 `m_Border` 不同，
        /// 只 `SetTexture` 会按旧 border 切角块，见那个类的 doc）。</summary>
        Transform TabToggle(Transform parent, PxRect r, string name, string text, bool on, out Label label)
        {
            var n = Node(parent, name, r);
            // 原版这两颗 `Image.Type = Sliced` · `m_PixelsPerUnitMultiplier = 1.5` ⇒ 九宫格
            // （改前走 `Rect` = 单块拉伸：489px 整图横向压进 260px，圆角端帽被压扁）。
            AllianceTabBg.Build(Page, n, r, on, ArtTabOn, ArtTabOff, TabOnCol, TabOffCol, QOff + L_Btn);
            var tr = new PxRect(r.x1 + 9.66f, r.y1 + 4.66f, r.x2 - 9.66f, r.y2 + 4.66f);   // `Button Text` 的实测矩形
            // 🔴 **2026-10-08（A213）`wrap: false`**：原版这 **2** 颗页签键（`Join` / `Create`）的
            //    `Button Text` 实读 **`折行=0`**（`字号=60 auto[12~60] 对齐=Center/Midline`）——
            //    判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Social Submenu Variant" --depth 16 --md`
            //    ⇒ `Alliance Header Buttons/Tab buttons/Generic Tab UI Button {Search,Create}/Button Text`。
            //    走 `SocialWindow.Text` 时**默认恒折行**（那条路的 `MenuDraw.TextBox` 无条件 `SetWrapWidth`）。
            label = Text(n, tr, text, Color.white, "Button Text", TabBtnPx, L_Text, TabBtnAutoMin,
                         alignLeft: false, wrap: false, autoMaxPx: 60f, autoBasePx: 12f);
            // ☝ A414：原版 `…/Generic Tab UI Button {Search,Create}/Button Text` = `auto[12.0~60.0] 基准=12.0`（表 #1）
            // 🆕 A255：原版 `Center/Midline`（判据见文件头那张表）
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版 `m_VerticalAlignment = 4096 (Midline)`。
            MenuDraw.SetVAlign(label, Label.VAlign.Midline, tr);
            return n;
        }

        /// <summary>转圈件（原版 `MenuLoadingController`）的取用口。
        /// <para>判据（反编译 `JoinAllianceMenu__Initialize.c:53` / `JoinAllianceMenu__FillOpenAlliances.c:29`）：
        /// 原版**唯一**两处 `ShowLoading()` 都在联盟搜索这一支；回调第一句 `HideLoading()`
        /// （`JoinAllianceMenu.__c__DisplayClass23_0___FillOpenAlliances_b__0.c:30`）。
        /// 生产态走 `ShellRuntime.Instance.LoadingCtl`；批处理/自检里 `ShellRuntime.Instance` 恒 null ⇒
        /// 兜底 `LoadingMenu.Current`（同一个件，见它那段 doc）。取不到就返回 null（调用处不静默：见 `WithLoading`）。</para></summary>
        static LoadingController Loading
        {
            get
            {
                var rt = ShellRuntime.Instance;
                var lm = rt != null && rt.Loading != null ? rt.Loading : LoadingMenu.Current;
                return lm != null ? lm.Controller : null;
            }
        }

        /// <summary>按原版次序包一层「显示转圈 → 重填 → 隐藏转圈」。
        /// <para>⚠️ **视觉上仍是 0 帧**：我们三张联盟表是本地同步数据、没有异步源 ⇒ `Show()` 与 `Hide()`
        /// 落在**同一个调用栈**里（引擎帧末才画）。这不是「假装加载」，而是**只复刻代码路径** ——
        /// 将来接上真异步源即自动生效。⛔ **别为了看见转圈加人为延时 / 最小持续时间**（那是自造行为）。</para></summary>
        void WithLoading(System.Action refill)
        {
            var loading = Loading;
            if (loading == null)
            {
                // 不许静默：原版这里必有一次转圈；取不到宿主 = 这一层没接上
                Debug.LogWarning("[AllianceSearchTab] 转圈件取不到（`ShellRuntime.Instance` 与 `LoadingMenu.Current` 都为 null）"
                               + " —— 联盟搜索的 `ShowLoading/HideLoading` 这一段**没跑**；数据重填照常。");
                refill();
                return;
            }
            loading.Show();
            try { refill(); }
            finally { loading.Hide(); }
        }

        /// <summary>`ShowJoin`（原版 `AllianceSearchTab.ShowJoinAllianceMenu`）—— 切到 `List View`。
        /// 🆕 A55②：也是「看别的盟」那一态的**退路** —— 原版在那一态把两个 `EverguildToggle` 都置
        /// `isOn = 0`（`HandleDisplayAlliance` 的头两句），而 `ShowJoinAllianceMenu` 的第一句就是
        /// `set_isOn(1)` ⇒ **再点一次 `Join` 键就回到列表**（`Start` 里就是把这两个键的 `onValueChanged`
        /// 接到 `ShowJoinAllianceMenu` / `ShowCreateAllianceMenu`，反编译 `AllianceSearchTab__Start.c`）。
        /// <para>🔴 **2026-10-20（W58·D4）**：本方法末尾那一步重填按原版走 `WithLoading`
        /// （`JoinAllianceMenu.Initialize` 的 `ShowLoading → GetGroupList → 回调首句 HideLoading`）。</para></summary>
        public void ShowJoin()
        {
            if (_listView != null) _listView.gameObject.SetActive(true);
            if (_createView != null) _createView.gameObject.SetActive(false);
            if (_detail != null) _detail.gameObject.SetActive(false);
            SetJoinLabel(LabelJoin);
            SwapTabArt(0);
            _onCreateTab = false;        // 原版 `ShowJoinAllianceMenu` 末句
            // 原版 `ShowJoinAllianceMenu` → `joinAllianceMenu.Initialize()` → **`FillGroupInvitations()`**
            // （`JoinAllianceMenu__Initialize.c` 的方法体与 `FillGroupInvitations` 逐句相同）。
            // 🔴 **2026-10-27（E9 · 账 410）就地订正（铁律 5）**：本处原来包着 `WithLoading(...)` 且重填的是
            //   **公开联盟列** —— 两处都不对：
            //   ① 判据：`FillGroupInvitations` **没有** `ShowLoading/HideLoading`（那两句只在
            //      `FillOpenAlliances`：`JoinAllianceMenu.cs:1189/1203`，且它的回调首句才 `HideLoading`）
            //      ⇒ 进 Join 页**不转圈**，只有点「搜索」圆钮才转。旧注释「Initialize 的 ShowLoading → …」
            //      是把 `FillOpenAlliances` 那一段读到了 `Initialize` 头上。
            //   ② 判据：`Initialize` 重填的是**邀请列**（`FetchGroupInvitations`），公开列由**搜索钮**
            //      （`JoinAllianceMenu.OnEnable` 挂的 `FillOpenAlliances`）填 ⇒ 我们照做。
            FillInvitations();
        }

        /// <summary>`ShowCreateAllianceMenu` —— 切到 `Create Alliance View`（并**出声**：建盟要服务器）。
        /// 🆕 A55②：原版那一段也**关掉 `allianceView`**（`SetActive(*(param_1 + 0x48), 0)`，
        /// 就在 `SetActive(createAllianceMenu, 1)` 之前）⇒ 我们照做。
        /// <para>🔴 **2026-10-27（E9 · 账 70）**：原版 `ShowCreateAllianceMenu`（`AllianceSearchTab.cs:186-200`）
        /// **不碰 `joinButtonText`** —— 我们从详情态（`joinButtonText` 已被换成 `Back`）切过来时，
        /// 原来那句 `SetJoinLabel(LabelJoin)` 会把它改回 `Join` = **多改了一次**。已删；
        /// 只有 `ShowJoinAllianceMenu`（:210-231）与 `Initialize` 的 Join 支才写那一格。</para></summary>
        public void ShowCreate()
        {
            if (_listView != null) _listView.gameObject.SetActive(false);
            if (_createView != null) _createView.gameObject.SetActive(true);
            if (_detail != null) _detail.gameObject.SetActive(false);
            _onCreateTab = true;         // 原版 `ShowCreateAllianceMenu` 末句（`Initialize` 靠它恢复）
            SwapTabArt(1);
            Say("`Create` 页：原版这一步是向服务器**建一个联盟**（`CreateAllianceMenu` 的 `createButton`）——"
              + "本地没有服务器 ⇒ **表填不了、盟也建不了**，按下去的钮会如实出声。");
        }

        // ---------------------------------------------------------- `GeneralDetails`（未入盟支那一棵，act F）

        /// <summary>🔴 **2026-10-18（第八轮）：这一颗页签的文案走词条**（原来是一个写死的英文字面量）。
        /// <para>键 = 原版那颗 `Generic Tab UI Button Search/Button Text` 的 `Localize.mTerm` 原文
        /// **`SocialMenu/Alliances/Join`**（6 颗同键；本颗父链 = `Button Text &lt; Generic Tab UI Button Search
        /// &lt; Tab buttons &lt; Alliance Header Buttons &lt; AllianceNotMemberVariant &lt; Alliances Tab` ⇒ 与我们同一条）。
        /// 英文列 = TMP 原文 `Join`；中文列「加入」**是我们自拟的**（`zh_CN.csv` 里没有 `Join` 这个英文串）。</para>
        /// <para>🔴 **为什么做成【属性】而不是 `const`**：这一个串**有三个求值点** —— 建窗时
        /// （`BuildHeader` 的 `TabToggle`）与**运行期换字**（`ShowJoin` / `ShowCreate` 各调一次
        /// `SetJoinLabel(LabelJoin)`）。若留 `const`，那两处换字会把**中文档已经画好的中文**改回英文
        /// （静默、只在切页签时现形）⇒ 必须每次现取。⛔ 别改回 `const`。</para></summary>
        static string LabelJoin { get { return Loc.T(LabelJoinTerm); } }
        /// <summary>上面那条词条的**键**（原版 `Localize.mTerm` 原文）。</summary>
        const string LabelJoinTerm = "SocialMenu/Alliances/Join";
        /// <summary>🔴 **详情态里 `Join` 那颗会换一句文案** —— 原版 `HandleDisplayAlliance` 把
        /// `joinButtonText` 设成**另一个词条**（`AllianceSearchTab__HandleDisplayAlliance.c` 里那个
        /// `I2_Loc_LocalizationManager__GetTranslation(DAT_1842bfc18)`，与 `Initialize` /
        /// `ShowJoinAllianceMenu` 用的 `DAT_1842532b0` **不是同一条**）。
        /// <para>🔴🔴 **2026-10-18（第九轮）就地订正（铁律 5）**：这一段原来接着写「**词条在远端本地化表，
        /// 本地一条都取不到** ⇒ 下面这个串是我们挑的兜底」——**前半对、后半不成立**。
        /// **实际是：那两个 `DAT_` 常量本身就是【词条的键名】，本地能读**。判据（可复跑）：
        /// `d:/2/tools/il2cpp_out/stringliteral.json`（26,507 条 `{value, address}`，`address` 是 **RVA**）——
        /// `address 0x42BFC18` → **`MainMenu/MainButtons/ButtonLabel/Back`**（= `0x1842bfc18 − 0x180000000`）·
        /// `address 0x42532B0` → `SocialMenu/Alliances/Join`（同上一条）。错因 = 当年**没查那张表就断定「远端才有」**。
        /// ⇒ 现在**照 `LabelJoin` 同款接上词条**（本表 `本文件` 早就有这条键：中文「返回」= `zh_CN.csv:5`）。
        /// 🔑 **可复用的读法**：`_DAT_xxxxxxxx` 常量**别急着判「远端才有」** —— 先拿
        /// `RVA = 地址 − 0x180000000` 去 `stringliteral.json` 查一次（UI 词条名基本都在里面）。</para>
        /// <para>🔴 **为什么做成【属性】而不是 `const`**：理由同 <see cref="LabelJoin"/> —— 它也在
        /// **运行期**被 `SetJoinLabel` 取一次（`HandleDisplayAlliance`），留 `const` 会把中文档已画好的中文改回英文。
        /// ⛔ 别改回 `const`。</para></summary>
        static string LabelBack { get { return Loc.T(LabelBackTerm); } }
        /// <summary>上面那条词条的**键**（原版 `Localize.mTerm` 原文；判据 = `stringliteral.json` 的 `0x42BFC18`）。</summary>
        const string LabelBackTerm = "MainMenu/MainButtons/ButtonLabel/Back";

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
            _onCreateTab = false;        // 原版 `HandleDisplayAlliance` 末句（判据 :270）
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
            // 🔴 **2026-10-18（P1 §八 #3）**：原版 `EverguildToggle.ToggleSprite` 是**每颗按自己的 `m_IsOn`
            //   各取各的 sprite**（`isOn ? onSprite : offSprite`）。改前我们把**同一张 onSprite** 刷到两颗身上、
            //   只靠染色区分 ⇒ 未选中那颗的底图本来就不对。现在各按各的状态取图；又因为两张 sprite 的
            //   `m_Border` 不同，换图必须**重建九宫格**（`AllianceTabBg.Build`）。
            //   `onTab` 约定：0 = `Join` 亮 · 1 = `Create` 亮 · −1 = 两颗都灭（详情态，见 `ShowTabArtNone`）。
            AllianceTabBg.Build(Page, _joinBg, JoinBtnR, onTab == 0, ArtTabOn, ArtTabOff, TabOnCol, TabOffCol, QOff + L_Btn);
            AllianceTabBg.Build(Page, _createBg, CreateBtnR, onTab == 1, ArtTabOn, ArtTabOff, TabOnCol, TabOffCol, QOff + L_Btn);
        }

        /// <summary>详情态那一档：两颗都灭（`SwapTabArt(-1)`，名字留一个、免得调用处写魔数）。</summary>
        void ShowTabArtNone() { SwapTabArt(-1); }

        // ---------------------------------------------------------- `List View`（`JoinAllianceMenu`）

        void BuildListView()
        {
            _listView = Node(Root, "List View", ListViewR);

            // 搜索框（在右边）：`InputFieldBackground` 九宫 + 占位 `Search` + 那颗圆形搜索钮
            // 🔴 **2026-10-30（执行代理 `B102_ALLY` · 判据 AUD04 第 49 行）**：这一格从
            //   「底图 + 占位字 + **只出声**的命中区」改成**真能输入的输入框**。原版
            //   `JoinAllianceMenu.searchInput` 就是 `TMP_InputField`（`decomp_full/JoinAllianceMenu__FillOpenAlliances.c:29-32`
            //   读它 `+0x210` 的 `m_Text` → `String.Trim()` → 当搜索串交给 `AlliancesManager.GetGroupList`）
            //   ⇒ 我们此前那句 `Say("打不了字")` **是缺口，不是原版行为**。
            //   等价物 = 本仓 `Shell/TextInputField.cs`（与 `AllianceMemberTab` 的描述框**同一件**）。
            //   ⚠️ 这里走它的 **`Bind`（只借键盘与值）**、**不是** `Create`：这一格的底图/占位字/命中区
            //      **本来就是对的**（下面那几句一字未动），缺的只是「值从哪来」⇒ 让 `Create` 重建一遍
            //      会换掉 `InputFieldBackground` 那套版面、还会把 `Placeholder` 挪成孙节点
            //      （`Editor/MainMenuScene.cs` 有三条断言按**直接子件**名找它 ⇒ 会白红）。
            var sf = Node(_listView, "Search Field", SearchFieldR);
            Nine(sf, "InputFieldBackground", SearchFieldR, new Vector4(10f, 10f, 10f, 10f), "Background",
                 L_Panel, new Color(0.0627f, 0f, 0f, 1f));
            // 🔴 **2026-10-08（A213）`wrap: false`**：原版 `List View/Search Field/Text Area/Placeholder`
            //    （文本 `Search`）实读 **`折行=0`**（`字号=50 auto[18~50] 对齐=Left/Midline`，同一条 dump）。
            // 🆕 **2026-10-12（A323）`alignLeft: true` 显式声明**：原版 `Left/Midline`（同一条 dump）——
            //   `SocialWindow.Text` 的 `alignLeft` 缺省**本批已删**（那口变必填）⇒ 逐处现读补齐。
            // 🔴 **2026-10-18（第五轮）：占位符走词条** —— 键 = 原版那颗 `Placeholder` 的
            //   `Localize.mTerm` 原文 **`MenuDeck/HUD/SearchFilter`**（**与卡组线那几颗同一条 mTerm**！
            //   本批按 pid 亲读：全库该 mTerm **11 颗**，其中 **6 颗**在 `SocialMenu/` / `Draft Mode` 族 ——
            //   本颗 = `Alliances Tab > … > List View > Search Field > Text Area > Placeholder`，
            //   另 5 颗是各扇 `No Alliance > Join Alliances Button/Button Text`）。
            //   ⚠️ **同一条 mTerm 挂在两个族的节点上** ⇒ 判据文件原来那句「这两颗属 `SocialMenu/` 族、
            //   **不在** `MenuDeck/HUD/SearchFilter` 那条线上」**是错的**（就地订正，铁律 5）——
            //   这正是「**只给节点名会撞**」那条坑：键是**资产上的 mTerm**，不是按族分的。
            //   英文列 = 那颗 TMP 的 `m_text` 原文 `Search`；中文列 = 「搜索」(`zh_CN.csv:10`)。
            //   ⛔ 节点名 `"Placeholder"` 不动（`Editor/MainMenuScene.cs` 按名找它读折行/字号档）。
            var lbSearch = Text(sf, SearchPhR, Loc.T("MenuDeck/HUD/SearchFilter"),
                 new Color(1f, 1f, 1f, 0.58f), "Placeholder", 50f, L_Text, 18f,
                 wrap: false, alignLeft: true, autoMaxPx: 50f, autoBasePx: 26f);   // A414（表 #2）`auto[18~50] 基准=26.0`
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向档 = 原版那一颗的 `m_VerticalAlignment = 4096 (Midline)`
            //   （判据 = 上面那条 dump 的 `对齐=Left/Midline` —— A255/A323 只落了**横向**那一半）。
            MenuDraw.SetVAlign(lbSearch, Label.VAlign.Midline, SearchPhR);
            // 🔴 **2026-10-30（`B102_ALLY`）**：占位字**原样保留**（上面那两句一字未动 —— 树形、字号、折行、对齐、
            //   词条键都还是原版那一档），**另加一颗正文**（玩家打的字）。两颗都交给 `TextInputField.Bind`
            //   —— 它**只借键盘与值**、不建任何视觉件（所以 `Search Field/Placeholder` 仍然是**直接子件**，
            //   `Editor/MainMenuScene.cs` 那三条按名 + 折行/字号档的断言**不受影响**）。
            //   ⚠️ 正文那颗的字档 = 占位那颗同档（原版输入文字那颗 TMP **本轮没读到**，如实记，⛔ 不编）。
            var lbTyped = Text(sf, SearchPhR, "", Color.white, "Text", 50f, L_Text + 1, 18f,
                 wrap: false, alignLeft: true, autoMaxPx: 50f, autoBasePx: 26f);
            MenuDraw.SetVAlign(lbTyped, Label.VAlign.Midline, SearchPhR);
            _searchInput = TextInputField.Bind(sf,
                new PromptContext.InputData("Search Field", "MenuDeck/HUD/SearchFilter",
                                            TMPro.TMP_InputField.ContentType.Standard, ""),
                lbSearch, lbTyped, 0, _ => RebuildOpenAlliances());
            if (_searchInput == null)
                Debug.LogWarning("[Alliance] `Search Field` 的输入件没绑上（`TextInputField.Bind` 返回 null）"
                               + " —— 如实说：这一格仍然打不了字（⛔ 不假装能输）");
            Hit(sf, "SearchHit", SearchFieldR, L_Hit, () =>
            {
                // 原版这一格是 `TMP_InputField` 自己吃 `IPointerClickHandler` ⇒ 点一下就开始编辑。
                if (_searchInput != null) _searchInput.BeginEdit();
                else Say("`Search Field`：输入件没绑上 ⇒ 这一格打不了字（如实说，不假装能输）。");
            });

            var go = Node(_listView, "Generic Round Button Variant", SearchGoR);
            var goQ = Rect(go, "40k_general_bt_yellow", SearchGoR, "Bg", L_Btn, null, true);
            Rect(go, "40k_icon_search", SearchGoIconR, "Image", L_Art, null, true);
            // ⚠️ 原版这个钮里那个 `Button Text = 'X'`（act **F**）是调试残留 ⇒ 不建。
            // 🆕 A17：原版 `Social Submenu Variant>…>Search Field>Generic Round Button Variant` 是 SpriteSwap（普查 §块 5 第 12 行）
            // 🔴 **2026-10-20（W58·D4）**：原版这一颗的点击 = `JoinAllianceMenu.OnEnable` 挂的
            //   `FillOpenAlliances`（`JoinAllianceMenu__FillOpenAlliances.c:29`）⇒ **`ShowLoading()` → 取数重填 →
            //   回调第一句 `HideLoading()`**（`…b__0.c:30`）。本地同步 ⇒ 同帧收尾、视觉 0 帧（见 `WithLoading`）。
            var goHit = Hit(go, "GoHit", SearchGoR, L_Hit, () =>
            {
                // 🆕 2026-10-27（E9 · 账 408④）：原版 `FillOpenAlliances` 的头尾两句
                // `searchButton.interactable = false` / `= true`（`JoinAllianceMenu.cs:1189/1203`）
                // —— 取数期间这颗钮**不可点**（我们的取数是同步的 ⇒ 同一个调用栈里关掉又开回来）。
                if (_searchBtn != null) _searchBtn.Interactable = false;
                try { WithLoading(RebuildOpenAlliances); }   // 原版 `FillOpenAlliances` 本身也是「清空重填」
                finally { if (_searchBtn != null) _searchBtn.Interactable = true; }
                // 🆕 2026-10-30（`B102_ALLY`）：搜索串现在有来源了 —— 原版 `FillOpenAlliances` 先读它再发请求，
                //   本机把「服务器侧用同一个串筛」与「回调里那道 `Name.Contains`」合并成**本地一道过滤**
                //   （见 `OpenAllianceRows` 的注释）；这里把当前串说出来，免得玩家以为没生效。
                Say("搜索联盟：原版走 `JoinAllianceMenu.searchButton` → `GetGroupList(searchText)` —— **要服务器**；"
                  + "本机按同一套规则**在本地过滤**（`Open Alliances` 现按搜索串 \""
                  + (_searchInput != null ? (_searchInput.Value ?? "").Trim() : "") + "\" 筛过）。");
            }, goQ, "40k_general_bt_yellow");
            _searchBtn = goHit != null ? goHit.GetComponent<WindowButton>() : null;

            // `List Area`（VLG spacing 25）→ `Invitations` / `Open Alliances`
            var area = Node(_listView, "List Area", new PxRect(361.00f, 252.29f, 1874.90f, 1079.77f));

            var inv = Node(area, "Invitations", new PxRect(360.99f, 252.29f, 1874.90f, 252.29f));
            // 🔴 **2026-10-18（N56）**：原来这里**硬编码英文** `"Alliances invitations:"`，而下面
            //   `Open Alliances` 那颗已经走 `Loc.T` ⇒ 中文档里两行语言不同（用户截图 `12_social.png`）。
            //   官方词条 = `SocialMenu/Alliances/Invitations`（EN `Alliances invitations:` / ZH `联盟邀请：`）。
            Text(inv, InvTitleR, Loc.T("SocialMenu/Alliances/Invitations"), Color.white, "Title", 47.5f, L_Text, 18f,
                 wrap: true, alignLeft: true, autoMaxPx: 72f, autoBasePx: 36f);   // A323：原版 `Left/Middle`；A414（表 #3）`auto[18~72] 基准=36.0`
            // ⚠️ 上面这一行以下的 `wrap: true` 全是 **A258**（原版 `折行=1`）⇒ 逐处显式声明。
            //   ⛔ 判据别再抄第二份 —— 逐条真值见文件头那张表 + `…/波C3_A212其余_A213_A214.md` §A213 表 A。
            // ⚠️ `SocialWindow.Text` 的 `wrap` 缺省值**当时（A258）没删成**（阻塞点 = `Editor/MainMenuScene.cs`
            //   一条 7 实参的 `Clip` 探针，那文件不在白名单）⇒ 当时这些显式声明算**提前补全**。
            //   ✅ **2026-10-12（A323）订正**：`wrap` 的缺省已在 **A317** 删掉（`autoMinPx` / `wrap` 现在
            //   **都是必填**）⇒ 今天这些是**必填实参**，不是「提前补全」。
            _invList = Node(inv, "List", InvListR);
            // ⚠️ `Invitations` 这一列**原版不是滚动区**（树里没有 `ScrollRect`/`Mask`，只有 `List` 的 VLG）
            //    ⇒ 这一处 `sc` 传 **null**：不偏移、不裁（照原版）。
            _invNode = inv;
            // 🆕 2026-10-27（E9 · 账 407/410）：改走 `FillInvitations()` —— 它带原版那三件事
            //   （重入守卫 `if (loading) return` · 空表把**整颗 `Invitations` 关掉** · `MemberCount > 0` 过滤）。
            FillInvitations();

            var open = Node(area, "Open Alliances", new PxRect(360.99f, 277.29f, 1874.90f, 1079.77f));
            // 🔴 **2026-10-18（第六轮）：字走 `Loc.T`** —— 键 = 原版那颗 `Title` 的 `Localize.mTerm` 原文
            //   `SocialMenu/Alliances/OpenAlliances`（1 颗；父链 = `Title < Open Alliances < List Area <
            //   List View < AllianceNotMemberVariant < Alliances Tab` ⇒ **与我们这颗同一条**）。
            //   英文列 = TMP 原文 `Open alliances:`（**带冒号**，照抄）；中文列「开放联盟：」**是我们自拟的**（CSV 无此英文串）。
            //   ⛔ 节点名 `"Title"` 与后面那串 wrap/alignLeft/auto* 实参**一个字没动**。
            Text(open, OpenTitleR, Loc.T("SocialMenu/Alliances/OpenAlliances"), Color.white, "Title", 47.5f, L_Text, 18f,
                 wrap: true, alignLeft: true, autoMaxPx: 72f, autoBasePx: 36f);   // A323：原版 `Left/Middle`；A414（表 #4）`auto[18~72] 基准=36.0`
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：这颗 `Viewport` 是**裁切状态的载体**
            //    （原版这上面是 `Image + RectMask2D`）—— 参数取原版实读的全 0
            //    ⇒ 与迁移前的 `SetClip(sc.Viewport)`（同一个 `OpenViewportR`）逐位同值。
            var vp = ViewportClip.Hang(open, "Viewport", OpenViewportR, Vector4.zero, Vector2Int.zero).transform;
            // 🆕 2026-10-03（A25④）：**照原版把滚动区补上**（此前这一格一处滚动都没有 —— 见 `_openScroll` 注释）。
            //   ⚠️ 顺序要紧：**先有滚动区、再谈裁切** —— 只补裁切会把后面的行**藏掉**而不是可滚。
            //   ⚠️ `Owner` 取 `List View`（不是整页）：点 `Create` 键切到建盟表时 `List View` 会被
            //     `SetActive(false)`，这一格必须**一起失去滚轮命中**（`HitScroll` 判的就是 `Owner.activeInHierarchy`）。
            _openScroll = MenuScroll.TopAligned(OpenViewportR, 0f, 1f);   // 内容高在 `BuildRows` 里按条数写；原版 `List View/List Area/Open Alliances` = 1.0（D_N29）
            _openScroll.Owner = _listView.gameObject;
            _openScroll.Elastic = true;                               // 原版 `m_MovementType = 1` = Elastic
            _openScroll.OnChanged = RebuildOpenAlliances;             // 滚轮只改 `Offset`、**画是调用方的事**
            SocialPage.RegisterScroll(_openScroll);                    // 指针层要认识它，滚轮才落得到这一格上
            _openList = Node(vp, "List", OpenListR);
            // 🆕 2026-10-27（E9 · 账 408）：改走 `RebuildOpenAlliances()` —— 过滤（`MemberCount > 0`）与
            //   截断（`Take(200)`）都在它里面（与「清空重填」同一条路，⛔ 别在这里另写一遍条数）。
            RebuildOpenAlliances();
        }

        /// <summary>= 原版 `JoinAllianceMenu.FillGroupInvitations()`（**逐句**，判据
        /// `out_cpp2il_dev_ilspy/JoinAllianceMenu.cs:871-1030` ＋ `JoinAllianceMenu__FillGroupInvitations.c`）：
        /// ① `if (loading) return; loading = true;`（重入守卫）
        /// ② `invitationList.SetActive(false)` + `DestroyAllChildren(invitationHolder)`（清空）
        /// ③ 回调首句 `loading = false`，然后 `invitationList.SetActive(invitations.Count != 0)`
        ///    —— **空表整颗关掉**（账 407①）
        /// ④ 逐条 `predicate = x =&gt; x.group != null &amp;&amp; x.group.MemberCount &gt; 0` 过滤后再建行（账 407②）
        /// <para>⚠️ 原版那两个表是**异步**的（`AlliancesManager.FetchGroupInvitations(cb)`），我们是同步的
        /// ⇒ ②③④ 落在**同一个调用栈**里、视觉 0 帧（与 `WithLoading` 那条口径同族）。</para></summary>
        void FillInvitations()
        {
            if (_loading) return;        // ① 重入守卫
            _loading = true;
            try
            {
                if (_invNode != null) _invNode.gameObject.SetActive(false);      // ②
                if (_invList != null)
                    for (int i = _invList.childCount - 1; i >= 0; i--)
                        SocialWindow.DestroySafe(_invList.GetChild(i).gameObject);

                // ③+④ 过滤（原版 `_003CFillGroupInvitations_003Eb__22_1`）
                var src = SocialData.Invitations;
                int n = 0;
                for (int i = 0; i < src.Count; i++)
                    if (src[i] != null && src[i].Members > 0) n++;     // `group != null && MemberCount > 0`
                if (_invNode != null) _invNode.gameObject.SetActive(n != 0);

                // 行按 `InvRowGap`（= 10）从 `InvListR` 顶边往下排 —— 与 `BuildRows` 同一套算式
                // （这一列**不是**滚动区 ⇒ 不偏移、不裁，`sc` 那一支整段不适用）。
                int k = 0;
                for (int i = 0; i < src.Count; i++)
                {
                    var m = src[i];
                    if (m == null || m.Members <= 0) continue;
                    float y = InvListR.y1 + k * (RowH + InvRowGap);
                    BuildInvitationRow(_invList, new PxRect(InvListR.x1, y, InvListR.x2, y + RowH), m);
                    k++;
                }
            }
            finally { _loading = false; }    // 原版：回调首句 `loading = false`（这里同步 ⇒ finally）
        }

        /// <summary>滚轮改了偏移 ⇒ 重画公开联盟那一列（**先清再建**，回调会重入 —— 同 `ForgeTab.BuildRewardCells` 那条）。
        /// 数据变了也走它（原版 `FillOpenAlliances` 就是「清空重填」）。**自检的 `RebuildForTest` 也走它** —— 同一条路。
        /// <para>🆕 2026-10-27（E9 · 账 408）：补上原版 `FillOpenAlliances` 的两道**数据侧**判定（判据
        /// `out_cpp2il_dev_ilspy/JoinAllianceMenu.cs:1196-1235`）：
        /// ① `Where(a =&gt; a != null &amp;&amp; a.MemberCount &gt; 0)`（`_003CFillOpenAlliances_003Eb__23_1`）；
        /// ② `Take(200)`（:860）。③ 搜索串过滤 `name.Contains(searchText)` —— 🔴 **2026-10-30（`B102_ALLY`）起真的生效**：
        /// 搜索框已经是 `TextInputField`（见 `BuildListView`），`OpenAllianceRows()` 读它的 `Value`（先 `Trim`、
        /// 再 `OrdinalIgnoreCase` 比较，判据见那个方法的注释）。
        /// ⚠️ 本行原写「本地没有输入系统 ⇒ 搜索串恒空（诚实记着）」—— **已过期**，就地订正（铁律 5）。</para></summary>
        void RebuildOpenAlliances()
        {
            var rows = OpenAllianceRows();
            BuildRows(_openList, OpenListR, rows.Count,
                      (l, r, i) => BuildAllianceListRow(l, r, rows[i]), _openScroll, OpenRowGap);
        }

        /// <summary>原版 `FillOpenAlliances` 过滤 + 截断之后的**那一份**（见 `RebuildOpenAlliances` 的 doc）。
        /// `const 200` = `Enumerable.Take(source, 200)` 的字面量（`JoinAllianceMenu.cs:860`）。</summary>
        System.Collections.Generic.List<SocialData.AllianceListing> OpenAllianceRows()
        {
            // 🔴 **2026-10-30（执行代理 `B102_ALLY`）**：**搜索串真的进来了**（此前 `searchInput` 恒空 ⇒ 等价于不过滤）。
            //   原版 `FillOpenAlliances` 的那条链逐句照抄（`decomp_full/`，四份产物）：
            //     ① 取数前 `searchText = searchInput.text.Trim()`（`JoinAllianceMenu__FillOpenAlliances.c:29-32`：
            //        读 `searchInput(+0x48)` 的 `+0x210`，再 `System_String__Trim`）；
            //     ② 回调里 `Where(a => a != null && a.MemberCount > 0)`（`JoinAllianceMenu.__c___FillOpenAlliances_b__23_1.c`）；
            //     ③ **仅当 `searchText.Length > 0`** 再 `Where(a => a.Name.Contains(searchText, …))`
            //        （`…__DisplayClass23_0___FillOpenAlliances_b__0.c` 里那句 `if (0 < *(int*)(searchText + 0x10))`
            //         + `…b__2.c` 的 `String.Contains(group.name(+0x18), searchInput.text(+0x210), 5)`；
            //         **`5` = `StringComparison.OrdinalIgnoreCase`** ⇒ **大小写不敏感**）；
            //     ④ `Take(200)`（`JoinAllianceMenu.cs:860` 那个字面量）。
            //   ⚠️ 原版 ③ 之前服务器已经用同一个串筛过一遍（`AlliancesManager.GetGroupList(searchText, cb)`）——
            //      本机没有服务器 ⇒ **本地这一道 `Where` 就是那两道的全部**（可观测结果一致）；
            //      ⛔ 不假装发过请求、也不把「服务器侧匹配」编成别的规则。
            const int MaxOpen = 200;
            string q = _searchInput != null ? (_searchInput.Value ?? "").Trim() : "";   // ①
            var src = SocialData.OpenAlliances;
            var rows = new System.Collections.Generic.List<SocialData.AllianceListing>(src.Count);
            for (int i = 0; i < src.Count; i++)
            {
                if (rows.Count >= MaxOpen) break;                    // ④
                var m = src[i];
                if (m == null || m.Members <= 0) continue;            // ②
                if (q.Length > 0 && (m.Name == null                        // ③
                                     || m.Name.IndexOf(q, System.StringComparison.OrdinalIgnoreCase) < 0))
                    continue;
                rows.Add(m);
            }
            return rows;
        }

        /// <summary>自检用：喂了数据之后重画（= 原版 `FillOpenAlliances` 那条路）。**只给自检**。</summary>
        public void RebuildForTest() { RebuildOpenAlliances(); }

        /// <summary>逐行建（原版 `FillGroupInvitations` / `FillOpenAlliances` 会**清空重填**）。
        /// 行高恒 110、行距**逐列给**（`rowGap`）—— 因为原版两款行族**不是同一个列表实现**：
        /// `Invitations>List` 是普通 VLG（`spacing=10`）⇒ 步进 120；`Open Alliances` 是 `RecyclableScrollRect`
        /// （`_spacingY 5`）⇒ 步进 **115**（判据见 `InvRowGap` / `OpenRowGap` 两处注释）。
        /// 从列表顶边往下排 —— 表里那两个邀请行实例的 y 正是这么摆的（312.29→422.29、432.29→542.29）。
        /// 🆕 2026-10-03（A25④）：`sc != null` 时行按**滚动偏移之后**的位置摆（`MenuScroll.Shift`）、
        /// 整行滚出视口的**不建**；裁切长在 `Open Alliances/Viewport` 那颗 `ViewportClip` 上
        /// （A435·丙 起 —— 原来那句「画之前把 `SetClip(视口)` 设上、画完清掉」已作废）。
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
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：原来这里是 `SetClip(sc.Viewport)` / 末尾 `SetClip(null)`
            //    那一对 —— **删掉了**：裁切状态长在 `BuildTabsContents` 建的那颗 `Open Alliances/Viewport`
            //    的 `ViewportClip` 上（`SocialPage.*` 沿父链取它）。
            //    ⛔ 留着 = `SocialPage.Clip` 非空 ⇒ 形参赢 ⇒ 节点一像素都不生效（静默）。
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
                    // 🔴 **2026-10-13（A776 · δ 族）**：`sc.Viewport` 这一份自持矩形**不再喂给粗筛**
                    //    （那是第二状态源：节点搬了它不跟，白建 / 漏建几行而**不出声**），改沿父链取
                    //    `Open Alliances/Viewport` 那颗 `ViewportClip`。等价性（本处逐处算过）：
                    //    `Hang(open, "Viewport", OpenViewportR, …)` 与 `MenuScroll.TopAligned(OpenViewportR, 0f)`
                    //    ⇒ `sc.Viewport == OpenViewportR == 节点框`（只差一趟 float32 往返 ~1e-4px ≪ 0.05px）。
                    //    ⚠️ 节点 `padding = zero` ⇒ `RenderClip == ClipPx`。
                    //    ⚠️ `Invitations` 那一列 `sc` 传 `null`（原版不是滚动区）⇒ 整段不进这条路，逐位不变。
                    if (!MenuDraw.VisibleAbove(list, r, null)) continue;
                }
                build(list, r, i);      // `i` 仍然是**数据下标**（不是「第几个建出来的」）—— 行内容取的是 `[i]`
            }
        }

        // ---------------------------------------------------------- 行模板一：`AllianceInvitationEntry`

        /// <summary>邀请行（原版 `AllianceInvitationEntry`，§A·2·1 的独立根 `-5586301021970909275`）。
        /// 尾部两个钮的文案是 **`Join` + `Dismiss`**（节点名却叫 `Reject`）。</summary>
        void BuildInvitationRow(Transform list, PxRect r, SocialData.Invitation m)
        {
            var row = Node(list, "Invitation List Entry", r);
            RowBase(row, r, "background");
            var bdr = new PxRect(r.x1 + 18.80f, r.y1 + 10.00f, r.x1 + 108.80f, r.y1 + 100.00f);
            var bd = Node(row, "BadgeDrawer", bdr);
            // 🔴 **2026-10-28（B88-B7 · 账 14①）**：`Frame` / `Badge` 两层**画出来了**（此前是两个空节点）。
            //   判据 = `AllianceInvitationEntry__Initialize.c:82-84`：
            //     `badgeDrawer.Draw(Group__get_Badge(alliance))` ⇒ 先按 `GroupBadge.tierLevel` 取档位框
            //     （`Math.Max(0, tier-1)` → `Alliance_Trophies_TIER1..4`），再画徽章（`AllianceBadge.uniqueId` → sprite）。
            //   id→图 映射 = `AllianceBadgeArt`（16 份 SO 全解出，判据 `B88_R8_安卓包徽章挖掘.md` §二）；
            //   行数据没有 id 时走**原版同一条回落**（`DefaultTrophy` = `Default_Badge`），⛔ 不是我们编的图。
            if (AllianceBadgeArt.DrawTierFrame(bd, bdr, m.BadgeTier, "Frame", Page.Q + QOff + L_Art) == null)
                Node(bd, "Frame", bdr);
            if (AllianceBadgeArt.Draw(bd, bdr, m.BadgeId, "Badge", Page.Q + QOff + L_Art + 1) == null)
                Node(bd, "Badge", bdr);
            RowTexts(row, r, m.Name, m.Region, m.Members, m.MemberMax, m.Rating, 4.50f);
            // 两个钮（我方 `+393.8` / 邀请方 `+603.8`，都 200×57）
            // 🔴 **2026-10-18（第八轮）**：这颗钮的**文案**走词条（与上面那颗页签**同一条键**
            //   `SocialMenu/Alliances/Join` —— 6 颗里就含列表行这两颗，父链 = `Button Text < Join <
            //   Invitation List Entry < …`）。⛔ 第 3 实参那个**节点名 `"Join"` 不动**。
            // 🔴 **2026-10-27（E9 · 账 14②）**：`joinButton.interactable = MemberCount < config.maxMemberCount`
            //    （判据 `AllianceInvitationEntry__Initialize.c` 的 `Selectable__set_interactable(lVar3, iVar4 < *(int*)(lVar5+0x10))`；
            //    `lVar5` = `ConfigManager.GetConfig<AlliancesConfig>()`、`+0x10` 就是 `maxMemberCount`）。
            //    ⚠️ 行上的 `MemberMax` 印的就是那个 `maxMemberCount`（`memberCountText = "{MemberCount}/{max}"`）
            //    ⇒ 用行上的两份数据比即可，⛔ 不另引一份配置。
            RowButton(row, r, 1046.30f, 24.46f, "Join", Loc.T("SocialMenu/Alliances/Join"), () => Say(
                "`Join`（接受邀请）：要**服务器**（原版 `AllianceInvitationEntry.HandleJoin`）。"),
                interactable: m.Members < m.MemberMax);
            // 🔴 **2026-10-18（第十轮 · B 类）**：这颗钮的**文案**走词条（只换第 4 实参）——
            //   键 = 原版那颗 `Reject/Button Text` 的 `Localize.mTerm` 原文 `SocialMenu/Alliances/Dismiss`
            //   （3 颗同键；父链 = `Button Text < Reject < Invitation List Entry < List < Invitations < List Area
            //   < List View < AllianceNotMemberVariant < Alliances Tab` ⇒ **节点名 `Reject` 与我们逐字相同**、
            //   连它「节点名 `Reject` / 文案 `Dismiss`」这个错位都对得上）。
            //   英文列 = TMP 原文 `Dismiss`；中文列「拒绝」**我们自拟**（`zh_CN.csv` 没有 `Dismiss`）。
            //   ⛔ 节点名 `"Reject"` 与 x/y 那两个实参没动。⚠️ 建行时求值。
            RowButton(row, r, 1256.30f, 23.70f, "Reject", Loc.T("SocialMenu/Alliances/Dismiss"), () => Say(
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
        void BuildAllianceListRow(Transform list, PxRect r, SocialData.AllianceListing m)
        {
            var row = Node(list, "Entry", r);
            RowBase(row, r, "background");
            var bdr = new PxRect(r.x1 + 18.80f, r.y1 + 10.00f, r.x1 + 108.80f, r.y1 + 100.00f);
            var bd = Node(row, "BadgeDrawer", bdr);
            // 🔴 **2026-10-28（B88-B7 · 账 21①）**：同邀请行 —— 两层画出来。判据 =
            //   `AllianceListEntry__Initialize.c:80-81`：`badgeDrawer.Draw(Group__get_Badge(group))`。
            //   （映射与回落见 `AllianceBadgeArt`。）
            if (AllianceBadgeArt.DrawTierFrame(bd, bdr, m.BadgeTier, "Frame", Page.Q + QOff + L_Art) == null)
                Node(bd, "Frame", bdr);
            if (AllianceBadgeArt.Draw(bd, bdr, m.BadgeId, "Badge", Page.Q + QOff + L_Art + 1) == null)
                Node(bd, "Badge", bdr);
            RowTexts(row, r, m.Name, m.Region, m.Members, m.MemberMax, m.Rating, 4.50f);
            // 🔴 **2026-10-18（第八轮）**：同上（与页签那颗 `Join` **同一条键**；父链 = `Button Text < Generic UI Button
            //   < Entry < List < Viewport < Open Alliances < …`）。⛔ 节点名 `"Generic UI Button"` 不动。
            // 🔴 **2026-10-27（E9 · 账 21）**：`AllianceListEntry.Initialize`（`out_cpp2il_dev_ilspy/AllianceListEntry.cs:224-300`）
            //   那两句：
            //   ① `joinButton.interactable = (privacy == GroupPrivacy.Open) && MemberCount < maxMemberCount`
            //      —— 非 Open 直接 false；Open 时再比人数（`GroupPrivacy`：`Open = 0 / Private = 1` 实读）。
            //   ② `privacy == Private` ⇒ `joinButton.text.text = GetTranslation("SocialMenu/Alliances/Privacy/Private")`
            //      —— **这颗钮的字被换成「Private」**（不是仍然印 `Join`）。词条已在官方表里（现读命中）。
            bool openForJoin = m.Privacy == 0 && m.Members < m.MemberMax;
            string joinText = m.Privacy == 1 ? Loc.T("SocialMenu/Alliances/Privacy/Private")
                                             : Loc.T("SocialMenu/Alliances/Join");
            RowButton(row, r, 1188.00f, 21.18f, "Generic UI Button", joinText, () => Say(
                "`Join`（加入这个联盟）：要**服务器**（原版 `AllianceListEntry.TryJoin`）。"),
                interactable: openForJoin);
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
            var nameR = new PxRect(r.x1 + 133.55f, r.y1 + 4.50f, r.x1 + 639.03f, r.y1 + 57.50f);
            var regionR = new PxRect(r.x1 + 133.55f, r.y1 + 58.36f, r.x1 + 639.03f, r.y1 + 102.94f);
            // 🔴 **2026-10-27（E9 · 顺手发现，铁律 14 当场消化）**：原版两族行的 `regionText` **不是原样印**，
            //   走的是词条 `"SocialMenu/Alliances/Language/" + region`（判据：
            //   `AllianceInvitationEntry.Initialize` 的 `System_String__Concat(DAT_1842533b0, *(…+0x48))` +
            //   `I2_Loc_LocalizationManager__GetTermTranslation`；`AllianceListEntry.Initialize` 同一句，
            //   `out_cpp2il_dev_ilspy/AllianceListEntry.cs:238-245`）。词根那份表 = `SocialData.GroupLanguages`
            //   （14 个值的枚举名）。官方表里 `Language/{Global,English,…,Turkish}` **14 条全在**（现读命中）
            //   ⇒ 数据侧照原版填枚举名（`"Global"` / `"English"` …）时显示的就是译文。
            //   ⚠️ 空串**不拼键**（原版 `group.region` 不会空；拼了会印出键名 = 静默丑）。
            string regionText = string.IsNullOrEmpty(region)
                ? "" : Loc.T("SocialMenu/Alliances/Language/" + region);
            var ratingR = new PxRect(r.x1 + 941.86f, r.y1 + 53.00f, r.x1 + 1024.55f, r.y1 + 106.90f);
            var lbName = Text(row, nameR, name ?? "",
                 Color.white, "Title", 55.9f, L_Text, 18f, wrap: true, alignLeft: true,
                 autoMaxPx: 72f, autoBasePx: 36f);   // A323：原版 `Left/Midline`；A414（表 #5）`auto[18~72] 基准=36.0`
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版 `m_VerticalAlignment = 4096 (Midline)`
            //   （判据 = 上面那条 dump 的 `对齐=Left/Midline`；A323 只落了**横向**那一半）。
            MenuDraw.SetVAlign(lbName, Label.VAlign.Midline, nameR);
            var lbRegion = Text(row, regionR, regionText,
                 new Color(0.906f, 0.906f, 0.906f, 1f), "Region", 47.05f, L_Text, 18f, wrap: true, alignLeft: true,
                 autoMaxPx: 72f, autoBasePx: 36f);   // A323：原版 `Left/Midline`；A414（表 #6）`auto[18~72] 基准=36.0`
            MenuDraw.SetVAlign(lbRegion, Label.VAlign.Midline, regionR);
            // 🆕 **A255**：下面这**三段**（`Members Header` / `Member Count` / `Ranking Header`）原版都是
            //   **`Center/Middle`**（判据见文件头那张表 —— 同一格里的 `Title`/`Region`/`Ranking Value` 是 `Left`，
            //   ⛔ 别一刀切）⇒ 各自显式传 `alignLeft: false`。
            // 🆕 **A258**：本段三处原版 `折行=1` ⇒ `wrap: true` 显式声明（缺省值**本批没删成**，
            //   阻塞点见上面那条；这三行是提前补全）。
            // 🔴 **2026-10-18（第六轮）认过树才接的**（`Text(row, …, "Members:", …, **"Members Header"**, …)`）：
            //   键 = 原版那颗 `Members Header` 的 `Localize.mTerm` 原文 `SocialMenu/Alliances/Members`（3 颗）。
            //   **配对判据（三条一起）**：① 我们的**节点名就是 `"Members Header"`**；② 本文件头那张 A414 表
            //   （`本文件`）早就把这一格标成 `…/{Invitation List Entry,Entry}/Members Header` —— 与原版那颗的
            //   父链 `Members Header < {Entry | Invitation List Entry | Alliance List Entry}` **逐节同名**；
            //   ③ 原版两列行族（`Invitations` / `Open Alliances`）**共用这一条键**，我们也一样（同一条键覆盖两列）。
            //   英文列 = TMP 原文 `Members:`（**带冒号**）；中文列 = `zh_CN.csv:273`「成员」+ 全角冒号。
            //   ⛔ 节点名与那串 alignLeft/wrap/auto* 实参没动。
            Text(row, new PxRect(r.x1 + 639.04f, r.y1 + 12.82f + dy, r.x1 + 816.14f, r.y1 + 49.18f + dy),
                 Loc.T("SocialMenu/Alliances/Members"),
                 Color.white, "Members Header", 38.35f, L_Text, 18f, alignLeft: false, wrap: true,
                 autoMaxPx: 72f, autoBasePx: 36f);   // A414（表 #7）原版 `auto[18~72] 基准=36.0`
            Text(row, new PxRect(r.x1 + 639.04f, r.y1 + 55.31f + dy, r.x1 + 816.15f, r.y1 + 107.70f + dy),
                 members + "/" + max, Color.white, "Member Count", 50f, L_Text, 18f, alignLeft: false, wrap: true,
                 autoMaxPx: 50f, autoBasePx: 36f);   // A414（表 #8）原版 `auto[18~50] 基准=36.0`
            // 🔴 **2026-10-18（第六轮）**：同上（节点 = `"Ranking Header"`，判据表 = 本文件头 `本文件`）——
            //   键 = `SocialMenu/Alliances/Ranking`（4 颗）。英文列 = TMP 原文 `Ranking:`（带冒号）；
            //   中文列「排名：」**是我们自拟的**（`zh_CN.csv` 里没有 `Ranking` 这个英文串）。
            Text(row, new PxRect(r.x1 + 847.45f, r.y1 + 12.82f + dy, r.x1 + 1024.55f, r.y1 + 49.18f + dy),
                 Loc.T("SocialMenu/Alliances/Ranking"),
                 Color.white, "Ranking Header", 38.35f, L_Text, 18f, alignLeft: false, wrap: true,
                 autoMaxPx: 72f, autoBasePx: 36f);   // A414（表 #9）原版 `auto[18~72] 基准=36.0`
            // `Ranking`：图标（段位）+ 数值。原版是 `HorizontalLayoutGroup` 排的（图标 53.6/55.4 见方）
            Rect(row, "40k_UI_icon_ranked_Skirmish",
                 new PxRect(r.x1 + 888.23f, r.y1 + 51.50f, r.x1 + 941.86f, r.y1 + 106.90f),
                 "Icon", L_Art, null, true);
            var lbRating = Text(row, ratingR,
                 rating ?? "", Color.white, "Ranking Value", 50f, L_Text, 18f, wrap: true, alignLeft: true,
                 autoMaxPx: 50f, autoBasePx: 36f);   // A323：原版 `Left/Midline`；A414（表 #10）`auto[18~50] 基准=36.0`
            MenuDraw.SetVAlign(lbRating, Label.VAlign.Midline, ratingR);   // A712 阶段 2：原版 `Left/Midline` 的纵向那一半
        }

        /// <summary>行尾那颗钮（`40K_button` 489×107 · 九宫 (234,46,234,46) · preserveAspect）。
        /// <paramref name="interactable"/> = 原版 `Selectable.interactable`（两款行族各自的判据见两个调用点）。
        /// 🔴 **必须排在 `BindNine` 之后**（`WindowButton.Interactable` 的 doc：`Bind` 之前调会缓存一个空表 ⇒
        /// 这颗钮以后永远不变色，静默）。</summary>
        void RowButton(Transform row, PxRect r, float x1, float y1, string name, string text, System.Action onClick,
                       bool interactable = true)
        {
            var br = new PxRect(x1, r.y1 + y1, x1 + 200f, r.y1 + y1 + 57f);
            var n = Node(row, name, br);
            var bgNine = Nine(n, "40K_button", br, new Vector4(234f, 46f, 234f, 46f), "Bg", L_Btn);
            // 🔴 **2026-10-08（A213）`wrap: false`**：原版行尾那颗 `Join` / `Reject` 的
            //    `Invitation List Entry … Button Text` 实读 **`折行=0`**（`字号=36.65 / 44 auto[12~44]`，
            //    同一条 dump；公开联盟行那颗是 44）—— 走 `SocialWindow.Text` 会**默认折行**。
            var btnR = new PxRect(br.x1 + 13f, br.y1, br.x2 - 13f, br.y2);
            var lbRowBtn = Text(n, btnR, text, Color.white, "Button Text",
                 36.65f, L_Text, 12f, alignLeft: false, wrap: false,
                 autoMaxPx: 44f, autoBasePx: 12f);
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向档 = 原版 `m_VerticalAlignment = 4096 (Midline)`。
            MenuDraw.SetVAlign(lbRowBtn, Label.VAlign.Midline, btnR);
            // ☝ A414（表 #11）：原版 `…/{Invitation List Entry/Join,Reject,Entry/Generic UI Button}/Button Text`
            //   = `auto[12.0~44.0] 基准=12.0`（`字号` 邀请行 36.65 / 公开行 44）
            //   —— ⚠️ 上限 **44 ≠ 36.65** 正是这一批要治的「短文案永远画小一档」
            // 🆕 A255：原版 `Center/Midline`
            // 🆕 A17：`Invitation List Entry>Invitations>List>…>Join/Reject` 是 SpriteSwap（普查 §块 5 第 13 行）
            // —— 底图是**九宫格** ⇒ 九张一起换
            var h = Hit(n, "Hit", br, L_Hit, onClick);
            var wb = h != null ? h.GetComponent<WindowButton>() : null;
            if (wb != null)
            {
                wb.BindNine(bgNine, "40K_button");
                if (!interactable) wb.Interactable = false;   // ⛔ 必须在 BindNine 之后（见 doc）
            }
        }

        // ---------------------------------------------------------- `Create Alliance View`（`CreateAllianceMenu`）

        /// <summary>建盟表（原版 `CreateAllianceMenu`，**act F**）。⚠️ **五项操作全要服务器**：
        /// 名字/描述（要打字）、语言/隐私（两个 `TMP_Dropdown`）、`Continue`（花 1000 水晶建盟）
        /// ⇒ 我们把**看得见的版面照建**，点下去一律出声。
        /// 两个下拉的 `Template` 是 **Unity 内置模板**（`Item Label = 'Option A'`）⇒ **不建**（§B·13）。</summary>
        void BuildCreateView()
        {
            _createView = Node(Root, "Create Alliance View", CreateViewR);

            // 🔴 **2026-10-18（第八轮）**：建盟页这两行标题的**文案**走词条（只换「显示文案」那一个实参）——
            //   `Name input title` → `SocialMenu/Alliances/NameInput`、`Desc input title` → `…/DescriptionInput`
            //   （各 1 颗；父链 = `{Name,Desc} input title < TopAnchor < Create Alliance View <
            //   AllianceNotMemberVariant < Alliances Tab` ⇒ **节点名与我们逐字相同**）。
            //   英文列 = 原版 TMP 原文（`Alliance Name` / `Alliance Description`）；中文列**我们自拟**
            //   （`zh_CN.csv` 里两条英文串都没有）。⛔ 第 2 实参（节点名）与那串字号/auto*/对齐实参没动。
            _nameInput = Field(_createView, "Name input title", Loc.T("SocialMenu/Alliances/NameInput"),
                  new PxRect(432.47f, 257.65f, 1125.42f, 307.65f),
                  new PxRect(432.47f, 307.95f, 1332.47f, 367.35f), "Name Input");
            _descInput = Field(_createView, "Desc input title", Loc.T("SocialMenu/Alliances/DescriptionInput"),
                  new PxRect(432.48f, 402.99f, 1125.43f, 452.99f),
                  new PxRect(432.48f, 454.55f, 1332.48f, 659.00f), "Desc Input");

            // `Create Alliance Text`（标题）+ `Price Display Button`（`Continue` + 1000 水晶）
            // 🔴 **2026-10-18（第六轮）：字走 `Loc.T`** —— 键 = 原版那颗 `Create Alliance Text` 的
            //   `Localize.mTerm` 原文 `SocialMenu/Alliances/CreateAlliance`（1 颗；父链 =
            //   `Create Alliance Text < TopAnchor < Create Alliance View < AllianceNotMemberVariant < Alliances Tab`
            //   ⇒ **节点名与我们这颗逐字相同**）。英文列 = TMP 原文 `Create alliance`；
            //   中文列「创建联盟」**是我们自拟的**（⚠️ `zh_CN.csv:229` 那条是 `Create Alliance (1000 Gold)`，**另一个串**）。
            //   ⛔ 节点名 `"Create Alliance Text"` 与那串实参没动。
            Text(_createView, new PxRect(428.10f, 666.82f, 678.10f, 723.60f), Loc.T("SocialMenu/Alliances/CreateAlliance"),
                 Color.white, "Create Alliance Text", 40f, L_Text, 18f, wrap: true, alignLeft: true,
                 autoMaxPx: 40f, autoBasePx: 36f);   // A323：原版 `Left/Middle`；A414（表 #12）`auto[18~40] 基准=36.0`
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
            // 🔴 **2026-10-10（A259）**：这个矩形原来是**内联字面量**（同文件别处都抽了名字，就它没抽）
            //   ⇒ 就地抽成同段的局部名（与上面那颗 `price` 同一族）。**零行为变化**。
            var priceIcon = new PxRect(495.15f, 730.73f, 542.06f, 777.65f);   // `Price Display > icon`（水晶）
            Rect(pb, "40k_general_icon_currency_crystal", priceIcon, "icon", L_Art, null, true);
            var priceTextR = new PxRect(546.75f, 730.73f, 613.81f, 777.65f);
            var lbPrice = Text(pb, priceTextR, "1000", Color.white, "text", 40f, L_Text, 13.46f,
                 alignLeft: false, wrap: false, autoMaxPx: 40f, autoBasePx: 39f);
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版 `m_VerticalAlignment = 8192 (Capline)`
            //   （判据 = 上面 `Center/Capline` 那一条 dump）。
            MenuDraw.SetVAlign(lbPrice, Label.VAlign.Capline, priceTextR);
            // ☝ A414（表 #13）：原版 `…/Price Display Button/…/Price Display/text`（`'1000'`）
            //   = `auto[13.46~40.0]` **`基准=39.0`** —— ⚠️ base **不是 36**，别按同族一刀切（铁律 5·c）
            // 🆕 A255：原版 `Center/Capline`（判据见文件头那张表）
            // 🔴 **2026-10-08（A213 · 本件点名的判例）`wrap: false`**：原版
            //    `Create Alliance View/…/Price Display Button/Generic UI Button/Price Display/text`（文本 `1000`）
            //    实读 **`折行=0 · auto[13.46~40] · Center/Capline`**（判据 = `python 工具/menu_dump.py
            //    bundle_menus_assets_all "Social Submenu Variant" --depth 16 --md`）⇒ 走 `SocialWindow.Text`
            //    原来**恒折行** = **真偏离**（A213 普查 §七·C2 顺手读到的真值，本批落地）。
            // 🆕 A17：原版 `Social Submenu Variant>…Create Alliance Text>…` 是 SpriteSwap（普查 §块 5 第 15 行）
            var pbH = Hit(pb, "Hit", price, L_Hit, () =>
            {
                // 🔴 **2026-10-30（执行代理 `B102_ALLY`）**：**Name / Desc 两格的输入真的被消费了** ——
                //   原版这一跳把这两格（连语言 / 隐私两个下拉）组进建盟请求发给服务器（`CreateAllianceMenu`
                //   的 continue 回调；`Group` 是在**服务端**建出来的）。
                //   本机**没有服务器**（原版这条调用链断在 `AlliancesManager` 那一跳 —— 也是 P3「玩家账户/云后端」
                //   那一类）⇒ 照 P2 口径「UI 照原版做、数据本地模拟或**出声**」：
                //   **把请求参数原样收下 + 明确说出去没发**（先例 = `AllianceMemberTab` 的 `Confirm button`）。
                //   ⛔ **不造一条本地「建盟」假链**（那会让玩家以为盟真的建了、列表里凭空多一个盟）。
                string nm = _nameInput != null ? _nameInput.Value : null;
                string ds = _descInput != null ? _descInput.Value : null;
                LastCreateName = nm; LastCreateDesc = ds;
                Debug.Log("[Alliance] `Continue`（建盟请求的参数已收下）：Name = \"" + (nm ?? "") + "\" · Desc = \""
                        + (ds ?? "") + "\"（描述长度 " + (ds != null ? ds.Length : 0) + "）"
                        + " —— 原版把这两格 + 语言/隐私发给服务器；**本机没有服务器** ⇒ 不发请求、不落任何本地状态。");
                Say("`Continue`（花 1000 建盟）：要**服务器** —— 本地没有联盟系统，资源也花不掉。"
                  + "你输入的名字/描述**已经收到**（见日志），但**不会发出去**。");
            });
            var pbWb = pbH != null ? pbH.GetComponent<WindowButton>() : null;
            if (pbWb != null) pbWb.BindNine(pbNine, "40K_button");

            // 语言 / 隐私两个下拉（只建「合上的那一面」：底图 + 空 Label + 箭头）
            // 🔴 **2026-10-18（第十轮 · B 类）**：两个标题的**文案**走词条（只换 `title` 那一个实参）——
            //   键 = 原版那两颗 `Localize.mTerm` 的原文 `SocialMenu/Alliances/SelectLanguage` /
            //   `…/SelectPrivacy`（各 1 颗；父链 = `{Select Language,Select Privacy} < TopAnchor <
            //   Create Alliance View < AllianceNotMemberVariant < Alliances Tab` ⇒ **节点名与我们逐字相同**）。
            //   ⚠️ 与本表 `MainMenu/Settings/ButtonLabel/SelectLanguage` **是两条不同的键**（原版设置窗用那条、
            //   建盟页用这两条）⇒ ⛔ 别因为中文一样就合并。英文列 = TMP 原文（`Select language` / `Select privacy`，
            //   **小写 l/p**，照抄）；中文列「选择语言」/「选择隐私」**我们自拟**（`zh_CN.csv` 里两条英文串都没有）。
            //   ⛔ 节点名（`"Select Language"` / `"Select Privacy"`）与其余实参没动。
            //   ⚠️ 两处都在 `BuildCreateView()`（**建窗时**求值，不是 static 初始化）⇒ 与其它窗同口径。
            Dropdown(_createView, "Select Language", Loc.T("SocialMenu/Alliances/SelectLanguage"),
                     new PxRect(1440.43f, 250.87f, 1690.43f, 307.65f),
                     new PxRect(1440.43f, 307.64f, 1690.43f, 367.04f), "LanguagesDropdown",
                     SocialData.LanguageOptionLabels());
            Dropdown(_createView, "Select Privacy", Loc.T("SocialMenu/Alliances/SelectPrivacy"),
                     new PxRect(1440.43f, 397.50f, 1690.43f, 454.28f),
                     new PxRect(1440.43f, 454.28f, 1690.43f, 513.68f), "Privacy Dropdown",
                     SocialData.PrivacyOptionLabels());
        }

        /// <summary>一个「标题 + 输入框」组（建盟页那两组）。输入框是 `40K_dropdown_bg` 九宫。
        /// 🔴 **2026-10-30（`B102_ALLY`）**：原来这里只建底图 + `Say("打不了字")` —— **已改成真能输入**
        /// （原版这两颗是 `TMP_InputField`；本仓等价物 = `Shell/TextInputField.cs`）。
        /// 返回值 = 那颗输入框（`BuildCreateView` 把它存进 `_nameInput` / `_descInput`，供 `Continue` 消费 + 自检读）。</summary>
        TextInputField Field(Transform parent, string titleName, string title, PxRect titleR, PxRect boxR, string boxName)
        {
            var lbTitle = Text(parent, titleR, title, Color.white, titleName, 40f, L_Text, 18f, wrap: true, alignLeft: true,
                 autoMaxPx: 40f, autoBasePx: 36f);   // A414（表 #14）原版 `auto[18~40] 基准=36.0`
            // ☝ A258：原版 `折行=1`；🆕 A323：原版 `Name/Desc input title` 两处都是 `Left/Midline` ⇒ `alignLeft: true`
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 两处原版都是 `m_VerticalAlignment = 4096 (Midline)`。
            MenuDraw.SetVAlign(lbTitle, Label.VAlign.Midline, titleR);
            var box = Node(parent, boxName, boxR);
            // 🔴 **2026-10-30（`B102_ALLY` · 判据 AUD04 第 50 行）**：底图 / 九宫 / tint 仍是原来那三句的**原样实参**
            //   （`40K_dropdown_bg` · (23,20,23,20) · (1,0.475,0.098,1)）—— tint 走 `Create` 的 `bgTint` 形参
            //   （⛔ 没换图、没换色）；原来那句 `Hit(...)⇒Say("打不了字")` 交代给 `TextInputField.Create` 自己的命中区。
            //   ⚠️ 占位字**留空**：原版这两格的 placeholder 文字本轮**没读到** ⇒ 不编一个字放上去（`InputData.Label = ""`）。
            //   ⚠️ 字体面传 **Cjk**：这两格的字可能含中文（提示窗缺省是拉丁体，会画不出汉字）。
            var input = TextInputField.Create(box, boxR,
                new PromptContext.InputData(boxName, "", TMPro.TMP_InputField.ContentType.Standard, ""),
                0, Page.Q + QOff + L_Btn, null,
                bgTint: new Color(1f, 0.475f, 0.098f, 1f), face: TmpFont.Face.Cjk);
            if (input == null)
                Debug.LogWarning("[Alliance] `" + boxName + "` 的输入框没建出来（`TextInputField.Create` 返回 null）"
                               + " —— 如实说：这一格仍然打不了字（⛔ 不假装能输）");
            return input;
        }

        /// <summary>一个**可展开**的下拉（标题 + 合上面 + `Template` 列表）。🔴 **2026-10-28（`B90-N5`）就地订正**：
        /// 原写「点开要 `Template`，那是 Unity 内置模板 ⇒ 我们**不建**」—— **不成立**，`Template` 是原版 prefab 真有的子树
        /// （rect/贴图/九宫/字号见 `普查产出_0927/社交_联盟与好友页.md:191-205`）⇒ 现在真能展开；几何/素材/开合全在
        /// `AllianceDropdownBox`（⛔ 别在这儿抄第二份）。`options` = 展开后那几行。</summary>
        void Dropdown(Transform parent, string name, string title, PxRect titleR, PxRect fieldR, string fieldName,
                      string[] options)
        {
            Text(parent, titleR, title, Color.white, name, 40f, L_Text, 18f, wrap: true, alignLeft: true,
                 autoMaxPx: 40f, autoBasePx: 36f);   // A414（表 #15）原版 `auto[18~40] 基准=36.0`
            // ☝ A258：原版 `折行=1`；🆕 A323：原版 `Select Language`/`Select Privacy` 两处都是 `Left/Middle` ⇒ `alignLeft: true`
            // 🆕 2026-10-28（`B90-N5`）：合上面（九宫底 + 箭头 + 空 Label）与**展开列表**都交给 `AllianceDropdownBox`
            //   —— 两处（本页 / `AllianceMemberTab` 改盟设置那一态）用**同一份**几何与开合逻辑。
            //   原版这两颗的箭头矩形 = `fieldR.x1 + 225 … + 245`、`fieldR.y1 + 19.7 … + 39.7`，与那个类按
            //   `x2−25 / cy±10` 算出来的**逐值相同**（fieldR 宽 250）⇒ 传不传那一对都一样，故不再单传。
            //    ⚠️ `Create` 恒返回非 null（节点建不出来时它自己出声）⇒ 这里不判空，也别再补一个空分支。
            AllianceDropdownBox.Create(this, parent, fieldName, fieldR, options);
        }

        // ============================================================================================
        // 🆕 2026-10-30（执行代理 `B102_ALLY`）—— **类尾新增**：三处输入件的字段 / 样式档 / 自检读口
        // ============================================================================================
        // 为什么追加在**类尾**：本文件上半部有一批账行是**按行号**引用的（`账/社交_*.tsv`），
        //   往成员之间插东西会让它们整体漂（同 `Shell/NetBattle.cs` 那条教训，见 CLAUDE.md §18·b）。
        //   已有成员一个都没动过（`Build` / `BuildListView` / `Field` 只改了各自的**方法体**）。
        // 判据 & 施工单：AUD04 第 49/50 行 = `资料/完全复刻/判据/AUD04_interactions.tsv`。
        // ============================================================================================

        /// <summary>`List View/Search Field` 那颗真输入框（原版 `JoinAllianceMenu.searchInput`，`TMP_InputField`）。</summary>
        TextInputField _searchInput;
        /// <summary>`Create Alliance View/Name Input`（原版 `CreateAllianceMenu` 那一格 `TMP_InputField`）。</summary>
        TextInputField _nameInput;
        /// <summary>`Create Alliance View/Desc Input`（同上；原版那颗是 `TMP_InputField` 的多行档）。</summary>
        TextInputField _descInput;

        /// <summary>自检读口：搜索框（`null` = 没建出来 —— ⛔ 那是缺口，不是「没数据」）。</summary>
        public TextInputField SearchInput { get { return _searchInput; } }
        /// <summary>自检读口：建盟 Name 那一格。</summary>
        public TextInputField NameInput { get { return _nameInput; } }
        /// <summary>自检读口：建盟 Desc 那一格。</summary>
        public TextInputField DescInput { get { return _descInput; } }
        /// <summary>自检读口：`Open Alliances` 的行容器（数它底下有几个 `Entry` = 过滤之后剩几条）。</summary>
        public Transform OpenListForTest { get { return _openList; } }
        /// <summary>`Continue` 那一刻**收到的**建盟名字（= 原版请求参数的第一格；`null` = 还没点过）。
        /// ⚠️ 它只是「收到的参数」，**不代表建过盟**（本机没有服务器，见 `Continue` 那一跳的注释）。</summary>
        public string LastCreateName { get; private set; }
        /// <summary>`Continue` 那一刻**收到的**建盟描述（同 <see cref="LastCreateName"/>）。</summary>
        public string LastCreateDesc { get; private set; }

        /// <summary>自检用：把上面两格读口清回 `null`（点 `Continue` 之前调，防止读到上一轮的残值）。
        /// ⚠️ **只给自检**（生产里这两个值由 `Continue` 那一跳写，没有别的写手）。</summary>
        public void ResetLastCreateForTest() { LastCreateName = null; LastCreateDesc = null; }

        /// <summary>⚠️ 两处都**没有「样式档」常量**（别照着加）：搜索框走 `TextInputField.Bind`（版面/占位字/命中区
        /// 都是本文件 `BuildListView` 里原来那几句画的）；建盟两格走 `Create` 的 `bgTint` / `face` 两个形参
        /// （见 `Field` 那一段）。⛔ 数值只写在调用点一处，别在这儿再抄一份。</summary>
    }
}
