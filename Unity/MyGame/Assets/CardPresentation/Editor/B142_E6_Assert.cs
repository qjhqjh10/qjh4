// B142_E6_Assert.cs — 执行代理 `B142-E6`（第一百二十九会话 · 波次 `B142`）
//   盯的是 `B141-TOOLTIP-CJK` / `B141-TOOLTIP-WRAP` / `B141-TOOLTIP-MAXH` 三条 —— 它们指向**同一条**
//   `Core/Tooltip.cs` 的 `Layout` 链（判据文档 `判据/B142_对账_W129界面参数.md` 顺手发现 8）。
//
// ============================ 判据（逐字段现读 2026-11-04）============================
//   `d:/2/tools/assetripper_out/ExportedProject/Assets/BasicToolTip.prefab`：
//     · 正文 `Text (TMP)` `m_fontSize = 28`（`:481`）· `m_enableAutoSizing 1` / min 10 / max 28（`:484-486`）
//     · 正文框 `m_SizeDelta = {x: 468.90002, y: 0}`（`:424`）· `m_TextWrappingMode: 1`（`:497`）= TMP 自己在 468.9 px 里自动折
//     · 正文 `m_overflowMode: 0`（`:499`，Overflow）⇒ **超高不裁不省略**，就那么画出去
//     · 正文自定义 fitter `clampHeight 1` / `heightMin 90` / `heightMax 200`（`:542-544`）
//     · `Generic Popup Background` 的 `VerticalLayoutGroup.m_Padding` 上下各 10（`:228`）⇒ 面板高 ∈ **[110, 220]**
//   栏宽自洽：`(500 − 468.9) / 2 = 15.55`（与 `Text (TMP)` 的 `m_AnchoredPosition.x = 250` 独立算出同一个数）。
//
// ============================ 🔴 灭自证（🧨 什么坏法会让哪条红）============================
//  · **B1** 断「中文 tooltip 正文的 em 读回 = `BodyMaxPx`（桌面 28 / 小屏 34）」，期望值**从字体资产反算**（不是把常量再乘一遍）；
//    🧨 把 `Layout` 里那句 `SetScriptHeight(wrapped, BodyMaxPx, UnitPx)` 改回 `SetCapHeight(BodyCap)`
//    （= 改前的写法）⇒ 读回 **20.11** ⇒ **B1 红、B6 红**（A5 那条「旧口径算出来就是 20.11」与 B1 **结构上不可能同时成立**）。
//    🔴 **2026-11-04（`B142-E72`）**：B1/B8/B10b 原先把期望钉死在 `BodyFontPx`（= 28）⇒ `B142-E60` 之后**只有** `smallScreenUI = true` 时会红（那时正文 em = `BodyMaxPx` = 34）；已按 E60 同口径改读 `Tooltip.BodyMaxPx`。
//  · **B8** 断「CJK 一行 = **floor(468.9 / BodyMaxPx)** 或再少 1 字」（桌面 16/15 · 小屏 13/12；`B142-E25` 起 `SetAutoFitBox` 会把
//    `textWrappingMode` 设成 `Normal`，若真机量出 CJK 步进略大于 em（16 字 = 472 px > 468.9）TMP 会再折成少 1 字 —— 两者都算对，真机量 `W=` 可定）；🧨 把 `MaxCharsPerLine` 改回写死的 `22` ⇒ 一行 22 字 ⇒ 红；A7 另把 `WrapWidthPx` 与「面板宽 − 2×pad」钉在一起。
//  · **B9** 断「超长文案（**480 字 = 30 行**）的面板高 = 220 且正文高 > 200（溢出而不是被压）」，🧨 去掉 `Mathf.Clamp(..., MaxH)`
//    ⇒ 面板高跟着行数涨到 400+ ⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主里加：
//      B142_E6_Assert.Run(); pass += B142_E6_Assert.Pass; fail += B142_E6_Assert.Fail;
//  推荐宿主 = `Editor/BattleScene.cs`（tooltip 已有 §15c 那一段、且战斗侧是 uGUI 档）或 `Editor/ShellScene.cs`（9 s，最便宜）。
//  ⚠️ 本段会建一只 tooltip + 一只探针 Label，**结束前都 DestroyImmediate**（`Tooltip` 是**静态单例**，不清会污染后面的用例）。
//  判绿红：按行首 `B142E6 ✓` / `B142E6 ✗` 数（⛔ 别 `grep -c ✗`）。

