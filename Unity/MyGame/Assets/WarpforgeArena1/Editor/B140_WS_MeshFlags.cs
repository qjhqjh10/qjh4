// B140_WS_MeshFlags.cs — 网格内部标志写口（审计 `B136` top-5 #4）
//
// 背景：审计 `资料/完全复刻/报告/B136_审计_网格与预制体结构.md` A-14/A-15/A-16 ——
//   原版 101 份同名网格的**内部序列化标志**与我们生成出来的**逐份不同**：
//     · `m_MeshOptimizationFlags`  原版 **-1**（Everything） / 我们 **1**（PolygonOrder）· 101/101 全偏
//     · `m_MeshUsageFlags`         原版 **4**（1 份是 8）      / 我们 **0**          · 65 份不同
//     · `m_KeepVertices / m_KeepIndices` 原版 **1/1**         / 我们 **0/0**       · 29 份不同
//   这批网格的来源 = `WarpforgeArena1/Editor/EffectExporter.cs` 的 `ImportMesh`
//   （`DeepCopyMesh` 或 `Instantiate` → `AssetDatabase.CreateAsset` / `CopySerialized`），
//   以及 `Resources/Art/card3d` 那两份（同一个导入族）。**新造出来的 `Mesh` 对象带的是 Unity 默认值**
//   ⇒ `m_MeshOptimizationFlags` 落成 1、三个 keep/usage 落成 0。原版那份是**他们自己的 asset pipeline
//   写死的**（-1/4/1/1），我们从来没有写过 —— 所以 `grep 生成器` 0 命中，不是「写错了」而是「没写」。
//
// 判据出处（逐份原版值 → `Assets/Mesh/<同名>.asset`）：
//   `d:/2/tools/assetripper_out/ExportedProject/Assets/Mesh/`
//   本轮把 101 份的**原版逐格值**烘进下面的 `Manifest`（⛔ 不是「一律 -1/4/1/1」——
//   原版有 34 份 usage = 0、1 份 usage = 8、1 份 keep = 0/0，照抄才不会把对的改错）。
//
// ⚠️ 两条**故意不做**的事（都在报告 §没做的部分 里写了理由）：
//   ① **不调 `MeshUtility.Optimize`** —— 它会真的重排几何；这批网格的顶点字节是**逐字节验过**与原版相同的
//      （审计 A-04/A-10：92/101 对逐字节相同）。我们只要那个**序列化标志**的数值，⛔ 不要它顺手改几何。
//   ② **不动 `m_IsReadable`**（原版 79 份 = 0、我们 = 1）。它决定**运行时** `mesh.vertices` 能不能读；
//      改了会直接影响读顶点的那些路径，且本活判据（top-5 #4）只点了那四个字段。见报告「没做的部分」。
//
// 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B140_WS_MeshFlags.Run -logFile -
//   只读校验： `-executeMethod B140_WS_MeshFlags.Verify`
//   试运行（只打印差异、不写盘）： `-executeMethod B140_WS_MeshFlags.DryRun`
//
// 落盘纪律（铁律：`SetDirty` 之后「写完当场回读」抓不到「没落盘」）：
//   每一份都走**三段式**：① `SerializedObject` 写 → `SaveAssets` → **重开文件按字节读回**
//   ② 不一致 ⇒ **文本级改那 4 行** → `ImportAsset(ForceUpdate)` → **再重开文件读回**
//   ③ 还不一致 ⇒ **点名报红**（⛔ 不静默、不假装成功）。行尾/ BOM 原样保留（只替换行内数值）。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class B140_WS_MeshFlags
{
    const string T = "[B140_WS] ";

    /// <summary>写口要覆盖的网格数（原版逐份对出来 = 101）。⛔ 改名单就同步改这里 —— 它防的是「名单被截断」。</summary>
    public const int ExpectedCount = 101;

    /// <summary>四个字段在 YAML 里的名字（顺序 = 下面的 `int[4]` 顺序）。</summary>
    public static readonly string[] Fields =
        { "m_MeshOptimizationFlags", "m_MeshUsageFlags", "m_KeepVertices", "m_KeepIndices" };

    // 清单格式：`<相对 Assets 的路径>|<OptimizationFlags>|<UsageFlags>|<KeepVertices>|<KeepIndices>`
    // ⚠️ 值 = **同名原版 asset 的现读值**（2026-11-04 读 `ExportedProject/Assets/Mesh/`），⛔ 别「觉得应该一样就统一填」。
    static readonly string[] Manifest =
    {
        "CardPresentation/Resources/Art/card3d/Card 3D WH40k.asset|-1|4|1|1",
        "CardPresentation/Resources/Art/card3d/FxObject_cylinder_short.asset|-1|4|1|1",
        "CardPresentation/Resources/Art/vfx/Cylinder.asset|-1|4|1|1",
        "WarpforgeVFX/ArenaOriginalPrefabs/battlearenaaeldari/Tower 11.asset|-1|8|0|0",
        "WarpforgeVFX/Meshes/400px-Choppa.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Axe_1.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Background Wolf.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Background space.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Blade_DA.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/BoosterPack.asset|-1|0|0|0",
        "WarpforgeVFX/Meshes/Bullet_Body.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Card 3D Subdivided.asset|-1|0|0|0",
        "WarpforgeVFX/Meshes/Card 3D WH40k.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Card_Remnant_HO Optimization 2.asset|-1|0|0|0",
        "WarpforgeVFX/Meshes/ChemVial.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Chitin barb.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Cone.006.asset|-1|1|0|0",
        "WarpforgeVFX/Meshes/Cone.007.asset|-1|1|0|0",
        "WarpforgeVFX/Meshes/Cylinder curved shallow.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Cylinder_Ring.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Cylinder_curve.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Disk.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Dynamic Lights 11.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Dynamic Lights 21.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Dynamic Lights 31.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Dynamic Lights 51.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Dynamic Lights 61.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/DynamiteBundle.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/EC_Sword.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Full Moon 101.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Full Moon 111.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Full Moon 121.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Full Moon 131.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Full Moon 14.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Full Moon 21.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Full Moon 31.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Full Moon 41.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Full Moon 51.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Full Moon 61.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Full Moon 71.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Full Moon 81.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Full Moon 91.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Full Moon1.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/FxObject_cylinder_short.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/GSC Dagger.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Grenade.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Hammer.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Harpoon.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Helspear.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Hemisphere.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Ice.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/IceShard01.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/IceShardTiny.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Keep Light1.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/KrootJavelin.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Missile.001.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Missile.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Moon 4.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Mountain Background First Light.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Mushroom.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Pray VFX Mesh3.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Rock003.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Rock004.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Rock005.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/RockDebris_Low.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/RockSpike_Low.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Rock_A_02_LOD00.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Rock_A_02_LOD03.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Rock_A_02_LOD04.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/RocketWarhead.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Shatter Piece 1.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Shatter Piece 3.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Shell Bolter Imperial.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Shell Bolter Traitor.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Shell Ork.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Shield Ico.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Shield_1.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Ship 11.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Ship 21.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Ship 31.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Ship 41.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Ship 51.asset|-1|0|1|1",
        "WarpforgeVFX/Meshes/Slash.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Spear.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Spike_1.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Spore Mine.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Squig1.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Squig2.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Squig_low Merged.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Stikka.001.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Tube_pf_sh_11.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Tube_pf_sh_2.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Tube_pf_sh_21.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Tube_pf_sh_6.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Tyranid_Claw.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Tyranid_Rib.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/Vanguard Frame Animated_Mesh.asset|-1|0|0|0",
        "WarpforgeVFX/Meshes/debris_1_sm.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/debris_2_sm.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/stone001.asset|-1|4|1|1",
        "WarpforgeVFX/Meshes/sword-mesh.asset|-1|4|1|1",
    };

    // ── 清单读口 ───────────────────────────────────────────────────────────────

    /// <summary>清单里的相对路径（相对 `Assets/`，不含 `Assets/` 前缀）。</summary>
    public static string RelPath(int i) { return Manifest[i].Split('|')[0]; }
    /// <summary>清单里的 `Assets/...` 工程路径。</summary>
    public static string AssetPath(int i) { return "Assets/" + RelPath(i); }
    /// <summary>清单里第 i 条的**原版目标值**（顺序同 <see cref="Fields"/>）。</summary>
    public static int[] Expected(int i)
    {
        var p = Manifest[i].Split('|');
        return new[] { int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]), int.Parse(p[4]) };
    }
    public static int Count { get { return Manifest.Length; } }

    // ── 写入 ───────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Warforge/B140_WS 网格内部标志 · 写入（101 份）", false, 401)]
    public static void Run() { ApplyAll(false); }

    [MenuItem("Tools/Warforge/B140_WS 网格内部标志 · 试运行（不写盘）", false, 402)]
    public static void DryRun() { ApplyAll(true); }

    [MenuItem("Tools/Warforge/B140_WS 网格内部标志 · 只读校验", false, 403)]
    public static void Verify() { VerifyAll(); }

    public static void ApplyAll(bool dry)
    {
        int ok = 0, changed = 0, fail = 0, vertsChanged = 0;
        if (Manifest.Length != ExpectedCount)
            Debug.LogError(T + $"🔴 清单条数 {Manifest.Length} ≠ 期望 {ExpectedCount} —— 名单被截断/追加过，先核对再写");

        for (int i = 0; i < Manifest.Length; i++)
        {
            string assetPath = AssetPath(i);
            string full = FullPath(assetPath);
            string want = string.Join("/", Array.ConvertAll(Expected(i), v => v.ToString()));

            if (!File.Exists(full)) { fail++; Debug.LogError(T + $"🔴 缺文件：{assetPath}"); continue; }

            int[] before = ReadFlagsFromFile(full);
            if (before == null) { fail++; Debug.LogError(T + $"🔴 读不到四个字段：{assetPath}"); continue; }
            string vBefore = VertexFingerprint(full);

            int[] expect = Expected(i);
            if (Same(before, expect)) { ok++; continue; }   // 已经对了

            if (dry) { changed++; Debug.Log(T + $"· 需要改：{assetPath}  现 {Show(before)} → 原版 {want}"); continue; }

            bool wrote = WriteOne(assetPath, expect);
            int[] after = ReadFlagsFromFile(full);
            string vAfter = VertexFingerprint(full);
            if (vBefore != vAfter)
            {
                vertsChanged++;
                Debug.LogError(T + $"🔴 顶点数据指纹变了（写口动了不该动的东西）：{assetPath}  {vBefore} → {vAfter}");
            }
            if (wrote && after != null && Same(after, expect)) { ok++; changed++; }
            else { fail++; Debug.LogError(T + $"🔴 写不进去：{assetPath}  现 {(after == null ? "<读不到>" : Show(after))} / 原版 {want}"); }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string head = dry ? "试运行" : "写入";
        Debug.Log(T + $"{head}完成：已对 {ok} · 本次改动 {changed} · 失败 {fail} · 顶点指纹变化 {vertsChanged}（共 {Manifest.Length} 份）"
                  + (fail == 0 && vertsChanged == 0 ? "（全绿）" : "（有红，见上）"));
    }

    /// <summary>先 `SerializedObject`，不成就**文本级改那 4 行**；每段都**重开文件回读**核对。</summary>
    static bool WriteOne(string assetPath, int[] expect)
    {
        // ① SerializedObject（能落盘的正路）
        try
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (mesh == null)
            {
                Debug.LogWarning(T + $"`{assetPath}` 不是可加载的 Mesh ⇒ 只能走文本级那条路");
            }
            else
            {
                bool allFound = true;
                var so = new SerializedObject(mesh);
                for (int k = 0; k < Fields.Length; k++)
                {
                    var p = so.FindProperty(Fields[k]);
                    if (p == null) { allFound = false; Debug.LogWarning(T + $"`{assetPath}` 的 SerializedObject 里没有 `{Fields[k]}`"); continue; }
                    p.intValue = expect[k];
                }
                if (allFound)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(mesh);
                    so.Dispose();
                    AssetDatabase.SaveAssets();
                    var back1 = ReadFlagsFromFile(FullPath(assetPath));
                    if (back1 != null && Same(back1, expect))
                    {
                        Debug.Log(T + $"✓ SerializedObject 落盘：{assetPath}");
                        return true;
                    }
                    Debug.LogWarning(T + $"`{assetPath}` SerializedObject 之后**回读不一致** ⇒ 退回文本级改写");
                }
                else so.Dispose();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(T + $"`{assetPath}` SerializedObject 抛了（{e.GetType().Name}: {e.Message}）⇒ 退回文本级改写");
        }

        // ② 文本级改那 4 行（只改数值，行尾 / BOM 原样）
        string full = FullPath(assetPath);
        if (!PatchFileFields(full, expect, out string err))
        {
            Debug.LogError(T + $"🔴 文本级改写失败：{assetPath} —— {err}");
            return false;
        }
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        var back2 = ReadFlagsFromFile(full);
        if (back2 != null && Same(back2, expect))
        {
            Debug.Log(T + $"✓ 文本级改写落盘：{assetPath}");
            return true;
        }
        return false;
    }

    /// <summary>只读校验（⛔ 不写盘）。断言宿主 `B140_WS_Assert` 用的也是这几个读口。</summary>
    public static void VerifyAll()
    {
        int good = 0, bad = 0;
        if (Manifest.Length != ExpectedCount)
            Debug.LogError(T + $"🔴 清单条数 {Manifest.Length} ≠ 期望 {ExpectedCount}");
        for (int i = 0; i < Manifest.Length; i++)
        {
            string full = FullPath(AssetPath(i));
            if (!File.Exists(full)) { bad++; Debug.LogError(T + $"✗ 缺文件 {AssetPath(i)}"); continue; }
            int[] got = ReadFlagsFromFile(full);
            int[] want = Expected(i);
            if (got != null && Same(got, want)) good++;
            else
            {
                bad++;
                Debug.LogError(T + $"✗ {AssetPath(i)}  现 {(got == null ? "<读不到>" : Show(got))} / 原版 {Show(want)}");
            }
        }
        Debug.Log(T + $"校验：✓ {good} · ✗ {bad}（共 {Manifest.Length} 份）" + (bad == 0 ? "（全绿）" : ""));
    }

    // ── 读 / 写工具（也供断言宿主复用）────────────────────────────────────────

    public static string FullPath(string assetPath)
    {
        const string prefix = "Assets/";
        string rel = assetPath.StartsWith(prefix, StringComparison.Ordinal) ? assetPath.Substring(prefix.Length) : assetPath;
        return Path.Combine(Application.dataPath, rel).Replace('\\', '/');
    }

    /// <summary>从 `.asset`（Force Text ⇒ YAML）文本里读那四个字段。读不全 ⇒ `null`（⛔ 不拿 0 顶替）。</summary>
    public static int[] ReadFlagsFromFile(string full)
    {
        if (!File.Exists(full)) return null;
        string text = Encoding.UTF8.GetString(File.ReadAllBytes(full));
        var got = new int[Fields.Length];
        for (int k = 0; k < Fields.Length; k++)
        {
            var m = Regex.Match(text, @"(?m)^[ \t]*" + Regex.Escape(Fields[k]) + @":[ \t]*(-?\d+)[ \t]*\r?$");
            if (!m.Success) return null;
            got[k] = int.Parse(m.Groups[1].Value);
        }
        return got;
    }

    /// <summary>顶点指纹 = `m_VertexCount` / `m_DataSize` / `_typelessdata` 的长度与前 64 字符。
    /// 写口**不许**改它（这批网格的顶点字节是逐字节验过与原版相同的，见审计 A-04/A-10）。</summary>
    public static string VertexFingerprint(string full)
    {
        if (!File.Exists(full)) return "<missing>";
        string text = Encoding.UTF8.GetString(File.ReadAllBytes(full));
        var c = Regex.Match(text, @"m_VertexCount: (\d+)");
        var d = Regex.Match(text, @"m_DataSize: (\d+)");
        var t = Regex.Match(text, @"_typelessdata: ([0-9a-fA-F]+)");
        if (!t.Success) return $"vc={(c.Success ? c.Groups[1].Value : "?")};ds={(d.Success ? d.Groups[1].Value : "?")};data=<none>";
        string hex = t.Groups[1].Value;
        return $"vc={c.Groups[1].Value};ds={d.Groups[1].Value};n={hex.Length};head={hex.Substring(0, Math.Min(64, hex.Length))}";
    }

    /// <summary>文本级把四个字段的**数值**换掉（行尾 / 编码 / BOM 原样保留）。
    /// 返回 false 时 `err` 写清是哪个字段没找到。</summary>
    public static bool PatchFileFields(string full, int[] values, out string err)
    {
        err = null;
        if (!File.Exists(full)) { err = "文件不存在"; return false; }
        byte[] raw = File.ReadAllBytes(full);
        bool bom = raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF;
        int off = bom ? 3 : 0;
        string text = Encoding.UTF8.GetString(raw, off, raw.Length - off);

        for (int k = 0; k < Fields.Length; k++)
        {
            var re = new Regex(@"(?m)^([ \t]*" + Regex.Escape(Fields[k]) + @":[ \t]*)(-?\d+)([ \t]*\r?)$");
            if (!re.IsMatch(text)) { err = $"找不到 `{Fields[k]}` 那一行"; return false; }
            text = re.Replace(text, m => m.Groups[1].Value + values[k] + m.Groups[3].Value, 1);
        }

        byte[] body = Encoding.UTF8.GetBytes(text);
        byte[] outp = body;
        if (bom)
        {
            outp = new byte[body.Length + 3];
            outp[0] = 0xEF; outp[1] = 0xBB; outp[2] = 0xBF;
            Array.Copy(body, 0, outp, 3, body.Length);
        }
        File.WriteAllBytes(full, outp);
        return true;
    }

    static bool Same(int[] a, int[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    public static string Show(int[] v)
    {
        return v == null ? "<null>" : string.Join("/", Array.ConvertAll(v, x => x.ToString()));
    }
}
