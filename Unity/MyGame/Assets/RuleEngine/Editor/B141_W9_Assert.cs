// B141_W9_Assert.cs — 批次 **B141** · 执行写手 `W9`（引擎 · `EffectResolver` 独占包）
//
// ============================ 它盯的是什么 ============================
//   `ENG-24`：原版 `AbilityLogic.GetValueFromCriteria` 的 **26 档分派表**
//   （唯一实现 = `RuleEngine/Core/EffectResolverPartial_B141VO.cs` 的 `TryResolveValueOption`）。
//     A 组：26 档**逐档**返回原版值（含 `min == 0 ? 原值 : min × 原值` 与 `min + 原值 × max` 两种收尾、
//           100/101/110/120/204/140 六级「不乘 min」）。
//     B 组：`default => min`（原版兜底）。
//     C 组：`opt 8` / `opt 17` **不被表接管** + `gap` 出声（⛔ 不按 min 压成恒 1）；
//           以及 【数据面】`ApplyRefCardValueOption` 对 `opt 8` **真的打一条警告**且数值原样不动。
//     D 组：灭自证 —— 把 `default` 改回「不结算」必红；把 8/17 交给 default 也必红。
//
// ============================ 原版判据（现读 `d:/2/tools/decomp_full/`）============================
//   `AbilityLogic__GetValueFromCriteria.c` 全文（366 行）；枚举 = `cpp2il/out_cpp2il_dev_ilspy/ChooseValueOptions.cs:1-29`。
//   · `.c:36-46` 0 · `.c:47-52` 10 · `.c:57-61` 20 · `.c:62-79` 30 · `.c:82-86` 40 · `.c:87-92` 41
//   · `.c:93-119` 50 · `.c:120-124` 60 · `.c:126-130` 70 · `.c:113-116` 80 · `.c:130` 90
//   · `.c:138-160` 100 · `.c:157-177` 101 · `.c:172-190` 110 · `.c:195-207` 120 · `.c:214-220` 130
//   · `.c:243-252` 140 · `.c:253-256` 150 · `.c:258-270` 160 · `.c:278-283` 170
//   · `case 200/0xc9/0xca/0xcb/0xcc/0xd2` = 200/201/202/203/204/210
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加：
//     `Guard("B141W9_Assert", B141_W9_Assert.Run); _pass += B141_W9_Assert.Pass; _fail += B141_W9_Assert.Fail;`
//   （⚠️ 追加的文本必须落在该行第一个 `//` **之前**；接线由调度台统一做。）
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B141-W9 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B141_W9_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B141-W9：ChooseValueOptions 26 档分派表（ENG-24）");
        GroupA_AllBranches();
        GroupB_DefaultIsMin();
        GroupC_NotInEnum();
        Summary();
    }

    // ==================================================================
    //  A 组：26 档逐档
    // ==================================================================
    static void GroupA_AllBranches()
    {
        Section("A 26 档逐档（`AbilityLogic__GetValueFromCriteria.c` 全文）");
        var f = NewFixture();
        if (f == null) { Report(false, "A0 前提不成立：测试对局没建起来"); return; }

        // --- 前提（硬检查，⛔ 不用 `if (前提) Check(...)` 那种静默跳过）---
        Check(f.This.AttackBase, 3, "A0a 前提：载体近战 = 3");
        Check(f.This.RangedAttackBase, 2, "A0b 前提：载体远程 = 2");
        Check(f.This.Health, 7, "A0c 前提：载体当前血 = 7");
        Check(f.Target.AttackBase, 4, "A0d 前提：目标近战 = 4");
        Check(f.Target.RangedAttackBase, 1, "A0e 前提：目标远程 = 1");
        Check(f.Target.Health, 6, "A0f 前提：目标当前血 = 6");
        Check(f.Target.CurrentMaxHealthProcessed, 10, "A0g 前提：目标 `currentMaxHealthProcessed` = 10（`GetCurrentDamage` 的减数）");
        Check(f.This.CurrentMaxHealthProcessed, 12, "A0h 前提：载体 `currentMaxHealthProcessed` = 12");
        Check(f.Ref.CostOfCard(), 5, "A0i 前提：参照卡费用 = 5");
        Check(f.Target.CostOfCard(), 6, "A0j 前提：目标费用 = 6");
        Check(f.This.KwValue("shuriken"), 2, "A0k 前提：载体 Shuriken = 2");
        Check(f.This.KwValue(KeywordTable.MarkOfChaos), 3, "A0l 前提：载体 混沌印记 = 3");
        Check(f.Target.KwValue(KeywordTable.MarkOfChaos), 5, "A0m 前提：目标 混沌印记 = 5");
        Check(f.Ally.KwValue(KeywordTable.MarkOfChaos), 7, "A0n 前提：另一个友军 混沌印记 = 7");
        Check(f.Target.Bastion, 2, "A0o 前提：目标 Bastion = 2");
        Check(f.This.Bastion, 1, "A0p 前提：载体 Bastion = 1");
        Check(f.Ctx.Players[0].Faith, 12, "A0q 前提：座位 0 信仰 = 12");
        var hero = f.Ctx.Players[0].Warlord;
        CheckTrue(hero != null, "A0r 前提：座位 0 督军在场");
        if (hero == null) return;
        Check(hero.CurrentMeleeAttack, 4, "A0s 前提：督军当前近战 = 4");

        // --- 0 Normal：`min == max` ⇒ min；否则 `GetRandomInt(min, max+1)` ---
        Eval(f, 0, 5, 5, 1, "5", "A1 0 Normal：`min == max` ⇒ min");
        { int v2; bool ok2 = Try(f, 0, 1, 6, 1, out v2, out _);
          CheckTrue(ok2 && v2 >= 1 && v2 <= 6, "A1b 0 Normal：区间命中 [1,6]（实际 " + v2 + "）"); }

        // --- 10/20/30：目标近战 / 远程 / 当前血（收尾 = min==0 ? 原值 : min×原值）---
        Eval(f, 10, 1, 0, 1, "4", "A2 10 TargetMeleeAttack：1 × 4");
        Eval(f, 10, 2, 0, 1, "8", "A2b 10：2 × 4（min 是乘数）");
        Eval(f, 10, 0, 0, 1, "4", "A2c 10：`min == 0` ⇒ 原值 4（⛔ 不是 0）");
        Eval(f, 20, 1, 0, 1, "1", "A3 20 TargetRangeAttack：1 × 1");
        Eval(f, 30, 2, 0, 1, "12", "A4 30 TargetHealth：2 × 6");

        // --- 40/41/50：载体近战 / 远程 / 当前血 ---
        Eval(f, 40, 0, 0, 1, "3", "A5 40 ThisCardAttack：`min == 0` ⇒ 3");
        Eval(f, 40, 2, 0, 1, "6", "A5b 40：2 × 3");
        Eval(f, 41, 1, 0, 1, "2", "A6 41 ThisCardAttackRanged：1 × 2");
        Eval(f, 50, 3, 0, 1, "21", "A7 50 ThisCardHealth：3 × 7");

        // --- 60/70：GetCurrentDamage = currentMaxHealthProcessed − currentHealth ---
        Eval(f, 60, 1, 0, 1, "4", "A8 60 TargetDamage：1 × (10 − 6)");
        Eval(f, 70, 1, 0, 1, "5", "A9 70 ThisCardDamage：1 × (12 − 7)");

        // --- 80：min + max × factor ---
        Eval(f, 80, 1, 2, 3, "7", "A10 80 MinPlusMaxTimesMultiplier：1 + 2 × 3 = 7（⛔ 不是 (1+2)×3 = 9）");

        // --- 90：min（逐目标掷在别处）---
        Eval(f, 90, 3, 9, 5, "3", "A11 90 RandomizedOnResolution：min = 3");

        // --- 100/101/110/120：参照卡三档 + 恒 min 那档（**都不乘 min**）---
        Eval(f, 100, 5, 0, 1, "3", "A12 100 RefCardAttack：参照卡近战 = 3（⛔ 不乘 min=5）");
        Eval(f, 101, 4, 0, 1, "4", "A13 101 BaseRefCardMAttack：现池恒 min（`referencedBaseCard` 恒 null）");
        Eval(f, 110, 5, 0, 1, "8", "A14 110 RefCardHealth：参照卡当前血 = 8（⛔ 不乘 min=5）");
        Eval(f, 120, 5, 0, 1, "5", "A15 120 RefCardCost：参照卡费用 = 5（⛔ 不乘 min=5）");

        // --- 130/150/160/170：目标/载体费用、Bastion、血−近战（收尾 = min==0 ? 原值 : min×原值）---
        Eval(f, 130, 0, 0, 1, "6", "A16 130 TargetCost：目标费用 = 6（min=0 ⇒ 原值）");
        Eval(f, 130, 2, 0, 1, "12", "A16b 130：2 × 6");
        Eval(f, 150, 1, 0, 1, "2", "A17 150 TargetBastion：1 × 2");
        Eval(f, 160, 1, 0, 1, "1", "A18 160 SelfBastion：1 × 1");
        Eval(f, 170, 0, 0, 1, "2", "A19 170 TargetCardHealthMinusAttack：6 − 4 = 2（min=0 ⇒ 原值）");

        // --- 140：督军当前近战 × factor（**不乘 min**）---
        Eval(f, 140, 9, 0, 3, "12", "A20 140 OwnWarlordAttack：4 × 3 = 12（⛔ 不乘 min=9）");

        // --- 200/201/202/203：min + 值 × max（加法，⛔ 不是 min × 值）---
        Eval(f, 200, 1, 2, 1, "7", "A21 200 ThisCardMarksOfChaos：1 + 3 × 2 = 7");
        Eval(f, 201, 1, 2, 1, "11", "A22 201 OtherCardMarksOfChaos：1 + 5 × 2 = 11");
        Eval(f, 202, 1, 2, 1, "21", "A23 202 AllFriendlyMarkOfChaos：1 + (3+7) × 2 = 21（含载体自己）");
        Eval(f, 203, 1, 2, 1, "5", "A24 203 ThisCardShuriken：1 + 2 × 2 = 5");

        // --- 204：信仰（直接返回）---
        Eval(f, 204, 9, 0, 1, "12", "A25 204 FaithValue：座位 0 信仰 = 12（⛔ 不乘 min=9）");

        // --- 210：目标血 >= 0 ⇒ 0；否则 -(health × min) ---
        Eval(f, 210, 3, 0, 1, "0", "A26 210 TargetExcessDamage：目标当前血 >= 0 ⇒ 0");
        int savedHealth = f.Target.Health;
        f.Target.Health = -4;
        Eval(f, 210, 3, 0, 1, "12", "A27 210：目标血 = −4 ⇒ -( −4 × 3 ) = 12");
        f.Target.Health = savedHealth;
    }

    // ==================================================================
    //  B 组：`default => min`
    // ==================================================================
    static void GroupB_DefaultIsMin()
    {
        Section("B 原版兜底：所有**未命中**的值都 `return min`（`.c` 每一处 `return uVar6`）");
        var f = NewFixture();
        if (f == null) { Report(false, "B0 前提不成立：测试对局没建起来"); return; }
        Eval(f, 999, 3, 8, 2, "3", "B1 ★★★ 未命中的正整数 999 ⇒ **min = 3**"
                                 + "｜🧨 把 `default` 改回「不结算」⇒ 红");
        Eval(f, 37, 4, 4, 4, "4", "B2 ★★★ 未命中的 37 ⇒ min = 4（不是 4+4×4）"
                                 + "｜🧨 让 default 走别的算式 ⇒ 红");
        Eval(f, 1000, 0, 0, 0, "0", "B3 未命中 + min = 0 ⇒ 0（不是「不结算」）");
        // 负数哨兵（我们自己的「缺格」）⇒ 出声 + false（⛔ 不拿 min 顶）
        int v; string gap;
        bool ok = Try(f, -1, 3, 3, 1, out v, out gap);
        CheckTrue(!ok && !string.IsNullOrEmpty(gap),
                  "B4 ★★ 哨兵 `-1`（缺格）⇒ **不结算 + 出声**（⛔ 不拿 min=3 顶）");
    }

    // ==================================================================
    //  C 组：`opt 8` / `opt 17` 不在 26 档里
    // ==================================================================
    static void GroupC_NotInEnum()
    {
        Section("C `opt 8` / `opt 17` 不在 `ChooseValueOptions` 26 档里（`ChooseValueOptions.cs:1-29` 现读无 8/17）");
        var f = NewFixture();
        if (f == null) { Report(false, "C0 前提不成立：测试对局没建起来"); return; }

        int v; string gap;
        bool ok8 = Try(f, 8, 1, 2, 3, out v, out gap);
        CheckTrue(!ok8 && !string.IsNullOrEmpty(gap),
                  "C1 ★★★ `opt 8` **不被本表接管**（`false` + 出声）"
                + "｜🧨 把它交给 `default => min` ⇒ 返回 true、值 1 ⇒ 红（`Sawbonez` 的 1-2 会被压成恒 1）");
        CheckTrue(gap != null && gap.Contains("26 档"),
                  "C2 ★★ 警告文案点明「不在 26 档里」（实际：" + gap + "）");

        bool ok17 = Try(f, 17, 1, 2, 3, out v, out gap);
        CheckTrue(!ok17 && !string.IsNullOrEmpty(gap),
                  "C3 ★★★ `healthValueOptions 17` **不被本表接管**（`false` + 出声）");

        // --- 数据面：`ApplyRefCardValueOption` 对 opt 8 真的打一条警告、数值原样不动 ---
        var card = PlainCard("B141W9Opt8", "unit");
        card.AttachAbilityDamage(new[] {
            new CardDef.AbilityDamageSpec { Ab = 0, Lg = 0, Min = 1, Max = 2, ValueOptions = 8, Type = 5 } });
        var op = new EffectOp { Verb = "deal", Source = "B141W9Opt8-source", AbilityIndex = 0, LogicIndex = 0, Amount = 1, AmountMax = 2 };
        int before = f.Ctx.Events != null ? f.Ctx.Events.Count : 0;
        var got = RuleCore.ApplyRefCardValueOption(f.Ctx, op, card, 0, f.This);
        Check(got.Amount, 1, "C4 ★★ 数据面：`opt 8` 的 `Amount` **原样不动**（= 1，⛔ 不被压成 min）");
        Check(got.AmountMax, 2, "C4b ★★ 数据面：`opt 8` 的 `AmountMax` 原样不动（= 2）");
        int after = f.Ctx.Events != null ? f.Ctx.Events.Count : 0;
        bool warned = false;
        if (f.Ctx.Events != null)
            for (int i = before; i < f.Ctx.Events.Count; i++)
                if (f.Ctx.Events[i] != null && f.Ctx.Events[i].Contains("valueOptions = 8") && f.Ctx.Events[i].Contains("26 档"))
                    warned = true;
        CheckTrue(after > before && warned,
                  "C5 ★★★ **不许静默**：`ApplyRefCardValueOption` 对 `opt 8` 打了一条「不在 26 档里」的警告"
                + "｜🧨 删掉那条 `ctx.Log` ⇒ 静默 ⇒ 红");
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    sealed class Fixture
    {
        public BattleContext Ctx;
        public UnitState This, Target, Ref, Ally;
    }

    static int CostOfCard(this UnitState u)
    {
        // 便捷读法：只用于前提断言（卡片印的费用）。
        return u != null && u.Card != null ? u.Card.Cost : -1;
    }

    static Fixture NewFixture()
    {
        var heroA = new CardDef("B141W9HeroA", "B141W9HeroA", "hero", "", null, "Ultramarines", 0, 4, 30, 0, null, subtype: "Warlord");
        var heroB = new CardDef("B141W9HeroB", "B141W9HeroB", "hero", "", null, "Goff", 0, 2, 30, 0, null, subtype: "Warlord");
        var deckA = new List<CardDef> { heroA };
        var deckB = new List<CardDef> { heroB };
        for (int i = 0; i < 6; i++) { deckA.Add(PlainCard("B141W9FillA" + i, "unit")); deckB.Add(PlainCard("B141W9FillB" + i, "unit")); }
        var ctx = RuleCore.NewBattle(deckA, deckB, seed: 0, shuffle: false, cardPool: new List<CardDef>());
        if (ctx == null) return null;

        var f = new Fixture { Ctx = ctx };
        // 载体：费用 1 / 近战 3 / 血 7 / 远程 2；最大血 12（供 70 用）
        f.This = Place(ctx, 0, 3, Card("B141W9This", 1, 3, 7, 2));
        // 目标：费用 6 / 近战 4 / 血 6 / 远程 1；最大血 10（供 60 用）
        f.Target = Place(ctx, 1, 3, Card("B141W9Target", 6, 4, 6, 1));
        // 参照卡：费用 5 / 近战 3 / 血 8
        f.Ref = Place(ctx, 1, 4, Card("B141W9Ref", 5, 3, 8, 0));
        // 另一个友军（供 202 求和）
        f.Ally = Place(ctx, 0, 4, Card("B141W9Ally", 2, 1, 5, 0));
        if (f.This == null || f.Target == null || f.Ref == null || f.Ally == null) return null;
        f.This.MaxHealthBase = 12; f.This.Health = 7;
        f.Target.MaxHealthBase = 10; f.Target.Health = 6;

        f.This.AddKeyword("shuriken", 2);
        // ✅ 已复刻（2026-11-05 · B150 · `markOfChaos(330)` 是原版唯一 `_stackable == count(20)` 的 trait ⇒
        //   `UnitState.AddKeyword` 对它是「每次只 +1、不看 traitValue」（`Core/UnitState.cs:1129-1139`，判据
        //   `判据/CardTrait_stackable_真值表_第2轮.md`）⇒ 夹具必须连调 N 次，⛔ 不能写 `AddKeyword(kw, N)`）
        for (int i = 0; i < 3; i++) f.This.AddKeyword(KeywordTable.MarkOfChaos, 1);
        for (int i = 0; i < 5; i++) f.Target.AddKeyword(KeywordTable.MarkOfChaos, 1);
        for (int i = 0; i < 7; i++) f.Ally.AddKeyword(KeywordTable.MarkOfChaos, 1);
        f.Target.AddKeyword(KeywordTable.Bastion, 2);
        f.This.AddKeyword(KeywordTable.Bastion, 1);
        ctx.Players[0].Faith = 12;
        return f;
    }

    static CardDef Card(string id, int cost, int attack, int health, int ranged)
    {
        var c = new CardDef(id, id, "unit", "", "common", "Test", cost, attack, health, ranged, null,
                            subtype: "Infantry");
        c.AttachCardTraits(new string[0]);
        return c;
    }

    static CardDef PlainCard(string id, string type)
    {
        var c = new CardDef(id, id, type, "", "common", "Test", 1, 1, 20, 0, null,
                            subtype: type == "unit" ? "Infantry" : null);
        c.AttachCardTraits(new string[0]);
        return c;
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        if (ctx == null || card == null) return null;
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    // ==================================================================
    //  取值口（生产入口 = `TryResolveValueOptionForTest`）
    // ==================================================================

    static bool Try(Fixture f, int vo, int min, int max, int factor, out int value, out string gap)
    {
        return RuleCore.TryResolveValueOptionForTest(f.Ctx, vo, min, max, factor, f.This, f.Target, f.Ref, 0,
                                                     out value, out gap);
    }

    static void Eval(Fixture f, int vo, int min, int max, int factor, string expected, string msg)
    {
        int v; string gap;
        bool ok = Try(f, vo, min, max, factor, out v, out gap);
        Report(ok && v.ToString() == expected,
               msg + "（实际 = " + (ok ? v.ToString() : "未结算：" + gap) + "，期望 = " + expected + "）");
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
        if (_fail == 0) Debug.Log($"B141_W9 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B141_W9 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
