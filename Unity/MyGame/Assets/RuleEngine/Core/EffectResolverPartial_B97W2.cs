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
        //    thisCard = this, created /*→ **actingCard** 位*/, 0, 0)` ⇒ **`actingCard` = 被造出来的那张**、`targetCard = null`（⚠️ ILSpy `CardScript.cs:16000-16002` 把它渲染成 `actingCard = default`，那是**丢实参**的假象 —— 与「ILSpy 丢常量」同族）。
        //
        //  ✅ **2026-10-30（`B99_C1` · 账 `:203` / `#34 G23③`）棋盘那一跳已补齐** —— 见本文件类尾
        //    `BroadcastCardCreatedBoardHop`：遍历双方 `PlayerState.Board`、闸 `UnitState.IsInPlayState(ctx)`
        //    （= 原版 `CardScript__IsInPlay()` 的 `{2,3,15,17}` 四档，判据 `CardScript__IsInPlay.c` 全文）、
        //    逐个 `RuleCore.FireTriggerByNumber(…, 210, …)`。
        //    ⚠️ 旧头注写「`RuleCore.FireTriggerAt` 缺 `opsOverride` 形参（账 `:1288`）所以棋盘跳做不了」——**已过期**：`RuleCore.cs` 现读**已有** `List<EffectOp> opsOverride = null`（`B97_W28` 加的）。
        //    今天的可达面：**全池 `abilityTriggers` 含 210 的 = 0 / 1,129**（`cards_engine.json` 现读）
        //    ⇒ 那一跳落不落，**零可观测**（但铁律 11：原版有 ⇒ 就得照做，⛔ 不是「不做」的理由）。
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
                        // `subject` / `target` / `actingCard` 三个形参：原版这一跳把 `created` 送进 **`actingCard`**
                        //   位（`CardScript__ReactToCardCreated.c` 的**第 5 个**实参）；我们的 `actingCard` 口只收
                        //   `UnitState`，而 created 是 `CardInstance` ⇒ 今天**只能传 `null`**（结构性残差，另立账）；
                        //   `targetCard` 原版是 0 ⇒ `null`；`subject`（「这件事发生在谁身上」）手牌上没有 ⇒ `null`。
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
        /// <para>🔴 **2026-11-03 订正（铁律 5）：原写「今天没有任何生产调用点」已过期** —— 现读有 **2 个真实生产调用点**：`EnchantmentExecutor_B115.cs:312`（附魔模板路）与 `ChoiceOptionCards_B116W32.cs:155`（选项卡路 → `RunAbilityBuffRowOfEnchantment`）。⇒ `buffType` 表在**附魔 / 选项卡那条路上是活的**，⛔ 别再说「整张表没有消费者」。卡表主路（`FireTriggerRowsByChosenAbility`）仍然只在正文 op 为空时兜底。本方法是引擎侧的
        /// `CardScript.AddEffect` 等价位；**卡表主路**要不要也接，仍等「正文路让位」这件事定了再说。
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
                // ---- 🆕 2026-11-02（`B115_S5` · 波 2b）：`case 1 addStats`（`.c:331-459` 的合并循环）----
                //  原版那一段**不是单条赋值**：先倒序扫 `activeEffects(+0x108)` 找「同 `enchantingCardId(+0xa4)` ∧
                //  同 `buffType(+0x38)` ∧ 同 `markOfChaosType(+0x78)` ∧ `daemonHostType(+0x7c)==0` ∧
                //  `mutationType(+0x80)==0` ∧ `doNotStack(+0x75)==0` ∧ `SameTimingAsEffect`」那一条，
                //  把 `meleeAttackChange(+0x48)` / `rangeAttackChange(+0x50)` / `healthChange(+0x60)` /
                //  `costChange(+0x68)` **累加**进去（`.c:399-433`）；找不到才 `List.Add`（`.c:436`）。
                //  然后 `healthChange != 0` ⇒ 改上限（正数直接加、负数夹到当前上限，`.c:441-479`）；
                //  `armourChange(+0x58) != 0` ⇒ `AddArmour`（`.c:480`）。
                //  ⇒ 我们的等价位 = **`UnitState.AddStatMod`**：它的「同属性同来源合并」就是那个合并循环，
                //     `health` 那一格改的是上限，`armour` 走 `AddKeyword(armour, n)`（与 `case 45` 的 droppod 同形）。
                //  ⚠️ `costChange` 那一格**没有落**（原版改的是 `CardEffect.costChange` 那一本的合并；
                //     我们的手牌费用在 `ctx.CostMods`、与 `RuleCore.DoLowerCost` 同一条 —— 见 `case 2`）。
                case 1:
                {
                    bool any = false;
                    if (b.MeleeAttackChange != 0)
                    {
                        target.AddStatMod("attack", b.MeleeAttackChange, "AddEffect:" + who,
                                          extrinsic: b.Extrinsic != 0, buffType: b.BuffType, permanent: b.PermanentBuff != 0);
                        any = true;
                    }
                    if (b.RangeAttackChange != 0)
                    {
                        target.AddStatMod("ranged", b.RangeAttackChange, "AddEffect:" + who,
                                          extrinsic: b.Extrinsic != 0, buffType: b.BuffType, permanent: b.PermanentBuff != 0);
                        any = true;
                    }
                    if (b.HealthChange != 0)
                    {
                        target.AddStatMod("health", b.HealthChange, "AddEffect:" + who,
                                          extrinsic: b.Extrinsic != 0, buffType: b.BuffType, permanent: b.PermanentBuff != 0);
                        any = true;
                    }
                    if (b.ArmourChange != 0)
                    {
                        target.AddKeyword(KeywordTable.Armour, b.ArmourChange);   // 原版 `.c:480 CardScript__AddArmour`
                        any = true;
                    }
                    if (b.CostChange != 0)
                    {
                        ctx.Log($"⚠️{who}：`addStats(1)` 里的 `costChange = {b.CostChange}` **本仓没有落点**"
                              + "（原版改 `CardEffect.costChange` 那本合并账；我们的手牌费用在 `ctx.CostMods`，"
                              + "与 `case 2 changeCost` 同一条 —— 见那一支）");
                        if (unresolved != null) unresolved.Add($"{who}（buffType 1 的 costChange 未建）");
                    }
                    ctx.Log($"{who}：「{target.Name}」的 `addStats(1)` ⇒ 近战 {b.MeleeAttackChange:+#;-#;0} · "
                          + $"远程 {b.RangeAttackChange:+#;-#;0} · 生命上限 {b.HealthChange:+#;-#;0} · "
                          + $"护甲 {b.ArmourChange:+#;-#;0}（原版 `CardScript__AddEffect.c:331-459`）");
                    return any;
                }

                // ---- 🆕 2026-11-02（`B115_S5` · 波 2b）：`case 2 changeCost`（`.c:331-459` 同一段合并循环的另一半）----
                //  原版与 `case 1` 共用那个合并循环，累加的是 `costChange(+0x68)`；我们这一侧手牌费用
                //  的唯一台账是 `ctx.CostMods`（`RuleCore.DoLowerCost` 那一路）—— 但那条是**翻出正文 op 之后**
                //  才写的，直接从 `CardEffect` 灌进去会越过它自己的判据 ⇒ **本支先出声、不改状态**，
                //  留待「`changeCost` 的数据行落点」单独定口径（⛔ 不拿 `CostMods` 硬塞）。
                case 2:
                    ctx.Log($"⚠️{who}：`changeCost(2)` 的 `costChange = {b.CostChange}` **本仓暂无数据行落点**"
                          + "（原版 `.c:331-459` 把它累加进 `CardEffect` 那本合并账；我们的手牌费用由"
                          + " `ctx.CostMods` 承载、写点只有正文 op 那一路）⇒ **本行不改状态**");
                    if (unresolved != null) unresolved.Add($"{who}（buffType 2 未建）");
                    return false;

                // ---- 🆕 2026-11-02（`B115_S5` · 波 2b）：`case 3 addTrait`（`.c:460-679`）----
                //  原版：`CardTraitCollection.GetTrait(trait)` → 若 `trait != poisoned(200)` 或身上没有
                //  `resistant(600)`：先 `RemoveActiveEffects(this, 4)`（摘掉同卡「去 trait」那本账，`.c:474`）；
                //  再倒序扫 `activeEffects` 找「`buffType(+0x38)==3` ∧ 同 `trait(+0x3c)` ∧ 同 `markOfChaosType`
                //  ∧ 同 `daemonHostType` ∧ `SameTiming` ∧ **两条都非 extrinsic**(`+0x30==0`)」那一条，
                //  按 `GetTrait(trait).+0x24` 三档合并 `traitValue(+0x40)`：**0 ⇒ Max**（`.c:529`）·
                //  **10 ⇒ 相加**（`.c:541`）·  **0x14(20) ⇒ 计数 +1**（`.c:549`）；
                //  找不到才 `List.Add` + `AddTraitSilently(this, trait, traitValue)`（`.c:567-568`）。
                //  ⇒ 我们的等价位 = **`UnitState.ApplyTraitEffect(…)`**（记录账 + 合并 + 落到 `_keywords`）—— 改前只调
                //     `AddKeyword`（**D118-77**：结构化 buff 路的 addTrait 在记录账上不存在 ⇒ 到期 / 按来源撤 / `AnyEffectGrantsTrait` 全失效）。
                //     ⚠️ 三档合并（Max/相加/计数+1）本仓没有 `CardTraitCollection` 静态表 ⇒ 交给 `TraitStackTypeOf`（只写死 `markOfChaos=count`）。
                case 3: case 90:   // 🆕 2026-11-02（`B116_W8` · ENG-11）：`90 addTraitRemnant` 在**原版就是同一个体**（`CardScript__AddEffect.c:220` `if (iVar16 == 0x5a) goto switchD_1805e5668_caseD_3;` ⇒ 直接跳进 `case 3` 那段），⛔ 别另写第二份
                {
                    string kw = CardDef.DefinedTraitName(b.Trait);
                    if (string.IsNullOrEmpty(kw))
                    {
                        ctx.Log($"⚠️{who}：`addTrait(3)/addTraitRemnant(90)` 的 `trait = {b.Trait}` **不在 `DefinedTrait` 反查表里**"
                              + " ⇒ 本行不结算（⛔ 不猜名字；表 = `CardDef.DefinedTraitName`，`CardDefPartial_B94B16.cs`）");
                        if (unresolved != null) unresolved.Add($"{who}（trait {b.Trait} 认不出）");
                        return false;
                    }
                    bool added = ApplyAddTraitRecordRow(target, b, kw, who);   // 🔴 2026-11-03（`B120-W7` · `D118-77`）：值 > 0 走**记录原语** `UnitState.ApplyTraitEffect`（= 原版 `activeEffects(+0x108)` 那一条记录 + 合并 + 落到 `_keywords`）；`traitValue <= 0`（空给）保持 `AddKeyword`（与文本 op 路 `ApplyOneGain` 同一口径）⇒ **净关键词增量与改前逐字相同**，多的只是记录账。判据全文 → `RuleEngine/Core/RuleCorePartial_B120W7.cs` 的方法头
                    ctx.Log($"{who}：「{target.Name}」的 `{(b.BuffType == 90 ? "addTraitRemnant(90)" : "addTrait(3)")}` ⇒ `{kw}` {b.TraitValue}"
                          + (added ? "" : " —— **没挂上**（`AddKeyword` 拒收，见它的闸）")
                          + "（原版 `CardScript__AddEffect.c:460-679` → `AddTraitSilently`）");
                    return added;
                }

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
                { if (target.Has("ephemeral")) { UnityEngine.Debug.LogWarning($"{who}：「{target.Name}」的 `ephemeralCopy(25)` **已带 `ephemeral` ⇒ 去重**（原版 `CardScript__AddTraitSilently.c`：`Any` 命中即 return；⛔ 不是 `AddKeyword` 的同键累加）"); return false; }
                    bool added = target.AddKeyword("ephemeral", 1, silent: true);   // `silent` = 原版 `AddTraitSilently` 的「不置图标脏」（`UnitState.AddKeyword` 头注；缺省会把 `TraitRev` +1）
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
                    if (b.BuffType == 8) return ApplyMarkOfChaosBuff(ctx, owner, target, b, who, unresolved);
                    if (b.BuffType == 12 || b.BuffType == 15 || b.BuffType == 16)
                        return ApplyAttachEnchantmentBuff(ctx, target, b, who, unresolved);
                    // 🆕 2026-11-02（`B116_W8` · ENG-11）：`8` / `12/15/16` 已从这一档分出去（两个 helper 在类尾）；
                    //   `90 addTraitRemnant` 更早一步就并进了 `case 3`（原版同一个体）。剩下的只剩「本表连分组都没有」的档。
                    //   `12/15` **已接上**（`B116_W24`；`enchantmentId` 列 `B116_W18` 补的），`16` 仍不结算（原版那一支多出四个 spellType 旁支，见 helper 的 remarks）；
                    //   `2 changeCost`（15 行）同样不结算，缺**两件**：① 手牌 buff 台账（`ctx.CostMods` 的契约只认正文 op 一个写点）② `ta=50` 的目标解析；现读 15 行里 **10 行 `at=handBuff`** 根本不走 `AddEffect`、剩 5 行里 **3 行 `ta=50`**（`Living Icon` 1 + `Solar Eclipse` 2）先卡在 `ResolveBuffOthersTargets`、**2 行 `ca=50`** 走手牌路 ⇒ 今天硬写只会是死码（详见 `case 2` 自己那段头注）。
                    ctx.Log($"⚠️{who}：`buffType {b.BuffType}` **本表尚未实现** —— "
                          + "⚠️ 这不是「原版也没有」：原版 `CardScript__AddEffect.c` 对它有自己的分支，"
                          + "本仓这一档**今天不改状态**（判据 → `报告/B115_S5_附魔执行器波01.md` §十三）");
                    return false;
            }
        }

        // ==============================================================================================
        //  账 `:203` / `#34 G23③` —— `BattleManager.BroadcastCardCreated` 的**棋盘那一跳**
        // ==============================================================================================
        //  **判据（第一权威，全文现读）** = `decomp_full/BattleManager__BroadcastCardCreated.c:19-33`：
        //    `lVar3 = *(longlong *)(bm + 0x470);`  ← `BattleManager.unitsInPlay`（ILSpy `BattleManager.cs:14355`）
        //    逐个 `CardScript__IsInPlay(卡, 0)` 为真 ⇒ `CardScript__ReactToCardCreated(卡, created, 0)`。
        //  **闸** = `CardScript__IsInPlay.c` 全文 = `cardState ∈ {2 inPlay, 15 inPlayTransforming, 3 inPlayAttacking,
        //    17 inPlayAminingAttack}` ⇒ 闸写 `UnitState.IsInPlayState(ctx)`。
        //    ⛔ **别写成 `CardStateOf(ctx) != inPlay`** —— 那会把正在结算瞬态（3/15/17）的单位漏掉，与原版差三档。
        //  **接收体** = `CardScript__ReactToCardCreated.c`：`OnTrigger(0xd2 = 210, mgr, thisCard = 监听者,
        //    actingCard = **created**, targetCard = 0, 0)` ⇒ 走 `RuleCore.FireTriggerByNumber(…, 210, …)`。
        //
        //  ⚠️ **`actingCard` 今天只能传 `null`（如实标的结构性残差，另立账）**：`FireTriggerByNumber` 与
        //    `EventSlotsScope.TargetCardAndActing` 的 `actingCard` 形参都是 `UnitState`，而这里被造出来的是
        //    `CardInstance`（可能根本不在场上）⇒ 送不进去。要逐字对齐得给 `BattleContext` 加一格
        //    `CardDef EventActingCardDef`（触碰 `BattleContext.cs`，**不属本代理**）。
        //    池内 `abilityTriggers` 含 210 的 = **0 / 1,129**（`cards_engine.json` 现读）⇒ 零可观测。
        //
        //  ⚠️ **`unitsInPlay` ≈ 我们的 `PlayerState.Board`**（督军在 `Board[BoardSpec.WarlordSlot]` 上）：
        //    原版那条 List 里还有英雄部件 / 武器等我们没建模的槽 ⇒ 覆盖面比原版窄，这是**既有的建模差**，
        //    ⛔ 不是本方法该补的东西（别为它新造棋盘槽）。
        //
        //  ⛔ **「召唤路径」那半不做（原版没有）**：原版 `BroadcastCardCreated` 全树只有 2 个调用点
        //    （`_ResolveCreateHandCard_d__512__MoveNext.c:117` ＋ `_ResolveRecallToHand_d__542__MoveNext.c:85`）；
        //    召唤走 `ResolveUnitSummoned` / `BroadcastUnitSummoned` 那一族，**不调** `BroadcastCardCreated`
        //    ⇒ 给 `DeployFreeCore` 补 210 = **造原版没有的行为**（铁律 18·0）。判据 → `判据/B99_R3a_施工单_战斗按文件.md` §五 #9。
        /// <summary>**`BroadcastCardCreated` 的棋盘那一跳**（遍历双方 `Board`、`IsInPlay` 闸、`CardCreated(210)`）。
        /// <para>判据 / 残差 / 可达性 → 上面那一整段。</para></summary>
        /// <param name="created">刚被造出来的那一份（原版把它送进 `actingCard` 位；今天只能传 null，见上）。</param>
        /// <returns>真排进队列的监听器条数。</returns>
        public static int BroadcastCardCreatedBoardHop(BattleContext ctx, CardInstance created)
        {
            if (ctx == null || ctx.IsOver || created == null || created.Card == null) return 0;
            int n = 0;
            using (BeginAction(ctx))
            {
                for (int seat = 0; seat < 2; seat++)
                {
                    var ps = ctx.Players[seat];
                    if (ps == null || ps.Board == null) continue;
                    for (int slot = 0; slot < BoardSpec.Size && slot < ps.Board.Length; slot++)
                    {
                        var u = ps.Board[slot];
                        if (u == null || u.Card == null) continue;
                        // 原版 `CardScript__IsInPlay()` 闸（`{2,3,15,17}`，见上）
                        if (!u.IsInPlayState(ctx)) continue;
                        if (FireTriggerByNumber(ctx, u, 210, seat, slot,
                                                actingCard: null, targetCard: null)) n++;
                    }
                }
            }
            if (n > 0)
                ctx.Log($"（`CardCreated(210)` 棋盘那一跳：「{created.Card.Name}」被造出来 ⇒ "
                      + $"{n} 个在场监听器入队；原版 `BattleManager__BroadcastCardCreated.c:19-33`）");
            return n;
        }

        // ==============================================================================================
        //  🆕 2026-11-02（执行代理 `B116_W8` · ENG-11）：`switch` 的 `default:` 里那两档的下沉实现
        // ==============================================================================================
        //  **为什么体在类尾、分派在 `default:`**：本文件被 `账/*.tsv` 按 `文件:行号` 引用
        //    （`战斗逻辑驱动.tsv` 的 `:109` / `:175` / `:205` / `:533`）⇒ `switch` 里**必须零行数变化**
        //    （铁律 18·b），所以 `default:` 只加两行 `if … return Helper(...)`，体全部追加在这里。
        //
        //  **本批动到哪几档（第二张表 `enchantment_templates.json` 的 86 行里的 37 行）**：
        //    · `8 markOfChaos`（3 行）—— ✅ **本批接上**（原版 `.c:776-793`）。
        //    · `90 addTraitRemnant`（1 行）—— ✅ **本批接上**（原版 `.c:220` 直接 `goto` 进 `case 3` 那段，
        //      ⇒ 已经在 `case 3: case 90:` 那个合并标签里，没有新方法）。
        //    · `12 enchantment` 13 行 / `15 singleUseEnchantment` 5 行 —— ✅ **2026-11-02（`B116_W24`）接上**
        //      （数据列 `enchantmentId` 由 `B116_W18` 补，执行器 = `ApplyAttachEnchantmentBuff` 里的新支）。
        //    · `16 choiceEnchantment` **0 行** —— ⛔ **仍不结算**：原版 `.c:830-932` 除了 `AddEnchantment` 还多出
        //      **四个 spellType 旁支**（`0x3c` secretBuff / `0x78` questBuff / `0x82` 用掉并触发 / `0x8c,0xbe` 印记），
        //      本仓这四支一个都没建 ⇒ 只做半条会成为「做一半」，判据见 helper 的 remarks。
        //    · `2 changeCost` 15 行 —— 按既有裁定保持「出声、不改状态」（理由在 `case 2` 自己的头注里）。
        //  ⇒ 37 行里**接上 22 行**（8 的 3 + 90 的 1 + 12 的 13 + 15 的 5），剩 15 行（`2 changeCost`）。

        /// <summary>`MarkOfChaosType` → 我们这边的黑暗契约种类名（`DarkPactFx` 的第一列）。
        ///
        /// <para>映射**照抄** `EffectResolver.MarkOfChaosTypeOf` 那张表（`blood 10 Khorne` / `resilience 20 Nurgle`
        /// / `excess 30 Slaanesh` / `fate 40 Tzeentch`，三重已证）的**反向**；`50` = 原版
        /// `Enchantments.json` 把四张卡挂在同一格 ⇒ 均匀四选一。⛔ 别在这里再写一张**不同**的数字表
        /// （铁律 6；那边是「种类 → 数字」的唯一正本，这里只做反向查）。</para>
        /// <returns>`null` = 原版 `Enchantments.GetMarkOfChaos` 也取不到（`0 none` / 认不出）⇒ 整条 case 8 不发生。</returns>
        static string DarkPactKindOfMarkType(BattleContext ctx, int markType)
        {
            switch (markType)
            {
                case 10: return "blood";        // Khorne
                case 20: return "resilience";   // Nurgle
                case 30: return "excess";       // Slaanesh
                case 40: return "fate";         // Tzeentch
                case 50:                        // Random：`Enchantments.json` 的 `cardEffect: 50` 那一格挂了 4 张卡
                    if (ctx == null) return null;
                    return DarkPactFx[ctx.GetRandomInt(0, DarkPactFx.Length)][0];
            }
            return null;
        }

        /// <summary>**`buffType 8 markOfChaos`**（原版 `CardScript__AddEffect.c:776-793`）。
        ///
        /// <para>原版逐句：`Log` → `Enchantments.GetMarkOfChaos(bm + 0x38, newEffect.markOfChaosType(+0x78))`
        /// （**取不到就整条跳过**，`:782` 的 null 闸）→ `RawCardScript.TriggerEnchantment(那张附魔卡, …)`
        /// → `bm.lastMarkOfChaos(+0x2f0) = 那张卡` → `BattleManager.BroadcastMarkResolved(this)`。</para>
        ///
        /// <para>⚠️ **我们的等价位是整个 `GrantDarkPact`**（正文路 `give … a Dark Pact` 走的同一条），
        /// 它已经把这五步逐一对上：① `markOfChaos(330)` 计数 + `darkpact` 层数（= 那张附魔卡 500 能力的
        /// `addTrait(330)`）② `SetLastMarkOfChaos`（= `+0x2f0`）③ `EmitEnchantmentTrigger`（= `TriggerEnchantment`）
        /// ④ `BroadcastWhen(GetsDarkPact)`（= `BroadcastMarkResolved` → `OnTrigger(510 UnitReceivesMarkOfChaos)`）
        /// ⑤ 「After receiving a Dark Pact」/ 同伴那两跳。⛔ **别在这里另写一份印记逻辑**（铁律 6）。
        /// 判据与两处模型差异的完整说明在 `EffectResolver.cs` 的 `ApplyMarkBody` 头注里。</para>
        /// </summary>
        /// <returns>`true` = 印记落地（`GrantDarkPact` 已改状态）；`false` = 取不到那张附魔卡（原版同样整条跳过）。</returns>
        static bool ApplyMarkOfChaosBuff(BattleContext ctx, int owner, UnitState target,
                                         CardDatabase.AbilityBuffSpec b, string who,
                                         List<string> unresolved)
        {
            if (ctx == null || target == null || b == null) return false;
            string kind = DarkPactKindOfMarkType(ctx, b.MarkOfChaosType);
            if (kind == null)
            {
                // 原版 `.c:782` 的 null 闸：`GetMarkOfChaos` 回 null ⇒ **整条 case 8 不发生**
                ctx.Log($"⚠️{who}：`markOfChaos(8)` 的 `markOfChaosType = {b.MarkOfChaosType}` "
                      + "**在 `Enchantments` 表里没有对应的附魔卡**（原版 `Enchantments__GetMarkOfChaos.c:20-36` "
                      + "遍历后回 null，`.c:782` 那一句随即整条跳过）⇒ **本行不改状态**");
                if (unresolved != null) unresolved.Add($"{who}（markOfChaosType {b.MarkOfChaosType} 无对应附魔卡）");
                return false;
            }
            int seat = OwnerOf(ctx, target);
            if (seat < 0) seat = owner;
            GrantDarkPact(ctx, seat, target, kind, who + "·markOfChaos(8)");
            ctx.Log($"{who}：`markOfChaos(8)` ⇒ 「{target.Name}」获得「{kind}」印记"
                  + $"（由 `markOfChaosType {b.MarkOfChaosType}` 定；原版 `CardScript__AddEffect.c:776-793`）");
            return true;
        }

        /// <summary>**`buffType 12 / 15`**（原版 `CardScript__AddEffect.c:826-829` → `CardScript__AddEnchantment`）
        /// —— **已接上**（2026-11-02 `B116_W24`）。`16 choiceEnchantment` 仍不结算（见 remarks 第 3 段）。</summary>
        /// <remarks>
        /// **原版逐句（`CardScript__AddEnchantment.c` 全文现读）**：
        ///   `:24` `newEffect == null` ⇒ 什么都不做；`:25` 读 `newEffect.enchantment(+0x88)`（PPtr → 模板卡）；
        ///   `:29-33` `op_Equality(那个 PPtr, 0)` ⇒ **`LogWarning` + `return`（不挂）**；
        ///   `:37-39` 惰性建 `+0x110`；`:40-48` 新记录 = `{enchantment, BattleManager.enchatmentsUsed(+0x4e4)}`
        ///   → `BattleManager__UsedEnchantment`（流水号 +1）；`:50-67` 追加进 `+0x110 activeEnchantments`；
        ///   `:68-90` **同时**把 `param_2`（那条 `CardEffect` 本身）追加进 `+0x108 activeEffects`；
        ///   `:91` `CardScript__ActivateTraitsOnSummonOrEnchantment(this, 0, 1, 0)`。
        ///   ⇒ 我们的等价位 = **`Enchantments.Attach(ctx, carrier, 模板, buffType, source, acting, trait, permanent)`**
        ///   （它逐条对了上面每一步，判据写在它自己的 doc 上 —— ⛔ 别在这里另写一份挂载逻辑，铁律 6）。
        ///   `case 0xc(12)` 与 `case 0xf(15)` 在 `.c` 里是**同一支**（`:826-828` 两个 label 共用一句
        ///   `CardScript__AddEnchantment(param_1, param_2)`）⇒ 本方法不为它们各写一份。
        ///
        /// **数据面（`B116_W18` 补齐，本代理只消费）**：`AbilityBuffSpec.EnchantmentId` / `.EnchantmentPathId`
        ///   = 原版 `CardEffect.enchantment(+0x88)` 解出来的模板 id / PPtr 原值。哨兵口径：
        ///   `PathId == 0` ⇒ **原版那一格就是 null**（走 :29-33 的 `LogWarning` + 不挂，**不是错误**）；
        ///   `PathId != 0 而 Id 为空` ⇒ 数据侧解不出（坏行）；`Id` 有值但表里查不到 ⇒ **表外模板**。
        ///   三档都**跳过 + 出声**，⛔ 静默（红线）。
        ///
        /// 🔴 **`16 choiceEnchantment` 为什么仍不结算**（`.c:830-932` 现读，**池内 0 行**）：
        ///   原版那一支在 `AddEnchantment` 之外**还多出四组旁支**，本仓一个都没建 ——
        ///   ① `:850-855` 模板 `spellType == 0x3c(60 secretOrder)` ⇒ 给那条 effect 置 `secretBuff(+0x90)`；
        ///   ② `:856-886` `spellType == 0x78(120 quest)` ⇒ **先摘掉身上其它 quest 附魔**、置 `questBuff(+0x91)`、
        ///      `+0x270 = 0`、`DisplayStatusAnim(0x2ee)`、`UpdateTraitIcons`；
        ///   ③ `:887-905` `spellType == 0x82(130)` ⇒ `SpendEnchantment` + `TriggerEnchantment` + `OnTrigger(0x1cc = 460)`；
        ///   ④ `:906-930` `spellType ∈ {0x8c(140), 0xbe(190)}` ⇒ `SpendEnchantment` + 写 `bm.lastMarkOfChaos(+0x2f0)`
        ///      + `TriggerEnchantment` + `BroadcastMarkResolved`，末尾清 `+0x2e8`。
        ///   只做第 0 句（挂上去）而漏掉这四支 = **做一半**（玩家会看到「挂上了但那一跳没发生」）⇒ 本代理**整档不做**，
        ///   维持出声。判据 → `报告/B116_W24_ENG11_12_15档.md` §3。
        ///
        /// 📊 现读分布（`enchantment_templates.json` 现读，断言 `B116W24_Assert` 钉住）：
        ///   12 ×13 · 15 ×5 · 16 ×0；非零 PPtr 17 行 → 解出 17 / 解不出 0 / **表外 2**
        ///   （`Raging Storm` 的 15 两行指向 `Z-Sororitas-Raging Storm_enchantment1`，只落在 3 个模板上：
        ///   `Z-NC Goff-Night_enchantment` 11 行 · `T2 BlackLegion-Spreading Corruption_enchantment` 自挂 3 行 ·
        ///   表外 2 行）＋ **2 行 `m_PathID: 0`**（`Daemonic Feast`）。其中**池内可达**的只有
        ///   `Spreading Corruption` 那 3 行（`owners[]` 里 `BL74.inPool = true`）。
        /// </remarks>
        /// <returns>真挂上返回 `true`；`false` = 这一行没有挂（**已经出声**，原因见上）。</returns>
        static bool ApplyAttachEnchantmentBuff(BattleContext ctx, UnitState target,
                                               CardDatabase.AbilityBuffSpec b, string who,
                                               List<string> unresolved)
        {
            if (ctx == null || b == null) return false;
            string name = b.BuffType == 12 ? "enchantment"
                        : b.BuffType == 15 ? "singleUseEnchantment"
                        : "choiceEnchantment";

            // ---------- ① `16 choiceEnchantment`：整档不做（四个 spellType 旁支没建，理由见 remarks）----------
            if (b.BuffType == 16)
            {
                ctx.Log($"⚠️{who}：`choiceEnchantment(16)` **整档不结算** —— 原版 `CardScript__AddEffect.c:830-932`"
                      + " 在 `AddEnchantment` 之外还有四组 spellType 旁支（`0x3c` 置 secretBuff · `0x78` 摘旧 quest"
                      + " 并置 questBuff · `0x82` 用掉+触发 · `0x8c,0xbe` 写 lastMarkOfChaos+广播印记），"
                      + "本仓四支一个都没建 ⇒ 只挂不判 = **做一半**，故整档维持「出声、不改状态」"
                      + "（池内现读 0 行，零可观测）");
                if (unresolved != null) unresolved.Add($"{who}（buffType 16 的四个 spellType 旁支未建）");
                return false;
            }

            // ---------- ② 原版 `AddEnchantment.c:25-33`：`enchantment` 那一格取不到 ⇒ `LogWarning` + 不挂 ----------
            if (string.IsNullOrEmpty(b.EnchantmentId))
            {
                if (b.EnchantmentPathId == 0)
                {
                    // `m_PathID: 0` = 原版 PPtr 的 null（**不是**数据错误）⇒ 走 `.c:29-33` 那条 LogWarning
                    ctx.Log($"⚠️{who}：`buffType {b.BuffType}`（{name}）的 `CardEffect.enchantment(+0x88)` 是 **null**"
                          + "（`m_PathID: 0`）⇒ 原版 `CardScript__AddEnchantment.c:29-33` 打 `LogWarning` 后"
                          + " **直接 return、一张都不挂**（照抄：本行不改状态）");
                    if (unresolved != null) unresolved.Add($"{who}（buffType {b.BuffType} 的 `enchantment` 为 null）");
                    return false;
                }
                // 有 PPtr 却解不出 id ⇒ 数据侧坏行（`CardDatabase.MapAbilityBuffs` 已 `LogError` 一次）
                ctx.Log($"⚠️{who}：`buffType {b.BuffType}`（{name}）带附魔 PPtr（`m_PathID = {b.EnchantmentPathId}`）"
                      + " 但 `enchantmentId` 解不出 ⇒ **判不了挂哪张模板**，本行不改状态"
                      + "（重跑 工具/gen_cards_engine.py --write）");
                if (unresolved != null) unresolved.Add($"{who}（buffType {b.BuffType} 的 PPtr 解不出模板 id）");
                return false;
            }

            // ---------- ③ 载体：原版 `param_1` = **那张 CardScript**（场上与手牌同一套）⇒ 我们的 `CardInstance` ----------
            CardInstance carrier = target != null ? target.Instance : null;
            if (carrier == null)
            {
                ctx.Log($"⚠️{who}：`buffType {b.BuffType}`（{name}）的载体（`target.Instance`）是 null ⇒ "
                      + "原版 `CardScript__AddEnchantment` 拿不到 CardScript，**这一条不挂**");
                if (unresolved != null) unresolved.Add($"{who}（buffType {b.BuffType} 的载体不是 CardInstance）");
                return false;
            }

            // ---------- ④ 取模板：`+0x88` 那个 PPtr 指向的那张（先池后表）----------
            CardDef tmpl = ctx.FindEnchantmentTemplate(b.EnchantmentId);
            if (tmpl == null)
            {
                ctx.Log($"⚠️{who}：`buffType {b.BuffType}`（{name}）指向的附魔模板 `{b.EnchantmentId}` "
                      + "**在本局两张表里都查不到**（多半是「表外模板」：`enchantment_templates.json` 的收录闸是"
                      + " `m_Name` 尾 `_enchantment`）⇒ 原版 `CardScript__AddEnchantment.c:29-33` 的取不到闸，"
                      + "**本行一张都不挂**");
                if (unresolved != null) unresolved.Add($"{who}（buffType {b.BuffType} 的模板 `{b.EnchantmentId}` 查不到）");
                return false;
            }

            // ---------- ⑤ 挂：唯一等价位 `Enchantments.Attach`（⛔ 别另写一份）----------
            //   `trait`：原版这一格 = 那条 `CardEffect.trait(+0x3C)` 本身（`param_2` 原样进 `+0x108`）⇒
            //     `b.Trait == 0` 时明确传 `""`（= 没有 trait），⛔ 别传 `null` —— 那会让 `Attach` 回落到
            //     载体卡的 `EnchantTrait`，凭空给这条附魔安一个原版没有的 trait。
            //  🔴 2026-11-03（`B124_W39` · 账 `B124-SOURCE`）：`source` 传**卡名**（`Source` 的契约就是卡名，
            //     读者 `EffectsToShow` 拿它去 `abilityBuffs` 查原版 `effectText`）⇒ ⛔ 别传 `who` 这条日志标签
            //     （改前传的就是它 ⇒ 这条路挂上的附魔**取不到原文、且不发声**）。原版 `CardEffect+0x18` 是
            //     `enchantingCard`（= 我们的 `ActingUnit`）；回落取 `CreationSource`（save/restore 的
            //     出牌作用域量），⛔ 别回落 `PlayingCard` —— 它**从不清空**，会把上一张牌的名字盖上来
            //     （见 `BattleContext.CreationSource` 的 doc）。这一格本身是 🔶 自己设计（原版没有「来源字符串」）。
            string traitName = "";
            if (b.Trait != 0)
            {
                traitName = CardDef.DefinedTraitName(b.Trait);
                if (traitName == null)
                {
                    ctx.Log($"⚠️{who}：`buffType {b.BuffType}`（{name}）带 `trait = {b.Trait}` 但**不在 `DefinedTrait` 反查表里**"
                          + " ⇒ 这条附魔的 `CardEffect.trait` 记成空（⛔ 不猜名字）");
                }
            }
            var eff = Enchantments.Attach(ctx, carrier, tmpl, b.BuffType,
                                          source: ctx.ActingUnit?.Card?.Name ?? ctx.CreationSource?.Name,
                                          acting: ctx.ActingUnit,
                                          trait: traitName,
                                          permanent: b.PermanentBuff != 0);
            if (eff == null)
            {
                ctx.Log($"⚠️{who}：`buffType {b.BuffType}`（{name}）⇒ `Enchantments.Attach` 拒收，**这一条没挂上**");
                if (unresolved != null) unresolved.Add($"{who}（buffType {b.BuffType} 的 Attach 拒收）");
                return false;
            }
            ctx.Log($"{who}：`{(b.BuffType == 12 ? "enchantment(12)" : "singleUseEnchantment(15)")}` ⇒ "
                  + $"把附魔「{tmpl.Name}」挂到「{target.Name}」身上"
                  + "（原版 `CardScript__AddEffect.c:826-829` → `CardScript__AddEnchantment.c`："
                  + "读 `newEffect.enchantment(+0x88)` → 流水号 → `+0x110`/`+0x108` 各追加一份 → `ActivateTraitsOnSummonOrEnchantment`）");
            return true;
        }
    }
}
