// B141_W57_Assert.cs — 执行写手 `B141-W57`（2026-11-04 · 第一百二十八会话）· 条目 `B141-MULT2-CLAMP`
//
// ============================ 它盯的是什么 ============================
//  `m_Colors.m_ColorMultiplier = 2`（原版 1276 颗里 **15 颗**）那几颗按钮上，两套按钮实现**夹在哪一步**
//  曾经不一致（`B141-W52` 报告 §六 ❓1 抓到的）：
//    · `Shell/WindowButton.ApplyTint`（W34）：**先** `Clamp01(色键 × 2)`，**再**乘底座 ⇒ (0.2718, 0.8941, 0.5874)
//    · `Battle/BattleButtonFx.Tint`  （W35）：只乘不夹、把 >1 留给存储层 ⇒ (0.2718, **1.0**, **0.6901**)
//  判据（本机现读，逐跳写在 `WindowButton.ComposeVertexColor` 的 doc 里）：
//    ① `Selectable.DoStateTransition` → `StartColorTween(tintColor * colorMultiplier)`（`Selectable.cs:702`，**不夹**）
//    ② `Graphic.CrossFadeColor` 原样交给 `canvasRenderer.SetColor`（`Graphic.cs:1009-1030`，**不夹**）
//    ③ `CanvasRenderer.SetColor` 是「Renderer **multiply** color」，乘在图形自己的颜色上（`UnityEngine.UIModule.xml:442`）
//    ④ 顶点色容器 = `UIVertex.color` = **`Color32`**（`VertexHelper.AddVert(…, Color32, …)`，`VertexHelper.cs:273`）
//       ⇒ **>1 的通道在这一次字节写入才饱和**
//  ⇒ 唯一算式 = `Clamp01(底座 × 色键 × colorMultiplier)`；两套按钮共用 `WindowButton.ComposeVertexColor`。
//
// ============================ 断言 ============================
//  A 组（纯算式，零前提 —— 期望值**自己算**，⛔ 不从被测实现读回来）：
//    A1  白 × 白 × 2 = 白（不变）
//    A2  绿族 Normal × Normal × 2 = (0.2717724, **1.0**, **0.69014**)（G 饱和）
//    A3  🧨 与**旧口径**（夹色键再乘底座）= (0.2718, 0.8941, 0.5874) **必须不同**（G 差 > 0.1）
//    A4  乘数 = 1 那一档 = 底座 × 色键（0.1359, 0.7994, 0.3451），且 ≠ 乘数 2 那一档
//    A5  α 也**乘完再夹**：0.5019608 × 2 = 1.0039216 ⇒ 1.0
//    A6  底座自身 >1 也不外泄（顶点色是字节）：(1.5, 0.5, 0.5) ⇒ (1.0, 0.5, 0.5)
//  B 组（**行为**：两套机制在同一个底座 + 同一套四态上算出同一组顶点色）：
//    B1  两侧 Normal 相同   B2  Highlighted 相同   B3  Pressed 相同
//    B4  WindowButton 的 Selected = 公式值（⚠️ `BattleButtonFx` 没有 Selected 态 —— 见它的常量 doc）
//    B5  默认灰键（0.9608 / 0.7843）+ 白底座 + 乘数 1：两侧相同且**不发生任何饱和**
//    B6  乘数 = 1 那一档两侧相同（证明统一的是**夹取次序**，不是把乘数吃掉）
//  C 组（源码守卫 —— 灭自证的另一半，⛔ 不许两边一起改回去）：
//    C1  `WindowButton.ApplyTint` 走 `ComposeVertexColor(b, _k, ColorMultiplier)`，且旧写法**不存在**
//    C2  `BattleButtonFx.Apply` 走 `WindowButton.ComposeVertexColor(_base, _key, 1f)`，且旧的裸乘**不存在**
//    C3  唯一实现只在 `WindowButton`（`Mathf.Clamp01(baseColor.r * key.r * multiplier)`）
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · `WindowButton` 改回「先 `Clamp01(色键 × m)` 再乘底座」⇒ C1 + B1/B2/B3 红（B 组拿公式算期望）。
//  · `BattleButtonFx` 改回「只乘不夹」⇒ C2 红；把它的 clamp 去掉但保留调用 ⇒ B1/B2/B3 红（它那侧会给出 >1）。
//  · 任一侧把乘数吃掉 ⇒ A4/B6 红。
//  · 把 `ComposeVertexColor` 的 `Clamp01` 删掉 ⇒ A2/A3/A5/A6 全红。
//  ⚠️ 单边改回旧口径时，**只有一侧**会变 ⇒ B 组的「两侧相等」必然红（这就是本条的结构位）。
//
// ============================ 怎么接（⚠️ 本件不碰宿主聚合）============================
//  `public static void Run();` —— **零前提**（自带临时根 + 收尾 `DestroyImmediate`；不碰场景 / 存档 / 真实 HUD）。
//  挂法（由调度台决定）：同一物理行追加
//      B141_W57_Assert.Run(); pass += B141_W57_Assert.Pass; fail += B141_W57_Assert.Fail;
//  判绿红：按**行首** `B141W57 ✓` / `B141W57 ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。

