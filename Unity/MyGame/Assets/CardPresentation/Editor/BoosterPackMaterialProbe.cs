// BoosterPackMaterialProbe.cs — 卡包「粒子材质」分包后的 **Unity 侧探针**（2026-10-30 · `B102_D10` 的下游）
//
// ============================ 它验的是什么 ============================
// 2026-10-29 的做法是「把 16 个材质 + 4 个 shader 并进**同一个产物**的兄弟 CAB」，并把引用改写成
// `archive:/CAB-boostermatsdup/CAB-boostermatsdup`（**同包兄弟 CAB**）。真机读数（`_tmp_view/shell.log:143201`）：
//   渲染器 350 · 材质槽 389 · **空槽 200** · **shader 坏 172**
// ⇒ **Unity 运行时不认「同一个 bundle 里的兄弟 CAB」的引用**（189 个 `m_FileID=0` 同 CAB 槽全部正常；
//    两个数字逐槽对得上）。判据与对账 → `资料/完全复刻/报告/B102_D10_卡包材质定性.md`。
//
// 2026-10-30 改成**照原版分包**：6 批材质/shader 各自写成独立依赖包
//   `StreamingAssets/WarpforgeVFX/wf_booster_{matsdup,matsbs,matsmenu,matsfont,fxshaders,fxbuiltin}.bundle`，
//   主产物的引用仍是 `archive:/CAB-booster*/CAB-booster*` —— 但这次那些 CAB **在别的 bundle 里**，
//   与原版 `boosterpacks_assets_all.bundle` 的 10 条 `externals` 逐字同形。
//
// 本探针是这条路唯一的前置闸：**先把 7 只包都加载，再看槽位还空不空**。
//   · 绿 = 「独立 bundle + `archive:/<内层CAB>/<内层CAB>` 引用」在本运行时成立 ⇒ 才轮到改运行时
//     （`Core/BoosterPackAssets.cs` 去加载那 6 只依赖包）。
//   · 红（仍是 200）= 这条路在本运行时也不成立 ⇒ **如实打红**，改走「单 CAB 搬对象」那条（报告 §3.2）。
//
// ============================ 怎么判（数字口径与 B97W32 / ShellScene 一致）============================
//   · 空槽：`renderer.sharedMaterials[i] == null` 的个数（基线 **200**）；
//   · 坏 shader：材质非空但 `mat.shader == null` 或 `mat.shader.name == "Hidden/InternalErrorShader"`（基线 **172**）；
//   · 统计口径 = **全部 17 棵 prefab**（`LoadAllAssets<GameObject>()`），这样才与 350/389 对得上；
//     另外单独给一行「只算 `Booster Pack Standard`」（任务的判据点）。
//
// 用法（由调度台跑；⚠️ 本文件作者没跑 Unity）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod BoosterPackMaterialProbe.Run -logFile "d:/4/_tmp_view/bpm.log"
//   判绿红看最后那行 `BP === … ===`；进程退出码同样 0/1。
//
// ============================ 依赖包表 ============================
// ⚠️ 权威表在 `工具/extract_missing_shaders.py` 的 `BOOSTER_IMPORTS[].dep`；本表是探针用的副本
//    （与 `ArenaPrefabProbe.Roots` 同一个老问题：两处写同一张表 = 迟早不一致；改一处必须改两处）。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>类名 == 文件名（Unity 只为「类名 == 文件名」的类建脚本资产；否则场景存盘会烘成 `!u!115` 存根）。</summary>
public static class BoosterPackMaterialProbe
{
    const string P = "BP ";
    const string Dir = "Assets/StreamingAssets/WarpforgeVFX";

    /// <summary>主产物（`repack_tree` 打的 17 棵 prefab + 2 条 clip）。</summary>
    const string MainBundle = "wf_boosters_extra.bundle";

    /// <summary>任务的判据点：这棵 prefab 上就有那 16 个材质里的若干个。</summary>
    const string StandardPrefab = "Booster Pack Standard";

    /// <summary>6 只依赖包（`BOOSTER_IMPORTS[].dep` 的探针副本；**内层 CAB 名故意改过防撞名**）。</summary>
    static readonly string[][] DepBundles =
    {
        // 文件名                                    内层 CAB（判据里点名用）
        new[] { "wf_booster_matsdup.bundle",      "CAB-boostermatsdup"   },
        new[] { "wf_booster_matsbs.bundle",       "CAB-boostermatsbs"    },
        new[] { "wf_booster_matsmenu.bundle",     "CAB-boostermatsmenu"  },
        new[] { "wf_booster_matsfont.bundle",     "CAB-boostermatsfont"  },
        new[] { "wf_booster_fxshaders.bundle",    "CAB-boosterfxshaders" },
        new[] { "wf_booster_fxbuiltin.bundle",    "CAB-boosterfxbuiltin" },
    };

    /// <summary>改之前（并把引用指到**同包兄弟 CAB** 之后）的真机基线 —— 就是本探针要打掉的数。
    /// 出处 `_tmp_view/shell.log:143201`「渲染器 350 · 材质槽 389 · 空槽 200 · shader 坏 172」。</summary>
    const int BaselineNullSlots = 200;
    const int BaselineBadShader = 172;

