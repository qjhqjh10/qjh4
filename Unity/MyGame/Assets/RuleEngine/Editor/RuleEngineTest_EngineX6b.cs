// RuleEngineTest_EngineX6b.cs — 引擎线 `X6`（第二批 · 2026-10-25 · 执行代理 `X6` · 口径乙）
//
// 为什么单独一个文件：`RuleEngineTest_EngineX6.cs` 那一批（`T1`–`T5`）验的是**数据层 + 出牌路**
// （卡表 v29 的 `ca` / 按族认行 / `ResolveOps` 的 `abilityFamilyCard`）；本文件验的是
// **单位 trigger 那一跳**（`RuleCore.FireTriggerAt` → `AbilityFamilyGateOpsForTrigger`）——
// 两条路的入口/参数完全不同（一条是「出牌正文体」、一条是「关键词触发体」），混在一个方法里
// 出错时分不清是哪条路。⛔ 别把两组断言合并。
//
// 判据（⛔ 不要重新论证）→ `资料/待办判据_引擎_1025b.md` §一 ＋ `X6_口径乙ca列_执行报告.md` §六·2。
// 🔴 本轮的白名单只有 `Core/RuleCore.cs` + 本批测试文件（`Core/EffectResolver.cs` 当时有别的写手）。
//
// 覆盖（判据文档 §一 里那 5 张**单位**卡）：
//   · 可判 2 张：`BL33 Vex Machinator`（`destroy` 族 + `keywordFilter=10` Vehicle）·
//     `DA38 Repulsor`（`resource` 族 + `enemyUnits=1`）；
//   · 判不了 3 张：`ASH30` / `DA33` / `SOR31`（家族不唯一）⇒ **出声一次 + 仍放行**。
//
// 🔴 每条都带 `🧨`（什么改动能让它红）。期望值一律写死字面量，⛔ 不从被测实现反算。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（`Run()` 里一行 `Step(TestEngineX6b)`）。</summary>
    public static void TestEngineX6b()
    {
        Section("🆕 引擎线 X6b：口径乙 · **单位 trigger 那一跳**（`FireTriggerAt` 的族闸）");
        X6T6UnitTriggerGate();
    }

    /// <summary>
    /// `RuleCore.FireTriggerAt` 的族闸（`AbilityFamilyGateOpsForTrigger`）。
    ///
    /// <para>🔴 **闸门只在「按 trigger 定位不唯一」的卡上启用** —— `BL33`/`DA38` 的 `trigger 0`
    /// 各有两条 ability（实测），所以 `UniqueAbilityIndexForTrigger("ThisCardPlayed")` 回 `-1`+`ambiguous`
    /// ⇒ 启用。其余单位卡（`Rally` 唯一）**行为逐字不变**。</para>
    /// <para>⚠️ **今天 `Rally` 那一跳不传 `chosen`**（`FireTriggerOnBoard` → `FireTriggerAt(…, owner, slot)`）
    /// ⇒ 生产环境里这 5 张都是「判不了 ⇒ 出声 + 放行」。本件的断言**显式传 `chosen`**
    /// 把「判据本身对不对」钉住 —— 传 `chosen` 是合法调用形状（`AboutToAttack` / 替代行动那两族就在传），
    /// 只是 `Rally` 今天还没接上（如实标着，见 `AbilityFamilyGateOpsForTrigger` 的 doc）。</para>
    /// </summary>
    static void X6T6UnitTriggerGate()
    {
        var pool = CardDatabase.Load();
        var bl33 = X6UnitCard(pool, "BL33");
        var da38 = X6UnitCard(pool, "DA38");
        var ash30 = X6UnitCard(pool, "ASH30");
        var da33 = X6UnitCard(pool, "DA33");
        var sor31 = X6UnitCard(pool, "SOR31");
        var vehicle = X6UnitCard(pool, "ASH61");      // Weapons Platform：`subtype = Vehicle`（真卡，不是合成）
        CheckTrue(bl33 != null && da38 != null && ash30 != null && da33 != null && sor31 != null && vehicle != null,
                  "★ ⑥ 六张夹具卡都在池内（`BL33`/`DA38`/`ASH30`/`DA33`/`SOR31`/`ASH61`）");
        if (bl33 == null || da38 == null || ash30 == null || da33 == null || sor31 == null || vehicle == null) return;
        Check(vehicle.Subtype, "Vehicle", "★ ⑥ 靶子 `ASH61` 的 `subtype` = **Vehicle**（`keywordFilter=10` 走 `CreatePool.MatchesKind`）");

        // ---- ① BL33：非载具目标 ⇒ `destroy` 那一族被剔；载具目标 ⇒ 保留（灭自证） ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var carrier = Place(ctx, 0, 3, bl33);
            var plain = Place(ctx, 1, 3, Unit("X6Plain", 1, 1, 6));    // 无 subtype ⇒ 不是 Vehicle
            RuleCore.ResetAbilityFamilyGateForTest();
            bool q = RuleCore.FireTriggerAtForTest(ctx, carrier, KeywordTable.Rally, 0, 3, chosen: plain);
            CheckTrue(q, "★ ⑥ BL33 的 `Rally` 入队了（`FireTriggerAt` 回 true）");
            Check(RuleCore.AbilityFamilyGateSkipForTest, 1,
                  "★★ ⑥ BL33 `Rally` + **非载具**敌方目标 ⇒ `destroy` 那一族被**剔掉**（skip 计数 = 1）"
                + "｜🧨 族闸没接上 `FireTriggerAt` / 认行错 / 恒放行 ⇒ 0 ⇒ 红");
            Check(RuleCore.AbilityFamilyGateWarningsForTest, 0,
                  "★ ⑥ BL33 是**可判**的那一档 ⇒ 不出声（计数 = 0）");
            RuleCore.PumpActionQueues(ctx);
            CheckTrue(plain.IsAlive && plain.Health == 1,
                  $"★ ⑥ BL33 结算后：非载具目标只吃到 `Deal 5`（6 → 1，仍活着；实得 {plain.Health}）"
                + "｜🧨 那一族没被剔时 `destroy` 会跑（它还有文本条件挡着，所以这一条只作旁证；判据以 skip 计数为准）");
        }
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var carrier = Place(ctx, 0, 3, bl33);
            var veh = Place(ctx, 1, 3, vehicle);
            RuleCore.ResetAbilityFamilyGateForTest();
            RuleCore.FireTriggerAtForTest(ctx, carrier, KeywordTable.Rally, 0, 3, chosen: veh);
            Check(RuleCore.AbilityFamilyGateSkipForTest, 0,
                  "★★ 灭自证 ⑥ BL33 `Rally` + **载具**敌方目标 ⇒ criteria 通过 ⇒ **不剔**（skip 计数 = 0）"
                + "｜🧨 若实现变成「恒剔」/「按 `subtype` 判反」⇒ 这条红");
        }

        // ---- ② DA38：友方目标 ⇒ `resource` 族（`enemyUnits=1`）被剔；敌方目标 ⇒ 被第 ⑧ 格剔 ----
        //   （两个方向都「剔」，但**原因不同**：前者是阵营闸、后者是 `cond 220 dealtKillDmg ex=0`
        //     —— 这一对同时把 criteria 的两半都验到了，⛔ 别把「都剔」当成「恒剔」；
        //     「恒剔」会由 ①·BL33 的载具目标那条（skip = 0）打红。）
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var carrier = Place(ctx, 0, 3, da38);
            var mate = Place(ctx, 0, 5, Unit("X6Mate", 1, 1, 8));
            RuleCore.ResetAbilityFamilyGateForTest();
            RuleCore.FireTriggerAtForTest(ctx, carrier, KeywordTable.Rally, 0, 3, chosen: mate);
            Check(RuleCore.AbilityFamilyGateSkipForTest, 1,
                  "★★ ⑥ DA38 `Rally` + **友方**目标 ⇒ `resource` 那一族（`enemyUnits=1`）被**剔掉**（skip = 1）"
                + "｜🧨 阵营闸没跑 / 族认错 ⇒ 红");

            var ctx2 = Battle(new CardDef[0], new CardDef[0]);
            var carrier2 = Place(ctx2, 0, 3, da38);
            var foe = Place(ctx2, 1, 3, Unit("X6Foe", 1, 1, 8));
            RuleCore.ResetAbilityFamilyGateForTest();
            RuleCore.FireTriggerAtForTest(ctx2, carrier2, KeywordTable.Rally, 0, 3, chosen: foe);
            // ⚠️ 敌方 ⇒ **仍然剔**：`DA38 ab=1` 那行带 `unitConditions role="target" cond=220 dealtKillDmg ex=0`
            //   （= 原版同一个 `TargetCriteria` 的第 ⑧ 格）—— 那个新摆的敌方单位没造成过击杀伤害 ⇒ 不满足。
            Check(RuleCore.AbilityFamilyGateSkipForTest, 1,
                  "★★ ⑥ DA38 `Rally` + **敌方**目标 ⇒ 仍被剔 —— 因为第 ⑧ 格 `cond 220 dealtKillDmg ex=0` 拼进来了"
                + "｜🧨 若 `AbilityLevelConditionRowAt`（第 ⑧ 格）被拿掉 ⇒ 这条变 0 ⇒ 红"
                + "（这一对**同时**证明了「阵营闸」与「第 ⑧ 格」两半都在跑：友方靠前者、敌方靠后者）");
        }

        // ---- ③ 3 张判不了：出声一次 + 仍放行（正文照跑）+ 第二次不再出声 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var crash = new (CardDef card, UnitState carrier, UnitState target)[3];
            RuleCore.ResetAbilityFamilyGateForTest();
            int w0 = RuleCore.AbilityFamilyGateWarningsForTest;
            // 每张卡摆一台自己的载体 + 一个敌方目标（目标 6 血，足够扛住那几点伤害）
            crash[0] = (ash30, Place(ctx, 0, 3, ash30), Place(ctx, 1, 3, Unit("X6E0", 1, 1, 6)));
            crash[1] = (da33, Place(ctx, 0, 5, da33), Place(ctx, 1, 5, Unit("X6E1", 1, 1, 6)));
            crash[2] = (sor31, Place(ctx, 0, 6, sor31), Place(ctx, 1, 6, Unit("X6E2", 1, 1, 6)));
            int[] slots = { 3, 5, 6 };
            for (int i = 0; i < 3; i++)
            {
                bool q = RuleCore.FireTriggerAtForTest(ctx, crash[i].carrier, KeywordTable.Rally, 0, slots[i],
                                                       chosen: crash[i].target);
                CheckTrue(q, $"★ ⑥ 「{crash[i].card.Name}」的 `Rally` **仍入队**（判不了 ⇒ 放行，不是不触发）");
            }
            int w1 = RuleCore.AbilityFamilyGateWarningsForTest;
            Check(w1 - w0, 3,
                  "★★ ⑥ 3 张判不了的单位 ⇒ **各出声一次**（+3）"
                + "｜🧨 判不了却静默放行 ⇒ 0 ⇒ 红；按「族」去重（`DA33` 两族会喊两遍）⇒ 4 ⇒ 也红");
            Check(RuleCore.AbilityFamilyGateSkipForTest, 0,
                  "★★ ⑥ 3 张判不了 ⇒ **一条 op 都不剔**（skip = 0，仍放行）"
                + "｜🧨 把「判不了」当「不满足」⇒ 正文被整条掐死 ⇒ 红");
            RuleCore.PumpActionQueues(ctx);
            CheckTrue(crash[0].target.Health < 6,
                  $"★ ⑥ 放行的证据：ASH30 的 `Rally` 正文真的跑了（目标 6 → {crash[0].target.Health}）"
                + "｜🧨 判不了却把正文丢了 ⇒ 血量不变 ⇒ 红");

            // 去重：同一张卡再触发一次 ⇒ 不再出声
            var carrier2 = Place(ctx, 0, 4, ash30);
            RuleCore.FireTriggerAtForTest(ctx, carrier2, KeywordTable.Rally, 0, 4, chosen: crash[0].target);
            Check(RuleCore.AbilityFamilyGateWarningsForTest, w1,
                  "★★ ⑥ 同卡第二次 ⇒ **不再出声**（去重表生效；计数不动）");
        }
    }

    /// <summary>池内取一张**单位**卡（找不到 ⇒ `null`；测试会先断「找得到」）。</summary>
    static CardDef X6UnitCard(List<CardDef> pool, string id)
    {
        var c = X6Card(pool, id);
        return c != null && string.Equals(c.Type, "unit", StringComparison.Ordinal) ? c : null;
    }
}
