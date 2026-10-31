// LiveOpsUtilityPartial_B112B.cs —— 执行代理 `B112_B`（2026-11-01）· 账 `B-82`
//
// ============================ 这条活是什么 ============================
//  原版 `LiveOpsUtility`（`cpp2il/out_il2csharp/Assembly-CSharp/LiveOpsUtility.cs`，**全文 122 行现读**）有**四个**静态成员：
//     `IsTriggeredEvent(ILiveOps)` · `IsTimedEvent(ILiveOps, ref long)` ·
//     `GetDateForEventStage(ILiveOps, int)` · `GetDateForNextEventStage(ILiveOps)`
//  本仓原来只把前两个挂到了 `LiveOpsUtility` 上（`LiveOpsUtilityPartial_B110W4.cs` / `…_B111S3.cs`），
//  后两个 `LiveOpsUtility.` 上**一个都找不到**：
//    · `GetDateForEventStage` → 逻辑在 `Shell/RaidEvent.cs:542` 的 **`RaidHeaderLogic.GetDateForEventStage(LiveOpsEventData, int)`**
//      （签名不同：吃 `LiveOpsEventData` 不是 `ILiveOps`）；
//    · `GetDateForNextEventStage` → 全仓 **0 命中**。
//  ⇒ 本文件把它们补齐到**同一个类名**上，并且**不复制判断**（铁律 6）：
//    第一个**转发**到 `RaidHeaderLogic`（那是唯一实现），第二个在这里实现（此前没有实现）。
//
// ============================ 判据（逐句）============================
//  `LiveOpsUtility.GetDateForEventStage(ILiveOps liveOps, int stageIndex)`（`il2csharp:52-66`）：
//  <code>
//  var data = liveOps.GetBaseData();
//  long v = Enumerable.ElementAtOrDefault(data.dates.eventStages, stageIndex);   // 越界 / 负数 ⇒ 0
//  if (v == 0) v = data.dates.endDate;        // ← `sub_1800021f0(0xf, ILiveOps, liveOps)` = 再取一次 GetBaseData(槽 15)
//  return v;                                   //   然后 +0x28(dates) → +0x18(endDate)
//  </code>
//  ⇒ 与 `RaidHeaderLogic.GetDateForEventStage` **逐项相同**（那边注释里已写明「0 视作没有这一档 ⇒ 落到结束时刻」），
//    唯一差别 = 那边多一层 null 保护（`data == null` ⇒ 0；原版这里 NRE）。
//
//  `LiveOpsUtility.GetDateForNextEventStage(ILiveOps liveOps)`（`il2csharp:68-92` + 两个闭包体 `:107-120`）：
//  <code>
//  currentTime = PlayerDataManager.singletonManager.currentServerTime;        // ← 「现在」（毫秒）
//  return data.dates.eventStages.Where(s => s > currentTime)                 // 严格大于
//                               .OrderBy(s => s)                             // 升序（λ 是恒等函数）
//                               .FirstOrDefault();                           // 一条都没有 ⇒ 0（**不回落到 endDate**）
//  </code>
//  🔴 三条容易记错的：① 是 **`>`** 不是 `>=`（正好等于现在的那一档**不算**下一档）；
//    ② **没有** `endDate` 兜底（`GetDateForEventStage` 那一条有，这一条没有 —— 两根函数不要互相「照应」着改）；
//    ③ 排序是**升序**（`__9__3_1` 的体 = `return s;`，恒等）。
//
//  `currentTime` 的等价物 = `LiveOpsClock.NowUtc`（`Shell/LiveOpsHandler.cs` 的「现在」唯一读口；
//  原版读 `PlayerDataManager.currentServerTime` = 服务器时间，关服后本机 UTC，口径同 `LiveOpsScheduler.NowMillis`）。

using System;
using UnityEngine;

namespace CardPresentation
{
    public static partial class LiveOpsUtility
    {
        // ⚠️ 新成员追加在类尾（本文件是独立 partial ⇒ 对同族另外两个 partial 的文件行号零影响）。

        /// <summary>原版 `LiveOpsUtility.GetDateForEventStage(ILiveOps liveOps, int stageIndex)`
        /// （`decomp_full/LiveOpsUtility__GetDateForEventStage.c` + `il2csharp:52-66`）。
        /// <para>**转发**到 <see cref="RaidHeaderLogic.GetDateForEventStage"/>（那一处是唯一实现，逐句判据写在它的 doc 上）；
        /// 本方法只补「原版它的第一个实参是 `ILiveOps` 而不是 `LiveOpsEventData`」这一层。</para>
        /// <para>⚠️ **原版这一格 NRE 的路**（`liveOps == null`）本仓改成出声 + `0`（同 `LiveOpsManager.IsTriggeredEvent` 的口径）。</para></summary>
        public static long GetDateForEventStage(ILiveOps liveOps, int stageIndex)
        {
            if (liveOps == null)
            {
                Debug.LogWarning("[LiveOpsUtility] `GetDateForEventStage(null, " + stageIndex
                                 + ")`：原版这一格会 NRE（`liveOps.GetBaseData()`）⇒ 本仓出声 + 返回 0");
                return 0L;
            }
            return RaidHeaderLogic.GetDateForEventStage(liveOps.GetBaseData(), stageIndex);
        }

        /// <summary>原版 `LiveOpsUtility.GetDateForNextEventStage(ILiveOps liveOps)`
        /// （`decomp_full/LiveOpsUtility__GetDateForNextEventStage.c` 全文 + `il2csharp:68-92`）：
        /// 「**严格大于现在**的最小一档」；一档都没有 ⇒ **0**（⛔ 不回落 `dates.endDate`）。
        /// <para>🔴 判据三条（`> ` 不是 `>=` · 无 endDate 兜底 · 升序取首个）见文件头。</para>
        /// <para>⚠️ `liveOps == null` / `data == null` / `dates == null` 三格：原版 NRE，本仓出声 + 0。</para></summary>
        public static long GetDateForNextEventStage(ILiveOps liveOps)
        {
            if (liveOps == null)
            {
                Debug.LogWarning("[LiveOpsUtility] `GetDateForNextEventStage(null)`：原版这一格会 NRE ⇒ 本仓出声 + 返回 0");
                return 0L;
            }
            var data = liveOps.GetBaseData();
            if (data == null || data.dates == null)
            {
                Debug.LogWarning("[LiveOpsUtility] `GetDateForNextEventStage`：`GetBaseData()` 或 `dates` 为空"
                                 + "（原版这一格会 NRE）⇒ 本仓出声 + 返回 0");
                return 0L;
            }

            long now = (long)(LiveOpsClock.NowUtc.ToUniversalTime()
                              - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            var stages = data.dates.eventStages;
            if (stages == null) return 0L;

            long best = 0L;      // 原版 `FirstOrDefault()` 在空序列上给的就是 `default(long)` = 0
            bool any = false;
            for (int i = 0; i < stages.Count; i++)
            {
                long s = stages[i];
                if (s <= now) continue;                       // 原版 `s > currentTime`（严格大于）
                if (!any || s < best) { best = s; any = true; }   // 原版 `OrderBy(s => s).FirstOrDefault()`
            }
            return any ? best : 0L;
        }
    }
}
