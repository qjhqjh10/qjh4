// B104_P26_Assert.cs — 第一百零四会话 · 执行代理 `B104_P26`（2026-10-31 · P2/P5/P6 六条）
//
// ============================ 它盯的是什么 ============================
//   P2-1 聊天卡组分享的**导入入口**：`DeckInfoPopup` 在 `Import + OverrideDeck` 时 `Edit Deck` 必须**露着**，
//        点它 = 把**卡组对象本身**交给编辑器（`CollectionData.PendingNewDeck` + `CollectionWindow.GoNewDeck()`），
//        而不是原来那条「LogWarning + return」的死路；顺带 `Select Deck` 必须**藏**（原版 `context.SelectButton == null`）。
//   P2-2 经 `WindowsManager`/外部 `Close()` 关卡组编辑窗时，**脏就必须查脏**（`DeckRuntime.TryClose`）；
//        运行时自己的离场链走 `CloseFromRuntime()` 绕开（那一跳已查过脏/已丢弃）。
//   P5-2 `CollectionData.Select` 失败 ⇒ **不关窗** + 上屏（原来紧接一句 `Close()` = 静默失败）。
//   P5-6 `DeckRuntime.EquipCardback` 的**未知卡背名**必须出声（原来 `return false` 无日志）。
//   P6   静态事件的入口先判「我还活着」：已销毁的实例被信号打到 ⇒ 出声 + 当场退订 + return
//        （`BattleCameraSreenSize` · `ScreenAspectRatioController` · `TimerDisplay` · `TopBar`）。
//
// 判据 / 原版出处 → 报告 `资料/完全复刻/报告/B104_P26.md`。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   `B104_P26_Assert.CheckB104P26();`
//   · **不需要开局、不需要 battle**；会现建一台 `WindowsManager`（`WindowsManager.Get()`，幂等）。
//   · P2-2 那段会临时把 `DeckStore.OverridePath` 指向 `Temp/_p26_deckwin.json`（跑完删掉 + 还原），
//     **不碰玩家真卡组**；建出来的窗与运行时**全部 `DestroyImmediate` 收尾**。
//   · 跑完会把 `CollectionData` 的四个交接/错误口还原成进来时的值（自检之间互不影响）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   · `EditDeckShown()` 把 `OverrideDeck == null` 合取项加回去 ⇒ **P21a** 红。
//   · `CreateForPicked` 的 `SelectButtonProvided` 改回恒 `true` ⇒ **P21b** 红。
//   · `OnButton("Edit Deck")` 把 `HandToEditor()` 换回 `LogWarning + return` ⇒ **P21c/P21d** 红。
//   · `DeckEditingWindow.Close()` 删掉查脏闸 ⇒ **P22a/P22b** 红；把 `CloseFromRuntime()` 也改成走查脏 ⇒ **P22c** 红（二次弹窗/关不掉）。
//   · `Select Deck` 失败支把 `Close()` 加回去 ⇒ **P52a** 红。
//   · 删掉 `EquipCardback` 的两条 `LogWarning` ⇒ **P56a/P56b** 红（捕获不到）。
//   · 三个静态事件 handler 删掉 `this == null` 闸 ⇒ **P6a/P6c/P6d** 的「计数回基线」红（订阅者永远挂着）；
//     `BattleCameraSreenSize` 那条还会让 `InitializeCount` 从 0 变 1（**P6b**）。
//   · `TopBar.ApplyChallengeVisible` 的 `_challengeRoot == null` 闸删掉 ⇒ **P6f** 红（在已销毁的 GO 上 `SetActive` 抛异常）；
//     guard 里只 `return` 不退订 ⇒ **P6f2** 红（计数停在 beforeDead+1）。
//   · `TopBar.DetachChallengeButton()` 把 `-=` 改成清空 event（或漏 `-=`）⇒ **P6e/P6e2** 红；收尾不还原 ⇒ **P6g** 红。
//   · ⚠️ **P6e/P6f 首次上机曾双红（ShellScene）**：那是**断言写错**（把「Detach 不会摘宿主自己那条」当成了期望），
//     不是实现缺陷 —— 订正记录 → `报告/B104_P26.md` §二·P6 那条 2026-10-31 晚订正。
using System;
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B104_P26`：P2-1 / P2-2 / P5-2 / P5-6 / P6 的**结构断言**。
/// <para>⛔ 期望值尽量是字面量（`0` / `1` / `false`）或**基线加减**；不从被测实现反算期望值。</para></summary>
public static class B104_P26_Assert
{
    static readonly CheckSink _sink = new CheckSink("[P26] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    /// <summary>本件唯一入口（宿主 `Run()` 里调一次；⛔ 不用 `Test` 前缀，同 `B104_V2_Assert` 的理由）。</summary>
    public static void CheckB104P26()
    {
        MenuCheck.Section(_sink, "P26：聊天卡组分享导入 · 关窗查脏 · 选择失败留窗 · 卡背出声 · 静态事件退订");

        var mgr = WindowsManager.Get();
        Tr(mgr != null, "（前提）`WindowsManager` 在（`Get()` 幂等，没就现建）");

        // 交接/错误口：进来时是什么、出去还原成什么
        int keepPendEdit = CollectionData.PendingEditDeck;
        var keepPendNew = CollectionData.PendingNewDeck;
        var keepReturn = CollectionData.PendingReturn;
        string keepStorePath = DeckStore.OverridePath;
        try
        {
            CheckP21ChatShareImport(mgr);
            CheckP22CloseChecksDirty(mgr);
            CheckP52SelectFailureKeepsWindow(mgr);
        }
        finally
        {
            // 收尾：把静态口还原（自检之间互不影响）
            CollectionData.PendingEditDeck = keepPendEdit;
            CollectionData.PendingNewDeck = keepPendNew;
            CollectionData.PendingReturn = keepReturn;
            DeckStore.OverridePath = keepStorePath;
        }

        CheckP6StaticEventLiveness();

        // P5-6 需要一台**已建好**的 `DeckRuntime` —— 上面 P2-2 里已经验过；这里单独再建一台轻量的
        CheckP56CardbackSpeaks(mgr);

        Debug.Log(_sink.P + "P26 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ==================================================================
    //  P2-1：聊天卡组分享（Import + OverrideDeck）的导入入口
    // ==================================================================
    static void CheckP21ChatShareImport(WindowsManager mgr)
    {
        if (mgr == null) { Tr(false, "P21（前提）没有 `WindowsManager` ⇒ 本段空转（上面已有红）"); return; }

        var deck = new PlayerDeck { Name = "P26 聊天分享卡组", WarlordId = "AM1", GameMode = 0 };
        var info = new CollectionData.DeckInfo
        {
            Name = deck.Name,
            WarlordId = "AM1",
            Count = 0,
            CardbackId = null,
            GameMode = 0,
            Faction = "",
        };
        var pop = DeckInfoPopup.CreateForPicked(mgr, -1, deck, info,
                                                DeckInfoPopup.DeckInfoState.Import, false, null, null);
        Tr(pop != null, "（前提）Import 态详情窗建出来了");
        if (pop == null) return;
        mgr.OpenWindow(pop);
        Tr(pop.IsOpen(), "（前提）Import 态详情窗开着");

        // ---- 显隐：原版 `DeckInfoControls__Initialize`:77（Edit Deck ← state < 2）与 :~100（Select ← context.SelectButton != null）
        Tr(pop.IsItemShown("Btn:Edit Deck"),
           "P21a ★ 聊天 `DeckShare`（Import + OverrideDeck）⇒ `Edit Deck` **露着**（原版 `state < 2`）"
         + "｜🧨 把 `EditDeckShown()` 的 `OverrideDeck == null` 合取项加回去 ⇒ 本条红");
        Tr(!pop.IsItemShown("Btn:Select Deck"),
           "P21b ★ …而 `Select Deck` **藏**（原版 `context.SelectButton == null`）"
         + "｜🧨 把 `CreateForPicked` 的 `SelectButtonProvided` 改回恒 `true` ⇒ 本条红");

        // ---- 点击：原版 `DeckInfoPopup__EditDeck.c` 最后两句 = `Close()` → `OpenWindow<DeckEditingWindow>(context.Deck)`
        int keepEdit = CollectionData.PendingEditDeck;
        var keepNew = CollectionData.PendingNewDeck;
        var keepRet = CollectionData.PendingReturn;
        try
        {
            CollectionData.PendingEditDeck = 7;        // 故意留一个脏值 ⇒ 交接必须把它清成 −1
            CollectionData.PendingNewDeck = null;
            var t = pop.Btn("Edit Deck");
            var wb = t != null ? t.GetComponent<WindowButton>() : null;
            Tr(wb != null, "（前提）`Edit Deck` 有点击区（关着的节点也查得到）");
            if (wb != null) wb.Click();

            C(CollectionData.PendingEditDeck, -1,
              "P21c ★ 点 `Edit Deck` ⇒ 交接给编辑器的 = **这一副（未入库）**、`PendingEditDeck` 清成 −1（两个交接口互斥）"
            + "｜🧨 把 `HandToEditor()` 换回 `LogWarning + return` ⇒ 本条红（还是 7）");
            Tr(ReferenceEquals(CollectionData.PendingNewDeck, deck),
              "P21d ★ …`PendingNewDeck` **就是**这一副（原版交的是 `context.Deck` 卡组对象，不是库下标）");
            Tr(!pop.IsOpen(), "P21e ★ …而且本窗**先关**（原版顺序：`Close()` → `OpenWindow`）");
        }
        finally
        {
            CollectionData.PendingEditDeck = keepEdit;
            CollectionData.PendingNewDeck = keepNew;
            CollectionData.PendingReturn = keepRet;
        }

        if (pop != null && pop.gameObject != null) UnityEngine.Object.DestroyImmediate(pop.gameObject);
    }

    // ==================================================================
    //  P2-2：经 `WindowsManager`/外部 `Close()` ⇒ 脏就查脏；离场链用 `CloseFromRuntime()` 绕开
    // ==================================================================
    static void CheckP22CloseChecksDirty(WindowsManager mgr)
    {
        if (mgr == null) { Tr(false, "P22（前提）没有 `WindowsManager` ⇒ 本段空转（上面已有红）"); return; }

        string tmpDeck = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, "../Temp/_p26_deckwin.json"));
        DeckStore.OverridePath = tmpDeck;
        DeckStore.DeleteFile();

        DeckEditingWindow deW = null;
        try
        {
            deW = DeckEditingWindow.Create(mgr);
            Tr(deW != null, "（前提）卡组编辑窗建出来了");
            if (deW == null) return;
            deW.Open();                                  // 只建内容、**不登记进 openWindows** ⇒ 不触发 `HideAllWindows`
            Tr(deW.Runtime != null, "（前提）`DeckRuntime` 建起来了");
            if (deW.Runtime == null) return;
            Tr(!deW.Runtime.DeckDirty, "（前提）起手**干净**");

            deW.Runtime.MarkDeckDirty();                 // 只标脏（`MarkDeckDirty` 是 public 的）
            Tr(deW.Runtime.DeckDirty, "（前提）标脏了");

            bool activeBefore = deW.gameObject.activeSelf;
            deW.Close();
            Tr(deW.gameObject.activeSelf == activeBefore,
               "P22a ★ 脏时经 `Close()` ⇒ **不关窗**（走 `DeckRuntime.TryClose` 查脏）"
             + "｜🧨 删掉 `DeckEditingWindow.Close()` 里那道查脏闸 ⇒ 本条红（窗被关掉、改动静默丢弃）");
            Tr(deW.Runtime.ModalPopupOpen,
               "P22b ★ …而是弹「丢弃改动?」那扇（= 原版关闭钮 `TryClose` → `ConfirmDiscard`）｜🧨 同上 ⇒ 本条红");
            if (deW.Runtime.ModalPopupOpen) mgr.HidePopUp();

            // 离场链（已查过脏/玩家已丢弃）⇒ 绕开闸、真关
            deW.CloseFromRuntime();
            Tr(!deW.gameObject.activeSelf,
               "P22c ★ `CloseFromRuntime()`（运行时离场链）⇒ **真关**、不再二次弹窗"
             + "｜🧨 让 `LeaveByHostWindow` 直接调 `Close()` ⇒ 这条或上面两条红（二次弹窗/关不掉）");
        }
        finally
        {
            if (deW != null && deW.gameObject != null) UnityEngine.Object.DestroyImmediate(deW.gameObject);
            if (mgr != null) mgr.HidePopUp(true);      // `force`：本段可能把栈顶换成别的窗，收尾要保证那扇消息弹窗一定关掉
            DeckStore.DeleteFile();
            DeckStore.OverridePath = null;
        }
    }

    // ==================================================================
    //  P5-2：`CollectionData.Select` 失败 ⇒ 不关窗 + 上屏
    // ==================================================================
    static void CheckP52SelectFailureKeepsWindow(WindowsManager mgr)
    {
        if (mgr == null) { Tr(false, "P52（前提）没有 `WindowsManager` ⇒ 本段空转（上面已有红）"); return; }

        var deck = new PlayerDeck { Name = "P26 选择失败夹具", WarlordId = "AM1", GameMode = 0 };
        var info = new CollectionData.DeckInfo
        {
            Name = deck.Name, WarlordId = "AM1", Count = 0, CardbackId = null, GameMode = 0, Faction = "",
        };
        // `DeckIndex = -1` + `OnSelectDeck = null` ⇒ 走 `CollectionData.Select(-1)`，它必然失败（越界）
        var pop = DeckInfoPopup.CreateForPicked(mgr, -1, deck, info,
                                                DeckInfoPopup.DeckInfoState.Edit, true, "Select deck", null);
        Tr(pop != null, "（前提）详情窗建出来了");
        if (pop == null) return;
        mgr.OpenWindow(pop);
        Tr(pop.IsItemShown("Btn:Select Deck"), "（前提）给了文案 ⇒ `Select Deck` 露着");

        var t = pop.Btn("Select Deck");
        var wb = t != null ? t.GetComponent<WindowButton>() : null;
        Tr(wb != null, "（前提）`Select Deck` 有点击区");
        if (wb != null) wb.Click();

        Tr(!string.IsNullOrEmpty(CollectionData.LastSelectError)
           && CollectionData.LastSelectError.IndexOf("越界", StringComparison.Ordinal) >= 0,
           "P52a ★ 选中失败 ⇒ **说出了原因**（`LastSelectError` 含「越界」）");
        Tr(pop.IsOpen(),
           "P52b ★ …而且**不关窗**（玩家能看见失败、不是「窗没了 = 以为选中了」）"
         + "｜🧨 在失败支里把 `Close()` 加回去 ⇒ 本条红");

        pop.Close();
        if (pop.gameObject != null) UnityEngine.Object.DestroyImmediate(pop.gameObject);
    }

    // ==================================================================
    //  P5-6：`EquipCardback` 的未知卡背名必须出声
    // ==================================================================
    static void CheckP56CardbackSpeaks(WindowsManager mgr)
    {
        if (mgr == null) { Tr(false, "P56（前提）没有 `WindowsManager` ⇒ 本段空转（上面已有红）"); return; }

        string tmpDeck = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, "../Temp/_p26_cardback.json"));
        DeckStore.OverridePath = tmpDeck;
        DeckStore.DeleteFile();

        GameObject go = null;
        var logs = new List<string>();
        Application.LogCallback h = (msg, stack, type) => { logs.Add(msg ?? ""); };
        try
        {
            // ⚠️ **不调 `Build`**：两个用例（未知名 / 空名）都在 `EquipCardback` 的**前两道闸**里返回、
            //    一个字节都碰不到 `State`；不 Build ⇒ 不会连带建顶栏（那会把挑战总线挂到静态上，
            //    而编辑模式 `DestroyImmediate` 不派 `OnDestroy` ⇒ 反倒给宿主留一条悬挂订阅）。
            go = new GameObject("P26 equip probe");
            var rt = go.AddComponent<DeckRuntime>();
            Tr(rt != null, "（前提）`DeckRuntime` 建出来了");
            if (rt == null) return;

            Application.logMessageReceived += h;
            bool r1 = rt.EquipCardback("__p26_no_such_cardback__");
            Application.logMessageReceived -= h;

            Tr(!r1, "P56a ★ 未知卡背名 ⇒ `EquipCardback` 返回 **false**（不写进存档）");
            Tr(Contains(logs, "不认识的卡背名"),
               "P56b ★ …而且**出声**（`LogWarning` 里带「不认识的卡背名」）"
             + "｜🧨 删掉 `EquipCardback` 那条 `LogWarning` ⇒ 本条红");

            logs.Clear();
            Application.logMessageReceived += h;
            bool r2 = rt.EquipCardback("");
            Application.logMessageReceived -= h;
            Tr(!r2 && Contains(logs, "名字为空"),
               "P56c ★ 空名字 ⇒ false + 「名字为空」出声｜🧨 删掉空名那条 `LogWarning` ⇒ 本条红");
        }
        finally
        {
            Application.logMessageReceived -= h;
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
            DeckStore.DeleteFile();
            DeckStore.OverridePath = null;
        }
    }

    static bool Contains(List<string> logs, string needle)
    {
        for (int i = 0; i < logs.Count; i++) if (logs[i] != null && logs[i].IndexOf(needle, StringComparison.Ordinal) >= 0) return true;
        return false;
    }

    // ==================================================================
    //  P6：静态事件的「入口先判我还活着」+ 当场退订
    // ==================================================================
    static void CheckP6StaticEventLiveness()
    {
        // ---- ① `BattleCameraSreenSize.ScreenResolutionChanged` ----
        int base0 = BattleCameraSreenSize.ResolutionSignalSubscriberCountForTest;
        GameObject go0 = null;
        BattleCameraSreenSize cam0 = null;
        try
        {
            go0 = new GameObject("P26 BattleCameraSreenSize");
            cam0 = go0.AddComponent<BattleCameraSreenSize>();
            cam0.RegisterResolutionSignal();
            C(BattleCameraSreenSize.ResolutionSignalSubscriberCountForTest, base0 + 1,
              "（前提）`BattleCameraSreenSize` 订上了分辨率信号");

            UnityEngine.Object.DestroyImmediate(go0); go0 = null;   // 编辑模式**不派** `OnDestroy`（已知坑）
            BattleCameraSreenSize.NotifyScreenResolutionChanged();

            C(BattleCameraSreenSize.ResolutionSignalSubscriberCountForTest, base0,
              "P6a ★ 已销毁的实例被信号打到 ⇒ **当场退订**（订阅者计数回基线）"
            + "｜🧨 删掉 `ResolutionHasChanged` 里的 `this == null` 闸 ⇒ 计数停在 baseline+1");
            C(cam0.InitializeCount, 0,
              "P6b ★ …而且**根本没进 `Initialize`**（guard 早退）｜🧨 删掉闸 ⇒ 变 1");
        }
        finally
        {
            if (go0 != null) UnityEngine.Object.DestroyImmediate(go0);
            BattleCameraSreenSize.NotifyScreenResolutionChanged();   // 兜底再清一次（万一上面中途抛）
        }

        // ---- ② `ScreenAspectRatioController`（同一个分辨率信号 + beginCameraRendering）----
        int base1 = BattleCameraSreenSize.ResolutionSignalSubscriberCountForTest;
        GameObject go1 = null, camGo = null;
        try
        {
            camGo = new GameObject("P26 Aspect cam");
            var c = camGo.AddComponent<Camera>();
            go1 = new GameObject("P26 ScreenAspectRatioController");
            var sc = go1.AddComponent<ScreenAspectRatioController>();
            sc.SetTargetCameras(c);          // ⛔ 不给相机会打一条 Error（`targetCameras == null` 那条）
            sc.Attach();
            C(BattleCameraSreenSize.ResolutionSignalSubscriberCountForTest, base1 + 1,
              "（前提）`ScreenAspectRatioController` 订上了分辨率信号");

            UnityEngine.Object.DestroyImmediate(go1); go1 = null;
            BattleCameraSreenSize.NotifyScreenResolutionChanged();

            C(BattleCameraSreenSize.ResolutionSignalSubscriberCountForTest, base1,
              "P6c ★ 已销毁 ⇒ `AdjustCameraViewport` 的 guard **当场 `Detach()`**（计数回基线）"
            + "｜🧨 删掉 `AdjustCameraViewport` 里的闸 ⇒ 计数停在 baseline+1");
        }
        finally
        {
            if (go1 != null) UnityEngine.Object.DestroyImmediate(go1);
            if (camGo != null) UnityEngine.Object.DestroyImmediate(camGo);
            BattleCameraSreenSize.NotifyScreenResolutionChanged();
        }

        // ---- ③ `TimerDisplay` 订 `Loc.LanguageChanged` ----
        int base2 = Loc.LanguageChangedSubscribers;
        GameObject go2 = null;
        try
        {
            go2 = new GameObject("P26 TimerDisplay");
            var td = go2.AddComponent<TimerDisplay>();
            td.SendMessage("Awake");          // 编辑模式 `AddComponent` **不跑** `Awake`（已知坑）⇒ 显式发一次
            C(Loc.LanguageChangedSubscribers, base2 + 1,
              "（前提）`TimerDisplay` 订上了换语言广播");

            UnityEngine.Object.DestroyImmediate(go2); go2 = null;
            Loc.NotifyLanguageChanged();

            C(Loc.LanguageChangedSubscribers, base2,
              "P6d ★ 已销毁 ⇒ `LocalizationChange` 的 guard **当场退订**（计数回基线）"
            + "｜🧨 删掉 `LocalizationChange` 里的闸 ⇒ 计数停在 baseline+1");
        }
        finally
        {
            if (go2 != null) UnityEngine.Object.DestroyImmediate(go2);
            Loc.NotifyLanguageChanged();
        }

        // ---- ④ `TopBar` / `ChallengeEngine.VisibleChanged` ----
        //  🔴 **2026-10-31（P6e/P6f 就地订正 · 断言侧）**：原写法把 `DetachChallengeButton()` 当成
        //  「不会摘掉宿主那条 `_challengeApply`」——**错**：它摘的**就是自己那条**（这正是它的用途）。
        //  ShellScene 里 `TopBar` 已建 ⇒ 基线 = 1；原写法拿 `base3` 当期望 ⇒ 实得 0，P6e/P6f 双红。
        //  ⇒ 改判据：① 摘线之后**只剩自检探针**（= 1，与基线无关）；② 用反射借来**真的**
        //  `ApplyChallengeVisible` 造「根已死、订阅还在」的现场，验证 guard 真退订且不抛；
        //  ③ 收尾把宿主的接线**原样挂回去**（⛔ 不污染后面的宿主断言）。
        int base3 = ChallengeEngine.VisibleListenerCount;

        // 快照 `TopBar` 的两格私有静态状态（收尾原样还原）
        var fRoot = typeof(TopBar).GetField("_challengeRoot", BindingFlags.NonPublic | BindingFlags.Static);
        var fApply = typeof(TopBar).GetField("_challengeApply", BindingFlags.NonPublic | BindingFlags.Static);
        if (fRoot == null || fApply == null)
        {
            Tr(false, "P6e（前提）`TopBar._challengeRoot/_challengeApply` 两格静态字段还在（本件用反射做快照/还原）");
            return;
        }
        object oldRoot = fRoot.GetValue(null);
        object oldApply = fApply.GetValue(null);
        bool hadOwn = oldApply != null;      // 进来时 `_challengeApply` 在不在（决定 `DetachChallengeButton` 会摘掉几条）

        // ---- P6e：`DetachChallengeButton()` 幂等、只摘自己那条 ----
        Action<bool> probe = v => { };
        ChallengeEngine.VisibleChanged += probe;
        C(ChallengeEngine.VisibleListenerCount, base3 + 1,
          "（前提）自检探针挂上去了（`VisibleChanged` 能订）");
        TopBar.DetachChallengeButton();
        TopBar.DetachChallengeButton();          // 幂等：第二次不许把别人的订阅也摘掉 / 不许抛
        // 两次调用之后：宿主的 `_challengeApply`（若在）被摘掉一条、探针仍在 ⇒ base3 − (hadOwn?1:0) + 1
        C(ChallengeEngine.VisibleListenerCount, base3 - (hadOwn ? 1 : 0) + 1,
          "P6e ★ `DetachChallengeButton()` **幂等**、且**只摘自己那条**（两次调用后：宿主那条已摘、自检探针仍在）"
        + "｜🧨 把 `-=` 改成 `VisibleChanged = null`（连别人的一起清）⇒ 本条红；第二次调用不幂等 ⇒ 本条红");
        ChallengeEngine.VisibleChanged -= probe;
        C(ChallengeEngine.VisibleListenerCount, base3 - (hadOwn ? 1 : 0),
          "P6e2 ★ 探针也摘掉 ⇒ **宿主的 `_challengeApply` 一条都不剩**（含已销毁对象）");

        // ---- P6f：构造「根已死、订阅还在」的现场（= 编辑模式 `DestroyImmediate` 不派 `OnDestroy` 的真实形状）----
        var mApply = typeof(TopBar).GetMethod("ApplyChallengeVisible", BindingFlags.NonPublic | BindingFlags.Static);
        if (mApply == null)
        {
            Tr(false, "P6f（前提）`TopBar.ApplyChallengeVisible` 还在（本件反射借它造现场）");
        }
        else
        {
            var realApply = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), mApply);
            var deadRoot = new GameObject("P26 dead challenge root");
            fApply.SetValue(null, realApply);
            fRoot.SetValue(null, deadRoot);
            int beforeDead = ChallengeEngine.VisibleListenerCount;
            ChallengeEngine.VisibleChanged -= realApply;
            ChallengeEngine.VisibleChanged += realApply;
            C(ChallengeEngine.VisibleListenerCount, beforeDead + 1, "（前提）「根还在、订阅在」的现场摆好了");

            UnityEngine.Object.DestroyImmediate(deadRoot);   // 编辑模式**不派** `OnDestroy` ⇒ 订阅留在静态事件上
            bool threw = false;
            // ⚠️ 传「当前值的反」⇒ **一定真的翻状态**（`SetVisible` 对同值早退，否则 guard 可能一次都跑不到）
            try { ChallengeEngine.Toggle(!ChallengeEngine.Visible); }
            catch (Exception) { threw = true; }
            Tr(!threw,
              "P6f ★ 已销毁的根仍被 `VisibleChanged` 打到 ⇒ 走 guard、**没有 `MissingReferenceException`**"
            + "｜🧨 删掉 `ApplyChallengeVisible` 里的 `_challengeRoot == null` 闸 ⇒ 在已销毁的 `GameObject` 上 `SetActive` ⇒ 抛异常 ⇒ 本条红");
            C(ChallengeEngine.VisibleListenerCount, beforeDead,
              "P6f2 ★ …而且 guard **当场退订**（`DetachChallengeButton()` → 自己那条被摘掉）"
            + "｜🧨 闸里只 `return` 不退订 ⇒ 计数停在 beforeDead+1 ⇒ 本条红");

            bool threw2 = false;
            try { ChallengeEngine.Toggle(!ChallengeEngine.Visible); }
            catch (Exception) { threw2 = true; }
            Tr(!threw2 && ChallengeEngine.VisibleListenerCount == beforeDead,
              "P6f3 ★ 退订之后再来一次信号 ⇒ 计数不变、不抛（拆干净了）");
        }

        // ---- 收尾：把宿主的接线**原样挂回去**（`_challengeApply` / `_challengeRoot` + `ChallengeEngine` 自己的总线）----
        fApply.SetValue(null, oldApply);
        fRoot.SetValue(null, oldRoot);
        if (oldApply is Action<bool> restore) ChallengeEngine.VisibleChanged += restore;
        ChallengeEngine.Attach();
        C(ChallengeEngine.VisibleListenerCount, base3,
          "P6g ★ 收尾还原：`VisibleChanged` 的订阅数回到进来时（不污染后续宿主断言）");
    }
}
