// RuleEngineTest_EngineB95WRS.cs — 第九十五会话 · 执行代理 `B95_WRS`（`UnitState.cs` / `PlayerState.cs` 的写手）
//
// 为什么另开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆），
// 同一时刻只有调度台能改 `RuleEngineTest.cs` ⇒ 本批断言全写在这里、**只交一个入口方法名**
// `CheckB95WRSGaps`，由调度台在 `Run()` 里加一行（⛔ 本代理不改调度台的文件）。
//
// 本件只钉 2026-10-29 本批在 `UnitState.cs` 上的**两处代码改动**（其余 13 条是「核完仍 B」，
// 它们的断言已由 `RuleEngineTest_EngineB94B2` 覆盖，本件不重复）：
//   ① 账 866 `CardScript.ResetToValuesInHand` —— `.c:154` 调 `SetBaseCardTraits`，
//      而 `CardScript__SetBaseCardTraits.c` 首句是 `+0x220 hasActiveAbility = rawCard + 0x2a9`
//      （字段名判据 `dump.cs:35444`）⇒ 重置时**两个实例闸要一起清**，派生视图回到卡面值。
//   ② 账 636 `CardScript.RemoveExtrinsicEffectsFrom` —— 原版摘的是 `activeEffects(+0x108)` 里
//      **整条记录**；我们那几本账里 **`_grants` 是护甲唯一的账**（`ApplyGrant` 的 `armour` 支
//      走 `AddKeyword/RemoveKeyword`、⛔ 不进 `_statMods`）⇒ 按来源撤时必须一起过。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）；期望值写死字面量，⛔ 不从被测实现反算。
using System;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（调度台在 `Run()` 里加一行）。</summary>
    public static void CheckB95WRSGaps()
    {
        Section("🆕 B95_WRS：重置时恢复 `+0x220`（两个实例闸一起清）· 按来源撤连护甲那一本账");
        B95WRST1ResetRestoresHasActiveAbility();
        B95WRST2RemoveExtrinsicCoversArmourGrants();
    }

    // ==============================================================================================
    //  ① 账 866：`ResetToValuesInHand` ⇒ `SetBaseCardTraits` ⇒ `+0x220 = rawCard+0x2a9`
    //     判据链（2026-10-29 现读）：
    //       `decomp_full/CardScript__ResetToValuesInHand.c:154`  `CardScript__SetBaseCardTraits(param_1,0);`
    //       `decomp_full/CardScript__SetBaseCardTraits.c:37`      `*(undefined1 *)(param_1 + 0x220) = *(undefined1 *)(*(…) + 0x2a9);`
    //       `d:/2/tools/il2cpp_out/dump.cs:35444`                `private bool <hasActiveAbility>k__BackingField; // 0x220`
    //     ⚠️ 原版**只有这一个字段**，jam（`ReactToUnitJammed.c:153`）与教程（`Hide…ForTutorial`）**都写它**；
    //        我们拆成两个实例闸（`ActiveAbilitySuppressed` / `ActiveAbilityHidden`）⇒ 重置时两个都要清。
    // ==============================================================================================

    static void B95WRST1ResetRestoresHasActiveAbility()
    {
        var cardOn = new CardDef("B95WRSAA1", "B95WRSAA1", "unit", "", null, "Test", 1, 1, 3, 0,
                                 hasActiveAbility: 1);
        var cardOff = new CardDef("B95WRSAA0", "B95WRSAA0", "unit", "", null, "Test", 1, 1, 3, 0,
                                  hasActiveAbility: 0);

        // ── jam 那一支：`ApplyJam` 把 `+0x220` 写 0（非督军）──
        var uJam = new UnitState(CardInstance.Detached(cardOn), false);
        uJam.ApplyJam();
        // 前提（硬检查）：那两位**现在真的是关的**，否则下面「重置后变开」是空转。
        CheckTrue(uJam.ActiveAbilitySuppressed, "① （前提）`ApplyJam` 对非督军置压制位（= `+0x220 = 0`）");
        CheckTrue(!uJam.ActiveAbilityAvailable, "① （前提）被 jam ⇒ 主动能力不可用");

        uJam.ResetToValuesInHand(clearBacklash: true);

        CheckTrue(!uJam.ActiveAbilitySuppressed,
                  "★★ ① 重置后压制位清 0 —— 原版 `SetBaseCardTraits` 把 `+0x220` **从卡面 `+0x2a9` 重灌**"
                + "（`CardScript__ResetToValuesInHand.c:154` → `CardScript__SetBaseCardTraits.c:37`；"
                + "字段名 `dump.cs:35444`）⇒ 不是「jam 的痕迹永远留着」"
                + "｜🧨 删掉 `ResetToValuesInHand` 里那句 `ActiveAbilitySuppressed = false` ⇒ 实得 true ⇒ 红");
        CheckTrue(uJam.ActiveAbilityAvailable,
                  "★★ ① 卡面 `hasActiveAbility = 1` ⇒ 重置后**可用**（= 原版那句「从卡面恢复」）"
                + "｜🧨 同上 ⇒ 红");

        // ── 教程那一支：`Hide…ForTutorial` 也写同一个 `+0x220` ──
        var uHid = new UnitState(CardInstance.Detached(cardOn), false);
        uHid.HideWarlordActiveAbilityForTutorial();
        CheckTrue(!uHid.ActiveAbilityAvailable, "① （前提）教程关闸生效 ⇒ 不可用");
        uHid.ResetToValuesInHand(clearBacklash: true);
        CheckTrue(uHid.ActiveAbilityAvailable,
                  "★★ ① 重置同样清教程关闸（原版两个来源写的是**同一个** `+0x220`）"
                + "｜🧨 只清 `ActiveAbilitySuppressed`、漏 `ActiveAbilityHidden` ⇒ 实得 false ⇒ 红");

        // ── 灭自证：卡面 `hasActiveAbility = 0` 的卡，重置后**必须仍不可用** ──
        //    否则「重置」被写成「无条件点亮」，上面两条也照样绿。
        var uOff = new UnitState(CardInstance.Detached(cardOff), false);
        uOff.HideWarlordActiveAbilityForTutorial();
        uOff.ResetToValuesInHand(clearBacklash: true);
        CheckTrue(!uOff.ActiveAbilityAvailable,
                  "★★ ① 卡面 **0** 的卡重置后**仍然不可用** —— 这是「从卡面恢复」与「无条件置 1」的分水岭"
                + "（原版 `+0x220 = rawCardData + 0x2a9`；⛔ 若写成无条件可用，那 45 张有 `activeAbilityLogic` "
                + "却 `hasActiveAbility = 0` 的卡会被好心点亮）"
                + "｜🧨 把重置写成「一律 `ActiveAbilityAvailable = true`」⇒ 红");
    }

    // ==============================================================================================
    //  ② 账 636：`UnitState.RemoveExtrinsicEffectsFrom(卡名)` 要覆盖 `_grants`（护甲那一本账）
    //     判据：`decomp_full/CardScript__RemoveExtrinsicEffectsFrom.c` 摘的是 `activeEffects(+0x108)` 里
    //           `extrinsic(+0x30) != 0 ∧ enchantingCard(+0x18) == 参` 的**整条记录** ——
    //           原版一条记录同时带「近战/远程/血/甲/费」五对字段 ⇒ 不可能出现「数值撤了、护甲留着」。
    //     我们：`attack/ranged/health` 落在 `_statMods`（有 `Source`），而 **`armour` 只落在 `_grants`**
    //           （`ApplyGrant` 的 `armour` 支走 `AddKeyword/RemoveKeyword`）⇒ 漏了 `RevertGrantsFrom` 就拆成两种结果。
    // ==============================================================================================

    static void B95WRST2RemoveExtrinsicCoversArmourGrants()
    {
        var u = new UnitState(CardInstance.Detached(Unit("B95WRSArm", 1, 1, 3)), false);
        Check(u.Armor, 0, "② （前提）这张卡自己没有护甲");

        u.RecordGrant("armour", 2, "B95SrcA");
        u.RecordGrant("armour", 1, "B95SrcB");
        Check(u.Armor, 3, "② （前提）两个来源的护甲都在：2 + 1 = 3");
        Check(u.PendingGrants.Count, 2, "② （前提）`_grants` 里两条");

        u.RemoveExtrinsicEffectsFrom("B95SrcA");
        Check(u.Armor, 1,
              "★★ ② 按来源撤 ⇒ **护甲也要撤**（原版摘的是整条 `activeEffects` 记录，五个数值字段一起走）"
            + "｜🧨 不调 `RevertGrantsFrom`（= 2026-10-29 改前那版）⇒ 实得 3 ⇒ 红");
        Check(u.PendingGrants.Count, 1,
              "★★ ② 账里只剩 B 那一条｜🧨 同上 ⇒ 实得 2 ⇒ 红");

        // 灭自证：撤**别的**来源不能把 B 的也带走。
        u.RemoveExtrinsicEffectsFrom("B95SrcZzz");
        Check(u.Armor, 1, "★★ ② 撤一个不存在的来源 ⇒ 一点不动（⛔ 不是「清全部」）"
                        + "｜🧨 写成「清掉所有 `_grants`」⇒ 实得 0 ⇒ 红");

        u.RemoveExtrinsicEffectsFrom("B95SrcB");
        Check(u.Armor, 0, "★★ ② 再撤 B ⇒ 回 0");
        Check(u.PendingGrants.Count, 0, "② 两批账都清干净");

        // 回归护栏：`_statMods` 那条老路（不走 `RecordGrant`）**行为不变**。
        var v = new UnitState(CardInstance.Detached(Unit("B95WRSMod", 1, 2, 3)), false);
        v.AddStatMod("attack", 3, "B95CardA", extrinsic: true);
        Check(v.Attack, 5, "② （前提）2 + 3 = 5");
        int n = v.RemoveExtrinsicEffectsFrom("B95CardA");
        Check(n, 1, "② 摘掉 1 条（`_buffs` / `_statMods` 那条老路的读数不变）");
        Check(v.Attack, 2, "★★ ② 近战回卡面 2｜🧨 把 `_statMods` 那半也改成走 `_grants` ⇒ 实得 5 ⇒ 红");
    }
}
