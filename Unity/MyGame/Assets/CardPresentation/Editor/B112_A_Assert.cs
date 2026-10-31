// B112_A_Assert.cs — 第一百一十一会话 · 执行代理 `B112_A`（`B-77` · `B-78`）
//
// ============================ 它盯的是什么 ============================
//  `B-77`：`Battle/SettingsPanel.cs` 的语言下拉行**原来只有 normal + hover**（`RefreshLangHover` 只算 `hot`），
//    按下（`_press`）与选中（`_selected`）两档没接。本轮接上，判据 = 原版 `battlearena1` 的
//    `LanguagesDropdown`（TMP_Dropdown）→ `Template` → `Viewport` → `Content` → `Item` 那颗 `Toggle`
//    （MonoBehaviour 4939）：`m_Transition = 2`，`m_SpriteState` 三张 = `_hover` / `_press` / `_selected`，
//    常态图在 `Item Background` 的 `Image.m_Sprite`（`40K_dropdown_item`）。
//    · `_selected` 的语义 = **UGUI `Selectable` 的 EventSystem 选中**（`hasSelection`），⛔ 不是 `Toggle.isOn`
//      —— `com.unity.ugui@2.0.0` 的 `Toggle` 没有 `DoStateTransition` 覆写（见报告 §判据）。
//      但当前语言那行**两者同时成立**：`TMP_Dropdown.Show()` 先 `isOn = value == i`、再 `if (isOn) Select()`。
//    · 优先级 = `Pressed > Selected > Highlighted > Normal`；「按下后拖出去仍算按下」（`OnPointerExit` 不清 `isPointerDown`）。
//
//  `B-78`：`Shell/AllianceMemberTab.cs` 的下拉项 `MenuDraw.Hit(..., art: null, hoverArt: "…_hover", pressedArt: null)`
//    ⇒ `WindowButton.PressedNameFor(null)` 返回 null ⇒ **按下不换图**（原版那颗 `Toggle` 有 `m_PressedSprite`）。
//    附带：`art == null` 时 `MissingPressedArt` / `MissingSwapArt` 会记一条无意义的 `" → "`（WindowButton.cs:349/353）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A3/A4：把 `LangItemArtFor` 写回「`hot ? hover : normal`」⇒ A3（常态）/A4（悬停当前行仍是 `_selected`）红。
//  · A6/A7：把「按下」排在 `Selected`/`Highlighted` 之后 ⇒ A6/A7 红（原版 `isPointerDown` 优先级最高）。
//  · B5：把四档填成同一张图 ⇒ B5 红。
//  · C3：把 `WindowButton.PressedNames` 里 `40K_dropdown_item` 那条删掉 ⇒ C3/D 红（B-78 的落点会退回 `_pressed`）。
//  · D1/D3b/D3c：删掉 `RecordMissingArt` 的调用（退回「什么都记」）⇒ D3b/D3c 红。
//  · D2/D3d：把 `RecordMissingArt` 改成恒 `false`（=「什么都不记」的另一条捷径）⇒ D2/D3d 红。
//    ⇒ **「全都记」与「全都不记」结构上不可能同时满足 D3b/D3c 与 D2/D3d**，唯一解 = 按图名是否为空记。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `B-78` 那半边与外壳共用件 `WindowButton` 有关 ⇒ 放 `Editor/ShellScene.cs` 最稳；
//  `B-77` 那半边是 `Battle/SettingsPanel.cs` 的纯函数 + 资产，放哪个宿主都成立（`BattleScene.cs` 也跑得到它）。
//  在宿主那条聚合行末尾**同一物理行**追加：
//      B112A_Assert.Run(); pass += B112A_Assert.Pass; fail += B112A_Assert.Fail;
//  ⚠️ 追加时先看该行第一个 `//` 在哪 —— 文本必须落在它**之前**（2026-10-30 那次 17 个断言类被注释吞掉的坑）。
//
// ============================ 判据 / 报告 ============================
//  · 原版 prefab：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/`
//    （`GameObject/LanguagesDropdown.json` / `GameObject/Item.json` / `MonoBehaviour/MonoBehaviour_4939.json` /
//      `MonoBehaviour/MonoBehaviour_4776.json`）；精灵名解析 = `bundle_duplicateassetisolation_assets_all/Sprite/`
//  · UGUI 源码：`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/{Selectable,Toggle}.cs`
//    ＋ `Runtime/TMP/TMP_Dropdown.cs`
//  · 报告：`资料/完全复刻/报告/B112_A.md`
using System.Reflection;
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B112_A`：语言下拉四档（`B-77`）+ 联盟下拉项按下图（`B-78`）。</summary>
public static class B112A_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B112A] ");

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    const BindingFlags PrivStatic = BindingFlags.NonPublic | BindingFlags.Static;
    const BindingFlags PrivInst = BindingFlags.NonPublic | BindingFlags.Instance;

    // 四个图名（逐字；`_press` 不是 `_pressed`）。
    const string ArtNormal = "40K_dropdown_item";
    const string ArtHover = "40K_dropdown_item_hover";
    const string ArtPress = "40K_dropdown_item_press";
    const string ArtSelected = "40K_dropdown_item_selected";

    public static void Run()
    {
        MenuCheck.Section(_sink, "B112_A：语言下拉四档（B-77）+ 联盟下拉项按下图（B-78）");
        SectionA_FourStates();
        SectionB_Sprites();
        SectionC_Discriminators();
        SectionD_PressArtWire();
        Debug.Log(_sink.P + "B112A 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · B-77 四档判据
    static void SectionA_FourStates()
    {
        MenuCheck.Section(_sink, "A `SettingsPanel` 语言行：常态 / 悬停 / 选中 / 按下 四档与优先级");

        var miArt = typeof(SettingsPanel).GetMethod("LangItemArtForTest", BindingFlags.Public | BindingFlags.Static);
        var fiA = typeof(SettingsPanel).GetField("LstItemArt", PrivStatic);
        var fiH = typeof(SettingsPanel).GetField("LstItemHiArt", PrivStatic);
        var fiP = typeof(SettingsPanel).GetField("LstItemPrArt", PrivStatic);
        var fiS = typeof(SettingsPanel).GetField("LstItemSelArt", PrivStatic);
        Tr(miArt != null, "A0 **前提**：反射拿到 `SettingsPanel.LangItemArtForTest`（public static）");
        Tr(fiA != null && fiH != null && fiP != null && fiS != null,
           "A0b **前提**：反射拿到四个 private const（`LstItemArt` / `LstItemHiArt` / `LstItemPrArt` / `LstItemSelArt`）");
        if (miArt == null || fiA == null || fiH == null || fiP == null || fiS == null) return;

        C((string)fiA.GetRawConstantValue(), ArtNormal, "A0c `LstItemArt` = `" + ArtNormal + "`（原版 `Item Background.m_Sprite`）");
        C((string)fiH.GetRawConstantValue(), ArtHover, "A0d `LstItemHiArt` = `" + ArtHover + "`");
        C((string)fiP.GetRawConstantValue(), ArtPress, "A0e `LstItemPrArt` = `" + ArtPress + "`");
        C((string)fiS.GetRawConstantValue(), ArtSelected, "A0f `LstItemSelArt` = `" + ArtSelected + "`");

        System.Func<bool, bool, bool, string> art =
            (cur, hov, prs) => (string)miArt.Invoke(null, new object[] { cur, hov, prs });

        // 四档 —— 判据逐字照 UGUI `Selectable.currentSelectionState` 的优先级。
        C(art(false, false, false), ArtNormal, "A1 常态行（不悬停 / 不按下 / 没选中）⇒ `" + ArtNormal + "`");
        C(art(false, true, false), ArtHover, "A2 悬停别的行 ⇒ `" + ArtHover + "`（`m_HighlightedSprite`）");
        C(art(true, false, false), ArtSelected, "A3 选中的那一行（打开时 = 当前语言行）⇒ `" + ArtSelected + "`（`m_SelectedSprite`）");
        C(art(true, true, false), ArtSelected,
          "A4 **悬停选中行仍是 `_selected`**（Selected 优先于 Highlighted；⛔ 不是 `hot ? hover : normal`）");
        C(art(false, false, true), ArtPress, "A5 按下别的行 ⇒ `" + ArtPress + "`（`m_PressedSprite`）");
        C(art(true, true, true), ArtPress, "A6 按下选中行 ⇒ `_press`（Pressed 优先于 Selected）");
        C(art(false, false, true), ArtPress,
          "A7 **按下后拖出去仍算按下**（`hover=假 / pressed=真` ⇒ 仍 `_press`；原版 `OnPointerExit` 不清 `isPointerDown`）");

        // A8 接线：`RefreshLangHover` 必须收得到「按住」这一位（否则上面那条状态机永远拿不到 down）。
        var miHover = typeof(SettingsPanel).GetMethod("RefreshLangHover", PrivInst);
        var ps = miHover != null ? miHover.GetParameters() : null;
        Tr(ps != null && ps.Length == 2 && ps[0].ParameterType == typeof(Vector3) && ps[1].ParameterType == typeof(bool),
           "A8 接线：`RefreshLangHover(Vector3 world, bool down)` —— 每帧的「按住」已经喂进语言行那一档");

        // A9 打开列表时「选中的那一行」= 当前语言那一行（原版 `Show():903` 的 `if (isOn) Select()`）。
        var miSel = typeof(SettingsPanel).GetMethod("LangSelRowForCurrent", PrivStatic);
        Tr(miSel != null, "A9 **前提**：反射拿到 `SettingsPanel.LangSelRowForCurrent`（private static）");
        if (miSel != null)
        {
            int want = -1;
            var langs = Loc.Languages;
            var cur = Loc.Current;
            for (int i = 0; langs != null && i < langs.Length; i++) if (langs[i] == cur) { want = i; break; }
            Tr(want >= 0, "A9a **前提**：`Loc.Current` 在 `Loc.Languages` 里找得到（找不到 ⇒ 下面那条会和 -1 一起假绿）");
            C((int)miSel.Invoke(null, null), want,
              "A9b 打开时被选中 = **当前语言**那一行（默认选中不是「第一行」，也不是「上次点的那行」）");
        }
    }

    // ============================================================ B · 四张图真的在、且是四张不同的图
    static void SectionB_Sprites()
    {
        MenuCheck.Section(_sink, "B 四张切片都在 `Resources/` 下、都是 717×92、且互不相同");

        var n = CardArt.MenuUi(ArtNormal);
        var h = CardArt.MenuUi(ArtHover);
        var p = CardArt.MenuUi(ArtPress);
        var s = CardArt.MenuUi(ArtSelected);
        Tr(n != null && h != null && p != null && s != null,
           "B0 四张图都取得到（`40K_dropdown_item` / `_hover` / `_press` / `_selected`）");
        // ⚠️ 不写成 `if (tex != null) Check(...)`（前提不成立时静默跳过 = 假绿）：取不到就喂 −1，四条一起红。
        C(p != null ? p.width : -1, 717, "B1 `_press` 宽 = 717 px（原版 `Sprite.m_Rect` 717×92）");
        C(p != null ? p.height : -1, 92, "B2 `_press` 高 = 92 px");
        C(s != null ? s.width : -1, 717, "B3 `_selected` 宽 = 717 px（同一族四个切片同尺寸）");
        C(s != null ? s.height : -1, 92, "B4 `_selected` 高 = 92 px");

        // B5 灭自证：四档必须是**四张不同的资产**（填成同一张 ⇒ 这条红）。
        bool distinct = n != null && h != null && p != null && s != null
                        && !ReferenceEquals(n, h) && !ReferenceEquals(n, p) && !ReferenceEquals(n, s)
                        && !ReferenceEquals(h, p) && !ReferenceEquals(h, s) && !ReferenceEquals(p, s);
        Tr(distinct, "B5 四档是**四张不同的图**（任何两张填成同一张 ⇒ 红）");
    }

    // ============================================================ C · 灭自证（结构上不可能同时满足的几条）
    static void SectionC_Discriminators()
    {
        MenuCheck.Section(_sink, "C 灭自证：改回两档 / 四档填同一张 / 删掉按下表 —— 三种「假绿」都要能红");

        var miArt = typeof(SettingsPanel).GetMethod("LangItemArtForTest", BindingFlags.Public | BindingFlags.Static);
        if (miArt == null) { Tr(false, "C0 `LangItemArtForTest` 反射失败 ⇒ 本段**无法核**（不是绿）"); return; }
        System.Func<bool, bool, bool, string> art =
            (cur, hov, prs) => (string)miArt.Invoke(null, new object[] { cur, hov, prs });

        // C1：四个语义不同的组合必须给出四个不同的名字（两档实现最多给出两个）。
        var k = new System.Collections.Generic.HashSet<string>
        {
            art(false, false, false), art(false, true, false), art(true, false, false), art(false, false, true)
        };
        C(k.Count, 4, "C1 四个语义组合 ⇒ 四个不同图名（只有 normal + hover 的实现最多 2 个）");

        // C2：两条优先级（Selected > Highlighted、Pressed > Selected）**同时**成立 —— 一条 `hot ? hover : normal` 过不了。
        Tr(art(true, true, false) != ArtHover, "C2a 悬停**选中行**不是 `_hover`（Selected 优先于 Highlighted）");
        Tr(art(true, true, true) != ArtSelected, "C2b 按下**选中行**不是 `_selected`（Pressed 优先于 Selected）");

        // C3：B-78 的落点依赖 `WindowButton.PressedNames` 那条**唯一规则**（⛔ 别删）。
        var miPN = typeof(WindowButton).GetMethod("PressedNameFor", PrivStatic);
        Tr(miPN != null, "C3 **前提**：反射拿到 `WindowButton.PressedNameFor`（private static）");
        if (miPN != null)
            C((string)miPN.Invoke(null, new object[] { ArtNormal }), ArtPress,
              "C4 `PressedNameFor(\"" + ArtNormal + "\")` = `" + ArtPress + "` —— 联盟下拉项按下态的**唯一来源**");
    }

    // ============================================================ D · B-78：按下图接线 + 空图名不记噪声
    static void SectionD_PressArtWire()
    {
        MenuCheck.Section(_sink, "D B-78：`art` 非空才有按下图；`art == null` 不再记一条无意义的 `\" → \"`");

        var miRec = typeof(WindowButton).GetMethod("RecordMissingArt", PrivStatic);
        Tr(miRec != null, "D0 **前提**：反射拿到 `WindowButton.RecordMissingArt`（private static）");
        if (miRec != null)
        {
            System.Func<string, bool> rec = (nm) => (bool)miRec.Invoke(null, new object[] { nm });
            C(rec(null), false, "D1 `RecordMissingArt(null)` = false（`art`/图名都空 ⇒ 没有可报的东西）");
            C(rec(""), false, "D1b `RecordMissingArt(\"\")` = false");
            C(rec(ArtPress), true, "D2 **灭自证**：`RecordMissingArt(\"40K_dropdown_item_press\")` = true（⛔ 不是「全都不记」）");
        }

        // D3：真 `Bind` 一遍（探针用完立刻销毁；记录也当场清回，不污染别的宿主那条「名单必须为空」）。
        var root = new GameObject("B112A_probe");
        try
        {
            var q = ImageQuad.Create(root.transform, CardArt.Solid(), Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "q");
            Tr(q != null, "D3a **前提**：探针 `ImageQuad` 建出来了（`CardArt.Solid()` 非空）");
            if (q != null)
            {
                var wb = root.AddComponent<WindowButton>();
                int s0 = WindowButton.MissingSwapArt.Count, p0 = WindowButton.MissingPressedArt.Count;
                wb.Bind(q, null);
                C(WindowButton.MissingSwapArt.Count, s0,
                  "D3b `Bind(q, art: null)` **不再**往 `MissingSwapArt` 塞一条 `\" → \"`");
                C(WindowButton.MissingPressedArt.Count, p0,
                  "D3c 同上：`MissingPressedArt` 也不塞（B-78 附带项）");

                wb.Bind(q, "B112A_ProbeArt_不存在");
                Tr(WindowButton.MissingSwapArt.Count == s0 + 1 && WindowButton.MissingPressedArt.Count == p0 + 1,
                   "D3d **灭自证**：图名非空且取不到时**照样记**（否则 D3b/D3c 可以靠「什么都不记」混过去）");
                WindowButton.MissingSwapArt.Remove("B112A_ProbeArt_不存在 → B112A_ProbeArt_不存在_hover");
                WindowButton.MissingPressedArt.Remove("B112A_ProbeArt_不存在 → B112A_ProbeArt_不存在_pressed");
                C(WindowButton.MissingSwapArt.Count, s0, "D3e 探针记录已清回（`MissingSwapArt`）");
                C(WindowButton.MissingPressedArt.Count, p0, "D3f 探针记录已清回（`MissingPressedArt`）");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
