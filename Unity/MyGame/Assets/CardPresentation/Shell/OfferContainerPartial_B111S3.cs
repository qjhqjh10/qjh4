// OfferContainerPartial_B111S3.cs —— 执行代理 `B111_S3`（2026-11-01）· 账 `B-67`
//
// ============================ 这条活是什么 ============================
//  原版 `ShopOfferContainer.OnInitialize` 的第 ④ 句：
//      `this.schedule = ActionScheduler.Instance.Schedule(endDate, new Action(this.OnOfferTimerEnded), null);`
//  —— 商品条目**到点**时的那一条「兜底回调」（`TimerDisplay` 自己也会 `OnRunOutOfTime`，但那一颗被
//  关着/被销毁时只有这一条还在跑）。本仓原来**整条没有**（`OfferContainer.Build` 只建节点、不排期）
//  ⇒ `B110_W4` 报告 §六.4 记为「商品条目到点链在本仓不存在」。本文件把那一条接上。
//
// ============================ 判据（三源交叉，逐句）============================
//  ① `d:/2/tools/decomp_full/ActionScheduler__Schedule.c`（Ghidra 伪码，**方法体**）
//  ② `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ActionScheduler.cs`（ILSpy，控制流碎但**签名/重载关系**清楚）
//  ③ `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/ActionScheduler.cs`（il2csharp，**唯一三源都齐的那份**：
//     `Schedule(DateTime)` → 换算成 epoch 毫秒 → 转调 `Schedule(long)`；`Schedule(long)` = 过点 `return null` /
//     建 `ScheduledAction` / 同 id 去重 / 入队后按 `TargetTime` 排序；`Update()` 每拍只看**队首**弹一条）
//  ④ `ShopOfferContainer__OnInitialize.c` + `ShopOfferContainer__OnOfferTimerEnded.c` + `ShopOfferContainer__OnDestroy.c`
//
//  🔴 **三源读出的一条容易记错的**：原版 `OnInitialize` 里 **两个回调不是同一个方法** ——
//     · `timer.OnRunOutOfTime += RefreshExpire`（`.c` 的方法常量 `DAT_18426de58`，il2csharp 直读 `RefreshExpire`）
//     · `ActionScheduler.Schedule(endDate, OnOfferTimerEnded)`（`.c` 的 `DAT_18426dd60`）
//     `B110_W4` 报告把它写成「到点自动刷新」，**语义不准**：`OnOfferTimerEnded` 不刷新，它只做
//     「没持有过这件 ⇒ 把价签置灰」。刷新那一条是 `RefreshExpire`（打远端），**不在本文件**（见报告 §五）。
//
//  `OnOfferTimerEnded`（`ShopOfferContainer__OnOfferTimerEnded.c` 全文两句，il2csharp `:90-101` 互证）：
//  <code>
//  int n = liveOps.GetOwnedQuantity();      // 槽 0xc；liveOps == null ⇒ 原版 NRE
//  if (n != 0) return;                      // 已经持有 ⇒ 什么都不做
//  priceDisplayButton.Interactable = false; // 否则置灰
//  </code>
//
// ============================ 我们接在哪 ============================
//  `ActionScheduler` 的等价件**仓里早就有**（`Shell/LiveOpsHandler.cs` 的 `LiveOpsScheduler`：`Schedule(long)` /
//  `Schedule(DateTime)` / `ScheduleRelative` / `Release` / `Tick` / `PendingCount`，判据逐条写在它的 doc 里，
//  且 `LiveOpsPump` 已经把 `Tick` 挂到每帧泵上）⇒ **不另造第二份排程器**（铁律 6）。
//  本文件只做「原版 `ShopOfferContainer` 那一侧的三件事」：排期 · 到期回调 · 释放句柄。
//
// ============================ 与原版的**如实差异**（⛔ 不是「已经一样」）============================
//  ① **拿不到持有量**：原版 `liveOps.GetOwnedQuantity()` 是服务端报价里的数。本机没有报价数据
//     ⇒ `OwnedQuantitySource` 默认 `null`，回调**出声**并返回（⛔ 不猜一个 0 去假装置灰）。
//  ② ✅ **可交互价签已补（2026-11-01 `B112_B` · 账 `B-81`）**：`OfferContainer.Build` 现在会建
//     `PriceDisplayButton`（`Shell/PriceDisplayButtonPartial_B112B.cs`）+ 命中区，并由 `Build` **按容器**闭包进回调
//     ⇒ 原版 `priceDisplayButton.Interactable = false` 有落点了。`PriceButtonInteractable` 降为**自检注入口**（生产不再用）。
//  ③ 原版 `TimerDisplay` 自己那一跳 `OnRunOutOfTime += RefreshExpire` **仍然没有等价物**（远端刷新），
//     本文件**只补 `ActionScheduler` 这一条**（B-67 的范围）。

