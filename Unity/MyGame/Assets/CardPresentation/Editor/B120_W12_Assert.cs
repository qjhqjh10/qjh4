// B120_W12_Assert.cs — 执行代理 `B120-W12`（`B120-E08` 加时阈值 · `B120-E09` AI 牌池两维 · `B120-E10` 来源回填）
//
// ============================ 它盯的是哪几条 ============================
//  A. **18 格真值表（`B120-OFF` §二 的那张表）** —— 逐格钉**字面值**（期望值一律写死在断言里，
//     ⛔ 不从被测实现读回来），防「改注释时顺手把数改了」：11 格有本地判据（官方文案 / 序列化资产）
//     ＋ 7~8 格自定，值必须与 `报告/B120_OFF_离线对战参数溯源.md` 逐格一致。
//  B. **加时阈值（`B120-E08`）** —— 经典 **18** · 遭遇 **8**，且**行为**真的落在全局第 18 / 第 8 回合
//     （不是只改常量不生效）；遭遇**比经典早**（官方文案 `Overtime begins earlier` 由机制复现）；
//     `overtimeTurn = null` 那一支仍然永不触发（照原版形状）。
//     判据：客户端常量 `GameStaticData.overtimeStartTurn = 18`（两份反编译一致）＋
//     规则书「后手最大能量达 10」⇒ 经典全局第 18 / 遭遇（4→7→9→11）全局第 8。
//  C. **AI 牌池（`B120-E09`）** —— 本地 224 副的分档交叉表 = `B120-OFF` §二·B 那张
//     （Classic 12/15/74/17 = 118 · Skirmish 0/26/40/40 = 106），`BotDecks` = 196；
//     我们 bot 挑牌链的两维 = **阵营 × 模式**（`FilterDecks`/`GetRandomDeck`），
//     **`difficulty` 不参与**（原版 `GetBotDeck` 那一支同样不按难度挑 —— 只有 `GetDeckByDifficulty`
//     按难度，而它的唯一调用者 `GetRankedBotPrebuiltDeck` 只挂在 `GetBotDeck` 的 unknown-matchType 兜底上）。
//  D. **`B120-E10` 回填在场** —— `GameplayVariables.cs` 的注释里真的带着 11 格的来源键
//     （`Event_Description/*` · `Tips/OvertimeTip` · `MenuShop/ExtraLegendaryWarning` · `MinionManager.slotsPerSide=4` ·
//     `GameStaticData.overtimeStartTurn = 18`），且那句**错的**「18 个值…全在服务器」已不在。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  接在 `Editor/BattleScene.cs:46983` 那条**方法尾聚合行**上 —— 即
//    `… B120_W11_Assert.Run(); pass += B120_W11_Assert.Pass; fail += B120_W11_Assert.Fail;`
//  之后、那条 `/* 🆕 2026-11-02（B117-WWIREA 接线）… */` **之前**，**同一物理行追加**（行数零变化）：
//    ` B120_W12_Assert.Run(); pass += B120_W12_Assert.Pass; fail += B120_W12_Assert.Fail;`
//  🔑 本件**零前提**：只用纯引擎（`RuleCore.NewBattle` 自建一局）＋只读 `Resources/prebuilt_deck_lists.json`，
//     不建 GameObject、不要活 driver / 场景。
//
// ============================ 🔴 灭自证 ============================
//  · B1：把 `ClassicOvertimeTurn` 改回 10 ⇒ B1（行为在第 18 回合）与 A1（常量 18）**同时**红。
//  · B2：把 `Skirmish` 的 `overtimeTurn` 改回跟经典同值 ⇒ B3「遭遇更早」红。
//  · C4：给 `FilterDecks` 加一道 `difficulty` 闸 ⇒ C4（四个难度都在）红。
//  · C5：把 `playMode` 那道闸删掉 ⇒ C5（抽到另一模式的副）红。
//  · D1~D4：把注释回滚成旧说法 ⇒ D 组红（那条正是本轮要消灭的错误口径）。
using System;
using System.Collections.Generic;
using System.IO;
using RuleEngine;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B120-W12`：加时阈值（`B120-E08`）· AI 牌池两维（`B120-E09`）· 参数来源回填（`B120-E10`）的落地断言。
/// <para>⛔ 期望值全是**原版判据 / `B120-OFF` 那张真值表**写死的字面量，不从被测实现里读回来。</para></summary>
public static class B120_W12_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B120W12] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    const string SrcVars = "Assets/RuleEngine/Core/GameplayVariables.cs";
    const string SrcDriver = "Assets/CardPresentation/Battle/BattleDriver.cs";
    const string SrcPrebuilt = "Assets/CardPresentation/Shell/PrebuiltDecks.cs";

    // ============================================================ 入口
    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();

        MenuCheck.Section(_sink, "B120-W12 · 加时阈值(E08) / AI 牌池两维(E09) / 参数来源回填(E10)");

        RunGroup("A · 18 格真值表（B120-OFF §二）", A_ValueTable);
        RunGroup("B · 加时阈值行为（经典 18 / 遭遇 8）", B_Overtime);
        RunGroup("C · AI 牌池两维（阵营 × 模式，difficulty 不参与）", C_DeckPool);
        RunGroup("D · E10 来源回填在场", D_DocBackfill);

        Debug.Log(_sink.P + "B120_W12 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    static void RunGroup(string name, Action body)
    {
        MenuCheck.Section(_sink, name);
        try { body(); }
        catch (Exception e)
        {
            Tr(false, name + "：这一组抛异常 ⇒ 本组结论**未验**：" + e.GetType().Name + " " + e.Message);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  A. 18 格真值表 —— 11 格有本地判据（官方客户端文案 / 序列化资产），其余自定，值见 `B120-OFF` §二
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void A_ValueTable()
    {
        var cl = GameplayVariables.Classic;
        // ---- 经典：8 格【官方文案】+ 1 格【序列化资产】+ 2 格【推断反推】+ 其余自定 ----
        C(cl.maxLegendaries, 0, "A1 经典 `maxLegendaries` = **0**（`Classic/Help`「No Legendary card limit」⇒ 0 = 关掉闸，⛔ 不是「一张都不许」）");
        C(cl.deckSize, 30, "A2 经典 `deckSize` = **30**（`Classic/Help`「30-card decks」；旁证 `VarsGlobal.deckSize=30`）");
        C(cl.startingHand, 3, "A3 经典 `startingHand` = **3**（`ScenarioVariables.startingHand`，`DefaultScenario.asset`）");
        C(cl.handLimit, 10, "A4 经典 `handLimit` = **10**（自定；`ScenarioVariables.maxCardsInHand=10` 只是旁证，原版不读它）");
        C(cl.manaPerTurn, 1, "A5 经典 `manaPerTurn` = **1**（`Classic/Help`「Gain +1 Maximum Energy each turn」）");
        C(cl.startingMana, 1, "A6 经典 `startingMana` = **1**（官方文案反推：`Both players start at 2 energy` − `manaPerTurn`）");
        C(cl.startingManaSecond, 1, "A7 经典 `startingManaSecond` = **1**（同上反推）");
        C(cl.drawCardsPerTurn, 1, "A8 经典 `drawCardsPerTurn` = **1**（`Tips/OvertimeTip`「Draw 2 cards per turn」＝基础 1 ＋ 加时 1）");
        C(cl.manaAccumulation, 0, "A9 经典 `manaAccumulation` = **0**（没有任何文案说经典累积；`Skirmish/Help` 那句只对遭遇）");
        C(cl.warlordLifeChange, 10, "A10 经典 `warlordLifeChange` = **+10**（`Classic/Help`「Warlords start with 10 more Health」＋真机实拍）");
        Tr(cl.showMulligan, "A11 经典 `showMulligan` = **true**（`Classic/Help`「Both players get to mulligan their starting hand」）");
        C(cl.slotsPerSide, 9, "A12 `slotsPerSide` = **9**（我们这一格是「每边总格数」，与原版同名不同义；原版每侧 4 格的序列化真值见注释）");
        C(cl.overtimeTurn.HasValue ? cl.overtimeTurn.Value : -1, 18,
          "A13 经典 `overtimeTurn` = **18**（客户端常量 `GameStaticData.overtimeStartTurn = 18` ＋ 规则「后手能量达 10」⇒ 全局第 18）");
        C(GameplayVariables.ClassicOvertimeTurn, 18, "A14 常量 `ClassicOvertimeTurn` = **18**（界面/日志读的是它，与 A13 同源）");

        // ---- 遭遇：官方文案 4 格 + 规则书 2 格 + 反推 2 格 + 自定 3 格 ----
        var sk = GameplayVariables.Skirmish;
        C(sk.maxLegendaries, 4, "A15 遭遇 `maxLegendaries` = **4**（`Skirmish/Help`「Up to 4 Legendaries (Warlord not included)」）");
        C(sk.deckSize, 12, "A16 遭遇 `deckSize` = **12**（`Skirmish/Help`「12-card decks」）");
        C(sk.manaPerTurn, 2, "A17 遭遇 `manaPerTurn` = **2**（`Skirmish/Help`「Gain +2 Maximum Energy each turn」）");
        C(sk.startingManaSecond, 2, "A18 遭遇 `startingManaSecond` = **2**（文案 `Player 2 at 3+1 Energy` 反推）");
        C(sk.manaAccumulation, 1, "A19 遭遇 `manaAccumulation` = **1**（`Skirmish/Help`「accumulate 1 unused point」）");
        C(sk.warlordLifeChange, 0, "A20 遭遇 `warlordLifeChange` = **0**（「10 less Health」是相对经典 +10；SO 基准就是这一档）");
        Tr(!sk.showMulligan, "A21 遭遇 `showMulligan` = **false**（`Skirmish/Help`「No mulligan」）");
        C(sk.handLimit, 8, "A22 遭遇 `handLimit` = **8**（只有非官方规则书:58 一条支撑 —— 如实标着）");
        C(sk.startingHand, 3, "A23 遭遇 `startingHand` = **3**（原版 `DefaultScenario.startingHand`=3，与非官方规则书:58 的 4 不同）");   // ✅ 已复刻（2026-11-04 · B141 · DefaultScenario）
        C(sk.overtimeTurn.HasValue ? sk.overtimeTurn.Value : -1, 8,
          "A24 遭遇 `overtimeTurn` = **8**（🔶 自己设计：官方文案只说 `earlier`；推导见 B 组）");
        C(GameplayVariables.SkirmishOvertimeTurn, 8, "A25 常量 `SkirmishOvertimeTurn` = **8**（与 A24 同源）");
        Tr(sk.IsSkirmish, "A26 遭遇实例的 `IsSkirmish` = true（判据 = `deckSize < 30`，不是「是不是那个实例」）");

        // ---- 另外两档（教程 / 轮抽）沿用哪一份 ----
        var tu = GameplayVariables.Tutorial;
        C(tu.startingHand, 3, "A27 教程 `startingHand` = **3**（`TutorialScenario.startingHand`）");
        C(tu.secondExtraCards, 0, "A28 教程 `secondExtraCards` = **0**（`TutorialScenario.secondExtraCards`）");
        C(tu.handLimit, 10, "A29 教程 `handLimit` = **10**（`TutorialScenario.maxCardsInHand`）");
        Tr(!tu.showMulligan, "A30 教程 `showMulligan` = false（教程走 `_TutorialStartSequence`，根本没有换牌阶段）");
        var cd = GameplayVariables.ClosedDeck;
        C(cd.deckSize, 12, "A31 轮抽 `deckSize` = **12**（`Skirmish.Clone()` ⇒ 12 张那一套）");
        C(cd.overtimeTurn.HasValue ? cd.overtimeTurn.Value : -1, 8, "A32 轮抽 `overtimeTurn` = **8**（克隆所得 ⇒ 跟着遭遇）");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  B. 加时阈值 —— 行为必须在全局第 18（经典）/ 第 8（遭遇）回合触发
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void B_Overtime()
    {
        // ---- B1 经典：第 17 回合还不能进、第 18 回合进 ----
        var vc = GameplayVariables.Classic.Clone();
        var c1 = Fixture(vc);
        RuleCore.BeginTurn(c1);
        int g1 = 0;
        while (c1.Turn < 17 && g1++ < 60) { RuleCore.EndTurn(c1); RuleCore.BeginTurn(c1); }
        C(c1.Turn, 17, "B1a 尺子：停在**全局第 17 回合**");
        Tr(!c1.IsOvertime, "B1b ★ 全局第 17 回合**还不能进加时**（经典阈值 18）｜🧨 阈值若改回 10 ⇒ 这里早就进加时了");
        RuleCore.EndTurn(c1); RuleCore.BeginTurn(c1);
        C(c1.Turn, 18, "B1c 尺子：现在 = **全局第 18 回合**");
        Tr(c1.IsOvertime, "B1d ★ **全局第 18 回合进加时**（客户端常量 `overtimeStartTurn = 18`）");

        // ---- B2 遭遇：第 7 回合不进、第 8 回合进 ----
        var vs = GameplayVariables.Skirmish.Clone();
        var c2 = Fixture(vs);
        RuleCore.BeginTurn(c2);
        int g2 = 0;
        while (c2.Turn < 7 && g2++ < 40) { RuleCore.EndTurn(c2); RuleCore.BeginTurn(c2); }
        C(c2.Turn, 7, "B2a 尺子：停在**全局第 7 回合**");
        Tr(!c2.IsOvertime, "B2b ★ 遭遇：全局第 7 回合**还不能进加时**（阈值 8）");
        RuleCore.EndTurn(c2); RuleCore.BeginTurn(c2);
        C(c2.Turn, 8, "B2c 尺子：现在 = **全局第 8 回合**");
        Tr(c2.IsOvertime, "B2d ★ 遭遇：**全局第 8 回合进加时**（后手第 4 个自己的回合 = 能量首次 ≥ 10）");

        // ---- B3 「更早」= 数值上更早（官方文案 `Overtime begins earlier` 由机制复现）----
        Tr(vs.overtimeTurn.HasValue && vc.overtimeTurn.HasValue && vs.overtimeTurn.Value < vc.overtimeTurn.Value,
           "B3 ★ 遭遇阈值 **< 经典阈值**（" + (vs.overtimeTurn.HasValue ? vs.overtimeTurn.Value.ToString() : "null")
         + " < " + (vc.overtimeTurn.HasValue ? vc.overtimeTurn.Value.ToString() : "null")
         + "）—— `Event_Description/Skirmish/Help`「Overtime begins earlier」现在**由机制复现**"
         + "｜🧨 两档同值（2026-11-02 之前都是 10）⇒ 本行红");

        // ---- B4 照原版形状：`null` ⇒ 这一局永不进加时 ----
        var vn = GameplayVariables.Classic.Clone();
        vn.overtimeTurn = null;
        var c3 = Fixture(vn);
        RuleCore.BeginTurn(c3);
        int g3 = 0;
        while (c3.Turn < 30 && !c3.IsOver && g3++ < 80) { RuleCore.EndTurn(c3); RuleCore.BeginTurn(c3); }
        Tr(!c3.IsOvertime, "B4 ★ `overtimeTurn = null` ⇒ **永不进加时**（原版拿 `null` 表达「服务器不下发」；这条守住形状没被改坏）");

        // ---- B5 加时那一回合就多抽 1 张（判据 `TurnStartCardDraw.c:64-70`）----
        var vd = GameplayVariables.Skirmish.Clone();
        var c4 = Fixture(vd);
        RuleCore.BeginTurn(c4);
        int g4 = 0;
        while (c4.Turn < 7 && g4++ < 40) { RuleCore.EndTurn(c4); RuleCore.BeginTurn(c4); }
        C(c4.Turn, 7, "B5a 尺子：停在**全局第 7 回合**（先手那一回合）");
        int who = c4.SecondSeat;
        RuleCore.EndTurn(c4);                        // 结束第 7 回合 ⇒ 下一个开局的是后手（全局第 8 回合）
        int dk0 = c4.Players[who].Deck.Count;
        RuleCore.BeginTurn(c4);
        Tr(c4.IsOvertime, "B5b （前提）这是加时**第一**个回合");
        C(c4.Players[who].Deck.Count, dk0 - (DeckRules.DrawPerTurn + DeckRules.OvertimeExtraDraw),
          "B5c ★ 进入加时的那一回合就抽 **" + (DeckRules.DrawPerTurn + DeckRules.OvertimeExtraDraw)
        + "** 张（基础 " + DeckRules.DrawPerTurn + " + 加时 " + DeckRules.OvertimeExtraDraw + "）｜🧨 少抽那一张 ⇒ 红");
    }

    /// <summary>一局最小对局：两个督军 + **40 张填充牌**（牌库抽不空 ⇒ 加时那几回合真的在抽牌；血量拉高 ⇒ 跑得完 18 个回合）。</summary>
    static BattleContext Fixture(GameplayVariables vars)
    {
        var a = new List<CardDef> { Hero("B120W12Hero0", "SpaceWolves") };
        var b = new List<CardDef> { Hero("B120W12Hero1", "Goff") };
        for (int i = 0; i < 40; i++)
        {
            a.Add(Filler("B120W12A" + i));
            b.Add(Filler("B120W12B" + i));
        }
        return RuleCore.NewBattle(a, b, seed: 0, shuffle: false, vars: vars);
    }

    static CardDef Filler(string name)
    {
        return new CardDef(name, name, "unit", "", null, "SpaceWolves", 0, 1, 1, 0, null, subtype: "Infantry");
    }

    static CardDef Hero(string name, string faction)
    {
        // ⚠️ `subtype` 必须给 `"Warlord"`（与 `RuleEngineTest.HeroOf` 同口径）；血量拉高只为让 18 个回合跑得完
        return new CardDef(name, name, "hero", "", null, faction, 0, 2, 9999, 0, null, subtype: "Warlord");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  C. AI 牌池两维 —— 本地 224 副的分档交叉表 + 「难度不参与挑选」
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void C_DeckPool()
    {
        var all = PrebuiltDecks.Decks;
        var bots = PrebuiltDecks.BotDecks;
        Tr(all != null && all.Count > 0, "（C0 前提）`PrebuiltDecks.Decks` 非空（读 `Resources/prebuilt_deck_lists.json`）");
        if (all == null || all.Count == 0) return;

        C(all.Count, 224, "C1 原版 `decks`（非教程副）= **224**（`bundle_prebuiltdecks` 236 − 12 副名字含 Tutorial 的）");
        C(bots.Count, 196, "C2 原版 `botDecks`（`Type == Bot`）= **196**");

        // ---- C3 交叉表（`B120-OFF` §二·B，剔 12 副 Tutorial 之后）----
        int[,] want = { { 0, 0, 12 }, { 0, 5, 15 }, { 0, 10, 74 }, { 0, 15, 17 },
                        { 13, 5, 26 }, { 13, 10, 40 }, { 13, 15, 40 } };
        string tbl = "";
        bool allOk = true;
        for (int i = 0; i < want.GetLength(0); i++)
        {
            int mode = want[i, 0], dif = want[i, 1], n = 0;
            foreach (var d in all) if (d != null && d.gameMode == mode && d.difficulty == dif) n++;
            tbl += "(" + mode + "," + dif + ")=" + n + (i + 1 < want.GetLength(0) ? " · " : "");
            if (n != want[i, 2]) allOk = false;
        }
        Tr(allOk, "C3 ★ 分档交叉表与 `B120-OFF` §二·B 逐格一致（现读 " + tbl + "）");
        int c118 = 0, s106 = 0, s0 = 0;
        foreach (var d in all)
        {
            if (d == null) continue;
            if (d.gameMode == 0) c118++;
            if (d.gameMode == 13) { s106++; if (d.difficulty == 0) s0++; }
        }
        C(c118, 118, "C4 经典（gameMode 0）合计 = **118**");
        C(s106, 106, "C5 遭遇（gameMode 13）合计 = **106**");
        C(s0, 0, "C6 ★ 遭遇**没有 difficulty=0（SuperEasy）的副** —— 这是 `GetDeckByDifficulty(0,13)` 恒 null 的根因");

        // ---- C7 原版 `GetDeckByDifficulty` 的照抄行为（下标取自未过滤表 ⇒ 常 null）----
        C(PrebuiltDecks.FilterByDifficulty(0, 13).Count, 0, "C7 `FilterByDifficulty(0, 13)` = **0 条**（SuperEasy 没有 12 张副）");
        Tr(PrebuiltDecks.GetDeckByDifficulty(0, 13, new System.Random(7)) == null,
           "C8 ★ `GetDeckByDifficulty(0,13)` = **null**（原版自带的缺陷：下标取自**未过滤**的 224 条 ⇒ 照抄不修）");
        var f = PrebuiltDecks.FilterByDifficulty(10, 0);
        bool fOk = f.Count > 0;
        foreach (var d in f) if (d.difficulty != 10 || d.gameMode != 0) fOk = false;
        Tr(fOk, "C9 `FilterByDifficulty(10, 0)` 逐副都满足 `difficulty==10 && gameMode==0`（" + f.Count + " 条）");

        // ---- C10 我们的挑牌链两维 = 阵营 × 模式；`difficulty` **不参与** ----
        var m0 = PrebuiltDecks.FilterDecks(playMode: 0);
        var m13 = PrebuiltDecks.FilterDecks(playMode: 13);
        C(m0.Count, 118, "C10 `FilterDecks(playMode: 0)` = **118**（模式闸生效）");
        C(m13.Count, 106, "C11 `FilterDecks(playMode: 13)` = **106**");
        var dset = new HashSet<int>();
        foreach (var d in m0) dset.Add(d.difficulty);
        bool four = dset.Contains(0) && dset.Contains(5) && dset.Contains(10) && dset.Contains(15);
        Tr(four, "C12 ★★ **`difficulty` 不是筛选维**：`FilterDecks(playMode: 0)` 里四个难度（0/5/10/15）**全在**"
                + "（现读 " + dset.Count + " 个）—— 原版 `MatchData.GetBotDeck` 那一支同样不按难度挑（难度只在 `GetDeckByDifficulty`，"
                + "而它只挂在 unknown-matchType 兜底上）｜🧨 给 `FilterDecks` 加一道难度闸 ⇒ 本行红");

        // ---- C13 `GetRandomDeck`：模式闸真的生效 + 等概率（不是恒取第一个）----
        var ids = new HashSet<string>();
        bool modeOk = true; int nulls = 0;
        for (int s = 0; s < 24; s++)
        {
            var d = PrebuiltDecks.GetRandomDeck(new System.Random(s), playMode: 13, faction: "SpaceWolves");
            if (d == null) { nulls++; continue; }
            if (d.gameMode != 13 || d.faction != "SpaceWolves") modeOk = false;
            if (!string.IsNullOrEmpty(d.deckId)) ids.Add(d.deckId);
        }
        C(nulls, 0, "C13 24 个种子、`playMode: 13 + faction: SpaceWolves` ⇒ **一次都不返回 null**");
        Tr(modeOk, "C14 ★ 抽到的每一副都满足 `gameMode == 13 && faction == SpaceWolves`（模式闸 + 阵营闸）｜🧨 删掉 playMode 那道闸 ⇒ 红");
        Tr(ids.Count > 1, "C15 ★ 24 个种子抽出 **" + ids.Count + " 种不同的副** ⇒ 不是「恒取过滤后第 0 个」"
                       + "｜🧨 把 `rng.Next(cand.Count)` 改成常量 0 ⇒ 本行红");

        // ---- C16 接线闸：司机那条链只能走 `GetRandomDeck`（带模式），不许走 `GetDeckByDifficulty` ----
        string drv = Read(SrcDriver);
        Tr(drv != null, "（C16 前提）读得到 `" + SrcDriver + "`");
        if (drv != null)
        {
            Tr(drv.IndexOf("PrebuiltDecks.GetRandomDeck(new System.Random(seed), playMode: gameMode, faction: faction)",
                           StringComparison.Ordinal) >= 0,
               "C16 ★ `PickBotDeck` 走的是 `GetRandomDeck(…, playMode: gameMode, faction: faction)`（两维 = 阵营 × 模式）");
            Tr(drv.IndexOf("GetDeckByDifficulty", StringComparison.Ordinal) < 0,
               "C17 ★ `BattleDriver.cs` 里**没有** `GetDeckByDifficulty`（难度那一支不参与 bot 挑牌；它只服务 unknown-matchType 兜底）");
        }
        string pb = Read(SrcPrebuilt);
        Tr(pb != null, "（C18 前提）读得到 `" + SrcPrebuilt + "`");
        if (pb != null)
        {
            int i = pb.IndexOf("public static List<Deck> FilterDecks(", StringComparison.Ordinal);
            int j = i < 0 ? -1 : pb.IndexOf("\n        }", i, StringComparison.Ordinal);
            string body = (i >= 0 && j > i) ? pb.Substring(i, j - i) : null;
            Tr(body != null, "（C19 前提）切得出 `FilterDecks` 的方法体");
            if (body != null)
                Tr(body.IndexOf("difficulty", StringComparison.Ordinal) < 0,
                   "C19 ★ `FilterDecks` 的方法体里**没有** `difficulty` 这个词 ⇒ 难度确实不是筛选维");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  D. `B120-E10` 来源回填在场（注释里真的带着 11 格的来源键）
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void D_DocBackfill()
    {
        string src = Read(SrcVars);
        Tr(src != null, "（D0 前提）读得到 `" + SrcVars + "`");
        if (src == null) return;

        Guard(src, "Event_Description/Classic/Help", "D1 经典那一串官方文案的**键名**在注释里（`B120-OFF` §附一的来源坐标）");
        Guard(src, "Event_Description/Skirmish/Help", "D2 遭遇那一串官方文案的**键名**在注释里");
        Guard(src, "Tips/OvertimeTip", "D3 `drawCardsPerTurn` 的判据键（`Draw 2 cards per turn` ⇒ 基础 1）在注释里");
        Guard(src, "MenuShop/ExtraLegendaryWarning", "D4 `numberOfCopiesLegendary = 1` 的官方判据键在注释里");
        Guard(src, "MinionManager.slotsPerSide=4", "D5 序列化资产的来源（13 场 ×2 个 `MinionManager` 全 = 4）在注释里");
        Guard(src, "GameStaticData.overtimeStartTurn = 18", "D6 加时阈值的客户端常量与出处写在注释里");
        Guard(src, "SkirmishOvertimeTurn = 8", "D7 遭遇阈值常量 `SkirmishOvertimeTurn = 8` 在场");
        Guard(src, "11 格", "D8 订正句在场：「18 格里 11 格已由官方文案/序列化资产定出真值」（旧说法「全部只能自定」已作废）");

        Tr(src.IndexOf("18 个值本身全在服务器", StringComparison.Ordinal) < 0,
           "D9 ★ 那句**错的**「18 个值本身全在服务器 ⇒ 抄不到」已不在（铁律 5 就地订正）");
        Tr(src.IndexOf("两个模式都是 10", StringComparison.Ordinal) < 0,
           "D10 ★ 旧口径「两个模式都是 10」已不在（E08 之后经典 18 / 遭遇 8）");
    }

    // ============================================================ 小工具
    static string Read(string path)
    {
        try { if (File.Exists(path)) return File.ReadAllText(path); }
        catch (Exception e) { Debug.LogWarning("[B120W12] 读不到源码（" + e.GetType().Name + "）：" + path); }
        return null;
    }

    static void Guard(string src, string need, string msg)
    {
        Tr(src.IndexOf(need, StringComparison.Ordinal) >= 0, msg + "（要找的片段：「" + need + "」）");
    }
}
