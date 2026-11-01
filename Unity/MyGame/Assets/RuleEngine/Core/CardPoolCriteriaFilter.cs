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
//   `FilterMethods__CheckIfRawCardMeetsCriteria.c:18-103`），**缺的是卡池消费端**。
//   本类就是那个消费端 —— 🔴 **2026-11-02 就地订正（铁律 5 · `B116_W41` 现核）**：原句写「`ChooseCardCandidates` 全程只走文本筛…从不看 `op.TargetCriteriaSet`」**已过期**
//   （`B99_C1` 起它两处 `return CardPoolCriteriaFilter.Apply(...)` 就是生产调用点，`报告/B116_W31` 的「0 个生产调用点」是 grep 读数、不是事实）⇒ ✅ **真缺口** = `ta=65` 的**抽牌支**（`Draw two troops …` → `DoDrawType`），现走类尾 `DeckDrawCandidates`。
//
// ============================ 用哪几个 `targetsAffected`（判据只此一处）============================
//   只对【候选池本来就是一批卡】的那几档生效（枚举值 → `d:/2/Warpforge_code/Scripts/Assembly-CSharp/TargetsAffected.cs`）：
//     · 50 `inHandFiltered`            （`:895`，0x32）
//     · 51 `inHandAndDeckFiltered`     （`:907`，0x33）
//     · 52 `inHandAndPlayFiltered`     （`:917`，0x34）
//     · 60 `inCemeteryFiltered`        （`:926`，0x3c）
//     · 65 `inDeckFiltered`            （`:963`，0x41）
//     · ~~180 `choiceResolution`~~      **已删除（2026-10-30）**：判据 = ILSpy 可读版 `BattleManager.cs:25622 GetChoiceFullPool` —— 选牌候选池归 `rawCard.choicesDefinition`，⛔ 不是 `abilityLogic.targetCriteria`（详见本文件 `:73-76`）
//     · ~~190 `lastPlayedIfMeetsCriteria`~~ **已删除（2026-10-30）**，判据 `AbilityLogic__GetTargets.c:1378-1380` 是 `return`，不过末尾 `CheckIfMeetsCriteria` 循环（池内 0 行）
//   ⛔ **其余 `ta` 一律不筛**（返回原池）。理由：`20/30/40/70/80` 是**身份选择**（self/target/attacker/
//   abilityOwner/actingCard），`10/55` 是**棋盘**档，`150` 是「上一条能力的」目标，`200/201` 是引用卡，
//   它们选出来的**从来不是一批卡** ⇒ 拿卡面级 `MatchesRawCard` 去套会**筛错东西**。
//   ⚠️ 这比施工单写的 `ta ∉ {20,30,40,70,80}` **更窄**：施工单把 `ta=10/150` 也算进「可套」，
//   而那两档在那 23 行里分别是 17 行 / 1 行（2026-10-30 现读；⚠️ 全池 `ta=10` 共 399 行 —— 这里只数带 stat 过滤的）—— 更窄只会少筛、不会多筛（安全方向）。
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
//   `cards_engine.json` 1,129 张（2026-10-30 现读，当日 `version` = 45；⚠️ 该字段每次重生成都会变）：`abilityTargetCriteria` 带**四组数值 stat 过滤**的 = 23 行 / 14 张，
//   `ta` 分布 `10×17 · 50×3 · 0×1 · 180×1 · 150×1`。**删掉 180/190 之后**落到本类射程的只有 **3 行**：
//     · ta=50 三行（`AM31`/`GOF24`/`UM20`，全是 `manaCostFilter=1 + option=2(EqualOrHigher)`）。
//   ⚠️ ta=180 那一行（`UM114 Lead From the Front`，`rarityFilter=3 + option=3(EqualOrLower)` + `armyFilter=1`）
//   **已不在射程**（2026-10-30 删 `case 180`，判据见本文件 `:73-76`）；它曾会真的改变结果 —— `rarity <= 3` 剔 `Special(=5)`
//   与 `Legendary(=4)`，而我们的文本筛 `CreatePool.FilterChoose` 的 `non-legendary` **只剔 `legendary`**
//   （`CreatePool.IsLegendary` 只比 `"legendary"`）⇒ 池内 3 张 `Special` 的 Ultramarines 卡（`UM52/53/54`）会漏进来。
//   原版语义（`CardRarity` = `None0/Common1/Rare2/Epic3/Legendary4/Special5`，见 `CardRarity.cs`）是**该剔**的
//   ⇒ `B99_D1` 已实测删 180 后 `play check` 指纹回到基线；这一格留作备忘，⛔ 别照它把 `case 180` 加回去。
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
                // 🔴 2026-10-30 删除 `case 180`（原 `choiceResolution`）：`B99_D1` 实测它会把选牌池筛空 ⇒ `EffectResolver.cs:4556` 越界崩 3 张卡。
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

        /// <summary>🔴 **2026-11-02（执行代理 `B116_W41` · 活状态表 `B-340`）：`ta=65 inDeckFiltered` 的
        /// 【抽牌那一支】的卡池消费端** —— `Draw two troops …`（`GOF39` / `TAU47`）走的是
        /// `EffectResolver.DoDrawType`（按文本翻找牌库），**不经过** <see cref="Apply"/> 原来唯一的
        /// 生产调用点（`EffectResolver.ChooseCardCandidates`）⇒ 那一条支的 36 格 criteria 一直没接。
        ///
        /// <para>**判据**（原版）：`AbilityLogic__GetTargets.c:963`（`0x41 = 65 inDeckFiltered`）用
        /// `GetCardsInDeck` 建池 ⇒ 末尾 `:1086-1095` 对池里每个元素一律 `CheckIfMeetsCriteria`，
        /// 之后按 `targetCriteria.maxTargets` 取 N 张交给「抽」那一跳。我们这一侧「取 N 张」由文本
        /// （`Draw two …` 的 `Amount`）给，**`maxTargets` 不在这里消费**（它那两个数今天对得上，
        /// 但语义是「从牌库取几张」而不是动画目标数 —— 见 `报告/B116_W31_SAN04_27收尾.md` §2）。</para>
        ///
        /// <para>**顺序**：与 `DoDrawType` 改前**逐字相同**（牌库顶 = 列表末尾，往下走）；
        /// 文本筛（<see cref="CreatePool.MatchesKind"/>）在前、criteria 在后（取交集 = 只少不增）。</para>
        ///
        /// <para>🔴 **今天零行为变化**：这条 op 的 `TargetCriteriaSet` 恒 `null`
        /// （`EffectText.AssignAbilityTargets` 的 `logic-gate`：整条 `desc` 一块里两条 op ⇒ 不贴 36 格）
        /// ⇒ 直接原样返回；即使将来挂上，`Apply` 判不了时也原样返回（同一纪律）。
        /// 验收 = `play all dump` 逐字节不变。</para></summary>
        public static List<CardInstance> DeckDrawCandidates(BattleContext ctx, EffectOp op,
                                                            List<CardInstance> deck, string kind)
        {
            var cand = new List<CardInstance>();
            if (deck == null) return cand;
            for (int i = deck.Count - 1; i >= 0; i--)
            {
                var c = deck[i];
                if (c == null || !CreatePool.MatchesKind(c.Card, kind)) continue;
                cand.Add(c);
            }
            if (cand.Count == 0 || op == null || op.TargetCriteriaSet == null) return cand;

            var pool = new List<CardDef>(cand.Count);
            for (int i = 0; i < cand.Count; i++) pool.Add(cand[i].Card);
            int before = pool.Count;
            Apply(ctx, op, pool);                                  // 36 格（同一份判据，⛔ 这里不重写筛选）
            if (pool.Count == before) return cand;                 // 一条都没筛掉（或判不了 ⇒ `Apply` 原样返回）

            // 模板 → 实例回落：**同一模板出现几次就保留前几次**（顺序 = 牌库顶往下，不动）
            var need = new Dictionary<CardDef, int>();
            for (int i = 0; i < pool.Count; i++)
            {
                int n; need.TryGetValue(pool[i], out n); need[pool[i]] = n + 1;
            }
            var keep = new List<CardInstance>(pool.Count);
            for (int i = 0; i < cand.Count; i++)
            {
                int n;
                if (!need.TryGetValue(cand[i].Card, out n) || n <= 0) continue;
                keep.Add(cand[i]); need[cand[i].Card] = n - 1;
            }
            return keep;
        }
    }
}
