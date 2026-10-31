// B109_E32_EventImage_Assert.cs — 执行代理 `B109_E32`（第一百零九会话）的断言集
//
// ============================ 它盯的是什么 ============================
// 判据 = `资料/完全复刻/判据/B109_R16_死码C45.md` 的 **C14 / C15 / C17 / C18 / C26 / C27** 六条。
//
//  · **C26 / C27**（真做）：`TwoSidesEvent.PurchaseMilestone` / `TryCollect` 的**本地那一半**
//    （可领判定三支 + no-op 回调），落点 = `Shell/ParentEventWindow.cs` 类尾追加区。
//    判据三源 → `cpp2il/out_il2csharp/Assembly-CSharp/TwoSidesEvent.cs:87-146`（主力）·
//               `decomp_full/TwoSidesEvent__PurchaseMilestone.c` · `SelectTeam` 读 `+0x38` 反证 `+0x20 = Save`。
//  · **C14 / C15 / C17 / C18**（被判为「组件已实现、只差宿主接线」）：本文件只断**组件本身**的语义
//    （`EventImageSelectorController.Initialize` / `TryDraw` · `LiveopUIDrawer_EventAssetSprites.Initialize`）。
//    🔴 **接线没做，而且原版宿主不是 `ParentEventWindow`**（2026-10-31 `B109_E32` 现核）：
//      `EventImageSelectorController` 唯一实例在 prefab **`DraftModeMenuDemo`** 的 `Special Missions` 下
//      （MB `-316068969454671126`：两格 `RankingIcon(6)` / `HeaderImage(3)`）；
//      `LiveopUIDrawer_EventAssetSprites` 两个实例也都在 **`DraftModeMenuDemo`**（MB `-256574493845456187` /
//      `5714981760248277738`，各一格 `GameModeIcon(12)`，挂 `Game Mode Header With Back Button/Header Background/Game Mode Icon`）。
//      ⇒ 生产接线属于 `Shell/DraftModeWindow.cs`（不在本批白名单）⇒ 本文件**不会**把那条缺口断成绿，
//      只把组件的真调用/参数断死；接线补丁见报告 §补丁。
//
// ============================ 怎么用（宿主）============================
//   `B109_E32_EventImage_Assert.Run(_sink);`     // 需要一个 `CheckSink`
//   推荐位置 = `Editor/ShellScene.cs` 或 `Editor/MainMenuScene.cs` 里**已有的一行**行尾追加（同一物理行 ⇒ 行号零漂移；
//   ⚠️ 追加前先看那一行第一个 `//` 在哪 —— `CLAUDE.md` §三 那条「代码落进行末注释」的坑）。
//   ⚠️ 本文件白名单不含任何宿主 ⇒ 今天**一条都不会跑**（如实标）。
//   自建探针：一棵**裸** `ParentEventWindow`（不 `Open()` ⇒ 不建树、不依赖 `Resources/Art/` 与字体），
//   跑完 `DestroyImmediate`；`InitializeMilestones` 在裸窗上已安全（`EnsureP2RewardPanel` 全是 null 闸）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · K3 / K4 只差 `collectedRewards` 一个量（2 与 1）：把判据写成 `collectedRewards > Count` 之类 ⇒ 两条不会翻面；
//     把 `index < collectedRewards` 的严格不等号写成 `<=` ⇒ K4 会掉进「已领完」支 ⇒ 红。
//   · K5 用 `currentScore == scoreToUnlock` 这一档：把原版谓词 `r.scoreToUnlock > PlayerScore` 写成 `>=`
//     （= 把「刚好够分」判成没够）⇒ K5 红。
//   · K2 断言 `onComplete` 被调 **且长度 0**、`onError` **没被调**：把数据缺那一支改成「什么都不回调」⇒ K2 红；
//     改成「调 onError」⇒ K2 红。
//   · K4 断言 `onError` 被调 **且 `onComplete` 没被调**：把 P3 支写成「回调空数组假装成功」⇒ K4 红（那是静默失败）。
//   · E1/E2 是同一组输入只差 `eventAssetType`：把 `TryDraw` 的 type 比较写反/写成恒真 ⇒ E2 红。
//   · W1 数的是**目标被写了几次**（不是返回值）：把外层循环提前 `break` / 只喂第一格 ⇒ W1 红。

