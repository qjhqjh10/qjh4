// RuleEngineTest_B116W19.cs — 执行写手 `B116_W19`（2026-11-02 · 第一百一十六会话 · `ENG-07` `jam`）
//
// ============================ 这件活是什么 ============================
// 活状态表 `ENG-07` 的 `jam`(DefinedTrait 130) 那一格。施工面 = `资料/完全复刻/判据/B116_R7_ENG07剩余四条施工面.md` §1·2。
// 本件只盖**四类真缺口**（其余 20 处已有等价落点 / 原版零行为 / 0 读者，见施工面 §1·1 那张表）：
//   ① `cardTraits` 列没被读 —— 我们的 `Has()` 只读 `keywords` 列
//   ② `HasWhileInPlayAbility` 把「jam ⇒ **跳过**自己的能力」读成了「jam ⇒ **命中**」
//   ③ `RawCardScript.GetAbilitiesForTrigger` 的 jam 闸（本层拿不到实例 ⇒ 等价位在调用方）
//   ④ 誓约闸（`CanPlayWithOathNowInHand.c:25`）
//
// ============================ 判据（⛔ 不是我挑的）============================
//   · `decomp_full/EntityScript__HasCurrentTrait.c:17-33`（正常档：当前表 `+0x128` ∨ `activeEffects` 的 `buffType 3`）
//     / `:54-69`（残骸档：只认 `buffType 90`）
//   · `decomp_full/CardScript__SetBaseCardTraits.c:37-42`（`cardTraits(+0x230)` 整表灌进 `+0x128`）
//   · `decomp_full/CardScript__HasWhileInPlayAbility.c:10-22`（`if (jam == 0) { 查 ability 20/240 }` ⇒ jam **只跳过**）
//   · `decomp_full/RawCardScript__GetAbilitiesForTrigger.c:26-88`（jam ⇒ 跳过「按 trigger 过滤自己的 abilities」那一支）
//   · `decomp_full/CardScript__CanPlayWithOathNowInHand.c:25`（法力比较**之前** `if (jam) return false`）
//   · `decomp_full/CardScript__ReactToUnitJammed.c:96-113`（jam 逐条 `RemoveTrait` **当前表全部**、含卡面那批）
//
// ============================ 灭自证（每条怎么才能红）============================
//   · A 段：把 `HasCurrentTrait` 写成 `Has` 的同义词 ⇒ A1 红；写成恒 true ⇒ A3 红；不做残骸/已摘除两道闸 ⇒ A4/A5 红。
//   · B 段：保留改前的 `if (Has(jam)) return true;` ⇒ B2 红；改成「jam ⇒ 一律 false」⇒ B3 红。
//   · C 段：把 jam 闸挪进 `CardDef.AbilitiesForTrigger`（那层拿不到实例、也 0 生产调用点）⇒ C1/C2 一起绿但 C3 绿不了
//            （C3 断的是「本层与 jam 无关」这一契约 —— 闸装错层会把它弄红）。
//   · D 段：恒 false ⇒ D2 红；恒 true ⇒ D1 红。
//
// ⚠️ 本文件的两张夹具卡直接用 `B94Z2Ctx` / `B94Z2Fixture`（同 `RuleEngineTest` partial 内的既有夹具，
//    `Editor/RuleEngineTest_EngineB94Z2.cs:230-280`）—— 光靠它们就把 `abilityTriggers` / `abilityContext` 接对了。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>`B116_W19` 的唯一入口（宿主 `RuleEngineTest.cs` 同一物理行一个 `Guard(...)`）。</summary>
    public static void CheckB116W19Jam()
    {
        Section("B116·W19 · `ENG-07` `jam`：`cardTraits` 口径 ＋ `HasWhileInPlayAbility` 判反 ＋ 触发闸 ＋ 誓约闸");
        B116W19T1CurrentTrait();
        B116W19T2WhileInPlay();
        B116W19T3AbilitiesForTriggerGate();
        B116W19T4OathGate();
    }

    // ==========================================================================================
    //  夹具
    // ==========================================================================================

    /// <summary>一张「只有 `cardTraits` 列、`keywords` 列为空」的部队卡。</summary>
    static CardDef W19TraitsCard(string id, params string[] cardTraits)
    {
        var c = new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 1, 0, new string[0], subtype: "Infantry");
        c.AttachCardTraits(cardTraits);
        return c;
    }

    /// <summary>一张「平 / 誓二选一」的战术卡（`choicesTargetsAffected 180` + 两个变体 id），
    /// `cardTraits` 里给 `Oath 2`，<paramref name="jam"/> 时再给 `jam`。</summary>
    static CardDef W19OathCard(string id, bool jam)
    {
        var c = new CardDef(id, id, "tactic", "", "common", "Test", 1, 0, 0, 0, new string[0],
                            choicesTargetsAffected: 180, choicesMaxTargets: 2,
                            choicesSpecificCardIds: new[] { "W19OathPay", "W19OathPlain" });
        c.AttachCardTraits(jam ? new[] { "Oath 2", "jam" } : new[] { "Oath 2" });
        return c;
    }

    // ==========================================================================================
    //  ① `cardTraits` 口径：`HasCurrentTrait` = `keywords` ∪ 运行期 ∪ `cardTraits`（原版 `+0x128`）
    // ==========================================================================================

    static void B116W19T1CurrentTrait()
    {
        // ---------------- A1：卡面 `cardTraits` 里写 jam 的卡 —— `Has` 看不见、`HasCurrentTrait` 必须看得见 ----------------
        {
            var card = W19TraitsCard("W19A", "jam");
            var u = new UnitState(card, false);
            CheckTrue(!u.Has("jam"),
                      "前提：`Has` 只读 `keywords` 列 ⇒ 卡面 `cardTraits` 里那条**看不见**（这就是缺口本体，⛔ 不是「断言写错」）");
            CheckTrue(card.HasDefaultTrait("jam", out _), "前提：那张卡的 `cardTraits` 里**真有** `jam`");
            CheckTrue(u.HasCurrentTrait("jam"),
                      "★★ A1：`cardTraits` 列里写的 `jam` ⇒ `HasCurrentTrait` **命中**"
                    + "｜🧨 把 `HasCurrentTrait` 写成 `Has` 的同义词 ⇒ 本行红（原版 `SetBaseCardTraits.c:37-42` 把 `cardTraits` 灌进 `+0x128`）");
        }

        // ---------------- A2：`keywords` 列那一档照旧命中（防「只顾新口径、把老口径丢了」）----------------
        {
            var kw = new CardDef("W19B", "W19B", "unit", "", "common", "Test", 1, 1, 1, 0, new[] { KeywordTable.Jam });
            var u = new UnitState(kw, false);
            CheckTrue(u.Has("jam"), "前提：`keywords` 列里的 `jam` ⇒ `Has` 命中");
            CheckTrue(u.HasCurrentTrait("jam"), "A2：`keywords` 列那一档**照旧**命中（新口是并集、不是替换）");
        }

        // ---------------- A3（灭自证）：没有任何 jam 的卡 ⇒ 必须为假 ----------------
        {
            var u = new UnitState(W19TraitsCard("W19C", "pack"), false);
            CheckTrue(!u.HasCurrentTrait("jam"),
                      "★ A3（灭自证）：卡面 `cardTraits` 里**没有** jam ⇒ `HasCurrentTrait(jam)` 必须**为假**"
                    + "｜🧨 恒 true 的实现过不了这一行");
        }

        // ---------------- A4（灭自证）：残骸档只认 90 档授予（`HasCurrentTrait.c:54-69`）----------------
        {
            var u = new UnitState(W19TraitsCard("W19D", "jam"), false) { IsRemnant = true };
            CheckTrue(u.Card != null && u.Card.HasDefaultTrait("jam", out _), "前提：那张卡的卡面**有** jam（所以下面不是在测「数据没有」）");
            CheckTrue(!u.HasCurrentTrait("jam"),
                      "★ A4（灭自证）：残骸档（`IsRemnant` 且未在离场）⇒ 只认 90 档授予 ⇒ **为假**"
                    + "｜🧨 无条件 `|| Card.HasDefaultTrait(...)` 的实现过不了这一行");
        }

        // ---------------- A5（灭自证）：曾被 `removeTrait` 摘掉 ⇒ 不在当前表 ----------------
        {
            var u = new UnitState(W19TraitsCard("W19E", "jam"), false);
            CheckTrue(u.HasCurrentTrait("jam"), "前提：摘除前**为真**（否则这一行与 A5 不是一对）");
            u.MarkTraitRemoved("jam");
            CheckTrue(!u.HasCurrentTrait("jam"),
                      "★ A5（灭自证）：`MarkTraitRemoved(jam)` 之后 ⇒ **为假**（原版当前表里那条已被 `RemoveTrait` 摘掉）");
        }

        // ---------------- A6：jam 会摘掉**卡面**那批（`ReactToUnitJammed.c:96-113` 逐条 RemoveTrait）----------------
        {
            var card = W19TraitsCard("W19F", "jam", "pack");
            var u = new UnitState(card, false);
            CheckTrue(u.HasCurrentTrait("pack"), "前提：`ApplyJam` 之前卡面的 `pack` 在当前表里（为真）");
            u.ApplyJam();
            CheckTrue(card.HasDefaultTrait("pack", out _), "前提：`ApplyJam` **不改卡面数据**（`cardTraits` 里 pack 还在）");
            CheckTrue(!u.HasCurrentTrait("pack"),
                      "★★ A6：`ApplyJam()` 之后卡面那批**从当前表里摘掉**（原版 `.c:96-113` 对当前表逐条 `RemoveTrait`）"
                    + "｜🧨 不做「卡面表被摘」那一位 ⇒ 本行红（那时会一直读到卡面的 pack）");
            CheckTrue(u.HasCurrentTrait("jam"),
                      "A6-b：同一时刻 `jam` 本身**在**（`.c:118-130` 摘完再 `new CardTraitData(jam)` 挂上）"
                    + "｜🧨 把 `ApplyJam` 的 ④ 删掉 ⇒ 本行红");
        }
    }

    // ==========================================================================================
    //  ② `HasWhileInPlayAbility`：jam **跳过**自己的能力那两查（⛔ 不是「命中就 true」）
    // ==========================================================================================

    static void B116W19T2WhileInPlay()
    {
        // 夹具：带 `abilityTriggers = [20 WhileInPlay]`、正文随便给一条（只用到 presence）
        var wp = B94Z2Fixture("W19WP", 0, new[] { 20 }, new string[0], body: "Strike: Draw 1 card");
        Check(wp.AbilityTriggerPresence(20), 1, "前提：夹具卡**真有** `trigger 20`（`while-in-play`）");
        if (wp.AbilityTriggerPresence(20) != 1) return;   // 前提已红 ⇒ 后面三条无从谈起（失败已记在上面）

        {
            var u = new UnitState(wp, false);
            CheckTrue(u.HasWhileInPlayAbility(out _),
                      "★ B1：未 jam ⇒ `while-in-play` 能力在 ⇒ **true**（走 ② `AbilityTriggerPresence(20)` 那一支）");
        }
        {
            var u = new UnitState(wp, false);
            u.ApplyJam();
            CheckTrue(u.HasCurrentTrait("jam"), "前提：`ApplyJam()` 成功挂上 jam");
            CheckTrue(!u.HasWhileInPlayAbility(out _),
                      "★★★ B2（本件最要紧的一条）：**被 jam ⇒ false** —— 原版那一句是 `if (jam == 0) { 查 20/240 }`"
                    + "（`CardScript__HasWhileInPlayAbility.c:10-22`）⇒ jam **只跳过**这两查、继续走 ④⑤⑥ 全不成立 ⇒ false"
                    + "｜🧨 改前的 `if (Has(jam)) return true;` ⇒ 本行红（这就是「判反」）");
        }
        {
            var u = new UnitState(wp, false);
            u.ApplyJam();
            u.AddKeyword("pack", 1);   // ⑤ `HasCurrentTrait(pack)` —— jam 之后**运行时**挂上的仍然算
            CheckTrue(u.HasWhileInPlayAbility(out _),
                      "★ B3（灭自证 · 反向）：jam 之后挂上 `pack` ⇒ 走 ⑤ ⇒ **必须仍为 true**"
                    + "｜🧨 「jam ⇒ 一律 return false」这种过度修正过不了这一行");
        }
        {
            var u = new UnitState(B94Z2Filler("W19None"), false);
            CheckTrue(!u.HasWhileInPlayAbility(out _),
                      "B4（灭自证）：既没 jam、也没任何能力/附魔/pack ⇒ **false**（防「恒 true」）");
        }
    }

    // ==========================================================================================
    //  ③ `GetAbilitiesForTrigger` 的 jam 闸 —— 本层是纯 `CardDef`（拿不到实例），
    //     等价位在调用方 `TriggerAbilitiesPassGates` ①（`EffectResolver.cs:18595`）。
    // ==========================================================================================

    static void B116W19T3AbilitiesForTriggerGate()
    {
        // 🔴 2026-11-02（`B116_W45`）夹具订正：`B94Z2Fixture` 默认造的是**全 null 占位的**
        //   `abilityTargets` ⇒ `AbilitiesForTrigger(580)` 恒空表 ⇒ C3 变成「0 == 0」的空断言。
        //   这里显式喂一行 `Trigger = 580`（与真卡池同形：池内 1,273 行里 null 行 = 0）。
        var card = B94Z2Fixture("W19T3", 0, new[] { 580 }, new string[0], body: "Strike: Draw 1 card",
                                abilityTargets: new[]
                                {
                                    new CardDef.AbilityTargetSpec { Trigger = 580 },
                                });
        var ctx = B94Z2Ctx(0xB1163);
        ctx.Players[0].Deck.Add(ctx.NewInstance(B94Z2Filler("W19T3Deck")));
        var u = new UnitState(card, false);
        ctx.Players[0].Board[0] = u;

        // C3 先断契约：这一层**不含** jam 闸（jam 前后 `AbilitiesForTrigger` 逐值相同）——
        //   闸装错层（塞进 `CardDef.AbilitiesForTrigger`）时本行会红。
        var before = card.AbilitiesForTrigger(580);
        CheckTrue(before != null && before.Count == 1, "前提：`AbilitiesForTrigger(580)` 非空且只有 1 条（本层是 `CardDef` 的纯函数）");

        // C1：未 jam ⇒ 自己那一趟**有东西可跑**（`FireTriggerAt` 入队成功）
        bool ranClean = RuleCore.FireTriggerAtForTest(ctx, u, KeywordTable.Strike, 0, 0);
        CheckTrue(ranClean,
                  "★ C1：未 jam ⇒ `Strike` 自己那一趟**入队**（`FireTriggerAt` 返回 true；照 `EngineB94Z2.cs:118-128` 的同形夹具）"
                + "｜🧨 拿不到 `Card.TriggerOps` ⇒ 本行红（那是夹具问题，不是被测逻辑）");

        // C2：jam ⇒ `TriggerAbilitiesPassGates` ① 把 `ops/spec/skipDraw` 清空 ⇒ 没有东西可跑 ⇒ false
        u.ApplyJam();
        var after = u.Card.AbilitiesForTrigger(580);
        Check(after != null ? after.Count : -1, before != null ? before.Count : -2,
              "★ C3：jam **不改变** `AbilitiesForTrigger`（原版 `GetAbilitiesForTrigger.c:26` 的闸在**调用方**、不在这一层）"
            + "｜🧨 把闸塞进 `CardDef.AbilitiesForTrigger` ⇒ 本行红");
        CheckTrue(!RuleCore.FireTriggerAtForTest(ctx, u, KeywordTable.Strike, 0, 0),
                  "★★ C2：被 jam ⇒ `Strike` 自己那一趟**整趟不跑**（`TriggerAbilitiesPassGates` ① 清空 ops ⇒ `FireTriggerAt` 返回 false）"
                + "｜🧨 删掉 `EffectResolver.cs:18595` 那道 jam 闸 ⇒ 本行红"
                + "；⚠️ 本层（`CardDef.AbilitiesForTrigger`）**生产调用点 = 0**，所以这条闸今天只能从调用方验（施工面 §1·2③）");
    }

    // ==========================================================================================
    //  ④ 誓约闸：`CardScript__CanPlayWithOathNowInHand.c:25` 在法力比较**之前** `if (jam) return false`
    // ==========================================================================================

    static void B116W19T4OathGate()
    {
        // ---------------- D1：带 jam 的「平 / 誓」卡 ⇒ AI **不问池**、一格随机都不消耗 ----------------
        {
            var card = W19OathCard("W19OathJam", jam: true);
            CheckTrue(RuleCore.OathChoiceNeeded(card),
                      "前提：夹具是「平 / 誓二选一」那种战术卡（`choicesTargetsAffected 180` + 2 个变体 id + `Oath` trait）");
            CheckTrue(card.HasDefaultTrait(KeywordTable.Jam, out _), "前提：夹具的 `cardTraits` 里**真有** jam");
            if (!RuleCore.OathChoiceNeeded(card)) return;

            var ctx = B94Z2Ctx(0xB11641);
            ctx.Players[0].Energy = 99;
            var inst = ctx.NewInstance(card);
            ctx.Players[0].Hand.Add(inst);
            var a = new AiAction { Kind = AiActionKind.PlayCard, HandIdx = 0, Slot = 0 };
            int idx0 = ctx.RandomNumbersIndex;

            CheckTrue(!RuleCore.NeedsToChooseFromPool(ctx, 0, card, null, inst: inst),
                      "★ D0：带 jam 的誓约卡 ⇒ 原版那道池闸判否（`NeedsToChooseFromPool` ⑤，`CardScript__NeedsToChooseFromPool.c`）");
            bool rolled = SimpleAI.DecideOathForAi(ctx, 0, a, inst);
            CheckTrue(!rolled,
                      "★★ D1：AI 这一手**不掷、不摆答案**（`DecideOathForAi` 返回 false）"
                    + "｜🧨 去掉 jam 闸 ⇒ 本行红");
            Check(ctx.RandomNumbersIndex, idx0,
                  "★★ D1-b：**一格预烧随机数都不消耗**（原版这时根本不问池）｜🧨 闸漏 ⇒ 这里会 +1");
        }

        // ---------------- D2（灭自证）：同一夹具**去掉 jam** ⇒ 必须照常问池、净消耗 1 格 ----------------
        {
            var card = W19OathCard("W19OathNoJam", jam: false);
            if (!RuleCore.OathChoiceNeeded(card)) { CheckTrue(false, "前提：对照组也是「平 / 誓」卡"); return; }

            var ctx = B94Z2Ctx(0xB11642);
            ctx.Players[0].Energy = 99;
            var inst = ctx.NewInstance(card);
            ctx.Players[0].Hand.Add(inst);
            var a = new AiAction { Kind = AiActionKind.PlayCard, HandIdx = 0, Slot = 0 };
            int idx0 = ctx.RandomNumbersIndex;

            bool rolled = SimpleAI.DecideOathForAi(ctx, 0, a, inst);
            CheckTrue(rolled,
                      "★★ D2（灭自证）：同一张卡**去掉 jam** ⇒ AI **会**问池（`DecideOathForAi` 返回 true）"
                    + "｜🧨 「恒 false」的实现过不了这一行");
            Check(ctx.RandomNumbersIndex - idx0, 1,
                  "★ D2-b（灭自证）：净消耗**正好 1 格**（池 2 项 + `maxTargets 2`，见 `SimpleAIPartial_B111E2.cs` 头注）");
        }
    }
}
