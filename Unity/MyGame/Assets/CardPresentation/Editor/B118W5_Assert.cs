// B118W5_Assert.cs — 执行写手 `B118_W5`（2026-11-02）的断言宿主
//
// 本文件**只放断言**（⛔ 不改产品行为、不建场景、不进任何宿主的 `RunCore` 正文）。
//
// ============================ 主对话怎么挂（⚠️ 本批**没挂**：简报要求「可新建断言类，⛔ 不接线」） ============================
// 适合的宿主 = `Editor/ShellScene.cs`（`ShellScene.Run` 9 秒，活动/存档都在壳域）或 `Editor/MainMenuScene.cs`
// （活动钮/活动窗那几段就在那儿）。挂法照 `B93D3_Assert` 的既有惯例：
//
//     B118W5Assert.Run();
//     _sink.Pass += B118W5Assert.Pass; _sink.Fail += B118W5Assert.Fail;
//
//     ⚠️ 加进宿主 ⇒ 下游行号整体 +2；重同步点跑一次 `check_ledger_points --all-A --fix`。
//
// ============================ 覆盖 `B117-R69` 的三条 ============================
// ① **存档类型**：`LocalLiveOpsEvent.SaveType` 必须是 `PlayEventSaveGame`（原版 `IPlayEvent.SaveType` 口径；
//    改前是基类 `LiveOpsSave` ⇒ `defaultDeckId` 那一格没有地方落）。
// ② **往返保真**：`PlayEventSaveGame.defaultDeckId` 经 `LiveOpsSaveStore.Save → Load` 不丢；
//    灭自证 = **同时**证明基类那条路必丢（`LiveOpsSave` 上根本没有 `defaultDeckId` 这个字段，反射取不到）。
// ③ **接线真的在**：源码层面 `RememberEventDeck` 调了 `RememberEventDeckSave(`、`EventDeckIndex` 调了
//    `EventDeckIndexFromEventSave(`（`PlayEvent.SetDefaultDeck` 在本仓没有实例可调，见 `LiveOpsEventWindow` 类尾那段判据）。
//
// 判据出处：`d:/2/tools/decomp_full/RankedDeckSelector__ChangeDeck.c:121`（槽 8 = `SetDefaultDeck(deck, upload:false)`）
//           + `d:/2/tools/il2cpp_out/dump.cs:130038`（`IPlayEvent` `// Slot: 8`）。
// ==================================================================
using System;
using System.IO;
using System.Reflection;
using CardPresentation;
using UnityEngine;

public static class B118W5Assert
{
    const string P = "[B118W5] ";

    /// <summary>本类这一趟的通过 / 失败数（主对话想并进宿主的总数就加这两格）。</summary>
    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    static void Ok(bool cond, string what)
    {
        if (cond) { Pass++; Debug.Log(P + "✓ " + what); }
        else { Fail++; Debug.LogError(P + "✗ " + what); }
    }
    static void Eq<T>(T got, T want, string what)
    {
        bool ok = System.Collections.Generic.EqualityComparer<T>.Default.Equals(got, want);
        Ok(ok, what + "（期望 `" + want + "`，实得 `" + got + "`）");
    }

    public static void Run()
    {
        Pass = 0; Fail = 0;
        try { RunSaveTypeShape(); } catch (Exception ex) { Fail++; Debug.LogError(P + "✗ ① 段抛了异常（后面没跑）：" + ex); }
        try { RunRoundTrip(); } catch (Exception ex) { Fail++; Debug.LogError(P + "✗ ② 段抛了异常（后面没跑）：" + ex); }
        try { RunWiring(); } catch (Exception ex) { Fail++; Debug.LogError(P + "✗ ③ 段抛了异常（后面没跑）：" + ex); }
        Debug.Log(P + "合计：通过 " + Pass + " / 失败 " + Fail);
    }

    // ============================================================ ① 存档类型形状
    static void RunSaveTypeShape()
    {
        Ok(typeof(PlayEventSaveGame).IsSubclassOf(typeof(LiveOpsSave)),
           "（前提）`PlayEventSaveGame : LiveOpsSave` —— 换成子类不会破坏 `ILiveOps.GetBaseSave()` 的返回类型");

        var ev = new LocalLiveOpsEvent();
        Eq(ev.SaveType, typeof(PlayEventSaveGame),
           "① `LocalLiveOpsEvent.SaveType` = 具体子类（原版 `IPlayEvent.SaveType` 口径；`LoadFromCatalog` 的反序列化类型跟它一致）");
        Eq(ev.DataType, typeof(LiveOpsEventData), "①（对照）`DataType` 不变");

        // 灭自证：基类上必须有 / 没有那一格 —— 证明「改前按基类反序列化必丢」不是猜的
        Ok(typeof(LiveOpsSave).GetField("defaultDeckId") == null,
           "①（灭自证）`LiveOpsSave` **没有** `defaultDeckId` 字段 ⇒ 旧口径（按基类反序列化）必丢这一格");
        Ok(typeof(PlayEventSaveGame).GetField("defaultDeckId") != null,
           "①（灭自证）`PlayEventSaveGame` **有** `defaultDeckId` 字段（原版 `PlayEventSaveGame.cs:15`）");
    }

