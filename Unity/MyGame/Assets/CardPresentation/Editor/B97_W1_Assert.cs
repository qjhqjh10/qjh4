// B97_W1_Assert.cs — 第九十七会话 · 执行代理 `B97_W1`
//
// ============================ 它盯的是什么 ============================
// 本批两件（判据 → `资料/完全复刻/判据/B96_战斗B103_分诊.md` §四；逐条证据 → `资料/完全复刻/报告/B97_W1.md`）：
//   · **账 832 ②**（`CardScript.ReactToUnitJammed(CardScript disenchantedCard)` 的 else 支）
//     —— `UnitState.RecordGrant` 要记**施放者**那一列，`RemoveExtrinsicEffectsFrom` 才摘得到「谁给的」。
//     判据 = `decomp_full/CardScript__ReactToUnitJammed.c:53-56` →
//            `decomp_full/CardScript__RemoveExtrinsicEffectsFrom.c:17/22/32/37/47/51/56`
//            （倒序扫 `activeEffects(+0x108)`：`extrinsic(+0x30) != 0` ∧ `enchantingCard(+0x18) != null` ∧ `== 参` ⇒ `RemoveEffect`）。
//     落点 = `UnitState.GrantRecord.Caster` / `RecordGrant(..., caster:)` / `RevertGrantsCastBy` /
//            `RemoveExtrinsicEffectsFrom` 里新加的那个 `n += RevertGrantsCastBy(...)` / `ContainsExtrinsicEffectsFrom` 也认它。
//   · **账 1053 的残余一句**（`CardScript__AddEffect.c:886-889` 那个任务块）—— `questBuff = 1` 之后还有
//     `*(int*)(param_1 + 0x270) = 0`（**载体的** `questCounter` 归零）。`B96_CR` 只做了 `questBuff`，这一句本批补上。
//     ⚠️ 同处 `:890/:891` 的 `DisplayStatusAnim(0x2ee)` / `UpdateTraitIcons` 是**表现层**，引擎侧无对应层 ⇒ 如实留缺口。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
// `RuleEngineTest.cs` 里加**一个块**（位置 = 紧接 `{ CheckB96CRCombatBits(); }` 那一行之后）：
//       // 🆕 2026-10-29（调度台代接线）：B97_W1 —— 账 832 ② `RecordGrant` 的「施放者」列 · 账 1053 的 `questCounter` 归零。
//       { CheckB97W1Grants(); }
//   🔴 **故意不用 `Step(...)`、方法名也不带 `Test`/`Report` 前缀**，理由与 `B96_CR` 那份同一条：
//     `Step(...)` 会让 `_ranCases`（`RuleEngineTest.cs:1379`）记账、`VerifyRunCaseCoverage()` 会拿**反射清单**对账，
//     而 `工具/gen_diag_runner.py` 只认「8 空格缩进的顶层语句」⇒ 两者都得跟着改。块调用**两条都不碰**（已验证可绿）。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」都写在文案里）============================
//   · `RecordGrant` 去掉 `Caster = caster` 那一格 ⇒ 1 段两条红（找不到那一条）。
//   · `RevertGrantsCastBy` 改成按 `Source` 匹配 ⇒ 1/2 段全红（那条 `Source` 故意写成 tag，不是施放者名）。
//   · `RemoveExtrinsicEffectsFrom` 里删掉 `n += RevertGrantsCastBy(...)` ⇒ 2 段红。
//   · `ContainsExtrinsicEffectsFrom` 不认 `Caster` ⇒ 3 段首条红（「先查说没有、再摘却摘得掉」）。
//   · `ApplyRevealedFlagsCore` 删掉 `carrier.QuestCounter = 0` ⇒ 4 段末条红（实得 3）。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀，见文件头）。</summary>
    public static void CheckB97W1Grants()
    {
        Section("🆕 B97_W1：账 832 ② `RecordGrant` 的「施放者」列（`enchantingCard`）· 账 1053 的 `questCounter` 归零");
        B97W1GrantCasterColumn();
        B97W1RemoveExtrinsicByCaster();
        B97W1ContainsMatchesCaster();
        B97W1QuestCounterReset();
    }

    // ==================================================================
    //  1 · `RecordGrant(..., caster: X)` 真的记下「施放者」，且 `RevertGrantsCastBy` 按它撤
    //  判据 = `CardScript__RemoveExtrinsicEffectsFrom.c:47/51`（`effect.enchantingCard(+0x18) == 参`）
    // ==================================================================
    static void B97W1GrantCasterColumn()
    {
        var card = Unit("B97W1A", 1, 2, 5);
        var ctx = Battle(new[] { card }, new[] { Unit("B97W1A2", 1, 1, 5) });
        var u = Place(ctx, 0, 1, card);
        Check(u != null, true, "前提：夹具把单位放上了棋盘");
        if (u == null) return;
        int baseAtk = u.CurrentMeleeAttack;
        Check(baseAtk, 2, "前提：卡面近战 2（期望值照 `Unit(\"B97W1A\", 1, 2, 5)` 的实参写死，不向实现反算）");

        // 关键：`Source` 故意写成**一个 tag**（不是施放者名）—— 「按 Source 撤」在这里永远撤不到。
        u.RecordGrant("attack", 3, "B97W1_TAG_A", caster: "B97W1CasterA");
        Check(u.CurrentMeleeAttack, baseAtk + 3, "前提：`RecordGrant` 的数值照常生效（base+3，与 `caster` 那一列无关）");

        int hit = 0;
        bool valueOk = false;
        foreach (var g in u.PendingGrants)
            if (g != null && g.Caster == "B97W1CasterA") { hit++; valueOk = g.Attr == "attack" && g.Value == 3; }
        Check(hit, 1, "🧨 `PendingGrants` 里恰有 1 条 `Caster == \"B97W1CasterA\"`（删掉 `Caster = caster` 那一格 ⇒ 实得 0）");
        Check(valueOk, true, "前提：那一条是 `attack +3`（否则上面那条命中的是别的账）");

        int n = u.RevertGrantsCastBy("B97W1CasterA");
        Check(n, 1, "🧨 `RevertGrantsCastBy` 按【施放者】撤 1 条（改成按 `Source` 匹配 ⇒ 实得 0：那条 `Source` 是 tag）");
        Check(u.CurrentMeleeAttack, baseAtk, "🧨 撤后数值回 base（`ApplyGrant(-1)` 把那条 `StatMod` 按引用摘掉了）");
    }

    // ==================================================================
    //  2 · `RemoveExtrinsicEffectsFrom(施放者)` 真的摘得到（= `ReactToUnitJammed` else 支的入口）
    //  判据 = `CardScript__ReactToUnitJammed.c:53-56`
    // ==================================================================
    static void B97W1RemoveExtrinsicByCaster()
    {
        var card = Unit("B97W1B", 1, 2, 5);
        var ctx = Battle(new[] { card }, new[] { Unit("B97W1B2", 1, 1, 5) });
        var u = Place(ctx, 0, 1, card);
        Check(u != null, true, "前提：夹具把单位放上了棋盘");
        if (u == null) return;
        int baseAtk = u.CurrentMeleeAttack;

        u.RecordGrant("attack", 2, "B97W1_TAG_B", caster: "B97W1SrcB");
        u.RecordGrant("attack", 5, "B97W1_TAG_C", caster: "B97W1SrcC");
        Check(u.CurrentMeleeAttack, baseAtk + 7, "前提：两条账都吃到（base+2+5）");

        int n = u.RemoveExtrinsicEffectsFrom("B97W1SrcB");
        Check(u.CurrentMeleeAttack, baseAtk + 5, "🧨 只撤了施放者 B 那一条（C 的 +5 还在）—— 删掉 `n += RevertGrantsCastBy(...)` ⇒ 实得 base+7");
        Check(n, 1, "🧨 返回 1（撤掉几条就报几条，⛔ 不虚报）");

        u.RemoveExtrinsicEffectsFrom("B97W1SrcC");
        Check(u.CurrentMeleeAttack, baseAtk, "剩余那条也能按施放者撤掉（不是只认第一条）");
    }

    // ==================================================================
    //  3 · 「先查」与「再摘」同口径（原版 `RemoveExtrinsicEffectsFrom` 是**先查再排队**）
    //  判据 = `decomp_full/BattleManager__RemoveExtrinsicEffectsFrom.c:48/73/91` 的 `ContainsExtrinsicEffectFrom(card, 0, 0)`
    // ==================================================================
    static void B97W1ContainsMatchesCaster()
    {
        var card = Unit("B97W1C", 1, 2, 5);
        var ctx = Battle(new[] { card }, new[] { Unit("B97W1C2", 1, 1, 5) });
        var u = Place(ctx, 0, 1, card);
        Check(u != null, true, "前提：夹具把单位放上了棋盘");
        if (u == null) return;

        Check(u.ContainsExtrinsicEffectsFrom("B97W1SrcD"), false, "前提：还没记任何账 ⇒ 假");
        u.RecordGrant("attack", 1, "B97W1_TAG_D", caster: "B97W1SrcD");
        Check(u.ContainsExtrinsicEffectsFrom("B97W1SrcD"), true,
              "🧨 「先查」认施放者（不认 ⇒ 实得假 ⇒ `removeEffectsFrom(47)` 那条动作根本不会入队 = 静默漏摘）");
        Check(u.ContainsExtrinsicEffectsFrom("B97W1Nobody"), false, "反例：别的名字不误报");

        u.RemoveExtrinsicEffectsFrom("B97W1SrcD");
        Check(u.ContainsExtrinsicEffectsFrom("B97W1SrcD"), false, "撤完之后「先查」为假（谓词与摘口必须同口径）");
    }

    // ==================================================================
    //  4 · 账 1053 残余：挂任务附魔那一下把**载体的** `questCounter` 归零
    //  判据 = `decomp_full/CardScript__AddEffect.c:887`（`+0x91 = 1`）/ `:888`（`*(int*)(param_1 + 0x270) = 0`）
    // ==================================================================
    static void B97W1QuestCounterReset()
    {
        var carrier = Unit("B97W1Q", 1, 1, 5);
        var ctx = Battle(new[] { carrier }, new[] { Unit("B97W1Q2", 1, 1, 5) });
        var u = Place(ctx, 0, 1, carrier);
        Check(u != null && u.Instance != null, true, "前提：夹具拿到 `UnitState` 与其 `CardInstance`");
        if (u == null || u.Instance == null) return;

        u.Instance.QuestCounter = 3;   // 先弄脏：模拟「这条任务已经累计过 3 次」（`ReachedQuestCounter` 的累计器）

        var quest = new CardDef("B97W1Quest", "B97W1Quest", "enchant", "", null, "Test",
                                0, 0, 0, 0, null, spellType: Enchantments.SpellTypeQuest);
        var eff = Enchantments.Attach(ctx, u.Instance, quest, Enchantments.BuffTypeEnchantment,
                                      Enchantments.SpellTypeQuest);
        Check(eff != null, true, "前提：任务附魔挂上了（`spellType 120`、`buffType 12` 属附魔三档）");
        if (eff == null) return;
        Check(eff.QuestBuff, true, "前提：`questBuff` 置真（`AddEffect.c:887`；既有断言在 `RuleEngineTest_EngineB96CR.cs` T2）");
        Check(u.Instance.QuestCounter, 0,
              "🧨 挂任务附魔 ⇒ 载体 `questCounter` 归零（原版 `AddEffect.c:888`；删掉 `carrier.QuestCounter = 0` ⇒ 实得 3）");
    }
}
