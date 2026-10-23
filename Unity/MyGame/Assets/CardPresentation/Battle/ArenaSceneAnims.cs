// ArenaSceneAnims.cs — 把**原版战场里的布景 / 机械 / 场景动画**在运行时接回我们实例化出来的那棵树
//
// 【为什么需要这一层】（2026-10-22 用户点名「用素材重新布置」；判据 → `资料/普查产出_1022/J-A_战场素材与材质诊断.md` §1·4）
//   我们生成的 13 件 arena prefab 里，原版那些会动的对象（炮台 / 泵 / 骷髅 / 孢子 / 雷尔贡炮）
//   只有**空的 `Animation` 组件**、**一个 `Animator` 都不建** ⇒ 原版满场在动的东西我们全是定住的。
//   这一层按原版序列化数据把「哪个节点播哪条 clip / 挂哪个 controller」接回去。
//
// 【数据从哪来】`Resources/arena_scene_anims.json`（323 行 / 小文件）
//   由 `工具/gen_arena_scene_anims.py` 从 `d:/2/新解包资源/assets_full/` 生成（本包优先、再查
//   `battlesharedresources`；**⛔ 不许按全库 pid 查**——不同场的 pid 17/18/19/20 是不同对象）。
//   全量件另存 `Unity/数据/游戏数据/arena_scene_anims.json`。
//
// 【clip / controller 从哪取】`WarpforgeAnimatorBridge`（`StreamingAssets/WarpforgeVFX/wf_arena_anims_*.bundle`）
//   · 9 条 legacy 场景 clip（6 场的 `* Scene Animation(s)` + 3 条 `Railgun*` + `Battle Arena 3 Animations`）
//     里含 `GameObject.m_IsActive` / `MonoBehaviour.m_Enabled` / `SkinnedMeshRenderer.blendShape.*`
//     三种非 Transform 绑定 ⇒ **取原件**比重建曲线忠实（判据见 `extract_missing_shaders.py` 的 `ARENA_ANIM_GROUPS`）。
//   · 6 个 `AnimatorController`（炮台 ×2 / 泵 / 动态骷髅 / 竞技场 2 / 雷尔贡）。
//
// 【⛔ 别接第二份】`BattlePrefab/BattleHud`（`BattleHudAnim`）· `Cinemachine Vcam`（`CameraIntro`）
//   已经各有系统在管；生成器里按路径跳过（`SKIP_PREFIXES`），本文件不再判断。
//
// 【失败要出声】找不到节点 / 取不到 clip / 取不到 controller 一律 `LogWarning` 并计数
//   （本工程不许静默失败；`LastMissed*` 就是给自检与真机日志看的）。
using System;
using System.Collections.Generic;
using UnityEngine;
using WarpforgeVFX;

namespace CardPresentation
{
    [Serializable] public class ArenaAnimClipRef
    {
        public string name;
        public bool legacy;
        public int muscle;
        public string source;          // "pkg" | "shared"
    }

    [Serializable] public class ArenaAnimEntry
    {
        public string path;
        public bool playAutomatically;
        public int wrapMode;           // UnityEngine.WrapMode 序列化值
        public ArenaAnimClipRef[] clips;
    }

    [Serializable] public class ArenaAnimatorEntry
    {
        public string path;
        public string controller;
        public int controllerPid;
        public string source;
    }

    [Serializable] public class ArenaAnimsArena
    {
        public string key;
        public ArenaAnimEntry[] animations;
        public ArenaAnimatorEntry[] animators;
    }

    [Serializable] public class ArenaAnimsDoc
    {
        public int schema;
        public string note;
        public ArenaAnimsArena[] arenas;
    }

    /// <summary>把 `arena_scene_anims.json` 接进实例化出来的战场树。**每场进局接一次**（幂等）。</summary>
    public static class ArenaSceneAnims
    {
        public const string ResourcePath = "arena_scene_anims";

        /// <summary>上一次 `Bind()` 的读数（自检/真机日志用）。</summary>
        public static int LastAnimated;        // 接上的 Animation 组件数
        public static int LastClips;           // 接上的 clip 数
        public static int LastAnimators;       // 接上 controller 的 Animator 数
        public static int LastMissed;          // 没找到的节点 + 取不到的资产数
        public static readonly List<string> LastMissedWhat = new List<string>();

        static ArenaAnimsDoc _doc;
        static bool _tried;

        static ArenaAnimsDoc Doc()
        {
            if (_tried) return _doc;
            _tried = true;
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                Debug.LogError($"[ArenaAnims] 🔴 `Resources/{ResourcePath}.json` 取不到 ⇒ **战场布景动画一条都不会播**。"
                             + "先跑 `python -I -X utf8 工具/gen_arena_scene_anims.py`");
                return null;
            }
            try { _doc = JsonUtility.FromJson<ArenaAnimsDoc>(ta.text); }
            catch (Exception e)
            {
                Debug.LogError($"[ArenaAnims] 🔴 `{ResourcePath}.json` 解析失败（{e.GetType().Name}: {e.Message}）");
                _doc = null;
            }
            return _doc;
        }

