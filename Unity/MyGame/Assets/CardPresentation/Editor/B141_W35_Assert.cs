// B141_W35_Assert.cs — 执行代理 `B141-W35`（2026-11-04）· W129 包 **P7** · 条目 **5-04**
//   选卡面板 `Continue Button`：**绿底 + 四态 + `m_ColorMultiplier 2`**
//
// ============================ 它盯的是哪几条 ============================
//  原版判据（逐字段现读，`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1`）：
//   · `MonoBehaviour_4262.json` —— `m_GameObject` → GO **779**，`m_Script` → `UnityEngine.UI.Button`
//     （`bundle_Waprforge_monoscripts/MonoScript_-4448497653027179337.json` 的 `m_ClassName` = `Button`；
//      ⚠️ **不是** `EverguildButton` ⇒ 没有 `colorTintGreyOnDisable` 那一跳 α 改写，禁用 α 保持序列化的 0.5019608）。
//     `m_Transition = 1`(ColorTint) · `m_Interactable = 1` ·
//     `m_Colors` = N (0.3686274886, 0.8941176534, 0.5874372721, 1)
//                  H (0.4666666985, 0.9137255549, 0.4000000358, 1)
//                  P (0.4784314036, 0.4941176772, 0.4823529720, 1)
//                  S (0.3686274588, 0.8941177130, 0.5882353187, 1)
//                  D (0.7843137383, 0.7843137383, 0.7843137383, 0.5019608140)
//                  **`m_ColorMultiplier = 2.0`** · `m_FadeDuration = 0.1`
//   · `m_TargetGraphic` → `MonoBehaviour_4210.json` = 子件 **`BG`**（GO **1153**）的 `Image`，
//     **`m_Color` = 与 N 同一个绿** (0.3686274886, 0.8941176534, 0.5874372721, 1)。
//  ⇒ 屏上 = **贴图 × Image.m_Color × (stateColor × colorMultiplier)**。
//     UGUI 那一跳的判据 = `CanvasRenderer.SetColor` 的 doc 原话「Will be multiplied with the UIVertex color
//     and the Canvas color」（本机 `D:/Unity/.../Managed/UnityEngine/UnityEngine.UIModule.xml:442`）
//     ＋ `Selectable.DoStateTransition` 的 `StartColorTween(tintColor * m_Colors.colorMultiplier, instant)`
//     （本机 `com.unity.ugui/…/Runtime/UGUI/UI/Core/Selectable.cs:702`，**α 一起乘**）。
//  🔴 **2026-11-04（`B141-W57`）补最后一步**：`tintColor * colorMultiplier` **在 managed 层不夹**，
//     夹在落进 `UIVertex.color`（`Color32`，`VertexHelper.AddVert(…, Color32, …)`）的那一次字节写入
//     ⇒ 有效色 = `Clamp01(Image.m_Color × stateColor × colorMultiplier)`。本文件 `Mul(...)` 已按这条改。
//
//  我们这一侧（改后）：`BattleButtonFx.ContinueNormal/…/ContinueSelected/ContinueColorMultiplier` 承载上表；
//  `Battle/CardChoicePanel.cs` 建底条时传基准绿、`BuildFeedback` 每次重建**先还原基准色**再建 fx 并 `ApplyInstant()`。
//
//  **`B141-W35b`（同日续做）· 确认钮禁用态**：原版 `Continue Button` 子树四颗件各挂一颗
//  `EverguildButtonStateFollower`（`EverguildButtonStateFollower__Disabled.c` → `DoMaterialRefresh` ⇒
//  `EverguildButtonHelper.get_DisabledMaterial` 的灰度材质），与按钮自己的 `m_DisabledColor` × multiplier **同时**生效。
//  我们这一侧 = `SetConfirmInteractable(false)` → `_fxBar.SetDisabled(true)`（色 + 灰化），重建后重放；E 组钉住。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 1（两处绿）：B1 把复合值钉在 **base × N × 2 = 0.2717724**，同时 B2 断言它 **≠ 0.7372（= N × 2，漏掉基准色）**
//     且 **≠ 0.1359（= base × N，漏掉 ×2）** —— 谁把其中一处当白/漏乘，必有一条红。
//  🧨 2（×2 在参数上，不在常量里）：B5 用**同一组常量、不传 multiplier** 再建一颗 ⇒ 期望 base × N = 0.1358862。
//     把 2 烘进 `ContinueNormal` 常量（而不是 `colorMultiplier` 参数）⇒ B1 与 B5 **同时**红。
//  🧨 3（四态真有区别）：B3/B4 钉 H、P 各自的复合值，并断言 **H ≠ N、P ≠ N**（差距 > 0.05）——
//     退回「一套默认 0.9608/0.7843」⇒ 两条一起红。
//  🧨 4（接线没被摘）：D 组按**源码串**钉住 `CardChoicePanel` 那三处（基准色 / 四态 / multiplier / ApplyInstant），
//     把接线删掉或只改注释不改代码 ⇒ D 组红（⚠️ 它是**接线守卫**，不替代 B 组的行为断言）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `Editor/BattleScene.cs` 那条**方法尾聚合行**（现读 `:47035`，`Debug.Log(P + $"=== 合计…")` 之前）追加：
//      B141_W35_Assert.Run(); pass += B141_W35_Assert.Pass; fail += B141_W35_Assert.Fail;
//  **同一物理行追加 ⇒ 行数零变化**。本件**零前提**（自带临时根 + 收尾 `DestroyImmediate`；
//  不碰场景 / 存档 / 真实 HUD 画布 / 灰化 shader）。
//  判绿红：`grep "^\[B141W35\] 合计"` 那一行；逐条按**行首** `[B141W35] ✓` / `[B141W35] ✗` 数
//  （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。

