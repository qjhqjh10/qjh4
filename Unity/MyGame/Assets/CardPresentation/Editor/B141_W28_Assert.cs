// B141_W28_Assert.cs — 执行写手 `B141-W28`（2026-11-04 · 第一百二十八会话）· `B138 §4` **4-17**
//   = 「队列带重叠：档案页 Avatar 与开包窗同号 3170」（队列表三 #15）的验收断言。
//   施工判据 = `资料/完全复刻/判据/B141_W129_对账_UI动画.md` W15 包 + `报告/B138_审计汇总_问题清单.md` §4 的 4-17。
//
// ============================ 本文件断什么（五组，逐条独立于实现）============================
//   ① **不再同号**：开包窗整带（`QShade`…`QHintHit`）与 Avatar 页 `Q`（= `PlayerProfileWindow.QPageBase + 1*10`）
//      既不相等、也不区间相交。
//   ② **先后按原版**：原版两扇**不在同一画布** —— 档案窗 `windowsPlacement 15 (Popup)` ⇒ `3 - PopUp Holder`
//      （其 `Canvas_1075` = `m_OverrideSorting: 1` + `m_SortingLayerID 1054366423` = `PopUps`，`TagManager` 里
//      **索引 7**）> 开包窗 `5 (Canvas)` ⇒ `2 - Canvas Holder Above upper bar`（合成进 `Canvas_1077`／`Game UI`，
//      `Default` = **索引 0**）⇒ **排序层索引压过 order** ⇒ 档案窗整棵**永远**画在开包窗之上。
//      ⇒ 断：开包窗整带**严格低于** `PlayerProfileWindow.QShade`（档案窗最低一层）。
//   ③ **灭自证**：同一条「低于档案带」的判据，喂**旧值 3170** 必须判**假**、喂「把本窗抬到档案带之上」也必须判**假**
//      ⇒ 「两边改成同一个新号」「随便换个号」都过不了。
//   ④ **带内不变量没被挪坏**：`QShade < QCard < QHint < QHintHit` · 带长 28 · 5 张卡（各 5 档）全落在 `QCard…QHint−1` 内。
//   ⑤ **顶栏之上的那一半 + 左栏那一半**：本带仍 **> 顶栏 `QBarOverlay`(2998)**（原版 `2 - Canvas Holder` 是
//      `Upper bar` 之后的兄弟）；但已降到 `QNavHit`(3156) **之下** ⇒ **必须** `BlocksLeftNav = true`
//      （否则左栏会变成能点 —— 点到一个被整屏压暗层挡住、看不见的按钮），且仍 **> `QNavHitBlocked`(3001)**。
//
// ============================ 怎么接线（宿主）============================
//   宿主在**白名单外**（`Editor/ShellScene.cs` / `Editor/ShopScene.cs`），本批没有自己往宿主里接。宿主里加两行即可：
//
//       B141_W28_Assert.Run();
//       pass += B141_W28_Assert.Pass; fail += B141_W28_Assert.Fail;
//
//   ⚠️ 它**不建窗口、不动场景**，只读常量 + 建一颗**裸 `GameObject` 探针**读 `BlocksLeftNav`（读完立刻 `DestroyImmediate`）
//      ⇒ 挂到哪个宿主都安全（`BoosterPackOpenWindow` 自己没有 `Awake/OnEnable`，裸组件不会被初始化）。
using UnityEngine;

/// <summary>`B141-W28`：4-17「档案页 Avatar ↔ 开包窗」队列带的先后 / 分带 / 模态那半边。</summary>
public static class B141_W28_Assert
{
    public static int Pass;
    public static int Fail;

    /// <summary>改前那个同号值（4-17 抓到的就是它）—— **只当「灭自证」的反面样本**，⛔ 不当现状。</summary>
    const int OldQBase = 3170;
    /// <summary>本窗整带长度：`QShade` + 5 张卡 × 5 档（卡影SDF/卡背/卡面/角标/命中区）+ `QHint` + `QHintHit`。</summary>
    const int BandSize = 28;

    public static void Run()
    {
        Pass = 0; Fail = 0;
        Debug.Log("[B141_W28] ===== 4-17 开包窗 / 档案 Avatar 页 队列带 断言开始 =====");
        CheckSplit();
        CheckOriginalOrder();
        CheckBandShape();
        CheckTopBarAndNav();
        CheckAntiSelfProof();
        Debug.Log("[B141_W28] ===== 合计 pass=" + Pass + " fail=" + Fail + " =====");
    }

    // ---- 结构读口（全部走 public 常量，⛔ 不抄字面量）----

    static int QShade { get { return CardPresentation.BoosterPackOpenWindow.QShade; } }
    static int QCard  { get { return CardPresentation.BoosterPackOpenWindow.QCard; } }
    static int QHint  { get { return CardPresentation.BoosterPackOpenWindow.QHint; } }
    static int QTop   { get { return CardPresentation.BoosterPackOpenWindow.QHintHit; } }
    static int QProfileShade { get { return CardPresentation.PlayerProfileWindow.QShade; } }
    /// <summary>Avatar 页的页带起点 —— `AvatarTab.PageIndex = 1`（`ProfilePage.Q` = `QPageBase + PageIndex*10`）。</summary>
    static int QAvatarPage { get { return CardPresentation.PlayerProfileWindow.QPageBase + 1 * 10; } }

