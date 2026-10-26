// SkirmishWindowAssertE15.cs — 遭遇战窗**左列分两版**（活动期 / 非活动期）的断言（执行代理 E15，2026-10-24）
//
// ============================ 它盯的是什么（判据出处）============================
// 用户 2026-10-24 指出：「冲突 = 活动进度 / 说明 / 胜利数 / 5 档里程碑 / 收集 —— **这个可能是以前活动的时候才有的**；
// 平常的时候，**应该和经典大概一样**」。只读判据代理 S1 查实（**全文 → `资料/普查产出_1024b/S1_小冲突非活动期界面.md`**）：
//   ① 原版 `SkirmishEventWindow : LiveOpsEventWindow<IFastModeEvent>`（`dump.cs:63944`）**本身就是 LiveOps 活动窗**，
//      `LiveOp` = 一场 FastMode 活动；**没有活动就没有这扇窗**（`TryOpen/RefreshScore` 拿空实例 = 抛 `FUN_1803f47a0`）；
//   ② 它左列出厂就是 `Reward Display`（Progression / 说明 / 胜利数 / 5 档记分条 / Collect），
//      运行时**没有任何一句 `SetActive` 开关它** ⇒ 那一棵 = **活动期形态**；
//   ③ 常驻/排名版小冲突走 `RankedEventWindowV2` 的**经典式左列** `Ranked Division Info`
//      （`RankingDisplay` 638×812 @0,146.9 = 与本窗 `Reward Display` 同一个槽）——
//      `RankedEventWindowV2__LeaderboardButtonClick` 按 `playMode` 二选一（`Classic(0)` ⇒ 经典榜，否则 ⇒ 遭遇榜）
//      ⇒ **V2 确实也服务小冲突玩法**（直读）。
//   第一权威 = 反编译 `d:/2/tools/decomp_full/`；⛔ 粉丝规则书不是官方（`CLAUDE.md` 二·2）。
//
// ============================ 怎么用（宿主）============================
// `SkirmishWindowAssertE15.Run();`
//   · **不跑 Unity 批处理**、不建持久资产；只读全局状态 + 自建两台临时窗口（跑完 `DestroySafe`）。
//   · 存档指向临时文件（`DeckStore.OverridePath` → `_tmp_view/e15/`），**不碰玩家真卡组**，跑完复原。
//   · 宿主建议挂 `Editor/MainMenuScene.cs`（那里本来就有 `SkirmishEventWindow` 的现场）或 `ShellScene`。
//
// 🔴 **灭自证 / 结构互斥（这一份断言的设计核心）**：
//   A 组（**非活动期**）断「经典式那一族**在**、`Reward Display` 那一族**一个节点都没有**」；
//   B 组（**活动期**，靠给 `LiveOpsLocalCatalog.FastModeEventsOverrideForTest` 塞一条**合成** FastMode 事件走到）
//        断「`Reward Display` 那一族**在**、经典式那一族**没有**」；
//   C 组把两族名字**逐名取异或**（`在A` ≠ `在B`）⇒ 「两棵都建」或「两棵都不建」必红。
//   🧨 改坏法（逐条写在文案里）：`BuildLeftColumn` 去掉分叉（恒画 `Reward Display`）⇒ A 组红一片；
//      恒不画（或判据恒 false 且经典式也不建）⇒ B 组红；把 `Reward Display` 建了再 `SetActive(false)`
//      ⇒ A 组「一个节点都没有」红（用户要的是**不画**，不是藏起来）。
//
// 🔴 **D 组盯的是「FastMode 判据是真的」**（不是写死 false）：本地表空 ⇒「读表」与「写死 false」在
//   产品路径上**观察不到差别**（那是自证）。办法 = 同一个挑选函数 `TryPickEventIn` 上做**对照实验**：
//   D1 在 `_events`（13 份 SO）上选得出 Scoring（证明这条链是活的）；
//   D2/D3 给 FastMode 表塞一条**在期**合成事件 ⇒ 选得出；塞一条**过期**的 ⇒ 选不出；
//   D4 不给数据 ⇒ false **且 `why` 说得清是「表里没数据」**（🧨 改成裸 `return false;` ⇒ `why` 空 ⇒ 红）。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 E15：**遭遇战窗左列分两版**（活动期 ⇒ `Reward Display`；非活动期 ⇒ 经典式 `Ranked Division Info`）。
/// <para>计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</para></summary>
public static class SkirmishWindowAssertE15
{
    static readonly CheckSink _sink = new CheckSink("[E15] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Check<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void CheckTrue(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ------------------------------------------------------------ 小工具
    /// <summary>按名字递归找（含 inactive）—— 与 `MenuCheck.FindChild` 同一个实现，只是短一点。</summary>
    static Transform Find(Transform root, string name) { return MenuCheck.FindChild(root, name); }

    /// <summary>**只看直接子件**（`Timer` 两版同名、父不同 ⇒ 必须按层查，⛔ 不能用递归找）。</summary>
    static Transform DirectChild(Transform root, string name)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            if (c != null && c.name == name) return c;
        }
        return null;
    }

    static string TextOf(Transform t)
    {
        if (t == null) return null;
        var lb = t.GetComponent<Label>();
        if (lb == null) lb = t.GetComponentInChildren<Label>(true);
        return lb != null ? lb.Text : null;
    }

    /// <summary>同名节点全树计数（**A 组「一个都没有」那条靠它**）。</summary>
    static int CountNamed(Transform root, string name)
    {
        if (root == null) return 0;
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) n++;
        return n;
    }

    /// <summary>活动期那一族的节点名（`Reward Display` 那棵树里的件）。</summary>
    static readonly string[] EventFamily =
    {
        "Reward Display", "Reward Tile", "Reward Help", "Player victories",
        "Skull Victories", "Total Victories", "Scoring Bar Event Score Info",
        "Score Levels", "Score Bar Line Level 1", "Score Bar Line Level 5",
    };

    /// <summary>非活动期（经典式）那一族的节点名（`Ranked Division Info` 那棵树里的件）。
    /// ⚠️ **不含 `Button Text`** —— 基类（`Battle!` 那颗钮）与活动期的 `Collect` 都用这个名字，
    /// 两棵树都有 ⇒ 放进来会把「结构互斥」那条假红（A4 已单独断 `LeaderboardButton/Button Text` 的文案）。</summary>
    static readonly string[] ClassicFamily =
    {
        "Ranked Division Info", "Rank Title", "LeaderboardButton",
        "ChangeRankedToggle", "UnrankedText", "RankedText",
        "Content", "DivisionText", "DivisionImage", "footer", "Mission Milestones Progress",
        "RankedSealStep", "RankedSealStep (1)",
    };

    static System.DateTime Utc(int y, int mo, int d, int h = 12)
    {
        return new System.DateTime(y, mo, d, h, 0, 0, System.DateTimeKind.Utc);
    }

    /// <summary>合成一条 FastMode 事件（⛔ **不是伪造原版活动**：`EventId` 明写 `E15 合成`，只为把判据链走通）。</summary>
    static LiveOpsLocalEvent SyntheticFastMode(System.DateTime startUtc, System.DateTime endUtc)
    {
        return new LiveOpsLocalEvent
        {
            EventId = "E15_Synthetic_FastMode",
            SoName = "E15 合成（不落盘 · 只为断言判据链）",
            Kind = LiveOpsLocalKind.FastMode,
            Enabled = true,
            UploadToLive = true,
            StartMs = new System.DateTimeOffset(startUtc).ToUnixTimeMilliseconds(),
            EndMs = new System.DateTimeOffset(endUtc).ToUnixTimeMilliseconds(),
            Expirable = false,
            ExpireMs = 0,
            Labels = new LiveOpsLocalLabel[0],
            Assets = new LiveOpsLocalAsset[0],
            Components = new KeyValuePair<int, string>[0],
        };
    }

    // ============================================================ 入口
    public static void Run(WindowsManager mgr = null)
    {
        MenuCheck.Section(_sink, "E15 遭遇战窗左列 —— 活动期 / 非活动期两版（互斥）");

        string keepPath = DeckStore.OverridePath;
        string tmp = "d:/4/_tmp_view/e15/_e15_decks.json";
        var keepLast = LiveOpsEventWindow.LastOpened;
        var keepForce = SkirmishEventWindow.ForceActiveEventForTest;
        var keepFastOverride = LiveOpsLocalCatalog.FastModeEventsOverrideForTest;
        SkirmishEventWindow skOff = null, skOn = null;

        try
        {
            System.IO.Directory.CreateDirectory("d:/4/_tmp_view/e15");
            DeckStore.OverridePath = tmp;
            try { System.IO.File.Delete(tmp); } catch { }
            CollectionData.ResetForTest();

            // 判据起点：**不给任何数据 / 不注入** ⇒ 必须是「非活动期」。
            SkirmishEventWindow.ResetEventProbeForTest();
            LiveOpsLocalCatalog.FastModeEventsOverrideForTest = null;
            CheckTrue(!SkirmishEventWindow.HasActiveSkirmishEvent,
                      "（前提·A）`HasActiveSkirmishEvent = false` —— 本机没有 FastMode 活动（表空）；"
                    + "🧨 注入点没复位会把这一条连带后面整组弄红");

            // ========================================================== A：非活动期 ⇒ 经典式左列
            skOff = SkirmishEventWindow.Create(mgr);
            if (mgr != null) mgr.OpenWindow(skOff); else skOff.Open();   // ⚠️  不调 base ⇒ 状态要由管理器置 Open
            CheckTrue(skOff != null && skOff.CurrentState == WindowState.Open, "（前提·A）遭遇战窗开起来了");

            var colOff = DirectChild(skOff.transform, "Ranked Division Info");
            CheckTrue(colOff != null,
                      "★ A1 左列 = **`Ranked Division Info`**（经典式；用户口径「平常应该和经典大概一样」）"
                    + "｜🧨 去掉 `BuildLeftColumn` 的分叉（恒画 `Reward Display`）⇒ 红");
            CheckTrue(DirectChild(skOff.transform, "Reward Display") == null,
                      "★ A2 左列**不是** `Reward Display`（活动期那一棵）");
            Check(TextOf(Find(colOff, "Rank Title")), Loc.T("PlayerProfile/Rank"),
                  "★ A3 `Rank Title` = 官方词条 `PlayerProfile/Rank`（en `Rank` / zh `排名`；"
                + "⛔ 不从实现常量取期望值）");
            var lbn = Find(colOff, "LeaderboardButton");
            Check(TextOf(lbn != null ? DirectChild(lbn, "Button Text") : null),
                  Loc.T("MainMenu/RankedWindow/Leaderboard"),
                  "★ A4 `LeaderboardButton/Button Text` = 官方词条 `MainMenu/RankedWindow/Leaderboard`（zh `排行榜`）");
            CheckTrue(lbn != null && DirectChild(lbn, "Hit") != null,
                      "★ A4b `LeaderboardButton` 有命中区（`Hit`）—— 这颗钮在经典式左列里是**能点**的"
                    + "（原版 `Ranked Division Info` 的 `LeaderboardButton` 是 SpriteSwap 钮）");
            CheckTrue(Find(colOff, "ChangeRankedToggle") != null,
                      "★ A5 `ChangeRankedToggle`（未排名 ⟷ 排名）在 —— 经典式左列的那颗开关");
            Check(TextOf(Find(colOff, "UnrankedText")), Loc.T("MainMenu/RankedWindow/Unranked"),
                  "★ A5b `UnrankedText` = 官方词条 `MainMenu/RankedWindow/Unranked`");
            Check(TextOf(Find(colOff, "RankedText")), Loc.T("MainMenu/RankedWindow/Ranked"),
                  "★ A5c `RankedText` = 官方词条 `MainMenu/RankedWindow/Ranked`");

            // 段位信息块（`RankedDivisionInfo` 那一棵）：结构齐、**数值留空**
            CheckTrue(Find(colOff, "Content") != null, "★ A6 段位块 `Content` 在（`RankedDivisionInfo.Build` 的产出）");
            CheckTrue(Find(colOff, "footer") != null && Find(colOff, "Mission Milestones Progress") != null,
                      "★ A6b `footer` + `Mission Milestones Progress`（里程碑那一行）在");
            CheckTrue(Find(colOff, "RankedSealStep") != null && Find(colOff, "RankedSealStep (1)") != null,
                      "★ A6c `RankedSealStep` **两颗**都在（照 prefab 那两颗样板；`Fill` 那一态不画 = 进度未知）");
            // 🔴 **2026-10-24 就地订正（铁律 5 · `E19`）**：原来断「段位名**留空**（服务器数据、不编数字）」——
            //   现在按用户口径**画演示段位**（`RankedDivisionInfo.DemoDivisionNumber = 7`，**我们挑的**），
            //   段位名走官方词条 `Ranked/Division/7`（「传奇」）。期望值取**官方表**，⛔ 不用产品侧 helper。
            {
                string divTerm = Loc.OfficialCurrent("Ranked/Division/" + RankedDivisionInfo.DemoDivisionNumber);
                CheckTrue(!string.IsNullOrEmpty(divTerm) && TextOf(Find(colOff, "DivisionText")) == divTerm,
                          "★ A6d `DivisionText`（段位名）= 官方词条 `Ranked/Division/"
                        + RankedDivisionInfo.DemoDivisionNumber + "`（演示段位**我们挑的**；原版真实段位在服务器）"
                        + "（实得「" + (TextOf(Find(colOff, "DivisionText")) ?? "<无>") + "」）"
                        + "｜🧨 退回留空 ⇒ 红");
            }

            // 「结束于：」那一行（`Content/Timer`）
            var cTimer = Find(colOff, "Timer");
            CheckTrue(cTimer != null && cTimer.parent != null && cTimer.parent.name == "Content",
                      "★ A7 `Content/Timer`（= `RankingDisplay.timerDisplay`）在，父件是 `Content`"
                    + "｜🧨 挂到窗根 / 不建 ⇒ 红");
            Check(TextOf(DirectChild(cTimer, "Timer")),
                  TimerDisplay.Compose(Loc.T("Event_Description/General/EndsInPlusOrangeTime"), "--"),
                  "★ A7b 倒计时文案 = 官方词条 `EndsInPlusOrangeTime` 填 `--`（本地无截止时刻）");
            CheckTrue(DirectChild(cTimer, "Timer Icon") != null
                      && DirectChild(cTimer, "Timer Icon").GetComponentInChildren<ImageQuad>(true) != null,
                      "★ A7c 倒计时那颗 `Timer Icon`（`WF_icon_clock`）画出来了");
            CheckTrue(DirectChild(skOff.transform, "Timer") == null,
                      "★ A8 **活动倒计时那颗 `Timer` 不在**（非活动期没有活动可倒计时；"
                    + "否则屏幕上会同时出现两行「结束于：」）"
                    + "｜🧨 `BuildExtras` 不加条件、恒调 `BuildTimer` ⇒ 红");

            // 🔴 用户点名的五样：一个节点都不许在
            foreach (var nm in EventFamily)
                Check(CountNamed(skOff.transform, nm), 0,
                      $"★ A9 非活动期 ⇒ **`{nm}` 一个节点都没有**（用户点名的那五样：活动进度/说明/胜利数/5 档里程碑/收集）"
                    + "｜🧨 把 `Reward Display` 建好再 `SetActive(false)` ⇒ 本条照样红（要的是**不画**，不是藏起来）");

            // 🔴 **2026-10-26（调度台裁定 · `X4`）**：A10 原来读**容器** `Banned card in deck` 的 `activeSelf`，理由写
            //   「主动关着 = 本地没有禁用卡表」—— **两句现在都不成立了**：
            //   ① 那句理由 **2026-10-25 起过期**（`E` 已按原版把这一格接成**动态**那一行：卡组校验不通过才显示）；
            //   ② 形状也订正了 —— 原版 `SkirmishEventWindow.OnDeckSelected` 只
            //      `SetActive(bannedCardInDeckText.get_gameObject())` = **那颗 TMP 自己**（`…/Text` 子件），
            //      容器 prefab 出厂 `act = 1`、**原版从不动它** ⇒ 容器现在是**恒开**的。
            //   ⇒ 本条改读 `Text` 子件；判定值与改前**完全等价**（该时刻当前卡组合法 ⇒ `Text` 也关着）。
            var a10Ban = DirectChild(skOff.transform, "Banned card in deck");
            CheckTrue(a10Ban != null,
                      "（前提 · A10）`Banned card in deck` **容器在**（没被非活动期这条分叉误删）");
            var a10BanText = DirectChild(a10Ban, "Text");
            CheckTrue(a10BanText != null,
                      "（前提 · A10）`Banned card in deck/Text`（那颗 TMP 自己）在 —— 显隐读的是它，⛔ 不是容器");
            CheckTrue(a10BanText != null && !a10BanText.gameObject.activeSelf,
                      "★ A10 `Banned card in deck/Text` **建着、关着**（容器恒开，只有这颗 TMP 参与显隐 ——"
                    + "判据 = 原版 `SkirmishEventWindow.OnDeckSelected` 只 SetActive 这颗 TMP、容器出厂 `act=1`）");
            Check(skOff.MissingArt.Count, 0,
                  "★ A11 遭遇战窗（非活动期）**图一张都不缺**（`Tex()` 会记账；取不到的层 `MenuDraw` 静默不画）");

            // ========================================================== B：活动期 ⇒ `Reward Display`
            // 🔴 走**生产判据链**（不给窗灌 `ForceActiveEventForTest`，只给**表**塞一条在期合成事件）
            //    ⇒ 「读表 → 判据 → 分叉」这条链**真的通**，⛔ 不是把分支灌绿。
            var now = LiveOpsLocalCatalog.NowUtc;
            LiveOpsLocalCatalog.FastModeEventsOverrideForTest =
                new[] { SyntheticFastMode(now.AddHours(-1), now.AddHours(1)) };
            CheckTrue(SkirmishEventWindow.HasActiveSkirmishEvent,
                      "★ B0 （前提）表里有一条**在期** FastMode 事件 ⇒ `HasActiveSkirmishEvent = true`"
                    + "｜🧨 把判据写成 `return false;` ⇒ 红");

            skOn = SkirmishEventWindow.Create(mgr);
            if (mgr != null) mgr.OpenWindow(skOn); else skOn.Open();

            var rewardOn = DirectChild(skOn.transform, "Reward Display");
            CheckTrue(rewardOn != null,
                      "★ B1 活动期 ⇒ 左列 = **`Reward Display`**（原版 prefab 那一棵，行为保持现状）");
            Check(TextOf(Find(rewardOn, "Reward Tile")), Loc.T("Ranked/Skirmish/ProgressionTitle"),
                  "★ B1b `Reward Tile` = 官方词条 `Ranked/Skirmish/ProgressionTitle`");
            CheckTrue(Find(rewardOn, "Scoring Bar Event Score Info") != null,
                      "★ B2 5 档里程碑记分条在（活动期形态）");
            CheckTrue(Find(rewardOn, "Score Bar Line Level 1") != null
                      && Find(rewardOn, "Score Bar Line Level 5") != null,
                      "★ B2b `Level 1`…`Level 5` 都在（`Scoring Bar Event Score Info` 的用户点名件）");
            var topTimer = DirectChild(skOn.transform, "Timer");
            CheckTrue(topTimer != null && topTimer.GetComponentInChildren<ImageQuad>(true) != null,
                      "★ B3 活动倒计时那颗 `Timer` **在**（活动期才有它）"
                    + "｜🧨 把 `BuildExtras` 改成恒不建 ⇒ 红");
            foreach (var nm in ClassicFamily)
                Check(CountNamed(skOn.transform, nm), 0,
                      $"★ B4 活动期 ⇒ **`{nm}` 一个节点都没有**（经典式那一族不许混进活动期形态）");

            // ========================================================== C：结构互斥（两棵树同时活着，逐名取异或）
            bool allXor = true;
            string firstBad = null;
            foreach (var nm in EventFamily)
            {
                bool inA = CountNamed(skOff.transform, nm) > 0, inB = CountNamed(skOn.transform, nm) > 0;
                if (inA == inB) { allXor = false; if (firstBad == null) firstBad = nm + "(" + inA + "/" + inB + ")"; }
            }
            foreach (var nm in ClassicFamily)
            {
                bool inA = CountNamed(skOff.transform, nm) > 0, inB = CountNamed(skOn.transform, nm) > 0;
                if (inA == inB) { allXor = false; if (firstBad == null) firstBad = nm + "(" + inA + "/" + inB + ")"; }
            }
            CheckTrue(allXor,
                      "★ C1 **结构互斥**：两族共 " + (EventFamily.Length + ClassicFamily.Length) + " 个节点名，"
                    + "每一个都**只出现在其中一棵树**里（异或全成立）"
                    + (firstBad == null ? "" : "；反例 = " + firstBad)
                    + "｜🧨 「两棵都建」或「两棵都不建」⇒ 红");

            // ========================================================== D：FastMode 判据是「真的」
            // D1 对照实验：**同一个挑选函数**在 13 份 SO 上选得出 Scoring ⇒ 这条链是活的。
            LiveOpsLocalEvent dEv; string dWhy;
            CheckTrue(LiveOpsLocalCatalog.TrySelectActive(LiveOpsLocalKind.Scoring,
                                                          Utc(2025, 4, 20), out dEv, out dWhy)
                      && dEv != null && dEv.Kind == LiveOpsLocalKind.Scoring,
                      "★ D1 对照：`_events`（13 份 SO）在 2025-04-20 选得出 Scoring —— "
                    + "说明 `TryPickEventIn` 这条挑选链是**活的**（D2/D3 拿它当基准）");

            // D2 在期合成事件 ⇒ 选得出（且选中就是它）
            LiveOpsLocalCatalog.FastModeEventsOverrideForTest =
                new[] { SyntheticFastMode(Utc(2025, 4, 19), Utc(2025, 4, 21)) };
            CheckTrue(LiveOpsLocalCatalog.TrySelectActiveFastMode(Utc(2025, 4, 20), out dEv, out dWhy)
                      && dEv != null && dEv.EventId == "E15_Synthetic_FastMode",
                      "★ D2 FastMode 表里有**在期**事件 ⇒ 读口返回 true 且选中的就是它"
                    + "｜🧨 判据改成写死 `false` ⇒ 红");

            // D3 过期合成事件 ⇒ 选不出（`⛔ 过期事件永不入选`，与其余三类同一条闸）
            LiveOpsLocalCatalog.FastModeEventsOverrideForTest =
                new[] { SyntheticFastMode(Utc(2024, 1, 1), Utc(2024, 1, 2)) };
            CheckTrue(!LiveOpsLocalCatalog.TrySelectActiveFastMode(Utc(2025, 4, 20), out dEv, out dWhy),
                      "★ D3 表里只有**过期**事件 ⇒ 选不出（`IsInPeriod` 那道闸对 FastMode 同样有效）"
                    + "｜🧨 把 `IsInPeriod` 那道判去掉 ⇒ 红");

            // D4 表空 ⇒ false **且说得清原因**（出声一次；⛔ 不静默）
            LiveOpsLocalCatalog.FastModeEventsOverrideForTest = null;
            LiveOpsLocalCatalog.ResetWarnedForTest();      // 让这一条能重新走到「出声」那一支
            bool dEmpty = LiveOpsLocalCatalog.TrySelectActiveFastMode(Utc(2025, 4, 20), out dEv, out dWhy);
            CheckTrue(!dEmpty && dEv == null
                      && !string.IsNullOrEmpty(dWhy)
                      && dWhy.Contains("FastMode") && dWhy.Contains("表"),
                      "★ D4 表空 ⇒ `false` + `why` 说清「本机没有 FastMode 事件 / 表里没数据」"
                    + "（⛔ 不是裸 `return false;`）"
                    + "｜🧨 改成裸 `return false;` ⇒ `why` 空 ⇒ 红");
            CheckTrue(!LiveOpsLocalCatalog.HasActiveFastModeEvent(Utc(2025, 4, 20), out dEv, out dWhy),
                      "★ D4b `HasActiveFastModeEvent`（`SkirmishEventWindow` 用的那个名字）与上一行同解");
            Check((int)LiveOpsLocalKind.FastMode, 3,
                  "★ D5 `LiveOpsLocalKind.FastMode = 3` —— **新加的一类**，不与 Parent(0)/Scoring(1)/PremiumItemOffer(2) 撞号");
            Check(LiveOpsLocalCatalog.FastModeEventCount, LiveOpsLocalCatalog.FastModeEvents.Count,
                  "★ D5b `FastModeEventCount` 与表长同真（现读 " + LiveOpsLocalCatalog.FastModeEventCount + " 条）");
        }
        catch (System.Exception ex)
        {
            // 🔴 不许让异常静默跳过一整节（同「不许静默失败」那条红线）
            CheckTrue(false, "★ E15 断言自身抛异常（这一条算**红**、⛔ 不是跳过）："
                             + ex.GetType().Name + " · " + ex.Message);
        }
        finally
        {
            // ---- 收尾：拆窗 + 复位所有注入点 + 复原存档指向（⛔ 不留窗、不碰玩家真存档）
            if (skOff != null) RewardsWindow.DestroySafe(skOff.gameObject);
            if (skOn != null) RewardsWindow.DestroySafe(skOn.gameObject);
            LiveOpsLocalCatalog.FastModeEventsOverrideForTest = keepFastOverride;
            SkirmishEventWindow.ResetEventProbeForTest();     // 先把闸/闩清干净…
            SkirmishEventWindow.ForceActiveEventForTest = keepForce;   // …再把宿主原来的注入值放回去
            LiveOpsEventWindow.LastOpened = keepLast;
            DeckStore.OverridePath = keepPath;
            try { System.IO.File.Delete(tmp); } catch { }
            CollectionData.ResetForTest();
            Debug.Log(_sink.P + "合计：通过 " + _sink.Pass + " / 失败 " + _sink.Fail
                      + (keepPath == null ? "" : "（存档指向已还原）"));
        }
    }
}