    static int _ok, _bad;

    static void Check(bool cond, string label)
    {
        if (cond) { _ok++; Debug.Log(P + "✓ " + label); }
        else { _bad++; Debug.LogError(P + "✗ " + label); }
    }

    public static void Run()
    {
        _ok = 0; _bad = 0;
        Debug.Log(P + "=== 卡包粒子材质「独立依赖包」探针 开始 ===");

        // ---------------------------------------------------------------- 1 · 六只依赖包先加载（保持不卸载）
        int depOk = 0;
        foreach (var dep in DepBundles)
        {
            string path = Path.Combine(Dir, dep[0]);
            if (!File.Exists(path))
            {
                Debug.LogError(P + "✗ 找不到依赖包 `" + dep[0] + "` —— 先跑 "
                             + "`python -I -X utf8 工具/extract_missing_shaders.py --booster-mats`"
                             + "（它除了主产物还会写 6 只 `wf_booster_*.bundle`）");
                continue;
            }
            AssetBundle b = null;
            try { b = AssetBundle.LoadFromFile(path); }
            catch (Exception e) { Debug.LogError(P + "✗ `" + dep[0] + "` LoadFromFile 抛异常：" + e.Message); }
            if (b == null)
            {
                // 同一份内容已在进程里加载过时 LoadFromFile 返回 null（Unity 限制）—— 依赖包没有可认领的容器键
                // （容器里只有材质），所以这里**只能出声**，别假装加载上了。
                Debug.LogError(P + "✗ `" + dep[0] + "` LoadFromFile 返回 null"
                             + "（同内容已在进程里加载过？本探针是独立进程，正常不该出现）");
                continue;
            }
            depOk++;
            var names = b.GetAllAssetNames();
            Debug.Log(P + "依赖包 ✓ `" + dep[0] + "`（内层 `" + dep[1] + "`）· 容器 " + names.Length + " 条");
        }
        Check(depOk == DepBundles.Length, "6 只依赖包全部加载（实得 " + depOk + "/" + DepBundles.Length + "）");

        // ---------------------------------------------------------------- 2 · 主产物加载
        string mainPath = Path.Combine(Dir, MainBundle);
        if (!File.Exists(mainPath))
        {
            Debug.LogError(P + "✗ 找不到主产物 `" + MainBundle + "` —— 先跑 `--booster-mats`");
            Done(1);
            return;
        }
        AssetBundle main = null;
        try { main = AssetBundle.LoadFromFile(mainPath); }
        catch (Exception e) { Debug.LogError(P + "✗ 主产物 LoadFromFile 抛异常：" + e.Message); }
        if (main == null)
        {
            Debug.LogError(P + "✗ 主产物 LoadFromFile 返回 null（同内容已在进程里加载过？）");
            Done(1);
            return;
        }
        Debug.Log(P + "主产物 ✓ `" + MainBundle + "`（" + new FileInfo(mainPath).Length + " B）· 容器 "
                  + main.GetAllAssetNames().Length + " 条");

        // ---------------------------------------------------------------- 3 · 任务的判据点：标准 prefab
        GameObject std = null;
        try { std = main.LoadAsset<GameObject>(StandardPrefab); }
        catch (Exception e) { Debug.LogError(P + "✗ LoadAsset<GameObject>(\"" + StandardPrefab + "\") 抛异常：" + e.Message); }
        Check(std != null, "`LoadAsset<GameObject>(\"" + StandardPrefab + "\")` 非 null（null = 主包本身坏了/被拒收）");
        if (std != null)
        {
            int n0, s0, bad0, null0 = 0;
            var badNames0 = new List<string>();
            CountSlots(new[] { std }, out n0, out s0, out bad0, badNames0, ref null0);
            Debug.Log(P + "只算 `" + StandardPrefab + "`：渲染器 " + n0 + " · 材质槽 " + s0
                      + " · 空槽 " + null0 + " · shader 坏 " + bad0);
        }

        // ---------------------------------------------------------------- 4 · 全量（17 棵）—— 与 350/389/200/172 同一个口径
        int renderers = 0, slots = 0, nullSlots = 0, badShader = 0;
        var badNames = new List<string>();
        GameObject[] all = null;
        try { all = main.LoadAllAssets<GameObject>(); }
        catch (Exception e) { Debug.LogError(P + "✗ `LoadAllAssets<GameObject>()` 抛异常：" + e.Message); }
        int prefabCount = all == null ? 0 : all.Length;
        if (prefabCount == 0 && std != null)
        {
            // 兜底：`LoadAllAssets` 只覆盖容器里登记过的资产；万一这条在本 build 上取不到，至少把标准那棵算进去。
            // ⚠️ 此时下面的「全包统计」就只是 1 棵 —— 与 350/389 那个基线**不可比**，日志里会点出来。
            Debug.LogError(P + "⚠️ `LoadAllAssets<GameObject>()` 返回 0 棵 ⇒ 只统计 `" + StandardPrefab
                         + "`（此时与基线 350/389 不可比）");
            all = new[] { std };
            prefabCount = 1;
        }
        CountSlots(all, out renderers, out slots, out badShader, badNames, ref nullSlots);

        Debug.Log(P + "全包统计（" + prefabCount + " 棵 prefab）：渲染器 " + renderers + " · 材质槽 " + slots
                  + " · **空槽 " + nullSlots + "** · **shader 坏 " + badShader + "**   〔基线 空槽 "
                  + BaselineNullSlots + " / 坏 " + BaselineBadShader + "〕");

        // ---------------------------------------------------------------- 5 · 前 20 个坏槽（点名）
        int show = Math.Min(20, badNames.Count);
        for (int i = 0; i < show; i++) Debug.Log(P + "  坏槽[" + (i + 1) + "] " + badNames[i]);
        if (badNames.Count > show) Debug.Log(P + "  …另有 " + (badNames.Count - show) + " 个未列出");

        // ---------------------------------------------------------------- 6 · 判定
        Check(nullSlots == 0, "材质槽**没有一个**是 null（基线 " + BaselineNullSlots + "；实得 " + nullSlots + "）");
        Check(badShader == 0, "材质 shader 全部可取且不是 `Hidden/InternalErrorShader`（基线 "
                              + BaselineBadShader + "；实得 " + badShader + "）");

        if (nullSlots == 0 && badShader == 0)
        {
            Debug.Log(P + "判定：**绿** —— 「独立 bundle + `archive:/<内层CAB>/<内层CAB>` 引用」在本运行时成立。"
                      + "下一步才轮到改 `Core/BoosterPackAssets.cs`：在 LoadFromFile 主包之前把那 6 只依赖包加载好并保持不卸载。");
        }
        else if (nullSlots == BaselineNullSlots && badShader == BaselineBadShader)
        {
            Debug.LogError(P + "判定：**红 —— 这条路在本运行时也不成立**。空槽/坏 shader 与「并进同包」时**一模一样**"
                         + "（" + nullSlots + "/" + badShader + "）⇒ 独立 bundle + `archive:/` 引用没被解到。"
                         + "先查三件：① 6 只依赖包是不是真加载上了（上面每一行）；② 内层 CAB 名有没有撞名被拒收"
                         + "（日志里 `another AssetBundle with the same files is already loaded`）；"
                         + "③ 再退到报告 §3.2 的「单 CAB 搬对象」那条路（⚠️ 那条在 `--single-cab` 上原生崩溃过，先查机制）。");
        }
        else
        {
            Debug.LogError(P + "判定：**红 —— 只对了一部分**（空槽 " + nullSlots + "/基线 " + BaselineNullSlots
                         + " · 坏 shader " + badShader + "/基线 " + BaselineBadShader + "）⇒ 引用解到了一些、另一些没解到；"
                         + "看上面「前 20 个坏槽」是不是集中在一只依赖包上（那只包可能根本没加载上）。");
        }

        Debug.Log(P + "=== " + _ok + " 通过 / " + _bad + " 失败 ===");
        Done(_bad == 0 ? 0 : 1);
    }

