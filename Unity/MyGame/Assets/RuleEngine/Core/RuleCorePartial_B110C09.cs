// RuleCorePartial_B110C09.cs — 死码 C09：`BattleManager.ReplaceHeroPower`（换督军主动能力）的引擎侧写口
//
// ==================================================================================================
//  一、判据（第一权威 = 全量反编译；三源交叉：`decomp_full/*.c` → `out_cpp2il_dev_ilspy` → `out_il2csharp`）
// ==================================================================================================
//  ① **方法本体**（`d:/2/tools/decomp_full/BattleManager__ReplaceHeroPower.c:2-140`，逐句亲读）：
//     ```
//     void ReplaceHeroPower(this, RawCardScript newPower /*param_2*/, bool isPlayer /*param_3*/, int priority /*param_4*/)
//     {
//         BattleAction a;                 // 栈上 128 字节
//         a.actionType   = 0x18;          // `:56`  `local_208 = 0x18`（= `BattleActionType.replaceHeroPower = 24`）
//         a.isPlayer     = isPlayer;      // `:90`  `uStack_204 = CONCAT71(…, param_3)`
//         a.newCardData  = newPower;      // `:88`  `local_180 = param_2` + 写屏障 `FUN_1803f3730`
//         // 其余字段全 0（`:57-87` 一串 `= 0`）
//         AddAutoActionToQueue(this, &a, priority);   // `:138`  ← **只入队，什么都没干**
//     }
//     ```
//     ⇒ **本方法除了「入队」不做任何事**。真正生效在 `ResolveAction` 结算那一条动作，
//       也就是**动作队列的泵点**（原版 `BattleManager.Update` → `ResolveActionFromQueue`）。
//     三源交叉：`out_cpp2il_dev_ilspy/BattleManager.cs:39644` 与 `out_il2csharp/Assembly-CSharp/BattleManager.cs:18631`
//     的体都只剩「结构体拷贝 + `AddAutoActionToQueue`」，与 `.c` 同形（ILSpy 那份丢了 `24` 那个常量，
//     il2csharp 那份明确写着 `object obj5 = 24;`）—— 以 `.c` + il2csharp 为准。
//
//  ② **入队的语义**（`BattleManager__AddAutoActionToQueue.c:47-171`）：`priority` 0 → `actionQueue(+0x350)`、
//     1 → `instantQueue(+0x358)`、2 → `interruptQueue(+0x368)`、3 → `delayedInstantQueue(+0x360)`；
//     **其它值 `LogError` + 丢弃（⛔ 不 clamp）**（`:224-229`）。我们的等价格 = `ActionQueue.Enqueue(act, lane)`
//     （`Core/ActionQueue.cs:261`），lane 的字面量就是 0/1/2/3（`ActionPriority`）。
//
//  ③ **「换上去」那一步的载体**（这是关键的一格，判据不是猜的）：
//     · `BattleAction.newCardData`（`dump.cs` 的 `BattleAction` 字段表，`RawCardScript`）就是新的能力卡；
//     · 它落到督军身上的形状 = **一条 `CardEffect{ buffType = replaceAbility(40), enchantment = newPower }`**
//       （`BuffType` 枚举 `d:/2/tools/il2cpp_out/dump.cs:45430`：`replaceAbility = 40`；
//        `CardEffect.enchantment // +0x88` 是 `RawCardScript`，`dump.cs:119060` 段）；
//     · 那条效果被加进 `activeEffects(+0x108)` 之后，`CardScript__CheckReplacedAbility.c:19-40` 逐条扫：
//       命中 `buffType == 0x28` ⇒ `*(this + 0x220) = 1`（`hasActiveAbility`）＋
//       `*(this + 0x118) = *(effect + 0x88)`（`activeAbilityRef` = 新能力卡）＋ 刷新图标。
//       ⇒ **原版是「运行期覆盖 activeAbilityRef」，不是改卡面定义。**
//
//  ④ 🔴 **反证（必须记，⚠️ 施工单的前提与二进制不符，见报告 §「没查清的部分」/ §顺手发现）**：
//     · `BattleManager.ReplaceHeroPower` 在**三源里 0 个调用点**（`grep -rn ReplaceHeroPower` 只命中它自己的定义；
//       il2csharp 里连 `BattleActionType.replaceHeroPower` 这个枚举名都没有第二个使用者）；
//     · `ScriptedActionType`（原版 `dump.cs:42004`）**根本没有 24 这个值**（只有 0/10/20/30/31/35/36/40/50/55/60/65/
//       70/80/90/100/110/120/130/140/150/160/170）⇒ **不存在「教程动作 24」**；`replaceHeroPower` 是
//       **`BattleActionType`（引擎动作）** 的 24 号；
//     · 卡池 `buffType 40` **0 行**（既有账 `:1026` 的读数，本条已在 `EffectResolverPartial_B97W2.cs:302-310` 记死）。
//     ⇒ 原版这条链**今天不可达**（休眠死代码）。本文件照 铁律 11 / §18·0「原版有的就照抄」把它补齐：
//       写口 + 读口建好，**0 输入 ⇒ 零可观测**；⛔ **没有**、也**不许**为此给 `ScriptedActionType` 编一个 24 号
//       （那是造一个原版没有的枚举值 —— 违反 铁律 3 / §18·0）。
//
// ==================================================================================================
//  二、我们这一侧的两个「落点」，为什么是这个形状
// ==================================================================================================
//  · **状态位放在 `UnitState` 上**（下面那个 partial 半），对应原版 `CardScript + 0x118 / + 0x220` ——
//    原版那一格是**实体（CardScript）级**的，不是玩家级、也不是卡定义级 ⇒ 我们照抄到「场上的这个单位」。
//    ⛔ **别把覆盖写进 `CardDef`**：那是**全场共享的不可变模板**，写它会污染全池（同
//    `UnitState.ActiveAbilityHidden` 的既有裁定，`UnitState.cs:3180-3190`）。
//  · ⚠️ **已知缺口（白名单外，交回补丁）**：真正的「用技能」入口 `RuleCore.CanStartAbility` /
//    `RuleCore.UseAbilityCore`（`Core/RuleCore.cs:11306 / 11750`）与 `UnitState.Ability`（`UnitState.cs:985`）
//    今天**仍读 `Card`**，没有走本文件的 <see cref="RuleCore.HeroPowerCard"/>。它们都在本批白名单外
//    （`RuleCore.cs` 不插行、`UnitState.cs` 不动）⇒ 消费端的接线写在报告 §改动请求里，**⛔ 本文件不硬改**。
//    这也正是「口径与实现分两步」的老实记法 —— 本文件今天给的是**写口 + 唯一读口**，不是「已接满」。
//
// ==================================================================================================
//  三、🔴 灭自证（断言怎么才能红，见 `CardPresentation/Editor/B110_C09_Assert.cs`）
// ==================================================================================================
//  · <see cref="RuleCore.ReplaceHeroPower"/> 里**当场改**（而不是入队）⇒ 「入队后、未泵队时能力仍是旧的」那条红。
//  · 把 priority 越界改成 clamp ⇒ 「越界 ⇒ LogError + 不入队」那条红。
//  · 把 `newPower == null` 的分支从「LogError + 什么都不做」改成「清回卡面」⇒ 对应用例红。
using System;

