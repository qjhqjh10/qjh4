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
    /// <summary>战役页。原版 `CampaignWindowTab : WindowTabBase&lt;MainMenuRewardsWindow>`。</summary>
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
        /// <summary>🆕 **2026-10-13（A465 · W-A435己）**：`Campaign Track/Viewport` 那颗节点
        /// （= `Build()` 里 `ViewportClip.Hang` 建的那一颗）。`BuildTrack()` 拿它喂 `_trackScroll.ClipNode`
        /// —— **每次 `BuildTrack()` 都重喂一次**：`Build()` 每次都会新建节点，而 `_trackScroll`
        /// 只在 `== null` 时新建（`BuildTrack` 的 `if` 支）⇒ 一次性赋值会**静默**停在已销毁的组件上
        /// （Unity 对已销毁对象判 `!= null` 为 `false`，于是悄悄回落 `Viewport`）。</summary>
        ViewportClip _trackVp;
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
            // 🔴 **A353 第①处**：原版这一件**没有 mask**（`ClearClip` 的注释里有判据与反证）⇒ 显式清三件套。
            var noClip1 = ClearClip();
            var cbg = RewardsWindow.Node(root, "Campaign Background",
                UguiRect.Child(_tabR, UguiRect.A00, UguiRect.A11, new Vector2(0f, 0.5f),
                               new Vector2(-0.344849f, 0f), new Vector2(0.34485f, 0f)));
            // 纪律④：这个子件**出厂是关的、运行时才开**，所以建它、并且开着
            var bgImg = UguiRect.Child(_tabR, UguiRect.A00, UguiRect.A11, new Vector2(0f, 0.8f),
                                       Vector2.zero, new Vector2(0f, 580.595f));
            _bgQuad = _win.Rect(cbg, CampaignData.Background(CampaignData.Selected), bgImg, "Background Image", QTabBgImage);
            RestoreClip(noClip1);

            // ---- ② `Campaign Army Selector`（阵营选择条）----
            // 🔴 **A353 第②处**：`Campaign Army Selector/Background` 原版**没有 mask**（这正是它与
            //   `…/Viewport` 的区别 —— 视口那条 mask 在 `BuildArmyItems` 里成对拿捏）⇒ 显式清三件套。
            var noClip2 = ClearClip();
            var sel = RewardsWindow.Node(root, "Campaign Army Selector", _selR);
            _win.Rect(sel, null, UguiRect.Child(_selR, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                      new Vector2(0f, 1f), new Vector2(-102.678f, 68f), new Vector2(102.677f, 136f)),
                      "Background", QTabSel, new Color(0f, 0f, 0f, 0.349f));
            RestoreClip(noClip2);
            // 🔴🔴 **2026-10-13（A435 甲 · A198② 阶段 2）**：同轨道那一颗 —— 裁切状态长在**视口节点**上，
            //   由 `ViewportClip.Hang` 一次写死（**框 · `padding` · `softness`** 三样）。
            //   判据（原版 `RectMask2D` 实读，`d:/4/_tmp_view/q1_rm2d.txt:186` 与 `:150` 两条路径）：
            //   `soft=(0,0)` · `pad=(0,0,0,0)`。⛔ 别改回 `Node(...)`（理由同上面 `Campaign Track` 那颗）。
            var selVc = ViewportClip.Hang(sel, "Viewport", _selR, Vector4.zero, Vector2Int.zero);
            var selVp = selVc.transform;
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
            // 🔴 **2026-10-13（A465 · W-A435己）**：构建循环那一行（`BuildArmyItems` 里
            //   `if (_armyScroll != null && !_armyScroll.Intersects(r)) continue;`）从今天起读**同一颗节点**的
            //   状态（`MenuScroll.Intersects` 走 `ClipNode.State.RenderClip`）—— 就是上面那颗
            //   `Campaign Army Selector/Viewport`。⚠️ 今天两值同（节点框 = `_selR`、`padding` 全 0）
            //   ⇒ **逐个位不变**。`_armyScroll` 每次 `Build()` 都新建 ⇒ 不会停在已销毁的组件上。
            _armyScroll.ClipNode = selVc;
            _armyScroll.Owner = root.gameObject;
            _armyScroll.OnChanged = BuildArmyItems;
            PointerLayer.RegisterScroll(_armyScroll);

            // ---- ③ `Campaign Header`（阵营徽记 + 名字 + 点数 + 信息钮）----
            // 🔴 **A353 第③处**：`Campaign Header` 整棵（底图 / 徽记 / `Title` / `Points` / `Point Icon` 两张 /
            //   `Info Button`）原版**一个 mask 都没有** ⇒ 显式清三件套（含那两段 `_win.Text` ——
            //   `MenuWindowBase.Text` 也吃 `RenderClip`，`Clip` 非空时它们会被 `MenuDraw.Visible` 判不可见
            //   而**整条返回 null** ⇒ 静默少两段字）。
            var noClip3 = ClearClip();
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
                // 🔴 **2026-10-11（A305①）**：第 5 个实参 = 原版这一颗的 `m_fontSizeBase` **原文**。
                //    判据（原版实读）：`/Campaign Tab/Campaign Header/Title`
                //    `m_fontSize 31.75` · `auto[25~35]` · **`m_fontSizeBase 36.0`**（= TMP 序列化默认值，
                //    即原版**没显式设过** —— `TMP_Text.cs:473` / 开着自适应时 setter 不回写 base，`:467`）。
                //    逐站表 → `资料/普查产出_1011/V7_A305_A304_普查.md` §二·3 #1。
                //    ⚠️ 上限那一格原版是 **35**、我们传的是 31.75（= `m_fontSize`）—— 那是**另一条**（A333），本轮不动。
                _title.SetAutoFitBox(LayoutSpace.Px(_titleR.W), LayoutSpace.Px(_titleR.H), 25f, 31.75f, 36f);
                // 🆕 **2026-10-11（A303②）：还原本条 TMP 的 `m_TextWrappingMode = 0`** ——
                //   上面那句 `SetAutoFitBox` 内部会 `SetWrapWidth`，而那个**无条件**把模式设成
                //   `Normal(=1)`（`Core/TmpFont.cs` 的 `SetWrapWidthRect` 头）。照 A62 那一族的既有写法补一句。
                //   **判据（原版实读，就在本件现场量的）**：`python 工具/menu_dump.py bundle_menus_assets_all
                //   "Rewards Base Submenu Variant" --depth 6` →
                //   `Campaign Header/Title` = 字号 31.75 · auto[25.0~35.0] · 对齐 Left/Bottom · **折行=0**；
                //   （同一条内的 `Campaign Header/Points` 也是 折行=0，见下面那一处。）
                //   ⚠️ **顺序固定 `SetAutoFitBox` → 本句**（A205：`SetWrapping` 内部 `ForceRelayout` 会把版面推下去
                //   ⇒ 谁在它之前对齐，谁就得在它之后再算一次）。本页这两处**在 `Build` 里没有紧跟对齐**
                //   （`Refresh()` 里那次 `MenuDraw.AlignLeft` 是后来单独跑的，它自己会按当前宽度重算）。
                _title.SetWrapping(false);
            }
            var ptsRect = UguiRect.Child(_headerR, new Vector2(0.2f, 0.25f), new Vector2(1f, 0.45f),
                                         UguiRect.P01, new Vector2(57.9551f, 0f), Vector2.zero);
            _pointsR = ptsRect;
            _points = _win.Text(hdr, "", ptsRect.x1, ptsRect.x2, ptsRect.y1, ptsRect.y2, 5,
                                Color.white, "Points", 34.8f);
            if (_points != null)
            {
                _points.SetRenderQueue(QHeaderPts);
                // 🔴 **2026-10-11（A305①）**：base = 原版 `m_fontSizeBase` **36.0**（同上一条，TMP 默认值）。
                //    判据：`/Campaign Tab/Campaign Header/Points` = `m_fontSize 34.8` · `auto[18~40]` · `base 36.0`
                //    （逐站表 §二·3 #2）。⚠️ 上限原版 **40**、我们传 34.8 —— A333，本轮不动。
                _points.SetAutoFitBox(LayoutSpace.Px(ptsRect.W), LayoutSpace.Px(ptsRect.H), 18f, 34.8f, 36f);
                // 🆕 **2026-10-11（A303②）**：同上一处（`_title`）—— 还原本条 TMP 的 `m_TextWrappingMode = 0`。
                //   **判据（原版实读）**：`menu_dump.py bundle_menus_assets_all "Rewards Base Submenu Variant"
                //   --depth 6` → `Campaign Header/Points` = 字号 34.8 · auto[18.0~40.0] · **折行=0**
                //   （原版这颗还带 `ContentSizeFitter(m_HorizontalFit = PreferredSize)` —— 框由内容撑开，**不折行**）。
                _points.SetWrapping(false);
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
            RestoreClip(noClip3);

            // ---- ④ `Campaign Track`：轨道（连线 + 47 个节点）----
            var track = RewardsWindow.Node(root, "Campaign Track", _trackR);
            _vpR = UguiRect.Child(_trackR, UguiRect.A00, UguiRect.A11, UguiRect.P01,
                                  new Vector2(0f, 50f), new Vector2(0f, 50f));
            // 🔴🔴 **2026-10-13（A435 甲 · A198② 阶段 2）**：这一颗是**视口节点**，裁切状态就挂在它身上
            //   （= 原版 `RectMask2D` 挂 `Campaign Track/Viewport`；契约 → `Shell/ViewportClip.cs` 文件头）。
            //   建节点走 `ViewportClip.Hang`（= `MenuDraw.Node` + `AddComponent<ViewportClip>`）⇒
            //   **框（节点自己的 rect）· `padding` · `softness` 三样一次写死**，此后生产代码一个字都不再动它。
            //   判据（原版 `RectMask2D` 实读，`d:/4/_tmp_view/q1_rm2d.txt:297-298` 与 `:93-94` 两条路径）：
            //   `soft=(0,0)` · `pad=(0,0,0,0)`。
            //   ⛔ **别改回 `Node(...)`**：那样这颗节点上就没有状态了，`Editor/RewardsScene.cs` 的
            //   A489 那条（`NodeResolutions > 0`）与 A303① 那三条会一起红。
            var trackVc = ViewportClip.Hang(track, "Viewport", _vpR, Vector4.zero, Vector2Int.zero);
            _trackVp = trackVc;                          // 🔴 A465（W-A435己）：`BuildTrack()` 拿它喂 `_trackScroll.ClipNode`
            var vpNode = trackVc.transform;
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

            // 🔴 **2026-10-13（A465 · W-A435己）**：构建循环那一行（`BuildNode` 里
            //   `if (_trackScroll != null && !_trackScroll.Intersects(r)) …`）从今天起读**同一颗节点**的状态
            //   （`MenuScroll.Intersects` 走 `ClipNode.State.RenderClip`）—— 就是 `Build()` 里那颗
            //   `Campaign Track/Viewport`。⚠️ 今天两值同（节点框 = `_vpR`、`padding` 全 0）⇒ **逐个位不变**。
            //   🔴 **为什么写在 `if/else` 【之后】而不是新建那一支里**：`_trackScroll` 只在 `== null` 时新建
            //   （这一支），而**节点每次 `Build()` 都是新的** ⇒ 只喂一次会在第二次 `Build()` 之后
            //   **静默**停在已销毁的组件上（Unity 判 `!= null` 为 `false` ⇒ 悄悄回落 `Viewport`）
            //   —— 同一形状的坑见 `Shell/ShopWindow.cs` 的 `_gridScroll`（A762 记的）。
            _trackScroll.ClipNode = _trackVp;

            // 🔴🔴 **2026-10-13（A435 甲 · A198② 阶段 2）：三件套【改了载体】—— 这一段的三行没了。**
            //   原来这里写的是「`prevClip/prevPad/prevSoft` 存一份 → 设 `_win.Clip = _vpR` / pad 0 / soft 0 →
            //   建完再成对还原」（A326 那一批的写法）。现在**状态长在【视口节点】上**（= 原版
            //   「每个 `Viewport` 一个 `RectMask2D`」，判据/契约 → `Shell/ViewportClip.cs` 文件头）：
            //   `Build()` 里 `ViewportClip.Hang(track, "Viewport", _vpR, Vector4.zero, Vector2Int.zero)`
            //   那**一句**已经把 **框（= 节点自己的 rect）· `padding` · `softness`** 三样一起写死在
            //   `Campaign Track/Viewport` 那颗节点上 ⇒ **这里一个字都不用设，也就没有「还原」这回事**
            //   （节点是**常驻**状态 —— 这正是原版那个组件的样子；自检由
            //   `Editor/RewardsScene.cs` 的 A303① 那三条守：节点上写的是原版那两个值、`RefreshNodes()`
            //   一个字段都不许动它、以及控制组「节点软边非 0 ⇒ 建出来的 quad 带顶点色」）。
            //   **判据（原版 `RectMask2D` 实读 —— 2026-10-11 当场复扫 `_tmp_view/q1_rectmask2d_paths.py`）**：
            //     · `Campaign Tab/Campaign Track/Viewport`（MB `_8978203380136193421`）
            //       = **`soft=(0,0)` · `pad=(0.0,0.0,0.0,0.0)` · `en=1`**（留档 `d:/4/_tmp_view/q1_rm2d.txt:297-298`）
            //     · `Rewards Base Submenu Variant/…/Campaign Tab/Campaign Track/Viewport`
            //       （MB `_-7653785760633121025`）= 同一份值（`:93-94`）。
            //   ⚠️ **逐处实读，⛔ 不是照抄隔壁**（铁律 5·c）：这三处**恰好**同值；而同页另一条视口
            //      （`Campaign Army Selector/Viewport`）与锻造轨道那一条**都是各自读出来的** ——
            //      锻造轨道的 `pad` 就**不是**零（`(10,0,0,0)`，`ForgeTab` 的 `TrackPad`）。
            //   🔴 **改坏法**（现在唯一能红的地方）= 改 `Build()` 里 `ViewportClip.Hang(…)` 那两个实参；
            //      ⛔ **别再把那三行 `_win.Clip…` 加回来** —— 那会让 A489 那条
            //      `NodeShadowedByParam == 0` 红（= 「节点挂着却一个像素都不生效」）。

            // 连线**先建**（照原版 `SetAsFirstSibling`：连线在节点的所有图形下面）
            for (int i = 0; i < CampaignData.NodeCount; i++)
                foreach (int j in CampaignData.At(i).Next) BuildLine(i, j);

            for (int i = 0; i < CampaignData.NodeCount; i++) BuildNode(i);
        }

        /// <summary>清空轨道的内容（`_trackContent` 的全部子件 + `_nodeTf`）。
        /// 🔴 **唯一一份** —— `RefreshNodes` 那句「先销毁再重建」与本类那个自检钩子都走它，
        /// ⛔ 别把那三行再抄一遍（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。</summary>
        void ClearTrackContent()
        {
            if (_trackContent == null) return;
            for (int i = _trackContent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(_trackContent.GetChild(i).gameObject);
            _nodeTf = null;
        }

        /// <summary>自检直调：**单独**跑一趟 `BuildTrack()`（⛔ 不是 `Build()`）。
        /// 🔴 **为什么非要这个钩子**（2026-10-11 · A326，**按代码路径推演**、⛔ 没实跑）：
        ///   `Build()` 末尾那句 `Refresh()` → `RefreshNodes()` 会把 `_trackContent` **整棵销毁再重建**，
        ///   而那一趟用的是 `RefreshNodes` 自己设对的三件套 ⇒ **「`BuildTrack` 里设没设」在完整
        ///   `Build()` 之后观测不到**（「下毒 → `Build()` → 数 `CampaignNode_*` 里带顶点色的 quad」
        ///   **恒 0 = 空转**：删掉那三行照样绿）。⇒ 只有把这一趟**单独**跑出来，三件套才有观测面
        ///   （断言全文 → `资料/普查产出_1011/WA3_A326.md` §四）。
        /// ⚠️ `BuildTrack` **自己不幂等**（与 `BuildArmyItems` / `RefreshNodes` 那两处不一样，它不先清）
        ///   ⇒ 本钩子先 `ClearTrackContent()`，这样「这一趟建的」= 树里唯一的那一份（否则同名节点叠两套、
        ///   断言数出来的是两趟的和）。
        /// ⛔ 生产路径一次都不调它（`Setup` → `Build` 那条链一个字没改）。</summary>
        public void BuildTrackForTest() { ClearTrackContent(); BuildTrack(); }

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
            // 节点名（**我们挑的**）：原版这里是 `Instantiate` 出来的抽屉 prefab、名字在 prefab 里。
            // 🔴 **2026-10-03 就地更正（铁律 5 · A86）**：本行原来接着写「（本地没有）」—— **假的**：
            //    那批抽屉 prefab **dump 得出来**（`python 工具/menu_dump.py bundle_menus_assets_all "Deck Drawer"`
            //    摊得出整棵子树、每层的节点名都在）⇒ 「这个名字还是我们挑的」的理由是**我们还没照它改**
            //    （= `Shell/ItemDrawer.cs` 文件头 ② 那件待做的活），**不是读不到**。
            // ⚠️ **别叫 `Item Drawer`** —— 普查 §三 里那 4 个同名件其实是 **GridLayoutGroup 容器、不是抽屉本体**，
            //    而别的窗（商店 / 头像页 / 称号页）已经有同名的节点。这里用**抽屉类名**，
            //    自检可以直接断「这一格用的是哪个抽屉」。
            st.NodeName = ItemDrawer.PickDrawer(item, DrawerOverride.Icon) ?? "Item Drawer";
            st.QuantityPx = 0f;                               // `Icon` 档不画数量（`WildcardIconDrawer.Draw` 只碰一张图）
            st.NamePx = 0f;                                   // **我们挑的**：节点只有 100²，8 位 hex 的短名在这里读不出来 ⇒ 不画
            // 🔴 **2026-10-13（A435 甲 · B9）：这一行原来把窗级状态【派生】喂给抽屉** ——
            //   `st.Clip = _win != null ? _win.Clip : null;`。迁到「状态长在视口节点上」之后
            //   `_win.Clip` **恒为 `null`**（本页一个字节都不写它了）⇒ 这一行成了**恒等于默认值的派生**。
            //   ⇒ 整行删掉：抽屉自己的那两条守卫（`VisibleAbove` / `ClipText`）**沿父链解析**
            //   （这些格挂在 `_trackContent` 之下 ⇒ 命中 `Campaign Track/Viewport` 那颗节点）。
            //   ⚠️ `ItemDrawerStyle.Clip` 这个字段**留着**（= 显式覆盖的口子，同 `MenuDraw.Rect` 的 `clip` 形参）——
            //   唯一还用它的是 `Editor/RewardsScene.cs` 的 A302 探针（它**故意**显式传一个框来验裁切）。
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
                                 + " —— 原版走 `ItemDrawer`；🔴 2026-10-03 更正（A86）：本行原来接着说「`ItemDrawerConfig` SO"
                                 + "与那批抽屉 prefab 本地都没有」，**假的** —— SO 已整张解出、prefab 也 dump 得出来；"
                                 + "**真正缺的是这几个 id 的图标**（那批 id 连 SO 都没导出 ⇒ 判据空）"
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
            // 🔴 **2026-10-11（A306⑤）**：`− parent.position` → `− MenuDraw.PosInDesignSpace(parent)`。
            // 这个是「**设计点 − 父的世界位置** → 写进 `localPosition`」那个形状里**连包装都没有**的一份
            // （直接喂 `ImageQuad.Create` 的 `pos`，所以最容易被漏），与 A294 / A297 修掉的
            // `MenuDraw.Local` / `MainMenuSubmenuWindow.Local` 是**同一个病**：`a + ab*0.5f` 是**设计**世界坐标
            // （`LayoutSpace.FromPixel` 出来的），而 `parent.position` 是**已缩放**的视觉世界坐标 ——
            // 小屏缩放开关一开（窗根 ×M）两者差一层 `lossyScale`。
            // ⚠️ **上面那句视口剔除（`midPx` 比 `_vpR`）不用改**：两边本来就是设计 px/设计世界坐标
            //（`ToPixel(a + ab*0.5f)` ↔ `_vpR` 的设计 px），与这里要修的量纲不是同一处。
            // 📌 **`k == 1`（缩放开关出厂关）时与改前【逐位相同】**。
            // 🔴 **改坏法**：换回裸 `parent.position` ⇒ **今天一条现有断言都不会红**（`k == 1` 两式逐位相同
            // ⇒ 这是**潜伏缺陷**）⇒ 要补的两态断言写在 `资料/普查产出_1011/W4_子3.md` §四，由调度台安排。
            var q = ImageQuad.Create(parent, _win.Art("40k_Generic_Smooth_line"),
                                     a + ab * 0.5f - MenuDraw.PosInDesignSpace(parent), LayoutSpace.Px(LineH),
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
            // 🔴🔴 **2026-10-13（A435 甲 · A198② 阶段 2）：三件套【改了载体】—— 这里原来那六行没了。**
            //   旧写法（A306 追加件 · W4·子3 · C）= 「存 `Clip`/`ClipPad`/`ClipSoftness` 三件 → 设成
            //   `_selR` / 0 / 0 → 建完成对还原」。现在状态长在**视口节点**上：
            //   `Build()` 里 `ViewportClip.Hang(sel, "Viewport", _selR, Vector4.zero, Vector2Int.zero)`
            //   那一句已经把 **框 · `padding` · `softness`** 三样写死在 `Campaign Army Selector/Viewport` 上
            //   ⇒ 本函数一个字都不设（节点是**常驻**状态 —— 原版 `RectMask2D` 就是这个样子）。
            //   **判据（原版 `RectMask2D` 实读，`d:/4/_tmp_view/q1_rm2d.txt`）**：
            //     · `Campaign Tab/Campaign Army Selector/Viewport`（`:186`）= **`soft=(0,0) pad=(0,0,0,0)`**
            //     · `Rewards Base Submenu Variant/Content Area/Tabs/Campaign Tab/Campaign Army Selector/Viewport`
            //       （`:150`）= 同一份值（同一个视口的另一条路径）。
            //   🔴 **改坏法**（现在唯一能红的地方）= 改 `Build()` 里 `ViewportClip.Hang(…)` 那两个实参
            //      ⇒ `Editor/RewardsScene.cs` 的 A327·C 那几条立刻红（含「命中区左沿 = `V.x1 + pad`」那条）；
            //      ⛔ **别再把那六行加回来** —— 那会让 A489 的 `NodeShadowedByParam == 0` 红。

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
            ClearTrackContent();
            // 🔴🔴 **2026-10-13（A435 甲 · A198② 阶段 2）：三件套【改了载体】—— 这里原来那六行没了。**
            //   旧写法 = 「存三件 → 设 `_win.Clip = _vpR` / pad 0 / soft 0 → 建完还原」（A48 与 A303① 两批
            //   先后补出来的）。现在裁切状态长在**视口节点**上 —— `Build()` 里
            //   `ViewportClip.Hang(track, "Viewport", _vpR, Vector4.zero, Vector2Int.zero)` 那一句已经把
            //   **框 · `padding` · `softness`** 三样写死在 `Campaign Track/Viewport` 上 ⇒ 本函数一个字都不设。
            //   **判据（原版 `RectMask2D` 实读，`d:/4/_tmp_view/q1_rm2d.txt:297-298` 与 `:93-94`）**：
            //   `Campaign Tab/Campaign Track/Viewport` 与另一条路径同值 = `soft=(0,0)` · `pad=(0,0,0,0)`
            //   （= **硬边**；pad 非零的只有**锻造**那一族 —— `(10,0,0,0)`，⛔ 别按族照抄）。
            //   🔴 **改坏法**（现在唯一能红的地方）= 改 `Build()` 里 `ViewportClip.Hang(…)` 那两个实参
            //      ⇒ `Editor/RewardsScene.cs` 的 A303① 三条立刻红（含「`RefreshNodes()` 一个字段都不许动节点」
            //      与控制组「节点软边非 0 ⇒ quad 带顶点色」）；
            //      ⛔ **别再把那六行加回来** —— 那会让 A489 的 `NodeShadowedByParam == 0` 红。
            for (int i = 0; i < CampaignData.NodeCount; i++)
                foreach (int j in CampaignData.At(i).Next) BuildLine(i, j);
            for (int i = 0; i < CampaignData.NodeCount; i++) BuildNode(i);
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
            // 🔴 **2026-10-14（A814）就地订正（铁律 5）—— 这是一条【生产路径】上的真缺陷**（由 Block14 的写手在加 A469 断言时查出）：
            //   原来写的是 `win.Reopen(BuildContext(i)); _win.Manager.OpenWindow(win);` ——
            //   而 `OpenWindow` 走的是**带参** `TryOpen(null)`（`Shell/WindowsManager.cs`）⇒ 第一句就是
            //   `SetupData(null)` ⇒ **把刚置好的 `_ctx` 又清成 null**（`CampaignRewardWindow.SetupData`），
            //   紧接着 `Open()` = `Build()` 按**空 context** 重建 ⇒ **点节点开出来的窗是空的**
            //   （两列全关、一颗 `Unlock Button` 都没有）。
            //   ✅ 正确做法 = 把 ctx 从**带参那一跳**喂进去 —— `SetupData` 本来就会把它塞进 `_ctx`
            //   （同族先例：`Editor/RewardsScene.cs` 的夹具一律 `wm2.OpenWindow(cw, ctx0)`）。
            //   ⛔ 别改回 `Reopen(...)`（那会重建两次、且第二次是空的）。
            _win.Manager.OpenWindow(win, BuildContext(i));
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
        /// 返回「真的领到了吗」；领不到时**说清原因**（红线：不许静默失败）。
        /// <para>🆕 **2026-10-12（A438）：领到那一拍要【弹领奖窗】**（判据逐条在下面，第一权威 = 反编译方法体）。
        /// <list type="number">
        /// <item>原版**领取成功 ⇒ 开领奖窗**：`d:/2/tools/decomp_full/`
        ///   `Everguild.LiveOps.Campaign.__c__DisplayClass11_0___CollectRewards_g__OnSuccess_0.c:30`（现读）
        ///   = `RewardService__Collect(uVar1, **1**, uVar6, 0, 1, 0, 1, 0);` ⇒ 第 2 参 `showAnimation = 1`。
        ///   📌 `资料/普查产出_1012/H15_战役节点与商店购买.md` §三 记的是 `:27` —— **行号已漂，认那条语句**。</item>
        /// <item>第 2 参**就是** `showAnimation`（⛔ 别按位置猜）：`d:/2/tools/il2cpp_out/dump.cs:94273` 的签名
        ///   `Collect(IReadOnlyList&lt;RewardInfo&gt; rewards, bool showAnimation = True, Action&lt;…&gt; onCollected,
        ///   IReadOnlyList&lt;RewardInfo&gt; collectedRewards, bool isPremiumLocked = True, string customTitle,
        ///   bool showXpToast = True)`；反编译那个调用点尾参多一个 `MethodInfo*` ⇒ 按**顶层括号**切。</item>
        /// <item>`showAnimation == 0` 那一支**根本不开窗**：`decomp_full/RewardService__Collect.c:170`
        ///   （现读 = `if (param_2 == '\0') {` —— `param_2` 就是第 2 个形参）。</item>
        /// <item>我们这一侧的「真领到那一拍」= `CampaignData.Claim(i, tier, out why) == true`（本函数那个
        ///   `if` 的真支）；上游 = 奖励窗里那颗 `Unlock` 钮 → `CampaignRewardWindow.OnUnlock` → `OnCollect`
        ///   （`BuildContext` 把 `OnCollect` 挂成 `tier => ClaimForTest(i, tier)`）。</item>
        /// <item>窗里**装哪几条 = 按下那一档**：`CampaignData.RewardsOf(i, tier)`（`Shell/CampaignData.cs`）——
        ///   它就是原版 `CampaignRewardsWindow.Open` 里 `Rewards.Where(r =&gt; r.rewardTier == tier)` 那个谓词，
        ///   **也是本页奖励窗自己列的那一列**（⛔ 别传不分档的 `RewardsOf(i)`，那是两条）。</item>
        /// <item>关窗回调 = `Refresh()`：对位 = 原版的 `onCollected`；我们这一侧 `Refresh()` 是**幂等**的重刷
        ///   （文本 + 阵营图 + `RefreshNodes()`）。</item>
        /// </list>
        /// ⛔ **不是另写一份开窗** —— 走的是 `RewardWindow.ShowCollected`，与**日常那六条 / 锻造那条**同一个入口
        /// （先例 = `Shell/ForgeTab.cs` 的 `ClaimCell`，那一处是本类该照着写的那一条）。
        /// ⚠️ **一处【还没查清】**（照实写，不猜）：领**高级档**时 `CampaignData.Claim` 会把基础档**顺带置真**
        /// （引擎语义如此），而原版那扇窗里**会不会**因此多画一条基础档 —— **判据在服务端**（`Collect` 的回包
        /// `collectedRewards` 由服务端给），本地查不到 ⇒ 我们按「**只画按下那一档**」落，留着这一条。</para></summary>
        public bool ClaimForTest(int i, int tier)
        {
            string why;
            if (CampaignData.Claim(i, tier, out why))
            {
                RefreshNodes();
                // 🆕 2026-10-12（A438）：**领到那一拍弹领奖窗**（判据见方法头；`showAnimation = 1` 那一支）。
                RewardWindow.ShowCollected(CampaignData.RewardsOf(i, tier), collected => Refresh());
                // 🔴 **2026-10-12（A466 顺手订正 · 铁律 5）**：这两句原来是「领完就把窗刷新成 `Get` 态
                //   （原版 `OnCollect` 之后按钮进 claimed，而 `CampaignRewardsWindow` 是「领一次就
                //   `CloseWindow`」——见正本 §十四「关窗三条路」）」—— **前半句是半错的**：
                //   它描述的是**窗还留着**时的样子，而 A448 之后窗**直接关**（领取成功那一拍
                //   `CampaignRewardWindow.OnUnlock` 走原版 `g__Refresh_0` 末尾那一跳 `Close()`；
                //   判据 → `Shell/CampaignRewardWindow.cs` 的 `OnUnlock` 方法头）⇒ 本窗是**被关掉**、
                //   不是被刷新；`Get` 态要**重新点节点把奖励窗开回来**才看得到。
                // ⚠️ 本件**只订正这句注释**（白名单只到这里），代码一个字符都没动。
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
            // 🔴 **A353 第④处**：`Premium Panel` 整块（底图 / `Title` / 两张点图标 / `Quantity` / 按钮 +
            //   文字 + 命中区 / `Timer` 的图标与文字）原版**没有 mask** ⇒ 显式清三件套。
            //   ⚠️ **本函数在 `Build()` 里排在 `BuildTrack()`（第 ④ 步）【之后】** —— 旧时候 `BuildTrack` 自己
            //   成对拿捏过 `Clip = _vpR`，它**一旦不还原**，这一整块（`344.29,867.01 → 720.35,1080.00`，
            //   整块落在轨道视口 `_vpR` 的下沿之外）就会被**整个裁掉**（连 `AddHit` 的命中区一起）
            //   —— 那正是「谁设 `Clip` 谁负责还原」那条纪律的**观测面**（判据 → `WA3_A326.md` §四·4 可选条）。
            //   🔴 **2026-10-13（A435 甲）：那个观测面【消失了】**（`BuildTrack` 不再写窗字段）——
            //   现在保护这一块的是**结构**：它挂 `root` 的直系下，而两颗视口节点都在
            //   `Campaign Track` / `Campaign Army Selector` 里面（**不在它的父链上**）⇒ 节点态解析不到裁切。
            //   本句 `ClearClip()` 保留（理由与去留判据 → `ClearClip` 的 doc）。
            var noClip4 = ClearClip();
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
                //    （`bundle_menus_assets_all/MonoBehaviour/` 里那条 `"Premium Campaign daily bonus"`；
                //     两份实例 `MonoBehaviour_-818462233560502899.json` 与 `…1918117691617384191.json` **逐位一致**）。
                //    实测：不开自适应时这段 28 字的串在 fs33.3 下**渲出 ≈545px**，而框只有 **355.79px**
                //    ⇒ `AlignRight` 之后**左边冲出面板 180px**（`_tmp_view/rewards/02_战役.png` 一眼可见），
                //    而当时**没有任何断言管它的左边缘**（只断过右边缘）。
                //    ⚠️ 顺序：**先 `SetAutoFitBox` 再对齐**（对齐按当前宽度算）。
                //    🔴 **2026-10-09（A274）就地订正**：第 4 个实参原来写的是 **33.3** —— 那是把上面
                //       `_win.Text(…, "Title", 33.3f)` 那个 **`m_fontSize`** 当成了上限。原版这两个数**不等**：
                //       `m_fontSize = 33.3`（那一行**别动**）· **`m_fontSizeMax = 40`** ⇒ 上限原来矮 **6.7px**
                //       （短文案永远画小一档）。现在按原版传 `40f`；断言在 `Editor/RewardsScene.cs`
                //       （`CheckFontWindow(cpan2, "Title", 10f, 40f, …)` —— 读的是 **TMP 真字段** `fontSizeMin/Max`）。
                //    🔴 **2026-10-11（A281）就地订正（铁律 5）**：紧接着那句原来写的是
                //       「**框装不下时原版是【缩字号】，不是溢出**」—— 原话的**后半段对**，但 A281 给它换的
                //       **理由写错了**（原话：「横向那一支长在折行闸里面 ⇒ `NoWrap` 时它一次都不跑
                //       ⇒ 原版这条就是横向溢出」）。
                //    🔴🔴 **2026-10-11 再订正（批次1 · F5 · 铁律 5）：那个理由反了，结论也反了。**
                //       判据 = 本地 uGUI/TMP 源码 `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/`：
                //         · `TextMeshProUGUI.cs`：折行闸 `if (m_TextWrappingMode != NoWrap …)`（`:3565`）的
                //           `{`（`:3566`）**闭合在 `:3888`** · `else` 在 **`:3889`**，而「Text Exceeds Horizontal
                //           Bounds - Reducing Point Size」的 `#region` 在 **`:3917`** ⇒ **正落在那个 `else` 里**
                //           （实配：`:3890` 的 `{` 闭合于 `:4006`）；
                //         · 我们实际用的那份（`Label._tmp` 是 `TextMeshPro`、不是 UGUI）同构，逐字配过：
                //           闸 `:3216`/`{`:3217 **闭合 `:3539`** · `else :3540`/`{`:3541（闭合 `:3657`）·
                //           那一支的 `region :3568` —— 且 `:3540` 配的是**闸**，不是外层那条
                //           `if (isBaseGlyph && textWidth > widthOfTextArea…)`（`:3211`/`{`:3212，闭合 `:3660`）。
                //         ⇒ **`NoWrap` ⇒ 闸不成立 ⇒ 直接落进 `else` ⇒ 照样缩字号**（`else` 里先试的
                //           「Character Width Adjustments」那一支被原版 `m_charWidthMaxAdj = 0` 跳过）。
                //       而且**原版也是**：那两条 MB 里还有一个 A281 **没引**的字段 **`m_fontSizeBase = 12`**，
                //       而 `TextMeshPro.cs:2149` 每次重排都 `m_fontSize = Clamp(m_fontSizeBase, 10, 40)` ⇒
                //       从 **12** 起算、二分**涨**到「一行塞得下」为止 ⇒ **原版画的是一行贴框宽、不是溢出**。
                //       ⚠️ 正本 `资料/阶段二_锻造厂与战役页_原版规格.md:676` 那句「框本身就装不下」同批订正
                //       （它只对序列化的 `m_fontSize = 33.3` 那**一个字段**成立）。
                //       ⛔ **别去断「收敛到多少 px」**—— 那个数**没人查过**（且依赖字体资产，原版那份 ≠ 我们这份）。
                //    🔴 **A281 修的是【折行】**：`SetAutoFitBox` → `SetWrapWidth`（`Core/TmpFont.cs:208` 第一句）
                //       **无条件**把 `m_TextWrappingMode` 设成 `Normal` ⇒ 上面那个 `0` 被悄悄改成 `1`
                //       （这段 28 字的串因此折成两行、正好塞进 355.79 的框里 —— 看着「对」，
                //        而英文 `or` 之外这里还有一条：原版**根本不该折行**）。
                //       ⇒ 照 A62 那一族补一句 `SetWrapping(false)`（`MainMenuScene.cs:1365` / `AvatarTab.cs:186`
                //         · `Shell/CollectionWindow.cs:1623` 同一写法），**放在 `SetAutoFitBox` 之后、对齐之前**
                //         （A205：`SetWrapping` 内部会 `ForceRelayout` ⇒ 对齐必须在它**之后**算）。
                //       ⛔ **不许改 `Core/TmpFont.cs`**（共用件）—— 这一条只改**调用侧**。
                // 🔴 **2026-10-11（A305①）**：第 5 个实参 = 原版 `m_fontSizeBase`。
                //    判据：`/Campaign Tab/Premium Panel/Title` = `m_fontSize 33.3` · `auto[10~40]` ·
                //    **`base 12.0`**（逐站表 §二·3 #3）—— ⚠️ 同页三颗里**只有它是 12**（另两颗是 36），
                //    所以这一颗必须显式传（这正是「12 不是通则」那条订正的现场证据）。
                panTitle.SetAutoFitBox(LayoutSpace.Px(panTitleR.W), LayoutSpace.Px(panTitleR.H), 10f, 40f, 12f);
                panTitle.SetWrapping(false);        // 🆕 2026-10-11（A281）：还原本条 TMP 的 `m_TextWrappingMode = 0`
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
            RestoreClip(noClip4);
        }

        // ============================================================ 「这一页四处没有 mask」（A353）

        /// <summary>裁切三件套的一份快照（`Clip` / `ClipPad` / `ClipSoftness`）—— 见 `ClearClip` / `RestoreClip`。</summary>
        struct ClipSnap { public PxRect? clip; public Vector4 pad; public Vector2 soft; }

        /// <summary>🔴 **2026-10-12（A353）：把本窗的「裁切三件套」显式清成 `null` / 零** —— 这一页有**四处**
        /// 原版**没有 mask** 的件（见下），它们的正确值就是「没有裁切」；用完必须 `RestoreClip(snap)` 成对还原
        /// （顺序同 `BuildTrack` / `RefreshNodes` / `BuildArmyItems` 那三处：先 pad/soft、后 `Clip`）。
        ///
        /// <para>🔴🔴 **2026-10-13（A435 甲 · A198② 阶段 2）：这一对【暂时留着】，而且理由变了** ——
        /// 迁移表 A13 的处置是「**整对删掉**」（迁移后本页不再写那三个窗级字段 ⇒ 清空是空操作）。
        /// **本轮没删**，因为：① 它**不是缺陷**、只是失去了作用（本页 `BuildTrack` / `BuildArmyItems` /
        /// `RefreshNodes` 三处的设站点**已经在同一批里删干净** ⇒ 这四处的快照恒是 `(null, 0, 0)`）；
        /// ② 它是 `Editor/RewardsScene.cs` 那段 A353 夹具（四条 ★ + 两条 `CheckAt`）**唯一的被测对象**
        /// —— 那段夹具**故意**把毒值下在窗字段 `win.Clip` 上（`(2500,1200→2600,1300)` + pad
        /// `(40,40,40,40)` + soft `(10000,10000)`），删掉这一对那六条会**全红**；
        /// ③ 而那段夹具**不在本块的可碰范围**（调度台明令「W-E3 那七段一行都别碰」）。
        /// ⇒ **要删它必须先一并改写那段夹具**（那时它该断的是「这四件的父链上没有 `ViewportClip` 节点」，
        /// 因为节点态下「清窗字段」**已经保护不了这四件**了）—— 账记在
        /// `资料/普查产出_1013/WA435甲_迁移.md` §七。</para>
        ///
        /// <para>🔴 **下面这段「反证」是【迁移前】的原话，数字已过期（铁律 5 留痕）**：
        /// 它列的 `ForgeTab.cs:572/609` · `:658/674` · 本文件 `:326/342` · `:619/656` · `:741/764`
        /// **五处设站点在 2026-10-13 全部删掉了**（改走 `ViewportClip.Hang`）⇒ 「本窗上写它的只有两处文件」
        /// 现在是「**一处都没有**」。判据（那四件原版没有 mask）**仍然成立、仍然有效**。</para>
        ///
        /// <para>**判据（原版实读，2026-10-11 当场复扫 `_tmp_view/q1_rectmask2d_paths.py`）**：路径含
        /// `Campaign Tab` 的 `RectMask2D` **只有两条视口**（`Campaign Track/Viewport` · `Campaign Army
        /// Selector/Viewport`，两处各两个来源路径、值各自读出来的）⇒
        /// **`Background Image` · `Campaign Army Selector/Background` · `Campaign Header/*` ·
        /// `Premium Panel/*` 原版都没有 mask**。出处 → `资料/普查产出_1011/WA3_A326.md` §六·1 与
        /// `资料/普查产出_1012/S4_外壳共用件_开账现核.md` 的 A353 那一行。
        /// 🔴 **节点态下这四件为什么仍然安全**：它们挂在本页 `root`（= `Campaign Tab`）的**直系**下，
        /// 而两颗视口节点在 `Campaign Track` / `Campaign Army Selector` 里 —— **不在它们的父链上**
        /// ⇒ `ViewportClip.Resolve` 沿父链**找不到任何节点** ⇒ 不裁（判据 = 上面那条原版实读）。</para>
        ///
        /// <para>⚠️ **为什么必须显式清**（与 A326 **同形、修法相反**：那一处要**补框**，这一处要**清空**）：
        /// 这一页所有图形都走 `_win.Rect` / `_win.Text` / `AddHit`，而它们**恒**转发 `RenderClip`
        /// （= `Clip` 按 `ClipPad` 内缩，`Shell/WindowsManager.cs:250-265` 的 `DrawRect` 同一份）——
        /// `Clip` 一旦非空，这几件就会被**上一个设过它的人**悄悄裁掉 / 整块不建，**没有任何断言会红**。</para>
        ///
        /// <para>🔴 **今天这四件吃不到脏值 —— 反证（A353 点名要的那条，判据逐条给行号）**：
        /// ① `Clip` 的初值是 `null`（`Shell/WindowsManager.cs:147` 声明 `public PxRect? Clip;`，`PxRect?`
        /// 默认值）；
        /// ② **本窗（`RewardsWindow` 实例）上写它的只有两处文件**，且**每一处都成对还原**：
        /// `Shell/ForgeTab.cs:572/609`（阵营条，存 2 件还原 2 件）· `:658/674`（奖励轨道，存 3 件还原 3 件）；
        /// 本文件 `:326/342`（`BuildTrack`）· `:619/656`（`BuildArmyItems`）· `:741/764`（`RefreshNodes`）
        /// —— 逐段读过：**保存与还原之间没有任何 `return`**（只有循环内的 `continue`）；
        /// ③ `Shell/MissionsTab.cs` · `Shell/RewardsWindow.cs` · `Shell/MenuWindowBase.cs` ·
        /// `Shell/WindowsManager.cs`（`GameWindow` 本体）对这三个字段**一次都不写**
        /// （`grep -n "Clip\s*=" ` 四处 **0 命中**）；
        /// ⇒ 这四处是**潜伏型**缺口（同 A326：`Build()` 之外没人下毒就看不出来），不是今天的可见缺陷。
        /// ⚠️ **但「今天不漏」不等于「可以不清」** —— 只要将来有**任何一个不还原的写入方**（或某条路上
        /// 提前 `return`），这四件就会静默地被裁；清空是把「这一处没有 mask」写死在这一页自己身上。</para>
        ///
        /// <para>⚠️ **三件里今天只有 `Clip` 那一条能单独起作用**：`MenuDraw.Rect` 只在 `clip.HasValue`
        /// 时才求交 / 采软边（那两处 `if (clip.HasValue …)`）、`MenuWindowBase.Text` 只在
        /// `RenderClip.HasValue` 时才 `ClipText`、`MenuDraw.PaddedClip(null, pad)` 第一句就返 `null`
        /// ⇒ 清 `pad` / `soft` 是**把「这一处没有 mask」写全**（同 `RefreshNodes` 里那句「显式写出来的
        /// 『本来就是 0』」的纪律），⛔ **不是**今天多出来的行为。</para></summary>
        ClipSnap ClearClip()
        {
            var s = new ClipSnap { clip = _win.Clip, pad = _win.ClipPad, soft = _win.ClipSoftness };
            _win.Clip = null;
            _win.ClipPad = Vector4.zero;
            _win.ClipSoftness = Vector2.zero;
            return s;
        }

        /// <summary>与 `ClearClip` 成对（⛔ 别只调一个）。</summary>
        void RestoreClip(ClipSnap s)
        {
            _win.ClipPad = s.pad;
            _win.ClipSoftness = s.soft;
            _win.Clip = s.clip;
        }

        // ============================================================ 小工具

        /// <summary>🔴 **2026-10-04（A48 接线批）：本文件自己那份副本【删掉】，转调基类 `_win.AddHit`。**
        /// 这是 A25① / A9-A15 那条老账的最后一截（`ForgeTab` 那份 2026-10-03 就转调了）：原来这份副本
        /// **既不吃 `Clip`（视口外的点击区照样建、压在边上的也不截）也不吃 `ClipPad`**
        /// （`RectMask2D.m_Padding` —— 🔴 **2026-10-10 订正（A189）**：原写「只改射线那一面」**是错的**，
        /// A140 已证伪：**渲染那一面也读它**，见 `MenuDraw.PaddedClip`）—— 判据与出处 → `MenuWindowBase.AddHit` 的注释
        /// （UGUI 源码行号写在 `MenuDraw.ClipRect` / `MenuDraw.PaddedHitRect` 里）。
        /// ⚠️ 行为**只有变严**：视口外的条目命中区**不再建**（原版 `RectMask2D` 同时是射线过滤器）；
        /// 本页两条 `Clip` 的窗口（`RefreshNodes` 的轨道 `_vpR`、阵营条的 `_selR`）现在才真的吃到它。</summary>
        void AddHit(Transform parent, string name, PxRect r, int q, System.Action onClick,
                    ImageQuad target = null, string art = null, string hoverArt = null, string pressedArt = null)
            => _win.AddHit(parent, name, r, q, onClick, target, art, hoverArt, pressedArt);

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
