// B141_W70_Assert.cs — 执行写手 `B141-W70`（第一百二十八会话 · 2026-11-04）· W129 **5-07 剩余 30 颗**
//
// ============================ 它盯的是什么 ============================
//  5-07 = 原版 `bundle_menus_assets_all` 里 1276 颗 `Selectable` 中 **77 颗** `m_SpriteState.m_SelectedSprite` 非空。
//  `B141-W44` 接掉 47/77；本写手接**余下 30 颗**（判据 = `报告/B141_W129_SelectedArt.md` §三 的「该派给谁」）。
//  机制（`Texture _selectedTex` / `Bind(..., selectedArt)` / `SelectedTexForTest` / `MissingSelectedArt`）
//  是 `B141-W34` 在 `Shell/WindowButton.cs` 里做好的 —— **本写手一个字节没动那个文件**。
//
//  ── 30 颗的四种去向（本批的口径；⛔ 不是「没查」也不是「不做」）──
//  | 去向 | 颗数 | 逐族 |
//  |---|---|---|
//  | ✅ **本批新接线** | **11** | 成就页 4（`ArtTabSel`，运行时 4 颗 = `ProfileData.TypeOrder.Length`）＋ 下拉项 6（`ItemSelArt`）＋ `Achievement Type Toggle.prefab` 模板 1（与成就页同一站点） |
//  | ✅ **等价（现状已画同一张图）** | **7** | 社交 4 颗 `Generic Tab UI Button`（原版 `trans=0` 死值，选中图 = `40K_tab_button_overwindow` = 我们的 `ArtTabOff`）＋ `CircleButton` 3 颗（选中图 = 常态图 `40k_UI_bt_play`） |
//  | ⛔ **我们树上没有对应节点** | **6** | 成就页第 5 颗（`Secret` 分类，运行时只建 4）· `CircleButton` 2 颗（Ranked 走 `To Battle`、轮抽走 `UI_Button_Mulligan`）· `Debug Battle Button` 3 颗（原版 `OnEnable` 强制 `SetActive(false)`） |
//  | 🔴 **机制阻塞（已上报）** | **6** | 社交 6 个下拉框的合上面：`WindowButton.BindNine` 会把 9 块全设成同一宽高比 ⇒ 九宫格中段被压扁（`Shell/WindowButton.cs` 不在本写手白名单） |
//  合计 11 + 7 + 6 + 6 = **30** ✔（与 W44 的 47/77 自洽）。
//
// ============================ 断言 ============================
//  A 组（逐站接线，零前提 —— 只读白名单里的 `.cs` 文本 + 公开常量）：
//    A1  `AchievementsMenu.ArtTabSel` = `40K_settings_button_hover`（= 原版 5/5 颗 `m_SelectedSprite`）
//    A2  成就页页签**逐颗**在 `tabWb` 上补 `Bind(..., ArtTabSel)`（在同一 `for` 循环里 ⇒ 覆盖每个运行时节点）
//    A3  达成页运行时颗数 = `ProfileData.TypeOrder.Length` = **4**（原版 prefab 5 颗里有 1 颗是 `Secret` 分类）
//    A4  `AllianceDropdownBox.ItemSelArt` = `40K_dropdown_item_selected`（= 原版 6 个 `Item` 模板逐颗）
//    A5  `BindRow` 把 `ItemSelArt` 传成**第五格**（不是常态图、不是悬停图）
//    A6  `ItemArtName` 与 `BindRow` **共用** `ItemSelArt`（一处判据，⛔ 不再写第二份字面量）
//    A7  社交两文件：页签选中图 = `ArtTabOff`（= `40K_tab_button_overwindow`）且**没有**接 `selectedArt`（`trans=0` 死值，接了反成偏离）
//    A8  `PracticeModePopup` / `MultiCardDisplay`：`CircleButton` 那张图 = `40k_UI_bt_play`（= 原版选中图，也 = 常态图）
//    A9  `RankedEventWindow` / `DraftModeWindow`：**确实没有** `CircleButton`（所以那 2 颗「无对应节点」是可核的，不是没找）
//    A10 `Debug Battle Button` 我们**不建**（同 A9 口径）
//  B 组（灭自证 / 口径 / 运行时探针）：
//    B1  `WindowButton.TexOf(Selected)` 仍是 `_selectedTex ?? _normalTex`（改回无条件 `_normalTex` ⇒ 5-07 全废）
//    B2  成就页第五格**不是**常态图（`ArtTabSel` ≠ `ArtTab`）
//    B3  下拉项第五格**不是**常态图（`ItemSelArt` ≠ `40K_dropdown_item`）
//    B4  用到的 4 张选中图**真的取得到**（`CardArt.MenuUi`；取不到就会落进 `MissingSelectedArt`）
//    B5  运行时探针：`Bind(..., selectedArt: 真图)` ⇒ `SelectedTexForTest != null`
//    B6  运行时探针：`selectedArt` 缺省 ⇒ `SelectedTexForTest == null`（= 原版那 1199 颗 null）
//    B7  运行时探针：显式给了取不到的名字 ⇒ **记进** `MissingSelectedArt`（红线：不许静默）
//    B8  探针把 `MissingSelectedArt` 清回原样（⛔ 不污染别的宿主那条「名单必须为空」）
//    B9  30 颗的四种去向求和 = 30（表与代码一起漂时这条会红）
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · 删掉 `tabWb.Bind(..., ArtTabSel)` 那一句 ⇒ **A2** 红。
//  · 把 `ArtTabSel` 改成常态图那串 / 把 `BindRow` 的第五格改回 `null` ⇒ **A1 / A5 / B2 / B3** 红。
//  · 把 `ItemArtName` 里的 `ItemSelArt` 改回字面量（两处判据） ⇒ **A6** 红。
//  · 给 `trans=0` 的社交页签硬接 `selectedArt` ⇒ **A7** 红（那正是「把死值当活值」那一类偏离）。
//  · `WindowButton.TexOf(Selected)` 改回无条件 `_normalTex` ⇒ **B1** 红。
//  · 从 `Resources/Art/` 删掉任一选中图 ⇒ **B4** 红（且运行时 `MissingSelectedArt` 非空）。
//  · `Bind` 里把 `_selectedTex` 的赋值删掉 ⇒ **B5** 红；`MissingSelectedArt.Add` 删掉 ⇒ **B7** 红。
//
// ============================ 怎么接（本文件不碰宿主聚合）============================
//  `public static void Run();` —— **零前提**（只读 6 份源码文本 + 4 张图 + 一个用完即销毁的探针节点）。
//  挂法（由调度台决定）：同一物理行追加
//      B141_W70_Assert.Run(); pass += B141_W70_Assert.Pass; fail += B141_W70_Assert.Fail;
//  判绿红：按行首 `B141W70 ✓` / `B141W70 ✗` 数（⛔ 别 `grep -c ✗`）。

