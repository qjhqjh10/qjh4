// B110_W4_Assert.cs — 执行代理 `B110_W4`（2026-10-31）· 账 `B-44`（`N34`）
//
// ============================ 它盯的是什么 ============================
//  原版 `LiveOpsUtility.IsTimedEvent(ILiveOps, out long endDate)` 在**本仓 0 实现**（`grep -rn IsTimedEvent Assets/*.cs`
//  只命中 5 处注释）；而它唯一的算式那一半 `EventFilter.GetExpirationTime` **全仓 0 调用者**。
//  本件落地 = `Shell/LiveOpsUtilityPartial_B110W4.cs` 的 `LiveOpsUtility.IsTimedEvent`，
//  判据 = `decomp_full/LiveOpsUtility__IsTimedEvent.c` 全文（可读互校 = `cpp2il/out_il2csharp/Assembly-CSharp/LiveOpsUtility.cs:18-50`）。
//
// ============================ 逐支为什么必须这么断 ============================
//  原版的判定是**三支**，不是两支：
//    ① `activationType != Permanent`            ⇒ endDate = `dates.endDate`，返回 true（**不读 hasDuration 也能定**）
//    ② `Permanent` 且 `!filter.hasDuration`      ⇒ endDate = long.MaxValue，返回 **false**
//    ③ `Permanent` 且 ` filter.hasDuration`      ⇒ endDate = long.MaxValue → 交给下面的 `Math.Min`
//  然后（①②③ 之后统一一句）：`if (hasDuration) endDate = Math.Min(endDate, filter.GetExpirationTime(liveOps));`
//  ⇒ `A3`（OnDate + 时长关）是**区分「只看 hasDuration」那种写错的判别式**；`A5`（endDate 更早）是**区分
//    「直接用 GetExpirationTime」那种写错的判别式**。这两条少一条，另外那条写错也能全绿。
//
// ============================ 🔴 灭自证（🧨 每条文案里都写了「改哪一处会让它红」）============================
//  · 🧨 **把 `EventFilter.GetExpirationTime` 改成恒 `long.MaxValue`**（`Shell/LiveOpsFilter.cs:866` 那一行）
//        ⇒ `A2` / `A4` / `C1` **同时红**（`A5` 与 `A3` 仍绿 —— 它们本来就该不受这句话影响，这正是判据分得开）。
//  · 🧨 把 `IsTimedEvent` 的 `Math.Min` 写成「直接用 `GetExpirationTime`」 ⇒ `A5` 红（实得「下次刷新时刻」而不是 endDate）。
//  · 🧨 把「计时活动」的判据写成只看 `hasDuration`（丢掉 `activationType != Permanent`）⇒ `A3` 红。
//  · 🧨 把 `Permanent + !hasDuration` 那一支的返回值写成 true ⇒ `A1` 红（调用点会因此显示一个 `long.MaxValue` 的倒计时）。
//  · 🧨 在 `Permanent` 那一支顺手补一句 `dates` 判空/读取 ⇒ `A7` 红（原版那一支**一个字节都不读 dates**）。
//  · 🧨 把 32 位乘法「修」成 long（`GetExpirationTime` 的 `unchecked` 去掉）⇒ `C1` 红（原版就是溢出照抄）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `CardPresentation/Editor/ShellScene.cs` 的活动那一段里（与 `B93D3_Assert.Run(...)` 同族），**同一物理行内追加**：
//      B110W4_Assert.Run(); pass += B110W4_Assert.Pass; fail += B110W4_Assert.Fail;
//  本件**不建 GameObject / 不建资产 / 不需要场景**（只用 POCO 夹具），任何一个宿主都能跑。
//
// ============================ 判据 / 报告 ============================
//  · 原版：`d:/2/tools/decomp_full/LiveOpsUtility__IsTimedEvent.c`（全文）· `…EventFilter__GetExpirationTime.c`
//  · 调用点：`BaseOfferPopup__SetTimer.c:21` · `BaseOfferPopup__SetEventData.c:105` · `ItemShopTab__CheckTimer.c:19`
//            · `OpenEventButton__SetButtonInfo.c:72` · `ShopOfferContainer__OnInitialize.c:26`
//  · 本件报告：`资料/完全复刻/报告/B110_W4.md`
using System;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B110_W4`：`LiveOpsUtility.IsTimedEvent` 逐支（含 `Math.Min` 那一半）与 32 位溢出。</summary>
public static class B110W4_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B110W4] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 夹具

    /// <summary>固定基准毫秒（2023-11-14T22:13:20Z）—— ⛔ 不用 `DateTime.Now`（自检要可复现）。</summary>
    const long T0 = 1700000000000L;
    const long HOUR = 3600000L;

    /// <summary>最小的 `ILiveOps` 载体：`LiveOpsEvent&lt;LiveOpsSave, LiveOpsEventData&gt;` 已经实现了 `ILiveOps` 的全部成员，
    /// 这里只把 data/save 装进去（走原版的 `LoadEventData` 那一跳，⛔ 不绕开它直接写字段 —— 字段是 protected set）。</summary>
    sealed class ProbeEvent : LiveOpsEvent<LiveOpsSave, LiveOpsEventData>
    {
        public ProbeEvent(LiveOpsEventData d, LiveOpsSave s) { LoadEventData(d, s, null); }
    }

    /// <summary>造一份事件数据（`dates.endDate` / `filter.hasDuration` / `filter.eventDuration` 三格就是本件要压的全部输入）。</summary>
    static LiveOpsEventData MakeData(LiveOpsActivationType t, long endDate, bool hasDuration, int durationHours)
    {
        return new LiveOpsEventData
        {
            eventId = "b110w4_probe",
            activationType = t,
            dates = new LiveOpsEventTimeData { endDate = endDate },
            filter = new LiveOpsEventFilter { hasDuration = hasDuration, eventDuration = durationHours },
        };
    }

    static ILiveOps MakeEvent(LiveOpsEventData d, long triggerTime)
    {
        return new ProbeEvent(d, new LiveOpsSave { eventId = "b110w4_probe", triggerTime = triggerTime });
    }

    // ============================================================ 入口
    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();

        MenuCheck.Section(_sink, "B110_W4 `LiveOpsUtility.IsTimedEvent`（原版 N34 / 账 B-44）");

        SectionA_Branches();
        SectionB_NullGuards();
        SectionC_Int32Overflow();
        SectionD_Discriminators();

        Debug.Log(_sink.P + "B110W4 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A 三支 + Math.Min

    static void SectionA_Branches()
    {
        MenuCheck.Section(_sink, "A 原版三支 + `Math.Min` 那一半（判据 = `LiveOpsUtility__IsTimedEvent.c` 全文）");

        // ---- A1 永久 + 时长关 ⇒ 不是计时活动 ----
        {
            var ev = MakeEvent(MakeData(LiveOpsActivationType.Permanent, T0 + 99 * HOUR, false, 24), T0);
            long end;
            bool timed = LiveOpsUtility.IsTimedEvent(ev, out end);
            Tr(!timed, "A1 ★★ `Permanent` + `hasDuration=false` ⇒ **不是计时活动**（返回 false）"
                     + "｜🧨 把这一支的返回值写成 true ⇒ A1 红（调用点会显示一个 long.MaxValue 的倒计时）");
            C(end, long.MaxValue, "A1b ★★ 同一支的 `endDate` = `long.MaxValue`（原版 `*param_2 = 0x7fffffffffffffff` 后 return 0）"
                                + "｜🧨 回 0 或丢着不写 ⇒ A1b 红");
        }

        // ---- A2 永久 + 时长开 ⇒ 常量基底下靠 GetExpirationTime 定 ----
        {
            var ev = MakeEvent(MakeData(LiveOpsActivationType.Permanent, T0 + 99 * HOUR, true, 2), T0);
            long end;
            bool timed = LiveOpsUtility.IsTimedEvent(ev, out end);
            Tr(timed, "A2 ★★ `Permanent` + `hasDuration=true` ⇒ **是计时活动**（返回 true）");
            C(end, T0 + 2 * HOUR,
              "A2b ★★★ `endDate` = `triggerTime + 2h`（基底 = long.MaxValue ⇒ `Math.Min` 取的是 `GetExpirationTime`）"
            + "｜🧨 **把 `EventFilter.GetExpirationTime` 改成恒 `long.MaxValue` ⇒ 本条红**（实得 long.MaxValue）");
        }

        // ---- A3 OnDate + 时长关 ⇒ 仍然是计时活动（判别式：光看 hasDuration 会写错） ----
        {
            var ev = MakeEvent(MakeData(LiveOpsActivationType.OnDate, T0 + 10 * HOUR, false, 24), T0);
            long end;
            bool timed = LiveOpsUtility.IsTimedEvent(ev, out end);
            Tr(timed, "A3 ★★ `OnDate` + `hasDuration=false` ⇒ **仍然是计时活动**（原版判据是 `activationType != Permanent` **或** "
                    + "`hasDuration`）｜🧨 把判据写成「只看 hasDuration」⇒ A3 红");
            C(end, T0 + 10 * HOUR, "A3b ★★ `endDate` = `dates.endDate`（这一支**不读** GetExpirationTime）"
                                + "｜🧨 无条件调 GetExpirationTime ⇒ A3b 红");
        }

        // ---- A4 OnDate + 时长开、刷新时刻更早 ⇒ Math.Min 取刷新时刻 ----
        {
            var ev = MakeEvent(MakeData(LiveOpsActivationType.OnDate, T0 + 10 * HOUR, true, 2), T0);
            long end;
            bool timed = LiveOpsUtility.IsTimedEvent(ev, out end);
            Tr(timed, "A4 `OnDate` + `hasDuration=true` ⇒ 计时活动");
            C(end, T0 + 2 * HOUR,
              "A4b ★★ `Math.Min` 取**更早的**那个：`triggerTime+2h` < `endDate` ⇒ `endDate` = `triggerTime+2h`"
            + "｜🧨 `GetExpirationTime` 改成恒 `long.MaxValue` ⇒ A4b 红");
        }

        // ---- A5 OnDate + 时长开、endDate 更早 ⇒ Math.Min 取 endDate（方向判别式） ----
        {
            var ev = MakeEvent(MakeData(LiveOpsActivationType.OnDate, T0 + 1 * HOUR, true, 2), T0);
            long end;
            bool timed = LiveOpsUtility.IsTimedEvent(ev, out end);
            Tr(timed, "A5 `OnDate` + `hasDuration=true`（endDate 更早）⇒ 计时活动");
            C(end, T0 + 1 * HOUR,
              "A5b ★★★ `Math.Min` 的另一半：`endDate`(1h) < 刷新时刻(2h) ⇒ 取 **`endDate`**"
            + "｜🧨 把 `Math.Min` 写成「直接用 `GetExpirationTime`」⇒ A5b 红（实得 T0+2h）");
        }

        // ---- A6 OneTime 也走 `!= Permanent` 那一支 ----
        {
            var ev = MakeEvent(MakeData(LiveOpsActivationType.OneTime, T0 + 5 * HOUR, false, 24), T0);
            long end;
            bool timed = LiveOpsUtility.IsTimedEvent(ev, out end);
            Tr(timed, "A6 `OneTime` ≠ Permanent ⇒ 计时活动（原版只有 `Permanent` 走短路支）");
            C(end, T0 + 5 * HOUR, "A6b `endDate` = `dates.endDate`");
        }

        // ---- A7 Permanent 支根本不读 dates（原版可达的一支，别补判空） ----
        {
            var d = MakeData(LiveOpsActivationType.Permanent, T0 + 99 * HOUR, true, 2);
            d.dates = null;                       // ★ 原版这一支不会解引用 +0x28
            var ev = MakeEvent(d, T0);
            long end;
            bool timed = LiveOpsUtility.IsTimedEvent(ev, out end);
            Tr(timed, "A7 ★★ `Permanent` + `hasDuration=true` 且 **`dates == null`** ⇒ 仍然是计时活动"
                    + "（原版这一支一个字节都不读 `dates`）｜🧨 在这一支补 `dates` 判空 ⇒ A7 红（返回 false）");
            C(end, T0 + 2 * HOUR, "A7b 同上：`endDate` = `triggerTime+2h`，与 `dates` 无关");
        }
    }

    // ============================================================ B 空/坏数据（原版 NRE 的四格）

    static void SectionB_NullGuards()
    {
        MenuCheck.Section(_sink, "B 原版会 NRE 的四格（本仓口径 = 出声 + 当「不是计时活动」，⛔ 不是「已经一样」）");

        {
            long end;
            bool timed = LiveOpsUtility.IsTimedEvent(null, out end);
            Tr(!timed, "B1 ★ `liveOps == null` ⇒ false（原版 `IsTimedEvent.c` 落到抛异常桩；我们出声走安全支）");
            C(end, long.MaxValue, "B1b 同一支的 `endDate` = `long.MaxValue`");
        }

        {
            var ev = new ProbeEvent(null, null);        // LoadEventData 拒收 null ⇒ GetBaseData() 恒 null
            long end;
            bool timed = LiveOpsUtility.IsTimedEvent(ev, out end);
            Tr(!timed, "B2 ★ `GetBaseData() == null` ⇒ false（原版 NRE）｜🧨 前提：`ProbeEvent(null,null)` 的 Data 真的没被装上");
            C(end, long.MaxValue, "B2b 同一支的 `endDate` = `long.MaxValue`");
        }

        {
            var d = MakeData(LiveOpsActivationType.OnDate, T0 + HOUR, false, 24);
            d.filter = null;                            // 原版 +0x30 为空 ⇒ NRE
            long end;
            bool timed = LiveOpsUtility.IsTimedEvent(MakeEvent(d, T0), out end);
            Tr(!timed, "B3 ★ `data.filter == null` ⇒ false（原版 NRE）｜🧨 把 filter 判空删掉 ⇒ 这里 NullReferenceException 冒到宿主");
            C(end, long.MaxValue, "B3b 同一支的 `endDate` = `long.MaxValue`");
        }

        {
            var d = MakeData(LiveOpsActivationType.OnDate, T0 + HOUR, false, 24);
            d.dates = null;                             // 原版 `activationType != Permanent` 支解引用 +0x28 ⇒ NRE
            long end;
            bool timed = LiveOpsUtility.IsTimedEvent(MakeEvent(d, T0), out end);
            Tr(!timed, "B4 ★ `OnDate` 且 `dates == null` ⇒ false（原版 NRE；注意与 A7 相反 —— 那一支不读 dates）");
            C(end, long.MaxValue, "B4b 同一支的 `endDate` = `long.MaxValue`");
        }
    }

    // ============================================================ C 32 位乘法照抄

    static void SectionC_Int32Overflow()
    {
        MenuCheck.Section(_sink, "C `GetExpirationTime` 的 32 位乘法（`unchecked(eventDuration * 3600000)`）—— 溢出照抄");

        {
            // 600 小时 × 3,600,000 = 2,160,000,000 > int.MaxValue(2,147,483,647) ⇒ unchecked 回绕成负数
            int span = unchecked(600 * 3600000);       // 走变量而不是常量表达式，避免编译期常量折叠的歧义
            long want = (long)span;
            var ev = MakeEvent(MakeData(LiveOpsActivationType.Permanent, 0, true, 600), 0);
            long end;
            bool timed = LiveOpsUtility.IsTimedEvent(ev, out end);
            Tr(timed, "C0 时长 600h ⇒ 计时活动");
            C(end, want,
              "C1 ★★ 32 位乘法溢出照抄：`unchecked(600*3600000)` = " + want + "（负值）"
            + "｜🧨 把 `Shell/LiveOpsFilter.cs` 那句 `unchecked` 改成 long 运算 ⇒ C1 红（实得 2,160,000,000）");
        }
    }

    // ============================================================ D 判别式（两条合起来才封死）

    static void SectionD_Discriminators()
    {
        MenuCheck.Section(_sink, "D 「计时与否」与「打到哪一刻」是**两个独立轴**（少一条就分不开）");

        {
            // 只差 hasDuration：一真一假
            var on = MakeEvent(MakeData(LiveOpsActivationType.Permanent, T0, true, 2), T0);
            var off = MakeEvent(MakeData(LiveOpsActivationType.Permanent, T0, false, 2), T0);
            long e1, e2;
            bool t1 = LiveOpsUtility.IsTimedEvent(on, out e1);
            bool t2 = LiveOpsUtility.IsTimedEvent(off, out e2);
            Tr(t1 && !t2, "D1 ★★ 同一份数据只翻 `hasDuration`：true ⇒ 计时、false ⇒ 不计时"
                        + "（`Permanent` 短路支的判别式；🧨 两支都返 true / 都返 false ⇒ D1 红）");
            C(e1, T0 + 2 * HOUR, "D1b 计时那一支的 endDate = `triggerTime + 2h`");
            C(e2, long.MaxValue, "D1c 不计时那一支的 endDate = `long.MaxValue`");
        }

        {
            // 只差 triggerTime：endDate 必须跟着变（证明真的读了 save）
            var e1 = MakeEvent(MakeData(LiveOpsActivationType.Permanent, 0, true, 2), T0);
            var e2 = MakeEvent(MakeData(LiveOpsActivationType.Permanent, 0, true, 2), T0 + 3 * HOUR);
            long a, b;
            LiveOpsUtility.IsTimedEvent(e1, out a);
            LiveOpsUtility.IsTimedEvent(e2, out b);
            C(b - a, 3 * HOUR, "D2 ★★ 只把 `save.triggerTime` 推后 3h ⇒ `endDate` 同步推后 3h（= `GetExpirationTime` 读的是存档那半）"
                             + "｜🧨 返回一个常量 ⇒ D2 红");
        }

        {
            // 时长关时 triggerTime 完全不影响（证明那一支**没有**调 GetExpirationTime）
            var e1 = MakeEvent(MakeData(LiveOpsActivationType.OnDate, T0 + 9 * HOUR, false, 2), T0);
            var e2 = MakeEvent(MakeData(LiveOpsActivationType.OnDate, T0 + 9 * HOUR, false, 2), T0 + 50 * HOUR);
            long a, b;
            LiveOpsUtility.IsTimedEvent(e1, out a);
            LiveOpsUtility.IsTimedEvent(e2, out b);
            C(b, a, "D3 ★ `hasDuration=false` 时 `triggerTime` **完全不影响** endDate（这一支不调 GetExpirationTime）"
                  + "｜🧨 无条件调 GetExpirationTime ⇒ D3 红");
        }
    }
}
