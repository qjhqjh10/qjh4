// ArenaRuntimeLoader.cs — **运行时按场把战场实例化出来**（§27 架构 · 施工图 `资料/§27架构_施工图.md`）
//
// 原来：13 份 `Battle_<场>.unity`，每份把战场几何**烘进场景**（`ArenaBuilder.BuildContent`），
//   共享的构建代码一改就得重打 13 份。现在：**1 份 `Battle.unity` + 13 件 prefab**
//   （`Assets/Resources/ArenaPrefabs/<场>.prefab`，由 `ArenaBuilder.BuildArenaPrefabs` 建），
//   进局时按督军阵营取一件实例化到 `Arena3D` 下。
//
// 场名从哪来（**判据只留一处**：`ArenaByArmy.SceneFor`）：
//   · 玩家对局 = 督军阵营 → `ArenaByArmy.SceneFor(army)`；
//   · 🔴 **批处理 / 预览** = 环境变量 `WF_ARENA`（`ArenaBuilder.ArenaFromEnv` 的运行时等价物）；
//     自检与出图那条链就是靠它指定「这一轮看哪一场」。
//
// ⚠️ **一个场景里只允许有一件战场**：重复 `Load` 同一个 key 直接返回（不重建）；
//    换 key 时**先把旧的销毁**（`RenderSettings` 每条都是显式赋值 ⇒ 不会残留上一场的值）。
//    ⚠️ 批处理下没有帧循环 ⇒ 销毁用 `DestroyImmediate`（`CLAUDE.md` §三）。
using UnityEngine;

namespace CardPresentation
{
    public class ArenaRuntimeLoader : MonoBehaviour
    {
        /// <summary>当前那件战场实例（`Warpforge_&lt;场>`）。</summary>
        public GameObject Current { get; private set; }
        /// <summary>当前这场是哪一场（`battlearena3` 这种键）。</summary>
        public string CurrentKey { get; private set; }
        /// <summary>已经应用过几次（自检用：判「没有重复实例化」）。</summary>
        public int LoadCount { get; private set; }

        /// <summary>**上一次 `Load()` 的耗时（毫秒，小数）** —— 这就是 §27 那条
        /// 「运行时实例化的加载时间要实测」里的**加载时间**那半。
        /// 🔴 **`-1` = 本次 `Load()` 没建造任何东西**（`arenaKey` 空 / 同一场不重建 / prefab 取不到）
        /// —— 故意拿负数当哨兵、**不用 `0`**：`0` 会被读成「快到量不出」，而事实是「这条路上什么都没建」。
        /// 每次 `Load()` 入口先重置成 `-1`，只有真走了实例化那条路才被写上真值。
        /// ⚠️ 它说的是「上次调用」这件事，不是「现在有没有战场」⇒ `Unload()` **不**清它。
        /// ⚠️ **内存那半批处理量不了**（没有帧循环、Profiler 失真）⇒ 只能真 Play：
        /// 见 `资料/真Play待验清单.md` E10（本属性只负责「加载时间」这半边）。</summary>
        public double LastLoadMs { get; private set; } = -1.0;

        /// <summary>只为「同一场不重建 ⇒ 无耗时可报」那条日志**去重**：同一场名最多解释一次
        /// （`BattleDriver.Begin` 一局调一次，而自检里 `BattleScene.Run` 会 `Begin` 十几趟、场名都是同一个
        /// ⇒ 不去重会刷十几行同样的话，反倒淹了那条真数字）。⚠️ 只影响日志，不影响任何取值。</summary>
        private string _noRebuildNotedKey;

        /// <summary>13 个场名（**与 `ArenaBuilder` 同一份口径**：不在这里另抄一张表 —— 运行时读
        /// `ArenaByArmy.Rows`，它是运行时那张权威表）。</summary>
        public static bool IsKnownArena(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            foreach (var r in ArenaByArmy.Rows)
                if (r.Scene == key) return true;
            return false;
        }

        /// <summary>本局/本轮该用哪一场：优先 `WF_ARENA`（批处理/自检），否则查督军阵营。</summary>
        public static string ResolveArenaKey(string army)
        {
            var env = System.Environment.GetEnvironmentVariable("WF_ARENA");
            if (!string.IsNullOrEmpty(env))
            {
                if (!IsKnownArena(env))
                    Debug.LogWarning($"[Arena] `WF_ARENA=\"{env}\"` 不是已知场名 ⇒ 忽略它、按阵营查表");
                else return env;
            }
            return ArenaByArmy.SceneFor(army);
        }

