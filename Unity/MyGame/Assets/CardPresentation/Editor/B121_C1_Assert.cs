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
//   COLL-08 卡面灰度（未拥有 / 禁卡 ⇒ 全灰）· COLL-09a `Import` 的 114 闸 ——
//     🆕 **2026-11-04（`B140_WG`）纯离线夹具**（空 `CollectionWindow` 壳 + 临时卡组库，不建真窗、不动玩家真档）
//   （只剩 COLL-03 一条「未验」：真机 2026-11-03 已过、离线档本类仍验不了。COLL-02 早已是
//     `Editor/CollectionScene.cs:2235-2244` 的离线断言 ⇒ 原来那条「未验」是过期文案，已删 —— 见 §B）
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
        GroupCardGrey();          // 🆕 2026-11-04（B140_WG）：COLL-08 —— 纯离线（空窗壳 + 反射调真实现）
        GroupDeckImportGate();    // 🆕 2026-11-04（B140_WG）：COLL-09a —— 纯离线（临时卡组库 113 / 114 两档）

        // ---------------- B 组：真机 / 建真窗才验得了的（本宿主只报「未验」，⛔ 不伪造） ----------------
        // 🔴 **2026-11-04（`B140_WG`）就地订正（铁律 5）**：本组原来挂了 **4** 条 `MarkUnverified`，其中 **3 条已过期**：
        //   · **COLL-02**（点格不改「当前卡组」）**早已是离线断言** —— `Editor/CollectionScene.cs:2235-2244`
        //     （`int before = CollectionData.CurrentIndex(); … Check(CurrentIndex(), before, …)`，随 `CollectionScene.Run`
        //     跑、不读盘）⇒ 本条「未验」已删，⛔ 别再写回来。
        //   · **COLL-08 / COLL-09a** 自 2026-11-04 起走上面 A 组那两条**纯离线夹具** ⇒ 也已删。
        MarkUnverified("COLL-03 Deck 页 `Clear filters` 显隐：**真机 2026-11-03 已过**"
                     + "（`_tmp_view/shots/20261103_s123/29_faction_pick.png`：选阵营后页头出现 chip + 清除过滤钮）"
                     + "· 本类不建真窗 ⇒ 离线档**仍验不了**（不是待办，只是真机证据，所以仍单独计数、⛔ 不算通过）");

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
        CheckNear(firstFacY, 65f + FilterPanelModel.ArmyContentTop, 0.01f,
                  "COLL-06 收藏窗卡背抽屉：`Army` 行顶 = 65 + `ArmyContentTop`(50) = **115**（行顶 65 = 15 + Owned 50；"
                + "`BuildCosmetics:1136` 写的是 `y = armyTop + ArmyContentTop` ⇒ 实测值恒 = 行顶 + 50）");
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
        CheckNear(firstFacY2, 15f + FilterPanelModel.ArmyContentTop, 0.01f,
                  "COLL-06 卡组编辑窗卡背抽屉：`Army` 行顶 = 15 + `ArmyContentTop`(50) = **65**（`armyTop` 15；"
                + "同一条 `y = armyTop + ArmyContentTop` 的偏移）");
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

    // ============================================================ 🆕 2026-11-04（`B140_WG`）· COLL-08 / COLL-09a 的**纯离线**夹具
    //   本节**追加在类尾**（本文件被 `判据/*` 按行号引用过 ⇒ 新成员别插进既有组之间）。
    //   两条都**不建真窗、不读玩家存档**：用一个空 `CollectionWindow` 壳 + 反射直调真实现，
    //   判据值全部走 `Shell/CollectionWindow.cs` 里那两段（⛔ 本文件不写第二份判据 —— 那是自证）。

    /// <summary>COLL-08：卡池格的**整卡灰度** = `grey = !owned || isBanned`（未拥有/禁卡 ⇒ `SetGrey(1, 0f)` 瞬时）。
    /// <para>判据 = 原版 `CardCollectionDisplay__SetCell.c` 末段（逐字；实现见 `Shell/CollectionWindow.cs:7011-7023`）。
    /// 本组走**真实现**（`CollectionWindow.ApplyCardGrey`，反射调私有方法）：它只读两个静态判据 + `CardView.SetGrey`，
    /// 不碰窗体状态 ⇒ 一个 `AddComponent` 出来的**空窗壳**就够。</para>
    /// <para>数据怎么造（不碰玩家真档）：未拥有 = 背包里给这张卡塞一条**数量 0** 的条目（`CardOwned` 首查背包）；
    /// 禁卡 = 临时把 `DeckRuntime.CurrentEventData` 赋成只带一条禁卡的 `PlayEventData`（活动线同一个写入口），`finally` 还原。</para></summary>
    static void GroupCardGrey()
    {
        var mi = typeof(CollectionWindow).GetMethod("ApplyCardGrey",
                     System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Check(mi != null, "COLL-08 前提：`CollectionWindow.ApplyCardGrey(CardView, CardDef)` 存在（判据落点没被改签名）");
        if (mi == null) return;

        var host = new GameObject("B121_C1_GreyHost");
        var win = host.AddComponent<CollectionWindow>();
        var ownedDef = new RuleEngine.CardDef("B121C1GreyOwned", "B121C1GreyOwned", "unit", "", "common", "Test", 1, 1, 1, 0, null);
        var unownedDef = new RuleEngine.CardDef("B121C1GreyUnowned", "B121C1GreyUnowned", "unit", "", "common", "Test", 1, 1, 1, 0, null);
        var bannedDef = new RuleEngine.CardDef("B121C1GreyBanned", "B121C1GreyBanned", "unit", "", "common", "Test", 1, 1, 1, 0, null);
        var cards = Inventories.Cards;
        var oldEvt = DeckRuntime.CurrentEventData;
        var pre = cards.Get(unownedDef);
        CardView v1 = null, v2 = null, v3 = null;
        try
        {
            // 前提①：未拥有那一档**必须**在背包里有一条数量 0 的条目 —— 否则 `CardOwned` 会回退到
            //   `CardProgress.Owned` 的「给足」口径（恒 > 0）⇒ 这一档根本造不出来（假绿来源）。
            cards.Create(unownedDef);
            Check(cards.Get(unownedDef) != null && cards.Get(unownedDef).Amount == 0,
                  "COLL-08 前提：`unownedDef` 在背包里是**数量 0** 的条目（`CardOwned` 走背包、答 false）");
            Check(cards.Get(ownedDef) == null, "COLL-08 前提：`ownedDef` 不在背包 ⇒ 走「给足」口径（= 已拥有）");

            v1 = ProbeCard("B121_C1_GreyOwned");
            v2 = ProbeCard("B121_C1_GreyUnowned");
            v3 = ProbeCard("B121_C1_GreyBanned");
            v1.SetGrey(1f, 0f);   // ⚠️ 先**人为全灰**：已拥有那一条要证明 `ApplyCardGrey` **主动把灰清回 0**，而不是「本来就是 0」的假绿

            mi.Invoke(win, new object[] { v1, ownedDef });
            CheckNear(v1.Grey, 0f, 1e-4f, "COLL-08 已拥有 ⇒ 卡面**不灰**（原版 `grey = !owned || isBanned`）");
            mi.Invoke(win, new object[] { v2, unownedDef });
            CheckNear(v2.Grey, 1f, 1e-4f,
                      "COLL-08 **未拥有 ⇒ 整卡全灰**（`CardCollectionDisplay__SetCell.c` 末段；"
                    + "🧨 把 `ApplyCardGrey` 的判据改反 ⇒ 本条红）");

            DeckRuntime.CurrentEventData = Event(bannedDef.Id);
            mi.Invoke(win, new object[] { v3, bannedDef });
            CheckNear(v3.Grey, 1f, 1e-4f, "COLL-08 **禁卡 ⇒ 整卡全灰**（`PlayEventData.IsCardBanned` 灌进一张）");
            DeckRuntime.CurrentEventData = null;
            mi.Invoke(win, new object[] { v3, bannedDef });
            CheckNear(v3.Grey, 0f, 1e-4f,
                      "COLL-08 灭自证：摘掉禁卡事件后**同一张卡回原色**"
                    + "（🧨 写成恒灰 / 恒不灰 ⇒ 与本组前三条只能对一半）");
        }
        catch (System.Exception e)
        {
            Check(false, "COLL-08 夹具**抛了**（" + e.GetType().Name + "：" + e.Message
                       + "）⇒ 本组结论不成立（⛔ 不许当通过）");
        }
        finally
        {
            DeckRuntime.CurrentEventData = oldEvt;
            var mine = cards.Get(unownedDef);
            if (mine != null) mine.SetAmount(pre != null ? pre.Amount : 0);   // 还原那一格（原来没有就回 0）
            DestroyCard(v1); DestroyCard(v2); DestroyCard(v3);
            Object.DestroyImmediate(host);
        }
    }

    /// <summary>COLL-09a：`Import` 钮的可点闸 = **`卡组数 &lt; 114`**。
    /// <para>判据 = 原版 `importDeck.interactable = InventoryManager.GetOwnedCount&lt;CardDeck&gt;() &lt; GameStaticData.totalCustomDecks`
    /// （`SelectDecksTab__Setup.c:44-53`；`totalCustomDecks = 114`，`GameStaticData.cs:2419`）。实现 → `Shell/CollectionWindow.cs:7040-7046`。</para>
    /// <para>🔴 卡组库是**临时沙箱**造的（`DeckStore.OverridePath` + 三个钩子临时置空 ⇒ 玩家真档一个字节都不碰，
    /// 跑完 `finally` 全还原）；造完直接塞回 `CollectionData._lib`，让 `DeckCount()` 读到同一份 —— ⛔ 不复制判据。</para></summary>
    static void GroupDeckImportGate()
    {
        var mi = typeof(CollectionWindow).GetMethod("RefreshDeckImportGate",
                     System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var fi = typeof(CollectionWindow).GetField("_deckImportHit",
                     System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var lf = typeof(CollectionData).GetField("_lib",
                     System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Check(mi != null && fi != null && lf != null,
              "COLL-09a 前提：`RefreshDeckImportGate` / `_deckImportHit` / `CollectionData._lib` 都在（判据落点没被改名）");
        if (mi == null || fi == null || lf == null) return;

        var host = new GameObject("B121_C1_ImportGateHost");
        var win = host.AddComponent<CollectionWindow>();
        var hitGo = new GameObject("B121_C1_ImportHit");
        var wb = hitGo.AddComponent<WindowButton>();
        object keepHit = fi.GetValue(win);
        object keepLib = lf.GetValue(null);

        string oldOver = RuleEngine.DeckStore.OverridePath;
        var oldRead = RuleEngine.DeckLibrary.ReadLegacyText;
        var oldSave = RuleEngine.DeckLibrary.SaveLegacyText;
        var oldExists = RuleEngine.DeckLibrary.CentralSaveExists;
        string tmpDir = System.IO.Path.Combine(Application.temporaryCachePath, "B121_C1_coll19");
        bool sandboxed = false;
        try
        {
            System.IO.Directory.CreateDirectory(tmpDir);
            RuleEngine.DeckStore.OverridePath = System.IO.Path.Combine(tmpDir, "B121_C1_decks.json");
            RuleEngine.DeckLibrary.ReadLegacyText = null;      // ⛔ 别让 Shell 的中央档钩子把临时库遮掉
            RuleEngine.DeckLibrary.SaveLegacyText = null;      // ⛔ 落盘只准进临时文件（玩家真档一个字节都不碰）
            RuleEngine.DeckLibrary.CentralSaveExists = null;
            sandboxed = true;

#pragma warning disable 0618   // `DeckLibrary.Create` 标了 `[Obsolete]`（自陈「本口只服务自检夹具」）—— 本处正是那个用途
            var lib = RuleEngine.DeckLibrary.Load();
            // ⚠️ 循环带**上限**：万一 `Create` 哪天不再入库，这里必须**停下来判红**，⛔ 不能把宿主挂死。
            int guard = 0;
            while (lib.Decks.Count < 113 && guard++ < 200) lib.Create("B121C1Deck" + lib.Decks.Count);
            Check(lib.Decks.Count == 113, "COLL-09a 前提：夹具造出 **113** 套卡组（实得 " + lib.Decks.Count + "）");

            lf.SetValue(null, lib);
            Check(CollectionData.DeckCount() == 113,
                  "COLL-09a 前提：`CollectionData.DeckCount()` 读到 113（实得 " + CollectionData.DeckCount() + "）");

            fi.SetValue(win, hitGo.transform);
            mi.Invoke(win, null);
            Check(wb.Interactable, "COLL-09a **113 < 114 ⇒ `Import` 可点**（原版 `interactable = 卡组数 < 114`）");

            lib.Create("B121C1Deck113");
#pragma warning restore 0618
            Check(lib.Decks.Count == 114, "COLL-09a 前提：第 **114** 套造出来了（实得 " + lib.Decks.Count + "）");
            mi.Invoke(win, null);
            Check(!wb.Interactable,
                  "COLL-09a **114 不 < 114 ⇒ `Import` 灰掉**（🧨 把 `114` 改成 `113` ⇒ 上一条 + 本条必红）");

            lib.Delete(0);
            mi.Invoke(win, null);
            Check(wb.Interactable,
                  "COLL-09a 灭自证：删回 **113** 套后 `Import` **又亮了**（🧨 写成恒假 / 只判一次 ⇒ 本条红）");
        }
        catch (System.Exception e)
        {
            Check(false, "COLL-09a 夹具**抛了**（" + e.GetType().Name + "：" + e.Message
                       + "）⇒ 本组结论不成立（⛔ 不许当通过）");
        }
        finally
        {
            if (sandboxed)
            {
                lf.SetValue(null, keepLib);
                RuleEngine.DeckLibrary.ReadLegacyText = oldRead;
                RuleEngine.DeckLibrary.SaveLegacyText = oldSave;
                RuleEngine.DeckLibrary.CentralSaveExists = oldExists;
                RuleEngine.DeckStore.OverridePath = oldOver;
                try { if (System.IO.Directory.Exists(tmpDir)) System.IO.Directory.Delete(tmpDir, true); } catch { }
            }
            fi.SetValue(win, keepHit);
            Object.DestroyImmediate(hitGo);
            Object.DestroyImmediate(host);
        }
    }

    /// <summary>`CardView` 探针（不建卡面层 —— 本组只读 `Grey` 这一格）。</summary>
    static CardView ProbeCard(string name)
    {
        var go = new GameObject(name);
        return go.AddComponent<CardView>();
    }

    static void DestroyCard(CardView v) { if (v != null) Object.DestroyImmediate(v.gameObject); }

    /// <summary>造一个只带一条禁卡的 `PlayEventData`（走活动线同一个写入口 `SetBannedCards`）。</summary>
    static PlayEventData Event(string bannedId)
    {
        var evt = new TestEvent();
        evt.SetBannedCards(string.IsNullOrEmpty(bannedId) ? null : new List<string> { bannedId });
        return evt;
    }

    /// <summary>禁卡表夹具（`PlayEventData` 是 abstract；`SetBannedCards` 是 Shell 侧的扩展）。</summary>
    sealed class TestEvent : PlayEventData { }
}
