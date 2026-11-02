// EnchantmentPayload_B121D1B.cs — 第一百二十一~二十二会话 · 执行写手 `B121-D1B`
//   **附魔模板 `abilityLogic[]` 的正文载荷**（`healCriteria` / `manaBuff` / 目标集 36 格）的运行时载体
//
// ── 为什么另起 partial 文件（⛔ 别搬进 CardDef.cs 中间）────────────────────────────────────────
//   `CardDef.cs` 被 `账/*.tsv` 按【行号】钉了大量落点（现读最大 `CardDef.cs:9436` 一带）
//   ⇒ 在类中间插成员会让下游行号整体漂移（铁律 18·b）。本文件是**新文件**，类成员全部追加在这里。
//
// ── 本文件只补【本仓还没有】的两类载荷 ─────────────────────────────────────────────────────
//   · 伤害 → **复用** `CardDef.AbilityDamageSpec` + `AttachAbilityDamage`（`CardDef.cs:6797/6822`）；
//   · 召唤 → **复用** `CardDef.AbilitySummonSpec` + `AttachAbilitySummon`（`CardDef.cs:9138/9175`）；
//   · 目标集 36 格 → **复用** `CardDef.CountSetSpec` / `TargetCriteriaRowSpec`（`CardDef.cs:2304/2528`），
//     但 `_abilityTargetCriteria` 原来**只有构造函数能写**（`CardDef.cs:1196`）⇒ 这里补一个 public `Attach`；
//   · **治疗 / 法力** → 本仓**没有**载荷类型 ⇒ 本文件定义（字段照原版，见下）。
//
// ── 判据（第一权威 = `d:/2/tools/decomp_full/` 逐句现读）──────────────────────────────────────
//   · `healCriteria`（`HealCriteria`，`dump.cs`）：`valueOptions(+0x10)` · `minValue(+0x14)` · `maxValue(+0x18)`
//     —— 读点 = `AbilityLogic__PlayAbility.c:1262-1306`（`ca 20 doHeal`，`param_1 + 0x48`）。
//   · `manaBuff`（`ManaBuff`）：读点 = `AbilityLogic__PlayAbility.c:685-721`（`ca 60 buffMana`，`param_1 + 0x68`），
//     逐格拷贝顺序 `+0x10..+0x1D` = `manaAmount / friendlyPlayer / enemyPlayer / createMana / refillMana /
//     untilEndOfTurn / psychicMana / plasmaMana / spiritStoneMana / faithMana / questPoints`；
//     消费 = `BattleManager__ResolveManaBuff.c:94-104`（两侧各跑一遍）+ `BattleManager__ProcessManaBuff.c`
//     （优先级：spiritStone → faith → questPoints → 普通法力 create/refill）。
//   · 全部字段 `-1` = **缺键 / 旧表（未知）**，⛔ 不是 `0`（`0` 是合法真值，例如 `valueOptions = Normal`）。
//     `null`（整个数组）= **整列未知**；空数组 = **明确没有这一行** —— 同 `AttachAbilityDamage` 的三态口径。
using System.Collections.Generic;

namespace RuleEngine
{
    public partial class CardDef
    {
        /// <summary>🆕 B121-D1B：一行 `abilityLogic[].healCriteria`（原版 `HealCriteria`）。
        /// 三格 `-1` = 缺键/坏值 —— ⛔ 不是 `0`。</summary>
        public sealed class AbilityHealSpec
        {
            /// <summary>`cardAbilities` 下标。</summary>
            public int Ab = -1;
            /// <summary>`abilityLogic` 下标。</summary>
            public int Lg = -1;
            /// <summary>原版 `healCriteria.minValue (+0x14)`。</summary>
            public int Min = -1;
            /// <summary>原版 `healCriteria.maxValue (+0x18)`。</summary>
            public int Max = -1;
            /// <summary>原版 `healCriteria.valueOptions (+0x10)`（`0` 固定/区间，其余档见 `GetValueFromCriteria`）。</summary>
            public int ValueOptions = -1;
        }

        AbilityHealSpec[] _abilityHeals;

        /// <summary>`abilityHeal` 列是否来自卡表（`null` = 未知）。</summary>
        public bool HasAbilityHeal { get { return _abilityHeals != null; } }

