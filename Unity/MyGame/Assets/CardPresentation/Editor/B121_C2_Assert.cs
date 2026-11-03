// B121_C2_Assert.cs — 执行代理 `B121-C2`（2026-11-03）· 收藏弹窗三扇 + 导入链路
//
// ============================ 它盯的是什么（逐条对着施工单）============================
//  A. **导入链拆成两站**（施工单 #2）：`CollectionData.ParseDeck` **只解析、不落库**；解析成功 ⇒
//     `ImportDeckPopup` 开 `DeckInfoPopup(State=Import)` 再关自己（原版 `ImportDeckPopup__TryImport.c:20-40`）。
//  B. **`Confirm` 的禁用态**（施工单 #1）：出厂 `interactable = false`，输入非空才亮（原版
//     `ImportDeckPopup__OnEnable.c` / `__ValidateField.c`）。
//  C. **错误文案收成一条官方键**（施工单 #3）：`MenuDeck/Share/InvalidDeckString`（原版全类只用这一句）。
//  D. **空串不再报错**（施工单 #4）：原版**没有空串守卫** ⇒ 造一份「无名无卡无督军」的畸形卡组并开信息窗。
//  E. **`Deck Info` 抽屉补 `Cardback Front` 一层**（施工单 #5）：父 ↻+1.60° / 子 ↻−3.43°、
//     387.68×559.32、两个 `Image` 同贴图（`DeckInfoDrawer.cs:14-36`）。
//  F. **`Edit Deck` 在 `state == Import` 时换文案**（施工单 #6）：`MenuDeck/MenuButtons/ImportDeck`。
//  G. **`Deck Selection Popup` 空态照原版硬编码英文**（施工单 #7）：原版那颗 TMP **没有 `Localize`**。
//
// ============================ 判据（逐条实读）============================
//  · `d:/2/tools/decomp_full/ImportDeckPopup__TryImport.c`（失败支 = 一条 `LocalizedString`；成功支 =
//    构造 `DeckInfoContext{Deck, State=1, SelectButton=null, CheckOwnership=true}` → `OpenWindow<DeckInfoPopup>` → `Close`）
//  · `d:/2/tools/decomp_full/ImportDeckPopup__OnEnable.c`（`set_interactable(importButton, 0)` + 错误行 `SetActive(0)`）
//  · `d:/2/tools/decomp_full/ImportDeckPopup__ValidateField.c`（`interactable = !IsNullOrEmpty(value)` + 错误行 `SetActive(0)`）
//  · `d:/2/tools/decomp_full/CardDeck__DeserializeDeckString.c`（**没有空串守卫**）
//  · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/DeckInfoDrawer.cs:14-36`（两个 `Image` 同一张贴图）
//  · `python 工具/menu_dump.py bundle_menus_assets_all "Deck info Popup" --depth 12`（`Cardback` / `Cardback Front` 的矩形与角度）
//  · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/DeckInfoControls.cs:84-95`（`Edit Deck` 文案键由 `state == 1` 决定）
//  · `Deck Selection Popup ▸ Deck Scroll View ▸ Empty Collection Warning ▸ Warning`（那颗 TMP **无 `Localize`**）
//
// ============================ 灭自证（🧨 一条改动不能两边都绿）============================
//  🧨 1（A1 + A4）：同一句「这不是一条卡组串」—— `ParseDeck` 必须回 **null**，而 `ImportDeck` 必须回
//     **空串 + 官方那句**。把 `ParseDeck` 改成「解析失败也回一个空卡组」⇒ A4 红、A6（空串那一档）会一起变绿
//     ⇒ 所以 A5 单独钉「空串 = 成功」这一支，两条不会同时被一种坏法满足。
//  🧨 2（B1/B2/B3）：同一个窗，`SetTextForTest("")` ⇒ 不可点、`SetTextForTest("x")` ⇒ 可点。
//     把 `RefreshConfirmState` 改成恒 `true` ⇒ B1/B3 红；改成恒 `false` ⇒ B2 红。
//  🧨 3（D1）：合法串导入后 **`DeckCount()` 必须不变**（原版不落库）＋ **新开的那扇必须是 `State=Import`**
//     且 `OverrideDeck != null`。改回「直接 `ImportDeck` 落库 + 关窗」⇒ 第一条计数红。
//  🧨 4（E2）：`Cardback Front` 的世界角必须是 **父 +1.60° + 子 −3.43° = −1.83°**。
//     把子件也设成 +1.60°（= 漏了子件自己那一转）⇒ 本条红。
//  🧨 5（F2/G1 是结构断言）：源码里 `Edit Deck` 那条 `Loc.T` 必须**按 state 分支**、空态那行必须**不再**
//     走官方键 `MenuCollection/NoDecksFound`。改回去 ⇒ 红。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零前提**（自造窗口、`finally` 里全部销毁；不改存档 —— 只**读**库、
//  唯一的写入面是 `ParseDeck`/`ImportDeck` 失败支，它们都不落盘）。
//  挂法（由调度台决定，⛔ 本文件不碰 `Editor/*Scene.cs`）：同一物理行追加
//      B121_C2_Assert.Run(); pass += B121_C2_Assert.Pass; fail += B121_C2_Assert.Fail;
//  判绿红：`grep "^B121C2"` 的**合计行**；逐条按**行首** `B121C2 ✓` / `B121C2 ✗` 数
//  （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CardPresentation;
using RuleEngine;

