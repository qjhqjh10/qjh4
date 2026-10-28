// TriggerCarrier.cs — **触发派发核心的载体视图**（原版 `RawCardScript.OnTrigger` 的 `thisCard` 那一格）。
//
// ══════════════════════════════════════════════════════════════════════════════════════════
//  这是什么
// ══════════════════════════════════════════════════════════════════════════════════════════
//  原版只有**一个** `CardScript` 类型：一张卡在手里、在场上、在墓地都是**同一个对象** ⇒
//  `RawCardScript.OnTrigger → TriggerAbilities` 从来不需要区分「这张卡在哪个区域」。
//  我们把它拆成了 `CardInstance`（手牌/牌库/弃牌堆）+ `UnitState`（场上）两个类型，
//  于是派发核心被写成了「必须 `UnitState`」—— **这是我们自己的架构偏离**，不是原版形状。
//
//  本类型把派发核心**真正需要的那几个成员**抽成一层窄视图，让
//  「棋盘单位」与「手牌的那一份」各提供一个适配器、**走同一个 switch**。
//
//  📌 **这不是「🔶 自己设计」**（按 `CLAUDE.md` 铁律 18 的「⛔ 不许打标记」第 ② 条）：
//    它**只改结构、不改语义** —— 每个成员都是到既有实现的**转发**，行为恒等。
//    （本句是 2026-10-28 B92-B5 对施工单 §3 草稿里那个 `🔶` 标记的**就地订正**：
//      草稿把它当自设计，其实它属于「命名/结构不同但语义一致」= 实现风格。）
//
// ══════════════════════════════════════════════════════════════════════════════════════════
//  判据（第一权威，逐句，2026-10-28 现读）
// ══════════════════════════════════════════════════════════════════════════════════════════
//  · `decomp_full/RawCardScript__OnTrigger.c:1-19` —— `OnTrigger` 是个 **2 行转发器**：
//    造一个匿名对象后直接 `TriggerAbilities(...)`。**它没有任何区域判断。**
//  · `decomp_full/RawCardScript__TriggerAbilities.c:1-272`（全文读）—— 派发核心只读
//    「rawCard 的 abilities 表 / 卡的 traits / IsSpentEnchantment / ReachedQuestCounter /
//     CardAbility.CanTriggerAbility / activeEnchantments」这几类，**一次都不读棋盘位置**。
//  · 「在不在场」这件事**只**通过 `CardAbility.CanTriggerAbility → CardAbility.IsRightContext`
//    （`decomp_full/CardAbility__IsRightContext.c:1-90`，六档）作为**一道闸**进来：
//      `0` = `IsInPlay` ∧ `!EnoughPendingDamageToDie` ∧ `+0x65 == 0`
//      `10` = **`IsInHand`**   `20` = `cardState == 0`   `30` = `cardState == 6`
//      `40` = `IsInPlayOrDying`   `50` = `IsInCemetery ∨ IsInPlay`   其它 ⇒ 警告 + 拒
//    ⇒ 所以「手牌里能不能响」的答案在**那道闸**里，**不在**派发核心里。
//
// ══════════════════════════════════════════════════════════════════════════════════════════
//  🔴 施工状态（⛔ 别把「类型已建」当成「已经接线」）
// ══════════════════════════════════════════════════════════════════════════════════════════
//  · **本文件 = 施工单的第 ❶ 步**（`判据/B92_B5_手牌触发执行口_施工单.md` §9）：
//    类型 + 两个工厂 + 逐成员转发，**先不接任何调用点**。
//  · **第 ❷/❸ 步（把 `RuleCore.FireTriggerAt` 改成「一行转发 + 本类型的重载」）不在本笔**
//    —— `RuleCore.cs` 是另一个写手的文件。接线时的**逐条映射**见文件末「§接线备忘」。
//  ⇒ 现读全仓**零调用点**；`typecheck` 通过不代表它被验证过行为（行为等接线后才能验）。
//
// ══════════════════════════════════════════════════════════════════════════════════════════
//  ⛔ 三条纪律
// ══════════════════════════════════════════════════════════════════════════════════════════
//  1. **`FxOps` 是「原生 or 挂上去」的唯一取法** —— ⛔ 别在别处再写一份（`UnitState.FxOps`
//     的头注释已经写了「全仓只此一处判据」，本类型只是转发，不是第二份）。
//  2. **`Unit` 只准「棋盘专属的那几段」用**（附魔扫描 / 棋盘监听者快照 / 格号与 pinnedOwner）。
//     手牌载体的 `Unit` 恒 `null` ⇒ 用它之前**必须先判 `IsBoardPath`**（这一处就是分支点）。
//  3. 🔴 **禁止用手牌载体做「单位」的事**：`Health` / `PlayOrder` / `keywords` / buffs 那些是
//     棋盘态，手牌没有。⛔ 别为了让某个下游编译过去而给 `CardInstance` 补一个假的 `UnitState`
//     —— 那会读出一堆**伪造默认值**，而且是静默的（本仓红线）。
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>
    /// 触发派发核心的**载体视图**（原版 `RawCardScript.OnTrigger` 的 `thisCard`）。
    ///
    /// <para>**为什么要有它**：原版一个 `CardScript` 通吃「手牌 / 场上」，派发核心
    /// （`RawCardScript.TriggerAbilities`）因此从不判区域；我们拆成 `CardInstance` + `UnitState`
    /// 之后把核心写成了「必须 `UnitState`」⇒ 手牌卡的 `abilityTriggers` **一条都发不出来**。
    /// 本类型把核心真正需要的成员抽平，两种载体各出一个工厂。</para>
    ///
    /// <para>**成员清单的判据**：实测 `RuleCore.FireTriggerAt` / `FireTriggerDeferred` /
    /// `FireTriggerAlways` 三段里对 `UnitState` 的**全部**用法只有
    /// `Card`(14 处) · `Name`(8) · `InstanceId`(3) · `FxOps`(2) · `Effect`(2) · `GrantedText`(1)
    /// ⇒ 一个不多、一个不少。⛔ 别把「顺手能读到的」都塞进来。</para>
    /// </summary>
    public readonly struct TriggerCarrier
    {
        // ---- 底层（只有一个非 null）--------------------------------------------------
        readonly CardInstance _inst;      // 两种载体都有（棋盘单位也一定有：`UnitState.Instance`）
        readonly UnitState _unit;         // **只有棋盘载体**有；手牌恒 null
        readonly string _name;            // 日志用（棋盘 = `UnitState.Name`；手牌 = `Card.Name`）
        readonly int _owner;              // 归谁（手牌/棋盘都由调用方给 —— 原版从管理器取）
        readonly int _slot;               // 棋盘格号；手牌 = -1
        readonly bool _isBoardPath;       // ⭐ 唯一分支开关（见属性 doc）

        TriggerCarrier(CardInstance inst, UnitState unit, string name,
                       int owner, int slot, bool isBoardPath)
        {
            _inst = inst; _unit = unit; _name = name;
            _owner = owner; _slot = slot; _isBoardPath = isBoardPath;
        }

        // ---- 工厂（**只有这两个**）----------------------------------------------------

        /// <summary>
        /// **棋盘载体**（`FireTriggerAt` 现在那条路）。
        ///
        /// <para>🔴 `isBoardPath` 恒 `true`，**即使 `u == null`** —— 因为今天就存在
        /// `u == null` 的调用点（例如灵魂石那种「没有来源」的广播），而它们**仍然跑棋盘那几段**
        /// （`Enchantments.HasAny(null)` ⇒ false、`FindSlot(null)` ⇒ -1）。
        /// 若把「null 单位」当成「手牌路」而跳过那几段，就会**悄悄改掉**这些调用点的行为
        /// ⇒ ⛔ 别把 `IsBoardPath` 实现成 `_unit != null`。</para>
        /// </summary>
        public static TriggerCarrier From(UnitState u, int owner, int slot)
        {
            if (u == null) return new TriggerCarrier(null, null, "?", owner, slot, true);
            return new TriggerCarrier(u.Instance, u, u.Name, owner, slot, true);
        }

        /// <summary>
        /// **手牌载体**（新路；⛔ 本笔还没有调用点）。
        /// <para>`slot` 恒 `-1`；`Unit` 恒 `null`（手牌卡没有棋盘态）。
        /// ⚠️ 入口那道「这张卡真的在手吗」的闸**不在这里** —— 那是
        /// `IsRightContext(context 10)` 的等价物（`ActionPump.AbilityContextOfAbilityV14 ==
        /// CardDef.AbilityContextHand`），由调用点判（施工单 §5 阶段 3）。</para>
        /// </summary>
        public static TriggerCarrier From(CardInstance inst, int owner)
        {
            if (inst == null) return new TriggerCarrier(null, null, "?", owner, -1, false);
            return new TriggerCarrier(inst, null, inst.Card != null ? inst.Card.Name : "?", owner, -1, false);
        }

        // ---- 只读视图 -----------------------------------------------------------------

        /// <summary>棋盘上的那个单位。**手牌载体恒 `null`** —— 用它前先判 <see cref="IsBoardPath"/>。</summary>
        public UnitState Unit { get { return _unit; } }

        /// <summary>这一份牌（两种载体都有）。</summary>
        public CardInstance Inst { get { return _inst; } }

        /// <summary>卡定义（= `Inst.Card`；无实例时 `null`）。</summary>
        public CardDef Card { get { return _inst != null ? _inst.Card : null; } }

        /// <summary>日志名（棋盘 = 单位名，通常就是卡名；手牌 = 卡名）。</summary>
        public string Name { get { return _name; } }

        /// <summary>归谁（0/1）。</summary>
        public int Owner { get { return _owner; } }

        /// <summary>棋盘格号；手牌 = `-1`。</summary>
        public int Slot { get { return _slot; } }

        /// <summary>稳定实例号（= `CardInstance.Id`；无实例 = 0）。表现层配对 / 日志用。</summary>
        public int InstanceId { get { return _inst != null ? _inst.Id : 0; } }

        /// <summary>
        /// ⭐ **这一趟是不是「棋盘那条路」** —— 唯一的阶段分支开关。
        /// <para>判据：棋盘专属那三段**只吃 `UnitState`**
        /// （附魔 `Enchantments.HasAny/Matching` · 棋盘监听者快照 `SnapshotBoardListeners` 的预调/排除 ·
        ///  格号与 `pinnedOwner`）⇒ 手牌路必须跳过。**别用别的条件代替它。**</para>
        /// <para>⚠️ 由**工厂**决定（`From(UnitState)` ⇒ true / `From(CardInstance)` ⇒ false），
        /// ⛔ **不是** `Unit != null`（见 `From(UnitState)` 的 null 说明）。</para>
        /// </summary>
        public bool IsBoardPath { get { return _isBoardPath; } }

        /// <summary>`true` = 手牌/牌库/弃牌堆那一份（**没有棋盘态**）。</summary>
        public bool IsHandSide { get { return !_isBoardPath; } }

        // ---- 三个转发（⛔ 别在别处重写「原生 or 挂上去」的取法）------------------------

        /// <summary>
        /// 这个关键词**能结算的正文**：棋盘 = <see cref="UnitState.FxOps"/>（原生优先、其次运行时挂的）；
        /// 手牌 = `Card.TriggerOps(keyword)`（**只有原生那一份** —— 手牌卡今天没有 `GrantOps` 通道，
        /// 见施工单 §7·3 第 2 条）。
        /// </summary>
        public IReadOnlyList<EffectOp> FxOps(string keyword)
        {
            if (_unit != null) return _unit.FxOps(keyword);
            var c = Card;
            return c != null ? c.TriggerOps(keyword) : null;
        }

        /// <summary>封闭文法那条触发效果（<see cref="UnitState.Effect"/> 的转发）。</summary>
        public EffectSpec Effect(string keyword)
        {
            if (_unit != null) return _unit.Effect(keyword);
            var c = Card;
            return c != null ? c.Effect(keyword) : null;
        }

        /// <summary>
        /// 运行时挂上去的那份正文原文（`Give "…" to a troop`）。
        /// 手牌载体恒 `null`（`CardInstance` 没有 `GrantOps` 通道）。
        /// </summary>
        public string GrantedText(string keyword)
        {
            return _unit != null ? _unit.GrantedText(keyword) : null;
        }

        /// <summary>这个关键词**有没有任何**可结算的东西（原生正文 / 挂上去的正文）。
        /// 与 <see cref="UnitState.HasFx"/> 同义；手牌侧 = 只有原生。</summary>
        public bool HasFx(string keyword)
        {
            if (_unit != null) return _unit.HasFx(keyword);
            var c = Card;
            return c != null && c.TriggerOps(keyword) != null;
        }

        public override string ToString()
        {
            return (_isBoardPath ? "棋盘:" : "手牌:") + (_name ?? "?")
                 + "#" + InstanceId + "@" + _owner + (Slot >= 0 ? "/" + Slot : "");
        }
    }
}

