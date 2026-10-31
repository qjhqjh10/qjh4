// B109_E19_Texture3DBuild.cs — 第一百零九会话 · 执行代理 `B109_E19` · R4-05
//
// ============================ 它做什么 ============================
//  把原版 `Gargoyle Swarm` 的两个 `Texture3D` 从**原始体数据**重建成本工程的 `.asset`：
//    vectorfield  64 x 64 x 64   RGBA8（RGBA32）  Repeat / Bilinear  1,048,576 B
//    SDF_Test     31 x 31 x 64   R16_SFloat（RHalf）Clamp / Bilinear  123,008 B
//
// ============================ 判据（第一权威 = 解包 JSON 的字段 + 实解出的字节）============================
//  源：`D:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/Texture3D/`
//      · `Texture3D_-5601361985474094491.json` → m_Name=vectorfield · m_Format=8 · m_DataSize=1048576
//        m_Width=Height=Depth=64 · m_MipCount=1 · m_ColorSpace=0 · m_IsReadable=true
//        m_TextureSettings：FilterMode=1(Bilinear) · WrapU/V/W=0(Repeat) · Aniso=1
//      · `Texture3D_-6523327969514304178.json` → m_Name=SDF_Test · m_Format=45 · m_DataSize=123008
//        m_Width=31 · m_Height=31 · m_Depth=64 · m_MipCount=1 · m_ColorSpace=0 · m_IsReadable=true
//        m_TextureSettings：FilterMode=1(Bilinear) · WrapU/V/W=1(Clamp) · Aniso=1
//      两条 `m_StreamData.size = 0` ⇒ 数据**内联**在 `image data.$bytes`（base64）。
//  判据全文 → `资料/完全复刻/判据/B109_R9_资源侧剩余.md` §5。
//
//  ⚠️ **`判据/B106_资源侧R0R4_施工单.md:135` 的 `RFloat` 是错的**：123,008 / 61,504 体素 = 2 B/体素
//     ⇒ 半精度（`RHalf`）。4 B 的 `RFloat` 会是 246,016 B。本脚本按 2 B 解，并在 `ExpectBytes` 里钉死。
//
// ============================ 为什么不用 PNG ============================
//  AssetRipper 导出的 `vectorfield.png`（64x4096）是「逐 z 切片竖排 + 每切片上下翻转」：
//  实测四种排布，只有 `png_row = z*H + (H-1-y)` 与原始字节的 mean|Δ| = 0.0000（另三种 5.89 / 7.02 / 6.08，
//  复算命令 `python -I -X utf8 d:/4/Unity/工具/build_texture3d.py --verify --png-verify`）。
//  而 `Texture3D.SetPixelData` 吃的就是 Unity 原生内存序（x 最快 → y → z），与 `image data` 字节序**一致**
//  ⇒ **直接喂原始字节，不需要任何翻转 / 重排**。SDF 那份 PNG 更只是 8 bit 可视化（首像素 26,0,0,255），
//  **拿它当数据会掉精度**，不满足原版的 R16_SFloat。
//
// ============================ 怎么跑（⛔ 本代理不跑 Unity，由调度台跑）============================
//  ① 先解原始字节（只读 d:/2，写到临时目录）：
//       python -I -X utf8 d:/4/Unity/工具/build_texture3d.py --write --out d:/4/_tmp_view/b109_e19/texture3d_raw
//  ② 跑本入口（两种取源，默认 ②-b 直接读原版 json）：
//       ②-a 用解出来的原始文件：设环境变量 B109_E19_RAW=d:/4/_tmp_view/b109_e19/texture3d_raw
//       ②-b 不设 ⇒ 自己读 `D:/2/.../Texture3D/*.json` 的 `image data.$bytes`（同样逐字节，不经过任何转换）
//       unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//         -projectPath "D:\4\Unity\MyGame" -executeMethod B109_E19_Texture3DBuild.Run -logFile -
//  ③ 产出：`Assets/WarpforgeVFX/ArenaOriginalPrefabs/_shared/vectorfield.asset`
//           `Assets/WarpforgeVFX/ArenaOriginalPrefabs/_shared/SDF_Test.asset`
//  ④ 自检：本入口在保存后**重新从盘上 Load** 一遍资产，核 `width/height/depth/format/mipCount`，
//     并把 `GetPixelData` 与源字节**逐字节**比对；任何一项不过 ⇒ 抛异常（⛔ 不静默）。
//     汇总同时写到 `d:/4/_tmp_view/b109_e19/texture3d_build_report.txt`，控制台每行以 `[B109E19]` 开头。
//
//  ⚠️ 还差一步（不在本入口里）：两个 `.asset` 建出来后要**接回** `Gargoyle Swarm` 的 VFX 图
//     （`B101_R3` 的 4 处 `m_Asset` 空 guid + 6 个输出 shader 还悬空）—— 那一步依赖 VisualEffectAsset 先导进来。
//
// ============================ 🔴 灭自证 ============================
//  · 把 `ExpectBytes` 改小一档 ⇒ `SetPixelData` 抛「数组长度不匹配」⇒ 入口失败（不会静默建出半张图）。
//  · 把 `SetPixelData` 换成「先上下翻转再喂」⇒ 第 ④ 步的逐字节比对红。
//  · 把 SDF 的 `RHalf` 换成 `RFloat` ⇒ 格式断言红（且字节数只有一半）。

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class B109_E19_Texture3DBuild
{
    const string SrcDir = "D:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/Texture3D";
    const string OutDir = "Assets/WarpforgeVFX/ArenaOriginalPrefabs/_shared";
    const string ReportPath = "d:/4/_tmp_view/b109_e19/texture3d_build_report.txt";

    sealed class Spec
    {
        public string Name;
        public string Json;
        public int W, H, D;
        public TextureFormat Format;
        public TextureWrapMode Wrap;
        public int ExpectBytes;
    }

    static readonly Spec[] Specs =
    {
        new Spec { Name = "vectorfield", Json = "Texture3D_-5601361985474094491.json",
                   W = 64, H = 64, D = 64, Format = TextureFormat.RGBA32,
                   Wrap = TextureWrapMode.Repeat, ExpectBytes = 1048576 },
        new Spec { Name = "SDF_Test", Json = "Texture3D_-6523327969514304178.json",
                   W = 31, H = 31, D = 64, Format = TextureFormat.RHalf,
                   Wrap = TextureWrapMode.Clamp, ExpectBytes = 123008 },
    };

    [MenuItem("Tools/B109 E19/重建两个 Gargoyle Texture3D")]
    public static void RunFromMenu()
    {
        Run();
        EditorUtility.DisplayDialog("B109_E19", "两个 Texture3D 已重建（详细读数看 Console）", "好");
    }

    /// <summary>Unity 批处理入口：`-executeMethod B109_E19_Texture3DBuild.Run`。</summary>
    public static void Run()
    {
        var lines = new List<string>();
        void Say(string s) { lines.Add(s); Debug.Log("[B109E19] " + s); }

        Say("开始：源 = " + (string.IsNullOrEmpty(RawDir()) ? SrcDir + "（json 内联 base64）" : RawDir()));
        Say("输出目录 = " + OutDir);

        var made = new List<string>();
        foreach (var spec in Specs)
        {
            byte[] src = LoadSource(spec);
            var tex = Build(spec, src, Say);
            var path = OutDir + "/" + spec.Name + ".asset";
            SaveAsset(tex, path, Say);
            made.Add(path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        bool allOk = true;
        foreach (var spec in Specs)
        {
            byte[] src = LoadSource(spec);
            allOk &= VerifySaved(spec, src, OutDir + "/" + spec.Name + ".asset", Say);
        }

        Say(allOk ? "结论：PASS（两个资产 尺寸/格式/mip/逐字节 全部与源一致）"
                  : "结论：FAIL（见上面 ✗ 行）");
        Flush(lines);
        if (!allOk) throw new Exception("B109_E19：Texture3D 重建自检未通过（见 Console 的 [B109E19] 行）");
    }

    static string RawDir()
    {
        return Environment.GetEnvironmentVariable("B109_E19_RAW");
    }

    // ---------------------------------------------------------------- 取源
    static byte[] LoadSource(Spec spec)
    {
        string raw = RawDir();
        if (!string.IsNullOrEmpty(raw))
        {
            string p = Path.Combine(raw, spec.Name + ".bytes");
            if (!File.Exists(p)) throw new FileNotFoundException("B109_E19_RAW 目录里没有 " + p);
            byte[] rawBytes = File.ReadAllBytes(p);
            if (rawBytes.Length != spec.ExpectBytes)
                throw new Exception(string.Format("{0}: {1} = {2} B，期望 {3} B", spec.Name, p, rawBytes.Length, spec.ExpectBytes));
            return rawBytes;
        }

        string json = Path.Combine(SrcDir, spec.Json);
        if (!File.Exists(json)) throw new FileNotFoundException("找不到原版 json：" + json);
        // ⚠️ 不引 JsonUtility：它要求字段名可映射到 C# 类；这里只要一个 base64 串，按串切更快也更稳。
        string text = File.ReadAllText(json, Encoding.UTF8);
        string key = "\"$bytes\"";
        int k = text.IndexOf(key, StringComparison.Ordinal);
        if (k < 0) throw new Exception(spec.Json + "：找不到 \"$bytes\"（判据要求数据内联在 image data.$bytes）");
        int q1 = text.IndexOf('"', k + key.Length);
        int q2 = q1 < 0 ? -1 : text.IndexOf('"', q1 + 1);
        if (q1 < 0 || q2 < 0) throw new Exception(spec.Json + "：\"$bytes\" 的值不是字符串");
        byte[] b = Convert.FromBase64String(text.Substring(q1 + 1, q2 - q1 - 1));
        if (b.Length != spec.ExpectBytes)
            throw new Exception(string.Format("{0}: 解出 {1} B，期望 {2} B", spec.Name, b.Length, spec.ExpectBytes));
        return b;
    }

    // ---------------------------------------------------------------- 建
    static Texture3D Build(Spec spec, byte[] src, Action<string> say)
    {
        if (spec.W * spec.H * spec.D * BytesPerVoxel(spec.Format) != src.Length)
            throw new Exception(spec.Name + "：尺寸 x 每体素字节数 != 源字节数");

        var t = new Texture3D(spec.W, spec.H, spec.D, spec.Format, false);
        int voxels = spec.W * spec.H * spec.D;

        try
        {
            if (spec.Format == TextureFormat.RGBA32)
            {
                var px = new Color32[voxels];
                for (int i = 0; i < voxels; i++)
                    px[i] = new Color32(src[i * 4 + 0], src[i * 4 + 1], src[i * 4 + 2], src[i * 4 + 3]);
                t.SetPixelData(px, 0);
            }
            else if (spec.Format == TextureFormat.RHalf)
            {
                var px = new ushort[voxels];
                Buffer.BlockCopy(src, 0, px, 0, src.Length);
                t.SetPixelData(px, 0);
            }
            else
            {
                throw new Exception("未覆盖的格式：" + spec.Format);
            }
        }
        catch (Exception e)
        {
            // 兜底：Unity 若拒绝 short 的元素类型，退到 `SetPixels(Color[])`（half→float→half 是精确的）
            say("  ⚠ " + spec.Name + " SetPixelData 失败（" + e.GetType().Name + ": " + e.Message + "），改走 SetPixels 兜底");
            if (spec.Format == TextureFormat.RHalf)
            {
                var cols = new Color[voxels];
                for (int i = 0; i < voxels; i++)
                {
                    ushort bits = (ushort)(src[i * 2] | (src[i * 2 + 1] << 8));
                    float v = HalfBitsToFloat(bits);
                    cols[i] = new Color(v, 0f, 0f, 0f);
                }
                t.SetPixels(cols);
            }
            else
            {
                throw;
            }
        }

        t.wrapMode = spec.Wrap;
        t.filterMode = FilterMode.Bilinear;
        t.anisoLevel = 1;
        t.Apply(false, false);   // 不要 mip、不要把 CPU 数据丢掉（原版 m_IsReadable = true）
        say("  建好 " + spec.Name + "：" + spec.W + "x" + spec.H + "x" + spec.D + " " + spec.Format + " " + spec.Wrap);
        return t;
    }

    static int BytesPerVoxel(TextureFormat f)
    {
        if (f == TextureFormat.RGBA32) return 4;
        if (f == TextureFormat.RHalf) return 2;
        throw new Exception("未覆盖的格式：" + f);
    }

    static void SaveAsset(Texture3D tex, string path, Action<string> say)
    {
        var old = AssetDatabase.LoadAssetAtPath<Texture3D>(path);
        if (old == null)
        {
            AssetDatabase.CreateAsset(tex, path);
            say("  新建 " + path);
        }
        else if (old.width == tex.width && old.height == tex.height && old.depth == tex.depth && old.format == tex.format)
        {
            // 同尺寸同格式 ⇒ 原地覆盖（**保住 guid**；换 guid 会让已存盘的引用悬空）
            EditorUtility.CopySerialized(tex, old);
            EditorUtility.SetDirty(old);
            UnityEngine.Object.DestroyImmediate(tex);
            say("  原地覆盖 " + path + "（guid 不变）");
        }
        else
        {
            // 尺寸/格式变了 ⇒ 只能重建（guid 会换，必须出声）
            say("  ⚠ " + path + " 已存在但尺寸/格式不同（旧 " + old.width + "x" + old.height + "x" + old.depth +
                " " + old.format + "）⇒ 删除重建（**guid 会变，引用要重接**）");
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(tex, path);
        }
    }

    // ---------------------------------------------------------------- 自检
    static bool VerifySaved(Spec spec, byte[] src, string path, Action<string> say)
    {
        var t = AssetDatabase.LoadAssetAtPath<Texture3D>(path);
        if (t == null) { say("  ✗ " + path + " 读不回来"); return false; }
        bool ok = true;
        if (t.width != spec.W || t.height != spec.H || t.depth != spec.D)
        { say(string.Format("  ✗ {0} 尺寸 = {1}x{2}x{3}，期望 {4}x{5}x{6}", spec.Name, t.width, t.height, t.depth, spec.W, spec.H, spec.D)); ok = false; }
        if (t.format != spec.Format)
        { say(string.Format("  ✗ {0} 格式 = {1}，期望 {2}", spec.Name, t.format, spec.Format)); ok = false; }
        if (t.mipmapCount != 1)
        { say(string.Format("  ✗ {0} mipCount = {1}，期望 1", spec.Name, t.mipmapCount)); ok = false; }
        if (t.wrapMode != spec.Wrap)
        { say(string.Format("  ✗ {0} wrapMode = {1}，期望 {2}", spec.Name, t.wrapMode, spec.Wrap)); ok = false; }

        string cmp;
        try
        {
            int voxels = spec.W * spec.H * spec.D;
            if (spec.Format == TextureFormat.RGBA32)
            {
                var got = t.GetPixelData<Color32>(0);
                if (got.Length != voxels) { cmp = "FAIL len " + got.Length; }
                else
                {
                    cmp = "OK";
                    for (int i = 0; i < voxels; i++)
                    {
                        if (got[i].r != src[i * 4 + 0] || got[i].g != src[i * 4 + 1] ||
                            got[i].b != src[i * 4 + 2] || got[i].a != src[i * 4 + 3])
                        { cmp = "FAIL 首个差异 voxel " + i; break; }
                    }
                }
            }
            else
            {
                var got = t.GetPixelData<ushort>(0);
                if (got.Length != voxels) { cmp = "FAIL len " + got.Length; }
                else
                {
                    cmp = "OK";
                    for (int i = 0; i < voxels; i++)
                    {
                        ushort want = (ushort)(src[i * 2] | (src[i * 2 + 1] << 8));
                        if (got[i] != want) { cmp = "FAIL 首个差异 voxel " + i + " got=" + got[i] + " want=" + want; break; }
                    }
                }
            }
        }
        catch (Exception e)
        {
            cmp = "VERIFY-API-UNAVAILABLE " + e.GetType().Name + ": " + e.Message;
        }

        if (cmp != "OK") { say("  ✗ " + spec.Name + " 逐字节比对：" + cmp); ok = false; }
        else say("  ✓ " + spec.Name + "：" + spec.W + "x" + spec.H + "x" + spec.D + " " + spec.Format +
                 " mip=1 wrap=" + spec.Wrap + " · GetPixelData 与源逐字节相同（" + src.Length + " B）");
        return ok;
    }

    // ---------------------------------------------------------------- 工具
    static void Flush(List<string> lines)
    {
        try
        {
            string dir = Path.GetDirectoryName(ReportPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllLines(ReportPath, lines, new UTF8Encoding(false));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[B109E19] 汇总文件写不了（不影响资产）：" + e.Message);
        }
    }

    /// <summary>IEEE 754 half（U16）→ float。只用于 SetPixels 兜底路径。</summary>
    static float HalfBitsToFloat(ushort h)
    {
        int sign = (h >> 15) & 1;
        int exp = (h >> 10) & 0x1F;
        int mant = h & 0x3FF;
        float v;
        if (exp == 0)
        {
            v = (float)(mant / 1024.0 * Math.Pow(2.0, -14));
        }
        else if (exp == 31)
        {
            v = mant == 0 ? float.PositiveInfinity : float.NaN;
        }
        else
        {
            v = (float)((1.0 + mant / 1024.0) * Math.Pow(2.0, exp - 15));
        }
        return sign == 1 ? -v : v;
    }
}
#endif
