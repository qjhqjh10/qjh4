// B94B4_Assert.cs — 第九十四会话 · 波 2 · 执行代理 B4（日常/奖励线）的**自检断言集**。
//
// 覆盖两条「本批真的动了东西」的账（其余 58 条只做复核，见 `资料/完全复刻/报告/B94_B4.md`）：
//   · `日常_奖励 :702 / :704`（`SignalEventTrigger<T>.Register/Unregister`）—— **B→A**。
//     判据很久以来记的是「四源全无、挂哪条泛型重载是推的」；2026-10-29 `B94_B4` 在**第五源**
//     `d:/2/tools/decomp_generic_trial/out_b94/SignalEventTrigger_object_.{Register__12549a0,
//     Unregister__1254b90,TryTrigger__1254a40}.c` 找到泛型共享体并逐句核过：
//       `Register(cb)`    = `*(this+0x18) = cb` → `Signal<T>.Register(new Action<T>(this.TryTrigger))`
//       `Unregister()`    = 不碰 `callback`，只 `Signal<T>.Unregister(new Action<T>(this.TryTrigger))`
//       `TryTrigger(sig)` = 先虚调用 `ValidateSignal`（`+0x198`），通过且 `callback != null` 才 `Invoke`
//     ⇒ 本断言集把**「落在哪条泛型重载上」**钉死：订阅必须出现在 **`Signal<ProbeSignal>` 那本台账**里
//       （`ListenerCount<T>()` 的增减），⛔ 不能只看「回调打出来了」（那种断言挂在任何一条重载上都会绿）。
//   · `日常_奖励 :526`（`MissionsTab.InstantiateGridMissions`）—— **C→B**：建树那一半仍然不落
//     （`missionsHolder` / `FlexibleGridLayout` / 格子数据本地都没有），但**列数那三条判据**已经从
//     「一句话描述」落成可执行纯函数 `MissionsTab.GridMissionLayoutLogic`。判据 = `.c:53-67`：
//     `columns = (n == 1 || n == 2) ? 2 : (n == 3 ? 1 : 0)`。
//
// 跑法（与 `B94S2SelfCheck` / `LiveOpsB93N2SelfCheck` 同一套形状，宿主由调度台接线）：
//     Debug.Log(P + "B94B4Assert 跑 " + B94B4Assert.Run((ok, msg) => CheckTrue(ok, msg)) + " 条断言");
//
// 🔴 **期望值一律来自原版判据**（`.c` 的分支 / 泛型共享体的调用面），⛔ 不取我们自己的常量当期望。
// 🔴 **每条先断前提、再断结论**（⛔ 不用 `if (前提) Check(结论)` —— 那种写法前提不成立时会**静默跳过**）。
// 🔴 **每条都配灭自证**：把实现改回「记在别的重载上」/「列数 = 条数」时，**必须至少有一条变红**。
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`B4` 的断言集。返回**实际执行的断言条数**（调用方打印它 = 自证真被跑到）。</summary>
    public static class B94B4Assert
    {
        // ============================================================ 夹具（信号触发器）
        /// <summary>探针信号（`ILiveOpsSignal` 是空标记接口 ⇒ 只带一格 id 用来区分匹配/不匹配）。</summary>
        sealed class ProbeSignal : ILiveOpsSignal { public int Id; }

        /// <summary>探针触发器：只认 `Id == Accept` 的那条信号。
        /// <para>⚠️ 它继承的是**我们**的 `SignalEventTrigger&lt;T&gt;`（原版同名类，按本项目扁平命名空间改了 `EventTrigger` 的名）。</para></summary>
        sealed class ProbeTrigger : SignalEventTrigger<ProbeSignal>
        {
            public int Accept;
            public int Validated;
            protected override bool ValidateSignal(ProbeSignal signal)
            {
                Validated++;
                if (signal == null || signal.Id != Accept) return false;
                return true;
            }
        }

        /// <summary>跑全部断言。`check(条件, 断言文案)` 由宿主提供。返回执行的断言条数。</summary>
        public static int Run(Action<bool, string> check)
        {
            if (check == null) { Debug.LogError("[B94B4] `B94B4Assert.Run(null)` ⇒ 不跑"); return 0; }
            int n = 0;
            Action<bool, string> C = delegate (bool ok, string msg) { n++; check(ok, msg); };

            GridMissionColumns(C);
            SignalTriggerOverload(C);
            WeeklyTimerDescription(C);
            return n;
        }

        // ============================================================ ① 网格任务的列数（账 `:526`）
        //   判据：`decomp_full/MissionsTab__InstantiateGridMissions.c:53-67`
        //     `if (n != 1) { if (n != 2) { if (n == 3) iVar2 = 1; else iVar2 = 0; goto L; } } iVar2 = 2;`
        //   ⇒ **1 条 ⇒ 2 列 · 2 条 ⇒ 2 列 · 3 条 ⇒ 1 列 · 其它 ⇒ 0**
        //   🔴 账 `:526` 原来写「按 1/2/3 条写 1/2 格位」，容易被读成「条数 = 列数」——
        //      其中 **1 条给的是 2 列**，这一条就是那句订正的灭自证（写成「列数 = 条数」⇒ 第 1 条红）。
        static void GridMissionColumns(Action<bool, string> C)
        {
            // 前提（硬）：那个类型真的在、而且真的给出 4 档不同结果的空间
            C(MissionsTab.GridMissionLayoutLogic.ColumnCount(2) == 2,
              "①（前提）`MissionsTab.GridMissionLayoutLogic.ColumnCount` 在，且 2 条 ⇒ 2 列（否则下面几条无从谈起）");

            C(MissionsTab.GridMissionLayoutLogic.ColumnCount(1) == 2,
              "★ ①a 只有 **1 条**网格任务 ⇒ **2 列**（`.c:63` 落到 `iVar2 = 2`）—— 灭自证：写成「列数 = 条数」⇒ 这里得 1、红");
            C(MissionsTab.GridMissionLayoutLogic.ColumnCount(2) == 2,
              "★ ①b **2 条** ⇒ **2 列**（同上；两档同值 ⇒ 别把 1/2 写成两个不同列数）");
            C(MissionsTab.GridMissionLayoutLogic.ColumnCount(3) == 1,
              "★ ①c **3 条** ⇒ **1 列**（`.c:59-60`）—— 灭自证：三段分支抄错顺序 ⇒ 红");
            C(MissionsTab.GridMissionLayoutLogic.ColumnCount(0) == 0,
              "★ ①d **0 条** ⇒ **0**（`.c:62` 的 else 支；原版这一支什么都不写）");
            C(MissionsTab.GridMissionLayoutLogic.ColumnCount(4) == 0,
              "★ ①e **4 条以上**也落到 else ⇒ **0**（原版只认 1/2/3 三档）");

            // `.c:100` 那个开关 = `columns <= 全局序号`：2 列时前两条 false、第 3 条起 true
            C(!MissionsTab.GridMissionLayoutLogic.WritesSwitch(2, 0)
              && !MissionsTab.GridMissionLayoutLogic.WritesSwitch(2, 1)
              && MissionsTab.GridMissionLayoutLogic.WritesSwitch(2, 2),
              "★ ①f 那个布尔 = `columns <= index`（2 列 ⇒ 序号 0/1 为假、2 起为真）—— 灭自证：改成 `<` ⇒ 第 3 条红");
        }

        // ============================================================ ② 信号触发器挂在【哪条泛型重载】上（账 `:702 / :704`）
        //   判据：`decomp_generic_trial/out_b94/SignalEventTrigger_object_.Register__12549a0.c`
        //     `… Scaffold_Core_Events_Signal__Register<object>(uVar2, …)` 参数 = `new Action<object>(this.TryTrigger)`
        //   ⇒ `Signal<T>.Register`，`T` = 触发器的类型参数 ⇒ 订阅必须落在 **`Signal<ProbeSignal>` 那本台账**里。
        static void SignalTriggerOverload(Action<bool, string> C)
        {
            var bus = LiveOpsSignal.Bus;
            // 前提（硬）：强类型总线在（否则下面的计数口不存在 —— ⛔ 不用 if 包住结论）
            C(bus != null, "②（前提）`LiveOpsSignal.Bus` 是强类型总线（`ListenerCount<T>()` 可读）");
            if (bus == null)
            {
                // 前提不成立时**明确报红到底**，把剩余几条也数进去（⛔ 静默跳过）
                C(false, "②a 前提不成立 ⇒ 「挂在哪条泛型重载上」无法验证（这一批不算通过）");
                C(false, "②b 同上");
                C(false, "②c 同上");
                C(false, "②d 同上");
                return;
            }

            int before = bus.ListenerCount<ProbeSignal>();
            int beforeOther = bus.ListenerCount<EventExpiredSignal>();   // 对照组：**别的** T 的台账不该动

            int cb = 0;
            var t = new ProbeTrigger { Accept = 7 };
            t.Register(delegate { cb++; });

            C(bus.ListenerCount<ProbeSignal>() == before + 1,
              "★ ②a `Register` 把 `TryTrigger` 挂进 **`Signal<ProbeSignal>`** 那本台账（+1）—— 灭自证：挂到别的泛型重载 / 别的 T 上 ⇒ 这本台账不变、红");
            C(bus.ListenerCount<EventExpiredSignal>() == beforeOther,
              "★ ②b 同一刻 **`Signal<EventExpiredSignal>`** 那本台账**不变**（挂对了类型，没挂串）");

            LiveOpsSignal.Raise(new ProbeSignal { Id = 1 });                 // 不匹配
            C(cb == 0 && t.Validated == 1,
              "★ ②c 不匹配的信号：`ValidateSignal` **被调到**（先校验、`+0x198`）且回调**不打**（`.c` TryTrigger 的 `cVar2 != '\\0'` 闸）");

            LiveOpsSignal.Raise(new ProbeSignal { Id = 7 });                 // 匹配
            C(cb == 1, "★ ②d 匹配的信号 ⇒ 回调恰好 1 次");

            t.Unregister();
            C(bus.ListenerCount<ProbeSignal>() == before,
              "★ ②e `Unregister` 把同一条台账清回原数（判据：`Unregister` 不碰 `callback`，只 `Signal<T>.Unregister(同一个 ∵方法组)`)");

            LiveOpsSignal.Raise(new ProbeSignal { Id = 7 });                 // 摘掉之后再发一次
            C(cb == 1,
              "★ ②f 摘掉之后再发同一信号 ⇒ 回调**不再增长**（灭自证：只 Register 不 Unregister ⇒ 这里会变成 2、红）");
        }

        // ============================================================ ③ 周常倒计时的描述串按「已领/未领」分档（账 `:510`）
        //   判据：`decomp_full/MissionTimerDisplay__Update.c` ——
        //     `text = progress.IsComplete() ? GetTranslation("Missions/CompleteCounter") : GetTranslation("Missions/CollectCounter")`
        //   ⇒ 每日行 / 登录卡 / 骷髅卡早就按这条分档，而**周常那一格原来写死 `Missions/CollectCounter`**
        //      （2026-10-29 `B94_B4` 修：`DailyData.WeeklyEndsIn()` + `MissionsTab` 的 `TimerDisplay.Attach` 都改走分档）。
        //   ⚠️ 这里**只碰 public 面**：`MissionTimerDisplay` 是 `internal`（Editor 程序集看不见），
        //      所以期望值取**官方词条正文**（`Loc.T(键).Replace("{0}","")`），⛔ 不引用我们的内部常量。
        static void WeeklyTimerDescription(Action<bool, string> C)
        {
            const string CollectKey = "Missions/CollectCounter";     // 原版 `.c` 里的键名（不是我们编的）
            const string CompleteKey = "Missions/CompleteCounter";
            string collectBody = Loc.T(CollectKey) == null ? null : Loc.T(CollectKey).Replace("{0}", "");
            string completeBody = Loc.T(CompleteKey) == null ? null : Loc.T(CompleteKey).Replace("{0}", "");

            // 前提（硬）：两个官方词条都真的取到、正文非空且不同（否则下面两条无从区分）
            C(!string.IsNullOrEmpty(collectBody) && !string.IsNullOrEmpty(completeBody)
              && collectBody != completeBody,
              "③（前提）官方表里 `" + CollectKey + "` 与 `" + CompleteKey + "` 的正文都非空且不同（"
              + collectBody + " / " + completeBody + "）");
            if (string.IsNullOrEmpty(collectBody) || string.IsNullOrEmpty(completeBody) || collectBody == completeBody)
            {
                C(false, "③a 前提不成立 ⇒ 周常倒计时的分档无法验证（不算通过）");
                C(false, "③b 同上");
                C(false, "③c 同上");
                return;
            }

            // 行为：`DailyData.WeeklyEndsIn()` 必须随「已领」态换串
            bool wasClaimed = DailyData.WeeklyClaimed();
            bool wasCollectable = DailyData.CanCollectWeekly();
            string unclaimed, claimed;
            try
            {
                DailyData.ForceWeeklyStateForTest(DailyData.State.InProgress);
                unclaimed = DailyData.WeeklyEndsIn();
                DailyData.ForceWeeklyStateForTest(DailyData.State.Claimed);
                claimed = DailyData.WeeklyEndsIn();
            }
            finally
            {
                // 还原：只分得出「已领 / 可领 / 其它」三档 ⇒ 按进去时的两个读口还原（避免污染同宿主的其它自检）
                DailyData.ForceWeeklyStateForTest(wasClaimed ? DailyData.State.Claimed
                                                : (wasCollectable ? DailyData.State.Collectable : DailyData.State.InProgress));
            }

            C(!string.IsNullOrEmpty(unclaimed) && unclaimed.Contains(collectBody),
              "★ ③a 周常**未领** ⇒ 串里是 `Missions/CollectCounter` 的正文（原版 `.c` 的 else 支）；实得「" + unclaimed + "」");
            C(!string.IsNullOrEmpty(claimed) && claimed.Contains(completeBody),
              "★ ③b 周常**已领** ⇒ 串里是 `Missions/CompleteCounter` 的正文（同一条判据的 if 支）；实得「" + claimed + "」"
              + " —— 灭自证：改回写死 `CollectCounter` ⇒ 这一条红");
            C(unclaimed != claimed,
              "★ ③c 两态**不同串**（两条相反方向的断言合起来 ⇒ 只改一侧也躲不过）");
        }
    }
}
