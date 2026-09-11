// WarpforgeSetup.cs — 把自建替代 shader 注册进 GraphicsSettings 的 Always Included Shaders
//
// 为什么必须做：binder 是运行时用 Shader.Find(名字) 取 shader 的，而打包之后
// Shader.Find 只能找到「被材质引用」或「列入 Always Included Shaders」的 shader。
// 自建替代 shader 目前没有被工程里任何材质直接引用，不注册就会在打包后变粉块。
//
// 用法：Unity.exe -batchmode -quit ... -executeMethod WarpforgeSetup.RegisterShaders -logFile -
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WarpforgeVFX;

public static class WarpforgeSetup
{
    public static void RegisterShaders()
    {
        Debug.Log("=== 注册自建 shader ===");
        var gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
        if (gs == null) { Debug.LogError("拿不到 GraphicsSettings.asset"); return; }

        var so = new SerializedObject(gs);
        var arr = so.FindProperty("m_AlwaysIncludedShaders");
        if (arr == null) { Debug.LogError("找不到 m_AlwaysIncludedShaders"); return; }

        var have = new HashSet<string>();
        for (int i = 0; i < arr.arraySize; i++)
        {
            var s = arr.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
            if (s != null) have.Add(s.name);
        }

        int added = 0;
        foreach (var name in WarpforgeShaderMap.Replacements.Values.Distinct())
        {
            if (have.Contains(name)) continue;
            var sh = Shader.Find(name);
            if (sh == null) { Debug.LogWarning($"  找不到 shader: {name}"); continue; }
            arr.arraySize++;
            arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
            have.Add(name);
            added++;
            Debug.Log($"  已加入: {name}");
        }

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log($"=== 完成：新增 {added} 个（当前共 {arr.arraySize} 个 Always Included）===");
    }
}