        /// <summary>把某一场的 prefab 实例化到 `Arena3D` 下（已同一场 ⇒ 不重建）。</summary>
        public bool Load(string arenaKey, Camera cam = null)
        {
            // ⏱ 计时 —— §27 那条「运行时实例化的**加载时间**与内存要实测」里的**加载时间**这半边
            //   （内存那半边批处理量不了：没有帧循环、Profiler 失真 ⇒ 只能真 Play，见 `LastLoadMs` 的注释）。
            //   🔴 一律用 `Stopwatch.Elapsed.TotalMilliseconds`（小数）—— `ElapsedMilliseconds` 是**整数毫秒**，
            //      小量会被截成 `0`，那就成了「默默返回 0」（本工程不许静默失败）。
            //   🔴 量不到就**写 `-1` 并出声**，绝不留一个「看着像真值」的数（见 `LastLoadMs`）。
            LastLoadMs = -1.0;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (string.IsNullOrEmpty(arenaKey))
            {
                Debug.LogWarning("[Arena] `arenaKey` 是空的 ⇒ 不建战场（不许静默）");
                return false;
            }
            if (Current != null && CurrentKey == arenaKey)
            {
                // 同一场不重建 ⇒ 本次**真的没有耗时可报**（不是「快到量不出」）。这里得出声说明，
                // 否则真 Play 里第二局看不到那条 ⏱ 会以为计时没做上 —— 那正是静默失败的一种。
                // ⚠️ 同一场名只解释一次（见 `_noRebuildNotedKey`）：自检里这行会被走到十几趟。
                sw.Stop();
                if (_noRebuildNotedKey != arenaKey)
                {
                    _noRebuildNotedKey = arenaKey;
                    Debug.Log($"[Arena] ⏱ `{arenaKey}` 已经是当前场 ⇒ 本次不重建、**无耗时可报**（`LastLoadMs` 留 -1）"
                            + " · 之后同一场的重复调用不再逐次出声");
                }
                return true;                                                 // 同一场，不重建
            }
            Unload();
            double msUnload = sw.Elapsed.TotalMilliseconds;                  // ① 撤上一场（换场才走到）

            var prefab = Resources.Load<GameObject>("ArenaPrefabs/" + arenaKey);
            double msRead = sw.Elapsed.TotalMilliseconds;                    // ② 读 prefab
            if (prefab == null)
            {
                Debug.LogError($"[Arena] 🔴 `Resources/ArenaPrefabs/{arenaKey}.prefab` 取不到 ⇒ 这一局没有战场。"
                             + "先跑 `-executeMethod ArenaBuilder.BuildArenaPrefabs` 建这 13 件 prefab");
                return false;
            }
            Current = Instantiate(prefab, transform);
            Current.name = "Warpforge_" + arenaKey;
            CurrentKey = arenaKey;
            LoadCount++;
            // 🆕 A393：场景侧那 5 条 `AnimFXController`（不被任何 blendable 管）—— 原版它们
            //   序列化在场景里 ⇒ 从「战场出现」这一刻就该在。判据 → 资料/普查产出_1012/H2_场景侧AnimFX.md
            CardPresentation.ScenarioBlendableFactory.BuildSceneAnimFx(Current.transform, arenaKey);

            var data = Current.GetComponentInChildren<ArenaPrefabData>();
            double msInst = sw.Elapsed.TotalMilliseconds;                    // ③ 实例化（含取 `ArenaPrefabData`）
            if (data == null)
            {
                Debug.LogWarning($"[Arena] 🔴 `{arenaKey}` 的 prefab 上没有 `ArenaPrefabData` ⇒ "
                               + "**雾 / 环境光 / 天空盒 / 相机这些逐场值没应用**（画面会不对，但不会报错的那种）");
            }
            else if (data.ArenaKey != arenaKey)
            {
                Debug.LogWarning($"[Arena] `{arenaKey}` 的 prefab 里记的场名是 `{data.ArenaKey}` —— 两者不一致，按 prefab 里那份用");
            }
            CardPresentation.ArenaSceneState.Apply(data != null ? data.state : null, Current, cam);
            sw.Stop();
            // 四段是**相邻 checkpoint 的差** ⇒ 它们加起来正好等于总（读的人不用自己配平，也不会有余项说不清）。
            LastLoadMs = sw.Elapsed.TotalMilliseconds;
            double msApply = LastLoadMs - msInst;                            // ④ 应用（`ArenaSceneState.Apply`）
            Debug.Log($"[Arena] ⏱ `{arenaKey}` 载入 **{LastLoadMs:F2} ms** = 撤旧场 {msUnload:F2}"
                    + $" + 读 prefab {msRead - msUnload:F2} + 实例化 {msInst - msRead:F2} + 应用 {msApply:F2}"
                    + " · ⚠️ 内存那半边批处理量不了 ⇒ 要真 Play（`资料/真Play待验清单.md` E10）"
                    + (System.Diagnostics.Stopwatch.IsHighResolution ? ""
                       : " · ⚠️ 本机 `Stopwatch` 不是高精度计时器 ⇒ 这个数只有毫秒级精度，别比小数位"));
            return true;
        }

        /// <summary>撤掉当前战场（换场/收工）。</summary>
        public void Unload()
        {
            if (Current == null) { CurrentKey = null; return; }
            if (Application.isPlaying) Destroy(Current); else DestroyImmediate(Current);
            Current = null;
            CurrentKey = null;
        }
    }
}
