// B142_E5_Assert.cs — 执行写手 `B142-E5`（第一百二十九会话 · 2026-11-04）· W129 三条**逐值参数**
//
// ============================ 它盯的是什么 ============================
//  | ID | 内容 | 原版判据（prefab 现读） |
//  |---|---|---|
//  | `B141-ITEMBG-TINT` | 社交下拉「条目底」染色（`AllianceMemberTab.cs:2546`，原来传 `null` = 不染色） | `Social Submenu Variant.prefab` **6 颗** `Item Background ▸ Image.m_Color` = `(1, 0.4739555, 0.0990566, 1)` |
//  | `B141-TINTS-ROUND` | 两族取整 tint → 逐位（4 处） | 社交 **16 颗** = `(1, 0.4745098, 0.09803922, 1)` · 设置窗 **11 颗** = `(0.28627452, 0.9647059, 0.6862745, 1)` |
//  | `B141-SETTINGS-STYLE` | 设置窗 **10 颗**非 0 `m_fontStyle`（4 Italic + 6 Underline） | `Main Menu Settings Window.prefab`（逐颗见 `判据/B142_对账_W129卡组收藏.md` 表 A） |
//  判据全文 → `判据/B142_对账_W129卡组收藏.md`（表 A / 表 C）+ `判据/B142_核销清单.md` §三 B。
//
//  ── 10 颗 `m_fontStyle` 落在 **7 个代码点**（`AcInput.Create` 一颗覆盖 4 颗 Placeholder）──
//  | # | 原版节点（父链） | `m_fontStyle` | 我们的落点（`Shell/SettingsWindow.cs`） |
//  |---:|---|---:|---|
//  | 1–4 | `Placeholder < Text Area < InputEmail/InputPassword < Account Form / Login Window` | 2（Italic） | `AcInput.Create`（`:6817`，**本类 Create 只被这 4 处调用**） |
//  | 5 | `Reset Password < Account Form < Account Tab` | 4（Underline） | `:2601` |
//  | 6 | `Forgot Password < Account Form < Account Tab` | 4 | `:2611` |
//  | 7 | `Forgot Password < Login Window < Account Tab` | 4 | `:3283` |
//  | 8 | `Subscribe Newsletter < Account Tab` | 4 | `:2634` |
//  | 9 | `Terms of Service Text < Terms of Service < bottom links < Support Tab` | 4 | `:3090` |
//  | 10 | `Button Text < Privacy Policy Button < Privacy Policy < bottom links < Support Tab` | 4 | `SpUrlButton(..., underlineText: true)`（`:3102` → `:3196`） |
//  | ⛔ 11 | `Button Text < Steam Button < old menu` | 4 | **不建** —— `old menu` 的 `m_IsActive: 0`、`decomp_full` 0 引用（死子树） |
//
// ============================ 断言 ============================
//  A 组（逐值 / 逐点接线；零前提，只读源码文本 + 公开常量）：
//    A1  `AllianceMemberTab.cs` 的 `Item Background` 那颗 tint = `new Color(1f, 0.4739555f, 0.0990566f, 1f)`（**不再是 `null`**）
//    A2  `SettingsWindow.GenFieldTint` 逐通道 = (0.28627452, 0.9647059, 0.6862745, 1)
//    A3  社交两文件里 `new Color(1f, 0.475f, 0.098f, …)`（旧取整）**0 处**
//    A4  `AllianceSearchTab.cs` 的 `bgTint:` = `new Color(1f, 0.4745098f, 0.09803922f, 1f)`
//    A5  `SetFontStyle(TMPro.FontStyles.Underline)` 在 `SettingsWindow.cs` 里 **6 处**（5/6/7/8/9/10 号）
//    A6  `SetFontStyle(TMPro.FontStyles.Italic)` **1 处**，且在 `AcInput.Create` 里对 `_placeholderLb`
//    A7  `AcInput.Create(` 调用点 **4 处**（⇒ A6 那一处正好覆盖 4 颗 Placeholder，而不是 1 颗也不是 5 颗）
//    A8  `SpUrlButton` 有形参 `bool underlineText = false`，且 `underlineText: true` 只传 **1 次**（= 只有 Privacy Policy）
//    A9  `Terms of Service Text` 那颗确实走 `termsLb.SetFontStyle(...)`（不是「建了没染」）
//    A10 第 11 颗（`Steam Button`）**没有**被实现（`SettingsWindow.cs` 不含 `Steam Button`）
//    A11 同族收尾①：下拉箭头 = `(0.36078432, 0.1882353, 0.05882353, 1)`（原版 `Arrow` 4 颗）
//    A12 同族收尾②：成员行左侧竖带 = `(0.4811321, 0.1822391, 0, 1)`（原版 `Image` 2 颗，`m_Sprite = null` 纯色块）
//    A13 同族收尾③：行底（`RowBase`）= `(0.9811321, 0.46865106, 0.35635456, 1)`（原版 3 颗 `background`）
//    A14 成员行行底（非玩家支）= `(1, 0.3596305, 0, 1)`（原版 `background < Alliance Member Entry` 2 颗）
//    A15 4 颗橙钮 → 3 个代码点：`(1, 0.4739555, 0.0990566, 1)` 在 `AllianceSearchTab.cs` 里 **3 处**
//    A16 `RowButton` 新增 `Color? tint`，缺省 `null` = 旧行为
//    A17 `RowButton` 把 `tint` 真传进 `Nine(...)`（⛔ 不是收下不用）
//    A18 建盟页 `Price Display Button ▸ Generic UI Button` 那颗 `Nine` 直接带同一抹橙
//  B 组（灭自证）：
//    B1  `GenFieldTint` 逐通道都 **≠** 旧取整 (0.286, 0.965, 0.686)
//    B2  社交「条目底」那族 **≠** 下拉底板那族（0.4739555 vs 0.4745098）—— 两族**不是同一个值**，⛔ 别归一
//    B3  反向：下拉底板那条 **≠** 条目底那族（改回归一 ⇒ B2/B3 一起红）
//    B3b 灭自证：箭头 / 行内竖带那两处都**不再是**旧取整 (0.361,0.188,0.0588) 与 (0.481,0.182,0)
//    B3c 灭自证对照：「未传 tint ⇒ 仍然是白」——`MenuDraw.Nine` 只在 `tint.HasValue` 时 `SetTint`
//    B3d 对照：`ImageQuad` 出厂染色确实是白（`_vertexTintColor = Color.white`）
//    B4  `Label.SetFontStyle` 仍是 `_tmp.fontStyle |= s`（改回 `=` ⇒ Italic 与 Underline 互相覆盖）
//    B5  运行时探针：新建 `Label` 出厂不含这两个位；先 Italic 再 Underline ⇒ **两位都在**（= `|=` 语义真的成立）
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · 把 `Item Background` 的 tint 改回 `null` / 改成底板那族 0.4745098 ⇒ **A1 / B2** 红。
//  · 把任一处逐位 tint 改回 3 位取整 ⇒ **A3**（旧取整归零那条）或 **A2 / B1** 红。
//  · 删掉任一句 `SetFontStyle` ⇒ **A5 / A6 / A9** 红（按处数咬）。
//  · 把 `SpUrlButton` 的 `underlineText` 缺省改真、或给 FAQ/Contact/Support 也传 true ⇒ **A8** 红。
//  · `Label.SetFontStyle` 改成 `_tmp.fontStyle = s` ⇒ **B4 / B5** 红（第二档会把第一档冲掉）。
//
// ============================ 怎么接（本文件不碰宿主聚合）============================
//  `public static void Run();` —— **零前提**（读 3 份源码文本 + 1 个公开常量 + 一个用完即销毁的探针节点）。
//  挂法（由调度台决定）：同一物理行追加
//      B142_E5_Assert.Run(); pass += B142_E5_Assert.Pass; fail += B142_E5_Assert.Fail;
//  判绿红：按行首 `B142E5 ✓` / `B142E5 ✗` 数（⛔ 别 `grep -c ✗`）。

