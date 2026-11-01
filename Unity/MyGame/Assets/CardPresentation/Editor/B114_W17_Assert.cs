// B114_W17_Assert.cs — `B-247`：边框高亮那两件在原版是**九宫格**（`Image.m_Type = 1`），我们过去画成「整张拉伸」。
// （写手 `W-17` · 2026-11-01 · **没跑 Unity**，只写断言；宿主由调度台接线）
//
// ============================ 怎么接（⚠️ 只由调度台改宿主）============================
//  唯一入口 = `B114_W17_Assert.Run()`；宿主 `Editor/BattleScene.cs` 第 **`46896`** 行那条聚合行
//  **同一物理行、第一个 `//` 之前**追加（⚠️ 该行用的是局部变量 **`pass` / `fail`**，不是 `_sink`：
//      B114_W17_Assert.Run(); pass += B114_W17_Assert.Pass; fail += B114_W17_Assert.Fail;
//  ✅ 2026-11-01 `B114_W26` 已接（插入区间 `[4750,4916)`，`B114_W14_Assert.Fail;` 之后）。
//
// 判据（全部现读 `d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/MainMenu Warpforge.unity`）：
//   · Image **2545**（GameObject 338 `Level Up Effect`）：`m_Type 1` · `m_PixelsPerUnitMultiplier 0.5`
//   · Image **2057**（GameObject 404 `Level Up Effect Down`）：`m_Type 1` · `m_PixelsPerUnitMultiplier 1.11`
//   · 两张 sprite（`OctagonUI Filled/Border SDF`）：`m_Border (52,52,52,52)` · `m_Rect 128×128` · `m_PixelsToUnits 100`
//   · 同场景 CanvasScaler：`m_ReferencePixelsPerUnit 100` ⇒ `Image.pixelsPerUnit = 100/100 = 1`
//     ⇒ `multipliedPixelsPerUnit = 1 × ppuMul` ⇒ **边框(矩形单位) = 52 ÷ ppuMul**
//   · 两颗 Image 的 RectTransform（`&1126` / `&1422`，其中 `&1422` 的尺寸由 stretch 算出）：
//       `Level Up Effect`      rect = 616.10315 × 837.97217，`m_LocalScale 0.004545454`
//       `Level Up Effect Down` rect = 616.10315−110.032 × 837.97217−121.037 = 506.07115 × 716.93517，继承同一 0.004545454
//     ⇒ 矩形单位 → 本层局部单位 × 0.004545454（= 1/220）
//
// 期望值**全部由上面这些原版字段算出**（⛔ 不读 `ImageQuad.NineSliceBorderWorld` 之类由我们实现写下的量 —— 那是自证）。
//
// 灭自证（两条）：
//   ① 两个 ppuMul 不同 ⇒ 两边框必须**不同**（0.4727272 vs 0.2129402）。把 `BorderNineSlice` 写死成同一个数会红。
//   ② `A9` 先把九宫格关掉再数顶点：**同一个对象**上必须是 4 顶点 ⇒ 证明 A2/A4 量的确实是九宫格，不是别的东西恰好对上。
//
// 怎么跑（主对话 · 串行 · 要跑 Unity）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B114_W17_Assert.Run -logFile -
using System.IO;
using UnityEngine;
using CardPresentation;

public static class B114_W17_Assert
{
    const string P = "[B114_W17] ";
    const string FxCpPath   = "CardPresentation/Shell/CardDisplayEffects.cs";
    const string QuadPath   = "CardPresentation/Battle/ImageQuad.cs";

    static int _ok, _bad;

