// B109_E35_DraftImage_Assert.cs — 执行代理 `B109_E35`（第一百零九会话）的断言集
//
// ============================ 它盯的是什么 ============================
// 判据 = `资料/完全复刻/判据/B109_R16_死码C45.md` 的 **C14 / C15 / C17 / C18** 四条（施工单 → `资料/完全复刻/报告/B109_E32.md` §2/§3）。
//
//  · C14 `EventImageSelectorController.Initialize(List<EventAsset>)`
//  · C15 `ImageEventDrawData.TryDraw(List<EventAsset>)`
//  · C17 `LiveopUIDrawer_EventAssetSprites.EventAssetImages.Initialize(ILiveOps)`
//  · C18 `LiveopUIDrawer_EventAssetSprites.Initialize(ILiveOps)`（外层遍历）
//
// 🔴 R16 把宿主判成 `ParentEventWindow` 是**错的**（`B109_E32` 2026-10-31 现核）：
//    · `EventImageSelectorController` 的唯一实例在 `bundle_menus_assets_all` 的
//      `Draft Mode Timed Mode Window/Draft Mode Pay State/Alliance Trophy Image controller`
//      （MB `-316068969454671126`：两格 `RankingIcon(6)` → 子件 `Frame` · `HeaderImage(3)` → 子件 `Badge`）；
//    · `LiveopUIDrawer_EventAssetSprites` 的两个实例（MB `-256574493845456187` Demo ·
//      `5714981760248277738` Timed）各一格 `GameModeIcon(12)`，挂各自的 `Game Mode Header With Back Button/
//      Header Background/Game Mode Icon`。
//    ⇒ 生产宿主 = 我们的 `Shell/DraftModeWindow.cs`（Demo 与 Timed 两族），原版调用点 =
//      `DraftModeMenuPayStateTimed.OnOpen` 末句 `eventUISpecialImageController.Initialize(drawData.EventAssets)`。
//    本文件盯的就是**那条生产接线真的走了**（不只是组件本身能算对 —— 那半由 `B109_E32_EventImage_Assert` 盯）。
//
// ============================ 怎么用（宿主）============================
//   `B109_E35_DraftImage_Assert.Run(_sink);`      // 需要 `CheckSink`
//   或单独跑：`-executeMethod B109_E35_DraftImage_Assert.Run`
//   ⚠️ 本批白名单不含任何宿主 ⇒ 今天**一条都不会跑**（如实标）。
//   自建探针：`WindowsManager.Get()` + `DraftModeWindow.Create(wm, seed)`（与 `DraftWindowAssertD2` 同一套建法），
//   跑完三扇窗全部 `DestroyImmediate` 并把四个静态通道还原。
//
// ============================ 🔴 灭自证（每组怎么才能红）============================
//   · **G1/G3 是「没有事件资产」那一档**：把 `HasAsset == false` 那一支写成「有格子就画上」⇒ G1-⑤/G3-④ 红。
//   · **G2 是 G3 的对照**：Timed 关时 `PayImageInitCalls` 必须是 **0**；把 Timed 闸拆掉 ⇒ G2 红。
//   · **G4 只加一份事件资产**（`GameModeIcon` + `RankingIcon`，**不含** `HeaderImage`）⇒
//     表头那格必须画上、`Badge` 那格必须仍关着；把「按 type 命中」写成「喂了就全画」⇒ G4-⑦ 红。
//   · **G4 的资产名**只落在 `RankingIcon` 那一格 ⇒ `LastPayImageAssetNames = [probe, null]`；
//     把两格的 type 写反（6 ↔ 3）⇒ G4-⑤ 与 G4-⑨ 同时红。
//   · G5 数的是**生产读口**（`HeaderImageInitCalls` / `PayImageInitCalls`），不是调用组件的返回值：
//     把接线整段删掉 ⇒ G1-① / G3-① 红。

