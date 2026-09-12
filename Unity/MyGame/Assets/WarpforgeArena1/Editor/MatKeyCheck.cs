// MatKeyCheck.cs — 查 binder 的材质缓存键有没有串：同名同 shader 但**属性不同**的材质有多少
//
// 为什么查这个：`WarpforgeEffectBinder.Cache` 的键是 `材质名 + "|" + 原 shader 名`。
// 而「Glow Additive」「Smoke」「Glow」这类名字在几百个效果里重复出现 —— 如果它们的
// **属性值不一样**，先建的那个材质就会顶替掉后面所有同名的：别的效果用的其实是别人的材质。
//
// 这能同时解释两件事：
//   · 只改了一处（精灵导出），却有 400+ 个效果的 |ln| 动了 —— 因为重导后 prefab 的
//     读取顺序变了，谁先建材质就变了
//   · E 组「亮度/密度不对」里那一大片方向不一的偏差
//
// 纯读资产、不渲染，几秒钟跑完。
//
// 用法：Unity.exe -batchmode -quit -projectPath ... -executeMethod MatKeyCheck.Run -logFile -
//   筛输出：grep "^MK " d:/4/_tmp_view/matkey.log
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using WarpforgeVFX;

public static class MatKeyCheck
{
    const string P = "MK ";
    const string PrefabDir = "Assets/WarpforgeVFX/Prefabs";

    public static void Run()
    {
        var files = Directory.GetFiles(PrefabDir, "*.prefab").OrderBy(f => f).ToList();
        Debug.Log(P + $"=== 材质缓存键检查：{files.Count} 个 prefab ===");

        // 键 → 指纹 → 用到它的效果
        var keys = new Dictionary<string, Dictionary<string, List<string>>>();
        int total = 0;

        foreach (var f in files)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(f.Replace('\\', '/'));
            if (go == null) continue;
            var b = go.GetComponent<WarpforgeEffectBinder>();
            if (b == null || b.materials == null) continue;

            foreach (var d in b.materials)
            {
                if (d == null || string.IsNullOrEmpty(d.name)) continue;
                total++;
                string key = d.name + "|" + d.shader;
                string fp = Fingerprint(d);
                if (!keys.TryGetValue(key, out var byFp)) keys[key] = byFp = new Dictionary<string, List<string>>();
                if (!byFp.TryGetValue(fp, out var uses)) byFp[fp] = uses = new List<string>();
                if (uses.Count < 4 && !uses.Contains(go.name)) uses.Add(go.name);
            }
        }

        var collided = keys.Where(kv => kv.Value.Count > 1).ToList();
        int affectedEffects = collided.SelectMany(kv => kv.Value.Values.SelectMany(v => v)).Distinct().Count();
        Debug.Log(P + $"材质定义 {total} 条，不同键 {keys.Count} 个");
        Debug.Log(P + $"⚠️ 同一个键（名字|shader）却有**不同属性值**的：{collided.Count} 个键");
        Debug.Log(P + $"   这些键会互相顶替 —— 涉及 {affectedEffects} 个效果（按每个键前 4 个统计，实际更多）");

        Debug.Log(P + "  —— 按「同名但属性不同的份数」排序，前 20 个：");
        foreach (var kv in collided.OrderByDescending(kv => kv.Value.Count).Take(20))
        {
            Debug.Log(P + $"    {kv.Key,-58} 有 {kv.Value.Count} 种不同属性  例："
                        + string.Join(", ", kv.Value.Values.SelectMany(v => v).Take(3)));
        }

        // 顺带统计：有多少材质名字被复用得最凶
        var byName = keys.Keys.GroupBy(k => k.Split('|')[0])
                              .OrderByDescending(g => g.Count()).Take(10);
        Debug.Log(P + "  —— 名字被复用最多的：");
        foreach (var g in byName)
            Debug.Log(P + $"    {g.Key,-40} {g.Count()} 个不同 shader 变体");
    }

    /// <summary>属性指纹：把 float / color / texture / 关键字都算进去（顺序无关）</summary>
    static string Fingerprint(WFMatDef d)
    {
        var sb = new StringBuilder();
        sb.Append("rq=").Append(d.renderQueue).Append(';');
        if (d.floatNames != null)
        {
            var pairs = Enumerable.Range(0, Mathf.Min(d.floatNames.Length, d.floatVals.Length))
                                  .Select(i => $"{d.floatNames[i]}={d.floatVals[i]:G7}")
                                  .OrderBy(x => x);
            sb.Append(string.Join(",", pairs)).Append(';');
        }
        if (d.colorNames != null)
        {
            var pairs = Enumerable.Range(0, Mathf.Min(d.colorNames.Length, d.colorVals.Length))
                                  .Select(i => $"{d.colorNames[i]}={d.colorVals[i]:G5}")
                                  .OrderBy(x => x);
            sb.Append(string.Join(",", pairs)).Append(';');
        }
        if (d.texNames != null)
        {
            var pairs = Enumerable.Range(0, Mathf.Min(d.texNames.Length, d.texVals.Length))
                                  .Select(i => $"{d.texNames[i]}={(d.texVals[i] != null ? d.texVals[i].name : "null")}")
                                  .OrderBy(x => x);
            sb.Append(string.Join(",", pairs)).Append(';');
        }
        if (d.keywords != null) sb.Append(string.Join(",", d.keywords.OrderBy(x => x)));
        return sb.ToString();
    }
}
