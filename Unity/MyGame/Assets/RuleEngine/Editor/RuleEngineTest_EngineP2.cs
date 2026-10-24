// RuleEngineTest_EngineP2.cs — 执行代理 `E1`（2026-10-24 · 第七十三会话）两条引擎件
//
// 判据（⛔ 不要重新论证）→ `资料/普查产出_1024b/`：
//   · ① 触发层 `PerCount` 增量型 → `C3_剩余判据_引擎战斗.md` §1·1：
//        原版是**增量**（一次结算、数值乘进去），**不是**跑 N 遍 ——
//        `AbilityLogic.multiplier` / `furtherMultiplier`（`AbilityLogic__PlayAbility.c:181-236` 的 `local_178`）；
//        伤害族 `:1461-1508` = **一个** `DamageInfo` 一次 `DealMultiDamage`（只有
//        `case 12 doDamageMultiRandom`（`:1511-1530`）才循环 N 次）；
//        增益族 `NewBuffStat.c:61-67 / :207-232` ⇒ `计数 × (change × multiplier)`（数据实证 `BL43`）。
//        改前：`EffectResolver` 只在**出牌路**折叠 `PerCount`，触发路把它**整条丢掉**
//        （`ResolveOne` 不读 `PerCount`）⇒ 触发正文里的 `give/gain/lose` 只剩基础值。
//   · ② 整条 `Parse(Desc)` 路给「替代行动块」贴 `cardAbilities.ta` → `E6_DiedLastTurn与desc路.md` §3·4：
//        替代行动（`Duty`/`Pray`/`Ferocity`/`Agenda`/`Oath`）的效果在
//        `activeAbility/alternativeAbility.activeAbilityLogic` 里，**不是** `cardAbilities` ⇒
//        不许贴它的 `ta`/`fm`（判据词表 = `CardDef.IsActiveActionKeyword`，唯一一份）。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 期望值一律写死字面量，⛔ 不从被测实现反算。
// ✅ 接线：`RuleEngineTest.Run()` 里加一行 `Step(TestEngineP2);`
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批**唯一入口**（`Run()` 里一行 `Step(TestEngineP2)`）。
    /// 子用例用 `E1P2…` 打头（⛔ 不用 `Test`/`Report` 打头 —— `RunCoverageCompiledCases()`
    /// 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestEngineP2()
    {
        Section("🆕 执行代理 `E1`：触发层 `PerCount` 增量 · 整条 desc 路的替代行动块不贴 `cardAbilities.ta`");
        E1P2T1TriggerPerCountIncrement();
        E1P2T2WholeDescAltBlockGate();
    }

    // ==============================================================================================
    //  ① 触发路 `PerCount` = **增量**（一次结算、数值乘进去），不是跑 N 遍
    // ==============================================================================================
    /// <summary>
    /// 判据 = `C3` §1·1（原版 `AbilityLogic.multiplier` → 数值；不是重复触发）。
    ///
    /// <para>**今天 0 张可达**（加法型全池 4 张 `BL43`/`BL47`/`GOF74`/`Alpha Warrior`，
    /// `abilityTriggers` 全是 `[0]` = 出牌路）⇒ 本用例用**合成正文**打触发重载
    /// （`ResolveOps(ctx, owner, source, IReadOnlyList, by, seed)`），
    /// 观测的是**同一条实现**，不是另一条路。</para>
    ///
    /// <para>**判别式（三档互斥）**：同一段正文、同一批 op，
    /// · 增量（正确）⇒ 每张牌 `基础 2 + 2×n`；
    /// · 增量被丢掉（改前）⇒ 每张只有 `+2`；
    /// · 被误当成「跑 N 遍」⇒ 每张 `+2` 加 n 遍 = `+2n`（**注意 n=2 时与增量的 6 不同**）。</para>
    /// </summary>
    static void E1P2T1TriggerPerCountIncrement()
    {
        var tac = Tactic("E1PerCount", 1,
            "Give +2 Melee Attack to your troops, and an additional +2 Melee Attack for each friendly troop");
        var ops = EffectText.Parse(tac.Desc, out var unparsed, out var partial);
        Check(unparsed.Count, 0, "① 增量型正文没有不认识的句子");
        Check(partial.Count, 0, "① 增量型正文没有半懂的句子");
        CheckTrue(ops != null && ops.Count == 1, "① （前提）解析出恰好 1 条 op");
        if (ops == null || ops.Count != 1) return;
        Check(ops[0].PerCount, 2, "① 增量 = 2（`and an additional +2 … for each`）");
        Check(ops[0].CountRef, "own|troop|all",
              "① 计数 = 己方**部队**（不含督军）—— 卡面写的是 `friendly troop`");
        Check(ops[0].Amount, 2, "① 基础值 = 2（从载荷里抠出来，`give` 的 `Amount` 不是 0）");

        // ---- ① 场上 1 个友方部队 ⇒ +2 + 2×1 = +4（2 → 6）----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var a = Place(ctx, 0, 3, Unit("E1A", 1, 2, 9));
            CheckTrue(a != null, "① （前提）友方部队上场");
            if (a == null) return;

            RuleCore.ResolveOps(ctx, 0, a, ops, "E1自检", a);
            Check(a.Attack, 6,
                  "★★ ① 触发路：1 个友方部队 ⇒ 基础 2 + 2×1 = **+4**（攻击 2 → 6）"
                + "｜🧨 触发路不读 `PerCount`（改前）⇒ 只剩基础 +2 ⇒ 4 ⇒ 红");

            // 同一批 op 再结算一次：**不得**一次比一次高（增量必须落在副本上）
            RuleCore.ResolveOps(ctx, 0, a, ops, "E1自检", a);
            Check(a.Attack, 10,
                  "★ ① 第二次结算同样 **+4**（2+4+4 = 10）"
                + "｜🧨 把增量写回 `op.Amount` 本身（不 `Clone`）⇒ 第二次按 +6 ⇒ 12 ⇒ 红");
            Check(ops[0].Amount, 2,
                  "★ ① 解析产物自己的 `Amount` 没被写脏（还是基础 2）—— 同一份 op 列表会被反复结算");
        }

        // ---- ② 场上 2 个友方部队 ⇒ +2 + 2×2 = +6（2 → 8）----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var a = Place(ctx, 0, 3, Unit("E1A", 1, 2, 9));
            var b = Place(ctx, 0, 5, Unit("E1B", 1, 2, 9));
            CheckTrue(a != null && b != null, "② （前提）两个友方部队上场");
            if (a == null || b == null) return;

            RuleCore.ResolveOps(ctx, 0, a, ops, "E1自检", a);
            Check(a.Attack, 8,
                  "★★ ② 计数 = 2 ⇒ 基础 2 + 2×2 = **+6**（攻击 2 → 8）"
                + "｜🧨 误当「跑 N 遍」（+2 加 2 遍 = +4）⇒ 6 ⇒ 红；丢掉增量 ⇒ 4 ⇒ 红");
            Check(b.Attack, 8,
                  "★ ② `to your troops` 是**全体**（`Count = 0`）⇒ 另一个友方部队同样 +6");
            Check(a.Attack + b.Attack, 16,
                  "★ ② 两张合计 **16**（8 + 8）—— 与「+4/+4 = 8」结构上不可能同时成立");
        }
    }

    // ==============================================================================================
    //  ② 整条 `desc` 的替代行动块**不许**贴 `cardAbilities` 的 `ta` / `fm`
    // ==============================================================================================
    /// <summary>
    /// 判据 = `E6` §3·4（那笔账）＋ `CardDef.IsActiveActionKeyword`（唯一一份词表）。
    ///
    /// <para>**三条合成探针**（不依赖真卡数据的巧合；与 `E6T2` 同形但**读的是现在的实现**）：</para>
    /// <list type="bullet">
    /// <item>`E1Duty`：`keywords=["Duty"]` ＋ **裸写正文** `Deal 3 damage` ⇒ 这张卡的整条
    ///   `Desc` 就是 `Duty` 的正文（`CardDef.CollectBareKeywordBody` 那条路）⇒
    ///   整条 desc 分支**一块 `ta` 都不贴**（`-1`），运行期那条由 `ApplyActiveAbilityLogic` 给 `30`。</item>
    /// <item>`E1Plain`：同样的正文/数据、**没有**替代行动关键词 ⇒ 照旧贴 `cardAbilities` 的 `25`
    ///   —— 这是**灭自证**：两道闸若被写成「整条 desc 一律不贴」，它会变 `-1` ⇒ 红。</item>
    /// <item>`E1Blocks`：`Deal 1 damage. Duty: Deal 3 damage` ＋ 两条 ability（`ta=[30]` / `[120]`）⇒
    ///   第 0 块（普通块）拿 `30`、第 1 块（`Duty:` 头）拿 `-1` —— 这是**按块**的判别式：
    ///   删掉块首闸 ⇒ 第 1 块变 `120`；把闸放宽成「整卡一律不贴」⇒ 第 0 块变 `-1`。</item>
    /// </list>
    /// </summary>
    static void E1P2T2WholeDescAltBlockGate()
    {
        var realPool = CardDatabase.Load();
        const string json =
            "{\"version\":24,\"count\":3,\"cards\":["
          + "{\"id\":\"E1Duty\",\"name\":\"E1 替代行动裸写正文探针\","
          + "\"type\":\"unit\",\"desc\":\"Deal 3 damage\",\"rarity\":\"Common\",\"faction\":\"Ultramarines\","
          + "\"cost\":1,\"attack\":1,\"health\":1,\"ranged\":0,\"subtype\":\"Infantry\","
          + "\"keywords\":[\"Duty\"],\"cardTraits\":[],"
          + "\"abilityTargets\":[{\"trigger\":662,\"ta\":[25]}],"
          + "\"abilityMultipliers\":[{\"trigger\":662,\"fm\":[0]}],"
          + "\"activeAbilityLogic\":[{\"ab\":-1,\"gate\":1,\"trigger\":400,\"needsLoading\":0,"
          + "\"ta\":[30],\"fm\":[0],\"ca\":[10],\"pa\":[0],"
          + "\"dmg\":[{\"min\":3,\"max\":3,\"opt\":0,\"type\":0}]}]},"
          + "{\"id\":\"E1Plain\",\"name\":\"E1 无替代行动对照探针\","
          + "\"type\":\"unit\",\"desc\":\"Deal 3 damage\",\"rarity\":\"Common\",\"faction\":\"Ultramarines\","
          + "\"cost\":1,\"attack\":1,\"health\":1,\"ranged\":0,\"subtype\":\"Infantry\","
          + "\"keywords\":[],\"cardTraits\":[],"
          + "\"abilityTargets\":[{\"trigger\":0,\"ta\":[25]}],"
          + "\"abilityMultipliers\":[{\"trigger\":0,\"fm\":[0]}]},"
          + "{\"id\":\"E1Blocks\",\"name\":\"E1 块首闸探针\","
          + "\"type\":\"unit\",\"desc\":\"Deal 1 damage. Duty: Deal 3 damage\",\"rarity\":\"Common\","
          + "\"faction\":\"Ultramarines\",\"cost\":1,\"attack\":1,\"health\":1,\"ranged\":0,"
          + "\"subtype\":\"Infantry\",\"keywords\":[],\"cardTraits\":[],"
          + "\"abilityTargets\":[{\"trigger\":0,\"ta\":[30]},{\"trigger\":662,\"ta\":[120]}],"
          + "\"abilityMultipliers\":[{\"trigger\":0,\"fm\":[0]},{\"trigger\":662,\"fm\":[0]}]}"
          + "]}";
        List<CardDef> probes = null;
        try
        {
            probes = CardDatabase.Parse(json);
            Check(probes != null ? probes.Count : -1, 3, "② （前提）三张合成探针都解析出来了");
        }
        finally
        {
            // 合成卡的名字进了全局名字索引 ⇒ 复原（否则后面的用例按名字找卡会撞上它们）
            CreatePool.BuildNameIndex(realPool);
        }
        if (probes == null || probes.Count != 3) return;
        CardDef duty = null, plain = null, blocks = null;
        foreach (var c in probes)
        {
            if (c == null) continue;
            if (c.Id == "E1Duty") duty = c;
            else if (c.Id == "E1Plain") plain = c;
            else if (c.Id == "E1Blocks") blocks = c;
        }
        CheckTrue(duty != null && plain != null && blocks != null, "② （前提）三张探针按 id 都拿到了");
        if (duty == null || plain == null || blocks == null) return;

        // ---- ① `Duty` 裸写正文 ⇒ 整条 desc 分支一块 `ta` 都不贴 ----
        {
            var fresh = EffectText.Parse(duty.Desc, out _, out _, card: duty);
            CheckTrue(fresh != null && fresh.Count == 1, "②① （前提）`E1Duty` 整条 desc 解析出 1 条 op");
            var fop = fresh != null && fresh.Count == 1 ? fresh[0] : null;
            CheckTrue(fop != null && fop.SubjectTargetsAffected == -1,
                  "★★ ②① `Duty` 的**裸写正文**不再贴 `cardAbilities` 的 `ta`（留 **-1** = 读不到）"
                + "｜🧨 撤掉整条 desc 分支那道替代行动闸 ⇒ 回到合成表里的 **25** ⇒ 红");

            // 运行期那一路仍由 `ApplyActiveAbilityLogic` 按 `chosenAbility` 挂上（来源正确）
            var resolved = duty.TriggerOps(KeywordTable.Duty);
            var rop = resolved != null && resolved.Count == 1 ? resolved[0] : null;
            CheckTrue(rop != null && rop.SubjectTargetsAffected == 30,
                  "★ ②① 运行期那条 op 的 `ta` = **30**（`activeAbilityLogic` —— 正确来源）"
                + "｜🧨 把 `ApplyActiveAbilityLogic` 的按种类归位删掉 ⇒ 变 -1 ⇒ 红");
            Check(duty.ActiveAbilityMap ?? "(null)", "attached",
                  "★ ②① （前提）`ActiveAbilityMap = attached`（按 `chosenAbility` 种类对上了）");
        }

        // ---- ② 灭自证：同样的正文、**没有**替代行动关键词 ⇒ 照旧贴 `25` ----
        {
            var fresh = EffectText.Parse(plain.Desc, out _, out _, card: plain);
            CheckTrue(fresh != null && fresh.Count == 1, "②② （前提）`E1Plain` 解析出 1 条 op");
            var fop = fresh != null && fresh.Count == 1 ? fresh[0] : null;
            CheckTrue(fop != null && fop.SubjectTargetsAffected == 25,
                  "★★ ②② **反例（灭自证）**：没有替代行动关键词的同一张表 ⇒ 照旧贴 `cardAbilities` 的 **25**"
                + "｜🧨 把闸写成「整条 desc 一律不贴」⇒ 变 -1 ⇒ 红（与 ②① 的 -1 结构上不可能同时由一种实现满足）");
        }

        // ---- ③ 按块：普通块照贴、`Duty:` 块不贴 ----
        {
            var fresh = EffectText.Parse(blocks.Desc, out _, out _, card: blocks);
            CheckTrue(fresh != null && fresh.Count == 2, "②③ （前提）`E1Blocks` 解析出 2 条 op（两块各一条）");
            if (fresh != null && fresh.Count == 2)
            {
                Check(fresh[0].SubjectTargetsAffected, 30,
                      "★★ ②③ 第 0 块（普通块 `Deal 1 damage`）照旧贴 `cardAbilities[0].ta = 30`"
                    + "｜🧨 把块首闸放宽成「整卡一律不贴」⇒ 变 -1 ⇒ 红");
                Check(fresh[1].SubjectTargetsAffected, -1,
                      "★★ ②③ 第 1 块（`Duty:` 头）**不贴** —— 留 **-1**"
                    + "｜🧨 删掉块首闸 ⇒ 按块下标硬配成 `cardAbilities[1].ta = 120` ⇒ 红");
            }
        }
    }
}
