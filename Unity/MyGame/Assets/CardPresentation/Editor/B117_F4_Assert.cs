// B117_F4_Assert.cs — 执行代理 `B117_F4`（2026-11-02 · 存档线 · `UI-15` / `B-322` 剩下两个通道）
//
// ============================ 它盯的是什么 ============================
//  `UI-15` / `B-322` 四个「没走中央存档」的通道里**最后两个**：
//   ① **卡牌等级**（`Shell/CardDetailPopup.cs` 的 `CardProgress`，旧档 `WarpforgeCardLevels.json`）
//   ② **轮抽一局存档**（`Shell/DraftModeWindow.cs`，旧档 `WarpforgeDraft/run.json` → key `ActiveEvent`）
//  切之前：两处都是裸 `System.IO.File`（卡牌等级 `CardDetailPopup.cs:185-186` 读 / `:215` 写；
//          轮抽 `DraftModeWindow.cs:788-792` 读 / `:825/:830` 写删），完全没纳入中央体系。
//  切之后：读写都走 `SaveCenter.{Read,Save}ThroughLegacy`（key = `_ours_CardProgress` / `ActiveEvent`）。
//
// ============================ 判据（本代理 2026-11-02 现读）============================
//  · `Shell/SaveCenter.cs:1093` `SaveThroughLegacy`（StoreOn 真 ⇒ 中央 + 双写旧档；StoreOn 假 ⇒ **只写旧档**）
//  · `Shell/SaveCenter.cs:1165` `ReadThroughLegacy`（**先中央、再旧档**；中央缺失/坏档 ⇒ 退回旧档原文）
//  · `Shell/LegacySaveReader.cs:492`（轮抽 → `ActiveEvent`）· `:498`（卡牌等级 → `OursCardProgress`）
//  · 模板 = `Editor/B116W16_Assert.cs`（收件箱）· `Editor/B116W21_Assert.cs`（活动通行证）
//
// ⚠️ 2026-11-04 更正（`B139_V1`）：`SaveCenter.LegacyMirrorOn`（`Shell/SaveCenter.cs:915`）自 `81ea4fbb` 起是
//    `!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WF_LEGACY_MIRROR"))`（用户 2026-11-03 裁定「关双写」）
//    ⇒ **默认 false**。本夹具原来**没显式打开**、靠旧默认值 `true`，所以 A2 / A3 / C1 一族在这道默认值改掉后全倒。
//    现在 `Run()` 开头**显式 `= true`**、`finally` 还原原值（先例 = `Editor/B102DECK_Assert.cs:132`）。
//    ⛔ 别把开关默认值改回 `true`（那是上一会话的正式决定，见 `SaveCenter.cs:913-915`）。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 写口退回裸写 ⇒ 中央档不存在 ⇒ `*-A1` / `*-A3` 当场红。
//  🧨 读口退回「裸读旧档」⇒ B 段让中央档与旧档**值不同** ⇒ `*-B1` 读到旧档那个值 ⇒ 红。
//  🧨 读口改成「只读中央」⇒ C 段删掉中央档 ⇒ `*-C1` 读到出厂值 ⇒ 红。
//  🧨 轮抽删档只删旧档 ⇒ E 段 `_run == null` 之后中央档还在 ⇒ 重读**复活**上一局 ⇒ `F4-D-E2` 红。
//  🧨 门位：D 段只注入子系统自己的门（**不**注入 `SaveCenter.OverrideRoot`）⇒ 中央档必须一个字节都不动。
//  🧨 双写开关「关着」⇒ `F4-P-A4''` 断旧档**一个字节都不动** —— 没有这条，整套只剩「开着能写」，分不出开/关。
//
// ============================ 怎么接 ============================
//  `Editor/ShellScene.cs:11093` 的聚合行（`Debug.Log(… 合计 …)` 之前）**同一物理行**追加：
//      B117_F4_Assert.Run(); _sink.Pass += B117_F4_Assert.Pass; _sink.Fail += B117_F4_Assert.Fail;
//  ⚠️ ShellScene 的账落点最深 `:11092`（聚合行 `:11093` 在它之后）⇒ 同物理行追加**零漂移**。
//
// ⚠️ 本文件**不碰玩家真档**：全程临时目录（`%TEMP%/wf_b117f4/...`），`finally` 里把
//    `OverridePath` / `OverrideDir` / `OverrideRoot` / `_persist` / 内存态 / 双写登记**逐样还原**并删目录。

