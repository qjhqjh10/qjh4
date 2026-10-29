// CardDefPartial_B98G6.cs — 第九十八会话 · 波 5d · 执行代理 `B98_G6`
//   `CardDef` 的又一半，只装本波「主动能力行 ↔ 正文 op」配对的**两处增强**
//   （账 `战斗逻辑驱动:1282` 的 (a) 格：`AttachByAbilityKind` 读不到的 3 张里的 2 张）：
//     ① `Give A or B` 折成的 `chooseone + RandomPick` **单条** op（`EffectText.EmitEitherOr`）
//        —— `AM22 Primaris Psyker`：伤害族 / 增益族**两边都挂不上** ⇒ `gap:kind-no-op`；
//     ② 同族多行、但各行 `ta` 不同 ⇒ **按 op 的目标形逐条配对**
//        —— `SOR13 Simulacrum Bearer`：`ta=10`（部队）+ `ta=120`（督军）与两条 `give` op 一一对应，
//        而旧口径要求「同族各行的 `ta` 逐格相同」⇒ 永远 `gap:kind-ambiguous`。
//   （第 3 张 `SW9 Fenrisian Wolf`（`ca=320 followAfterResolution`）**不在本件** —— 它要走 ca 分派、
//    落在 `EffectResolver.cs` / `RuleCore.cs`，那两个文件此刻由别的写手持有。）
//
// ============================ 判据（🥇 全量反编译，逐句读）============================
//   · `decomp_full/AbilityLogic__PlayAbility.c:262` —— 每条 logic 行都带自己的 `targetCriteria` 去
//     `AbilityLogic.GetTargets(…)`；`:1179-1234`（`buffOthers(30)`）= 对**该行**选出来的每个目标
//     各复制一份 `logic.buffEffect` 再 `AddEffect` ⇒ **两行 = 两次各自的应用**，不是同一条 op 跑两遍。
//   · `decomp_full/AbilityLogic__GetTargets.c:721-737` —— `friendlyHero(120)` ⇒ 施放方督军；
//     `:184-191` `self(20)` · `:901-908` `any(0)` · `:155-172` `allFiltered(10)` = 按 criteria 筛场上。
//   · 变体判据表 → `资料/完全复刻/判据/B92_战斗判据_B9.md §4·2:424-446`
//     ＋ `资料/完全复刻/判据/B98_R5_波5剩余判据.md §一·B`（本轮施工单）。
//
// ============================ 🔴 为什么另起文件 ============================
//   `CardDef.cs` 的账落点按【行号】引用（现读 86 处，最大 `CardDef.cs:9100`）⇒ 在既有文件里插成员会
//   让它们整体漂移（铁律 18·b）。本块只**新增本文件**；`CardDef.cs` 本体上的三处改动都是
//   **同行改写**（行数 0 变化、零漂移）：`AttachByAbilityKind` 里
//     ① `anyAttached` 的初值 → `TryAttachEitherOrRow(grp, ops)`（二选一那条 op 的独家挂载）；
//     ② `fops` 的匹配条件多一个 `|| ChooseOneBranchHasVerb(fam, op)`（下潜一层看分支动词）；
//     ③ 「同族多行 `ta` 不同」那一支 → `TryAttachRowsByOpTarget(use, fops)`（逐 op 配对）。
//
// ============================ ✅ 执行器已改成「只跑本行 op」（2026-10-29 `B98_G8`）============================
//   **历史**（`B98_G6` 当时的状态，留痕 ⛔ 别当现状）：`RuleCore.UseActiveAbilityCore` 对 `ca ∈ {30, 31}`
//   的行执行的是**全部已挂 op**（`ActiveAbilityBodyOps`），而对其它行只执行**本行**的 op ⇒ 同卡 2 条以上
//   `ca ∈ {30,31}` 时 op 会被跑 N 遍（现池实证：`SOR25 Imagifier` 基线 `6/5 → 8/7 {flank2}`，卡面只写
//   +1/+1/Flank 各一次）。G6 因此留了两道**执行器耦合闸**（① 二选一 op 只挂唯一那条 `ca∈{30,31}` 行；
//   ② 同族多行里有 `ca∈{30,31}` ⇒ 不配对）。
//   🔴 **2026-10-29 `B98_G8` 已把 `RuleCore` 那一支改成「只跑本行（`ActiveAbilityRow == j`）的 op」**
//   （判据 = `AbilityLogic__PlayAbility.c:1179-1234`，补丁 → `资料/完全复刻/报告/B98_G8.md`）
//   ⇒ **闸 ② 已删**（`SOR13` 立刻从 `gap:kind-ambiguous` 变 `attached`）；**闸 ① 保留**——
//   它不再是为了绕执行器，而是「这条 op 横跨两族分支、只能整条一次性执行」的**语义**结论
//   （`TryAttachEitherOrRow`，⛔ 别把闸 ① 也删掉：删了 `AM22` 那条 op 会落 `gap:kind-no-op`）。
using System.Collections.Generic;

