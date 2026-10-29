// ExpansionPassPremiumOffers.cs — 账 `商店:89` / `商店:91` / `商店:92` 的**本地报价表 + 关窗回调接线**
// （原版 `ExpansionPassPurchasePremiumPanel.PurchasePremiumClick` 一族的 REMOTE 半边）
//
// ============================ 判据（第一权威 = 反编译方法体，2026-10-29 现读）============================
// ① `d:/2/tools/decomp_full/ExpansionPassPurchasePremiumPanel__PurchasePremiumClick.c` —— 五跳：
//      `data = 组件 +0x58` / `premiumOfferData = data.PremiumOfferData`
//      → `LiveOpsManager.GetHandler<>().GetEvent(premiumOfferData.GetID())`
//      → `prefab = LiveOpsAssetUtility.GetComponentReference(premiumOfferData, 0xc)`
//      → `win = WindowsManager.Instance.OpenWindow(prefab, eventData)`
//      → `win == null`（或它不是 `GameWindow`）⇒ `CustomDebug.LogWarning("…" + premiumOfferData.GetID())`
//        —— 那条告警**带事件 id**（本文件的 `SayCannotOpen` 逐字照这个形状）
//      → 否则往 `win` 的 `+0x60` 那个 nullary 委托上**连续 Combine 两条**：
//        `new UIGenericEventCatcher.SourceDelegate(this, <PurchasePremiumClick>b__12_0)`，再 `… b__12_1`。
//        ⚠️ **原版没有 `RemoveListener`/`-=`** ⇒ 点几次就叠几次订阅 —— 本文件照抄（⛔ 别顺手去重）。
// ② `…__<PurchasePremiumClick>b__12_0.c` 全文一句：`Initialize(this.data(+0x58), this.onClose(+0x60))`
//      ⇒ 对位 = `ExpansionPassEventWindow.ApplyPremiumPanel(_onClosePurchasePopup)`（那个方法就是本仓的 `Initialize`）。
// ③ `…__<PurchasePremiumClick>b__12_1.c` 全文一句：`onClose(+0x60)` 非空就调它（nullary 委托）。
// ④ 类桩 `ExpansionPassPurchasePremiumPanel.Initialize(ExpansionPassEvent, Action onClosePurchasePopup)`
//      ⇒ `onClose(+0x60)` 是 **nullary `Action`**；本仓对位 = `ExpansionPassEventWindow.OnClosePurchasePopup`。
//
// ============================ 🔶 哪一部分是我们定的（照实记，⛔ 不冒充原版）============================
// · **这张报价表**（`_table` / `ForEvent`）= 🔶 自己设计（非原版）：
//     原版那一格 `ExpansionPassData.PremiumOfferData` 由**远端 LiveOps / Addressables** 下发
//     （它同时给出①事件 id ②`GetComponentReference(…, 0xc)` 那扇窗的 prefab 引用）。
//     本机 `assets_full` 全库 0 命中（判据 → 账 `商店:89` 的差异列；原版已关服 ⇒ 判据永远在远端）
//     ⇒ 按 CLAUDE.md §18·0「只有和服务器有关的才允许自己设计」建这张表，把那一格补上。
//     · 窗口那一格：本地用 `WindowsManager.OpenPurchasePremiumWindow`（= 原版 `PurchasePremiumWindow`
//       的本仓实现，`Shop` 家族那扇）顶替远端 prefab 引用；`offers` 传 `null`
//       —— 这不是偷懒：原版 prefab 里 `premiumStoreReference` **本身就是空引用** ⇒ `FetchData()` 空表
//       ⇒ 「一个 `Army Container` 都不建」，与传 `null` 同形（判据 → `Shell/PurchasePremiumWindow.cs` 文件头 ④）。
//     · 阵营那一格：`FocusArmy`，缺省 `10` = `Ultramarines`（原版 `PurchasePremiumWindow__TryOpen.c`：
//       `data as CardArmy? ?? Ultramarines(10)`）。
//     谁定的：调度台 2026-10-29 裁定（依铁律 18 / §18·0 打标）。
// · **`win + 0x60` 的对位** = `GameWindow.OnCloseWindow`（nullary `Action`，`GameWindow__Hide.c` 末句 Invoke）。
//     ⚠️ 判据强度**如实标**：偏移（`+0xc` 个 `longlong` = `+0x60`）与委托形状（nullary）都对得上，但
//     那个字段的**名字没从类桩确证** —— `GameWindow` 桩里只有 `OnOpen` / `OnCloseWindow` 两个 `Action`，
//     而全库没有任何类声明 `UIGenericEventCatcher.SourceDelegate` 字段（`grep` 现读）⇒ 反编译把
//     `Action` 的 `___ctor` 显示成了同址的 `SourceDelegate___ctor`（同一份泛型委托实现共用 VA）。
//     取「关窗」这一支的理由（语义，不是猜名字）：b__12_1 调的是面板自己的 `onClosePurchasePopup`
//     （命名 = 关掉购买弹窗）、b__12_0 重跑 `Initialize` = 关窗后刷新面板 —— 两条都读作「购买弹窗关掉了」。
//     ⛔ 别改挂到别的事件上（会被判成「原版是买入完成就刷新」）。
//
// ============================ 出声（本工程红线：不许静默失败）============================
// · 第一次用到本地表 ⇒ 一条 `Debug.Log`，把「这一格是本地模拟」说出来（⛔ 不让它冒充远端数据）。
// · 打不开窗 ⇒ `SayCannotOpen` 的 `LogWarning`（**带事件 id**，= 原版第 ⑤ 句那条）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>账 `商店:89`/`91`/`92`：Expansion Pass 付费档（premium）报价的**本地替身表** ＋
    /// 原版那两条 `b__12_0` / `b__12_1` 关窗回调的接线。判据与「哪一格是我们定的」见文件头。</summary>
    public static class ExpansionPassPremiumOffers
    {
        /// <summary>原版 `PurchasePremiumWindow.TryOpen` 的缺省聚焦阵营：`Ultramarines = 10`
        /// （`PurchasePremiumWindow__TryOpen.c`：`data as CardArmy? ?? Ultramarines(10)`）。</summary>
        public const int DefaultFocusArmy = 10;

        /// <summary>🔶 自己设计（非原版）：报价表的一行 = 原版 `PremiumOfferData` 在本地的**最小等价物**。
        /// 原版那个对象另有一格 `GetComponentReference(…, 0xc)`（要开的窗口）；本地那一格恒指向
        /// 本仓的 `PurchasePremiumWindow`，所以**不建字段**（⛔ 别加一个永远填同一个值的字段冒充数据）。</summary>
        public struct Offer
        {
            /// <summary>= 原版 `premiumOfferData.GetID()`（事件 id）。`"*"` = 通配档（本地常态 `_event == null` 时用）。</summary>
            public string EventId;
            /// <summary>= 原版 `CardArmy`（`PurchasePremiumWindow.TryOpen` 的 `data`）。</summary>
            public int FocusArmy;
        }

        /// <summary>通配键：`_event == null`（本地没有远端 liveops 事件数据时）也走这一档 ——
        /// 否则「Get now!」永远打不开窗，那等于把 REMOTE 这一条继续挂着。</summary>
        public const string AnyEventId = "*";

        /// <summary>🔶 自己设计（非原版）：报价表本体。出厂只有「通配档 = Ultramarines」一条。
        /// 要接真数据时用 <see cref="Replace"/> 逐事件改写（⛔ 别在别处再开第二张表）。</summary>
        static readonly List<Offer> _table = new List<Offer>
        {
            new Offer { EventId = AnyEventId, FocusArmy = DefaultFocusArmy },
        };

        static bool _localSaid;

        // ------------------------------------------------------------ 读

        /// <summary>原版第 ① 句那两格（`data` / `premiumOfferData`）在本地的查表口。
        /// <para>`null` = **表里一条都没有**（自检可以 `ClearForTest()` 造这一态）⇒ 调用方出声并返回 false。</para>
        /// <para>顺序：先找 `eventId` 完全相等的，再退回通配档 —— 与原版「先拿 id、再拿引用」的次序一致。</para></summary>
        public static Offer? ForEvent(string eventId)
        {
            if (!string.IsNullOrEmpty(eventId))
                for (int i = 0; i < _table.Count; i++)
                    if (_table[i].EventId == eventId) return _table[i];
            for (int i = 0; i < _table.Count; i++)
                if (_table[i].EventId == AnyEventId) return _table[i];
            return null;
        }

        /// <summary>表里现在有几条（自检读口）。</summary>
        public static int Count { get { return _table.Count; } }

        /// <summary>🔶 自己设计（非原版）：写一条报价（同 id 覆盖）。返回 `true` = 新加、`false` = 覆盖。
        /// <para>留给「将来真有远端数据」与自检用；生产路径**不调**它。</para></summary>
        public static bool Replace(string eventId, int focusArmy = DefaultFocusArmy)
        {
            string id = string.IsNullOrEmpty(eventId) ? AnyEventId : eventId;
            for (int i = 0; i < _table.Count; i++)
                if (_table[i].EventId == id) { var o = _table[i]; o.FocusArmy = focusArmy; _table[i] = o; return false; }
            _table.Add(new Offer { EventId = id, FocusArmy = focusArmy });
            return true;
        }

        /// <summary>清空整张表（自检专用 —— 用来造「打不开」那一态）。</summary>
        public static void ClearForTest() { _table.Clear(); }

        /// <summary>把表恢复出厂（通配档一条）。自检收尾用。</summary>
        public static void ResetForTest()
        {
            _table.Clear();
            _table.Add(new Offer { EventId = AnyEventId, FocusArmy = DefaultFocusArmy });
            _localSaid = false;
        }

        // ------------------------------------------------------------ 写（两条回调用）

        /// <summary>自检读口：`b__12_0`（重跑面板 `Initialize`）被调了几次。</summary>
        public static int ReinitializeCalls { get; private set; }
        /// <summary>自检读口：`b__12_1`（把 `onClosePurchasePopup` 转出去）**真的调用到回调**几次。</summary>
        public static int NotifyParentCalls { get; private set; }
        /// <summary>自检读口：`b__12_1` 发现 `onClosePurchasePopup` 是 null 的次数（原版那一支**什么都不做**）。</summary>
        public static int NotifyParentNullCalls { get; private set; }
        /// <summary>自检读口：`SayCannotOpen` 被调了几次（= 原版第 ⑤ 句那条告警）。</summary>
        public static int CannotOpenCalls { get; private set; }
        /// <summary>自检读口：最近一次「打不开」的成因。</summary>
        public static string LastCannotOpenReason { get; private set; }

        /// <summary>自检读口：两条关窗回调**实际跑的次序**（`R` = `b__12_0`、`N` = `b__12_1`）。
        /// 原版先 `Delegate.Combine` `b__12_0` 再 Combine `b__12_1` ⇒ 期望 `"RN"`（把两条 `+=` 对调就会变 `"NR"`）。</summary>
        public static string Order { get { return _order.ToString(); } }
        static readonly System.Text.StringBuilder _order = new System.Text.StringBuilder();

        /// <summary>自检收尾：把四个计数 + 次序串清零。</summary>
        public static void ResetCountersForTest()
        {
            ReinitializeCalls = 0; NotifyParentCalls = 0; NotifyParentNullCalls = 0; CannotOpenCalls = 0;
            LastCannotOpenReason = null;
            _order.Length = 0;
        }

        /// <summary>= 原版 `PurchasePremiumClick` 第 ⑤ 句那条 `CustomDebug.LogWarning` 的形状：
        /// **把事件 id 打出来**，并说清「这一格是本地模拟」。返回 `false` 给调用方当返回值。</summary>
        public static bool SayCannotOpen(string eventId, string why)
        {
            CannotOpenCalls++;
            LastCannotOpenReason = why;
            Debug.LogWarning("[ExpansionPass] **买付费档**打不开：事件 `" + (eventId ?? "<没有事件数据>") + "` —— " + why
                           + "（原版 `ExpansionPassPurchasePremiumPanel.PurchasePremiumClick.c` 第 ⑤ 句就是"
                           + " `LogWarning(… + premiumOfferData.GetID())`：告警**带事件 id**；"
                           + "本地那一格 `ExpansionPassData.PremiumOfferData` 在远端 LiveOps，🔶 用 "
                           + typeof(ExpansionPassPremiumOffers).Name + " 的本地表顶替）");
            return false;
        }

        /// <summary>= 原版 `PurchasePremiumClick` 第 ⑥ 句：把那两条回调 Combine 到**购买窗**上。
        /// <list type="number">
        /// <item>`b__12_0`（先挂）= 重跑面板 `Initialize`（`ApplyPremiumPanel(onClosePurchasePopup)`）</item>
        /// <item>`b__12_1`（后挂）= 调面板自己的 `onClosePurchasePopup`（非空才调）</item>
        /// </list>
        /// <para>⚠️ 两条**都**挂在同一个事件上、**都不去重**（原版两次 `Delegate.Combine`、没有 `-=`）
        /// ⇒ 窗开几次就叠几遍，关一次全跑 —— 照抄。</para>
        /// <returns>`true` = 挂上了（窗非 null）。</returns></summary>
        public static bool WireClose(PurchasePremiumWindow win, string eventId, ExpansionPassEventWindow panel)
        {
            if (win == null) return SayCannotOpen(eventId, "`OpenPurchasePremiumWindow` 返回 null");
            if (panel == null)
            {
                Debug.LogWarning("[ExpansionPass] `WireClose`：面板是 null ⇒ 两条关窗回调挂不上去（原版那两条都是"
                               + " `this.data` / `this.onClose` 的实例方法，没有面板就没有 `this`）");
                return false;
            }

            // 🔴 原版 `Delegate.Combine(win + 0xc, new SourceDelegate(this, b__12_0))` —— 第一条
            win.OnCloseWindow += () => Reinitialize(panel);
            // 🔴 再 Combine 一次：`… b__12_1`（顺序照反编译：b__12_0 先、b__12_1 后）
            win.OnCloseWindow += () => NotifyParent(panel);

            if (!_localSaid)
            {
                _localSaid = true;
                var hit = ForEvent(eventId);
                Debug.Log("[ExpansionPass] 买付费档：**真把窗开出来了**（事件 `" + (eventId ?? "<null>") + "`，"
                        + "聚焦阵营 = " + (hit.HasValue ? hit.Value.FocusArmy.ToString() : "?") + "）。🔶 这一格是本地模拟：原版第 ③ 句"
                        + " `LiveOpsAssetUtility.GetComponentReference(premiumOfferData, 0xc)` 那扇窗的 prefab 引用在"
                        + "远端 premium 报价资产里（本地 0 命中），我们用 `Shell/PurchasePremiumWindow.cs` 顶替；"
                        + "两条关窗回调（`b__12_0` 重跑面板 `Initialize` / `b__12_1` 转出 `onClosePurchasePopup`）"
                        + "已挂到 `GameWindow.OnCloseWindow`（对位说明见 `ExpansionPassPremiumOffers.cs` 文件头）。");
            }
            return true;
        }

        /// <summary>= 原版 `<PurchasePremiumClick>b__12_0`（全文一句：`Initialize(this.data, this.onClose)`）。
        /// <para>本仓 `Initialize` = <see cref="ExpansionPassEventWindow.ApplyPremiumPanel(System.Action)"/>：
        /// 它第一句就是 `onClosePurchasePopup = 入参`（原版 `Initialize` 第一句），后面按 `HasPremium` 重摆两态、
        /// 重画两条奖励锚点 ⇒ **重跑它就是「刷新面板」**（`_event == null` 时它自己早退，⛔ 不用在这里再判）。</para></summary>
        static void Reinitialize(ExpansionPassEventWindow panel)
        {
            if (panel == null) return;
            ReinitializeCalls++;
            _order.Append('R');
            panel.ApplyPremiumPanel(panel.OnClosePurchasePopup);
        }

        /// <summary>= 原版 `<PurchasePremiumClick>b__12_1`（全文一句：`onClose(+0x60)` 非空就调它）。
        /// <para>⛔ 原版这一支**不判空以外的东西**、也**不吞异常**；我们把「null」也记一格（出声用），
        /// 但**不加任何闸**（⛔ 别改成「只调一次」）。</para></summary>
        static void NotifyParent(ExpansionPassEventWindow panel)
        {
            if (panel == null) return;
            _order.Append('N');
            var cb = panel.OnClosePurchasePopup;
            if (cb == null)
            {
                NotifyParentNullCalls++;
                // 原版这一支什么都不做；本地出声一次（红线：不许静默失败 —— 这一格说明「没人接这个关窗通知」）
                Debug.Log("[ExpansionPass] 购买弹窗关了：面板的 `onClosePurchasePopup`(原版 `+0x60`) 是 null ⇒ "
                        + "原版这一支什么都不做（本窗的父级没接这个回调；`ApplyPremiumPanel` 的入参今天恒 null）。");
                return;
            }
            NotifyParentCalls++;
            cb();
        }
    }
}
