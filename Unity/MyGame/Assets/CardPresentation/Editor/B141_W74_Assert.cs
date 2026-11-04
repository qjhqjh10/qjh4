// B141_W74_Assert.cs — 执行写手 `B141-W74`（第一百二十八会话 · 2026-11-04 · 波 `W129` · 批 `B141`）
//   负责：`B141-ARMY-LATCH` —— 阵营锁是**闩**（原版 `CardArmyFilter.setted`）。
//
//   原版判据（`d:/2/tools/decomp_full/CardArmyFilter__SetFilterToDeck.c` 逐句现读）：
//     · `:16-21`  `deckHero(+0x40) != null` **或** `CardDeck.CardCount(deck, 0) > 0` ⇒ 走「锁」那支；
//                 两者皆空 ⇒ `:29-39` else 支：`setted = 0` + `ClearArmyFilter` + `SetActive(true)`。
//     · `:22-24`  锁那支第一句 `if (param_1[8] != '\0') return;`（`+0x40` = `setted`）⇒ **锁过就不再重算**
//                 ⇒ 原版**锁住之后又换督军，筛会停在旧阵营**（它自己的行为，照抄）。
//     · `:25-26`  `FilterToArmy(deck + 0x50)` + `setted = 1`。
//   复位时机：① 新窗口 = 新 filter 实例（我们 = `DeckEditorState.LoadDeck` / `NewDeck`）；
//             ② 卡组清空到「无督军 + 无牌」（`.c:29-39` 的 else 支）。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 L4 vs L5：同一个序列，**先刷新再换督军** ⇒ 停在旧阵营（L4）；
//     **刷新之前就换到 B** ⇒ 闩落在 B（L5）。「永远按当前值重算」⇒ L4 红；
//     「闩在 SetWarlord 那一刻就写死第一个督军」⇒ L5 红。两边不能一起绿。
//  🧨 L4 vs R3：不重开窗 ⇒ 停在 A（L4）；`LoadDeck` 重开窗 ⇒ 按新督军 B 重锁（R3）。
//     `ResetArmyLatch` 每拍都调 ⇒ L4 红；忘了复位 ⇒ R3 红。
//  🧨 E2/E3：清空整副要**解除**闩（E2），之后重新加 B 的牌要锁成 **B** 而不是旧 A（E3）——
//     漏掉 `.c:29-39` else 支的复位 ⇒ E3 红；把「锁」做成恒真 ⇒ E2 红。
//  🧨 C1/C2：收藏窗那一路（不开 `WarlordGatedPool`）**不许**写闩（C1），且玩家点的阵营照常生效（C2）——
//     把闩做成全局 ⇒ C1/C2 红；把 `FactionOk` 废掉 ⇒ C2 红。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零场景前提**（不建场景、不读存档、不写 PlayerPrefs；无外部数据依赖）。
//  宿主聚合行**同一物理行**追加：
//      B141_W74_Assert.Run(); _sink.Pass += B141_W74_Assert.Pass; _sink.Fail += B141_W74_Assert.Fail;
//  判绿红：逐条按行首 `B141W74 ✓` / `B141W74 ✗` 数（⛔ 别 `grep -c ✗`）。

