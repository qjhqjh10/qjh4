// CardAbilityDispatch_B116.cs — 执行写手 `B116_W27`（2026-11-02 · 第一百一十六会话 · 活状态表 `B-211` ＋ `B-255`）
//
// ============================ 这件活是什么 ============================
// 原版 `trigger 0`（`AbilityTrigger.ThisCardPlayed`）的派发形状 =
//   **遍历 `cardAbilities(+0x290)` → 逐条 `CanTriggerAbility` → 逐条 `PlayAbility`**；
// 我们只有「把 `desc` 解析成一条扁平 op 流」这一条路 ⇒ 缺的是「**一行 ability 一份结算**」这一层。
// 本文件补这一层，并在**两处出牌点**各挂一次**卡级**派发行：
//   · 单位 `Core/RuleCore.cs:4222` 那一物理行之后（walk `Rally` 正文之后）；
//   · 战术 `Core/EffectResolver.cs:2805`（`tactic-body` 入队之后，独立队列动作）。
// 判据（⛔ 不是我挑的）：
//   · `资料/完全复刻/判据/B116_R6_B211逐ability派发施工面.md`（6 步施工 + B 表 B1..B9 + D 表断言）
//   · `资料/完全复刻/判据/B116_R9_B211三个判据空白.md`（320/325/360 三支）
//   · `decomp_full/AbilityLogic__PlayAbility.c:286`（`chosenAbility` 的巨型 switch）
//   · `decomp_full/RawCardScript__TriggerAbilities.c:201-215`（逐条 `CanTriggerAbility` → 逐条 `TriggerAbility`）
//   · `decomp_full/AbilityLogic__PlayAbility.c:2128-2160`（380 写 `actingCard.referencedCards`）
//   · `decomp_full/AbilityLogic__PlayAbility.c:2244` / `:2471-2478`（320 = `AddFollowAfterResolution`）
//
// ============================ 本批【只接两支】============================
//   · `380 setReferenceCard` —— 现池 **3 行 / 3 卡**（`AM54` ta=150 · `GOF93` ta=150 · `GOF104` ta=30）
//   · `320 followAfterResolution` —— 现池 **15 行**，**只对战术/防御路**派（`FollowUpSchedulerOf == "ThisCardPlayed"`）；
//     单位卡的 320 已由关键词路（`FireTriggerDeferred` 的 `FollowUpSchedulerOf` 那一跳）负责 ⇒ 这一层**不派**，
//     否则 `GOF93` 那类「单位 + 320」会从 1 拍变 2 拍。
//   ⛔ `325`/`360`/`480` 本批**出声不跑**（执行器另立：`325` 的门必须在伤害结算前判、`360` 的选项池 17/18 在池外）
//   ⛔ 本层**不发**逐 ability 的 `EvtKind.Trigger`：`BattleContext.Emit` 会进
//      `CountTriggerForAchievements`（成就计数器）⇒ 多派一条 = 成就重复计数。判据 → `BattleContext.cs:1868-1890`。
//
// ============================ 灭自证 ============================
//   · `AbilityLayerOwnsRow` 六组夹具（B1..B6）：把 `ca < 0` 折成 `0` ⇒ B3 红；
//     把 `null`（列未知）当「明确没有」⇒ B1 红；把 `320` 去掉战术限定 ⇒ 单位卡的 320 会被本层多派一次 ⇒ D4 红。
//   · 380 端到端（`GOF104`）：不写引用表 ⇒ C 段红；不判 `ta`、一律拿 `chosen` ⇒ `AM54`（ta=150）条数错 ⇒ D 段红。
//   · `AbilityLayerOwns320Row`：改成恒 `true` ⇒ `ASH73`（只有 325）硬编码那一拍被让路 ⇒ 0 拍 ⇒ B 段红；
//     改成恒 `false` ⇒ `AM54` 那 17 张双发 ⇒ B 段红。**两侧不可能同时绿**。
using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ======================================================================================
        //  一、行级判定（B 表 · 唯一出处）
        // ======================================================================================

        /// <summary>这一行 `(ab, lg)` 的 `targetsAffected`（原版 `AbilityLogic.targetCriteria.targetsAffected`）。
        /// <para>数据面 = `CardDef.AbilityTargets[ab].TargetsAffected[lg]`（`abilityTargets` 列，按 `(ab,lg)` 对齐；
        /// ⚠️ 它和 `triggerAbilityIndices[].ta` **不是同一列** —— 后者 `MapTriggerAbilityIndices` 还没写，恒 `-1`）。</para>
        /// <returns>`-1` = 读不到（旧表 / 下标越界 / 该格坏值）。</returns></summary>
        public static int TargetsAffectedOfRow(CardDef card, CardDef.TriggerAbilityIndexSpec row)
        {
            if (card == null || row == null) return -1;
            var ats = card.AbilityTargets;
            if (ats == null || row.Ab < 0 || row.Ab >= ats.Count) return -1;
            var spec = ats[row.Ab];
            if (spec == null || spec.TargetsAffected == null) return -1;
            if (row.Lg < 0 || row.Lg >= spec.TargetsAffected.Length) return -1;
            return spec.TargetsAffected[row.Lg];
        }

        /// <summary>`B-211` 派发面**唯一**判定：这一行归 **ability 层**（本文件派）还是归**文本层**（卡面 `desc` 解析出的 op）。
        /// <para>四档（判据 = 施工面 §B 表）：</para>
        /// <list type="bullet">
        /// <item>B1 `TriggerAbilityRowsOf == null` —— 列未知（旧表 &lt; v42）⇒ **不进**（照旧走文本层）；</item>
        /// <item>B2 `Length == 0` —— 明确没有这条 trigger ⇒ **不进**；</item>
        /// <item>B3 `Ca &lt; 0` —— `-1` = 未知（`lg == -1` / 旧表 &lt; v47 / 手写夹具）⇒ **不进**；</item>
        /// <item>B4 `Ca ∈ {325,360,380,480}` —— 本体是 meta-op（卡面文本结构上表达不出来）⇒ **进**；</item>
        /// <item>B5 `Ca == 320` —— **只有战术/防御路**（`FollowUpSchedulerOf == "ThisCardPlayed"`）进；
        ///   单位卡的 320 由关键词路负责（否则双发）；</item>
        /// <item>B6 其余（真效果种类）—— 卡面正文就在跑它 ⇒ **不进**。</item>
        /// </list>
        /// <para>⛔ 别在调用点再抄一份 —— 本函数是唯一判据。</para></summary>
        public static bool AbilityLayerOwnsRow(CardDef card, CardDef.TriggerAbilityIndexSpec row, out string why)
        {
            why = null;
            if (card == null) { why = "没有卡 ⇒ 判不了"; return false; }
            if (row == null) { why = "没有行 ⇒ 判不了"; return false; }
            var rows = card.TriggerAbilityRowsOf(row.Trigger);
            if (rows == null) { why = "`triggerAbilityIndices` 列未知（旧表 < v42）⇒ 照旧走文本层"; return false; }   // B1
            if (rows.Length == 0) { why = "这张卡明确没有这条 trigger ⇒ 照旧走文本层"; return false; }                // B2
            int ca = row.Ca;
            if (ca < 0) { why = "`chosenAbility` 未知（`-1`：`lg == -1` / 旧表 < v47 / 手写夹具）⇒ 照旧"; return false; }   // B3
            if (ca == 320)                                                                                            // B5
            {
                if (FollowUpSchedulerOf(card) != "ThisCardPlayed")
                { why = "`320` 但载体不是战术/防御（单位卡的 320 由关键词路 `FireTriggerDeferred` 负责）⇒ 本层不派"; return false; }
                why = "`320 followAfterResolution`（战术/防御路）";
                return true;
            }
            if (ca == 325 || ca == 360 || ca == 380 || ca == 480)                                                     // B4
            { why = "`ca " + ca + "` 是 meta-op（卡面文本结构上表达不出来）"; return true; }
            why = "`ca " + ca + "` 是真效果种类 ⇒ 卡面正文 op 负责";                                                  // B6
            return false;
        }

        /// <summary>`B-255` 排他闸的**唯一判据**：这张卡的 `trigger 0` 上那一段「后续结算」表现拍
        /// （`EffectResolver.cs:2917` 的硬编码块）该不该**让路**给本层。
        /// <para>🔴 **必须数据驱动**（施工面 §B·5 的实证）：硬编码表 18 张里 **17 张**数据上确有 `ca 320`；
        /// `ASH73` **只有 `ca 325`** ⇒ 让路条件若写成「表命中就让路」，`ASH73` 会从 **1 拍变 0 拍**。
        /// ⇒ 条件 = 「战术/防御路 ∧ 数据里确有 `ca 320` 行」（三态：`null`/`[]` ⇒ **不让路**）。</para>
        /// <para>🧨 灭自证：改成 `return FollowUpSchedulerOf(card) != null;` ⇒ `ASH73` 0 拍 ⇒ 断言 B 段红；
        /// 改成 `return false;` ⇒ 那 17 张双发 ⇒ 断言 B 段红。</para></summary>
        public static bool AbilityLayerOwns320Row(CardDef card)
        {
            if (card == null) return false;
            if (FollowUpSchedulerOf(card) != "ThisCardPlayed") return false;
            var rows = card.TriggerAbilityRowsOf(0);
            if (rows == null || rows.Length == 0) return false;
            for (int i = 0; i < rows.Length; i++)
                if (rows[i] != null && rows[i].Ca == 320) return true;
            return false;
        }

        // ======================================================================================
        //  二、派发（两处出牌点各一次）
        // ======================================================================================

        /// <summary>`B-211` 的**卡级派发口**：遍历 `card.TriggerAbilityRowsOf(trigger)`，逐行过 B1..B8 后按 `ca` 分派。
        /// <para>**为什么是卡级而不是 `UnitState` 级**：战术 / 防御卡结构上没有 `UnitState` —— 这正是
        /// `B-255` 结构上接不上的真因（施工面 §〇）。</para>
        /// <para>⚠️ **`tacticPath` 是必需形参、不是可选装饰**：`320` 只允许战术路派（见 `AbilityLayerOwnsRow` B5），
        /// 单位路（`RuleCore.cs:4222`）传 `false` ⇒ 它那一段 `Rally` 关键词路已经在跑 320 了。</para>
        /// <para>⚠️ `tacticSide` = `PlayTacticCore` 算出来的 `side`（`"enemy"` / `""`），只服务 320 那一拍的落点
        /// （与改前硬编码块**逐字同一算式**，⛔ 别另起一套）。</para>
        /// <returns>真派出去的**行数**（0 = 这一张没有本层该跑的行 = 改前行为）。</returns></summary>
        internal static int DispatchCardAbilityRows(BattleContext ctx, CardDef card, CardInstance inst, int trigger,
                                                    int owner, int slot, UnitState actingCard, UnitState targetCard,
                                                    bool tacticPath = false, string tacticSide = null)
        {
            if (ctx == null || card == null) return 0;
            var rows = card.TriggerAbilityRowsOf(trigger);
            if (rows == null || rows.Length == 0) return 0;                 // B1 / B2（三态：⛔ 别把 null 折成空）
            int fired = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (row == null) continue;
                string why;
                if (!AbilityLayerOwnsRow(card, row, out why)) continue;      // B3 / B5 / B6
                int ca = row.Ca;
                if (ca == 320 && !tacticPath) continue;                      // B5 的双保险（单位路不派 320）
                if (CardAbilityRowCoveredByTextLayer(card, trigger, row, out why))
                {
                    NoteCardAbilityRowSkippedByTextCoverage(card, trigger, row, why);   // B7
                    continue;
                }
                string gateWhy;
                bool? gate = AbilityCriteriaAllowsAt(ctx, owner, thisCard: actingCard, card: card,
                                                                abilityIndex: row.Ab,
                                                                actingCandidate: actingCard, targetCandidate: targetCard,
                                                                out gateWhy);
                if (gate == false)
                {
                    NoteCardAbilityRowGateRejected(card, trigger, row, gateWhy);
                    continue;                                                // B8：明确不满足 ⇒ 这一行不跑
                }
                if (gate == null)
                    NoteCardAbilityRowGateUnknown(card, trigger, row, gateWhy);   // B8：判不了 ⇒ **放行 + 出声**
                if (ca == 380) { fired += DispatchCardAbility380(ctx, card, inst, row, owner, actingCard, targetCard); continue; }
                if (ca == 320) { fired += DispatchCardAbility320(ctx, card, row, owner, slot, targetCard, tacticSide); continue; }
                NoteCardAbilityRowPending(card, trigger, row);               // 325 / 360 / 480：出声不跑
            }
            return fired;
        }

        /// <summary>`380 setReferenceCard` 那一支（判据 = `AbilityLogic__PlayAbility.c:2128-2160`）。
        /// <para>三档目标集，与 `RunSetReferenceCardRow`（主动能力那条路）**同一套判据**：
        /// `ta ∈ {30,220}` ⇒ 调用方点定的那一个；`ta ∈ {140,150}` ⇒ **上一条能力的目标表**；
        /// 其余 ⇒ 出声 + 不结算（⛔ 不猜，猜了会让后续 200/201 目标全错）。</para>
        /// <para>⚠️ `ta = 140/150` 这一跳会覆写 `ctx.LastTargets` / `ctx.LastTarget`（`ResolvePreviousAbilityTargets`
        /// 尾部那两行），与 `SetReferenceCards` **同一处置**：先存后还原（⛔ 让一条「设立引用表」的 ability
        /// 把后续正文的代词目标悄悄改掉）。</para></summary>
        static int DispatchCardAbility380(BattleContext ctx, CardDef card, CardInstance inst,
                                          CardDef.TriggerAbilityIndexSpec row, int owner,
                                          UnitState actingCard, UnitState targetCard)
        {
            var holder = inst != null ? inst : (actingCard != null ? actingCard.Instance : null);
            if (holder == null)
            {
                ctx.Log($"（`ca 380 setReferenceCard`：「{card.Name}」**没有写表载体**（`inst` / `actingCard.Instance` 都为空）"
                      + " ⇒ 引用表无处可写，本行空过 —— 原版 `PlayAbility.c:2139` 写的是 `actingCard + 0x278`）");
                return 0;
            }
            int ta = TargetsAffectedOfRow(card, row);
            if (ta == 30 || ta == 220)
            {
                if (targetCard == null)
                {
                    ctx.Log($"（`ca 380 setReferenceCard`：「{card.Name}」本行 `ta={ta}`（要调用方点定的那一个），"
                          + "而这次**没有点选目标** ⇒ 本行不结算（原版 `AbilityLogic__GetTargets.c:192-207` 只有 30/220 读 `param_5`））");
                    return 0;
                }
                holder.ReferencedCards.Clear();
                holder.ReferencedCards.Add(CardRef.Of(targetCard));
                ctx.Log($"（`ca 380 setReferenceCard`：「{card.Name}」的引用表被**覆盖**成 1 张（{targetCard.Name}）"
                      + " —— 目标集 = 调用方点定的那一个（`ta=30 target`，判据 `AbilityLogic__GetTargets.c:192-207`"
                      + " + `AbilityLogic__PlayAbility.c:2128-2160`））");
                return 1;
            }
            if (ta == 140 || ta == 150)
            {
                var saved = new List<UnitState>(ctx.LastTargets);
                var savedLast = ctx.LastTarget;
                var refs = new List<CardRef>();
                try
                {
                    ResolveTargets(ctx, owner, null, actingCard, targetCard, null,
                                   new EffectOp { SubjectTargetsAffected = ta }, holder, refs);
                }
                finally
                {
                    ctx.LastTargets.Clear(); ctx.LastTargets.AddRange(saved); ctx.LastTarget = savedLast;
                }
                if (refs.Count == 0)
                {
                    ctx.Log($"（`ca 380 setReferenceCard`：「{card.Name}」本行 `ta={ta}`（上一条能力的目标表）**为空** ⇒"
                          + " 原版 `AbilityLogic__PlayAbility.c:2131-2132` 的 `if (0 < count)` 不成立 ⇒ **不动引用表**）");
                    return 0;
                }
                holder.ReferencedCards.Clear();
                holder.ReferencedCards.AddRange(refs);
                ctx.Log($"（`ca 380 setReferenceCard`：「{card.Name}」的引用表被**覆盖**成 {refs.Count} 张（{NamesOfCardRefs(refs)}）"
                      + " —— 目标集 = 上一条能力的目标表（`ta=" + ta + "`，判据 `AbilityLogic__GetTargets.c:867-883`"
                      + " + `AbilityLogic__PlayAbility.c:2144-2153`））");
                return 1;
            }
            ctx.Log($"⚠️「{card.Name}」的 `ca 380 setReferenceCard` **解不出目标集**（`ta={ta}`，读不到该行 `targetsAffected`）"
                  + " ⇒ 本版**不猜**，这一行不结算。（原版这一跳填的是这条 logic 自己的目标表；"
                  + "目标集判据 = `AbilityLogic__GetTargets.c:192-207`，只有 `ta = 30/220` 读调用方给的那个目标）");
            return 0;
        }

        /// <summary>`320 followAfterResolution` 那一支（**只走战术/防御路**）。
        /// <para>执行体 = 现成的 `EmitFollowUpResolution`（施工面 §B·5 明写）—— 与改前
        /// `EffectResolver.cs:2917` 那段硬编码块**同一句、同一算式**；本层接管后那段让路
        /// （`AbilityLayerOwns320Row`），净「1 拍」不变。</para>
        /// <para>⚠️ `410` 的正文那一半**本批不接**：改前这一段也只发表现事件；把 410 正文一起接进来
        /// 会改结算结果（`play check` 指纹会变）⇒ 那是另一件活（判据已在 `RunFollowAfterResolutionRow`）。</para></summary>
        static int DispatchCardAbility320(BattleContext ctx, CardDef card, CardDef.TriggerAbilityIndexSpec row,
                                          int owner, int slot, UnitState targetCard, string tacticSide)
        {
            if (FollowUpSchedulerOf(card) != "ThisCardPlayed") return 0;   // 双保险（与 AbilityLayerOwnsRow B5 同一判据）
            int fuOwner = targetCard != null ? OwnerOf(ctx, targetCard) : -1;
            if (fuOwner < 0) fuOwner = targetCard != null ? (tacticSide == "enemy" ? 1 - owner : owner) : owner;
            var evt = EmitFollowUpResolution(ctx, card, fuOwner,
                                             targetCard != null ? slot : -1,
                                             targetCard != null ? targetCard.InstanceId : 0);
            return evt != null ? 1 : 0;
        }

        // ======================================================================================
        //  三、B7 双保险 / 出声口（⛔ 一律 `ctx.Log`，**不进 `ctx.Events` 之外的任何状态**）
        // ======================================================================================

        /// <summary>B7：本层真要跑这一行，但触发桶里已有 `op.AbilityIndex == row.Ab` 的 op（= 文本已覆盖该 ability）
        /// ⇒ **跳过 + 出声**（⛔ 不静默）。
        /// <para>数据面 = `CardDef.TriggerOpsByNumber(trigger)`（`CardDatabasePartial_B98H7.MapTriggerOpsByNumber` 按 `ab` 归位、按 trigger 分桶）。
        /// 现池 trigger 0 上桶非空只 14 张 ⇒ 本闸今天对 37 行**恒不命中**，是给将来数据兜底。</para></summary>
        static bool CardAbilityRowCoveredByTextLayer(CardDef card, int trigger,
                                                     CardDef.TriggerAbilityIndexSpec row, out string why)
        {
            why = null;
            if (card == null || row == null) return false;
            var ops = card.TriggerOpsByNumber(trigger);
            if (ops == null || ops.Count == 0) return false;
            for (int i = 0; i < ops.Count; i++)
            {
                var op = ops[i];
                if (op != null && op.AbilityIndex == row.Ab)
                { why = $"正文 op 已覆盖 ability#{row.Ab}（`TriggerOpsByNumber({trigger})`）"; return true; }
            }
            return false;
        }

        static readonly HashSet<string> _cardAbilityRowNoted = new HashSet<string>(System.StringComparer.Ordinal);

        static void NoteCardAbilityRowSkippedByTextCoverage(CardDef card, int trigger,
                                                            CardDef.TriggerAbilityIndexSpec row, string why)
        {
            string key = "cover|" + CardKeyOf(card) + "|" + trigger + "|" + row.Ab + "|" + row.Lg;
            if (!_cardAbilityRowNoted.Add(key)) return;
            UnityEngine.Debug.LogWarning($"[RuleEngine] 「{(card != null ? card.Name : "?")}」的 trigger {trigger}"
                + $"（ab{row.Ab}/lg{row.Lg}/ca{row.Ca}）：B7 双保险命中 —— {why} ⇒ **本层跳过**（⛔ 不双发）。");
        }

        static void NoteCardAbilityRowGateRejected(CardDef card, int trigger,
                                                   CardDef.TriggerAbilityIndexSpec row, string why)
        {
            string key = "gate|" + CardKeyOf(card) + "|" + trigger + "|" + row.Ab + "|" + row.Lg;
            if (!_cardAbilityRowNoted.Add(key)) return;
            UnityEngine.Debug.LogWarning($"[RuleEngine] 「{(card != null ? card.Name : "?")}」的 trigger {trigger}"
                + $"（ab{row.Ab}/lg{row.Lg}/ca{row.Ca}）：`CanTriggerAbility` 的 criteria **明确不满足**"
                + (string.IsNullOrEmpty(why) ? "" : "（" + why + "）") + " ⇒ 这一行不跑（判据 `CardAbility__CanTriggerAbility.c:131-140`）。");
        }

        static void NoteCardAbilityRowGateUnknown(CardDef card, int trigger,
                                                  CardDef.TriggerAbilityIndexSpec row, string why)
        {
            string key = "gateq|" + CardKeyOf(card) + "|" + trigger + "|" + row.Ab + "|" + row.Lg;
            if (!_cardAbilityRowNoted.Add(key)) return;
            UnityEngine.Debug.LogWarning($"[RuleEngine] 「{(card != null ? card.Name : "?")}」的 trigger {trigger}"
                + $"（ab{row.Ab}/lg{row.Lg}/ca{row.Ca}）：criteria **判不了**"
                + (string.IsNullOrEmpty(why) ? "" : "（" + why + "）") + " ⇒ **放行 + 出声**（⛔ 不把「判不了」当「不满足」，"
                + "与 `CostWhenAbilityCriteriaAllows` 同一口径；终态要改成卡级三件套，见 `B116_R9` §①）。");
        }

        static void NoteCardAbilityRowPending(CardDef card, int trigger, CardDef.TriggerAbilityIndexSpec row)
        {
            string key = "pending|" + CardKeyOf(card) + "|" + trigger + "|" + row.Ab + "|" + row.Lg;
            if (!_cardAbilityRowNoted.Add(key)) return;
            UnityEngine.Debug.LogWarning($"[RuleEngine] 「{(card != null ? card.Name : "?")}」的 trigger {trigger}"
                + $"（ab{row.Ab}/lg{row.Lg}/ca{row.Ca}）：这一支**本批没有执行器**（`325` 的门必须在伤害结算前判 /"
                + " `360` 的选项池 17/18 在池外 / `480` 要 `UnitState`） ⇒ **本行不结算**（⛔ 不猜，也⛔ 不与正文重复）。");
        }

        static string NamesOfCardRefs(List<CardRef> refs)
        {
            if (refs == null || refs.Count == 0) return "";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < refs.Count; i++)
            { if (i > 0) sb.Append('、'); sb.Append(refs[i].Name); }
            return sb.ToString();
        }

        // ======================================================================================
        //  四、自检门（⛔ 生产侧别调；纪律同 `FireTriggerRowsByChosenAbilityForTest`）
        // ======================================================================================

        /// <summary>自检门：<see cref="DispatchCardAbilityRows"/>（不改任何行为，只让 Editor 程序集调得到）。</summary>
        public static int DispatchCardAbilityRowsForTest(BattleContext ctx, CardDef card, CardInstance inst, int trigger,
                                                         int owner, int slot, UnitState actingCard, UnitState targetCard,
                                                         bool tacticPath = false, string tacticSide = null)
        {
            return DispatchCardAbilityRows(ctx, card, inst, trigger, owner, slot, actingCard, targetCard, tacticPath, tacticSide);
        }
    }
}