/// <summary>执行代理 `B121-C2`：收藏弹窗三扇 + 导入链路的回归断言。</summary>
public static class B121_C2_Assert
{
    const string Tag = "B121C2";
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    public static void Run()
    {
        int pass0 = _pass, fail0 = _fail;
        var made = new List<GameObject>();
        try
        {
            GroupA_ParseAndInfo(made);
            GroupB_ImportPopupConfirm(made);
            GroupC_ImportTwoStops(made);
            GroupD_DeckInfoCardbackFront(made);
            GroupE_EditDeckLabelByState(made);
            GroupF_DeckSelectionEmptyText();
        }
        finally
        {
            for (int i = 0; i < made.Count; i++)
                if (made[i] != null) UnityEngine.Object.DestroyImmediate(made[i]);
            Debug.Log(Tag + " === 合计：✓ " + (_pass - pass0) + " · ✗ " + (_fail - fail0)
                    + "（累计 ✓ " + _pass + " · ✗ " + _fail + "） ===");
        }
    }

    // ==================================================================
    //  A · 解析 / 显示规约（纯数据层，不需要任何窗）
    // ==================================================================
    static void GroupA_ParseAndInfo(List<GameObject> made)
    {
        Check(Loc.HasAnyTerm(CollectionData.TermInvalidDeckString),
              "A0 前提：官方键 `" + CollectionData.TermInvalidDeckString + "` 在本地化表里"
            + "（⛔ 不在 ⇒ A1 会退化成比较键名 = 假绿；判据 = `Resources/official_loc_13lang.json` 现读该键在表里）");

        // A1：乱串 ⇒ ParseDeck 回 null
        string why1;
        var bad = CollectionData.ParseDeck("这不是一条卡组串", out why1);
        Check(bad == null, "A1 ★ 施工单 #3：乱串 ⇒ `ParseDeck` 回 **null**"
                         + "（原版 `DeserializeDeckString` 抛/回 null，`TryImport.c` 那一支走错误行）｜实得 " + (bad == null ? "null" : "非null"));

        // A2：错误文案 = 唯一那条官方键
        Check(why1 == Loc.T(CollectionData.TermInvalidDeckString),
              "A2 ★ 施工单 #3：错误文案 = 官方键 `MenuDeck/Share/InvalidDeckString`（EN `Invalid deck string`）"
            + "（期望「" + Loc.T(CollectionData.TermInvalidDeckString) + "」/ 实得「" + why1 + "」）"
            + "｜🧨 改回三条自拟键 `MenuDeck/Error/Import{BadString}` ⇒ 本条红");

        Check(why1 != Loc.T("MenuDeck/Error/ImportBadString") && why1 != Loc.T("MenuDeck/Error/ImportEmpty"),
              "A3 ★ 那三条自拟键**不再被生产链用到**（`MenuDeck/Error/Import{Empty,BadString}` 都不等于本次文案）"
            + "（判据 = `ImportDeckPopup__TryImport.c` 全类只有一句 `LocalizedString`）");

        // A4/A5：空串 ⇒ 原版那个畸形卡组（**不是**报错）
        string why2;
        var empty = CollectionData.ParseDeck("", out why2);
        Check(empty != null && string.IsNullOrEmpty(why2),
              "A4 ★ 施工单 #4：**空串不报错** —— 照原版造出一个卡组对象、`why` 是空串（原版 `CardDeck__DeserializeDeckString.c` 没有空串守卫）"
            + "｜实得 " + (empty == null ? "null（= 又加回守卫了）" : ("非null / why「" + why2 + "」"))
            + "｜🧨 把 `ParseDeck` 改回 `ImportEmpty` 那一支 ⇒ 本条红");
        // 🔴 2026-11-03（`B124_W35`）：同一条静默跳过债 —— 原来是 `if (empty != null) Check(A5)`。
        Check(empty != null, "A5 前提 ★ `ParseDeck(\"\")` 回了**非 null** 对象（⛔ 为 null ⇒ A5 必须红，不得静默跳过）");
        Check(empty != null && string.IsNullOrEmpty(empty.Name) && (empty.CardIds == null || empty.CardIds.Count == 0)
              && string.IsNullOrEmpty(empty.WarlordId),
              "A5 ★ 那一副是「**无名 / 无卡 / 无督军**」的畸形卡组（原版原样：`Split(';')` 长度 1 ⇒ `gameMode=0`、"
            + "`Split(':')[0]` 空 ⇒ 名字空）｜实得 " + (empty == null ? "null" : "名「" + (empty.Name ?? "") + "」/ 卡 " + (empty.CardIds == null ? 0 : empty.CardIds.Count)
            + " / 督军「" + (empty.WarlordId ?? "") + "」"));

        // 🔴 A5b 灭自证（`B124_W2` · 调度台 2026-11-03 补）：**未走解析路**的 `PlayerDeck` ctor 仍应补「新卡组」——
        //   防「把 ctor 那条兜底整条删掉」也能让 A5 变绿（`ParseDeck` 现在显式写 `Name = ""`）。
        //   判据 = `RuleEngine/Core/DeckRules.cs:768`（`Name = string.IsNullOrEmpty(name) ? "新卡组" : name;`，ctor 口径不变）。
        Check(new PlayerDeck("", null, null, null, 0).Name == "新卡组",
              "A5b 灭自证：`new PlayerDeck(\"\", …)` **仍被 ctor 补成「新卡组」**"
            + "（判据 `RuleEngine/Core/DeckRules.cs:768`；A5 走的是 `ParseDeck` 那条**显式写空名**的路，两条路互不相同）"
            + "｜🧨 把 ctor 那条兜底删掉 ⇒ 本条红（而 A5 仍绿 ⇒ 两条路被合并了）");

        // A6：ParseDeck **不落库**
        int n0 = CollectionData.DeckCount();
        string why3;
        var again = CollectionData.ParseDeck("", out why3);
        Check(again != null && CollectionData.DeckCount() == n0,
              "A6 ★ 施工单 #2：`ParseDeck` **只解析、不落库**（前后 `DeckCount()` 都是 " + n0 + "）"
            + "｜🧨 在 `ParseDeck` 里调 `Lib.Add` ⇒ 本条红（原版解析成功**不入库**，保存要等编辑器 `Done`）");

        // A7：ImportDeck（旧口）失败文案也收成同一条
        string why4;
        string badImp = CollectionData.ImportDeck("这不是一条卡组串", out why4);
        Check(string.IsNullOrEmpty(badImp) && why4 == Loc.T(CollectionData.TermInvalidDeckString)
              && CollectionData.DeckCount() == n0,
              "A7 ★ `ImportDeck`（旧口，只剩自检在调）解析失败 ⇒ 空串 + **同一条**官方键、且**不入库**"
            + "｜实得 名「" + (badImp ?? "") + "」/ why「" + why4 + "」/ 库 " + CollectionData.DeckCount());

        // A8：`DeckAt(i)` ≡ `InfoOf(Raw(i))`（规约只留一份 —— `InfoOf` 是唯一实现）
        int mismatch = 0;
        for (int i = 0; i < CollectionData.DeckCount(); i++)
        {
            var a = CollectionData.DeckAt(i);
            var b = CollectionData.InfoOf(CollectionData.Raw(i));
            if (a.Name != b.Name || a.WarlordId != b.WarlordId || a.Faction != b.Faction
                || a.Count != b.Count || a.CardbackId != b.CardbackId || a.GameMode != b.GameMode) mismatch++;
        }
        Check(mismatch == 0, "A8 ★ `DeckAt(i)` 与 `InfoOf(Raw(i))` 六个字段逐下标全等（共 " + CollectionData.DeckCount()
                      + " 套，不等 " + mismatch + " 套）—— 规约只留一份，⛔ 别在两个口各写一套"
                    + "｜🧨 只改其中一个口（例如给 `InfoOf` 加卡背兜底）⇒ 本条红");
        // 🔴 2026-11-03（`B124_W2`）：原文案说「字段全默认」却断 `Name == ""` —— 字段默认是 **`null`**
        //   （`CollectionData.InfoOf:660-663` 的 `if (d == null) return info;` 不给 `Name` 赋值）⇒ 自相矛盾。
        //   改断「**不抛、且是空**」（null 与 "" 都算空）；文案与实现哪一支都不会假绿。
        Check(string.IsNullOrEmpty(CollectionData.InfoOf(null).Name) && CollectionData.InfoOf(null).Count == 0,
              "A9 `InfoOf(null)` 回一个**字段全默认**的 `DeckInfo`（不抛；`Name` 空（null/\"\" 都算空）、`Count` = 0）");
    }