    /// <summary>数 `prefabs` 下所有 `Renderer` 的材质槽。`nullSlots` 以 `ref` 传，便于「只算标准 prefab」那一段
    /// 单独叫它时不覆盖全量计数。</summary>
    static void CountSlots(GameObject[] prefabs, out int renderers, out int slots, out int badShader,
                           List<string> badNames, ref int nullSlots)
    {
        renderers = 0; slots = 0; badShader = 0;
        if (prefabs == null) return;
        for (int i = 0; i < prefabs.Length; i++)
        {
            var go = prefabs[i];
            if (go == null) continue;
            var rs = go.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < rs.Length; r++)
            {
                renderers++;
                Material[] ms;
                try { ms = rs[r].sharedMaterials; }
                catch (Exception e)
                {
                    Debug.LogError(P + "  `sharedMaterials` 抛异常（" + go.name + "）：" + e.Message);
                    continue;
                }
                for (int m = 0; m < ms.Length; m++)
                {
                    slots++;
                    var mat = ms[m];
                    if (mat == null)
                    {
                        nullSlots++;
                        if (badNames != null && badNames.Count < 200)
                            badNames.Add("null · " + go.name + " / " + rs[r].GetType().Name + "[" + r + "].slot" + m);
                        continue;
                    }
                    if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                    {
                        badShader++;
                        if (badNames != null && badNames.Count < 200)
                            badNames.Add("坏shader · " + go.name + " → 材质 `" + mat.name + "` shader="
                                         + (mat.shader == null ? "<null>" : mat.shader.name));
                    }
                }
            }
        }
    }

    /// <summary>`nullSlots` 从 0 起算的便捷重载（「只算标准 prefab」那一段用）。</summary>
    static void CountSlots(GameObject[] prefabs, out int renderers, out int slots, out int badShader,
                           List<string> badNames)
    {
        int zero = 0;
        CountSlots(prefabs, out renderers, out slots, out badShader, badNames, ref zero);
    }

    static void Done(int code)
    {
        if (Application.isBatchMode) EditorApplication.Exit(code);
    }
}
