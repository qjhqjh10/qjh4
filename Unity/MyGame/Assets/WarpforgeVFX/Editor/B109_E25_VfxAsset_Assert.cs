// B109_E25_VfxAsset_Assert.cs — 第一百零九会话 · 执行代理 `B109_E25` 的断言
//   任务 = 「灵族 VFX 的两个硬阻塞」：① 4 处 `VisualEffect.m_Asset` 空 guid ② 6 个 `Hidden/VFX/…` 输出 shader 不存在。
//   ⚠️ **开工时这两条已经落地**（`B107_R4` 做、`B108_A14` 复核修编译），本文件是**回归闸**，不是首次验收。
//   报告 = `资料/完全复刻/报告/B109_E25.md`（含逐条复核证据 + 与原版字节码/反编译体的对账）。
//
// ============================ 它盯的是什么（四组）============================
//  A 组 · **4 处 `m_Asset`**（`Y-4`）：Leviathan Sweeping Infestation ×3、Saim Hann Infinity Circuit Overload ×1。
//        断言 = ①文件存在（前提）②`m_Asset:` 条数 == 3 / 1 ③零 guid 的 `m_Asset` == 0
//        ④每一处都逐字等于 `{fileID: 2058629509, guid: <VFX 资产 guid>, type: 2}` ⑤该 guid 能解析到 **同名** 的 `.asset`
//        ⑥该 `.asset` 的 YAML 头是 `--- !u!2058629509`（= VisualEffectAsset 的 classID，**不依赖 Unity 认不认这个类型**）。
//  B 组 · **6 个输出 shader**（`Y-5`）：文件在 / `Shader "…"` 名逐字对 / 两个 pass 的 `Blend`+`ZWrite`+`Cull`+`ZClip`
//        + SubShader `"QUEUE"="Transparent+0"` 逐条对 / 无本地 `#include`（不能又拆回 `.hlsl` 变体）/
//        `Shader.Find` 非空（**这一条只有 Unity 导入后才成立**；`isSupported` 只打印不判红 —— 批处理可能无图形设备）。
//  C 组 · **6 个输出材质**：`m_Shader` 的 guid 必须解析到 B 组那一份 shader（不是「同名但另一份」）。
//  D 组 · **引用闭包**：三个 VFX 资产（Gargoyle Swarm / Aeldari Portal / Aeldari Dust）的传递引用里，
//        未解析的 guid 只允许是那两个 Texture3D（`R4-05`，归 `B109_E19`）—— **别的任何悬空都红**。
//
// ============================ 怎么跑（执行代理无权跑 Unity）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B109_E25_VfxAsset_Assert.Run -logFile -
//   🔴 **接线由调度台做**（本批不碰任何断言宿主）：在要用它的宿主里加一行 `B109_E25_VfxAsset_Assert.Run();`。
//      本类自带 `Check` / 计数，不依赖宿主的 `Check`。
//   判绿 = 末行 `E25 合计：N 通过 / 0 失败`；出现 `E25 … 未验到` 时**别读成通过**（那是批处理环境缺前提，不是实现对）。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · 把任一 prefab 的 `m_Asset` 改回零 guid / 少一处 / fileID 写错 ⇒ A 组红。
//   · 把 VFX 资产的 guid 指错（指到另一个 `.asset`）⇒ A5/A6 红（名字或 classID 对不上）。
//   · 删掉任一 `.shader` / 改 `Shader "…"` 名 / 把 Blend 改回 Opaque / 把 `ZWrite` 打开 / 去掉 `ZClip On`
//     ⇒ B 组红（B1…B7 各自红）。
//   · 把 `.mat` 的 `m_Shader` 指回原版那个悬空 guid（或指到另一份同名 shader）⇒ C 组红。
//   · 把 VFX 资产里某个子资产/材质/贴图引用改坏 ⇒ D 组红（**这是唯一能抓「资产半边丢了」的那组**）。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class B109_E25_VfxAsset_Assert
{
    const string P = "E25 ";
    /// <summary>`Application.dataPath` 之下的 VFX 根（本工程约定：没有 `CardPresentation/` 前缀）。</summary>
    const string VfxRoot = "WarpforgeVFX";

    static int _ok, _bad, _skip;

    // ---------------------------------------------------------------- 表（判据出处见文件头与报告）
    // A 组：prefab 相对路径 · 期望 `m_Asset` 处数 · VFX 资产名 · VFX 资产 guid
    //   判据 = 原版 `ExportedProject/Assets/EnvironmentalCondition {Leviathan Sweeping Infestation,Saim Hann Infinity Circuit Overload}.prefab`
    //   的 3+1 处 `m_Asset` 逐字（`报告/B109_E25.md` §1）。
    static readonly string[][] Prefabs =
    {
        new[] { "Prefabs/EnvironmentalCondition Leviathan Sweeping Infestation.prefab", "3", "Gargoyle Swarm", "4db4cc7ece60c8348ae08154b347a1bc" },
        new[] { "Prefabs/EnvironmentalCondition Saim Hann Infinity Circuit Overload.prefab", "1", "Aeldari Portal", "75c1de0d7cdd4dd4fb39c970d6d2de55" },
    };

    // B 组：shader 名 · 相对 `Shaders/` 的文件名 · `Blend` 三元组（两个 pass 相同，出现次数 = 2）
    //   判据 = `d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/<hash>.shader` 的**非 HLSL 骨架**
    //   （逐行 diff = 0，`报告/B109_E25.md` §2）；`OriginalShaderBlendTable.cs` 的 `{5,10,0}` / `{5,1,0}` 可交叉验证。
    static readonly string[][] Shaders =
    {
        new[] { "Hidden/VFX/Aeldari Dust/System/Output Particle Unlit Quad", "Hidden_VFX_Aeldari Dust_System_Output Particle Unlit Quad.shader", "SrcAlpha OneMinusSrcAlpha" },
        new[] { "Hidden/VFX/Aeldari Portal/System/Output Particle Unlit Quad", "Hidden_VFX_Aeldari Portal_System_Output Particle Unlit Quad.shader", "SrcAlpha One" },
        new[] { "Hidden/VFX/Aeldari Portal/System (3)/Output ParticleStrip Unlit Quad", "Hidden_VFX_Aeldari Portal_System (3)_Output ParticleStrip Unlit Quad.shader", "SrcAlpha One" },
        new[] { "Hidden/VFX/Aeldari Portal/System (4)/Output Particle Unlit Quad", "Hidden_VFX_Aeldari Portal_System (4)_Output Particle Unlit Quad.shader", "SrcAlpha One" },
        new[] { "Hidden/VFX/Aeldari Portal/System (5)/Output Particle Unlit Quad", "Hidden_VFX_Aeldari Portal_System (5)_Output Particle Unlit Quad.shader", "SrcAlpha One" },
        new[] { "Hidden/VFX/Gargoyle Swarm/System/Output Particle Unlit Quad", "Hidden_VFX_Gargoyle Swarm_System_Output Particle Unlit Quad.shader", "SrcAlpha OneMinusSrcAlpha" },
    };

    // C 组：输出材质相对路径 · 期望 shader 名（= `.mat` 的 `m_Name`，也是它的 `m_Shader` 应解析到的那份）
    //   这 6 个 `.mat` 与 EP 原件**逐字节相同**（`报告/B109_E25.md` §3）；`m_SavedProperties` 全空是原版就这样。
    static readonly string[][] Materials =
    {
        new[] { "ArenaOriginalPrefabs/_shared/Hidden_VFX_Aeldari Dust_System_Output Particle Unlit Quad.mat", "Hidden/VFX/Aeldari Dust/System/Output Particle Unlit Quad" },
        new[] { "ArenaOriginalPrefabs/battlearenaaeldari/Hidden_VFX_Aeldari Portal_System_Output Particle Unlit Quad.mat", "Hidden/VFX/Aeldari Portal/System/Output Particle Unlit Quad" },
        new[] { "ArenaOriginalPrefabs/battlearenaaeldari/Hidden_VFX_Aeldari Portal_System (3)_Output ParticleStrip Unlit Quad.mat", "Hidden/VFX/Aeldari Portal/System (3)/Output ParticleStrip Unlit Quad" },
        new[] { "ArenaOriginalPrefabs/battlearenaaeldari/Hidden_VFX_Aeldari Portal_System (4)_Output Particle Unlit Quad.mat", "Hidden/VFX/Aeldari Portal/System (4)/Output Particle Unlit Quad" },
        new[] { "ArenaOriginalPrefabs/battlearenaaeldari/Hidden_VFX_Aeldari Portal_System (5)_Output Particle Unlit Quad.mat", "Hidden/VFX/Aeldari Portal/System (5)/Output Particle Unlit Quad" },
        new[] { "ArenaOriginalPrefabs/_shared/Hidden_VFX_Gargoyle Swarm_System_Output Particle Unlit Quad.mat", "Hidden/VFX/Gargoyle Swarm/System/Output Particle Unlit Quad" },
    };

    // D 组：三个 VFX 资产（相对 VfxRoot）· 允许悬空的 guid（= R4-05 两个 Texture3D，归 `B109_E19`，⛔ 不归本代理）
    //   `97873f43…` = `SDF_Test` 31×31×64 · `0ca68a91…` = vectorfield 64³（`报告/B108_A14.md` §1）。
    static readonly string[] VfxAssets =
    {
        "ArenaOriginalPrefabs/_shared/Gargoyle Swarm.asset",
        "ArenaOriginalPrefabs/battlearenaaeldari/Aeldari Portal.asset",
        "ArenaOriginalPrefabs/_shared/Aeldari Dust.asset",
    };
    static readonly HashSet<string> AllowedDangling = new HashSet<string>
    {
        "97873f430c01bb8418bc8463d7963d81",
        "0ca68a919af0232488e11af9e15d3695",
    };

    // ---------------------------------------------------------------- 入口
    public static void Run()
    {
        _ok = _bad = _skip = 0;
        SectionA_PrefabsM_Asset();
        SectionB_Shaders();
        SectionC_Materials();
        SectionD_Closure();
        Debug.Log(P + "合计：" + _ok + " 通过 / " + _bad + " 失败" + (_skip > 0 ? " / " + _skip + " 未验到" : ""));
    }

    public static int Pass { get { return _ok; } }
    public static int Fail { get { return _bad; } }

    // ---------------------------------------------------------------- A 组
    static void SectionA_PrefabsM_Asset()
    {
        Debug.Log(P + "==== A 组 · 4 处 VisualEffect.m_Asset ====");
        int totalAsset = 0;
        foreach (var row in Prefabs)
        {
            string rel = row[0], want = row[1], vfxName = row[2], vfxGuid = row[3];
            int wantCount = int.Parse(want);
            string full = Path.Combine(Path.Combine(Application.dataPath, VfxRoot), rel.Replace('/', Path.DirectorySeparatorChar));

            // 前提先断：文件在不在（不在的话下面读文本会抛，宁可红在这一条）
            if (!Check(File.Exists(full), "A0 前提 · prefab 存在：" + rel)) continue;
            string text = File.ReadAllText(full);

            int mAssetCount = Regex.Matches(text, @"(^|\n)\s*m_Asset:").Count;
            Check(mAssetCount == wantCount, "A1 `m_Asset:` 条数 = " + mAssetCount + "（期望 " + wantCount + "）：" + Path.GetFileName(rel));

            int zeroCount = Regex.Matches(text, @"m_Asset:\s*\{fileID:\s*-?\d+,\s*guid:\s*0{32}").Count;
            Check(zeroCount == 0, "A2 零 guid 的 `m_Asset` = " + zeroCount + "（期望 0）：" + Path.GetFileName(rel));

            string exactLine = "m_Asset: {fileID: 2058629509, guid: " + vfxGuid + ", type: 2}";
            int exact = CountOccurrences(text, exactLine);
            Check(exact == wantCount, "A3 `" + exactLine + "` 出现 " + exact + " 次（期望 " + wantCount + "）");

            // 前提：guid 能解析（解析不到就不用往下比名字了）
            string vfxPath = AssetDatabase.GUIDToAssetPath(vfxGuid);
            if (!Check(!string.IsNullOrEmpty(vfxPath), "A4 前提 · VFX 资产 guid 可解析：" + vfxGuid)) continue;

            Check(Path.GetFileNameWithoutExtension(vfxPath) == vfxName,
                  "A5 VFX 资产名 = `" + Path.GetFileNameWithoutExtension(vfxPath) + "`（期望 `" + vfxName + "`）");

            string vfxFull = Path.Combine(Application.dataPath, vfxPath.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar));
            if (Check(File.Exists(vfxFull), "A6 前提 · VFX 资产文件在盘上：" + vfxPath))
            {
                string head = FirstLines(File.ReadAllText(vfxFull), 6);
                // classID 2058629509 = VisualEffectAsset（原版 `Aeldari Portal.asset` 的 YAML 锚点；`.meta` 的 `mainObjectFileID` 同值）
                Check(head.Contains("--- !u!2058629509"), "A6b YAML classID = 2058629509（VisualEffectAsset）：" + Path.GetFileName(vfxPath));
            }
            totalAsset += wantCount;
        }
        Check(totalAsset == 4, "A7 两 prefab 的 `m_Asset` 合计 = " + totalAsset + "（期望 4）");
        Check(AllZeroGuidMAssetCount() == 0, "A8 WarpforgeVFX 全目录零 guid `m_Asset` 计数（期望 0）");
    }

    /// <summary>全目录扫一遍（只扫 `.prefab`/`.asset`），抓「别处还有一处空着」这种漏网。</summary>
    static int AllZeroGuidMAssetCount()
    {
        string root = Path.Combine(Application.dataPath, VfxRoot);
        int n = 0;
        var re = new Regex(@"m_Asset:\s*\{fileID:\s*-?\d+,\s*guid:\s*0{32}");
        foreach (string f in SafeEnumerate(root, new[] { ".prefab", ".asset" }))
            n += re.Matches(File.ReadAllText(f)).Count;
        return n;
    }

    // ---------------------------------------------------------------- B 组
    static void SectionB_Shaders()
    {
        Debug.Log(P + "==== B 组 · 6 个 Hidden/VFX 输出 shader ====");
        foreach (var row in Shaders)
        {
            string name = row[0], file = row[1], blend = row[2];
            string full = Path.Combine(Path.Combine(Application.dataPath, VfxRoot), "Shaders/" + file);
            if (!Check(File.Exists(full), "B0 前提 · shader 文件存在：" + file)) continue;
            string text = File.ReadAllText(full);

            Check(text.Contains("Shader \"" + name + "\" {") || text.Contains("Shader \"" + name + "\"\r\n{")
                  || Regex.IsMatch(text, @"Shader\s+""" + Regex.Escape(name) + @"""\s*\{"),
                  "B1 Shader 名逐字 = `" + name + "`");

            Check(CountOccurrences(text, "Blend " + blend + ", " + blend) == 2,
                  "B2 `Blend " + blend + ", " + blend + "` 出现 2 次（两个 pass 各一）");
            Check(CountOccurrences(text, "ZWrite Off") == 2, "B3 `ZWrite Off` = 2 次");
            Check(CountOccurrences(text, "Cull Off") == 2, "B4 `Cull Off` = 2 次");
            Check(CountOccurrences(text, "ZClip On") == 2, "B5 `ZClip On` = 2 次");
            Check(text.Contains("\"QUEUE\"=\"Transparent+0\""), "B6 SubShader `\"QUEUE\"=\"Transparent+0\"`");
            // 反证：原版 `#pragma use_dxc` 必须已经去掉（Unity 不认这个 pragma，见 `B108_A14` §3·1 ①）
            //   ⚠️ 必须**跳过注释行**再判 —— 文件头那一段「可移植性归一 ①」里就写着这个 pragma 的名字（含 `//` 的不算）。
            Check(!HasCodeToken(text, "#pragma use_dxc"), "B7 无 `#pragma use_dxc`（可移植性归一 ①）");
            // 反证：不能又拆回 `.hlsl` 变体（Gargoyle 原件是 8 个变体，本工程要求就地内联 ⇒ 只许 include 内置 HLSLSupport.cginc）
            Check(LocalIncludeCount(text) == 0, "B8 无本地 `#include`（只许内置 `HLSLSupport.cginc`）");

            // 这一条**只有 Unity 导入过才成立** ⇒ 拿不到就记「未验到」，⛔ 不记通过
            Shader sh = Shader.Find(name);
            if (sh == null)
                Skip("B9 `Shader.Find(\"" + name + "\")` 非空（Unity 未导入 / 导入失败时为空）");
            else
            {
                Check(true, "B9 `Shader.Find(\"" + name + "\")` 非空");
                if (!sh.isSupported) Debug.Log(P + "… 提示：`" + name + "`.isSupported = false（批处理无图形设备时正常，不进计数）");
            }
        }
    }

    static int LocalIncludeCount(string text)
    {
        int n = 0;
        foreach (Match m in Regex.Matches(text, @"^\s*#include\s+""([^""]+)""", RegexOptions.Multiline))
            if (!m.Groups[1].Value.EndsWith("HLSLSupport.cginc")) n++;
        return n;
    }

    /// <summary>只看**非注释行**里有没有这个记号（`//` 开头的行不算 —— 文件头的归一说明里会提到被删掉的 pragma 名）。</summary>
    static bool HasCodeToken(string text, string token)
    {
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.Trim();
            if (line.StartsWith("//", StringComparison.Ordinal)) continue;
            if (line.IndexOf(token, StringComparison.Ordinal) >= 0) return true;
        }
        return false;
    }

    // ---------------------------------------------------------------- C 组
    static void SectionC_Materials()
    {
        Debug.Log(P + "==== C 组 · 6 个输出材质 → shader 绑定 ====");
        foreach (var row in Materials)
        {
            string rel = row[0], shaderName = row[1];
            string full = Path.Combine(Path.Combine(Application.dataPath, VfxRoot), rel.Replace('/', Path.DirectorySeparatorChar));
            if (!Check(File.Exists(full), "C0 前提 · 材质存在：" + Path.GetFileName(rel))) continue;
            string text = File.ReadAllText(full);

            Match m = Regex.Match(text, @"m_Shader:\s*\{fileID:\s*4800000,\s*guid:\s*([0-9a-f]{32}),\s*type:\s*3\}");
            if (!Check(m.Success, "C1 `m_Shader` 是 4800000 + guid + type 3：" + Path.GetFileName(rel))) continue;
            string g = m.Groups[1].Value;
            if (g == "00000000000000000000000000000000") { Check(false, "C2 `m_Shader` 是零 guid（= 悬空）：" + Path.GetFileName(rel)); continue; }

            string p = AssetDatabase.GUIDToAssetPath(g);
            if (!Check(!string.IsNullOrEmpty(p), "C2 `m_Shader` guid 可解析：" + g)) continue;
            // shader 名里带 `/`，映射到文件名是 `Hidden_VFX_…`：只比「解析到的路径确实在 Shaders/ 下、且其 .shader 里的 Shader 名 == 期望名」
            if (!Check(p.StartsWith("Assets/" + VfxRoot + "/Shaders/", StringComparison.Ordinal), "C3 `m_Shader` 解析到本工程 Shaders/ 下（现读 " + p + "）")) continue;
            string pFull = Path.Combine(Application.dataPath, p.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar));
            if (!Check(File.Exists(pFull), "C4 前提 · 解析出的 shader 文件存在：" + p)) continue;
            Check(File.ReadAllText(pFull).Contains("\"" + shaderName + "\""),
                  "C5 解析到的 shader 里 `Shader \"…\"` 名 == 期望的 `" + shaderName + "`（防「同名但另一份」）");

            // `.mat` 的 `m_Name` 必须是 shader 名（运行时 `Material.name` 取的就是它 —— `B108_A14` §3·4 那个哑巴兜底的成因）
            Match nm = Regex.Match(text, @"^\s*m_Name:\s*(.+)$", RegexOptions.Multiline);
            Check(nm.Success && nm.Groups[1].Value.Trim().Trim('\'', '"') == shaderName,
                  "C6 `.mat` 的 `m_Name` == shader 名（`" + (nm.Success ? nm.Groups[1].Value.Trim() : "?") + "`）");
        }
    }

    // ---------------------------------------------------------------- D 组
    static void SectionD_Closure()
    {
        Debug.Log(P + "==== D 组 · 三个 VFX 资产的引用闭包 ====");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dangling = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // guid → 第一个引用它的文件
        var stack = new Stack<string>();
        var guidRe = new Regex(@"guid:\s*([0-9a-f]{32})");

        foreach (string rel in VfxAssets)
        {
            string full = Path.Combine(Path.Combine(Application.dataPath, VfxRoot), rel.Replace('/', Path.DirectorySeparatorChar));
            if (!Check(File.Exists(full), "D0 前提 · VFX 资产存在：" + rel)) continue;
            stack.Push(full);
        }

        int guard = 0;
        while (stack.Count > 0 && guard++ < 400)
        {
            string f = stack.Pop();
            if (!seen.Add(f)) continue;
            string text = File.ReadAllText(f);
            foreach (Match m in guidRe.Matches(text))
            {
                string g = m.Groups[1].Value;
                if (g == "00000000000000000000000000000000") continue;
                if (g.StartsWith("0000000000000000", StringComparison.Ordinal)) continue; // Unity 内置资源
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (string.IsNullOrEmpty(p)) { if (!dangling.ContainsKey(g)) dangling[g] = Path.GetFileName(f); continue; }
                if (!p.StartsWith("Assets/", StringComparison.Ordinal)) continue;      // Packages/ 里的不追
                if (p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue;   // 脚本不追
                string pf = Path.Combine(Application.dataPath, p.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar));
                if (!seen.Contains(pf) && (p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)
                                        || p.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)))
                    stack.Push(pf);
            }
        }

        // 25 = 3 个 VFX 资产 + 6 个输出材质 + 16 个 system 子资产（`.asset`）—— 现读精确值；
        // 阈值取 20 只挡「根本没走进去」这一种（把 BFS 写坏 / 引用被删光），不挡正常增减。
        Check(seen.Count >= 20, "D1 闭包展开到 " + seen.Count + " 个 `.asset/.mat` 节点（现读 25；≥20 才说明真的走完了子资产）");

        int unexpected = 0;
        foreach (var kv in dangling)
        {
            if (AllowedDangling.Contains(kv.Key)) continue;
            unexpected++;
            Check(false, "D2 悬空引用：" + kv.Key + "（被 " + kv.Value + " 引用）—— 不在允许集里");
        }
        if (unexpected == 0)
            Check(true, "D2 悬空只有 R4-05 那两个 Texture3D（现读 " + dangling.Count + " 个；0 个时说明 `B109_E19` 已落）");
    }

    // ---------------------------------------------------------------- 小工具
    static bool Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(P + "✓ " + what); }
        else { _bad++; Debug.LogError(P + "✗ " + what); }
        return ok;
    }

    static void Skip(string what)
    {
        _skip++;
        Debug.Log(P + "… 未验到：" + what);
    }

    static int CountOccurrences(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    static string FirstLines(string text, int n)
    {
        var sb = new StringBuilder();
        using (var sr = new StringReader(text))
            for (int i = 0; i < n; i++) { string l = sr.ReadLine(); if (l == null) break; sb.AppendLine(l); }
        return sb.ToString();
    }

    static IEnumerable<string> SafeEnumerate(string root, string[] exts)
    {
        if (!Directory.Exists(root)) yield break;
        foreach (string f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            bool hit = false;
            foreach (string e in exts) if (f.EndsWith(e, StringComparison.OrdinalIgnoreCase)) { hit = true; break; }
            if (hit) yield return f;
        }
    }
}
