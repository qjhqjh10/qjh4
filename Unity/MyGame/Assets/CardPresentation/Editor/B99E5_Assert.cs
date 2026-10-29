// B99E5_Assert.cs — 执行代理 `B99_E5`（第九十九会话 · 波 1）的断言
//
// ============================ 它盯的是什么 ============================
//   账 `706②`：`CardPresentation/Battle/BattleDriver.cs` 的 `ShowSkillPanel` 里**技能面板「装填」两格**
//   （`requiresLoading:` / `abilityLoaded:`）原来**各自现写一份**：
//     · `requiresLoading = u.Card != null && u.Card.HasActiveAbility == 1 && !reloadRemoved
//                          && u.Card.ActiveAbilityNeedsLoading == 1;`   ← 读的是**卡面**（`CardDef`）
//     · `abilityLoaded: false,`                                      ← 写死常量（「我们没有装填机制」）
//   现在两格都走**唯一读点** `BattleDriver.SkillPanelLoadingFlags(u, out requiresLoading, out abilityLoaded)`，
//   它转发引擎侧的两个**实例视图**：`UnitState.RequiresAbilityLoading` / `UnitState.ActiveAbilityLoaded`。
//
// 判据（⛔ 别在这里抄第二份）→ `decomp_full/`：
//   · `CardScript__GetCurrentLocalizedAbilityDescription.c:43` —— **先** `DoesAbilityRequireLoading()`、
//     **再**按 `activeAbilityLoaded(+0x224)` 选 `-load` / `-effect` 两档描述（先后顺序 = 先 requires 后 loaded）。
//   · `CardScript__DoesAbilityRequireLoading.c:13-38` —— `+0x220 ≠ 0` ∧ 无 removed `reload`(0x1ea)
//     ∧ `ActiveAbility.needsLoading(+0x22)`；**三个输入全在 `CardScript`（实例）上**。
//   · `CardScript__SetBaseCardTraits.c:37-38` —— `+0x220 = rawCard.hasActiveAbility`、`+0x224 = 0`
//     （`CardSetup.c` 里那一跳就是它；`CardSetup.c` 本身**不碰** `+0x224`，现读 `grep -n 0x224` = 0 命中）。
//   · `CardScript__ResolveActiveAbilityPlayed.c:122/128` —— `+0x224` 的两次推进（我们 `StepActiveAbilityLoading`）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · C1：把 `SkillPanelLoadingFlags` 的 `u.RequiresAbilityLoading` 换回**卡级**
//        `u.Card.HasActiveAbility == 1 && ...` ⇒ C1 红（教程关闸位被忽略、`requiresLoading` 仍真）。
//   · B1：把 `requiresLoading` 改成从 `ActiveAbilityLoaded` 推（读点方向反）⇒ B1 红。
//   · A2/A3：把 `abilityLoaded = u.ActiveAbilityLoaded` 改回常量 `false` ⇒ A2 红（装填后仍报「没装」）。
//   · C3：把 `RequiresAbilityLoading` 里那条 `!HasRemovedTrait(ReloadTrait)` 删掉 ⇒ C3 红。
//
// ============================ 怎么用（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的 `RuleEngineTest.Run()` 里插一行：
//       Step(TestB99E5);
//   （本文件是 `public static partial class RuleEngineTest` 的**分册**，复用宿主的 `Step` / `Check` /
//    `CheckTrue` / `Section`；⛔ 另起一个类会把这套计数抄第二份。）
//   ⚠️ 本件只碰纯引擎类型（`UnitState` / `CardDef`）+ `BattleDriver.SkillPanelLoadingFlags`
//   （`static`，不建 GameObject、不要场景、不跑 `skillPanel`）。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static partial class RuleEngineTest
{
    /// <summary>B99_E5 的全部断言（宿主：`RuleEngineTest.Run` 里 `Step(TestB99E5);`）。</summary>
    public static void TestB99E5()
    {
        Section("B99_E5 —— 账 706②：技能面板「装填」两格改读实例视图（`BattleDriver.SkillPanelLoadingFlags`）");

        // ---- 夹具：把命名形参收在一处（`hasActiveAbility` / `activeNeedsLoading` 的缺省都是 `GateUnknown = -1`）----
        var loadCard = B99E5AbilityCard("B99E5Load", hasAbility: 1, needsLoading: 1);
        var noLoadCard = B99E5AbilityCard("B99E5NoLoad", hasAbility: 1, needsLoading: 0);
        var unknownCard = B99E5AbilityCard("B99E5Unknown", hasAbility: 1, needsLoading: null);

        // ============================================================ A 正向：不装填 → 装填 → 真发动
        var u1 = new UnitState(loadCard, false);
        Check(u1.Card.ActiveAbilityNeedsLoading, 1, "A0 （前提）`needsLoading` 这一列 = 1（三态里的「要装填」）");
        CheckTrue(u1.Card.HasActiveAbility == 1, "A0′ （前提）卡面 `hasActiveAbility` = 1 —— 否则 `RequiresAbilityLoading` 恒假、后面全是空转");
        CheckTrue(u1.RequiresAbilityLoading, "A0″ （前提）引擎侧实例视图 `RequiresAbilityLoading` 为真");

        bool r, l;
        BattleDriver.SkillPanelLoadingFlags(u1, out r, out l);
        CheckTrue(r, "★ A1 🔴 产品读点：需要装填 ⇒ `requiresLoading` 真｜🧨 读点退回卡级 / 删掉 needsLoading 那项 ⇒ 红");
        CheckTrue(!l, "★ A1′ `abilityLoaded` 初值假（原版 `SetBaseCardTraits.c:38` 写点 1 清零）｜🧨 改回常量 `false` 后 A2 会红");
        Check(r, u1.RequiresAbilityLoading,
              "A1″ 读点**转发**引擎视图（不是另写一份合取）—— `requiresLoading` == `UnitState.RequiresAbilityLoading`"
            + "｜🧨 在 `SkillPanelLoadingFlags` 里另写一份条件（漏掉实例位）⇒ 两者分叉 ⇒ 红");
        Check(l, u1.ActiveAbilityLoaded,
              "A1‴ 读点转发 `UnitState.ActiveAbilityLoaded`（= 原版 `+0x224`）");

        CheckTrue(u1.StepActiveAbilityLoading(), "A2 （前提）第一次打出 ⇒ 只装填（`ResolveActiveAbilityPlayed.c:128`）");
        BattleDriver.SkillPanelLoadingFlags(u1, out r, out l);
        CheckTrue(l, "★ A2 🔴 装填之后 `abilityLoaded` 真（面板这时必须切到 `-effect` 那一档描述）｜🧨 写死 `false` ⇒ 红");
        CheckTrue(r, "★ A2′ 两格**互相独立**：`loaded` 变真**不改变** `requiresLoading`（原版 `:43` 先问 requires、再读 loaded）"
                    + "｜🧨 把 requires 写成从 loaded 反推 ⇒ 红");

        u1.StepActiveAbilityLoading();                 // 第二次 ⇒ 真发动、发动后清 0（`.c:122`）
        BattleDriver.SkillPanelLoadingFlags(u1, out r, out l);
        CheckTrue(!l, "★ A3 🔴 真发动之后又清 0 ⇒ `abilityLoaded` 假（原版 `:122`）｜🧨 不转发字段 ⇒ 红");
        CheckTrue(r, "A3′ 清 0 之后仍然「需要装填」（下一次又要先装一次）");

        // ============================================================ B 方向没反 / 三态
        var u2 = new UnitState(noLoadCard, false);
        u2.ActiveAbilityLoaded = true;                 // 人为造出「装了但其实不用装」这种**不可能由引擎产生**的组合
        BattleDriver.SkillPanelLoadingFlags(u2, out r, out l);
        CheckTrue(!r, "★ B1 🔴 读点方向没反：`needsLoading == 0` ⇒ `requiresLoading` **假**，与 `loaded` 无关"
                    + "｜🧨 把 requires 写成从 loaded 推（方向反）⇒ 这里变真 ⇒ 红");
        CheckTrue(l, "B1′ 同一条里 `abilityLoaded` 照实转发（不被 requires 顺带压成假）｜🧨 写成 `requiresLoading && u.ActiveAbilityLoaded` ⇒ 红");

        var u3 = new UnitState(unknownCard, false);
        Check(u3.Card.ActiveAbilityNeedsLoading, -1, "B2 （前提）`needsLoading` 缺省 = **-1（未知）**");
        BattleDriver.SkillPanelLoadingFlags(u3, out r, out l);
        CheckTrue(!r, "★ B2 🔴 未知**不**当成「要装填」（三态口径，与改前的 `== 1` 一致）｜🧨 写成 `!= 0` ⇒ 红");

        // ============================================================ C 灭自证：实例位（这一笔真正修掉的东西）
        var u4 = new UnitState(B99E5AbilityCard("B99E5Hidden", hasAbility: 1, needsLoading: 1), false);
        u4.ActiveAbilityHidden = true;                 // 教程关闸（原版 `HideWarlordActiveAbilityForTutorial.c` 把 `+0x220` 写 0）
        BattleDriver.SkillPanelLoadingFlags(u4, out r, out l);
        CheckTrue(!r, "★ C1 🔴 **教程关闸**（实例位 `ActiveAbilityHidden`）⇒ 不需要装填"
                    + "｜🧨 读点退回**卡级** `Card.HasActiveAbility == 1`（卡面永远是 1）⇒ 这里变真 ⇒ 红（这正是改前那一份）");

        var u5 = new UnitState(B99E5AbilityCard("B99E5Jammed", hasAbility: 1, needsLoading: 1), false);
        u5.ActiveAbilitySuppressed = true;             // jam 压制（原版 `ReactToUnitJammed.c:153` 同样写 `+0x220 = 0`）
        BattleDriver.SkillPanelLoadingFlags(u5, out r, out l);
        CheckTrue(!r, "★ C2 🔴 **jam 压制**（实例位 `ActiveAbilitySuppressed`）⇒ 不需要装填"
                    + "｜🧨 同 C1：卡级读点看不见这个位 ⇒ 红");

        var u6 = new UnitState(B99E5AbilityCard("B99E5Reload", hasAbility: 1, needsLoading: 1), false);
        CheckTrue(!u6.HasRemovedTrait(ConditionEval.ReloadTrait), "C3 （前提）还没记「reload 被移除」");
        u6.MarkTraitRemoved(ConditionEval.ReloadTrait); // `DoesAbilityRequireLoading.c:35` 的 `HasRemovedTrait(0x1ea)`
        CheckTrue(u6.HasRemovedTrait(ConditionEval.ReloadTrait), "C3′ （前提）账已记上");
        BattleDriver.SkillPanelLoadingFlags(u6, out r, out l);
        CheckTrue(!r, "★ C3 🔴 摘过 `reload` ⇒ 不需要装填（原版 `.c:35` 那一跳）｜🧨 删掉 `!HasRemovedTrait(ReloadTrait)` ⇒ 红");

        // ============================================================ D 边界与复位
        BattleDriver.SkillPanelLoadingFlags(null, out r, out l);
        CheckTrue(!r && !l, "★ D1 `null` ⇒ 两格都假（⛔ 不抛异常 —— `ShowSkillPanel` 入口只判了 `u == null` 早退，这里再兜一层）");

        var u7 = new UnitState(B99E5AbilityCard("B99E5Reset", hasAbility: 1, needsLoading: 1), false);
        u7.StepActiveAbilityLoading();
        CheckTrue(u7.ActiveAbilityLoaded, "D2 （前提）已装填");
        u7.ResetToValuesInHand(clearBacklash: true);   // `SetBaseCardTraits` 那一跳
        BattleDriver.SkillPanelLoadingFlags(u7, out r, out l);
        CheckTrue(!l, "★ D2 🔴 回手复位（`SetBaseCardTraits.c:38`）⇒ `abilityLoaded` 清 0，面板读点立刻跟上｜🧨 读死值 ⇒ 红");

        var ua = new UnitState(B99E5AbilityCard("B99E5A", hasAbility: 1, needsLoading: 1), false);
        var ub = new UnitState(B99E5AbilityCard("B99E5B", hasAbility: 1, needsLoading: 1), false);
        ua.StepActiveAbilityLoading();
        BattleDriver.SkillPanelLoadingFlags(ub, out r, out l);
        CheckTrue(!l, "★ D3 装填位**每实例一份**（原版 `CardScript` 实例字段）：另一张卡不受影响｜🧨 做成静态位 ⇒ 红");
    }

    /// <summary>夹具卡：`hasActiveAbility` / `activeNeedsLoading` 都由调用方给（`null` = 不传，走缺省 `-1` 未知）。
    /// <para>⚠️ `needsLoading` 是**三态**（`-1` 未知 / `1` 要装填 / `0` 不要）—— ⛔ 别把 `null` 当 0。</para></summary>
    static CardDef B99E5AbilityCard(string id, int hasAbility, int? needsLoading)
    {
        return needsLoading.HasValue
            ? new CardDef(id, id, "troop", "", null, "SpaceWolves", 1, 1, 3, 0, null,
                          subtype: "troop", hasActiveAbility: hasAbility, activeNeedsLoading: needsLoading.Value)
            : new CardDef(id, id, "troop", "", null, "SpaceWolves", 1, 1, 3, 0, null,
                          subtype: "troop", hasActiveAbility: hasAbility);
    }
}
