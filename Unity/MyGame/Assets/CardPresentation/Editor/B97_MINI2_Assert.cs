// B97_MINI2_Assert.cs —— 执行代理 `B97_MINI2`（第九十六会话 · 2026-10-29）的断言
//
// ============================ 它盯的是哪两格 ============================
//   A  `资料/完全复刻/账/未分类_其他.tsv:312` —— `GameObjectUniqueIdentifier.FindComponent` 那一行里缺的第 12 个注册点：
//      原版 `CardCollectionDisplay.referenceCardPoint` 指的载体节点 **`Reference Card Pointer`**（走「**形状派**」真建）。
//      判据 = `bundle_menus_assets_all` 现读：`CardsTab ▸ Collection Display ▸ Reference Card Pointer`
//        · 组件 = `GameObjectUniqueIdentifier(id = "CardCollectionReferenceCardPointer")`
//        · `RectTransform_-1444448670045642027.json`：anchorMin/Max = pivot = (0.5,0.5) ·
//          anchoredPosition = (-544.3845, 316.5719) · sizeDelta = 193.839 × 45.0918
//          ⇒ 绝对矩形 **402.3,236.4 → 596.1,281.4**（= `CollectionWindow.WcRefL/T/R/B`）
//        · `GameObject/Reference Card Pointer.json`：`m_IsActive: true`、`m_Layer: 5`、**没有 Image / 没有子件**。
//      ⚠️ 铁律 5 订正：同名的另一颗（`…_4160045545866129188.json`，`m_IsActive: false`、417.69,260.95→611.53,306.05、
//         **一个组件都没有**）是**卡组编辑窗**那一份（`Deck/DeckRuntime.PoolRef*`），⛔ 不是本窗这一颗。
//   B  `资料/完全复刻/账/未分类_其他.tsv:430` —— `MilestoneScoreBar.Initialize` 的宿主接线：
//      两扇窗（`Shell/SkirmishEventWindow.cs` / `Shell/EnergySinglePlayerOnlyEventWindow.cs`）现在挂**真组件**
//      （`MilestoneScoreBar` + 5 × `GenericScoreBarLine` + `Progress Bar` 上的 `Slider`），
//      数据用 **本地模拟表**（`Shell/MilestoneLocalSimulation.cs`，`🔶 自己设计`：服务器已关服）。
//      🔴 这条要钉住的核心是：**接上组件这一步不改变任何可见像素**（分数仍 `1256`、箱子仍闭箱、高亮仍亮、进度仍 0）。
//
// 逐条结论 / 判据出处 / 没查清的部分 → `资料/完全复刻/报告/B97_MINI2.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么接（⚠️ 本代理不改宿主）============================
// 宿主建议 = **`Editor/ShellScene.cs`**（那里 `WindowsManager.Instance` 是活的，且同文件 `:9437` 已有
//   `CollectionWindow.Create(shell.Windows)` + `DestroySafe` 的先例）：
//     1) 在 `Run()` 里加一句 `B97MINI2Assert.Run();`
//     2) 紧挨着加计数转发：`_sink.Pass += B97MINI2Assert.Pass; _sink.Fail += B97MINI2Assert.Fail;`
//   ⛔ 不要接在 `Editor/RuleEngineTest.cs`（那是调度台独占、且在引擎侧跑，没有壳的 `WindowsManager`）。
//   · 本文件**不建持久资产**；建的 `GameObject` / `Texture2D` **全部在 `finally` 里 `DestroyImmediate`**。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   A1..A4：把 `WcRefL/T/R/B` 任一个改成别的数（或改成读 prefab 的「我以为的值」）⇒ 对应那一条红。
//   A6：  把 `BuildCardsPage` 里那两句 `Node(page, "Reference Card Pointer", …)` / `RegisterUnique(…)` 删掉 ⇒ 红。
//   A8：   把节点建在别的父件下 / 用错矩形 ⇒ 世界位置对不上 ⇒ 红。
//   A9/A10：不登记、或登记到别的节点 ⇒ 红。
//   B2/B3：把模拟表里的 `score` 改成别的（比如真编一套 100/300/… 的阈值）⇒ B2 红；
//          把 `state` 改成 `Locked` ⇒ B3/B7 红（Energy 窗那颗不透明金高亮会被关掉 = **改画面了**）。
//   B5/B6：`MilestoneScoreBar.Initialize` 没走到 `GenericScoreBarLine.Initialize` ⇒ 文本停在 `"?"` ⇒ 红。
//   B8：   把 `Unlocked` 支写成「换开箱图」⇒ 红（原版**只有 `Collected` 换 `chestOpen`**）。
//   B10：  把进度改成线性 `current/max` 或写死非 0 ⇒ 红。
//   C1..C4：把两扇宿主里的 `_scoreBar` / `WireScoreBar` 删掉（= 回到「手工等价族」）⇒ 红。
using System.Reflection;
using CardPresentation;
using SB = CardPresentation.ScoreBars;
using UnityEngine;

