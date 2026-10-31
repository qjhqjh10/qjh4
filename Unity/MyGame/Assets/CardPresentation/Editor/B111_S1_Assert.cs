// B111_S1_Assert.cs — 第一百一十一会话 · 执行代理 `B111_S1`（`B-63` · `B-65`）
//
// ============================ 它盯的是什么 ============================
//  `B-63`：`Shell/WindowButton.cs` 的 `PressedNames` 表**没有** `40K_dropdown_item → 40K_dropdown_item_press`。
//    `PressedNameFor(art)` 的默认规则是 `art + "_pressed"`，而**本族原版用的是 `_press`**（`Toggle.m_SpriteState`
//    四张切片：`40K_dropdown_item` / `_hover` / `_press` / `_selected`；盘上**没有** `40K_dropdown_item_pressed`）。
//    ⇒ 任何**不显式传 `pressedArt`** 的调用点都会去找 `…_pressed`，**静默落进 `MissingPressedArt`**（只出声、不断言）。
//    判据 = `资料/完全复刻/报告/B110_W7.md` §「顺手发现 #2」（本代理已回源复核，见报告 §②）。
//  `B-65`：`Battle/SettingsPanel.cs:727` / `:1985` 两处注释写「按下那一档没落 ⇒ 因为 `40K_dropdown_item_press`
//    本地 `Resources/` 里没有」。**图 2026-10-31 已经落进 `Resources/Art/ui_deck/` 了** ⇒ 那半句过期；
//    但**行为**上本扇确实仍只有 normal + hover（`RefreshLangHover` 只换 `_hover`）⇒ 注释已按「图有了 / 接线没做」
//    **分开写**。本件只断「注释指向的那两个常量名还在」，⛔ **不断言行为**（那要真机，见报告 §⑤）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A1：把 `PressedNames` 里那条删掉（= 退回默认规则）⇒ A1 红、A2 红。
//  · C1：把表的默认规则全局改成 `art + "_press"`（另一条能让 A1 变绿的"捷径"）⇒ A1/A2 绿但 **C2 红**
//        （`40K_button` 那六族的原版名都是 `_pressed`）。⇒ **「表里加一条」与「改默认规则」结构上不可能同时满足**。
//  · C3：把这条映射填成 hover 那张图（`40K_dropdown_item_hover`）⇒ A1/C3 红（两张图必须是不同资产）。
//  · B1/B2：把 `Resources/Art/ui_deck/40K_dropdown_item_press.png` 删掉 ⇒ B1（`Resources.Load` 取不到）/ B2 红。
//  · B6：`PressedNames` 里任何一条指向盘上不存在的图 ⇒ B6 红（这条顺便守住其余六族）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  放到「跑得到设置窗」的那条宿主里（`CardPresentation/Editor/SettingsScene.cs` 最稳 —— 两处注释都在
//  `Battle/SettingsPanel.cs`，`WindowButton` 是外壳/战斗共用件）。在它那条聚合行末尾**同一物理行**追加：
//      B111S1_Assert.Run(); pass += B111S1_Assert.Pass; fail += B111S1_Assert.Fail;
//  ⚠️ 追加时先看该行第一个 `//` 在哪 —— 文本必须落在它**之前**（2026-10-30 那次 17 个断言类被注释吞掉的坑）。
//
// ============================ 判据 / 报告 ============================
//  · 原版：`bundle_duplicateassetisolation_assets_all/Sprite/40K_dropdown_item_press.json`（`m_Name` / `m_Rect=717×92`）
//  · 报告：`资料/完全复刻/报告/B111_S1.md`
using System.IO;
using System.Reflection;
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B111_S1`：`40K_dropdown_item` 的**按下态图名**（`B-63`）+ 两处注释（`B-65`）。</summary>
public static class B111S1_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B111S1] ");

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ---- 反射口 ----
    //  ⚠️ `WindowButton.PressedNameFor` / `HoverNameFor` / `PressedNames` **都是 private**（无修饰符）。
    //  本件不改它们的可见性（那是 `Shell/WindowButton.cs` 的公共面，另一批人的活）；
    //  反射是这里唯一不扩面子的读法 —— `Bind()` 的第 346 行**逐字**就是 `PressedNameFor(pressedArt ?? …)`，
    //  ⇒ 断到 `PressedNameFor` 就等于断到「所有不显式传 `pressedArt` 的调用点」。
    const BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Static;
    static readonly MethodInfo MI_PressedName = typeof(WindowButton).GetMethod("PressedNameFor", Priv);
    static readonly MethodInfo MI_HoverName = typeof(WindowButton).GetMethod("HoverNameFor", Priv);
    static readonly FieldInfo FI_PressedNames = typeof(WindowButton).GetField("PressedNames", Priv);

    static string PressedNameFor(string art)
    {
        return MI_PressedName == null ? null : (string)MI_PressedName.Invoke(null, new object[] { art });
    }
    static string HoverNameFor(string art)
    {
        return MI_HoverName == null ? null : (string)MI_HoverName.Invoke(null, new object[] { art });
    }
    static System.Collections.Generic.Dictionary<string, string> PressedTable
    {
        get
        {
            return FI_PressedNames == null
                 ? null
                 : (System.Collections.Generic.Dictionary<string, string>)FI_PressedNames.GetValue(null);
        }
    }

    /// <summary>本族常态图 / 原版按下图的名字（逐字；`40K` 是大写 K、`_press` 不是 `_pressed`）。</summary>
    const string ArtNormal = "40K_dropdown_item", ArtPressed = "40K_dropdown_item_press";
    /// <summary>默认规则拼出来的那个**原版不存在**的名字 —— 旧行为会拿它去 `Resources` 里找。</summary>
    const string ArtWrongDefault = ArtNormal + "_pressed";

    public static void Run()
    {
        MenuCheck.Section(_sink, "B111_S1：`40K_dropdown_item` 按下态图名（B-63）+ SettingsPanel 注释所指数常量（B-65）");
        SectionA_Table();
        SectionB_Resource();
        SectionC_Discriminators();
        SectionD_CommentConsistency();
        Debug.Log(_sink.P + "B111S1 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 表与取名规则
    static void SectionA_Table()
    {
        MenuCheck.Section(_sink, "A `PressedNames` 里必须有 `40K_dropdown_item → 40K_dropdown_item_press`");

        // A0 前提（硬检查：反射口拿不到就下面几条**不是**「绿」，而是没法判）
        Tr(MI_PressedName != null && MI_HoverName != null,
           "A0 **前提**：反射拿到 `WindowButton.PressedNameFor` / `HoverNameFor`（private static）");
        Tr(PressedTable != null, "A0b **前提**：反射拿到 `WindowButton.PressedNames`（private static 表）");
        if (MI_PressedName == null || MI_HoverName == null || PressedTable == null) return;

        // A1 表里那一条（比 `PressedNameFor` 更直接：抓「有人把规则改巧了、表里却还是没有」）
        string tableVal = null;
        C(PressedTable.TryGetValue(ArtNormal, out tableVal), true,
          "A1 `PressedNames` 里有 `" + ArtNormal + "` 这一条（删掉 ⇒ 退回默认规则 `_pressed`）");
        C(tableVal, ArtPressed, "A2 且它的值 = `" + ArtPressed + "`（本族是 `_press`，不是 `_pressed`）");

        // A3/A4：不显式传 pressedArt 的调用点实际会拿到哪个名字（= `Bind` 第 346 行走的那条路）
        C(PressedNameFor(ArtNormal), ArtPressed,
          "A3 **不显式传 `pressedArt`** 的调用点取到的名字 = `" + ArtPressed + "`（`Bind` 里 `PressedNameFor(art)` 那条）");
        Tr(PressedNameFor(ArtNormal) != ArtWrongDefault,
          "A4 且**不是** `" + ArtWrongDefault + "`（原版无此名 ⇒ 旧写法必然落 `MissingPressedArt`）");

        // A5：悬停那一档不受影响（本族 `_hover` 本来就走默认规则）
        C(HoverNameFor(ArtNormal), ArtNormal + "_hover",
          "A5 悬停那一档仍是 `" + ArtNormal + "_hover`（这张盘上有；⛔ 别为了修按下态把 hover 也塞进表里）");
    }

    // ============================================================ B · 资源真的在
    static void SectionB_Resource()
    {
        MenuCheck.Section(_sink, "B `40K_dropdown_item_press` 确实在 `Resources/` 下、且能被 `CardArt.MenuUi` 取到");

        var tex = CardArt.MenuUi(ArtPressed);
        Tr(tex != null, "B1 `CardArt.MenuUi(\"" + ArtPressed + "\")` 取到图（`Resources.Load` 走 `Art/ui_menu → ui_deck → ui` 三级兜底）");
        // ⚠️ 这两条**不写成 `if (tex != null) Check(...)`**（前提不成立时静默跳过 = 假绿）：
        //    取不到就喂 −1 ⇒ B1 红、B2/B3 也红，三条一起出声。
        C(tex != null ? tex.width : -1, 717, "B2 图宽 = 717 px（原版 `Sprite.m_Rect` 717×92）");
        C(tex != null ? tex.height : -1, 92, "B3 图高 = 92 px（同上；NPOT ⇒ `.meta` 必须是 `nPOTScale: 0`，见 B6）");

        string png = Application.dataPath + "/CardPresentation/Resources/Art/ui_deck/" + ArtPressed + ".png";
        string meta = png + ".meta";
        Tr(File.Exists(png), "B4 盘上文件在：" + png);
        Tr(File.Exists(meta), "B5 同目录有 `.meta`（未导入的裸 PNG 不算落好）");

        // B6：Unity 自己生成的 `.meta` 会给 NPOT 写 `nPOTScale: 1`（ToNearest）⇒ 717×92 会被放大。
        //   这一条是 2026-10-31 那张图落盘时特意手写的字段，守住它。
        bool npotOk = false;
        if (File.Exists(meta))
        {
            string txt = File.ReadAllText(meta);
            npotOk = txt.Contains("nPOTScale: 0");
        }
        Tr(npotOk, "B6 `.meta` 里 `nPOTScale: 0`（717×92 是 NPOT；Unity 默认会写 1 ⇒ 贴图被放大）");

        // B7：表里**每一条**都指得着图（这条顺手守住其余六族，别让这张表烂掉）
        var table = PressedTable;
        if (table == null) { Tr(false, "B7 `PressedNames` 表取不到（反射失败）⇒ 全表覆盖**无法核**（不是绿）"); return; }
        int miss = 0;
        string firstMiss = null;
        foreach (var kv in table)
        {
            if (CardArt.MenuUi(kv.Value) == null) { miss++; if (firstMiss == null) firstMiss = kv.Key + " → " + kv.Value; }
        }
        C(miss, 0, "B7 `PressedNames` 全表 " + table.Count + " 条**都能取到图**"
                   + (miss > 0 ? "（取不到 " + miss + " 条，第一条 = " + firstMiss + "）" : ""));
    }

    // ============================================================ C · 灭自证
    static void SectionC_Discriminators()
    {
        MenuCheck.Section(_sink, "C 灭自证：改回默认名 / 改默认规则 / 填成 hover 那张 —— 三种「假绿」都要能红");

        // C1：旧默认名在盘上**确实不存在** ⇒ 「表改回去」这条路的失败是**可观测**的
        Tr(CardArt.MenuUi(ArtWrongDefault) == null,
           "C1 `" + ArtWrongDefault + "` 取不到（原版无此名）⇒ 退回默认规则必落 `MissingPressedArt`");

        // C2：**另一条能让 A2/A3 变绿的捷径**是「把默认规则全局改成 `_press`」——
        //     而其余六族的原版名都是 `_pressed`（`40K_button_pressed` / `40k_menu_bt_pressed` …）
        //     ⇒ A2 与 C2 **结构上不可能同时满足**，唯一解 = 在表里逐条写。
        C(PressedNameFor("40K_button"), "40K_button_pressed",
          "C2 默认规则仍是 `_pressed`：`40K_button → 40K_button_pressed`（全局改成 `_press` ⇒ A2 绿、本条红）");

        // C3：按下图与悬停图必须是**两张不同的资产**（防「表填对了名字、却填成 hover 那张」）
        var hi = CardArt.MenuUi(HoverNameFor(ArtNormal));
        var pr = CardArt.MenuUi(ArtPressed);
        Tr(hi != null && pr != null && !ReferenceEquals(hi, pr),
           "C3 按下图 ≠ 悬停图（两张都是独立资产；填成同一张 ⇒ 红）");
    }

    // ============================================================ D · B-65：注释指向的常量还在
    static void SectionD_CommentConsistency()
    {
        MenuCheck.Section(_sink, "D B-65：`SettingsPanel` 两处注释指的就是这两个常量名（**只钉文档面，行为面见报告 §⑤**）");

        var fiA = typeof(SettingsPanel).GetField("LstItemArt", Priv);
        var fiH = typeof(SettingsPanel).GetField("LstItemHiArt", Priv);
        Tr(fiA != null && fiH != null,
           "D0 **前提**：反射拿到 `SettingsPanel.LstItemArt` / `LstItemHiArt`（private const）");
        if (fiA == null || fiH == null) return;

        C((string)fiA.GetRawConstantValue(), ArtNormal,
          "D1 `LstItemArt` = `" + ArtNormal + "`（:727 那条注释指的名字）");
        C((string)fiH.GetRawConstantValue(), ArtNormal + "_hover",
          "D2 `LstItemHiArt` = `" + ArtNormal + "_hover`（:1985 那条注释说「只接了悬停」的那一张）");
    }
}
