// RuleEngineTest_B116W6.cs — 执行写手 `B116_W6`（2026-11-02 · 第一百一十六会话）
//
// ============================ 这件活是什么 ============================
//  ① `ENG-12` / `ENG-18`：「特定卡选项表」（原版 `choicesDefinition.filterSpecificCards`）从
//     「`choose one:` 的卡只能靠文本回炉」**通用化**成「任意 N 张 ⇒ 挂到那条 `chooseone` op 上，
//     结算时优先让**选项卡 SO** 跑它自己的 `ThisCardPlayed`」。改动两处：
//       · `EffectText.TryAttachChoiceTableToTextChoice`（新，挂在 `InjectChoicesDefinitionOp` 的 ② 位）
//       · `RuleCore.TryRunStructuredOptionOrText`（新，`DoChooseOne` 的结算口）
//  ② `B-323`：`ResolveOathAbility` 的 `Auras.Recompose` **只在真付了费时**才跑。
//
// ============================ 判据（⛔ 不是我挑的）============================
//  · `BattleManager__GetChoiceFullPool.c:36-55`：`ta == 180` 且 `filterSpecificCards` 非空
//    ⇒ 候选池**就是那张表**（再 `uniqueId != null` + 排序）；为空才回落 `cardsTargetedInChosenResolution`。
//  · 表长**任意**（现读池内 180 档 56 张：2 张 11 卡 · 3 张 13 · 4 张 11 · 5 张 1 · 6 张 4 · 0 张 16）
//    ⇒ 任何「只认 2 张」的形状都是缺的；`maxTargets` **只决定面板摆几张**（`GetChoiceOptions.c:40-45`）。
//  · 原版两条机制**别混**：① 明写 `choose one:` = `choicesDefinition` → 选项卡 SO；
//    ② 无 `choose` 的 `X or Y` = 同一 ability 多条 logic + 随机挑一行（`RawCardScript__ResolveAbilities.c:16-30`）。
//  · 机制 ①的下游 = `AbilityLogic__PlayAbility.c:2032-2040`：`thatCard.OnTriggerAlt(0 /* ThisCardPlayed */, …)`
//    （我们 = `RunAbilityOfAnotherCard(…, "rally", …)` —— 与 `chosenAbility 360` 同一个执行器）。
//  · `B-323`：`Recompose` **幂等 ≠ 无副作用**（重放夹血 / 费用重登记 / 誓约授予）⇒ 被拒那一手不许重算。
//
// ============================ 宿主接线 ============================
//   在 `RuleEngineTest.cs` 的 `Run()` 守卫聚合行里**同一物理行追加**（⛔ 别接到行尾 `//` 之后）：
//       Guard("CheckB116W6ChoiceTableAndOathRecompose", CheckB116W6ChoiceTableAndOathRecompose);
//
// ============================ 🔴 灭自证（每件各一对，结构上不可能同时绿）============================
//  · ENG-12/18：**同一个 op / 同一份数据**，只是「选项 SO 在不在卡池里」不同 ⇒
//      A6 手牌 **+1**（结构化支：跑选项卡的 `Rally: Draw a card`）/ A7 手牌 **+2**（回落文本：`Draw 2 cards`）。
//      外加 A3/A4/A5 三条「**不许挂**」的反例（张数对不上 / 卡上没数据 / 机制 ② 的随机 op）。
//  · B-323：**同一个观察点**（孤儿光环关键词 `fast` 在不在）——被拒那一手**留**（不重算）、
//      付了费那一手**被收回**（重算）。退化写法（恒不跑 / 恒跑）各自只能过其中一条。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

public static partial class RuleEngineTest
{
    // ---------------------------------------------------------------- 夹具
    /// <summary>「选项卡 SO」的替身：`Rally:` = 原版 `ThisCardPlayed(0)`（`PlayAbility.c:2032`）。</summary>
    static CardDef ChoiceOptB116W6(string id, string name, string rallyDesc)
    {
        return new CardDef(id, name, "tactic", rallyDesc, "common", "Test", 0, 0, 0, 0, null);
    }

