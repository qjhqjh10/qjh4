// RuleEngineTest_B117WGB.cs — 执行代理 `B117-WGB`（2026-11-02 · 第一百一十七会话 · 引擎线）
//
//   `B117-10`  `UM64 Angels of Death` **不该要求点目标格** —— 原版 `targeted = 0`，
//              而它 Codex 那一句在结构数据里是 `targetsAffected = 10 (allFiltered)`（场上**全部**匹配者）。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//   · `d:/2/tools/decomp_full/BattleManager__CanPlayCard.c` —— 费用判完之后：
//       `SupportMethods__IsSpellType(cardType)` 为假 **或** `EntityScript__get_targeted(card)` 为假 ⇒ 直接 `return 1`，
//       **根本不进**「遍历棋盘找合法法术目标」那个循环。`UM64` 的 `targeted = 0`（`cards_engine.json` / 原版 SO 同值）。
//   · `d:/2/tools/decomp_full/AbilityLogic__GetTargets.c:972-987` —— `targetsAffected == 0xa (10)` 那一支调
//       `BattleManager__GetUnitsInPlayList(...)`：**按 criteria 筛出场上全部**，完全不读 `targetCard`
//       （枚举名 `allFiltered`，`d:/2/Warpforge_code/Scripts/Assembly-CSharp/TargetsAffected.cs`）。
//   · 落点 = `EffectText.cs` 的 `Finish()` 里那条「几个」判据（`NameFilter` 非空 + `Side == "own"` ⇒ `Count = 0`）。
//
// ============================ 灭自证 ============================
//   · A3：`PickTarget(ops)` 必须为 **null**。改前这条 op 是 `Count = 1` ⇒ `PickTarget` 非 null ⇒ A3 红。
//   · A4：那条 op 的 `Count` 必须是 **0**（改前 1）。
//   · B2：`PlayTactic(..., targetSlot = -1)` 必须 `OK`（改前 `ErrNoTargetAvailable = 18`）。
//   · B3：**每一个**场上的原铸仲裁者都要 +1 远程（改前 `Count = 1` 只给一个、还要先点格 ⇒ 一个都给不了）。
//   · 全池影响面：只有 `UM64` 命中这条判据（`SAU52` 本来就是 `Count = 0`）⇒ `play all dump` 只该动它一张。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   宿主 = `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()`（本波由 `B117_WRED-RE` 占用 ⇒ 本代理**没有**加）：
//      Guard("RuleEngineTest_B117WGB", RuleEngineTest_B117WGB.Run);
//      _pass += RuleEngineTest_B117WGB.Pass; _fail += RuleEngineTest_B117WGB.Fail;
//   🔴 **没接上 = 一条都不跑**（⛔ 别当成已验收）。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B117-WGB`：`B117-10` —— `UM64 Angels of Death` 不要求目标格 + 全部己方原铸仲裁者 +1 远程。</summary>
public static class RuleEngineTest_B117WGB
{
    const string P = "[B117WGB] ";

