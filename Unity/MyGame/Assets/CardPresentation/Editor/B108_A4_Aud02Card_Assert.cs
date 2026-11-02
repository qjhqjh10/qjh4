// B108_A4_Aud02Card_Assert.cs — 第一百零八会话 · 波 1 · 执行代理 `B108_A4`
//
// ============================ 覆盖（逐条对上判据表）============================
//  A 段 `Run()`（自包含，只要一个空 GameObject）—— **AUD02 卡 UI 的 9 条 ❌**
//    （判据表 `资料/完全复刻/判据/AUD02_裁定_卡UI.tsv` 的 88 / 103 / 117 / 133 / 178 / 205 / 213 / 390 / 417）
//      A1  ★ 行 88 + 213：`NameTextUnit` 的 autosize 上界 —— 标准/商店页签 **1.90（19 pt）** · 开包族 **2.00（20 pt）**
//      A2  ★ 行 103：`DescTextUnit` 上界 —— 标准 **2.40（24 pt）** · 开包族 **1.90（19 pt）**
//      A3  ★ 行 133：`ArmyTextUnit`（单位卡阵营行）—— 开包族 / 标准 = **17/15**
//      A4  ★ 行 117：`RaceText`（单位卡兵种行）—— 开包族 / 标准 = **17/18**（方向相反！）
//      A5  ★ 行 178 + 390：`ArmyTextTactc`（战术卡阵营行）—— 两档**同值**（原版 `14 / max 15` ⇒ 有效 15）
//      A6  ★ 行 205 + 417：`New Card Badge` 走**九宫格**（原版 `m_Type = 1 (Sliced)`）⇒ 8 顶点 / 外接仍覆盖整块矩形
//      A7  ★ 行 88 的接线：开包宿主**不传 `use`** 时，靠原版节点名 `Booster Animation Parent` 自动判档
//    🔴 判据（一手，`bundle_menus_assets_all` 逐实例现读；本代理 2026-10-31 复核过一次）：
//      · 标准档：卡名 19/19 · 效果 23.55/24 · 兵种 18/18 · 阵营 15/15（战术卡阵营行 14/**15**）
//      · 开包族（6 棵 `CardUI` 树）：卡名 16.3/**20** · 效果 19/**19** · 兵种 17/**17** · 阵营 17/**17**
//      · 商店页签（`NameTextUnit` 16/**19**）：其余三行与标准档**同值** ⇒ 不单开一档（行 213 = 不是缺陷）
//      · `NameTextUnit No description` 29/29 都是 19/19 ⇒ 开包族那一支**不**吃 20
//    🔴 **有效字号看 `m_fontSizeMax`，名义 `m_fontSize` 只是 TMP autosize 的起点**
//      （`TextMeshPro.cs:2151` 起点 = clamp(base,min,max)；`:4139` 一路涨到 max）。
//
//  B 段 `RunCollectionCloseScrolls(CollectionWindow win)` —— **AUD03 收藏窗的 ❌ 行 #25**
//      B1 ★ 关窗要把**本窗的滚动区登记**撤掉（原来只撤拖拽 ⇒ 每次开/关净涨 4～6 条）；
//      B2 ★ 开→关重复 3 次 ⇒ 关窗后的登记条数**不随次数增长**。
//    判据：原版关窗 = `Object.Destroy`（`WindowsManager.CloseWindowCO`）⇒ 再开是**新实例**；
//    而 `PointerLayer.PruneScrolls` 只按 `Owner == null` 收尸（页节点一直活着 ⇒ 收不到）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  A1 把 `NameUnitBandMaxForUse` 改回恒 `NameUnitBandMax` ⇒ A1 红。
//  A2 把 `DescUnitBandMaxForUse` 改回恒 `DescUnitBandMax` ⇒ A2 红。
//  A3/A4 把 `ArmyFontSizeForUse` / `RaceFontSizeForUse` 改回静态常量 ⇒ 比值回 1 ⇒ 红；
//        而且这两条**方向相反**（17>15 vs 17<18）⇒ 不可能靠「整体放大/缩小」同时满足。
//  A5 把战术卡那一支也套 0.17（或按名义 fs 14 改）⇒ A5 红。
//  A6 把 `SlicedBadgeQuad` 换回 `SpriteQuad` ⇒ 4 顶点 ⇒ A6 红。
//  A7 删掉 `IsBoosterCardView`（`v._use = use`）⇒ A7 红。
//  B1 把 `Close()` 里那句 `UnregisterOwnedScrolls()` 删掉 ⇒ B1 红（开/关前后条数不变）。
//  B2 只做一次「撤窗根」那种写法（`UnregisterOwnedBy(gameObject)`）⇒ 页节点 Owner 一条也撤不掉 ⇒ B1/B2 红。
//
// ============================ 怎么接（⚠️ 本代理不动断言宿主）============================
//  · A 段（自包含）：任一宿主的聚合行、`//` 之前同样式追加即可（⚠️ 先看那一行第一个 `//`）：
//        B108A4_Assert.Run(); pass += B108A4_Assert.Pass; fail += B108A4_Assert.Fail;
//    （卡面共用件相关，落在 `Editor/CardBaseDemo.cs` / `Editor/BattleScene.cs` 那一族最合适；
//      `B106W9_Assert.Run()` 已接在 `Editor/CardBaseDemo.cs:392`，两者互不冲突。）
//  · B 段（要一扇**已建好的收藏窗**）：建议接在 **`Editor/CollectionScene.cs`** 收尾那一带
//    （与 `B103R1_Assert.RunCollectionClose(win)` 同一处；⚠️ 它会在 3 轮里反复 Build/Close ⇒ 放在最后）：
//        B108A4_Assert.RunCollectionCloseScrolls(win); pass += B108A4_Assert.Pass; fail += B108A4_Assert.Fail;
//
// ============================ 边界（如实记）============================
//  · A 段只验「我们按原版哪一档落地」，⛔ 不验「TMP 探针那一刻的真实字号」（要真 TMP 探针 = Unity 批处理，
//    本代理禁跑）。残留不确定性见 `资料/完全复刻/报告/B108_A4.md` §没查清。
//  · B 段只验登记表**条数**；不验「旧条目能不能被滚轮命中」（要 `PointerLayer.Update` 的帧循环）。
//  · 本文件只建临时 GameObject / 只读登记表，**不写盘**。
using System;
using CardPresentation;
using UnityEngine;
using Object = UnityEngine.Object;   // ⚠️ `using System;` 与 `UnityEngine` 同时在场 ⇒ 裸 `Object` 二义（CS0104）；这里钉成 Unity 的