using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>`B109_E35`：C14 / C15 / C17 / C18 在 `Shell/DraftModeWindow.cs` 的**生产接线**断言。</summary>
public static class B109_E35_DraftImage_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B109E35] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>探针资产名 —— `Resources/Art/ui_menu/40K_Chat_icon_Alliance_v2.png`（本机确实在工程里）。</summary>
    const string ProbeArt = "40K_Chat_icon_Alliance_v2";
    const int TestSeed = 20261031;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    /// <summary>独立跑（自带 sink，合计打到 Console）。</summary>
    public static void Run() { RunCore(_sink); }

    /// <summary>宿主跑（用宿主的 sink，合计进宿主读数）。</summary>
    public static void Run(CheckSink s) { RunCore(s ?? _sink); }

    static void RunCore(CheckSink s)
    {
        MenuCheck.Section(s, "B109_E35 · 轮抽窗四颗事件图（C14/C15/C17/C18）的生产接线");

        DraftModeWindow win = null;
        // ---- 现场保存（本段会动三个静态通道，跑完必须原样还回去）----
        bool prevTimed = TimedDraftLocal.Enabled;
        string[] prevArmies = TimedDraftLocal.Armies;
        var prevOverride = DraftModeWindow.EventAssetsOverrideForTest;

        try
        {
            var wm = WindowsManager.Get();
            MenuCheck.True(s, wm != null, "G0 **前提**：拿得到 `WindowsManager`（拿不到 ⇒ 下面全部空转｜⛔ 不静默跳过）");
            if (wm == null) return;

            MenuCheck.True(s, CardArt.MenuUi(ProbeArt) != null,
                           "G0 **前提**：探针图 `" + ProbeArt + "` 在 `Resources/Art/ui_menu/` 里（拿不到 ⇒ G4 那组画不上）");

            // ==================================================== G1/G2：生产口径（没有事件资产）+ Timed 关
            TimedDraftLocal.Enabled = false;
            DraftModeWindow.EventAssetsOverrideForTest = null;
            DraftModeWindow.ResetEventImageProbe();
            win = DraftModeWindow.Create(wm, TestSeed);
            MenuCheck.True(s, win != null, "G1 **前提**：`DraftModeWindow.Create` 建出窗来");
            if (win != null)
            {
                MenuCheck.Check(s, DraftModeWindow.HeaderImageInitCalls, 1,
                                "G1-① 表头那格的事件图 `Initialize` **被调了 1 次**（🧨 整段撤掉 ⇒ 0 ⇒ 红）");
                MenuCheck.Check(s, DraftModeWindow.LastHeaderImageCells, 1,
                                "G1-② 表头接 **1 格**（原版两个实例各一格；我们只有一颗表头 ⇒ Demo/Timed 共用一格）");
                MenuCheck.Check(s, DraftModeWindow.LastHeaderImageType, (int)LiveOpsAssetRef.GameModeIcon,
                                "G1-③ 那一格的 type = **GameModeIcon(12)**（原版 MB 逐字段实读）");
                var gm = FindPath(win.transform, "Game Mode Header With Back Button/Header Background/Game Mode Icon");
                MenuCheck.True(s, gm != null,
                               "G1-④ 树上有 `Game Mode Header With Back Button/Header Background/Game Mode Icon`（原版那条父链）");
                MenuCheck.True(s, gm != null && !gm.gameObject.activeSelf,
                               "G1-⑤ 本地没有事件资产 ⇒ 那一格**关着**（原版 `HasAsset == false` 的同一支；🧨 开局就亮 ⇒ 红）");
                MenuCheck.Check(s, DraftModeWindow.LastHeaderImageDrawn, 0,
                                "G1-⑥ 画上 **0** 格（本地没有 draft 事件资产 —— 原版同样关掉）");
                MenuCheck.Check(s, DraftModeWindow.PayImageInitCalls, 0,
                                "G2 Timed 关 ⇒ Pay 页那两格**根本不建**（`Initialize` 被调 0 次；🧨 把 Timed 闸拆掉 ⇒ 红）");
            }

            // ==================================================== G3：Timed 开、仍然没有事件资产
            DestroyWindow(ref win);
            TimedDraftLocal.EnableForDemo("Ultramarines", "Goff");
            DraftModeWindow.EventAssetsOverrideForTest = null;
            DraftModeWindow.ResetEventImageProbe();
            win = DraftModeWindow.Create(wm, TestSeed);
            MenuCheck.True(s, win != null, "G3 **前提**：Timed 族开着时窗也能建出来");
            if (win != null)
            {
                MenuCheck.Check(s, DraftModeWindow.PayImageInitCalls, 1,
                                "G3-① Timed Pay 页那格的 `Initialize` **被调了 1 次**（🧨 不接线 ⇒ 0 ⇒ 红）");
                MenuCheck.Check(s, Len(DraftModeWindow.LastPayImageTypes), 2, "G3-② 接了 **2 格**（原版那两格）");
                MenuCheck.Check(s, At(DraftModeWindow.LastPayImageTypes, 0), 6,
                                "G3-③ 第 1 格 type = **RankingIcon(6)**（原版 MB 实读；🧨 两格写反 ⇒ 红）");
                MenuCheck.Check(s, At(DraftModeWindow.LastPayImageTypes, 1), 3,
                                "G3-④ 第 2 格 type = **HeaderImage(3)**");
                var trophy = FindPath(win.transform, "Draft Mode Pay State/Alliance Trophy Image controller");
                MenuCheck.True(s, trophy != null,
                               "G3-⑤ 树上有 `Draft Mode Pay State/Alliance Trophy Image controller`（原版 Timed prefab 那颗）");
                var frame = FindPath(win.transform, "Draft Mode Pay State/Alliance Trophy Image controller/Frame");
                var badge = FindPath(win.transform, "Draft Mode Pay State/Alliance Trophy Image controller/Badge");
                MenuCheck.True(s, frame != null && badge != null,
                               "G3-⑥ 两个子件 `Frame` / `Badge` 都在（原版两格的落点）");
                MenuCheck.Check(s, DraftModeWindow.LastPayImageDrawn, 0, "G3-⑦ 没有事件资产 ⇒ 画上 **0** 格");
                MenuCheck.True(s, frame != null && !frame.gameObject.activeSelf && badge != null && !badge.gameObject.activeSelf,
                               "G3-⑧ 没有资产 ⇒ 两格**都关着**（原版 `enabled = false` 那支；🧨 有格就亮 ⇒ 红）");
            }

            // ==================================================== G4：注入一份事件资产（只给 12 与 6，**不给 3**）
            DestroyWindow(ref win);
            DraftModeWindow.EventAssetsOverrideForTest = new LiveOpsLocalEvent
            {
                EventId = "b109_e35_probe",
                Assets = new[]
                {
                    new LiveOpsLocalAsset { Key = (int)LiveOpsAssetRef.GameModeIcon, SubObjectName = ProbeArt },
                    new LiveOpsLocalAsset { Key = (int)LiveOpsAssetRef.RankingIcon,  SubObjectName = ProbeArt },
                },
            };
            DraftModeWindow.ResetEventImageProbe();
            win = DraftModeWindow.Create(wm, TestSeed);
            MenuCheck.True(s, win != null, "G4 **前提**：注入事件资产后窗也能建出来");
            if (win != null)
            {
                MenuCheck.Check(s, DraftModeWindow.LastHeaderImageDrawn, 1,
                                "G4-① 表头那格命中 `GameModeIcon` ⇒ 画上 **1** 格（🧨 空表当命中 ⇒ 红）");
                MenuCheck.True(s, DraftModeWindow.LastHeaderImageEnabled,
                               "G4-② 表头那格 `enabled = true`（原版 ③ 那句）");
                MenuCheck.Check(s, At(DraftModeWindow.LastPayImageTypes, 0), 6, "G4-③ 第 1 格 type 仍是 **RankingIcon(6)**");
                MenuCheck.Check(s, At(DraftModeWindow.LastPayImageTypes, 1), 3, "G4-④ 第 2 格 type 仍是 **HeaderImage(3)**");
                MenuCheck.Check(s, DraftModeWindow.LastPayImageDrawn, 1,
                                "G4-⑤ 只有 `RankingIcon` 那一格命中 ⇒ 画上 **1** 格（🧨 喂了就全画 ⇒ 2 ⇒ 红）");
                MenuCheck.Check(s, Len(DraftModeWindow.LastPayImageAssetNames), 2, "G4-⑥ 记下 2 格资产名");
                MenuCheck.Check(s, At(DraftModeWindow.LastPayImageAssetNames, 0), ProbeArt,
                                "G4-⑦ `Frame` 那格记下了资产名（按**名**取图那一跳真被调用）");
                MenuCheck.Check(s, At(DraftModeWindow.LastPayImageAssetNames, 1), null,
                                "G4-⑧ `Badge` 那格资产名保持 **null**（原版「没有该 type」那支；🧨 type 写反 ⇒ 两格同时翻 ⇒ 红）");
                var frame = FindPath(win.transform, "Draft Mode Pay State/Alliance Trophy Image controller/Frame");
                var badge = FindPath(win.transform, "Draft Mode Pay State/Alliance Trophy Image controller/Badge");
                MenuCheck.True(s, frame != null && frame.gameObject.activeSelf,
                               "G4-⑨ `Frame`（RankingIcon 那一格）真画上了（`activeSelf = true`）");
                MenuCheck.True(s, badge != null && !badge.gameObject.activeSelf,
                               "G4-⑩ `Badge`（HeaderImage 那一格）仍关着（原版「没有该 type ⇒ enabled = false」）");
                var gm = FindPath(win.transform, "Game Mode Header With Back Button/Header Background/Game Mode Icon");
                MenuCheck.True(s, gm != null && gm.gameObject.activeSelf,
                               "G4-⑪ 表头 `Game Mode Icon` 真画上了（`activeSelf = true`）");
            }

            // ==================================================== G5：读口是「生产路径」的读数，不是组件自己回的值
            MenuCheck.Check(s, DraftModeWindow.HeaderImageInitCalls + DraftModeWindow.PayImageInitCalls, 2,
                            "G5 一次建窗 = 表头 1 次 + Timed Pay 1 次（两个读口都由**生产接线**写；🧨 只在断言里调组件 ⇒ 0 ⇒ 红）");
        }
        catch (System.Exception e)
        {
            MenuCheck.True(s, false, "断言段抛异常（后面的条目没跑完）：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            DestroyWindow(ref win);
            DraftModeWindow.EventAssetsOverrideForTest = prevOverride;
            TimedDraftLocal.Enabled = prevTimed;
            if (prevArmies != null) TimedDraftLocal.Armies = prevArmies;
            DraftModeWindow.ResetEventImageProbe();
            DraftModeWindow.ClearRunForTest();                 // 别把自检那一局留给下一次真 Play
            WindowsManager.ClearReuseCacheForTest();
            DraftPacks.InvalidateCache();
            Debug.Log(s.P + "B109E35 合计：" + s.Pass + " 通过 / " + s.Fail + " 失败");
        }
    }

    /// <summary>按节点路径找一件（`Transform.Find` 只认**直接子件**，路径要写全）。</summary>
    static Transform FindPath(Transform root, string path)
    {
        return root == null ? null : root.Find(path);
    }

    // 🔴 逐格读数组的两把尺子：`MenuCheck.Check` 用 `EqualityComparer<T>.Default`，
    //    **比两个 `int[]` / `string[]` 会退化成引用相等**（永远红）⇒ 一律逐格断。
    /// <summary>`int[]` 的长度（`null` ⇒ 0）。</summary>
    static int Len(int[] a) { return a == null ? 0 : a.Length; }
    /// <summary>`string[]` 的长度（`null` ⇒ 0）。</summary>
    static int Len(string[] a) { return a == null ? 0 : a.Length; }
    /// <summary>取第 `i` 格（越界 ⇒ `-999`，与任何合法 type 都不相等）。</summary>
    static int At(int[] a, int i) { return a != null && i >= 0 && i < a.Length ? a[i] : -999; }
    /// <summary>取第 `i` 格（越界 ⇒ `"&lt;缺&gt;"`，与 `null` 不等 ⇒ 「数组根本没建」会红）。</summary>
    static string At(string[] a, int i) { return a != null && i >= 0 && i < a.Length ? a[i] : "<缺>"; }

    static void DestroyWindow(ref DraftModeWindow win)
    {
        if (win == null) return;
        // 先走 `Close()`（= 原版那次关窗：还原资源条 overlay / 放掉立绘 / 通知 `WindowsManager`），
        // 再 `DestroyImmediate` —— 直接销毁会在 `openWindows` 里留一具「已销毁但还登记着」的尸体
        // （下一次 `OpenWindow` 的 `ReapClosedWindows` 会去碰它 ⇒ `MissingReferenceException`）。
        try { win.Close(); } catch { }
        try { Object.DestroyImmediate(win.gameObject); } catch { }
        win = null;
    }
}
