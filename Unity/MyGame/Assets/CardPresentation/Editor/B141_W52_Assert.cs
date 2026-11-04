// B141_W52_Assert.cs — 执行写手 `B141-W52`（第一百二十八会话 · 2026-11-04）· W129 **5-06 剩余两颗**
//
// ============================ 它盯的是什么 ============================
//  `报告/B141_W129_ButtonFx.md` §5-06 的❓尾巴：15 颗 `m_Colors.m_ColorMultiplier = 2` 里，
//  **落点在我们树里、且本波能做的 2 颗**：
//    ① `PracticeModePopup` 的 `BattleHit`（`Shell/PracticeModePopup.cs`，= 原版 `Practice Mode Menu ▸ Continue Button`）
//    ② `MultiCardDisplay` 的 `Button`（`Battle/MultiCardDisplay.cs`，= 原版 `Generic Multi Card Display Combat ▸ Button`）
//  按 `B131` 色彩 A15/A16 把**四态底座色**与**乘数**一起落（⚠️ 单加乘数没视觉 —— W34 的提示）。
//
//  | 项 | 原版值（三份 prefab 的 `m_Colors` **逐位相同**） |
//  |---|---|
//  | Image `m_Color`（底座绿） | (0.3686275, 0.89411765, 0.5874373, 1) |
//  | `m_NormalColor` | (0.3686275, 0.89411765, 0.5874373, 1) —— **与底座同值** |
//  | `m_HighlightedColor` | (0.4666667, 0.91372555, 0.40000004, 1) |
//  | `m_PressedColor` | (0.4784314, 0.49411768, 0.48235297, 1) |
//  | `m_SelectedColor` | (0.36862746, 0.8941177, 0.5882353, 1) |
//  | `m_DisabledColor` | (0.78431374 ×3, a **0.5019608**) |
//  | `m_ColorMultiplier` | **2** |
//
// ============================ 断言 ============================
//  A 组（纯值，零前提 —— 只读公共常量 + 在本进程里复算 `ApplyTint` 的算式）：
//    A1  底座绿 / Normal **逐位**等于 prefab（且两者**同值** —— 原版就是这样）
//    A2  悬停 / 按下 / 选中三格逐位等于 prefab
//    A3  禁用色逐位，且 **α = 0.5019608**（纯 `UnityEngine.UI.Button` ⇒ 无 `greyOnDisable` α 改写）
//    A4  乘数 = **2**
//    A5  `MultiCardDisplay.BarColor`（战斗侧底座）= 同一个绿
//    A6  `BattleButtonFx.Continue*`（= 5-04 那颗的常量）与本次这两颗**逐位相同**（一份判据两处用）
//    A7  复算顶点色：**乘完再夹** `Clamp01(base × Normal × 2)` = **(0.2718, 1.0, 0.6901)**
//        ✅ 已复刻（2026-11-04 · B141 · W57 统一口径）：唯一实现 = `WindowButton.ComposeVertexColor`（两侧共用）。
//        —— 且 R **明显小于**「只留一层绿」那一档（0.3686）⇒ 这条钉的正是「两处都是绿」
//    A7c G **必须饱和到 1.0** / B ≈ 0.6901（⛔ 旧口径「先 `Clamp01(键 × m)` 再乘底座」会给 0.8941 / 0.5874）
//    A8  同一算式下「乘数 = 1」那一档 = (0.1359, 0.7994, 0.3451) ≠ A7 ⇒ 乘数**真的**改变了结果
//
//  B 组（源码结构，零前提 —— 只读 4 个 `.cs` 文本；灭自证的另一半）：
//    B1  `PracticeModePopup` 真的调了 `WireContinueButton(battleHit, contQuad)`（接线口存在）
//    B2  那颗可见图用的是 `ContinueFaceColor`，且**旧的圆整值 `0.369/0.894/0.587` 已不存在**
//    B3  `WireContinueButton` 把四态 + 禁用色 + 乘数 + `ColorTintGreyOnDisable=false` 全都赋了
//    B4  `WireContinueButton` 真的 `TintOn(face)` 且 `ApplyTintInstant()`（否则色偏看不见 / 停在旧色）
//    B5  `MultiCardDisplay` 的底座绿**无条件**写（旧写法 `barTex == null` 那道闸已删）
//    B6  `MultiCardDisplay` 的 `BattleButtonFx.Tint` 传了四态 + `ContinueColorMultiplier`
//    B7  `MultiCardDisplay` 设了 `ColorTintGreyOnDisable = false` 且 `ApplyInstant()`
//    B8  `MultiCardDisplay` **没有**自己再抄一份四态字面量（`0.4666667` 不得出现 —— 一份判据只留一处）
//    B9  ✅ 已复刻（2026-11-04 · B141 · W57 统一口径）：**两侧都走** `WindowButton.ComposeVertexColor`
//        （外壳 `ApplyTint` 传 `ColorMultiplier`；战斗侧乘数已在 ctor 乘进四态 ⇒ 传 1），且算式就在那一个口里
//    B10 灭自证：两侧的**旧口径都不得回来**（`Clamp01(k.r * ColorMultiplier)` · `_base.r * _key.r`）
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · 把 `ContinueColorMultiplier` 改回 1 ⇒ A4 + A8 红。
//  · 把 `ContinueNormal` / `ContinueFaceColor` 改成白 ⇒ A1 + A7 红（乘积 R 会从 0.2718 跳回 1.0）。
//  · 把可见图的 tint 改回旧的圆整值 ⇒ B2 红。
//  · 删 `WireContinueButton(battleHit, contQuad);` ⇒ B1 红（A 组管不到接线，这条是唯一证据）。
//  · 删 `wb.TintOn(face);` ⇒ B4 红（四态只打在透明命中区上，画面上不动）。
//  · 删 `wb.ApplyTintInstant();` ⇒ B4 红（建窗那一刻停在旧的「单层绿」，与原版 `OnEnable` 不符）。
//  · 把 `_bar.SetTint(BarColor)` 重新加上 `barTex == null` 那道闸 ⇒ B5 红。
//  · 把 `BattleButtonFx.Tint(...)` 的参数改回缺省（不传四态/乘数）⇒ B6 红。
//  · 在 `MultiCardDisplay` 里另抄一份四态色 ⇒ B8 红。
//  · 任一侧改回**旧口径**（`Clamp01(k.r * ColorMultiplier)` 或 `_base.r * _key.r`）⇒ B9/B10 红；
//    把 `ComposeVertexColor` 改回「先夹后乘」⇒ A7/A7c 红（G 停在 0.8941、B 停在 0.5874）。
//
// ============================ 怎么接（本文件不碰宿主聚合）============================
//  `public static void Run();` —— **零前提**（只读公共常量 + 4 份源码文本；⛔ 不建窗口、不写盘）。
//  挂法（由调度台决定）：同一物理行追加
//      B141_W52_Assert.Run(); pass += B141_W52_Assert.Pass; fail += B141_W52_Assert.Fail;
//  判绿红：按行首 `B141W52 ✓` / `B141W52 ✗` 数（⛔ 别 `grep -c ✗`）。