using System;
using UnityEngine;

namespace CardPresentation
{
    public static partial class OfferContainer
    {
        // ⚠️ 新成员一律追加在类尾（本文件是一个独立 partial 文件 ⇒ 对 `Shell/OfferContainer.cs` 的行号**零影响**，
        //    `check_ledger_points` 的 37 个不同行号 / 48 条 `OfferContainer.cs:NNN` 落点一个都不动）。

        /// <summary>原版 `ShopOfferContainer.schedule`（+0x88，`ScheduledAction`）：`OnInitialize` 排上的那个到期句柄，
        /// `OnDestroy` 里 `ActionScheduler.Instance.Release(this.schedule)`（`ShopOfferContainer__OnDestroy.c` 第一段）。
        /// <para>`null` = 本轮没排上（没有到期时刻 / 到期时刻已过）—— 与「排了一个永远不触发的」**不是一回事**。</para></summary>
        public static LiveOpsScheduledRefresh TimerSchedule;

        /// <summary>原版 `IShopOffer.GetOwnedQuantity()` 的读口（`ShopOfferContainer__OnOfferTimerEnded.c` 的
        /// `FUN_1800021f0(0xc, …)` = `ILiveOps` 槽 0xc）。
        /// <para>🔶 自己设计（非原版）：这是一个**注入点**，不是算式 —— 判据在远端（服务端报价的持有量，关服后拿不到）
        /// · 为什么必须自定：`GetOwnedQuantity` 没有本地数据源，而「猜一个 0 ⇒ 置灰」等于凭空造一个原版要到服务端才有的判断
        /// · 调度台 2026-11-01 裁定（`B111_S3`）。默认 `null` ⇒ 回调**出声**，⛔ 不静默吞掉。</para></summary>
        public static Func<int> OwnedQuantitySource;

        /// <summary>原版 `PriceDisplayButton.Interactable` 的 setter（`ShopOfferContainer__OnOfferTimerEnded.c` 的
        /// `PriceDisplayButton__set_Interactable(this+0x40, 0)`）。
        /// <para>🆕 **2026-11-01（`B112_B` · 账 `B-81`）**：生产路径改走 `Build` 里那颗**按容器**的 `PriceDisplayButton`
        /// （见 `ScheduleTimerEnd(deadline, priceButton)`）；本字段降为**自检注入口**（`Editor/B111_S3_Assert.cs` 的 C 段还在用它
        /// 断「无脑置灰」那一对；⛔ 别再把它当生产路径的落点）。</para></summary>
        public static Action<bool> PriceButtonInteractable;

