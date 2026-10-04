// RankedEventWindow.cs — 阶段二第 3 层第 6 件：**`RankedEventWindowV2`（排位）**
//   （原版 `RankedEventWindowV2 : LiveOpsEventWindow<IRankedEvent>`）
//
// ============================ 出处（唯一正本）============================
// `资料/阶段二_战斗入口_原版规格.md` **§一 + §二 C + §三**；几何**2026-09-24 从根实读复核**
// （`工具/menu_rect.py`/`menu_dump.py`）。**共用部分在 `LiveOpsEventWindow`**，这里只写排位独有的：
//   · `General Red Background`（**一个容器**，`UIAnchorAvoidSafeArea`，本身没有图）+ 它的四个子层
//     —— ⚠️ **四个背景层的 rect 与遭遇战那一套【不一样】**（多一层 `Noise`，其余三个也各偏一点）
//   · 左列 `Ranked Division Info`（`RankingDisplay`：`Rank Title` + `LeaderboardButton` + `ChangeRankedToggle`）
//
// 🔴 **窗口参数**：`type=1 Popup` · `windowsPlacement=5 Canvas` · `closeOnESC=1` · `extraScaleSmallScreen=1.0`。
// 🔴 **`TrophyIcon` 与遭遇战不同**：这一窗是 `40k_ranking_icon_trophy Plus`（§二 C 原文）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`RankedEventWindowV2` —— 排位活动窗。</summary>
    public class RankedEventWindow : LiveOpsEventWindow
    {
        // ---- `General Red Background`（容器本身没有图，四个子层才是画面）----
        const float GenL = 0f, GenT = 634.34f, GenR = 1920f, GenB = 445.66f;   // ⚠️ 原版高就是负的（−188.68）
        const float ShadeL = -1327.30f, ShadeT = -773.63f, ShadeR = 3247.30f, ShadeB = 1798.73f;
        const float RedL = -960f, RedT = 93.57f, RedR = 2880f, RedB = 986.43f;
        const float VigL = -960f, VigT = -28.23f, VigR = 2880f, VigB = 1053.32f;
        static readonly Vector4 RedBorder = new Vector4(0f, 26f, 0f, 26f);     // 21×81 · border(0,26,0,26)
        const float RedTexW = 21f, RedTexH = 81f;
        /// <summary>`Noise` 的 Tiled 格 = 贴图原始尺寸 ÷ `m_PixelsPerUnitMultiplier 1.0`。</summary>
        const float NoiseTilePx = 1024f;

        // ---- 左列 `Ranked Division Info` ----
        const float RDL = 0f, RDT = 146.93f, RDR = 638f, RDB = 959.07f;
        const float RankTitleL = 58f, RankTitleT = 161.33f, RankTitleR = 580f, RankTitleB = 216.02f;
        const float LeaderL = 140.46f, LeaderT = 965.97f, LeaderR = 497.54f, LeaderB = 1040.82f;
        const float LeaderTxL = 156.14f, LeaderTxT = 973.30f, LeaderTxR = 480.71f, LeaderTxB = 1033.48f;
        // `ChangeRankedToggle`（`ToggleDisplay`）—— **【Unranked 文字】【开关】【Ranked 文字】** 三个并排
        const float TgL = 191.45f, TgT = 868.12f, TgR = 446.55f, TgB = 921.97f;
        const float TgRankedL = 415.10f, TgRankedT = 868.12f, TgRankedR = 671.50f, TgRankedB = 920.52f;
        const float TgUnrankedL = 12.30f, TgUnrankedT = 868.12f, TgUnrankedR = 268.70f, TgUnrankedB = 920.52f;
        const float TgKnobL = 296.30f, TgKnobT = 875.04f, TgKnobR = 385.30f, TgKnobB = 915.04f;

        /// <summary>现在是「排位」还是「非排位」。**本地恒为 false（非排位）** —— 没有服务器、也没有排位赛数据；
        /// 点了**如实说明**，不静默切。</summary>
        public bool RankedMode { get; private set; }

        public static RankedEventWindow Create(WindowsManager mgr)
        {
            var go = new GameObject("RankedEventWindowV2");
            return Init(go.AddComponent<RankedEventWindow>(), mgr);
        }

        // ============================================================ 背景
        protected override void BuildBackdrop(Transform root)
        {
            // `General Red Background`（`UIAnchorAvoidSafeArea`，**没有图**；原版它是这四个层的父）
            var gen = MenuDraw.Node(root, "General Red Background", new PxRect(GenL, GenT, GenR, GenB));

            MenuDraw.Rect(gen, CardArt.Solid(), new PxRect(ShadeL, ShadeT, ShadeR, ShadeB),
                          "Menu Dark Background", QBg, new Color(0f, 0f, 0f, 0.773f));
            // 🔴 **2026-10-04（A47 接线批）订正档号 + 收口公共件**：原来用 `QHitBackdrop`(3115)，
            //   那是「内容档再往上留一档」的写法；规矩是**压暗层的命中区落在压暗层自己那一档**
            //   （`QBg` = 3104，见 `LiveOpsEventWindow.QBg` 的注释），且严格低于本窗内容命中区最低档
            //   （`QHit` = 3116）⇒ 改走 `MenuDraw.ShadeHit`。判据 → `MenuDraw.ShadeHit` ·
            //   `资料/待办判据_阶段二与联机.md` §A25·补（一）。
            MenuDraw.ShadeHit(gen, new PxRect(0f, 0f, 1920f, 1080f), QBg, QHit, () => Close(), "BackdropHit");
            MenuDraw.Nine(gen, Tex(ArtPopupRed), new PxRect(RedL, RedT, RedR, RedB),
                          RedBorder, RedTexW, RedTexH, QBg1, null, true, "Reward Background Get Reward");
            // 🆕 **2026-10-06（A94）：红底那块整幅底图吸收点击**。判据 = 原版 prefab
            //   `RankedEventWindowV2 > General Red Background > Reward Background Get Reward` 那颗 `Image` 的
            //   **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读，rect = −960,93.57→2880,986.43）
            //   —— 射线打到它自己、父链上没有点击处理器（关窗那颗 `BackgroundCloseButton` 在压暗层上）
            //   ⇒ 原版点红底那一带**什么都不做**（只有红底**之外**的上下两条窄边才是「点外面关窗」）。
            MenuDraw.Absorb(gen, "AbsorbHit", new PxRect(RedL, RedT, RedR, RedB), QBg, QHit);
            // `Noise`（`UI Dirt And Noise skratches` **Tiled** · col(0.311,0.127,0,0.718)）
            // ⚠️ `Noise` 原版是 **col(0.311,0.127,0,0.718)** —— 不染就是全白全不透明（`Tiled` 现在收 tint 了）
            MenuDraw.Tiled(gen, Tex("UI_Dirt_And_Noise_skratches"), new PxRect(RedL, RedT, RedR, RedB),
                           NoiseTilePx, QBg2, "Noise", new Color(0.311f, 0.127f, 0f, 0.718f));
            MenuDraw.Rect(gen, CardArt.Solid(), new PxRect(VigL, VigT, VigR, VigB),
                          "Menu Vignette", QBg3, new Color(0f, 0f, 0f, 0.58f));
        }

        // ============================================================ 左列
        protected override void BuildLeftColumn(Transform root)
        {
            var col = MenuDraw.Node(root, "Ranked Division Info", new PxRect(RDL, RDT, RDR, RDB));
            // 原版 hAlign = **Center** ⇒ 不调 `Align*`（原来右对齐了）
            MenuDraw.Text(col, new PxRect(RankTitleL, RankTitleT, RankTitleR, RankTitleB), "Rank",
                          Color.white, "Rank Title", 57.7f, QText);

            // `info`（原版出厂 **act=0**）⇒ 照纪律**不建**

            // `LeaderboardButton`（`UI_Button_Mulligan` + "Leaderboard" fs36）
            // ⚠️ 文字要挂在**按钮节点底下**（别直接挂 `col` —— 那样 `FindChild(按钮,"Button Text")` 找不到，
            //    原版那棵树里它也是按钮的子件）
            var lbn = MenuDraw.Node(col, "LeaderboardButton", new PxRect(LeaderL, LeaderT, LeaderR, LeaderB));
            // ⚠️ 原版 `LeaderboardButton` 的 Image **没有 preserveAspect**（`type=Simple`）⇒ 拉伸铺满
            //    （原来加了 keepAspect，底板只铺 247.5 / 357.09 宽 —— 找茬抓到的）
            var lbQ = MenuDraw.Rect(lbn, Tex(ArtMulligan), new PxRect(LeaderL, LeaderT, LeaderR, LeaderB),
                          "Bg", QArt);
            // 原版 Button Text hAlign = Center ⇒ 不调 `Align*`
            MenuDraw.Text(lbn, new PxRect(LeaderTxL, LeaderTxT, LeaderTxR, LeaderTxB), "Leaderboard",
                          Color.white, "Button Text", 36f, QText);
            // 🆕 A17：原版 `Ranked Division Info` 的 `LeaderboardButton` 是 SpriteSwap（普查 §块 5 第 24 行）
            MenuDraw.Hit(lbn, "Hit", new PxRect(LeaderL, LeaderT, LeaderR, LeaderB), QHit,
                         OpenLeaderboard, lbQ, ArtMulligan);

            // `Ranked Division Info/Content`（**段位信息块**）—— 2026-09-27 建；唯一正本 =
            // `资料/普查产出_0927/段位块_RankedDivisionInfo.md`（表 + 字段→节点对照 + 查不到的）。
            // ⚠️ 它建在 `col` 下（原版层级：`Ranked Division Info` 的直接子节点），
            //    节点名与 rect 全在 `RankedDivisionInfo` 那个文件里（**别在这儿再抄一份**）。
            RankedDivisionInfo.Build(col, Tex, QArt, QText, QArt1);

            // `ChangeRankedToggle`（排位 / 非排位）
            var tg = MenuDraw.Node(col, "ChangeRankedToggle", new PxRect(TgL, TgT, TgR, TgB));
            MenuDraw.Nine(tg, Tex("40k_menu_bt"), new PxRect(TgL, TgT, TgR, TgB),
                          MenuBtBorder, MenuBtTexW, MenuBtTexH, QBg3, new Color(1f, 1f, 1f, 0f), true, "Bg");  // col a=0 ⇒ 透明
            // 原版 hAlign：`UnrankedText` = **Right** · `RankedText` = **Left**（两行各自贴中间那个开关）
            // 🆕 **2026-10-03 补上了字距**：原版这两行 TMP `charSpacing = -4`
            //    （原来写「`Label` 没有字距接口」—— 那只是**没加**，`Label.SetCharSpacing` 现在有了）。
            //    ⚠️ 字距会改文字宽度 ⇒ 放在 `Align*` **之前**（与 `SetAutoFitBox` 那条顺序纪律同源）。
            var un = MenuDraw.Text(tg, new PxRect(TgUnrankedL, TgUnrankedT, TgUnrankedR, TgUnrankedB), "Unranked",
                                   Color.white, "UnrankedText", 45f, QText);
            if (un != null) un.SetCharSpacing(-4f);
            MenuDraw.AlignRight(un, new PxRect(TgUnrankedL, TgUnrankedT, TgUnrankedR, TgUnrankedB));
            var rk = MenuDraw.Text(tg, new PxRect(TgRankedL, TgRankedT, TgRankedR, TgRankedB), "Ranked",
                                   Color.white, "RankedText", 45f, QText);
            if (rk != null) rk.SetCharSpacing(-4f);
            MenuDraw.AlignLeft(rk, new PxRect(TgRankedL, TgRankedT, TgRankedR, TgRankedB));
            MenuDraw.Rect(tg, Tex("40_main_bt_toggle_on"),
                          new PxRect(TgKnobL, TgKnobT, TgKnobR, TgKnobB), "Image", QArt1, null, true);
            MenuDraw.Hit(tg, "Hit", new PxRect(TgL, TgT, TgR, TgB), QHit, ToggleRankedMode);
            ApplyToggleState(un, rk);
        }

        /// <summary>`40k_menu_bt`：**47×47 · border (10,10,10,10)**（`Sprite/*.json` 实测）。</summary>
        static readonly Vector4 MenuBtBorder = new Vector4(10f, 10f, 10f, 10f);
        const float MenuBtTexW = 47f, MenuBtTexH = 47f;

        // ============================================================ 排行榜（本窗那颗 `LeaderboardButton`）
        //
        // 🔴 **入口链（照原版，不是我们挑的）** —— 判据全文 → `资料/普查产出_0927/排行榜_入口与调用.md` §1：
        //    `RankedEventWindowV2.LeaderboardButtonClick` 手上有**两颗** prefab 字段：
        //    `rankingPrefab`（= `RankedSkirmishLeaderboardPopup`，0xA8）与
        //    `rankingPrefabClassic`（= `RankedClassicLeaderboardPopup Variant`，0xD8）；
        //    选哪颗由 `RankedEventWindowV2__SetDivision.c` 传进 `RankingDisplay.Initialize(…, playMode)` 的
        //    **`playMode`** 决定：`Classic(0)` ⇒ 经典榜，否则（如 `Skirmish=13`）⇒ 遭遇战榜。
        // ⚠️ 我们本地的 `playMode` 就是本窗的 `DeckGameMode`（`LiveOpsEventWindow` 的默认值 = 经典；
        //    `SkirmishEventWindow` 才覆写成遭遇）—— 与「排位窗按经典走」那条已有口径**同一处源**
        //    （`项目任务.md` §〇 第 1 条 ②），所以这里**不另造一个判据**。

        /// <summary>原版 `rankingPrefabClassic` / `rankingPrefab` 二选一的那条规则。</summary>
        public LeaderboardKind LeaderboardKindForMode
        {
            get
            {
                return DeckGameMode == (int)RuleEngine.GameMode.Classic
                     ? LeaderboardKind.Classic : LeaderboardKind.Skirmish;
            }
        }

        void OpenLeaderboard()
        {
            if (Manager == null)
            {
                // 静默失败是红线 —— 没有 `WindowsManager` 就说出来
                Debug.LogError("[Event] 点了 `LeaderboardButton` 但**没有 `WindowsManager`** ⇒ 开不了榜");
                return;
            }
            var kind = LeaderboardKindForMode;
            var win = LeaderboardWindow.Create(Manager, kind);
            Manager.OpenWindow(win);
            Debug.Log("[Event] `LeaderboardButton` ⇒ 开 `" + LeaderboardWindow.NameOf(kind) + "`"
                      + "（原版按 `playMode` 二选一：经典 ⇒ `rankingPrefabClassic`，其余 ⇒ `rankingPrefab`；"
                      + "本窗模式 = " + DeckGameModeName + "）");
        }

        void ToggleRankedMode()
        {
            // 原版 `ChangeRankedModeToggle` 切的是「打排位还是非排位」。本地**没有排位赛**（没有服务器、
            // 也没有分段数据）⇒ **不假装切了**，如实说明（用户 2026-09-24 的裁决：点了要如实说明）。
            Debug.LogWarning("[Event] `ChangeRankedToggle` 点了 —— 本地**没有排位赛**（没有服务器与分段数据），"
                             + "**如实说明、不假装切换**");
            if (Manager != null)
                Manager.ShowPopUp("排位赛需要服务器连接。\n本地版没有联机（将来做 P2P），所以这里只能看看界面。",
                                  "知道了", null);
        }

        void ApplyToggleState(Label unranked, Label ranked)
        {
            // 本地的真实状态 = 非排位（RankedMode 恒 false）⇒ 照状态把两行文字的颜色分开，
            // **不是**随便挑一个 —— 亮的那一个就是当前模式。
            var on = Color.white;
            var off = new Color(1f, 1f, 1f, 0.45f);
            if (unranked != null) unranked.SetColor(RankedMode ? off : on);
            if (ranked != null) ranked.SetColor(RankedMode ? on : off);
        }

        protected override string TrophyIconArt { get { return "40k_ranking_icon_trophy_Plus"; } }

        /// <summary>🆕 2026-10-03：`Header/Game Mode Icon` 用哪张。
        /// 原版由 `LiveopUIDrawer_GameModeIcon` 按 **`playMode`** 喂图，本地只有
        /// `40k_gamemode_icon_classic` / `_skirmish` 两张（全盘搜过，没有排位专用的那一张）。
        /// 🔴 **排位走的就是经典模式**（本窗 `DeckGameMode` 默认经典、`playMode = Classic(0)` ——
        /// 这条口径早就在，见 `LeaderboardWindow` 那段注释与 `资料/阶段二_多人界面_原版规格.md` §4·1）
        /// ⇒ 取 **classic** 那张。⚠️ **这是「按 playMode 映射」推出来的、不是本地读到的**，如实标。</summary>
        protected override string GameModeIconArt { get { return "40k_gamemode_icon_classic"; } }

        // ============================================================ 全屏「找对手」那一步（**入口是我们定的**）
        //
        // 🔴 原版**谁开 `SearchingOpponentWindow` 本地查不到**（全量反编译里没有开它的调用点，与那四扇窗的
        //    入口同一个原因）。按 §五 那条口径：**入口由我们定并如实标出来** —— 挂在**排位窗**的 `Battle!` 上
        //    （排位本来就是「等真人」那个模式）；另外三扇窗继续用窗口内的 `Searching Oponent Popup`。
        //    **这一条映射不是复刻。**
        // ⚠️ 它是 `type=0 Fullscreen`：`WindowsManager.OpenWindow` 对全屏窗会**关掉 `currentWindow`** ——
        //    而本窗是**弹窗**（`popUpWindow`），**不在** `currentWindow` 那个位上 ⇒ **本窗不会被关**，
        //    倒计时照走（`Update` 还在跑）。这条实测过（2026-09-24）。
        SearchingOpponentWindow _searchWin;

        /// <summary>排位走全屏那扇 ⇒ **不显示**窗口内的弹窗（两扇叠在一起过，实拍抓到的）。</summary>
        protected override bool ShowInlineSearchPopup { get { return false; } }

        public override void StartMatch()
        {
            base.StartMatch();
            // 🆕 2026-10-03：**联机那一支已经在 `OnNetSearchStarted()` 里开过全屏窗了**
            //    （`base.StartMatch()` 内部调它）—— 这里**别再开一次**（会开出两扇）。
            if (NetTookOver) return;
            if (!_searchSearching()) return;              // 卡组没有督军 ⇒ `base` 已经拦下并出声了
            OpenSearchWindow(false);
        }

        /// <summary>🆕 2026-10-03（A2）：排位窗走**全屏**那扇 `SearchingOpponentWindow`。
        /// ⚠️ 它是 `type=0 Fullscreen`：`OpenWindow` 对全屏窗会**关掉 `currentWindow`** ——
        ///    而本窗是**弹窗**（`popUpWindow`）、**不在** `currentWindow` 那个位上 ⇒ **本窗不会被关**。本条实测过。</summary>
        protected override void OnNetSearchStarted() { OpenSearchWindow(true); }

        /// <summary>`netWatch = true` ⇒ 这扇窗盯着联机配对结果（配到人时把敌方那格翻成 `Found`，
        /// 并且**替联机层把切场景那一口气接过来**，展示完再走）。</summary>
        void OpenSearchWindow(bool netWatch)
        {
            _searchWin = SearchingOpponentWindow.Create(Manager, DeckIndex);
            _searchWin.OnCancel = CancelSearch;
            _searchWin.NetWatch = netWatch;
            if (Manager != null) Manager.OpenWindow(_searchWin);
            if (netWatch)
                Debug.Log("[Event] 排位：这一局走**联机** ⇒ 开全屏 `SearchingOpponentWindow` 盯配对结果"
                          + "（配到人后展示「找到对手」再切战场 —— **这一态原版是死代码，时机是我们定的**）");
            else
                Debug.Log("[Event] 排位：匹配那一步开**全屏** `SearchingOpponentWindow`（**本地入口是我们定的** —— 原版查不到）");
        }

        /// <summary>`base.StartMatch` 有没有真的把匹配起起来（没起 = 卡组不合格）。</summary>
        bool _searchSearching() { return _search != null && _search.Searching; }

        protected override void OnSearchFinished()
        {
            if (_searchWin != null) { _searchWin.Close(); _searchWin = null; }
        }

        public override void CancelSearch()
        {
            // 🆕 **2026-10-03（A1）：联机那一支在 `base` 里【真拆局】** ——
            //    `LiveOpsEventWindow.CancelSearch` 会发 `match.cancel`，对面也退回大厅。
            //    ⚠️ 原来这里那条「取消的只是这扇窗、对面照样开局、『取消联机匹配』还没做」的警告**已作废**。
            base.CancelSearch();
            if (_searchWin != null)
            {
                _searchWin.Close();
                _searchWin = null;
            }
            Debug.Log("[Event] 排位：取消匹配 ⇒ 回到本窗（全屏那扇收掉）");
        }

        /// <summary>排位窗**没有** `Timer`、**没有** `Banned card in deck`（树里实测）—— 独有的只有上面那个
        /// `ChangeRankedToggle`，它已经在 `BuildLeftColumn` 里建完了 ⇒ 这里没有额外件。</summary>
        protected override void BuildExtras(Transform root)
        {
            // 同遭遇战窗：原版还有一层 `DEBUG`（`DebugAutoDisabler`），**不建**（方法体本地读不到）
            Debug.Log("[Event] `DEBUG` 那一层**没建** —— 原版挂 `DebugAutoDisabler`（方法体本地读不到），"
                      + "我们按「不建调试件」办");
        }
    }
}