namespace RuleEngine
{
    /// <summary>
    /// 引擎 `RuleCore` 那一半：`ReplaceHeroPower` 的**写口 + 唯一读口**。
    /// <para>⛔ 本类不碰 `RuleCore.cs`（白名单外）—— 全部是**新增成员**，零行号漂移。</para>
    /// </summary>
    public static partial class RuleCore
    {
        /// <summary>
        /// 原版 `BattleActionType.replaceHeroPower` 的数值（`d:/2/tools/il2cpp_out/dump.cs:45306` 段；
        /// 与 `BattleManager__ReplaceHeroPower.c:56` 的 `local_208 = 0x18` 互证）。
        /// <para>⚠️ 这是 **`BattleActionType`（引擎动作）** 的号，**不是** `ScriptedActionType`（教程脚本动作）
        /// —— 后者原版**没有** 24 号（`dump.cs:42004`，22 个值逐个核过）。⛔ 别混。</para>
        /// </summary>
        public const int ActionTypeReplaceHeroPower = 0x18;   // = 24

        /// <summary>
        /// 原版 `BattleManager.ReplaceHeroPower(RawCardScript newPower, bool isPlayer, int priority)`
        /// （`d:/2/tools/decomp_full/BattleManager__ReplaceHeroPower.c:2-140`）。
        ///
        /// <para>🔴 **按原版：只入队、不立即生效。** 本方法**不**开 `ActionScope`、**不**泵队 ——
        /// 生效时刻由泵点决定（`ActionPump.ActionScope` 退出 / 显式 `PumpActionQueues`），
        /// 与原版「`AddAutoActionToQueue` 只 `List.Add`，取队的是 `BattleManager.Update`」逐字同形。
        /// ⛔ 别在这里做「队列空就直接执行」的短路（同 `EnqueueAbility` 的 doc）。</para>
        ///
        /// <para>参数语义：</para>
        /// <list type="bullet">
        /// <item><paramref name="seat"/> = 原版的 `isPlayer`（**0 = 玩家 / 1 = 敌方**；本工程全链的座位约定，
        ///       见 `TutorialScript.cs` 类头那段「档位约定」）。</item>
        /// <item><paramref name="priority"/> = 原版的第 3 个形参，**字面量 0..3**（四条道）。
        ///       越界 ⇒ 照原版 `AddAutoActionToQueue.c:224-229` **LogError + 丢弃**（⛔ 不 clamp）。</item>
        /// <item><paramref name="newPower"/> = 原版 `newCardData`（新的主动能力卡）。`null` **不在这里拦** ——
        ///       原版构造动作时不校验，校验在结算那一跳（`enchantment == null ⇒ LogError + 不做事`）。
        ///       本条照抄：`null` 能入队，结算时出声、不换。</item>
        /// </list>
        ///
        /// <returns>`true` = 已入队；`false` = 被拒（ctx / 座位 / 督军 / priority 任一不合法，均已出声）。</returns>
        public static bool ReplaceHeroPower(BattleContext ctx, int seat, CardDef newPower, int priority)
        {
            if (ctx == null)
            {
                UnityEngine.Debug.LogError(
                    "[RuleCore] `ReplaceHeroPower`：`ctx == null` ⇒ 丢弃（没有对局就没有动作队列）。");
                return false;
            }
            if (seat < 0 || seat >= ctx.Players.Length)
            {
                UnityEngine.Debug.LogError(
                    $"[RuleCore] `ReplaceHeroPower`：座位 {seat} 越界（本局 {ctx.Players.Length} 席）⇒ 丢弃。"
                  + "原版那一格是 `isPlayer`（0/1），⛔ 不 clamp。");
                return false;
            }
            var ps = ctx.Players[seat];
            if (ps == null || ps.Warlord == null)
            {
                UnityEngine.Debug.LogError(
                    $"[RuleCore] `ReplaceHeroPower`：{seat} 号位的督军是 null ⇒ 没有可换的载体，丢弃。"
                  + "（原版 `BattleManager__ReplaceHeroPower.c` 自己**不判**这一格 —— 它只拼结构体；"
                  + " 我们是**当场**就能判出「换了也没人」⇒ 出声、不塞一条注定空转的动作。）");
                return false;
            }
            if (priority < 0 || priority > 3)
            {
                // 判据 = `BattleManager__AddAutoActionToQueue.c:224-229`：
                //   `CustomDebug.LogError(… + Int32.ToString(priority))` 然后 `return`（⛔ 不 clamp、不塞任何一道）。
                UnityEngine.Debug.LogError(
                    $"[RuleCore] `ReplaceHeroPower`：`priority = {priority}` 不在 0..3 ⇒ 丢弃"
                  + "（原版 `BattleManager__AddAutoActionToQueue.c:224-229` 对这一档是 LogError + return，"
                  + "⛔ 不是 clamp 到某一条道）。");
                return false;
            }

            var warlord = ps.Warlord;
            var lane = (ActionPriority)priority;
            var act = new QueuedAction
            {
                Priority = lane,
                // `Payload` 只作排查用（原版 `BattleAction` 里那一格是 `newCardData`，我们这条动作没有结构化载荷）。
                Payload = newPower,
                ActionType = ActionTypeReplaceHeroPower,
                // 原版这一条 `BattleAction` 的 `actingCard / targetCard` 都是 default(null)，⛔ 别拿督军顶上去 ——
                // 顶了会让泵点的「载体复核」把这条动作当成「某个单位的 ability」去取消（`ActionPump.PumpActionQueues`
                // 的 `CarrierReplacedByRemnant` 那一支）。
                Actor = null,
                Label = $"replaceHeroPower：{ps.Name} 的督军换成「{(newPower != null ? newPower.Name : "null")}」",
                Execute = () => ApplyReplaceHeroPower(ctx, seat, warlord, newPower),
            };
            return ctx.ActionQueue.Enqueue(act, lane);
        }

