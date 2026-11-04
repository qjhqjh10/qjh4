// B141_W54_Assert.cs — 执行写手 `B141-W54`（第一百二十八会话 · 2026-11-04）· W129 **5-17 页签悬停图**
//
// ============================ 它盯的是什么 ============================
//  原版 `bundle_menus_assets_all` 里这三扇窗的页签是 `EverguildToggle`：
//    · `offSprite`（常态）  = `40K_settings_button`（整扇窗的首颗 = `_hover`，因为它是「默认选中」那一颗）
//    · `onSprite`（选中态） = `40K_settings_button_hover`      ← 我们的 `ArtTabOn` / `ArtTabBgSel`
//    · `m_SpriteState.m_HighlightedSprite`（**悬停**）= **`40K_settings_button_selected`**  ← 本文件盯的就是它
//  改前三个生成器都**没传** `hoverArt`（靠 `WindowButton.HoverNames` 全局表隐式兜底）；本批改成**逐站显式传**。
//
//  判据（2026-11-04 逐颗现读，`d:/2/tools/assetripper_out/ExportedProject/Assets/*.prefab`
//  的 `m_SpriteState.m_HighlightedSprite` → sprite guid → `Sprite/*.asset.meta` 反查切片名）：
//
//  | prefab | 页签节点 | 颗数 | 常态图 | 悬停图（本批显式传的） | 选中图 |
//  |---|---|---:|---|---|---|
//  | `Main Menu Settings Window` | `Menu Area/Mask Tabs buttons/Tab Buttons/*`（General 首颗常态 = `_hover`） | 5 | `40K_settings_button` | **`40K_settings_button_selected`** | `40K_settings_button_hover` |
//  | `Player Profile Window` | `Menu Area/Tab Buttons/*`（Profile/Avatar/Title/Battle Log/Trophies/Ranked） | 6 | `40K_settings_button` | **`40K_settings_button_selected`** | `40K_settings_button_hover` |
//  | `RankedSkirmishLeaderboardPopup` | `Tab Buttons/*`（Player/Armies/Alliances） | 3 | `40K_settings_button_hover` | **`40K_settings_button_selected`** | `40K_settings_button_hover` |
//  | `RankedClassicLeaderboardPopup Variant` | `Tab Buttons/*`（Player/Armies） | 2 | 同上 | **`40K_settings_button_selected`** | 同上 |
//  | `DraftLeaderboardPopup` | `Tab Buttons/*`（Armies/Alliances） | 2 | 同上 | **`40K_settings_button_selected`** | 同上 |
//
//  ⇒ **18 颗页签（5+6+3+2+2），悬停图 18/18 = `40K_settings_button_selected`**（`Ranked` 那颗按用户口径隐藏，但绑定仍在，
//  本批同样显式传；实际生效 17 颗）。
//
// ============================ 断言 ============================
//  A 组（逐站接线，零前提 —— 只读 3 份 `.cs` 文本）：
//    A1  `SettingsWindow.ArtTabHover = "40K_settings_button_selected"`（且 `ArtTabBgSel` 仍是 `_hover` = 选中图）
//    A2  `SettingsWindow` 5 颗页签的 `Hit(...)` **显式**传 `hoverArt: ArtTabHover`
//    A3  `PlayerProfileWindow.ArtTabHover` + 6 颗页签的 `Bind(...)` 第三格 = `ArtTabHover`
//    A4  `LeaderboardWindow.ArtTabHover` + 三份 prefab 页签的 `Bind(...)` 第三格 = `ArtTabHover`
//    A5  三份文件的 `ArtTabHover` **都不是** `_hover`（挡住「把悬停图写成选中图」）
//  B 组（运行时探针 + 灭自证，零前提）：
//    B1  `CardArt.MenuUi` 真取得到 `40K_settings_button_selected` / `40K_settings_button_hover` 两张
//    B2  `Bind(..., hoverArt: …_selected)` ⇒ `HoverTexForTest.name == "40K_settings_button_selected"`（≠ `_hover`）
//    B3  真图 ⇒ **不**记进 `MissingSwapArt`（红线：不许静默缺图）
//    B4  🧨 **灭自证**：同一颗探针绑 `…_hover` ⇒ `HoverTexForTest.name` 变成 `…_hover`
//        （⇒ 「悬停图退成 `_hover`」在这条读数口上**看得见**，B2 不是恒真断言）
//    B5  口径：`Bind` **不传** `hoverArt` 时，全局表 `40K_settings_button → _selected` 仍然命中
//        （⇒ 如实记：本批是「把隐式依赖改成显式」，**不是**「改掉一张错图」；见报告 §一）
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · 删掉 SettingsWindow 的 `hoverArt: ArtTabHover` ⇒ **A2** 红（该站单独一条）。
//  · `PlayerProfileWindow` / `LeaderboardWindow` 把 `ArtTabHover` 改回 `null` ⇒ **A3 / A4** 红。
//  · 把三个 `ArtTabHover` 常量改成 `40K_settings_button_hover` ⇒ **A1/A3/A4 常量条 + A5** 红。
//  · 把 `ArtTabHover` 从 `Resources/Art/` 删掉 ⇒ **B1** 红（并会让运行时 `MissingSwapArt` 非空）。
//  · `WindowButton.Bind` 里 `hn = hoverArt ?? HoverNameFor(art)` 改成忽略 `hoverArt` ⇒ **B2** 红。
//  · 把 `MissingSwapArt` 的记录删掉 ⇒ **B3** 不红（B3 只断「真图不记」），那条红线由 `WindowButton` 自己的断言兜。
//
// ============================ 怎么接（本文件不碰宿主聚合）============================
//  `public static void Run();` —— **零前提**（只读 3 份源码 + 2 张图 + 一个用完即销毁的探针节点）。
//  挂法（由调度台决定）：同一物理行追加
//      B141_W54_Assert.Run(); pass += B141_W54_Assert.Pass; fail += B141_W54_Assert.Fail;
//  判绿红：按行首 `B141W54 ✓` / `B141W54 ✗` 数（⛔ 别 `grep -c ✗`）。