    // ==================================================================
    //  B · `Confirm` 的禁用态（原版 `OnEnable` + `ValidateField`）
    // ==================================================================
    static void GroupB_ImportPopupConfirm(List<GameObject> made)
    {
        var mgr = WindowsManager.Get();
        Check(mgr != null, "B0 前提：拿得到 `WindowsManager`（⛔ 拿不到 ⇒ 下面几条会红，不会假绿）");
        if (mgr == null) return;

        var imp = ImportDeckPopup.Create(mgr);
        made.Add(imp.gameObject);
        Check(imp.TryOpen(null), "B0b 前提：`Import Deck Popup` 开出来了（`TryOpen` 回 true）");

        Check(!imp.ConfirmInteractable,
              "B1 ★ 施工单 #1：出厂 `Confirm` **不可点**（原版 `ImportDeckPopup__OnEnable.c`：`set_interactable(importButton, 0)`）"
            + "｜🧨 出厂就设成 `true` ⇒ 本条红");
        Check(string.IsNullOrEmpty(imp.ErrorText), "B1b 出厂错误行是空的（原版 `OnEnable` 同时 `SetActive(0)`）");

        imp.SetTextForTest("x");
        Check(imp.ConfirmInteractable,
              "B2 ★ 输入非空 ⇒ **点亮**（原版 `ValidateField`：`interactable = !IsNullOrEmpty(value)`）"
            + "｜🧨 `RefreshConfirmState` 恒 false ⇒ 本条红");

        imp.SetTextForTest("");
        Check(!imp.ConfirmInteractable,
              "B3 ★ 再清空 ⇒ **又灰回去**（同一个 `ValidateField`，每次都重算）"
            + "｜🧨 只在 `Build()` 里算一次（不挂输入变化）⇒ 本条红");

        // B4：失败支 —— 错误行出现、按钮重新点亮、窗不关
        imp.SetTextForTest("这不是一条卡组串");
        bool okB4 = imp.TryImport();
        Check(!okB4 && imp.ErrorText == Loc.T(CollectionData.TermInvalidDeckString)
              && imp.CurrentState == WindowState.Open && imp.ConfirmInteractable,
              "B4 ★ 乱串 ⇒ 回 false、错误行 = 官方那句、**窗不关**、按钮**重新点亮**"
            + "（原版 `TryImport` 失败支：`set_interactable(…, 1)`）"
            + "｜实得 ok=" + okB4 + " / err「" + imp.ErrorText + "」/ state=" + imp.CurrentState
            + " / interactable=" + imp.ConfirmInteractable);

        // B5：输入一变就把错误行收掉（原版 `ValidateField` 第一句 `errorGo.SetActive(0)`）
        imp.SetTextForTest("又变了");
        Check(string.IsNullOrEmpty(imp.ErrorText) && imp.ConfirmInteractable,
              "B5 ★ 再输入 ⇒ **错误行立刻收掉**（原版 `ValidateField` 第一句 `SetActive(0)`）｜实得 err「" + imp.ErrorText + "」");

        imp.Close();
    }

