// EffectResolverPartial_B88E2.cs —— 第八十八会话 · 波 1 · 执行代理 `E2`（引擎线）
//
// 为什么单独一个文件：`Core/EffectResolver.cs` 有 17,561 行、且被 `账/*.tsv` 按 `文件:行号` 大量落点引用
//   ⇒ **往里插成员会让下游行号整体漂移**（`check_ledger_points` 立刻报，铁律 18·b / N9 先例）。
//   所以本批**所有新成员**都追加在这里 —— 编译期它与 `EffectResolver.cs` 是**同一个** `partial class RuleCore`
//   （见 `EffectResolver.cs:27`；全仓**没有**叫 `EffectResolver` 的类，照文件名写 `EffectResolver.` 编译不过）。
//
// 本批落的账行（`账/战斗逻辑驱动.tsv`）：
//   · 501 `BattleManager.ResolveChangeBaseAttack` / 711 `CardScript.ChangeBaseAttack(int newAttack)`
//   · 502 `BattleManager.ResolveChangeMaxHealth` / 712 `CardScript.ChangeMaxHealth(int newHealth)`
//   · 746 `CardScript.ForceChangeMaxHealth(int newHealth)`
//   · 698 `CardScript.CanPlayWithOathNowInPlay()`
//   · 1122 `MinionManager.GetNextSlotToCard(CardScript card)`
//   · 特效动画 520 `EntityScript.CreateMarkCardEffect(MarkOfChaosType)`（mark 分支那一半）
//
// ⚠️ **可达性如实标着**：501/502/711/712 在原版本 build 里**没有任何入队点**
//   （`grep -rn "AddActionToQueue(.*changeAttack\|changeAttack" cpp2il/out_cpp2il_dev_ilspy/*.cs` 只命中
//    `UnitOnBoardAttackTypeSelector.cs:192` 的**教程权限检查**，没有 `BattleActionType.changeAttack` 的生产者；
//    `decomp_full/` 里除两个 `Resolve*` 自身外 0 命中）⇒ 三条原语今天是**休眠机制**（铁律 11：休眠也做）。
//   `ForceChangeMaxHealth` 的唯一生产点是 `BattleManager` 初始化督军血量（`BattleManager.cs:15626/15633/15653/15662`），
//   我们那一格由 `RuleCore.ApplyWarlordLifeOverride`（`RuleCore.cs:14845`）负责 —— 它已按三格写，本文件不重复接管。

