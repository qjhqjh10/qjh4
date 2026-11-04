// B141_W31_Assert.cs — 执行写手 `B141-W31`（第一百二十八会话 · 2026-11-04 · 批 `B141` / 波次 `W129`）
//
// ============================ 它盯的是什么 ============================
//  判据 → `资料/完全复刻/判据/B141_W129_对账_卡组色彩.md` **P3 包** 4 条（`B138_审计汇总_问题清单.md` §3 的 3-18/3-20/3-22/3-24）：
//    · **3-18**（`Describe` 键半）：`RuleEngine/Core/DeckRules.cs` 的 `Describe` 三条键名映射
//         `CopyLimitExceeded ⇒ MenuDeck/Error/MaxCopiesReached` · `TooManyCards ⇒ MenuDeck/Tip/DeckFull` ·
//         `EffectOnlyCard ⇒ MenuDeck/Error/InvalidCard`（原版 `CardDeck__CanAddCard.c:40-47` 那条准入闸给的就是 `InvalidCard`）。
//    · **3-20**：`Validate` 的**④ 张数闸**从「⑤ 阵营之前」挪到「⑧ 之后」—— 原版 `DeckUtility__ValidateDeck.c:50`
//         先跑 `ValidateDeckCards`（逐张闸：阵营 / 卡型 / 准入 / 同名上限），**再**在 `.c:69` 比张数
//         ⇒「牌少 + 混阵营」必须报**阵营错**。
//    · **3-22**：`Validate` 的 `foreach (deck.CardIds)` 补 null 守卫（`RemoveInvalidCards` 早就有同一条）——
//         反序列化给 null 时不许 NRE，按空表校验并出声。
//    · **3-24**：`DeckLibrary.ImportString` / `SplitByType` 两处按原版
//         `CardDeck__DeserializeDeckString.c` 改：① **缺 `;` 照收**（`.c:54-59` 只在有尾巴时才 `Int32.Parse`，
//         否则 `gameMode` 保持 0 = Classic，继续解析）② **多余的督军/防御卡丢弃**（`.c:141-159` 从尾到头扫，
//         每遇到一张就 `RemoveAt`，只把串里**第一张**交给 `DeckBasicSetup`）。
//
// ============================ 灭自证（🧨 每条怎么才会红）============================
//   · A 组：把 `Describe` 三条映射摘掉（退回 `TermPrefix + e`）⇒ A2/A3/A4/a5/a6 红；
//           把 `EffectOnlyCard` 改回 `InvalidDeck` ⇒ A2 红；把 `CopyLimitExceeded` 塞进 `HasOfficialNamedKey`
//            ⇒ A9 红（`ErrorTextKey` 会带 `{0}` 上屏）。**a5/a6 直接钉「不许拼枚举名」这个老写法**。
//   · B 组：把张数闸搬回 ⑤ 之前 ⇒ B1 红（回 `TooFewCards`）；B2 是对照（同一副去掉异阵营卡 ⇒ 仍 `TooFewCards`），
//           证明 B1 的 `WrongFaction` 是**闸序**给的、不是别的原因；B3 证明张数闸**本身还生效**。
//   · C 组：把 null 守卫摘掉 ⇒ C1 抛 `NullReferenceException`（C1 用 `try/catch` 判「有没有抛」，不是「返回值碰巧对」）。
//   · D 组：D1 把 `else return null;` 加回去 ⇒ 红；D3/D4 把 `rest.Add(id)` 加回去 ⇒ 红（多余督军/防御卡会出现在 `CardIds` 里）。
//
// ============================ 覆盖边界（如实记）============================
//   · 只验**引擎层**（`RuleEngine/Core/DeckRules.cs` + `RuleEngine/Data/DeckLibrary.cs`）。
//     ⛔ 不验上屏文案（`Loc.T` 在 `CardPresentation` 侧，本程序集引用不到）——
//     四条键在官方 13 语言表里的存在性已由写手现读核过（`Resources/official_loc_13lang.json` 现读 10,862 键，四条都在），
//     但「取到的字」这条**没有**在这里断言。
//   · **`MaxCopiesReached` 的 `{0}` 尚未接线**（要给 `string.Format` 传同名份数上限，本层拿不到那张卡；
//     接线点 = `CardPresentation/Deck/DeckRuntime.DeckErrorText`，在 `B141-W31` 白名单外）⇒
//     `ErrorTextKey(CopyLimitExceeded)` 目前**故意**退回 `MenuDeck/Error/InvalidDeck`（⛔ 宁可笼统，绝不印 `{0}`）。
//     A9 钉的就是这条**过渡态**；接线那一批要把它改成 `MaxCopiesReached`。
//
// ============================ 怎么接（本文件不碰宿主聚合）============================
//  `public static void Run();` —— **零前提**（只读卡池 `CardDatabase.Load()`；不建窗口、不写盘）。
//  挂法（调度台）：`RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里**同一物理行**追加
//      Guard("B141_W31_Assert", B141_W31_Assert.Run); _pass += B141_W31_Assert.Pass; _fail += B141_W31_Assert.Fail;
using System;
using System.Collections.Generic;
using System.Text;
using RuleEngine;
using UnityEngine;

