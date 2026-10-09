// UnitState.cs — 场上的一个单位（可变实例状态）
//
// 和 `CardDef`（不可变的卡牌定义）分开：同一张卡多次上场是两个独立的 UnitState，
// 掉血/疲劳/增益互不影响。
//
// 字段**对照**我们上一版 Godot 复刻（`d:/warpforge/scripts/rule_core.gd` 的 `_make_unit`）——
// ⚠️ 那份 `.gd` 是**我们自己**的复刻，**只作旁证**（「我们当时是怎么拍这些字段的」），
// **不是原版语义判据**。本文件下面凡引 `rule_core.gd:<行>` 的，一律按此口径读。
// 🔴 **判据顺序**（`CLAUDE.md` 铁律 2 的 2026-09-25 口径）：
//   ① 原版全量反编译方法体 `D:/2/tools/decomp_full/` → ② 解包资源字段 → ③ 成品卡图卡面文字；
//   粉丝实体规则书（《Warpforge Offline Rulebook》，非官方）与那份 `.gd` 都只作**第二来源/旁证**。
// ⚠️ **2026-10-18 更正**：本行原来写「字段语义**对齐** `rule_core.gd` 的 `_make_unit`」——
//   口径错了（把我们的复刻当成了基准）。见 `CLAUDE.md` 铁律 2 的 2026-09-18 更正。
using System.Collections.Generic;

namespace RuleEngine
{
    public class UnitState
    {
        /// <summary>
        /// **场上这一个单位是哪一份牌**（待办第 7 行「卡实例身份」第 1 步，2026-09-18）。
        ///
        /// 为什么要它：以前「手牌 → 场上」只搬 `CardDef`（**卡模板**），
        /// 于是单位死掉时 `Discard.Add(u.Card)` 放回去的是**模板**，那一份的实例态
        /// （本回合抽到的 / 临时标记 / 手牌加成 / 减费）**在这一跳断了**。
        /// 加一个引用就把这条链接上：手牌的那一份 → 场上的这个单位 → 弃牌堆里还是那一份。
        ///
        /// 🔴 **谁发号**：真对局一律 `BattleContext.NewInstance`（每局从 1 开始、同种子同序号）；
        ///    没有 `BattleContext` 的场合（测试 / 演示）走 `CardInstance.Detached`（负数 id）。
        /// ⚠️ **翻面成残骸（`Remnant`）时沿用同一个实例** —— 卡没换，只是翻了过来。
        /// </summary>
        public readonly CardInstance Instance;

        /// <summary>
        /// 它是哪张卡（**卡模板**，`= Instance.Card`）。
        /// 🔴 **2026-09-18 改成了属性转发**（原来是 `readonly CardDef Card` 字段）——
        ///    这样全工程约 **165 处** `u.Card` 读点**一个字都不用改**，
        ///    而棋盘这一侧先拿到实例身份。换手牌类型那一步（第 2 步）也就不必再回头动这里。
        /// </summary>
        public CardDef Card { get { return Instance.Card; } }

        public readonly string Name;
        public readonly bool IsWarlord;

        public int Attack;            // 当前近战攻击力
        public int RangedAttack;      // 当前远程攻击力
        public int Armor;             // 伤害减免（最低 1）。**只有 Armour X 关键词给护甲**
        public int Health;
        public int MaxHealth;

        public bool Exhausted;        // 本回合是否已行动（部署当回合 = true）

        /// <summary>
        /// **召唤病**（原版 `EntityScript.summonSickness`，字段偏移 🔴 **`+0x58`**）——
        /// 「这一张牌是**刚被重新放到场上**的」那一格。**它是 `canAct` 的输入，不是 `canAct` 本身。**
        ///
        /// 🔴 **为什么要单独一位**（`A1093`，2026-10-18）：原版 `+0x58` **有独立读点**，
        ///    而我们的 <see cref="Exhausted"/> 把它和「本回合已经行动过」**挤在一位里**
        ///    ⇒ 「不能动**因为刚上场**」与「不能动**因为已经动过**」在我们这侧**分不开**。
        ///    原版分得开（下面两条是现读的 `d:/2/tools/decomp_full/`）：
        ///      · `AI__ScoreFromGivingCharge.c:9-12`（与 `AI__ScoreFromCriteria.c:296-299` 同形）
        ///        —— **先** `CardScript.CanActNow`，**为假时**再读裸 `*(char *)(… + 0x58)`
        ///        ⇒ **两位一起用**（区分「不能动，因为召唤病」与「不能动，因为已经动过」）；
        ///      · `BattleManager__IsValidAttackTarget.c:200` —— 直接读 `*(char *)(param_2 + 0x58)`，
        ///        且**只**配 `HasCurrentTrait(param_2, 0x28 /*fast*/)`、**没有**侧翼那一项
        ///        ⇒ 它与 `displaySummonSickness` **不是同一个谓词**（后者要 `!fast && !flank`）。
        ///
        /// **判据（第一权威，现读 `d:/2/tools/decomp_full/`）**：
        ///   · 它就是这个字节：读 `EntityScript__get_summonSickness.c:5` ·
        ///     写 `EntityScript__set_summonSickness.c:5`；
        ///   · **写 1** 的**方法体**只有一处 —— `CardScript.ResetSummonSickness`
        ///     （`CardScript__ResetSummonSickness.c:8`，`*(undefined1 *)(param_1 + 0x58) = 1;`）；
        ///     **写 0** = 回合开始（`CardScript__OnTurnStart.c:85`）；
        ///   · 它怎么进 `canAct`：`CardScript__ActivateMinion.c:39-42` 把 `canAct` / `canAttack`
        ///     **一起**写成 `!displaySummonSickness`，而
        ///     `EntityScript__get_displaySummonSickness.c:7-11` = **`+0x58 && !fast(0x28) && !flank(0x1cc)`**
        ///     ⇒ **只有 `fast` / `flank` 免召唤病**（`ferocity` / `oath` 走**另一条**
        ///     `ActivateTraitsOnSummonOrEnchantment.c:86-99`，只抬 `canAct`）。
        ///
        /// 🔴 **谁写它（我们这侧）**：**唯一一处** = <see cref="RuleCore.ResetSummonSickness"/>，
        ///    由两个「**转移归属 ⇒ 重新入场**」的点各调一次（抢：`EffectResolver.DoTakeControl` ·
        ///    回合末归还：`RuleCore.EndTurn`）。**清它** = <see cref="RefreshForNewTurn"/>。
        ///
        /// ⚠️ **如实标着（没查实的那一半）**：**普通部署那一路我们没有找到「写 1」的原版读点** ——
        ///    它**不**在这两个写点里，本类也**不**在构造 / `ApplyDeployTurnState` 时置这一位
        ///    （⛔ 别把「照原版」贴到一个没查实的推断上）。细节与证据见
        ///    `资料/普查产出_第六会话/W_补0x58位_与A1071那一行.md` §⑤。
        /// </summary>
        public bool SummonSickness;

        /// <summary>
        /// **本回合不能攻击**（部署当回合 = true，带 `fast` / `flank` 的除外）——
        /// 原版 `EntityScript.canAttack`（**`+0x23C`**）那一位的对应物。
        ///
        /// 🔴 **为什么不能拿 <see cref="Exhausted"/> 顶替**：`Exhausted` 是 `canAct`（`+0x230`）
        /// 那一位，而 `ferocity` / `oath` 这两个 trait 在原版里是 **`canAct = true` 且
        /// `canAttack = false`** ⇒ 「**能动，但不能攻击**」。一位模型表达不了这种组合：把「不能攻击」
        /// 写成 `Exhausted` 会连**主动技能 / 替代行动 / 誓约**一起禁掉，与原版不符。
        ///
        /// **判据**（现读 `d:/2/tools/decomp_full/`，第一权威）：
        ///   · **写点** `CardScript__ActivateTraitsOnSummonOrEnchantment.c:86-99` ——
        ///     `else if (iVar1 == 0x4f1 || iVar1 == 0x4fb)`（`0x4f1` = 1265 = `ferocity`、
        ///     `0x4fb` = 1275 = `oath`，见 `Assembly-CSharp/DefinedTrait.cs:130/132`）：
        ///     `:91` 把 `+0x230` 写**真**、`:96` 的 `op_Implicit(local_30, 0, 0)` 把 `+0x23c`
        ///     写**假** —— 这两个 trait **逐字节同形**。
        ///     （同文件 `:47-51` 是 `0x28` fast / `0x1cc` flank 那一支：**只**写 `+0x230` = 真，
        ///     `+0x23c` 一动不动 ⇒ 那两个词**不影响**这一位。）
        ///   · **字段序** `Assembly-CSharp/CardScript.cs:1492/1494`（`canAct` → `canAttack`，
        ///     `ObscuredBool` = 12 字节 ⇒ `0x230` / `0x23c`）；读法见
        ///     `CardScript__CanAttackNow.c:57-74`（先读 `+0x230`、非假才再读 `+0x23c`，
        ///     两个都真才算能攻击）。
        ///   · **平时怎么算** `CardScript__ActivateMinion.c:39-42`：**两个位一起**写成同一个值，
        ///     那个值 = `!displaySummonSickness`；而 `EntityScript__get_displaySummonSickness.c` 是
        ///     `+0x58 && !HasCurrentTrait(0x28 fast) && !HasCurrentTrait(0x1cc flank)`
        ///     ⇒ **`canAttack` = 「本回合上的场，且不带 `fast` / `flank`」**
        ///     （`ferocity` / `oath` **不豁免攻击**，它们只豁免「能动」那一半）。
        ///     同一方法的 `:43-69` 是那两个 trait 的**第二条写点**：只把 `canAct` **重算**一遍
        ///     （新值 = `!HasCurrentTrait(100 /*stun*/)`），`canAttack` **一个字都不动**。
        ///
        /// **什么时候置位**：`RuleCore.ApplyDeployTurnState`（每个「新单位落地」入口各调一次）——
        /// 它是 `ActivateMinion.c:39-42` 与 `ActivateTraitsOnSummonOrEnchantment.c:86-99` 两条写点的和。
        /// **什么时候清**：<see cref="RefreshForNewTurn"/> 清回 `false`（= 原版 `+0x58` 在回合开始被清、
        /// `ActivateMinion` 随即把两位都写成真）。
        /// ⚠️ **它不表达「已行动」** —— 那一半照旧归 <see cref="Exhausted"/>，两处判据互不替代。
        /// </summary>
        public bool CannotAttackThisTurn;

