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
    /// <summary>
    /// 工程**自建**的 shader（不是用来替代原版某个 shader 的，所以 `WarpforgeShaderMap` 里没有）。
    /// ⚠️ 改名要同步代码里的常量（这里这条在 `BattleDoors.ShaderName`）。
    /// </summary>
    static readonly string[] OwnShaders = { "CardPresentation/Video Split Alpha" };

    /// <summary>**引擎自带、但原版材质会「按名字」找的** shader 所在目录 —— 整目录扫。
    ///
    /// 🔴 **为什么必须显式注册（2026-09-16 构建后 player 验证实测抓到的）**：
    /// 原版材质引用的是**原版的 GUID**，那个 GUID 在本工程里根本不存在
    /// ⇒ Unity 的引用链**拉不进**对应的 shader ⇒ 打包时被剥掉
    /// ⇒ 运行时 `Shader.Find("TextMeshPro/Distance Field")` 返回 **null**
    /// ⇒ 材质槽保留占位材质 ⇒ **整块渲成洋红**。
    /// 实据：`Player.log` 里 5 条 `找不到 shader 'TextMeshPro/Distance Field'（材质 Pragati-Regular
    /// Atlas Material …）`，白板 `CardPrefab` 那一格就是一大块洋红。
    /// ⚠️ 编辑器里**看不出来** —— 编辑器的 `Shader.Find` 能在整个工程里找，不进包的也算。
    ///
    /// **为什么不一张张点名**：原版材质点名要哪张是**它**说了算，点名会漏。
    /// 这个目录是 TMP Essentials 的固定集合（13 张），整目录扫不会漏、也不会多到哪去。</summary>
    const string ThirdPartyShaderDir = "Assets/TextMesh Pro/Shaders";

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
        var names = new List<string>();
        names.AddRange(WarpforgeShaderMap.Replacements.Values);
        names.AddRange(OwnShaders);
        foreach (var name in names.Distinct())
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

        // ---- 引擎自带、但原版材质按名字找的那一族（TMP）----
        foreach (var guid in AssetDatabase.FindAssets("t:Shader", new[] { ThirdPartyShaderDir }))
        {
            var sh = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));
            if (sh == null) continue;
            if (have.Contains(sh.name)) { have.Add(sh.name); continue; }
            arr.arraySize++;
            arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
            have.Add(sh.name);
            added++;
            Debug.Log($"  已加入（第三方）: {sh.name}");
        }

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log($"=== 完成：新增 {added} 个（当前共 {arr.arraySize} 个 Always Included）===");
    }

    /// <summary>
    /// **把抓屏 Feature 挂到 URP 的 Renderer 资产上**（2026-09-13 第三十三轮）。
    ///
    /// 为什么要有这一步：`GrabPassTransparentFeature` 只是一个类，
    /// **必须被列进 `ScriptableRendererData.m_RendererFeatures`** 才会真的跑。
    /// 手动在 Inspector 里加也行，但那是「下一个会话不知道做过没有」的那种改动 ——
    /// 做成入口，可重跑、可核对（幂等，重复跑不会加两份）。
    ///
    /// 用法：
    ///   Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
    ///     -executeMethod WarpforgeSetup.RegisterRendererFeature -logFile -
    /// </summary>
    public static void RegisterRendererFeature()
    {
        Debug.Log("=== 挂上抓屏 Feature ===");
        string[] assets =
        {
            "Assets/Settings/PC_Renderer.asset",
            "Assets/Settings/Mobile_Renderer.asset",
        };
        int done = 0;
        foreach (var path in assets)
        {
            var data = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.ScriptableRendererData>(path);
            if (data == null) { Debug.LogWarning($"  找不到 {path}"); continue; }

            bool have = false;
            foreach (var f in data.rendererFeatures)
                if (f is WarpforgeVFX.GrabPassTransparentFeature) { have = true; break; }
            if (have) { Debug.Log($"  {path}：已经在里面了"); done++; continue; }

            var feat = ScriptableObject.CreateInstance<WarpforgeVFX.GrabPassTransparentFeature>();
            feat.name = "GrabPassTransparent";
            AssetDatabase.AddObjectToAsset(feat, data);
            data.rendererFeatures.Add(feat);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log($"  {path}：加上了");
            done++;
        }
        Debug.Log($"=== 完成：{done}/{assets.Length} ===");
    }
}