namespace RuleEngine
{
    public partial class CardDef
    {
        // ==========================================================================================
        //  ① `chooseone + RandomPick`（`Give A or B`）—— 一条 op 盖着两个族的正文
        // ==========================================================================================

        /// <summary>这条 op 的**分支动词**里有没有这一族的动词。
        ///
        /// <para>形状：`EffectText.EmitEitherOr`（`EffectText.cs:6082`）把 `Give A or B` 折成**一条**
        /// `Verb = "chooseone"` 的 op，各选项存在 <see cref="EffectOp.Payload"/> 里（`|` 分隔的**原文**，
        /// **没有子 op**）⇒ 只能逐项用**同一个解析器**（<see cref="EffectText.ParseSegment"/>）再解一次。
        /// ⛔ **判据只此一处**：动词表仍然只有 `ActiveAbilityFamilyHasVerb` 那一份，这里不另写。</para>
        ///
        /// <para>⚠️ **只认 `RandomPick = true`**：那一式是「**一条 ability 两族分支**」（原版 = 同一 ability
        /// 的两条 logic 行，例 `AM22` 的 `ca = [10, 30]`）。卡面真写 `Choose one:` 的那批 op
        /// （`RandomPick = false`）各选项的目标集未必相同 ⇒ **不猜**（返回 `false`，落 `gap:kind-no-op`）。</para>
        /// </summary>
        static bool ChooseOneBranchHasVerb(string family, EffectOp op)
        {
            if (op == null || family == null) return false;
            if (op.Verb != "chooseone" || !op.RandomPick) return false;
            if (string.IsNullOrEmpty(op.Payload)) return false;
            var parts = op.Payload.Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                string opt = parts[i] != null ? parts[i].Trim() : null;
                if (string.IsNullOrEmpty(opt)) continue;
                var seg = EffectText.ParseSegment(opt);
                if (seg == null || seg.Ops == null) continue;
                for (int k = 0; k < seg.Ops.Count; k++)
                    if (seg.Ops[k] != null && ActiveAbilityFamilyHasVerb(family, seg.Ops[k].Verb)) return true;
            }
            return false;
        }

        /// <summary>`Give A or B`（**整段正文只有这一条** `chooseone + RandomPick` op）⇒ 把它挂到
        /// **唯一的那条** `ca ∈ {30,31}` 逻辑项上，返回 `true`（= 已挂）。
        ///
        /// <para>**为什么挂在 `ca ∈ {30,31}` 那一行**（判据 = 上面的「执行器耦合」）：这张卡的两族分支
        /// 共用**同一条** op（`SubjectTargetsAffected` / `AbilityFurtherMultiplier` 只有一份），
        /// 而执行器对 `ca ∈ {30,31}` 的行会执行**全部已挂 op 恰好一次**、对其它行只执行**本行**的 op
        /// ⇒ 挂在 `ca=10` 那种行上会被同卡的 `ca=30` 行**再跑第二遍**（= 二选一结算两次）。
        /// 那一行同时也是原版「两族分支里唯一能一次跑完整条正文」的那一行。</para>
        ///
        /// <para>**闸门**（任一不过 ⇒ `false`，交给旧的按族配对 / `gap`）：
        /// ⓐ 整段正文**恰好一条** op（多于一条说明这张卡的正文不止二选一，那时整条塞给一行会丢东西）；
        /// ⓑ 这条 op 是 `chooseone + RandomPick` 且还没被别的族认领；
        /// ⓒ 本卡 gate=1 的 `chosenAbility` 里 **`ca ∈ {30,31}` 恰好一项**（两条 ⇒ 会跑两遍，见文件头）。</para>
        /// </summary>
        bool TryAttachEitherOrRow(List<ActiveAbilityLogicSpec> grp, IReadOnlyList<EffectOp> ops)
        {
            if (grp == null || ops == null || ops.Count != 1) return false;               // ⓐ
            var op = ops[0];
            if (op == null || op.Verb != "chooseone" || !op.RandomPick) return false;      // ⓑ
            if (op.ActiveAbilityRow >= 0) return false;

            ActiveAbilityLogicSpec hit = null; int hitJ = -1, n = 0;                       // ⓒ
            foreach (var s in grp)
            {
                if (s == null || s.ChosenAbility == null) continue;
                for (int j = 0; j < s.ChosenAbility.Length; j++)
                {
                    int ca = s.ChosenAbility[j];
                    if (ca != 30 && ca != 31) continue;
                    n++; hit = s; hitJ = j;
                }
            }
            if (n != 1) return false;

            int ta = ActiveRowInt(hit.TargetsAffected, hitJ);
            int fm = ActiveRowInt(hit.FurtherMultiplier, hitJ);
            if (ta < 0 && fm < 0) return false;                                            // 读不到 ⇒ 不挂
            if (ta >= 0) op.SubjectTargetsAffected = ta;
            if (fm >= 0) op.AbilityFurtherMultiplier = fm;
            op.ActiveAbilityRow = hitJ;
            // 36 格 criteria **不挂**：这条 op 横跨两族（伤害/增益各自的目标集不同）⇒ 挂哪一行的都是猜。
            // （`DoChooseOne` 只用 `Payload` 里的选项原文重新解析，不读这两格。）
            op.TargetCriteriaGap = "active-either-or";
            return true;
        }