        /// <summary>
        /// **正在眩晕**（`stun`）。
        ///
        /// 🔴 **2026-10-09（`A1116`）：它从【字段】改成【派生只读属性】—— 两个表示收成一处**
        /// （和 <see cref="HasShield"/> 同一条纪律、同一个成因）。
        ///
        /// **判据：原版只有一份表示**（`d:/2/tools/decomp_full/`，第一权威）—— `stun` 就是一个 trait：
        /// `DefinedTrait.stun = 100`（`d:/2/tools/il2cpp_out/dump.cs:45732`），原版**没有任何**
        /// 「眩晕布尔字段」：
        ///   · **施加**：`CardScript__Stun.c:48` `AddTraitSilently(param_1, 100, …)`
        ///     —— **全反编译里唯一**一处写 trait 100 的地方
        ///     （`grep -n "AddTraitSilently(param_1,100" *.c` 只此一条）；
        ///   · **读点**全走 `HasCurrentTrait(100)`：`CardScript__CheckStun.c:7` ·
        ///     `CardScript__ActivateMinion.c:23/:55` · `CardScript__get_mightAct.c:23` ·
        ///     `BattleManager__CanAttackCard.c:31` · `IsValidAttackTarget.c:215` ·
        ///     `AllowResolveAttack.c:237` · `AddScriptedAttack.c:49` · `AI__GetCardValueInPlay.c:42` ·
        ///     `CardScript__OnTurnStart.c:119` · `CardScript__OnTurnEnd.c:125`。
        ///   ⇒ 我们的四道 `ErrStunned`（= `RuleCore` 里那四处 `if (u.IsStunned) return RuleCodes.ErrStunned;`）
        ///     正好落在原版同一批位置上（`CanAttackCard` / `IsValidAttackTarget` / `AllowResolveAttack`）。
        ///
        /// ⚠️ **改之前这里是独立字段，与关键词两个方向都脱节**（本件修的就是这个）：
        ///   · `EffectResolver.DoStun` 与 `RuleCore` 的震荡（Concussion）**只写字段、从不挂关键词**
        ///     ⇒ 走 `Stun an enemy` 之后 `IsStunned == true` 而 `Has("stun") == false`
        ///     （`SimpleAI.TraitScore` 遍历的是 `_keywords` ⇒ AI 也少算一维）；
        ///   · 反向：<see cref="RemoveAll"/> 里**没有** `stun` 那一条 ⇒ `give X Stun` 的关键词被
        ///     `TempBuff` 到期 / `lose Stun` / 光环收回摘掉之后，`IsStunned` **仍为真**。
        ///
        /// ⛔ **别改回字段**、也别在别处补第二份同步 —— 那又变成「同一条规则两处写」。
        /// ⚠️ **别拿 <see cref="StunnedAtStartOfTurn"/> 顶它**：那是**另一个东西**
        ///    （「回合开始时就在这个状态」的闸门，原版 `+0x55`），语义完全不同。
        /// </summary>
        public bool IsStunned { get { return Has(KeywordTable.Stun); } }

        /// <summary>
        /// **职责已经用过了**（2026-09-13 A2）—— 规则书 `:181`「职责：**一次性能力**；
        /// 可由其他卡牌效果**装填**再次使用」。
        ///
        /// ⚠️ 它**不随回合重置**（和 <see cref="Exhausted"/> 正好相反）—— 这是本局一次的标记。
        /// 装填（`Reload the Duty abilities of all your units`）把它清回 false ——
        /// ✅ **2026-09-13 A4 已实现**（`EffectResolver.DoReloadDuty`）。
        /// </summary>
        public bool DutyUsed;

        /// <summary>
        /// **正在祈祷**（2026-09-13 A4 批 1）—— 执行过 `Pray` 替代行动的单位，**按回合重置**。
        ///
        /// 出处：我们上一版复刻 `rule_core.gd:2371`（`u["prayed"] = true`，在 `Pray` 那一点）·
        ///       `:1953`（`u["prayed"] = false`，和 `exhausted` / `attacks_turn` 同一批清）。
        /// ⚠️ 2026-10-18 更正：这里原来把这行标成「**规格书**」—— 那份 `.gd` 是我们自己的复刻、
        ///    只是**旁证**，不是规格/判据（见文件头）。
        /// 卡面两处：`Each friendly unit that is Praying heals 3`（`Devout Serenity`）·
        ///           `If any friendly unit is Praying, …`（`Sororitas Rhino`）。
        /// ⚠️ 它和 `When a friendly unit Prays` **不是一回事**：那个是**事件**（发生的那一下），
        ///    这个是**状态**（本回合一直挂着，回合开始才掉）。两张卡各要各的。
        /// </summary>
        public bool Prayed;

        /// <summary>
        /// **下一次用狂暴时不回牌库**（2026-09-14 A4 批 3）—— 卡面
        /// `The next time it uses Ferocity this turn, it stays in play`（`Bjorn's Shrine`，SpaceWolves）。
        ///
        /// 🔴 **语义出处（反编译里唯一有完整体的一条）**：`CardScript__UsedActiveAbility.c:52-64` ——
        ///    `has(ferocity)` → **广播** → `has(dontReturnFerocity)`（`DefinedTrait:131 = 1270`）
        ///    **或** `EnoughPendingDamageToDie` → 才跳过回牌库；否则 `AddRecallToDeck`。
        ///    `:75-88` 用完之后 `SendRemoveEffect(..., 1)` 把它**摘掉** ⇒ **一次性**。
        /// ⚠️ **「一次」和「本回合」两个修饰都要**（`Exhausted` 那套按回合清，表达不了「下一次」）：
        ///    消费点在 `RuleCore.UseAlternative` 的狂暴那一段（**用掉就清**），
        ///    兜底复位在 <see cref="RefreshForNewTurn"/>（「本回合」过了就没了）。
        /// ⚠️ 别和 `SW42 Bjorn the Fell-Handed` 的 `When a friendly unit uses Ferocity, it stays in play`
        ///    （**常驻、无 `next time`、无 `this turn`**）搞混 —— 那是**另一张卡、另一条语义**。
        /// </summary>
        public bool FerocityStay;

        /// <summary>
        /// **压在下面那几张牌**（虫群合并来的，2026-09-13 A2）。规则书 `:216`「置于其下」。
        /// 宿主进弃牌堆时它们**一起进**（`RuleCore.CleanupDeaths`）—— 物理上就是「压在下面」。
        /// </summary>
        /// 🔴 **2026-09-18 第 7 行第 2 步：元素类型从 `CardDef` 换成 `CardInstance`** ——
        ///    压在下面的**每一份**都要能分开（原来两张同名牌在下面是一个对象，宿主死了分不出谁是谁）。
        public readonly List<CardInstance> SwarmUnder = new List<CardInstance>();

        /// <summary>
        /// **这是一具残骸**（`Remnant`，2026-09-13 A2）—— 规则书 `:203`
        /// 「本部队死亡时**翻面**表示残骸；残骸**受伤害或控制者回合结束时被摧毁**」。
        ///
        /// 残骸是**留在格位上的一个单位**（原版在场上是一个独立的 3D 体：
        /// `BattleCardUI.CreateRemnantBody` / `RemnantBody3D`，`BattleManager.AddTransformIntoRemnant`），
        /// 所以：占着格位、攻 0、**1 点生命**（挨任何一下就没）、不能行动。
        /// ⚠️ 它是**背面朝上的牌**，没有任何能力 ⇒ 被摧毁时**不再触发**它自己的
        ///    `Backlash` / `Unstable`（那些在它「死」的那一次已经触发过了）。
        /// </summary>
        public bool IsRemnant;

        /// <summary>
        /// **面朝下**（`Ambush`，2026-09-13 A2）—— 规则书 `:166`「伏击：**面朝下打出**；
        /// **下次回合前**若被伤害：翻开**无效果**；若未被伤害：翻开**并触发效果**」。
        ///
        /// 两条出口（都在 `RuleCore` 里，判据各只一处）：
        ///   · 挨到**实际伤害** → <see cref="RuleCore.ApplyDamage"/> 里翻开，**不触发**；
        ///   · 撑到自己**下一个回合开始** → <see cref="RuleCore.RevealAmbush"/> 翻开并触发。
        ///
        /// ⚠️ **我们不做「藏起来」** —— 引擎里双方都看得见对方场上是什么（全工程没有隐藏信息这一层），
        ///    这里只实现**时机**那一半。表现层要盖张卡背是它自己的事。
        /// </summary>
        public bool FaceDown;

        /// <summary>
        /// **还带着护盾**（抵挡下一次伤害后失去）。
        ///
        /// 🔴 **2026-10-09（`A1106`）：它从【字段】改成【派生只读属性】—— 两个表示收成一处。**
        ///
        /// **判据：原版只有一份表示** —— 护盾就是一个 trait `0x50`
        /// （`DefinedTrait.shield = 80`，`d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs:9`），
        /// 原版**没有任何**「护盾布尔字段」：
        ///   · **免伤**：`CardScript__GetAdjustedDamage.c:45` `HasCurrentTrait(param_1, 0x50)`；
        ///   · **消耗**：`CardScript._ReceiveDamage_d__381__MoveNext.c:326-333`
        ///     —— `HasCurrentTrait(0x50)` → `RemoveBuffedTrait(0x50)` + `RemoveTrait(0x50)`，
        ///     **摘 trait、不写任何布尔字段**（`CardScript__RemoveTrait.c` 的实现是
        ///     `List.Find(id == param_2)` → `List.Remove`）；
        ///   · **对手 AI 也读它**：`AI__ScoreFromDamagingBuffedUnit.c:58-61`
        ///     `HasCurrentTrait(param,0xf0)/*(0x50)` 命中 ⇒ 走「只给一点点」那条出口。
        /// ⇒ 我们这边「关键词 `KeywordTable.Shield`」就是**唯一表示**，这一格**从它派生**。
        ///
        /// ⚠️ **改前它是个独立字段，与关键词会互相脱节**（本件修的就是这个）：
        ///   · 盾被用掉时 `RuleCore.ApplyDamage` 只写 `HasShield = false`、**从不摘关键词**
        ///     （全仓 `RemoveAll("shield")` 0 命中）⇒ `u.Has("shield")` 仍为真，而
        ///     `SimpleAI.ScoreDamaging`读的正是**关键词**
        ///     ⇒ **AI 把已经用掉盾的单位继续当带盾、只给一点点分**；
        ///   · 反向：`RemoveAll("shield")`（`lose Shield` / 光环收回 / `RemoveKeyword` 减到 0）
        ///     **不清**这个字段 ⇒ 关键词没了、那一刻**还能再挡一下**。
        ///
        /// ⛔ **别改回字段**、也别在别处补第二份同步 —— 那又变成「同一条规则两处写」。
        /// </summary>
        public bool HasShield { get { return Has(KeywordTable.Shield); } }

