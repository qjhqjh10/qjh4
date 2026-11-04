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
                NoteCardAbilityRowPending(card, trigger, row);               // 325 / 360 / 480：出声不跑。🔴 2026-11-02（`B117-WSMALL` · 判据 `判据/B117_逐卡核验_C_DarkAngels_Goff.md` §五·5）：**`ca 360`（`chooseOne`）这一支的效果已由正文层 `Choose one:` → `chooseone` op 承担**（`EffectText.cs:3958/5948` → 动词表 `chooseone` 1132；判据点名的例子 = `DA46 The Rock`，现读池内 `ca 360` 共 18 张卡）⇒ 现在**不是缺口**；⛔ **将来谁给 360 补卡级执行器，就会与正文双发** —— 补之前必须先让这些卡的正文 `chooseone` op 让路或去重
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

        // ======================================================================================
        //  五、手牌上下文（`abilityContext == 10 Hand`）的行级 `ca` 派发  —— ✅ 已复刻（2026-11-04 · B141_W67 ·
        //      判据 `decomp_full/AbilityLogic__PlayAbility.c:588-603`（`ca 160` 那一支逐目标 `AddDiscardFromHand`）
        //      ＋ `AbilityLogic__GetTargets.c:184-192`（`ta = 20 self` ⇒ 目标 = 能力所在那张卡自己））
        //
        //  为什么要有这一段：`cardAbilities` 行 + **Hand 上下文**那条路（`EffectResolver.FireHandAbilityTrigger`）
        //  改前**只取文本 op**（`TriggerCarrier.FxOps`），**不按 `chosenAbility` 数字分派** ⇒ 数据里真出现
        //  「手牌里靠 `ca 160` 弃自己」这种行（教程起手占位卡 `T1 BlackLegion-Abaddons Chosen_startinghand`，
        //  见 `资料/完全复刻/报告/B141_W128_BL37Discard.md`）时，我们**什么都不做、也不出声**。
        //
        //  🔴 **与主路同一张判据，⛔ 不是第二份规则**：
        //    · 「`ca → 哪一支`」的**唯一**分类口 = <see cref="HandAbilityRowKindOf"/>（本段）；
        //    · `AbilityLayerOwnsRow`（B1..B6）**不适用于**这一路 —— 它是「`trigger 0` 出牌路：ability 层 vs 正文层」
        //      的判法，B6 说「真效果种类 ⇒ 卡面正文负责」；而在**手牌**里没有那段正文可跑（正文 op 为空才进来）。
        //    · 「正文已覆盖这一行吗」仍然复用**同一条** B7 判据 <see cref="CardAbilityRowCoveredByTextLayer"/>。
        //    · 执行体复用现成的：`DiscardHandCardCore`（= `DoDiscardFromHand` 的结算核心，含 `RemoveCostEffects`），
        //      ⛔ 本段**不另写**搬运/弃牌逻辑。
        //
        //  ⚠️ **只实现 `ta = 20 self`**（今天数据里唯一的一档：占位卡 `targetCriteria.targetsAffected = 20`）。
        //     其余 `ta` 要在**手牌里**按 criteria 筛（`GetTargets`），我们这一层手牌不是 `UnitState`、
        //     没有实例级 criteria 收口口 ⇒ **出声 + 不结算**（⛔ 不猜；猜了 = 弃错牌，静默）。
        //     其它 `ca` 档（`100 createCardInHand` / `70 summonUnit` / `30 buffOthers` …）的主路执行器**都挂在
        //     `UnitState` 上**，手牌上下文没有那个对象 ⇒ 同样出声 + 不结算（= 真缺口，⛔ 不静默）。
        //
        //  🧨 灭自证：删掉 `FireHandAbilityTrigger` 里那两行 `rowLane` / `DispatchHandAbilityRows` 的接线
        //     ⇒ `B141_W67_Assert` 的 A2 段「占位卡的手牌 `ca 160` 行被派出去（手牌少 1 张、弃牌堆 +1）」变红；
        //     把 `ta` 那一格忽略（一律当 self）⇒ A3（`ta = 30` 的负例）红；把 `ca < 0` 折成 `0`
        //     ⇒ A4（未知行不出声、不派）红。
        // ======================================================================================

        /// <summary>手牌上下文里，一行 `(ab, lg)` 归谁派（**唯一判法**，⛔ 别在调用点复制）。</summary>
        public enum HandRowKind
        {
            /// <summary>`ca &lt; 0`：未知（旧表 &lt; v47 / `lg == -1` / 手写夹具）⇒ **改前行为**（什么都不做、不出声）。</summary>
            Unknown = 0,
            /// <summary>本层有执行器、会**真结算**（今天 = `ca 160 discardFromHand` ∧ `ta = 20 self`）。</summary>
            Owned = 1,
            /// <summary>原版这一刻会执行，但**手牌上下文没有执行器** ⇒ 出声、不结算（真缺口，⛔ 不静默）。</summary>
            Unsupported = 2,
            /// <summary>✅ 已复刻（2026-11-05 · 批次 `B143` · 执行写手 `B143-R3`）：原版这一刻会执行，但**本架构里
            /// 那条行的效果已经由 at-turn 文本路结算**（`RuleCore.ResolveAtTurn → AtTurnHandJobDeferred → ResolveOps`）
            /// ⇒ 不是缺口、不派发、也不出声。判据见 <see cref="HandTurnTextCoversRow"/>。
            /// <para>🔴 **为什么不能改成结构化执行**：`Cult Propaganda` 的手牌 `ca 100` 已经由文本路造出 `GSC6`；
            /// 再写一个结构化执行器 ⇒ **每回合双造一张**。而 B7 闸（<see cref="CardAbilityRowCoveredByTextLayer"/>）
            /// 只查 `card.TriggerOpsByNumber(trigger)`，`CollectTriggerOps` 不收 `At the end of your turn, …`
            /// （它不是关键词前缀）⇒ **B7 抓不到这一张**。</para></summary>
            CoveredByText = 3,
        }

        /// <summary>`chosenAbility` + `targetsAffected` → 手牌上下文的四档。纯函数（自检直接调，⛔ 不需要战场）。
        /// <para>🔴 **在共用的 <see cref="TriggerRowKindOf"/> 之上再判一格**（⛔ 不是另开一张表）：
        /// `Unknown`（`ca &lt; 0`）原样透传；其余各档在手牌上下文里都缺执行器（`320`/`325`/`340`/`380`/`480`
        /// 是 meta-op 或要 `UnitState`），**例外三处**：`ca 160 ∧ ta = 20 self`（本层真跑）·
        /// `ca 100` ∧ 卡面 at-turn 正文覆盖本行（✅ `B143-R3`：文本路已结算 ⇒ `CoveredByText`）·
        /// `ca 165 discardWholeHand`（✅ `B143-R3`：按 `row.Pa` 分侧整手弃；⚠️ **休眠件 —— 全池 0 行**）。</para>
        /// <para>判据 = `AbilityLogic__PlayAbility.c:588-603`（160 逐目标 `AddDiscardFromHand`）＋
        /// `AbilityLogic__GetTargets.c:184-192`（`ta = 20 self` ⇒ 目标 = `thisCard` 自己）＋
        /// `AbilityLogic__PlayAbility.c:986-1037`（100 = 逐张 `AddNewCardToHand`）。</para></summary>
        public static HandRowKind HandAbilityRowKindOf(CardDef card, CardDef.TriggerAbilityIndexSpec row, out string why)
        {
            why = null;
            if (card == null || row == null) { why = "没有卡/行 ⇒ 判不了"; return HandRowKind.Unknown; }
            var lane = TriggerRowKindOf(row.Ca);                      // 🔴 共用的「`ca → 哪一支」判据（唯一出处）
            if (lane == TriggerRowKind.Unknown)
            { why = "`chosenAbility` 未知（-1：旧表 < v47 / `lg == -1` / 手写夹具）⇒ 照旧"; return HandRowKind.Unknown; }
            if (row.Ca == 160)
            {
                int ta = TargetsAffectedOfRow(card, row);
                if (ta == 20) { why = "`160 discardFromHand` + `ta = 20 self` ⇒ 弃**能力所在的那张手牌自己**"; return HandRowKind.Owned; }
                why = "`160 discardFromHand` 但 `targetsAffected = " + ta + "`（手牌上下文只实现 `ta = 20 self`）⇒ 不猜";
                return HandRowKind.Unsupported;
            }
            if (row.Ca == 100)                                        // ✅ 已复刻（2026-11-05 · B143_R3 · 判据 `PlayAbility.c:986-1037` + 本文件 `HandTurnTextCoversRow`）
            {
                string textWhy;
                if (HandTurnTextCoversRow(card, row, out textWhy))
                { why = textWhy; return HandRowKind.CoveredByText; }
                why = "`ca 100 createCardInHand`：手牌上下文没有结构化执行器，且这张卡的 at-turn 正文**不覆盖**本行"
                    + (string.IsNullOrEmpty(textWhy) ? "" : "（" + textWhy + "）");
                return HandRowKind.Unsupported;
            }
            if (row.Ca == 165)                                        // ✅ 已复刻（2026-11-05 · B143_R3 · 判据 `AbilityLogic__PlayAbility.c:293-345`）—— ⚠️ 休眠件：全 1,351 张 SO 里 `ca 165` **0 行**，落地后仍不会有卡走到这里
            { why = "`165 discardWholeHand` ⇒ 手牌侧执行器 `RunDiscardWholeHandRowOfHandCard`（按 `row.Pa` 分侧）"; return HandRowKind.Owned; }
            why = "`ca " + row.Ca + "`（共用判据判成 " + lane + "）在手牌上下文**没有执行器**"
                + "（主路的执行器都挂在 `UnitState` 上）";
            return HandRowKind.Unsupported;
        }

        /// <summary>这张卡的这条 trigger 在手牌上下文**有没有本层要跑的行**（`FireHandAbilityTrigger` 用它决定「要不要入队」）。
        /// <para>⛔ 与 <see cref="DispatchHandAbilityRows"/> 用**同一个** <see cref="HandAbilityRowKindOf"/> 与**同一条** B7 判据
        /// —— 两处若各写一份，「入队了但一行都不跑」会静默出现。</para>
        /// <para>⚠️ 有「原版会跑、我们没有执行器」的行时**顺手出声一次**（<see cref="NoteHandAbilityRowUnsupported"/>），
        /// 因为这一格返回 `false` ⇒ 调用方会**静默**返回 ⇒ 不出声就等于「静默失败」（本项目红线）。
        /// 返回值仍只表示「有没有 <see cref="HandRowKind.Owned"/> 行」。</para></summary>
        internal static bool HandAbilityTriggerHasRows(CardDef card, string triggerName)
        {
            if (card == null || string.IsNullOrEmpty(triggerName)) return false;
            int tn = CardDef.AbilityTriggerNumber(triggerName);
            if (tn < 0) return false;
            var rows = card.TriggerAbilityRowsOf(tn);                 // `null` = 列未知；`[]` = 明确没有
            if (rows == null || rows.Length == 0) return false;
            string why;
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (row == null) continue;
                var kind = HandAbilityRowKindOf(card, row, out why);
                if (kind == HandRowKind.Unknown) continue;
                if (kind == HandRowKind.CoveredByText) continue;                          // ✅ B143_R3：文本路已结算 ⇒ 既不入队、也不出声（⛔ 不是缺口）
                if (CardAbilityRowCoveredByTextLayer(card, tn, row, out why)) continue;   // B7：正文已覆盖 ⇒ 不是缺口
                if (kind == HandRowKind.Unsupported) { NoteHandAbilityRowUnsupported(card, triggerName, tn, row, why); continue; }
                return true;
            }
            return false;
        }

        /// <summary>**手牌上下文的行级 `ca` 派发口**（唯一调用点 = `EffectResolver.HandAbilityDeferred` 的
        /// 「文本 op 为空」那一支）。返回**真结算的行数**（0 = 什么都没做 = 改前行为 + 可能已出声）。</summary>
        internal static int DispatchHandAbilityRows(BattleContext ctx, int owner, CardInstance inst,
                                                    string triggerName, string by)
        {
            if (ctx == null || inst == null || inst.Card == null || string.IsNullOrEmpty(triggerName)) return 0;
            var card = inst.Card;
            int tn = CardDef.AbilityTriggerNumber(triggerName);
            if (tn < 0) return 0;                                     // 认不出 trigger 名 ⇒ 判不了（⛔ 不猜 0）
            var rows = card.TriggerAbilityRowsOf(tn);                  // `null` = 列未知；`[]` = 明确没有（三态原样）
            if (rows == null || rows.Length == 0) return 0;
            int fired = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (row == null) continue;
                string why;
                var kind = HandAbilityRowKindOf(card, row, out why);
                if (kind == HandRowKind.Unknown) continue;                                  // 改前行为
                if (kind == HandRowKind.CoveredByText) continue;                            // ✅ B143_R3：文本路已结算 ⇒ 本层不派发、不出声（⛔ 双发）
                if (CardAbilityRowCoveredByTextLayer(card, tn, row, out why))               // B7 双保险（同一条判据）
                { NoteCardAbilityRowSkippedByTextCoverage(card, tn, row, why); continue; }
                if (kind == HandRowKind.Unsupported) { NoteHandAbilityRowUnsupported(card, triggerName, tn, row, why); continue; }
                if (row.Ca == 160) { fired += RunDiscardFromHandRowOfHandCard(ctx, owner, inst, row, by); continue; }
                if (row.Ca == 165) { fired += RunDiscardWholeHandRowOfHandCard(ctx, owner, inst, row, by); continue; }   // ✅ 已复刻（2026-11-05 · B143_R3 · 判据 `PlayAbility.c:293-345`）—— ⚠️ 休眠件（全池 0 行）
                NoteHandAbilityRowUnsupported(card, triggerName, tn, row, "`ca " + row.Ca + "` 在手牌上下文没有执行器");
            }
            return fired;
        }

        /// <summary>`ca 160 discardFromHand` 的**手牌上下文**执行体。
        /// <para>判据 `AbilityLogic__PlayAbility.c:588-603`：`possibleTargets` 逐个 `AddDiscardFromHand`；
        /// `ta = 20 self` 的 `possibleTargets` = `[thisCard]`（`AbilityLogic__GetTargets.c:184-192`）
        /// ⇒ 结算体 = 把**这一张手牌**丢掉（`DiscardHandCardCore`，与 `DoDiscardFromHand` 同一个核心）。</para></summary>
        static int RunDiscardFromHandRowOfHandCard(BattleContext ctx, int owner, CardInstance inst,
                                                   CardDef.TriggerAbilityIndexSpec row, string by)
        {
            if (ctx == null || inst == null || inst.Card == null) return 0;
            var card = inst.Card;
            string who = by ?? "手牌能力";
            int ta = TargetsAffectedOfRow(card, row);
            if (ta != 20)
            {
                ctx.Log($"（手牌 `ca 160 discardFromHand`：「{card.Name}」本行 `ta={ta}` —— 手牌上下文只实现 "
                      + "`ta = 20 self`（能力所在那张牌自己，`AbilityLogic__GetTargets.c:184-192`）"
                      + "；其余档要在手牌里按 criteria 筛，本版**不猜** ⇒ 本行不结算）");
                return 0;
            }
            var unresolved = new List<string>();
            string source = card.Name ?? "?";
            bool ok = DiscardHandCardCore(ctx, owner, inst, who, source, unresolved);
            if (unresolved.Count > 0)
                ctx.Log($"「{source}」的手牌 `chosenAbility 160`（discardFromHand）：{string.Join("；", unresolved)}");
            ctx.Log($"{who}：「{source}」的手牌 `ca 160`（discardFromHand）⇒ "
                  + (ok ? "**弃掉了它自己这张手牌**" : "**没有弃成**（见上一条日志）")
                  + "（原版 `AbilityLogic__PlayAbility.c:588-603` 逐目标 `AddDiscardFromHand`；"
                  + "结算体 = `DiscardHandCardCore`，含 `RemoveCostEffects`）");
            return ok ? 1 : 0;
        }

        /// <summary>✅ 已复刻（2026-11-05 · 批次 `B143` · 执行写手 `B143-R3` · 账 `B141-HAND-CA165`）：
        /// **`chosenAbility 165 discardWholeHand` 的手牌上下文执行体**。
        ///
        /// <para>**判据**（`decomp_full/AbilityLogic__PlayAbility.c:293-345`，`iVar8 == 0xa5`）：读
        /// `AbilityLogic.playersAffected(+0x20)` ⇒ `IsPlayerAffected(thisCard.isPlayer, pa)` /
        /// `IsPlayerAffected(!thisCard.isPlayer, pa)` ⇒
        /// `GetCardsInHandList(inclMinions=1, inclSpells=1, inclWhispers=0, inclWeapons=1, inclPlayer, inclEnemy)`
        /// ⇒ **逐张** `AddDiscardFromHand(card, priority, blockPlayerActions)`。我们的等价物 = 同一套
        /// `DiscardHandCardCore`（含 `RemoveCostEffects`；与 `_ResolveDiscardFromHand` 同一个核心）。</para>
        ///
        /// <para>**为什么不用现成的 `DoDiscardWholeHand`**：它内部用 `PlayersAffectedOf(ctx, op, by)` 读
        /// `ActiveAbilityLogicGroup(ActiveAbilityObject)` —— 那是 **`activeAbility`** 那一组，而 `ca 165` 落在
        /// **`cardAbilities`** 行上 ⇒ 手牌上下文恒读成 `-1`（判据 → `判据/B143_手牌施放者模型.md` §6.2）。
        /// 本方法直接用行上的 `row.Pa`（v50 数据列），⛔ 不动 `PlayersAffectedOf`（那是一条独立账）。
        /// 结算体仍复用 `DiscardHandCardCore` —— **不写第二份弃牌逻辑**。</para>
        ///
        /// <para>⚠️ **不可达（写死）**：全 1,351 张卡 SO 递归扫 `chosenAbility == 165` = **0 行**（含
        /// `full_raw`）⇒ 落地后仍**不会有任何卡**走到这里。这是铁律 11 的「休眠机制照做」，⛔ 别当成活件。</para>
        /// </summary>
        static int RunDiscardWholeHandRowOfHandCard(BattleContext ctx, int owner, CardInstance inst,
                                                    CardDef.TriggerAbilityIndexSpec row, string by)
        {
            if (ctx == null || inst == null || inst.Card == null || row == null) return 0;
            string who = by ?? "手牌能力";
            int pa = row.Pa;
            if (pa < 0)
            {
                ctx.Log($"{who}：「{inst.Card.Name}」的手牌 `ca 165`（discardWholeHand）—— `playersAffected` **读不到**"
                      + "（`TriggerAbilityIndexSpec.Pa` = -1：旧表 < v50 / 缺键）⇒ 判不出弃哪一侧，本行**不结算**");
                return 0;
            }
            string paName = PaName(pa);
            bool mine = IsPlayerAffected(true, paName);        // 原版 `IsPlayerAffected(thisCard.isPlayer, pa)`
            bool theirs = IsPlayerAffected(false, paName);     // 原版 `IsPlayerAffected(!thisCard.isPlayer, pa)`
            var op = SyntheticRowOp(null, "discardwholehand", row.Lg);   // 复用唯一构造口；`u == null` ⇒ Source 回落 verb
            op.Source = inst.Card.Name ?? "?";
            var unresolved = new List<string>();
            int total = 0, done = 0;
            for (int s = 0; s < 2; s++)
            {
                bool affected = s == owner ? mine : theirs;
                if (!affected) continue;
                var ps = ctx.Players[s];
                if (ps == null || ps.Hand == null) continue;
                var snap = new List<CardInstance>(ps.Hand);    // 先快照再改（`Hand.Remove` 不能边遍历边改）
                for (int i = 0; i < snap.Count; i++)
                {
                    var c = snap[i];
                    if (c == null || c.Card == null) continue;
                    if (c.Card.CardType == CardDef.CardTypeWhispers) continue;   // `inclWhispers = 0`
                    total++;
                    if (DiscardHandCardCore(ctx, s, c, who, op.Source, unresolved)) done++;
                }
            }
            if (unresolved.Count > 0)
                ctx.Log($"「{inst.Card.Name}」的手牌 `chosenAbility 165`：{string.Join("；", unresolved)}");
            ctx.Log($"{who}：「{inst.Card.Name}」的手牌 `ca 165`（discardWholeHand）⇒ "
                  + (total == 0 ? "受影响那一侧**没有可弃的牌**（空过）"
                                : $"候选 {total} 张、真弃掉 {done} 张（`playersAffected = {pa} / {paName}`）")
                  + "（原版 `AbilityLogic__PlayAbility.c:293-345` 逐张 `AddDiscardFromHand`；结算体 = `DiscardHandCardCore`）");
            return done > 0 ? 1 : 0;
        }

        /// <summary>「同一 (卡, trigger, ca) 只喊一次」的闩（⛔ 不进 `ctx.Log` —— 那是诊断，
        /// 会改 `ctx.Events.Count` ⇒ 动联机指纹，纪律同 `NoteCardAbilityRowPending`）。</summary>
        static readonly HashSet<string> _handRowKindNoted = new HashSet<string>(System.StringComparer.Ordinal);

        /// <summary>手牌上下文「原版会跑、我们没有执行器」的**出声尝试次数**（只给自检；⛔ 生产侧别读）。
        /// <para>🔴 为什么需要它（`B143_R1` 复核指出）：只断 <see cref="HandRowKind"/> 的**枚举值**不足以证明
        /// 「这一档不出声」—— 删掉 <see cref="DispatchHandAbilityRows"/> 里 `CoveredByText` 那个 `continue` 时，
        /// 枚举值不变、手牌也不变，只会**静默**落到 <see cref="NoteHandAbilityRowUnsupported"/>。
        /// 有了本计数，断言就能钉住「`CoveredByText` 一声不吭」。</para>
        /// <para>计在去重闩**之前**（每次「想出声」都算）⇒ 同一张卡重复派发也数得出来。</para></summary>
        public static int HandRowKindNotesForTest { get; private set; }

        /// <summary>自检复位（⛔ 生产侧别调；只清计数，⛔ 不动去重闩）。</summary>
        public static void ResetHandRowKindNotesForTest() { HandRowKindNotesForTest = 0; }

        /// <summary>手牌上下文「原版这一刻会跑、我们没有执行器」的出声口。</summary>
        static void NoteHandAbilityRowUnsupported(CardDef card, string triggerName, int triggerNumber,
                                                  CardDef.TriggerAbilityIndexSpec row, string why)
        {
            HandRowKindNotesForTest++;                 // 计在去重闩之前（见属性 doc）
            string key = "hand|" + CardKeyOf(card) + "|" + triggerNumber + "|" + (row != null ? row.Ab : -1) + "|"
                       + (row != null ? row.Lg : -1) + "|" + (row != null ? row.Ca : -1);
            if (!_handRowKindNoted.Add(key)) return;
            UnityEngine.Debug.LogWarning(
                $"[RuleEngine] 手牌「{(card != null ? card.Name : "?")}」的 `{triggerName}`（trigger {triggerNumber}"
              + $" · ab{(row != null ? row.Ab : -1)}/lg{(row != null ? row.Lg : -1)}）有一行 `chosenAbility "
              + $"{(row != null ? row.Ca : -1)}`"
              + (string.IsNullOrEmpty(why) ? "" : "（" + why + "）")
              + " ⇒ 原版这一刻会执行，而**手牌上下文没有它的执行器** ⇒ **本行不结算**"
              + "（⛔ 不猜，也⛔ 不与正文重复；判据 = `AbilityLogic__PlayAbility.c:286` 起那个按 `chosenAbility` 分派的巨型 switch）。");
        }

        /// <summary>✅ 已复刻（2026-11-05 · 批次 `B143` · 执行写手 `B143-R3`）：**`ca 100` 这一行是不是已经由
        /// at-turn 文本路结算**（`RuleCore.ResolveAtTurn → AtTurnHandJobDeferred → ResolveOps`）。
        ///
        /// <para>**为什么需要它（判据）**：原版 `AbilityLogic.PlayAbility` 的 `chosenAbility == 100` 支
        /// （`decomp_full/AbilityLogic__PlayAbility.c:986-1037`）落到 `BattleManager.AddNewCardToHand`
        /// （`BattleManager.cs:39101`）。我们这一层把**手牌卡自己的回合起止正文**交给文本路：
        /// `EffectText.TryAtTurn`（`EffectText.cs:5660-5686`，doc 里点名 `Cult Propaganda` / `Poisoned Supplies`）
        /// 把它解析成 `atturn` op，`RuleCore.ResolveAtTurn`（`EffectResolver.cs:8926-8945`）逐张
        /// `EffectText.SplitAtTurn(c.Desc)` → `Parse(body)` → `DoCreate`（`EffectResolver.cs:1140`/`:4169`）
        /// 造到对手手牌。⇒ **这张卡的 `ca 100` 效果已经在跑，只是不在结构路**。</para>
        ///
        /// <para>🔴 **为什么不能改成结构化执行**：两条路会同时结算 ⇒ `Cult Propaganda` 每回合**双造一张**
        /// `GSC6`。而 B7 闸 <see cref="CardAbilityRowCoveredByTextLayer"/> 只查
        /// `card.TriggerOpsByNumber(trigger)`，`CollectTriggerOps`（`CardDef.cs:3722`）只从「关键词前缀句」
        /// 与 `keywords` 收 —— `At the end of your turn, …` 不是关键词前缀 ⇒ **B7 抓不到这一张**。</para>
        ///
        /// <para>**判据五条**：① `EffectText.SplitAtTurn(desc)` 非空（= 整条 desc 是一句、且是
        /// `At the start|end of your turn, …` 形态 —— 与 `ResolveAtTurn` 用的**同一个**函数）；
        /// ② 它的相位映射回的 trigger 号 == `row.Trigger`（`turn_start ⇒ 30` / `turn_end ⇒ 40`）；
        /// ③ 正文 `EffectText.Parse(body, card: card)` 产出 ≥1 条 op；
        /// ④ 其中**至少一条是 `create` op**（本行是 `ca 100 createCardInHand` ⇒ 文本路必须真在造牌，
        ///    否则 `At the end of your turn, your troops take 1 damage` 这种 at-turn 正文也会被误认成覆盖）；
        /// ⑤ 同一 trigger 下**只有 1 行 `ca 100`**（文本路整段只结算**一次** ⇒ 多行时不能让整批结构行都让路）。
        /// ⚠️ **不比 `op.AbilityIndex == row.Ab`**：at-turn 正文是**整段**执行的，行号归属由
        /// `AssignAbilityTargets` 事后贴片，拿它当闸会误伤多 ability 的卡（现读池内这两张都只有 1 组）。</para></summary>
        static bool HandTurnTextCoversRow(CardDef card, CardDef.TriggerAbilityIndexSpec row, out string why)
        {
            why = null;
            if (card == null || row == null) return false;
            string desc = card.Desc;
            if (string.IsNullOrEmpty(desc)) { why = "卡面没有正文"; return false; }
            var at = EffectText.SplitAtTurn(desc);          // 与 `ResolveAtTurn` 同一个判据（⛔ 别另写一份）
            if (at == null)
            { why = "卡面没有 `At the start/end of your turn, …` 从句（`EffectText.SplitAtTurn` = null）"; return false; }
            int phaseTrigger = at[0] == "turn_start" ? 30 : (at[0] == "turn_end" ? 40 : -1);
            if (phaseTrigger != row.Trigger)
            { why = "at-turn 相位 `" + at[0] + "`（= trigger " + phaseTrigger + "）≠ 本行 trigger " + row.Trigger; return false; }
            var ops = EffectText.Parse(at[1], out _, out _, card: card);
            if (ops == null || ops.Count == 0)
            { why = "at-turn 正文 `" + at[1] + "` 解析不出可结算 op"; return false; }
            bool hasCreate = false;
            for (int i = 0; i < ops.Count; i++)
                if (ops[i] != null && ops[i].Verb == "create") { hasCreate = true; break; }
            if (!hasCreate)
            { why = "at-turn 正文 `" + at[1] + "` 里没有 `create` op（本行是 `ca 100` ⇒ 文本路没在造牌）"; return false; }
            int ca100Rows = 0;
            var sameTrigger = card.TriggerAbilityRowsOf(row.Trigger);
            if (sameTrigger != null)
                for (int i = 0; i < sameTrigger.Length; i++)
                    if (sameTrigger[i] != null && sameTrigger[i].Ca == 100) ca100Rows++;
            if (ca100Rows != 1)
            { why = "同一 trigger 下有 " + ca100Rows + " 行 `ca 100`（文本路整段只结算一次）⇒ 不认整批覆盖"; return false; }
            why = "at-turn 文本 `" + at[1] + "` 已由 `RuleCore.ResolveAtTurn → AtTurnHandJobDeferred → ResolveOps` 结算"
                + "（原版 `AbilityLogic__PlayAbility.c:986-1037` 的等价物；⛔ 别再加结构化执行器 —— 会双发）";
            return true;
        }

        /// <summary>自检门：<see cref="DispatchHandAbilityRows"/>（不改任何行为，只让 Editor 程序集调得到）。</summary>
        public static int DispatchHandAbilityRowsForTest(BattleContext ctx, int owner, CardInstance inst,
                                                         string triggerName, string by = null)
        {
            return DispatchHandAbilityRows(ctx, owner, inst, triggerName, by);
        }

        /// <summary>自检门：<see cref="HandAbilityTriggerHasRows"/>。</summary>
        public static bool HandAbilityTriggerHasRowsForTest(CardDef card, string triggerName)
        {
            return HandAbilityTriggerHasRows(card, triggerName);
        }
    }
}
