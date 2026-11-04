// B144_R1_Manifest_Assert.cs — `B144-R1`：B136 B-07/08/09 **粒子那一半**（清单三字段）的钉死断言
//
// ============================ 盯的是什么 ============================
//  判据 = `资料/完全复刻/报告/B136_审计_网格与预制体结构.md`（B-07/08/09）
//        + `资料/完全复刻/报告/B144_R1_网格结构.md`（§3·2 / §四 #1 / §七）。
//  B136 原判「31 个可比渲染器里 26 个 = All、13 个 = BlendedProbes、1 个 = sortingOrder 1」——
//  2026-11-05 复核把范围缩到**粒子渲染器**：`meshes` 那 30 条早就 `(1,1,0)`（与原版同名 MR 逐节点一致 ✅），
//  真正落默认的是 **`particles[]` 里的 PSR**。本轮把生成器补成逐颗抽三字段（`工具/scripts快照/gen_unity_arena_manifest.py`）。
//
// ============================ 期望值从哪来（⛔ 不是从被测代码抄） ============================
//  原版 `bundle_scenes_scenes_battlearena1/ParticleSystemRenderer/*.json` **60 个 PSR 逐颗实读**：
//    (1, 0, 0) × 32 · (All, 1, 0) × 14 · (All, 0, 0) × 12 · (All, 0, 1) × 2
//  清单只收其中 **34 颗**（其余是 UI / 无贴图等，生成器本来就排除）⇒ 清单侧期望分布：
//    (All, 1, 0) × 14 · (All, 0, 0) × 12 · (1, 0, 0) × 6 · (All, 0, 1) × 2
//  并要求两颗 `Shockwave`（`Particles/Battle/TinyExplosion_Far/Shockwave`）逐字为 `(All, 0, 1)`。
//
// ============================ 🔴 灭自证 ============================
//  · P1/P2：把生成器那三行删掉（或忘了重生成）⇒ 清单缺键 ⇒ 红（现在 34/34 都有键）。
//  · P3：把 28 个 `4294967295` 任一个改回 `1` ⇒ 计数红。
//  · P4：把 14 个 `lightProbeUsage=1` 改回 0 ⇒ 红（这正是原来 34 颗全落默认时的形状）。
//  · P5：把 `Shockwave` 的 `sortingOrder` 改回 0 ⇒ 红。
//  · ⛔ 这三项都不是「拿我们的常量证明我们的常量」：期望值来自**原版 PSR JSON 的逐颗读数**，
//    不是生成器写出来的值；生成器改了，这里不会跟着变。
//
// ============================ 怎么接 ============================
//  `B144_R1_Manifest_Assert.Run(); _sink.Pass += ...; _sink.Fail += ...;`（宿主由调度台统一接）
//  只读工程文件；不建场景/资产。
using System;
using System.IO;
using UnityEngine;

