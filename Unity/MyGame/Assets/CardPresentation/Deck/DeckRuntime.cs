// DeckRuntime.cs — 卡组编辑界面的**运行时控制器**（建界面 + 听鼠标/键盘 + 重画）
//
// 为什么需要它：`DeckEditorState`（纯 C#）早就写好、自检也覆盖了行为，
// 但**运行时交互层从来没建过** —— `DeckEditor.unity` 里原来只有 `ImageQuad`/`Label` 画的
// 一堆静态方块，点/拖一律不响应。这个类补上「谁听鼠标 + 谁重画」这一层。
//
// ============================ 版面权威（唯一出处） ============================
// `D:/2/Warpforge_tools/data/ui_layout/_deck_editing_godot_rects.txt`
//   —— `chain_rect.py` 算的 Godot 绝对坐标（1920×1080，y 向下），每行带 `RectTransform_<id>.json` 可复查。
// 节点树同一棵：`资料/说明书/04_界面UI/菜单全树.md` 的 `Deck Editing Menu` 段（:9249-9496），
//   带每个元素的 sprite id；id → 名字是拿 `ui_extract/**/Sprite/*.json` 的 `pathid` 反查的。
// 三路查证的正本：`资料/卡组编辑界面_查证_0920.md`（**动手前先读它**）。
//
// 下面所有 `const float` 都是**从那张表逐行抄下来的**（写的是 x,y,w,h，和表里同序），不是估的。
// **改版面先改表、再改这里。**
//
// ============================ 交互判据（逐条照原版） ============================
//   · 卡池卡：**左键 = 弹放大窗** · **右键 = 加进卡组**（`DeckEditingWindow__OnCardClick.c:16,42`）
//   · 卡组条目：**左键 = 弹放大窗**（**不是删除**！`DeckEditingWindow__Start.c:109` → `OpenCardInformation`）
//   · **删除只有「拖出」一条路**（`DeckEditingPanel__CheckCardSlotDrag.c:59,66`）
//   · **ESC = 保存**（`DeckEditingWindow__ESCPressed.c:5`，**不是关闭**）· Done = 保存（`TrySaveDeck.c`）
//   · **翻页不存在** —— 卡池是 `RecyclableScrollRect` 无限滚动（`CollectionDisplay.cs:11`）；
//     我们 2026-09-20 之前那版**自加的 prev/next 分页已删**（`DeckEditorState` 里那套 Page/PageCount 也删了）
//   · 筛选栏**在左**、与侧栏同一块 rect（由 Header 的 `Filters` 键开合）—— 权威坐标 R:174-176
//
// ============================ 仍然是我们挑的（原版查不到，如实标） ============================
//   ① **卡池一屏 4 列 × 2 行** —— 原版是 `RecyclableScrollRect` 运行时算的，节点树给不出（`查证:166`）
//   ② **页签图标显示尺寸 100×100** —— 节点树里那三个 `Icon` 的 rect 是 **0×0**（VLG 撑的），
//      源图是 126×126；一排 3 个塞进 326.9 宽时 126 会互相压住，故取 100。**这是尺寸，不是版式。**
//   ③ **行距 56**（行高 55.7 是权威，垂直排布的 spacing 查不到）· **卡池行距 528**（卡高 512 + 16 缝）
//   ④ 侧栏三个钮（新建/复制/删除）**已按原版删掉** —— 原版那套动作在 `DeckInfoPopup` 的 5 圆钮里
//   ⑤ 稀有度色条的颜色值、筛选栏里各行的高度与字号（原版是 VLG 流式，绝对坐标只是模板位）
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>卡组编辑界面的运行时控制器。挂在 `DeckEditor.unity` 的根对象上。</summary>
    public class DeckRuntime : MonoBehaviour
    {
        public static DeckRuntime Instance { get; private set; }

        // ============================================================ 比例尺
        // 原版界面按 1920×1080 设计；这套布局可见高 10 世界单位 → 108 px/单位
        public const float PxPerUnit = 1080f / LayoutSpace.DesignHeight;   // = 108
        public const float ScreenW = 1920f, ScreenH = 1080f;

        // ============================================================ 版面常量（px，y 向下）
        // ---- Content Area / Header（附表 R:2-31）----
        const float HdrBackX = 192.2f, HdrBackY = 83.5f, HdrBackW = 150f, HdrBackH = 60f;
        const float HdrFltBtnX = 367.2f, HdrFltBtnY = 88.5f, HdrFltBtnS = 50f;
        const float HdrFltIconX = 377.2f, HdrFltIconY = 98.5f, HdrFltIconS = 30f;
        const float HdrFltLblX = 437.2f, HdrFltLblY = 88.5f, HdrFltLblW = 150f, HdrFltLblH = 50f;
        // ⚠️ `Clear filters` 的 x **不能照抄 dump 的 1488.6** —— 它落在父容器 `Filters [592,71 876×85]`
        //    的**外面**（容器右边界 1468.6），那是 **VLG 布局跑之前的模板位**（同 `查证:103` 那条）。
        //    容器右边界 1468.6 与 `WIldcard Counter` 的左边界 1470 **正好相接** ⇒ 头部是一行：
        //    [Filters 圆钮][容器：Clear filters **右对齐**][Wildcard Counter][Army Icon]。
        //    ⇒ 取 `1468.6 − 250 = 1218.6`（右对齐）。照抄 1488.6 会**压在四个稀有度计数上**（自检抓过）。
        const float HdrClearX = 1218.6f, HdrClearY = 83.5f, HdrClearW = 250f, HdrClearH = 60f;
        const float HdrSepY = 151f, HdrSepH = 10f;
        const float WcBgX = 1550f, WcBgY = 91.5f, WcBgW = 320f, WcBgH = 44f;
        const float WcIconY = 71f, WcIconW = 30f, WcIconH = 44f;
        static readonly float[] WcIconX = { 1565f, 1640f, 1715f, 1790f };
        const float ArmyIconX = 1470f, ArmyIconY = 71f, ArmyIconW = 80f, ArmyIconH = 85f;

        // ---- Sidebar（R:33-56）----
        const float SideBgX = -203f, SideBgY = 156f, SideBgW = 538.5f, SideBgH = 924.1f;
        const float TabsX = 0.3f, TabsY = 156f, TabsW = 326.9f, TabsH = 150f;
        const float TabIconS = 100f;                      // ⚠️ 我们挑的（见文件头 ②）
        const float NameX = 9.5f, NameY = 311f, NameW = 307.7f, NameH = 50f;
        const float NameTxX = 19.5f, NameTxY = 318f, NameTxW = 287.7f, NameTxH = 37f;
        const float NameClrX = 277.2f, NameClrY = 316f, NameClrW = 35f, NameClrH = 40f;

        // ---- Deck Details：卡组列表（R:57-96）----
        const float ListX = 0.4f, ListY = 366f, ListW = 325f, ListH = 644.1f;
        const float RowW = 325f, RowH = 55.7f;
        const float RowPitch = 56f;                       // ⚠️ 我们挑的（见文件头 ③）
        const float RowCostX = 17f, RowCostS = 38f;
        const int RowVisible = 11;                        // 644.1 / 55.7 = 11.56 ⇒ 11 行整

        // ---- 费用曲线（R:97-153；原版在 Deck info 页签下）----
        const float CurveX = 56.1f, CurveY = 411f, CurveRowH = 18.9f, CurveStep = 22.3f;
        const float CurveBarX = 92.2f, CurveBarW = 151.3f;

        // ---- Footer（R:159-166）----
        const float DoneX = 13f, DoneY = 1020.5f, DoneW = 188.5f, DoneH = 50.2f;
        const float DoneHlX = 67.2f, DoneHlY = 933.5f, DoneHlW = 81.4f, DoneHlH = 223.2f;
        const float CntX = 251.6f, CntY = 1020f, CntW = 75f, CntH = 50f;
        const float FootIcX = 201.6f, FootIcY = 1025f, FootIcW = 50f, FootIcH = 40f;

        // ---- Card Display 卡池（R:167-170）----
        const float PoolX = 330.2f, PoolY = 156f, PoolW = 1589.8f, PoolH = 924.1f;
        const float CardW = 350f, CardH = 512f;           // 原版 `Collection Card`
        const int PoolCols = 4, PoolRows = 2;             // ⚠️ 我们挑的（见文件头 ①）
        const float PoolPadY = 8f;                        // ⚠️ 我们挑的
        const float PoolRowPitch = CardH + 16f;           // ⚠️ 我们挑的（见文件头 ③）
        /// <summary>卡池里那张卡缩到多大 —— 按原版卡位**高度** 512 反解
        /// （`CardView.Height 3.3313 × 108 = 359.7 px`）。
        /// ⚠️ 原版卡位 350×512 的宽高比 0.684 和我们的卡（2.0927/3.3313 = 0.628）**不是同一个**，
        /// 两个都按原尺寸放会互相压住 —— 这里选「高度对齐」，宽度差 8%（322 vs 350）。</summary>
        const float PoolCardScale = CardH / (CardView.Height * PxPerUnit);
        static readonly float PoolPadX = (PoolW - PoolCols * CardW) / (PoolCols + 1);   // = 37.96

        // ---- Card Filters 筛选栏（**在左**，R:174-217）----
        const float FltX = 2.2f, FltY = 156f, FltW = 331.7f, FltH = 924.1f;
        const float FltPad = 10f;                         // ⚠️ 我们挑的（原版内部是 VLG 流式）
        const float FltRowH = 42f;                        // ⚠️ 我们挑的

        // ---- 分层：**用渲染队列，不用 z** ----
        // 🔴 踩过的坑（2026-09-20）：透明队列里 Unity 是按**到相机的 3D 距离**排序的，
        //    而我们的图铺满整屏 ⇒ **屏幕中间那张（离相机近）会盖住屏幕边缘那张**，哪怕它的 z 更远。
        //    实测：侧栏底板（中心 x≈-5.3）盖住了整个卡组列表（x≈-7.4）—— 卡组行一个都看不见。
        //    ⇒ 改用 `SetRenderQueue`（每个 quad 一份独立材质，改队列是安全的）。
        //    ⚠️ 同一条队列里仍然只放**互不重叠**的东西（自检有一条「同一层不许压住」的断言）。
        const int QSide = 3000;      // 侧栏底板
        const int QDoneHl = 3001;    // Done 的外发光（原版它纵跨到卡组列表区，单独一层）
        const int QGrad = 3002;      // 行的稀有度色条 / 空卡组提示
        const int QPanel = 3003;     // 行底 / 分隔线 / 输入框底 / 曲线槽 / 计数器底
        const int QBorder = 3004;    // 行描边 / 小图标
        const int QPoolInfo = 3005;  // 卡池读数（⚠️ 我们自己加的，原版没有）
        const int QRow = 3006;       // 按钮底 / 费用圆
        const int QText = 3007;      // 文字
        const int QFlt = 3020;       // 筛选栏（盖住侧栏 ⇒ 队列更大 = 更后画）
        const int QFltRow = 3021, QFltText = 3022;
        // 导入弹窗是**模态**，压在一切之上（`CardDisplayWindow` 那套也在 3000 段，所以留足余量）
        const int QModal = 3100, QModalRow = 3101, QModalText = 3102;

        // ============================================================ 状态
        public DeckEditorState State { get; private set; }
        public DeckLibrary Library { get; private set; }
        public Transform Root { get; private set; }

        Camera _cam;
        int _tab;                       // 0 = Cards · 1 = Deck info · 2 = Cosmetics
        bool _filtersOpen;
        bool _ownedOnly = true;         // 单机全解锁 ⇒ 恒真，见 HandleFilterRow
        float _poolScroll, _deckScroll, _fltScroll;
        /// <summary>正在编辑的文本缓冲（`null` = 没在编辑）。<see cref="_editKind"/> 说明它在改什么。</summary>
        string _nameEdit;
        /// <summary>0 = 没在编辑 · 1 = 改**卡组名**（原版 `DeckEditingPanel.ChangeName`）· 2 = 改**卡名筛选**（原版 `CardNameFilter`）。</summary>
        int _editKind;

        // 导入卡组弹窗（原版 `ImportDeckPopup`，坐标 = `资料/说明书/04_界面UI/卡组界面说明书.md` §五）
        bool _importOpen;
        string _importText = "";
        string _importError = "";
        readonly HashSet<string> _missingArt = new HashSet<string>();

        readonly List<CardView> _poolViews = new List<CardView>();
        readonly List<int> _poolIndex = new List<int>();

        readonly List<ImageQuad> _deckRowBg = new List<ImageQuad>();
        readonly List<ImageQuad> _deckRowGrad = new List<ImageQuad>();
        readonly List<ImageQuad> _deckRowBorder = new List<ImageQuad>();
        readonly List<ImageQuad> _deckRowCntIc = new List<ImageQuad>();
        readonly List<Label> _deckRowName = new List<Label>();
        readonly List<Label> _deckRowCount = new List<Label>();
        readonly List<ImageQuad> _deckRowCost = new List<ImageQuad>();

        readonly List<Label> _tabLabel = new List<Label>();
        readonly List<ImageQuad> _tabHi = new List<ImageQuad>();
        readonly List<ImageQuad> _tabIcon = new List<ImageQuad>();

        readonly List<ImageQuad> _curveFill = new List<ImageQuad>();
        readonly List<Label> _curveLabel = new List<Label>();
        readonly List<ImageQuad> _curveBar = new List<ImageQuad>();
        readonly List<GameObject> _infoOnly = new List<GameObject>();   // 只在 Deck info 页签显示的东西

        /// <summary>可点的矩形（px 坐标）。命中判定一律走它 —— 比拿 quad 反推尺寸稳。</summary>
        struct Btn { public string Key; public float X, Y, W, H; }
        readonly List<Btn> _btns = new List<Btn>();

        Label _title, _notice, _storeErr, _deckNameText, _deckNameHint, _counterTxt, _verdict, _poolInfo;
        readonly Label[] _wcTxt = new Label[4];
        ImageQuad _emptyWarn, _doneHl;
        GameObject _emptyWarnGo;
        string _noticeText = "";
        float _noticeUntil;

        // 拖拽状态（把卡组条目拖出侧栏 = 删除，照原版）
        ImageQuad _dragQuad;
        Label _dragText;
        int _dragRow = -1;
        bool _dragging, _draggingMoved;
        Vector3 _dragOrigin;

        // ============================================================ 生命周期

        void Awake() { Instance = this; }

        void Start()
        {
            // 按 Play 的入口 —— 和自检**必须是同一条路**（`CardPresentation` 的规矩：
            // 开局写在 `Start()` 里 = 批处理下永远验不到，所以抽成公开的 `Build` 让两边都调它）
            if (State == null) Build(DeckLibrary.Load());
        }

        void Update()
        {
            HandlePointer();
            HandleScroll();
            HandleTyping();
            TickTooltip();
            if (_notice != null && _noticeText.Length > 0 && Time.unscaledTime > _noticeUntil)
            {
                _noticeText = "";
                _notice.SetText("");
            }
        }

        /// <summary>悬停信息层。原版那套触发器挂在**卡面数值容器**上（`Health/Range Attack/Melee Attack/Cost Container`），
        /// 卡池里的卡也是同一批 prefab ⇒ 卡组编辑这边一样有。
        /// ⚠️ **护甲那个容器原版就没有 tooltip**（`子代理读报_2dcard_0827.md:95`）⇒ 这里也不给。</summary>
        void TickTooltip()
        {
            var mouse = Mouse.current;
            if (mouse == null) { Tooltip.Hide(); return; }
            if (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame) { Tooltip.Hide(); return; }
            TickTooltipAt(LayoutSpace.ScreenToWorld(mouse.position.ReadValue(), _cam));
        }

        /// <summary>自检入口：批处理没有鼠标 ⇒ 直接喂世界坐标（**和鼠标那条路同一个函数**）。</summary>
        public bool TickTooltipAt(Vector3 wp)
        {
            for (int i = 0; i < _poolViews.Count; i++)
            {
                var v = _poolViews[i];
                if (v == null || !v.gameObject.activeSelf) continue;
                int s = v.StatAt(wp);
                if (s == CardView.StatNone || s == CardView.StatArmour) continue;
                Vector3 at = v.StatWorld(s);
                switch (s)
                {
                    case CardView.StatMelee:  Tooltip.Show(TipText.Melee,  at, 15, new Vector3(-49.33f / 108f, 0f, 0f)); return true;
                    case CardView.StatRanged: Tooltip.Show(TipText.Ranged, at, 15, new Vector3(-53.54f / 108f, 0f, 0f)); return true;
                    case CardView.StatHealth: Tooltip.Show(TipText.Health, at, 10, new Vector3( 73.05f / 108f, 0f, 0f)); return true;
                    case CardView.StatCost:   Tooltip.Show(TipText.Cost,   at, 10, new Vector3( 52.60f / 108f, 0f, 0f)); return true;
                }
            }
            Tooltip.Hide();
            return false;
        }

        // ============================================================ 建

        /// <summary>按像素坐标建整个界面。**自检与运行时调同一个**（见 <see cref="Start"/>）。</summary>
        public void Build(DeckLibrary lib)
        {
            Library = lib ?? DeckLibrary.Load();
            _cam = Camera.main;
            if (_cam != null) LayoutSpace.Apply(_cam);

            Root = transform;
            foreach (Transform c in Root) DestroySafe(c.gameObject);
            _btns.Clear(); _missingArt.Clear();

            // 空库时先替玩家建一套（演示卡组），这样界面一打开就有内容
            if (Library.Count == 0)
            {
                var fresh = NewState();
                Library.Create("我的卡组");
                Library.CommitCurrent(PlayerDeckForDemo(fresh));
            }
            State = NewState();
            State.LoadDeck(Library.Current);

            BuildHeader();
            BuildSidebar();
            BuildDeckList();
            BuildCurve();
            BuildInfoActions();
            BuildFooter();
            BuildFilters();
            BuildPool();
            BuildCosmeticsEmpty();
            BuildImportPopup();
            BuildNotice();

            if (_missingArt.Count > 0)
                Debug.LogWarning("[DeckRuntime] 缺 " + _missingArt.Count + " 张原版 UI 图（会画成纯白占位，"
                                 + "**不是静默**）：" + string.Join("、", _missingArt));

            RefreshAll();
        }

        DeckEditorState NewState() { return new DeckEditorState(CardDatabase.Load()); }

        // ------------------------------------------------------------ Header

        void BuildHeader()
        {
            Img("hdr_sep", "40k_main_line", 167.2f, HdrSepY, 1752.8f, HdrSepH, QPanel);
            Img("hdr_back", "UI_Button_Mulligan", HdrBackX, HdrBackY, HdrBackW, HdrBackH, QRow);
            Txt("hdr_back_t", "返回", HdrBackX, HdrBackY, HdrBackW, HdrBackH, 2, Ink, QText);
            Btn_("hdr_back", HdrBackX, HdrBackY, HdrBackW, HdrBackH);

            // Filters 圆钮 + 图标 + 文字（原版这三块是分开的三条 rect）
            Img("hdr_fltbtn", "40k_menu_bt", HdrFltBtnX, HdrFltBtnY, HdrFltBtnS, HdrFltBtnS, QRow);
            Img("hdr_flticon", "40k_bt_icon_search", HdrFltIconX, HdrFltIconY, HdrFltIconS, HdrFltIconS, QBorder);
            Txt("hdr_fltlbl", "Filters", HdrFltLblX, HdrFltLblY, HdrFltLblW, HdrFltLblH, 2, Ink, QText);
            Btn_("hdr_filters", HdrFltBtnX, HdrFltBtnY, HdrFltLblX + HdrFltLblW - HdrFltBtnX, HdrFltLblH);

            Img("hdr_clear", "UI_Button_Mulligan", HdrClearX, HdrClearY, HdrClearW, HdrClearH, QRow);
            Txt("hdr_clear_t", "Clear filters", HdrClearX, HdrClearY, HdrClearW, HdrClearH, 2, Ink, QText);
            Btn_("hdr_clear", HdrClearX, HdrClearY, HdrClearW, HdrClearH);

            // Wildcard Counter：底板 + 四个稀有度图标（30×44）+ 各自的数量
            Img("hdr_wcbg", "40k_topmarquee_currency_display_BW", WcBgX, WcBgY, WcBgW, WcBgH, QPanel);
            string[] wcIc = { "40k_general_wildcard_common_small", "40k_general_wildcard_rare_small",
                              "40k_general_wildcard_epic_small", "40k_general_wildcard_legendary_small" };
            for (int i = 0; i < 4; i++)
            {
                Img("hdr_wc" + i, wcIc[i], WcIconX[i], WcIconY, WcIconW, WcIconH, QRow);
                _wcTxt[i] = Txt("hdr_wct" + i, "0", WcIconX[i] - 8f, WcIconY + WcIconH, 26f, 22f, 1, Ink, QText);
            }
            Img("hdr_army", FactionIcon(null), ArmyIconX, ArmyIconY, ArmyIconW, ArmyIconH, QRow);
        }

        // ------------------------------------------------------------ Sidebar

        void BuildSidebar()
        {
            Img("side_bg", "40k_main_tab_background", SideBgX, SideBgY, SideBgW, SideBgH, QSide);

            // Window Options：三个互斥页签（Cards / Deck info / Cosmetics）
            string[] tabIc = { "40k_collection_bt_cards", "40k_collection_bt_decks", "40k_collection_bt_cosmetics" };
            string[] tabTx = { "Cards", "Deck info", "Cosmetics" };
            for (int i = 0; i < 3; i++)
            {
                float slot = TabsW / 3f;
                float x = TabsX + slot * (i + 0.5f) - TabIconS * 0.5f;
                float y = TabsY + (TabsH - TabIconS) * 0.5f;
                _tabHi.Add(Img("tab_hi" + i, "40k_main_bt_selected_BW", x, y, TabIconS, TabIconS, QPanel));
                _tabIcon.Add(Img("tab_ic" + i, tabIc[i], x, y, TabIconS, TabIconS, QRow));
                _tabLabel.Add(Txt("tab_tx" + i, tabTx[i], x, y + TabIconS - 10f, TabIconS, 26f, 1, Ink, QText));
                Btn_("tab_" + i, x, y, TabIconS, TabIconS + 20f);
            }

            // Deck Name（原版是 `EverguildInputField : TMP_InputField`，占位字 'Tap to edit deck name'）
            Img("name_bg", "40K_dropdown_bg", NameX, NameY, NameW, NameH, QPanel);
            _deckNameText = Txt("name_t", "", NameTxX, NameTxY, NameTxW, NameTxH, 2, Ink, QText);
            _deckNameHint = Txt("name_h", "Tap to edit deck name", NameTxX, NameTxY, NameTxW, NameTxH, 2,
                                new Color(1f, 1f, 1f, 0.42f), QText);
            Img("name_clear", "40k_icon_search", NameClrX, NameClrY, NameClrW, NameClrH, QBorder);
            Btn_("name_box", NameX, NameY, NameW, NameH);
            Btn_("name_clear", NameClrX, NameClrY, NameClrW, NameClrH);
        }

        // ------------------------------------------------------------ 卡组列表

        void BuildDeckList()
        {
            var rowTex = Ui("UI_Card_name_background_normal_BW");
            var gradTex = Ui("40k_deck_cardlist_bg_rarityColorGradient");
            var borderTex = Ui("40k_deck_cardlist_border");
            // ⚠️ `Card Frame Cost Icon` 的**文件名是下划线版**（`sync_battle_ui_art.py` 把空格换成 `_`）
            var costTex = Ui("Card_Frame_Cost_Icon");
            var cntTex = Ui("40K_main_deck_card_counter");

            for (int i = 0; i < RowVisible; i++)
            {
                float y = ListY + i * RowPitch;
                // ⚠️ 三条的 z 必须**互不相同**（同层重叠会被自检抓）——
                //    色条(0.35) < 行底(0.30) < 描边(0.25) < 费用圆(0.20) < 文字(0.19)
                _deckRowGrad.Add(Img("row_g" + i, gradTex, ListX, y, RowW, RowH, QGrad));
                _deckRowBg.Add(Img("row_" + i, rowTex, ListX, y, RowW, RowH, QPanel));
                _deckRowBorder.Add(Img("row_b" + i, borderTex, ListX, y, RowW, RowH, QBorder));
                _deckRowCost.Add(Img("row_c" + i, costTex, RowCostX, y + (RowH - RowCostS) * 0.5f, RowCostS, RowCostS, QRow));
                _deckRowCntIc.Add(Img("row_k" + i, cntTex, 258f, y + 8f, 40f, RowH - 16f, QRow));
                _deckRowName.Add(Txt("row_n" + i, "", 62f, y, 190f, RowH, 1, Ink, QText));
                _deckRowCount.Add(Txt("row_cnt" + i, "", RowCostX, y + (RowH - RowCostS) * 0.5f, RowCostS, RowCostS, 1, Ink, QText));
            }

            // Empty Warning（原版 inactive，空卡组时才显示）
            _emptyWarn = Img("empty_warn", "40k_deck_cardlist_doted_bg", 20.3f, 445f, 295.3f, 186f, QGrad);
            var warnLabel = Txt("empty_warn_l", "把卡拖到这里", 20.3f, 500f, 295.3f, 60f, 2,
                                new Color(1f, 1f, 1f, 0.65f), QText);
            if (warnLabel != null) _emptyWarnGo = warnLabel.gameObject;
        }

        // ------------------------------------------------------------ 费用曲线（Deck info 页签）

        void BuildCurve()
        {
            var barBg = Ui("40k_CardAmount_bar_bg");
            var barFill = Ui("40k_CardAmount_bar_fill");
            for (int c = 0; c <= 8; c++)
            {
                float y = CurveY + c * CurveStep;
                var lb = Txt("curve_l" + c, c.ToString(), CurveX - 14f, y, 26f, CurveRowH, 1, Ink, QText);
                var bar = Img("curve_b" + c, barBg, CurveBarX, y + 1.6f, CurveBarW, 16.6f, QPanel);
                var fill = Img("curve_f" + c, barFill, CurveBarX, y + 2f, 1f, 15.3f, QRow);
                var num = Txt("curve_n" + c, "0", CurveBarX + CurveBarW + 6f, y, 34f, CurveRowH, 1, Ink, QText);
                _curveBar.Add(bar); _curveFill.Add(fill); _curveLabel.Add(lb); _curveLabel.Add(num);
                _infoOnly.Add(bar != null ? bar.gameObject : null);
                _infoOnly.Add(fill != null ? fill.gameObject : null);
                _infoOnly.Add(lb != null ? lb.gameObject : null);
                _infoOnly.Add(num != null ? num.gameObject : null);
            }
        }

        // ------------------------------------------------------------ Footer

        void BuildFooter()
        {
            _doneHl = Img("foot_hl", "FX_Square_UI_SDF", DoneHlX, DoneHlY, DoneHlW, DoneHlH, QDoneHl);
            Img("foot_done", "UI_Button_Mulligan", DoneX, DoneY, DoneW, DoneH, QRow);
            Txt("foot_done_t", "Done", DoneX, DoneY, DoneW, DoneH, 2, Ink, QText);
            Img("foot_ic", "40k_general_icon_card_amount", FootIcX, FootIcY, FootIcW, FootIcH, QBorder);
            _counterTxt = Txt("foot_cnt", "0/30", CntX, CntY, CntW, CntH, 2, Ink, QText);
            Btn_("foot_done", DoneX, DoneY, DoneW, DoneH);
            _verdict = Txt("foot_verdict", "", CntX - 70f, CntY - 42f, 180f, 34f, 1, Ink, QText);
        }

        // ------------------------------------------------------------ 筛选栏（在左）

        void BuildFilters()
        {
            // ⚠️ 这两张是**面板本体**，开/关由 `RefreshFilters` 控制 ——
            //    忘了关的话它（队列 3020，比侧栏大）会**一直盖住整个侧栏**
            //    （2026-09-20 实测：卡组行/页签/Done 全被它盖住，画面上只剩一块底板色）。
            _fltPanelShadow = Img("flt_shadow", "40k_main_tab_shadow", FltX, FltY, FltW, FltH, QFlt - 1);
            _fltPanelBg = Img("flt_bg", "40k_main_tab_background", FltX, FltY, FltW, FltH, QFlt);
        }
        ImageQuad _fltPanelBg, _fltPanelShadow;

        readonly List<ImageQuad> _fltBg = new List<ImageQuad>();
        readonly List<Label> _fltTx = new List<Label>();
        readonly List<FilterRow> _fltRows = new List<FilterRow>();
        struct FilterRow { public string Key; public string Label; public bool On; public float X, Y, W, H; }

        // ------------------------------------------------------------ 卡池

        void BuildPool()
        {
            // 🔴 卡池的视图**不能拿 `default(CardData)` 建** —— 空数据会走 `CardView.Build` 的
            //    「空卡位」分支（没有卡框/立绘那几层），而 `SetData` **只更新已存在的层**、
            //    不会把缺的层补出来 ⇒ 整个卡池的画面上是**一排空卡位**（2026-09-20 截图看出来的）。
            //    ⇒ 改成「**卡变了就重建视图**」：一屏只有 8 张，重建的代价可以接受，正确性优先。
            _poolViews.Clear();
            _poolViewIds.Clear();
            Img("pool_info_bg", "40k_topmarquee_currency_display_BW", PoolX + 8f, PoolY - 8f, 220f, 40f, QPoolInfo);
            _poolInfo = Txt("pool_info", "", PoolX + 8f, PoolY - 8f, 220f, 40f, 1, Ink, QText);
        }

        /// <summary>每个格子里现在建的是哪张卡（`null` = 空位）。和 <see cref="_poolViews"/> 一一对应。</summary>
        readonly List<string> _poolViewIds = new List<string>();

        void RefreshPool()
        {
            var all = State.VisibleCards();

            float pitch = PoolRowPitch;
            int firstRow = Mathf.FloorToInt(_poolScroll / pitch);
            float off = _poolScroll - firstRow * pitch;
            float top = PoolY + PoolPadY;

            _poolIndex.Clear();
            for (int vi = 0; vi < PoolCols * PoolRows; vi++)
            {
                int r = vi / PoolCols, c = vi % PoolCols;
                int idx = (firstRow + r) * PoolCols + c;
                bool on = idx < all.Count;
                _poolIndex.Add(on ? idx : -1);
                while (_poolViews.Count <= vi) { _poolViews.Add(null); _poolViewIds.Add(null); }

                if (!on)
                {
                    if (_poolViews[vi] != null) _poolViews[vi].gameObject.SetActive(false);
                    continue;
                }

                var def = all[idx];
                float cx = PoolX + PoolPadX + CardW * 0.5f + c * (CardW + PoolPadX);
                float cy = top + CardH * 0.5f + r * pitch - off;

                if (_poolViews[vi] == null || _poolViewIds[vi] != def.Id)
                {
                    if (_poolViews[vi] != null)
                    {
                        // ⚠️ Play 模式下 `Destroy` 要等帧末 —— 先关掉，否则那一帧新旧两张会叠在一起
                        _poolViews[vi].gameObject.SetActive(false);
                        DestroySafe(_poolViews[vi].gameObject);
                    }
                    _poolViews[vi] = CardView.Create(Root, BattleDriver.ToCardData(def, def.Faction), "pool_" + vi);
                    _poolViewIds[vi] = def.Id;
                }
                var v = _poolViews[vi];
                v.gameObject.SetActive(true);
                v.SetPose(Pos(cx, cy), 0f, PoolCardScale);
                v.SetData(BattleDriver.ToCardData(def, def.Faction));
                v.SetFace(CardFace.Full);
                v.SetHighlight(State.CanAdd(def) == DeckError.None
                               ? CardHighlightState.Playable : CardHighlightState.Normal);
            }

            int rows = Mathf.Max(1, Mathf.CeilToInt(all.Count / (float)PoolCols));
            float maxScroll = Mathf.Max(0f, rows * pitch - 16f - PoolH + PoolPadY * 2f);
            _poolScroll = Mathf.Clamp(_poolScroll, 0f, maxScroll);
        }

        // ------------------------------------------------------------ Deck info 的动作钮 + Cosmetics 空态

        /// <summary>分享 / 导入两个钮。
        /// 🔴 **入口位置是我们放的** —— 原版这两个动作**不在编辑器窗口里**：
        /// 分享 = `DeckInfoPopup.ShareDeck()`（卡组选择界面的 Info 弹窗）、
        /// 导入 = `SelectDecksTab.ImportDeck()` → `ImportDeckPopup`。
        /// 那两个界面我们还没有（属第 16 行阶段二），所以先摆在 **Deck info 页签**下面，
        /// 免得玩家找不到入口。**别当原版抄。**</summary>
        void BuildInfoActions()
        {
            // ⚠️ 这两张是 **71×71 的圆钮图标**（`40k_general_bt_yellow_share` / `_edit`，
            //    和 `DeckInfoPopup` 那 5 个圆钮同一族）—— 按**原尺寸**摆，别拉成横条
            //    （拉过一次：变成两条斜飘带，截图看出来的）。文字放钮下面。
            const float bs = 71f, by = 636f, lh = 26f;
            float bx1 = 60f, bx2 = 200f;
            // ⚠️ 这两张**只是图标**（四周透明）—— 原版那套圆钮是**两层**：
            //    底 `UI_Button_Round_background` + 图标。只画图标的话屏幕上什么都没有。
            Img("info_share_bg", "UI_Button_Round_background", bx1, by, bs, bs, QRow);
            Img("info_share", "40k_general_bt_yellow_share", bx1, by, bs, bs, QBorder);
            var shareTx = Txt("info_share_t", "分享", bx1, by + bs, bs, lh, 1, Ink, QText);
            Img("info_import_bg", "UI_Button_Round_background", bx2, by, bs, bs, QRow);
            Img("info_import", "40k_general_bt_yellow_edit", bx2, by, bs, bs, QBorder);
            var importTx = Txt("info_import_t", "导入", bx2, by + bs, bs, lh, 1, Ink, QText);
            Btn_("info_share", bx1, by, bs, bs);
            Btn_("info_import", bx2, by, bs, bs);
            foreach (var k in new[] { "info_share_bg", "info_share", "info_import_bg", "info_import" })
            { var q = Lookup(k); _infoOnly.Add(q != null ? q.gameObject : null); }
            // ⚠️ **标签要显式收进来** —— `Lookup()` 只认 `ImageQuad`（`_named` 是 `Img()` 填的），
            //    `Label` 不在里面 ⇒ 拿 `Lookup("info_share_t")` 永远拿到 null、字就**不跟着页签隐藏**
            //    （2026-09-20 截图抓到：Cards 页签下还飘着「分享 / 导入」两个字）。
            foreach (var l in new[] { shareTx, importTx }) if (l != null) _infoOnly.Add(l.gameObject);
        }

        /// <summary>Cosmetics 页签的空态。原版那一页是**饰品收藏**（`Cosmetic Display`），
        /// 我们**没有饰品数据**（单机版不做外观经济）⇒ **如实说**，不让它是一片空白。</summary>
        void BuildCosmeticsEmpty()
        {
            float x = PoolX, y = PoolY, w = PoolW, h = PoolH;
            var bg = Img("cosm_empty_bg", "40k_main_tab_background", x, y, w, h, QPanel);
            var t1 = Txt("cosm_t1", "Cosmetics", x, y + h * 0.5f - 60f, w, 60f, 3, Ink, QText);
            var t2 = Txt("cosm_t2", "饰品系统未实现 —— 单机版不做外观/经济（原版这一页是 `Cosmetic Display`）",
                         x, y + h * 0.5f + 10f, w, 50f, 2, new Color(1f, 1f, 1f, 0.75f), QText);
            _cosmOnly.Add(bg != null ? bg.gameObject : null);
            _cosmOnly.Add(t1 != null ? t1.gameObject : null);
            _cosmOnly.Add(t2 != null ? t2.gameObject : null);
        }
        readonly List<GameObject> _cosmOnly = new List<GameObject>();

        // ------------------------------------------------------------ 导入卡组弹窗（原版 `ImportDeckPopup`）

        /// <summary>原版 `ImportDeckPopup` 的 rect（`资料/说明书/04_界面UI/卡组界面说明书.md` §五）。
        /// ⚠️ **Confirm 那两个数是改过的**，理由写在下面。</summary>
        void BuildImportPopup()
        {
            const float wx = 560f, wy = 234f, ww = 800f, wh = 452f;
            Img("imp_bg", "40k_popup", wx, wy, ww, wh, QModal);
            _impTitle = Txt("imp_title", "Paste your deck", 610f, 280f, 700f, 60f, 2, Ink, QModalText);
            // 输入框：dump 给的是 [610,370] 700×141
            _impInputBg = Img("imp_input_bg", "40K_dropdown_bg", 610f, 370f, 700f, 141f, QModalRow);
            _impInputTx = Txt("imp_input", "Enter text...", 630f, 370f, 660f, 141f, 2,
                              new Color(1f, 1f, 1f, 0.5f), QModalText);
            _impErr = Txt("imp_err", "", 593f, 528f, 734f, 35f, 1, new Color(0.95f, 0.45f, 0.4f), QModalText);
            // ⚠️ Confirm：dump 说 `[354,615] 478×75` —— **x 落在窗口（560..1360）外面**，
            //    又是「VLG 布局前的模板位」（同 `Clear filters` 那条）⇒ 取**窗口内水平居中** = 721；
            //    y 取 615 会让按钮**冒出窗口下沿 4px**，改成**贴窗口底** = 686−75 = 611。
            Img("imp_ok", "40K_button", 721f, 611f, 478f, 75f, QModalRow);
            _impOkTx = Txt("imp_ok_t", "Confirm", 721f, 611f, 478f, 75f, 2, Ink, QModalText);
            Img("imp_close", "UI_Button_Round_background", 1317f, 202f, 75f, 75f, QModalRow);
            Img("imp_close_x", "40k_bt_close", 1317f, 202f, 75f, 75f, QModalText - 1);
            Btn_("imp_ok", 721f, 611f, 478f, 75f);
            Btn_("imp_close", 1317f, 202f, 75f, 75f);
            Btn_("imp_input", 610f, 370f, 700f, 141f);
            _modalOnly.Add(Lookup("imp_bg") != null ? Lookup("imp_bg").gameObject : null);
            foreach (var k in new[] { "imp_input_bg", "imp_ok", "imp_close", "imp_close_x" })
            { var q = Lookup(k); _modalOnly.Add(q != null ? q.gameObject : null); }
            // ⚠️ 标签同样要显式收（`Lookup()` 不认 Label —— 同 `info_*` 那条）
            foreach (var l in new[] { _impTitle, _impInputTx, _impErr, _impOkTx })
                if (l != null) _modalOnly.Add(l.gameObject);
            foreach (var go in _modalOnly) if (go != null) go.SetActive(false);
        }
        ImageQuad _impInputBg;
        Label _impInputTx, _impTitle, _impErr, _impOkTx;
        readonly List<GameObject> _modalOnly = new List<GameObject>();

        void BuildNotice()
        {
            _notice = Txt("notice", "", 640f, 1004f, 640f, 44f, 2, new Color(0.95f, 0.85f, 0.5f), QText);
            _storeErr = Txt("store_err", "", 640f, 966f, 640f, 34f, 1, new Color(0.95f, 0.5f, 0.4f), QText);
            _title = Txt("side_title", "卡组编辑", 0.3f, 160f, 326.9f, 0f, 2, Ink, QText);
        }

        // ============================================================ 刷新

        public void RefreshAll()
        {
            RefreshPool();
            RefreshDeckList();
            RefreshHeader();
            RefreshFilters();
        }

        /// <summary>卡组里那些条目（督军 / 防御卡 / 普通卡，顺序照原版 `DeckEditingPanel` 的三个槽）。</summary>
        public List<CardDef> DeckEntries()
        {
            var shown = new List<CardDef>();
            if (State.Deck.WarlordId != null) shown.Add(State.Find(State.Deck.WarlordId));
            if (State.Deck.DefensiveId != null) shown.Add(State.Find(State.Deck.DefensiveId));
            foreach (var id in State.Deck.CardIds) shown.Add(State.Find(id));
            shown.RemoveAll(x => x == null);
            return shown;
        }

        void RefreshDeckList()
        {
            bool cards = _tab == 0, info = _tab == 1;
            var shown = DeckEntries();

            float maxScroll = Mathf.Max(0f, shown.Count * RowPitch - ListH);
            _deckScroll = Mathf.Clamp(_deckScroll, 0f, maxScroll);
            int firstRow = Mathf.FloorToInt(_deckScroll / RowPitch);
            float off = _deckScroll - firstRow * RowPitch;

            for (int i = 0; i < RowVisible; i++)
            {
                bool on = cards && (firstRow + i) < shown.Count;
                SetOn(_deckRowBg[i], on); SetOn(_deckRowGrad[i], on);
                SetOn(_deckRowBorder[i], on); SetOn(_deckRowCntIc[i], on);
                SetOn(_deckRowCost[i], on);
                SetOn(_deckRowName[i], on); SetOn(_deckRowCount[i], on);
                if (!on) continue;

                var def = shown[firstRow + i];
                float y = ListY + i * RowPitch - off;
                MoveRow(i, y);
                _deckRowName[i].SetText(CardText.Name(def.Name, def.NameZh));
                int n = (def.Type == "hero" || def.Type == "defence") ? 1 : State.Deck.CountOf(def.Id);
                _deckRowCount[i].SetText(n.ToString());
                _deckRowGrad[i].SetTint(RarityTint(def.Rarity));
            }

            SetOn(_emptyWarn, cards && shown.Count == 0);
            if (_emptyWarnGo != null) _emptyWarnGo.SetActive(cards && shown.Count == 0);
            RefreshTabVisibility();

            var curve = State.CostCurve();
            for (int c = 0; c < _curveFill.Count; c++)
            {
                int n = c < curve.Length ? curve[c] : 0;
                SetQuadWidth(_curveFill[c], Mathf.Clamp01(n / 10f) * CurveBarW);   // 原版按 10 张归一化
                _curveLabel[c * 2 + 1].SetText(n.ToString());
            }
        }

        List<ImageQuad> AllRowParts(int i) { return null; }     // 已废弃（改用显式列表，见上）
        readonly Dictionary<string, ImageQuad> _named = new Dictionary<string, ImageQuad>();
        ImageQuad Lookup(string key) { ImageQuad q; return _named.TryGetValue(key, out q) ? q : null; }

        void MoveRow(int i, float y)
        {
            float cy = y + RowH * 0.5f;
            Move(_deckRowBg[i], ListX + RowW * 0.5f, cy, QPanel);
            Move(_deckRowGrad[i], ListX + RowW * 0.5f, cy, QGrad);
            Move(_deckRowBorder[i], ListX + RowW * 0.5f, cy, QBorder);
            Move(_deckRowCost[i], RowCostX + RowCostS * 0.5f, cy, QRow);
            Move(_deckRowCntIc[i], 258f + 20f, y + 8f + (RowH - 16f) * 0.5f, QRow);
            MoveLabel(_deckRowName[i], 62f + 95f, cy, QText);
            MoveLabel(_deckRowCount[i], RowCostX + RowCostS * 0.5f, cy, QText);
        }

        void Move(ImageQuad q, float cx, float cy, int queue)
        {
            if (q == null) return;
            q.transform.localPosition = Pos(cx, cy);
            q.SetRenderQueue(queue);
        }
        void MoveLabel(Label l, float cx, float cy, int queue)
        {
            if (l == null) return;
            l.transform.localPosition = Pos(cx, cy);
            l.SetRenderQueue(queue);
        }
        static void SetOn(Component c, bool on) { if (c != null) c.gameObject.SetActive(on); }

        void RefreshHeader()
        {
            _deckNameText.SetText(State.Deck.Name);
            _deckNameHint.gameObject.SetActive(string.IsNullOrEmpty(State.Deck.Name));
            _title.gameObject.SetActive(!_filtersOpen);

            for (int i = 0; i < 3; i++)
            {
                bool cur = i == _tab;
                if (_tabHi[i] != null) _tabHi[i].SetTint(cur ? Color.white : new Color(1f, 1f, 1f, 0.15f));
                _tabLabel[i].SetColor(cur ? Gold : Ink);
            }

            int total = State.DeckCount + (State.Deck.WarlordId != null ? 1 : 0)
                      + (State.Deck.DefensiveId != null ? 1 : 0);
            _counterTxt.SetText(total + "/" + (State.MaxDeckCount + 2));
            var err = State.Validate();
            _verdict.SetText(err == DeckError.None ? "合法" : DeckRules.Describe(err));
            _verdict.SetColor(err == DeckError.None ? new Color(0.5f, 0.9f, 0.5f) : new Color(0.95f, 0.6f, 0.4f));
            SetOn(_doneHl, err == DeckError.None);          // 原版 `Done Highlight` 就是这个开关
            _storeErr.SetText(Library.LastError ?? "");

            // Wildcard 计数 = 卡池里四个稀有度各有几张（单机全解锁 ⇒ 就是卡池的分布）
            int[] rc = new int[4];
            foreach (var c in State.Pool)
            {
                if (c.Rarity == "common") rc[0]++;
                else if (c.Rarity == "rare") rc[1]++;
                else if (c.Rarity == "epic") rc[2]++;
                else if (c.Rarity == "legendary") rc[3]++;
            }
            for (int i = 0; i < 4; i++) _wcTxt[i].SetText(rc[i].ToString());

            _poolInfo.SetText("卡池 " + State.VisibleCards().Count + " / " + State.PoolCount + " 张");
        }

        void Say(string s)
        {
            _noticeText = s ?? "";
            if (_notice != null) _notice.SetText(_noticeText);
            _noticeUntil = Time.unscaledTime + 2.5f;
        }

        // ============================================================ 交互：鼠标

        void HandlePointer()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 sp = mouse.position.ReadValue();
            Vector3 wp = LayoutSpace.ScreenToWorld(sp, _cam);
            Vector2 px = ToPx(wp);

            bool downL = mouse.leftButton.wasPressedThisFrame;
            bool downR = mouse.rightButton.wasPressedThisFrame;

            // ---- 拖拽中：让那一行跟着鼠标，松开时判定 ----
            if (_dragging && _dragQuad != null)
            {
                _dragQuad.transform.position = new Vector3(wp.x, wp.y, _dragQuad.transform.position.z);
                if (_dragText != null) _dragText.transform.position = new Vector3(wp.x, wp.y, _dragText.transform.position.z);
                if (!_draggingMoved && Mathf.Abs(wp.x - _dragOrigin.x) > 0.15f) _draggingMoved = true;
                if (mouse.leftButton.wasReleasedThisFrame || !mouse.leftButton.isPressed) { EndDrag(px.x); return; }
                return;
            }

            if (!downL && !downR) return;

            // 导入弹窗是**模态** —— 开着的时候只认它（点别处不穿透）
            if (_importOpen) { if (downL) HandleButtons(px); return; }

            // 筛选栏**盖住侧栏**（两者是同一块 rect）⇒ 它开着的时候先问它、并且不许穿透
            if (_filtersOpen && px.x < FltX + FltW)
            {
                if (downL) { HandleFilterClick(px); return; }
                return;
            }

            if (HandlePoolClick(wp, px, downR)) return;
            if (HandleDeckRowClick(wp, px, downL)) return;
            if (downL && HandleButtons(px)) return;
        }

        /// <summary>卡池：**左键放大 / 右键加牌**（照原版 `DeckEditingWindow__OnCardClick.c:16,42`）。</summary>
        bool HandlePoolClick(Vector3 wp, Vector2 px, bool right)
        {
            if (px.x < PoolX || px.x > PoolX + PoolW) return false;
            for (int i = 0; i < _poolViews.Count; i++)
            {
                var v = _poolViews[i];
                if (!v.gameObject.activeSelf || i >= _poolIndex.Count || _poolIndex[i] < 0) continue;
                if (!v.Contains(wp)) continue;
                var def = State.VisibleAt(_poolIndex[i]);
                if (def == null) return true;
                if (right) TryAddCard(def);
                else OpenCardWindow(def);
                return true;
            }
            return false;
        }

        /// <summary>卡组条目：**左键放大**（原版**不是删除**）；按住拖出侧栏 = 删除。</summary>
        bool HandleDeckRowClick(Vector3 wp, Vector2 px, bool left)
        {
            if (_tab != 0) return false;
            if (px.x > ListX + RowW || px.y < ListY || px.y > ListY + ListH) return false;
            for (int i = 0; i < _deckRowBg.Count; i++)
            {
                var q = _deckRowBg[i];
                if (q == null || !q.gameObject.activeSelf || !Hit(q, wp)) continue;
                if (!left) return true;
                var shown = DeckEntries();
                int idx = Mathf.FloorToInt(_deckScroll / RowPitch) + i;
                if (idx >= shown.Count) return true;
                StartRowDrag(idx, i, wp);
                return true;
            }
            return false;
        }

        /// <summary>记下拖拽起点。**鼠标与自检走同一个函数**（删牌那条路只有「拖出」一条，必须验到）。</summary>
        void StartRowDrag(int deckIndex, int viewRow, Vector3 fromWorld)
        {
            _dragRow = deckIndex;
            _dragQuad = _deckRowBg[viewRow];
            _dragText = _deckRowName[viewRow];
            _dragOrigin = fromWorld;
            _dragging = true;
            _draggingMoved = false;
        }

        void EndDrag(float screenX)
        {
            _dragging = false;
            int row = _dragRow;
            _dragQuad = null; _dragText = null; _dragRow = -1;

            // 没怎么动 = 点击 ⇒ 弹放大窗（照原版）
            if (!_draggingMoved)
            {
                var shown0 = DeckEntries();
                if (row >= 0 && row < shown0.Count) OpenCardWindow(shown0[row]);
                RefreshDeckList();
                return;
            }
            // 拖出侧栏 = 删除（照原版 `DeckEditingPanel__CheckCardSlotDrag.c:59,66`）
            // 🔴 `screenX` 是**设计坐标（1920 宽）**，不是屏幕像素 —— 传进来的是 `ToPx()` 的结果。
            //    原来这里又乘了一次 `Screen.width / ScreenW`（那是给真实像素用的）⇒
            //    批处理下 `Screen.width` 不是 1920，阈值被缩小，**拖到侧栏里面也会删**
            //    （自检抓到：往 x=150 拖，卡组照样少一张）。
            if (screenX > ListX + RowW)
            {
                var shown = DeckEntries();
                if (row >= 0 && row < shown.Count)
                {
                    var def = shown[row];
                    if (State.TryRemove(def)) { CommitDeck(); Say("已移出卡组：" + CardText.Name(def.Name, def.NameZh)); }
                }
            }
            RefreshAll();
        }

        bool HandleButtons(Vector2 px)
        {
            if (HitBtn("hdr_filters", px)) { ToggleFilters(); return true; }
            if (HitBtn("hdr_clear", px)) { ClearFilters(); return true; }
            if (HitBtn("hdr_back", px)) { SaveAndSay(); return true; }
            if (HitBtn("foot_done", px)) { SaveAndSay(); return true; }
            if (HitBtn("name_box", px)) { BeginNameEdit(); return true; }
            if (HitBtn("name_clear", px)) { State.SetDeckName("新卡组"); CommitDeck(); RefreshHeader(); return true; }
            if (HitBtn("info_share", px)) { ShareDeckString(); return true; }
            if (HitBtn("info_import", px)) { OpenImport(); return true; }
            if (HitBtn("imp_input", px)) { _nameEdit = _importText ?? ""; _editKind = 3; RefreshImportText(); return true; }
            if (HitBtn("imp_ok", px)) { TryImport(); return true; }
            if (HitBtn("imp_close", px)) { CloseImport(); return true; }
            for (int i = 0; i < 3; i++)
                if (HitBtn("tab_" + i, px)) { SetTab(i); return true; }
            return false;
        }

        void SaveAndSay() { CommitDeck(); Say("已保存"); }

        // ============================================================ 分享 / 导入

        /// <summary>分享 = **把卡组串写进系统剪贴板**（原版 `DeckInfoPopup__ShareDeck.c:16`
        /// 就一句 `GUIUtility.systemCopyBuffer = MakeDeckString()`）。
        /// ⚠️ 原版**只有写、没有读**（`get_systemCopyBuffer` 全库 0 命中）⇒ 我们也不做「一键从剪贴板导入」。</summary>
        void ShareDeckString()
        {
            var s = DeckLibrary.ExportString(State.Deck);
            GUIUtility.systemCopyBuffer = s;
            Say("卡组串已复制到剪贴板（" + s.Length + " 字符）");
        }

        void OpenImport()
        {
            _importOpen = true; _importText = ""; _importError = "";
            RefreshImportText();
            foreach (var go in _modalOnly) if (go != null) go.SetActive(true);
            Say("把卡组串粘进输入框（Ctrl+V），再点 Confirm");
        }

        void CloseImport()
        {
            _importOpen = false;
            if (_editKind == 3) { _nameEdit = null; _editKind = 0; _typed.Clear(); }
            foreach (var go in _modalOnly) if (go != null) go.SetActive(false);
        }

        /// <summary>走**原版那条链**：`Split(';').Last()` → `DeserializeDeckString`
        /// （我们这边是 `DeckLibrary.ImportString`，格式逐字对齐）。失败要给**人话**。</summary>
        public bool TryImport()
        {
            var deck = DeckLibrary.ImportString(_importText, State.Find);
            if (deck == null)
            {
                _importError = string.IsNullOrWhiteSpace(_importText) ? "先粘贴卡组串" : "这不是一条合法的卡组串";
                RefreshImportText();
                return false;
            }
            CloseImport();
            Library.Add(deck);
            State.LoadDeck(Library.Current);
            RefreshAll();
            Say("已导入「" + deck.Name + "」" +
                (DeckLibrary.LastDroppedIds.Count > 0
                 ? "（有 " + DeckLibrary.LastDroppedIds.Count + " 张卡在我们卡池里没有，已按原版丢掉）" : ""));
            return true;
        }

        void RefreshImportText()
        {
            if (_impInputTx == null) return;
            bool empty = _importText.Length == 0;
            _impInputTx.SetText(empty ? "Enter text..." : _importText);
            _impInputTx.SetColor(empty ? new Color(1f, 1f, 1f, 0.5f) : Ink);
            if (_impErr != null) _impErr.SetText(_importError ?? "");
        }

        /// <summary>页签决定「侧栏那三组东西谁显示」：卡组行(Cards) / 费用曲线+动作钮(Deck info) / 饰品空态(Cosmetics)。</summary>
        void RefreshTabVisibility()
        {
            bool info = _tab == 1, cosm = _tab == 2;
            foreach (var go in _infoOnly) if (go != null) go.SetActive(info);
            foreach (var go in _cosmOnly) if (go != null) go.SetActive(cosm);
        }

        // ============================================================ 自检入口
        // 🔴 批处理里**没有真鼠标** ⇒ 自检从这几个口子直接驱动状态。
        //    鼠标回调调的是**同一个函数**（`HandleButtons`/`HandleScroll`/`HandleFilterRow` 都转发到这里）
        //    —— 「两处写同一条规则 = 迟早不一致」。
        public bool FiltersOpen { get { return _filtersOpen; } }
        public int ActiveTab { get { return _tab; } }
        public float PoolScrollPx { get { return _poolScroll; } }
        public float DeckScrollPx { get { return _deckScroll; } }
        public float MaxPoolScrollPx
        {
            get
            {
                int rows = Mathf.Max(1, Mathf.CeilToInt(State.VisibleCards().Count / (float)PoolCols));
                return Mathf.Max(0f, rows * PoolRowPitch - 16f - PoolH + PoolPadY * 2f);
            }
        }
        public float MaxDeckScrollPx { get { return Mathf.Max(0f, DeckEntries().Count * RowPitch - ListH); } }

        public void UiToggleFilters() { ToggleFilters(); }
        public void UiSetTab(int t) { SetTab(t); }
        public void UiScrollPool(float dy) { _poolScroll = Mathf.Max(0f, _poolScroll + dy); RefreshPool(); RefreshHeader(); }
        public void UiScrollDeck(float dy) { _deckScroll = Mathf.Max(0f, _deckScroll + dy); RefreshDeckList(); }
        public void UiClearFilters() { ClearFilters(); }
        public void UiFilterRow(string key) { HandleFilterRow(key); }
        public void UiAddCard(CardDef d) { TryAddCard(d); }
        public void UiBeginNameEdit() { BeginNameEdit(); }
        /// <summary>改名：批处理下没有键盘 ⇒ 直接把缓冲设进去再走「提交」那条路（和回车是同一个函数）。</summary>
        public void UiCommitName(string name)
        {
            _nameEdit = name ?? ""; _editKind = 1;
            EndTextEdit(true);
        }

        /// <summary>通用「输入完回车」：改名(1) / 卡名筛选(2) / 导入框(3) 都走它。</summary>
        public void UiCommitEdit(string text)
        {
            if (_editKind == 0) return;
            _nameEdit = text ?? "";
            EndTextEdit(true);
        }

        /// <summary>通用「按 ESC 取消」。</summary>
        public void UiCancelEdit() { if (_editKind != 0) EndTextEdit(false); }

        /// <summary>卡池第 i 个格子现在显示的是哪张卡（没显示给 null）。</summary>
        public CardDef UiPoolCardAt(int view)
        {
            if (view < 0 || view >= _poolIndex.Count || _poolIndex[view] < 0) return null;
            return State.VisibleAt(_poolIndex[view]);
        }

        /// <summary>卡组列表第 i 行现在**显示**的是哪张卡（那一行不可见就给 null —— 页签不对也算不可见）。</summary>
        public CardDef UiDeckRowAt(int i)
        {
            if (_tab != 0) return null;
            var shown = DeckEntries();
            int idx = Mathf.FloorToInt(_deckScroll / RowPitch) + i;
            return (i >= 0 && i < RowVisible && idx >= 0 && idx < shown.Count) ? shown[idx] : null;
        }

        /// <summary>某个具名图在不在（自检用来点验「Header/页签/Footer 这些件真的建了」）。</summary>
        public bool UiHasQuad(string key) { return Lookup(key) != null; }

        /// <summary>某个具名图**显示出来了没有**（`UiHasQuad` 只问建没建）。</summary>
        public bool UiQuadActive(string key) { var q = Lookup(key); return q != null && q.gameObject.activeSelf; }

        /// <summary>某个可点矩形的 px 位置（自检比版面用）。</summary>
        public bool UiBtnRect(string key, out float x, out float y, out float w, out float h)
        {
            foreach (var b in _btns)
                if (b.Key == key) { x = b.X; y = b.Y; w = b.W; h = b.H; return true; }
            x = y = w = h = 0f; return false;
        }

        /// <summary>具名图的 px 中心与尺寸（自检比版面用）。</summary>
        public bool UiQuadRect(string key, out float cx, out float cy, out float w, out float h)
        {
            var q = Lookup(key);
            if (q == null) { cx = cy = w = h = 0f; return false; }
            var p = ToPx(q.transform.localPosition);
            cx = p.x; cy = p.y; w = q.WorldW * PxPerUnit; h = q.WorldH * PxPerUnit;
            return true;
        }

        /// <summary>费用曲线那 9 根柱子现在可不可见（Deck info 页签才显示）。</summary>
        public bool UiCurveVisible { get { return _curveFill.Count > 0 && _curveFill[0] != null && _curveFill[0].gameObject.activeSelf; } }

        /// <summary>🔴 用**合成坐标**走一遍鼠标那条路（批处理没有真鼠标）。
        /// `Ui*()` 那组只驱动状态、**验不到命中矩形**；这条专门验「点在哪儿、命中谁」。</summary>
        public bool UiClickPx(float x, float y)
        {
            var px = new Vector2(x, y);
            if (_importOpen) return HandleButtons(px);
            if (_filtersOpen && px.x < FltX + FltW) return HandleFilterClick(px);
            return HandleButtons(px);
        }

        public bool ImportOpen { get { return _importOpen; } }
        public string ImportText { get { return _importText; } }
        public string ImportError { get { return _importError; } }
        public void UiOpenImport() { OpenImport(); }
        public void UiSetImportText(string s) { _importText = s ?? ""; RefreshImportText(); }
        public bool UiTryImport() { return TryImport(); }
        public void UiCloseImport() { CloseImport(); }
        public string UiShareString() { return DeckLibrary.ExportString(State.Deck); }
        /// <summary>Cosmetics 页签那块空态显示出来了没有。</summary>
        public bool UiCosmeticsVisible { get { return _cosmOnly.Count > 0 && _cosmOnly[0] != null && _cosmOnly[0].activeSelf; } }
        /// <summary>Deck info 页签那两颗动作钮显示出来了没有。</summary>
        public bool UiInfoActionsVisible { get { var q = Lookup("info_import"); return q != null && q.gameObject.activeSelf; } }
        public bool UiModalVisible { get { var q = Lookup("imp_bg"); return q != null && q.gameObject.activeSelf; } }

        /// <summary>🔴 **「该藏的东西藏住了吗」的通用守卫**（图 + 文字都算）。
        /// 为什么单开一条：`Lookup()` 只认 `ImageQuad` ⇒ 只验图的话**文字会漏**，
        /// 而漏了文字的表现是「别的页签上飘着一行字」—— 2026-09-20 就是这么漏的。</summary>
        public int UiInfoOnlyActive { get { int n = 0; foreach (var g in _infoOnly) if (g != null && g.activeSelf) n++; return n; } }
        public int UiCosmOnlyActive { get { int n = 0; foreach (var g in _cosmOnly) if (g != null && g.activeSelf) n++; return n; } }
        public int UiModalActive { get { int n = 0; foreach (var g in _modalOnly) if (g != null && g.activeSelf) n++; return n; } }
        /// <summary>正在编辑文本（改名 / 卡名筛选 / 导入框）。</summary>
        public int UiEditKind { get { return _editKind; } }

        /// <summary>🔴 模拟「按住卡组第 <paramref name="viewRow"/> 行 → 拖到屏幕 x = <paramref name="toScreenX"/> → 松开」。
        /// **走的是鼠标那条路**（`StartRowDrag` → `EndDrag`），批处理没有真鼠标、这是唯一能验到
        /// 「拖出侧栏 = 删除」的办法（删牌只有这一条路，原版 `DeckEditingPanel__CheckCardSlotDrag.c:59,66`）。</summary>
        public bool UiDragDeckRow(int viewRow, float toScreenX)
        {
            var shown = DeckEntries();
            int idx = Mathf.FloorToInt(_deckScroll / RowPitch) + viewRow;
            if (viewRow < 0 || viewRow >= RowVisible || idx >= shown.Count) return false;
            var q = _deckRowBg[viewRow];
            if (q == null || !q.gameObject.activeSelf) return false;
            StartRowDrag(idx, viewRow, q.transform.position);
            _draggingMoved = true;                 // 模拟「拖动过阈值」⇒ 松开时按拖出处理
            EndDrag(toScreenX);
            return true;
        }

        /// <summary>模拟「点一下卡组第 viewRow 行」（不动 ⇒ 松开时弹放大窗，照原版）。</summary>
        public bool UiClickDeckRow(int viewRow)
        {
            var shown = DeckEntries();
            int idx = Mathf.FloorToInt(_deckScroll / RowPitch) + viewRow;
            if (viewRow < 0 || viewRow >= RowVisible || idx >= shown.Count) return false;
            var q = _deckRowBg[viewRow];
            if (q == null || !q.gameObject.activeSelf) return false;
            StartRowDrag(idx, viewRow, q.transform.position);
            EndDrag(q.transform.position.x);       // 没动过 ⇒ 走「点击」那一支
            return true;
        }
        /// <summary>放大窗现在开着没有（`UiClickDeckRow` / 池卡左键都会开它）。</summary>
        public bool UiCardWindowVisible { get { return _cardWindow != null && _cardWindow.Visible; } }
        public string UiCardWindowTitle { get { return _cardWindow != null ? _cardWindow.ShownTitle : null; } }
        public void UiCloseCardWindow() { if (_cardWindow != null) _cardWindow.Hide(); }

        void ToggleFilters()
        {
            _filtersOpen = !_filtersOpen;
            RefreshHeader(); RefreshFilters();
        }

        void SetTab(int t)
        {
            _tab = Mathf.Clamp(t, 0, 2);
            _deckScroll = 0f;
            RefreshDeckList(); RefreshHeader();
        }

        // ============================================================ 交互：滚轮

        void HandleScroll()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            float dy = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(dy) < 1f) return;
            Vector2 px = ToPx(LayoutSpace.ScreenToWorld(mouse.position.ReadValue(), _cam));
            float step = dy * 0.4f;

            if (_filtersOpen && px.x < FltX + FltW) { _fltScroll = Mathf.Max(0f, _fltScroll - step); RefreshFilters(); }
            else if (px.x < SideBgX + SideBgW) { _deckScroll = Mathf.Max(0f, _deckScroll - step); RefreshDeckList(); }
            else { _poolScroll = Mathf.Max(0f, _poolScroll - step); RefreshPool(); RefreshHeader(); }
        }

        // ============================================================ 交互：键盘（改名）

        public bool NameEditing { get { return _nameEdit != null; } }
        public string NameBuffer { get { return _nameEdit; } }

        void BeginNameEdit() { _nameEdit = State.Deck.Name ?? ""; _editKind = 1; Say("改名中：输入后回车确认，ESC 取消"); }
        /// <summary>卡名筛选的输入框（原版 `CardNameFilter` 用 `TMP_InputField`）。</summary>
        void BeginFilterNameEdit() { _nameEdit = State.Filter.Name ?? ""; _editKind = 2; RefreshFilters(); Say("筛选卡名：输入后回车确认，ESC 取消"); }

        void HandleTyping()
        {
            var kb = Keyboard.current;
            if (kb == null || _nameEdit == null) return;

            if (kb.escapeKey.wasPressedThisFrame) { EndTextEdit(false); return; }
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) { EndTextEdit(true); return; }
            if (kb.backspaceKey.wasPressedThisFrame && _nameEdit.Length > 0)
                _nameEdit = _nameEdit.Substring(0, _nameEdit.Length - 1);

            // `onTextInput` 是 InputSystem 的回调，拿得到已按本地化处理的字符（中文输入法也能进来）
            for (int i = 0; i < _typed.Count; i++) if (_nameEdit.Length < 24) _nameEdit += _typed[i];
            _typed.Clear();

            if (_editKind == 1)
            {
                _deckNameHint.gameObject.SetActive(false);
                _deckNameText.SetText(_nameEdit + "_");
            }
            else if (_editKind == 3) { _importText = _nameEdit; RefreshImportText(); }
            else RefreshFilters();
        }
        readonly List<char> _typed = new List<char>();

        /// <summary>结束文本编辑。**提交与取消走同一个函数** —— 两条路各写一份迟早不一致。</summary>
        void EndTextEdit(bool commit)
        {
            int kind = _editKind;
            string buf = _nameEdit ?? "";
            _nameEdit = null; _editKind = 0;
            _typed.Clear();

            if (kind == 1)
            {
                if (commit)
                {
                    if (State.SetDeckName(buf)) { CommitDeck(); Say("卡组名已改"); }
                    else Say("名字不能是空的");
                }
                else Say("已取消改名");
                RefreshHeader();
            }
            else if (kind == 2)
            {
                var f = State.Filter;
                f.Name = commit ? buf.Trim() : f.Name;
                if (commit) ApplyFilter(f); else RefreshFilters();
                if (commit) Say(string.IsNullOrEmpty(f.Name) ? "已清空卡名筛选" : "卡名筛选：" + f.Name);
            }
            else if (kind == 3)
            {
                if (commit) _importText = buf.Trim();
                RefreshImportText();
            }
        }

        void OnEnable() { if (Keyboard.current != null) Keyboard.current.onTextInput += OnText; }
        void OnDisable() { if (Keyboard.current != null) Keyboard.current.onTextInput -= OnText; }
        void OnText(char c) { if (_nameEdit != null && !char.IsControl(c)) _typed.Add(c); }

        // ============================================================ 筛选栏

        void ClearFilters()
        {
            // 清空 = 回到「按卡组自动筛选」那套（`CardCollectionFilterController__SetFiltersToDeck.c:29`）
            State.SetFilter(DeckFilter.None);
            _fltScroll = 0f;
            RefreshPool(); RefreshFilters(); RefreshHeader();
            Say("已清空筛选");
        }

        void ApplyFilter(DeckFilter f)
        {
            State.SetFilter(f);
            _poolScroll = 0f;
            RefreshPool(); RefreshFilters(); RefreshHeader();
        }

        /// <summary>筛选栏内容按当前 `_fltScroll` 摆放。**刷新时重摆**（行数少，省掉一套脏标记）。</summary>
        void RefreshFilters()
        {
            SetOn(_fltPanelBg, _filtersOpen);
            SetOn(_fltPanelShadow, _filtersOpen);
            _fltRows.Clear();

            float x = FltX + FltPad, w = FltW - FltPad * 2f;
            float y = 10f;

            AddRow("$h:Name", "Name", false, x, y, w, 24f); y += 26f;
            AddRow("$name",
                   _editKind == 2 ? (_nameEdit ?? "") + "_"
                                  : (string.IsNullOrEmpty(State.Filter.Name) ? "搜索卡名…" : State.Filter.Name),
                   _editKind == 2, x, y, w, FltRowH); y += FltRowH + 6f;

            // Owned / Upgradable（原版两个开关；单机**全解锁** ⇒ Owned 恒真，
            //   Upgradable 没有升级系统 ⇒ 点了会**明说没实现**，不装作有）
            AddRow("$owned", (_ownedOnly ? "☑ " : "☐ ") + "Owned only", _ownedOnly, x, y, w, FltRowH);
            y += FltRowH + 2f;
            AddRow("$upgradable", "☐ Upgradable only（单机版没有升级系统）", false, x, y, w, FltRowH);
            y += FltRowH + 6f;

            AddRow("$h:Army", "Army", false, x, y, w, 24f); y += 26f;
            var facs = State.Factions();
            for (int i = 0; i < facs.Count; i++)
            {
                float rx = x + (i % 2) * (w * 0.5f), ry = y + (i / 2) * 36f;
                bool on = State.Filter.Faction == facs[i];
                AddRow("$fac:" + facs[i], (on ? "● " : "○ ") + CardText.Faction(facs[i]), on,
                       rx, ry, w * 0.5f - 4f, 34f);
            }
            y += Mathf.CeilToInt(facs.Count / 2f) * 36f + 6f;

            AddRow("$h:Rarity", "Rarity", false, x, y, w, 24f); y += 26f;
            string[] rar = { "common", "rare", "epic", "legendary", "special" };
            for (int i = 0; i < rar.Length; i++)
            {
                bool on = State.Filter.Rarity == rar[i];
                AddRow("$rar:" + rar[i], (on ? "● " : "○ ") + rar[i], on, x + (i % 3) * (w / 3f), y + (i / 3) * 36f, w / 3f - 4f, 34f);
            }
            y += 2 * 36f + 6f;

            AddRow("$h:Energy Cost", "Energy Cost", false, x, y, w, 24f); y += 26f;
            var costs = State.Costs();
            for (int i = 0; i < costs.Count; i++)
            {
                bool on = State.Filter.Cost == costs[i];
                AddRow("$cost:" + costs[i], costs[i].ToString(), on, x + (i % 10) * (w / 10f), y, w / 10f - 3f, 30f);
            }
            y += 36f;

            AddRow("$h:Type", "Type", false, x, y, w, 24f); y += 26f;
            string[,] types = { { "hero", "Warlord" }, { "unit", "Troops" }, { "tactic", "Tactics" }, { "defence", "Defensive" } };
            for (int i = 0; i < 4; i++)
            {
                bool on = State.Filter.Type == types[i, 0];
                AddRow("$type:" + types[i, 0], (on ? "● " : "○ ") + types[i, 1], on,
                       x + (i % 2) * (w * 0.5f), y + (i / 2) * 34f, w * 0.5f - 4f, 32f);
            }
            y += 2 * 34f;
            _fltContentH = y;

            LayoutFilterRows();
        }

        float _fltContentH;

        void AddRow(string key, string label, bool on, float x, float y, float w, float h)
        {
            _fltRows.Add(new FilterRow { Key = key, Label = label, On = on, X = x, Y = y, W = w, H = h });
        }

        /// <summary>把 `_fltRows` 摆到屏幕上（不够就新建行物件，多了就藏起来）。</summary>
        void LayoutFilterRows()
        {
            var item = Ui("40K_dropdown_item");
            while (_fltBg.Count < _fltRows.Count)
            {
                int k = _fltBg.Count;
                var q = item != null ? ImageQuad.Create(Root, item, Vector3.zero, 0.1f, new Vector2(0.5f, 0.5f), "flt_b" + k) : null;
                if (q != null) q.SetRenderQueue(QFltRow);
                var t = Label.Create(Root, "", Vector3.zero, 1, Ink, new Vector2(0.5f, 0.5f), "flt_t" + k);
                if (t != null) t.SetRenderQueue(QFltText);
                _fltBg.Add(q); _fltTx.Add(t);
            }

            for (int i = 0; i < _fltBg.Count; i++)
            {
                bool on = _filtersOpen && i < _fltRows.Count && _fltScroll < FltH + 200f;
                var r = i < _fltRows.Count ? _fltRows[i] : default(FilterRow);
                float cy = FltY + r.Y + r.H * 0.5f - _fltScroll;
                bool head = on && r.Key.StartsWith("$h:");
                if (_fltBg[i] != null)
                {
                    _fltBg[i].gameObject.SetActive(on && !head);
                    _fltBg[i].transform.localPosition = Pos(r.X + r.W * 0.5f, cy) + new Vector3(0f, 0f, QFltRow);
                    _fltBg[i].SetWorldHeight(U(r.H));
                    _fltBg[i].SetAspect(r.W / Mathf.Max(1f, r.H));
                    _fltBg[i].SetTint(r.On ? new Color(1f, 1f, 1f, 0.95f) : new Color(1f, 1f, 1f, 0.5f));
                }
                _fltTx[i].gameObject.SetActive(on);
                _fltTx[i].transform.localPosition = Pos(r.X + r.W * 0.5f, cy) + new Vector3(0f, 0f, QFltText);
                _fltTx[i].SetText(r.Label);
                _fltTx[i].SetColor(r.On ? Gold : Ink);
            }
        }

        bool HandleFilterClick(Vector2 px)
        {
            float lx = px.x - FltX, ly = px.y - FltY + _fltScroll;
            foreach (var r in _fltRows)
            {
                if (lx < r.X - FltX || lx > r.X - FltX + r.W) continue;
                if (ly < r.Y || ly > r.Y + r.H) continue;
                HandleFilterRow(r.Key);
                return true;
            }
            return false;
        }

        void HandleFilterRow(string key)
        {
            if (key.StartsWith("$h:")) return;
            if (key == "$owned")
            {
                _ownedOnly = !_ownedOnly;
                Say("全部卡牌均已拥有（单机版不做开包）⇒ 这个开关不改变结果");
                RefreshFilters(); return;
            }
            if (key == "$upgradable") { Say("Upgradable only：单机版没有升级系统（不装作有）"); return; }
            if (key == "$name") { BeginFilterNameEdit(); return; }

            var f = State.Filter;
            if (key.StartsWith("$fac:")) { var v = key.Substring(5); f.Faction = f.Faction == v ? "" : v; }
            else if (key.StartsWith("$rar:")) { var v = key.Substring(5); f.Rarity = f.Rarity == v ? "" : v; }
            else if (key.StartsWith("$cost:")) { int v = int.Parse(key.Substring(6)); f.Cost = f.Cost == v ? -1 : v; }
            else if (key.StartsWith("$type:")) { var v = key.Substring(6); f.Type = f.Type == v ? "" : v; }
            else return;
            ApplyFilter(f);
        }

        // ============================================================ 卡组 / 卡

        void TryAddCard(CardDef def)
        {
            string why;
            if (State.TryAdd(def, out why)) { CommitDeck(); RefreshAll(); Say("已加入：" + CardText.Name(def.Name, def.NameZh)); }
            else Say(why);
        }

        /// <summary>写回卡组库（原版是 `syncedToServer=false` 标脏 + Done 时才上传；我们单机直接落盘）。</summary>
        public void CommitDeck()
        {
            Library.CommitCurrent(State.Deck);
            RefreshHeader();
        }

        CardDisplayWindow _cardWindow;

        void OpenCardWindow(CardDef def)
        {
            if (def == null) return;
            // 复用对战的放大窗（查证：原版放大窗与战斗里那个是同一套 `CardDisplayWindow`）
            if (_cardWindow == null) _cardWindow = CardDisplayWindow.Create(Root);
            _cardWindow.Show(BattleDriver.ToCardData(def, def.Faction));
        }

        // ============================================================ 命中判定

        /// <summary>世界坐标 → 原版像素坐标（1920×1080，y 向下）。</summary>
        public Vector2 ToPx(Vector3 world)
        {
            return new Vector2(world.x * PxPerUnit + ScreenW * 0.5f, ScreenH * 0.5f - world.y * PxPerUnit);
        }

        bool Hit(ImageQuad q, Vector3 wp)
        {
            if (q == null || !q.gameObject.activeSelf) return false;
            var p = q.transform.position;
            return Mathf.Abs(wp.x - p.x) <= q.WorldW * 0.5f && Mathf.Abs(wp.y - p.y) <= q.WorldH * 0.5f;
        }

        void Btn_(string key, float x, float y, float w, float h)
        {
            _btns.Add(new Btn { Key = key, X = x, Y = y, W = w, H = h });
        }

        bool HitBtn(string key, Vector2 px)
        {
            foreach (var b in _btns)
                if (b.Key == key && px.x >= b.X && px.x <= b.X + b.W && px.y >= b.Y && px.y <= b.Y + b.H)
                    return true;
            return false;
        }

        // ============================================================ 建图工具

        public Vector3 Pos(float px, float py)
        {
            return new Vector3((px - ScreenW * 0.5f) / PxPerUnit, (ScreenH * 0.5f - py) / PxPerUnit, 0f);
        }

        public float U(float px) { return px / PxPerUnit; }

        /// <summary>原版 UI 图。名字 = sprite 名（空格换下划线），见 `工具/sync_battle_ui_art.py`。
        /// ⚠️ **两个目录都要找**：卡组编辑这批在 `Art/ui_deck/`，但有几张（`40K_button` /
        /// `UI_Button_Round_background` / `40k_bt_close`）是**战斗那批**、在 `Art/ui/` 里 ——
        /// 只查 ui_deck 会误报「缺图」（自检抓到过）。
        /// **取不到时记进 `_missingArt`，建完之后统一报警**（不许静默变空白）。</summary>
        Texture2D Ui(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.DeckUi(name);
            if (t == null) t = CardArt.Ui(name);
            if (t == null) _missingArt.Add(name);
            return t;
        }

        /// <summary>按**左上角 + 宽高**摆一张图（和权威坐标表同序，抄表不会抄错）。
        /// ⚠️ 原版 rect 的比例和源图常不一样（例：行底源图 462×62、显示 325×55.7）⇒ 必须强制宽高比。</summary>
        ImageQuad Img(string key, string sprite, float x, float y, float w, float h, int q)
        {
            return Img(key, Ui(sprite), x, y, w, h, q);
        }

        ImageQuad Img(string key, Texture2D tex, float x, float y, float w, float h, int queue)
        {
            if (tex == null) return null;      // 缺图由 `Ui()` 记账（别在这里按 key 再记一次）
            var q = ImageQuad.Create(Root, tex, Pos(x + w * 0.5f, y + h * 0.5f),
                                     h > 0f ? U(h) : 0.01f, new Vector2(0.5f, 0.5f), key);
            if (q != null && h > 0f) q.SetAspect(w / h);
            if (q != null)
            {
                q.SetRenderQueue(queue);
                if (!string.IsNullOrEmpty(key)) _named[key] = q;
            }
            return q;
        }

        Label Txt(string key, string s, float x, float y, float w, float h, int scale, Color c, int queue)
        {
            float cy = h > 0f ? y + h * 0.5f : y;
            var l = Label.Create(Root, s, Pos(x + w * 0.5f, cy), scale, c, new Vector2(0.5f, 0.5f), key);
            if (l != null) l.SetRenderQueue(queue);
            return l;
        }

        static void SetQuadWidth(ImageQuad q, float wPx)
        {
            if (q == null) return;
            float h = q.WorldH;
            if (h > 1e-5f) q.SetAspect(Mathf.Max(1f, wPx) / PxPerUnit / h);
        }

        static void DestroySafe(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        /// <summary>自检用：建完之后还缺哪些图（空 = 素材齐）。</summary>
        public List<string> MissingArt { get { return new List<string>(_missingArt); } }

        // ============================================================ 配色 / 阵营

        static readonly Color Ink = new Color(0.93f, 0.93f, 0.95f, 1f);
        static readonly Color Gold = new Color(0.96f, 0.83f, 0.45f, 1f);

        /// <summary>稀有色条的颜色 —— ⚠️ **我们挑的**：原版是 `40k_deck_cardlist_bg_rarityColorGradient`
        /// 这张图 + 运行时染色，**染色值没查到**（节点树里 `m_Color` 是白的）。</summary>
        public static Color RarityTint(string rarity)
        {
            switch (rarity)
            {
                case "rare": return new Color(0.35f, 0.55f, 0.95f, 0.9f);
                case "epic": return new Color(0.75f, 0.85f, 0.95f, 0.9f);
                case "legendary": return new Color(0.95f, 0.78f, 0.35f, 0.9f);
                case "special": return new Color(0.85f, 0.45f, 0.85f, 0.9f);
                default: return new Color(0.65f, 0.68f, 0.72f, 0.9f);
            }
        }

        /// <summary>当前卡组的阵营（督军决定）。</summary>
        public string ArmyFaction()
        {
            if (State == null) return null;
            var w = State.Deck.WarlordId != null ? State.Find(State.Deck.WarlordId) : null;
            return w != null ? w.Faction : null;
        }

        /// <summary>阵营 → 原版徽记图（13 张 `40k_DeckSelection_icon_*`）。</summary>
        public static string FactionIcon(string faction)
        {
            switch (faction)
            {
                case "Ultramarines": return "40k_DeckSelection_icon_FactionUM";
                case "Goff": return "40k_DeckSelection_icon_FactionOrks";
                case "SaimHann": return "40k_DeckSelection_icon_FactionSaimHann";
                case "Sautekh": return "40k_DeckSelection_icon_FactionSautekh";
                case "BlackLegion": return "40k_DeckSelection_icon_FactionBlackLegion";
                case "Leviathan": return "40k_DeckSelection_icon_FactionLeviathan";
                case "TauEmpire": return "40k_DeckSelection_icon_FactionTauEmpire";
                case "Sororitas": return "40k_DeckSelection_icon_FactionSororitas";
                case "Genestealers": return "40k_DeckSelection_icon_Genestealers";
                case "AstraMilitarum": return "40k_DeckSelection_icon_FactionAstraMilitarum";
                case "DarkAngels": return "40k_DeckSelection_icon_FactionDarkAngels";
                case "EmperorsChildren": return "40k_DeckSelection_icon_FactionEmperorsChildren";
                case "SpaceWolves": return "40k_DeckSelection_icon_SpaceWolves";
                default: return "40k_collection_bt_decks";
            }
        }

        /// <summary>自检/演示用的一副卡组：能凑合法就凑合法，凑不出就有什么用什么。</summary>
        public static PlayerDeck PlayerDeckForDemo(DeckEditorState state)
        {
            var deck = new PlayerDeck { Name = "复仇者之刃" };
            CardDef warlord = null;
            foreach (var c in state.Pool) if (c.Type == "hero") { warlord = c; break; }
            if (warlord == null) return deck;
            deck.WarlordId = warlord.Id;
            foreach (var c in state.Pool)
                if (c.Type == "defence" && DeckRules.SameFaction(c.Faction, warlord.Faction)) { deck.DefensiveId = c.Id; break; }
            foreach (var c in state.Pool)
            {
                if (deck.CardIds.Count >= DeckRules.ClassicCards) break;
                if (c.Type != "unit" || !DeckRules.SameFaction(c.Faction, warlord.Faction)) continue;
                for (int i = 0; i < DeckRules.CopyLimit(c.Rarity) && deck.CardIds.Count < DeckRules.ClassicCards; i++)
                    deck.CardIds.Add(c.Id);
            }
            return deck;
        }
    }
}