using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B141_W70`：W129 5-07 余下 30 颗（接线 11 · 等价 7 · 无节点 6 · 机制阻塞 6）的回归断言。</summary>
public static class B141_W70_Assert
{
    const string Tag = "B141W70";
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _fail; } }

    // ---- 30 颗的四种去向（报告 §三 的逐族表；B9 断它们求和 = 30）----
    /// <summary>本批**新接线**的颗数：成就页 4 ＋ 下拉项 6 ＋ 模板 1（同一站点）。</summary>
    public const int WiredCount = 11;
    /// <summary>**等价**（现状已画同一张图）：社交 `trans=0` 页签 4 ＋ `CircleButton` 3。</summary>
    public const int EquivalentCount = 7;
    /// <summary>**我们树上没有对应节点**：`Secret` 页签 1 ＋ `CircleButton` 2 ＋ `Debug Battle Button` 3。</summary>
    public const int NoNodeCount = 6;
    /// <summary>**机制阻塞**：社交 6 个下拉框合上面（`BindNine` 会压扁九宫格中段）。</summary>
    public const int BlockedCount = 6;
    /// <summary>差额口径：30 = 11 + 7 + 6 + 6（与 W44 的 47/77 自洽）。</summary>
    public const int DeltaTotal = 30;

    /// <summary>本批**代码站点**数：成就页 1 ＋ 下拉项 1 = 2（逐族表在报告 §一）。</summary>
    public const int WiringSites = 2;

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

    public static void Run()
    {
        _pass = 0; _fail = 0;
        GroupA_Wiring();
        GroupB_AntiSelfProof();
        GroupC_DropdownTints();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail
                  + "（站点 " + WiringSites + " 个 · 30 颗 = 接线 " + WiredCount + " + 等价 " + EquivalentCount
                  + " + 无节点 " + NoNodeCount + " + 机制阻塞 " + BlockedCount + "）");
    }

    // ==================================================================
    //  A · 逐站接线
    // ==================================================================
    static void GroupA_Wiring()
    {
        // ---- 成就页（原版 `Player Profile Window ▸ Trophies Tab/buttons/Achievement Type Toggle (0..4)`）----
        var ach = Src("Shell/AchievementsMenu.cs");
        Check(ach.Contains("ArtTabSel = \"40K_settings_button_hover\""),
              "A1 成就页选中图常量 = `40K_settings_button_hover`（原版 5/5 颗 `m_SelectedSprite` 逐颗现读）");
        Check(ach.Contains("tabWb.Bind(_tabBg[i], ArtTab, ArtTabHover, null, ArtTabSel);"),
              "A2 成就页页签**逐颗**补了 `selectedArt: ArtTabSel`（在 `for (i …)` 里 ⇒ 每个运行时节点都吃到）");
        int typeOrder = ProfileData.TypeOrder != null ? ProfileData.TypeOrder.Length : -1;
        Check(typeOrder == 4,
              "A3 成就页运行时颗数 = `ProfileData.TypeOrder.Length` = **4**（原版 prefab 5 颗里第 5 颗是 `Secret` 分类）｜实得 " + typeOrder);
        Check(ach.Contains("public const string ArtTab = \"40K_settings_button\""),
              "A3b 常态图常量仍在（`ArtTab` = `40K_settings_button`）—— 挡「把常态图顶替成选中图」");

        // ---- 社交下拉项（原版 `Social Submenu Variant ▸ {Languages,Privacy}Dropdown/…/Item` ×6）----
        var mt = Src("Shell/AllianceMemberTab.cs");
        Check(mt.Contains("public const string ItemSelArt = \"40K_dropdown_item_selected\""),
              "A4 下拉项选中图常量 = `40K_dropdown_item_selected`（原版 6 个 `Item` 模板逐颗同值）");
        Check(mt.Contains("null, ItemSelArt)"),
              "A5 `BindRow` 把选中图传成**第五格**（`Bind(..., hoverArt, null, ItemSelArt)`）");
        Check(mt.Contains("i == _selRow ? ItemSelArt : \"40K_dropdown_item\""),
              "A6 `ItemArtName` 与 `BindRow` 共用 `ItemSelArt`（一处判据，⛔ 不写第二份字面量）");

        // ---- 社交 4 颗 `Generic Tab UI Button`：`trans=0` 死值 ⇒ 等价、**不接** ----
        Check(mt.Contains("ArtTabOff = \"40K_tab_button_overwindow\""),
              "A7 社交成员页：`ArtTabOff` = `40K_tab_button_overwindow`（= 原版 4 颗页签的 `m_SelectedSprite`；`trans=0` ⇒ 等价）");
        Check(!mt.Contains("selectedArt: ArtTabOff"),
              "A7b 成员页那 4 颗**没有**硬接 `selectedArt`（`trans=0` 死值：接了会在 `on` 态页签被选中时画出 `_overwindow`，反成偏离）");
        var st = Src("Shell/AllianceSearchTab.cs");
        Check(st.Contains("ArtTabOff = \"40K_tab_button_overwindow\"") && !st.Contains("selectedArt: ArtTabOff"),
              "A7c 社交搜索页同口径（`Search` / `Create` 两颗 = `trans=0` ⇒ 等价、不接）");

        // ---- `CircleButton` 一族：选中图 = 常态图（`40k_UI_bt_play`）⇒ 等价 ----
        var pm = Src("Shell/PracticeModePopup.cs");
        Check(pm.Contains("Img(root, \"40k_UI_bt_play\""),
              "A8 `Practice Mode Menu ▸ DeckSelectionContinueButton ▸ CircleButton` 画的是 `40k_UI_bt_play`（= 它自己的选中图）");
        var mc = Src("Battle/MultiCardDisplay.cs");
        Check(mc.Contains("CardArt.Ui(\"40k_UI_bt_play\")"),
              "A8b `Generic Multi Card Display Combat ▸ CircleButton` 同样画 `40k_UI_bt_play`（= 选中图）");
        Check(mc.Contains("40k_UI_bt_play_hover") && mc.Contains("40k_UI_bt_play_pressed"),
              "A8c 那张圆钮的悬停/按下两图**本地已有**且被接上（顺带订正旧注释里「本地缺那两张」）");

        // ---- 无对应节点的两族：结构性证明（不是「没找」）----
        var rk = Src("Shell/RankedEventWindow.cs");
        Check(!rk.Contains("40k_UI_bt_play"),
              "A9 `RankedEventWindow` 树上**确实没有** `CircleButton`（原版那 1 颗的等价物是 `To Battle Button` ⇒ 无节点）");
        var dr = Src("Shell/DraftModeWindow.cs");
        // 🔴 2026-11-05（`B150-W-D`）：**先剥整行注释再比** —— `DraftModeWindow.cs:1720` 那句说明注释里就写着
        //   「`Debug Battle Button Button`」，`Contains` 命中的是**注释**不是接线（原来这条从没绿过就是这个原因）。
        dr = string.Join("\n", System.Array.FindAll(dr.Split('\n'), l => !l.TrimStart().StartsWith("//")));
        Check(!dr.Contains("40k_UI_bt_play"),
              "A9b `DraftModeWindow`（轮抽 `To Battle`）同样是 `UI_Button_Mulligan` + 文字 ⇒ 无 `CircleButton` 节点（剥注释后比）");
        Check(!dr.Contains("Debug Battle Button"),
              "A10 `Debug Battle Button` 三颗我们**不建**（原版 `OnEnable` 强制 `SetActive(false)`，正式包也不显示；剥注释后比）");

        // ---- 6 个社交下拉框：⚠️ 2026-11-04 `B141-W81` 已把机制缺陷修掉、6 颗已接上线 ----
        //   原来这条断的是「本批不接的理由写在代码里」；那条理由（`BindNine` 压扁九宫格中段）已经**不成立**
        //   ⇒ 改成断「那条过期理由**不许再出现**」（代码里留着与事实相反的说明 = 下一个会话的误导源）。
        Check(!mt.Contains("BindNine` 绑完把 9 块全按 `qs[0]` 的宽高比"),
              "A11 过期理由已就地删掉（`BindNine` 压扁九宫格中段 —— 缺陷已在 `B141-W81` 修掉、6 颗已接上）");
    }

    // ==================================================================
    //  B · 灭自证 / 口径 / 运行时探针
    // ==================================================================
    static void GroupB_AntiSelfProof()
    {
        // ---- B1 · 换图链本身（5-07 的题目就在这一句）----
        var wb = Src("Shell/WindowButton.cs");
        Check(wb.Contains("case BtnState.Selected: return _selectedTex ?? _normalTex;"),
              "B1 `TexOf(Selected)` 仍是 `_selectedTex ?? _normalTex`（改回无条件 `_normalTex` ⇒ 本批接线全部失效）");

        // ---- B2 / B3 · 选中图 ≠ 常态图 ----
        Check(!Src("Shell/AchievementsMenu.cs").Contains("tabWb.Bind(_tabBg[i], ArtTab, ArtTabHover, null, ArtTab);"),
              "B2 成就页第五格**不是**常态图（`ArtTabSel` = `…_hover` vs `ArtTab` = `40K_settings_button`）");
        Check(!Src("Shell/AllianceMemberTab.cs").Contains("null, \"40K_dropdown_item\")"),
              "B3 下拉项第五格**不是**常态图（`_selected` vs 无后缀）");

        // ---- B4 · 四张选中图真的取得到 ----
        string[] arts = { "40K_settings_button_hover", "40K_dropdown_item_selected",
                          "40K_tab_button_overwindow", "40k_UI_bt_play" };
        for (int i = 0; i < arts.Length; i++)
        {
            var tex = CardArt.MenuUi(arts[i]);
            Check(tex != null, "B4 选中图 `" + arts[i] + "` 取得到（`CardArt.MenuUi`）—— 取不到就会落进 `MissingSelectedArt`");
        }

        // ---- B5~B8 · 运行时探针 ----
        var root = new GameObject("B141W70_probe");
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

                w.Bind(q, "40K_dropdown_item", null, null, "40K_dropdown_item_selected");
                Check(w.SelectedTexForTest != null,
                      "B5d 下拉项那一条链同样通（`ItemSelArt`）");

                w.Bind(q, "40K_settings_button");
                Check(w.SelectedTexForTest == null,
                      "B6 `selectedArt` 缺省 ⇒ `SelectedTexForTest == null`（= 原版那 1199 颗 `m_SelectedSprite = null`）");

                w.Bind(q, "40K_settings_button", null, null, "B141W70_NoSuchSelectedArt");
                Check(w.SelectedTexForTest == null,
                      "B7 显式给了**取不到**的名字 ⇒ `SelectedTexForTest == null`（不静默画一张错的）");
                Check(WindowButton.MissingSelectedArt.Contains("[selected] B141W70_NoSuchSelectedArt"),
                      "B7b **灭自证**：取不到时**照样记**进 `MissingSelectedArt`（⛔ 不是「什么都不记」）｜实得 "
                      + (WindowButton.MissingSelectedArt.Count > m0
                         ? string.Join("、", WindowButton.MissingSelectedArt.ToArray()) : "<空>"));
                WindowButton.MissingSelectedArt.Remove("[selected] B141W70_NoSuchSelectedArt");
                Check(WindowButton.MissingSelectedArt.Count == m0,
                      "B8 探针记录已清回（不污染别的宿主那条「名单必须为空」）");
                Check(WindowButton.MissingSelectedArt.Count == 0,
                      "B8b 本批接线的图**一条都不缺**（`MissingSelectedArt` 全空 —— 4 张图都在 `Resources/Art/` 里）");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        // ---- B9 · 30 颗的四种去向求和（表与代码一起漂时这条会红）----
        Check(WiredCount + EquivalentCount + NoNodeCount + BlockedCount == DeltaTotal,
              "B9 30 颗求和自洽：" + WiredCount + " 接线 + " + EquivalentCount + " 等价 + "
              + NoNodeCount + " 无节点 + " + BlockedCount + " 机制阻塞 = " + DeltaTotal);
    }

    // ==================================================================
    //  C · 两处下拉「条目底/列表底」染色：取整 → 逐位（`B141-W85` · 铁律 21 色彩参数·逐通道 1e-6）
    // ==================================================================
    //  判据（原版 prefab 现读，`d:/2/tools/assetripper_out/ExportedProject/Assets/`）：
    //   · 设置窗 `Item Background` 四颗 = `Main Menu Settings Window.prefab:16349/21477/35588/36765`
    //     `Image.m_Color = (0.28627452, 0.9647059, 0.6862745, 1)`（旧值取整成 `0.2863/0.9647/0.6863`）。
    //   · 社交下拉列表底板 = `Social Submenu Variant.prefab:20174/21319/23796/24779/32090/33073`
    //     六颗 `Template ▸ Image.m_Color = (1, 0.4745098, 0.09803922, 1)`（= 121/255 · 25/255；
    //     旧值取整成 `1/0.475/0.098`）。
    //  🔴 **别把两族搞混**：同一份社交 prefab 里 `Item Background` 是**另一个值**
    //     `(1, 0.4739555, 0.0990566)`（`Template` 与它不同色，已逐一按 sprite guid 分开核过）。
    //  🧨 灭自证：把任一值改回取整 ⇒ C2 / C4 红（差分 4.9e-4 / 3.9e-5 / 5.5e-6 量级，远大于 1e-6）。
    static void GroupC_DropdownTints()
    {
        // ---- C1 / C2 · 设置窗 `Item Background` ----
        var se = SettingsWindow.LstItemTint;
        Check(Near(se.r, 0.28627452f) && Near(se.g, 0.9647059f) && Near(se.b, 0.6862745f),
              "C1 设置窗 `LstItemTint` 逐通道 = (0.28627452, 0.9647059, 0.6862745)（容差 1e-6）｜实得 " + Fmt(se));
        Check(Far(se.r, 0.2863f) && Far(se.g, 0.9647f) && Far(se.b, 0.6863f),
              "C2 **灭自证**：`LstItemTint` 逐通道都**不等于**旧取整 (0.2863, 0.9647, 0.6863)｜实得 " + Fmt(se));
        Check(Near(se.a, 1f), "C2b 设置窗那颗 alpha = 1（原版 `a: 1`）｜实得 " + se.a.ToString("F6"));

        // ---- C3 / C4 · 社交下拉列表底板（`AllianceDropdownBox.BuildList` 的 `Template ▸ Bg`）----
        string src = Src("Shell/AllianceMemberTab.cs");
        var m = System.Text.RegularExpressions.Regex.Match(src,
                    @"QListBg,\s*new Color\(\s*([0-9.]+)f,\s*([0-9.]+)f,\s*([0-9.]+)f,\s*([0-9.]+)f\s*\)");
        Check(m.Success, "C3 前提：在 `AllianceMemberTab.cs` 里找到了列表底板那条 `QListBg, new Color(r,g,b,a)`");
        if (m.Success)
        {
            float r = Parse(m.Groups[1].Value), g = Parse(m.Groups[2].Value),
                  b = Parse(m.Groups[3].Value), a = Parse(m.Groups[4].Value);
            Check(Near(r, 1f) && Near(g, 0.4745098f) && Near(b, 0.09803922f) && Near(a, 1f),
                  "C3 社交下拉列表底板逐通道 = (1, 0.4745098, 0.09803922, 1)（原版六颗 `Template`；容差 1e-6）｜实得 ("
                  + r.ToString("F8") + ", " + g.ToString("F8") + ", " + b.ToString("F8") + ", " + a.ToString("F4") + ")");
            Check(Far(g, 0.475f) && Far(b, 0.098f),
                  "C4 **灭自证**：g/b 都**不等于**旧取整 (0.475, 0.098)（改回取整 ⇒ C3/C4 红）｜实得 g="
                  + g.ToString("F8") + " · b=" + b.ToString("F8"));
        }
    }

    const float TintEps = 1e-6f;
    static bool Near(float v, float want) { return Mathf.Abs(v - want) <= TintEps; }
    static bool Far(float v, float old) { return Mathf.Abs(v - old) > TintEps; }
    static float Parse(string s) { return float.Parse(s, System.Globalization.CultureInfo.InvariantCulture); }
    static string Fmt(Color c)
    {
        return "(" + c.r.ToString("F8") + ", " + c.g.ToString("F8") + ", " + c.b.ToString("F8")
             + ", " + c.a.ToString("F4") + ")";
    }
}