    // ==================================================================
    //  C · 导入的两站（解析成功 ⇒ 开 `Deck info Popup(Import)`，**不落库**）
    // ==================================================================
    static void GroupC_ImportTwoStops(List<GameObject> made)
    {
        var mgr = WindowsManager.Get();
        if (mgr == null) { Check(false, "C0 前提：拿得到 `WindowsManager`"); return; }

        // 拿一条**合法**卡组串：优先用库里第 0 套（自检宿主一般会垫一副），没有就现造一副
        string good = null;
        if (CollectionData.DeckCount() > 0) good = DeckLibrary.ExportString(CollectionData.Raw(0));
        if (string.IsNullOrEmpty(good))
        {
            var probe = new PlayerDeck("C2 import probe", null, null, new List<string>(), 0);
            good = DeckLibrary.ExportString(probe);
        }
        Check(!string.IsNullOrEmpty(good), "C0b 前提：拿到了一条合法卡组串（拿不到 ⇒ C1/C2 会红，不会假绿）");
        if (string.IsNullOrEmpty(good)) return;

        int before = CollectionData.DeckCount();
        var imp = ImportDeckPopup.Create(mgr);
        made.Add(imp.gameObject);
        imp.TryOpen(null);
        imp.SetTextForTest(good);

        // 记录「开之前」已有的 DeckInfoPopup，事后只认新开的那一扇
        var oldOnes = new List<DeckInfoPopup>(Object.FindObjectsByType<DeckInfoPopup>(FindObjectsSortMode.None));
        bool ok = imp.TryImport();
        var all = Object.FindObjectsByType<DeckInfoPopup>(FindObjectsSortMode.None);
        DeckInfoPopup opened = null;
        for (int i = 0; i < all.Length; i++)
            if (!oldOnes.Contains(all[i])) { opened = all[i]; break; }
        if (opened != null) made.Add(opened.gameObject);

        Check(ok && imp.CurrentState == WindowState.Closed,
              "C1 ★ 施工单 #2：合法串 ⇒ 本窗**关自己**（原版 `__TryImport.c` 末句 `Close()`）"
            + "｜实得 ok=" + ok + " / state=" + imp.CurrentState);
        Check(opened != null,
              "C2 ★ 第二站真的开了：`Deck info Popup` 被新建出来（原版 `OpenWindow<DeckInfoPopup>`）"
            + "｜🧨 改回「直接落库 + 关窗」⇒ 本条红");
        Check(CollectionData.DeckCount() == before,
              "C3 ★ **这一站不落库**（`DeckCount()` 前后都是 " + before + "；保存要等玩家在编辑器点 `Done`）"
            + "｜🧨 在 `TryImport` 里调 `CollectionData.ImportDeck` ⇒ 本条红");
        // 🔴 2026-11-03（`B124_W35`）：同一条静默跳过债 —— 原来是 `if (opened != null) { C4…C7 }`，
        //   前提一破这四条**从未执行**。改成逐条挂 `opened != null` 前缀（空安全、不抛异常拖垮后面的组）。
        Check(opened != null && opened.State == DeckInfoPopup.DeckInfoState.Import,
              "C4 ★ 那一扇的 `State = Import(1)`（原版 `DeckInfoContext{State = 1}`）｜实得 "
            + (opened == null ? "窗没开 ⇒ 无从判" : opened.State.ToString()));
        Check(opened != null && opened.OverrideDeck != null,
              "C5 ★ 它拿的是**刚解析出来的那个对象**（`OverrideDeck != null`；库里没有它的下标 ⇒ `DeckIndex = −1`）"
            + "｜实得 OverrideDeck=" + (opened == null ? "窗没开 ⇒ 无从判" : (opened.OverrideDeck == null ? "null" : "非null"))
            + (opened == null ? "" : " / DeckIndex=" + opened.DeckIndex));
        Check(opened != null && !opened.SelectButtonProvided,
              "C6 ★ `Select Deck` 那颗**藏着**（原版 `SelectButton = null`）｜实得 "
            + (opened == null ? "窗没开 ⇒ 无从判" : opened.SelectButtonProvided.ToString()));
        Check(opened != null && opened.IsItemShown("Btn:Edit Deck") && !opened.IsItemShown("Opt:Share")
              && !opened.IsItemShown("Btn:Practice Deck"),
              "C7 ★ 该态下只有 `Edit Deck` / `Switch Deck Info` 这类露：`Edit Deck` 露、`Share` 藏、`Practice Deck` 藏"
            + "（判据 = `DeckInfoControls__Initialize.c` 的 SetActive 表）"
            + "｜🧨 窗没开 ⇒ 本条红（旧写法会静默跳过）");
        if (opened != null) opened.Close();
    }

