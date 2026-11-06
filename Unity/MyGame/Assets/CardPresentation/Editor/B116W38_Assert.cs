// B116W38_Assert.cs — 第一百一十六会话 · 执行代理 `B116_W38`（账 `B-339`）的断言
//
// ============================ 它盯的是什么 ============================
//  `B-339`：**把卡组库做成跨窗存活**，好让 `B-334` 的「保存失败也关窗」能对齐原版。
//
//  判据（`d:/2/tools/decomp_full/`，2026-11-02 现读）：
//    · `DeckEditingWindow.__c__DisplayClass43_0___UploadDeck_g__OnSave_0.c`：
//      `if (param_2 != '\0') { … InventoryManager__Add … }` —— 这个 `if` **只包着「发通配符」**；
//      紧接着那三行在 `if` 之外：`plVar1 = …; (**(code **)(*plVar1 + 0x1b8))(plVar1, …);` =
//      虚槽 `0x1b8` = `GameWindow.Close()` ⇒ **`param_2 == 0`（上传失败）同样关窗**。
//    · 原版那份卡组列表由 `PlayerDataManager` 持有（`CardDeck.syncedToServer = 0` 可重试）
//      ⇒ 关窗**不会**丢玩家的改动：改动在内存那份列表里。
//  ⇒ 我们的对应物 = `DeckRuntime.SharedLibrary`（进程级实例，`DeckRuntime.Build` 里
//    `HostWindow != null` 那一支用它）+ `SaveAndSay()` 失败支新加的 `HideDeckPopUp(); BackToMenu();`。
//
// ============================ 本件测什么 / 不测什么 ============================
//  ✅ A 组：**保存失败支** ⇒ 窗关了 · `LeaveCount` +1 · 脏标记仍真 · **库里的改动没丢** ·
//            **重新 `Load()` 读不回来**（灭自证：证明读到的是内存那份，不是「反正盘上有」）·
//            盘上那份逐字节没变 · **重开窗还是同一份库、改动还在**（= 跨窗存活）。
//  ✅ B 组：**保存成功支** ⇒ 窗关了 · 库/盘都是新内容 · **成功后没有被「冤枉地重读」**（同一实例）·
//            重开窗还是同一份库（否则 `NoteSaveSucceeded` 那一跳丢了 ⇒ B5 红）。
//  ❌ 不测文案（`Say("保存失败：" + …)` 一个字没动）· ❌ 不测 `CollectionData` 自己那份缓存（那有它自己的作废口）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1 ★：托管腿若不是进程级那份库（退回 `Build(DeckLibrary.Load())`）⇒ **这条红、A6/A9/A15 全是假绿**。
//   · A3/A4 ★：**按 `Done` 之前**窗开着 + 库里还是旧名 —— 「一按就关」/「改动早就在库里」的实现过不了这一对。
//   · A11 ★★：**重新 `Load()` 读不到那个改动** —— 库若还是「每次开窗重读盘」，A9 会跟着假绿。
//   · A13/A15 ★★：关窗后（且恢复存档路径后）**同一份实例**、且**重开窗看到那份改动** ——
//     这才是「库跨窗存活」的结论；「每次开窗重读盘」的实现 A15 照样能过，但 **A13 与 A9 会一起红**。
//   · B5 ★★：成功后**不重读** —— 删掉 `CommitDeck` 里那句 `NoteSaveSucceeded(Library)` ⇒ 指纹过期 ⇒
//     下一次访问重读 ⇒ 拿到的是**另一个实例** ⇒ 这条红（而 B4 照样绿 ⇒ 它是唯一能分出来的那条）。
using System;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>B-339 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。
/// 接线（`Editor/DeckScene.cs`，与 `B116W36_Assert` **同一物理行**）：`B116W38_Assert.Run(); _sink.Pass += …`。</summary>
public static class B116W38_Assert
{
    const string P = "[B116/W38] ";

    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B-339：卡组库**跨窗存活** ⇒ 保存失败支也能关窗（原版 `g__OnSave_0` 的 `param_2 == 0` 同样 `Close()`）");

