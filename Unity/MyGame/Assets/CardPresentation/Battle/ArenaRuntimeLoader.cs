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
        /// <summary>当前那件战场实例（`Warpforge_<场>`）。</summary>
        public GameObject Current { get; private set; }
        /// <summary>当前这场是哪一场（`battlearena3` 这种键）。</summary>
        public string CurrentKey { get; private set; }
        /// <summary>已经应用过几次（自检用：判「没有重复实例化」）。</summary>
        public int LoadCount { get; private set; }

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
            if (string.IsNullOrEmpty(arenaKey))
            {
                Debug.LogWarning("[Arena] `arenaKey` 是空的 ⇒ 不建战场（不许静默）");
                return false;
            }
            if (Current != null && CurrentKey == arenaKey) return true;      // 同一场，不重建
            Unload();

            var prefab = Resources.Load<GameObject>("ArenaPrefabs/" + arenaKey);
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

            var data = Current.GetComponentInChildren<ArenaPrefabData>();
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