        /// <summary>`null` = 旧表（未知）⇒ 整列判不了；`[]` = 明确没有。</summary>
        public void AttachAbilityHeal(AbilityHealSpec[] rows)
        {
            _abilityHeals = rows;
        }

        /// <summary>第 <paramref name="ability"/> 条第 <paramref name="logic"/> 行的 `healCriteria`。
        /// `null` = 没有这一行 / 旧表 / 坏格。</summary>
        public AbilityHealSpec AbilityHealAt(int ability, int logic)
        {
            if (_abilityHeals == null || ability < 0 || logic < 0) return null;
            for (int i = 0; i < _abilityHeals.Length; i++)
            {
                var s = _abilityHeals[i];
                if (s != null && s.Ab == ability && s.Lg == logic) return s;
            }
            return null;
        }

        /// <summary>🆕 B121-D1B：一行 `abilityLogic[].manaBuff`（原版 `ManaBuff`，十一格）。
        /// 字段名 = 原版字段名；全部 `-1` = 缺键/坏值（⛔ 不是 `0`）。</summary>
        public sealed class AbilityManaBuffSpec
        {
            /// <summary>`cardAbilities` 下标。</summary>
            public int Ab = -1;
            /// <summary>`abilityLogic` 下标。</summary>
            public int Lg = -1;
            /// <summary>原版 `ManaBuff.manaAmount (+0x10)`（数量；`ca 60` 里再乘 `local_178`）。</summary>
            public int ManaAmount = -1;
            /// <summary>原版 `ManaBuff.friendlyPlayer (+0x14)`：真 ⇒ 给**载体属主**那一边。</summary>
            public int FriendlyPlayer = -1;
            /// <summary>原版 `ManaBuff.enemyPlayer (+0x15)`：真 ⇒ 给**对手**那一边（`ResolveManaBuff.c:96-104`）。</summary>
            public int EnemyPlayer = -1;
            /// <summary>原版 `ManaBuff.createMana (+0x16)` ⇒ `PlayerManager.AddNewMana`（**永久抬上限**）。</summary>
            public int CreateMana = -1;
            /// <summary>原版 `ManaBuff.refillMana (+0x17)` ⇒ `PlayerManager.RefillOneMana`（补当前能量）。</summary>
            public int RefillMana = -1;
            /// <summary>原版 `ManaBuff.untilEndOfTurn (+0x18)`（本版没有「限时能量」模型 ⇒ 只存档、不出声）。</summary>
            public int UntilEndOfTurn = -1;
            /// <summary>原版 `ManaBuff.psychicMana (+0x19)`（本版法力只有一条能量池 ⇒ 只存档）。</summary>
            public int PsychicMana = -1;
            /// <summary>原版 `ManaBuff.plasmaMana (+0x1a)`（同上）。</summary>
            public int PlasmaMana = -1;
            /// <summary>原版 `ManaBuff.spiritStoneMana (+0x1b)` ⇒ 灵魂石 +N（`ProcessManaBuff.c:88-104`）。</summary>
            public int SpiritStoneMana = -1;
            /// <summary>原版 `ManaBuff.faithMana (+0x1c)` ⇒ 信仰 +N（`ProcessManaBuff.c:66-87`）。</summary>
            public int FaithMana = -1;
            /// <summary>原版 `ManaBuff.questPoints (+0x1d)` ⇒ 任务点 +N（`ProcessManaBuff.c:40-65`）。</summary>
            public int QuestPoints = -1;
        }

        AbilityManaBuffSpec[] _abilityManaBuffs;

        /// <summary>`abilityManaBuff` 列是否来自卡表（`null` = 未知）。</summary>
        public bool HasAbilityManaBuff { get { return _abilityManaBuffs != null; } }

        /// <summary>`null` = 旧表（未知）；`[]` = 明确没有。</summary>
        public void AttachAbilityManaBuff(AbilityManaBuffSpec[] rows)
        {
            _abilityManaBuffs = rows;
        }