        /// <summary>
        /// 动作**结算**那一刻（队列出队时）真正落状态的那一跳 —— 原版 `ResolveAction` 的
        /// `case 0x18` 那一支的等价物：把 `CardEffect{ buffType = replaceAbility(40), enchantment = newPower }`
        /// 挂到督军身上，由 `CardScript__CheckReplacedAbility` 落成
        /// `hasActiveAbility(+0x220) = 1` + `activeAbilityRef(+0x118) = 新卡`。
        ///
        /// <para>⚠️ 反编译那一支**读不出来**：`BattleManager__ResolveAction.c` 的 `switch(local_228[0])`
        /// 里 `case 0x17` / `case 0x19` 在、**`case 0x18` 不在**（Ghidra 丢了一支；同族已知坑：
        /// 「Cpp2IL 静默丢体 / 跳表漏项」）。⇒ 本方法的**形态**取自上面 ①③ 两段判据（结构体 → 效果 → CheckReplacedAbility），
        /// 而**不是**逐字搬某一支行号。这一点如实记在这里，⛔ 别当成「已核到 case 0x18」。</para>
        /// </summary>
        static void ApplyReplaceHeroPower(BattleContext ctx, int seat, UnitState warlord, CardDef newPower)
        {
            if (ctx == null || warlord == null) return;
            if (newPower == null)
            {
                // 原版 `CardScript__AddEffect` 的 `replaceAbility(40)` 那一支：`enchantment == null ⇒ LogError + 什么都不做`。
                UnityEngine.Debug.LogError(
                    "[RuleCore] `replaceHeroPower` 结算时 `newPower == null` ⇒ **不换**（原版 `replaceAbility(40)` 的"
                  + " `enchantment == null` 支就是 LogError 后整条 return）。这一条动作到此为止。");
                return;
            }
            warlord.HeroPowerOverride = newPower;
            ctx.Log($"{ctx.Players[seat].Name} 的督军「{warlord.Name}」主动能力被换成「{newPower.Name}」"
                  + "（原版 `BattleManager.ReplaceHeroPower` → `AddAutoActionToQueue(0x18)` → 结算 →"
                  + " `CardEffect{buffType = replaceAbility(40)}` → `CardScript__CheckReplacedAbility`："
                  + " `hasActiveAbility(+0x220) = 1` · `activeAbilityRef(+0x118) = 新卡`）");
        }