        /// <summary>
        /// 失明（规则书 :166「本单位失明期间**远程攻击设为 0**」）。
        ///
        /// ⚠️ 和 `stun` 一样，我们上一版复刻里是**独立的布尔状态字段**（`rule_core.gd:239` 的 `blind`），
        ///    不是关键词 —— 它靠 `_damage_unit` 那族置位（`:2814`）、`field_attack` 之外读它。
        ///    出处：那份 `.gd` 的 `:4212` `if is_ranged and bool(attacker.get("blind", false)): return ERR_NO_ATTACK`
        ///    （⚠️ 这些 `:NNNN` 全是**我们自己的复刻**的行号，**旁证、非原版**；
        ///     2026-10-18 更正：原来写作「原版是……」「出处：原版」）。
        ///
        /// 🔴 **2026-10-09（`A1116`）：它从【字段】改成【派生只读属性】—— 和 <see cref="IsStunned"/> 同批。**
        ///
        /// **判据：原版只有一份表示**（`d:/2/tools/decomp_full/`，第一权威）—— `blind` 也是一个 trait：
        /// `DefinedTrait.blind = 975`（`d:/2/tools/il2cpp_out/dump.cs:45816`，反编译里写作 `0x3cf`）：
        ///   · **施加**：`CardScript__AddEffect.c:475-487`（挂上 `0x3cf`）；
        ///   · **读点**：`EntityScript__get_CurrentRangeAttack.c:25-27`
        ///     （`HasCurrentTrait(param_1, 0x3cf)` ⇒ **`return 0`**）—— 正是
        ///     `RuleCore` 里那句 `if (ranged && u.IsBlind) return 0;`；
        ///   · **摘除**：`CardScript__AddEffect.c:692`（case 4 摘 trait）。
        ///
        /// ⚠️ **改之前这里是独立字段**：`DoBlind` 同时写字段和关键词，而 `RemoveAll` 只清这一个字段
        ///   ⇒ 「关键词在、字段不在」和「字段在、关键词不在」两个方向都出现过。
        ///
        /// ⛔ **别改回字段**。⚠️ **别拿 <see cref="BlindedAtStartOfTurn"/> 顶它**（那是闸门，见下）。
        /// ⚠️ <see cref="BlindTurnEnd"/> / <see cref="BlindOwner"/> **不是这一维** —— 那是「到期点」，
        ///    ⛔ 别顺手删（`RuleEngineTest` 在断言它）。
        /// </summary>
        public bool IsBlind { get { return Has(KeywordTable.Blind); } }

        // ---- 🆕 2026-10-09（`A1121`）「回合开始时就在这个状态」的两个闸门（原版 `+0x55` / `+0x56`）----
        //
        // 🔴 **它们不是「当前是否眩晕/失明」的第二个口** —— 上面那两个派生属性才是那个口。
        //    语义 = **「这个状态是在本单位自己的回合开始时就在的」**，只用来决定**回合末摘不摘**：
        //      · 置 1：`CardScript__OnTurnStart.c:119-126` —— `ActivateMinion` 之后，
        //        `HasCurrentTrait(100 /*stun*/)`（或 `0x3cf /*blind*/`）
        //        **且**「这一回合属于这张卡的拥有者」（`param_2 == *(char*)(card + 0x40)`）
        //        ⇒ `card[+0x55] = 1`（`:121`）/ `+0x56 = 1`（`:125`）；
        //      · 摘掉并清 0：`CardScript__OnTurnEnd.c:124-134`（`+0x55` 那一段清了，`+0x56` 那一段**没清**
        //        —— 原版的遗漏，我们这边由 <see cref="RemoveAll"/> 补上，见那里的注释）；
        //      · 施加时清 0：`CardScript__Stun.c:98`（stun）· `CardScript__AddEffect.c:487`（blind）；
        //      · 摘除时清 0：`CardScript__AddEffect.c:692`（**只有 blind**）；
        //      · 另有 `CardScript__ResetToValuesInHand.c:152` / `CardScript__ReactToUnitJammed.c:125`
        //        （`*(undefined2 *)(param_1 + 0x55) = 0` = **一次清两格**）：
        //        前者 = 卡回手时重置 ⇒ 我们这边**天然等价**（回手再上场是 `RuleCore.PlayCard` 里
        //        `new UnitState(...)` 新建对象，闸门跟着没了）；
        //        后者 = `jam`，**本工程全池 0 张卡带它、未建模**（见 `KeywordTable.Jam`）⇒ 无对应入口。
        //
        // ⇒ **效果 = 眩晕/失明只废掉「一个自己的回合」**（在别人回合里挨的那一下，
        //    撑到自己下个回合开始时置闸门 ⇒ 那一整个回合动不了 ⇒ 自己回合末摘掉）。
        //    没有这两个闸门的话，眩晕**永不解除**（`A1121` 记的那个真缺陷）。
        //
        // ⚠️ **照原版命名**（`EntityScript.stunnedAtStartOfTurn` / `blindedAtStartOfTurn`，
        //    `dump.cs:21985/:21987`，属性在 `:22046-22047`），⛔ 别起我们自己的名字。
        // ⚠️ 两者**都必须**是**独立字段**（不能从关键词派生）—— 它记的是**历史事实**
        //    （「回合开始时它在不在」），与「现在在不在」是两件事。

        /// <summary>「**本回合开始时就是晕的**」（原版 `EntityScript.stunnedAtStartOfTurn`，字段偏移 `+0x55`）。
        /// 只用来决定回合末摘不摘。见上面那一段的完整判据。</summary>
        public bool StunnedAtStartOfTurn;

        /// <summary>「**本回合开始时就是失明的**」（原版 `EntityScript.blindedAtStartOfTurn`，字段偏移 `+0x56`）。
        /// 只用来决定回合末摘不摘。见上面那一段的完整判据。</summary>
        public bool BlindedAtStartOfTurn;

        /// <summary>
        /// 🔴 **2026-10-19（`D26`）这一位【已删】** —— 它原来叫 `EcstasyFired`
        /// （`rule_core.gd:4447` 的 `_ecstasy_fired`，**旁证**）。
        ///
        /// **为什么删**：回到原版反编译核过之后，那个「一次性置位」是**错的** ——
        /// `decomp_full/CardScript__ShouldTriggerEcastasy.c` 的守卫里**没有任何置位**，
        /// 它判的是 **`traitValue(0x4e2) &lt; healthBefore` ∧ `healthAfter &lt;= traitValue(0x4e2)`**
        /// （= 「**从 &gt; X 掉到 ≤ X**」）⇒ 治回 X 以上再被打下去**会再触发**，
        /// 不是「一辈子一次」。判据全文与触发点 → `RuleCore.Hurt` 那一格的注释。
        /// ⛔ **别把这一位加回来**。
        /// </summary>

        /// <summary>
        /// 失明的**到期回合**（`blind_turn_end`，我们上一版复刻 `rule_core.gd:2815`；⚠️ **旁证，非原版**）。
        /// `-1` = 没有失明。语义是「到**施放者自己的下个回合开始**时清」——
        /// 也就是撑过对手的一整个回合（卡面写 `until your next turn`）。
        /// ⚠️ 那份 `.gd` 清除时读的是**另一个字段名**（`:1962` 读 `blind_turn`、`:2815` 写 `blind_turn_end`），
        ///    所以它这条清除**从来没生效过**（`blind` 一旦中上就永久）。我们按**卡面语义**实现，
        ///    不照搬这个笔误（⚠️ 2026-10-18 更正：原句把这些写成了「原版」）。
        /// </summary>
        public int BlindTurnEnd = -1;

        /// <summary>失明是谁施放的（0/1）—— 到期按**他的**回合算（见 <see cref="BlindTurnEnd"/>）。
        /// ⚠️ 不能靠「回合号的奇偶」推施放者：那种假设在本工程里没有依据（回合所有权是可变的）。</summary>
        public int BlindOwner = -1;

        public int AttacksThisTurn;   // 重置于回合开始

        /// <summary>🆕 2026-09-29：这个单位**上一次用的打法**（原版 `EntityScript.currentAttackType`）。
        /// 取值照原版 `AttackTypes`：**0 没打过 · 1 近战 · 2 远程 · 4 主动技能**。
        /// 用在哪：`AttackSelector` 里那圈黄圈 + 1.3 倍高亮**挂的就是它**
        /// （原版 `AttackTypesButtonsController.HighlightSelectedAttackTypeButtons`），
        /// 原来我们错挂在「指针悬停」上。
        /// ⚠️ **只用于表现** —— 我们的伤害是显式按 `ranged` 算的（不走 `currentAttackType` 那条），
        /// 所以这个字段**别拿去做规则判据**。
        /// 🔴 **2026-10-18（A938）更正**：本注释原来把这一格写成「原版 `EntityScript.currentAttackType`」——
        ///    **那是两个字段**：这一格是「**上一次用了哪一档**」（表现用，原版另有其源），
        ///    而 `currentAttackType` 是**卡上持久的当前攻击型**，落在下面的
        ///    <see cref="CurrentAttackType"/>。原版 `Attack` 读的是**后者**
        ///    （`AiScripted__ExecuteAction.c:1139` 的 `uVar18 = *(undefined4 *)(lVar12 + 0x120)`），
        ///    ⛔ 不是这一格。**错因**：2026-09-29 加这一格时只看了偏移名、没去读 `Attack` 那一段。</summary>
        public int LastAttackType;

