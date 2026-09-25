// ChatBoxProbe.cs — 定点探针：**台词框**那套（限宽折行 + 字号自适应）到底算成了什么
//
// 为什么要它：这条链上出错时，全量自检一轮要 6 分钟，而这里只需要建一个 Label。
// 2026-09-25 的现场：台词被压到 **17.3 × 0.8 px**（字号 px 0.5，原版该是 33），
// 而 `Label` 的折行/自适应在 Shell 那边一直是好用的 ⇒ 要一次把中间量全打出来。
//
// 入口：`-executeMethod ChatBoxProbe.Run`
// ⚠️ **类必须在全局命名空间**（`-executeMethod` 按名字找，带 namespace 会「could not be found」）
using CardPresentation;
using UnityEngine;

public static class ChatBoxProbe
{
    const string P = "CHATPROBE ";

    public static void Run()
    {
        var cam = new GameObject("cam").AddComponent<Camera>();
        cam.orthographic = true;
        var root = new GameObject("hud").transform;

        // 原版 `ChatText` 那个框（出处见 `UnitChatPanel.TextW/TextH`）
        const float TextW = 440.9f, TextH = 81.7f, OrigFontPx = 33f, MinPx = 10f;
        // 实跑里挑出来的**最长那句**：66 字（跑一次自检就会在日志里印出字数）
        const string Long = "The Emperor protects those who stand firm against the darkness of the warp";

        Debug.Log($"{P}TMP 可用 = {TmpFont.Available} · Wglyph={TmpFont.WorldGlyphPerFontSize:R}"
                + $" · Wcap={TmpFont.WorldCapPerFontSize:R}");

        var lb = Label.Create(root, "", Vector3.zero, 4, Color.white, new Vector2(0f, 0.5f), "probe");
        if (lb == null) { Debug.LogError(P + "建不出 Label"); return; }

        // **连说五句**：那个「每说一句再缩一档」的棘轮只有这样才看得出来
        for (int i = 1; i <= 5; i++)
        {
            lb.SetText(Long);
            lb.SetGlyphHeight(OrigFontPx / 108f);
            lb.SetAlignLeft();
            float before = lb.FontSize;          // 开自适应**之前**的 live fontSize（看棘轮从哪一步开始）
            lb.SetAutoFitBox(TextW / 108f, TextH / 108f, MinPx, OrigFontPx);

            float w = lb.WorldW * 108f, h = lb.WorldH * 108f;
            Debug.Log($"{P}第 {i} 句：wrap={lb.Wrapping} 行={lb.LineCount}"
                    + $" 渲染={w:F1}×{h:F1}px 字号px={lb.FontPxNow:F2}"
                    + $" 开自适应前={before * TmpFont.WorldGlyphPerFontSize * 108f:F2}px"
                    + $" live={lb.FontSize:R} 目标={TmpFont.FontSizeForGlyphHeight(OrigFontPx / 108f):R}"
                    + $" rect={RectOf(lb)} | {lb.DumpSizes()}");
        }

        // 只做一次自适应（对照：分辨「棘轮」与「第一次就缩」）
        var lb2 = Label.Create(root, "", Vector3.zero, 4, Color.white, new Vector2(0f, 0.5f), "probe2");
        lb2.SetText(Long);
        lb2.SetGlyphHeight(OrigFontPx / 108f);
        lb2.SetAlignLeft();
        lb2.SetAutoFitBox(TextW / 108f, TextH / 108f, MinPx, OrigFontPx);
        Debug.Log($"{P}只一次：行={lb2.LineCount} 渲染={lb2.WorldW * 108f:F1}×{lb2.WorldH * 108f:F1}px"
                + $" 字号px={lb2.FontPxNow:F2} live={lb2.FontSize:R} rect={RectOf(lb2)}");

        // 对照：**不开自适应**，只折行 —— 应当正好 33px、折成 2~3 行
        var lb3 = Label.Create(root, "", Vector3.zero, 4, Color.white, new Vector2(0f, 0.5f), "probe3");
        lb3.SetText(Long);
        lb3.SetGlyphHeight(OrigFontPx / 108f);
        lb3.SetAlignLeft();
        lb3.SetWrapWidth(TextW / 108f);
        lb3.SetText("");                     // 逼一次重排，让 `RefreshBounds` 按**新框宽**重新量（不然读到的是旧值）
        lb3.SetText(Long);
        Debug.Log($"{P}只折行：行={lb3.LineCount} 渲染={lb3.WorldW * 108f:F1}×{lb3.WorldH * 108f:F1}px"
                + $" 字号px={lb3.FontPxNow:F2} rect={RectOf(lb3)}");

        Object.DestroyImmediate(cam.gameObject);
        Object.DestroyImmediate(root.gameObject);
    }

    static string RectOf(Label lb)
    {
        var rt = lb.GetComponentInChildren<RectTransform>();
        return rt != null ? $"{rt.rect.width:F3}×{rt.rect.height:F3}" : "<无>";
    }
}
