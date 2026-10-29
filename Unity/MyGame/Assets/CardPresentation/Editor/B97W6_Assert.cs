// B97W6_Assert.cs — 执行代理 B97_W6（第九十七会话 · 波 3）的断言
//
// ============================ 它盯的是什么 ============================
//   收藏窗**卡池**的「藏卡 ban 徽标」整层（原版 `Collection Card/CardUI/Ban Icon` + 子件 `Banned Text`）。
//   生产实现 = `Shell/CollectionWindow.cs` 的 `BuildCardsBanBadge`（图标 + 「禁止」那行字 + 显隐）
//   与 `IsCardBanned`（数据源 = `PlayEventData.IsCardBanned`）。
//
// 判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B97_藏卡ban徽标_施工单.md` ＋ 本轮报告 `资料/完全复刻/报告/B97_W6.md`。
//
// ============================ 三条断言 ============================
//   ① 几何：`Ban Icon 0` 的实绘框 = **196.875 × 196.875 px**（±0.5），中心 = 格中心，队列 > 卡(3000)。
//      期望值**从原版 prefab 字段推**（1.75 卡单位 × `CardUI.localScale 150` × 本页 0.75 缩），
//      ⛔ 不读我们实现里的 `side` 常量（那是自证）。
//   ② 字框 = 方叉的 **0.8 × 0.2**（157.5 × 39.375 px）、字号 base ≈ **27 px**、文案 = `Loc.T("MenuDeck/Banned")`。
//   ③ 显隐：默认（无活动）**图标与文字都关**；造一个带禁卡表的活动事件后 **两者都开**（灭自证：两条都要）。
//
// ============================ 怎么用（宿主）============================
//   宿主里一行（放在切到 Cards 页之后；本函数自己也会切并重建页）：
//       B97W6Assert.Run(win);        // win = 已建好的 CollectionWindow（Editor/CollectionScene.cs 的 Build(out root) 之后）
//   🔴 **别传 null** —— 传 null 会判一条红（不许静默跳过整段）。
//   本文件**不建持久资产**；只临时改 `DeckRuntime.CurrentEventData`，`finally` 里还原并重建回出厂态。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · 把 `side = 1.75f * pxPerUnit` 改成 `side = 1.75f`（忘乘）⇒ 量到 ~1.99 px ⇒ ①红。
//   · 把 `tw = side * 0.8f, th = side * 0.2f` 的系数改掉 ⇒ ②红；
//     `autoBasePx: basePx` 抹成 0 ⇒ 字号回落到 TMP 出厂 ⇒ ②红。
//   · 把 `icon.gameObject.SetActive(banned)` 改成 `SetActive(true)` ⇒ 「默认关」那半红；
//     改成 `SetActive(false)` ⇒ 「点亮」那半红。**两条都断 ⇒ 两边一起改不可能同时绿。**
//   · 把调用行 `BuildCardsBanBadge(...)` 删掉 ⇒ 控件根本不存在 ⇒ ①②③全红。
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 B97_W6：收藏窗卡池「藏卡 ban 徽标」整层的断言。计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B97W6Assert
{
    static readonly CheckSink _sink = new CheckSink("[B97W6] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Near(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ---------------------------------------------------------------- 原版 prefab 真值（逐字段现读）
    // `d:/2/tools/assetripper_out/ExportedProject/Assets/GameObject/Collection Card.prefab`
    const float OrigCardH = 512f;          // `Collection Card` 原型高
    const float OrigCardUiScale = 150f;    // `CardUI.m_LocalScale`
    const float OrigBanUnits = 1.75f;      // `Ban Icon.sizeDelta`（**卡单位**）
    const float OrigBanPx = OrigBanUnits * OrigCardUiScale;   // = 262.5（原版 350×512 卡里的方叉）
    const float OrigFontBase = 36f;        // `Banned Text` TMP `m_fontSizeBase`
    const float OrigTextW = 0.8f, OrigTextH = 0.2f;           // `Banned Text` 框 = 方叉的 0.8 × 0.2

    /// <summary>本页格子 = 原版 350×512 × **0.75**（`CollectionWindow.CardsCellH = 384` = 512 × 0.75）。</summary>
    static float PageScale { get { return CollectionWindow.CardsCellH / OrigCardH; } }
    /// <summary>方叉边长 = 1.75 × 150 × 0.75 = **196.875 px**（期望值不从实现的 `side` 读）。</summary>
    static float ExpectSidePx { get { return OrigBanUnits * OrigCardUiScale * PageScale; } }

    /// <summary>测试用 `PlayEventData` 子类（原版那个是 `abstract`；我们只借 `bannedCardIds` + `IsCardBanned`，无其它抽象成员）。</summary>
    sealed class TestEvent : PlayEventData { }

    /// <summary>跑整段（宿主 = `Editor/CollectionScene.cs`，`win` 已建好）。</summary>
    public static void Run(CollectionWindow win)
    {
        MenuCheck.Section(_sink, "藏卡 ban 徽标（收藏窗卡池）");
        if (win == null)
        {
            Tr(false, "宿主没给 `CollectionWindow`（`B97W6Assert.Run(null)`）⇒ 本段整段跳过 = 静默 ⇒ 判红");
            return;
        }
        var oldEvent = DeckRuntime.CurrentEventData;
        try
        {
            win.tabButtons.Click(1);                                     // 切到 Cards 页（懒建）
            if (win.CardsScroll != null) win.CardsScroll.SetOffset(0f);  // 归零滚动 ⇒ 第 0 格必在视口内（否则徽标不建、下面全假阴）

            var cards = CollectionWindow.CardsState != null ? CollectionWindow.CardsState.VisibleCards() : null;
            Tr(cards != null && cards.Count > 0, "卡池非空（否则下面每条都无从量）");
            if (cards == null || cards.Count == 0) return;

            // ---------- ③-a 默认（无活动）⇒ 图标与文字都关 ----------
            DeckRuntime.CurrentEventData = null;
            win.RebuildCardsPage();
            var icon0 = FindDeep(win.PageRoot(1), "Ban Icon 0");
            Tr(icon0 != null, "第 0 格建出了 ban 图标节点（`Ban Icon 0`）—— 没有 ⇒ 徽标整层没建 / 被视口裁掉");
            if (icon0 == null) return;
            var text0 = FindDeep(icon0, "Banned Text");
            Tr(text0 != null, "`Ban Icon 0` 下有 `Banned Text` 子件（原版那行「禁止」）");
            Tr(!icon0.gameObject.activeSelf,
               "【默认关】无活动 ⇒ 图标 `activeSelf == false`（原版 `SetActive(banned)`，banned = false）");
            Tr(text0 != null && !text0.gameObject.activeSelf, "【默认关】无活动 ⇒ 文字 `activeSelf == false`");

            // ---------- ③-b 造禁卡事件 ⇒ 该格图标与文字都开 ----------
            string banId = cards[0].Id;
            var evt = new TestEvent();
            evt.SetBannedCards(new[] { banId });
            DeckRuntime.CurrentEventData = evt;
            win.RebuildCardsPage();
            icon0 = FindDeep(win.PageRoot(1), "Ban Icon 0");
            Tr(icon0 != null, "点亮后 `Ban Icon 0` 仍在");
            if (icon0 == null) return;
            text0 = FindDeep(icon0, "Banned Text");
            Tr(icon0.gameObject.activeSelf, "【点亮】禁掉第 0 格那张卡（" + banId + "）⇒ 图标 `activeSelf == true`");
            Tr(text0 != null && text0.gameObject.activeSelf, "【点亮】文字 `activeSelf == true`");

            // ---------- ② 字框 = 0.8×0.2、字号 base ≈ 27 ----------
            var lb = text0 != null ? text0.GetComponent<Label>() : null;
            Tr(lb != null, "`Banned Text` 上有 `Label` 组件");
            if (lb != null)
            {
                var rt = lb.transform as RectTransform;
                Tr(rt != null, "`Banned Text` 有 RectTransform（量框用）");
                if (rt != null)
                {
                    // `SetWrapWidth` 把 sizeDelta.x 写成折行宽、`SetAutoFitBox` 写 sizeDelta.y = 框高（都是世界单位）
                    Near(rt.sizeDelta.x * 108f, ExpectSidePx * OrigTextW, 0.6f, "字框宽 = 方叉 × 0.8（157.5 px）");
                    Near(rt.sizeDelta.y * 108f, ExpectSidePx * OrigTextH, 0.6f, "字框高 = 方叉 × 0.2（39.375 px）");
                }
                float basePx = lb.FontSizeToPxNow(lb.FontSizeBase, lb.ActualFace);
                Near(basePx, ExpectSidePx * (OrigFontBase / OrigBanPx), 0.6f,
                     "字号 base = 方叉 × 36 / 262.5 ≈ 27 px");
                var want = Loc.T("MenuDeck/Banned");
                Tr(lb.Text == want, "文案 = 本地化 `MenuDeck/Banned`（en Banned / zh 禁止）；实际 `" + lb.Text + "`，期望 `" + want + "`");
            }

            // ---------- ① 几何：方叉边长 196.875 px、中心 = 格中心、队列 > 卡 ----------
            var q = icon0.GetComponent<ImageQuad>();
            Tr(q != null, "`Ban Icon 0` 上有 `ImageQuad`（图标是真图，不是空节点）");
            if (q != null)
            {
                var rr = MenuDraw.QuadRectPx(q);
                Near(rr.W, ExpectSidePx, 0.5f, "方叉实绘宽 = 196.875 px（原版 1.75 卡单位 × 150 × 0.75）");
                Near(rr.H, ExpectSidePx, 0.5f, "方叉实绘高 = 196.875 px");
                var cell = win.CardsScroll != null ? win.CardsScroll.Shift(CollectionWindow.CardsCellRect(0))
                                                   : CollectionWindow.CardsCellRect(0);
                Near(rr.CX, cell.CX, 0.5f, "方叉中心 x = 格中心 x");
                Near(rr.CY, cell.CY, 0.5f, "方叉中心 y = 格中心 y");
                Tr(q.RenderQueue > 3000, "图标渲染队列 > 卡(3000)（实际 " + q.RenderQueue + "）");
                if (lb != null) Tr(lb.RenderQueue > 3000, "文字渲染队列 > 卡(3000)（实际 " + lb.RenderQueue + "）");
            }
        }
        finally
        {
            DeckRuntime.CurrentEventData = oldEvent;
            try { win.RebuildCardsPage(); } catch { /* 还原失败不该盖住上面的断言结果 */ }
        }
    }

    /// <summary>按名字**往深处**找一个节点（`MenuCheck.FindChild` 只认直接子件，这里要跨 `Content &gt; Ban Icon &gt; Banned Text`）。</summary>
    static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var r = FindDeep(root.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }
}
