// B145_E22_Assert.cs — 执行代理 `B145-E22`（2026-11-05）· 施工单 `B145-SENTRY-KILL-SKIP-ATTACK`
//
// **断言什么**：**哨戒（Sentry）把攻击者打死的这一击，主攻击整套不结算** ——
//   主伤害不发生 · 守方不反击 · 相邻伤害（Blast）不传播；而 `BroadcastUnitAttacked`
//   与哨戒伤害本身**照发/照打**。
//
// **判据（第一权威 = 反编译 `d:/2/tools/decomp_full/`，由 `B145-E17` 读全链查清）**：
//   `BattleManager._ResolveAttack_d__438__MoveNext.c`
//     · `:719` 结算段开头清零 `<attackerKilledBySentry>(+0x13a)`；
//     · `:731-736` 哨戒伤害**之前**预判 `EnoughPendingDamageToDieWithDamageValues(攻击者, [CurrentSentry], [0])`
//       ⇒ 是则置 `+0x13a = 1`，**同时** `defenderReturnsDamage(+0x138) = 0`（⇒ 不反击）；
//     · `:823-824` 用它跳过 `RecordAttackPendingDamage` + `SetupAttack`；
//     · `:863 if (+0x13a == 0 && defenderKilledByShuriken(+0x139) == 0) ResolveAttackDamage…`
//       ⇒ 跳过主伤害 **与** 恐怖 / 无归狙击那两条支；
//     · `:1013-1014 if ((ShouldTriggerAdjacentDamage == 0) || (+0x13a != 0))` ⇒ 跳过**相邻伤害传播**（Blast）；
//     · `:988 BroadcastUnitAttacked` **不在任何闸里**（无条件）· 哨戒伤害本身 `:781-797` 照打
//       （只有 `:736` 那一句 `+0x138 = 0` 把「守方反击」关掉）；
//     · `:1114 if (defenderReturnsDamage(+0x138) != 0)` 才是反击段 ⇒ 攻击者被哨戒打死时**不进**。
//   ⚠️ **判定是预判**（伤害之前），⛔ 不是 `!attacker.IsAlive` —— 我们死亡延后到批末，
//      `Hurt` 之后读血量会把这一记重复扣一次。落地口 = `RuleCore.DeclareAttack` 的 `attackerKilledBySentry`。
//
// **灭自证**：
//   · 每一格都配**反例**（攻击者活得下来 ⇒ 主伤害 / 反击 / Blast **都必须发生**）——
//     若有人把闸写成 `if (true)`（永远跳过）或删掉闸（永不跳过），必有一侧变红；
//   · `BroadcastUnitAttacked` 那一格用**卡面正文自己**的效果（`When this unit attacks an enemy with Hunt Mark,
//     gain +1 Attack`）验「真的落了地」，不拿日志行数单独充数（日志只作**前提**硬检查）。
//
// 🔴 每条都带 `🧨`（什么改动能让它红）—— 没写 `🧨` 的断言一律当没写。
// ⚠️ 本文件是**夹具直调**（同 `RuleEngineTest_T03d.cs` 口径）：`ProbeBattle` + `Place` + `RuleCore.DeclareAttack`，
//    **不跑 Unity 批处理**。建议宿主 = `RuleEngineTest.Run()` 里一行
//    `Step(TestB145E22SentryKillSkipsAttack);`（本代理**不改** `RuleEngineTest.cs` —— 不在白名单，接线由调度台做）。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>
    /// **哨戒击杀 ⇒ 跳过主伤害 / 反击 / 相邻传播；`BroadcastUnitAttacked` 照发**
    /// （施工单 `B145-SENTRY-KILL-SKIP-ATTACK`；判据 = `_ResolveAttack_d__438__MoveNext.c:731-736/863/1013-1014`）。
    /// 建议接线：`RuleEngineTest.Run()` 里一行 `Step(TestB145E22SentryKillSkipsAttack);`
    /// </summary>
    public static void TestB145E22SentryKillSkipsAttack()
    {
        Section("🆕 `B145-E22`：**哨戒把攻击者打死 ⇒ 主伤害 / 反击 / Blast 全跳过**（原版 `+0x13a` 三道闸）");

        // ==========================================================================================
        //  ① 主伤害 + 反击：攻击者 2 血挨 `Sentry 5` ⇒ 死；守方 3 攻的反击也不该发生
        // ==========================================================================================
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 3, Unit("B145E22Atk", 1, 2, 2), exhausted: false);
            var tgt = Place(ctx, 1, 3, Unit("B145E22Sentry", 1, 3, 12, "Sentry 5"), exhausted: true);

            // 前提（硬检查，⛔ 不写成 `if (前提) Check`）
            CheckTrue(tgt.Has("sentry") && tgt.KwValue("sentry") == 5,
                      "🧪 夹具自检：被打者带 `Sentry 5`｜🧨 关键词没解析 ⇒ 本组三条都会假绿");
            Check(atk.Health, 2, "🧪 （前提）攻击者 2 血（哨戒必杀）");

            int from = ctx.Events.Count;
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK,
                      "近战打那个带 `Sentry 5` 的目标（守方 3 攻，正常会给 3 点反击）");

            CheckTrue(!atk.IsAlive,
                      "（前提）**哨戒把攻击者打死了**（2 − 5）" + LogTail(ctx));
            CheckTrue(B145E22FirstIndexOf(ctx, from, "跳过主伤害") >= 0,
                      "（前提）`+0x13a` 那道闸**真的触发了**（日志里有「跳过主伤害」）"
                    + "｜🧨 把 `attackerKilledBySentry` 恒置假 ⇒ 本行红" + LogTail(ctx));

            Check(tgt.Health, 12,
                  "★★ ①-a **主伤害不发生**：目标仍是 12 血"
                + "｜判据 = `_ResolveAttack_d__438__MoveNext.c:863`（`+0x13a` 跳过 `ResolveAttackDamage`）"
                + "｜🧨 去掉主伤害那道 `&& !attackerKilledBySentry` ⇒ 实得 10" + LogTail(ctx));
            Check(atk.Health, 2 - 5,
                  "★★ ①-b **守方不反击**：攻击者只吃了哨戒那 5 点（2 → −3）"
                + "｜判据 = `:736` 把 `defenderReturnsDamage(+0x138)` 清 0 + `:1114` 才进反击段"
                + "｜🧨 去掉反击那道 `!attackerKilledBySentry` ⇒ 再多 3 点、实得 −6" + LogTail(ctx));
        }

        // ==========================================================================================
        //  ② 反例（灭自证）：同一个目标，攻击者 20 血**活得下来** ⇒ 主伤害与反击**都要发生**
        // ==========================================================================================
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 3, Unit("B145E22AtkBig", 1, 2, 20), exhausted: false);
            var tgt = Place(ctx, 1, 3, Unit("B145E22Sentry2", 1, 3, 12, "Sentry 5"), exhausted: true);

            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK,
                      "近战打同一个 `Sentry 5` 目标（这次攻击者 20 血，死不了）");

            CheckTrue(atk.IsAlive,
                      "（前提）攻击者活着（20 − 5 = 15）" + LogTail(ctx));
            Check(tgt.Health, 12 - 2,
                  "★★ ②-a 反例：**攻击者活下来 ⇒ 主伤害照常**（目标 12 → 10）"
                + "｜🧨 把闸写成无条件跳过（或 `attackerKilledBySentry` 恒真）⇒ 本行红"
                + LogTail(ctx));
            Check(atk.Health, 20 - 5 - 3,
                  "★★ ②-b 反例：**攻击者活下来 ⇒ 反击照常**（20 − 5 哨戒 − 3 反击 = 12）"
                + "｜🧨 同上 ⇒ 实得 15" + LogTail(ctx));
        }

        // ==========================================================================================
        //  ③ 相邻伤害传播（Blast）：攻击者被哨戒打死 ⇒ 目标左右两格各 10 血**一点不掉**
        //     目标 = P1 槽 2（相邻 = 槽 1 / 3）；攻击者自己也带 `Blast 2`
        // ==========================================================================================
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 2, Unit("B145E22Blast", 1, 2, 2, "Blast 2"), exhausted: false);
            var tgt = Place(ctx, 1, 2, Unit("B145E22Sentry3", 1, 0, 12, "Sentry 5"), exhausted: true);
            var adj1 = Place(ctx, 1, 1, Unit("B145E22Adj1", 1, 0, 10));
            var adj3 = Place(ctx, 1, 3, Unit("B145E22Adj2", 1, 0, 10));

            CheckTrue(atk.Has("blast") && atk.KwValue("blast") == 2,
                      "🧪 夹具自检：攻击者带 `Blast 2`");
            CheckTrue(adj1 != null && adj3 != null,
                      "🧪 （前提）目标左右两格各有单位（槽 1 / 槽 3）");

            CheckCode(RuleCore.DeclareAttack(ctx, 0, 2, 1, 2), RuleCodes.OK,
                      "近战打 `Sentry 5` 目标（攻击者 2 血 ⇒ 被哨戒打死）");

            CheckTrue(!atk.IsAlive, "（前提）攻击者被哨戒打死" + LogTail(ctx));
            Check(tgt.Health, 12, "（前提）主伤害也不发生（目标 12）" + LogTail(ctx));
            Check(adj1.Health, 10,
                  "★★ ③-a **相邻伤害传播被跳过**：槽 1 的相邻单位仍是 10 血"
                + "｜判据 = `_ResolveAttack_d__438__MoveNext.c:1013-1014`（`+0x13a != 0` ⇒ 整段相邻伤害不做）"
                + "｜🧨 去掉 Blast 那道 `&& !attackerKilledBySentry` ⇒ 实得 8" + LogTail(ctx));
            Check(adj3.Health, 10,
                  "★★ ③-b 同上：槽 3 的相邻单位仍是 10 血｜🧨 同上 ⇒ 实得 8" + LogTail(ctx));
        }

        // ==========================================================================================
        //  ④ 反例（灭自证）：同样的 Blast 形状，攻击者 20 血 ⇒ 主伤害 + Blast **都要发生**
        // ==========================================================================================
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 2, Unit("B145E22BlastBig", 1, 2, 20, "Blast 2"), exhausted: false);
            var tgt = Place(ctx, 1, 2, Unit("B145E22Sentry4", 1, 0, 12, "Sentry 5"), exhausted: true);
            var adj1 = Place(ctx, 1, 1, Unit("B145E22Adj3", 1, 0, 10));
            var adj3 = Place(ctx, 1, 3, Unit("B145E22Adj4", 1, 0, 10));

            CheckCode(RuleCore.DeclareAttack(ctx, 0, 2, 1, 2), RuleCodes.OK,
                      "近战打同一个 `Sentry 5` 目标（这次攻击者 20 血）");

            CheckTrue(atk.IsAlive, "（前提）攻击者活着" + LogTail(ctx));
            Check(tgt.Health, 12 - 2,
                  "★★ ④-a 反例：主伤害照常（目标 12 → 10）｜🧨 闸恒真 ⇒ 实得 12" + LogTail(ctx));
            Check(adj1.Health, 8,
                  "★★ ④-b 反例：`Blast 2` 照常传开（槽 1：10 → 8）｜🧨 闸恒真 ⇒ 实得 10" + LogTail(ctx));
            Check(adj3.Health, 8,
                  "★★ ④-c 反例：`Blast 2` 照常传开（槽 3：10 → 8）｜🧨 闸恒真 ⇒ 实得 10" + LogTail(ctx));
        }

        // ==========================================================================================
        //  ⑤ `BroadcastUnitAttacked` 照发（`:988` 不在任何闸里）：
        //     被哨戒打死的攻击者，自己的 `When this unit attacks …` **仍然响、且效果真的落地**
        // ==========================================================================================
        {
            var atkCard = new CardDef("B145E22AtkTrig", "B145E22AtkTrig", "unit",
                                      "When this unit attacks an enemy with Hunt Mark, gain +1 Attack",
                                      "common", "Test", 1, 2, 2, 0, null, subtype: "Infantry");
            CheckTrue(atkCard.WhenTriggers.Count > 0,
                      "🧪 夹具自检：攻击者的 `When this unit attacks …` 监听器注册上了");

            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var a = Place(ctx, 0, 3, atkCard, exhausted: false);
            var b = Place(ctx, 1, 3, Unit("B145E22Sentry5", 1, 0, 12, "Sentry 5"), exhausted: true);
            b.AddKeyword("huntmark", 1);      // 宾语筛选（`an enemy with Hunt Mark`）要它
            ctx.ClearSignals();

            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK,
                      "近战打 `Sentry 5` 目标（攻击者死）");

            CheckTrue(!a.IsAlive, "（前提）哨戒把攻击者打死了" + LogTail(ctx));
            Check(b.Health, 12, "（前提）主伤害没发生（目标 12）" + LogTail(ctx));
            Check(WhenFired(ctx, "B145E22AtkTrig"), 1,
                  "★★ ⑤-a **`BroadcastUnitAttacked` 照发**：被哨戒打死的攻击者，「攻击时」监听器仍响 1 次"
                + "｜判据 = `_ResolveAttack_d__438__MoveNext.c:988`（无条件）+ "
                + "`BattleManagerSupport__BroadcastUnitAttacked.c:29`（攻击者那一下无条件预调）"
                + "｜🧨 把 `ctx.Emit(EvtKind.Attack)` / `BroadcastWhen(Attack)` 也塞进 `!attackerKilledBySentry` 闸 ⇒ 本行 0"
                + LogTail(ctx));
            Check(a.Attack, 3,
                  "★★ ⑤-b 而且效果真的落了地：攻击者 2 → 3（待死单位仍在棋盘上，`DoGive` 的逐目标闸放行）"
                + "｜🧨 同上 ⇒ 实得 2" + LogTail(ctx));
        }

        // ⚠️ **本组不测「星镖击杀」那一半**（`defenderKilledByShuriken(+0x139)`）：读原版 `:632`
        //   `*(undefined2 *)(+0x138) = 0x100`（= `0x138=0` **且** `0x139=1`）⇒ 星镖击杀**同时**关掉反击；
        //   而我们现在的 `targetDied` 一个布尔同时表示「星镖杀」与「主伤害杀」，主伤害杀**不该**免反击
        //   （规则书 `:145` 那个例子：被打死的初生者照样反击 1 点）⇒ 要精确收口得拆两个标志，
        //   那是**另一条**结构差（已写进 `B145_E22` 报告 §顺手发现，⛔ 不在本笔里改）。
    }

    /// <summary>从 `from` 开始找第一条含 `needle` 的日志，返回下标；找不到 = -1。⛔ 别整串搜（旧的同名日志会串味）。</summary>
    static int B145E22FirstIndexOf(BattleContext ctx, int from, string needle)
    {
        if (ctx == null || ctx.Events == null) return -1;
        for (int i = from; i < ctx.Events.Count; i++)
            if (ctx.Events[i] != null && ctx.Events[i].Contains(needle)) return i;
        return -1;
    }
}