using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行写手 `B141-W74`：阵营筛选闩（原版 `CardArmyFilter.setted`）的验收点。</summary>
public static class B141_W74_Assert
{
    static int _pass, _fail;
    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B141W74 ✓ " + msg); }
        else { _fail++; Debug.LogError("B141W74 ✗ " + msg); }
    }

    // ---- 夹具 ------------------------------------------------------------
    const string FacA = "SpaceWolves", FacB = "Ultramarines";

    static CardDef Hero(string id, string fac)
    { return new CardDef(id, "W74 " + id, "hero", "", "Common", fac, 0, 1, 30, 0, inventoryOptions: 0, showInCollectionOptions: 5); }
    static CardDef Unit(string id, string fac)
    { return new CardDef(id, "W74 " + id, "unit", "", "Common", fac, 1, 1, 1, 0, inventoryOptions: 0, showInCollectionOptions: 5); }

    static DeckEditorState Fresh()
    {
        var pool = new List<CardDef>
        {
            Hero("W74_HA", FacA), Hero("W74_HB", FacB),
            Unit("W74_UA", FacA), Unit("W74_UB", FacB),
        };
        return new DeckEditorState(pool);
    }

    static bool HasId(List<CardDef> list, string id)
    {
        for (int i = 0; i < list.Count; i++) if (list[i] != null && list[i].Id == id) return true;
        return false;
    }

    /// <summary>唯一的入口（宿主聚合行里调）。</summary>
    public static void Run()
    {
        L_LatchHoldsOldArmy();
        R_ResetOnReopen();
        E_EmptyDeckClearsLatch();
        C_CollectionWindowUnaffected();
        G_ResolverGuard();
    }

    // ==================================================================
    //  L：闩本身 —— 锁住之后换督军 ⇒ 仍锁**旧**阵营（原版 `.c:22-24` 早退句）
    // ==================================================================
    static void L_LatchHoldsOldArmy()
    {
        // ① 有督军 A ⇒ 第一次卡池刷新把它锁成 A
        var s = Fresh();
        s.WarlordGatedPool = true;
        s.SetWarlord("W74_HA");
        var before = s.VisibleCards();
        Check(HasId(before, "W74_UA") && !HasId(before, "W74_UB"),
              "L1（前提）有督军 A ⇒ 卡池只列 A 阵营（A 的在 / B 的不在）");
        Check(s.ArmyLatchSettled && s.ArmyLatchFaction == FacA,
              "L2 ★ 第一次卡池刷新之后，闩已锁在 A（`ArmyLatchFaction` = " + (s.ArmyLatchFaction ?? "null") + "）"
            + " ｜判据 = 原版 `CardArmyFilter__SetFilterToDeck.c:25-26`（`FilterToArmy(deckArmy)` + `setted = 1`）");

        // ② 换督军到 B（`SetWarlord` 是玩家/夹具都能走的那条换将路，会写 `deckArmy`）
        s.SetWarlord("W74_HB");
        Check(s.DeckArmyFaction() == FacB, "L3（前提）换督军到 B 之后 `deckArmy` = B（实得 "
              + (s.DeckArmyFaction() ?? "null") + "）");

        // ③ 关键：**重新刷新卡池**，闩仍然停在 A（原版 `.c:22-24` 早退）
        var after = s.VisibleCards();
        Check(!HasId(after, "W74_UB") && !HasId(after, "W74_HB"),
              "L4 ★★ 闩生效：换督军到 B 之后再刷新，卡池**仍停在旧阵营 A**（B 的卡一个都不冒出来）"
            + " ｜🧨 改成「每拍按当前 `deckArmy` 重算」⇒ 本句红（原版自己的行为，照抄）");
        Check(HasId(after, "W74_UA") && HasId(after, "W74_HA"),
              "L4b ……而 A 阵营的卡仍然在（锁的是一档阵营，不是把池子清空）");
        Check(s.ArmyLatchFaction == FacA,
              "L4c ……闩里存的仍然是 A（实得 " + (s.ArmyLatchFaction ?? "null") + "）");
        Check(!FilterPanelModel.ArmyRowVisible(s, true),
              "L4d ……Army 行仍然隐藏（行显隐看**当前**「有督军/有牌」，与闩无关）");

        // ④ 灭自证：**刷新之前**就换到 B ⇒ 闩落在 B（不是「永远锁第一个督军」）
        var s2 = Fresh();
        s2.WarlordGatedPool = true;
        s2.SetWarlord("W74_HA");
        s2.SetWarlord("W74_HB");            // 中间没有任何一次卡池刷新
        var vis2 = s2.VisibleCards();
        Check(s2.ArmyLatchFaction == FacB && HasId(vis2, "W74_UB") && !HasId(vis2, "W74_UA"),
              "L5 ★★ 灭自证：**刷新之前**就换到 B ⇒ 闩落在 B（B 的卡看得见、A 的看不见）"
            + " ｜🧨 与 L4 成对：把闩写成「在 `SetWarlord` 那一刻锁第一个督军」⇒ 本句红");
    }

    // ==================================================================
    //  R：重开窗 = 原版新 filter 实例 ⇒ 闩复位，按**新**督军重锁
    // ==================================================================
    static void R_ResetOnReopen()
    {
        var r = Fresh();
        r.WarlordGatedPool = true;
        r.SetWarlord("W74_HA");
        r.VisibleCards();                    // 锁 A
        r.SetWarlord("W74_HB");              // deckArmy = B
        Check(!HasId(r.VisibleCards(), "W74_UB"), "R1（前提）闩住之后 B 看不见（同 L4）");

        // `LoadDeck` = 开窗 / 换卡组 / 丢弃回滚那一拍（`DeckRuntime.Build:1365`、`RollBackEditingCopy:5738`）
        var edited = r.Deck;                 // 当前这副（督军 B）
        r.LoadDeck(edited);
        Check(!r.ArmyLatchSettled,
              "R2 ★ 重开窗（`LoadDeck`）⇒ 闩复位（`ArmyLatchSettled` = 假）"
            + " ｜判据 = 原版新窗口 = 新 `CardArmyFilter` 实例 ⇒ `setted` 从 0 起");

        var reopen = r.VisibleCards();
        Check(HasId(reopen, "W74_UB") && !HasId(reopen, "W74_UA"),
              "R3 ★★ 重开窗之后按**新**督军 B 重锁 ⇒ B 的卡看得见、A 的看不见"
            + " ｜🧨 与 L4 成对：`ResetArmyLatch` 每拍都调 ⇒ L4 红；忘了复位 ⇒ 本句红");

        // 新建卡组（`NewDeck`）同样是「换一副」⇒ 也要复位
        var n = Fresh();
        n.WarlordGatedPool = true;
        n.SetWarlord("W74_HA");
        n.VisibleCards();
        Check(n.ArmyLatchSettled, "R4（前提）`NewDeck` 之前闩是锁着的");
        n.NewDeck("W74 新卡组");
        Check(!n.ArmyLatchSettled, "R5 ★ `NewDeck`（换一副卡组）同样复位闩");
    }

    // ==================================================================
    //  E：卡组清空到「无督军 + 无牌」⇒ `.c:29-39` else 支：setted = 0
    // ==================================================================
    static void E_EmptyDeckClearsLatch()
    {
        var e = Fresh();
        e.WarlordGatedPool = true;
        e.SetWarlord("W74_HA");
        e.VisibleCards();
        Check(e.ArmyLatchSettled, "E1（前提）清空之前闩锁在 A");

        e.ClearCards();                      // 督军 + 卡表都空
        var eVis = e.VisibleCards();
        Check(!e.ArmyLatchSettled && !FilterPanelModel.ArmyLocked(e),
              "E2 ★ 清空整副（无督军 + 无牌）⇒ 闩**解除**、也不再锁"
            + " ｜判据 = 原版 `CardArmyFilter__SetFilterToDeck.c:29-39`（else 支把 `setted` 写 0）");
        Check(FilterPanelModel.ArmyRowVisible(e, true), "E2b ……Army 行重新露出来（`SetActive(true)` 那一支）");

        // 之后重新加一张 B 的牌 ⇒ `deckArmy` 派生为 B ⇒ 必须锁成 **B**（不是旧 A）
        e.Deck.CardIds.Add("W74_UB");
        var eVis2 = e.VisibleCards();
        Check(e.ArmyLatchFaction == FacB && HasId(eVis2, "W74_HB") && !HasId(eVis2, "W74_HA"),
              "E3 ★★ 清空后重新加 B 的牌 ⇒ 锁成 **B**（闩里实得 " + (e.ArmyLatchFaction ?? "null")
              + "；只列 B 的战将）｜🧨 漏掉 else 支的复位 ⇒ 仍停在旧 A、本句红");
    }

    // ==================================================================
    //  C：收藏窗那一路（`WarlordGatedPool = false`）**不参与**闩
    // ==================================================================
    static void C_CollectionWindowUnaffected()
    {
        var c = Fresh();
        c.SetWarlord("W74_HA");              // 有督军，但收藏窗从不下发卡组给筛选栏
        c.VisibleCards();
        Check(!c.ArmyLatchSettled && c.ArmyLatchFaction == null,
              "C1 ★ 收藏窗那一路 ⇒ **不写闩**（原版 `SetFiltersToDeck` 全库只有卡组编辑窗一个调用点）");

        var f = DeckFilter.None;
        f.Factions = new HashSet<string> { FacB };
        c.SetFilter(f);
        var vis = c.VisibleCards();
        Check(HasId(vis, "W74_UB") && HasId(vis, "W74_HB"),
              "C2 ★★ 收藏窗照常按玩家点的阵营 B 筛（B 的卡都在）"
            + " ｜🧨 与 C1 成对：把闩做成全局 ⇒ C1 红；把 `FactionOk` 废掉 ⇒ 本句红");
        Check(!c.ArmyLatchSettled && HasId(vis, "W74_UB"),
              "C3 ……而且这一路刷新**不会**顺手把闩写进去");
    }

    // ==================================================================
    //  G：唯一判据口 `FilterPanelModel.ArmyLockFaction` 的护栏
    // ==================================================================
    static void G_ResolverGuard()
    {
        var empty = Fresh();                 // 空卡组：没有督军、没有牌
        empty.WarlordGatedPool = true;
        Check(FilterPanelModel.ArmyLockFaction(empty) == null,
              "G1 空卡组 ⇒ `ArmyLockFaction` = `null`（没锁，调用方走 `Filter.Factions` 普通路）");

        var g = Fresh();
        g.WarlordGatedPool = true;
        g.SetWarlord("W74_HA");
        string lockedAtA = FilterPanelModel.ArmyLockFaction(g);
        g.SetWarlord("W74_HB");
        string stillA = FilterPanelModel.ArmyLockFaction(g);
        Check(lockedAtA == FacA && stillA == FacA,
              "G2 ★ 同一个口连读两次（中间换督军到 B）⇒ 两次都返回 **A**（闩 = 早退，⛔ 不是当前值）");
        Check(FilterPanelModel.ArmyLockFaction(null) == null, "G3 `state == null` ⇒ 返回 `null`（不抛）");
    }
}
