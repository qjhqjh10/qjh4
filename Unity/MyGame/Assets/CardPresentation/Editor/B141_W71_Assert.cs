// B141_W71_Assert.cs — 执行写手 `B141-W71`（2026-11-04 · 第一百二十八会话）· 条目 `B141-DISABLED-ALPHA`
//
// ============================ 它盯的是什么 ============================
//  `B141_W129_Mult2Clamp.md` §六 ❓2 抓到的那一条：**两套按钮机制的 α 语义不同** ——
//    · `Shell/WindowButton.ApplyTint`：算出 `Clamp01(底座 × 色键 × m)` 之后**把 α 换回底座 α**（`c.a = b.a`）
//      ⇒ 状态色的 α **永远不生效**（禁用档 `m_DisabledColor.a = 0.5019608` 画出来仍是**不透明**）。
//    · `Battle/BattleButtonFx.Apply`：四通道一起乘 ⇒ 禁用档**真的是半透明**。
//  判据（本机现读，逐跳）：
//    ① `Selectable.DoStateTransition` → `StartColorTween(tintColor * m_Colors.colorMultiplier, instant)`
//       （`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Selectable.cs:702`）
//       —— Unity 的 `Color * float` **四通道同乘，含 α**；
//    ② `StartColorTween` 末句 `m_TargetGraphic.CrossFadeColor(targetColor, …, true, **true**)`
//       （同文件 `:1091`，第 4 个实参就是 `useAlpha`）⇒ α **确实参与补间**；
//    ③ `Graphic.CrossFadeColor` 把 `targetColor` 原样交给 `canvasRenderer.SetColor`（同包 `Graphic.cs:1012-1028`）；
//    ④ 顶点色容器 = `UIVertex.color` = `Color32`（`VertexHelper.cs:273`）⇒ 夹在**最后一次字节写入**。
//  ⇒ 唯一算式 = `Clamp01(底座 × 色键 × colorMultiplier)`，**四通道，α 一起**；两侧共用 `WindowButton.ComposeVertexColor`。
//
// ============================ 断言 ============================
//  A 组（纯算式，零前提 —— 期望值**自己算**，⛔ 不从被测实现读回来）：
//    A1  α 也乘：白(α1) × 禁用色(α0.5019608) × 1 ⇒ α = **0.5019608**（不是 1.0）
//    A2  α 乘完再夹：白 × 禁用色 × 2 ⇒ 1.0039216 ⇒ **1.0**
//    A3  🧨 与「先夹色键再乘底座」**必须不同**（α 0.9×0.8×2 = 1.44 ⇒ 1.0 vs 旧序 0.9）
//    A4  🧨 与 `WindowButton` 旧口径（α 取底座 α = 1.0）**必须不同**（差 > 0.4）
//    A5  RGB 不受影响（白底 × 禁用色 ⇒ 0.7843137）
//  B 组（**行为**：两套机制在 m = 1 / 2 的五个态上逐通道相等，且都等于公式值）：
//    B{m}N / B{m}H / B{m}P / B{m}S / B{m}D —— 每档两条：🅐 = 公式值 · 🅑 = 两套机制相同
//      （`Selected` 只有 `WindowButton` —— `BattleButtonFx` 没有 Selected 态，见其常量 doc）
//    B{m}Dα —— 禁用档的 α **显式**等于 `底座α × 0.5019608 × m`（夹到 1）
//  C 组（源码守卫 · 灭自证的另一半，⛔ 不许两边一起改回去）：
//    C1  `WindowButton.ApplyTint` 里**没有**把 α 换回底座那一句（`c.a = b.a`）
//    C2  `ComposeVertexColor(b, _k, ColorMultiplier)` 之后**直接** `_tq[i].SetTint(c)`，中间没有 α 覆写
//    C3  `BattleButtonFx.Apply` 仍走**同一个**静态口，且它**没有**自己的 α 覆写
//    C4  唯一算式仍只有一份（`Mathf.Clamp01(baseColor.a * key.a * multiplier)` 在 `WindowButton`）
//  D 组（回归：`grey = 1` 那一族**不受本笔影响**）：
//    D1  默认 `ColorTintGreyOnDisable = true` + 乘数 1 ⇒ 禁用档 α = **1.0**（原版把 `m_DisabledColor.a`
//        重写成 `colorTintGreyOnDisableAlpha`）⇒ 本笔不许把这一族画成半透明
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · `WindowButton` 改回「α 取底座 α」（`c.a = b.a`）⇒ C1 + C2 + B1D🅐/B1Dα + A4 红。
//  · `WindowButton` 干脆不叠 α（α 恒 1）⇒ B1D🅐/B1Dα 红（期望 0.5019608）。
//  · `BattleButtonFx` 把 α 换回底座 ⇒ B1D🅑 红（两侧不等）。
//  · 把 `ComposeVertexColor` 的 α 通道删成 `baseColor.a` ⇒ A1/A2/A3 + 所有 D 组红。
//  · 两侧一起改成「α 取底座」⇒ B 组可能同绿，但 **A1/A4（自算期望）必红** ⇒ 这条结构位挡住了「一起改回去」。
//
// ============================ 怎么接（⚠️ 本件不碰宿主聚合）============================
//  `public static void Run();` —— **零前提**（自带临时根 + 收尾 `DestroyImmediate`；不碰场景 / 存档 / 真实 HUD / 资源）。
//  挂法（由调度台决定）：同一物理行追加
//      B141_W71_Assert.Run(); pass += B141_W71_Assert.Pass; fail += B141_W71_Assert.Fail;
//  判绿红：按**行首** `B141W71 ✓` / `B141W71 ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。

