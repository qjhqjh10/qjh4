// B142_E34_Assert.cs — 执行写手 `B142-E34`（第一百二十九会话 · 2026-11-04）· 尾B 三域里的 P1 / P2 + EventBadge 绑定口
//
// ============================ 它盯的是什么 ============================
//  P1（账 `未分类_其他:281`）：`Shell/Inventory.cs` 的 `EphemeralState.FormatTime()` 原来**自造** `Xd Yh Zm`；
//      原版 = `SupportMethods.GetTimeString(ts, eventTimer:true)` + 两次 `Replace`。
//      判据 = `d:/2/tools/decomp_full/EphemeralState__FormatTime.c`（`GetTimeString(ts,1,0,0,0)` 后
//      `Replace("0m","")` → `Replace("0h","")`，**没有 `Trim`**；ILSpy 同文 → `out_cpp2il_dev_ilspy/EphemeralState.cs:165-171`）。
//      逐句移植早就在 `Shell/TimerDisplay.cs:164`（`FormatTimeString`，三个开关语义照 `.c`）⇒ P1 = 改成一行转发。
//
//  P2（账 `日常_奖励:549` 残差①）：`Shell/MainMenuRuntime.cs` 的 `NavTimerText` 原来**自造**三档串。
//      原版 = `TimeHelper.FormatTimeSpan(ts, showDays = true, hide0Values = false)`
//           = 15 行纯转发 `GetTimeString(ts, showDays, 0, hide0Values, 0)`
//           （`decomp_full/TimeHelper__FormatTimeSpan.c` · `decomp_full/SupportMethods__GetTimeString.c` 170 行）；
//      活动钮那颗 `Timer`（`TimerDisplay`）自己的调用点也是 `GetTimeString(remaining, 1, 0, 0, 0)`
//      ⇒ 两条判据同档 ⇒ P2 = 转发 `TimerDisplay.FormatTimeString(left, eventTimer: true)`。
//      ⚠️ 剩 6 个 I2 词条 token（`DAT_1842c1b10` / `DAT_1842c1f10` / `DAT_1842c2210` / `DAT_1842c1c10` /
//      `DAT_1842c2010` / `DAT_1842c2310`）**身份仍未从二进制里逐个读出**；其中 DH/HM/MS 三条已由
//      官方本地化表 `MainMenu/Time/{DH,HM,MS}` 逐字核过（见 `Shell/TimerDisplay.cs` 文件头），
//      三条 `_Only` 仍是「按单位机械拼」（`TimerDisplay` 里已标「我们挑的」）⇒ 不在本轮范围。
//
//  EventBadge（账 `日常/奖励:12`）：补的是**绑定口本身的验证**。原版「绑定」= prefab 序列化
//      （`Instantiate(ComponentReference<EventBadge>(GUID).Load(), transform)`，`decomp_full/EventBadgeController__Awake.c`），
//      而该 prefab **本机拿不到**（2026-11-04 三源复核：离线 `assets_full` 字段名 0 命中 · AssetRipper 导出只有脚本 ·
//      安卓 1.35.0 `UnityDataAssetPack.apk` 里 460 个引用脚本可解析 459 个、722 个 `MonoScript` **无 `EventBadge`**）
//      ⇒ **节点路径 / 几何判据为空，⛔ 不伪造视觉树**（铁律 11 例外① + 18·0）。
//      本文件因此只断三件**有判据**的事：① `Bind(...)` 这个口真的把 6 个件收下（不是空函数）；
//      ② 没绑时 `ShowNotification` **不播也不静默**（出声）；③ 控制器建几个就有几个 active 子件（计数 → active）。
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · 把 `FormatTime()` 改回 `((long)ts.TotalDays) + "d " ...` ⇒ A1 红（不再是 `FormatTimeString` 的串）。
//  · 把 `FormatTime()` 的 `Replace("0m","")` 删掉 ⇒ A1 红（原版那两次 Replace 是这条链的一部分）。
//  · 给 `FormatTime()` 加回 `.Trim()` ⇒ A1 红（原版 `.c` 没有 Trim —— 这条就是防「顺手加个更漂亮的」）。
//  · 把 `NavTimerText` 改回三档自造 ⇒ A3/A4 红（旧写法给 `3d 5h 7m`，新写法给 `3d 5h`）。
//  · 把 `EventBadge.Bind` 改成空函数（或漏赋某个字段）⇒ B1 红。
//  · 把没绑时那句 `WarnOnce + return` 删掉（让它照样起时间线）⇒ B2 红。
//  · `BuildBadge` 不再返回/不建子件 ⇒ B4/B5 红。
//
// ============================ 怎么接（本文件不碰宿主聚合）============================
//  `public static void Run();` —— **零前提**（只在临时 GameObject 上建件，收尾 `DestroyImmediate`）。
//  挂法（由调度台决定）：同一物理行追加
//      B142_E34_Assert.Run(); pass += B142_E34_Assert.Pass; fail += B142_E34_Assert.Fail;
//  判绿红：按行首 `B142E34 ✓` / `B142E34 ✗` 数（⛔ 别 `grep -c ✗`）。