using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B142-E6`：Tooltip 正文的语种字号（CJK 28 px）/ 像素宽折行（468.9）/ 面板高上限（220）。</summary>
public static class B142_E6_Assert
{
    const string Tag = "B142E6";
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

    static void Near(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（" + got.ToString("F4") + " ≈ " + want.ToString("F4") + " ± " + tol.ToString("F4") + "）");
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        Debug.Log(Tag + " --- Tooltip 正文：语种字号（28 px）+ 像素宽折行（468.9）+ 面板高上限（220）"
                       + "（判据 = 原版 `BasicToolTip.prefab`）---");
        GroupA_Params();
        GroupB_Applied();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail);
    }

    // ==================================================================
    //  A · 参数层（`BasicToolTip.prefab` 逐字段，全静态）
    // ==================================================================
    static void GroupA_Params()
    {
        // ---- 4-14 / MAXH：面板高上限 ----
        Near(Tooltip.MaxH, 220f, 0.001f,
             "A1 ★ 面板**高上限 = 220**（原版正文 fitter `heightMax 200` + 上下各 10；`:542-544`）"
           + "｜🧨 删掉 `MaxH`（改回只有下限）⇒ 本红 + B9 红");
        Near(Tooltip.MaxH - Tooltip.PadY * 2f, 200f, 0.001f,
             "A2 面板高上限 − 2×上下内边距 = **正文高上限 200**（= prefab 里那个 `heightMax`；两段独立算出来的同一数）");
        Near(Tooltip.MinH, 110f, 0.001f,
             "A3 面板高下限仍是 **110**（`heightMin 90` + 上下各 10）—— 上限这一条**没有**把下限顶掉");

        // ---- CJK / WRAP：折行宽 = 原版正文框宽 ----
        Near(Tooltip.WrapWidthPx, 468.90002f, 0.01f,
             "A4 ★ 折行宽 = **468.9 px**（原版正文 `m_SizeDelta.x`，`:424`）");
        Near(Tooltip.WrapWidthPx - (Tooltip.MinW - Tooltip.PadX * 2f), 0f, 0.02f,
             "A5 ★ 折行宽与面板几何**自洽**：`MinW − 2×PadX` = 500 − 2×15.55 = 468.9"
           + "｜🧨 单改 `WrapWidthPx` 或 `PadX`（另一条不动）⇒ 本红（两条不可能同时被拨歪）");
        Near(Tooltip.BodyFontPx, 28f, 0.001f,
             "A6 ★ 正文标称字号 = **28 px**（= 原版 `m_fontSize`，`:481`；`SetScriptHeight` 的 px 口径）");

        // 折行单位：CJK 一行 = floor(468.9 / 28) = 16 字
        string w = Tooltip.WrapForTest(new string('试', 40));
        string first = w.Split('\n')[0];
        int wantPerLine = Mathf.FloorToInt(Tooltip.WrapWidthPx / Tooltip.BodyFontPx);
        Check(first.Length == wantPerLine,
              "A7 ★ CJK 折行 = **" + wantPerLine + " 字/行**（468.9 px ÷ 28 px/字 —— 像素宽口径，不是字符数）"
            + "｜实得首行 " + first.Length + " 字");
        Check(first.Length != 22,
              "A8 ★……而且**不再是旧口径的 22 字/行**（22 是「字符数」常数，与 468.9 px 无关）｜实得 " + first.Length);
        Check(w.Replace("\n", "") == new string('试', 40),
              "A9 折行只插 `\\n`、一个字都不改（`Wrap` 的既有不变量）");
    }

    // ==================================================================
    //  B · 实例级：真的进了 `Layout`
    // ==================================================================
    static void GroupB_Applied()
    {
        // 先清掉上一轮留下的件（静态单例、档位可能已变），并解开别人的抑制
        var stale = Tooltip.InstanceForTest;
        if (stale != null) { Tooltip.Hide(); Tooltip.FinishFade(); UnityEngine.Object.DestroyImmediate(stale); }
        if (Tooltip.TooltipsSuppressed)
        {
            Debug.Log(Tag + " ⚠ 进入本段时 tooltip 被别的 owner 抑制着（" + Tooltip.SuppressorCountForTest
                          + " 个）⇒ 已按自检口清空");
            Tooltip.ResetSuppressionsForTest();
        }
        Check(!Tooltip.TooltipsSuppressed, "B0a（前提）清空后没有 owner 再抑制 tooltip");

        float unitPx = Tooltip.UguiMode ? 1f : Tooltip.PxPerUnit;     // 「本档单位 → 画布 px」的折算

        // 期望值**从字体资产反算**（⛔ 不是把常量再乘一遍）
        float capCjk = TmpFont.CapPerFontSize(TmpFont.Face.Cjk);
        float emCjk = TmpFont.EmPerFontSize(TmpFont.Face.Cjk);
        float capLat = TmpFont.CapPerFontSize(TmpFont.Face.LatinCardBody);
        float emLat = TmpFont.EmPerFontSize(TmpFont.Face.LatinCardBody);
        Check(capCjk > 0.05f && emCjk > 0.05f && capLat > 0.05f && emLat > 0.05f,
              "B0b（前提）两个面的 `CapPerFontSize` / `EmPerFontSize` 都量到了（CJK " + capCjk.ToString("F5")
            + "/" + emCjk.ToString("F5") + " · 拉丁 " + capLat.ToString("F5") + "/" + emLat.ToString("F5")
            + "；量不到说明字体资产没载入 ⇒ B1/B6 不算数）");
        if (!(capCjk > 0.05f && emCjk > 0.05f && capLat > 0.05f && emLat > 0.05f)) return;

        // ---- B1 · 中文：改后的 em 读回必须是 `BodyMaxPx`（桌面 28 / 小屏 34；改前是 20.11）----
        string cjk = new string('试', 40);
        Tooltip.Show(cjk, Vector3.zero, 0);
        Tooltip.FinishFade();
        Check(Tooltip.InstanceForTest != null,
              "B1a（前提）真建出了一只 tooltip｜⛔ 建不出就别读字号（本行先红，不静默跳过）");
        if (Tooltip.InstanceForTest == null) return;

        float emNow = ParseAfter(Tooltip.DebugDump(), " em=");
        Near(emNow, Tooltip.BodyMaxPx, 0.4f,
             "B1 ★ 中文正文的 em 读回 = **BodyMaxPx**（桌面 28 / 小屏 34 px；本档 = " + Tooltip.BodyMaxPx.ToString("F0")
           + "）（`Label.SetScriptHeight` 的 CJK 路：汉字墨高 = px；`B142-E60` 起 `Layout` 下发的是 `BodyMaxPx`，⛔ 别再钉死 28）"
           + "｜🧨 `Layout` 改回 `SetCapHeight(BodyCap)` ⇒ 读回 20.11 ⇒ 本红");

        // ---- B6 · 结构对照：**旧写法**在同一个 Label 上读回 20.11 ----
        float oldWant = Tooltip.BodyFontPx * (capLat / emLat) * emCjk / capCjk;   // = 28×0.656×em/cap ≈ 20.11
        var probeRoot = new GameObject("b142e6_probe");
        try
        {
            var lab = Label.Create(probeRoot.transform, "", Vector3.zero, 2, Color.white,
                                   new Vector2(0.5f, 0.5f), "b142e6_probe_label");
            Check(lab != null, "B6a（前提）建得出探针 Label");
            if (lab != null)
            {
                lab.SetFace(TmpFont.Face.LatinCardBody);
                lab.SetCapHeight(Tooltip.BodyCapWorld * (Tooltip.UguiMode ? Tooltip.PxPerUnit : 1f));
                lab.SetText("试");                       // 汉字 ⇒ `ActualFace` 翻 CJK（`SyncFace`）
                float oldEm = lab.FontPxNow;
                Near(oldEm, oldWant, 0.4f,
                     "B6 ★ **改前那一条路**（`SetCapHeight(BodyCap)`）在同样的中文上读回 "
                     + oldWant.ToString("F2") + " px —— 那就是被修掉的 −" + ((1f - oldWant / 28f) * 100f).ToString("F1")
                     + "% ｜🧨 这条与 B1 **不可能同时成立**（一个是 20.11、一个是 28）");
                lab.SetScriptHeight("试", Tooltip.BodyFontPx, Tooltip.PxPerUnit);
                Near(lab.FontPxNow, Tooltip.BodyFontPx, 0.4f,
                     "B7 ★ 同一个探针换 `SetScriptHeight` 之后读回 **28 px**（两条路的差 = 本批修的那 28.2%）");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(probeRoot); }

        // ---- B8 · 折行真按像素宽断 ----
        string rendered = Tooltip.BodyTextForTest ?? "";
        var lines = rendered.Split('\n');
        int maxCjk = 0, over = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            int n = 0;
            for (int k = 0; k < lines[i].Length; k++) if (lines[i][k] >= 0x2E80) n++;
            if (n > maxCjk) maxCjk = n;
            if (n > 17) over++;
        }
        Check(maxCjk <= Mathf.FloorToInt(Tooltip.WrapWidthPx / Tooltip.BodyMaxPx) && maxCjk >= Mathf.FloorToInt(Tooltip.WrapWidthPx / Tooltip.BodyMaxPx) - 1,
              "B8 ★ 最长的一行 = **" + maxCjk + " 个 CJK 字**（接受 **floor(468.9 / BodyMaxPx)** 或再少 1 字 = 桌面 16/15 · 小屏 13/12：手折口径 = floor(468.9/" + Tooltip.BodyMaxPx.ToString("F0") + ") = " + Mathf.FloorToInt(Tooltip.WrapWidthPx / Tooltip.BodyMaxPx) + "；`B142-E25` 起 `textWrappingMode = Normal`，若真机量出 CJK 步进略大于 em ⇒ TMP 再折成少 1 字）"
            + "｜🧨 改回 `MaxCharsPerLine = 22f` ⇒ 最长行 22 字 ⇒ 本红｜哪一档对由真机量 `DebugDump()` 的 `W=` 定（≈ 行数×BodyMaxPx 就对上），⛔ 不是改这条断言");
        Check(over == 0,
              "B8b 没有任何一行超过 17 字（超过就意味着行宽 > 468.9 px 的预算）｜超限行数 " + over);

        // 面板宽：**不低于 500**、**不超过「正文框 + 一个字/行」的量化余量**，且一定盖得住正文。
        // ⚠️ 不写死 `== 500`：`Wrap` 仍是**按字宽估**（CJK 1 em / ASCII 0.5 em），一行的真实渲染宽
        //    最多比 468.9 差一个字 ⇒ 面板最多 468.9 + 2×15.55 + 28 ≈ 528。真正的判别力在 **B8**（15/16 字/行 vs 旧口径 22）。
        string dump2 = Tooltip.DebugDump() ?? "";
        float bodyWpx = ParseAfter(dump2, "W=") * unitPx;
        Vector2 sz = Tooltip.PanelSizePx;
        Check(sz.x >= Tooltip.MinW - 0.5f,
              "B10 ★ 面板宽**不低于 500**（= 原版根 `m_SizeDelta.x`；`MinW` 兜底真的在起作用）｜实得 "
            + sz.x.ToString("F1"));
        Check(sz.x <= Tooltip.WrapWidthPx + Tooltip.PadX * 2f + Tooltip.BodyMaxPx,
              "B10b ★ 面板宽**不超过**「正文框 468.9 + 2×pad + 一个字的量化余量（BodyMaxPx）」= "
            + (Tooltip.WrapWidthPx + Tooltip.PadX * 2f + Tooltip.BodyMaxPx).ToString("F1")
            + "｜实得 " + sz.x.ToString("F1")
            + "｜🧨 改回 `MaxCharsPerLine = 22f` ⇒ 一行 22 字 ⇒ 面板被 `MaxW` 顶到 640 ⇒ 本红");
        Check(sz.x + 0.5f >= bodyWpx + Tooltip.PadX * 2f,
              "B10c 面板宽盖得住正文宽（字不会画到底图外）｜面板 " + sz.x.ToString("F1")
            + " ≥ 正文 " + bodyWpx.ToString("F1") + " + " + (Tooltip.PadX * 2f).ToString("F1"));

        // ---- B9 · 超高：面板夹在 220，正文溢出（原版 `m_overflowMode: 0`，不裁不缩）⚠️ 夹具 **480 字 = 30 行**（`B142-E25` 自适应会把 240 字 / 15 行缩到 ~10.3 px ⇒ 约 194 px **装得进** 200 px 框 ⇒ 面板 ~214 且不溢出 ⇒ 原来那对期望会红）；30 行即使压到 10 px 地板仍 ≈390 px > 200 ⇒ 面板必然 220、正文必然溢出（与 `B142-E25` 的 `B6` 同一夹具）
        string huge = new string('试', 480);                 // 480 / 16 = 30 行
        Tooltip.Show(huge, Vector3.zero, 0);
        Tooltip.FinishFade();
        string dump = Tooltip.DebugDump() ?? "";
        Vector2 szH = Tooltip.PanelSizePx;
        float bodyHpx = ParseAfter(dump, "H=") * unitPx;
        Near(szH.y, Tooltip.MaxH, 0.5f,
             "B9 ★ 超长正文（30 行 = 480 字）的面板高被夹到 **220**（原版 fitter `heightMax 200` + 上下各 10）"
           + "｜🧨 去掉 `Mathf.Clamp(..., MaxH)` ⇒ 面板跟着行数涨到 400+ ⇒ 本红");
        Check(bodyHpx > 200f || ParseAfter(dump, " em=") <= Tooltip.BodyMinPx + 0.6f,
              "B9b ★……而且正文**没有被压**（量到 " + bodyHpx.ToString("F1") + " px > 200，**或** em 已到 " + Tooltip.BodyMinPx.ToString("F0") + " px 地板 —— 自适应到地板仍装不进时原版就是「照样画出去」）"
            + "—— 原版 `m_overflowMode: 0`（Overflow，`:499`）就是「fitter 到 200 之后照样画出去」，我们同此"
            + "（`Label` 不裁不缩，只有面板被夹）；30 行在 10 px 下 ≈390 px ⇒ 两条都该成立");

        // ---- 短文案：下限还在 ----
        Tooltip.Show("x", Vector3.zero, 0);
        Tooltip.FinishFade();
        Vector2 szS = Tooltip.PanelSizePx;
        Near(szS.y, Tooltip.MinH, 0.5f,
             "B11 ★ 单行短文案的面板高仍是 **110**（`MinH` 没被这次的上限改动顶掉）");

        Tooltip.Hide();
        Tooltip.FinishFade();
        var go = Tooltip.InstanceForTest;
        if (go != null) UnityEngine.Object.DestroyImmediate(go);
        Tooltip.ResetSuppressionsForTest();
    }

    // ==================================================================
    //  小工具
    // ==================================================================

    /// <summary>从 `DebugDump()` 那种 `key=数值` 串里取一个 float（取不到 ⇒ `NaN` ⇒ 断言红，⛔ 不当 0）。</summary>
    static float ParseAfter(string s, string key)
    {
        if (string.IsNullOrEmpty(s)) return float.NaN;
        int i = s.IndexOf(key, System.StringComparison.Ordinal);
        if (i < 0) return float.NaN;
        i += key.Length;
        var sb = new System.Text.StringBuilder();
        while (i < s.Length)
        {
            char c = s[i];
            if (char.IsDigit(c) || c == '.' || c == '-' || c == '+' || c == 'e' || c == 'E') { sb.Append(c); i++; }
            else break;
        }
        float v;
        return float.TryParse(sb.ToString(), System.Globalization.NumberStyles.Float,
                              System.Globalization.CultureInfo.InvariantCulture, out v) ? v : float.NaN;
    }
}