    /// <summary>「整带严格低于档案窗最低一层」—— ③ 的两条反面样本就是拿它判假的，所以它是**方向敏感**的判据。</summary>
    static bool BandBelowProfile(int qShade, int bandSize)
    {
        return qShade + bandSize - 1 < QProfileShade;
    }

    // ---- ① 分带 ----
    static void CheckSplit()
    {
        Check("①-A 两扇不再同号：`QShade`(" + QShade + ") ≠ Avatar 页 `Q`(" + QAvatarPage + ")",
              QShade != QAvatarPage);
        Check("①-B 开包窗整带 " + QShade + "–" + QTop + " 与 Avatar 页 `Q`(" + QAvatarPage + ") **不重合**",
              QTop < QAvatarPage || QShade > QAvatarPage);
        Check("①-C 开包窗整带 " + QShade + "–" + QTop + " 与 Avatar 格子带起点 3180 不重合（整带 < 3180）",
              QTop < 3180);
    }

    // ---- ② 原版先后 ----
    static void CheckOriginalOrder()
    {
        Check("②-A 原版先后：开包窗整带**严格低于**档案窗最低一层 `PlayerProfileWindow.QShade`(" + QProfileShade + ")",
              BandBelowProfile(QShade, BandSize));
        Check("②-B Avatar 页 `Q`(" + QAvatarPage + ") 在开包窗整带（" + QShade + "–" + QTop + "）**之上**"
              + "（原版 `PopUps` 排序层索引 7 > `Default` 索引 0）",
              QAvatarPage > QTop);
    }

    // ---- ④ 带内不变量 ----
    static void CheckBandShape()
    {
        Check("④-A `QShade`(" + QShade + ") < `QCard`(" + QCard + ")", QShade < QCard);
        Check("④-B `QHint`(" + QHint + ") < `QHintHit`(" + QTop + ")", QHint < QTop);
        Check("④-C 整带长度 = " + BandSize + "（现读 " + (QTop - QShade + 1) + "）", QTop - QShade + 1 == BandSize);
        Check("④-D 5 张卡 × 5 档全部落在 `QCard`…`QHint − 1` 内（末张末档 "
              + (QCard + 4 * 5 + 4) + " < " + QHint + "）",
              QCard + 4 * 5 + 4 < QHint);
    }

    // ---- ⑤ 顶栏之上 + 左栏那一半 ----
    static void CheckTopBarAndNav()
    {
        int barOverlay = CardPresentation.MainMenuRuntime.QBarOverlay;              // 2998
        int navHit = CardPresentation.MainMenuRuntime.QNavHitForTest;               // 3156
        int navBlocked = CardPresentation.MainMenuRuntime.QNavHitBlockedForTest;    // 3001

        Check("⑤-A 原版「在顶栏之上」：`QShade`(" + QShade + ") > 顶栏最高档 `QBarOverlay`(" + barOverlay + ")",
              QShade > barOverlay);
        Check("⑤-B 本带已降到左栏命中档 `QNavHit`(" + navHit + ") **之下** ⇒ 必须显式声明模态（下一条）",
              QShade < navHit);
        Check("⑤-C 本带仍 **>** 模态时的左栏命中档 `QNavHitBlocked`(" + navBlocked
              + ")（模态窗在场时点左栏仍落到本窗压暗层）",
              QShade > navBlocked);

        var go = new GameObject("B141_W28_Probe");
        try
        {
            var w = go.AddComponent<CardPresentation.BoosterPackOpenWindow>();
            Check("⑤-D 本窗 `BlocksLeftNav` = true（= 左栏点击落到「点哪儿都关」那块，与改档前逐字相同）",
                  w != null && w.BlocksLeftNav);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    // ---- ③ 灭自证（改回同号必红 / 抬到档案带之上必红）----
    static void CheckAntiSelfProof()
    {
        Check("③-A 灭自证：把 `QBase` 改回旧值 " + OldQBase + " ⇒ 「整带低于档案带」那条(②-A)必须判**假**",
              !BandBelowProfile(OldQBase, BandSize));
        Check("③-B 灭自证：把本窗抬到档案带之上（例 3400）⇒ 同一条(②-A)也必须判**假**",
              !BandBelowProfile(3400, BandSize));
        Check("③-C 灭自证：旧整带（" + OldQBase + "–" + (OldQBase + BandSize - 1) + "）下，"
              + "「Avatar 页 `Q` 在整带之上」那条(②-B)必须判**假**",
              !(QAvatarPage > OldQBase + BandSize - 1));
    }

    static void Check(string what, bool ok)
    {
        if (ok) { Pass++; Debug.Log("[B141_W28] ✓ " + what); }
        else { Fail++; Debug.LogError("[B141_W28] ✗ " + what); }
    }
}
