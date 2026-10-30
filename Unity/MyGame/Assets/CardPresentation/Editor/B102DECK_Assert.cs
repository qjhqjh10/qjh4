// B102DECK_Assert.cs — 第一百零二会话 · 执行代理 `B102_DECK`
//
// 账 = `资料/完全复刻/账/其他_基础设施.tsv:780`
//      （`DeckStore.Parse` / `DeckStore.Serialize` 的**数据半**；现记 B，
//        判 A 的最后一步 = 本文件那两条 ★ 验收。切换点已经接好了 —— 本文件⛔ 不改任何产品代码。）
//
// ============================ 它盯的是什么（判据只写在这里一次）============================
//  判据 = `资料/完全审查与完全复刻_方案.md` §七 Step 4
//         （「双写期 + 切前 dump 与切后中央档 payload 规范化后逐字段相等」）。
//  两个切换口（**实现**，本文件只调用、不修改）：
//    · 写 = `CardPresentation/Shell/SaveCenter.cs:1093`
//          `SaveThroughLegacy(string key, string legacyFullPath, string payloadJson, out string error)`
//    · 读 = `CardPresentation/Shell/SaveCenter.cs:1165`
//          `ReadThroughLegacy(string key, string legacyFullPath)`
//  生产接线 = `CardPresentation/Deck/DeckRuntime.cs:9677-9679`（key = `SaveKeys.PlayerDecks`）——
//    ⚠️ 它挂在 `RuntimeInitializeOnLoadMethod` 上 ⇒ **批处理自检不会自动接**
//    （见 `DeckRuntime` 文件尾那段注释的第 ② 条）⇒ 本断言**自己注、跑完逐字还原**。
//  子系统侧注入点 = `RuleEngine/Data/DeckLibrary.cs:618`（`ReadLegacyText`）·
//    `:623`（`SaveLegacyText`）· `:627`（`CentralSaveExists`）·
//    `:634` `ReadDecksThroughHooks` · `:644` `WriteDecksThroughHooks`。
//    三钩子 = `null` ⇒ 走 W6 之前那条**纯文件老路**（`DeckStore.LoadAll` / `DeckStore.SaveAll`）。
//
//  ★ A1 切前/切后等价 —— 同一份卡组数据：
//       「切前（旧文件路）」= 三钩子 null ⇒ `DeckLibrary.Save()` 走 `DeckStore.SaveAll`（原子写临时旧档）；
//       「切后（中央档路）」= 三钩子接 `SaveCenter` ⇒ `DeckLibrary.Save()` 走 `SaveThroughLegacy`
//                             （中央档带 `_v` 信封 + 旧档镜像不带）。
//       两边写出去的 payload **规范化后逐字段相等** —— 用 `SaveJsonDiff.Diff` 比 **JSON 结构**
//       （键序 / 空白 / `1` vs `1.0` 都不算差异；数组顺序算）。
//  ★ A2 中央档坏 ⇒ 回落旧档成功 —— 中央档放坏数据（① 空文件 ② 截断的 JSON 信封）之后：
//       `DeckLibrary.Load()` 的 `LastLoadIssue` **必须不是 `Failed`**（✅ 本轮实测是 `None` = 读到了），
//       而且**卡组条数 / 名字**与旧档逐条一致（= 真的回落了，不是「返回一个空库」）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  🧨 A1-5 / A1 / A1-a：把 `DeckLibrary.WriteDecksThroughHooks`（`:644`）改回
//        `return DeckStore.SaveAll(_decks, _current, out error);`
//        ⇒ 中央档根本不会生成 ⇒ `SaveCenter.Exists(Key)` = false、`GetRaw` = null
//        ⇒ A1-5 红、A1（`Diff(旧, null)` 报「内容是 null」）红。
//  🧨 A2-r：把 `DeckLibrary.ReadDecksThroughHooks`（`:634`）短路成 `return DeckStore.LoadAll(...)`
//        （= 不读中央档）⇒ A2-r 红（**把旧档挪走后**必须仍读得到 3 套，那只能来自中央档）。
//  🧨 A2 / A2-a：删掉 `SaveCenter.ReadThroughLegacy` 里「中央档读不出来 ⇒ 回落旧档」那一句
//        （改前形状 = 直接 `return null`）⇒ `DeckStore.Parse(null)` = `NoSaveFile` ⇒ 条数 0 ⇒ A2 / A2-a 红。
//  ⚠️ **A2-r 与 A2 必须分开**：前者盯「读钩子被用到没有」，后者盯「坏档会不会回落」。
//     只断 A2 的话，把读钩子短路掉**照样绿**（短路后直接读旧档，中央档坏不坏与它无关）。
//  ⚠️ A2-a / A2-b 是「真的回落到了旧档内容」那一半 —— 只断 `LastLoadIssue != Failed` 的话，
//     「返回空库 + 说读到了」也能蒙过去。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `CardPresentation/Editor/DeckScene.cs` 的 `Run()` 里那一串代接线之后加：
//      B102DECK_Assert.Run(); pass += B102DECK_Assert.Pass; fail += B102DECK_Assert.Fail;
//  （宿主由调度台代接线。本文件只碰**公开口** + 自建临时目录：
//    `DeckStore.OverridePath` 与 `SaveCenter.OverrideRoot` 全程指到 `%TEMP%` 下的一次性目录，
//    A4 还逐字比对「玩家的真卡组档 存在性 / 大小 / mtime 三样都没变」，`finally` 里全部还原并删目录。）
using System;
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B102_DECK`：账 `其他_基础设施:780` 的两条 ★ 验收
/// （切前/切后 payload 逐字段相等 · 中央档坏 ⇒ 回落旧档成功）。</summary>
public static class B102DECK_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B102DECK] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    /// <summary>生产口径的 key（`DeckRuntime.cs:9677-9679` 注册的就是它）。⛔ 别自创 key 名。</summary>
    const string Key = SaveKeys.PlayerDecks;

    /// <summary>夹具里几套卡组（三条断言按它断条数）。</summary>
    const int DeckCount = 3;
    /// <summary>夹具的 `current` —— ⚠️ **不能用 0**：`Parse` 会把越界的 `current` 归成 0，
    /// 用 0 就分不出「原样带回来」与「被归一了」。这里 1 在 3 套之内 ⇒ 两边都必须是 1。</summary>
    const int CurrentIndex = 1;

    /// <summary>夹具（非空卡组库；`CardbackId` / `GameMode` 都填非默认值 ⇒ 这两个字段也进「逐字段相等」）。
    /// ⚠️ 卡 id 用**现池就有的** id：`Parse` 里的 `LegacyCardIds.MigrateDeckVerified` 是「双解」闸门，
    /// 而这副夹具本身不是合法构筑 ⇒ 现代解/旧代解都不合法 ⇒ **它一个字都不会改**（实测见 A1）。</summary>
    static List<PlayerDeck> Fixture()
    {
        return new List<PlayerDeck>
        {
            new PlayerDeck("B102DECK_甲", "UM1", "", new List<string> { "UM1", "UM2" }, 0)
                { CardbackId = "Ultramarines" },
            new PlayerDeck("B102DECK_乙", "DA1", "", new List<string> { "DA1", "DA2" }, 13)
                { CardbackId = "Dark Angels" },
            new PlayerDeck("B102DECK_丙", "SW1", "", new List<string> { "SW1" }, 0),
        };
    }

    /// <summary>「切前（旧文件路）」那一份 payload —— A0 里抓到、A1 里拿来比。</summary>
    static string _payloadBefore;

    public static void Run()
    {
        MenuCheck.Section(_sink, "B102_DECK 账 `其他_基础设施:780`：★ 切前/切后等价 · ★ 中央档坏 ⇒ 回落旧档");

        string tmp = Path.Combine(Path.GetTempPath(), "wf_b102deck_" + Guid.NewGuid().ToString("N"));
        string legacyPath = Path.Combine(tmp, "WarpforgeDecks.json");
        string centralRoot = Path.Combine(tmp, "central");

        // ==== 现场快照（跑完逐项还原 —— 不让本断言污染同宿主里后面的断言）====
        string keepOverridePath = DeckStore.OverridePath;
        string keepRoot = SaveCenter.OverrideRoot;
        Func<string, string> keepRead = DeckLibrary.ReadLegacyText;
        DeckLibrary.DeckLegacySaver keepSave = DeckLibrary.SaveLegacyText;
        Func<bool> keepExists = DeckLibrary.CentralSaveExists;
        bool keepMirrorOn = SaveCenter.LegacyMirrorOn;
        string keepRegistered = SaveCenter.LegacyMirrorPathOf(Key);

        // 「玩家真档」的三样读数（纯粹算出来的路径，**不读不写**）—— A4 用它证明没被写脏。
        string realDeckPath = Path.Combine(Application.persistentDataPath, DeckStore.FileName);
        bool realExisted = File.Exists(realDeckPath);
        long realLen = realExisted ? new FileInfo(realDeckPath).Length : -1L;
        DateTime realMtime = realExisted ? File.GetLastWriteTimeUtc(realDeckPath) : DateTime.MinValue;

        try
        {
            Directory.CreateDirectory(tmp);
            Directory.CreateDirectory(centralRoot);

            // ---- 隔离（A4 的前半）：两条路都指到临时目录；三钩子先清空 = 「旧文件路」那一侧 ----
            DeckStore.OverridePath = legacyPath;
            SaveCenter.OverrideRoot = centralRoot;
            SaveCenter.LegacyMirrorOn = true;
            DeckLibrary.ReadLegacyText = null;
            DeckLibrary.SaveLegacyText = null;
            DeckLibrary.CentralSaveExists = null;

            SectionPremise(tmp, legacyPath, centralRoot);
            SectionCutBeforeAfterEquals();
            SectionCentralBrokenFallsBack();
            SectionIsolation(tmp, realDeckPath, realExisted, realLen, realMtime);
        }
        catch (Exception e)
        {
            Tr(false, "B102DECK 断言抛异常：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            // 先撤钩子（它们指向的临时目录马上要删），再还原路径与登记。
            DeckLibrary.ReadLegacyText = keepRead;
            DeckLibrary.SaveLegacyText = keepSave;
            DeckLibrary.CentralSaveExists = keepExists;
            DeckStore.OverridePath = keepOverridePath;
            SaveCenter.OverrideRoot = keepRoot;
            SaveCenter.LegacyMirrorOn = keepMirrorOn;

            SaveCenter.UnregisterLegacyMirror(Key);
            if (!string.IsNullOrEmpty(keepRegistered))
            {
                bool ok = SaveCenter.RegisterLegacyMirror(Key, keepRegistered);
                if (!ok)
                    Debug.LogWarning("[B102DECK] 收尾还原旧档登记失败：'" + Key + "' ⇒ `" + keepRegistered
                                   + "`（不影响本次结论；下一个用到它的宿主会自己重登）");
            }

            try { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
            catch { /* 临时目录删不掉不影响结论 */ }
        }
    }

    // ==========================================================================================
    //  A0 前提（**硬断言**：⛔ 不用 `if (x != null) Check(...)` 那种前提不成立就静默跳过的写法）
    // ==========================================================================================
    static void SectionPremise(string tmp, string legacyPath, string centralRoot)
    {
        MenuCheck.Section(_sink, "A0 前提（能拿到 `DeckStore.Path` · 能构造非空卡组库 · 两条路都在临时目录）");

        Tr(!string.IsNullOrEmpty(DeckStore.FileName) && DeckStore.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase),
           "A0-a `DeckStore.FileName` 是 .json 文件名（实得 `" + DeckStore.FileName + "`）");
        Tr(DeckStore.OverridePath == legacyPath && DeckStore.Path == legacyPath,
           "A0-b `DeckStore.Path` 走的正是自检注入的那条临时档（实得 `" + DeckStore.Path + "`）");
        Tr(DeckStore.Path.StartsWith(tmp, StringComparison.OrdinalIgnoreCase),
           "A0-c 旧档路径落在临时目录里（⛔ 不碰玩家真档）");
        Tr(SaveCenter.StoreOn, "A0-d `SaveCenter.StoreOn` 为真（`OverrideRoot` 指到临时目录 ⇒ 允许写盘）");
        Tr(SaveCenter.PathOf(Key).StartsWith(centralRoot, StringComparison.OrdinalIgnoreCase),
           "A0-e 中央档路径也落在临时目录（实得 `" + SaveCenter.PathOf(Key) + "`）");
        Tr(!File.Exists(legacyPath) && !File.Exists(SaveCenter.PathOf(Key)),
           "A0-f 前提：动手前**两边都没有档**（这才是「第一次跑」的起点，A1 的对比才有意义）");

        // 造夹具 —— 先按**旧文件路**写一份（此刻三钩子全是 null ⇒ `DeckStore.SaveAll`）
        string err;
        bool wrote = DeckStore.SaveAll(Fixture(), CurrentIndex, out err);
        Tr(wrote, "A0-g 夹具写进临时旧档（`DeckStore.SaveAll`）："
                + (string.IsNullOrEmpty(err) ? "成功" : "失败 —— " + err));
        Tr(File.Exists(legacyPath), "A0-h 临时旧档文件真的落盘了（`" + legacyPath + "`）");

        // 「非空卡组库」这件事由真读一遍来证（⛔ 不是直接把内存里的 List 拿来当结论）
        var lib = DeckLibrary.Load();
        C(lib.Count, DeckCount, "A0-i 从临时档读回来的**卡组套数 = 3**（能构造出一个非空卡组库）");
        C(lib.LastLoadIssue, DeckLibrary.DeckLoadIssue.None, "A0-j 读到一份合法存档 ⇒ `LastLoadIssue = None`");

        // 旧文件路再落一次盘 —— 这一份就是 **A1 的「切前」payload**
        Tr(lib.Save(), "A0-k 旧文件路落盘成功（`DeckLibrary.Save` 且三钩子 null ⇒ `DeckStore.SaveAll`）"
                     + (lib.LastError == null ? "" : "：" + lib.LastError));
        _payloadBefore = File.ReadAllText(DeckStore.Path);
        Tr(!string.IsNullOrEmpty(_payloadBefore) && _payloadBefore.TrimStart().Length > 0
           && _payloadBefore.TrimStart()[0] == '{',
           "A0-l 「切前」payload 是 **JSON 对象**（`{` 开头 —— `SaveThroughLegacy` 只收这种，否则当场拒）");
    }

    // ==========================================================================================
    //  ★ A1 —— 切前（旧文件路）vs 切后（中央档路）：规范化后逐字段相等
    // ==========================================================================================
    static void SectionCutBeforeAfterEquals()
    {
        MenuCheck.Section(_sink, "A1 ★ 切前/切后等价（`DeckStore.SaveAll` vs `SaveCenter.SaveThroughLegacy`）");

        // ② 接钩子 —— **逐字同一套**生产接线（`DeckRuntime.cs:9677-9679`），只是批处理里没人替我们接
        DeckLibrary.ReadLegacyText = path => SaveCenter.ReadThroughLegacy(Key, path);
        DeckLibrary.SaveLegacyText = (string path, string json, out string e) => SaveCenter.SaveThroughLegacy(Key, path, json, out e);
        DeckLibrary.CentralSaveExists = () => SaveCenter.StoreOn && SaveCenter.Exists(Key);
        Tr(DeckLibrary.ReadLegacyText != null && DeckLibrary.SaveLegacyText != null && DeckLibrary.CentralSaveExists != null,
           "A1-0 三个注入点都接上了（`DeckLibrary` 自己不认识 `SaveCenter` —— 那条反向依赖由 Shell 侧注进来）");

        // ③ 中央档路：读（此刻中央档还没有 ⇒ `ReadThroughLegacy` 必须回落旧档）→ 写（中央档 + 旧档双写）
        var lib2 = DeckLibrary.Load();
        C(lib2.Count, DeckCount, "A1-1 中央档路**读**：中央档还没有 ⇒ `ReadThroughLegacy` 回落旧档，仍是 3 套");
        C(lib2.LastLoadIssue, DeckLibrary.DeckLoadIssue.None, "A1-2 ……而且判成「读到了」（⛔ 不是 `NoSaveFile`）");
        Tr(lib2.Save(), "A1-3 中央档路落盘成功（`DeckLibrary.Save` → `SaveCenter.SaveThroughLegacy`）"
                      + (lib2.LastError == null ? "" : "：" + lib2.LastError));

        // ④ 结构性证据：**中央档真的生成了**（写钩子若被改回 `DeckStore.SaveAll`，这里恒 false）
        Tr(SaveCenter.Exists(Key),
           "A1-4 中央档真的生成了（`SaveCenter.Exists(\"" + Key + "\")`）"
         + "｜🧨 把 `WriteDecksThroughHooks` 改回 `DeckStore.SaveAll` ⇒ 中央档不生成 ⇒ 本行红");
        string centralPayload = SaveCenter.GetRaw(Key);
        Tr(!string.IsNullOrEmpty(centralPayload),
           "A1-5 `SaveCenter.GetRaw(\"" + Key + "\")` 取得到 payload（`_v` 信封能被剥掉 ⇒ 不是坏档）");

        // ⑤ ★ 两条等价
        string d1 = SaveJsonDiff.Diff(_payloadBefore, centralPayload);
        Tr(d1 == "",
           "A1 **★ 切前（旧文件路）与切后（中央档 payload）规范化后逐字段相等**"
         + "｜🧨 写钩子短路 / 中央档写成别的形状 ⇒ 本行红" + (d1 == "" ? "" : "\n" + Clip(d1)));

        string legacyAfter = File.ReadAllText(DeckStore.Path);
        string d2 = SaveJsonDiff.Diff(_payloadBefore, legacyAfter);
        Tr(d2 == "",
           "A1-a **★ 中央档路写出的「旧档镜像」也逐字段相等**（双写那一份不带 `_v`，旧客户端照旧读得回）"
         + (d2 == "" ? "" : "\n" + Clip(d2)));

        // ⑥ `current` 那一格单独点一次（它最容易被「归一成 0」这种静默差异吃掉）
        int cur;
        DeckStore.LoadNote note; string detail;
        var roundTrip = DeckStore.Parse(centralPayload, out note, out detail, out cur);
        C(cur, CurrentIndex, "A1-b 中央档里的 `current` 原样带回来（= " + CurrentIndex + "；⛔ 别被归一成 0）");
        C(roundTrip == null ? -1 : roundTrip.Count, DeckCount, "A1-c 中央档 payload 自己能解析回 3 套");
    }

    // ==========================================================================================
    //  ★ A2 —— 中央档坏 ⇒ 回落旧档成功（不判 `Failed`，且内容真的是旧档那几套）
    // ==========================================================================================
    static void SectionCentralBrokenFallsBack()
    {
        MenuCheck.Section(_sink, "A2 ★ 中央档坏 ⇒ `ReadThroughLegacy` 回落旧档成功（`Load` 不判 `Failed`）");

        string centralPath = SaveCenter.PathOf(Key);
        Tr(File.Exists(centralPath), "A2-0 前提：中央档在盘上（`" + centralPath + "`）");

        // ---- A2-r：**读钩子真的被用到**的结构性证据 ----
        //   把**旧档**挪走 ⇒ 仍读得出 3 套，那只可能来自中央档。
        //   🧨 `ReadDecksThroughHooks` 短路成 `DeckStore.LoadAll` ⇒ 读不到 ⇒ 0 套 ⇒ 本行红。
        string parked = Path.Combine(Path.GetDirectoryName(DeckStore.Path), "WarpforgeDecks.json.parked");
        File.Move(DeckStore.Path, parked);
        try
        {
            var libR = DeckLibrary.Load();
            C(libR.Count, DeckCount,
              "A2-r **读钩子真的被用到**：旧档挪走后仍读到 3 套（只可能来自中央档）"
            + "｜🧨 `ReadDecksThroughHooks` 短路成 `DeckStore.LoadAll` ⇒ 读不到 ⇒ 0 套 ⇒ 本行红");
        }
        finally
        {
            // 挪回来（顺序：先删掉可能被写出的同名文件，再还原）
            if (File.Exists(DeckStore.Path)) File.Delete(DeckStore.Path);
            File.Move(parked, DeckStore.Path);
        }
        Tr(File.Exists(DeckStore.Path), "A2-0b 旧档已挪回原位（后面两条要靠它做回落目标）");

        // ---- A2-1：中央档 = **空文件**（`ProbeVersion` 对空内容恒判「读不出来」）----
        File.WriteAllText(centralPath, "");
        Tr(SaveCenter.GetRaw(Key) == null,
           "A2-1 前提：空文件被 `GetRaw` 判成「读不出来」（回 null，并按 Step 3 隔离成 `.bak`）");
        var libBad1 = DeckLibrary.Load();
        C(libBad1.LastLoadIssue, DeckLibrary.DeckLoadIssue.None,
          "A2 **★ 中央档坏 ⇒ 回落旧档成功**：`LastLoadIssue = None`（读到了；⛔ 不是 `Failed`）"
        + "｜🧨 删掉 `ReadThroughLegacy` 里「坏档 ⇒ 回落旧档」那一句 ⇒ `Parse(null)` = `NoSaveFile` ⇒ 本行红");
        C(libBad1.Count, DeckCount,
          "A2-a **真的回落到了旧档内容**：条数 = 3（只断「不是 Failed」的话，「返回空库 + 说读到了」也能蒙过去）");
        C(libBad1.Decks.Count > 0 ? libBad1.Decks[0].Name : "<空库>", "B102DECK_甲",
          "A2-b ……而且就是旧档里那几套（第 0 套名字）");

        // ---- A2-2：中央档 = **截断的 JSON 信封**（另一种坏法：不是空的、但解析不了）----
        File.WriteAllText(centralPath, "{\"_v\":1,\"decks\":[{\"Name\":\"截断\"},");
        Tr(SaveCenter.GetRaw(Key) == null,
           "A2-2 前提：截断的 JSON 同样被 `GetRaw` 判成「读不出来」");
        var libBad2 = DeckLibrary.Load();
        Tr(libBad2.LastLoadIssue != DeckLibrary.DeckLoadIssue.Failed,
           "A2-3 **截断档也不判 `Failed`**（实得 " + libBad2.LastLoadIssue
         + "；✅ 判据只要求「不判失败」—— `None` 是回落成功）");
        C(libBad2.Count, DeckCount, "A2-4 ……条数仍与旧档一致（3 套）");
    }

    // ==========================================================================================
    //  A4 —— 自检不许写脏玩家真档（全程临时目录；真档 存在性/大小/mtime 三样逐字比对）
    // ==========================================================================================
    static void SectionIsolation(string tmp, string realDeckPath, bool realExisted, long realLen, DateTime realMtime)
    {
        MenuCheck.Section(_sink, "A4 自检不许写脏玩家的真档（全程临时目录 + 收尾还原）");

        Tr(DeckStore.Path.StartsWith(tmp, StringComparison.OrdinalIgnoreCase),
           "A4-a 全程 `DeckStore.Path` 都在临时目录里（`" + DeckStore.Path + "`）");
        Tr(SaveCenter.PathOf(Key).StartsWith(tmp, StringComparison.OrdinalIgnoreCase),
           "A4-b 全程中央档也都在临时目录里（`" + SaveCenter.PathOf(Key) + "`）");

        bool nowExists = File.Exists(realDeckPath);
        long nowLen = nowExists ? new FileInfo(realDeckPath).Length : -1L;
        DateTime nowMtime = nowExists ? File.GetLastWriteTimeUtc(realDeckPath) : DateTime.MinValue;
        Tr(nowExists == realExisted,
           "A4-c 玩家的真卡组档**存在性没变**（`" + realDeckPath + "`：" + realExisted + " → " + nowExists + "）");
        Tr(nowLen == realLen, "A4-d 玩家的真卡组档**大小没变**（" + realLen + " → " + nowLen + " 字节）");
        Tr(nowMtime == realMtime,
           "A4-e 玩家的真卡组档**修改时间没变**（" + realMtime.ToString("O") + " → " + nowMtime.ToString("O") + "）");
    }

    // ==========================================================================================
    //  小工具
    // ==========================================================================================
    /// <summary>差异清单截断（`SaveJsonDiff.Diff` 最多列 40 条，逐条带换行 ⇒ 原样塞进断言文案太长）。</summary>
    static string Clip(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Replace("\r", "").TrimEnd();
        return s.Length <= 600 ? s : s.Substring(0, 600) + "…（截断）";
    }
}
