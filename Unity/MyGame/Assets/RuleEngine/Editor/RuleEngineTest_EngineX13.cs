// RuleEngineTest_EngineX13.cs — 引擎线 `X13`（2026-10-25 · 第七十五/七十六会话）
//                                ＋ 引擎线 `X20`（2026-10-25 · 卡数据那条链接上之后的**加严版**）
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
// 🔴 **2026-10-25 `X20`（用户拍板「允许加入」）把链路换成【真的卡数据】**：
//    `UnitState.PrayEnchantment`（bool）已删除 ⇒ 本件的入池断言改成「**附魔层里真的挂着那张
//    `Pray Enchantment` 的 `ActiveEnchantment`**」，而且卡对象**就是卡池里的那一张**
//    （`Enchantments.PrayEnchantmentCardId`）。
//    ⚠️ 夹具**必须给真卡池**（`ProbeBattleX13`）—— 旧的 `ProbeBattle` 故意传空池，用它的话
//    「挂附魔」那几条会红（那正是**新版与旧版唯一的可判别差异**）。
//    ⛔ **灭自证**：把实现退回 bool（或 `AttachPray` 改回只置 flag）⇒ 「挂着卡数据」那几条红；
//    把 `RemovePray` 删掉 ⇒ 「结算/受伤后真的移除」那几条红。
//
// 🔴 每条断言都带 `🧨`（什么改动能让它红）。期望值一律写死字面量。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（`Run()` 里一行 `Step(TestPrayEnchantmentX13)`）。</summary>
    public static void TestPrayEnchantmentX13()
    {
        // ---- 前提：卡池里真有那张附魔卡（`X20` 起整件都在验「走卡数据」）----
        var realPool = CardDatabase.Load();
        var prayCard = CardDatabase.FindById(realPool, Enchantments.PrayEnchantmentCardId);
        CheckTrue(prayCard != null,
                  "（前提）卡池里真有 `" + Enchantments.PrayEnchantmentName + "`（`id "
                + Enchantments.PrayEnchantmentCardId + "`）—— 判据 `资料/普查产出_1025/"
                + "X20_PrayEnchantment入池_执行报告.md`｜🧨 生成器不再收这张卡 ⇒ 红");
        if (prayCard == null) return;                       // 前提不成立就别往下演（免得整片假红）

        // 祈祷卡：正文 `Pray: Gain Shield`（沿用 `TestPrayedState` 的夹具形状 —— 它已验证能被解析）。
        // ⚠️ 它祈祷后**自带 Shield** ⇒ 要测「真挨伤害」的那一格必须显式摘盾（下面 ② 就这么做）。
        var prayer = new CardDef("FX13Prayer", "FX13Prayer", "unit", "Pray: Gain Shield",
                                 "common", "Test", 2, 1, 6, 0, new[] { "Pray" }, subtype: "Infantry");

        // ---- ① 经典档：祈祷 → 不挨打 → **自己**下回合开始 +1；敌方回合开始**不**加、也**不**摘 ----
        {
            var ctx = ProbeBattleX13(new CardDef[0], new[] { Unit("X13Foe", 1, 1, 9) });
            ToP1Turn(ctx, 3);
            var a = Place(ctx, 0, 2, prayer);
            int f0 = ctx.Players[0].Faith;

            CheckCode(RuleCore.UseAlternative(ctx, 0, 2, "pray"), RuleCodes.OK, "0 号格执行 `Pray`");
            var e1 = Enchantments.FindPray(a);
            CheckTrue(e1 != null,
                      "★★ **祈祷那一刻挂上「祈祷附魔」** —— 而且是**附魔层里的一个实例**"
                    + "（原版 `trigger=655` 的 `buffEffect.enchantment` → `CardScript__AddEnchantment`）"
                    + "｜🧨 退回 `UnitState.PrayEnchantment` 那种 bool 实现 ⇒ 红");
            CheckTrue(e1 != null && ReferenceEquals(e1.Card, prayCard),
                      "★★ 挂的**就是卡池里那一张** `Pray Enchantment`（同一个 `CardDef` 对象）"
                    + "｜🧨 挂一张自造的假卡 / 只置 flag ⇒ 红");
            CheckTrue(e1 != null && e1.Effect != null && e1.Effect.BuffType == 15,
                      "★ `buffType = 15 singleUseEnchantment`（原版 `buffEffect.buffType` 直搬）"
                    + "｜🧨 传 12/16 ⇒ 红");
            Check(a.Instance.ActiveEnchantments.Count, 1,
                  "★ 同一个人身上**只有一条**（`AttachPray` 幂等：祈祷两次不会叠两条）");
            Check(ctx.Players[0].Faith, f0,
                  "★ 经典档（`alternativeTraitLogic = false`）祈祷**不立刻加信仰**"
                + "（立刻 +1 是**冲突档**专属，见 ④）");

            PassTurn(ctx);      // → 对手的回合开始
            Check(ctx.Players[0].Faith, f0,
                  "★★ **敌方回合开始【不】给我们加信仰**（原版 `TurnStartPlayer(31)` 的闸是 "
                + "`card.owner == IsPlayerTurn()`，`CardScript__OnTurnStart.c:145`）"
                + "｜🧨 把 `RuleCore.BeginTurn` 里那一圈改成扫**双方** ⇒ 红");
            CheckTrue(Enchantments.HasPray(a),
                      "★ 敌方回合开始**不摘**（它只在「主人自己回合结算」或「受伤」时消费）");

            PassTurn(ctx);      // → 自己的下一个回合开始
            Check(ctx.Players[0].Faith, f0 + 1,
                  "★★ **未受伤 ⇒ 自己下回合开始 +1 信仰**（官方文案 `CardTraitDescription/pray`；"
                + "原版 = PE 的 `TurnStartPlayer → buffMana{faithMana:1}`）"
                + "｜🧨 删掉 `GrantPrayEnchantmentFaith` 的调用 ⇒ 红");
            CheckTrue(!Enchantments.HasPray(a),
                      "★★ **一次性**：结算完**真的从附魔层移除**（原版同一张 PE 上的 "
                    + "`removeThisEnchantment`）｜🧨 只 `Faith += 1` 不 `RemovePray` ⇒ 红");
            Check(a.Instance.ActiveEnchantments.Count, 0,
                  "★ 移除之后附魔表**清空**（不是「留着但标记已用」—— 原版就是摘掉）");

            PassTurn(ctx); PassTurn(ctx);       // 再过整整一轮
            Check(ctx.Players[0].Faith, f0 + 1,
                  "★ 一次性：再过一个自己的回合**不再加**｜🧨 结算后不摘（留着会每个回合都发）⇒ 红");
            CheckTrue(!Enchantments.HasPray(a), "★ ……而且不会重新挂上（没有再次祈祷）");
        }

        // ---- ② 经典档：祈祷 → **挨一次真伤** → 下回合开始**不** +1 ----
        {
            var ctx = ProbeBattleX13(new CardDef[0], new[] { Unit("X13Foe", 1, 1, 9) });
            ToP1Turn(ctx, 3);
            var b = Place(ctx, 0, 2, prayer);
            int f0 = ctx.Players[0].Faith;

            CheckCode(RuleCore.UseAlternative(ctx, 0, 2, "pray"), RuleCodes.OK, "0 号格执行 `Pray`");
            CheckTrue(Enchantments.HasPray(b), "（前提）附魔挂上了");
            // 夹具前提：`Pray: Gain Shield` ⇒ 祈完自带盾；不摘的话这一下会被全挡下（= ③ 那一格）。
            b.RemoveAll(KeywordTable.Shield);
            RuleCore.ApplyDamage(ctx, b, 2, "自检");
            CheckTrue(!Enchantments.HasPray(b),
                      "★★ **真挨到伤害 ⇒ 附魔被摘掉**（原版 PE ability[0]："
                    + "`trigger 60 UnitDamaged → removeThisEnchantment`）"
                    + "｜🧨 删掉 `ApplyDamage` 里的 `Enchantments.RemovePray` ⇒ 红");
            Check(b.Instance.ActiveEnchantments.Count, 0, "★ ……附魔表清空");

            PassTurn(ctx); PassTurn(ctx);
            Check(ctx.Players[0].Faith, f0,
                  "★★ **挨过真伤 ⇒ 下回合开始【不】+1**（文案那句 `unless interrupted by damage`）"
                + "｜🧨 同上 ⇒ 红");
        }

        // ---- ③ 正例：**护甲/盾挡掉的 0 伤不算「打断」** ----
        //   判据 = `CardScript__ResolveDamageDealt.c:25-28`（`damage == 0` 直接 `return`）⇒
        //   附魔的 `UnitDamaged` 不会响。我们与 `Prayed` **共用同一个闸**（`!absorbed && dmg != 0`）。
        {
            var ctx = ProbeBattleX13(new CardDef[0], new[] { Unit("X13Foe", 1, 1, 9) });
            ToP1Turn(ctx, 3);
            var c = Place(ctx, 0, 2, prayer);
            int f0 = ctx.Players[0].Faith;

            CheckCode(RuleCore.UseAlternative(ctx, 0, 2, "pray"), RuleCodes.OK, "0 号格执行 `Pray`");
            c.AddKeyword(KeywordTable.Shield, 1);
            RuleCore.ApplyDamage(ctx, c, 3, "自检");
            CheckTrue(Enchantments.HasPray(c),
                      "★★ **被 Shield 全挡下（掉 0 血）⇒ 不算挨到伤害、附魔仍在**"
                    + "｜🧨 把 `!absorbed && dmg != 0` 那道闸删掉（改成无条件摘）⇒ 红");

            PassTurn(ctx); PassTurn(ctx);
            Check(ctx.Players[0].Faith, f0 + 1,
                  "★ 于是下回合照样 +1（这一格是「0 伤不打断」的**正例**，别只钉负例）");
            CheckTrue(!Enchantments.HasPray(c), "★ 而且结算之后照样摘掉");
        }

        // ---- ④ 冲突档：祈祷**立刻** +1，**并且**下回合开始**再** +1（= 文案 `prayAlt` 的 `another 1`）----
        //   立刻那一支 = `EffectResolver.FinishActiveAbility`（`alternativeTraitLogic = true` 才跑）；
        //   下回合那一支与经典**同一条**（`X20` 起 = PE 那张卡的 `TurnStartPlayer` 能力）。
        {
            var ctx = ProbeBattleX13(new CardDef[0], new[] { Unit("X13Foe", 1, 1, 9) });
            ToP1Turn(ctx, 3);
            ctx.Vars.alternativeTraitLogic = true;          // 合成冲突档（⛔ 不依赖真实下发的数据）
            var e = Place(ctx, 0, 2, prayer);
            int f0 = ctx.Players[0].Faith;

            CheckCode(RuleCore.UseAlternative(ctx, 0, 2, "pray"), RuleCodes.OK, "冲突档下执行 `Pray`");
            Check(ctx.Players[0].Faith, f0 + 1,
                  "★★ **冲突档：祈祷立刻 +1 信仰**（`FinishActiveAbility.c:11,13,19,21`）");
            CheckTrue(Enchantments.HasPray(e), "★ ……而且附魔**照样**挂上（两档都有这一条）");
            Check(e.Instance.ActiveEnchantments.Count, 1, "★ 冲突档也只有一条（立刻 +1 不是第二条附魔）");

            PassTurn(ctx); PassTurn(ctx);
            Check(ctx.Players[0].Faith, f0 + 2,
                  "★★ **冲突档：下回合开始再 +1** = 文案 `prayAlt` 的 `another 1 Faith`"
                + "｜🧨 把 PE 的下回合结算删掉 ⇒ 红（立刻那 +1 还在，所以只会差 1 点）");
            CheckTrue(!Enchantments.HasPray(e), "★ 冲突档同样一次性（结算即摘）");
        }
    }

    /// <summary>
    /// `X20` 夹具：**给真卡池** —— `ProbeBattle` 故意传 `new List&lt;CardDef&gt;()`（大多数老用例不需要池），
    /// 而「挂那张 `Pray Enchantment`」需要卡池里真有它。
    /// ⚠️ 只在 `NewBattle` **之后**赋值（`CardPool` 是 public 字段，`NewBattle` 期间没人读它）。
    /// </summary>
    static BattleContext ProbeBattleX13(CardDef[] hand0, CardDef[] hand1)
    {
        var ctx = ProbeBattle(hand0, hand1);
        ctx.CardPool = CardDatabase.Load();
        return ctx;
    }
}
