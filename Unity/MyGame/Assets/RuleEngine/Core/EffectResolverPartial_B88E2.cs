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
        /// <para>📊 **可达性**（`cards_truth.json` 全池 `createAtPos` 71 处）：`14 nextFriendlyMinionSpot` × 63 ·
        ///   `140 createCardPos` × 4 · `6 boardCenterUI` × 2 · `3 friendlyHero` × 1 · `18 friendlyHand` × 1 ——
        ///   **`0 self` / `1 cardTarget` 全池 0 处**；`3 friendlyHero` 那 1 处落在一张 GUID 名卡上。
        ///   ⚠️ **本函数今天没有调用点**：`SummonCriteria.createAtPos` 在我们的 DTO 上**还没有格子**
        ///   （`grep -rn createAtPos Assets` = 只有注释）⇒ 「贴卡三档」的**接线**要
        ///   ① `CardDef`/`EffectSpec` 加 `createAtPos` 字段（卡牌线）② 生成器带出数据 ③
        ///   `RuleCore.DeployFreeCore` 接受「请求格」（`RuleCore.cs`，本代理白名单外）。
        ///   本函数 = 那三件里**算法那一件的判据正本**，接上即生效。</para>
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
        /// <para>**唯一持卡**：`Indomitus Crusade`（Ultramarines）的 Oath 句
        /// 「Deploy an Ultramarines troop that costs 6 or more. **Oath 6**: Deal damage to all enemy troops
        /// **equal to its [Attack]**」⇒ 「its」= **同一条 ability 前半句刚部署出来的那个单位**。</para>
        ///
        /// <para>**判据（名字本身就说明了口径）**：`cpp2il/out_cpp2il_dev_ilspy/ChooseValueOptions.cs:15`
        /// —— `BaseRefCardMAttack = 101`（同表还有 `RefCardAttack = 100`，那一档是**当前**近战）。
        /// ⇒ 本函数取 `UnitState.AttackBase`（**base**，不含 `addStats` 修饰），⛔ 别用 `Attack`（那是 100 那一档）。</para>
        ///
        /// <para>**"参照卡" 在我们这套模型里的落点**：`ctx.LastCreated` 的**最后一份**实例
        /// （`RuleCore.DoCreate` / `DoDeploy` 造牌时写它；原版对应 `BattleAction.refCard`）。
        /// 找不到（不在棋盘上 / 没造过牌）⇒ 返回 `-1`，调用方据此**出声**、⛔ 不当 0
        /// （0 会让这条伤害「看起来打出去了但没伤害」—— 正是账 77 那条缺陷的形状）。</para>
        /// </summary>
        public static int RefCardBaseAttack(BattleContext ctx)
        {
            if (ctx == null) return -1;
            // ① `ctx.LastCreated` 的**最后一份**（造牌窗口里最可靠的「刚才造出来的」）
            if (ctx.LastCreated != null && ctx.LastCreated.Count > 0)
            {
                var inst = ctx.LastCreated[ctx.LastCreated.Count - 1];
                if (inst != null)
                    for (int p = 0; p < 2; p++)
                    {
                        var board = ctx.Players[p].Board;
                        for (int s = 0; s < BoardSpec.Size; s++)
                        {
                            var u = board[s];
                            if (u != null && ReferenceEquals(u.Instance, inst)) return u.AttackBase;
                        }
                    }
            }
            // ② 退路：`ctx.LastTarget`（部署那条 op 自己会把它种成刚落场的那个单位 ——
            //    `DoDeploy` 里 `ctx.LastTarget = dep`）。⚠️ 誓约句是**另一条 ability**，
            //    `ResolveOps` 入口会清 `LastCreated` / `LastTarget`（除非给了 `prime`）
            //    ⇒ 这条退路只在「同一条 ability 里 deploy 后立刻算」时可用；都拿不到就返回 -1。
            if (ctx.LastTarget != null)
            {
                for (int p = 0; p < 2; p++)
                {
                    var board = ctx.Players[p].Board;
                    for (int s = 0; s < BoardSpec.Size; s++)
                        if (ReferenceEquals(board[s], ctx.LastTarget)) return ctx.LastTarget.AttackBase;
                }
            }
            return -1;
        }

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
    }
}
