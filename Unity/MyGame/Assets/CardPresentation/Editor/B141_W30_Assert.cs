// B141_W30_Assert.cs — 执行写手 `B141_W30`（第一百二十八会话 · 2026-11-04 · 波 `W129` · 批 `B141`）
//   负责的 7 条（判据 → `资料/完全复刻/判据/B141_W129_对账_卡组色彩.md` §一；报告 → `资料/完全复刻/报告/B141_W129_DeckFilter.md`）：
//     · `3-01`（模型半）阵营筛选行：有督军**或有牌** ⇒ **整行隐藏**并锁 `deckArmy`
//     · `3-02` 删掉「玩家显式点了阵营 ⇒ 绕过督军闸」那条越权路（`armyPicked`）
//     · `3-03` 有牌**没督军** ⇒ 卡池只列 `deckArmy` 那一阵营的战将
//     · `3-04` 类型筛按**原版 `cardType`**（`defence` = 20 = Tactic）⇒ 点 Tactic 不再丢那 39 张
//     · `3-18`（返回码半）同名上限 / 整副满 / 准入 三档**返回值分得开**，且准入那档回 `InvalidCard`
//     · `3-30` 锁住之后 `Clear filters` **不清阵营**（池子结果不变）
//     · `3-31` `Neutral` 按原版枚举序（= 0）**排最前**
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 H5 vs H6：同一个「有督军」的 state，**卡组编辑那一档**要隐藏、**收藏窗那一档**要显示 ——
//     只按 state 判 ⇒ H6 红；永不隐藏 ⇒ H5 红；两边不能一起绿。
//  🧨 L1 vs L2：同一个 state、同一份筛选，`WarlordGatedPool`（卡组编辑）下 B 阵营看不见、
//     收藏窗那一路**看得见** ⇒ 「直接把 `FactionOk` 废掉」会让 L2 红，「锁不生效」会让 L1 红。
//  🧨 L3 vs L4：有牌没督军 ⇒ 只列 `deckArmy` 的战将；同样没督军但**空卡组** ⇒ 各阵营战将都在。
//  🧨 T1/T2 vs T3：`defence` 要落进 Tactic、但 `unit` 不能（「匹配一切」的实现 T1 绿 T3 红）。
//  🧨 T5：真实卡池里 `CardType==20` 的**每一张**都必须命中 Tactic，而 `0/10` 的**一张都不许**命中。
//  🧨 R4：三档返回码必须**互不相同**（并档 ⇒ 红）；R6 又要求准入那档拿得到**真词条**。
//  🧨 N1 vs N2：`Neutral` 要排首，但 `ArmyOrder` 那 13 条的顺序**一个字不许动**（宿主按 `ArmyOrder[0]` 断言）。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零场景前提**（不建场景、不读存档、不写 PlayerPrefs；唯一的外部依赖是
//  `CardDatabase.Load()`，拿不到时那三条自动转成「前提不成立」的出声 `✗`，⛔ 不静默跳过）。
//  宿主聚合行**同一物理行**追加：
//      B141_W30_Assert.Run(); _sink.Pass += B141_W30_Assert.Pass; _sink.Fail += B141_W30_Assert.Fail;
//  判绿红：`grep "^B141W30"` 的**合计行**；逐条按行首 `B141W30 ✓` / `B141W30 ✗` 数（⛔ 别 `grep -c ✗`）。

