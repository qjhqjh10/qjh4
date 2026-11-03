// B116W36_Assert.cs — 第一百一十六会话 · 执行代理 `B116_W36`（账 `B-334`）的断言
//
// ============================ 它盯的是什么 ============================
//  `B-334`：卡组编辑窗按 `Done` **保存成功后不关窗** —— 原版**保存成功就把窗关掉**。
//
//  判据（`d:/2/tools/decomp_full/`，2026-11-02 现读；`B116_W34` 已逐句核过）：
//    · `DeckEditingWindow__TrySaveDeck.c`：合法 ⇒ `:100 DeckEditingWindow__UploadDeck(param_1, 0)` + `:103 HidePopUp`；
//    · `DeckEditingWindow__UploadDeck.c`：`:63 CardDeck__CopyDeck` → `:74 PlayerDataManager__UploadUnsyncedDeck(…, 回调)`；
//    · `DeckEditingWindow.__c__DisplayClass43_0___UploadDeck_g__OnSave_0.c`：**末句**
//      `(**(code **)(*plVar1 + 0x1b8))(plVar1, …)` —— 虚槽 `0x1b8` = `GameWindow.Close()`
//      （同一虚槽在 `DeckEditingWindow__TryClose.c:15` 与 `…___ConfirmDiscard_b__45_1.c` 里也是「关窗」，
//       两处互证 ⇒ 读法本身不是猜的）。
//  ⇒ 我们的对应物 = `DeckRuntime.SaveAndSay()` **成功支**新加的那句 `BackToMenu()` —— 走的是**既有离场口**
//    （`HostWindow != null` ⇒ `LeaveByHostWindow()` 关宿主窗；独立场景 ⇒ `LoadScene`），⛔ 没另造一个「关窗口」。
//
// ============================ 本件测什么 / 不测什么 ============================
//  ✅ A 组：**真开一扇** `DeckEditingWindow`（= 被窗托管那条腿）、卡组合法，按 `Done` ⇒ 窗 `IsOpen()` 与
//           `activeSelf` **双双**为假 + `LeaveCount` +1 + 真的落盘。
//  ✅ B 组：`ESC` 走**同一个** `SaveAndSay()`（原版 `DeckEditingWindow__ESCPressed.c:5` 全文只有一句
//           `DeckEditingWindow__TrySaveDeck(this, 0)`）⇒ 同样关窗。
//  ❌ **不测「保存失败支关不关」**：原版失败支（`g__OnSave_0` 的 `param_2 == 0`）**也**关窗，但我们这一支
//     是**本地写盘失败**、而 `DeckLibrary` 是**窗内实例**（`DeckEditingWindow.Open()` → `Build(DeckLibrary.Load())`）
//     ⇒ 一关就**丢玩家的未保存改动**（原版那份改动由 `PlayerDataManager` 持有 + `syncedToServer = 0` 可重试）
//     ⇒ 那一支**已停手待调度台裁**（见报告），本件故意不钉它。
//     🔴 **2026-11-02 订正（`B-339` / `B116_W38` 之后）**：上一段的前提已**不成立** —— 库现在对「窗托管那条腿」
//     是**进程级**的（`DeckRuntime.SharedLibrary`），失败支关窗**不再丢改动**、且已实现。
//     那一条的断言在**本文件之外**的 `Editor/B116W38_Assert.cs`（与本件挂在同一物理行）；本件仍只钉成功支。
//  ❌ 不测文案：`Say("已保存")` / `Say("保存失败：" + …)` 一个字都没动。
//
// ============================ 🔴 存档隔离（`B-343` · 2026-11-02 `B116_W40` 补）============================
//  问题（`B116_W38` 查出）：本件原来**自己一个 override 都没设**，只靠「跑完把字节写回」兜底。
//    而它挂在 `Editor/DeckScene.cs` 的 `RunCore:1399`，那时 `:1151` 的 `finally` 早已把
//    `DeckStore.OverridePath` 还原成 `null` ⇒ `DeckStore.Path` = `persistentDataPath/WarpforgeDecks.json`
//    = **玩家的真存档**（`RestoreFile` 事后写回字节，但**中途真的落盘过一次**）⇒ 违反红线。
//  改法（照 `Editor/B116W38_Assert.cs` 的做法，**不依赖外层那道门**）：每组进来自己设
//    `DeckStore.OverridePath = MyGame/Temp/_b116w36_<a|b>.json` + `DeckRuntime.InvalidateSharedLibrary()`
//    （让库从空临时档重读），`finally` 里还原 override、作废夹具态那份库、删临时档（含 `SaveAll` 留下的 `.tmp`）。
//  灭自证：`CheckStoreIsolated` 三条前提 —— ① 读写口 == 那个临时文件 ② 它不在 `persistentDataPath` 下
//    ③ `DeckLibrary.ReadLegacyText == null`（批处理是 edit mode ⇒ 中央档钩子不注册 ⇒ 不经 `SaveCenter`）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A5 ★：**没按 `Done`** 时窗必须是**开着**的 —— 一个「Open 就关」/「恒关」的实现过不了这条
//     （而 A7/A8 那两条它照样绿 ⇒ 缺了 A5 就分不出两态）。
//   · A8 ★：关窗必须走 `BackToMenu`（`LeaveCount` +1）—— 另写一个只 `gameObject.SetActive(false)` 的关法 ⇒ 红。
//   · B5 ★：`ESC` 与 `Done` **同一个成功支** —— 只给 `Done` 加关窗、漏掉 `ESC` 那条路 ⇒ B5 红。
//   · A10：场景**不许换**（被窗托管那条腿 = 关窗不切场景；退回 `LoadScene` ⇒ 红）。
using System;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>B-334 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。
/// 接线归调度台：`B116W36_Assert.Run(); _sink.Pass += B116W36_Assert.Pass; _sink.Fail += B116W36_Assert.Fail;`。</summary>
public static class B116W36_Assert
{
    const string P = "[B116/W36] ";

    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B-334：卡组编辑窗 `Done` **保存成功 ⇒ 关窗**（原版 `TrySaveDeck → UploadDeck → OnSave → Close()`）");

