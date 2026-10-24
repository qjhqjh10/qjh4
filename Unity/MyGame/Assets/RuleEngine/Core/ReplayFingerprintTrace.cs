// ReplayFingerprintTrace.cs — 回放「逐动作【全量状态】指纹」的记账 / 对账器（引擎侧那一半）
//
// =====================================================================================
//  为什么要这个东西（真机实据）
// =====================================================================================
//  2026-10-24 真机：`[Replay] 演完了：8 回合 · 终局指纹 -1912646073 · 轨迹摘要 1187623750
//  （录制时 1187623750）≠ 录制时的终局 -1762953645 ⇒ 不是同一局`。
//  🔴 **动作轨迹逐条相同、终局却不同** ⇒ 不是「录像缺动作」，是**重演本身分叉**（或开局条件不同源）。
//
//  为什么现有的「分叉点」定位抓不到它：`BattleDriver.PlayReplay` 比的是 `DeepHash`
//  （`BattleDriver.cs` 的 `_replayDivergence` / `rec.trace`），而**终局**比的是
//  `NetProtocol.StateHash`。**两者口径不同**，`StateHash` 覆盖而 `DeepHash` 不覆盖的维度
//  （信仰 / 灵魂石 / 任务点 / 已出隐秘 / 疲劳 / 能量上限 / 费用修正 / 手牌效果记录 / 附魔计数 /
//    残骸与 `Exhausted` / 弃牌堆身份 …，逐维清单一 → `NetProtocol.PlayerHash` 的 doc）
//  一旦是「两边真正不一样的那一格」，就会出现**现在这种「轨迹全同、终局不同」**的形状。
//
//  ⇒ 本类把对账改成**每一动作后取一次【同一个】全量哈希**（= 终局那一个口径），
//  第一条不等就**出声并记下动作序号**；同时把「开局那一刻的全量哈希」也记一笔
//  （区分「开局条件不同源」与「中途分叉」这两种完全不同的病）。
//
// =====================================================================================
//  哈希从哪来（⛔ 别在本类里另算一份）
// =====================================================================================
//  全量状态哈希**只有一份实现** = `CardPresentation/Net/NetProtocol.StateHash(BattleContext)`
//  （`RuleEngine` 不依赖表现层，所以这里**不引用它**，由调用方把采样函数传进来）。
//  🔴 **录制端与回放端传进来的必须是同一个函数** —— 否则这一层对账就是自欺。
//  ⛔ **不许用 `string.GetHashCode()`** 顶替（.NET 的字符串哈希按进程随机化 ⇒ 跨进程必然全红；
//     判据 → `CLAUDE.md` 铁律 / `NetProtocol.Hash` 的 doc）。
//
// =====================================================================================
//  两个角色（同一个类，两种用法）
// =====================================================================================
//  录制端（活局）：
//      var t = ReplayFingerprintTrace.ForRecording();
//      t.RecordStart(StateHash(ctx));           // **开局那一刻**（= `RecBegin` 之后、第一条动作之前）
//      … 每执行完一条动作 …
//      t.RecordAfterAction(StateHash(ctx));
//      存盘：`record.startFullHash = t.StartHash; record.traceFull = t.ToArray();`
//
//  回放端：
//      var t = ReplayFingerprintTrace.ForReplay(rec.startFullHash, rec.traceFull);
//      t.CheckStart(StateHash(ctx));            // 先判「开局是不是同一局」
//      每执行完第 i 条录像动作：
//      t.Observe(i, StateHash(ctx), m.kind + "/" + m.actor);
//      … 循环结束后 …
//      t.Finish(t.ObservedCount);               // 条数对不对（少演 / 多演都要出声）
//      // 🔴 2026-10-24（执行代理 `W24` · `D2` §12·1）就地订正（铁律 5）：本行原来写
//      //   `t.Finish(rec.actions.Count)` —— **那是录制侧那一份**，与 `Finish` 里的 `_expected.Count`
//      //   （同样来自录制）**同源** ⇒ 永远 0 mismatch。必须传 `ObservedCount`（`Observe` 被调几次）。
//      失败位（自检直接断它）：`t.FirstDivergentIndex`（`-1` = 没分叉；`-2` = 开局就不同源）
//
//  ⚠️ 本类**只记账、只出声，不改引擎状态**；⛔ 它不是第二个 `StateHash`，也不是第二个录像格式。
//  ⚠️ `traceFull` 与既有的 `trace`（`DeepHash`）**并存、分工不同**：
//      · `trace`（DeepHash）= 细粒度、含实例号，用来定位「第几条」；**保留不动**；
//      · `traceFull`（StateHash）= 与终局判据同口径，用来回答「到底哪一格不一样」。
//    ⛔ 别把 `trace` 换成 `traceFull` —— `Editor/BattleScene.cs` 第 21·1 节有一个字面量锚点钉着它。
//
// 出处：真机日志 → `资料/普查产出_1024d/真机验收记录_1024d.md` F7；口径 → `NetProtocol.StateHash`。

