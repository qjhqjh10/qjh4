// B120_W7_Assert.cs — 第一百二十会话 · 写手 `B120_W7`（引擎数值 / trait 记账）
//
// 判据正本 → `资料/完全复刻/报告/B120_E_引擎数值.md` E-1 · 活状态表 `D118-77` / `D118-80`
//   · `D118-77` → `decomp_full/CardScript__AddEffect.c:460-679`（每条 `addTrait` 都在 `activeEffects(+0x108)`
//     里留**一条 `CardEffect` 记录**；合并键 = 同 trait ∧ 同 `markOfChaosType` ∧ 同 `daemonHostType`
//     ∧ `SameTimingAsEffect` ∧ 两侧 `extrinsic` 都为 0；未命中才 `List.Add` + `AddTraitSilently`）。
//     我们改前的结构化路（`EffectResolverPartial_B97W2.cs` 的 `case 3: case 90:`）**只调 `AddKeyword`**
//     ⇒ 记录账 / `untilNextAttack` 到期 / 按来源撤 / `AnyEffectGrantsTrait` 第二道闸对它全失效。
//     本批把它换成 `ApplyAddTraitRecordRow`（`RuleEngine/Core/RuleCorePartial_B120W7.cs`）。
//   · `E-01` → `BattleCardManager__CreateDeck.c:27-70`（按牌表**原序**造牌、`SetActive(index == 0)` ⇒ **下标 0 = 顶**）
//     ＋ `BattleManager__DrawCardFromDeck.c:27`（`get_Item(deck, 0)`）。🔴 **2026-11-03 `B120-W7b` 已落补丁**
//     （`DeckBuilder.FromDeck` 里 `list.Reverse(deckStart, …)`，只倒牌库段）⇒ 文件末那两段现在应当**绿**。
//   · `D118-80`（原版 **14 个调用方方法** / 18 处调用点各自刷 trait 图标、4 类不刷）**没有运行期断言** —— 那条的全部残差都在
//     **表现层**（`CardView.SetBadges` 的调用时机），引擎侧两个特例（`RuleCore.cs` 的低血 Invulnerable、
//     `TransformIntoRemnant`）现读**已经**是静默路径。审计结论写进报告 §D118-80，⛔ 不为一条调用点审计造自证断言。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · W1：把 `ApplyAddTraitRecordRow` 改回 `return target.AddKeyword(kw, b.TraitValue);` ⇒ W1 实得 0 ⇒ 红。
//   · W2：把 `ApplyTraitEffect` 换成「每次新建一条记录（不走 FindTraitEffect）」⇒ W2 条数变 2 ⇒ 红；
//          换成「整条丢弃、只加关键词」⇒ W2 条数 1 但 W3 值仍是 1 ⇒ 红（**条数与值必须一起断**）。
//   · W4：把 `extrinsic` 那格不传（恒 false）⇒ W4 条数变 1 ⇒ 红。
//   · W5：把 `traitValue <= 0` 也丢进 `ApplyTraitEffect`（它返回 null）⇒ 关键词不再挂上 ⇒ W5 红；
//          反过来给 `value <= 0` 也建记录 ⇒ W5 的记录条数变 1 ⇒ 红。
//   · W6：`timingBits` 不传（恒 0）⇒ 两条并入 1 条 ⇒ W6 红。
//   · E-01a/b：把 `DeckBuilder.FromDeck` 的 `deckStart` 改成 0（整表倒序）⇒ 督军/防御卡被甩到末尾 ⇒ 红。
//   · E-01c：删掉 `list.Reverse(deckStart, list.Count - deckStart);` ⇒ 牌库段回到 A,B,C ⇒ 红。
//   · E1/E2：同上（端到端看抽牌序），删掉那句 ⇒ 实得 `B120W7DeckB` ⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
// `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里那一行（现读 `:1303`）
//       Guard("CheckB97ER2Gaps", …); { Guard("CheckB97W2Gaps", …); Guard("CheckB97W5DataColumnsAndPoolFilters", …); }   // 注释…
// **同一物理行追加**（⚠️ 必须落在该行第一个 `//` 之前 ⇒ 零行数变化）：
//       Guard("CheckB120W7Gaps", CheckB120W7Gaps);
// 那一行**一个入口就够** —— `CheckB120W7Gaps()` 内部已经把 W 段（`D118-77`）、装载器段与端到端段（`E-01`）都跑了。
// （`B120W7DeckLoaderOrder` / `CheckB120W7DeckOrder` 仍各自 `public`，要单独定位失败时可以逐个调。）
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>`D118-77`：结构化 `addTrait(3)/addTraitRemnant(90)` 路要建 `TraitEffectRecord`。</summary>
    public static void CheckB120W7Gaps()
    {
        Section("🆕 B120_W7 · `D118-77` 结构化 buff 路的 `addTrait` 补建 `TraitEffectRecord`（原版 `AddEffect.c:460-679`）");
        B120W7RecordRows();
        B120W7DeckLoaderOrder();
        CheckB120W7DeckOrder();
    }

    /// <summary>trait `flank` 的 `DefinedTrait` 数值（`CardDefPartial_B94B16.cs:56` 现读 `{ "flank", 460 }`）。</summary>
    const int TraitFlank = 460;

    static void B120W7RecordRows()
    {
        // ---- W1~W3：记录建起来了 · 同 timing 合并 · 关键词增量与改前逐字相同 ----
        {
            var c = Unit("B120W7A", 1, 2, 3);
            var ctx = ProbeBattle(new[] { c }, new CardDef[] { });
            var u = Place(ctx, 0, 1, c);
            Check(u != null, true, "W0（前提）：`B120W7A` 上了棋盘");
            if (u == null) return;
            Check(u.TraitEffects.Count, 0, "W0（前提）：它身上还没有任何 `addTrait` 记录");
            int kw0 = u.KwValue("flank");
            Check(kw0, 0, "W0（前提）：卡面本身**没有** `flank`（免得把默认值算进增量）");

            bool ok = RuleCore.ApplyAbilityBuff(ctx, 0, u,
                new CardDatabase.AbilityBuffSpec { BuffType = 3, Trait = TraitFlank, TraitValue = 1 },
                "B120W7:W1");
            Check(ok, true, "W1（前提）：`addTrait(3) flank 1` 落地了");
            Check(u.TraitEffects.Count, 1,
                  "🧨 W1 **建了一条 `TraitEffectRecord`** —— 改前这条结构化路只调 `AddKeyword`（实得 **0**）⇒\n"
                + "            `untilNextAttack` 到期 / 按来源撤 / `AnyEffectGrantsTrait` 第二道闸对这条路全失效（`D118-77`）");
            Check(u.KwValue("flank"), kw0 + 1,
                  "🧨 W1b 关键词增量与改前**逐字相同**（`ApplyTraitEffect` 内部也走 `AddKeyword`）⇒ 本批只加记录、不改数值");

            RuleCore.ApplyAbilityBuff(ctx, 0, u,
                new CardDatabase.AbilityBuffSpec { BuffType = 3, Trait = TraitFlank, TraitValue = 2 },
                "B120W7:W2");
            Check(u.TraitEffects.Count, 1,
                  "🧨 W2 同 timing 的第二次**合并进同一条**记录（原版合并键；每条都新建 ⇒ 实得 2）");
            Check(u.TraitEffectValue("flank"), 3,
                  "🧨 W3 记录值累加 1+2 = 3（`_stackable = add`）——\n"
                + "            ⚠️ W2/W3 必须**一起**断：只断条数会被「整条丢弃」的实现混过、只断值会被「重复建记录」混过");
            Check(u.KwValue("flank"), kw0 + 3, "🧨 W3b 关键词值 = 累计 3（净值仍与「N 次 `AddKeyword`」相同）");
        }

        // ---- W4：`extrinsic` 的记录**不合并**（原版合并键要求两侧 extrinsic 都为 0）----
        {
            var c = Unit("B120W7B", 1, 2, 3);
            var ctx = ProbeBattle(new[] { c }, new CardDef[] { });
            var u = Place(ctx, 0, 1, c);
            if (u == null) { Check(true, false, "W4（前提）：`B120W7B` 上了棋盘"); return; }
            RuleCore.ApplyAbilityBuff(ctx, 0, u,
                new CardDatabase.AbilityBuffSpec { BuffType = 3, Trait = TraitFlank, TraitValue = 1 }, "B120W7:W4a");
            RuleCore.ApplyAbilityBuff(ctx, 0, u,
                new CardDatabase.AbilityBuffSpec { BuffType = 3, Trait = TraitFlank, TraitValue = 1, Extrinsic = 1 },
                "B120W7:W4b");
            Check(u.TraitEffects.Count, 2,
                  "🧨 W4 `extrinsic` 那条**另起一条记录**（原版合并键「两侧 extrinsic 都为 0」）；\n"
                + "            不把 `b.Extrinsic` 透传 ⇒ 两条并成 1 条 ⇒ 红");
            bool sawExtrinsic = false;
            for (int i = 0; i < u.TraitEffects.Count; i++)
                if (u.TraitEffects[i] != null && u.TraitEffects[i].Extrinsic) sawExtrinsic = true;
            Check(sawExtrinsic, true, "🧨 W4b 那条记录的 `Extrinsic` 位真的是 `true`（不是只有条数对）");
            Check(u.TraitEffectValue("flank"), 2, "🧨 W4c 值 = 1 + 1（两条各记各的）");
        }

        // ---- W5：空给（`traitValue <= 0`）⇒ **不建记录**、但词条照挂（与文本 op 路 `ApplyOneGain` 同一口径）----
        {
            var c = Unit("B120W7C", 1, 2, 3);
            var ctx = ProbeBattle(new[] { c }, new CardDef[] { });
            var u = Place(ctx, 0, 1, c);
            if (u == null) { Check(true, false, "W5（前提）：`B120W7C` 上了棋盘"); return; }
            bool ok = RuleCore.ApplyAbilityBuff(ctx, 0, u,
                new CardDatabase.AbilityBuffSpec { BuffType = 3, Trait = TraitFlank, TraitValue = 0 }, "B120W7:W5");
            Check(ok, true, "W5（前提）：`traitValue = 0` 那一档仍然回 `true`（改前就是 `AddKeyword(kw, 0)` 成功）");
            Check(u.Has("flank"), true,
                  "🧨 W5 空给**照样挂上词条** —— 本池 321 条 `buffType ∈ {3,90}` 行里 **234 条 `traitValue` 缺失（= 0）**\n"
                + "            （`cards_engine.json` 现扫）⇒ 这一档必须与改前逐字相同（⛔ 别把 0 也丢进 `ApplyTraitEffect`，它对 `<= 0` 直接返回 null）");
            Check(u.TraitEffects.Count, 0,
                  "🧨 W5b 但**不建记录**（文本 op 路同一条口径：`EffectResolver.cs:12304` 的 `else added = u.AddKeyword(...)`）");
        }

        // ---- W6：`timingBits` 真的透传（不同 `until*` ⇒ 不合并）----
        {
            var c = Unit("B120W7D", 1, 2, 3);
            var ctx = ProbeBattle(new[] { c }, new CardDef[] { });
            var u = Place(ctx, 0, 1, c);
            if (u == null) { Check(true, false, "W6（前提）：`B120W7D` 上了棋盘"); return; }
            RuleCore.ApplyAbilityBuff(ctx, 0, u,
                new CardDatabase.AbilityBuffSpec { BuffType = 3, Trait = TraitFlank, TraitValue = 1 }, "B120W7:W6a");
            RuleCore.ApplyAbilityBuff(ctx, 0, u,
                new CardDatabase.AbilityBuffSpec { BuffType = 3, Trait = TraitFlank, TraitValue = 1, UntilEndOfTurn = 1 },
                "B120W7:W6b");
            Check(u.TraitEffects.Count, 2,
                  "🧨 W6 不同 `untilEndOfTurn` ⇒ **两条记录**（原版合并键含 `SameTimingAsEffect`）；\n"
                + "            `timingBits` 不透传（恒 0）⇒ 并成 1 条 ⇒ 红 —— 而这正是「本回合 +1 撤不掉、且永远显示成永久」的成因");
            int bitOfSecond = -1;
            for (int i = 0; i < u.TraitEffects.Count; i++)
                if (u.TraitEffects[i] != null && u.TraitEffects[i].TimingBits != 0) bitOfSecond = u.TraitEffects[i].TimingBits;
            Check(bitOfSecond, 0x01, "🧨 W6b 限时那条的位掩码 = bit0（`untilEndOfTurn`，位序见 `TraitEffectRecord.TimingBits` 的 doc）");
            Check(u.KwValue("flank"), 2, "🧨 W6c 两条的关键词增量照常累加 = 2");
        }
    }

    // ================================================================================================
    //  E-01 · 牌库顶方向 —— **2026-11-03 `B120-W7b` 已落补丁，这两段现在应当绿**
    //
    //  原版：`BattleCardManager__CreateDeck.c:27-70` 按 `deckData` **原序**依次造牌（`:44` `Insert(list, i, card)`，
    //        i 递增 ⇒ 等价 append）、`:67` `SetAsDeck(card, i)`、`:70` `SetActive(go, i == 0)` ⇒ **下标 0 = 牌库顶**；
    //        `BattleManager__DrawCardFromDeck.c:27` 取 `List__get_Item(deck, 0)` ⇒ 「牌表第一张最先抽到」。
    //  我们：`RuleCore.MoveDeckTopToHand` 取 `Count-1`（末尾 = 顶的镜像约定）⇒ 装载口必须**倒序喂**。
    //        补丁 = `DeckBuilder.FromDeck` 的 `list.Reverse(deckStart, …)`（只倒牌库段，督军/防御卡两格不动）。
    //
    //  ⛔ 为什么补丁不在 `RuleCore.BuildPlayer` 里反转：教程那条链的装载口（`BattleDriver.BuildTutorialDeck`，
    //     2026-10-26 `G18` 已按镜像约定倒序）**已经喂的是倒序**，在 `BuildPlayer` 再倒一次会把它翻回去；
    //     而且 `RuleEngineTest` / `ruleprobe` 的夹具（`Deck` / `DeckOf` / `ProbeBattle`）**也是按镜像约定写的**
    //     （`RuleEngineTest.cs:2681` 的注释：「其余倒序（抽牌是 pop_back，所以最后入列的先抽到）」）
    //     ⇒ 在 `BuildPlayer` 反转会把 1,128 张的 `play all dump` 基线一起打翻。
    // ================================================================================================

    /// <summary>`E-01` 的**装载器**不变量（灭自证）：牌库段与原表**逐张相反**，督军/防御卡**位置未动**。</summary>
    public static void B120W7DeckLoaderOrder()
    {
        Section("🆕 B120_W7 · `E-01` 装载器：`DeckBuilder.FromDeck` 只倒**牌库段**");
        var hero = HeroOf("B120W7Hero", "Ultramarines", 2, 30);
        // 防御卡的 `Desc` 留空 ⇒ `TacticPlayable` 两道闸（`IsHandTrap` / `IsFullyParsed`）都过（同 `DeckRulesTest` 的夹具形状）
        var def = new CardDef("B120W7Def", "B120W7Def", "defence", "", null, "Ultramarines", 0, 0, 0, 0, null);
        var a = Unit("B120W7LA", 1, 1, 1);
        var b = Unit("B120W7LB", 1, 1, 1);
        var c = Unit("B120W7LC", 1, 1, 1);
        var pool = new List<CardDef> { hero, def, a, b, c };
        var deck = new PlayerDeck("B120W7 装载器", hero.Id, def.Id,
                                  new List<string> { a.Id, b.Id, c.Id }, (int)GameMode.Classic);
        var list = DeckBuilder.FromDeck(pool, deck, null, hero.Faction);
        Check(list.Count, 5, "E0（前提）：督军 + 防御卡 + 3 张牌库卡**全部**展开了（少一张下面的下标就对不上）");
        if (list.Count != 5) return;
        Check(list[0].Id, "B120W7Hero",
              "🧨 E-01a 督军**位置未动**（仍是 `list[0]`）—— 倒序只作用于「牌库段」；\n"
            + "            把 `deckStart` 写成 0（整表倒序）⇒ 督军被甩到末尾 ⇒ 红");
        Check(list[1].Id, "B120W7Def",
              "🧨 E-01b 防御卡**位置未动**（仍是 `list[1]`）—— 它也是独立一格、不进牌库");
        Check(list[2].Id + "," + list[3].Id + "," + list[4].Id, "B120W7LC,B120W7LB,B120W7LA",
              "🧨 E-01c 牌库段与原表（A,B,C）**逐张相反**（C,B,A）；\n"
            + "            删掉 `list.Reverse(deckStart, …)` 那句 ⇒ 实得 A,B,C ⇒ 红");
    }

    /// <summary>`E-01` 的**端到端**不变量：走真实装载口之后，**牌表里第一张 = 最先抽到**。</summary>
    public static void CheckB120W7DeckOrder()
    {
        Section("🆕 B120_W7 · `E-01` 端到端：牌表第一张最先抽到（原版 `DrawCardFromDeck.c:27` 的 `List[0]`）");
        var hero = HeroOf("B120W7Hero2", "Ultramarines", 2, 30);
        var a = Unit("B120W7DeckA", 1, 1, 1);
        var b = Unit("B120W7DeckB", 1, 1, 1);
        var pool = new List<CardDef> { hero, a, b };
        // 走**产品那条装载口**（`DeckBuilder.FromDeck`）—— ⛔ 别直接把牌表递给 `NewBattle`（那会绕过被测代码）
        var deck = new PlayerDeck("B120W7 抽牌序", hero.Id, null,
                                  new List<string> { a.Id, b.Id }, (int)GameMode.Classic);
        var mine = DeckBuilder.FromDeck(pool, deck, null, hero.Faction);
        var foeHero = HeroOf("FixtureWarlord", "Goff", 2, 30);
        var foe = new List<CardDef> { foeHero };
        var ctx = RuleCore.NewBattle(mine, foe, seed: 0, shuffle: false, cardPool: pool);
        Check(ctx.Players[0].Hand.Count, 2, "E1（前提）：牌库里那两张都被起手抽走了（不洗牌 ⇒ 顺序可判）");
        if (ctx.Players[0].Hand.Count != 2) return;
        Check(ctx.Players[0].Hand[0].Card.Id, "B120W7DeckA",
              "🧨 E1 **牌表第一张 = 最先抽到**（原版下标 0 = 牌库顶；`FromDeck` 倒序喂 + `Draw` 取末尾 = 同一个）；\n"
            + "            删掉 `FromDeck` 的 `list.Reverse(...)` ⇒ 实得 `B120W7DeckB` = 整副倒着抽 ⇒ 红");
        Check(ctx.Players[0].Hand[1].Card.Id, "B120W7DeckB", "🧨 E2 第二张同理（顺序整体一致）");
    }
}
