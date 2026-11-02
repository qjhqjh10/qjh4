// RuleEngineTest_B117WMULL.cs — 执行代理 `B117-WMULL`（2026-11-02 · 第一百一十七会话）
//   文件所有权 = `RuleEngine/Core/RuleCore.cs`（只改 `Mulligan` / `EndMulligan` 的补抽次序与洗牌时机）
//                ＋ `CardPresentation/Battle/BattleDriver.cs`（只加「选环境卡那扇面板的倒计时」）
//                ＋ 本文件（新建）
//
// ============================ 它盯的是什么 ============================
//  · A 组：**换掉的牌回库走表尾、补抽走表头、换牌期一次都不洗**（原版 `PlayerHand.FinishMulligan`
//          那条链）。改前我们是 `Deck.Add`（在这套镜像约定下 = 加到**牌堆顶**）+ 当场 `Shuffle`
//          + 当场抽 ⇒ **换掉的牌有概率当次就被抽回来**（原版概率恒 0）。
//  · B 组：**选环境卡那扇面板的倒计时**由「换牌剩下的秒数」喂（原版 `_MulliganCountdown` 协程
//          `+0x4f9` 那一支）。改前它只被「回合表走完的 15 s」喂，而选环境卡发生在 `BeginTurn`
//          **之前** ⇒ 那扇面板的倒计时实机上永不出现。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//  权威判据文件 = `资料/完全复刻/判据/B117-G10-17_换牌Mulligan.md` §二·A / §二·B。
//  一手出处（A）：
//    · `d:/2/tools/decomp_full/PlayerHand__ReplaceMulliganCard.c`：卡改父件到牌堆后 **`List.Add`**
//      （= 追加到**表尾**）。
//    · `d:/2/tools/decomp_full/BattleManager__GetMulliganCard.c`：`get_Item(list, 0)` → `RemoveAt(list, 0)`
//      （= 从**表头**取）。
//    · `d:/2/tools/decomp_full/BattleManager._FinishMulliganFinalPhase_d__351__MoveNext.c:156-160`：
//      **case 3** 才 `ShuffleDeck` 两副（`playerGoesFirst` 与取反），**那时双方补牌都已完成**。
//  一手出处（B）：
//    · `BattleManager._MulliganCountdown_d__347__MoveNext.c:75`：`+0x4f9` 为真 ⇒
//      `ChooseCardMenu.CountdownTrigger(count)`。
//    · `BattleManager._SetupEnviromentalEffectPhase_d__352__MoveNext.c:28/106/107`：
//      `StopCoroutine(DAT_18423bb80)` → `ChooseCardMenu.Setup(...)` → `StartCoroutine(DAT_18423bb80)`
//      ⇒ 同一个协程字面量停完立刻重启（计数器**接着剩下的秒数走**，⚠️ 推断、无实况）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A1/A2：把 `ps.Deck.Insert(0, moved[i])` 改回 `ps.Deck.Add(...)`（= 加到牌堆顶）⇒ A2 红
//           （换掉的那两张**会**被当次抽回来）。把 `moved` 的收集循环改成降序 ⇒ A3 红
//           （表尾那两张的**镜像次序**会反）。
//  · A4：把洗牌留在 `Mulligan` 里（或 `EndMulligan` 里不洗）⇒ A4/A5 红（两态必须一真一假）。
//  · A6：`EndMulligan` 幂等 —— 第二次调用若再洗一次 ⇒ A6 红。
//  · B1/B2：`EnvPickTickDue` 的「秒数没变就不喂」拆掉 ⇒ B2 红（每帧都会重掷「没选就随机选一张」）。
//  · B3：把「不是选环境卡那扇」也喂 ⇒ B3 红（会抢回合时钟那条链的 `_lastChoiceTick`）。
//  · B4：`EnvPickSecondsAfter` 的 `Mathf.Max(0, …)` 拆掉 ⇒ B4 红（会喂负数）。
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B117-WMULL`：换牌补抽次序 + 选环境卡面板倒计时。</summary>
public static class RuleEngineTest_B117WMULL
{
    const string P = "[B117WMULL] ";

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get; private set; }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    static bool Eq(int got, int want, string msg)
    {
        if (got == want) { Pass++; Debug.Log(P + $"   ✓ {msg}（实际 {got}）"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}｜实际 {got}，期望 {want}"); return false;
    }

    static bool EqStr(string got, string want, string msg)
    {
        if (got == want) { Pass++; Debug.Log(P + $"   ✓ {msg}（实际 {got}）"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}｜实际「{got}」，期望「{want}」"); return false;
    }

    /// <summary>逐项比一串名字（顺序敏感）—— 换牌的**次序**判据全靠它。</summary>
    static bool EqSeq(string got, string want, string msg)
    {
        if (got == want) { Pass++; Debug.Log(P + $"   ✓ {msg}｜{got}"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}｜实际 [{got}]，期望 [{want}]"); return false;
    }

    public static void Run()
    {
        Pass = 0; Fail = 0;
        Debug.Log(P + "==== B117-WMULL：换牌补抽次序（表尾回库 / 表头补抽 / 换牌期不洗）+ 选环境卡倒计时 ====");
        SectionA_MulliganOrder();
        SectionB_EnvPickCountdown();
        Debug.Log(P + $"==== 合计：✓ {Pass} · ✗ {Fail} ====");
    }

    // ============================================================
    //  夹具
    // ============================================================
    static CardDef Hero(string name)
    {
        return new CardDef(name, name, "hero", "", null, "Test", 0, 0, 30, 0, new string[0]);
    }
    static CardDef Unit(string name)
    {
        return new CardDef(name, name, "unit", "", null, "Test", 1, 0, 1, 0, new string[0]);
    }

    /// <summary>牌库：`[督军, d00…d19, HAND_C, HAND_B, HAND_A]`。
    /// ⚠️ 我们抽牌取 `Deck[Count-1]`（= 牌堆顶）⇒ **表末那张先抽**：
    /// 起手 = `HAND_A, HAND_B, HAND_C`，起手之后的牌堆顶 = `d19`。</summary>
    static List<CardDef> RigDeck(string hero)
    {
        var d = new List<CardDef> { Hero(hero) };
        for (int i = 0; i < 20; i++) d.Add(Unit("d" + i.ToString("00")));
        d.Add(Unit("HAND_C"));
        d.Add(Unit("HAND_B"));
        d.Add(Unit("HAND_A"));
        return d;
    }

    static List<string> Names(IList<CardInstance> list)
    {
        var r = new List<string>();
        if (list == null) return r;
        for (int i = 0; i < list.Count; i++) r.Add(list[i] != null && list[i].Card != null ? list[i].Card.Name : "<null>");
        return r;
    }

    static string Seq(IList<string> list) { return string.Join(",", list); }
    static string Seq(string[] list) { return string.Join(",", list); }

    static string SortedSeq(IList<string> list)
    {
        var a = new List<string>(list);
        a.Sort(StringComparer.Ordinal);
        return string.Join(",", a);
    }

    // ============================================================
    //  A · 换牌补抽的次序 / 洗牌时机（`RuleCore.Mulligan` / `RuleCore.EndMulligan`）
    // ============================================================
    static void SectionA_MulliganOrder()
    {
        Debug.Log(P + "--- A · 换掉的牌回库走**表尾**、补抽走**表头**、换牌期**不洗** ---");

        var ctx = RuleCore.NewBattle(RigDeck("MyHero"), RigDeck("FoeHero"),
                                     seed: 7, shuffle: false, openMulligan: true);
        Ok(ctx.MulliganOpen, "A0 前提：`openMulligan: true` ⇒ 真的在换牌阶段（不然下面全是空转）");
        var ps = ctx.Players[0];

        var handBefore = Names(ps.Hand);
        var deckBefore = Names(ps.Deck);
        Ok(handBefore.Count == 3 && deckBefore.Count == 20,
           $"A0′ 前提：起手 {handBefore.Count} 张 · 牌库 {deckBefore.Count} 张"
           + "（原版起手 3 张；督军那一张在 `NewBattle` 里已被提成 `Warlord`、**不在牌库**）");
        EqSeq(Seq(handBefore), "HAND_A,HAND_B,HAND_C", "A0″ 前提：起手 = 牌堆顶三张（取 `Count-1` 的方向）");

        // 换掉手牌下标 1、2（= HAND_B / HAND_C），留 HAND_A
        int n = RuleCore.Mulligan(ctx, 0, new List<int> { 1, 2 });
        Eq(n, 2, "A1 `Mulligan` 换掉 2 张");

        var handAfter = Names(ps.Hand);
        var deckAfter = Names(ps.Deck);

        // ---- ① 换掉的牌**绝不能**当次抽回来（改前 `Deck.Add` = 加到牌堆顶 ⇒ 会抽回来）----
        bool backInHand = handAfter.Contains("HAND_B") || handAfter.Contains("HAND_C");
        Ok(!backInHand, $"★ A2 换掉的两张**没有**被当次抽回来（实际手牌 {Seq(handAfter)}）"
                      + "｜🧨 把 `Insert(0, …)` 改回 `Deck.Add` ⇒ 本条红");

        // ---- ② 补抽拿的是**原来牌堆顶**那两张 ----
        EqSeq(Seq(handAfter), handBefore[0] + "," + deckBefore[deckBefore.Count - 1] + "," + deckBefore[deckBefore.Count - 2],
              "★ A3 补抽抽走的是**原来的牌堆顶两张**（表头取；原版 `GetMulliganCard` = `GetItem(0)`）");

        // ---- ③ 换掉的牌在**表尾**（我们的表尾 = 下标 0），而且**镜像次序**对 ----
        //   原版：升序扫描手牌，逐张 `List.Add` ⇒ 表末依次是 B、C（C 在最末）。
        //   镜像：我们的下标 0 = 原版表末 ⇒ `[C, B, …]`。
        string bottom2 = deckAfter.Count >= 2 ? deckAfter[0] + "," + deckAfter[1] : "<不够>";
        EqSeq(bottom2, "HAND_C,HAND_B",
              "★ A4 回库的两张压在**牌堆底**、且按原版升序扫描的**镜像**排（下标 0 = 原版表末）"
              + "｜🧨 收集循环改成降序 ⇒ 本条红");

        // ---- ④ 换牌期**一次都不洗**：牌堆里剩下的相对次序逐张不变 ----
        var expectDeck = new List<string> { "HAND_C", "HAND_B" };
        for (int i = 0; i < deckBefore.Count - 2; i++) expectDeck.Add(deckBefore[i]);
        EqSeq(Seq(deckAfter), Seq(expectDeck),
              "★ A5 换牌期**不洗牌**（除回库那两张外，其余 18 张相对次序逐张不变）"
              + "｜🧨 把 `Shuffle(ps.Deck, ctx)` 加回 `Mulligan` ⇒ 本条红");

        // ---- ⑤ 洗牌挪到 `EndMulligan`：双方各一次（原版 `_FinishMulliganFinalPhase` case 3）----
        var d0Before = Names(ctx.Players[0].Deck);
        var d1Before = Names(ctx.Players[1].Deck);
        string sig0 = SortedSeq(d0Before), sig1 = SortedSeq(d1Before);

        RuleCore.EndMulligan(ctx);

        var d0After = Names(ctx.Players[0].Deck);
        var d1After = Names(ctx.Players[1].Deck);
        Ok(!ctx.MulliganOpen, "A6 `EndMulligan` 关掉了换牌阶段");
        EqStr(SortedSeq(d0After), sig0, "A7 先手那副**一张不多一张不少**（洗牌只是换序）");
        EqStr(SortedSeq(d1After), sig1, "A8 后手那副**一张不多一张不少**");
        // 21 张牌洗出「恰好原序」的概率 ≈ 1/21! ⇒ 这条不会闪
        Ok(Seq(d0After) != Seq(d0Before), "★ A9 `EndMulligan` **洗了先手那副**"
              + "｜🧨 从 `EndMulligan` 删掉那次 `Shuffle` ⇒ 本条红");
        Ok(Seq(d1After) != Seq(d1Before), "★ A10 `EndMulligan` **洗了后手那副**（原版 case 3 两副各一次）");

        // ---- ⑥ 幂等：再调一次不许再洗（`EndMulligan` 第一句就早退）----
        var d0Again = Names(ctx.Players[0].Deck);
        RuleCore.EndMulligan(ctx);
        EqSeq(Seq(Names(ctx.Players[0].Deck)), Seq(d0Again),
              "A11 再调一次 `EndMulligan` **不再洗**（幂等 —— 原版 `CompleteMulliganPhase` 也只跑一次）");
    }

    // ============================================================
    //  B · 选环境卡那扇面板的倒计时（源 = 换牌剩下的秒数）
    // ============================================================
    static void SectionB_EnvPickCountdown()
    {
        Debug.Log(P + "--- B · 选环境卡面板的倒计时（接着换牌剩下的秒数走；每秒只喂一次）---");

        // 秒数换算（原版逐秒 −1；下界钳 0）
        Eq(CardPresentation.BattleDriver.EnvPickSecondsAfter(22.4f), 23, "B1 剩余 22.4 s ⇒ 面板显示 **23**（`CeilToInt`，与 `TickMulligan` 同一口径）");
        Eq(CardPresentation.BattleDriver.EnvPickSecondsAfter(0.2f), 1, "B1′ 剩余 0.2 s ⇒ **1**");
        Eq(CardPresentation.BattleDriver.EnvPickSecondsAfter(0f), 0, "B1″ 剩余 0 ⇒ **0**");
        Eq(CardPresentation.BattleDriver.EnvPickSecondsAfter(-3f), 0, "★ B2 剩余为负 ⇒ **0**（⛔ 不许喂负数）｜🧨 拆掉 `Mathf.Max(0, …)` ⇒ 本条红");

        int sec;
        // 面板开着 + 是选环境卡那扇 + 秒数变了 ⇒ 喂
        Ok(CardPresentation.BattleDriver.EnvPickTickDue(true, true, -1, 22.4f, out sec) && sec == 23,
           "★ B3 面板开着、秒数从「没喂过」变成 23 ⇒ **要喂**（原版 `CountdownTrigger(count)` 每秒一次）");
        // 秒数没变 ⇒ 不喂（否则每帧都会重掷「没选就随机选一张」）
        Ok(!CardPresentation.BattleDriver.EnvPickTickDue(true, true, 23, 22.4f, out sec),
           "★ B4 秒数没变（还是 23）⇒ **不喂**｜🧨 去掉这道闸 ⇒ 每帧重掷选牌（静默、而且改随机流）");
        // 不是「选环境卡」那扇（= 战斗中的选牌 / 选一 / 选效果）⇒ 不碰（那条链归回合时钟）
        Ok(!CardPresentation.BattleDriver.EnvPickTickDue(true, false, -1, 22.4f, out sec) && sec == -1,
           "★ B5 「选牌」那扇（不是选环境卡）⇒ **不喂**、并且给调用方 `-1` 提示复位"
           + "｜🧨 去掉 `isEnvPick` 闸 ⇒ 抢回合时钟那条链的状态（两套状态混用）");
        // 面板没开 ⇒ 不喂
        Ok(!CardPresentation.BattleDriver.EnvPickTickDue(false, true, -1, 22.4f, out sec) && sec == -1,
           "B6 面板没开 ⇒ 不喂、复位");

        // `< 3` 自动确认那一档的边界（阈值由 `CardChoicePanel.CountdownTrigger` 判，这里只钉「喂进去的数是几」）
        Ok(CardPresentation.BattleDriver.EnvPickSecondsAfter(2.1f) == 3,
           "B7 剩余 2.1 s ⇒ 喂 **3** ⇒ **不**触发自动确认（原版 `param_2 < 3` 才确认）");
        Ok(CardPresentation.BattleDriver.EnvPickSecondsAfter(2.0f) == 2,
           "★ B8 剩余 2.0 s ⇒ 喂 **2** ⇒ 触发自动确认（原版 `< 3`）");
    }
}
