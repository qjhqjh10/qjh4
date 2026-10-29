// EnergySpent.cs — 原版 `EnergySpent : MissionChallenge`（「花掉 N 点能量」这类任务挑战的**消费方**）
// 🆕 2026-10-29 · 执行代理 `B98_A11` · 账 `战斗逻辑驱动:1373`（`EnergySpentSignal.Raise()`）
//   · 判据总表 → `资料/完全复刻/判据/B98_R7_消费方两条.md` §一·1373（本文件只做它派下来的 A2′ 那一块）
//
// ============================ 判据（逐条现读 2026-10-29）============================
// ① 消费者本体（`cpp2il/out_il2csharp/Assembly-CSharp/EnergySpent.cs:8-45` + `.ctor`）：
//    <code>
//    [Serializable] class EnergySpent : MissionChallenge
//    public CostCondition costCondition;   // +0x58
//    public EnergyCostType costType;       // +0x60
//    .ctor() { costCondition = new CostCondition(); costType = EnergyCostType.CardCost; }   // ★ 默认值 = CardCost
//    </code>
// ② handler 真身 = `decomp_full/EnergySpent__OnEnergySpent.c`（**全文 33 行**，🥇；`+0x58/+0x60/+0x10/+0x20/+0x24`
//    五个偏移与 `dump.cs` 的字段表逐个对得上）—— **四道闸，顺序不能换**：
//    <code>
//    if (obj == null) throw;                                                  // 原版 NRE
//    ① if (!obj.IsPlayer(+0x10)) return;                                       // ★ 只看本地玩家花的
//    ② if (!Compare(costCondition.ComparisonMethod(+0x10),                     // ★ 金额比较
//                   obj.Cost(+0x20), costCondition.Cost(+0x14))) return;
//    ③ if (!ContainsFlag&lt;EnergyCostType&gt;(costType(+0x60), obj.Type(+0x24))) return;  // ★ 类型位
//    ④ UpdateProgress(obj.Cost, shouldOverride: false);                        // ★ 累加**金额**，不是次数
//    </code>
// ③ 三个配套件（都从 `dump.cs` / `out_il2csharp` 逐字抄，⛔ 别按语义猜）：
//    · `ChallengeComparison`（`dump.cs:59865` 一带）= `EqualTo=0 / HigherOrEqual=10 / LowerOrEqual=20`
//      —— ⚠️ **不是 0/1/2**，`Compare` 的判定树见 <see cref="ChallengeComparisonExtensions"/>。
//    · `EnergyCostType`（`dump.cs:59319` 带 `[Flags]`）= `CardCost=1 / AbilityCost=2`。
//    · `CostCondition`（`dump.cs:58916`）= 两个字段 `ComparisonMethod(+0x10)` / `Cost(+0x14)`。
// ④ 信号生产者 = `BattleManager.PayCardCost`（`decomp_full/BattleManager__PayCardCost.c:28-31`，Type=CardCost）
//    与 `PayActiveAbilityCostOath`（Type=AbilityCost）。全库**只有一个消费者** = 本类。
//    ⚠️ 全库 `EnergySpentSignal___ctor` **只有这两个命中** ⇒ 维护费/其它扣能量路**原版就不发**这条信号
//    （我们 `RuleCore.SpendNormalMana` 的 `energySpentSignal` 开关也照这个口径，见 `RuleCore.cs:1833`）。
// ⑤ `ContainsFlag` 的**真语义已按机器码定死**：`SolidUtilities.EnumExtensions.ContainsFlag&lt;Int32Enum&gt;`
//    （VA `0x180C8A1F0`）的体是 `66 90 / 85 CA / 0F 97 C0 / C3` = `test edx,ecx; setne al; ret`
//    ⇒ **`(thisEnum &amp; flag) != 0`**（⛔ 不是 `(a&amp;b)==b`）。
//
// ============================ 与我们的架构的对接 ============================
// · `Signal` = `Shell/LiveOpsFilter.cs` 的 `LiveOpsSignal`（同 `TroopsKilledCount` 的处置）。
// · 信号载荷：原版四格 `IsPlayer(+0x10) / Card(+0x18) / Cost(+0x20) / Type(+0x24)`，`Card` 是 `CardScript`
//   ⇒ 我们收窄成三格（去掉 `Card`）。**消费者一格都没读它**（`.c` 全文没读过 `+0x18`）⇒ 结构不同、语义相同。
// · 🔴 **喂入点不在这里**：本类只负责「收到信号 ⇒ 过四道闸 ⇒ 累加金额」。谁发信号 =
//   `CardPresentation/Battle/BattleDriver.cs` 的结算拍（报告 §B4 给了可粘贴指引）。本批**没有**接。
// · 🔴 **必须按类型分档喂**：我们的 `AchievementTally` 把能量按类型分了两格
//   （`EnergySpentOf(seat, 1/2)`，见 `RuleEngine/Core/AchievementTally.cs:428`）⇒ 喂入端要按类型各发一条，
//   **别把合计塞进一条信号**（否则「只数 CardCost」的挑战会拿到合计 = 静默多算）。
//   ⛔ 别在 `RuleEngine/Core/RuleCore.RecordEnergySpent` 里加 `ctx.Log` —— 那会改 `ctx.Events.Count`、动 `play check` 指纹。
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `EnergyCostType`（`dump.cs:59319`，**带 `[Flags]`**）：`CardCost = 1` / `AbilityCost = 2`。
    /// <para>⚠️ 我们的引擎侧同值常量在 `RuleEngine/Core/AchievementTally.cs:376/379`（`EnergyCostCard = 1` /
    /// `EnergyCostAbility = 2`），那边是 `int`（⛔ 不是这个枚举，别指望两处能直接互换 —— 喂入端显式转一次）。</para></summary>
    [Flags]
    public enum EnergyCostType
    {
        CardCost = 1,
        AbilityCost = 2,
    }

    /// <summary>原版 `ChallengeComparison`（`out_il2csharp/Assembly-CSharp/ChallengeComparison.cs`）：
    /// ⚠️ **`EqualTo=0 / HigherOrEqual=10 / LowerOrEqual=20`** —— 不是 0/1/2（照 0/1/2 写会静默判错全部挑战）。</summary>
    public enum ChallengeComparison
    {
        EqualTo = 0,
        HigherOrEqual = 10,
        LowerOrEqual = 20,
    }

    /// <summary>原版 `ChallengeComparison` 的判定树（`out_il2csharp/ChallengeComparisonExtensions.cs`，**全文四支**）：
    /// <code>
    /// EqualTo       ⇒ a == b
    /// HigherOrEqual ⇒ a &gt;= b
    /// LowerOrEqual  ⇒ a &lt;= b
    /// 其它值        ⇒ false          // ★ 原版兜底是 false，⛔ 别抛、别当 EqualTo
    /// </code></summary>
    public static class ChallengeComparisonExtensions
    {
        public static bool Compare(ChallengeComparison comparison, int a, int b)
        {
            switch (comparison)
            {
                case ChallengeComparison.EqualTo: return a == b;
                case ChallengeComparison.HigherOrEqual: return a >= b;
                case ChallengeComparison.LowerOrEqual: return a <= b;
                default: return false;                    // 原版 `if` 链走到最后就是 `return false`
            }
        }
    }

    /// <summary>原版 `CostCondition`（`dump.cs:58916`，两个字段逐个抄）：
    /// `ComparisonMethod(+0x10)` / `Cost(+0x14)`。⚠️ 它是 **class**（引用类型）不是 struct —— 原版
    /// `EnergySpent..ctor` 里 `new CostCondition()` 再整体赋给字段（`.c` 那两行 `costCondition1/this.costCondition`）。</summary>
    [Serializable]
    public class CostCondition
    {
        public ChallengeComparison ComparisonMethod;
        public int Cost;
    }

    /// <summary>原版 `EnergySpentSignal : IBattleSignal, ISignal`（`out_il2csharp/EnergySpentSignal.cs`）：
    /// 四格 `IsPlayer(+0x10) / Card(+0x18) / Cost(+0x20) / Type(+0x24)`。
    /// <para>⚠️ 去掉 `Card`（`CardScript` 在我们这一层拿不到），**唯一消费者一格都没读它** —— 见文件头「与我们的架构的对接」。</para></summary>
    public class EnergySpentSignal : ILiveOpsSignal
    {
        /// <summary>原版 `+0x10`：这一笔是**本地玩家**花的吗。</summary>
        public readonly bool IsPlayer;
        /// <summary>原版 `+0x20`：**实付**金额（不是卡面费用 —— 原版在 `UseMana` 之后按实付发）。</summary>
        public readonly int Cost;
        /// <summary>原版 `+0x24`：`CardCost(1)` / `AbilityCost(2)`（单值；`[Flags]` 但信号只发一个）。</summary>
        public readonly EnergyCostType Type;

        public EnergySpentSignal(bool isPlayer, int cost, EnergyCostType type)
        {
            IsPlayer = isPlayer;
            Cost = cost;
            Type = type;
        }
    }

    /// <summary>原版 `EnergySpent`（「花掉 N 点能量」这类挑战）。行为只有三跳 + 四道闸，见文件头判据 ①②。</summary>
    [Serializable]
    public class EnergySpent : MissionChallenge
    {
        /// <summary>原版 `public CostCondition costCondition; // 0x58`。原版 `.ctor` 里 `new CostCondition()` ⇒
        /// 默认 `ComparisonMethod = EqualTo(0)` / `Cost = 0`（**默认实例什么都数不到**，这是原版默认值，⛔ 别改成「更好用」的默认）。</summary>
        public CostCondition costCondition = new CostCondition();

        /// <summary>原版 `public EnergyCostType costType; // 0x60`，`.ctor` 里给 **`CardCost`**（逐字，`.c` 末段 `0x69C650`）。</summary>
        public EnergyCostType costType = EnergyCostType.CardCost;

        /// <summary>原版 `protected override void OnInitialize()`（Slot 9）= `Signal.Register&lt;EnergySpentSignal&gt;(OnEnergySpent)`。</summary>
        protected override void OnInitialize()
        {
            LiveOpsSignal.Register<EnergySpentSignal>(OnEnergySpent);
        }

        /// <summary>原版 `protected override void OnReset()`（Slot 8）= `Signal.Unregister&lt;EnergySpentSignal&gt;(OnEnergySpent)`。
        /// ⚠️ 非 `override` 的理由与 `TroopsKilledCount.OnReset` 完全一样（基类 `MissionChallenge` 没有 `OnReset` 抽象位）。</summary>
        public void OnReset()
        {
            LiveOpsSignal.Unregister<EnergySpentSignal>(OnEnergySpent);
        }

        /// <summary>原版 `private void OnEnergySpent(EnergySpentSignal obj)` —— 四道闸逐句见文件头判据 ②。
        /// <para>⚠️ 原版 `obj == null` 那一跳是 NRE（`.c:12-13` 两道空闸后 `FUN_1803f47a0()`）；我们出声 + 不算（全工程口径）。
        /// ⚠️ 原版 `costCondition == null` 也是 NRE（`.c:14-15`）；我们出声 + 不算。</para></summary>
        public void OnEnergySpent(EnergySpentSignal obj)
        {
            if (obj == null)
            {
                Debug.LogError("[EnergySpent] OnEnergySpent：信号是 null（原版这一跳 NRE）⇒ 不推进");
                return;
            }
            if (!obj.IsPlayer) return;                                        // ① `.c:13`
            var cond = costCondition;
            if (cond == null)
            {
                Debug.LogError("[EnergySpent] OnEnergySpent：`costCondition` 是 null（原版这一跳 NRE）⇒ 不推进");
                return;
            }
            if (!ChallengeComparisonExtensions.Compare(cond.ComparisonMethod, obj.Cost, cond.Cost)) return;   // ② `.c:16-18`
            if (!ContainsFlag(costType, obj.Type)) return;                    // ③ `.c:20-23`
            UpdateProgress(obj.Cost, false);                                  // ④ `.c:24` —— 累加**金额**
        }

        /// <summary>`SolidUtilities.EnumExtensions.ContainsFlag&lt;EnergyCostType&gt;(thisEnum, flag)` 的**本地等价**。
        /// 🔴 真语义按机器码定死（VA `0x180C8A1F0`，体 = `test edx,ecx; setne al; ret`）⇒ **`(a &amp; b) != 0`**，
        /// ⛔ **不是** `(a &amp; b) == b`（两者只在 `flag == 0` 时不同；我们的信号只发 1/2，仍按机器码写死以免将来踩）。</summary>
        public static bool ContainsFlag(EnergyCostType mask, EnergyCostType flag)
        {
            return ((int)mask & (int)flag) != 0;
        }
    }
}