        /// <summary>
        /// **督军此刻的主动能力卡**（唯一读口）。
        /// <para>有覆盖 ⇒ 覆盖那张（原版 `activeAbilityRef(+0x118)` 非 null）；否则 ⇒ 卡面那张（`UnitState.Card`）。</para>
        /// <para>⚠️ 今天**还没有生产消费者**（`CanStartAbility` / `UseAbilityCore` 仍在白名单外，见类头 §二）——
        /// 这不影响语义：没有覆盖时它**逐字等于** `u.Card`，接上之后行为才变。</para>
        /// </summary>
        public static CardDef HeroPowerCard(BattleContext ctx, int seat)
        {
            var w = WarlordOf(ctx, seat);
            return w != null ? w.HeroPowerCard : null;
        }

        /// <summary>督军此刻的主动能力正文（`EffectSpec`）—— 取 <see cref="HeroPowerCard"/> 那一张的 `Ability`。</summary>
        public static EffectSpec HeroPowerAbility(BattleContext ctx, int seat)
        {
            var c = HeroPowerCard(ctx, seat);
            return c != null ? c.Ability : null;
        }

        /// <summary>督军此刻的主动能力费用（原版 `ActiveAbility.manaCost`）—— 取 <see cref="HeroPowerCard"/> 那一张。
        /// <para>`-1` = 未知（旧卡表 / 夹具没给），⛔ 别当 0（见 `CardDef.ActiveAbilityManaCost` 的 doc）。</para></summary>
        public static int HeroPowerManaCost(BattleContext ctx, int seat)
        {
            var c = HeroPowerCard(ctx, seat);
            return c != null ? c.ActiveAbilityManaCost : CardDef.GateUnknown;
        }

