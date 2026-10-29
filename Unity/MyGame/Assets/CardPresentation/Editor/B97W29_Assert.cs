// B97W29_Assert.cs — 第九十七会话 · 波 1 补位 · 执行代理 `B97_W29`（工具 / 着色器线）
//
// ============================ 它盯的是什么 ============================
// 一件活：把 `B97_W23` 查实的「卡包 shader 运行时装载」那三处补丁落到实处，让
// `WarpforgeShaderLoader.TryGetShader("Everguild/Cards/Shader Graphs/Booster Pack")` **在全工程可用**
// —— 也就是 16 个 `Card Booster Pack*` 材质不再只有「经 prefab 传递」一条取件路。
//
// 判据（⛔ 别重新设计）→ `资料/完全复刻/报告/B97_W23.md` §一 / §二 / §四·1：
//   · shader **没有 `.shader` 源**（ShaderGraph 生成体，`m_Script = null`）⇒ ⛔ 抄不成工程资产；
//   · 17 棵 prefab 的产物 `wf_boosters_extra.bundle` 的 `m_Container`(38) / `m_PreloadTable`(19)
//     **不含 Shader** ⇒ `LoadAsset<Shader>` / `LoadAllAssets<Shader>` 两条都取不到；
//   · ⇒ 走 `工具/extract_missing_shaders.py --booster-shaders` 单独打 `wf_booster_shaders.bundle`
//     （容器键 = **shader 名**，只有 1 个 Shader + 1 个 AssetBundle 对象）。
//
// ============================ 怎么跑（⚠️ 本代理无权跑 Unity，本轮未跑）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B97W29Assert.Run -logFile "d:/4/_tmp_view/b97w29.log"
//   判绿 = 小结那行 `✗` 为 0。
//   ⚠️ 与 `B97W23Assert.Run` **互补、不重叠**：那支盯「经 prefab 传递那条路」（prefab / 材质 / 4 张贴图 /
//      模板材质 `SetPass`），本支盯「`WarpforgeShaderLoader` + `WarpforgeShaderMap` 这条新路」
//      ＋「两条路并存不互相顶掉」。⛔ 别再往本类里抄一遍 prefab / 贴图那些断言。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · 把 `WarpforgeShaderLoader.BoosterShaderBundleRelPath` 那一行 `LoadOne(...)` 删掉 ⇒ 第 2 段全红。
//   · 把 `wf_booster_shaders.bundle` 换成源包的原始拷贝（容器 0 条）⇒ 产物包那条红（字节数也对不上）。
//   · 把 `WarpforgeShaderMap.UseOriginal` 里那条注释掉 ⇒ 第 2 段 `白名单` / `TryResolve` 两条红。
//   · 重跑 `--booster-shaders` 时源包变了（多/少一个 `Booster Pack` shader）⇒ 第 1 段的条数断言红。
//   · 两个包的内层 CAB 名撞车（有人在 `repack` 里改了内层名）⇒ 第 3 段「两条路都在」那条红
//     （Unity 会整包拒收，`BoosterPackAssets.TryEnsureLoaded` 或 loader 那侧必然失败）。
using System;
using System.IO;
using UnityEngine;
using CardPresentation;
using WarpforgeVFX;

public static class B97W29Assert
{
    const string P = "B97W29 ";

    /// <summary>本件新增的那个包（逐字照 `WarpforgeShaderLoader.BoosterShaderBundleRelPath`）。</summary>
    const string BoosterShaderBundleRelPath = "WarpforgeVFX/wf_booster_shaders.bundle";

    /// <summary>原版 shader 名（逐字，`m_ParsedForm.m_Name`；与 `BoosterPackAssets.ShaderName` 同一串）。</summary>
    const string ShaderName = "Everguild/Cards/Shader Graphs/Booster Pack";

    /// <summary>**重打成独立包时的实测字节数**（2026-10-29 本代理实跑：`repack()` 原样调用，产物 73,826 B ·
    /// 容器 1 条 · 预载 1 条）。⚠️ 若源包被换版，这个数会变 —— 那时**先核** `--check` 的条数，再改这里。</summary>
    const long ExpectedBundleBytes = 73826L;

    /// <summary>本 shader 的属性表条数（`m_PropInfo.m_Props[]` = 17 条，见报告 §二·2.1）。
    /// 用它判「重打没把 `compressedBlob` 截断」（截断的 shader 属性表会变短/变空）。</summary>
    const int ExpectedPropertyCount = 17;

