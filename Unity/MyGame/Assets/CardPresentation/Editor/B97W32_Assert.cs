// B97W32_Assert.cs — 第九十七会话 · 波 1 补位 · 执行代理 `B97_W32`（工具 / 资源线）
//
// ============================ 它盯的是什么 ============================
// 一件活：`B97_W23` §五·5.1 记下的那个缺口 —— `wf_boosters_extra.bundle` 里 **299 个
// `ParticleSystemRenderer` / 200 个材质槽** 指向的 16 个材质**根本不在包里**（它们的 PPtr 是
// `m_FileID 4..7 → archive:/CAB-…`，原版是 Addressables 的**依赖包**），另有 **8 个包内材质
// 的 `m_Shader` 悬空**。⇒ 开卡包「爆散/翻牌」那一半粒子会缺材质或渲错。
//
// 修法（本件已落地，改动只在工具 + 产物，⛔ 没动任何业务 `.cs`）：
//   `工具/extract_missing_shaders.py --booster-mats`（`--prefabs` 也会走同一步）：
//   ① 从 4 个源包把 16 个材质（+ 贴图）抽成独立内层 CAB；② 从 2 个源包抽 4 个 shader；
//   ③ 并进**同一个产物**并把引用改写成「包内兄弟 CAB」。
//
// 判据（⛔ 别重新设计）→ `资料/完全复刻/报告/B97_W32.md` §二/§三：
//   · 16 个材质在原版**都在**（分属 duplicateassetisolation / battlesharedresources /
//     menusharedresources / fonts 四个包）⇒ 不是「原版本身没有」；
//   · 改前实测：材质槽 **200 悬空** · 材质→shader **8 悬空**；改后 **0 / 0**（同一统计脚本）。
//
// ============================ 怎么跑（⚠️ 本代理无权跑 Unity，本轮未跑）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B97W32Assert.Run -logFile "d:/4/_tmp_view/b97w32.log"
//   判绿 = 小结那行 `✗` 为 0。
//   ⚠️ 与 `B97W23Assert` / `B97W29Assert` **互补、不重叠**：W23 第 6 段**只是打印**缺口的量
//      （不计入红）；本支把它变成**硬断言**（改前 200/8 → 改后必须 0），并补「容器/prefab 没被并坏」。
//      ⛔ 别再往本类里抄一遍 SetPass / 白名单 / 克隆材质那些断言。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · 把 `merge_imports_into_bundle(...)` 那一步跳过（只跑 `repack_tree`）⇒ 第 3 段大量 `null`
//     （实测会回到 200 个空槽）。
//   · 把 `BOOSTER_IMPORTS` 里两条 `kind="shader"` 删掉 ⇒ 第 4 段的「shader 不是 InternalErrorShader」
//     大量红（材质搬进来了、shader 仍指 `archive:/CAB-805d8e…`）。
//   · 把导入 CAB 的名字改回源包名（`CAB-805d8e…` 等）⇒ 与运行时已加载的 `wf_shaders.bundle`
//     内层同名 ⇒ Unity 判「同一个包」**整包拒收** ⇒ 第 1 段（能不能按容器键取 prefab）直接红。
//   · 把产物换回 `--booster-mats` 之前那一份（备份在 `_tmp_view/b97w32/backup/`，5,972,456 B）
//     ⇒ 第 1 段的字节数红 + 第 3 段 200 个空槽红。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CardPresentation;

public static class B97W32Assert
{
    const string P = "B97W32 ";

    const string BundleRelPath = "WarpforgeVFX/wf_boosters_extra.bundle";

    /// <summary>`--booster-mats` 之后的实测字节数（2026-10-29 本代理实跑，连跑两次 sha256 相同
    /// `7ec0508f61469252bab2508cb9e612873adff5d28bf5ac9c6e939a586c4d14e3`）。
    /// ⚠️ 改前是 **5,972,456 B**；源包换版 / 工具再改会变 —— 那时先跑 `--booster-mats --check`，再改这里。</summary>
    const long ExpectedBundleBytes = 7578817L;