        /// <summary>原版 `ShopOfferContainer.OnInitialize` 第 ④ 句：
        /// `schedule = ActionScheduler.Instance.Schedule(endDate, OnOfferTimerEnded, null)`。
        /// <para>我们的等价件 = <see cref="LiveOpsScheduler.Schedule(long, Action, string)"/>：**过点 ⇒ 返回 `null`、不入队**
        /// （判据 `ActionScheduler__Schedule.c:26-30` 的 `if (param_2 &lt; TimeHelper.get_ServerTime()) return 0;`）。</para>
        /// <para>⚠️ 原版传的 `actionId` 是 `null`（`.c` 第四个实参 = 0）⇒ **不走同 id 去重**，`ScheduledAction`
        /// 自己 `Guid.NewGuid()`。本方法照抄（也传 `null`）。</para></summary>
        public static LiveOpsScheduledRefresh ScheduleTimerEnd(DateTime deadline, PriceDisplayButton priceButton = null)
        {
            long ms = (long)(deadline.ToUniversalTime()
                             - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            var h = LiveOpsScheduler.Schedule(ms, () => OnOfferTimerEnded(priceButton), null);
            TimerSchedule = h;
            if (h == null)
                Debug.LogWarning("[OfferContainer] 到期时刻 `" + deadline.ToUniversalTime().ToString("u")
                                 + "`（" + ms + " ms）**已过** ⇒ 原版 `ActionScheduler.Schedule` 直接 `return null`、"
                                 + "**不入队**（`ActionScheduler__Schedule.c:26-30`；`out_il2csharp/ActionScheduler.cs` 同支）");
            else
                Debug.Log("[OfferContainer] 照原版 `ShopOfferContainer.OnInitialize` 第 ④ 句排上到期回调："
                          + "`ActionScheduler.Schedule(" + ms + ", OnOfferTimerEnded, null)` ⇒ `" + h + "`");
            return h;
        }

        /// <summary>「本轮没有到期时刻」那一条**出声**（调用点是 `Build` 里 `TimerDeadline.HasValue` 的 else 支）。
        /// <para>原版 `OnInitialize` 的不计时支是**静默**的（只 `timer.gameObject.SetActive(false)`）——
        /// 我们多这一句，是因为**本机恒走这一支**（没有报价数据）：不出声的话，「一条都没排上」会被当成「已经做完了」，
        /// 正是铁律 11 与红线「不许静默失败」要挡的那种。</para></summary>
        public static void NoteNoTimerDeadline(string prefab)
        {
            Debug.LogWarning("[OfferContainer] `" + prefab + "`：**本轮没有到期时刻**（`TimerDeadline` 为空 ——"
                             + "本族数据源 = 服务端 LiveOps 报价，本地没有）⇒ 照原版 no-data 支整颗 `Timer` 关着，"
                             + "**不排 `ActionScheduler.Schedule`**（原版那一条也要 `endDate` 才有得排）");
        }

        /// <summary>原版 `ShopOfferContainer.OnOfferTimerEnded`（`ShopOfferContainer__OnOfferTimerEnded.c` 全文两句）。
        /// <para>两种「这一格没落点」都**出声**、⛔ 不假装：没有持有量数据源（远端）/ 没有可交互价签（复刻缺口）。</para></summary>
        public static void OnOfferTimerEnded(PriceDisplayButton priceButton = null)
        {
            if (OwnedQuantitySource == null)
            {
                Debug.LogWarning("[OfferContainer] 到期回调 `OnOfferTimerEnded`：**没有持有量数据源**（原版 "
                                 + "`liveOps.GetOwnedQuantity()`，服务端报价）⇒ 「没持有过就置灰价签」这一格没落点，"
                                 + "出声、⛔ 不假装（猜 0 会凭空置灰）");
                return;
            }
            if (OwnedQuantitySource() != 0) return;      // 原版第一句：已持有 ⇒ 什么都不做

            if (priceButton != null) { priceButton.Interactable = false; return; } else if (PriceButtonInteractable != null) { PriceButtonInteractable(false); return; }
            Debug.LogWarning("[OfferContainer] 到期回调 `OnOfferTimerEnded`：持有量为 0 ⇒ 原版会 "
                             + "`priceDisplayButton.Interactable = false`，而这一拍**没有可交互价签**"
                             + "（`AttachPriceButton` 没建出命中区 / 调用方没传容器）⇒ 这一格不下发");
        }

        /// <summary>自检收尾用：把三格注入点、句柄、到期时刻一起还原（⛔ 不还原会让下一条断言读到上一条的残留）。
        /// <para>⚠️ 会 `Release` 还在途的句柄 —— 原版 `OnDestroy` 做的就是这一句。</para></summary>
        public static void ResetTimerForTest()
        {
            if (TimerSchedule != null) LiveOpsScheduler.Release(TimerSchedule);
            TimerSchedule = null;
            OwnedQuantitySource = null;
            PriceButtonInteractable = null;
            TimerDeadline = null;
        }
    }
}
