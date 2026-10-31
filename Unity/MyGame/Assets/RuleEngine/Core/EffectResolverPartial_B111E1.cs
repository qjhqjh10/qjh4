// EffectResolverPartial_B111E1.cs —— 第一百一十一会话 · 波 1 · 执行代理 `B111_E1`
//
// 为什么单独一个文件：`Core/EffectResolver.cs` 有 20,000+ 行、且被 `账/*.tsv` 按 `文件:行号` 大量落点引用
//   ⇒ **往里插成员会让下游行号整体漂移**（`check_ledger_points` 立刻报，铁律 18·b / N9 先例）。
//   本批**所有新成员**都追加在这里 —— 编译期它与 `EffectResolver.cs` 是**同一个** `partial class RuleCore`
//   （全仓**没有**叫 `EffectResolver` 的类，照文件名写 `EffectResolver.` 编译不过）。
//
// 本文件落的账（活状态表 `B-06d` / 原版 `N29` 的后两段）：
//   · `CardScript.AddEffect.c:91-150` —— **extrinsic 效果的「去重 / 替换」闸**
//     （Ghidra `decomp_full/CardScript__AddEffect.c:91-150`；ILSpy `cpp2il/out_cpp2il_dev_ilspy/CardScript.cs:17465-17565`）
//   · 与它配套的**施放者取数口** `ExtrinsicCasterName`（写/读同一把尺子）
//
// 🔴 **第一权威三源交叉（2026-11-01 现读，三份都打开看过）**：
//   · `decomp_full/CardScript__AddEffect.c:91-150`（Ghidra）：`effect.+0x30(extrinsic)` ⇒
//     `ContainsExtrinsicEffectFrom(this, sourceCard, buffType, baseEnchantmentCard)`；
//     命中且 `buffType != 0xb(11 removeExtrinsic)` 时：`+0x31 == 0` ⇒ `return 0`；
//     否则扫 `activeEffects(+0x108)` 找「`extrinsic` ∧ `enchantingCard == 本条施放者`」：
//     找到 ⇒ `IsSameEffect(那条, 本条)` 为真就 `return 0`，为假就 `RemoveExtrinsicEffectsFrom(施放者)`；
//     一条都没找到 ⇒ 照常挂。
//   · ILSpy `CardScript.cs:17465-17565`：把 `+0x31` 明确成 **`CardEffect.extrinsicVariable`**
//     （不是「stackable」）、把那三段拍平成 `IL_059a: return false;` / `IL_0368` / `IL_03db: RemoveExtrinsicEffectsFrom(...)`。
//     🔴 Ghidra 那份**没有**字段名，只写 `+0x31`；只看它会把语义猜成「可叠加位」（正好反了）。
//   · `CardEffect__IsSameEffect.c`：比的是一整排字段（`buffType` / `enchantingCard` /
//     `extrinsic` / `extrinsicVariable` / 五个 `until*` 位 / `trait` / `traitValue` /
//     `healthChange` / `meleeAttackChange` / `rangeAttackChange` / `costChange` / `optionFlags` …）。
//   · `extrinsicVariable` 的**唯一生产点** = `CardScript.cs:16064` / `:16106`（`pack` 关键词的
//     「按场上部队数重算 `+N/+N`」那条 `CheckAndApplyCurrentPack` / `UpdateWhileInPlay` 支）。
//
// ⚠️ **如实标的残余（⛔ 别当成「已完全对齐」）**：
//   ① 我们的效果记录**不是**原版那样「一条 `CardEffect` 带齐十几格」—— 属性在 `StatMod`、关键词在
//      `TraitEffectRecord`，一条卡面子句会被拆成 N 条。⇒ 本闸的「同一效果」判据**收敛到**
//      `(buffType, 属性名/关键词名, 值, 是否覆盖语义)` 四格（见 `ExtrinsicSameEffect`）。
//      原版那 12 格里的 `until*` 五个位我们压成 `TimingBits`（`TraitEffectRecord` 那一格）；
//      属性那条没有 `TimingBits` ⇒ 限时与永久在**同一个 attr 上**会被当成「同一效果」。
//   ② `baseEnchantmentCard`（原版 `ContainsExtrinsicEffectFrom` 的第 3 个筛）我们**没有这一列** ⇒
//      不参与比较（原版绝大多数调用点传 `0`，那一位本来就不启用）。
//   ③ **`extrinsicVariable` 今天恒 `false`**：唯一生产点是 `pack` 关键词那条重算路，我们**没建模**
//      `pack`（`lastPackTroops` / `RemovePackBuffs` / `CheckAndApplyCurrentPack` 全仓 0 命中）
//      ⇒ 「替换」那一半今天**不可达**（`SAU27 Scarab Swarm` 那条 `trigger 240 + buffType 70` 走的是
//      `Auras.RefreshMeleeEqualsHealth`，本来就不是这条闸）。照铁律 11 把机制建全，断言直调验它。
//   ④ 原版比的是 `enchantingCard` **对象引用**；我们按**卡名**（与 `ContainsExtrinsicEffectsFrom` /
//      `RemoveExtrinsicEffectsFrom` 既有口径一致，同名两张会多撤 —— 那条已记在账 547 ③）。
//   ⑤ 「这份记录是谁给的」我们原来**没存**（`DoGive` 走 `AddStatMod(...)` 时 `source` 是 `null`、
//      `ApplyTraitEffect(...)` 里存的是日志标签 `by`）⇒ 本批把**施放者卡名**补进 `source`
//      （`ExtrinsicCasterName(ctx)`，**只在 extrinsic 时为非 `null`** ⇒ 今天逐字节不变）。
using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==============================================================================================
        //  ① 施放者取数口（写/读同一把尺子）
        // ==============================================================================================
        /// <summary>
        /// **这条 extrinsic 效果的「施放者」卡名** —— 原版 `CardEffect.enchantingCard(+0x18)`
        /// （`dump.cs` Tooltip = `Ignore when creating card`）在我们这侧的等价格。
        ///
        /// <para>**取谁**：`ctx.ActingUnit` —— 与 `AddEffectCasterNotJammed`（原版 `:85-86` 那条 jam 闸）
        /// **同一个口**。⛔ 别另造第二个口：闸② 与 `:91-130` 的去重键**必须**是同一张卡，
        /// 否则「jam 闸认的那个施放者」和「去重认的那个施放者」会打架。</para>
        ///
        /// <para>⚠️ 只在 <see cref="BattleContext.ResolvingExtrinsic"/> 为真时返回非 `null`
        /// ⇒ 灌进 `StatMod.Source` / `TraitEffectRecord.Source` 时**非 extrinsic 的记录一个字都不变**
        /// （今天 `ResolvingExtrinsic` 恒 `false` ⇒ 本方法恒回 `null` ⇒ **零行为变化**）。</para>
        /// </summary>
        public static string ExtrinsicCasterName(BattleContext ctx)
        {
            if (ctx == null || !ctx.ResolvingExtrinsic) return null;
            return ctx.ActingUnit != null ? ctx.ActingUnit.Name : null;
        }

        // ==============================================================================================
        //  ② 原版 `CardScript.AddEffect.c:91-150` —— extrinsic 的「去重 / 替换」闸
        // ==============================================================================================
        /// <summary>
        /// **`AddEffect` 的第 3 道前置闸：extrinsic 效果的去重 / 替换**
        /// （`decomp_full/CardScript__AddEffect.c:91-150` ＝ ILSpy `CardScript.cs:17465-17565`）。
        ///
        /// <para>**原版判定树（逐句）**：
        /// <code>
        /// if (effect.extrinsic) {
        ///     if (ContainsExtrinsicEffectFrom(this, effect.enchantingCard, effect.buffType, effect.baseEnchantmentCard)
        ///         &amp;&amp; effect.buffType != removeExtrinsic(11)) {
        ///         if (!effect.extrinsicVariable) return false;              // 固定型：同源同档已有 ⇒ 拒
        ///         foreach (ae in activeEffects) if (ae IsSameEffect(effect)) return false;   // 完全一样 ⇒ 拒
        ///         RemoveExtrinsicEffectsFrom(this, effect.enchantingCard);   // 值变了 ⇒ 整份摘掉旧的
        ///     }
        /// }
        /// </code></para>
        ///
        /// <para><b>我们的映射</b>：`ContainsExtrinsicEffectFrom` ⇒ <see cref="FindExtrinsicFrom"/>
        /// （扫 `StatMod` / `TraitEffectRecord` 两本账，键 = 施放者 + `buffType` + 属性名/关键词名）；
        /// `RemoveExtrinsicEffectsFrom` ⇒ `UnitState.RemoveExtrinsicEffectsFrom(施放者)`（既有口，
        /// 四本账都过 `Extrinsic` 合取）；`IsSameEffect` ⇒ <see cref="ExtrinsicSameEffect"/>。
        /// 残余（① `baseEnchantmentCard` 无此列 ② `extrinsicVariable` 今天无生产者）见文件头。</para>
        /// </summary>
        /// <param name="buffType">原版 `CardEffect.buffType(+0x38)`：`1 addStats` / `3 addTrait` / `90 addTraitRemnant` …</param>
        /// <param name="key">属性名（已过 <see cref="NormalizeAttr"/>）或关键词名 —— 原版没有这一格，
        ///   它是我们对「一条 `CardEffect` 带齐十几格」那个形状的**收敛键**（见文件头 ①）。</param>
        /// <param name="value">本条要写进去的值（`IsSameEffect` 那一格）。</param>
        /// <param name="isSet">`setMeleeAttack` / `setRangeAttack` / `setHealth` 那三档的「覆盖」语义。</param>
        /// <param name="extrinsicVariable">原版 `CardEffect.extrinsicVariable(+0x31)` —— 真 = **变量型**
        ///   （`pack` 那条每动一次就重算的 `+N`）⇒ 值变了要**整份替换**；假 = 固定型 ⇒ 同源同档已有就**拒**。</param>
        /// <returns>`true` = 允许继续挂（可能刚摘掉了旧的）；`false` = **拒**（这条不该生效）。</returns>
        public static bool ExtrinsicAddEffectGate(BattleContext ctx, UnitState u, int buffType, string key,
                                                  int value, bool isSet, bool extrinsicVariable, string by)
        {
            if (ctx == null || u == null) return true;
            if (!ctx.ResolvingExtrinsic) return true;                 // 原版整段包在 `if (effect.extrinsic)`
            if (buffType == 11) return true;                          // 原版 `&& buffType != removeExtrinsic(0xb)`
            string caster = ExtrinsicCasterName(ctx);
            if (string.IsNullOrEmpty(caster)) return true;            // 原版 `ContainsExtrinsicEffectFrom(null,…)` 恒假 ⇒ 照挂

            int oldValue; bool oldIsSet;
            if (!FindExtrinsicFrom(u, caster, buffType, key, out oldValue, out oldIsSet)) return true;   // 没有同源记录 ⇒ 照挂

            if (!extrinsicVariable)
            {
                ctx.Log($"{by}：「{u.Name}」身上**已经有**「{caster}」挂的 extrinsic「{key}」"
                      + $"（`buffType {buffType}`）—— 固定型效果**不重复挂**"
                      + "（原版 `CardScript.AddEffect.c:97-101`：`extrinsicVariable == 0` ⇒ `return false`）");
                return false;
            }

            if (ExtrinsicSameEffect(oldValue, oldIsSet, value, isSet))
            {
                ctx.Log($"{by}：「{u.Name}」身上那条「{caster}」的 extrinsic「{key}」**一模一样**"
                      + $"（都是 {value}）—— 重挂没有意义，跳过"
                      + "（原版 `CardScript.AddEffect.c:144`：`IsSameEffect` ⇒ `return false`）");
                return false;
            }

            int removed = u.RemoveExtrinsicEffectsFrom(caster);
            ctx.Log($"{by}：「{u.Name}」身上「{caster}」的 extrinsic「{key}」要换值（{oldValue} ⇒ {value}）"
                  + $"=> 先整份摘掉同源旧记录（摘了 {removed} 条）再挂"
                  + "（原版 `CardScript.AddEffect.c:112/147`：`RemoveExtrinsicEffectsFrom(enchantingCard)`）");
            return true;
        }

        /// <summary><see cref="ExtrinsicAddEffectGate"/> 的**载荷版** —— 把一条 `give` 载荷翻成
        /// `(buffType, key)` 再进闸。调用点 = `EffectResolver.ApplyOneGain` 的第一行。
        ///
        /// <para>⚠️ `extrinsicVariable` 这里**恒传 `false`**：唯一生产点（`pack` 重算）我们没建模，
        /// 见文件头 ③。⛔ 别因为「不知道传什么」就把它写成 `true` —— 那会让固定型效果每次都整份替换。</para>
        /// </summary>
        public static bool ExtrinsicAddEffectGate(BattleContext ctx, UnitState u, PayloadOp p, int val,
                                                  string by, bool viaAddTraitRemnant)
        {
            if (ctx == null || u == null || p == null || !ctx.ResolvingExtrinsic) return true;
            if (p.IsKeyword)
                return ExtrinsicAddEffectGate(ctx, u,
                                              viaAddTraitRemnant ? UnitState.BuffTypeAddTraitRemnant
                                                                 : UnitState.BuffTypeAddTrait,
                                              p.Keyword, val, false, false, by);
            if (p.Attr == "energy") return true;      // 能量是玩家资源、不是这条 `CardEffect` 的格子
            return ExtrinsicAddEffectGate(ctx, u, EffectsToShow.BuffTypeAddStats,
                                          NormalizeAttr(p.Attr), val, false, false, by);
        }

        /// <summary>**这张单位身上有没有「施放者 = <paramref name="caster"/>」的 extrinsic 记录**，
        /// 命中就回填那条的 `(值, 是否覆盖语义)`。
        ///
        /// <para>扫两本账（`StatMod` / `TraitEffectRecord`）—— 它们正是 `ApplyOneGain` 永久档**唯一**的两本账：
        /// 属性走 `AddStatMod`、关键词走 `ApplyTraitEffect`。限时那一档另有 `TempBuff`
        /// （`SourceCard` 存的是 `ctx.PlayingCard`）⇒ **本闸看不到它**，如实记（见文件头 ① ③）。</para>
        /// </summary>
        static bool FindExtrinsicFrom(UnitState u, string caster, int buffType, string key,
                                      out int value, out bool isSet)
        {
            value = 0; isSet = false;
            if (u == null || string.IsNullOrEmpty(caster) || string.IsNullOrEmpty(key)) return false;
            var mods = u.StatMods;
            for (int i = 0; i < mods.Count; i++)
            {
                var m = mods[i];
                if (m == null || !m.Extrinsic) continue;
                if (m.Source != caster || m.BuffType != buffType || m.Attr != key) continue;
                value = m.Value; isSet = m.IsSet;
                return true;
            }
            var traits = u.TraitEffects;
            for (int i = 0; i < traits.Count; i++)
            {
                var r = traits[i];
                if (r == null || !r.Extrinsic) continue;
                if (r.Source != caster || r.BuffType != buffType || r.Trait != key) continue;
                value = r.TraitValue; isSet = false;
                return true;
            }
            return false;
        }

        /// <summary>**`CardEffect.IsSameEffect` 的收敛版** —— 只比我们从记录里读得回来的那几格。
        /// <para>⚠️ 原版比 12 格（含五个 `until*` 位、`costChange`、`optionFlags` 等）；属性那条记录
        /// 我们**没有** `TimingBits` ⇒ 限时/永久在这里分不开（文件头 ① 已记）。</para></summary>
        static bool ExtrinsicSameEffect(int oldValue, bool oldIsSet, int newValue, bool newIsSet)
            => oldValue == newValue && oldIsSet == newIsSet;
    }
}
