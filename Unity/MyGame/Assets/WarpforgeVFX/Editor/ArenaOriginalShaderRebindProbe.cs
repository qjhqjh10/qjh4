// ArenaOriginalShaderRebindProbe.cs — 战场线 T00·P0-2 的验收探针（2026-10-23）
//
// 验什么（做完之后应该 37/37 解析到 shader、9 块地面不再是品红）：
//   A 组 · 索引表 vs 正本 tsv：18 行 / 17 个 shader 名 / 37 个材质，且**每个 `.mat` 的 `m_Shader` guid
//        逐条等于它那一行的 `old_guid`**（否则「材质名 → shader 名」这张表认错了人）。
//   B 组 · 37 个材质**逐个** `WarpforgeShaderMap.TryResolve(原版名)` ⇒ 必须全部拿到 shader（37/37）。
//        ⚠️ 其中 5 个（`Hidden_VFX_Aeldari Dust…` + 4 个 `Hidden_VFX_Aeldari Portal…`）由 **VFX Graph 资产**
//        引用，13 颗 Scenario prefab 的树里**一个都碰不到** ⇒ 只能在这一组按「材质资产级」验。
//   C 组 · 13 颗 prefab：根上有 `ArenaOriginalShaderRebind` + `targets` 与 `shaderNames` 对得上；
//        实例化后调 `Rebind()`（**与 `Awake` 同一个入口**）⇒ 每颗 `unresolved=0 / unknown=0`，
//        `Survey` 复查 `broken=0`。合计 **69 个目标材质槽 / 32 个不同材质**（= T00 报告 §2 的口径）。
//        再补一条「9 块地面重绑后 `_BaseMap` 非空」—— 盯「换 shader 之后属性有没有丢」。
//   D 组 · **灭自证**：把重绑出来的结果**故意打回品红**（`Hidden/InternalErrorShader`）⇒ 必须数出
//        **N = 该 prefab 的目标槽数**（>0）；再跑一次 `RebindTree` ⇒ 必须回到 0。
//        ⇒ **删掉重绑这一跳，C 组就会以「N 个 null」变红** —— 这条断言就是那个 N。
//
// 用法（主对话跑）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod ArenaOriginalShaderRebindProbe.Run -logFile -
//   判绿红：日志里 `ASR === X 过 / Y 红 ===` 那一行（逐条按行首 `ASR ✓` / `ASR ✗` 数）。
//
// ⚠️ **本探针不落盘、不碰材质资产**：重绑走的是**克隆材质**（`ArenaOriginalShaderRebind` 文件头第二节），
//   所以工程内那 37 个 `.mat` 不会被改成脏（结尾把 `EditorUtility.IsDirty` 打一行，只报不断言）。
// ⚠️ **编辑器（非 Play）下 Unity 不调 `Awake`** ⇒ C 组是**显式调 `Rebind()`**（与 `Awake` 同一个方法）。
//    「实例化就自动重绑」这一跳要进 Play / player 看日志 `[Arena/Rebind]`（本轮不跑）。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using WarpforgeVFX;

public static class ArenaOriginalShaderRebindProbe
{
    const string P = "ASR ";
    const string PrefabRoot = "Assets/WarpforgeVFX/ArenaOriginalPrefabs";

    /// <summary>T00 报告 §2 的口径（13 场 · 69 个目标材质槽 / 32 个不同材质 / 9 块地面）。</summary>
    const int ExpectSlots = 69;
    const int ExpectDistinctMats = 32;
    const int ExpectGroundMats = 9;

    static readonly string[] Arenas =
    {
        "battlearena1", "battlearena2", "battlearena3", "battlearenaaeldari",
        "battlearenaastramilitarum", "battlearenablacklegion", "battlearenadarkangels",
        "battlearenaemperorschildren", "battlearenagenestealers", "battlearenaleviathan",
        "battlearenasororitas", "battlearenaspacewolves", "battlearenatauviorla",
    };

    /// <summary>tsv 的一行 = 一个材质条目（同一个 shader 名的两行 guid 会展开成各自的材质）。</summary>
    struct TsvMat
    {
        public string rel;      // 相对 `ArenaOriginalPrefabs/` 的材质路径
        public string shader;   // 原版 shader 名
        public string guid;     // 该 `.mat` 的 `m_Shader` guid（tsv 第 1 列）
    }

