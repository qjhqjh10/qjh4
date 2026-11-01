// ChoiceOptionCards_B116W32.cs — 执行写手 `B116_W32`（2026-11-02 · 活状态表 `B-329`）
//   **选项 SO 旁表的读取口 + 结构化执行器**（`ENG-18` 的结构化支从「休眠」变「可达」）
//
// ── 判据（现读 `d:/2/tools/decomp_full/`，⛔ 不拿账里的转述当判据）────────────────────
//   · 候选池 = 父卡的 `TargetCriteria.filterSpecificCards`（**PPtr 直接点名**，
//     `BattleManager__GetChoiceFullPool.c:36-55`；`List<RawCardScript>` → `dump.cs:23870`）
//     ⇒ **选项 SO 不在可玩池里也能被取到**（原版根本不查 collection）。
//   · 选中之后跑的是**那张选项 SO 自己的 `ThisCardPlayed(0)` ability**：
//     `AbilityLogic__PlayAbility.c:2032-2040`（`chosenAbility 360 playChosenCardAbility` 支）
//     → `RawCardScript.OnTriggerAlt(0 /* ThisCardPlayed */, mgr, thisCard, actingCard, targetCard, …)`，
//     其中 `thisCard = actingCard = AbilityData.thisCard(+0x10)` = **父卡所在的那个单位**
//     （所以选项 SO 的 `ca 40 buffSelf` 落在**父卡自己**身上，不是「己方全体」）。
//   · `ca 40 buffSelf` 的载荷 = 该行 `buffEffect`：`AbilityLogic__PlayAbility.c:1310-1380`
//     （`:1369 AddEffect`，**硬写 thisCard**、不读 `ta`）。执行器 = 已有的
//     `RunAbilityBuffRowOfEnchantment`（`Core/EnchantmentExecutor_B115.cs:299`）——
//     ⛔ 这里**不另写一份加成逻辑**（铁律 6）。
//
// ── 为什么「全检后才跑」而不是逐行能跑就跑 ────────────────────────────────────────
//   本类只在**选项卡 SO 的正文 op 为空**时被调用（`TryRunStructuredOptionOrText` 的回落前一步）。
//   如果只跑得动其中一部分行、剩下让给文本支 ⇒ 同一张卡的同一个效果会**部分双发**。
//   ⇒ 闸：**任何一行没有执行器 ⇒ 整张让路**（一行都不跑，回落卡面文本 = 改前行为）。
//   现读 57 张选项 SO 里可执行的只有 TL20 那 3 张（`ca 40 buffSelf`，buffType 1）；
//   其余（`ca 60 buffMana` / `70 summonUnit` / `0 drawCard` / `20 doHeal` / `100` / `30`）没有
//   本版的非正文执行器 ⇒ 照旧回落文本（**零回归**，并由日志如实点名）。
//
// ── 与 W6 那条「结构化支」的关系 ─────────────────────────────────────────────────
//   `B116_W6` 已把闸/分支接好（`EffectResolver.TryRunStructuredOptionOrText`），但它只认
//   「选项卡 SO 在**可玩池**里」+「SO 有自己的**正文**（`rally` 文本 op）」。
//   现读池内 0/57 ⇒ 该支休眠；而且真卡的选项 SO **根本没有正文**（`desc` 空，
//   它们是纯结构化载体）⇒ 光把 SO 塞进池也跑不动。本文件补的就是这一格：
//   池里没有 ⇒ 查旁表；旁表里有、正文为空 ⇒ 跑**结构化行**。
//
// ── 灭自证（两对，结构上不可能同时绿）────────────────────────────────────────────
//   · 同一份 op / 同一个夹具，只是**旁表有没有那张 SO**：
//     A3（有）⇒ TL20 自己 +2 melee、其余友军**不变**；A4（空表）⇒ 回落文本「己方全体 +2」。
//   · 同一张 SO，只是**行里有没有执行器**：把 `ca 40` 改成 `ca 60` ⇒ A3 红（不再结构化）而 A4 不变。
namespace RuleEngine
{
    // ==========================================================================================
    //  一、`BattleContext` 的第三张表注入格（照 `EnchantmentTemplates` 那一套）
    // ==========================================================================================
    public partial class BattleContext
    {
        /// <summary>第三张表（选项 SO）的**加载器钩子**（由 `Data/CardDatabase` 的静态字段初始化器登记；
        /// `null` = 没人注册 ⇒ 判不了）。⚠️ 进程级静态；列表由每个 ctx 各自持有一份引用。</summary>
        public static System.Func<System.Collections.Generic.List<CardDef>> ChoiceOptionCardsProvider;