using System;
using System.IO;
using CardPresentation;
using UnityEngine;
using UnityEngine.UI;

/// <summary>执行写手 `B141_W57`：`m_ColorMultiplier = 2` 的夹取次序统一（两套按钮同一组顶点色）。</summary>
public static class B141_W57_Assert
{
    const string Tag = "B141W57";
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
    static readonly Color DisabledKey = new Color(0.7843137383f, 0.7843137383f, 0.7843137383f, 0.5019608140f);
    const float Mult2 = 2f;

    // 默认族（原版其余 ColorTint 钮）
    static readonly Color DefaultHL = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);
    static readonly Color DefaultPressed = new Color(0.7843137f, 0.7843137f, 0.7843137f, 1f);

    /// <summary>期望值**自己算**（⛔ 不读被测实现）：`Clamp01(底座 × 色键 × 乘数)`。</summary>
    static Color Want(Color baseColor, Color key, float m)
    {
        return new Color(Mathf.Clamp01(baseColor.r * key.r * m), Mathf.Clamp01(baseColor.g * key.g * m),
                         Mathf.Clamp01(baseColor.b * key.b * m), Mathf.Clamp01(baseColor.a * key.a * m));
    }

    /// <summary>**旧口径**（W34 改前）：先夹色键、再乘底座 —— 只在 A3 里当「不该是这个」的靶子。</summary>
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
        var root = new GameObject("B141_W57_Root");
        try { B_BothMechanismsSame(root.transform); }
        catch (Exception e)
        {
            Check(false, "B 组抛异常（后面的断言**没跑到**，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message
                       + "\n" + e.StackTrace);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        C_SourceGuards();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ============================================================ A · 纯算式
    static void A_Formula()
    {
        // A1
        var a1 = Want(Color.white, Color.white, 2f);
        Check(Near(a1, Color.white), "A1 白 × 白 × 2 = 白（乘法本身不变）｜实得 " + Show(a1));

        // A2 · 绿族 Normal 态（底座 = 色键 = 原版那两处绿）—— G 会饱和
        //   自算：r = 0.3686274886² × 2 = 0.27177245 · g = 0.8941176534² × 2 = 1.59889276 ⇒ **1.0**
        //        b = 0.5874372721² × 2 = 0.69016510
        var a2 = Want(Green, Green, Mult2);
        Check(Near(a2, new Color(0.27177245f, 1f, 0.69016510f, 1f), 1e-4f),
              "A2 绿 × 绿 × 2 = (0.27177245, **1.0**, **0.69016510**)（G 超 1 ⇒ 饱和）｜实得 " + Show(a2));

        // A3 · 🧨 与旧口径必须不同（旧口径把 G 停在 0.8941）
        var oldA3 = OldTintFirst(Green, Green, Mult2);
        Check(Mathf.Abs(a2.g - oldA3.g) > 0.1f && a2.g > 0.99f,
              "🧨 A3 新口径 G = " + a2.g.ToString("F4") + " vs 旧口径 G = " + oldA3.g.ToString("F4")
              + "（差 > 0.1；把夹取挪回色键上 ⇒ 本条红）");
        Check(Mathf.Abs(a2.r - oldA3.r) < 1e-5f,
              "A3b R 两种口径**相同**（0.2718）—— 差别只在会被饱和的通道（证明那是夹取次序、不是漏乘）");

        // A4 · 乘数 = 1
        //   自算：0.3686274886² = 0.13588623 · 0.8941176534² = 0.79944638 · 0.5874372721² = 0.34508255
        var a4 = Want(Green, Green, 1f);
        Check(Near(a4, new Color(0.13588623f, 0.79944638f, 0.34508255f, 1f), 1e-4f),
              "A4 乘数 = 1 ⇒ 底座 × 色键 = (0.13588623, 0.79944638, 0.34508255)（不饱和）｜实得 " + Show(a4));
        Check(Mathf.Abs(a4.r - a2.r) > 0.1f, "A4b 乘数 1 与乘数 2 的结果**不同**（乘数没有被吃掉）");

        // A5 · α 也乘完再夹
        var a5 = Want(Color.white, DisabledKey, Mult2);
        Check(Mathf.Abs(a5.a - 1f) < 1e-6f,
              "A5 α：0.5019608 × 2 = 1.0039216 ⇒ **夹到 1.0**｜实得 " + a5.a.ToString("F7"));

        // A6 · 底座自身 >1 也不外泄
        var a6 = Want(new Color(1.5f, 0.5f, 0.5f, 1f), Color.white, 1f);
        Check(Near(a6, new Color(1f, 0.5f, 0.5f, 1f)),
              "A6 底座 r = 1.5 ⇒ 顶点色落到 1.0（字节色不许把 >1 带出去）｜实得 " + Show(a6));
    }

    // ============================================================ B · 两套机制算出同一组顶点色
    static void B_BothMechanismsSame(Transform parent)
    {
        // ---- 夹具 ----
        ImageQuad face = null;
        var wb = MakeButton(parent, Green, out face);
        var piece = MakePiece(parent, Green);
        if (wb == null || face == null || piece == null)
        {
            Check(false, "B0 夹具没建出来（`ImageQuad.Create` / `HudPiece.Of` 返 null）");
            return;
        }
        // 两侧喂**同一套**四态 + 同一个乘数
        wb.NormalKey = Green; wb.HighlightKey = GreenHL; wb.PressedKey = GreenPressed; wb.SelectedKey = GreenSelected;
        wb.ColorMultiplier = Mult2;
        var fx = BattleButtonFx.Tint(piece, "W57 Continue", Green, GreenHL, GreenPressed, DisabledKey, Mult2);
        if (fx == null) { Check(false, "B0b `BattleButtonFx.Tint` 返 null"); return; }
        fx.ApplyInstant();

        // B1 · Normal
        wb.ApplyTintInstant();
        var wbN = face.Tint;
        Check(Near(wbN, Want(Green, Green, Mult2), 1e-5f),
              "B1🅐 `WindowButton` Normal = 公式值｜实得 " + Show(wbN));
        Check(Near(wbN, piece.Tint, 1e-5f),
              "B1🅑 **两套机制同一组顶点色**（WindowButton " + Show(wbN) + " vs BattleButtonFx " + Show(piece.Tint) + "）");

        // B2 · Highlighted
        wb.Enter();
        wb.ApplyTintInstant();
        var wbH = face.Tint;
        fx.SetHover(true);
        Check(Near(wbH, Want(Green, GreenHL, Mult2), 1e-5f), "B2🅐 `WindowButton` Highlighted = 公式值｜实得 " + Show(wbH));
        Check(Near(wbH, piece.Tint, 1e-5f), "B2🅑 悬停两侧相同（" + Show(wbH) + "）");

        // B3 · Pressed
        wb.Press();
        wb.ApplyTintInstant();
        var wbP = face.Tint;
        fx.SetPressed(true);
        Check(Near(wbP, Want(Green, GreenPressed, Mult2), 1e-5f), "B3🅐 `WindowButton` Pressed = 公式值｜实得 " + Show(wbP));
        Check(Near(wbP, piece.Tint, 1e-5f), "B3🅑 按下两侧相同（" + Show(wbP) + "）");

        // B4 · Selected（`BattleButtonFx` 没有 Selected 态 ⇒ 只对公式）
        wb.Release();
        wb.Press(); wb.Release();          // 清干净 Pressed
        wb.SetSelected(true);
        wb.ApplyTintInstant();
        var wbS = face.Tint;
        Check(Near(wbS, Want(Green, GreenSelected, Mult2), 1e-5f),
              "B4 `WindowButton` Selected = 公式值（战斗侧类没有 Selected 态，见 `ContinueSelected` 的 doc）｜实得 " + Show(wbS));
        wb.SetSelected(false);

        // B5 · 默认灰键 + 白底座 + 乘数 1：两侧相同、且**不饱和**（证明统一的是次序，不是把一切都夹平）
        ImageQuad face5 = null;
        var wb5 = MakeButton(parent, Color.white, out face5);
        var piece5 = MakePiece(parent, Color.white);
        if (wb5 == null || face5 == null || piece5 == null) { Check(false, "B5 夹具没建出来"); return; }
        wb5.HighlightKey = DefaultHL; wb5.PressedKey = DefaultPressed; wb5.ColorMultiplier = 1f;
        var fx5 = BattleButtonFx.Tint(piece5, "W57 default");
        if (fx5 == null) { Check(false, "B5b `BattleButtonFx.Tint` 返 null"); return; }
        fx5.ApplyInstant();
        wb5.ApplyTintInstant();
        Check(Near(face5.Tint, Color.white), "B5🅐 白底 Normal 仍是白｜实得 " + Show(face5.Tint));
        wb5.Enter(); wb5.ApplyTintInstant();
        fx5.SetHover(true);
        Check(Near(face5.Tint, DefaultHL, 1e-5f) && Near(face5.Tint, piece5.Tint, 1e-5f),
              "B5🅑 白底悬停 = 0.9607843，两侧相同｜WB " + Show(face5.Tint) + " · FX " + Show(piece5.Tint));

        // B6 · 乘数 = 1 那一档两侧相同（绿族、不饱和）
        ImageQuad face6 = null;
        var wb6 = MakeButton(parent, Green, out face6);
        var piece6 = MakePiece(parent, Green);
        if (wb6 == null || face6 == null || piece6 == null) { Check(false, "B6 夹具没建出来"); return; }
        wb6.NormalKey = Green; wb6.ColorMultiplier = 1f;
        var fx6 = BattleButtonFx.Tint(piece6, "W57 mult1", Green, GreenHL, GreenPressed, DisabledKey, 1f);
        if (fx6 == null) { Check(false, "B6b `BattleButtonFx.Tint` 返 null"); return; }
        fx6.ApplyInstant();
        wb6.ApplyTintInstant();
        Check(Near(face6.Tint, Want(Green, Green, 1f), 1e-5f), "B6🅐 乘数 1 = 底座 × 色键｜实得 " + Show(face6.Tint));
        Check(Near(face6.Tint, piece6.Tint, 1e-5f), "B6🅑 乘数 1 那一档两侧相同｜" + Show(face6.Tint));
        Check(Mathf.Abs(face6.Tint.g - face5.Tint.g) > 0.05f || face6.Tint.r < 0.5f,
              "B6🅒 绿族、乘数 1 ⇒ 明显不是白（B5/B6 两组夹具确实是不同输入）");
    }

    // ============================================================ C · 源码守卫（灭自证）
    static void C_SourceGuards()
    {
        var wbSrc = Src("Shell/WindowButton.cs");
        var fxSrc = Src("Battle/BattleButtonFx.cs");

        Check(wbSrc.Contains("ComposeVertexColor(b, _k, ColorMultiplier)"),
              "C1🅐 `WindowButton.ApplyTint` 走 `ComposeVertexColor(底座, 色键, 乘数)`");
        Check(!wbSrc.Contains("Mathf.Clamp01(k.r * ColorMultiplier)"),
              "🧨 C1🅑 旧口径「先 `Clamp01(色键 × m)` 再乘底座」**不存在**（改回去 ⇒ 红）");
        Check(wbSrc.Contains("Mathf.Clamp01(baseColor.r * key.r * multiplier)"),
              "C3🅐 唯一算式就在 `ComposeVertexColor` 里（`Clamp01(base × key × multiplier)`）");

        Check(fxSrc.Contains("WindowButton.ComposeVertexColor(_base, _key, 1f)"),
              "C2🅐 `BattleButtonFx.Apply` 走**同一个**静态口（⛔ 不再自己写一份乘法）");
        Check(!fxSrc.Contains("new Color(_base.r * _key.r"),
              "🧨 C2🅑 战斗侧旧的裸乘（不夹）**不存在**（改回去 ⇒ 红）");
        Check(!fxSrc.Contains("Mathf.Clamp01(_base.r * _key.r)"),
              "C2🅒 战斗侧**没有**第二份夹取实现（两份迟早不一致 —— CLAUDE.md §三）");
    }

    // ============================================================ 夹具
    /// <summary>`WindowButton` 夹具：按钮节点 + 子树里一颗 `ImageQuad`（`Collect()` 收的就是它）。
    /// ⚠️ 底色必须在**第一次** `ApplyTint` 之前写好（`Collect()` 只收一次基准色）。</summary>
    static WindowButton MakeButton(Transform parent, Color baseColor, out ImageQuad face)
    {
        var go = new GameObject("W57_btn", typeof(RectTransform));
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
        var go = new GameObject("W57_piece", typeof(RectTransform));
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
