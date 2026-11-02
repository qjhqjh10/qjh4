// RuleCorePartial_B120W7.cs —— 第一百二十会话 · 写手 `B120-W7`（引擎数值 / trait 记账）
//
// 为什么单开一个文件：`Core/EffectResolver.cs`（19,5xx 行）与 `Core/RuleCore.cs`（2 万行级）都被
//   `账/*.tsv` 按 `文件:行号` 大量落点引用 ⇒ **往既有段落里插成员会让下游行号整体漂移**
//   （铁律 18·b / N9 先例）。本批**新成员一律写在这里**（编译期它与那两个文件是**同一个**
//   `RuleCore` —— `EffectResolver.cs` 的类声明是 `public static partial class RuleCore`）。
//
// 本件的落点（活状态表 `D118-77`）：
//   · `ApplyAddTraitRecordRow` —— 结构化 `abilityBuffs` 路（`AddEffect` 的 `case 3 addTrait` /
//     `case 90 addTraitRemnant`）**补建 `TraitEffectRecord`**。改前那条路只调 `UnitState.AddKeyword`
//     ⇒ 记录账 / `untilNextAttack` 到期 / 按来源撤 / `AnyEffectGrantsTrait` 第二道闸对它**全失效**
//     （只有文本 op 路 `EffectResolver.ApplyOneGain` 建记录）。生产调用点 = `ApplyAbilityBuff` 的
//     `case 3: case 90:`（`EffectResolverPartial_B97W2.cs`，**同一物理行**换成一个函数调用 ⇒ 该文件零行数变化）。

