// B110_W2_Assert.cs — 执行代理 `B110_W2`（2026-10-31）· `B-22`：`ImageQuad.Tint` 的读写顺序缺陷
//   判据 = `资料/完全复刻/判据/B110_R2_小账时效复核.md` §八⑦ + 「顺手发现」⑥
//   先例（本会话同族第 2 处） = `Shell/TitleTab.cs:568`
//
// ============================ 本文件断什么（三层）============================
//   ① **机理（运行期真建一颗 quad）**：`ImageQuad.SetMaterial` 只搬贴图/队列、**不搬材质色**
//      ⇒「先 tint、后换材质」必被新材质的默认白冲掉；正确顺序（存 tint → 换材质 → 写回）保得住。
//      其中「旧顺序必丢色」那条就是**灭自证**：把顺序改回去 ⇒ 上面那条红。
//   ② `Battle/UnitChatPanel.cs`：波形换材质是「存（`var keep = b.wave.Tint;`）→ 换 → 写回」的先后序，
//      且全文件仍只有那两处 `b.wave.SetTint(`（⛔ 没有给 uGUI / 世界两档各写一份）。
//   ③ `Shell/MissionsTab.cs`：`GreyMilestone` 里 `SetMaterial` 必须在 `SetTint` **之前**；
//      ⛔ 没有改成 `keep = q.Tint`（那里要的是**灰**，保留换之前的白会把这格画回原色）。
//
// ============================ 怎么接线（宿主是白名单外）============================
//   本文件**没有**自己往断言宿主里接。宿主里加两行即可：
//
//       B110_W2_Assert.Run();
//       pass += B110_W2_Assert.Pass; fail += B110_W2_Assert.Fail;
//
//   ⚠️ 它只读源码文本 + 在内存里建一颗**临时** quad（`finally` 里 `DestroyImmediate` 清干净），
//     不建窗、不动场景、不跑播放 ⇒ 挂到哪个宿主都安全。
using System.IO;
using UnityEngine;

/// <summary>`B110_W2`：`ImageQuad` tint / 换材质的顺序（`UnitChatPanel` 波形 + `MissionsTab` 里程碑灰）。</summary>
public static class B110_W2_Assert
{
    public static int Pass;
    public static int Fail;

    /// <summary>探针色 —— 原版 `wave` 那格 `m_Color`（`MonoBehaviour_4509.json`），**非白**才能暴露「被冲成白」。</summary>
    static readonly Color Probe = new Color(0.6988f, 0.9811f, 0.9265f, 1f);

    public static void Run()
    {
        Pass = 0; Fail = 0;
        Debug.Log("[B110_W2] ===== B-22 `ImageQuad` tint 顺序（UnitChatPanel 波形 / MissionsTab 里程碑灰）=====");
        CheckMechanism();
        CheckUnitChat();
        CheckMissionsTab();
        Debug.Log("[B110_W2] ===== 合计 pass=" + Pass + " fail=" + Fail + " =====");
    }

    // ------------------------------------------------------------------ ① 机理（运行期）

    static void CheckMechanism()
    {
        Debug.Log("[B110_W2] ---- ① 机理（运行期，临时 quad）----");

        var host = new GameObject("B110_W2_probe");
        Texture2D tex = null;
        Material m1 = null, m2 = null;
        try
        {
            tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            tex.Apply();
            var q = CardPresentation.ImageQuad.Create(host.transform, tex, Vector3.zero, 1f,
                                                      new Vector2(0.5f, 0.5f), "probe");
            Check("（前提）`ImageQuad.Create` 建出了一颗 quad", q != null);
            if (q == null) return;

            var sh = Shader.Find("Sprites/Default");
            Check("（前提）`Sprites/Default` shader 取得到（`ImageQuad.Create` 的默认材质）", sh != null);
            if (sh == null) return;

            // 前提：`SetTint` 写的是 `sharedMaterial.color`（不是顶点色）—— 这正是「换材质会冲掉它」的根因。
            q.SetTint(Probe);
            Check("前提：`SetTint` 之后 `Tint` 读回 = 传入值（" + Fmt(Probe) + " · 实测 " + Fmt(q.Tint) + "）",
                  Same(q.Tint, Probe));

            // —— 本仓正确写法（`Shell/TitleTab.cs:568`）：存 tint → 换材质 → 写回
            m1 = new Material(sh);
            var keep = q.Tint;
            q.SetMaterial(m1);
            q.SetTint(keep);
            Check("正确顺序「存 → 换材质 → 写回」之后 `Tint` 仍是 " + Fmt(Probe) + "（实测 " + Fmt(q.Tint) + "）",
                  Same(q.Tint, Probe));

            // —— 🔴 灭自证：同一颗 quad 把顺序改回旧写法（先 tint、后换材质）⇒ 必被新材质的默认白冲掉
            m2 = new Material(sh);
            q.SetTint(Probe);        // 旧写法第一句
            q.SetMaterial(m2);       // 旧写法第二句（`SetMaterial` 不搬材质色）
            Check("灭自证：旧顺序（先 tint、后换材质）`Tint` **一定不再**等于 " + Fmt(Probe)
                  + "（实测 " + Fmt(q.Tint) + "）—— 这条绿 = 上面两条确实咬得住顺序",
                  !Same(q.Tint, Probe));
        }
        finally
        {
            // 先销毁宿主（连带 quad / 渲染器），再销毁临时材质与贴图。
            UnityEngine.Object.DestroyImmediate(host);
            if (m1 != null) UnityEngine.Object.DestroyImmediate(m1);
            if (m2 != null) UnityEngine.Object.DestroyImmediate(m2);
            if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
        }
    }