/// <summary>执行代理 `B108_A4`：AUD02 卡 UI 9 条 ❌（A）+ AUD03 收藏窗 #25（B）。</summary>
public static class B108A4_Assert
{
    static readonly CheckSink _sink = new CheckSink("[A4] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Near(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ==========================================================================================
    //  A 段 —— AUD02 卡 UI 的 9 条 ❌
    // ==========================================================================================
    /// <summary>自包含：建两张夹具卡（标准档 / 开包族）逐条对字号档，再验角标九宫格与宿主自动识别。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "B108_A4 · AUD02 卡 UI 9 条 ❌（88/103/117/133/178/205/213/390/417）");
        try { A_FontAndBadge(); }
        catch (Exception e) { Tr(false, "「AUD02 卡 UI」这一组抛异常（后面的条目没跑完）：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "B108A4(AUD02) 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    static void A_FontAndBadge()
    {
        // 前提硬检查（⛔ 不用 `if (x != null) Check(...)` —— 前提不成立时会静默跳过）
        Tr(TmpFont.Available, "A0 **前提**：TMP 字体资产可用（`TmpFont.Available`）；不可用 ⇒ 文字层根本不建，下面全是空断");
        if (!TmpFont.Available) return;

        var root = new GameObject("B108A4_ROOT");
        CardView std = null, boost = null, tacticStd = null, tacticBoost = null, badge = null;
        try
        {
            var unit = UnitData();
            var tactic = TacticData();
            std = CardView.Create(root.transform, unit, "B108A4_std");
            boost = CardView.Create(root.transform, unit, "B108A4_boost", CardFace.Full, null, CardUse.Booster);
            tacticStd = CardView.Create(root.transform, tactic, "B108A4_tactic_std");
            tacticBoost = CardView.Create(root.transform, tactic, "B108A4_tactic_boost", CardFace.Full, null, CardUse.Booster);
            Tr(std != null && boost != null && tacticStd != null && tacticBoost != null,
               "A0-a **前提**：四张夹具卡都建出来了");
            if (std == null || boost == null || tacticStd == null || tacticBoost == null) return;

            // ---- 行 88 + 213：卡名上界 ----
            Near(std.NameBandMaxForTest, 1.425f, 1e-3f,
                 "A1 ★ 行 213 标准档 / 商店页签的卡名 autosize 上界 = 1.425（原版 `NameTextUnit` 19 pt ÷10 × shipped 实拍 0.75；`B117_F14`，判据 → `CardView.TitleEmOfCard`）；"
               + "商店页签那 5 个实例虽 `m_fontSize = 16`，但 `m_fontSizeMax = 19` ⇒ 有效字号同标准档 ⇒ 不单开一档）");
            Near(boost.NameBandMaxForTest, 1.500f, 1e-3f,
                 "A1b ★ 行 88 开包族卡名上界 = 1.500（原版开包实例 `16.3 / max 20` × 0.75）"
               + "｜🧨 把 `NameUnitBandMaxForUse` 改回恒 `NameUnitBandMax` ⇒ 本行红");

            // ---- 行 103：效果文字上界 ----
            Near(std.DescBandMaxForTest, 1.872f, 1e-3f,
                 "A2 ★ 行 103 标准档效果文字上界 = 1.872（原版 `DescTextUnit` 23.55 / max 24 × 实拍 0.78；`B117_F14`）");
            Near(boost.DescBandMaxForTest, 1.482f, 1e-3f,
                 "A2b ★ 行 103 开包族效果文字上界 = 1.482（原版开包实例 `19 / max 19` × 0.78）"
               + "｜🧨 把 `DescUnitBandMaxForUse` 改回恒 `DescUnitBandMax` ⇒ 本行红");

            // ---- 行 133 / 117：阵营行 / 兵种行（两条**方向相反**）----
            float armyRatio = boost.ArmyFontSizeForTest(true) / Mathf.Max(1e-6f, std.ArmyFontSizeForTest(true));
            Near(armyRatio, 17f / 15f, 2e-3f,
                 "A3 ★ 行 133 开包族 / 标准档 **单位卡阵营行**字号比 = 17/15（原版 `ArmyTextUnit` 开包 `17/17` ↔ 标准 `15/15`）"
               + "｜🧨 把 `ArmyFontSizeForUse` 改回静态常量 ⇒ 比值回 1 ⇒ 红");
            float raceRatio = boost.RaceFontSizeForTest(true) / Mathf.Max(1e-6f, std.RaceFontSizeForTest(true));
            Near(raceRatio, 17f / 18f, 2e-3f,
                 "A4 ★ 行 117 开包族 / 标准档 **单位卡兵种行**字号比 = 17/18（原版 `RaceText` 开包 `17/17` ↔ 标准 `18/18`，**变小**）"
               + "｜🧨 与 A3 方向相反 ⇒ 不可能靠「整体放大/缩小」同时满足");

            // ---- 行 178 + 390：战术卡阵营行两档同值 ----
            Near(tacticStd.ArmyFontSizeForTest(false), std.ArmyFontSizeForTest(true), 1e-4f,
                 "A5 ★ 行 178/390 标准档：战术卡阵营行 = 单位卡阵营行 0.15（原版 `ArmyTextTactc` 41/41 `14 / max 15`"
               + " ⇒ 有效上界 15；⚠️ 名义 14 只是 autosize 起点，⛔ 别照字面改成 0.14）");
            Near(tacticBoost.ArmyFontSizeForTest(false), std.ArmyFontSizeForTest(true), 1e-4f,
                 "A5b ★ 行 178/390 开包族**战术卡**阵营行 = 标准档 0.15（两棵 prefab 的 `ArmyTextTactc` 都是 `14 / max 15`）"
               + "｜🧨 让战术卡也吃开包族 0.17 ⇒ 本行红");
            Near(tacticBoost.RaceFontSizeForTest(false), std.RaceFontSizeForTest(true), 1e-4f,
                 "A5c 开包族**战术卡**兵种行 = 标准档 0.18（`Tactic Container/RaceText` 两棵 prefab 都是 18/18）");

            // ---- 行 205 + 417：新卡角标 = Sliced 九宫格 ----
            badge = CardView.Create(root.transform, unit, "B108A4_badge");
            Tr(badge != null, "A6-前提①：角标夹具卡建出来了");
            if (badge != null)
            {
                badge.ToggleNewCardBadge(true);
                Tr(badge.NewCardBadgeBuilt,
                   "A6-前提②：角标那一层建出来了（图 `Resources/Art/ui_menu/WF_Special_offer_Value.png` 取得到）");
                if (badge.NewCardBadgeBuilt)
                {
                    C(CardView.NewBadgeMeshVertexCountForTest(), 8,
                      "A6 ★ 行 205/417 角标网格 = **2 块 × 4 顶点 = 8**（原版 Image `m_Type = 1 (Sliced)`、sprite border 左右各 162px，"
                    + "矩形只有 1.2101 ⇒ `GetAdjustedBorders` 把两边缩到刚好装下、中段宽 0 ⇒ `Image.cs:1199` 跳过那 3 块）"
                    + "｜🧨 换回 `SpriteQuad`（整图拉伸）⇒ 4 顶点 ⇒ 红");
                    float s = CardView.Width / CardView.CardUnitW;
                    var want = new Vector2(CardView.NewBadgeW * s, CardView.NewBadgeH * s);
                    var got = CardView.NewBadgeMeshSizeForTest();
                    Near(got.x, want.x, 1e-3f,
                         "A6b ★ 行 205/417 角标网格外接宽 = 原矩形（缩边后两块拼起来仍覆盖整块矩形）｜🧨 边宽算式方向写反 ⇒ 红");
                    Near(got.y, want.y, 1e-3f, "A6c 角标网格外接高 = 原矩形");
                }
            }

            // ---- 行 88 的接线：开包宿主不传 `use`，按原版节点名自动判档 ----
            var bp = new GameObject("Booster Animation Parent");
            bp.transform.SetParent(root.transform, false);
            var cardsNode = new GameObject("Cards");
            cardsNode.transform.SetParent(bp.transform, false);
            var inCard = new GameObject("CardInBoosterPack UI 1");
            inCard.transform.SetParent(cardsNode.transform, false);
            var inferred = CardView.Create(inCard.transform, unit, "CardUI");
            Tr(inferred != null, "A7-前提①：自动判档那张卡建出来了");
            if (inferred != null)
            {
                Tr(inferred.Use == CardUse.Booster,
                   "A7 ★ 行 88 挂在 `Booster Animation Parent/Cards/…` 下、**不传 `use`** 时自动落到 `CardUse.Booster`"
                 + "（原版宿主节点路径 = `Booster Pack Open Window / Booster Animation Parent / Cards / CardInBoosterPack UI 1..5`）"
                 + "｜🧨 删掉 `IsBoosterCardView`（改回 `v._use = use`）⇒ 本行红");
                Object.DestroyImmediate(inferred.gameObject);
            }
            Object.DestroyImmediate(bp);
        }
        finally
        {
            if (std != null) Object.DestroyImmediate(std.gameObject);
            if (boost != null) Object.DestroyImmediate(boost.gameObject);
            if (tacticStd != null) Object.DestroyImmediate(tacticStd.gameObject);
            if (tacticBoost != null) Object.DestroyImmediate(tacticBoost.gameObject);
            if (badge != null) Object.DestroyImmediate(badge.gameObject);
            Object.DestroyImmediate(root);
        }
    }

    // ==========================================================================================
    //  B 段 —— AUD03 收藏窗 #25：关窗撤滚动区登记
    // ==========================================================================================
    /// <summary>#25：关窗要把本窗的滚动区登记撤掉；开→关重复 3 次，关窗后的条数不随次数增长。
    /// <para>⚠️ **会在 3 轮里反复 `Build()` / `Close()`** ⇒ 由调度台在收尾那一带调（同 `B103R1_Assert.RunCollectionClose`）。</para></summary>
    public static void RunCollectionCloseScrolls(CollectionWindow win)
    {
        MenuCheck.Section(_sink, "B108_A4 · AUD03 #25（关窗撤滚动区登记 / 反复开关不增长）");
        try { B_CloseUnregistersScrolls(win); }
        catch (Exception e) { Tr(false, "「#25 关窗撤滚动登记」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "B108A4(AUD03) 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    static void B_CloseUnregistersScrolls(CollectionWindow win)
    {
        Tr(win != null, "B0-前提①：收藏窗在（为 null ⇒ 本条无意义）");
        if (win == null) return;
        Tr(win.gameObject.activeSelf, "B0-前提②：收藏窗是**开着**的（关窗断言必须先有一个开着的窗）");
        Tr(PointerLayer.Instance != null, "B0-前提③：`PointerLayer.Instance` 在（不在 ⇒ `ScrollCountForTest` 恒 0，断言无意义）");
        if (!win.gameObject.activeSelf || PointerLayer.Instance == null) return;

        // 四页都建出来 ⇒ 四条主页滚动区都登记过（`PageRoot` 触发懒建/SetupOnce，与生产路径同形）。
        for (int p = 0; p <= 3; p++)
            Tr(win.PageRoot(p) != null, "B0-前提④：第 " + p + " 页建出来了（`PageRoot(" + p + ")`）");

        int open1 = PointerLayer.ScrollCountForTest;
        Tr(open1 > 0, "B0-前提⑤：关窗前登记表非空（" + open1 + " 条）");

        win.Close();
        int closed1 = PointerLayer.ScrollCountForTest;
        Tr(closed1 < open1,
           "B1 ★ #25 关窗把本窗的滚动区登记**撤掉了**（" + open1 + " → " + closed1 + " 条）"
         + "｜🧨 删掉 `Close()` 里那句 `UnregisterOwnedScrolls()` ⇒ 开/关前后条数不变 ⇒ 本行红"
         + "（原版关窗 = `Object.Destroy` ⇒ 旧 `MenuScroll` 一条都不该留在登记表里）");
        Tr(closed1 >= 0, "B1b-记录：关窗后登记表剩 " + closed1 + " 条（别的窗/别的宿主留下的，本窗不应再占一条）");

        int baseline = closed1;
        for (int cycle = 2; cycle <= 3; cycle++)
        {
            // 再开一轮：`Build()` 会重建整窗并重新 `RegisterScroll`（原版这一拍是**新实例**，我们复用同一实例）
            win.Build();
            for (int p = 0; p <= 3; p++) win.PageRoot(p);
            int reopened = PointerLayer.ScrollCountForTest;
            Tr(reopened > baseline,
               "B2-前提：第 " + cycle + " 轮 `Build()` 之后登记表又长回去（" + baseline + " → " + reopened + " 条）"
             + "；没长回去 ⇒ 下面那条是空断（`Build()` 没把页建起来）");

            win.Close();
            int closedN = PointerLayer.ScrollCountForTest;
            C(closedN, baseline,
              "B3 ★ #25 第 " + cycle + " 轮开→关之后登记表**不随次数增长**（关窗后 " + closedN
            + " 条 / 首轮关窗后 " + baseline + " 条）｜🧨 关窗不撤滚动区 ⇒ 每轮净涨 4～6 条 ⇒ 本行红"
            + "；🧨 只写 `UnregisterOwnedBy(gameObject)`（撤窗根）⇒ 页节点 Owner 一条也撤不掉 ⇒ 本行红");
        }
    }

    // ==========================================================================================
    //  夹具
    // ==========================================================================================
    /// <summary>单位卡夹具：阵营 + 兵种都填满（否则阵营行 / 兵种行不建，字号断言等于没量）。</summary>
    static CardData UnitData()
    {
        var d = CardData.Simple("B108A4 Unit", 3, 3, 4);
        d.type = "unit";          // ⚠️ `Simple` 的 `faction` 恒 null（本仓已知坑）⇒ 下面显式填
        d.faction = "Ultramarines";
        d.subtype = "Infantry";
        return d;
    }

    /// <summary>战术卡夹具（只量阵营行 / 兵种行字号；`isUnit = false` 走 `Tactic Container` 那一套节点）。</summary>
    static CardData TacticData()
    {
        var d = CardData.Simple("B108A4 Tactic", 2, 0, 0);
        d.isUnit = false;
        d.type = "tactic";
        d.faction = "Ultramarines";
        d.subtype = "Infantry";
        return d;
    }
}
