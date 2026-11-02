// B121_C1_Assert.cs — 执行代理 `B121-C1` 的断言宿主（收藏窗审计的可做项）
//
// 活：把「收藏界面审计」里 8 条可做项逐个钉住（审计正本 → `资料/完全复刻/报告/B121_COLL_收藏界面审计.md`）。
// 接线由**调度台**做（⛔ 本文件不改 `Editor/*Scene.cs`）：宿主里调 `B121_C1_Assert.Run()` 即可。
//
// 跑法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B121_C1_Assert.Run -logFile -
//
// 判绿红：按**行首** `B121C1 ✓` / `B121C1 ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在文案里）；
//   「未验」单独计数、**不算通过**（⛔ 不静默）。
//
// 覆盖的 8 条（编号 = 审计 §问题清单）：
//   COLL-04 `ClearFilterX`：Cardback 页 1488.59、其余三页 612.2（判据 = prefab 父 `Filters.sizeDelta.x`）
//   COLL-06 卡背抽屉行序：收藏窗 = Owned(15) 在前、Army(65) 在后（判据 = `menu_dump … "Cardback Tab"`）
//   COLL-07 chip 上限 = 5（判据 = `FillToggleList(toggleOptions, 5)` + `if (5 < count) break`）
//   COLL-01 `PointerLayer.InsertText`：允许 `\n`、丢 `\r`/其它控制字符、按上限截断
//   COLL-09 模式页进入条件（离线等价表 2 项 ⇒ 进模式页）
//   （COLL-02/03/08 需要真实窗口 + 存档，见 §B：本宿主只报「未验」，不伪造）
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;

public static class B121_C1_Assert
{
    static int _pass, _fail, _unverified;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
    public static int Unverified { get { return _unverified; } }

    const string Tag = "B121C1";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void CheckNear(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F4") + " ± " + tol.ToString("F4") + "；实得 " + got.ToString("F4") + "）");
    }

    static void MarkUnverified(string msg) { _unverified++; Debug.Log(Tag + " … 未验：" + msg); }

    public static void Run()
    {
        _pass = _fail = _unverified = 0;

        // ---------------- A 组：纯函数 / 纯几何（随时可跑） ----------------
        GroupClearFilterX();
        GroupCosmoOrder();
        GroupChipLimit();
        GroupInsertText();
        GroupModePage();

        // ---------------- B 组：需要真窗口（本宿主不建窗） ----------------
        MarkUnverified("COLL-02 点格不再改「当前卡组」：需要 `CollectionWindow` + 真实存档（宿主里点 `SelectDeck(i)` 后比 "
                     + "`CollectionData.CurrentIndex()` 未变）");
        MarkUnverified("COLL-03 Deck 页 `Clear filters` 显隐：需要 `CollectionWindow` 实例（选一个阵营后 `_hdrClearQ[0].gameObject.activeSelf` 为真）");
        MarkUnverified("COLL-08 卡面灰度：需要 `CardView` + 一张未拥有/被禁的卡（本机全解锁 + 无 LiveOps ⇒ 常态不可达）");
        MarkUnverified("COLL-09 `Import` 可点闸：需要 `CollectionWindow`（卡组数 ≥114 时才翻假，本机到不了）");

        Debug.Log(Tag + " ==== 合计：通过 " + _pass + " · 失败 " + _fail + " · 未验 " + _unverified + " ====");
    }

    // ---------------- COLL-04 ----------------
    static void GroupClearFilterX()
    {
        // 判据：四颗按钮自己的序列化值相同（pos(20,0)·anchor(1,.5)·pivot(0,.5)·250×60）；
        //   父 `Filters.sizeDelta.x`：Deck/Cards/AltArt = −876.415（宽 0 ⇒ 592.2+20），Cardback = 0（宽 876.415 ⇒ 1468.6+20）
        CheckNear(CollectionWindow.ClearFilterX(2), 1488.59f, 0.02f, "COLL-04 Cardback 页 `Clear filters` 左缘 = 1488.59");
        CheckNear(CollectionWindow.ClearFilterX(0), 612.2f, 0.02f, "COLL-04 Deck 页 = 612.2");
        CheckNear(CollectionWindow.ClearFilterX(1), 612.2f, 0.02f, "COLL-04 Cards 页 = 612.2");
        CheckNear(CollectionWindow.ClearFilterX(3), 612.2f, 0.02f, "COLL-04 AltArt 页 = 612.2");
        // 灭自证：卡背页与另三页**必须不同**（若哪天被人「收口」成同一个常量，这条红）
        Check(Mathf.Abs(CollectionWindow.ClearFilterX(2) - CollectionWindow.ClearFilterX(1)) > 800f,
              "COLL-04 灭自证：Cardback 与 Cards 的清除钮**不是同一个 x**（差 > 800）");
    }

