// SweepStabilityProbe.cs — 尺子稳定性探针：把「跨进程漂移」定位到「**仿真**」还是「**渲染**」
//
// 为什么需要它（2026-09-19 深夜）：
//   `EffectSweepBatch` 的尺子**跨进程会漂** —— `orig` 那趟（与任何改动无关）两跑
//   在 7657 行里差 471 行（6.2%），`sum>=1000` 的行里 17.2% 会漂，最大相对差 61%。
//   而钉种子那三件事（`SeedAndReset`）**是做了的**，且取景缓存逐字节相同、代码同版本、
//   prefab 一个都没开 `SubEmittersModule` —— 三个候选已被排除。
//   剩下的只能在两个位置之一：**粒子状态本身**（仿真层）或**像素**（渲染层）。
//
// 判据（这就是本探针的全部意义）：
//   · **粒子状态哈希相同、像素哈希不同** ⇒ 锅在**渲染**（shader 变体 / 贴图 mip / 全局关键字 / 材质缓存）
//   · **粒子状态哈希就不同**           ⇒ 锅在**仿真**（种子没真正生效）
//   每一侧都跑 3 次「全新实例 + SeedAndReset + Simulate」：**同进程三次若相同、跨进程不同**，
//   锅在**进程级状态**；**同进程三次就不同**，锅在**调用序列本身**。
//
// 用法：
//   unset ELECTRON_RUN_AS_NODE && Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod SweepStabilityProbe.Run -logFile "d:/4/_tmp_view/stab_a.log"
//   筛输出：grep "^SS " d:/4/_tmp_view/stab_a.log
//   **跑两遍**（两个进程），逐字段比两边的输出 —— 差异在哪一列，锅就在哪一层。
//
//   WFPROBE_TARGETS="A,B" 可换目标（默认取两个已知会漂的：一个单粒子系统、一个多发射器）。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SweepStabilityProbe
{
    const string P = "SS ";
    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string VfxBundleName = "battleprefabs_vfxandmisc_assets_all.bundle";
    const string PrefabDir = "Assets/WarpforgeVFX/Prefabs";

    const float SimTime = 1.2f;
    const int W = 512, H = 512;
    const string OutDir = @"d:\4\_tmp_view\stab";
    static readonly Color Bg = new Color(0.07f, 0.08f, 0.10f, 1f);

    // 默认目标：① 单粒子系统（最小复现）② 多发射器（漂 25~29% 的那条）
    static readonly string[] DefaultTargets = {
        "EC Sword Cut Board DMC Style",
        "Environmental Condition Emperor's Children 2 Fumes",
    };

    static string[] Targets()
    {
        var env = Environment.GetEnvironmentVariable("WFPROBE_TARGETS");
        if (!string.IsNullOrEmpty(env))
            return env.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
        return DefaultTargets;
    }

    public static void Run()
    {
        Debug.Log(P + "=== 尺子稳定性探针 开始 ===");
        // 进程级状态：这两条若跨进程不同，就是「渲染层漂移」的现成解释
        Debug.Log($"{P}进程状态\tglobalTextureMipmapLimit={QualitySettings.globalTextureMipmapLimit}" +
                  $"\tmaxTextureSize={QualitySettings.masterTextureLimit}" +
                  $"\tactiveColorSpace={QualitySettings.activeColorSpace}" +
                  $"\taniso={QualitySettings.anisotropicFiltering}" +
                  $"\tvSync={QualitySettings.vSyncCount}");

        // 🔬 `WFPROBE_BOTH=1`：**同进程两阶段**（先 orig 全渲、Unload、再 exp 全渲）。
        //    这是「跨进程漂移」的**候选修法**：两侧在同一进程里渲，进程间差异在比值里自洽抵消。
        //    判据：跑两遍，**比值 (exp/orig) 应稳定**，哪怕两侧的绝对 sum 各自漂。
        if (Environment.GetEnvironmentVariable("WFPROBE_BOTH") == "1") { BothInOneProcess(); return; }

        AssetBundle vfx = null;
        foreach (var f in Directory.GetFiles(BundleDir, "*.bundle"))
        {
            var b = AssetBundle.LoadFromFile(f);
            if (b != null && Path.GetFileName(f) == VfxBundleName) vfx = b;
        }
        if (vfx == null) { Debug.LogError(P + "特效 bundle 未加载"); return; }

        var originals = new Dictionary<string, GameObject>();
        foreach (var n in vfx.GetAllAssetNames())
        {
            GameObject g = null;
            try { g = vfx.LoadAsset<GameObject>(n); } catch { }
            if (g != null && g.GetComponentsInChildren<ParticleSystemRenderer>(true).Length > 0)
                originals[g.name] = g;
        }

        // ---- 第一趟：只碰原版（与 `EffectSweepBatch` 同样的顺序）----
        foreach (var t in Targets())
        {
            if (originals.TryGetValue(t, out var o)) Probe(t, "orig", o);
            else Debug.LogWarning(P + $"原版里找不到 {t}");
        }

        // ---- 卸掉源 bundle，回到真实运行时条件，再测导出侧 ----
        originals.Clear();
        vfx.Unload(true);
        foreach (var t in Targets())
        {
            var path = $"{PrefabDir}/{t}.prefab";
            if (!File.Exists(path)) { Debug.LogWarning(P + $"导出里找不到 {t}"); continue; }
            Probe(t, "exp", AssetDatabase.LoadAssetAtPath<GameObject>(path));
        }

        Debug.Log(P + "=== 结束 ===");
    }

    /// <summary>同进程两阶段：先「加载全部源包 + 渲原版」，再「卸包 + 渲导出」。
    /// 这是对 `EffectSweepBatch` 那个「必须分两个进程」设计的**可行性验证**（`EffectCompare` 已经是这么干的）。</summary>
    static void BothInOneProcess()
    {
        var targets = Targets();
        var got = new Dictionary<string, List<(int lit, double sum, string px)>>();

        // ---- 阶段 1：加载全部源包，渲原版 ----
        var all = new List<AssetBundle>();
        foreach (var f in Directory.GetFiles(BundleDir, "*.bundle"))
        {
            var b = AssetBundle.LoadFromFile(f);
            if (b != null) all.Add(b);
        }
        Debug.Log(P + $"阶段1：加载了 {all.Count} 个源包");
        var originals = new Dictionary<string, GameObject>();
        foreach (var b in all)
        {
            foreach (var n in b.GetAllAssetNames())
            {
                GameObject g = null;
                try { g = b.LoadAsset<GameObject>(n); } catch { }
                if (g != null && g.GetComponentsInChildren<ParticleSystemRenderer>(true).Length > 0)
                    originals[g.name] = g;
            }
        }
        foreach (var t in targets)
        {
            if (!originals.TryGetValue(t, out var o)) { Debug.LogWarning(P + $"原版无 {t}"); continue; }
            got[t] = new List<(int, double, string)>();
            for (int i = 1; i <= 3; i++)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var inst = UnityEngine.Object.Instantiate(o);
                inst.transform.position = Vector3.zero;
                inst.transform.rotation = Quaternion.identity;
                var pss = inst.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in pss)
                {
                    EffectSweepBatch.SeedAndReset(ps);
                    ps.Simulate(SimTime, withChildren: true, restart: false, fixedTimeStep: true);
                }
                var cam = MakeCamera(inst);
                int lit; double sum; string px;
                Render(cam, out lit, out sum, out px);
                got[t].Add((lit, sum, px));
                Debug.Log($"{P}BOTH\t{t}\torig\t第{i}次\tlit={lit}\tsum={sum:F0}\t像素={px}");
                UnityEngine.Object.DestroyImmediate(inst);
            }
        }

        // ---- 卸掉全部源包（这正是 `EffectSweepBatch` 分两趟的原因）----
        originals.Clear();
        foreach (var b in all) b.Unload(true);
        AssetBundle.UnloadAllAssetBundles(true);
        Resources.UnloadUnusedAssets();
        Debug.Log(P + "阶段1 结束：全部源包已卸载");

        // ---- 阶段 2：渲导出（真实运行时条件）----
        foreach (var t in targets)
        {
            var path = $"{PrefabDir}/{t}.prefab";
            if (!File.Exists(path)) { Debug.LogWarning(P + $"导出无 {t}"); continue; }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            for (int i = 1; i <= 3; i++)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var inst = UnityEngine.Object.Instantiate(prefab);
                inst.transform.position = Vector3.zero;
                inst.transform.rotation = Quaternion.identity;
                var binder = inst.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
                if (binder != null) binder.Apply();
                var pss = inst.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in pss)
                {
                    EffectSweepBatch.SeedAndReset(ps);
                    ps.Simulate(SimTime, withChildren: true, restart: false, fixedTimeStep: true);
                }
                var cam = MakeCamera(inst);
                int lit; double sum; string px;
                Render(cam, out lit, out sum, out px);
                got[t].Add((lit, sum, px));
                Debug.Log($"{P}BOTH\t{t}\texp\t第{i}次\tlit={lit}\tsum={sum:F0}\t像素={px}");
                UnityEngine.Object.DestroyImmediate(inst);
            }
        }

        // ---- 比值（这才是尺子真正用的量）----
        foreach (var kv in got)
        {
            var v = kv.Value;
            if (v.Count < 6) continue;
            double o = (v[0].sum + v[1].sum + v[2].sum) / 3.0;
            double e = (v[3].sum + v[4].sum + v[5].sum) / 3.0;
            Debug.Log($"{P}BOTH比值\t{kv.Key}\torig中位3={o:F0}\texp中位3={e:F0}\t**比值={e / Math.Max(1.0, o):F4}**");
        }
        Debug.Log(P + "=== BOTH 结束 ===");
    }

    /// <summary>3 次「全新实例 + SeedAndReset + Simulate」+ 两条对照臂。</summary>
    static void Probe(string name, string side, GameObject prefab)
    {
        for (int trial = 1; trial <= 3; trial++)
            OneRun(name, side, "试验" + trial, prefab, twice: false);
        // 对照臂 A：不重新播种，直接把同一个实例再推一次 —— 若它会变，
        // 说明「种子只在播种那一刻确定」，那 `SeedAndReset` 的调用位置就是关键。
        OneRun(name, side, "连推两下", prefab, twice: true);
        // 🔬 对照臂 B/C：**把时间类全局 uniform 钉死再渲**。
        //    假设（2026-09-19 深夜）：漂移来自**读墙钟的 shader**（`_Time` 做 UV 滚动/噪声相位），
        //    而仿真侧已经证明是确定的（粒子哈希三次相同）⇒ 那就只剩「渲染时的时间」这一个变量。
        //    判据：**钉死后两次的像素哈希应相同**；不钉死则每次不同。
        OneRun(name, side, "钉时间-1", prefab, twice: false, pinTime: true);
        OneRun(name, side, "钉时间-2", prefab, twice: false, pinTime: true);
        OneRun(name, side, "不钉时间-1", prefab, twice: false, pinTime: false);
        OneRun(name, side, "不钉时间-2", prefab, twice: false, pinTime: false);
        // 🔬 最后的隔离：**同一个实例、同一个相机，连渲两次**。
        //    两次若相同 ⇒ 渲染路径本身是确定的，差异来自「建实例 / 建场景 / 算取景」这一段；
        //    两次若不同 ⇒ 渲染调用本身就不确定（排序 / 异步编译 / RT 残留）。
        SameInstanceTwice(name, side, prefab);
        // 🔬 判别实验：把「为什么同一实例连渲会变」的两大候选分开。
        //    A 粒子绘制顺序（并行排序在等距时次序可能变）→ 关 `sortMode` 应让它稳定
        //    B 跨渲染反馈（工程挂了 `GrabPassTransparentFeature`，它把颜色缓冲拷进**全局纹理**，
        //      而全局纹理会跨渲染留存）→ **每次换一张全新 RT** 应让它稳定
        Discriminate(name, side, prefab);
        // 🔬 对照：**不在渲染之间做任何哈希**（`HashParticles` 会读 `GetParticles`/`GetTrails`，
        //    得排除「是探针自己的读数在改状态」这个可能）。
        NoHashRepeat(name, side, prefab);

        // 材质/贴图那几项进程级状态（只在第一次打印，够可比了）
        if (side == "orig")
        {
            var inst = UnityEngine.Object.Instantiate(prefab);
            var sb = new StringBuilder();
            int i = 0;
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    sb.Append($"\n{P}材质[{i++}]\t{name}\t{m.name}\tshader={m.shader.name}" +
                              $"\tqueue={m.renderQueue}\tkeywords={string.Join("|", m.shaderKeywords.Take(8))}");
                    foreach (var pn in m.GetTexturePropertyNames())
                    {
                        var tx = m.GetTexture(pn) as Texture2D;
                        if (tx == null) continue;
                        sb.Append($"\n{P}贴图\t{name}\t{m.name}.{pn}\t{tx.name}\t{tx.width}x{tx.height}" +
                                  $"\tmips={tx.mipmapCount}\tstreaming={tx.streamingMipmaps}" +
                                  $"\twanted={tx.requestedMipmapLevel}");
                    }
                }
            }
            Debug.Log(sb.ToString());
            UnityEngine.Object.DestroyImmediate(inst);
        }
    }

    /// <summary>判别实验：同一实例连渲，四组条件。
    /// A 关粒子排序 · B 每次换全新 RT · C 两者都上 —— **哪一组稳定下来，锅就在那一层**。</summary>
    static void Discriminate(string name, string side, GameObject prefab)
    {
        var arms = new (string tag, bool noSort, bool freshRT)[]
        {
            ("基线",            false, false),
            ("A关排序",          true,  false),
            ("B新RT",           false, true ),
            ("C关排序+新RT",      true,  true ),
        };
        foreach (var arm in arms)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var inst = UnityEngine.Object.Instantiate(prefab);
            inst.transform.position = Vector3.zero;
            inst.transform.rotation = Quaternion.identity;
            var binder = inst.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
            if (binder != null) binder.Apply();

            var pss = inst.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in pss)
            {
                EffectSweepBatch.SeedAndReset(ps);
                ps.Simulate(SimTime, withChildren: true, restart: false, fixedTimeStep: true);
            }
            if (arm.noSort)
            {
                foreach (var r in inst.GetComponentsInChildren<ParticleSystemRenderer>(true))
                    r.sortMode = ParticleSystemSortMode.None;
            }
            var cam = MakeCamera(inst);
            var hashes = new List<string>();
            Debug.Log($"{P}{name}\t{side}\t判别[{arm.tag}]开跑前\t_GrabPassTransparent={HashGlobal("_GrabPassTransparent")}");
            for (int i = 1; i <= 3; i++)
            {
                int lit; double sum; string px;
                Render(cam, out lit, out sum, out px, false, null,
                       destroyCam: false, freshRT: arm.freshRT);
                hashes.Add(px);
                Debug.Log($"{P}{name}\t{side}\t判别[{arm.tag}]第{i}次\tlit={lit}\tsum={sum:F0}\t像素={px}" +
                          $"\t_GrabPassTransparent={HashGlobal("_GrabPassTransparent")}");
            }
            Debug.Log($"{P}{name}\t{side}\t判别[{arm.tag}]小结\t三次全同={hashes.Distinct().Count() == 1}\t" +
                      $"唯一值={hashes.Distinct().Count()}");
            UnityEngine.Object.DestroyImmediate(cam.gameObject);
            UnityEngine.Object.DestroyImmediate(inst);
        }
    }

    /// <summary>把一个**全局纹理**缩到 64×64 哈希出来 —— 用来判「跨渲染反馈」：
    /// 工程挂了 `GrabPassTransparentFeature`，它每帧把相机颜色缓冲拷进全局 `_GrabPassTransparent`。
    /// 全局是**跨渲染留存**的 ⇒ 若它在连渲之间变了，就说明**上一帧的结果被带进下一帧**。</summary>
    static string HashGlobal(string name)
    {
        Texture tex = null;
        try { tex = Shader.GetGlobalTexture(name); } catch { }
        if (tex == null) return "无";
        var tmp = RenderTexture.GetTemporary(64, 64, 0, RenderTextureFormat.ARGB32);
        var prev = RenderTexture.active;
        try
        {
            Graphics.Blit(tex, tmp);
            RenderTexture.active = tmp;
            var t2 = new Texture2D(64, 64, TextureFormat.RGB24, false);
            t2.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
            t2.Apply();
            var px = t2.GetPixels32();
            var bytes = new byte[px.Length * 3];
            for (int i = 0; i < px.Length; i++)
            {
                bytes[i * 3] = px[i].r; bytes[i * 3 + 1] = px[i].g; bytes[i * 3 + 2] = px[i].b;
            }
            UnityEngine.Object.DestroyImmediate(t2);
            using (var md5 = MD5.Create())
                return BitConverter.ToString(md5.ComputeHash(bytes)).Replace("-", "").Substring(0, 10);
        }
        catch (Exception e) { return "err:" + e.GetType().Name; }
        finally
        {
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(tmp);
        }
    }

    /// <summary>连渲 5 次，**渲染之间一次哈希都不做** —— 排除「是探针自己的读数在改状态」。</summary>
    static void NoHashRepeat(string name, string side, GameObject prefab)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var inst = UnityEngine.Object.Instantiate(prefab);
        inst.transform.position = Vector3.zero;
        inst.transform.rotation = Quaternion.identity;
        var binder = inst.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
        if (binder != null) binder.Apply();
        var pss = inst.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in pss)
        {
            EffectSweepBatch.SeedAndReset(ps);
            ps.Simulate(SimTime, withChildren: true, restart: false, fixedTimeStep: true);
        }
        string first = HashParticles(pss);          // 只在这里哈希一次
        var cam = MakeCamera(inst);
        for (int i = 1; i <= 5; i++)
        {
            int lit; double sum; string px;
            Render(cam, out lit, out sum, out px, false, null, destroyCam: false);
            Debug.Log($"{P}{name}\t{side}\t无哈希第{i}次\tlit={lit}\tsum={sum:F0}\t像素={px}");
        }
        Debug.Log($"{P}{name}\t{side}\t无哈希对照\t首哈希={first}\t末哈希={HashParticles(pss)}");
        UnityEngine.Object.DestroyImmediate(cam.gameObject);
        UnityEngine.Object.DestroyImmediate(inst);
    }

    static void OneRun(string name, string side, string tag, GameObject prefab, bool twice,
                       bool pinTime = false)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var inst = UnityEngine.Object.Instantiate(prefab);
        inst.transform.position = Vector3.zero;
        inst.transform.rotation = Quaternion.identity;
        var binder = inst.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
        if (binder != null) binder.Apply();

        var pss = inst.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in pss)
        {
            EffectSweepBatch.SeedAndReset(ps);
            ps.Simulate(SimTime, withChildren: true, restart: false, fixedTimeStep: true);
            if (twice) ps.Simulate(SimTime, withChildren: true, restart: false, fixedTimeStep: true);
        }
        string simHash = HashParticles(pss);

        var cam = MakeCamera(inst);
        int lit; double sum; string pxHash;
        Render(cam, out lit, out sum, out pxHash, pinTime);

        Debug.Log($"{P}{name}\t{side}\t{tag}\tPS={pss.Length}\t粒子={simHash}\tlit={lit}\tsum={sum:F0}" +
                  $"\t像素={pxHash}\tTime={Time.time:F3}\tpinTime={pinTime}");
        UnityEngine.Object.DestroyImmediate(inst);
    }

    /// <summary>把时间类全局 uniform 钉死（`_Time` 是 `float4(t/20, t, t*2, t*3)`）。
    /// **假设**：漂移来自读墙钟的 shader（UV 滚动 / 噪声相位）。这一段就是那条假设的判据。</summary>
    static void PinTimeGlobals(float t)
    {
        Shader.SetGlobalVector("_Time", new Vector4(t / 20f, t, t * 2f, t * 3f));
        Shader.SetGlobalVector("_SinTime", new Vector4(
            Mathf.Sin(t / 8f), Mathf.Sin(t / 4f), Mathf.Sin(t / 2f), Mathf.Sin(t)));
        Shader.SetGlobalVector("_CosTime", new Vector4(
            Mathf.Cos(t / 8f), Mathf.Cos(t / 4f), Mathf.Cos(t / 2f), Mathf.Cos(t)));
        Shader.SetGlobalVector("unity_DeltaTime", new Vector4(0.02f, 0.02f, 0.02f, 0.02f));
    }

    /// <summary>同一个实例、同一个相机，连渲三次 —— 隔离「渲染调用本身」与「建实例/算取景」。</summary>
    static void SameInstanceTwice(string name, string side, GameObject prefab)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var inst = UnityEngine.Object.Instantiate(prefab);
        inst.transform.position = Vector3.zero;
        inst.transform.rotation = Quaternion.identity;
        var binder = inst.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
        if (binder != null) binder.Apply();

        var pss = inst.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in pss)
        {
            EffectSweepBatch.SeedAndReset(ps);
            ps.Simulate(SimTime, withChildren: true, restart: false, fixedTimeStep: true);
        }
        var cam = MakeCamera(inst);
        Debug.Log($"{P}{name}\t{side}\t相机\tpos=({cam.transform.position.x:F4},{cam.transform.position.y:F4}," +
                  $"{cam.transform.position.z:F4})\tfwd=({cam.transform.forward.x:F4},{cam.transform.forward.y:F4}," +
                  $"{cam.transform.forward.z:F4})\tfov={cam.fieldOfView:F4}\tnear={cam.nearClipPlane:F5}" +
                  $"\tfar={cam.farClipPlane:F2}");

        for (int i = 1; i <= 4; i++)
        {
            bool pin = i > 2;                       // 前两次不钉时间、后两次钉 —— 同一实例内的 A/B
            // 🔬 每次渲染**之前**重取一次粒子哈希：若它在「连渲」之间会变，
            //    说明粒子在**渲染调用之间自己走了**（编辑模式下粒子系统仍会跟真实时间跑），
            //    那漂移就既不是种子、也不是 shader，而是「仿真没停」。
            string simNow = HashParticles(pss);
            int lit; double sum; string px;
            string png = i <= 2 ? $"{OutDir}/{name}__{side}__r{i}.png" : null;
            Render(cam, out lit, out sum, out px, pin, png, destroyCam: false);
            Debug.Log($"{P}{name}\t{side}\t连渲第{i}次(pin={pin})\t粒子此刻={simNow}\tlit={lit}\tsum={sum:F0}" +
                      $"\t像素={px}\t" +
                      $"Time={Time.time:F3}\tlevel={Time.timeSinceLevelLoad:F3}\treal={Time.realtimeSinceStartup:F3}" +
                      $"\tframe={Time.frameCount}\t图={png}");
        }
        // 🔬 第二段：**显式 Pause 之后**再连渲三次。
        //    假设：`Simulate()` 之后系统仍处于「在播」状态 ⇒ 每次 `cam.Render()` 都会按真实时间
        //    往前推一点（渲染器的插值/外推），于是**同一实例连渲会单调漂**。
        //    判据：Pause 后三次像素哈希若相同 ⇒ 根因就是「没停播」，修法就是 Simulate 完 `Pause()`。
        foreach (var ps in pss) ps.Pause();
        for (int i = 5; i <= 7; i++)
        {
            string simNow = HashParticles(pss);
            int lit; double sum; string px;
            Render(cam, out lit, out sum, out px, false, null, destroyCam: false);
            Debug.Log($"{P}{name}\t{side}\tPause后第{i - 4}次\t粒子此刻={simNow}\tlit={lit}\tsum={sum:F0}\t像素={px}");
        }

        // ⚠️ `Discriminate` 必须由外面调：它内部 `NewScene` 会把本方法的相机一起销毁
        //    （踩过：`MissingReferenceException: Camera has been destroyed`）。
        UnityEngine.Object.DestroyImmediate(cam.gameObject);
        UnityEngine.Object.DestroyImmediate(inst);
    }

    static string HashParticles(ParticleSystem[] pss)
    {
        var sb = new StringBuilder();
        foreach (var ps in pss)
        {
            int cap = Mathf.Clamp(ps.main.maxParticles, 64, 200000);
            var buf = new ParticleSystem.Particle[cap];
            int n = ps.GetParticles(buf);
            sb.Append(n).Append(';');
            for (int i = 0; i < n; i++)
            {
                var p = buf[i];
                sb.Append(p.position.x.ToString("F5")).Append(',')
                  .Append(p.position.y.ToString("F5")).Append(',')
                  .Append(p.position.z.ToString("F5")).Append(',')
                  .Append(p.startSize.ToString("F5")).Append(',')
                  .Append(p.remainingLifetime.ToString("F5")).Append(',')
                  .Append(p.rotation.ToString("F5")).Append(',')
                  .Append(p.velocity.x.ToString("F5")).Append(',')
                  .Append(p.velocity.y.ToString("F5")).Append(',')
                  .Append(p.velocity.z.ToString("F5")).Append(',')
                  .Append(p.startColor.r.ToString("F3")).Append(',')
                  .Append(p.startColor.a.ToString("F3")).Append(',')
                  .Append(p.randomSeed).Append(';');
            }
            // 🔴 拖尾带（ribbon）**不在 `GetParticles` 里** —— 它是靠粒子**历史**撑起来的几何。
            //    只哈希粒子快照会漏掉「当前状态一样、历史不一样」这一整类差异（那正是本探针第一版漏掉的）。
            //    用**反射**读（`ParticleSystem.Trails` 的字段名跨版本改过，写死会编译不过）。
            try
            {
                var mi = typeof(ParticleSystem).GetMethod("GetTrails");
                if (mi == null) { sb.Append("T-nomethod;"); }
                else
                {
                    var pt = mi.GetParameters()[0].ParameterType;
                    var tt = pt.IsByRef ? pt.GetElementType() : pt;
                    object trails = Activator.CreateInstance(tt);
                    object[] args = { trails };
                    mi.Invoke(ps, args);
                    if (pt.IsByRef) trails = args[0];
                    var pos = tt.GetField("positions")?.GetValue(trails) as Vector3[];
                    sb.Append("T").Append(pos?.Length ?? -1).Append(';');
                    if (pos != null)
                        foreach (var v in pos)
                            sb.Append(v.x.ToString("F5")).Append(',')
                              .Append(v.y.ToString("F5")).Append(',')
                              .Append(v.z.ToString("F5")).Append(';');
                    var wid = tt.GetField("widths")?.GetValue(trails) as float[];
                    if (wid != null)
                        foreach (var w in wid) sb.Append(w.ToString("F5")).Append(',');
                }
            }
            catch (Exception e) { sb.Append("T? ").Append(e.GetType().Name).Append(';'); }
        }
        using (var md5 = MD5.Create())
            return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())))
                                .Replace("-", "").Substring(0, 12);
    }

    static Camera MakeCamera(GameObject root)
    {
        Bounds b = new Bounds(Vector3.zero, Vector3.one);
        bool first = true;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
        }
        float radius = Mathf.Max(0.5f, b.extents.magnitude);
        float fov = 40f;
        float dist = radius / Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f) * 1.15f;
        var go = new GameObject("Cam");
        var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Bg;
        cam.fieldOfView = fov;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = radius * 100f;
        cam.transform.position = b.center + new Vector3(0f, 0f, -dist);
        cam.transform.LookAt(b.center);
        cam.allowHDR = false;
        return cam;
    }

    static void Render(Camera cam, out int lit, out double sum, out string pxHash, bool pinTime = false,
                       string pngPath = null, bool destroyCam = true, bool freshRT = false)
    {
        // 🔬 关键：`cam.Render()` **之前**钉一次时间（若引擎在渲染时覆盖它，这次实验就会显示「没用」——
        //    那本身也是结论：说明得换别的办法，例如把 `Time.timeScale` 冻住或直接改 shader 读的时间源）。
        if (pinTime) PinTimeGlobals(10.0f);

        var rt = freshRT
            ? new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32)
            : RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        if (freshRT) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
        else RenderTexture.ReleaseTemporary(rt);

        var px = tex.GetPixels32();
        lit = 0; sum = 0;
        var bytes = new byte[px.Length * 3];
        for (int i = 0; i < px.Length; i++)
        {
            int d = Mathf.Abs(px[i].r - (int)(Bg.r * 255)) + Mathf.Abs(px[i].g - (int)(Bg.g * 255))
                  + Mathf.Abs(px[i].b - (int)(Bg.b * 255));
            if (d > 3) lit++;
            sum += d;
            bytes[i * 3] = px[i].r; bytes[i * 3 + 1] = px[i].g; bytes[i * 3 + 2] = px[i].b;
        }
        using (var md5 = MD5.Create())
            pxHash = BitConverter.ToString(md5.ComputeHash(bytes)).Replace("-", "").Substring(0, 12);
        if (!string.IsNullOrEmpty(pngPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(pngPath));
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
        }
        UnityEngine.Object.DestroyImmediate(tex);
        if (destroyCam) UnityEngine.Object.DestroyImmediate(cam.gameObject);
    }
}
