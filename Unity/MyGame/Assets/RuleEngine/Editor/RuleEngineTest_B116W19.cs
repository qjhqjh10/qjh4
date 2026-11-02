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
        B117W1CardTraitsOnly();   // 🆕 2026-11-02（`B117_W1` · 活状态表 `B117-01`）：两条只写在 `cardTraits` 列的 trait
        B117F1TraitConsumers();   // 🆕 2026-11-02（`B117_F1` · 活状态表 `B117-21/22/23/24`）：同病灶的其余消费端
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

    // ==========================================================================================
    //  ⑤ 🆕 2026-11-02（`B117_W1` · 活状态表 `B117-01`）
    //     两条 trait **只写在 `cardTraits` 列、卡面不印** ⇒ 只读 `keywords` 的判据**静默判不到**。
    //
    //  判据：原版 `MeetsTraitCriteria` 读的是**当前 trait 表** `+0x128`
    //    （`FilterMethods__MeetsTraitCriteria.c:30-37` 的 `*(param_1 + 0x128)`），而
    //    `CardScript__SetBaseCardTraits.c:37-42` 把 **`cardTraits(+0x230)` 整表**灌进 `+0x128`
    //    ⇒ 「当前 trait 表」= `keywords` ∪ 运行期授予 ∪ **`cardTraits`**。
    //  受害者（逐张核过卡面，且 `cards_engine.json` 现扫全池只有这两张差）：
    //    · `SAU7 Canoptek Wraith`：`keywords=["Remnant"]` / `cardTraits=["Unstoppable","Remnant"]`
    //      （全池 Unstoppable 在 `keywords` **0 张**、在 `cardTraits` **1 张**）
    //    · `Daemonbreaker`（`e01f4d54e8a474d96b5a1167cc28eebf`）：`keywords=[]` / `cardTraits=["Ephemeral"]`
    //      （Ephemeral 在 `keywords` **103 张**、在 `cardTraits` **104 张**，差的就是它）
    //
    //  灭自证（每条怎么才能红）：
    //    · ①-c：把 `ConditionEval` 那三处 `HasCurrentTrait` 改回 `Has` ⇒ 红
    //    · ②-b：把 `BattleContext.IsEphemeral` 里的 `HasTrait("ephemeral")` 去掉 ⇒ 红
    //    · ③：恒 true 的实现过不了（两个表都没有的卡必须为假）
    //    · ④：把并集写成 `Has(x) || Card.HasTrait(x)`（绕过残骸档）⇒ 红（原版 `TransformIntoRemnant.c:54-75`
    //      翻面那一刻把当前表逐条摘光）
    // ==========================================================================================

    static void B117W1CardTraitsOnly()
    {
        var pool = CardDatabase.Load();

        // ---------------- ① `SAU7` 的 Unstoppable：只在 `cardTraits` 列 ⇒ 条件 50 必须判真 ----------------
        {
            var card = CardDatabase.FindById(pool, "SAU7");
            CheckTrue(card != null, "前提：`SAU7 Canoptek Wraith` 在卡池里");
            if (card != null)
            {
                CheckTrue(!card.Has(RuleCore.Unstoppable),
                          "前提：`SAU7` 的 `keywords` 列里**没有** unstoppable（缺口本体：`Has` 一个读点都发现不了）");
                CheckTrue(card.HasTrait(RuleCore.Unstoppable),
                          "★★ ①：`SAU7` 的 `cardTraits` 列里**有** `Unstoppable` ⇒ `HasTrait` 为真"
                        + "｜🧨 把 `CardDef.HasTrait` 删掉 / 改成只读 `keywords` ⇒ 本行红");

                var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
                var u = Place(ctx, 0, 0, card);
                CheckTrue(u.HasCurrentTrait(RuleCore.Unstoppable),
                          "①-b：场上单位走 `HasCurrentTrait`（原版「当前 trait 表」`+0x128`）也判得到");
                CheckTrue(RuleCore.MeetsUnitCondition(ctx, u, 50, false) == true,
                          "★★★ ①-c（消费端 · 本件主诉）：条件 **50 `flying`** 对 `SAU7` **为真**"
                        + "（原版 `MeetsConditionCriteria.c:38-45` 的 `MeetsTraitCriteria(180 unstoppable)`）"
                        + "｜🧨 把 `ConditionEval` 那三处 `HasCurrentTrait` 改回 `Has` ⇒ 本行红");
            }
        }

        // ---------------- ④（灭自证 · 残骸档）：同一张卡翻成残骸 ⇒ 必须为假 ----------------
        {
            var card = CardDatabase.FindById(pool, "SAU7");
            if (card != null)
            {
                var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
                var u = Place(ctx, 0, 1, card);
                CheckTrue(RuleCore.MeetsUnitCondition(ctx, u, 50, false) == true, "④ 前提：活着的时候为真（否则本行与下面不是一对）");
                u.IsRemnant = true;
                CheckTrue(RuleCore.MeetsUnitCondition(ctx, u, 50, false) == false,
                          "★★ ④（灭自证）：翻成**残骸** ⇒ 条件 50 必须**为假**"
                        + "（原版 `CardScript__TransformIntoRemnant.c:54-75` 翻面那一刻逐条 `RemoveTraitSilently` ⇒ 当前表里一条不剩；"
                        + "`EntityScript__HasCurrentTrait.c:54-69` 残骸档也只认 90 档授予）"
                        + "｜🧨 把并集写成 `u.Has(x) || u.Card.HasTrait(x)`（绕过残骸档）⇒ 本行红");
            }
        }

        // ---------------- ② `Daemonbreaker` 的 Ephemeral：只在 `cardTraits` 列 ⇒ `IsEphemeral` 必须判真 ----------------
        {
            var card = CardDatabase.FindById(pool, "e01f4d54e8a474d96b5a1167cc28eebf");
            CheckTrue(card != null, "前提：`Daemonbreaker`（`e01f4d54…`）在卡池里");
            if (card != null)
            {
                CheckTrue(!card.Has("ephemeral"), "前提：它的 `keywords` 列是**空表** ⇒ `Has` 判不到（缺口本体）");
                CheckTrue(card.HasTrait("ephemeral"), "★★ ②：`cardTraits` 列里有 `Ephemeral` ⇒ `HasTrait` 为真");

                var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
                var inst = ctx.NewInstance(card);
                CheckTrue(!inst.EphemeralMarked, "前提：这一份**没有**被打上临时标记（所以下面不是在测 `EphemeralMarked` 那一位）");
                CheckTrue(ctx.IsEphemeral(inst),
                          "★★★ ②-b（消费端）：`IsEphemeral` **为真** ⇒ 回合末 `SweepEphemeral` 会把它移出"
                        + "｜🧨 把 `BattleContext.IsEphemeral` 里的 `HasTrait(\"ephemeral\")` 去掉 ⇒ 本行红");
            }
        }

        // ---------------- ③（灭自证 · 反证）：两张表都没有 ephemeral 的卡 ⇒ 必须为假 ----------------
        {
            var card = CardDatabase.FindById(pool, "ASH83");
            CheckTrue(card != null, "前提：`ASH83 Autarch` 在卡池里");
            if (card != null)
            {
                CheckTrue(!card.Has("ephemeral") && !card.HasTrait("ephemeral"),
                          "前提：`ASH83` 的 `keywords` 与 `cardTraits` 两张表里**都没有** ephemeral"
                        + "（现读 = `keywords=[Waystone]` / `cardTraits=[Waystone]`）");
                var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
                var inst = ctx.NewInstance(card);
                CheckTrue(!ctx.IsEphemeral(inst),
                          "★★ ③（灭自证）：两张表都没有 ⇒ `IsEphemeral` **为假**｜🧨 把 `HasTrait` 写成恒真 ⇒ 本行红");
            }
        }
    }

    // ==========================================================================================
    //  🆕 2026-11-02（执行代理 `B117_F1` · 活状态表 `B117-21` / `B117-22` / `B117-23` / `B117-24`）
    //
    //  同一条病灶的其余消费端：原版读的是**当前 trait 表** `+0x128`（`keywords` ∪ 运行期 ∪ `cardTraits`），
    //  我们一批读点只走 `Has()`（`keywords` 列）⇒ 只写在 `cardTraits` 列的 trait 静默判不到。
    //  判据 = `FilterMethods__MeetsTraitCriteria.c:30-47`（读 `+0x128`）· `CardScript__SetBaseCardTraits.c:37-42`
    //        （`cardTraits` 整表灌进 `+0x128`）· `CardScript__get_mightAct.c`（五个词全走 `HasCurrentTrait`）·
    //        `BattleManager__IsThereDoubleOathEffect.c:33`（`HasCurrentTrait(card, 0x4fd = oathDouble)`）。
    //
    //  ⚠️ 本段全部按「先 `Check(前提)` 再 `Check(结论)`」写，⛔ 没有 `if (前提) Check(结论)` 那种静默跳过。
    // ==========================================================================================
    static void B117F1TraitConsumers()
    {
        var pool = CardDatabase.Load();

        // ---------------- ① `B117-21`：`GOF57 Ork Nob` 的 `Flank` 只在 `cardTraits` 列 ⇒ 部署当回合能攻击 ----------------
        {
            var card = CardDatabase.FindById(pool, "GOF57");
            CheckTrue(card != null, "前提：`GOF57 Ork Nob` 在卡池里");
            if (card == null) { CheckTrue(false, "① 结论无法判定：前提不成立（⛔ 不是跳过）"); }
            else
            {
                CheckTrue(!card.Has("flank"),
                          "前提：`GOF57` 的 `keywords` 是**空表** ⇒ `Has(\"flank\")` 判不到（缺口本体）");
                CheckTrue(card.HasTrait("flank"),
                          "★★ ①-a：`GOF57` 的 `cardTraits` 列里有 `Flank` ⇒ `HasTrait` 为真");

                var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
                var u = Place(ctx, 0, 0, card);
                u.SummonSickness = true;                      // 夹具：部署当回合 = 带召唤病
                CheckTrue(u.SummonSickness, "前提：夹具单位带召唤病（否则下面那几条不成立）");
                CheckTrue(u.AttacksThisTurn == 0, "前提：本回合还没攻击过");
                CheckTrue(u.HasCurrentTrait("flank"),
                          "①-b：场上单位走 `HasCurrentTrait` 判得到 `flank`（原版读当前 trait 表 `+0x128`）");

                CheckTrue(RuleCore.SummonSicknessExempt(u),
                          "★★ ①-c：`SummonSicknessExempt` 为**真**（= 免掉召唤病）"
                        + "｜🧨 把 `RuleCore.SummonSicknessExempt` 改回 `u.Has(\"flank\")` ⇒ 本行红");
                CheckTrue(!RuleCore.DisplaySummonSickness(u),
                          "①-d：`displaySummonSickness` 为**假**（原版 `EntityScript__get_displaySummonSickness.c:7-11`）");
                CheckTrue(RuleCore.HasDeployExemption(u),
                          "①-e：`HasDeployExemption` 为**真**（部署当回合不疲劳）");

                // 消费端 · `B117-21` 的主诉：条件 20 `mightAttack`
                CheckTrue(RuleCore.MeetsUnitCondition(ctx, u, 20, false) == true,
                          "★★★ ①-f（消费端 · 本件主诉）：条件 **20 `mightAttack`** 对带召唤病的 `GOF57` **为真**"
                        + "（原版 `CardScript__get_mightAct.c` 的 `displaySummonSickness` 支 = `!HasCurrentTrait(0x28) && !HasCurrentTrait(0x1cc)`）"
                        + "｜🧨 把 `ConditionEval` 条件 20 的 `HasCurrentTrait` / `RuleCore.DisplaySummonSickness` 改回 `Has` ⇒ 本行红");
            }
        }

        // ---------------- ①'（灭自证 · 反证）：没有 flank 的单位带召唤病 ⇒ 条件 20 必须为假 ----------------
        {
            var card = CardDatabase.FindById(pool, "SAU7");
            CheckTrue(card != null, "前提：`SAU7 Canoptek Wraith` 在卡池里（作反证用）");
            if (card == null) { CheckTrue(false, "①' 结论无法判定：前提不成立（⛔ 不是跳过）"); }
            else
            {
                CheckTrue(!card.Has("flank") && !card.HasTrait("flank"),
                          "前提：`SAU7` 两张表里都**没有** flank");
                var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
                var u = Place(ctx, 0, 0, card);
                u.SummonSickness = true;
                CheckTrue(!RuleCore.SummonSicknessExempt(u),
                          "★★ ①'-a（灭自证）：没有 flank/fast ⇒ `SummonSicknessExempt` **为假**"
                        + "｜🧨 把 `HasCurrentTrait` 写成恒真 ⇒ 本行红");
                CheckTrue(RuleCore.MeetsUnitCondition(ctx, u, 20, false) == false,
                          "★ ①'-b（灭自证）：条件 20 对带召唤病的 `SAU7` **为假**（原版同形）");
            }
        }

        // ---------------- ② `B117-22`：`Achievement / Ach_12 Play Card With Ephemeral` 认 `cardTraits` ----------------
        {
            var daemon = CardDatabase.FindById(pool, "e01f4d54e8a474d96b5a1167cc28eebf");
            var plain = CardDatabase.FindById(pool, "ASH83");
            CheckTrue(daemon != null && plain != null, "前提：`Daemonbreaker` 与 `ASH83` 都在卡池里");
            // `RuleCore.CardPlayedTagMask` 是 `internal`，本仓没有 `InternalsVisibleTo` ⇒ 只能反射调。
            var mi = typeof(RuleCore).GetMethod("CardPlayedTagMask",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            CheckTrue(mi != null, "前提：`RuleCore.CardPlayedTagMask(CardDef, CardInstance)` 存在（`internal` ⇒ 反射调）");
            if (daemon == null || plain == null || mi == null)
            {
                CheckTrue(false, "② 结论无法判定：前提不成立（⛔ 不是跳过）");
            }
            else
            {
                CheckTrue(!daemon.Has("ephemeral"), "前提：`Daemonbreaker` 的 `keywords` 是空表 ⇒ 旧写法判不到");
                CheckTrue(daemon.HasTrait("ephemeral"), "前提：它的 `cardTraits` 列里有 `Ephemeral`");
                int m1 = (int)mi.Invoke(null, new object[] { daemon, null });
                CheckTrue((m1 & (1 << CardPlayedTag.Ephemeral)) != 0,
                          "★★ ②-a（消费端）：`CardPlayedTagMask(Daemonbreaker)` 带 **`Ephemeral` 位**"
                        + "（原版 `Ach_12` 的 `withTrait=[5]` = `DefinedTrait.ephemeral` 读当前 trait 表）"
                        + "｜🧨 把 `RuleCore.CardPlayedTagMask` 里的 `HasTrait(\"ephemeral\")` 去掉 ⇒ 本行红");
                int m2 = (int)mi.Invoke(null, new object[] { plain, null });
                CheckTrue((m2 & (1 << CardPlayedTag.Ephemeral)) == 0,
                          "★★ ②-b（灭自证）：`ASH83`（两张表都没有 ephemeral）**不带** `Ephemeral` 位｜🧨 恒置位 ⇒ 本行红");
            }
        }

        // ---------------- ③ `B117-23`：`UM100 Chaplain Cassius` 的 `oathDouble` 让誓约多结算一次 ----------------
        {
            var cassius = CardDatabase.FindById(pool, "UM100");
            var oathUnit = CardDatabase.FindById(pool, "UM101");   // `cardTraits=["oath 2"]` —— 前缀归一化会与 `oathDouble` 撞名
            CheckTrue(cassius != null && oathUnit != null, "前提：`UM100 Chaplain Cassius` 与 `UM101 1st Company Terminator` 都在卡池里");
            if (cassius == null || oathUnit == null)
            {
                CheckTrue(false, "③ 结论无法判定：前提不成立（⛔ 不是跳过）");
            }
            else
            {
                CheckTrue(!cassius.Has("oathdouble"),
                          "前提：`UM100` 的 `keywords` 里**没有** oathdouble（只有 `Talent: Catechism of Death`）");
                var ct = cassius.CardTraits;
                CheckTrue(ct != null, "前提：`UM100` 的 `cardTraits` 列**已喂引擎**（`CardTraitsKnown`）");
                bool rawHit = false;
                if (ct != null)
                    for (int i = 0; i < ct.Count; i++)
                        if (ct[i].Raw != null && ct[i].Raw.Trim().Replace(" ", "").ToLowerInvariant().StartsWith("oathdouble")) rawHit = true;
                CheckTrue(ct != null && rawHit,
                          "★★ ③-a：`UM100` 的 `cardTraits` 列里有 `oathDouble`（原版 `DefinedTrait.oathDouble = 1277 = 0x4fd`；这里是按 `CardTraitEntry.Raw` 精确判的**前提**断言 —— 本行不验归一化，归一化由下面 ⑤ 段单独验）");
                CheckTrue(cassius.OathDouble,
                          "前提：`UM100` 的正文**会**解析出 `CardDef.OathDouble = true`（desc = `Friendly Oath abilities apply an additional time.` ⇒ `MatchStaticBattleRule` 的「誓约加倍」）"
                        + "—— 旧实现**不是**「恒 false」：真正的缺口是它读的是【被激活的那张 oath 卡】而不是【来源卡】（旧 `RuleCore.OathExtraReplays` 读 `u.Card.OathDouble`，`u` 是被激活的 `UM101`，不是 `UM100`）");
                CheckTrue(oathUnit.HasTrait("oath"),
                          "前提：`UM101`（`cardTraits=[\"oath 2\"]`）确实是「带 oath 的卡」—— 用来验前缀撞名那一档");

                var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
                CheckTrue(RuleCore.OathExtraReplays(ctx, 0) == 0,
                          "前提：场上没有 `UM100` 时 `OathExtraReplays == 0`");
                Place(ctx, 0, 0, cassius);
                CheckTrue(RuleCore.OathExtraReplays(ctx, 0) == 1,
                          "★★★ ③-b（消费端 · 本件主诉）：`UM100` 在场 ⇒ `OathExtraReplays == 1`（誓约能力多结算一次）"
                        + "（原版 `BattleManager__IsThereDoubleOathEffect.c:33` 逐张 `EntityScript__HasCurrentTrait(card, 0x4fd)` 计数）"
                        + "｜🧨 把 `RuleCore.OathExtraReplays` 改回 `u.Card.OathDouble` ⇒ 本行红");
                CheckTrue(RuleCore.OathExtraReplays(ctx, 1) == 0,
                          "★ ③-c（灭自证 · 分侧）：对手侧（`p=1`）**不受**我方 `UM100` 影响 ⇒ 仍为 0");

                // 灭自证 · 前缀撞名：换成一张普通的 `oath 2` 单位 ⇒ 必须**不**算加倍源
                var ctx2 = ProbeBattle(new CardDef[0], new CardDef[0]);
                Place(ctx2, 0, 0, oathUnit);
                CheckTrue(RuleCore.OathExtraReplays(ctx2, 0) == 0,
                          "★★ ③-d（灭自证 · 前缀撞名）：`UM101`（`cardTraits=[\"oath 2\"]`）**不算**加倍源"
                        + "（`B117-26` 之前两者归一化后同名、本行只能靠 `Raw` 精确比才绿；根治之后 `oath 2` → `oath`、"
                        + "`oathDouble` → `oathdouble` 各归各名，本行断的是「不许把 oath 卡算成加倍源」这条语义本身）");
            }

        // ---------------- ⑤ `B117-26`：`oathDouble` 归一化撞车的**根治**（`KeywordTable.Prefixes`） ----------------
        //  病灶：`Prefixes` 原来只有 `"oath"`，而 `Normalize` 是 `StartsWith` + **先到先得** ⇒ `oathDouble`
        //  被折成 `oath` ⇒ 全池 28 张 `cardTraits=["oath N"]` 与 `UM100` 的 `oathDouble` **归一化后同名**
        //  （原版是两条独立的闸：`HasCurrentTrait(0x4fb = oath)` / `HasCurrentTrait(0x4fd = oathDouble)`）。
        //  根治 = 往 `Prefixes` 补 `oath` 三个变体、**排在 `"oath"` 之前**（同 `wardwb` 必须排 `ward` 前那条先例）。
        {
            var cassius26 = CardDatabase.FindById(pool, "UM100");
            var oathOnly = CardDatabase.FindById(pool, "UM101");   // `cardTraits=["oath 2"]`
            CheckTrue(cassius26 != null && oathOnly != null, "前提：`UM100` 与 `UM101` 都在卡池里");
            if (cassius26 == null || oathOnly == null)
            {
                CheckTrue(false, "⑤ 结论无法判定：前提不成立（⛔ 不是跳过）");
            }
            else
            {
                CheckTrue(KeywordTable.Normalize("oathDouble") == "oathdouble",
                          "★★ ⑤-a（本件主诉 · 归一化）：`Normalize(\"oathDouble\")` == `\"oathdouble\"`"
                        + "（`Prefixes` 那三条 oath 变体**排在 `\"oath\"` 之前** —— 排在后面等于没写，先到先得）"
                        + "｜🧨 把 `oathdouble` 那条从 `Prefixes` 删掉 / 挪到 `\"oath\"` 之后 ⇒ 本行红");
                CheckTrue(KeywordTable.Normalize("oath 2") == "oath",
                          "★ ⑤-b（反证 · 老词不许改坏）：`Normalize(\"oath 2\")` 仍 == `\"oath\"`");
                CheckTrue(cassius26.HasTrait("oathdouble") && !cassius26.HasTrait("oath"),
                          "★★ ⑤-c（结论 · 卡这一侧）：`UM100` 的 `HasTrait(\"oathdouble\")` 为**真**、`HasTrait(\"oath\")` 为**假**"
                        + "（根治前两者**同时为真** —— 那就是本件的假阳性）");
                CheckTrue(oathOnly.HasTrait("oath") && !oathOnly.HasTrait("oathdouble"),
                          "★★ ⑤-d（反证 · 不许反向污染）：`UM101`（`cardTraits=[\"oath 2\"]`）`HasTrait(\"oath\")` 真、"
                        + "`HasTrait(\"oathdouble\")` **假**");

                var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
                var u100 = Place(ctx, 0, 0, cassius26);
                var u101 = Place(ctx, 0, 1, oathOnly);
                CheckTrue(u100.HasCurrentTrait("oathdouble") && !u100.HasCurrentTrait("oath"),
                          "★★ ⑤-e（结论 · 场上单位）：`UM100` 在场时 `HasCurrentTrait(\"oathdouble\")` 真、`HasCurrentTrait(\"oath\")` 假"
                        + "｜🧨 前缀表回退 ⇒ 两个都为真 ⇒ 红");
                CheckTrue(u101.HasCurrentTrait("oath") && !u101.HasCurrentTrait("oathdouble"),
                          "★★ ⑤-f（反证）：`UM101` 反向 —— `oath` 真、`oathdouble` 假（它是全池 28 张 `oath N` 的代表）");
                CheckTrue(RuleCore.HasDeployExemption(u101) && !RuleCore.HasDeployExemption(u100),
                          "★★ ⑤-g（消费端 · `RuleCore.HasDeployExemption` 的 `oath` 半格）：带 `oath` 的 `UM101` 有部署豁免、"
                        + "`oathDouble` 的 `UM100` **没有**（原版 `CardScript__ActivateTraitsOnSummonOrEnchantment.c:86-99` 读 `HasCurrentTrait(0x4fb)`）"
                        + "｜🧨 前缀表回退 ⇒ `UM100` 也变成有豁免 ⇒ 红");
                CheckTrue(RuleCore.OathExtraReplays(ctx, 0) == 1,
                          "★ ⑤-h（`HasOathDouble` 简化之后仍成立）：同侧摆了 `UM100` + `UM101` 两张 ⇒ `OathExtraReplays == 1`"
                        + "（只有 `UM100` 算加倍源；`UM101` 被算进来就会是 2）");
            }
        }
        }

        // ---------------- ④ `B117-24`：`MeetsTraitCriteria` 一族的其余读点（夹具卡只有 `cardTraits`） ----------------
        {
            var stealther = W19TraitsCard("F1_StealthOnly", "Stealth");
            var dodger = W19TraitsCard("F1_DodgeOnly", "Dodge");
            var ferocious = W19TraitsCard("F1_FerocityOnly", "ferocity");
            var plain = W19TraitsCard("F1_Plain", "Remnant");

            CheckTrue(!stealther.Has("stealth") && stealther.HasTrait("stealth"),
                      "前提：夹具卡 `F1_StealthOnly` 只在 `cardTraits` 列有 `Stealth`");
            CheckTrue(!dodger.Has("dodge") && dodger.HasTrait("dodge"),
                      "前提：夹具卡 `F1_DodgeOnly` 只在 `cardTraits` 列有 `Dodge`");
            CheckTrue(!ferocious.Has("ferocity") && ferocious.HasTrait("ferocity"),
                      "前提：夹具卡 `F1_FerocityOnly` 只在 `cardTraits` 列有 `ferocity`");

            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            var uS = Place(ctx, 0, 0, stealther);
            var uD = Place(ctx, 0, 1, dodger);
            var uF = Place(ctx, 0, 2, ferocious);
            var uP = Place(ctx, 0, 3, plain);

            CheckTrue(RuleCore.MeetsUnitCondition(ctx, uS, 330, false) == true,
                      "★★ ④-a：条件 **330 `stealthOrWardOrDuplicitous`** 认 `cardTraits` 里的 stealth"
                    + "｜🧨 把那一行改回 `u.Has(...)` ⇒ 本行红");
            CheckTrue(RuleCore.MeetsUnitCondition(ctx, uS, 230, false) == true,
                      "★★ ④-b：条件 **230 `stealthAndNotDealtKillDmg`** 认 `cardTraits` 里的 stealth"
                    + "｜🧨 改回 `u.Has(...)` ⇒ 本行红");
            CheckTrue(RuleCore.MeetsUnitCondition(ctx, uS, 331, false) == true,
                      "★★ ④-c：条件 **331 `stealthOrCamouflage`** 认 `cardTraits` 里的 stealth｜🧨 改回 `u.Has(...)` ⇒ 本行红");
            CheckTrue(RuleCore.MeetsUnitCondition(ctx, uD, 100, false) == true,
                      "★★ ④-d：条件 **100 `protectedFromDamage`** 认 `cardTraits` 里的 dodge（`MeetsTraitCriteriaFromList`）"
                    + "｜🧨 改回 `u.Has(...)` ⇒ 本行红");
            CheckTrue(RuleCore.MeetsUnitCondition(ctx, uD, 120, false) == true,
                      "★★ ④-e：条件 **120 `protectedFromDamageOrSurvivor`** 认 `cardTraits` 里的 dodge｜🧨 改回 `u.Has(...)` ⇒ 本行红");
            CheckTrue(RuleCore.HasDeployExemption(uF) && RuleCore.MeetsUnitCondition(ctx, uF, 20, false) == true,
                      "★★ ④-f：`ferocity` 只在 `cardTraits` ⇒ `HasDeployExemption` 与条件 20 都为真"
                    + "（原版 `CardScript__ActivateTraitsOnSummonOrEnchantment.c:86-99` 读 `HasCurrentTrait`）"
                    + "｜🧨 把 `HasDeployExemption` 里的 `HasCurrentTrait` 改回 `Has` ⇒ 本行红");
            CheckTrue(RuleCore.MeetsUnitCondition(ctx, uP, 330, false) == false
                      && RuleCore.MeetsUnitCondition(ctx, uP, 230, false) == false
                      && RuleCore.MeetsUnitCondition(ctx, uP, 100, false) == false,
                      "★★ ④-g（灭自证 · 反证）：`F1_Plain`（只有 `Remnant`）三条全**假**｜🧨 写成恒真 ⇒ 本行红");
        }

        // ---------------- ⑥ `B117-45`：`RuleEngine/Core/EffectResolverPartial_B88E2.cs` 的两处 trait 读点 ----------------
        //  ⑥-A `CanPlayWithOathNowInPlay` 的 jam 闸（该文件 `:205`）——
        //      原版 `CardScript__CanPlayWithOathNowInPlay.c:22` = `EntityScript__HasCurrentTrait(param_1, 0x82 /*jam*/)`；
        //      改前写成 `thisCard.Has(...)`（只读 `keywords` 列）⇒ 只写在 `cardTraits` 里的 jam 会被漏判。
        //  ⑥-B `RunPlayChosenCardAbility` 的誓约闸（同文件 `:992`）——
        //      原版 `AbilityLogic__PlayAbility.c:2025` = `RawCardScript__HasDefaultTrait(thatCard, 0x4fb /*oath*/)`；
        //      改前写成 `cc.Has(...)`（只读 `keywords` 列）。⚠️ 这一句原版读的是**卡面默认 trait**
        //      （`defaultTraits(+0x230)`）⇒ 等价位 = `CardDef.HasDefaultTrait`，⛔ **不是 `HasCurrentTrait`**
        //      （`cc` 是 `CardDef`、选中的那张可能根本不在场；判据 → `B117_F13` 报告 §二）。
        {
            // ---- ⑥-A：jam 只写在 `cardTraits` ⇒ 誓约闸必须把它拦下 ----
            var jamOnly = W19TraitsCard("F13_JamOnly", "oath 1", "jam");
            var noJam = W19TraitsCard("F13_NoJam", "oath 1");
            CheckTrue(!jamOnly.Has("jam") && jamOnly.HasTrait("jam"),
                      "前提：夹具卡 `F13_JamOnly` 的 `jam` **只**在 `cardTraits` 列（`keywords` 列为空）");
            CheckTrue(jamOnly.Has("oath") == false && jamOnly.HasTrait("oath"),
                      "前提：同一张卡的 `oath 1` 也**只**在 `cardTraits` 列（`Has` 判不到，后两道闸仍能过）");

            var ctxJam = ProbeBattle(new CardDef[0], new CardDef[0]);
            ctxJam.Active = 0; ctxJam.Players[0].Energy = 5; ctxJam.Players[0].MaxEnergy = 5;
            var uJam = Place(ctxJam, 0, 0, jamOnly);
            CheckTrue(uJam.HasCurrentTrait("jam"),
                      "前提：`HasCurrentTrait(\"jam\")` 为真（场上那半支读得到 `cardTraits`）");
            CheckTrue(RuleCore.CanPlayWithOathNowInPlay(ctxJam, 0, jamOnly, uJam) == false,
                      "★★ ⑥-a（本件主诉 · jam 闸）：jam **只在 `cardTraits`** ⇒ `CanPlayWithOathNowInPlay` **为假**"
                    + "（原版 `CardScript__CanPlayWithOathNowInPlay.c:22` 读 `EntityScript__HasCurrentTrait(param_1, 0x82)`）"
                    + "｜🧨 把那一行改回 `thisCard.Has(...)` ⇒ 本行红（该夹具的回合 / oath / 法力三道闸都为真）");

            var ctxNoJam = ProbeBattle(new CardDef[0], new CardDef[0]);
            ctxNoJam.Active = 0; ctxNoJam.Players[0].Energy = 5; ctxNoJam.Players[0].MaxEnergy = 5;
            var uNoJam = Place(ctxNoJam, 0, 0, noJam);
            CheckTrue(RuleCore.CanPlayWithOathNowInPlay(ctxNoJam, 0, noJam, uNoJam) == true,
                      "★★ ⑥-b（灭自证 · 反证）：同样的夹具但**不带 jam** ⇒ 为**真**"
                    + "（证明 ⑥-a 不是「这个口恒 false」，那三道闸在该夹具下确实都过）");

            // ---- ⑥-B：`ca = 360` 的誓约闸读 `cardTraits`（不是 `keywords`）；读数口 = 法力 ----
            //   `PayCardPlayedCostOath` 付的是 **actingCard 自己的 `oath` 值**（`.c:11`），
            //   所以 actingCard 自己必须带 `oath 1` —— 用 `keywords` 那条路造（那条路本来就没改）。
            var actor = Unit("F13_Actor", 1, 1, 5, "Oath 1");
            var chosenTraits = W19TraitsCard("F13_ChosenTraits", "oath 2");   // 选中的那张：oath 只在 cardTraits
            var chosenKw = Unit("F13_ChosenKw", 1, 1, 1, "Oath 2");           // 对照：oath 只在 keywords
            CheckTrue(actor.Has("oath") && actor.KwValue("oath") == 1,
                      "前提：actingCard 的 `keywords` 里有 `oath 1`（付费闸与扣款额都读它）");
            CheckTrue(chosenTraits.Has("oath") == false && chosenTraits.HasTrait("oath"),
                      "前提：`F13_ChosenTraits` 的 `oath` **只**在 `cardTraits` 列");
            CheckTrue(chosenKw.Has("oath") && !chosenKw.HasTrait("oath"),
                      "前提：对照卡 `F13_ChosenKw` 的 `oath` **只**在 `keywords` 列（`cardTraits` 没喂）");

            var ctxT = ProbeBattle(new CardDef[0], new CardDef[0]);
            ctxT.Players[0].Energy = 5; ctxT.Players[0].MaxEnergy = 5;
            var uT = Place(ctxT, 0, 0, actor);
            ctxT.CardChosenInSelection = chosenTraits;
            int e0 = ctxT.Players[0].Energy;
            RuleCore.RunPlayChosenCardAbility(ctxT, 0, uT, null);
            CheckTrue(ctxT.CardChosenInSelection == null,
                      "前提：这一支真的进来了（跑完会把 `CardChosenInSelection` 清空 —— 原版 `PlayAbility.c:2060-2068`）");
            CheckTrue(ctxT.Players[0].Energy == e0 - 1,
                      "★★ ⑥-c（本件主诉 · 誓约闸）：选中的那张 `oath` **只在 `cardTraits`** ⇒ 走誓约支，按 actingCard 的 `oath 1` 扣 1 点"
                    + "（原版 `AbilityLogic__PlayAbility.c:2025` = `RawCardScript__HasDefaultTrait(thatCard, 0x4fb)`）"
                    + "｜🧨 把 `cc.HasDefaultTrait(...)` 改回 `cc.Has(...)` ⇒ 不扣钱、本行红");

            var ctxK = ProbeBattle(new CardDef[0], new CardDef[0]);
            ctxK.Players[0].Energy = 5; ctxK.Players[0].MaxEnergy = 5;
            var uK = Place(ctxK, 0, 0, actor);
            ctxK.CardChosenInSelection = chosenKw;
            int eK = ctxK.Players[0].Energy;
            RuleCore.RunPlayChosenCardAbility(ctxK, 0, uK, null);
            CheckTrue(ctxK.Players[0].Energy == eK,
                      "★★ ⑥-d（灭自证 · 反证）：同一张卡的 `oath` **只在 `keywords`** ⇒ 誓约闸为假、**不扣钱**"
                    + "（原版读的是卡面 `defaultTraits`，`keywords` 那张表不是判据）"
                    + "｜🧨 改回 `cc.Has(...)` ⇒ 这里会扣 1 点 ⇒ 本行红");
        }
    }
}
