// RuleEngineTest_W12.cs — 引擎线 `W12`（2026-10-24）：`ThisUnitSummoned(195)` 的「被效果召唤」途径
// ＋ 再造（`DoReanimate`）两条退路**不发** `When deployed`。
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册 `_G2` / `_EngineB3` /
// `_EngineE6` … 都在），而同一时刻只有一个写手改一个文件 ⇒ 本批测试体全写在这里，
// `RuleEngineTest.cs` **只加 `Run()` 里一行 `Step(TestW12SummonedAndReanimate)`**。
//
// 判据（第一权威 = 反编译，**已闭合、⛔ 不要重新论证**）：
//   ① `CardScript__ResolveUnitSummoned.c:40-53` —— 195 发给**被召唤的那张卡自己**
//      （`op_Equality(param_2, param_1)` ∧ `cardType == 0 Minion`），别的单位发 190
//      （`op_Inequality` 那一支）。C2 只核过「打出 / 免费部署」两条入口；
//      本文件把**第三条入口「被效果召唤（法术造兵）」**钉住。
//   ② `BattleManager.__c__DisplayClass555_0___ResolveTransformFromRemnant_b__0.c:7-31` ——
//      原版再造链的回调清单：`ResetToValuesInHand` → **`BroadcastUnitReanimated`** →
//      `SetAsUnitInPlay` → `ResetSummonSickness` → `ActivateTraitsOnSummonOrEnchantment` →
//      `UpdateWhileInPlay` → `CheckStun` → `UpdateFigures` → **`BroadcastWhileInPlay`** → 手牌刷新。
//      **整条链没有 `ResolveUnitSummoned` / `BroadcastUnitSummoned`** ⇒ 190 / 195 / 605 一条都不该发。
//      （补充：原版**没有「墓地翻回来」这条路** —— 唯一入口是 `BattleManager__AddTransformFromRemnant`；
//       我们那条墓地退路是自己的旧口径，但语义是再造 ⇒ 输出面照上表。）
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 期望值一律写死字面量（1 / 0 / 卡模板的原始 `Health`），⛔ 不从实现常量反算（那会自证）。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批的**唯一入口**（`Run()` 里一行 `Step(TestW12SummonedAndReanimate)`）。
    /// 两个子用例是 `W12…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestW12SummonedAndReanimate()
    {
        Section("🆕 引擎线 `W12`：被效果召唤 ⇒ 195（第三条入口）· 再造两条退路 ⇒ 不发 190/195");
        W12EffectSummonEmitsThisUnitSummoned();
        W12ReanimateSkipsDeployBroadcast();
        // 🆕 2026-10-28（执行代理 `B93_E17`）：`cardState` 三档（3 / 15 / 17）的窗口断言。
        //   写点 = `RuleCore.DeclareAttackCore`(:6604) / `RuleCore.UseAbilityCore`(:11664) /
        //   `RuleCore.CleanupDeaths` 变残骸支(:9858) / `EffectResolver.DoReanimate` 残骸退路(:15543)。
        W12CardStateWindows();
    }

    // ==============================================================================================
    //  ① 被效果召唤（法术造兵）⇒ 被召唤的那张卡自己发 `ThisUnitSummoned`(195)
    // ==============================================================================================

    /// <summary>
    /// **「被效果召唤」也走 `DeployFree` ⇒ 也走 195 那一支。**
    ///
    /// 现状核对（2026-10-24 现读，按符号不按行号）：`EffectResolver` 里所有造兵入口
    /// （`DoDeployOnce` 的 `DeployFree` · `DoChooseCard` 的 `"deploy"` 支 · `DoReanimate` 的墓地退路）
    /// **全部收敛到 `RuleCore.DeployFree`** —— 没有第二条直接写 `Board[…]` 的造兵路
    /// （`DoCreate` 只造进手牌/牌库；`DoTakeControl` / 回合末归还是**移动已存在单位**，不是召唤）。
    /// 而 `DeployFreeCore` 的 `PreCallBoardListener(…, selfOnlyOnly: true)` 就在那条路上
    /// ⇒ 「被法术召唤」与「从手牌打出 / 免费部署」**同一份判据**，不需要另补代码。
    ///
    /// 夹具（**真卡**，因为 `EvtKind.Trigger` 的闸要看 `abilityTriggers` 真值）：
    ///   · 被召唤的 = `SW10 Grey Hunter`（`ThisUnitSummoned` 195 · 卡面 `When deployed, …`）；
    ///   · 旁观监听 = `TAU34 Coldstar Battlesuit`（`OtherUnitSummoned` 190 · 卡面
    ///     `When you deploy a troop, give it +2 Health`）。
    /// 入口 = 夹具战术 `Deploy a Grey Hunter`（走 `EffectText` 的 `deploy` 动词 → `DoDeployOnce`）。
    ///
    /// **灭自证**：
    ///   · 把预调那一路的 `selfOnlyOnly` 去掉 / 换成整份 `FireWhen` ⇒ 被召唤者会吃 190
    ///     （`CountTriggerSignalsA30(…, "OtherUnitSummoned", grey.Name)` 由 0 变 1，本文件红）；
    ///   · 把 `DeployFree` 的 `PreCallBoardListener` 整句删掉 ⇒ 195 由 1 变 0（`SW10` 那一格红）。
    /// </summary>
    static void W12EffectSummonEmitsThisUnitSummoned()
    {
        var pool = CardDatabase.Load();
        var grey = CardDatabase.FindById(pool, "SW10");      // 195 · `When deployed, …`
        var cold = CardDatabase.FindById(pool, "TAU34");     // 190 · `When you deploy a troop, …`
        CheckTrue(pool != null && pool.Count > 0, "🧪（前提）卡池载到了");
        CheckTrue(grey != null && cold != null, "🧪（前提）卡池里有 `SW10` / `TAU34`");
        if (grey == null || cold == null) return;
        Check(grey.AbilityTriggerPresence("ThisUnitSummoned"), 1,
              "🧪（前提）`SW10` 的 `abilityTriggers` 里有 `ThisUnitSummoned`(195) —— 没有的话下面「195 == 1」是假绿");
        Check(cold.AbilityTriggerPresence("OtherUnitSummoned"), 1,
              "🧪（前提）`TAU34` 的 `abilityTriggers` 里有 `OtherUnitSummoned`(190) —— 对照那一半才有意义");

        var tac = Tactic("W12SummonTactic", 0, "Deploy a Grey Hunter");
        // 夹具自检：这句卡面真的解析成 `deploy` 动词（解析不出来 ⇒ 下面量到的是空局面）
        var ops = EffectText.Parse(tac.Desc, out _, out _);
        bool hasDeploy = false;
        foreach (var o in ops) if (o.Verb == "deploy") { hasDeploy = true; break; }
        CheckTrue(hasDeploy, "🧪（前提）夹具战术 `Deploy a Grey Hunter` 解析成 `deploy` 动词");

        // ⚠️ **阵营 = `SpaceWolves`**：`DoDeployOnce` 的「从全卡池造」那一档按**施放者阵营**筛
        //    （`CreatePool.Resolve(…, faction, …)`）—— 阵营给错 ⇒ 候选空 ⇒ 一个都召唤不出来。
        var ctx = BattlePool(new[] { tac }, new[] { Unit("W12Foe", 1, 0, 9) }, pool,
                             warlordFaction: "SpaceWolves");
        ToP1Turn(ctx, 1);
        var watcher = Place(ctx, 0, 2, cold, exhausted: true);   // 190 的旁观监听者
        ctx.ClearSignals();

        CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "W12SummonTactic"), -1), RuleCodes.OK,
                  "打出 `Deploy a Grey Hunter`（**法术造兵**那条入口）");

        UnitState got = null;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = Board(ctx, 0, s);
            if (u != null && ReferenceEquals(u.Card, grey)) { got = u; break; }
        }
        CheckTrue(got != null,
                  "🧪（前提）`Grey Hunter` 真的被**效果召唤**上场了 —— 没上场下面全是假绿" + LogTail(ctx));
        if (got == null) return;

        Check(CountTriggerSignalsA30(ctx, "ThisUnitSummoned", grey.Name), 1,
              "★★ **被效果召唤的那张卡自己恰 1 条 `ThisUnitSummoned`(195)**"
            + "｜判据 `CardScript__ResolveUnitSummoned.c:40-53` 的 `op_Equality(param_2, param_1)` 那一支；"
            + "🧨 删掉 `DeployFreeCore` 的 `PreCallBoardListener` ⇒ 0；"
            + "把 `TriggerNameOfWhenKind` 的 `Deploy` 支改回恒 190 ⇒ 0");
        Check(CountTriggerSignalsA30(ctx, "ThisUnitSummoned"), 1,
              "★★ 全场**只有它这一条 195**（旁观者不该拿到自指那一支）");
        Check(CountTriggerSignalsA30(ctx, "OtherUnitSummoned", cold.Name), 1,
              "★★ 旁观监听者（`TAU34` · `When you deploy a troop`）恰 1 条 190"
            + "｜判据 `BroadcastUnitSummoned.c:28-39` 的棋盘循环；🧨 循环不排除主体/整句不广播 ⇒ 0 或 2");
        Check(CountTriggerSignalsA30(ctx, "OtherUnitSummoned", grey.Name), 0,
              "★★ 被召唤者**没有** 190 —— 两支不许互相顶替"
            + "｜🧨 预调改成整份 `FireWhen`（`selfOnlyOnly:false`）⇒ 本行 1");
        Check(got.Health, grey.Health + 2,
              "★★ `TAU34` 的 190 监听器**真的落了地**（`give it +2 Health` 挂在被召唤者身上）"
            + "｜🧨 不广播 ⇒ 停在 " + grey.Health + "；把 `it` 指错人 ⇒ 旁观者自己 +2");
        CheckTrue(watcher != null && watcher.Health == cold.Health,
                  "★ 对照：那条 `+2 Health` **没有**洒给旁观者自己（`it` = 被召唤者）");
    }

    // ==============================================================================================
    //  ② 再造（残骸 / 墓地两条退路）⇒ 不发 190 / 195，但 `Reanimated` / `WhileInPlay` 照发
    // ==============================================================================================

    /// <summary>
    /// **`DoReanimate` 两条退路的输出面 = 原版 `ResolveTransformFromRemnant` 的回调清单。**
    ///
    /// 判据（逐行读过）：`…DisplayClass555_0___ResolveTransformFromRemnant_b__0.c:7-31` ——
    ///   `BroadcastUnitReanimated` 有 · `BroadcastWhileInPlay` 有 ·
    ///   **`ResolveUnitSummoned` / `BroadcastUnitSummoned` 没有**
    ///   ⇒ 195（`When deployed`）· 190（`When you deploy a troop`）· 605（`UseFaith`）**一条都不该发**。
    /// 我们改前：**墓地退路**借 `DeployFree` ⇒ 上面那一整组全发（静默多触发）；残骸退路本来就不发。
    /// 现在 `DeployFreeCore` 按 `DeploySource.Reanimate` 整组跳过（判据与理由 → 那边 `reanimated` 段的长注释）。
    ///
    /// 四个子用例（都跑**真卡**，因为 `EvtKind.Trigger` 的闸看 `abilityTriggers` 真值）：
    ///   · (a) **残骸退路（残骸是「真死」出来的）** · **照发** `Reanimated`(570) 与 `WhileInPlay`(20)
    ///         （`SAU3` 带 `remnant` ⇒ 被打死当场翻面成残骸 ⇒ 与 (c) **同一条路**）；
    ///   · (b) 墓地退路 · **不发** 195 / 190（`SW10` 先死再翻回来，`TAU34` 当 190 旁观监听）；
    ///   · (c) 残骸退路（残骸是**手工摆的**）· **照发** `Reanimated`(570) 与 `WhileInPlay`(20)（`SAU3` 当残骸）；
    ///   · (d) 残骸退路 · **不发** 195 / 190（`SW10` 当残骸）。
    /// ⚠️ **(a) 与 (c) 走同一条路**（`D1` 第 8 条：`SAU3` 带 `remnant`，永远不进 `DeadUnits`）；
    ///    「**墓地**退路 + 570」这一档**用真卡结构上做不出**（池内 3 张带 `When Reanimated` 的全带 `Remnant`），
    ///    目前**无人覆盖**（(b) 走真墓地退路，但只看 195/190）。⛔ 别为此手搓一张原版没有的卡。
    ///
    /// **灭自证**：把 `DeployFreeCore` 里 `if (!reanimated)` 那道闸去掉 ⇒ (b) 的 195 / 190 各变 1，
    /// 而且 `Grey Hunter` 的 `Health` 会多 2（`TAU34` 的 190 效果落地）—— 三条同时红。
    /// 反过来，把 `BroadcastWhen(Reanimated)` / `EmitWhileInPlayPass` 一起关掉 ⇒ (a)/(c) 红。
    /// </summary>
    static void W12ReanimateSkipsDeployBroadcast()
    {
        var pool = CardDatabase.Load();
        var grey = CardDatabase.FindById(pool, "SW10");      // 195 的载体（再造时**不该**发）
        var cold = CardDatabase.FindById(pool, "TAU34");     // 190 的旁观监听者
        var sau3 = CardDatabase.FindById(pool, "SAU3");      // `When Reanimated, gain Fast`（570）
        var sw18 = CardDatabase.FindById(pool, "SW18");      // `WhileInPlay`(20) 的表现口
        CheckTrue(grey != null && cold != null && sau3 != null && sw18 != null,
                  "🧪（前提）卡池里有 `SW10` / `TAU34` / `SAU3` / `SW18`");
        if (grey == null || cold == null || sau3 == null || sw18 == null) return;
        Check(sau3.AbilityTriggerPresence("Reanimated"), 1,
              "🧪（前提）`SAU3` 的 `abilityTriggers` 里有 `Reanimated`(570)");
        CheckTrue(sw18.HasAnimTrigger("WhileInPlay"),
                  "🧪（前提）`SW18` 确有 `WhileInPlay` 的动画行（否则 20 那一拍恒 0，量不出「照发」）");

        var kill = Tactic("W12Kill", 0, "Deal 99 damage to a friendly unit");

        // ---- (a) 残骸退路（残骸 = 「真死」出来的）· `Reanimated` / `WhileInPlay` 照发 ----
        {
            var raise = Tactic("W12RaiseA", 0, "Reanimate a friendly Remnant");
            var ctx = BattlePool(new[] { kill, raise }, new[] { Unit("W12Foe2", 1, 0, 9) }, pool,
                                 warlordFaction: "Sautekh");
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 2, cold, exhausted: true);                 // 190 的旁观监听者（不该响）
            Place(ctx, 0, 3, sw18, exhausted: true);                 // WhileInPlay(20) 的载体
            Place(ctx, 0, 5, sau3, exhausted: true);
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "W12Kill"), 5), RuleCodes.OK,
                      "（a）先把 `Flayed One` 打死（它带 `remnant` ⇒ **不进场墓地**）");
            // 🔴🆕 2026-10-24（执行代理 `W25` · `D1` 的第 8 条 · 性质 **δ = 夹具前提不成立**）：
            //   原来这里断「墓地表里记着它」，但 `SAU3 Flayed One` 的 `kw = [destroyer, remnant]`
            //   ⇒ 死亡支走 `RuleCore.cs` 的 `(Remnant || Waystone) && !IsRemnant` 那一跳：
            //   **当场翻面成残骸、留在格位上、`leftPlay = false`** ⇒ **永远不进 `DeadUnits`**。
            //   ⇒ 前提改成事实本身（残骸留在 5 号格）。**这不是把红抹平** —— 下面那句
            //   「它真的翻回场上了 / `Reanimated` 照发」照样在验行为。
            //   ⚠️ 因此 **(a) 与 (c) 走的是同一条路（残骸退路）**；(a) 的额外价值只剩「残骸是
            //   **真死出来的**、不是手工 `IsRemnant = true` 摆的」。
            //   ⛔ 想验「**墓地**退路 + 570」：池内带 `When Reanimated` 的 3 张
            //   （`SAU3` / `SAU4` / `SAU22`）**全部带 `Remnant`** ⇒ 用真卡**结构上做不出**
            //   （打死必翻残骸）。⛔ 别为此手搓一张原版没有的卡（手搓卡没有 `abilityTriggers`，
            //   `EvtKind.Trigger` 会回退「有没有动画行」而**静默不发** ⇒ 断言因另一个原因假红）。
            CheckTrue(ctx.Players[0].Board[5] is { IsRemnant: true },
                      "🧪（a 前提）它翻面成**残骸**、留在 5 号格（⇒ (a) 与 (c) 同一条路：残骸退路；"
                    + "「墓地退路 + 570」用真卡结构上做不出 —— 池内 3 张带 `When Reanimated` 的全部带 `Remnant`）");
            ctx.ClearSignals();

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "W12RaiseA"), -1), RuleCodes.OK,
                      "（a）打出 `Reanimate a friendly Remnant`（场上那具残骸 ⇒ 走**残骸退路**，与 (c) 同一条）");

            UnitState back = null;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = Board(ctx, 0, s);
                if (u != null && ReferenceEquals(u.Card, sau3)) { back = u; break; }
            }
            CheckTrue(back != null, "🧪（a 前提）它**真的从残骸翻回场上了** —— 没上场下面全是假绿" + LogTail(ctx));
            if (back == null) return;

            CheckTrue(back.Has("fast"),
                      "★★（a）**`BroadcastUnitReanimated` 照发** —— `When Reanimated, gain Fast` 落到它身上"
                    + "｜判据 = 原版回调 `:9-10`；🧨 把 `DoReanimate` 的 `BroadcastWhen(Reanimated)` 关掉 ⇒ 本行红");
            Check(CountTriggerSignalsA30(ctx, "Reanimated", sau3.Name), 1,
                  "★★（a）`Reanimated`(570) 恰 1 条｜🧨 同上 ⇒ 0");
            Check(CountTriggerSignalsA30(ctx, "WhileInPlay", sw18.Name), 1,
                  "★★（a）`WhileInPlay`(20) **照发**（一次顶层动作恰 1 条）"
                + "｜判据 = 原版回调 `:18/:23`（`UpdateWhileInPlay` ＋ `BroadcastWhileInPlay`）；"
                + "🧨 把再造那条路的顶层动作一起掐掉（`ActionScope` 不 Dispose）⇒ 0");
            Check(CountTriggerSignalsA30(ctx, "OtherUnitSummoned"), 0,
                  "★★（a）残骸退路**不发 190**（与 (c) 同一条路）｜🧨 给这条退路补 `BroadcastWhen(Deploy)` ⇒ `TAU34` 那一条 1");
        }

        // ---- (b) 墓地退路 · 不发 195 / 190 ----
        {
            var raise = Tactic("W12RaiseB", 0, "Reanimate a friendly Remnant");
            var ctx = BattlePool(new[] { kill, raise }, new[] { Unit("W12Foe3", 1, 0, 9) }, pool,
                                 warlordFaction: "SpaceWolves");
            ToP1Turn(ctx, 1);
            var watcher = Place(ctx, 0, 2, cold, exhausted: true);   // 190 的旁观监听者
            Place(ctx, 0, 5, grey, exhausted: true);
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "W12Kill"), 5), RuleCodes.OK,
                      "（b）先把 `Grey Hunter` 打死（进墓地）");
            CheckTrue(ctx.DeadUnits.Exists(d => d != null && ReferenceEquals(d.Card, grey)),
                      "🧪（b 前提）墓地表里记着它");
            ctx.ClearSignals();

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "W12RaiseB"), -1), RuleCodes.OK,
                      "（b）打出 `Reanimate a friendly Remnant`（走**墓地退路**）");

            UnitState back = null;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = Board(ctx, 0, s);
                if (u != null && ReferenceEquals(u.Card, grey)) { back = u; break; }
            }
            CheckTrue(back != null, "🧪（b 前提）它真的从墓地翻回来了" + LogTail(ctx));
            if (back == null) return;

            // `EvtKind.Deploy`（表现层落地两条 cue）**照发**、来源仍是 `Reanimate`
            bool deploySignal = false;
            foreach (var e in ctx.Signals)
                if (e != null && e.Kind == EvtKind.Deploy
                    && e.CardId == grey.Name && e.DeployFrom == DeploySource.Reanimate)
                    deploySignal = true;
            CheckTrue(deploySignal,
                      "★（b）`EvtKind.Deploy`（来源 `Reanimate`）**照发** —— 表现层落地 cue 靠它"
                    + "｜🧨 把 `Emit(Deploy)` 一起关掉 ⇒ 本行红（那是另一条账，别顺手关）");

            Check(CountTriggerSignalsA30(ctx, "ThisUnitSummoned"), 0,
                  "★★（b）**再造不发 195**（`When deployed` 那一支）"
                + "｜判据 `…DisplayClass555_0___ResolveTransformFromRemnant_b__0.c:7-31` 的清单里没有 Summoned 广播；"
                + "🧨 去掉 `DeployFreeCore` 的 `if (!reanimated)` 闸 ⇒ 1");
            Check(CountTriggerSignalsA30(ctx, "OtherUnitSummoned"), 0,
                  "★★（b）**再造不发 190**（`When you deploy a troop` 那一支）"
                + "｜🧨 同上 ⇒ `TAU34` 那一条 1");
            Check(back.Health, grey.Health,
                  "★★（b）`TAU34` 的 `+2 Health` **没落**（190 是真没发，不是「信号没发、效果照跑」）"
                + "｜🧨 去掉闸 ⇒ 实得 " + (grey.Health + 2));
            CheckTrue(watcher != null && watcher.Health == cold.Health,
                      "★（b）对照：旁观者自己也没吃到任何东西");
        }

        // ---- (c) 残骸退路 · `Reanimated` / `WhileInPlay` 照发 ----
        {
            var raise = Tactic("W12RaiseC", 0, "Reanimate a friendly Remnant");
            var ctx = BattlePool(new[] { raise }, new[] { Unit("W12Foe4", 1, 0, 9) }, pool,
                                 warlordFaction: "Sautekh");
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 2, cold, exhausted: true);                 // 190 的旁观监听者（不该响）
            Place(ctx, 0, 3, sw18, exhausted: true);                 // WhileInPlay(20) 的载体
            var rem = Place(ctx, 0, 5, sau3, exhausted: true);
            rem.IsRemnant = true;                                    // 一具残骸（攻 0 / 1 血，按定义）
            ctx.ClearSignals();

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "W12RaiseC"), -1), RuleCodes.OK,
                      "（c）打出 `Reanimate a friendly Remnant`（场上有一具残骸 ⇒ 走**残骸退路**）");

            UnitState alive = Board(ctx, 0, 5);
            CheckTrue(alive != null && ReferenceEquals(alive.Card, sau3) && !alive.IsRemnant,
                      "🧪（c 前提）残骸**翻回成活的单位**了" + LogTail(ctx));
            CheckTrue(alive != null && alive.Has("fast"),
                      "★★（c）**`BroadcastUnitReanimated` 照发** —— `When Reanimated, gain Fast` 落到它身上"
                    + "｜🧨 把 `DoReanimate` 的 `BroadcastWhen(Reanimated)` 一起关掉 ⇒ 本行红");
            Check(CountTriggerSignalsA30(ctx, "Reanimated", sau3.Name), 1,
                  "★★（c）`Reanimated`(570) 恰 1 条｜🧨 同上 ⇒ 0");
            Check(CountTriggerSignalsA30(ctx, "WhileInPlay", sw18.Name), 1,
                  "★★（c）`WhileInPlay`(20) **照发**（一次顶层动作恰 1 条）"
                + "｜判据 = 原版回调 `:18/:23`；🧨 掐掉顶层动作的 `EmitWhileInPlayPass` ⇒ 0");
            Check(CountTriggerSignalsA30(ctx, "OtherUnitSummoned"), 0,
                  "★★（c）残骸退路**不发 190**｜🧨 给残骸那条路补 `BroadcastWhen(Deploy)` ⇒ `TAU34` 那一条 1");
        }

        // ---- (d) 残骸退路（`SW10` 当残骸）⇒ 不发 195 / 190 ----
        {
            var raise = Tactic("W12RaiseD", 0, "Reanimate a friendly Remnant");
            var ctx = BattlePool(new[] { raise }, new[] { Unit("W12Foe5", 1, 0, 9) }, pool,
                                 warlordFaction: "SpaceWolves");
            ToP1Turn(ctx, 1);
            var watcher = Place(ctx, 0, 2, cold, exhausted: true);
            var rem = Place(ctx, 0, 5, grey, exhausted: true);
            rem.IsRemnant = true;
            ctx.ClearSignals();

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "W12RaiseD"), -1), RuleCodes.OK,
                      "（d）把 `Grey Hunter` 从残骸翻回来");
            UnitState alive = Board(ctx, 0, 5);
            CheckTrue(alive != null && ReferenceEquals(alive.Card, grey) && !alive.IsRemnant,
                      "🧪（d 前提）它真的翻回来了" + LogTail(ctx));
            Check(CountTriggerSignalsA30(ctx, "ThisUnitSummoned"), 0,
                  "★★（d）残骸翻回来 ⇏ `When deployed`（195 == 0）"
                + "｜🧨 给残骸那条路补 `PreCallBoardListener` ⇒ 1");
            Check(CountTriggerSignalsA30(ctx, "OtherUnitSummoned"), 0,
                  "★★（d）……也不发 190｜🧨 同上 ⇒ `TAU34` 那一条 1");
            CheckTrue(alive != null && alive.Health == grey.Health,
                      "★（d）`TAU34` 的 `+2 Health` 没落（190 真的没发）");
            CheckTrue(watcher != null && watcher.Health == cold.Health,
                      "★（d）对照：旁观者自己也没吃到任何东西");
        }
    }

    // ==============================================================================================
    //  🆕 2026-10-28（执行代理 `B93_E17`）：`cardState` 三档（3 / 15 / 17）的**窗口**断言
    // ==============================================================================================
    //  判据（第一权威 = 反编译，**已闭合、⛔ 不要重新论证**）：
    //    · 3 `inPlayAttacking`      —— `CardScript__SetupActiveAbility.c:5`（复位 = `UsedActiveAbility` /
    //      `CancelActiveAbility` 回 2）；
    //    · 17 `inPlayAminingAttack` —— `CardScript__SetupAttack.c:12`（复位 = `FinishAfterAttack.c:80-81`
    //      `cardState != 5 ⇒ 2` / `CancelAttack`）；
    //    · 15 `inPlayTransforming`  —— `CardScript.SetAsUnitTransforming()` 末句（`CardScript.cs:15656`），
    //      两个**活**入口：① `case 0x48` **into 支** → `ResolveTransformIntoRemnant`（ILSpy `BattleManager.cs:12097`）
    //      = 我们的变残骸支；② `case 0x48` **from 支**（`ResolveAction.c:6482` 内联）= 我们的残骸退路复活支。
    //      ⚠️ `ResolveWeaponKilled`（`:15509`）那条本 build **没有武器卡种 ⇒ 是死的**，
    //      **不能**因为「只查了那一条」就判「15 零可达」（`B92_B2b` §2·4 的旧错，`B93_Q1` §3·1 已推翻）。
    //
    //  🔴 **为什么用 `ctx.CardStateProbe` 而不是 `BroadcastWhen` 的监听器**：
    //    窗口外的监听器是**入队**的（`FireBoardWhenListener` → `EnqueueAbility`），出队时窗口早已关闭
    //    ⇒ 从监听器里读到的永远是复位后的 2。探针是**同步**调用的（`UnitStateCardState.cs` 类尾的
    //    `partial class BattleContext`），生产恒 `null`。
    //
    //  🔴 **⛔ 绝对不能断 `IsInPlayState()`** —— `{2,15,3,17}` 四档同真 ⇒ 恒定绿、等于没断。
    //    这里一律**直接比枚举值**，并各配一条 `!= inPlay` 的灭自证（删掉 `MarkCardState` 那一句 ⇒ 必红）。
    static void W12CardStateWindows()
    {
        // ---- ① `inPlayAttacking`(3)：主动技能窗口 ----
        {
            UnitState caster;
            UnitState prey;
            var ctx = Duel(Unit("E17Caster", 3, 2, 5, "Ability: Damage 3 EnemyUnit"),
                           Unit("E17Prey", 1, 4, 6), out caster, out prey);
            int seen = -1;                                  // ⚠️ 初值 -1：探针**没被调用**也必红（不是假绿）
            ctx.CardStateProbe = u => { if (ReferenceEquals(u, caster)) seen = (int)u.CardStateOf(ctx); };
            CheckCode(RuleCore.UseAbility(ctx, 0, 3, 3), RuleCodes.OK,
                      "🧪（① 前提）施法者真的放出了主动技能");
            ctx.CardStateProbe = null;

            Check(seen, (int)CardStateOptions.inPlayAttacking,
                  "★★（①）**主动技能窗口内 `cardState == 3 inPlayAttacking`**"
                + "｜判据 = `CardScript__SetupActiveAbility.c:5`；🧨 删掉 `UseAbilityCore` 那句 "
                + "`u.MarkCardState(CardStateOptions.inPlayAttacking);` ⇒ 实得 2 ⇒ 本行红");
            CheckTrue(seen != (int)CardStateOptions.inPlay,
                      "★★（① 灭自证）窗口内**不是** `inPlay(2)` —— 上一条不许靠「2 也算在场」蒙过去");
            Check((int)caster.CardStateOf(null), (int)CardStateOptions.inPlay,
                  "★★（①）**窗口后复位成 `inPlay(2)`**"
                + "｜🧨 把 `finally { u.ClearCardStateMark(); }` 删掉 ⇒ 脏标记永久 3 ⇒ 本行红");
        }

        // ---- ② `inPlayAminingAttack`(17)：攻击窗口 ----
        {
            UnitState atk;
            UnitState tgt;
            var ctx = Duel(Unit("E17Atk", 1, 2, 5), Unit("E17Tgt", 1, 0, 9), out atk, out tgt);
            int seen = -1;
            ctx.CardStateProbe = u => { if (ReferenceEquals(u, atk)) seen = (int)u.CardStateOf(ctx); };
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK,
                      "🧪（② 前提）攻击真的宣告成功（2 攻打 9 血 ⇒ 攻击者活着，好读窗口后的值）");
            ctx.CardStateProbe = null;

            Check(seen, (int)CardStateOptions.inPlayAminingAttack,
                  "★★（②）**攻击窗口内 `cardState == 17 inPlayAminingAttack`**"
                + "｜判据 = `CardScript__SetupAttack.c:12`；🧨 删掉 `DeclareAttackCore` 那句 "
                + "`attacker.MarkCardState(CardStateOptions.inPlayAminingAttack);` ⇒ 实得 2 ⇒ 本行红");
            CheckTrue(seen != (int)CardStateOptions.inPlay,
                      "★★（② 灭自证）窗口内**不是** `inPlay(2)`");
            Check((int)atk.CardStateOf(null), (int)CardStateOptions.inPlay,
                  "★★（②）**窗口后复位成 `inPlay(2)`**"
                + "｜🧨 删掉 `finally { attacker.ClearCardStateMark(); }` ⇒ 永久 17 ⇒ 本行红");
        }

        // ---- ③ `inPlayTransforming`(15) · 变残骸支（原版 `case 0x48` into 支）----
        {
            // 夹具照 `RuleEngineTest.cs:6432` 那具「带 Remnant 关键词的 5 血单位」：
            //   `CleanupDeaths` 的 `(Remnant || Waystone) && !IsRemnant` 那一跳 ⇒ 当场翻面成残骸。
            var remDef = new CardDef("E17RemnantDoomed", "E17RemnantDoomed", "unit", "",
                                     "common", "Test", 1, 2, 5, 0,
                                     new[] { KeywordTable.Remnant }, subtype: "Infantry");
            var ctx = Battle(new[] { Unit("E17HandA", 1, 1, 1) }, new[] { Unit("E17HandX", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 3, Unit("E17Con", 1, 5, 5));      // 5 攻 ⇒ 一击打掉 5 血
            var doomed = Place(ctx, 1, 3, remDef);           // 5 血 ⇒ 正好被打死

            int seen = -1;
            int again = -1;
            ctx.CardStateProbe = u =>
            {
                if (!ReferenceEquals(u, doomed)) return;
                seen = (int)u.CardStateOf(ctx);
                // 原版幂等闸（`CardScript.cs:15617`：`cardState == inPlayTransforming ⇒ goto`）：
                // 已经是 15 时**再标一次仍是 15**。⛔ 这不是「测实现」，是照原版那一道闸的判别式。
                u.MarkCardState(CardStateOptions.inPlayTransforming);
                again = (int)u.CardStateOf(ctx);
            };
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK,
                      "🧪（③ 前提）打死那个带 `Remnant` 的单位");
            ctx.CardStateProbe = null;

            CheckTrue(Board(ctx, 1, 3) is { IsRemnant: true },
                      "🧪（③ 前提）它**真的翻面成残骸**、留在 3 号格（没翻面 ⇒ 下面全是假绿）"
                    + LogTail(ctx));
            Check(seen, (int)CardStateOptions.inPlayTransforming,
                  "★★★（③）**变残骸窗口内 `cardState == 15 inPlayTransforming`**"
                + "｜判据 = `case 0x48` into 支 → `ResolveTransformIntoRemnant`（ILSpy `BattleManager.cs:12097`）"
                + "→ `SetAsUnitTransforming`（`CardScript.cs:15656`）；🧨 删掉 `CleanupDeaths` 残骸支那句 "
                + "`u.MarkCardState(CardStateOptions.inPlayTransforming);` ⇒ 实得 2 ⇒ 本行红");
            CheckTrue(seen != (int)CardStateOptions.inPlay,
                      "★★★（③ 灭自证）窗口内**不是** `inPlay(2)` —— 15 与 2 在 `IsInPlay()` 里同真，"
                    + "只断 `IsInPlayState` 恒绿 ⇒ 必须比枚举值本身");
            Check(again, (int)CardStateOptions.inPlayTransforming,
                  "★★（③）照原版幂等闸：窗口内**再标一次 15，仍是 15**（`CardScript.cs:15617`）");
            Check((int)doomed.CardStateOf(null), (int)CardStateOptions.inPlay,
                  "★★（③）**窗口后 `doomed` 的标记复位成 `inPlay(2)`**"
                + "｜🧨 删掉分支收尾的 `finally { u.ClearCardStateMark(); }` ⇒ 永久 15 ⇒ 本行红"
                + "（⚠️ 残骸是**新建的另一个 `UnitState`** ⇒ 复位读的是 `doomed` 那一份，不是 `Board[1,3]`）");
        }

        // ---- ④ `inPlayTransforming`(15) · 复活支（原版 `case 0x48` from 支）----
        {
            var raise = Tactic("E17Raise", 0, "Reanimate a friendly Remnant");
            var ctx = Battle(new[] { raise }, new[] { Unit("E17Foe", 1, 0, 9) });
            ToP1Turn(ctx, 1);
            var rem = Place(ctx, 0, 5, Unit("E17Rem", 1, 0, 1), exhausted: true);
            rem.IsRemnant = true;                            // 一具残骸（攻 0 / 1 血，按定义）

            int seenFirst = -1, seenLast = -1;
            int got = 0;
            ctx.CardStateProbe = u =>
            {
                if (!ReferenceEquals(u, rem)) return;
                int st = (int)u.CardStateOf(ctx);
                if (got == 0) seenFirst = st; seenLast = st; got++;   // 🔴 2026-10-30（`B100_F18` · G4）：探针 off-by-one —— 第 1 次调用（开窗、仍是 15）才是 `seenFirst`；`got == 1` 记的是**第 2 次**（此时已 `ClearCardStateMark` ⇒ 2），它同时把 `seenLast` 覆盖掉。
            };
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "E17Raise"), -1), RuleCodes.OK,
                      "🧪（④ 前提）打出 `Reanimate a friendly Remnant`（场上有一具残骸 ⇒ 残骸退路）");
            ctx.CardStateProbe = null;

            var back = Board(ctx, 0, 5);
            CheckTrue(back != null && ReferenceEquals(back, rem) && !back.IsRemnant,
                      "🧪（④ 前提）残骸**翻回成活的单位**了，而且**是同一个 `UnitState`**"
                    + "（`ReanimateRemnantInPlace` 原地返）" + LogTail(ctx));
            Check(got, 2, "🧪（④ 前提）窗口探针恰被调用 **2** 次（= 开窗一次 + 收尾一次；1 次 ⇒ 少了一处探针；0 次 ⇒ 窗口没开、"
                        + "下面几条是假绿；>2 次 ⇒ 同一具残骸被标了多遍）");
            Check(seenFirst, (int)CardStateOptions.inPlayTransforming,
                  "★★★（④）**复活窗口内 `cardState == 15 inPlayTransforming`**"
                + "｜判据 = `case 0x48` from 支（`ResolveAction.c:6482` 内联 `SetAsUnitTransforming`）；"
                + "🧨 删掉 `DoReanimate` 那句 `rem.MarkCardState(CardStateOptions.inPlayTransforming);` ⇒ 实得 2 ⇒ 本行红");
            Check(seenLast, (int)CardStateOptions.inPlay,
                      "★★★（④）**收尾时已回 `inPlay(2)`**（原版 `SetAsUnitInPlay` 在 `BroadcastUnitReanimated` 之后）");
            Check((int)rem.CardStateOf(null), (int)CardStateOptions.inPlay,
                  "★★（④）**窗口后复位成 `inPlay(2)`**（原版 onComplete 的 `SetAsUnitInPlay`）"
                + "｜🧨 删掉循环体收尾的 `finally { rem.ClearCardStateMark(); }` ⇒ 永久 15 ⇒ 本行红");
            // ⚠️ 墓地退路（候选 ②）**不标 15** —— 原版没有那条路（`B93_Q1` §7 硬约束 ③），
            //    所以这里**不**为它写断言；哪天要标必须先按铁律 18 打 `🔶 自己设计`。
        }
    }
}
