// CampaignTab.cs — 阶段二第 3 层：战役页（`Campaign Tab`）
//
// ============================ 出处（唯一正本） ============================
// `资料/阶段二_锻造厂与战役页_原版规格.md` §一（位置）· §四（层 × 参数表 + `Premium Panel` 实算）·
// §七（驱动链）· §十二（**内容数据 —— UM 那套本地可读**）· §十三（节点 prefab + 6 状态色 + 连线算法）。
// **每一个矩形的锚点五元组都是原版 JSON 原文**，由 `工具/menu_rect.py <pid> --cs` 机械吐出来。
//
// 🔴 **六条纪律**（前四条同 `ForgeTab`）：
//   ① **出厂 `activeSelf=false` 的件**：`Debug Point Button`（`OnSetup` 里**无条件** `SetActive(false)`）·
//      `Premium Button Container`（**全 bundle 无脚本引用 ⇒ 发行版永不显示**）· `Tutorial Message`
//      （`ToggleChooseArmyText` **全库 0 调用者**）⇒ **一律不建**。
//   ② **被布局组排的子节点**（`Army Content` / `Premium Panel` 及其内的 `Points`/`Timer`）
//      ⇒ 用 `UguiLayout` / §四 的实算值，**别抄 JSON 的模板位**。
//   ③ **有 `localScale` 的子件**：`Premium Mark` 的 `localScale = (2,2,2)` ⇒ 100² 的框实显 **200²**。
//   ④ 🔴 **`Background Image` 出厂 `m_IsActive=false`，但运行时由 `CampaignUIBackground` 打开并换图** ——
//      所以**要建、而且开着**（与纪律①那种「原版真不用」的不一样）。
//   ⑤ **渲染队列一层一个**（照原版兄弟序）—— 同队列里 Unity 按到相机的距离排，谁盖谁不可控（见 `已知的坑.md`）。
//   ⑥ 🔴 **`SetAsFirstSibling`：连线在节点的所有图形下面** —— 原版 `CampaignNode.Connect` 第一件事。
//
// 🔴 **本轮没建的三样**（**出声**，不静默 —— 项目红线）：
//   · ~~**节点上的奖励物品图标**：原版 `CampaignNode.Setup` 里 `ItemDrawer.Draw(itemHolder, 首奖励, …)`；
//     我们还没有 ItemDrawer（奖励物品 id 是 `Booster Pack Ultramarines` / `WildcardUltramarines2` 这类**服务端 id**）。~~
//     ✅ **2026-10-03 做完了** —— 首奖励走**抽屉库**（`Shell/ItemDrawer.cs`，与战役奖励窗**同一个入口**）：
//     `ItemDrawer.Draw(itemHolder, 首奖励, **1**, **10**)`，那两个参数是 `CampaignNode__Setup.c:109` 实读的。
//     ⚠️ 仍**没有图**的那些奖励（`UM34` / `C2` / 32 位 hex 这类）画的是**占位板**，并由 `AuditNodeRewards()` **逐条出声**。
//   · ~~**`RectMask2D` 遮罩**：两个 `Viewport` 的裁剪没实现 ⇒ 轨道右侧会画到屏外~~
//     ✅ **2026-10-03 做完了** —— `Viewport` 的裁剪接上了（`MenuWindowBase.Clip`，等效 `RectMask2D`），
//     节点与连线都逐 quad 截、整颗/整段在视口外的**连点击一起不建**。
//     ⚠️ 仍差的只有**软边**（原版 `RectMask2D.m_Softness` 那一项，我们一律硬边）→ `项目任务.md` §三 第 29 条 A9。
//   · ~~**横向滚动**：原版 `Campaign Track` 是横向 ScrollRect；我们还没做滚动~~
//     ✅ **2026-10-03 做完了** —— `Campaign Track` 现在有自己的 `MenuScroll`（横向、可拖可滚、
//     带惯性/回弹，与锻造页共用同一份实现），起手滚到最左。`_shiftX` 那道「让出一个光圈半径」的
//     补偿**保留**（它管的是最左那个节点的高亮圈别压住左栏四键），但现在它落在**内容坐标**里。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>战役页。原版 `CampaignWindowTab : WindowTabBase<MainMenuRewardsWindow>`。</summary>
    public class CampaignTab : WindowTabBase
    {
        public override WindowTabType Type { get { return WindowTabType.Campaign; } }

        RewardsWindow _win;
        Transform _root;

        // ---- 出处：正本 §一（`Campaign Tab` 实测 x 330.69..1920.00 · y 70.94..1080.00）----
        public const float TabL = 330.69f, TabT = 70.94f, TabR = 1920f, TabB = 1080f;

        /// <summary>渲染队列：照原版兄弟序逐层 +1（见纪律⑤）。
        /// ⚠️ 条目内的次序照原版 `Campaign Army Item Button` 的子节点序：`HighlightBG` → `Icon` → `Slider`
        /// （**没有 `Arrow`** —— 这一变体没这个节点）。</summary>
        public const int QTabBg = 3030, QTabBgImage = 3031, QTabSel = 3032,
                           QArmyItem = 3033, QArmyArrow = 3034, QArmyIcon = 3035, QArmySlider = 3036,
                           QTrackLine = 3040,
                           QNodePremium = 3041, QNodeBg = 3042, QNodeHighlight = 3043, QNodeItem = 3044,
                           QTabHeader = 3050, QHeaderIcon = 3051, QHeaderTitle = 3052, QHeaderPts = 3053, QHeaderInfo = 3054,
                           QPanel = 3060, QPanelTitle = 3061, QPanelPts = 3062, QPanelBtn = 3063, QPanelTimer = 3064;

        // `Campaign Background`     N(1, 0,0, 1,1, 0,0.5, -0.344849,0, 0.34485,0)
        // `…/Background Image`      N(2, 0,0, 1,1, 0,0.8, 0,0, 0,580.595)        ← 阵营背景图（正方形，锚 0.8 靠上）
        // `Campaign Army Selector`  N(1, 0,1, 1,1, .5,1, 207.786,0, -414.892,137)
        // `…/Background`            N(2, 0,.5, 1,.5, 0,1, -102.678,68, 102.677,136)
        // `Campaign Header`         N(1, 0,1, 0,1, 0,1, 0,9.99991, 460.225,165)
        // `Campaign Track`          N(1, 0,.5, 1,.5, 0,.5, 0,-114.53, 0.34009,709.06)
        // `…/Viewport`              N(2, 0,0, 1,1, 0,1, 0,50, 0,50)
        /// <summary>阵营选择条的条目尺寸。
        /// 🔴 **2026-09-24 订正**：原来这里写「`Campaign Army Item Button` 的根尺寸**没查到确证**
        /// ⇒ 沿用母版 136.36×121.59」，那是**错的** —— 正本（`资料/阶段二_锻造厂与战役页_原版规格.md` §六
        /// 那张三变体对照表）早就有直读值，而且**三个变体各自都与母版不同**：
        ///   · 根 **120×0**（子件全挂在**顶边**、往下垂）· `HighlightBG` **120×110**（不是 136×122）
        ///   · `Icon` **120×100** 顶边下 **5**（不是母版那套拉伸锚）· **没有 `Arrow`**（母版与 Forge 版都有）
        ///   · `Badge Highlight` 35×35 右上角 `(−17.5,−17.5)`（母版在中心偏左下）
        ///   · **多一条 `Slider`** 110×20（在 `HighlightBG` 正下方）
        /// ⇒ 条目可视高 = 110（高亮/图标）+ 20（进度条）= **130**。
        /// 出处：`menu_rect.py bundle_menus_assets_all "Campaign Army Item Button" --depth 4 --cs`。</summary>
        const float ArmyItemW = 120f, ArmyItemH = 130f, ArmySpacing = -14f;
        /// <summary>`HighlightBG` / `Icon` 的高度与图标相对顶边的下移（照上面那棵树直读）</summary>
        const float ArmyHlH = 110f, ArmyIconH = 100f, ArmyIconDy = 5f;
        /// <summary>`Slider`：锚 `(0,1)-(1,1)` `sizeDelta (−10,20)` ⇒ 宽 = 条目宽 −10、贴着高亮框下沿。</summary>
        const float ArmySliderH = 20f, ArmySliderInset = 5f;
        /// <summary>进度条那一条 12 高的芯（原版 `Background`/`Fill Area`/`Outline` 都是它，
        /// 锚 `(0,0.2)-(1,0.8)`、`sizeDelta 0`）。</summary>
        const float ArmyBarH = 12f;

        /// <summary>节点的原始尺寸（原版 `Campaign Node` 根 = **100×100**）。</summary>
        const float NodeSize = 100f;
        /// <summary>`Collectable Highlight` 的尺寸（原版 `N(1, .5,.5, .5,.5, .5,.5, 0,0, 178.915,178.915)`）。</summary>
        const float HighlightSize = 178.915f;
        /// <summary>连线的粗细（原版 `Node Line` 的 `sizeDelta = 290.6 × 10`）。</summary>
        const float LineH = 10f;
        /// <summary>连线起点离节点中心的距离系数。原版是全局常量 `DAT_1834b2bb4`，
        /// 全库 187 处用法都是「取一半」语义 ⇒ **推断 0.5f，未证实**（正本 §十三）。</summary>
        const float RimK = 0.5f;

        public void SetHost(RewardsWindow win, Transform root) { _win = win; _root = root; }
        public override void Setup() { Build(); }
        public override void OnOpen() { FocusSelectedArmy(); }

        Transform _armyContent, _trackContent;
        /// <summary>`Army Content` 的矩形 = **选择条中心的一个对称展开区**
        /// （见 `UguiLayout.HorizontalContentCentered`）。条目由它算出，**不是**从 `_selR` 左边缘起。</summary>
        PxRect _armyContentR;
        /// <summary>阵营条的滚动区（全壳唯一一份滚动实现 = `MenuScroll`，与锻造页共用）。</summary>
        MenuScroll _armyScroll;
        /// <summary>自检用：批处理里没有滚轮事件 ⇒ 直调 `MenuScroll.Wheel/ScrollBy`（**和真滚同一条**）。</summary>
        public MenuScroll ArmyScroll { get { return _armyScroll; } }

        /// <summary>🆕 **`Campaign Track` 的横向滚动区**（原版这一件也是 `ScrollRect` + `RectMask2D`）。</summary>
        MenuScroll _trackScroll;
        /// <summary>自检用（同上）。</summary>
        public MenuScroll TrackScroll { get { return _trackScroll; } }
        Label _title, _points;
        ImageQuad _armyIcon;

        /// <summary>节点首奖励里**本地点不出图**的那些 id（画的是占位板）——**逐条出声**（红线：不许静默失败）。
        /// 由 `AuditNodeRewards()` 逐条查 47 个节点填出来，**与视口滚到哪无关**（自检也读它）。</summary>
        public readonly List<string> NoIconRewards = new List<string>();
        /// <summary>**首奖励画不出来的节点数**（不去重；`NoIconRewards` 是去重的 id）—— `AuditNodeRewards()` 填。</summary>
        public int NoIconRewardNodes { get; private set; }
        PxRect _tabR, _selR, _headerR, _trackR, _vpR, _titleR, _pointsR;

        public void Build()
        {
            var root = _root;
            _tabR = new PxRect(TabL, TabT, TabR, TabB);
            _selR = UguiRect.Child(_tabR, UguiRect.A01, UguiRect.A11, UguiRect.P51,
                                   new Vector2(207.786f, 0f), new Vector2(-414.892f, 137f));
            _headerR = UguiRect.Child(_tabR, UguiRect.A01, UguiRect.A01, UguiRect.P01,
                                      new Vector2(0f, 9.99991f), new Vector2(460.225f, 165f));
            _trackR = UguiRect.Child(_tabR, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f),
                                     new Vector2(0f, -114.53f), new Vector2(0.34009f, 709.06f));

            // ---- ① `Campaign Background`（`Mask`）+ `Background Image`（阵营背景图）----
            var cbg = RewardsWindow.Node(root, "Campaign Background",
                UguiRect.Child(_tabR, UguiRect.A00, UguiRect.A11, new Vector2(0f, 0.5f),
                               new Vector2(-0.344849f, 0f), new Vector2(0.34485f, 0f)));
            // 纪律④：这个子件**出厂是关的、运行时才开**，所以建它、并且开着
            var bgImg = UguiRect.Child(_tabR, UguiRect.A00, UguiRect.A11, new Vector2(0f, 0.8f),
                                       Vector2.zero, new Vector2(0f, 580.595f));
            _bgQuad = _win.Rect(cbg, CampaignData.Background(CampaignData.Selected), bgImg, "Background Image", QTabBgImage);

            // ---- ② `Campaign Army Selector`（阵营选择条）----
            var sel = RewardsWindow.Node(root, "Campaign Army Selector", _selR);
            _win.Rect(sel, null, UguiRect.Child(_selR, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                      new Vector2(0f, 1f), new Vector2(-102.678f, 68f), new Vector2(102.677f, 136f)),
                      "Background", QTabSel, new Color(0f, 0f, 0f, 0.349f));
            var selVp = RewardsWindow.Node(sel, "Viewport", _selR);
            // 🔴 **`Army Content` 与锻造页同形**：原版锚点是「选择条正中心的一个零宽点」
            //    （`N(2, .5,1, .5,1, .5,.5, -0.0010376,-65, 0,130)`）+ `ContentSizeFitter`
            //    ⇒ 条目**居中**排。2026-09-23 找茬查出两页原来都从左边缘排 ⇒ 最后几个阵营出屏。
            //    判据与共用实现 = `UguiLayout.HorizontalContentCentered`（**别在这里再写一遍**）。
            _armyContentR = UguiLayout.HorizontalContentCentered(_selR, CampaignData.Armies.Length,
                                                                 ArmyItemW, 0f, 0f, ArmySpacing);
            _armyContent = RewardsWindow.Node(selVp, "Army Content", _armyContentR);
            // 🔴 阵营条**横向可滚**（原版 `Campaign Army Selector` 也是 `ScrollRect(横)` + `RectMask2D`）；
            //    居中内容的范围**两侧都有** ⇒ 两端各 2 个够不着的都能滚出来（`MenuScroll` 自己算极值）。
            _armyScroll = new MenuScroll(_selR, _armyContentR.x1, _armyContentR.x2);
            _armyScroll.Owner = root.gameObject;
            _armyScroll.OnChanged = BuildArmyItems;
            PointerLayer.RegisterScroll(_armyScroll);

            // ---- ③ `Campaign Header`（阵营徽记 + 名字 + 点数 + 信息钮）----
            var hdr = RewardsWindow.Node(root, "Campaign Header", _headerR);
            _win.Rect(hdr, "WF_Campaign_Info_Background", _headerR, "bg", QTabHeader);
            _armyIcon = _win.Rect(hdr, DeckRuntime.FactionIcon(CampaignData.Selected),
                UguiRect.Child(_headerR, UguiRect.P50c, UguiRect.P50c, new Vector2(1f, 0.5f),
                               new Vector2(-80.1127f, 0f), new Vector2(135f, 165f)),
                "Army Icon", QHeaderIcon, null, true);
            _titleR = UguiRect.Child(_headerR, new Vector2(0.2f, 0.55f), new Vector2(0.5f, 1f),
                                     UguiRect.P50c, new Vector2(101.421f, 0f), new Vector2(86.9325f, 0f));
            // 🔴 **这里一律用 `_win.Text(...)` 而不是 `TextBox(...)`**：`TextBox` **总是**调
            //    `SetWrapWidth(框宽)` ⇒ 字一超框就**折行**；而原版这几处是 `ContentSizeFitter` 把框撑开、
            //    **不折行**（`Timer Text` 更是 `ContentSizeFitterMinMax`）。
            //    2026-09-23 实测：`Siguiente: 5d 20h 15m`（37.9fs ≈ 400px）在 285.92 的框里被折成两行。
            // 🔴 另外**文字必须显式排队列** —— `Text` 默认落在 `QText`(3011)，而本页底板在 3050+
            //    ⇒ 不设就是「底板把字盖死」（实测：一个字都看不见，而**矩形断言全绿**）。
            _title = _win.Text(hdr, "", _titleR.x1, _titleR.x2, _titleR.y1, _titleR.y2, 5,
                               Color.white, "Title", 31.75f);
            if (_title != null)
            {
                _title.SetRenderQueue(QHeaderTitle);
                _title.SetAutoFitBox(LayoutSpace.Px(_titleR.W), LayoutSpace.Px(_titleR.H), 25f, 31.75f);
            }
            var ptsRect = UguiRect.Child(_headerR, new Vector2(0.2f, 0.25f), new Vector2(1f, 0.45f),
                                         UguiRect.P01, new Vector2(57.9551f, 0f), Vector2.zero);
            _pointsR = ptsRect;
            _points = _win.Text(hdr, "", ptsRect.x1, ptsRect.x2, ptsRect.y1, ptsRect.y2, 5,
                                Color.white, "Points", 34.8f);
            if (_points != null)
            {
                _points.SetRenderQueue(QHeaderPts);
                _points.SetAutoFitBox(LayoutSpace.Px(ptsRect.W), LayoutSpace.Px(ptsRect.H), 18f, 34.8f);
            }
            // `Point Icon`：`scl 2` 的容器里放「战役点底图(1.2) + 阵营徽记」
            var ptIcon = UguiRect.Child(ptsRect, UguiRect.A10, UguiRect.A10, new Vector2(0f, 0.5f),
                                        new Vector2(-20.76f, 0f), new Vector2(50f, 0f));
            _win.Rect(hdr, "40K_genearl_icon_Campaign_points", ptIcon, "Campaign Point Background",
                      QHeaderPts, null, true);
            _win.Rect(hdr, DeckRuntime.FactionIcon(CampaignData.Selected), ptIcon, "Army Icon", QHeaderPts, null, true);
            _win.Rect(hdr, "40K_generic_bt_info",
                UguiRect.Child(_headerR, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                               new Vector2(179.57f, 28.829f), new Vector2(41.1569f, 41.1573f)),
                "Info Button", QHeaderInfo);

            // ---- ④ `Campaign Track`：轨道（连线 + 47 个节点）----
            var track = RewardsWindow.Node(root, "Campaign Track", _trackR);
            _vpR = UguiRect.Child(_trackR, UguiRect.A00, UguiRect.A11, UguiRect.P01,
                                  new Vector2(0f, 50f), new Vector2(0f, 50f));
            var vpNode = RewardsWindow.Node(track, "Viewport", _vpR);
            _trackContent = RewardsWindow.Node(vpNode, "Content", _vpR);
            // 🆕 **2026-10-03：接上横向滚动 + 裁剪**（原版 `Campaign Track` 就是横向 `ScrollRect` +
            //   `Viewport` 上的 `RectMask2D`）。在此之前是**把内容整体右移一个光圈半径**的位移补偿
            //   —— 那个补偿让最左节点不被左栏压住，但**横向滚不了**、右边溢出的一直画到屏外。
            //   滚动区在 `BuildTrack` 里建（内容两端要用到 `Ratio`，它在那儿才算得出来）。
            BuildTrack();

            // ---- ⑤ `Premium Panel`（左下常显的面板；高度是布局组算出来的）----
            BuildPremiumPanel(root);

            // ---- ⑥ 两条活数据 ----
            BuildArmyItems();
            Refresh();
            // ---- ⑦ 首奖励图标那件事**逐条出声**（47 个节点全查一遍，与视口无关）----
            AuditNodeRewards();
        }

        ImageQuad _bgQuad;

        // ============================================================ 轨道

        /// <summary>缩放比。**照原版 `CampaignTrack.CalculateRatio` 的口径**：
        /// `(视口高 − 节点高) / (2 × |y| 的最大值)`（原版用的是 `trackHolder` 的高与节点预制体的高）。
        /// ⚠️ 原版那一段反编译里有几个没解出的常量（`DAT_1834b2bc8` 等），
        /// **这个式子是照它的结构复刻的**，跑出来节点的行距与视口高对得上（见 `Dump()`）。</summary>
        public float Ratio { get; private set; }

        /// <summary>建整条轨道：**先从 SO 抄出来的 47 个节点**（`CampaignData.Nodes`）+ 连线。</summary>
        void BuildTrack()
        {
            int minX, maxX, maxAbsY;
            CampaignData.Span(out minX, out maxX, out maxAbsY);
            // ⚠️ 用 **Viewport 的高**（759.06）算 —— 原版用的是 `trackHolder`（= `Content`）的高，
            //    而那个高是 `SetTrackSize` 运行时定的、本地读不到。
            float vpH = _vpR.H;
            Ratio = maxAbsY <= 0 ? 1f : (vpH - NodeSize) / (2f * maxAbsY);
            // 🔴 **2026-10-03：`_shiftX` 现在算出的是「偏移 0 时的屏幕坐标」**（内容坐标系 = 偏移 0 时的
            //    屏幕坐标系，这样 `MenuScroll` 的两个极值天然就是「两端各差多少」）。
            //    它仍然是「把最左节点往右让出一个**光圈半径**」那道补偿
            //    （否则最左那个节点的 `Collectable Highlight`（178.915² 的圆环）会压到左栏四键上；
            //     2026-09-23 实测白圈压住了 FORGE 那个键）。**相对位置与缩放比照原版。**
            _shiftX = _vpR.x1 + HighlightSize * 0.5f - minX * Ratio;
            _centerY = _vpR.CY;

            // 🆕 滚动区：内容两端 = 节点位置 ∓ 光圈半径（把最外那圈高亮也算进去）
            float cx1, cx2;
            ContentSpan(out cx1, out cx2);
            if (_trackScroll == null)
            {
                _trackScroll = new MenuScroll(_vpR, cx1, cx2);
                _trackScroll.Owner = _root != null ? _root.gameObject : null;
                _trackScroll.OnChanged = RefreshNodes;
                PointerLayer.RegisterScroll(_trackScroll);
            }
            else { _trackScroll.ContentX1 = cx1; _trackScroll.ContentX2 = cx2; }

            // 连线**先建**（照原版 `SetAsFirstSibling`：连线在节点的所有图形下面）
            for (int i = 0; i < CampaignData.NodeCount; i++)
                foreach (int j in CampaignData.At(i).Next) BuildLine(i, j);

            for (int i = 0; i < CampaignData.NodeCount; i++) BuildNode(i);
        }

        /// <summary>轨道的**内容坐标**两端（= 所有节点中心的极值 ∓ 光圈半径）。</summary>
        void ContentSpan(out float cx1, out float cx2)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            for (int i = 0; i < CampaignData.NodeCount; i++)
            {
                float x = _shiftX + CampaignData.At(i).X * Ratio;
                if (x < lo) lo = x;
                if (x > hi) hi = x;
            }
            if (lo > hi) { lo = 0f; hi = 0f; }                      // 没有节点 ⇒ 零宽
            float pad = HighlightSize * 0.5f;
            cx1 = lo - pad; cx2 = hi + pad;
        }

        float _shiftX, _centerY;

        /// <summary>节点在 `Content` 里的中心（原版 `GetNodePosition`：`(Position − offset) * ratio`，**y 取反**）。
        /// ⚠️ **这是内容坐标**（偏移 0 时的那一套）—— 屏幕坐标要再过 `NodeScreen`。</summary>
        Vector2 NodePos(int i)
        {
            var n = CampaignData.At(i);
            return new Vector2(_shiftX + n.X * Ratio, _centerY - n.Y * Ratio);
        }

        /// <summary>内容坐标 → **屏幕坐标**（把 `MenuScroll` 的偏移加上去）。</summary>
        Vector2 NodeScreen(int i)
        {
            var c = NodePos(i);
            if (_trackScroll == null) return c;
            var r = _trackScroll.Shift(new PxRect(c.x, c.y, c.x, c.y));
            return new Vector2(r.x1, r.y1);
        }

        PxRect NodeRect(int i)
        {
            Vector2 c = NodeScreen(i);
            float h = NodeSize * 0.5f;
            return new PxRect(c.x - h, c.y - h, c.x + h, c.y + h);
        }

        /// <summary>自检用：第 i 个节点**这一刻**的屏幕矩形（= 内容坐标 + 当前滚动偏移）。
        /// ⚠️ 给它是因为**视口外的节点现在不建** —— 断言不能只靠 `Find("CampaignNode_i")`
        /// （找不到 ≠ 不存在，是「被 `RectMask2D` 裁掉了」）。</summary>
        public PxRect NodeRectForTest(int i) { return NodeRect(i); }

        Transform[] _nodeTf;

        /// <summary>一个节点。**照原版 `CampaignNode.Setup` + `SetNodeStyle`**：
        /// ① `Premium Mark` 只在「该节点有高级档奖励」时显示；② 圆盘按**状态色**着色；
        /// ③ `Collectable Highlight` 只在 `Unlocked` 且点数够时显示。</summary>
        void BuildNode(int i)
        {
            var r = NodeRect(i);
            if (_nodeTf == null) _nodeTf = new Transform[CampaignData.NodeCount];
            // 🆕 **整颗在视口外就不建**（原版 `RectMask2D` 会把它连**点击**一起裁掉 ——
            //    接滚动之前右边那几十个节点一直画到屏外、而且**还点得到**）
            if (_trackScroll != null && !_trackScroll.Intersects(r)) { _nodeTf[i] = null; return; }
            var node = RewardsWindow.Node(_trackContent, "CampaignNode_" + i, r);
            _nodeTf[i] = node;

            int st = CampaignData.StateOf(i);
            bool premium = CampaignData.At(i).HasPremium;

            // `Premium Mark`：N(1, .5,.5, .5,.5, .5,.5, 0,0, 100,100) + **localScale (2,2,2)** ⇒ 实显 200²
            if (premium)
            {
                var pm = RewardsWindow.Node(node, "Premium Mark", r);
                pm.localScale = new Vector3(2f, 2f, 1f);
                _win.Rect(pm, "WF_Campaign_Levelspot-Premium", r, "img", QNodePremium, PremiumColor(st));
            }
            // `Generic Round Button Variant`（= `background`）：N(1, .5,.5, .5,.5, .5,.5, 0,0, 100,100)
            _win.Rect(node, "WF_Campaign_Levelspot", r, "Generic Round Button Variant", QNodeBg, StateColor(st));
            // `Collectable Highlight`：N(1, .5,.5, .5,.5, .5,.5, 0,0, 178.915,178.915) —— **出厂 inactive**
            if (st == CampaignData.Unlocked && CampaignData.Claimable(i))
            {
                var hl = UguiRect.Child(r, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                        Vector2.zero, new Vector2(HighlightSize, HighlightSize));
                _win.Rect(node, "Border_Thick_Circle_FX", hl, "Collectable Highlight", QNodeHighlight);
            }
            // `Item Holder`：奖励物品的运行时挂点（出厂空、`CanvasGroup.blocksRaycasts=false`）。
            // 🆕 **2026-10-03：把首奖励画进去**（原版 `CampaignNode.Setup` 那句 `ItemDrawer.Draw`）——
            //    矩形 `N(1, 0,0, 1,1, .5,.5, 0,0, 0,0)` = **铺满节点那 100×100**（`menu_rect.py` 实读）。
            var ih = UguiRect.Child(r, UguiRect.A00, UguiRect.A11, UguiRect.P50c, Vector2.zero, Vector2.zero);
            BuildNodeReward(RewardsWindow.Node(node, "Item Holder", ih), ih, i);

            AddHit(node, "Hit", r, QNodeItem, () => OnNodeClicked(i));
        }

        /// <summary>节点上的**首奖励** —— 照原版 `CampaignNode.Setup` 那一句
        /// `ItemDrawer.Draw(itemHolder, Data.Rewards.FirstOrDefault(), **1**, **10**)`（`CampaignNode__Setup.c:109` 实读）：
        /// **数量写死 1**、覆盖档 = **`Icon(10)`**（那一档的抽屉只画一张图，没有数量、没有名字）。
        /// ⚠️ 那个 `FirstOrDefault` = **列表第一条**：这一族里的两个 lambda 一个是 `char.IsDigit`
        /// （解析 NodeId 的序号）、一个是 `rewardTier == 10`（给 `premiumMark` 的 `Any`），
        /// **都不是这里的选取器**（`CampaignNode.__c___Setup_b__37_0/1.c` 逐个体读过）。
        /// 🔴 **走抽屉库**（`Shell/ItemDrawer.cs`）—— 与战役奖励窗**同一个入口**，别在这里另写一套画法。</summary>
        void BuildNodeReward(Transform holder, PxRect box, int i)
        {
            var rw = CampaignData.RewardsOf(i);
            if (rw.Length == 0) return;                       // 原版那句「首奖励 != null」的守卫
            string id = rw[0].Id;
            var item = ItemDrawer.Spec(id, CampaignData.ItemIcon(id), CampaignData.ItemShortName(id));
            var st = ItemDrawerStyle.Default(QNodeItem, QNodeItem, QNodeItem);
            // 节点名（**我们挑的**）：原版这里是 `Instantiate` 出来的抽屉 prefab、名字在 prefab 里（本地没有）。
            // ⚠️ **别叫 `Item Drawer`** —— 普查 §三 里那 4 个同名件其实是 **GridLayoutGroup 容器、不是抽屉本体**，
            //    而别的窗（商店 / 头像页 / 称号页）已经有同名的节点。这里用**抽屉类名**，
            //    自检可以直接断「这一格用的是哪个抽屉」。
            st.NodeName = ItemDrawer.PickDrawer(item, DrawerOverride.Icon) ?? "Item Drawer";
            st.QuantityPx = 0f;                               // `Icon` 档不画数量（`WildcardIconDrawer.Draw` 只碰一张图）
            st.NamePx = 0f;                                   // **我们挑的**：节点只有 100²，8 位 hex 的短名在这里读不出来 ⇒ 不画
            st.Clip = _win != null ? _win.Clip : null;        // 轨道视口（`RefreshNodes` 里设的那个）的裁剪
            ItemDrawer.Draw(holder, box, item, 1, DrawerOverride.Icon, st);
            // ⚠️ **不在这里记「没图」** —— 那个清单的唯一出处是 `AuditNodeRewards()`（47 个节点全查一遍）：
            //    它不受「这一刻视口里建了哪几个节点」影响，滚动重建也不会把数字改来改去。
        }

        /// <summary>奖励图标那件事的**出声**：47 个节点的首奖励**逐条**问一遍抽屉库「本地点得出来吗」，
        /// 与**视口滚到哪无关**（不是「建了哪几个」）。`Build()` 末尾调一次。
        /// 🔴 判据 ⇄ 原版：原版每个物品都有真抽屉（图在 prefab 里）⇒ 我们这边**只有判据空的那批**要出声。</summary>
        public void AuditNodeRewards()
        {
            NoIconRewards.Clear();
            int nodesWithout = 0;
            for (int i = 0; i < CampaignData.NodeCount; i++)
            {
                var rw = CampaignData.RewardsOf(i);
                if (rw.Length == 0) continue;
                string id = rw[0].Id;
                var item = ItemDrawer.Spec(id, CampaignData.ItemIcon(id), CampaignData.ItemShortName(id));
                if (ItemDrawer.HasArt(item, DrawerOverride.Icon)) continue;
                nodesWithout++;
                if (!NoIconRewards.Contains(id)) NoIconRewards.Add(id);
            }
            NoIconRewardNodes = nodesWithout;
            if (nodesWithout == 0)
                Debug.Log("[Campaign] " + CampaignData.NodeCount + " 个节点的首奖励**都画得出来**（走抽屉库、覆盖档 `Icon`）");
            else
                Debug.LogWarning("[Campaign] ⚠️ " + CampaignData.NodeCount + " 个节点里有 " + nodesWithout
                                 + " 个的首奖励**本地点不出图**（涉及 " + NoIconRewards.Count + " 个不同的 id，画的是占位板）："
                                 + string.Join("、", NoIconRewards.ToArray())
                                 + " —— 原版走 `ItemDrawer`；`ItemDrawerConfig` SO 与那批抽屉 prefab 本地都没有"
                                 + "（铁律 11 第①种：原版本身取不到 ⇒ 占位板 + 出声）");
        }

        /// <summary>一条连线。**照原版 `CampaignNode.Connect`**：
        /// `rim = 方向 × 节点半宽 × K` · 起点 = 源节点中心 + `rim` · 终点 = 目标节点中心 ·
        /// 长度 = 两点距离 · `right = rim`（即旋转角跟着方向走）。</summary>
        void BuildLine(int i, int j)
        {
            Vector3 wi = LayoutSpace.FromPixel(NodeScreen(i).x, NodeScreen(i).y);
            Vector3 wj = LayoutSpace.FromPixel(NodeScreen(j).x, NodeScreen(j).y);
            Vector3 d = wj - wi;
            if (d.sqrMagnitude < 1e-6f) return;
            Vector3 dir = d.normalized;
            float halfW = LayoutSpace.Px(NodeSize) * 0.5f;
            Vector3 rim = new Vector3(dir.x * halfW, dir.y * halfW, 0f) * RimK;
            Vector3 a = wi + rim, b = wj;
            Vector3 ab = b - a;
            if (ab.sqrMagnitude < 1e-6f) return;

            // 🆕 **一段连线整段在视口外就不建**（原版 `RectMask2D` 会把它整个裁掉；
            //    接滚动之前这里没有这道判断 —— 右边几十个节点的连线一直画到屏外）
            Vector2 midPx = LayoutSpace.ToPixel(a + ab * 0.5f);
            float halfLen = ab.magnitude * 108f * 0.5f;
            if (_trackScroll != null
                && (midPx.x + halfLen < _vpR.x1 - 2f || midPx.x - halfLen > _vpR.x2 + 2f)) return;

            float len = ab.magnitude;
            var parent = _trackContent;
            var q = ImageQuad.Create(parent, _win.Art("40k_Generic_Smooth_line"),
                                     a + ab * 0.5f - parent.position, LayoutSpace.Px(LineH),
                                     new Vector2(0.5f, 0.5f), "NodeLine_" + i + "_" + j);
            if (q == null) return;
            q.SetAspect(len / LayoutSpace.Px(LineH));
            q.SetRenderQueue(QTrackLine);
            q.SetTint(StateColor(CampaignData.StateOf(i)));      // 原版 `SetLineColors`：**跟源节点的 background 同色**
            q.transform.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(ab.y, ab.x) * Mathf.Rad2Deg);
        }

        // ---- 6 种状态色（**照原版** `MonoBehaviour_1891907725419958151.json` 原文，正本 §十三）----
        static readonly Color LockedC = new Color(0.6981132f, 0.6487184f, 0.6487184f, 0.6823530f);   // #B2A5A5AE
        static readonly Color UnlockedC = new Color(1f, 1f, 1f, 0.8078431f);                          // #FFFFFFCE
        static readonly Color CollectedC = new Color(0f, 1f, 0.0666666f, 0.6823530f);                 // #00FF11AE
        static readonly Color PremLockedC = new Color(0.6886792f, 0.6886792f, 0.6886792f, 0.7725490f);// #B0B0B0C5
        static readonly Color PremUnlockedC = new Color(1f, 1f, 1f, 1f);                              // #FFFFFFFF
        static readonly Color PremCollectedC = new Color(0.5789730f, 0.8301887f, 0.4973300f, 1f);     // #94D47FFF

        /// <summary>圆盘与连线的颜色。**照原版 `SetNodeStyle` 的表**（locked 是**默认分支**）。</summary>
        static Color StateColor(int st)
        {
            switch (st)
            {
                case CampaignData.Unlocked: return UnlockedC;
                case CampaignData.BaseCollected:
                case CampaignData.AllCollected:
                case CampaignData.Repeatable: return CollectedC;
                default: return LockedC;
            }
        }

        /// <summary>`Premium Mark` 的颜色。🔴 **`premium*` 三色只上 `premiumMark`、永不上连线。**</summary>
        static Color PremiumColor(int st)
        {
            switch (st)
            {
                case CampaignData.Unlocked: return PremUnlockedC;
                case CampaignData.AllCollected:
                case CampaignData.Repeatable: return PremCollectedC;
                default: return PremLockedC;
            }
        }

        // ============================================================ 阵营选择条

        void BuildArmyItems()
        {
            // 🔴 **幂等（必须先清）**：滚动回调 `OnChanged` 会重入这里（同锻造页那条）
            for (int i = _armyContent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(_armyContent.GetChild(i).gameObject);
            // 🔴 **偏移 + 裁切**（与锻造页同一条路，见 `MenuScroll` / `MenuWindowBase.Clip`）
            var prevClip = _win.Clip;
            _win.Clip = _selR;
            for (int i = 0; i < CampaignData.Armies.Length; i++)
            {
                string army = CampaignData.Armies[i];
                // 内容坐标：从 `Army Content` 的**居中**矩形起排
                var content = UguiLayout.HorizontalChild(_armyContentR, ArmyItemW, ArmyItemH, i, 0f, ArmySpacing);
                var r = _armyScroll != null ? _armyScroll.Shift(content) : content;
                if (_armyScroll != null && !_armyScroll.Intersects(r)) continue;   // 整条在视口外 ⇒ 不建
                var item = RewardsWindow.Node(_armyContent, "CampaignArmyItem_" + i, r);
                // ⚠️ 下面三层的矩形**全部照 `Campaign Army Item Button` 那棵树直读**（见 `ArmyItemW` 的注释）：
                //    子件锚在条目的**顶边**、往下垂；`HighlightBG` 与 `Icon` 尺寸不同（110 vs 100）。
                //    原来照母版画 = 高亮框高 122（多 12）、图标按拉伸锚铺满（该是 120×100 顶边下 5）。
                if (army == CampaignData.Selected)
                    // 🔴 **2026-10-03 就地更正（A21）**：原版 `Campaign Army Item Button>HighlightBG` 的图是
                    //    **`40K_settings_button_hover`**（实测见 `menu_dump.py bundle_menus_assets_all "Campaign Army Item Button"`），
                    //    我们原来画的是 `…_selected` —— ⚠️ **同族的 `Forge Army Item Button` / `Army Item Button`
                    //    （排行榜）用的才是 `_selected`**，**别按族照搬**，这三处各是各的件。
                    _win.Rect(item, "40K_settings_button_hover",
                              new PxRect(r.x1, r.y1, r.x1 + ArmyItemW, r.y1 + ArmyHlH), "HighlightBG", QArmyItem,
                              new Color(1f, 0.631f, 0.2784f, 1f));      // 母版是**橙**（Forge 页那份才是品红）
                _win.Rect(item, DeckRuntime.FactionIcon(army),
                          new PxRect(r.x1, r.y1 + ArmyIconDy, r.x1 + ArmyItemW, r.y1 + ArmyIconDy + ArmyIconH),
                          "Icon", QArmyIcon, null, true);
                // `Slider`（**这一页独有**）：在 `HighlightBG` 正下方，宽 = 条目宽 − 10
                BuildArmySlider(item, army, new PxRect(r.x1 + ArmySliderInset, r.y1 + ArmyHlH,
                                                 r.x1 + ArmyItemW - ArmySliderInset, r.y1 + ArmyHlH + ArmySliderH));
                // ⚠️ **不画 `Arrow`** —— 这一变体**没有这个节点**（母版与 Forge 版才有）。我们本来就没画，记着别加。
                AddHit(item, "Hit", r, QArmyIcon, () => SelectArmy(army));
            }
            _win.Clip = prevClip;
        }

        /// <summary>阵营格底下那条**战役进度条**（原版 `Campaign Army Item Button/Slider`）。
        /// 🔴 **两张图和值都是我们挑的**（出声，别当原版）：
        ///   · **图**：原版那三张（`Background`/`Fill`/`Outline`）的 sprite pid 我**没解析出来**
        ///     （`bundle_menus_assets_all` 里按 pid 反查 sprite 名那次没跑通）⇒ 借用工程里已有的进度条
        ///     `40k_CardAmount_bar_bg` / `40k_CardAmount_bar_fill`（费用曲线那条用的同一对）。
        ///   · **值** = `ClaimedCount ÷ NodeCount`，且**只给「本地有内容」的阵营填**（`CampaignData.HasContent`）——
        ///     ⚠️ 第一版对**全部 13 格**都填同一个值，那是错的：原版的滑条是**逐阵营**的进度，
        ///     本地只有 Ultramarines 有内容 ⇒ 别的阵营**一格都不该有填充**。
        ///     原版的 `Slider.value` 到底由哪个字段写**没查到**。
        /// 版式是原版的：滑条 110×20、芯 12 高（锚 `(0,0.2)-(1,0.8)`）、节点段贴着高亮框下沿。</summary>
        void BuildArmySlider(Transform item, string army, PxRect r)
        {
            // ⚠️ **不能挂 `keepAspect`**：原版那条是**拉伸**的（锚 (0,0.2)-(1,0.8)、`sizeDelta 0`），
            //    而工程里那两张进度条图比例不是 110:12 ⇒ 等比会被缩成 25.7 宽（自检第一版就抓到）。
            float barY1 = r.y1 + (r.H - ArmyBarH) * 0.5f;
            var band = new PxRect(r.x1, barY1, r.x2, barY1 + ArmyBarH);
            _win.Rect(item, "40k_CardAmount_bar_bg", band, "Slider/Background", QArmySlider);
            float frac = (CampaignData.HasContent(army) && CampaignData.NodeCount > 0)
                ? Mathf.Clamp01(CampaignData.ClaimedCount / (float)CampaignData.NodeCount) : 0f;
            var fill = new PxRect(band.x1, band.y1, band.x1 + band.W * frac, band.y2);
            if (fill.W > 0.01f) _win.Rect(item, "40k_CardAmount_bar_fill", fill, "Slider/Fill", QArmySlider);
        }

        /// <summary>把**选中的阵营**对到视口中心（照原版 `ArmySelector.FocusOnArmy`）。开页 / 换阵营时调。</summary>
        public void FocusSelectedArmy()
        {
            if (_armyScroll == null) return;
            int i = System.Array.IndexOf(CampaignData.Armies, CampaignData.Selected);
            if (i < 0) return;
            _armyScroll.FocusOn(_armyContentR.x1 + i * (ArmyItemW + ArmySpacing) + ArmyItemW * 0.5f);
        }

        /// <summary>换阵营。**照原版 `CampaignWindowTab.ClickChangeArmy`**：不同才换 → `SetActiveCampaign` → `handler.SetSelectedArmy`。
        /// ⚠️ **本地只有 UM 一套内容** ⇒ 选别的阵营时**如实说明**（不许静默失败）。</summary>
        public void SelectArmy(string army)
        {
            CampaignData.Select(army);
            if (!CampaignData.HasContent(army))
                Debug.Log("[Campaign] 选中了 `" + army + "`，但**这一套战役本地没有**"
                          + "（只导出了 Ultramarines 那 47 个节点 SO）—— 轨道按空态显示，不假装有内容");
            BuildArmyItems();          // 幂等：它自己会先清
            FocusSelectedArmy();
            Refresh();
        }

        /// <summary>把当前阵营的数据刷到画面上（原版 `SetActiveCampaign` 那段）。</summary>
        public void Refresh()
        {
            string a = CampaignData.Selected;
            if (_title != null)
            {
                _title.SetText(a.ToUpperInvariant());
                // 🔴 **对齐必须在 `SetText` 之后** —— 原版这两个 TMP 是 `H=Left`。
                //    第一版把 `AlignLeft` 写在 `Build` 里（那时文字还是空串）⇒ 位置按空串算，
                //    实测左边缘量出 **2.3e11**（TMP 边界是垃圾值）。见 `已知的坑.md` 那条「文字边界是缓存的」。
                MenuDraw.AlignLeft(_title, _titleR);
            }
            if (_points != null)
            {
                _points.SetText("Points: " + CampaignData.Points);
                MenuDraw.AlignLeft(_points, _pointsR);
            }
            if (_armyIcon != null)
            {
                var t = _win.Art(DeckRuntime.FactionIcon(a));
                if (t != null) { _armyIcon.SetTexture(t); _armyIcon.SetAspect((float)t.width / t.height); }
            }
            if (_bgQuad != null)
            {
                var bt = _win.Art(CampaignData.Background(a));
                if (bt != null) { _bgQuad.SetTexture(bt); _bgQuad.SetAspect((float)bt.width / bt.height); }
            }
            RefreshNodes();
        }

        /// <summary>重刷 47 个节点的状态色/高亮（领取之后要调）。
        /// 🆕 也是**滚动区的 `OnChanged`**（接上横向滚动之后，滚动就走到这里重建）。</summary>
        public void RefreshNodes()
        {
            if (_nodeTf == null || _trackContent == null) return;
            for (int i = _trackContent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(_trackContent.GetChild(i).gameObject);
            _nodeTf = null;
            // 🆕 **接上裁剪**（原版 `Viewport` 上的 `RectMask2D`）—— 越出视口的部分逐 quad 截掉
            var prevClip = _win.Clip;
            _win.Clip = _vpR;
            for (int i = 0; i < CampaignData.NodeCount; i++)
                foreach (int j in CampaignData.At(i).Next) BuildLine(i, j);
            for (int i = 0; i < CampaignData.NodeCount; i++) BuildNode(i);
            _win.Clip = prevClip;
        }

        /// <summary>点节点。**照原版 `CampaignWindowTab.OnNodeClicked`**：组一个
        /// `CampaignRewardsWindowContext` 开奖励窗（领取发生在窗里那个 `Unlock` 钮上，
        /// **不是点节点就发奖**）。</summary>
        void OnNodeClicked(int i) { ClickNodeForTest(i); }

        /// <summary>自检直调这条路（批处理里没法真的点）。返回「窗开了吗」。
        /// ⚠️ **语义 2026-09-23 改过**：原来这里**直接按基础档结算**（那时还没有
        /// `CampaignRewardWindow`）；现在照原版走**开窗**，真正的领取在 `ClaimForTest`。</summary>
        public bool ClickNodeForTest(int i)
        {
            if (!CampaignData.Claimable(i))
            {
                Debug.Log("[Campaign] 节点 " + CampaignData.NodeName(i) + " 现在点不开："
                          + (CampaignData.StateOf(i) == CampaignData.Locked ? "前驱还没领" : "已经领过了"));
                return false;
            }
            if (_win == null || _win.Manager == null)
            {
                Debug.LogError("[Campaign] 没有 `WindowsManager` ⇒ 开不了奖励窗（**不是静默**：这条是错误）");
                return false;
            }
            var win = CampaignRewardWindow.Create(_win.Manager);
            win.Reopen(BuildContext(i));
            _win.Manager.OpenWindow(win);
            return true;
        }

        /// <summary>组 context。**照原版 `CampaignRewardsWindowContext` 的 8 个字段**（正本 §十四）。
        /// ⚠️ 我们的口径下 `IsPremiumLocked` **恒 false** —— 用户 2026-09-22 裁决「Premium 轨全解锁」
        /// （`资料/阶段二外壳_待裁决清单_0922.md` #2）。</summary>
        public CampaignRewardsContext BuildContext(int i)
        {
            return new CampaignRewardsContext
            {
                Rewards = CampaignData.RewardsOf(i),
                BaseCollected = CampaignData.BaseClaimed(i),
                PremiumCollected = CampaignData.PremiumClaimed(i),
                Claimable = CampaignData.Claimable(i),
                IsPremiumLocked = false,
                PointCost = CampaignData.At(i).Cost,
                Army = 10,                                   // `filter.army 10` = Ultramarines（`Ultramarines Campaign.json` 实读到）
                OnCollect = tier => ClaimForTest(i, tier),
            };
        }

        /// <summary>窗里那个 `Unlock` 钮按下去之后走这里（= 原版 `UnlockClicked` → `TryCollect`）。
        /// 返回「真的领到了吗」；领不到时**说清原因**（红线：不许静默失败）。</summary>
        public bool ClaimForTest(int i, int tier)
        {
            string why;
            if (CampaignData.Claim(i, tier, out why))
            {
                RefreshNodes();
                // 领完就把窗刷新成 `Get` 态（原版 `OnCollect` 之后按钮进 claimed，
                // 而 `CampaignRewardsWindow` 是「领一次就 `CloseWindow`」——见正本 §十四「关窗三条路」）
                Debug.Log("[Campaign] 节点 " + CampaignData.NodeName(i) + " 领到了"
                          + (tier == CampaignData.TierPremium ? "高级档" : "基础档"));
                return true;
            }
            Debug.Log("[Campaign] 节点 " + CampaignData.NodeName(i) + " 领不了：" + why);
            return false;
        }

        // ============================================================ Premium Panel

        /// <summary>`Premium Panel`。**矩形不是 JSON 值** —— 它 dump 出来高 = 0，
        /// 位置与尺寸是 `ContentSizeFitter` + `VerticalLayoutGroup` 跑出来的，见正本 §四的实算。</summary>
        void BuildPremiumPanel(Transform root)
        {
            var panel = new PxRect(344.29f, 867.01f, 720.35f, 1080.00f);
            var p = RewardsWindow.Node(root, "Premium Panel", panel);
            _win.Rect(p, "WF_UI_Ranked_Background_Gold", panel, "Background", QPanel);
            var panTitleR = new PxRect(354.43f, 874.01f, 710.22f, 916.33f);
            var panTitle = _win.Text(p, "Premium Campaign daily bonus", panTitleR.x1, panTitleR.x2,
                                     panTitleR.y1, panTitleR.y2, 5, Color.white, "Title", 33.3f);
            if (panTitle != null)
            {
                panTitle.SetRenderQueue(QPanelTitle);
                // 🔴 **必须开自适应字号** —— 原版那条 TMP 的字段原文是
                //    `m_enableAutoSizing = 1` · `m_fontSizeMin = 10` · `m_fontSizeMax = 40` · `m_TextWrappingMode = 0`（**不折行**）
                //    （`bundle_menus_assets_all/MonoBehaviour/` 里那条 `"Premium Campaign daily bonus"`）⇒
                //    **框装不下时原版是【缩字号】**，不是溢出。
                //    实测：不开自适应时这段 28 字的串在 fs33.3 下**渲出 ≈545px**，而框只有 **355.79px**
                //    ⇒ `AlignRight` 之后**左边冲出面板 180px**（`_tmp_view/rewards/02_战役.png` 一眼可见），
                //    而当时**没有任何断言管它的左边缘**（只断过右边缘）。
                //    ⚠️ 顺序：**先 `SetAutoFitBox` 再对齐**（对齐按当前宽度算）。
                panTitle.SetAutoFitBox(LayoutSpace.Px(panTitleR.W), LayoutSpace.Px(panTitleR.H), 10f, 33.3f);
                MenuDraw.AlignRight(panTitle, panTitleR);
            }
            // `Points`（HLG：Quantity + 战役点图标）—— 实算 rect 见正版 §四
            var panQtyR = new PxRect(458.43f, 916.33f, 535.41f, 974.24f);
            var panQty = _win.Text(p, "200", panQtyR.x1, panQtyR.x2, panQtyR.y1, panQtyR.y2, 5,
                                   Color.white, "Quantity", 61.23f);
            if (panQty != null)
            {
                panQty.SetRenderQueue(QPanelPts);
                MenuDraw.AlignLeft(panQty, panQtyR);
            }
            _win.Rect(p, "40K_genearl_icon_Campaign_points_big", new PxRect(541.22f, 910.43f, 606.22f, 980.15f),
                      "Points", QPanelPts, null, true);
            // `Generic Simplified UI Button`（`Continue`）
            var btn = new PxRect(404.36f, 974.24f, 660.29f, 1029.71f);
            // A17：原版 `Campaign Tab>Premium Panel>Generic Simplified UI Button` 是 SpriteSwap（普查 §块 2 第 7 行）
            var contQ = _win.Rect(p, "UI_Button_Mulligan", btn, "Generic Simplified UI Button", QPanelBtn);
            var btnTxR = new PxRect(btn.x1 + 9f, btn.y1 + 4f, btn.x2 - 9f, btn.y2 - 4f);
            var panBtn = _win.Text(p, "Continue", btnTxR.x1, btnTxR.x2, btnTxR.y1, btnTxR.y2, 5,
                                   Color.white, "Button Text", 49.35f);
            if (panBtn != null) { panBtn.SetRenderQueue(QPanelBtn); MenuDraw.AlignRight(panBtn, btnTxR); }
            AddHit(p, "ContinueHit", btn, QPanelBtn, () =>
                Debug.Log("[Campaign] `Continue`：原版是「领高级每日奖励」，走 PlayFab 云脚本 —— **单机没有服务器**，本轮不实现"),
                contQ, "UI_Button_Mulligan");
            // `Timer`（HLG：时钟图标 + 倒计时文本）
            _win.Rect(p, "WF_icon_clock", new PxRect(370.10f, 1030.09f, 408.63f, 1068.62f), "Icon", QPanelTimer,
                      new Color(0.764f, 0.764f, 0.764f, 1f));
            var panTimerR = new PxRect(408.63f, 1037.15f, 694.55f, 1061.57f);
            var panTimer = _win.Text(p, "Siguiente: 5d 20h 15m", panTimerR.x1, panTimerR.x2,
                                     panTimerR.y1, panTimerR.y2, 5,
                                     new Color(0.745f, 0.745f, 0.745f, 1f), "Timer Text", 37.9f);
            if (panTimer != null)
            {
                panTimer.SetRenderQueue(QPanelTimer);
                MenuDraw.AlignLeft(panTimer, panTimerR);
            }
        }

        // ============================================================ 小工具

        void AddHit(Transform parent, string name, PxRect r, int q, System.Action onClick,
                    ImageQuad target = null, string art = null, string hoverArt = null, string pressedArt = null)
        {
            var hit = RewardsWindow.New(parent, name);
            var hq = ImageQuad.Create(hit, CardArt.Solid(), RewardsWindow.Local(parent, r.x1, r.y1, r.x2, r.y2),
                                      LayoutSpace.Px(r.H), new Vector2(0.5f, 0.5f), "Hit");
            if (hq != null)
            {
                hq.SetAspect(r.W / Mathf.Max(1e-6f, r.H));
                hq.SetTint(new Color(0f, 0f, 0f, 0f));
                hq.SetRenderQueue(q);
            }
            var wb = hit.gameObject.AddComponent<WindowButton>();
            wb.onClick = onClick;
            if (target != null) wb.Bind(target, art, hoverArt, pressedArt);
        }

        public string Dump()
        {
            int unlocked = 0;
            for (int i = 0; i < CampaignData.NodeCount; i++) if (CampaignData.StateOf(i) >= CampaignData.Unlocked) unlocked++;
            return "Campaign：阵营 " + CampaignData.Selected + "（有内容=" + CampaignData.HasContent(CampaignData.Selected) + "）"
                   + " · 节点 " + CampaignData.NodeCount + "（开着的 " + unlocked + " · 已领 " + CampaignData.ClaimedCount + "）"
                   + " · 缩放比 " + Ratio.ToString("F4") + "（行距 " + (128f * Ratio).ToString("F1") + "px）"
                   + " · 首奖励无图 " + NoIconRewardNodes + " 个节点 / " + NoIconRewards.Count + " 个 id";
        }
    }
}
