// B121_C3_Assert.cs — 执行代理 `B121-C3`（2026-11-03）· 收藏窗**卡组格**「被 Ban」那一档
//
// ============================ 它盯的是什么 ============================
//  原版 `CollectionDeck.Config(CardDeck)` 末尾那一段（= 账 `收藏` 的 `B121-COLL-09`）：
//    ① `bannedImage.SetActive(Any(deck.GetLibraryInFull(), c => deck.CustomGameModeEvent.IsCardBanned(c)))`
//    ② 卡名颜色 = `banned ? (1, 0.3, 0.3, 1) : (1, 1, 1, 1)`
//    ③ `greyscaleController.ToggleGreyScale(banned, 0.25f)` —— 灰的是 **4 张图**：
//       `Deck Image` / `Frame` / `Faction Icon` / `Game Mode Icon`（`Ban Icon` 自己与 `Deck Name` 不灰）
//  生产实现 = `Shell/MenuDraw.cs` 类尾的 `DeckBan`（判据全文与几何常量都在那个类的注释里，⛔ 别在这里抄第二份）。
//
// ============================ 判据出处（⛔ 本文件不重抄数值推导）============================
//  · `d:/2/tools/decomp_full/CollectionDeck__Config.c` 末尾那一段
//  · `d:/2/tools/decomp_full/CollectionDeck.__c__DisplayClass16_0___Config_b__0.c`（谓词 = `PlayEventData.IsCardBanned`）
//  · `d:/2/tools/decomp_full/CardDeck__GetLibraryInFull.c`（`cardIds` + 督军 `Insert(0)` + 防御卡 `Insert(0)`）
//  · `python -I 工具/menu_dump.py bundle_menus_assets_all -2024334647938937812 --depth 3`
//    （`Ban Icon -25,-25.11 → 275,352.5` 300×377.61 · 子件 `Banned Text` 绝对 20,125.93 → 230,201.45 ·
//     TMP 38.25 / 基准 36 / auto 0.25…72 / Center-Midline / 折行 1 · `Localize.mTerm = MenuDeck/Banned`）
//  · `工具/read_literal.py` 实读：`0x1834b3210…` = (1, 0.3, 0.3, 1) · `0x1834b2e50…` = (1,1,1,1) · `0x1834b2e7c` = **0.25**
//
// ============================ 🔴 为什么本段今天「靠注入才亮」============================
//  生产路 `DeckRuntime.CurrentEventData == null`（禁卡表在 LiveOps 远端，本地 0 事件）⇒ `DeckBan.IsBanned` 恒 false。
//  本段**临时**把 `DeckRuntime.CurrentEventData` 赋成 `TestEvent`（`finally` 里还原），用来证明机制是通的。
//  ⛔ 别把这段当成「生产上会看到 ban 图标」—— 报告里已如实写「休眠」。
//
// ============================ 🧨 灭自证（一条改动不能两边都绿）============================
//  🧨 1「默认关 vs 点亮」：无活动 ⇒ 必须**没有** `Ban Icon` 节点；注入禁卡 ⇒ 必须**有**且 `activeSelf`。
//     把 `Apply` 的早退删掉（恒建）⇒ 前半红；把 `IsBanned` 写成恒 false ⇒ 后半红。
//  🧨 2「有活动 ≠ 就点亮」：注入一个**不在这副牌里**的 id ⇒ 必须仍然没有图标。
//     把谓词写成 `CurrentEventData != null` ⇒ 这一条红。
//  🧨 3「三处都要扫」：督军 / 普通卡 / 防御卡 **各注入一次**，每次都必须点亮
//     （= 原版 `GetLibraryInFull` 的 `cardIds` + 两次 `Insert(0, …)`）。少扫一处 ⇒ 对应那条红。
//  🧨 4「灰的是哪 4 张」：`Deck Image` / `Frame` / `Faction Icon` / `Game Mode Icon` 各 0.25，
//     而 `Ban Icon` **不灰**。把 `Grey(...)` 的四行删任一行 ⇒ 对应那条红；把 `Ban Icon` 也灰上 ⇒ 最后那条红。
//  🧨 5 几何：Ban Icon 实绘 = 300×377.61 × sx/sy（本段格子 225×364.5 ⇒ ×0.9 = 270×339.849）。
//     常量抄错 ⇒ 红。队列：图标 > `Frame`（同一格同一时刻两个队列不能相等，否则谁盖谁看运气）。
//
// ============================ 怎么接 ============================
//  宿主里一行（`Run()` 自建一个临时根、跑完销毁；不需要 `CollectionWindow`）：
//      B121_C3_Assert.Run(); pass += B121_C3_Assert.Pass; fail += B121_C3_Assert.Fail;
//  判绿红：`grep "^B121C3"` 的**合计行**；逐条按**行首** `B121C3 ✓` / `B121C3 ✗` 数（⛔ 别 `grep -c ✗`）。
//  ⚠️ 本文件**不建持久资产**；只临时改 `DeckRuntime.CurrentEventData`，`finally` 里还原并销毁根节点。
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B121-C3`：卡组格「被 Ban」（Ban Icon + 名字变红 + 四张图灰 0.25）的回归断言。</summary>
public static class B121_C3_Assert
{
    const string Tag = "B121C3";
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

    static void Near(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "｜实际 " + got.ToString("F3") + "，期望 " + want.ToString("F3") + " ±" + tol.ToString("F3"));
    }

    /// <summary>测试用 `PlayEventData` 子类（原版那个是 `abstract`；我们只借 `bannedCardIds` + `IsCardBanned`）。</summary>
    sealed class TestEvent : PlayEventData { }

    // 本段用的那一格：桌面档 225×364.5（= 原版作者系 250×405 × 0.9）⇒ sx = sy = 0.9
    const float CellW = 225f, CellH = 364.5f;
    static PxRect CellRect { get { return new PxRect(0f, 0f, CellW, CellH); } }
    static float SX { get { return CellW / 250f; } }
    static float SY { get { return CellH / 405f; } }

    // 原版 `Ban Icon` 作者系矩形（与 `DeckBan` 的常量同源；这里**独立写一份**，抄错实现才会红）
    const float OrigIconX = -25f, OrigIconY = -25.11f, OrigIconW = 300f, OrigIconH = 377.61f;

    // 队列口径照 `CollectionWindow` 那一页（q/qBelow/qText/qOverlay/qHit）+ 本档自己的 qOverlay+1
    const int Q = 3032, QBelow = 3031, QText = 3033, QOverlay = 3034, QHit = 3032;

    static int _seq;

    static Transform Build(Transform root, CollectionData.DeckInfo info, int deckIndex, string tag)
    {
        return MenuDraw.DeckCell(root, "B121C3_" + tag + "_" + (_seq++), CellRect, info, false,
                                 Q, QBelow, QText, QOverlay, QHit, null,
                                 gameMode: info.GameMode, deckIndex: deckIndex);
    }

    static float GreyOf(Transform cell, string layer)
    {
        if (cell == null) return -999f;
        var t = cell.Find(layer);
        if (t == null) return -998f;
        var q = t.GetComponent<ImageQuad>();
        return q != null ? q.Grey : -997f;
    }

    static Color NameInk(Transform cell)
    {
        if (cell == null) return Color.clear;
        var t = cell.Find("Deck Name");
        var lb = t != null ? t.GetComponent<Label>() : null;
        return lb != null ? lb.color : Color.clear;
    }

    static bool IsRed(Color c) { return Mathf.Abs(c.r - 1f) < 1e-3f && Mathf.Abs(c.g - 0.3f) < 1e-3f && Mathf.Abs(c.b - 0.3f) < 1e-3f; }
    static bool IsWhite(Color c) { return Mathf.Abs(c.r - 1f) < 1e-3f && Mathf.Abs(c.g - 1f) < 1e-3f && Mathf.Abs(c.b - 1f) < 1e-3f; }

    /// <summary>跑整段（不需要宿主窗口；`Run()` 自建临时根）。</summary>
    public static void Run() { Run(null); }

    /// <summary>跑整段。<paramref name="host"/> = 可选父节点（给宿主把这段挂在它自己的树下面用；`null` ⇒ 自建根）。</summary>
    public static void Run(Transform host)
    {
        int n = CollectionData.DeckCount();
        Check(n > 0, "前提：卡组库里至少有一套卡组（现有 " + n + " 套）—— 否则本段无从量");
        if (n <= 0) return;

        var info = CollectionData.DeckAt(0);
        var deck = CollectionData.Raw(0);
        Check(deck != null, "前提：第 0 套卡组读得到（`CollectionData.Raw(0)`）");
        if (deck == null) return;
        Check(!string.IsNullOrEmpty(deck.WarlordId), "前提：第 0 套有督军（`GetLibraryInFull` 的第一处插入）｜实际 `" + deck.WarlordId + "`");

        var oldEvent = DeckRuntime.CurrentEventData;
        Transform root = null;
        try
        {
            root = host != null
                 ? MenuDraw.Node(host, "B121C3Root", CellRect)
                 : new GameObject("B121C3Root", typeof(RectTransform)).transform;
            Check(root != null, "临时根节点建出来了（宿主没给 ⇒ 自建在原点）");
            if (root == null) return;

            // ---------------------------- ① 休眠档：无活动 ⇒ 整层不建、名字白、不灰 ----------------------------
            DeckRuntime.CurrentEventData = null;
            var idle = Build(root, info, 0, "idle");
            Check(idle != null, "无活动档：卡组格建出来了");
            if (idle == null) return;
            Check(idle.Find("Ban Icon") == null, "【默认关】无活动 ⇒ **不建** `Ban Icon`（原版 `bannedImage.SetActive(false)`）");
            Check(IsWhite(NameInk(idle)), "【默认关】卡名 = 白 (1,1,1,1)");
            string idleMain = idle.Find("Deck Image") != null ? "Deck Image" : "CardBack";
            // ⚠️ `ImageQuad.Grey` 在「材质根本没有 `_GreyScale`」时返回 **-1**（没灰过的 `Sprites/Default` 就是这一档），
            //    而 `ArtOpaque`（`Deck Image` 换过材质）读回 **0** ⇒ 「没灰」= `-1 或 0`，⛔ 别只断 0。
            Check(GreyOf(idle, idleMain) < 0.001f,
                  "【默认关】主图（`" + idleMain + "`）没被灰（-1 = 材质没 `_GreyScale` / 0 = 灰度为 0）｜实际 "
                  + GreyOf(idle, idleMain).ToString("F3"));

            // ---------------------------- ② 点亮档：禁掉督军 ⇒ 图标 + 红名 + 四张图 0.25 ----------------------------
            DeckRuntime.CurrentEventData = Event(deck.WarlordId);
            var hit = Build(root, info, 0, "warlord");
            var icon = hit != null ? hit.Find("Ban Icon") : null;
            Check(icon != null, "禁掉督军(`" + deck.WarlordId + "`) ⇒ **建出** `Ban Icon`");
            if (icon == null) return;
            Check(icon.gameObject.activeSelf, "禁掉督军 ⇒ `Ban Icon` `activeSelf == true`");
            Check(icon.parent == hit, "`Ban Icon` 是**卡组格根的直接子件**（原版它是 `content` 下 `Deck` 的兄弟）");

            // ②-a 几何（原版 300 × 377.61 作者 px × sx/sy）
            var q = icon.GetComponent<ImageQuad>();
            Check(q != null, "`Ban Icon` 上有 `ImageQuad`（是真图，不是空节点）");
            if (q != null)
            {
                var rr = MenuDraw.QuadRectPx(q);
                Near(rr.W, OrigIconW * SX, 0.6f, "`Ban Icon` 实绘宽 = 300 × sx（= 270.00 px @0.9）");
                Near(rr.H, OrigIconH * SY, 0.6f, "`Ban Icon` 实绘高 = 377.61 × sy（= 339.849 px @0.9）");
                Near(rr.x1, CellRect.x1 + OrigIconX * SX, 0.6f, "`Ban Icon` 左 = 格左 + (−25)×sx（= −22.50）");
                Near(rr.y1, CellRect.y1 + OrigIconY * SY, 0.6f, "`Ban Icon` 上 = 格上 + (−25.11)×sy（≈ −22.599）");
                Check(q.Grey < 0.001f, "`Ban Icon` **自己不被灰**（原版那 4 条 `imageMaterialPairs` 里没有它）｜实际 " + q.Grey.ToString("F3"));
            }
            var frameT = hit.Find("Frame");
            var frameQ = frameT != null ? frameT.GetComponent<ImageQuad>() : null;
            Check(frameQ != null, "前提：本格有 `Frame` 那一层（队列比较的基准）");
            if (q != null && frameQ != null)
            {
                Check(q.RenderQueue > frameQ.RenderQueue,
                      "`Ban Icon` 队列 > `Frame` 队列（原版它排在整块 `Deck` 之后 ⇒ 必须画在上面）｜"
                      + q.RenderQueue + " vs " + frameQ.RenderQueue);
                Check(q.RenderQueue > QOverlay, "`Ban Icon` 队列 > 本格内容层顶（qOverlay " + QOverlay + "）｜实际 " + q.RenderQueue);
            }

            // ②-b 子件 `Banned Text`
            var txt = icon.Find("Banned Text");
            Check(txt != null, "`Ban Icon` 下有 `Banned Text` 子件（原版 `Ban Icon/Banned Text`）");
            var tlb = txt != null ? txt.GetComponent<Label>() : null;
            Check(tlb != null, "`Banned Text` 上有 `Label` 组件");
            if (tlb != null)
            {
                var want = Loc.T("MenuDeck/Banned");
                Check(tlb.Text == want, "文案 = 本地化 `MenuDeck/Banned`；实际 `" + tlb.Text + "`，期望 `" + want + "`");
                Near(tlb.FontSizeToPxNow(tlb.FontSizeBase, tlb.ActualFace), 38.25f * SY, 0.6f,
                     "字号 = 38.25 × sy（原版 `m_fontSize 38.25` · 基准 36）");
                var tmp = tlb.GetComponentInChildren<TMPro.TMP_Text>(true);
                var rt = tmp != null ? tmp.rectTransform : null;
                Check(rt != null, "`Banned Text` 的 TMP 子节点有 `RectTransform`（框宽高写在它身上）");
                if (rt != null)
                {
                    Near(rt.sizeDelta.x * 108f, 210f * SX, 0.8f, "字框宽 = 210 × sx（原版绝对 20→230）= 189.00 px");
                    Near(rt.sizeDelta.y * 108f, 75.52f * SY, 0.8f, "字框高 = 75.52 × sy = 67.968 px");
                }
                Check(tlb.RenderQueue > QOverlay, "`Banned Text` 队列 > qOverlay｜实际 " + tlb.RenderQueue);
            }

            // ②-c 卡名变红
            Check(IsRed(NameInk(hit)), "禁卡命中 ⇒ 卡名 = (1, 0.3, 0.3, 1)｜实际 " + NameInk(hit));

            // ②-d 四张图灰 0.25（`Ban Icon` / `Deck Name` 不灰，见上面 ②-a 与这条的红名）
            //    ⚠️ 主图那一格两种节点名：有督军插图 ⇒ `Deck Image`；退回画卡背 ⇒ `CardBack`（同一格只有一个）。
            string mainLayer = hit.Find("Deck Image") != null ? "Deck Image" : "CardBack";
            Check(hit.Find("Deck Image") != null || hit.Find("CardBack") != null,
                  "前提：本格有主图（`Deck Image` 或退回档 `CardBack`）—— 那条 `deckCardback` 的 `imageMaterialPairs` 之一");
            Near(GreyOf(hit, mainLayer), 0.25f, 1e-4f, "主图（`" + mainLayer + "`）灰度 = 0.25");
            Near(GreyOf(hit, "Frame"), 0.25f, 1e-4f, "`Frame` 灰度 = 0.25");
            var fac = hit.Find("Faction Icon");
            Check(fac != null, "前提：本格有 `Faction Icon`（阵营名非空）—— 那条 `imageMaterialPairs` 之一");
            if (fac != null) Near(GreyOf(hit, "Faction Icon"), 0.25f, 1e-4f, "`Faction Icon` 灰度 = 0.25");
            var modeT = hit.Find("Game Mode Icon");
            Check(modeT != null, "前提：本格有 `Game Mode Icon`（`gameMode` 能映射到图）—— 那条 `imageMaterialPairs` 之一");
            if (modeT != null) Near(GreyOf(hit, "Game Mode Icon"), 0.25f, 1e-4f, "`Game Mode Icon` 灰度 = 0.25");

            // ---------------------------- ③ 灭自证：有活动 ≠ 就点亮 ----------------------------
            DeckRuntime.CurrentEventData = Event("__b121c3_not_a_real_card__");
            var miss = Build(root, info, 0, "miss");
            Check(miss != null && miss.Find("Ban Icon") == null,
                  "活动在、但这副牌里没有那张禁卡 ⇒ **仍然不点亮**（谓词是「Any(GetLibraryInFull(), IsCardBanned)」，不是「有活动就点亮」）");
            Check(miss != null && IsWhite(NameInk(miss)), "未命中 ⇒ 卡名仍白");

            // ---------------------------- ④ 三处扫描：普通卡 / 防御卡 ----------------------------
            if (deck.CardIds != null && deck.CardIds.Count > 0)
            {
                DeckRuntime.CurrentEventData = Event(deck.CardIds[0]);
                var c = Build(root, info, 0, "card");
                Check(c != null && c.Find("Ban Icon") != null,
                      "禁掉普通卡 `" + deck.CardIds[0] + "` ⇒ 点亮（`GetLibraryInFull` 的主表那一半）");
            }
            else Check(false, "前提：第 0 套有普通卡（`CardIds` 非空）—— 没有 ⇒ 「扫主表」那一半没被验到");

            if (!string.IsNullOrEmpty(deck.DefensiveId))
            {
                DeckRuntime.CurrentEventData = Event(deck.DefensiveId);
                var c = Build(root, info, 0, "def");
                Check(c != null && c.Find("Ban Icon") != null,
                      "禁掉防御卡 `" + deck.DefensiveId + "` ⇒ 点亮（`GetLibraryInFull` 的 `Insert(0, defensive)` 那一半）");
            }
            else Debug.Log(Tag + " ⓘ 跳过「防御卡」那一支：第 0 套没有 `DefensiveId`（原版那一处 `Insert(0, …)` 本机无从验）");
        }
        finally
        {
            DeckRuntime.CurrentEventData = oldEvent;
            if (root != null) Object.DestroyImmediate(root.gameObject);   // 自建根与挂在宿主上的临时根一起收掉（⛔ 不留脏节点）
        }
    }

    /// <summary>造一个只带一条禁卡的 `PlayEventData`（走活动线同一个写入口 `SetBannedCards`）。</summary>
    static PlayEventData Event(string bannedId)
    {
        var evt = new TestEvent();
        evt.SetBannedCards(string.IsNullOrEmpty(bannedId) ? null : new List<string> { bannedId });
        return evt;
    }
}
