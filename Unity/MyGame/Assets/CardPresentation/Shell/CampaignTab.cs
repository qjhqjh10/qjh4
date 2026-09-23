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
//   · **节点上的奖励物品图标**：原版 `CampaignNode.Setup` 里 `ItemDrawer.Draw(itemHolder, 首奖励, …)`；
//     我们还没有 ItemDrawer（奖励物品 id 是 `Booster Pack Ultramarines` / `WildcardUltramarines2` 这类**服务端 id**）。
//   · **`RectMask2D` 遮罩**：两个 `Viewport` 的裁剪没实现 ⇒ 轨道右侧会画到屏外（见 `Dump()` 的说明）。
//   · **横向滚动**：原版 `Campaign Track` 是横向 ScrollRect；我们还没做滚动 ⇒ **把内容左移，让最左节点贴视口左边**
//     （节点之间的**相对位置与缩放比照原版**，只是整体做了位移补偿）。
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

        /// <summary>渲染队列：照原版兄弟序逐层 +1（见纪律⑤）。</summary>
        public const int QTabBg = 3030, QTabBgImage = 3031, QTabSel = 3032,
                           QArmyItem = 3033, QArmyArrow = 3034, QArmyIcon = 3035,
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
        /// <summary>阵营选择条的条目尺寸。原版 `Campaign Army Item Button` 的根尺寸**没查到确证**
        /// （0917 记为 `[-60,1080 120x0]`）⇒ 这里沿用**母版 `Army Item Button` 的 136.36×121.59**
        /// （两者 29 个节点逐项相同、只差高亮底图与一条进度条）—— ⚠️ **这一格是推的，不是直读**。</summary>
        const float ArmyItemW = 136.36f, ArmyItemH = 121.59f, ArmySpacing = -14f;

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
        public override void OnOpen() { }

        Transform _armyContent, _trackContent;
        Label _title, _points;
        ImageQuad _armyIcon;
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
            _armyContent = RewardsWindow.Node(selVp, "Army Content", new PxRect(_selR.x1, _selR.y1, _selR.x1, _selR.y2));

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
            BuildTrack();

            // ---- ⑤ `Premium Panel`（左下常显的面板；高度是布局组算出来的）----
            BuildPremiumPanel(root);

            // ---- ⑥ 两条活数据 ----
            BuildArmyItems();
            Refresh();
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
            // ⚠️ 原版是横向 ScrollRect、起手滚到最左；我们**还没做滚动、也没做 `RectMask2D`** ⇒
            //    把内容**整体右移一个「光圈半径」**，否则最左那个节点的 `Collectable Highlight`
            //    （178.915² 的圆环）会**画到左栏四键上面去**（2026-09-23 实测：白圈压住了 FORGE 那个键）。
            //    **相对位置与缩放比照原版**，这里只做**位移补偿**（原因写在 `Dump()` 里）。
            _shiftX = _vpR.x1 + HighlightSize * 0.5f - minX * Ratio;
            _centerY = _vpR.CY;

            // 连线**先建**（照原版 `SetAsFirstSibling`：连线在节点的所有图形下面）
            for (int i = 0; i < CampaignData.NodeCount; i++)
                foreach (int j in CampaignData.At(i).Next) BuildLine(i, j);

            for (int i = 0; i < CampaignData.NodeCount; i++) BuildNode(i);
        }

        float _shiftX, _centerY;

        /// <summary>节点在 `Content` 里的中心（原版 `GetNodePosition`：`(Position − offset) * ratio`，**y 取反**）。</summary>
        Vector2 NodePos(int i)
        {
            var n = CampaignData.At(i);
            return new Vector2(_shiftX + n.X * Ratio, _centerY - n.Y * Ratio);
        }

        PxRect NodeRect(int i)
        {
            Vector2 c = NodePos(i);
            float h = NodeSize * 0.5f;
            return new PxRect(c.x - h, c.y - h, c.x + h, c.y + h);
        }

        Transform[] _nodeTf;

        /// <summary>一个节点。**照原版 `CampaignNode.Setup` + `SetNodeStyle`**：
        /// ① `Premium Mark` 只在「该节点有高级档奖励」时显示；② 圆盘按**状态色**着色；
        /// ③ `Collectable Highlight` 只在 `Unlocked` 且点数够时显示。</summary>
        void BuildNode(int i)
        {
            var r = NodeRect(i);
            var node = RewardsWindow.Node(_trackContent, "CampaignNode_" + i, r);
            if (_nodeTf == null) _nodeTf = new Transform[CampaignData.NodeCount];
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
            // `Item Holder`：奖励物品的运行时挂点（出厂空；**本轮不画物品图标** —— 见文件头）
            var ih = UguiRect.Child(r, UguiRect.A00, UguiRect.A11, UguiRect.P50c, Vector2.zero, Vector2.zero);
            RewardsWindow.Node(node, "Item Holder", ih);

            AddHit(node, "Hit", r, QNodeItem, () => OnNodeClicked(i));
        }

        /// <summary>一条连线。**照原版 `CampaignNode.Connect`**：
        /// `rim = 方向 × 节点半宽 × K` · 起点 = 源节点中心 + `rim` · 终点 = 目标节点中心 ·
        /// 长度 = 两点距离 · `right = rim`（即旋转角跟着方向走）。</summary>
        void BuildLine(int i, int j)
        {
            Vector3 wi = LayoutSpace.FromPixel(NodePos(i).x, NodePos(i).y);
            Vector3 wj = LayoutSpace.FromPixel(NodePos(j).x, NodePos(j).y);
            Vector3 d = wj - wi;
            if (d.sqrMagnitude < 1e-6f) return;
            Vector3 dir = d.normalized;
            float halfW = LayoutSpace.Px(NodeSize) * 0.5f;
            Vector3 rim = new Vector3(dir.x * halfW, dir.y * halfW, 0f) * RimK;
            Vector3 a = wi + rim, b = wj;
            Vector3 ab = b - a;
            if (ab.sqrMagnitude < 1e-6f) return;

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
            for (int i = 0; i < CampaignData.Armies.Length; i++)
            {
                string army = CampaignData.Armies[i];
                var r = UguiLayout.HorizontalChild(_selR, ArmyItemW, ArmyItemH, i, 0f, ArmySpacing);
                var item = RewardsWindow.Node(_armyContent, "CampaignArmyItem_" + i, r);
                if (army == CampaignData.Selected)
                    _win.Rect(item, "40K_settings_button_selected", r, "HighlightBG", QArmyItem,
                              new Color(1f, 0.631f, 0.2784f, 1f));      // 母版是**橙**（Forge 页那份才是品红）
                _win.Rect(item, DeckRuntime.FactionIcon(army),
                          UguiRect.Child(r, new Vector2(0.0807f, 0.0822f), new Vector2(0.9267f, 0.9260f),
                                         UguiRect.P50c, Vector2.zero, Vector2.zero),
                          "Icon", QArmyIcon, null, true);
                AddHit(item, "Hit", r, QArmyIcon, () => SelectArmy(army));
            }
        }

        /// <summary>换阵营。**照原版 `CampaignWindowTab.ClickChangeArmy`**：不同才换 → `SetActiveCampaign` → `handler.SetSelectedArmy`。
        /// ⚠️ **本地只有 UM 一套内容** ⇒ 选别的阵营时**如实说明**（不许静默失败）。</summary>
        public void SelectArmy(string army)
        {
            CampaignData.Select(army);
            if (!CampaignData.HasContent(army))
                Debug.Log("[Campaign] 选中了 `" + army + "`，但**这一套战役本地没有**"
                          + "（只导出了 Ultramarines 那 47 个节点 SO）—— 轨道按空态显示，不假装有内容");
            for (int i = _armyContent.childCount - 1; i >= 0; i--) Object.DestroyImmediate(_armyContent.GetChild(i).gameObject);
            BuildArmyItems();
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

        /// <summary>重刷 47 个节点的状态色/高亮（领取之后要调）。</summary>
        public void RefreshNodes()
        {
            if (_nodeTf == null || _trackContent == null) return;
            for (int i = _trackContent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(_trackContent.GetChild(i).gameObject);
            _nodeTf = null;
            for (int i = 0; i < CampaignData.NodeCount; i++)
                foreach (int j in CampaignData.At(i).Next) BuildLine(i, j);
            for (int i = 0; i < CampaignData.NodeCount; i++) BuildNode(i);
        }

        /// <summary>点节点。**照原版 `CampaignWindowTab.OnNodeClicked`**：组一个 context 开奖励窗。
        /// ⚠️ **`CampaignRewardsWindow` 本轮还没建** ⇒ 这里**如实说明**并**直接按基础档结算**
        /// （免得变成一个点了没反应的按钮 —— 项目红线）。等奖励窗做完再把这一跳改成开窗。</summary>
        void OnNodeClicked(int i) { ClickNodeForTest(i); }

        /// <summary>自检直调这条路（批处理里没法真的点）。语义与 `OnNodeClicked` 一致。</summary>
        public bool ClickNodeForTest(int i)
        {
            string why;
            if (!CampaignData.Claimable(i))
            {
                Debug.Log("[Campaign] 节点 " + CampaignData.NodeName(i) + " 现在点不了："
                          + (CampaignData.StateOf(i) == CampaignData.Locked ? "前驱还没领" : "已经领过了"));
                return false;
            }
            if (CampaignData.Claim(i, CampaignData.TierBasic, out why)) { RefreshNodes(); return true; }
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
            _win.Rect(p, "UI_Button_Mulligan", btn, "Generic Simplified UI Button", QPanelBtn);
            var btnTxR = new PxRect(btn.x1 + 9f, btn.y1 + 4f, btn.x2 - 9f, btn.y2 - 4f);
            var panBtn = _win.Text(p, "Continue", btnTxR.x1, btnTxR.x2, btnTxR.y1, btnTxR.y2, 5,
                                   Color.white, "Button Text", 49.35f);
            if (panBtn != null) { panBtn.SetRenderQueue(QPanelBtn); MenuDraw.AlignRight(panBtn, btnTxR); }
            AddHit(p, "ContinueHit", btn, QPanelBtn, () =>
                Debug.Log("[Campaign] `Continue`：原版是「领高级每日奖励」，走 PlayFab 云脚本 —— **单机没有服务器**，本轮不实现"));
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

        void AddHit(Transform parent, string name, PxRect r, int q, System.Action onClick)
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
        }

        public string Dump()
        {
            int unlocked = 0;
            for (int i = 0; i < CampaignData.NodeCount; i++) if (CampaignData.StateOf(i) >= CampaignData.Unlocked) unlocked++;
            return "Campaign：阵营 " + CampaignData.Selected + "（有内容=" + CampaignData.HasContent(CampaignData.Selected) + "）"
                   + " · 节点 " + CampaignData.NodeCount + "（开着的 " + unlocked + " · 已领 " + CampaignData.ClaimedCount + "）"
                   + " · 缩放比 " + Ratio.ToString("F4") + "（行距 " + (128f * Ratio).ToString("F1") + "px）";
        }
    }
}