    // ==================================================================
    //  D · `Deck Info` 抽屉的 `Cardback Front`（施工单 #5）
    // ==================================================================
    static void GroupD_DeckInfoCardbackFront(List<GameObject> made)
    {
        var mgr = WindowsManager.Get();
        if (mgr == null) { Check(false, "D0 前提：拿得到 `WindowsManager`"); return; }

        // 🔴 2026-11-03（`B124_W2`）：夹具原来给的是**无督军**的空卡组 ⇒ `InfoOf` 的 `Faction` 是空串
        //   ⇒ `CardArt.DeckCardback(null, "")` 回 null ⇒ `DeckInfoPopup.cs:2145-2147` 整块跳过
        //   （日志 `[DeckInfo] 卡背取不到（`` / 阵营 ）`）⇒ D1 红、D2/D3 静默没跑。
        //   给一个**真督军**（取库里第 0 套的；判据 = 原版 `Deck Info ▸ Cardback` 只在 `GetDeckCardback()` 有值时才建）。
        var probeHero = CollectionData.Raw(0);
        var deck = new PlayerDeck("C2 cardback probe", probeHero != null ? probeHero.WarlordId : null, null, new List<string>(), 0);
        var info = CollectionData.InfoOf(deck);
        var w = DeckInfoPopup.CreateForPicked(mgr, -1, deck, info, DeckInfoPopup.DeckInfoState.Edit, true, null, null);
        made.Add(w.gameObject);
        Check(w.TryOpen(null), "D0b 前提：`Deck info Popup` 开出来了");

        var front = w.CardbackFrontForTest;
        Check(front != null,
              "D1 ★ 施工单 #5：`Cardback Front` 这一层**建出来了**（原版 `Deck Info ▸ Cardback ▸ Cardback Front`）"
            + "｜🧨 只画父层（= 改前）⇒ 本条红");
        // 🔴 2026-11-03（`B124_W35`）：原来是 `if (front != null) { … }` —— 前提一破，**D2/D3 静默不跑**
        //   （看着只有 D1 一条红，其实两条从没执行过）。改成「先硬断前提、再无条件断结论」：
        //   取值一律走空安全三元（落 `float.NaN`）⇒ 前提破了结论那条照样红，且不抛异常把后面的组带崩。
        Check(front != null, "D2 前提 ★ `Cardback Front` 拿到手了（⛔ 不在 ⇒ D2/D3 必须红，不得静默跳过）");
        // 世界角 = 父 +1.60° + 子 −3.43° = −1.83°（Unity 的 `eulerAngles.z` 落在 [0,360) ⇒ 归一到 (−180,180]）
        float z = front != null ? front.localRotation.eulerAngles.z : float.NaN;
        if (z > 180f) z -= 360f;
        Check(Mathf.Abs(z - (-1.83f)) < 0.05f,
              "D2 ★ 子件的世界角 = 父 `+1.60°` + 子 `−3.43°` = **−1.83°**（判据 = `menu_dump` 的 `↻1.60°` / `↻-3.43°`）"
            + "｜实得 " + z.ToString("F2") + "°｜🧨 子件也设成 +1.60°（漏掉自己那一转）⇒ 本条红");

        // 位置：相对父层 `Cardback` 的中心偏 (+20.72, −9.07) 屏幕 px ⇒ 距离 22.62 px
        //   （原版父局部 (+11.2, −4.9) × localScale 1.85；`menu_dump` 的父/子绝对矩形中心差也是 22.62）
        //   ⚠️ 用「**两节点之间的距离**」而不是绝对坐标：窗根挂在哪由 `AttachToAnchor` 决定，
        //      绝对坐标会随宿主漂；距离是这套几何里唯一不随宿主变的量。
        var cb = w.transform.Find("Deck Info/Cardback");
        Check(cb != null, "D2b 前提：父层 `Cardback` 节点在（⛔ 不在 ⇒ D3 无从量起，不会假绿）");
        float d = (cb != null && front != null) ? Vector3.Distance(front.position, cb.position) : float.NaN;
        Check(Mathf.Abs(d - LayoutSpace.Px(22.62f)) < LayoutSpace.Px(2f),
              "D3 ★ 子件中心相对父件偏 **(+20.72, −9.07) 屏幕 px ⇒ 距离 22.62px**"
            + "（原版父局部 (+11.2, −4.9) × 1.85；`menu_dump` 的两个绝对矩形中心差同样是 22.62）"
            + "｜实得 " + (d / LayoutSpace.Px(1f)).ToString("F2") + "px（容差 2px）"
            + "｜🧨 把子件摆在父件正中（偏移 0）⇒ 本条红");
        w.Close();
    }

