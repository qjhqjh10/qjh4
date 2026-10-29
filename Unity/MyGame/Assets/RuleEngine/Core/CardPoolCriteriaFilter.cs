// CardPoolCriteriaFilter.cs — 账 `引擎:#4`（`C5·Q4` v25「候选是卡」）的**卡池消费端**
//
// ============================ 它补的是哪一段 ============================
//   原版 `AbilityLogic.GetTargets`（`d:/2/tools/decomp_full/AbilityLogic__GetTargets.c`）建好候选池之后
//   **一律**跑同一句筛：`:1086-1095` `FilterMethods.CheckIfMeetsCriteria(param_3, 池里那个元素, param_5,
//   param_2 /*criteria*/, 0)`；候选池是**卡**（手牌 `:895` 0x32=50 / 手牌+牌库 `:907` 0x33=51 /
//   手牌+场上 `:917` 0x34=52 / 墓地 `:926` 0x3c=60 / 牌库 `:963` 0x41=65）时也**不例外**。
//   卡模板级那一支（`choiceResolution(180)` 的全卡池回落）另有函数 `:123`
//   `FilterMethods.CheckIfRawCardMeetsCriteria`。
//
//   我们这一侧：**求值入口早就有**（`TargetCriteriaEval.MatchesRawCard`，逐句照
//   `FilterMethods__CheckIfRawCardMeetsCriteria.c:18-103`），**缺的是卡池消费端** ——
//   `RuleCore.ChooseCardCandidates` 全程只走文本筛（`CreatePool.FilterChoose`），从不看 `op.TargetCriteriaSet`。
//   本类就是那个消费端。
//
// ============================ 用哪几个 `targetsAffected`（判据只此一处）============================
//   只对【候选池本来就是一批卡】的那几档生效（枚举值 → `d:/2/Warpforge_code/Scripts/Assembly-CSharp/TargetsAffected.cs`）：
//     · 50 `inHandFiltered`            （`:895`，0x32）
//     · 51 `inHandAndDeckFiltered`     （`:907`，0x33）
//     · 52 `inHandAndPlayFiltered`     （`:917`，0x34）
//     · 60 `inCemeteryFiltered`        （`:926`，0x3c）
//     · 65 `inDeckFiltered`            （`:963`，0x41）
//     · 180 `choiceResolution`         （`:123`，全卡池回落 —— 那一支原版直接调 `CheckIfRawCardMeetsCriteria`）
//     · 190 `lastPlayedIfMeetsCriteria`（名字本身写着「若合条件」）
//   ⛔ **其余 `ta` 一律不筛**（返回原池）。理由：`20/30/40/70/80` 是**身份选择**（self/target/attacker/
//   abilityOwner/actingCard），`10/55` 是**棋盘**档，`150` 是「上一条能力的」目标，`200/201` 是引用卡，
//   它们选出来的**从来不是一批卡** ⇒ 拿卡面级 `MatchesRawCard` 去套会**筛错东西**。
//   ⚠️ 这比施工单写的 `ta ∉ {20,30,40,70,80}` **更窄**：施工单把 `ta=10/150` 也算进「可套」，
//   而那两档在当前卡池里分别是 17 行 / 1 行 —— 更窄只会少筛、不会多筛（安全方向）。
//
// ============================ 身份档那半为什么不做 ============================
//   手牌池原版走的是 `CheckIfMeetsCriteria`（带 `self(20)/target(30)/attacker(40)/abilityOwner(70)/
//   actingCard(80)` 的身份档），**不是** `CheckIfRawCardMeetsCriteria` —— 卡面级没有棋盘、没有身份。
//   所以本类只做**卡面级**能判的那几格（卡种 / spellType / traits / 攻血费稀 / 指定 id / 阵营 / 关键词），
//   身份那半仍由既有单位侧通路负责（`ApplyTargetCriteriaFilter`，本类不碰）。
//
// ============================ `null` = 判不了 ⇒ 不筛（出声一次）============================
//   `MatchesRawCard` 返回 `null` 表示「某一格本版没建模」⇒ **整池原样返回**（⛔ 不当成「不成立」筛空），
//   并用 `StructuredConditionNotice` 出声一次（与 `ApplyTargetCriteriaFilter` 同一条纪律）。
//
// ============================ 现池影响面（2026-10-30 现读）============================
//   `cards_engine.json` v43 / 1,129 张：`abilityTargetCriteria` 带**四组数值 stat 过滤**的 = 23 行 / 14 张，
//   `ta` 分布 `10×17 · 50×3 · 0×1 · 180×1 · 150×1`。落到本类射程的只有 **4 行**：
//     · ta=50 三行（`AM31`/`GOF24`/`UM20`，全是 `manaCostFilter=1 + option=2(EqualOrHigher)`）；
//     · ta=180 一行（`UM114 Lead From the Front`，`rarityFilter=3 + option=3(EqualOrLower)` + `armyFilter=1`）。
//   ⚠️ **`UM114` 那一行会真的改变结果**（如实标，⛔ 不是 bug）：`rarity <= 3` 会剔掉 `Special(=5)` 与
//   `Legendary(=4)`，而我们的文本筛 `CreatePool.FilterChoose` 的 `non-legendary` **只剔 `legendary`**
//   （`CreatePool.IsLegendary` 只比 `"legendary"`）⇒ 池内 3 张 `Special` 的 Ultramarines 卡今天会漏进来。
//   原版语义（`CardRarity` = `None0/Common1/Rare2/Epic3/Legendary4/Special5`，见 `CardRarity.cs`）
//   是**该剔**的 ⇒ 本类接上后与原始一致、但**可能动 `play check` 指纹**（须调度台核/重钉）。
//
// ⚠️ 本文件属 `Core/` —— **不允许依赖 UnityEngine**（`StructuredConditionNotice` 在 `RuleCore` 里，见下）。
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>账 `引擎:#4`：`AbilityLogic.GetTargets` 里「候选是**卡**」那一支的 criteria 筛。
    /// <para>判据 / 射程 / 现池影响面 → 文件头那一整段。⛔ 别把单位侧那一份搬进来（那是
    /// <see cref="RuleCore"/> 的 `ApplyTargetCriteriaFilter`，两处写同一条规则迟早不一致）。</para></summary>
    public static class CardPoolCriteriaFilter
    {
        /// <summary>这个 `targetsAffected` 是不是「候选池本来就是一批卡」的那几档（判据 → 文件头）。
        /// <para>`ta` 为空 / 未知 ⇒ `false`（**不筛**）。</para></summary>
        public static bool IsCardPoolTargetsAffected(int ta)
        {
            switch (ta)
            {
                case 50:   // inHandFiltered            （`GetTargets.c:895`，0x32）
                case 51:   // inHandAndDeckFiltered     （`:907`，0x33）
                case 52:   // inHandAndPlayFiltered     （`:917`，0x34）
                case 60:   // inCemeteryFiltered        （`GetTargets.c:926`，0x3c）
                case 65:   // inDeckFiltered            （`GetTargets.c:963`，0x41）
                // 🔴 2026-10-30 删除 `case 180`（原 `choiceResolution）`：`B99_D1` 实测它会把选牌池筛空 ⇒ `EffectResolver.cs:4556` 越界崩 3 张卡。
                //   判据（ILSpy 可读版 `BattleManager.cs:25622 GetChoiceFullPool`）：选牌候选池归 **`rawCard.choicesDefinition`**，⛔ 不是 `abilityLogic.targetCriteria`；且 `ta==180` 且 `filterSpecificCards` 非空时池子**就是那张表**、一格 36 格都不跑。
                //   ⚠️ 池内 `ta=180` 共 43 行（全部 `filterSpecificCards` 空 + `selectFromChooseResolution=1`）⇒ 今天只有 3 张真走进 `pool` 支，其余 40 张是同一颗雷。
                // 🔴 2026-10-30 删除 `case 190`（`lastPlayedIfMeetsCriteria`）：判据 `AbilityLogic__GetTargets.c:1378-1380` 的 `iVar6==0xbe` 支是 `goto LAB_180818485` = `return *puVar13;` —— **不过末尾那句 `CheckIfMeetsCriteria` 循环**。
                //   `B99_D1` 实测：删它在当前池上零差异（今天没有卡走到这一格）。
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>把 `op.TargetCriteriaSet` 那条 36 格 criteria 套到**卡池**上（原地过滤 + 返回同一份）。
        /// <para>调用的前置：<paramref name="pool"/> 是**候选卡模板表**（`ChooseCardCandidates` 的产物）。</para>
        /// <para>以下任一情况**原样返回**（⛔ 一个元素都不动）：`op`/`set` 为空 · `ta` 不是卡池档 ·
        /// `ta` 判不了 · 某一格 `MatchesRawCard` 判不了。见文件头。</para></summary>
        public static List<CardDef> Apply(BattleContext ctx, EffectOp op, List<CardDef> pool)
        {
            if (pool == null || pool.Count == 0) return pool;
            if (op == null) return pool;
            var set = op.TargetCriteriaSet;
            if (set == null) return pool;                    // 没有 criteria 行 ⇒ 不筛（同 `MatchesRawCard` 的约定）
            int ta = set.TargetsAffected;
            if (ta == CardDef.CountSetSpec.Unknown) return pool;   // 判不了 ⇒ 不筛（⛔ 不当「不成立」）
            if (!IsCardPoolTargetsAffected(ta)) return pool;       // 不是卡池档 ⇒ 不筛（见文件头那张表）

            int before = pool.Count;
            var kept = new List<CardDef>(pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                string gap;
                bool? ok = TargetCriteriaEval.MatchesRawCard(c, set, out gap);
                if (ok == null)
                {
                    // 某一格本版没建模 ⇒ **整池不筛**（与 `ApplyTargetCriteriaFilter` 同一条纪律：宁可不筛也不猜）
                    RuleCore.StructuredConditionNotice(
                        "cardpoolcriteria|" + (op.Source ?? "?") + "|" + op.TargetCriteriaAbility + "|" + op.TargetCriteriaLogic + "|" + gap,
                        $"「{op.Source}」的 36 格 `targetCriteria`（`(ab={op.TargetCriteriaAbility}, lg={op.TargetCriteriaLogic})`）"
                      + $"**本版判不了卡池那一路**：{gap} ⇒ 这一条**不筛候选卡池**"
                      + "（⛔ 没有当成「不成立」、也没有筛空）。");
                    return pool;
                }
                if (ok.Value) kept.Add(c);
            }
            pool.Clear();
            pool.AddRange(kept);
            if (ctx != null && pool.Count != before)
                ctx.Log($"（按原版 `targetCriteria` 36 格筛**候选卡池**：{before} → {pool.Count}；"
                      + $"`ta={ta}`，原版 `AbilityLogic__GetTargets.c:1086-1095`）");
            return pool;
        }
    }
}
