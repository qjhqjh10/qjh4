// EffectLibraryBuilder.cs — 把 `数据/游戏数据/effect_index.json` 变成工程里的效果库资产
//
// 分工：Python（工具/gen_effect_index.py）负责 join 三份数据（prefab 目录 + animfx 组件参数 +
// 还原台账），产出**扁平数组**的 JSON —— Unity 的 JsonUtility 读不了字典，只能读固定字段和数组。
// 这里负责 Unity 那一半：把 JSON 里的路径解析成 prefab 引用、补上只有 Unity 才算得出来的
// 「粒子自然时长 / 有没有循环发射器」，然后存成 ScriptableObject。
//
// 为什么要单独算「自然时长」：63 个效果**原版就没有 AnimFXController**（自己不会销毁），
// 它们的兜底寿命只能从粒子系统本身推。循环发射器推不出来（永远播不完），只能标记出来。
//
// 用法（菜单）：Tools > Warpforge > 生成效果库
// 用法（CLI）：
//   unset ELECTRON_RUN_AS_NODE && Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod EffectLibraryBuilder.Run -logFile -
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using WarpforgeVFX;

public static class EffectLibraryBuilder
{
    const string P = "WFLIB ";

    const string IndexJson = @"d:/4/Unity/数据/游戏数据/effect_index.json";
    const string OutAsset = "Assets/Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset";
    const string ReportPath = "Assets/WarpforgeVFX/效果库报告.tsv";

    // 只装一部分效果时填这里（子串匹配，空 = 全量）。做「打包后」验证时用得上 ——
    // 全量库会把 958 个 prefab + 1.8 GB 贴图全拖进构建，光验证用不着那么大。
    static readonly string[] NameFilter = { };
    const int Limit = 0;                                            // 0 = 不限

    [MenuItem("Tools/Warpforge/生成效果库")]
    public static void Run()
    {
        Debug.Log(P + "=== 生成效果库 开始 ===");

        if (!File.Exists(IndexJson))
        {
            Debug.LogError(P + $"没有 {IndexJson} —— 先跑 工具/gen_effect_index.py");
            return;
        }
        var doc = JsonUtility.FromJson<IndexDoc>(File.ReadAllText(IndexJson));
        if (doc == null || doc.effects == null || doc.effects.Length == 0)
        {
            Debug.LogError(P + "索引解析失败或为空");
            return;
        }
        Debug.Log(P + $"索引 {doc.count} 个效果（生成于 {doc.generated}，带控制器 {doc.withController} 个）");

        var sel = doc.effects.AsEnumerable();
        if (NameFilter.Length > 0)
            sel = sel.Where(e => NameFilter.Any(k => e.name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0));
        if (Limit > 0) sel = sel.Take(Limit);
        var list = sel.ToList();

        var entries = new List<WFEffectEntry>(list.Count);
        var missing = new List<string>();
        var sb = new StringBuilder();
        sb.AppendLine("效果名\t判定\t置信度\t亮度比\tdestroyTime\texitDestroyTime\tpreventDestroy\t控制器数\t自然时长\t循环发射器\t寿命风险\t模块\tprefab");

        int loopRisk = 0;
        foreach (var e in list)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(e.prefab);
            if (prefab == null) { missing.Add(e.name); continue; }

            float natural = -1f;
            bool loops = false;
            MeasureParticles(prefab, out natural, out loops);

            // 寿命风险 = 原版不自己销毁（或压根没有控制器）**而且**有循环发射器 ——
            // 这种没人收就永远留在场景里，是内存泄漏的候选。播放器只能给调用方一个抓手，
            // 真正的收尾得靠牌局代码调 Kill()。
            bool risk = loops && (e.preventDestroy || !e.hasController);
            if (risk) loopRisk++;

            entries.Add(new WFEffectEntry
            {
                name = e.name,
                prefab = prefab,
                destroyTime = e.destroyTime,
                exitDestroyTime = e.exitDestroyTime,
                preventDestroy = e.preventDestroy,
                natural = natural,
                loops = loops,
                verdict = ShortVerdict(e.verdict),
                confidence = e.confidence,
                ratio = e.ratio,
            });

            sb.AppendLine(string.Join("\t", new[] {
                e.name, ShortVerdict(e.verdict), e.confidence,
                e.ratio >= 0f ? e.ratio.ToString("F2") : "",
                e.destroyTime >= 0f ? e.destroyTime.ToString("F2") : "",
                e.exitDestroyTime >= 0f ? e.exitDestroyTime.ToString("F2") : "",
                e.preventDestroy ? "1" : "",
                e.controllers.ToString(),
                natural >= 0f ? natural.ToString("F2") : "",
                loops ? "1" : "",
                risk ? "★" : "",
                e.modules, e.prefab,
            }));
        }