    // ==================================================================
    //  E · `Edit Deck` 按 `state` 换文案（施工单 #6）
    // ==================================================================
    static void GroupE_EditDeckLabelByState(List<GameObject> made)
    {
        var mgr = WindowsManager.Get();
        if (mgr == null) { Check(false, "E0 前提：拿得到 `WindowsManager`"); return; }

        Check(Loc.HasAnyTerm(DeckInfoPopup.TermImportDeckKey) && Loc.HasAnyTerm(DeckInfoPopup.TermEditDeckKey),
              "E0b 前提：两条键 `" + DeckInfoPopup.TermImportDeckKey + "` / `" + DeckInfoPopup.TermEditDeckKey + "` 都在表里");

        var deck = new PlayerDeck("C2 label probe", null, null, new List<string>(), 0);
        var info = CollectionData.InfoOf(deck);
        var w = DeckInfoPopup.CreateForPicked(mgr, -1, deck, info, DeckInfoPopup.DeckInfoState.Import, true, null, null);
        made.Add(w.gameObject);
        w.TryOpen(null);
        Check(w.EditDeckLabelTextForTest == Loc.T(DeckInfoPopup.TermImportDeckKey),
              "E1 ★ 施工单 #6：`state == Import` ⇒ 那颗钮印 **`MenuDeck/MenuButtons/ImportDeck`**"
            + "（原版 `DeckInfoControls.cs:84-95`）｜期望「" + Loc.T(DeckInfoPopup.TermImportDeckKey) + "」/ 实得「" + w.EditDeckLabelTextForTest + "」"
            + "｜🧨 停在 `EditDeck` 那条键（= 改前）⇒ 本条红");

        w.State = DeckInfoPopup.DeckInfoState.Edit;
        w.ApplyStateVisibility();          // 建完之后改 state ⇒ 必须重钉文案（不是只在 Build 里算一次）
        Check(w.EditDeckLabelTextForTest == Loc.T(DeckInfoPopup.TermEditDeckKey),
              "E2 ★ 改回 `state == Edit` ⇒ 文案**跟着回去**（`ApplyStateVisibility` 里那次重钉）"
            + "｜实得「" + w.EditDeckLabelTextForTest + "」｜🧨 只在 `Build()` 里算一次 ⇒ 本条红");
        w.Close();
    }

