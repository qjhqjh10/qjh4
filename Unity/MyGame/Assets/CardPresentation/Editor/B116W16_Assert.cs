// B116W16_Assert.cs — 执行代理 `B116_W16`（2026-11-02 · 第一百一十六会话 · 存档线）
//
// ============================ 它盯的是什么 ============================
//  `UI-15` / `B-322` 四个「没走中央存档」的通道里**风险最低的那个：收件箱**（`Shell/InboxData.cs`）。
//  施工面 = `资料/完全复刻/判据/B116_R8_存档切换施工面.md` §1 第 4 行 + §3 表 B（B1…B6）。
//  切之前：`InboxData` 裸 `File.ReadAllText` / 三段「`.tmp` → 删旧 → `File.Move`」，**完全没纳入中央体系**
//          （无 key、无 `Entries` 条目）。切之后：key = `_ours_Inbox`（原版 `GameMessagePersistence` 在服务端、
//          无 `PlayerDataKey` ⇒ 铁律 18·0 允许的自设计门类），读写都走 `SaveCenter.{Read,Save}ThroughLegacy`。
//
// ============================ 判据（本代理 2026-11-02 现读）============================
//  · `SaveCenter.cs:1093` `SaveThroughLegacy`（StoreOn 真 ⇒ 中央 + 双写旧档；StoreOn 假 ⇒ **只写旧档**）
//  · `SaveCenter.cs:1165` `ReadThroughLegacy`（**先中央、再旧档**；中央坏档/缺失 ⇒ 退回旧档原文）
//  · `DailyData.cs:1969 / :2109` = 已切通道的**同一份模板**（本文件 A3 的「逐字段相等」就是它那条验收）
//  · `InboxData.cs:427`（读）/ `:488`（写）—— 本批改的就是这两处
//
// ⚠️ 2026-11-04 更正（`B139_V1`）：`SaveCenter.LegacyMirrorOn`（`Shell/SaveCenter.cs:915`）自 `81ea4fbb` 起是
//    `!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WF_LEGACY_MIRROR"))`（用户 2026-11-03 裁定「关双写」）
//    ⇒ **默认 false**。本夹具原来**没显式打开**、靠旧默认值 `true`，所以 A2 / A2' / A2'' / A3 / C1' 五条全倒。
//    现在 `Run()` 开头**显式 `= true`**、`finally` 还原原值（先例 = `Editor/B102DECK_Assert.cs:132`）。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 A1：把写口退回裸 `File.WriteAllText` ⇒ 中央档不存在 ⇒ A1/A1'/A1''/A3 四条**当场红**。
//  🧨 B1：把读口退回「裸 `File.ReadAllText(旧档)`」⇒ B1 读到 0（旧档那份），期望 777000 ⇒ 红。
//     ⚠️ 只断「读得出来」是抓不到这一点的 —— 所以第 B 段**故意让中央档与旧档的值不同**。
//  🧨 C1：把读口改成「只读中央」⇒ 中央档被 `Delete` 之后读到出厂态 ⇒ C1（claim=0 但 read 必须 true）与 C1' 红。
//  🧨 D1：把 `StoreOn` 那道门弄丢（不管 `SaveCenter.StoreOn` 都去写中央）⇒ 自检常态下会写到玩家真档；
//     这一段用「只注入 `InboxData.OverrideDir`」把它钉住：旧档必须照样写、形状与切换前逐字节同。
//  🧨 双写开关「关着」⇒ `W16-A4'` 断旧档**一个字节都不动** —— 没有这条，整套只剩「开着能写」，分不出开/关。
//
// ============================ 怎么接 ============================
//  `Editor/RewardsScene.cs` 的聚合行（`:13725`，`Debug.Log(… 合计 …)` 之前）**同一物理行**追加：
//      B116W16_Assert.Run(); _sink.Pass += B116W16_Assert.Pass; _sink.Fail += B116W16_Assert.Fail;
//  ⚠️ 为什么挂在 Rewards 而不是 Shell/MainMenu：收件箱真正的功能断言就在这份宿主里（`:11777` 起），
//     而且 RewardsScene 的行号引用**只有 5 条、最深 `:13605`**（聚合行 `:13725` 在它之后）⇒ 同物理行追加**零漂移**。
//
// ⚠️ 本文件**不碰玩家真档**：全程两个临时目录（`%TEMP%/wf_b116w16/{inbox,save}`），
//    `finally` 里把 `OverrideDir` / `OverrideRoot` / 双写登记 / 内存态**逐样还原**并删目录。
using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B116_W16`：`UI-15` / `B-322` —— 收件箱切中央存档（写口 + 读口同一次改）。</summary>
public static class B116W16_Assert
{
    const string Prefix = "[B116W16] ";
    static readonly CheckSink _sink = new CheckSink(Prefix) { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>一份**与正常写出来那份不同**的 payload（B 段用它证明「读的是中央档」）。</summary>
    static string CentralProbePayload(long claimedAt)
    {
        return "{\"version\":1,\"states\":[{\"id\":\"local_welcome\",\"read\":true,\"claimedAt\":" + claimedAt + "}]}";
    }

    static void TryDelete(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch (Exception) { /* 临时目录删不掉不影响断言 */ }
    }

    public static void Run()
    {
        MenuCheck.Section(_sink, "W16 收件箱 → 中央存档（UI-15 / B-322 · 四通道里第一个切的）");

        string prevInboxDir = InboxData.OverrideDir;
        string prevRoot = SaveCenter.OverrideRoot;
        string prevMirror = SaveCenter.LegacyMirrorPathOf(SaveKeys.OursInbox);
        bool prevMirrorOn = SaveCenter.LegacyMirrorOn;      // ⚠️ 2026-11-04（B139_V1）：默认已改成 false（见文件头）

        var msgs = InboxData.Messages;
        int n = msgs.Count;
        var ids = new string[n];
        var reads = new bool[n];
        var claims = new long[n];
        for (int i = 0; i < n; i++) { ids[i] = msgs[i].Id; reads[i] = msgs[i].Read; claims[i] = msgs[i].ClaimedAtMs; }

        string root = Path.Combine(Path.GetTempPath(), "wf_b116w16");
        string inboxDir = Path.Combine(root, "inbox");            // 子系统自己的旧档目录
        string saveRoot = Path.Combine(root, "save");             // `SaveCenter` 的中央档根
        string inboxDir2 = Path.Combine(root, "inbox_only");      // D 段：只注入子系统门
        try
        {
            TryDelete(root);
            // ⚠️ 2026-11-04（B139_V1）：双写开关默认已是 false（用户 2026-11-03 裁定「关双写」）⇒ 本夹具要验
            //    「中央档 + 旧档同时写」，必须**显式**打开；`finally` 还原原值（⛔ 别留脏状态给同进程后续套件）。
            SaveCenter.LegacyMirrorOn = true;

            // ---------------------------------------------------------------- 前提：键表一个字没动
            C(SaveKeys.ValidateTable(), "", "W16-0 前提：`SaveKeys.ValidateTable()` 通过");
            C(SaveKeys.Count, 51, "W16-0' ★ `OursInbox` **不进 `All`** ⇒ 数据 key 总数仍是 51（进了就红，`SaveKeys.cs:244`）");
            Tr(SaveKeys.IsKnown(SaveKeys.OursInbox), "W16-0'' `_ours_Inbox` 被 `IsKnown` 的 `_ours_` 前缀那一支放行");
            C(SaveKeys.OursInbox, "_ours_Inbox", "W16-0''' key 名 = `_ours_Inbox`（原版在服务端、无 `PlayerDataKey`）");

            // ---------------------------------------------------------------- A：写口（中央 + 双写旧档）
            InboxData.OverrideDir = inboxDir;
            SaveCenter.OverrideRoot = saveRoot;
            Tr(SaveCenter.StoreOn, "W16-A 前提：`SaveCenter.StoreOn` 为真（`OverrideRoot` 已注入临时目录）");
            var welcome = InboxData.Find("local_welcome");
            Tr(welcome != null, "W16-A' 前提：出厂表里有 `local_welcome`（`InboxData.BuildSeed` 第一条）");
            if (welcome == null) return;
            InboxData.ReloadForTest();
            Tr(!InboxData.IsRead("local_welcome"), "W16-A'' 前提：出厂态 `local_welcome` 还没读过");
            Tr(!SaveCenter.Exists(SaveKeys.OursInbox), "W16-A''' 前提：写之前中央档**不存在**");

            InboxData.MarkRead(InboxData.Find("local_welcome"));
            Tr(SaveCenter.Exists(SaveKeys.OursInbox), "W16-A1 ★★ 写后**中央档有值**（`WarpforgeSave/_ours_Inbox.json`）");
            string central = SaveCenter.GetRaw(SaveKeys.OursInbox);
            Tr(central != null && central.Contains("local_welcome"), "W16-A1' ★ 中央档 payload 里真有那条消息的 id");
            Tr(central != null && central.Contains("\"read\":true"), "W16-A1'' ★ 中央档里那一格是 `read: true`");

            string legacyPath = Path.Combine(inboxDir, "inbox.json");
            Tr(File.Exists(legacyPath), "W16-A2 ★ 旧档**照旧写**（双写期：回滚旧客户端还读得到）");
            string legacyText = File.Exists(legacyPath) ? File.ReadAllText(legacyPath) : null;
            Tr(legacyText != null && legacyText.TrimStart().StartsWith("{\"version\":"),
               "W16-A2' 旧档形状没变（顶层 `version`，不是中央的 `_v` 信封）");
            Tr(legacyText != null && !legacyText.Contains("\"_v\""),
               "W16-A2'' ★ 旧档**不带 `_v`** ⇒ 旧客户端照旧解析得动");
            C(SaveCenter.DiffWithLegacy(SaveKeys.OursInbox), "",
              "W16-A3 ★★ 中央档与旧档**逐字段相等**（施工面 §3 B4 = Step 4 的验收判据）");

            // ---------------------------------------------------------------- A4：灭自证 —— 开关**关着** ⇒ 旧档不写
            // ⚠️ 2026-11-04（`B139_V1`）：`LegacyMirrorOn` 现在默认 false ⇒ 这一段必须**先显式 false** 才有意义；
            //    它同时是「上面 A2 / A3 绿是因为开关真的开着、不是因为别的东西顺手写了旧档」的反证。
            InboxData.ReloadForTest();          // 回到盘上那一份（`local_welcome` 已读、claim 0）
            welcome.ClaimedAtMs = 424242L;      // 换一个**必然不同**的值 ⇒ 下一拍落盘一定会改中央档
            welcome.Read = false;               // `MarkRead` 只在「未读」时落盘，复位一下制造一次确定会发生的写
            SaveCenter.LegacyMirrorOn = false;
            try
            {
                InboxData.MarkRead(welcome);
                string centralOff = SaveCenter.GetRaw(SaveKeys.OursInbox);
                Tr(centralOff != null && centralOff.Contains("424242"),
                   "W16-A4 前提：中央档确实是新值（`claimedAt:424242`）—— 证明这一趟真的走了写口");
                string legacyOff = File.Exists(legacyPath) ? File.ReadAllText(legacyPath) : null;
                C(legacyOff, legacyText,
                  "W16-A4' ★★ 开关**关着** ⇒ 旧档**一个字节都没动**（仍是 A2 那份）—— 熔断这条，整套就只剩「开着能写」，分不出开/关");
            }
            finally { SaveCenter.LegacyMirrorOn = true; }

            // ---------------------------------------------------------------- B：读口「中央优先」
            // 🔴 先撤双写登记 —— 否则 `SetRaw` 会把这份探针 payload **也双写进旧档**，两边就一样了、B 段失去意义。
            SaveCenter.UnregisterLegacyMirror(SaveKeys.OursInbox);
            Tr(SaveCenter.SetRaw(SaveKeys.OursInbox, CentralProbePayload(777000L)),
               "W16-B 前提：直接改写中央档（造一个与旧档**不同**的值：claim = 777000，旧档是 0）");
            InboxData.ReloadForTest();
            C(InboxData.ClaimedAt("local_welcome"), 777000L,
              "W16-B1 ★★ 读回的是**中央档**那份（旧档仍是 0）—— 读口若退回裸 `File.ReadAllText` ⇒ 读到 0 ⇒ 红");
            Tr(InboxData.IsRead("local_welcome"), "W16-B1' 中央档那份的已读位照样生效");

            // ---------------------------------------------------------------- C：灭自证 —— 只删中央档
            Tr(SaveCenter.Delete(SaveKeys.OursInbox), "W16-C 前提：`SaveCenter.Delete` 真的把中央档挪走了（改名 `.del-*`，可回滚）");
            Tr(!SaveCenter.Exists(SaveKeys.OursInbox), "W16-C' 前提：中央档现在不存在");
            InboxData.ReloadForTest();
            C(InboxData.ClaimedAt("local_welcome"), 0L,
              "W16-C1 ★★ 中央档没了 ⇒ 读到**旧档**那份（claim 回 0）—— 读口若改成「只读中央」⇒ 出厂态");
            Tr(InboxData.IsRead("local_welcome"),
               "W16-C1' ★ 旧档那份的已读位生效（`read: true` 来自 `WarpforgeInbox/inbox.json`）");

            // ---------------------------------------------------------------- D：门位回归（自检常态）
            // 只注入子系统自己的 `OverrideDir`（**不**注入 `SaveCenter.OverrideRoot`）⇒ 必须只写旧档，
            // 内容与切换前逐字节同；⛔ 这时若还去写中央，写的就是玩家真档（红线）。
            SaveCenter.OverrideRoot = null;
            InboxData.OverrideDir = inboxDir2;
            Tr(!SaveCenter.StoreOn, "W16-D 前提：不注入 `SaveCenter.OverrideRoot` ⇒ 中央档这道门关着");
            // 🔴 硬闸：Play 里 `SaveCenter.StoreOn` 恒真 ⇒ 这一段会把临时旧档写成**玩家真档**。
            //    ⛔ 不许静默跳过 —— 前提不成立就红，而且**一次写盘都不做**。
            Tr(!Application.isPlaying, "W16-D0 ★ 前提：本段只能在**非 Play**（批处理自检）下跑 —— Play 里 `StoreOn` 恒真，会把临时档写成玩家真档");
            if (!Application.isPlaying)
            {
                InboxData.ReloadForTest();
                Tr(!InboxData.IsRead("local_welcome"), "W16-D' 前提：新目录 ⇒ 出厂态");
                InboxData.MarkRead(InboxData.Find("local_welcome"));
                string legacy2 = Path.Combine(inboxDir2, "inbox.json");
                Tr(File.Exists(legacy2), "W16-D1 ★ 只注入子系统 `OverrideDir` ⇒ 旧档照样写（切换前的老行为没被误杀）");
                C(InboxData.LastStorePath, legacy2, "W16-D1' `LastStorePath` 仍指旧档路径（日志 / 自检口径没变）");
                string text2 = File.Exists(legacy2) ? File.ReadAllText(legacy2) : null;
                Tr(text2 != null && text2.Contains("\"read\":true") && !text2.Contains("\"_v\""),
                   "W16-D1'' 内容与切换前同形（`read:true`、无 `_v`）");
                InboxData.ReloadForTest();
                Tr(InboxData.IsRead("local_welcome"),
                   "W16-D2 再读回：`SaveCenter.StoreOn` 关着时只走旧档原文（⛔ 不看中央档）");
            }
        }
        finally
        {
            // ---- 逐样还原（⛔ 不给后面的断言段留脏状态）
            InboxData.OverrideDir = prevInboxDir;
            SaveCenter.OverrideRoot = prevRoot;
            SaveCenter.LegacyMirrorOn = prevMirrorOn;      // ⚠️ 2026-11-04（B139_V1）：还原双写开关（⛔ 别留给同进程后续套件）
            for (int i = 0; i < n; i++)
            {
                var m = InboxData.Find(ids[i]);
                if (m == null) continue;
                m.Read = reads[i];
                m.ClaimedAtMs = claims[i];
            }
            SaveCenter.UnregisterLegacyMirror(SaveKeys.OursInbox);
            if (prevMirror != null) SaveCenter.RegisterLegacyMirror(SaveKeys.OursInbox, prevMirror);
            TryDelete(root);
        }
    }
}
