// B123_C2_Assert.cs — 执行代理 `B123-C2`（2026-11-03）· 收藏窗 / 卡组编辑窗四件
//
// ============================ 它盯的是什么 ============================
//  · `B121-COLL-24`：页头 `Clear filters` 的**显隐 / 命中闸 / 清空动作**三处必须读**同一页自己的**筛选
//    （卡背页读 `_cosmoFilter`）。改前三处各写 `!State.Filter.IsEmpty` ⇒ 卡背页「有 chip 却没有钮」。
//  · `B121-COLL-13`：`ImportDeckPopup.OnImported` 是**死回调**（全仓 0 处调用）⇒ 字段 / 形参 / 实参三处已删。
//  · `B121-COLL-14`：`DeckLibrary.ImportString` 不再给空名补「导入的卡组」 —— 原版不补（见下）。
//  · `B121-COLL-12`：**零改动交付**（队列占位表在报告里）—— 这里只钉一条**记账断言**，谁重排梯子谁同步账。
//
// ============================ 判据出处（⛔ 本文件不重抄数值推导）============================
//  · 原版那条钮清的是**当前那个 controller** 的 `filters[]`：`decomp_full/CardCollectionFilterController__SetFiltersToDeck.c:29`
//  · 空名不补：`decomp_full/CardDeck__DeserializeDeckString.c`（`Split(':')[0]` 原样交给 `DeckBasicSetup`）
//    ＋ `decomp_full/CardDeck__DeckBasicSetup.c:21-24`（`*(param_1+0x10) = param_2` 直接写名字，**无空名守卫**；
//      补 GUID 的是 `+0x18` 那个 **id**）
//  · `OnImported` 死回调：`grep -rn "OnImported" Assets` 改前只有 字段/形参/赋值/一句注释 四处，**0 个调用点**
//  · 队列占位表：报告 `资料/完全复刻/报告/B123_C2_收藏与卡组窗.md` §四（扫描口径 = 全 `Assets/**/*.cs` 的 `Qxxx = 31xx`）
//
// ============================ 🧨 灭自证（一条改动不能两边都绿）============================
//  🧨 1：`HdrClearOn` 退回 `!State.Filter.IsEmpty` ⇒ A4/A5 红（卡背页有筛选、钮却不出现）。
//  🧨 2：只改显隐、不把 `KeyLive` 换成 `HdrClearOn` ⇒ A6（藏起来还能点）红。
//  🧨 3：只改显隐+命中闸、不清 `_cosmoFilter` ⇒ A8 红（点了清空、卡背筛选还在）。
//  🧨 4：删掉 `ApplyCosmoFilter` 尾那句 `RefreshHeader()` ⇒ A4 红（点完阵营，钮要等下一次别的刷新才出现）。
//  🧨 5：把 `OnImported` 字段加回去 ⇒ B1 红；`Create` 加回第二参 ⇒ B2 红。
//  🧨 6：把「导入的卡组」兜底写回 `ImportString` ⇒ C1 红。
//
// ============================ 怎么接 ============================
//  宿主（`Editor/DeckScene.cs` 由**调度台**接线，⛔ 本文件不改 `*Scene.cs`）里一行：
//      B123_C2_Assert.Run(); pass += B123_C2_Assert.Pass; fail += B123_C2_Assert.Fail;
//  判绿红：`grep "^B123C2"` 的**合计行**；逐条按**行首** `B123C2 ✓` / `B123C2 ✗` 数（⛔ 别 `grep -c ✗`）。
//  ⚠️ 本文件**不建持久资产**、**不落盘**（`DeckLibrary.Add` 那条**故意不测**：它会 `SaveOrWarn` 写玩家存档 ⇒
//     改成读代码 + `ImportString` 那条单测，见报告 §三「没覆盖到的」）。
using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using CardPresentation;
using RuleEngine;

/// <summary>执行代理 `B123-C2`：`Clear filters` 按页判据 · `OnImported` 死回调已删 · 空名不补文案 的回归断言。</summary>
public static class B123_C2_Assert
{
    const string Tag = "B123C2";
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

    /// <summary>跑整段。</summary>
    public static void Run()
    {
        _pass = _fail = 0;

        try { A_HdrClearFollowsPageFilter(); }
        catch (Exception e) { Check(false, "A 组（`Clear filters` 按页判据）抛异常：" + e.GetType().Name + "：" + e.Message); }

        try { B_ImportCallbackRemoved(); }
        catch (Exception e) { Check(false, "B 组（`OnImported` 已删）抛异常：" + e.GetType().Name + "：" + e.Message); }

        try { C_EmptyDeckNameKept(); }
        catch (Exception e) { Check(false, "C 组（空名不补文案）抛异常：" + e.GetType().Name + "：" + e.Message); }

        try { D_QueueLedger(); }
        catch (Exception e) { Check(false, "D 组（队列记账）抛异常：" + e.GetType().Name + "：" + e.Message); }

        Debug.Log(Tag + " ==== 合计：通过 " + _pass + " · 失败 " + _fail + " ====");
    }

