// B141_W44_Assert.cs — 执行写手 `B141-W44`（第一百二十八会话 · 2026-11-04）· W129 **5-07 的 77 颗接线**
//
// ============================ 它盯的是什么 ============================
//  5-07 = 原版 `bundle_menus_assets_all` 里 1276 颗 `Selectable` 中 **77 颗** `m_SpriteState.m_SelectedSprite` 非空。
//  机制（`Texture _selectedTex` / `Bind(..., selectedArt)` / `SelectedTexForTest` / `MissingSelectedArt`）
//  是 `B141-W34` 在 `Shell/WindowButton.cs` 里做好的（**本写手不动那个文件**）。
//  本文件盯的是**接线那一半**：把 77 颗按「节点族 → 选中图」表接进下面 8 个窗口。
//
//  判据（现读）= `报告/B141_W129_ButtonFx.md` §5-07 ＋ `报告/B138_审计汇总_问题清单.md` §5 的 5-07
//  ＋ 原版 prefab 全量重扫（2026-11-04，79 处含嵌套重复计；去掉 `Debug Reactivate Event Window` 那 2 颗
//  不建的调试件 = **77 颗**）。六张图（逐颗 guid → 切片名反查，全部在 `Resources/Art/` 里取得到）：
//
//  | 选中图 | 节点族 | 我们的落点（本批） |
//  |---|---|---|
//  | `40K_settings_button_hover`     | 设置/档案/排行榜页签 · `Orange Tab Toggle` · `Draft Deck Info Panel` 三颗 toggle | `SettingsWindow` · `PlayerProfileWindow` · `LeaderboardWindow` · `ChatPanel` · `DraftModeWindow` |
//  | `40K_dropdown_field_opened`     | 4 颗下拉框自己（Language/Quality/TextInHand/WindowMode） | `SettingsWindow` |
//  | `40K_dropdown_item_selected`    | 下拉项 `Template ▸ Item` | `SettingsWindow` |
//  | `40k_general_bt_yellow`         | `Match Log ▸ ReplayButton/PinButton` · `Deck info ▸ Switch Deck Info Button` | `MatchLogRow` · `DeckInfoPopup` |
//  | `40K_tab_button_overwindow`     | 牌组选择弹窗 2 颗 `Generic Tab UI Button`（原版 `trans=0` 死值） | `DeckSelectionPopup` |
//  | `40k_UI_bt_play`（`CircleButton`）| Continue / Battle 圆钮一族 | **本批 0 落点**（我们这几扇窗里没有 `CircleButton`） |
//
// ============================ 断言 ============================
//  A 组（逐站接线，零前提 —— 只读 8 份 `.cs` 文本）：
//    A1  `SettingsWindow` 三条常量（选中图 = 原版图名，⛔ 不是常态图）
//    A2  `SettingsWindow` 5 个页签 + 4 个下拉框 + 2 条下拉项生成器**逐站**传了 `selectedArt`
//    A3  `PlayerProfileWindow` 6 颗页签（`Ranked` 按用户口径隐藏 ⇒ 实际 5 颗）传 `ArtTabOn`
//    A4  `LeaderboardWindow` 三份排行榜 prefab 的页签传 `ArtTabOn`
//    A5  `ChatPanel` 两颗 `Orange Tab Toggle` 传 `TabArtOn`
//    A6  `MatchLogRow` `ReplayButton` / `PinButton` 两颗都传 `selectedArt: ArtBtn`
//    A7  `DraftModeWindow` 三颗 Deck Info toggle 传 `ToggleArtOnOff[0]`
//    A8  `DeckInfoPopup` **只有** `Switch Deck Info Button` 传（其余四颗原版 null）
//    A9  `DeckSelectionPopup` 两颗 `Generic Tab UI Button` 走 `BindNine(...)`，选中图 = `_overwindow`
//  B 组（灭自证 / 口径，零前提 + 2 条运行时探针）：
//    B1  `WindowButton.TexOf(Selected)` 仍是 `_selectedTex ?? _normalTex`（换图链没被改回去）
//    B2  **选中图 ≠ 常态图**（设置/档案/排行榜/聊天页签三条断言）—— 挡「顺手把常态图当选中图传」
//    B3  `DeckSelectionPopup` 的两颗：`on` 那颗常态图 `40K_tab_button` ≠ 选中图 `…_overwindow`（原版 `trans=0` 死值）
//    B4  `CardArt.MenuUi` 真取得到这 5 张选中图（取不到 ⇒ `MissingSelectedArt` 就会有值）
//    B5  运行时探针：`Bind(..., selectedArt: 真图)` ⇒ `SelectedTexForTest != null`
//    B6  运行时探针：`selectedArt` 缺省 ⇒ `SelectedTexForTest == null`（= 原版那 1199 颗）
//    B7  运行时探针：显式给了取不到的名字 ⇒ 记进 `MissingSelectedArt`（红线：不许静默）
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · 把任一站点的 `selectedArt:` 实参删掉 ⇒ 对应 A 条红（逐站点各一条，不是「整体一条」）。
//  · 把选中图改回常态图（例：`PlayerProfileWindow` 的 `ArtTabOn` 写成 `ArtTabOff`）⇒ 该文件常量条 + B2 红。
//  · 把 `WindowButton.TexOf(Selected)` 改回无条件 `_normalTex` ⇒ B1 红（**这就是 5-07 的题目本身**）。
//  · 把某张选中图从 `Resources/Art/` 里删掉 ⇒ B4 红（并会让运行时的 `MissingSelectedArt` 非空）。
//  · `Bind` 里把 `_selectedTex` 的赋值删掉 ⇒ B5 红。
//
// ============================ 怎么接（本文件不碰宿主聚合）============================
//  `public static void Run();` —— **零前提**（只读 8 份源码文本 + 5 张图 + 一个用完即销毁的探针节点）。
//  挂法（由调度台决定）：同一物理行追加
//      B141_W44_Assert.Run(); pass += B141_W44_Assert.Pass; fail += B141_W44_Assert.Fail;
//  判绿红：按行首 `B141W44 ✓` / `B141W44 ✗` 数（⛔ 别 `grep -c ✗`）。