        /// <summary>攻击型：近战（原版 `AttackTypes.Melee = 1`）。</summary>
        public const int AttackTypeMelee = 1;
        /// <summary>攻击型：远程（原版 `AttackTypes.Ranged = 2`）。</summary>
        public const int AttackTypeRanged = 2;
        /// <summary>攻击型：未定（原版 `EntityScript.__ctor` 的初值虽写 `1`，
        /// `AttackTypes` 枚举里 `0` 是「没选」—— 我们只在**强制档**上用不到它，留着对齐枚举）。
        /// 取值照 `AttackTypes`：`0 未定 · 1 近战 · 2 远程 · 4 主动技能`。</summary>
        public const int AttackTypeNone = 0;
        /// <summary>攻击型：**主动技能**（原版 `AttackTypes.Active = 4`）。
        /// 写点 = `CardScript__ActivateMinion.c:71-72`（召唤病 + `ferocity`/`oath` ⇒ 写 `4`）。</summary>
        public const int AttackTypeActive = 4;

        /// <summary>🆕 2026-10-18（A938）：**这张牌当前选定的攻击型**（原版
        /// `EntityScript.currentAttackType`，字段偏移 **`+0x120`**，`d:/2/tools/il2cpp_out/dump.cs` 的
        /// `EntityScript` 段）。取值照原版 `AttackTypes`：`0` 未定 · `1` 近战 · `2` 远程 · `4` 主动技能。
        ///
        /// 🔴 **为什么必须进引擎**（这一条是本轮最要紧的判据）：
        ///   原版 `AiScripted.ExecuteAction` 的 `Attack(30)` / `AttackFreeMode(31)` 那一支把
        ///   **`attackType = *(actingCard + 0x120)`** 直接喂给 `AddAttackAction`
        ///   （`AiScripted__ExecuteAction.c:1139-1143`），而 `ChangeToRanged(35)` / `ChangeToMelee(36)`
        ///   那一支调 `CardScript.ChangeAttackType(card, 2|1, true)`
        ///   （`AiScripted__ExecuteAction.c:1201,:1249` → `CardScript__ChangeAttackType.c:15` **写这一格**）
        ///   ⇒ **`ChangeTo*` 是 `Attack` 的前置**，不是可选演出。
        ///   ⚠️ 我们的伤害是**显式传 `ranged`** 算的（`RuleCore.DeclareAttack`）—— 现在补上这条链：
        ///   `ChangeTo*` 写这一格、`RuleCore.DeclareAttackByCurrentType` 读它（**唯一**读点）。
        ///
        /// 🔴 **`+0x120` 的全部写点（2026-10-18 `W5` 逐条扫 `d:/2/tools/decomp_full/`
        ///   `grep -n "+ 0x120" CardScript__*.c EntityScript__*.c` 得 10 条写入语句 / 9 个方法）**：
        ///
        /// | # | 原版写点 | 写什么 | 我们落在哪 |
        /// |---|---|---|---|
        /// | 1 | `EntityScript__.ctor.c:5` | `1` | 本构造器（初值随后被 #2 覆盖） |
        /// | 2 | `CardScript__CardSetup.c:263-267` | `(近战 &lt; 远程) + 1` | 本构造器 —— `RuleCore.ChooseAttackTypeAutomatically`（**判据只此一份**） |
        /// | 3 | `CardScript__ChangeAttackType.c:14-15` | 形参 | `RuleCore.SetCurrentAttackType`（教程 `ChangeTo*`） |
        /// | 4 | `CardScript__ChooseAttackTypeAutomatically.c:11-12` | `(近战 &lt; 远程) + 1` | `RuleCore.RecomputeCurrentAttackType`（同一条算式，公开入口） |
        /// | 5 | `CardScript__AddEffect.c:479-480` | **`1`**（挂上 `blind`/975 时） | 本类 `AddKeyword`（`blind` 分支） |
        /// | 6 | `CardScript__AddEffect.c:625-626` | **`2`**（挂上 `pindown`/970 时） | 本类 `AddKeyword`（`pindown` 分支） |
        /// | 7 | `CardScript__ActivateMinion.c:70-72` | **`4`**（召唤病 **且** 带 `ferocity`/`oath` 时） | `RuleCore.PlayCard` 的部署收尾（`RuleCore.ForceAttackTypeOnDeploy`） |
        /// | 8 | `CardScript__OnTurnEnd.c:214-218` | `(近战 &lt; 远程) + 1`（带 `oath` 时） | `RuleCore.EndTurn` |
        /// | 9 | `CardScript__ResolveActiveAbilityPlayed.c:35-42` | `(近战 &lt; 远程) + 1`（带 `duty`/`ferocity`/`oath` 时） | `RuleCore.UseAbility` / `UseOathAbility` / `UseAlternative` |
        /// | 10 | `CardScript__UpdateAttackText.c:21-29` | `(近战 &lt; 远程) + 1`（**仅当 `+0x40 == 0`**） | **数值一变就重挑**：`RuleCore.RecomputeCurrentAttackTypes`（跟着 `Auras.Recompose` / `ApplyOneGain` 走） |
        ///
        /// ⚠️ **#10 的 `+0x40` 守卫我们【不复刻】，如实标着**：那一格是 `CardScript.CardSetup` 的第 3 个实参
        ///   `isPlayer`（`CardScript.cs:1573` 的签名 `CardSetup(BattleManager mgr, bool isPlayer, …)`）
        ///   ⇒ 原版**只对「不是本机玩家那一侧」的卡**自动重挑，**本机玩家手选的那一档不被覆盖**
        ///   （本机玩家靠 `UnitOnBoardAttackTypeSelector.AttackButtonClick` 显式改）。
        ///   我们的 `Core/` **没有「本机 / 对手」这个概念**（对局两侧是对称的，
        ///   `RuleCore` 不许引表现层），而且我们**没有**「手选攻击型」那个 UI 入口
        ///   （`SetCurrentAttackType` 的唯一调用点是教程脚本）⇒ 复刻这个守卫 **既做不到也没有对象**。
        ///   ⛔ 别写成「我们做了 `+0x40` 守卫」。
        ///
        /// **谁读**：`RuleCore.DeclareAttackByCurrentType`（教程 `Attack` 那一档）。
        /// </summary>
        public int CurrentAttackType;

        /// <summary>🆕 2026-09-16 **「某个机制的触发再发生 N 次」的额度**（按关键词分开记）。
        ///
        /// 卡面只有两句在用（都是**监听别的单位**的卡写在事件层里的）：
        ///   · `When a friendly unit triggers Mob, it triggers an additional time`（`GOF_Big_Choppa_Nob`）
        ///   · `When this unit triggers Synapse, it applies the effect twice`（`TL30 Broodlord`）
        /// 语义：**监听者**在广播里给**事件主语**（也就是那个正在触发机制的单位）挂一份额度，
        /// 机制自己跑完之后**就地消费**（`RuleCore.TakeExtraTrigger`）——
        /// 所以「再触发一次」是**这一次**的事，不跨时机、不跨回合。
        ///
        /// ⚠️ **必须是「就地消费」**：`RuleCore.DeclareAttack` 的 Mob 那一段与
        ///    `EffectResolver.RepeatTacticOnAdjacent` 的 Synapse 那一段各自消费自己那一份。
        ///    写成一个共享的 bool 就会串机制（Mob 的额度被 Synapse 吃掉）。
        /// ⚠️ `RefreshForNewTurn` 里**清空**只是兜底（正常路径用完就摘了）——
        ///    留着会让「上一次没消费掉的额度」在下一回合突然生效，那比没有更糟。
        /// </summary>
        public readonly Dictionary<string, int> ExtraTriggers = new Dictionary<string, int>();

        /// <summary>🆕 2026-09-16 **本回合激活过几次誓约（Oath）能力** —— 重置于回合开始。
        ///
        /// 和 `AttacksThisTurn` 分开：誓约是**独立的一次激活**，**不占单位那次行动**
        /// （原版 `CanUseOathAbility` 与「本回合已行动」是两套判据，扣的也是别的计数器：
        ///  `CardScript` 的 `+0x50` 每次激活 ++、`+0x4c` 置 1，回合末清零 ——
        ///  `decomp_out/CardScript__ResolveActiveAbilityPlayed.c:31-32` +
        ///  `CardScript__OnTurnEnd.c:209`）。
        /// 上限默认 1；场上有 `oathTripleActivation` 的友方卡时是 3
        /// （`CardScript__CanUseOathAbility.c:16-20`）。
        /// </summary>
        public int OathUsesThisTurn;

        /// <summary>🆕 2026-09-29 **本回合激活过主动能力**（含技能 / 誓约 / 替代行动）—— 重置于回合开始。
        ///
        /// 原版出处：`EntityScript.usedActiveAbility`（**字段偏移 `+0x4C`**，
        /// `d:/2/tools/il2cpp_out/dump.cs` 的 `EntityScript` 段：`0x4C usedActiveAbility`、
        /// `0x50 usedActiveAbilityTimesPerTurn`）。
        /// 写入点 = `CardScript__ResolveActiveAbilityPlayed.c:31-32`（`+0x50` 每次 ++、`+0x4c` 置 1），
        /// 清零 = `CardScript__OnTurnEnd.c:209` —— 与上面 `OathUsesThisTurn` **同一处**、同一条注释链。
        ///
        /// 🔴 **谁在读它（这一位存在的唯一理由）**：`CardTraitDuty.IsActive(card)` ——
        ///    `CardTraitDuty__IsActive.c:19` 就是 `return *(char *)(param_2 + 0x4c) == 0;`
        ///    ⇒ **单位本回合用过主动能力之后，`Duty` 那个徽标要画成「未激活」（灰化）**。
        ///    `CardTraitOath.IsActive` 是另一条判据（`CardScript.CanUseOathAbility`），见 `Badges.For`。
        /// ⚠️ **别拿 `Exhausted` 顶替** —— 攻击也会置 `Exhausted`，而原版这里只认「主动能力」。
        /// </summary>
        public bool UsedActiveAbilityThisTurn;

        /// <summary>🆕 2026-09-16 **这张牌上场的回合号**（`ctx.Turn`；没上场过 = -1）。
        ///
        /// 用途：原版誓约能力的默认限制是「**本回合部署的才能激活**」
        /// （`CardScript__IsTheSameTurnPlayed.c:24-38`），
        /// 由 `oathInAllTurns` 豁免（`CardScript__CanUseOathAbility.c:8`）。
        /// 全仓原来**没有任何「部署回合」字段**，这是唯一一处写点（`RuleCore.PlayCard` / `DeployFree`）。
        /// </summary>
        public int DeployedTurn = -1;

        readonly Dictionary<string, int> _keywords;