        /// <summary>本局可查的**选项 SO**（第三张表）。`null` = 还没物化（懒加载）或没人注册。</summary>
        public System.Collections.Generic.List<CardDef> ChoiceOptionCards;

        /// <summary>把选项 SO **物化一次**（幂等）。没人注册 ⇒ 保持 `null`（= 判不了，⛔ 不造假空表）。</summary>
        public void EnsureChoiceOptionCards()
        {
            if (ChoiceOptionCards != null) return;
            var p = ChoiceOptionCardsProvider;
            if (p == null) return;
            ChoiceOptionCards = p() ?? new System.Collections.Generic.List<CardDef>();
        }

        /// <summary>按 id 查选项 SO：**先 <see cref="CardPool"/>（可玩池，兼容「SO 也在池里」那种表）
        /// → 再第三张表**。
        /// <para>`null` = 两张表都没有它（调用方必须如实出声，⛔ 别当「这条链不适用」）。</para></summary>
        public CardDef FindChoiceOptionCard(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var pool = CardPool;
            if (pool != null)
                for (int i = 0; i < pool.Count; i++)
                    if (pool[i] != null && string.Equals(pool[i].Id, id, System.StringComparison.Ordinal)) return pool[i];
            EnsureChoiceOptionCards();
            var tbl = ChoiceOptionCards;
            if (tbl != null)
                for (int i = 0; i < tbl.Count; i++)
                    if (tbl[i] != null && string.Equals(tbl[i].Id, id, System.StringComparison.Ordinal)) return tbl[i];
            return null;
        }
    }

    // ==========================================================================================
    //  二、结构化执行器（`ThisCardPlayed(0)` 的 `ca` 行）
    // ==========================================================================================
    public static partial class RuleCore
    {
        /// <summary>「这一张选项 SO 的结构化支**让路**了」的出声口。
        /// <para>⛔ **走 `UnityEngine.Debug.LogWarning`、不进 `ctx.Log`** —— 后者会改 `ctx.Events.Count`
        /// 进而改 `NetProtocol.Fingerprint`（与 `NoteTriggerRowKindGap` / `NoteEnchantRowUnsupported` 同一条纪律）。
        /// 同一张选项 SO 只喊一次。</para></summary>
        static readonly System.Collections.Generic.HashSet<string> _choiceOptionLetGoNoted
            = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        static void NoteChoiceOptionLetGo(CardDef optionCard, string why)
        {
            string key = (optionCard != null ? (string.IsNullOrEmpty(optionCard.Id) ? optionCard.Name : optionCard.Id) : "?") + "|" + why;
            if (!_choiceOptionLetGoNoted.Add(key)) return;
            UnityEngine.Debug.LogWarning($"[RuleEngine] 选项 SO「{(optionCard != null ? optionCard.Name : "?")}」的结构化支**让路**："
                                       + why + " ⇒ 按卡面文本结算这一项（⛔ 不猜，也⛔ 不与文本支双发）。");
        }

        /// <summary>这个 `ca` 本版有没有**非正文**执行器（唯一判据；⛔ 别在别处再抄一份）。
        /// <para>`30 buffOthers` / `40 buffSelf` / `50 buffHand` 走 `RunAbilityBuffRowOfEnchantment`
        /// （判据 → 该方法的头注）。其余一律「没有」。</para></summary>
        public static bool HasChoiceOptionRowExecutor(int ca)
        {
            return ca == 30 || ca == 40 || ca == 50;
        }

