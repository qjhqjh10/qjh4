// RuleEngineTest_EngineB94Y1.cs — 第九十四会话 · 波 1.5 · 执行代理 `Y1`（`RuleCore.cs` 文件所有者补丁）
//
// 为什么另开文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆）；宿主
// `RuleEngineTest.cs` 归别人写 ⇒ 本件只放本批**行为改动**的断言、只交一个入口方法名
// `CheckB94Y1TroopKilled`（⛔ 不用 `Test`/`Report` 打前缀 —— 那样会被 `VerifyRunCaseCoverage` /
// `gen_diag_runner` 认成用例），由调度台在 `Run()` 里接线。
//
// 本批覆盖的一条**真实行为改动**（判据 = `d:/2/tools/decomp_full/`，逐句亲读 2026-10-29）：
//   `TroopKilled` 的**计数口径** —— 原版 `BattleManager._ResolveMinionDeath_d__454__MoveNext.c:112`
//   对**每一次**小兵死亡结算**无条件**调 `ChallengeLogMgr.TroopKilled`；接收方
//   `ChallengeLogMgr__TroopKilled.c` 本身只按**死者** `+0x40(isPlayer)` 选边
//   （`stats.enemyTroopsDied += 1`，`PlayerBattleStats(+0x10)` / `EnemyBattleStats`），
//   **不看**凶手死活 / 凶手是不是行动方 / 死者是不是残骸。
//
//   改前我们把 `ctx.Tally.AddTroopKilled(kOwner)` 挂在 `RuleCore.TryCreditKillCore` 里、带 6 道闸
//   ⇒ **少计三类**：① 凶手不是当前行动方（`kOwner != ctx.Active`）· ② 凶手已死（`!killer.IsAlive`）·
//   ③ 被摧毁的是**残骸**（`victim.IsRemnant`）。本批把计数口移到 `CleanupDeaths`
//   （每次小兵真离场一次：`leftPlay = true;` 行 + `if (u.IsRemnant)` 支），按**凶手座位**记
//   （`killer` 座位；缺失时取 `actingCard` 的主人 = 消费者 `TroopsKilledCount` 读的 `killer.isPlayer`）。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）；期望值一律写死字面量，⛔ 不从被测实现反算。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（由调度台在宿主 `Run()` 里接线；⛔ 不用 `Test` 前缀）。</summary>
    public static void CheckB94Y1TroopKilled()
    {
        Section("🆕 B94_Y1：`TroopKilled` 计数口径（原版 `_ResolveMinionDeath.c:112` 无条件发）"
              + " —— 凶手非行动方 / 凶手已死 / 残骸 三档都计到");
        B94Y1T1KillerNotActive();
        B94Y1T2KillerDead();
        B94Y1T3VictimIsRemnant();
        B94Y1T4NegativeControls();
    }

    /// <summary>造一个已打死/待打的普通单位（`Health` = <paramref name="hp"/>、无护甲、无关键词）。
    /// ⚠️ 直接摆棋盘 —— `HurtWithSourceForTest` 用 `FindUnit` 扫棋盘找格位（= 真实死亡入口）。</summary>
    static UnitState B94Y1Put(BattleContext ctx, int owner, int slot, string name, int hp)
    {
        var u = new UnitState(ctx.NewInstance(Unit(name, 1, 1, hp)), false);
        ctx.Players[owner].Board[slot] = u;
        return u;
    }

    // ==============================================================================================
    //  ① 凶手【不是当前行动方】—— 原版不计回合归属；改前被 `kOwner != ctx.Active` 闸吃掉
    // ==============================================================================================
    static void B94Y1T1KillerNotActive()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Active = 0;                                  // 行动方 = 0；凶手在 1 ⇒ 凶手不是行动方
        var victim = B94Y1Put(ctx, 0, 6, "Y1VictimA", 2);   // 死者归 0（p = 0）
        var killer = B94Y1Put(ctx, 1, 6, "Y1KillerA", 3);   // 凶手归 1（活着、在场、≠ 行动方）

        int beforeSide1 = ctx.Tally.TroopsKilledOf(1);
        int beforeSide0 = ctx.Tally.TroopsKilledOf(0);

        RuleCore.HurtWithSourceForTest(ctx, victim, 99, "Y1T1",
                                       killer: 1, actingCard: killer, deathType: UnitDeathType.Ability);

        CheckTrue(!victim.IsAlive, "① （前提）`victim` 已被打死（`!IsAlive`）");
        Check(ctx.Tally.TroopsKilledOf(1) - beforeSide1, 1,
              "★★ ① 凶手（= 座 1）**不是当前行动方** ⇒ 击杀**照样计 +1**（记给凶手座位 = 1）"
            + "｜判据 `_ResolveMinionDeath_d__454__MoveNext.c:112` 无条件调 `TroopKilled`（不看回合）"
            + "｜🧨 把计数移回 `TryCreditKillCore` 的 `if (kOwner != ctx.Active) return false;` 之后 ⇒ 实得 0 ⇒ 红");
        Check(ctx.Tally.TroopsKilledOf(0) - beforeSide0, 0,
              "① 死者自己那一侧（座 0）**不**加（原版写的是死者【对面】的 `enemyTroopsDied`）"
            + "｜🧨 把凶手座位写反成死者自己（`p`）⇒ 座 0 那条红");
    }

    // ==============================================================================================
    //  ② 凶手【已死】—— 原版不看凶手死活；改前被 `!killer.IsAlive` 闸吃掉
    // ==============================================================================================
    static void B94Y1T2KillerDead()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Active = 0;
        var killer = B94Y1Put(ctx, 1, 6, "Y1KillerB", 2);
        // 先把「凶手」打死（无来源），它就不在棋盘上了。
        RuleCore.HurtWithSourceForTest(ctx, killer, 99, "Y1T2-pre",
                                       killer: -1, actingCard: null, deathType: UnitDeathType.Unknown);
        CheckTrue(!killer.IsAlive, "② （前提）`killer` 已被打死（`!IsAlive`）");

        var victim = B94Y1Put(ctx, 0, 6, "Y1VictimB", 2);   // 死者归 0
        int beforeSide1 = ctx.Tally.TroopsKilledOf(1);

        RuleCore.HurtWithSourceForTest(ctx, victim, 99, "Y1T2",
                                       killer: 1, actingCard: killer, deathType: UnitDeathType.Ability);

        CheckTrue(!victim.IsAlive, "② （前提）`victim` 已被打死");
        Check(ctx.Tally.TroopsKilledOf(1) - beforeSide1, 1,
              "★★ ② 凶手**已死**（不在棋盘上）⇒ 击杀**照样计 +1**（记给凶手座位 = 1）"
            + "｜判据同上：`TroopKilled` 不看凶手死活（原版只判死者 `+0x40`）"
            + "｜🧨 把计数移回 `TryCreditKillCore` 的 `if (!killer.IsAlive) return false;` / `if (!FindSlot(...)) return false;` 之后 ⇒ 实得 0 ⇒ 红");
    }

    // ==============================================================================================
    //  ③ 死者是【残骸】—— 原版把「残骸被摧毁」也算一次部队阵亡；改前被 `victim.IsRemnant` 闸吃掉
    // ==============================================================================================
    static void B94Y1T3VictimIsRemnant()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Active = 0;
        // 残骸 = 场上一个 `IsRemnant` 的 1 血单位（受任意一下即被摧毁）。
        var rem = B94Y1Put(ctx, 0, 6, "Y1Remnant", 1);
        rem.IsRemnant = true;
        CheckTrue(rem.IsRemnant && rem.IsAlive, "③ （前提）残骸在场上、还活着（1 血）");

        int beforeSide1 = ctx.Tally.TroopsKilledOf(1);

        RuleCore.HurtWithSourceForTest(ctx, rem, 99, "Y1T3",
                                       killer: 1, actingCard: null, deathType: UnitDeathType.Unknown);

        CheckTrue(!rem.IsAlive, "③ （前提）残骸已被摧毁");
        Check(ctx.Tally.TroopsKilledOf(1) - beforeSide1, 1,
              "★★ ③ 被摧毁的是**残骸**（`IsRemnant`）⇒ 击杀**照样计 +1**（记给凶手座位 = 1）"
            + "｜判据：`_ResolveMinionDeath.c:112` 对残骸被摧毁同样无条件发 `TroopKilled`"
            + "｜🧨 把计数移回 `TryCreditKillCore` 的 `if (victim.IsRemnant) return false;` 之后 ⇒ 实得 0 ⇒ 红");
    }

    // ==============================================================================================
    //  ④ 灭自证 / 边界：正常一死**只计一次**（不多计）· 督军倒下**不**计
    // ==============================================================================================
    static void B94Y1T4NegativeControls()
    {
        // ── ④-a 同时伤害批里的一死（走 `FlushDeaths` 那条真路径、并把队列泵完）⇒ 恰好 +1 ────────
        //   外层再包一层 `BeginAction` ⇒ 内层批 flush 之后才泵队，让 `FlushDeaths` 排的 slay-credit
        //   （→ `TryCreditKill`）也真的跑一遍：这两条路**一共只许计一次**。
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ctx.Active = 0;
            var killer = B94Y1Put(ctx, 0, 6, "Y1KillerD", 3);   // 凶手：座 0（= 行动方、活着、在场）
            var victim = B94Y1Put(ctx, 1, 6, "Y1VictimD", 2);   // 死者：座 1（p = 1）
            int beforeSide0 = ctx.Tally.TroopsKilledOf(0);

            using (RuleCore.BeginAction(ctx))          // 外层：批 flush 之后再泵队
            using (RuleCore.SimultaneousDamage(ctx))   // 内层：先 Dispose ⇒ FlushDeaths
            {
                RuleCore.HurtWithSourceForTest(ctx, victim, 99, "Y1T4",
                                               killer: 0, actingCard: killer, deathType: UnitDeathType.Ability);
            }

            CheckTrue(!victim.IsAlive, "④-a （前提）`victim` 在批末被结算死");
            Check(ctx.Tally.TroopsKilledOf(0) - beforeSide0, 1,
                  "★★ ④-a 正常击杀（凶手活着/在场/是行动方）走「同时伤害批 + 泵队」整条真路径 ⇒ **恰好 +1**（记给座 0）"
                + "｜🧨 ① 只留 `CleanupDeaths` 那格被删 ⇒ 0 · ② 把计数**同时**留在 `TryCreditKillCore`（批泵里 slay-credit 会再记一次）⇒ 2 ⇒ 红");
        }

        // ── ④-b 督军倒下 ⇒ **不**计（原版 `_ResolveMinionDeath` 只服务小兵；督军不走那条路）──────
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ctx.Active = 0;
            var warlord = ctx.Players[0].Warlord;
            CheckTrue(warlord != null && warlord.IsWarlord, "④-b （前提）拿到座 0 的督军（`IsWarlord`）");
            if (warlord != null)
            {
                int before0 = ctx.Tally.TroopsKilledOf(0);
                int before1 = ctx.Tally.TroopsKilledOf(1);
                RuleCore.HurtWithSourceForTest(ctx, warlord, 99, "Y1T4b",
                                               killer: 1, actingCard: null, deathType: UnitDeathType.Unknown);
                Check(ctx.Tally.TroopsKilledOf(0) - before0, 0,
                      "★★ ④-b 督军倒下 ⇒ 击杀计数**不变**（`CleanupDeaths` 的 `if (u.IsWarlord) return;` 在计数之前）"
                    + "｜🧨 把 `AddTroopKilled` 挪到 `u.IsWarlord` 判断【之前】⇒ 实得 1 ⇒ 红");
                Check(ctx.Tally.TroopsKilledOf(1) - before1, 0, "④-b 督军倒下不给对面加（同上）");
            }
        }

        // ── ④-c 幸存者救回 ⇒ 不死亡、**不**计（`maySurvive && Survivor>=1` 那一支 return 在计数之前）──
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ctx.Active = 0;
            var saved = B94Y1Put(ctx, 0, 6, "Y1Saved", 2);
            saved.AddKeyword(KeywordTable.Survivor, 1);      // `Survivor` = `KwValue(survivor)` ⇒ 挂上即 = 1
            bool hasSurvivor = saved.Survivor >= 1;
            // 前提不成立（挂不上 survivor）⇒ 如实跳过下面那条（⛔ 不静默把它当通过）。
            CheckTrue(hasSurvivor, "④-c （前提）能给夹具挂上 `survivor`（`saved.Survivor >= 1`）");
            if (hasSurvivor)
            {
                int before0 = ctx.Tally.TroopsKilledOf(0);
                int before1 = ctx.Tally.TroopsKilledOf(1);
                RuleCore.HurtWithSourceForTest(ctx, saved, 99, "Y1T4c",
                                               killer: 1, actingCard: null, deathType: UnitDeathType.Unknown);
                CheckTrue(saved.IsAlive, "④-c （前提）幸存者把单位救了回来（仍 `IsAlive`）");
                Check(ctx.Tally.TroopsKilledOf(0) - before0, 0, "★★ ④-c 幸存者留场 ⇒ 座 0 击杀数不变");
                Check(ctx.Tally.TroopsKilledOf(1) - before1, 0,
                      "★★ ④-c 幸存者留场（没真的死）⇒ 击杀计数**不变**（计数在 `maySurvive` 早退之后）"
                    + "｜🧨 把计数挪到 `if (maySurvive && u.Survivor >= 1)` 那一支【之前】⇒ 实得 1 ⇒ 红");
            }
        }
    }
}
