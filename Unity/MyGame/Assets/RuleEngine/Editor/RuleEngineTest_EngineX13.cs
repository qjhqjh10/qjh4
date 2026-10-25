// RuleEngineTest_EngineX13.cs — 引擎线 `X13`（2026-10-25 · 第七十五/七十六会话）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册 `_EngineE4` /
// `_EngineA3` / `_EngineC5` … 都在），同一时刻只有一个写手改一个文件 ⇒ 测试体全写在这里，
// `RuleEngineTest.cs` **只加 `Run()` 里一行 `Step(TestPrayEnchantmentX13)`**。
//
// 🔴 **这一件补的是什么**：`pray` 的经典语义「**未受伤 ⇒ 你下个回合开始 +1 信仰**」（官方文案
//    `CardTraitDescription/pray`）。上一轮曾把它误判成「原版没有」；真值**在卡数据里**：
//    隐藏附魔 `Pray Enchantment`（`m_Name = Z-NC Sororitas-Pray_enchantment`，`cardType 20`，
//    `path_id 1528516720421242597`）—— 23 张 `pray` 卡在 `cardAbilities[trigger=655 (Pray)]
//    .abilityLogic[0].buffEffect` 挂 `{buffType:15 singleUseEnchantment, enchantment → PE}`；
//    PE ability[0] `trigger 60 (UnitDamaged) → removeThisEnchantment`（受伤即撤）、
//    ability[1] `trigger 31 (TurnStartPlayer) → buffMana{manaAmount:1, faithMana:1} + removeThisEnchantment`。
//    判据全文 → `资料/待办判据_祈祷信仰与任务点_1025b.md` §二·④ / §四·3。
//
// ⚠️ **我们这一版是【引擎内等价实现】**（`UnitState.PrayEnchantment`）：原版那条链要吃卡数据
//    （要把 `Pray Enchantment` 补进卡池 ⇒ 会动卡池 md5）⇒ 卡数据那条另立一笔，不在本文件范围内。
//
// 🔴 每条断言都带 `🧨`（什么改动能让它红）。期望值一律写死字面量。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（`Run()` 里一行 `Step(TestPrayEnchantmentX13)`）。</summary>
    public static void TestPrayEnchantmentX13()
    {
        // 祈祷卡：正文 `Pray: Gain Shield`（沿用 `TestPrayedState` 的夹具形状 —— 它已验证能被解析）。
        // ⚠️ 它祈祷后**自带 Shield** ⇒ 要测「真挨伤害」的那一格必须显式摘盾（下面 ② 就这么做）。
        var prayer = new CardDef("FX13Prayer", "FX13Prayer", "unit", "Pray: Gain Shield",
                                 "common", "Test", 2, 1, 6, 0, new[] { "Pray" }, subtype: "Infantry");

        // ---- ① 经典档：祈祷 → 不挨打 → **自己**下回合开始 +1；敌方回合开始**不**加、也**不**摘 ----
        {
            var ctx = ProbeBattle(new CardDef[0], new[] { Unit("X13Foe", 1, 1, 9) });
            ToP1Turn(ctx, 3);
            var a = Place(ctx, 0, 2, prayer);
            int f0 = ctx.Players[0].Faith;

            CheckCode(RuleCore.UseAlternative(ctx, 0, 2, "pray"), RuleCodes.OK, "0 号格执行 `Pray`");
            CheckTrue(a.PrayEnchantment,
                      "★★ **祈祷那一刻挂上「祈祷附魔」**（原版 = 卡数据里 `trigger=655` 的 `buffEffect.enchantment`）"
                    + "｜🧨 把 `UseAlternativeCore` 里那句 `u.PrayEnchantment = true;` 删掉 ⇒ 红");
            Check(ctx.Players[0].Faith, f0,
                  "★ 经典档（`alternativeTraitLogic = false`）祈祷**不立刻加信仰**"
                + "（立刻 +1 是**冲突档**专属，见 ⑤）");

            PassTurn(ctx);      // → 对手的回合开始
            Check(ctx.Players[0].Faith, f0,
                  "★★ **敌方回合开始【不】给我们加信仰**（原版 `TurnStartPlayer(31)` 的闸是 "
                + "`card.owner == IsPlayerTurn()`，`CardScript__OnTurnStart.c:145`）"
                + "｜🧨 把 `RuleCore.BeginTurn` 里那一圈改成扫**双方** ⇒ 红");
            CheckTrue(a.PrayEnchantment,
                      "★ 敌方回合开始**不摘**（它只在「主人自己回合结算」或「受伤」时消费）");

            PassTurn(ctx);      // → 自己的下一个回合开始
            Check(ctx.Players[0].Faith, f0 + 1,
                  "★★ **未受伤 ⇒ 自己下回合开始 +1 信仰**（官方文案 `CardTraitDescription/pray`；"
                + "原版 = PE 的 `TurnStartPlayer → buffMana{faithMana:1}`）"
                + "｜🧨 删掉 `GrantPrayEnchantmentFaith` 的调用 ⇒ 红");
            CheckTrue(!a.PrayEnchantment, "★ **一次性**：结算完就摘（原版同一张 PE 上还有 `removeThisEnchantment`）");

            PassTurn(ctx); PassTurn(ctx);       // 再过整整一轮
            Check(ctx.Players[0].Faith, f0 + 1,
                  "★ 一次性：再过一个自己的回合**不再加**｜🧨 结算后不清位 ⇒ 红");
        }

        // ---- ② 经典档：祈祷 → **挨一次真伤** → 下回合开始**不** +1 ----
        {
            var ctx = ProbeBattle(new CardDef[0], new[] { Unit("X13Foe", 1, 1, 9) });
            ToP1Turn(ctx, 3);
            var b = Place(ctx, 0, 2, prayer);
            int f0 = ctx.Players[0].Faith;

            CheckCode(RuleCore.UseAlternative(ctx, 0, 2, "pray"), RuleCodes.OK, "0 号格执行 `Pray`");
            // 夹具前提：`Pray: Gain Shield` ⇒ 祈完自带盾；不摘的话这一下会被全挡下（= ③ 那一格）。
            b.RemoveAll(KeywordTable.Shield);
            RuleCore.ApplyDamage(ctx, b, 2, "自检");
            CheckTrue(!b.PrayEnchantment,
                      "★★ **真挨到伤害 ⇒ 附魔被撤**（原版 PE ability[0]：`trigger 60 UnitDamaged → removeThisEnchantment`）"
                    + "｜🧨 删掉 `ApplyDamage` 里 `u.PrayEnchantment = false;` ⇒ 红");

            PassTurn(ctx); PassTurn(ctx);
            Check(ctx.Players[0].Faith, f0,
                  "★★ **挨过真伤 ⇒ 下回合开始【不】+1**（文案那句 `unless interrupted by damage`）"
                + "｜🧨 同上 ⇒ 红");
        }

        // ---- ③ 正例：**护甲/盾挡掉的 0 伤不算「打断」** ----
        //   判据 = `CardScript__ResolveDamageDealt.c:25-28`（`damage == 0` 直接 `return`）⇒
        //   附魔的 `UnitDamaged` 不会响。我们与 `Prayed` **共用同一个闸**（`!absorbed && dmg != 0`）。
        {
            var ctx = ProbeBattle(new CardDef[0], new[] { Unit("X13Foe", 1, 1, 9) });
            ToP1Turn(ctx, 3);
            var c = Place(ctx, 0, 2, prayer);
            int f0 = ctx.Players[0].Faith;

            CheckCode(RuleCore.UseAlternative(ctx, 0, 2, "pray"), RuleCodes.OK, "0 号格执行 `Pray`");
            c.AddKeyword(KeywordTable.Shield, 1);
            RuleCore.ApplyDamage(ctx, c, 3, "自检");
            CheckTrue(c.PrayEnchantment,
                      "★★ **被 Shield 全挡下（掉 0 血）⇒ 不算挨到伤害、附魔仍在**"
                    + "｜🧨 把 `!absorbed && dmg != 0` 那道闸删掉（改成无条件清）⇒ 红");

            PassTurn(ctx); PassTurn(ctx);
            Check(ctx.Players[0].Faith, f0 + 1,
                  "★ 于是下回合照样 +1（这一格是「0 伤不打断」的**正例**，别只钉负例）");
        }

        // ---- ④ 冲突档：祈祷**立刻** +1，**并且**下回合开始**再** +1（= 文案 `prayAlt` 的 `another 1`）----
        //   立刻那一支 = `EffectResolver.FinishActiveAbility`（`alternativeTraitLogic = true` 才跑）；
        //   下回合那一支与经典**同一条**（本件新加的引擎内等价实现）。
        {
            var ctx = ProbeBattle(new CardDef[0], new[] { Unit("X13Foe", 1, 1, 9) });
            ToP1Turn(ctx, 3);
            ctx.Vars.alternativeTraitLogic = true;          // 合成冲突档（⛔ 不依赖真实下发的数据）
            var e = Place(ctx, 0, 2, prayer);
            int f0 = ctx.Players[0].Faith;

            CheckCode(RuleCore.UseAlternative(ctx, 0, 2, "pray"), RuleCodes.OK, "冲突档下执行 `Pray`");
            Check(ctx.Players[0].Faith, f0 + 1,
                  "★★ **冲突档：祈祷立刻 +1 信仰**（`FinishActiveAbility.c:11,13,19,21`）");
            CheckTrue(e.PrayEnchantment, "★ ……而且附魔**照样**挂上（两档都有这一条）");

            PassTurn(ctx); PassTurn(ctx);
            Check(ctx.Players[0].Faith, f0 + 2,
                  "★★ **冲突档：下回合开始再 +1** = 文案 `prayAlt` 的 `another 1 Faith`"
                + "｜🧨 把新加的下回合结算删掉 ⇒ 红（立刻那 +1 还在，所以只会差 1 点）");
        }
    }
}
