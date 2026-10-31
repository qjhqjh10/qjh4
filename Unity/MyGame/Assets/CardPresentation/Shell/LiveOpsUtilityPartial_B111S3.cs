// LiveOpsUtilityPartial_B111S3.cs —— 执行代理 `B111_S3`（2026-11-01）· 账 `B-68`
//
// ============================ 这条活是什么 ============================
//  原版 `LiveOpsUtility`（`cpp2il/out_cpp2il_dev_ilspy/LiveOpsUtility.cs`，静态类）有四个成员：
//    `IsTriggeredEvent(ILiveOps)` · `IsTimedEvent(ILiveOps, out long)` ·
//    `GetDateForEventStage(ILiveOps, int)` · `GetDateForNextEventStage(ILiveOps)`
//  —— **同一个类**。
//  而本仓把它们拆到了两个家：
//    · `IsTimedEvent` → `Shell/LiveOpsUtilityPartial_B110W4.cs` 的 `LiveOpsUtility`（✅ 类名对）
//    · `IsTriggeredEvent` → `Shell/LiveOpsManager.cs:593` 的 `LiveOpsManager`（❌ 类名不对）
//  ⇒ 下一个人按 `LiveOpsUtility.` 找**只找得到一半**（`B110_W4` 报告 §六.1 提的）。
//
// ============================ 选了哪条路 / 为什么不选另一条 ============================
//  ✅ **做法 ①：做一个真正的 `LiveOpsUtility` 门面（两个成员都转发过去）**。
//     `IsTimedEvent` 本来就在 `LiveOpsUtility` 上（真身，不转发）；
//     这里只把缺的那一个 `IsTriggeredEvent` 转发到 `LiveOpsManager.IsTriggeredEvent`（**唯一实现**，不复制判断）。
//  ⛔ **做法 ②（去掉 `LiveOpsManager` 的 `sealed`、改 `partial` 再合并）被否**，理由三条：
//     ① `LiveOpsManager` 是 `public sealed class`（`Shell/LiveOpsManager.cs:74`）—— `sealed` 是**有意**的：
//        它是 `SingletonBehaviour` 那套的等价单例（文件头差异 ② 写着），去掉修饰符会**打开外部继承**这个口子，
//        而全仓**没有一处**需要继承它；
//     ② 它**已经被按 `文件:行号` 引用 36 个不同行号 / 39 条**（`账/*.tsv` 现读）⇒ 把 `IsTriggeredEvent` 从它身上搬走
//        会让那些落点里指向 `:593` 的、以及所有 `LiveOpsManager.IsTriggeredEvent` 调用点一起动，
//        收益只是「类名好看」，风险是**一次大规模行号漂移**（铁律 18·b 的同类代价）；
//     ③ 门面转发**不引入第二份判断**（`return LiveOpsManager.IsTriggeredEvent(liveOps);` 只有一句），
//        与原版「一个静态工具类」的调用形状也一致（`LiveOpsUtility.IsTriggeredEvent(ev)`）。
//
// ============================ 判据 ============================
//  原版 `LiveOpsUtility.IsTriggeredEvent` 的真身已经在 `Shell/LiveOpsManager.cs:593` 逐句对过
//  （`decomp_full/LiveOpsUtility__IsTriggeredEvent.c`：`return ev.GetBaseData().filter.Triggers.Count > 0;`）
//  ⇒ 本文件**不重写那一句**，只转发。原版成员表 → `cpp2il/out_cpp2il_dev_ilspy/LiveOpsUtility.cs`
//  （`:50 IsTriggeredEvent` / `:187 GetDateForEventStage` / `:237 GetDateForNextEventStage`）。

namespace CardPresentation
{
    public static partial class LiveOpsUtility
    {
        // ⚠️ 新成员追加在类尾（本文件是独立 partial 文件 ⇒ 对 `Shell/LiveOpsUtilityPartial_B110W4.cs` 零影响）。

        /// <summary>原版 `LiveOpsUtility.IsTriggeredEvent(ILiveOps liveOps)`（`decomp_full/LiveOpsUtility__IsTriggeredEvent.c`）。
        /// <para>**转发**到 <see cref="LiveOpsManager.IsTriggeredEvent"/>（那一处是唯一实现，判据与逐句说明写在它的 doc 上）。
        /// 保留同一个类名 = 下一个人按 `LiveOpsUtility.` 能一次找到**两个**成员（`IsTimedEvent` + 本方法）。</para>
        /// <para>⚠️ 原版两处都是 `static` 且挂同一个类；我们**不搬实现**（36 个账落点指着 `LiveOpsManager.cs`，见文件头）。</para></summary>
        public static bool IsTriggeredEvent(ILiveOps liveOps)
        {
            return LiveOpsManager.IsTriggeredEvent(liveOps);
        }
    }
}
