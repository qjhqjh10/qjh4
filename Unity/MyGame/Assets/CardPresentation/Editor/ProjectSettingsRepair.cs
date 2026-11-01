// ProjectSettingsRepair.cs — 2026-11-01（第一百一十三会话 · `B113W`）
//
// 为什么要有它：`B-11`（URP 两份资产的 7 处悬空）与 `B-13`（`ProjectSettings/VFXManager.asset` 一个键都没写）
// 都**不能靠「打开工程 Unity 会自动修」** —— 实测多次批处理跑过，两个文件的 mtime 一次都没动。
// 所以这里给**显式入口 + 读回断言**：改完立刻把结果读回来打出来，⛔ 不靠「应该会好」。
//
// 用法（批处理，串行）：
//   Unity -batchmode -quit -projectPath <工程> -executeMethod ProjectSettingsRepair.RunFromCLI -logFile -
//
// ⚠️ 只改两件事：URP 的两个 `.asset` 里**指向包内资源**的引用、`VFXManager.asset` 的 compute shader 槽。
//    不动任何游戏资产、不动场景。

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ProjectSettingsRepair
{
    const string CorePkg = "Packages/com.unity.render-pipelines.core/";
    const string VfxPkg = "Packages/com.unity.visualeffectgraph/";
    const string RendererAsset = "Assets/Settings/PC_Renderer.asset";
    const string VolumeProfileAsset = "Assets/Settings/DefaultVolumeProfile.asset";
    const string VfxManagerAsset = "ProjectSettings/VFXManager.asset";

    public static void RunFromCLI()
    {
        Debug.Log("[PSR] ===== 开始 =====");
        int a = RepairUrpProbeVolume();
        int b = RepairVfxManager();
        int c = RepairVolumeProfile();
        Debug.Log("[PSR] ===== 结束：URP 补 " + a + " 处 · VFXManager 补 " + b + " 处 · VolumeProfile 清 " + c + " 条 =====");
    }

    // -------------------------------------------------------------- B-11 的另一半
    /// <summary>把 `DefaultVolumeProfile` 里**类不存在**的 Volume 组件剔掉。
    /// <para>2026-11-01 决策：`OutlineVolumeComponent` / `OasisFogVolumeComponent` / `TestAnimationCurveVolumeComponent` / `TestVolume`
    /// 这 4 个类的 `.cs` 在**我们工程、原版反编译产物里都没有**（属于我们没装的包/原版自带测试件）⇒ 引用永远解析不到。
    /// 它们在 Unity 加载时**本来就被丢成 null**（惰性），删掉只是把「已死的东西」从资产里去掉、让闸能绿。
    /// ⛔ 不是「决定不复刻它们」—— 真要复刻得先有那 4 个类的实现，另立账。</para></summary>
    static int RepairVolumeProfile()
    {
        var p = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(VolumeProfileAsset);
        if (p == null) { Debug.LogWarning("[PSR] 读不到 " + VolumeProfileAsset); return 0; }
        int dead = 0;
        for (int i = p.components.Count - 1; i >= 0; i--)
            if (p.components[i] == null) { p.components.RemoveAt(i); dead++; }
        // ⚠️ 2026-11-01 实测：那 4 条在 **Unity 加载时就被丢掉**（内存里 19 个组件、0 个 null），
        //   但**文件 YAML 里还留着** ⇒ 必须**强制存一次盘**把文件刷成内存真值，否则 guid 永远悬空。
        EditorUtility.SetDirty(p);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(VolumeProfileAsset, ImportAssetOptions.ForceUpdate);
        var re = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(VolumeProfileAsset);
        int stillNull = 0;
        for (int i = 0; i < re.components.Count; i++) if (re.components[i] == null) stillNull++;
        Debug.Log("[PSR] VolumeProfile：删 " + dead + " 条 · 剩余组件 " + re.components.Count + " · 仍为 null " + stillNull);
        return dead;
    }

    // ------------------------------------------------------------------ B-11
    static int RepairUrpProbeVolume()
    {
        var data = AssetDatabase.LoadAssetAtPath<Object>(RendererAsset);
        if (data == null)
        {
            Debug.LogError("[PSR] 找不到 " + RendererAsset);
            return 0;
        }

        var map = new (string prop, string asset, System.Type type)[]
        {
            ("probeVolumeDebugShader",               CorePkg + "Runtime/Debug/ProbeVolumeDebug.shader", typeof(Shader)),
            ("probeVolumeFragmentationDebugShader",  CorePkg + "Runtime/Debug/ProbeVolumeFragmentationDebug.shader", typeof(Shader)),
            ("probeVolumeOffsetDebugShader",         CorePkg + "Runtime/Debug/ProbeVolumeOffsetDebug.shader", typeof(Shader)),
            ("probeVolumeSamplingDebugShader",       CorePkg + "Runtime/Debug/ProbeVolumeSamplingDebug.shader", typeof(Shader)),
            ("probeSamplingDebugMesh",               CorePkg + "Runtime/Debug/ProbeSamplingDebugMesh.fbx", typeof(Mesh)),
            ("probeSamplingDebugTexture",            CorePkg + "Runtime/Debug/ProbeVolumeNumbersDisplayTex.png", typeof(Texture2D)),
            ("probeVolumeBlendStatesCS",             CorePkg + "Runtime/Lighting/ProbeVolume/ProbeVolumeBlendStates.compute", typeof(ComputeShader)),
        };

        var so = new SerializedObject(data);
        int fixedCount = 0;
        foreach (var m in map)
        {
            var obj = AssetDatabase.LoadAssetAtPath(m.asset, m.type);
            if (obj == null)
            {
                Debug.LogWarning("[PSR] 包内没找到 " + m.asset + " ⇒ 跳过 " + m.prop);
                continue;
            }
            var p = FindByName(so, m.prop);
            if (p == null)
            {
                Debug.LogWarning("[PSR] 序列化属性里没有 " + m.prop + " ⇒ 跳过（URP 版本可能改了字段名）");
                continue;
            }
            if (p.objectReferenceValue == obj) continue;
            p.objectReferenceValue = obj;
            fixedCount++;
        }
        if (fixedCount > 0)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }

        // 读回：逐项核「现在指向的对象是不是包内那一份」
        var so2 = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(RendererAsset));
        int ok = 0, bad = 0;
        foreach (var m in map)
        {
            var p = FindByName(so2, m.prop);
            if (p == null) continue;
            var want = AssetDatabase.LoadAssetAtPath(m.asset, m.type);
            bool good = p.objectReferenceValue != null && p.objectReferenceValue == want;
            if (good) ok++; else { bad++; Debug.LogError("[PSR] 读回不符：" + m.prop + " 现值=" + (p.objectReferenceValue == null ? "<null>" : p.objectReferenceValue.name)); }
        }
        Debug.Log("[PSR] URP 读回：符合 " + ok + " / 不符 " + bad + "（共 " + map.Length + " 项）");
        return fixedCount;
    }

    // ------------------------------------------------------------------ B-13
    static int RepairVfxManager()
    {
        var all = AssetDatabase.LoadAllAssetsAtPath(VfxManagerAsset);
        if (all == null || all.Length == 0)
        {
            Debug.LogError("[PSR] 读不到 " + VfxManagerAsset);
            return 0;
        }
        var mgr = all[0];
        var map = new (string prop, string asset, System.Type type)[]
        {
            ("m_IndirectShader",     VfxPkg + "Shaders/VFXFillIndirectArgs.compute", typeof(ComputeShader)),
            ("m_CopyBufferShader",   VfxPkg + "Shaders/VFXCopyBuffer.compute", typeof(ComputeShader)),
            ("m_SortShader",         VfxPkg + "Shaders/Sort.compute", typeof(ComputeShader)),
            ("m_StripUpdateShader",  VfxPkg + "Shaders/UpdateStrips.compute", typeof(ComputeShader)),
            ("m_PrefixSumShader",    VfxPkg + "Shaders/VFXPrefixSum.compute", typeof(ComputeShader)),
            ("m_RuntimeResources",   VfxPkg + "Runtime/Utilities/RuntimeResources.asset", typeof(Object)),
        };

        var so = new SerializedObject(mgr);
        int fixedCount = 0;
        foreach (var m in map)
        {
            var obj = AssetDatabase.LoadAssetAtPath(m.asset, m.type);
            if (obj == null)
            {
                Debug.LogWarning("[PSR] 包内没找到 " + m.asset + " ⇒ 跳过 " + m.prop);
                continue;
            }
            var p = so.FindProperty(m.prop);
            if (p == null)
            {
                Debug.LogWarning("[PSR] VFXManager 没有字段 " + m.prop + "（本版 URP 可能不需要它）");
                continue;
            }
            if (p.objectReferenceValue == obj) continue;
            p.objectReferenceValue = obj;
            fixedCount++;
        }
        if (fixedCount > 0)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        var so2 = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(VfxManagerAsset)[0]);
        int ok = 0, bad = 0;
        foreach (var m in map)
        {
            var p = so2.FindProperty(m.prop);
            if (p == null) continue;
            var want = AssetDatabase.LoadAssetAtPath(m.asset, m.type);
            if (p.objectReferenceValue != null && p.objectReferenceValue == want) ok++;
            else { bad++; Debug.LogError("[PSR] VFXManager 读回不符：" + m.prop); }
        }
        var ver = so2.FindProperty("m_RuntimeVersion");
        Debug.Log("[PSR] VFXManager 读回：符合 " + ok + " / 不符 " + bad
                  + " · m_RuntimeVersion = " + (ver != null ? ver.intValue.ToString() : "<无此字段>"));
        return fixedCount;
    }

    /// <summary>按**属性名**递归找（不依赖 path，免得 URP 改了层级名就失配）。</summary>
    static SerializedProperty FindByName(SerializedObject so, string name)
    {
        var p = so.GetIterator();
        while (p.NextVisible(true))
            if (p.name == name) return p.Copy();
        return null;
    }
}
