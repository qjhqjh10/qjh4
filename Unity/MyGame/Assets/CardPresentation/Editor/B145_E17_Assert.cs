// B145_E17_Assert.cs — 执行代理 `B145-E17`（第一百三十一会话 · 完全复刻 · 离线批）的断言集
//
// ============================ 它盯的是什么 ============================
//   `资料/完全复刻/活状态表.md` 的三行：
//     · **`B145-STUN-GATE-POSITION`**：`stun`(trait 100) 的闸位。
//       原版 `BattleManager__IsValidAttackTarget.c:215-218` 在**目标校验**里；我们原来**只**在
//       `RuleCore.CanAttackNow` 里有。本笔把 `IsValidTarget` 那一处**补上**（§A-b）。
//       🔴 **并【没有】把 `CanAttackNow` 那一句删掉** —— 施工单的前提（「`CardScript.CanAttackNow`
//       不查 stun ⇒ 绿光被误灭」）**与二进制冲突**：`CardScript__ActivateMinion.c:23/:55` 把
//       `!HasCurrentTrait(100)` 算进 `canAct(+0x230)`/`canAttack(+0x23c)`，`:82-90` 再拿
//       `CardScript__CanAttackNow()` 的返回值直接 `SetActive(canAttackAnim)`；
//       `CardScript__Stun.c` 施加眩晕时**当场**把 `+0x230`/`+0x23c` 双双清 0；
//       `BattleManager__CanAttackCard.c:31` 也独立查一次 trait 100。
//       ⇒ **原版被晕单位绿光就是灭的**，两位是**并存关系**、不是「闸放错位置」。证据全文见报告。
//     · **`B145-HIT0-BLOCKED-FLAG`**：`Hit` 事件新增「被完全挡下」位。
//       原版 `CardScript._ReceiveDamage_d__381__MoveNext.c:54`（`+0x35 = 1` 默认弹）→
//       `:185-187`（`dodge(0xf0)` ∨ `invulnerable(0x136)` ∨ `shield(0x50)` ⇒ `:186` `+0x35 = 0`）→
//       `:313`（`if (+0x35 != 0) BattleCardUI__DamageReceived(amount)`）⇒ **那三档挡下时不弹数字**。
//
// ============================ 每条断言的「灭自证」============================
//   A（stun 闸位）：
//     · §A-0 前提：夹具攻击者**真的挂着 `stun`**（关键词命中，否则下面全是空转）；控制组同一张卡**不挂**。
//     · §A-a **`IsValidTarget` 这一层管**：近战 ⇒ `ErrStunned`、远程 ⇒ `ErrStunned`（原版那一格 `param_5` 不参与）。
//       🧨 把本笔加的那一句删掉（= 改前）⇒ 实得 `OK` ⇒ 红。
//     · §A-b **绿光这一层仍然管**（本笔**刻意不删**）：`CanAttackNow(近战)` ⇒ `ErrStunned`、`CanActNow` ⇒ `false`。
//       🧨 按施工单字面「把闸挪走」删掉 `CanAttackNow` 的 `u.IsStunned` ⇒ 实得 `OK` / `true` ⇒ 红。
//       a 与 b 在**同一次调用**上结构上不可能同时满足「位置对」与「删掉一句」两种写法 —— 只有
//       「a 绿 b 也绿」这一态才是二进制支持的那一态。
//     · §A-c 摘掉 `stun` ⇒ 同一对、同样打法恢复 `OK`（证明 a 那两条是 stun 造成的，不是夹具别处的闸）。
//     · §A-d 端到端 `DeclareAttack` ⇒ `ErrStunned`（改前改后**净返回码不变**，防止「挪闸把整条链挪漏」）。
//   B（Hit 的「完全挡下」位）：
//     · §B-0 前提：`shield` 那张**真的带着关键词**；控制组同一张卡**不带**。
//     · §B-a `shield` 挡下 ⇒ `ApplyDamage` 返回 0 且最新一条 `Hit`：`Amount == 0` **且** `DamageFullyBlocked == true`。
//     · §B-b `dodge` / `invulnerable` 同档（原版 `:185-187` 三条并列）。
//     · §B-c 控制组（不带那三条）真掉血 ⇒ 最新 `Hit` 的 `DamageFullyBlocked == false`。
//       🧨 把 `fullyBlocked` 写成恒 `true` ⇒ c 红；写成恒 `false` ⇒ a/b 红 —— 结构上不可能同时满足。
//     · §B-d 决策口：`ShowHitDamagePop(0, true) == false`（挡下不弹）但 `ShowHitDamagePop(0, false) == true`
//       （原版 `amount` 不参与判断、0 也弹）—— 两位都要，缺一即红。
//
// ============================ 怎么跑（⚠️ 执行代理无权跑 Unity）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B145_E17_Assert.Run -logFile "d:/4/_tmp_view/b145e17.log"
//   判绿 = 合计那行 `失败 0`。
//   ⚠️ 本文件**不建场景 / 不建资产 / 不进任何 `Editor/*Scene.cs`**；只造内存里的 `BattleContext`。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>`B145-E17`：stun 闸位（原文补 `IsValidTarget`）＋ `Hit` 的「被完全挡下」位。计数口径照 `CheckSink`。</summary>
public static class B145_E17_Assert
{
    static readonly CheckSink _sink = new CheckSink("[E17] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>本趟通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>本趟失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }
    /// <summary>本趟实际执行的断言条数（= Pass + Fail；证明断言真被跑到）。</summary>
    public static int RunCount { get { return _sink.Pass + _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    const int MyAtkSlot = 5;   // 我方攻击单位（⛔ 别用 4 = 督军格）
    const int FoeTgtSlot = 3;  // 敌方目标

    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0;
        try
        {
            MenuCheck.Section(_sink, "B145-E17 · stun 闸位（补 `IsValidTarget`、保留 `CanAttackNow`）"
                                   + " ＋ `Hit` 的「被完全挡下」位（原版 `+0x35`）");
            SectionA_StunGate();
            SectionB_HitFullyBlocked();
        }
        catch (System.Exception e)
        {
            _sink.Fail++;
            Debug.LogError(_sink.P + "✗ 断言过程抛异常（后面的条目没跑完）：" + e);
        }
        Debug.Log(_sink.P + $"===== 合计 通过 {_sink.Pass} / 失败 {_sink.Fail} —— "
                          + (_sink.Fail == 0 ? "✅ 全绿" : "❌ 有红") + " =====");
    }

    // ==================================================================
    //  A —— `stun`(trait 100) 的闸位
    //    ① `IsValidTarget`（= 原版 `IsValidAttackTarget` `.c:215-218`）**管**；
    //    ② `CanAttackNow`（= 绿光/行动那条链）**仍然管** —— 判据 = `ActivateMinion.c:23/:55` + `Stun.c` + `CanAttackCard.c:31`
    // ==================================================================
    static void SectionA_StunGate()
    {
        MenuCheck.Section(_sink, "A `stun`(trait 100)：`IsValidTarget` 管（`.c:215-218`）· `CanAttackNow` 也管"
                               + "（`ActivateMinion.c:23/:55` 的 `canAct` + `CanAttackCard.c:31`）");

        // ---- 控制组：同一张卡、不挂 stun ----
        {
            var ctx = Battle();
            Place(ctx, 0, MyAtkSlot, Unit("E17Clean", 1, 3, 9), exhausted: false);
            Place(ctx, 1, FoeTgtSlot, Unit("E17Prey0", 1, 0, 9), exhausted: true);
            C(RuleCore.IsValidTarget(ctx, 0, MyAtkSlot, 1, FoeTgtSlot, false), RuleCodes.OK,
              "★ §A-0a 前提控制组：不挂 `stun` ⇒ `IsValidTarget(近战)` = `OK`（证明下面那条 `ErrStunned` 是 stun 造成的）");
        }

        // ---- 挂 stun ----
        var stunCtx = Battle();
        var stunned = Place(stunCtx, 0, MyAtkSlot, Unit("E17Stunned", 1, 3, 9), exhausted: false);
        Place(stunCtx, 1, FoeTgtSlot, Unit("E17Prey1", 1, 0, 9), exhausted: true);
        stunned.AddKeyword(KeywordTable.Stun, 1);
        Tr(stunned.IsStunned,
           "★ §A-0b 前提：夹具攻击者**真的挂上了 `stun`**（关键词命中，否则下面全是空转）");

        C(RuleCore.IsValidTarget(stunCtx, 0, MyAtkSlot, 1, FoeTgtSlot, false), RuleCodes.ErrStunned,
          "★ §A-a **「攻击某个目标」这一层管**：近战 ⇒ `IsValidTarget` = `ErrStunned`"
        + "（原版 `BattleManager__IsValidAttackTarget.c:215-218`：`HasCurrentTrait(攻方,100)` ⇒ `return 0`）"
        + "｜🧨 把本笔加的那一句删掉（= 改前）⇒ 实得 `OK` ⇒ 红");
        C(RuleCore.IsValidTarget(stunCtx, 0, MyAtkSlot, 1, FoeTgtSlot, true), RuleCodes.ErrStunned,
          "★ §A-a2 **不和打法相关**：远程同样 `ErrStunned`（原版那一格只读 trait 100、不读 `param_5`）"
        + "｜🧨 写成 `if (!ranged && attacker.IsStunned)`（照 pindown 那条抄）⇒ 本条红");

        C(RuleCore.CanAttackNow(stunCtx, 0, MyAtkSlot, false), RuleCodes.ErrStunned,
          "★ §A-b **绿光/行动那条链仍然管**（本笔**刻意不删**）：`CanAttackNow(近战)` = `ErrStunned`"
        + "（判据 = `CardScript__ActivateMinion.c:23/:55` 把 `!stun` 算进 `canAct`/`canAttack`，"
        + "`CardScript__Stun.c` 施加时当场把 `+0x230`/`+0x23c` 清 0，`CardScript__ActivateMinion.c:82-90` 用 `CanAttackNow()` 直接点绿光）"
        + "｜🧨 按施工单字面「把闸挪到 `IsValidTarget`」删掉这一句 ⇒ 实得 `OK` ⇒ 红（绿光会在被晕单位上误亮）");
        Tr(!RuleCore.CanActNow(stunCtx, 0, MyAtkSlot),
           "★ §A-b2 `CanActNow` = 绿光亮不亮 ⇒ 被晕单位必须 `false`"
         + "｜🧨 同上一行（删 `CanAttackNow` 的 `u.IsStunned`）⇒ 实得 `true` ⇒ 红");

        // ---- 摘掉 stun ⇒ 恢复 ----
        stunned.RemoveAll(KeywordTable.Stun);
        C(RuleCore.IsValidTarget(stunCtx, 0, MyAtkSlot, 1, FoeTgtSlot, false), RuleCodes.OK,
          "★ §A-c 摘掉 `stun` ⇒ 同一对、同样打法恢复 `OK`（证明 a 的两条是 stun 造成的，不是夹具里别的闸）");

        // ---- 端到端 ----
        var endCtx = Battle();
        var endAtk = Place(endCtx, 0, MyAtkSlot, Unit("E17StunnedEnd", 1, 3, 9), exhausted: false);
        Place(endCtx, 1, FoeTgtSlot, Unit("E17Prey2", 1, 0, 20), exhausted: true);
        endAtk.AddKeyword(KeywordTable.Stun, 1);
        C(RuleCore.DeclareAttack(endCtx, 0, MyAtkSlot, 1, FoeTgtSlot, ranged: false), RuleCodes.ErrStunned,
          "★ §A-d 端到端 `DeclareAttack` = `ErrStunned`（改前由 `CanAttackNow` 拦、改后由 `CanAttackNow` 仍先拦 ⇒ **净返回码不变**）"
        + "｜🧨 两处闸都删 ⇒ 实得 `OK` ⇒ 红");
    }

    // ==================================================================
    //  B —— `Hit` 事件的「被完全挡下」位（原版 `+0x35`，`_ReceiveDamage…:185-187`）
    // ==================================================================
    static void SectionB_HitFullyBlocked()
    {
        MenuCheck.Section(_sink, "B `Hit.DamageFullyBlocked`：`shield` / `dodge` / `invulnerable` 挡下 ⇒ 置真"
                               + "（原版 `_ReceiveDamage…:186` 的 `+0x35 = 0`）· 控制组置假 · 决策口读它");

        // ---- 控制组：不带那三条 ⇒ 真掉血、位为假 ----
        {
            var ctx = Battle();
            var u = Place(ctx, 1, FoeTgtSlot, Unit("E17NoWard", 1, 0, 9), exhausted: true);
            Tr(!u.Has(KeywordTable.Shield) && !u.Has(KeywordTable.Dodge) && !u.Has("invulnerable"),
               "★ §B-0a 前提：控制组**一条免伤都不得带**（否则下面那条「位为假」是空转）");
            ctx.ClearSignals();
            int dealt = RuleCore.ApplyDamage(ctx, u, 4, "E17 测试");
            C(dealt, 4, "★ §B-0b 前提：控制组真的掉 4 血（`ApplyDamage` 返回 4）");
            var hit = LastHit(ctx);
            Tr(hit != null, "★ §B-0c 前提：这一下**真的发了一条 `Hit`**（⛔ 没有它下面全是空转）");
            C(hit == null ? -1 : hit.Amount, 4, "★ §B-0d 前提：`Hit.Amount` = 4（真伤害）");
            Tr(hit != null && !hit.DamageFullyBlocked,
               "★ §B-c **控制组：`DamageFullyBlocked = false`**（没有被挡下）"
             + "｜🧨 把 `fullyBlocked` 写成恒 `true` ⇒ 本条红");
        }

        // ---- shield ----
        {
            var ctx = Battle();
            var u = Place(ctx, 1, FoeTgtSlot, Unit("E17Shield", 1, 0, 9), exhausted: true);
            u.AddKeyword(KeywordTable.Shield, 1);
            Tr(u.Has(KeywordTable.Shield), "★ §B-0e 前提：`shield` 那张**真的带着关键词**");
            ctx.ClearSignals();
            int dealt = RuleCore.ApplyDamage(ctx, u, 5, "E17 测试");
            C(dealt, 0, "★ §B-0f 前提：`shield` 整份挡下 ⇒ 实伤 0");
            var hit = LastHit(ctx);
            C(hit == null ? -1 : hit.Amount, 0, "★ §B-a 挡下也**照旧发** `Hit`（`Amount = 0` —— 原版 `:313` 不看 amount）");
            Tr(hit != null && hit.DamageFullyBlocked,
               "★ §B-a2 **`shield` 挡下 ⇒ `DamageFullyBlocked = true`**"
             + "（原版 `_ReceiveDamage…:185-187`：`shield(0x50)` 命中 ⇒ `:186` `+0x35 = 0` ⇒ `:313` 不调 `DamageReceived`）"
             + "｜🧨 把引擎那两个 `fullyBlocked: true` 删掉（= 改前）⇒ 本源红（表现层会在盾挡下时冒一个「0」）");
            Tr(!u.Has(KeywordTable.Shield), "★ §B-a3 （对照）盾被这一下消耗掉了 —— 与「挡下」是同一支，⛔ 别把它当障碍");
        }

        // ---- dodge ----
        {
            var ctx = Battle();
            var u = Place(ctx, 1, FoeTgtSlot, Unit("E17Dodge", 1, 0, 9), exhausted: true);
            u.AddKeyword(KeywordTable.Dodge, 1);
            Tr(u.Has(KeywordTable.Dodge), "★ §B-0g 前提：`dodge` 那张**真的带着关键词**");
            ctx.ClearSignals();
            int dealt = RuleCore.ApplyDamage(ctx, u, 5, "E17 测试");
            C(dealt, 0, "★ §B-0h 前提：`dodge` 整份挡下 ⇒ 实伤 0");
            var hit = LastHit(ctx);
            Tr(hit != null && hit.DamageFullyBlocked,
               "★ §B-b **`dodge` 挡下 ⇒ `DamageFullyBlocked = true`**（原版 `:185-187` 三条并列之一：`dodge(0xf0)`）"
             + "｜🧨 只给 `shield` 那一支传 `fullyBlocked: true`、漏掉这一支 ⇒ 本条红");
        }

        // ---- invulnerable ----
        {
            var ctx = Battle();
            var u = Place(ctx, 1, FoeTgtSlot, Unit("E17Invuln", 1, 0, 9), exhausted: true);
            u.AddKeyword("invulnerable", 1);
            Tr(u.Has("invulnerable"), "★ §B-0i 前提：`invulnerable` 那张**真的带着关键词**");
            ctx.ClearSignals();
            int dealt = RuleCore.ApplyDamage(ctx, u, 5, "E17 测试");
            C(dealt, 0, "★ §B-0j 前提：`invulnerable` 整份挡下 ⇒ 实伤 0");
            var hit = LastHit(ctx);
            Tr(hit != null && hit.DamageFullyBlocked,
               "★ §B-b2 **`invulnerable` 挡下 ⇒ `DamageFullyBlocked = true`**（原版 `:185-187` 的 `invulnerable(0x136)`）"
             + "｜🧨 只给 shield/dodge 那一支传、漏掉无敌那一支 ⇒ 本条红");
            Tr(u.Has("invulnerable"), "★ §B-b3 （对照）无敌**不被消耗**（原版那一带没有 `0x136` 的 `RemoveTrait`）");
        }

        // ---- 决策口 ----
        Tr(!BattleDriver.ShowHitDamagePop(0, true),
           "★ §B-d **决策口**：`Amount = 0` 且 `DamageFullyBlocked = true` ⇒ **不弹**"
         + "（原版 `:313` 的 `if (+0x35 != 0)`）｜🧨 忽略第二位（`return amount >= 0`）⇒ 本条红");
        Tr(BattleDriver.ShowHitDamagePop(0, false),
           "★ §B-d2 **灭自证（同一次判断的另一半）**：`Amount = 0` 但**没被挡下** ⇒ **照旧弹 `0`**"
         + "（原版 `amount` 不参与判断 —— `B145-E3` 那条改法不能被本笔顺手改回去）"
         + "｜🧨 写成 `amount > 0`（或让 `fullyBlocked` 恒真）⇒ 本条红、d 仍绿 ⇒ 结构上不可能同时满足");
        Tr(BattleDriver.ShowHitDamagePop(-1, false),
           "★ §B-d3 负档（幸存者广播标记）仍然不弹（`B145-E3` B3 同一条，别被本笔改掉）");
    }

    // ==================================================================
    //  夹具（与 `B145_E2_Assert` / `RuleEngineTest` 同名夹具**同形**：只摆局面，⛔ 不复刻任何规则判断）
    // ==================================================================
    /// <summary>最新一条 `EvtKind.Hit`（没有 ⇒ `null`）。</summary>
    static BattleEvent LastHit(BattleContext ctx)
    {
        if (ctx == null || ctx.Signals == null) return null;
        for (int i = ctx.Signals.Count - 1; i >= 0; i--)
        {
            var e = ctx.Signals[i];
            if (e != null && e.Kind == EvtKind.Hit) return e;
        }
        return null;
    }

    static CardDef Unit(string name, int cost, int atk, int hp, params string[] kws)
    {
        return new CardDef(name, name, "unit", "", null, "Test", cost, atk, hp, 0, kws);
    }

    static CardDef Hero()
    {
        return new CardDef("E17FixtureWarlord", "E17FixtureWarlord", "hero", "", null, "Test", 0, 2, 30, 0, null,
                           subtype: "Warlord");
    }

    /// <summary>督军 + 20 张垫牌（不使用手牌 ⇒ 开局抽到什么都不影响本节两条活）。</summary>
    static List<CardDef> Deck()
    {
        var d = new List<CardDef> { Hero() };
        for (int i = 0; i < 20; i++) d.Add(Unit("e17filler" + i, 1, 0, 1));
        return d;
    }

    static BattleContext Battle()
    {
        var ctx = RuleCore.NewBattle(Deck(), Deck(), seed: 0, shuffle: false);
        ToP0Turn1(ctx);
        return ctx;
    }

    /// <summary>推进到「P0 的第 1 个回合」。</summary>
    static void ToP0Turn1(BattleContext ctx)
    {
        int guard = 0;
        while (ctx.Turn != 1 || ctx.Active != 0)
        {
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            else { RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx); }
            if (++guard > 40) { Tr(false, "回合推进超限，疑似死循环"); return; }
        }
    }

    /// <summary>把一个单位直接摆到场上（跳过部署流程，只为构造场景）。</summary>
    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card, bool exhausted)
    {
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = exhausted };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }
}
