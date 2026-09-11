// ProbeBundle.cs — 临时探针：测试能否把 bundle 里的 Shader 落成工程资产（决定方案 B 的实现方式）
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public static class ProbeBundle
{
    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string Out = "Assets/_ShaderTest";

    public static void Run()
    {
        Debug.Log("=== Shader 落资产测试 开始 ===");
        if (!AssetDatabase.IsValidFolder(Out))
            AssetDatabase.CreateFolder("Assets", "_ShaderTest");

        var sb = AssetBundle.LoadFromFile(Path.Combine(BundleDir, "shaders_assets_all.bundle"));
        var shaders = new List<Shader>();
        foreach (var n in sb.GetAllAssetNames())
        {
            Shader s = null;
            try { s = sb.LoadAsset<Shader>(n); } catch { }
            if (s != null) shaders.Add(s);
        }
        Debug.Log($"载入 shader {shaders.Count}");
        var target = shaders.FirstOrDefault(s => s.name == "Everguild/FX/Extra Color") ?? shaders[0];
        Debug.Log($"测试对象: {target.name}  属性数={target.GetPropertyCount()}");

        // ① CreateAsset 存成 .asset
        try
        {
            var p = $"{Out}/S1.asset";
            AssetDatabase.CreateAsset(target, p);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            var back = AssetDatabase.LoadAssetAtPath<Shader>(p);
            Debug.Log($"① CreateAsset(.asset): {(back == null ? "读不回" : $"读回 OK，属性数={back.GetPropertyCount()}")}");
        }
        catch (Exception e) { Debug.Log($"① CreateAsset(.asset) 失败: {e.GetType().Name}: {e.Message}"); }

        // ② 先建材质再用 CreateAsset 保存材质
        try
        {
            var mat = new Material(target) { name = "M2" };
            var p = $"{Out}/M2.mat";
            AssetDatabase.CreateAsset(mat, p);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            var back = AssetDatabase.LoadAssetAtPath<Material>(p);
            Debug.Log($"② 材质 CreateAsset: {(back == null ? "读不回" : $"读回 OK，shader={(back.shader == null ? "null" : back.shader.name)}")}");
        }
        catch (Exception e) { Debug.Log($"② 材质 CreateAsset 失败: {e.GetType().Name}: {e.Message}"); }

        // ③ CreateAsset 存成 .shader（预期报错，确认 Unity 拒绝）
        try
        {
            var p = $"{Out}/S3.shader";
            AssetDatabase.CreateAsset(target, p);
            Debug.Log("③ CreateAsset(.shader): 竟然成功了");
        }
        catch (Exception e) { Debug.Log($"③ CreateAsset(.shader) 失败（预期）: {e.GetType().Name}"); }

        // ④ 把 Shader 的序列化字节写出来，看能不能作为 .shader 文本重新导入
        try
        {
            byte[] blob = null;
            var f = target.GetType().GetField("m_SubProgramBlob", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Debug.Log($"④ 反射取 m_SubProgramBlob: {(f == null ? "字段不存在" : "存在")}");
        }
        catch (Exception e) { Debug.Log($"④ 失败: {e.Message}"); }

        // ⑤ 运行时方案可行性：加载 bundle 拿 shader → 建材质 → 赋给渲染器（模拟游戏内重建）
        Debug.Log("⑤ 运行时重建路径（bundle→shader→material→renderer）本身已在游戏里验证可行");

        Debug.Log("=== Shader 落资产测试 结束 ===");
    }
}