using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B141_W54`：W129 5-17（三扇窗页签悬停图 = `m_HighlightedSprite`）的回归断言。</summary>
public static class B141_W54_Assert
{
    const string Tag = "B141W54";
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _fail; } }
    /// <summary>本批接线的【代码站点】数 —— 3 个页签生成器（设置窗 / 档案窗 / 排行榜），覆盖原版 18 颗页签实例。</summary>
    public const int WiringSites = 3;

    /// <summary>本批逐颗现读的原版悬停图名（18/18 颗同值）。</summary>
    public const string HoverArt = "40K_settings_button_selected";
    /// <summary>选中图（= 原版 `onSprite` / `m_SelectedSprite`）；**与悬停图不是同一张**。</summary>
    public const string SelectedArt = "40K_settings_button_hover";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static string Src(string rel)
    {
        try { return File.ReadAllText(Application.dataPath + "/CardPresentation/" + rel); }
        catch (System.Exception e) { Debug.LogError(Tag + " 读不到 `" + rel + "`：" + e.Message); return ""; }
    }

    static bool Has(string s, string needle)
    {
        return !string.IsNullOrEmpty(s) && s.IndexOf(needle, System.StringComparison.Ordinal) >= 0;
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        GroupA_Wiring();
        GroupB_Probes();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail
                  + "（接线站点 " + WiringSites + " 个 · 覆盖原版 18 颗页签实例 · 悬停图 18/18 = " + HoverArt + "）");
    }

    // ==================================================================
    //  A · 逐站接线（判据 = 三份原版 prefab 的 `m_SpriteState.m_HighlightedSprite`）
    // ==================================================================
    static void GroupA_Wiring()
    {
        // ---- A1/A2 · SettingsWindow（原版 `Main Menu Settings Window` 5 颗页签）----
        var sw = Src("Shell/SettingsWindow.cs");
        Check(Has(sw, "ArtTabHover = \"" + HoverArt + "\""),
              "A1 设置窗页签**悬停**图 = `" + HoverArt + "`（= 原版 5/5 颗 `m_HighlightedSprite`）");
        Check(Has(sw, "public const string ArtTabBgSel = \"" + SelectedArt + "\";"),
              "A1b 设置窗页签**选中**图仍是 `" + SelectedArt + "`（= 原版 `onSprite` / `m_SelectedSprite`，两态不混）");
        Check(Has(sw, "hoverArt: ArtTabHover, clickCue: WindowSounds.SelectToggleCue, selectedArt: ArtTabBgSel"),
              "A2 设置窗 5 颗页签**逐颗**显式传 `hoverArt: ArtTabHover`（不再靠全局表隐式兜底）");

        // ---- A3 · PlayerProfileWindow（原版 `Player Profile Window` 6 颗；`Ranked` 按用户口径隐藏、绑定照做）----
        var pp = Src("Shell/PlayerProfileWindow.cs");
        Check(Has(pp, "ArtTabHover = \"" + HoverArt + "\""),
              "A3 档案窗页签**悬停**图 = `" + HoverArt + "`（= 原版 6/6 颗 `m_HighlightedSprite`）");
        Check(Has(pp, "Bind(_btnBg[i], ArtTabOff, ArtTabHover, null, ArtTabOn)"),
              "A3b 档案窗页签**逐颗**显式传悬停图（第三格 = `ArtTabHover`）");

        // ---- A4 · LeaderboardWindow（三份 prefab：Skirmish 3 · Classic 2 · Draft 2）----
        var lb = Src("Shell/LeaderboardWindow.cs");
        Check(Has(lb, "ArtTabHover = \"" + HoverArt + "\""),
              "A4 排行榜页签**悬停**图 = `" + HoverArt + "`（= 三份 prefab 7/7 颗 `m_HighlightedSprite`）");
        Check(Has(lb, "tabWb.Bind(bgQ, ArtTabOff, ArtTabHover, null, ArtTabOn)"),
              "A4b 排行榜页签**逐颗**显式传悬停图（第三格 = `ArtTabHover` · 三份 `TabsOf(Kind)` 共用这一处）");

        // ---- A5 · 悬停图 ≠ 选中图（挡「把 `_hover` 当悬停图传」）----
        Check(!Has(sw, "ArtTabHover = \"" + SelectedArt + "\"")
              && !Has(pp, "ArtTabHover = \"" + SelectedArt + "\"")
              && !Has(lb, "ArtTabHover = \"" + SelectedArt + "\""),
              "A5 三扇窗的 `ArtTabHover` **都不等于** `" + SelectedArt + "`（悬停 ≠ 选中）");
        Check(HoverArt != SelectedArt,
              "A5b 常量层：悬停图名 `" + HoverArt + "` ≠ 选中图名 `" + SelectedArt + "`");
    }

    // ==================================================================
    //  B · 运行时探针 / 灭自证 / 口径
    // ==================================================================
    static void GroupB_Probes()
    {
        // ---- B1 · 两张图真取得到 ----
        Check(CardArt.MenuUi(HoverArt) != null,
              "B1 悬停图 `" + HoverArt + "` 取得到（`CardArt.MenuUi`）—— 取不到就会落进 `MissingSwapArt`");
        Check(CardArt.MenuUi(SelectedArt) != null,
              "B1b 选中图 `" + SelectedArt + "` 取得到（对照项：两态都在 `Resources/Art/` 里）");

        var root = new GameObject("B141W54_probe");
        try
        {
            var q = ImageQuad.Create(root.transform, CardArt.Solid(), Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "q");
            Check(q != null, "B2 前提：探针 `ImageQuad` 建出来了（`CardArt.Solid()` 非空）");
            if (q != null)
            {
                var w = root.AddComponent<WindowButton>();
                int m0 = WindowButton.MissingSwapArt.Count;

                // ---- B2/B3 · 显式传悬停图 ⇒ 读数口拿到 `_selected`，且不记缺图 ----
                w.Bind(q, "40K_settings_button", HoverArt);
                Check(w.HoverTexForTest != null && w.HoverTexForTest.name == HoverArt,
                      "B2 `Bind(art: 40K_settings_button, hoverArt: `" + HoverArt + "`) ⇒ `HoverTexForTest.name == "
                      + HoverArt + "`｜实得 " + (w.HoverTexForTest != null ? w.HoverTexForTest.name : "<null>"));
                Check(w.HoverTexForTest != null && w.HoverTexForTest.name != SelectedArt,
                      "B2b 悬停图**不是** `" + SelectedArt + "`（挡「两态传反」）");
                Check(WindowButton.MissingSwapArt.Count == m0,
                      "B3 真图 ⇒ **不**记进 `MissingSwapArt`｜实得 " + (WindowButton.MissingSwapArt.Count - m0) + " 条");
                Check(!WindowButton.MissingSwapArt.Contains("40K_settings_button → " + HoverArt),
                      "B3b `MissingSwapArt` 里没有 `40K_settings_button → " + HoverArt + "`（本批三扇窗的缺图登记）");

                // ---- B4 · 🧨 灭自证：读数口能区分两张图 ----
                w.Bind(q, "40K_settings_button", SelectedArt);
                Check(w.HoverTexForTest != null && w.HoverTexForTest.name == SelectedArt,
                      "B4 🧨 同一颗探针改绑 `" + SelectedArt + "` ⇒ 读数口**看得见**变化（⇒ B2 不是恒真断言）｜实得 "
                      + (w.HoverTexForTest != null ? w.HoverTexForTest.name : "<null>"));

                // ---- B5 · 口径：不传 hoverArt 时全局表仍然命中 `_selected`（本批是「隐式→显式」，不是改错图）----
                w.Bind(q, "40K_settings_button");
                Check(w.HoverTexForTest != null && w.HoverTexForTest.name == HoverArt,
                      "B5 `Bind` **不传** `hoverArt` 时全局表 `40K_settings_button → " + HoverArt
                      + "` 仍命中（如实记：改前实际渲染已是 `_selected`，本批去掉的是**隐式依赖**）｜实得 "
                      + (w.HoverTexForTest != null ? w.HoverTexForTest.name : "<null>"));
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