    public static void Run()
    {
        int ok = 0, bad = 0;
        Debug.Log(P + "WarpforgeShaderMap：" + WarpforgeShaderMap.Describe());
        try
        {
            int rows; List<string> shaderNames; List<TsvMat> mats;
            if (!LoadTsv(out rows, out shaderNames, out mats))
            {
                Debug.LogError(P + "=== 0 过 / 1 红 ===（判据表读不到，后面的组全跳过）");
                Done(1);
                return;
            }
            CheckA_IndexVsTsv(rows, shaderNames, mats, ref ok, ref bad);
            CheckB_MaterialsResolve(mats, ref ok, ref bad);
            CheckC_Prefabs(ref ok, ref bad);
        }
        catch (Exception e)
        {
            Fail(ref bad, "探针异常：" + e);
        }
        Debug.Log(P + "=== " + ok + " 过 / " + bad + " 红 ===");
        Done(bad == 0 ? 0 : 1);
    }

    // ---------------------------------------------------------------- tsv

    static bool LoadTsv(out int rows, out List<string> shaderNames, out List<TsvMat> mats)
    {
        rows = 0; shaderNames = new List<string>(); mats = new List<TsvMat>();
        string projRoot = Path.GetDirectoryName(Application.dataPath);
        string tsvPath = Path.Combine(projRoot, ArenaOriginalShaderIndex.TsvAssetPath);
        if (!File.Exists(tsvPath)) { Debug.LogError(P + "✗ A0 找不到判据表 " + tsvPath); return false; }

        var lines = File.ReadAllLines(tsvPath);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var c = lines[i].Split('\t');
            if (c.Length < 5) { Debug.LogError(P + "✗ A1 tsv 第 " + (i + 1) + " 行只有 " + c.Length + " 列（要 5 列）"); continue; }
            rows++;
            if (!shaderNames.Contains(c[1])) shaderNames.Add(c[1]);
            foreach (var raw in c[4].Split(';'))
            {
                string rel = raw.Trim();
                if (rel.Length == 0) continue;
                mats.Add(new TsvMat { rel = rel, shader = c[1], guid = c[0].Trim() });
            }
        }
        return true;
    }

    // ---------------------------------------------------------------- A 组

    static void CheckA_IndexVsTsv(int rows, List<string> shaderNames, List<TsvMat> mats, ref int ok, ref int bad)
    {
        Pass(ref ok, "A0 判据表在：" + ArenaOriginalShaderIndex.TsvAssetPath +
                     "（行 " + rows + " · shader 名 " + shaderNames.Count + " · 材质 " + mats.Count + "）");
        Check(rows == 18, ref ok, ref bad, "A1 tsv 行数 = " + rows + "（期望 18）");

        var idxNames = new List<string>(ArenaOriginalShaderIndex.ShaderNames);
        bool sameNames = idxNames.Count == shaderNames.Count && idxNames.TrueForAll(n => shaderNames.Contains(n));
        Check(sameNames, ref ok, ref bad,
              "A2 tsv 去重后 " + shaderNames.Count + " 个 shader 名 == 索引表 " + idxNames.Count + " 个" +
              (sameNames ? "" : "（tsv 有、表里没有：" + string.Join(" | ", shaderNames.FindAll(n => !idxNames.Contains(n))) +
                                " ；表里有、tsv 没有：" + string.Join(" | ", idxNames.FindAll(n => !shaderNames.Contains(n))) + "）"));

        Check(mats.Count == 37 && ArenaOriginalShaderIndex.MaterialNames.Length == 37, ref ok, ref bad,
              "A3 材质条目 = " + mats.Count + "（tsv）/ " + ArenaOriginalShaderIndex.MaterialNames.Length +
              "（索引表），期望都是 37");

        // A4 逐条：tsv 的 (材质 → shader 名) 与索引表一致
        int mapBad = 0; string firstMapBad = null;
        foreach (var t in mats)
        {
            string matName = Path.GetFileNameWithoutExtension(t.rel);
            string idxShader;
            if (!ArenaOriginalShaderIndex.TryGetOriginalShaderName(matName, out idxShader)
                || !string.Equals(idxShader, t.shader, StringComparison.Ordinal))
            {
                mapBad++;
                if (firstMapBad == null) firstMapBad = matName + " → tsv:`" + t.shader + "` / 表:`" + idxShader + "`";
            }
        }
        Check(mapBad == 0, ref ok, ref bad,
              "A4 37 条「材质名 → shader 名」与 tsv 逐条一致" +
              (mapBad == 0 ? "" : "（" + mapBad + " 条不一致，首个：" + firstMapBad + "）"));

        // A5 逐条：`.mat` 的 `m_Shader` guid == 该行 `old_guid`（= 我们没认错材质）
        var shaderGuidRe = new Regex(@"m_Shader:\s*\{fileID:\s*-?\d+,\s*guid:\s*([0-9a-f]{32})");
        string projRoot = Path.GetDirectoryName(Application.dataPath);
        int guidOk = 0, guidBad = 0; string firstGuidBad = null;
        foreach (var t in mats)
        {
            string abs = Path.Combine(projRoot, PrefabRoot + "/" + t.rel.Replace('\\', '/'));
            if (!File.Exists(abs))
            {
                guidBad++;
                if (firstGuidBad == null) firstGuidBad = t.rel + "（文件不存在）";
                continue;
            }
            var m = shaderGuidRe.Match(File.ReadAllText(abs));
            if (!m.Success || !string.Equals(m.Groups[1].Value, t.guid, StringComparison.OrdinalIgnoreCase))
            {
                guidBad++;
                if (firstGuidBad == null)
                    firstGuidBad = t.rel + "（文件里 " + (m.Success ? m.Groups[1].Value : "<没找到 m_Shader>") +
                                   " ≠ tsv " + t.guid + "）";
            }
            else guidOk++;
        }
        Check(guidBad == 0, ref ok, ref bad,
              "A5 `.mat` 的 m_Shader guid 与 tsv 逐条相等：" + guidOk + "/" + mats.Count +
              (guidBad == 0 ? "" : "（" + guidBad + " 条不符，首个：" + firstGuidBad + "）"));
    }

    // ---------------------------------------------------------------- B 组

    static void CheckB_MaterialsResolve(List<TsvMat> mats, ref int ok, ref int bad)
    {
        int okN = 0;
        var failed = new List<string>();
        foreach (var t in mats)
        {
            string assetPath = PrefabRoot + "/" + t.rel.Replace('\\', '/');
            string matName = Path.GetFileNameWithoutExtension(t.rel);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null) { failed.Add(matName + "（材质资产加载不到）"); continue; }

            Shader sh; string source;
            bool got = WarpforgeShaderMap.TryResolve(t.shader, out sh, out source) && sh != null;
            if (!got)
            {
                failed.Add(matName + " ← `" + t.shader + "`（**仍是 null**）");
                Debug.LogError(P + "✗ B `" + matName + "`：`" + t.shader + "` **解析不到**");
                continue;
            }
            okN++;
            Debug.Log(P + "✓ B 解析到 [" + source + "] `" + sh.name + "`（isSupported=" + sh.isSupported +
                      "）← 材质 `" + matName + "`");
        }

        Check(failed.Count == 0, ref ok, ref bad,
              "B 37 个材质逐个解析到原版 shader：" + okN + "/" + mats.Count +
              (failed.Count == 0 ? "" : "\n     失败 " + failed.Count + " 条：" + string.Join("\n     ", failed)));
    }

    // ---------------------------------------------------------------- C / D 组

    static void CheckC_Prefabs(ref int ok, ref int bad)
    {
        int totalTargetSlots = 0;
        var seenMatNames = new HashSet<string>();
        var sourceMats = new HashSet<Material>();     // 工程内材质资产（用来证明没被改成脏）
        var groundNames = new HashSet<string>();
        var slotMismatch = new List<string>();
        int prefabsSeen = 0;

        foreach (var arena in Arenas)
        {
            string path = PrefabRoot + "/" + arena + "/" + arena + "__Scenario.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Fail(ref bad, "C1 `" + arena + "` prefab 加载不到：" + path); continue; }
            prefabsSeen++;

            var comp = prefab.GetComponent<ArenaOriginalShaderRebind>();
            if (comp == null)
            {
                Fail(ref bad, "C1 `" + arena + "` 的**根上没挂** `ArenaOriginalShaderRebind`（重绑这一跳不会发生）");
                continue;
            }
            if (comp.targets == null || comp.shaderNames == null
                || comp.targets.Length != comp.shaderNames.Length || comp.targets.Length == 0)
            {
                Fail(ref bad, "C1 `" + arena + "` 的 targets/shaderNames 为空或长度不等（" +
                              (comp.targets == null ? "null" : comp.targets.Length.ToString()) + " / " +
                              (comp.shaderNames == null ? "null" : comp.shaderNames.Length.ToString()) + "）");
                continue;
            }

            // targets 里每个材质 → 索引表里的名字必须一致（防 YAML 里名字写错）
            int targetBad = 0;
            for (int i = 0; i < comp.targets.Length; i++)
            {
                if (comp.targets[i] == null) { targetBad++; continue; }
                sourceMats.Add(comp.targets[i]);
                string idx;
                if (!ArenaOriginalShaderIndex.TryGetOriginalShaderName(comp.targets[i].name, out idx)
                    || !string.Equals(idx, comp.shaderNames[i], StringComparison.Ordinal)) targetBad++;
            }
            if (targetBad > 0) Fail(ref bad, "C1 `" + arena + "` 的 targets 有 " + targetBad + " 条与索引表对不上");
            else Pass(ref ok, "C1 `" + arena + "`：根上有组件，targets " + comp.targets.Length + " 条与索引表逐条一致");

            // 实例化 → 显式调 Rebind（与 Awake 同一个入口）
            var inst = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var ic = inst.GetComponent<ArenaOriginalShaderRebind>();
                if (ic == null) { Fail(ref bad, "C2 `" + arena + "` 实例上没有组件（实例化丢了？）"); continue; }

                var rep = ic.Rebind();
                if (rep.unresolved != 0 || rep.unknown != 0)
                    Fail(ref bad, "C2 `" + arena + "` 重绑有失败：" + rep);
                else
                    Pass(ref ok, "C2 `" + arena + "` 重绑：" + rep);

                var sv = ArenaOriginalShaderRebind.Survey(inst, ic.targets, ic.shaderNames);
                if (!sv.Clean) Fail(ref bad, "C2 `" + arena + "` 复查仍有坏 shader：" + sv);
                else Pass(ref ok, "C2 `" + arena + "` 复查：" + sv);

                if (rep.rebound != rep.targetSlots)
                    slotMismatch.Add(arena + "(" + rep.rebound + "/" + rep.targetSlots + ")");
                totalTargetSlots += rep.targetSlots;
                // 复查是**幂等**那一侧：重绑完目标槽应归 0（不然下一次还会再克隆一遍）
                if (sv.targetSlots != 0)
                    slotMismatch.Add(arena + "(复查仍有目标槽 " + sv.targetSlots + ")");

                // 收集：不同材质名 + 地面材质（顺带盯属性有没有丢）—— 同时**逐材质**打一行
                var perMatSlots = new Dictionary<string, int>();     // 材质名 → 槽数
                var perMatShader = new Dictionary<string, Shader>();  // 材质名 → 现在绑的 shader
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null) continue;
                        string idx;
                        if (!ArenaOriginalShaderIndex.TryGetOriginalShaderName(m.name, out idx)) continue;
                        seenMatNames.Add(m.name);
                        int c;
                        perMatSlots.TryGetValue(m.name, out c);
                        perMatSlots[m.name] = c + 1;
                        if (!perMatShader.ContainsKey(m.name)) perMatShader[m.name] = m.shader;
                        if (idx == "Everguild/Misc/Unlit shadows receiver")
                        {
                            groundNames.Add(m.name);
                            var tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                            if (tex == null)
                                Fail(ref bad, "C3 地面材质 `" + m.name + "`（" + arena +
                                              "）重绑后 `_BaseMap` 是**空的** —— 换 shader 把属性丢了");
                        }
                    }
                }
                // 「逐材质报」：这个 prefab 里每个目标材质现在绑到了谁（够不着的一律 ✗）
                foreach (var kv in perMatSlots)
                {
                    string idx;
                    ArenaOriginalShaderIndex.TryGetOriginalShaderName(kv.Key, out idx);
                    Shader got;
                    perMatShader.TryGetValue(kv.Key, out got);
                    string tail = "C 材质 `" + kv.Key + "`（" + kv.Value + " 槽）→ " +
                                  (got != null ? "`" + got.name + "`" : "**null**") + "（" + arena + "）";
                    if (got != null && got.name == idx) Pass(ref ok, "解析到 " + tail);
                    else Fail(ref bad, "**仍是 null/错**：" + tail + " —— 期望 `" + idx + "`");
                }

                // ---- D 组 · 灭自证：把重绑结果打回品红，数出来的 N =「删掉重绑会红多少个」 ----
                var err = Shader.Find("Hidden/InternalErrorShader");
                int broke = 0;
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                {
                    var arr = r.sharedMaterials;
                    bool ch = false;
                    for (int i = 0; i < arr.Length; i++)
                    {
                        var m = arr[i];
                        if (m == null) continue;
                        string idx;
                        if (!ArenaOriginalShaderIndex.TryGetOriginalShaderName(m.name, out idx)) continue;
                        try { m.shader = err; }            // err 拿不到时退 null（同样算坏）
                        catch { try { m.shader = null; } catch { } }
                        broke++; ch = true;
                    }
                    if (ch) r.sharedMaterials = arr;
                }
                var svBroken = ArenaOriginalShaderRebind.Survey(inst, ic.targets, ic.shaderNames);
                Check(broke > 0 && svBroken.broken == broke, ref ok, ref bad,
                      "D `" + arena + "` 负控：把重绑结果打回品红 ⇒ " + svBroken.broken + " 个目标槽坏（打回 " +
                      broke + " 个）—— **删掉重绑这一跳，C 组就会以这 " + broke + " 个 null 变红**");

                var rep2 = ArenaOriginalShaderRebind.RebindTree(inst);
                var sv2 = ArenaOriginalShaderRebind.Survey(inst, ic.targets, ic.shaderNames);
                Check(rep2.unresolved == 0 && rep2.unknown == 0 && sv2.Clean, ref ok, ref bad,
                      "D `" + arena + "` 负控后重绑救得回来：重绑 " + rep2.rebound + " 个槽 · 复查 " + sv2);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(inst);
            }
        }

        Check(prefabsSeen == Arenas.Length, ref ok, ref bad,
              "C0 13 颗 prefab 全部加载到：" + prefabsSeen + "/" + Arenas.Length);
        Check(slotMismatch.Count == 0, ref ok, ref bad,
              "C2 重绑数 = 重绑前的目标槽数（且复查幂等：目标槽归 0）" +
              (slotMismatch.Count == 0 ? "" : "（不一致：" + string.Join(", ", slotMismatch) + "）"));
        Check(totalTargetSlots == ExpectSlots, ref ok, ref bad,
              "C4 13 颗 prefab 的目标材质槽合计 = " + totalTargetSlots + "（期望 " + ExpectSlots + "）");
        Check(seenMatNames.Count == ExpectDistinctMats, ref ok, ref bad,
              "C4 13 颗 prefab 树里出现的不同目标材质 = " + seenMatNames.Count + "（期望 " + ExpectDistinctMats +
              "；另 5 个只在 VFX Graph 资产里，见 B 组）");
        Check(groundNames.Count == ExpectGroundMats, ref ok, ref bad,
              "C3 9 块地面（`Everguild/Misc/Unlit shadows receiver`）全部出现且 `_BaseMap` 非空：" +
              groundNames.Count + "/" + ExpectGroundMats + " → " + string.Join(" | ", groundNames));

        // 只报不断言：没把工程内 .mat 改成脏（克隆路线）
        int dirty = 0;
        foreach (var m in sourceMats) if (m != null && EditorUtility.IsDirty(m)) dirty++;
        Debug.Log(P + "i 工程内 `.mat` 被改成脏的个数 = " + dirty + "（克隆路线，期望 0；本行只报不断言）");
    }

    // ---------------------------------------------------------------- helpers

    static void Check(bool cond, ref int ok, ref int bad, string msg)
    {
        if (cond) Pass(ref ok, msg); else Fail(ref bad, msg);
    }

    static void Pass(ref int ok, string msg) { ok++; Debug.Log(P + "✓ " + msg); }

    static void Fail(ref int bad, string msg) { bad++; Debug.LogError(P + "✗ " + msg); }

    static void Done(int code)
    {
        if (Application.isBatchMode) EditorApplication.Exit(code);
    }
}