public static class B144_R1_Manifest_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string Tag = "[B144R1M]";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static string Root { get { return Directory.GetParent(Application.dataPath).FullName; } }

    static string Read(string rel)
    {
        string p = Path.Combine(Root, rel);
        if (!File.Exists(p)) { Debug.LogError(Tag + " 读不到：" + p); return null; }
        return File.ReadAllText(p);
    }

    static int Count(string s, string needle)
    {
        int n = 0, i = 0;
        while ((i = s.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    // 只取 `"particles": [ ... ]` 那一段（顶层数组缩进两个空格 ⇒ 收尾是 "\n  ],"）。
    // ⚠️ `meshes` 也有同样的三个键、且值多为 (1,1,0) ⇒ 不切片会把 30 条网格算进来。
    static string ParticlesJson(string js)
    {
        int i = js.IndexOf("\"particles\": [", StringComparison.Ordinal);
        if (i < 0) return "";
        int j = js.IndexOf("\n  ],", i, StringComparison.Ordinal);
        return j < 0 ? js.Substring(i) : js.Substring(i, j - i);
    }

    static readonly string[] Arenas = {
        "battlearena1", "battlearena2", "battlearena3", "battlearenaaeldari",
        "battlearenaastramilitarum", "battlearenablacklegion", "battlearenadarkangels",
        "battlearenaemperorschildren", "battlearenagenestealers", "battlearenaleviathan",
        "battlearenasororitas", "battlearenaspacewolves", "battlearenatauviorla",
    };

    public static void Run()
    {
        _pass = _fail = 0;

        // ------------------------------------------------ A · 全 13 场：每个 particle 条目都有三键
        int arenasOk = 0, totalAll = 0, totalLp1 = 0, totalSort1 = 0, totalEntries = 0;
        foreach (var a in Arenas)
        {
            string rel = "Assets/WarpforgeArena1/arenas/" + a + "/" + a + "_manifest.json";
            string js = Read(rel);
            if (js == null) { Check(false, "A0 读不到 " + a + " manifest"); continue; }
            string pj = ParticlesJson(js);
            int nE = Count(pj, "\"go\":");
            int nR = Count(pj, "\"renderingLayerMask\"");
            int nL = Count(pj, "\"lightProbeUsage\"");
            int nS = Count(pj, "\"sortingOrder\"");
            bool ok = nE > 0 && nR == nE && nL == nE && nS == nE;
            Check(ok, "A1 " + a + "：粒子 " + nE + " 条全带三键（实得 " + nR + "/" + nL + "/" + nS + "）"
                    + "｜🧨 生成器少抽一行 ⇒ 红");
            if (ok) arenasOk++;
            totalEntries += nE;
            totalAll += Count(pj, "\"renderingLayerMask\": 4294967295");
            totalLp1 += Count(pj, "\"lightProbeUsage\": 1");
            totalSort1 += Count(pj, "\"sortingOrder\": 1");
        }
        Check(arenasOk == 13, "A2 13/13 场清单的粒子侧都带三键（实得 " + arenasOk + "，共 " + totalEntries + " 颗）");
        Check(totalAll > 0 && totalLp1 > 0 && totalSort1 > 0,
              "A3 13 场粒子汇总有非默认值（All " + totalAll + " · lightProbe1 " + totalLp1
            + " · sortingOrder1 " + totalSort1 + "）｜🧨 三行删掉 ⇒ 全 0 ⇒ 红");

        // ------------------------------------------------ P · arena1 精确分布（原版字面量）
        string m = Read("Assets/WarpforgeArena1/arenas/battlearena1/battlearena1_manifest.json");
        Check(m != null, "P0 **前提**：读得到 battlearena1 manifest");
        if (m != null)
        {
            string pj = ParticlesJson(m);
            Check(Count(pj, "\"go\":") == 34, "P0b arena1 清单粒子 = 34 条（实得 " + Count(pj, "\"go\":") + "）");
            Check(Count(pj, "\"renderingLayerMask\": 4294967295") == 28,
                  "P1 arena1 `renderingLayerMask = All` 28 条（原版 60 PSR 里 28 颗 All；清单 34 颗全保留）"
                + "｜🧨 任一条改回 1 ⇒ 红；实得 " + Count(pj, "\"renderingLayerMask\": 4294967295"));
            Check(Count(pj, "\"renderingLayerMask\": 1,") == 6,
                  "P2 arena1 `renderingLayerMask = 1` 6 条（原版 (1,0,0) 32 颗里进清单的那 6 颗）"
                + "；实得 " + Count(pj, "\"renderingLayerMask\": 1,"));
            Check(Count(pj, "\"lightProbeUsage\": 1") == 14,
                  "P3 arena1 `lightProbeUsage = 1 (BlendedProbes)` 14 条（原版 14 颗进清单的都在）"
                + "｜🧨 改回 0 ⇒ 红；实得 " + Count(pj, "\"lightProbeUsage\": 1"));
            Check(Count(pj, "\"sortingOrder\": 1") == 2,
                  "P4 arena1 `sortingOrder = 1` 2 条（= 两颗 `Shockwave`）"
                + "｜🧨 改回 0 ⇒ 红；实得 " + Count(pj, "\"sortingOrder\": 1"));
            // 两颗 Shockwave 的完整三元组（原版 PSR 实读：All / 0 / 1）
            string shock = "\"go\": \"Shockwave\",";
            int si = pj.IndexOf(shock, StringComparison.Ordinal);
            bool shockOk = false;
            if (si >= 0)
            {
                int next = pj.IndexOf("\"go\":", si + shock.Length, StringComparison.Ordinal);
                int segEnd = next > si ? next : pj.Length;
                string seg = pj.Substring(si, segEnd - si);
                shockOk = seg.Contains("\"renderingLayerMask\": 4294967295")
                       && seg.Contains("\"lightProbeUsage\": 0")
                       && seg.Contains("\"sortingOrder\": 1");
            }
            Check(shockOk, "P5 `Shockwave` 条目逐字为 (All, 0, 1)（原版 PSR 实读）｜🧨 任一项改默认 ⇒ 红");
        }

        Debug.Log(Tag + " 合计：" + _pass + " 通过 / " + _fail + " 失败");
    }
}
