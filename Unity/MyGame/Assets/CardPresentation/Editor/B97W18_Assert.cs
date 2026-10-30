// B97W18_Assert.cs — 执行代理 B97_W18（第九十七会话 · 波 1 补位）的断言
//
// ============================ 它盯的是什么 ============================
//   ① 账 `战斗界面_HUD:364` `ChooseCardMenu.AutoProcessDone` —— **回放里的选牌面板**：
//      摆出来 · 按**录像里记的下标**预选 · 只在「自动播放中」才自动收口 · 2 秒后收口且不选卡。
//   ② 账 `战斗逻辑驱动:1081` `MatchData.SetMatchRecording` —— **录像里装回当时的规则参数**
//      （`GameplayVariables` 往返 + `overtimeTurn` 的可空编码 + `slotsPerSide` **必须跳过**）。
//
// 判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B97_R5_零散剩余施工单.md`（`战斗界面_HUD:364` 那一行）
//   ＋ `资料/完全复刻/判据/B97_R1_判据补全.md` 的 §`:1081` 与「第二轮·2」那一节
//   ＋ 第一权威 `d:/2/tools/decomp_full/ChooseCardMenu__Setup.c:113-119` /
//     `ChooseCardMenu._AutoProcessDone_d__21__MoveNext.c` / `MatchData__SetMatchRecording.c:47-78`。
//   本轮报告 → `资料/完全复刻/报告/B97_W18.md`。
//
// ============================ 怎么用（宿主）============================
//   `B97W18Assert.CheckB97W18(driver);`
//   · **宿主 = `Editor/BattleScene.cs`（`BattleScene.Run`）** —— A 段要一台活着的 `BattleDriver`
//     （`Ctx.CardPool` + 已建出来的 `Choose` 面板）。⛔ 别接到别的宿主：A0/A0a 会红（那是**故意的**，不许静默跳过）。
//   · 本文件**不建持久资产**；面板一律在 `finally` 里收掉、`_replayPaused` 原值还原。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1c / A2：两次只改录像里的 `picked`（2 → 0），读数必须跟着变 ⇒ 「恒 0」「恒 2」「不读记录」三种写法
//     不可能同时让这两条绿。
//   · A4：把 `ReplayAskGateBeforePush` 里 `if (_replayPaused) return false;` 删掉 ⇒ A4/A4a/A4b 红。
//   · B2 / B2a：把 `ToVars` 改成「总是写 `slotsPerSide`」⇒ B2 红；改成「永远不写」⇒ B2a 红。
//     两条**结构上不可能同时满足**。
//   · B3e：去掉 `overtimeTurnSet` 哨兵（改用 `int?` 或「0 当没有」）⇒ 设过 / 没设过 两态不再可分 ⇒ B3e 红。
//   · B4：给 `ReplayVars` 加一个 `int?` 字段 ⇒ B4 红（`JsonUtility` 会静默丢掉可空字段）。
using System;
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B97_W18`：回放选牌面板（账 `战斗界面_HUD:364`）＋ 录像装回规则参数（账 `战斗逻辑驱动:1081`）。</summary>
public static class B97W18Assert
{
    static readonly CheckSink _sink = new CheckSink("[W18] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    /// <summary>本件唯一入口（宿主 `Run()` 里用一个 `{ … }` 块调；⛔ 不用 `Test` 前缀，理由同 `B97_BD_Assert`）。</summary>
    public static void CheckB97W18(BattleDriver drv = null)
    {
        MenuCheck.Section(_sink, "W18：账 `战斗界面_HUD:364`（回放选牌面板 · AutoProcessDone）"
                               + " · 账 `战斗逻辑驱动:1081`（录像装回当时的规则参数）");
        SectionA_回放选牌面板(drv);
        SectionB_规则参数往返();
        Debug.Log(_sink.P + "W18 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ==================================================================
    //  A · 回放选牌面板
    //      原版：`ChooseCardMenu__Setup.c:113-119`（`matchType == 0xA0` ⇒ `SelectCard(_chooseCardUis[target])`
    //            ＋ `if (!isReplayPlayActive)` ⇒ 起 `AutoProcessDone`）
    //            ＋ `ChooseCardMenu._AutoProcessDone_d__21__MoveNext.c`（`WaitForSeconds(2f)` → `ProcessChooseDone()`）
    // ==================================================================
    static void SectionA_回放选牌面板(BattleDriver drv)
    {
        MenuCheck.Section(_sink, "A 回放面板：按**记录的下标**预选（⛔ 不是 0 号）· 只在自动播放中才收口");

        if (drv == null) drv = UnityEngine.Object.FindFirstObjectByType<BattleDriver>();
        Tr(drv != null, "A0（前提）拿到一个活着的 `BattleDriver`");
        if (drv == null) return;
        Tr(drv.Choose != null, "A0a（前提）这一局建了选牌面板（`BattleDriver.Choose` ≠ null）"
                             + " —— 不成立 ⇒ A1..A6 **没跑**（本条会红，不静默跳过）");
        var pool = drv.Ctx != null ? drv.Ctx.CardPool : null;
        var ids = new string[3];
        int got = 0;
        if (pool != null)
            foreach (var c in pool)
            {
                if (c == null || string.IsNullOrEmpty(c.Id)) continue;
                ids[got++] = c.Id;
                if (got >= 3) break;
            }
        Tr(got == 3, "A0b（前提）从 `Ctx.CardPool` 取到 3 张真卡的 id（现取 " + got + " 张）");
        if (drv.Choose == null || got != 3) return;

        bool savedPaused = drv.ReplayPaused;
        try
        {
            drv.SetReplayPaused(false);       // = 原版 `ClickPlayReplay`（`isReplayPlayActive = 0`，自动播放中）

            // ---- A1：按【记录的下标】预选（下标 = 2，故意不是 0）----
            drv.SetReplayAsksForTest(new List<ReplayAsk>
            {
                new ReplayAsk { action = 0, verb = "choosecard", candIds = ids, picked = 2 },
            });
            Tr(drv.ShowReplayAskForTest(0), "A1 回放里「第 0 条动作之前」那一处面板**摆出来了**");
            C(drv.ReplayAskShownForTest, 1, "A1a 摆过 1 处");
            Tr(drv.ReplayAskPanelOpenForTest, "A1b 面板现在开着");
            C(drv.Choose.CardCount, 3, "A1c 面板上摆了 3 张候选");
            C(drv.ReplayAskPickedForTest, 2, "A1d ★ **预选的是第 2 张**（= 录像里记的 `picked`；原版 "
                                           + "`SelectCard(_chooseCardUis[target])` 按**下标**选）"
                                           + "｜⚠️ ILSpy 版把下标印成 `int index = default;` 是**丢常量**，"
                                           + "`out_il2csharp/Assembly-CSharp/ChooseCardMenu.cs:116` 才是 `[target]`");
            drv.CloseReplayAskForTest();

            // ---- A2：灭自证 —— 只把记录里的 `picked` 改成 0，读数必须跟着变 ----
            drv.SetReplayAsksForTest(new List<ReplayAsk>
            {
                new ReplayAsk { action = 0, verb = "choosecard", candIds = ids, picked = 0 },
            });
            drv.ShowReplayAskForTest(0);
            C(drv.ReplayAskPickedForTest, 0, "A2 ★ 灭自证：把 `picked` 换成 0 ⇒ 读数跟着变 0"
                                           + "（与 A1d 合起来 ⇒ 这条读数**真的来自录像记录**，不是恒 0 / 恒 2 / 不读记录）");
            drv.CloseReplayAskForTest();

            // ---- A3：自动播放中，刚摆出来的那一帧**不收口**（原版 `WaitForSeconds(2f)`）----
            drv.SetReplayAsksForTest(new List<ReplayAsk>
            {
                new ReplayAsk { action = 0, verb = "choosecard", candIds = ids, picked = 2 },
            });
            drv.ShowReplayAskForTest(0);
            Tr(drv.ReplayAskGateForTest(), "A3a 刚摆出来的那一帧，闸返回 true（这一帧**别推**下一条动作）");
            Tr(drv.ReplayAskPanelOpenForTest, "A3b 2 秒没到 ⇒ 面板**还开着**");
            C(drv.ReplayAskPickedForTest, 2, "A3c 仍然预选第 2 张");
            drv.CloseReplayAskForTest();

            // ---- A4：暂停（= 原版 `isReplayPlayActive` 为真）⇒ **不起**自动处理 ----
            drv.SetReplayPaused(true);
            drv.SetReplayAsksForTest(new List<ReplayAsk>
            {
                new ReplayAsk { action = 0, verb = "choosecard", candIds = ids, picked = 2 },
            });
            Tr(!drv.ReplayAskGateForTest(), "A4 ★ 暂停（`_replayPaused`，= 原版 `isReplayPlayActive`）⇒ 闸返回 false："
                                          + "**不起自动处理**（`ChooseCardMenu__Setup.c:118` 的 `if (!isReplayPlayActive)`；"
                                          + "`ClickPauseReplay` 置 1 / `ClickPlayReplay` 置 0，见 `BattleManager.cs:5553/5560`）");
            Tr(!drv.ReplayAskPanelOpenForTest, "A4a 暂停时**没有**把面板摆出来（整条自动处理都不进）");
            C(drv.ReplayAskShownForTest, 0, "A4b 摆过 0 处");
            drv.SetReplayPaused(false);
            drv.CloseReplayAskForTest();

            // ---- A5：空表（老录像）⇒ 不摆面板 ----
            drv.SetReplayAsksForTest(new List<ReplayAsk>());
            Tr(!drv.ShowReplayAskForTest(0), "A5 `asks` 空（老录像 / 录制端没接上）⇒ 不摆面板（= 加这一格之前的行为）");
            C(drv.ReplayAskShownForTest, 0, "A5a 摆过 0 处");
            C(drv.ReplayAskRemainingForTest, 0, "A5b 剩余 0 处");
        }
        finally
        {
            drv.CloseReplayAskForTest();
            drv.SetReplayAsksForTest(null);
            drv.SetReplayPaused(savedPaused);
        }

        // ---- A6：`ReplayAsk` 过一遍 `JsonUtility`（= 真存档那条路）----
        var rec = new ReplayRecord();
        rec.asks.Add(new ReplayAsk { action = 3, verb = "choosecard", candIds = ids, picked = 1, actingId = "AM1" });
        var back = JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(rec));
        Tr(back != null && back.asks != null && back.asks.Count == 1, "A6（前提）`asks` 过一遍 `JsonUtility` 读得回来（1 条）");
        if (back != null && back.asks != null && back.asks.Count == 1)
        {
            C(back.asks[0].action, 3, "A6a `action` 往返");
            C(back.asks[0].picked, 1, "A6b `picked` 往返");
            C(back.asks[0].actingId, "AM1", "A6c `actingId` 往返");
            C(back.asks[0].candIds != null ? back.asks[0].candIds.Length : -1, 3, "A6d `candIds` 往返（3 张）");
            C(back.asks[0].candIds != null && back.asks[0].candIds[0] == ids[0] ? ids[0] : "<不等>", ids[0],
              "A6e `candIds` 逐张往返（第 0 张）");
        }

        // ---- A7：向后兼容 —— 老档 JSON 里根本没有这两个键 ----
        var oldRec = JsonUtility.FromJson<ReplayRecord>("{\"version\":4}");
        Tr(oldRec != null && oldRec.asks != null && oldRec.asks.Count == 0,
           "A7 **向后兼容**：老档 JSON 没有 `asks` 键 ⇒ 读成**空表**（不抛、不留 null ⇒ `ReplayAskResetForReplay` 走「不弹面板」那一支）");
        Tr(oldRec != null && !CardPresentation.ReplayVars.IsRecorded(oldRec.vars),
           "A7a **向后兼容**：老档 JSON 没有 `vars` 键 ⇒ `JsonUtility` 会把它**物化成全 0 对象**（不是 `null`）⇒ 只认 `ReplayVars.IsRecorded` = **false**（= 没记 ⇒ `ReplayVarsForReplay` **出声**后用默认，⛔ 不静默）｜判据 → 报告/B100_X3 #17 · 报告/B100_F10");
    }

    // ==================================================================
    //  B · 录像装回规则参数
    //      原版：`MatchData__SetMatchRecording.c:47-78` 把当时那一局的 `GameplayVariablesData`
    //            装回 `MatchData.GameplayData(+0xa8)`；我们 = `ReplayRecord.vars` → `BeginFromPendingCore(varsOverride:)`
    // ==================================================================
    static void SectionB_规则参数往返()
    {
        MenuCheck.Section(_sink, "B 录像装回规则参数：22 格往返（`slotsPerSide` **跳过**）· `overtimeTurn` 两态可分");

        // 造一份「每一格都给一个可辨认的值」的参数（经典模式为底）
        var src = GameplayVariables.For(GameMode.Classic).Clone();
        src.maxLegendaries = 1;
        src.numberOfCopiesOtherRarities = 3;
        src.numberOfCopiesLegendary = 2;
        src.slotsPerSide = 4;                  // 🔴 故意写成**原版那一档**（半场 4 格）⇒ 证明**不装回**
        src.warlordLifeChange = -7;
        src.deckSize = 27;
        src.overtimeTurn = 12;
        src.startingMana = 2;
        src.startingManaSecond = 3;
        src.addedManaSecond = 1;
        src.manaPerTurn = 2;
        src.drawCardsPerTurn = 2;
        src.manaAccumulation = 1;
        src.handLimit = 9;
        src.shouldReshuffle = true;
        src.showMulligan = false;
        src.assignDefensiveCardsInEditMode = true;
        src.alternativeTraitLogic = true;
        src.startingHand = 5;
        src.secondExtraCards = 2;
        src.manaBonusSeat0 = 1;
        src.manaBonusSeat1 = 2;

        var rec = new ReplayRecord { vars = ReplayVars.Capture(src) };
        Tr(rec.vars != null, "B0（前提）`ReplayVars.Capture` 抓得到一份快照");
        if (rec.vars == null) return;

        // 真过一遍 JSON —— 这才是「写 → 读」那条真存档路
        var back = JsonUtility.FromJson<ReplayRecord>(JsonUtility.ToJson(rec));
        Tr(back != null && back.vars != null, "B1（前提）`vars` 过一遍 `JsonUtility` 读得回来");
        if (back == null || back.vars == null) return;

        var v = back.vars.ToVars();            // 生产那条路（默认**不**装 `slotsPerSide`）

        C(v.maxLegendaries, 1, "B2a `maxLegendaries` 往返");
        C(v.numberOfCopiesOtherRarities, 3, "B2b `numberOfCopiesOtherRarities` 往返");
        C(v.numberOfCopiesLegendary, 2, "B2c `numberOfCopiesLegendary` 往返");
        C(v.warlordLifeChange, -7, "B2d `warlordLifeChange` 往返（在 `NewBattle` 那一趟就被读掉 ⇒ 装不回 = 督军血不对）");
        C(v.deckSize, 27, "B2e `deckSize` 往返（同上）");
        C(v.startingMana, 2, "B2f `startingMana` 往返");
        C(v.startingManaSecond, 3, "B2g `startingManaSecond` 往返");
        C(v.addedManaSecond, 1, "B2h `addedManaSecond` 往返");
        C(v.manaPerTurn, 2, "B2i `manaPerTurn` 往返");
        C(v.drawCardsPerTurn, 2, "B2j `drawCardsPerTurn` 往返");
        C(v.manaAccumulation, 1, "B2k `manaAccumulation` 往返");
        C(v.handLimit, 9, "B2l `handLimit` 往返");
        C(v.shouldReshuffle, true, "B2m `shouldReshuffle` 往返（⚠️ 我们默认 false；装不回 ⇒ 牌库抽空那一路根本不触发）");
        C(v.showMulligan, false, "B2n `showMulligan` 往返");
        C(v.assignDefensiveCardsInEditMode, true, "B2o `assignDefensiveCardsInEditMode` 往返");
        C(v.alternativeTraitLogic, true, "B2p `alternativeTraitLogic` 往返（冲突档开关）");
        // 🔴 我们独有的 4 格（原版没有，但**必须一起存**，否则起手 / 后手多抽 / 座位能量回到模式默认）
        C(v.startingHand, 5, "B2q `startingHand` 往返（**我们独有** —— 不存 ⇒ 起手张数回到模式默认）");
        C(v.secondExtraCards, 2, "B2r `secondExtraCards` 往返（**我们独有**）");
        C(v.manaBonusSeat0, 1, "B2s `manaBonusSeat0` 往返（**我们独有**）");
        C(v.manaBonusSeat1, 2, "B2t `manaBonusSeat1` 往返（**我们独有**）");

        // ---- B3：`slotsPerSide` 同名不同义 ⇒ **必须跳过** ----
        int ourDefaultSlots = new GameplayVariables().slotsPerSide;
        C(v.slotsPerSide, ourDefaultSlots,
          "B3 ★ `slotsPerSide` **没有装回**：源里写的是 4（原版「半场」那一档），装回来的是**我们自己的**每边格数（"
        + ourDefaultSlots + "）｜⚠️ 装回它 = 把棋盘从每边 " + ourDefaultSlots + " 格改成每边 4 格 ⇒ **另一局**"
        + "（判据 → `GameplayVariables.slotsPerSide` 的 doc：同名不同义）");
        Tr(v.slotsPerSide != src.slotsPerSide, "B3b 装回来的值**确实不等于**录像里那个 4（⇒ 这一格真的被跳过了）");
        C(back.vars.ToVars(includeSlotsPerSide: true).slotsPerSide, 4,
          "B3a 灭自证：`ToVars(includeSlotsPerSide: true)` ⇒ 4 —— 证明「装回」这条路**真的会改棋盘几何**，"
        + "所以 B3 那条不是空转（把 `ToVars` 写成「永远不写 slotsPerSide」⇒ 本条红）");

        // ---- B4：`overtimeTurn` 的「设过 / 没设过」两态 ----
        Tr(back.vars.overtimeTurnSet, "B4a 设过（12）⇒ `overtimeTurnSet` 过一遍 JSON 还是 true");
        Tr(v.overtimeTurn.HasValue, "B4b 设过 ⇒ 装回来 `HasValue`");
        if (v.overtimeTurn.HasValue) C(v.overtimeTurn.Value, 12, "B4c 设过 ⇒ 值 12");

        var noOt = GameplayVariables.For(GameMode.Classic).Clone();
        noOt.overtimeTurn = null;
        var noBack = JsonUtility.FromJson<ReplayRecord>(
            JsonUtility.ToJson(new ReplayRecord { vars = ReplayVars.Capture(noOt) }));
        Tr(noBack != null && noBack.vars != null, "B5（前提）「没设过」那一份也读得回来");
        if (noBack != null && noBack.vars != null)
        {
            Tr(!noBack.vars.overtimeTurnSet, "B5a 没设过（`null`）⇒ `overtimeTurnSet` = false");
            Tr(!noBack.vars.ToVars().overtimeTurn.HasValue, "B5b 没设过 ⇒ 装回来仍然是 `null`（= 这个模式没有加时）");
            Tr(v.overtimeTurn.HasValue && !noBack.vars.ToVars().overtimeTurn.HasValue,
               "B5c ★ 灭自证：「设过」与「没设过」**同时**成立才通过 —— 哨兵一去掉（`JsonUtility` 对可空字段静默丢 / "
             + "用「0 = 没有」糊）⇒ 两态有一个变样 ⇒ 本条红");
        }

        // ---- B6：结构上不许有可空字段（`JsonUtility` 会**静默丢掉**）----
        int nullable = 0, total = 0;
        foreach (var f in typeof(ReplayVars).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            total++;
            if (f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(Nullable<>)) nullable++;
        }
        C(nullable, 0, "B6 ★ `ReplayVars` **没有任何可空字段**（`JsonUtility` 不支持可空 ⇒ 会**静默丢**）"
                     + "—— `overtimeTurn` 用 `overtimeTurn` + `overtimeTurnSet` 两个非可空字段编码");
        C(total, ReplayVars.TotalFieldCount, "B6a `ReplayVars` 公有实例字段数 = " + ReplayVars.TotalFieldCount
                                           + "（18 原版 + 4 我们独有）—— 少一格就是漏存");
    }
}