        try { GroupA_FailedSaveClosesAndKeeps(); } catch (Exception e) { Crash("A 保存失败支 ⇒ 关窗 + 库不丢", e); }
        try { GroupB_SuccessKeepsSameLibrary(); } catch (Exception e) { Crash("B 保存成功 ⇒ 库仍同一份", e); }

        Summary();
    }

    // ==================================================================
    //  A 组：保存**失败** ⇒ 关窗，而库内容没丢（跨窗存活）
    // ==================================================================
    static void GroupA_FailedSaveClosesAndKeeps()
    {
        Section("A 保存失败支：**窗关了**、库里的改动**还在**（= 原版 `PlayerDataManager` 持有那份列表）");

        string keepOverride = DeckStore.OverridePath;
        var keepRet = CollectionData.PendingReturn;
        int keepEdit = CollectionData.PendingEditDeck;
        var keepNew = CollectionData.PendingNewDeck;
        string tmpPath = TempPath("a");
        string keepSaveRoot = SaveCenter.OverrideRoot;   // 🆕 2026-11-06（B150）：中央档改道存档-还原
        string marker = "W38·未落盘·" + Guid.NewGuid().ToString("N").Substring(0, 6);
        DeckEditingWindow win = null, win2 = null;
        try
        {
            DeckStore.OverridePath = tmpPath;
            try { if (System.IO.Directory.Exists(tmpPath + ".central")) System.IO.Directory.Delete(tmpPath + ".central", true); } catch { }
            SaveCenter.OverrideRoot = tmpPath + ".central";   // 🆕 2026-11-06（B150）：中央档同改道（Play 下不写玩家真档；A12 的字节快照量就是这一份）
            SeedOneDeck();                                   // 临时库里先放一副合法满编（下面 `Validate` 才过）
            DeckRuntime.InvalidateSharedLibrary();           // 从干净的一拍开始（别吃上一段留下的库）

            win = OpenWindow("A");
            if (win == null || win.Runtime == null) return;
            var rt = win.Runtime;
            var lib = rt.Library;
            CheckTrue(lib != null && lib.Current != null,
                      "（前提·A）库里有一副可编辑的卡组（`Library.Current != null`）");
            if (lib == null || lib.Current == null) return;
            Check(rt.State.Validate(), DeckError.None,
                  "（前提·A）这副牌**合法** ⇒ `Done` 会走到 `CommitDeck`（不合法只会弹错误窗，那是另一支）");

            CheckTrue(ReferenceEquals(lib, DeckRuntime.SharedLibrary),
                      "★ A1 前提：**托管腿用的是进程级那份库**（`DeckRuntime.SharedLibrary`）——"
                    + " 库要是还随窗生灭（`Build(DeckLibrary.Load())`）⇒ 这条红，而下面 A6/A9/A15 会全是假绿");

            // ---- 改动：**只改内存**（还没 `Done`）----
            string oldName = lib.Current.Name;
            rt.State.Deck.Name = marker;
            rt.MarkDeckDirty();
            int leave0 = rt.LeaveCount;
            CheckTrue(win.IsOpen() && win.gameObject.activeSelf,
                      "★ A3 灭自证：**没按 `Done`** ⇒ 窗开着（「一按就关」/「恒关」的实现过不了这条）");
            CheckTrue(!string.Equals(lib.Current.Name, marker, StringComparison.Ordinal),
                      "★ A4 灭自证：此刻**库里还是旧名字**「" + oldName + "」—— 改动只在编辑副本里"
                    + "（否则下面「库里有改动」分不清是这一次写进去的，还是本来就在）");

            byte[] beforeBytes = ReadBytes(SaveCenter.PathOf(SaveKeys.PlayerDecks));          // 失败之前盘上那份（下面 A12 逐字节比）

            // ---- 让落盘必然失败：换到「父目录不存在」的路径（同 `Editor/DeckScene.cs` 的 A398 手法）----
            string dir = System.IO.Path.GetDirectoryName(tmpPath);
            string badPath = System.IO.Path.Combine(dir, "__wf_b116w38_no_such_dir__", "x.json");
            CheckTrue(!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(badPath)),
                      "（前提·A）坏路径的父目录**不存在** ⇒ 这一次落盘必然失败（写得进去 ⇒ 下面全是假绿）");
            DeckStore.OverridePath = badPath;
            // 🆕 2026-11-06（B150）：落盘开着时产品**只写中央档** ⇒ 光换个写不进的旧档路径不再等于「保存失败」。
            //   把中央档写口也指到「必然失败」的一处：`blocker` 是个**文件**，`Directory.CreateDirectory(blocker)` 必抛。
            string blocker = System.IO.Path.Combine(dir, "__wf_b116w38_blocker_file__"); System.IO.File.WriteAllText(blocker, "x");
            SaveCenter.OverrideRoot = System.IO.Path.Combine(blocker, "central");

            rt.UiPressDone();          // = `Done` 钮 → `SaveAndSay()` → 校验过 → `CommitDeck()` **失败**

            // ---- 结论 ①：窗关了，而且走的是既有离场口 ----
            CheckTrue(!win.IsOpen() && !win.gameObject.activeSelf,
                      "★★ A6 结论：**保存失败支也关窗**（原版 `g__OnSave_0` 那句 `Close()` 在 `if (param_2 != 0)` **之外**）"
                    + " —— 把 `SaveAndSay()` 失败支里那句 `HideDeckPopUp(); BackToMenu();` 删掉 ⇒ 这条红");
            Check(rt.LeaveCount, leave0 + 1,
                      "★ A7 判别式：失败支走的是**既有离场口** `BackToMenu`（`LeaveCount` +1）——"
                    + " 另写一个只 `SetActive(false)` 的关法 ⇒ 红");
            CheckTrue(rt.DeckDirty,
                      "★ A8 ……而且**脏标记仍为真**（= 没写进盘、下次 `Done` 还会再试；⛔ 别在这一支里清脏 —— 那是「假装存过」）");

            // ---- 结论 ②：库里的改动没丢（这就是失败支敢关窗的前提）----
            CheckTrue(string.Equals(lib.Current.Name, marker, StringComparison.Ordinal),
                      "★★ A9 结论：**库里的内容没丢**（落盘失败，但内存那份已带着本次改动「" + marker + "」）");
            CheckTrue(lib.LastError != null,
                      "★ A10 ……而失败原因**说得出话**（`Library.LastError` 非空；⛔ 不静默失败）");

            // ---- 灭自证：盘上那份**读不回来**这个改动 ----
            //   ⚠️ 只是把 `OverridePath` 指回我们那份临时存档（失败那一下写的是**坏路径**，临时存档没被动过）——
            //      这时**不许**碰 `DeckRuntime.SharedLibrary`：路径一换指纹就变、会触发重读，把要验的内存态冲掉。
            DeckStore.OverridePath = tmpPath;
            SaveCenter.OverrideRoot = tmpPath + ".central";   // 🆕 2026-11-06（B150）：读口恢复回「成功那一侧」（A12 要比的是那一次真写出来的字节）
            var fresh = DeckLibrary.Load();
            CheckTrue(fresh.Current == null || !string.Equals(fresh.Current.Name, marker, StringComparison.Ordinal),
                      "★★ A11 灭自证：**重新 `Load()` 读不到这个改动** ⇒ A9 读的是**内存那份库**，不是「反正盘上有」"
                    + "（库若是「每次开窗重读盘」的实现 ⇒ 这条与 A9 一起红）");
            CheckTrue(BytesEqual(beforeBytes, ReadBytes(SaveCenter.PathOf(SaveKeys.PlayerDecks))),
                      "★ A12 灭自证：盘上那份**逐字节没变**（失败就是失败，没有半写进去）");

            // ---- 结论 ③：**跨窗存活** —— 还是同一份库，重开窗还能看到那份改动 ----
            CheckTrue(ReferenceEquals(DeckRuntime.SharedLibrary, lib),
                      "★★ A13 结论：窗关掉之后**还是同一份库**（`SharedLibrary` 没有因为关窗而重读/重建）");
            win2 = OpenWindow("A2");
            if (win2 == null || win2.Runtime == null) return;
            CheckTrue(ReferenceEquals(win2.Runtime.Library, lib),
                      "★★ A14 结论：**重开的那扇窗用的是同一份库**（⛔ 不是重新 `Load()` 出来的一份）");
            CheckTrue(win2.Runtime.State != null && win2.Runtime.State.Deck != null
                      && string.Equals(win2.Runtime.State.Deck.Name, marker, StringComparison.Ordinal),
                      "★★ A15 结论：**重开窗之后，编辑器里就是刚才那份没落盘的改动**「" + marker + "」"
                    + " —— 这一条才是「失败也关窗」敢做的前提（玩家下次打开还能再按一次 `Done` 重试）");
        }
        finally
        {
            if (win2 != null) { try { win2.CloseFromRuntime(); UnityEngine.Object.DestroyImmediate(win2.gameObject); } catch { } }
            if (win != null) { try { win.CloseFromRuntime(); UnityEngine.Object.DestroyImmediate(win.gameObject); } catch { } }
            CollectionData.PendingReturn = keepRet;
            CollectionData.PendingEditDeck = keepEdit;
            CollectionData.PendingNewDeck = keepNew;
            DeckStore.OverridePath = keepOverride;
            SaveCenter.OverrideRoot = keepSaveRoot;   // 🆕 2026-11-06（B150）：中央档改道存档-还原（⛔ 别写死 null）
            try { if (System.IO.Directory.Exists(tmpPath + ".central")) System.IO.Directory.Delete(tmpPath + ".central", true); } catch { }
            try { string blk = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(tmpPath), "__wf_b116w38_blocker_file__"); if (System.IO.File.Exists(blk)) System.IO.File.Delete(blk); } catch { }
            DeckRuntime.InvalidateSharedLibrary();          // ⛔ 别把夹具态那份库漏给后面的段落
            try { if (System.IO.File.Exists(tmpPath)) System.IO.File.Delete(tmpPath); } catch { }
        }
    }

    // ==================================================================
    //  B 组：保存**成功** ⇒ 库仍是同一份（成功后不许被冤枉地重读）
    // ==================================================================
    static void GroupB_SuccessKeepsSameLibrary()
    {
        Section("B 保存成功支：库**仍是同一份实例**（`NoteSaveSucceeded` 刷新指纹），重开窗内容还在");

        string keepOverride = DeckStore.OverridePath;
        var keepRet = CollectionData.PendingReturn;
        int keepEdit = CollectionData.PendingEditDeck;
        var keepNew = CollectionData.PendingNewDeck;
        string tmpPath = TempPath("b");
        string keepSaveRootB = SaveCenter.OverrideRoot;   // 🆕 2026-11-06（B150）：中央档改道存档-还原（不注入 ⇒ Play 下读到的会是玩家真卡组）
        string marker = "W38·已落盘·" + Guid.NewGuid().ToString("N").Substring(0, 6);
        DeckEditingWindow win = null, win2 = null;
        try
        {
            DeckStore.OverridePath = tmpPath;
            try { if (System.IO.Directory.Exists(tmpPath + ".central")) System.IO.Directory.Delete(tmpPath + ".central", true); } catch { }
            SaveCenter.OverrideRoot = tmpPath + ".central";   // 🆕 2026-11-06（B150）：中央档同改道（Play 下不写玩家真档；下面 `Load()` 读的也是这一份）
            SeedOneDeck();
            DeckRuntime.InvalidateSharedLibrary();

            win = OpenWindow("B");
            if (win == null || win.Runtime == null) return;
            var rt = win.Runtime;
            var lib = rt.Library;
            CheckTrue(lib != null && lib.Current != null, "（前提·B）库里有一副可编辑的卡组");
            if (lib == null || lib.Current == null) return;
            Check(rt.State.Validate(), DeckError.None, "（前提·B）这副牌合法 ⇒ `Done` 走成功支");
            CheckTrue(ReferenceEquals(lib, DeckRuntime.SharedLibrary), "（前提·B）托管腿用的是进程级那份库");

            string oldName = lib.Current.Name;
            rt.State.Deck.Name = marker;
            rt.MarkDeckDirty();
            CheckTrue(!string.Equals(lib.Current.Name, marker, StringComparison.Ordinal),
                      "★ B2 灭自证：按之前库里还是旧名字「" + oldName + "」");

            rt.UiPressDone();

            CheckTrue(!win.IsOpen() && !win.gameObject.activeSelf,
                      "★ B3 结论：保存成功 ⇒ 关窗（`B-334` 那一条，本件不重钉；这里只当 B4/B5 的前提）");
            CheckTrue(string.Equals(lib.Current.Name, marker, StringComparison.Ordinal),
                      "★ B4 结论：库内容 = 新名字「" + marker + "」");
            DeckStore.OverridePath = tmpPath;      // （本来就是它；显式写一遍，免得读者以为下面读的是玩家真存档）
            var fresh = DeckLibrary.Load();
            CheckTrue(fresh.Current != null && string.Equals(fresh.Current.Name, marker, StringComparison.Ordinal),
                      "★ B4b ……而且**真的落了盘**（重新 `Load()` 读到同一个新名字）");
            CheckTrue(ReferenceEquals(DeckRuntime.SharedLibrary, lib),
                      "★★ B5 灭自证：写成功之后**没有被动重读**（还是同一份实例）—— 删掉 `CommitDeck` 里那句"
                    + " `NoteSaveSucceeded(Library)` ⇒ 指纹过期 ⇒ 下一次访问重读 ⇒ 拿到**另一个实例** ⇒ 这条红"
                    + "（而 B4/B4b 照样绿 ⇒ 它是唯一能分出来的那条）");

            win2 = OpenWindow("B2");
            if (win2 == null || win2.Runtime == null) return;
            CheckTrue(ReferenceEquals(win2.Runtime.Library, lib),
                      "★ B6 结论：重开窗用的是**同一份库**（跨窗存活）");
            CheckTrue(win2.Runtime.State != null && win2.Runtime.State.Deck != null
                      && string.Equals(win2.Runtime.State.Deck.Name, marker, StringComparison.Ordinal),
                      "★ B6b ……而且重开之后编辑器里看到的就是「" + marker + "」（关窗没把它弄丢）");
        }
        finally
        {
            if (win2 != null) { try { win2.CloseFromRuntime(); UnityEngine.Object.DestroyImmediate(win2.gameObject); } catch { } }
            if (win != null) { try { win.CloseFromRuntime(); UnityEngine.Object.DestroyImmediate(win.gameObject); } catch { } }
            CollectionData.PendingReturn = keepRet;
            CollectionData.PendingEditDeck = keepEdit;
            CollectionData.PendingNewDeck = keepNew;
            DeckStore.OverridePath = keepOverride;
            SaveCenter.OverrideRoot = keepSaveRootB;   // 🆕 2026-11-06（B150）：中央档改道存档-还原
            try { if (System.IO.Directory.Exists(tmpPath + ".central")) System.IO.Directory.Delete(tmpPath + ".central", true); } catch { }
            DeckRuntime.InvalidateSharedLibrary();
            try { if (System.IO.File.Exists(tmpPath)) System.IO.File.Delete(tmpPath); } catch { }
        }
    }

    // ==================================================================
    //  夹具
    // ==================================================================
    /// <summary>`Assets/../Temp/_b116w38_&lt;tag&gt;.json` —— 与 `Editor/DeckScene.cs` 的 `TempStorePath()` 同一个落点
    /// （⚠️ **不碰玩家的真存档**：全程只读写这个临时文件，收尾删掉）。</summary>
    static string TempPath(string tag)
    {
        string p = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, "../Temp/_b116w38_" + tag + ".json"));
        try { Directory.CreateDirectory(System.IO.Path.GetDirectoryName(p)); } catch { }
        try { if (System.IO.File.Exists(p)) System.IO.File.Delete(p); } catch { }
        return p;
    }

    /// <summary>往当前（临时）存档里放一副**合法满编**的演示卡组，并把它设为当前那套。
    /// 判据 = `DeckRuntime.PlayerDeckForDemo`（`Editor/DeckScene.cs:1010` 那条既有夹具同源：「演示卡组 = 合法满编 30 张」）。</summary>
    static void SeedOneDeck()
    {
        var seedState = new DeckEditorState(CardDatabase.Load());
        var demo = DeckRuntime.PlayerDeckForDemo(seedState);
        if (demo == null) { CheckTrue(false, "（前提）`DeckRuntime.PlayerDeckForDemo` 造得出一副演示卡组"); return; }
        demo.Name = "W38·夹具";
        var lib0 = DeckLibrary.Load();                   // 临时文件刚删过 ⇒ 空库
        CheckTrue(lib0.Count == 0, "（前提）临时存档起手是空的（不是玩家真存档）");
        CheckTrue(lib0.Insert(demo), "（前提）夹具卡组**入库并落盘**成功");
        Check(lib0.CurrentIndex, 0, "（前提）入库之后它就是「当前那套」（`Insert` 的口径）");
    }

    /// <summary>开一扇**真的** `DeckEditingWindow`（= 被窗托管那条腿）。
    /// ⚠️ 前提不成立时**照样红**（⛔ 不用 `if (x != null) Check(...)` 那种「前提不成立就静默跳过」的写法）。</summary>
    static DeckEditingWindow OpenWindow(string tag)
    {
        // ⚠️ 两个交接口都清掉：`DeckRuntime.Build` 开局会读它们 —— 留着会把「当前那套」换成别的
        CollectionData.PendingEditDeck = -1;
        CollectionData.PendingNewDeck = null;

        var wm = WindowsManager.EnsureHost();
        CheckTrue(wm != null, "（前提·" + tag + "）壳宿主 `WindowsManager.EnsureHost()` 建得出来");
        if (wm == null) return null;

        var win = DeckEditingWindow.OpenFrom(wm);
        CheckTrue(win != null && win.Runtime != null,
                  "（前提·" + tag + "）`DeckEditingWindow.OpenFrom` 真的开出一扇带 `DeckRuntime` 的窗");
        if (win == null || win.Runtime == null) return win;
        CheckTrue(win.Runtime.HostWindow == win,
                  "（前提·" + tag + "）这台 `DeckRuntime` **有宿主窗**（= 被窗托管那条腿，才会走进程级那份库）");
        return win;
    }

    /// <summary>读文件字节（不存在 ⇒ `null`）。</summary>
    static byte[] ReadBytes(string path)
    {
        try { return System.IO.File.Exists(path) ? System.IO.File.ReadAllBytes(path) : null; } catch { return null; }
    }

    /// <summary>逐字节比两份存档（都为 `null` 也算相等 —— 「都不存在」是同一个状态）。</summary>
    static bool BytesEqual(byte[] a, byte[] b)
    {
        if (a == null || b == null) return a == b;
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
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
        if (_fail == 0) Debug.Log("B116_W38 合计：" + _pass + " 通过 / 0 失败");
        else Debug.LogError("B116_W38 合计：" + _pass + " 通过 / " + _fail + " 失败");
    }

    // 宿主聚合行读这两个口（追加到类尾、零行漂）。
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