// ══════════════════════════════════════════════════════════════════════════════════════════
//  §接线备忘（第 ❷ 步，给 `RuleCore.cs` 的写手 —— ⛔ 本笔不动那个文件）
// ══════════════════════════════════════════════════════════════════════════════════════════
//  ① 现有重载变一行转发（调用点 100+ 处一个字都不用改）：
//       static bool FireTriggerAt(BattleContext ctx, UnitState u, string keyword,
//                                 int owner, int slot, …)
//           => FireTriggerAt(ctx, TriggerCarrier.From(u, owner, slot), keyword, owner, slot, …);
//     ⚠️ ⛔ 别用 `with { Slot = … }`（C# 10；Unity 6000.3 只到 C# 9）⇒ 走上面那个三参工厂。
//  ② 原体搬到新重载，逐处替换（**只有这几处**，因为核心只用了这 6 个成员）：
//       `u.Card`        → `c.Card`
//       `u.Name`        → `c.Name`
//       `u.InstanceId`  → `c.InstanceId`
//       `u.FxOps(k)`    → `c.FxOps(k)`
//       `u.Effect(k)`   → `c.Effect(k)`
//       `u.GrantedText(k)` → `c.GrantedText(k)`
//  ③ 棋盘专属三段加闸（**这是唯一的行为分支**；`u` 换成 `c.Unit`）：
//       `Enchantments.HasAny(u)` / `Enchantments.Matching(u, kw)`  → 仅在 `c.IsBoardPath` 时
//       `SnapshotBoardListeners(ctx, preCalled, preCalled2, …)`    → 仅在 `c.IsBoardPath` 时走「预调/排除」那半
//       `pinnedOwner` / 格号那一族                                       → 仅在 `c.IsBoardPath` 时有意义
//  ④ 放行条件（**必须**）：`bash 工具/ruleprobe.sh play check` 的指纹**逐字节相同**。
//     变了就是映射错了 —— 先查 ③ 里有没有把 `u == null` 的调用点误当手牌路（见 `From(UnitState)` 的 doc）。
//  ⑤ ⛔ `EmitCardTrigger` 的 `instanceId` 现在是 `unit != null ? unit.InstanceId : 0`；
//     手牌路要传 `c.InstanceId`（否则手牌卡恒 0，表现层配对会串）—— 那是
//     `EffectResolver` 的文件，⛔ 不在本笔范围。