        /// <summary>
        /// **真对局用的构造**：把手上的 / 弃牌堆里的**那一份**放上场。
        /// 实例由 <see cref="BattleContext.NewInstance"/> 发（新造一张）或从来源区域**沿用**（挪一份）。
        /// </summary>
        public UnitState(CardInstance instance, bool isWarlord)
        {
            Instance = instance;
            var card = instance != null ? instance.Card : null;
            Name = card != null ? card.Name : "?";
            IsWarlord = isWarlord;

            Attack = card != null ? card.Attack : 0;
            RangedAttack = card != null ? card.RangedAttack : 0;
            Health = card != null ? card.Health : 0;
            MaxHealth = Health;

            _keywords = new Dictionary<string, int>();
            if (card != null)
            {
                foreach (var kv in card.Keywords) _keywords[kv.Key] = kv.Value;
            }
            // 护甲只有 Armour X 关键词这一个来源。
            // ⚠️ JSON 里的 `armor` 字段经核实实为**远程攻击**（OCR 误读），已在数据层迁移到 RangedAttack
            Armor = KwValue(KeywordTable.Armour);

            // 部署当回合不可行动 —— 除非带**迅捷 / 侧翼 / 狂暴**。
            // 规则书 :98「部署当回合不能行动（除非注明，如迅捷/侧翼/狂暴）」、
            // :187「侧翼：打出当回合可攻击任意敌方部队」；我们上一版复刻 `rule_core.gd:2248`
            // 把这两个写在同一句里（`fast` / `flank` → `exhausted = false`）（⚠️ **旁证，非原版**）。
            // ⚠️ **狂暴（`ferocity`）是 2026-09-13 A2 补进来的** —— `:98` 那句话里
            //    本来就点着它（「如迅捷/侧翼/狂暴」），那份 `.gd` 的同一处也把它和 fast/flank 并列，
            //    只是我们先前没实现这个关键词。慢的 `pray` **不在**这一行（`:198`）。
            //
            // 🔴 **2026-10-17（F7）：三个词的表**不在这儿** —— 判据收在 `RuleCore.HasDeployExemption` 一处**
            //    （原来这里是内联的三词表，是**同一条规则的第二份写法**，改一处漏一处）。
            //    ⚠️ 此处能直接调它：`_keywords` 在上面那个 `foreach` 里**已经填好**，
            //       而同在 `RuleEngine` 命名空间下有单一程序集（无 asmdef）可直达。
            //    ⚠️ `Exhausted` 仍然是**构造时的快照** —— 之后才挂上来的关键词
            //       （手牌加成 / 光环）照旧由 `RuleCore.PlayCard` 那一次重算兜住。
            Exhausted = !RuleCore.HasDeployExemption(this);
            // 🔴 **2026-10-09（`A1106`）：这里原来还有一句 `HasShield = Has(KeywordTable.Shield);`** ——
            //    `HasShield` 改成**派生只读属性**之后（见它的注释），那句就是同一个判据的第二次求值、
            //    而且**只能写字段不能写属性**（编译不过）。删掉它**不漏任何东西**：
            //    `_keywords` 就在上面那个 `foreach` 里填好，属性直接读它。⛔ 别再加回来。

            // 🆕 2026-10-18（A938）：**卡上当前的攻击型**（原版 `EntityScript.currentAttackType`，`+0x120`）。
            //   判据 = `EntityScript__.ctor.c:5` 初值写 `1`、`CardScript__CardSetup.c:263-267` 与
            //   `CardScript__ChooseAttackTypeAutomatically.c:10-12` 都是 `(近战 < 远程) + 1`
            //   ⇒ **远程更高才取远程，平手取近战**。这与 `SimpleAI.UseRanged`（`Data/`，
            //   我们那处唯一的「近战还是远程」判据）**逐个局面等价** —— 此处是构造期快照，
            //   ⛔ **不在这里调它**（`Core/` 不许引 `Data/`，离屏探针只编 `Core/*.cs`，见 `TutorialScript.cs` 类头）。
            // 🔴 **2026-10-18（`W5` · K3）：算式收进 `RuleCore.ChooseAttackTypeAutomatically` 一处** ——
            //   本处原来自己写了一遍（那是同一条规则的**第 3 份写法**：`SimpleAI.UseRanged` /
            //   `BattleDriver.UseRanged` / 这里；`RuleCore` 与 `UnitState` 同程序集，直接调得到）。
            CurrentAttackType = RuleCore.ChooseAttackTypeAutomatically(this);
        }

        /// <summary>
        /// ⚠️ **只给没有 `BattleContext` 的场合用**（测试自己拼棋盘 / 演示场景塞探针 / 卡池预览）：
        /// 发一个**分离实例**（<see cref="CardInstance.Detached"/>，负数 id）——
        /// 它**没有**「手牌 → 场上 → 弃牌堆」的连续身份，只是一个够用的身份位。
        /// **真对局里的部署一律走上面那个收 `CardInstance` 的重载**（实例来自手牌/牌库/弃牌堆）。
        /// </summary>
        public UnitState(CardDef card, bool isWarlord) : this(CardInstance.Detached(card), isWarlord) { }

        public bool Has(string keyword) { return _keywords.ContainsKey(keyword); }

        /// <summary>
        /// 这个单位**当前**身上有哪些关键词（含加/减益、光环给的、限时增益）—— 只读。
        ///
        /// ⚠️ **别拿它当「卡面印的关键词」用**：那是 `Card.Keywords`（卡模板上的、不变的）。
        /// 表现层要画「场上这张卡现在挂了什么 buff/debuff」时用的**是这一个**
        /// （原版 `BattleCardUI.UpdateTraitIcons` 走 `EntityScript.GetCurrentTraitValueWithModifiers`）。
        /// ⚠️ 枚举顺序**不稳定**（`Dictionary`）—— 要稳定的显示顺序得自己排，别依赖它。
        /// </summary>
        public IReadOnlyDictionary<string, int> Keywords { get { return _keywords; } }

        /// <summary>场上单位的**主动技能**（来自卡的 `Ability:`）。没有就是 null</summary>
        public EffectSpec Ability { get { return Card != null ? Card.Ability : null; } }

        public bool HasAbility { get { return Ability != null; } }

        /// <summary>
        /// 这个关键词对应的**触发效果**（**封闭文法**那条：`Card.Effect`，我们自己设计的那 26 张卡）。
        /// 没有返回 null —— 调用方必须判。
        /// ⚠️ **原版卡那一族不走这里**：它们的正文是 `Card.TriggerOps` / <see cref="FxOps"/> 的 op。
        /// </summary>
        public EffectSpec Effect(string keyword)
        {
            if (string.IsNullOrEmpty(keyword) || Card == null) return null;
            return Card.Effect(keyword);
        }

        // ---- 运行时补上的「触发正文」（`Give "💀 Backlash: …" to a friendly troop`）----
        //
        // 原版把这种段落当**卡片自带的一段效果文字**存着，靠 `desc.contains(...)` 现搜现解；
        // 我们没有卡片定义可写（那是不可变的 `CardDef`），所以在单位身上挂一份。
        // 判据：**卡上原生的那条优先**（那是这张卡自己的设计），没有才用后挂上去的。
        //
        // 🔴 **2026-09-14 改**：原来这里存的是 `EffectSpec`（**封闭文法** —— 那是给
        //    我们**自己设计的 26 张卡**写的极小文法，只认 `Damage/Heal/Draw`）。
        //    而挂上来的正文是**原版卡面原文**（`Return to your hand` / `Trigger this troop's
        //    Codex ability` / `Lower the cost of a random troop in hand by 1` …）—— 一条都解不了。
        //    表现：那一族（全池 **10 张**）**解析得出、有机制、但永远不会发生**，
        //    而且 `GivePayload.Mechanized` 报的「嵌入的效果文字解析不了」**报表看得见、玩家看不见**。
        //    ⇒ 改成存 **`EffectText.Parse` 出来的 op** —— 和卡上原生正文**同一台解析器、同一条
        //    结算路径**（`RuleCore.FireTriggerAt` 里那条 `ResolveOps`）。判据只有一份。
        readonly Dictionary<string, List<EffectOp>> _grantedOps = new Dictionary<string, List<EffectOp>>();
        /// <summary>挂上去的那份**正文原文**（日志与卡面要印它）</summary>
        readonly Dictionary<string, string> _grantedText = new Dictionary<string, string>();

        public void GrantOps(string keyword, List<EffectOp> ops, string text)
        {
            if (string.IsNullOrEmpty(keyword) || ops == null || ops.Count == 0) return;
            _grantedOps[keyword] = ops;
            _grantedText[keyword] = text ?? keyword;
        }

        public bool HasGrantedEffect(string keyword)
        {
            return !string.IsNullOrEmpty(keyword) && _grantedOps.ContainsKey(keyword);
        }

        /// <summary>挂上去的那份正文原文。没挂过返回 null。</summary>
        public string GrantedText(string keyword)
        {
            string s;
            return (keyword != null && _grantedText.TryGetValue(keyword, out s)) ? s : null;
        }

        /// <summary>
        /// 这个关键词**能结算的正文**（按 `FireTriggerAt` 的取法）：卡上原生的优先，
        /// 没有就用运行时挂上去的那份。两处都没有 → null。
        /// 🔴 **全仓只此一处判据** —— 原来这条「原生 or 挂的」被散在 5 个地方各写一遍。
        /// </summary>
        public IReadOnlyList<EffectOp> FxOps(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return null;
            var own = Card != null ? Card.TriggerOps(keyword) : null;
            if (own != null) return own;
            List<EffectOp> g;
            return _grantedOps.TryGetValue(keyword, out g) ? g : null;
        }

        /// <summary>这个关键词**有没有任何**可结算的东西（原生正文 / 挂上去的正文 / 封闭文法那条）。
        /// 「触发了」和「触发了但那条关键词压根没有正文」必须分得开（红线）。</summary>
        public bool HasFx(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return false;
            if (Card != null && (Card.TriggerOps(keyword) != null || Card.Effect(keyword) != null)) return true;
            return _grantedOps.ContainsKey(keyword);
        }

        /// <summary>
        /// 挂一份**光环给的**触发正文（`Slay: Gain Blood Thirst this turn`，A7）。
        /// 和 <see cref="GrantOps"/> 只差一件事：**记进光环那本账**，好让重算时精确收回。
        /// ⚠️ 收回时只把**光环挂的**那一份摘掉 ——
        ///    别的效果（`Give "💀 Backlash: …"`）挂的不能被连带清掉。
        /// </summary>
        public void GrantAuraOps(string keyword, List<EffectOp> ops, string text)
        {
            if (string.IsNullOrEmpty(keyword) || ops == null || ops.Count == 0) return;
            _grantedOps[keyword] = ops;
            _grantedText[keyword] = text ?? keyword;
            _auraFx.Add(keyword);
        }

