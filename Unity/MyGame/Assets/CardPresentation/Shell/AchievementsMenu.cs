// AchievementsMenu.cs — 玩家档案窗第 5 页：`Trophies Tab`（键名叫 `Trophies`、**文案是 `Achievements`**、
// 原版类名就叫 `AchievementsMenu`）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/档案窗_Trophies页.md` —— §A·1 层×参数（工具逐行）· §A·2 activeSelf ·
// §A·3 图参数 · **§A·4 三个「运行时生成」的族**（分类页签 / 成就格子 / 计数条，逐个查清）·
// §B 入口调用监听 · §C 查不到的（含 §C3 **102 条成就在本地**那个大更正）。
// 表 = `python 工具/menu_dump.py bundle_menus_assets_all --rt 8482975979751504434 --depth 8 --md`
//
// ---- 🔴 这一页的四条判据（读反编译定的，不是照工具表猜的）----
// ① **分类页签运行时是 4 个，不是预制体里烘焙的 5 个**：`Awake` 走
//    `Enum.GetValues(typeof(Achievement.AchievementType))`（`Battle=1/Collection=2/Victories=4/Account=8`，
//    按值升序）⇒ 先 `DestroyAllChildren` 再逐个 `Instantiate`（§A·4 ①）。**出厂选中 `Battle`**（ctor 里写死 `filter=1`）。
// ② **成就格子运行时 = 命中筛选的条数**（`Refresh()` 先清空再重建），预制体里那 12 个只是模板位（§A·4 ②）。
// ③ **计数条 `Counter` 显示的是「全量成就积分之和」，不受筛选影响**，格式就是 `ToString()`（无千分位、无后缀）（§A·4 ③）。
// ④ **`ContainerHolder.TryAdd/Clear` 在本页是死代码**（调用点只在 `MissionsTab`）⇒ **别去实现它**（§A·4 ②末）。
//
// ---- 🔴 数据：本地有 102 条成就（**真数据**，不是我们编的）----
// `Resources/profile_cosmetics.json` 的 `achievements`（生成器 `工具/gen_profile_cosmetics.py` 读
// `bundle_staticgeneralassets_assets_all` 的 `AllAchievements` + `ACH1..102` 两个 SO）。
// **进度/积分**：原版读 `Achievement.CurrentValue` / `CurrentPoints`（服务器存档）。
//   🆕 **2026-10-22（P1 · 账户界面批次）**：改读 **`ProfileData` 的本地存档**
//   （`persistentDataPath/WarpforgeProfile/profile.json` 的 `achIds/achValues`；
//    形状/边界 → `Shell/ProfileData.cs` 末尾「本地存档」一节）。
//   没记录 ⇒ 0 ⇒ 第一档、进度条空 —— 与本地真实状态一致（我们**没有**服务端进度可导）。
//   · `title`        = `"{名} {档}/{总档}"`（原版格式串 `{0} {1}/{2}`，档从 1 起）
//   · `description`  = 官方词条 `Achievements/<Ach_N>`（现读 **102/102**；见下面那节订正）
//   · `rewards`      = 官方词条 `Achievements/Points`（`{0} Points` / `{0} 分`）—— **2026-10-22 接上**
//   · `counter`      = `"{当前值}/{该档阈值}"`（原版格式串 `{0}/{1}`）
//   · 进度条          = 按 `当前值 / 本档阈值` 画 `Fill`（`40k_campaign_bar_fill`，色 (1,0.509,0,1)）
//   · 勋章图          = `40k_Achievements_icon_medal{档}`（原版 `AchievementsManager.GetIconByTier(tier)`；
//                      ✅ **档↔图已是证据**（2026-10-28）：`defaultTierIcon[tier-1]`，5 条 PPtr 落名 medal1..5 → 见 `ArtMedalPrefix` 的 doc）
// ⚠️ **分类那 4 行文案**走官方词条 `Achievements/Types/<枚举名>`（见 `ProfileData.TypeName`）。
//
// 🔴 **2026-10-14（第二十八会话 · `A1803`）就地订正上面两条「远端查不到」**（铁律 5）：
//   官方 zh-CN 词条表已解开（`数据/游戏数据/官方本地化_en+zh-CN.json`）⇒
//     · `title` 那一格现在走 `Achievements/Title/<Ach_N>`；
//     · `description` 那一格现在走 `Achievements/<Ach_N>`；
//     · **分类那 4 行**现在走 `Achievements/Types/<枚举名>`。
//   🔴 **2026-10-22（P1）再订正（铁律 5）**：上面那两条括号里的「现读 95/102」是 10-14 的旧读数 ——
//     **现读 = 102/102（两列都是）**；`rewards` 那句也从自拟的 `"{n} points"` **换成官方词条**
//     （`Achievements/Points` = `{0} Points` / `{0} 分`，与参考图「50 分」逐字吻合）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `AchievementsMenu`（挂在 `Trophies Tab` 上）。五个字段：
    /// `categoryTogglePrefab, categoryToggles, containerPrefab, holder, pointsCounter`。</summary>
    public class AchievementsMenu : ProfilePage
    {
        public override WindowTabType Type { get { return WindowTabType.ProfileTrophies; } }
        protected override int PageIndex { get { return 4; } }
        public override PxRect PageRect
        {
            get { return new PxRect(PlayerProfileWindow.ContentL, PlayerProfileWindow.RedT,
                                    PlayerProfileWindow.ContentR, PlayerProfileWindow.AreaB); }
        }

        // ============================================================ 队列档（页内分层）
        const int L_Bg = 0;      // 面板底 / 格子底 / 条底
        const int L_Bg2 = 1;     // 第二层底（按钮底 / 条的外框）
        const int L_Art = 2;     // 勋章 / 印章图标
        const int L_Frame = 3;   // 描边
        const int L_Text = 4;    // 正文
        const int L_Text2 = 5;   // 次要文字
        const int L_Hit = 6;     // 命中区
        /// <summary>🔴 命中区两档：**后面建的格子要压住前面的**（`PointerLayer` 按队列挑赢家，大的先吃）。
        /// 分类页签恒在最上层（它们不会被格子盖住）。</summary>
        const int L_HitCell = 5;

        // ============================================================ 真值（绝对画布像素 · §A·1）
        /// <summary>`bg`：`635.16,213.16 → 1746.96,891.68` · `UI_Deck_Information_submenu_Back` 九宫 18,18,18,18。</summary>
        const float BgL = 635.16f, BgT = 213.16f, BgR = 1746.96f, BgB = 891.68f;
        /// <summary>`buttons`（`ToggleGroup` + `VerticalLayoutGroup` spacing 20 · align 0 UpperLeft · ctrlW=1 expandW=1）：
        /// `351.04,230.64 → 635.15,928.28`。每键 **284.11×100**、步进 **120**。</summary>
        const float BtL = 351.04f, BtT = 230.64f, BtR = 635.15f, BtB = 928.28f;
        const float TogW = 284.11f, TogH = 100f, TogStep = 120f;
        /// <summary>键里 `Label`（155×40，**居中**）+ `Tab Toggle Title`（145×40 ⇒ `sizeDelta=(-10,0)`,
        /// fs 35 · auto[23~35] · **Center/Middle** · **折行=1**）。</summary>
        const float TogPx = 35f, TogAutoMin = 23f;
        /// <summary>键的 `button_bg`：`40K_settings_button`（**`m_Type=0 Simple`** ⇒ 那颗 `ppuMul=0.92` **不起作用**，§A·3）
        /// · 色 (1, 0.5723677, 0, **0.7098039388656616**)。</summary>
        static readonly Color TogBgTint = new Color(1f, 0.5723676681518555f, 0f, 0.7098039388656616f);
        /// <summary>键底两张图（**复用设置窗那两张**，与档案窗左栏六键同一对）。自检按它断「换图不换色」。</summary>
        public const string ArtTab = "40K_settings_button", ArtTabOn = "40K_settings_button_hover";

        /// <summary>`Scroll`（`ScrollRect` v=1 · **`mode=2` ⇒ Clamped**；底是 UGUI 内置 `Background`、**a=0 ⇒ 看不见**）：
        /// `635.14,213.16 → 1746.98,891.69`。
        /// ⚠️ **2026-10-04 更正**：原来这里写「mode=2 Elastic」—— **映射写反了**
        /// （UGUI 枚举 `Unrestricted = 0 / Elastic = 1 / Clamped = 2`）⇒ 这一件原版是 **Clamped**，
        /// **我们的实现碰巧就是 Clamped（默认值）⇒ 行为本来就对，错的只是这条注释。**</summary>
        const float ScL = 635.14f, ScT = 213.16f, ScR = 1746.98f, ScB = 891.69f;
        /// <summary>`Viewport`（**`RectMask2D`**，不是 `Mask` —— 与 Battle Log 那页不同，§A·3 末）：
        /// `635.14,216.14 → 1746.98,891.69`。</summary>
        const float VpT = 216.14f;
        /// <summary>`ContainerHolder`（`GridLayoutGroup` + `ContentSizeFitter` 竖自适应）：
        /// 顶 `200.27`（**比视口顶还高 15.87** —— 真值）· 宽 **1111.82**。</summary>
        const float HoT = 200.27f, HoW = 1111.82f;
        /// <summary>`GridLayoutGroup` 真值：`cellSize 520×150` · `spacing (10,10)` · `padding L0 R0 **T32 B32**` ·
        /// `align UpperCenter` · `constraint Flexible` ⇒ **列数由宽度推 = 2**（§A·4 ②）。</summary>
        const float CellW = 520f, CellH = 150f, GapX = 10f, GapY = 10f, PadT = 32f, PadB = 32f;
        /// <summary>⌊(1111.82 + 10) / (520 + 10)⌋ = 2 —— 列出来是为了**自检能断它**。</summary>
        public const int Columns = 2;

        /// <summary>格子底板 = `UI_Deck_Information_submenu_Back`（九宫 18,18,18,18）。</summary>
        const string ArtPanel = "UI_Deck_Information_submenu_Back";
        static readonly Vector4 PanelBorder = new Vector4(18f, 18f, 18f, 18f);
        // ---- 格子内部（**锚点/pos/sizeDelta 全照 §A·1 的表**，用 `UguiRect.Child` 算）----
        static readonly Vector2 TxA = new Vector2(0.3f, 0.5f), TxB = new Vector2(1f, 0.5f), TxP = new Vector2(0f, 0.5f);
        static readonly Vector2 TitlePos = new Vector2(-4f, 37.691f), TitleSz = new Vector2(-5.041f, 32.691f);
        static readonly Vector2 DescPos = new Vector2(-4f, -2.867f), DescSz = new Vector2(-5.041f, 48.423f);
        static readonly Vector2 RewPos = new Vector2(246.7f, -41.807f), RewSz = new Vector2(-255.741f, 29.457f);
        static readonly Vector2 ProgPos = new Vector2(-4f, -41.807f), ProgSz = new Vector2(-143.315f, 29.457f);
        /// <summary>格子里的字：fs 12 · auto[12~35] · **Left/Midline** · 折行=1 · 白。
        /// ⚠️ 表里那几个 `字号=12.0 / 29.55` 是**编辑器烘焙值，不是渲染字号**（坑 C）⇒ 我们按 `fontSizeMin=12` 起摆，
        /// 让 TMP 自己缩（与原版同一套 autosize 设置）。</summary>
        const float CellPx = 29.55f, CellAutoMin = 12f;
        /// <summary>`rewardIcon`：`40k_Achievements_icon_seal points`（54×74 · preserveAspect）。
        /// 🔴 **传的必须是导入后的文件名**（**空格换成下划线**）—— `CardArt.MenuUi` **不做**这个转换，
        /// 写成精灵名会**静默取不到图**（自检的 `MissingArt` 那条会红 —— 2026-09-27 就是这么抓到的）。</summary>
        const string ArtSeal = "40k_Achievements_icon_seal_points";
        static readonly Vector2 SealA = new Vector2(0f, 0.5f), SealP = new Vector2(1f, 0.5f);
        static readonly Vector2 SealSz = new Vector2(28.536f, 39.065f);
        /// <summary>`Progress/Slider`（`MissionProgressBarDisplay`）：`a=(0,0)-(1,1) p=(0.5,1) pos=(0,8.2581) sz=(0,14.1818)`。</summary>
        static readonly Vector2 SliderPos = new Vector2(0f, 8.2581f), SliderSz = new Vector2(0f, 14.1818f);
        static readonly Vector2 BarA = new Vector2(0f, 0.2f), BarB = new Vector2(1f, 0.8f), BarP = UguiRect.P50c;
        /// <summary>`Background`：`40k_campaign_bar_bg`（42×18 · 九宫 20,0,20,0 · 色 (0.299,0.289,0.689,1)）。</summary>
        const string ArtBarBg = "40k_campaign_bar_bg";
        static readonly Vector4 BarBorder = new Vector4(20f, 0f, 20f, 0f);
        static readonly Color BarBgTint = new Color(0.299f, 0.289f, 0.689f, 1f);
        /// <summary>`Outline`：`40k_campaign_bar_outline`（46×22 · 九宫 20,0,20,0 · 色 (1,0.841,0,1)）。</summary>
        const string ArtBarOutline = "40k_campaign_bar_outline";
        static readonly Color BarOutlineTint = new Color(1f, 0.841f, 0f, 1f);
        /// <summary>`Fill`：`40k_campaign_bar_fill`（42×18 · 九宫 10,0,10,0 · 色 (1,0.509,0,1)）——
        /// 原版 `Slider` 的 `m_FillRect`，宽度由进度值驱动（`MissionProgressBarDisplay`）。</summary>
        const string ArtBarFill = "40k_campaign_bar_fill";
        static readonly Vector4 BarFillBorder = new Vector4(10f, 0f, 10f, 0f);
        static readonly Color BarFillTint = new Color(1f, 0.509f, 0f, 1f);
        /// <summary>`Fill/end`（进度条右端那枚箭头）：`40k_campaign_bar_end`（22×18 · Simple · 色 (1,1,1,0.698)）。</summary>
        const string ArtBarEnd = "40k_campaign_bar_end";
        static readonly Color BarEndTint = new Color(1f, 1f, 1f, 0.698f);
        /// <summary>🔴 **成就 `title` 的字色 = 橙色**（参考图逐字：标题橙、描述白）。
        /// 判据（不是眼睛量的）= 原版 prefab `Achievement Container > title` 那颗 TMP 的
        /// `m_sharedMaterial` = pid **-5772249770677823286** ⇒ `bundle_fonts_assets_all/Material/
        /// Material_-5772249770677823286.json` 的 `m_Name = **Pragati-Regular Orange**`、
        /// `_FaceColor = (0.996078, 0.596078, 0.117647, 1)`。
        /// （`description` 用的是另一份 `Pragati-Regular Atlas Material Thin Outline`、`_FaceColor` 白 ⇒ `Color.white`。）</summary>
        static readonly Color TitleColor = new Color(0.9960784912109375f, 0.5960784554481506f, 0.11764706671237946f, 1f);
        /// <summary>`Achievements/Points` 的**兜底原文**（官方表取不到时才用；⛔ 不上屏，只作兜底）。</summary>
        const string PointsTerm = "Achievements/Points";
        const string PointsFallback = "{0} Points";
        /// <summary>`counter`（`a=(0.2,0.2)-(0.8,0.7) p=(0,0.5)`）：fs 12 · **Center/Middle** · 折行=1。</summary>
        static readonly Vector2 CntA = new Vector2(0.2f, 0.2f), CntB = new Vector2(0.8f, 0.7f), CntP = new Vector2(0f, 0.5f);
        /// <summary>勋章图（`Image` 130×130，`a=(0,0.5) p=(0,0.5) pos=(15,0)`）——
        /// 原版由 `AchievementsManager.GetIconByTier(tier)` 喂 `40k_Achievements_icon_medal1..5`。</summary>
        static readonly Vector2 MedalA = new Vector2(0f, 0.5f), MedalP = new Vector2(0f, 0.5f);
        static readonly Vector2 MedalPos = new Vector2(15f, 0.000640869f), MedalSz = new Vector2(130f, 130f);
        public const string ArtMedalPrefix = "40k_Achievements_icon_medal";
        /// <summary>✅ **档图 ↔ 档 = 证据**（2026-10-28 订正旧「推断」）：`GetIconByTier` = `defaultTierIcon[tier-1]`
        /// （`.c` 体）· 实例 `Intro.unity:7141` 的 5 条 PPtr 落名 `medal1..5` ⇒ 前缀/档数只此一份，Toast 侧也读它。</summary>
        public const int MedalMaxTier = 5;

        /// <summary>`Counter`（计数条）：`1568.89,161.10 → 1703.96,202.22` ·
        /// 底 `Feedback Scoring Button`（96×60 · 九宫 38,20,38,20）· 色用**精确浮点**（§增补 1）。</summary>
        const float CnL = 1568.89f, CnT = 161.10f, CnR = 1703.96f, CnB = 202.22f;
        const string ArtCounter = "Feedback_Scoring_Button";
        static readonly Vector4 CounterBorder = new Vector4(38f, 20f, 38f, 20f);
        static readonly Color CounterTint = new Color(0.6528301239013672f, 0.06774645298719406f, 0.06774645298719406f, 1f);
        const float CnPx = 34.85f, CnAutoMin = 18f;
        /// <summary>计数条上那枚印章：`40k_Achievements_icon_seal points`（65.34×79.03，相对条中心 −82.6,−2.9）。</summary>
        static readonly Vector2 CnIcPos = new Vector2(-82.6f, -2.9f), CnIcSz = new Vector2(65.335f, 79.032f);

        MenuScroll _scroll;
        Transform _holder;
        Label _points;
        ImageQuad[] _tabBg = new ImageQuad[0];
        Transform[] _tabHit = new Transform[0];
        int[] _tabType = new int[0];
        int _filter = ProfileData.TypeBattle;     // 原版 ctor 写死 `filter = 1 (Battle)`

        /// <summary>当前选中的分类（位标志）。</summary>
        public int Filter { get { return _filter; } }
        /// <summary>这一屏建出来的格子数（自检用：102 条全建会卡）。</summary>
        public int BuiltCells { get; private set; }

        /// <summary>🆕 **2026-10-13（A465 · W-A435己）**：这一页那个**纵向**滚动区（**自检用** ——
        /// 同 `LeaderboardWindow.RowsScroll` / `BattleLogTab.RowsScroll` 那条理由：断言要能读到
        /// `ClipNode` 与 `Viewport` 两态）。⛔ 生产代码不用它。</summary>
        public MenuScroll RowsScroll { get { return _scroll; } }

        protected override void Build()
        {
            var bgN = Node("bg", new PxRect(BgL, BgT, BgR, BgB));
            Nine(bgN, ArtPanel, new PxRect(BgL, BgT, BgR, BgB), PanelBorder, "Image", L_Bg);

            BuildTabs();

            var scR = new PxRect(ScL, ScT, ScR, ScB);
            var scN = Node("Scroll", scR);
            // ⚠️ `Scroll` 自己的底是 UGUI 内置 `Background`、**`m_Color.a = 0`**（§增补 1）⇒ **不画**（画了也看不见）
            var vp = new PxRect(ScL, VpT, ScR, ScB);
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：裁切状态**长在这个视口节点上**（= 原版 `Viewport` 那个 `RectMask2D`）。
            //    参数 = 原版 `Trophies Tab/Scroll/Viewport` 实读的 `m_Padding = (0,0,0,0)` · `m_Softness = (0,0)`
            //    （硬边）—— 与迁移前 `Clip = vp; ClipSoftness = <默认 0>` 逐位同值。
            //    ⚠️ **旧的「设 `Clip` → 画 → 清 `Clip`」那一对整对删了**（见下面 `BuildCells()` 与 `RebuildCells()`）：
            //    留着它 = 形参永远非空 ⇒ `Resolve` 走第 1 支 ⇒ **节点一个像素都不生效**（静默，
            //    唯一痕迹是 `ViewportClip.NodeShadowedByParam`）。
            var vpVc = ViewportClip.Hang(scN, "Viewport", vp, Vector4.zero, Vector2Int.zero);
            var vpN = vpVc.transform;

            _scroll = NewScroll(vp, HoW, 0f, true, 1f);   // 原版 `Achievements Tab/Scroll` = 1.0（D_N29）
            // 🔴 **2026-10-13（A465 · W-A435己）**：构建循环那一行（`BuildCells` 里
            //   `if (!_scroll.Intersects(rr)) continue;`）从今天起读**同一颗节点**的状态
            //   （`MenuScroll.Intersects` 走 `ClipNode.State.RenderClip`）—— 之前它只看 `Viewport` 那个
            //   设计矩形，于是「画出来被裁掉的那一块」与「干脆不建的那一块」是**两份框**。
            //   ⚠️ 今天两值同（节点框 = `vp`、`padding` 全 0）⇒ **逐个位不变**；判据/护栏 → `MenuScroll.ClipNode` 的注释。
            _scroll.ClipNode = vpVc;
            _scroll.ContentX1 = HoT;                      // 内容顶 = holder 顶（**比视口顶高 15.87**）
            // 🔴 滚轮要能重画（不接 = 滚了什么都不动；102 条只看得见前 4 行）
            _scroll.OnChanged = RebuildCells;

            _holder = Node(vpN, "ContainerHolder", new PxRect(ScL, HoT, ScL + HoW, HoT));

            BuildCells();

            BuildCounter();
        }

        /// <summary>四个分类页签（运行时建的，判据 ①）。出厂选中 `Battle`（`this.filter = 1`）。</summary>
        void BuildTabs()
        {
            var bar = Node("buttons", new PxRect(BtL, BtT, BtR, BtB));
            _tabBg = new ImageQuad[ProfileData.TypeOrder.Length];
            _tabHit = new Transform[ProfileData.TypeOrder.Length];
            _tabType = new int[ProfileData.TypeOrder.Length];

            for (int i = 0; i < ProfileData.TypeOrder.Length; i++)
            {
                int type = ProfileData.TypeOrder[i];
                float top = BtT + TogStep * i;
                var r = new PxRect(BtL, top, BtL + TogW, top + TogH);
                var key = Node(bar, i == 0 ? "Achievement Type Toggle" : "Achievement Type Toggle (" + i + ")", r);
                _tabBg[i] = Rect(key, ArtTab, r, "button_bg", L_Bg2, TogBgTint);
                // `Label`（155×40，居中在键里）> `Tab Toggle Title`（145×40 = sizeDelta(−10,0)）
                var lr = new PxRect(r.CX - 77.5f, r.CY - 20f, r.CX + 77.5f, r.CY + 20f);
                // 🔴 **2026-10-08（A212）**：本页是 A62 子表 A 的 **A32~A37**（此前一处都没核过）。
                //    逐处现读原版 `m_TextWrappingMode` 的结论 = **全是 `1`（折行）** ⇒ 这里的 `wrap: true`
                //    **本来就对**，本批只把它**显式写出来**（`ProfilePage.Text` 从本批起 `autoFit` 不再隐含折行
                //    ⇒ 少写这一句就会**静默**退回 `0`）。判据 = `python 工具/menu_dump.py bundle_menus_assets_all
                //    "Player Profile Window" --depth 25 --md`：
                //      · 本行 `Trophies Tab/buttons/Achievement Type Toggle/Label/Tab Toggle Title` = **1**
                //        （`字号=35 auto[23~35]`）；
                //      · 格子里 `Achievement Container > title / description / rewards`（`:290/:294/:298`）
                //        与 `Progress/Slider/counter`（`:314`）= **全 1**；
                //      · 计数条 `Counter/EverguildTextMeshPro`（`:333`）同一条 dump。
                Text(key, ProfileData.TypeName(type), lr, Color.white, "Tab Toggle Title", TogPx, L_Text,
                     autoFit: true, autoMinPx: TogAutoMin, alignLeft: false, wrap: true);
                // 🔴 **2026-10-12（A323）**：`alignLeft: false` 是**显式补上的**（原来靠缺省 ⇒ 行为不变）。
                //   原版值 = **`Center/Middle`**（`Trophies Tab/buttons/Achievement Type Toggle (n)/Label/
                //   Tab Toggle Title`，`字号=35 auto[23~35] 折行=1`）—— 本次亲跑复核：
                //   `python 工具/menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 25 --md`
                //   的「对齐=」列（同一支里 6 个 `Tab Toggle Title` 全是 `Center/Middle`）。
                //   ⚠️ `ProfilePage.Text` 的 `alignLeft` 缺省是 **`false`（= 居中）** ⇒ 这一句**零行为变化**，
                //   补它只为**显式**（那个缺省值 2026-10-12 本批已删掉，见 `PlayerProfileWindow.cs`）。
                _tabType[i] = type;
                int captured = type;
                // 🆕 A17：原版该页是 `Trophies Tab>buttons>Achievement Type Toggle (n)`（**不是 Achievements Tab**），
                // 是 Toggle、悬停换 `…_selected`（普查 §块 5 第 11 行）
                _tabHit[i] = Hit(key, "Hit", r, L_Hit, () => Select(captured), _tabBg[i], ArtTab,
                                 clickCue: WindowSounds.SelectToggleCue);
                // 🆕 **2026-10-18（音效线缺口 `AUD` §三 C9）**：原版这一族是 `EverguildToggle` +
                //   **`allowClickWhenAlreadySelected = 0`** —— `Select()` 里那句 `if (_filter == type) return;`
                //   只是**我们 onClick 里**的守卫，改前**声音已经先响过了**（`WindowButton.Click` 先播音）。
                //   现在把它搬到 `WindowButton` 的守卫上（连声音一起跳过，与原版 `OnPointerClick` 第一段同效）。
                var tabWb = _tabHit[i] != null ? _tabHit[i].GetComponent<WindowButton>() : null;
                if (tabWb != null)
                {
                    tabWb.allowClickWhenAlreadySelected = false;
                    tabWb.isOn = () => _filter == captured;
                }
            }
            RefreshTabs();
        }

        /// <summary>切分类：原版 `NotifyToggleOn(sendCallback:false)` → 写 `filter` → `Refresh()`。</summary>
        public void Select(int type)
        {
            if (_filter == type) return;      // 原版 `allowClickWhenAlreadySelected = 0`（同一键再点不回调）
            _filter = type;
            RefreshTabs();
            RebuildCells();
            Debug.Log("[Profile] `Trophies` 切到「" + ProfileData.TypeName(type) + "」（"
                    + ProfileData.OfType(type).Count + " 条）");
        }

        /// <summary>选中态：**换贴图不换色**（`EverguildToggle.changeSpriteOnValueChange=1` /
        /// `colorTintOnValueChange=0` —— 与档案窗左栏六键同一条判据）。</summary>
        void RefreshTabs()
        {
            for (int i = 0; i < _tabBg.Length; i++)
                if (_tabBg[i] != null)
                {
                    var t = Art(_tabType[i] == _filter ? ArtTabOn : ArtTab);
                    _tabBg[i].SetTexture(t);
                    // 🔴 `SetTexture` 会把 `_aspect` 冲成贴图自己的比值 ⇒ 拉回「按原版矩形定的」那个
                    //（同族先例 `BattleLogPanel:532` · `AlliancesTab:189`）
                    _tabBg[i].SetAspect(TogW / TogH);
                    // A17：选中态是这里换的底图 ⇒ 同步按钮记的「常态图」（否则悬停退出会还原成未选中的图）
                    var wb = _tabHit[i] != null ? _tabHit[i].GetComponent<WindowButton>() : null;
                    if (wb != null) wb.SetNormalTex(t);
                }
        }

        /// <summary>滚轮改了偏移 ⇒ 重画（**先清再建**，回调会重入 —— 同 `ForgeTab.BuildRewardCells` 那条）。
        /// 切分类也走它（原版 `Refresh()` 就是「清空 + 逐条 Instantiate」）。
        /// 🔴 **2026-10-13（A435 阶段 2 · 丙）**：本方法里原来那一对 `Clip = _scroll.Viewport; … Clip = null;`
        /// **整对删掉** —— 裁切状态已经长在 `Build()` 建的 `Viewport` 节点上（`ViewportClip`），
        /// 重画时它**自动继续生效**（`FindAbove` 走父链取的是同一个节点、不需要重设）。</summary>
        public void RebuildCells()
        {
            if (_holder == null) return;
            for (int i = _holder.childCount - 1; i >= 0; i--) DestroyNow(_holder.GetChild(i).gameObject);
            BuildCells();
        }

        /// <summary>按当前分类铺格子。**运行时个数 = 命中筛选的条数**（判据 ②）。</summary>
        void BuildCells()
        {
            var list = ProfileData.OfType(_filter);
            int n = list.Count;
            int rows = n == 0 ? 0 : (n + Columns - 1) / Columns;
            float contentH = rows == 0 ? 0f : PadT + PadB + rows * CellH + (rows - 1) * GapY;
            _scroll.ContentX1 = HoT;
            _scroll.ContentX2 = HoT + contentH;

            BuiltCells = 0;
            // `GridLayoutGroup` align `UpperCenter` ⇒ 横向余量 (1111.82 − (2×520 + 10)) = 61.82 ⇒ 左内缩 30.91
            float inset = (HoW - (Columns * CellW + (Columns - 1) * GapX)) * 0.5f;
            for (int i = 0; i < n; i++)
            {
                int col = i % Columns, row = i / Columns;
                float x = ScL + inset + col * (CellW + GapX);
                float y = HoT + PadT + row * (CellH + GapY);
                var rr = _scroll.Shift(new PxRect(x, y, x + CellW, y + CellH));
                if (!_scroll.Intersects(rr)) continue;
                BuildCell(rr, list[i]);
                BuiltCells++;
            }
            if (n == 0)
                Debug.Log("[Profile] `Trophies` 这一分类下**一条成就都没有** —— 原版这块也**没有空态**（§C6："
                        + "`GetIconByTier` 越界直接抛，没有任何空态分支）⇒ 我们也不自造文案。");
        }

        /// <summary>一个成就格子（`AchievementContainer`）—— 七件照 §A·4 ④ 那张表逐个落。</summary>
        void BuildCell(PxRect r, ProfileData.Achievement a)
        {
            // 🆕 2026-10-22（P1）：档位/进度/积分 **从本地存档读**（原版 `Achievement.TargetTier/CurrentValue`，
            //   服务器；我们的等价物 → `ProfileData` 末尾「本地存档」）。没记录 ⇒ 0 ⇒ 第 1 档、进度条空。
            int total = Mathf.Max(1, a.TierCount);
            int tier = ProfileData.AchievementTierOf(a);
            int target = ProfileData.AchievementTargetOf(a, tier);
            int reward = ProfileData.AchievementRewardOf(a, tier);
            int value = ProfileData.AchievementValue(a.Id);

            var cell = Node(_holder, "Achievement Container", r);
            Nine(cell, ArtPanel, r, PanelBorder, "Background", L_Bg);

            var tR = UguiRect.Child(r, TxA, TxB, TxP, TitlePos, TitleSz);
            // 🔴 标题色 = 橙（判据见 `TitleColor` 的注释：原版 title 那颗 TMP 的材质是 `Pragati-Regular Orange`）。
            var tLb = Text(cell, a.Name + " " + tier + "/" + total, tR, TitleColor, "title", CellPx, L_Text,
                 autoFit: true, autoMinPx: CellAutoMin, alignLeft: true, wrap: true);
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 本文件 `本文件` 那条 summary 写的是
            //   「格子里的字：fs 12 · auto[12~35] · **Left/Midline** · 折行=1」⇒ 格子内 `title`/`description`/`rewards`
            //   **三颗同档 `Midline`**（该 summary 只核了横向那一半）。
            MenuDraw.SetVAlign(tLb, Label.VAlign.Midline, tR);
            // 🆕 **2026-10-23（P6）**：**必须在 `SetVAlign` 之后**再贴一次左（根因/判据 → `RealignLeft`）。
            //   判据（原版 prefab 现读，`Achievement Container`）：`title` = `818.06,253.24 → 1177.01,285.93`
            //   （`对齐=Left/Midline`）· 勋章槽 `Image` = `681.06,242.27 → 811.06,372.27`
            //   ⇒ **字块左缘 818.06 在勋章右缘 811.06 之右 7.0px**（原版本来就不相交）。
            RealignLeft(tLb, tR);

            var dR = UguiRect.Child(r, TxA, TxB, TxP, DescPos, DescSz);
            var dLb = Text(cell, a.Challenge, dR, Color.white, "description", CellPx, L_Text,
                 autoFit: true, autoMinPx: CellAutoMin, alignLeft: true, wrap: true);
            // 🆕 **2026-10-23（P5）**：把行内 `<sprite>` 关键词图标抬到正文那一档队列
            //   （改前图标停在 3000、被格子底 3200 盖住 ⇒ 实拍那一格只有一个字符宽的空洞）。
            //   根因 / 三条反证 / 做法 → `SocketKeywordSprites` 上面那一段。
            SocketKeywordSprites(dLb, Q + L_Text);
            MenuDraw.SetVAlign(dLb, Label.VAlign.Midline, dR);   // A712 阶段 2：格子内三颗同档 `Left/Midline`
            RealignLeft(dLb, dR);                                // 🆕 P6：同 `title`（这一颗框高 48.423，本来就没中招）

            var rwR = UguiRect.Child(r, TxA, TxB, TxP, RewPos, RewSz);
            // 🆕 2026-10-22（P1）：**换官方词条**（原来是自拟的 `"{n} points"`）。
            //   键 `Achievements/Points`、官方两列 = `{0} Points` / `{0} 分`（参考图「50 分」逐字吻合）。
            var rwLb = Text(cell, PointsText(reward), rwR, Color.white, "rewards", CellPx, L_Text2,
                 autoFit: true, autoMinPx: CellAutoMin, alignLeft: true, wrap: true);
            MenuDraw.SetVAlign(rwLb, Label.VAlign.Midline, rwR);   // A712 阶段 2：同上（`rewards` 也在那一格里）
            // 🆕 **2026-10-23（P6）**：`rewards` 与 `title` 是**同一个病**（框高 29.457 < 行盒 ⇒ 自适应缩字号
            //   ⇒ 对齐那一刻量到的宽度是空的）。实拍里「5 分」正压在印章 `rewardIcon` 上。
            //   原版 prefab 现读：`rewards` = `1068.75,334.35 → 1177.01,363.81` · `rewardIcon` = `1040.22…1068.75`
            //   ⇒ 字块左缘正好贴印章右缘。
            RealignLeft(rwLb, rwR);
            // `rewardIcon` 挂在 `rewards` 里（自己的锚点是 a=(0,.5) p=(1,.5) ⇒ 贴在 `rewards` 左缘外）
            var siR = UguiRect.Child(rwR, SealA, SealA, SealP, Vector2.zero, SealSz);
            Rect(cell, ArtSeal, siR, "rewardIcon", L_Art, null, true);

            // `Progress` > `Slider`（`MissionProgressBarDisplay`）：底 + **填充** + 外框 + 数字
            var prR = UguiRect.Child(r, TxA, TxB, TxP, ProgPos, ProgSz);
            var prog = Node(cell, "Progress", prR);
            var slR = UguiRect.Child(prR, UguiRect.A00, UguiRect.A11, UguiRect.P51, SliderPos, SliderSz);
            var sl = Node(prog, "Slider", slR);
            var barR = UguiRect.Child(slR, BarA, BarB, BarP, Vector2.zero, Vector2.zero);
            var barN = Node(sl, "Background", barR);
            Nine(barN, ArtBarBg, barR, BarBorder, "Image", L_Bg, BarBgTint);
            var faR = UguiRect.Child(barR, BarA, BarB, BarP, Vector2.zero, new Vector2(0f, 6.56498f));
            var faN = Node(barN, "Fill Area", faR);
            // 🆕 2026-10-22（P1）：**进度条真的画出来了**（原来 `Fill` 恒空 —— 那时进度恒 0）。
            //   宽度 = `Fill Area` 宽 × `AchievementProgress01`（本档区间内的比例）；
            //   右端那枚 `end` 箭头照原版 prefab（29.44×31.86，以填充右缘为中心）。
            float frac = ProfileData.AchievementProgress01(a, tier);
            if (frac > 0.0005f)
            {
                var flR = new PxRect(faR.x1, faR.y1, faR.x1 + faR.W * frac, faR.y2);
                var flN = Node(faN, "Fill", flR);
                Nine(flN, ArtBarFill, flR, BarFillBorder, "Image", L_Bg2, BarFillTint);
                var enR = new PxRect(flR.x2 - 14.718f, flR.CY - 15.929f, flR.x2 + 14.718f, flR.CY + 15.929f);
                Rect(flN, ArtBarEnd, enR, "end", L_Art, BarEndTint, true);
            }
            var cntR = UguiRect.Child(slR, CntA, CntB, CntP, Vector2.zero, Vector2.zero);
            Text(sl, value + "/" + target, cntR, Color.white, "counter", CellPx, L_Text2,
                 autoFit: true, autoMinPx: CellAutoMin, alignLeft: false, wrap: true);
            // 🔴 **2026-10-12（A323）**：`alignLeft: false` 显式补上（原来靠缺省 ⇒ 零行为变化）。
            //   原版值 = **`Center/Middle`**（`Achievement Container > Progress > Slider > counter`，
            //   `'100/200' 字号=12 auto[12~35] 折行=1`，同一份 profile dump）。
            Nine(sl, ArtBarOutline, barR, BarBorder, "Outline", L_Frame, BarOutlineTint);

            // 勋章图：`GetIconByTier(tier)`（⚠️ 图↔档的对应是**推断**，见常量注释）
            var mdR = UguiRect.Child(r, MedalA, MedalA, MedalP, MedalPos, MedalSz);
            string art = ArtMedalPrefix + Mathf.Clamp(tier, 1, MedalMaxTier);
            Rect(cell, art, mdR, "Image", L_Art, null, true);

            // ⚠️ 原版格子的 `backgroundButton` 字段 **`m_PathID = 0`（原版没接）** ⇒ **我们也不加点击**（判据在 §A·4 ④ 末）
        }

        /// <summary>🔴 **2026-10-23（P6）**：把一颗「原版 `m_HorizontalAlignment = Left`」的标签**真正**贴到框左沿 ——
        /// **重排一次，再 `AlignLeft`**（顺序不能反）。
        ///
        /// <para>**病灶**（实拍 `_tmp_view/menu/13_档案窗_成就页.png` · 2026-10-23 01:24）：格子里 `title` 与
        /// `rewards` 的字块**中心**落在**框左沿**上，于是左缘 = 框左沿 − 字宽/2 ⇒ 头几个字压到勋章绶带 / 印章上
        /// （量到的像素：`title` 左缘 **756** vs 框左沿 **818.06**，差 **62 = 字宽 124 / 2**；
        /// `rewards` 左缘 **1050** vs 框左沿 **1068.76**，差 **17.5 = 字宽 35 / 2** —— 两颗都是「中心 = 框左沿」）。
        /// 即 `Label.AlignLeftOn` 当时拿到的 `WorldW ≈ 0`（`Battle/Label.cs` 的 `RefreshBounds` 在量不到时把
        /// `_tmpW` 写成 `Max(|0|, 1e-4)`，而 `HasMeasuredWidth` 只挡 `w <= 0` ⇒ **1e-4 被当成真测量**；
        /// 同族守卫 `A70`/`A1751` 挡的是 `4.29e9` 那种**哨兵**，挡不住这一档）。</para>
        ///
        /// <para>**为什么偏偏是 `title` / `rewards` 中招、`description` 不中**（判据 = 三颗的框高，现读 prefab）：
        /// `title` 框高 **32.691** · `rewards` **29.457** · `description` **48.423**；而字号 29.55 的**行盒**
        /// ≈ **44.8**（Noto Serif CJK 的行盒/字形 ≈ **1.516**，见 `Core/TmpFont.cs` 的 `MeasureGlyph` doc）
        /// ⇒ 前两颗**装不下** ⇒ `SetAutoFitBox` 的自适应要**缩字号**（实拍缩到 ≈21.6px），
        /// 而那一版版面是**后续重排**才排出来的 ⇒ 对齐那一刻量到的还是空的。
        /// `description` 装得下（48.4 > 44.8）⇒ 一次就排定 ⇒ 一直是对的。
        /// ⚠️ **这是推导 + 像素实证**（上面的 62 / 17.5 两个差值是量出来的）；TMP 内部在哪一趟把
        /// `textBounds` 留成 0 **本笔没有逐帧验**（本笔不跑 Unity）—— 所以修法不依赖那个细节：
        /// 不管它是「没排」还是「排在后面」，**先重排、再对齐**都成立。</para>
        ///
        /// <para>**为什么必须是「重排 + 对齐」两步**：只补 `AlignLeft` 不够 —— 量到的还是空的那一版
        /// （`1e-4`），贴完照旧是「中心 = 框左沿」。`Label.ForceRelayout()` 是现成的公开口
        /// （`SetFontSize(现值)` → `ForceMeshUpdate()` + `RefreshBounds()`），它会把**自适应缩过字号**的
        /// 那一版版面真正排出来并刷进 `_tmpW` ⇒ 随后的 `AlignLeftOn` 才拿到真宽度。
        /// 同族先例 = **`A490`**（`LiveOpsEventWindow` 那颗 `Window Title`：`SetCharSpacing` 排在
        /// `MenuDraw.AlignLeft` **之后** ⇒ 停在「按旧宽定位」的位置）—— 那一处的结论也是「对齐必须排最后」。</para>
        ///
        /// <para>⛔ **别改成「硬把字挪走」**：原版 prefab 里标题本来就**不与勋章重叠**
        /// （`title` 左缘 818.06 vs 勋章槽右缘 811.06），所以这不是「原版靠层级/裁切解决」的那一档，
        /// 而是我们这边**对齐没落地**。也 ⛔ 别动 rect 常量（它们与 prefab 逐位相同）。
        /// 断言 → `Editor/MainMenuScene.cs` 成就段（`CheckLeftAlignedAtWorld` + 与勋章不相交那两条）。</para>
        ///
        /// <para>🔴 **2026-10-23（P7）就地订正（铁律 5）**：上面那段「TMP 内部在哪一趟把 `textBounds`
        /// 留成 0 本笔没有逐帧验」**已经查清**，而且原来那条「`ForceRelayout()` 会把自适应缩过字号的那一版
        /// 真正排出来」**不成立** —— 本页的标签是在**未激活**的页里建的，`ForceMeshUpdate()` 在未激活对象上
        /// 早退（`TextMeshPro.cs:2119`），所以 `ForceRelayout()` 当时**什么都没排**。
        /// **真根因**（判据 = TMP 源码现读）：`GetTextInfo` 只走**一遍** `GenerateTextMesh`，而**自适应**的
        /// 「超框 ⇒ 缩字号 ⇒ `return`」那一族 early-return 全在 Phase II 之前，`m_textInfo.characterCount`
        /// 是 Phase II（`:5005`）才赋值的 ⇒ 一遍排完 `characterCount` 仍是 **0** ⇒ `textBounds` 退化成
        /// 空集哨兵（画布 px ≈ ±2.3e11）⇒ `RefreshBounds` 早退 ⇒ `_tmpW` 停在 `1e-4`
        /// ⇒ `HasMeasuredWidth` 原来只挡 `w <= 0` ⇒ **1e-4 被当成真测量** ⇒ 中心钉在框左沿。
        /// ✅ **修在 `Battle/Label.cs`**（P7）：① 未激活时 `ForceRelayout`/`EnsureMeasured` 走
        /// `PushLayoutInactive()`（反复 `GetTextInfo` 直到自适应收敛）；② `_measured` 让「一次都没量到」
        /// 不再被当成测量；③ 量不到宽时把对齐**记成待办**，由真测量落定的那一拍补上。
        /// ⇒ 本方法的「重排 + 对齐」两步**现在第一步真的排了**（这一处代码不必再改）。</para></summary>
        static void RealignLeft(Label lb, PxRect r)
        {
            if (lb == null) return;
            lb.ForceRelayout();          // ① 把（自适应之后的）版面真正排出来 ⇒ `WorldW` 才是真宽
            MenuDraw.AlignLeft(lb, r);   // ② 再把字块左缘钉到框左沿
            // ③ 仍然量不到宽（版面没排出来）⇒ **出声**（红线：不许静默失败）。
            //    ⚠️ 只说一次：一屏 3 颗 × 每次重画都会走到这里，逐次打会把日志刷满。
            //    🔴 **2026-10-23（P7）就地订正（铁律 5）**：阈值原来是 `< 0.01f` px —— 而「一次都没量到」
            //       那一档的 `WorldW` = **1e-4 世界单位 = 0.0108 px** > 0.01 ⇒ **这条告警恰恰在它要报的
            //       那一档上永远不响**（本次实跑日志里确实一条都没有）。改成 0.1 px（≈ 0.001 世界单位）：
            //       正常文本（最短的 `rewards` 也有 35 px）离它两个数量级，不会误报。
            if (!_warnedDegenerateWidth && lb.WorldW * 108f < 0.1f)
            {
                _warnedDegenerateWidth = true;
                // ⚠️ 文案按 P7 之后的真实后果写：现在**不会**再把中心钉到框左沿 —— `AlignLeftOn` 会把这次
                //    请求记成待办（`Label._pendAlignX`），量到宽的那一拍才补；在那之前字块停在**框心**。
                Debug.LogWarning("[Profile] 成就格 `" + (lb.Text ?? "<null>") + "` 左对齐时量不到宽度"
                               + "（`WorldW` ≈ " + (lb.WorldW * 108f).ToString("F3") + "px）"
                               + " ⇒ 这次对齐**没落下**（已记成待办，等量到宽再补；在那之前字块停在框心，"
                               + "可能压在勋章/印章上）（本进程只报这一次）");
            }
        }
        static bool _warnedDegenerateWidth;

        /// <summary>`Counter`：**全量成就积分之和**（判据 ③：不受筛选影响、`ToString()` 无格式）。
        /// 🔴 **2026-10-22（P1）**：数值从**本地存档**读（`ProfileData.AchievementPointsTotal()`；
        /// 原版是 `Achievement.CurrentPoints`，服务器）。没记录 ⇒ `0`（本地真实状态，不编）。</summary>
        void BuildCounter()
        {
            var c = Node("Counter", new PxRect(CnL, CnT, CnR, CnB));
            Nine(c, ArtCounter, new PxRect(CnL, CnT, CnR, CnB), CounterBorder, "Image", L_Bg2, CounterTint);
            _points = Text(c, PointsText(), new PxRect(CnL, CnT, CnR, CnB), Color.white, "EverguildTextMeshPro",
                           CnPx, L_Text, autoFit: true, autoMinPx: CnAutoMin, alignLeft: false, wrap: true);
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版 `Counter/EverguildTextMeshPro`（`'300'`）
            //   = `Center/**Capline**`（判据 = 下面那条注释自己写着的「⚠️ 注意它**不是** `Midline` 而是 `Capline`
            //   —— 那是**竖**档，本口管不了，如实记着」⇒ 本笔就是来关掉那句「本口管不了」的）。
            MenuDraw.SetVAlign(_points, Label.VAlign.Capline, new PxRect(CnL, CnT, CnR, CnB));
            // 🔴 **2026-10-12（A323）**：`alignLeft: false` 显式补上（原来靠缺省 ⇒ 零行为变化）。
            //   原版值 = **`Center/Capline`**（`Counter/EverguildTextMeshPro`，`'300' 字号=34.85 auto[18~72]
            //   折行=1`，同一份 profile dump；⚠️ 注意它**不是** `Midline` 而是 `Capline` ——
            //   那是**竖**档，本口管不了，如实记着）。
            var icR = UguiRect.Child(new PxRect(CnL, CnT, CnR, CnB), UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                     CnIcPos, CnIcSz);
            Rect(c, ArtSeal, icR, "Image", L_Art, null, true);
        }

        // ============================================================ 🆕 2026-10-23（P5）：行内关键词图标
        //
        // 🔴 **病灶（实拍 `d:/4/_tmp_view/menu/13_档案窗_成就页.png`）**：描述里 `[[codex]]` / `[[markOfChaos]]`
        //   已经被渲染成 `<nobr><sprite name="Atlas_trait_icon_codex"><b>典籍</b></nobr>`
        //   （`ProfileData` 走 `Loc.BulkOr` → `CardIcons.RewriteMarks`），画面上却是
        //   「触发　　**典籍**」—— **一个字符宽的空洞**（词在、图不在）。
        //
        // 🔴 **根因 = 图标的渲染队列没跟上正文**（**不是**词条缺、也不是图缺）：
        //   · TMP 的行内 `<sprite>` 画在**另一个子网格**上（`TMP_SubMesh`，自带一个 `MeshRenderer`），
        //     它的材质是 `CardIcons.SpriteAsset.material` —— 一份**全工程共享**的材质，队列停在 shader 默认 **3000**；
        //   · 而 `Label.ApplyQueue` 只盖**字体材质**（本页正文 = `Q + L_Text` = **3204**）
        //     ⇒ 图标留在 3000，被本页格子底（`Q + L_Bg` = **3200**、不透明）整块盖住 ⇒ 只剩 TMP 量出来的空白。
        //   · 同族先例 = `CardView.StampTmpQueue`（卡面「图标与文字同队列」就靠它）；卡面能用，`Label` 这条路没接。
        //   · **三条反证**（都指向「不是数据问题」）：① 那次运行的日志里 **0 条** `[CardIcons]` 告警
        //     （`SpriteExists` 为真才会插 `<sprite>` 标签；图不在时它把标签整个省掉、**连空洞都没有**）；
        //     ② 空洞宽度正好等于一个字形（TMP 认出了这张图、留了位）；
        //     ③ 同页 `title` / `rewards`（不带图标）位置完全正常。
        //   · ⚠️ 判据不足、如实标着的那一半：**「为什么卡面上正常」我没有逐帧验**（本笔不跑 Unity）；
        //     从代码看是 `CardView.StampTmpQueue` 把子网格一起盖了队列 —— 与本笔修的是同一件事。
        //
        // ✅ **做法**：给这颗标签换一份**按队列克隆**的 `TMP_SpriteAsset`（`material` = 同一张贴图的克隆、
        //   `renderQueue` = 正文那一档）；**已经建出来的**子网格再当场盖一次材质
        //   （TMP 是在**建子网格那一刻**把 `spriteAsset.material` 抄给 `TMP_SubMesh` 的 ⇒ 光换 asset 不会回头改它）。
        //   ⛔ **绝不写共享材质本体** —— `Editor/CardFaceProbe` / `Editor/BattleScene` 的 A1665/A1672
        //   两条断言会当场红（写脏它 = 所有卡的文字/图标队列一起被改）。

        /// <summary>「本页某一档队列」那一份 sprite asset（首次按队列克隆，之后缓存）。
        /// `CardIcons.SpriteAsset` 取不到 ⇒ 返回 null（那边自己会出声；⛔ 我们不自造一份空 asset）。</summary>
        static readonly Dictionary<int, TMPro.TMP_SpriteAsset> _spriteAssetAtQ =
            new Dictionary<int, TMPro.TMP_SpriteAsset>();

        static TMPro.TMP_SpriteAsset SpriteAssetAtQueue(int q)
        {
            var baseSa = CardIcons.SpriteAsset;
            if (baseSa == null || baseSa.material == null) return null;
            if (baseSa.material.renderQueue == q) return baseSa;      // 共享那份本来就是这一档 ⇒ 直接用（不克隆）
            TMPro.TMP_SpriteAsset hit;
            if (_spriteAssetAtQ.TryGetValue(q, out hit) && hit != null) return hit;
            var sa = Object.Instantiate(baseSa);
            sa.name = baseSa.name + " (q" + q + ")";
            var m = new Material(baseSa.material);
            m.name = baseSa.material.name + " (q" + q + ")";
            m.renderQueue = q;
            sa.material = m;
            _spriteAssetAtQ[q] = sa;
            return sa;
        }

        /// <summary>把一颗标签的行内图标抬到它自己那一档队列（根因 / 判据 → 上面那一段）。</summary>
        static void SocketKeywordSprites(Label lb, int q)
        {
            if (lb == null) return;
            var tmp = lb.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (tmp == null) return;                  // 点阵后端没有 `<sprite>` 这回事（`StripTags` 已剥）
            var sa = SpriteAssetAtQueue(q);
            if (sa == null) return;
            tmp.spriteAsset = sa;                     // 将来重建子网格时用它
            // 已经建出来的子网格：当场换成同一份按队列克隆的材质（理由见上）
            var subs = lb.GetComponentsInChildren<TMPro.TMP_SubMesh>(true);
            for (int i = 0; i < subs.Length; i++)
            {
                if (subs[i] == null) continue;
                var mr = subs[i].GetComponent<MeshRenderer>();
                if (mr == null || mr.sharedMaterial == null) continue;
                if (mr.sharedMaterial.renderQueue == q) continue;
                mr.sharedMaterial = sa.material;
            }
        }

        /// <summary>计数条那串数字（自检与将来接进度都读它）。**全量成就积分之和**（来自本地存档）。</summary>
        public static string PointsText()
        {
            return ProfileData.AchievementPointsTotal().ToString();
        }

        /// <summary>一条成就「已拿到的分」（来自本地存档）。原版读 `Achievement.CurrentPoints`(+0x30)。
        /// 单独抽出来是为了将来接进度时**只改这一处**。</summary>
        public static int CurrentPointsOf(ProfileData.Achievement a) { return ProfileData.AchievementPointsOf(a); }

        /// <summary>格子 `rewards` 那一句 = 官方词条 `Achievements/Points`（`{0} Points` / `{0} 分`）。
        /// 🔴 判据：`数据/游戏数据/官方本地化_en+zh-CN.json` 的 `Achievements/Points`；参考图「50 分」逐字吻合。
        /// 取不到词条 / 词条里没有 `{0}` ⇒ 回英文兜底（⛔ 别把 `{0}` 原样印上屏）。</summary>
        public static string PointsText(int reward)
        {
            string fmt = Loc.T(PointsTerm);
            if (string.IsNullOrEmpty(fmt) || fmt == PointsTerm || fmt.IndexOf("{0}") < 0) fmt = PointsFallback;
            return string.Format(fmt, reward);
        }

        // ============================================================ 类尾追加（B88_E10 2026-10-27 · 账 `个人资料_成就_排行:48`）
        /// <summary>原版 `AchievementsMenu.OnOpen()`（`AchievementsMenu.cs:298`）体**就一句** `Refresh()`
        /// （🥇 `decomp_full/AchievementsMenu__OnOpen.c:5-8` = 转调 `Refresh`）⇒ **每次切到成就页重画**。
        /// 我们原来没覆写（`WindowTabBase.OnOpen` 是空实现，`TryOpenTab` = `SetActive(true); OnOpen();`）
        /// ⇒ 切回本页**不重画**，只有切分类或滚动才重建。
        /// 🔑 我们的等价物 = `RebuildCells()`（「清空 + 逐条重画」= 原版 `Refresh()` 的形状）。
        /// ⚠️ 追加在**类尾**（⛔ 不插中间：本文件账落点 `:320` 在它之前，插中间会漂）。</summary>
        public override void OnOpen() { RebuildCells(); }
    }
}