        /// <summary>督军此刻「有没有主动能力」—— 有覆盖 ⇒ 真（原版 `CheckReplacedAbility` 无条件置 `+0x220 = 1`）；
        /// 否则 ⇒ 卡面那一位（`CardDef.HasActiveAbility == 1`）。</summary>
        public static bool HeroPowerHasActiveAbility(BattleContext ctx, int seat)
        {
            var w = WarlordOf(ctx, seat);
            return w != null && w.HeroPowerHasActiveAbility;
        }

        /// <summary>座位 → 督军（越界 / 空局 ⇒ `null`）。⛔ 不抛。</summary>
        static UnitState WarlordOf(BattleContext ctx, int seat)
        {
            if (ctx == null || seat < 0 || seat >= ctx.Players.Length) return null;
            var ps = ctx.Players[seat];
            return ps != null ? ps.Warlord : null;
        }
    }

    /// <summary>
    /// `UnitState` 那一半：**运行期覆盖位**，对应原版 `CardScript.activeAbilityRef(+0x118)` 与
    /// `hasActiveAbility(+0x220)` 这一对。
    ///
    /// <para>🔴 **为什么另开一个 partial 文件而不是写进 `UnitState.cs`**：那个文件下游有大量 `账/*.tsv`
    /// 按【行号】引用（`check_ledger_points` 会整体漂移）⇒ 新成员一律进新文件。
    /// 本条与 `CardDefActiveAbility.cs` 是同一处置（那份也是「为 `CardDef` 补标量」的独立 partial）。</para>
    ///
    /// <para>⛔ **别把它做成 `CardDef` 上的可变字段** —— `CardDef` 是全场共享的不可变模板
    /// （同 `ActiveAbilityHidden` 的既有裁定）。这里的覆盖**只影响这一个单位**。</para>
    /// </summary>
    public partial class UnitState
    {
        /// <summary>
        /// 原版 `CardScript.activeAbilityRef(+0x118)` 的等价位 —— 由 `replaceAbility(40)` 效果写入
        /// （`CardScript__CheckReplacedAbility.c:19-40`）。
        /// <para>`null` = 没有覆盖（**绝大多数单位**，读卡面）。写点 = `RuleCore.ApplyReplaceHeroPower`；
        /// 复位点 = <see cref="ClearHeroPowerOverride"/>（原版 `SetBaseCardTraits` / 摘掉那条效果时）。</para>
        /// <para>⚠️ 不进 `PlayerHash` / `Fingerprint`：它是**表现/入口选卡**用的指向，不参与任何数值结算
        /// （与原版那一格一样 —— 原版结算读的是 `activeAbilityRef` 指的卡，但**没有**把引用本身算进局面哈希）。</para>
        /// </summary>
        public CardDef HeroPowerOverride;

        /// <summary>此刻的主动能力卡：有覆盖用覆盖，否则用卡面（= 原版 `+0x118 ?? 卡面`）。</summary>
        public CardDef HeroPowerCard { get { return HeroPowerOverride != null ? HeroPowerOverride : Card; } }

        /// <summary>此刻的主动能力正文（见 <see cref="HeroPowerCard"/>）。</summary>
        public EffectSpec HeroPowerAbility { get { var c = HeroPowerCard; return c != null ? c.Ability : null; } }

        /// <summary>此刻「有没有主动能力」：有覆盖 ⇒ 真（原版 `CheckReplacedAbility` 无条件置 `+0x220 = 1`）；
        /// 否则 ⇒ 卡面 `HasActiveAbility == 1`。</summary>
        public bool HeroPowerHasActiveAbility
        {
            get
            {
                var c = HeroPowerCard;
                if (c == null) return false;
                return HeroPowerOverride != null || c.HasActiveAbility == 1;
            }
        }

        /// <summary>
        /// 摘掉覆盖位（回到卡面）—— 原版摘掉那条 `replaceAbility(40)` 效果之后 `CheckReplacedAbility`
        /// 会重新扫一遍 `activeEffects`：没有那一条 ⇒ `+0x118` 回到卡面。
        /// <para>🔴 **今天没有生产调用点**：`UnitState.RemoveEffectRecord` 的 `case 40`（`UnitState.cs:2798`）
        /// 现在还是 `break;`（那时我们还没有覆盖位，注释写的是「恒读卡面 ⇒ 已是重置态」）—— 那条一并交回补丁
        /// （见报告 §改动请求 ②）。</para>
        /// </summary>
        public void ClearHeroPowerOverride() { HeroPowerOverride = null; }
    }
}
