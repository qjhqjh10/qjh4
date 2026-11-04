// B141_W10_Assert.cs — 执行代理 `B141_W10`（第一百二十八会话 · 2026-11-04）· UI-05 D1
//
// ============================ 它盯的是什么 ============================
//  **`Game mode` 开关在开战时决定「取哪一副预组」**（原版 `DemoDeckInfoSO.GetDeck(gameMode)`）：
//    · 原版判据（`d:/2/tools/decomp_full/` 现读）：
//      - `PracticeModePopup__BattleButtonOnClick.c:122`（`:165` / `:192` 同）—— 用窗口字段 `gameMode`
//        （字节 `+0x110`，写点 `PracticeModePopup__GameModeValueChange.c:9-12`，值 0 / 13）
//        二选一 `plVar1[3]` / `plVar1[4]`（= `DemoDeckInfoSO` 的 `+0x18` / `+0x20`）。
//      - `DemoDeckInfoSO__GetDeck.c:5-8` —— `mode == 0` → `+0x18`（`classicDeck`）、否则 → `+0x20`（`skirmishDeck`）。
//      - `PracticeModePopup__BattleButtonOnClick.c:235` —— `StartMatch(…, 6, …)`：**第 2 实参恒 6**
//        ⇒ 开关**不进** `playMode`（所以本件**不**断「开关变成 playMode」——那会是错的做法）。
//
// ============================ 断言 ============================
//  A 组（纯数据 + 纯函数，零前提）：`PairOf` 反查、`ResolveForGameMode` 两档各取各的副；
//    含 **78 对全量 sweep**（期望值来自 `demo_deck_pairs.json` 那一侧，⛔ 不从被测实现反算）。
//  B 组（源码结构）：生产入口 `PickPrebuilt` **真的**调了 `ResolveForGameMode(d, SelectedGameMode)`；
//    且 `SetPendingPlayMode` **没有**被写成 `SelectedGameMode`（⛔ 那是错的修法）。
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · `ResolveForGameMode` 改成「原样返回、不看 mode」⇒ A5 / A6 / A7 / A8 全红
//    （A5/A6 是「选了另一档的副 ⇒ 换到当前档」，A8 是全量 sweep 的 0 不符）。
//  · `PairOf` 改成按名字/前缀猜 ⇒ A2 红（同一对两侧必须是同一对象）。
//  · `PickPrebuilt` 删掉那一次调用 ⇒ B1 红（源码结构）。
//  · 把开关塞进 `SetPendingPlayMode(SelectedGameMode)` ⇒ B2 红（原版那里恒 OfflinePractice(6)）。
//
// ============================ 怎么接（本文件不碰宿主聚合）============================
//  `public static void Run();` —— **零前提**（只读 `Resources` 里那两份 JSON + 读一份源码文本；
//  ⛔ 不建窗口、不写盘、不碰玩家存档）。
//  挂法（由调度台决定）：同一物理行追加
//      B141_W10_Assert.Run(); pass += B141_W10_Assert.Pass; fail += B141_W10_Assert.Fail;
//  判绿红：按行首 `B141W10 ✓` / `B141W10 ✗` 数（⛔ 别 `grep -c ✗`）。