using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B117_F4`：`UI-15` / `B-322` —— 卡牌等级 + 轮抽切中央存档（写口 + 读口同一次改）。</summary>
public static class B117_F4_Assert
{
    const string Prefix = "[B117F4] ";
    static readonly CheckSink _sink = new CheckSink(Prefix) { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ---- CardProgress 的「重新读盘」口 ------------------------------------------------------
    // `CardProgress` 没有公开的 reload（`_loaded` 是私有静态；`ResetForTest()` 反而把它置 true）。
    // 本波不给生产代码加成员（那会把 `CardDetailPopup.cs` 下游 ~190 处账行号整体推后）⇒
    // 自检用反射置 `_loaded = false`，并**先断「字段找到了」**（改名 ⇒ 红，⛔ 不静默跳过）。
    static bool ForceCardProgressReload()
    {
        var f = typeof(CardProgress).GetField("_loaded",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (f == null) return false;
        f.SetValue(null, false);
        return true;
    }

    /// <summary>清内存 + 打开落盘 + 强制下一次访问重新读盘（= 模拟「进程刚起来」）。</summary>
    static bool CardFreshProcess()
    {
        CardProgress.ResetForTest();          // 清三张表 + `_persist=false` + `_loaded=true`
        CardProgress.SetPersistForTest(true);
        return ForceCardProgressReload();     // `_loaded=false` ⇒ 下一次 `Level()` 真的读盘
    }

    static string CardProbePayload(int v)
    {
        return "{\"version\":1,\"levels\":[{\"id\":\"UM82\",\"v\":" + v + "}],\"owned\":[],\"wildcards\":[]}";
    }

    static string DraftProbePayload(int won)
    {
        return "{\"version\":1,\"run\":{\"joinState\":0,\"payState\":0,\"matchesWon\":" + won
             + ",\"matchesLost\":1,\"rewardsCollected\":false,\"seed\":4242,\"warlordId\":\"UM1\","
             + "\"pickedKeys\":[],\"selectedCards\":[],\"cardbackId\":\"\",\"reRolls\":0,\"consumedDraftMatches\":0}}";
    }

    static DraftRunSave MakeRun(int seed, int won)
    {
        return new DraftRunSave
        {
            joinState = 0, payState = 0,
            matchesWon = won, matchesLost = 0, rewardsCollected = false,
            seed = seed, warlordId = "UM1",
            pickedKeys = new string[0], selectedCards = new string[0],
            cardbackId = "", reRolls = 0, consumedDraftMatches = 0,
        };
    }

    /// <summary>`LegacySaveReader.Entries` 里有没有这条 key（= 导入 / 双写登记表覆盖到它）。</summary>
    static bool HasLegacyEntry(string key)
    {
        var es = LegacySaveReader.Entries;
        for (int i = 0; i < es.Length; i++) if (es[i].Key == key) return true;
        return false;
    }

    static void TryDelete(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch (Exception) { /* 临时目录删不掉不影响断言 */ }
    }

    static string TextOf(string path) { try { return File.Exists(path) ? File.ReadAllText(path) : null; } catch (Exception) { return null; } }

    /// <summary>`"key": N` 在不在文本里（⛔ 不假设 `JsonUtility` 的空白格式：compact / pretty 两种都认）。</summary>
    static bool HasNum(string text, string key, int v)
    {
        if (string.IsNullOrEmpty(text)) return false;
        return text.Contains("\"" + key + "\": " + v) || text.Contains("\"" + key + "\":" + v);
    }

    /// <summary>从一份 `CardProgressSave` JSON 里取某张卡的等级（取不到 ⇒ -1）。</summary>
    static int LevelIn(string payload, string id)
    {
        if (string.IsNullOrEmpty(payload)) return -1;
        var d = JsonUtility.FromJson<CardProgressSave>(payload);
        if (d == null || d.levels == null) return -1;
        for (int i = 0; i < d.levels.Count; i++)
            if (d.levels[i] != null && d.levels[i].id == id) return d.levels[i].v;
        return -1;
    }

    public static void Run()
    {
        string prevCardPath = CardProgress.OverridePath;
        bool prevPersist = CardProgress.PersistOn;
        string prevRoot = SaveCenter.OverrideRoot;
        string prevCardMirror = SaveCenter.LegacyMirrorPathOf(SaveKeys.OursCardProgress);
        string prevDraftMirror = SaveCenter.LegacyMirrorPathOf(SaveKeys.ActiveEvent);
        string prevDraftDir = DraftModeWindow.OverrideDir;
        DraftRunSave prevRun = DraftModeWindow.RunForTest;
        bool prevMirrorOn = SaveCenter.LegacyMirrorOn;      // ⚠️ 2026-11-04（B139_V1）：默认已改成 false（见文件头）

        string root = Path.Combine(Path.GetTempPath(), "wf_b117f4");
        string cardLegacy = Path.Combine(root, "card", "WarpforgeCardLevels.json");   // 卡牌等级旧档位
        string cardLegacy2 = Path.Combine(root, "card_only", "WarpforgeCardLevels.json"); // D 段：只注入子系统门
        string draftDir = Path.Combine(root, "draft");                               // 轮抽旧档目录
        string draftDir2 = Path.Combine(root, "draft_only");
        string saveRoot = Path.Combine(root, "save");                                // `SaveCenter` 中央档根
        try
        {
            TryDelete(root);
            // ⚠️ 2026-11-04（B139_V1）：双写开关默认已是 false（用户 2026-11-03 裁定「关双写」）⇒ 本夹具要验
            //    「中央档 + 旧档同时写」，必须**显式**打开；`finally` 还原原值（⛔ 别留脏状态给同进程后续套件）。
            SaveCenter.LegacyMirrorOn = true;

            // ================================================================ 前提：键表一个字没动
            MenuCheck.Section(_sink, "F4-0 键表 / 旧档映射表（两个通道都不许进 `All`）");
            C(SaveKeys.ValidateTable(), "", "F4-0 前提：`SaveKeys.ValidateTable()` 通过");
            C(SaveKeys.Count, 51, "F4-0' ★ 两个新 key 都**不进 `All`** ⇒ 数据 key 总数仍是 51（进了就红）");
            Tr(SaveKeys.IsKnown(SaveKeys.OursCardProgress), "F4-0'' `_ours_CardProgress` 被 `IsKnown` 的 `_ours_` 前缀那一支放行");
            C(SaveKeys.OursCardProgress, "_ours_CardProgress", "F4-0''' key 名 = `_ours_CardProgress`（原版在服务端 ⇒ 铁律 18·0 允许的自设计门类）");
            Tr(HasLegacyEntry(SaveKeys.OursCardProgress), "F4-0'''' ★ `LegacySaveReader.Entries` 里有「卡牌等级 → `_ours_CardProgress`」（导入 / 双写登记表覆盖到它）");
            Tr(HasLegacyEntry(SaveKeys.ActiveEvent), "F4-0''''' ★ `LegacySaveReader.Entries` 里有「轮抽 → `ActiveEvent`」");

            // ================================================================ ① 卡牌等级
            MenuCheck.Section(_sink, "F4-P 卡牌等级 → 中央存档（`WarpforgeCardLevels.json` / `_ours_CardProgress`）");

            // ---- P-A：写口（中央 + 双写旧档）
            CardProgress.OverridePath = cardLegacy;
            SaveCenter.OverrideRoot = saveRoot;
            Tr(CardFreshProcess(), "F4-P 前提：能强制 `CardProgress` 重读盘（反射置 `_loaded=false`；字段改名 ⇒ 红，⛔ 不静默跳过）");
            Tr(SaveCenter.StoreOn, "F4-P-A 前提：`SaveCenter.StoreOn` 为真（`OverrideRoot` 已注入临时目录）");
            Tr(!SaveCenter.Exists(SaveKeys.OursCardProgress), "F4-P-A' 前提：写之前中央档**不存在**");
            Tr(CardProgress.SetLevel("UM82", 4), "F4-P-A'' 前提：`SetLevel` 返回 true（落盘门开着、目录可写）");

            Tr(SaveCenter.Exists(SaveKeys.OursCardProgress), "F4-P-A1 ★★ 写后**中央档有值**（`WarpforgeSave/_ours_CardProgress.json`）—— 写口若退回裸 `File.WriteAllText` ⇒ 红");
            C(LevelIn(SaveCenter.GetRaw(SaveKeys.OursCardProgress), "UM82"), 4,
              "F4-P-A1' ★ 中央档 payload 里 `UM82` 的等级真是 4");

            string cardLegacyText = TextOf(cardLegacy);
            Tr(cardLegacyText != null, "F4-P-A2 ★ 旧档**照旧写**（双写期：回滚旧客户端还读得到）");
            Tr(cardLegacyText != null && !cardLegacyText.Contains("\"_v\""), "F4-P-A2' ★ 旧档**不带 `_v`** ⇒ 旧客户端照旧解析得动");
            // ⚠️ 2026-11-04（`B139_V1` 补齐）：P 段原来只有 `A2` / `A2'`，比 D 段与 W21 / W16 两族少一条牙口。
            //    这里**不照抄** W21 / W16 的 `TrimStart().StartsWith("{\"version\"")` —— 卡牌等级走的是
            //    `JsonUtility.ToJson(d, true)`（**pretty**）⇒ 文本是 `{` 换行 `"version": 1` 开头，字面比对必然假红。
            //    改成「能被**切换前那份 DTO** 原样解析回来」：`_v` 信封解出来会是默认 `version=1` + `levels` 为空 ⇒ 抓得住。
            var legacyDto = string.IsNullOrEmpty(cardLegacyText) ? null : JsonUtility.FromJson<CardProgressSave>(cardLegacyText);
            Tr(legacyDto != null && legacyDto.version == 1 && legacyDto.levels != null && legacyDto.levels.Count == 1,
               "F4-P-A2'' ★ 旧档形状没变：能被**切换前那份 DTO** 原样解析（顶层 `version` + `levels` 一条），不是中央的 `_v` 信封");
            C(SaveCenter.DiffWithLegacy(SaveKeys.OursCardProgress), "",
              "F4-P-A3 ★★ 中央档与旧档**逐字段相等**（Step 4 的验收判据）");

            // ---- P-A4：灭自证 —— 开关**关着** ⇒ 旧档不写（🧨 熔断这条，整套就只剩「开着能写」，分不出开/关）
            //  ⚠️ 2026-11-04（`B139_V1`）：`LegacyMirrorOn` 现在默认 false ⇒ 这一段必须**先显式 false** 才有意义；
            //    它同时是「上面 A2 / A3 绿是因为开关真的开着、不是因为别的东西顺手写了旧档」的反证。
            SaveCenter.LegacyMirrorOn = false;
            try
            {
                Tr(CardProgress.SetLevel("UM82", 3), "F4-P-A4 前提：开关**关着**时 `SetLevel` 仍返回 true（中央档照写，只挡双写）");
                Tr(HasNum(SaveCenter.GetRaw(SaveKeys.OursCardProgress), "v", 3),
                   "F4-P-A4' 前提：中央档确实是新值（`UM82` = 3）—— 证明这一趟真的走了写口");
                C(TextOf(cardLegacy), cardLegacyText,
                  "F4-P-A4'' ★★ 开关**关着** ⇒ 旧档**一个字节都没动**（仍是 A2 那份）—— 熔断这条，整套就只剩「开着能写」，分不出开/关");
            }
            finally { SaveCenter.LegacyMirrorOn = true; }

            // ---- P-B：读口「中央优先」（中央 = 2，旧档 = 4，值故意不同）
            SaveCenter.UnregisterLegacyMirror(SaveKeys.OursCardProgress);   // 别让 `SetRaw` 把探针双写进旧档
            Tr(SaveCenter.SetRaw(SaveKeys.OursCardProgress, CardProbePayload(2)),
               "F4-P-B 前提：直接改写中央档（`UM82` = 2；旧档那边是 4）");
            Tr(CardFreshProcess(), "F4-P-B' 前提：重新模拟「进程刚起来」");
            C(CardProgress.Level("UM82"), 2,
              "F4-P-B1 ★★ 读回的是**中央档**那份（旧档仍是 4）—— 读口若退回裸 `File.ReadAllText` ⇒ 读到 4 ⇒ 红");
            C(LevelIn(TextOf(cardLegacy), "UM82"), 4,
              "F4-P-B1' ★ 旧档那一侧**确实还是 4**（证明上面读到的 2 只可能来自中央档）");

            // ---- P-C：灭自证 —— 只删中央档 ⇒ 必须退回读旧档
            Tr(SaveCenter.Delete(SaveKeys.OursCardProgress), "F4-P-C 前提：`SaveCenter.Delete` 真的把中央档挪走了（改名 `.del-*`，可回滚）");
            Tr(!SaveCenter.Exists(SaveKeys.OursCardProgress), "F4-P-C' 前提：中央档现在不存在");
            Tr(CardFreshProcess(), "F4-P-C'' 前提：重新模拟「进程刚起来」");
            C(CardProgress.Level("UM82"), 4,
              "F4-P-C1 ★★ 中央档没了 ⇒ 读到**旧档**那份（`UM82` = 4）—— 读口若改成「只读中央」⇒ 出厂态 1");

            // ---- P-D：门位回归（只注入子系统自己的门）
            Tr(SaveCenter.SetRaw(SaveKeys.OursCardProgress, CardProbePayload(3)),
               "F4-P-D 前提：中央档放一份哨兵值（`UM82` = 3）");
            SaveCenter.OverrideRoot = null;
            CardProgress.OverridePath = cardLegacy2;
            Tr(!SaveCenter.StoreOn, "F4-P-D0 前提：不注入 `SaveCenter.OverrideRoot` ⇒ 中央档这道门关着");
            Tr(!Application.isPlaying, "F4-P-D0' ★ 前提：本段只能在**非 Play**（批处理自检）下跑 —— Play 里 `StoreOn` 恒真，会把临时档写成玩家真档");
            if (!Application.isPlaying)
            {
                Tr(CardFreshProcess(), "F4-P-D1 前提：重新模拟「进程刚起来」");
                Tr(CardProgress.SetLevel("UM82", 1), "F4-P-D1' ★ 只注入子系统路径 ⇒ 旧档照样写（切换前的老行为没被误杀）");
                string cardLegacy2Text = TextOf(cardLegacy2);
                C(LevelIn(cardLegacy2Text, "UM82"), 1, "F4-P-D1'' 旧档内容与切换前同形（`UM82` = 1）");
                Tr(cardLegacy2Text != null && !cardLegacy2Text.Contains("\"_v\""), "F4-P-D1''' 旧档**不带 `_v`**（旧客户端照旧解析得动）");
                // ⚠️ 直接读**临时根上那个文件**：此刻 `OverrideRoot` 已清空，`SaveCenter.GetRaw` 会指回玩家真档目录。
                string cardCentralPath = Path.Combine(saveRoot, SaveKeys.OursCardProgress + ".json");
                string sentinelCard = TextOf(cardCentralPath);
                Tr(HasNum(sentinelCard, "v", 3),
                   "F4-P-D2 ★★ 中央档**一个字节都没动**（仍是哨兵 3）—— 门关着 ⇒ 绝不写玩家真档");
            }
            SaveCenter.OverrideRoot = saveRoot;

            // ================================================================ ② 轮抽一局存档
            MenuCheck.Section(_sink, "F4-D 轮抽一局存档 → 中央存档（`WarpforgeDraft/run.json` / `ActiveEvent`）");

            // ---- D-A：写口（中央 + 双写旧档）
            DraftModeWindow.OverrideDir = draftDir;
            DraftModeWindow.SetRunForTest(MakeRun(1234, 0));
            Tr(SaveCenter.StoreOn, "F4-D-A 前提：`SaveCenter.StoreOn` 为真（`OverrideRoot` 已注入临时目录）");
            Tr(!SaveCenter.Exists(SaveKeys.ActiveEvent), "F4-D-A' 前提：写之前中央档**不存在**");
            DraftModeWindow.SaveRunNowForTest();
            Tr(SaveCenter.Exists(SaveKeys.ActiveEvent), "F4-D-A1 ★★ 写后**中央档有值**（`WarpforgeSave/ActiveEvent.json`）—— 写口若退回裸 `File.WriteAllText` ⇒ 红");
            string centralDraftRaw = SaveCenter.GetRaw(SaveKeys.ActiveEvent);
            Tr(HasNum(centralDraftRaw, "seed", 1234), "F4-D-A1' ★ 中央档 payload 里真是 `seed: 1234`");

            string draftLegacyPath = Path.Combine(draftDir, "run.json");
            string draftLegacyText = TextOf(draftLegacyPath);
            Tr(draftLegacyText != null, "F4-D-A2 ★ 旧档**照旧写**（双写期：回滚旧客户端还读得到）");
            Tr(draftLegacyText != null && !draftLegacyText.Contains("\"_v\""), "F4-D-A2' ★ 旧档**不带 `_v`** ⇒ 旧客户端照旧解析得动");
            C(SaveCenter.DiffWithLegacy(SaveKeys.ActiveEvent), "", "F4-D-A3 ★★ 中央档与旧档**逐字段相等**");

            // ---- D-B：读口「中央优先」（中央 matchesWon = 7，旧档 = 0）
            SaveCenter.UnregisterLegacyMirror(SaveKeys.ActiveEvent);
            Tr(SaveCenter.SetRaw(SaveKeys.ActiveEvent, DraftProbePayload(7)),
               "F4-D-B 前提：直接改写中央档（`matchesWon` = 7；旧档那边是 0）");
            DraftModeWindow.ReloadRunForTest();
            var runCentral = DraftModeWindow.RunForTest;
            C(runCentral == null ? -1 : runCentral.matchesWon, 7,
              "F4-D-B1 ★★ 读回的是**中央档**那份（旧档仍是 0）—— 读口若退回裸 `File.ReadAllText` ⇒ 读到 0 ⇒ 红");
            Tr(HasNum(TextOf(draftLegacyPath), "matchesWon", 0),
               "F4-D-B1' ★ 旧档那一侧**确实还是 0**（证明上面读到的 7 只可能来自中央档）");

            // ---- D-C：灭自证 —— 只删中央档 ⇒ 必须退回读旧档
            Tr(SaveCenter.Delete(SaveKeys.ActiveEvent), "F4-D-C 前提：`SaveCenter.Delete` 真的把中央档挪走了");
            Tr(!SaveCenter.Exists(SaveKeys.ActiveEvent), "F4-D-C' 前提：中央档现在不存在");
            DraftModeWindow.ReloadRunForTest();
            var runLegacy = DraftModeWindow.RunForTest;
            C(runLegacy == null ? -1 : runLegacy.matchesWon, 0,
              "F4-D-C1 ★★ 中央档没了 ⇒ 读到**旧档**那份（`matchesWon` = 0）—— 读口若改成「只读中央」⇒ 没有一局");

            // ---- D-E：★ 删档必须【两边一起删】—— 否则「已结束的一局」会被中央档复活
            DraftModeWindow.SetRunForTest(MakeRun(5555, 3));
            DraftModeWindow.SaveRunNowForTest();
            Tr(SaveCenter.Exists(SaveKeys.ActiveEvent), "F4-D-E1 前提：先写一局进去（中央档有值）");
            DraftModeWindow.SetRunForTest(null);          // = 放弃 / 收尾（`Resign` / `CollectReward` 两条生产路径）
            DraftModeWindow.SaveRunNowForTest();          // `_run == null` ⇒ 删档分支
            Tr(!SaveCenter.Exists(SaveKeys.ActiveEvent),
               "F4-D-E1' ★★ **删档时中央档也删了**（只删旧档 ⇒ 中央档把已结束的一局复活 ⇒ 红）");
            Tr(!File.Exists(draftLegacyPath), "F4-D-E1'' 旧档那份也删了（切换前的老行为没被误杀）");
            DraftModeWindow.ReloadRunForTest();
            Tr(DraftModeWindow.RunForTest == null,
               "F4-D-E2 ★★ 删档后重读 ⇒ **没有进行中的一局**（灭自证：中央档不删 ⇒ 这里会复活刚才那局）");

            // ---- D-F：自检删档口（`DeleteRunForTest`）同样两边一起删
            DraftModeWindow.SetRunForTest(MakeRun(6666, 1));
            DraftModeWindow.SaveRunNowForTest();
            Tr(SaveCenter.Exists(SaveKeys.ActiveEvent), "F4-D-F1 前提：再写一局进去");
            DraftModeWindow.DeleteRunForTest();
            Tr(!SaveCenter.Exists(SaveKeys.ActiveEvent),
               "F4-D-F1' ★★ `DeleteRunForTest` 也把中央档删了（⛔ 只删旧档 ⇒ 下一段自检会读到上一段留下的那一局）");

            // ---- D-G：门位回归（只注入子系统自己的门）
            Tr(SaveCenter.SetRaw(SaveKeys.ActiveEvent, DraftProbePayload(999)),
               "F4-D-G 前提：中央档放一份哨兵值（`matchesWon` = 999）");
            SaveCenter.OverrideRoot = null;
            DraftModeWindow.OverrideDir = draftDir2;
            Tr(!SaveCenter.StoreOn, "F4-D-G0 前提：不注入 `SaveCenter.OverrideRoot` ⇒ 中央档这道门关着");
            Tr(!Application.isPlaying, "F4-D-G0' ★ 前提：本段只能在**非 Play**（批处理自检）下跑");
            if (!Application.isPlaying)
            {
                DraftModeWindow.SetRunForTest(MakeRun(7777, 2));
                DraftModeWindow.SaveRunNowForTest();
                string draftLegacy2 = TextOf(Path.Combine(draftDir2, "run.json"));
                Tr(HasNum(draftLegacy2, "seed", 7777) && draftLegacy2 != null && !draftLegacy2.Contains("\"_v\""),
                   "F4-D-G1 ★ 只注入子系统 `OverrideDir` ⇒ 旧档照样写，且与切换前同形（无 `_v`）");
                string draftCentralPath = Path.Combine(saveRoot, SaveKeys.ActiveEvent + ".json");
                string sentinelDraft = TextOf(draftCentralPath);
                Tr(HasNum(sentinelDraft, "matchesWon", 999),
                   "F4-D-G2 ★★ 中央档**一个字节都没动**（仍是哨兵 999）—— 门关着 ⇒ 绝不写玩家真档");
                DraftModeWindow.ReloadRunForTest();
                var runGate = DraftModeWindow.RunForTest;
                C(runGate == null ? -1 : runGate.seed, 7777,
                  "F4-D-G3 再读回：`SaveCenter.StoreOn` 关着时只走旧档原文（⛔ 不看中央档）");
            }
        }
        finally
        {
            // ---- 逐样还原（⛔ 不给后面的断言段留脏状态）
            CardProgress.ResetForTest();
            CardProgress.SetPersistForTest(prevPersist);
            CardProgress.OverridePath = prevCardPath;
            DraftModeWindow.SetRunForTest(prevRun);
            DraftModeWindow.OverrideDir = prevDraftDir;
            SaveCenter.OverrideRoot = prevRoot;
            SaveCenter.LegacyMirrorOn = prevMirrorOn;      // ⚠️ 2026-11-04（B139_V1）：还原双写开关（⛔ 别留给同进程后续套件）
            SaveCenter.UnregisterLegacyMirror(SaveKeys.OursCardProgress);
            if (prevCardMirror != null) SaveCenter.RegisterLegacyMirror(SaveKeys.OursCardProgress, prevCardMirror);
            SaveCenter.UnregisterLegacyMirror(SaveKeys.ActiveEvent);
            if (prevDraftMirror != null) SaveCenter.RegisterLegacyMirror(SaveKeys.ActiveEvent, prevDraftMirror);
            TryDelete(root);
        }
    }
}