    // ---------------- COLL-06 ----------------
    static void GroupCosmoOrder()
    {
        var facs = new List<string>();
        for (int i = 0; i < 13; i++) facs.Add("F" + i);          // 只用来数行数/生成 key，图标取不到不影响几何
        var f = new DeckFilter();
        var cells = new List<FilterPanelModel.Cell>();

        FilterPanelModel.BuildCosmetics(facs, f, 335.5f, cells, ownedFirst: true);
        float ownedY = -1f, firstFacY = -1f;
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].Key == "$owned") ownedY = cells[i].R.y1;
            else if (firstFacY < 0f && cells[i].Key != null && cells[i].Key.StartsWith("$fac:")) firstFacY = cells[i].R.y1;
        }
        CheckNear(ownedY, 15f, 0.01f, "COLL-06 收藏窗卡背抽屉：`Owned` 行顶 = 15（原版 `Spacing` 15 之后第一行）");
        CheckNear(firstFacY, 65f, 0.01f, "COLL-06 收藏窗卡背抽屉：`Army` 行顶 = 65（= 15 + Owned 50）");
        Check(ownedY >= 0f && firstFacY > ownedY, "COLL-06 灭自证：Owned **在 Army 之前**（原版孩子序）");

        // 卡组编辑窗那一档（`ownedFirst:false`）方向必须相反 —— 两扇窗的行序是**两套参数**，别被收口
        var cells2 = new List<FilterPanelModel.Cell>();
        FilterPanelModel.BuildCosmetics(facs, f, 335.5f, cells2, armySpY: 20f, iconW: 80f, labW: 230f);
        float ownedY2 = -1f, firstFacY2 = -1f;
        for (int i = 0; i < cells2.Count; i++)
        {
            if (cells2[i].Key == "$owned") ownedY2 = cells2[i].R.y1;
            else if (firstFacY2 < 0f && cells2[i].Key != null && cells2[i].Key.StartsWith("$fac:")) firstFacY2 = cells2[i].R.y1;
        }
        CheckNear(firstFacY2, 15f, 0.01f, "COLL-06 卡组编辑窗卡背抽屉：`Army` 行顶 = 15（原版 Army 在前）");
        Check(ownedY2 > firstFacY2, "COLL-06 灭自证：卡组编辑窗 **Army 在 Owned 之前**（与收藏窗相反）");
    }

    // ---------------- COLL-07 ----------------
    static void GroupChipLimit()
    {
        // 判据：`FillToggleList(toggleOptions, 5)`（`…NotifyFilterChange__15f01a0.c:110`）
        //   + `if (5 < toggleOptions.Count) break`（同文件 `:141`）
        // 🔴 **2026-11-03（`B121-COLL-21` · B121-COLLFIX 改写）**：本组原来**直测 `AppendChips`**（行内 chip）。
        //   原版 `FillToggleList` 在行内建完副本后**立刻 `SetParent(filterIconHolder)` 搬进页头**（`.c:154`）
        //   ⇒ 行内一颗都没有，上限那条判据的**生产落点**现在是 `BuildHeaderChips`（页头 85×70 网格）。
        //   ⇒ 本组改测页头那一份：10 项全选 ⇒ 只出 5 颗、且被截掉的是**排在后面**的那几项。
        var facs = new List<string>();
        for (int i = 0; i < 10; i++) facs.Add("F" + i);
        var f = DeckFilter.None;
        f.Factions = new HashSet<string>(facs);
        var cells = new List<FilterPanelModel.Cell>();
        FilterPanelModel.BuildHeaderChips(facs, f, cells);
        Check(cells.Count == 5, "COLL-07 页头 chip 上限 = 5（10 项全选也只画 5 颗；实得 " + cells.Count + "）");
        Check(cells.Count == 5 && cells[4].Key == "$chip:$fac:F4",
              "COLL-07 灭自证：被截掉的是**排最后**的那几项（第 5 颗 = 第 5 个选项，顺序没被打乱）");
        Check(cells.Count > 0 && cells[0].Key == "$chip:$fac:F0", "COLL-07 第一颗 = 选项表第一项（顺序照 `optionsFinal`）");
    }

    // ---------------- COLL-01 ----------------
    static void GroupInsertText()
    {
        var go = new GameObject("B121_C1_TextProbe");
        var pl = go.AddComponent<PointerLayer>();
        try
        {
            string got = null;
            pl.BeginText("", 20, s => { got = s; }, null, s => { got = s; });
            pl.InsertText("ab\r\ncd");
            Check(pl.TextBuffer == "ab\ncd", "COLL-01 粘贴：`\\r\\n` 归一成 `\\n`（**换行保留**）· 实得 `" + pl.TextBuffer.Replace("\n", "\\n") + "`");
            pl.InsertText("\u0001e");            // 其它控制字符照旧丢
            Check(pl.TextBuffer == "ab\ncde", "COLL-01 粘贴：其它控制字符被丢弃");
            pl.EndText(true);
            Check(got == "ab\ncde", "COLL-01 提交拿到的缓冲与插入内容一致");

            pl.BeginText("", 4, null);
            pl.InsertText("0123456789");
            Check(pl.TextBuffer == "0123", "COLL-01 粘贴按上限截断（maxLen=4，实得 `" + pl.TextBuffer + "`）");
            pl.EndText(false);
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ---------------- COLL-09 ----------------
    static void GroupModePage()
    {
        // 原版 `ChooseTabToOpen`：事件数 > 1 ⇒ 先进模式页；≤ 1 ⇒ 直接进列表。
        // 本机零 LiveOps ⇒ 用离线等价表 `DeckModeCards`（2 项）当那一步的「事件列表」。
        Check(CollectionWindow.ShouldOpenDeckModePage(), "COLL-09 离线等价表 2 项 ⇒ >1 ⇒ 进模式页");
        Check(CollectionWindow.DeckModeCards.Length == 2, "COLL-09 灭自证：离线等价表就是 2 项（改表要同步改这条）");
    }
}
