// EffectsToShow.cs — 原版 `EntityScript.GetCardEffectsToShow`（第一道过滤 + mark 四神聚合）的**纯规则核**
//
// 判据（第一权威）：
//   · `d:/2/tools/decomp_full/EntityScript__GetCardEffectsToShow.c:1-196`
//   · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/EntityScript.cs:2149-2497`（可读，推荐对照）
//   · `d:/4/Unity/资料/完全复刻/判据/GetCardEffectsToShow_施工细则.md` §1.3 / §1.4
//
// 🔴 本文件【只】实现「第一道过滤 + mark 聚合」这块**纯规则**（= 施工细则 §1.3 那一段，S1 的规则半边）：
//    ① 外闸（markOfChaosType ∈ {none(0), Random(50)} || trait == markOfChaos(330)）；
//    ② mark 分支（按四神各聚一条、traitValue 累加、占位 = 该神首现位置；God ∉ {10,20,30,40} ⇒ 丢弃不加入）；
//    ③ 非 mark 的两条排除档（changeCost(2)&&costChange==0 · addTrait(3)&&trait∈{1276,1277,1278}）。
//
// ⛔ **不在这里写第二道过滤**（`CardDisplayWindow.DisplayCardEffects` 那道：
//    changeRace(6) / addRace(30) / daemonHost(35) / choiceEnchantment(16)&&!hasQuests）——
//    它**已经有一份** = `UnitState.IsEffectListEntry`（`UnitStateEffectDisplay.cs:61`）。
//    铁律：同一条规则只写一处，⛔ 别在这里抄第二份（两处迟早不一致）。
//
// ⚠️ **整条投影仍未接**（口径同账 `特效动画:521`：整条未实现）：
//    把 7 个源（_statMods / _traitEffects / TempBuffs / CardInstance.ActiveEffects / HandEffects / …）
//    合成「一条有序 List」+ 每条的 `Seq` 序戳（施工细则 S2，落在 `RuleEngine/Core/UnitState.cs`，本批白名单外）
//    + 表现侧消费（`CardDisplayWindow.SetEffectRows`）**都还没做** ⇒ 本类**今天没有生产消费者**，
//    它是先建好的规则核，等 S2 的 Seq + 记录面接齐后由投影层调用。
//
// ⚠️ 本文件属 `Core/` —— **不允许依赖 UnityEngine**。
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>原版 `EntityScript.GetCardEffectsToShow` 的第一道过滤 + mark 四神聚合（判据见文件头）。</summary>
    public static class EffectsToShow
    {
        // ── markOfChaosType 枚举值（`dump.cs` / 施工细则 §1.3）──────────────────────────
        /// <summary>无 mark。</summary>
        public const int MarkNone = 0;
        /// <summary>Khorne。</summary>
        public const int MarkKhorne = 10;
        /// <summary>Nurgle。</summary>
        public const int MarkNurgle = 20;
        /// <summary>Slaanesh。</summary>
        public const int MarkSlaanesh = 30;
        /// <summary>Tzeentch。</summary>
        public const int MarkTzeentch = 40;
        /// <summary>随机一神（外闸放行，但 mark 分支的 default ⇒ 丢弃）。</summary>
        public const int MarkRandom = 50;

        /// <summary>`DefinedTrait.markOfChaos = 330`（第一道外闸与 mark 分支的判据）。</summary>
        public const int TraitMarkOfChaos = 330;

        // ── BuffType 值（`dump.cs:45430-45462`）────────────────────────────────────────
        /// <summary>`BuffType.changeCost = 2`（排除档之一）。</summary>
        public const int BuffTypeChangeCost = 2;
        /// <summary>`BuffType.addTrait = 3`（排除档之一，只对 oath 三条生效）。</summary>
        public const int BuffTypeAddTrait = 3;
        /// <summary>`BuffType.markOfChaos = 8`（`CreateMarkCardEffect` 造出来的聚合条就用它）。</summary>
        public const int BuffTypeMarkOfChaos = 8;
        /// <summary>`BuffType.lastMarkOfChaos = 17`（下游 `LoadEffect` 与 8 同走 mark 双行分支）。</summary>
        public const int BuffTypeLastMarkOfChaos = 17;

        // ── DefinedTrait：被排除的三个 oath（`dump.cs` `DefinedTrait` 段）──────────────
        /// <summary>`oathInAllTurns = 1276`（`0x4fc`）。</summary>
        public const int OathInAllTurns = 1276;
        /// <summary>`oathDouble = 1277`（`0x4fd`）。</summary>
        public const int OathDouble = 1277;
        /// <summary>`oathTripleActivation = 1278`（`0x4fe`）。</summary>
        public const int OathTripleActivation = 1278;

        // ── 环境效果卡 spellType（`RawCardScript.IsEnviromentaleffect`）────────────────
        /// <summary>`spellType == 210`（池内 39 张 defence）。</summary>
        public const int SpellTypeEnv210 = 210;
        /// <summary>`spellType == 200`（池内 0 张，判据照样收）。</summary>
        public const int SpellTypeEnv200 = 200;

        /// <summary>第一道外闸（`.c:40` / `:175-186`）：只放行「普通效果」与「mark」。
        /// <c>markOfChaosType ∈ {none(0), Random(50)} || trait == markOfChaos(330)</c>。</summary>
        public static bool PassesOuterGate(int markOfChaosType, int trait)
        {
            return markOfChaosType == MarkNone || markOfChaosType == MarkRandom || trait == TraitMarkOfChaos;
        }

        /// <summary>普通效果（非 mark）的两条排除档（`.c:131-173`）：
        /// <c>changeCost(2) &amp;&amp; costChange == 0</c> · <c>addTrait(3) &amp;&amp; trait ∈ {1276,1277,1278}</c> ⇒ 不展示。</summary>
        public static bool IsExcludedNormal(int buffType, int costChange, int trait)
        {
            if (buffType == BuffTypeChangeCost && costChange == 0) return true;
            if (buffType == BuffTypeAddTrait)
                return trait == OathInAllTurns || trait == OathDouble || trait == OathTripleActivation;
            return false;
        }

        /// <summary>mark 分支的「四神槽位」：Khorne(10)→0 · Nurgle(20)→1 · Slaanesh(30)→2 · Tzeentch(40)→3；
        /// 其余（none/Random）⇒ <c>-1</c>（原版 `.c:47-130` 的 default ⇒ <c>continue</c>，**丢弃且不占位**）。
        /// ⚠️ 「外闸放行」与「mark 分支接纳」是两件事：trait==330 但 markOfChaosType∈{none,Random} 的，外闸过、这里丢。</summary>
        public static int MarkGodSlot(int markOfChaosType)
        {
            switch (markOfChaosType)
            {
                case MarkKhorne: return 0;
                case MarkNurgle: return 1;
                case MarkSlaanesh: return 2;
                case MarkTzeentch: return 3;
                default: return -1;
            }
        }

        /// <summary>下游是否按「mark 双行合成」渲染（`CardEffectItem.LoadEffect`：`buffType == 8 || == 17`）。</summary>
        public static bool IsMarkBuffType(int buffType)
        {
            return buffType == BuffTypeMarkOfChaos || buffType == BuffTypeLastMarkOfChaos;
        }

        /// <summary>环境效果卡 ⇒ 「受到以下影响」整组必须隐藏（`DisplayCardEffects` `.c:88-105`；
        /// `RawCardScript.IsEnviromentaleffect` = `spellType ∈ {210, 200}`）。</summary>
        public static bool IsEnvEffectCard(int spellType)
        {
            return spellType == SpellTypeEnv210 || spellType == SpellTypeEnv200;
        }

        /// <summary>一条记录会不会被第一道过滤保留（把外闸 + mark 分支 + 排除档合成一次判定，供「只问在不在」的调用方）。
        /// <para>mark 分支（`trait == 330`）⇒ 返回 `MarkGodSlot &gt;= 0`（false ⇒ 该条不进聚合）；</para>
        /// <para>非 mark ⇒ 过外闸且不含排除档。</para></summary>
        public static bool IsKept(int markOfChaosType, int trait, int buffType, int costChange)
        {
            if (!PassesOuterGate(markOfChaosType, trait)) return false;
            if (trait == TraitMarkOfChaos) return MarkGodSlot(markOfChaosType) >= 0;
            return !IsExcludedNormal(buffType, costChange, trait);
        }

        /// <summary>四神聚合的一条**输入**（= `.c` 遍历到的一条 mark 记录）。</summary>
        public struct MarkInput
        {
            /// <summary>`CardEffect.markOfChaosType(+0x78)`。</summary>
            public int MarkOfChaosType;
            /// <summary>`CardEffect.traitValue(+0x40)`。</summary>
            public int TraitValue;
        }

        /// <summary>四神聚合的一条**输出**（= `CreateMarkCardEffect` 那条：`buffType = 8`、`trait = 0`）。</summary>
        public struct MarkAggregate
        {
            /// <summary>归属的神（`10/20/30/40`）。</summary>
            public int MarkOfChaosType;
            /// <summary>同神多条累加后的 `traitValue`。</summary>
            public int TraitValue;
            /// <summary>该神在输入里的**首现下标**（调用方据此把聚合条插回原序的对应位置 —— `.c` 的「占位 = 首现位置」）。</summary>
            public int FirstIndex;
        }

        /// <summary>四神聚合（`.c:47-130`）：按 <paramref name="input"/> 的**给定顺序**，
        /// 每个神**首次出现处**占位、同神 <c>traitValue</c> **累加**；`MarkGodSlot == -1`（none/Random）**丢弃且不占位**。
        /// <para>顺序 = 输入 List 的 `Add` 序、不排序、不截断。返回表按「四神首现序」。</para>
        /// <para>⚠️ 输出条目的 `buffType` 恒 8、`trait` 恒 0（见施工细则 §1.3 的 `CreateMarkCardEffect` 注）；
        /// 与**普通效果**的交错位置由调用方按 <see cref="MarkAggregate.FirstIndex"/> 在合并时保持
        /// （本方法只管四神内部合并，不掺普通效果 —— 那属于 7 源投影，不在这里）。</para></summary>
        public static List<MarkAggregate> AggregateMarks(IReadOnlyList<MarkInput> input)
        {
            var outp = new List<MarkAggregate>();
            if (input == null) return outp;
            int[] slot = { -1, -1, -1, -1 };
            for (int i = 0; i < input.Count; i++)
            {
                int g = MarkGodSlot(input[i].MarkOfChaosType);
                if (g < 0) continue;                       // none/Random ⇒ continue（丢弃、不占位）
                if (slot[g] < 0)
                {
                    slot[g] = outp.Count;
                    outp.Add(new MarkAggregate { MarkOfChaosType = input[i].MarkOfChaosType, TraitValue = 0, FirstIndex = i });
                }
                var a = outp[slot[g]];
                a.TraitValue += input[i].TraitValue;
                outp[slot[g]] = a;
            }
            return outp;
        }
    }
}
