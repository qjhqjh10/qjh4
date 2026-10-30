// B104_P4_Assert.cs — 第一百零四会话 · 执行代理 `B104_P4`（2026-10-31 · 「名字当 id / 选中失败静默」这一族）
//
// ============================ 它盯的是什么 ============================
// 本批修的四件（判据 / 改动逐条 → `资料/完全复刻/报告/B104_P4.md`）：
//   P4-1 `Shell/LiveOpsEventWindow.cs` 的「选卡组」回调：**按名回查下标** ⇒ 改**用 `DeckPick.OwnIndex`**。
//   P4-2 `Shell/PracticeModePopup.cs` 同上。
//   P4-3 活动卡组槽：**按卡组名**持久化 + 开窗 `IndexOf(name)` ⇒ 改**按卡组 id**（原版 `PlayEventSaveGame.defaultDeckId`）。
//   P4-4 拖拽重排：整库转成**名字数组**再按名匹配 ⇒ 改**按对象身份**（原版 `DeckInventory.UpdateOrder(List<CardDeck>)` 比 `deckId`）。
//   P5-3 `Shell/CollectionWindow.SelectDeck`：选中失败只 `LogWarning` 仍重建+开详情 ⇒ 改**出声 + 回滚高亮 + 不开窗**。
//   P5-4 `Shell/PracticeModePopup.StartBotBattle`：选中失败照常开战 ⇒ 改**挡住开战 + 出声**。
//
// ============================ 两条入口（⚠️ 接线由调度台做，本件不动宿主）============================
//   · `B104_P4_Assert.Run();`            —— **不需要 UI**（只在 `DeckLibrary` / `DeckStore` 上跑）。
//   · `B104_P4_Assert.RunUi(wm);`        —— 需要一具 `WindowsManager`（建窗口那几条）；`wm` 传 null 会**先红一条前提**。
//   ⚠️ 两者都会**自己收尾**：`DeckStore.OverridePath` / `CollectionData` 缓存 / `CollectionWindow.LastOpened`
//      全部还原，建出来的窗口自己 `DestroyImmediate` ⇒ 不会留在宿主里。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   · `DeckLibrary.UpdateOrder(IList<PlayerDeck>)` 改成「先把对象转成名字再比名」 ⇒ **A3 / B3** 红
//     （两副卡组同名，按名根本换不动位置）。
//   · `PlayerDeck.DeckId` 没真的进存档（例如只在内存里发、没落盘 / `JsonUtility` 读不回来）⇒ **A2** 红。
//   · `IndexOfDeckId` 改成按名 ⇒ **A1** 红（两副同名会指到同一个下标）。
//   · `LiveOpsEventWindow.OpenDeckSelection` 回调退回 `CollectionData.IndexOf(pick.Info.Name)` ⇒ **B2** 红（落到第 0 套）。
//   · `PracticeModePopup.OpenDeckSelection` 同上 ⇒ **B1** 红。
//   · 活动卡组槽退回存名字（`RememberEventDeck` 写 `raw.Name`）⇒ **B2** 的「槽里存的是 id」那条红。
//   · `CollectionWindow.MoveDeckInOrder` 退回「转名字数组」⇒ **B3** 红。
//   · `CollectionWindow.SelectDeck` 删掉 `RestoreSelection` / 删掉 `return` ⇒ **B4** 红。
//   · `PracticeModePopup.StartBotBattle` 删掉那个 `return` ⇒ **B5** 红。
//
// ============================ 边界（如实记）============================
//   · **不验**「原版窗口长什么样 / 排序后的视觉顺序」—— 那要真机（`资料/真Play待验清单.md`）。
//   · **不验**「旧存档（只有名字、没有 id）升级后一定能认回原来那副」—— 同名时按名回落**本来就有歧义**，
//     代码里是**出声 + 下次写入升级成 id**（`EventDeckIndex` 的注释），那一条只断「旧档不会丢记录」。
using System;
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B104_P4`：「名字当 id / 选中失败静默」这一族的结构断言。
/// <para>⛔ 期望值全是字面量 / 夹具自己摆的两个值，不从被测实现里反算。</para></summary>
public static class B104_P4_Assert
{
    static readonly CheckSink _sink = new CheckSink("[P4] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ==================================================================
    //  夹具：一份「两套同名卡组」的临时库（⛔ 不碰玩家的真存档）
    // ==================================================================

    // 🔴 **为什么要把 `DeckLibrary` 的三个「中央档」钩子临时摘掉**：Shell 会把它们接到 `SaveCenter`
    //    （`Deck/DeckRuntime.cs:9679`）。不摘的话，宿主一旦设了 `SaveCenter.OverrideRoot`（= `StoreOn` 真），
    //    夹具里每一次 `Save()` 都会写一份 `SaveKeys.PlayerDecks` 到宿主那份中央档上 ——
    //    污染的是**宿主自己的**测试数据（后面的断言会读到被我们改过的卡组表）。摘掉 ⇒ 只走 `DeckStore` 的纯文件路。
    static Func<string, string> _keepReadHook;
    static DeckLibrary.DeckLegacySaver _keepSaveHook;
    static Func<bool> _keepExistsHook;

    static void DetachSaveHooks()
    {
        _keepReadHook = DeckLibrary.ReadLegacyText;
        _keepSaveHook = DeckLibrary.SaveLegacyText;
        _keepExistsHook = DeckLibrary.CentralSaveExists;
        DeckLibrary.ReadLegacyText = null;
        DeckLibrary.SaveLegacyText = null;
        DeckLibrary.CentralSaveExists = null;
    }

    static void RestoreSaveHooks()
    {
        DeckLibrary.ReadLegacyText = _keepReadHook;
        DeckLibrary.SaveLegacyText = _keepSaveHook;
        DeckLibrary.CentralSaveExists = _keepExistsHook;
        _keepReadHook = null; _keepSaveHook = null; _keepExistsHook = null;
    }

    static string NewTempDir()
    {
        string d = Path.Combine(Path.GetTempPath(), "wf_b104p4_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(d);
        return d;
    }

    /// <summary>造一份卡组存档：**两套名字完全相同**（`Rename` 不做唯一化 —— 这正是同名卡组的生产来源），
    /// 督军也相同 ⇒ **只有身份能区分它们**。返回**从盘上重读过**的一份（不是内存里那份，避免自证）。</summary>
    static DeckLibrary MakeTwoSameName(string dir, out string path)
    {
        path = Path.Combine(dir, "decks.json");
        DeckStore.OverridePath = path;
        var lib = DeckLibrary.Load();                 // 空库（这个路径还没有存档）
        var a = lib.Create("Same");
        var b = lib.Create("Same 2");
        lib.Rename(1, "Same");                        // ⇒ 两套都叫 "Same"
        a.WarlordId = "AM1";                          // 真实 id（`AM1` = Ultramarines 督军）：阵营解析得出来，
        b.WarlordId = "AM1";                          //   两副**连督军都一样** ⇒ 只有身份能区分
        a.CardIds = new List<string> { "AM14" };      // 内容给一点，免得别的口把空卡组当非法
        b.CardIds = new List<string> { "AM15" };
        lib.Save();
        return DeckLibrary.Load();                    // 重读：断言要量**盘上**那一份
    }

    // ==================================================================
    //  Run()：不需要 UI 的四条
    // ==================================================================
    /// <summary>本件入口（无 UI 那一半）。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "P4：卡组身份（DeckId）—— 发放 / 落盘 / 按身份重排 / 幂等");
        string keepPath = DeckStore.OverridePath;
        DetachSaveHooks();
        try
        {
            CheckA1IdentityIsUnique();
            CheckA2IdentityPersists();
            CheckA3ReorderByIdentity();
            CheckA4EnsureIsIdempotent();
        }
        finally
        {
            DeckStore.OverridePath = keepPath;
            RestoreSaveHooks();
        }
        Debug.Log(_sink.P + "P4(Run) 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    /// <summary>A1：两套同名卡组各自拿到**不同**的 id，且 `IndexOfDeckId` **各回各的**（同名不歧义）。</summary>
    static void CheckA1IdentityIsUnique()
    {
        string dir = NewTempDir(), path;
        var lib = MakeTwoSameName(dir, out path);
        try
        {
            Tr(lib.Decks.Count == 2, "A1a（前提）临时库里正好 2 套卡组（实得 " + lib.Decks.Count + "）");
            if (lib.Decks.Count != 2) return;
            C(lib.Decks[0].Name, lib.Decks[1].Name,
              "A1b（前提）两套**同名**（`Rename` 不做唯一化 ⇒ 同名卡组是真实存在的形状；名字区分不了它们）");

            string id0 = lib.EnsureDeckId(0);
            string id1 = lib.EnsureDeckId(1);
            Tr(!string.IsNullOrEmpty(id0) && !string.IsNullOrEmpty(id1), "A1c 两套都发到了 id（" + id0 + " / " + id1 + "）");
            Tr(id0 != id1, "A1d 两套的 id **不同**（同 id = 身份撞车）");
            C(lib.IndexOfDeckId(id0), 0, "A1e 按 id0 反查 ⇒ 第 0 套");
            C(lib.IndexOfDeckId(id1), 1, "A1f 按 id1 反查 ⇒ 第 1 套（🧨 改回按名 ⇒ 两条都落到第 0 套）");
            C(lib.IndexOfDeckId("no-such-id"), -1, "A1g 查不到的 id ⇒ -1（⛔ 别回落到 0）");
        }
        finally { TryDelete(dir); }
    }

    /// <summary>A2：id **真的进了存档**（重读一份仍在）—— 这是 P4-3 跨进程能认回卡组的前提。</summary>
    static void CheckA2IdentityPersists()
    {
        string dir = NewTempDir(), path;
        var lib = MakeTwoSameName(dir, out path);
        try
        {
            string id0 = lib.EnsureDeckId(0), id1 = lib.EnsureDeckId(1);   // 内部会 Save
            var re = DeckLibrary.Load();                                  // ⛔ 不是自证：真的是另一份对象
            Tr(re != null && re.Decks.Count == 2, "A2a（前提）重读回来 2 套");
            if (re == null || re.Decks.Count != 2) return;
            C(re.Decks[0].DeckId, id0, "A2b **盘上**第 0 套的 id 就是发出去那个（🧨 只在内存里发、不落盘 ⇒ 这条红）");
            C(re.Decks[1].DeckId, id1, "A2c **盘上**第 1 套的 id 就是发出去那个");
            C(re.IndexOfDeckId(id1), 1, "A2d 重读之后按 id 反查仍然对得上（序列化往返没丢）");
        }
        finally { TryDelete(dir); }
    }

    /// <summary>A3：**按对象身份**重排（原版 `DeckInventory.UpdateOrder(List<CardDeck>)` 比的 `deckId`）。</summary>
    static void CheckA3ReorderByIdentity()
    {
        string dir = NewTempDir(), path;
        var lib = MakeTwoSameName(dir, out path);
        try
        {
            string id0 = lib.EnsureDeckId(0), id1 = lib.EnsureDeckId(1);
            var d0 = lib.Decks[0];
            var d1 = lib.Decks[1];
            C(d0.Name, d1.Name, "A3a（前提）名字相同 ⇒ 「按名重排」在这里**换不动位置**，只有身份能换");
            bool ok = lib.UpdateOrder(new List<PlayerDeck> { d1, d0 });
            Tr(ok, "A3b `UpdateOrder(按对象身份)` 回 true（写进去了）");
            var re = DeckLibrary.Load();
            Tr(re != null && re.Decks.Count == 2, "A3c（前提）重读回来 2 套");
            if (re == null || re.Decks.Count != 2) return;
            C(re.Decks[0].DeckId, id1,
              "A3d **盘上第 1 格 = 原来第 2 套**（比 id，不比名字 —— 两副同名，🧨 按名实现给不出这个结果）");
            C(re.Decks[1].DeckId, id0, "A3e **盘上第 2 格 = 原来第 1 套**（Remove + Insert，不是交换）");
            C(re.Decks[0].CardIds[0], "AM15", "A3f 同上，按**内容**再钉一次（第 1 格是原来第 2 套那张卡）");
        }
        finally { TryDelete(dir); }
    }

    /// <summary>A4：`EnsureDeckId` 幂等 —— 第二次不许再发一个新 id（否则每次开窗都换身份）。</summary>
    static void CheckA4EnsureIsIdempotent()
    {
        string dir = NewTempDir(), path;
        var lib = MakeTwoSameName(dir, out path);
        try
        {
            string first = lib.EnsureDeckId(0);
            string second = lib.EnsureDeckId(0);
            C(second, first, "A4a 连续两次 `EnsureDeckId(0)` ⇒ **同一个** id（🧨 每次都新发 ⇒ 这条红）");
            C(lib.DeckIdAt(0), first, "A4b `DeckIdAt(0)` 读到的是同一个");
            C(lib.EnsureDeckId(99), null, "A4c 越界 ⇒ null（⛔ 别偷偷发一个挂在库外）");
        }
        finally { TryDelete(dir); }
    }

    // ==================================================================
    //  RunUi(WindowsManager)：建窗口那五条
    // ==================================================================
    /// <summary>本件的 UI 那一半（需要一具 `WindowsManager`）。</summary>
    public static void RunUi(WindowsManager wm)
    {
        MenuCheck.Section(_sink, "P4/P5：窗口级 —— 选第二副同名 / 活动槽存 id / 拖拽按身份 / 失败挡开窗与开战");
        Tr(wm != null, "（前提）传进来的 `WindowsManager` 不是 null（null ⇒ 下面几条都建不了窗，**不许静默跳过**）");
        if (wm == null) return;

        string keepPath = DeckStore.OverridePath;
        var keepLastOpened = CollectionWindow.LastOpened;
        DetachSaveHooks();                                 // 夹具只走 `DeckStore` 的纯文件路（理由见 `DetachSaveHooks`）
        try
        {
            RunUiInner(wm);
        }
        finally
        {
            DeckStore.OverridePath = keepPath;
            CollectionWindow.LastOpened = keepLastOpened;
            CollectionData.ResetForTest();                 // 让宿主后面读回**它自己**的夹具路径
            RestoreSaveHooks();
        }
        Debug.Log(_sink.P + "P4(RunUi) 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    static void RunUiInner(WindowsManager wm)
    {
        string dir = NewTempDir();
        string path;
        MakeTwoSameName(dir, out path);
        DeckStore.OverridePath = path;
        CollectionData.ResetForTest();
        try
        {
            Tr(CollectionData.DeckCount() == 2, "（前提）`CollectionData` 读到的临时库有 2 套（实得 " + CollectionData.DeckCount() + "）");
            if (CollectionData.DeckCount() != 2) return;
            C(CollectionData.DeckAt(0).Name, CollectionData.DeckAt(1).Name, "（前提）这两套**同名**");

            // 先把两副的**身份**物化出来（旧档没有 id 时第一次用到才补发）—— 下面几条都拿它当判据。
            string idA = CollectionData.EnsureDeckId(0), idB = CollectionData.EnsureDeckId(1);
            Tr(!string.IsNullOrEmpty(idA) && !string.IsNullOrEmpty(idB) && idA != idB,
               "（前提）两副各自有一个**不同**的 id（" + idA + " / " + idB + "）");
            if (string.IsNullOrEmpty(idA) || string.IsNullOrEmpty(idB) || idA == idB) return;

            CheckB1PracticeOwnIndex(wm);
            CheckB2EventOwnIndexAndSlotId(wm);
            CheckB3ReorderByIdentity(wm);
            CheckB4SelectFailBlocksDetail(wm);
            CheckB5SelectFailBlocksBattle(wm);
        }
        finally
        {
            TryDelete(dir);
        }
    }

    /// <summary>B1（P4-2）：练习窗的「选卡组」回调 —— 选**第 2 副同名**必须落到第 2 副。</summary>
    static void CheckB1PracticeOwnIndex(WindowsManager wm)
    {
        PracticeModePopup pw = null;
        DeckSelectionPopup sel = null;
        try
        {
            pw = PracticeModePopup.Create(wm);
            Tr(pw != null, "B1a（前提）建出了 `Practice Mode Menu`");
            if (pw == null) return;
            pw.TryOpen(null);
            sel = pw.OpenDeckSelection();
            Tr(sel != null, "B1b（前提）开出了 `Deck Selection Popup with Tabs`");
            if (sel == null) return;

            int before = pw.DeckIndex;
            sel.Pick(new DeckSelectionPopup.DeckPick
            {
                Prebuilt = false,
                Info = CollectionData.DeckAt(1),
                OwnIndex = 1,
                PrebuiltDeck = null,
            });
            C(pw.DeckIndex, 1,
              "B1c 选**第 2 副（同名）** ⇒ `DeckIndex == 1`（🧨 回调退回 `IndexOf(pick.Info.Name)` ⇒ 落到第 0 套，这条红）");
            Debug.Log("[P4] （记录）B1 回调前 `DeckIndex = " + before + "` —— 只入日志，不作为判据");
        }
        finally
        {
            if (sel != null) SafeDestroy(sel.gameObject);
            if (pw != null) SafeDestroy(pw.gameObject);
        }
    }

    /// <summary>B2（P4-1 + P4-3）：事件窗的「选卡组」回调 + **活动卡组槽存的是卡组 id**。</summary>
    static void CheckB2EventOwnIndexAndSlotId(WindowsManager wm)
    {
        SkirmishEventWindow sk = null;
        DeckSelectionPopup sel = null;
        string eventId = "B104P4_" + Guid.NewGuid().ToString("N").Substring(0, 6);   // 唯一：别撞上别的夹具
        try
        {
            sk = SkirmishEventWindow.Create(wm);
            Tr(sk != null, "B2a（前提）建出了 `Skirmish Mode Event Window`");
            if (sk == null) return;
            sk.EventId = eventId;
            sk.Open();
            sel = sk.OpenDeckSelection();
            Tr(sel != null, "B2b（前提）开出了 `Deck Selection Popup with Tabs`");
            if (sel == null) return;

            sel.Pick(new DeckSelectionPopup.DeckPick
            {
                Prebuilt = false,
                Info = CollectionData.DeckAt(1),
                OwnIndex = 1,
                PrebuiltDeck = null,
            });
            C(sk.DeckIndex, 1,
              "B2c 选**第 2 副（同名）** ⇒ `DeckIndex == 1`（🧨 回调退回 `IndexOf(name)` ⇒ 落到第 0 套，这条红）");

            string wantId = CollectionData.DeckIdAt(1);
            Tr(!string.IsNullOrEmpty(wantId), "B2d（前提）第 2 副有一个 id（" + wantId + "）");
            C(LiveOpsEventWindow.EventDeckSlotIdFor(eventId), wantId,
              "B2e 活动卡组槽里存的是**卡组 id**（原版 `PlayEventSaveGame.defaultDeckId`；"
            + "🧨 退回存 `raw.Name` ⇒ 这里是 null，这条红）");
            C(LiveOpsEventWindow.EventDeckSlotFor(eventId), CollectionData.DeckAt(1).Name,
              "B2f 槽里同时留了一份**名字**（只为诊断 / 旧档回落；两条都要对，名字对而 id 缺 = 旧档形状）");
        }
        finally
        {
            if (sel != null) SafeDestroy(sel.gameObject);
            if (sk != null) SafeDestroy(sk.gameObject);
        }
    }

    /// <summary>B3（P4-4）：收藏窗拖拽重排 —— 两副同名，拖完必须**按对象**换位。</summary>
    static void CheckB3ReorderByIdentity(WindowsManager wm)
    {
        CollectionWindow cw = null;
        try
        {
            cw = CollectionWindow.Create(wm);
            Tr(cw != null, "B3a（前提）建出了收藏窗");
            if (cw == null) return;
            cw.ClearDeckFilters();
            cw.ClearDeckModeFilter();

            string w0 = CollectionData.Raw(0).WarlordId, w1 = CollectionData.Raw(1).WarlordId;
            string id0 = CollectionData.DeckIdAt(0), id1 = CollectionData.DeckIdAt(1);
            C(w0, w1, "B3b（前提）两副的督军也相同 ⇒ 名字 / 督军都区分不了，只能靠身份");

            Tr(cw.DeckMoveForTest(0, 1), "B3c 拖第 1 格到第 2 格 ⇒ 报「写进去了」");
            var re = DeckLibrary.Load();                       // ⛔ 读盘上那一份（不是 `CollectionData` 的缓存）
            Tr(re != null && re.Decks.Count == 2, "B3d（前提）盘上仍是 2 套");
            if (re == null || re.Decks.Count != 2) return;
            C(re.Decks[0].DeckId, id1,
              "B3e **盘上第 1 格 = 原来第 2 套**（🧨 退回「转名字数组再按名匹配」⇒ 两副同名，换不动，这条红）");
            C(re.Decks[1].DeckId, id0, "B3f **盘上第 2 格 = 原来第 1 套**");
        }
        finally
        {
            if (cw != null) SafeDestroy(cw.gameObject);
        }
    }

    /// <summary>B4（P5-3）：收藏窗选卡组**落盘失败** ⇒ 不出声就完了那一支已被修掉：
    /// 要**回滚当前选中**、**不开详情窗**。</summary>
    static void CheckB4SelectFailBlocksDetail(WindowsManager wm)
    {
        string keepPath = DeckStore.OverridePath;
        CollectionWindow cw = null;
        try
        {
            int prev = CollectionData.CurrentIndex();
            int tgt = prev == 0 ? 1 : 0;
            Tr(tgt != prev, "B4a（前提）挑一个**不是当前选中**的下标（" + tgt + " ≠ " + prev + "）");

            // 把**写盘**打断：内存库里还是刚才那 2 套，但 `Save()` 会往一个不存在的目录写 ⇒ 必失败。
            string badDir = Path.Combine(Path.GetTempPath(), "wf_b104p4_nodir_" + Guid.NewGuid().ToString("N").Substring(0, 6));
            DeckStore.OverridePath = Path.Combine(badDir, "x.json");
            Tr(!Directory.Exists(badDir), "B4b（前提）坏路径的目录**真的不存在**（否则这条测不出写盘失败）");

            cw = CollectionWindow.Create(wm);
            CollectionWindow.LastOpened = null;               // 只量「这一次点格有没有开详情窗」
            cw.SelectDeck(tgt);

            C(CollectionData.CurrentIndex(), prev,
              "B4c 落盘失败 ⇒ **当前卡组/高亮回滚到原来那套**（🧨 删掉 `RestoreSelection` ⇒ 这条红）");
            Tr(CollectionWindow.LastOpened == null,
               "B4d 落盘失败 ⇒ **不开 `Deck info Popup`**（🧨 删掉那个 `return` ⇒ 这条红）");
        }
        finally
        {
            DeckStore.OverridePath = keepPath;
            try { wm.HidePopUp(true); } catch { /* 没弹出来就没什么可收的 */ }   // 本条的**错误弹窗是真的弹了** ⇒ 收掉，别留给下一段
            if (cw != null) SafeDestroy(cw.gameObject);
        }
    }

    /// <summary>B5（P5-4）：练习窗开战前「选中失败」⇒ **必须挡住开战**并回滚当前卡组。</summary>
    static void CheckB5SelectFailBlocksBattle(WindowsManager wm)
    {
        string keepPath = DeckStore.OverridePath;
        PracticeModePopup pw = null;
        try
        {
            int prev = CollectionData.CurrentIndex();
            int tgt = prev == 0 ? 1 : 0;

            string badDir = Path.Combine(Path.GetTempPath(), "wf_b104p4_nodir2_" + Guid.NewGuid().ToString("N").Substring(0, 6));
            DeckStore.OverridePath = Path.Combine(badDir, "x.json");
            Tr(!Directory.Exists(badDir), "B5a（前提）坏路径的目录**真的不存在**");

            pw = PracticeModePopup.Create(wm);
            pw.TryOpen(null);
            pw.DeckIndex = tgt;                               // 摆成「玩家挑了另一套」
            pw.StartBotBattle();

            Tr(!pw.StartedBattle,
               "B5b 落盘失败 ⇒ **没有开战**（`StartedBattle` 仍为 false；🧨 删掉 `StartBotBattle` 里那个 `return` ⇒ 这条红）");
            C(CollectionData.CurrentIndex(), prev, "B5c 落盘失败 ⇒ 当前卡组回滚到原来那套");
        }
        finally
        {
            DeckStore.OverridePath = keepPath;
            try { wm.HidePopUp(true); } catch { /* 没弹出来就没什么可收的 */ }
            if (pw != null) SafeDestroy(pw.gameObject);
        }
    }

    // ==================================================================
    //  小工具
    // ==================================================================
    static void SafeDestroy(GameObject go)
    {
        if (go == null) return;
        try { PointerLayer.UnregisterOwnedBy(go); } catch { /* 没登记过就算了 */ }
        UnityEngine.Object.DestroyImmediate(go);
    }

    static void TryDelete(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { /* 临时目录，删不掉不算失败 */ }
    }
}
