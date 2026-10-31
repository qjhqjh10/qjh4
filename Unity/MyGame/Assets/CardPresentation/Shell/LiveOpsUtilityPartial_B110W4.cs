// LiveOpsUtilityPartial_B110W4.cs —— 执行代理 `B110_W4`（2026-10-31）· 账 `B-44`（`N34`）
//
// ============================ 这是什么 ============================
// 原版 `LiveOpsUtility.IsTimedEvent(ILiveOps liveOps, out long endDate)`。「计时活动 / 不计时活动」的**唯一判据**，
// 原版 5 个调用点全在「要不要显示倒计时、显示到哪一刻」这一件事上：
//   `BaseOfferPopup__SetTimer.c:21` · `BaseOfferPopup__SetEventData.c:105` · `ItemShopTab__CheckTimer.c:19`
//   · `OpenEventButton__SetButtonInfo.c:72` · `ShopOfferContainer__OnInitialize.c:26`（`grep -rn IsTimedEvent decomp_full/*.c` 现读）。
//
// ============================ 判据（第一权威 = `decomp_full/LiveOpsUtility__IsTimedEvent.c` 全文）============================
// 可读互校版 = `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/LiveOpsUtility.cs:18-50`（两源逐支一致；ILSpy 版
// `out_cpp2il_dev_ilspy/LiveOpsUtility.cs` 的控制流被反编译打散，只能对结构、不能对次序）。
// 逐句：
// <code>
// var data = liveOps.GetBaseData();                         // 槽 15；原版每读一格都重取一次（同一次调用的同一对象）
// if (data.activationType != ActivationType.Permanent)      // +0x24，Permanent = 0
//     endDate = data.dates.endDate;                         // dates(+0x28) 为 null ⇒ NRE
// else if (!data.filter.hasDuration)                        // filter(+0x30) 为 null ⇒ NRE
//     { endDate = long.MaxValue; return false; }
// else
//     endDate = long.MaxValue;
// if (data.filter.hasDuration)                              // ★ `Math.Min` 那一半
//     endDate = Math.Min(endDate, data.filter.GetExpirationTime(liveOps));
// return true;
// </code>
//
// 🔴 四条**照抄、⛔ 别「修好」**的语义：
//   ① 「是不是计时活动」 = `activationType != Permanent` **或** `filter.hasDuration`。
//      **永久活动且时长没开 ⇒ 不是计时活动** ⇒ `endDate = long.MaxValue`、返回 false（调用点据此藏掉整件倒计时）。
//   ② `activationType == Permanent` 走 else 支时**一个字节都不读 `dates`** ⇒ 那一支 `dates == null` 原版**不炸**
//      （本文件照抄；自检 `A7` 钉的就是这条 —— 顺手在 `dates` 上补判空会把原版可达的一支做成不可达）。
//   ③ `hasDuration` 为真 ⇒ **一定再过一遍 `Math.Min`**，且**基底是 `endDate`**（不是「直接用 `GetExpirationTime`」）：
//      `OnDate` 事件里 `endDate` 早于「下次刷新时刻」时取的是 **`endDate`**。自检 `A4` / `A5` 把两个方向各钉一次。
//   ④ `GetExpirationTime` 原版是 **32 位乘法**（`Shell/LiveOpsFilter.cs:866` 的 `unchecked`）⇒ 溢出照抄，不自作聪明改 long。
//
// ============================ 与原版的**有意差异**（如实标，⛔ 别当成「已经一样」）============================
//   原版在 `liveOps == null` / `GetBaseData() == null` / `filter == null` / （`activationType != Permanent` 时）
//   `dates == null` 这四格上是**直接 NRE**（`FUN_1803f47a0` = 抛异常桩）。本仓的既有口径（同
//   `EventFilter.GetExpirationTime` / `LiveOpsManager.IsTriggeredEvent`）= **出声一次 + 走安全支**，不让坏数据把游戏打崩：
//     · 前三格 ⇒ `endDate = long.MaxValue` + **返回 false**（= 调用点走「不显示倒计时」那一支，与原版崩溃后什么都看不见的观感一致）；
//     · 第四格 ⇒ 同上。
//   这四处都在自检 `B 段` 里逐条钉住（含「原版会 NRE ⇒ 我们不出声就不对」的出声断言）。
//
// ============================ 落点 / 接线状态（宿主归调度台）============================
//   本文件只补**方法本体**。原版 5 个调用点在我们仓里的现状**逐个核过**（判据 + 逐条结论 → `资料/完全复刻/报告/B110_W4.md`）：
//     · `BaseOfferPopup.SetTimer` / `.SetEventData` → 本仓该窗**根本没有报价对象**（建树时 `Timer` 整件建成关着）⇒ 恒走「不显示」支；
//     · `ItemShopTab.CheckTimer` → 本仓等价物 `ShopTabPage.PageShowsTimer`（Daily/Items 恒 false，B88_J1 裁定）；
//     · `OpenEventButton.SetButtonInfo` → 本仓等价物 `MainMenuRuntime.BuildEventNavButtons`（到期取 `LiveOpsLocalEvent.EndMs`）；
//     · `ShopOfferContainer.OnInitialize` → 本仓无对应容器（商店那颗走 `CatalogItemContainer`，不建倒计时）。
//   ⇒ **一个都不硬造第二份判断**（铁律 6）；将来接报价数据时**只调这一处**。
//
// ============================ 为什么单独一个文件 ============================
// `LiveOpsManager.cs` 是 `sealed class`（无 `partial`）且**不在本批白名单内** ⇒ 不能往它里面挂成员；
// 而这里要的正是原版的类名 `LiveOpsUtility`（`Shell/LiveOpsManager.cs` 的 `IsTriggeredEvent` 是它的另一个成员，
// 当年落在了 `LiveOpsManager` 上 —— 这处「同类两名」已写进报告「顺手发现」，⛔ 本批不动它）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `LiveOpsUtility` 的静态工具族。**本批只补 `IsTimedEvent`**（其余成员见 `Shell/LiveOpsManager.cs` 的 `IsTriggeredEvent`、
    /// `Shell/RaidEvent.cs` 的 `GetDateForEventStage`）。
    /// <para>声明成 `partial` 是为了将来别的成员能并进来（本批只有这一份 part）。</para></summary>
    public static partial class LiveOpsUtility
    {
        // ⚠️ 新成员一律追加在类尾（本类只有一个方法，不存在插入点问题）。

        /// <summary>原版 `LiveOpsUtility.IsTimedEvent(ILiveOps liveOps, out long endDate)`（`decomp_full/LiveOpsUtility__IsTimedEvent.c`，**逐句**）：
        /// <code>
        /// var data = liveOps.GetBaseData();
        /// if (data.activationType != ActivationType.Permanent) endDate = data.dates.endDate;
        /// else if (!data.filter.hasDuration) { endDate = long.MaxValue; return false; }
        /// else endDate = long.MaxValue;
        /// if (data.filter.hasDuration) endDate = Math.Min(endDate, data.filter.GetExpirationTime(liveOps));
        /// return true;
        /// </code>
        /// <para>返回 true = **这个活动有倒计时**，<paramref name="endDate"/> = 倒计时打到哪一刻（毫秒，UTC epoch）。</para>
        /// <para>🔴 `activationType == Permanent` 那一支**不读 `dates`**（原版同此）—— 别在这里补 `dates` 判空。</para>
        /// <para>⚠️ 与原版的有意差异：null 入参原版**抛 NRE**（`FUN_1803f47a0`），我们出声一次并返回 false
        /// （`endDate = long.MaxValue`）—— 口径同 `EventFilter.GetExpirationTime` / `LiveOpsManager.IsTriggeredEvent`。</para></summary>
        public static bool IsTimedEvent(ILiveOps liveOps, out long endDate)
        {
            endDate = long.MaxValue;                                   // 原版：两条「非计时」支都是这个值

            if (liveOps == null)
            {
                // 原版：NRE（`IsTimedEvent.c` 里 `param_1 != 0` 的 else 直接落到抛异常桩）
                Debug.LogError("[LiveOps] LiveOpsUtility.IsTimedEvent(null)（原版会 NRE）⇒ 当成「不是计时活动」");
                return false;
            }

            var data = liveOps.GetBaseData();                          // 原版槽 15；null ⇒ NRE
            if (data == null)
            {
                Debug.LogError("[LiveOps] LiveOpsUtility.IsTimedEvent：活动 '" + (liveOps.GetID() ?? "<null>")
                             + "' 没有 baseData（原版会 NRE）⇒ 当成「不是计时活动」");
                return false;
            }

            var filter = data.filter;                                  // 原版 +0x30；null ⇒ NRE
            if (filter == null)
            {
                Debug.LogError("[LiveOps] LiveOpsUtility.IsTimedEvent：活动 '" + (data.eventId ?? "<null>")
                             + "' 的 filter 是 null（原版会 NRE）⇒ 当成「不是计时活动」");
                return false;
            }

            if (data.activationType != LiveOpsActivationType.Permanent)
            {
                // ★ 原版这一支**只读 dates.endDate**（不读 filter.hasDuration 就能定 endDate）
                var dates = data.dates;                                // 原版 +0x28；null ⇒ NRE
                if (dates == null)
                {
                    Debug.LogError("[LiveOps] LiveOpsUtility.IsTimedEvent：活动 '" + (data.eventId ?? "<null>")
                                 + "' 的 activationType=" + data.activationType + " 但没有 dates（原版会 NRE）⇒ 当成「不是计时活动」");
                    return false;
                }
                endDate = dates.endDate;
            }
            else if (!filter.hasDuration)
            {
                // ★ 永久活动 + 没有时长 ⇒ **不是计时活动**（endDate 保持 long.MaxValue）
                return false;
            }
            // else：永久活动 + hasDuration 为真 ⇒ endDate 保持 long.MaxValue，交给下面那句 Math.Min 定

            if (filter.hasDuration)
            {
                // ★ Math.Min 那一半：基底是 endDate（上一步算出来的），不是「直接用 GetExpirationTime」
                endDate = System.Math.Min(endDate, filter.GetExpirationTime(liveOps));
            }

            return true;
        }
    }
}
