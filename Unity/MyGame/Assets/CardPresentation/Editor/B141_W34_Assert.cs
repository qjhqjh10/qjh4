// B141_W34_Assert.cs — 执行写手 `B141-W34`（第一百二十八会话 · 2026-11-04）· W129 **P6 包 7 条**
//
// ============================ 它盯的是什么 ============================
//  `判据/B141_W129_对账_卡组色彩.md` §二 的 P6 包 = `5-03 · 5-06 · 5-07 · 5-08 · 5-14 · 5-17 · 5-18`
//  （`Shell/WindowButton.cs` · `Shell/GiveFeedbackPopup.cs` · `Shell/ParentEventWindow.cs` ·
//   `Shell/BoosterPackOpenWindow.cs`）。逐条原版值 / 我们改后值 → `报告/B141_W129_ButtonFx.md`。
//
//  | 编号 | 一句话 | 判据出处（原版资产 + 字段） |
//  |---|---|---|
//  | 5-03 | Give Feedback 41 颗绿复选框四态 + 禁用色 | `Assets/Give Feedback Popup menu.prefab` `Checkbox*` 的 `m_Colors` |
//  | 5-06 | `m_Colors.m_ColorMultiplier`（15 颗 = 2.0） | `Assets/DeckSelectionContinueButton.prefab` 等 15 颗 |
//  | 5-07 | `m_SpriteState.m_SelectedSprite`（77/1276 非空） | `bundle_menus_assets_all` 逐颗 |
//  | 5-08 | `m_DisabledColor` 三族 + α 两档 | 同包 1276 颗逐颗 `m_Colors.m_DisabledColor` |
//  | 5-14 | 页签 `m_HighlightedColor` 浅蓝 + 禁用色 | `Assets/Parent Event Window.prefab` `Menu Navigation Panel Button` |
//  | 5-17 | 630 颗 SpriteSwap 换图（逐颗 `m_SpriteState`） | `bundle_menus_assets_all` / 普查表 |
//  | 5-18 | 开包稀有度色 `CardRarityColorsSO` 6 档（原样不 clamp） | `bundle_menus_assets_all/MonoBehaviour/CardRarityColorsSO.json` |
//
// ============================ 断言 ============================
//  A 组（纯值，零前提）：
//    A1  5-03 复选框四态 + 禁用色**逐位**等于 prefab（5 个常量）
//    A2  5-06 乘 + Clamp01 的算式：白×2 = 白；0.9608×2 / 0.7843×2 **都饱和到 1**
//    A3  5-07 `WindowButton.Bind` 的 `selectedArt` 形参存在且缺省 null（反射）
//    A4  5-08 `DisabledKey` / `GrayOnDisableAlpha` 出厂值
//    A5  5-14 页签悬停色 / 禁用色**逐位**等于 prefab
//    A6  5-18 稀有度 6 档**逐位**等于 SO，**含两个异常值原样**（α=0 · r=1.282163>1 不 clamp）
//    A7  5-18 越界档位落第 0 档（同上 `Core/RarityColors` 的 fallback 口径）
//  B 组（源码结构，零前提 —— 只读 4 个 `.cs` 文本）：
//    B1~B4 5-03：四态 + 禁用色真的赋给了复选框的 `WindowButton`，且**真的** `TintOn(togQ)`
//    B5~B6 5-06：`ApplyTint` 走 `ComposeVertexColor(底座, 色键, 乘数)`（= 乘完再夹）；且旧写法**不在**了；两个站点真的写了 `= 2f`
//    B7~B8 5-08：`KeyOf` 里禁用档直接返回 `DisabledKeyNow`；`DisabledKeyNow` 的 α 只在 `ColorTintGreyOnDisable` 时改写
//    B9~B10 5-07：`TexOf(Selected)` 走 `_selectedTex ?? _normalTex`；`Bind` 里 `_selectedTex` 真的被赋过
//    B11~B12 5-14：`WireTabHover` 存在、且**内容页签 / 事件钮两处都调**
//    B13~B14 5-18：开包建卡那一路**真的**调了 `v.SetOutline(BoosterRarityColor(...))`
//    B15~B17 5-17：`WindowButton` 仍保留「按名推 + 显式覆盖」两条路（逐颗映射靠 `Bind(...)` 的
//          `hoverArt/pressedArt` 显式实参），且 `MissingSwapArt` 两条记录口都在
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · 复选框退回白底（`CheckNormal` 改成 `Color.white`）⇒ A1 + B1 红。
//  · 删 `cbWb.TintOn(togQ)` ⇒ B2 红（A 组管不到接线，这条是唯一证据）。
//  · `ColorMultiplier` 缺省改成 2 ⇒ B5b（`= 1f` 那条）红。
//  · 把 `ApplyTint` 改回「先 `Clamp01(色键 × m)` 再乘底座」⇒ B5 + B5c 红；把夹取整个删掉（只乘不夹）⇒ B5 红。
//    （✅ 2026-11-04 · `B141-W57` 同步：正确口径 = **乘完再夹**，判据见 `ComposeVertexColor` 的 doc。）
//  · 把 `KeyOf` 的禁用档改回 `NormalKey` ⇒ B7 红；把 α 无条件写死 0.502 ⇒ B8 红。
//  · 把 `TexOf(Selected)` 改回无条件 `_normalTex` ⇒ B9 红（5-07 的换图链就断在这里）。
//  · 删掉任意一处 `WireTabHover(...)` ⇒ B11/B12 红。
//  · 给稀有度表加 `Mathf.Clamp01` / 把 r 改成 1.0 ⇒ A6 红（**这一条正是 5-18 的题目**）。
//
// ============================ 怎么接（本文件不碰宿主聚合）============================
//  `public static void Run();` —— **零前提**（只读 4 个公共常量组 + 4 份源码文本；⛔ 不建窗口、不写盘）。
//  挂法（由调度台决定）：同一物理行追加
//      B141_W34_Assert.Run(); pass += B141_W34_Assert.Pass; fail += B141_W34_Assert.Fail;
//  判绿红：按行首 `B141W34 ✓` / `B141W34 ✗` 数（⛔ 别 `grep -c ✗`）。