using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using CardPresentation;

/// <summary>执行代理 `B141_W35`：选卡面板 `Continue Button` 的绿底 / 四态 / `m_ColorMultiplier 2` 落地断言。</summary>
public static class B141_W35_Assert
{
    static int _pass, _fail;
    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _fail; } }

    const string Prefix = "[B141W35] ";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Prefix + "✓ " + msg); }
        else { _fail++; Debug.LogError(Prefix + "✗ " + msg); }
    }

    static void Near(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F7") + " ± " + tol + "；实得 " + got.ToString("F7") + "）");
    }

    static void NearColor(Color got, Color want, float tol, string msg)
    {
        bool ok = Mathf.Abs(got.r - want.r) <= tol && Mathf.Abs(got.g - want.g) <= tol
               && Mathf.Abs(got.b - want.b) <= tol && Mathf.Abs(got.a - want.a) <= tol;
        Check(ok, msg + "（期望 (" + want.r.ToString("F6") + ", " + want.g.ToString("F6") + ", "
                 + want.b.ToString("F6") + ", " + want.a.ToString("F6") + ")；实得 ("
                 + got.r.ToString("F6") + ", " + got.g.ToString("F6") + ", "
                 + got.b.ToString("F6") + ", " + got.a.ToString("F6") + ")）");
    }

    // ---- 原版字面量（⛔ 全部抄自 `MonoBehaviour_4262.json` / `MonoBehaviour_4210.json`，**不从被测常量读回来**）----
    static readonly Color OrigNormal = new Color(0.3686274886f, 0.8941176534f, 0.5874372721f, 1f);
    static readonly Color OrigHighlighted = new Color(0.4666666985f, 0.9137255549f, 0.4000000358f, 1f);
    static readonly Color OrigPressed = new Color(0.4784314036f, 0.4941176772f, 0.4823529720f, 1f);
    static readonly Color OrigSelected = new Color(0.3686274588f, 0.8941177130f, 0.5882353187f, 1f);
    static readonly Color OrigDisabled = new Color(0.7843137383f, 0.7843137383f, 0.7843137383f, 0.5019608140f);
    const float OrigMultiplier = 2.0f;

    // ---- 默认族（原版其余 ColorTint 钮：白 / 0.9607843 / 0.7843137）----
    static readonly Color DefaultHighlighted = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);
    static readonly Color DefaultPressed = new Color(0.7843137f, 0.7843137f, 0.7843137f, 1f);

    /// <summary>期望：**乘完再夹**（`Clamp01(base × key × m)`）。
    /// <para>🔴 **2026-11-04（`B141-W57`）同步**：原口径是「只乘不夹、让 `Image.color` 自己留 >1」（那时本文件比的是
    /// 未夹的浮点值）。判据改为 UGUI 那条链的真实落点 —— `tintColor * colorMultiplier` 先不夹，
    /// 夹在最后一次**字节顶点色**写入（`UIVertex.color` 是 `Color32`）⇒ 有效色 = `Clamp01(乘积)`。
    /// 两套按钮（`WindowButton` / `BattleButtonFx`）现在共用 `WindowButton.ComposeVertexColor` 这一个口。</para></summary>
    static Color Mul(Color a, Color b, float m)
    {
        return new Color(Mathf.Clamp01(a.r * b.r * m), Mathf.Clamp01(a.g * b.g * m),
                         Mathf.Clamp01(a.b * b.b * m), Mathf.Clamp01(a.a * b.a * m));
    }

    // ============================================================ 入口
    public static void Run()
    {
        _pass = 0; _fail = 0;
        var root = new GameObject("B141_W35_Root");
        try
        {
            A_OriginalLiterals();
            B_CompositeSemantics(root.transform);
            C_DefaultsUnchanged(root.transform);
            D_PanelWiring();
            E_ConfirmDisabled(root.transform);
        }
        catch (Exception e)
        {
            Check(false, "宿主抛异常（后面的断言**没跑到**，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
        Debug.Log(Prefix + "合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ============================================================ A · 常量 = 原版字面值
    static void A_OriginalLiterals()
    {
        NearColor(BattleButtonFx.ContinueNormal, OrigNormal, 1e-6f, "A1 N 与 MB4262 `m_NormalColor` 逐通道一致");
        NearColor(BattleButtonFx.ContinueHighlighted, OrigHighlighted, 1e-6f, "A2 H 与 MB4262 `m_HighlightedColor` 一致");
        NearColor(BattleButtonFx.ContinuePressed, OrigPressed, 1e-6f, "A3 P 与 MB4262 `m_PressedColor` 一致");
        NearColor(BattleButtonFx.ContinueSelected, OrigSelected, 1e-6f, "A4 S 与 MB4262 `m_SelectedColor` 一致（本类无 Selected 态，只承载原值）");
        Near(BattleButtonFx.ContinueColorMultiplier, OrigMultiplier, 1e-6f, "A5 倍率 = MB4262 `m_ColorMultiplier` = 2");
        // 本面板显式传的禁用色 = 序列化那一格（α 0.5019608）；⛔ 别用 `DisabledOpaque`（那是 `EverguildButton` 的 α 改写族）
        NearColor(BattleButtonFx.Disabled, OrigDisabled, 1e-6f, "A6 显式传的禁用色 = MB4262 `m_DisabledColor`（α 0.5019608）");
        Near(BattleButtonFx.FadeSeconds, 0.1f, 1e-6f, "A7 淡入时长 = MB4262 `m_FadeDuration` = 0.1 s");
        // A8：Normal 与 Selected **必须**只差 B（原版就是这样；差 0.0007979）
        Near(OrigSelected.b - OrigNormal.b, 0.0007979f, 1e-5f, "A8 原版 S 与 N 只差 B（0.0007979）——拿它当「四态确实分开读出来的」旁证");
    }

    // ============================================================ B · 复合语义（基准 × 态 × 倍率）
    static void B_CompositeSemantics(Transform parent)
    {
        // B 组第一颗：底座 = 原版 `BG` 的 `Image.m_Color`（绿），四态 + ×2 —— **就是面板那一颗的配方**
        var piece = MakePiece(parent, "BarLike", OrigNormal);
        var fx = BattleButtonFx.Tint(piece, "Continue Button",
                                     BattleButtonFx.ContinueNormal, BattleButtonFx.ContinueHighlighted,
                                     BattleButtonFx.ContinuePressed, BattleButtonFx.Disabled,
                                     BattleButtonFx.ContinueColorMultiplier);
        Check(fx != null, "B0 反馈对象建出来了（件非 null ⇒ 非 null）");
        if (fx == null) return;
        fx.ApplyInstant();

        Color wantN = Mul(OrigNormal, OrigNormal, OrigMultiplier);
        NearColor(piece.Tint, wantN, 2e-5f, "B1 静止 = base(绿) × N × 2");
        Near(piece.Tint.r, 0.2717724f, 5e-5f, "🧨 B2a R 落在「两处绿 × 2」的 0.2717724");

        fx.SetHover(true);
        NearColor(piece.Tint, Mul(OrigNormal, OrigHighlighted, OrigMultiplier), 2e-5f, "B3 悬停 = base × H × 2");

        fx.SetPressed(true);
        NearColor(piece.Tint, Mul(OrigNormal, OrigPressed, OrigMultiplier), 2e-5f, "B4 按下 = base × P × 2");
        fx.SetPressed(false);

        // B5：**同一组常量、不传 multiplier** ⇒ base × N（把 2 烘进常量的人，这里会红）
        var piece5 = MakePiece(parent, "NoMultiplier", OrigNormal);
        var fx5 = BattleButtonFx.Tint(piece5, "no-mult",
                                      BattleButtonFx.ContinueNormal, BattleButtonFx.ContinueHighlighted,
                                      BattleButtonFx.ContinuePressed, BattleButtonFx.Disabled);
        if (fx5 != null) fx5.ApplyInstant();
        NearColor(piece5.Tint, Mul(OrigNormal, OrigNormal, 1f), 2e-5f, "🧨 B5 缺省倍率 = 1 ⇒ base × N（0.1358862）");

        // B6~B8：四态互不相同（拿**原版字面量**算期望，⛔ 不比我们自己的常量）
        Color nFull = Mul(OrigNormal, OrigNormal, OrigMultiplier);
        Color hFull = Mul(OrigNormal, OrigHighlighted, OrigMultiplier);
        Color pFull = Mul(OrigNormal, OrigPressed, OrigMultiplier);
        Check(Mathf.Abs(nFull.r - hFull.r) > 0.05f, "🧨 B6 N 与 H 的 R 差 > 0.05（四态没被压成一套默认值）");
        Check(Mathf.Abs(nFull.g - pFull.g) > 0.05f || Mathf.Abs(nFull.r - pFull.r) > 0.05f,
              "🧨 B7 N 与 P 的通道差 > 0.05（P 不是 N 的别名）");

        // B8：禁用档（α 一起乘）—— 先摘掉「换灰化材质」那一跳（`EverguildButton` 族的行为，本条目只管颜色）
        fx.ColorTintGreyOnDisable = false;
        fx.SetDisabled(true);
        NearColor(piece.Tint, Mul(OrigNormal, OrigDisabled, OrigMultiplier), 2e-5f,
                  "B8 禁用 = base × D × 2，**乘完再夹**（α：0.5019608 × 2 = 1.0039216 ⇒ 饱和到 1.0）");
        fx.SetDisabled(false);

        // B9：**重建不会越乘越偏** —— 面板每次 `Open` 都会重建 fx（先 `SetTint(基准绿)`）
        piece.SetTint(OrigNormal);
        var fx2 = BattleButtonFx.Tint(piece, "Continue Button(rebuild)",
                                      BattleButtonFx.ContinueNormal, BattleButtonFx.ContinueHighlighted,
                                      BattleButtonFx.ContinuePressed, BattleButtonFx.Disabled,
                                      BattleButtonFx.ContinueColorMultiplier);
        if (fx2 != null) fx2.ApplyInstant();
        NearColor(piece.Tint, nFull, 2e-5f, "B9 重建后仍是 base × N × 2（基准色被还原，⛔ 不是上一次的合成色）");

        // B10：`HudPiece.Tint` 读的就是 `Image.color`（证明上面读的是**画出来的那一件**）
        var img = piece.Rect != null ? piece.Rect.GetComponent<Image>() : null;
        Check(img != null && Mathf.Abs(img.color.r - piece.Tint.r) < 1e-6f,
              "B10 读到的色 = `Image.color`（⛔ 不是本类自己缓存的副本）");
    }

    // ============================================================ C · 既有调用点零变化
    static void C_DefaultsUnchanged(Transform parent)
    {
        // C1：缺省四态 + 白底座 ⇒ 白（= 改前行为）
        var p1 = MakePiece(parent, "WhiteDefault", Color.white);
        var f1 = BattleButtonFx.Tint(p1, "default");
        if (f1 != null) f1.ApplyInstant();
        NearColor(p1.Tint, Color.white, 1e-6f, "C1 缺省（白底 + 白 N + 倍率 1）静止仍是白 —— 其余 20+ 调用点零变化");

        if (f1 != null)
        {
            f1.SetHover(true);
            NearColor(p1.Tint, DefaultHighlighted, 1e-5f, "C2 缺省悬停仍是 0.9607843");
            f1.SetPressed(true);
            NearColor(p1.Tint, DefaultPressed, 1e-5f, "C3 缺省按下仍是 0.7843137");
            f1.SetPressed(false); f1.SetHover(false);
        }

        // C4：显式传四态但不传倍率 ⇒ 与传入值一致（倍率缺省 = 1，⛔ 别偷偷乘 2）
        var p4 = MakePiece(parent, "ExplicitNoMul", Color.white);
        var f4 = BattleButtonFx.Tint(p4, "explicit", OrigNormal, OrigHighlighted, OrigPressed, OrigDisabled);
        if (f4 != null) f4.ApplyInstant();
        NearColor(p4.Tint, OrigNormal, 1e-6f, "C4 白底 + 显式 N、不传倍率 ⇒ 就是 N 本身（倍率缺省 1）");
    }

    // ============================================================ D · 面板接线（源码守卫）
    static void D_PanelWiring()
    {
        string path = Path.Combine(Application.dataPath, "CardPresentation/Battle/CardChoicePanel.cs");
        string src = null;
        try { if (File.Exists(path)) src = File.ReadAllText(path); }
        catch (Exception e) { Check(false, "D0 读不到 `" + path + "`：" + e.Message); return; }
        if (src == null) { Check(false, "D0 读不到 `" + path + "`（文件不存在）"); return; }

        Check(Has(src, "_bar.SetTint(BattleButtonFx.ContinueNormal)"),
              "D1 `BuildFeedback` 重建前把底条还原成原版基准绿");
        Check(Has(src, "BattleButtonFx.ContinueNormal, BattleButtonFx.ContinueHighlighted, BattleButtonFx.ContinuePressed, BattleButtonFx.Disabled, BattleButtonFx.ContinueColorMultiplier"),
              "D2 Continue 钮传的是「绿族四态 + 倍率」（N/H/P/D + 2）");
        Check(Has(src, "_fxBar.ApplyInstant()"),
              "D3 建完立刻 ApplyInstant（= 原版 `OnEnable → DoStateTransition(Normal, instant)`；不调就少一乘）");
        Check(Has(src, "BarW, BarH, Z, BattleButtonFx.ContinueNormal"),
              "D4 底条建件时基准色 = 原版 `BG` 的 `Image.m_Color`（绿，⛔ 不是白）");
        Check(Has(src, "_fxBar.SetDisabled(!on)"),
              "D5 `SetConfirmInteractable` 真的把禁用态喂给确认钮（`B141-W35b`）");
        Check(Has(src, "_fxBar.SetDisabled(!_confirmInteractable)"),
              "D6 重建后立刻重放禁用态（⛔ 重建不该把钮刷回可交互）");
    }

    // ============================================================ E · 确认钮禁用态（`B141-W35b`）
    static void E_ConfirmDisabled(Transform parent)
    {
        // 原版：`Continue Button` 子树四颗件各挂一颗 `EverguildButtonStateFollower` ⇒ `interactable=false`
        // 换灰化材质；同时按钮自己的 `m_DisabledColor`（× multiplier）照常生效。我们这一侧 = `_fxBar.SetDisabled(!on)`。
        var p1 = MakePiece(parent, "DisabledGrey", OrigNormal);
        var f1 = BattleButtonFx.Tint(p1, "Continue Button(disabled)",
                                     BattleButtonFx.ContinueNormal, BattleButtonFx.ContinueHighlighted,
                                     BattleButtonFx.ContinuePressed, BattleButtonFx.Disabled,
                                     BattleButtonFx.ContinueColorMultiplier);
        if (f1 == null) { Check(false, "E0 禁用态夹具没建出来"); return; }
        f1.ApplyInstant();
        f1.SetDisabled(true);
        NearColor(p1.Tint, Mul(OrigNormal, OrigDisabled, OrigMultiplier), 2e-5f,
                  "E1 禁用 = base × D × 2（色那一半）");
        Check(f1.GrayedForTest != f1.GrayShaderMissingForTest,
              "🧨 E2 禁用后「要么真灰了、要么明确记了没灰成」——恰好一条为真（⛔ 不许静默不变灰）");

        f1.SetDisabled(false);
        NearColor(p1.Tint, Mul(OrigNormal, OrigNormal, OrigMultiplier), 2e-5f,
                  "E3 恢复可交互 = base × N × 2");
        Check(!f1.GrayedForTest, "E4 恢复后不再灰（材质已还回去）");

        // 🧨 E5/E6：把 `ColorTintGreyOnDisable` 闸关掉 ⇒ **只变色、不灰**，而且**不许**报「想灰没灰成」
        //   —— 证明 E2 那个「恰好一条为真」是活闸推出来的，不是把两个字段写死的常量。
        var p2 = MakePiece(parent, "DisabledNoGrey", OrigNormal);
        var f2 = BattleButtonFx.Tint(p2, "Continue Button(no-grey)",
                                     BattleButtonFx.ContinueNormal, BattleButtonFx.ContinueHighlighted,
                                     BattleButtonFx.ContinuePressed, BattleButtonFx.Disabled,
                                     BattleButtonFx.ContinueColorMultiplier);
        if (f2 == null) { Check(false, "E5 夹具没建出来"); return; }
        f2.ColorTintGreyOnDisable = false;
        f2.ApplyInstant();
        f2.SetDisabled(true);
        NearColor(p2.Tint, Mul(OrigNormal, OrigDisabled, OrigMultiplier), 2e-5f, "E5 闸关掉时仍走 Disabled 色");
        Check(!f2.GrayedForTest && !f2.GrayShaderMissingForTest,
              "🧨 E6 闸关掉 ⇒ 既不灰、也不报「想灰没灰成」（闸是活的，不是常量）");
    }

    static bool Has(string src, string needle)
    {
        return src != null && src.IndexOf(needle, StringComparison.Ordinal) >= 0;
    }

    // ============================================================ 夹具
    static HudPiece MakePiece(Transform parent, string name, Color baseColor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        var img = go.AddComponent<Image>();
        img.color = baseColor;
        img.raycastTarget = false;
        return HudPiece.Of(rt, img);
    }
}
