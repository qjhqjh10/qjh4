// B143_W1b_Assert.cs — 批次 **B143** · 执行写手 `W1b`（`B143-REPLAY-RANDOM-INDEX` 的**数据模型那一半**）
//
// ============================ 它盯的是什么 ============================
//   `ReplayRecord` 新增两列 `randomCursorBefore` / `randomCursorAfter`（逐动作的预烧表游标），
//   以及它们的**唯一体检口** `ReplayRecord.ValidateRandomCursors`。
//   判据 / 施工单 → `判据/B143_回放随机流契约.md` §3·1（形状）· §3·4（旧档兼容）· §3·5（序列化）· §4（验收）。
//   ⛔ 本件**不测录制端/回放端**（那五处在 `BattleDriver.cs`，不在本写手白名单）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1b/A1d（旧档两种形状）：校验层若把判形状写成 `randomCursorBefore == null`（而不是 `Length > 0`），
//     「键存在、值是 `[]`」那一档就会从 `missing` 变成 `ok` ⇒ A1d 红。
//     （⚠️ 本仓实测 `JsonUtility` 会把**缺键的集合字段填成空表** —— `RuleEngine/Data/DeckStore.cs:174-179`。）
//   · A2（往返）：`randomCursorBefore[k]` 与 `randomCursorAfter[k]` 在夹具里**刻意给不同的值**
//     （3 / 4 / 7 vs 5 / 5 / 9）⇒ 「逐元素相等」不是靠「全是 0」蒙过去的。
//   · A3/A4（校验）：把 `ValidateRandomCursors` 改成恒 `"ok"` ⇒ A3/A4 红；改成「不符也不置 null」⇒ A3b/A4b 红。
//   · A5（版本）：把 `version` 5 → 6 ⇒ 红（本半**不 bump**，见施工单 §3·5 的结论）。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行追加**（落在该行第一个 `//` 之前）：
//     `Guard("B143W1b_Assert", B143_W1b_Assert.Run); _pass += B143_W1b_Assert.Pass; _fail += B143_W1b_Assert.Fail;`
//   （接线由调度台统一做；本写手不改宿主文件。）
//
// ⚠️ 本文件**不建 GameObject、不建资产、不写盘**（`JsonUtility` 只在内存里往返）。
using System.Collections.Generic;
using CardPresentation;
using CardPresentation.Net;
using UnityEngine;

