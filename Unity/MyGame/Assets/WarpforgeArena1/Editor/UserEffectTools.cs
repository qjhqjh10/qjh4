// UserEffectTools.cs — **自制特效那条小道的样本 + 自检**
//
// 为什么要有它：效果库那条链原来是单向的「导出产物 → `effect_index.json` → 库」，
// **自制的东西没有任何入口能进来** —— 你往工程里丢一个 prefab，`Play("我的特效")` 是空的，
// 而且不报错。`EffectLibraryBuilder.CollectUserEffects` 补上了那个入口，本工具负责**验它真的通**。
//
// 它做三件事：
//   ① 建一个**样本特效**（`Effects/Example_Spark.prefab`）—— 既是自检的靶子，也是抄的范例；
//   ② 故意造一个**与库里重名的 prefab**，验证「重名不会顶掉原版」这条安全性质；
//   ③ 跑一次建库，然后**逐条断言**（库里有它 / 原版一条不少 / 播得起来且会自己收尾）。
//
// 用法：`-executeMethod UserEffectTools.Run`
//   判据看末尾的 `=== 合计：N 通过 / M 失败 ===`，以及批处理退出码。
using System.IO;
using UnityEditor;
using UnityEngine;
using WarpforgeVFX;

public static class UserEffectTools
{
    const string P = "WFEFF ";
    const string UserDir = "Assets/CardPresentation/Effects";
    const string SampleName = "Example_Spark";
    /// <summary>拿来验「重名」的原版条目名（取自库里的真实效果，别改成不存在的名字，否则这条断言是假的）。</summary>
    const string ClashName = "Acid Rain Damage Target";

    static int _pass, _fail;
    static void Check(bool ok, string what)
    {
        if (ok) { _pass++; Debug.Log(P + "  ✓ " + what); }
        else { _fail++; Debug.LogError(P + "  ✗ " + what); }
    }

    public static void Run()
    {
        Debug.Log(P + "=== 自制特效管道 自检 开始 ===");
        EnsureFolder();

        string samplePath = MakeSample();
        Check(!string.IsNullOrEmpty(samplePath) && File.Exists(samplePath), $"样本 prefab 就绪：{samplePath}");

        string clashPath = MakeClash();
        Check(!string.IsNullOrEmpty(clashPath), $"重名靶子就绪：{clashPath}");

        bool built = EffectLibraryBuilder.Build(
            new string[0], EffectLibraryBuilder.DefaultOutAsset, EffectLibraryBuilder.DefaultReportPath);
        Check(built, "建库返回 true");

        var lib = AssetDatabase.LoadAssetAtPath<WarpforgeEffectLibrary>(EffectLibraryBuilder.DefaultOutAsset);
        Check(lib != null, "效果库资产读得到");
        if (lib == null) { Done(); return; }

        // ---- ① 自制特效进库了 ----
        var mine = lib.Get(SampleName);
        Check(mine != null, $"自制特效「{SampleName}」在库里");
        Check(mine != null && mine.prefab != null, "它的 prefab 引用不是空的");

        // ---- ② 重名没顶掉原版 ----
        var clash = lib.Get(ClashName);
        Check(clash != null, $"原版条目「{ClashName}」还在");
        string clashAsset = clash != null && clash.prefab != null
            ? AssetDatabase.GetAssetPath(clash.prefab) : "<null>";
        Check(clashAsset.StartsWith("Assets/WarpforgeVFX/Prefabs/"),
              $"重名**没顶掉原版**（它的 prefab 仍指向导出目录，实际 = {clashAsset}）");

        // ---- ③ 原版那批一条不少 ----
        int originals = 0, mineCount = 0;
        foreach (var e in lib.entries ?? new WFEffectEntry[0])
        {
            if (e == null) continue;
            if (e.verdict == "自制") mineCount++; else originals++;
        }
        Check(originals >= 958, $"原版条目一条不少（实测 {originals}，应 ≥ 958）");
        Check(mineCount == 1, $"自制条目只有样本这 1 个（实测 {mineCount}）");

        // ---- ④ 播得起来，而且会自己进收尾 ----
        int before = WarpforgeEffectPlayer.ActiveCount;
        var player = WarpforgeEffectPlayer.Play(SampleName, null, Vector3.zero, 1f);
        Check(player != null, "Play() 起了（库找不到 / prefab 空都会在这里变红）");
        if (player != null)
        {
            Check(WarpforgeEffectPlayer.ActiveCount == before + 1, "ActiveCount +1（进池了）");
            Check(player.IsPlaying, "刚开始是「在播」");

            float life = mine != null ? mine.AutoLifetime() : 1f;
            player.Tick(life + 0.01f);
            Check(player.IsExiting, $"过了寿命（{life:F2}s）进了收尾");

            float tail = (mine != null && mine.exitDestroyTime > 0f) ? mine.exitDestroyTime : 3f;
            player.Tick(tail + 0.01f);
            Check(!player.IsPlaying, "收尾走完就不在播了");

            player.Kill();
            Check(WarpforgeEffectPlayer.ActiveCount == before, "Kill() 之后不再占着池子");
        }

        Cleanup(clashPath);
        Done();
    }