        /// <summary>把<b>选项 SO</b> 的 `ThisCardPlayed(0)` 能力按 `chosenAbility` 行结算。
        ///
        /// <para>🔴 **只在卡面正文 op 为空时调用**（调用方 = `TryRunStructuredOptionOrText`，
        /// 那正是原版「选项 SO 没有正文、只有结构化行」的形状）。</para>
        /// <para>闸：**任何一行没有执行器 ⇒ 整张让路**（`return false`，一条都不跑）。
        /// 理由见文件头「为什么全检后才跑」——半跑 + 文本回落 = 部分双发。</para>
        /// </summary>
        /// <param name="carrier">原版 `AbilityData.thisCard(+0x10)` = **父卡所在的那个单位**
        /// （`ca 40 buffSelf` 硬写它）。传 `null` ⇒ 无法落点 ⇒ 整张让路。</param>
        /// <returns>真结算了 ≥1 行。</returns>
        internal static bool TryRunChoiceOptionRows(BattleContext ctx, int owner, CardDef optionCard,
                                                    UnitState carrier, UnitState actingCard, string by)
        {
            if (ctx == null || optionCard == null || carrier == null) return false;

            // 🔴 **两道闸（缺一就会把别的机制吃进来）**
            //   ① **父卡判得出来**：判不出 ⇒ 让路（⛔ 不猜）。取法与仓内既有口径逐字一致
            //      （`EffectResolver.cs:7130` / `:19324`：先 `ActingUnit` 再回落 `PlayingCard`）。
            //   ② **父卡不是「平 / 誓」对**：`choose one:` 的选项表是 N 项任意；而**带 `Oath N`**
            //      的那一族（现读 10 张：UM109–UM118 + `Angel's Wrath` / `Catechism of Death`）
            //      的 `filterSpecificCards` 恰好是 **[平, 誓] 两张**（消费方按 `ids[0]`=平 / `ids[1]`=誓，
            //      见 `CardDef.OathChoiceNeeded` 与 `EffectResolverPartial_B111E1B.cs:42`）。
            //      🔴 **实测**：若放这一族进结构化支，它会只跑选中那一项的 `buffEffect`
            //      ⇒ **誓约加值与誓约付费一起丢**（UM111 的 `+8/+8` 没了、能量 −10 变 −2）。
            //      ⇒ 这一类**整张让路**，回到它原来那条文本 + 誓约链（⛔ 不改那条链）。
            var parentCard = (ctx.ActingUnit != null ? ctx.ActingUnit.Card : null) ?? ctx.PlayingCard;
            if (parentCard == null) { NoteChoiceOptionLetGo(optionCard, "判不出父卡（`ActingUnit` / `PlayingCard` 都为空）"); return false; }
            if (parentCard.OathCost > 0)
            { NoteChoiceOptionLetGo(optionCard, $"父卡「{parentCard.Name}」带 `Oath {parentCard.OathCost}` ⇒ 这张表是**平/誓对**，不是 `choose one:` 表"); return false; }

            const int ThisCardPlayed = 0;                          // `AbilityTrigger.ThisCardPlayed`
            var rows = optionCard.TriggerAbilityRowsOf(ThisCardPlayed);
            if (rows == null || rows.Length == 0) return false;    // 列未知 / 明确没有 ⇒ 让路
            bool any = false;
            for (int i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                if (r == null) continue;
                if (r.Lg < 0) continue;                            // 空 logic 行 = 原版空循环，跳过不算缺口
                if (!HasChoiceOptionRowExecutor(r.Ca)) return false;   // 有一行跑不动 ⇒ 整张让路
                any = true;
            }
            if (!any) return false;
            int fired = 0;
            string who = string.IsNullOrEmpty(by) ? "选项卡 SO" : by;
            for (int i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                if (r == null || r.Lg < 0) continue;
                fired += RunAbilityBuffRowOfEnchantment(ctx, owner, carrier, optionCard, r, actingCard,
                                                        who + "·选项卡 SO");
            }
            if (fired > 0)
                ctx.Log($"{who}：「{optionCard.Name}」的 `ThisCardPlayed` 按**结构化行**结算 {fired} 条"
                      + $"（`ca {(rows[0] != null ? rows[0].Ca : -1)}`；原版 `AbilityLogic__PlayAbility.c:1310-1380` 硬写 thisCard"
                      + " ⇒ 落在**父卡自己**身上，⛔ 不是「己方全体」）");
            return fired > 0;
        }
    }
}
