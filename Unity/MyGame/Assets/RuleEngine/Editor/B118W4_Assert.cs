// B118W4_Assert.cs — 执行代理 `B118_W4`（2026-11-02）· 引擎/表现两件
//
// ============================ 它盯的是什么 ============================
//  A. `B117-46` —— `DoReloadDuty`（`Reload the Duty abilities`）改成**按目标表逐张清**：
//     · 不再按 `duty` trait 筛（原版 `BattleManager__ResolveResetDuty.c:5-9` 对传进来的那张
//       **无条件**清 `+0x4c`；`BattleManager__AddResetDuty.c:34/98` 只组一条 `BattleAction(0x57)` 入队、无 trait 判断）；
//     · 目标 = 己方场上**全部存活单位（含督军 `Board[4]`）** / 单目标档 = `ctx.LastTarget`；
//     · **失败档已删**（原版没有「没找到可装填单位 ⇒ 这条没生效」这条分支）。
//     判据全文 → `资料/完全复刻/报告/B117_F13_阻塞断言与剩余trait.md` §三 ·
//                `资料/完全复刻/判据/B118_对账_B_真缺口6条.md` 的 `B117-46` 行。
//
//  B. `B117-41` —— `EventTiming.DeathHold` 归零：原版阵亡协程收尾即放行，每具尸体不再白多 0.1 s。
//     判据 → `资料/完全复刻/判据/B117_G4G6_战斗卡牌链与节奏.md` §二·5。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A1/A2：把 `Has(KeywordTable.Duty)` 加回 `DoReloadDuty` ⇒ **不带 duty** 那个单位的位不会被清 ⇒ A1 红；
//            A2（带 duty 的照常清）仍在 ⇒ 两条一起看，证明「按目标表清」而不是「删了整段」。
//  · A3：把「空目标表」那个失败档写回来（`unresolved.Add` / `return false`）⇒ A3 红。
//  · A4：单目标档若又按 trait 筛 ⇒ A4 红（`LastTarget` 是不带 duty 的单位）。
//  · A5：若只清 `Board[0..3]`、漏掉督军格 ⇒ A5 红（`for s < BoardSpec.Size` 天然含 `Board[4]`）。
//  · B2 与 B1 **互相锁死**：把 `DeathHold` 改回 0.1 ⇒ B1 红；把 `case EvtKind.Death` 写死 0.1 ⇒ B2 红
//    （B2 比的是 `DelayBetween(Hit→Death)` 与 `DeathHold` **本身**，不是比字面 0）。
//
// ============================ 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）============================
//   建议宿主 = `Editor/RuleEngineTest.Run`（纯引擎夹具，零场景/零存档/零 `BattleDriver` 前提）。
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod RuleEngineTest.Run -logFile -
//
//   判绿红：`grep "^\[B118W4\]"` 的**合计行**；逐条按**行首** `✓` / `✗` 数（⛔ 别 `grep -c ✗`）。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B118_W4`：`B117-46`（`DoReloadDuty` 按目标表逐张清）· `B117-41`（`DeathHold` 归零）。</summary>
public static class B118W4_Assert
{
    const string P = "[B118W4] ";

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get; private set; }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get; private set; }

    static void Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); }
        else { Fail++; Debug.LogError(P + "   ✗ " + msg); }
    }

    static void Eq(float got, float want, float tol, string msg)
    {
        Ok(Mathf.Abs(got - want) <= tol,
           msg + "（期望 " + want.ToString("F4") + " ± " + tol.ToString("F4")
               + "；实得 " + got.ToString("F4") + "）");
    }

    static void Sec(string t) { Debug.Log(P + "── " + t); }

    // ============================================================ 夹具

    static CardDef Hero(string id) { return new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0); }
    static CardDef Troop(string id) { return new CardDef(id, id, "unit", "", "common", "Test", 1, 2, 5, 0); }

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B118W4Warlord0") };
        var d1 = new List<CardDef> { Hero("B118W4Warlord1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    /// <summary>往棋盘摆一个单位。⚠️ `Exhausted = false` 必须显式写（`UnitState` 的 ctor 恒 `true`）。</summary>
    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = false };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        u.SetOwnerSeat(p);
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    static EffectOp ReloadOp(string payload)
    {
        return new EffectOp
        {
            Verb = "reloadduty",
            Source = "Reload the Duty abilities",
            Payload = payload,
        };
    }

    /// <summary>走**生产入口**（`EffectDispatch` 那张表），⛔ 不直调私有方法 —— 保证测的是接线后的那一条。</summary>
    static bool Reload(BattleContext ctx, int owner, string payload, List<string> unresolved)
    {
        return RuleCore.EffectDispatch["reloadduty"](ctx, owner, "测试", ReloadOp(payload), null, unresolved);
    }

    // ============================================================ 入口

    public static void Run()
    {
        _reset();
        Sec("B118_W4：`B117-46`（按目标表逐张清）· `B117-41`（`DeathHold` 归零）");
        try { SectionA_ReloadDuty(); }
        catch (System.Exception e)
        {
            Ok(false, "A 段抛异常（后面的断言**没跑到**，⛔ 别读成通过）："
                    + e.GetType().Name + ": " + e.Message);
        }
        try { SectionB_DeathHold(); }
        catch (System.Exception e)
        {
            Ok(false, "B 段抛异常（后面的断言**没跑到**，⛔ 别读成通过）："
                    + e.GetType().Name + ": " + e.Message);
        }
        Debug.Log(P + "合计：" + Pass + " 通过 / " + Fail + " 失败");
    }

    static void _reset() { Pass = 0; Fail = 0; }

    // ============================================================ A · `B117-46`

    static void SectionA_ReloadDuty()
    {
        Sec("A · `B117-46`：`DoReloadDuty` 按目标表逐张清（不按 `duty` 筛、无失败档）");

        // ---- A0 前提：棋盘上真有「不带 duty」的单位，且它的位是可置真的 ----
        var ctx = NewCtx();
        var plain = Place(ctx, 0, 3, Troop("B118W4Plain"));   // 不带 duty
        var duty = Place(ctx, 0, 5, Troop("B118W4Duty"));
        duty.AddKeyword(KeywordTable.Duty, 1);
        plain.ActiveAbilityUsed = true; plain.DutyUsed = true;
        duty.ActiveAbilityUsed = true; duty.DutyUsed = true;

        Ok(!plain.Has(KeywordTable.Duty),
           "前提 A0a：`B118W4Plain` 身上**没有** `duty`（`Has(\"duty\") = 假`）");
        Ok(duty.Has(KeywordTable.Duty),
           "前提 A0b：`B118W4Duty` 身上**有** `duty`（`AddKeyword(duty,1)` 真的写进了 `_keywords`）");
        Ok(plain.ActiveAbilityUsed && plain.DutyUsed,
           "前提 A0c：那个不带 duty 的单位的两个「用过」位**都置真**了（否则 A1 的「清掉了」是假绿）");

        // ---- A1/A2：`all` 档 ----
        var un = new List<string>();
        bool ret = Reload(ctx, 0, "all", un);

        Ok(ret, "A1a `all` 档返回 `true`（目标表非空）");
        Ok(!plain.ActiveAbilityUsed && !plain.DutyUsed,
           "★ A1 **不带 `duty` 的单位照样被清**（`ActiveAbilityUsed` / `DutyUsed` 都回假）"
         + "｜🧨 把 `Has(KeywordTable.Duty)` 加回筛选 ⇒ 这一行红（原版 `ResolveResetDuty.c:5-9` 不看 trait）");
        Ok(!duty.ActiveAbilityUsed && !duty.DutyUsed,
           "★ A2 **带 `duty` 的单位照常清**（对照：证明不是把整段删了）");
        Ok(un.Count == 0, "A1b `unresolved` 没被写（成功路径）｜实得 " + un.Count + " 条");

        // ---- A3：`all` 档也会清**督军**（`Board[WarlordSlot]`，⛔ 无额外特判）----
        var hero = ctx.Players[0].Board[BoardSpec.WarlordSlot];
        Ok(hero != null && hero.IsWarlord,
           "前提 A3a：督军就在 `Board[" + BoardSpec.WarlordSlot + "]` 上（`NewBattle` 摆的）");
        if (hero != null)
        {
            hero.ActiveAbilityUsed = true; hero.DutyUsed = true;
            var un3 = new List<string>();
            Reload(ctx, 0, "all", un3);
            Ok(!hero.ActiveAbilityUsed && !hero.DutyUsed,
               "★ A3 督军格也在清位范围内（`for s < BoardSpec.Size` 含 `Board[4]`）"
             + "｜🧨 只扫 `0..3` 或给督军加特判 ⇒ 这一行红");
        }

        // ---- A4：单目标档 —— 清 `ctx.LastTarget`，同样不看 trait ----
        var ctx2 = NewCtx();
        var tgt = Place(ctx2, 0, 6, Troop("B118W4Tgt"));      // 不带 duty
        tgt.ActiveAbilityUsed = true; tgt.DutyUsed = true;
        ctx2.LastTarget = tgt;
        var un4 = new List<string>();
        bool ret4 = Reload(ctx2, 0, "single", un4);
        Ok(ret4, "A4a 单目标档返回 `true`");
        Ok(!tgt.ActiveAbilityUsed && !tgt.DutyUsed,
           "★ A4 单目标档**照样清**一个不带 duty 的 `LastTarget`"
         + "｜🧨 单目标那一行还留着 `Has(KeywordTable.Duty)` ⇒ 这一行红");

        // ---- A5：**没有失败档** —— 目标表为空 ⇒ 返回 `true` 且不记 `unresolved` ----
        var ctx3 = NewCtx();
        ctx3.LastTarget = null;                                // 单目标档 + 无目标 = 目标表空
        var un5 = new List<string>();
        bool ret5 = Reload(ctx3, 0, "single", un5);
        Ok(ret5, "★ A5a 目标表为空 ⇒ 返回 `true`（原版没有「这条没生效」这个档）"
               + "｜🧨 把 `return false` 写回来 ⇒ 这一行红");
        Ok(un5.Count == 0, "★ A5b 目标表为空 ⇒ **不写 `unresolved`**｜实得 " + un5.Count + " 条"
                         + "｜🧨 把 `unresolved.Add(...)` 写回来 ⇒ 这一行红");
    }

    // ============================================================ B · `B117-41`

    static void SectionB_DeathHold()
    {
        Sec("B · `B117-41`：`DeathHold` 归零（原版阵亡协程收尾即放行）");

        Eq(EventTiming.DeathHold, 0f, 1e-6f,
           "★ B1 `EventTiming.DeathHold` = 0｜🧨 改回 0.1 ⇒ 这一行红");

        var hit = new BattleEvent { Kind = EvtKind.Hit };
        var death = new BattleEvent { Kind = EvtKind.Death };
        float gap = EventTiming.DelayBetween(hit, death, -1f);

        // 前提：这个口对**别的**档确实会给非 0（否则 B2 的 0 可能是「整个函数恒 0」的假绿）
        float premise = EventTiming.DelayBetween(
            new BattleEvent { Kind = EvtKind.Attack, Ranged = false },
            new BattleEvent { Kind = EvtKind.Hit }, -1f);
        Ok(premise > 1e-3f,
           "前提 B2a：同一个口对 `Attack → Hit` 给 **" + premise.ToString("F4") + " s**（> 0）"
         + "⇒ 下面那个 0 是 `Death` 那一支的值，不是函数恒 0");

        Eq(gap, EventTiming.DeathHold, 1e-6f,
           "★ B2 `DelayBetween(Hit → Death)` == `DeathHold`（**比的是常量本身，不是字面 0**）"
         + "｜🧨 在 `case EvtKind.Death` 里写死 0.1 ⇒ 这一行红（常量已是 0）");

        string why;
        bool sourced = EventTiming.IsSourced(EvtKind.Death, out why);
        Ok(sourced && why == null,
           "★ B3 `IsSourced(Death)` = **true** 且 `unsourcedPart = null`（这一档现在全有出处）"
         + "｜🧨 把 `unsourcedPart` 那段 / `return false` 写回去 ⇒ 这一行红；实得 why = "
         + (why == null ? "(null)" : why));

        // 对照：别的档仍会如实报「无出处」—— 证明 `IsSourced` 没被改成「一律 true」
        string why2;
        Ok(!EventTiming.IsSourced(EvtKind.Ability, out why2) && why2 != null,
           "★ B4 对照：`IsSourced(Ability)` 仍回 **false**（`AbilityLoadFailSeconds` 兜底不是原版值）"
         + "⇒ B3 的 true 是 `Death` 专属，不是「全放行」");
    }
}
