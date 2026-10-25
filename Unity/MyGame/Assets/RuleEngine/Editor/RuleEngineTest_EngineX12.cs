// RuleEngineTest_EngineX12.cs — 引擎线 `X12`（2026-10-25 · 判据代理 → 执行代理 `X12`）
//
// 为什么单独一个文件：`TestSwarm()`（`RuleEngineTest.cs:197`）钉的是**旧口径**三条
// （合并 / 名字不同不合并 / 同名在左边不合并），**缺原版那第二个条件**——
// 「**右邻自己也要带 `swarm`**」（`CardScript__ResolveCardPlayed.c:70`）。本件只补这一格。
//
// 判据（⛔ 不要重新论证）→ `资料/待办判据_三条文案与代码差异_1025b.md` §四 ＋ §「4 swarm」。
// 🔴 本轮白名单只有 `Core/RuleCore.cs` + 本批测试文件。
//
// 🔴 **现池可达 0 张**：现池里「同名 + 都带 `swarm`」的卡必然同词条 ⇒ 这一格今天**零可观测差异**，
//    ⇒ 必须用**合成夹具**（本文件就是），⛔ 别拿现池真卡来断。
// 🔴 每条都带 `🧨`（什么改动能让它红）。期望值一律写死字面量，⛔ 不从被测实现反算。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（`Run()` 里一行 `Step(TestEngineX12)`；本批白名单不含 `RuleEngineTest.cs`，
    /// 登记由调度台合并）。</summary>
    public static void TestEngineX12()
    {
        Section("🆕 引擎线 X12：虫群合并的**右邻那一半判据**（右邻也必须带 `swarm`）");
        X12SwarmHostMustAlsoHaveSwarm();
    }

    /// <summary>
    /// 原版 `CardScript__ResolveCardPlayed.c` 的**两个**条件：`:70` 右邻 `HasCurrentTrait(swarm)`
    /// **且** `:74-76` 卡名 `String.Equals`。我们原来只有「打出的那张带 swarm」+ 卡名，
    /// **缺右邻那一半**（`RuleCore.IsSwarmHost`，2026-10-25 补）。
    ///
    /// <para>方向不变（本轮已证实 `slot+1` 就是原版的「右」）：打出的牌落在同名部队的**左**边、
    /// 合并进 `slot+1`。</para>
    /// </summary>
    static void X12SwarmHostMustAlsoHaveSwarm()
    {
        // 两件夹具：带 `swarm` 的 / 不带的（**除关键词外逐字相同**，只留关键词一个变量）。
        CardDef SwarmCard(string n)
        {
            return new CardDef(n, n, "unit", "", "common", "Test", 1, 2, 3, 0,
                               new[] { KeywordTable.Swarm }, subtype: "Infantry");
        }
        CardDef PlainCard(string n)
        {
            return new CardDef(n, n, "unit", "", "common", "Test", 1, 2, 3, 0,
                               null, subtype: "Infantry");
        }

        // ---- ① 对照（必须一直绿）：两边都带 `swarm` + 同名 ⇒ **合并** ----
        //   这一条**不区分**「新补的那一行在不在」（旧写法也会合并）——它的作用是钉住
        //   「补完那行之后**没有**把正常合并也一并挡掉」。
        {
            var host = SwarmCard("X12Guy");
            var ctx = ProbeBattle(new[] { SwarmCard("X12Guy") }, new[] { Unit("X12Foe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 3, host, exhausted: true);
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "X12Guy"), 2), RuleCodes.OK,
                      "① 把带 `swarm` 的同名部队打到**右邻也带 `swarm`** 的那张左边");
            Check(SlotOf(ctx, 0, "X12Guy"), 3, "① 合并后只剩**右边那一格**（3）有单位");
            CheckTrue(Board(ctx, 0, 2) == null, "① 左边那格（2）已经空了（新来的并进去了）");
            Check(Board(ctx, 0, 3).Attack, 4,
                  "① 攻击相加 2 + 2 = 4（合并真的发生了）"
                + "｜🧨 若 `IsSwarmHost` 把「两边都带 swarm」也挡掉 ⇒ 这里仍是 1 张 2 攻 ⇒ 红");
            Check(Board(ctx, 0, 3).SwarmUnder.Count, 1, "① 被并掉那张压在下面（`SwarmUnder` = 1）");
        }

        // ---- ② 新增那一行**唯一**能区分出来的形状：打出的带 `swarm`，**右邻不带** ⇒ **不合并** ----
        {
            var plainHost = PlainCard("X12Guy2");
            var ctx = ProbeBattle(new[] { SwarmCard("X12Guy2") }, new[] { Unit("X12Foe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 3, plainHost, exhausted: true);
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "X12Guy2"), 2), RuleCodes.OK,
                      "② 把带 `swarm` 的那张打到**右邻不带 `swarm`** 的同名部队左边");
            CheckTrue(Board(ctx, 0, 2) != null && Board(ctx, 0, 3) != null,
                      "★★ ② 右邻**不带 `swarm`** ⇒ **不合并**（2 / 3 两格都还有单位）"
                    + "｜🧨 把 `RuleCore.IsSwarmHost` 里 `!host.Has(Swarm)` 那一行删掉 ⇒ 这里会合并、"
                    + "只剩 3 号格一张 ⇒ **红**（这就是本条的全部价值：现池打不出来，只有夹具能红）");
            Check(Board(ctx, 0, 3).Attack, 2,
                      "② 右边那张**数值没被动过**（还是 2 攻 —— 合并没发生，不是「合并了但没删卡」）");
        }

        // ---- ③ 反方向（原有那一半，防「顺手把前提也写反」）：右邻带 `swarm`，**打出的不带** ⇒ 不合并 ----
        {
            var swarmHost = SwarmCard("X12Guy3");
            var ctx = ProbeBattle(new[] { PlainCard("X12Guy3") }, new[] { Unit("X12Foe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 3, swarmHost, exhausted: true);
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "X12Guy3"), 2), RuleCodes.OK,
                      "③ 把**不带 `swarm`** 的那张打到带 `swarm` 的同名部队左边");
            CheckTrue(Board(ctx, 0, 2) != null && Board(ctx, 0, 3) != null,
                      "③ 打出的那张**不带 `swarm`** ⇒ 不合并（原版 `ResolveCardPlayed.c:60-62` 那一半；"
                    + "`TrySwarmMerge` 的早返 `!just.Has(Swarm)` 管这一格）");
        }
    }
}