    static int _ok, _bad;

    static void Check(bool cond, string label)
    {
        if (cond) { _ok++; } else { _bad++; }
        Debug.Log(P + (cond ? "✓ " : "✗ ") + label);
    }

    public static void Run()
    {
        _ok = 0; _bad = 0;
        Debug.Log(P + "=== 卡包 shader「独立包 + 白名单」自检 开始 ===");

        // ---------------------------------------------------------------- 1 · 产物包在盘上
        var path = Path.Combine(Application.streamingAssetsPath, BoosterShaderBundleRelPath);
        bool exists = File.Exists(path);
        Check(exists, "产物包在盘上：" + path);
        if (exists)
        {
            long size = 0;
            try { size = new FileInfo(path).Length; } catch (Exception e) { Debug.Log(P + "  取文件大小抛异常：" + e.Message); }
            Check(size == ExpectedBundleBytes,
                  "产物字节数与实跑一致（期望 " + ExpectedBundleBytes + "，实得 " + size + "）");
        }

        // ---------------------------------------------------------------- 2 · 新路：loader + 白名单
        Check(WarpforgeShaderLoader.Ready, "`WarpforgeShaderLoader.Ready`（至少有一个包加载成功）");

        Shader byLoader;
        bool haveLoader = WarpforgeShaderLoader.TryGetShader(ShaderName, out byLoader) && byLoader != null;
        Check(haveLoader, "`TryGetShader(\"" + ShaderName + "\")` 取得回来（🔴 本件要的就是这一条）");

        bool inUseOriginal = WarpforgeShaderMap.UseOriginal.Contains(ShaderName);
        Check(inUseOriginal, "`WarpforgeShaderMap.UseOriginal` 收了这个名字（现读 " + WarpforgeShaderMap.UseOriginal.Count + " 条）");

        if (!haveLoader && inUseOriginal)
        {
            // 白名单收了名、但 loader 取不到 ⇒ 一定是包没加载 / 容器键不对 —— 出声，别让它静默。
            Debug.Log(P + "  ⚠️ 白名单有这条、loader 却取不到 ⇒ 先看 `wf_booster_shaders.bundle` 是否在盘上、"
                      + "以及 `EnsureLoaded` 里 `LoadOne(BoosterShaderBundleRelPath, …)` 那一行还在不在。");
        }

        if (haveLoader)
        {
            Check(byLoader.name == ShaderName,
                  "shader.name 逐字等于 `" + ShaderName + "`（实得 `" + byLoader.name + "`）");
            Check(byLoader.name != "Hidden/InternalErrorShader", "没掉进 `Hidden/InternalErrorShader`");

            // 🔴 **属性表** = 判「重打没截断 `compressedBlob`」的最便宜判据（报告 §二·2.1 实读 17 条：
            //    `_MainTex` `_Normal_map` `_MatCap` `_MatCapIntensity` … + 3 条 `unity_Lightmaps*`）。
            //    ⚠️ 条数**只打印、不作等值断言** —— `GetPropertyCount()` 可能不含内部隐藏项，
            //    实得的数第一次跑出来才知道（W23 / 本代理都没跑过 Unity）。**断言靠下面那几个名字**。
            int props = -1;
            try { props = byLoader.GetPropertyCount(); } catch (Exception e) { Debug.Log(P + "  GetPropertyCount 抛异常：" + e.Message); }
            Check(props > 0, "属性表非空（实得 " + props + " 条；判据文档实读 " + ExpectedPropertyCount + " 条 —— 条数不等 ≠ 错，看名字）");

            var propNames = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; props > 0 && i < props; i++)
            {
                string n = null;
                try { n = byLoader.GetPropertyName(i); } catch { }
                if (!string.IsNullOrEmpty(n)) propNames.Add(n);
            }
            Debug.Log(P + "  属性表逐个名：" + string.Join(", ", new System.Collections.Generic.List<string>(propNames)));
            foreach (var slot in new[] { "_MainTex", "_ExplodeTex", "_MatCap", "_Normal_map",
                                         "_ExplodeRange", "_MatCapIntensity", "_InflateMaskStrength" })
                Check(propNames.Contains(slot), "属性表里有 `" + slot + "`");

            // 白名单那条链走到底：`TryResolve` 必须给出**同一份原件**（不是自建近似、不是 null）。
            Shader resolved; string source;
            bool ok = WarpforgeShaderMap.TryResolve(ShaderName, out resolved, out source);
            Check(ok && resolved != null, "`WarpforgeShaderMap.TryResolve` 解析成功（来源 = " + (source ?? "<null>") + "）");
            if (ok && resolved != null)
            {
                Check(resolved.name == ShaderName, "`TryResolve` 拿到的是原版那一条（实得 `" + resolved.name + "`）");
                Debug.Log(P + "  （参考）`TryResolve` 与 `TryGetShader` 是否同一个对象："
                          + (ReferenceEquals(resolved, byLoader) ? "是" : "否（不同包各一份序列化对象，正常）"));
            }

            // ⚠️ `isSupported` 只打印、不作断言 —— 本工程口径是「判据看 SetPass」（同 `B97W23Assert`）。
            Debug.Log(P + "  （参考，不作判据）`shader.isSupported` = " + byLoader.isSupported);

            // ------------------------------------------------------------ 2b · SetPass（本件唯一「只能跑 Unity 才验」的那条）
            // ⚠️ 与 `B97W23Assert` 第 4 段**不是同一件事**：那边 SetPass 的是**经 prefab 传递**拿到的那份材质；
            //    这里是**独立包里那份 shader 对象**（另一份序列化体）—— 报告 §六·1 那条「未跑 Unity，未知」正是指它。
            Material probe = null;
            try
            {
                probe = new Material(byLoader);
                bool setPass = probe.SetPass(0);
                Check(setPass, "独立包里那份 shader 的 `SetPass(0)` = true（🔴 报告 §六·1 那条未知，就靠这一条结）");
            }
            catch (Exception e)
            {
                _bad++;
                Debug.Log(P + "✗ `new Material(独立包 shader).SetPass(0)` 抛异常：" + e.Message);
            }
            finally
            {
                if (probe != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(probe);
                    else UnityEngine.Object.DestroyImmediate(probe);
                }
            }
        }