/// <summary>执行代理 `B97_MINI2`：账 `:312` 的载体节点 + 账 `:430` 的记分条组件接线。
/// <para>计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。⛔ 期望值全是**字面量 / prefab 现读值**，
/// 不从被测实现里读回来（例：`A1..A4` 写的是 402.3 / 236.4 / 596.1 / 281.4 这四个字面量）。</para></summary>
public static class B97MINI2Assert
{
    static readonly CheckSink _sink = new CheckSink("[MINI2] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Near(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    /// <summary>唯一入口（宿主 `Run()` 里调一句）。</summary>
    public static void Run()
    {
        SectionA_ReferenceCardPointer();
        SectionB_MilestoneScoreBarFactoryNeutral();
        SectionC_WindowWiringShape();
        Debug.Log(_sink.P + "MINI2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 账 312（形状派：真建那颗节点）
    static void SectionA_ReferenceCardPointer()
    {
        MenuCheck.Section(_sink, "A 账 312：`Reference Card Pointer` 载体节点（矩形 = prefab 现读；唯一 id 已登记）");

        // ---- A1..A4：尺子本身（= `CheckFocusedArmy` 与建节点**共用**的那一组常量）----
        Near(CollectionWindow.WcRefL, 402.3f, 0.01f, "A1 `WcRefL` = 原版 **402.3**");
        Near(CollectionWindow.WcRefT, 236.4f, 0.01f, "A2 `WcRefT` = 原版 **236.4**");
        Near(CollectionWindow.WcRefR, 596.1f, 0.01f, "A3 `WcRefR` = 原版 **596.1**");
        Near(CollectionWindow.WcRefB, 281.4f, 0.01f, "A4 `WcRefB` = 原版 **281.4**");

        var mgr = WindowsManager.Instance;
        Tr(mgr != null, "A0 前提：`WindowsManager.Instance` 非空（本节要建一棵收藏窗来量它建出来的节点）"
                      + " —— 不成立 ⇒ A5..A10 **没跑**（本文件不静默跳过：这一条会红）");
        if (mgr == null) return;

        CollectionWindow win = null;
        try
        {
            win = CollectionWindow.Create(mgr);
            // 🔴 2026-10-30 补：`Create()` **不建页**（`Build()` 只在 `Open()` 里，`Shell/CollectionWindow.cs:3412`）
            //   ⇒ 不先建一遍，`PageRoot(1)` / `_pages[1]` 恒 null（`tabs` 也只有 `Build()` 才填，见 `:3856-3863`）。
            //   ⚠️ 这里用 **`Build()`** 而**不是** `mgr.OpenWindow(win)`（后者是两个先例 `Editor/CollectionScene.cs:862-863` /
            //   `Editor/B99W2_Assert.cs:78-79` 的写法）：本件是挂在外壳宿主里跑的，`OpenWindow` 的全屏支会
            //   `HideAllWindows()` 把宿主此刻开着的别的窗一起藏掉、还会用 `null` 调 `SetupData`（`B99W2` 那行自己写了这个坑）。
            //   本段只要「四页建出来」⇒ `Build()` 就够，⛔ 不动别人的窗。
            win.Build();
            var page = win.PageRoot(1);            // 1 = Cards 页（首次取页会触发 `SetupOnce()` ⇒ `BuildCardsPage`）
            // ⚠️ 上面两句若在本宿主里抛异常（缺存档/缺壳锚点），**不许静默跳过** ⇒ 由下面的 catch 报成一条红。
            Tr(page != null, "A5 前提：Cards 页建出来了");
            if (page == null) return;

            var node = MenuCheck.FindChild(page, "Reference Card Pointer");
            Tr(node != null, "A6 ★ 收藏窗的 Cards 页里**真建**了 `Reference Card Pointer`（原来本仓一颗都没有）");
            if (node == null) return;

            var rt = node as RectTransform;
            Tr(rt != null, "A7 前提：它带 `RectTransform`（原版就是 `RectTransform`）");
            if (rt != null)
            {
                // `MenuDraw.SetPxSize` 写的是 `LayoutSpace.Px(px)`（108 px = 1 世界单位）⇒ 乘回来看 px。
                Near(rt.sizeDelta.x * 108f, 193.84f, 0.5f, "A7a ★ `sizeDelta.x` 还原成 px ≈ **193.84**（原版矩形宽）");
                Near(rt.sizeDelta.y * 108f, 45.09f, 0.5f, "A7b ★ `sizeDelta.y` 还原成 px ≈ **45.09**（原版矩形高）");
            }

            var want = LayoutSpace.RectCenter(402.3f, 236.4f, 596.1f, 281.4f);
            float d = Vector3.Distance(node.position, want);
            Tr(d <= 0.01f, "A8 ★ 节点世界位置 = 原版矩形中心（402.3,236.4→596.1,281.4）——"
                         + " 误差 " + (d * 108f).ToString("F2") + "px（容差 1.08px）");

            var reg = MenuUniqueIds.Find("CardCollectionReferenceCardPointer");
            Tr(reg != null, "A9 ★ 已按唯一 id `CardCollectionReferenceCardPointer` 登记"
                          + "（= 原版 `GameObjectUniqueIdentifier.Awake` 自注册那一跳；本仓由 `MenuTutorialFlow.RegisterUnique` 代劳）");
            Tr(reg == node.gameObject, "A10 登记表里那一格**就是**这颗节点（不是别的同 id 节点）");
        }
        catch (System.Exception e)
        {
            // ⛔ 不静默：建窗/建页在**这个宿主**里跑不起来 ⇒ 记一条红（而不是「跳过」）。
            Tr(false, "A 段：建收藏窗 / 建 Cards 页时抛异常 —— " + e.GetType().Name + "：" + e.Message
                    + "（= 「断言环境不成立」，不是被复刻的行为有问题；换宿主 `Editor/CollectionScene.cs` 可避开）");
        }
        finally
        {
            if (win != null) MainMenuSubmenuWindow.DestroySafe(win.gameObject);
        }
    }

    // ============================================================ B · 账 430（接组件后出厂外观零变化）
    static void SectionB_MilestoneScoreBarFactoryNeutral()
    {
        MenuCheck.Section(_sink, "B 账 430：`MilestoneScoreBar` 接上后**出厂外观零变化**（本地模拟表，`🔶 自己设计`）");

        // ---- B1..B4：模拟表的取值口径（见 `Shell/MilestoneLocalSimulation.cs` 文件头）----
        var tbl = SB.MilestoneLocalSimulation.CreateTable();
        C(tbl.Length, 5, "B1 模拟表 = 5 档（prefab 的 `scoringLines` 就是 5 行 `Score Bar Line Level 1..5`）");
        bool allScore = true, allUnlocked = true, allRewards = true;
        for (int i = 0; i < tbl.Length; i++)
        {
            if (tbl[i] == null) { allScore = allUnlocked = allRewards = false; continue; }
            if (tbl[i].score != 1256) allScore = false;
            if (tbl[i].state != GenericScoringMilestoneState.Unlocked) allUnlocked = false;
            if (tbl[i].rewards == null) allRewards = false;
        }
        Tr(allScore, "B2 ★ 每档 `score` = **1256**（= prefab 里 5 行 `Score` 的 TMP 出厂原文 `'1256'`）"
                   + "｜🧨 改成一串真阈值（100/300/…）⇒ 屏上数字跟着变 ⇒ 这条红（那正是「画面不能变」要挡的）");
        Tr(allUnlocked, "B3 ★ 每档 `state` = `Unlocked`（= prefab 出厂「`Highlight Crate` `act = 1`」那一态）");
        Tr(allRewards, "B4 每档 `rewards` 非 null（空数组 —— 本地没有奖励对象）");

        // ---- B5..B11：真组件 + 同形 5 行树 ⇒ 出厂外观逐条不变 ----
        var root = new GameObject("B97MINI2_B", typeof(RectTransform));
        var texFactory = new Texture2D(4, 4) { name = "B97MINI2_chestFactory" };
        var texOpen = new Texture2D(4, 4) { name = "B97MINI2_chestOpen" };
        try
        {
            var barGo = new GameObject("Scoring Bar Event Score Info", typeof(RectTransform));
            barGo.transform.SetParent(root.transform, false);
            var slider = barGo.AddComponent<UnityEngine.UI.Slider>();
            var msb = barGo.AddComponent<SB.MilestoneScoreBar>();

            var lines = new SB.GenericScoreBarLine[5];
            var labels = new Label[5];
            var chests = new ImageQuad[5];
            var hi = new GameObject[5];
            for (int i = 0; i < 5; i++)
            {
                var row = new GameObject("Score Bar Line Level " + (i + 1), typeof(RectTransform));
                row.transform.SetParent(barGo.transform, false);
                labels[i] = Label.Create(row.transform, "?", Vector3.zero, 4, Color.white,
                                         new Vector2(0.5f, 0.5f), "Score");
                chests[i] = ImageQuad.Create(row.transform, texFactory, Vector3.zero, 0.1f,
                                             new Vector2(0.5f, 0.5f), "Chest");
                hi[i] = new GameObject("Highlight Crate", typeof(RectTransform));
                hi[i].transform.SetParent(row.transform, false);
                lines[i] = row.AddComponent<SB.GenericScoreBarLine>();
                lines[i].Bind(labels[i], chests[i], texOpen, hi[i], null);
            }
            Tr(labels[0] != null && chests[0] != null && lines[0] != null,
               "B5 前提：5 行树建出来了（Label / Chest quad / `GenericScoreBarLine`）");
            if (labels[0] == null || chests[0] == null || lines[0] == null) return;

            msb.Bind(slider, lines);
            CoreLogOnce();
            msb.Initialize(SB.MilestoneLocalSimulation.CurrentScore, tbl, null);

            C(labels[0] != null ? labels[0].Text : null, "1256",
              "B6 ★ 第 1 行 `Score` = `1256`（与改前出厂逐字相同 ⇒ 接组件这一步**没有改画面**）");
            C(labels[4] != null ? labels[4].Text : null, "1256", "B7 第 5 行同");
            Tr(hi[0] != null && hi[0].activeSelf,
               "B8 ★ 高亮节点保持**开着**（`Unlocked` 支 ⇒ `readyToCollect.SetActive(true)`；"
             + "改前 Energy 窗画不透明金、Skirmish 窗画 alpha 0 ⇒ 开/关对两扇窗的可见结果都一致）");
            Tr(chests[0] != null && ReferenceEquals(chests[0].Texture, texFactory),
               "B9 ★ `Unlocked` 的箱子仍是**出厂闭箱图**（`originalChest`；原版**只有 `Collected` 换 `chestOpen`**）");
            Near(slider.maxValue, 1256f, 0.01f, "B10 `progressBar.maxValue` = 末档 1256");
            Near(slider.normalizedValue, 0f, 1e-4f,
                 "B11 ★ 出厂进度 = 0（`InterpolateMilestones(0, [0,1256…])`；改成线性或写死非 0 ⇒ 红）");
            C(slider.wholeNumbers, false, "B12 `wholeNumbers = false`（原版那句常量，不是形参）");

            // ---- B13..B16：第二态 —— `FromScore` 那三档状态机（原版 `RefreshScore` 的投影）----
            var ms2 = SB.MilestoneLocalSimulation.FromScore(500, new[] { 100, 400, 900 }, 1);
            C(ms2.Length, 3, "B13 `FromScore` 按阈值表长度成表");
            if (ms2.Length == 3)
            {
                C((int)ms2[0].state, 2, "B14 ★ `500 >= 100` 且 `0 < 已领 1` ⇒ 第 1 档 `Collected`（2）");
                C((int)ms2[1].state, 1, "B15 ★ `500 >= 400`、`1 < 1` 不成立 ⇒ 第 2 档 `Unlocked`（1）");
                C((int)ms2[2].state, 0, "B16 ★ `500 < 900` ⇒ 第 3 档 `Locked`（0）");
            }
        }
        finally
        {
            Object.DestroyImmediate(texFactory);
            Object.DestroyImmediate(texOpen);
            Object.DestroyImmediate(root);
        }
    }

    static void CoreLogOnce() { SB.MilestoneLocalSimulation.LogOnce("B97MINI2"); }

    // ============================================================ C · 两扇宿主的结构钉子
    static void SectionC_WindowWiringShape()
    {
        MenuCheck.Section(_sink, "C 两扇宿主的接线结构（字段/方法在 ⇐ 行为见 B 段；⛔ 别把结构当行为证明）");

        const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;

        var fS = typeof(SkirmishEventWindow).GetField("_scoreBar", F);
        Tr(fS != null && fS.FieldType == typeof(SB.MilestoneScoreBar),
           "C1 ★ `SkirmishEventWindow._scoreBar` 是 `MilestoneScoreBar`（原来这扇窗只有手工等价族、组件零实例化）");
        Tr(typeof(SkirmishEventWindow).GetMethod("WireScoreBar", F) != null,
           "C2 ★ `SkirmishEventWindow.WireScoreBar(Transform, Slider)` 在（绑 5 行 + 喂模拟表的那一跳）");
        Tr(typeof(SkirmishEventWindow).GetProperty("ScoreBarForTest", BindingFlags.Instance | BindingFlags.Public) != null,
           "C3 …`SkirmishEventWindow.ScoreBarForTest` 自检读口在");

        var fE = typeof(EnergySinglePlayerOnlyEventWindow).GetField("_scoreBar", F);
        Tr(fE != null && fE.FieldType == typeof(SB.MilestoneScoreBar),
           "C4 ★ `EnergySinglePlayerOnlyEventWindow._scoreBar` 是 `MilestoneScoreBar`");
        Tr(typeof(EnergySinglePlayerOnlyEventWindow).GetMethod("WireScoreBar", F) != null,
           "C5 ★ `EnergySinglePlayerOnlyEventWindow.WireScoreBar(Transform, Slider)` 在");
        Tr(typeof(EnergySinglePlayerOnlyEventWindow).GetMethod("PinChestAspect", F) != null,
           "C5b ★ `EnergySinglePlayerOnlyEventWindow.PinChestAspect()` 在 —— 箱子框 1.1211 而贴图 512² ⇒ "
         + "`GenericScoreBarLine.SetChest` 会把 `_aspect` 拉成 1.0（窄 10.8%，静默）⇒ 这一跳必须把比例钉回框");
        Tr(typeof(EnergySinglePlayerOnlyEventWindow).GetProperty("ScoreBarForTest", BindingFlags.Instance | BindingFlags.Public) != null,
           "C6 …`EnergySinglePlayerOnlyEventWindow.ScoreBarForTest` 自检读口在");
    }
}