using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B142-E34`：P1/P2 时间串单源化 + `EventBadge.Bind` 绑定口回归断言。</summary>
public static class B142_E34_Assert
{
    const string Tag = "B142E34";
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static string Src(string rel)
    {
        try { return File.ReadAllText(Application.dataPath + "/CardPresentation/" + rel); }
        catch (Exception e) { Debug.LogError(Tag + " 读不到 `" + rel + "`：" + e.Message); return ""; }
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        GroupA_TimeSingleSource();
        GroupB_EventBadgeBindPort();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail);
    }

    // ==================================================================
    //  A · 时间串单源（P1 = `EphemeralState.FormatTime` · P2 = `NavTimerText`）
    // ==================================================================
    static void GroupA_TimeSingleSource()
    {
        // ---- A1 · P1：`FormatTime()` 的输出 == `GetTimeString(ts, eventTimer:true)` + 那两次 Replace（逐档比）----
        //   起点固定（不走 `End − ServerNow` 那一支）⇒ 期望值完全确定，没有时钟抖动。
        long startMs = 1700000000000L;                       // 2023-11-14T22:13:20Z（UTC，只当基准）
        long[] durationsMs =
        {
            3L * 86400000L + 5L * 3600000L + 30L * 60000L,   // 3d 5h 30m ⇒ 天档：`3d 5h`（分钟根本不出现）
            5L * 3600000L + 30L * 60000L,                    // 5h 30m     ⇒ 小时档：`5h 30m` → `Replace("0m") ⇒ 5h 3`
            30L * 60000L + 15000L,                           // 30m 15s    ⇒ 分钟档：`30m 15s` → `3 15s`
            45000L,                                          // 45s        ⇒ `0m 45s` → ` 45s`
        };
        for (int i = 0; i < durationsMs.Length; i++)
        {
            var st = new EphemeralState { StartTimeMs = startMs, DurationMs = durationsMs[i] };   // ⚠️ 构造 `EphemeralState(long)` 的实参是**时长**、不是起点（`Shell/Inventory.cs:759`）⇒ `B150` #15-#18 就地订正（原写 `new EphemeralState(startMs)` ⇒ `StartTimeMs` 恒 0、落 `TimeSpan.Zero`）
            string got = st.FormatTime();
            var ts = TimeSpan.FromMilliseconds(durationsMs[i]);
            string want = TimerDisplay.FormatTimeString(ts, eventTimer: true).Replace("0m", "").Replace("0h", "");
            Check(got == want, "A1 档 " + i + "（" + durationsMs[i] + " ms）`FormatTime()` = `TimerDisplay.FormatTimeString` + 两次 Replace"
                             + "｜got=[" + got + "] want=[" + want + "]");
        }
        // A1b · 灭自证：旧实现（`Xd Yh Zm` 三档）在 3d 5h 30m 上给 `3d 5h 30m`（ASCII，"d/h/m" 是硬编码的）
        //   ⇒ 新输出**必须**不是它（⛔ 不断具体文案：`FormatTimeString` 会按 `Loc.Current` 换中文格式）
        var ts3 = TimeSpan.FromDays(3) + TimeSpan.FromHours(5) + TimeSpan.FromMinutes(30);
        var s3 = new EphemeralState { StartTimeMs = startMs, DurationMs = (long)ts3.TotalMilliseconds }.FormatTime();
        string want3 = TimerDisplay.FormatTimeString(ts3, eventTimer: true).Replace("0m", "").Replace("0h", "");
        Check(s3 != "3d 5h 30m" && s3 == want3,
              "A1b ★ 灭自证：3d5h30m 不再输出旧格式 `3d 5h 30m`，而是原版那条链的结果｜实得 [" + s3 + "] want=[" + want3 + "]");
        // A1c · 原版**不带 `Trim`**（`.c` / ILSpy 都只做两次 Replace）：只要那条链留下了首尾空白，就必须原样留着。
        //   ⚠️ 中文表下 `Replace("0m","")` 不一定命中（`0分钟` 里没有 ASCII `m`）⇒ 那条「留白」断言只在真出现留白时才断。
        var ts45 = TimeSpan.FromSeconds(45);
        var s45 = new EphemeralState { StartTimeMs = startMs, DurationMs = 45000L }.FormatTime();
        string want45 = TimerDisplay.FormatTimeString(ts45, eventTimer: true).Replace("0m", "").Replace("0h", "");
        Check(s45 == want45 && (want45 == want45.Trim() || s45 != s45.Trim()),
              "A1c 45s 档 = 原版那条链；链上有留白时**不 Trim**｜got=[" + s45 + "] want=[" + want45 + "]");

        // A2 · 源码：`Inventory.cs` 只留一行转发，旧的拼接式**不在**了
        var inv = Src("Shell/Inventory.cs");
        Check(inv.Contains("string s = TimerDisplay.FormatTimeString(ts, eventTimer: true);"),
              "A2 `Inventory.cs` 的 `FormatTime()` 已改成一行转发 `TimerDisplay.FormatTimeString(ts, eventTimer: true)`");
        Check(!inv.Contains("+ \"d \" + ts.Hours"),
              "A2b ★ `Inventory.cs` 里旧的 `Xd Yh Zm` 拼接式已删除（⛔ 不许两套并存）");
        Check(inv.Contains("return s.Replace(\"0m\", \"\").Replace(\"0h\", \"\");"),
              "A2c 那两次 `Replace` 保留、且**没有** `.Trim()`（= 原版 `EphemeralState__FormatTime.c` 的形状）");

        // ---- A3 · P2 源码：`NavTimerText` 转发、旧三档不在 ----
        var mm = Src("Shell/MainMenuRuntime.cs");
        Check(mm.Contains("return TimerDisplay.FormatTimeString(left, eventTimer: true);"),
              "A3 `MainMenuRuntime.NavTimerText` 已改成转发 `TimerDisplay.FormatTimeString(left, eventTimer: true)`");
        Check(!mm.Contains("int d = (int)left.TotalDays"),
              "A3b ★ `NavTimerText` 里旧的三档自造式已删除");

        // ---- A4 · 运行时：`NavTimerText` 的输出 == `FormatTimeString`（同一条链，不是同义反复）----
        //   取「半秒后 + 3d5h7m3s」⇒ `left` 的秒内小数 ≈ 0.5，两次取时钟不会跨秒/跨分钟边界。
        var now = DateTime.UtcNow;
        var end = now.AddMilliseconds(500).AddDays(3).AddHours(5).AddMinutes(7).AddSeconds(3);
        string nav = MainMenuRuntime.NavTimerTextForTest(end);
        string navWant = TimerDisplay.FormatTimeString(end - DateTime.UtcNow, eventTimer: true);
        Check(nav == navWant, "A4 `NavTimerText(3d5h7m3s)` = `TimerDisplay.FormatTimeString(left, eventTimer:true)`｜got=["
                            + nav + "] want=[" + navWant + "]");
        Check(nav != "3d 5h 7m", "A4b ★ 灭自证：不再是旧三档串 `3d 5h 7m`｜实得 [" + nav + "]");

        // A5 · 严格边界原样：正好 24h 落**小时档**（`TotalDays > 1` 是严格大于），24h1s 已经进**天档**
        //   ⇒ 两者必须**不同串**（`B150` #21 就地订正：原写「与 24h1s 同串（都丢秒）」是把 `>` 的后果写反了）。⛔ 不断具体文案。
        string h24 = TimerDisplay.FormatTimeString(TimeSpan.FromHours(24), eventTimer: true);
        string h24b = TimerDisplay.FormatTimeString(TimeSpan.FromHours(24) + TimeSpan.FromSeconds(1), eventTimer: true);
        string h25 = TimerDisplay.FormatTimeString(TimeSpan.FromHours(25), eventTimer: true);
        Check(h24 != h24b && h24b != h25 && h24 == string.Format(Loc.T("MainMenu/Time/H_Only"), 24),
              "A5 24h 落**小时档**（`TotalDays > 1` 严格大于）⇒ 与 24h1s 必须不同档；正控 = 24h 走官方 `MainMenu/Time/H_Only` 模板｜24h=[" + h24 + "] 24h1s=[" + h24b + "] 25h=[" + h25 + "]");
    }

    // ==================================================================
    //  B · `EventBadge.Bind` 绑定口（prefab 不可得 ⇒ 只断「口」与「计数→active」）
    // ==================================================================
    static void GroupB_EventBadgeBindPort()
    {
        var root = new GameObject("B142E34_EventBadge");
        try
        {
            // ---- B1 · `Bind(...)` 真的把 6 个件收下 ----
            var badgeGo = new GameObject("badge");
            badgeGo.transform.SetParent(root.transform, false);
            var badge = badgeGo.AddComponent<EventBadge>();

            var tex = CardArt.Solid();
            Check(tex != null, "B0-前提①：`CardArt.Solid()` 取得到（下面两个 `ImageQuad` 靠它建）");
            if (tex == null) return;

            var holder = ImageQuad.Create(root.transform, tex, Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "holder");
            var image = ImageQuad.Create(root.transform, tex, Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "image");
            var text = Label.Create(root.transform, "probe", Vector3.zero, 3, Color.white, new Vector2(0.5f, 0.5f), "text");
            Check(holder != null && image != null && text != null,
                  "B0-前提②：`ImageQuad.Create` / `Label.Create` 都建出来了（否则下面「6 个件都收下」等于空断）");
            var drawerHolder = new GameObject("drawerHolder").transform; drawerHolder.SetParent(root.transform, false);
            var icon = new GameObject("icon").transform; icon.SetParent(root.transform, false);
            var onClick = new UnityEngine.Events.UnityEvent();

            badge.Bind(holder, text, image, drawerHolder, icon, onClick);
            Check(badge.notificationHolder == holder && badge.notificationText == text && badge.notificationImage == image
                  && badge.drawerHolder == drawerHolder && badge.icon == icon && badge.buttonOnClick == onClick,
                  "B1 `EventBadge.Bind(...)` 把 6 个件**逐个**收下（holder/text/image/drawerHolder/icon/onClick）");

            // ---- B2 · 没绑的壳：`ShowNotification` 不播、且出声（⛔ 不静默）----
            var bareGo = new GameObject("bare");
            bareGo.transform.SetParent(root.transform, false);
            var bare = bareGo.AddComponent<EventBadge>();
            bare.ShowNotification(new ToastNotifications.Context { Text = "probe", IconArt = "probe" });
            Check(!bare.IsPlayingForTest, "B2 未绑定（`notificationHolder == null`）⇒ `ShowNotification` **不起时间线**（原版会 NRE；我们出声后 return）");

            // ---- B3 · 绑好的件：`ShowNotification` 真能起播、`Tick` 走完会停 ----
            badge.ShowNotification(new ToastNotifications.Context { Text = "probe", IconArt = "probe" });
            Check(badge.IsPlayingForTest, "B3 绑定后 `ShowNotification` 起播（时间线在跑）—— 证明 B1 的绑定**不是摆设**");
            badge.TickForTest(10f);
            Check(!badge.IsPlayingForTest, "B3b 推 10 秒后时间线自己收尾停下（8 拍总长 3.55s）");

            // ---- B4/B5 · 控制器：建几个就有几个 active 子件（「count → active」这一条）----
            var ctrlGo = new GameObject("B142E34_Controller");
            try
            {
                var ctrl = ctrlGo.AddComponent<EventBadgeController>();
                int n0 = ctrl.EnsureBadges();
                Check(n0 == 0 && ctrlGo.transform.childCount == 0,
                      "B4 离线目录下 `EnsureBadges()` = 0 且**一个子件都不建**（13 份 SO 无一带 `EventComponents[25]`）｜实得 n=" + n0
                      + " children=" + ctrlGo.transform.childCount);

                var ev = LiveOpsLocalCatalog.Events.Count > 0 ? LiveOpsLocalCatalog.Events[0] : null;
                Check(ev != null, "B5-前提①：离线活动目录非空（`LiveOpsLocalCatalog.Events`）");
                if (ev != null)
                {
                    var lo = new LocalLiveOpsEvent(ev);
                    var b1 = ctrl.BuildBadgeForTest(lo);
                    var b2 = ctrl.BuildBadgeForTest(lo);
                    Check(b1 != null && b2 != null && b1.LiveOps == lo,
                          "B5 `BuildBadgeForTest` 建出 `EventBadge` 并 `Initialize`（`LiveOps` = 传入事件）");
                    Check(ctrlGo.transform.childCount == 2,
                          "B5b 建 2 个 ⇒ 控制器下正好 2 个子件（计数跟着建了几个走）｜实得 " + ctrlGo.transform.childCount);
                    Check(b1 != null && b1.gameObject.activeSelf && b2 != null && b2.gameObject.activeSelf,
                          "B5c 两个角标 GameObject 都是 **active** 的（建出来 = 亮着）");
                    // B6 · 口径断言：视觉节点仍为 null —— 这是**如实记录**（prefab 本机拿不到，铁律 11 例外①）。
                    //   哪天有人把节点接上了，这条会红 ⇒ 提醒同步账 `日常/奖励:12` 与本报告。
                    Check(b1 != null && b1.notificationHolder == null && b1.notificationText == null && b1.icon == null,
                          "B6 口径：内置路径建出的仍是**逻辑壳**（视觉节点 null = prefab 本机拿不到，⛔ 不是已复刻视觉）");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(ctrlGo); }
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