    static void Done()
    {
        Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败 ===");
        if (Application.isBatchMode) EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(UserDir))
        {
            var parent = Path.GetDirectoryName(UserDir).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder("Assets", Path.GetFileName(parent));
            AssetDatabase.CreateFolder(parent, Path.GetFileName(UserDir));
            Debug.Log(P + $"新建目录 {UserDir}");
        }
    }

    /// <summary>建样本（幂等）。**故意的简单**：它要能当范例抄，不要塞花活。</summary>
    static string MakeSample()
    {
        string path = $"{UserDir}/{SampleName}.prefab";
        if (File.Exists(path)) return path;
        return BuildSimpleEffect(SampleName, path, tint: new Color(0.55f, 0.85f, 1f, 1f));
    }

    /// <summary>故意与库里某条重名的靶子（自检用完就删）。</summary>
    static string MakeClash()
    {
        string path = $"{UserDir}/{ClashName}.prefab";
        if (File.Exists(path)) return path;
        return BuildSimpleEffect(ClashName, path, tint: Color.white);
    }

    static void Cleanup(string clashPath)
    {
        if (!string.IsNullOrEmpty(clashPath) && File.Exists(clashPath))
        {
            AssetDatabase.DeleteAsset(clashPath);
            Debug.Log(P + "已删掉重名靶子（那是自检用的，不该留在工程里）");
        }
    }

    /// <summary>造一个能看的简单粒子 prefab。渲染器材质单独存成 .mat 资产 —— 不存的话
    /// prefab 里会引用一个「运行时才存在」的材质，存盘时落不下来、下次打开是空的。</summary>
    static string BuildSimpleEffect(string name, string prefabPath, Color tint)
    {
        var go = new GameObject(name);
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = false;
        main.duration = 0.8f;
        main.startLifetime = 0.6f;
        main.startSpeed = 2.5f;
        main.startSize = 0.18f;
        main.startColor = tint;
        main.playOnAwake = false;

        var em = ps.emission;
        em.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var psr = go.GetComponent<ParticleSystemRenderer>();
        psr.sharedMaterial = MakeMaterial(name, tint);

        var info = go.AddComponent<WFEffectInfo>();
        info.displayName = name;
        info.exitDestroyTime = 1f;          // 样本演示「收尾时长可以自己定」
        info.notes = "自制特效范例：prefab 放 " + UserDir + "，挂 WFEffectInfo，然后跑 EffectLibraryBuilder.Run";

        var saved = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(P + $"已建 {prefabPath}");
        return saved != null ? prefabPath : null;
    }

    static Material MakeMaterial(string name, Color tint)
    {
        string matPath = $"{UserDir}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (existing != null) return existing;

        var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        var mat = new Material(sh) { name = name };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        AssetDatabase.CreateAsset(mat, matPath);
        AssetDatabase.SaveAssets();
        return mat;
    }
}
