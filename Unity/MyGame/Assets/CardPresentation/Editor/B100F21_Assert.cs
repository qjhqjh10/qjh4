// B100F21_Assert.cs — 第一百会话 · 执行代理 `B100_F21`（`报告/B100_X8.md` §二·2 / §二·3 / §三）
//
// 为什么另开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆），
// 同一时刻只有调度台能改 `RuleEngineTest.cs` ⇒ 本批断言全写在这里、**只交一个入口方法名**
// `CheckB100F21RemnantSlay`，由调度台在 `Run()` 里加一行（⛔ 本代理不改调度台的文件）。
//
// 本件钉的是 2026-10-30 `B100_F21` 落在 `RuleCore.cs` 的**两处同源修复**（零行数变化，都写在同一物理行上）：
//   ① **`:9913` 残骸翻面支**：`u.ResetToValuesInHand(clearBacklash:false)` 之后补 `u.Health = 0;`
//      —— 那个对象是**墓碑**（档位已被新造的 `rem` 顶替），`ResetToValuesInHand` 末尾的
//      `Health = MaxHealth`（`UnitState.cs:2646`）会把它**复活成满血**，于是 `FlushDeaths` 随后入队的
//      `TryCreditKill` 撞第 ④ 道闸 `if (victim.IsAlive) return false;`（`RuleCore.cs:6265`）
//      ⇒ `Slay` 不响、击杀记账整笔丢。
//      判据 = `报告/B100_X8.md` §三：原版这一跳（`_ResolveTransformIntoRemnant_d__554__MoveNext.c:43`）
//      跑在 `BattleManager__AddTransformIntoRemnant.c:99` 那条**排队 action** 体内，而 Slay 记账在
//      `CardScript__ResolveDeadCard.c:~232-259` 的 death action 里 ⇒ 记账那一刻死者**还是死的**
//      ⇒ **原版把「打死一个会变残骸的真单位」算作击杀**（`isRemnant` 仍是 0、血仍是死时的值）。
//   ② **`:10060` 死讯 `Amount`**：从「现读 `_du.KwValue("huntmark")`」改成「读 `:9794` 那一格快照
//      `huntMarks`」。残骸支的 `ResetToValuesInHand` 已 `_keywords.Clear()` 再按卡面重建
//      （`UnitState.cs:2642-2645`）⇒ 运行时挂上去的 `huntmark` 没了 ⇒ `EvtKind.Death.Amount` 恒 0。
//      原版 `CardScript__TriggerOnMinionDeath.c:77` 在 death action 里直调、早于那一跳 ⇒ 读到 2。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · 把 `:9913` 的 `u.Health = 0;` 删掉 ⇒ §A 的 A3（墓碑仍是活的）与 A4（Slay 不响）**同时红**
//     —— 这两条**结构上不可能同时满足**：要么墓碑活着且不记账，要么墓碑死着且记账。
//   · 把 `:10060` 改回 `_du.KwValue("huntmark")`（或任何「重置之后再现读」的写法）⇒ §B 的 B2 红（实得 0）。
//   · 把 `TryCreditKill` 的闸 ④（`victim.IsAlive`）整个删掉 ⇒ §A 的 A5 控制支红
//     （「不带 `Remnant` 的真死支」本来该绿，闸没了会让**已复活的墓碑**也被记账 —— 那条控制支在这里
//       钉的是「同一条记账口两条路都走得到」，删闸会让上面 A3/A4 的判别力失去意义）。
//
// ============================ 怎么跑（⚠️ 执行代理无权跑 Unity）============================
//   接线由调度台做（`RuleEngineTest.Run()`）：加一行 `CheckB100F21RemnantSlay();`。
//   判绿 = 末行 `合计：N 通过 / 0 失败`。
using System;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（调度台在 `Run()` 里加一行）。</summary>
    public static void CheckB100F21RemnantSlay()
    {
        Section("🆕 B100_F21：残骸翻面不许把「已判死」的墓碑对象复活 · 死讯 Amount 取死亡那一刻的猎杀标记");
        B100F21T1TombstoneStaysDead();
        B100F21T2DeathAmountIsHuntMarkAtDeath();
    }

    // ==============================================================================================
    //  ① 打死一个**带 `Remnant` 的真单位** ⇒ 墓碑对象必须仍是死的、Slay 必须响
    //     判据 = `报告/B100_X8.md` §三·1~4（原版记账那一刻 `isRemnant == 0` 且血仍是死时的值）
    // ==============================================================================================

    static void B100F21T1TombstoneStaysDead()
    {
        var slayer = Unit("B100F21Slayer", 4, 5, 6, "Slay: Damage 2 EnemyWarlord");
        // 2 血 ⇒ 攻击力 5 一刀打死；带 `Remnant` ⇒ 会走 `CleanupDeaths` 的残骸翻面支
        var remCard = new CardDef("B100F21Rem", "B100F21Rem", "unit", "", "common", "Test",
                                  1, 0, 2, 0, new[] { KeywordTable.Remnant }, subtype: "Infantry");

        UnitState atk, victim;
        var ctx = Duel(slayer, remCard, out atk, out victim);
        ctx.ClearSignals();
        var lord = ctx.Players[1].Warlord;
        int lordHp = lord.Health;

        CheckTrue(atk != null && victim != null, "①（前提）双方都摆到了场上");
        CheckTrue(victim != null && victim.IsAlive, "①（前提）受害者挨打前**是活的**（2 血）");
        CheckTrue(atk != null && atk.IsAlive, "①（前提）攻方活着且未力竭");
        if (victim == null || atk == null) return;

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK, "① 斩杀那一刀");

        var occ = Board(ctx, 1, 3);
        CheckTrue(occ != null && occ.IsRemnant,
                  "①（前提）那一格**翻面成了残骸**（`CheckIfDead.c:110-147` 的变残骸支）" + LogTail(ctx));
        CheckTrue(occ != null && occ.IsAlive, "①（前提）新造的残骸对象是活的（`Health = 1`）");

        // ---- 🔴 本件的主角：**墓碑对象**（被换下场的那个 `UnitState`）必须仍然是死的 ----
        CheckTrue(!victim.IsAlive,
                  "★★ ① 被换下场的**墓碑对象**必须仍是死的 —— `ResetToValuesInHand` 末尾的 " +
                  "`Health = MaxHealth`（`UnitState.cs:2646`）会把它复活成满血" +
                  "｜🧨 删掉 `RuleCore.cs:9913` 的 `u.Health = 0;` ⇒ 实得 IsAlive=True ⇒ 红");
        CheckTrue(!ReferenceEquals(occ, victim),
                  "①（前提）那一格上挂的是**新对象**（`rem`），不是被换下来的墓碑" + LogTail(ctx));

        // ---- 行为面：Slay 必须响（原版在 death action 里记账，那一刻死者还没被重置）----
        Check(lord.Health, lordHp - 2,
              "★★ ① 打死**会变残骸的真单位** ⇒ **算击杀**（`Slay: Damage 2 EnemyWarlord` 生效）" +
              "｜🧨 同上：墓碑活着 ⇒ `TryCreditKill` 第 ④ 道闸 `if (victim.IsAlive) return false;`" +
              "（`RuleCore.cs:6265`）把这一笔记账整个吃掉 ⇒ 实得 " + lordHp + " ⇒ 红" + LogTail(ctx));
        CheckTrue(HasSignal(ctx, EvtKind.Trigger, KeywordTable.Slay), "★★ ① 事件流里有 Slay");

        // ---- 控制支（灭自证的另一半）：**不带 `Remnant`** 的真死支 —— 它本来就该记账 ----
        //   两条支**同一张攻方卡、同一个格位、同一个致死数字**，唯一变量 = 「会不会翻面成残骸」
        //   ⇒ 把两边的结果都钉住，就不可能靠「一边改回去」同时变绿。
        UnitState atk2, victim2;
        var ctx2 = Duel(Unit("B100F21Slayer2", 4, 5, 6, "Slay: Damage 2 EnemyWarlord"),
                        Unit("B100F21Plain", 1, 0, 2), out atk2, out victim2);
        ctx2.ClearSignals();
        var lord2 = ctx2.Players[1].Warlord;
        int lord2Hp = lord2.Health;
        CheckCode(RuleCore.DeclareAttack(ctx2, 0, 3, 1, 3), RuleCodes.OK, "① 对照：打一个**不带残骸**的 2 血单位");
        Check(Board(ctx2, 1, 3), null, "① 对照（前提）目标真死离场");
        Check(lord2.Health, lord2Hp - 2,
              "★★ ① 对照：真死支照样记账（`Slay` 响）—— 这条**改前改后都绿**，" +
              "它保证上面的 A4 不是「Slay 整条链被关掉」的假绿" + LogTail(ctx2));
    }

    // ==============================================================================================
    //  ② 残骸翻面支的 `EvtKind.Death.Amount` = **死亡那一刻**的猎杀标记数
    //     判据 = `报告/B100_X8.md` §二·3：原来 `:10060` 在 `ResetToValuesInHand`（`:9913`）**之后**现读
    //            ⇒ `_keywords` 已被清空重建 ⇒ 恒 0；原版 `CardScript__TriggerOnMinionDeath.c:77`
    //            在 death action 里直调、早于翻面那一跳 ⇒ 读到 2。
    //     🔴 两条支（带/不带 `Remnant`）**要求同一个值**（都是 2）—— 改前是「带残骸 0 / 真死 2」。
    // ==============================================================================================

    static void B100F21T2DeathAmountIsHuntMarkAtDeath()
    {
        // ---- 带 `Remnant`：这就是改前恒 0 的那一支 ----
        {
            var remCard = new CardDef("B100F21HuntRem", "B100F21HuntRem", "unit", "", "common", "Test",
                                      1, 0, 2, 0, new[] { KeywordTable.Remnant }, subtype: "Infantry");
            UnitState atk, victim;
            var ctx = Duel(Unit("B100F21Slayer3", 4, 5, 6), remCard, out atk, out victim);
            CheckTrue(victim != null, "②（前提）受害者摆到了场上");
            if (victim == null) return;
            victim.AddKeyword("huntmark", 2);
            Check(victim.KwValue("huntmark"), 2, "②（前提）夹具真的挂上了 `huntmark 2`");
            ctx.ClearSignals();

            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK, "② 斩杀那一刀（带猎杀标记）");
            var occ = Board(ctx, 1, 3);
            CheckTrue(occ != null && occ.IsRemnant, "②（前提）它翻面成了残骸（走进了残骸翻面支）" + LogTail(ctx));

            var death = B100F21DeathOf(ctx, victim.InstanceId);
            CheckTrue(death != null, "②（前提）发过这一份牌的死讯信号" + LogTail(ctx));
            if (death != null)
                Check(death.Amount, 2,
                      "★★ ② 残骸翻面支的 `EvtKind.Death.Amount` = **死亡那一刻**的猎杀标记数（2）" +
                      "｜🧨 把 `RuleCore.cs:10060` 改回 `_du.KwValue(\"huntmark\")`（= 重置之后再现读）" +
                      "⇒ `_keywords` 已被 `ResetToValuesInHand` 清空重建 ⇒ 实得 0 ⇒ 红");
        }

        // ---- 不带 `Remnant`（控制支）：同一份夹具、只差「会不会翻面」 ⇒ 也必须是 2 ----
        {
            UnitState atk, victim;
            var ctx = Duel(Unit("B100F21Slayer4", 4, 5, 6), Unit("B100F21HuntPlain", 1, 0, 2),
                           out atk, out victim);
            CheckTrue(victim != null, "②（前提）对照支的受害者摆到了场上");
            if (victim == null) return;
            victim.AddKeyword("huntmark", 2);
            ctx.ClearSignals();

            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK, "② 对照：真死支（带猎杀标记）");
            Check(Board(ctx, 1, 3), null, "② 对照（前提）目标真死离场");
            var death = B100F21DeathOf(ctx, victim.InstanceId);
            CheckTrue(death != null, "② 对照（前提）发过这一份牌的死讯信号" + LogTail(ctx));
            if (death != null)
                Check(death.Amount, 2,
                      "★★ ② 对照：真死支的 `Amount` 也是 2（改前改后都该绿 —— 它保证上面那条不是" +
                      "「整个 Amount 被写死成 2」的假绿）" + LogTail(ctx));
        }
    }

    /// <summary>按**实例号**找那一份牌的死讯信号（`EvtKind.Death` 的 `InstanceId`）。
    /// ⚠️ 不按 `Player/Slot` 找：连续棋盘下格号会被补位改掉。</summary>
    static BattleEvent B100F21DeathOf(BattleContext ctx, int instanceId)
    {
        if (ctx == null || ctx.Signals == null) return null;
        foreach (var e in ctx.Signals)
            if (e != null && e.Kind == EvtKind.Death && e.InstanceId == instanceId) return e;
        return null;
    }
}