    /// <summary>带原版 `choicesDefinition` 三列的父卡（`ta = 180` = `choiceResolution`）。</summary>
    static CardDef ChoiceParentB116W6(string name, string desc, int maxTargets, params string[] ids)
    {
        return new CardDef(name, name, "tactic", desc, "common", "Test", 1, 0, 0, 0, null,
                           choicesTargetsAffected: 180, choicesMaxTargets: maxTargets,
                           choicesSpecificCardIds: ids);
    }

    static EffectOp FirstChooseOneB116W6(List<EffectOp> ops)
    {
        if (ops == null) return null;
        for (int i = 0; i < ops.Count; i++)
            if (ops[i] != null && ops[i].Verb == "chooseone") return ops[i];
        return null;
    }

    /// <summary>`ctx.Events` 里有没有这条日志（判「走的是结构化支还是文本支」的**第二观测点**，与手牌数互证）。
    /// <para>`BattleContext.Log(string)` 只做 `Events.Add(message)`（`BattleContext.cs:1857-1861`）——
    /// ⛔ 别去猜别的载体。</para></summary>
    static bool HasLogB116W6(BattleContext ctx, string needle)
    {
        if (ctx == null || ctx.Events == null || needle == null) return false;
        for (int i = 0; i < ctx.Events.Count; i++)
        {
            string line = ctx.Events[i];
            if (line != null && line.IndexOf(needle, System.StringComparison.Ordinal) >= 0) return true;
        }
        return false;
    }

    // ================================================================ 宿主入口
    public static void CheckB116W6ChoiceTableAndOathRecompose()
    {
        Section("🆕 B116_W6 · ENG-12/ENG-18（选项表任意 N 张 + 结构化支）· B-323（被拒誓约不重算光环）");
        ChoiceTableSectionB116W6();
        OathRecomposeSectionB116W6();
    }