        /// <summary>自检用：清掉缓存（下一次 `Bind` 重新读 JSON）。</summary>
        public static void Reset() { _doc = null; _tried = false; }

        /// <summary>按场把原版的 `Animation`（布景/机械）与 `Animator`（炮台/泵/骷髅）接上去。</summary>
        public static void Bind(Transform root, string arenaKey)
        {
            LastAnimated = LastClips = LastAnimators = LastMissed = 0;
            LastMissedWhat.Clear();
            if (root == null || string.IsNullOrEmpty(arenaKey)) return;

            var doc = Doc();
            if (doc == null || doc.arenas == null) return;

            ArenaAnimsArena arena = null;
            foreach (var a in doc.arenas)
                if (a != null && a.key == arenaKey) { arena = a; break; }
            if (arena == null)
            {
                Debug.LogWarning($"[ArenaAnims] `{ResourcePath}.json` 里没有场 `{arenaKey}` ⇒ 本场不接布景动画");
                LastMissed++;
                LastMissedWhat.Add($"no arena entry `{arenaKey}`");
                return;
            }

            // ---- ① `Animation`：布景 / 机械 / 场景动画 ----
            if (arena.animations != null)
            {
                foreach (var e in arena.animations)
                {
                    if (e == null || string.IsNullOrEmpty(e.path)) continue;
                    var t = root.Find(e.path);
                    if (t == null)
                    {
                        LastMissed++;
                        LastMissedWhat.Add($"Animation `{e.path}` 在 prefab 树里找不到");
                        continue;
                    }
                    var anim = t.GetComponent<Animation>();
                    if (anim == null) anim = t.gameObject.AddComponent<Animation>();

                    int added = 0;
                    if (e.clips != null)
                    {
                        foreach (var c in e.clips)
                        {
                            if (c == null || string.IsNullOrEmpty(c.name)) continue;
                            if (anim.GetClip(c.name) != null) { added++; continue; }   // 幂等
                            var clip = WarpforgeAnimatorBridge.TryGetClip(c.name);
                            if (clip == null)
                            {
                                LastMissed++;
                                LastMissedWhat.Add($"clip `{c.name}` 取不到（{e.path}）");
                                continue;
                            }
                            anim.AddClip(clip, c.name);
                            added++;
                        }
                    }
                    if (added == 0) continue;

                    anim.playAutomatically = e.playAutomatically;
                    anim.wrapMode = (WrapMode)e.wrapMode;
                    anim.enabled = true;
                    // `playAutomatically=true` 的组件原版一出生就播；`Animation` 组件在运行时
                    // 需要显式 `Play()`（预制体里没有序列化播放状态）。
                    if (e.playAutomatically && e.clips != null && e.clips.Length > 0 && e.clips[0] != null)
                        anim.Play(e.clips[0].name);

                    LastAnimated++;
                    LastClips += added;
                }
            }

            // ---- ② `Animator`：炮台 / 泵 / 动态骷髅 / 竞技场 2 / 雷尔贡 ----
            if (arena.animators != null)
            {
                foreach (var e in arena.animators)
                {
                    if (e == null || string.IsNullOrEmpty(e.path) || string.IsNullOrEmpty(e.controller)) continue;
                    var t = root.Find(e.path);
                    if (t == null)
                    {
                        LastMissed++;
                        LastMissedWhat.Add($"Animator `{e.path}` 在 prefab 树里找不到");
                        continue;
                    }
                    var an = t.GetComponent<Animator>();
                    if (an == null) an = t.gameObject.AddComponent<Animator>();
                    if (an.runtimeAnimatorController != null && an.runtimeAnimatorController.name == e.controller)
                    { LastAnimators++; continue; }                              // 幂等

                    var rc = WarpforgeAnimatorBridge.TryGet(e.controller);
                    if (rc == null)
                    {
                        LastMissed++;
                        LastMissedWhat.Add($"controller `{e.controller}` 取不到（{e.path}）");
                        continue;
                    }
                    an.runtimeAnimatorController = rc;
                    an.applyRootMotion = false;
                    an.enabled = true;
                    LastAnimators++;
                }
            }

            if (LastMissed > 0)
                Debug.LogWarning($"[ArenaAnims] `{arenaKey}`：Animation {LastAnimated}（clip {LastClips}）· "
                               + $"Animator {LastAnimators} · **没接上 {LastMissed}**："
                               + string.Join(" / ", LastMissedWhat.ToArray()));
            else
                Debug.Log($"[ArenaAnims] `{arenaKey}`：Animation {LastAnimated}（clip {LastClips}）· "
                        + $"Animator {LastAnimators} —— 全部接上");
        }
    }
}