        // ==========================================================================================
        //  ② 同族多行、`ta` 不同 —— 按 op 的目标形逐条配对
        // ==========================================================================================

        /// <summary>`ta` 与**这条 op 的目标**对得上吗（`true` 命中 / `false` 明确不是 / `null` 判不了）。
        ///
        /// <para>只判**有确切判据的两族形状**（`AbilityLogic__GetTargets.c:721-746` +
        /// `TargetsAffected` 枚举 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/TargetsAffected.cs`）：
        /// <list type="bullet">
        /// <item>`ta = 120 friendlyHero` ⇒ 施放方督军；`130 enemyHero` ⇒ 对方督军
        ///   ⇒ 目标写 `warlord` 才对得上（`own`/`self` ↔ 120、`enemy` ↔ 130）。</item>
        /// <item>`ta = 10 allFiltered` / `55 inPlayOrDyingFiltered` ⇒ 场上按 criteria 筛
        ///   ⇒ 目标写 `troop`（**部队**）才对得上；写 `warlord` 的**明确不是**
        ///   （督军 ≠ 部队，判据 = `CLAUDE.md` §二 那条「单位 ≠ 部队」）。</item>
        /// </list>
        /// ⚠️ 其余组合一律 `null`（判不了）—— ⛔ 不在这里扩展一张「ta ↔ 文本」的大表：没有判据的格
        /// 一扩展就会「看着有依据」地贴错目标集（本仓红线：「宁可读不到」）。</para>
        /// </summary>
        public static bool? RowMatchesOpTarget(int ta, EffectOp op)
        {
            var t = op != null ? op.Target : null;
            if (t == null || ta < 0) return null;

            if (t.Kind == "warlord")
            {
                if (ta == 120)                                   // friendlyHero
                    return (t.Side == "own" || t.Side == "self") ? true
                         : (t.Side == "enemy" ? (bool?)false : null);
                if (ta == 130)                                   // enemyHero
                    return t.Side == "enemy" ? true
                         : ((t.Side == "own" || t.Side == "self") ? (bool?)false : null);
                return null;
            }

            if (t.Kind == "troop")
            {
                if (ta == 10 || ta == 55) return true;           // allFiltered / inPlayOrDyingFiltered
                if (ta == 120 || ta == 130) return false;        // 督军不是部队
                return null;
            }

            return null;
        }

