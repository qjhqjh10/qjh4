// B141_W32_Assert.cs — 执行写手 `B141-W32`（第一百二十八会话 · 波次 `W129` · 条目 3-13 / 3-28 / 3-10 / 4-07 / 4-15）
//
// ============================ 它盯的是哪 5 条 ============================
//  判据 = `资料/完全复刻/判据/B141_W129_对账_卡组色彩.md` 的 **P4 包**（3-10 / 3-13 / 3-28）·
//         `资料/完全复刻/判据/B141_W129_对账_UI动画.md` 的 **W6 包**（4-07 / 4-15(3961)）·
//         审计 = `资料/完全复刻/报告/B138_审计汇总_问题清单.md` §3 / §4。
//
//  ── 3-13 新建卡组默认名（原版名字 = **空串**，首次 Done 补督军卡名）──
//    原版：`d:/2/tools/decomp_full/CardDeck__GetEmptyDeck.c:10-27` —— 只赋 `+0x20/+0x28/+0x30`
//      （prebuiltDeckId / cardbackId / warcryId）+ `DeckBasicSetup(卡组名 = 空串)` + `+0x40/+0x48/+0x50 = 0`，
//      **`deckName`(`+0x10`) 从头到尾没被赋值**（`CardDeck` 的字段偏移 → `d:/2/tools/il2cpp_out/dump.cs` 的 `CardDeck` 类）。
//    名字的补写 = `DeckUtility__ValidateDeck.c:70-76`：`IsNullOrWhiteSpace(deckName)` ⇒
//      `deckName = RawCardScript_GetLocalizedCardName(deckHero)`（**督军那张卡的本地化卡名**），且**只在校验全过之后**。
//    我们：`Shell/CollectionData.cs` 的 `CreateDeck` 现在 `NewUnsaved(...)` 之后**把名字清回空串**
//      （`NewUnsaved` 会把空名替成「新卡组」），补名的落地 = 既有的 `DeckEditorState.Validate → FillNameFromWarlord`。
//
//  ── 3-28 拖到视口外（**判据订正**：原版也不裁）──
//    `DragAndDropItem__OnBeginDrag.c:33-36`：本体 `set_parentInternal(本体, transform.**root**)`（跳出整条父链，
//    含 ScrollRect 的 `RectMask2D`）；`__OnDrag.c:37` 每帧 `set_position(鼠标)`、**无 clamp**
//    ⇒ **原版拖起来那一格【同样不被视口裁】**；被裁的是留在 content 里的 ghost（绝对摆位网格不需要它）。
//    ⇒ 本条**不改代码**，只钉「我们没 reparent / 没重裁」这一等价形态（见 §E）。
//
//  ── 3-10 方向门（30°~150° 竖向拖 = 滚列表）──
//    `DragAndDropItem__OnBeginDrag.c:32-38`：`|atan2(dy,dx)|·Rad2Deg`（`Rad2Deg = 57.29578` @`0x1834b2e98`）→
//    `& 0x7FFFFFFF`（绝对值）→ **`if (ang < 30 || 150 < ang)` 才拖格**（`30` @`0x1834b3168` · `150` @`0x1834b2d90`，
//    两值用 `工具/read_literal.py` 现读）⇒ **30 ≤ ang ≤ 150 转交 `ScrollRect.OnBeginDrag`**（`:38`，虚槽 `0x3f8`）。
//    落地 = `Shell/PointerLayer.cs` 的 `IsScrollDragAngle` + 两条输入路（`Update` / `MoveTo`）各一道门，
//      并且**只对 `ScrollGate` 件生效**（= 原版只有 `Initialize(父 ScrollRect)` 过的 `DragAndDropItem` 才有这道门；
//      收藏卡组格在 `AttachDeckDrag` 里 `RegisterDrag(..., true)` 打开它，别的可拖件缺省 false ⇒ 行为不变）。
//      ⚠️ 转交目标用 `HitScroll(按下点)` 现取（= 那颗件自己的 `scrollRect`），**不在按下那一刻就抢滚动区**
//      （`DraggingScroll` 在未拖起时恒 null，`Editor/ShellScene.cs` ⑤ 那条「可拖件优先」断言因此不受影响）。
//
//  ── 4-07 收藏 Deck 页搜索框字号 ──
//    原版 `…/Select Deck Tab/Deck Filters/Filters/Deck Name Filter/Input Field/Text Area/Text`
//      = `m_fontSize 35` · auto[10~35] · base 26（判据 → B141_W129_对账_UI动画.md W6 行 + B131 §UI 二 #27）。
//    我们：`Shell/CollectionWindow.cs` 那颗 `"Input Text"` 由 `30f` 改成 `35f`（auto 三格本来就是 10/35/26）。
//
//  ── 4-15 收藏窗关闭钮命中区 ──
//    原版 `Collection Menu Variant.prefab` 的 `Close Button` Image `m_RaycastPadding = (-10,-10,-10,-10)`
//      （负 = **外扩**；符号约定 → `MenuDraw.PaddedRect`）⇒ 命中区 150×60 → **170×80**。
//
// ============================ 🔴 灭自证（🧨 每条怎么才能红）============================
//  · A2：把 `CreateDeck` 里那句 `if (d != null) d.Name = "";` 删掉 ⇒ `d.Name` 回到「新卡组」⇒ **红**
//        （A1 仍会绿 —— 它只查对象在不在，故意与 A2 不共因）。
//  · A6：把 `DeckEditorState.Validate` 里的 `FillNameFromWarlord()` 删掉（或改成填**第一张卡**的名字）⇒ **红**；
//        A7 是**结构性判别式**（督军名 ≠ 某张部队卡名）—— 与 A6 不共因：只看「填了没」拦不住「填错卡」。
//  · B3：把字号改回 `30f` ⇒ **红**（B1 拿**字面量** `35f` 比，⛔ 不从被测实现读回来）。
//  · C3：把 `ClosePad` 改成 `(0,0,0,0)` / 正数 ⇒ **红**（C2 拿 `MenuDraw.PaddedHitRect` 的**纯函数**结果比字面量
//        `(-10,-10,160,70)`，与源码扫描 C1 不共因）。
//  · D1–D9：把方向门删掉 / 把带子写成 60~120（= 两条窄带搞混）⇒ 成组红。
//  · D11/D12：只留一条输入路（生产路或自检路缺一道门）⇒ 红。
//  · D13/D14：把 `ScrollGate` 那道闸去掉 ⇒ 别的可拖件被劫持 ⇒ 红；`AttachDeckDrag` 漏传 `true` ⇒ D14 红。
//  · E1/E2：给拖起那一格加 `SetParent` / 改成世界位移动 ⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主建议 = `CardPresentation/Editor/CollectionScene.cs`（本批窗口的自检宿主）。
//  在该方法**合计行同一个物理行**、且**落在该行第一个 `//` 之前**追加（行数零漂移）：
//      B141W32_Assert.Run(); pass += B141W32_Assert.Pass; fail += B141W32_Assert.Fail;
//  （计数器变量名以宿主当时那一对为准。⚠️ 追加必须落在第一个 `//` 之前 —— 本仓已 6 次踩
//   「代码落进行尾注释 ⇒ 编译过、账绿、断言一条没跑」，见 `CLAUDE.md` §三。）
//  ⚠️ 本件**零前提**：不建 GameObject、不切场景、不写盘。`CreateDeck` 自 A1697 起**不入库不落盘**
//     ⇒ §A 只动内存，且**结束时把 `PendingNewDeck` / `PendingEditDeck` 原样还回去**（⛔ 别让后面的断言吃到它们）。
//
// ============================ 判据 / 报告 ============================
//  · 报告：`资料/完全复刻/报告/B141_W129_Collection.md`
using System;
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行写手 `B141-W32`：3-13 / 3-28 / 3-10 / 4-07 / 4-15 的落地断言（零前提，可在任意宿主聚合行里跑）。</summary>
public static class B141W32_Assert
{
    const string Tag = "B141W32";
    static int _pass, _fail;

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _fail; } }

    static void C(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    const string SrcCollectionData = "Assets/CardPresentation/Shell/CollectionData.cs";
    const string SrcCollectionWindow = "Assets/CardPresentation/Shell/CollectionWindow.cs";
    const string SrcPointerLayer = "Assets/CardPresentation/Shell/PointerLayer.cs";

    /// <summary>唯一的入口（宿主聚合行里调）。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        A_NewDeckUnnamed_FillsFromWarlord();
        B_SearchBoxFontSize();
        C_CloseHitPadding();
        D_ScrollDragDirectionGate();
        E_DragCellNotReparentedNorReclipped();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail);
    }

    // ==================================================================
    //  A · 3-13：新建卡组名 = 空串；合法 + 空名 ⇒ Validate 补成**督军卡名**
    // ==================================================================
    static void A_NewDeckUnnamed_FillsFromWarlord()
    {
        var keepNew = CollectionData.PendingNewDeck;
        var keepIdx = CollectionData.PendingEditDeck;
        try
        {
            // 13 = `RuleEngine.GameMode.Skirmish`（12 张；⛔ 别用 30 张的经典，夹具只要 12 张就够）
            string nm = CollectionData.CreateDeck(13);
            PlayerDeck d = CollectionData.PendingNewDeck;   // 🔴 本口的返回值现在照原版是**空串** ⇒ 对象从这里取（判成败也读它）
            C(d != null && ReferenceEquals(CollectionData.PendingNewDeck, d),
              "A1 ★ 3-13：`CreateDeck` 交出了 `PendingNewDeck`（判「造没造出来」读**对象**，⛔ 不读名字）"
            + "（实得返回值「" + nm + "」= 空串即原版行为）");
            C(d != null && d.Name == "",
              "A2 ★ 3-13：新建卡组名 = **空串**（原版 `CardDeck__GetEmptyDeck.c:10-27` 不给 `deckName`(+0x10) 赋值）"
            + "｜🧨 删掉 `CollectionData.CreateDeck` 里那句 `d.Name = \"\"` ⇒ 回到「新卡组」⇒ 红");
            if (d == null) { C(false, "A3 （前提）没造出卡组 ⇒ 本节后面的都跑不了"); return; }
            C(string.IsNullOrEmpty(d.WarlordId), "A3 ……而且是一副**空**卡组（督军未选：`+0x40 = 0`）");

            // ---- 补成合法遭遇卡组：督军 + 12 张同阵营部队（同 `Editor/MainMenuScene.cs` 的 `FillLegalSkirmishDeck`）----
            //   ⚠️ **先挑一个「凑得齐 12 张」的督军**（否则夹具会以 `TooFewCards` 红成假警报）。
            var pool = CardDatabase.Load();
            int want = DeckRules.CardCount(true);            // 遭遇模式张数（唯一出处）
            CardDef hero = null, other = null;
            var picks = new List<CardDef>();
            foreach (var h in pool)
            {
                if (h == null || h.Type != "hero" || string.IsNullOrEmpty(h.Faction)) continue;
                picks.Clear();
                var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                foreach (var u in pool)
                {
                    if (picks.Count >= want) break;
                    if (u == null || u.Type != "unit" || string.IsNullOrEmpty(u.Id)) continue;
                    if (!DeckRules.SameFaction(u.Faction, h.Faction)) continue;
                    if (!seen.Add(u.Id)) continue;
                    if (DeckRules.IsLegendary(u.Rarity)) continue;
                    if (DeckRules.IsNotDeckable(u)) continue;
                    if (DeckRules.CannotAddToDeck(u)) continue;
                    picks.Add(u);
                }
                if (picks.Count >= want) { hero = h; break; }
            }
            C(hero != null && picks.Count == want,
              "A4 （前提）卡池里取到一个能被凑满 " + want + " 张合法部队的督军（null / 张数不足 ⇒ 后面两条全是空转 ⇒ 当场红）");
            if (hero == null) return;
            foreach (var c in pool)
                if (other == null && c != null && c.Type == "unit" && !DeckRules.SameFaction(c.Faction, hero.Faction))
                    other = c;
            d.WarlordId = hero.Id;
            d.CardIds.Clear();
            foreach (var p in picks) d.CardIds.Add(p.Id);

            var st = new DeckEditorState(pool);
            st.LoadDeck(d);
            C(st.Deck != null && st.Deck.Name == "",
              "A5 （前提）装进编辑器时名字**仍是空串** —— `PlayerDeck` 构造器会把空名替成「新卡组」，`LoadDeck` 逐格照抄才保住它"
            + "｜🧨 删掉 `LoadDeck` 末尾那句 `Deck.Name = deck.Name` ⇒ 这里就红了（那样 3-13 静默失效）");
            var err = st.Validate();
            C(err == DeckError.None, "A6a （前提）这副夹具当下过 `DeckRules.Validate(…, skirmish: true)`（实得 " + err
                                     + "，" + d.CardIds.Count + "/" + want + " 张）—— 不合法时 `FillNameFromWarlord` **故意不跑**");
            C(st.Deck != null && st.Deck.Name == CardText.Name(hero),
              "A6b ★ 3-13：合法 + 空名 ⇒ `Validate` 把名字补成**督军那张卡**的本地化卡名（原版 `DeckUtility__ValidateDeck.c:70-76`）"
            + "｜🧨 删掉 `DeckEditorState.Validate` 里的 `FillNameFromWarlord()` ⇒ 名字仍是空串 ⇒ 红"
            + "（实得「" + (st.Deck != null ? st.Deck.Name : "<null>") + "」· 期望「" + CardText.Name(hero) + "」）");
            // 结构性判别式：督军名必须与「随便一张别的卡」不同 —— 否则 A6b 分不出「填了督军」与「填了第一张」
            C(other != null && CardText.Name(other) != CardText.Name(hero),
              "A7 ★ 灭自证：督军名 ≠ 另一张卡的名（判别式；两串相等时 A6b 没有分辨力 ⇒ 本行红）");
            C(st.Deck != null && st.Deck.Name != (other != null ? CardText.Name(other) : null),
              "A8 ★ ……而且补上的名字**不是**那张无关卡的名（改了补名对象 ⇒ 红）");
        }
        catch (Exception e)
        {
            C(false, "A★ 3-13 段抛异常：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            CollectionData.PendingNewDeck = keepNew;
            CollectionData.PendingEditDeck = keepIdx;
        }
    }

    // ==================================================================
    //  B · 4-07：收藏 Deck 页搜索框字号 = 35（字面量比，⛔ 不从实现读回）
    // ==================================================================
    static void B_SearchBoxFontSize()
    {
        var src = ReadSrc(SrcCollectionWindow);
        if (src == null) return;
        C(CountOfCode(src, "\"Input Text\", 35f") == 1,
          "B1 ★ 4-07：源码里 `\"Input Text\", 35f` 恰 **1** 处（原版那颗 `Text Area/Text` 的 `m_fontSize = 35`）"
        + "｜🧨 改回 `30f` ⇒ 0 ⇒ 红（实得 " + CountOfCode(src, "\"Input Text\", 35f") + "）");
        C(CountOfCode(src, "\"Input Text\", 30f") == 0,
          "B2 ★ ……旧的 `30f` 已消失（0 处）｜🧨 两处并存 ⇒ 红");
        C(CountOfCode(src, "autoMinPx: 10f, autoMaxPx: 35f, autoBasePx: 26f") >= 1,
          "B3 ……自适应三格仍是原版的 **10 / 35 / 26**（`m_enableAutoSizing` 的 `m_fontSizeMin/Max/Base`）");
    }

    // ==================================================================
    //  C · 4-15：收藏窗关闭钮命中区外扩 10（原版 `m_RaycastPadding = (-10,-10,-10,-10)`）
    // ==================================================================
    static void C_CloseHitPadding()
    {
        var src = ReadSrc(SrcCollectionWindow);
        if (src != null)
        {
            C(CountOfCode(src, "AddHit(transform, \"CloseHit\", MenuDraw.PaddedHitRect(cr, ClosePad)") == 1,
              "C1 ★ 4-15：`CloseHit` 的命中矩形走 `MenuDraw.PaddedHitRect(cr, ClosePad)`（恰 1 处）"
            + "｜🧨 传回裸 `cr` ⇒ 0 ⇒ 红");
            C(CountOfCode(src, "static readonly Vector4 ClosePad = new Vector4(-10f, -10f, -10f, -10f);") == 1,
              "C2 ……而且 `ClosePad` = **(-10,-10,-10,-10)** 字面量（原版 `Collection Menu Variant.prefab` 的 `Close Button`；负 = 外扩）");
        }
        // 纯函数判别式：150×60 的钮 + pad(-10) ⇒ 170×80（与上面的源码扫描**不共因**）
        var box = MenuDraw.PaddedHitRect(new PxRect(0f, 0f, 150f, 60f), new Vector4(-10f, -10f, -10f, -10f));
        C(Mathf.Abs(box.x1 + 10f) < 1e-3f && Mathf.Abs(box.y1 + 10f) < 1e-3f
          && Mathf.Abs(box.x2 - 160f) < 1e-3f && Mathf.Abs(box.y2 - 70f) < 1e-3f
          && Mathf.Abs(box.W - 170f) < 1e-3f && Mathf.Abs(box.H - 80f) < 1e-3f,
          "C3 ★ 纯函数：`PaddedHitRect(150×60, -10 四边)` = (-10,-10)-(160,70) 即 **170×80**"
        + "（实得 (" + box.x1.ToString("F2") + "," + box.y1.ToString("F2") + ")-(" + box.x2.ToString("F2") + "," + box.y2.ToString("F2") + ")"
        + " " + box.W.ToString("F2") + "×" + box.H.ToString("F2") + "）｜🧨 `ClosePad` 改 0/正数 ⇒ 红");
    }

    // ==================================================================
    //  D · 3-10：方向门 = |atan2(dy,dx)|·Rad2Deg 落在 [30,150] ⇒ 这一下归滚动区
    // ==================================================================
    static void D_ScrollDragDirectionGate()
    {
        C(PointerLayer.IsScrollDragAngle(new Vector2(0f, 10f)), "D1 ★ 3-10：**正下方**拖动（90°）⇒ 归滚动区");
        C(PointerLayer.IsScrollDragAngle(new Vector2(0f, -10f)), "D2 ★ ……**正上方**也是（−90° ⇒ |·| = 90°）");
        C(PointerLayer.IsScrollDragAngle(new Vector2(0.5f, 0.8660254f)), "D3 ……60° 仍在带内（带边界是 30/150，不是 60/120）");
        C(PointerLayer.IsScrollDragAngle(new Vector2(-0.5f, 0.8660254f)), "D4 ……120° 也在带内");
        C(!PointerLayer.IsScrollDragAngle(new Vector2(10f, 0f)), "D5 ★ 正右方（0°）⇒ **不**归滚动区（= 拖格）");
        C(!PointerLayer.IsScrollDragAngle(new Vector2(-10f, 0f)), "D6 ……正左方（180°）同样拖格");
        C(!PointerLayer.IsScrollDragAngle(new Vector2(0.9659258f, 0.2588190f)), "D7 15° ⇒ 拖格（带外）");
        C(!PointerLayer.IsScrollDragAngle(new Vector2(-0.9659258f, 0.2588190f)), "D8 165° ⇒ 拖格（带外）");
        C(!PointerLayer.IsScrollDragAngle(Vector2.zero), "D9 ……零位移 ⇒ false（不判方向）");

        var src = ReadSrc(SrcPointerLayer);
        if (src == null) return;
        C(CountOfCode(src, "return ang >= 30f && ang <= 150f;") == 1,
          "D10 ★ 带子字面量 = `ang >= 30f && ang <= 150f`（原版 `ang < 30 || 150 < ang` 才拖格 ⇒ 30/150 **含在带内**）"
        + "｜🧨 改成 60/120（那是 `SupportMethods__IsScrollDragThreshold` 的窄带）⇒ 红");
        C(CountOfCode(src, "IsScrollDragAngle(px - _pressPx)") == 1,
          "D11 ★ 生产路（`Update`）有一道门｜🧨 只做自检路 ⇒ 0 ⇒ 红");
        C(CountOfCode(src, "IsScrollDragAngle(p - _pressPx)") == 1,
          "D12 ★ 自检直调路（`MoveTo`）也有一道门（与生产路同语义）｜🧨 两条路不一致 ⇒ 批处理绿/真机不对");
        C(CountOfCode(src, "_dragItem.ScrollGate") == 2,
          "D13 ★ 两道门都只对 **`ScrollGate` 件**生效（= 原版只有 `Initialize(父 ScrollRect)` 过的 `DragAndDropItem` 才有这道门；"
        + "别的可拖件保持旧行为）｜🧨 去掉这个闸 ⇒ 两道门会劫持所有可拖件（如 `Editor/ShellScene.cs` ⑤ 那个竖直拖探针）"
        + "（实得 " + CountOfCode(src, "_dragItem.ScrollGate") + "）");
        var cw = ReadSrc(SrcCollectionWindow);
        if (cw != null)
            C(CountOfCode(cw, "DeckDragDrop(cell, displayPos, pos), true)") == 1,
              "D14 ★ 收藏卡组格登记时**开了**方向门（`RegisterDrag(..., true)`）—— ⛔ 漏掉这个实参 ⇒ 本条修复等于没接上"
            + "｜🧨 删掉那个 `true` ⇒ 0 ⇒ 红");
    }

    // ==================================================================
    //  E · 3-28：拖起那一格**不 reparent、不重裁**（= 原版等价形态；判据见文件头「3-28」）
    // ==================================================================
    static void E_DragCellNotReparentedNorReclipped()
    {
        var src = ReadSrc(SrcCollectionWindow);
        if (src == null) return;
        var move = MethodBody(src, "void DeckDragMove(Transform cell, int displayPos, Vector2 pos)");
        C(move != null, "E1 （前提）从源码里切出了 `DeckDragMove` 的方法体（切不出来时本节**等于没验** ⇒ 当场红）");
        if (move == null) return;
        C(CountOfCode(move, "SetParent") == 0,
          "E2 ★ 3-28：`DeckDragMove` 里**没有** `SetParent` —— 原版 reparent 到 `transform.root` 的**目的就是跳出视口裁**，"
        + "我们这套绝对摆位网格不 reparent ⇒ **同样不被裁**（等价，不是缺做）｜🧨 加一句 reparent ⇒ 红");
        C(CountOfCode(move, "cell.localPosition = _deckDragHomeLocal + DeckDragDesignDelta(") == 1,
          "E3 ★ ……位移走的是 **`localPosition` + 起拖前基准**（不碰世界位置、也不碰裁切）");
        C(CountOfCode(move, "Clip") == 0, "E4 ★ ……而且**没有**任何重裁调用（`MenuDraw.ClipQuad` / `SetClipFromTree` 都不在方法体里）"
                                         + "｜🧨 为「补裁切」加一句 ⇒ 红（原版本来就不裁，见文件头判据）");
        C(CountOfCode(src, "public const int DeckDragQueueBoost = 20;") == 1,
          "E5 ……抬层仍走 `DeckDragQueueBoost`（本工程分层靠 `SetRenderQueue`、不靠 z / 不靠 reparent）");
    }

    // ==================================================================
    //  工具
    // ==================================================================
    static string ReadSrc(string rel)
    {
        try
        {
            string p = Path.Combine(Application.dataPath, rel.Replace("Assets/", ""));
            if (!File.Exists(p))
            {
                C(false, "S0 读得到源码 `" + rel + "`（" + p + "）—— 读不到时源码闸**等于没验** ⇒ 当场红");
                return null;
            }
            return File.ReadAllText(p);
        }
        catch (Exception e)
        {
            C(false, "S0 读源码 `" + rel + "` 抛异常：" + e.GetType().Name + "：" + e.Message);
            return null;
        }
    }

    /// <summary>数**非注释行**里子串出现次数（整行 `//` 开头的跳过；`grep` 口径同 `B141_W6_Assert.CountOfCode`）。</summary>
    static int CountOfCode(string src, string needle)
    {
        if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(needle)) return -1;
        int n = 0;
        foreach (var raw in src.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            var t = line.TrimStart();
            if (t.StartsWith("//", StringComparison.Ordinal)) continue;
            int at = 0;
            while ((at = line.IndexOf(needle, at, StringComparison.Ordinal)) >= 0) { n++; at += needle.Length; }
        }
        return n;
    }

    /// <summary>按签名切出方法体（花括号配平；切不出来返回 null ⇒ 调用方**必须**当场红，⛔ 不许静默跳过）。</summary>
    static string MethodBody(string src, string signature)
    {
        if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(signature)) return null;
        int at = src.IndexOf(signature, StringComparison.Ordinal);
        if (at < 0) return null;
        int open = src.IndexOf('{', at);
        if (open < 0) return null;
        int depth = 0;
        for (int i = open; i < src.Length; i++)
        {
            if (src[i] == '{') depth++;
            else if (src[i] == '}')
            {
                depth--;
                if (depth == 0) return src.Substring(open, i - open + 1);
            }
        }
        return null;
    }
}