using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==============================================================================================
        //  账 501 / 711 —— `CardScript.ChangeBaseAttack(int newAttack)` 的**逐句等价位**
        // ==============================================================================================

        /// <summary>
        /// **原版 `CardScript.ChangeBaseAttack(int newAttack)`**（`decomp_full/CardScript__ChangeBaseAttack.c:16-70`；
        /// ILSpy 交叉核对 `out_cpp2il_dev_ilspy/CardScript.cs:17233-17273`）。
        ///
        /// <para>**原版逐句**（`.c` 行号）：</para>
        /// <list type="number">
        /// <item>`:17` `if (activeEffects(+0x108) != null)` —— 有效果表才扫；</item>
        /// <item>`:18-56` **倒序**遍历（`iVar1 = Count-1 … 0`），逐条取 `effect`；
        ///   命中三重条件 ⇒ `:56 List.RemoveAt(iVar1)`：
        ///   `extrinsic(+0x30) == 0`（**非外来**）∧ `buffType(+0x38) == 1`（`addStats`）∧
        ///   `meleeAttackChange(+0x48) != 0`（**近战那一项有量**）。</item>
        /// <item>`:34-38` 循环结束后把 `newAttack` 写进 **`currentBaseMeleeAttack(+0x7c)`** 那 5 个 int
        ///   （`ObscuredInt` 的五个字），然后 `UpdateAttackText(this, 0)`。</item>
        /// </list>
        ///
        /// <para>⇒ **两条语义**：① 先把「非 extrinsic 的近战增益」整条摘掉（不是清零、不是减）；
        ///   ② 再把**基础值**（base）写成新值。⚠️ 它**不是** `setMeleeAttack(70)` 那条路 ——
        ///   那一档走的是「落一条 `IsSet` 修饰、投影里命中即 return」（`UnitState.ProjectMeleeAttack`），
        ///   两条别混（前者改 base、后者是覆盖修饰）。</para>
        ///
        /// <para>**我们的等价落点**：`UnitState.AttackBase`（= `currentBaseMeleeAttack`）+
        ///   `UnitState.StatMods` 里 `Attr=="attack" ∧ !Extrinsic ∧ BuffType==1 ∧ Value!=0` 的那批。
        ///   我们**一属性一条修饰**（原版一条 `CardEffect` 可同时带 melee/health 两项）⇒
        ///   「摘掉近战那半」= 摘掉这条 `attack` 修饰本身（原版是「整条 RemoveAt」或「整条保留」，
        ///   判定条件正是「近战项是否 != 0」⇒ 逐条等价）。</para>
        ///
        /// <para>⚠️ 摘完要重挑攻击型（原版 `UpdateAttackText`）；本仓的口 = `RecomputeCurrentAttackTypeOf`。</para>
        /// </summary>
        public static void ChangeBaseAttackFidelity(BattleContext ctx, UnitState target, int newAttack)
        {
            if (target == null) return;
            // ① 倒序摘「非 extrinsic 的 addStats 近战增益」（`.c:18-56`）
            var mods = target.StatMods;
            for (int i = mods.Count - 1; i >= 0; i--)
            {
                var m = mods[i];
                if (m == null) continue;
                if (m.Attr != "attack") continue;
                if (m.Extrinsic) continue;                       // `extrinsic(+0x30) != 0` ⇒ 留着
                if (m.IsSet || m.BuffType != 1) continue;         // 只摘 `buffType == 1 addStats`
                if (m.Value == 0) continue;                      // `meleeAttackChange(+0x48) != 0` 才摘
                target.RemoveStatMod(m);
            }
            // ② 写 base（`.c:34-38`）
            target.AttackBase = newAttack;
            RuleCore.RecomputeCurrentAttackTypeOf(ctx, target);   // 原版 `UpdateAttackText(this, 0)`
            ctx?.Log($"{target.Name} 的近战**基础值**被设为 {newAttack}"
                   + $"（当前近战 {target.Attack}；已先摘掉非外来近战增益 —— 原版 `ChangeBaseAttack`）");
        }

        // ==============================================================================================
        //  账 502 / 712 —— `CardScript.ChangeMaxHealth(int newHealth)` 的**逐句等价位**
        // ==============================================================================================

        /// <summary>
        /// **原版 `CardScript.ChangeMaxHealth(int newHealth)`**（`decomp_full/CardScript__ChangeMaxHealth.c:16-140`；
        /// ILSpy 交叉 `out_cpp2il_dev_ilspy/CardScript.cs:17297-17383`）。
        ///
        /// <para>**原版逐句**（`.c` 行号）：</para>
        /// <list type="number">
        /// <item>`:33-140` **倒序**遍历 `activeEffects`，逐条按两条支处理
        ///   （前提都是 `buffType(+0x38) == 1 addStats` ∧ `healthChange(+0x60) != 0`）：
        ///   · `extrinsic(+0x30) == 0`（非外来）：
        ///     – `meleeAttackChange(+0x48) == 0` ⇒ `:118 List.RemoveAt(i)`（整条摘掉，这条效果只剩生命，摘了正好）；
        ///     – 否则 ⇒ `:126` 把 `healthChange(+0x60) = 0`（保留近战那半，把生命那半清零）。
        ///   · `extrinsic != 0`（外来）⇒ `:135` `iVar8 += healthChange`（**累加进上限**，不摘）。</item>
        /// <item>`:43-52` 写上限：`maxHealthBase(+0xa4) = newHealth + Σ(extrinsic healthChange)`
        ///   （`ObscuredInt` 五个字 `+0xa4..+0xb4`）。</item>
        /// <item>`:56-70` 当前血：`health = min(旧 currentHealth(+0x68), 新上限)` —— 🔴 **只向下夹、不抬**。</item>
        /// <item>`:71` `UpdateHealthText(this, true, false)`。</item>
        /// </list>
        ///
        /// <para>**我们的等价落点**：`UnitState.MaxHealthBase`（= 原版 `currentMaxHealth(+0xa4)` 的 base 那一格）+
        ///   `StatMods`（`ProjectMaxHealthProcessed` 会把「非 extrinsic / extrinsic 的 addStats 生命项」各加回来）。
        ///   ⇒ ①「清零非外来生命项」= 摘掉那条 `health` 修饰（我们一属性一条，没有「同一条里还有近战」这回事）；
        ///   ② `MaxHealthBase = newHealth`（extrinsic 那一半由投影加，**别在这里重复加**，否则加两遍）；
        ///   ③ `Health = min(Health, MaxHealth)`。</para>
        ///
        /// <para>⚠️ 与 `DoSetHealth`（`buffType 9 setHealth`，`.c:794-820`）**不是一件事**：
        ///   那条写的是「当前血 = processed 上限」，**不动 base**；本条动 base 并夹当前血。</para>
        /// </summary>
        public static void ChangeMaxHealthFidelity(BattleContext ctx, UnitState target, int newHealth)
        {
            if (target == null) return;
            // ① 清零「非 extrinsic 的 addStats 生命项」（`.c:33-135`）
            var mods = target.StatMods;
            for (int i = mods.Count - 1; i >= 0; i--)
            {
                var m = mods[i];
                if (m == null) continue;
                if (m.Attr != "health") continue;
                if (m.Extrinsic) continue;                       // 外来那半要**留着**（它们进上限求和）
                if (m.IsSet || m.BuffType != 1) continue;         // 只清 `buffType == 1 addStats`
                if (m.Value == 0) continue;
                target.RemoveStatMod(m);
            }
            // ② 写 base（`.c:43-52`）—— ⚠️ **extrinsic 那一半由 `ProjectMaxHealthProcessed` 加**，这里不重复加
            target.MaxHealthBase = newHealth;
            // ③ 当前血只向下夹（`.c:56-70`）
            int before = target.Health;
            if (target.Health > target.MaxHealth) target.Health = target.MaxHealth;
            RuleCore.RecomputeCurrentAttackTypeOf(ctx, target);   // 原版 `UpdateHealthText`（同族刷新口）
            ctx?.Log($"{target.Name} 的生命**上限**被设为 {newHealth}"
                   + $"（上限 {target.MaxHealth}、当前血 {before} → {target.Health}；只向下夹 —— 原版 `ChangeMaxHealth`）");
        }

        // ==============================================================================================
        //  账 746 —— `CardScript.ForceChangeMaxHealth(int newHealth)` 的**逐句等价位**
        // ==============================================================================================

        /// <summary>
        /// **原版 `CardScript.ForceChangeMaxHealth(int newHealth)`**（`decomp_full/CardScript__ForceChangeMaxHealth.c:1-46`）。
        ///
        /// <para>**原版逐句**：**三格无条件写同一个值**（不扫效果表、不夹、不清任何增益）：
        /// ① `:10 maxHealth(+0x24c) = N`；② `:16-28 maxHealthBase(+0xa4) = N`（`ObscuredInt` 五字）；
        /// ③ `:30-42 currentHealth(+0x68) = N`；④ `:44 UpdateHealthText(this, true, false)`。</para>
        ///
        /// <para>**我们的等价**：`MaxHealthBase = N`（②）· `Health = N`（③）·（同族刷新 `RecomputeCurrentAttackTypeOf`）。
        ///   ⚠️ ①那一格（`+0x24c`）我们**没有独立字段** —— 本仓 `MaxHealth` 是
        ///   `ProjectMaxHealthProcessed` 的**投影**（= 原版 `currentMaxHealthProcessed`），
        ///   不是 `+0x24c`。原版 `+0x24c` 的读者本轮**没查到**（`grep MaxHealth` 只见这两处写点）
        ///   ⇒ **如实标着：这一格我们没有等价位，也不假装有**。
        ///   ⚠️ 本函数**不清**任何修饰（原版也不清）⇒ 挂着 +N 生命的单位，`MaxHealth`（投影）
        ///   仍会 = N + Σ增益，而 `Health` 恰好 = N（与原版形状一致）。</para>
        /// </summary>
        public static void ForceChangeMaxHealthFidelity(BattleContext ctx, UnitState target, int newHealth)
        {
            if (target == null) return;
            target.MaxHealthBase = newHealth;     // `.c:16-28`
            target.Health = newHealth;            // `.c:30-42`
            RuleCore.RecomputeCurrentAttackTypeOf(ctx, target);
            ctx?.Log($"{target.Name} 的生命**被强制设为** {newHealth}"
                   + $"（上限 {target.MaxHealth}、当前血 {target.Health} —— 原版 `ForceChangeMaxHealth` 三格同写）");
        }

        // ==============================================================================================
        //  账 698 —— `CardScript.CanPlayWithOathNowInPlay()` 的**逐句等价位**
        // ==============================================================================================

        /// <summary>
        /// **原版 `CardScript.CanPlayWithOathNowInPlay()`**（`decomp_full/CardScript__CanPlayWithOathNowInPlay.c:1-48`；
        /// ILSpy 交叉 `out_cpp2il_dev_ilspy/CardScript.cs:8241-8308`）。
        ///
        /// <para>**五道闸，顺序照原版**（全部 AND，任一不过 ⇒ `false`）：</para>
        /// <list type="number">
        /// <item>`:5-21` **回合闸**：`isPlayer(+0x40)` 的卡要 `IsPlayerTurn()` **且**
        ///   `allowPlayerActionsFlag(+0x244)`；AI 的卡要 `!IsPlayerTurn()`。
        ///   ⇒ 合起来 = 「**必须是这张卡那一侧的回合**」。</item>
        /// <item>`:22 HasCurrentTrait(0x82 = jam/130) == true` ⇒ `false`（被干扰的卡不能付誓约）。</item>
        /// <item>`:27-30 rawCardData.HasDefaultTrait(0x4fb = oath/1275) == false` ⇒ `false`。</item>
        /// <item>`:31-36 BoardAnalysis.GetManaLeft(owner) - GetDefaultTraitValue(oath) >= 0` ⇒ `true`；
        ///   🔴 **不减卡费**（这是与**手牌版** `CanPlayWithOathNow` 的唯一差别：那一版本要
        ///   `manaLeft >= cost + oath`，本版只在「卡已经在场」时问「还付得起誓约吗」）。</item>
        /// </list>
        ///
        /// <para>⚠️ **判据边界（如实）**：
        /// ① `allowPlayerActionsFlag(+0x244)` 我们**没有等价位** ⇒ 用「是不是这一侧的回合」代替
        ///    （与 `NeedsToChooseFromPool` 的门⑤ 同一口径，⛔ 别另立一套）；
        /// ② 「卡费」这一维原版本方法**根本不看** ⇒ 我们也不看。</para>
        ///
        /// <para>📊 **可达性**：原版本 build 里 `grep CanPlayWithOathNowInPlay` = **0 个调用点**
        ///   （`decomp_full` / `cpp2il` 两路都只命中定义本身）⇒ 休眠机制（铁律 11 仍要做）。</para>
        /// </summary>
        /// <param name="thisCard">这张卡在场上的**实例**（拿运行期关键词 / jam）；`null` ⇒ 退回卡的默认 trait。</param>
        public static bool CanPlayWithOathNowInPlay(BattleContext ctx, int owner, CardDef card, UnitState thisCard = null)
        {
            if (ctx == null || card == null || owner < 0 || owner > 1) return false;

            // ① 回合闸（`:5-21`）—— `allowPlayerActionsFlag` 无等价位，用「这一侧是不是行动方」代替
            if (ctx.Active != owner) return false;

            // ② jam（130）闸（`:22`）—— 运行期 trait 优先，没有实例才退回卡的默认 trait
            bool jammed = (thisCard != null && thisCard.Has(KeywordTable.Jam))
                       || (thisCard == null && card.HasDefaultTrait(KeywordTable.Jam, out _));
            if (jammed) return false;

            // ③ 必须带誓约默认 trait（`:27-30`）
            if (!card.HasDefaultTrait(KeywordTable.Oath, out _)) return false;

            // ④ 法力 ≥ 誓约值（`:31-36`）—— 🔴 **不减卡费**
            int left = ctx.Players[owner].Energy;
            int need = card.GetDefaultTraitValue(KeywordTable.Oath, out _);
            return left >= need;
        }

        // ==============================================================================================
        //  账 1122 —— `MinionManager.GetNextSlotToCard(CardScript card)` 的**逐句等价位**
        // ==============================================================================================

        /// <summary>
        /// **原版 `MinionManager.GetNextSlotToCard(CardScript card)`**（`decomp_full/MinionManager__GetNextSlotToCard.c:1-110`；
        /// 唯一调用点 = `AbilityLogic__PlayAbility.c:755-780` 的召唤落点四档）。
        ///
        /// <para>**原版逐句**：</para>
        /// <list type="number">
        /// <item>`:20 iVar2 = GetNextSlotWithoutDisplacing()` —— **先算默认落点**（人少那侧最外一格）；
        ///   ⇒ 我们 = <see cref="BoardSlots.NextWithoutDisplacing"/>。</item>
        /// <item>`:21-24 card == null` ∨ `card.cardType(+0x24?) == 10 (Hero)` ⇒ **直接返回默认落点**。</item>
        /// <item>`:25-47` **右表（`+0x68`）含该卡** ∧ `slotsPerSide(+0x78 的 Count) > 右侧人数(+0x28/+0x38)`
        ///   ⇒ 返回 `IndexOf(右表, card) + 2`。语义 = 「落到**这张卡旁边**（更外侧一格）」。</item>
        /// <item>`:48-49/:95-99` 否则 **左表（`+0x60`）含该卡** ∧ `AvailableLeftSlot()` ⇒ 返回
        ///   `-2 - IndexOf(左表, card)`（对称支）。</item>
        /// <item>都不中 ⇒ `:50-93` 拼一句 `LogWarning`（含卡名与两侧人数）+ 返回**默认落点**
        ///   （`iVar2`，即第 ① 步那个）。</item>
        /// </list>
        ///
        /// <para>**格号换算**：原版右表下标 `i` ⇒ 原版有符号格 `i+1` ⇒ 我们 `BoardSlots.SlotOf(Right, i)`
        ///   （= `WarlordSlot + (i+1)`）；原版返回的 `i+2` 就是「同侧再外一格」= 我们的 `SlotOf(side, i+1)`。
        ///   ⚠️ 我们的棋盘是**连续无洞数组**、落点由 `BoardSlots.Insert(ps, u, requestedSlot)` 的
        ///   「插推」实现（原版是 `List.Insert`）⇒ 本函数**只回答格号**，真正落位仍走 `Insert`。</para>
        ///
        /// <para>📊 **可达性**（🔴 2026-10-29 `B95_WRE` 现读订正 —— 原文「`0 self` / `1 cardTarget` 仍是 0 处」「本函数今天仍没有调用点」两句**双双作废**）：
        ///   ⚠️ **错因**：原来读 `cards_truth.json`（**稀疏导出**，`defaults.omitted` 明写 `0 / 0.0 / "" / false / [] / null` 一律省略）⇒ 按「有没有这个键」数 `0` 值**必然**数出 0；要读 0 值只能读原始 SO（`cards_raw.json` 的 `full_raw`）。
        ///   现读真值（`报告/B94_Z3.md` §3·3 的普查；`chosenAbility ∈ {70,71,72,75,77}` 的召唤行共 **109 行**）：
        ///   `createAtPos = 0 (self)` **73 行**（61 战术 + **12 单位**）· `= 14` 35 行 · `= 3` 1 行（`Rites of Possession`）。
        ///   ⇒ 那 **12 行属场上单位卡**（`TL6` / `SAU13` / `TL14` / `TAU67` / `GOF61` / `UM17` / `GOF94` / `SAU34` / `TL24` / `SAU1` / `SAU28` …），
        ///   施放者就在场上 ⇒ 原版落到「**贴着它**」，**与默认落点真的不同**（不是「接上也不改落点」）。
        ///   ✅ **接线已完成**：消费者 = `EffectResolver.cs:4001` 的 `DeployFree(..., requestSlot: SummonRequestSlotFor(ctx, owner, op))`；
        ///   取数口 = 同文件类尾 `SummonRequestSlotFor`（走 `CardDef.AbilitySummonAt(op.AbilityIndex, op.LogicIndex)`）。
        /// </summary>
        /// <param name="anchor">「贴着谁落位」（原版 `card`）。`null` / 督军 ⇒ 默认落点。</param>
        /// <returns>请求格号（可能已被占 —— 交给 `BoardSlots.Insert` 插推）；`-1` = 两侧都满。</returns>
        public static int NextSlotToCard(BattleContext ctx, int owner, UnitState anchor)
        {
            if (ctx == null || owner < 0 || owner > 1) return -1;
            var ps = ctx.Players[owner];
            if (ps == null) return -1;

            // ① 默认落点（原版 `GetNextSlotWithoutDisplacing`）
            int dflt = BoardSlots.NextWithoutDisplacing(ps);

            // ② 空卡 / 督军（cardType == 10 Hero）⇒ 直接默认落点
            if (anchor == null || anchor.IsWarlord) return dflt;

            // ③④ 在**这一侧**的两条列表里按引用找它（原版右表 +0x68 / 左表 +0x60）
            int right = -1, left = -1;
            for (int i = 0; i < BoardSpec.SlotsPerSide; i++)
            {
                if (right < 0 && ReferenceEquals(ps.Board[BoardSlots.SlotOf(BoardSlots.Right, i)], anchor)) right = i;
                if (left < 0 && ReferenceEquals(ps.Board[BoardSlots.SlotOf(BoardSlots.Left, i)], anchor)) left = i;
            }
            // 右表含该卡 ∧ 右侧还有位 ⇒ IndexOf + 2（= 同侧再外一格）
            if (right >= 0 && BoardSlots.RoomOnSide(ps, BoardSlots.Right) > 0)
                return BoardSlots.SlotOf(BoardSlots.Right, right + 1);
            // 左表含该卡 ∧ 左侧还有位 ⇒ -2 - IndexOf（对称）
            if (left >= 0 && BoardSlots.RoomOnSide(ps, BoardSlots.Left) > 0)
                return BoardSlots.SlotOf(BoardSlots.Left, left + 1);

            // ⑤ 都不中 ⇒ 出声 + 默认落点（原版 `LogWarning` 里带卡名与两侧人数）
            ctx.Log($"（召唤落点：`GetNextSlotToCard` 找不到「{anchor.Name}」的可贴位"
                  + $"（右侧 {BoardSlots.CountOnSide(ps, BoardSlots.Right)} 人 / 左侧 {BoardSlots.CountOnSide(ps, BoardSlots.Left)} 人）"
                  + " ⇒ 退回默认落点 —— 原版这里也打一条 LogWarning）");
            return dflt;
        }

        // ==============================================================================================
        //  账 77 —— `ChooseValueOptions` 的两种新「数值来源」（`BaseRefCardMAttack 101` / `OwnWarlordAttack 140`）
        // ==============================================================================================

        /// <summary>
        /// **`ChooseValueOptions.BaseRefCardMAttack = 101` 的取值口** —— 「参照卡的基础近战」。
        ///
        /// <para>🔴 **2026-10-28（B91 · 执行写手 `A1`）：改成【照抄原版】—— 这一格在我们池内恒 `null`。**
        /// 原版读点 = `decomp_full/AbilityLogic__GetValueFromCriteria.c:157-177`（`param_7 == 0x65` = 101）：
        /// `if (actingCard.referencedBaseCard != null) return 它.rawCard.meleeAttack(+0xa4);` **否则 `return param_3`（= minDamage）**。</para>
        ///
        /// <para>**`referencedBaseCard`（`CardScript +0x280`）为什么恒 `null`**（判据 → `判据/B91_P3_战斗数据列缺口.md` §二·5）：</para>
        /// <list type="bullet">
        /// <item>它是 `[CompilerGenerated]` 自动属性**背书字段** ⇒ prefab / 场景里**不存值**，只有运行时写；</item>
        /// <item>**唯一写点** = `AbilityLogic__PlayAbility.c:861-870`，门 = `chosenAbility == 0x47 (71 summonUnitAndSetRef)`；</item>
        /// <item>`chosenAbility 71` 池内 **0 行**（1,351 张全无）；`RawCardScript.referenceCard(+0x110)` **1,351/1,351 全空**；</item>
        /// <item>`valueOptions == 101` 池内 **1 行** = `UM118 Indomitus Crusade`（它的召唤走 `70 summonUnit`，**不是 71**）。</item>
        /// </list>
        ///
        /// <para>⇒ 原版这一格**恒 `null` ⇒ 恒返回 `minDamage`**（`UM118` 那张的誓约伤害因此是 **0** ——
        /// 原版自己就这样，与 `G14` 那个「漏一个 `!`」同族：**照抄，⛔ 别修**）。</para>
        ///
        /// <para>⚠️ **与 `100 RefCardAttack` 的分工**：那一档取**参照卡**的当前近战（`+0x7c` = `currentBaseMeleeAttack`，
        /// 见 <see cref="ApplyRefCardValueOption"/>）；本档取的是**另一个字段**（`actingCard.referencedBaseCard`）。
        /// ⛔ **别拿 `ctx.LastCreated` / `ctx.LastTarget` 顶替** —— 那是改前的近似（旧注释自己写着「机制不是原版那条」），
        /// 已按调度台 2026-10-28 裁定「照抄原版」删掉：它会让 `UM118` 打出原版没有的伤害。</para>
        ///
        /// <para>📊 **可达性**：`cards_truth.json` 现读 `valueOptions == 101` = **1 行**（`UM118`）⇒ 这一档可达，
        /// 但语义是「恒 = 这条 op 自身的 `minDamage`」，即**与原版逐字相同的空效果**（⛔ 不是「我们没做」）。</para>
        /// </summary>
        /// <param name="minDamage">这条 op 自己的 `damageCriteria.minDamage`（原版 `param_3`）。
        /// 唯一调用点 = `EffectResolver.TryApplyAmountRef` 的 `refcardbaseattack` 那一条，传 `op.Amount`。</param>
        public static int RefCardBaseAttack(BattleContext ctx, int minDamage = 0)
        {
            // 原版 `.c:157-177`：`referencedBaseCard == null ⇒ return param_3`；我们这一版该字段恒 null（见 doc）
            // ⇒ **无条件返回 `minDamage`**。⛔ 别再加回 `ctx.LastCreated` / `ctx.LastTarget` 的近似 ——
            //   那会让 `UM118 Indomitus Crusade` 的誓约打出原版没有的伤害（铁律 11 · 调度台 2026-10-28 裁定）。
            ctx?.Log($"`ChooseValueOptions.BaseRefCardMAttack(101)`：原版读 `actingCard.referencedBaseCard`，"
                   + "而它在现池**恒 null**（唯一生产者 `chosenAbility 71` 池内 0 行）⇒ 恒返回 `minDamage` "
                   + $"= {minDamage}（UM118 的誓约伤害 = 0，原版自己就这样，⛔ 别修）");
            return minDamage;
        }
        // ---- 下面几行是「为什么不能换个更好看的近似」的留痕（⛔ 别删，会有人再问一遍）----
        //  · `referencedBaseCard` 是**运行时**字段、不是卡表列 ⇒ ⛔ 别指望给卡表加一列补上（`B91_P3` §3·4）。
        //  · `referenceCard(+0x110)`（PPtr）与 `referencedCards(+0x278)`（List）**都不是这一格** ——
        //    `+0x110` 是 SO 侧的引用、`+0x278` 是 `380 setReferenceCard` 写的另一本账（`PlayAbility.c:2128-2160`）。
        //  · 与 `140 OwnWarlordAttack` 无关：那一档读**自己督军**的当前近战（`GetValueFromCriteria.c` 另一支）。
        //  · 这条改动**会动 `play check` 指纹**（`UM118` 的誓约从「按 LastCreated 取」变成 0）
        //    ⇒ 由调度台在「无 `.cs` 写手」窗口**重钉基线**（同 `stealMinion` 永久化那一条）。
        //  · 判据与 A/B 两支全文 → `判据/B91_P3_战斗数据列缺口.md` §二·5（**本批取 A：照抄原版**）。
        //  · `101` 这一档全池**唯一一张**是 `UM118`，而它的召唤走 `70 summonUnit` ⇒ 原版那个写点（71）**永远不被它触发**。
        //  · 改前的实现（`ctx.LastCreated` 末份 → 退 `ctx.LastTarget` 的 `AttackBase`）**已按铁律 5 删除**，不是注释掉。
        //  · 这条**不需要**给卡表加列（`B91_P3` §3·4 明写：它不是卡表列，加了就是死列）。

        // ==============================================================================================
        //  特效动画 520 —— `EntityScript.CreateMarkCardEffect(MarkOfChaosType)`（mark 分支那一半）
        // ==============================================================================================

        /// <summary>
        /// **原版 `EntityScript.CreateMarkCardEffect(MarkOfChaosType markOfChaosType)`** 的等价**数据形状**
        /// （`decomp_full/EntityScript__CreateMarkCardEffect.c:1-21`；细则 §1.3 末段）。
        ///
        /// <para>**原版逐句**：`new CardEffect{ buffType = markOfChaos(8), markOfChaosType = 参入的 god }`，
        /// **其余全默认** —— `trait = none(0)` · `traitValue = 0` · `effectText = 空`；
        /// 调用点（`GetCardEffectsToShow` 的 mark 分支）再把**原记录**的 `enchantment(+0x88)` 拷进来。</para>
        ///
        /// <para>🔴 **为什么必须是 `BuffType = 8`**：下游 `CardEffectItem.LoadEffect` 走
        /// 「`buffType == 8 || 0x11(17)` ⇒ mark 分支」那一判 ⇒ 换成别的 buffType 这行就**印不出来**
        /// （静默、不报错）。</para>
        ///
        /// <para>⚠️ **消费面**：`GetCardEffectsToShow`（账 特效 521）今天仍未实现（S1 整条投影要
        /// `UnitState` 的 `Seq` 等前置，见报告）⇒ 本行是**给它准备的数据形状**，
        /// **今天没有调用点**（这一点与 `TraitEffects` 的生产者同族：先有形、后接线）。</para>
        /// </summary>
        public static ShownMarkRow CreateMarkCardEffect(int markOfChaosType, int traitValue = 0, string source = null)
        {
            return new ShownMarkRow
            {
                BuffType = 8,                        // `BuffType.markOfChaos` —— LoadEffect 的分支键
                Trait = null,                        // 原版 `trait = none(0)`
                TraitValue = traitValue,             // 原版这里恒 0，聚合时由调用点累加
                MarkOfChaosType = markOfChaosType,    // 10 Khorne / 20 Nurgle / 30 Slaanesh / 40 Tzeentch
                Source = source,
                Body = "",                           // 原版 `effectText` 空 ⇒ 右列走「谁给的」兜底
            };
        }

        /// <summary>
        /// mark 聚合行的**数据形状**（<see cref="CreateMarkCardEffect"/> 的返回类型）。
        /// 字段名对齐原版 `CardEffect`：`buffType(+0x38)` · `trait(+0x3c)` · `traitValue(+0x40)` ·
        /// `markOfChaosType(+0x78)` · `enchantment(+0x88)`（这里用 `Source` 表示「哪张契约卡给的」，
        /// 与 `TraitEffectRecord.Source` 同一口径 —— 🔶 自己设计：原版没有这一列）。
        /// </summary>
        public struct ShownMarkRow
        {
            public int BuffType;
            public string Trait;
            public int TraitValue;
            public int MarkOfChaosType;
            public string Source;
            public string Body;
        }

        // ==============================================================================================
        //  账 L74 —— `AbilityLogic.GetRefCard()` + `ChooseValueOptions` 的 100 / 110 / 120 三档
        //           （2026-10-28 · B91 · 执行写手 `W2`）
        // ==============================================================================================

        /// <summary>
        /// **原版 `AbilityLogic.GetRefCard()`**（`decomp_full/AbilityLogic__GetRefCard.c:1-38` 全文）的等价位 ——
        /// 「**上一条 ability 的目标表 [0]**」。
        ///
        /// <para>**原版逐句**：读静态表（`&DAT_1842c3a28 + 0xb8` 那份 `List`，**就是**
        /// `AbilityLogic__SetPreviousAbilityTargets.c:17-27` 写的**同一份**静态表）；表为空 ⇒ 返回 `null`；
        /// 否则 `List[0]`（`.c:23` 的 `Count < 1` 早退 + `.c:30-31` 的 `get_Item(0)`）。</para>
        ///
        /// <para>🔑 **我们这一格 = `ctx.PreviousAbilityTargets`（它的 doc 已写明它就是原版那张静态表的
        /// 发布视图）** ⇒ 直接取 `[0]`。⚠️ **注意时机**：原版这张表**只在「每条效果出口」被整表覆盖**
        /// （`SetPreviousAbilityTargets` 的 48 处调用点），**没有「每次都清空」这一步** ⇒
        /// 一条新 ability 的**第一条 op 取值时**读到的仍是**上一条 ability** 的那批目标 —— 这正是
        /// `Indomitus Crusade` 的 Oath 句能拿到「前半句刚部署出来的那个单位」的原因。
        /// 我们的 `NotePreviousAbilityTargets` 同样是「结算完一条 op 才覆盖」，起手只清
        /// `AbilityTargetsSaved`（那张并集表），**不动** `PreviousAbilityTargets` ⇒ 时机与原版一致。</para>
        ///
        /// <para>⚠️ 本表**只装场上单位**（`UnitState`）；原版还可能装手牌 / 牌库的卡（`AddDrawCard` 塞的）——
        /// 那半边由 `CountEval` 的 `DrawnThisResolve` 分支单独建模（`PreviousAbilityTargets` 的 doc §②）。
        /// 取不到返回 `null`，调用方**必须出声**、⛔ 不许当 0（0 会让伤害「看起来打出去了但没伤害」）。</para>
        /// </summary>
        public static UnitState RefCardOf(BattleContext ctx)
        {
            if (ctx == null) return null;
            var prev = ctx.PreviousAbilityTargets;
            if (prev == null || prev.Count == 0) return null;
            return prev[0];
        }

        /// <summary>参照卡的**当前费用**（原版 `GetValueFromCriteria` 的 `120 RefCardCost` 走
        /// `EntityScript.get_CurrentCost(refCard)`，`AbilityLogic__GetValueFromCriteria.c:195-207`）。
        /// 我们 = `RuleCore.CostOf(ctx, owner, refCard.Instance)`（按份的费用修正只它看得见）。
        /// 取不到（没有实例 / 没有卡）⇒ `-1`（调用方出声、⛔ 不当 0）。</summary>
        public static int RefCardCostOf(BattleContext ctx, UnitState refCard)
        {
            if (ctx == null || refCard == null || refCard.Card == null) return -1;
            int owner = -1;
            for (int p = 0; p < 2; p++)
            {
                var b = ctx.Players[p].Board;
                for (int s = 0; s < BoardSpec.Size; s++)
                    if (ReferenceEquals(b[s], refCard)) { owner = p; break; }
                if (owner >= 0) break;
            }
            if (owner < 0) return -1;
            return refCard.Instance != null
                ? RuleCore.CostOf(ctx, owner, refCard.Instance)
                : RuleCore.CostOf(ctx, owner, refCard.Card);
        }

        /// <summary>
        /// **`ChooseValueOptions.RefCardAttack(100)` / `RefCardHealth(110)` / `RefCardCost(120)` 三档的求值口**
        /// （2026-10-28 · B91 · 账 L74）。判据 = `decomp_full/AbilityLogic__GetValueFromCriteria.c` 逐分支：
        /// <list type="bullet">
        /// <item>`:133-156` **100**：取 `refCard(+0x7c..+0x8c)` 那条 `ObscuredInt` ——
        ///   字段序（`dump.cs:22009`）= **`currentBaseMeleeAttack`** ⇒ 我们 `UnitState.AttackBase`。</item>
        /// <item>`:171-190` **110**：取 `refCard(+0x68..+0x78)` = **`currentHealth`** ⇒ 我们 `UnitState.Health`
        ///   （⚠️ **不是** `currentMaxHealth(+0xa4)` —— 那一个是 `101` 用的另一格，别混）。</item>
        /// <item>`:195-207` **120**：`get_CurrentCost(refCard)` ⇒ <see cref="RefCardCostOf"/>。</item>
        /// </list>
        ///
        /// <para>🔴 **原版这三档都【直接返回那个值】**（不走 `min/max` 区间、不乘 `multiplier`）⇒
        /// 我们把它折成**定值**（`Amount = AmountMax = n`），⛔ 别留成区间（那会让 `DoDeal` 去掷骰）。</para>
        ///
        /// <para>📊 **可达性（如实标休眠）**：现池 `cards_truth.json` 全量 `valueOptions` 分布 =
        /// `0×3476 · 90×87 · 8×38 · 80×3 · 204×3 · 210×2 · 203/202/140/101/40 各 1`
        /// ⇒ **100 / 110 / 120 全池 0 行**（2026-10-28 现数，⛔ 别抄死）。它和「101 / 140 已接」同族：
        /// **先有形、等数据**（铁律 11：休眠机制也做）。</para>
        ///
        /// <para>⚠️ **只处理 `deal`**（其余动词没有 `damageCriteria` 这一层）；`AbilityMap == "attached"`
        /// 那一支**不碰**（与 `StampRollPerTarget` 同一条边界：主动能力是另一套 `(ab,lg)` 下标空间）。</para>
        /// </summary>
        public static EffectOp ApplyRefCardValueOption(BattleContext ctx, EffectOp op, CardDef card)
        {
            if (ctx == null || op == null || card == null) return op;
            if (!string.Equals(op.Verb, "deal", System.StringComparison.Ordinal)) return op;
            if (op.AbilityIndex < 0 || op.LogicIndex < 0) return op;
            if (string.Equals(op.AbilityMap, "attached", System.StringComparison.Ordinal)) return op;
            var spec = card.AbilityDamageAt(op.AbilityIndex, op.LogicIndex);
            if (spec == null) return op;
            int vo = spec.ValueOptions;
            if (vo != 100 && vo != 110 && vo != 120) return op;

            var rc = RefCardOf(ctx);
            if (rc == null)
            {
                ctx.Log($"「{op.Source}」的数值要取**参照卡**（`ChooseValueOptions` {vo} 档），"
                      + "但「上一条 ability 的目标表」是空的 ⇒ **这一条没生效**"
                      + "（原版 `AbilityLogic__GetRefCard.c:23-25` 返回 null）");
                return op;
            }
            int n = vo == 100 ? rc.AttackBase            // `currentBaseMeleeAttack`（+0x7c）
                  : vo == 110 ? rc.Health                // `currentHealth`（+0x68）
                  : RefCardCostOf(ctx, rc);              // `get_CurrentCost`
            if (n < 0)
            {
                ctx.Log($"「{op.Source}」要取参照卡「{rc.Name}」的 "
                      + (vo == 100 ? "近战" : vo == 110 ? "当前血" : "费用")
                      + " —— **取不到**（这一条没生效）");
                return op;
            }
            var c = op.Clone();
            c.Amount = n;
            c.AmountMax = n;      // 原版这三档直接返回值：不是区间、不掷骰
            ctx.Log($"「{op.Source}」的数值 = 参照卡「{rc.Name}」的 "
                  + (vo == 100 ? $"近战 {n}" : vo == 110 ? $"当前血 {n}" : $"费用 {n}")
                  + "（原版 `ChooseValueOptions` 的 " + vo + " 档）");
            return c;
        }

        // ==============================================================================================
        //  账 L546 —— `CardScript.CheckStun()` 的等价位（2026-10-28 · B91 · `W2`）
        // ==============================================================================================

        /// <summary>
        /// **原版 `CardScript.CheckStun()`** 的**转发口** —— 唯一实现 = <see cref="RuleCore.CheckStun(BattleContext, UnitState)"/>
        /// （`RuleCore.cs:16581`，`W1` 2026-10-28 建的；判据 `decomp_full/CardScript__CheckStun.c:1-25`）。
        ///
        /// <para>🔴 **为什么只剩转发**（2026-10-28 · B91 · `W2`）：本文件原来有一份等价实现，`W1` 同时在 `RuleCore`
        /// 建了同名口 ⇒ 同一条规则两份实现（铁律「两处写同一条规则 = 迟早不一致」）⇒ 本方法改成**纯转发**，
        /// 保留三参签名只是为了**不动既有两个调用点**（`DoReanimate` 的两条支，`EffectResolver.cs:15578` / `:15638`）。
        /// `by` 参数**不再使用**（日志由 `RuleCore.CheckStun` 打，⛔ 别再在这里补一份，会双响）。</para>
        ///
        /// <para>📌 **原版调用点**：`BattleManager__ResolveTransformFromRemnant` 的完成回调
        /// `…DisplayClass555_0___ResolveTransformFromRemnant_b__0.c:20/22`（残骸翻回）· `ResolveTransformUnit.c:1629`（在场变身）。
        /// ⚠️ 第二条（在场变身 = `transformUnit`）**本仓还没有机制**（`EffectDispatch` 50 个键里无 transform；见账 L172 / L548）
        /// ⇒ 那一格**没有真实落点可接**，如实记在报告里，⛔ 没在 `DoBecome`（手牌战略卡变身，不是在场单位）里硬塞。</para>
        /// </summary>
        public static bool CheckStun(BattleContext ctx, UnitState u, string by = null)
        {
            if (u == null) return false;
            return RuleCore.CheckStun(ctx, u);      // ← 唯一实现（`RuleCore.cs:16581`，W1 建的口）
        }

        // ==============================================================================================
        //  账 L875(b) —— `untilNextAttack` 到期档的【入队版】（2026-10-28 · B91 · `W2`）
        // ==============================================================================================

        /// <summary>
        /// **`untilNextAttack` 到期扫描的【入队版】** —— 原版那两处扫点走的是
        /// `CardScript__SendRemoveEffect(card, 效果, 1)`（= 造一条 `battleActionType 0x10 RemoveEffect` 的动作
        /// → `BattleManager__AddAutoActionToQueue(…, priority=1, …)`），**不是当场摘**。
        ///
        /// <para>**判据**（逐句现读）：
        /// <list type="bullet">
        /// <item>`CardScript__UsedActiveAbility.c:66-84`（主动能力收尾那一趟；`attacksThisTurn(+0x48) += 1` 之后）</item>
        /// <item>`CardScript__ResolveUnitAttacked.c:79-94`（只在 `actingCard == this` 那一支 —— 这一记是它自己打出去的）</item>
        /// </list>
        /// 两处形状**完全相同**：扫 `activeEffects(+0x108)`，`if (*(char*)(效果 + 0x36) != '\0')`
        /// ⇒ `SendRemoveEffect(card, 效果, /*priority*/ 1)`。⇒ **每条到期记录一条动作**、priority = **1**。
        /// </para>
        ///
        /// <para>**为什么要有这个口**（而不是让调用方直接 `u.ExpireUntilNextAttack()`）：
        /// 原版摘除发生在**下一拍动作**里（`ResolveAction` case `0x10`），中间还留着这一条可被
        /// `CancelEffectsFromDisappearingUnit` 撤掉的窗口；同步摘会**少这一个窗口**。
        /// 我们的队列入口 = `EnqueueAbility`（`Core/ActionPump.cs`），lane **`ActionPriority.Instant`（= 1）**
        /// 与原版 priority 1 同值；`actor: u`（原版 `BattleAction.casterCard` 就是这张卡自己）。
        /// </para>
        ///
        /// <para>⚠️ **两处如实标的残余**（⛔ 别当已完全等价）：
        /// ① `QueuedAction.ActionType` 仍是 **0**（原版 0x10）—— 回填要 `EnqueueAbility` 加形参（账 L490，
        ///    白名单外）⇒ 今天被撤队列的豁免判据（`attack 1` / `recallToHand 0x1a` / `destroyUnit 0x2d`）
        ///    对这条动作**一律不命中**（它本来也不该命中，因为原版 0x10 三条都不属于）。
        /// ② `permanentBuff(+0x74) ∧ untilNextAttack(+0x36)` 那一格：原版 `RemoveEffect` 第一句就 `return`；
        ///    而到期扫描吃的是 `TraitEffectRecord`（**没有 `Permanent` 列**）⇒ 表达不了。今天等价：
        ///    `TimingBitUntilNextAttack` 无生产者（解析层只有 空/turn/nextturn 三档）⇒ 恒不触发。
        /// </para>
        ///
        /// <para>📊 **可达性**：`cards_truth.json` 只有 1 处 `untilNextAttack`（`EffectBjornShrine`，不在当前池）
        /// ⇒ 本函数今天恒入队 0 条（休眠件，铁律 11 也做）。**调用点归 `RuleCore`（W1）** ——
        /// 那两个原版位置。⛔ 本函数不自带触发接线。</para>
        /// </summary>
        /// <returns>入队了几条（= 有几条到期记录）。</returns>
        public static int EnqueueExpireUntilNextAttack(BattleContext ctx, UnitState u, string by = null)
        {
            if (ctx == null || u == null) return 0;
            // 先拷一份：入队期间不动记录表（出队时才 `RemoveTraitEffect`）
            var due = new List<UnitState.TraitEffectRecord>();
            for (int i = 0; i < u.TraitEffects.Count; i++)
            {
                var r = u.TraitEffects[i];
                if (r != null && UnitState.IsUntilNextAttack(r.TimingBits)) due.Add(r);
            }
            int n = 0;
            for (int i = 0; i < due.Count; i++)
            {
                var rec = due[i];      // 每遍一个新局部 ⇒ 闭包各自捕获一条（⛔ 别循环变量共享）
                bool ok = EnqueueAbility(ctx, ActionPriority.Instant, "removeEffect(untilNextAttack):" + u.Name,
                                         () =>
                                         {
                                             // 出队那一刻记录可能已经被别的路摘掉 ⇒ `RemoveTraitEffect` 自己返回 false，
                                             // 我们**出声**（原版那条动作也会撞上同一种空过；⛔ 不静默）。
                                             if (u.RemoveTraitEffect(rec))
                                                 ctx.Log($"{by ?? u.Name}：「{u.Name}」的 `untilNextAttack` 档到期 ⇒ 摘掉一条"
                                                       + "（原版 `SendRemoveEffect` → `ResolveAction` 0x10）");
                                             else
                                                 ctx.Log($"{by ?? u.Name}：「{u.Name}」的 `untilNextAttack` 那条记录"
                                                       + "**出队时已经不在**（被前面的动作摘掉了）⇒ 这条空过");
                                         },
                                         actor: u, actionType: (int)BattleActionType.removeEffect);   // 账 L490 · ActionType 回填：原版 `SendRemoveEffect` 组的是 `action 0x10 = 16 removeEffect`（判据 = `CardScript__SendRemoveEffect.c:1-87`，账 ROW 28；B92_B4c 2026-10-28）
                if (ok) n++;
            }
            if (due.Count > 0)
                ctx.Log($"{by ?? u.Name}：「{u.Name}」有 {due.Count} 条 `untilNextAttack` 到期 ⇒ 入队 {n} 条摘除动作"
                      + "（原版两处扫点都用 priority 1；见 `EnqueueExpireUntilNextAttack` 的 doc）");
            return n;
        }

        // ==============================================================================================
        //  账 254 —— `GameStaticData.IsOneTurnEffect(CardEffect)` 的等价位（2026-10-28 · B91 · `W2`）
        // ==============================================================================================

        /// <summary>
        /// **原版 `GameStaticData.IsOneTurnEffect(CardEffect effect)`**
        /// （`decomp_full/GameStaticData__IsOneTurnEffect.c:1-14` 全文；ILSpy 交叉 `GameStaticData.cs:1835-1847`）的等价位。
        ///
        /// <para>**原版逐句**：`effect == null` ⇒ 抛 NRE（不可达支）；否则
        /// `untilEndOfTurn(+0x32) || untilPlayerTurnStart(+0x33) || untilEnemyTurnStart(+0x34) || untilFollowingPlayerTurnStart(+0x35)`
        /// —— `.c:6-12` 写成「前三个都假 ⇒ 返回 `+0x35` 本身；否则 1」，
        /// 与「四个位或起来」逐值等价（`+0x35` 是 bool）。</para>
        ///
        /// <para>**我们这一格** = `TraitEffectRecord.TimingBits` 的低 4 位（位序已对齐，见
        /// <see cref="UnitState.TraitEffectRecord.TimingBits"/> 的 doc：bit0..3 = 上面那四个 `until*`；
        /// bit4 = `untilNextAttack`，**不在本判据里**；bit5 = `isPlayer`）⇒ `(bits &amp; 0x0F) != 0`。
        /// ⚠️ **位序对齐不算做完了**：`TimingBitsOfDuration` 原来只产 bit0/bit1，那样 `&amp; 0x0F` 与 `&amp; 0x03` 同值
        /// ⇒ 本批同时补齐 bit2/bit3 两档（见那个方法的注释）。</para>
        ///
        /// <para>📊 **可达性（如实标）**：原版这一个方法在**三源里都是 0 个调用点**
        /// （`decomp_full` 全树 / `out_cpp2il_dev_ilspy` / `out_il2csharp` 只命中定义本身），
        /// 且 `private static` ⇒ 要么是死代码、要么其调用者被内联/剥体（同族先例 `ManaBuff.DeepCopy`）。
        /// 我们这边**今天也没有消费者**（本函数是给它准备的取值口）⇒ 休眠件（铁律 11 也做）。</para>
        /// </summary>
        public static bool IsOneTurnEffect(int timingBits)
        {
            return (timingBits & 0x0F) != 0;
        }

        // ==============================================================================================
        //  账 B91 · P1 §四 / §五 / §六 —— `discardFromHand(160)` / `discardWholeHand(165)` /
        //  `disappearUnit(240)` / `stealMinionSelf(185)`（2026-10-28 · 执行写手 `A1`）
        //
        //  🔴 **可达性（现读，⛔ 别当「我们漏接了」）—— 这四条今天一条都到不了**：
        //    · `cards_truth.json` 1,351 张的 `chosenAbility`：`160` × 1（BL37 的**重复件** `f3df0cfc…`，
        //      池内那张记录是 30）· `165` × 0 · `240` × 0 · `185` × 1（GSC49 同名 `Telephatic Domination`
        //      的**另一份 SO** `1a647ea3…`，不在 1,129 池内）；
        //    · 解析层 `EffectText.cs` 里这些动词 **0 个生产者**（池内 `desc` 的
        //      `\b(discard|discards|discarded)\b` 也是 0 命中）⇒ **不编英文句式**（铁律 2）；
        //    · `stealMinionSelf` 复用**已有**的 `takecontrol` handler（`DoTakeControl`），本文件只补
        //      「目标 = 施放者自己」那一档（`ResolveTakeControlTargets`）—— ⛔ 不写第二份搬运逻辑。
        //  ⇒ 三个新动词都是**休眠件**（铁律 11：休眠机制也做，只排后面）。做完**出声**，绝不静默。
        //
        //  ⚠️ 为什么新成员全放这个 partial 文件：`Core/EffectResolver.cs` 被 `账/*.tsv` 按 `文件:行号`
        //    钉了 **357 处**落点（其中 342 处在 1,161 行之后）⇒ 往里插成员会让下游整体漂移（铁律 18·b / N9 先例）。
        // ==============================================================================================

        // ----------------------------------------------------------------------------------------------
        //  §五 · `discardFromHand(160)` / `discardWholeHand(165)`
        // ----------------------------------------------------------------------------------------------

        /// <summary>
        /// **`discardfromhand` 动词** —— 原版 `chosenAbility 160 discardFromHand`。
        ///
        /// <para>**入口**（`decomp_full/AbilityLogic__PlayAbility.c:588-603`）：遍历 `possibleTargets` ⇒
        /// `BattleManager__AddDiscardFromHand(bm, discardedCard = target, prio(+0x44), block(+0x48))`
        /// —— 动作槽 `discardFromHand(28)`（`AddDiscardFromHand.c:23` `ctor(0x1c)`；`checkIfCasterInPlay` 固定 `false`、
        /// `actingCard` 固定 `null`）。结算 = `_ResolveDiscardFromHand_d__544` 的**净语义**（164 行逐句读过）：</para>
        /// <list type="number">
        /// <item>`:28-30` 闸 = `card.IsInHand(false)`；不过 ⇒ `Log("Failed to discard from hand " + …)` + `Finish`，
        ///   **棋盘/手牌一格都不动**。（⚠️ 原版那句文案里写的就是 `discard` —— `ResolveDisappearFromHand` 是**同一句**，
        ///   拷贝粘贴，⛔ 别以为我们读错。）</item>
        /// <item>`:86-142` 三段演出（`ShowCardBeingDrawn(0.2f)` → `ShowCardLight` / `ShowDescription` → 等三档
        ///   `WaitForSeconds`）+ `DestroyCard(0.3f, false)` —— **我们这一层没有那个演出层**，只落规则侧（如实记）。</item>
        /// <item>`:144-158` `RemoveCostEffects()` → `GetPlayerHand(card.isPlayer).RemoveCardFromHand(card)` → `Finish`。</item>
        /// </list>
        ///
        /// <para>**去向** = `CardScript.GoToCemetery`（`decomp_full/CardScript__GoToCemetery.c` 全文三段）：
        /// `cardState = 6` → 是单位 ⇒ `ResetToValuesInHand(card, 1)` → `HasCurrentTrait(5 ephemeral)`
        /// **为真 ⇒ 只 `SetActive(false)`（跳过 `AddToCemetery`、不进弃牌堆）**，否则 `AddToCemetery`。</para>
        ///
        /// <para>⚠️ **目标怎么来**：原版是 `GetTargets` 按 `targetCriteria` 在**手牌**里当场筛出来的；
        /// 我们这一层**没有实例级 criteria 收口口**（与 `DoStealFromHand` 同一条边界）⇒ 只做到
        /// 「受影响那一侧的**手牌全集**里挑一份」（面板给了就按面板、否则等概率），**并出声**。
        /// 受影响侧按 `op.Target.Side` 判：`own`/`self` ⇒ 本侧，其余 ⇒ 对面（与 `stealhand` 同一条默认）。</para>
        ///
        /// <para>📊 **可达性**：现池 `chosenAbility 160` 的唯一一行落在 **BL37 的重复件**（非池内）⇒ 休眠件。</para>
        /// </summary>
        static bool DoDiscardFromHand(BattleContext ctx, int owner, string by, EffectOp op,
                                      UnitState chosen, List<string> unresolved)
        {
            int seat = DiscardHandSeatFor(ctx, owner, op, by);
            var ps = ctx.Players[seat];
            var inst = PickStealTarget(ctx, ps.Hand, ctx.LastChosenCard, by, op.Source,
                                       (seat == owner ? "你" : "对手") + "的手牌", unresolved);
            if (inst == null) return false;
            return DiscardHandCardCore(ctx, seat, inst, by, op.Source, unresolved);
        }

        /// <summary>
        /// **`discardwholehand` 动词** —— 原版 `chosenAbility 165 discardWholeHand`
        /// （`decomp_full/AbilityLogic__PlayAbility.c:293-345`）。
        ///
        /// <para>**逐句**：`GetCardsInHandList(bm, **inclMinions=1, inclSpells=1, inclWhispers=0, inclWeapons=1**,
        /// inclPlayer, inclEnemy)`（六格签名 = `dump.cs:32595`；三格字面量 `1,1,0,1` **取自 Ghidra 原样** ——
        /// ⛔ 别拿 ILSpy 的实参，它会把整排反编译成 `default`，本项目踩过），后两格 =
        /// `SupportMethods.IsPlayerAffected(card.isPlayer, playersAffected)` 与 `IsPlayerAffected(!card.isPlayer, …)`
        /// ⇒ **整批遍历、每张一次 `AddDiscardFromHand`**（同一套 `_ResolveDiscardFromHand`）。</para>
        ///
        /// <para>⚠️ `inclWeapons=1` 在我们这一版**没有独立的卡种位**（`CardTypeOptions` 只有
        /// `Minion 0 / Hero 10 / Tactic 20 / Whispers 40`）⇒ 等价实现 = **「除密语(40)以外全收」**（如实记）。</para>
        /// </summary>
        static bool DoDiscardWholeHand(BattleContext ctx, int owner, string by, EffectOp op,
                                       UnitState chosen, List<string> unresolved)
        {
            int pa = PlayersAffectedOf(ctx, op, by);
            string paName = PaName(pa);
            bool mine = RuleCore.IsPlayerAffected(true, paName);       // 原版 `IsPlayerAffected(card.isPlayer, pa)`
            bool theirs = RuleCore.IsPlayerAffected(false, paName);    // 原版 `IsPlayerAffected(!card.isPlayer, pa)`
            ctx.Log($"{by}：「{op.Source}」要弃掉整手牌 —— 原版 `PlayAbility.c:293-345` 取 "
                  + "`GetCardsInHandList(inclMinions=1, inclSpells=1, inclWhispers=0, inclWeapons=1, "
                  + $"affected(本侧)={(mine ? 1 : 0)}, affected(对面)={(theirs ? 1 : 0)})`"
                  + $"（`playersAffected={pa}` ⇒ {(pa < 0 ? "**读不到**，按原版 SO 缺省 0 = Both" : paName)}）");
            int total = 0, done = 0;
            for (int s = 0; s < 2; s++)
            {
                bool affected = s == owner ? mine : theirs;
                if (!affected) continue;
                var ps = ctx.Players[s];
                var snap = new List<CardInstance>(ps.Hand);    // 先快照再改（`Hand.Remove` 不能边遍历边改）
                foreach (var c in snap)
                {
                    if (c == null || c.Card == null) continue;
                    // `inclWhispers = 0` ⇒ 密语卡（`cardType 40`）不在候选里
                    if (c.Card.CardType == CardDef.CardTypeWhispers) continue;
                    total++;
                    if (DiscardHandCardCore(ctx, s, c, by, op.Source, unresolved)) done++;
                }
            }
            if (total == 0)
            {
                ctx.Log($"{by}：「{op.Source}」要弃掉整手牌，但受影响那一侧的手牌里**没有可弃的牌** —— 空过");
                return true;                                  // 没牌可弃 = 正常空过（与 `DoDeal` 同一条口径）
            }
            ctx.Log($"{by}：「{op.Source}」整手牌弃掉：候选 {total} 张、真弃掉 {done} 张");
            return done > 0;
        }

        /// <summary>
        /// **一张手牌「弃掉」的三步** —— 原版 `_ResolveDiscardFromHand_d__544` 状态 3/4 与
        /// `CardScript.GoToCemetery` 的**规则侧**等价物（演出那三段我们不落，见 `DoDiscardFromHand` 的 doc）。
        ///
        /// <para>顺序照原版：**闸 `IsInHand` → `RemoveCostEffects` → `DestroyCard/GoToCemetery` → `RemoveCardFromHand`**。
        /// ⚠️ `RemoveCostEffects` 在 `DestroyCard` **之前**（`.c:144-158` 里它其实在同一状态里最先调；
        /// `disappearFromHand` 那一支才是「动画前」—— 两者净效果相同，⛔ 别把两族的文案抄串）。</para>
        /// </summary>
        static bool DiscardHandCardCore(BattleContext ctx, int seat, CardInstance inst, string by,
                                        string source, List<string> unresolved)
        {
            if (ctx == null || inst == null || seat < 0 || seat > 1) return false;
            var ps = ctx.Players[seat];
            // ⓪ `CardScript.IsInHand(false)` 闸（`.c:28-30` 的失败支：一格都不动）
            if (ps.Hand == null || !ps.Hand.Contains(inst))
            {
                ctx.Log($"{by}：Failed to discard from hand {inst.Card?.Name} for player {(seat == 0 ? "False" : "True")}"
                      + " because its state is not inHand —— 原版 `_ResolveDiscardFromHand:31-72` 的失败支"
                      + "（棋盘/手牌一格都不动；⛔ 那句文案原版就写 `discard`，拷贝粘贴，不是我们读错）");
                unresolved.Add(source + "（弃牌目标已不在手牌）");
                return false;
            }
            // ① `CardScript.RemoveCostEffects()`（`.c:144-158`）—— 摘「钉在这一份手牌上」的费用修正
            RuleCore.RemoveCostEffects(ctx, inst);
            // ② `DestroyCard(0.3f, false)` → `GoToCemetery`：**只有单位**才 `ResetToValuesInHand(card, 1)`
            if (inst.Card != null && inst.Card.IsUnit)
                ResetHandCardToValuesInHand(ctx, inst, by, source);
            // ③ ephemeral（trait 5）⇒ 原版只 `SetActive(false)`、**跳过 `AddToCemetery`** = 移出游戏
            if (ctx.IsEphemeral(inst))
            {
                // 复用全仓唯一的「手牌 → 移出游戏」口（它自己会摘费用修正、记 `ctx.Removed`、发 `EvtKind.Return`）
                bool gone = RuleCore.RemoveCardFromHandToRemoved(ctx, seat, inst, "discard-ephemeral");
                ctx.Log($"{by}：「{inst.Card?.Name}」带 `ephemeral`（trait 5）⇒ 原版 `GoToCemetery` "
                      + "只 `SetActive(false)`、**不进弃牌堆**"
                      + (gone ? " ⇒ 我们记进「移出游戏」" : " ⚠️ 但手牌里已经找不到它了"));
                return gone;
            }
            // ④ `RemoveCardFromHand` + `AddToCemetery`
            if (!ps.Hand.Remove(inst))
            {
                ctx.Log($"{by}：「{inst.Card?.Name}」要弃掉时已经不在手牌里 —— **这条没生效**");
                unresolved.Add(source + "（弃牌时手牌里已找不到该卡）");
                return false;
            }
            AddToCemetery(ps, inst);
            ctx.Log($"{by}：**{inst.Card?.Name} 从 {ps.Name} 的手牌进了弃牌堆**"
                  + "（原版 `_ResolveDiscardFromHand` 状态 4 = `RemoveCostEffects` → `DestroyCard` → `RemoveCardFromHand`；"
                  + "翻牌/亮牌那三段演出我们没有，只做结算 —— 如实记）");
            // 表现层要**知道哪张牌离开了手牌**（不是阵亡）⇒ 与 `RemoveCardFromHandToRemoved` 同一条事件
            ctx.Emit(EvtKind.Return, seat, -1, inst.Card?.Name, effect: "discard");
            return true;
        }

        /// <summary>
        /// 这条 op 的目标落在**哪一侧的手牌**（`discardfromhand` 用）：
        /// `own`/`self` ⇒ 本侧；其余（含没写）⇒ 对面 —— 与 `DoStealFromHand` 的默认同口径。
        /// ⚠️ 原版是在**手牌**里按 `targetCriteria` 当场筛（`AbilityLogic.GetTargets`）；
        /// 我们只做到「区域全集里挑一份」⇒ **必须出声**（不静默换目标）。
        /// </summary>
        static int DiscardHandSeatFor(BattleContext ctx, int owner, EffectOp op, string by)
        {
            string side = op != null && op.Target != null ? op.Target.Side : null;
            bool own = side == "own" || side == "self";
            ctx.Log($"{by}：「{op?.Source}」弃手牌的候选 = **{(own ? "本侧" : "对面")}**手牌"
                  + $"（本引擎只做到「区域全集」，原版按 `targetCriteria` 当场筛 —— 与 `stealhand` 同一条边界；"
                  + $"side=`{(string.IsNullOrEmpty(side) ? "（未写）" : side)}`）");
            return own ? owner : 1 - owner;
        }

        /// <summary>
        /// 取这条 op 所在 ability 行的 **`playersAffected`**（原版 `AbilityLogic.playersAffected`：
        /// `0 Both / 10 Self / 20 Enemy`）。**读不到 ⇒ `-1`**（⛔ 不猜；调用方按原版 SO 缺省 `0` 处理并**出声**）。
        ///
        /// <para>载体 = `CardDef.ActiveAbilityLogic` 的逐行 `PlayersAffected`（v24 卡表那一列）。
        /// 只有在「这张卡能取到 ∧ 那一组正好 1 行 ∧ 下标落在范围内」时才返回真值 ——
        /// 多行时「哪一行对应这条 op」判不出（与 `AttachByAbilityKind` 的 `gap:kind-ambiguous` 同一条纪律），
        /// ⛔ 不挑第一行。</para>
        /// </summary>
        static int PlayersAffectedOf(BattleContext ctx, EffectOp op, string by)
        {
            if (ctx == null || op == null || op.LogicIndex < 0) return -1;
            var card = ctx.ActingUnit != null ? ctx.ActingUnit.Card : ctx.PlayingCard;
            if (card == null) return -1;
            var rows = card.ActiveAbilityLogicGroup(CardDef.ActiveAbilityObject);
            if (rows == null || rows.Count != 1) return -1;
            var r = rows[0];
            if (r == null || r.PlayersAffected == null) return -1;
            if (op.LogicIndex >= r.PlayersAffected.Length) return -1;
            int v = r.PlayersAffected[op.LogicIndex];
            if (v < 0) return -1;
            ctx.Log($"{by}：`playersAffected` 读到了 = {v}（`AbilityLogic.playersAffected`；0 Both / 10 Self / 20 Enemy）");
            return v;
        }

        /// <summary>
        /// `playersAffected` 的**原值 → 字符串**（<see cref="RuleCore.IsPlayerAffected"/> 的入参形态）：
        /// `0 ⇒ Both` · `10 ⇒ Self` · `20 ⇒ Enemy` · **其余非 0 值 ⇒ 原样传下去**（它会落到 `false`）。
        /// ⚠️ **未知非 0 值不是 Both** —— 判据 `SupportMethods__IsPlayerAffected.c:5-11` 是**等值比较**。
        /// `-1`（读不到）也按原样传 ⇒ `IsPlayerAffected` 两侧都 `false`；调用点另行出声说明。
        /// </summary>
        static string PaName(int pa)
        {
            if (pa == 0) return "Both";
            if (pa == 10) return "Self";
            if (pa == 20) return "Enemy";
            return pa.ToString();
        }

        // ----------------------------------------------------------------------------------------------
        //  §六 · `disappearUnit(240)` —— 原版**本身没做完**（只有 `SetActive(false)` + 一句 TODO）
        // ----------------------------------------------------------------------------------------------

        /// <summary>
        /// **`disappearunit` 动词** —— 原版 `chosenAbility 240 disappearUnit`
        /// （`decomp_full/AbilityLogic__PlayAbility.c:2780-2795`：遍历 `possibleTargets` ⇒
        /// `BattleManager__AddDisappearUnit(bm, target, prio(+0x44), **checkIfCasterInPlay = 0（写死）**,
        /// casterCard = `abilityData.thisCard(+0x10)`, block(+0x48))`；动作号 `0x2c = 44`）。
        ///
        /// <para>**原版唯一动作**（`BattleManager__ResolveDisappearUnit.c` 全文 234 行、真逻辑 4 句；
        /// 另有一份内联副本 = `ResolveAction.c:3816` 的 `case 0x2c`）：</para>
        /// <code>
        /// if (targetCard != null) { LogWarning("[WARNING] Implement FX. Was on BattleAnims"); targetCard.gameObject.SetActive(false); }
        /// else { LogWarning("Trying to resolve unit disappear without target"); }
        /// FinishResolvingAction(actionType);
        /// </code>
        /// <para>🔴 **它不改棋盘、不移除、不进墓地、不动 `cardState`** —— 纯 `SetActive(false)`，而且原版自己挂着
        /// `Implement FX` 的 TODO。</para>
        ///
        /// <para>🔶 机制不同（**不是自设计**）：原版那一句是**表现层**动作（关掉 GameObject）；我们这一层没有
        /// 「隐藏但仍占格」这个中间态（渲染层按棋盘格号重认视图）⇒ 规则侧**一格不动**（与原版逐字等价），
        /// **画面上的「消失」没做**。接入条件 = 将来若真做「隐身单位」表现，这条就是它的规则侧入口
        /// （届时把 `<see cref="UnitState"/>` 加一个 hidden 位、渲染层照它跳过即可，⛔ 别在结算层假造移除）。</para>
        ///
        /// <para>📊 **可达性**：`chosenAbility 240` 池内 **0 条**（1,351 张全无）⇒ 休眠件。</para>
        /// </summary>
        static bool DoDisappearUnit(BattleContext ctx, int owner, string by, EffectOp op,
                                    UnitState chosen, List<string> unresolved)
        {
            var spec = op.Target ?? new EffectTargetSpec
            {
                Raw = "(未写目标：默认一个敌方单位)", Side = "enemy", Kind = "any", Count = 1, Auto = true,
            };
            var targets = ResolveTargets(ctx, owner, spec, null, chosen, op.TargetCondition, op);
            if (targets.Count == 0)
            {
                // 原版的 else 支：`LogWarning("Trying to resolve unit disappear without target")`，然后什么都不做
                ctx.Log($"{by}：「{op.Source}」要「让单位消失」，但没有指到任何单位 —— "
                      + "原版这一支打 `Trying to resolve unit disappear without target` 后什么都不做"
                      + "（`ResolveDisappearUnit.c` 的 else 支）");
                unresolved.Add(op.Source + "（disappearunit 没有目标）");
                return false;
            }
            int n = 0;
            foreach (var t in targets)
            {
                if (t == null) continue;
                ctx.Log($"{by}：「{op.Source}」让「{t.Name}」消失 —— 原版唯一动作是 "
                      + "`[WARNING] Implement FX. Was on BattleAnims` + `gameObject.SetActive(false)`"
                      + "（**不改棋盘 / 不移除 / 不进墓地 / 不动 cardState**；原版自己挂着 `Implement FX` 的 TODO）"
                      + " ⇒ 我们规则侧一格不动（与原版等价），**画面上的隐藏没做**（🔶 我们没有「隐藏但占格」态）");
                unresolved.Add(op.Source + "（disappearUnit：原版只 SetActive(false)，我们没有隐藏态 ⇒ 画面未做）");
                n++;
            }
            if (n == 0) return false;
            ctx.Log($"{by}：「{op.Source}」共指到 {n} 个单位（规则侧无变化 —— 见上一条）");
            return true;
        }

        // ----------------------------------------------------------------------------------------------
        //  §四·4 · `stealMinionSelf(185)` 的**目标来源**（搬运逻辑不重写：仍走 `DoTakeControl`）
        // ----------------------------------------------------------------------------------------------

        /// <summary>
        /// **`takecontrol` / `stealminionself` 的目标来源分流口**（`DoTakeControl` 的第一句就调它）。
        ///
        /// <para>· **180 `stealMinion`**（`decomp_full/AbilityLogic__PlayAbility.c:384-403`）：遍历 `possibleTargets`
        ///   ⇒ 走文本 / 结构化目标条件那一路（<see cref="ResolveTargets"/>）。</para>
        /// <para>· **185 `stealMinionSelf`**（`:406-414`，**单张、不遍历目标表**）：
        ///   `AddStealMinion(bm, **abilityData.actingCard(+0x18)**, prio(+0x44), block(+0x48))`
        ///   ⇒ 目标是**施放者自己** = 我们的 `ctx.ActingUnit`。</para>
        ///
        /// <para>⚠️ **抢到哪一侧不在这里定**：原版 `CardScript__ChangeOwner.c` 只把 `isPlayer(+0x40)` **取反**、
        /// `EndTurn` 里没有翻回去的路 ⇒ `DoTakeControl` 一律落到「**当前所属那一侧的对侧**」（`dest = 1 - fromP`）。
        /// 所以 185 在「施放者属于本侧」时 = **把它翻给对面**（原版行为，**不是** bug，⛔ 别「修」成不动）。</para>
        ///
        /// <para>⚠️ `ActingUnit` 为空 ⇒ 空表 + 出声（原版那一支 `AddStealMinion` 拿到 `null` 也是空转）。</para>
        ///
        /// <para>📊 **可达性**：`chosenAbility 185` 池内 **0 行**（唯一那行在重复件 `1a647ea3…`）⇒ 休眠件。</para>
        /// </summary>
        static List<UnitState> ResolveTakeControlTargets(BattleContext ctx, int owner, EffectOp op, UnitState chosen)
        {
            if (op != null && string.Equals(op.Verb, "stealminionself", System.StringComparison.Ordinal))
            {
                var list = new List<UnitState>();
                if (ctx.ActingUnit == null)
                {
                    ctx.Log($"「{op.Source}」要抢**施放者自己**，但 `ctx.ActingUnit` 是空的 —— **这条没生效**"
                          + "（原版 `PlayAbility.c:406-414` 的 `abilityData.actingCard` 也取自同一位）");
                    return list;
                }
                list.Add(ctx.ActingUnit);
                ctx.Log($"「{op.Source}」的目标 = **施放者自己**「{ctx.ActingUnit.Name}」"
                      + "（原版 `stealMinionSelf(185)`：`AddStealMinion(actingCard)`，单张、不遍历目标表）");
                return list;
            }
            return ResolveTargets(ctx, owner, op.Target, null, chosen, op.TargetCondition, op);
        }

        // ==========================================================================================
        //  🆕 2026-10-29（第九十四会话 · 执行代理 `B94_F2`）：账 455 / 947 的两件等价位
        //  （新成员一律追加在类尾 / 本文件尾 —— 避免给 `账/*.tsv` 的 `文件:行号` 落点造成漂移）
        // ==========================================================================================

        /// <summary>
        /// **原版 `AbilityLogic.PlayAbility` 的 `chosenAbility 360 = playChosenCardAbility` 支**
        /// （`decomp_full/AbilityLogic__PlayAbility.c:2014-2070`，逐句现读）。
        ///
        /// <para>**「选中的那张」存在哪**：原版 `bm.cardChosenInSelection(+0x2E8)`，写点 =
        /// `BattleManager__ChoiceOfCardEnemy.c:47` / `BattleManager__ClickChosenCardDone.c:22`；
        /// 我们 = `BattleContext.CardChosenInSelection`（写点 = `DoChooseCard`）。</para>
        ///
        /// <para>**原版逐句**：</para>
        /// <list type="number">
        /// <item>`:2015-2019` 取 `thatCard = bm.cardChosenInSelection`；为 `null` ⇒ **整支什么都不做**；</item>
        /// <item>`:2025-2031` `thatCard` **卡面默认**带 `oath(0x4fb)` ⇒ `BattleManager.PayCardPlayedCostOath(bm, abilityData.actingCard, 0)`
        ///   （`BattleManager__PayCardPlayedCostOath.c:11-24`：**按 actingCard 自己的 `oath` 值**扣法力 → `CheckIfCodexTriggered(shouldCheckCost:0)`）；</item>
        /// <item>`:2032-2040` `thatCard.OnTriggerAlt(0 /* ThisCardPlayed */, bm, thisCard, actingCard, targetCard, …, isEnchantment=1, 0)` —— **第一次**；</item>
        /// <item>`:2043-2057` 带 oath 时 `while (IsThereDoubleOathEffect(bm, actingCard.isPlayer) > i) { 再放一次; i++ }`
        ///   ⇒ **总次数 = 1 + 同方场上带 `oathDouble(1277)` 的牌数**（我们 = `RuleCore.OathExtraReplays`，判据只此一处）；</item>
        /// <item>`:2060-2068` 末尾把 `bm.cardChosenInSelection` **清空**。</item>
        /// </list>
        ///
        /// <para>⚠️ **两条如实标着**：① 原版 `OnTriggerAlt` 的 `isEnchantment=1`（同一趟也跑这张卡挂着的附魔能力）
        /// 我们只在正文解析不出来时才试 `Enchantments.RunGrantedEnvAbility`（见 `RunAbilityOfAnotherCard`），
        /// **有正文时不跑附魔** —— 与账 1288 同一处缺口；② 目标那一格原版是 `abilityData.targetCard`，
        /// 我们取调用方给的 `chosen`（= 玩家在主动能力里点定的那个）。</para>
        ///
        /// <para>📊 **可达性**：`chosenAbility 360` 池内 **18 张**（其中 10 张带 `oath`）——
        /// **但上游 `UseActiveAbilityCore` 的 plan 还必须把 `ca == 360` 放进计划表**（那一行归 F1）⇒ 今天仍不达。</para>
        /// </summary>
        public static int RunPlayChosenCardAbility(BattleContext ctx, int p, UnitState u, UnitState chosen)   // `public`：自检宿主在**编辑器程序集**里（本仓**没有** `InternalsVisibleTo`，见 `EffectResolver.cs:7794` 那条记录）⇒ 要能被断言直调（先例 = `BroadcastUnitHealed` 也是 public）
        {
            if (ctx == null || u == null) return 0;
            var cc = ctx.CardChosenInSelection;
            if (cc == null)
            {
                ctx.Log($"「{u.Name}」的主动能力 `chosenAbility 360`（playChosenCardAbility）要看**选中的那张卡**，"
                      + "但 `CardChosenInSelection` 是空的 ⇒ **这一支什么都不做**"
                      + "（原版 `PlayAbility.c:2015-2019`：`bm.cardChosenInSelection == null` 直接返回）");
                return 0;
            }
            bool oath = cc.Has(KeywordTable.Oath);                 // 原版 `:2025` `HasDefaultTrait(thatCard, oath)` = **卡面印的**
            if (oath) PayCardPlayedCostOath(ctx, p, u.Instance);   // 原版 `:2032` —— 付的是 **actingCard** 自己的 oath（`PayCardPlayedCostOath.c:11`）
            int times = oath ? 1 + RuleCore.OathExtraReplays(ctx, p) : 1;   // 原版 `:2043-2057` 的 while（只有带 oath 才补放）
            int ran = 0;
            // 🔴 正文键用 **`rally`**，不是 `"ThisCardPlayed"` —— 本仓 `CardDef.TriggerOps` 的键是**卡面触发词**，
            //   而 `ThisCardPlayed(0)` 的卡面写法就是 `Rally:`（判据 = `ActionPump` 的 `AbilityTriggerNameOfKeyword`
            //   `case KeywordTable.Rally: return "ThisCardPlayed"`，且池内 105/105 张带 rally 的卡 `abilityTriggers` 都有 0）。
            //   传 `"ThisCardPlayed"` ⇒ `TriggerOps` 恒 `null` ⇒ 静默空转（本代理 2026-10-29 实测）。
            for (int i = 0; i < times; i++)
                if (RunAbilityOfAnotherCard(ctx, cc, KeywordTable.Rally, p, u, actingCard: u, targetCard: chosen, by: u.Name)) ran++;
            ctx.CardChosenInSelection = null;                      // 原版 `:2060-2068`：`bm+0x2e8 = 0`
            ctx.Log($"「{u.Name}」的主动能力 `chosenAbility 360`：让选中的「{cc.Name}」按 `ThisCardPlayed` 跑它自己的能力 "
                  + $"**{ran}/{times}** 次" + (oath ? "（它带 `oath` ⇒ 已按誓约再付一次、并按 `oathDouble` 牌数补放）" : "（不带 `oath` ⇒ 只放一次、不付费）")
                  + "；选中那一格已清空（原版 `AbilityLogic__PlayAbility.c:2014-2070`）");
            return ran;
        }

        /// <summary>
        /// **原版 `CardScript.TriggerOtherCardAbility(RawCardScript abilityCard)`** 的等价位
        /// （`out_cpp2il_dev_ilspy/CardScript.cs:7718-7726` ＋ 真值 `out_il2csharp/CardScript.cs:2187-2189`）。
        ///
        /// <para>原版逐句：`if (abilityCard != null) abilityCard.OnTriggerEffectInCard(ThisCardPlayed, mgr, this, this, null);`
        /// ⇒ **trigger 硬编码 `ThisCardPlayed`**、`thisCard = actingCard = 调用者`、`targetCard = null`
        /// （`il2csharp` 的真值就是 `(ThisCardPlayed, mgr, this, this, 0)`）。</para>
        ///
        /// <para>5 个调用点（现读）：`BattleManager__ApplyEnvEffect.c:15` · `_StartBattlePhase_d__334__MoveNext.c:88/201` ·
        /// `_ResolveSummonUnit_d__510__MoveNext.c:252` · `AbilityLogic__PlayAbility.c:2869`。</para>
        ///
        /// <para>⚠️ 账 947 的落点原来写 `EffectResolver.cs:237/334`（那是 **ability 级 `targetCriteria` 闸**，
        /// 与本方法**无关**）⇒ 本口才是它的等价位（落点已订正）。</para>
        /// </summary>
        public static bool TriggerOtherCardAbility(BattleContext ctx, CardDef abilityCard, int owner,
                                                   UnitState callerCard = null, string by = null)   // `public`：理由同上（编辑器程序集的断言宿主）
        {
            if (ctx == null || abilityCard == null) return false;
            return RunAbilityOfAnotherCard(ctx, abilityCard, KeywordTable.Rally, owner,   // 正文键 = `rally`（= `ThisCardPlayed(0)` 的卡面写法，见上一条注释的判据）
                                           abilityOwnerUnit: callerCard, actingCard: callerCard, targetCard: null,
                                           by: by ?? (callerCard != null ? callerCard.Name : abilityCard.Name));
        }
    }
}