using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B141_W52`：W129 5-06 剩余两颗（Practice Continue / MultiCard Continue）的回归断言。</summary>
public static class B141_W52_Assert
{
    const string Tag = "B141W52";
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
    //  A · 纯值（判据 = 原版 prefab 逐字段现读）
    // ==================================================================
    static void GroupA_Values()
    {
        var face = new Color(0.3686275f, 0.89411765f, 0.5874373f, 1f);
        var hl = new Color(0.4666667f, 0.91372555f, 0.40000004f, 1f);
        var pr = new Color(0.4784314f, 0.49411768f, 0.48235297f, 1f);
        var sel = new Color(0.36862746f, 0.8941177f, 0.5882353f, 1f);
        var dis = new Color(0.78431374f, 0.78431374f, 0.78431374f, 0.5019608f);

        // ---- A1 · 底座绿 / Normal（两处都是绿，且与原版逐位相同）----
        Check(Near(PracticeModePopup.ContinueFaceColor, face),
              "A1 5-06/A16 底座绿（Image `m_Color`）逐位 = prefab｜实得 " + PracticeModePopup.ContinueFaceColor);
        Check(Near(PracticeModePopup.ContinueNormal, face),
              "A1b 5-06 `m_NormalColor` 与底座**同值**（原版就是这样 —— ⛔ 别把其中一处当白）｜实得 " + PracticeModePopup.ContinueNormal);
        Check(Near(PracticeModePopup.ContinueNormal, PracticeModePopup.ContinueFaceColor),
              "A1c 两处常量恒等（改一处不改另一处 ⇒ 红）");

        // ---- A2 · 悬停 / 按下 / 选中 ----
        Check(Near(PracticeModePopup.ContinueHighlighted, hl),
              "A2 5-06 `m_HighlightedColor` 逐位 = prefab｜实得 " + PracticeModePopup.ContinueHighlighted);
        Check(Near(PracticeModePopup.ContinuePressed, pr),
              "A2b 5-06 `m_PressedColor` 逐位 = prefab｜实得 " + PracticeModePopup.ContinuePressed);
        Check(Near(PracticeModePopup.ContinueSelected, sel),
              "A2c 5-06 `m_SelectedColor` 逐位 = prefab（与 Normal 只差 B）｜实得 " + PracticeModePopup.ContinueSelected);
        Check(Mathf.Abs(PracticeModePopup.ContinueSelected.b - PracticeModePopup.ContinueNormal.b) > 1e-6f,
              "A2d 选中与常态的 B **必须不等**（原版 0.5882353 vs 0.5874373；抄成同一个数 ⇒ 红）");

        // ---- A3 · 禁用色 + α（纯 UnityEngine.UI.Button，无 α 改写）----
        Check(Near(PracticeModePopup.ContinueDisabled, dis),
              "A3 5-06 `m_DisabledColor` 逐位 = prefab｜实得 " + PracticeModePopup.ContinueDisabled);
        Check(Mathf.Abs(PracticeModePopup.ContinueDisabled.a - 0.5019608f) < 1e-6f,
              "A3b 禁用 α = **0.5019608**（不是 1.0 —— 纯 `UnityEngine.UI.Button` 没有 `greyOnDisable` 那一跳）｜实得 "
              + PracticeModePopup.ContinueDisabled.a);

        // ---- A4 · 乘数 ----
        Check(Mathf.Abs(PracticeModePopup.ContinueColorMultiplier - 2f) < 1e-6f,
              "A4 5-06 `m_ColorMultiplier` = **2**（原版 15 颗之一；改回 1 ⇒ 红）｜实得 " + PracticeModePopup.ContinueColorMultiplier);

        // ---- A5 · 战斗侧底座 = 同一个绿 ----
        Check(Near(MultiCardDisplay.BarColor, face),
              "A5 A16 `MultiCardDisplay.BarColor`（战斗侧底座）逐位 = prefab 的 Image `m_Color`｜实得 " + MultiCardDisplay.BarColor);

        // ---- A6 · 5-04 那套常量与本次逐位相同（一份判据两处用）----
        Check(Near(BattleButtonFx.ContinueNormal, face)
              && Near(BattleButtonFx.ContinueHighlighted, hl)
              && Near(BattleButtonFx.ContinuePressed, pr)
              && Near(BattleButtonFx.ContinueSelected, sel)
              && Mathf.Abs(BattleButtonFx.ContinueColorMultiplier - 2f) < 1e-6f,
              "A6 5-04 的 `BattleButtonFx.Continue*` 与本次两颗**逐位相同**（同族 Continue 钮，一份常量）");

        // ---- A7/A8 · 复算顶点色那一跳 ----
        //   ✅ 已复刻（2026-11-04 · B141 · W57 统一口径）：唯一实现 = `WindowButton.ComposeVertexColor`
        //   ⇒ 屏上 = `Clamp01(base × key × multiplier)`（**四通道，α 一起**；夹在最后一次字节顶点色写入）。
        //   ⛔ 旧口径「先 `Clamp01(key × m)` 再乘 base」已作废 —— 它在乘数 = 2 时把 G/B 停在 0.8941 / 0.5874。
        Color prod2 = WindowButton.ComposeVertexColor(face, face, 2f);
        var want2 = new Color(0.27177f, 1f, 0.69017f, 1f);
        Check(Mathf.Abs(prod2.r - want2.r) < 2e-4f && Mathf.Abs(prod2.g - want2.g) < 2e-4f
              && Mathf.Abs(prod2.b - want2.b) < 2e-4f && Mathf.Abs(prod2.a - want2.a) < 1e-6f,
              "A7 5-06 复算（**乘完再夹**）：`Clamp01(base × Normal × 2)` = (0.2718, 1.0, 0.6901)｜实得 " + prod2);
        Check(prod2.r < 0.30f && face.r > 0.35f,
              "A7b 5-06 乘积 R **明显小于**底座 R 0.3686（= 「两处都是绿」这一条真的生效）｜R=" + prod2.r
              + " · base=" + face.r);
        Check(prod2.g > 0.999f && prod2.b > 0.68f,
              "A7c 5-06（灭自证）G **饱和到 1.0**、B ≈ 0.6901 —— ⛔ 旧口径「先夹后乘」会给 0.8941 / 0.5874 ⇒ 红｜g="
              + prod2.g + " · b=" + prod2.b);

        Color prod1 = WindowButton.ComposeVertexColor(face, face, 1f);
        Check(!Near(prod1, prod2, 1e-3f),
              "A8 5-06 乘数 = 1 那一档 (0.1359, 0.7994, 0.3451) ≠ 乘数 = 2 那一档 ⇒ 乘数**真的**改变结果｜实得 " + prod1);
    }

    // ==================================================================
    //  B · 接线（源码结构；灭自证的另一半）
    // ==================================================================
    static void GroupB_Wiring()
    {
        var pmSrc = Src("Shell/PracticeModePopup.cs");
        var mcSrc = Src("Battle/MultiCardDisplay.cs");
        var wbSrc = Src("Shell/WindowButton.cs");
        var fxSrc = Src("Battle/BattleButtonFx.cs");

        // ---- B1/B2 · PracticeModePopup 的接线 ----
        Check(pmSrc.Contains("var battleHit = HitOn(root, root, \"BattleHit\""),
              "B1 5-06 `BattleHit` 那颗命中区建出来后被接住了（否则 `WireContinueButton` 拿不到它）");
        Check(pmSrc.Contains("WireContinueButton(battleHit, contQuad);"),
              "B1b 5-06 真的调了 `WireContinueButton(battleHit, contQuad)`（删掉 ⇒ 四态/乘数全不生效）");
        Check(pmSrc.Contains("var contQuad = Img(root, \"40k_bt_underbutton\"")
              && pmSrc.Contains("ContinueFaceColor);"),
              "B2 A16 那颗可见图的 tint 用 `ContinueFaceColor`（原版 Image `m_Color`）");
        Check(!pmSrc.Contains("new Color(0.369f, 0.894f, 0.587f, 1f)"),
              "B2b A16 旧的圆整值 `(0.369, 0.894, 0.587)` 已不存在（⛔ 别退回不精确的字面量）");

        // ---- B3/B4 · WireContinueButton 的内容 ----
        Check(pmSrc.Contains("wb.NormalKey = ContinueNormal;")
              && pmSrc.Contains("wb.HighlightKey = ContinueHighlighted;")
              && pmSrc.Contains("wb.PressedKey = ContinuePressed;")
              && pmSrc.Contains("wb.SelectedKey = ContinueSelected;")
              && pmSrc.Contains("wb.DisabledKey = ContinueDisabled;"),
              "B3 5-06 Continue 钮把**四态 + 禁用色**逐格赋给了 `WindowButton`");
        Check(pmSrc.Contains("wb.ColorMultiplier = ContinueColorMultiplier;"),
              "B3b 5-06 乘数真的写进了 `WindowButton.ColorMultiplier`");
        Check(pmSrc.Contains("wb.ColorTintGreyOnDisable = false;"),
              "B3c 5-06 这颗是纯 `UnityEngine.UI.Button` ⇒ **不**走 `greyOnDisable` 那套（禁用 α 保持 0.5019608）");
        Check(pmSrc.Contains("wb.TintOn(face)"),
              "B4 5-06 色偏目标 = 那颗可见 `Continue Button`（删掉 ⇒ 四态打在透明命中区上，画面不动）");
        Check(pmSrc.Contains("wb.ApplyTintInstant();"),
              "B4b 5-06 建完立刻落 Normal（= 原版 `OnEnable → DoStateTransition(Normal, instant)`）");

        // ---- B5/B6/B7/B8 · MultiCardDisplay 的接线 ----
        Check(mcSrc.Contains("if (_bar != null) _bar.SetTint(BarColor);"),
              "B5 A16 战斗侧底座绿**无条件**写（旧写法 `&& barTex == null` ⇒ 有贴图时画成白 tint）");
        Check(!mcSrc.Contains("barTex == null) _bar.SetTint(BarColor)"),
              "B5b A16 旧的 `barTex == null` 那道闸已删");
        Check(mcSrc.Contains("BattleButtonFx.Tint(_bar, \"Continue Button\",")
              && mcSrc.Contains("BattleButtonFx.ContinueNormal")
              && mcSrc.Contains("BattleButtonFx.ContinueHighlighted")
              && mcSrc.Contains("BattleButtonFx.ContinuePressed")
              && mcSrc.Contains("BattleButtonFx.ContinueColorMultiplier"),
              "B6 5-06 战斗侧 Continue 钮传了**四态 + 乘数**（不传 = 走缺省白/1.0）");
        Check(mcSrc.Contains("_fxBar.ColorTintGreyOnDisable = false;"),
              "B7 5-06 战斗侧也标记成纯 `UnityEngine.UI.Button`（不换灰化材质）");
        Check(mcSrc.Contains("_fxBar.ApplyInstant();"),
              "B7b 5-06 战斗侧建完立刻落 Normal（原版 `OnEnable → DoStateTransition(Normal, instant)`）");
        Check(!mcSrc.Contains("0.4666667"),
              "B8 5-06 战斗侧**没有**自己再抄一份四态字面量（一份判据只留一处 —— 用 `BattleButtonFx.Continue*`）");

        // ---- B9/B10 · ✅ 已复刻（2026-11-04 · B141 · W57 统一口径）：两套按钮共用同一个算式口 ----
        Check(wbSrc.Contains("ComposeVertexColor(b, _k, ColorMultiplier)"),
              "B9 5-06 外壳侧 `WindowButton.ApplyTint` 走**唯一口** `ComposeVertexColor(底座, 色键, 乘数)`");
        Check(fxSrc.Contains("WindowButton.ComposeVertexColor(_base, _key, 1f)"),
              "B9b 5-06 战斗侧 `BattleButtonFx.Apply` 走**同一个口**（乘数已在 ctor 乘进四态 ⇒ 传 1）");
        Check(wbSrc.Contains("public static Color ComposeVertexColor(Color baseColor, Color key, float multiplier)")
              && wbSrc.Contains("Mathf.Clamp01(baseColor.r * key.r * multiplier)"),
              "B9c 5-06 唯一算式 `Clamp01(底座 × 色键 × 乘数)` 就收在 `ComposeVertexColor` 里（⛔ 别在任一侧另写一份）");
        Check(!wbSrc.Contains("Mathf.Clamp01(k.r * ColorMultiplier)") && !fxSrc.Contains("_base.r * _key.r"),
              "B10 5-06（灭自证）两侧的**旧口径都不得回来**：外壳侧 `Clamp01(k.r * ColorMultiplier)` 与战斗侧裸乘 "
              + "`_base.r * _key.r` 都不在源码里（任一侧改回旧口径 ⇒ 红）");
    }
}
