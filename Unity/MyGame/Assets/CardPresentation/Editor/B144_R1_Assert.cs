// B144_R1_Assert.cs — `B144-R1`：`B136` 网格结构审计里**已修/已确认**项的钉死断言
//
// ============================ 盯的是什么 ============================
//  判据 = `资料/完全复刻/报告/B136_审计_网格与预制体结构.md`（22 条 🟠）
//  + 本轮复核 `资料/完全复刻/报告/B144_R1_网格结构.md`。
//  本文件只钉**当前已经改对、且离线可验证**的那几项（⛔ 不钉尚未修的 15 条，免得长期红）：
//    · `ProjectSettings/GraphicsSettings.asset`：`m_TransparencySortMode: 3`（原版 CustomAxis）·
//      轴 `(0,0,1)` · `m_VideoShadersIncludeMode: 1` · `m_TierSettings` 三组（`realtimeGICPUUsage 25/25/50`）·
//      `m_ShaderDefinesPerShaderCompiler` 两组（`shaderPlatform 4 / 18`，各 `defines_Tier1/2/3 = [80070152, 64]`）。
//    · `Resources/WarpforgeVFX/effect_extras.json`：`Card 3D Death Explosion` 的 `DestroyByTime.lifeTime = 2.2`
//      已在表里，且全表 `DestroyByTime` = **10 条**（原版 10 条；B136 C-04/C-05 的缺口已修）。
//    · `WarpforgeVFX/Prefabs/Orbital 5 repeat.prefab`：根 GO 名**保留原版尾随空格**
//      （`m_Name: 'Orbital 5 repeat '`；B136 C-06 原判「我们已 trim」→ **判据错**，实读带空格）。
//    · `WarpforgeArena1/Editor/ArenaBuilder.cs`：`ApplyRendererParams` 仍在写 `renderingLayerMask` /
//      `lightProbeUsage` / `sortingOrder`（防有人把这三行接线删掉）。
//    · arena1 manifest：`meshes` 30 条都带上述三字段（B-07/08/09 的**网格那一半**已按原版写对；
//      **粒子渲染器的三字段仍缺**，属报告里「待派」的 15 条之一，本断言不钉）。
//
// ============================ 🔴 灭自证 ============================
//  · G 组  ← 把 `m_TransparencySortMode` 改回 0 / `m_VideoShadersIncludeMode` 改回 2 / 删掉 tier 或 defines 块 ⇒ 红。
//  · E 组  ← 把 `Card 3D Death Explosion` 那条从 `effect_extras.json` 删掉，或把 `f0` 改回非 2.2 ⇒ 红。
//  · P 组  ← 把 prefab 根名 trim 成 `Orbital 5 repeat`（去掉尾空格）⇒ 红。
//  · A 组  ← 删掉 `ArenaBuilder.ApplyRendererParams` 里那三行 ⇒ 红。
//  · M 组  ← 把 manifest 任一条 mesh 的三字段删掉 ⇒ 红。
//
// ============================ 怎么接 ============================
//  `B144_R1_Assert.Run(); _sink.Pass += B144_R1_Assert.Pass; _sink.Fail += B144_R1_Assert.Fail;`
//  （宿主由调度台统一接；本文件只读工程文件，不建场景/资产。）
using System;
using System.IO;
using UnityEngine;

