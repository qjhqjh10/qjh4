// MultiplyRewardsFunction.cs — 原版 `MultiplyRewardsFunction : EventFunctionBase`（把一批奖励按倍率放大）
// 🆕 2026-10-28 · 执行代理 `B91_W7` · 账 `日常_奖励` 535（G1 奖励事件层，裁定 1：按原版类型名独立成文件）
//
// ── 判据 ────────────────────────────────────────────────────────────────────
//   · 类形态：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/MultiplyRewardsFunction.cs`
//     （`EventFunctionBase` 派生，字段只有一个 `[SerializeField] float multiplier`）。
//   · `Preview(ILiveOps _, IEnumerable<RewardInfo> rewardInfos)`
//     判据 = `decomp_full/MultiplyRewardsFunction__Preview.c`：**全部内容**
//     = `Enumerable.Select(rewardInfos, <b__3_0>)` ⇒ **逐条**过一遍 `b__3_0`，没有别的分支。
//   · `_003CPreview_003Eb__3_0(RewardInfo r)` 判据 = `decomp_full/MultiplyRewardsFunction___Preview_b__3_0.c`：
//     `new RewardInfo(r.Item, (int)Math.Round(r.Quantity * multiplier, MidpointRounding.AwayFromZero), Basic, null, null)`
//     —— `.c` 里就是「乘倍率 → 四舍五入（负值走 ceil-0.5、正值走 floor+0.5 的远离零版）→ 取整」。
//     ⚠️ 档位回落到 `RewardTier.Basic(0)`、实例 id 传 null —— **原版这一跳就是丢掉原来的 tier/instanceId**，照抄。
//   · `get_CloudScriptFunction` 判据 = `decomp_full/MultiplyRewardsFunction__get_CloudScriptFunction.c`
//     = `return <类静态字符串字段>`。**那个字面量本机读不到**（类静态字段只在运行时才有值，ILSpy 也没给字面量）
//     ⇒ 如实返回 `null` 并出声一次，⛔ 不编一个名字（铁律 2）。
//
// ── 与我们的架构的对接 ─────────────────────────────────────────────────────────
//   原版的基类 `EventFunctionBase` 在本工程**不存在**（`grep` 全仓 0 命中），而「奖励变换函数」这一族
//   在本工程只需要**一个方法**（`Preview`）⇒ 用接口 <see cref="IRewardFunction"/> 顶它的位置
//   （接口不需要脚本资产，也就绕开「类名 == 文件名」那条 Unity 约束）。
//   🔶 自己设计（非原版）：用 `IRewardFunction` 代替 `EventFunctionBase` 继承链 ·
//     为什么必须自己定：`EventFunctionBase` 那一族（`EventScoringFunction` / `GenericEventFunction`）在本工程都没有，
//     判据件只给了它们各自的 `TargetEvent` / `Args` 字段，没有共同契约的完整形状 ·
//     调度台 2026-10-28（裁定 1）定。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `EventFunctionBase`（奖励变换函数家族）在**奖励这一路**需要的最小契约。
    /// <para>⚠️ 原版里它是 `ScriptableObject` 派生、带 `CloudScriptFunction` 名与一段虚调用；本工程只需要
    /// 「给一批奖励，返回一批奖励」这一跳 ⇒ 只搬这一个方法 + 一个只读名。</para>
    /// <para>🔶 自己设计（非原版）：见文件头 · 调度台 2026-10-28 定。</para></summary>
    public interface IRewardFunction
    {
        /// <summary>原版 `string CloudScriptFunction`（派生类各自给自己的云脚本名；本工程无云脚本 ⇒ 多为 null）。</summary>
        string CloudScriptFunction { get; }

        /// <summary>原版 `IEnumerable&lt;RewardInfo&gt; Preview(ILiveOps evt, IEnumerable&lt;RewardInfo&gt; rewards)`。</summary>
        IEnumerable<RewardInfo> Preview(ILiveOps evt, IEnumerable<RewardInfo> rewards);
    }

    /// <summary>原版 `MultiplyRewardsFunction`：把一批奖励的数量**逐条乘倍率**（四舍五入、档位归 `Basic`）。
    /// <para>判据：`MultiplyRewardsFunction__Preview.c`（= 一句 `Select`）＋ `___Preview_b__3_0.c`（乘 + 舍入）。</para></summary>
    [Serializable]
    public class MultiplyRewardsFunction : IRewardFunction
    {
        /// <summary>原版 `[SerializeField] private float multiplier`（唯一字段）。</summary>
        public float multiplier = 1f;

        public MultiplyRewardsFunction() { }
        public MultiplyRewardsFunction(float multiplier) { this.multiplier = multiplier; }

        /// <summary>原版 `get_CloudScriptFunction` = 类静态字符串字段 —— **字面量本机读不到** ⇒ null + 出声一次
        /// （⛔ 不编名字；铁律 2「没查到就说没查到」）。</summary>
        public virtual string CloudScriptFunction
        {
            get
            {
                WarnCloudScriptOnce();
                return null;
            }
        }

        /// <summary>原版 `override IEnumerable&lt;RewardInfo&gt; Preview(ILiveOps _, IEnumerable&lt;RewardInfo&gt; rewardInfos)`
        /// （`MultiplyRewardsFunction__Preview.c` 全文 = `Enumerable.Select(rewardInfos, b__3_0)`）。
        /// ⚠️ 第一个形参在原版里**完全没用**（名字就是 `_`）⇒ 这里也保留但不读。</summary>
        public IEnumerable<RewardInfo> Preview(ILiveOps evt, IEnumerable<RewardInfo> rewards)
        {
            if (rewards == null)
            {
                Debug.LogWarning("[MultiplyRewardsFunction] Preview：奖励表为 null ⇒ 返空表（原版这一档 `Enumerable.Select(null)` 会抛 ArgumentNull）");
                return new RewardInfo[0];
            }
            var outList = new List<RewardInfo>();
            foreach (var r in rewards)
            {
                if (r == null) { outList.Add(null); continue; }   // 原版 `b__3_0(null)` 会 NRE；我们保留 null 占位、不崩
                outList.Add(Scale(r));
            }
            return outList;
        }

        /// <summary>原版 `_003CPreview_003Eb__3_0(RewardInfo r)`（`MultiplyRewardsFunction___Preview_b__3_0.c`）：
        /// `new RewardInfo(r.Item, (int)Math.Round(r.Quantity * multiplier, MidpointRounding.AwayFromZero), Basic, null, null)`。
        /// <para>🔴 三个字面量照抄：档位 → `RewardTier.Basic`(=0)、`instanceId` → null、`ephemeralState` → null。</para></summary>
        protected virtual RewardInfo Scale(RewardInfo r)
        {
            int scaled = (int)Math.Round(r.Quantity * (double)multiplier, MidpointRounding.AwayFromZero);
            // ⚠️ 原版把 tier / instanceId 一起丢掉（新建对象时只传了三个实参）—— 照抄，⛔ 别「顺手保留」。
            return new RewardInfo(r.Id, scaled, 0, null);
        }

        static bool _warnedCloudScript;
        static void WarnCloudScriptOnce()
        {
            if (_warnedCloudScript) return;
            _warnedCloudScript = true;
            Debug.Log("[MultiplyRewardsFunction] `CloudScriptFunction` 名字是原版的**类静态字符串字段**，字面量本机读不到 "
                    + "（`decomp_full/MultiplyRewardsFunction__get_CloudScriptFunction.c` 只给了 `return <static string>`）⇒ 返 null。");
        }

        /// <summary>自检收尾（一次性出声的标志要清，否则跨用例串味）。</summary>
        public static void ResetForTest() { _warnedCloudScript = false; }
    }
}
