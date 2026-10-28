// ShopWindow.cs — 阶段二第 3 层第 4 件：商店（原版 `Shop Menu Variant` + 三个页签）
//
// ============================ 出处（唯一正本） ============================
// 本文件里**每一个矩形/字号/颜色/sprite 名都是直读原版资产**，工具与命令：
//   `工具/menu_rect.py bundle_menus_assets_all "<名>" …`  ·  `工具/menu_dump.py bundle_menus_assets_all "<名>" …`
//   ⚠️ 三个页签 prefab（`Card Shop Tab` / `Daily Shop Tab` / `Item Shop Tab`）的 **`m_Father = 0`**
//      —— 它们是**独立 prefab 根**、运行期才被 `ShopWindow.CreateStoreTab` 实例化进 `Tabs`，
//      所以量它们要 `--size 1752.83x1009.06 --relative` 再统一 **+(167.17, 70.94)**（= `Tabs` 的左上角）。
//      交叉验证：这样算出来的 `Packs Scroll View` 与直接量 `Shop Menu Variant` 的同一节点**逐位相同**。
//
// ---- 🔴 窗口参数（`Shop Menu Variant` 的 MB 原文，与奖励窗**不同**，别互推）----
//   `type = 0 (Fullscreen)` · `windowsPlacement = 10 (World)` · `closeOnESC = 1` ·
//   `updateNavPanel = 1` · `extraScaleSmallScreen = 1.0`。
//
// ---- 🔴 三个页签是「同一套树的三份拷贝」（🆕 2026-10-27：第 4 页 VIP 是另一个 prefab `Card Shop VIP Tab Variant`，同构 + 多一段 `VIP Status` 与非 VIP 遮罩）----
//   `Card Shop Tab` / `Daily Shop Tab` / `Item Shop Tab` **逐节点同构**（节点数 71、结构一致，
//   但每个节点的 RT/GO pid 各不相同 = 复制粘贴）；**唯一的结构差**在
//   `daily shop header/TimeCounter` 的第一个子件：
//     · `Card Shop Tab` → TMP **`Refreshes in:`**
//     · `Daily Shop Tab` → TMP `Atualiza em:`（⚠️ **葡语占位串**，别当英文抄）
//     · `Item Shop Tab` → **`Clock Icon`**（`WF_icon_clock`，`LayoutElement.preferredWidth = 26`）
//   ⇒ 我们三个页**共用一套 View**，那个子件用一个开关（`ShopData.PageSpec.TimerAsText`）。
//
// ---- 🔴 驱动链（反编译，`d:/2/tools/decomp_full/`）----
//   `ShopWindow__SetupTabs` 按**存档里的 store 列表**逐个建页签 · `__CreateStoreTab` 载 prefab 并建签 ·
//   `__GetStartingTab` 决定默认落在哪一签 · `ShopTab__Setup` 设 `GridLayoutGroup.cellSize`
//   （= 预制默认 × `contentLayoutElementsSizeMultiplierSmallScreens` **1.18**，**只在小屏时乘**）·
//   `ShopTab__SetStore` 总开关计时器 · `ShopTab__OnOpen` → `RefreshOffers` → `CreateOffers`
//   （把报价卡实例化进 **`mountPoint` = `Content`**）· `ItemShopTab__DoOfferReordering` 按 `itemOrder` 排序
//   （`0` = **整个重排跳过**，见 `Item Shop Tab No Automatic Ordering`）。
//   ⚠️ **反编译有撞名丢产物**：`ItemShopTab__.ctor.c` 里装的是 `BoosterShopTab___ctor`；
//      `ShopTab__ToFocus.c` 与 `ShopTab__OnOpen.c` 内容相同 ⇒ **这两处读不出东西，别当结论**。
//
// ---- 🔴 我们挑的（原版取不到，逐条出声）----
//   · **页签表**：原版由**服务端 store 列表**驱动，prefab 里**只序列化了一个母版键**
//     （label `Pacotes`、图标 `40K_shop_bt_boosters`）⇒ 我们的三个键**按三个页 prefab 建**，
//     **图标从那 7 张 `*_shop_bt_*` 里挑**（见 `ShopData.Pages` 的注释）。
//   · **商品数据**（有哪些报价、多少钱、拥有几张、限购几次）**全是我们编的** —— 原版在 PlayFab。
//     按用户边界②（不做真实经济）⇒ **买了不扣钱**，只记账 + 打日志。
//   · 🆕 **2026-10-03（§三第29条 A6）：`Booster Info Popup` 的入口是我们定的** ——
//     点 `Booster Pack` 格子的**商品主图**开那扇窗。原版走 `ShopOfferContainer.OnClick → OpenContainer`
//     （从 offer 取 AssetGroup 0xc 的窗口），**那一族我们没有**；`CatalogItemContainer.OnInitialize`
//     只画抽屉、不开窗 ⇒ 见 `Shell/BoosterInfoPopup.cs` 文件头（那里是判据正本）。
//   · **格里的「名字 / 类型」两行**：原版由 `Card Drawer` 把卡画出来（⚠️ 本行原括注「我们还没有那套抽屉」
//     **已作废** —— 见下面那条更正痕迹；留着它就和相邻两行打架了）
//     ⇒ 这里**我们直接画两行字**顶在那，标明是**我们加的**。
//     ⚠️ **2026-10-03 就地更正（铁律 5）**：这条原来写的是「我们还没有那套抽屉」—— **当天 A8 把那条链接上了**
//     （主图改走 `ItemDrawer.Draw(..., DrawerOverride.Shop, ...)`，见 `BuildCell`）。
//     **但这两行字仍然是我们加的**：原版那两行是 `Card Drawer` 画在**卡面**上的，
//     🔴 **2026-10-03 就地更正（铁律 5 · A86）**：本行原来接着写「而我们工程里没有 `ItemDrawerConfig` 那张
//     『类型 → 抽屉 prefab』表」—— **假的**：那张表（SO）**2026-10-04 已整张解出**
//     （`资料/普查产出_1004/ItemDrawerConfig_映射表.md`），`ItemDrawer.ItemTypeSets` 现在就住着我们这份；
//     我们缺的是**那个抽屉变体本身**（`Card Drawer` 那个变体**没建** ⇒ 抽屉那一侧画出来的是
//     **我们自建的那 4 个抽屉**，见 `ItemDrawer.cs` 文件头）。
//     ⇒ 这一条的后半句仍然成立，前半句（「还没有那套抽屉」）已作废。
//   · **`TimedOffer` / `New` 两个角标不建**：出厂 INACT，而且按原版锚点算出来落在**格外面**
//     （y −151..−101，见 `menu_rect`），运行时位置无从查证 ⇒ **不建**（纪律①）。
//   · **`Line`（出厂 INACT）/ `WebShop Button Square Variant`（出厂 INACT）/ `price-bg`（`m_Enabled=0`）
//     不建**。
//   · **滚动与 `RectMask2D`**：三个页的 `Viewport` 都带 `RectMask2D`（softness y 25）—— 纵向滚动 +
//     硬裁 + **软边**都接了（2026-10-04 补软边，见 `PacksSoft`）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>商店。原版 `ShopWindow : GameWindowWithTabs`。</summary>
    public class ShopWindow : MainMenuSubmenuWindow
    {
        // ============================================================ 页签

        /// <summary>左栏键表：三个页 + **一个母版**（`TabButtons.tabButtonPrefab`，`Initialize` 一进来就关掉）。
        /// 🔴 **这张表是我们定的**（原版由服务端 store 列表驱动）—— 见文件头。</summary>
        public static TabBtnSpec[] ButtonsNow()
        {
            // 🔴 **2026-10-18（N59）**：页签文案补 `Term`（`BuildBar` 非空即走 `Loc.T`）——
            //   三个页签的**显示名**照内容取官方 `MenuShop/ShopItemType/*`（Boosters/Gold/Cosmetics），
            //   `Label` 仍留英文原名（诊断/自检用，别改）。原版的页签名本来由**服务端 store 列表**下发
            //   （本地没有），这三个 term 是**按页内容挑的** —— 如实记，不冒充原版。
            return new[]
            {
                new TabBtnSpec(ShopData.Pages[0].Icon, ShopData.Pages[0].Label, 36f, 5f, 36f,
                               "ShopMenu_CardsButton", term: "MenuShop/ShopItemType/BoosterPacks"),
                new TabBtnSpec(ShopData.Pages[1].Icon, ShopData.Pages[1].Label, 36f, 5f, 36f,
                               "ShopMenu_DailyButton", term: "MenuShop/ShopItemType/Gold"),
                new TabBtnSpec(ShopData.Pages[2].Icon, ShopData.Pages[2].Label, 36f, 5f, 36f,
                               "ShopMenu_ItemsButton", term: "MenuShop/ShopItemType/Cosmetics"),
                // 🆕 **2026-10-27（B88_E12）第 4 颗键 = VIP 页**（原版 `VIPCardShopTab`）。
                //   图标 `40K_shop_bt_cards_vip`（原版 `bundle_liveopsicons` 里那张，名字自带 vip ⇒ **不是我们挑的**）；
                //   文案走官方词条 `MenuShop/VIPCardStore/Title`（en `VIP Card store` / zh `VIP 张牌商店`）。
                new TabBtnSpec(ShopData.Pages[3].Icon, ShopData.Pages[3].Label, 36f, 5f, 36f,
                               "ShopMenu_VipButton", term: "MenuShop/VIPCardStore/Title"),
                // 🆕 **2026-10-28（`B91_W5`）第 5 颗键 = Boosters/Crates 页**（原版 `BoosterShopTab : ShopTab`）：图标/文案照 `Pages[4]`，文案走官方词条 `MenuShop/ShopItemType/Boosters`（en `Boosters` / zh `增强包`）；实例 ID 是我们的诊断名（`BadgeWiring` 里没有它的徽记定义）。
                new TabBtnSpec(ShopData.Pages[4].Icon, ShopData.Pages[4].Label, 36f, 5f, 36f, "ShopMenu_BoostersButton", term: "MenuShop/ShopItemType/Boosters"),
                // 第 5 个 = 母版（原版 `TabButtons.tabButtonPrefab`）：**照建但关着**。
                new TabBtnSpec(ShopData.MasterKeyIcon, "Booster Packs", 25.65f, 12f, 33f, "", 47.9f),
            };
        }

        // ============================================================ 左栏 VLG（原版逐窗不同）

        /// <summary>🔴 **2026-10-23（A6 · 界面线第二批）**：左栏第一个键距**条顶边**的距离
        /// = 原版 `Shop Menu Variant/Tab Buttons` 那颗 `VerticalLayoutGroup` 的 **`m_Padding.m_Top = 20`**。
        /// <para>判据（`工具/menu_dump.py bundle_menus_assets_all "Shop Menu Variant" --depth 3 --no-sprite` 现读）：
        /// `Tab Buttons` 那行 = `spacing=-20.0 align=1 pad=0,0,20,0 ctrlW=1 ctrlH=0 expandW=1 expandH=0`，
        /// 它排出来的第一个键 `Shop Icon` = **167.2,90.9 → 332.2,270.9**（= 条顶 70.94 + 20）。</para>
        /// <para>⚠️ 改之前我们吃基类缺省 **120** ⇒ 首键落在 **190.94**（比原版低 **100 px**）。
        /// 基类缺省是**奖励窗 / 社交窗**那一档（120 / 0），⛔ 不许把这几个数塞进基类 —— 那会把那两扇一起改坏。</para></summary>
        protected override float BarPadTop { get { return 20f; } }

        /// <summary>🔴 **2026-10-23（A6）**：左栏 VLG 的 **`m_Spacing = −20`**
        /// （同一行读数 `spacing=-20.0`；键高仍是 180 ⇒ **步长 160**，相邻两键**重叠 20 px**）。
        /// 基类缺省是 **0**（奖励 / 社交 / 收藏三扇），⛔ 别搬进基类。</summary>
        protected override float BarSpacing { get { return -20f; } }

        public ShopWindow()
        {
            // 左栏的**视觉顺序** = 四个页 + 母版（母版那格是 None ⇒ 点它走 `NotifyNotBuilt`）
            visualTypes.Clear();
            visualTypes.Add(WindowTabType.ShopCards);
            visualTypes.Add(WindowTabType.ShopDaily);
            visualTypes.Add(WindowTabType.ShopItems);
            visualTypes.Add(WindowTabType.ShopVip);      // 🆕 2026-10-27（B88_E12）第 4 颗 = VIP 页（枚举成员在 `RewardsWindow.cs` 的 `WindowTabType`）
            visualTypes.AddRange(new[] { WindowTabType.ShopCrates, WindowTabType.None });   // 🆕 2026-10-28（B91_W5）第 5 颗 = Boosters/Crates 页（枚举门槛在 `RewardsWindow.cs`）；最后一格仍是母版那一格（`None`）
        }

        public static ShopWindow Create(WindowsManager mgr)
        {
            // 🔴 **2026-10-11（A218）**：窗口根是 `RectTransform` ＋ 写 `sizeDelta`。
            //    判据 = 原版同名 prefab 实读：`Shop Menu Variant` 的 `RectTransform`
            //    `anchor (0,0)-(1,1)` · `sizeDelta (0,0)` · pivot (0.5,0.5) · **绝对矩形 (0,0)-(1920,1080)**
            //    （`bundle_menus_assets_all`，2026-10-11 现读）⇒ 整屏矩形。
            //    ⚠️ 原版靠 stretch 拿父（Canvas）的尺寸 ⇒ 我们用「重合锚点 + 屏尺寸」表达同一个矩形
            //    （锚点不复刻，见 `MenuDraw.SetPxSize`）。
            //    改坏法：删掉 `SetPxSize` 那句 ⇒ `Editor/ShopScene.cs` §A218「商店窗根 = 整屏矩形」红。
            var go = new GameObject("Shop Menu Variant", typeof(RectTransform));
            MenuDraw.SetPxSize(go.transform, LayoutSpace.DesignPxW, LayoutSpace.DesignPxH);
            var win = go.AddComponent<ShopWindow>();
            win.type = WindowType.Fullscreen;                 // 实证 type=0
            win.placement = WindowsPlacement.World;           // 实证 windowsPlacement=10（**奖励窗是 5**）
            win.closeOnEsc = true;                            // 实证 closeOnESC=1
            win.extraScaleSmallScreen = 1f;                   // 实证 1.0
            // 🔴 **2026-10-17（音效线 `W_G`）**：原版 prefab `useDefaultCloseSoundIfNull = 0`（实读）
            //   ⇒ 关窗**不出声**。`GameWindow` 的出厂值是 `true` ⇒ 这一扇必须显式关。
            win.useDefaultCloseSoundIfNull = false;
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open()
        {
            Build();
            BuildPages();
            // 🆕 2026-10-25：页对象已建出来 ⇒ 原版 lazy `SetupTabs` 那一跳（只跑一次）
            EnsureTabsSetup();
            // 新实例默认落在第 1 个可见键（`__GetStartingTab` 的回落分支）。
            // 🔴 **2026-10-19（`N117` 顺手 · 与 `CollectionWindow` 同一条判据）**：这里原来**恒 `Click(0)`**
            //   ⇒ 每次 `Open()` 都回第 0 页（`Hide` 之后被 `ShowPreviousWindow` 带回来也一样）。
            //   判据（原版 `GameWindowWithTabs__Open.c:9-21`）：**当前页签**（字段 `0x12`）非空就只重开那一页；
            //   只有它是**空**（= 新建实例）才走 `GetStartingTab()`（虚表 `0x218`，回落 = `tabs` 的
            //   `FirstOrDefault` = 第一页）。而 `WindowTabBase__CloseTab.c` 只 `SetActive(false)`、
            //   `GameWindowWithTabs__Close` 也不写 `0x12` ⇒ **隐藏 → 带回来这一路原版保留当前页**。
            //   ⚠️ 为什么必须是 `Click(visualIndex)` 而不是 `ChangeTab(CurrentTab)`：左栏高亮读的是
            //   `TabButtons.CurrentVisualIndex`（`RefreshHighlights`），只调 `ChangeTab` 会让高亮停在第 0 键上。
            int startVisual = StartVisualIndex();
            if (tabButtons != null) tabButtons.Click(startVisual);
            RefreshHighlights();
        }

        /// <summary>🔴 **2026-10-19（`N117` 顺手）**：`Open()` 该落在左栏第几颗 —— **与
        /// `CollectionWindow.StartVisualIndex` 同一条规则**（文件所有权所限没能收口到 `GameWindowWithTabs`，
        /// 两处逐值相同；⚠️ 改一处必须同步另一处）。
        /// <para>原版 `GameWindowWithTabs.Open`（`decomp_full/GameWindowWithTabs__Open.c:9-21`）：
        /// 当前页签（字段 `0x12`）**非空 ⇒ 重开它**；**空 ⇒ `GetStartingTab()`**（虚表 `0x218`，
        /// 回落 = `tabs` 的 `FirstOrDefault` = 第一页）。`Hide()` / `WindowTabBase.CloseTab()` 都**不清** `0x12`
        /// ⇒ 「藏起来 → 被 `ShowPreviousWindow` 带回来」这一路**保留当前页**。</para>
        /// <para>⚠️ 本窗 `visualTypes` 第 4 项是**母版**（`None`，`TabButtons.Click` 对它走 `NotifyNotBuilt`）
        /// ⇒ 它永远不是 `CurrentTab`；`CurrentTab == None` 只在「新建实例」出现 ⇒ 回落第 0 页。</para></summary>
        int StartVisualIndex()
        {
            if (Context != null && Context.army != ArmyIcons.Neutral) { int crates = visualTypes != null ? visualTypes.IndexOf(WindowTabType.ShopCrates) : -1; if (crates >= 0) return crates; }   // 🆕 2026-10-28（B91_W5）带阵营提示 ⇒ 落 **Boosters/Crates** 页（原版走 `GameWindowWithTabsOptions.startTab`，本地无 options 资产 ⇒ 用 `army != Neutral` 顶这根信号；判据见类尾 `OpenData` 的注释）
            if (CurrentTab == WindowTabType.None) return 0;
            int keep = visualTypes != null ? visualTypes.IndexOf(CurrentTab) : -1;
            return keep >= 0 ? keep : 0;
        }

        /// <summary>自检读口（静态断言用：⛔ 不为此真建一遍整窗）—— 见 <see cref="StartVisualIndex"/>。</summary>
        public int StartVisualIndexForTest { get { return StartVisualIndex(); } }

        /// <summary>🆕 **2026-10-18（N63）**：换语言后刷新**页内**文案（`TimeCounter` 的「可用的」/时间格式）。
        /// 左栏页签文字是 `BuildBar` 建时取的词条（`TabBtnSpec.Term`），要下次开窗才换 —— 如实记账。</summary>
        public override void OnLanguageChanged()
        {
            if (tabs == null) return;
            for (int i = 0; i < tabs.Count; i++)
                if (tabs[i] is ShopTabPage p) p.RefreshLanguageTexts();
        }

        /// <summary>点了还没做的件 —— **出声**（红线：不许静默失败）。</summary>
        public override void NotifyNotBuilt(string what)
        {
            Debug.Log("[Shop] `" + what + "` 还没实现（原版是 `ShopWindow.CreateStoreTab` 按服务端 store 列表建的页签；" +
                      "本地只有 `Card/Daily/Item Shop Tab` + `Card Shop VIP Tab Variant` 四页 prefab ⇒ 见 `ShopWindow.cs` 文件头）");
        }

        // ============================================================ 建

        public void Build()
        {
            // 🔴 **2026-10-11（`A1498`）显式表态**：原版 `Shop Menu Variant` 的
            //   `tabButtons.tabButtonPrefab` = MB `764348890710493308` 的 `m_PathID`（**非 0**）⇒ **末项【是】母版**。
            var res = BuildShell(transform, ButtonsNow(), "ShopTabButton_", "Tabs", lastIsMaster: true);
            tabButtons = res.buttons;
            _btnRoot = res.roots;
            _btnHighlight = res.highlight;
            _btnRes = res;
            _btnBadge = res.badge;

            if (MissingArt.Count > 0)
                Debug.LogWarning("[Shop] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray())
                                 + " —— 导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            // ⚠️ 只画一次、不静默 —— 🆕 **2026-10-03：这一条缺口补上了**（原来这里出声「没做」）。
            //    现在 `Packs Scroll View` 的纵向滚动 + `Viewport` 的等效裁剪（硬边 + 软边）都接了
            //    （`ShopTabPage.BuildGrid` → `MenuScroll.TopAligned` + `Packs Scroll View/Viewport` 那颗 `ViewportClip`；
            //     ⚠️ 2026-10-13（A435 阶段 2 · A26）起走**节点态**，不再是 `MenuWindowBase.Clip/ClipSoftness`）。
            Debug.Log("[Shop] `Packs Scroll View` 纵向滚动 + 裁剪（硬边 + 软边 `m_Softness = (0,25)`）**已接**"
                      + "（2026-10-03 接滚动/硬裁 · 2026-10-04 补软边）");
        }

        /// <summary>自检用：某一页的 `ShopTabPage`（`Buy` / `GridScroll` 那些都在它上面）。</summary>
        public ShopTabPage PageOf(int page)
        {
            return page >= 0 && page < tabs.Count ? tabs[page] as ShopTabPage : null;
        }

        /// <summary>自检用：某一页的栅格滚动区（批处理里没有滚轮 ⇒ 直调它的 `Wheel/SetOffset/Tick`）。</summary>
        public MenuScroll GridScrollOf(int page)
        {
            var t = page >= 0 && page < tabs.Count ? tabs[page] as ShopTabPage : null;
            return t != null ? t.GridScroll : null;
        }

        Transform[] _btnRoot;
        ImageQuad[] _btnHighlight, _btnBadge;
        /// <summary>🆕 2026-10-03：高亮层要**整棵**开关（九宫格）⇒ 留一份 `BarResult`。</summary>
        MainMenuSubmenuWindow.BarResult _btnRes;

        void BuildPages()
        {
            tabs.Clear();
            for (int p = 0; p < ShopData.Pages.Length; p++)
            {
                var pg = Node(tabHolder, ShopData.Pages[p].Prefab, TabsRect);
                var t = pg.gameObject.AddComponent<ShopTabPage>();
                t.SetHost(this, pg, p);
                tabs.Add(t);
            }
            foreach (var t in tabs) t.Setup();
        }

        /// <summary>选中态：**只画选中的那一个**（原版四键出厂都亮、可见性由运行时 `TabButtons` 驱动）。</summary>
        public override void RefreshHighlights()
        {
            int sel = tabButtons != null ? tabButtons.CurrentVisualIndex : -1;
            // 🆕 2026-10-03：**整棵九宫格一起开关**（`Highlight` 现在是 `Sliced`，一棵树 9 个 quad）
            MainMenuSubmenuWindow.SetHighlight(_btnRes, sel);
        }

        public string Dump()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"Shop：页签 {CurrentTab} · 页 {tabs.Count} 个 · 左栏 {ButtonsNow().Length} 键");
            sb.Append(" · 取不到的图 ").Append(MissingArt.Count).Append(" 张");
            return sb.ToString();
        }

        // ============================================================ 🆕 2026-10-28（B91_W5）：开窗上下文 `OpenData`（原版形状）
        // 判据（`ShopWindow.cs` 类桩 + `cpp2il/out_cpp2il_dev_ilspy/ShopWindow.cs:8-18/487-518`）：
        //   原版 `public class OpenData { public CardArmy army; public bool forcedOpened; }` ·
        //   `public OpenData Context => data as OpenData;`（读的是基类 `GameWindow.data` 那个字段）。
        // 🔴 **本机唯一的生产者** = `RankedDeckSelector.OnGoToStoreButtonClick`（`out_cpp2il_dev_ilspy/RankedDeckSelector.cs:527-545`）：
        //   `var openData = new ShopWindow.OpenData { army = currentArmy };` ＋ **另一个参数**
        //   `GameWindowWithTabsOptions.startTab = TypeReference(typeof(BoosterShopTab))` ⇒ 开商店**直奔卡包页**。
        //   ⚠️ 我们的 `Create` 只有一个 `OpenData` 形参（本地没有 options 资产）⇒ 「落哪一页」由
        //   `army != Neutral` 推（见 `StartVisualIndex`），如实标成**我们的换算**，⛔ 不写成原版行为。

        /// <summary>原版 `ShopWindow.OpenData`。**形状照抄**（只有这两个字段）。
        /// <para>⚠️ `forcedOpened`：原版这**两个字段本地都没有读写点**（全量产物里只有它的声明与默认值）⇒
        /// 我们**原样携带、不发明语义**；谁把它设成 `true`，`Create` 会**出声**一次（红线：不许静默失败）。</para></summary>
        public class OpenData
        {
            /// <summary>`CardArmy` 枚举值（`0 = Neutral` = 没带阵营提示）。</summary>
            public int army;
            /// <summary>原版字段（本地未解出用途，见类注释）。</summary>
            public bool forcedOpened;

            public OpenData() { }
            public OpenData(int army) { this.army = army; }

            /// <summary>🔶 自己设计（非原版）：按**阵营名字符串**建一份上下文 · 原版形参是 `CardArmy` 枚举，而我们的 UI 层（`LiveOpsEventWindow.SelectedArmyFaction` 等）用的是 `CardDef.Faction` 字符串 ⇒ 给一个**唯一**换算口（⛔ 别让每个调用点各写一次 `ArmyIcons.Id`，两处换算迟早不一致）· B91_W5 2026-10-28。</summary>
            public static OpenData ForFaction(string faction)
            {
                int id = ArmyIcons.Id(faction);
                if (string.IsNullOrEmpty(faction) || id < 0)
                {
                    Debug.LogWarning("[Shop] `OpenData.ForFaction(\"" + faction + "\")` 认不出这个阵营 ⇒ 当 `Neutral`"
                                   + "（不落 crates 页、也不自动开任何卡包）");
                    return new OpenData();
                }
                return new OpenData(id);
            }
        }

        /// <summary>原版 `ShopWindow.Context`（`= data as OpenData`）—— 页那边（`ShopTabPage`）读它来跑
        /// `BoosterShopTab.TryOpenContainerArmy`。
        /// <para>🔴 **为什么还要留一份自己的字段**（`_openData`）：本仓开商店**只走 `WindowsManager.OpenByRef`**
        /// （`ShellScene` 的窗口表 / `LiveOpsEventWindow.OnGoToStoreButtonClick` / `MainMenuRuntime`），而它最后一句是
        /// `wm.OpenWindow(win, **null**, closeAll)` ⇒ `TryOpen(null)` → `SetupData(null)` 会把基类 `Data` **清空**
        /// （`Shell/WindowsManager.cs:940`）。原版那一跳是 `OpenWindow(shopWindow, openData, …)`（带 data），我们没有那条路
        /// ⇒ **把同一份 `OpenData` 另存一份**，`Data` 那条照旧留给 `OpenWindow(win, data)` 的调用方。两者语义相同、只取一个。</para></summary>
        public OpenData Context { get { return _openData ?? (Data as OpenData); } }

        /// <summary>`Context` 的那一份（见上一条 doc：`OpenByRef` 会把基类 `Data` 清成 null）。</summary>
        OpenData _openData;

        /// <summary>🆕 `Create(WindowsManager, OpenData)`（原版 `OpenWindow(shopWindow, openData, …)` 那一跳）。
        /// <para>与 <see cref="Create(WindowsManager)"/> 的差别：**只在建窗那一刻记下 `OpenData`**
        /// （`SetupData` 走原版那一条 = 只写 `data` 字段、一个 UI 字段都不碰；另存一份给 `Context`，理由见上）。</para>
        /// <para>⚠️ 调用方**直接用 `WindowsManager.OpenByRef("ShopWindow", m => ShopWindow.Create(m, data))` 即可** ——
        /// `OpenByRef` 随后那次 `OpenWindow(win, null)` 只会把 `Data` 清空，`_openData` 不受影响。</para></summary>
        public static ShopWindow Create(WindowsManager mgr, OpenData data)
        {
            var w = Create(mgr);
            w.SetupData(data);          // = 原版 `GameWindow.SetupData`（只写 `data`）
            w._openData = data;         // 见 `Context` 的 doc：`OpenByRef` 会把 `Data` 清掉
            if (data != null && data.forcedOpened)
                Debug.Log("[Shop] `OpenData.forcedOpened = true` —— 原版这根字段**本地无读写点**（唯一构造点只赋 `army`）"
                        + " ⇒ 我们原样携带、**不改变行为**（⛔ 不发明语义）");
            return w;
        }
    }

}
