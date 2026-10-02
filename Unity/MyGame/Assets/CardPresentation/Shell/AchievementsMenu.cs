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
// **我们自己在进度上没有任何数据**（原版读 `Achievement.CurrentValue`，那是服务器存档）⇒ 逐格按「0 进度」渲染：
//   · `title`        = `"{名} {档}/{总档}"`（原版格式串 `{0} {1}/{2}`，档从 1 起 ⇒ 我们一律第 1 档）
//   · `description`  = **`challenge` 的 `id`**（真字符串）——⚠️ **原版那格是 I2 词条 `LocalizedText`，译文在远端查不到**（§C1）
//   · `rewards`      = `"{该档奖励数} points"`（照预制体 `'2 points'` 那个形式；真词条 `Achievements/Points` 也在远端）
//   · `counter`      = `"0/{该档阈值}"`（原版格式串 `{0}/{1}`）
//   · 进度条          = 空的（我们的进度恒 0 ⇒ `Fill` 那一层**不画**）
//   · 勋章图          = `40k_Achievements_icon_medal{档}`（原版 `AchievementsManager.GetIconByTier(tier)`；
//                      ⚠️ **档位图 ↔ 档位的对应关系本地查不到**，我们按 `medal1..5` 顺序 = 档 1..5 推 —— **这是推断**）
// ⚠️ **分类那 4 行文案是我们挑的**（用枚举名，真译在远端）—— 见 `ProfileData.TypeName` 的注释。
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

        /// <summary>`Scroll`（`ScrollRect` v=1 · mode=2 Elastic；底是 UGUI 内置 `Background`、**a=0 ⇒ 看不见**）：
        /// `635.14,213.16 → 1746.98,891.69`。</summary>
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
        /// <summary>`counter`（`a=(0.2,0.2)-(0.8,0.7) p=(0,0.5)`）：fs 12 · **Center/Middle** · 折行=1。</summary>
        static readonly Vector2 CntA = new Vector2(0.2f, 0.2f), CntB = new Vector2(0.8f, 0.7f), CntP = new Vector2(0f, 0.5f);
        /// <summary>勋章图（`Image` 130×130，`a=(0,0.5) p=(0,0.5) pos=(15,0)`）——
        /// 原版由 `AchievementsManager.GetIconByTier(tier)` 喂 `40k_Achievements_icon_medal1..5`。</summary>
        static readonly Vector2 MedalA = new Vector2(0f, 0.5f), MedalP = new Vector2(0f, 0.5f);
        static readonly Vector2 MedalPos = new Vector2(15f, 0.000640869f), MedalSz = new Vector2(130f, 130f);
        const string ArtMedalPrefix = "40k_Achievements_icon_medal";
        /// <summary>⚠️ **档位图 ↔ 档位的对应关系本地查不到**（`AchievementsManager.tierIcons` 全库 0 命中，§C3）
        /// ⇒ 按 `medal1..5` 顺序 = 档 1..5 推。**这是推断，不是证据**。</summary>
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

        protected override void Build()
        {
            var bgN = Node("bg", new PxRect(BgL, BgT, BgR, BgB));
            Nine(bgN, ArtPanel, new PxRect(BgL, BgT, BgR, BgB), PanelBorder, "Image", L_Bg);

            BuildTabs();

            var scR = new PxRect(ScL, ScT, ScR, ScB);
            var scN = Node("Scroll", scR);
            // ⚠️ `Scroll` 自己的底是 UGUI 内置 `Background`、**`m_Color.a = 0`**（§增补 1）⇒ **不画**（画了也看不见）
            var vp = new PxRect(ScL, VpT, ScR, ScB);
            var vpN = Node(scN, "Viewport", vp);          // `RectMask2D`（padding/softness 全 0 ⇒ 与我们的裁切等价）

            _scroll = NewScroll(vp, HoW, 0f, true);
            _scroll.ContentX1 = HoT;                      // 内容顶 = holder 顶（**比视口顶高 15.87**）
            // 🔴 滚轮要能重画（不接 = 滚了什么都不动；102 条只看得见前 4 行）
            _scroll.OnChanged = RebuildCells;

            _holder = Node(vpN, "ContainerHolder", new PxRect(ScL, HoT, ScL + HoW, HoT));

            Clip = vp;
            BuildCells();
            Clip = null;

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
                Text(key, ProfileData.TypeName(type), lr, Color.white, "Tab Toggle Title", TogPx, L_Text,
                     autoFit: true, autoMinPx: TogAutoMin, wrap: true);
                _tabType[i] = type;
                int captured = type;
                // 🆕 A17：原版该页是 `Trophies Tab>buttons>Achievement Type Toggle (n)`（**不是 Achievements Tab**），
                // 是 Toggle、悬停换 `…_selected`（普查 §块 5 第 11 行）
                _tabHit[i] = Hit(key, "Hit", r, L_Hit, () => Select(captured), _tabBg[i], ArtTab);
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
        /// 切分类也走它（原版 `Refresh()` 就是「清空 + 逐条 Instantiate」）。</summary>
        public void RebuildCells()
        {
            if (_holder == null) return;
            for (int i = _holder.childCount - 1; i >= 0; i--) DestroyNow(_holder.GetChild(i).gameObject);
            Clip = _scroll.Viewport;
            BuildCells();
            Clip = null;
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
            int tier = 1;                                   // 我们没有进度 ⇒ 一律第 1 档（原版读 `TargetTier`）
            int total = Mathf.Max(1, a.TierCount);
            int target = (a.Thresholds != null && a.Thresholds.Length >= tier) ? a.Thresholds[tier - 1] : 0;
            int reward = (a.Quantities != null && a.Quantities.Length >= tier) ? a.Quantities[tier - 1] : 0;

            var cell = Node(_holder, "Achievement Container", r);
            Nine(cell, ArtPanel, r, PanelBorder, "Background", L_Bg);

            var tR = UguiRect.Child(r, TxA, TxB, TxP, TitlePos, TitleSz);
            Text(cell, a.Name + " " + tier + "/" + total, tR, Color.white, "title", CellPx, L_Text,
                 autoFit: true, autoMinPx: CellAutoMin, alignLeft: true, wrap: true);

            var dR = UguiRect.Child(r, TxA, TxB, TxP, DescPos, DescSz);
            Text(cell, a.Challenge, dR, Color.white, "description", CellPx, L_Text,
                 autoFit: true, autoMinPx: CellAutoMin, alignLeft: true, wrap: true);

            var rwR = UguiRect.Child(r, TxA, TxB, TxP, RewPos, RewSz);
            Text(cell, reward + " points", rwR, Color.white, "rewards", CellPx, L_Text2,
                 autoFit: true, autoMinPx: CellAutoMin, alignLeft: true, wrap: true);
            // `rewardIcon` 挂在 `rewards` 里（自己的锚点是 a=(0,.5) p=(1,.5) ⇒ 贴在 `rewards` 左缘外）
            var siR = UguiRect.Child(rwR, SealA, SealA, SealP, Vector2.zero, SealSz);
            Rect(cell, ArtSeal, siR, "rewardIcon", L_Art, null, true);

            // `Progress` > `Slider`（`MissionProgressBarDisplay`）：底 + 外框 + 数字。**没有进度 ⇒ 不画 `Fill`**
            var prR = UguiRect.Child(r, TxA, TxB, TxP, ProgPos, ProgSz);
            var prog = Node(cell, "Progress", prR);
            var slR = UguiRect.Child(prR, UguiRect.A00, UguiRect.A11, UguiRect.P51, SliderPos, SliderSz);
            var sl = Node(prog, "Slider", slR);
            var barR = UguiRect.Child(slR, BarA, BarB, BarP, Vector2.zero, Vector2.zero);
            var barN = Node(sl, "Background", barR);
            Nine(barN, ArtBarBg, barR, BarBorder, "Image", L_Bg, BarBgTint);
            Node(barN, "Fill Area", UguiRect.Child(barR, BarA, BarB, BarP, Vector2.zero, new Vector2(0f, 6.56498f)));
            var cntR = UguiRect.Child(slR, CntA, CntB, CntP, Vector2.zero, Vector2.zero);
            Text(sl, "0/" + target, cntR, Color.white, "counter", CellPx, L_Text2,
                 autoFit: true, autoMinPx: CellAutoMin, wrap: true);
            Nine(sl, ArtBarOutline, barR, BarBorder, "Outline", L_Frame, BarOutlineTint);

            // 勋章图：`GetIconByTier(tier)`（⚠️ 图↔档的对应是**推断**，见常量注释）
            var mdR = UguiRect.Child(r, MedalA, MedalA, MedalP, MedalPos, MedalSz);
            string art = ArtMedalPrefix + Mathf.Clamp(tier, 1, MedalMaxTier);
            Rect(cell, art, mdR, "Image", L_Art, null, true);

            // ⚠️ 原版格子的 `backgroundButton` 字段 **`m_PathID = 0`（原版没接）** ⇒ **我们也不加点击**（判据在 §A·4 ④ 末）
        }

        /// <summary>`Counter`：**全量成就积分之和**（判据 ③：不受筛选影响、`ToString()` 无格式）。
        /// ⚠️ 原版那和是**玩家进度**（`Achievement.CurrentPoints`，服务器）；我们**没有进度** ⇒ 显示 `0`
        /// （这是**真值**不是编的：本地确实一点进度都没有）。</summary>
        void BuildCounter()
        {
            var c = Node("Counter", new PxRect(CnL, CnT, CnR, CnB));
            Nine(c, ArtCounter, new PxRect(CnL, CnT, CnR, CnB), CounterBorder, "Image", L_Bg2, CounterTint);
            _points = Text(c, PointsText(), new PxRect(CnL, CnT, CnR, CnB), Color.white, "EverguildTextMeshPro",
                           CnPx, L_Text, autoFit: true, autoMinPx: CnAutoMin, wrap: true);
            var icR = UguiRect.Child(new PxRect(CnL, CnT, CnR, CnB), UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                     CnIcPos, CnIcSz);
            Rect(c, ArtSeal, icR, "Image", L_Art, null, true);
        }

        /// <summary>计数条那串数字（自检与将来接进度都读它）。</summary>
        public static string PointsText()
        {
            var all = ProfileData.Achievements;
            int sum = 0;
            for (int i = 0; i < all.Count; i++) sum += CurrentPointsOf(all[i]);
            return sum.ToString();
        }

        /// <summary>一条成就「已拿到的分」。**我们恒 0**（没有存档）；原版读 `Achievement.CurrentPoints`(+0x30)。
        /// 单独抽出来是为了将来接进度时**只改这一处**。</summary>
        public static int CurrentPointsOf(ProfileData.Achievement a) { return 0; }
    }
}
