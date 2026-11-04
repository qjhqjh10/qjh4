// B141_W15_Assert.cs — 执行代理 `B141_W15`（第一百二十八会话 · 2026-11-04）· UI-05 D6「存档 key 形状」
//
// ============================ 它盯的是什么 ============================
//  原版 `CurrentDeck` = **卡组 id 字符串**，不是「第几套」的序号；`PlayerDecks` 只有卡组表（没有 `current`）。
//    · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/PlayerDataManager.cs:11472-11473` ——
//      `GetPlayerOnlineDataString(..., PlayerDataKey.CurrentDeck)` → `currentDeckId:string`。
//    · `d:/2/tools/decomp_full/PlayerDataManager__set_currentDeckId.c:5` —— 写字段偏移 `+0x238`。
//    · `DeckInventory.cs:209` / `:216` —— `PlayerDecks` / `PlayerDecksFast` = `AzureDeckInfo[]`（无 current；两者 Concat）。
//    · `PlayerDataManager.cs:11459-11470` —— `PlayerDecks` 缺 ⇒ 读 `Decks`（`Dictionary<string,AzureDeckInfo>`）。
//
// ============================ 断言 ============================
//  A 纯形状：`SerializeStore` 无 `current` · `SerializeCurrentDeck`/`ParseCurrentDeckId` 往返（含原版裸串）
//  B 库读写：新档写 `CurrentDeck` = **id**；旧档（`current:int`）能回落；重载按 id 命中
//  C 读兜底：`PlayerDecks` 缺 ⇒ `Decks`(map) + `PlayerDecksFast`(array)；有 `PlayerDecks` 时并 Fast
//  D 结构：生产接线真的用了新口（源码判据）
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · `WriteCurrentDeckKey` 写成「下标」而不是 id ⇒ B3/B4 红（`DeckIdAt` 对不上，重载也落不回第 2 套）。
//  · `SerializeStore` 把 `current` 写回去 ⇒ A1 红。
//  · 解析器把 `PlayerDecks` 的旧 `current` 丢了 ⇒ B5 红。
//  · `MergeFallbackDecks` 删掉 ⇒ C1/C2 红（0 套）。
//  · `WriteDecksThroughHooks` 退回 `Serialize(_decks,_current)` ⇒ D1 红。
//
// ============================ 怎么接（本文件不碰宿主聚合）============================
//  `public static void Run();` —— **零前提**（临时目录 + `DeckStore.OverridePath`，`finally` 全部还原；
//  ⛔ 不碰玩家真存档、不建窗口）。
//  挂法（调度台）：同一物理行追加
//      B141_W15_Assert.Run(); pass += B141_W15_Assert.Pass; fail += B141_W15_Assert.Fail;
using System;
using System.Collections.Generic;
using System.IO;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B141_W15`：UI-05 D6「`CurrentDeck` = 卡组 id 字符串 / `PlayerDecks` 不含 current」的回归断言。</summary>
public static class B141_W15_Assert
{
    const string Tag = "B141W15";
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
        return cond;
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        string dir = Path.Combine(Application.temporaryCachePath, "B141W15_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string decksPath = Path.Combine(dir, "WarpforgeDecks.json");
        var keepHooks = KeepHooks();
        DeckStore.OverridePath = decksPath;
        try
        {
            GroupA_Shapes();
            GroupB_LibraryRoundTrip(dir, decksPath);
            GroupC_ReadFallbacks(dir, decksPath);
            GroupD_WiringStructure();
        }
        finally
        {
            RestoreHooks(keepHooks);
            DeckStore.OverridePath = null;
            try { Directory.Delete(dir, true); } catch (Exception) { }
        }
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail);
    }

    // ==================================================================
    //  A · 纯形状
    // ==================================================================
    static void GroupA_Shapes()
    {
        var decks = Fixture();
        string store = DeckStore.SerializeStore(decks);
        Ok(store != null && store.TrimStart()[0] == '{' && store.IndexOf("\"decks\":", StringComparison.Ordinal) >= 0,
           "A0 `SerializeStore` 是 JSON 对象且含 `decks`");
        Ok(store.IndexOf("\"current\"", StringComparison.Ordinal) < 0,
           "A1 ★ 新写形状**不含 `current`**（原版 `PlayerDecks` = `AzureDeckInfo[]`，`DeckInventory.cs:209`）"
         + "｜🧨 把 current 写回去 ⇒ 本红");

        string id = "deck-id-abc";
        string cur = DeckStore.SerializeCurrentDeck(id);
        Ok(cur.IndexOf("\"id\"", StringComparison.Ordinal) >= 0 && DeckStore.ParseCurrentDeckId(cur) == id,
           "A2 `CurrentDeck` 载荷往返：`{\"id\":…}` ⇒ `" + id + "`｜实得 " + DeckStore.ParseCurrentDeckId(cur));
        Ok(DeckStore.ParseCurrentDeckId("\"" + id + "\"") == id,
           "A3 ★ 原版裸 JSON 字符串（`\"…\"`）也能解析——本地只是承载不同，语义同原版");
        Ok(DeckStore.ParseCurrentDeckId(null) == null && DeckStore.ParseCurrentDeckId("  ") == null
           && DeckStore.ParseCurrentDeckId("{}") == null && DeckStore.ParseCurrentDeckId("{\"id\":\"\"}") == null,
           "A4 空 / null / 对象里没有 id ⇒ null（调用方退回旧 `current:int`；⛔ 别把整段 JSON 当 id）");
        Ok(DeckStore.ParseCurrentDeckId("{\"_v\":7,\"id\":\"" + id + "\"}") == id,
           "A4-a 带信封残留（`_v`）也能取到 `id`（`JsonUtility` 忽略未知键）");

        // 旧档（带 current）仍能读
        string legacy = "{\"version\":1,\"current\":1,\"decks\":[{\"Name\":\"A\",\"WarlordId\":\"W\",\"DefensiveId\":\"\",\"CardIds\":[]},{\"Name\":\"B\",\"WarlordId\":\"W\",\"DefensiveId\":\"\",\"CardIds\":[]}]}";
        DeckStore.LoadNote note; string detail; int readCur;
        var back = DeckStore.Parse(legacy, out note, out detail, out readCur);
        Ok(back != null && back.Count == 2 && readCur == 1,
           "A5 ★ 旧档（`current:int`）读回来仍是 `current = 1`（向后兼容，⛔ 不归 0）｜实得 " + readCur);
    }

    // ==================================================================
    //  B · 库读写：新档写 id、旧档回落
    // ==================================================================
    static void GroupB_LibraryRoundTrip(string dir, string decksPath)
    {
        var decks = Fixture();
        decks[1].DeckId = null;            // 旧档没有 id
        string err;
        Ok(DeckStore.SaveAll(decks, 1, out err), "B0（前提）旧形状卡组文件写入临时目录（`DeckStore.SaveAll`）" + (err == null ? "" : "：" + err));

        // 旧档：没有 `CurrentDeck` key ⇒ 回落到 payload 的 `current:int`
        DeckLibrary.ReadCurrentDeckText = () => null;
        var lib = DeckLibrary.Load();
        Ok(lib.Count == 2 && lib.CurrentIndex == 1,
           "B1 ★ 旧档回落：`CurrentDeck` key 没有 ⇒ 用 payload 的 `current = 1`｜实得 index " + lib.CurrentIndex);

        // 接上中央档三个老钩子 + 两个新钩子（模拟 `DeckRuntime` 的生产注册）
        string currentKeyText = null;
        string savedDecksPayload = null;
        string savedId = null;
        DeckLibrary.ReadLegacyText = p => File.Exists(p) ? File.ReadAllText(p) : null;
        DeckLibrary.SaveLegacyText = (string p, string json, out string e) =>
        {
            e = null; savedDecksPayload = json;
            try { File.WriteAllText(p, json); return true; }
            catch (Exception ex) { e = ex.Message; return false; }
        };
        DeckLibrary.CentralSaveExists = () => File.Exists(decksPath);
        DeckLibrary.ReadCurrentDeckText = () => currentKeyText;
        DeckLibrary.SaveCurrentDeckText = id => { savedId = id; currentKeyText = DeckStore.SerializeCurrentDeck(id); return true; };

        Ok(lib.Save(), "B2 中央档路落盘成功（`DeckLibrary.Save`，含 `CurrentDeck` 写入）" + (lib.LastError == null ? "" : "：" + lib.LastError));
        Ok(savedDecksPayload != null && savedDecksPayload.IndexOf("\"current\"", StringComparison.Ordinal) < 0,
           "B3 ★ 写出的 `PlayerDecks` payload **没有 `current`**（拆成独立 key）｜🧨 写回 current ⇒ 本红");
        string expectId = lib.DeckIdAt(1);
        Ok(!string.IsNullOrEmpty(expectId) && savedId == expectId,
           "B4 ★★ `CurrentDeck` 写的是**卡组 id**（`DeckIdAt(1)`），不是序号｜实得 `" + savedId + "`（期望 `" + expectId + "`）"
         + "｜🧨 写 `\"1\"` 这类序号 ⇒ 本红");
        Ok(DeckStore.ParseCurrentDeckId(currentKeyText) == expectId,
           "B5 `CurrentDeck` 载荷可反解回同一个 id");

        // 重载：payload 无 current，只能靠 `CurrentDeck`(id) 定位
        var lib2 = DeckLibrary.Load();
        Ok(lib2.Count == 2 && lib2.CurrentIndex == 1 && lib2.DeckIdAt(1) == expectId,
           "B6 ★ 重载按 **id** 命中第 2 套（payload 里已经**没有** current）｜实得 index " + lib2.CurrentIndex);

        // 再写一次：id 稳定（`EnsureCurrentDeckIdInMemory` 不会每次换新 id）
        string idAgain = null;
        DeckLibrary.SaveCurrentDeckText = id => { idAgain = id; return true; };
        lib2.Save();
        Ok(idAgain == expectId, "B7 同一套再次落盘，id **稳定**（不重发）｜实得 `" + idAgain + "`");

        // 清掉 key 后（模拟老客户端回滚）仍能靠旧 `current` 读
        currentKeyText = null;
        var lib3 = DeckLibrary.Load();
        Ok(lib3.CurrentIndex == 0,
           "B8 说明档：payload 已无 `current` ⇒ 去掉 `CurrentDeck` 会回落到 0（这正是**必须**有独立 key 的原因，⛔ 不是缺陷）｜实得 " + lib3.CurrentIndex);
    }

    // ==================================================================
    //  C · 读兜底：`Decks`(map) + `PlayerDecksFast`(array)
    // ==================================================================
    static void GroupC_ReadFallbacks(string dir, string decksPath)
    {
        string map = "{\"d1\":{\"id\":\"d1\",\"name\":\"映射甲\",\"hero\":\"UM1\",\"defenseCard\":\"DEF1\",\"cardback\":\"cb\",\"warcry\":\"wc\",\"gameMode\":0,\"cards\":{\"C1\":2,\"C2\":1}}}";
        string fast = "[{\"id\":\"f1\",\"name\":\"快甲\",\"hero\":\"DA1\",\"defenseCard\":\"DEF2\",\"cardback\":\"\",\"warcry\":null,\"gameMode\":13,\"cards\":{\"C3\":1}}]";

        // 先测「没有任何 key」的基线（灭自证：兜底钩子才是产出 2 套的唯一原因）
        DeckLibrary.ReadLegacyText = p => File.Exists(p) ? File.ReadAllText(p) : null;
        DeckLibrary.CentralSaveExists = () => false;
        DeckLibrary.ReadDecksKeyText = null;
        DeckLibrary.ReadPlayerDecksFastText = null;
        DeckLibrary.ReadCurrentDeckText = () => null;
        string parked = null;
        if (File.Exists(decksPath)) { parked = decksPath + ".parked"; File.Move(decksPath, parked); }
        try
        {
            var none = DeckLibrary.Load();
            Ok(none.Count == 0, "C0（前提）没有 `PlayerDecks` 也没有兜底 key ⇒ 0 套｜实得 " + none.Count);

            DeckLibrary.ReadDecksKeyText = () => map;
            DeckLibrary.ReadPlayerDecksFastText = () => fast;
            var lib = DeckLibrary.Load();
            Ok(lib.Count == 2,
               "C1 ★ `PlayerDecks` 缺 ⇒ `Decks`(map) + `PlayerDecksFast`(array) = 2 套（原版 `PlayerManager:11459-11470` + `DeckInventory.cs:216`）｜实得 " + lib.Count);
            var a = lib.Count > 0 ? lib.Decks[0] : null;
            var b = lib.Count > 1 ? lib.Decks[1] : null;
            Ok(a != null && a.Name == "映射甲" && a.WarlordId == "UM1" && a.DefensiveId == "DEF1"
               && a.CardIds != null && a.CardIds.Count == 3 && a.CardIds.FindAll(c => c == "C1").Count == 2,
               "C2 `Decks` 的 map 值 → `PlayerDeck` 字段逐格（name/hero/defenseCard/cards(id→张数)）");
            Ok(a != null && a.DeckId == "d1" && a.CardbackId == "cb" && a.WarcryId == "wc" && a.GameMode == 0,
               "C3 map 的**键**当 id（值里 `id` 缺省时也一样）· cardback/warcry/gameMode 都带上");
            Ok(b != null && b.Name == "快甲" && b.GameMode == 13 && b.DeckId == "f1" && b.CardIds.Count == 1,
               "C4 `PlayerDecksFast` 的数组 → `PlayerDeck`（gameMode 13 / 卡表 1 张 / id 保留）");

            // 有 `PlayerDecks` 时 Fast **也**要并进来（原版 Concat）
            if (parked != null) { File.Move(parked, decksPath); parked = null; }
            var lib2 = DeckLibrary.Load();
            Ok(lib2.Count == 3,
               "C5 ★ 有 `PlayerDecks`（2 套）时 **仍然并 `PlayerDecksFast`**（+1 = 3，原版 Concat）｜实得 " + lib2.Count);
        }
        finally
        {
            if (parked != null && File.Exists(parked)) { if (File.Exists(decksPath)) File.Delete(decksPath); File.Move(parked, decksPath); }
        }
    }

    // ==================================================================
    //  D · 生产接线（源码结构）
    // ==================================================================
    static void GroupD_WiringStructure()
    {
        string root = Application.dataPath;
        string lib = ReadSrc(root + "/RuleEngine/Data/DeckLibrary.cs");
        string store = ReadSrc(root + "/RuleEngine/Data/DeckStore.cs");
        string reader = ReadSrc(root + "/CardPresentation/Shell/LegacySaveReader.cs");
        Ok(lib.Length > 0 && store.Length > 0 && reader.Length > 0, "D0（前提）三份源码都读得到");
        if (lib.Length == 0 || store.Length == 0 || reader.Length == 0) return;

        Ok(CountOf(lib, "SerializeStore(_decks)") >= 1 && CountOf(lib, "WriteCurrentDeckKey(") >= 2,
           "D1 ★ 生产写入走新形状 + 写 `CurrentDeck`（源码：`SerializeStore(_decks)` ≥1、`WriteCurrentDeckKey(` ≥2）"
         + "｜实得 " + CountOf(lib, "SerializeStore(_decks)") + " / " + CountOf(lib, "WriteCurrentDeckKey(")
         + "｜🧨 `WriteDecksThroughHooks` 退回 `Serialize(_decks,_current)` ⇒ 本红");
        Ok(CountOf(lib, "MergeFallbackDecks(") >= 2 && CountOf(lib, "ReadDecksKeyText") >= 2 && CountOf(lib, "ReadPlayerDecksFastText") >= 2,
           "D2 ★ 读侧接了兜底（`MergeFallbackDecks` 定义+调用；`Decks`/`PlayerDecksFast` 各 ≥2 处引用）");
        Ok(CountOf(lib, "ApplyCurrentDeckKey()") >= 2,
           "D3 ★ `Load` 之后按 `CurrentDeck` 校正 `_current`（`ApplyCurrentDeckKey` 定义+调用 ≥2）");
        Ok(CountOf(reader, "ImportDecksFile(") >= 2 && CountOf(reader, "SerializeCurrentDeck") >= 1,
           "D4 ★ 旧档导入做了形状换算（`ImportDecksFile` 定义+调用；写 `CurrentDeck`）");
        Ok(CountOf(store, "public static string SerializeStore") >= 1
           && CountOf(store, "public static string SerializeCurrentDeck") >= 1
           && CountOf(store, "public static string ParseCurrentDeckId") >= 1
           && CountOf(store, "public static List<PlayerDeck> ParseAzureDeckArray") >= 1
           && CountOf(store, "public static List<PlayerDeck> ParseAzureDeckMap") >= 1,
           "D5 `DeckStore` 五个新口都在");
    }

    // ==================================================================
    //  夹具 / 工具
    // ==================================================================
    static List<PlayerDeck> Fixture()
    {
        var a = new PlayerDeck("甲", "UM1", "DEF", new List<string> { "C1", "C2" }, 0) { DeckId = "id-a" };
        var b = new PlayerDeck("乙", "DA1", "DEF", new List<string> { "C3" }, 13) { DeckId = "id-b" };
        return new List<PlayerDeck> { a, b };
    }

    struct HookKeep
    {
        public Func<string, string> Read; public DeckLibrary.DeckLegacySaver Save;
        public Func<bool> Exists; public Func<string> ReadCur; public Func<string, bool> SaveCur;
        public Func<string> ReadDecks; public Func<string> ReadFast;
    }

    static HookKeep KeepHooks()
    {
        return new HookKeep
        {
            Read = DeckLibrary.ReadLegacyText, Save = DeckLibrary.SaveLegacyText, Exists = DeckLibrary.CentralSaveExists,
            ReadCur = DeckLibrary.ReadCurrentDeckText, SaveCur = DeckLibrary.SaveCurrentDeckText,
            ReadDecks = DeckLibrary.ReadDecksKeyText, ReadFast = DeckLibrary.ReadPlayerDecksFastText,
        };
    }

    static void RestoreHooks(HookKeep k)
    {
        DeckLibrary.ReadLegacyText = k.Read; DeckLibrary.SaveLegacyText = k.Save; DeckLibrary.CentralSaveExists = k.Exists;
        DeckLibrary.ReadCurrentDeckText = k.ReadCur; DeckLibrary.SaveCurrentDeckText = k.SaveCur;
        DeckLibrary.ReadDecksKeyText = k.ReadDecks; DeckLibrary.ReadPlayerDecksFastText = k.ReadFast;
    }

    static string ReadSrc(string p) { try { return File.Exists(p) ? File.ReadAllText(p) : ""; } catch (Exception) { return ""; } }

    static int CountOf(string s, string sub)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(sub)) return 0;
        int n = 0, i = 0;
        while ((i = s.IndexOf(sub, i, StringComparison.Ordinal)) >= 0) { n++; i += sub.Length; }
        return n;
    }
}
