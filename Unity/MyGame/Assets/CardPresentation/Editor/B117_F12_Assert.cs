// B117_F12_Assert.cs — 两条账的断言：`B117-30`（详情窗督军血量）与 `B117-R19`（模式窗战将立绘几何）
//   · 执行代理 `F12` · 2026-11-02
//
// ============================ 怎么跑（两个宿主，两段分开接）============================
//   ① **`Run()`** —— 血量那一段要**真窗**（`CardDisplayWindow.Create` 依赖 `LayoutSpace.Cam` / `HudCanvas`，
//      只有战斗宿主立得起来）⇒ **宿主 = `Editor/BattleScene.cs` 的聚合行**
//      （那一条 `Bxxx_Assert.Run(); pass += …; fail += …;` 的同一物理行尾、`Debug.Log(合计)` 之前追加）。
//   ② **`RunModeWindow()`** —— 立绘几何那一段**是纯常量算术、不建窗** ⇒ 可接在任何宿主上；
//      **推荐宿主 = `Editor/MainMenuScene.cs`**（模式窗那一族本来就归菜单场景）。
//
// ============================ 判据（一手，逐条现读）============================
//   B117-30（血量）
//     · `d:/2/tools/decomp_full/CardDisplayWindow__ShowCard.c` 尾段：
//       `InitializeCardForDisplay(this, card, 0, tier, warlordHealthModifier, 0)` —— 第 5 个实参 = 模式增量。
//     · `CardDisplayWindow__InitializeCardForDisplay.c` —— 那个增量**只发给 index 0**；
//       `CardDisplayWindow__InitializeRelatedCards.c` 给相关卡那一档第 5 个实参恒 `0`。
//     · `BasicCardUI__SetRawCardData.c` —— `if (card.cardType(+0x94) != 10 /*Hero*/) modifier = 0;`
//       `SetText(+0xa0, modifier + maxHealth ×3)` ⇒ **三档同值**（当前/基准/上限），卡面不着色。
//     · 调用方示例：`DeckInfoPopup__OnWarlordClick.c:24-34`（传 `gameplayVariables.warlordLifeChange`）。
//   B117-R19（立绘几何）
//     · `D:/4/_tmp_view/orig_ui_proj/Assets/SkirmishModeEventWindow.prefab:2315-2335`（`Warlod Image` 的 RectTransform）
//       与 `…/RankedEventWindowV2.prefab` 的同名件 —— **两份逐值相同**：
//       `m_AnchorMin/Max = (0.5, 0.5)` · `m_Pivot = (0.5, 0.5)` · `m_AnchoredPosition = (40, 86)` ·
//       `m_SizeDelta = (1098.144, 1098.144)` · 组件 `m_PreserveAspect = 0` · `m_Sprite = {fileID: 0}`（运行时喂）。
//     · 子件 `Warlord Darkening`：`m_AnchoredPosition = (0, −243)` · `m_SizeDelta = (940.053, 557.644)` ·
//       `m_Color = (0,0,0,0.8156863)` · `m_Type = 1`（Sliced）· `m_PixelsPerUnitMultiplier = 0.04`。
//     ⇒ 相对窗口（1920×1080）解算：中心 `(960+40, 540−86) = (1000, 454)` · 边长 **1098.144**
//       ⇒ 绝对矩形 `(450.928, −95.072) – (1549.072, 1003.072)`（= `LiveOpsEventWindow.Wl*` 那四个常量）。
//
// ⚠️ **本文件不复刻「立绘怎么画」**（`keepAspect` 那一档）：原版 `m_PreserveAspect = 0`，
//    而我们的主档喂的是 **2048² 方形整身立绘** ⇒ 拉满 ≡ 等比，两种写法同解
//    （判据与取舍已写在 `LiveOpsEventWindow.BuildDeckSelection` 那一段注释里）。
//    这里只钉**几何**（矩形 / 相对窗口的比例），那是原版 prefab 上一字不差的四个数。
using System;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static class B117F12_Assert
{
    const string P = "[B117_F12] ";

    static int _ok, _bad;
    /// <summary>宿主聚合行用的读口（否则失败不会让宿主变红 = 静默失败）。</summary>
    public static int Pass { get { return _ok; } }
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(P + "✓ " + what); }
        else { _bad++; Debug.LogError(P + "✗ " + what); }
    }

    static string ReadSrc(string rel)
    {
        try
        {
            string p = Path.Combine(Application.dataPath, rel.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(p) ? File.ReadAllText(p) : null;
        }
        catch (Exception ex) { Debug.LogWarning(P + "读源码失败 " + rel + "：" + ex.Message); return null; }
    }

    // ================================================================================================
    //  B117-30 · 详情窗督军血量 = SO + 本局模式的 `warlordLifeChange`
    //  ⚠️ 宿主必须能建真窗（`Editor/BattleScene.cs`）。
    // ================================================================================================
    public static void Run()
    {
        Debug.Log(P + "--- B117-30：详情窗督军血量 = SO + 模式增量（`warlordHealthModifier`）---");
        SectionHealth();
        Debug.Log(P + "B117-30 合计：" + _ok + " 通过 / " + _bad + " 失败");
    }

    static System.Collections.Generic.List<CardDef> _pool;
    static System.Collections.Generic.List<CardDef> Pool
    {
        get { if (_pool == null) _pool = CardDatabase.Load(); return _pool; }
    }

    static CardDef FirstHero()
    {
        foreach (var c in Pool) if (c != null && c.Type == "hero" && c.Health > 0) return c;
        return null;
    }

    static int HealthShown(CardDisplayWindow win)
    {
        var v = win != null ? win.Card : null;
        if (v == null) return int.MinValue;
        string t = v.FaceStatTextNow(CardView.StatHealth);
        int n;
        return int.TryParse(t, out n) ? n : int.MinValue;
    }

    static void SectionHealth()
    {
        // ---- 前提（⛔ 不静默跳过：前提不成立就红，而不是「看起来绿」）----
        var hero = FirstHero();
        Check(hero != null, "（前提）卡池里找得到一个督军（`CardDef.Type == \"hero\"` 且 `Health > 0`）");
        var troop = CardDatabase.Find(Pool, "Deathwing Terminator");
        Check(troop != null, "（前提）卡池里找得到 `Deathwing Terminator`（非督军的对照组）");
        if (hero == null || troop == null) return;

        CardDisplayWindow win = null;
        try { win = CardDisplayWindow.Create(null); }
        catch (Exception ex) { Debug.LogWarning(P + "建临时展示窗抛了：" + ex); win = null; }
        Check(win != null, "（前提）临时展示窗建起来了（`CardDisplayWindow.Create(null)`）—— 宿主必须是 `Editor/BattleScene.cs`；"
                         + "不成立 ⇒ 下面 ①②③④ 一条都没跑（这一条红，不许静默跳过）");
        if (win == null) return;

        int so = hero.Health;
        try
        {
            // ---- ① 没喂增量 ⇒ 显示 SO 原值（改前就是这样，也是「收藏窗那一档」的语义）----
            CardDisplayWindow.SetWarlordLifeChange(0);
            win.Show(BattleDriver.ToCardData(hero, hero.Faction), hero, null);
            int h0 = HealthShown(win);
            Check(h0 == so, $"① 没喂增量 ⇒ 详情窗督军血量 = SO 原值 {so}（实得 {h0}）");

            // ---- ② 经典档 +10 --------------------------------------------------------------
            CardDisplayWindow.SetWarlordLifeChange(10);
            win.Show(BattleDriver.ToCardData(hero, hero.Faction), hero, null);
            int h1 = HealthShown(win);
            Check(h1 == so + 10, $"② 喂 +10（经典档 `ClassicWarlordLifeChange`）⇒ 详情窗 = SO+10 = {so + 10}（实得 {h1}）");

            // ---- ③ 负增量照样成立（判别式：把加法写成「恒 +10」或「写死绝对值」时这条红）----
            CardDisplayWindow.SetWarlordLifeChange(-5);
            win.Show(BattleDriver.ToCardData(hero, hero.Faction), hero, null);
            int h2 = HealthShown(win);
            Check(h2 == so - 5, $"③ 喂 −5 ⇒ 详情窗 = SO−5 = {so - 5}（实得 {h2}）—— 判别式：写死 +10 时这里会变 {so + 10}");

            // ---- ④ 灭自证 / 语义边界：增量**只作用于督军**（原版 `cardType != Hero ⇒ modifier = 0`）----
            CardDisplayWindow.SetWarlordLifeChange(10);
            win.Show(BattleDriver.ToCardData(troop, troop.Faction), troop, null);
            int ht = HealthShown(win);
            Check(ht == troop.Health, $"④ 同一份 +10 喂给**非督军**（`{troop.Name}`）⇒ 血量仍是 SO {troop.Health}（实得 {ht}）"
                                    + " —— 改坏法：把增量无条件加到每张卡上 ⇒ 本条红");

            // ---- ⑤ 收藏窗 / 菜单详情窗不经过本类（源码级，防「两边一起改」）-------------------
            var popupSrc = ReadSrc("CardPresentation/Shell/CardDetailPopup.cs");
            Check(popupSrc != null,
                  "⑤（前提）读得到 `Shell/CardDetailPopup.cs`（收藏/菜单详情窗）");
            if (popupSrc != null)
            {
                Check(popupSrc.IndexOf("SetWarlordLifeChange", StringComparison.Ordinal) < 0
                   && popupSrc.IndexOf("WarlordLifeChangeForTest", StringComparison.Ordinal) < 0,
                      "⑤ 收藏窗（`Shell/CardDetailPopup.cs`）**不引用**本口 ⇒ 非局内仍显示 SO 原值"
                    + "（改坏法：在那里也调一次 `SetWarlordLifeChange` ⇒ 本条红）");
            }
        }
        finally
        {
            CardDisplayWindow.SetWarlordLifeChange(0);   // 收尾复位（别把增量漏给后续断言）
            try { win.Hide(); } catch (Exception) { }
            try { UnityEngine.Object.DestroyImmediate(win.gameObject); } catch (Exception) { }
        }
    }

    // ================================================================================================
    //  B117-R19 · 模式窗（小冲突 / 经典 / 排位共用 `LiveOpsEventWindow`）的战将立绘几何
    //  纯常量算术 —— 不建窗、不需要 Unity 渲染。
    // ================================================================================================
    public static void RunModeWindow()
    {
        Debug.Log(P + "--- B117-R19：模式窗战将立绘层几何（钉原版 prefab 的四个数 + 相对窗口比例）---");
        SectionPortraitGeometry();
        Debug.Log(P + "B117-R19 合计：" + _ok + " 通过 / " + _bad + " 失败");
    }

    /// <summary>窗口设计尺寸（原版 `Canvas` = 1920×1080）。</summary>
    const float WinW = 1920f, WinH = 1080f;
    /// <summary>原版 `Warlod Image` 的 `m_SizeDelta`（两份 prefab 逐值相同）。</summary>
    const float OrigSize = 1098.144f;
    /// <summary>原版 `m_AnchoredPosition`（Unity y **向上**为正）。</summary>
    const float OrigOffX = 40f, OrigOffY = 86f;

    static bool Near(float a, float b, float eps) { return Mathf.Abs(a - b) <= eps; }

    static void SectionPortraitGeometry()
    {
        float cxl = WinW * 0.5f + OrigOffX;          // 1000.0   —— 矩形中心 x（屏 px，y 向下）
        float cyl = WinH * 0.5f - OrigOffY;          // 454.0    —— 矩形中心 y
        float x1 = cxl - OrigSize * 0.5f;            // 450.928
        float x2 = cxl + OrigSize * 0.5f;            // 1549.072
        float y1 = cyl - OrigSize * 0.5f;            // -95.072
        float y2 = cyl + OrigSize * 0.5f;            // 1003.072

        // ---- ① 四个矩形值（= `LiveOpsEventWindow.Wl*`；改回旧/错几何 ⇒ 这里红）------------------
        Check(Near(LiveOpsEventWindow.WlL, x1, 0.01f) && Near(LiveOpsEventWindow.WlR, x2, 0.01f)
           && Near(LiveOpsEventWindow.WlT, y1, 0.01f) && Near(LiveOpsEventWindow.WlB, y2, 0.01f),
              $"① 立绘层矩形 = 原版 prefab 解算值 ({x1:F2},{y1:F2})-({x2:F2},{y2:F2})"
            + $"（实得 ({LiveOpsEventWindow.WlL:F2},{LiveOpsEventWindow.WlT:F2})-"
            + $"({LiveOpsEventWindow.WlR:F2},{LiveOpsEventWindow.WlB:F2})）");

        // ---- ② 边长 1098.144（宽高相等 = 正方形槽）---------------------------------------------
        Check(Near(LiveOpsEventWindow.WlR - LiveOpsEventWindow.WlL, OrigSize, 0.01f)
           && Near(LiveOpsEventWindow.WlB - LiveOpsEventWindow.WlT, OrigSize, 0.01f),
              $"② 立绘槽是 {OrigSize:F3}² 的正方形（实得 {LiveOpsEventWindow.WlR - LiveOpsEventWindow.WlL:F2} × "
            + $"{LiveOpsEventWindow.WlB - LiveOpsEventWindow.WlT:F2}）");

        // ---- ③ 高度 / 窗口高度 = 1098.144 / 1080 = 1.0168（±2%）-------------------------------
        float hRatio = (LiveOpsEventWindow.WlB - LiveOpsEventWindow.WlT) / WinH;
        float wRatio = (LiveOpsEventWindow.WlR - LiveOpsEventWindow.WlL) / WinW;
        Check(Near(hRatio, OrigSize / WinH, 0.02f),
              $"③ 立绘层高度 / 窗口高度 = {hRatio:F4}（原版 {OrigSize / WinH:F4}，容差 ±2%）");
        Check(Near(wRatio, OrigSize / WinW, 0.02f),
              $"③-b 立绘层宽度 / 窗口宽度 = {wRatio:F4}（原版 {OrigSize / WinW:F4}，容差 ±2%）");

        // ---- ④ 上沿必须在窗口 10% 处或更高（= 可以是负数，越出屏幕顶也算「够高」）-------------
        float topRatio = LiveOpsEventWindow.WlT / WinH;                       // -0.0880
        Check(topRatio <= 0.10f,
              $"④ 立绘上沿 = 窗口 {topRatio * 100f:F1}% 处（原版 {y1 / WinH * 100f:F1}%；要求 ≤ 10%）"
            + " —— 判别式：把上沿放到「中下一条」（例 16% 那档）⇒ 本条红");

        // ---- ⑤ `Warlord Darkening` 子件（相对父件中心的偏移与尺寸）-----------------------------
        float dCx = (LiveOpsEventWindow.WlDarkL + LiveOpsEventWindow.WlDarkR) * 0.5f;   // 1000.0
        float dCy = (LiveOpsEventWindow.WlDarkT + LiveOpsEventWindow.WlDarkB) * 0.5f;   // 696.99
        Check(Near(dCx - cxl, 0f, 0.05f) && Near(dCy - cyl, 243f, 0.05f),
              $"⑤ 压暗层中心相对立绘中心 = (0, +243px 屏向 / 原版 m_AnchoredPosition (0,−243))"
            + $"（实得 ({dCx - cxl:F2}, {dCy - cyl:F2})）");
        Check(Near(LiveOpsEventWindow.WlDarkR - LiveOpsEventWindow.WlDarkL, 940.053f, 0.02f)
           && Near(LiveOpsEventWindow.WlDarkB - LiveOpsEventWindow.WlDarkT, 557.644f, 0.02f),
              $"⑤-b 压暗层 940.053 × 557.644（实得 {LiveOpsEventWindow.WlDarkR - LiveOpsEventWindow.WlDarkL:F3} × "
            + $"{LiveOpsEventWindow.WlDarkB - LiveOpsEventWindow.WlDarkT:F3}）");

        // ---- ⑥ 空态徽记**不是**立绘位（两件事，别混）：它有自己那 59.2px 的下移 ----------------
        Check(Near(LiveOpsEventWindow.FacIconR - LiveOpsEventWindow.FacIconL,
                   LiveOpsEventWindow.FacIconB - LiveOpsEventWindow.FacIconT, 0.01f),
              "⑥ 空态 `Faction Icon Image` 也是正方形槽（522.63²）—— 与立绘位**互不替代**"
            + "（没有卡组时画的是它；有了卡组才画立绘）");
    }
}