using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行写手 `B141_W30`：W129 卡组筛选模型 7 条的验收点。</summary>
public static class B141_W30_Assert
{
    static int _pass, _fail;
    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B141W30 ✓ " + msg); }
        else { _fail++; Debug.LogError("B141W30 ✗ " + msg); }
    }

    /// <summary>`Check(实测, 期望, msg)`（同一族断言里最常用的形状；两值不等 ⇒ 红）。</summary>
    static void Check(int got, int want, string msg) { Check(got == want, msg + "（期望 " + want + "）"); }

    /// <summary>`Check(实测错误码, 期望错误码, msg)`。</summary>
    static void Check(DeckError got, DeckError want, string msg) { Check(got == want, msg + "（实测 `" + got + "` / 期望 `" + want + "`）"); }

    // ---- 夹具 ------------------------------------------------------------
    const string FacA = "SpaceWolves", FacB = "Ultramarines";

    static CardDef Hero(string id, string fac)
    { return new CardDef(id, "W30 " + id, "hero", "", "Common", fac, 0, 1, 30, 0, inventoryOptions: 0, showInCollectionOptions: 5); }
    static CardDef Unit(string id, string fac)
    { return new CardDef(id, "W30 " + id, "unit", "", "Common", fac, 1, 1, 1, 0, inventoryOptions: 0, showInCollectionOptions: 5); }
    static CardDef Def(string id, string fac)
    { return new CardDef(id, "W30 " + id, "defence", "", "Common", fac, 1, 0, 0, 0, inventoryOptions: 5, showInCollectionOptions: 5); }
    /// <summary>`inventoryOptions = CantAddToDeck(5)` 且 `showInCollectionOptions != 5` 的**非防御卡**
    /// ⇒ 原版 `CanAddCard.c:40`（`inv != 0 && show != 5`）的 `MenuDeck/Error/InvalidCard` 那一支。
    /// ⚠️ **两列都要**（只把 `inv` 设成 5、`show` 留 5 ⇒ `CannotAddToDeck` 的 `&&` 不成立 ⇒ 这张卡**能加**，
    /// 那条断言就成了假红 —— 原版判据本身就是 `&&`）。</summary>
    static CardDef BadUnit(string id, string fac)
    { return new CardDef(id, "W30 " + id, "unit", "", "Common", fac, 1, 1, 1, 0, inventoryOptions: 5, showInCollectionOptions: 0); }

    static DeckEditorState Fresh()
    {
        var pool = new List<CardDef>
        {
            Hero("W30_HA", FacA), Hero("W30_HB", FacB),
            Unit("W30_UA", FacA), Unit("W30_UB", FacB),
            Def("W30_DA", FacA), BadUnit("W30_BA", FacA),
        };
        return new DeckEditorState(pool);
    }

    static int HiddenArmyCells(List<FilterPanelModel.Cell> cells, out int total)
    {
        int hidden = 0; total = 0;
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].Key == null || !cells[i].Key.StartsWith("$fac:")) continue;
            total++;
            if (cells[i].Hidden) hidden++;
        }
        return hidden;
    }

    static bool HasId(List<CardDef> list, string id)
    {
        for (int i = 0; i < list.Count; i++) if (list[i] != null && list[i].Id == id) return true;
        return false;
    }

    /// <summary>唯一的入口（宿主聚合行里调）。</summary>
    public static void Run()
    {
        H_ArmyRowHidden();
        L_ArmyLockAndDeckArmy();
        T_TypeFilterByCardType();
        R_ReturnCodes();
        N_NeutralOrder();
    }

    // ==================================================================
    //  H：`3-01`（模型半）—— 有督军 / 有牌 ⇒ Army 行**整行隐藏**；收藏窗那一档**永远显示**
    //     判据 = 原版 `CardArmyFilter__SetFilterToDeck.c:16-25` + `FilterToArmy.c` 末句 `SetActive(false)`
    // ==================================================================
    static void H_ArmyRowHidden()
    {
        // ① 空卡组（没督军没牌）⇒ 行要露出来（原版 `ClearArmyFilter` + `SetActive(true)`）
        var empty = Fresh();
        Check(FilterPanelModel.ArmyRowVisible(empty, true),
              "H1 ★ 空卡组（没督军 / 没牌）⇒ `ArmyRowVisible(卡组编辑)` = **真**（原版 `SetActive(true)` 那一支）");
        Check(!FilterPanelModel.ArmyLocked(empty), "H2 空卡组 ⇒ **没有锁**（`ArmyLocked` = 假）");

        // ② 有督军 ⇒ 隐藏 + 锁
        var wl = Fresh();
        wl.WarlordGatedPool = true;
        wl.SetWarlord("W30_HA");
        Check(!FilterPanelModel.ArmyRowVisible(wl, true),
              "H3 ★★ 有督军 ⇒ `ArmyRowVisible(卡组编辑)` = **假**（整行隐藏）"
            + " ｜🧨 永不隐藏 ⇒ 本句红");
        Check(FilterPanelModel.ArmyLocked(wl), "H4 ……而且**锁住**了（`ArmyLocked` = 真，= 原版 `setted`）");

        // ③ 有牌但没督军 ⇒ **也**隐藏（原版 `CardCount(hero:1) > 0` 那一条）
        var cardsOnly = Fresh();
        cardsOnly.WarlordGatedPool = true;
        cardsOnly.Deck.CardIds.Add("W30_UB");
        Check(!FilterPanelModel.ArmyRowVisible(cardsOnly, true),
              "H5 ★★ **有牌、没督军** ⇒ 同样整行隐藏（原版 `.c:16-19` 是 `或` 关系：`CardCount(hero:1) > 0`）"
            + " ｜🧨 只按「有没有督军」判 ⇒ 本句红");

        // ④ 同一份 state、**收藏窗那一档** ⇒ 永远显示（原版 `SetFiltersToDeck` 全库只有卡组编辑那一个调用点）
        Check(FilterPanelModel.ArmyRowVisible(wl, false),
              "H6 ★★ 同一个有督军的 state，**收藏窗那一档**（`deckEdit = false`）⇒ 仍然显示"
            + " ｜🧨 把隐藏条件写成「只看 state」⇒ 本句红（H3/H5 与 H6 **不可能同时满足**）");

        // ⑤ 真正 `Build` 出来的格子 + 小标题：`Hidden` 落在格上、标题上，且**格数一个不少**（只标记、不删）
        var cellsWl = new List<FilterPanelModel.Cell>();
        FilterPanelModel.Build(wl, 331.7f, cellsWl,
                               raritySpX: FilterPanelModel.RaritySpXDeckEdit,
                               costSpX: FilterPanelModel.CostSpXDeckEdit,
                               typePadL: FilterPanelModel.TypePadLDeckEdit,
                               costRowNeedsWarlord: FilterPanelModel.CostRowNeedsWarlordDeckEdit);
        int totalWl; int hiddenWl = HiddenArmyCells(cellsWl, out totalWl);
        Check(totalWl > 0, "H7（前提）`Build` 出得来阵营格（共 " + totalWl + " 格）");
        Check(hiddenWl == totalWl, "H8 ★★ 有督军 ⇒ **每一颗** `$fac:` 格都 `Hidden`（" + hiddenWl + "/" + totalWl + "）");
        Check(cellsWl.Count >= totalWl, "H9 ……但格子**照建**（`cells.Count` = " + cellsWl.Count
              + " ≥ 阵营格数）—— 节点照建、只是不画（⛔ 别改成「不建」）");

        var titlesWl = new List<FilterPanelModel.Title>();
        FilterPanelModel.BuildTitles(wl, 331.7f, titlesWl, costRowNeedsWarlord: FilterPanelModel.CostRowNeedsWarlordDeckEdit);
        Check(titlesWl.Count == 4, "H10 小标题仍然发**四条**（顺序/下标是两个宿主共用的映射，实得 " + titlesWl.Count + "）");
        Check(titlesWl.Count == 4 && titlesWl[0].Text == "Army" && titlesWl[0].Hidden,
              "H11 ★★ `Army` 小标题**一起隐藏**（原版那颗 `Army Filter` 节点一关，`Title` + `Content` 一起不画）");

        // ⑥ 同一 state、收藏窗那一档 ⇒ 一格都不隐藏 + 标题不隐藏（🧨 与 H8/H11 成对）
        var cellsColl = new List<FilterPanelModel.Cell>();
        FilterPanelModel.Build(wl, 335.31f, cellsColl);
        int totalColl; int hiddenColl = HiddenArmyCells(cellsColl, out totalColl);
        Check(totalColl == totalWl && hiddenColl == 0,
              "H12 ★★ **收藏窗那一档**（缺省实参）⇒ 阵营格一颗都不隐藏（" + hiddenColl + "/" + totalColl + "）"
            + " ｜🧨 与 H8 成对：只按 state 判 ⇒ 本句红");
        var titlesColl = new List<FilterPanelModel.Title>();
        FilterPanelModel.BuildTitles(wl, 335.31f, titlesColl);
        Check(titlesColl.Count == 4 && !titlesColl[0].Hidden, "H13 ……收藏窗的 `Army` 标题同样不隐藏");

        // ⑦ 卡组清空（督军 + 牌都去掉）⇒ 行重新露出来（原版 `ClearArmyFilter` + `SetActive(true)` 那一支）
        var cleared = Fresh();
        cleared.WarlordGatedPool = true;
        cleared.SetWarlord("W30_HA");
        cleared.Deck.CardIds.Add("W30_UA");
        cleared.ClearCards();
        Check(FilterPanelModel.ArmyRowVisible(cleared, true) && !FilterPanelModel.ArmyLocked(cleared),
              "H14 ★ 清空整副（督军与牌都没了）⇒ 行**重新露出来**、锁解除");
    }

    // ==================================================================
    //  L：`3-02` / `3-03` / `3-30` —— 锁住时阵营由 `deckArmy` 独占（玩家点过的阵营不再作数；
    //      `Clear filters` 也清不掉它）；有牌没督军 ⇒ 只列 `deckArmy` 的战将
    // ==================================================================
    static void L_ArmyLockAndDeckArmy()
    {
        // ① `3-02`：有督军（A）时，就算筛选里还留着 B 阵营，池子里也不许冒出 B 的卡
        var s = Fresh();
        s.WarlordGatedPool = true;
        s.SetWarlord("W30_HA");
        var f = DeckFilter.None;
        f.Factions = new HashSet<string> { FacB };
        s.SetFilter(f);
        var visLocked = s.VisibleCards();
        Check(!HasId(visLocked, "W30_UB") && !HasId(visLocked, "W30_HB"),
              "L1 ★★ 有督军 A + 筛选里残留阵营 B ⇒ 卡池里**看不见** B 的卡（原版行已隐藏、没有切换入口）"
            + " ｜🧨 把 `armyPicked` 那条越权路写回来 ⇒ 本句红");
        Check(HasId(visLocked, "W30_UA"), "L1b ……而 A 阵营自己的卡**在**（锁的是阵营，不是把池子清空）");

        // ② 对照：同一个 state 换成**收藏窗那一路**（`WarlordGatedPool = false`）⇒ 阵营筛选照常生效
        var c = Fresh();
        c.SetWarlord("W30_HA");
        var f2 = DeckFilter.None;
        f2.Factions = new HashSet<string> { FacB };
        c.SetFilter(f2);
        var visColl = c.VisibleCards();
        Check(HasId(visColl, "W30_UB"),
              "L2 ★★ 收藏窗那一路（不开 `WarlordGatedPool`）⇒ 点阵营 B **看得到** B 的卡"
            + " ｜🧨 与 L1 成对：把 `FactionOk` 废掉 / 把锁做成全局 ⇒ 本句红");

        // ③ `3-03`：有牌、**没督军** ⇒ 只列 `deckArmy` 那一阵营的**战将**
        var nw = Fresh();
        nw.WarlordGatedPool = true;
        nw.Deck.CardIds.Add("W30_UB");            // 空卡组加第一张 ⇒ `deckArmy` = B（原版 `AddCard` 那条不变量）
        var visNoWl = nw.VisibleCards();
        Check(HasId(visNoWl, "W30_HB"),
              "L3 ★★ 有牌没督军 ⇒ `deckArmy`（B）那一阵营的**战将**在池里（玩家从这里挑督军）");
        Check(!HasId(visNoWl, "W30_HA"),
              "L3b ★★ ……而**别的阵营**的战将**不在**（原版 `FilterToArmy(deckArmy)` 之后行隐藏）"
            + " ｜🧨 只判 `c.Type != \"hero\"`（旧写法）⇒ 本句红");

        // ④ 对照：同样没督军、但**空卡组** ⇒ 各阵营战将都在（🧨 与 L3b 成对）
        var nwEmpty = Fresh();
        nwEmpty.WarlordGatedPool = true;
        var visEmpty = nwEmpty.VisibleCards();
        Check(HasId(visEmpty, "W30_HA") && HasId(visEmpty, "W30_HB"),
              "L4 ★★ 没督军 + **空卡组** ⇒ 两个阵营的战将都在（原版 `ClearArmyFilter` 那一支）"
            + " ｜🧨 与 L3b 成对：把「有牌」那条判据写成「恒真」⇒ 本句红");

        // ⑤ `3-30`：锁住之后 `Clear filters`（= 把筛选整个换成 `DeckFilter.None`）**不清阵营**
        var cl = Fresh();
        cl.WarlordGatedPool = true;
        cl.SetWarlord("W30_HA");
        var before = cl.VisibleCards();
        var fB = DeckFilter.None; fB.Factions = new HashSet<string> { FacB };
        cl.SetFilter(fB);
        var mid = cl.VisibleCards();
        cl.SetFilter(DeckFilter.None);              // = `DeckRuntime.ClearFilters` 那一句
        var after = cl.VisibleCards();
        Check(mid.Count == before.Count && after.Count == before.Count,
              "L5 ★★ 锁住时：留一份 B 筛选 / 再 `Clear filters` ⇒ 卡池张数**三次都一样**（" +
              before.Count + " / " + mid.Count + " / " + after.Count + "）"
            + " ｜判据 = 原版 `CardArmyFilter__ClearFilters.c:10-13`（`setted` 为真 ⇒ 直接 return）");
        Check(HasId(after, "W30_UA") && !HasId(after, "W30_UB"),
              "L5b ★ ……而且清空之后**仍是** A 阵营那一批（阵营由 `deckArmy` 独占，不随清空变）");

        // ⑥ 对照：**没锁**（空卡组）时 `Clear filters` 之前/之后**不同**（🧨 与 L5 成对）
        var fr = Fresh();
        fr.WarlordGatedPool = true;
        var fB2 = DeckFilter.None; fB2.Factions = new HashSet<string> { FacB };
        fr.SetFilter(fB2);
        int nFiltered = fr.VisibleCards().Count;
        fr.SetFilter(DeckFilter.None);
        int nAll = fr.VisibleCards().Count;
        Check(nAll > nFiltered,
              "L6 ★★ 空卡组（没锁）时：点阵营 B 会筛少（" + nFiltered + " < " + nAll + "）"
            + " ｜🧨 与 L5 成对：把「锁」做成恒真 ⇒ 本句红（清不清都一样）");
    }

    // ==================================================================
    //  T：`3-04` —— 类型筛按**原版 `cardType`**：`defence`（20 = Tactic）要落进 Tactic 档
    // ==================================================================
    static void T_TypeFilterByCardType()
    {
        var fT = DeckFilter.None; fT.Types = new HashSet<string> { "tactic" };
        var fU = DeckFilter.None; fU.Types = new HashSet<string> { "unit" };

        Check(DeckFilter.TypeOk(fT, "defence", -1),
              "T1 ★★ 点 Tactic ⇒ **防御卡**通过（真值缺失时按 `type` 折算：`defence` ⇒ `tactic`）"
            + " ｜🧨 没这条折算 ⇒ 本句红（原版点 Tactic = 377、我们改前 338，少的就是这 39 张）");
        Check(DeckFilter.TypeOk(fT, "defence", 20),
              "T2 ★★ ……而**有 `cardType` 真值**时按真值判：`defence` 的 `cardType` = 20 = Tactic ⇒ 通过");
        Check(!DeckFilter.TypeOk(fT, "unit", 0),
              "T3 ★ 点 Tactic ⇒ **单位卡不通过**（`cardType` 0）"
            + " ｜🧨 把匹配写成「一律 true」⇒ 本句红（T1 照样绿）");
        Check(!DeckFilter.TypeOk(fU, "defence", 20),
              "T4 ★ 点 Troops ⇒ 防御卡**不通过**（折算不是单向放行）");
        Check(DeckFilter.TypeOk(fU, "unit", 0) && !DeckFilter.TypeOk(fU, "unit", 20),
              "T4b ★ Troops 档也只认 `cardType == 0`（附魔模板 / 战术卡 20 不许混进来）");

        // ⑤ 真实卡池：`CardType == 20` 的**每一张**都命中 Tactic，`0/10` 的**一张都不许**命中
        List<CardDef> pool = null;
        try { pool = new List<CardDef>(CardDatabase.Load()); } catch (System.Exception e) { Debug.LogWarning("B141W30 卡表加载失败：" + e.Message); }
        if (pool == null || pool.Count < 1000)
        {
            Check(false, "T5 ✗（前提不成立）`CardDatabase.Load()` 拿不到卡表（实得 " +
                  (pool == null ? "null" : pool.Count.ToString()) + " 张）—— 这一条**没有验到**，⛔ 不当绿");
        }
        else
        {
            int n20 = 0, miss20 = 0, wrongHit = 0, nDef = 0;
            string firstMiss = null, firstWrong = null;
            for (int i = 0; i < pool.Count; i++)
            {
                var cd = pool[i];
                if (cd == null) continue;
                bool hit = DeckFilter.TypeOk(fT, cd.Type, cd.CardType);
                if (cd.CardType == 20)
                {
                    n20++;
                    if (cd.Type == "defence") nDef++;
                    if (!hit) { miss20++; if (firstMiss == null) firstMiss = cd.Name + "(" + cd.Type + ")"; }
                }
                else if (cd.CardType == 0 || cd.CardType == 10)
                {
                    if (hit) { wrongHit++; if (firstWrong == null) firstWrong = cd.Name + "(" + cd.Type + "/" + cd.CardType + ")"; }
                }
            }
            Check(n20 > 0 && miss20 == 0,
                  "T5 ★★ 真实卡池里 `cardType == 20` 的 **" + n20 + " 张全部**命中 Tactic（漏 " + miss20 + " 张"
                + (firstMiss == null ? "）" : "，如 `" + firstMiss + "`）"));
            Check(wrongHit == 0,
                  "T6 ★★ 真实卡池里 `cardType ∈ {0,10}` 的卡**一张都没**落进 Tactic（误命中 " + wrongHit + " 张"
                + (firstWrong == null ? "）" : "，如 `" + firstWrong + "`）"));
            Check(nDef > 0, "T7 （前提）池子里真有防御卡（" + nDef + " 张，`cardType` 全是 20）"
                + " —— 这正是原版点 Tactic 比我们改前多的那一批");
        }

        // ⑧ 端点：`VisibleCards` 走的是同一个口（改坏法：调用点忘了传 `c.CardType` ⇒ 只剩字符串折算，
        //    附魔模板那 2 张会掉）—— 这里直接比「按 type 折算」与「按真值」两种口径在真实卡池上的差集
        if (pool != null && pool.Count >= 1000)
        {
            int diff = 0; string firstDiff = null;
            for (int i = 0; i < pool.Count; i++)
            {
                var cd = pool[i];
                if (cd == null) continue;
                bool byTruth = DeckFilter.TypeOk(fT, cd.Type, cd.CardType);
                bool byName = DeckFilter.TypeOk(fT, cd.Type, -1);
                if (byTruth != byName) { diff++; if (firstDiff == null) firstDiff = cd.Name + "(" + cd.Type + "/" + cd.CardType + ")"; }
            }
            Debug.Log("B141W30 （登记）“按 cardType 真值”与“按 type 字符串折算”在现池上差 " + diff + " 张"
                + (diff == 0 ? "（今天两种口径同结果；`enchantment` 那 2 张若进池会分叉）" : "（如 `" + firstDiff + "`）—— 以真值为准，T5/T6 已钉"));
        }
    }

    // ==================================================================
    //  R：`3-18`（返回码半）—— 原版三条错误分得开，准入那档回 `InvalidCard`
    //     判据 = `CardDeck__CanAddCard.c:40`（`InvalidCard`）/ `.c:48`（`MaxCopiesReached`）/ `.c:166`（`MenuDeck/Tip/DeckFull`）
    // ==================================================================
    static void R_ReturnCodes()
    {
        var s = Fresh();
        s.WarlordGatedPool = true;
        s.SetWarlord("W30_HA");

        // ① 同名上限（槽里就是这张督军）⇒ `CopyLimitExceeded`（= 原版 `MenuDeck/Error/MaxCopiesReached`）
        Check(s.CanAdd(s.Find("W30_HA")) == DeckError.CopyLimitExceeded,
              "R1 ★ 槽里**就是这一张** ⇒ `CopyLimitExceeded`（原版 `.c:33-46` 那一支给 `MaxCopiesReached`；"
            + "词条键映射在 `DeckRules.Describe`，见 R5）");

        // ② 准入（`inventoryOptions = 5`、非防御卡）⇒ **`InvalidCard`**（原版 `CanAddCard.c:40` 的具名键）
        var bad = s.Find("W30_BA");
        Check(bad != null, "R2（前提）夹具里有那张 `inv=5` 的非防御卡");
        Check(bad == null ? DeckError.None : s.CanAdd(bad), DeckError.InvalidCard,
              "R2b ★★ 准入被挡 ⇒ **`InvalidCard`**（原版那一支返回 `MenuDeck/Error/InvalidCard`）"
            + " ｜🧨 退回 `EffectOnlyCard` ⇒ 本句红（那条键原版**没有**，显示层会摊成 catch-all `InvalidDeck`）");
        Check(bad != null && s.CanAdd(s.Find("W30_DA")) == DeckError.None,
              "R2c ★ 对照：**防御卡**（同样 `inv=5`）**不被**这条挡（原版 `.c:32-39` 先把它路由走）");

        // ③ 整副满 ⇒ `TooManyCards`（= 原版 `.c:166` 的 `MenuDeck/Tip/DeckFull`）
        //    夹具：1 位督军 + 7 张同阵营单位（同名上限 2）⇒ 遭遇模式 12 张装得满，第 7 种还剩 0 张可加。
        var wide = new List<CardDef> { Hero("W30_HA", FacA) };
        for (int i = 1; i <= 7; i++) wide.Add(Unit("W30_U" + i, FacA));
        var full = new DeckEditorState(wide);
        full.WarlordGatedPool = true;
        var ids = new List<string>();
        for (int i = 1; i <= 6 && ids.Count < 12; i++) { ids.Add("W30_U" + i); ids.Add("W30_U" + i); }
        full.LoadDeck(new PlayerDeck("满的遭遇牌", "W30_HA", null, ids, (int)RuleEngine.GameMode.Skirmish));
        Check(full.Deck.CardIds.Count, 12,
              "R3（前提）遭遇牌 12 张装得满（`LoadDeck` 没把夹具卡摘掉）—— 实得 " + full.Deck.CardIds.Count);
        var extra = full.Find("W30_U7");
        Check(extra != null, "R3b（前提）夹具里有那张**还没加过**的 `W30_U7`");
        Check(extra == null ? DeckError.None : full.CanAdd(extra), DeckError.TooManyCards,
              "R3c ★★ 满 12 张再加一张（非传奇 / 准入干净 / 同阵营）⇒ **`TooManyCards`**"
            + "（原版 `.c:166` 的 `MenuDeck/Tip/DeckFull` 那一支）");

        // ④ 三档**互不相同**（并档会让「只按返回值出文案」的调用点说反）
        Check(DeckError.CopyLimitExceeded != DeckError.TooManyCards
              && DeckError.TooManyCards != DeckError.InvalidCard
              && DeckError.CopyLimitExceeded != DeckError.InvalidCard,
              "R4 ★★ 三档返回码**互不相同**（同名上限 / 整副满 / 准入）｜🧨 并成同一档 ⇒ 本句红");

        // ⑤ 目标词条键**真实存在**（W31 那半 `Describe` 映射的落点；⛔ 不存在的键会印键名上屏）
        Check(Loc.HasAnyTerm("MenuDeck/Error/MaxCopiesReached"),
              "R5 ★ `MenuDeck/Error/MaxCopiesReached` 在官方词条表里（`CopyLimitExceeded` 该映射到它）");
        Check(Loc.HasAnyTerm("MenuDeck/Tip/DeckFull"),
              "R5b ★ `MenuDeck/Tip/DeckFull` 在官方词条表里（`TooManyCards` 该映射到它 —— ⚠️ 是 **Tip** 族、不是 Error 族）");
        Check(Loc.HasAnyTerm("MenuDeck/Error/InvalidCard"),
              "R6 ★★ `MenuDeck/Error/InvalidCard` 在表里 ⇒ R2b 那档**今天就能印出人话**（不是键名）"
            + "｜实读 = 「" + Loc.T("MenuDeck/Error/InvalidCard") + "」");
        Check(DeckRules.Describe(DeckError.InvalidCard) != DeckRules.Describe(DeckError.EffectOnlyCard),
              "R7 ★ `InvalidCard` 与 `EffectOnlyCard` 的**键不同**（后者原版没有 ⇒ 归 catch-all）");
    }

    // ==================================================================
    //  N：`3-31` —— `Neutral` 按原版枚举序（`CardArmy.Neutral = 0`）排最前；`ArmyOrder` 那 13 条不动
    // ==================================================================
    static void N_NeutralOrder()
    {
        var pool = new List<CardDef>
        {
            Unit("W30_NU", "Neutral"), Unit("W30_UB", FacB), Unit("W30_UA", FacA),
        };
        var s = new DeckEditorState(pool);
        var facs = s.Factions();
        Check(facs.Count >= 3, "N1（前提）夹具里三个阵营都进表（实得 " + facs.Count + "）");
        Check(facs.Count > 0 && facs[0] == "Neutral",
              "N2 ★★ `Neutral` **排最前**（原版 `CardArmy.Neutral = 0`，13 个真阵营是 10…130）"
            + "｜实得首项 = " + (facs.Count > 0 ? facs[0] : "(空)"));
        Check(facs.IndexOf(FacB) >= 0 && facs.IndexOf(FacA) >= 0 && facs.IndexOf(FacB) < facs.IndexOf(FacA),
              "N3 ★ 其余阵营仍按 `ArmyOrder` 的原版序（Ultramarines 在 SpaceWolves 之前）");
        Check(System.Array.IndexOf(DeckEditorState.ArmyOrder, "Neutral") < 0,
              "N4 ★★ `ArmyOrder` 本身**不含** `Neutral`（那是原版 `options` 的 13 条；"
            + " `Editor/DeckScene.cs` 有一条断言拿 `ArmyOrder[0]` 当 Ultramarines）"
            + " ｜🧨 「直接把 Neutral 塞进表首」⇒ 本句红、且那条宿主断言跟着红");
    }
}
