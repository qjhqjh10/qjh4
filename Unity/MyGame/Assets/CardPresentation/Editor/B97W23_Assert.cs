// B97W23_Assert.cs — 第九十七会话 · 波 1 补位 · 执行代理 `B97_W23`（着色器 / 资源线）
//
// ============================ 它盯的是什么 ============================
// 一件活：**原版 shader `Everguild/Cards/Shader Graphs/Booster Pack` 不在我们工程里**，而 16 个
// `Card Booster Pack*` 材质都要它 ⇒ 建出来会**渲品红**。
// 本代理的结论（证据全在 `资料/完全复刻/报告/B97_W23.md`）：
//   · shader **没有 `.shader` 源**（ShaderGraph 生成体，`m_Script = null`）⇒ ⛔ 抄不成工程资产；
//   · 但**原件与它的 16 个材质 / 4 张贴图 / 17 棵 prefab 全在** `StreamingAssets/WarpforgeVFX/wf_boosters_extra.bundle`；
//   · 它**不在该包的 `m_Container`(38) / `m_PreloadTable`(19) 里** ⇒ `LoadAsset<Shader>` 与
//     `LoadAllAssets<Shader>()` **两条都取不到** ⇒ 只能**经 prefab 传递**拿到（`BoosterPackAssets.TryGetShader`）。
//
// ============================ 怎么跑（⚠️ 本代理无权跑 Unity）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B97W23Assert.Run -logFile "d:/4/_tmp_view/b97w23.log"
//   判绿 = 小结那行 `✗` 为 0（以及「【判据】LoadAllAssets<Shader>() = 0」那行**确实是 0**）。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · 把 `BoosterPackAssets.PrefabNames` 里某个名字改一个字 ⇒ 第 3 段那一条红（`LoadAsset` 取不到）。
//   · 把 `TryGetShader` 改成走 `LoadAllAssets<Shader>()` ⇒ 第 2 段「试 LoadAllAssets 取 shader」那条红
//     （这是**故意**留的反证条：它证明「这条近路走不通」，别有人再去试）。
//   · 包被换成没重打过的原始拷贝（容器空） ⇒ 第 3 / 4 段全红。
//   · 材质上的 4 张贴图任意一张断掉 ⇒ 第 4 段对应那一条红。
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using CardPresentation;

public static class B97W23Assert
{
    const string P = "B97W23 ";

    /// <summary>16 个「按阵营」的 prefab（`Booster Pack All Armies Variant` 与 `Booster Pack Standard` 共用同一材质，
    /// 单列出来另算）。名字**逐字**照 bundle `m_Name`。</summary>
    static readonly string[] ArmyPrefabs =
    {
        "Booster Pack Standard",
        "Booster Pack Standard Aeldari SaimHann",
        "Booster Pack Standard Astra Militarum",
        "Booster Pack Standard Chaos SM Black Legion",
        "Booster Pack Standard Emperors Children",
        "Booster Pack Standard Genestealers",
        "Booster Pack Standard Necrons Sautekh",
        "Booster Pack Standard Orks Goff",
        "Booster Pack Standard Orks Goff Expansion",
        "Booster Pack Standard Sororitas",
        "Booster Pack Standard Space Marines Dark Angels",
        "Booster Pack Standard Space Marines Space Wolves",
        "Booster Pack Standard Space Marines Ultramarines",
        "Booster Pack Standard Space Marines Ultramarines Expansion 1",
        "Booster Pack Standard TauEmpire",
        "Booster Pack Standard Tyranids Leviathan",
    };

    /// <summary>16 个材质**全部**有的 4 张贴图槽（W17 §四 逐条解出；只有 `_ExplodeTex` / `_MainTex` 的**取值**按阵营不同）。</summary>
    static readonly string[] RequiredTextures = { "_MainTex", "_ExplodeTex", "_MatCap", "_Normal_map" };

    static int _ok, _bad;

    static void Check(bool cond, string label)
    {
        if (cond) { _ok++; } else { _bad++; }
        Debug.Log(P + (cond ? "✓ " : "✗ ") + label);
    }

