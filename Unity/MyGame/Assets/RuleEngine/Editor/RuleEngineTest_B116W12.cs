// RuleEngineTest_B116W12.cs — 执行写手 `B116_W12`（2026-11-02 · 第一百一十六会话）
//
// ============================ 这件活是什么 ============================
// 活状态表 `B-314`：**`TL15 Tyranid Prime` 的 Swarm 监听 buff 落错了对象**。
//   卡面 = `Synapse. When a friendly unit triggers Swarm, give it +1 [Melee] and +1 Health`
//   卡表 = `abilityTargets[{trigger:620, ta:[30]}]` —— `ta = 30 (target)` = 「调用方传进来的那个目标」
//          = 原版 `CardScript.TriggeredSwarm(observer, param_2, param_3)` 的 **`param_3` = 留下的 host**
//          （`BattleManagerSupport__BroadcastUnitSwarm.c:44` → `OnTrigger(0x26c, …, actingCard = param_2, targetCard = param_3)`）。
//   ⇒ 原版把「被并掉那张」当 `actingCard`、把**留下的 host** 当 `targetCard`，而 `ta=30` 只读 `targetCard`
//     （`AbilityLogic__GetTargets.c:192-207`；整张函数只有 `ta ∈ {30, 220}` 读 `param_5`）。
//
//   ⚠️ 本文件的断言**今天（补丁未落）是红的** —— 那就是缺陷本体。补丁 = `报告/B116_W12_B314_TL15归属.md` §三
//     的**一行**（`EffectResolver.FireBoardListenerDeferred` 里 seed 从 `subject` 换成 host）。
//
// ============================ 判据（⛔ 不是我挑的）============================
//   · `资料/普查产出_1025/E6_十三行接闸.md:153-156`（逐句）；
//   · `CardScript.<>c__DisplayClass401_0.<ExecuteSwarm>b__0.c:45/51/54-55` ⇒ `+0x10` = 被并掉那张、`+0x18` = 留下那张；
//   · `BattleManagerSupport__BroadcastUnitSwarm.c:44` ⇒ `TriggeredSwarm(卡, param_2, param_3)`；
//   · `AbilityLogic__GetTargets.c:192-207` ⇒ `ta=30` 的结果表 = `{targetCard}`（传 null 就是空表、**不拿自己兜底**）。
//
// ============================ 实测读数（离线探针 `_tmp_view/B116W12_probe`，非 Unity）============================
//   夹具：host（`swarm` 2/3）摆 3 号格 + 监听者摆 6 号格 + 把同名 `swarm`（2/3）打到 2 号格 ⇒ 合并。
//     · 改前：host = **4/6**（只有合并那 2/2）· TL15 = 5/5（一点没加 —— buff 落在**已离场**的被并掉那张身上）
//     · 改后：host = **5/7** · TL15 = 5/5
//     · `TL75 Termagant Brood`（`gain +1 [Ranged]`，`ta=20` self）改前/改后**都是 5 → 6**（这一行是**不回归**的对照）
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>`B116_W12` 的唯一入口（`Run()` 里一行 `Guard(...)`）。</summary>
    public static void CheckB116W12SwarmListenerTarget()
    {
        Section("B116·W12 · `B-314`：Swarm 监听（620）的 `it` = **留下的 host**（`ta=30` ← `targetCard`）");

        var pool = CardDatabase.Load();
        var prime = CreatePool.FindByName(pool, "Tyranid Prime");
        var brood = CreatePool.FindByName(pool, "Termagant Brood");
        CheckTrue(prime != null, "（前提）池里有 `Tyranid Prime`（TL15）");
        CheckTrue(brood != null, "（前提）池里有 `Termagant Brood`（TL75）");
        if (prime == null || brood == null) return;   // 前提已红 ⇒ 后面两条断言无从谈起。⛔ 这不是「静默跳过」：失败已经记在上面两条里

        CardDef SwarmCard(string n)
        {
            return new CardDef(n, n, "unit", "", "common", "Test", 1, 2, 3, 0,
                               new[] { KeywordTable.Swarm }, subtype: "Infantry");
        }

        // ============================================================
        //  ① 主断言（`B-314` 本体）：TL15 听 Swarm ⇒ buff 落在**留下的 host**
        // ============================================================
        {
            var ctx = ProbeBattle(new[] { SwarmCard("B116W12Guy") }, new[] { Unit("B116W12Foe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 3, SwarmCard("B116W12Guy"), exhausted: true);   // = 留下的 host（右邻）
            var lis = Place(ctx, 0, 6, prime, exhausted: true);           // = TL15 监听者
            int lisAtk0 = lis.Attack, lisHp0 = lis.Health;                // 监听者的初始值（改前/改后都该不动）

            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "B116W12Guy"), 2), RuleCodes.OK,
                      "① 把同名的 `swarm` 部队打到 host 左边 ⇒ 走虫群合并（`RuleCore.TrySwarmMerge`）");

            int hs = SlotOf(ctx, 0, "B116W12Guy");
            CheckTrue(hs >= 0, "（前提）合并后 `B116W12Guy` 还在场上（留在右邻那一格）");
            var host = hs >= 0 ? Board(ctx, 0, hs) : null;
            CheckTrue(host != null, "（前提）拿得到 host 的 `UnitState`");
            if (host != null)
            {
                Check(host.SwarmUnder.Count, 1,
                      "（前提）合并真的发生了（`SwarmUnder` = 1 —— 没有它，「+1」根本无从谈起）");

                // 数值推导（全部来自夹具字面量，⛔ 不从被测实现反算）：
                //   host 2/3 + 被并掉那张 2/3 = 4/6；再吃 TL15 的 `+1 [Melee] and +1 Health` ⇒ 5/7。
                Check(host.Attack, 5,
                      "★★ ①-a 留下的 host 拿到 TL15 的 `+1 [Melee]`（2+2 合并 +1 = **5**）"
                    + "｜🧨 改前 `FireBoardListenerDeferred` 把 `subject`（= **被并掉那张**）当 `it` ⇒ host 停在 **4** ⇒ 红"
                    + "（离线实测：改前 4/6 · 改后 5/7）");
                Check(host.Health, 7,
                      "★★ ①-b 同一跳的 `+1 Health`（3+3 合并 +1 = **7**）"
                    + "｜🧨 改前 **6** ⇒ 红");
            }

            // 灭自证的另一半：**监听者自己**不许被加成。
            //   与 ①-a/①-b 结构上不可能同时被同一种错实现满足：
            //     · seed 还是 `subject` ⇒ ①-a/①-b 红；
            //     · seed 改成 `u`（监听者自己）⇒ 这两条红；
            //     · 只有 seed = `target`（留下的 host）才两条一起绿。
            Check(lis.Attack, lisAtk0,
                  "①-c 监听者 TL15 **自己**没被加成（`it` 指的不是监听者）"
                + "｜🧨 把 seed 改成 `u` ⇒ 这里 +1 ⇒ 红");
            Check(lis.Health, lisHp0, "①-d 同上，生命没变");
        }

        // ============================================================
        //  ② 不回归对照：`TL75 Termagant Brood`（同一 trigger 620、`ta=20 self`）必须照旧
        //     这一行改前/改后**都该绿** —— 它证明「①」的修法不是把所有 620 监听一律改到 host 上。
        // ============================================================
        {
            var ctx = ProbeBattle(new[] { SwarmCard("B116W12Guy2") }, new[] { Unit("B116W12Foe2", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 3, SwarmCard("B116W12Guy2"), exhausted: true);
            var bl = Place(ctx, 0, 6, brood, exhausted: true);
            int r0 = bl.RangedAttack;

            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "B116W12Guy2"), 2), RuleCodes.OK,
                      "② 同样的虫群合并（监听者换成 TL75）");
            Check(bl.RangedAttack, r0 + 1,
                  "② TL75 的 `gain +1 [Ranged]` 加在**它自己**身上（+1）—— 改前/改后都绿"
                + "｜🧨 若把 `chosen: host` 也一起塞给 `ResolveOps`，`ta` 缺失时那条兜底会把整张目标表换成 host ⇒ 这里 0 ⇒ 红");

            int hs2 = SlotOf(ctx, 0, "B116W12Guy2");
            var host2 = hs2 >= 0 ? Board(ctx, 0, hs2) : null;
            CheckTrue(host2 != null, "（前提）② 的 host 还在场上");
            if (host2 != null)
                Check(host2.Attack, 4,
                      "② host 只拿到合并那 2 点（4 = 2+2）—— TL75 的 buff **不**落在它身上"
                    + "｜🧨 把 620 的 seed 一律改成 host 而不看 `ta` ⇒ 这里 5 ⇒ 红");
        }
    }
}