using System;
using System.IO;
using CardPresentation;
using UnityEngine;
using UnityEngine.UI;

/// <summary>执行写手 `B141_W71`：禁用档 α 也乘状态色（两套按钮机制在 N/H/P/S/D 五档逐通道相等）。</summary>
public static class B141_W71_Assert
{
    const string Tag = "B141W71";
    static int _pass, _fail;

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static bool Near(Color a, Color b, float eps = 1e-5f)
    {
        return Mathf.Abs(a.r - b.r) < eps && Mathf.Abs(a.g - b.g) < eps
            && Mathf.Abs(a.b - b.b) < eps && Mathf.Abs(a.a - b.a) < eps;
    }

    static string Show(Color c)
    {
        return "(" + c.r.ToString("F7") + ", " + c.g.ToString("F7") + ", "
             + c.b.ToString("F7") + ", " + c.a.ToString("F7") + ")";
    }

    // 原版字面量（`Battle Arena 1.unity` GO779 MB4262 + Image MB4210 现读；⛔ 不从被测常量读回来）
    static readonly Color Green = new Color(0.3686274886f, 0.8941176534f, 0.5874372721f, 1f);
    static readonly Color GreenHL = new Color(0.4666666985f, 0.9137255549f, 0.4000000358f, 1f);
    static readonly Color GreenPressed = new Color(0.4784314036f, 0.4941176772f, 0.4823529720f, 1f);
    static readonly Color GreenSelected = new Color(0.3686274588f, 0.8941177130f, 0.5882353187f, 1f);
    /// <summary>原版 `m_DisabledColor`（`grey = 0` 那一族：α **保留 0.5019608** —— 本笔要它真的生效）。</summary>
    static readonly Color DisabledKey = new Color(0.7843137383f, 0.7843137383f, 0.7843137383f, 0.5019608140f);

    /// <summary>期望值**自己算**（⛔ 不读被测实现）：`Clamp01(底座 × 色键 × 乘数)`，四通道。</summary>
    static Color Want(Color baseColor, Color key, float m)
    {
        return new Color(Mathf.Clamp01(baseColor.r * key.r * m), Mathf.Clamp01(baseColor.g * key.g * m),
                         Mathf.Clamp01(baseColor.b * key.b * m), Mathf.Clamp01(baseColor.a * key.a * m));
    }

    /// <summary>**旧口径 A**（W34 改前）：先夹色键、再乘底座，α 取底座 —— 只在 A3/A4 里当靶子。</summary>
    static Color OldTintFirst(Color baseColor, Color key, float m)
    {
        var k = new Color(Mathf.Clamp01(key.r * m), Mathf.Clamp01(key.g * m),
                          Mathf.Clamp01(key.b * m), Mathf.Clamp01(key.a * m));
        return new Color(baseColor.r * k.r, baseColor.g * k.g, baseColor.b * k.b, baseColor.a);
    }