    // ------------------------------------------------------------------ ② UnitChatPanel

    static void CheckUnitChat()
    {
        Debug.Log("[B110_W2] ---- ② `Battle/UnitChatPanel.cs` ----");
        var t = Text("Battle/UnitChatPanel.cs");
        Check("`Battle/UnitChatPanel.cs` 文件在", t != null);
        if (t == null) return;

        int iKeep    = t.IndexOf("var keep = b.wave.Tint;", System.StringComparison.Ordinal);
        int iMat     = t.IndexOf("b.wave.SetMaterial(b.waveMat);", System.StringComparison.Ordinal);
        int iRestore = t.IndexOf("b.wave.SetTint(keep);", System.StringComparison.Ordinal);
        Check("波形换材质是「存 tint（@" + iKeep + "）→ 换材质（@" + iMat + "）→ 写回（@" + iRestore + "）」的先后序",
              iKeep >= 0 && iMat > iKeep && iRestore > iMat);
        Check("写回那句**不在行末注释里**（`b.wave.SetTint(keep);`）", BeforeLineComment(t, "b.wave.SetTint(keep);"));

        // 换材质失败 / 世界档退回旧材质时，前面那句 `SetTint(WaveTint)` 仍要兜底
        Check("`b.wave.SetTint(WaveTint);` 仍在（材质建失败时旧材质也是对的颜色）",
              t.Contains("b.wave.SetTint(WaveTint);"));

        // ⛔ 两档（uGUI / 世界）共用同一段：不许为其中一档再写一份 tint 逻辑
        Check("全文件只有两处 `b.wave.SetTint(`（= `WaveTint` 一处 + 写回一处，实测 "
              + CountOf(t, "b.wave.SetTint(") + "）",
              CountOf(t, "b.wave.SetTint(") == 2);
    }

    // ------------------------------------------------------------------ ③ MissionsTab

    static void CheckMissionsTab()
    {
        Debug.Log("[B110_W2] ---- ③ `Shell/MissionsTab.cs` ----");
        var t = Text("Shell/MissionsTab.cs");
        Check("`Shell/MissionsTab.cs` 文件在", t != null);
        if (t == null) return;

        int iMat  = t.IndexOf("q.SetMaterial(m);", System.StringComparison.Ordinal);
        int iTint = t.IndexOf("q.SetTint(MilestoneDisabledTint);", System.StringComparison.Ordinal);
        Check("`GreyMilestone`：`SetMaterial`（@" + iMat + "）在 `SetTint(MilestoneDisabledTint)`（@" + iTint + "）**之前**",
              iMat >= 0 && iTint > iMat);
        Check("`q.SetTint(MilestoneDisabledTint);` **不在行末注释里**",
              BeforeLineComment(t, "q.SetTint(MilestoneDisabledTint);"));

        // ⛔ 灭自证的反向保险：这里**不许**照 `TitleTab` 写成 `keep = q.Tint`
        //   （那颗 quad 换之前是白的，`keep` 会把刚设好的灰写回白 ⇒ 这格不再灰）。
        Check("⛔ 没有改成 `keep = q.Tint`（会把这格画回原色，而不是灰）", !t.Contains("var keep = q.Tint;"));
        Check("灰仍是原版 `m_DisabledColor` 的 0.7843137（⛔ 没被换成别的灰）",
              t.Contains("new Color(0.7843137f, 0.7843137f, 0.7843137f, 1f)"));
    }

    // ------------------------------------------------------------------ 工具

    static void Check(string what, bool ok)
    {
        if (ok) { Pass++; Debug.Log("[B110_W2] ✓ " + what); }
        else    { Fail++; Debug.LogError("[B110_W2] ✗ " + what); }
    }

    static bool Same(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 1e-4f && Mathf.Abs(a.g - b.g) < 1e-4f
            && Mathf.Abs(a.b - b.b) < 1e-4f && Mathf.Abs(a.a - b.a) < 1e-4f;
    }

    static string Fmt(Color c)
    {
        return "(" + c.r.ToString("R") + ", " + c.g.ToString("R") + ", " + c.b.ToString("R") + ", " + c.a.ToString("R") + ")";
    }

    static int CountOf(string s, string token)
    {
        if (s == null || token == null || token.Length == 0) return 0;
        int n = 0, i = 0;
        while ((i = s.IndexOf(token, i, System.StringComparison.Ordinal)) >= 0) { n++; i += token.Length; }
        return n;
    }

    static string Text(string assetRel)
    {
        var p = "Assets/CardPresentation/" + assetRel;
        return File.Exists(p) ? File.ReadAllText(p) : null;
    }

    /// <summary>在整份文本里找 `token` 出现的那一行，判它的**第一个 `//`** 是不是在 `token` 之后。
    /// 🔴 2026-10-30 的坑：追加代码落在**行末 `//` 之后** ⇒ 看着接上了、跑起来一行不执行。</summary>
    static bool BeforeLineComment(string text, string token)
    {
        if (text == null) return false;
        int idx = text.IndexOf(token, System.StringComparison.Ordinal);
        if (idx < 0) return false;
        int lineStart = text.LastIndexOf('\n', idx) + 1;
        int comment = text.IndexOf("//", lineStart, System.StringComparison.Ordinal);
        return comment < 0 || comment > idx;
    }
}