    // ============================================================ A · `Clear filters` 按页判据（`B121-COLL-24`）
    static void A_HdrClearFollowsPageFilter()
    {
        var go = new GameObject("B123C2_DeckRuntime");
        try
        {
            var rt = go.AddComponent<DeckRuntime>();
            rt.Build(new DeckLibrary());

            // ---- 前提（先立前提再断结论：前提不成立时红要指向「没建出来」，不是「改错了」）----
            Check(rt.UiHasQuad("hdr_clear"), "A0-前提：`hdr_clear` **建出来了**（不在 ⇒ 下面几条全是空断）");

            rt.UiSetTab(2);                                   // 卡背页
            Check(rt.ActiveTab == 2, "A1-前提：已经切到卡背页（`ActiveTab == 2`，实得 " + rt.ActiveTab + "）");
            Check(string.IsNullOrEmpty(rt.CosmoFilterArmy), "A2-前提：卡背阵营筛选初始为空（实得「" + (rt.CosmoFilterArmy ?? "null") + "」）");
            Check(!rt.UiQuadActive("hdr_clear"), "A3-前提：无筛选 ⇒ 钮藏着（改前改后都该是这样）");

            // 设一条卡背筛选（**只设卡背那一份**，卡牌那份一个字不动）
            rt.UiCosmoFilterRow("$fac:Ultramarines");
            Check(rt.CosmoFilterArmy == "Ultramarines", "A4-前提：卡背阵营筛选已设成 `Ultramarines`（实得「" + (rt.CosmoFilterArmy ?? "null") + "」）");
            Check(rt.State.Filter.IsEmpty, "A5-前提：**卡牌那份筛选仍是空的**（本组要证的正是「卡背那份单独算数」）");

            // ---- 被测：显隐 ----
            Check(rt.UiQuadActive("hdr_clear"),
                  "A6 ★ `B121-COLL-24`：卡背页**有筛选** ⇒ 页头 `Clear filters` **出现** —— 🧨 把 `HdrClearOn` 退回 `!State.Filter.IsEmpty` ⇒ 本条红；"
                + "🧨 删掉 `ApplyCosmoFilter` 尾那句 `RefreshHeader()` ⇒ 本条也红（点完阵营要等下一次别的刷新才出现）");

            // ---- 被测：命中闸（藏起来却能点到 = 静默失败，本仓红线）----
            float x, y, w, h;
            Check(rt.UiBtnRect("hdr_clear", out x, out y, out w, out h), "A7-前提：`hdr_clear` 的命中矩形还在 `_btns` 里（在，才能测「点不点得到」）");
            string top = rt.UiTopKeyAt(x + w * 0.5f, y + h * 0.5f);
            Check(top == "hdr_clear", "A8 ★ 看得见 ⇒ 命中闸放行（`UiTopKeyAt` 实得 " + (top ?? "null") + "）—— 🧨 `KeyLive` 不改 `HdrClearOn` ⇒ 本条红");

            // ---- 被测：清空动作（只清、不清干净 = 「按钮出现了、点下去筛选还在」）----
            bool ownedBefore = rt.CosmoFilterOwned;
            rt.UiClearFilters();
            Check(string.IsNullOrEmpty(rt.CosmoFilterArmy),
                  "A9 ★ `B121-COLL-24`：清空动作把 `_cosmoFilter.Faction` 清掉了（实得「" + (rt.CosmoFilterArmy ?? "null") + "」）"
                + " —— 🧨 删掉 `ClearFilters` 里那句 `ClearCosmoFilterKeepOwned()` ⇒ 本条红");
            Check(rt.CosmoFilterOwned == ownedBefore,
                  "A10 `Owned` 是**开关**、不是 `filters[]` 项 ⇒ 清空**不动它**（实得 " + rt.CosmoFilterOwned + "，期望 " + ownedBefore + "）");
            Check(!rt.UiQuadActive("hdr_clear"), "A11 清完之后钮**自己藏回去**（显隐与动作同一条判据 ⇒ 同一拍收口）");
            float x2, y2, w2, h2;   // ⚠️ 重新读一次矩形：`MoveHdrClear` 会随着 chip 数量把钮挪走（清空后 chip 归 0 ⇒ 钮回静态位）
            string top2 = rt.UiBtnRect("hdr_clear", out x2, out y2, out w2, out h2)
                        ? rt.UiTopKeyAt(x2 + w2 * 0.5f, y2 + h2 * 0.5f) : "（矩形没了）";
            Check(top2 != "hdr_clear", "A12 藏起来之后**不再吃点击**（`UiTopKeyAt` 实得 " + (top2 ?? "null") + "）—— 🧨 `KeyLive` 不改 `HdrClearOn` ⇒ 本条红");

            // ---- 反证：卡牌页不认卡背那份（谓词按**当前页**取，不是「任一份非空就常显」）----
            rt.UiCosmoFilterRow("$fac:Goff");
            Check(rt.CosmoFilterArmy == "Goff", "A13-前提：卡背筛选又设回来了（`Goff`）");
            rt.UiSetTab(0);
            Check(!rt.UiQuadActive("hdr_clear"),
                  "A14 反证：卡牌页本页无筛选 ⇒ 钮**不出现**（卡背那份不着数）—— 🧨 把谓词写成「任一份非空」⇒ 本条红");

            // ---- 反证 2：在卡背页点清空，**不许**顺手把卡牌那份也清掉（原版那颗钮只清「当前那个 controller」）----
            rt.UiFilterRow("$fac:Ultramarines");
            Check(!rt.State.Filter.IsEmpty, "A15-前提：卡牌页设上了阵营筛选（`State.Filter` 非空）");
            rt.UiSetTab(2);
            rt.UiClearFilters();
            Check(!rt.State.Filter.IsEmpty,
                  "A16 ★ 在卡背页点清空 ⇒ **卡牌那份筛选一个字节不动** —— 🧨 把 `ClearFilters` 里 `if (_tab != 2)` 去掉 ⇒ 本条红");
            Check(string.IsNullOrEmpty(rt.CosmoFilterArmy), "A17 卡背那份被清掉（同一个动作的另一半）");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    // ============================================================ B · `OnImported` 死回调已删（`B121-COLL-13`）
    static void B_ImportCallbackRemoved()
    {
        var t = typeof(ImportDeckPopup);
        var f = t.GetField("OnImported", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Check(f == null, "B1 ★ 字段 `OnImported` 已删（反射找人找不到）—— 🧨 加回去 ⇒ 本条红");

        var m = t.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
        Check(m != null, "B2-前提：`ImportDeckPopup.Create` 还在（不然下面那条是空断）");
        if (m != null)
        {
            var ps = m.GetParameters();
            Check(ps.Length == 1 && ps[0].ParameterType == typeof(WindowsManager),
                  "B3 ★ `Create` **只剩一个形参**（`WindowsManager`）—— 实得 " + ps.Length + " 个"
                + "（死回调那个形参已删；🧨 加回第二个 ⇒ 本条红）");
        }
    }

    // ============================================================ C · 空名不补「导入的卡组」（`B121-COLL-14`）
    static string B64(string s) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(s)); }

    static void C_EmptyDeckNameKept()
    {
        // 原版串格式 = `<名字>:<id>:<id>…;<gameMode>`（`CardDeck__DeserializeDeckString.c`）。
        // `lookup == null` ⇒ 走「不解析卡、只解出名字」那一支（不碰任何卡表 / 存档）。
        var emptyName = DeckLibrary.ImportString(B64(":abc;0"), null);
        Check(emptyName != null, "C0-前提：空名串解析成功（`ImportString` 非 null）");
        if (emptyName != null)
            Check(emptyName.Name == "",
                  "C1 ★ 空名**保持空名**（实得「" + (emptyName.Name ?? "null") + "」）—— 原版 `CardDeck__DeckBasicSetup.c:21-24` 直接写名字、**无空名守卫**；"
                + "🧨 把 `? \"导入的卡组\" :` 兜底写回 `DeckLibrary.ImportString` ⇒ 本条红");

        var named = DeckLibrary.ImportString(B64("MyDeck:abc;0"), null);
        Check(named != null && named.Name == "MyDeck",
              "C2 非空名**逐字带过来**（实得「" + (named != null ? named.Name : "null") + "」）");
    }

    // ============================================================ D · 队列记账（`B121-COLL-12` · 2026-11-03 整梯 −2 已落地 · 本段守**新号**）
    static void D_QueueLedger()
    {
        // 🔴 **这不是「期望的行为」，是一颗【记账钉子】** —— `B121-COLL-12` 起初判的是「**没有安全空档 ⇒ 零改动 + 占位表**」；
        //    （占位表 → 报告 §四）。这条断言的作用：**谁将来重排了这段梯子，谁就得同步改这里 + 报告 + 账**，
        //    而不是让撞号在无人察觉的情况下继续漂。
        int ban = DeckSelectionPopup.QDsHit + 1;      // `MenuDraw.DeckCell` 尾句 `DeckBan.Apply(…, qOverlay + 1)`，而本窗 `qOverlay = QDsText + 1 = QDsHit` ⇒ 现读 **3128**
        Check(ban == 3128 && ban < DeckInfoPopup.QDIOver,
              "D1 ★ `B121-COLL-12` 整梯 −2 后：Ban = `QDsHit + 1` = " + ban + " **严格小于** `DeckInfoPopup.QDIOver` = " + DeckInfoPopup.QDIOver
            + " ⇒ **不再同号**（3128 < 3130；撞号是重排**之前**的休眠坑，已由这次重排解掉）。"
            + "🧨 灭自证：把 `QDsHit` 改回 3129（或整梯加回 2 档）⇒ Ban = 3130 ⇒ `ban == 3128` 与 `ban < QDIOver` **同时假** ⇒ 本条红");
        Check(ban == 3128 && ban < ImportDeckPopup.QImp,
              "D2 ★ 同一档也**严格小于** `ImportDeckPopup.QImp` = " + ImportDeckPopup.QImp + "（3128 < 3130；这两扇**同在卡组编辑场景**，撞号会真盖住）"
            + " —— 🧨 同上：`QDsHit` 回 3129 ⇒ 本条红（`Banned Text` = 3129 也仍 < 3130）");
    }
}
