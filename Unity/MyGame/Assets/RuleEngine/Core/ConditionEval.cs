// ConditionEval.cs — 原版**结构化条件**求值器（`UnitConditions` / `TargetCriteria.conditionsFilter`）
//
// 本文件是**唯一**一处「`UnitConditions` 数值 → 真/假」的实现。消费者：
//   · `RuleCore.MeetsUnitCondition`（`EffectResolver.cs`，**转发到本类**，别在那边再写一份）；
//   · 目标池筛（`EffectResolver.ApplyStructuredConditionFilter`）、条件从句（`TryStructuredCondition`）。
//
// 判据（第一权威 = 全量反编译 `d:/2/tools/decomp_full/`）：
//   · 求值入口 **`FilterMethods__MeetsConditionCriteria.c`**（390 行，逐值 switch）。
//     参数 `(card, UnitConditions conditionsFilter, bool exclude, CardScript thisCard)`；
//     末段 `:384-389` 的语义 = **`exclude ? !match : match`**（唯一的取反处）。
//     ⚠️ 桩里那个形参名叫 `excludeTrait`，**是错的** —— 调用点
//     `FilterMethods__CheckIfMeetsCriteria.c:298-300` 传的是 `*(undefined1 *)(param_4 + 0x3d)`
//     = `TargetCriteria.excludeCondition`（`+0x3c` 才是 `excludeTrait`，它给 `MeetsTraitCriteria`）。
//   · 枚举 **`UnitConditions`**：`d:/2/tools/il2cpp_out/dump.cs:20735-20819`（35 个成员，含 `none = 0`）。
//     合法值表另有一份在 `CardDef.UnitConditionValues`（数据入口的校验名单，与本文件同源）。
//   · 每个值用到的辅助方法体，逐条在下面每一格标了**文件名**（都在 `d:/2/tools/decomp_full/`）。
//
// 🔴 **返回 `null` = 「本版判不了」**（⛔ 不是「不成立」）—— 调用方必须区别对待：
//    目标池那一侧拿到 null 时**不筛**并出声（`ApplyStructuredConditionFilter`），
//    ⛔ 不许把 null 读成 false（那会把池子筛空 / 把条件从句判成不成立，两种都是静默错）。
//
// 🔴 **`exclude` 只在 <see cref="Meets"/> 的出口做一次**（原版 `:384-389`）——
//    ⛔ 调用方不要再取反一次；`Match` 的每一格返回的都是**未取反**的 match。
//
// ⚠️ **枚举名 ≠ 语义**（踩过）：`220` 的枚举名是 `dealtKillDmg`，实现却是
//    `EnoughPendingDamageToDie`（「这条命已经救不回来了」）。逐值判据一律以下面的行号为准。
//
// ⚠️ **本类只回答「一个单位/一张卡满不满足这一格条件」**。`role == "count"` 的「数几个、
//    跟谁比」是另一层（`BoardCriteria.countOptions/triggerIf/referenceValue` ＋ `countSet`），
//    不在本类里；`role == "multi"/"mult"/卡级计分` 同理（各有各的消费者）。
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>原版 `FilterMethods.MeetsConditionCriteria` 的等价求值器（**唯一一处**）。</summary>
    public static class ConditionEval
    {
        // ==================================================================
        //  本工程用到的两个「字面量关键词」
        // ==================================================================
        // 这两个 trait 在原版是 `DefinedTrait` 数值（`0x136` = 310 invulnerable · `0x3d9` = 985
        // camouflage），在我们引擎里**登记在 `CardDef.KeywordTable.Prefixes`**（`CardDef.cs:5416-5418`）
        // 但**没有同名的常量** ⇒ 这里各给一个（⛔ 别在判断里散写裸字面量）。
        // ⚠️ `ward`(140) / `tainted`(160) 两个 trait **我们整条都没有**（Prefixes 没登记、机制也没有）
        //    ⇒ 依赖它们的 `330` 判不了（见下面的 `Kind.ModelGap`）。
        /// <summary>`invulnerable`（原版 `DefinedTrait.invulnerable = 310`；读点 `RuleCore.cs:6472/6610`）。</summary>
        public const string Invulnerable = "invulnerable";
        /// <summary>`camouflage`（原版 `DefinedTrait.camouflage = 985`；读点 `RuleCore.cs:5619`）。</summary>
        public const string Camouflage = "camouflage";

        /// <summary>这一格的「本版判得了吗」——给自检/报告用（⛔ 不参与求值）。</summary>
        public enum Kind
        {
            /// <summary>判得了（`Meets` 在主语/上下文齐全时返回真/假）。</summary>
            Ok = 0,
            /// <summary>**原版自己也没实现**：`MeetsConditionCriteria.c` 走 `default` 的
            /// `CustomDebug.LogError` + `bVar3 = 0`（`:354-361`）⇒ 本版**照抄 false** 并同样出声。</summary>
            OriginalUnhandled = 1,
            /// <summary>原版有实现，但**我们模型缺那一层数据/子系统** ⇒ `Meets` 返回 `null`（⛔ 不猜）。</summary>
            ModelGap = 2,
            /// <summary>不是 `UnitConditions` 的成员（坏数据）⇒ `null`。</summary>
            UnknownValue = 3,
        }

        /// <summary>这个数值在原版/我们这边的可判性。见 <see cref="Kind"/>。</summary>
        public static Kind KindOf(int cond)
        {
            switch (cond)
            {
                case 0: case 10: case 20: case 40: case 50:
                case 100: case 110: case 120: case 190:
                case 220: case 221: case 222: case 230:
                case 243: case 244: case 245:
                case 260: case 270: case 275: case 276: case 277:
                case 331: case 340: case 341: case 342:
                // 🆕 2026-10-23（`B-16` 残余）：241/242 判得了了 —— 数据侧是 v17 的
                //   `abilityTriggers` 列（`cardAbilities[].trigger` 全量集合）。
                case 241: case 242:
                    return Kind.Ok;
                case 240: case 250:
                case 280: case 320: case 330:
                    return Kind.ModelGap;
                case 290: case 300: case 310:
                    // 原版 switch 里**没有**这三格 ⇒ 落到 `:354-361` 的 LogError + `bVar3 = 0`。
                    return Kind.OriginalUnhandled;
                default:
                    return CardDef.IsLegalUnitCondition(cond) ? Kind.ModelGap : Kind.UnknownValue;
            }
        }

        /// <summary>`Kind != Ok` 时的原因（一句话 + 判据行号）；`Ok` ⇒ `null`。</summary>
        public static string GapOf(int cond)
        {
            // 🔴 `Ok` 一律 null（本方法是「**缺口**说明」，不是「实现说明」）——
            //    没有这一句，下面 `default` 会把判得了的值说成「本版没有求值分支」（假话）。
            if (KindOf(cond) == Kind.Ok) return null;
            switch (cond)
            {
                case 240:
                    return "240 hasSecretOrder：原版 `:281-285` → `EntityScript__HasSecretOrders`"
                         + "（数 `card+0x110` 的 activeEnchantments 里 `enchantmentCard.spellType == 60 SecretOrder`）。"
                         + "我们没建「附魔实例 → 卡 spellType」这一层。";
                // 241 / 242 **已判得了**（2026-10-23 · `B-16` 残余）：上面 `KindOf` 归 `Ok`，
                //   本方法对 `Ok` 一律返回 null ⇒ 这两格不再需要缺口说明（⛔ 别加回来）。
                case 250:
                    return "250 hasQuests：原版 `:323-327` → `EntityScript__HasQuests`"
                         + "（activeEnchantments 里 `spellType == 120 Quest`）。同 240：缺附魔 spellType 账。";
                case 280:
                    return "280 siege：原版 `:220-225` → `CardScript__DoesAbilityRequireLoading`"
                         + "（`hasActiveAbility(+0x220)` ∧ `HasRemovedTrait(490 reload)` ∧ `ActiveAbility` 的加载位）。"
                         + "我们没有「主动技能是否需装填」这一层。";
                case 320:
                    return "320 hasQuestCompleted：原版 `:227-248` = `!HasCurrentTrait(130 jam)` ∧ `raw._quest(+0x240)`"
                         + " ∧ `HasQuests(card) <= 0`。我们没有 `_quest` 列、也没有任务附魔那一层。";
                case 330:
                    return "330 stealthOrWardOrDuplicitous：原版 `:124-145` = `stealth(70) || ward(140)"
                         + " || (tainted(160) && !vanguard(980))`。`ward` / `tainted` 两个 trait 我们**整条都没有**"
                         + "（`CardDef.Prefixes` 没登记、也没有机制）⇒ 判成 true/false 都会静默错一半。";
                case 290: case 300: case 310:
                    return $"原版 `MeetsConditionCriteria.c` **没有** {cond} 这一格 ⇒ 它落到 `:354-361` 的"
                         + " `CustomDebug.LogError` ＋ `bVar3 = 0`。本版**照抄 false**（同样出声）。";
                default:
                    return CardDef.IsLegalUnitCondition(cond)
                         ? $"UnitConditions.{cond}：本版没有求值分支（缺判据 ⇒ 返回 null，⛔ 不猜）。"
                         : $"{cond} 不是 `UnitConditions` 的成员（坏数据）⇒ 返回 null。";
            }
        }

        static readonly HashSet<int> _originalUnhandledNotices = new HashSet<int>();

        // ==================================================================
        //  公开入口
        // ==================================================================

        /// <summary>判**场上一个单位**满不满足这一格条件（原版 `MeetsConditionCriteria(card, cond, exclude, thisCard)`）。
        /// 返回 `null` = 本版判不了（见 <see cref="KindOf"/> / <see cref="GapOf"/>）。
        /// <para>⚠️ `thisUnit` 只有 `110`（`protectedFromAttackDamage`）需要（原版拿它比
        /// `CurrentBastion`）；其余值传 `null` 不影响。</para></summary>
        public static bool? MeetsUnit(BattleContext ctx, UnitState u, int cond, bool exclude = false,
                                      UnitState thisUnit = null)
        {
            return Meets(ctx, u, u != null ? u.Card : null, -1, cond, exclude, thisUnit);
        }

        /// <summary>判**一张不在场的卡**（阵亡/弃牌堆里的那一份）满不满足这一格条件。
        /// 只对「卡本身」那一族（`220/221/222/270/275/276/277/340` 等）有意义；
        /// 依赖运行时状态的格子返回 `null`（⛔ 不拿卡面上的静态数字冒充当前状态）。</summary>
        public static bool? MeetsCard(BattleContext ctx, CardDef card, int owner, int cond, bool exclude = false)
        {
            return Meets(ctx, null, card, owner, cond, exclude, null);
        }

        /// <summary>核心入口。返回**未取反**的 match（`exclude` 在这里统一取反一次 —— 原版 `:384-389`）。</summary>
        /// <param name="subject">场上的单位；`null` = 主语是一张不在场的卡（此时看 <paramref name="card"/>）。</param>
        /// <param name="card">主语的卡定义（`subject` 非空时可省 —— 但两者都给最稳）。</param>
        /// <param name="owner">主语属于哪一方（`-1` = 不知道；只有 `260` 需要，不知道时现扫棋盘）。</param>
        /// <param name="thisUnit">原版形参 `thisCard`（只有 `110` 用）。</param>
        public static bool? Meets(BattleContext ctx, UnitState subject, CardDef card, int owner,
                                  int cond, bool exclude, UnitState thisUnit = null)
        {
            bool? match = Match(ctx, subject, card, owner, cond, thisUnit);
            if (match == null) return null;
            return exclude ? !match.Value : match.Value;      // 原版 `:384-389`
        }

        // ==================================================================
        //  逐值判定（顺序照原版 switch；行号 = `FilterMethods__MeetsConditionCriteria.c`）
        // ==================================================================

        static bool? Match(BattleContext ctx, UnitState u, CardDef card, int owner, int cond, UnitState thisUnit)
        {
            switch (cond)
            {
                // ---------------------------------------------------------------- 0 none
                // 原版 `:362-378`（`LAB_180934469`）：
                //   `armedTrap(+0x54) == 0` ⇒ 1；`cardState(+0x228) != 0` ⇒ 1；否则 LogWarning 后 0。
                // ⚠️ 我们**没有**「已布置的陷阱」这一层（`armedTrap` / `cardState` 都不在模型里）。
                //    但 `none` 在数据里**从不作为一行出现**（生成器丢弃 `cond == 0`，`CardDef.ConditionOf`
                //    返回 0 = 「这一格没有条件」）⇒ 走到这里的 `0` 只可能是「无条件」⇒ 恒真。
                case 0:
                    return true;

                // ---------------------------------------------------------------- 10 canAttack
                // 原版 `:101-117`：读 `ObscuredBool canAct`（`card+0x230`，`CardScript.cs` 的
                // `[CompilerGenerated] <canAct>k__BackingField`）。我们的 `UnitState.Exhausted`
                // 就是那一位（见 `UnitState.Exhausted` 的长注释：它就是 `canAct`）。
                case 10:
                    if (u == null) return null;
                    return !u.Exhausted;

                // ---------------------------------------------------------------- 20 mightAttack
                // 原版 `:82-86` → `CardScript__get_mightAct.c`（全文）：
                //   ① `attacksThisTurn(+0x48) != 0` 时：必须带 `bloodThirst(220)` 且 `== 1`，否则假；
                //   ② `RawCardScript.IsUnit(raw)`（`cardType` 0 Minion 或 10 Hero）；
                //   ③ `displaySummonSickness`（`+0x58 && !fast && !flank`）⇒ 假；
                //   ④ `HasCurrentTrait(100 stun)` / `(150 cantAttack)` / `(880 nonCombatant)` 任一 ⇒ 假；
                //   ⑤ 其余真。
                case 20:
                {
                    if (u == null) return null;
                    if (u.AttacksThisTurn != 0)
                    {
                        if (!u.Has(KeywordTable.BloodThirst)) return false;
                        if (u.AttacksThisTurn != 1) return false;
                    }
                    var c = card ?? u.Card;
                    if (!IsUnitOrHero(c)) return false;
                    if (u.SummonSickness && !u.Has("fast") && !u.Has("flank")) return false;   // displaySummonSickness
                    if (u.IsStunned) return false;                                             // stun(100)
                    if (u.Has(KeywordTable.CantAttack)) return false;                          // cantAttack(150)
                    if (u.Has(RuleCore.Noncombatant)) return false;                            // nonCombatant(880)
                    return true;
                }

                // ---------------------------------------------------------------- 40 damaged
                // 原版 `:87-98`：`GetCurrentDamage >= 1` ∧ **没有**「够回满的待处理治疗」
                // （`CardScript__EnoughPendingHealingToFull`）。我们没有「待处理治疗」这一档
                // ⇒ 等价于 `Health < MaxHealth`（与 `Data/AiTargets.MeetsCondition` 的 `case 40` 同口径）。
                case 40:
                    if (u == null) return null;
                    return u.Health < u.MaxHealth;

                // ---------------------------------------------------------------- 50 flying
                // 原版 `:38-45`：`(MeetsTraitCriteria(180 unstoppable) || MeetsTraitCriteria(390 flying))
                //   && !MeetsTraitCriteria(980 vanguard)`。
                // 三个 trait 在我们这边都是**一等关键词**：`RuleCore.Unstoppable` / `KeywordTable.Flying`
                // / `KeywordTable.Vanguard`（`CardDef.Prefixes:5107-5112` 起）。
                case 50:
                    if (u == null) return null;
                    return (u.Has(RuleCore.Unstoppable) || u.Has(KeywordTable.Flying))
                        && !u.Has(KeywordTable.Vanguard);

                // ---------------------------------------------------------------- 100 protectedFromDamage
                // 原版 `:46-54`：`MeetsTraitCriteriaFromList({dodge(240), invulnerable(310), dropPod(230)})`
                //   ⇒ 任一命中即真（`FilterMethods__MeetsTraitCriteriaFromList.c` 是「Any」）。
                case 100:
                    if (u == null) return null;
                    return u.Has(KeywordTable.Dodge) || u.Has(Invulnerable) || u.Has(KeywordTable.DropPod);

                // ---------------------------------------------------------------- 110 protectedFromAttackDamage
                // 原版 `:57-67`：先递归判 `100`；不成立再看
                //   `CardScript__HasBastionHigherThanAttack(card, thisCard)`：
                //   `thisCard.currentCombatAttack <= card.CurrentBastion`（`HasBastionHigherThanAttack.c:18-21`）。
                // 🔴 **主语别取反**（2026-10-23 修过一次，错因就是这一处）：`currentCombatAttack` 与
                //   dropPod / 残骸那两道守卫读的都是**施法者 `thisUnit`**；**只有** `CurrentBastion` 读候选 `u`。
                //   （`EntityScript__get_currentCombatAttack.c:22` 的 `0xe6 = dropPod`、`:36-38` 的残骸位
                //    都是 `param_1` 自己的位 —— 原版 `HasBastionHigherThanAttack.c:18-20` 把 `param_2`
                //    = `thisCard` 传进去。）
                // ⚠️ `thisCard` 拿不到（`thisUnit == null`）时我们返回 `null`（⛔ 不猜）。
                //   🔴 **这是已知的偏离**（铁律 5·b 记账，⛔ 别当原版口径）：原版
                //   `HasBastionHigherThanAttack.c:16-17,26` 在施法者为空时**明确 `return false`**
                //   ⇒ 110 退化成只看前半段的 `100`，**不是**「判不了」。改它要连**生产入口形参**
                //   （`RuleCore.MeetsUnitCondition` → `EffectResolver.cs:10106-10109` 现在 4 个形参、
                //   调用点 `:1776`/`:10216` 都拿不到施法者）与 `RuleEngineTest.cs:35137` 那条
                //   已钉成断言的 `null` 一起动 ⇒ 另立账；当前池 `unitConditions` 里 `cond=110` **0 行**，影响潜伏。
                case 110:
                {
                    bool? p100 = Match(ctx, u, card, owner, 100, null);
                    if (p100 == true) return true;
                    if (u == null || thisUnit == null) return null;
                    // `currentCombatAttack`（`EntityScript__get_currentCombatAttack.c`）——主语 = **施法者**：
                    // 带 dropPod(230) 时读空投舱攻击（我们**没有**「空投舱在飞」这一层 ⇒ 拿不到 ⇒ 不猜）；
                    // 残骸 ⇒ 0；其余按当前攻击型取近战/远程。
                    if (thisUnit.Has(KeywordTable.DropPod)) return null;
                    int combat = thisUnit.IsRemnant ? 0
                               : (thisUnit.CurrentAttackType == UnitState.AttackTypeRanged
                                  ? thisUnit.RangedAttack : thisUnit.Attack);
                    return combat <= u.Bastion;          // 原版：`CurrentBastion(card = 候选)`
                }

                // ---------------------------------------------------------------- 120 protectedFromDamageOrSurvivor
                // 原版 `:68-72` → `CardScript__IsProtectedFromDamageOrSurvivor.c`：
                //   `MeetsTraitCriteriaFromList({dodge(240), shield(80), invulnerable(310), dropPod(230), survivor(430)})`
                //   ⇒ 任一命中即真。注意它比 `100` **多 shield 与 survivor**（两者不是一个集合，⛔ 别互相顶替）。
                case 120:
                    if (u == null) return null;
                    return u.Has(KeywordTable.Dodge) || u.Has(KeywordTable.Shield)
                        || u.Has(Invulnerable) || u.Has(KeywordTable.DropPod) || u.Survivor > 0;

                // ---------------------------------------------------------------- 190 hasAbility
                // 原版 `:73-77`：读 `bool hasActiveAbility`（`card+0x220`，
                //   `CardScript.cs` 的 `<hasActiveAbility>k__BackingField`）。
                // 我们这边它 = 「这张卡解析出了 `Ability:` 那一条」⇒ `UnitState.HasAbility`（= `Ability != null`）。
                case 190:
                    if (u == null) return null;
                    return u.HasAbility;

                // ---------------------------------------------------------------- 220 dealtKillDmg（= 将死）
                // 原版 `:253-257` → `CardScript__EnoughPendingDamageToDie.c:30-45`：
                //   `cardState(+0x228) == 5(waitingToDie)` ⇒ 真；否则 `health > 0 || Survivor != 0`
                //   时看 pendingDamage（空 ⇒ 假），否则（血 ≤ 0 且无幸存者）⇒ 真。
                // 我们：已进「将死」队列 **或** `Health <= 0 && Survivor <= 0`（同一个判死闸）。
                // ⚠️ 枚举名 `dealtKillDmg` **名字 ≠ 语义**（同一条已在 `RuleCore.cs` 订正过）。
                case 220:
                    if (u == null) return null;
                    return (ctx != null && ctx.IsPendingDeath(u)) || RuleCore.EnoughPendingDamageToDie(u);

                // ---------------------------------------------------------------- 221 diedThisTurn
                // 原版 `:258-262` → `CardScript__HasDiedThisTurn.c:14-24`：
                //   `card.turnDied(+0x250) == bm.turnCounter(+0x3f8)`。
                // 我们：`BattleContext.DeadUnits`（`{Card, Owner, DeathTurn}`）—— 按 **(卡模板, 阵营, 死亡回合)** 记。
                // ⚠️ 边界：那张表按**卡模板**存 ⇒ 同名两张死在不同回合时分不出是哪一份（表自己的 doc 已记）；
                //    「死后又复活」那种边界同样表达不了。
                case 221:
                    if (u != null) return false;                       // 活着的单位：`turnDied` 不等于当前回合
                    return DiedInTurn(ctx, card, owner, 0);

                // ---------------------------------------------------------------- 222 diedSinceLastTurn
                // 原版 `:263-267` → `CardScript__HasDiedSinceLastTurn.c:26-47`：
                //   `turnDied == turnCounter` **或** `turnDied + 1 == turnCounter`（本回合或上一回合死的）。
                case 222:
                    if (u != null) return false;
                    return DiedInTurn(ctx, card, owner, 0) || DiedInTurn(ctx, card, owner, 1);

                // ---------------------------------------------------------------- 230 stealthAndNotDealtKillDmg
                // 原版 `:268-277`：`MeetsTraitCriteria(70 stealth)` ∧ `!EnoughPendingDamageToDie(card)`
                //   （不满足 stealth ⇒ 假，**不看**将死）。
                case 230:
                    if (u == null) return null;
                    return u.Has(KeywordTable.Stealth) && !RuleCore.EnoughPendingDamageToDie(u);

                // ---------------------------------------------------------------- 240/250（附魔账，缺）
                // 240 `:281-285` → `EntityScript__HasSecretOrders`（activeEnchantments 里 spellType == 60）；
                // 250 `:323-327` → `EntityScript__HasQuests`（同表 spellType == 120）。
                // 我们没建「附魔实例 → 卡的 spellType」这一层 ⇒ 判不了（⛔ 不猜）。
                case 240:
                case 250:
                    return null;

                // ---------------------------------------------------------------- 241/242（逐 ability 的 trigger）
                // 241 `:286-289` → `RawCardScript__HasAbilityType(raw, 600 UseSpiritStone)`；
                // 242 `:290-293` → 同法，605 UseFaith。实现（`RawCardScript__HasAbilityType.c:16-38`）
                // = 遍历 `rawCardData.cardAbilities`（`RawCardScript+0x290`）看 `CardAbility.trigger(+0x10)`
                //   是否等于参数。
                // 🆕 2026-10-23（`B-16` 残余）：v17 卡表的 `abilityTriggers` 列**就是**那个全量集合
                //   （`sorted(set(cardAbilities[].trigger))`，生成器 `ability_triggers_of`）⇒ 判得了。
                //   ⚠️ `AbilityTriggerPresence` 返回 `-1`（旧表/缺列）= 未知 ⇒ 仍旧 `null`（⛔ 不判假）。
                case 241:
                case 242:
                {
                    if (card == null) return null;
                    int presence = card.AbilityTriggerPresence(cond == 241 ? 600 : 605);
                    if (presence < 0) return null;
                    return presence == 1;
                }

                // ---------------------------------------------------------------- 243 isPraying
                // 原版 `:294-297`：读 `bool isPraying`（`card+0x67`）= 我们的 `UnitState.Prayed`。
                case 243:
                    if (u == null) return null;
                    return u.Prayed;

                // ---------------------------------------------------------------- 244 hasArmour
                // 原版 `:298-311`：读 `ObscuredInt currentArmourValue`（`card+0xb8`）`0 <` ⇒ 真。
                // 我们的唯一来源 = `UnitState.Armor`（唯一写法见 `KeywordTable.Armour` 的 doc）。
                case 244:
                    if (u == null) return null;
                    return u.Armor > 0;

                // ---------------------------------------------------------------- 245 hasAttackValues
                // 原版 `:312-322`：`CurrentMeleeAttack >= 1` **或** `CurrentRangeAttack > 0`。
                case 245:
                    if (u == null) return null;
                    return u.Attack > 0 || u.RangedAttack > 0;

                // ---------------------------------------------------------------- 260 costEqualToAvailableEnergy
                // 原版 `:329-342`：`EntityScript.CurrentCost == PlayerManager.GetUnusedMana()`。
                // 我们：`CardDef.Cost` vs `PlayerState.Energy`（后者就是「本回合还剩多少能量」）。
                // ⚠️ 已知边界：原版 `CurrentCost` 带**费用增减效果**，我们**没有在场费用修改层** ⇒
                //    两者只在「没有费用修改」时相同（`EffectResolver.CostOf` 那套只管手牌打出的那一步）。
                //    `owner` 不知道时现扫棋盘（`OwnerOf`）。
                case 260:
                {
                    if (u == null || card == null) return null;
                    int o = owner >= 0 ? owner : OwnerOf(ctx, u);
                    if (ctx == null || o < 0) return null;
                    return card.Cost == ctx.Players[o].Energy;
                }

                // ---------------------------------------------------------------- 270 isWarlordEquivalent
                // 原版 `:343-349` → `CardScript__IsWarlordEquivalent.c:8` →
                //   `GameStaticData__IsWarlordEquivalent.c:3-5`：**`rawCardData.cardType == 10(Hero)`**。
                // 我们：卡面 `type` 的 `"hero"`（`CardDatabase` 的映射，与 `cardType` 同源）。
                case 270:
                    return card != null ? card.Type == "hero" : (u != null ? u.IsWarlord : (bool?)null);

                // ---------------------------------------------------------------- 275 isWarlordOrVehicle
                // 原版 `:183-190`：`IsWarlordEquivalent(card)` **或** `RawCardScript.IsKeyword(raw, 10 Vehicle)`
                //   （`RawCardScript__IsKeyword.c:3-4` = `raw.keyword(+0x98) == param`）。
                case 275:
                    if (card == null) return null;
                    return card.Type == "hero" || SubtypeIs(card, "Vehicle");

                // ---------------------------------------------------------------- 276 isBattlesuitOrVehicle
                // 原版 `:191-203`：`IsKeyword(raw, 25 Battlesuit)` **或** `IsKeyword(raw, 10 Vehicle)`。
                case 276:
                    if (card == null) return null;
                    return SubtypeIs(card, "Battlesuit") || SubtypeIs(card, "Vehicle");

                // ---------------------------------------------------------------- 277 isInfrantryOrBeast
                // 原版 `:204-216`：`IsKeyword(raw, 5 Infantry)` **或** `IsKeyword(raw, 35 Beast)`。
                case 277:
                    if (card == null) return null;
                    return SubtypeIs(card, "Infantry") || SubtypeIs(card, "Beast");

                // ---------------------------------------------------------------- 280 siege（缺）
                // 原版 `:220-225` → `CardScript__DoesAbilityRequireLoading.c`：
                //   无 `hasActiveAbility` ⇒ 假；否则查 `HasRemovedTrait(0x1ea = 490 reload)`，
                //   再取主动技能卡的加载位（`+0x22`）。我们没有「主动技能是否需装填」这一层。
                case 280:
                    return null;

                // ---------------------------------------------------------------- 290/300/310（原版未实现）
                // 原版 switch 里**没有**这三格 ⇒ 落到 `:354-361`：`CustomDebug.LogError` + `bVar3 = 0`
                // ⇒ **照抄 false**（并且同样出声 —— 本工程红线是「不许静默」，不是「不许有 falsе」）。
                case 290:
                case 300:
                case 310:
                    NoteOriginalUnhandled(cond);
                    return false;

                // ---------------------------------------------------------------- 320 hasQuestCompleted（缺）
                // 原版 `:227-248`：`!HasCurrentTrait(130 jam)` ∧ `raw._quest(+0x240)` ∧ `HasQuests(card) <= 0`。
                // 我们既没有 `_quest` 列、也没有任务附魔计数 ⇒ 判不了。
                case 320:
                    return null;

                // ---------------------------------------------------------------- 330 stealthOrWardOrDuplicitous（缺）
                // 原版 `:124-145`：`MeetsTraitCriteriaFromList({stealth(70), ward(140)})`
                //   或 `MeetsTraitCriteria(160 tainted) && !MeetsTraitCriteria(980 vanguard)`。
                // `ward` / `tainted` 我们**整条都没有** ⇒ 判 true/false 都会静默错一半（⛔ 不猜）。
                case 330:
                    return null;

                // ---------------------------------------------------------------- 331 stealthOrCamouflage
                // 原版 `:146-156`：`MeetsTraitCriteriaFromList({stealth(70), camouflage(985)})`。
                case 331:
                    if (u == null) return null;
                    return u.Has(KeywordTable.Stealth) || u.Has(Camouflage);

                // ---------------------------------------------------------------- 340 onlyRemnants
                // 原版 `:157-169`：`isRemnant(+0x65) && !isLeavingRemnant(+0x66)` ∧ `IsNecronRemnant(card)`。
                // `SupportMethods__IsNecronRemnant.c:4-11` = `rawCardData.cardArmy(+0x2c) == 40(Sautekh)` **且** `+0x65`。
                // ⚠️ `+0x2c` 是 `ObtainableItem.cardArmy`（不是 `cardType`）—— `Sautekh = 40` 是**唯一**的太空死灵阵营。
                // ⚠️ 我们**没有** `isLeavingRemnant` 这一位（残骸离场是即时的）⇒ 少一个合取项，记在报告里。
                case 340:
                    if (u == null) return null;
                    return u.IsRemnant && card != null
                        && string.Equals(card.Faction, "Sautekh", System.StringComparison.Ordinal);

                // ---------------------------------------------------------------- 341 canUseDuty
                // 原版 `:170-174`：`bool usedActiveAbility(+0x4c) == 0`。
                // 我们：`UnitState.DutyUsed`（B-18 已核 `+0x4c` 就是它；`resetDuty` opcode 475 清的就是这一位）。
                case 341:
                    if (u == null) return null;
                    return !u.DutyUsed;

                // ---------------------------------------------------------------- 342 summonSickness
                // 原版 `:175-179`：读 `bool summonSickness(+0x58)` = 我们的 `UnitState.SummonSickness`。
                case 342:
                    if (u == null) return null;
                    return u.SummonSickness;

                default:
                    return null;              // 不是枚举成员 ⇒ 坏数据（⛔ 不猜）
            }
        }

        // ==================================================================
        //  小工具
        // ==================================================================

        /// <summary>原版 `RawCardScript.IsUnit`（`RawCardScript__IsUnit.c:5-11`）= `cardType` 0 Minion 或 10 Hero。</summary>
        static bool IsUnitOrHero(CardDef card)
        {
            if (card == null) return false;
            return card.Type == "unit" || card.Type == "hero";
        }

        /// <summary>卡面兵种（`RawCardScript.keyword` 在我们这边的字符串表达）。
        /// ⚠️ 真值是**单个** `Keyword` 枚举值；我们卡表的 `subtype` 是卡面那行橙字 ⇒ 逐个比。</summary>
        static bool SubtypeIs(CardDef card, string name)
        {
            return card != null && !string.IsNullOrEmpty(card.Subtype)
                && string.Equals(card.Subtype, name, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>`card` 在 `ctx` 里属于哪一方（按**引用**找那个单位）；找不到 ⇒ `-1`。</summary>
        static int OwnerOf(BattleContext ctx, UnitState u)
        {
            if (ctx == null || u == null) return -1;
            for (int p = 0; p < 2; p++)
            {
                if (p >= ctx.Players.Length || ctx.Players[p] == null) continue;
                var b = ctx.Players[p].Board;
                for (int s = 0; s < b.Length; s++)
                    if (ReferenceEquals(b[s], u)) return p;
            }
            return -1;
        }

        /// <summary>`DeadUnits` 里有没有「这张卡、这一方、死在 `turnsAgo` 个回合之前（0 = 本回合）」的一条。</summary>
        static bool DiedInTurn(BattleContext ctx, CardDef card, int owner, int turnsAgo)
        {
            if (ctx == null || card == null) return false;
            for (int i = 0; i < ctx.DeadUnits.Count; i++)
            {
                var d = ctx.DeadUnits[i];
                if (d == null || !ReferenceEquals(d.Card, card)) continue;
                if (owner >= 0 && d.Owner != owner) continue;
                if (d.DeathTurn == ctx.Turn - turnsAgo) return true;
            }
            return false;
        }

        /// <summary>原版对未实现的值会 `LogError` —— 本版照抄（同一个值只喊一次，⛔ 刷屏不是「不静默」）。</summary>
        static void NoteOriginalUnhandled(int cond)
        {
            if (!_originalUnhandledNotices.Add(cond)) return;
            UnityEngine.Debug.LogError(
                $"[RuleEngine] 结构化条件 `UnitConditions.{cond}` **原版自己也没实现**"
              + "（`FilterMethods__MeetsConditionCriteria.c` 落 default → `CustomDebug.LogError` + false）"
              + " ⇒ 本版同样判 **false**。");
        }
    }
}