        readonly HashSet<string> _auraFx = new HashSet<string>();

        /// <summary>带了这个关键词、且它带的效果文字解析不出来 → 卡面要标 `*`</summary>
        public bool EffectUnparsed(string keyword)
        {
            if (Card == null || !Card.Has(keyword)) return HasGrantedEffect(keyword) == false;
            if (keyword == KeywordTable.Ability) return Card.HasAbility == false;
            return Card.Effect(keyword) == null;
        }

        public int KwValue(string keyword)
        {
            int v;
            return _keywords.TryGetValue(keyword, out v) ? v : 0;
        }

        /// <summary>关键词授予/叠加（我们上一版复刻 `rule_core._apply_gain:3292`：`kws[name] += val`；⚠️ **旁证**）。
        /// ⚠️ 有副作用的那几个关键词走 <see cref="SyncKeywordState"/>（`armour` 要连着 `Armor`、
        ///    `blind`/`pindown` 要连着**攻击型**那一格）。
        ///    🔴 **2026-10-09（`A1116`）就地订正**：这一句原来写「`armour`/`stun` **两个**还要同步状态字段」——
        ///    `stun`/`blind` 那两个状态字段**已经删掉了**（`IsStunned`/`IsBlind` 改成关键词的派生属性）。
        /// 🔴 **2026-10-18（`W5`）**：那一串同步搬进 <see cref="SyncKeywordState"/> ——
        /// 它原来与 `AddAuraKeyword` 各写一份（光环那份漏了 `blind` 与攻击型两条）。
        /// 🔴 **2026-10-09（`A1106`）**：原来这里是**三个**（多一个 `shield`）—— `shield` 已从
        ///    `SyncKeywordState` 里**删掉**：`HasShield` 改成读关键词的**派生属性**，不需要也不许
        ///    再有第二处同步（见 <see cref="HasShield"/> 的注释）。</summary>
        public void AddKeyword(string keyword, int value)
        {
            if (string.IsNullOrEmpty(keyword)) return;
            _keywords[keyword] = KwValue(keyword) + value;
            SyncKeywordState(keyword, value);
        }

        /// <summary>
        /// **「关键词被挂上」的副作用**（引擎别处按**字段**结算，不按 `_keywords`）——
        /// 判据只此一份，`AddKeyword` 与 <see cref="AddAuraKeyword"/> 共用。
        ///   · `armour` —— 见方法尾那一句（**加减都要**，所以不在 `value &gt; 0` 里）；
        ///   · `blind` —— **攻击型**那一格（见下面 `K3` 那两条）。**2026-09-14 A6 族 C 补**：
        ///     当年缺了它会让 `give them Blind` 「给了关键词却什么都没发生」= 典型静默失效。
        ///   · 🔴 **2026-10-09（`A1116`）**：原来这一栏还有 `stun` 与 `blind` 两条
        ///     「同步状态字段（`IsStunned` / `IsBlind`）」—— **两条都删掉了**，理由见那两个属性的
        ///     注释（改成关键词的派生属性 ⇒ 不需要、也不许再有第二处同步）。
        ///     ⛔ **别再往这里加 `if (keyword == …Stun) …` 这种行**：那正是当初两个表示互相脱节的
        ///     成因（`DoStun` 走字段、`GiveKw` 走关键词，各写一半，两个方向都漏）。
        ///   · 🆕 **`K3` 的攻击型那两条**（原版 `CardScript.AddEffect` 里那两处）：
        ///     `blind` ⇒ `+0x120 = 1`（`CardScript__AddEffect.c:479-480`，trait `0x3cf = 975`）·
        ///     `pindown` ⇒ `+0x120 = 2`（`CardScript__AddEffect.c:625-626`，trait `0x3ca = 970`；
        ///     `pindown` 只禁近战，见 `RuleCore.CanAttackNow` 的 `ErrPindown`）。
        ///     ⚠️ 原版**只在写值不同时才写**（`if (… + 0x120) != 1`）—— 那只是省一次 UI 刷新
        ///     （`BasicCardUI.AttackTypeChanged`），**写进去的值一样** ⇒ 我们直接赋值，等价。
        ///     ⚠️ **摘掉时不回挑**（原版那两条是**单向**的：`RemoveTraitSilently` 不碰这一格）——
        ///     之后由 `RuleCore.ChooseAttackTypeAutomatically` 那条链重挑。
        ///
        /// 🔴 **2026-10-09（`A1106`）：`shield` 那一条已经从本方法删掉** —— `HasShield` 现在是
        ///    `Has(KeywordTable.Shield)` 的**派生只读属性**（见它的注释），而 `_keywords`
        ///    在调本方法**之前**就已经写好 ⇒ 它自动为真。**别再往这里加 `if (…Shield) …`**：
        ///    「同一个判据写两处」正是这个字段当初与关键词脱节的成因（原版只有 trait 一份表示）。
        /// 🔴 **2026-10-09（`A1116`）`stun` / `blind` 那两条同批删掉** —— 同一条理由。
        /// </summary>
        void SyncKeywordState(string keyword, int value)
        {
            if (value > 0)
            {
                // ⚠️ **这一行不是「是否失明」** —— 它是 `+0x120`（当前攻击型）那一维：
                //    原版 `CardScript__AddEffect.c:479-483` 挂上 `blind`(`0x3cf`) 时把 `+0x120` 写成 `1`
                //    （近战）。⇒ **留着，别顺手删**（`RuleEngineTest` 在断言它，见 §「`W5` ①a」）。
                if (keyword == KeywordTable.Blind) CurrentAttackType = AttackTypeMelee;
                else if (keyword == "pindown") CurrentAttackType = AttackTypeRanged;
            }
            if (keyword == KeywordTable.Armour) Armor += value;
        }

        /// <summary>移除关键词（`lose X` 用）。**值降到 0 以下就摘掉**</summary>
        public void RemoveKeyword(string keyword, int value = 1)
        {
            if (string.IsNullOrEmpty(keyword)) return;
            int now = KwValue(keyword) - value;
            if (now > 0) _keywords[keyword] = now;
            else RemoveAll(keyword);
        }

        /// <summary>
        /// **整个摘掉**（不管叠了几层）。
        /// 规则书里明确说「移除全部」的地方必须用这个 —— 例：`Markerlight X`
        /// 「受远程伤害后**移除全部**标记光」（:192）；用 `RemoveKeyword` 只会减一层。
        ///
        /// ⚠️ 摘掉关键词时**连带撤掉它当初授予的属性增益** —— 见 <see cref="PendingGrants"/>。
        /// </summary>
        public void RemoveAll(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return;
            _keywords.Remove(keyword);
            if (keyword == KeywordTable.Armour) Armor = 0;
            // 🔴 **2026-10-09（`A1116`）**：这里原来写的是
            //   `if (keyword == "blind") IsBlind = false;`（「`AddKeyword` 那边补了置位，这是它的对偶」）
            //   —— 那是**两个表示的第二次同步**，删掉：`IsBlind` 现在派生自关键词，摘掉键它自己就为假。
            //   ⚠️ **`stun` 当年连这一行都漏了**（本件修的另一半）：`give X Stun` 的关键词被
            //   `TempBuff` 到期 / `lose Stun` / 光环收回摘掉之后 `IsStunned` **仍为真** ——
            //   派生属性一并把那个坑堵上（**没有第二份可漏**）。
            // 🆕 2026-10-09（`A1121`）：**「回合开始时就在失明」那个闸门在摘掉失明时清 0**。
            //   判据 = 原版 `CardScript__AddEffect.c:692`（case 4 摘 trait：`RemoveBuffedTrait` +
            //   `RemoveTrait` 之后 `if (*(int*)(param_2 + 0x3c) == 0x3cf) *(char*)(card + 0x56) = 0;`）。
            //   ⚠️ 原版那一处**只判 `blind`（`0x3cf`），没有 `stun`（100）** —— 如实照做
            //   （`stun` 的闸门只在 `CardScript__Stun.c:98` 与 `OnTurnEnd.c:128` 清）。
            //   ⚠️ 顺带说明：`OnTurnEnd.c:130-134` 那一段 blind **没清 `+0x56`**（原版遗漏），
            //   而我们摘除一律走本方法 ⇒ 那处遗漏在我们这里不成立（行为上等于清了）。
            if (keyword == KeywordTable.Blind) BlindedAtStartOfTurn = false;
            // 🆕 2026-09-16：**授予的正文也要一起撤**（`GrantOps` 那一本账）。
            //   ⚠️ 不撤的话：关键词被摘掉了，`_grantedOps` 还在 ⇒ `RuleCore.FireTriggerAt`
            //   （它的门在 `FxOps` 里、**不在触发点**）照样找得到正文 ⇒
            //   表现是「**关键词没了、效果照放**」。
            //   限时嵌入效果（`Give "Slay: …"` **this turn**）刚接上 `TempBuff` 就会踩到这一条 ——
            //   光环那条路（`Recompose`）本来就在清，这里补上的是**普通路**。
            _grantedOps.Remove(keyword);
            _grantedText.Remove(keyword);
            RevertGrantsOf(keyword);
        }

        // ---- 关键词「带出来的」属性增益 ----------------------------------
        //
        // 有些关键词一旦授予就会**顺手改属性**（原版 `KW_GRANT` 那类）。
        // 记下来是为了「关键词被移除时能把增益一起撤走」——
        // 否则 `+2 Melee Attack` 会永远留在身上（标掉了、值还在 = 静默不一致）。
        public class GrantRecord
        {
            public string Keyword;
            public string Attr;        // attack / ranged / health / armour
            public int Value;
            /// <summary>谁给的（黑暗契约的种类名 / 卡名）。用来「整份收回」——见 <see cref="RevertGrantsFrom"/></summary>
            public string Source;
        }

        readonly List<GrantRecord> _grants = new List<GrantRecord>();
        public IReadOnlyList<GrantRecord> PendingGrants { get { return _grants; } }