        /// <summary>同族多行、各行 `(ta, fm)` **不同**时：逐条 op 按目标形配对，全部唯一命中才挂。
        ///
        /// <para>判据：`AbilityLogic__PlayAbility.c:1179-1234`（`buffOthers` = 对**该行**每个目标各应用
        /// 一次该行的 `buffEffect`）⇒ 行与 op 的对应关系由**目标集**决定，⛔ 不能「所有 op 都贴第 0 行」
        /// （那正是旧口径：同族多行只认「逐格相同」⇒ `SOR13` 永远 `gap:kind-ambiguous`）。</para>
        ///
        /// <para>**唯一性**：每条 op 必须有**恰好一条**行说 `true`（两条行都说 `true` ⇒ 歧义；一条都没有
        /// ⇒ 配不上）⇒ 返回 `false`，调用方落 `gap:kind-ambiguous`（⛔ 不挑第一个 = 位置假设）。</para>
        ///
        /// <para>✅ **执行器耦合闸已于 2026-10-29 由 `B98_G8` 删除**：`RuleCore.UseActiveAbilityCore` 现在对
        /// `ca ∈ {30,31}` 的行**只跑本行（`ActiveAbilityRow == j`）的 op**（判据 =
        /// `AbilityLogic__PlayAbility.c:1179-1234`，补丁 → `报告/B98_G8.md`）⇒ 多行不再把同一条 op 跑 N 遍，
        /// 本方法可以放心逐条配对（`SOR13` 因此立刻从 `gap:kind-ambiguous` 变 `attached`）。</para>
        /// </summary>
        bool TryAttachRowsByOpTarget(List<KeyValuePair<ActiveAbilityLogicSpec, int>> use, List<EffectOp> fops)
        {
            if (use == null || fops == null || fops.Count == 0 || fops.Count > use.Count) return false;

            var pick = new int[fops.Count];
            var taken = new bool[use.Count];
            for (int k = 0; k < fops.Count; k++)
            {
                int hit = -1;
                for (int i = 0; i < use.Count; i++)
                {
                    if (taken[i]) continue;
                    if (RowMatchesOpTarget(ActiveRowInt(use[i].Key.TargetsAffected, use[i].Value), fops[k]) != true)
                        continue;
                    if (hit >= 0) return false;                                   // 两行都对得上 ⇒ 歧义
                    hit = i;
                }
                if (hit < 0) return false;                                        // 这条 op 配不上任何一行
                pick[k] = hit;
                taken[hit] = true;
            }

            for (int k = 0; k < fops.Count; k++)
            {
                var spec = use[pick[k]].Key;
                int j = use[pick[k]].Value;
                int ta = ActiveRowInt(spec.TargetsAffected, j);
                int fm = ActiveRowInt(spec.FurtherMultiplier, j);
                if (ta >= 0) fops[k].SubjectTargetsAffected = ta;
                if (fm >= 0) fops[k].AbilityFurtherMultiplier = fm;
                fops[k].ActiveAbilityRow = j;
                // 36 格 criteria：多行时「挂哪一行」仍是猜（各行 criteria 可能不同）⇒ 照旧留 gap 并出声。
                fops[k].TargetCriteriaGap = "active-multi-row";
            }
            return true;
        }

        // ==========================================================================================
        //  ③ 行键 = `(能力对象, 行号)` 二元 —— 「这些 op 是哪个对象挂出来的」这一格
        //     （🆕 2026-10-29 · `B98_H1` · 报告 `报告/B98_H1.md` §一）
        // ==========================================================================================
        /// <summary>**`ApplyActiveAbilityLogic` 实际用的是哪个能力对象**（`-1` = `activeAbility` /
        /// `-2` = `alternativeAbility`）；`int.MinValue` = 判不出（这张卡**没有** `gate = 1` 的行
        /// ⇒ 一条 op 都不该挂）。
        ///
        /// <para>**为什么需要它**：`EffectOp.ActiveAbilityRow` 只存**行号**，而一张卡可以有**两个**
        /// `activeAbilityLogic` 对象（现池唯一一例 = `SW21 Wulfen Pack Leader`：两个 `gate = 1` 对象、
        /// 各 1 行、`ca` 都是 `[10]`）⇒ 两行的行号都是 0。执行器要知道「这一行的 op 属于哪个对象」
        /// 才谈得上 `(对象, 行)` 二元键（消费端 = `RuleCore.ActiveRowOpsOf(UnitState,int,int)`）。</para>
        ///
        /// <para>🔴 **这是 `ApplyActiveAbilityLogic` 闸① 的镜像，⛔ 不是第二份判据**：
        /// 那一处按 `ActiveAbilityObjectPair`（先 `-1` 后 `-2`）逐对象收 `gate == 1` 的行、
        /// 只拿 `groups[0]` 去挂 op。这里逐字照同一个次序取**第一个有 `gate = 1` 行的对象**。
        /// ⚠️ 两份会不一致的唯一情形 = 有人改了挂载侧的次序/闸 ⇒ **那时两处一起改**
        /// （挂载侧改完 `ActiveAbilityMap` 会立刻对不上，自检会红，不会静默）。</para>
        /// </summary>
        internal int ActiveAbilityObjectOfAttachedOps()
        {
            if (_activeAbilityLogic == null) return int.MinValue;
            foreach (var r in _activeAbilityLogic)          // 闸① 的另一半：gate **未知** ⇒ 挂载侧整体不挂
                if (r != null && r.Gate < 0) return int.MinValue;
            foreach (int ab in ActiveAbilityObjectPair)
            {
                var g = ActiveAbilityLogicGroup(ab);
                if (g == null) continue;
                for (int i = 0; i < g.Count; i++)
                    if (g[i] != null && g[i].Gate == 1) return ab;
            }
            return int.MinValue;
        }
    }
}
