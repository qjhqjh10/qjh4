// B142_E25_Assert.cs — 执行代理 `B142-E25`（第一百二十九会话 · 波次 `B142`）
//   盯的是 `B142-TOOLTIP-AUTOSIZE`（原版正文**自动缩字号**我们没做 —— 见 `报告/B142_E6.md` §四·1）
//   ＋ `B142-TOOLTIP-OVERFLOW`（原版「超高向下溢出」**判据本身错** —— 见本文件 §B3 与 `报告/B142_E25.md` §四·2）。
//
// ============================ 判据（逐字段现读 2026-11-04）============================
//   `d:/2/tools/assetripper_out/ExportedProject/Assets/BasicToolTip.prefab` · 正文 `Text (TMP)`：
//     · `m_fontSize: 28`（`:481`）· `m_fontSizeBase: 28`（`:482`）
//     · `m_enableAutoSizing: 1`（`:484`）· `m_fontSizeMin: 10`（`:485`）· `m_fontSizeMax: 28`（`:486`）
//     · `m_HorizontalAlignment: 2`（`:488`）· **`m_VerticalAlignment: 512` = Middle**（`:489`）
//     · `m_SizeDelta = {468.90002, 0}`（`:424`）· `m_TextWrappingMode: 1`（`:497`）· `m_overflowMode: 0`（`:499`）
//     · 自定义 fitter `ContentSizeFitterMinMax`：`clampHeight 1` / `heightMin 90` / `heightMax 200`（`:542-544`）
//       —— 反编译 `d:/2/tools/decomp_full/ContentSizeFitterMinMax__ClampSize.c` 实证：内容驱动出高度后
//       **再把 `sizeDelta` 夹进 [heightMin, heightMax]**。
//     · `Generic Popup Background` 的 `VerticalLayoutGroup.m_Padding` 上下各 10（`:228-232`）、`m_ChildAlignment: 4`
//   ⇒ 框高 = `clamp(正文自然高, 90, 200)` = `MinH−2×PadY … MaxH−2×PadY`（与 `_h` 的 Clamp **同源**）。
//
// 🔴 **`m_VerticalAlignment: 512`（Middle）⇒ 「超高向下溢出」不成立**（本文件 B3 钉这一条）：
//   TMP 的 Middle 档把整块字的**竖直中心**放在**容器矩形中心**
//   （`Library/PackageCache/com.unity.ugui@…/Runtime/TMP/TextMeshPro.cs:4202-4206`：
//    `anchorOffset = (corners[0] + corners[1]) / 2 + (0, −(m_maxTextAscender + … ) / 2)`）；
//   而容器矩形被 `VerticalLayoutGroup` 摆在**上下 pad 对称**的位置（`:228-232` 的 10/10，且
//   `ContentSizeFitter.VerticalFit=1` 让面板高 = 正文高 + 20 ⇒ 富余量 0 ⇒ 子件正好居中）
//   ⇒ 超高时文字**上下对称**溢出，与我们的实现一致。`报告/B142_E25.md` §四·2 有完整推导。
//
// ============================ 🔴 灭自证（🧨 什么坏法会让哪条红）============================
//  · **B1** 断「超长文案的 em 落进 [10, 28] 且真的缩了」。🧨 把 `Layout` 里新加的 `SetAutoFitBox` 删掉
//    （= 改前的写法）⇒ 超长文案的 em 仍是 **28.00**、正文高 ~360 px ⇒ **B1 / B4 同时红**。
//  · **B1c** 断「同一个超长文案、**不开自适应**的探针读回 28.00」，期望值**从探针实测**来（不是把常量再念一遍）
//    ⇒ 这条与 B1 **结构上不可能同时成立**（一个是缩过的、一个是没缩的）。
//  · **B3** 断「超长时正文标签中心 = 面板中心」。🧨 把 `Pivot()` 里 `_body.transform.localPosition` 改成
//    顶锚（`cy + (h/2 − PadY)`，= 向下溢出的写法）⇒ 本红。
//  · **B2/A2** 把框高的两个界与 `MinH/MaxH/PadY` 互钉：🧨 单改任一边（改成写死的 90/200 第二份常数、
//    或把 `MaxH` 挪走）⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主里加：
//      B142_E25_Assert.Run(); pass += B142_E25_Assert.Pass; fail += B142_E25_Assert.Fail;
//  推荐宿主 = `Editor/BattleScene.cs` 的 tooltip §15c 聚合行，或 `Editor/ShellScene.cs`（9 s，最便宜）。
//  ⚠️ 本段会建一只 tooltip + 探针 Label，**结束前都 DestroyImmediate**（`Tooltip` 是**静态单例**，不清会污染后面的用例）。
//  判绿红：按行首 `B142E25 ✓` / `B142E25 ✗` 数（⛔ 别 `grep -c ✗`）。