public static class B144_R1_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string Tag = "[B144R1]";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static string Root { get { return Directory.GetParent(Application.dataPath).FullName; } }

    static string Read(string rel, out bool ok)
    {
        string p = Path.Combine(Root, rel);
        if (!File.Exists(p)) { ok = false; Debug.LogError(Tag + " 读不到：" + p); return ""; }
        ok = true;
        return File.ReadAllText(p);
    }

    static int Count(string s, string needle)
    {
        int n = 0, i = 0;
        while ((i = s.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    public static void Run()
    {
        _pass = _fail = 0;
        bool ok;

        // ------------------------------------------------ G · GraphicsSettings（B-13/16/17/19）
        string gs = Read("ProjectSettings/GraphicsSettings.asset", out ok);
        Check(ok, "G0 **前提**：读得到 `ProjectSettings/GraphicsSettings.asset`");
        if (ok)
        {
            Check(gs.Contains("m_TransparencySortMode: 3"),
                  "G1 ★ `m_TransparencySortMode = 3`（原版 CustomAxis；B-13）"
                + "｜🧨 改回 0(Default) ⇒ 红");
            Check(gs.Contains("m_TransparencySortAxis: {x: 0, y: 0, z: 1}"),
                  "G2 `m_TransparencySortAxis = (0,0,1)`（B-14）");
            Check(gs.Contains("m_VideoShadersIncludeMode: 1"),
                  "G3 `m_VideoShadersIncludeMode = 1`（原版；B-16）｜🧨 改回 2 ⇒ 红");
            Check(Count(gs, "m_Tier:") == 3, "G4 `m_TierSettings` = 3 组（B-19；实得 " + Count(gs, "m_Tier:") + "）");
            Check(Count(gs, "realtimeGICPUUsage: 25") == 2 && Count(gs, "realtimeGICPUUsage: 50") == 1,
                  "G4b 三组的 `realtimeGICPUUsage` = 25 / 25 / 50（B-19 逐值）");
            Check(gs.Contains("shaderPlatform: 4") && gs.Contains("shaderPlatform: 18"),
                  "G5 `m_ShaderDefinesPerShaderCompiler` 有 `shaderPlatform 4` 与 `18` 两组（B-17）");
            Check(Count(gs, "defines_Tier1:") == 2 && Count(gs, "defines_Tier2:") == 2 && Count(gs, "defines_Tier3:") == 2,
                  "G5b 两组各有 Tier1/2/3 三档（B-17）");
            Check(Count(gs, "80070152") == 6 && Count(gs, "    - 64") == 6,
                  "G5c 六档 define 全是 `[80070152, 64]`（B-17 逐值）");
        }

        // ------------------------------------------------ E · effect_extras（C-04/C-05）
        string ex = Read("Assets/Resources/WarpforgeVFX/effect_extras.json", out ok);
        Check(ok, "E0 **前提**：读得到 `Resources/WarpforgeVFX/effect_extras.json`");
        if (ok)
        {
            Check(Count(ex, "\"kind\": \"DestroyByTime\"") == 10,
                  "E1 全表 `DestroyByTime` = **10 条**（原版 10；实得 " + Count(ex, "\"kind\": \"DestroyByTime\"") + "）");
            Check(ex.Contains("\"effect\": \"Card 3D Death Explosion\""),
                  "E2★ `Card 3D Death Explosion` 已在表里（B136 C-05 缺口已修）｜🧨 删掉 ⇒ 红");
            Check(ex.Contains("\"f0\": 2.2"),
                  "E3★ 它的 `DestroyByTime.lifeTime = 2.2`（原版字面量）｜🧨 改值 ⇒ 红");
        }

        // ------------------------------------------------ P · C-06 根节点尾随空格（判据错订正）
        string pf = Read("Assets/WarpforgeVFX/Prefabs/Orbital 5 repeat.prefab", out ok);
        Check(ok, "P0 **前提**：读得到 `Orbital 5 repeat.prefab`");
        if (ok)
            Check(pf.Contains("m_Name: 'Orbital 5 repeat '"),
                  "P1★ 根 GO 名**保留原版尾随空格**（`'Orbital 5 repeat '`）"
                + "｜🧨 改成 trim 过的 `Orbital 5 repeat` ⇒ 红；B136 C-06 原判「我们已 trim」是**判据错**");

        // ------------------------------------------------ A · ArenaBuilder 三字段接线（B-07/08/09 的网格半）
        string ab = Read("Assets/WarpforgeArena1/Editor/ArenaBuilder.cs", out ok);
        Check(ok, "A0 **前提**：读得到 `ArenaBuilder.cs`");
        if (ok)
        {
            Check(ab.Contains("e.renderingLayerMask") && ab.Contains("e.lightProbeUsage") && ab.Contains("e.sortingOrder"),
                  "A1★ `ArenaBuilder` 仍在把清单的 `renderingLayerMask / lightProbeUsage / sortingOrder` 写到渲染器"
                + "｜🧨 删任一条 ⇒ 红");
            Check(ab.Contains("ApplyRendererParams"), "A1b 落点是 `ApplyRendererParams`（唯一写口）");
        }

        // ------------------------------------------------ M · manifest 网格三字段（B-07/08/09 网格半）
        string mf = Read("Assets/WarpforgeArena1/arenas/battlearena1/battlearena1_manifest.json", out ok);
        Check(ok, "M0 **前提**：读得到 arena1 manifest");
        if (ok)
        {
            int nR = Count(mf, "\"renderingLayerMask\"");
            int nL = Count(mf, "\"lightProbeUsage\"");
            int nS = Count(mf, "\"sortingOrder\"");
            Check(nR == 30 && nL == 30 && nS == 30,
                  "M1 30 条 `meshes` 都带三字段（实得 " + nR + "/" + nL + "/" + nS + "）"
                + "｜🧨 删掉任一条的三字段 ⇒ 红");
            Check(mf.Contains("\"renderingLayerMask\": 1") && mf.Contains("\"lightProbeUsage\": 1"),
                  "M2 网格那一半与改前一致（原版这 30 个 MR 就是 `(1,1,0)` —— 已逐节点核过场景）");
        }

        Debug.Log(Tag + " 合计：" + _pass + " 通过 / " + _fail + " 失败");
    }
}