    static int _pass, _fail;
    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + "   ✓ " + msg); }
        else { _fail++; Debug.LogError(P + "   ✗ " + msg); }
    }

    static void Eq(int got, int want, string msg)
    {
        Check(got == want, msg + "｜实际 " + got + "，期望 " + want);
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        try
        {
            Group("A·解析面", A_Parse);
            Group("B·结算面", B_Resolve);
        }
        catch (System.Exception e)
        {
            Check(false, "断言宿主抛异常（后面的条目没跑完）：" + e.GetType().Name + ": " + e.Message);
        }
        finally
        {
            Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败（共 {_pass + _fail} 条）===");
        }
    }

    static void Group(string tag, System.Action body)
    {
        try { body(); }
        catch (System.Exception e)
        {
            Check(false, tag + " 组抛异常（本组后面的断言没跑到，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message);
        }
    }

    // ============================================================ 夹具
    static List<CardDef> Pool() { return CardDatabase.Load(); }

    static CardDef Hero(string name, string faction)
    {
        return new CardDef(name, name, "hero", "", null, faction, 0, 2, 30, 0, null, subtype: "Warlord");
    }

    static CardDef Unit(string id, int atk, int hp, int ranged = 0)
    {
        return new CardDef(id, id, "unit", "", null, "Test", 1, atk, hp, ranged);
    }

    /// <summary>推进到「P1（`p = 0`）的第 n 个回合」（同 `RuleEngineTest.ToP1Turn`）。</summary>
    static void ToP1Turn(BattleContext ctx, int n)
    {
        int target = 2 * n - 1;
        int guard = 0;
        while (ctx.Turn != target || ctx.Active != 0)
        {
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            else { RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx); }
            if (++guard > 40) { Check(false, "回合推进超限，疑似死循环"); return; }
        }
    }

    static int HandIdx(BattleContext ctx, int p, string name)
    {
        var hand = ctx.Players[p].Hand;
        for (int i = 0; i < hand.Count; i++) if (hand[i].Card.Name == name) return i;
        return -1;
    }

    // ============================================================
    //  A · 解析面：`PickTarget` 必须放行（`Count == 0`）
    // ============================================================
    static void A_Parse()
    {
        Debug.Log(P + "--- A · 解析面（`EffectText.Finish` 的「具名卡的己方目标 = 全体」那条）---");

        var pool = Pool();
        var um64 = CardDatabase.FindById(pool, "UM64");
        Check(um64 != null, "（前提）卡池里找得到 `UM64`（Angels of Death）");
        if (um64 == null) return;

        List<string> un, pa;
        var ops = EffectText.Parse(um64.Desc, out un, out pa, card: um64);
        Eq(un.Count + pa.Count, 0,
           "（前提）`UM64` 的正文**完整可解析**（否则结算入口会先被 `ErrUnimplemented` 挡掉，后面几条就都是空验）");

        var pick = EffectText.PickTarget(ops);
        Check(pick == null,
              "★ A3 `PickTarget(ops)` = **null** ⇒ 出牌**不要求点目标格**（原版 `targeted = 0` ⇒ `CanPlayCard` 直接 `return 1`）"
            + "｜🧨 这条 op 回到 `Count = 1` ⇒ `PickTarget` 非 null ⇒ 红");

        // 找到那条「给己方原铸仲裁者」的 op（按卡名筛）
        EffectTargetSpec hit = null;
        int hits = 0;
        for (int i = 0; i < ops.Count; i++)
        {
            var t = ops[i] != null ? ops[i].Target : null;
            if (t == null || string.IsNullOrEmpty(t.NameFilter)) continue;
            if (!string.Equals(t.NameFilter, "Primaris Intercessor", System.StringComparison.OrdinalIgnoreCase)) continue;
            hits++; hit = t;
        }
        Eq(hits, 1, "A4 前提：`UM64` 只有**一条** op 带 `NameFilter = Primaris Intercessor`");
        if (hit == null) return;

        Eq(hit.Count, 0,
           "★ A4 那条 op 的 `Count` = **0**（= 全体）"
         + "｜判据 `AbilityLogic__GetTargets.c:972-987` 的 `targetsAffected == 10 (allFiltered)`"
         + "｜🧨 改前是 1（正文里的 `+1` 被 `\\b(\\d+)\\b` 当成了「取一个」）⇒ 红");
        Check(string.Equals(hit.Side, "own", System.StringComparison.Ordinal),
              "★ A4′ 那一侧的 `Side` = `own`（`your Primaris Intercessor`）—— 本批那条判据要求 `Side == own` 才放开：实得 `" + hit.Side + "`");
    }

    // ============================================================
    //  B · 结算面：不给格位也打得出去，且**全部**己方原铸仲裁者 +1 远程
    // ============================================================
    static void B_Resolve()
    {
        Debug.Log(P + "--- B · 结算面（`PlayTactic(targetSlot = -1)` + 生效范围）---");

        var pool = Pool();
        var um64 = CardDatabase.FindById(pool, "UM64");
        var um6 = CardDatabase.FindById(pool, "UM6");
        Check(um64 != null && um6 != null, "（前提）卡池里找得到 `UM64` 与 `UM6`（Primaris Intercessor）");
        if (um64 == null || um6 == null) return;
        Eq(um6.RangedAttack, 2, "（前提）`UM6` 的远程基础值 = 2（卡表实测 2/3/2）");

        var d0 = new List<CardDef> { Hero("B117WGB_Hero", "Ultramarines") };
        var d1 = new List<CardDef> { Hero("B117WGB_Foe", "Goff") };
        // ⚠️ 带 `cardPool`：`Deploy 3 Primaris Intercessor` 要从池子里取卡。
        var ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: pool);
        Check(ctx != null, "（前提）`NewBattle` 建起来了");
        if (ctx == null) return;

        ToP1Turn(ctx, 1);
        // 🔴 能量必须**刚好等于卡费**：`Codex:` 的条件是「结算那一刻你的能量为 0」
        //    （`EffectResolver.ConditionHolds` 的 `energyzero` 支）⇒ 给 20 会让这一条**永远不触发**。
        ctx.Players[0].Energy = um64.Cost;
        ctx.Players[0].MaxEnergy = 20;

        ctx.Players[0].Hand.Add(ctx.NewInstance(um64));
        int hi = HandIdx(ctx, 0, "Angels of Death");
        Check(hi >= 0, "（前提）`Angels of Death` 在手里（下标 " + hi + "）");
        if (hi < 0) return;

        // 场上放一个**不是**原铸仲裁者的己方单位 —— 它**不该**吃到这个 buff
        var by = new UnitState(ctx.NewInstance(Unit("B117WGB_Bystander", 1, 1, 0)), false) { Exhausted = false };
        by.DeployedTurn = ctx.Turn;
        by.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[0].Board[6] = by;
        Eq(by.RangedAttack, 0, "（前提）旁观单位的远程 = 0");

        int rc = RuleCore.PlayTactic(ctx, 0, hi);      // targetSlot 缺省 = -1 ⇒ **不给格位**
        Eq(rc, RuleCodes.OK,
           "★ B2 **不给目标格**也打得出去（改前 `ErrNoTargetAvailable = 18`）"
         + "｜判据 `BattleManager__CanPlayCard.c`：`targeted = 0` ⇒ 不进找目标格那段");

        // 台上所有的原铸仲裁者都该 +1 远程（`Deploy 3` 会把它们放上场，`allFiltered` 一条给全）
        int n = 0, bad = 0;
        for (int s = 0; s < ctx.Players[0].Board.Length; s++)
        {
            var u = ctx.Players[0].Board[s];
            if (u == null || u.Card == null) continue;
            if (!string.Equals(u.Card.Name, "Primaris Intercessor", System.StringComparison.Ordinal)) continue;
            n++;
            if (u.RangedAttack != 3) bad++;
        }
        Check(n >= 2,
              "B3 前提：台上**至少两个**原铸仲裁者（`Deploy 3 Primaris Intercessor` 落下来了；实得 " + n + " 个）"
            + "｜🧨 只有一个 ⇒ 下面那条「全部 +1」是同义反复，验不出 `allFiltered`");
        Eq(bad, 0,
           "★ B3 台上**每一个**原铸仲裁者都 **+1 远程**（2 → 3；实得 " + (n - bad) + " / " + n + " 个达标）"
         + "｜🧨 `Count` 回到 1 ⇒ 只有一个吃到 ⇒ 红（这一条与 A4 一起钉住「全体」）");
        Eq(by.RangedAttack, 0,
           "★ B3′ 旁观单位（不是原铸仲裁者）**一点没涨** —— 卡名筛仍然生效（`ApplyTargetFilters` 的 `NameFilter` 那一支）");
    }
}
