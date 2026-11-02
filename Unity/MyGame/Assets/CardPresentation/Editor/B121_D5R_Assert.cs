// B121_D5R_Assert.cs — 第一百二十一~二十二会话 · 执行写手 `B121-D5R`（附魔 `ca 70 / 100` 执行体 · 五支取牌）
//
// ============================ 它盯的是什么 ============================
//   `B121-D5` 收尾时 `ca 70 summonUnit` / `ca 100 createCardInHand` 仍是「已知 ca、载荷缺 ⇒ 精确出声」；
//   `B121-D4` 已把数据列（`CardDef.AbilitySummonSpec.UnitsToSummon` + 生成器 `summon.ids` + `CardDef.RawCardPool`
//   全池钩子）补齐。本件验的是 **执行体真的接上了**，且**五支取牌逐支对得上原版**。
//
// ============================ 原版口径（判据，现读 `d:/2/tools/decomp_full/`）============================
//   · 五支取牌 = `AbilityLogic__GetCardToSummon.c` 全文（①随机一张 ②整表 ③场上目标 ④扫收藏 ⑤选牌）：
//     ⑤ `:239-276`（**最先判**，`selectFromChooseResolution`：`cardChosenInSelection` 取一次就清空 / 否则 `poolChoosenCard[0]`）
//     ① `:191-208`（`unitsToSummon` 非空 ∧ `!summonAllTargets` ⇒ `GetRandomInt(0,Count)`）
//     ② `:209-236`（`unitsToSummon` 非空 ∧ `summonAllTargets` ⇒ 整表）
//     ③ `:103-149`（`unitsToSummon` 空 ∧ `chooseCardInPlay` ⇒ `GetTargets(...)` 的目标集；`shouldRemoveFromCemetery`）
//     ④ `:45-101`（`unitsToSummon` 空 ∧ `!chooseCardInPlay` ⇒ 扫整个收藏，三闸 = criteria ∧ `!cantBeRandomlyCreated` ∧ `!IsHidden()`）
//     ③④ 共同收尾 `:150-187`：空表原样返回 · `summonAllTargets` ⇒ 整表 · 否则随机一张
//   · 落点：`ca 70` = `PlayAbility.c:723-878`（`case 0x46`，`AddSummonUnit` → 我们 `RuleCore.DeployFree`，
//     侧 = `carrier.isPlayer XOR isForEnemy`，落点 = `createAtPos` 四档经 `GetNextSlotToCard`）
//   · `ca 100` = `PlayAbility.c:986-1037`（`case 0x64`，`AddNewCardToHand` → 我们 `RuleCore.AddCardToHandWithinLimit`）
//   · 重复次数 `local_178` = `PlayAbility.c:180-236`（`multiplier` 目标数 × `furtherMultiplier`）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A2：把「随机取一张」改成「整表全给」⇒ 场上多出 2 个 ⇒ 红（与 A1 结构上互斥）；
//   · B2：把 `chooseCardInPlay` 判在 `unitsToSummon` **之前** ⇒ 召出的会是载体而不是 `ids[0]` ⇒ 红；
//   · C1：把三闸任一拿掉 ⇒ 候选从 1 变 3（`all=1` 全给）⇒ 场上多出人 ⇒ 红；
//   · C2：把 `unitsToSummon == null` 折成空表 ⇒ 会真去扫池 ⇒ 返回 >0 ⇒ 红；
//   · C3：把「`RawCardPool` 没登记」当成空池 / 拿可玩池顶替 ⇒ 返回 >0 或不出声 ⇒ 红；
//   · D1：删掉 `cardChosenInSelection` 的清空 ⇒ 字段还留着 ⇒ 红（原版是**取一次就清空**）；
//   · E2：把 `isForEnemy` 的 XOR 删掉 ⇒ 牌造到己方手牌 ⇒ 红；
//   · F2：把 `createAtPos` 的请求落点删掉（恒 -1）⇒ 两档落点相同 ⇒ 红；
//   · F3：把 `Mult != 0` 当 1 ⇒ 返回 >0 ⇒ 红；
//   · G1：把 `ca 70` 的执行器接到**所有** ca 上（或让 `summonCriteria` 自己找 ca）⇒ ca 130 也召出人 ⇒ 红。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加：
//     `Guard("B121D5R_Assert", B121_D5R_Assert.Run); _pass += B121_D5R_Assert.Pass; _fail += B121_D5R_Assert.Fail;`
//   （⚠️ 追加的文本必须落在该行第一个 `//` **之前**。）
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B121-D5R 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B121_D5R_Assert
{
    const int Trig = 641;          // 模板行挂一个存在的 trigger（本组只测 `ca` 分派与取牌，不测派发闸）
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B121-D5R：附魔 `ca 70 / 100` 执行体（五支取牌 + 落点）");

        // ---- A 组：支①「随机取一张」vs 支②「整表全给」-------------------------------
        Section("A 支①②：`unitsToSummon` 非空 —— `summonAllTargets` 决定「整表 / 随机一张」");
        {
            var u1 = Unit("B121D5RSumA"); var u2 = Unit("B121D5RSumB");
            var pool = new List<CardDef> { u1, u2 };
            PushPool(pool);
            try
            {
                // A1 = 支②：整表全给
                var ctx1 = MakeBattle(out var carrier1, out _);
                if (ctx1 == null || carrier1 == null) CheckTrue(false, "A0 前提不成立：第一场没建起来");
                else
                {
                    var tpl = SummonTpl("B121D5RTplAll", 70, ta: 20, all: 1, choose: 0, cap: -1, enemy: 0,
                                        ids: new[] { u1.Id, u2.Id });
                    int before = BoardCount(ctx1, 0);
                    int n = Run(ctx1, carrier1, tpl);
                    Check(n, 2, "A1 ★★★ **结论（支②）**：`summonAllTargets = 1` ⇒ 候选表**整表全给**（召 2 个）"
                              + "｜🧨 改成「随机取一张」⇒ 1 ⇒ 红");
                    Check(BoardCount(ctx1, 0) - before, 2, "A1b 场上真的多了 2 个单位");
                    Check(HasUnit(ctx1, 0, u1.Id) && HasUnit(ctx1, 0, u2.Id), true,
                          "A1c 两张候选都上了场（顺序与重复都保留）");
                }

                // A2 = 支①：随机取一张
                var ctx2 = MakeBattle(out var carrier2, out _);
                if (ctx2 == null || carrier2 == null) CheckTrue(false, "A0b 前提不成立：第二场没建起来");
                else
                {
                    var tpl = SummonTpl("B121D5RTplRand", 70, ta: 20, all: 0, choose: 0, cap: -1, enemy: 0,
                                        ids: new[] { u1.Id, u2.Id });
                    int before = BoardCount(ctx2, 0);
                    int n = Run(ctx2, carrier2, tpl);
                    Check(n, 1, "A2 ★★★ **结论（支①）**：`summonAllTargets = 0` ⇒ **随机取一张**（只召 1 个）"
                              + "｜🧨 改成「整表全给」⇒ 2 ⇒ 红（与 A1 结构上不可能同时满足）");
                    Check(BoardCount(ctx2, 0) - before, 1, "A2b 场上只多出 1 个");
                    bool a = HasUnit(ctx2, 0, u1.Id), b = HasUnit(ctx2, 0, u2.Id);
                    Check(a ^ b, true, "A2c 召出来的两张里**恰好命中一张**（真的只在候选表里挑）");
                }
            }
            finally { PopPool(); }
        }

        // ---- B 组：支③「场上目标」-----------------------------------------------------
        Section("B 支③：`unitsToSummon` 空 ∧ `chooseCardInPlay` ⇒ 取 `GetTargets` 的目标集");
        {
            var u3 = Unit("B121D5RSumC");
            PushPool(new List<CardDef> { u3 });
            try
            {
                var ctx = MakeBattle(out var carrier, out _);
                if (ctx == null || carrier == null) CheckTrue(false, "B0 前提不成立");
                else
                {
                    int before = BoardCount(ctx, 0);
                    // `ta = 20 (self)` ⇒ 目标集 = 载体 ⇒ 候选 = 载体那张卡
                    var tpl = SummonTpl("B121D5RTplChoose", 70, ta: 20, all: 1, choose: 1, cap: -1, enemy: 0,
                                        ids: new string[0]);
                    int n = Run(ctx, carrier, tpl);
                    Check(n, 1, "B1 ★★★ **结论（支③）**：`unitsToSummon = []` + `chooseCardInPlay = 1` ⇒ "
                              + "按**目标集**取牌（召 1 个）｜🧨 把这一支走成「扫收藏」⇒ 0 或别的卡 ⇒ 红");
                    Check(CountUnitOf(ctx, 0, carrier.Card.Id), 2,
                          "B1b 召出来的**就是目标的卡**（`ta=20` ⇒ 载体自己那张 ⇒ 场上同 id 变成 2 份）");
                    Check(BoardCount(ctx, 0) - before, 1, "B1c 场上正好多了 1 个");
                }
            }
            finally { PopPool(); }

            // B2 反证：`unitsToSummon` **非空**时 `chooseCardInPlay` 必须被忽略（原版分支顺序）
            PushPool(new List<CardDef> { u3 });
            try
            {
                var ctx2 = MakeBattle(out var carrier2, out _);
                if (ctx2 == null || carrier2 == null) CheckTrue(false, "B2a 前提不成立");
                else
                {
                    var tpl2 = SummonTpl("B121D5RTplChooseIgnored", 70, ta: 20, all: 1, choose: 1, cap: -1, enemy: 0,
                                         ids: new[] { u3.Id });
                    Run(ctx2, carrier2, tpl2);
                    Check(HasUnit(ctx2, 0, u3.Id) && CountUnitOf(ctx2, 0, carrier2.Card.Id) == 1, true,
                          "B2 ★★★ **反证**：`ids` 非空时 `chooseCardInPlay` **被忽略**（原版分支顺序：先看 `unitsToSummon`）"
                        + "｜🧨 把 `chooseCardInPlay` 判在前面 ⇒ 召出的会是载体（同 id 变 2 份）⇒ 红");
                }
            }
            finally { PopPool(); }
        }

        // ---- C 组：支④「扫整个收藏」+ 空 ids / 未登记 的出声 -----------------------------
        Section("C 支④：`unitsToSummon` 空 ∧ `!chooseCardInPlay` ⇒ 扫整个收藏（三闸）；空 ids / 未登记**出声不崩**");
        {
            var keep = Unit("B121D5RPoolKeep");
            var noRand = new CardDef("B121D5RPoolNoRand", "B121D5RPoolNoRand", "unit", "", "common", "Test",
                                     1, 1, 20, 0, null, subtype: "Infantry", cantBeRandomlyCreated: true);
            var hidden = new CardDef("B121D5RPoolHidden", "B121D5R 教程卡 (tutorial)", "unit", "", "common", "Test",
                                     1, 1, 20, 0, null, subtype: "Infantry");
            PushPool(new List<CardDef> { keep, noRand, hidden });
            try
            {
                var ctx = MakeBattle(out var carrier, out _);
                if (ctx == null || carrier == null) CheckTrue(false, "C0 前提不成立");
                else
                {
                    // 三闸：`cantBeRandomlyCreated` / `IsHidden()`（卡名带 `(tutorial)`）/ 准则（`ta=0` ⇒ 不筛）
                    var tpl = SummonTpl("B121D5RTplScan", 70, ta: 0, all: 1, choose: 0, cap: 20, enemy: 0,
                                        ids: new string[0], crit: Criteria(0));
                    int before = BoardCount(ctx, 0);
                    int n = Run(ctx, carrier, tpl);
                    Check(n, 1, "C1 ★★★ **结论（支④）**：扫收藏 + 三闸 ⇒ 只剩 **1 张**候选（整表全给 ⇒ 召 1 个）"
                              + "｜🧨 拿掉 `cantBeRandomlyCreated` 或 `IsHidden` 任一闸 ⇒ 2~3 张 ⇒ 红");
                    Check(HasUnit(ctx, 0, keep.Id), true, "C1b 召出来的是**过了三闸的那一张**");
                    Check(BoardCount(ctx, 0) - before, 1, "C1c 场上正好多了 1 个");
                }
            }
            finally { PopPool(); }

            // C2：`unitsToSummon == null`（**没导出**，不是空表）⇒ 出声 + 不结算 + **不崩**
            {
                var ctx = MakeBattle(out var carrier, out _);
                if (ctx == null || carrier == null) CheckTrue(false, "C2a 前提不成立");
                else
                {
                    var tpl = SummonTpl("B121D5RTplIdsNull", 70, ta: 0, all: 0, choose: 0, cap: -1, enemy: 0,
                                        ids: null);
                    int gapBefore = RuleCore.EnchantRowsDataGapForTest;
                    int before = BoardCount(ctx, 0);
                    int n = Run(ctx, carrier, tpl);
                    Check(n, 0, "C2 ★★★ **空 ids（`null` = 没导出）⇒ 不结算**（返回 0，⛔ 不拿「随便召一个」顶）");
                    CheckTrue(RuleCore.EnchantRowsDataGapForTest > gapBefore,
                              "C2b ★★★ **不静默**：走了出声口（计数涨了）｜不是「静默 return 0」");
                    Check(BoardCount(ctx, 0) - before, 0, "C2c 场上一个都没多（**不崩**、也没落怪东西）");
                }
            }

            // C3：`RawCardPool` **没登记**（探针 / 手写夹具）⇒ 出声 + 不拿可玩池顶替
            {
                var ctx = MakeBattle(out var carrier, out _);
                if (ctx == null || carrier == null) CheckTrue(false, "C3a 前提不成立");
                else
                {
                    var saved = CardDef.RawCardPoolProvider;
                    CardDef.RawCardPoolProvider = null;
                    try
                    {
                        var tpl = SummonTpl("B121D5RTplNoPool", 70, ta: 0, all: 1, choose: 0, cap: -1, enemy: 0,
                                            ids: new string[0], crit: Criteria(0));
                        int gapBefore = RuleCore.EnchantRowsDataGapForTest;
                        int before = BoardCount(ctx, 0);
                        int n = Run(ctx, carrier, tpl);
                        Check(n, 0, "C3 ★★★ **空 ids + 全池钩子没登记 ⇒ 判不了、不结算**（返回 0）"
                                  + "｜🧨 把 `RawCardPool == null` 当空池 / 拿可玩池顶替 ⇒ 可能召出人 ⇒ 红");
                        CheckTrue(RuleCore.EnchantRowsDataGapForTest > gapBefore, "C3b ★★★ **不静默**：出声了");
                        Check(BoardCount(ctx, 0) - before, 0, "C3c 场上一个都没多");
                    }
                    finally { CardDef.RawCardPoolProvider = saved; }
                }
            }

            // C4：`unitsToSummon` 非空 + 全池钩子没登记 ⇒ 出声 + 不结算（⛔ 不拿可玩池顶替查表）
            {
                var ctx = MakeBattle(out var carrier, out _);
                if (ctx == null || carrier == null) CheckTrue(false, "C4a 前提不成立");
                else
                {
                    var saved = CardDef.RawCardPoolProvider;
                    CardDef.RawCardPoolProvider = null;
                    try
                    {
                        var tpl = SummonTpl("B121D5RTplIdNoPool", 70, ta: 20, all: 1, choose: 0, cap: -1, enemy: 0,
                                            ids: new[] { "B121D5RAnyId" });
                        int gapBefore = RuleCore.EnchantRowsDataGapForTest;
                        int before = BoardCount(ctx, 0);
                        int n = Run(ctx, carrier, tpl);
                        Check(n, 0, "C4 ★★★ **ids 非空 + 全池钩子没登记 ⇒ 判不了、不结算**（返回 0）"
                                  + "｜🧨 拿 `ctx.CardPool` 顶替查表 ⇒ 可能召出人 ⇒ 红");
                        CheckTrue(RuleCore.EnchantRowsDataGapForTest > gapBefore, "C4b ★★★ **不静默**：出声了");
                        Check(BoardCount(ctx, 0) - before, 0, "C4c 场上一个都没多");
                    }
                    finally { CardDef.RawCardPoolProvider = saved; }
                }
            }
        }

        // ---- D 组：支⑤「选牌里点定的那张」----------------------------------------------
        Section("D 支⑤：`selectFromChooseResolution` ⇒ `cardChosenInSelection`（**取一次就清空**）");
        {
            var picked = Unit("B121D5RPicked");
            PushPool(new List<CardDef> { picked });
            try
            {
                var ctx = MakeBattle(out var carrier, out _);
                if (ctx == null || carrier == null) CheckTrue(false, "D0 前提不成立");
                else
                {
                    var crit = Criteria(0);
                    crit.SelectFromChooseResolution = 1;
                    var tpl = SummonTpl("B121D5RTplChooseRes", 70, ta: 0, all: 0, choose: 0, cap: -1, enemy: 0,
                                        ids: new[] { "B121D5RNeverUsed" }, crit: crit);
                    ctx.CardChosenInSelection = picked;
                    int n = Run(ctx, carrier, tpl);
                    Check(n, 1, "D1 ★★★ **结论（支⑤）**：取「刚在选牌里点定的那张」⇒ 召 1 个"
                              + "｜🧨 把它走成支①（`ids` 那条）⇒ 会去查 `B121D5RNeverUsed` ⇒ 0 ⇒ 红");
                    Check(HasUnit(ctx, 0, picked.Id), true, "D1b 召出来的**就是选定的那张**");
                    CheckTrue(ctx.CardChosenInSelection == null,
                              "D2 ★★★ **一次性**：`cardChosenInSelection` 取完就清空（原版 `.c:270` 的 `*(manager+0x2e8) = 0`）"
                            + "｜🧨 忘了清 ⇒ 下一条带同一旗的效果会重复吃到它");
                }
            }
            finally { PopPool(); }
        }

        // ---- E 组：`ca 100 createCardInHand`（造牌进手牌 + 侧翻转）------------------------
        Section("E `ca 100`：造牌进**收牌方**手牌（`isForEnemy` 决定哪一侧）");
        {
            var spell = new CardDef("B121D5RSpell", "B121D5RSpell", "tactic", "", "common", "Test",
                                    0, 0, 1, 0, null, subtype: "Sabotage");
            PushPool(new List<CardDef> { spell });
            try
            {
                var ctx = MakeBattle(out var carrier, out _);
                if (ctx == null || carrier == null) CheckTrue(false, "E0 前提不成立");
                else
                {
                    var tpl = SummonTpl("B121D5RTplHand", 100, ta: 0, all: 0, choose: 0, cap: -1, enemy: 0,
                                        ids: new[] { spell.Id });
                    int handBefore = ctx.Players[0].Hand.Count;
                    int n = Run(ctx, carrier, tpl);
                    Check(n, 1, "E1 ★★★ **结论**：`ca 100` 造 1 张进**己方**手牌");
                    Check(ctx.Players[0].Hand.Count - handBefore, 1, "E1b 己方手牌 +1");
                    Check(HandHas(ctx, 0, spell.Id), true, "E1c 手牌里那张就是 `unitsToSummon` 指的卡");
                }

                var ctx2 = MakeBattle(out var carrier2, out _);
                if (ctx2 == null || carrier2 == null) CheckTrue(false, "E2a 前提不成立");
                else
                {
                    var tpl2 = SummonTpl("B121D5RTplHandEnemy", 100, ta: 0, all: 0, choose: 0, cap: -1, enemy: 1,
                                         ids: new[] { spell.Id });
                    int mine = ctx2.Players[0].Hand.Count, theirs = ctx2.Players[1].Hand.Count;
                    Run(ctx2, carrier2, tpl2);
                    Check(ctx2.Players[0].Hand.Count - mine, 0, "E2 ★★★ **反证**：`isForEnemy = 1` ⇒ **己方**手牌不动");
                    Check(ctx2.Players[1].Hand.Count - theirs, 1, "E2b 牌造到**敌方**手牌（`carrier.isPlayer XOR isForEnemy`）");
                }
            }
            finally { PopPool(); }
        }

        // ---- F 组：重复次数 / `createAtPos` 落点 / `Mult != 0` 判不了 -----------------------
        Section("F 重复次数（`local_178`）· `createAtPos` 落点 · `multiplier` 判不了");
        {
            var u = Unit("B121D5RRepeat");
            PushPool(new List<CardDef> { u });
            try
            {
                // F1：`furtherMultiplier = 2` ⇒ 2 轮 × 每轮 1 张
                var ctx = MakeBattle(out var carrier, out _);
                if (ctx == null || carrier == null) CheckTrue(false, "F1a 前提不成立");
                else
                {
                    var tpl = SummonTpl("B121D5RTplRepeat", 70, ta: 20, all: 1, choose: 0, cap: -1, enemy: 0,
                                        ids: new[] { u.Id }, fm: 2);
                    int n = Run(ctx, carrier, tpl);
                    Check(n, 2, "F1 ★★★ **结论**：`furtherMultiplier = 2` ⇒ 原版 `local_178` 那段 `do{}while` 跑 **2 轮**"
                              + "（每轮整表 ⇒ 2 个）｜🧨 忽略 `Fm` ⇒ 1 ⇒ 红");
                }

                // F2：`createAtPos` 0(self) ⇒ 贴载体；14 ⇒ 原版「出声 + 默认落点」
                var ctx2 = MakeBattle(out var carrier2, out _);
                if (ctx2 == null || carrier2 == null) CheckTrue(false, "F2a 前提不成立");
                else
                {
                    var tpl2 = SummonTpl("B121D5RTplCapSelf", 70, ta: 20, all: 1, choose: 0, cap: 0, enemy: 0,
                                         ids: new[] { u.Id });
                    Run(ctx2, carrier2, tpl2);
                    int wantSelf = BoardSlots.SlotOf(BoardSlots.Left, 1);          // 载体在左 0 号 ⇒ 贴它 = 左 1 号
                    Check(FindSlotOf(ctx2, 0, u.Id), wantSelf,
                          "F2 ★★★ **结论**：`createAtPos = 0 (self)` ⇒ 落到**载体旁边**那一格（原版 `GetNextSlotToCard`）"
                        + "｜🧨 删掉请求落点（恒 -1）⇒ 会落到默认格 ⇒ 红");
                }

                var ctx3 = MakeBattle(out var carrier3, out _);
                if (ctx3 == null || carrier3 == null) CheckTrue(false, "F3a 前提不成立");
                else
                {
                    var tpl3 = SummonTpl("B121D5RTplCapOther", 70, ta: 20, all: 1, choose: 0, cap: 14, enemy: 0,
                                         ids: new[] { u.Id });
                    Run(ctx3, carrier3, tpl3);
                    int wantDefault = BoardSlots.SlotOf(BoardSlots.Right, 0);       // 人少那侧最外一格
                    Check(FindSlotOf(ctx3, 0, u.Id), wantDefault,
                          "F2b **反证**：`createAtPos = 14` 不在三档内 ⇒ 原版出声 + **默认落点**（与 F2 不同格）");
                }

                // F3：`multiplier.targetsAffected != 0` ⇒ 重复次数判不了 ⇒ 出声、不结算
                var ctx4 = MakeBattle(out var carrier4, out _);
                if (ctx4 == null || carrier4 == null) CheckTrue(false, "F4a 前提不成立");
                else
                {
                    var tpl4 = SummonTpl("B121D5RTplMult", 70, ta: 20, all: 1, choose: 0, cap: -1, enemy: 0,
                                         ids: new[] { u.Id }, mult: 1);
                    int gapBefore = RuleCore.EnchantRowsDataGapForTest;
                    int n = Run(ctx4, carrier4, tpl4);
                    Check(n, 0, "F3 ★★★ **反证**：`multiplier.targetsAffected = 1` ⇒ 重复次数判不了 ⇒ **不结算**"
                              + "｜🧨 拿 1 顶目标数 ⇒ 返回 >0 ⇒ 红");
                    CheckTrue(RuleCore.EnchantRowsDataGapForTest > gapBefore, "F3b ★★★ **不静默**：出声了");
                }
            }
            finally { PopPool(); }
        }

        // ---- G 组：别的 ca 不走这支（分派反证）------------------------------------------
        Section("G 反证：别的 `ca` **不走**这支（同一份 `summonCriteria` 挂到 `ca 130` ⇒ 一个都不召）");
        {
            var u = Unit("B121D5RNotSummoned");
            PushPool(new List<CardDef> { u });
            try
            {
                var ctx = MakeBattle(out var carrier, out _);
                if (ctx == null || carrier == null) CheckTrue(false, "G0 前提不成立");
                else
                {
                    var tpl = SummonTpl("B121D5RTplWrongCa", 130, ta: 20, all: 1, choose: 0, cap: -1, enemy: 0,
                                        ids: new[] { u.Id });
                    int before = BoardCount(ctx, 0);
                    Run(ctx, carrier, tpl);
                    Check(HasUnit(ctx, 0, u.Id), false,
                          "G1 ★★★ **反证**：`ca 130 (stun)` 拿到同一份 `summonCriteria` 也**不召人**"
                        + "（分派按 `ca`，⛔ 不是「有 summonCriteria 就走召唤」）");
                    Check(BoardCount(ctx, 0) - before, 0, "G1b 场上一个都没多");
                }
            }
            finally { PopPool(); }
        }

        Summary();
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    /// <summary>一张附魔模板卡：1 条 ability / 1 行 logic + 一行 `summonCriteria`（+ 可选 36 格）。</summary>
    static CardDef SummonTpl(string id, int ca, int ta, int all, int choose, int cap, int enemy,
                             string[] ids, int fm = 0, int mult = 0, CardDef.CountSetSpec crit = null)
    {
        var c = new CardDef(id, id, "enchantment", "", "", "", 0, 0, 0, 0, null);
        c.AttachTriggerAbilityIndices(new[]
        {
            new CardDef.TriggerAbilityIndexSpec
            { Ab = 0, Lg = 0, Trigger = Trig, TriggerValue = 0, Ca = ca, Ta = ta, Pa = 0, Fm = fm, Mult = mult },
        });
        c.AttachAbilityTriggers(new[] { Trig });
        c.AttachAbilitySummon(new[]
        {
            new CardDef.AbilitySummonSpec
            {
                Ab = 0, Lg = 0, CreateAtPos = cap, MoveToPos = 0,
                SummonAllTargets = all, ChooseCardInPlay = choose, KeepEffects = 0,
                ShouldRemoveFromCemetery = 0, IsForEnemy = enemy, UnitsToSummon = ids,
            },
        });
        if (crit != null)
            c.AttachAbilityTargetCriteria(new[]
            {
                new CardDef.TargetCriteriaRowSpec { Ability = 0, Trigger = Trig, Logic = 0, Criteria = crit },
            });
        return c;
    }

    /// <summary>一份**全格都已知**的 36 格 criteria（`-1` 会让 `TargetCriteriaEval` 返回「判不了」）。</summary>
    static CardDef.CountSetSpec Criteria(int ta)
    {
        var s = new CardDef.CountSetSpec();
        s.PlayerTurn = -1;
        s.TargetsAffected = ta;
        s.ChosenArmy = 0; s.ArmyFilter = 0; s.KeywordFilter = 0; s.TraitsFilter = 0;
        s.MaxTargets = 0; s.TargetSelectMethod = 0;
        s.AttackFilter = 0; s.AttackFilterOption = 0;
        s.CurrentHealthFilter = 0; s.CurrentHealthFilterOption = 0;
        s.ManaCostFilter = 0; s.ManaCostFilterOption = 0;
        s.RarityFilter = 0; s.RarityFilterOption = 0;
        s.SpellType = 0;
        s.FriendlyUnits = 0; s.EnemyUnits = 0;
        s.IncludeHero = 0; s.IncludeMinions = 0; s.IncludeSpells = 0; s.IncludeWhispers = 0;
        s.IncludeWeapons = 0; s.IncludeRemnants = 0; s.IncludeHeroParts = 0;
        s.IncludeSelf = 0; s.ExcludeNotSelf = 0; s.ExcludeSimilarCards = 0; s.ExcludeTargetted = 0;
        s.ExcludeFriendlyHero = 0; s.ExcludeEnemyHero = 0; s.ExcludeRace = 0; s.ExcludeTrait = 0;
        s.SelectFromChooseResolution = 0;
        s.FilterSpecificCardIds = new string[0];
        s.ExcludeEnchantmentIds = new string[0];
        return s;
    }

    static CardDef Unit(string id)
    {
        return new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 20, 0, null, subtype: "Infantry");
    }

    /// <summary>建一局：双方各 6 张「填充卡」+ 一张督军；0 号玩家格 3 放载体、1 号玩家格 3 放一个敌兵。</summary>
    static BattleContext MakeBattle(out UnitState carrier, out UnitState opp)
    {
        var heroA = new CardDef("B121D5RHeroA", "B121D5RHeroA", "hero", "", null, "Ultramarines", 0, 2, 30, 0, null, subtype: "Warlord");
        var heroB = new CardDef("B121D5RHeroB", "B121D5RHeroB", "hero", "", null, "Goff", 0, 2, 30, 0, null, subtype: "Warlord");
        var carrierCard = Unit("B121D5RCarrier");
        var deckA = new List<CardDef> { heroA, carrierCard };
        var deckB = new List<CardDef> { heroB };
        for (int i = 0; i < 6; i++) { deckA.Add(Unit("B121D5RFillA" + i)); deckB.Add(Unit("B121D5RFillB" + i)); }
        var ctx = RuleCore.NewBattle(deckA, deckB, seed: 0, shuffle: false, cardPool: new List<CardDef>());
        carrier = PlaceUnit(ctx, 0, 3, carrierCard);
        opp = PlaceUnit(ctx, 1, 3, Unit("B121D5ROpp"));
        return ctx;
    }

    static UnitState PlaceUnit(BattleContext ctx, int p, int slot, CardDef card)
    {
        if (ctx == null || card == null) return null;
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>挂一件附魔（唯一写口 = `Enchantments.Attach`）再跑这一行 `abilityLogic`。</summary>
    static int Run(BattleContext ctx, UnitState carrier, CardDef tpl)
    {
        if (ctx == null || carrier == null || carrier.Instance == null || tpl == null) return 0;
        var eff = Enchantments.Attach(ctx, carrier.Instance, tpl, Enchantments.BuffTypeSingleUse);
        if (eff == null || eff.Enchantment == null) return 0;
        return RuleCore.RunEnchantmentLogicRowsForTest(ctx, eff.Enchantment, carrier, 0, 3, Trig, "EarnedSkull", null, null);
    }

    static void PushPool(List<CardDef> cards)
    {
        _savedProvider = CardDef.RawCardPoolProvider;
        CardDef.RawCardPoolProvider = () => cards;
    }

    static void PopPool() { CardDef.RawCardPoolProvider = _savedProvider; }
    static Func<IReadOnlyList<CardDef>> _savedProvider;

    static int BoardCount(BattleContext ctx, int p)
    {
        if (ctx == null) return -1;
        var b = ctx.Players[p].Board;
        int n = 0;
        for (int i = 0; i < b.Length; i++) if (b[i] != null) n++;
        return n;
    }

    static bool HasUnit(BattleContext ctx, int p, string cardId)
    {
        return FindSlotOf(ctx, p, cardId) >= 0;
    }

    static int FindSlotOf(BattleContext ctx, int p, string cardId)
    {
        if (ctx == null || string.IsNullOrEmpty(cardId)) return -1;
        var b = ctx.Players[p].Board;
        for (int i = 0; i < b.Length; i++)
            if (b[i] != null && b[i].Card != null && string.Equals(b[i].Card.Id, cardId, StringComparison.Ordinal)) return i;
        return -1;
    }

    /// <summary>某一侧场上**同 id** 的单位数（⚠️ 不能用 `HasUnit` 判「有没有被召出来」—— 载体自己就在场）。</summary>
    static int CountUnitOf(BattleContext ctx, int p, string cardId)
    {
        if (ctx == null || string.IsNullOrEmpty(cardId)) return 0;
        var b = ctx.Players[p].Board;
        int n = 0;
        for (int i = 0; i < b.Length; i++)
            if (b[i] != null && b[i].Card != null && string.Equals(b[i].Card.Id, cardId, StringComparison.Ordinal)) n++;
        return n;
    }

    static bool HandHas(BattleContext ctx, int p, string cardId)
    {
        if (ctx == null) return false;
        var h = ctx.Players[p].Hand;
        for (int i = 0; i < h.Count; i++)
            if (h[i] != null && h[i].Card != null && string.Equals(h[i].Card.Id, cardId, StringComparison.Ordinal)) return true;
        return false;
    }

    // ==================================================================
    //  最小断言框架
    // ==================================================================

    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("✓ " + msg); }
        else { _fail++; Debug.LogError("✗ " + msg); }
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B121_D5R 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B121_D5R 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