using System;
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>
    /// 录像的**逐动作全量状态指纹**轨迹：录制端收集、回放端逐条对账。
    /// 语义与用法 → 文件头（⛔ 别在这里再抄一份）。
    /// </summary>
    public sealed class ReplayFingerprintTrace
    {
        /// <summary>`FirstDivergentIndex` 的「开局就不同源」档（不是动作序号 —— 动作序号从 0 起）。</summary>
        public const int StartDiverged = -2;
        /// <summary>`FirstDivergentIndex` 的「没有分叉」档。</summary>
        public const int NoDivergence = -1;

        readonly List<int> _samples = new List<int>();       // 录制端：收集
        IReadOnlyList<int> _expected;                        // 回放端：录制时那一份

        /// <summary>录制端：开局那一刻的全量状态哈希（`false` = 还没记）。</summary>
        public bool HasStartHash { get; private set; }
        /// <summary>录制端收集到的 / 回放端拿来对照的开局全量状态哈希。</summary>
        public int StartHash { get; private set; }

        /// <summary>录制端已经记了几条动作后样本（= 回放端该有这么多条）。</summary>
        public int SampleCount { get { return _expected != null ? _expected.Count : _samples.Count; } }

        /// <summary>
        /// 🔴 **第一条对不上的位置**：`-1` = 没分叉 · `-2` = **开局就不同源** · `>= 0` = 第几条动作之后分叉。
        /// 自检/验收直接断这一格（与 `BattleDriver._replayDivergence` 同一个用法）。
        /// </summary>
        public int FirstDivergentIndex { get; private set; } = NoDivergence;

        /// <summary>分叉点那两格（诊断用；没有分叉时都是 0）。</summary>
        public int DivergentExpected { get; private set; }
        /// <summary>见 <see cref="DivergentExpected"/>。</summary>
        public int DivergentActual { get; private set; }

        /// <summary>`true` = 这一份录像**没有**逐动作全量指纹（老档 / 录制端没接）——
        /// ⚠️ 这**不是「通过」**：本类会在 `Finish` 里出声说明「这条对账没做」。</summary>
        public bool NoExpectedTrace { get; private set; }

        /// <summary>回放端：动作条数与录制时不一致（`0` = 一致；否则 = 实际演了几条）。</summary>
        public int ActionCountMismatch { get; private set; }
        /// <summary>见 <see cref="ActionCountMismatch"/>。</summary>
        public int ExpectedActionCount { get; private set; } = -1;

        /// <summary>🔴 **回放端：`Observe` 被调了几次** = 「这一次**真演了几条**动作」。
        /// <para>2026-10-24（执行代理 `W24` · `D2_BattleScene11红诊断.md` §12·1）：**这个数以前没有**，
        /// `Finish` 比的是调用方传进来的 `rec.actions.Count`（**录制侧那一份**）⇒
        /// 「录制 2 条 · 演了 1 条」永远报 `0 mismatch`（**自己跟自己比 = 空判**）。
        /// ⇒ 现在 `BattleDriver.FinishReplay` 传**这一格**，`Finish` 拿它比 `<see cref="ExpectedActionCount"/>`。
        /// ⚠️ 它在 `Observe` **最开头**自增（连老档 / 越界那两档也算）—— 它量的是「落地了几条」，
        /// 不是「对了几条」。</para></summary>
        public int ObservedCount { get; private set; }

        ReplayFingerprintTrace() { }

        /// <summary>录制端：活局里收集。</summary>
        public static ReplayFingerprintTrace ForRecording() { return new ReplayFingerprintTrace(); }

        /// <summary>回放端：拿录制时那两格来对账。
        /// <paramref name="samples"/> 为 `null` / 空 ⇒ 标记 <see cref="NoExpectedTrace"/>（旧档）。</summary>
        public static ReplayFingerprintTrace ForReplay(int startHash, IReadOnlyList<int> samples,
                                                       bool hasStartHash = true)
        {
            var t = new ReplayFingerprintTrace();
            t.HasStartHash = hasStartHash;
            t.StartHash = startHash;
            if (samples == null || samples.Count == 0) t.NoExpectedTrace = true;
            else t._expected = samples;
            return t;
        }

        // ------------------------------------------------------------------
        //  录制端
        // ------------------------------------------------------------------

        /// <summary>录制端：记开局那一刻的全量状态哈希（`NewBattle` + 换牌落地之后、第一条动作之前）。</summary>
        public void RecordStart(int startHash)
        {
            if (_expected != null) return;          // 回放角色不收样本（⛔ 别两边混用）
            HasStartHash = true;
            StartHash = startHash;
        }

        /// <summary>录制端：每执行完一条动作记一次全量状态哈希。</summary>
        public void RecordAfterAction(int hash)
        {
            if (_expected != null) return;          // 同上
            _samples.Add(hash);
        }

        /// <summary>录制端：落盘用（`int[]`，可直接进 `JsonUtility` 的字段）。</summary>
        public int[] ToArray() { return _samples.ToArray(); }

        // ------------------------------------------------------------------
        //  回放端
        // ------------------------------------------------------------------

        /// <summary>
        /// 回放端：**先判开局**。返回 `false` = 开局条件就不同源（⇒ 后面的逐条对账意义有限，
        /// 但仍然会跑 —— 那是两条独立的病，要分开看）。
        /// </summary>
        public bool CheckStart(int actual, BattleContext ctx = null)
        {
            if (!HasStartHash) return true;         // 旧档没记 ⇒ 这一格不判（`Finish` 会说明）
            if (actual == StartHash) return true;
            if (FirstDivergentIndex == NoDivergence)
            {
                FirstDivergentIndex = StartDiverged;
                DivergentExpected = StartHash;
                DivergentActual = actual;
            }
            Speak($"[ReplayTrace] 🔴 **开局就不是同一局**：录制时开局全量指纹 {StartHash}，"
                + $"回放重建出来是 {actual} ⇒ 起始条件不同源（种子 / 双方卡组 / 先手 / 换牌结果 / 环境卡）。"
                + "判据 = `NetProtocol.StateHash`（与终局同一个口径）—— 这一格与「中途分叉」是两种病，别混。", ctx);
            return false;
        }

        /// <summary>
        /// 回放端：**每执行完一条录像动作**调一次。
        /// 返回 `false` = 这一条之后状态对不上（第一次就置 <see cref="FirstDivergentIndex"/> 并出声；
        /// 后续条仍然照跑，但只记第 <paramref name="actionIndex"/> 条那一格 —— 第一条才是根因）。
        /// </summary>
        /// <param name="actionIndex">录像里的动作下标（从 0 起）。</param>
        /// <param name="actual">这一刻的全量状态哈希（录制端同一个函数）。</param>
        /// <param name="actionDesc">给人看的一条动作描述（kind / actor / slot …），可空。</param>
        public bool Observe(int actionIndex, int actual, string actionDesc = null, BattleContext ctx = null)
        {
            // 「这一条动作真的落地了」的计数 —— 放在**所有早退之前**（老档 / 越界那两档也要算，
            // 否则 `Finish` 的条数对账会低估）。见 `ObservedCount` 的 doc。
            ObservedCount++;
            if (_expected == null)
            {
                // 旧档：没有逐动作全量指纹 ⇒ 这一层判不了。⛔ 不许当「通过」—— 由 `Finish` 出声。
                return true;
            }
            if (actionIndex < 0 || actionIndex >= _expected.Count)
            {
                // 演得比录的多：条数问题在 `Finish` 里统一报；这里不重复刷屏。
                return true;
            }
            int want = _expected[actionIndex];
            if (actual == want) return true;
            if (FirstDivergentIndex == NoDivergence)
            {
                FirstDivergentIndex = actionIndex;
                DivergentExpected = want;
                DivergentActual = actual;
            }
            Speak($"[ReplayTrace] 🔴 **全量指纹分叉 = 第 {actionIndex} 条动作之后**"
                + (string.IsNullOrEmpty(actionDesc) ? "" : $"（{actionDesc}）")
                + $"：录制时 {want}，现在 {actual}。"
                + "这一条口径 = 终局那一个 `NetProtocol.StateHash`（比 `DeepHash` 多覆盖信仰/资源/疲劳/"
                + "手牌效果记录/附魔/费用修正等维度）⇒ **差在这一格就是「轨迹全同、终局不同」的真凶**，"
                + "去比同一时刻的 `StateBrief` 与 `NetProtocol.StateHash` 的维度清单。", ctx);
            return false;
        }

        /// <summary>
        /// 回放端：动作循环跑完之后调一次 —— **条数对账**（少演 / 多演都出声）。
        /// <paramref name="actualActions"/> = 这次**真演了几条**。
        /// <para>🔴 **2026-10-24（执行代理 `W24` · `D2` §12·1）**：这个实参**必须**传
        /// <see cref="ObservedCount"/>（= `Observe` 被调了几次）。原来调用方传的是 `rec.actions.Count`
        /// （**录制侧那一份**）⇒ 与 <see cref="ExpectedActionCount"/>（也来自录制侧 `traceFull`）
        /// **同源** ⇒ 除非录像本身畸形，这一层**永远报 0 mismatch**（「这次真演了几条」从来没被测过）。</para>
        /// </summary>
        public void Finish(int actualActions, BattleContext ctx = null)
        {
            if (_expected != null)
            {
                ExpectedActionCount = _expected.Count;
                if (actualActions != _expected.Count)
                {
                    ActionCountMismatch = actualActions;
                    Speak($"[ReplayTrace] 🔴 **动作条数不一致**：录制时逐动作指纹有 {_expected.Count} 条，"
                        + $"这次演了 {actualActions} 条 ⇒ "
                        + (actualActions < _expected.Count ? "**少演了**（录像读漏 / 中途 break）"
                                                          : "**多演了**（重复落地 / 循环没停）")
                        + "。⛔ 别把这一格当「哈希碰巧对上了」就放过。", ctx);
                }
            }
            if (NoExpectedTrace)
            {
                Speak("[ReplayTrace] ⚠️ 这份录像**没有「逐动作全量指纹」**（老档 / 录制端没接这一列）"
                    + " ⇒ 本层对账**没做**（⛔ **不是通过**）。终局那一格仍然按 `StateHash` 判；"
                    + $"要能定位到「第几条」请用本版之后录的档（字段 `traceFull`）。", ctx);
            }
        }

        /// <summary>一句话总结（诊断 / 自检文案用）。</summary>
        public string Summary()
        {
            if (FirstDivergentIndex == StartDiverged)
                return $"回放全量指纹：**开局不同源**（录 {DivergentExpected} / 现 {DivergentActual}）";
            if (FirstDivergentIndex >= 0)
                return $"回放全量指纹：**第 {FirstDivergentIndex} 条动作之后分叉**"
                     + $"（录 {DivergentExpected} / 现 {DivergentActual}）";
            if (ActionCountMismatch != 0)
                return $"回放全量指纹：条数不一致（录 {ExpectedActionCount} / 演 {ActionCountMismatch}）";
            if (NoExpectedTrace) return "回放全量指纹：**没有这一列**（老档 ⇒ 本层没判）";
            return "回放全量指纹：逐条一致";
        }

        static void Speak(string msg, BattleContext ctx)
        {
            UnityEngine.Debug.LogError(msg);     // 红线：不许静默失败 —— 分叉一律 LogError
            if (ctx != null) ctx.Log(msg);
        }
    }
}