/// <summary>B143-W1b 的断言（独立类：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B143_W1b_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B143-W1b：回放随机游标两列（数据模型 + 体检）");
        GroupA_ShapeAndCompat();
        Summary();
    }

    // ==================================================================
    //  夹具
    // ==================================================================
    static ReplayRecord Rec(int actionCount, int[] before, int[] after)
    {
        var r = new ReplayRecord();
        r.actions = new List<MsgAction>();
        for (int i = 0; i < actionCount; i++) r.actions.Add(new MsgAction { seq = i, actor = 0, kind = 4 });
        r.randomCursorBefore = before;
        r.randomCursorAfter = after;
        return r;
    }

    // ==================================================================
    //  A 组
    // ==================================================================
    static void GroupA_ShapeAndCompat()
    {
        // ---- A5：版本不 bump（纯新增字段的既有纪律，同 `traceFull`）----
        Check(new ReplayRecord().version, 5,
              "A5 ★ 本半**不动** `version`（纯新增字段；施工单 §3·5：缺键 = 老行为）");

        // ---- A1：旧档（JSON 里没有这两个键）⇒ null + `missing` + 出声 ----
        const string legacy = "{\"version\":5,\"savedAt\":\"x\",\"actions\":[]}";
        var old = JsonUtility.FromJson<ReplayRecord>(legacy);
        CheckTrue(old != null, "A1a 前提：旧档 JSON 解得开");
        if (old != null)
        {
            bool emptyOrNull = (old.randomCursorBefore == null || old.randomCursorBefore.Length == 0)
                            && (old.randomCursorAfter == null || old.randomCursorAfter.Length == 0);
            CheckTrue(emptyOrNull,
                      "A1b ★ 旧档缺键 ⇒ 两列都是 **`null` 或空表**（两种形状都算「没记」；"
                    + "本仓实测 `JsonUtility` 会把**缺键的集合字段填成空表** —— `RuleEngine/Data/DeckStore.cs:174-179` 的两条运行时日志）");
            Check(old.ValidateRandomCursors("B143W1b:legacy"), "missing",
                  "A1c ★★ 旧档体检 = `missing`（⇒ 回放端**不恢复不判**，但要**出声**「本层对账没做」）");
        }

        // ---- A1d：**键存在、值是 `[]`** —— 必须与缺键同档（复核 `B143-W2` 指出的反例形状）----
        const string emptyArrays = "{\"version\":5,\"actions\":[],\"randomCursorBefore\":[],\"randomCursorAfter\":[]}";
        var ea = JsonUtility.FromJson<ReplayRecord>(emptyArrays);
        CheckTrue(ea != null, "A1d0 前提：空数组形状的 JSON 解得开");
        if (ea != null)
        {
            Check(ea.ValidateRandomCursors("B143W1b:empty-arrays"), "missing",
                  "A1d ★★ 键存在但值是 `[]` ⇒ 同样返回 `missing`（校验层用 `Length > 0` 判形状，`null` / `[]` / 缺键三形状同档）");
        }

        // ---- A2：新档往返（逐元素相等；夹具里 before/after 刻意不同）----
        var src = Rec(3, new[] { 3, 4, 7 }, new[] { 5, 5, 9 });
        string json = JsonUtility.ToJson(src);
        var back = JsonUtility.FromJson<ReplayRecord>(json);
        CheckTrue(back != null, "A2a 前提：新档往返解得开");
        if (back != null)
        {
            CheckTrue(back.randomCursorBefore != null && back.randomCursorAfter != null,
                      "A2b ★ 往返后两列都还在（⛔ 不是 `null`）");
            Check(back.ValidateRandomCursors("B143W1b:roundtrip"), "ok", "A2c ★★ 体检 = `ok`（条数 = 动作数）");
            bool eq = back.randomCursorBefore != null && back.randomCursorAfter != null
                      && back.randomCursorBefore.Length == 3 && back.randomCursorAfter.Length == 3;
            for (int i = 0; eq && i < 3; i++)
                eq = back.randomCursorBefore[i] == src.randomCursorBefore[i]
                     && back.randomCursorAfter[i] == src.randomCursorAfter[i];
            CheckTrue(eq, "A2d ★★ 逐元素相等（before {3,4,7} / after {5,5,9} —— **两列不同值**，挡「全 0 蒙混」）");
        }

        // ---- A3：条数不符 ⇒ `count-mismatch` + 两列置 null ----
        var bad = Rec(2, new[] { 1, 2, 3 }, new[] { 2, 3, 4 });        // 3 条游标 vs 2 条动作
        Check(bad.ValidateRandomCursors("B143W1b:count"), "count-mismatch",
              "A3a ★★ 游标条数 ≠ `actions.Count` ⇒ `count-mismatch`");
        CheckTrue(bad.randomCursorBefore == null && bad.randomCursorAfter == null,
                  "A3b ★★ 且**两列都被置 null**（⛔ 不许留半截 —— 回放侧会逐条拿错游标且不报错）");

        // ---- A3c：两列互不等长（同样算 count-mismatch）----
        var bad2 = Rec(3, new[] { 1, 2, 3 }, new[] { 2, 3 });
        Check(bad2.ValidateRandomCursors("B143W1b:lenmismatch"), "count-mismatch",
              "A3c ★ before / after **互不等长** ⇒ 同样 `count-mismatch`");

        // ---- A4：只有一列 ⇒ `half` + 两列置 null ----
        var half = Rec(2, new[] { 1, 2 }, null);
        Check(half.ValidateRandomCursors("B143W1b:half"), "half",
              "A4a ★★ 只记了一列（`after == null`）⇒ `half`");
        CheckTrue(half.randomCursorBefore == null,
                  "A4b ★★ 且把**留下那一列也置 null**（半列 = 恢复或断言必有一半是瞎的）");

        // ---- A6：好的那档**不会**被误判（判别式：同一夹具只改条数就换档）----
        var good = Rec(2, new[] { 1, 2 }, new[] { 3, 4 });
        Check(good.ValidateRandomCursors("B143W1b:good"), "ok",
              "A6 ★ 判别式：2 条动作 + 2 条游标 ⇒ `ok`（与 A3 只差条数 ⇒ 证明 A3 的红是真红）");
        CheckTrue(good.randomCursorBefore != null && good.randomCursorAfter != null,
                  "A6b 且 `ok` 那一档**不动**数组（⛔ 别把正常档也清掉）");
    }

    // ==================================================================
    //  口令 / 汇总
    // ==================================================================
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("[B143W1b] ✓ " + msg); }
        else { _fail++; Debug.LogError("[B143W1b] ✗ " + msg); }
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B143_W1b 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B143_W1b 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
