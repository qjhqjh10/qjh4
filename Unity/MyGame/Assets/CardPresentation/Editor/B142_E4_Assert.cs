// B142_E4_Assert.cs — 执行写手 `B142-E4`（第一百二十九会话 · 2026-11-04）
//   · 账 `B141-DECK-INERTIA`：滚动惯性截止 **10 px/s → 1 px/s**（原版 UGUI `ScrollRect.cs:866`）
//   · 账 `B141-MAXCOPIES-0`：`CopyLimitExceeded` 的具名键 `MenuDeck/Error/MaxCopiesReached` 的 `{0}` 填数链
//
// ============================ 判据（逐条实读）============================
//  · 惯性截止 = 本工程 `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/ScrollRect.cs:866`
//    `if (Mathf.Abs(m_Velocity[axis]) < 1) m_Velocity[axis] = 0;`（画布 px/s）；
//    衰减率 = `m_DecelerationRate` 四台 prefab 实读 **0.135**（`DeckRuntime.cs:10518` 早已对）。
//    同一原版值另一处 = `CardPresentation/Shell/MenuScroll.cs:594`（内联字面量 `1f`）—— 那条本来就是对的。
//  · `MenuDeck/Error/MaxCopiesReached` = 官方 13 语言表（`CardPresentation/Resources/official_loc_13lang.json`）
//    EN「Deck already has {0} copies of that card」/ ZH「卡组已有{0}张该牌的副本」；
//    `{0}` = `GetMaxCopiesInDeck`（`CardDeck__CanAddCard.c:58,65-71`）⇒ 只有 `DeckEditorState.TryAdd` 那一层手里有那张卡。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 A2「`DeckRuntime` 的截止常量 == 1f」：改回 10f（或任何 >= 2f）⇒ 红。
//  🧨 A3「整条规则只有**一处常量**」：在别处再声明一份 `ScrollInertiaMinPxPerSec`（= 回到「同一规则两处两值」）⇒ 红。
//  🧨 B3 × B4：`ErrorTextKey(有上限)` 必须给具名键、`ErrorTextKey(无上限)` 必须退回 catch-all ——
//     把默认档放开（拿不到上限也走具名键）⇒ B4/B7 红；把具名键删掉 ⇒ B3 红。两边不能一起绿。
//  🧨 B7「拿不到上限时上屏 = catch-all 那一句」：把 `{0}` 直接印上屏 ⇒ 红。
//  🧨 B9（结构）「所有档的**默认**上屏键都不含 `{0}`」：任何一档漏了上限闸 ⇒ 红。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零前提**（不建场景、不读存档、不写 PlayerPrefs；A4 会读一次源文件做结构检查）。
//  挂法（调度台决定）：宿主聚合行**同一物理行**追加
//      B142_E4_Assert.Run(); _sink.Pass += B142_E4_Assert.Pass; _sink.Fail += B142_E4_Assert.Fail;
//  判绿红：`grep "^B142E4"` 的**合计行**；逐条按行首 `B142E4 ✓` / `B142E4 ✗` 数（⛔ 别 `grep -c ✗`）。

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行写手 `B142-E4` 的验收点：`B141-DECK-INERTIA`（1 px/s）+ `B141-MAXCOPIES-0`（`{0}` 填数链）。</summary>
public static class B142_E4_Assert
{
    static int _pass, _fail;
    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B142E4 ✓ " + msg); }
        else { _fail++; Debug.LogError("B142E4 ✗ " + msg); }
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        A_Inertia();
        B_MaxCopies();
        Debug.Log("B142E4 合计 " + _pass + " 过 / " + _fail + " 红");
    }

    // ============================================================
    //  A：`B141-DECK-INERTIA` —— 惯性截止 1 px/s（原版 UGUI `ScrollRect.cs:866`）
    // ============================================================
    const string InertiaConstName = "ScrollInertiaMinPxPerSec";

    /// <summary>全程序集按**字段名**找这一条常量（= 「同一规则只有一处常量」那条结构检查的载体）。
    /// 找不到 / 找到多份都返回 null 并给出命中类型名，方便一眼看出是「删了」还是「又抄了一份」。</summary>
    static FieldInfo FindInertiaConst(out string hits)
    {
        hits = "";
        FieldInfo found = null;
        int n = 0;
        Type[] types;
        try { types = typeof(DeckRuntime).Assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { types = ex.Types; }
        foreach (var t in types)
        {
            if (t == null) continue;
            var f = t.GetField(InertiaConstName,
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (f == null) continue;
            n++;
            hits += (hits.Length == 0 ? "" : " · ") + t.FullName;
            found = f;
        }
        if (n != 1) return null;
        return found;
    }

    static void A_Inertia()
    {
        string hits;
        var f = FindInertiaConst(out hits);
        Check(f != null,
            "A1 ★（前提）整程序集**恰好**一个 `" + InertiaConstName + "` 常量 —— 同一规则只有一处值"
          + "｜🧨 在别处再抄一份 ⇒ 红（命中：" + (hits.Length == 0 ? "0 处" : hits) + "）");

        float v = f == null ? float.NaN : Convert.ToSingle(f.GetValue(null));
        Check(f != null && Mathf.Approximately(v, 1f),
            "A2 ★★ 惯性截止 = **1 px/s**（原版 UGUI `ScrollRect.cs:866` `if (Mathf.Abs(m_Velocity[axis]) < 1) m_Velocity[axis] = 0;`）"
          + "｜🧨 改回 10f ⇒ 红｜实际 " + v.ToString("0.###"));

        // 衰减率在同一段里，顺带钉住（原版四台 prefab 实读 0.135）——它是**另一条**参数，不参与上面那条结构检查。
        // ⚠️ 按**宿主类型**取，不按字段名全程序集扫：`Battle/MultiCardDisplay.cs:312` 也有一份同名的（值同为 0.135，
        //    但它是战斗侧手牌区那台滚动，不是本类这一台）⇒ 全程序集扫会拿到「谁最后找到算谁的」，不稳。
        var g = typeof(DeckRuntime).GetField("ScrollDecelerationRate",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        float decel = g == null ? float.NaN : Convert.ToSingle(g.GetValue(null));
        Check(g != null && Mathf.Approximately(decel, 0.135f),
            "A3（不回归）`DeckRuntime.ScrollDecelerationRate` = **0.135**（原版 `m_DecelerationRate` 四台 prefab 实读）"
          + "｜实际 " + decel.ToString("0.####"));

        // 结构检查（源级）：原版这个值在菜单滚动那边**已经**是对的（内联字面量），本批只把 DeckRuntime 那一处对齐。
        string path = Path.Combine(Application.dataPath, "CardPresentation/Shell/MenuScroll.cs");
        string line = null;
        if (File.Exists(path))
        {
            foreach (var ln in File.ReadAllLines(path))
                if (ln.IndexOf("Mathf.Abs(_vel)", StringComparison.Ordinal) >= 0 && ln.IndexOf("<", StringComparison.Ordinal) >= 0)
                { line = ln; break; }
        }
        Check(line != null && line.IndexOf("< 1f", StringComparison.Ordinal) >= 0,
            "A4 ★（结构）`CardPresentation/Shell/MenuScroll.cs` 的同一规则也是 **< 1f**（原版同值；两处对齐才算收口）"
          + "｜🧨 那边改成别的截止 ⇒ 红｜实际 " + (line == null ? "（未找到该行 / 文件缺失：" + path + "）" : "\"" + line.Trim() + "\""));
    }

    // ============================================================
    //  B：`B141-MAXCOPIES-0` —— `MaxCopiesReached` 的 `{0}`（= 同名份数上限）
    // ============================================================
    /// <summary>B10/B11 的夹具：一张**可进卡组**的同阵营卡（`inventoryOptions: 0` / `showInCollectionOptions: 5` = 可入组，
    /// 与 `B121_WC_Assert` 的 `Hero` / `Unit` 同一口径）。</summary>
    static CardDef E4Card(string id, string type, string rarity)
    {
        return new CardDef(id, "E4 " + id, type, "", rarity, "SpaceWolves", 1, 1, 1, 0,
                           inventoryOptions: 0, showInCollectionOptions: 5);
    }

    static void B_MaxCopies()
    {
        const string Pre = DeckRules.TermPrefix;          // "MenuDeck/Error/"
        string named = Pre + "MaxCopiesReached";          // 原版键名（官方两表真有）

        Check(DeckRules.HasOfficialNamedKey(DeckError.CopyLimitExceeded),
            "B1 ★ `HasOfficialNamedKey(CopyLimitExceeded)` = 真（官方 13 语言表真有 `MenuDeck/Error/MaxCopiesReached`）"
          + "—— 这条判的是「原版表里有没有这条键」，与「拿不拿得到上限」是两件事");

        string desc = DeckRules.Describe(DeckError.CopyLimitExceeded);
        Check(desc == named,
            "B2 ★ `Describe(CopyLimitExceeded)` = `" + named + "`（原版 `CardDeck__CanAddCard.c:58,65-71` 的 `GetMaxCopiesInDeck` 支）"
          + "｜实际 " + desc);

        string withCap = DeckRules.ErrorTextKey(DeckError.CopyLimitExceeded, 2);
        Check(withCap == named,
            "B3 ★★ 拿得到上限（`copiesCap = 2`）⇒ 上屏键 = 具名键 `MenuDeck/Error/MaxCopiesReached`"
          + "｜🧨 把上限闸写死成「永远退回」⇒ 红｜实际 " + withCap);

        string noCap = DeckRules.ErrorTextKey(DeckError.CopyLimitExceeded);
        Check(noCap == Pre + "InvalidDeck" && noCap.IndexOf("{0}", StringComparison.Ordinal) < 0,
            "B4 ★★ 拿不到上限（默认 `copiesCap = -1`）⇒ 退回原版 catch-all `InvalidDeck`，⛔ 绝不把 `{0}` 印上屏"
          + "｜🧨 把默认档放开（无上限也走具名键）⇒ 红｜实际 " + noCap);

        bool hasTerm = Loc.HasAnyTerm(named);
        string tpl = hasTerm ? Loc.T(named) : "";
        Check(hasTerm && tpl.IndexOf("{0}", StringComparison.Ordinal) >= 0,
            "B5（前提）`" + named + "` 词条真取得到、且**文案自带 `{0}`**"
          + "（官方 EN「Deck already has {0} copies of that card」/ ZH「卡组已有{0}张该牌的副本」）"
          + "｜实际 \"" + tpl + "\"");

        string one = DeckRuntime.DeckErrorText(DeckError.CopyLimitExceeded, 1);
        Check(one.IndexOf("{0}", StringComparison.Ordinal) < 0 && one == tpl.Replace("{0}", "1"),
            "B6 ★★ 上屏那一句把 `{0}` 填成了上限（`DeckRuntime.DeckErrorText(e, 1)`）"
          + "｜🧨 不做 `Replace` ⇒ 屏幕上会出现 `{0}` ⇒ 红｜实际 \"" + one + "\"");

        string dflt = DeckRuntime.DeckErrorText(DeckError.CopyLimitExceeded);
        Check(dflt == Loc.T(Pre + "InvalidDeck") && dflt.IndexOf("{0}", StringComparison.Ordinal) < 0,
            "B7 ★★ 拿不到上限时上屏 = catch-all（`DeckEditorState.TryAdd` 还没传上限之前的**安全档**，与原版 `ToRawLocalizationString` 同形）"
          + "｜🧨 直接印 `{0}` ⇒ 红｜实际 \"" + dflt + "\"");

        Check(DeckRuntime.DeckErrorText(DeckError.InvalidArmy) == Loc.T(DeckRules.ErrorTextKey(DeckError.InvalidArmy)),
            "B8（回归）不走 `{0}` 的档（`InvalidArmy`）上屏与改动前**逐字节相同**（新形参默认值不许改别的档）");

        bool ok = true; string bad = "";
        foreach (DeckError e in Enum.GetValues(typeof(DeckError)))
        {
            string k = DeckRules.ErrorTextKey(e);
            if (k != null && k.IndexOf("{0}", StringComparison.Ordinal) >= 0) { ok = false; bad += e + " "; }
        }
        Check(ok,
            "B9 ★★（结构）`DeckError` **全部档的默认上屏键**都不含 `{0}` —— 只有显式传了上限的调用方才许走带占位符的键"
          + "｜越界档：" + (bad.Length == 0 ? "无" : bad));

        // ---- B10 / B11：端到端 —— `DeckEditorState.TryAdd` 真的把上限传下去了（这就是 `B141-MAXCOPIES-0` 的正身）
        var def = E4Card("E4_DEF", "defence", "Common");           // 防御卡槽那一支
        var sDef = new DeckEditorState(new List<CardDef> { def });
        string whyA, whyB;
        bool addA = sDef.TryAdd(def, out whyA);
        bool addB = sDef.TryAdd(def, out whyB);
        Check(addA && !addB,
            "B10（前提）防御卡槽：第一次进组成功、**第二次被挡**（槽里就是这一张 ⇒ `CopyLimitExceeded`）"
          + "｜第一次 why = \"" + whyA + "\"");
        Check(whyB == Loc.T(named).Replace("{0}", "1") && whyB.IndexOf("{0}", StringComparison.Ordinal) < 0,
            "B10 ★★ 防御卡槽上屏 = 「已有 **1** 张」（原版 `CardDeck__CanAddDefensive.c:99-103` 那支把 `{0}` **硬编码成 1**，"
          + "不是 `GetMaxCopiesInDeck` 的 rarity 上限）｜🧨 传 `CopyLimit(rarity,\"defence\")` ⇒ 印 2 ⇒ 红｜实际 \"" + whyB + "\"");

        var unit = E4Card("E4_U1", "unit", "Common");              // 普通卡那一支（rarity 上限 = 2）
        var sUnit = new DeckEditorState(new List<CardDef> { unit });
        string w1, w2, w3;
        bool u1 = sUnit.TryAdd(unit, out w1);
        bool u2 = sUnit.TryAdd(unit, out w2);
        bool u3 = sUnit.TryAdd(unit, out w3);
        Check(u1 && u2 && !u3,
            "B11（前提）普通卡：前两张进组、**第三张被挡**（`CopyLimit(Common)` = 2）");
        Check(w3 == Loc.T(named).Replace("{0}", "2") && w3.IndexOf("{0}", StringComparison.Ordinal) < 0,
            "B11 ★★ 普通卡上屏 = 「已有 **2** 张」（原版 `CardDeck__CanAddCard.c:58,65-70` 用 `GetMaxCopiesInDeck(rarity)`）"
          + "｜🧨 把上限写死成 1 ⇒ 红｜实际 \"" + w3 + "\"");
    }
}