using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B141_W10`：UI-05 D1「`Game mode` 开关决定取哪一副预组」的回归断言。</summary>
public static class B141_W10_Assert
{
    const string Tag = "B141W10";
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

    public static void Run()
    {
        _pass = 0; _fail = 0;
        GroupA_ResolveByGameMode();
        GroupB_WiringStructure();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail);
    }

    // ==================================================================
    //  A · `ResolveForGameMode` = 原版 `DemoDeckInfoSO.GetDeck(mode)`
    // ==================================================================
    static void GroupA_ResolveByGameMode()
    {
        var pairs = PrebuiltDecks.Pairs;
        Check(pairs != null && pairs.Count > 0,
              "A0（前提）读到了练习对：`demo_deck_pairs.json` 的 practice 对 > 0｜实得 "
            + (pairs == null ? "null" : pairs.Count.ToString()));
        if (pairs == null || pairs.Count == 0) return;

        // 找一对**两侧都能解析出 Deck** 的作为夹具（判据仍是数据里那一侧）
        PrebuiltDecks.Pair pair = null;
        PrebuiltDecks.Deck classic = null, skirmish = null;
        for (int i = 0; i < pairs.Count; i++)
        {
            var p = pairs[i];
            if (p == null || p.classic == null || p.skirmish == null) continue;
            var c = PrebuiltDecks.PairDeck(p, GameMode.Classic);
            var s = PrebuiltDecks.PairDeck(p, GameMode.Skirmish);
            if (c != null && s != null) { pair = p; classic = c; skirmish = s; break; }
        }
        Check(pair != null,
              "A1（前提）存在「两侧都能解析成 Deck」的练习对（夹具来自 `Pairs` + `PairDeck`，⛔ 不手造数据）");
        if (pair == null) return;

        Check(classic.deckId == pair.classic.deckId && skirmish.deckId == pair.skirmish.deckId,
              "A2 夹具自检：`PairDeck(classic)` / `PairDeck(skirmish)` 的 deckId == 对里两侧的 deckId｜实得 "
            + classic.deckId + " / " + skirmish.deckId + "（期望 " + pair.classic.deckId + " / " + pair.skirmish.deckId + "）");
        Check(classic.deckId != skirmish.deckId,
              "A3（前提）同一对的两侧 deckId **不同**（否则「换槽位」这件事无意义）｜实得 " + classic.deckId + " / " + skirmish.deckId);

        Check(ReferenceEquals(PrebuiltDecks.PairOf(classic.deckId), pair)
              && ReferenceEquals(PrebuiltDecks.PairOf(skirmish.deckId), pair),
              "A4 `PairOf` 用**两侧任意一方的 deckId** 都能反查到同一个对（同一对象）｜对 = " + pair.so
            + "｜🧨 改成按名字/前缀猜 ⇒ 本红");

        var c2c = PrebuiltDecks.ResolveForGameMode(classic, GameMode.Classic);
        var s2s = PrebuiltDecks.ResolveForGameMode(skirmish, GameMode.Skirmish);
        Check(c2c != null && c2c.deckId == pair.classic.deckId,
              "A5 `Game mode` = Classic 时，classic 副**仍是** classicDeck（原版 `GetDeck(0)` → `+0x18`）｜实得 "
            + (c2c == null ? "null" : c2c.deckId) + "（期望 " + pair.classic.deckId + "）");
        Check(s2s != null && s2s.deckId == pair.skirmish.deckId,
              "A6 `Game mode` = Skirmish 时，skirmish 副**仍是** skirmishDeck（原版 `GetDeck(13)` → `+0x20`）｜实得 "
            + (s2s == null ? "null" : s2s.deckId) + "（期望 " + pair.skirmish.deckId + "）");

        // ★ 核心：选中的是「另一档」的副 ⇒ 换成**当前档**那一副（= 原版按 gameMode 取同一 SO 的另一套槽位）
        var c2s = PrebuiltDecks.ResolveForGameMode(classic, GameMode.Skirmish);
        var s2c = PrebuiltDecks.ResolveForGameMode(skirmish, GameMode.Classic);
        Check(c2s != null && c2s.deckId == pair.skirmish.deckId,
              "A7 ★ 开关 = Skirmish 时，**选中 classic 副**也会被换成 skirmishDeck｜实得 "
            + (c2s == null ? "null" : c2s.deckId) + "（期望 " + pair.skirmish.deckId + "）｜🧨 解析器忽略 mode（原样返回）⇒ 本红");
        Check(s2c != null && s2c.deckId == pair.classic.deckId,
              "A8 ★ 开关 = Classic 时，**选中 skirmish 副**也会被换成 classicDeck｜实得 "
            + (s2c == null ? "null" : s2c.deckId) + "（期望 " + pair.classic.deckId + "）｜🧨 解析器忽略 mode ⇒ 本红");
        Check(!ReferenceEquals(c2s, classic) && !ReferenceEquals(s2c, skirmish),
              "A9 换档时**实例真的换了**（不是同一个 Deck 只改了名字）｜🧨 解析器原样返回 ⇒ 本红");

        // 全量 sweep：78 对的每一对、每一侧，按 mode 解析都必须落到该 mode 的 deckId
        int comparable = 0, wrongC = 0, wrongS = 0;
        for (int i = 0; i < pairs.Count; i++)
        {
            var p = pairs[i];
            if (p == null || p.classic == null || p.skirmish == null) continue;
            var c = PrebuiltDecks.PairDeck(p, GameMode.Classic);
            var s = PrebuiltDecks.PairDeck(p, GameMode.Skirmish);
            if (c == null || s == null) continue;
            comparable++;
            var rc = PrebuiltDecks.ResolveForGameMode(s, GameMode.Classic);
            var rs = PrebuiltDecks.ResolveForGameMode(c, GameMode.Skirmish);
            if (rc == null || rc.deckId != p.classic.deckId) wrongC++;
            if (rs == null || rs.deckId != p.skirmish.deckId) wrongS++;
        }
        Check(comparable == pairs.Count,
              "A10（前提）全量：**每一对**两侧都能解析成 Deck（`Pairs` = " + pairs.Count + " 对）｜实得可比 " + comparable
            + "｜差 = 数据缺副（`prebuilt_decks*.json`），不是本改动引起");
        Check(wrongC == 0 && wrongS == 0,
              "A11 ★ 全量 sweep：" + comparable + " 对 × 两侧，按 mode 解析的 deckId **全部等于**对里该侧的 deckId"
            + "（不符 classic " + wrongC + " / skirmish " + wrongS + "）｜🧨 解析器忽略 mode ⇒ 本红");

        Check(PrebuiltDecks.ResolveForGameMode(null, GameMode.Skirmish) == null,
              "A12 null 入参 ⇒ null（不抛、不造一副）");
    }

    // ==================================================================
    //  B · 生产接线（源码结构，⛔ 不需要窗口）
    // ==================================================================
    static void GroupB_WiringStructure()
    {
        string p = Application.dataPath + "/CardPresentation/Shell/PracticeModePopup.cs";
        string src = File.Exists(p) ? File.ReadAllText(p) : "";
        Check(src.Length > 0, "B0（前提）读得到 `PracticeModePopup.cs` 源码（" + p + "）");
        if (src.Length == 0) return;

        int calls = CountOf(src, "PrebuiltDecks.ResolveForGameMode(d, SelectedGameMode)");
        Check(calls >= 1,
              "B1 ★ `PickPrebuilt` 里**真的**按开关解析了（源码含 `PrebuiltDecks.ResolveForGameMode(d, SelectedGameMode)`）｜实得 "
            + calls + " 处｜🧨 删掉那一次调用 ⇒ 本红（D1 退回「只警告不换牌」）");

        int wrongPlay = CountOf(src, "SetPendingPlayMode(SelectedGameMode)");
        int rightPlay = CountOf(src, "SetPendingPlayMode(PlayMode)");
        Check(wrongPlay == 0 && rightPlay >= 1,
              "B2 ★ 交出去的 playMode 仍是 `PlayMode`（= OfflinePractice(6)，原版 `BattleButtonOnClick.c:235` 第 2 实参恒 6）"
            + "｜`SetPendingPlayMode(PlayMode)` = " + rightPlay + " 处、`SetPendingPlayMode(SelectedGameMode)` = " + wrongPlay
            + " 处｜🧨 把开关塞进 playMode ⇒ 本红");

        int stale = CountOf(src, "我们没有那套数据");
        Check(stale == 0,
              "B3 已删掉过期的「我们没有那套数据」（成对数据 `PrebuiltDecks.Pairs` / `PairDeck` 早已存在）｜实得 " + stale + " 处");
    }

    static int CountOf(string s, string sub)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(sub)) return 0;
        int n = 0, i = 0;
        while ((i = s.IndexOf(sub, i, System.StringComparison.Ordinal)) >= 0) { n++; i += sub.Length; }
        return n;
    }
}