    /// <summary>给宿主聚合行用的读口（否则失败不会让宿主变红 = 静默失败）。</summary>
    public static int Pass { get { return _ok; } }
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(P + "✓ " + what); }
        else { _bad++; Debug.LogError(P + "✗ " + what); }
    }

    static bool Near(float a, float b, float tol = 1e-4f) { return Mathf.Abs(a - b) <= tol; }

    // ── 原版字段（**只写原版数**，见文件头）────────────────────────────────────────
    const float SpriteBorderPx = 52f;      // 两张 sprite 的 `m_Border` 四边
    const float SpriteRectPx   = 128f;     // `m_Rect.width/height`
    const float LocalScale     = 0.004545454f;   // 两颗 Image 的 RectTransform `m_LocalScale.x`（= 1/220）
    const float RectWL         = 616.10315f, RectHL = 837.97217f;    // `Level Up Effect` 矩形（矩形单位）
    const float RectWD         = 506.07115f, RectHD = 716.93517f;    // `Level Up Effect Down` 矩形
    const float PpuMulL        = 0.5f;     // Image 2545
    const float PpuMulD        = 1.11f;    // Image 2057

    /// <summary>一个九宫格块的 rect + uv（uv 用 0..1）。</summary>
    struct Block { public Rect Rect; public Rect Uv; }

    /// <summary>把网格按「每 4 个顶点一块」（`ImageQuad.RebuildNineSliceMesh` / uGUI `AddQuad` 的顶点分组）拆回来。</summary>
    static Block[] SplitBlocks(Mesh m)
    {
        int n = m.vertexCount / 4;
        var verts = m.vertices; var uvs = m.uv;
        var outp = new Block[n];
        for (int b = 0; b < n; b++)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            float u0 = float.MaxValue, v0 = float.MaxValue, u1 = float.MinValue, v1 = float.MinValue;
            for (int k = 0; k < 4; k++)
            {
                var v = verts[b * 4 + k]; var t = uvs[b * 4 + k];
                x0 = Mathf.Min(x0, v.x); y0 = Mathf.Min(y0, v.y);
                x1 = Mathf.Max(x1, v.x); y1 = Mathf.Max(y1, v.y);
                u0 = Mathf.Min(u0, t.x); v0 = Mathf.Min(v0, t.y);
                u1 = Mathf.Max(u1, t.x); v1 = Mathf.Max(v1, t.y);
            }
            outp[b] = new Block { Rect = new Rect(x0, y0, x1 - x0, y1 - y0), Uv = new Rect(u0, v0, u1 - u0, v1 - v0) };
        }
        return outp;
    }

    /// <summary>对一件做全套几何断言。<paramref name="ppuMul"/> / <paramref name="rectW/H"/> 来自原版 prefab。</summary>
    static void CheckOne(Transform layerRoot, string nodeName, float ppuMul,
                         float rectW, float rectH, string tag)
    {
        var node = FindExact(layerRoot, nodeName);
        Check(node != null, tag + " · 前提：光效层里有 `" + nodeName + "` 节点");
        if (node == null) return;

        var quad = node.Find(nodeName + " Gfx");
        Check(quad != null, tag + " · 前提：`" + nodeName + " Gfx` 载体在（判据 = `CardDisplayEffects.BuildImage`）");
        if (quad == null) return;
        var iq = quad.GetComponent<ImageQuad>();
        var mf = quad.GetComponent<MeshFilter>();
        Check(iq != null && mf != null, tag + " · 前提：载体上有 `ImageQuad` + `MeshFilter`");
        if (iq == null || mf == null) return;

        // ── 期望值：**只由原版字段算**（⛔ 不读我们的实现写下的量）──────────────
        float expW  = rectW * LocalScale;              // 616.10315 × 1/220 = 2.8004689
        float expH  = rectH * LocalScale;              // 837.97217 × 1/220 = 3.8089640
        float expB  = SpriteBorderPx / ppuMul * LocalScale;   // 52 ÷ ppuMul × 1/220
        float expCW = expW - 2f * expB;
        float expCH = expH - 2f * expB;
        float expUv = SpriteBorderPx / SpriteRectPx;   // 0.40625

        Check(iq.IsNineSlice, tag + " · A1 `IsNineSlice` 为真（原版 `m_Type = 1`，⛔ 不是整张拉伸）");

        var m = mf.sharedMesh;
        Check(m != null, tag + " · 前提：共享网格在");
        if (m == null) return;

        Check(m.vertexCount == 36 && m.triangles.Length == 54,
              tag + " · A2 九块 × 4 顶点 = 36、三角 = 54（实际 " + m.vertexCount + " / " + m.triangles.Length + "）");

        var bs = SplitBlocks(m);
        Check(bs.Length == 9, tag + " · A2b 拆回来正好 9 块（实际 " + bs.Length + "）");
        if (bs.Length != 9) return;

        // 块序 = xi(0..2) 外层、yi(0..2) 内层 ⇒ idx = xi*3 + yi
        var bl = bs[0]; var br = bs[2]; var tl = bs[6]; var tr = bs[8];
        var cen = bs[4];

        // A3 整体尺寸 = 原版矩形 × 1/220
        float totW = (tr.Rect.xMax - bl.Rect.xMin), totH = (tr.Rect.yMax - bl.Rect.yMin);
        Check(Near(totW, expW) && Near(totH, expH),
              tag + " · A3 整体 = 原版矩形×1/220 = " + expW.ToString("F7") + " × " + expH.ToString("F7")
              + "（实际 " + totW.ToString("F7") + " × " + totH.ToString("F7") + "）");

        // A4 四个角块都是 expB × expB（**四边同值**，不是按比例分配）
        Check(Near(bl.Rect.width, expB) && Near(bl.Rect.height, expB)
           && Near(br.Rect.width, expB) && Near(br.Rect.height, expB)
           && Near(tl.Rect.width, expB) && Near(tl.Rect.height, expB)
           && Near(tr.Rect.width, expB) && Near(tr.Rect.height, expB),
              tag + " · A4 四角块 = `52 ÷ " + ppuMul + " × 1/220` = " + expB.ToString("F7") + " 见方"
              + "（实际 BL " + bl.Rect.width.ToString("F7") + "×" + bl.Rect.height.ToString("F7") + "）");

        // A5 中段 = 总尺寸 − 2×边框（边块拉伸、角块不拉伸）
        Check(Near(cen.Rect.width, expCW) && Near(cen.Rect.height, expCH),
              tag + " · A5 中心块 = 总 − 2×边框 = " + expCW.ToString("F7") + " × " + expCH.ToString("F7")
              + "（实际 " + cen.Rect.width.ToString("F7") + " × " + cen.Rect.height.ToString("F7") + "）");

        // A6 uv：每块拿 sprite 自己的那一段（判据 = uGUI `Image.GetInnerUV`）
        Check(Near(bl.Uv.xMin, 0f) && Near(bl.Uv.yMin, 0f) && Near(bl.Uv.xMax, expUv) && Near(bl.Uv.yMax, expUv),
              tag + " · A6 左下角块 uv = [0, 52/128]²（实际 " + bl.Uv.xMax.ToString("F5") + "）");
        Check(Near(cen.Uv.xMin, expUv) && Near(cen.Uv.yMin, expUv)
           && Near(cen.Uv.xMax, 1f - expUv) && Near(cen.Uv.yMax, 1f - expUv),
              tag + " · A6b 中心块 uv = [0.40625, 0.59375]²（实际 "
              + cen.Uv.xMin.ToString("F5") + "–" + cen.Uv.xMax.ToString("F5") + "）");
        Check(Near(tr.Uv.xMax, 1f) && Near(tr.Uv.yMax, 1f) && Near(tr.Uv.xMin, 1f - expUv),
              tag + " · A6c 右上角块 uv 贴到 1 边（⛔ 别把整张图压进每一块）");

        // A8 回归闸：升级演出会把这两件当 `FadeImage` → `SetTint` → `UploadVertexTint` → `RebuildMesh()`
        //    ⇒ 没有 `RebuildMesh` 第一句那个 dispatch，九宫格会在动画第一帧被静默换回单 quad。
        iq.SetTint(new Color(1f, 1f, 1f, 0.5f));
        var m2 = mf.sharedMesh;
        Check(m2 != null && m2.vertexCount == 36,
              tag + " · A8 `SetTint` 之后仍是 36 顶点（= `RebuildMesh` 的九宫格 dispatch 真的接上了）"
              + "（实际 " + (m2 != null ? m2.vertexCount : -1) + "）");
    }

    static Transform FindExact(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var r = FindExact(root.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }

    static string ReadSrc(string rel)
    {
        try
        {
            var p = Path.Combine(Application.dataPath, rel);
            return File.Exists(p) ? File.ReadAllText(p) : null;
        }
        catch { return null; }
    }

    static int Count(string s, string sub)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(sub)) return 0;
        int n = 0, i = 0;
        while ((i = s.IndexOf(sub, i, System.StringComparison.Ordinal)) >= 0) { n++; i += sub.Length; }
        return n;
    }

    public static void Run()
    {
        _ok = _bad = 0;
        GameObject host = null;
        try
        {
            // ================================================================ A · 运行时
            // 🔴 走**生产路径** `BuildUnder2DCard`（`s = 1`）：只有这一档的尺寸才等于「原版矩形 × 1/220」。
            //    ⛔ 别用旧的 `Build(cardParent, cardWidthPx)`（`CardsS = 100` 是定位框、不是卡宽，那一档的 `s` 是近似值）。
            host = new GameObject("B114_W17_Host");
            var twoDCard = new GameObject("2DCard").transform;
            twoDCard.SetParent(host.transform, false);

            var layer = CardDisplayEffects.BuildUnder2DCard(twoDCard);
            Check(layer != null, "A · 前提：`CardDisplayEffects.BuildUnder2DCard` 建得出光效层（判据 json + 材质齐）");
            if (layer != null)
            {
                CheckOne(layer, "Level Up Effect",      PpuMulL, RectWL, RectHL, "LU");
                CheckOne(layer, "Level Up Effect Down", PpuMulD, RectWD, RectHD, "DN");

                // A7 灭自证①：两个 ppuMul 不同 ⇒ 两个边框必须**不同**（写死成同一个数会红）
                var a = FindExact(layer, "Level Up Effect");
                var b = FindExact(layer, "Level Up Effect Down");
                var qa = a != null ? a.Find("Level Up Effect Gfx") : null;
                var qb = b != null ? b.Find("Level Up Effect Down Gfx") : null;
                // ⚠️ 前提要**显式断**（`if (x != null) Check(...)` 会静默跳过 = 假的绿，见 CLAUDE.md §三）
                Check(qa != null && qb != null, "A7 前提：两件载体都找得到（`… Gfx`）");
                float ba = (qa != null && qa.GetComponent<ImageQuad>() != null)
                         ? qa.GetComponent<ImageQuad>().NineSliceBorderWorld.x : -1f;
                float bb = (qb != null && qb.GetComponent<ImageQuad>() != null)
                         ? qb.GetComponent<ImageQuad>().NineSliceBorderWorld.x : -1f;
                Check(ba > 0f && bb > 0f && !Near(ba, bb, 1e-3f),
                      "A7 两件边框**不同**（0.5 vs 1.11 两个 ppuMul）—— 实际 " + ba.ToString("F6") + " / " + bb.ToString("F6")
                      + "｜🧨 把 `BorderNineSlice` 写死成同一个数 ⇒ 红");

                // A9 灭自证②：同一个对象上**关掉**九宫格 ⇒ 顶点数必须回到 4
                if (qa != null)
                {
                    var iqa = qa.GetComponent<ImageQuad>();
                    var mfa = qa.GetComponent<MeshFilter>();
                    iqa.ClearNineSlice();
                    Check(mfa != null && mfa.sharedMesh != null && mfa.sharedMesh.vertexCount == 4,
                          "A9 关掉九宫格后回到 4 顶点（证明 A2/A4 量的确实是九宫格，不是别的东西恰好对上）"
                          + "（实际 " + (mfa != null && mfa.sharedMesh != null ? mfa.sharedMesh.vertexCount : -1) + "）");
                }
            }

            // ================================================================ B · 源码级
            var fx = ReadSrc(FxCpPath);
            var qd = ReadSrc(QuadPath);
            Check(fx != null, "B · 前提：读得到 `" + FxCpPath + "`");
            Check(qd != null, "B · 前提：读得到 `" + QuadPath + "`");

            if (fx != null)
            {
                Check(Count(fx, "quad.SetNineSlice(nbW, nbUv, true)") == 1,
                      "B1 · `CardDisplayEffects.BuildImage` 里对这两件调了 `SetNineSlice` 恰好一次"
                      + "（实际 " + Count(fx, "quad.SetNineSlice(nbW, nbUv, true)") + "）");
                Check(Count(fx, "static bool BorderNineSlice(") == 1,
                      "B1b · 边框表的唯一实现 `BorderNineSlice` 在（判据数字只此一处）");
            }
            if (qd != null)
            {
                // 顺序很重要：`SetNineSlice` 必须排在 `SetVertexTint` **之后**（后者会 RebuildMesh）
                int ivt = fx != null ? fx.IndexOf("quad.SetVertexTint(tint)", System.StringComparison.Ordinal) : -1;
                int ins = fx != null ? fx.IndexOf("quad.SetNineSlice(nbW, nbUv, true)", System.StringComparison.Ordinal) : -1;
                Check(ivt >= 0 && ins > ivt,
                      "B2 · `SetNineSlice` 排在 `SetVertexTint` **之后**（后者会 `RebuildMesh()` ⇒ 九宫格要最后落地）"
                      + "（" + ivt + " / " + ins + "）");
                Check(Count(qd, "if (_nineOn) { RebuildNineSliceMesh(); return; }") == 1,
                      "B3 · `RebuildMesh` 第一句的九宫格 dispatch 在（⛔ 删了它 ⇒ `SetTint` 会把九宫格静默换回单 quad）");
                Check(Count(qd, "void RebuildNineSliceMesh()") == 1,
                      "B3b · `RebuildNineSliceMesh` 的唯一实现");
            }
        }
        catch (System.Exception e)
        {
            _bad++;
            Debug.LogError(P + "✗ 异常：" + e);
        }
        finally
        {
            if (host != null) Object.DestroyImmediate(host);
        }

        Debug.Log(P + "合计 " + _ok + " 通过 / " + _bad + " 失败");
    }
}