    // ---------------------------------------------------------------- ENG-12 / ENG-18
    static void ChoiceTableSectionB116W6()
    {
        Section("B116_W6 · ENG-12/ENG-18：`choicesDefinition` 选项表（任意 N 张）挂到 `chooseone` + 结构化支");

        var optDraw = ChoiceOptB116W6("B116W6_OPT_DRAW", "W6 Opt Draw", "Rally: Draw a card");
        var optSpirit = ChoiceOptB116W6("B116W6_OPT_SPIRIT", "W6 Opt Spirit", "Rally: Gain 1 Spirit Stones");
        var optFaith = ChoiceOptB116W6("B116W6_OPT_FAITH", "W6 Opt Faith", "Rally: Gain 1 ☀");

        const string Desc3 = "Choose one: Draw 2 cards; Gain 1 Spirit Stones; Gain 1 ☀";
        const string Desc2 = "Choose one: Draw 2 cards; Gain 1 Spirit Stones";
        var p3 = ChoiceParentB116W6("W6 P3", Desc3, 3, "B116W6_OPT_DRAW", "B116W6_OPT_SPIRIT", "B116W6_OPT_FAITH");
        var p2 = ChoiceParentB116W6("W6 P2", Desc2, 2, "B116W6_OPT_DRAW", "B116W6_OPT_SPIRIT");

        // ---- A0 前提：新夹具的 desc 真的解析成**非随机**的 `chooseone`（拿不到 ⇒ 下面全等于没验）----
        List<string> un0, pa0;
        var ops3 = EffectText.Parse(Desc3, out un0, out pa0, card: p3);
        var op3 = FirstChooseOneB116W6(ops3);
        CheckTrue(op3 != null, "A0 前提：`" + Desc3 + "` 解析出一条 `chooseone`（0 条 ⇒ 本节余下全部没验）");
        if (op3 == null) return;
        CheckTrue(!op3.RandomPick, "A0b 前提：这条是**玩家选**（`RandomPick=false`）—— 卡面明写 `Choose one:`");
        Check(op3.Amount, 3, "A0c 前提：3 个选项（`Amount` = 文本选项数）");

        // ---- A1：N = 3（**不是 2**）⇒ 原版选项表按原序挂上 ----
        CheckTrue(op3.ChooseSpecificIds != null,
                  "★ A1 `chooseone` 被挂上原版选项表（改前 `InjectChoicesDefinitionOp` 见到 `chooseone` 直接 return ⇒ 这里恒 null）");
        if (op3.ChooseSpecificIds != null)
        {
            Check(op3.ChooseSpecificIds.Count, 3,
                  "★ A1b 表长 = **3**（`ENG-12` 的「只认 2 张」已被去掉；⛔ 别退回 `== 2`）");
            Check(op3.ChooseSpecificIds[0], "B116W6_OPT_DRAW", "★ A1c 顺序 = 原版 `filterSpecificCards` 原序（第 1 项）");
            Check(op3.ChooseSpecificIds[2], "B116W6_OPT_FAITH", "★ A1d 顺序（第 3 项）");
        }

        // ---- A2：N = 2 的那一档仍然工作（别只顾着通用化把老档弄坏）----
        List<string> un2, pa2;
        var op2 = FirstChooseOneB116W6(EffectText.Parse(Desc2, out un2, out pa2, card: p2));
        CheckTrue(op2 != null && op2.ChooseSpecificIds != null && op2.ChooseSpecificIds.Count == 2,
                  "★ A2 N = 2 同样挂上（两档都过 ⇒ 既是「不限 2」也不是「只认 3」）");

        // ---- A3 灭自证①：**张数对不上 ⇒ 不许挂**（挂了下标会错位）----
        List<string> un3, pa3;
        var opMismatch = FirstChooseOneB116W6(EffectText.Parse(Desc3, out un3, out pa3, card: p2));   // 3 个选项 + 2 个 id
        CheckTrue(opMismatch != null && opMismatch.ChooseSpecificIds == null,
                  "★ A3 灭自证：3 个文本选项 + 2 个 id ⇒ **不挂**（挂上是无条件挂 ⇒ 本条红）");

        // ---- A4 灭自证②：卡上**没有** `choicesDefinition` ⇒ 不许挂（证明标记来自卡数据、不是来自文本）----
        var noData = new CardDef("W6 NODATA", "W6 NODATA", "tactic", Desc3, "common", "Test", 1, 0, 0, 0, null);
        List<string> un4, pa4;
        var opNoData = FirstChooseOneB116W6(EffectText.Parse(Desc3, out un4, out pa4, card: noData));
        CheckTrue(opNoData != null && opNoData.ChooseSpecificIds == null,
                  "★ A4 灭自证：同一句 desc、卡上没有 `choicesDefinition` ⇒ **不挂**（文本本身给不出 id）");

        // ---- A5 灭自证③：机制 ②（无 `choose` 的 `X or Y`，`RandomPick=true`）⇒ 不许挂 ----
        const string DescOr = "Draw 2 cards or Gain 1 Spirit Stones";
        var pOr = ChoiceParentB116W6("W6 POR", DescOr, 2, "B116W6_OPT_DRAW", "B116W6_OPT_SPIRIT");
        List<string> un5, pa5;
        var opOr = FirstChooseOneB116W6(EffectText.Parse(DescOr, out un5, out pa5, card: pOr));
        CheckTrue(opOr != null && opOr.RandomPick,
                  "A5 前提：`" + DescOr + "` 是机制 ② 的随机 op（`RandomPick=true`）");
        CheckTrue(opOr != null && opOr.ChooseSpecificIds == null,
                  "★ A5 灭自证：机制 ② 那档**不挂**原版选项表（两套机制⛔ 别混）");

        // ---- A6：结构化支真的跑选项卡 SO（池里有 ⇒ 跑它的 `Rally: Draw a card` = 手牌 +1）----
        {
            var pool = new List<CardDef> { optDraw, optSpirit, optFaith };
            var ctx = BattlePool(new CardDef[0], new CardDef[0], pool);
            ToP1Turn(ctx, 1);
            int handBefore = ctx.Players[0].Hand.Count;
            ctx.ChoosePicks.Enqueue(0);                            // 面板选第 1 项（文本 = `Draw 2 cards`）
            List<string> un6, pa6;
            var ops6 = EffectText.Parse(Desc3, out un6, out pa6, card: p3);
            bool ok6 = RuleCore.ResolveOps(ctx, 0, null, ops6, "B116W6", chosen: null);
            CheckTrue(ok6, "A6 前提：这条 `chooseone` 结算返回 true");
            Check(ctx.Players[0].Hand.Count - handBefore, 1,
                  "★ A6 **结构化支**：选项 SO「W6 Opt Draw」的 `Rally: Draw a card` 跑了 ⇒ 手牌 **+1**"
                + "（文本是 `Draw 2 cards`；改前/回落那一支会是 +2 —— 与 A7 是一对灭自证）");
            CheckTrue(HasLogB116W6(ctx, "选项卡 SO"),
                      "★ A6b 日志点名走的是**选项卡 SO**（第二观测点，与 A6 的读数互证）");
        }

        // ---- A7 灭自证④：**同一个 op**，选项 SO 不在池里 ⇒ 回落文本（手牌 +2）----
        {
            var ctx = BattlePool(new CardDef[0], new CardDef[0], new List<CardDef>());
            ToP1Turn(ctx, 1);
            int handBefore = ctx.Players[0].Hand.Count;
            ctx.ChoosePicks.Enqueue(0);
            List<string> un7, pa7;
            var ops7 = EffectText.Parse(Desc3, out un7, out pa7, card: p3);
            CheckTrue(RuleCore.ResolveOps(ctx, 0, null, ops7, "B116W6", chosen: null),
                      "A7 前提：选项 SO 不在池里时这条仍然结算成功（⛔ 不许变成静默不生效）");
            Check(ctx.Players[0].Hand.Count - handBefore, 2,
                  "★ A7 灭自证：选项 SO 不在池里 ⇒ **按卡面文本**结算「Draw 2 cards」= 手牌 **+2**"
                + "（与 A6 的 +1 结构上不可能同时成立）");
            CheckTrue(HasLogB116W6(ctx, "都没有它"),
                      "★ A7b 回落时**出声**点名「可玩池与选项 SO 旁表都没有它」（⛔ 不许静默）");
        }

        // ---- A8：**真实数据** —— 6 张 `choose one:` 卡全部挂上原版选项表（改动对真卡生效）----
        {
            var real = CardDatabase.Load();
            string[] names = { "Craftworld Convergence", "The Rock", "Duelist's Hubris",
                               "Inscrutable Cunning", "Hymn of Battle", "The Fang" };
            int hit = 0, expect = 0, inPool = 0, total = 0;
            foreach (string nm in names)
            {
                var c = CardDatabase.Find(real, nm);
                CheckTrue(c != null, "A8 前提：真实卡池里有「" + nm + "」");
                if (c == null) continue;
                List<string> un8, pa8;
                var op8 = FirstChooseOneB116W6(EffectText.Parse(c.Desc, out un8, out pa8, card: c));
                CheckTrue(op8 != null, "A8 前提：「" + nm + "」解析出一条 `chooseone`");
                if (op8 == null) continue;
                bool ok = op8.ChooseSpecificIds != null
                          && c.ChoicesSpecificCardIds != null
                          && op8.ChooseSpecificIds.Count == c.ChoicesSpecificCardIds.Count
                          && op8.ChooseSpecificIds.Count == op8.Amount;
                CheckTrue(ok, "★ A8 真实卡：「" + nm + "」的选项表挂上了（" + op8.Amount + " 项）");
                if (ok) hit++;
                expect++;
                // 顺带量一次「选项 SO 在不在可玩池里」—— 这一格解释**为什么**今天仍走文本支。
                // ⚠️ 只**打印**、不断言（选项 SO 将来进池时这条必须自动变，硬断 0 会变成假红）。
                if (c.ChoicesSpecificCardIds != null)
                {
                    foreach (var id in c.ChoicesSpecificCardIds)
                    {
                        total++;
                        foreach (var pc in real)
                            if (pc != null && string.Equals(pc.Id, id, System.StringComparison.Ordinal)) { inPool++; break; }
                    }
                }
            }
            Check(hit, expect, "★ A8b 6 张真卡**全部**挂上（" + hit + "/" + expect + "）");
            CheckTrue(total > 0, "A8c 前提：6 张卡的选项表合计非空（拿不到 id ⇒ 下一行的读数无意义）");
            Debug.Log(P + $"   A8d 读数（**不断言**）：6 张卡的选项 SO 在**可玩池**里命中 {inPool}/{total}"
                        + "（现读 0 —— 它们在 `cards_truth.json` 1,351 张里、不在 `cards_engine.json` 1,129 张里 "
                        + "⇒ 结构化支今天走不到、走文本支；这是**数据面缺口**，选项 SO 进池是另一笔）");
        }
    }

