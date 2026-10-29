// EffectResolverPartial_B97W2.cs —— 第九十七会话 · 波 3 · 执行代理 `B97_W2`
//
// 为什么单独一个文件：`Core/EffectResolver.cs` 有 19,5xx 行、且被 `账/*.tsv` 按 `文件:行号` 大量落点引用
//   ⇒ **往里插成员会让下游行号整体漂移**（`check_ledger_points` 立刻报，铁律 18·b / N9 先例）。
//   本批**所有新成员**都追加在这里 —— 编译期它与 `EffectResolver.cs` 是**同一个** `partial class RuleCore`
//   （见 `EffectResolver.cs` 的类声明；全仓**没有**叫 `EffectResolver` 的类，照文件名写 `EffectResolver.` 编译不过）。
//
// 本批落的账行（`账/战斗逻辑驱动.tsv`）：
//   · `:847` `CardScript.RemoveBuffHandCostEffects` —— **手牌效果记录上 `permanentBuff` 的生产点**
//     （消费点三处早就在 `RuleCore.ExpireHandBuffs` / `RemoveHandEffectFromHand`，生产点一直空着）
//   · `:506` `BattleManager.ResolveCreateHandCard` —— **`CardCreated(210)` 的手牌两跳**
//   · `:686` `CardScript.AddEffect` —— **11 组 `buffType` 分派的引擎落点**（`ApplyAbilityBuff`）
//
// 本批**没动**的两行（如实标，别当漏做）：`:921`（`ShowCardBeingDrawn` 两跳）与 `:427`
//   （誓约重放每圈重算）**已由同会话的 `B97_ER2` 落地**（`EffectResolverPartial_B88E2.cs:785` ·
//   `EffectResolver.cs:17372` · `EffectResolver.cs:9191` · `EffectResolverPartial_B88E2.cs:1058`），
//   本代理已逐条现读复核并写进报告 §二，⛔ 不重复实现。