        try { GroupA_DoneClosesWindow(); } catch (Exception e) { Crash("A `Done` ⇒ 关窗", e); }
        try { GroupB_EscClosesWindow(); } catch (Exception e) { Crash("B `ESC` ⇒ 关窗", e); }

        Summary();
    }

    // ==================================================================
    //  A 组：`Done` 保存成功 ⇒ 宿主窗关掉
    // ==================================================================
    static void GroupA_DoneClosesWindow()
    {
        Section("A `Done` 保存成功 ⇒ 宿主窗关掉（= 原版虚槽 `0x1b8` `GameWindow.Close()`）");

        string keepOverride = DeckStore.OverridePath;
        string tmpPath = TempPath("a");            // ⚠️ 两组各自一份：`_a` / `_b`
        var keepRet = CollectionData.PendingReturn;
        int keepEdit = CollectionData.PendingEditDeck;
        var keepNew = CollectionData.PendingNewDeck;
        DeckEditingWindow win = null;
        try
        {
            // 🔴 **B-343（2026-11-02 · `B116_W40`）：本组自建临时存档。**
            //   原来这里**一个字都没设** override，而 `Editor/DeckScene.cs:1151` 的 `finally` 早已把它还原成 `null`
            //   ⇒ 本组（挂在 `RunCore:1399`）读写的是 `persistentDataPath` 下的**玩家真存档**（`RestoreFile` 事后写回字节，
            //   但**中途真的落盘过一次**）—— 违反「自检一次都不许碰玩家真档」。
            DeckStore.OverridePath = tmpPath;
            DeckRuntime.InvalidateSharedLibrary();          // 从干净的一拍开始（别吃前面段落留在内存里的那份库）
            CheckStoreIsolated("A", tmpPath);

            win = OpenValidWindow("A");
            if (win == null || win.Runtime == null) return;
            var rt = win.Runtime;

            int leave0 = rt.LeaveCount;
            CheckTrue(win.IsOpen() && win.gameObject.activeSelf,
                      "★ A5 灭自证：**没按 `Done`** ⇒ 窗开着（`IsOpen()` 与 `activeSelf` 两条都查 ——"
                    + " 「一打开就关」/「恒关」的实现过不了这条，而 A7/A8 那两条它照样绿）");
            Check(rt.LeaveCount, leave0, "★ A5b 灭自证：……而且此刻**一次都没离场**（`LeaveCount` 没动）");
            string sceneBefore = SceneManager.GetActiveScene().name;   // ⚠️ 必须在按之前取 —— 按之后再读一次比，是**同义反复**（零鉴别力）

            rt.UiPressDone();

            CheckTrue(!win.IsOpen(),
                      "★★ A6 结论：**保存成功 ⇒ 宿主窗 `IsOpen()` 为假**（原版 `g__OnSave_0` 末句 = 虚槽 `0x1b8`；"
                    + "删掉 `DeckRuntime.SaveAndSay()` 成功支那句 `BackToMenu()` ⇒ 这条红）");
            CheckTrue(!win.gameObject.activeSelf,
                      "★★ A7 结论：……而且物体**不活**（`activeSelf == false`）—— 与上一条**并存**才算真关掉"
                    + "（只写 `CurrentState` 不改激活态、或反过来，都过不了这一对）");
            Check(rt.LeaveCount, leave0 + 1,
                      "★ A8 判别式：关窗走的是**既有离场口** `BackToMenu`（`LeaveCount` +1）——"
                    + " 另写一个只 `SetActive(false)` 的关法 ⇒ 这条红（其余几条照样绿 ⇒ 它是唯一能分出来的那条）");
            CheckTrue(!rt.DeckDirty,
                      "★ A9 而且**脏标记清了** —— 证明这一下真的走了成功支（「假装保存 + 关窗」不会有这个）");
            var reread = DeckLibrary.Load();
            Check(reread.Current != null ? reread.Current.CardIds.Count : -1,
                      rt.State.Deck != null ? rt.State.Deck.CardIds.Count : -1,
                      "★ A9b ……重读存档：盘上那副的张数与编辑器一致（保存**真的落盘了**，不是只关窗）");
            Check(SceneManager.GetActiveScene().name, sceneBefore,
                      "★ A10 场景**没换**（被窗托管那条腿 = 关窗、不切场景 —— 退回 `LoadScene(\"MainMenu\")` ⇒ 这条红）");
        }
        finally
        {
            if (win != null)
            {
                // ⚠️ 走 `CloseFromRuntime()`（绕开 `DeckEditingWindow.Close()` 的查脏闸）：自检收尾**不许**再弹一扇
                //    「丢弃改动?」，那扇弹窗会留在场上把后面整片断言挡住。已经关过的窗再 `Close()` 走首句守卫、零副作用。
                win.CloseFromRuntime();
                UnityEngine.Object.DestroyImmediate(win.gameObject);
            }
            CollectionData.PendingReturn = keepRet;
            CollectionData.PendingEditDeck = keepEdit;
            CollectionData.PendingNewDeck = keepNew;
            // 🔴 **B-343 收尾**：还原外层那份 override（本组加的那一层只活在本组内）+ 作废夹具态那份库 + 删临时档。
            DeckStore.OverridePath = keepOverride;
            DeckRuntime.InvalidateSharedLibrary();          // ⛔ 别把夹具态那份库漏给后面的段落
            try { if (System.IO.File.Exists(tmpPath)) System.IO.File.Delete(tmpPath); } catch { }
            try { if (System.IO.File.Exists(tmpPath + ".tmp")) System.IO.File.Delete(tmpPath + ".tmp"); } catch { }
        }
    }

    // ==================================================================
    //  B 组：`ESC` 走同一个 `SaveAndSay()` ⇒ 同样关窗
    // ==================================================================
    static void GroupB_EscClosesWindow()
    {
        Section("B `ESC` = 同一个 `TrySaveDeck` ⇒ 同样关窗（原版 `__ESCPressed.c:5` 全文只有一句 `TrySaveDeck`）");

        string keepOverride = DeckStore.OverridePath;
        string tmpPath = TempPath("b");            // ⚠️ B 组用另一份临时档（A 组收尾已删掉 `_a`）
        var keepRet = CollectionData.PendingReturn;
        int keepEdit = CollectionData.PendingEditDeck;
        var keepNew = CollectionData.PendingNewDeck;
        DeckEditingWindow win = null;
        try
        {
            // 🔴 **B-343**：与 A 组同一套 —— 本组自建临时存档，⛔ 不依赖外层的 override 门（它在 `:1151` 已被还原成 `null`）。
            DeckStore.OverridePath = tmpPath;
            DeckRuntime.InvalidateSharedLibrary();
            CheckStoreIsolated("B", tmpPath);

            win = OpenValidWindow("B");
            if (win == null || win.Runtime == null) return;
            var rt = win.Runtime;

            int leave0 = rt.LeaveCount;
            CheckTrue(win.IsOpen() && win.gameObject.activeSelf,
                      "★ B3 灭自证：**没按 `ESC`** ⇒ 窗开着（同 A5：恒关的实现过不了这条）");

            win.ESCPressed();          // = 窗自己的入口 → `DeckRuntime.EscPressed()` → `SaveAndSay()`

            CheckTrue(!win.IsOpen() && !win.gameObject.activeSelf,
                      "★★ B4 结论：**`ESC` 保存成功也关窗**（原版 `ESCPressed` → `TrySaveDeck` → `UploadDeck` → `OnSave` → `Close()`）"
                    + " —— 只给 `Done` 加关窗、漏掉这条同源入口 ⇒ 这条红");
            Check(rt.LeaveCount, leave0 + 1,
                      "★ B5 判别式：`ESC` 那条也走**同一个**离场口（`LeaveCount` +1，⛔ 不是两条各写一份关窗）");
            CheckTrue(!rt.DeckDirty, "★ B6 ……而且真的落盘了（脏标记清）");
        }
        finally
        {
            if (win != null)
            {
                win.CloseFromRuntime();
                UnityEngine.Object.DestroyImmediate(win.gameObject);
            }
            CollectionData.PendingReturn = keepRet;
            CollectionData.PendingEditDeck = keepEdit;
            CollectionData.PendingNewDeck = keepNew;
            // 🔴 **B-343 收尾**：还原外层那份 override（本组加的那一层只活在本组内）+ 作废夹具态那份库 + 删临时档。
            DeckStore.OverridePath = keepOverride;
            DeckRuntime.InvalidateSharedLibrary();          // ⛔ 别把夹具态那份库漏给后面的段落
            try { if (System.IO.File.Exists(tmpPath)) System.IO.File.Delete(tmpPath); } catch { }
            try { if (System.IO.File.Exists(tmpPath + ".tmp")) System.IO.File.Delete(tmpPath + ".tmp"); } catch { }
        }
    }

    // ==================================================================
    //  夹具 / 读数
    // ==================================================================
    /// <summary>开一扇**真的** `DeckEditingWindow`（= 被窗托管那条腿）并把卡组修成合法。
    /// ⚠️ 前提不成立时**照样返回**已开的窗（由调用方判 null），并**当场红** —— ⛔ 不用 `if (x != null) Check(...)`
    /// 那种「前提不成立就静默跳过」的写法。</summary>
    static DeckEditingWindow OpenValidWindow(string tag)
    {
        var wm = WindowsManager.EnsureHost();
        CheckTrue(wm != null, "（前提·" + tag + "）壳宿主 `WindowsManager.EnsureHost()` 建得出来");
        if (wm == null) return null;

        var win = DeckEditingWindow.OpenFrom(wm);
        CheckTrue(win != null && win.Runtime != null,
                  "（前提·" + tag + "）`DeckEditingWindow.OpenFrom` 真的开出一扇带 `DeckRuntime` 的窗");
        if (win == null || win.Runtime == null) return win;

        var rt = win.Runtime;
        CheckTrue(rt.HostWindow == win,
                  "（前提·" + tag + "）这台 `DeckRuntime` **有宿主窗**（= 被窗托管那条腿；⛔ 独立场景那条腿关的是场景）");

        MakeDeckValid(rt);
        Check(rt.State.Validate(), DeckError.None,
              "（前提·" + tag + "）这副牌**合法** ⇒ `Done`/`ESC` 会走**成功支**（不合法只会弹错误窗、不关窗，那是另一支）");
        return win;
    }

    /// <summary>把编辑器里那副牌补成合法满编（照 `Editor/DeckScene.cs` 既有夹具的写法：`PlayerDeckForDemo` + 三个字段拷）。
    /// 已合法 ⇒ 一个字都不改（⛔ 别平白改盘上那份）。⚠️ `B-343` 起盘上那份是本组的**临时档**（`TempPath()`），
    /// `finally` 直接删掉，不再需要「事后写回字节」。</summary>
    static void MakeDeckValid(DeckRuntime rt)
    {
        if (rt == null || rt.State == null || rt.State.Deck == null) return;
        if (rt.State.Validate() == DeckError.None) return;
        var demo = DeckRuntime.PlayerDeckForDemo(rt.State);
        if (demo == null) return;
        rt.State.Deck.WarlordId = demo.WarlordId;
        rt.State.Deck.DefensiveId = demo.DefensiveId;
        rt.State.Deck.CardIds.Clear();
        rt.State.Deck.CardIds.AddRange(demo.CardIds);
    }

    /// <summary>`Assets/../Temp/_b116w36_&lt;tag&gt;.json`（= `MyGame/Temp/` 下）—— 与 `Editor/DeckScene.cs` 的
    /// `TempStorePath()` / `Editor/B116W38_Assert.cs` 的 `TempPath()` **同一个落点**。
    /// 起手就把旧文件删掉（每次从空库开始），收尾由调用方的 `finally` 删。
    /// ⚠️ **不碰玩家的真存档**：整组只读写这一个临时文件。</summary>
    static string TempPath(string tag)
    {
        string p = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, "../Temp/_b116w36_" + tag + ".json"));
        try { Directory.CreateDirectory(System.IO.Path.GetDirectoryName(p)); } catch { }
        try { if (System.IO.File.Exists(p)) System.IO.File.Delete(p); } catch { }
        return p;
    }

    /// <summary>🔴 **B-343 的灭自证**：证明本组此刻**读写口确实落在那个临时文件上**、而且**不经过中央档**
    /// —— 三条任意一条红，都说明这组又开始摸玩家真档了。
    /// <para>判据 ③：`DeckLibrary` 的中央档注入钩子在 `DeckRuntime.RegisterDeckLibrarySaveHooks`
    /// （`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`）里注册；**批处理自检是 edit mode ⇒ 它不触发**
    /// （`CardPresentation/Deck/DeckRuntime.cs` 文件尾那段注释第 ② 条明写这件事）⇒ `DeckLibrary.Load()`
    /// 走 `DeckStore` 的纯文件路 = `DeckStore.Path` = 上面那个临时文件，⛔ 不会经 `SaveCenter` 摸到玩家的中央档。</para></summary>
    static void CheckStoreIsolated(string tag, string tmpPath)
    {
        string cur = DeckStore.Path;
        CheckTrue(string.Equals(cur, tmpPath, StringComparison.OrdinalIgnoreCase),
                  "（前提·" + tag + "·B-343）本组读写口 = **临时档**「" + tmpPath + "」"
                + "（`DeckStore.OverridePath` 由本组自己设、`finally` 自己还原 —— ⛔ 不依赖外层那道门）");
        string pdp = SaveRoot.Path;      // 🆕 2026-11-03 B128：玩家存档根 = `C:\warpforgesave`（原来比 `persistentDataPath`，改根后会**永远绿**）
        CheckTrue(string.IsNullOrEmpty(pdp) || !cur.StartsWith(pdp, StringComparison.OrdinalIgnoreCase),
                  "（前提·" + tag + "·B-343）……而且它**不在**玩家的存档根 `SaveRoot.Path`（" + pdp + "）下"
                + " —— 这条红 = 又开始动玩家真存档了（红线）");
        CheckTrue(DeckLibrary.ReadLegacyText == null,
                  "（前提·" + tag + "·B-343）`DeckLibrary` 的**中央档注入钩子没注册**"
                + "（`RuntimeInitializeOnLoadMethod` 在 edit mode 不触发）⇒ 读档只走 `DeckStore` 的纯文件路 = 上面那个临时文件");
    }

    // ==================================================================
    //  最小断言框架（独立类 ⇒ 不借用宿主的 private 助手）
    // ==================================================================
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = System.Collections.Generic.EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, ok ? msg : msg + "｜实测「" + actual + "」，期望「" + expected + "」");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("✓ " + msg); }
        else { _fail++; Debug.LogError("✗ " + msg); }
    }

    static void Crash(string seg, Exception e)
    {
        _fail++;
        Debug.LogError(P + "✗ 断言段「" + seg + "」抛异常（后面的条目没跑完）：" + e);
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log("B116_W36 合计：" + _pass + " 通过 / 0 失败");
        else Debug.LogError("B116_W36 合计：" + _pass + " 通过 / " + _fail + " 失败");
    }

    // 宿主聚合行读这两个口（追加到类尾、零行漂）。
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