    /// <summary>17 棵 prefab 的容器键（`m_Container` 38 条 = 19 资产 × 2 种写法 ⇒ 17 prefab + 2 clip）。
    /// 并完外部 CAB 之后这两张表**必须原样**（⛔ 别把材质也登记进容器：W23 §4.3 第 2 条钉的是
    /// `LoadAllAssets<Shader>()` 在这里仍然取不到东西）。</summary>
    const int ExpectedPrefabCount = 17;

    /// <summary>16 个「原本整个不在包里」的材质名（逐字，来自 `--check` 的 [P1] 行）。
    /// 改后它们应当在 prefab 的渲染器上**按名字**出现。</summary>
    static readonly string[] ImportedMaterialNames = new string[]
    {
        "Spark Additive Extra Color", "ParticlesUnlit", "Glow Rays Additive Extra Color",
        "Smoke Sprite Sheet Extra Blend", "smoke_light", "Crosshair_2_Thin", "Sororitas_Icon",
        "Hexagon_tiled", "Smoke5_extra color", "Crosshair Ring Extra", "levelup_ring",
        "Shine trail Additive Extra", "Smoke Sprite Sheet Extra Additive", "Waterfall_Clouds",
        "Aura_2", "Pragati-Regular Atlas Material",
    };

    static int _ok, _bad;

    static void Check(bool cond, string label)
    {
        if (cond) { _ok++; } else { _bad++; }
        Debug.Log(P + (cond ? "✓ " : "✗ ") + label);
    }

    public static void Run()
    {
        _ok = 0; _bad = 0;
        Debug.Log(P + "=== 卡包 17 棵 prefab「粒子材质并进产物」自检 开始 ===");

        // ---------------------------------------------------------------- 1 · 产物与容器
        var path = Path.Combine(Application.streamingAssetsPath, BundleRelPath);
        bool exists = File.Exists(path);
        Check(exists, "产物包在盘上：" + path);
        if (exists)
        {
            long size = 0;
            try { size = new FileInfo(path).Length; }
            catch (Exception e) { Debug.Log(P + "  取文件大小抛异常：" + e.Message); }
            Check(size == ExpectedBundleBytes,
                  "产物字节数 == " + ExpectedBundleBytes + "（期望改后那一份；改前是 5,972,456）—— 实得 " + size);
        }

        bool loaded = BoosterPackAssets.TryEnsureLoaded();
        Check(loaded, "包能被加载（判定 = 按容器键取得到 `Booster Pack Standard`）");

        // 17 棵 prefab 按容器键取回 —— 逐棵那个断言在 `B97W23Assert` 第 3 段已经钉过，
        // 这里只要**计数**（它同时挡「内层 CAB 撞名 ⇒ 整包被拒收」：那种情况下一个都取不到）。
        var prefabs = new List<GameObject>();
        for (int i = 0; i < BoosterPackAssets.PrefabNames.Length; i++)
        {
            GameObject pf;
            if (BoosterPackAssets.TryGetPrefab(BoosterPackAssets.PrefabNames[i], out pf) && pf != null)
                prefabs.Add(pf);
        }
        Check(prefabs.Count == ExpectedPrefabCount,
              "17 棵 prefab 全部按容器键取回（实得 " + prefabs.Count + "；0 = 整包被拒收）");

        // ---------------------------------------------------------------- 2 · 渲染器材质槽：一个都不许是 null
        int renderers = 0, slots = 0, nullSlots = 0, badShader = 0;
        var seenMatNames = new HashSet<string>();
        for (int i = 0; i < prefabs.Count; i++)
        {
            var rs = prefabs[i].GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < rs.Length; r++)
            {
                renderers++;
                var ms = rs[r].sharedMaterials;
                for (int m = 0; m < ms.Length; m++)
                {
                    slots++;
                    var mat = ms[m];
                    if (mat == null) { nullSlots++; continue; }
                    if (mat.name != null) seenMatNames.Add(mat.name);
                    if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader") badShader++;
                }
            }
        }
        Debug.Log(P + "渲染器 " + renderers + " 个 · 材质槽 " + slots + " 个 · 空槽 " + nullSlots + " · shader 坏 " + badShader);
        // 🔴 核心两条：改前实测 200 / 8。⛔ 别放宽成「少于多少」——原版这两个数就是 0。
        Check(nullSlots == 0, "材质槽**没有一个**是 null（改前实测 200 个 —— 这就是本件修的那个缺口）");
        Check(badShader == 0, "材质 shader 全部可取且不是 `Hidden/InternalErrorShader`（改前实测 8 个坏）");

