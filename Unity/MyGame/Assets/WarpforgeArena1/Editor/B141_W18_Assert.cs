// B141_W18_Assert.cs — W130-O6（生成器后处理）与 `B124-PREVIEWMTIME`（预览时效守卫第三路）的自检
//
// 判据出处：
//   · `资料/完全复刻/判据/B141_W130_对账_资源.md` §一 16-02 / 16-03 / 16-08 / 16-11 · §二 **W130-O6**
//   · `B124-PREVIEWMTIME`（2026-11-04 裁定采纳）→ `ArenaBuilder.EnsurePreviewFresh` 的第三路
//
// 覆盖四件：
//   ① **第三路的判定式本身**（两态：源比 scene 旧 ⇒ 放行；源比 scene 新 ⇒ 红），用临时文件构造，⛔ 不碰工程内任何文件
//   ② 第三路的**源名单真的生效**（sceneT=2000 时必然判出 ≥2 份：`ArenaBuilder.cs` + 清单生成器）
//   ③ 写口清单**范围正确**：四字段 101 份 / metrics 58 份 / dimension **只 4 份**，且后两族都在四字段名单内；
//      4 份**压缩网格**（`Rock003/004/005` · `stone001`，属 16-16 的 ❓）**绝不能**出现在 dimension 名单里
//   ④ 盘上值是否已经是原版值（⚠️ 这一项**要求先跑过写口**：`ArenaBuilder.BuildAll` 或 `B140_WS_MeshFlags.Run`；
//      没跑过时它是红的，那是事实、不是缺陷 —— 16-08/16-11 本来就必须 Unity 存盘才生效）
//
// 用法（命令行，⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B141_W18_Assert.RunFromCLI -logFile -
//
// ⚠️ 本文件**只做检查**，所有写入都在 `B140_WS_MeshFlags`（唯一写口）里；
//    断言失败一律 `Debug.LogWarning`（红），并在开头先断「前提成立」（名单条数、文件在不在）——
//    「0 条 = 什么都没验到」不能算绿（CLAUDE.md §三 的 `if (x != null) Check(...)` 那条）。

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class B141_W18_Assert
{
    const string T = "[B141_W18] ";
    static int _ok, _bad;

    [MenuItem("Tools/Warforge/B141_W18 自检（网格后处理 + 预览时效守卫）")]
    public static void RunFromCLI() { VerifyAll(); }

    static void Check(bool cond, string what)
    {
        if (cond) { _ok++; Debug.Log(T + "✓ " + what); }
        else { _bad++; Debug.LogWarning(T + "✗ " + what); }
    }

    public static void VerifyAll()
    {
        _ok = _bad = 0;
        CheckSourceGuardTwoState();
        CheckSourceListLive();
        CheckManifestScope();
        CheckWrittenValues();
        Debug.Log(T + $"合计：✓ {_ok} · ✗ {_bad}" + (_bad == 0 ? "（全绿）" : "（有红，见上）"));
    }

    // ── ① / ② 第三路（源码 mtime）──────────────────────────────────────────────

    /// <summary>判定式两态（临时文件，⛔ 不碰工程内文件；不跑 Unity 也能在编辑器里过）。</summary>
    static void CheckSourceGuardTwoState()
    {
        string dir = Path.Combine(Path.GetTempPath(), "wf_b141_w18_assert");
        string probe = Path.Combine(dir, "src_probe.py");
        string missing = Path.Combine(dir, "does_not_exist_9f3a.py");
        var sceneT = new DateTime(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(probe, "# probe\n");

            // 态 1：源比 scene 旧 ⇒ 放行
            File.SetLastWriteTimeUtc(probe, sceneT.AddMinutes(-10));
            int nOld = ArenaBuilder.StaleSources(new[] { probe }, sceneT).Count;
            Check(nOld == 0, "第三路 · 源比 scene **旧** ⇒ 守卫放行（stale=0）");

            // 态 2：源比 scene 新 ⇒ 红，且原因里点明「场景没重建」
            File.SetLastWriteTimeUtc(probe, sceneT.AddMinutes(10));
            var stale = ArenaBuilder.StaleSources(new[] { probe }, sceneT);
            Check(stale.Count == 1 && stale[0].Contains("场景没重建"),
                  "第三路 · 源比 scene **新** ⇒ 守卫拒绝出图（stale=1，原因含「场景没重建」）");

            // 边界：缺文件 ⇒ 不误判成红
            Check(ArenaBuilder.StaleSources(new[] { missing }, sceneT).Count == 0,
                  "第三路 · 源文件缺失 ⇒ 跳过这一路（⛔ 不误判成红）");
        }
        catch (Exception e)
        {
            Check(false, $"第三路两态自检抛了（{e.GetType().Name}: {e.Message}）");
        }
        finally
        {
            try { if (File.Exists(probe)) File.Delete(probe); } catch { /* 临时文件删不掉不影响判定 */ }
        }
    }

    /// <summary>源名单是否真的接上（用「比任何场景都旧」的时间基准，凡存在的源都必须判出来）。</summary>
    static void CheckSourceListLive()
    {
        var stale = ArenaBuilder.StalePreviewSources(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Check(stale.Count >= 2,
              $"第三路名单生效：sceneT=2000 时判出 {stale.Count} 份源比 scene 新（期望 ≥2 = `ArenaBuilder.cs` + 清单生成器）");
    }

    // ── ③ 写口清单范围 ─────────────────────────────────────────────────────────

    static void CheckManifestScope()
    {
        Check(B140_WS_MeshFlags.Count == B140_WS_MeshFlags.ExpectedCount,
              $"四字段名单条数 = 期望（{B140_WS_MeshFlags.Count}/{B140_WS_MeshFlags.ExpectedCount}）");
        Check(B140_WS_MeshFlags.MetricCount > 0,
              $"metrics（16-08）名单非空（{B140_WS_MeshFlags.MetricCount} 份）");
        Check(B140_WS_MeshFlags.DimCount == 4,
              $"dimension（16-11）名单 = 4 份（原版确实是 52 的那四份；现读 {B140_WS_MeshFlags.DimCount}）");

        // 后两族必须落在四字段那 101 份网格之内（⛔ 别冒出清单外的文件）
        var all = new HashSet<string>();
        for (int i = 0; i < B140_WS_MeshFlags.Count; i++) all.Add(B140_WS_MeshFlags.AssetPath(i));
        int outside = 0;
        for (int i = 0; i < B140_WS_MeshFlags.MetricCount; i++) if (!all.Contains(B140_WS_MeshFlags.MetricAssetPath(i))) outside++;
        for (int i = 0; i < B140_WS_MeshFlags.DimCount; i++) if (!all.Contains(B140_WS_MeshFlags.DimAssetPath(i))) outside++;
        Check(outside == 0, $"metrics/dimension 名单全部落在四字段名单内（越界 {outside} 份）");

        // 4 份压缩网格（原版通道全 0、`m_VertexCount: 0`）属 16-16 的 ❓ ⇒ ⛔ 不许进 dimension 名单
        int compressed = 0;
        for (int i = 0; i < B140_WS_MeshFlags.DimCount; i++)
        {
            string p = B140_WS_MeshFlags.DimAssetPath(i);
            if (p.EndsWith("Rock003.asset", StringComparison.Ordinal)
             || p.EndsWith("Rock004.asset", StringComparison.Ordinal)
             || p.EndsWith("Rock005.asset", StringComparison.Ordinal)
             || p.EndsWith("stone001.asset", StringComparison.Ordinal)) compressed++;
        }
        Check(compressed == 0, $"dimension 名单不含 4 份压缩网格（16-16 的 ❓，⛔ 不在本条范围；现读越界 {compressed} 份）");

        // 每份名单文件都得真的在盘上（缺文件 = 名单坏了，不是「原版没有」）
        int missing = 0;
        for (int i = 0; i < B140_WS_MeshFlags.MetricCount; i++)
            if (!File.Exists(B140_WS_MeshFlags.FullPath(B140_WS_MeshFlags.MetricAssetPath(i)))) missing++;
        for (int i = 0; i < B140_WS_MeshFlags.DimCount; i++)
            if (!File.Exists(B140_WS_MeshFlags.FullPath(B140_WS_MeshFlags.DimAssetPath(i)))) missing++;
        Check(missing == 0, $"metrics/dimension 名单里的文件全部存在（缺 {missing} 份）");
    }

    // ── ④ 盘上值（要求先跑过写口）──────────────────────────────────────────────

    static void CheckWrittenValues()
    {
        Debug.Log(T + "④ 盘上值校验（**要求先跑过写口**：`ArenaBuilder.BuildAll` / `B140_WS_MeshFlags.Run`；"
                     + "没跑过时红是事实，不是缺陷 —— 16-08/16-11 必须 Unity 存盘才生效）");
        int bad = B140_WS_MeshFlags.VerifyMetricsAndDims();
        Check(bad == 0, $"盘上 metrics（16-08）+ dimension（16-11）全部 = 原版值（不符 {bad} 份）");
    }
}