    // ============================================================ ② Save → Load 往返
    static void RunRoundTrip()
    {
        string oldRoot = SaveCenter.OverrideRoot;
        string tmp = Path.Combine(Path.GetTempPath(), "wf_b118w5_" + Guid.NewGuid().ToString("N"));
        SaveCenter.OverrideRoot = tmp;                 // 自检只碰临时档（`SaveCenter` 的落盘门要求）
        const string id = "b118w5_probe";
        try
        {
            var s = new PlayEventSaveGame { eventId = id, defaultDeckId = "deck-probe-x", matchStarted = true };
            Ok(LiveOpsSaveStore.Save(s), "（前提）`LiveOpsSaveStore.Save` 写临时档成功");

            var back = LiveOpsSaveStore.Load(id, typeof(PlayEventSaveGame)) as PlayEventSaveGame;
            Ok(back != null, "（前提）按具体子类反序列化拿到了实例");
            Eq(back == null ? null : back.defaultDeckId, "deck-probe-x",
               "② `defaultDeckId` 经 Save→Load 往返不丢（= 原版那一格，活动卡组槽的落点）");
            Ok(back != null && back.matchStarted,
               "②（同族）`matchStarted` 也读得回（子类字段整体不再被基类反序列化吃掉）");

            // 灭自证：同一条 JSON 按基类读 ⇒ 出来的对象上根本没有那个字段（旧口径）
            var asBase = LiveOpsSaveStore.Load(id, typeof(LiveOpsSave));
            Eq(asBase == null ? null : asBase.GetType(), typeof(LiveOpsSave),
               "②（灭自证）按基类读回来的运行时类型就是 `LiveOpsSave` ⇒ 拿不到 `defaultDeckId`（这就是改前的形状）");
        }
        finally
        {
            SaveCenter.OverrideRoot = oldRoot;
            try { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); } catch (Exception) { /* 临时目录清不掉不影响结论 */ }
        }
    }

    // ============================================================ ③ 接线（源码层面）
    static void RunWiring()
    {
        string path = Path.Combine(Application.dataPath, "CardPresentation/Shell/LiveOpsEventWindow.cs");
        Ok(File.Exists(path), "（前提）能读到 `Shell/LiveOpsEventWindow.cs`（判据 = 文件本身）");
        if (!File.Exists(path)) { Fail++; return; }
        string src = File.ReadAllText(path);

        Ok(src.Contains("RememberEventDeckSave(EventId, id)"),
           "③ `RememberEventDeck`（改卡组 4 条路都经过）真的调了 `RememberEventDeckSave`");
        Ok(src.Contains("EventDeckIndexFromEventSave(EventId)"),
           "③ `EventDeckIndex`（开窗取当前那一副）真的先读原版那一格");
        Ok(src.Contains("s.defaultDeckId = deckId;"),
           "③ 写的就是原版那一句（`SaveGame.defaultDeckId = deck.deckId`），⛔ 不是第三份自定存储");
        Ok(!src.Contains("我们没有活动存档）"),
           "③（铁律 5）那条**原句**（`…在活动存档里，我们没有活动存档）`）已订正 —— 别只删前半句");
        Ok(src.Contains("我们有（`LiveOpsEvent.cs:953`"),
           "③（铁律 5）订正句写清了反证：我们**有**活动存档（`LiveOpsSaveStore`，键 = `SaveKeys.LiveOps`）");

        var t = typeof(LiveOpsEventWindow);
        Ok(t.GetMethod("EventDeckSave", BindingFlags.NonPublic | BindingFlags.Static) != null,
           "③（结构）私有静态口 `EventDeckSave` 存在");
        Ok(t.GetMethod("RememberEventDeckSave", BindingFlags.NonPublic | BindingFlags.Static) != null,
           "③（结构）私有静态口 `RememberEventDeckSave` 存在");
        Ok(t.GetMethod("EventDeckIndexFromEventSave", BindingFlags.NonPublic | BindingFlags.Instance) != null,
           "③（结构）私有实例口 `EventDeckIndexFromEventSave` 存在");
    }
}