    // ============================================================ 入口
    public static void Run()
    {
        _pass = 0; _fail = 0;
        A_Formula();
        var root = new GameObject("B141_W71_Root");
        try { B_BothMechanismsSame(root.transform); }
        catch (Exception e)
        {
            Check(false, "B 组抛异常（后面的断言**没跑到**，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message
                       + "\n" + e.StackTrace);
        }
        // 🔴 2026-11-05（`B148`）：原来在 `finally` 里就 `DestroyImmediate(root)`，而紧跟的 `D_GreyFamilyUnchanged(root.transform)` 要读它 ⇒ MissingReferenceException，`RunCore` 在 `ShellScene.cs:11301` 中断（其后断言一条没跑）。销毁挪到 D 组之后。
        C_SourceGuards();
        D_GreyFamilyUnchanged(root.transform); UnityEngine.Object.DestroyImmediate(root);
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ============================================================ A · 纯算式
    static void A_Formula()
    {
        // A1 · α 也乘（本笔的核心）
        //   自算：1 × 0.5019608 × 1 = 0.5019608
        var a1 = Want(Color.white, DisabledKey, 1f);
        Check(Mathf.Abs(a1.a - 0.5019608f) < 1e-6f,
              "A1 禁用色 α 也乘：白(α1) × 0.5019608 × 1 = **0.5019608**（不是 1.0）｜实得 " + a1.a.ToString("F7"));

        // A2 · α 乘完再夹
        var a2 = Want(Color.white, DisabledKey, 2f);
        Check(Mathf.Abs(a2.a - 1f) < 1e-6f,
              "A2 α 乘完再夹：0.5019608 × 2 = 1.0039216 ⇒ **1.0**｜实得 " + a2.a.ToString("F7"));

        // A3 · 🧨 与「先夹色键再乘底座」不同（α 上也能分辨）
        //   自算：0.9 × 0.8 × 2 = 1.44 ⇒ 1.0（乘完再夹） vs 旧序 Clamp01(0.8×2)=1.0 × 0.9 = 0.9
        var a3base = new Color(1f, 1f, 1f, 0.9f);
        var a3key = new Color(1f, 1f, 1f, 0.8f);
        var a3 = Want(a3base, a3key, 2f);
        var a3old = OldTintFirst(a3base, a3key, 2f);
        Check(Mathf.Abs(a3.a - 1f) < 1e-6f && Mathf.Abs(a3.a - a3old.a) > 0.05f,
              "🧨 A3 α 也是「乘完再夹」：新 1.0 vs 旧序 " + a3old.a.ToString("F4")
              + "（把夹取挪回色键上 ⇒ 本条红）");

        // A4 · 🧨 与 `WindowButton` 旧口径（α = 底座 α）必须不同
        Check(Mathf.Abs(a1.a - 1f) > 0.4f,
              "🧨 A4 旧口径（α 取底座 = 1.0）与新值 0.5019608 **差 > 0.4**（改回去 ⇒ 本条红）");

        // A5 · RGB 不受影响
        var a5 = Want(Color.white, DisabledKey, 1f);
        Check(Near(new Color(a5.r, a5.g, a5.b, 0.5019608f), DisabledKey, 1e-6f),
              "A5 RGB 不变：白底 × 禁用色 ⇒ 0.7843137（本笔只动 α）｜实得 " + Show(a5));
    }

    // ============================================================ B · 两套机制 × 五个态 × 两个乘数
    static void B_BothMechanismsSame(Transform parent)
    {
        B_OneMultiplier(parent, 1f);
        B_OneMultiplier(parent, 2f);
    }

    static void B_OneMultiplier(Transform parent, float m)
    {
        string M = m.ToString("F0");            // 断言标签后缀
        ImageQuad face = null;
        var wb = MakeButton(parent, Green, out face);
        var piece = MakePiece(parent, Green);
        if (wb == null || face == null || piece == null)
        {
            Check(false, "B" + M + " 夹具没建出来（`ImageQuad.Create` / `HudPiece.Of` 返 null）");
            return;
        }
        // 🔴 `grey = 0` 那一族：α **保留 prefab 里的 0.5019608**（= 原版 66 颗 `grey=0 + trans=1 + a=0.502`）
        wb.ColorTintGreyOnDisable = false;
        wb.NormalKey = Green; wb.HighlightKey = GreenHL;
        wb.PressedKey = GreenPressed; wb.SelectedKey = GreenSelected;
        wb.DisabledKey = DisabledKey;
        wb.ColorMultiplier = m;
        var fx = BattleButtonFx.Tint(piece, "W71 Continue m=" + M, Green, GreenHL, GreenPressed, DisabledKey, m);
        if (fx == null) { Check(false, "B" + M + "b `BattleButtonFx.Tint` 返 null"); return; }
        fx.ApplyInstant();

        // ---- Normal ----
        wb.ApplyTintInstant();
        var wbN = face.Tint;
        Check(Near(wbN, Want(Green, Green, m), 1e-5f),
              "B" + M + "N🅐 `WindowButton` Normal = 公式值｜实得 " + Show(wbN));
        Check(Near(wbN, piece.Tint, 1e-5f),
              "B" + M + "N🅑 **两套机制同一组顶点色**（WB " + Show(wbN) + " vs FX " + Show(piece.Tint) + "）");

        // ---- Highlighted ----
        wb.Enter(); wb.ApplyTintInstant();
        fx.SetHover(true);
        var wbH = face.Tint;
        Check(Near(wbH, Want(Green, GreenHL, m), 1e-5f),
              "B" + M + "H🅐 `WindowButton` Highlighted = 公式值｜实得 " + Show(wbH));
        Check(Near(wbH, piece.Tint, 1e-5f), "B" + M + "H🅑 悬停两侧相同（" + Show(wbH) + "）");

        // ---- Pressed ----
        wb.Press(); wb.ApplyTintInstant();
        fx.SetPressed(true);
        var wbP = face.Tint;
        Check(Near(wbP, Want(Green, GreenPressed, m), 1e-5f),
              "B" + M + "P🅐 `WindowButton` Pressed = 公式值｜实得 " + Show(wbP));
        Check(Near(wbP, piece.Tint, 1e-5f), "B" + M + "P🅑 按下两侧相同（" + Show(wbP) + "）");

        // ---- Selected（`BattleButtonFx` 没有 Selected 态 ⇒ 只对公式）----
        wb.Release(); wb.Exit(); wb.SetSelected(true); wb.ApplyTintInstant();
        var wbS = face.Tint;
        Check(Near(wbS, Want(Green, GreenSelected, m), 1e-5f),
              "B" + M + "S `WindowButton` Selected = 公式值（战斗侧类无 Selected 态）｜实得 " + Show(wbS));
        wb.SetSelected(false);

        // ---- Disabled ----
        fx.SetPressed(false); fx.SetHover(false);
        wb.Interactable = false;
        fx.SetDisabled(true);
        var wbD = face.Tint;
        var wantD = Want(Green, DisabledKey, m);
        Check(Near(wbD, wantD, 1e-5f),
              "B" + M + "D🅐 `WindowButton` Disabled = 公式值｜实得 " + Show(wbD) + " 期望 " + Show(wantD));
        Check(Near(wbD, piece.Tint, 1e-5f),
              "B" + M + "D🅑 **禁用档两侧相同**（WB " + Show(wbD) + " vs FX " + Show(piece.Tint) + "）");
        Check(Mathf.Abs(wbD.a - Mathf.Clamp01(1f * 0.5019608f * m)) < 1e-5f,
              "B" + M + "Dα 禁用档 α = 底座α × 0.5019608 × " + M + " = "
              + Mathf.Clamp01(1f * 0.5019608f * m).ToString("F7") + "｜实得 " + wbD.a.ToString("F7"));

        wb.Interactable = true;
        // 夹具全部挂在 `root` 下 ⇒ 收尾由 `Run()` 的 `DestroyImmediate(root)` 一并清掉。
    }

    // ============================================================ C · 源码守卫（灭自证）
    static void C_SourceGuards()
    {
        var wbSrc = Src("Shell/WindowButton.cs");
        var fxSrc = Src("Battle/BattleButtonFx.cs");

        Check(!wbSrc.Contains("c.a = b.a"),
              "🧨 C1 `WindowButton.ApplyTint` 里**没有**「把 α 换回底座」那一句（改回去 ⇒ 红）");

        int call = wbSrc.IndexOf("ComposeVertexColor(b, _k, ColorMultiplier)", StringComparison.Ordinal);
        int set = call >= 0 ? wbSrc.IndexOf("_tq[i].SetTint(c)", call, StringComparison.Ordinal) : -1;
        bool callOnce = call >= 0 && set > call;
        string mid = callOnce ? wbSrc.Substring(call, set - call) : "";
        Check(callOnce && mid.Length > 0 && !mid.Contains(".a ="),
              "C2 `ComposeVertexColor(b, _k, ColorMultiplier)` 之后**直接**落顶点色，中间没有 α 覆写"
              + (callOnce ? "" : "（⛔ 找不到那次调用）"));

        Check(fxSrc.Contains("WindowButton.ComposeVertexColor(_base, _key, 1f)"),
              "C3🅐 `BattleButtonFx.Apply` 仍走**同一个**静态口");
        Check(!fxSrc.Contains(".a = _base.a") && !fxSrc.Contains(".a = _key.a"),
              "C3🅑 战斗侧**没有**自己的 α 覆写（两侧 α 语义从本笔起一致）");

        Check(wbSrc.Contains("Mathf.Clamp01(baseColor.a * key.a * multiplier)"),
              "C4 唯一算式的 **α 通道**就在 `ComposeVertexColor`（`Clamp01(base.a * key.a * multiplier)`）");
    }

    // ============================================================ D · 回归：`grey = 1` 那一族不受影响
    static void D_GreyFamilyUnchanged(Transform parent)
    {
        ImageQuad face = null;
        var wb = MakeButton(parent, Green, out face);
        if (wb == null || face == null) { Check(false, "D 夹具没建出来"); return; }
        // 缺省 `ColorTintGreyOnDisable = true` ⇒ 原版把 `m_DisabledColor.a` 重写成 `GrayOnDisableAlpha`(1.0)
        wb.NormalKey = Green; wb.DisabledKey = DisabledKey; wb.ColorMultiplier = 1f;
        wb.ApplyTintInstant();
        wb.Interactable = false;
        Check(Mathf.Abs(face.Tint.a - 1f) < 1e-6f,
              "D1 `grey = 1` 那一族禁用档 α 仍是 **1.0**（原版重写 `m_DisabledColor.a`）—— 本笔不许把这一族画成半透明｜实得 "
              + face.Tint.a.ToString("F7"));
        wb.Interactable = true;
        UnityEngine.Object.DestroyImmediate(wb.gameObject);
    }

    // ============================================================ 夹具
    /// <summary>`WindowButton` 夹具：按钮节点 + 子树里一颗 `ImageQuad`（`Collect()` 收的就是它）。
    /// ⚠️ 底色必须在**第一次** `ApplyTint` 之前写好（`Collect()` 只收一次基准色）。</summary>
    static WindowButton MakeButton(Transform parent, Color baseColor, out ImageQuad face)
    {
        var go = new GameObject("W71_btn", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var wb = go.AddComponent<WindowButton>();
        face = ImageQuad.Create(go.transform, Texture2D.whiteTexture, Vector3.zero, 1f,
                                new Vector2(0.5f, 0.5f), "face");
        if (face != null) face.SetTint(baseColor);
        return wb;
    }

    /// <summary>`BattleButtonFx` 夹具：uGUI `Image` + `HudPiece`（= 战斗 HUD 的真实形态）。</summary>
    static HudPiece MakePiece(Transform parent, Color baseColor)
    {
        var go = new GameObject("W71_piece", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        var img = go.AddComponent<Image>();
        img.color = baseColor;
        img.raycastTarget = false;
        return HudPiece.Of(rt, img);
    }

    static string Src(string rel)
    {
        try { return File.ReadAllText(Application.dataPath + "/CardPresentation/" + rel); }
        catch (Exception e) { Debug.LogError(Tag + " 读不到 `" + rel + "`：" + e.Message); return ""; }
    }
}
