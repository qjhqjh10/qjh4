// BundleSmokeTest.cs — 最小 bundle 加载自检：给一批 bundle 路径，逐个 LoadFromFile 并报结果
//
// 用途：判断「UnityPy 重新打包出来的 bundle，Unity 到底认不认」。
// 之前遇到的错误是 "could not be loaded because it is not compatible with this newer
// version of the Unity runtime"，但那句话也可能是别的原因引起的，
// 所以先用**未经修改的纯往返包**做对照 —— 一次只改一个变量。
//
// 用法：
//   Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod BundleSmokeTest.Run -logFile -
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BundleSmokeTest
{
    const string P = "WFSMOKE ";

    static readonly string[] Targets = {
        // 二分：对 battleprefabs 做不同程度的删减，找「删到什么程度会被 Unity 拒」
        @"d:\4\_tmp_view\bundletest\b5_del_one_texture.bundle",
        @"d:\4\_tmp_view\bundletest\b6_del_textures.bundle",
        @"d:\4\_tmp_view\bundletest\b7_del_go_transform.bundle",
        @"d:\4\_tmp_view\bundletest\b8_keep_only_shader.bundle",
        @"d:\4\_tmp_view\bundletest\e3_filter_dropstream_orig.bundle",
    };

    public static void Run()
    {
        Application.logMessageReceived += (msg, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Warning || type == LogType.Exception)
                Debug.Log(P + "LOG " + type + ": " + msg.Replace("\n", " | "));
        };

        // 支持 -bundle <路径>：一次进程只测一个包，彻底排除「先加载谁」的影响。
        // 之前把多个包放一个进程里测，结果同一个文件在不同位置报出**不同的**错误
        // （"not compatible" vs "same files already loaded"），说明是 Unity 的
        // 内容去重机制在干扰，不是包本身的属性 —— 那种测法得不出结论。
        var argv = Environment.GetCommandLineArgs();
        for (int i = 0; i < argv.Length - 1; i++)
        {
            if (argv[i] == "-bundle") { TestOne(argv[i + 1]); Debug.Log(P + "=== 结束 ==="); return; }
        }

        foreach (var path in Targets) TestOne(path);
        Debug.Log(P + "=== 结束 ===");
    }

    static void TestOne(string path)
    {
        var name = Path.GetFileName(path);
        if (!File.Exists(path)) { Debug.Log(P + $"SKIP  {name} —— 文件不存在"); return; }
        AssetBundle b = null;
        try { b = AssetBundle.LoadFromFile(path); }
        catch (Exception e) { Debug.Log(P + $"THROW {name}: {e.Message}"); return; }
        if (b == null) { Debug.Log(P + $"FAIL  {name} —— LoadFromFile 返回 null"); return; }

        int shaderN = 0;
        try { shaderN = b.LoadAllAssets<Shader>().Count(s => s != null); } catch { }
        string[] assetNames = new string[0];
        try { assetNames = b.GetAllAssetNames(); } catch { }
        Debug.Log(P + $"OK    {name} —— 资产名 {assetNames.Length} 条，Shader {shaderN} 个，bundle.name={b.name}");
    }

    /// <summary>把一个 bundle 里的 Shader 到底能不能捞出来，逐条路都试一遍。
    /// 关键问题：battleprefabs 那个包里 UnityPy 能看到 42 个 Shader 对象，
    /// 但 LoadAllAssets&lt;Shader&gt;() 返回 0 —— 得确认到底是「取不到」还是「取法不对」。</summary>
    static void DigShaders(string path)
    {
        var name = Path.GetFileName(path);
        Debug.Log(P + $"########## 剖析 {name} ##########");
        var b = AssetBundle.LoadFromFile(path);
        if (b == null) { Debug.Log(P + "  加载失败"); return; }

        var names = b.GetAllAssetNames();
        Debug.Log(P + $"  资产名 {names.Length} 条；含 'shader' 字样的 " +
                      $"{names.Count(n => n.IndexOf("shader", StringComparison.OrdinalIgnoreCase) >= 0)} 条");
        foreach (var n in names.Where(n => n.IndexOf("shader", StringComparison.OrdinalIgnoreCase) >= 0).Take(5))
            Debug.Log(P + $"      样本名: {n}");

        int allShader = 0;
        try { allShader = b.LoadAllAssets<Shader>().Count(s => s != null); } catch (Exception e) { Debug.Log(P + "  LoadAllAssets<Shader> 抛: " + e.Message); }
        Debug.Log(P + $"  ① LoadAllAssets<Shader>()       = {allShader}");

        int allAny = 0, shaderInAny = 0;
        try
        {
            var any = b.LoadAllAssets();
            allAny = any.Length;
            shaderInAny = any.Count(o => o is Shader);
            foreach (var o in any.Where(o => o is Shader).Take(5))
                Debug.Log(P + $"      样本 shader: {o.name}");
        }
        catch (Exception e) { Debug.Log(P + "  LoadAllAssets() 抛: " + e.Message); }
        Debug.Log(P + $"  ② LoadAllAssets() 共 {allAny} 个，其中 Shader {shaderInAny} 个");

        int byName = 0;
        var hits = new System.Collections.Generic.List<string>();
        foreach (var n in names)
        {
            try
            {
                var s = b.LoadAsset<Shader>(n);
                if (s != null) { byName++; if (hits.Count < 5) hits.Add($"{n} → {s.name}"); }
            }
            catch { }
        }
        Debug.Log(P + $"  ③ 按资产名逐个 LoadAsset<Shader> = {byName} 个");
        foreach (var h in hits) Debug.Log(P + $"      {h}");

        b.Unload(false);
    }
}