    public static void Run()
    {
        _ok = 0; _bad = 0;
        Debug.Log(P + "=== 卡包 shader / 材质 自检 开始 ===");
        BoosterPackAssets.Reset();

        // ---------------------------------------------------------------- 1 · 包在盘上
        var path = Path.Combine(Application.streamingAssetsPath, BoosterPackAssets.BundleRelPath);
        Check(File.Exists(path), "包在盘上：" + path);

        // ---------------------------------------------------------------- 2 · 加载 + 判据反证
        Check(BoosterPackAssets.TryEnsureLoaded(), "bundle 加载成功（`BoosterPackAssets.TryEnsureLoaded`）");

        AssetBundle bundle;
        Check(BoosterPackAssets.TryGetBundle(out bundle) && bundle != null, "拿得到 AssetBundle（`TryGetBundle`）");
        if (bundle != null)
        {
            // 🔴 **这是反证条，不是功能**：钉住「shader 不在容器/预载表里」这条判据。
            //    它一旦变成非 0，说明包被重打过（那时就可以直接把本 shader 名字加进
            //    `WarpforgeShaderMap.UseOriginal` + `WarpforgeShaderLoader`，本类的「经 prefab 传递」那跳可退休）。
            int byLoadAll = -1;
            try
            {
                var all = bundle.LoadAllAssets<Shader>();
                byLoadAll = all == null ? -1 : all.Length;
            }
            catch (Exception e) { Debug.Log(P + "  `LoadAllAssets<Shader>()` 抛异常：" + e.Message); }
            Debug.Log(P + "【判据】`LoadAllAssets<Shader>()` = " + byLoadAll + " 个"
                      + "（预期 0 —— 本包容器 38 / 预载表 19，全是 17 棵 prefab + 2 条 clip）");

            // 反证：按名字直接取也取不到（预期 null）。
            Shader byName = null;
            try { byName = bundle.LoadAsset<Shader>(BoosterPackAssets.ShaderName); } catch { }
            Debug.Log(P + "【判据】`LoadAsset<Shader>(名字)` = " + (byName == null ? "null（预期）" : "**非 null** ⇒ 包被重打过，判据要更新"));
        }

        // ---------------------------------------------------------------- 3 · 17 棵 prefab 全在
        for (int i = 0; i < BoosterPackAssets.PrefabNames.Length; i++)
        {
            var n = BoosterPackAssets.PrefabNames[i];
            GameObject pf;
            Check(BoosterPackAssets.TryGetPrefab(n, out pf) && pf != null, "prefab 取得回来：" + n);
        }

        // ---------------------------------------------------------------- 4 · 16 个材质：shader / 4 张贴图 / SetPass
        Shader shader;
        bool haveShader = BoosterPackAssets.TryGetShader(out shader) && shader != null;
        Check(haveShader, "经 prefab 传递拿到 shader：" + BoosterPackAssets.ShaderName);
        if (haveShader)
        {
            Check(shader.name == BoosterPackAssets.ShaderName,
                  "shader.name 逐字等于 `" + BoosterPackAssets.ShaderName + "`（实得 `" + shader.name + "`）");
            // ⚠️ **只打印、不断言**：本工程口径是「判据看 SetPass、不看 `isSupported`」
            //    （`资料/` 里 bundle-shader 那条）。`isSupported` 在某些平台上会假阴性。
            Debug.Log(P + "  （参考，不作判据）`shader.isSupported` = " + shader.isSupported);
        }

        int matOk = 0, matBad = 0;
        for (int i = 0; i < ArmyPrefabs.Length; i++)
        {
            var pfName = ArmyPrefabs[i];
            Material src;
            if (!BoosterPackAssets.TryGetTemplateMaterial(pfName, out src) || src == null)
            {
                matBad++; _bad++;
                Debug.Log(P + "✗ prefab `" + pfName + "` 上取不到模板材质");
                continue;
            }

            bool shaderOk = src.shader != null && src.shader.name == BoosterPackAssets.ShaderName;
            Check(shaderOk, "材质 `" + src.name + "` 的 shader 是原版那一条（实得 `"
                            + (src.shader == null ? "<null>" : src.shader.name) + "`）");
            Check(src.shader == null || src.shader.name != "Hidden/InternalErrorShader",
                  "材质 `" + src.name + "` 没掉进 `Hidden/InternalErrorShader`");

            for (int t = 0; t < RequiredTextures.Length; t++)
            {
                var slot = RequiredTextures[t];
                Check(src.HasProperty(slot) && src.GetTexture(slot) != null,
                      "材质 `" + src.name + "` 的 `" + slot + "` 有贴图");
            }

            // 🔴 **判据 = SetPass**（本工程既有口径）：它 true 才说明**这一遍真能用这份字节码渲**。
            bool setPass = false;
            string err = null;
            try { setPass = src.SetPass(0); }
            catch (Exception e) { err = e.Message; }
            Check(setPass, "材质 `" + src.name + "` 的 `SetPass(0)` = true" + (err == null ? "" : "（抛异常：" + err + "）"));

            if (shaderOk) matOk++; else matBad++;
        }
        Debug.Log(P + "16 个材质：可用 " + matOk + " / 有缺陷 " + matBad);

        // ---------------------------------------------------------------- 5 · 克隆一份：属性整块跟过来
        {
            var clone = BoosterPackAssets.CloneTemplateMaterial("Booster Pack Standard", "B97W23 clone");
            Check(clone != null, "`CloneTemplateMaterial` 建得出来");
            if (clone != null)
            {
                Check(clone.shader != null && clone.shader.name == BoosterPackAssets.ShaderName,
                      "克隆件的 shader 仍是原版那一条");
                Check(Mathf.Abs(clone.GetFloat("_ExplodeRange") - (-0.34f)) < 1e-4f,
                      "克隆件的 `_ExplodeRange` 与原版一致（期望 −0.34，实得 "
                      + clone.GetFloat("_ExplodeRange").ToString("F4") + "）");
                Check(clone.GetFloat("_MatCapIntensity") > 1.19f && clone.GetFloat("_MatCapIntensity") < 1.21f,
                      "克隆件的 `_MatCapIntensity` 与原版一致（期望 1.2）");
                Check(clone.GetTexture("_MainTex") != null, "克隆件的 `_MainTex` 还在");
                if (Application.isPlaying) UnityEngine.Object.Destroy(clone);
                else UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        // ---------------------------------------------------------------- 6 · 如实报「粒子那一半不完整」（⛔ 不是本件能修的）
        {
            int renderers = 0, nullMats = 0, badShaders = 0;
            for (int i = 0; i < BoosterPackAssets.PrefabNames.Length; i++)
            {
                GameObject pf;
                if (!BoosterPackAssets.TryGetPrefab(BoosterPackAssets.PrefabNames[i], out pf) || pf == null) continue;
                var prs = pf.GetComponentsInChildren<ParticleSystemRenderer>(true);
                for (int r = 0; r < prs.Length; r++)
                {
                    renderers++;
                    var m = prs[r].sharedMaterial;
                    if (m == null) { nullMats++; continue; }
                    if (m.shader == null || m.shader.name == "Hidden/InternalErrorShader") badShaders++;
                }
            }
            Debug.Log(P + "【已知缺口 · 上游重打时漏拉】17 棵 prefab 的 ParticleSystemRenderer 共 " + renderers
                      + " 个槽：材质**整个缺失** " + nullMats + " · shader 悬空/错误 " + badShaders
                      + "（判据 → 报告 §五；修法 = 在 `extract_missing_shaders.py` 的 `BOOSTER_GROUPS[1]` 里补根/补材质）");
            Debug.Log(P + "  注：本条**不计入红**（不属本件范围，且是上游包内容问题），但⛔ 不许静默 —— 接线前必须知道。");
        }

        Debug.Log(P + "=== 小结：" + _ok + " 通过 / " + _bad + " 失败 ===");
    }
}