        /// <summary>
        /// 记一条「这个属性增益是谁给的」，**并立刻加上去**。
        /// `keyword` 非空 = 是某个关键词带出来的（关键词被移除时一起撤）；
        /// `source` = 给予者（比如黑暗契约的种类名），用来在来源被替换时整份收回。
        /// </summary>
        public void RecordGrant(string attr, int value, string source = null, string keyword = null)
        {
            var g = new GrantRecord { Keyword = keyword, Attr = attr, Value = value, Source = source };
            _grants.Add(g);
            ApplyGrant(g, +1);
        }

        /// <summary>撤掉某个关键词带出来的全部属性增益</summary>
        void RevertGrantsOf(string keyword)
        {
            for (int i = _grants.Count - 1; i >= 0; i--)
            {
                var g = _grants[i];
                if (g.Keyword != keyword) continue;
                _grants.RemoveAt(i);
                ApplyGrant(g, -1);
            }
        }

        /// <summary>撤掉**某一个来源**（例如被替换掉的那份黑暗契约）留下的全部属性增益</summary>
        public void RevertGrantsFrom(string source)
        {
            if (string.IsNullOrEmpty(source)) return;
            for (int i = _grants.Count - 1; i >= 0; i--)
            {
                var g = _grants[i];
                if (g.Source != source) continue;
                _grants.RemoveAt(i);
                ApplyGrant(g, -1);
            }
        }

        void ApplyGrant(GrantRecord g, int sign)
        {
            int v = g.Value * sign;
            switch (g.Attr)
            {
                case "attack": Attack += v; break;
                case "ranged": RangedAttack += v; break;
                case "health":
                    Health += v;
                    MaxHealth = System.Math.Max(1, MaxHealth + v);
                    break;
                case "armour": Armor = System.Math.Max(0, Armor + v); break;
            }
            SyncAttackTypeAfterStatChange();     // 🆕 `W5`：数值变了 ⇒ 重挑攻击型（见该方法的注释）
        }

        /// <summary>
        /// 🆕 **2026-10-18（`W5` · `K3` 账 · `+0x120` 第 10 个写点）**：
        /// **攻/远攻的值一变就按当前数值重挑攻击型** —— 原版 `CardScript.UpdateAttackText`
        /// （`CardScript__UpdateAttackText.c:21-29`：`(近战 &lt; 远程) + 1`），它被
        /// `AddEffect` / `ChangeBaseAttack` / `UpdateFigures` / `ReactToUnitJammed` 等**所有改属性的地方**调用。
        ///
        /// 🔴 **原版那一条带 `+0x40 == 0` 守卫**（`UpdateAttackText.c:21`），
        ///   `+0x40` = `CardScript.CardSetup` 的第 3 个实参 = **`isPlayer`**
        ///   （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardScript.cs:1573`
        ///    `CardSetup(BattleManager mgr, bool isPlayer, CardArmy mainArmy, bool keepStats = false)`；
        ///    同一格在 `ActivateMinion.c:18/:50` 也是拿 `param_2` 比它）。
        ///   ⇒ 原版**只对「不是本机玩家那一侧」的卡**自动重挑，本机玩家手选的那一档不被覆盖
        ///   （本机玩家靠 `UnitOnBoardAttackTypeSelector.AttackButtonClick` → `ChangeAttackType` 显式改）。
        ///   ⛔ **我们不复刻这个守卫，如实标着**：`RuleEngine/Core/` **没有「本机 / 对手」这个概念**
        ///   （两侧对称，`Core/` 不许引表现层），而且我们**没有**「手选攻击型」那个 UI 入口 ——
        ///   `RuleCore.SetCurrentAttackType` 的**唯一**调用点是教程脚本的 `ChangeTo*`。
        ///   后果（如实说）：教程 `ChangeTo*` 之后若**先**发生一次属性增减（走 `ApplyGrant` /
        ///   `RevertBuffs` / `RemoveBuffsFromCard` / `ApplyOneGain`），那把「手选」会被重挑覆盖 ——
        ///   原版不会。今天的教程脚本里那两步之间没有属性变动，所以**不构成现有的行为差异**；
        ///   真要做那一步的判据（本机侧标记）时，**改这里**、别在四个写点各加一份。
        ///   ⛔ 更不要改成「全场重挑」—— 那会把**与本属性无关的**单位的手选也冲掉。
        /// </summary>
        void SyncAttackTypeAfterStatChange()
        {
            CurrentAttackType = RuleCore.ChooseAttackTypeAutomatically(this);
        }

        // ---- 光环加成（2026-09-14 A7）--------------------------------------
        //
        // 光环是**持续**加成：来源在场就有效、离场就收回，而**重算**是「整份摘掉再重加」
        // （照原版 `CardScript__UpdateWhileInPlay`，见设计稿 §8.2/§8.4）。
        // ⇒ 每次重算都要先**精确地**把上一次给的那一份收回来。
        //
        // 🔴 **为什么不能直接用 `AddKeyword` / `RemoveAll` 收**：
        //    `RemoveAll` 是**整个摘掉**（不管叠了几层），而且 `armour` 那条会把 `Armor` **直接清零**。
        //    光环要收的**只是自己那一份**：
        //      · `Baneblade Tank` 自己印着 `Armour 2`，旁边再来一个 `Armour 1` 光环
        //        ⇒ 收回时**只能减 1**，`RemoveAll` 会把它自己的 2 点也抹掉（静默变脆）。
        //      · 同一个单位被**两个**光环加同一个关键词时，得**一人一份**地收。
        //    ⇒ 单开一本账（`_auraKw`）。
        //
        // ⚠️ 属性那一份**不在这里**，走 `RecordGrant(..., source: Auras.GrantTag)` /
        //    `RevertGrantsFrom` —— 那套「整份收回」的机器本来就有（黑暗契约在用），别写第二份。

        readonly Dictionary<string, int> _auraKw = new Dictionary<string, int>();

        /// <summary>光环（A7）给这个单位加的关键词点数。报表与自检要看它</summary>
        public int AuraKwValue(string keyword)
        {
            int v;
            return _auraKw.TryGetValue(keyword, out v) ? v : 0;
        }

        /// <summary>本回合单位身上**由光环给的**关键词（只读，报表用）</summary>
        public IReadOnlyDictionary<string, int> AuraKeywords { get { return _auraKw; } }

        /// <summary>
        /// 加一份**光环给的**关键词。语义同 <see cref="AddKeyword"/>（含那三个要同步状态字段的），
        /// 额外记进 `_auraKw` 这本账，好让 <see cref="ClearAuraGrants"/> 精确收回。
        /// </summary>
        public void AddAuraKeyword(string keyword, int value)
        {
            if (string.IsNullOrEmpty(keyword) || value <= 0) return;
            _keywords[keyword] = KwValue(keyword) + value;
            _auraKw[keyword] = AuraKwValue(keyword) + value;

            // ⚠️ 和 `AddKeyword` 一样**必须同步状态字段** —— 引擎别处是按字段结算的，
            //    只加 `_keywords` 不改字段 = 「给了护甲却不减伤」（原版 `:3293-3299` 专门补过这个 bug）。
            // 🔴 **2026-10-18（`W5`）**：这一段原来是**同一条规则的第二份写法**（只抄了
            //    `armour`/`shield`/`stun` 三条，**漏了 `blind` 与 `K3` 的攻击型那两条**）——
            //    现在收进 `SyncKeywordState`（**判据只此一份**，与 `AddKeyword` 共用）。
            //    ⚠️ 今天全池**没有任何**光环授予 `blind`/`pindown`（实测正则扫过 `have/has/gains …
            //    blind|pindown` **0 命中**）⇒ 这两条是**机制对齐**、不是行为变化。
            SyncKeywordState(keyword, value);
        }

        /// <summary>
        /// 把**光环给的那一份**整份收回来（关键词 + 属性）。
        /// 单位自己的、别的效果给的，一律不动 —— 见上面那段 🔴。
        /// **幂等**：没有光环加成时什么都不做。
        /// </summary>
        public void ClearAuraGrants()
        {
            if (_auraKw.Count > 0)
            {
                foreach (var kv in _auraKw)
                {
                    string kw = kv.Key;
                    int n = kv.Value;
                    int now = KwValue(kw) - n;
                    if (now > 0) _keywords[kw] = now;
                    else _keywords.Remove(kw);
                    if (kw == KeywordTable.Armour) Armor = System.Math.Max(0, Armor - n);
                }
                _auraKw.Clear();
            }
            // 光环挂的**触发效果**也一并摘（`Beastboss on Squigosaur` 给友方野兽挂的 `Slay`）——
            // ⚠️ 只摘光环挂的那几个，`Give "💀 Backlash: …"` 那种效果挂的不动
            if (_auraFx.Count > 0)
            {
                foreach (string kw in _auraFx) { _grantedOps.Remove(kw); _grantedText.Remove(kw); }
                _auraFx.Clear();
            }
            // 属性那一份：`RecordGrant` 记的账，按来源整份撤（黑暗契约用的是同一台机器）
            RevertGrantsFrom(Auras.GrantTag);
            // 障碍标记也要清 —— 它**不一定**伴随关键词/属性（`Nemesor Zahndrekh` 那条只置这个标记），
            // 所以**无条件**清，不能塞在上面那个 `if` 里
            AuraRemnantStay = false;
            // 🆕 2026-10-19（`D28` 施工单 H）：誓约授予那一份同理 —— **无条件**清
            // （`Ferren Areios` 那两句只置这两格，不伴随关键词/属性）
            OathTripleGranted = false;
            OathAllTurnsGranted = false;
        }

        /// <summary>
        /// **这一个残骸不会被「回合结束时摧毁」**（`Nemesor Zahndrekh` 的
        /// `Adjacent Remnants do not disappear at the end of your turn`，A7）。
        /// 由 `Auras.Recompose` 置、`Auras` 重算时清；**读点只此一处**：`RuleCore.DestroyRemnants`。
        /// ⚠️ 它是**光环给的**，所以来源离场后就该变回 false —— 别在这儿写死。
        /// </summary>
        public bool AuraRemnantStay;