using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B141_W44`：W129 5-07（`m_SelectedSprite` 77 颗）接线的回归断言。</summary>
public static class B141_W44_Assert
{
    const string Tag = "B141W44";
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _fail; } }
    /// <summary>本批接线的【代码站点】数（77 颗按族归并后的落点数；报告里有逐族对照）。
    /// 15 = `SettingsWindow` 7（页签 / 语言框 / 画质框 / 手牌框 / 窗口模式框 / 语言项 / 通用项）
    /// ＋ `PlayerProfileWindow` 1 ＋ `LeaderboardWindow` 1 ＋ `ChatPanel` 1 ＋ `MatchLogRow` 2
    /// ＋ `DraftModeWindow` 1 ＋ `DeckInfoPopup` 1 ＋ `DeckSelectionPopup` 1。</summary>
    public const int WiringSites = 15;

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

    static int Count(string s, string needle)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = s.IndexOf(needle, i, System.StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        GroupA_Wiring();
        GroupB_AntiSelfProof();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail
                  + "（接线站点 " + WiringSites + " 个 · 覆盖 47/77 颗 prefab 实例，余 30 颗判据在别的文件）");
    }

    // ==================================================================
    //  A · 逐站接线（判据 = `报告/B141_W129_ButtonFx.md` §5-07 的族→图表）
    // ==================================================================
    static void GroupA_Wiring()
    {
        // ---- A1/A2 · SettingsWindow（原版 `Main Menu Settings Window.prefab` 13 颗 = 5 页签 + 4 下拉框 + 4 下拉项）----
        var sw = Src("Shell/SettingsWindow.cs");
        Check(sw.Contains("public const string ArtTabBgSel = \"40K_settings_button_hover\";"),
              "A1 设置窗页签选中图 = `40K_settings_button_hover`（= 原版 `onSprite` · `m_SelectedSprite`）");
        Check(sw.Contains("public const string ArtLangFieldSel = \"40K_dropdown_field_opened\";"),
              "A1b 设置窗下拉框选中图 = `40K_dropdown_field_opened`");
        Check(sw.Contains("ArtLstItemSel = \"40K_dropdown_item_selected\""),
              "A1c 设置窗下拉项选中图 = `40K_dropdown_item_selected`");
        Check(sw.Contains("clickCue: WindowSounds.SelectToggleCue, selectedArt: ArtTabBgSel"),
              "A2 设置窗 5 颗页签**逐颗**传了 `selectedArt: ArtTabBgSel`（原版 5/5 颗 `m_SelectedSprite` 非空）");
        Check(sw.Contains("field, ArtLangField, selectedArt: ArtLangFieldSel"),
              "A2b 语言下拉框（`LanguagesDropdown`）传了选中图");
        Check(sw.Contains("qualQ, \"40K_dropdown_field_closed\", selectedArt: ArtLangFieldSel"),
              "A2c 画质下拉框（`Quality DropDown`）传了选中图");
        Check(sw.Contains("tihBg, ArtLangField, selectedArt: ArtLangFieldSel"),
              "A2d 手牌下拉框（`Text in Hand DropDown`）传了选中图");
        Check(sw.Contains("winBg, ArtLangField, selectedArt: ArtLangFieldSel"),
              "A2e 窗口模式下拉框（`Media ▸ Dropdown`）传了选中图");
        Check(Count(sw, "ArtLstItemPr, ArtLstItemSel") == 2,
              "A2f 两条下拉项生成器（语言 / 通用）**都**把 `ArtLstItemSel` 传下去了｜实得 "
              + Count(sw, "ArtLstItemPr, ArtLstItemSel"));
        Check(sw.Contains("BindSelected(hit, target, art, hoverArt, pressArt, selectedArt);")
              && sw.Contains("BindSelected(hit, target, art, hoverArt, null, selectedArt);"),
              "A2g 本窗两个 `Hit`/`Hit3` 包装**都**把 `selectedArt` 送进 `BindSelected`（否则形参是摆设）");

        // ---- A3 · PlayerProfileWindow（原版 13 颗 = 6 页签 + 5 成就页签 + 2 对局行；本批只到 6 页签 + 2 对局行）----
        var pp = Src("Shell/PlayerProfileWindow.cs");
        Check(pp.Contains("public const string ArtTabOn = \"40K_settings_button_hover\";"),
              "A3 档案窗页签选中图 = `40K_settings_button_hover`（6/6 颗逐颗实读同值）");
        Check(pp.Contains("_btnWb[i].Bind(_btnBg[i], ArtTabOff, null, null, ArtTabOn);"),
              "A3b 档案窗页签**逐颗**绑了选中图（`Ranked` 那颗按用户口径隐藏 ⇒ 实际生效 5 颗）");

        // ---- A4 · LeaderboardWindow（三份 prefab：Skirmish 3 颗 · Classic 2 颗 · Draft 2 颗）----
        var lb = Src("Shell/LeaderboardWindow.cs");
        Check(lb.Contains("const string ArtTabOn = \"40K_settings_button_hover\", ArtTabOff = \"40K_settings_button\";"),
              "A4 排行榜页签选中图 = `40K_settings_button_hover`（三份 prefab 逐颗同值）");
        Check(lb.Contains("tabWb.Bind(bgQ, ArtTabOff, null, null, ArtTabOn);"),
              "A4b 排行榜页签**逐颗**绑了选中图（按 `TabsOf(Kind)` 的 2~3 颗）");

        // ---- A5 · ChatPanel（原版 `ChatPanel ▸ Tab Buttons ▸ Orange Tab Toggle`）----
        var cp = Src("Shell/ChatPanel.cs");
        Check(cp.Contains("const string TabArtOn = \"40K_settings_button_hover\";"),
              "A5 聊天窗频道键选中图 = `40K_settings_button_hover`（= 原版 `onSprite` = `m_SelectedSprite`）");
        Check(cp.Contains("_tabBtn[idx].Bind(_tabBg[idx], TabArtOff, TabArtHover, TabArtPressed, TabArtOn);"),
              "A5b 聊天窗**逐颗**绑了选中图（两个频道键各一次）");

        // ---- A6 · MatchLogRow（原版 `Match Log ▸ {ReplayButton,PinButton}`；也被档案窗/弹窗复用）----
        var ml = Src("Shell/MatchLogRow.cs");
        Check(ml.Contains("const string ArtBtn = \"40k_general_bt_yellow\""),
              "A6 对局行两个圆钮的选中图 = `40k_general_bt_yellow`（= 它们的常态图，原版如此）");
        Check(Count(ml, "selectedArt: ArtBtn") == 2,
              "A6b `ReplayButton` 与 `PinButton` **两颗都**传了选中图｜实得 " + Count(ml, "selectedArt: ArtBtn"));

        // ---- A7 · DraftModeWindow（原版 `Draft Mode Deck Info Panel ▸ Buttons` 三颗 toggle）----
        var dw = Src("Shell/DraftModeWindow.cs");
        Check(dw.Contains("static readonly string[] ToggleArtOnOff = { \"40K_settings_button_hover\", \"40K_settings_button\" };"),
              "A7 轮抽右栏三颗 toggle 的选中图 = `40K_settings_button_hover`（= 它们出厂那张 `onSprite`）");
        Check(dw.Contains("deckTabWb.Bind(bgQ, ToggleArtOnOff[1], ToggleArtOnOff[0], ToggleArtOnOff[0], ToggleArtOnOff[0]);"),
              "A7b 三颗 toggle **逐颗**绑了选中图（`Alliance Info` / `Card List` / `Deck Info`）");

        // ---- A8 · DeckInfoPopup（五颗 `Deck Options` 里**只有** `Switch Deck Info Button` 非空）----
        var di = Src("Shell/DeckInfoPopup.cs");
        Check(di.Contains("selectedArt: key == \"Switch Deck Info\" ? \"40k_general_bt_yellow\" : null"),
              "A8 卡组信息窗：**只有** `Switch Deck Info` 传选中图（其余四颗原版 `m_SelectedSprite = null`）");
        Check(di.Contains("wb.Bind(target, art, hoverArt, null, selectedArt);"),
              "A8b 本窗 `Hit` 包装真的把 `selectedArt` 送进 `Bind`（否则形参是摆设）");

        // ---- A9 · DeckSelectionPopup（两颗 `Generic Tab UI Button`；原版 `trans=0` 死值）----
        var ds = Src("Shell/DeckSelectionPopup.cs");
        Check(ds.Contains("const string TabOnArt = \"40K_tab_button\", TabOffArt = \"40K_tab_button_overwindow\";"),
              "A9 牌组选择弹窗两颗页签的选中图 = `40K_tab_button_overwindow`（逐颗实读 · 两颗同值）");
        Check(ds.Contains("tabWb.BindNine(tabBgNode, norm, norm, norm, TabOffArt"),
              "A9b 两颗页签**逐颗**绑了选中图（九宫格走 `BindNine`；✅ 2026-11-04 B142-E17：判据改成**前缀**匹配 —— 尾随可选的 `NineMeta` 实参不该让这条假红）");
        Check(ds.Contains("var tabBgNode = MenuDraw.Nine("),
              "A9c 九宫格根节点被接住（否则 `BindNine` 没东西可绑）");
    }

    // ==================================================================
    //  B · 灭自证 / 口径 / 运行时探针
    // ==================================================================
    static void GroupB_AntiSelfProof()
    {
        // ---- B1 · 换图链本身（5-07 的题目就在这一句）----
        var wb = Src("Shell/WindowButton.cs");
        Check(wb.Contains("case BtnState.Selected: return _selectedTex ?? _normalTex;"),
              "B1 `TexOf(Selected)` 仍是 `_selectedTex ?? _normalTex`（改回无条件 `_normalTex` ⇒ 本批全部接线失效）");

        // ---- B2 · 选中图 ≠ 常态图（挡「顺手把常态图当选中图传」）----
        Check(!Src("Shell/PlayerProfileWindow.cs").Contains("ArtTabOn = \"40K_settings_button\";"),
              "B2 档案窗页签选中图**不是**常态图（`40K_settings_button` vs `…_hover`）");
        Check(!Src("Shell/ChatPanel.cs").Contains("TabArtOn = \"40K_settings_button\";"),
              "B2b 聊天窗频道键选中图**不是**常态图");
        Check(!Src("Shell/LeaderboardWindow.cs").Contains("const string ArtTabOn = \"40K_settings_button\","),
              "B2c 排行榜页签选中图**不是**常态图");
        // 设置窗三条也逐条核（它一个文件里用三张不同的选中图）
        var sw2 = Src("Shell/SettingsWindow.cs");
        Check(!sw2.Contains("ArtLangFieldSel = \"40K_dropdown_field_closed\""),
              "B2d 设置窗下拉框选中图**不是**常态图（`_opened` vs `_closed`）");
        Check(!sw2.Contains("ArtLstItemSel = \"40K_dropdown_item\""),
              "B2e 设置窗下拉项选中图**不是**常态图（`_selected` vs 无后缀）");

        // ---- B3 · 牌组选择弹窗那两颗：`on` 那颗常态图与选中图**不是同一张**（原版死值，但字段确实不同）----
        Check(Src("Shell/DeckSelectionPopup.cs").Contains("const string TabOnArt = \"40K_tab_button\""),
              "B3 牌组选择弹窗 `on` 那颗常态图 = `40K_tab_button`（≠ 选中图 `…_overwindow`）");

        // ---- B4 · 五张选中图**真的取得到**（取不到 ⇒ 运行时 `MissingSelectedArt` 非空）----
        string[] arts =
        {
            "40K_settings_button_hover", "40K_dropdown_field_opened", "40K_dropdown_item_selected",
            "40k_general_bt_yellow", "40K_tab_button_overwindow",
        };
        for (int i = 0; i < arts.Length; i++)
        {
            var tex = CardArt.MenuUi(arts[i]);
            Check(tex != null, "B4 选中图 `" + arts[i] + "` 取得到（`CardArt.MenuUi`）—— 取不到就会落进 `MissingSelectedArt`");
        }
        // 第六张（`40k_UI_bt_play`）本批 0 落点，但**机制上要能取到**，否则别处接 `CircleButton` 会静默缺图。
        Check(CardArt.MenuUi("40k_UI_bt_play") != null,
              "B4b `40k_UI_bt_play`（`CircleButton` 那一族）也取得到 —— 本批 0 落点，但机制得先可用");

        // ---- B5~B7 · 运行时探针：`Bind` 的第五个实参真的把 `_selectedTex` 写上 / 缺省不写 / 缺图要记 ----
        var root = new GameObject("B141W44_probe");
        try
        {
            var q = ImageQuad.Create(root.transform, CardArt.Solid(), Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "q");
            Check(q != null, "B5 前提：探针 `ImageQuad` 建出来了（`CardArt.Solid()` 非空）");
            if (q != null)
            {
                var w = root.AddComponent<WindowButton>();
                int m0 = WindowButton.MissingSelectedArt.Count;

                w.Bind(q, "40K_settings_button", null, null, "40K_settings_button_hover");
                Check(w.SelectedTexForTest != null,
                      "B5b `Bind(..., selectedArt: 40K_settings_button_hover)` ⇒ `SelectedTexForTest` 非空（换图链通了）");
                Check(WindowButton.MissingSelectedArt.Count == m0,
                      "B5c 真图 ⇒ **不**记进 `MissingSelectedArt`｜实得 " + (WindowButton.MissingSelectedArt.Count - m0) + " 条");

                w.Bind(q, "40K_settings_button");
                Check(w.SelectedTexForTest == null,
                      "B6 `selectedArt` 缺省 ⇒ `SelectedTexForTest == null`（= 原版那 1199 颗 `m_SelectedSprite = null`）");

                w.Bind(q, "40K_settings_button", null, null, "B141W44_NoSuchSelectedArt");
                Check(w.SelectedTexForTest == null,
                      "B7 显式给了**取不到**的名字 ⇒ `SelectedTexForTest == null`（不静默画一张错的）");
                Check(WindowButton.MissingSelectedArt.Contains("[selected] B141W44_NoSuchSelectedArt"),
                      "B7b **灭自证**：取不到时**照样记**进 `MissingSelectedArt`（⛔ 不是「什么都不记」）｜实得 "
                      + (WindowButton.MissingSelectedArt.Count > m0
                         ? string.Join("、", WindowButton.MissingSelectedArt.ToArray()) : "<空>"));
                WindowButton.MissingSelectedArt.Remove("[selected] B141W44_NoSuchSelectedArt");
                Check(WindowButton.MissingSelectedArt.Count == m0,
                      "B7c 探针记录已清回（不污染别的宿主那条「名单必须为空」）");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