using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B142-E25`：Tooltip 正文**自适应缩字号**（[10, 28] px，框 468.9 × clamp(自然高,90,200)）。</summary>
public static class B142_E25_Assert
{
    const string Tag = "B142E25";
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
        Debug.Log(Tag + " --- Tooltip 正文自适应缩字号（原版 `BasicToolTip.prefab:484-486` + fitter `:542-544`）---");
        GroupA_Params();
        GroupB_Applied();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail);
    }

    // ==================================================================
    //  A · 参数层（`BasicToolTip.prefab` 逐字段，全静态）
    // ==================================================================
    static void GroupA_Params()
    {
        Near(Tooltip.BodyMinPx, 10f, 0.001f,
             "A1 ★ 自适应**下限 = 10 px**（原版 `m_fontSizeMin: 10`，`:485`）"
           + "｜🧨 传 0/负数 ⇒ `SetAutoFitBox` 走「入参不成立」出声分支 ⇒ 自适应整个不生效");

        // 框高的两个界 = 面板高区间 − 上下内边距 —— 与 `_h` 的 Clamp **同源**（⛔ 不写第二份常数）
        Near(Tooltip.MaxH - Tooltip.PadY * 2f, 200f, 0.001f,
             "A2 ★ 自适应框的**高度上限 = 200 px**（= 原版 fitter `heightMax: 200`，`:544`）"
           + "—— 由 `MaxH − 2×PadY` 推出来，不是另写一个 200");
        Near(Tooltip.MinH - Tooltip.PadY * 2f, 90f, 0.001f,
             "A3 ★ 自适应框的**高度下限 = 90 px**（= 原版 fitter `heightMin: 90`，`:543`）—— 由 `MinH − 2×PadY` 推出来"
           + "｜🧨 单改 `MaxH`/`MinH`/`PadY` 任一个 ⇒ A2/A3 与 B 段的框高自洽性一起红");

        Near(Tooltip.BodyFontPx, 28f, 0.001f,
             "A4 自适应**上限仍是标称 28 px**（原版 `m_fontSizeMax: 28`，`:486`）");
        Check(Tooltip.BodyMinPx < Tooltip.BodyFontPx - 0.5f,
              "A5 ★ 区间**非退化**（min 10 < max 28）｜🧨 把两者写成同一个数 ⇒ 「缩字号」退化成空操作 ⇒ 本红");
    }

    // ==================================================================
    //  B · 实例级：真的进了 `Layout`
    // ==================================================================
    static void GroupB_Applied()
    {
        // 先清掉上一轮留下的件（静态单例），并解开别人的抑制
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
        float boxMin = Tooltip.MinH - Tooltip.PadY * 2f;              // 90
        float boxMax = Tooltip.MaxH - Tooltip.PadY * 2f;              // 200

        // 字体面量不到就一切免谈（先硬断，⛔ 不静默跳过）
        float emCjk = TmpFont.EmPerFontSize(TmpFont.Face.Cjk);
        Check(emCjk > 0.05f,
              "B0b（前提）CJK 面的 `EmPerFontSize` 量到了（" + emCjk.ToString("F5") + "）"
            + "—— 量不到说明字体资产没载入 ⇒ 下面几条不算数");
        if (!(emCjk > 0.05f)) return;

        // ---- 普通文案：仍是 28 px（上限那一格不许被自适应改写）----
        Tooltip.Show("试", Vector3.zero, 0);
        Tooltip.FinishFade();
        Check(Tooltip.InstanceForTest != null,
              "B1a（前提）真建出了一只 tooltip｜⛔ 建不出就别读字号（本行先红，不静默跳过）");
        if (Tooltip.InstanceForTest == null) return;

        float emShort = ParseAfter(Tooltip.DebugDump(), " em=");
        Near(emShort, Tooltip.BodyMaxPx, 0.6f,
             "B1 ★ 单行短文案的 em 读回 **本档上限 px**（桌面 28 / 小屏 34，`B142-E60` 起改读 `BodyMaxPx` —— 门控判据见 `Core/Tooltip.cs` 常量行：原版 `EverguildTextController.Awake` 只在 `smallScreenUI` 档把 max 覆写成 34；框高被下限 90 兜住 ⇒ 装得下 ⇒ 不缩）"
           + "｜🧨 把 `fontSizeMax` 误写成下限/把 base 写错 ⇒ 本红");
        Vector2 szShort = Tooltip.PanelSizePx;
        Near(szShort.y, Tooltip.MinH, 0.5f,
             "B1b 短文案面板高仍是 **110**（自适应没把下限顶掉）");

        // ---- 超长文案：缩字号 ----
        string longText = new string('试', 160);          // 160 / 16 = 10 行；28 px 下 ≈ 360 px > 200
        Tooltip.Show(longText, Vector3.zero, 0);
        Tooltip.FinishFade();
        string dumpLong = Tooltip.DebugDump() ?? "";
        float emLong = ParseAfter(dumpLong, " em=");
        float bodyHLong = ParseAfter(dumpLong, " H=") * unitPx;
        Vector2 szLong = Tooltip.PanelSizePx;

        Check(emLong >= Tooltip.BodyMinPx - 0.6f && emLong <= Tooltip.BodyMaxPx + 0.6f,
              "B2 ★ 超长文案的 em **落在 [10, 本档上限] px**（原版 `m_fontSizeMin/Max`，`:485-486`；上限 = `BodyMaxPx`：桌面 28 / 小屏 34）｜实得 "
            + emLong.ToString("F2") + " px｜🧨 删掉 `SetAutoFitBox` ⇒ 读回 28.00 但正文高 ~360 ⇒ B2 仍过、**B3 红**");
        Check(emLong <= Tooltip.BodyMaxPx - 1f,
              "B2b ★……而且是**真的缩了**（≤ " + (Tooltip.BodyMaxPx - 1f).ToString("F0") + " px）｜实得 "
            + emLong.ToString("F2") + " px｜🧨 自适应没接上 ⇒ 本红");
        Check(emLong >= Tooltip.BodyMinPx - 0.6f,
              "B2c ★……**没有跌破下限 10 px**（TMP 的自适应地板 = 原版 `m_fontSizeMin`）｜实得 "
            + emLong.ToString("F2") + " px");

        // ---- 灭自证：同文案 + **不开自适应** 的探针 ----
        var probeRoot = new GameObject("b142e25_probe");
        try
        {
            string wrapped = Tooltip.WrapForTest(longText);
            var probe = Label.Create(probeRoot.transform, "", Vector3.zero, 2, Color.white,
                                     new Vector2(0.5f, 0.5f), "b142e25_probe_label");
            Check(probe != null, "B3a（前提）建得出探针 Label");
            if (probe != null)
            {
                probe.SetFace(TmpFont.Face.LatinCardBody);
                probe.SetText(wrapped);
                probe.SetScriptHeight(wrapped, Tooltip.BodyFontPx, unitPx);   // ✅ 2026-11-04（B142-E46）：世界档 = `PxPerUnit`(108)、uGUI 档 = 1（原来写死 `PxPerUnit` ⇒ uGUI 档折算错 108 倍）
                float probeEm = probe.FontPxNow;
                float probeHpx = probe.WorldH * unitPx;                     // ✅ 2026-11-04（B142-E46）：同上，按本档折算（`Tooltip.UnitPx` 的口径）
                Near(probeEm, Tooltip.BodyFontPx, 0.6f,
                     "B3 ★ **不开自适应**的探针在同样文案上读回 **28.00 px**（= 改前的行为）"
                   + "—— 与 B2b「超长必须 ≤ 27」**结构上不可能同时成立**（🧨 把自适应删掉 ⇒ 这里仍 28、B2b 红）");
                Check(bodyHLong < probeHpx - 20f,
                      "B3b ★ 开了自适应之后正文**真的矮下来**了｜tooltip " + bodyHLong.ToString("F1")
                    + " px < 探针（不缩）" + probeHpx.ToString("F1") + " px − 20"
                    + "｜🧨 删掉 `SetAutoFitBox` ⇒ 两者逐位相同 ⇒ 本红");
                Check(bodyHLong <= boxMax + probeHpx / Mathf.Max(1f, wrapped.Split('\n').Length) + 1f,
                      "B3c ★……而且缩到**装进 200 px 的框**（或触到字号地板）｜正文高 "
                    + bodyHLong.ToString("F1") + " px ≤ 框高 200 + 一行余量");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(probeRoot); }

        // ---- 面板高仍 ∈ [110, 220] ----
        Check(szLong.y >= Tooltip.MinH - 0.5f && szLong.y <= Tooltip.MaxH + 0.5f,
              "B4 ★ 超长文案的面板高仍在 **[110, 220]**（与 `B142-E6` 的 Clamp 自洽）｜实得 "
            + szLong.y.ToString("F1") + "｜🧨 自适应之后不重算 `_h` ⇒ 面板停在「缩之前」的高（≈380）⇒ 本红");
        Check(szLong.y + 0.5f >= Mathf.Min(bodyHLong + Tooltip.PadY * 2f, Tooltip.MaxH) - 0.5f,
              "B4b 面板高盖得住正文高（缩过之后仍成立；被 `MaxH` 夹住那一档同样算对）｜面板 " + szLong.y.ToString("F1")
            + " ≥ min(正文 " + bodyHLong.ToString("F1") + " + " + (Tooltip.PadY * 2f).ToString("F1")
            + ", " + Tooltip.MaxH.ToString("F0") + ")");

        // ---- 溢出方向：正文标签中心 = 面板中心（原版 Middle + 对称 pad ⇒ 对称溢出，⛔ 不是向下）----
        Vector3 bodyPos = ParseVec3(dumpLong, " size=(");
        Vector3 panelPos = ParseVec3(dumpLong, " bgPos=(");
        Check(!float.IsNaN(bodyPos.y) && !float.IsNaN(panelPos.y),
              "B5a（前提）`DebugDump` 里读得到正文标签位置与面板位置｜正文 " + bodyPos + " · 面板 " + panelPos);
        Near(bodyPos.y, panelPos.y, 1.0f / unitPx,
             "B5 ★ 超长（= 溢出那一档）时**正文标签仍在面板中心**（原版 `m_VerticalAlignment: 512` = Middle，"
           + "`:489`；容器被上下各 10 的 `VerticalLayoutGroup` 摆成居中，`:228-232`）"
           + "⇒ 溢出**上下对称**，⛔ **不是**「向下溢出」（判据推导 → `报告/B142_E25.md` §四·2）"
           + "｜🧨 把正文改成顶锚（向下溢出）⇒ 本红");

        // ---- 极端长：地板 10 px 仍超高 ⇒ 面板夹 220、溢出不裁 ----
        string huge = new string('试', 480);              // 30 行；10 px 下 ≈ 390 px 仍 > 200
        Tooltip.Show(huge, Vector3.zero, 0);
        Tooltip.FinishFade();
        Vector2 szHuge = Tooltip.PanelSizePx;
        float emHuge = ParseAfter(Tooltip.DebugDump(), " em=");
        Check(emHuge >= Tooltip.BodyMinPx - 0.6f,
              "B6 ★ 极端长文案的 em **没有跌破 10 px**（原版字号地板）｜实得 " + emHuge.ToString("F2") + " px");
        Check(szHuge.y >= Tooltip.MinH - 0.5f && szHuge.y <= Tooltip.MaxH + 0.5f,
              "B6b ★ 极端长文案的面板高仍被夹在 **[110, 220]**｜实得 " + szHuge.y.ToString("F1")
            + "｜🧨 去掉 `_h` 的 Clamp ⇒ 面板涨到 400+ ⇒ 本红");
        Check(szHuge.x >= Tooltip.MinW - 0.5f && szHuge.x <= Tooltip.MaxW + 0.5f,
              "B6c 面板宽（回归）仍在 [500, 640]｜实得 " + szHuge.x.ToString("F1")
            + "｜（自适应只改字号/高度，不该动折行宽）");

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

    /// <summary>从 `DebugDump()` 里取一个 `Vector3`（形如 `(0.7, -0.3, 0.0)`；取不到 ⇒ 全 `NaN` ⇒ 断言红）。</summary>
    static Vector3 ParseVec3(string s, string key)
    {
        var nan = new Vector3(float.NaN, float.NaN, float.NaN);
        if (string.IsNullOrEmpty(s)) return nan;
        int i = s.IndexOf(key, System.StringComparison.Ordinal);
        if (i < 0) return nan;
        i += key.Length;
        int close = s.IndexOf(')', i);
        if (close < 0) return nan;
        var parts = s.Substring(i, close - i).Split(',');
        if (parts.Length != 3) return nan;
        float x, y, z;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        if (!float.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, inv, out x)) return nan;
        if (!float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, inv, out y)) return nan;
        if (!float.TryParse(parts[2].Trim(), System.Globalization.NumberStyles.Float, inv, out z)) return nan;
        return new Vector3(x, y, z);
    }
}
