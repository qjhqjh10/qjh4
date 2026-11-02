// ArenaTimelinePlayer.cs — 把原版战场里的 **Timeline（PlayableDirector + TimelineAsset）** 在运行时重建出来
//
// 【为什么要这一层】（战场线 `T09 · Timeline 地堡门`；判据 → 判据/R2_R3_现状与判据.md §4 ·
//   报告/B105_R14.md §四·1）
//   原版 `battlesharedresources` 的 `Bunker Door Parent` 上挂着
//   `PlayableDirector 7845524703085758649`（`m_InitialState 1`=Playing · `m_WrapMode 1`=Loop ·
//   `m_DirectorUpdateMode 1`=GameTime · `m_InitialTime 15.0`），
//   `PlayableAsset` = `Leviathan Bunker Door Animation`（5 条 track：1 条 Animation +
//   4 条 Control，共 7 个 Control clip）⇒ **原版进这张图，地堡门自动循环播**
//   （`Recorded` 那条 muscle clip 就在 Animation Track 的 `m_InfiniteClip` 上；
//    旧口径「原版 0 消费者」只查了 `Animation.m_Animation`、漏了 Timeline，`B105_R14` 已推翻）。
//
//   我们的 arena prefab 是**从清单重建**的 ⇒ 树里没有 PlayableDirector、
//   `Bunker_door` 只是一块静态网格。`Recorded` 早已重打进 `wf_arena_anims_shared.bundle`
//   （`工具/extract_missing_shaders.py` 的 `ARENA_ANIM_GROUPS`），**但一直没人绑定**。
//
// 【为什么 TimelineAsset 在运行时建、不做成工程资产】
//   Timeline 上那条 Animation Track 是**无限 clip 模式**，指着的 `Recorded` 是 **muscle 格式**
//   （`m_MuscleClipSize 4600`、`m_Legacy false`、`m_FloatCurves/m_PositionCurves/...` 全空）。
//   muscle clip 落不成工程 `.anim`（`WarpforgeAnimatorBridge` 头注有完整判据：`CreateAsset`
//   出来是空壳 `length=1`）⇒ **只能用运行时从 bundle 取到的那份原件**。
//   ⇒ 本层按 `Resources/arena_timelines.json`（由 `工具/gen_arena_timeline.py` **直读原版包**生成）
//   在 `Start()` 里把 `TimelineAsset` / 5 条 track / 每个 clip 的 start·duration·
//   `ControlPlayableAsset` 参数 **逐字段照原版值重建**，再把 director 的
//   `m_SceneBindings`（track→GameObject）与 `m_ExposedReferences`（exposedName→GameObject）
//   按原版那两条表重新绑上去。
//
// 【与规范的偏差（有意的）】
//   ① 原版 Animation Track 是 **infinite clip**（`m_Clips` 为空、`m_InfiniteClip` 指 `Recorded`）；
//      运行时 `AnimationTrack.infiniteClip` 的 setter 是 `internal`（Timeline 1.8.13 实读）
//      ⇒ 改用 `AnimationTrack.CreateClip(clip)` 生成一条 **同样时长（17.133 s）的 clip**。
//      播放结果等价（一段从 0 起、整段播放的动画），并且整条 Timeline 的时长仍是 17.133 s
//      （= 原版 `m_InfiniteClip` 的 `m_StopTime`）。
//   ② 原版 director 的 `m_PlayableAsset` 是**工程资产**；这里是运行时内存对象（见上）。
//   其余（track 数 / 顺序 / 名字、每个 clip 的 start / duration / displayName / exposedName /
//   particleRandomSeed / active / postPlayback / updateParticle / updateDirector /
//   updateITimeControl / searchHierarchy、director 的四个状态字段）**逐字段照原版**。
//
// 【失败要出声】找不到节点 / 取不到 clip / 条目缺失一律 `LogWarning` 并计数（`LastMissed*`）——
//   本工程不许静默失败（CLAUDE.md §三）。
//
// 【谁把它挂上去】`WarpforgeArena1/Editor/ArenaBuilder.cs` 的 `ApplyArenaTimelines`
//   （建 prefab / 建场景时都会走）⇒ 产物 `Resources/ArenaPrefabs/<场>.prefab` 里带这一层。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace WarpforgeVFX
{
    [Serializable] public class ArenaTimelineControlClip
    {
        public string display;
        public float start;
        public float duration;
        public float clipIn;
        public float timeScale;
        public string exposedName;
        public string target;
        public int active;
        public int postPlayback;
        public int updateParticle;
        public int updateDirector;
        public int updateITimeControl;
        public int searchHierarchy;
        public int particleRandomSeed;
        public int directorOnClipEnd;
    }

    [Serializable] public class ArenaTimelineTrackDesc
    {
        public string name;
        public long trackPid;
        public string kind;              // "animation" | "control"
        public string animatorPath;      // animation
        public string clip;              // animation：clip 名（Recorded）
        public float clipDuration;       // animation：原版 `m_StopTime`
        public int applyOffsets;         // animation（原版 `m_ApplyOffsets`）
        public int trackOffset;          // animation（原版 `m_TrackOffset`；`applyOffsets` 已废弃，用这个）
        public int matchTargetFields;    // animation
        public ArenaTimelineControlClip[] clips;   // control
    }

    [Serializable] public class ArenaTimelineDirectorDesc
    {
        public string path;
        public string playableAsset;
        public int initialState;    // 1 = Playing（原版 `m_InitialState`）
        public int wrapMode;        // 1 = Loop（原版 `m_WrapMode`）
        public int updateMode;      // 1 = GameTime（原版 `m_DirectorUpdateMode`）
        public float initialTime;   // 原版 `m_InitialTime`（实测 15.0）
        public int durationMode;
        public ArenaTimelineTrackDesc[] tracks;
    }

    [Serializable] public class ArenaTimelineArena
    {
        public string key;
        public ArenaTimelineDirectorDesc[] directors;
    }

    [Serializable] public class ArenaTimelineDoc
    {
        public int schema;
        public string note;
        public ArenaTimelineArena[] arenas;
    }

    /// <summary>把 `<see cref="ArenaTimelineDoc"/>` 里的原版 Timeline 在运行时重建到实例化出来的战场树上。</summary>
    [DisallowMultipleComponent]
    public sealed class ArenaTimelinePlayer : MonoBehaviour
    {
        public const string ResourcePath = "arena_timelines";

        /// <summary>由 `ArenaBuilder` 写入（建 prefab 时就知道是哪一场）。空 = 运行时按根节点名推。</summary>
        public string arenaKey;

        /// <summary>上一次 `Bind()` 的读数（自检 / 真机日志用）。</summary>
        public static int LastDirectors, LastAssets, LastTracks, LastClips, LastMissed;
        public static readonly List<string> LastMissedWhat = new List<string>();

        static ArenaTimelineDoc _doc;
        static bool _tried;

        bool _bound;

        public static ArenaTimelineDoc Doc()
        {
            if (_tried) return _doc;
            _tried = true;
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                Debug.LogError("[ArenaTimeline] 🔴 `Resources/" + ResourcePath + ".json` 取不到 ⇒ 原版 Timeline 一条都播不了。"
                             + "先跑 `python -I -X utf8 工具/gen_arena_timeline.py --write`");
                return null;
            }
            try { _doc = JsonUtility.FromJson<ArenaTimelineDoc>(ta.text); }
            catch (Exception e)
            {
                Debug.LogError("[ArenaTimeline] 🔴 `" + ResourcePath + ".json` 解析失败（"
                             + e.GetType().Name + ": " + e.Message + "）");
                _doc = null;
            }
            return _doc;
        }

        /// <summary>自检用：清缓存。</summary>
        public static void Reset() { _doc = null; _tried = false; }

        void Start() { Bind(); }

        public void Bind()
        {
            if (_bound) return;
            _bound = true;
            LastDirectors = LastAssets = LastTracks = LastClips = LastMissed = 0;
            LastMissedWhat.Clear();

            var doc = Doc();
            if (doc == null || doc.arenas == null) return;

            string key = !string.IsNullOrEmpty(arenaKey) ? arenaKey : GuessArenaKey();
            var arena = FindArena(doc, key);
            if (arena == null)
            {
                LastMissed++;
                LastMissedWhat.Add("`" + ResourcePath + ".json` 里没有场 `" + key + "`");
                return;
            }

            var root = FindArenaRoot();
            if (root == null)
            {
                LastMissed++;
                LastMissedWhat.Add("找不到战场根节点（`" + key + "`）");
                return;
            }

            int bound = 0;
            if (arena.directors != null)
            {
                // 哪个 director 归我：按「本节点相对战场根的路径」比对；表里只有一条时直接认它。
                var rel = RelativePath(root, transform);
                ArenaTimelineDirectorDesc pick = null;
                foreach (var d in arena.directors)
                    if (d != null && !string.IsNullOrEmpty(d.path) && string.Equals(rel, d.path, StringComparison.Ordinal))
                    { pick = d; break; }
                if (pick == null && arena.directors.Length == 1) pick = arena.directors[0];
                if (pick != null && BindOne(root, pick)) bound++;
            }
            LastDirectors = bound;
            if (bound == 0)
            {
                LastMissed++;
                LastMissedWhat.Add("`" + key + "`：本节点的路径与表里的 director 对不上（"
                                 + RelativePath(root, transform) + "）");
            }

            if (LastMissed > 0)
                Debug.LogWarning("[ArenaTimeline] `" + key + "`：director " + LastDirectors
                               + " · track " + LastTracks + " · clip " + LastClips
                               + " · **没接上 " + LastMissed + "**：" + string.Join(" / ", LastMissedWhat.ToArray()));
            else
                Debug.Log("[ArenaTimeline] `" + key + "`：director " + LastDirectors
                        + " · track " + LastTracks + " · clip " + LastClips + " —— 全部接上");
        }

        private bool BindOne(Transform root, ArenaTimelineDirectorDesc d)
        {
            var director = GetComponent<PlayableDirector>();
            if (director == null) director = gameObject.AddComponent<PlayableDirector>();

            var asset = ScriptableObject.CreateInstance<TimelineAsset>();
            asset.name = string.IsNullOrEmpty(d.playableAsset) ? "Arena Timeline" : d.playableAsset;

            var clipCache = new Dictionary<string, AnimationClip>();
            var trackList = new List<TrackAsset>();
            int clips = 0;

            if (d.tracks != null)
            {
                foreach (var t in d.tracks)
                {
                    if (t == null || string.IsNullOrEmpty(t.kind)) continue;
                    if (t.kind == "animation")
                    {
                        var tr = root.Find(t.animatorPath);
                        if (tr == null)
                        {
                            LastMissed++;
                            LastMissedWhat.Add("animation track 的绑定节点 `" + t.animatorPath + "` 找不到");
                            continue;
                        }
                        var animator = tr.GetComponent<Animator>();
                        if (animator == null) animator = tr.gameObject.AddComponent<Animator>();
                        animator.applyRootMotion = false;
                        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;   // 原版 m_CullingMode = 0
                        animator.enabled = true;

                        AnimationClip clip = null;
                        if (!string.IsNullOrEmpty(t.clip))
                        {
                            if (!clipCache.TryGetValue(t.clip, out clip))
                            {
                                clip = WarpforgeAnimatorBridge.TryGetClip(t.clip);
                                clipCache[t.clip] = clip;
                            }
                        }
                        if (clip == null)
                        {
                            LastMissed++;
                            LastMissedWhat.Add("animation track 的 clip `" + t.clip + "` 取不到"
                                             + "（`wf_arena_anims_shared.bundle`）");
                            continue;
                        }

                        var track = asset.CreateTrack<AnimationTrack>(null, t.name);
                        // 原版 `m_TrackOffset = 0`（ApplyTransformOffsets）；`m_ApplyOffsets = 0` ⇒ 不施加偏移。
                        // ⚠️ `AnimationTrack.applyOffsets` 在 Timeline 1.8.13 已废弃（CS0619 编译错）⇒ 用 `trackOffset`。
                        track.trackOffset = (TrackOffset)t.trackOffset;
                        var tc = track.CreateClip(clip);
                        if (tc != null)
                        {
                            tc.start = 0.0;
                            tc.duration = Math.Max(clip.length, 1.0 / 60.0);   // 原版 m_InfiniteClip 时长 = clip 时长
                            tc.displayName = clip.name;
                            clips++;
                        }
                        director.SetGenericBinding(track, tr.gameObject);
                        trackList.Add(track);
                    }
                    else if (t.kind == "control")
                    {
                        var track = asset.CreateTrack<ControlTrack>(null, t.name);
                        if (t.clips != null)
                        {
                            foreach (var c in t.clips)
                            {
                                if (c == null) continue;
                                var target = string.IsNullOrEmpty(c.target) ? null : root.Find(c.target);
                                if (target == null)
                                {
                                    LastMissed++;
                                    LastMissedWhat.Add("control clip `" + c.display + "` 的目标 `" + c.target + "` 找不到");
                                    continue;
                                }
                                var tc = track.CreateClip<ControlPlayableAsset>();
                                if (tc == null) continue;
                                tc.start = c.start;
                                tc.duration = Math.Max(c.duration, 1.0 / 60.0);
                                tc.clipIn = c.clipIn;
                                tc.timeScale = c.timeScale;
                                tc.displayName = c.display;

                                var cpa = tc.asset as ControlPlayableAsset;
                                if (cpa == null) continue;
                                cpa.active = c.active != 0;
                                cpa.postPlayback = (ActivationControlPlayable.PostPlaybackState)c.postPlayback;
                                cpa.updateParticle = c.updateParticle != 0;
                                cpa.updateDirector = c.updateDirector != 0;
                                cpa.updateITimeControl = c.updateITimeControl != 0;
                                cpa.searchHierarchy = c.searchHierarchy != 0;
                                cpa.particleRandomSeed = (uint)Math.Max(0, c.particleRandomSeed);
                                cpa.directorOnClipEnd = (DirectorControlPlayable.PauseAction)c.directorOnClipEnd;
                                if (!string.IsNullOrEmpty(c.exposedName))
                                {
                                    cpa.sourceGameObject = new ExposedReference<GameObject>
                                    {
                                        exposedName = new PropertyName(c.exposedName),
                                    };
                                    // 原版把「exposedName → 对象」存在 PlayableDirector 上（场景实例里逐条解析过）
                                    director.SetReferenceValue(new PropertyName(c.exposedName), target.gameObject);
                                }
                                clips++;
                            }
                        }
                        trackList.Add(track);
                    }
                    else
                    {
                        LastMissed++;
                        LastMissedWhat.Add("track `" + t.name + "` 的 kind `" + t.kind + "` 不认识");
                    }
                }
            }

            director.playableAsset = asset;
            director.playOnAwake = false;                    // 由本函数显式 Play（预制体里没有可序列化的 asset）
            director.extrapolationMode = (DirectorWrapMode)d.wrapMode;          // 1 = Loop
            director.timeUpdateMode = (DirectorUpdateMode)d.updateMode;         // 1 = GameTime
            director.initialTime = d.initialTime;            // 15.0
            director.time = d.initialTime;
            if (d.initialState == 1)
            {
                director.Play();
                // `Play()` 之后**再钉一次时间**：原版 `m_InitialTime` 是「开始播时的时刻」，
                // 而 `Play()` 在无参调用下不保证从 `time` 起（实测两条路径不一）⇒ 显式钉住。
                director.time = d.initialTime;
            }
            else director.Pause();

            LastTracks += trackList.Count;
            LastClips += clips;
            LastAssets++;
            return true;
        }

        static ArenaTimelineArena FindArena(ArenaTimelineDoc doc, string key)
        {
            if (doc.arenas == null) return null;
            foreach (var a in doc.arenas)
                if (a != null && string.Equals(a.key, key, StringComparison.Ordinal)) return a;
            return null;
        }

        /// <summary>战场根节点：往上找名字 == 场 key 的那一层（建 prefab 时它就是 prefab 根；
        /// 建场景时它挂在别的东西下面，`transform.root` 会指错 ⇒ ⛔ 不能直接用 `transform.root`）。</summary>
        Transform FindArenaRoot()
        {
            string key = !string.IsNullOrEmpty(arenaKey) ? arenaKey : GuessArenaKey();
            var cur = transform;
            while (cur != null)
            {
                if (!string.IsNullOrEmpty(key) && string.Equals(cur.name, key, StringComparison.Ordinal)) return cur;
                cur = cur.parent;
            }
            return transform.root;
        }

        string GuessArenaKey()
        {
            // 组件就挂在战场树里 ⇒ 往上找 `battlearena*` 那个节点
            var cur = transform;
            while (cur != null)
            {
                if (cur.name != null && cur.name.StartsWith("battlearena", StringComparison.Ordinal)) return cur.name;
                cur = cur.parent;
            }
            return transform.root != null ? transform.root.name : null;
        }

        static string RelativePath(Transform root, Transform leaf)
        {
            if (leaf == null) return "";
            if (root == null) return leaf.name;
            var parts = new List<string>();
            var cur = leaf;
            while (cur != null && cur != root)
            {
                parts.Add(cur.name);
                cur = cur.parent;
            }
            if (cur != root) return "";                     // 不在同一棵树下
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }
    }
}