        // ---------------------------------------------------------------- 3 · 那 16 个材质确实在树上
        int missing = 0;
        for (int i = 0; i < ImportedMaterialNames.Length; i++)
        {
            bool hit = seenMatNames.Contains(ImportedMaterialNames[i]);
            if (!hit) { missing++; Debug.Log(P + "  ✗ 树上没有材质：" + ImportedMaterialNames[i]); }
        }
        Check(missing == 0, "16 个「原本不在包里」的材质全部出现在渲染器上（缺 " + missing + "）");

        // ---------------------------------------------------------------- 4 · 贴图槽（14 张流式贴图搬过来之后要还在）
        int texSlots = 0, texNull = 0;
        for (int i = 0; i < prefabs.Count; i++)
        {
            var rs = prefabs[i].GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < rs.Length; r++)
            {
                var m = rs[r].sharedMaterial;
                if (m == null) continue;
                string[] names = m.GetTexturePropertyNames();
                for (int t = 0; t < names.Length; t++)
                {
                    texSlots++;
                    var tex = m.GetTexture(names[t]);
                    if (tex == null) { texNull++; Debug.Log(P + "  空贴图槽：" + m.name + "." + names[t]); }
                }
            }
        }
        Debug.Log(P + "贴图槽 " + texSlots + " 个 · 空 " + texNull + " 个");
        // ⚠️ 只断「非 null」：**流数据是否真在**这一层验不了（UnityPy 侧已逐张比过字节，见报告 §四）。
        Check(texNull == 0, "渲染器上材质的贴图槽没有一个是 null");

        // ---------------------------------------------------------------- 5 · 容器/预载表的钉子仍然成立
        AssetBundle bundle;
        if (BoosterPackAssets.TryGetBundle(out bundle) && bundle != null)
        {
            int byLoadAll = -1;
            try
            {
                var all = bundle.LoadAllAssets<Shader>();
                byLoadAll = all == null ? -1 : all.Length;
            }
            catch (Exception e) { Debug.Log(P + "  `LoadAllAssets<Shader>()` 抛异常：" + e.Message); }
            Debug.Log(P + "【判据钉子】本包 `LoadAllAssets<Shader>()` = " + byLoadAll
                      + "（设计上**必须是 0**：6 个 shader 只在次内层 CAB 里、不进容器）");
            // 🔴 本件**特意没**把材质/shader 登记进容器：容器仍是 38 条 = 17 prefab + 2 clip ×2 种写法。
            //    万一这条变成非 0，说明有人把 shader 也登记进容器了 ⇒ W23 §4.3 第 2 条的判据要跟着更新。
            Check(byLoadAll == 0, "`LoadAllAssets<Shader>()` 仍然是 0（W23 §4.3 第 2 条那条钉子）");
        }
        else
        {
            Check(false, "拿得到 AssetBundle（`TryGetBundle`）");
        }

        Debug.Log(P + "=== 小结：" + _ok + " 通过 / " + _bad + " 失败 ===");
    }

    // 🔴 2026-10-30（第一百零二会话 · 调度台）：本件此前**全仓 0 宿主**（从没被执行过）。接进 `ShellScene` 时需要
    //   两个只读口把计数并进宿主的 `_sink`，否则它的 ✗ 只印不计（= 静默）。追加在类尾 ⇒ 本文件既有账落点零漂移。
    public static int OkCount { get { return _ok; } }
    public static int BadCount { get { return _bad; } }
}