namespace RuleEngine
{
    public static partial class RuleCore
    {
        /// <summary>
        /// **`CardScript.AddEffect` 的 `case 3 addTrait` / `case 90 addTraitRemnant` 那一行的记录落点**
        /// （原版每条 `addTrait` 都在 `activeEffects(+0x108)` 里留**一条 `CardEffect` 记录**）。
        ///
        /// <para>**判据（第一权威 · 逐句）** = `decomp_full/CardScript__AddEffect.c:460-679`：
        /// ① `Log("Add Trait")` + `CardTraitCollection.GetTrait(trait)`；
        /// ② 毒 × 抗性 / survivor 零血两道闸；
        /// ③ `RemoveActiveEffects(4, trait)`（带 trait 过滤 —— 实参个数以两份 IL 为准，见
        ///    `判据/战斗逻辑驱动_Stage2前置_第2轮.md` §三·1）；
        /// ④ 倒序扫 `activeEffects` 找「同 `trait(+0x3c)` ∧ 同 `markOfChaosType(+0x78)` ∧ 同
        ///    `daemonHostType(+0x7c)` ∧ `SameTimingAsEffect` ∧ **两侧 `extrinsic(+0x30)` 都为 0**」那一条，
        ///    命中 ⇒ 按 `CardTrait._stackable` 三档合并 `traitValue(+0x40)`（`replace` 取 Max / `add` 相加 /
        ///    `count` 每次 +1，`.c:529/541/549`）；**未命中** ⇒ `List.Add(newEffect)` 后
        ///    再 `AddTraitSilently(trait, …)`（`.c:561-580`）。
        /// ⑤ 收尾的逐 trait 特例（fast/flank 重算、poisoned×resistant 互摘…）走两边共用的那段 —— **本方法不管**。</para>
        ///
        /// <para>**我们的等价位** = `UnitState.ApplyTraitEffect`（`UnitState.cs:2943`）：
        /// 它内部就是「按 `TraitStackTypeOf` 合并进同一条记录（合并不上就新建）+ 把**增量**落到 `_keywords`
        /// （走 `AddKeyword`，毒 × 抗性那道拒收一并带上）」⇒ **净关键词增量与改前逐字相同**，
        /// 多出来的只有**记录账**。文本 op 路（`EffectResolver.ApplyOneGain`）自 2026-10-27 起就用它，
        /// 只有这条结构化 buff 路一直只调 `AddKeyword` ⇒ 同一条 trait 在两条路上的可撤性不一致。</para>
        ///
        /// <para>⚠️ **`traitValue <= 0`（空给）保持改前的 `AddKeyword`**：`ApplyTraitEffect` 对
        /// `value <= 0` 直接返回 `null`（那是 `removeTrait(4)` 那一族，⛔ 别拿它做「减」）。
        /// 文本 op 路同一口径（`EffectResolver.cs:12304` 的 `else added = u.AddKeyword(...)`，理由写在同一行的上一段注释）。
        /// ⇒ 本仓 370 条 `addTrait` 行里 **234 条 `traitValue` 缺失（= 0）**（`cards_engine.json` 现扫），
        /// 这些行**不建记录**、行为与改前逐字节相同；**87 条 > 0 的行**才建记录。</para>
        ///
        /// <para>🔴 **`timingBits` 必须一起传**：它是原版合并键里那五个 `until*` 位
        /// （位序 → `UnitState.TraitEffectRecord.TimingBits` 的 doc）⇒ 不传的话「本回合 +1 Flank」会与
        /// 「永久 +1 Flank」**并成一条**，于是限时那条既撤不掉、又永远显示成永久。
        /// 数据面直接有这五列（`CardDatabase.AbilityBuffSpec.UntilEndOfTurn/…/UntilNextAttack`）⇒ 逐位照抄，
        /// ⛔ 别绕 `TimingBitsOfDuration`（那是**解析层**的字符串档 → 位掩码，只有 `""`/`turn`/`nextturn` 三档）。</para>
        ///
        /// <para>⚠️ **归属四元组（`origin`）这一格留 `default`**：`ApplyAbilityBuff` 的形参里没有 `CardDef`
        /// （只有 `AbilityBuffSpec` 的 `Ab/Lg`，没有卡名）⇒ `EffectOwner = null` ⇒ `EffectAb/EffectLg` 落 `-1`
        /// = 「判不到」。⛔ 不猜卡名（`UnitState.DeriveEffectOrigin` 是给「正文 op 路」反解用的，
        /// 在这里再反解一次会把已知的 `b.Ab/b.Lg` 换成它自己搜出来的另一条 —— 宁可不填）。</para>
        ///
        /// <para>📊 **今天的影响面（现扫 `cards_engine.json`）**：`abilityBuffs` 里 `buffType ∈ {3,90}`
        /// 的 321 行、其中 `traitValue > 0` 的 **87 行**。该表的两个**生产**调用点
        /// （`EnchantmentExecutor_B115.cs:312` 附魔模板路 · `ChoiceOptionCards_B116W32.cs:155` 选项卡路）
        /// 才会走到这里；卡表主路仍以正文 op 为准（那边的口径见 `ApplyAbilityBuff` 的方法头，⛔ 别混）。</para>
        /// </summary>
        /// <param name="target">被加 trait 的单位（原版 `this`）。</param>
        /// <param name="b">那一条 `CardEffect` 行（`abilityBuffs` 映射后的形状）。</param>
        /// <param name="kw">已经反解好的关键词名（调用方用 `CardDef.DefinedTraitName(b.Trait)` 得到，⛔ 别在这里再查一次）。</param>
        /// <param name="who">日志标签（同 `ApplyAbilityBuff` 的 `who`）—— 记录里的 `Source` 记它。</param>
        /// <returns>真的挂上了返回 `true`（与改前 `AddKeyword` 的返回值同义：`false` = 毒 × 抗性拒收，或 `trait` 认不出）。</returns>
        static bool ApplyAddTraitRecordRow(UnitState target, CardDatabase.AbilityBuffSpec b, string kw, string who)
        {
            if (target == null || b == null || string.IsNullOrEmpty(kw)) return false;
            // 空给（`traitValue <= 0`）⇒ 保持改前的 `AddKeyword`：`ApplyTraitEffect` 对 `value <= 0`
            //   直接返回 null（见方法头那一段），而改前这条路的 `AddKeyword(kw, 0)` 会**照挂词条**。
            if (b.TraitValue <= 0) return target.AddKeyword(kw, b.TraitValue);

            // 五个 `until*` 位（原版 `CardEffect +0x32..+0x36`；位序 = `TraitEffectRecord.TimingBits` 的 doc）
            int timingBits = (b.UntilEndOfTurn != 0 ? 1 : 0)
                           | (b.UntilPlayerTurnStart != 0 ? 2 : 0)
                           | (b.UntilEnemyTurnStart != 0 ? 4 : 0)
                           | (b.UntilFollowingPlayerTurnStart != 0 ? 8 : 0)
                           | (b.UntilNextAttack != 0 ? 16 : 0);

            // `buffType` 原样传（3 = addTrait / 90 = addTraitRemnant —— 原版两条走**同一整块**，`.c:219`）。
            // `stackType: -1` ⇒ 按 `TraitStackTypeOf` 现查（本 build 只有 `markOfChaos(330)` 是 `count`）。
            // `extrinsic` / `markOfChaosType` / `daemonHostType` / `permanentBuff` 都是**原版合并键**里的列 ⇒ 逐格照抄。
            var rec = target.ApplyTraitEffect(kw, b.TraitValue,
                                             buffType: b.BuffType, stackType: -1,
                                             source: who, extrinsic: b.Extrinsic != 0,
                                             markOfChaosType: b.MarkOfChaosType,
                                             daemonHostType: b.DaemonHostType,
                                             timingBits: timingBits,
                                             permanent: b.PermanentBuff != 0);
            return rec != null;
        }
    }
}