/// <summary>执行写手 `B141-W31`：卡组数据 4 条（`Describe` 键 / `Validate` 闸序 / `CardIds` null 守卫 / 导入串两处）的回归断言。</summary>
public static class B141_W31_Assert
{
    const string P = "[B141W31] ";

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get; private set; }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    static bool Str(string got, string want, string msg)
    {
        if (got == want) { Pass++; Debug.Log(P + "   ✓ " + msg + "（" + Show(got) + "）"); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg + "｜实际 " + Show(got) + "，期望 " + Show(want)); return false;
    }

    static string Show(string s) { return s == null ? "<null>" : "\"" + s + "\""; }

    // ============================================================ 入口
    public static void Run()
    {
        Pass = 0; Fail = 0;
        A_DescribeKeys();

        var pool = CardDatabase.Load();
        if (pool == null || pool.Count == 0)
        {
            // ⛔ 不静默跳过：B/C/D 三组全都要真卡池，拿不到就明确报红。
            Ok(false, "前提：卡池读得到（`CardDatabase.Load()` 返 0 张 ⇒ B/C/D 三组本轮全部未验）");
            Debug.Log(P + " 合计 " + Pass + " 过 / " + Fail + " 红（B/C/D 未跑）");
            return;
        }
        var byId = new Dictionary<string, CardDef>();
        foreach (var c in pool) if (c != null && !byId.ContainsKey(c.Id)) byId[c.Id] = c;
        Func<string, CardDef> lookup = id =>
        {
            CardDef c;
            return (id != null && byId.TryGetValue(id, out c)) ? c : null;
        };

        // 夹具：一张督军 + 它阵营的两张普通卡 + 一张**别的阵营**的普通卡。
        CardDef hero = null;
        foreach (var c in pool) if (c != null && c.Type == "hero") { hero = c; break; }
        if (!Ok(hero != null, "前提：卡池里找得到一张督军"))
        { Debug.Log(P + " 合计 " + Pass + " 过 / " + Fail + " 红（B/C/D 未跑）"); return; }

        CardDef sameUnit = null, sameUnit2 = null, foreignUnit = null;
        foreach (var c in pool)
        {
            if (c == null || c.Type != "unit") continue;
            // ⚠️ 夹具要**准入干净**：否则 B2 的「只剩张数问题」会先撞 ⑥b（`EffectOnlyCard` / `InvalidCard`）⇒ 假红。
            if (DeckRules.CannotAddToDeck(c) || DeckRules.IsNotDeckable(c)) continue;
            if (DeckRules.SameFaction(c.Faction, hero.Faction))
            {
                if (sameUnit == null) sameUnit = c;
                else if (sameUnit2 == null && c.Id != sameUnit.Id) sameUnit2 = c;
            }
            else if (foreignUnit == null) foreignUnit = c;
        }
        bool okA = Ok(sameUnit != null && sameUnit2 != null, "前提：找得到两张同阵营、准入干净的普通卡");
        bool okB = Ok(foreignUnit != null, "前提：找得到一张别阵营、准入干净的普通卡");
        if (!okA || !okB)
        { Debug.Log(P + " 合计 " + Pass + " 过 / " + Fail + " 红（B/C/D 未跑）"); return; }

        B_ValidateGateOrder(hero, sameUnit, sameUnit2, foreignUnit, lookup);
        C_NullCardIds(hero, lookup);
        D_ImportDeckString(pool, hero, sameUnit, lookup);

        Debug.Log(P + " 合计 " + Pass + " 过 / " + Fail + " 红");
    }

    // ============================================================
    //  A：3-18 —— `Describe` 的三条键名映射（纯枚举，不需要卡池）
    // ============================================================
    static void A_DescribeKeys()
    {
        const string Pre = DeckRules.TermPrefix;      // "MenuDeck/Error/"

        Str(DeckRules.Describe(DeckError.InvalidCard), Pre + "InvalidCard",
            "A1（前提）`InvalidCard` = `MenuDeck/Error/InvalidCard`（原版具名键，不回归）");

        Str(DeckRules.Describe(DeckError.EffectOnlyCard), Pre + "InvalidCard",
            "A2 ★★ 3-18：准入档 `EffectOnlyCard` 归 **`InvalidCard`**"
          + "（原版 `CardDeck__CanAddCard.c:40-47`：`+0x4c != 0 && +0x48 != 5` 那支给的就是 `DAT_1842cf4f0` = `MenuDeck/Error/InvalidCard`）"
          + "｜🧨 改回 `InvalidDeck` ⇒ 本句红");

        Str(DeckRules.Describe(DeckError.CopyLimitExceeded), Pre + "MaxCopiesReached",
            "A3 ★★ 3-18：同名上限 `CopyLimitExceeded` ⇒ **`MaxCopiesReached`**"
          + "（`CardDeck__CanAddCard.c:58,65-71` 的 `GetMaxCopiesInDeck` 支）"
          + "｜🧨 拼枚举名（老写法）⇒ 红");

        Str(DeckRules.Describe(DeckError.TooManyCards), "MenuDeck/Tip/DeckFull",
            "A4 ★★ 3-18：满编 `TooManyCards` ⇒ **`MenuDeck/Tip/DeckFull`**"
          + "（`CardDeck__CanAddCard.c:166-174` 的 `DAT_1842d0be0`；⚠️ 它**不在** `MenuDeck/Error/` 前缀下）"
          + "｜🧨 拼 `MenuDeck/Error/TooManyCards`（那条键官方表里没有）⇒ 红");

        // 🧨 灭自证：直接钉住「不许退回 `TermPrefix + 枚举名`」这个老写法。
        Ok(DeckRules.Describe(DeckError.CopyLimitExceeded) != Pre + "CopyLimitExceeded",
            "A5 🧨 灭自证：`CopyLimitExceeded` **不再**拼成 `MenuDeck/Error/CopyLimitExceeded`（官方表 0 命中 ⇒ 会印键名）");
        Ok(DeckRules.Describe(DeckError.TooManyCards) != Pre + "TooManyCards",
            "A6 🧨 灭自证：`TooManyCards` **不再**拼成 `MenuDeck/Error/TooManyCards`（同上）");

        Str(DeckRules.Describe(DeckError.LegendaryTotalExceeded), Pre + "MaxLegendayCopiesReached",
            "A7（不回归）传奇总数那档仍是 `MaxLegendayCopiesReached`（⚠️ 原版把 `Legendary` 拼成 `Legenday`，⛔ 别改拼写）");

        Ok(DeckRules.Describe(DeckError.EffectOnlyCard) == DeckRules.Describe(DeckError.InvalidCard),
            "A8 ★ `EffectOnlyCard` 与 `InvalidCard` 现在**同键** —— 这是原版形状（原版这两类在 `CanAddCard` 里就是一句话）"
          + "｜⛔ 别再照 2026-11-03 那条「两档必须两句不同文案」的断言写（`B121_WC_Assert.cs:91` 那条已随本判据失效）");

        // A9：`{0}` 那条**过渡态** —— 宁可笼统，绝不把占位符印到屏幕上。
        string keyC = DeckRules.ErrorTextKey(DeckError.CopyLimitExceeded);
        Ok(keyC != null && keyC.IndexOf("{0}", StringComparison.Ordinal) < 0,
            "A9 ★★ `ErrorTextKey(CopyLimitExceeded)` **不许**带 `{0}`（`MaxCopiesReached` 的文案是「卡组已有{0}张该牌的副本」，"
          + "`{0}` = 同名份数上限；本层拿不到那张卡 ⇒ 不填就不许上屏）｜实际 " + Show(keyC));
        Str(keyC, Pre + "InvalidDeck",
            "A10 ……过渡态 = 退回 catch-all `InvalidDeck`（接线点 = `CardPresentation/Deck/DeckRuntime.DeckErrorText`，本批白名单外；"
          + "接上 `string.Format` 之后 A9 不变、A10 要改成 `MaxCopiesReached`）");

        Str(DeckRules.ErrorTextKey(DeckError.TooManyCards), "MenuDeck/Tip/DeckFull",
            "A11 ★ 上屏键：`TooManyCards` 走 `Describe` 那条具名映射（`Tip/DeckFull` 没有占位符 ⇒ 能直接上屏）");
    }

    // ============================================================
    //  B：3-20 —— `Validate` 的闸序（逐张闸在前、张数闸在后）
    // ============================================================
    static void B_ValidateGateOrder(CardDef hero, CardDef u1, CardDef u2, CardDef foreign,
                                    Func<string, CardDef> lookup)
    {
        // B1：牌**少**（3 张）**且**混了一张异阵营 ⇒ 必须报 `WrongFaction`（逐张闸在前）。
        //   ⚠️ 三张**互不相同**：同名重复会先撞 ⑦ 同名上限，测的就不是闸序了。
        var mixed = new PlayerDeck("闸序-混阵营", hero.Id, null,
                                   new List<string> { u1.Id, u2.Id, foreign.Id });
        var e1 = DeckRules.Validate(mixed, lookup);
        Ok(e1 == DeckError.WrongFaction,
            "B1 ★★ 3-20：「牌少 + 混阵营」报的是 **`WrongFaction`**（原版 `ValidateDeck.c:50` 先跑 `ValidateDeckCards`、`.c:69` 才比张数）"
          + "｜实际 " + e1 + "｜🧨 把 ④ 张数闸搬回 ⑤ 之前 ⇒ 回 `TooFewCards` ⇒ 本句红");

        // B2：对照 —— 同一副去掉那张异阵营卡 ⇒ 只剩张数问题（证明 B1 的 `WrongFaction` 是**闸序**给的，不是别的原因）。
        var few = new PlayerDeck("闸序-不混阵营", hero.Id, null,
                                 new List<string> { u1.Id, u2.Id });
        var e2 = DeckRules.Validate(few, lookup);
        Ok(e2 == DeckError.TooFewCards,
            "B2（对照）同阵营的 2 张 ⇒ `TooFewCards`（张数闸仍在，只是**排在**逐张闸之后）｜实际 " + e2);

        // B3：张数闸**本身还生效** —— 造一副 30 张全合法的，再多塞一张。
        var ids = new List<string>();
        foreach (var c in CardDatabase.Load())
        {
            if (ids.Count >= DeckRules.CardCount(false)) break;
            if (c == null || c.Type != "unit" || !DeckRules.SameFaction(c.Faction, hero.Faction)) continue;
            if (DeckRules.CannotAddToDeck(c) || DeckRules.IsNotDeckable(c)) continue;
            for (int i = 0; i < DeckRules.CopyLimit(c.Rarity) && ids.Count < DeckRules.CardCount(false); i++) ids.Add(c.Id);
        }
        if (ids.Count == DeckRules.CardCount(false))
        {
            CardDef extra = null;
            foreach (var c in CardDatabase.Load())
            {
                if (c == null || c.Type != "unit" || !DeckRules.SameFaction(c.Faction, hero.Faction)) continue;
                if (ids.Contains(c.Id) || DeckRules.IsLegendary(c.Rarity)) continue;
                if (DeckRules.CannotAddToDeck(c) || DeckRules.IsNotDeckable(c)) continue;
                extra = c; break;
            }
            if (extra != null)
            {
                var full = new PlayerDeck("闸序-满编", hero.Id, null, ids);
                Ok(DeckRules.Validate(full, lookup) == DeckError.None,
                   "B3a（前提）凑出来的 30 张是全合法的（⛔ 前提不成立 ⇒ B3b 是假绿）");
                full.CardIds.Add(extra.Id);
                var e3 = DeckRules.Validate(full, lookup);
                Ok(e3 == DeckError.TooManyCards,
                   "B3b ★ 31 张、其余全合法 ⇒ `TooManyCards`（张数闸只是挪了位置、没被删掉）｜实际 " + e3);
            }
            else Ok(false, "B3（前提）找不到可多塞一张的卡 ⇒ B3b 未验，报出来而不是跳过");
        }
        else Ok(false, $"B3（前提）凑不出 30 张合法牌（实得 {ids.Count}）⇒ B3b 未验");
    }

    // ============================================================
    //  C：3-22 —— `CardIds == null` 不许 NRE
    // ============================================================
    static void C_NullCardIds(CardDef hero, Func<string, CardDef> lookup)
    {
        var d = new PlayerDeck("空表", hero.Id, null, null);
        d.CardIds = null;                       // 模拟「JSON 缺键 / 反序列化给 null」
        var e = DeckError.None;
        Exception caught = null;
        try { e = DeckRules.Validate(d, lookup); }
        catch (Exception ex) { caught = ex; }

        Ok(caught == null,
            "C1 ★★ 3-22：`CardIds == null` 走 `Validate` **不抛**（`RemoveInvalidCards` 早就有同一条守卫，`Validate` 这条老路一直裸 `foreach`）"
          + (caught == null ? "" : "｜实际抛了 " + caught.GetType().Name + "：" + caught.Message)
          + "｜🧨 去掉 null 守卫 ⇒ 本句红（`NullReferenceException`）");
        if (caught == null)
        {
            Ok(d.CardIds != null, "C2 ……而且把卡表补成了空表（不是把 null 留在对象里，后面的调用点还会炸）");
            Ok(e == DeckError.TooFewCards,
                "C3 ……按**空表**校验 ⇒ `TooFewCards`（有督军 ⇒ 不是 `NoWarlord`）｜实际 " + e);
        }
    }

    // ============================================================
    //  D：3-24 —— 导入串两处（缺 `;` 照收 · 多余督军/防御卡丢弃）
    // ============================================================
    static string Enc(string plain) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(plain)); }

    static void D_ImportDeckString(List<CardDef> pool, CardDef hero, CardDef unit, Func<string, CardDef> lookup)
    {
        CardDef def = null, def2 = null, hero2 = null;
        foreach (var c in pool)
        {
            if (c == null) continue;
            if (c.Type == "defence")
            {
                if (def == null) def = c;
                else if (def2 == null && c.Id != def.Id) def2 = c;
            }
            else if (c.Type == "hero" && hero2 == null && c.Id != hero.Id) hero2 = c;
        }

        // ---- D1：**缺 `;`** 的串 —— 原版照样解析（`.c:54-59` 没有尾巴 ⇒ gameMode 保持 0）----
        var d1 = DeckLibrary.ImportString(Enc("无分号:" + hero.Id + ":" + unit.Id + ":" + unit.Id), lookup);
        if (Ok(d1 != null, "D1 ★★ 3-24：缺 `;` 的卡组串**不再判死**（原版 `CardDeck__DeserializeDeckString.c:54-59` 只在 `Split(';').Length > 1` 时才 `Int32.Parse` 尾巴，否则 `gameMode` = 0 并继续）"
                              + "｜🧨 把 `else return null;` 加回去 ⇒ 本句红"))
        {
            Str(d1.Name, "无分号", "D1b 名字照 `Split(':')[0]` 取（空名守卫不存在，与原版 `DeckBasicSetup` 同口径）");
            Str(d1.WarlordId, hero.Id, "D1c 督军位认出来了（缺 `;` 不影响按类型摘）");
            Ok(d1.CardIds != null && d1.CardIds.Count == 2, "D1d 两张普通卡都在｜实际 " + (d1.CardIds == null ? -1 : d1.CardIds.Count));
            Ok(d1.GameMode == 0, "D1e 缺 `;` ⇒ `GameMode` = **0**（Classic），⛔ 不是因为解析失败才 0 ｜实际 " + d1.GameMode);
        }

        // ---- D2：带 `;` 的回归（尾巴照读）----
        var d2 = DeckLibrary.ImportString(Enc("有分号:" + hero.Id + ":" + unit.Id + ";13"), lookup);
        Ok(d2 != null && d2.GameMode == 13,
            "D2（回归）带 `;` 的串照旧读尾巴 ⇒ `GameMode` = 13（改造只放宽了「没有 `;`」那一支）｜实际 "
          + (d2 == null ? "<null>" : d2.GameMode.ToString()));

        // ---- D3：多余督军**丢弃** ----
        if (Ok(hero2 != null, "D3（前提）卡池里找得到第二张督军"))
        {
            var d3 = DeckLibrary.ImportString(Enc("双督军:" + hero.Id + ":" + hero2.Id + ":" + unit.Id + ";0"), lookup);
            if (Ok(d3 != null, "D3a 双督军的串解析得到"))
            {
                Str(d3.WarlordId, hero.Id, "D3b 串里**第一张**督军进督军位（原版倒序扫、最后一次赋值 = 下标最小那张）");
                Ok(!d3.CardIds.Contains(hero2.Id),
                    "D3c ★★ 3-24：**第二张督军被丢弃**，⛔ 不进普通卡位（原版 `.c:141-151` 每遇到一张就 `RemoveAt`）"
                  + "｜🧨 恢复老写法 `rest.Add(id)` ⇒ 本句红");
            }
        }

        // ---- D4：多余防御卡**丢弃** ----
        if (def == null || def2 == null)
        {
            Ok(false, "D4（前提）卡池里找得到两张防御卡（拿不到 ⇒ 本条未验，报出来而不是跳过）");
            return;
        }
        var d4 = DeckLibrary.ImportString(Enc("双防御:" + def.Id + ":" + def2.Id + ":" + hero.Id + ":" + unit.Id + ";0"), lookup);
        if (Ok(d4 != null, "D4a 双防御的串解析得到"))
        {
            Str(d4.DefensiveId, def.Id, "D4b 串里**第一张**防御卡进防御位");
            Ok(!d4.CardIds.Contains(def2.Id),
                "D4c ★★ 3-24：**第二张防御卡被丢弃**，⛔ 不进普通卡位（原版 `.c:152-159`）"
              + "｜🧨 恢复老写法 `rest.Add(id)` ⇒ 本句红");
        }
    }
}