        if (missing.Count > 0)
        {
            Debug.LogWarning(P + $"索引里有 {missing.Count} 个效果在工程里找不到 prefab（已跳过）："
                             + string.Join(", ", missing.Take(8)) + (missing.Count > 8 ? " …" : ""));
        }

        var lib = AssetDatabase.LoadAssetAtPath<WarpforgeEffectLibrary>(OutAsset);
        bool isNew = lib == null;
        if (isNew) lib = ScriptableObject.CreateInstance<WarpforgeEffectLibrary>();
        lib.entries = entries.ToArray();
        lib.generated = $"{DateTime.Now:yyyy-MM-dd HH:mm}　来源 {IndexJson}，"
                      + (NameFilter.Length > 0 ? $"筛选 {string.Join("/", NameFilter)}，" : "")
                      + $"共 {entries.Count} 个";
        lib.Rebuild();

        var dir = Path.GetDirectoryName(OutAsset).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(dir))
        {
            var parent = Path.GetDirectoryName(dir).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder("Assets", Path.GetFileName(parent));
            AssetDatabase.CreateFolder(parent, Path.GetFileName(dir));
        }
        if (isNew) AssetDatabase.CreateAsset(lib, OutAsset);
        else EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        File.WriteAllText(ReportPath, sb.ToString());

        // ⚠️ 自检：生成完必须确认每条都有 prefab。prefab 引用为空 = 库废了，
        //    而它**不会报错**，只会让所有特效一声不响地不播（踩过：重导 prefab 后
        //    GameObject 的 fileID 全变了，而 GUID 还对得上，所以特别难发现）。
        int nullPrefab = entries.Count(x => x.prefab == null);
        if (nullPrefab > 0)
            Debug.LogError(P + $"⚠️⚠️ {nullPrefab} 条效果的 prefab 引用是空的 —— 这个库不能用，"
                            + "特效会静默地不播。重跑一次本工具，还不行就查 Prefabs/ 目录。");

        int noCtrl = entries.Count(x => x.destroyTime < 0f);
        int noLifetime = entries.Count(x => x.destroyTime < 0f && x.natural < 0f && !x.loops);
        Debug.Log(P + $"=== 结束：{entries.Count} 个效果 → {OutAsset} ===");
        Debug.Log(P + $"  原版没有控制器（寿命靠兜底）{noCtrl} 个；其中连自然时长都算不出的 {noLifetime} 个");
        Debug.Log(P + $"  寿命风险（循环发射器 + 原版不管销毁）{loopRisk} 个 —— 详见 {ReportPath} 的「寿命风险」列");
    }

    /// <summary>把判定长句收成台账里的短代号，方便在 Inspector 里一眼看。</summary>
    static string ShortVerdict(string v)
    {
        if (string.IsNullOrEmpty(v)) return "";
        if (v.StartsWith("对得上")) return "Z";
        if (v.StartsWith("偏亮")) return "E+";
        if (v.StartsWith("偏暗")) return "E-";
        if (v.StartsWith("只有原版")) return "C";
        if (v.StartsWith("只有导出")) return "D";
        if (v.StartsWith("时序")) return "T";
        if (v.StartsWith("两边全程空")) return "W";
        return v;
    }

    /// <summary>粒子的自然时长：不循环时 = max(单次时长 + 最长寿命 + 延迟)。
    /// 循环发射器永远播不完，推不出「自然结束」，只能把 loops 标出来让调用方自己收。</summary>
    static void MeasureParticles(GameObject prefab, out float natural, out bool loops)
    {
        natural = -1f;
        loops = false;
        ParticleSystem[] systems;
        try { systems = prefab.GetComponentsInChildren<ParticleSystem>(true); }
        catch { return; }

        foreach (var ps in systems)
        {
            if (ps == null) continue;
            if (ps.main.loop) { loops = true; continue; }
            float life = ps.main.duration
                       + ps.main.startLifetime.constantMax
                       + ps.main.startDelay.constantMax;
            if (life > natural) natural = life;
        }
    }

    [Serializable]
    class IndexDoc
    {
        public string generated;
        public string prefabDir;
        public int count;
        public int withController;
        public string source;
        public IndexEntry[] effects;
    }

    [Serializable]
    class IndexEntry
    {
        public string name;
        public string prefab;
        public bool hasController;
        public float destroyTime;
        public float exitDestroyTime;
        public bool preventDestroy;
        public string modules;
        public int controllers;
        public string verdict;
        public string confidence;
        public float ratio;
    }
}
