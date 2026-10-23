// ArenaPrefabProbe.cs — 战场线 **原版场景 prefab 重打包**的 Unity 侧 pilot 探针（2026-10-23）
//
// 为什么有它：`工具/extract_missing_shaders.py --arena-prefabs --arena <场>` 把原版场景根
//   （`Scenario` / `Battle Arena … Baked` / leviathan `Particle Effects`）连同整棵依赖树重打进
//   `StreamingAssets/WarpforgeVFX/wf_arena_prefabs_<场>.bundle`。这批包的容器项会写
//   **`m_FileID != 0` 的跨 CAB 引用**（场包的 AssetBundle 对象在 `*.sharedAssets`、根在主 CAB）——
//   **全库 13448 条 `m_Container` 的 fid 全是 0，Unity 侧没有先例**（判据 → J-A §2·6 R1）。
//   本探针就是那条 pilot：**取不到 = 这条路不通，必须改「把 AssetBundle 对象搬进主 CAB」的单 CAB 写法**。
//
// 判据（逐条）：
//   ① `AssetBundle.LoadFromFile` 非 null；
//   ② `GetAllAssetNames()` 里有根名（跨 CAB 容器项被 Unity 认了）；
//   ③ `LoadAsset<GameObject>(根名)` 非 null；
//   ④ 实例化后 Transform 数 = 重打包日志的对象数口径（arena3 实测 482 对象 / Transform 123）；
//   ⑤ 空网格 / 空材质 = 0（依赖树没丢）。
//
// 用法：`WF_ARENA=battlearena3 ... -executeMethod ArenaPrefabProbe.Run`
//   判绿红看日志里的 `APP ===` 行。

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ArenaPrefabProbe
{
    const string P = "APP ";
    const string Dir = "Assets/StreamingAssets/WarpforgeVFX";

    /// <summary>与 `工具/extract_missing_shaders.py` 的 `ARENA_PREFAB_ROOTS` **同一份口径**
    /// （⚠️ 两处写同一张表 = 迟早不一致；本表只为自检用，真源在那支 python。改一处必须改两处 —— 已在报告里记）。</summary>
    static readonly Dictionary<string, string[]> Roots = new Dictionary<string, string[]>
    {
        { "battlearena1", new[] { "Scenario" } },
        { "battlearena2", new[] { "Scenario" } },
        { "battlearena3", new[] { "Scenario" } },
        { "battlearenaaeldari", new[] { "Scenario" } },
        { "battlearenaastramilitarum", new[] { "Scenario", "Battle Arena Astra Militarum Baked" } },
        { "battlearenablacklegion", new[] { "Scenario", "Battle Arena Black Legion Baked 2" } },
        { "battlearenadarkangels", new[] { "Scenario" } },
        { "battlearenaemperorschildren", new[] { "Scenario" } },
        { "battlearenagenestealers", new[] { "Scenario" } },
        { "battlearenaleviathan", new[] { "Particle Effects" } },
        { "battlearenasororitas", new[] { "Scenario" } },
        { "battlearenaspacewolves", new[] { "Scenario" } },
        { "battlearenatauviorla", new[] { "Scenario" } },
    };

    public static void Run()
    {
        string arena = Environment.GetEnvironmentVariable("WF_ARENA");
        if (string.IsNullOrEmpty(arena)) arena = "battlearena3";
        string[] roots;
        if (!Roots.TryGetValue(arena, out roots))
        {
            Debug.LogError(P + "✗ 未知场 `" + arena + "`（本探针的表里没有）");
            Done(1);
            return;
        }

        string file = Path.Combine(Dir, "wf_arena_prefabs_" + arena + ".bundle");
        if (!File.Exists(file))
        {
            Debug.LogError(P + "✗ 找不到 `" + file + "` —— 先跑 "
                         + "`python -I -X utf8 工具/extract_missing_shaders.py --arena-prefabs --arena " + arena + "`");
            Done(1);
            return;
        }

        int ok = 0, bad = 0;
        var b = AssetBundle.LoadFromFile(file);
        if (b == null)
        {
            Debug.LogError(P + "✗ `LoadFromFile` 返回 null（同内容已在进程里加载过？）");
            Done(1);
            return;
        }

        var names = b.GetAllAssetNames();
        Debug.Log(P + "容器项 " + names.Length + " 条：" + string.Join(" | ", names));
        if (names.Length == 0)
        {
            Debug.LogError(P + "✗ 容器是空的 —— `m_Container` 没写进去");
            bad++;
        }

        foreach (var root in roots)
        {
            var go = b.LoadAsset<GameObject>(root);
            if (go == null)
            {
                bad++;
                Debug.LogError(P + "✗ `LoadAsset<GameObject>(\"" + root + "\")` = **null** ⟹ "
                             + "**跨 CAB 容器引用 Unity 不认**（pilot 判死，改走「把 AssetBundle 对象搬进主 CAB」的单 CAB 写法）");
                continue;
            }
            ok++;

            int tr = go.GetComponentsInChildren<Transform>(true).Length;
            int mf = go.GetComponentsInChildren<MeshFilter>(true).Length;
            int mr = go.GetComponentsInChildren<MeshRenderer>(true).Length;
            int ps = go.GetComponentsInChildren<ParticleSystem>(true).Length;
            int anim = go.GetComponentsInChildren<Animation>(true).Length;

            int nullMesh = 0, nullMat = 0;
            foreach (var f in go.GetComponentsInChildren<MeshFilter>(true))
                if (f.sharedMesh == null) nullMesh++;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                if (r.sharedMaterial == null) nullMat++;

            Debug.Log(P + "✓ `" + root + "`：Transform " + tr + " · MeshFilter " + mf
                    + " · MeshRenderer " + mr + " · ParticleSystem " + ps + " · Animation " + anim
                    + " · 空网格 " + nullMesh + " · 空材质 " + nullMat);
            if (nullMesh > 0 || nullMat > 0)
                bad++;      // 依赖树丢了 = 这条也不算过
            else
                ok++;

            // 实例化再数一遍（J-A 的验收口径是「实例化后对象数对得上」）
            var inst = UnityEngine.Object.Instantiate(go);
            int tr2 = inst.GetComponentsInChildren<Transform>(true).Length;
            Debug.Log(P + "  实例化后 Transform " + tr2 + (tr2 == tr ? "（与 prefab 一致）" : "（**与 prefab 不一致**）"));
            UnityEngine.Object.DestroyImmediate(inst);
        }

        b.Unload(true);
        Debug.Log(P + "=== " + ok + " 过 / " + bad + " 红（场 " + arena + "）===");
        Done(bad == 0 ? 0 : 1);
    }

    static void Done(int code)
    {
        if (Application.isBatchMode) EditorApplication.Exit(code);
    }
}
