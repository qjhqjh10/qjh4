// B118W20_Assert.cs — 执行写手 `B118_W20`（2026-11-02 · 第一百一十八会话 · `B118_J10` 批 1）
//
// ============================ 它盯的是什么 ============================
//  A. 账 #58 —— **三本兄弟账的 `Permanent` 位**：原版 `CardScript__RemoveEffect.c:43-45` 第一句
//     `if (permanentBuff(+0x74) != 0) return;` ⇒ 带这一位的效果记录**任何撤法都摘不掉**。
//     我们原来只有 `_statMods` 有这个位，`_buffs`(TempBuff) / `_grants`(GrantRecord) /
//     `TraitEffectRecord` 三本没有 ⇒ jam 会把 `permanentBuff` 一并摘掉。
//     本文件钉：三本账各「永久的一条留下 · 非永久的一条照旧被摘」，以及它们连带的 `StatMod` 同进同退。
//     判据 → `d:/2/tools/decomp_full/CardScript__RemoveEffect.c:43-45` ·
//            `CardScript__ReactToUnitJammed.c:61-71`（jam 第 ① 步逐条 `RemoveEffect`）·
//            `CardScript__RemoveExtrinsicEffectsFrom.c:56`（按来源撤也经 `RemoveEffect`）。
//  B. 账 #63 —— `CleanupTargetSacrificeFor` 的**显式 `actionTarget` 通道**：原版观察者必须
//     `BattleAction.targetCard == 自己`（`CardScript__ResolveDeadCard.c:242`）；我们原来只能拿
//     `ctx.LastTarget` 兜底。现在 `Hurt` / `HurtCore` / `RemoveIfDead` 各带一个可选 `actionTarget`，
//     一路喂进 `CleanupDeaths(actionTarget:)`。本文件钉：**给了就按它**（献祭响）· **不给且 `LastTarget == null`** ⇒ 不响。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A2/A4/A6：把新增的 `Permanent` 闸从 `RemoveTempBuffCore` / `RevertGrants*` / `RemoveTraitEffect`
//    里去掉 ⇒ 那三条「永久的一条还在」当场红。
//  · A3/A5/A7 **与** A2/A4/A6 **互相锁死**：把闸写成「恒不摘」⇒ A3/A5/A7 红；
//    写成「恒摘」⇒ A2/A4/A6 红。**只写一半必有一半红**。
//  · B2/B3 同理互相锁死：`actionTarget` 不往下传 ⇒ B2 红；写成「恒用 `ctx.LastTarget`」⇒ B3 红。
//
// ============================ 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）============================
//   宿主 = `RuleEngineTest.Run`（纯引擎夹具，不碰 Unity 资源）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod RuleEngineTest.Run -logFile -
//   判绿红：`grep "^\[B118W20\]"` 的**合计行**；逐条按**行首** `✓` / `✗` 数（⛔ 别 `grep -c ✗`）。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行写手 `B118_W20`：三本账的 `Permanent`（账 #58）· 献祭的显式 `actionTarget` 通道（账 #63）。</summary>
public static class B118W20_Assert
{
    const string P = "[B118W20] ";

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get; private set; }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get; private set; }

    static void Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); }
        else { Fail++; Debug.LogError(P + "   ✗ " + msg); }
    }

    static void Sec(string t) { Debug.Log(P + "── " + t); }

    // ============================================================ 夹具

    static CardDef Hero(string id) { return new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0); }
    static CardDef Troop(string id) { return new CardDef(id, id, "unit", "", "common", "Test", 1, 2, 5, 0); }

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B118W20Warlord0") };
        var d1 = new List<CardDef> { Hero("B118W20Warlord1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    /// <summary>摆一个在场单位（槽位直接写棋盘；`CardStateOf` 由棋盘派生 ⇒ 天然 `IsInPlayState`）。</summary>
    static UnitState Place(BattleContext ctx, int owner, int slot, string id)
    {
        var u = new UnitState(ctx.NewInstance(Troop(id)), false) { Exhausted = false };
        ctx.Players[owner].Board[slot] = u;
        return u;
    }

    public static void Run()
    {
        Pass = 0; Fail = 0;
        Sec("B118_W20：三本账 Permanent（#58） · 献祭显式 actionTarget（#63）");
        try { SectionA_PermanentLedgers(); }
        catch (System.Exception e) { Ok(false, "A 段抛异常（后面没跑，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message); }
        try { SectionB_ActionTargetChannel(); }
        catch (System.Exception e) { Ok(false, "B 段抛异常（后面没跑，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message); }
        Debug.Log(P + "==== 合计：✓ " + Pass + " · ✗ " + Fail + " ====");
    }

    // ============================================================ A · 三本账的 Permanent（账 #58）

    static void SectionA_PermanentLedgers()
    {
        Sec("A · `_buffs` / `_grants` / `_traitEffects` 的 `Permanent`（原版 `permanentBuff(+0x74)`）");

        // ---- A1/A2/A3：限时增益（`_buffs`）----
        {
            var ctx = NewCtx();
            var u = Place(ctx, 0, 5, "B118W20_BuffHost");
            Ok(u != null && u.IsInPlayState(ctx), "A1 （前提）夹具摆好：一个在场单位");

            var keepBuff = new UnitState.TempBuff { Name = "attack", Value = 2, Owner = 0, Src = "B118W20", Permanent = true }; u.AddTempBuff(keepBuff);
            var dropBuff = new UnitState.TempBuff { Name = "attack", Value = 3, Owner = 0, Src = "B118W20", Permanent = false }; u.AddTempBuff(dropBuff);
            int atk0 = u.Attack;
            Ok(u.TempBuffs.Count == 2, "A1b （前提）两条限时增益都登记上了（实得 " + u.TempBuffs.Count + "）");

            u.RemoveAllAppliedEffectsForJam();

            Ok(u.TempBuffs.Count == 1 && u.TempBuffs[0].Permanent, "★ A2 `permanentBuff` 那条**活着**（jam 摘不掉）｜🧨 去掉 `RemoveTempBuffCore` 的 `b.Permanent` 闸 ⇒ 本行红（实得 " + u.TempBuffs.Count + " 条）");
            Ok(u.TempBuffs.Count == 1 && !ReferenceEquals(u.TempBuffs[0], dropBuff) && ReferenceEquals(u.TempBuffs[0], keepBuff), "★ A3 被摘掉的**确实是【非永久】那条**（剩的是永久那条的同一对象；不是「什么都不摘」）｜判据 `decomp_full/CardScript__RemoveEffect.c:43-45`（`permanentBuff(+0x74) != 0 ⇒ return`）· 摘除内核 `UnitState.cs:2188-2192`｜🧨 把闸写成「恒不摘」⇒ 本行红。🔴 2026-11-03 `B124-W7` 订正：原来断 `!TempBuffs[0].Permanent`，与上面 A2 在「只剩 1 条」时**逻辑互斥**（必有一红）⇒ 改成按对象身份断「非永久那条被摘了」");
            Ok(u.Attack == atk0 - 3, "A3b 数值同进同退：攻击 +2 还在、+3 已撤（实得 " + u.Attack + "，改前 " + atk0 + "）");
        }

        // ---- A4/A5：关键词带出来的属性增益（`_grants`）----
        {
            var ctx = NewCtx();
            var u = Place(ctx, 0, 5, "B118W20_GrantHost");
            Ok(u.PendingGrants.Count == 0, "A4a （前提）`_grants` 一开始是空的");

            u.RecordGrant("attack", 2, source: "B118W20-src", permanent: true);
            u.RecordGrant("attack", 3, source: "B118W20-src", permanent: false);
            int atk0 = u.Attack;
            Ok(u.PendingGrants.Count == 2, "A4b （前提）两条授予都登记上了（实得 " + u.PendingGrants.Count + "）");

            u.RemoveAllAppliedEffectsForJam();
            Ok(u.PendingGrants.Count == 1 && u.PendingGrants[0].Permanent, "★ A4 永久授予**活着**（jam ② 不摘）｜🧨 去掉 jam ② 的 `g.Permanent` 闸 ⇒ 本行红（实得 " + u.PendingGrants.Count + " 条）");
            Ok(u.Attack == atk0 - 3, "A4c 它连带的那条 `StatMod` 也没被 ④ 摘（实得 " + u.Attack + "，改前 " + atk0 + "）");

            // 按来源撤（原版 `RemoveExtrinsicEffectsFrom` → `RemoveEffect` ⇒ 同样早退）
            int grantedBefore = u.PendingGrants.Count;
            u.RevertGrantsFrom("B118W20-src");
            Ok(u.PendingGrants.Count == grantedBefore && grantedBefore == 1, "★ A4d 按**来源**撤也摘不掉它（原版走同一个 `RemoveEffect`）｜🧨 去掉 `RevertGrantsFrom` 的闸 ⇒ 本行红（实得剩 " + u.PendingGrants.Count + " 条）");
        }

        // ---- A6/A7：`addTrait` 记录（`TraitEffectRecord`）----
        {
            var ctx = NewCtx();
            var u = Place(ctx, 0, 5, "B118W20_TraitHost");
            // ⚠️ 用裸词 `"fast"` / `"flank"`（`KeywordTable` 里没有这两个常量，全仓都是字面量）；
            //    两个**不同**的 trait ⇒ 两条独立记录（同 trait 会被合并成一条）。
            var keep = u.ApplyTraitEffect("fast", 1, source: "B118W20",
                                          timingBits: UnitState.TimingBitUntilNextAttack, permanent: true);
            var drop = u.ApplyTraitEffect("flank", 1, source: "B118W20",
                                          timingBits: UnitState.TimingBitUntilNextAttack, permanent: false);
            Ok(u.TraitEffects.Count == 2 && keep != null && drop != null, "A6a （前提）两条 `untilNextAttack` 记录都建了（实得 " + u.TraitEffects.Count + "）");

            int expired = u.ExpireUntilNextAttack();
            Ok(expired == 1 && u.TraitEffects.Count == 1, "★ A6 `permanentBuff` 那条**不随 `untilNextAttack` 到期**（原版 `RemoveEffect` 第一句早退）｜🧨 去掉 `RemoveTraitEffect` 的 `rec.Permanent` 闸 ⇒ 本行红（实得到期 " + expired + " 条 · 剩 " + u.TraitEffects.Count + "）");
            Ok(u.TraitEffects.Count == 1 && u.TraitEffects[0].Permanent && ReferenceEquals(u.TraitEffects[0], keep), "★ A7 掉的是**非永久那条**（剩的就是 `keep`）｜🧨 把闸写成「恒不摘」⇒ 本行红");
        }
    }

    // ============================================================ B · 献祭的显式 `actionTarget` 通道（账 #63）

    static void SectionB_ActionTargetChannel()
    {
        Sec("B · `CleanupDeaths(actionTarget:)` → `CleanupTargetSacrificeFor`（原版 `ResolveDeadCard.c:242`）");

        // 摆盘：P0 格 5 = 观察者（带 `sacrifice`）· P1 格 5 = 被打死的那个
        System.Func<int, BattleContext, UnitState, UnitState, int> run = (passTarget, c, obs, victim) =>
        {
            int before = c.Events.Count;
            RuleCore.HurtWithSourceForTest(c, victim, 99, "B118W20 的测试伤害", killer: 0,
                                           actionTarget: passTarget == 1 ? obs : null);
            int n = 0;
            for (int i = before; i < c.Events.Count; i++) if (c.Events[i] != null && c.Events[i].Contains("触发**献祭**")) n++;
            return n;
        };

        {
            var ctx = NewCtx();
            ctx.Active = 0;                                   // 原版闸 ②：本方回合（`IsPlayerTurn == card.isPlayer`）
            var obs = Place(ctx, 0, 5, "B118W20_Observer");
            var victim = Place(ctx, 1, 5, "B118W20_Victim");
            obs.AddKeyword(KeywordTable.Sacrifice, 1);
            ctx.LastTarget = null;

            Ok(ctx.Active == 0 && obs.Has(KeywordTable.Sacrifice) && ctx.LastTarget == null,
               "B1 （前提）本方回合 · 观察者带 `sacrifice` · `LastTarget` 为空");

            int hits = run(1, ctx, obs, victim);
            Ok(!victim.IsAlive, "B1b （前提）被打的那个**真的死了**（否则本就是空转）");
            Ok(hits >= 1, "★ B2 给了 `actionTarget` ⇒ 观察者的献祭**响了**｜🧨 `Hurt`/`RemoveIfDead` 不往下传 `actionTarget` ⇒ 本行红（实得 " + hits + " 声）");
        }

        {
            var ctx = NewCtx();
            ctx.Active = 0;
            var obs = Place(ctx, 0, 5, "B118W20_Observer2");
            var victim = Place(ctx, 1, 5, "B118W20_Victim2");
            obs.AddKeyword(KeywordTable.Sacrifice, 1);
            ctx.LastTarget = null;

            int hits = run(0, ctx, obs, victim);
            Ok(!victim.IsAlive && hits == 0, "★ B3 不给 `actionTarget` 且 `LastTarget` 为空 ⇒ **不响**（旧兜底口径照旧）｜🧨 把 `CleanupTargetSacrificeFor` 写成「恒不依赖 actionTarget」⇒ 本行红（实得 " + hits + " 声）");
        }
    }
}
