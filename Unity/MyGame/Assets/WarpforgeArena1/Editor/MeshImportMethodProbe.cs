// MeshImportMethodProbe.cs —— **流式网格**（顶点数据在 bundle 的 `.resS` 里）怎么导才不会变成垃圾
//
// 症状（2026-10-01 晚实测）：`EffectExporter.ImportMesh` 走的是
//   `Object.Instantiate(bundleMesh)` → `AssetDatabase.CreateAsset`
// 而**顶点数据走流式**的那 3 个网格导出来是**未初始化内存**：
//
//   | 资产 | 顶点 | stride | 顶点最小/最大 |
//   |---|---|---|---|
//   | `Spirt Stone 1.asset` | 748 | 20 | **±1.3e38 / inf**（垃圾） |
//   | `Card_Remnant_HO Optimization 2.asset` | 6541 | 40 | **inf** |
//   | `Card 3D Subdivided.asset` | 1641 | 40 | **inf** |
//
// 其余 **199 个网格资产全部正常**（顶点落在 ±1 量级）—— **区别就在「原件是不是流式」**：
// 这 3 个在 bundle 里的 `m_VertexData.m_DataSize == 0`、数据挂在 `m_StreamData` 上。
//
// 后果是**看得见的**：`RemnantBody3D Aeldari` 那 16×（台账 E 组）追到底就是这颗石头 ——
// 它渲成一团铺满画面的白色，把整条效果的亮度顶上去 145×。
// 判据链 → `资料/特效还原_进度与交接.md` §〇之四 + `资料/Animator动画_导入路与判据.md`。
//
// 本探针把**几种导法**并排量一遍，看哪种能把顶点数据真带过去。
// **判据 = 落盘之后从 `.asset` 的 YAML 里把 `_typelessdata` 读回来、算顶点极值**
// （工程是 Force Text 序列化）—— 不看「有没有抛异常」，只看**值对不对**。
//
// 用法：
//   unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod MeshImportMethodProbe.Run -logFile "d:/4/_tmp_view/meshmethod.log"
// 筛输出：grep "^MM "
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MeshImportMethodProbe
{
    // `GraphicsBuffer.GetData<T>` 要求元素宽度与 stride 一致 ⇒ 按 stride 备几个等宽 blittable 结构。
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    struct V20 { public Vector3 pos; public ulong nrm; }
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    struct V40 { public Vector3 pos; public ulong a; public ulong b; public ulong c; }

    const string Tag = "MM ";
    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string VfxBundleName = "battleprefabs_vfxandmisc_assets_all.bundle";
    const string PrefabName = "RemnantBody3D Aeldari";
    const string GoName = "Spirit Stone Appear";
    const string TmpDir = "Assets/WarpforgeVFX/Meshes/_probe";
    const string TmpDirParent = "Assets/WarpforgeVFX/Meshes";

    /// <summary>把落盘的 `.asset`（Force Text ⇒ YAML）里的顶点读回来，算 min/max。
    /// 返回 null 表示读不出来（文件没落盘 / 没有 typelessdata）。</summary>
    static (float min, float max, int verts) ReadBackVertexRange(string assetPath)
    {
        var full = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
        if (!File.Exists(full)) return (0, 0, -1);
        var s = File.ReadAllText(full);
        var mv = Regex.Match(s, @"m_VertexCount: (\d+)");
        var md = Regex.Match(s, @"m_DataSize: (\d+)");
        var mt = Regex.Match(s, @"_typelessdata: ([0-9a-fA-F]+)");
        if (!mv.Success || !md.Success || !mt.Success) return (0, 0, mv.Success ? int.Parse(mv.Groups[1].Value) : -1);
        int vc = int.Parse(mv.Groups[1].Value), ds = int.Parse(md.Groups[1].Value);
        if (vc <= 0 || ds <= 0) return (0, 0, vc);
        int stride = ds / vc;
        var hex = mt.Groups[1].Value;
        var raw = new byte[hex.Length / 2];
        for (int i = 0; i < raw.Length; i++)
            raw[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber);
        float mn = float.MaxValue, mx = float.MinValue;
        int n = Mathf.Min(vc, 400);
        for (int i = 0; i < n; i++)
        {
            int b = i * stride;
            if (b + 12 > raw.Length) break;
            for (int k = 0; k < 3; k++)
            {
                float v = BitConverter.ToSingle(raw, b + k * 4);
                if (float.IsNaN(v) || float.IsInfinity(v)) return (float.NaN, float.NaN, vc);
                mn = Mathf.Min(mn, v); mx = Mathf.Max(mx, v);
            }
        }
        return (mn, mx, vc);
    }


    /// <summary>`GraphicsBuffer.GetData` 换个姿势：先按 `uint[]`（1 元素 = 4 字节）试，长度对不上就按元素个数试。</summary>
    static bool TryGetData(GraphicsBuffer buf, uint[] dst)
    {
        try { buf.GetData(dst); return true; }
        catch (Exception e1)
        {
            Debug.Log(Tag + $"   GetData(uint[{dst.Length}]) 抛了：{e1.GetType().Name}: {e1.Message}");
        }
        return false;
    }

    static void Report(string label, string assetPath, Mesh m)
    {
        var r = ReadBackVertexRange(assetPath);
        string verdict;
        if (r.verts < 0) verdict = "🔴 文件里读不到（没落盘？）";
        else if (r.verts == 0) verdict = "🔴 顶点数 = 0";
        else if (float.IsNaN(r.min)) verdict = "🔴 **有 NaN / Inf ⇒ 还是垃圾**";
        else if (Mathf.Max(Mathf.Abs(r.min), Mathf.Abs(r.max)) > 100f) verdict = "🔴 **值越界（>100）⇒ 垃圾**";
        else verdict = "✅ **顶点正常**";
        Debug.Log(Tag + $"{label}：顶点 {(m != null ? m.vertexCount : -1)}"
                + $" · 读回 {r.verts} 个 · 极值 [{r.min:0.###}, {r.max:0.###}] ⇒ {verdict}");
    }

    static Mesh SaveCopy(Mesh src, string name)
    {
        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), TmpDir));
        var path = $"{TmpDir}/{name}.asset";
        AssetDatabase.DeleteAsset(path);
        var copy = UnityEngine.Object.Instantiate(src);
        copy.name = name;
        AssetDatabase.CreateAsset(copy, path);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }

    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder(TmpDirParent))
            AssetDatabase.CreateFolder("Assets/WarpforgeVFX", "Meshes");
        if (!AssetDatabase.IsValidFolder(TmpDir))
            AssetDatabase.CreateFolder(TmpDirParent, "_probe");

        var b = AssetBundle.LoadFromFile(Path.Combine(BundleDir, VfxBundleName));
        if (b == null) { Debug.LogError(Tag + "源包加载不了"); return; }
        GameObject orig = null;
        foreach (var g in b.LoadAllAssets<GameObject>())
            if (g != null && g.name == PrefabName) { orig = g; break; }
        if (orig == null) { Debug.LogError(Tag + $"源包里没有 `{PrefabName}`"); return; }
        var t = orig.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == GoName);
        var mf = t != null ? t.GetComponent<MeshFilter>() : null;
        var src = mf != null ? mf.sharedMesh : null;
        if (src == null) { Debug.LogError(Tag + "原版那件没有 MeshFilter.sharedMesh"); return; }
        Debug.Log(Tag + $"源网格 `{src.name}`：顶点 {src.vertexCount} · subMesh {src.subMeshCount}"
                      + $" · bounds {src.bounds.size.ToString("F3")} · 可读 {src.isReadable}");
        var va = src.GetVertexAttributes();
        Debug.Log(Tag + "  通道：" + string.Join(" · ", va.Select(a => $"{a.attribute} {a.format} x{a.dimension} @{a.stream}")));

        // ---- M1：现行做法（`Instantiate` + `CreateAsset`）----
        Report("M1 `Instantiate`+`CreateAsset`（现行）", $"{TmpDir}/m1.asset", SaveCopy(src, "m1"));

        // ---- M2：先 `UploadMeshData(false)` 再落盘（false = 保留 CPU 可读）----
        {
            var c = UnityEngine.Object.Instantiate(src);
            c.name = "m2";
            try { c.UploadMeshData(false); } catch (Exception e) { Debug.LogWarning(Tag + "M2 UploadMeshData 抛了：" + e.Message); }
            string p = $"{TmpDir}/m2.asset";
            AssetDatabase.DeleteAsset(p);
            AssetDatabase.CreateAsset(c, p);
            AssetDatabase.SaveAssets();
            Report("M2 `UploadMeshData(false)` 后落盘", p, AssetDatabase.LoadAssetAtPath<Mesh>(p));
        }

        // ---- M3：`CopySerialized` 进一个空 Mesh ----
        {
            var c = new Mesh();
            c.name = "m3";
            try { EditorUtility.CopySerialized(src, c); } catch (Exception e) { Debug.LogWarning(Tag + "M3 CopySerialized 抛了：" + e.Message); }
            string p = $"{TmpDir}/m3.asset";
            AssetDatabase.DeleteAsset(p);
            AssetDatabase.CreateAsset(c, p);
            AssetDatabase.SaveAssets();
            Report("M3 `CopySerialized` 进 new Mesh", p, AssetDatabase.LoadAssetAtPath<Mesh>(p));
        }

        // ---- M4：先把该包里**所有** Mesh 加载一遍（逼 Unity 把流式数据读进来）再 `Instantiate` ----
        {
            int n = 0;
            try { n = b.LoadAllAssets<Mesh>().Length; } catch (Exception e) { Debug.LogWarning(Tag + "M4 枚举 Mesh 抛了：" + e.Message); }
            Debug.Log(Tag + $"M4 前置：包里枚举到 {n} 个 Mesh");
            Report("M4 全量加载后 `Instantiate`", $"{TmpDir}/m4.asset", SaveCopy(src, "m4"));
        }

        // ---- M5：先让源网格**可读**（`GetVertexBuffer` 失败就退回）再落盘 ----
        bool touched = false;
        {
            try
            {
                using (var buf = src.GetVertexBuffer(0))
                {
                    Debug.Log(Tag + $"M5 `GetVertexBuffer` 拿到了（{buf.count} 个）—— 说明数据能读");
                    touched = true;
                }
            }
            catch (Exception e) { Debug.Log(Tag + "M5 `GetVertexBuffer` 抛了（预期，网格不可读）：" + e.GetType().Name); }
        }

        // ---- M6：**「先碰缓冲、再复制」** —— 假设是流式数据**还没读进来**就被 `Instantiate` 拷走了 ----
        if (touched)
        {
            Report("M6 先 `GetVertexBuffer`、再 `Instantiate`", $"{TmpDir}/m6.asset", SaveCopy(src, "m6"));
        }

        // ---- M7：M6 + `UploadMeshData(false)`（要求 Unity 把 CPU 侧的数据坐实）----
        if (touched)
        {
            var c = UnityEngine.Object.Instantiate(src);
            c.name = "m7";
            try { c.UploadMeshData(false); } catch (Exception e) { Debug.LogWarning(Tag + "M7 抛了：" + e.Message); }
            string p = $"{TmpDir}/m7.asset";
            AssetDatabase.DeleteAsset(p);
            AssetDatabase.CreateAsset(c, p);
            AssetDatabase.SaveAssets();
            Report("M7 碰缓冲 + `UploadMeshData(false)`", p, AssetDatabase.LoadAssetAtPath<Mesh>(p));
        }

        // ---- M8：**逐流把顶点/索引读出来自己重建**（最稳；`GetData` 只认基元数组 ⇒ 按 4 字节切）----
        if (touched)
        {
            try
            {
                using (var vb = src.GetVertexBuffer(0))
                using (var ib = src.GetIndexBuffer())
                {
                    int vstride = src.GetVertexBufferStride(0);
                    int vwords = vb.count * vstride / 4;
                    var vints = new uint[vwords];
                    bool gotV = TryGetData(vb, vints);
                    Debug.Log(Tag + $"M8 顶点缓冲：count={vb.count} stride={vstride} ⇒ {vwords} 个 32 位字 · GetData={(gotV ? "成功" : "失败")}");
                    if (!gotV) throw new Exception("顶点缓冲 GetData 两种姿势都不认");
                    var vbytes = new byte[vwords * 4];
                    Buffer.BlockCopy(vints, 0, vbytes, 0, vbytes.Length);

                    var iints = new uint[ib.count * ib.stride / 4];
                    bool gotI = TryGetData(ib, iints);
                    Debug.Log(Tag + $"M8 索引缓冲：count={ib.count} stride={ib.stride} · GetData={(gotI ? "成功" : "失败")}");
                    if (!gotI) throw new Exception("索引缓冲 GetData 失败");
                    var ibytes = new byte[iints.Length * 4];
                    Buffer.BlockCopy(iints, 0, ibytes, 0, ibytes.Length);

                    var m = new Mesh();
                    m.name = "m8";
                    m.SetVertexBufferParams(src.vertexCount, src.GetVertexAttributes());
                    m.SetVertexBufferData(vbytes, 0, 0, vbytes.Length, 0, MeshUpdateFlags.Default);
                    m.indexFormat = src.indexFormat;
                    m.SetIndexBufferParams((int)src.GetIndexCount(0), src.indexFormat);
                    m.SetIndexBufferData(ibytes, 0, 0, ibytes.Length, MeshUpdateFlags.Default);
                    m.subMeshCount = src.subMeshCount;
                    for (int i = 0; i < src.subMeshCount; i++) m.SetSubMesh(i, src.GetSubMesh(i));
                    m.bounds = src.bounds;
                    string p = $"{TmpDir}/m8.asset";
                    AssetDatabase.DeleteAsset(p);
                    AssetDatabase.CreateAsset(m, p);
                    AssetDatabase.SaveAssets();
                    Report("M8 逐流读出 + 自己重建", p, AssetDatabase.LoadAssetAtPath<Mesh>(p));
                }
            }
            catch (Exception e) { Debug.LogWarning(Tag + "M8 抛了：" + e.GetType().Name + "：" + e.Message); }
        }

        AssetDatabase.SaveAssets();
        Debug.Log(Tag + "结束（临时产物在 " + TmpDir + "，探针自己清）");
        AssetDatabase.DeleteAsset(TmpDir);
    }
}
