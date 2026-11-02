// AudioSourceMixerGroupFixer.cs — 原版 `AudioSourceMixerGroupFixer` 的等价物（`B120-W11` · 账 `Q9③`）
//
// ============================================================================================
//  判据（第一权威 = 反编译方法体 + 解包字段，逐条实读）
// ============================================================================================
//  ① **类定义**：`AudioSourceMixerGroupFixer : MonoBehaviour`（`d:/2/tools/il2cpp_out/dump.cs:17479-17497`）
//     两个序列化字段：`AudioSource audioSource (+0x20)` · `MixerType mixerType (+0x28)`。
//     pathID = **7753607692688777346**（`_tmp_view/apk135/monoscripts.json` 现读；
//     ⚠️ `globalgamemanagers/MonoScript/MonoScript_1359.json` 的 **1359 是文件序号、不是 pathID** —— 本仓已记的坑）。
//  ② **`Start()`**（`decomp_full/AudioSourceMixerGroupFixer__Start.c`）：
//       `if (AudioMixerAddresableReferences.Instance 有效) {`
//           `var g = AudioMixerAddresableReferences.Instance.GetMixerGroup(mixerType, false);`
//           `if (audioSource != null) audioSource.outputAudioMixerGroup = g;`
//       `}`
//     —— 也就是**唯一**的运行时动作：按 `mixerType` 取组、写进 `AudioSource`。
//     `GetMixerGroup`（`decomp_full/AudioMixerAddresableReferences__GetMixerGroup.c`）遍历
//     `AudioMixerReference<MixerType, AudioMixerGroup>[] groups`，取**第一个类型对得上的**；一个都不匹配 ⇒ **null（不抛）**。
//  ③ **`OnValidate()`**（`..._OnValidate.c`）：`audioSource` 为空 ⇒ `GetComponent<AudioSource>()` 补上。
//  ④ **实例数 = 80**（本代理现读，生成器 `Unity/工具/gen_audio_mixer_fixers.py`，数据 `Resources/audio_mixer_fixers.json`）：
//       13 个 `bundle_scenes_scenes_battlearena*` 各 **6** 个（3 聊天源 + 3 条 BGM 轨）= 78
//       ＋ `bundle_staticgeneralassets_assets_all` 的 `MusicManager` **2** 个（都 `mixerType = 1 = Music`）= **80**。
//     · 13 场的 6 条路径**逐场完全相同**（生成器有断言：`all 13 arenas have identical (path, mixerType) sets`）。
//     · 聊天三源 = `Player/Enemy/RadioChatAudioSource`，`mixerType = 2 (Voices)`；
//       BGM 三轨 = `Music Low/Medium/High Intensity`，`mixerType = 4/5/6`（`BattleGroupLow/Medium/High`）。
//     · **旁证（这条很关键）**：原版这 6 颗源的**序列化 `OutputAudioMixerGroup` 已经等于** fixer 的目标组
//       （`AudioSource_1596 → Voices` · `_1597 → BattleGroupLow` · `_1599 → BattleGroupHigh` ·
//        `_1601 → BattleGroupMedium`；MusicManager 两颗 → `General`）。
//       ⇒ fixer 的作用 = **运行时按 Addressables 载进来的那个组实例重新绑一次**（序列化引用来自另一个包，
//          装载后可能不是同一份实例），不是「首次赋值」。
//  ⑤ **`MixerType` 七个值**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/MixerType.cs` 逐字）：
//       `FX=0 · Music=1 · Voices=2 · Jingles=3 · BattleGroupLow=4 · BattleGroupMedium=5 · BattleGroupHigh=6`。
//
// ============================================================================================
//  我们这一侧怎么落（⛔ 不新造第二套 mixer 机制 —— 组映射一律走 `WarpforgeAudio`，铁律 6）
// ============================================================================================
//  · `GroupFor(MixerType)` 是**唯一**的 `MixerType → AudioMixerGroup` 映射口：
//      FX → `WFSoundPlayer.FxGroup`（FX 子 mixer 的 `General`）· Music → `WarpforgeAudio.MusicGroup`（Music 子 mixer `General`）
//      Voices → `WarpforgeAudio.VoicesGroup` · Jingles → `WarpforgeAudio.JinglesGroup`
//      BattleGroupLow/Medium/High → `WarpforgeAudio.BattleGroupGroup(0/1/2)`
//    路由口径的判据 = `资料/完全复刻/判据/B109_R11_音频mixer.md` §1.6（`WarpforgeAudio` 文件头同一份）。
//  · **装配（§27 架构下的等价物）**：原版这 80 个是**场景/prefab 上的序列化组件**；本工程
//    `Resources/ArenaPrefabs/*.prefab` 里 **一个 `AudioSource` 都没有**（`grep -c "^--- !u!82"` 现读 = 0）——
//    HUD 与 BGM 音源都是**运行期建的**（`UnitChatPanel.MakeBubble` / `BattleMusic.Initialize` /
//    `ShellRuntime.MakeMusicSource`）⇒ 补挂点在那三条创建链上，数据表与补挂逻辑在本文件，
//    创建链各一行 `AudioSourceMixerGroupFixerInstaller.Attach(src, MixerType.X)`（补丁见
//    `资料/完全复刻/报告/B120_W11_执行.md`）。
//    `ArenaRuntimeLoader.Load()` 里仍**原样调用一次** `AttachArena(...)` —— 若哪天这些对象被烘回 prefab，
//    它会自动按路径挂上（⛔ 不靠「遍历所有 AudioSource 猜组」）。
//  · **批处理补充（🔶 自己设计 —— 不是原版语义）**：原版靠 `Start()` 生效；批处理**没有帧循环**
//    ⇒ `AddComponent` 之后 `Start` 不会自己跑。所以装配口 `Attach()` 额外**立即 `Apply()` 一次**
//    （幂等：同一组写两遍无副作用）。`Start()` 仍照原版保留。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace CardPresentation
{
    /// <summary>原版 `MixerType`（`MixerType.cs` 逐字照抄；枚举值 = 序列化里的整数，⛔ 别改顺序）。</summary>
    public enum MixerType
    {
        FX = 0,
        Music = 1,
        Voices = 2,
        Jingles = 3,
        BattleGroupLow = 4,
        BattleGroupMedium = 5,
        BattleGroupHigh = 6,
    }

    /// <summary>原版 `AudioSourceMixerGroupFixer`（`dump.cs:17479-17497`）的等价物：
    /// `Start()` 时按 <see cref="mixerType"/> 把 `AudioSource.outputAudioMixerGroup` 重新绑到
    /// `AudioMixerAddresableReferences.GetMixerGroup` 的对应组上。</summary>
    public class AudioSourceMixerGroupFixer : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private MixerType mixerType;

        /// <summary>原版字段名（自检 / 装配读口用）。</summary>
        public AudioSource Source { get { return audioSource; } }
        /// <summary>原版字段名（自检 / 装配读口用）。</summary>
        public MixerType Type { get { return mixerType; } }

        /// <summary>原版 `Start()`：按 `mixerType` 取组并写进 `audioSource`。
        /// ⚠️ 取不到组时**照样写 null**（原版 `GetMixerGroup` 不匹配就返回 null）——
        /// `AudioSourceMixerGroupFixerInstaller.GroupFor` 会在取不到时**出声一次**，⛔ 不静默。</summary>
        private void Start() { Apply(); }

        /// <summary>原版 `OnValidate()`：`audioSource` 为空 ⇒ 同 GO 上找一颗补上。</summary>
        private void OnValidate()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }

        /// <summary>`Start()` 的那段方法体（装配口会在 `AddComponent` 之后显式调一次 —— 见文件头「批处理补充」）。</summary>
        public void Apply()
        {
            var group = AudioSourceMixerGroupFixerInstaller.GroupFor(mixerType);
            if (audioSource == null)
            {
                // 原版：`audioSource` 为空的实例**什么都不做**（`Start` 里那个 `op_Implicit` 判空）。
                // 但它是一颗挂在场景上的组件 ⇒ 「没绑到源」是装配错误，出声（⛔ 不静默）。
                Debug.LogWarning("[AudioFixer] 这一件 `AudioSourceMixerGroupFixer` 的 `audioSource` 是空的 ⇒ 没有可绑定的源"
                               + "（原版此时整段跳过；判据 `AudioSourceMixerGroupFixer__Start.c`）");
                return;
            }
            audioSource.outputAudioMixerGroup = group;
        }

        /// <summary>装配口：设两个字段（然后由 <see cref="AudioSourceMixerGroupFixerInstaller.Attach"/> 立即 Apply 一次）。</summary>
        public void Setup(AudioSource src, MixerType type) { audioSource = src; mixerType = type; }
    }

    /// <summary>80 个实例的数据表 + 补挂逻辑（原版 `audioSource`/`mixerType` 那两格的数据侧等价物）。
    /// <para>数据：`Resources/audio_mixer_fixers.json`（生成器 `Unity/工具/gen_audio_mixer_fixers.py`，⛔ 别手改）。</para></summary>
    public static class AudioSourceMixerGroupFixerInstaller
    {
        public const string TableResource = "audio_mixer_fixers";

        [Serializable] public sealed class Row { public string path = ""; public int mixerType; }
        [Serializable] public sealed class ArenaTable { public string arena = ""; public Row[] rows = new Row[0]; }
        [Serializable] public sealed class MusicRow { public string path = ""; public int mixerType; }
        [Serializable] public sealed class Table
        {
            public ArenaTable[] arenas = new ArenaTable[0];
            public MusicRow[] musicManager = new MusicRow[0];
        }

        static Table _table;
        static bool _tried;
        static readonly HashSet<string> _warned = new HashSet<string>();
        static readonly HashSet<int> _warnedGroup = new HashSet<int>();

        /// <summary>最近一次 <see cref="AttachArena"/> 真的挂上了几件。</summary>
        public static int LastArenaAttached { get; private set; }
        /// <summary>最近一次 <see cref="AttachArena"/> 里**清单有、对象没找到**的条数。</summary>
        public static int LastArenaMissing { get; private set; }
        /// <summary>本进程累计挂上几件（自检用）。</summary>
        public static int AttachedTotal { get; private set; }

        /// <summary>读表（一次）。取不到 ⇒ null + 出声一次（⛔ 不静默 —— 静默的后果是「80 个位置一个都没挂」）。</summary>
        public static Table LoadTable()
        {
            if (_tried) return _table;
            _tried = true;
            var ta = Resources.Load<TextAsset>(TableResource);
            if (ta == null)
            {
                Debug.LogWarning($"[AudioFixer] `Resources/{TableResource}.json` 取不到 ⇒ 那 80 个位置的 `mixerType` 无从查起"
                               + "（生成器：`Unity/工具/gen_audio_mixer_fixers.py --write`）");
                return null;
            }
            try { _table = JsonUtility.FromJson<Table>(ta.text); }
            catch (Exception e) { Debug.LogWarning($"[AudioFixer] `{TableResource}.json` 解析不了：{e.Message}"); }
            if (_table == null)
                Debug.LogWarning($"[AudioFixer] `{TableResource}.json` 解析成 null");
            return _table;
        }

        /// <summary>清单总条数（13 场 × 6 + MusicManager 2 = **80**）。取不到表 ⇒ 0 + 出声（`LoadTable` 已喊）。</summary>
        public static int TableTotal
        {
            get
            {
                var t = LoadTable();
                if (t == null) return 0;
                int n = t.musicManager != null ? t.musicManager.Length : 0;
                if (t.arenas != null)
                    foreach (var a in t.arenas) if (a != null && a.rows != null) n += a.rows.Length;
                return n;
            }
        }

        /// <summary>某一场的清单行（取不到 ⇒ null）。</summary>
        public static Row[] ArenaRows(string arenaKey)
        {
            var t = LoadTable();
            if (t == null || t.arenas == null || string.IsNullOrEmpty(arenaKey)) return null;
            foreach (var a in t.arenas)
                if (a != null && a.arena == arenaKey) return a.rows;
            return null;
        }

        /// <summary>`MixerType → AudioMixerGroup` 的**唯一**映射口（原版 `AudioMixerAddresableReferences.GetMixerGroup` 的等价物）。
        /// 取不到 ⇒ null + 按类型出声一次（⛔ 不静默；原版这一支也是「返回 null、不抛」）。</summary>
        public static AudioMixerGroup GroupFor(MixerType type)
        {
            AudioMixerGroup g;
            switch (type)
            {
                case MixerType.FX:           g = WarpforgeVFX.WFSoundPlayer.FxGroup; break;
                case MixerType.Music:        g = WarpforgeAudio.MusicGroup; break;
                case MixerType.Voices:       g = WarpforgeAudio.VoicesGroup; break;
                case MixerType.Jingles:      g = WarpforgeAudio.JinglesGroup; break;
                case MixerType.BattleGroupLow:    g = WarpforgeAudio.BattleGroupGroup(0); break;
                case MixerType.BattleGroupMedium: g = WarpforgeAudio.BattleGroupGroup(1); break;
                case MixerType.BattleGroupHigh:   g = WarpforgeAudio.BattleGroupGroup(2); break;
                default:                     g = null; break;
            }
            if (g == null && _warnedGroup.Add((int)type))
                Debug.LogWarning($"[AudioFixer] `MixerType.{type}` 取不到混音组 ⇒ 这一档的音源会走 Master"
                               + "（判据 → `资料/完全复刻/判据/B109_R11_音频mixer.md` §1.6）");
            return g;
        }

        /// <summary>把 <paramref name="src"/> 所在的 GameObject 挂上一件 fixer（已在 ⇒ 复用，⛔ 不叠第二件）。
        /// <para>⚠️ **本口会立即 `Apply()` 一次**（批处理没有帧循环 ⇒ `Start` 不会自己跑；见文件头「批处理补充」）。</para>
        /// <returns>挂上的那一件；`src` 为空 ⇒ null + 出声。</returns></summary>
        public static AudioSourceMixerGroupFixer Attach(AudioSource src, MixerType type)
        {
            if (src == null)
            {
                Debug.LogWarning($"[AudioFixer] `Attach(..., {type})` 的 `AudioSource` 是空的 ⇒ 这一处没挂上");
                return null;
            }
            var go = src.gameObject;
            var f = go.GetComponent<AudioSourceMixerGroupFixer>();
            if (f == null) f = go.AddComponent<AudioSourceMixerGroupFixer>();
            f.Setup(src, type);
            f.Apply();
            AttachedTotal++;
            return f;
        }

        /// <summary>按数据表把 <paramref name="root"/> 下**同名路径**的对象挂上 fixer
        /// （原版是序列化组件 ⇒ 这里是「对象已经在、脚本被剥掉」时的补挂号）。返回真的挂上了几件。</summary>
        public static int AttachArena(GameObject root, string arenaKey)
        {
            LastArenaAttached = 0;
            LastArenaMissing = 0;
            if (root == null) return 0;
            var rows = ArenaRows(arenaKey);
            if (rows == null)
            {
                if (_warned.Add("noarena:" + arenaKey))
                    Debug.LogWarning($"[AudioFixer] 数据表里没有 `{arenaKey}` 这一场 ⇒ 本场**一件都没挂**"
                                   + "（⛔ 这不是「原版没有」—— 13 场各 6 件，见 `Resources/audio_mixer_fixers.json`）");
                return 0;
            }

            int n = 0, missing = 0;
            foreach (var r in rows)
            {
                if (r == null || string.IsNullOrEmpty(r.path)) { missing++; continue; }
                var tr = Resolve(root.transform, r.path);
                if (tr == null) { missing++; continue; }
                var src = tr.GetComponent<AudioSource>();
                if (src == null) src = tr.GetComponentInChildren<AudioSource>(true);
                if (src == null) { missing++; continue; }
                if (Attach(src, (MixerType)r.mixerType) != null) n++;
            }
            LastArenaAttached = n;
            LastArenaMissing = missing;
            if (missing > 0 && _warned.Add("miss:" + arenaKey))
            {
                // 一条汇总（⛔ 不逐条刷屏），并且把**为什么**说清 —— 这条在 2026-11-02 是**预期**状态：
                // 本工程的 HUD / BGM 音源都是运行期建的，arena prefab 里一个 AudioSource 都没有。
                Debug.LogWarning($"[AudioFixer] `{arenaKey}`：清单 {rows.Length} 条、按路径只找到 {n} 件（缺 {missing}）"
                               + " —— 本工程 HUD/BGM 音源是**运行期建**的（`UnitChatPanel.MakeBubble` / `BattleMusic.Initialize`），"
                               + "补挂点在那三条创建链上（判据 → `资料/完全复刻/报告/B120_W11_执行.md`）");
            }
            return n;
        }

        /// <summary>按 `a/b/c` 逐段 `Find` 解析。根节点名字**恰好等于**第一段时跳过第一段
        /// （原版路径以 `BattlePrefab` 起头，而我们的根就是那件 prefab 的实例）。</summary>
        static Transform Resolve(Transform root, string path)
        {
            if (root == null) return null;
            var segs = path.Split('/');
            int i = (segs.Length > 0 && root.name == segs[0]) ? 1 : 0;
            var cur = root;
            for (; i < segs.Length; i++)
            {
                if (string.IsNullOrEmpty(segs[i])) return null;
                var next = cur.Find(segs[i]);
                if (next == null) return null;
                cur = next;
            }
            return cur;
        }

        /// <summary>自检用：把累计计数清零（一件自检跑完别把下一条的读数污染）。</summary>
        public static void ResetCounters() { AttachedTotal = 0; LastArenaAttached = 0; LastArenaMissing = 0; }
    }
}
