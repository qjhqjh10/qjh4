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
    public partial class UnitState
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

        /// <summary>
        /// 🆕 **稳定实例号**（= <see cref="CardInstance.Id"/>；**只转发、不另存一份**）。
        /// 给 `BattleEvent.InstanceId` / 表现层「同名单位血账」用 —— 判据与「为什么不能用座位 + 格号」
        /// 见 `BattleEvent.InstanceId` 的 doc（原版 `CardScript + 0x22c` 的本局唯一号）。
        /// `0` = 没有实例（只有手搓 `null` 才会出现；真对局给正数、`CardInstance.Detached` 给负数）。
        /// </summary>
        public int InstanceId { get { return Instance != null ? Instance.Id : 0; } }

        public readonly string Name;
        public readonly bool IsWarlord;

        // 🔴 **2026-10-25（E1 · 属性模型 Stage 2a）**：这三格从「物化当前值」改名为 **base**
        //   （原版 `currentBaseMeleeAttack`+0x7c / `currentBaseRangedAttack`+0x90 / `currentMaxHealth`+0xa4）。
        //   旧名字 `Attack` / `RangedAttack` / `MaxHealth` 现在是**只读转发属性**（见**类尾**那一段）。
        public int AttackBase;        // 近战攻击力（**base**，不含任何 `addStats` 修饰）
        public int RangedAttackBase;  // 远程攻击力（**base**）
        public int Armor;             // 伤害减免（最低 1）。**只有 Armour X 关键词给护甲**
        public int Health;
        public int MaxHealthBase;     // 最大生命（**base**；原版 `currentMaxHealth` +0xa4）

        // ============================================================================================
        // 🆕 2026-10-25（`N7` · 属性模型重构 **Stage 1：只加投影、无人调用**）
        //
        // 【为什么有这一段】原版是「**一半存档、一半现算**」：
        //   存档的只有 `currentHealth`(+0x68) / `currentMaxHealth`(+0xa4) / `currentArmourValue`(+0xb8) /
        //   `currentBaseMeleeAttack`(+0x7c) / `currentBaseRangedAttack`(+0x90)；
        //   而 `CurrentMeleeAttack` / `CurrentRangeAttack` / `CurrentCost` / `currentMaxHealthProcessed` /
        //   全部 `Current<关键词>` 都是**每次读时从 `activeEffects`(+0x108) 现算**的。
        //   我们上面那三格（`Attack` / `RangedAttack` / `MaxHealth`）是**把这一族物化成了字段**
        //   ⇒ 模型差一层。可观察的后果（例）：原版 `setMeleeAttack(70)` 命中即 `return`，
        //   之后再叠 `addStats(+1 melee)` **不涨**；我们会涨。
        //
        // 【四步走】（判据 → `Unity/资料/完全复刻/判据/战斗逻辑驱动_前置门与现算口径.md` §六，方案 A）
        //   · **Stage 1（本段）= 只加只读投影，旧字段原样保留，谁都不调**；
        //   · Stage 2a/2b = 换写者（`Attack` → `AttackBase` + 转发属性）；
        //   · Stage 2c = **换读者**（`Attack => CurrentMeleeAttack` 等）—— 🔴 **行为在 2c 才变**。
        //
        // 🔴 **Stage 1 的恒等纪律**（判据 §6·1 / §6·3）：本段每一条**必须**与旧字段逐单位恒等 ——
        //   `CurrentMeleeAttack == Attack` ∧ `CurrentRangeAttack == RangedAttack` ∧
        //   `CurrentMaxHealthProcessed == MaxHealth` ∧ `CurrentIntrinsicHealth == Health` ∧
        //   `CurrentIntrinsicMeleeAttack == Attack`。所以这里**只转发旧字段、不查任何效果表**：
        //   修饰表要到 2a/2b 才建，Stage 1 时它「还不存在/还空」⇒ Σ = 0 ⇒ 恒等。
        //
        // ⚠️ 两条**给 Stage 2 的写手**的硬提醒（踩了就是栈溢出 / 静默差）：
        //   ① `IsBlind ⇒ CurrentRangeAttack = 0`（`get_CurrentRangeAttack.c:24-28`）**故意没有写进来** ——
        //      写了就不恒等（真 Play 里给单位上 blind 就会差）。它落在 **2c**，届时「读攻值的每一处
        //      同时看到 0」才是判据 §7·2 要的**一次性全对**；⛔ 别在 Stage 1/2a/2b 提前加。
        //   ② **2c 换投影时，本段这几条必须同时改写成「读 base + 查修饰表」** —— 因为 2c 会把
        //      `Attack` / `RangedAttack` / `MaxHealth` 换成**转发到本段的属性**，
        //      本段若还读 `Attack` 就是**无限递归**（`Attack` → `CurrentMeleeAttack` → `Attack` …）。
        //      改写点逐条写在各自的注释里。
        //
        // 判据来源一律是**第一权威** `d:/2/tools/decomp_full/EntityScript__get_*.c`（本轮逐方法复读过）。
        // ============================================================================================

        /// <summary>
        /// **当前近战攻击力** —— 原版 `EntityScript.get_CurrentMeleeAttack`
        /// （`d:/2/tools/decomp_full/EntityScript__get_CurrentMeleeAttack.c:29-67`）。
        ///
        /// 原版算法（逐句读的，别再按印象改）：
        ///   · **正序**遍历 `activeEffects`(+0x108)：`i = 0 … n-1`（`:29-51`）；
        ///   · `buffType(+0x38) == 1`（`addStats`）⇒ 累加 `meleeAttackChange(+0x48)`（`:39-41`）；
        ///   · `buffType == 70`（`setMeleeAttack`，反编译写成 `0x46`）⇒ 🔴 **立刻 `return` 它的
        ///     `meleeAttackChange`**（`:44-47`）—— **连 `currentBaseMeleeAttack`、连已经累加的那部分都不算**。
        ///     因为是正序遍历 ⇒ 多条时**列表里第一条**胜出（后面的根本读不到）；
        ///   · 扫完 ⇒ `clamp0(currentBaseMeleeAttack(+0x7c) + Σ)`（`:52-66`：
        ///     `if (-1 < iVar2 + iVar7) return …; return 0;`）。
        ///
        /// 🔴 **这一条最容易被写错的口径 = 「`setMeleeAttack` 是覆盖、且命中即返回」**。
        ///   它的可观察后果：**一旦被 set 过，之后再叠 `addStats(+1 melee)` 不涨**（判据 §6·4① 的验收用例）。
        ///   ⇒ Stage 3 把写点改成「落修饰」时，**不能**把 set 当成一条普通加数。
        ///
        /// **Stage 1**：恒等（`== Attack`）—— ① 修饰表还没有（永久增益仍直接写字段）
        ///   ② 没有 `setMeleeAttack` 记录。**Stage 2c 才切换，届时行为才变。**
        /// ⚠️ **2c 改写点**：`return Attack;` 换成「扫记录 → **命中 set 立即返回它的值** /
        ///   否则 `clamp0(AttackBase + Σ addStats.meleeAttackChange)`」，⛔ **不能留着读 `Attack`**（见上 ②）。
        /// </summary>
        public int CurrentMeleeAttack { get { return ProjectMeleeAttack(); } }

        /// <summary>
        /// **当前远程攻击力** —— 原版 `EntityScript.get_CurrentRangeAttack`
        /// （`d:/2/tools/decomp_full/EntityScript__get_CurrentRangeAttack.c:24-70`）。
        ///
        /// 原版算法 = <see cref="CurrentMeleeAttack"/> 的同形，两处换字段：
        ///   · **第一句**先判 `HasCurrentTrait(0x3cf /*blind*/) ⇒ return 0`（`:24-28`）；
        ///   · `buffType==1` 累加 `rangeAttackChange(+0x50)`；`buffType==85`（`setRangeAttack`，
        ///     反编译写成 `0x55`）⇒ **命中即 `return`** 它的 `rangeAttackChange`（`:41-49`）；
        ///   · 扫完 ⇒ `clamp0(currentBaseRangedAttack(+0x90) + Σ)`（`:50-64`）。
        ///
        /// **Stage 1**：恒等（`== RangedAttack`）—— 🔴 **`IsBlind ⇒ 0` 那一支故意没写**
        ///   （见本段顶部 ⚠️①；判据 §6·5 的「Stage 1 退化成恒等」说的就是这条）。
        ///   今天「blind ⇒ 远程算 0」只发生在 `RuleCore.FieldAttack`（算伤害那一跳）；
        ///   其它读点（AI / 条件 / 卡面）看到的仍不是 0 —— 那是 §7·2 那 11 组差异，**归 2c 一次性修**。
        /// ⚠️ **2c 改写点**：先 `if (IsBlind) return 0;`，再「扫记录 → 命中 set 即返回 /
        ///   否则 `clamp0(RangedAttackBase + Σ)`」，⛔ 不能读 `RangedAttack`（递归）。
        /// </summary>
        public int CurrentRangeAttack { get { return ProjectRangeAttack(); } }

        /// <summary>
        /// **当前最大生命（已处理）** —— 原版 `EntityScript.get_currentMaxHealthProcessed`
        /// （`d:/2/tools/decomp_full/EntityScript__get_currentMaxHealthProcessed.c:22-108`）。
        ///
        /// 原版算法（**倒序遍历两趟**，`:33-59` 与 `:60-107`，⭐ 顺序有语义、别改用正序）：
        ///   1) 起手 `base = currentMaxHealth(+0xa4)`（`:22-32`）；
        ///   2) **第一趟（倒序）**：`extrinsic(+0x30) == 0` ∧ `buffType==1` ∧ `healthChange(+0x60) != 0`
        ///      ⇒ `base += healthChange`（`:40-58`）；
        ///   3) **第二趟（倒序）**：`extrinsic != 0` ∧ `buffType==1` ∧ `healthChange != 0`
        ///      ⇒ `ext += healthChange`（`:71-89`）；
        ///      同一趟里再判 `buffType == 9`（`setHealth`）∧ `healthChange != 0` ⇒ **`base` 整项被覆盖成
        ///      它**（`:94-105`）—— 注意这是**覆盖前两趟已经算好的 base**，不是加；
        ///      ⚠️ 倒序遍历 ⇒ 多条 `setHealth` 时**列表里第一条（下标 0）最后写入、因而生效**；
        ///   4) `return base + ext`（`:65` / `:107`）—— 🔴 **全程无 clamp**（与 CurrentMeleeAttack 不同）。
        ///
        /// 为什么我们非得加这一层：我们把原版的**两格并成了一格** —— 我们的 `MaxHealth` 扮演的是
        /// **processed**（`EffectResolver.ApplyOneGain` 的 health 支直接 `MaxHealth += val`），
        /// **拿不到「原始上限」**。Stage 2a 会把字段改名 `MaxHealthBase` + 加转发属性；
        /// 本投影就是「processed」那一格。
        ///
        /// **Stage 1**：恒等（`== MaxHealth`）—— 修饰表还不存在 ⇒ ① Σ 两项 = 0 ② 无 `setHealth` 记录。
        ///   **Stage 2c 才切换，届时行为才变。**
        /// ⚠️ **2c 改写点**：`return MaxHealth;` 换成上面 4 步，**base 取 `MaxHealthBase`**
        ///   （⛔ 不能取 `MaxHealth` —— 2c 时它是转发到本属性的，会无限递归）。
        /// </summary>
        public int CurrentMaxHealthProcessed { get { return ProjectMaxHealthProcessed(); } }

        /// <summary>
        /// **固有生命**（不含 extrinsic 加成）—— 原版 `EntityScript.get_CurrentIntrinsicHealth`
        /// （`d:/2/tools/decomp_full/EntityScript__get_CurrentIntrinsicHealth.c:26-50`）。
        ///
        /// 原版 = `currentHealth(+0x68)` − Σ(`extrinsic(+0x30) != 0` ∧ `buffType==1` 的
        /// `healthChange(+0x60)`)（**正序**遍历，`:53-55`；返回 `:49-50`，**无 clamp**）。
        ///
        /// **Stage 1**：恒等（`== Health`；Σ = 0）。**Stage 2c 才切换**。⚠️ 2c 改写点见上一条同型
        /// （`Health` 本身**不做投影** —— 原版 `currentHealth` 就是存档，判据 §6·2·C）。
        /// </summary>
        public int CurrentIntrinsicHealth { get { return ProjectIntrinsicHealth(); } }

        /// <summary>
        /// **固有近战**（不含 extrinsic 加成）—— 原版 `EntityScript.get_CurrentIntrinsicMeleeAttack`
        /// （`d:/2/tools/decomp_full/EntityScript__get_CurrentIntrinsicMeleeAttack.c:22-46`）。
        ///
        /// 原版 = `CurrentMeleeAttack` − Σ(`extrinsic != 0` ∧ `buffType==1` 的 `meleeAttackChange(+0x48)`)
        /// （`**正序**` 遍历 `:29-39`；`:44` 调一次 getter；`:45` 返回差的**无 clamp**）。
        /// ⚠️ 它减的是**投影出来的值**，所以 `setMeleeAttack` 命中时也是「覆盖值 − extrinsic 加成」
        ///   （判据 §6·5 那一条注解，别自己改成「减 base」）。
        ///
        /// **Stage 1**：恒等（`== Attack`；Σ = 0）。**Stage 2c 才切换**。
        /// ⚠️ 2c 改写点：`return CurrentMeleeAttack;`（⛔ 那时**不要**改成读 `Attack` —— 它转发回本族属性）。
        /// ⚠️ 同族的 `CurrentIntrinsicRangeAttack` **没有加** —— 原版那个方法体本轮**没读**
        ///   （判据 §3·5 ② 已如实记「未证」）⇒ 不凭对称猜着写。
        /// </summary>
        public int CurrentIntrinsicMeleeAttack { get { return ProjectIntrinsicMeleeAttack(); } }

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
        /// 🔴 **谁写它（我们这侧）—— 两条路、两处写点**：
        ///     · **普通部署**（手牌打出 / 免费部署 / 再造残骸）= <see cref="RuleCore.ApplyDeployTurnState"/>；
        ///     · **转移归属 ⇒ 重新入场**（抢 `EffectResolver.DoTakeControl` ·
        ///       回合末归还 `RuleCore.EndTurn`）= <see cref="RuleCore.ResetSummonSickness"/>。
        ///    **清它** = <see cref="RefreshForNewTurn"/>。
        ///
        /// 🔴 **2026-10-18 之后·第十二会话（`A1098`）就地订正（铁律 5）** —— 本段原来写
        ///    「**普通部署那一路我们没有找到「写 1」的原版读点**」（并注明出处在
        ///    `资料/普查产出_第六会话/W_补0x58位_与A1071那一行.md` §⑤）。**那一句已不成立：找到了。**
        ///    真身 = **`CardScript._MinionPlayedIntoField_d__314__MoveNext.c:69-71`**
        ///    （现读 `d:/2/tools/decomp_full/`）：
        ///    `if (*(char *)(param_1 + 0x30) == '\0') *(undefined1 *)(lVar4 + 0x58) = 1;`
        ///    = **`CardScript.MinionPlayedIntoField(...)` 协程体里的 `if (!keepStatus) summonSickness = true;`**。
        ///    字段序逐条对上（`CardScript.cs:523-560` 那个 `_003CMinionPlayedIntoField_003Ed__314`：
        ///    `doTransformAnim` `+0x20` / `<>4__this` `+0x28` / `keepStatus` `+0x30` /
        ///    `unitInPlayCounter` `+0x34` / `delay` `+0x38` / `shouldTriggerSummon` `+0x3c` /
        ///    `minionManager` `+0x40`；工厂 `CardScript__MinionPlayedIntoField.c:20-25` 逐条印证）。
        ///    ⚠️ **错因**：上一批按**方法名**找（`grep ResetSummonSickness` / `set_summonSickness`），
        ///    而这一处写在一个 **`[IteratorStateMachine]` 生成的协程类**里，名字里**没有**
        ///    `summonSickness` 字样 ⇒ 按名字搜必然漏。**`set_summonSickness` 零调用点不是
        ///    「方法体未解出」的证据** —— 原版对这个字段**一直是直接写**（`ResetSummonSickness.c:8`
        ///    也是直接写），那个 public setter 本来就没人用。
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
        /// **正在祈祷**（2026-09-13 A4 批 1）—— 执行过 `Pray` 替代行动的单位。
        ///
        /// 🔴 **2026-10-14（账 `A1155②`）时长【已核】—— 原来这里写「按回合重置」是【模糊的】**：
        ///    真值区间 = 「**祈祷那一刻**」→ 「**下一个回合开始（不分敌我）**」或
        ///    「**自己挨一次伤害**」，**以先到者为准**。
        ///    判据 = `d:/2/tools/decomp_full/` 里 `EntityScript.isPraying`（`+0x67`）的**全部写点**
        ///    （`grep -rn "param_1 + 0x67)"` **只有 3 处**）：
        ///      · 置真 `BattleManager__ResolveTriggerPray.c:266`；
        ///      · 置假① `CardScript__OnTurnStart.c:100`（**无条件**，且对**全场**每张在场卡各跑一次
        ///        —— `BattleManagerSupport__BroadcastTurnStart.c:20-26`）⇒ 落在 `RuleCore.BeginTurn`；
        ///      · 置假② `CardScript__ResolveDamageDealt.c:168`（闸 = 受伤者 == 自己）⇒ 落在 `RuleCore.ApplyDamage`。
        ///    ⛔ **别再写成「按回合重置」**（那样读起来像「只在自己回合开始清」，正是改前的错口径）。
        /// 出处：我们上一版复刻 `rule_core.gd:2371`（`u["prayed"] = true`，在 `Pray` 那一点）·
        ///       `:1953`（`u["prayed"] = false`，和 `exhausted` / `attacks_turn` 同一批清）。
        /// ⚠️ 2026-10-18 更正：这里原来把这行标成「**规格书**」—— 那份 `.gd` 是我们自己的复刻、
        ///    只是**旁证**，不是规格/判据（见文件头）。
        /// 卡面两处：`Each friendly unit that is Praying heals 3`（`Devout Serenity`）·
        ///           `If any friendly unit is Praying, …`（`Sororitas Rhino`）。
        /// ⚠️ 它和 `When a friendly unit Prays` **不是一回事**：那个是**事件**（发生的那一下），
        ///    这个是**状态**（挂着，直到上面那两件事之一发生）。两张卡各要各的。
        /// </summary>
        public bool Prayed;

        /// <summary>
        /// ⛔ **这里【没有】`PrayEnchantment` 这个字段了**（2026-10-25 引擎线 `X20` 删除）。
        ///
        /// <para>**为什么删**：原版那条语义**不是代码、是一张隐藏附魔卡** —— `Pray Enchantment`
        /// （`m_Name = Z-NC Sororitas-Pray_enchantment`，`cardType 20`，`path_id 1528516720421242597`）。
        /// 23 张带 `pray` 特性的卡在 `cardAbilities[trigger = 655 (Pray)].abilityLogic[0].buffEffect`
        /// 挂 `{buffType: 15 singleUseEnchantment, enchantment → PE}` ⇒ **祈祷那一刻挂到自己身上**；
        /// 该附魔自己两条能力：ability[0] `trigger 60 (UnitDamaged) → removeThisEnchantment`（**受伤即撤**）、
        /// ability[1] `trigger 31 (TurnStartPlayer) → buffMana{manaAmount:1, faithMana:1} + removeThisEnchantment`
        /// （**自己回合开始 +1 信仰、一次性**）。判据全文
        /// → `资料/待办判据_祈祷信仰与任务点_1025b.md` §二·④。</para>
        ///
        /// <para>`X13`（2026-10-25 早）曾用「`bool PrayEnchantment`」做引擎内等价实现；`X20` 把那张附魔卡
        /// 按原版卡数据链入池之后，状态**只剩一个存放处** = 载体实例上的附魔层
        /// （`CardInstance.ActiveEnchantments`，入口 `Enchantments.AttachPray` / `HasPray` / `RemovePray`）。
        /// ⛔ **别再把 bool 长回来**：两处写同一条规则 = 迟早不一致（`CLAUDE.md` §三）。
        /// 四个落点（挂 = `RuleCore.UseAlternativeCore` 的 `Pray` 支；结算 = `BeginTurn` 的
        /// `GrantPrayEnchantmentFaith`；受伤 = `ApplyDamage` 与疲劳那一支）全部走 `Enchantments`。</para>
        ///
        /// <para>⚠️ 与上面 <see cref="Prayed"/> **仍是两件事**：`Prayed` 是「正在祈祷」**状态**
        /// （每次回合开始、双方全清），附魔活到**它自己主人的下个回合**。</para>
        /// </summary>

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
        /// ⚠️ 别和 `SW36 Bjorn the Fell-Handed` 的 `When a friendly unit uses Ferocity, it stays in play`
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
        /// 🔴🆕 **2026-10-24（引擎线 `T03·c` · `C1` §5）：这具残骸**正在离场**（原版 `EntityScript +0x66
        /// = `isLeavingRemnant`）。
        ///
        /// <para>**为什么需要它**：原版 `EntityScript__get_currentCombatAttack.c:40-47` 的两道闸是
        /// **`+0x65 (isRemnant) && !+0x66 (isLeavingRemnant)` ⇒ 返回 0** —— 也就是说
        /// **「正在离场的残骸」会落回正常攻击值**（按 `+0x120` 的近战/远程），不是 0。
        /// 我们原来只有 <see cref="IsRemnant"/> 一维 ⇒ 那一档会算成 0（terror 豁免 / 反击预判偏）。</para>
        ///
        /// <para>⚠️ **今天没有写入点**（`grep` 全仓只有本字段与 `CounterAttack` 的读）——
        /// 原版那个 `+0x66` 在哪一刻置 1 **没查清**（C1 §5 末：写入点本轮未追）。
        /// ⇒ 语义按判据补齐，**可达性未证**；⛔ 别自行在死亡路径上顺手置它（铁律 2：不许猜口径）。</para>
        /// </summary>
        public bool IsLeavingRemnant;

        /// <summary>
        /// **面朝下**（`Ambush`，2026-09-13 A2）—— 规则书 `:166`「伏击：**面朝下打出**；
        /// **下次回合前**若被伤害：翻开**无效果**；若未被伤害：翻开**并触发效果**」。
        /// ⚠️ **2026-10-20 补注**：上面这句是**粉丝实体版规则书**（非官方）的说法，
        ///    **不是数字版判据** —— 数字版的真实行为见下（两者在「挨打触不触发」上相反）。
        ///
        /// 🔴 **2026-10-20（`W58-AMBUSH` · 铁律 5 就地订正）**：本段原写
        /// 「两条出口：挨到**实际伤害** → `ApplyDamage` 里翻开、**不触发**；撑到自己**下一个回合开始**
        /// → `RuleCore.RevealAmbush` 翻开并触发」—— **两句都已作废**：
        ///   ① 原版 `CardScript.SetAmbush` 全二进制**只有 7 个调用点、没有一个在回合开始**
        ///      ⇒ 「窗口到期自动揭示」**不存在**（`RuleCore.RevealAmbush` **已删除**）；
        ///   ② 「挨打不触发」也不对 —— 触发与否看**回合归属**，不是看有没有挨打
        ///      （`CardScript__SetAmbush.c:69-129` 最后那道 `isPlayer == IsPlayerTurn()` 闸）。
        ///
        /// **现行两条出口**（都在 `RuleCore` 里，判据各只一处）：
        ///   · 挨到**实际伤害**（所有减免之后）→ `RuleCore.ApplyDamage` 里 `FaceDown = false`
        ///     并发 `EvtKind.AmbushExit{Effect="damage"}`；**卡主自己那一方是当前行动方**才触发伏击正文；
        ///   · **面朝下地出手攻击** → `RuleCore.DeclareAttack` 里 `FaceDown = false`
        ///     并发 `EvtKind.AmbushExit{Effect="attack"}`，取消这一记、**再由续攻补打**
        ///     （原版 `_ResolveAttack:288-305` + `:488`）；同样只在卡主自己那一方是当前行动方时触发正文。
        /// ⚠️ `EvtKind.AmbushExit.Effect` 里 `"window"` 是**历史值、今天不可达**（见它的 doc）。
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

        // ---- 🆕 2026-10-11（第十三会话 · `A1159` / `A1218②`）：**伤害模型上的两个「第二条命」----
        //
        // 🔴 **为什么是【派生属性】而不是新字段**：原版这两个都是 `DefinedTrait`，
        //    读法一律走 `EntityScript.GetCurrentTraitValueWithModifiers` ——
        //      · `CurrentSurvivor` = `EntityScript__get_CurrentSurvivor.c:5`（`0x1ae` = 430）
        //      · `CurrentBastion`  = `EntityScript__get_CurrentBastion.c:5`（`0x352` = 850）
        //    ⇒ 我们的 `KwValue`（= `_keywords` 里那个值）**就是**那个读法，**只有一份表示**。
        //    加一个 int 字段 = 同一条规则的第二个表示 —— 那正是 `A1116` 把 `stun` / `blind`
        //    从布尔字段改成派生属性时铲掉的东西（`RemoveAll` 的注释点过名）。
        //    ⛔ **别改回字段、也别在别处补第二份同步**。

        /// <summary>
        /// **当前幸存者**（`CurrentSurvivor`）—— 带它就把「这一击会打死我」改成
        /// 「消耗掉它、把生命设回来、留场」的**第二条命**。
        /// 常量与语义 → <see cref="KeywordTable.Survivor"/>；消耗口 → <see cref="UseSurvivor"/>；
        /// 判死闸 → `RuleCore.EnoughPendingDamageToDie`；留场那条支路 → `RuleCore.CleanupDeaths`。
        /// </summary>
        public int Survivor { get { return KwValue(KeywordTable.Survivor); } }

        /// <summary>
        /// **当前堡垒**（`CurrentBastion`）—— 两处**形状不一样**，别混：
        ///   · **预览 / 判死**（`CardScript__EnoughPendingDamageToDieWithDamageValues.c:177/222/241/248`）：
        ///     当作**额外血量**与生命相加（`health + CurrentBastion`）→ <see cref="RuleCore.WouldKillByEntries"/>。
        ///   · 🔴 **结算**（`CardScript._ReceiveDamage_d__381__MoveNext.c:126-156` →
        ///     `CardScript__RemoveBastionDamage.c`）：堡垒 ≥ 1 时**整份伤害被它吃掉**、生命不动，
        ///     存量按伤害量扣减、**打穿了才把溢出打到生命** → `RuleCore.ApplyDamage`（`W4` 落地）。
        /// </summary>
        public int Bastion { get { return KwValue(KeywordTable.Bastion); } }

        /// <summary>
        /// 🔴 **消耗掉幸存者**（原版 `CardScript.UseSurvivor`）—— 「第二条命」真正用掉的那一步。
        ///
        /// 判据 = `d:/2/tools/decomp_full/CardScript__UseSurvivor.c`（逐句）：
        ///   · `:29-39` 生命 = **`Min(CurrentSurvivor, CurrentMaxHealthProcessed)`**
        ///     （`System_Math__Min`）写回 `+0x68..0x78`；
        ///   · `:41-42` `RemoveBuffedTrait(0x1ae)` + `RemoveTrait(0x1ae)`（= **整个摘掉** `survivor`）；
        ///   · `:71` `AddTraitSilently(0x1b8 /*survivorSpent*/)`；
        ///   · `:87` `BattleManager.BroadcastUnitHealed(card)` → 我们发一条**负数 `Hit`**
        ///     （🔴 **2026-10-20（`W58`）就地订正（铁律 5）**：本行原来写「负数 `Hit` = 治疗，
        ///     表现层据此走绿字，与 `EndTurn` 的再生段**同一个约定**」—— **已过期**。
        ///     现行口径：**治疗 = `EvtKind.Heal`**（唯一口 `RuleCore.HealUnit`，`Amount` = 实际治疗量、正数；
        ///     再生 / 猎杀标记 / `heal` op / `heal` 能力都走它）；
        ///     **负数 `Hit` 仅作幸存者广播的标记**（发出点 = `RuleCore.CleanupDeaths`，
        ///     **表现层对它全静默** —— 原版 Survivor 不进 `CardScript.Heal`，不飘治疗字、没有治疗圈）。
        ///     详见 `BattleEvent.Amount` 的 doc；⛔ 别把它改成 `EvtKind.Heal`）。
        ///   · `:73-75` 🔴 **`RawCardScript.OnTrigger(raw, 0x1a4, …)`** ——
        ///     `0x1a4` = `AbilityTrigger.Survivor = 420` ⇒ 「幸存者**作为触发 id**」那一半
        ///     （本方法**自己不发** —— `UnitState` 上没有 ctx，由调用点
        ///     `RuleCore.CleanupDeaths` 在本方法**之后**补一次 `FireTriggerAt(Survivor)`）。
        /// 返回实际回的血（`0` = 没得回，调用方据此决定要不要出声）。
        ///
        /// ⚠️ **`survivorSpent` 那一条照原版加了**（<see cref="KeywordTable.SurvivorSpent"/>）——
        ///    它**只是**一个 trait（原版 `:71` `AddTraitSilently(0x1b8)`），**没有**配套的触发时机：
        ///    🔴 **2026-10-11（`W4` · 铁律 5）**：本段原来写着「它承诺的触发时机（`OnTrigger(0x1b8, …)`）
        ///    我们还没做」—— **那句是错的**，`0x1b8` 在 `_ResolveAttack…:379` / `OnTurnStart.c:266`
        ///    是 **`AbilityTrigger.Landing = 440`**（两个枚举数值巧合相同），
        ///    而 `AbilityTrigger.SurvivorSpent = 422 = 0x1a6` 全量反编译 `grep` **0 命中**
        ///    ⇒ 原版**根本没有**这条触发。详见 <see cref="KeywordTable.SurvivorSpent"/> 的 doc。
        /// ⚠️ 原版紧邻的 `:152-155` 还有一支 **`sacrifice`（trait 470）** ——
        ///    ⚠️ **它在 `UseSurvivor` 【之前】**（`TriggerSacrifice` 在 `:157`，`UseSurvivor` 在 `:165`）
        ///    ⇒ 由调用点 `RuleCore.CleanupDeaths` 排在本方法之前，⛔ 别挪到本方法里。｜⚠️ 🔶 自己设计（非原版）：函数体第一句 `if (sv &lt; 1) return 0;` 是**我们加的护栏**（原版 `UseSurvivor.c:29-44` **没有**早退 —— `CurrentSurvivor == 0` 时它会 `Health = Min(0, max) = 0` 当场判死）· 因为原版唯一调用点只会在 `HasCurrentTrait(survivor)` 为真时调它 ⇒ 这一格在本池不可达，我们选择「不动作并返回 0」而不是「杀死单位」· 调度台 B8_AM 2026-10-26 裁定。
        /// </summary>
        public int UseSurvivor()
        {
            int sv = Survivor;
            if (sv < 1) return 0;
            int before = Health;
            Health = System.Math.Min(sv, MaxHealth);        // 原版 `UseSurvivor.c:29-39`
            RemoveAll(KeywordTable.Survivor);               // 原版 `:41-42`（整个摘掉，不是减一层）
            AddKeyword(KeywordTable.SurvivorSpent, 1);      // 原版 `:71`
            return Health - before;
        }

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
        /// 🔴🆕 **2026-10-27 就地订正（铁律 5 · 执行代理 `B9_W27ENG`）**：本段原来把这一位写成
        /// 「原版 `EntityScript.usedActiveAbility`，**偏移 `+0x4C`**，清零 = `CardScript__OnTurnEnd.c:209`」
        /// —— **两句都错**（`+0x4C` 是**另一格**：**本局一次、不随回合重置**，见 <see cref="ActiveAbilityUsed"/>；
        /// `OnTurnEnd.c:209-210` 清的是 `+0x48 attacksThisTurn` 与 `+0x50 usedActiveAbilityTimesPerTurn`）。
        /// 本字段对应 **`+0x50` 那一族每回合计数位**的布尔化（「本回合已激活过」）—— 写入点仍是 `CardScript__ResolveActiveAbilityPlayed.c:31-32`（`+0x50` 每次 ++），清零 = `OnTurnEnd.c:209`。
        ///
        /// ✅ **2026-10-27（`B9_W49BDR3`）已改完**：`CardPresentation/Battle/BattleDriver.cs:27866` 画 `Duty` 徽标
        ///    已改读 `!u.ActiveAbilityUsed`（原版 `CardTraitDuty__IsActive.c:19` 读的是 `+0x4C`，不是这一位）。
        ///    ⇒ 本字段**今天没有任何表现层/规则消费者**（剩下的读点只有引擎写入、清零与自检）。
        ///    `CardTraitOath.IsActive` 是另一条判据（`CardScript.CanUseOathAbility`），见 `Badges.For`。
        /// ⚠️ **别拿 `Exhausted` 顶替** —— 攻击也会置 `Exhausted`，而原版这里只认「主动能力」。
        /// </summary>
        public bool UsedActiveAbilityThisTurn;

        /// <summary>🆕 2026-09-16 **这张牌上场的回合号**（`ctx.Turn`；没上场过 = -1）。
        ///
        /// 用途：原版誓约能力的默认限制是「**本回合部署的才能激活**」
        /// （`CardScript__IsTheSameTurnPlayed.c:24-38`），
        /// 由 `oathInAllTurns` 豁免（`CardScript__CanUseOathAbility.c:8`）。
        /// 全仓原来**没有任何「部署回合」字段**，这是唯一一处写点（`RuleCore.PlayCard` / `DeployFree`
        /// / `TutorialScript.PlaceTroops` —— 🔴 2026-10-19（`A1990`）补上第三个写点：
        /// 教程起始单位也会写它，原来这句只列了两个入口）。
        /// </summary>
        public int DeployedTurn = -1;

        /// <summary>🔴 2026-10-21（引擎线 `T03·c` 批 A · `W59`）：**单位进入战场的先后号**
        /// （`BattleContext.TakePlayOrder` 发的单调递增号；`-1` = 从没取过号）。
        ///
        /// **原版判据**（第一权威 → `资料/普查产出_1020/W58_战斗结算_多词条顺序判据.md` §一·A1）：
        ///   `BattleManager.allCardsInPlay`（字段 `+0x470`）是**「进场顺序」的追加列表** ——
        ///   `SetupFullHero.c:43-70`（督军）· `CreateStartingTroop.c:71-100`（起始兵）·
        ///   `_ResolvePlayCardFromHand_d__447__MoveNext.c:709-711`（手牌打出）·
        ///   `_ResolveSummonUnit_d__510__MoveNext.c:179-183`（召唤）全是 `List.Add`；
        ///   死亡是 `_ResolveMinionDeath_d__454__MoveNext.c:56-59` 的 `List.Remove`。
        ///   所有 `Broadcast*` 族都**照这个列表遍历** ⇒ **多单位「谁先响」= 谁先进场**，
        ///   ⛔ **不是棋盘格号、不是座位序、不是卡 id / 卡名**。
        ///
        /// 🔴 **每次进场都重取**（不是首次生成时固定一次）：原版死掉 = `Remove`、
        ///   复活 / 再造 = 重新 `Add` 到**表尾** ⇒ 它**不能复用 `InstanceId`**
        ///   （那是「建卡序」—— 同一份实例复活之后号不变，但原版里它的位置已经跑到表尾了）。
        ///   写点（= `TestNewUnitEntryList` 那 4 个正路 + 2 个特例，另加控制权归还）：
        ///     · `RuleCore.BuildPlayer`（督军）· `RuleCore.PlayCard` · `RuleCore.DeployFree`
        ///     · `EffectResolver.DoReanimate`（残骸翻回来 = 重新进场）· `TutorialScript.PlaceTroops`
        ///     · `RuleCore.CleanupDeaths`（残骸翻面**沿用**被翻面那张的号 —— 原版是否重排判据未闭合）
        ///     · 控制权归还（`RuleCore` 里 `TempControls` 到期那一跳 —— 重新入场，判据待核）
        ///   ⚠️ **夹具也要取号**：`RuleEngineTest.Place` 不取的话夹具里全是 `-1` ⇒
        ///     「按进场序排序」会**静默退化成原顺序**（排序等于没测）。
        ///
        /// ⚠️ 负数（`-1`）= 没取过号 ⇒ **排序时排在末尾**（见 `RuleCore.StableSortByPlayOrder`）。
        /// </summary>
        public int PlayOrder = -1;

        /// <summary>
        /// 🔴 **2026-10-24（引擎线 `G5`）：原版 `CardScript.referencedCards // +0x278` 的运行期列表**
        /// （`d:/2/tools/il2cpp_out/dump.cs:35480` 的 `<referencedCards>k__BackingField`，属性 `:35549`，
        /// `List&lt;CardScript&gt;`）。
        ///
        /// <para>**它不是卡 SO 字段**（`RawCardScript` 上没有它，`cards_raw.json` 的 116 个键里也没有）
        /// ⇒ **不落卡表、不序列化**、每局从空开始。数据面判据 →
        /// `资料/普查产出_1024/C5_剩余判据普查.md` §3。</para>
        ///
        /// <para>**写点（唯一一个）= 能力 `380 setReferenceCard`**
        /// （`AbilityLogic__PlayAbility.c:2128-2160`，`iVar8 == 0x17c`）：
        /// 先把 `actingCard.referencedCards` **换成一张新 List**（`:2139-2143` 直写 `+0x278`），
        /// 再把**那条 abilityLogic 自己的目标表**逐个 `Add` 进去（`:2144-2160`）。
        /// 我们的消费者 = <see cref="RuleCore.SetReferenceCards"/>（形状照抄：先清空再 `AddRange`）。</para>
        ///
        /// <para>**读点（原版 `targetsAffected` 两档）** —— 都收在
        /// `EffectResolver.ResolveTargets` 的最前面（按 `EffectOp.SubjectTargetsAffected` 分派）：
        ///   · `200 referencedCards`（`AbilityLogic__GetTargets.c:770-781`）= **整表 `AddRange`**，不过筛、不 Trim；
        ///   · `201 referencedCardsInPlay`（`:784-806`）= 同上，但**只留 `IsInPlay` 的**
        ///     （`CardScript__IsInPlay.c:7`：`cardState ∈ {2,15,3,17}`；我们的等价物 = `RuleCore.IsUnitInPlay`）。
        /// </para>
        ///
        /// ⚠️ **挂在 `UnitState` 而不是 `CardInstance` 的理由（如实标）**：原版载体是 `CardScript`
        /// （**手牌 + 场上**同一套），而本批白名单不含 `Core/CardInstance.cs`；场上的 `UnitState`
        /// 已经持有 `Instance`，是当前能做的最小落点。⇒ **从【手牌】打出的战术卡**（`AM54 At All Costs`
        /// 那条 380 就属于这种）在本结构里**没有存放处** —— 这条缺口**未做**，见
        /// `资料/普查产出_1024/G5_裸Deal与ta200.md` §件2。
        ///
        /// ⚠️ **今天不改任何一局的行为**：我们的引擎**不跑 SO `abilityLogic`**，380 没有自然调用点，
        /// 200/201 也没有任何 op 带得到（两张卡的卡面效果由文字管线覆盖，见报告 §件2）。
        /// 与 `UnitState` 上其它字段一样：**由 `BattleContext` 发号、同种子必然同结果**（不掷骰、不用 Guid）。
        /// ⚠️ 它是**引用列表**（同一批单位对象），不是快照 —— 与 `ctx.LastTargets` 同一种「引用了谁」的语义。
        /// </summary>
        public readonly List<UnitState> ReferencedCards = new List<UnitState>();

        readonly Dictionary<string, int> _keywords;

        /// <summary>
        /// 🆕 **2026-10-25（引擎线 `A2` · `A2-2`）：「已移除 trait」簿记** ——
        /// 原版 `EntityScript.HasRemovedTrait(card, trait)`（`d:/2/tools/decomp_full/EntityScript__HasRemovedTrait.c`）
        /// 遍历的**不是**当前 trait 表（`CardScript +0x128`），而是 `activeEffects`（`+0x108`）里
        /// **`CardEffect.buffType == removeTrait(4)`**（`BuffType.cs`）且 `trait(+0x3C) == 查询值` 的条目。
        /// ⇒ 「曾经被**移除效果**摘掉过」与「**当前**带不带」是两件事（只在「有过、后来被移除」时不同）。
        ///
        /// <para>**写入点** = `EffectResolver.ApplyOneGain` 的 `lose &lt;trait&gt;` 那一支
        /// （= 原版加一条 `CardEffect{buffType=removeTrait}`；原版 `AddEffect` 的 `case 4` 见
        /// `CardScript__AddEffect.c:680-694`：先 `RemoveActiveEffects(4, trait)` 去重、再 `List.Add`
        /// 这条效果、最后 `RemoveBuffedTrait + RemoveTrait`）。</para>
        /// <para>**清除点** = <see cref="AddKeyword"/>（= 原版 `AddEffect` 的 `case 3`：`case 3` 进门第一句是
        /// `CardScript__RemoveActiveEffects(card, 4)` —— **不带 trait 过滤 ⇒ 清掉全部 removeTrait 记录**
        /// （`CardScript__AddEffect.c:474`）。⚠️ 这条看着怪，但它是原版行为，照抄并留着。</para>
        /// <para>⚠️ **不写账的路径**（原版也不写，因为那几条走的是 `RemoveTrait` / `RemoveTraitAndEffects`
        /// **本身**，不是「加一条 removeTrait 效果」）：护盾/闪避消耗、开空投舱、失明/眩晕到期、
        /// 伏击翻开（`RemoveTraitAndEffects`）、光环收回。⇒ 我们的 `RemoveAll` **不写账**，
        /// 只有 `ApplyOneGain(lose)` 那一处调 <see cref="MarkTraitRemoved"/>。</para>
        /// </summary>
        readonly HashSet<string> _removedTraits = new HashSet<string>();

        /// <summary>🆕 `EntityScript.HasRemovedTrait(trait)` —— 这条 trait **曾经被「移除效果」摘掉过**。
        /// 判据/写入点/清除点全在 <see cref="RemovedTraits"/> 那段 doc 里（⛔ 别拿 `Has()` 顶替：
        /// 那个问的是「**当前**带不带」）。⚠️ 今天唯一消费者 = `ConditionEval` 的 `280 siege`
        /// （`CardScript__DoesAbilityRequireLoading.c:35` 的 `HasRemovedTrait(0x1ea = 490 reload)`）。</summary>
        public bool HasRemovedTrait(string trait)
        {
            return !string.IsNullOrEmpty(trait) && _removedTraits.Contains(trait);
        }

        /// <summary>🆕 记一条「已被移除」的 trait（写入点见 <see cref="RemovedTraits"/> 那段 doc）。
        /// ⚠️ 只由 `ApplyOneGain` 的 `lose` 支调用；内部幂等。</summary>
        public void MarkTraitRemoved(string trait)
        {
            if (!string.IsNullOrEmpty(trait)) _removedTraits.Add(trait);
        }

        /// <summary>🆕 只看不写（自检/报表用）：这本账里现在有哪些 trait。</summary>
        public IReadOnlyCollection<string> RemovedTraits { get { return _removedTraits; } }

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

            AttackBase = card != null ? card.Attack : 0;
            RangedAttackBase = card != null ? card.RangedAttack : 0;
            Health = card != null ? card.Health : 0;
            MaxHealthBase = Health;

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

        /// <summary>
        /// **当前带不带这个关键词** —— 原版 `EntityScript.HasCurrentTrait(trait)` 的投影。
        ///
        /// 🔴 **2026-10-26（`E10TB` · P-05 ①）：补上原版的【残骸闸】，并给残骸单开一本书。**
        /// 判据 = `decomp_full/EntityScript__HasCurrentTrait.c:1-75`（🥇 全文逐句）：
        /// ```
        /// if (isRemnant(+0x65) == 0 || isLeavingRemnant(+0x66) != 0) {
        ///     CurrentTraits(+0x128) 里有 ⇒ true; 否则
        ///     activeEffects(+0x108) 里 { buffType==3(addTrait), trait 命中 } 有 ⇒ true;
        ///     否则 false
        /// } else {                       // = 残骸 ∧ 不在离场窗口
        ///     activeEffects 里 { buffType==0x5a(addTraitRemnant=90), trait 命中 } 有 ⇒ true;
        ///     否则 false                // 🔴 **卡面印的那一份在残骸上【不算】**
        /// }
        /// ```
        /// ⇒ **残骸身上的 `Has` 只认 90 档授予的**（`90` 池内 2 条：`SAU36` / `eab54e4c…`，
        ///    卡面写法 `When … becomes a Remnant it gains …`，走 `EffectResolver` 的闸③ 唯一放行路）。
        ///
        /// ⚠️ **为什么需要一本书而不是只靠 `ClearAllKeywords`**：`RuleCore.CleanupDeaths` 造残骸时
        ///   已经调过 `ClearAllKeywords`（卡面那批确实清掉了），但**之后再挂上来的**关键词
        ///   （90 档授予 / 别的直接写点）若仍读同一本 `_keywords`，残骸就会「看得见非 90 的 trait」——
        ///   与原版差一个闸，且**只在残骸身上现形、不报错**。⇒ `_remnantTraits` 记「残骸期间授予的」，
        ///   `Has` 在残骸档只读它（见 <see cref="_remnantTraits"/> 的 doc）。
        /// ⚠️ **`KwValue` 不带这个闸**（原版 `GetCurrentTraitValueWithModifiers.c:1-80` 里**没有**
        ///   `isRemnant` 判断）⇒ 那一位保持原样，⛔ 别顺手一起改。
        /// </summary>
        public bool Has(string keyword)
        {
            if (IsRemnant && !IsLeavingRemnant) return _remnantTraits.Contains(keyword);
            return _keywords.ContainsKey(keyword);
        }

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

        /// <summary>带了这个关键词、且它带的效果文字解析不出来 → 卡面要标 `*`。
        ///
        /// 🔴 **2026-10-12（`A1618`）改**：判据原来问的是 `Card.Effect(keyword)`（**`EffectSpec`
        ///   那台封闭文法**）—— 对**原版卡**那是**问错了层**，会把「正文真的会跑」判成「解析不出来」。
        ///   实测代价：全池带 `rally`/`strike`/`slay`/`backlash`/`penitence` 的 **122 处**，
        ///   `EffectSpec` 命中 **0/122**、而真正喂给结算器的 `TriggerOps` 命中 **122/122**
        ///   ⇒ 那 122 处**全是假阳性**（`EffectNotes` 那一格会照它打 `RALLY*` 之类的星号）。
        ///   ⇒ 现在统一问 <see cref="CardDef.BodyResolves"/>（**全类唯一判据，按卡的来源分流**），
        ///   并保留「运行时挂上来的那一份也算会跑」（`FxOps` 的取法：卡上原生的优先、没有才用挂的）。
        ///
        /// 🔴 **2026-10-21（`A1631`）再收一格 —— 改问 <see cref="CardDef.BodyFullyResolves"/>**：
        ///   `BodyResolves` 只说「**有没有**正文可跑」，**说不了「那整段正文全懂了没有」** ——
        ///   同一段正文里解得出的收下、解不出的原来**不留痕**。实测：`EC8 Alluress` 的 `rally`
        ///   第二句（`Friendly Daemonette have Flank`）整句认不出，而 `BodyResolves(rally) == true`
        ///   ⇒ 本方法会判「解析得出来」、卡面不打 `*`（**假绿**）。
        ///   `BodyFullyResolves` = `BodyResolves` ∧ 没有半懂账目（<see cref="CardDef.TriggerGap"/> 为空）
        ///   ⇒ 半懂那一档也如实打 `*`（红线：不许静默失败）。
        ///   ⚠️ **本方法今天【没有调用点】**（卡面那个 `*` 走的是 `BattleDriver.EffectNotes` /
        ///   `EffectText.IsFullyParsed` 那两条）—— 本条改动**不动任何可见行为**，
        ///   它管的是「以后谁来问这一格」。
        /// </summary>
        public bool EffectUnparsed(string keyword)
        {
            if (Card == null || !Card.Has(keyword)) return HasGrantedEffect(keyword) == false;
            if (Card.BodyFullyResolves(keyword)) return false;
            return !HasGrantedEffect(keyword);
        }

        public int KwValue(string keyword)
        {
            int v;
            return _keywords.TryGetValue(keyword, out v) ? (v > 0 ? v : 0) : 0;   // 🔴 原版 `GetCurrentTraitValueWithModifiers` 的 `num5 < 0 ⇒ 0` 夹紧（判据见类尾 `RawKw` 那一节）
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
        public bool AddKeyword(string keyword, int value)
        {
            if (string.IsNullOrEmpty(keyword)) return false;

            // 🔴 **D3/D4（第三段）：毒 × 抗性 —— 两个方向都要拦**（判据 `判据/战斗逻辑驱动_Stage2前置_第2轮.md` §2·1 S3 / §2·2）
            //   · **加毒时**身上有 `resistant` ⇒ **这次中毒作废**（`CardScript.cs:17631-17643`，原版还会
            //     `HighlightTraitIcon(resistant)` + `DisplayStatusAnim(resistant)` —— 那是**表现层**，我们没有对应层，
            //     由调用方（`EffectResolver.ApplyOneGain`）打日志出声）。
            //   · **加抗性时**身上有毒 ⇒ 见下面 S9 那一段（整条摘毒）。
            //   ⚠️ 判据只此一处（D4 要求「`UnitState` 或 `EffectResolver` 二选一」⇒ 选 `UnitState`，
            //     因为它是**所有**授予路的唯一收口，光环/附魔/效果都过它）。
            if (keyword == KeywordTable.Poisoned && Has(KeywordTable.Resistant)) return false;

            // 🔴 **D2（第三段）：`_stackable == count(20)` 的 trait 每次只 +1**（判据 §2·3）
            //   原版 `CardTrait._stackable`：本 build **74 条 `add(10)`、1 条 `count(20)`（`markOfChaos(330)`）**、
            //   0 条 `replace(0)`（真值表 → `判据/CardTrait_stackable_真值表_第2轮.md`）。
            //   `count` 那一路：合并时 `旧 += 1`（**不看 `E.traitValue`**）、新挂时 `E.traitValue = 1`（S8①）
            //   ⇒ 净效果 = **每次施加 +1**。
            int add = (value > 0 && IsCountStackableTrait(keyword)) ? 1 : value;
            _keywords[keyword] = RawKw(keyword) + add;
            // 🆕 2026-10-25（引擎线 `A2` · `A2-2`）：**加一条 trait 会清掉「同名的已移除」簿记**。
            // 🔴 **2026-10-25 第三段就地订正（`E1` · 铁律 5 —— 这一条不只是注释错，是【行为错】）**：
            //   改前这里是 `_removedTraits.Clear()`（**整本清掉**），注释声称「原版 `RemoveActiveEffects(4)`
            //   不带 trait 过滤、照抄」。**那个口径来自 Ghidra 少印实参的假象**：
            //     · `decomp_full/CardScript__AddEffect.c:474` = `CardScript__RemoveActiveEffects(param_1, 4);`（**只有 2 个实参**）
            //     · `out_cpp2il_dev_ilspy/CardScript.cs:17651` = `RemoveActiveEffects(BuffType.removeTrait, newEffect.trait);`
            //     · `out_il2csharp/Assembly-CSharp/CardScript.cs:10855` = `this.RemoveActiveEffects(BuffType.removeTrait, newEffect.trait);`（**第二份独立 IL**）
            //   两份 IL 一致、且 `CardScript__RemoveActiveEffects.c:34-40` 自证「`param_3 != 0` ⇒ 按 `+0x3c`(trait) 过滤」
            //   ⇒ **原版只清 `T` 这一条**。裁定全文 → `判据/战斗逻辑驱动_Stage2前置_第2轮.md` §三·1。
            //   ⚠️ 症状（改前）：加**任何** trait 都会把「已移除」整本清空 ⇒ `HasRemovedTrait(reload 490)`
            //   在「摘掉 reload 之后又拿到别的 trait」时**假变真**（`ConditionEval:468` 的 siege 280 会误判可装填）。
            if (_removedTraits.Count > 0) _removedTraits.Remove(keyword);
            SyncKeywordState(keyword, add);
            // 🔴 2026-10-26（`E10TB` · P-05 ①）：**残骸期间挂上来的**另记一本（`Has` 的残骸档只读它）。
            //   原版残骸只认 `activeEffects` 里 `buffType 90 (addTraitRemnant)` 那一条；而闸③
            //   （`CardScript__AddEffect.c:159-170`）对残骸**只放行 90 档与生命增减**
            //   ⇒ 「残骸期间能被挂上的 trait」就是 90 那一批 ⇒ 按「此刻是残骸」记账与逐条判 90 等价。
            if (IsRemnant && !IsLeavingRemnant) _remnantTraits.Add(keyword);

            // 🔴 **D3（第三段）· §2·2 逐 trait 特例 —— 只做「池内 0 入口」那几条里还能落地的**。
            //   ⚠️ **池内现读**（`cards_truth.json`，`buffType ∈ {3,90}` 全卡扫）：`poisoned(200)` /
            //     `resistant(600)` / `survivor(430)` / `tide(910)` / `companion(1180)` / `pindown(970)` /
            //     `dropPod(230)` **一条都没有** ⇒ 下面这几条**今天零可观察量**，按铁律 11 照样实现。
            //   · **加抗性时身上有毒 ⇒ 整条摘毒**（`CardScript.cs:18142-18151`：`DisplayStatusAnim(resistant)`
            //     + `RemoveTrait(poisoned)`）。⚠️ 注意它走的是 `RemoveTrait`（**不是** `removeTrait` 效果）
            //     ⇒ **不写「已移除」簿记**，所以这里用 `RemoveAll` 而不是 `MarkTraitRemoved`。
            if (keyword == KeywordTable.Resistant && Has(KeywordTable.Poisoned))
                RemoveAll(KeywordTable.Poisoned);
            //   · **加 `survivor` ⇒ 静默摘 `survivorSpent`**（`CardScript.cs:18138-18141`
            //     `RemoveTraitSilently(survivorSpent=440)`）：`RemoveTraitSilently` **不碰面板/图标**，
            //     也**不写「已移除」簿记** ⇒ 直接动字典（绕开 `RemoveAll` 的副作用那一串）。
            if (keyword == KeywordTable.Survivor) _keywords.Remove(KeywordTable.SurvivorSpent);
            //   · **`dropPod(230)` ⇒ 重置成默认值**（`CardScript.cs:18129-18137`）：
            //     `currentDropPodHealth = dropPodMaxHealth` · `currentDropPodAttack = dropPodAttack`。
            //     ⚠️ 我们**没有 dropPod 这两个字段**（`RuleCore.OpenDropPod` 那条链是另一套）⇒
            //     **未建**，如实留在这里（池内 0 入口）。
            //   · **`tide(910)` / `companion(1180)` 的计数器快照**：我们的 `CardInstance.CompanionCounter`
            //     是「伴生链剩余环数」（`RuleCore.PlayCompanions` 写），**与「快照 CurrentCompanion」
            //     不是同一个东西**（判据 §四·5 也如实标「未核」）⇒ **不改**，只记在这里。
            //   · **`pindown(970)` 强制远程** 已在 `SyncKeywordState` 里（`AttackTypeRanged`）—— ✅
            //   · **`markerlight(1170)` / `huntMark(1260)` 的广播**在 `EffectResolver.ApplyOneGain`
            //     （与 `EmitStatusApplied` **不同事件种类**，见那里的注释：不双响）。
            // 下面这条 `fast`/`flank` 是**已有**的（判据 `.c:570-572`），保留在原地。
            // 🔴 🆕 **2026-10-25（引擎线 · 真机报告「给部队加上侧翼，当回合仍不能进攻」）：
            //    `fast` / `flank` 一落地，就要重算「能动 / 能攻击」两位。**
            //    判据 = 原版 `CardScript__AddEffect.c` 的 `case 3`（= 加 trait）`:570-572`：
            //      `if (*(int *)(param_2 + 0x3c) == 0x28 /*fast*/ || … == 0x1cc /*flank*/)
            //           CardScript__UpdateSummonSicknessAnims(param_1);`
            //    而 `UpdateSummonSicknessAnims.c:20` 转调 `ActivateMinion` —— 两个位一起按
            //    `displaySummonSickness = +0x58 && !fast && !flank` 重写（见
            //    `RuleCore.RecomputeActAndAttack` 的 doc，整条链在那儿）。
            //    ⚠️ **只对这两个词做**：原版别的 trait 加进来**不走这一跳**（它们不参与那个 getter）。
            //    ⛔ 别顺手把 `ferocity`/`oath` 也接上 —— 它们抬 `canAct` 的那一次是
            //       `ActivateMinion` 里的**第二写点**（部署那一刻求值），不是「加词就抬」。
            if (keyword == "fast" || keyword == "flank") RuleCore.RecomputeActAndAttack(this);
            return true;
        }

        /// <summary>
        /// **`CardTrait._stackable == count(20)` 的 trait** —— 每次施加 **只 +1**（不看给的值）。
        /// 判据 = `判据/战斗逻辑驱动_Stage2前置_第2轮.md` §2·3 + `判据/CardTrait_stackable_真值表_第2轮.md`：
        /// 本 build 75 个有 SO 的 trait 里 **74 个 `add(10)`**、**1 个 `count(20)` = `markOfChaos(330)`**
        /// （`berzerk(360)` / `dodge(240)` 原版就没有 SO）。
        /// ⚠️ 判据是「逐 trait 的 SO 真值」；这里**只写死那一条已经实读定案的 `count`**，
        ///    ⛔ 别凭感觉再加（加错一条会让那条 trait 永远只能叠到 1）。
        /// </summary>
        public static bool IsCountStackableTrait(string keyword)
        {
            return keyword == KeywordTable.MarkOfChaos;   // 330 · `count(20)`
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
            int now = RawKw(keyword) - value;
            if (now > 0)
            {
                _keywords[keyword] = now;
                // 🔴 **2026-10-19（`T01 §六·6`②）：护甲的【反方向】也要同步。**
                //   改前这一支只动 `_keywords`、不动 `Armor`（`AddKeyword` 走
                //   `SyncKeywordState` 会把字段一起加上，减这一半却没人补）——
                //   同一件事两种表示脱节 = `Armour 2` 被 `lose 1 Armour` 减到 1 之后，
                //   `KwValue("armour") == 1` 而 `u.Armor` 仍是 **2**（减伤照旧按 2 算）。
                //   判据与 `A2015`（`EffectResolver.ApplyOneGain` 的 `case "armour"`）同一条：
                //   全仓护甲读点是 `KwValue` / `Has` / `UnitState.Armor`，
                //   而 `Armor` 在构造时就是 `KwValue(armour)`（本文件 `:594`）⇒
                //   两边必须逐值相等。
                //   ⚠️ **整份摘掉那一支不用在这里补** —— `RemoveAll` 里已经有
                //   `if (keyword == Armour) Armor = 0;`（`:850`）；这一行只管「减一层」。
                if (keyword == KeywordTable.Armour)
                    Armor = System.Math.Max(0, Armor - value);
            }
            else RemoveAll(keyword);
        }

        /// <summary>
        /// **整个摘掉**（不管叠了几层）。
        /// 规则书里明确说「移除全部」的地方必须用这个 —— 例：`Markerlight X`
        /// 「受远程伤害后**移除全部**标记光」（:192）；用 `RemoveKeyword` 只会减一层。
        ///
        /// ⚠️ 摘掉关键词时**连带撤掉它当初授予的属性增益** —— 见 <see cref="PendingGrants"/>。🆕 **2026-10-26（第 4 轮 · `F3` · 任务 R4）`silent` = 原版 `CardScript.RemoveTraitSilently` 的等价位**（摘 trait 但**不重算**「能动/能攻击」；判据 = `CardScript__RemoveTrait.c:66-68` 对 flank/fast 会调 `UpdateSummonSicknessAnims`，而 `CardScript__RemoveTraitSilently.c` 全文 55 行**一次都没调**；两个原版调用点 = `CardScript__RemoveEffect.c:69` · `CardScript__TransformIntoRemnant.c:74`）。
        /// </summary>
        public void RemoveAll(string keyword, bool silent = false)
        {
            if (string.IsNullOrEmpty(keyword)) return;
            _keywords.Remove(keyword);
            _remnantTraits.Remove(keyword);   // 🆕 2026-10-26（`E10TB` · P-05 ①）：残骸那本账同步摘
            if (keyword == KeywordTable.Armour) Armor = 0;
            // 🔴 **2026-10-26（第 4 轮 · `F3` 去重）**：本段原来还有**第二份**一模一样的
            //   `if (keyword == "fast" || keyword == "flank") RuleCore.RecomputeActAndAttack(this);`
            //   （两笔不同批次的改动各加了一份）⇒ 「同一件事实写两遍」，已删掉这一份；
            //   唯一保留的那一处在本方法**末尾**（那里紧挨着 `RemoveTrait.c:66-68` 的逐行判据，
            //   并且是 `silent` 唯一生效的地方）。行为不变（重算是幂等的）。
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
            // 🔴 🆕 **2026-10-25（同 `AddKeyword` 那一笔的另一半）：摘掉 `fast` / `flank`
            //    ⇒ 召唤病**可能回来**，两位要重算。**
            //    判据 = 原版 `CardScript__RemoveTrait.c:66-68`：
            //      `if (trait == 0x1cc /*flank*/ || trait == 0x28 /*fast*/)
            //           CardScript__UpdateSummonSicknessAnims(param_1);`
            //    （→ `ActivateMinion` 按新的 `displaySummonSickness` 重写 `canAct`/`canAttack`：
            //      摘掉侧翼之后 `displaySummonSickness` 又为真 ⇒ 两位重新变假。）
            //    ⚠️ **光环那份收回不走这里**（`ClearAuraGrants` 是直接写 `_keywords` 的）——
            //      原版「光环给/收关键词」走哪条路**还没查清**，如实留着（见
            //      `RuleCore.RecomputeActAndAttack` 的 doc 末段）。
            // 🔴 **2026-10-26（第 4 轮 · `F3`）：`silent` 唯一生效的地方就是这一行** ——
            //    `silent = true` = 原版 `RemoveTraitSilently`（**不重算**，`canAct`/`canAttack` 停在旧值）。
            if (!silent && (keyword == "fast" || keyword == "flank")) RuleCore.RecomputeActAndAttack(this);
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
            /// <summary>🆕 2026-10-25（Stage 2）：这条账在**修饰表**里对应的那一格（`armour` 为 `null` —— 护甲走关键词）。
            /// ⚠️ **数值只在 `_statMods` 算一次**；本类只留「谁给的 / 关键词带出来的」这两件元数据（`PendingGrants` 的读点还在用）。</summary>
            public StatMod Mod;
        }

        readonly List<GrantRecord> _grants = new List<GrantRecord>();
        public IReadOnlyList<GrantRecord> PendingGrants { get { return _grants; } }

        /// <summary>
        /// 记一条「这个属性增益是谁给的」，**并立刻加上去**。
        /// `keyword` 非空 = 是某个关键词带出来的（关键词被移除时一起撤）；
        /// `source` = 给予者（比如黑暗契约的种类名），用来在来源被替换时整份收回。
        /// </summary>
        /// <returns>🆕 2026-10-25（Stage 2）：这条账在修饰表里的那一格（`armour` 返回 `null`）。</returns>
        public StatMod RecordGrant(string attr, int value, string source = null, string keyword = null)
        {
            var g = new GrantRecord { Keyword = keyword, Attr = attr, Value = value, Source = source };
            _grants.Add(g);
            g.Mod = AddStatMod(attr, value, source, IsExtrinsicSource(source));
            ApplyGrant(g, +1);
            return g.Mod;
        }

        /// <summary>
        /// 这个来源算不算**原版 `extrinsic`**（`CardEffect.extrinsic // +0x30`）。
        /// 判据 §7·1：`extrinsic = (ability.trigger ∈ {20 WhileInPlay, 240 WhileInPlayVariable})`
        /// （枚举/写点/落点逐句见 <see cref="CardDef.IsExtrinsicAbilityTrigger"/>）。
        ///
        /// <para>🔴 **我们这侧只有光环那条路**是 while-in-play 的载体（`Core/Aura.cs:180-181`：
        /// 「原版把这一族做成 `AbilityTrigger.WhileInPlay = 20` 的**常驻**能力」），而 `Auras` 的
        /// **正文形状**判据对 trigger 20 与 240 是同一套（实测 20: 11/19 段、240: 27/42 段被收下，
        /// 其余是印刷关键词或同卡上其它 trigger 的一次性效果）⇒ **`Auras.GrantTag` 就是 `extrinsic` 的运行时刻判据**。</para>
        ///
        /// ⚠️ **这条是「卡级近似」之外的那一半**：卡级有没有 extrinsic ability 看
        /// <see cref="CardDef.ExtrinsicAbilityPresence"/>；而**具体某一条增益**是不是 extrinsic，
        /// 只能看它走的是哪条授予路（光环 ⇒ 是）。⛔ 别用卡级 flag 逐句判（同一张卡上
        /// `Rally(30)` 与 `WhileInPlay(20)` 并存时会把 Rally 也给成 extrinsic）。
        ///
        /// 📌 **可观察量（如实记）**：`extrinsic` 只影响三处 ——
        /// ① `CurrentMaxHealthProcessed` 里「`setHealth(9)` 只覆盖非 extrinsic 的 base」那一支
        ///   （池内 `setHealth` **0 张** ⇒ 今天不可观察）；
        /// ② `CurrentIntrinsicHealth` ③ `CurrentIntrinsicMeleeAttack`（**两个都零调用者**）。
        /// ⇒ **本笔用行为验不了它**；下面「②」的验收只有结构读数（`StatMods` 里的 `Extrinsic` 位）。
        /// </summary>
        public static bool IsExtrinsicSource(string source)
        {
            return source == Auras.GrantTag;
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
                // 🔴 **2026-10-25（Stage 2）**：`attack` / `ranged` **不再写字段** ——
                //   数值已经在 `_statMods` 里（`RecordGrant` 建的那条），投影现算。
                //   撤销（`sign < 0`）时把那条修饰**摘掉**即可。
                case "attack":
                case "ranged":
                    if (sign < 0) RemoveStatMod(g.Mod);
                    break;
                // 🔴 **2026-10-25（Stage 2）**：生命**加的时候当场加当前生命**（原版 `addStats.health`
                //   就是 `currentHealth += healthChange`），**上限靠投影现算**。
                //   撤销时**只夹、不减** —— 原版 `RemoveEffect.c:147-181`
                //   `currentHealth = min(currentHealth, 新 currentMaxHealthProcessed)`（判据 §7·4）。
                //   ⚠️ 以前这里是 `Health -= v; MaxHealth = Max(1, MaxHealth + v)`（直接减）——
                //     只要在「挂着血 buff 的期间掉过血」就会多掉一截。
                case "health":
                    // 🔴 **2026-10-25（第四段 · H1 裁定「区分重算与真移除」）**：
                    //   `_recomputingAura` 为真 = 正在「**重算**」（`Auras.Recompose` 的先清后加），
                    //   这时**一个字节都不许动 `Health`** —— 由 `EndAuraRecompute` 在本次重算**结束时**
                    //   按「光环血修饰总量」的**前后差**统一结算（+差 ⇒ 加；−差 ⇒ 夹）。
                    //   否则「清（夹到新上限）+ 加回（+= v）」每一步都赚一点 —— 那正是 H1 那个回血洞。
                    if (sign < 0)
                    {
                        RemoveStatMod(g.Mod);
                        if (!_recomputingAura) ClampHealthToMax();
                    }
                    // 原版 `AddEffect.c:331-346` 同口径：正加 = `currentHealth += healthChange`（不夹上限）；
                    // 非正 = `min(currentHealth, currentMaxHealthProcessed)`（**只夹、不减**）。
                    else if (g.Value >= 1) { if (!_recomputingAura) Health += g.Value; }
                    else { if (!_recomputingAura) ClampHealthToMax(); }
                    break;
                // 🔴 **2026-10-19（`T01 §六·6`①）：护甲要【两种表示一起动】。**
                //   改前这一行只写 `Armor`、不同步 `_keywords["armour"]` —— 和 `A2015`
                //   （`EffectResolver.ApplyOneGain`）**一字不差地同病**：黑暗契约 / 光环
                //   走 `RecordGrant("armour", …)` 这条路由本方法落地，之后
                //   `KwValue("armour")` / `Has("armour")` 仍是 0（给甲了、关键词没有）。
                //   判据 = `CardDef` 构造函数头部那段（`Core/CardDef.cs:269-271`）：
                //   全仓护甲读点都是 `_keywords` 那一格（含本文件 `:594` 的构造）⇒ 必须相等。
                //   ⚠️ **加减都走现成的那两个口**（`AddKeyword` → `SyncKeywordState` 会把
                //   `Armor` 一起加；`RemoveKeyword` 现在减那一支也会同步 —— 见它的注释）。
                //   ⛔ 这里**别**再自己写 `Armor += v`（会加两次）。
                //   ⚠️ **负甲 clamp 0 照旧**（原版 `:3320`）；`delta` 取夹过之后的实际增量。
                case "armour":
                {
                    int before = Armor;
                    int after = System.Math.Max(0, Armor + v);   // 原版 `:3320` 负甲夹 0
                    int delta = after - before;
                    if (delta > 0) AddKeyword(KeywordTable.Armour, delta);
                    else if (delta < 0) RemoveKeyword(KeywordTable.Armour, -delta);
                    break;
                }
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
            _keywords[keyword] = RawKw(keyword) + value;
            _auraKw[keyword] = AuraKwValue(keyword) + value;

            // ⚠️ 和 `AddKeyword` 一样**必须同步状态字段** —— 引擎别处是按字段结算的，
            //    只加 `_keywords` 不改字段 = 「给了护甲却不减伤」（原版 `:3293-3299` 专门补过这个 bug）。
            // 🔴 **2026-10-18（`W5`）**：这一段原来是**同一条规则的第二份写法**（只抄了
            //    `armour`/`shield`/`stun` 三条，**漏了 `blind` 与 `K3` 的攻击型那两条**）——
            //    现在收进 `SyncKeywordState`（**判据只此一份**，与 `AddKeyword` 共用）。
            //    ⚠️ 今天全池**没有任何**光环授予 `blind`/`pindown`（实测正则扫过 `have/has/gains …
            //    blind|pindown` **0 命中**）⇒ 这两条是**机制对齐**、不是行为变化。
            SyncKeywordState(keyword, value); RecomputeActAfterAuraKeyword(keyword);   // 🔴 L1376：`AddEffect.c:570-572` —— 加 `fast`/`flank` 那一跳必须重算两位
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
                bool needActRecompute = false;   // 🆕 光环把 fast/flank 收回去时也要重算那两位
                foreach (var kv in _auraKw)
                {
                    string kw = kv.Key;
                    int n = kv.Value;
                    int now = RawKw(kw) - n;
                    if (now > 0) _keywords[kw] = now;
                    else _keywords.Remove(kw);
                    if (kw == KeywordTable.Armour) Armor = System.Math.Max(0, Armor - n);
                    // 🔴🆕 2026-10-25（第 9 段）：**光环这一本账是直接改字典的**（不走 `RemoveAll`），
                    //   所以 `fast`/`flank` 被光环收回时**也要**重算「能动 / 能攻击」——
                    //   判据同 `CardScript.RemoveTrait`（`flank||fast ⇒ UpdateSummonSicknessAnims`）。
                    //   🔴 2026-10-27（B9 · `W18AR`）：**重算的【时机】改了** —— 本方法跑在 `Auras.Recompose`
                    //   的 ①「整份摘掉」里、紧随其后 ③ 就把关键词**原样重挂回**；在这儿按中间态重算是回归
                    //   （光环给的侧翼/迅捷被判成疲劳，`RE1`–`RE4`）⇒ 改走 `RequestActRecomputeAfterAuraChange`。
                    if (kw == "fast" || kw == "flank") needActRecompute = true;
                }
                _auraKw.Clear();
                if (needActRecompute) RequestActRecomputeAfterAuraChange();
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
        public partial class TempBuff
        {
            public bool IsKeyword;
            public string Name;              // 属性名（attack/health/armour/ranged）或关键词名
            public int Value;
            public int Owner;                // 施放者玩家号（`until your next turn` 按他的回合算）
            public bool UntilMyNextTurn; public bool UntilOpponentNextTurn;   // 判据 = CardScript__OnTurnSetup.c:+0x33/+0x34/+0x35 三档。my-next-turn=true ⇒ 施放者下回合开始撤 · opponent-next-turn=true ⇒ **对面**回合开始撤（Owner≠player）· 两者都 false ⇒ 本回合结束撤；UntilOpponentNextTurn 优先
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
            /// <summary>
            /// 🆕 2026-10-25（Stage 2）：这条限时增益在**修饰表**里的那一格（关键词那条为 `null`）。
            /// ⚠️ 到期撤销 = **把这一格摘掉**（不是「减回去」）—— 见 <see cref="RevertBuffs"/>。
            /// </summary>
            public StatMod Mod;
            /// <summary>
            /// 🆕 **2026-10-27（B9 · S0-①）**：**限时关键词**在 <see cref="TraitEffects"/> 里的那条记录
            /// （非关键词 / `val == 0` / `AddKeyword` 拒收时为 `null`）。
            /// 判据 = 原版连限时那条也在 `activeEffects(+0x108)` 里留一条 `CardEffect`（五个 `until*` 位之一为真），
            /// 而我们的限时路原来只 `AddKeyword`、不建记录 ⇒ 「一回合」的关键词在记录账里**没有来源**。
            /// <para>⚠️ **到期必须一起收**：`RemoveKeyword` 只减关键词值，**不会**摘这条记录 ⇒
            /// 三处撤法（<see cref="RevertBuffs"/> · <see cref="RemoveBuffsFromCard"/> ·
            /// <see cref="RemoveAllAppliedEffectsForJam"/>）都要调 <see cref="ExpireTraitEffect"/>，
            /// 否则会留下一本「到期了还在」的记录账（投影里那条会永远显示成「一回合」还挂着）。</para>
            /// </summary>
            public TraitEffectRecord TraitRec;
        }

        readonly List<TempBuff> _buffs = new List<TempBuff>();
        public IReadOnlyList<TempBuff> TempBuffs { get { return _buffs; } }

        /// <summary>
        /// 登记一条限时增益。
        /// 🔴 **2026-10-25（Stage 2）**：非关键词的那几条**在这里建修饰表那一格**（调用方不必自己建），
        /// 除非调用方已经建好并塞进 <see cref="TempBuff.Mod"/>（`ApplyOneGain` 的带来源那条路）。⚠️ `IsKeyword=true` 只 `_buffs.Add`、**不写 `_keywords`** ⇒ <see cref="Has"/>（只读 `_keywords`）看不到它；关键词必须由调用方**先** `AddKeyword`/`ApplyTraitEffect` 挂上（现有三条生产者都如此：`EffectResolver.cs:11102 AddKeyword` / `:12004 ApplyTraitEffect`）；⛔ 别在这里补 `AddKeyword`，那会和调用方**双记**关键词值。
        /// </summary>
        public void AddTempBuff(TempBuff b)
        {
            if (b != null && !b.IsKeyword && b.Mod == null)
                b.Mod = AddStatMod(b.Name, b.Value, null, false);
            _buffs.Add(b);
        }

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
                bool due = atTurnEnd ? (!b.UntilMyNextTurn && !b.UntilOpponentNextTurn) : (b.UntilOpponentNextTurn ? (b.Owner != player) : (b.UntilMyNextTurn && b.Owner == player));   // 判据 = decomp_full/CardScript__OnTurnSetup.c 的 +0x34 档（对面回合撤）
                if (!due) continue;
                if (RemoveTempBuff(b)) n++;   // 撤除规则只在 RemoveTempBuff 一处（判据一份）；账 L722 的「逐条撤」也复用它
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
                if (RemoveTempBuff(b)) n++;   // 🆕 2026-10-28（W4b）：撤除规则只在 RemoveTempBuff/RemoveTempBuffCore 一处（含 SyncAttackType，与改前逐条一致）
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
            // 🔴 **2026-10-14（账 `A1155②`）：「正在祈祷」不再在这里复位** ——
            //    它**不属于**「只刷当前行动方」的那一批。判据 = 原版 `CardScript__OnTurnStart.c:100`
            //    的 `+0x67 = 0` 对**全场**每一张在场卡各来一次
            //    （`BattleManagerSupport__BroadcastTurnStart.c:20-26` 扫 `CardsInPlay` 全场），
            //    而本方法**只被 `RuleCore.BeginTurn` 对 `p.Board`（当前行动方）调**。
            //    ⇒ 清的动作已搬到 `RuleCore.BeginTurn` 里那一圈（**不分敌我**，有逐行判据）。
            //    ⛔ **别再往这里加回来**：加回来只会让**对手**那一侧又慢一个回合才失状态
            //    —— 静默、且只在那半边现形。
            //    ⚠️ 卡面（读它的人）：`Each friendly unit that is Praying heals 3`（`Devout Serenity`）·
            //       `If any friendly unit is Praying, …`（`Sororitas Rhino`）（判据见 `UnitState.Prayed`）。
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

        // ================================================================================================
        // 🆕 2026-10-25（`E1` · 属性模型重构 **Stage 2**）—— 「当前值」改成**投影** + **修饰表**
        //
        // 【为什么有这一段】原版是「一半存档、一半现算」：存档的只有
        //   `currentHealth`(+0x68) / `currentMaxHealth`(+0xa4) / `currentArmourValue`(+0xb8) /
        //   `currentBaseMeleeAttack`(+0x7c) / `currentBaseRangedAttack`(+0x90)；
        //   而 `CurrentMeleeAttack` / `CurrentRangeAttack` / `currentMaxHealthProcessed` 都是
        //   **每次读时从 `activeEffects`(+0x108) 现算**的。
        //   我们以前把「当前值」物化成了字段（`ApplyOneGain` 直接 `Attack += val`）⇒ 模型差一层，
        //   可观察后果见判据 §6·4（`setMeleeAttack` 命中即 return、`blind ⇒ 远程 0` 只有一处生效…）。
        //
        // 【修饰表 = 原版 `activeEffects` 里「会改三围的那几档」的等价物】
        //   `_statMods` **按施加顺序**保存；`CurrentMeleeAttack` 等的实现**完全照抄**
        //   `get_CurrentMeleeAttack.c:29-67` / `get_CurrentRangeAttack.c:24-70` /
        //   `get_currentMaxHealthProcessed.c:22-108` 的扫描顺序与早退（见各 `Project*` 的注释）。
        //   ⚠️ **顺序有语义**：`setMeleeAttack(70)` / `setRangeAttack(85)` 是「**正序遍历、命中即 return**」
        //      ⇒ 多条时**最先施加的那条**胜出。别把它改成「最后一条」或「取最大」。
        //   ⚠️ **`_grants` / `_buffs` 不重复记账**：这两本账仍管「谁给的 / 什么时候到期」，
        //      但**数值只在 `_statMods` 里算一次**；两边靠 `GrantRecord.Mod` / `TempBuff.Mod` 互相指。
        //      ⛔ 别在投影里把 `_grants` / `_buffs` 再扫一遍（那就是「两处写同一条规则」）。
        //
        // 【本段替代了哪几处直写字段】判据 §6·2·A 那 20 个写点（编译器报的那 20 条 CS0200）。
        // ================================================================================================

        /// <summary>
        /// **一条「会改三围」的修饰** —— 原版 `CardEffect` 里
        /// `buffType ∈ {1 addStats, 70 setMeleeAttack, 85 setRangeAttack, 9 setHealth}` 那几档的等价物。
        ///
        /// <para>🔴 **2026-10-25（E1 · ① `doNotStack` 复核）：我们的模型【从不合并】= 永远等价于
        /// `doNotStack = true`，这是**有意**的，不是漏做。** 判据如下（全部现读）：
        ///  · 原版 `CardScript.AddEffect` 的 `addStats` 主体里有一个**合并**循环：同一个
        ///    `enchantmentCardId` + `buffType` + `markOfChaosType` + `daemonHostType == none` +
        ///    `mutationType == none` + **`!newEffect.doNotStack`** + `SameTimingAsEffect`
        ///    ⇒ **把四个增量字段累加进已有那条记录**，而不是新加一条（`CardScript__AddEffect.c:331-402`；
        ///    字段偏移实测：`+0xa4` = `enchantmentCardId`、`+0x38` = `buffType`、`+0x78` = `markOfChaosType`、
        ///    `+0x7c` = `daemonHostType`、`+0x80` = `mutationType`、**`+0x75` = `doNotStack`**，
        ///    按 `CardEffect.cs` 的字段序 + 对齐算出来的，与 `.c` 的用法逐个对上）。
        ///  · **`doNotStack` 的唯一写点** = `PlayerHand.AddHandEffect`（`PlayerHand.cs:2507` 现读：
        ///    `cardEffect3.doNotStack = true;` 然后才 `activeEffects.Add`）—— 也就是说
        ///    **只有「手牌效果」会被强制标成不合并**，目的就是让手牌那一份与场上的那一份**分开记账**，
        ///    各自到期各自撤。除此之外全库**没有任何写点**（`grep doNotStack` 只命中 `CardEffect.cs` 的定义/拷贝
        ///    与 `PlayerHand.cs:2507`）。
        ///  · **合并 / 不合并的数值完全一样**（合并 = 累加）。差别只在**「按记录摘」的粒度**：
        ///    合并后摘一条 = 把几次贡献一起摘掉；我们是按条独立、摘哪条掉哪条。
        ///  · 而原版**按记录摘**的场合恰恰是手牌效果（那里 `doNotStack` 恒 true ⇒ 原版也不合并）
        ///    ⇒ **两条路在我们这里合流成同一个行为**。
        /// ⚠️ 因此 **`cards_truth.json` 里那 22 条 `doNotStack` 是序列化残留**（运行时才由 `AddHandEffect` 覆盖），
        ///    ⛔ **不能当成「池内 22 条会不叠」**（与 `extrinsic` 那两列同一个坑）。
        /// 📌 实测读数（`_tmp_view/e1_probe2`）：连加两次 `+1 近战` ⇒ 5、条目 2；摘第一条 ⇒ 4（原版合并时 ⇒ 3）；
        ///    再摘第二条 ⇒ 3（两边一致）。**求和逐字节相同**。</para>
        /// </summary>
        public class StatMod
        {
            /// <summary>`"attack"` / `"ranged"` / `"health"`（已过 `EffectResolver.NormalizeAttr`）</summary>
            public string Attr;
            /// <summary>数值。`IsSet == true` 时是**绝对覆盖值**，不是增量。</summary>
            public int Value;
            /// <summary>
            /// 原版 `CardEffect.extrinsic`(+0x30)。判据 §7·1：`extrinsic = (ability.trigger ∈ {20 WhileInPlay, 240 WhileInPlayVariable})`。
            /// ⚠️ **我们只在「光环那条路」上标 true**（`Auras.GrantTag`）—— 因为 `Aura` 就是
            /// `WhileInPlay(20)` 那一族的宿主（`Core/Aura.cs:180-181`）。
            /// 🔴 **240 那一档没有单独标**（我们把它当普通效果处理）⇒ 如实记「未覆盖」，见报告「没查清」。
            /// </summary>
            public bool Extrinsic;
            /// <summary>`true` = `setMeleeAttack` / `setRangeAttack` / `setHealth` 那三档（**覆盖**语义）。</summary>
            public bool IsSet;
            /// <summary>`Source` = 来源标签（`Auras.GrantTag` / 黑暗契约种类 / `"虫群合并"` / `null`），只用于「整份收回」｜🆕 2026-10-26（`B8_Q`）`Trait` = 原版 `CardEffect.trait`(`+0x3c`，`DefinedTrait` 名)｜`BuffType` = 原版 `CardEffect.buffType`(`+0x38`)：`1` = `addStats`、`-1` = 明确不是从 `CardEffect` 来的、`0` = 未知（判据 → 类尾 `HasWhileInPlayAbility` ⑥ 那一段）。</summary>
            public string Source; public string Trait; public int BuffType;
        }

        readonly List<StatMod> _statMods = new List<StatMod>();

        /// <summary>修饰表的只读视图（自检 / 报表用）。</summary>
        public IReadOnlyList<StatMod> StatMods { get { return _statMods; } }

        /// <summary>
        /// 往修饰表里加一条（**不写任何字段**）。返回那条记录，方便调用方日后按引用摘掉。
        /// ⚠️ 加完**自己**调一次 `SyncAttackTypeAfterStatChange()` —— 原版 `UpdateAttackText`
        ///   被所有改属性的地方调到（见该方法注释）。🆕 `buffType` / `trait` = 原版 `CardEffect.buffType(+0x38)` / `CardEffect.trait(+0x3c)` 两列，**今天只有类尾 `HasWhileInPlayAbility` 的 ⑥ 支消费**；缺省 `buffType = 0` ⇒ 按 `isSet ? -1 : 1(addStats)` 推断（⚠️ `RuleCore` 虫群合并那条也走这一支、它不是 `CardEffect`，只是今天 `Trait` 为空 ⇒ 不影响任何结果）。
        /// </summary>
        public StatMod AddStatMod(string attr, int value, string source = null,
                                  bool extrinsic = false, bool isSet = false, int buffType = 0, string trait = null)
        {
            // 🔴 **2026-10-25（K1 / K8）**：原版 `CardScript__AddEffect.c:208-213` 对
            //   `buffType == 0x46 setMeleeAttack` / `0x55 setRangeAttack` **先**
            //   `RemoveActiveEffects(buffType)` —— 同一档的旧记录**先被摘掉**再挂新的。
            //   ⇒ 我们这里同样：`isSet` 的修饰**先清掉同属性的旧 set 记录**（永远只有一条）。
            //   ⚠️ 这同时也把「正序遍历、命中即 return ⇒ 最先那条胜出」那个顺序怪癖**在实际上抹平**了
            //     （因为永远只剩一条）。⛔ 但投影里的「命中即 return」**别删** —— 那是判据原文。
            if (isSet)
            {
                for (int i = _statMods.Count - 1; i >= 0; i--)
                    if (_statMods[i].IsSet && _statMods[i].Attr == attr) _statMods.RemoveAt(i);
            }
            var m = new StatMod
            {
                Attr = attr, Value = value, Source = source, Extrinsic = extrinsic, IsSet = isSet, BuffType = (buffType != 0 ? buffType : (isSet ? -1 : 1)), Trait = trait,
            };
            _statMods.Add(m);
            return m;
        }

        /// <summary>按引用摘掉一条修饰（不在表里就什么都不做 —— 幂等）。</summary>
        public void RemoveStatMod(StatMod m)
        {
            if (m == null) return;
            _statMods.Remove(m);
        }

        /// <summary>摘掉**某一个来源**留下的全部修饰（例如被替换掉的那份黑暗契约 / 一个光环）。</summary>
        public void RemoveStatModsFrom(string source)
        {
            if (string.IsNullOrEmpty(source)) return;
            for (int i = _statMods.Count - 1; i >= 0; i--)
                if (_statMods[i].Source == source) _statMods.RemoveAt(i);
        }

        /// <summary>
        /// 摘掉修饰之后把当前生命**夹到新上限**（原版 `RemoveEffect.c:147-181`：
        /// `currentHealth = min(currentHealth, 新 currentMaxHealthProcessed)` —— **只夹、不减**）。
        ///
        /// 🔴 **判据 §7·4**：我们以前是 `Health -= b.Value`（直接减）⇒ 只要在「挂着血 buff 的期间
        ///   掉过血」，到期时就会多掉一截（实测差 = 掉血量）。**这一条是 ② 合批的核心。**
        /// ⚠️ 别只断上限：两侧上限都是 4，断不出来的就是生命那一格。
        /// </summary>
        public void ClampHealthToMax()
        {
            int mx = MaxHealth;
            if (Health > mx) Health = mx;
        }

        /// <summary>
        /// **近战攻击力（当前值）** —— 原版 `EntityScript.get_CurrentMeleeAttack` 的投影。
        /// 实现见 `ProjectMeleeAttack()`（**类尾**那一段）。
        /// ⚠️ 名与旧字段逐字相同 ⇒ 全工程几百个读点**一个字都不用改**（2-甲的全部理由）。
        /// </summary>
        public int Attack { get { return ProjectMeleeAttack(); } }

        /// <summary>**远程攻击力（当前值）** —— 原版 `get_CurrentRangeAttack` 的投影（**含 `blind ⇒ 0`**）。</summary>
        public int RangedAttack { get { return ProjectRangeAttack(); } }

        /// <summary>
        /// **最大生命（processed）** —— 原版 `currentMaxHealthProcessed` 的投影。
        /// ⚠️ `Health`（当前生命）**不是投影** —— 原版 `currentHealth` 就是存档（判据 §6·2·C）。
        /// </summary>
        public int MaxHealth { get { return ProjectMaxHealthProcessed(); } }

        /// <summary>
        /// <see cref="CurrentMeleeAttack"/> 的实现 —— 逐句照 `EntityScript__get_CurrentMeleeAttack.c:29-67`：
        /// **正序**扫 ① `buffType == 1 addStats` ⇒ 累加 `meleeAttackChange`；
        /// ② `buffType == 70 setMeleeAttack` ⇒ **立刻 `return` 它的值**（连 base、连已累加的都不算）；
        /// ③ 扫完 ⇒ `clamp0(currentBaseMeleeAttack + Σ)`。
        /// </summary>
        int ProjectMeleeAttack()
        {
            int sum = 0;
            for (int i = 0; i < _statMods.Count; i++)
            {
                var m = _statMods[i];
                if (m.Attr != "attack") continue;
                if (m.IsSet) return m.Value;             // `:44-47` 命中即 return（正序 ⇒ 最先那条胜出）
                sum += m.Value;
            }
            int v = AttackBase + sum;
            return v > 0 ? v : 0;                        // `:52-66` clamp0
        }

        /// <summary>
        /// <see cref="CurrentRangeAttack"/> 的实现 —— 照 `EntityScript__get_CurrentRangeAttack.c:24-70`：
        /// **第一句**判 `HasCurrentTrait(0x3cf blind) ⇒ return 0`（`:24-28`），其余同近战那一支。
        ///
        /// 🔴 **这一句就是判据 §7·2 那 11 组差异的落点**：以前只有 `RuleCore.FieldAttack` 在
        ///   算伤害那一刻归零 ⇒ 条件 / 目标筛选 / AI / 卡面看到的都不是 0。改成投影之后**一次性全对**。
        /// </summary>
        int ProjectRangeAttack()
        {
            if (IsBlind) return 0;                       // `:24-28`
            int sum = 0;
            for (int i = 0; i < _statMods.Count; i++)
            {
                var m = _statMods[i];
                if (m.Attr != "ranged") continue;
                if (m.IsSet) return m.Value;             // `:41-49`
                sum += m.Value;
            }
            int v = RangedAttackBase + sum;
            return v > 0 ? v : 0;
        }

        /// <summary>
        /// <see cref="CurrentMaxHealthProcessed"/> 的实现 —— 照 `EntityScript__get_currentMaxHealthProcessed.c:22-108`：
        /// ① `base = currentMaxHealth`（= 我们的 `MaxHealthBase`）；
        /// ② `base += Σ(非 extrinsic 的 addStats.healthChange)`；
        /// ③ 存在 `setHealth(9)` ⇒ **`base` 整项被覆盖成它的 `healthChange`**（**不是加**）；
        ///    ⚠️ 原版倒序遍历 ⇒ 下标 0 最后写入 ⇒ **最先施加的那条生效**；
        /// ④ `+ Σ(extrinsic 的 addStats.healthChange)`（**全程无 clamp** —— 与近战不同）。
        /// </summary>
        int ProjectMaxHealthProcessed()
        {
            int nonExt = 0, ext = 0, setVal = 0;
            bool hasSet = false;
            for (int i = 0; i < _statMods.Count; i++)
            {
                var m = _statMods[i];
                if (m.Attr != "health") continue;
                if (m.IsSet)
                {
                    // 原版倒序遍历、后写的覆盖先写的 ⇒ 生效的是**下标 0**（最先施加的那条）
                    if (!hasSet) { setVal = m.Value; hasSet = true; }
                    continue;
                }
                if (m.Extrinsic) ext += m.Value;
                else nonExt += m.Value;
            }
            int basePart = hasSet ? setVal : MaxHealthBase + nonExt;
            return basePart + ext;                       // `:65` / `:107` 无 clamp
        }

        /// <summary>
        /// <see cref="CurrentIntrinsicHealth"/> —— 原版 `get_CurrentIntrinsicHealth.c:26-50`：
        /// `currentHealth − Σ(extrinsic ∧ addStats 的 healthChange)`（正序，无 clamp）。
        /// </summary>
        int ProjectIntrinsicHealth()
        {
            int ext = 0;
            for (int i = 0; i < _statMods.Count; i++)
            {
                var m = _statMods[i];
                if (m.Attr == "health" && !m.IsSet && m.Extrinsic) ext += m.Value;
            }
            return Health - ext;
        }

        /// <summary>
        /// <see cref="CurrentIntrinsicMeleeAttack"/> —— 原版 `get_CurrentIntrinsicMeleeAttack.c:22-46`：
        /// `CurrentMeleeAttack − Σ(extrinsic ∧ addStats 的 meleeAttackChange)`（**减的是投影值**，无 clamp）。
        /// </summary>
        int ProjectIntrinsicMeleeAttack()
        {
            int ext = 0;
            for (int i = 0; i < _statMods.Count; i++)
            {
                var m = _statMods[i];
                if (m.Attr == "attack" && !m.IsSet && m.Extrinsic) ext += m.Value;
            }
            return CurrentMeleeAttack - ext;
        }

        // ================================================================================================
        // 🆕 2026-10-25（`E1` · 第四段）—— **「重算」与「真移除」分开**（H1 裁定）
        //
        // 【为什么】原版里这是**两件不同的事**：
        //   · `trigger 20 WhileInPlay`（光环）**重复施加是幂等空操作** —— `AddEffect` 的
        //     `ContainsExtrinsicEffectFrom` 已经为真就 `return false`（判据 §1·5 第二行）；
        //   · `trigger 240 WhileInPlayVariable` 才是「`RemoveExtrinsicEffectsFrom` + 重新 AddEffect」。
        // 而「摘 `+N health` 夹到新上限」那条判据（`RemoveEffect.c:147-181`）是给**真移除**用的。
        // 我们的 `Auras.Recompose` 是「先全清、再加回」—— 等于每次棋盘变动都走了一遍
        // 「真移除 + 重新施加」⇒ **净结果不为零**（清的时候夹一次、加回来又 += 一次 ⇒ 回血）。
        //
        // 【怎么分】`Auras.Recompose` 把整段重算包在 `BeginAuraRecompute` / `EndAuraRecompute` 之间：
        //   · **重算期间**：`ApplyGrant` 对 `health` **一个字节都不动**（连夹也不夹）；
        //   · **重算结束时**：按「光环给的血修饰总量」的**前后差**统一结算一次 ——
        //       +差 ⇒ `Health += 差`（新出现的光环，与首次施加同效）
        //       −差 ⇒ `ClampHealthToMax()`（光环离场 = **真移除**，照 Stage 2 的夹口径）
        //       零差 ⇒ **什么都不做**（这就是「棋盘无关变动前后 `Health` 逐位相等」）
        //
        // ⚠️ 只有 `health` 需要这一层：`attack`/`ranged` 是「摘修饰 + 重加修饰」，本来就净零；
        //   关键词 / 费用 / 誓约授予是「布尔或重登记」，也净零（见 `Aura.Recompose` 的 doc）。
        // ⛔ 别把这段推广成「重算期间什么副作用都不许有」—— 那会把新出现的光环也吃掉。
        // ================================================================================================

        bool _recomputingAura;
        int _auraHealthBefore;

        /// <summary>光环重算**开始**（见本段顶部那段说明）。可嵌套安全：内层调用不重置基准。</summary>
        public void BeginAuraRecompute()
        {
            if (_recomputingAura) return;       // 嵌套：保留最外层的基准
            _recomputingAura = true;
            _auraHealthBefore = AuraHealthGrantTotal();
            _auraExemptBefore = RuleCore.SummonSicknessExempt(this);   // 🆕 2026-10-27（B9·`W18AR`）：见类尾那段
        }

        /// <summary>光环重算**结束** ⇒ 按前后差结算一次（+差 加血 / −差 夹到新上限 / 零差 什么都不做）；🆕 2026-10-27（B9·`W18AR`）还负责 act 两位（只有 `fast`/`flank` 豁免**真的丢了**才重算，见类尾那段）。</summary>
        public void EndAuraRecompute()
        {
            if (!_recomputingAura) return;
            _recomputingAura = false;
            int after = AuraHealthGrantTotal();
            if (after > _auraHealthBefore) Health += after - _auraHealthBefore;
            else if (after < _auraHealthBefore) ClampHealthToMax();
            if (_auraExemptBefore && !RuleCore.SummonSicknessExempt(this)) RuleCore.RecomputeActAndAttack(this);
        }

        /// <summary>
        /// 🔴🆕 **2026-10-25（`E1` · 第四段 · F2）：标记光「攻击用掉后**留到回合末**才摘」的那一格。**
        ///
        /// <para>**判据（cpp2il 现读，`.c` 只交叉）**：
        /// `CardScript.SetRemoveMarkerlight`（`CardScript.cs:11260-11296`）= **只置这一位**
        /// （`removeMarkerlightAtEndOfTurn = true`，原版 `EntityScript +0x57`）+ 播「用掉了」的表现
        /// （`MarkerlightController.MarkerLightUsed()`），**一个 trait 都不摘**；
        /// 真正摘除在 `CardScript.OnTurnEnd`（`CardScript.cs:16599-16607`）：
        /// `if (removeMarkerlightAtEndOfTurn) { if (CurrentMarkerlight &gt; 0) {
        ///      RemoveActiveEffect(markerlight); RemoveTrait(markerlight); 该位 = false; } }`。</para>
        ///
        /// <para>⚠️ **改前我们是在攻击那一段打完就 `RemoveAll("markerlight")`**（`RuleCore.cs` 的
        /// 「标记光 X」那一段）—— 比原版**早了一整个回合**，`Expiring` 态也就永远看不见（E5 的 F2/W9 就是这条）。</para>
        ///
        /// <para>🟢 **2026-10-25（`E1` · 第五段）「谁的回合末」已坐实：两边都扫，我们做的对。**
        /// 判据（cpp2il 现读）= `BattleManagerSupport.BroadcastTurnEnd`（`BattleManagerSupport.cs:1516-1562`）：
        /// 它遍历的是 **`manager.unitsInPlay` 这个【全局】在场列表**（不是某一方的列表），
        /// 对每一张 `IsInPlay()` 的卡各调一次 `CardScript.OnTurnEnd(isPlayerTurn)`（残骸那一档另放行）；
        /// 之后才分别扫**双方手牌**。
        /// ⇒ **`OnTurnEnd` 跑在【所有在场卡】上、不分敌我** ⇒ 我们 `RuleCore.EndTurnCore` 那趟
        /// `RevertBuffs(true, …)` **两边都扫**与它同形，⛔ 不用改。「我方打给敌方的标记光」不会被提前摘。</para>
        /// </summary>
        public bool RemoveMarkerlightAtEndOfTurn;

        /// <summary>当前「由光环给的」生命修饰合计（`Source == Auras.GrantTag` 且 `Attr == "health"` 且非 set）。
        /// ⚠️ 与 <see cref="ClampHealthToMax"/> 一起构成「重算零副作用」的判据 —— 两个方法只此一份。</summary>
        public int AuraHealthGrantTotal()
        {
            int s = 0;
            for (int i = 0; i < _statMods.Count; i++)
            {
                var m = _statMods[i];
                if (m.IsSet) continue;
                if (m.Attr != "health") continue;
                if (m.Source != Auras.GrantTag) continue;
                s += m.Value;
            }
            return s;
        }

        // ================================================================================================
        // 🆕 2026-10-26（完全复刻 · 第 4 轮 · 执行代理 `F3` · 任务 R3）
        //      **`jam`（干扰，原版 `DefinedTrait.jam = 130`）的运行时语义 —— 状态侧**
        //
        // 【原版是什么】`CardScript.ReactToUnitJammed(disenchantedCard)`（cpp2il 真体
        //   `out_cpp2il_dev_ilspy/CardScript.cs:19980-20118`，**唯一有真方法体的那份**；
        //   `decomp_full/CardScript__ReactToUnitJammed.c` 只剩指针搬运 ⇒ **别拿它当判据**，判据见 E1 报告 §二·8）
        //   是**双分支**：
        //     · `disenchantedCard == this`（**自己被 jam**）：
        //         ① 倒序遍历 `activeEffects`(+0x108)，逐条 `RemoveEffect(effect, fromJam: true)`；
        //         ② 把**当前全部 trait** 收进一个列表，逐条 `RemoveTrait(trait)`
        //            （⚠️ 是 `RemoveTrait` 不是 `RemoveTraitSilently` ⇒ **会重算**召唤病/anims）；
        //         ③ `stunnedAtStartOfTurn(+0x55) = false` 与 `removeMarkerlightAtEndOfTurn(+0x57) = false`；
        //         ④ 新建 `CardTraitData(jam)` 压进 `CurrentTraits`；
        //         ⑤ 卡型 ≠ `Hero` ⇒ `hasActiveAbility(+0x220) = false`（**督军除外**）；
        //         ⑥ 一串表现 + `CheckIfDead(UnitDeathType.undefined)`（**jam 之后可能当场死**）。
        //     · 否则（**别的单位被 jam**）：`RemoveExtrinsicEffectsFrom(被 jam 的那张)`
        //         + `UpdateWhileInPlay(false)`（= 把「挂在它身上、由那张卡给的效果」整份撤掉并重算常驻）。
        //
        // 【我们这边怎么落】我们没有 `activeEffects` 那套统一记录 —— 效果被摊成四本账：
        //   `_statMods`（数值修饰）/ `_buffs`（限时增益）/ `_grants`（关键词带出来的增益）/
        //   `_auraKw` + `_grantedOps`（光环给的关键词与正文）。所以「清空 activeEffects」= **把这四本账
        //   全部收干净**（`RemoveAllAppliedEffectsForJam`）。⭐ 这与 `Auras.Recompose` 的
        //   `ClearAuraGrants` **不是同一件事**：后者只收**光环那一份**，本支收**全部由效果挂上来的**。
        //
        // ⚠️ **可达性如实记（铁律 11：休眠 ≠ 不做）**：池内**0 张卡**带 `jam` 词条
        //   （`cards_engine.json` / `cards_truth.json` 全文扫 `jam` 词根只命中卡名
        //   `Jammed Communications` 与 `JAM` 缩写，见 <see cref="KeywordTable.Jam"/> 的 doc）。
        //   ⇒ 本支今天**零可观察量**；实现依据是「原版有、我们缺」（铁律 11）。
        //   驱动侧（`ResolveJamUnit` / `BroadcastUnitJammed` / `CardScript.TriggerOtherCardJammed`）
        //   属 `EffectResolver.cs` + `WhenEvent.cs` ⇒ **不在 `F3` 白名单**，已写进改动请求（R3-F2）。
        // ================================================================================================

        /// <summary>
        /// **卡型 ≠ 督军时被压掉的「有主动能力」那一位** —— 原版 `CardScript.hasActiveAbility`
        /// （`+0x220`）**实例级**的那一份，写点只有 `ReactToUnitJammed` 一处
        /// （`CardScript.cs:20085-20089`：`if (cardType != Hero) hasActiveAbility = false;`）。
        ///
        /// ⚠️ **别和 <see cref="CardDef.HasActiveAbility"/> 混**：那一格是**卡面印的**（不可变定义，
        /// 池内 87 张为真），这一格是**这一局这一个单位**的运行时压制位。
        /// **读它的人**：主动能力可用性判定（原版 `DoesAbilityRequireLoading` / 主动技能按钮态）
        /// —— 我们的读点由 `RuleCore.CanUseAbility` 那一族接（属 F1/F2，见改动请求）。
        /// </summary>
        public bool ActiveAbilitySuppressed;

        /// <summary>
        /// **把「由效果挂上来的一切」整份收掉** —— 原版 `ReactToUnitJammed` ①那一轮
        /// `RemoveEffect(effect, fromJam: true)`（`CardScript.cs:19990-20010`）的等价位。
        ///
        /// <para>**逐本账**（这四本就是我们全部「运行时效果状态」）：</para>
        /// <list type="number">
        ///   <item>`_buffs`（限时增益）：逐条按「真移除」口径撤（关键词走 `RemoveKeyword`、
        ///         数值走 `RemoveStatMod`、生命走 `ClampHealthToMax`）—— 与 `RevertBuffs` 同一台机器。</item>
        ///   <item>`_grants`（关键词带出来的属性增益）：逐条 `ApplyGrant(g, -1)` 后清表。</item>
        ///   <item>`_auraKw` / `_auraFx` / `_statMods(GrantTag)`：`ClearAuraGrants()`（光环那一份）。</item>
        ///   <item>剩下的 `_statMods`（`addStats` 那一族直接建的）：**全摘**（`_statMods.Clear()`）。</item>
        /// </list>
        /// <para>⚠️ 顺序有语义：**先撤 buff / grants**（它们各自持有 `StatMod` 引用并会顺手夹血），
        /// 再 `ClearAuraGrants`，**最后**才清残留的修饰表 —— 反过来的话「夹到新上限」那一步
        /// 会在表已清空之后跑，看到的 `MaxHealth` 是**撤完的**值（结果反而对），但
        /// `TempBuff.Mod` 的引用会指向已不在表里的格子（幂等，仍安全）。按上面顺序可读性最好。</para>
        /// </summary>
        /// <returns>撤掉了几条（报表/探针用）</returns>
        public int RemoveAllAppliedEffectsForJam()
        {
            int n = 0;
            // ① 限时增益（`_buffs`）—— 走与 `RevertBuffs` 相同的三种撤法
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                var b = _buffs[i];
                if (RemoveTempBuffCore(b)) n++;   // 🆕 2026-10-28（W4b）：撤除规则一份；本路按现状**不**调 SyncAttackTypeAfterStatChange（保持既有行为，⛔ 别加）
            }
            // ② 关键词带出来的属性增益
            for (int i = _grants.Count - 1; i >= 0; i--)
            {
                var g = _grants[i];
                _grants.RemoveAt(i);
                ApplyGrant(g, -1);
                n++;
            }
            // ③ 光环那一份（关键词 + 属性 + 光环挂的触发正文 + 残骸/誓约那几个标记）
            ClearAuraGrants();
            // ④ 残留的数值修饰（`addStats` 直接建的、没进 `_grants` 的那一批）
            n += _statMods.Count;
            _statMods.Clear();
            // ⑤ 「关键词带出来的正文」那本账 —— 关键词马上要全摘，这里一并清掉
            _grantedOps.Clear();
            _grantedText.Clear();
            return n;
        }

        /// <summary>
        /// **`jam` 组合口** —— 原版 `ReactToUnitJammed(this)` 那 ①–⑤ 步（见上面那段头注）。
        ///
        /// <para>**做什么（顺序照原版）**：
        /// ① 清空全部「由效果挂上来的」状态（<see cref="RemoveAllAppliedEffectsForJam"/>）；
        /// ② **摘掉全部 trait**（含卡面印的那些）—— 用 <see cref="RemoveAll"/> 的**非 silent** 形态
        ///    （原版这里走 `RemoveTrait`，会重算 `fast`/`flank` 的召唤病那两位）；
        /// ③ `StunnedAtStartOfTurn` / `RemoveMarkerlightAtEndOfTurn` 清 0；
        /// ④ 挂上 `jam` 关键词（值 1，与原版 `new CardTraitData(jam)` 一致）；
        /// ⑤ 非督军 ⇒ <see cref="ActiveAbilitySuppressed"/> = true。</para>
        ///
        /// <para>🔴 **`CheckIfDead` 不在这里做**（它在 `RuleCore`，不是本文件的东西）——
        /// 本方法**返回 `Health &lt;= 0`**，调用方据此走原版第 ⑥ 步。
        /// 返回值**不是**「jam 成功没有」：`jam` 本身恒成功。</para>
        /// </summary>
        /// <returns>`Health &lt;= 0` ⇒ 调用方必须接着调死亡结算（原版 `CheckIfDead(undefined)`）</returns>
        public bool ApplyJam()
        {
            // ① 清空全部效果态
            RemoveAllAppliedEffectsForJam();
            // ② 摘掉全部 trait —— 键先快照（`RemoveAll` 会改字典）
            var keys = new List<string>(_keywords.Keys);
            foreach (string kw in keys) RemoveAll(kw);
            // ③ 两个「回合开始时就在某状态」的闸门清 0（原版两行直接写 0）
            StunnedAtStartOfTurn = false;
            RemoveMarkerlightAtEndOfTurn = false;
            // ④ 挂上 jam
            AddKeyword(KeywordTable.Jam, 1);
            // ⑤ 督军除外
            if (!IsWarlord) ActiveAbilitySuppressed = true;
            return Health <= 0;
        }

        /// <summary>
        /// **「别人被 jam」那一支的等价位** —— 原版 `ReactToUnitJammed` 的 else 分支
        /// `RemoveExtrinsicEffectsFrom(disenchantedCard)`（`CardScript.cs:20115`）。
        ///
        /// **原版语义**：把「施放者 = 那张卡」的那一批 extrinsic 效果整份摘掉（然后由调用方
        /// `UpdateWhileInPlay(false)` 重算常驻）。
        ///
        /// <para>⚠️ **我们只能做到一半，如实标着**：我们的 `_statMods.Source` / `TempBuff.SourceCard`
        /// 记的是**来源标签**（`Auras.GrantTag` / 卡名），`RecordGrant` 那条路**没有记施放者卡名**
        /// ⇒ 「按施放者整份撤」只能覆盖 `SourceCard`（限时增益）与 `Source`（若恰好填了卡名）两本账。
        /// 判据上**不假装**：返回撤掉几条，调用方（`F1`/`F2`）拿到后仍应跑一次光环重算。</para>
        /// </summary>
        /// <returns>撤掉了几条（限时增益 + 匹配来源的数值修饰）</returns>
        public int RemoveExtrinsicEffectsFrom(string sourceCardName)
        {
            if (string.IsNullOrEmpty(sourceCardName)) return 0;
            int n = RemoveBuffsFromCard(sourceCardName);
            for (int i = _statMods.Count - 1; i >= 0; i--)
            {
                if (_statMods[i].Source != sourceCardName) continue;
                bool health = _statMods[i].Attr == "health";
                _statMods.RemoveAt(i);
                if (health) ClampHealthToMax();
                n++;
            }
            return n;
        }

        /// <summary>
        /// 🆕 **2026-10-26（第 4 轮 · `F3` · 任务 R5）**：**只收「数值 + 关键词」那两层的
        /// 光环赠予**（`Auras.RefreshInPlayNumericAndKeywords` 专用）。
        ///
        /// <para>🔴 **与 <see cref="ClearAuraGrants"/> 的差别只有一句**：**不动誓约授予那两位**
        /// （<see cref="OathTripleGranted"/> / <see cref="OathAllTurnsGranted"/>）。</para>
        ///
        /// <para>**为什么需要这个更窄的口**（`调度台日志` 的 **H1 裁定** + `报告/E1_第3轮.md` 的 **R5**）：
        /// 誓约那两位是**布尔幂等授予**，但它只能由 `Recompose` 的 ②·b 层**重新授**。
        /// 若在「动作边界」那种**棋盘根本没变**的场合把它清掉（走 `ClearAuraGrants`）而不重授，
        /// 结果是**静默丢授予**（`Ferren Areios` 那两句会凭空失效）。
        /// ⇒ 动作边界那种重算只该覆盖「数值 + 关键词」，誓约/费用两层由棋盘变动那趟 `Recompose` 管。</para>
        /// </summary>
        public void ClearAuraNumericAndKeywords()
        {
            if (_auraKw.Count > 0)
            {
                bool needActRecompute = false;
                foreach (var kv in _auraKw)
                {
                    string kw = kv.Key;
                    int left = RawKw(kw) - kv.Value;
                    if (left > 0) _keywords[kw] = left; else _keywords.Remove(kw);
                    if (kw == KeywordTable.Armour) Armor = System.Math.Max(0, Armor - kv.Value);
                    if (kw == "fast" || kw == "flank") needActRecompute = true;
                }
                _auraKw.Clear();
                if (needActRecompute) RequestActRecomputeAfterAuraChange();
            }
            if (_auraFx.Count > 0)
            {
                foreach (string kw in _auraFx) { _grantedOps.Remove(kw); _grantedText.Remove(kw); }
                _auraFx.Clear();
            }
            // 属性那一份（`GrantTag`）—— 数值层，必须收
            RevertGrantsFrom(Auras.GrantTag);
            // ⚠️ **刻意不动**：`OathTripleGranted` / `OathAllTurnsGranted`（那是「授予」层，见上面）
            // ⚠️ `AuraRemnantStay` **要清** —— 它是棋盘派生标记（来源离场就该没），
            //    与本轮重算同一生命周期，不属于 H1 点名要保住的那三层。
            AuraRemnantStay = false;
        }

        // =====================================================================================
        //  🆕 **2026-10-26（执行代理 `B8_D` · 任务 3）：原版 `HasWhileInPlayAbility` /
        //  `HasWhileInPlayAdjacentEffect` 的【逐字】判定** —— 撤掉「拿 `AuraSpecs.Adjacent` 当替代」
        // =====================================================================================
        //
        //  **为什么要有这两个**：原版 AI 出小兵时问 `MinionManager.GetNextSlotCheckingAdjacents()`
        //  要格位，那一步的判据就是 `HasWhileInPlayAdjacentEffect()`（`AI__ExecuteAction.c:60-63`）。
        //  我们此前把「这张卡有没有相邻光环」（`AuraSpecs[i].Adjacent`）**当成了**它 ——
        //  那是**另一件事**（光环会不会作用到相邻格 ≠ 原版这个判据），会改变 AI 布场。
        //  🔴 **2026-10-26 调度台裁定：照原版逐字判**（恒 false 就恒 false）。
        //
        //  **判据（🥇 `D:/2/tools/decomp_full/` 两份一致）**：
        //   · `CardScript__HasWhileInPlayAdjacentEffect.c:1-44` ＋ 🥈
        //     `cpp2il/out_cpp2il_dev_ilspy/CardScript.cs:6865-6919`：
        //       ① `HasCurrentTrait(0x2f8 = 760 requiem)` ⇒ **true**
        //       ② 否则 `HasWhileInPlayAbility()` 为假 ⇒ **false**
        //       ③ 否则逐条 `rawCardScript.cardAbilities(+0x290)`：
        //          `ability.targetCriteria(+0x30)` 非空 且 `targetCriteria.targetsAffected(+0x10) == 100`
        //          ⇒ **true**；扫完没有 ⇒ **false**
        //   · `CardScript__HasWhileInPlayAbility.c:1-61` ＋ 🥈 同文件 `:6800-6863`，五支并集：
        //       a) `HasCurrentTrait(0x82 = 130 jam)` ⇒ true
        //       b) `rawCard.HasAbilityType(0x14 = 20 WhileInPlay)` ⇒ true
        //       c) `rawCard.HasAbilityType(0xf0 = 240 WhileInPlayVariable)` ⇒ true
        //       d) `HasEnchantment(20)` ∨ `HasEnchantment(240)` ⇒ true
        //       e) `HasCurrentTrait(0x262 = 610 pack)` ⇒ true
        //       f) `activeEffects(+0x108).Find(x => x.trait == pack && x.buffType == 1)` ⇒ true
        //          （判据 = ILSpy 里**读得出**的那个 lambda 体：`x.trait != DefinedTrait.pack`
        //            ⇒ false；`x.buffType - 1 == 0` ⇒ 即 `buffType == 1 addStats`）
        //
        //  ⚠️ **数据现状（2026-10-26 现读）**：全池 `abilityLevelCriteria` 的 `targetsAffected == 100`
        //  的行 = **0**（86 行里分布 `{10:62, 30:8, 20:8, 90:7, 55:1}`）；`requiem` 卡 = **0 张**；
        //  `jam` 卡 = **0 张**；`pack` 卡 = **10 张**
        //  ⇒ **这一对谓词对今天整个卡池恒 false**（这正是「照原版」的定义，⛔ 不是「没做」）。
        //  📌 对照读数（同一次现扫）：**logic 级** `abilityTargetCriteria` 的 `ta == 100` 也是 **0** 行
        //  ⇒ 即便将来有人提议「改用 logic 级那一列」，今天**结果相同**（两条都是 0）。
        // =====================================================================================

        /// <summary>
        /// 🆕 2026-10-26（`B8_D`）：原版 `CardScript.HasWhileInPlayAbility()`
        /// （判据 → 上面那一段，五支并集）。
        ///
        /// <para><paramref name="why"/> = **判不了的那几支**（`null` = 都判得出）。🆕 2026-10-26（`B8_Q` 任务 4）：
        /// 第 ⑥ 支**已接上** —— <see cref="StatMod"/> 补了 `Trait`(原版 `CardEffect.trait +0x3c`) / `BuffType`(`+0x38`) 两列。
        /// ⚠️ 写入侧只有缺省推断（`isSet ? -1 : 1`）、`Trait` 还没人传（`EffectResolver` 不在本代理白名单 ⇒ 报告 §⑥）。</para>
        ///
        /// <para>📌 **这一支今天恒 false 是【数据事实】，不是「判不了」**：现扫 `cards_truth.json` 1,351 张的
        /// 507 条 `buffType == 1` 记录，`trait` 只有 505×`null` / 1×930 / 1×1020 —— **`trait == 610(pack)` 一条都没有**
        /// （`pack` 全池只有 2 条、都是 `buffType 3 addTrait`）⇒ 将来 `Trait` 传全了结果也不变。
        /// ⚠️ 老口径仍成立：下游第 ③ 支（`targetsAffected == 100`）现读全池 **0 行** ⇒ 今天**零行为差异**。</para>
        ///
        /// <para>🔶（已撤）`B8_D` 2026-10-26 曾把第 ⑥ 支「按 false 处理」并打「自己设计」标 ——
        /// 那条标记**已撤销**：两列已补、判据拿得到，照原版逐字判即可，不再是自设计。</para>
        /// </summary>
        public bool HasWhileInPlayAbility(out string why)
        {
            why = null;
            // ① jam（130）—— 原版这一支**最先**判，命中就直接 true（`HasWhileInPlayAbility.c:9-12`）
            if (Has(KeywordTable.Jam)) return true;

            // ② / ③ 卡自己的 ability trigger 20 / 240
            if (Card != null)
            {
                int t20 = Card.AbilityTriggerPresence(20);    // AbilityTrigger.WhileInPlay
                int t240 = Card.AbilityTriggerPresence(240);  // AbilityTrigger.WhileInPlayVariable
                if (t20 == 1 || t240 == 1) return true;
                if (t20 < 0 || t240 < 0)
                    why = "`abilityTriggers` 列没落 ⇒ trigger 20/240 的存在性**未知**";
            }

            // ④ 同名附魔（原版 `EntityScript.HasEnchantment(20 / 240)`）
            if (Instance != null)
            {
                var on = Enchantments.On(Instance);
                for (int i = 0; i < on.Count; i++)
                {
                    var ec = on[i] != null ? on[i].Card : null;
                    if (ec == null) continue;
                    if (ec.AbilityTriggerPresence(20) == 1 || ec.AbilityTriggerPresence(240) == 1)
                        return true;
                }
            }

            // ⑤ pack（610）
            if (Has("pack")) return true;

            // ⑥ 原版 `activeEffects.Find(x => x.trait == pack && x.buffType == 1)` —— 逐字（判据 → 类尾 `StatMod.Trait/BuffType`）
            for (int i = 0; i < _statMods.Count; i++)
            {
                StatMod m = _statMods[i];
                if (m.BuffType == 1 && m.Trait == "pack") return true;
            }
            return false;
        }

        /// <summary>
        /// 🆕 2026-10-26（`B8_D`）：原版 `CardScript.HasWhileInPlayAdjacentEffect()`
        /// —— **逐字照搬**（判据 → 上面那一段）。
        ///
        /// <para>🔴 我们此前用「`CardDef.AuraSpecs` 里有没有 `Adjacent == true`」当它的替代
        /// （`Data/SimpleAI.HasAdjacentEffect`）—— **已裁定撤掉**：那是「光环作用到相邻格」，
        /// 与原版这个判据**不是一件事**（原版问的是 `requiem` trait ＋ ability 级
        /// `targetsAffected == 100`）。</para>
        ///
        /// <para>⚠️ **现读全池恒 false**（`requiem` 0 张 · ability 级 `ta == 100` 0 行），
        /// ⛔ **别为了「看起来有用」把 `AuraSpecs.Adjacent` 加回这个判据** —— 那正是本条要撤的东西。
        /// ⚠️ 调用点改造（`SimpleAI.HasAdjacentEffect` 应改成调本方法）**不在 `B8_D` 白名单**，
        /// 已写进报告 §⑥ 交调度台派活。</para>
        /// </summary>
        public bool HasWhileInPlayAdjacentEffect(out string why)
        {
            why = null;
            // ① requiem（760）
            if (Has(KeywordTable.Requiem)) return true;
            // ② HasWhileInPlayAbility 为假 ⇒ false
            if (!HasWhileInPlayAbility(out why)) return false;
            // ③ ability 级 `targetsAffected == 100`（`CardAbility.targetCriteria(+0x30)`）
            if (Card == null) return false;
            return Card.AnyAbilityLevelCriteriaTargetsAffected(100, out why);
        }

        /// <summary>无 `why` 的重载（只想要真值、且调用点已经知道今天恒 false 时用）。</summary>
        public bool HasWhileInPlayAdjacentEffect() { string ignored; return HasWhileInPlayAdjacentEffect(out ignored); }

        // =====================================================================================
        //  🆕 2026-10-26（执行代理 `B8_Q` · 任务 3）：`KwValue` 的 `≥0` 夹紧 + 内部原值读数
        // =====================================================================================
        //  【判据】原版 `EntityScript.GetCurrentTraitValueWithModifiers(DefinedTrait)` 末尾：
        //    `int num5 = num4 + num;  if (num5 < 0) return 0; return num5;`
        //    （🥈 `cpp2il/out_cpp2il_dev_ilspy/EntityScript.cs` 的
        //      `public int GetCurrentTraitValueWithModifiers(DefinedTrait trait)` 那一段；🥇 Ghidra
        //      同义：`decomp_full/EntityScript__GetCurrentTraitValueWithModifiers.c:68-71`
        //      `if (-1 < iVar8 + iVar7) iVar1 = iVar8 + iVar7;`）。
        //    原版**只有带修饰的那一支夹**：`GetCurrentTraitValue`（不带修饰，`.c:22-30`）直接返回
        //    `CardTraitData._traitValue`、**不夹** —— 但那条路的输入是序列化值（恒 ≥ 0）⇒ 两路在数据上
        //    等价。我们的 `KwValue` 是**合并视图**（印刷值 + 授予 − 摘除），对应的是**带修饰**那一路
        //    ⇒ 夹在 `KwValue` 上，与 `Current*` 那一族属性（`CurrentTide` / `CurrentBastion` …）同口径。
        //  【为什么内部还要一个原值读数】夹紧会让「记账用的算术」失真：
        //    `AddKeyword` / `RemoveKeyword` / `AddAuraKeyword` / `ClearAuraGrants` /
        //    `ClearAuraNumericAndKeywords` 都是「读旧值 ± 增量再写回」——
        //    若旧值已经是负的（`give -3 Attack` 那种减到穿零），读夹紧后的 0 会让**下一次授予多算 3**。
        //    ⇒ 这五处改用 <see cref="RawKw"/>（不夹）；**对外一律 `KwValue`**。
        //  【可观察量】池内 **0 张**可达（`cards_truth.json` 全池没有任何 `−` 值能穿零的授予路径）
        //    ⇒ 今天**零行为差异**（`ruleprobe check` / `play check` 指纹逐字节不变），补的是语义（铁律 11）。
        // =====================================================================================

        /// <summary>关键词字典的**原值**（不夹 0）。⛔ **只给本类内部的「读旧值 ± 增量」记账用**；
        /// 对外读数一律 <see cref="KwValue"/>（它带原版那条 `≥0` 夹紧）。</summary>
        int RawKw(string keyword)
        {
            int v;
            return _keywords.TryGetValue(keyword, out v) ? v : 0;
        }

        // =====================================================================================
        //  🆕 2026-10-26（执行代理 `B8_AX` · 任务 1 · **成对缺口的两个原语**）
        //  判据 → `报告/B8_AX_残骸trait与卡定义.md` §一、§二
        //  ⚠️ 今天**还没有调用点**（两个真正的落点都在 `RuleCore.cs`，本批那不归这一路独占）
        //     ⇒ 按本仓既有做法「类尾备好原语、改动请求交调度台」（先例：
        //       `RuleCore.AddCardToHandAtWithinLimit` 那条「真身在别的文件 ⇒ 备好原语」）。
        //  🔴 **两步必须同批落地**，⛔ 不许只做一半 —— 见下面 <see cref="IsAeldariRemnant"/> 的 doc。
        // =====================================================================================

        /// <summary>
        /// 原版 `SupportMethods.IsAeldariRemnant(CardScript)` —— **一颗能收集的灵族灵魂石**：
        /// `rawCardData.cardArmy(卡 SO +0x2c) == 30 (SaimHann)` ∧ `isRemnant(+0x65)`。
        ///
        /// <para>**判据（🥇 `decomp_full/`，14 行全文逐句）**：`SupportMethods__IsAeldariRemnant.c:7-12`
        /// —— `uVar1 = *(card + 0x28)`（卡 SO）→ `*(int*)(uVar1 + 0x2c) != 0x1e` ⇒ 假；
        /// 否则返回 `*(byte*)(card + 0x65)`。
        /// `+0x2c = cardArmy` 由**两处独立读数**坐实：`0x1e(30) = SaimHann`（本方法）·
        /// `0x28(40) = Sautekh`（`decomp_full/SupportMethods__ShouldRemnantDestroyOnTurnEnd.c:17`
        /// 同一个偏移问死灵）；`CardArmy` 枚举值见 `CardArmy.cs`。
        /// 🔴 **它一个字都不读 trait** —— 所以「翻面时把 trait 摘光」不会影响收集。</para>
        ///
        /// <para>⛔ **调用点的口径要一起换**：`RuleCore.CanCollectWaystone` 今天读的是
        /// `IsRemnant && Has(waystone)`（2026-09-25 那轮的等价替代）。一旦残骸真的不继承 trait，
        /// 那条判定会**恒假** ⇒ 灵族收灵魂石整条路断（用户可见）。正确替换 = 本属性。
        /// 池内两条口径**今天同集合**（`cards_engine.json` 1,128 张现扫：带 `Waystone` 的 **25 张全在
        /// `SaimHann`**；带 `Remnant` 的 **26 张全在 `Sautekh`**；两者交集 **0**）
        /// —— 所以这次替换**零回归**；但原版判据是**阵营**，⛔ 别再退回关键词。</para>
        ///
        /// <para>⚠️ 与 `CardPresentation/Battle/BattleDriver.cs` 的 `SpiritStoneFaction` 是**同一件事**的两份
        /// 写法（那一份已入账 `战斗逻辑驱动.tsv:1485`）⇒ 迟早要收成一处；本属性是引擎侧的那一处。</para>
        /// </summary>
        public bool IsAeldariRemnant
        {
            get { return IsRemnant && Card != null && Card.Faction == "SaimHann"; }
        }

        /// <summary>
        /// **把身上的关键词全部摘光** —— 原版 `CardScript.TransformIntoRemnant` 的第三步：
        /// **倒序遍历 `CurrentTraits`，逐条 `RemoveTraitSilently`**（残骸**一条 trait 都不剩**）。
        ///
        /// <para>**判据（🥇 `decomp_full/CardScript__TransformIntoRemnant.c:54-75`）**：
        /// `iVar2 = *(int*)(list + 0x18); while (iVar2 = iVar2 - 1, -1 < iVar2) …
        ///   CardScript__RemoveTraitSilently(card, *(undefined4*)(list[iVar2] + 0x18))`。
        /// 倒序是因为「删一个就左移一位」，顺序走会跳过下一项 ⇒ **逐条删光的等价做法 = 先取快照再删**。
        /// 前两步（`isRemnant=1` 并清 `isLeavingRemnant`、`remnantMaxHealth` 覆盖血/最大血）我们已经对上
        /// （`RuleCore.CleanupDeaths` 的翻面支：`IsRemnant=true / Health=1 / MaxHealthBase=1 / 攻 0 / 不行动`）。</para>
        ///
        /// <para>⚠️ **为什么不是一个 `_keywords.Clear()`**：摘关键词在这套模型里还连着三件东西 ——
        /// `Armor` 字段（`RemoveAll` 里那句 `if (keyword == Armour) Armor = 0;`）、
        /// `BlindedAtStartOfTurn`、以及关键词带出来的属性授予（`RevertGrantsOf`）
        /// ⇒ 绕开 `RemoveAll` 会留下「键没了、值还在」的静默不一致。
        /// 走 <see cref="RemoveAll"/> 的 `silent: true` 档 —— 它正是 `RemoveTraitSilently` 的等价位
        /// （判据 → `RemoveAll` 的 doc 末段）。</para>
        ///
        /// <para>**该在哪调**：`RuleCore.CleanupDeaths` 造出残骸 `UnitState` 之后、写进 `Board[slot]` 之前
        /// （`new UnitState(u.Instance, false) { IsRemnant = true, … }` 那一句的**下一句**）。
        /// 🔴 **必须与 <see cref="IsAeldariRemnant"/> 那一次替换同批做**，理由见它的 doc。</para>
        ///
        /// <para>⚠️ 与 `CardScript.RemoveTraitSilently` 的**已知结构差异**（已有账：`战斗逻辑驱动.tsv:979`）：
        /// 我们的 `_keywords` 是**合并字典**，`RemoveAll` 会把「卡面印的那一份」和「效果授予的那一份」
        /// 一起摘掉；原版只摘 `CurrentTraits(+0x128)` 里那条、`activeEffects(+0x108)` 里由 `buffType 3`
        /// 授予的那一份**仍在**。⇒ 翻面后**再**被授予的 trait，两侧都看得见；**翻面前**由效果授予的，
        /// 原版还看得见、我们看不见。⚠️ **可达性没核**（「翻面那一刻身上带 `buffType 3` 授予的 trait
        /// 的残骸有几张」没算）⇒ 如实记差异，⛔ 不在这里硬造双簿。</para>
        /// </summary>
        public void ClearAllKeywords()
        {
            if (_keywords.Count > 0)
            {
                var keys = new List<string>(_keywords.Keys);
                for (int i = 0; i < keys.Count; i++) RemoveAll(keys[i], silent: true);
            }
            Armor = 0;
            // 🆕 2026-10-26（`E10TB` · P-05 ①）：残骸那本书一起清（整本摘光时它必然也空）
            _remnantTraits.Clear();
            // 🆕 2026-10-26（`W13EN` · 账 64）：`addTrait` 那一族**记录账**也一起清 ——
            //   原版翻残骸时逐条 `RemoveTraitSilently`（只摘 `CurrentTraits` 那条），但
            //   `ResetToValuesInHand` 那一路会把 `activeEffects +0x108` 整个 `List.Clear()`
            //   （判据 = `ResetToValuesInHand.c:45-70`，`E10TB` 已按那条落地）⇒ 记录账与词条账**同生共死**。
            _traitEffects.Clear();
        }

        // ================================================================================================
        //  🆕 2026-10-26（`E10TB` · 完全复刻批次 9 · **P-05**）—— 残骸那一本书 ＋ 原版两个方法的等价位
        //  判据（🥇 第一权威 · 全量反编译，逐句读过）：
        //    · `decomp_full/EntityScript__HasCurrentTrait.c:1-75`          → `Has` 的残骸档（见 `Has` 的 doc）
        //    · `decomp_full/CardScript__TransformFromRemnant.c:1-19`       → `TransformBackFromRemnant`
        //    · `decomp_full/CardScript__ResetToValuesInHand.c:1-176`       → `ResetToValuesInHand`
        //    · `decomp_full/CardScript__RemoveEffect.c:1-279`              → `RemoveEffectRecord`
        //    · 调用链 `BattleManager__AddTransformFromRemnant.c`（`SetLeavingRemnant` + 入队 0x48）
        //      → `BattleManager__ResolveTransformFromRemnant.c`（`TransformFromRemnant`）→ 完成回调
        //      `BattleManager.__c__DisplayClass555_0___ResolveTransformFromRemnant_b__0.c:7-31`
        //      （`ResetToValuesInHand(true)` → 广播 Reanimated → `SetAsUnitInPlay` → `ResetSummonSickness`
        //       → `ActivateTraitsOnSummonOrEnchantment` → `UpdateWhileInPlay` → `CheckStun`）
        //  ⚠️ 按本仓既有做法（先例 `B8_AX` 的类尾原语）：**原语备在这里、调用点的改动交调度台** ——
        //     ② 与 ③ 的真身调用点都在 `Core/EffectResolver.cs`（不在本代理白名单）⇒ 改动请求见报告 §四。
        // ================================================================================================

        /// <summary>
        /// **残骸那一本书**：这具残骸身上「**残骸期间**由 90 档（`addTraitRemnant`）授予的」关键词。
        ///
        /// <para>**为什么必须有第二本**：原版有两本账 —— `CurrentTraits(+0x128)`（卡面那份 ＋ 3 档授予）
        /// 与 `activeEffects(+0x108)`（逐条效果记录），而 `HasCurrentTrait` 在**残骸档只扫
        /// `activeEffects` 里 `buffType 90` 那一条**（`EntityScript__HasCurrentTrait.c:1-75`）。
        /// 我们只有一本合并的 `_keywords` ⇒ 残骸分不出「哪一个是 90 档给的」⇒ 本集合专记那一批。</para>
        ///
        /// <para>**谁写**：<see cref="AddKeyword"/>（**且此刻 `IsRemnant &amp;&amp; !IsLeavingRemnant`**）——
        /// 闸③（`CardScript__AddEffect.c:159-170`）对残骸只放行 `addStats` 的生命增减与 `buffType 90`，
        /// 而前者不授 trait ⇒ 「残骸期间能被挂上的关键词」就是 90 那一批（判据只此一处）。</para>
        /// <para>**谁清**：<see cref="RemoveAll"/>（单条）· <see cref="ClearAllKeywords"/>（整本）·
        /// <see cref="TransformBackFromRemnant"/>（翻回时整本摘光 —— 原版翻回后 `HasCurrentTrait`
        /// 走非残骸支、只扫 `buffType 3`，那条 90 记录**仍在 `activeEffects` 里但看不见**）。</para>
        /// <para>**谁读**：<see cref="Has"/>（**只此一处**）。⛔ `KwValue` 不读 —— 原版
        /// `GetCurrentTraitValueWithModifiers.c:1-80` 里**没有** `isRemnant` 判断。</para>
        /// </summary>
        readonly HashSet<string> _remnantTraits = new HashSet<string>();

        /// <summary>
        /// **原版 `CardScript.ResetToValuesInHand(bool clearBacklash)` 的等价位**
        /// （`decomp_full/CardScript__ResetToValuesInHand.c:1-176` 逐句）。语义 = 「把这张在场牌**重置成
        /// 手牌里的数值**」；原版唯一的带 `true` 调用点 = **再造/翻回**那一路
        /// （`…_ResolveTransformFromRemnant_b__0.c:7`），所以 ① 也走它。
        ///
        /// <para>**逐句照抄（顺序同原版）**：
        /// ① `activeEffects.Clear()`（`+0x108`）⇒ 我们那几本：`_statMods` / `_grants` / `_buffs` /
        ///    `_grantedOps` / `_grantedText` / `_auraFx` / `_auraKw` / `_removedTraits` / `ExtraTriggers`；
        /// ② `activeEnchantments.Clear()`（`+0x110`）⇒ ⚠️ **不在这里**：我们的附魔挂在
        ///    `CardInstance.ActiveEnchantments`（`Enchantments` 那一套）⇒ 清点归调用方（改动请求 §四·2）；
        /// ③ `questCounter(+0x270) = 0` ⇒ `Instance.QuestCounter = 0`；
        /// ④ 单位：`currentMaxHealth(+0xa4) = 卡面血(+0x9c)` · `currentBaseMeleeAttack(+0x7c) = 卡面近战(+0xa4)` ·
        ///    `currentBaseRangedAttack(+0x90) = 卡面远程(+0xa8)` · `currentHealth(+0x68) = currentMaxHealth` ·
        ///    `currentArmourValue(+0xb8) = 卡面护甲(+0xa0)`；
        /// ⑤ `attacksThisTurn(+0x48)=0` · `usedActiveAbility(+0x4c)=false` ·
        ///    `usedActiveAbilityTimesPerTurn(+0x50)=0`；
        /// ⑥ 摘 `jam(130)` · `stun(100)`（＋同名 effect）· `blind(975)`（＋同名 effect）· `stealth(70)`；
        /// ⑦ 残骸档：`isRemnant=0`、`isLeavingRemnant=1`（原版 `.c:147-152` 的 `*(undefined2*)(+0x65)=0x100` ——
        ///    **两个字节一次写**，所以翻回中间态是「不是残骸、正在离场」）；
        /// ⑧ `stunnedAtStartOfTurn(+0x55)=false` · `removeMarkerlightAtEndOfTurn(+0x57)=false`；
        /// ⑨ `SetBaseCardTraits()` ⇒ 卡面那批 trait 重挂（我们：`_keywords` 只留卡面）。
        /// ⑩ 表现层那四句（`StopAnimsInPlay` / `DestroyRemnantBody` / `SetSpriteAlpha` /
        ///    `CardHighlight.ChangeState`）我们没有那一层 ⇒ 不在这里。</para>
        ///
        /// <para>🔴 **原版【没做】的，我们也别做**：`companionCounter(+0x60)` / `tideCounter(+0x5C)` /
        /// `orderInPlay(+0x44)` / `isPraying(+0x67)` **一个都不动**（`.c` 全文没碰它们）
        /// ⇒ 两个计数器（我们的 `CardInstance.CompanionCounter` 那本）**跨翻回保留**。</para>
        ///
        /// <para>⚠️ **没建的两位**（如实标，铁律 2）：`currentDropPodHealth/Attack(+0xcc/+0xe0)`
        /// 我们**没有这两个字段**（drop pod 在我们这边是关键词 `droppod` 的 `KwValue`，
        /// 见 `CardDef.cs` 的 DropPod 那一族注释）⇒ 原版 ④ 里那两句**不搬**。
        /// 🔴 `alreadyTriggeredBacklash(+0x222)` 没有字段 —— 判据 `CardScript__ResetToValuesInHand.c:138-140`（`if (param_2 != 0) *(byte*)(this + 0x222) = 0;`，**只在 `clearBacklash` 为真时清**；字段名 `dump.cs:35448`）⇒ 我们按「本回合死过」(`ctx.DiedThisTurn`) 算，而原版是**每卡一份**的位 ⇒ 两者**不等价**（同回合「死过 ⇒ 回手/翻回 ⇒ 再上场」原版能再触发、我们挡住）
        /// ⇒ `clearBacklash` 形参**今天仍不起作用**（保留只为让调用点与原版同形；要真做必须**同时**补「字段 + 本方法这一清 + `RuleCore` 的写点/读点」三步，⛔ 只加字段不加消费口 = 死代码）。</para>
        /// </summary>
        public void ResetToValuesInHand(bool clearBacklash)
        {
            // ① activeEffects.Clear() —— 我们那几本
            _statMods.Clear();
            _grants.Clear();
            _buffs.Clear();
            _grantedOps.Clear();
            _grantedText.Clear();
            _auraFx.Clear();
            _auraKw.Clear();
            _removedTraits.Clear();
            ExtraTriggers.Clear();

            // ③ questCounter(+0x270)
            if (Instance != null) Instance.QuestCounter = 0;

            // ④ 数值回卡面 ＋ ⑨ SetBaseCardTraits()
            var card = Card;
            if (card != null)
            {
                MaxHealthBase = card.Health;
                AttackBase = card.Attack;
                RangedAttackBase = card.RangedAttack;
                _keywords.Clear();
                foreach (var kv in card.Keywords) _keywords[kv.Key] = kv.Value;
                Armor = KwValue(KeywordTable.Armour);
            }
            Health = MaxHealth;            // currentHealth = currentMaxHealthProcessed

            // ⑤ 每回合那几位
            AttacksThisTurn = 0;
            // 🔴🆕 2026-10-27（`B9_W27ENG`）：`ActiveAbilityUsed`（原版 `+0x4C`，**本局一次**）也在这一档清 ——
            //   判据 `CardScript__ResetToValuesInHand.c:131`（原版那条方法里 `+0x4c` 与 `+0x50` 一起清）。
            UsedActiveAbilityThisTurn = ActiveAbilityUsed = false;
            OathUsesThisTurn = 0;          // 原版 `+0x50 usedActiveAbilityTimesPerTurn`

            // ⑥ 摘 jam / stun / blind / stealth（`RemoveTraitSilently` ⇒ 不重算「能动/能攻击」）
            RemoveAll(KeywordTable.Jam, silent: true);
            RemoveAll(KeywordTable.Stun, silent: true);
            RemoveAll(KeywordTable.Blind, silent: true);
            RemoveAll(KeywordTable.Stealth, silent: true);

            // ⑧ 两个「回合开始时」的状态位
            BlindedAtStartOfTurn = false;
            RemoveMarkerlightAtEndOfTurn = false;

            // ⑦ 残骸档：两个字节一次写 ⇒ 不是残骸、正在离场
            if (IsRemnant)
            {
                IsRemnant = false;
                IsLeavingRemnant = true;
            }
            _remnantTraits.Clear();        // 90 档那条记录原版仍在，但非残骸支看不见 ⇒ 整本摘
            AuraRemnantStay = false;       // 光环那本账同属 activeEffects
            OathTripleGranted = false;
            OathAllTurnsGranted = false; if (clearBacklash) AlreadyTriggeredBacklash = false;   // 原版 `.c:138-140`（⛔ 不能新增行：本文件下游有 A 行落点）
        }

        /// <summary>
        /// **原版 `CardScript.TransformFromRemnant(Action onComplete)` 的等价位**
        /// （`decomp_full/CardScript__TransformFromRemnant.c:1-19`，全文三句）。
        ///
        /// <para>原版这三句 = ① `*(undefined2*)(+0x65) = 0`（**一次清 `isRemnant` 与
        /// `isLeavingRemnant` 两个字节**）② `BattleCardUI.ChangeCardToRemnant(ui, 0, …)`（表现层，
        /// `BattleCardUI__ChangeCardToRemnant.c:1-42` **纯视觉**：销毁残骸体、显示卡面、
        /// `HideExtraEffects` —— 里面**没有任何数值恢复**）③ `UpdateHealthText()`。
        /// **属性不由这个方法恢复** —— 它在动作完成回调里由 `ResetToValuesInHand(true)` 恢复
        /// （见那个方法的 doc），两条一起才是原版的「翻回」。</para>
        ///
        /// <para>⇒ 本方法 = ①＋（调用方紧接着要做的）`ResetToValuesInHand(true)` 一起，**原地**完成翻回。
        /// 「原地」很重要：原版是**同一个 `CardScript` 对象**翻回来（`isRemnant` 只是两个字节），
        /// 所以 `companionCounter` / `tideCounter` 这类**没被重置字段**天然延续；
        /// 我们若换一个新 `UnitState`（改前的做法，`EffectResolver.DoReanimate`）就要逐个搬。</para>
        /// </summary>
        public void TransformBackFromRemnant()
        {
            ResetToValuesInHand(clearBacklash: true);   // 原版完成回调第一句（b__0.c:7）
            // ① `*(undefined2*)(+0x65) = 0` —— 两个字节一起清（`isRemnant` + `isLeavingRemnant`）
            IsRemnant = false;
            IsLeavingRemnant = false;
        }

        /// <summary>
        /// **原版 `CardEffect` 里与「按记录摘效果」相关的那几列**（只这一批 case 用得到）。
        /// 字段偏移 = `dump.cs` 的 `CardEffect`（`+0x38 buffType` / `+0x3c trait` / `+0x48 meleeAttackChange` /
        /// `+0x58 armourChange` / `+0x60 healthChange` / `+0x74 permanentBuff`）。
        /// ⚠️ 我们**没有** `activeEffects` 这份列表（数值在 `_statMods` / trait 在 `_keywords` /
        /// 附魔在 `Enchantment`）⇒ 摘「一条记录」时由调用方把这条记录的**相关列**投进来。
        /// </summary>
        public sealed class EffectRecord
        {
            /// <summary>`CardEffect.buffType(+0x38)` —— 语义见 `BuffType` 枚举（`dump.cs:45430-45462`）</summary>
            public int BuffType;
            /// <summary>`CardEffect.trait(+0x3c)`（`addTrait` / `addTraitRemnant` 才有）</summary>
            public string Trait;
            /// <summary>`CardEffect.meleeAttackChange(+0x48)`</summary>
            public int MeleeChange;
            /// <summary>`CardEffect.armourChange(+0x58)`</summary>
            public int ArmourChange;
            /// <summary>`CardEffect.healthChange(+0x60)`</summary>
            public int HealthChange;
            /// <summary>`CardEffect.permanentBuff(+0x74)` —— 非 0 ⇒ 这条效果**摘不掉**；同行第二个字段 = `untilNextAttack(+0x36)`</summary>
            public bool Permanent;  public bool UntilNextAttack;   // +0x74 摘不掉 / +0x36 到期档（全文见类尾「untilNextAttack 到期档」那一段）
        }

        /// <summary>
        /// **原版 `CardScript.RemoveEffect(CardEffect removedEffect, bool fromJam)` 的等价位**
        /// （`decomp_full/CardScript__RemoveEffect.c:1-279`，🥇 全文逐 case 对过）。
        ///
        /// <para>**原版的四步**：① `permanentBuff(+0x74) != 0 ⇒ 直接 return`；
        /// ② `activeEffects(+0x108).Remove(effect)`；③ `switch (buffType)` 的逐档收尾；④ 收尾后各自
        /// `UpdateFigures` / `UpdateTraitIcons`。我们没有 ②（见 <see cref="EffectRecord"/> 的 doc），
        /// ①③④ 照抄。</para>
        ///
        /// <para>**逐档**（`.c` 行号 = 现读）：
        /// · `1 addStats`（`:155-192`）：`currentHealth = min(currentHealth, currentMaxHealthProcessed)`；
        ///   `armourChange(+0x58) != 0` ⇒ `AddArmour(-armourChange)`。⇒ 我们：`ClampHealthToMax()` +
        ///   `RemoveKeyword(armour, ArmourChange)`（护甲在我们这边**只有** `armour` 关键词一本账，
        ///   `RemoveKeyword` 会同时减 `_keywords` 与 `Armor` 字段 —— 与 `AddArmour(-x)` 等价）。
        /// · `2 / 10`：只 `UpdateFigures`（我们无表现层 ⇒ 无动作）。
        /// · `3 addTrait` / `90 addTraitRemnant`（`:60-96`）：**印在卡面上就留着**；否则
        ///   `RemoveTraitSilently(trait)`。⚠️ 原版还有两条特判（`trait==230 dropPod ⇒ RemoveDropPod`、
        ///   `trait==610 pack ⇒ +0x358=0`）**我们没有落点**（前者在我们这边是 `RuleCore.OpenDropPod`
        ///   那条独立机制，后者那格缓存在 `Aura.cs`）⇒ 如实留缺口，见报告 §五。
        /// · `4 / 6 / 7 / 8 / 9 / 0xB / 0xD / 0xE`：**空档**（`.c:196-201` 只有 `break`）。
        /// · `5 swapStats`（`:202-238`）：`ChangeBaseAttack(卡面近战)` + `ChangeMaxHealth(卡面血)` + 夹血。
        /// · `0xC / 0xF / 0x10`（附魔）：摘 `activeEnchantments` 那条，并在
        ///   `cardType==0x28 && spellType==0x78 && !fromJam` 时 `BroadcastQuestResolved` + `questCounter=0`
        ///   ⇒ **不在本方法**（`Enchantment.Remove` 那一套），`fromJam` 这个形参就是给它留的。
        /// · `0x2D(45) addStatsDropPod`（`:98-152`）：`currentDropPodHealth(+0xcc) -= healthChange(+0x60)`、
        ///   `currentDropPodAttack(+0xe0) -= meleeAttackChange(+0x48)`。我们在这一档**只有血量那半**能落
        ///   （drop pod 血量 = `KwValue(droppod)`）⇒ 攻击那半如实记「未建」。
        /// · `default`（`:262-272`）：`buffType != 0x28(40 replaceAbility) ⇒ return`；40 档 =
        ///   `hasActiveAbility(+0x220) = 卡面 hasActiveAbility(+0x2A9)` + `activeAbilityRef(+0x118) = null`。
        ///   ⚠️ 我们的 `Ability` **恒读卡面**（`Card.Ability`，没有运行时覆盖位）⇒ 这一档
        ///   **已经等于「重置回卡面」**，等价实现 = 无动作（如实标「形状差、净行为等价」）。</para>
        /// </summary>
        public void RemoveEffectRecord(EffectRecord rec, bool fromJam = false)
        {
            if (rec == null) return;
            if (rec.Permanent) return;      // ① `permanentBuff(+0x74) != 0` ⇒ 摘不掉

            switch (rec.BuffType)
            {
                case 1:                     // addStats
                    ClampHealthToMax();
                    if (rec.ArmourChange != 0)
                        RemoveKeyword(KeywordTable.Armour, rec.ArmourChange);   // = `AddArmour(-x)`
                    SyncAttackTypeAfterStatChange();
                    break;

                case 3:                     // addTrait（⚠️ 本方法拿不到「哪一条记录」⇒ 只摘词条；要按记录摘那条 addTrait 走类尾 `RemoveTraitEffect`，两处必须一起改）
                case 90:                    // addTraitRemnant（残骸那本账也一起摘，见 RemoveAll）
                    if (string.IsNullOrEmpty(rec.Trait)) break;
                    if (Card != null && Card.Has(rec.Trait)) break;    // `HasDefaultTrait` ⇒ 印在卡面上，留着
                    RemoveAll(rec.Trait, silent: true);                // `RemoveTraitSilently`
                    break;

                case 5:                     // swapStats：回卡面攻血 + 夹血
                    if (Card != null)
                    {
                        AttackBase = Card.Attack;
                        MaxHealthBase = Card.Health;
                    }
                    ClampHealthToMax();
                    SyncAttackTypeAfterStatChange();
                    break;

                case 45:                    // addStatsDropPod：只有血量那半有落点（见 doc）—— // 🔶 自己设计（非原版）：drop pod 血量用 `KwValue(droppod)` 表达（原版是 `currentDropPodHealth +0xcc` 字段）· 判据拿不到：我们的 drop pod 是 `RuleCore.OpenDropPod` 那条独立机制、没有那两个字段（既有口径见 `CardDef.cs` 的 DropPod 族注释）· 调度台 E10TB 2026-10-26（照既有模型延伸，未新增口径）
                    if (rec.HealthChange > 0) RemoveKeyword(KeywordTable.DropPod, rec.HealthChange);
                    else if (rec.HealthChange < 0) AddKeyword(KeywordTable.DropPod, -rec.HealthChange);
                    break;

                case 40:                    // replaceAbility：我们的 Ability 恒读卡面 ⇒ 已是「重置态」
                    break;

                // 2 / 10 = 只 UpdateFigures；4/6/7/8/9/0xB/0xD/0xE = 空档；
                // 0xC/0xF/0x10 = 附魔（Enchantment.Remove，`fromJam` 是它那一支的闸）。
                // 以上都不动状态 ⇒ 与 `.c` 一致，⛔ 别补动作。
                default:
                    break;
            }
        }

        // ================================================================================================
        //  🆕 2026-10-26（第八十四会话 · `W13EN` · 账 64 / P-02 的 `addTrait` 半边）
        //     **`addTrait` 的「一条实例 ＋ `traitValue`」账**（原版 `CardEffect.traitValue // +0x40`）
        //  —— **本段全部追加在类尾**（⛔ 别插到中间的成员之间：`账/*.tsv` 按 `文件:行号` 引，插一行就漂）
        // ================================================================================================
        //  原版的形状（判据全在 `d:/2/tools/decomp_full/`，逐句亲读）：
        //   · **值** = `EntityScript.GetCurrentTraitValueWithModifiers(trait)`
        //     （`EntityScript__GetCurrentTraitValueWithModifiers.c:1-80`）：
        //         `Σ ( activeEffects(+0x108) 里 { buffType == 3 addTrait ∧ trait(+0x3c) 命中 } 的 traitValue(+0x40) )`
        //         ＋ 卡面那份（`CurrentTraits(+0x128)` 的 `_traitValue(+0x1c)`）。
        //     ⇒ **记录列表才是「值」的来源**；`CurrentTraits` 只管「在不在」
        //       （`CardScript__AddTraitSilently.c` 只建一条默认值的 `CardTrait` 并 `Linq.Any` 去重）。
        //   · **合并** = `CardScript__AddEffect.c` 的 `case 3`（现读 `:462-570`）：新记录进来时
        //     **倒序**扫「同 trait ∧ 同 `markOfChaosType(+0x78)` ∧ 同 `daemonHostType(+0x7c)` ∧
        //     `CardEffect.SameTimingAsEffect`（五个 `until*` 位）∧ **两边 `extrinsic(+0x30)` 都为 0**」的旧记录：
        //         `_stackable == add(10)`     ⇒ `旧.traitValue += 新.traitValue`（`:544`）
        //         `_stackable == replace(0)`  ⇒ `旧.traitValue = Max(旧, 新)`（`:531-535`）
        //         `_stackable == count(20)`   ⇒ `旧.traitValue += 1`（`:554`；新建那条先被**强置 1**，`:496`）
        //     匹配不上 ⇒ **新建一条**记录、`traitValue` = 新的值（count 档强置 1）追加进 `+0x108`。
        //   · `_stackable` 真值表 = `判据/CardTrait_stackable_真值表_第2轮.md`：池内 **74 add ·
        //     1 count（`markOfChaos 330`）· 0 replace**（`berzerk`/`dodge` 没有 SO）。
        //
        //  🔴 **为什么要有这一层（不改 `EffectResolver` 的前提下能做什么）**：
        //     我们把一条 `addTrait` 落地成「一路 `AddKeyword` 加值」，**没有「一条记录」这个概念** ⇒
        //       · 多目标 / 重复 N 遍时我们做的是「N 次加值」—— **净读数与「一条记录 `traitValue = change × N`」等价**
        //         （原版那条 `AdjustCardEffect` **只乘五对数值字段，⛔ 不碰 `traitValue(+0x40)`**，
        //         判据 = `decomp_full/AbilityLogic__AdjustCardEffect.c:1-59` 全文本笔亲读；
        //         `traitValue` 的 ×N 是由「同一条记录被合并 N 次」自然得到的），但**没有那条实例**可给
        //         `RemoveEffect` 按记录摘、也答不出「这条 trait 是哪条记录、谁给的」。
        //       · `count` 档每次 +1 这一位，`AddKeyword` 里 `IsCountStackableTrait` 已经等价实现了。
        //     ⇒ 本层把**记录的合并语义**备好；`EffectResolver` 侧把 `addTrait` 那一路改成调
        //       `ApplyTraitEffect(值)` 就落到「一条实例」上（**改动请求 → 报告 §五**；本写手不动 `EffectResolver`）。
        //
        //  ⚠️ **两本账的关系**：`_keywords` 仍是**唯一的数值读点**（`Has`/`KwValue` 全走它），
        //     本记录账只是它的**来路**（`Σ TraitEffects` 恒 ≤ `KwValue`：卡面那份与其它授予路不在记录里）。

        /// <summary>原版 `BuffType.addTrait = 3`（`dump.cs:45430-45462` 的枚举；与 <see cref="EffectRecord.BuffType"/> 同一套值）。</summary>
        public const int BuffTypeAddTrait = 3;

        /// <summary>原版 `BuffType.addTraitRemnant = 0x5a(90)`（残骸那一档；`AddEffect` 的 `case 3` 与它**共用同一个合并循环**）。</summary>
        public const int BuffTypeAddTraitRemnant = 90;

        /// <summary>原版 `CardTrait.TraitStackType.replace = 0`（`dump.cs:21302-21308`）。</summary>
        public const int TraitStackReplace = 0;
        /// <summary>原版 `CardTrait.TraitStackType.add = 10`。</summary>
        public const int TraitStackAdd = 10;
        /// <summary>原版 `CardTrait.TraitStackType.count = 20`。</summary>
        public const int TraitStackCount = 20;

        /// <summary>
        /// 原版 `CardEffect` 里 `addTrait` 那一族的**一条记录**（`activeEffects(+0x108)` 里的一条）。
        /// 字段偏移 = `dump.cs` 的 `CardEffect`（`+0x30 extrinsic` / `+0x38 buffType` / `+0x3c trait` /
        /// `+0x40 traitValue` / `+0x78 markOfChaosType` / `+0x7c daemonHostType`）。
        /// ⚠️ 我们**没有**原版那条统一的 `activeEffects` 列表（数值在 `_statMods` / trait 在 `_keywords` /
        /// 附魔在 `Enchantment`）⇒ 这本账**只收 `addTrait` 这一族**（与 <see cref="EffectRecord"/> 同一片缺口，
        /// 那一段的 doc 已如实标过）。
        /// </summary>
        public sealed class TraitEffectRecord
        {
            /// <summary>`CardEffect.trait(+0x3c)` —— 我们的关键词名（`KeywordTable` 那套）。</summary>
            public string Trait;
            /// <summary>`CardEffect.buffType(+0x38)` —— `3 addTrait` / `90 addTraitRemnant`（两边共用合并语义）。</summary>
            public int BuffType = BuffTypeAddTrait;
            /// <summary>`CardEffect.traitValue(+0x40)` —— **累计值**（同一条反复施加时按 `StackType` 合并）。</summary>
            public int TraitValue;
            /// <summary>合并语义（`CardTrait._stackable`）：`0 replace / 10 add / 20 count`。</summary>
            public int StackType = TraitStackAdd;
            /// <summary>`CardEffect.extrinsic(+0x30)` —— 为真 ⇒ **不与任何记录合并**（原版那条 `两边 extrinsic == 0` 的闸）。</summary>
            public bool Extrinsic;
            /// <summary>`CardEffect.markOfChaosType(+0x78)`（合并键之一）。</summary>
            public int MarkOfChaosType;
            /// <summary>`CardEffect.daemonHostType(+0x7c)`（合并键之一）。</summary>
            public int DaemonHostType;
            /// <summary>🔶 自己设计（非原版的数据形状）：原版合并键里的「五个 `until*` 位 + isPlayer」
            /// （`CardEffect__SameTimingAsEffect.c` 逐句比 `+0x32..+0x36` 与 `SameIsPlayerEffect`）
            /// 我们压成**一个位掩码**（语义相同、少 5 个字段）：bit0..4 =
            /// `untilEndOfTurn / untilPlayerTurnStart / untilEnemyTurnStart / untilFollowingPlayerTurnStart /
            /// untilNextAttack`，bit5 = `isPlayer`。`0` = 五个位全假（池内 `addTrait` 的绝大多数）。
            /// · 判据：`dump.cs:119090-119099` 的 `CardEffect` 字段序 + 上面那个方法的比较链 ·
            /// 为什么自己定：我们的效果记录没有这五列（没有 `activeEffects` 那张表）· `W13EN` 2026-10-26（承 `AB` 既有「自己设计」口径，未新增行为）</summary>
            public int TimingBits;
            /// <summary>谁给的（我们的日志/收回用；原版没有这一列 —— 🔶 自己设计，理由同上）。
            /// 只为「以后按来源整份收回」留一个抓手，今天**没有消费者**。</summary>
            public string Source;

            public override string ToString()
            {
                return (Trait ?? "?") + "=" + TraitValue
                     + "（buffType " + BuffType + "，stackType " + StackType
                     + (Extrinsic ? "，extrinsic" : "") + "）";
            }
        }

        readonly List<TraitEffectRecord> _traitEffects = new List<TraitEffectRecord>();

        /// <summary>只看不写：这个人身上**由 `addTrait` 记录授予的**那批（原版 `activeEffects(+0x108)` 里
        /// `buffType ∈ {3,90}` 那一族的对应物）。⚠️ 卡面那份与光环/手牌加成**不在**这里。</summary>
        public IReadOnlyList<TraitEffectRecord> TraitEffects { get { return _traitEffects; } }

        /// <summary>某条 trait 的**记录侧**累计值（原版 `GetCurrentTraitValueWithModifiers` 里
        /// `Σ traitValue` 那一半；**不含**卡面那份、也不含光环/直接 `AddKeyword` 的路）。</summary>
        public int TraitEffectValue(string trait)
        {
            if (string.IsNullOrEmpty(trait)) return 0;
            int sum = 0;
            for (int i = 0; i < _traitEffects.Count; i++)
            {
                var r = _traitEffects[i];
                if (r != null && r.Trait == trait) sum += r.TraitValue;
            }
            return sum;
        }

        /// <summary>
        /// 原版 `CardTrait._stackable` 的**逐 trait 真值**（判据 = `判据/CardTrait_stackable_真值表_第2轮.md`）：
        /// 本 build 75 个有 SO 的词条里 **74 个 `add(10)`、1 个 `count(20)`（`markOfChaos 330`）、0 个 `replace(0)`**
        /// ⇒ 这里只写死那条已实读定案的 `count`，其余一律 `add`。
        /// ⚠️ 与 <see cref="IsCountStackableTrait"/> 是**同一条真值的两个读法** —— 这里直接调它，⛔ 别各写一张表。
        /// </summary>
        public static int TraitStackTypeOf(string trait)
        {
            return IsCountStackableTrait(trait) ? TraitStackCount : TraitStackAdd;
        }

        /// <summary>
        /// **按原版 `CardScript__AddEffect.c` 的 `case 3` 施加一条 `addTrait`** ——
        /// 合并进已有记录（`StackType` 决定 `+= / Max / +1`）或新建一条，
        /// 并把**增量**落到 `_keywords`（走 <see cref="AddKeyword"/>，副作用一条不少）。
        ///
        /// <para>返回命中的那条记录；`null` = 这次没挂上（`AddKeyword` 拒收：毒 × 抗性那一档 ——
        /// 原版 `.c:469-471` 也是**整支跳过、不追加记录**）。</para>
        /// <para>⚠️ **`value &lt;= 0` 直接返回 `null`**：那属于 `removeTrait(4)` 那一族（**不在**
        /// `AdjustCardEffect` 的 8 个 buffType 里、原版根本不乘也不记 `addTrait` 记录）⇒ ⛔ 别拿本方法做「减」。</para>
        /// </summary>
        /// <param name="value">**这一次施加的值**（= 原版 `CardEffect.traitValue`）。多目标 / 重复 N 遍时
        /// 调用方**逐次调**（值不变）即可 —— 合并语义会把它们累成「一条记录 `traitValue = change × N`」，
        /// 与原版 `AdjustCardEffect` 的 ×N 结果**等价**（那条只乘五对数值字段，⛔ 不碰 `traitValue`）。</param>
        /// <param name="stackType">`-1` = 按 <see cref="TraitStackTypeOf"/> 现查（推荐，别手填）。</param>
        /// <param name="timingBits">合并键之一，见 <see cref="TraitEffectRecord.TimingBits"/>。</param>
        public TraitEffectRecord ApplyTraitEffect(string trait, int value, int buffType = BuffTypeAddTrait,
                                                  int stackType = -1, string source = null,
                                                  bool extrinsic = false, int markOfChaosType = 0,
                                                  int daemonHostType = 0, int timingBits = 0)
        {
            if (string.IsNullOrEmpty(trait) || value <= 0) return null;
            int stack = stackType >= 0 ? stackType : TraitStackTypeOf(trait);
            // count 档：新建那条 `traitValue` 先被**强置 1**（`CardScript__AddEffect.c:496`）
            int incoming = (stack == TraitStackCount) ? 1 : value;
            TraitEffectRecord rec = null;
            int keywordDelta = incoming;
            if (!extrinsic)
            {
                rec = FindTraitEffect(trait, markOfChaosType, daemonHostType, timingBits);
                if (rec != null)
                {
                    if (rec.StackType == TraitStackCount)
                    {
                        rec.TraitValue += 1;                     // `.c:554` —— 不看给的值
                        keywordDelta = 1;
                    }
                    else if (rec.StackType == TraitStackReplace)
                    {
                        int nv = System.Math.Max(rec.TraitValue, incoming);   // `.c:531-535`
                        keywordDelta = nv - rec.TraitValue;
                        rec.TraitValue = nv;
                    }
                    else
                    {
                        rec.TraitValue += incoming;               // `.c:544`
                        keywordDelta = incoming;
                    }
                }
            }
            // 毒 × 抗性那一档 `AddKeyword` 会拒收 ⇒ **不建记录**（原版也是整支跳过）
            if (!AddKeyword(trait, keywordDelta)) return null;
            if (rec == null)
            {
                rec = new TraitEffectRecord
                {
                    Trait = trait,
                    BuffType = buffType,
                    TraitValue = incoming,
                    StackType = stack,
                    Extrinsic = extrinsic,
                    Source = source,
                    MarkOfChaosType = markOfChaosType,
                    DaemonHostType = daemonHostType,
                    TimingBits = timingBits,
                };
                _traitEffects.Add(rec);
            }
            return rec;
        }

        /// <summary>
        /// 原版合并键的匹配：同 trait ∧ 同 `+0x78` ∧ 同 `+0x7c` ∧ 同 `SameTimingAsEffect` ∧ **两边都非 extrinsic**
        /// （`CardScript__AddEffect.c:492-568`，**倒序扫**，命中第一条即用 ⇒ 这里也倒序）。
        /// ⚠️ 原版那个匹配循环**不要求 buffType 相同**（`case 3` 的记录与 `0x5a addTraitRemnant` 的记录
        /// 走同一个分支，`.c:558-566`）⇒ 这里也不比 buffType。
        /// </summary>
        TraitEffectRecord FindTraitEffect(string trait, int markOfChaosType, int daemonHostType, int timingBits)
        {
            for (int i = _traitEffects.Count - 1; i >= 0; i--)
            {
                var r = _traitEffects[i];
                if (r == null || r.Extrinsic) continue;
                if (r.Trait != trait) continue;
                if (r.MarkOfChaosType != markOfChaosType) continue;
                if (r.DaemonHostType != daemonHostType) continue;
                if (r.TimingBits != timingBits) continue;
                return r;
            }
            return null;
        }

        /// <summary>
        /// **按记录摘一条 `addTrait`**（原版 `CardScript__RemoveEffect.c` 的 `case 3`：从 `+0x108` 摘掉那条记录，
        /// 再 `HasDefaultTrait ? 留着 : RemoveTraitSilently(trait)`）。
        /// 与 <see cref="RemoveEffectRecord"/> 的 `case 3` **同一口径**（那边是「调用方把记录的相关列投进来」，
        /// 这边是「记录本身就在我们账上」）—— ⛔ 两份必须一起改。
        ///
        /// <para>⚠️ **残余（如实记，不是省了）**：原版的值来自 `Σ 剩余记录`，我们只有 `_keywords` 一本账 ⇒
        /// 摘掉一条时「同 trait 的其它记录还留着值」这一格我们**做不到**（整条摘，但**不写**「已移除」簿记 ——
        /// `RemoveTraitSilently` 也不写）。今天零可观察：多记录只可能由本方法造出来，而 `EffectResolver`
        /// 还没切到本方法（那条路仍是一路 `AddKeyword`）；池内 `replace` 档 trait 0 条。</para>
        /// </summary>
        public bool RemoveTraitEffect(TraitEffectRecord rec)
        {
            if (rec == null) return false;
            if (!_traitEffects.Remove(rec)) return false;
            if (string.IsNullOrEmpty(rec.Trait)) return true;
            if (Card != null && Card.Has(rec.Trait)) return true;   // 印在卡面上 ⇒ 留着（原版 `HasDefaultTrait` 那一支）
            RemoveAll(rec.Trait, silent: true);                     // `RemoveTraitSilently`
            return true;
        }

        // ================================================================================================
        //  🆕 **2026-10-27（B9 · 执行代理 `W18AR`）：光环重算周期内「act 两位」的重算时机**
        //
        //  【问题（`D1DG` 定性 · `RE1`–`RE4`）】`Auras.Recompose` / `Auras.RefreshInPlayNumericAndKeywords`
        //    的形状都是「① 整份摘掉 → ③ 重新加回」。而 `ClearAuraGrants` / `ClearAuraNumericAndKeywords`
        //    在 ① 里收回 `fast`/`flank` 时会调 `RuleCore.RecomputeActAndAttack`，`AddAuraKeyword` 在 ③
        //    重挂时**不重算**（原版「光环给的关键词走哪条路」判据未定，见 `RuleCore.RecomputeActAndAttack`
        //    的 doc）⇒ **重算一次 = 停在中间态**：光环给的 `Flank`/`Fast` 单位被重新判成疲劳。
        //    带这类光环的 4 张卡（`AM73 Vitus Gryf` · `DA30 Ravenwing Talonmaster` · `EC8 Alluress` ·
        //    `TAU31 Devilfish`）覆盖的单位，**部署当回合本来能攻击、一有动作就被打回不能**。
        //
        //  【修法（**不新增规则**）】把「要不要重算」从「中途有没有摘掉 `fast`/`flank`」改成
        //    「**整趟重算前后豁免是否真的丢了**」：
        //      · **重算周期内**（`_recomputingAura`，= `Auras.Recompose` / 窄口刷新）⇒ 先不重算；
        //        由 `EndAuraRecompute` 按 `_auraExemptBefore && !现在是豁免` 补一次。
        //        关键词是「摘了又原样挂回」⇒ 净零 ⇒ **不重算**，两位保持原值（这就是窄口刷新**不动** act 两位）。
        //      · **不在周期里**（`RemoveAllAppliedEffectsForJam` 那条 `ClearAuraGrants` 调用）⇒ 照旧**立即**重算，
        //        那是**真移除**（= 2026-10-25 那条判据 `flank||fast ⇒ UpdateSummonSicknessAnims` 的原意）。
        //    ⇒ 净效果：**真丢**（光环来源离场）照旧会疲劳；**摘了又挂回**不再误判。
        //
        //  ⛔ **别改成「`AddAuraKeyword` 重挂时也重算」**（= 原版的另一条路）：那条判据还没查清
        //    （`RecomputeActAndAttack` 的 doc 自己写着「光环那条路**还没查清**」）⇒ 判据不足就别走。
        //  ⚠️ **也别把 `ActionPump.cs` 那行 `RefreshInPlayNumericAndKeywords` 注释回去** —— 它是账 669 的接线，
        //    退回去等于撤销 `BL18` / `UM18` 的修复（`play check` 指纹会退回旧值）。
        // ================================================================================================

        /// <summary>重算周期**开始那一刻**的「召唤病豁免」快照（`fast`/`flank`；判据只此一处 =
        /// <see cref="RuleCore.SummonSicknessExempt"/>）。结束时的比对见本段顶部说明。</summary>
        bool _auraExemptBefore;

        /// <summary>
        /// 光环收回了 `fast`/`flank` 之后**要不要立刻**重算「能动 / 能攻击」两位。
        /// <para>**重算周期内不重算** —— 那时关键词只是「暂时摘掉」，③ 马上会原样重挂（在周期里重算 = 按中间态误判）；
        /// 由 <see cref="EndAuraRecompute"/> 按前后差判。**周期外**（真移除，`RemoveAllAppliedEffectsForJam`）照旧立即重算。</para>
        /// <para>⛔ 别在调用点各写一份 `_recomputingAura` 判断 —— 判据只此一处。</para>
        /// </summary>
        void RequestActRecomputeAfterAuraChange()
        {
            if (_recomputingAura) return;
            RuleCore.RecomputeActAndAttack(this);
        }

        /// <summary>
        /// 🆕 **2026-10-27（B9 · S0-①）**：**限时关键词到期时，把 <see cref="TraitEffectRecord"/> 那一笔扣掉。**
        ///
        /// <para>**为什么是「扣」而不是 <see cref="RemoveTraitEffect"/>**：那个口对应原版
        /// `RemoveEffect`（按记录整条摘 + `RemoveTraitSilently`）—— 它会把**整条 trait 摘光**，
        /// 而我们限时到期的语义是「只撤这一份的增量」（`RemoveKeyword(b.Name, b.Value)` 已经在做）。
        /// 所以本方法**只动记录那一本账**、不碰 `_keywords`（两者分工与 `TempBuff.Mod` / `RemoveStatMod` 同形）。</para>
        ///
        /// <para>**为什么按 `value` 扣而不是整条删**：`add` 档合并语义下，两次同 `TimingBits` 的施加
        /// 会并成**一条**记录（`traitValue = 两次之和`）⇒ 先到期那一份只能扣掉自己那一份；
        /// 扣到 ≤ 0 才摘记录。`count` 档（`markOfChaos`）每次只 +1，所以扣 1。</para>
        ///
        /// <para>⚠️ **三处撤法都要调**（<see cref="RevertBuffs"/> · <see cref="RemoveBuffsFromCard"/> ·
        /// <see cref="RemoveAllAppliedEffectsForJam"/>）—— 漏一处 = 那条记录永远留着（静默）。</para>
        /// </summary>
        public void ExpireTraitEffect(TraitEffectRecord rec, int value)
        {
            if (rec == null) return;
            int n = (rec.StackType == TraitStackCount) ? 1 : (value > 0 ? value : 0);
            rec.TraitValue -= n;
            if (rec.TraitValue <= 0) _traitEffects.Remove(rec);
        }

        /// <summary>
        /// 🆕 **2026-10-27（完全复刻批次 9 · 执行代理 `B9_W27ENG`）**：
        /// **本【局】用过主动能力**（= 原版 `EntityScript.usedActiveAbility`，字段偏移 **`+0x4C`**）。
        ///
        /// <para>**与原版逐条对齐**（判据全文 → `资料/完全复刻/判据/cond341_canUseDuty_ex语义.md`）：</para>
        /// <list type="bullet">
        /// <item>**置位** = `CardScript__ResolveActiveAbilityPlayed.c:23-30`（`:36` `*(undefined1 *)(param_1 + 0x4c) = 1;`，
        ///   闸只有 `hasActiveAbility(+0x220)`）⇒ **任何一次主动能力发动都置**：`duty` / `oath` / `ferocity` / `agenda`
        ///   （四族在 `CardScript__UsedActiveAbility.c:29/34/41/50` **同一个方法**里分支）。</item>
        /// <item>🔴 **`pray(1185)` 不置** —— 它走另一条 `ResolveTriggerPray`（`decomp_full/BattleManager__ResolveTriggerPray.c` 里 `+0x4c` **0 命中**）。</item>
        /// <item>🔴 **不随回合重置**：`CardScript__OnTurnEnd.c:209-210` 只清 `+0x48 attacksThisTurn` 与 `+0x50 usedActiveAbilityTimesPerTurn`。
        ///   ⛔ **别往 `RefreshForNewTurn` 里加清零**（那是 `+0x50` 那一族的位，不是这一位）。</item>
        /// <item>**清零三处**：卡初始化（`CardScript__CardSetup.c:116-123`）· 变形回手牌（`CardScript__ResetToValuesInHand.c:131`；
        ///   我们 = <see cref="ResetToValuesInHand"/>）· **装填 duty**（`BattleManager__ResolveResetDuty.c:6`，我们 = `EffectResolver.DoReloadDuty`）。</item>
        /// </list>
        ///
        /// <para>**谁读它**：`ConditionEval` 的 `cond = 341 canUseDuty`（原版 `MeetsConditionCriteria.c:170-174` =
        /// `*(char *)(param_1 + 0x4c) == '\0'`）；`+0x4c` 同时也是 `CardTraitDuty.IsActive`（`CardTraitDuty__IsActive.c:19`）
        /// 与 `BattleManager.CanUseActiveAbility`（`BattleManager__CanUseActiveAbility.c:93-94`）读的那一位。</para>
        ///
        /// <para>🔴 **与 <see cref="DutyUsed"/> 的分工（⛔ 别合并）**：`DutyUsed` 兼着 `CanUseAlternative` 那道
        /// 「职责本局一次」的闸（只对 `duty` 生效）；本字段是**四族共用**的原版状态位。
        /// 与 <see cref="UsedActiveAbilityThisTurn"/> 的分工：那一位**每回合重置**（原版 `+0x50` 那一族），本字段不重置。</para>
        /// </summary>
        public bool ActiveAbilityUsed;

        // ==================================================================
        //  🆕 2026-10-27（B88_E4 · 引擎·小件 · 账 637）：**「移出游戏」这一态（引擎半边）**
        // ==================================================================
        //
        //  **原版**：`CardScript.cardState(+0x228)` 的 `CardStateOptions.removedFromGame = 11`
        //  （`d:/2/tools/il2cpp_out/dump.cs:45480`）。
        //    · **置位只发生在** `CardScript.RemoveFromGameCard(removedCard)` 的 `removedCard == this` 支
        //      （`cpp2il/out_cpp2il_dev_ilspy/CardScript.cs:16012`：`cardState = 11` +
        //      `SetColliderNotInHand(11)` + `StopAnimsInPlay` + `SetObjectVisibility(11)`）；
        //    · 清除 = 通用裸 setter `SetCardState(newState)`（`cpp2il CardScript.cs:9521`，
        //      **未找到** `removedFromGame → 其它` 的专用转移）；
        //    · 广播那一半（`BroadcastRemoveFromGame`，`decomp_full/BattleManagerSupport__BroadcastRemoveFromGame.c:20-136`）
        //      对场上 + 双方手牌逐张调 `RemoveFromGameCard(那张, 被移出那张)`，而**跳过** `unitRemoved` / `unitRemoved2` 自身
        //      ⇒ 它只走 `ReactToUnitJammed` 的 else 支（= 按来源撤 extrinsic，见 `RemoveExtrinsicEffectsFrom`）。
        //
        //  🔴 **今天是休眠机制**：state 11 的置位支**无调用点**（`T4/C3T4` 2026-10-27 现核），
        //    我们 `UnitState` / `PlayerState` 都没有这一态 ⇒ 本字段是 B9 施工单 637 的**步骤①（引擎状态）**。
        //    ⛔ **别只加这个字段就称为「做完了」**：还差
        //      ② `RuleCore.BroadcastRemoveFromGame(a,b)` 三跳（场上 / 当前方手牌 / 另一侧手牌）；
        //      ③ 视图侧 `SetObjectVisibility(11)` 等价物（隐藏 + 摘碰撞，`BattleDriver`，非本批白名单）。
        //    ⇒ 这三半凑齐才算「移出游戏」落地；本字段只是让 RuleCore 那一半有地方落。
        //
        //  ⚠️ **与「残骸（`IsRemnant`）」是两回事**：残骸是 `cardState 3/17`（死亡后留在场上的那一个），
        //    本字段是 `cardState 11`（**离开棋盘、不参与任何结算**）。⛔ 别合并。
        public bool RemovedFromGame;

        // ==================================================================
        //  🆕 2026-10-27（B88_E4c · 解账 783 `HideWarlordActiveAbilityForTutorial` 的锁）
        //  **实例级「主动能力被隐藏」**（教程关闸：只隐藏**这一张实例**）
        // ==================================================================
        //
        //  **原版**：`CardScript__HideWarlordActiveAbilityForTutorial.c:1-8` —— 只写
        //    `hasActiveAbility(+0x220) = 0`（教程关闸；无画面、无广播）。
        //
        //  🔴 **为什么落成【实例位】而不是写 `CardDef.HasActiveAbility`**：`CardDef` 是**共享不可变**的
        //    （全池同 id 卡共用一份；`EffectResolver.DoLowerCost` 的注释也在提醒「改它会污染整个卡池」）
        //    ⇒ 写 `CardDef` 会**把所有副本一起关掉**。原版写在 `CardScript`（= 一个实例）上，所以
        //    我们的等价物必须在 `UnitState`（实例）。
        //
        //  🔶 表示法差异（非原版的数据形状）：原版把「这一实例没有主动能力」编码在
        //    `hasActiveAbility(+0x220)` 这一个 int 上（写 0）；我们把它拆成第二个实例位
        //    `ActiveAbilityHidden`，读点 = `HasActiveAbility && !ActiveAbilityHidden`。
        //    理由：`CardDef.HasActiveAbility` 是**卡面印的**（不可变定义），不能承载「这一局这张被关掉」。
        //    派生物 = `ActiveAbilityAvailable`（见下）。调度台 2026-10-27 裁定。
        //
        //  ⚠️ **⛔ 别与 <see cref="ActiveAbilitySuppressed"/> 合并**：那个是 **jam** 那条语义
        //    （`ApplyJam` ①：非督军被 jam ⇒ 抑制），本字段是 **教程/关卡显式关闸**。两条来源不同、
        //    将来要分开读（合并成一个位以后分不开）。
        //
        //  **读点（本批不动）**：`RuleCore.CanStartActiveAbility`（E1）—— 建议判
        //    `if (!u.Card.HasActiveAbility可判 || u.ActiveAbilityHidden) return RuleCodes.ErrNoAbility;`
        //    具体接法由 E1 定；本处只提供字段 + 一个方便的合并属性 <see cref="ActiveAbilityAvailable"/>。
        public bool ActiveAbilityHidden;

        /// <summary>「这一实例的主动能力现在能不能用」的合并只读视图 =
        /// `CardDef.HasActiveAbility == 1 且 !ActiveAbilityHidden`。
        /// <para>⚠️ `NullCard`（<see cref="Card"/> == null）⇒ `false`（没有卡就没有能力）。
        /// ⚠️ 它**不**读 <see cref="ActiveAbilitySuppressed"/>（jam 那条另算，见本文件上面那段）。</para></summary>
        public bool ActiveAbilityAvailable
        {
            get { return Card != null && Card.HasActiveAbility == 1 && !ActiveAbilityHidden; }
        }

        // ================================================================================================
        //  🆕 **2026-10-28（完全复刻 · B89 第 1 波 · 执行代理 `E4` · 账 `L1376`）**
        //     **光环新挂上 `fast` / `flank` 的那一跳 ⇒ 重算 `canAct` / `canAttack` 两位**
        //  —— 本段全部追加在类尾（⛔ 别插到中间：`账/*.tsv` 按 `文件:行号` 引 `UnitState.cs`）
        // ================================================================================================
        //  【为什么加这一跳】原版 `CardScript__AddEffect.c` 的 `case 3`（= 加 trait）在**加词那一步之后**
        //    有一段（`.c:570-572`，现读原文）：
        //        `if ((*(int *)(param_2 + 0x3c) == 0x28) || (*(int *)(param_2 + 0x3c) == 0x1cc)) {
        //             CardScript__UpdateSummonSicknessAnims(param_1); }`
        //    `0x28 = fast` · `0x1cc = flank`；`UpdateSummonSicknessAnims.c:16-20` 再转调
        //    `CardScript__ActivateMinion` ⇒ 按 `displaySummonSickness` 重写那两个位
        //    （整条链的逐句判据在 `RuleCore.RecomputeActAndAttack` 的 doc 里，判据**只此一份**）。
        //
        //  🔴 **这一段把 `RuleCore.RecomputeActAndAttack` 的 doc 里那条「判据未定」结掉了** ——
        //    那儿原来写着「光环那条路（`AddAuraKeyword` / `ClearAuraGrants`）没有接 ——
        //    原版光环给的关键词走的是哪条路**还没查清**（`AddTraitSilently` 不重算、
        //    `UpdateWhileInPlay` 那一支没读到实据）」。实读的结论是**两半都对、只是问的是不同的方法**：
        //      · `AddTraitSilently`（`CardScript__AddTraitSilently.c`）确实**自己不重算** —— 那半没错；
        //      · 但**它的调用方 `AddEffect` 在 `.c:570-572` 补了这一跳** ⇒ 走 `AddEffect` 挂 trait 的路
        //        （= 光环/附魔/任何 extrinsic 效果**唯一**的挂载口）**一定会重算**。
        //    ⇒ 关键分支是「**摘**的时候重不重算」：
        //      · **摘**走 `RemoveEffect`（`RemoveExtrinsicEffectsFrom.c:44` 那条 240 刷新路就是这么摘的）
        //        → `case 3` → `RemoveTraitSilently`（`CardScript__RemoveEffect.c:60-78` 现读）
        //        ⇒ **不重算**（`RemoveTrait.c:66-68` 那条重算属于**真移除** `RemoveTrait`，不是这条路）。
        //    ⇒ 结论：**重挂要重算、重算周期内的收回不要重算** —— 与本文件 `RequestActRecomputeAfterAuraChange`
        //      那套「周期内延后、真丢才补」**不冲突、互补**（后者管「收回那一半」，本方法管「挂上那一半」）。
        //
        //  【修的是什么（`RE1`–`RE4` 那条回归的另一半）】**部署那一刻** `ApplyDeployTurnState` 先判过一次，
        //    那一刻光环还没算（`Auras.Recompose` 在后）⇒ 单位被判成「有召唤病」；随后光环给它 `flank`，
        //    而 `AddAuraKeyword` 当年**不重算** ⇒ 这一整个回合它就一直是「不能动」。
        //    `W18AR` 那次只补了「收回」那一半（周期末补一次），「挂上」这一半没人补 ⇒ 本方法补上。
        // ================================================================================================

        /// <summary>
        /// 光环**新挂上**一个关键词之后的收尾：只有 `fast` / `flank` 需要重算「能动 / 能攻击」两位
        /// （判据 = `CardScript__AddEffect.c:570-572`，见本段头注）。
        /// <para>⛔ **判据只此一处**：`AddKeyword` 那条路的重算在它自己的尾巴上（`.c:570-572` 的同一句），
        /// 两条路各调一次 <see cref="RuleCore.RecomputeActAndAttack"/>，⛔ 别把本判断搬到 `SyncKeywordState` ——
        /// 那会连「光环之外」的路一起改成两次调用（幂等，但那是同一条判据的第二份写法）。</para>
        /// <para>**幂等**：`RecomputeActAndAttack` 只按当前状态写两个 bool，重复调用结果不变 ⇒
        /// 在重算周期内被调多次也无副作用（它不碰 `Health` / 费用 / 誓约，见那个方法的逐句判据）。</para>
        /// </summary>
        void RecomputeActAfterAuraKeyword(string keyword)
        {
            if (keyword != "fast" && keyword != "flank") return;
            RuleCore.RecomputeActAndAttack(this);
        }

        // ================================================================================================
        //  🆕 **2026-10-28（完全复刻 · B89 第 1 波 · 执行代理 `E4` · 账 `L875`）**
        //     **`untilNextAttack` 到期档**（原版 `CardEffect.untilNextAttack(+0x36)`）
        //  —— 本段全部追加在类尾（⛔ 别插到中间，理由同上）
        // ================================================================================================
        //  原版怎么用这一档（判据 = `d:/2/tools/decomp_full/` + `cpp2il/out_cpp2il_dev_ilspy/`，逐句亲读）：
        //   · **只有两处扫描它**（全树 `grep -rn untilNextAttack` 现读 = 2 个扫点 + 1 个复位 + 1 个拷贝）：
        //       ① `CardScript__UsedActiveAbility.c:66-84`（ILSpy `CardScript.cs:12167` 的方法体，
        //          扫描在 `:12211-12223`）。它是「这张牌用掉一次行动」的收尾：`:20` 先
        //          `attacksThisTurn(+0x48) += 1`，再依次处理 duty / agenda / oath / ferocity，
        //          **最后**才是这一趟扫描。调用点 = `BattleManager.cs:9431`（`ResolveActiveAbilityPlayed` 之后）。
        //       ② `CardScript__ResolveUnitAttacked.c:79-94`（ILSpy `CardScript.cs:14113-14129`）——
        //          只在 `actingCard == this` 那一支里跑（即**这一记是它自己打出去的**）。
        //     两处的形状**完全相同**：扫 `activeEffects(+0x108)`，
        //     `if (*(char *)(效果 + 0x36) != '\0') CardScript__SendRemoveEffect(card, 效果, 1)`。
        //     ⇒ **语义 = 「这张牌的下一次行动（攻击 / 主动能力）之后整份摘掉」**。
        //   · `SendRemoveEffect`（`CardScript__SendRemoveEffect.c` 全文）= 造一条 `battleActionType 0x10`
        //     （RemoveEffect）→ `BattleManager__AddAutoActionToQueue(…, priority, 0)` ⇒ **入队**，不是立刻摘。
        //   · `CardEffect.GetNewInstanceOfThisEffect`（`CardEffect.cs:118`）把这一位**原样拷**；
        //     `CardScript.cs:17660` 是另一处把它清成 `false` 的复位点。
        //
        //  我们的形状（如实标，⛔ 别当成「一模一样」）：
        //   · `EffectRecord`（本文件那个「按记录摘效果」的列容器）加了 `UntilNextAttack` 一列 = 原版 `+0x36`。
        //     ⚠️ 那一列**加在 `Permanent` 的同一行**（写手 `E4` 2026-10-28 的决定）：`EffectRecord` 在文件
        //     中部，另起一行会让下游 6 处 `账` 落点整体 +1 漂移（其中 5 条状态是 B，
        //     `check_ledger_points --fix` 修不了）⇒ **零行数变化优先**。
        //   · **真正带列表的记录账只有 `_traitEffects`**（`TraitEffectRecord` 那本 `addTrait` 账）⇒
        //     到期扫描落在它上面，判据是 `TimingBits` 的 **bit4**
        //     （`TraitEffectRecord.TimingBits` 的 doc 已写明 bit0..4 = 五个 `until*` 位，bit4 = `untilNextAttack`）。
        //   · **触发器接线不在本批**（账 `L875` 的「缺什么」明写：那两个调用点在 `RuleCore`）⇒
        //     本段只给**原语**，今天 **0 调用点**、**0 可观察变化**。
        //
        //  📊 **可达性（现读数据，不是推测）**：卡数据里 `untilNextAttack` 只有 **1 处**
        //     （`cards_truth.json` 的效果串 `EffectBjornShrine`），而那张卡**不在当前池**
        //     （`cards_engine.json` 现读 0 命中）；解析层也**不产出**这一档
        //     （`EffectResolver.TimingBitsOfDuration` 只有 `""` / `"turn"` / `"nextturn"` 三档）
        //     ⇒ `TimingBitUntilNextAttack` 今天**永远不会被置上**，<see cref="ExpireUntilNextAttack"/>
        //     恒返回 0。这是「机制建好、数据没有」的休眠件（铁律 11：休眠也做），⛔ 别当「已接线」。
        //
        //  ⚠️ **两处如实记的残余**（⛔ 别以为做全了）：
        //     ① 原版**入队**（`SendRemoveEffect`）再摘，我们**同步**摘 —— 同一拍内、下一个动作之前结果相同；
        //        差的是「这一拍的可观察中间态」，而我们的引擎没有那条动作队列入口（`ActionQueue` / `ActionPump`）。
        //     ② `permanentBuff(+0x74)` 与 `untilNextAttack(+0x36)` 同时为真的组合：原版 `RemoveEffect`
        //        第一句就 `return`（摘不掉）。`TraitEffectRecord` **没有 `Permanent` 列** ⇒ 我们表达不了那一格。
        //        等价性论证：每一条 `TraitEffectRecord` 都来自 `ApplyTraitEffect`，而它**没有** `Permanent`
        //        形参 ⇒ 全为 false ⇒ **今天等价**（判据不全就先如实标，⛔ 别加一个猜的默认值）。
        // ================================================================================================

        /// <summary>`TraitEffectRecord.TimingBits` 的 **bit4** = 原版 `CardEffect.untilNextAttack(+0x36)`。
        /// 位序判据 → <see cref="TraitEffectRecord.TimingBits"/> 的 doc。</summary>
        public const int TimingBitUntilNextAttack = 0x10;

        /// <summary>这条 `addTrait` 记录是不是「临攻击到期」那一档（`TimingBits` 的 bit4）。</summary>
        public static bool IsUntilNextAttack(int timingBits)
        {
            return (timingBits & TimingBitUntilNextAttack) != 0;
        }

        /// <summary>「这条 <see cref="EffectRecord"/> 属于 `untilNextAttack` 档吗」——
        /// 原版那两处扫的就是 `CardEffect.untilNextAttack(+0x36)` 这一位。
        /// <para>⚠️ 只是**判定**；`EffectRecord` 没有列表（见那个类的 doc），真正的扫点在
        /// <see cref="ExpireUntilNextAttack"/>（吃 `_traitEffects` 那本账）。</para></summary>
        public static bool IsUntilNextAttack(EffectRecord rec)
        {
            return rec != null && rec.UntilNextAttack;
        }

        /// <summary>
        /// **`untilNextAttack` 到期档的扫描＋摘除**（原版 `CardScript__UsedActiveAbility.c:66-84` /
        /// `CardScript__ResolveUnitAttacked.c:79-94` 那两处，形状完全相同；判据全文 → 本段头注）。
        ///
        /// <para>**语义**：这张牌**下一次行动之后**，把带这一档的记录**整份摘掉**。
        /// 每一条走 <see cref="RemoveTraitEffect"/>（= 原版 `RemoveEffect` 的 `case 3`：
        /// 从记录表摘掉那条 + `HasDefaultTrait ? 留着 : RemoveTraitSilently`）。</para>
        ///
        /// <para>⚠️ **必须在这张牌「用掉一次行动」之后调**（原版两个调用点：主动能力那一路在
        /// `ResolveActiveAbilityPlayed` 之后、攻击那一路在 `actingCard == this` 的分支里）。
        /// 调用点属 `RuleCore`，**不在本批**（账 `L875` 的「缺什么」写明触发接线归后续批次）。</para>
        ///
        /// <para>⚠️ **同步摘 vs 原版入队**、以及 `permanentBuff` 那一格 ⇒ 见本段头注的「两处残余」。</para>
        /// </summary>
        /// <returns>摘掉几条（`0` = 身上没有这一档，或数据里从没置过这一位）。</returns>
        public int ExpireUntilNextAttack()
        {
            if (_traitEffects.Count == 0) return 0;
            int n = 0;
            // 倒序走：`RemoveTraitEffect` 会改 `_traitEffects`
            for (int i = _traitEffects.Count - 1; i >= 0; i--)
            {
                var r = _traitEffects[i];
                if (r == null || !IsUntilNextAttack(r.TimingBits)) continue;
                if (RemoveTraitEffect(r)) n++;
            }
            return n;
        }

        /// <summary>
        /// **原版 `CardScript.alreadyTriggeredBacklash`（`+0x222`，bool）** —— 「这一张卡的死亡已经
        /// 被处理过一次」的**每卡一位**（`dump.cs:35448`；getter/setter 见
        /// `CardScript__get_alreadyTriggeredBacklash.c` / `__set_...c`，各三行）。
        ///
        /// <para>**原版四处读写**（逐条现读）：
        /// · 写 1：`BattleManager._ResolveBacklash_d__451__MoveNext.c:146` —— `BroadcastDeadUnit` **之后**
        ///   对**死者**置 1（`:144` 广播 → `:146` 置位）；`_ResolveMultiDamageAndDestroySelf_d__457__MoveNext.c:316`
        ///   同一形状（摧毁那条路）。
        /// · 写 0：`CardScript__CardSetup.c:95`（建卡/重置卡时与 `+0x65` 一起清）· `CardScript__ResetToValuesInHand.c:138-140`
        ///   （**只在 `clearBacklash` 为真时清**）。
        /// · **读**：`CardScript__ResolveDeadCard.c:234-236` 的那一行复合闸
        ///   `(!isRemnant || cardState == 2) &amp;&amp; alreadyTriggeredBacklash == 0` ⇒ 才
        ///   `TriggerOnMinionDeath(观察者, 死者, …)`。
        ///   ⇒ 语义 = **同一张卡的「死亡族广播」只放行一次**；因为卡可以「变残骸 → 翻回 → 再上场」，
        ///   所以要有一条「回到手牌/重置」时把它清掉的路（这就是 `clearBacklash` 的那个形参）。</para>
        ///
        /// <para>🔴 **本引擎里的覆盖现状（如实标，⛔ 别当成已完全等价）**：
        /// ① **写 1** = `RuleCore.CleanupDeaths` 在「`BroadcastWhen(Die)` ＋ 死因三族」之后置真；
        /// ② **写 0** = 本类的 `ResetToValuesInHand(clearBacklash: true)`（回手 / 洗回牌库 / 翻回 那五处
        ///   调用点都传 `true`，判据 `_ResolveRecallToDeck…:108/177/276` · `_ResolveRecallToHand…:188/255`）；
        /// ③ **读点在本引擎里【不可达】**：原版那个闸防的是「`_ResolveMinionDeath` 与 `_ResolveBacklash`
        ///   两条路各广播一次」，而我们只有 `CleanupDeaths` **一条**死亡路、且当场把槽摘掉
        ///   ⇒ 同一单位不会有两趟死亡族广播。⛔ **因此没有加读点**（加了就是死代码）；
        ///   若将来出现第二条死亡广播路，闸应加在 `BroadcastWhen(WhenEventKind.Die)` 之前。</para>
        ///
        /// <para>**观测口**：打死一个单位 ⇒ 本字段真；把它 `Return … to your hand` / 翻回 ⇒ 变假。
        /// 本条是 `L866`（账：回手/洗回牌库要重置）的落地。</para>
        /// </summary>
        public bool AlreadyTriggeredBacklash;

        /// <summary>
        /// 🔴 **2026-10-28（批次 91 · `W4` · 账 L722）：把「撤某一条 tempBuff」抽成公开口** ——
        /// W1 要把 `CardScript.ClearEndOfTurnEffects` 从「整批重算」改成「逐条 `SendRemoveEffect` 入队」，
        /// 需要「按条撤」这一跳；本方法就是那个唯一口（`RevertBuffs` 已改成循环调它）。
        /// <para>⚠️ **撤除规则只此一份**：关键词记录收缩（`RemoveKeyword`/`ExpireTraitEffect`）＋
        /// `switch (b.Name)` 那串数值撤销（`attack`/`ranged`/`health`/`armour`）＋ 攻型重挑。
        /// ⛔ 别在别处再抄一份（`RevertBuffs` 不再自己写撤法）。返回「这条真被撤掉」（`null` / 不在表里 ⇒ false）。</para>
        /// </summary>
        public bool RemoveTempBuff(TempBuff b)
        {
            if (!RemoveTempBuffCore(b)) return false;
            // 🆕 2026-10-18（`W5`）：限时增益撤掉也会改攻值 ⇒ 重挑攻击型（原版 `UpdateAttackText.c:21-29`）。
            SyncAttackTypeAfterStatChange();
            return true;
        }

        /// <summary>
        /// <see cref="RemoveTempBuff"/> 的**内核**：从 `_buffs` 摘掉（按引用 `LastIndexOf`）+ 关键词记录收缩
        /// （`RemoveKeyword`/`ExpireTraitEffect`）+ `switch (b.Name)` 数值撤销（`attack`/`ranged`/`health`/`armour`）。
        /// ⛔ **不含** `SyncAttackTypeAfterStatChange()` —— 那一步由调用方决定（`RemoveTempBuff` 会调；
        /// `RemoveAllAppliedEffectsForJam` 那一路按**现状不调**，保住既有行为）。撤除规则**全仓只此一份**：
        /// `RevertBuffs` / `RemoveBuffsFromCard` 走 `RemoveTempBuff`，`RemoveAllAppliedEffectsForJam` 走本内核。
        /// </summary>
        bool RemoveTempBuffCore(TempBuff b)
        {
            if (b == null) return false;
            int i = _buffs.LastIndexOf(b);
            if (i < 0) return false;
            _buffs.RemoveAt(i);
            // 🆕 2026-10-27（B9 · S0-①）：关键词那条**记录**也要一起收（`RemoveKeyword` 只管值）。
            if (b.IsKeyword) { RemoveKeyword(b.Name, b.Value); ExpireTraitEffect(b.TraitRec, b.Value); }
            else
            {
                switch (b.Name)
                {
                    // 🔴 **2026-10-25（Stage 2）**：`attack` / `ranged` **不再「减回去」** —— 数值只在 `_statMods` 算一次，撤销 = 把那一格摘掉（投影自动变）。原 `Attack -= b.Value` 是两处记账。
                    case "attack": RemoveStatMod(b.Mod); break;
                    case "ranged": RemoveStatMod(b.Mod); break;
                    // 🔴 **2026-10-25（Stage 2 · 判据 §7·4）**：生命到期是**夹到新上限**、**不是减**（原版 `RemoveEffect.c:147-181`）。
                    case "health": RemoveStatMod(b.Mod); ClampHealthToMax(); break;
                    // 🔴 **2026-10-19（`T01 §六·6`）**：护甲走 `RemoveKeyword`（它会把 `Armor` 一起减）；护甲**不进**修饰表 ⇒ 不需要 `b.Mod`。
                    case "armour": RemoveKeyword(KeywordTable.Armour, b.Value); break;
                }
            }
            return true;
        }
    }
}