using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>`B109_E32`：C26 / C27（`PurchaseMilestone` / `TryCollect`）+ C14/C15/C17/C18（两个事件图组件）的断言。</summary>
public static class B109_E32_EventImage_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B109E32] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    /// <summary>独立跑（自带 sink，合计打到 Console）。</summary>
    public static void Run() { RunCore(_sink); }

    /// <summary>宿主跑（用宿主的 sink，合计进宿主读数）。</summary>
    public static void Run(CheckSink s) { RunCore(s ?? _sink); }

    /// <summary>自检用的「一格图」：只记两格 + 被写了几次。**不是 MonoBehaviour**。</summary>
    sealed class FakeTarget : IEventImageTarget
    {
        public Texture2D Sprite { get; set; }
        bool _enabled;
        public bool Enabled { get { return _enabled; } set { _enabled = value; EnabledWrites++; } }
        public int EnabledWrites;
    }

    static void RunCore(CheckSink s)
    {
        MenuCheck.Section(s, "B109_E32 · C26/C27（PurchaseMilestone/TryCollect）+ C14/15/17/18（事件图组件）");
        try { MilestonesCore(s); }
        catch (System.Exception e)
        {
            MenuCheck.True(s, false, "C26/C27 断言段抛异常：" + e.GetType().Name + "：" + e.Message);
        }
        try { ImageSelectorsCore(s); }
        catch (System.Exception e)
        {
            MenuCheck.True(s, false, "C14/C15/C17/C18 断言段抛异常：" + e.GetType().Name + "：" + e.Message);
        }
        Debug.Log(s.P + "B109E32 合计：" + s.Pass + " 通过 / " + s.Fail + " 失败");
    }

    // ============================================================ C26 / C27
    static void MilestonesCore(CheckSink s)
    {
        ParentEventWindow pw = null;
        try
        {
            var go = new GameObject("B109_E32_Probe_ParentEventWindow");
            pw = go.AddComponent<ParentEventWindow>();     // 裸窗：不 Open() ⇒ 不建树，只需要那几个字段
        }
        catch (System.Exception e)
        {
            MenuCheck.True(s, false, "K0 **前提**：裸 `ParentEventWindow` 建不出来（抛 " + e.GetType().Name
                                   + "：" + e.Message + "）⇒ 下面 C26/C27 全部空转｜本文件不静默跳过");
            return;
        }
        try
        {
            MenuCheck.True(s, pw != null, "K0 **前提**：拿到 `ParentEventWindow` 实例（拿不到 ⇒ 下面全部空转）");

            MenuCheck.Check(s, pw.LastCollectOutcome, 0, "K1 初始状态：还没走过 `TryCollect` ⇒ `LastCollectOutcome == 0`");

            // ---- K2：数据没注入 ⇒ 出声 + 走 no-op 支（原版 `Data.ScoringMilestones == null` 落到末句）----
            int noopCalls = 0, noopLen = -1, errCalls = 0;
            bool p3 = pw.PurchaseMilestone(0,
                                           r => { noopCalls++; noopLen = r == null ? -1 : r.Length; },
                                           _ => { errCalls++; });
            MenuCheck.Check(s, p3, false, "K2-① 数据缺 ⇒ **不走 P3 支**（原版 `ms == null` 落到末句）");
            MenuCheck.Check(s, pw.LastCollectOutcome, 3, "K2-② 数据缺 ⇒ `LastCollectOutcome == 3`（🧨 改成「什么都不回调」或「调 onError」⇒ 红）");
            MenuCheck.Check(s, noopCalls, 1, "K2-③ 数据缺 ⇒ `onComplete` **被调 1 次**（原版末句照抄）");
            MenuCheck.Check(s, noopLen, 0, "K2-④ 传给 `onComplete` 的是 **空数组**（长度 0；🧨 传 null ⇒ 读成 -1 ⇒ 红）");
            MenuCheck.Check(s, errCalls, 0, "K2-⑤ 数据缺 ⇒ `onError` **不调**（那一支不是失败）");

            // 两档里程碑：100 / 200 分。
            var ms = new List<ScoringMilestone>
            {
                new SimpleScoringMilestone(100, 0),
                new SimpleScoringMilestone(200, 0),
            };

            // ---- K3：够分但**全部已领**（collected 2/2）⇒ no-op ----
            pw.InitializeMilestones(250, 2, ms);
            noopCalls = 0; errCalls = 0;
            bool p3b = pw.PurchaseMilestone(2, r => { noopCalls++; noopLen = r == null ? -1 : r.Length; }, _ => { errCalls++; });
            MenuCheck.Check(s, p3b, false, "K3-① 够分 + 已领完 ⇒ 不走 P3 支");
            MenuCheck.Check(s, pw.LastCollectOutcome, 1, "K3-② ⇒ `LastCollectOutcome == 1`（no-op 支）");
            MenuCheck.Check(s, noopCalls, 1, "K3-③ ⇒ `onComplete` 被调一次（空数组）");
            MenuCheck.Check(s, errCalls, 0, "K3-④ ⇒ `onError` 不调");

            // ---- K4：够分 + 还有没领的（collected 1/2）⇒ P3 支（⛔ 不许假装领到）----
            pw.InitializeMilestones(250, 1, ms);
            noopCalls = 0; errCalls = 0;
            bool p3c = pw.PurchaseMilestone(1, r => { noopCalls++; }, _ => { errCalls++; });
            MenuCheck.Check(s, p3c, true, "K4-① 可领 ⇒ 走 P3 支（`PurchaseMilestone` 返回 true）");
            MenuCheck.Check(s, pw.LastCollectOutcome, 2, "K4-② ⇒ `LastCollectOutcome == 2`（🧨 与 K3 只差 collected 一个量 ⇒ 不能同时绿）");
            MenuCheck.Check(s, errCalls, 1, "K4-③ ⇒ `onError` **被调 1 次**（本地没有云脚本 ⇒ 如实失败）");
            MenuCheck.Check(s, noopCalls, 0, "K4-④ ⇒ `onComplete` **不调**（🧨 改成「回调空数组假装成功」⇒ 红，那是静默失败）");
            MenuCheck.True(s, !string.IsNullOrEmpty(pw.LastCollectError) && pw.LastCollectError.Contains("P3"),
                           "K4-⑤ `LastCollectError` 点名 **P3**（`" + (pw.LastCollectError ?? "<null>") + "`）");

            // ---- K5：边界 —— `currentScore == scoreToUnlock` 算「够分」（原版 `r.scoreToUnlock > PlayerScore` 取反）----
            pw.InitializeMilestones(100, 0, ms);
            bool p3d = pw.PurchaseMilestone(0, null, null);
            MenuCheck.Check(s, p3d, true, "K5 刚好够分（100 == 100）⇒ 仍然可领（🧨 把谓词写成 `>=` ⇒ 红）");

            // ---- K6：`CollectClicked` 真的走 `TryCollect`（不是另写一份）----
            pw.InitializeMilestones(250, 1, ms);
            pw.CollectClicked();
            MenuCheck.Check(s, pw.LastCollectOutcome, 2, "K6 ★ `CollectClicked()` 走 `TryCollect` ⇒ 同一支（🧨 各自实现一份 ⇒ 这里读到旧值 ⇒ 红）");

            // ---- K7：`TryCollect` 体只有一句转调（`onComplete` 真被调）----
            pw.InitializeMilestones(250, 2, ms);
            int viaTry = 0;
            pw.TryCollect(_ => { viaTry++; }, null);
            MenuCheck.Check(s, viaTry, 1, "K7 `TryCollect(onComplete)` 的 `onComplete` 被调（原版体只有一句转调；🧨 不转调 ⇒ 0 ⇒ 红）");
        }
        finally
        {
            if (pw != null) Object.DestroyImmediate(pw.gameObject);
        }
    }

    // ============================================================ C14 / C15 / C17 / C18
    static void ImageSelectorsCore(CheckSink s)
    {
        // ---- E 组：`EventImageSelectorController`（原版 `EventImageSelectorController.cs`）----
        var tgtHit = new FakeTarget();
        var tgtMiss = new FakeTarget();
        var draw = new List<EventImageSelectorController.ImageEventDrawData>
        {
            new EventImageSelectorController.ImageEventDrawData { Image = tgtHit,  EventAssetType = (int)LiveOpsAssetRef.GameModeIcon },
            new EventImageSelectorController.ImageEventDrawData { Image = tgtMiss, EventAssetType = (int)LiveOpsAssetRef.HeaderImage },
        };
        // 只喂 GameModeIcon 那一格 ⇒ 命中 1 格、另一格必须被关掉。
        var assets = new List<EventImageSelectorController.EventAsset>
        {
            new EventImageSelectorController.EventAsset { Type = (int)LiveOpsAssetRef.GameModeIcon, AssetName = "B109_E32_Probe_Icon" },
        };
        int hit = EventImageSelectorController.Initialize(draw, assets);

        MenuCheck.Check(s, hit, 0, "E1-① 宿主没导那张图 ⇒ 命中格画出 **0** 张（原版 `Load` 返 null 的同一支）");
        MenuCheck.Check(s, draw[0].LastAssetName, "B109_E32_Probe_Icon",
                        "E1-② 命中格记下了资产名（**按资产名取图**那一跳真被调用）");
        MenuCheck.True(s, tgtHit.EnabledWrites >= 1, "E1-③ 命中格被**写过 enabled**（🧨 不写 ⇒ 红）");
        MenuCheck.Check(s, draw[1].LastAssetName, (string)null, "E2-① 类型不命中 ⇒ 资产名**保持 null**（🧨 type 比较写反 ⇒ 红）");
        MenuCheck.Check(s, tgtMiss.Enabled, false, "E2-② 类型不命中 ⇒ `Enabled == false`（原版循环走完那一句）");
        MenuCheck.Check(s, EventImageSelectorController.Initialize(null, assets), 0,
                        "E3 `drawDatas == null` ⇒ 返 0（原版 NRE 那道闸的等价：不抛）");

        // ---- W 组：`LiveopUIDrawer_EventAssetSprites`（原版 `LiveopUIDrawer_EventAssetSprites.cs`）----
        // 事件里只有 GameModeIcon 一格（= `DraftModeMenuDemo` 那两个实例的**同一配置**，见文件头）。
        var evt = new LiveOpsLocalEvent
        {
            EventId = "b109_e32_probe",
            Assets = new[]
            {
                new LiveOpsLocalAsset { Key = (int)LiveOpsAssetRef.GameModeIcon, SubObjectName = "B109_E32_Probe_Icon" },
            },
        };
        var c0 = new FakeTarget();
        var c1 = new FakeTarget();
        var cells = new List<LiveopUIDrawer_EventAssetSprites.EventAssetImages>
        {
            new LiveopUIDrawer_EventAssetSprites.EventAssetImages { EventAssetType = (int)LiveOpsAssetRef.GameModeIcon,    Image = c0 },
            new LiveopUIDrawer_EventAssetSprites.EventAssetImages { EventAssetType = (int)LiveOpsAssetRef.BackgroundImage, Image = c1 },
        };
        int drawn = LiveopUIDrawer_EventAssetSprites.Initialize(cells, evt);

        MenuCheck.Check(s, drawn, 0, "W1-① 图没导 ⇒ 外层返 **0**（原版 `LoadAsset` 返 null 的同一支）");
        MenuCheck.Check(s, c0.EnabledWrites + c1.EnabledWrites, 2,
                        "W1-② 外层遍历 **2** 格 ⇒ 两格各被写一次 `enabled`（🧨 提前 break / 只喂第一格 ⇒ 红）");
        MenuCheck.Check(s, c0.Enabled, true, "W1-③ 有资产那一格 ⇒ `enabled = true`（原版 ③ 那句）");
        MenuCheck.Check(s, c1.Enabled, false, "W1-④ 没资产那一格 ⇒ `enabled = false`（原版 ② `HasAsset` 那闸）");
        MenuCheck.Check(s, EventArt.HasAsset(evt, (int)LiveOpsAssetRef.GameModeIcon), true, "W2-① `HasAsset` 命中格 = true");
        MenuCheck.Check(s, EventArt.HasAsset(evt, (int)LiveOpsAssetRef.BackgroundImage), false, "W2-② `HasAsset` 未配格 = false");
    }
}
