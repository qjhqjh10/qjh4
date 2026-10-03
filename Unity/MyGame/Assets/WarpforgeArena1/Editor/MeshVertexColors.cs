// MeshVertexColors.cs — 建场期把**原版网格的顶点色**贴到我们 OBJ 的导入网格上
//
// 判据与数据 → `工具/gen_arena_vertexcolors.py`（旁挂 `arenas/<场>/<场>_vcol.json`）
//   · 原版 **834 个 Mesh 里 132 个**带 `ch3`（顶点色），我们这 697 个 OBJ 里对得上 **104 个**（8,253 个顶点）
//   · 旁挂里的 `positions` **保留原版空间、【没有】按 X 取反**（⚠️ **2026-10-04 更正**：这里原来写「已经按 X 取反」—— **是错的**）；`positions` 与 `colors` 一一对应
//   · **必须按位置匹配、不能按索引**：我们的 OBJ 是按 `g` 组拆过文件的，而且 Unity 的 OBJ 导入器
//     会 `weldVertices`（焊接并**重排顶点**）⇒ 文件里第 i 个 `v` 未必是 Unity 网格的第 i 个顶点
//
// 🔴 **为什么不能直接写 `mesh.colors`**：这 697 个 OBJ 的导入设置是 `isReadable: 0`
//   （`Mesh.isReadable == false`）⇒ 连 `mesh.vertices` 都读不了，更别说写。
//   ⇒ **两步走**（⚠️ 不能合成一步，踩过）：
//     ① `-executeMethod MeshVertexColors.EnsureReadableAll`（**单独一趟**）：把需要色的 `.obj`
//        打开 `isReadable`（`SaveAndReimport` 会重载资产 ⇒ **建场中途做会把 BuildAll 打崩**，实测 NRE/退出码 1）
//     ② `ArenaBuilder.BuildAll`：建一张**新 Mesh**（位置/法线/UV/子网格原样搬）＋ `colors`
//   ⚠️ 代价：那 104 个网格**每份变成独立网格**（本来只有 19 个网格是共享的 ⇒ 影响可忽略）；
//      生成的 Mesh 不是资产 ⇒ 会被烘进 `.unity` 场景文件（8,253 个顶点的量级，可接受）。
//   🔴 **旁挂的 `pos`/`col` 是扁平数组** —— `JsonUtility` **不支持交错数组**（写 `float[][]` 会**静默变 null**）。
using System;
using System.Collections.Generic;
using UnityEngine;

    public static class MeshVertexColors
    {
        [Serializable] public class Item
        {
            public string obj;         // 我们工程里的 .obj 文件名（含扩展名）
            public string mesh;        // 原版网格名（留档/日志用）
            public string src;         // 来自哪个 bundle（留档）
            public int n;              // 顶点数
            // 🔴 **扁平数组**（`float[]`），不是 `float[][]`：**Unity 的 `JsonUtility` 不支持交错数组**
            //   —— 写 `float[][]` 会**静默变 null**（不报错），建场期直接 NRE。2026-09-30 实测踩过。
            //   配对靠下标：第 i 个顶点 = `pos[i*3..i*3+2]` / `col[i*4..i*4+3]`
            /// <summary>xyz 依次摊平。🔴 **保留原版空间、【没有】按 X 取反**（⚠️ **2026-10-04 更正**：
            /// 这里原来写「已按 X 取反，与我们 OBJ 同一手性」—— **是错的**）。
            /// 实测：我们的 OBJ 取反过、而 **Unity 的 OBJ 导入器会再取反一次** ⇒ **Unity 网格的 X ≈ 原版**，
            /// 所以这里照原样比即可。⚠️ **2026-10-04 更正：原来这里引的两组数引错了路** —— 正确的实测是
            /// **拿 Unity 网格比：`原样 186/186 · 取反 0/186`**（`Candles 52`；`Ground Lights.0011` 是 `258/258 · 0/258`，
            /// 出处 = `资料/战场场景线_交接.md:88` 的原始 DumpMiss 输出）；
            /// 而 **`0/186 → 186/186` 那一组是「拿我们 OBJ 文件比」**（`vkeys` 自检才要临时取反）。两组数同名同量、方向相反。同批把 13 份 `_vcol.json` 的 `_schema` 一起订正了。</summary>
            public float[] pos;
            public float[] col;        // rgba 依次摊平
        }

        [Serializable] public class File
        {
            public string arena;
            public Item[] items;
            public string[] unmatched;    // 有顶点色、但我们没建出来的网格（留档）
        }

        static readonly Dictionary<string, Dictionary<string, Item>> _cache =
            new Dictionary<string, Dictionary<string, Item>>();

        static Dictionary<string, Item> Load(string scene)
        {
            Dictionary<string, Item> map;
            if (_cache.TryGetValue(scene, out map)) return map;
            map = new Dictionary<string, Item>();
            var p = SidecarPath(scene);
            if (System.IO.File.Exists(p))
            {
                var f = JsonUtility.FromJson<File>(System.IO.File.ReadAllText(p));
                if (f != null && f.items != null)
                    foreach (var it in f.items)
                        if (it != null && !string.IsNullOrEmpty(it.obj)) map[it.obj] = it;
            }
            _cache[scene] = map;
            return map;
        }

        public static string SidecarPath(string scene)
            => $"d:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas/{scene}/{scene}_vcol.json";

        /// <summary>本场旁挂里有多少条（自检用）。</summary>
        public static int Count(string scene) { return Load(scene).Count; }

        // ---- 位置 → 颜色：**按网格自身尺度自适应的最近邻** ----
        //
        // 🔴 **2026-09-30 晚踩过一个大坑，别再改回固定精度**：原来把坐标按 **1e-4** 量化成 long 当键，
        //   而实测有的网格**小到坐标 ~2e-4**（`Candles 52`，包围盒对角 0.001）⇒ 整个网格被压进 0~3
        //   这几个桶、互相撞在一起 ⇒ **170/186 个顶点找不到色**（`sororitas` 全场 2009 个）。
        //   网格尺度跨 6 个数量级（~1e-3 到 ~1e3）⇒ **必须按每个网格自己的尺度定步长**。
        //   步长 = 包围盒对角 × 1e-3（下限 1e-7），命中判据 = 最近的候选且距离 ≤ 步长。
        class PosMap
        {
            readonly Dictionary<long, List<int>> _map = new Dictionary<long, List<int>>();
            readonly Item _it;
            readonly float _step, _minX, _minY, _minZ;

            public int Dup { get; private set; }

            public PosMap(Item it)
            {
                _it = it;
                float mnx = float.MaxValue, mny = float.MaxValue, mnz = float.MaxValue;
                float mxx = float.MinValue, mxy = float.MinValue, mxz = float.MinValue;
                for (int i = 0; i < it.n; i++)
                {
                    float x = it.pos[i * 3], y = it.pos[i * 3 + 1], z = it.pos[i * 3 + 2];
                    mnx = Mathf.Min(mnx, x); mny = Mathf.Min(mny, y); mnz = Mathf.Min(mnz, z);
                    mxx = Mathf.Max(mxx, x); mxy = Mathf.Max(mxy, y); mxz = Mathf.Max(mxz, z);
                }
                _minX = mnx; _minY = mny; _minZ = mnz;
                float diag = Mathf.Sqrt((mxx - mnx) * (mxx - mnx) + (mxy - mny) * (mxy - mny) + (mxz - mnz) * (mxz - mnz));
                _step = Mathf.Max(diag * 1e-3f, 1e-7f);
                for (int i = 0; i < it.n; i++)
                {
                    long k = Bucket(it.pos[i * 3], it.pos[i * 3 + 1], it.pos[i * 3 + 2]);
                    List<int> lst;
                    if (!_map.TryGetValue(k, out lst)) { lst = new List<int>(2); _map[k] = lst; }
                    else
                    {
                        // 同一个桶里已有顶点：若颜色不同就记账（稠密网格的退化情形）
                        var a = ColorAt(lst[0]); var b = ColorAt(i);
                        if (Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b)
                            + Mathf.Abs(a.a - b.a) > 0.01f) Dup++;
                    }
                    lst.Add(i);
                }
            }

            Color ColorAt(int i)
                => new Color(_it.col[i * 4], _it.col[i * 4 + 1], _it.col[i * 4 + 2], _it.col[i * 4 + 3]);

            long Bucket(float x, float y, float z)
            {
                long bx = (long)Mathf.Floor((x - _minX) / _step);
                long by = (long)Mathf.Floor((y - _minY) / _step);
                long bz = (long)Mathf.Floor((z - _minZ) / _step);
                const long M = 1 << 21;
                return ((bx & (M - 1)) << 42) | ((by & (M - 1)) << 21) | (bz & (M - 1));
            }

            /// <summary>找最近的顶点色。命中 = 最近的候选且距离 ≤ 步长。</summary>
            public bool TryGet(Vector3 p, out Color c)
            {
                c = Color.white;
                long bx = (long)Mathf.Floor((p.x - _minX) / _step);
                long by = (long)Mathf.Floor((p.y - _minY) / _step);
                long bz = (long)Mathf.Floor((p.z - _minZ) / _step);
                float best = float.MaxValue;
                bool found = false;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            const long M = 1 << 21;
                            long k = ((((bx + dx) & (M - 1)) << 42) | (((by + dy) & (M - 1)) << 21) | ((bz + dz) & (M - 1)));
                            List<int> lst;
                            if (!_map.TryGetValue(k, out lst)) continue;
                            for (int j = 0; j < lst.Count; j++)
                            {
                                int i = lst[j];
                                float d = (p.x - _it.pos[i * 3]) * (p.x - _it.pos[i * 3])
                                        + (p.y - _it.pos[i * 3 + 1]) * (p.y - _it.pos[i * 3 + 1])
                                        + (p.z - _it.pos[i * 3 + 2]) * (p.z - _it.pos[i * 3 + 2]);
                                if (d < best) { best = d; c = ColorAt(i); found = true; }
                            }
                        }
                return found && best <= _step * _step;
            }
        }

        /// <summary>建位置索引；**旁挂字段不完整就出声返回 null**（别 NRE —— `JsonUtility` 对交错数组就是这么静默的）。</summary>
        static PosMap PosMapOf(Item it)
        {
            if (it == null || it.pos == null || it.col == null || it.n <= 0
                || it.pos.Length < it.n * 3 || it.col.Length < it.n * 4)
            {
                Debug.LogWarning($"[VCol] `{it?.obj}` 的旁挂 `pos`/`col` 不完整（n={it?.n}）—— "
                               + "重新跑一次 `python 工具/gen_arena_vertexcolors.py`");
                return null;
            }
            return new PosMap(it);
        }

        /// <summary>趁手的入口：有旁挂就返回**带顶点色的新网格**，否则原样返回 `src`。
        /// `stat.visit/colored/miss/skipNoRead` 累计给建场日志用。</summary>
        public static Mesh ApplyIfAny(string scene, string objFile, Mesh src, string goName, string objAssetPath, Stat stat)
        {
            if (src == null) return null;
            stat.visit++;
            var item = (objFile != null && Load(scene).TryGetValue(objFile, out var it)) ? it : null;
            if (item == null) { stat.noData++; return src; }
            if (src.colors != null && src.colors.Length > 0) { stat.already++; return src; }
            if (!src.isReadable)
            {
                // ⚠️ **建场期不碰导入设置** —— `ModelImporter.SaveAndReimport()` 会在**建场中途重载资产**，
                //    2026-09-30 实测直接把 `ArenaBuilder.BuildAll` 打崩（NRE ⇒ 退出码 1）。
                //    ⇒ 改成**出声 + 跳过**，由 `EnsureReadableAll`（**单独一趟**、建场之前跑）先把它们打开。
                stat.skipNoRead++;
                Debug.LogWarning($"[VCol] {goName}：`{objFile}` 有顶点色，但网格不可读（`isReadable: 0`）"
                               + " ⇒ **这一份没贴上色**。先跑一趟 "
                               + "`-executeMethod MeshVertexColors.EnsureReadableAll`（建场之前）");
                return src;
            }
            var mesh = Build(src, item, out int miss, out int dup);
            if (miss > 0 || dup > 0)
                Debug.LogWarning($"[VCol] {goName}（{objFile}）：贴色时 {miss} 个顶点按位置找不到色"
                               + (dup > 0 ? $"，另有 {dup} 个位置有多个不同颜色（取了先出现的）" : "")
                               + " —— 判据 → `工具/gen_arena_vertexcolors.py`");
            stat.colored++;
            stat.missVert += miss;
            return mesh;
        }

        static Mesh Build(Mesh src, Item it, out int miss, out int dup)
        {
            var map = PosMapOf(it);
            dup = map == null ? 0 : map.Dup;
            var verts = src.vertices;
            var cols = new Color[verts.Length];
            miss = 0;
            for (int i = 0; i < verts.Length; i++)
            {
                Color c;
                if (map != null && map.TryGet(verts[i], out c)) { cols[i] = c; continue; }
                cols[i] = Color.white;      // 找不到就白（= 与「没顶点色」同效），并出声
                miss++;
            }
            var m = new Mesh { name = src.name + "_vcol", indexFormat = src.indexFormat };
            m.SetVertices(verts);
            var nrm = src.normals;
            if (nrm != null && nrm.Length == verts.Length) m.SetNormals(nrm);
            var tan = src.tangents;
            if (tan != null && tan.Length == verts.Length) m.SetTangents(tan);
            for (int ch = 0; ch < 8; ch++)
            {
                var list = new List<Vector4>();
                src.GetUVs(ch, list);
                if (list.Count == verts.Length) m.SetUVs(ch, list);
            }
            m.colors = cols;
            m.subMeshCount = src.subMeshCount;
            for (int i = 0; i < src.subMeshCount; i++)
                m.SetTriangles(src.GetTriangles(i), i, false);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>**单独一趟**（CLI）：把旁挂里出现过的那些 `.obj` 的 `ModelImporter.isReadable` 打开。
        /// 为什么必须单独一趟：建场期 `SaveAndReimport()` 会在**建场中途重载资产**
        /// （2026-09-30 实测把 `BuildAll` 打崩：NRE ⇒ 退出码 1）⇒ 只能**建场之前**做掉。
        /// 用法：`... -executeMethod MeshVertexColors.EnsureReadableAll`（幂等；跑完再跑 `ArenaBuilder.BuildAll`）</summary>
        public static void EnsureReadableAll()
        {
            string[] arenas =
            {
                "battlearena1", "battlearena2", "battlearena3", "battlearenaaeldari", "battlearenaastramilitarum",
                "battlearenablacklegion", "battlearenadarkangels", "battlearenaemperorschildren",
                "battlearenagenestealers", "battlearenaleviathan", "battlearenasororitas",
                "battlearenaspacewolves", "battlearenatauviorla",
            };
            int n = 0, miss = 0;
            foreach (var scene in arenas)
                foreach (var it in Load(scene).Values)
                {
                    string path = $"Assets/WarpforgeArena1/arenas/{scene}/Models/{it.obj}";
#if UNITY_EDITOR
                    var mi = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.ModelImporter;
                    if (mi == null) { miss++; continue; }
                    if (!mi.isReadable) { mi.isReadable = true; mi.SaveAndReimport(); n++; }
#endif
                }
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            Debug.Log($"[VCol] `isReadable` 预扫：打开 {n} 个 OBJ"
                    + (miss > 0 ? $"；**{miss} 个找不到 ModelImporter**（出声，别当没这回事）" : ""));
        }

        /// <summary>诊断（CLI）：把「贴色时找不到色的顶点」到底长什么样打出来 ——
        /// 判「是精度（量化边界）/ 尺度（导入器改了坐标）/ 还是数据本来就对不上」。
        /// 用法：`... -executeMethod MeshVertexColors.DumpMiss --` 后跟 `WF_VCOLDUMP=<场>:<obj>` 环境变量。</summary>
        public static void DumpMiss()
        {
            string spec = System.Environment.GetEnvironmentVariable("WF_VCOLDUMP");
            if (string.IsNullOrEmpty(spec) || !spec.Contains(":")) { Debug.LogWarning("[VColDump] 需要 WF_VCOLDUMP=<场>:<obj 文件>"); return; }
            var sp = spec.Split(':');
            string scene = sp[0], objFile = sp[1];
            var map = Load(scene);
            if (!map.TryGetValue(objFile, out var it)) { Debug.LogWarning($"[VColDump] 旁挂里没有 {objFile}"); return; }
            string path = $"Assets/WarpforgeArena1/arenas/{scene}/Models/{objFile}";
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var src = prefab != null ? prefab.GetComponentInChildren<MeshFilter>() : null;
            if (src == null || src.sharedMesh == null) { Debug.LogWarning($"[VColDump] 读不出网格 {path}"); return; }
            var mesh = src.sharedMesh;
            var verts = mesh.vertices;
            Debug.Log($"[VColDump] {objFile}：Unity 网格 {verts.Length} 顶点（可读={mesh.isReadable}）；"
                    + $"旁挂 {it.n} 顶点；bounds={mesh.bounds}");
            var posMap = PosMapOf(it);
            int dup = posMap == null ? 0 : posMap.Dup;
            int miss = 0, missFlip = 0;
            float maxAbs = 0;
            for (int i = 0; i < verts.Length; i++)
            {
                maxAbs = Mathf.Max(maxAbs, Mathf.Abs(verts[i].x), Mathf.Abs(verts[i].y), Mathf.Abs(verts[i].z));
                Color c;
                if (posMap == null || !posMap.TryGet(verts[i], out c))
                {
                    if (miss < 3)
                        Debug.Log($"[VColDump]   [原样] 找不到色：Unity=({verts[i].x:F6},{verts[i].y:F6},{verts[i].z:F6})");
                    miss++;
                }
                // 🔴 **两种手性都试**：我们的 OBJ 是「原版 X 取反」，而 **Unity 的 OBJ 导入器自己还会再取反一次**
                //   ⇒ Unity 网格的 X ≈ **原版**（= 旁挂里**未取反**的那个）。这一行就是判它到底该用哪种。
                var flipped = new Vector3(-verts[i].x, verts[i].y, verts[i].z);
                if (posMap == null || !posMap.TryGet(flipped, out c)) missFlip++;
            }
            Debug.Log($"[VColDump] **原样命中 {verts.Length - miss}/{verts.Length} · X 取反后命中 {verts.Length - missFlip}/{verts.Length}**"
                    + $"（谁高就用谁；坐标最大绝对值 {maxAbs:F3} · 旁挂重复桶 {dup}）");
            // 旁挂里那几个最近的坐标长什么样（判「是不是差一点点」）
            for (int k = 0; k < Mathf.Min(5, it.n); k++)
                Debug.Log($"[VColDump]   旁挂第 {k} 个：({it.pos[k * 3]:F6},{it.pos[k * 3 + 1]:F6},{it.pos[k * 3 + 2]:F6})"
                        + $" 色=({it.col[k * 4]:F3},{it.col[k * 4 + 1]:F3},{it.col[k * 4 + 2]:F3},{it.col[k * 4 + 3]:F3})");
            Debug.Log($"[VColDump] 结论依据：找不到色 {miss}/{verts.Length} · 坐标最大绝对值 {maxAbs:F3} · 旁挂重复位置色 {dup}");
        }

        public class Stat
        {
            public int visit, colored, noData, already, skipNoRead, missVert;
            public override string ToString()
                => $"看过 {visit} 个网格 · 贴了色 {colored} · 旁挂里没有 {noData} · 本来就有色 {already}"
                 + (skipNoRead > 0 ? $" · **读不了跳过 {skipNoRead}**" : "")
                 + (missVert > 0 ? $" · **共 {missVert} 个顶点按位置找不到色**" : "");
        }
    }