        // ---------------------------------------------------------------- 3 · 两条取件路并存（不互相顶掉）
        {
            // 判据：两个包的**内层 CAB 名必须不同**（`CAB-wfboostersextra` vs `CAB-68791fd6…`）⇒ 同时加载不会
            // 被 Unity 判成「同一个包」。这里不做「起两个进程」，而是在两个都加载的状态下核对各自都还取得回来。
            bool prefabsOk = BoosterPackAssets.TryEnsureLoaded();
            Check(prefabsOk, "`BoosterPackAssets.TryEnsureLoaded()`（= 17 棵 prefab 那个包）成功");

            if (prefabsOk)
            {
                GameObject standard;
                Check(BoosterPackAssets.TryGetPrefab(BoosterPackAssets.StandardPrefab, out standard) && standard != null,
                      "prefab 那条路仍取得回 `" + BoosterPackAssets.StandardPrefab + "`（两个包同时在场不互相顶掉）");

                Shader viaPrefab;
                bool havePrefabShader = BoosterPackAssets.TryGetShader(out viaPrefab) && viaPrefab != null;
                Check(havePrefabShader, "`BoosterPackAssets.TryGetShader`（经 prefab 传递那条路）仍取得回来");
                if (havePrefabShader)
                    Check(viaPrefab.name == ShaderName, "两条路拿到的是**同一个 shader 名**（实得 `" + viaPrefab.name + "`）");

                if (havePrefabShader && haveLoader)
                    Debug.Log(P + "  （参考）两条路是同一个对象吗："
                              + (ReferenceEquals(viaPrefab, byLoader) ? "是" : "否（不同包各一份序列化对象，正常）")
                              + " · 属性表条数 " + viaPrefab.GetPropertyCount() + " vs " + byLoader.GetPropertyCount());

                // 两个包都加载之后再问一次 loader —— 若后加载的包把 `_byName` 顶掉了，这里会变红。
                Shader again;
                Check(WarpforgeShaderLoader.TryGetShader(ShaderName, out again) && again != null,
                      "两个包都加载之后 `TryGetShader` 仍取得回来（`_byName` 没被顶掉）");
            }
        }

        Debug.Log(P + "=== 小结：" + _ok + " 通过 / " + _bad + " 失败 ===");
    }
}