using System.IO;
using CardPresentation;
using TMPro;
using UnityEngine;

/// <summary>执行写手 `B142-E5`：W129 三条逐值参数（ITEMBG-TINT / TINTS-ROUND / SETTINGS-STYLE）的回归断言。</summary>
public static class B142_E5_Assert
{
    const string Tag = "B142E5";
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _fail; } }

    /// <summary>本批 `<c>SetFontStyle</c>` 代码点数 = **7**（其中 `AcInput.Create` 那一处覆盖 4 颗 `Placeholder`）。</summary>
    public const int StyleCodePoints = 7;
    /// <summary>原版非 0 <c>m_fontStyle</c> 的**可见**颗数 = **10**（第 11 颗 `Steam Button` 在死子树里）。</summary>
    public const int StylePrefabNodes = 10;

    static bool Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
        return ok;
    }

    static string Src(string rel)
    {
        try { return File.ReadAllText(Application.dataPath + "/CardPresentation/" + rel); }
        catch (System.Exception e) { Debug.LogError(Tag + " 读不到 `" + rel + "`：" + e.Message); return ""; }
    }

    static int Count(string hay, string needle)
    {
        if (string.IsNullOrEmpty(hay) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = hay.IndexOf(needle, i, System.StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    static bool Near(float v, float want) { return Mathf.Abs(v - want) <= 1e-6f; }
    static bool Far(float v, float old) { return Mathf.Abs(v - old) > 1e-6f; }
    static string Fmt(Color c)
    {
        return "(" + c.r.ToString("F8") + ", " + c.g.ToString("F8") + ", " + c.b.ToString("F8")
             + ", " + c.a.ToString("F4") + ")";
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        GroupA_PerValue();
        GroupB_AntiSelfProof();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail
                  + "（代码点 " + StyleCodePoints + " 个 → 原版可见颗数 " + StylePrefabNodes + " 颗）");
    }

    // ==================================================================
    //  A · 逐值 / 逐点接线
    // ==================================================================
    static void GroupA_PerValue()
    {
        var mt = Src("Shell/AllianceMemberTab.cs");
        var st = Src("Shell/AllianceSearchTab.cs");
        var sw = Src("Shell/SettingsWindow.cs");

        // ---- A1 · ITEMBG-TINT（6 颗 Item Background）----
        Check(mt.Contains("\"Item Background\", QItemBg, new Color(1f, 0.4739555f, 0.0990566f, 1f), false);"),
              "A1 社交「条目底」染色 = `(1, 0.4739555, 0.0990566, 1)`（原版 6 颗 `Item Background ▸ Image.m_Color` 逐位；改前传 `null` = 整档不染色）");

        // ---- A2 · GenFieldTint 逐通道（11 颗）----
        var ge = SettingsWindow.GenFieldTint;
        Check(Near(ge.r, 0.28627452f) && Near(ge.g, 0.9647059f) && Near(ge.b, 0.6862745f) && Near(ge.a, 1f),
              "A2 设置窗 `GenFieldTint` 逐通道 = (0.28627452, 0.9647059, 0.6862745, 1)（原版 11 颗；容差 1e-6）｜实得 " + Fmt(ge));

        // ---- A3 · 旧取整 0 处 ----
        int oldMt = Count(mt, "new Color(1f, 0.475f, 0.098f");
        int oldSt = Count(st, "new Color(1f, 0.475f, 0.098f");
        Check(oldMt == 0 && oldSt == 0,
              "A3 社交两文件里旧取整 `new Color(1f, 0.475f, 0.098f, 1f)` 已 **0 处**｜实得 `AllianceMemberTab` "
              + oldMt + " 处 · `AllianceSearchTab` " + oldSt + " 处");

        // ---- A4 · 搜索页 bgTint ----
        Check(st.Contains("bgTint: new Color(1f, 0.4745098f, 0.09803922f, 1f)"),
              "A4 `AllianceSearchTab` 建盟输入框 `bgTint` = `(1, 0.4745098, 0.09803922, 1)`（原版 16 颗同值那一族）");

        // ---- A5 / A6 · SetFontStyle 代码点（6 Underline + 1 Italic）----
        int under = Count(sw, "SetFontStyle(TMPro.FontStyles.Underline)");
        int italic = Count(sw, "SetFontStyle(TMPro.FontStyles.Italic)");
        Check(under == 6,
              "A5 `SetFontStyle(Underline)` 在 `SettingsWindow.cs` 里 **6 处**（原版 6 颗：Reset Password / Forgot Password×2 / "
              + "Subscribe Newsletter / Terms of Service Text / Privacy Policy Button Text）｜实得 " + under);
        Check(italic == 1 && sw.Contains("f._placeholderLb.SetFontStyle(TMPro.FontStyles.Italic)"),
              "A6 `SetFontStyle(Italic)` **1 处**且落在 `AcInput.Create` 的 `_placeholderLb` 上（原版 4 颗 `Placeholder` 同族）｜实得 " + italic);

        // ---- A7 · AcInput.Create 只被 4 处调用（⇒ A6 正好覆盖 4 颗，不多不少）----
        int acCreate = Count(sw, "AcInput.Create(");
        Check(acCreate == 4,
              "A7 `AcInput.Create(` 调用点 = **4 处**（`Account Form` 的 InputEmail/InputPassword + `Login Window` 的同两颗）"
              + " ⇒ A6 那一句正好覆盖 4 颗 `Placeholder`｜实得 " + acCreate);

        // ---- A8 · SpUrlButton 的 underlineText 只给 Privacy Policy ----
        Check(sw.Contains("bool keepAspect = false, bool underlineText = false)"),
              "A8 `SpUrlButton` 有 `underlineText` 形参（缺省 false ⇒ 同族另三颗不染）");
        int ut = Count(sw, "underlineText: true");
        Check(ut == 1 && sw.Contains("SpPrivacyUrl), keepAspect: true, underlineText: true)"),
              "A8b `underlineText: true` 只传 **1 次**、且传在 `Privacy Policy Button` 那一处（原版只有它那颗 `Button Text` 带下划线）｜实得 " + ut);

        // ---- A9 · Terms of Service Text 真的染上了 ----
        Check(sw.Contains("var termsLb = SpText(terms, \"Terms of Service Text\"")
              && sw.Contains("termsLb.SetFontStyle(TMPro.FontStyles.Underline)"),
              "A9 `Terms of Service Text` 那颗建出来后确实调了 `SetFontStyle(Underline)`（⛔ 不是「建了没染」）");

        // ---- A10 · 第 11 颗在死子树里 ⇒ 不实现 ----
        Check(!sw.Contains("Steam Button"),
              "A10 第 11 颗 `Button Text < Steam Button < old menu` **没被实现**（`old menu` 的 `m_IsActive: 0` 且 `decomp_full` 0 引用 ⇒ 永久不显示）");

        // ---- A11 / A12 · 同族取整收尾（社交 prefab 的 Arrow ×4 / 行内竖带 ×2）----
        int arrow = Count(mt, "new Color(0.36078432f, 0.1882353f, 0.05882353f, 1f)");
        Check(arrow == 2,
              "A11 下拉箭头（合上面 / 展开面各一句）= `(0.36078432, 0.1882353, 0.05882353, 1)`"
              + "（原版 `Arrow` 4 颗：两个下拉 × 两个 Variant）｜实得 " + arrow + " 处");
        Check(mt.Contains("new Color(0.4811321f, 0.1822391f, 0f, 1f)"),
              "A12 成员行左侧竖带 = `(0.4811321, 0.1822391, 0, 1)`（原版 `Image < background < Alliance Member Entry` 2 颗；"
              + "那颗 `m_Sprite = {fileID: 0}` ⇒ 纯色块，与我们 `CardArt.Solid()` 等价）");
        Check(st.Contains("new Color(0.9811321f, 0.46865106f, 0.35635456f, 1f)"),
              "A13 两种行族共用的行底（`RowBase`）= `(0.9811321, 0.46865106, 0.35635456, 1)`（原版 3 颗 `background` 逐位）");

        // ---- A14–A18 · 第三批：成员行行底 + 4 颗橙钮（`40K_button` × 橙）----
        Check(mt.Contains("new Color(1f, 0.3596305f, 0f, 1f)") && !mt.Contains("new Color(1f, 0.36f, 0f, 1f)"),
              "A14 成员行行底（非玩家那一支）= `(1, 0.3596305, 0, 1)`（原版 `background < Alliance Member Entry` 2 颗，"
              + "sprite `40K_dropdown_bg_0`）；旧取整 (1, 0.36, 0) 已 0 处");
        int btnOrange = Count(st, "new Color(1f, 0.4739555f, 0.0990566f, 1f)");
        Check(btnOrange == 3,
              "A15 4 颗橙钮 → **3 个代码点**（邀请行 `Join` 模板一处覆盖 2 颗 + 公开行 `Generic UI Button` + 建盟页 `Price Display Button` 的 `Generic UI Button`）"
              + "｜实得 " + btnOrange + " 处");
        Check(st.Contains("bool interactable = true, Color? tint = null)"),
              "A16 `RowButton` 新增 `Color? tint`，**缺省 `null` = 旧行为**（`null` ⇒ `Nine` 不 `SetTint` ⇒ 白）");
        Check(st.Contains("\"Bg\", L_Btn, tint);"),
              "A17 `RowButton` 把 `tint` 一路传到 `Nine(...)` 的 tint 实参（⛔ 不是收下不用）");
        Check(st.Contains("\"Generic UI Button\", L_Btn, new Color(1f, 0.4739555f, 0.0990566f, 1f))"),
              "A18 建盟页 `Price Display Button ▸ Generic UI Button` 那颗 `Nine` 直接带同一抹橙（原版同值同图）");
    }

    // ==================================================================
    //  B · 灭自证
    // ==================================================================
    static void GroupB_AntiSelfProof()
    {
        var mt = Src("Shell/AllianceMemberTab.cs");

        // ---- B1 · GenFieldTint ≠ 旧取整 ----
        var ge = SettingsWindow.GenFieldTint;
        Check(Far(ge.r, 0.286f) && Far(ge.g, 0.965f) && Far(ge.b, 0.686f),
              "B1 **灭自证**：`GenFieldTint` 逐通道都**不等于**旧取整 (0.286, 0.965, 0.686)（改回取整 ⇒ B1 红）｜实得 " + Fmt(ge));

        // ---- B2 / B3 · 两族**不是同一个值**（⛔ 别归一）----
        Check(mt.Contains("\"Item Background\", QItemBg, new Color(1f, 0.4739555f, 0.0990566f, 1f), false);")
              && !mt.Contains("\"Item Background\", QItemBg, new Color(1f, 0.4745098f"),
              "B2 **灭自证**：社交「条目底」那族 = 0.4739555，**不是**下拉底板那族 0.4745098（两族不同件；归一 ⇒ B2 红）");
        Check(mt.Contains("QListBg, new Color(1f, 0.4745098f, 0.09803922f, 1f)")
              && !mt.Contains("QListBg, new Color(1f, 0.4739555f"),
              "B3 反向：下拉底板那条仍是 0.4745098，**不是**条目底那族 0.4739555（两边同时被改成同一个值 ⇒ B2/B3 一起红）");

        // ---- B3b · 同族取整收尾的两处**不是**取整值 ----
        Check(!mt.Contains("new Color(0.361f, 0.188f, 0.0588f")
              && !mt.Contains("new Color(0.481f, 0.182f, 0f"),
              "B3b **灭自证**：箭头 / 行内竖带那两处都**不再是**旧取整 (0.361,0.188,0.0588) 与 (0.481,0.182,0)（改回 ⇒ B3b 红）");

        // ---- B3c / B3d · 「未传 tint ⇒ 仍然是白」的对照（🧨 反过来 ⇒ 这两条红）----
        var md = Src("Shell/MenuDraw.cs");
        var iq = Src("Battle/ImageQuad.cs");
        Check(md.Contains("if (tint.HasValue) q2.SetTint(tint.Value);"),
              "B3c **灭自证**：`MenuDraw.Nine` 仍是 `if (tint.HasValue) q2.SetTint(tint.Value);`"
              + "（改成无条件 `SetTint(tint ?? Color.white)` 之外的写法 / 改成给缺省上色 ⇒ 「未传 = 白」这条不再成立 ⇒ B3c 红）");
        Check(iq.Contains("_vertexTintColor = Color.white;"),
              "B3d 对照：`ImageQuad` 的出厂染色确实是**白**（`_vertexTintColor = Color.white`）⇒ 「缺省 tint = null ⇒ 画白」这条可核，"
              + "不是「缺省其实染了别的色」");

        // ---- B4 · `|=` 语义（改回 `=` ⇒ 后一档把前一档冲掉）----
        var lb = Src("Battle/Label.cs");
        Check(lb.Contains("_tmp.fontStyle |= s;"),
              "B4 `Label.SetFontStyle` 仍是 `_tmp.fontStyle |= s;`（改回 `=` ⇒ Italic 与 Underline 互相覆盖）");

        // ---- B5 · 运行时探针：两位能同时存在 ----
        GameObject go = null;
        try
        {
            go = new GameObject("B142E5_probe");
            var probe = Label.Create(go.transform, "Wg", Vector3.zero, 1, Color.white,
                                     new Vector2(0.5f, 0.5f), "B142E5_probe_lb");
            if (!Check(probe != null, "B5 前提：探针 `Label` 建得出来（建不出来 ⇒ 下面两条无从谈起）")) return;

            bool clean = (probe.FontStyleNow & (FontStyles.Italic | FontStyles.Underline)) == 0;
            Check(clean, "B5b 出厂 `fontStyle` 不含 Italic/Underline（现读 = " + (int)probe.FontStyleNow
                         + "）⇒ 下面那两位只可能来自 `SetFontStyle`，不是全局默认");

            probe.SetFontStyle(FontStyles.Italic);
            probe.SetFontStyle(FontStyles.Underline);
            var now = probe.FontStyleNow;
            Check((now & FontStyles.Italic) == FontStyles.Italic && (now & FontStyles.Underline) == FontStyles.Underline,
                  "B5c 先 Italic 再 Underline ⇒ **两位都在**（现读 = " + (int)now + "）；`|=` 改成 `=` 时这一条必红");
        }
        catch (System.Exception e)
        {
            Check(false, "（B5）探针抛异常：" + e);
        }
        finally
        {
            if (go != null) Object.DestroyImmediate(go);
        }
    }
}