using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B141_W34`：W129 P6 包（5-03/06/07/08/14/17/18）的回归断言。</summary>
public static class B141_W34_Assert
{
    const string Tag = "B141W34";
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static bool Near(Color a, Color b, float eps = 1e-6f)
    {
        return Mathf.Abs(a.r - b.r) < eps && Mathf.Abs(a.g - b.g) < eps
            && Mathf.Abs(a.b - b.b) < eps && Mathf.Abs(a.a - b.a) < eps;
    }

    static string Src(string rel)
    {
        try { return File.ReadAllText(Application.dataPath + "/CardPresentation/" + rel); }
        catch (System.Exception e) { Debug.LogError(Tag + " 读不到 `" + rel + "`：" + e.Message); return ""; }
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        GroupA_Values();
        GroupB_Wiring();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail);
    }

    // ==================================================================
    //  A · 纯值（判据 = 原版 prefab / SO 逐字段现读）
    // ==================================================================
    static void GroupA_Values()
    {
        // ---- A1 · 5-03：41 颗复选框的四态 + 禁用色（`Give Feedback Popup menu.prefab` 逐颗同值） ----
        Check(Near(GiveFeedbackPopup.CheckNormal, new Color(0.28627452f, 0.9647059f, 0.6862745f, 1f)),
              "A1 5-03 复选框 `m_NormalColor` = (0.28627452, 0.9647059, 0.6862745, 1)｜实得 " + GiveFeedbackPopup.CheckNormal);
        Check(Near(GiveFeedbackPopup.CheckHover, new Color(0.29214135f, 0.9528302f, 0.6816804f, 1f)),
              "A1b 5-03 `m_HighlightedColor` = (0.29214135, 0.9528302, 0.6816804, 1)｜实得 " + GiveFeedbackPopup.CheckHover);
        Check(Near(GiveFeedbackPopup.CheckPressed, new Color(0.30584726f, 0.8207547f, 0.61052626f, 1f)),
              "A1c 5-03 `m_PressedColor` = (0.30584726, 0.8207547, 0.61052626, 1)｜实得 " + GiveFeedbackPopup.CheckPressed);
        Check(Near(GiveFeedbackPopup.CheckSelected, new Color(0.2901961f, 0.9529412f, 0.68235296f, 1f)),
              "A1d 5-03 `m_SelectedColor` = (0.2901961, 0.9529412, 0.68235296, 1)｜实得 " + GiveFeedbackPopup.CheckSelected);
        Check(Near(GiveFeedbackPopup.CheckDisabled, new Color(0.78431374f, 0.78431374f, 0.78431374f, 1f)),
              "A1e 5-03 复选框禁用色 = (0.78431374 ×3, **a 1.0** —— prefab 存 0.5019608，"
              + "但 `grey=1 + colorTintGreyOnDisableAlpha 1.0` 会被 `Awake` 重写成 1)｜实得 " + GiveFeedbackPopup.CheckDisabled);

        // ---- A2 · 5-06：底色**白**时的饱和（= UGUI 顶点色 Color32 的饱和行为） ----
        //  🔴 **2026-11-04（B141-W57）就地订正口径**：原版是 `tintColor * colorMultiplier` **先不夹**、
        //     乘在底座上、落进 `UIVertex.color`（`Color32`）时才逐通道饱和
        //     ⇒ 算式 = `Clamp01(base × key × m)`（唯一实现 = `WindowButton.ComposeVertexColor`）。
        //     ⚠️ 这一组**底座是白**，所以「夹色键」与「夹乘积」**结果相同**（白 × x = x）
        //     ⇒ 本条**照旧成立**，但它证明的是「白底座上的饱和」，不是「夹在哪一步」。
        //     「夹在哪一步」由 `B141_W57_Assert` 的 A3/B 组钉住（底座非白时两种口径不同）。
        Color mul2(Color c, float m) => new Color(Mathf.Clamp01(c.r * m), Mathf.Clamp01(c.g * m),
                                                 Mathf.Clamp01(c.b * m), Mathf.Clamp01(c.a * m));
        var w = mul2(Color.white, 2f);
        Check(Near(w, Color.white), "A2 5-06 白 × 2 ⇒ Clamp01 后仍是白（顶点色饱和）｜实得 " + w);
        var hl = mul2(new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f), 2f);
        Check(Near(hl, Color.white), "A2b 5-06 悬停键 0.9608 × 2 = 1.9216 ⇒ **饱和到 1**（= 悬停零变化）｜实得 " + hl);
        var pr = mul2(new Color(0.7843137f, 0.7843137f, 0.7843137f, 1f), 2f);
        Check(pr.r > 1f - 1e-4f, "A2c 5-06 按下键 0.7843 × 2 = 1.5686 ⇒ **饱和到 1**｜实得 " + pr);

        // ---- A3 · 5-07：`Bind` 的 `selectedArt` 形参（缺省 null = 原版那 1199 颗） ----
        var bind = typeof(WindowButton).GetMethod("Bind", new[]
        {
            typeof(ImageQuad), typeof(string), typeof(string), typeof(string), typeof(string)
        });
        Check(bind != null, "A3 5-07 `WindowButton.Bind` 有第 5 个形参（selectedArt）");
        if (bind != null)
        {
            var ps = bind.GetParameters();
            Check(ps[4].IsOptional && ps[4].DefaultValue == null,
                  "A3b 5-07 `selectedArt` **缺省 null**（⛔ 缺省值不许给一张「我们挑的图」）｜实得 "
                  + (ps[4].DefaultValue == null ? "null" : ps[4].DefaultValue.ToString()));
        }

        // ---- A4 · 5-08：禁用色的出厂值（= 原版 1276 颗里最大的那一族 + UGUI 默认 α） ----
        var wbType = typeof(WindowButton);
        var probe = new GameObject("B141W34_probe", typeof(RectTransform));
        var wbc = probe.AddComponent<WindowButton>();
        Check(Near(wbc.DisabledKey, new Color(0.7843137f, 0.7843137f, 0.7843137f, 0.5019608f)),
              "A4 5-08 `DisabledKey` 出厂 = (0.7843137 ×3, a 0.5019608)｜实得 " + wbc.DisabledKey);
        Check(Mathf.Abs(wbc.GrayOnDisableAlpha - 1f) < 1e-6f,
              "A4b 5-08 `GrayOnDisableAlpha` 出厂 = **1.0**（原版 ctor `+0x17C` 写 `0x3f800000`）｜实得 " + wbc.GrayOnDisableAlpha);
        Check(Mathf.Abs(wbc.ColorMultiplier - 1f) < 1e-6f,
              "A4c 5-06 `ColorMultiplier` 出厂 = **1.0**（1252/1276 颗；⛔ 缺省不许是 2）｜实得 " + wbc.ColorMultiplier);
        Object.DestroyImmediate(probe);

        // ---- A5 · 5-14：页签的悬停色 / 禁用色（`Parent Event Window.prefab ▸ Menu Navigation Panel Button`） ----
        Check(Near(ParentEventWindow.TabHoverKey, new Color(0.6084906f, 0.94261944f, 1f, 1f)),
              "A5 5-14 页签 `m_HighlightedColor` = (0.6084906, 0.94261944, 1, 1)｜实得 " + ParentEventWindow.TabHoverKey);
        Check(Near(ParentEventWindow.TabDisabledKey, new Color(0.8018868f, 0.8018868f, 0.8018868f, 1f)),
              "A5b 5-14 页签 `m_DisabledColor` = (0.8018868 ×3, a 1)（B131 色 A12 的「青族」）｜实得 " + ParentEventWindow.TabDisabledKey);

        // ---- A6 · 5-18：`CardRarityColorsSO` 六档**逐位**（含两个异常值） ----
        var t = BoosterPackOpenWindow.BoosterRarityColors;
        Check(t != null && t.Length == 6, "A6 5-18 稀有度色表是 **6 档**｜实得 " + (t == null ? "<null>" : t.Length.ToString()));
        if (t != null && t.Length == 6)
        {
            Check(Near(t[0], new Color(1f, 1f, 1f, 1f)), "A6a 档 0 (None) = 白 a1｜实得 " + t[0]);
            Check(Near(t[1], new Color(0.03671235218644142f, 0.26070666313171387f, 0.31132078170776367f, 0f)),
                  "A6b 档 1 (Common) **α = 0 原样保留**（B131 B11 的异常值）｜实得 " + t[1]);
            Check(Near(t[2], new Color(0.07176104187965393f, 0.5660377740859985f, 0.05606977641582489f, 0.6666666865348816f)),
                  "A6c 档 2 (Rare)｜实得 " + t[2]);
            Check(Near(t[3], new Color(0.5647059082984924f, 0.054901957511901855f, 0.525894820690155f, 0.6666666865348816f)),
                  "A6d 档 3 (Epic)｜实得 " + t[3]);
            Check(Near(t[4], new Color(1.282163381576538f, 0.470490038394928f, 0f, 0.6666666865348816f), 1e-5f),
                  "A6e 档 4 (Legendary) **r = 1.282163 > 1 原样不 clamp**（B131 B11）｜实得 " + t[4]);
            Check(t[4].r > 1f, "A6f 档 4 的 r **必须 > 1**（这一条就是 5-18 的题目；改成 1.0 ⇒ 红）｜实得 " + t[4].r);
            Check(Near(t[5], new Color(0.8235294222831726f, 0.16078431904315948f, 0f, 0.6392157077789307f), 1e-5f),
                  "A6g 档 5 (Special)｜实得 " + t[5]);
        }

        // ---- A7 · 5-18：越界 ⇒ 第 0 档 ----
        Check(Near(BoosterPackOpenWindow.BoosterRarityColor(-1), t[0]), "A7 5-18 档 -1 ⇒ 第 0 档");
        Check(Near(BoosterPackOpenWindow.BoosterRarityColor(99), t[0]), "A7b 5-18 档 99 ⇒ 第 0 档");
    }

    // ==================================================================
    //  B · 接线（源码结构；灭自证的另一半）
    // ==================================================================
    static void GroupB_Wiring()
    {
        var wbSrc = Src("Shell/WindowButton.cs");
        var fbSrc = Src("Shell/GiveFeedbackPopup.cs");
        var peSrc = Src("Shell/ParentEventWindow.cs");
        var bpSrc = Src("Shell/BoosterPackOpenWindow.cs");

        // ---- B1/B2 · 5-03 ----
        Check(fbSrc.Contains("cbWb.NormalKey = CheckNormal;") && fbSrc.Contains("cbWb.HighlightKey = CheckHover;")
              && fbSrc.Contains("cbWb.PressedKey = CheckPressed;") && fbSrc.Contains("cbWb.SelectedKey = CheckSelected;")
              && fbSrc.Contains("cbWb.DisabledKey = CheckDisabled;"),
              "B1 5-03 复选框把**四态 + 禁用色**逐格赋给了 `WindowButton`");
        Check(fbSrc.Contains("cbWb.TintOn(togQ)"),
              "B2 5-03 色偏目标 = `Toggle` 底那颗（`m_TargetGraphic`）—— `TintOn(togQ)` 真的在（删掉 ⇒ 悬停看不见）");
        Check(!fbSrc.Contains("MenuDraw.Rect(tog, Tex(ArtDropdownBg, \"复选框底\"), togR, \"Image\", QRow, CheckNormal"),
              "B3 5-03 底色**没有**顺手写成绿（那会和 `NormalKey` 再乘一次 = 绿×绿）");

        // ---- B5/B6 · 5-06 ----
        // ✅ 2026-11-04（`B141-W57`）同步：机制从「先 Clamp01(色键 × m) 再乘底座」改成
        //   「乘完再夹」（= `ComposeVertexColor`，判据 = UGUI 那条链，见该方法的 doc）。
        //   ⇒ 这里改钉**新机制**，并**反向**钉住旧写法不许回来（⛔ 两条一起看才算数）。
        Check(wbSrc.Contains("ComposeVertexColor(b, _k, ColorMultiplier)"),
              "B5 5-06 `ApplyTint` 走 `ComposeVertexColor(底座, 色键, 乘数)`（= 乘完再夹的唯一口）");
        Check(!wbSrc.Contains("Mathf.Clamp01(k.r * ColorMultiplier)"),
              "🧨 B5c 旧口径「先 Clamp01(色键 × 乘数) 再乘底座」**已不存在**（改回去 ⇒ 红；G/B 会退回 0.8941 / 0.5874）");
        Check(wbSrc.Contains("public static Color ComposeVertexColor(Color baseColor, Color key, float multiplier)")
              && wbSrc.Contains("Mathf.Clamp01(baseColor.r * key.r * multiplier)"),
              "B5d 唯一算式 `Clamp01(底座 × 色键 × 乘数)` 就在 `WindowButton.ComposeVertexColor` 里（两套按钮共用这一处）");
        Check(wbSrc.Contains("public float ColorMultiplier = 1f;"),
              "B5b 5-06 `ColorMultiplier` 缺省 = **1f**（⛔ 缺省不许写成 2）");
        Check(peSrc.Contains("teamWb.ColorMultiplier = 2f;"),
              "B6 5-06 `Two Side Event Select Team A/B` 那一颗真的写了 `= 2f`（判据 = 原版 15 颗之一）");
        Check(fbSrc.Contains("scoreWb.ColorMultiplier = 2f;"),
              "B6b 5-06 `Feedback Scoring Button` 也写了 `= 2f`（原版 `trans=0` ⇒ 四态零变化，这一乘把我们的悬停色压平）");

        // ---- B7/B8 · 5-08 ----
        Check(wbSrc.Contains("if (!_interactable) return DisabledKeyNow;"),
              "B7 5-08 `KeyOf` 的禁用档真的返回 `DisabledKeyNow`（改回 `NormalKey` ⇒ 禁用色永远不生效）");
        Check(wbSrc.Contains("if (ColorTintGreyOnDisable) c.a = GrayOnDisableAlpha;"),
              "B8 5-08 α 只在 `colorTintGreyOnDisable` 时被重写（原版 `Awake` / `SetUpButtonColors` 那个闸）");
        Check(wbSrc.Contains("|| tintOnHover) ApplyState();"),
              "B8b 5-08 `Interactable` setter 对**纯色偏**（未 `Bind`）的钮也重跑状态出口");

        // ---- B9/B10 · 5-07 ----
        Check(wbSrc.Contains("case BtnState.Selected: return _selectedTex ?? _normalTex;"),
              "B9 5-07 `TexOf(Selected)` 走 `_selectedTex ?? _normalTex`（改回无条件 `_normalTex` ⇒ 77 颗永远不换选中图）");
        Check(wbSrc.Contains("_selectedTex = string.IsNullOrEmpty(selectedArt) ? null : CardArt.MenuUi(selectedArt);"),
              "B10 5-07 `Bind` 里 `_selectedTex` 真的被赋过（⛔ 不按名字推）");
        Check(wbSrc.Contains("public static readonly System.Collections.Generic.List<string> MissingSelectedArt ="),
              "B10b 5-07 「传了选中文案却取不到图」有出声表（红线：不许静默失败）");

        // ---- B11/B12 · 5-14 ----
        Check(peSrc.Contains("static void WireTabHover(Transform tab, Transform hit)"),
              "B11 5-14 `WireTabHover` 存在（一份实现，两处共用）");
        int calls = CountOf(peSrc, "WireTabHover(t, ");
        Check(calls >= 2, "B12 5-14 内容页签 + 事件钮**两处都调**了 `WireTabHover`（现读 " + calls + " 处调用）");
        Check(peSrc.Contains("wb.HighlightKey = TabHoverKey;"), "B12b 5-14 悬停色真的写进 `WindowButton`");

        // ---- B13/B14 · 5-18 ----
        Check(bpSrc.Contains("v.SetOutline(BoosterRarityColor(RarityInt(Cards[i])))"),
              "B13 5-18 开包建卡那一路真的把稀有度色打到 `Card Highlight And Shadow` 的 `_Outline` 上");
        Check(!bpSrc.Contains("Mathf.Clamp01(BoosterRarityColor"),
              "B14 5-18 稀有度色**没有**被 Clamp（原版 r = 1.282163 必须原样）");

        // ---- B15/B16/B17 · 5-17 ----
        Check(wbSrc.Contains("string hn = hoverArt ?? HoverNameFor(art);")
              && wbSrc.Contains("string pn = pressedArt ?? PressedNameFor(art);"),
              "B15 5-17 「按名推 + 显式覆盖」两条路都还在（逐颗 `m_SpriteState` 靠 `Bind(... hoverArt, pressedArt)` 显式给）");
        Check(wbSrc.Contains("MissingSwapArt.Add(") && wbSrc.Contains("MissingPressedArt.Add("),
              "B16 5-17 悬停图 / 按下图两条缺图记录口都在（红线：不许静默失败）");
        Check(wbSrc.Contains("static readonly System.Collections.Generic.Dictionary<string, string> HoverNames")
              && wbSrc.Contains("static readonly System.Collections.Generic.Dictionary<string, string> PressedNames"),
              "B17 5-17 两张例外表（hover / pressed）未被删");
        Check(wbSrc.Contains("{ \"40K_settings_button_hover\",      \"40K_settings_button_selected\" }"),
              "B17b 5-17 新补的 hover 例外：`40K_settings_button_hover → …_selected`（13 颗）");
        Check(wbSrc.Contains("{ \"UI_Button_Organe_Square_Normal\", \"UI_Button_Organe_Square_Pressed\" }"),
              "B17c 5-17 新补的 pressed 例外：`UI_Button_Organe_Square_Normal → …_Pressed`（2 颗）");
        Check(wbSrc.Contains("{ \"40K_dropdown_field_closed\",      \"40K_dropdown_field_opened\" }"),
              "B17d 5-17 新补的 pressed 例外：`40K_dropdown_field_closed → …_opened`（11 颗）");
    }

    static int CountOf(string s, string sub)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(sub)) return 0;
        int n = 0, i = 0;
        while ((i = s.IndexOf(sub, i, System.StringComparison.Ordinal)) >= 0) { n++; i += sub.Length; }
        return n;
    }
}
