// RuleEngineTest_EngineB94B2.cs — 第九十四会话 · 波 2 · 执行代理 `B2`（引擎小文件族 · UnitState.cs / CardDef.cs）
//
// 为什么另开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆），
// 同一时刻只有一个写手改 `RuleEngineTest.cs` ⇒ 本批断言全写在这里、**只交一个入口方法名**
// `CheckB94B2EngineGaps`，由调度台在 `Run()` 里加一行（⛔ 本代理不改 F1 的文件）。
//
// 本批覆盖 `账/战斗逻辑驱动.tsv` 里落在 `UnitState.cs`(8) / `CardDef.cs`(6) 的 14 行：
//   UnitState：L64 · L533 · L636 · L707 · L863 · L866 · L1041 · L1376
//   CardDef  ：L720 · L867 · L994 · L1026 · L1217 · L1379
// 判据一律 `d:/2/tools/decomp_full/`（🥇）+ `out_cpp2il_dev_ilspy`（🥈），2026-10-29 逐句现读。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）；期望值写死字面量，⛔ 不从被测实现反算。
using System;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（调度台在 `Run()` 里加一行）。</summary>
    public static void CheckB94B2EngineGaps()
    {
        Section("🆕 B2（B94 波 2）：光环挂/收 fast·flank · 主动能力三源闸 · 记录式摘 trait · 重置七本账 · 按来源撤 · trigger 名表");
        B94B2T1AuraFastFlankActBits();
        B94B2T2ActiveAbilityThreeSources();
        B94B2T3SilentTraitRemovalAndRecordGate();
        B94B2T4ResetToValuesInHandBooks();
        B94B2T5RemoveBySource();
        B94B2T6AbilityTriggerNameTable();
        B94B2T7ApplyTraitEffectAndCardSetup();
    }

    // ==============================================================================================
    //  ① 光环「挂上 / 收回」`fast`~`flank` 的那两位（账 L1376）
    //     判据：挂 = `CardScript__AddEffect.c:570-572`（`trait ∈ {0x28 fast, 0x1cc flank}` ⇒
    //           `UpdateSummonSicknessAnims` ⇒ `ActivateMinion` 重写 canAct/canAttack）；
    //           收 = `RemoveExtrinsicEffectsFrom.c:44` → `RemoveEffect` → `RemoveTraitSilently`
    //           （`RemoveTraitSilently.c` 全文 **0 次** `UpdateSummonSicknessAnims`）。
    // ==============================================================================================

    static void B94B2T1AuraFastFlankActBits()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        var u = new UnitState(ctx.NewInstance(Unit("B94B2Aura", 1, 1, 3)), false);

        // 前提（硬检查）：部署当回合、没有 fast/flank ⇒ 召唤病 ⇒ 不能动（`Exhausted` = `!canAct`）。
        RuleCore.ApplyDeployTurnState(u);
        CheckTrue(u.SummonSickness, "① （前提）`ApplyDeployTurnState` 把召唤病 `+0x58` 置真");
        CheckTrue(u.Exhausted,
                  "① （前提）无 fast/flank ⇒ `Exhausted` = true（`RecomputeActAndAttack` 按 `displaySummonSickness` 写）");

        // ①-a 光环**挂上** `flank` ⇒ 立刻可动（原版 `AddEffect.c:570-572` 那一跳）。
        u.AddAuraKeyword("flank", 1);
        CheckTrue(RuleCore.SummonSicknessExempt(u), "① （前提）`SummonSicknessExempt` 认 `flank`");
        CheckTrue(!u.Exhausted,
                  "★★ ① 光环挂上 `flank` ⇒ **立刻**重算两位、当场能动（`AddEffect.c:570-572`）"
                + "｜🧨 删掉 `UnitState.AddAuraKeyword` 尾巴那句 `RecomputeActAfterAuraKeyword(keyword)` "
                + "⇒ 停在「不能动」⇒ 红");

        // ①-b `Auras.Recompose` 的「① 摘 → ③ 挂回」形状 **不许**按中间态误判
        //      （= `W18AR` 那条修法的核心：周期内摘不收，由 `EndAuraRecompute` 按前后差判）。
        u.BeginAuraRecompute();
        u.ClearAuraGrants();
        u.AddAuraKeyword("flank", 1);
        u.EndAuraRecompute();
        CheckTrue(!u.Exhausted,
                  "★★ ① 整趟重算里「摘了又原样挂回」⇒ 豁免**净零** ⇒ 两位保持「能动」"
                + "（原版也只在**真丢**时才会被 `RemoveTrait` 重算；摘了又挂回不是那种局面）"
                + "｜🧨 让 `EndAuraRecompute` 无条件 `RecomputeActAndAttack`（不看 `_auraExemptBefore`）⇒ 红");

        // ①-c 豁免**真丢**（光环来源离场）⇒ 我们**当场**重算。
        //      ⚠️ 这一条是**如实记录我们比原版严**的位置：原版这一刻走 `RemoveTraitSilently`（不重算），
        //      要等到 `BattleManager__FinishResolvingAction.c:105/115 → MinionManager__ActivateMinions`
        //      或下一次 `OnTurnStart` 才改写。我们**没有**那个「每动作结束对行动方 ActivateMinions」的钩子
        //      （`RuleCore.BroadcastActivateMinions` 只在回合开始调一次）⇒ 删掉这句会从「比原版严」
        //      变成「比原版松且永远补不回来」。⇒ **保留**，差异记在账 L1376 的差异列。
        u.BeginAuraRecompute();
        u.ClearAuraGrants();
        u.EndAuraRecompute();
        CheckTrue(!u.Has("flank"),
                  "① （前提）豁免真丢后再也查不到 `flank`");
        CheckTrue(RuleCore.SummonSicknessExempt(u) == false, "① （前提）`SummonSicknessExempt` = false");
        CheckTrue(u.Exhausted,
                  "① 豁免真丢 ⇒ 我们**当场**重算成「不能动」（原版要等到动作结束/新回合）—— "
                + "如实标：**我们比原版严**，保留的理由见本段注释"
                + "｜🧨 删掉 `EndAuraRecompute` 里那句 `RecomputeActAndAttack` ⇒ 实得 false ⇒ 这一条红");
    }

    // ==============================================================================================
    //  ② 主动能力「能不能用」的三源闸（账 L1379 / L720 / L867）
    //     原版 = **一个** `hasActiveAbility(+0x220)` 字段，写点六处：
    //       `SetBaseCardTraits.c:37` / `Show…ForTutorial.c` / `RemoveEffect.c:271`（三处**从 `+0x2a9` 恢复**）
    //       `AddEffect.c:250`（=0x101）· `CheckReplacedAbility.c:24`（=1）· `Hide…ForTutorial.c` / `ReactToUnitJammed.c:153`（=0）
    //     我们 = 卡面列 `CardDef.HasActiveAbility` + 两个实例位（教程关闸 / jam 压制）。
    // ==============================================================================================

    static void B94B2T2ActiveAbilityThreeSources()
    {
        // ── 卡面列（= 原版 `RawCardScript.hasActiveAbility @+0x2a9`，`SetBaseCardTraits.c:37` 灌）──
        var cardOn = new CardDef("B94B2AA1", "B94B2AA1", "unit", "", null, "Test", 1, 1, 3, 0,
                                 hasActiveAbility: 1);
        var cardOff = new CardDef("B94B2AA0", "B94B2AA0", "unit", "", null, "Test", 1, 1, 3, 0,
                                  hasActiveAbility: 0);
        var uOn = new UnitState(CardInstance.Detached(cardOn), false);
        var uOff = new UnitState(CardInstance.Detached(cardOff), false);

        CheckTrue(uOn.ActiveAbilityAvailable, "② 卡面 `hasActiveAbility = 1` ⇒ 可用");
        CheckTrue(!uOff.ActiveAbilityAvailable, "② 卡面 `hasActiveAbility = 0` ⇒ 不可用");

        // ── 教程关闸：Hide = `+0x220 = 0`；Show = **从卡面 `+0x2a9` 恢复**（⛔ 不是无条件置 1）──
        uOn.HideWarlordActiveAbilityForTutorial();
        CheckTrue(!uOn.ActiveAbilityAvailable,
                  "★★ ② `Hide…ForTutorial`（`CardScript__HideWarlordActiveAbilityForTutorial.c` 全文 = `+0x220 = 0`）"
                + " ⇒ 不可用｜🧨 让 `Hide…` 什么都不做 / 写成别的字段 ⇒ 实得 true ⇒ 红");
        uOn.ShowWarlordActiveAbilityForTutorial();
        CheckTrue(uOn.ActiveAbilityAvailable,
                  "★★ ② `Show…ForTutorial` = **从卡面那一列恢复**（`Show….c` 全文一句 `+0x220 = rawCard+0x2a9`）"
                + "⇒ 卡面 1 ⇒ 可用｜🧨 把 Show 写成 `= 1` 的常量 ⇒ 下面那条反向断言红");
        uOff.ShowWarlordActiveAbilityForTutorial();
        CheckTrue(!uOff.ActiveAbilityAvailable,
                  "★★ ② 卡面 **0** 的卡 Show 之后**仍然不可用** —— 这就是「恢复」与「置 1」的分水岭"
                + "（原版 `+0x220 = rawCardData+0x2a9`；⛔ 若写成无条件 1，那 45 张有 `activeAbilityLogic` "
                + "却 `hasActiveAbility=0` 的卡会被好心点亮）"
                + "｜🧨 Show 写成 `ActiveAbilityHidden = false` **之外**再置任何「强制可用」位 ⇒ 红");

        // ── jam 的压制位（原版 `ReactToUnitJammed.c:153`：`cardType != Hero ⇒ +0x220 = 0`）──
        var uJam = new UnitState(CardInstance.Detached(cardOn), false);
        bool died = uJam.ApplyJam();
        CheckTrue(!died, "② （前提）3 血单位被 jam 不当场死");
        CheckTrue(uJam.ActiveAbilitySuppressed, "② （前提）`ApplyJam` 对**非督军**置压制位");
        CheckTrue(!uJam.ActiveAbilityAvailable,
                  "★★ ② 被 jam 的非督军 ⇒ **不可用**（原版 `+0x220` 被写 0、`CanUseAbilityNow.c:26` 第一句就查它）"
                + "｜🧨 `ActiveAbilityAvailable` 不读 `ActiveAbilitySuppressed`（= 2026-10-28 那版）⇒ 实得 true ⇒ 红；"
                + "改前那条「jam 压制位另算」全仓**没有落点**，压制位是死位");

        // ── 督军除外（同一句的 `cardType != Hero` 闸）──
        var uWarlord = new UnitState(CardInstance.Detached(cardOn), true);
        uWarlord.ApplyJam();
        CheckTrue(!uWarlord.ActiveAbilitySuppressed,
                  "★★ ② **督军**被 jam **不压低**主动能力（`ReactToUnitJammed.c` 那句 `cardType != Hero` 的闸）"
                + "｜🧨 去掉 `if (!IsWarlord)` ⇒ 实得 true ⇒ 红");
        CheckTrue(uWarlord.ActiveAbilityAvailable, "② 督军 jam 后仍可用（卡面 1 ∧ 未隐藏 ∧ 未压制）");
    }

    // ==============================================================================================
    //  ③ `RemoveTraitSilently` 的「不重算」+ 记录侧的 `AnyEffectGrantsTrait` 第二道闸（账 L863 / L1041）
    // ==============================================================================================

    static void B94B2T3SilentTraitRemovalAndRecordGate()
    {
        // ── L863：silent 摘除**不重算** act 两位；非 silent（= 原版 `RemoveTrait`）**要**重算 ──
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var u = new UnitState(ctx.NewInstance(Unit("B94B2Silent", 1, 1, 3)), false);
            RuleCore.ApplyDeployTurnState(u);          // 召唤病 ⇒ Exhausted = true
            u.AddKeyword("flank", 1);       // 加词那一路会重算 ⇒ Exhausted = false
            CheckTrue(!u.Exhausted, "③ （前提）挂上 `flank` 后可动");

            u.RemoveAll("flank", silent: true);
            CheckTrue(!u.Exhausted,
                      "★★ ③ `RemoveAll(silent: true)`（= `CardScript.RemoveTraitSilently`）**不重算** —— "
                    + "两位停在旧值（判据：`CardScript__RemoveTraitSilently.c` 全文 **0 次** `UpdateSummonSicknessAnims`，"
                    + "而真移除 `CardScript__RemoveTrait.c:66-68` 才会调）"
                    + "｜🧨 把 `RemoveAll` 里 `!silent` 那个守卫删掉 ⇒ 实得 true ⇒ 红");

            u.AddKeyword("flank", 1);
            u.RemoveAll("flank", silent: false);
            CheckTrue(u.Exhausted,
                      "★★ ③ 非 silent 摘除（= 原版 `RemoveTrait`）**要**重算 ⇒ 又变「不能动」（`RemoveTrait.c:66-68`）"
                    + "｜🧨 让非 silent 也不重算 ⇒ 实得 false ⇒ 红");
        }

        // ── L1041：`AnyEffectGrantsTrait` —— 还有别的记录在给同一条 trait ⇒ 关键词留着 ──
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var u = new UnitState(ctx.NewInstance(Unit("B94B2Rec", 1, 1, 3)), false);
            // 两条记录：`TimingBits` 不同 ⇒ 合并键不同 ⇒ 不会并成一条（`FindTraitEffect` 逐键全比）。
            var r1 = u.ApplyTraitEffect("flank", 1, timingBits: 0);
            var r2 = u.ApplyTraitEffect("flank", 1, timingBits: 1);
            CheckTrue(r1 != null && r2 != null && !ReferenceEquals(r1, r2), "③ （前提）两条独立记录");
            Check(u.TraitEffects.Count, 2, "③ （前提）记录账上两条");

            u.RemoveTraitEffect(r1);
            CheckTrue(u.Has("flank"),
                      "★★ ③ 摘掉**一条**记录、另一条还在给 ⇒ 关键词**留着**"
                    + "（原版 `RemoveEffect.c:63-72` 的第二道闸 `!HasCurrentTraitActiveEffect(trait)`）"
                    + "｜🧨 删掉 `RemoveTraitEffect` 里 `AnyEffectGrantsTrait` 那一支 ⇒ 关键词被摘光 ⇒ 实得 false ⇒ 红");
            u.RemoveTraitEffect(r2);
            CheckTrue(!u.Has("flank"), "③ 记录全摘光 ⇒ 关键词才真的没了");
        }

        // ── L1041 的**残骸闸**：`!isRemnant ‖ isLeavingRemnant`（是「或」不是「且」）──
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var u = new UnitState(ctx.NewInstance(Unit("B94B2Rem", 1, 1, 3)), false);
            u.ApplyTraitEffect("flank", 1);
            CheckTrue(u.AnyEffectGrantsTrait("flank"), "③ （前提）非残骸 ⇒ 认这条记录");

            u.IsRemnant = true;
            CheckTrue(!u.AnyEffectGrantsTrait("flank"),
                      "★★ ③ **残骸**（且不在离场窗口）⇒ 整段跳过、恒 false"
                    + "（`EntityScript__HasCurrentTraitActiveEffect.c` 的门 = `isRemnant==0 || isLeavingRemnant!=0`）"
                    + "｜🧨 把门写成「且」或干脆不判 ⇒ 实得 true ⇒ 红");
            u.IsLeavingRemnant = true;
            CheckTrue(u.AnyEffectGrantsTrait("flank"),
                      "★★ ③ `isLeavingRemnant` ⇒ **不受残骸闸**（同一句的「或」的另一半）"
                    + "｜🧨 只判 `isRemnant` 就 return ⇒ 实得 false ⇒ 红");
        }
    }

    // ==============================================================================================
    //  ④ `ResetToValuesInHand` 清的七本账（账 L866）
    //     判据：`CardScript__ResetToValuesInHand.c:24-45`（两张表整表 Clear + `+0x270 = 0`）、
    //           `:131`（`+0x48/+0x4c/+0x50` 清）、`:137`（`clearBacklash` 才清 `+0x222`）、
    //           `:141-146`（摘 jam/stun/blind/stealth）、`:154`（`+0x55` 两字节 ⇒ stunned + blinded 一起清）
    // ==============================================================================================

    static void B94B2T4ResetToValuesInHandBooks()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        var inst = ctx.NewInstance(Unit("B94B2Reset", 1, 2, 4, KeywordTable.Armour));
        var u = new UnitState(inst, false);
        u.ApplyTraitEffect("flank", 1);
        u.AddKeyword(KeywordTable.Jam, 1);
        inst.QuestCounter = 3;
        inst.ActiveEffects.Add(new ActiveCardEffect { BuffType = 12 });
        inst.ActiveEnchantments.Add(new ActiveEnchantment { Card = u.Card });
        u.StunnedAtStartOfTurn = true;
        u.BlindedAtStartOfTurn = true;
        u.RemoveMarkerlightAtEndOfTurn = true;
        u.AlreadyTriggeredBacklash = true;
        u.UsedActiveAbilityThisTurn = true;

        // 前提（硬检查）：这些东西**现在真的有**，否则下面的「清干净了」是空转。
        Check(inst.ActiveEffects.Count, 1, "④ （前提）`ActiveEffects` 有 1 条");
        Check(inst.ActiveEnchantments.Count, 1, "④ （前提）`ActiveEnchantments` 有 1 条");
        CheckTrue(u.Has(KeywordTable.Jam), "④ （前提）身上挂着 `jam`");

        u.ResetToValuesInHand(clearBacklash: true);

        Check(inst.QuestCounter, 0,
              "★★ ④ `questCounter(+0x270)` 清 0（`.c:38`）｜🧨 删掉那一句 ⇒ 实得 3 ⇒ 红");
        Check(inst.ActiveEffects.Count, 0,
              "★★ ④ 手牌那份 `activeEffects(+0x108)` 整表清（`.c:24-33`）｜🧨 删掉 ⇒ 实得 1 ⇒ 红");
        Check(inst.ActiveEnchantments.Count, 0,
              "★★ ④ `activeEnchantments(+0x110)` 整表清（`.c:34-43`）｜🧨 删掉 ⇒ 实得 1 ⇒ 红");
        CheckTrue(!u.AnyEffectGrantsTrait("flank"),
                  "★★ ④ `_traitEffects`（同属 `+0x108`）也清 ⇒ 记录没了"
                + "｜🧨 删掉 `_traitEffects.Clear()` ⇒ 实得「还有记录」⇒ 红");
        CheckTrue(!u.Has(KeywordTable.Jam), "★★ ④ 摘 `jam(0x82)`（`.c:141`）｜🧨 删掉那行 ⇒ 红");
        CheckTrue(!u.StunnedAtStartOfTurn && !u.BlindedAtStartOfTurn,
                  "★★ ④ `+0x55` 写 2 字节 ⇒ `stunnedAtStartOfTurn` **＋** `blindedAtStartOfTurn` 一起清"
                + "（`dump.cs:21985/21987`）｜🧨 只清 blinded（旧版）⇒ `StunnedAtStartOfTurn` 仍 true ⇒ 红");
        CheckTrue(!u.RemoveMarkerlightAtEndOfTurn, "★★ ④ `+0x57`（`removeMarkerlightAtEndOfTurn`）清 0");
        CheckTrue(!u.UsedActiveAbilityThisTurn, "★★ ④ `+0x50`（`usedActiveAbilityTimesPerTurn`）清 0（`.c:131`）");
        CheckTrue(!u.AlreadyTriggeredBacklash,
                  "★★ ④ `clearBacklash = true` ⇒ `+0x222` 清 0（`.c:137-139`）"
                + "｜🧨 去掉那个 `if (clearBacklash)` ⇒ 下面反向那条红");
        Check(u.Attack, 2, "④ 攻回卡面");
        Check(u.Health, 4, "④ 血回卡面（`currentHealth = 卡面血`，`.c:52-59`）");
        Check(u.MaxHealth, 4, "④ 血上限回卡面");
        Check(u.Armor, 1, "④ 护甲回卡面（`SetBaseCardTraits` 那一跳）");

        // 反向（灭自证）：`clearBacklash = false` ⇒ 那一位**必须留着**（原版只在非 0 时清）。
        var inst2 = ctx.NewInstance(Unit("B94B2Reset2", 1, 2, 4));
        var u2 = new UnitState(inst2, false);
        u2.AlreadyTriggeredBacklash = true;
        u2.ResetToValuesInHand(clearBacklash: false);
        CheckTrue(u2.AlreadyTriggeredBacklash,
                  "★★ ④ `clearBacklash = false` ⇒ `alreadyTriggeredBacklash` **留着**"
                + "（`.c:137` 的 `if (param_2 != 0)`）｜🧨 无条件清 ⇒ 实得 false ⇒ 红");
    }

    // ==============================================================================================
    //  ⑤ 按来源撤（账 L533 / L636）—— 原版 `RemoveExtrinsicEffectsFrom(enchantingCard)` 的语义是
    //     「只摘**这一张来源卡**给的那批 extrinsic」，⛔ 不是「整场全场重算」。
    // ==============================================================================================

    static void B94B2T5RemoveBySource()
    {
        // ── L533：`RevertGrantsFrom(source)` 只撤**那一个来源**的「关键词带出来的属性增益」──
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var u = new UnitState(ctx.NewInstance(Unit("B94B2Grant", 1, 3, 5)), false);
            u.RecordGrant("attack", 2, "srcA");
            u.RecordGrant("attack", 1, "srcB");
            Check(u.Attack, 6, "⑤ （前提）3 + 2 + 1 = 6");

            u.RevertGrantsFrom("srcA");
            Check(u.Attack, 4,
                  "★★ ⑤ `RevertGrantsFrom(\"srcA\")` 只撤 A 的 +2、B 的 +1 留着（= 原版按 `enchantingCard` 撤）"
                + "｜🧨 改成「清全部」⇒ 实得 3 ⇒ 红；改成「撤错来源」⇒ 实得 4 但下面那条红");
            u.RevertGrantsFrom("srcB");
            Check(u.Attack, 3, "★★ ⑤ 再撤 B ⇒ 回卡面 3｜🧨 漏撤一条 ⇒ 实得 4 ⇒ 红");
            Check(u.PendingGrants.Count, 0, "⑤ 两批增益账都清干净了");
        }

        // ── L636：`RemoveExtrinsicEffectsFrom(卡名)` 撤「来源 = 那张卡」的修饰（含生命 ⇒ 夹上限）──
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var u = new UnitState(ctx.NewInstance(Unit("B94B2Extr", 1, 2, 4)), false);
            u.AddStatMod("attack", 3, "B94CardA", extrinsic: true);
            u.AddStatMod("attack", 1, "B94CardB", extrinsic: true);
            Check(u.Attack, 6, "⑤ （前提）2 + 3 + 1 = 6");

            int n = u.RemoveExtrinsicEffectsFrom("B94CardA");
            Check(n, 1, "★★ ⑤ `RemoveExtrinsicEffectsFrom(\"B94CardA\")` 摘掉 **1** 条"
                       + "｜🧨 按「全部 extrinsic」摘 ⇒ 实得 2 ⇒ 红");
            Check(u.Attack, 3,
                  "★★ ⑤ 只剩 B 的 +1 ⇒ 2 + 1 = 3（原版第三趟带 `actingCard` 实参那一跳的语义）"
                + "｜🧨 见上 ⇒ 红");
        }

        // ── `RemoveStatModsFrom(source)`：整份按来源摘（L636 的另一半，`Aura` 那条路在用）──
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var u = new UnitState(ctx.NewInstance(Unit("B94B2Mods", 1, 1, 2)), false);
            u.AddStatMod("health", 2, "srcH");
            u.AddStatMod("health", 3, "srcOther");
            Check(u.MaxHealth, 7, "⑤ （前提）卡面 2 + 2 + 3 = 7");
            u.RemoveStatModsFrom("srcH");
            Check(u.MaxHealth, 5, "★★ ⑤ `RemoveStatModsFrom(\"srcH\")` 只摘那一个来源 ⇒ 卡面 2 + 3 = **5**"
                                + "｜🧨 摘错来源 ⇒ 实得 7 ⇒ 红");
        }
    }

    // ==============================================================================================
    //  ⑥ `AbilityTrigger` 名 → 数值（账 L994 / L1217）
    //     判据：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs`（97 条，逐条照抄；
    //     2026-10-29 用脚本对过 —— **97/97 全等、0 缺 0 多 0 错**）。
    //     `GetTraitFromAbilityTrigger` 的三个硬编码（`CardTraitCollection__GetTraitFromAbilityTrigger.c`）：
    //       0 → 10（rally）· 0x50=80 → 0x78=120（backlash）· 0x51=81 → 0x3f2=1010（unstable）
    // ==============================================================================================

    static void B94B2T6AbilityTriggerNameTable()
    {
        Check(CardDef.AbilityTriggerNumber("ThisCardPlayed"), 0, "⑥ `ThisCardPlayed` = 0（枚举首项）");
        Check(CardDef.AbilityTriggerNumber("WhileInPlay"), 20,
              "★★ ⑥ `WhileInPlay` = **20** —— 它是 `extrinsic` 的判据值（`UnitState.IsExtrinsicSource` 的反面）"
            + "｜🧨 抄错一位 ⇒ 整族光环被当成非 extrinsic ⇒ 红");
        Check(CardDef.AbilityTriggerNumber("WhileInPlayVariable"), 240, "⑥ `WhileInPlayVariable` = 240（extrinsic 的另一档）");
        Check(CardDef.AbilityTriggerNumber("Unstable"), 81, "⑥ `Unstable` = 81（那个硬编码特例的入参）");
        Check(CardDef.AbilityTriggerNumber("UnitDeathByPoison"), 170, "⑥ `UnitDeathByPoison` = 170");
        Check(CardDef.AbilityTriggerNumber("TurnSetup"), 155, "⑥ `TurnSetup` = 155（账 L816 那一行引的值）");
        Check(CardDef.AbilityTriggerNumber("Ferocity"), 725,
              "⑥ `Ferocity` = 725（尾段那批高值，证明表**没有在 700 之前被截断**）");
        Check(CardDef.AbilityTriggerNumber("Oath"), 730, "⑥ `Oath` = 730（表尾最后一条）");
        Check(CardDef.AbilityTriggerNumber("NoSuchTriggerName"), -1,
              "★★ ⑥ 认不出 ⇒ **-1**，⛔ 不是 0（0 = `ThisCardPlayed`，猜 0 会让每张卡都「打出时响应」）"
            + "｜🧨 把兜底改成 0 ⇒ 实得 -1 那条红");

        // 三个硬编码 trait 在我们的词表里有同名的规范化词（= 原版「按名字」那把钥匙的两侧）。
        Check(KeywordTable.Rally, "rally", "⑥ 硬编码 trait `rally`（0 → DefinedTrait 10）在词表里");
        Check(KeywordTable.Backlash, "backlash", "⑥ 硬编码 trait `backlash`（80 → 120）");
        Check(KeywordTable.Unstable, "unstable",
              "★★ ⑥ 硬编码 trait `unstable`（81 → **1010 = 0x3f2**）—— 池内 18 张 `cardTraits._trait==1010`，"
            + "「三个视图同一集合」（`cardTraits` / `abilityTriggers` 含 81 / `keywords` 含 `unstable`）"
            + "｜🧨 词表里没有 `unstable` ⇒ 那 18 张的徽标脉冲接不上 ⇒ 红");
    }

    // ==============================================================================================
    //  ⑦ `ApplyTraitEffect` 的合并语义（账 L64）+ `UnitState` 构造 = `CardSetup` 的引擎半边（账 L707）
    //     + `CardDef.Ability` 是唯一来源（账 L1026）
    // ==============================================================================================

    static void B94B2T7ApplyTraitEffectAndCardSetup()
    {
        // ── L64：`AdjustCardEffect` 的闸是 buffType ∈ {1,2,3,10,70,85,90,95}，过闸后**只乘五对数值**、
        //      ⛔ 不碰 `traitValue(+0x40)` ⇒ `addTrait` 那条的 ×N 来自「同一条记录被合并 N 次」（= 本方法）。──
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var u = new UnitState(ctx.NewInstance(Unit("B94B2Merge", 1, 1, 3)), false);
            var r1 = u.ApplyTraitEffect(KeywordTable.Armour, 1);
            var r2 = u.ApplyTraitEffect(KeywordTable.Armour, 1);
            CheckTrue(ReferenceEquals(r1, r2),
                      "★★ ⑦ 同键（同 trait / 同 `markOfChaosType` / 同 `daemonHostType` / 同 `TimingBits` / 都非 extrinsic）"
                    + "**合并成同一条记录**（`CardScript__AddEffect.c:492-568` 的倒序匹配）"
                    + "｜🧨 每次都新建一条 ⇒ 实得 false ⇒ 红");
            Check(r1.TraitValue, 2,
                  "★★ ⑦ `add(10)` 档：同一条记录 `traitValue += 每次的值` ⇒ **2**（`.c:544`）"
                + "｜🧨 写成 `Max` 或覆盖 ⇒ 实得 1 ⇒ 红");
            Check(u.KwValue(KeywordTable.Armour), 2, "★★ ⑦ 关键词那本账跟着累加到 2");
            Check(u.Armor, 2, "⑦ 护甲字段同步（护甲只有关键词一个来源）");

            // extrinsic 的记录**不与任何记录合并**（原版那条「两边 `extrinsic == 0`」的闸）。
            var ctx2 = Battle(new CardDef[0], new CardDef[0]);
            var u2 = new UnitState(ctx2.NewInstance(Unit("B94B2MergeE", 1, 1, 3)), false);
            u2.ApplyTraitEffect(KeywordTable.Armour, 1);
            u2.ApplyTraitEffect(KeywordTable.Armour, 1, extrinsic: true);
            Check(u2.TraitEffects.Count, 2,
                  "★★ ⑦ `extrinsic` 那条**另起一条**（不合并不追加）⇒ 记录共 **2** 条"
                + "｜🧨 去掉 `if (!extrinsic)` 那层 ⇒ 实得 1 ⇒ 红");
        }

        // ── L707：`UnitState` 构造 = 原版 `CardSetup` 的**引擎半边**（攻/远攻/血/关键词/护甲/攻型快照）──
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            var u = new UnitState(ctx.NewInstance(Unit("B94B2Ctor", 2, 3, 4, KeywordTable.Armour)), false);
            Check(u.Attack, 3, "⑦ 构造时攻 = 卡面");
            Check(u.RangedAttack, 0, "⑦ 构造时远攻 = 卡面");
            Check(u.Health, 4, "⑦ 构造时血 = 卡面");
            Check(u.MaxHealth, 4, "⑦ 构造时血上限 = 卡面");
            Check(u.Armor, 1, "★★ ⑦ 护甲 = 卡面 `Armour` 关键词（`KwValue(armour)`；原版 `SetBaseCardTraits`）");
            CheckTrue(u.Has(KeywordTable.Armour), "⑦ 关键词表按卡面填好");
            CheckTrue(u.Exhausted,
                      "★★ ⑦ 无 fast/flank/ferocity ⇒ 部署显式态「不能动」"
                    + "（`Exhausted = !RuleCore.HasDeployExemption(this)`；原版 `CardSetup` 末尾 `ActivateMinion` 那一跳的引擎半边）");
        }

        // ── L1026：`CardDef.Ability` 是「卡自己的能力」**唯一**来源（原版第二来源 `+0x118` 池内恒 null）──
        {
            var noAbility = Unit("B94B2NoAb", 1, 1, 1);
            CheckTrue(noAbility.Ability == null,
                      "★★ ⑦ 没有 `Ability:` 的卡 ⇒ `CardDef.Ability = null`"
                    + "（原版 `GetActiveAbilityCard()` = `+0x118` 非空用它、否则回 `+0x28` 自己的 rawCard；"
                    + "而 `cards_truth.json` 1,351 张现扫 `buffType == 40`（replaceAbility）**0 条** "
                    + "⇒ `+0x118` 恒 null ⇒ 恒回自己 ⇒ 我们的静态读法**净等价**）"
                    + "｜🧨 给它编一个「默认能力」⇒ 红");
            var withAbility = Unit("B94B2WithAb", 2, 1, 4, "Ability: Damage 2 EnemyUnit");
            CheckTrue(withAbility.Ability != null,
                      "⑦ 带 `Ability:` 的卡 ⇒ `CardDef.Ability` 非空（= 原版 `+0x118` 为空时回落的 `+0x28` 那一份）");
        }
    }
}