    // ==================================================================
    //  F · `Deck Selection Popup` 的空态文案（施工单 #7）
    // ==================================================================
    static void GroupF_DeckSelectionEmptyText()
    {
        const string expect = "There are no deck in your collection for the selected filters";
        Check(DeckSelectionPopup.EmptyWarningText == expect,
              "F1 ★ 施工单 #7：空态 = 原版**硬编码英文**「" + expect + "」（单数 `deck`、没有 `or game mode`）"
            + "｜实得「" + DeckSelectionPopup.EmptyWarningText + "」");
        Check(DeckSelectionPopup.EmptyWarningText != Loc.T("MenuCollection/NoDecksFound"),
              "F2 ★ 它**不等于**官方键 `MenuCollection/NoDecksFound` 那条（改前就是那条 ⇒ 多 `s` + 多 `or game mode`、还随语言变）");

        // 结构断言：源码里空态那条**不再**走官方键（⛔ 光比常量挡不住「把常量也改成官方键」）
        string p = Application.dataPath + "/CardPresentation/Shell/DeckSelectionPopup.cs";
        string src = File.Exists(p) ? File.ReadAllText(p) : "";
        int hits = CountOf(src, "_empty = Loc.T(\"MenuCollection/NoDecksFound\")");
        int hits2 = CountOf(src, "EmptyWarningText");
        Check(src.Length > 0 && hits == 0 && hits2 >= 2,
              "F3 ★ 源码结构：`_empty = Loc.T(\"MenuCollection/NoDecksFound\")` **0 处**、`EmptyWarningText` ≥ 2 处"
            + "（定义 + 一处消费）｜实得 " + hits + " / " + hits2 + "｜🧨 把 `_empty` 改回官方键 ⇒ 本条红");
    }

    static int CountOf(string s, string sub)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(sub)) return 0;
        int n = 0, i = 0;
        while ((i = s.IndexOf(sub, i, System.StringComparison.Ordinal)) >= 0) { n++; i += sub.Length; }
        return n;
    }
}