        /// <summary>第 <paramref name="ability"/> 条第 <paramref name="logic"/> 行的 `manaBuff`。</summary>
        public AbilityManaBuffSpec AbilityManaBuffAt(int ability, int logic)
        {
            if (_abilityManaBuffs == null || ability < 0 || logic < 0) return null;
            for (int i = 0; i < _abilityManaBuffs.Length; i++)
            {
                var s = _abilityManaBuffs[i];
                if (s != null && s.Ab == ability && s.Lg == logic) return s;
            }
            return null;
        }

        /// <summary>🆕 B121-D1B：`AttachAbilityTargetCriteria` 收下的 `conditionsFilter` / `excludeCondition`
        /// （原版 `TargetCriteria` 里的两格；**不在** `CountSetSpec` 里 —— 卡表路用 `unitConditions` 的
        /// `role="target"` 行表达同一件事，附魔模板没有那一列 ⇒ 这里单独存一份，消费端拼成
        /// <see cref="ConditionSpec"/> 传给 `TargetCriteriaEval.Matches` 的 `cond` 形参）。
        /// 键 = 同 `(Ab, Lg)`；**没有这一条** = 两格都是 0/缺键 ⇒ `ConditionIsNone` 返回真（放行）。</summary>
        readonly Dictionary<long, ConditionSpec> _abilityTargetCondition = new Dictionary<long, ConditionSpec>();

        /// <summary>第 <paramref name="ability"/> 条第 <paramref name="logic"/> 行的 `cond`/`ex`。
        /// `null` = 没有这一行 / 两格都是 0（none ⇒ 放行）。</summary>
        public ConditionSpec AbilityTargetConditionAt(int ability, int logic)
        {
            if (ability < 0 || logic < 0) return null;
            ConditionSpec s;
            return _abilityTargetCondition.TryGetValue(CriteriaKey(ability, logic), out s) ? s : null;
        }

        /// <summary>🆕 B121-D1B：**logic 级 `targetCriteria` 的 36 格**（`CountSetSpec`）的构造后挂载口。
        ///
        /// <para>为什么需要：`_abilityTargetCriteria` 原来**只有构造函数**能写（`CardDef.cs:1196`），
        /// 而附魔模板的 `CardDef` 是在 `CardDatabase.LoadEnchantmentTemplates` 里**构造之后**才拿到表数据的
        /// （与 `AttachAbilityDamage` / `AttachAbilitySummon` 同族）⇒ 补这一个口。</para>
        /// <para>三态：`rows == null` ⇒ **整列未知**（什么都不动，`AbilityTargetCriteriaAt` 恒 `null`）；
        /// 空数组 ⇒ **列到位、明确没有行**（把 `AbilityTargetCriteriaVersioned` 置真）；否则逐行收下。
        /// 同一 `(Ab, Lg)` 重复 ⇒ 后一条丢弃并出声（⛔ 撞键谁赢都说不清，同构造函数那条闸）。</para>
        /// </summary>
        public void AttachAbilityTargetCriteria(IEnumerable<TargetCriteriaRowSpec> rows)
        {
            if (rows == null) return;                     // 未知（⛔ 别折成「明确没有」）
            AbilityTargetCriteriaVersioned = true;
            foreach (var r in rows)
            {
                if (r == null || r.Criteria == null) continue;
                if (r.Ability < 0 || r.Logic < 0) continue;
                long k = CriteriaKey(r.Ability, r.Logic);
                if (_abilityTargetCriteria.ContainsKey(k))
                {
                    UnityEngine.Debug.LogError(
                        $"[RuleEngine] 卡「{Name}」（{Id}）的 `abilityTargetCriteria` 有**重复的 "
                      + $"(ab={r.Ability}, lg={r.Logic})** —— 后一条已丢弃（⛔ 撞键 = 谁赢都说不清）。");
                    continue;
                }
                _abilityTargetCriteria[k] = r.Criteria;
                // 🆕 B121-D1B：`cond`/`ex` 原值（两格都 0 ⇒ 不存 = none = 放行，与 `unitConditions` 缺行同义）
                if ((r.ConditionsFilter > 0) || (r.ExcludeCondition > 0))
                {
                    _abilityTargetCondition[k] = new ConditionSpec
                    {
                        Ability = r.Ability, Logic = r.Logic, Role = "target",
                        Condition = r.ConditionsFilter,
                        Exclude = r.ExcludeCondition > 0,
                    };
                }
            }
        }
    }
}