    // ---------------------------------------------------------------- B-323
    static void OathRecomposeSectionB116W6()
    {
        Section("B116_W6 · B-323：`ResolveOathAbility` 被拒时 **不跑** `Auras.Recompose`");

        var oath = new CardDef("T_B116OATH", "T_B116OATH", "unit", "Oath 1: Gain Armour 1",
                               "common", "Test", 1, 2, 3, 0, null, subtype: "Infantry");
        var ctx = Battle(new[] { oath }, new[] { Unit("X", 1, 1, 5) });
        ToP1Turn(ctx, 1);
        var u = Place(ctx, 0, 3, oath, exhausted: true);

        // ---- B0 前提：这一条真的是「誓约能力」且代价 = 1 ----
        CheckTrue(u.Card.OathOps != null && u.Card.OathOps.Count > 0,
                  "B0 前提：`Oath 1: …` 的正文收下来了（拿不到 ⇒ B2/B4 等于没验）");
        Check(u.Card.OathCost, 1, "B0b 前提：代价 = 1");

        // ---- B1 前提：`AddAuraKeyword` 真的把「孤儿光环关键词」挂上去（观察点本身有效）----
        u.AddAuraKeyword("fast", 1);
        CheckTrue(u.Has("fast"),
                  "B1 前提：`AddAuraKeyword(\"fast\", 1)` 之后 `Has(\"fast\")` = true"
                + "（这一格就是「光环那一份」的观察点：`Auras.Recompose` 的 `ClearAuraGrants` 会把它收回、"
                + "而 `oath` 那张卡没有 `AuraSpecs` ⇒ **不会**再挂回来）");

        // ---- B2：能量不足 ⇒ 被拒，且 **不重算光环**（关键词留着）----
        ctx.Players[0].Energy = 0;
        bool paidNo = RuleCore.ResolveOathAbility(ctx, 0, u);
        CheckTrue(!paidNo, "B2 前提：能量 0 ⇒ `ResolveOathAbility` 返 false（付费被拒）");
        Check(ctx.Players[0].Energy, 0, "B2b 被拒时**没有扣费**（前提）");
        CheckTrue(u.Has("fast"),
                  "★ B2c **被拒那一手不跑 `Auras.Recompose`** ⇒ 孤儿光环关键词**留着**"
                + "｜🧨 把 `EffectResolver.cs:9419` 退回无条件的 `if (!ctx.IsOver) Auras.Recompose(ctx);` ⇒ 本条红"
                + "（`Recompose` 幂等 ≠ 无副作用：它会重放夹血 / 费用重登记 / 誓约授予）");

        // ---- B2c' 敏感性：**显式跑一次** `Auras.Recompose`（= 改前那一行的等价物）⇒ 观察点必须翻转 ----
        //   🔴 没有这一条，B2c 有可能是恒真（例如 `AddAuraKeyword` 压根挂不上、或 `ClearAuraGrants` 不认这本账）。
        Auras.Recompose(ctx);
        CheckTrue(!u.Has("fast"),
                  "★ B2c' 敏感性：显式 `Auras.Recompose(ctx)` 一次 ⇒ 孤儿光环关键词**被收回**"
                + " ⇒ 证明 B2c 的观察点**有判别力**（不是恒真）");
        u.AddAuraKeyword("fast", 1);

        // ---- B3 前提 + B4 灭自证：付得起那一手**会**重算（同一条观察点反向）----
        u.AddAuraKeyword("fast", 1);          // 补回（上一格若被收回也照样成立）
        CheckTrue(u.Has("fast"), "B3 前提：观察点重新就位");
        ctx.Players[0].Energy = 5;
        bool paidYes = RuleCore.ResolveOathAbility(ctx, 0, u);
        CheckTrue(paidYes, "B3b 前提：能量 5 ⇒ 这一手真的付了费、返回 true");
        Check(ctx.Players[0].Energy, 4, "B3c 前提：真扣了 1 费（5 → 4）");
        Check(u.Armor, 1, "B3d 前提：正文真的结算了（护甲 0 → 1）");
        CheckTrue(!u.Has("fast"),
                  "★ B4 灭自证：**付了费**那一手照旧跑 `Auras.Recompose` ⇒ 同一个观察点**反向**"
                + "（孤儿光环关键词被 `ClearAuraGrants` 收回）—— B2c 与 B4 结构上不可能靠「恒不跑 / 恒跑」同时变绿");
    }
}
