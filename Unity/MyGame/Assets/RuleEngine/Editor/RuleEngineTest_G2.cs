// RuleEngineTest_G2.cs — 引擎线 `G2`：死因三族（170 / 175 / 180）的发射口 + `tactic(30)` 修正（2026-10-24）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册
// `_EngineA1` / `_EngineB3` / `_EngineB6` / `_EngineB7` / `_EngineF8` / `_G1b` … 都在），
// 而**同一时刻只有一个写手改一个文件** ⇒ 本件测试体全写在这里，
// `RuleEngineTest.cs` **只加 `Run()` 里一段 `Section(...)` + 一行 `Step(TestG2DeathRoutes)`**。
//
// 判据（第一入口 = `资料/普查产出_1024/C5_剩余判据普查.md` §1，⛔ 不要重新论证）：
//   · `UnitDeathType` 全表 7 档：`0 undefined / 10 combatAttacker / 15 combatDefender /
//     20 ability / 30 tactic / 40 poison / 50 collectWaystone`
//     （`UnitDeathType.cs:1-10`；我们的枚举 `Core/BattleContext.cs:34-43` 逐字一致）；
//   · **死因白名单只有 3 条**（`RawCardScript__OnMinionDeath.c:25-37`）：
//     `10 → 175 UnitDeathAttacking` · `20 → 180 UnitDeathByAbility` · `40 → 170 UnitDeathByPoison`，
//     其余（`0 / 15 / 30 / 50`）在 `:34` 的 `return` 上 ⇒ **一条都不发**；
//     ⚠️ 同一函数在死因分派**之前**无条件发 `80 / 81 / 90`（与死因无关）——
//        本件的「一条都不发」**只指 170/175/180 这一族**；
//   · `30 tactic` 是真死因：`BattleManager__GetDeathType.c:140-146`（另一处 `:249-255`）在伤害来源卡
//     的 `cardType` 经 `SupportMethods__IsSpellType`（只认 `0x14 Tactic` / `0x28 Whispers`，
//     `SupportMethods__IsSpellType.c:3-6`）为真时**直接 `return 0x1e(30)`**。
//     ⇒ 战术卡 / 防御卡 / 密令卡造成的伤害致死**不是** `ability(20)`（改前打错，本件钉住）。
//
// ⚠️ **池内 1,351 张卡没有任何一张带 170 / 175 触发**（只有 4 张带 180）⇒ 这两条**现读不可观测**，
//    只能拿**合成夹具**断（`C5` §1 证据⑧）。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 期望值一律写死字面量，⛔ 不从实现常量反算（那会自证）。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（`Run()` 里一行 `Step(TestG2DeathRoutes)`）。
    /// 子用例用 `G2…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestG2DeathRoutes()
    {
        Section("🆕 引擎线 G2：死因三族 170/175/180 的发射口 · `tactic(30)` 修正");
        G2DeathRouteDispatch();
        G2DeathRouteEndToEnd();
        G2TacticDeathType();
    }

    // ==============================================================================================
    //  ① 分派层：10 → 175 · 20 → 180 · 40 → 170；0/15/30/50 一条都不发
    // ==============================================================================================

    /// <summary>
    /// 🔑 **可区分夹具 = 一张同时带 170 / 175 / 180 三条 `abilityTriggers` 的监听卡**：
    /// 每个死因只许响**正好那一条**，另两条必须是 0 —— 只带一条的夹具分不清
    /// 「发对了」和「全都发」（那种实现三条断言会一起绿，等于没验）。
    ///
    /// 🧨 灭自证：
    /// · 把 `RuleCore.Die` 里那三支 `switch` 改回**单闸**（`if (deathType == Ability) …180`）⇒
    ///   第一条（10）与第三条（40）实得 0 ⇒ 红；
    /// · 把闸放宽成「只要死了就发 180」⇒ 反例那几条实得 1 ⇒ 红；
    /// · 把 `EmitDeathTriggerToListeners` 里的 `TN` 写死成 180 ⇒ 三族的名字对不上 ⇒ 红。
    /// </summary>
    static void G2DeathRouteDispatch()
    {
        // ---- 正例：10 / 20 / 40 各自只响对的那一条 ----
        var positives = new[]
        {
            new { Dt = UnitDeathType.CombatAttacker, Name = "UnitDeathAttacking" },
            new { Dt = UnitDeathType.Ability,        Name = "UnitDeathByAbility"  },
            new { Dt = UnitDeathType.Poison,         Name = "UnitDeathByPoison"   },
        };
        var allNames = new[] { "UnitDeathAttacking", "UnitDeathByAbility", "UnitDeathByPoison" };

        foreach (var c in positives)
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            Place(ctx, 0, 1, G2AllThreeListener("G2RouteListener"));
            var victim = Place(ctx, 1, 3, Unit("G2RouteVictim", 1, 0, 1));
            RuleCore.HurtWithSourceForTest(ctx, victim, 5, "测试效果", deathType: c.Dt);
            CheckTrue(!victim.IsAlive, "① （前提）死因 " + c.Dt + " 的受害者真的死了");

            foreach (var n in allNames)
            {
                int want = string.Equals(n, c.Name, StringComparison.Ordinal) ? 1 : 0;
                Check(G2Count(ctx, n, "G2RouteListener"), want,
                      "★★ ① `deathType = " + c.Dt + "` ⇒ `" + n + "` 发 **" + want + "** 条"
                    + (want == 1 ? "（正例）" : "（另两族**不许**顺带发）")
                    + "｜🧨 三支 `switch` 退回单闸（只认 20）⇒ 10/40 那两条正例实得 0 ⇒ 红；"
                    + "🧨 把三族都接到同一个触发名上 ⇒ 另两族实得 1 ⇒ 红");
            }
        }

        // ---- 反例：0 / 15 / 30 / 50 ⇒ 170/175/180 **一条都不发**（原版 `:34` 的 return）----
        foreach (int dt in new[] { UnitDeathType.Unknown, UnitDeathType.CombatDefender,
                                   UnitDeathType.Tactic, UnitDeathType.CollectWaystone })
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            Place(ctx, 0, 1, G2AllThreeListener("G2NegListener"));
            var victim = Place(ctx, 1, 3, Unit("G2NegVictim", 1, 0, 1));
            RuleCore.HurtWithSourceForTest(ctx, victim, 5, "测试", deathType: dt);
            CheckTrue(!victim.IsAlive, "① （前提）死因 " + dt + " 的受害者死了");

            foreach (var n in allNames)
                Check(G2Count(ctx, n, "G2NegListener"), 0,
                      "★★ ① `deathType = " + dt + "` ⇒ `" + n + "` **一条都不发**"
                    + "（原版白名单只有 10/20/40；0/15/30/50 在 `:34` 直接 return）"
                    + "｜🧨 把闸放宽成「只要有死亡就发」⇒ 实得 1 ⇒ 红");
        }
    }

    // ==============================================================================================
    //  ② 端到端：**死因是调用点真传下去的**（不只分派认得出）
    // ==============================================================================================

    /// <summary>
    /// 分派层对了**不等于**有人真的打了标签 —— 这两条验「产地」：
    /// · **反击打死攻击者** ⇒ `combatAttacker(10)` ⇒ 发 175
    ///   （判据 = `_ResolveAttack_d__438__MoveNext.c:1142` 的 `ReceiveDamage(攻方, 10, …)`）；
    /// · **中毒单位在自己回合末被摧毁** ⇒ `poison(40)` ⇒ 发 170
    ///   （判据 = `CardScript__OnTurnEnd.c:258` 的 `DestroyUnit(…, deathType = poison(40))`）。
    ///
    /// ⚠️ 反击那一路的死亡**会先进「同时伤害」批**（`PendingDeath`）⇒ 本用例同时钉住
    /// 「死因随 `PendingDeath.DeathType` 走到 `FlushDeaths` 再发」（改前那条链上死因是 `Unknown`）。
    ///
    /// 🧨 灭自证：
    /// · 把反击那处 `deathType: UnitDeathType.CombatAttacker` 删掉（退回默认 `Unknown`）⇒ 175 实得 0 ⇒ 红；
    /// · 把毒那处 `deathType: UnitDeathType.Poison` 删掉 ⇒ 170 实得 0 ⇒ 红；
    /// · 两处都改成了 `Ability` ⇒ 180 实得 1、本行的 175/170 实得 0 ⇒ 红。
    /// </summary>
    static void G2DeathRouteEndToEnd()
    {
        // ---- ① 反击打死攻击者 ⇒ 175 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 1, G2AllThreeListener("G2CounterListener"));
            var atk = Place(ctx, 0, 3, Unit("G2CounterAtk", 1, 3, 1), exhausted: false);   // 1 血
            Place(ctx, 1, 3, Unit("G2CounterDef", 1, 5, 9), exhausted: true);             // 5 攻 ⇒ 反击 5

            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK, "① 攻击 5 攻的守方");
            CheckTrue(!atk.IsAlive, "② （前提）攻击者被反击打死（否则下面不具区分力）" + LogTail(ctx));
            Check(G2Count(ctx, "UnitDeathAttacking", "G2CounterListener"), 1,
                  "★★ ② **被反击打死 = `combatAttacker(10)`** ⇒ 监听者收到 **1 条** `UnitDeathAttacking`"
                + "（死因还要随 `PendingDeath` 走到批末的 `FlushDeaths` 才发）"
                + "｜🧨 删掉反击那处 `deathType: CombatAttacker` ⇒ 实得 0 ⇒ 红");
            Check(G2Count(ctx, "UnitDeathByAbility", "G2CounterListener"), 0,
                  "★ ② 同一次死亡**不许**顺带发 `UnitDeathByAbility`（10 与 20 是两档）"
                + "｜🧨 把反击那处打成 `Ability` ⇒ 实得 1 ⇒ 红");
        }

        // ---- ② 中毒单位在自己回合末被摧毁 ⇒ 170 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 1, G2AllThreeListener("G2PoisonListener"));
            var poisoned = Place(ctx, 0, 3, Unit("G2PoisonVictim", 1, 1, 9), exhausted: true);
            poisoned.AddKeyword(KeywordTable.Poisoned, 1);
            CheckTrue(poisoned.Has(KeywordTable.Poisoned),
                      "② （前提）它真带着 `poisoned`（全池 0 张卡带它 ⇒ 夹具自造）");
            Check(ctx.Active, 0, "② （前提）现在结束的是它自己那一方（P0）的回合");

            RuleCore.EndTurn(ctx);
            CheckTrue(SlotOf(ctx, 0, "G2PoisonVictim") < 0,
                      "② （前提）中毒单位在回合末被摧毁了" + LogTail(ctx));
            Check(G2Count(ctx, "UnitDeathByPoison", "G2PoisonListener"), 1,
                  "★★ ② **中毒死于自己回合末 = `poison(40)`** ⇒ 监听者收到 **1 条** `UnitDeathByPoison`"
                + "｜🧨 删掉毒那处 `deathType: Poison` ⇒ 实得 0 ⇒ 红；"
                + "🧨 把它打成 `Ability` ⇒ 180 实得 1、本行 0 ⇒ 红");
            Check(G2Count(ctx, "UnitDeathByAbility", "G2PoisonListener"), 0,
                  "★ ② 同一次死亡**不许**发 `UnitDeathByAbility`（40 与 20 是两档）");
        }
    }

    // ==============================================================================================
    //  ③ `tactic(30)` 修正：战术卡 / 密令卡的伤害致死**不是** `ability(20)`
    // ==============================================================================================

    /// <summary>
    /// 改前 `DoDeal` / `DoDestroy` / `ResolveEffect` 三处**一律**打 `ability(20)` ⇒ 战术卡打死一张
    /// 带 180 的卡时我们会发 `UnitDeathByAbility`，而原版 `GetDeathType` 会 `return 30`（白名单外）
    /// ⇒ **原版一条都不发**（`C5` 顺手发现 11）。
    ///
    /// 🔑 **观测方式（如实记着）**：`tactic(30)` 本身**不发任何事件**（白名单只有 10/20/40）
    /// ⇒ 「死因到底是 30 还是 20」在行为上**唯一的可观测差别就是「发不发 180」**。
    /// 「30 而不是 15 / 50」在行为上**没有**差别（那三档都不发）—— 所以下面用**对照组**钉死
    /// 「同一条路径、只有来源卡种不同 ⇒ 一个发一个不发」：
    /// · 实验组：**战术卡** `Deal 3 damage to an enemy` 打死 3 血目标 ⇒ **不发** 180；
    /// · 对照组：**单位能力** `Ability: Damage 3 EnemyUnit` 打死同一个夹具 ⇒ **发** 180（证明夹具是活的）。
    ///
    /// 🧨 灭自证：
    /// · 把 `RuleCore.DeathTypeOfSource` 里 `tactic/defence/whispers` 那一条删掉（退回一律 20）⇒
    ///   实验组实得 1 ⇒ 红；
    /// · 把三处调用点的 `DeathTypeOfSource(ctx)` 换回写死的 `UnitDeathType.Ability` ⇒ 同上 ⇒ 红；
    /// · 把「没有来源卡」那一档也改成 30 ⇒ 下面 `null` 那条映射断言实得 30 ⇒ 红。
    /// </summary>
    static void G2TacticDeathType()
    {
        // ---- 实验组：战术卡的伤害打死目标 ⇒ 不发 180（= 死因 30，不是 20）----
        {
            var tac = Tactic("G2TacKill", 0, "Deal 3 damage to an enemy");
            var ctx = ProbeBattle(new[] { tac }, new CardDef[0]);
            ToP1Turn(ctx, 4);
            Place(ctx, 0, 1, G2AllThreeListener("G2TacListener"));
            var victim = Place(ctx, 1, 3, Unit("G2TacVictim", 1, 0, 3), exhausted: true);

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "G2TacKill"), 3), RuleCodes.OK,
                      "③ 用**战术卡**打死一个 3 血单位");
            CheckTrue(!victim.IsAlive, "③ （前提）它死了" + LogTail(ctx));
            Check(G2Count(ctx, "UnitDeathByAbility", "G2TacListener"), 0,
                  "★★ ③ **战术卡致死 = `tactic(30)`**（原版 `GetDeathType.c:140-146`）⇒ **不发** `UnitDeathByAbility`"
                + "（30 不在 `OnMinionDeath` 的白名单里）"
                + "｜🧨 退回「效果伤害一律 ability(20)」⇒ 实得 1 ⇒ 红 —— 这就是本件要修的缺陷");
            foreach (var n in new[] { "UnitDeathAttacking", "UnitDeathByPoison" })
                Check(G2Count(ctx, n, "G2TacListener"), 0,
                      "★ ③ 战术卡致死也不许发 `" + n + "`（30 只对 170/175/180 这一族「一条都不发」）");
        }

        // ---- 对照组：同一条路径、来源换成**单位能力** ⇒ 180 要发（否则实验组不具区分力）----
        {
            var caster = new CardDef("G2AbilityCaster", "G2AbilityCaster", "unit", "",
                                     "common", "Test", 0, 2, 9, 0,
                                     new[] { "Ability: Damage 3 EnemyUnit" },
                                     subtype: "Infantry");
            var ctx = ProbeBattle(new[] { caster }, new CardDef[0]);
            ToP1Turn(ctx, 4);
            Place(ctx, 0, 1, G2AllThreeListener("G2AbilityListener"));
            Place(ctx, 0, 3, caster, exhausted: false);
            var victim = Place(ctx, 1, 3, Unit("G2AbilityVictim", 1, 0, 3), exhausted: true);

            CheckCode(RuleCore.UseAbility(ctx, 0, 3, 3), RuleCodes.OK,
                      "③ （对照组）用**单位能力**打死同一个 3 血夹具");
            CheckTrue(!victim.IsAlive, "③ （前提）它死了" + LogTail(ctx));
            Check(G2Count(ctx, "UnitDeathByAbility", "G2AbilityListener"), 1,
                  "★★ ③ （对照组）**单位能力致死 = `ability(20)`** ⇒ 发 **1 条** `UnitDeathByAbility`"
                + "（与实验组只差「来源卡种」一项）"
                + "｜🧨 把 180 的发射口一起关掉 ⇒ 实得 0 ⇒ 本行红（实验组的 0 也就不再有区分力）");
        }

        // ---- 映射本身：tactic / defence / whispers ⇒ 30；单位卡 ⇒ 20；无来源 ⇒ 20 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            try
            {
                Check(RuleCore.DeathTypeOfSourceForTest(ctx), UnitDeathType.Ability,
                      "★ ③ 没有来源卡 ⇒ `ability(20)`（原版 `GetDeathType.c:133` 的 `if (actingCard == 0) goto …`"
                    + " —— 跳过 spell 判、落到 damageType 那几档）"
                    + "｜🧨 把 `null` 也当 spell ⇒ 实得 30 ⇒ 红");

                ctx.CreationSource = Tactic("G2MapTactic", 0, "x");
                Check(RuleCore.DeathTypeOfSourceForTest(ctx), UnitDeathType.Tactic,
                      "★ ③ 来源 = **战术卡** ⇒ `tactic(30)`（`SupportMethods.IsSpellType` 认 `0x14 Tactic`）");

                ctx.CreationSource = new CardDef("G2MapDefence", "G2MapDefence", "defence", "",
                                                 "common", "Test", 0, 0, 0, 0, null);
                Check(RuleCore.DeathTypeOfSourceForTest(ctx), UnitDeathType.Tactic,
                      "★ ③ 来源 = **防御卡** ⇒ 也是 `tactic(30)`（原版 defence 就是 `cardType 20 Tactic`，"
                    + "见 `WhenEvent.ActingCardTypeMatches` 的映射）");

                ctx.CreationSource = new CardDef("G2MapWhispers", "G2MapWhispers", "whispers", "",
                                                 "common", "Test", 0, 0, 0, 0, null);
                Check(RuleCore.DeathTypeOfSourceForTest(ctx), UnitDeathType.Tactic,
                      "★ ③ 来源 = **密令卡**（`Whispers`，原版 `cardType 40`）⇒ 也是 `tactic(30)`"
                    + "（`IsSpellType` 认 0x28）");

                // `ActingUnit` 优先于 `CreationSource`（本仓既有的来源卡优先级）
                var unit = Place(ctx, 0, 3, Unit("G2MapUnit", 1, 1, 9));
                ctx.ActingUnit = unit;
                Check(RuleCore.DeathTypeOfSourceForTest(ctx), UnitDeathType.Ability,
                      "★ ③ 来源 = **单位**（`ActingUnit` 优先于仍在作用域里的 `CreationSource`）⇒ `ability(20)`"
                    + "（原版 `DamageInfo.actingCard` 取的是 `AbilityData.actingCard` = 能力属主那张牌）"
                    + "｜🧨 读反优先级（先看 `CreationSource`）⇒ 实得 30 ⇒ 红");
            }
            finally
            {
                ctx.ActingUnit = null;
                ctx.CreationSource = null;
            }
        }
    }

    // ==============================================================================================
    //  夹具
    // ==============================================================================================

    /// <summary>一张**同时带 170 / 175 / 180** 三条 `abilityTriggers` 的在场监听卡
    /// （`context = 0 Play`，与 `B3Listener180` 同一形状；三族共用 ⇒ 能验「只响对的那一条」）。</summary>
    static CardDef G2AllThreeListener(string name)
    {
        return new CardDef(name, name, "unit", "", null, "Test", 1, 1, 9, 0, null,
            abilityContext: new[]
            {
                new CardDef.AbilityContextSpec { Trigger = 170, Name = "UnitDeathByPoison",   Context = 0 },
                new CardDef.AbilityContextSpec { Trigger = 175, Name = "UnitDeathAttacking",  Context = 0 },
                new CardDef.AbilityContextSpec { Trigger = 180, Name = "UnitDeathByAbility",  Context = 0 },
            },
            abilityContextsVersioned: true,
            abilityTriggers: new[] { 170, 175, 180 }, abilityTriggersVersioned: true);
    }

    /// <summary>`ctx.ActionLog` 里「这张卡 × 这个 trigger 名」的 `EvtKind.Trigger` 条数。</summary>
    static int G2Count(BattleContext ctx, string keyword, string cardName)
    {
        int n = 0;
        foreach (var e in ctx.ActionLog)
        {
            if (e == null) continue;
            if (e.Kind != EvtKind.Trigger) continue;
            if (!string.Equals(e.Keyword, keyword, StringComparison.Ordinal)) continue;
            if (!string.Equals(e.CardId, cardName, StringComparison.Ordinal)) continue;
            n++;
        }
        return n;
    }
}