        /// <summary>
        /// 🆕 **2026-10-19（`D28` 施工单 H）**：这张牌**自己身上被授予的**「誓约可激活 3 次」
        /// （原版 `DefinedTrait.oathTripleActivation = 0x4fe`）。
        ///
        /// 🔴 **为什么是「身上带着的」而不是「读的时候扫同方」**：原版那两个 trait 的**唯一读点**
        /// 是 `CardScript__CanUseOathAbility.c:16` 的
        /// `EntityScript__HasCurrentTrait(param_1, 0x4fe)`，而 `param_1` = **正在被激活的那张牌**
        /// （全反编译里 `0x4fc` / `0x4fe` 只出现在 `CanUseOathAbility.c`
        /// 与 `EntityScript__GetCardEffectsToShow.c:151-161` 那处展示；
        /// `grep -rn "0x4fc\|0x4fe" d:/2/tools/decomp_full/` 可复现）。
        /// 源牌（`Ferren Areios`）是把它**当持续效果授予友方部队**的 —— 全反编译唯一的授予口是
        /// `CardScript__AddEffect.c:499` 的 `AddTraitSilently(card, effect.trait)`。
        /// ⇒ 读点必须在**被激活的牌自己身上**；「扫同方」会静默放宽（卡面写的是
        /// `friendly **troops**`，督军不该收）。
        ///
        /// **谁写**：<see cref="Auras.Recompose"/>（和别的光环同一趟、同一本账）。
        /// **谁清**：<see cref="ClearAuraGrants"/> —— 与光环**同生命周期**（来源离场就没了）。
        /// **谁读**：`RuleCore.OathActivationCap`（**只此一处**）。
        /// </summary>
        public bool OathTripleGranted;

        /// <summary>见 <see cref="OathTripleGranted"/>（另一半：`oathInAllTurns = 0x4fc`
        /// `CardScript__CanUseOathAbility.c:8`；读点 `RuleCore.OathAllTurns`）。</summary>
        public bool OathAllTurnsGranted;

        // ---- 限时增益（我们上一版复刻的 `temp_buffs`，`rule_core.gd:3300`；⚠️ **旁证，非原版**）----
        //
        // 一条 = 一次**带时长**的施加。到期按两条规则撤：
        //   · `this turn`            → **本回合结束时**撤（不管谁的回合）
        //   · `until your next turn` → **施放者自己的下个回合开始时**撤
        // 出处：`_resolve_text:3018-3019`。
        public class TempBuff
        {
            public bool IsKeyword;
            public string Name;              // 属性名（attack/health/armour/ranged）或关键词名
            public int Value;
            public int Owner;                // 施放者玩家号（`until your next turn` 按他的回合算）
            public bool UntilMyNextTurn;     // true = 施放者的下回合开始撤；false = 本回合结束撤
            public string Src;               // 来源卡名（日志用）
            /// <summary>
            /// **施加它的那张卡的卡名**（2026-09-13 A4 批 1 加）。
            ///
            /// ⚠️ 和 <see cref="Src"/> **不是一回事，别合并**：`Src` 填的是结算层传进来的 `by`，
            ///    而**战术卡那条路 `by` 恒为「战术卡」**（`EffectResolver.ResolveOps` 里那句
            ///    `source != null ? source.Name : "战术卡"`）⇒ 按 `Src` 撤销会把**别的战术卡**的
            ///    限时增益一起撤掉。付费修饰型激活（`Extend effect until your next turn`）要的是
            ///    「**本卡**施加的那些」—— 所以单独记一个真卡名（取自 `ctx.PlayingCard`）。
            /// </summary>
            public string SourceCard;
        }

        readonly List<TempBuff> _buffs = new List<TempBuff>();
        public IReadOnlyList<TempBuff> TempBuffs { get { return _buffs; } }
        public void AddTempBuff(TempBuff b) { _buffs.Add(b); }

        /// <summary>
        /// 撤掉到期的限时增益。
        /// </summary>
        /// <param name="atTurnEnd">true = 正在「回合结束」；false = 正在「某玩家回合开始」</param>
        /// <param name="player">回合开始/结束时，是**哪个玩家**的回合</param>
        /// <returns>撤掉了几条</returns>
        public int RevertBuffs(bool atTurnEnd, int player)
        {
            int n = 0;
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                var b = _buffs[i];
                bool due = atTurnEnd ? !b.UntilMyNextTurn : (b.UntilMyNextTurn && b.Owner == player);
                if (!due) continue;
                _buffs.RemoveAt(i);
                if (b.IsKeyword) RemoveKeyword(b.Name, b.Value);
                else
                {
                    switch (b.Name)
                    {
                        case "attack": Attack -= b.Value; break;
                        case "ranged": RangedAttack -= b.Value; break;
                        // 生命是**上限也跟着变**的（原版 `:3317` 同步 max_health），撤的时候一起回
                        case "health": Health -= b.Value; MaxHealth -= b.Value; break;
                        case "armour": Armor = System.Math.Max(0, Armor - b.Value); break;
                    }
                }
                n++;
                // 🆕 2026-10-18（`W5`）：限时增益撤掉也会改攻值 ⇒ 重挑攻击型
                //（原版 `UpdateAttackText.c:21-29` 会被 `ChangeBaseAttack` 那些地方调到）。
                SyncAttackTypeAfterStatChange();
            }
            return n;
        }

        public bool IsAlive { get { return Health > 0; } }

        /// <summary>
        /// 撤掉**某一张卡**施加的全部限时增益（不看有没有到期）。返回撤掉几条。
        ///
        /// 出处：我们上一版复刻 `rule_core.gd:1777 _undo_temp_buffs_src(ctx, src)`（⚠️ **旁证**；
        /// ⚠️ 2026-10-18 更正：原来标成「**规格书**」—— 它是我们自己的复刻，没有规格地位）—— 付费修饰型激活
        /// （`6 [Energy]: Extend effect until your next turn` / `8 [Energy]: Give it permanently`）
        /// 要先**撤销基础效果**，再用新时长重结算一遍。
        ///
        /// ⚠️ 撤销的**动作**和 <see cref="RevertBuffs"/> 是同一件事（关键词走 `RemoveKeyword`、
        ///    属性按名字回减、生命连上限一起回）—— 两处都照 `:3317` 那套来，**别再写第三份**。
        /// ⚠️ 按 <see cref="TempBuff.SourceCard"/>（**真卡名**）匹配，**不是 `Src`** ——
        ///    战术卡那条路上 `Src` 恒为「战术卡」，按它撤会把别的战术卡的增益一起撤掉。
        /// </summary>
        public int RemoveBuffsFromCard(string cardName)
        {
            if (string.IsNullOrEmpty(cardName)) return 0;
            int n = 0;
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                var b = _buffs[i];
                if (b == null || b.SourceCard != cardName) continue;
                _buffs.RemoveAt(i);
                if (b.IsKeyword) RemoveKeyword(b.Name, b.Value);
                else
                {
                    switch (b.Name)
                    {
                        case "attack": Attack -= b.Value; break;
                        case "ranged": RangedAttack -= b.Value; break;
                        case "health": Health -= b.Value; MaxHealth -= b.Value; break;
                        case "armour": Armor = System.Math.Max(0, Armor - b.Value); break;
                    }
                }
                n++;
                SyncAttackTypeAfterStatChange();   // 🆕 `W5`：同上（这一条是「撤某张卡给的全部增益」）
            }
            return n;
        }

        /// <summary>每回合开始时调用</summary>
        public void RefreshForNewTurn()
        {
            Exhausted = false;
            // 🆕 2026-10-18（`A1093`）：**召唤病也在这里清** —— 判据 = 原版同一处：
            //   `CardScript__OnTurnStart.c:85` 把 `+0x58` 写 **0**（紧接着 `:98` 的
            //   `CardScript__ActivateMinion` 按新值把 `canAct` / `canAttack` 一起写成真）。
            //   ⚠️ 原版那一句带一个回合号守卫（`*(int *)(param_1 + 0x2e0)` 那一比），
            //      我们**没有**对应字段 ⇒ 这里与上面 `Exhausted` 一样**无条件清**，
            //      如实标着（这是已知的、刻意的近似，不是「原版也这样」）。
            SummonSickness = false;
            // 🆕 2026-10-18（`A992` 遗留甲）：**「本回合不能攻击」那一位也按回合清** —— 原版
            //   `+0x58`（「这一回合上的场」）在回合开始被清，紧接着 `CardScript.ActivateMinion`
            //   把 `canAct` / `canAttack` **一起**写成真（`CardScript__ActivateMinion.c:39-42`）
            //   ⇒ 「部署当回合能动但不能攻击」只持续**到自己的下一个回合开始**。
            //   ⚠️ 本方法**只刷当前行动方的单位**（`RuleCore.BeginTurn` 里那一圈），
            //      所以它在对手回合仍然为真 —— 那是对的：对手回合本来就 `ErrNotTurn`。
            CannotAttackThisTurn = false;
            AttacksThisTurn = 0;
            // 「正在祈祷」是**按回合**的状态（我们上一版复刻 `rule_core.gd:1953` 就在这一批里清；⚠️ **旁证**）。
            // 卡面：`Each friendly unit that is Praying heals 3`（`Devout Serenity`）·
            //       `If any friendly unit is Praying, …`（`Sororitas Rhino`）。
            Prayed = false;
            // 「本回合下一次用狂暴时留在场上」的**兜底复位**（2026-09-14 A4 批 3）——
            // 卡面写 `this turn`：这一回合没用到，就作废。
            // ⚠️ **消费点是 `RuleCore.UseAlternative` 的狂暴那一段**（用掉当场清，才是「下一次」）；
            //    这里只是「回合过了」的兜底。两处都要，缺一个就会「用两次」或者「跨回合还留着」。
            FerocityStay = false;
            // 🆕 2026-09-16 誓约（Oath）能力的每回合激活计数 —— 原版在回合末清零
            // （`CardScript__OnTurnEnd.c:209`）⇒ 「本回合没用完就作废」。
            // ⚠️ `DeployedTurn` **不清**（它记的是历史：那张牌是哪一回合上场的）。
            OathUsesThisTurn = 0;
            // 🆕 2026-09-29 「本回合用过主动能力」同样按回合清（原版 `CardScript__OnTurnEnd.c:209`
            // 和上面那个计数器**在同一行附近**清零 —— 见 `UsedActiveAbilityThisTurn` 的注释）。
            // 读它的是 `Duty` 徽标的「未激活」态。
            UsedActiveAbilityThisTurn = false;
            // 🆕 「再触发一次」的额度兜底清空（正常路径**用掉就摘**，见 `ExtraTriggers` 的注释）
            ExtraTriggers.Clear();
        }

        public override string ToString()
        {
            return $"{Name} {Attack}/{Health}{(IsWarlord ? " (督军)" : "")}{(Exhausted ? " 疲劳" : "")}";
        }
    }
}