using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==============================================================================================
        //  账 `:847` —— 手牌效果记录上的 `permanentBuff`（原版 `CardEffect.permanentBuff // +0x74`）
        // ==============================================================================================
        //  **判据（第一权威，现读）**：
        //    · `decomp_full/CardScript__RemoveEffect.c` **第一句** = `if (permanentBuff != 0) return;`
        //      —— 带这一位的那条 `CardEffect` **摘不掉**；
        //    · `decomp_full/CardScript__RemoveBuffHandCostEffects.c:35` 是同一道闸的**最外层**
        //      （倒序扫 `activeEffects`，带这一位的整类跳过）。
        //  **我们的形状**：手牌效果 = `CardInstance.HandEffect`（数据槽 `Permanent`），
        //    消费点三处已接（`RuleCore.ExpireHandBuffs` / `RuleCore.RemoveHandEffectFromHand`）。
        //    缺的只有「谁灌这一位」——本方法就是那个生产点。
        //
        //  🔶 自己设计（非原版）：**读哪张卡的那一格** · 原版的 `permanentBuff` 长在**那一条 `CardEffect`**
        //     上（每条各带各的），而我们的手牌效果记录是从**卡面文本**解析出来的 op、**不带** `(ab,lg)`
        //     下标 ⇒ 判不到「这条 op 对应 SO 里哪一行」。`CardDef.HasPermanentHandCostBuff`
        //     （`CardDef.cs:9071`，同一裁定链）就是为这个缺口建的**卡级汇总**，所以这里读它。
        //     去哪张卡上读：① `ctx.PlayingCard`（正在打出的那张 = 产生这条效果的能力所在卡）
        //     ② 退 `ctx.ActingUnit?.Card`（持续型 / 附魔授予的来源）。③ 都取不到 ⇒ **判不了**，
        //     出声 + 按 `false` 处理（⛔ 不猜 `true`：猜 `true` 会让一条本该到期的效果永远赖在手上）。
        //     · 调度台 2026-10-28 授权「有需要决策的地方由你决策」＋ `CardDef.HasPermanentHandCostBuff`
        //       的既有裁定（同一条）。
        //
        //  ⚠️ **今天的可观测面 = 0（如实标，别当成「已验收」）**：
        //     ① 全池带 `permanentBuff` 的只有 `GSC51 Living Icon` 一张（`permanentBuffs` 列现读，
        //        `buffType == 2 changeCost` 两行，`at = enchantment`）；
        //     ② 而 GSC51 的「`Lower the cost of a random troop in hand by 1`」在**我们**这边走的是
        //        `DoLowerCost` → `ctx.CostMods`（`EffectResolver.cs` 的 `lowercost` 支），
        //        **不是** `AttachHandEffect` 那条手牌效果记录 ⇒ 它**本来就不经过**这道清扫；
        //        且那条 `CostMod` 的 `ExpireTurn` 在卡面没写时长时是 `-1`（= 永久）⇒ 已经不会被到期摘掉。
        //     ③ 池内通过 `AttachHandEffect` 挂的**费用类**手牌效果 = **0 条**（现读 `cards_engine.json`
        //        的 `desc` 里 `give … in hand` 那一族 10 张，全是属性/关键词，没有改费用）。
        //     ⇒ 本生产点今天**零行为变化**（`play check` 指纹应逐字节不变），
        //        它是「那一格以后有用」的正确落点，不是「修好了一个现有故障」。
        /// <summary>
        /// **这条手牌效果该不该带 `permanentBuff`**（原版 `CardEffect.permanentBuff // +0x74`）。
        /// <para>判据 / 为什么读卡级汇总 / 今天为什么零可观测 → 上面那一整段（⛔ 别在这里抄第二份）。</para>
        /// </summary>
        /// <param name="inst">要贴上去的那一份牌（`null` = 「只登记记录、不贴牌」那条路 ⇒ 用施放者的卡判）。</param>
        public static bool IsPermanentHandCostBuffOf(BattleContext ctx, CardInstance inst, string by)
        {
            if (ctx == null) return false;
            // ① 产生这条效果的那张卡（优先「正在打出的」= 原版 `abilityData.rawCard` 那一支）
            CardDef owner = ctx.PlayingCard;
            if (owner == null && ctx.ActingUnit != null) owner = ctx.ActingUnit.Card;
            if (owner == null) owner = inst != null ? inst.Card : null;
            if (owner == null) return false;   // 完全判不出「这是谁的效果」⇒ 不标（下面那句由调用方统一出声）
            bool? tri = owner.HasPermanentHandCostBuff;
            if (tri == null)
            {
                // 旧卡表 / 没有 `permanentBuffs` 列 ⇒ **判不了**（原版那是「这条根本没有这一位」，不是 0）
                ctx.Log($"（`permanentBuff`：卡「{owner.Name}」的 `permanentBuffs` 列缺失 ⇒ "
                      + "**判不了这条手牌效果能不能被摘**，按「可摘」处理；重跑 工具/gen_cards_engine.py）");
                return false;
            }
            return tri.Value;
        }

        // ==============================================================================================
        //  账 `:506` —— `BattleManager.ResolveCreateHandCard` 的 `CardCreated(210)` 手牌两跳
        // ==============================================================================================
        //  **判据（第一权威，全文现读）** = `decomp_full/BattleManager__BroadcastCardCreated.c:1-47`：
        //    ① 遍历 `bm.allCardsInPlay(+0x470)`，`IsInPlay()` 为真的逐个 `CardScript.ReactToCardCreated(卡, 新卡)`；
        //    ② 再取 `BattleManager.GetCardsInHandList(bm, 1,1,1,1,1,1,1, 0)` —— **双方手牌、全卡种**
        //       （实参现读是**七个 1**，施工单写的六个别当判据）—— 逐个 `ReactToCardCreated`。
        //  `CardScript__ReactToCardCreated.c` 全文 = `OnTrigger(0xd2 = **210**, mgr, this /*监听者*/,
        //    created /*→ thisCard 位*/, 0, 0)` ⇒ **`actingCard` = 被造出来的那张**、`targetCard = null`。
        //
        //  ⚠️ **本方法只做【手牌那一跳】**，棋盘那一跳如实留缺口（理由见下，⛔ 不是漏做）：
        //    我们的棋盘监听器走的是**卡面文本**（`CardDef.FireWhen(kind, …)` + `WhenEvents.Matches`），
        //    而原版这一跳走的是**数据面 `abilityTriggers` 含 210**。要给棋盘补这一跳，需要
        //    「按 trigger 数值 210 取正文并结算」那个执行口 —— 正是账 `:1288` 里记的
        //    `RuleCore.FireTriggerAt` 缺 `opsOverride` 形参那一条（`RuleCore.cs`，不属本代理）。
        //    今天的可达面：**全池 `abilityTriggers` 含 210 的 = 0 / 1,129**（`cards_engine.json` 现读）
        //    ⇒ 那一跳落不落，**零可观测**；⛔ 不为了「看起来做全了」造一条我们自己的棋盘监听路。
        //
        //  ⚠️ **卡名**：210 在 `CardDef.AbilityTriggerNumberOfName` 里的登记名 = **`CardCreatedInHand`**
        //    （`CardDef.cs` 现读）。⇒ 走 <see cref="FireHandAbilityTrigger"/> 时**用这个名字**，
        //    ⛔ 别另造一个 `WhenEventKind` 常量（`WhenEvent.cs` 不属本代理，且会造出没有消费口的死常量）。
        /// <summary>
        /// **`BroadcastCardCreated` 的手牌两跳**（双方手牌、逐卡 `CardCreatedInHand(210)`）。
        /// <para>判据 / 缺口 / 可达性 → 上面那一整段。</para>
        /// </summary>
        /// <param name="created">刚被造出来（且**真的进了手牌**）的那一份。</param>
        /// <returns>真排进队列的监听器条数。</returns>
        public static int BroadcastCardCreatedHandHops(BattleContext ctx, CardInstance created)
        {
            if (ctx == null || ctx.IsOver || created == null || created.Card == null) return 0;
            int n = 0;
            using (BeginAction(ctx))
            {
                for (int seat = 0; seat < 2; seat++)
                {
                    var ps = ctx.Players[seat];
                    if (ps == null || ps.Hand == null) continue;
                    // ⚠️ 手牌的**数量**在监听器结算时可能已经变了（它们会入队）⇒ 先拍一份
                    var snapshot = new List<CardInstance>(ps.Hand);
                    for (int i = 0; i < snapshot.Count; i++)
                    {
                        var inst = snapshot[i];
                        if (inst == null || inst.Card == null) continue;
                        // `subject` / `target` / `actingCard` 三个形参：原版这一跳只把 `created` 送进
                        //   `thisCard` 位（= 我们的 `ctx.EventCard`，由 `FireHandAbilityTrigger` 的队列体
                        //   `EventSlotsScope` 负责），`targetCard` = null；`subject`（「这件事发生在谁身上」）
                        //   在**手牌**上没有 `UnitState` ⇒ 只能 `null`（原版那一跳也没有 subject 这个概念）。
                        if (FireHandAbilityTrigger(ctx, seat, inst, "CardCreatedInHand",
                                                   keyword: null, ops: null, spec: null,
                                                   subject: null, target: null, actingCard: null,
                                                   by: "cardcreated:210")) n++;
                    }
                }
            }
            if (n > 0)
                ctx.Log($"（`CardCreated(210)` 手牌两跳：「{created.Card.Name}」被造出来 ⇒ "
                      + $"{n} 个手牌监听器入队；原版 `BattleManager__BroadcastCardCreated.c:29-44`）");
            return n;
        }

        // ==============================================================================================
        //  账 `:686` —— `CardScript.AddEffect` 的 11 组 `buffType` 分派
        // ==============================================================================================
        //  **判据（第一权威，逐 case 现读）** = `decomp_full/CardScript__AddEffect.c` 的 `switch(buffType)`
        //   （`:330-967`，跳表在 `:330`；ILSpy 侧同一段在 `out_cpp2il_dev_ilspy/CardScript.cs:17744-17900`
        //    —— ⚠️ 1..30 那一段被 ILSpy 编成了**未解析的间接跳转**，只有 Ghidra 那份 `.c` 逐 case 可读）。
        //  数据面 = 卡表 v40 `abilityBuffs` 列 → `CardDatabase.AbilityBuffsOf(card)`（旁表，零消费者至今）。
        //
        //  🔴 **为什么这张分派表今天【不接】进主结算路径（这是裁断，不是漏做）**：
        //    我们这一层的效果**全部由卡面正文解析出来**（`EffectText.Parse` → `EffectOp` → `EffectDispatch`），
        //    而原版的 `CardEffect` 来自 SO 的数据行。**两者是同一个东西的两种表示** ⇒ 对「正文已经解析出
        //    等价 op」的那些行，再把数据行结算一遍就是**做两遍**（多给一倍）。
        //    现读**全部 4 行有数据的行都已被正文路覆盖**（逐行给证据，见报告 §四）：
        //      · `buffType 95 Lying in Wait / Trail Shaper / Neurotyrant` = 「reduce its cost to 1」
        //        → `lowercost` 动词 → `DoLowerCost` 的「设为 N 费」支（`ctx.CostMods` 的 `SetTo`）；
        //      · `buffType 70 Scarab Swarm` = 「Melee is always equal to its Health」
        //        → `CardDef.MeleeEqualsHealth` + `Aura.RefreshMeleeEqualsHealth`（每动作边界重挂）；
        //      · `buffType 25 Neurotyrant` = 「create an Ephemeral copy」
        //        → `DoCreate` 的 `pool.MarkEphemeral`（`CreatePool` 从「Ephemeral copy」那句置位）。
        //    ⇒ 本表是**给「正文没覆盖到的那一类行」准备的引擎落点**，接线必须与「正文路要不要让位」
        //      同批决定（那是调度台的裁断）。⛔ 本代理不擅自接上（会立刻把 GSC39 / TAU68 / TL69 / SAU27 打成双份）。
        /// <summary>
        /// **把这张卡某 `(ab, lg)` 行上的 `CardEffect` 行按 `AddEffect` 的 case 表逐条结算**。
        ///
        /// <para>⚠️ **今天没有任何生产调用点**（见上面那段裁断）—— 本方法是引擎侧的
        /// `CardScript.AddEffect` 等价位，等「正文路让位」这件事定了再接。
        /// 自检直接调它验机制（`RuleEngineTest.CheckB97W2Gaps` 的 C 段）。</para>
        /// </summary>
        /// <param name="ab">`cardAbilities` 下标；`-1` = `activeAbility` · `-2` = `alternativeAbility`。</param>
        /// <param name="lg">`abilityLogic` 下标。</param>
        /// <param name="isHandBuff">`false` = `buffEffect`（走 `AddEffect`）· `true` = `handBuff`
        /// （**同一个 `CardEffect` 类型、但走 `PlayerHand.AddHandEffect`**，⛔ 别混）。本方法只处理 `false` 那一档。</param>
        /// <returns>真的落地的条数。</returns>
        public static int ResolveAbilityBuffRows(BattleContext ctx, int owner, UnitState target,
                                                 CardDef def, int ab, int lg, bool isHandBuff,
                                                 string by, List<string> unresolved = null)
        {
            if (ctx == null || def == null || target == null) return 0;
            var rows = CardDatabase.AbilityBuffsOf(def);
            if (rows == null)
            {
                // `null` = 未知（旧表 / 这张卡不在表里）—— ⛔ 不是「明确没有行」
                ctx.Log($"{by}：「{def.Name}」的 `abilityBuffs` 列缺失 ⇒ **判不了**有没有这一行的 `CardEffect`");
                return 0;
            }
            int n = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                var b = rows[i];
                if (b == null || b.Ab != ab || b.Lg != lg) continue;
                if (b.IsHandBuff != isHandBuff) continue;
                if (ApplyAbilityBuff(ctx, owner, target, b, by, unresolved)) n++;
            }
            return n;
        }

        /// <summary>
        /// **`CardScript.AddEffect` 的 11 组 `buffType` 分派**（账 `:686`）。
        ///
        /// <para>逐组的判据与「我们这一侧落成什么 / 哪半没建」→ 方法体里每一 `case` 的头注
        /// （⛔ 别在别处抄第二份）。三档状态：**落地** / **净等价（无动作但有判据）** / **未建（出声）**。</para>
        /// </summary>
        /// <returns>真的改了状态返回 `true`；`false` = 这一组今天没有落点（**已经出声**）或缺前置。</returns>
        public static bool ApplyAbilityBuff(BattleContext ctx, int owner, UnitState target,
                                            CardDatabase.AbilityBuffSpec b, string by,
                                            List<string> unresolved = null)
        {
            if (ctx == null || target == null || b == null) return false;
            string who = string.IsNullOrEmpty(by) ? "AddEffect" : by;
            // `AddEffect` 的四道前置闸里的两条（卡状态 + 施放者）；闸② jam / 闸④ 施放者不在场
            //   —— 与手牌那条路共用同一份判据（`AddEffectCasterGates`）。
            if (!AddEffectCasterGates(ctx, ctx.ActingUnit, target, b.Extrinsic != 0, $"效果（{who}）")) return false;

            switch (b.BuffType)
            {
                // ---- `case 5 swapStats`（`.c:695-748`）----
                // 原版：督军等价体 ⇒ `LogError` 什么都不做；否则取 `CurrentIntrinsicHealth` 与
                //   `CurrentIntrinsicMeleeAttack`（后者再加「extrinsic 且 `buffType==1`」那些记录的
                //   `healthChange(+0x60)` 之和），然后 `ChangeBaseAttack(血值)` + `ChangeMaxHealth(攻值)`。
                // 我们：`ChangeBaseAttackFidelity` / `ChangeMaxHealthFidelity` = 那两个原语的等价位
                //   （连「先摘非 extrinsic 的 addStats」都逐句一致）。
                // ⚠️ 如实标：原版那个「加 extrinsic 的 `healthChange` 和」我们**没有**等价循环
                //   （我们的 health 增益落在 `_statMods` 的 `health` 条目上、与原版那条记录不是一回事）
                //   ⇒ 只做纯交换。池内 `buffType 5` = **0 行** ⇒ 今天零可观测。
                case 5:
                {
                    if (target.IsWarlord)
                    {
                        // 原版 `.c:697-748` 的 `IsWarlordEquivalent` 支 = `LogError` 后 return
                        ctx.Log($"⚠️{who}：`swapStats(5)` 落在**督军**「{target.Name}」上 —— "
                              + "原版 `CardScript__AddEffect.c:697` 打 `LogError` 并**什么都不做**（照抄）");
                        return false;
                    }
                    int oldAttack = target.AttackBase;
                    int oldHealth = target.MaxHealthBase;
                    ChangeBaseAttackFidelity(ctx, target, oldHealth);
                    ChangeMaxHealthFidelity(ctx, target, oldAttack);
                    target.ClampHealthToMax();
                    ctx.Log($"{who}：「{target.Name}」的 `swapStats(5)`：近战 {oldAttack} ⇒ {oldHealth}、"
                          + $"生命上限 {oldHealth} ⇒ {oldAttack}（原版 `CardScript__AddEffect.c:695-748`）");
                    return true;
                }

                // ---- `case 6 changeRace` 与 `case 0x1e(30) addRace`：原版**同一支**（`.c:750-752` / `:951-953`）----
                // 两支都是：`RemoveActiveEffects(buffType)` → `CardScript.RemoveAllExtrinsicEffects(this)` → `List.Add`。
                // 我们：**没有 race 的运行期字段**（`CardDef.Subtype` 是不可变模板；`TargetCriteria` 的
                //   race 筛读的是它）⇒ 「换种族/加种族」这一整件事没有落点；
                //   而 `RemoveAllExtrinsicEffects(this)`（摘掉**这张牌身上**所有 extrinsic 效果）
                //   我们**也没有**等价物 —— 现有的两个口都不是它：
                //     · `RuleCore.RemoveExtrinsicEffectsFrom(caster)` = **全棋盘**按施放者摘 + 入队（超集）；
                //     · `Aura.RemoveExtrinsicBySource(name)` = 同上，按名字。
                //   ⇒ 两半都没建 ⇒ **出声、不改状态**（⛔ 不拿超集顶替：那会静默多摘别的牌身上的效果）。
                // 📊 池内 `buffType 6` = 0 行 · `buffType 30` = 0 行（`cards_truth.json` 现读）。
                case 6:
                case 30:
                    ctx.Log($"⚠️{who}：`buffType {b.BuffType}`（{(b.BuffType == 6 ? "changeRace" : "addRace")}）"
                          + "今天**没有落点**：本仓没有 race 的运行期字段，也没有"
                          + "「摘掉这张牌身上全部 extrinsic」的口 ⇒ 不改状态（原版 `AddEffect.c:750` / `:951` 那两半都没建）");
                    if (unresolved != null) unresolved.Add($"{who}（buffType {b.BuffType} 未建）");
                    return false;

                // ---- `case 10 changeAbilityCost`（与 `case 1 addStats` / `case 2 changeCost` 共用合并循环，
                //      `.c:330-459`）----
                // 我们：主动能力的费用读的是**卡表数据**（`CardDef.ActiveAbilityManaCost`，`RuleCore.AbilityCostGate`），
                //   没有运行期覆盖位 ⇒ 「改主动能力费用」没有落点。⚠️ 与 `buffType 2 changeCost` **不是**同一条
                //   （那条是**手牌/卡面费用**，我们有 `ctx.CostMods`）⇒ ⛔ 别拿 `CostMods` 顶。
                // 📊 池内 `buffType 10` = 0 行。
                case 10:
                    ctx.Log($"⚠️{who}：`changeAbilityCost(10)` 今天**没有落点**：我们的主动能力费用恒读卡表"
                          + $"（`CardDef.ActiveAbilityManaCost`；⛔ 与手牌费用的 `ctx.CostMods` 不是同一条）");
                    if (unresolved != null) unresolved.Add($"{who}（buffType 10 未建）");
                    return false;

                // ---- `case 0xb(11) removeExtrinsic`（`.c:823-825`）----
                // 原版 = **一句**：`CardScript.RemoveExtrinsicEffectsFrom(this, newEffect.enchantingCard)` ——
                //   把「施放者是 `enchantingCard`」的那批 extrinsic 效果从**这一张牌**身上摘掉
                //   （`.c` 那一支**不** `List.Add` ⇒ 这条 effect 本身不留记录）。
                // 我们：`UnitState.ContainsExtrinsicEffectsFrom(src)` 有（谓词），但**摘**那个动作只有
                //   全棋盘版（`RuleCore.RemoveExtrinsicEffectsFrom` / `Aura.RemoveExtrinsicBySource`）
                //   ⇒ 摘哪张牌不可控 ⇒ 出声、不改（见 `case 6` 头注的同一条理由）。
                // 📊 池内 `buffType 11` = 0 行。
                case 11:
                    ctx.Log($"⚠️{who}：`removeExtrinsic(11)` 今天**没有落点**：我们只有「全棋盘按施放者摘」的口"
                          + "（`RuleCore.RemoveExtrinsicEffectsFrom`），没有「只从这一张牌上摘」的口");
                    if (unresolved != null) unresolved.Add($"{who}（buffType 11 未建）");
                    return false;

                // ---- `case 0x19(25) ephemeralCopy`（`.c:943-950`）----
                // 原版：`CardScript.AddTraitSilently(this, 5 /*ephemeral*/)` + `BattleCardUI.ShowEphemeral`（表现层）。
                // 我们：`AddKeyword("ephemeral", 1)`（`_keywords` 那本账；`ephemeral` 是**认得出的 trait 名**，
                //   `CardDef` 的词表里有它）。表现层那颗图标本仓没有对应件（⛔ 不假装有）。
                case 25:
                {
                    bool added = target.AddKeyword("ephemeral", 1);
                    ctx.Log($"{who}：「{target.Name}」的 `ephemeralCopy(25)` ⇒ "
                          + (added ? "挂上 `ephemeral`（原版 `AddEffect.c:947` `AddTraitSilently(this, 5)`）"
                                   : "**没挂上**（`AddKeyword` 拒收 —— 见它的两支闸）"));
                    return added;
                }

                // ---- `case 0x28(40) replaceAbility`（ILSpy `:17808-17836`；`.c` 那一档在 `default` 里）----
                // 原版：`enchantment == null` ⇒ `LogError` 什么都不做；否则 `hasActiveAbility = true` +
                //   `activeAbilityRef(+0x118) = newEffect.enchantment`（**运行期覆盖**主动能力指向的那张卡）。
                // 我们：`CardDef.Ability` **恒读卡面**、没有运行期覆盖位 —— 这一档的**净行为等于「重置回卡面」**，
                //   而账 `:1026`（`EntityScript.GetActiveAbilityCard`）已经把这个结论记死：
                //   池内 `buffType 40` = **0 行** ⇒ 恒回 `+0x28` ⇒ **净等价**（⛔ 别为此新造运行时覆盖位）。
                case 40:
                    ctx.Log($"{who}：`replaceAbility(40)` 净等价 —— 我们的 `CardDef.Ability` 恒读卡面，"
                          + "原版那一格在池内 0 行时也恒回卡面（判据 = 账 `:1026`；⛔ 不新造覆盖位）");
                    return false;

                // ---- `case 0x2d(45) addStatsDropPod`（ILSpy `:17844-17880`）----
                // 原版：`List.Add`；`healthChange != 0 ⇒ currentDropPodHealth(+0xcc) += healthChange`；
                //   `meleeAttackChange != 0 ⇒ currentDropPodAttack(+0xe0) += meleeAttackChange`。
                // 我们：drop pod 血量用 `KwValue(droppod)` 表达（🔶 既有裁定，见 `UnitState.RemoveEffectRecord`
                //   `case 45` 的反向实现）⇒ **只有血量那半有落点**；攻击那半如实记「未建」。
                case 45:
                {
                    if (b.HealthChange == 0)
                    {
                        ctx.Log($"{who}：`addStatsDropPod(45)` 的 `healthChange == 0` ⇒ 只记日志"
                              + "（原版 `UpdateFigures` 无状态变化）");
                        return false;
                    }
                    target.AddKeyword(KeywordTable.DropPod, b.HealthChange);
                    ctx.Log($"{who}：`addStatsDropPod(45)` ⇒ `droppod` {b.HealthChange:+#;-#;0}"
                          + (b.MeleeAttackChange != 0
                             ? $"；⚠️ 近战那半（原版 `currentDropPodAttack(+0xe0) += {b.MeleeAttackChange}`）**本仓未建**"
                             : ""));
                    return true;
                }

                // ---- `case 0x46(70) setMeleeAttack` / `case 0x55(85) setRangeAttack`（ILSpy `:17791-17803`）----
                // 原版：`RemoveActiveEffects(buffType)`（同一档旧记录先摘）→ `List.Add` → `UpdateAttackText`。
                // 我们：`AddStatMod(..., isSet: true)` —— 它的头注已经写明「`isSet` 的修饰**先清掉同属性的旧
                //   set 记录**（永远只有一条）」= 同一句 `RemoveActiveEffects`；`SyncAttackTypeAfterStatChange`
                //   = `UpdateAttackText`。
                // ⚠️ 值的来源：`*ValueOptions`（`ChooseValueOptions`，例如 SAU27 的 `50 ThisCardHealth`）
                //   **不在本方法折** —— 原版那一步在 `AbilityLogic.AdjustCardEffect` / `PlayAbility`
                //   （账 `:64` 那一族），本方法收到的已经是折好的数 ⇒ 这里直接用 `MeleeAttackChange` /
                //   `RangeAttackChange`；若那一格是 `*ValueOptions` 形态（change 为 0 而 options 非 0），
                //   **出声**并跳过（⛔ 不拿 0 当「设成 0 攻」）。
                case 70:
                case 85:
                {
                    bool melee = b.BuffType == 70;
                    int change = melee ? b.MeleeAttackChange : b.RangeAttackChange;
                    int options = melee ? b.MeleeAttackValueOptions : b.RangeAttackValueOptions;
                    if (change == 0 && options != 0)
                    {
                        ctx.Log($"⚠️{who}：`buffType {b.BuffType}` 的数值还在 `*ValueOptions={options}` 形态"
                              + "（未折）⇒ 本方法不猜，**跳过**（折叠那一步在 `AbilityLogic.AdjustCardEffect`，账 `:64`）");
                        if (unresolved != null) unresolved.Add($"{who}（buffType {b.BuffType} 的 ValueOptions 未折）");
                        return false;
                    }
                    string attr = melee ? "attack" : "ranged";
                    target.AddStatMod(attr, change, "AddEffect:" + who, extrinsic: b.Extrinsic != 0,
                                      isSet: true, buffType: b.BuffType,
                                      permanent: b.PermanentBuff != 0);
                    ctx.Log($"{who}：`{(melee ? "setMeleeAttack(70)" : "setRangeAttack(85)")}` ⇒ "
                          + $"「{target.Name}」的{attr}基础值被**设为** {change}"
                          + (b.Extrinsic != 0 ? "（extrinsic ⇒ 施放者离场时整份收回）" : "")
                          + "（原版 `CardScript__AddEffect.c` 的 `case 70/85`：先 `RemoveActiveEffects(同档)` 再挂）");
                    return true;
                }

                // ---- `case 0x5f(95) setCost`（ILSpy `:17756-17766`）----
                // 原版：`RemoveActiveEffects(95)` → `List.Add` → `UpdateCostText`。
                // 我们：`ctx.CostMods` 里落一条 **`SetTo`**（不是折成 delta —— 判据见 `DoLowerCost` 的
                //   「设为 N 费」那一段：`get_CurrentCost` 是 `clamp0(N + Σ 全部 changeCost)`）。
                //   `RemoveActiveEffects(95)` 的等价：先摘掉同 `Key` 的旧 `SetTo` 记录（同一档永远只剩一条）。
                case 95:
                {
                    var def = target.Card;
                    if (def == null) return false;
                    int seat = OwnerOf(ctx, target);
                    if (seat < 0) seat = owner;
                    for (int i = ctx.CostMods.Count - 1; i >= 0; i--)
                    {
                        var m = ctx.CostMods[i];
                        if (m != null && m.Player == seat && m.SetTo >= 0 && m.Key == def.Id) ctx.CostMods.RemoveAt(i);
                    }
                    ctx.CostMods.Add(new CostMod
                    {
                        Player = seat,
                        Key = def.Id,
                        Delta = 0,
                        SetTo = b.CostChange,
                        ExpireTurn = -1,      // 原版 `setCost` 不带到期位 ⇒ 永久
                    });
                    ctx.Log($"{who}：`setCost(95)` ⇒ 「{def.Name}」的费用设为 {b.CostChange}"
                          + "（原版 `AddEffect.c` 的 `case 95`：`RemoveActiveEffects(95)` → `List.Add` → `UpdateCostText`）");
                    return true;
                }

                default:
                    // `AddEffect.c` 里这一档本来就走 `default:` 的「未实现」警告（`:958-966`）
                    ctx.Log($"⚠️{who}：`buffType {b.BuffType}` 不在本表覆盖的 11 组里 —— "
                          + "原版 `CardScript__AddEffect.c:960` 对它也是打警告、不改状态");
                    return false;
            }
        }
    }
}
