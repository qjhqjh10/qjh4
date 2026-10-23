// WarpforgeAnimatorBridge.cs — 运行时把**原版那份** `AnimatorController` / `AnimationClip` 取回来挂上
//
// 🔴 **为什么要运行时取原件、而不是用工程里那份 `.controller`**（2026-10-01 晚实测定案）：
//
//   阵亡爆散体（`Card 3D Death Explosion` / `Necrons death explosion`）的
//   `Animator.m_Controller` 指向的是一份 **bundle 资产**。导出时它落不下盘 ⇒ 原来的产物里是
//   `guid: 00000000000000000000000000000000` 的伪引用（运行时 **null**、动画一帧不播）。
//
//   我们试着把它变成工程资产，**三条路全走不通**（`AnimClipProbe.Run` 逐条量过）：
//     ① `Object.Instantiate(clip)` + `AssetDatabase.CreateAsset`
//     ② `EditorUtility.CopySerialized(clip, new AnimationClip())` + `CreateAsset`
//     ③ 直接 `CreateAsset(bundle 对象)`（Unity 直接拒收）
//   ⇒ 落出来的 `Card Explosion.anim` **`m_FloatCurves` 与 `m_ClipBindingConstant` 都是空的**，
//     `clip.length` **1**（Unity 空 clip 的默认值），而原版是 **0.8167 s**。
//   **根因**：包里的 clip 是 **muscle / streamed 格式**（`m_MuscleClipSize: 2808`、
//     `m_StreamedClip.data` 是压缩过的曲线块）—— 那是**跑起来用的运行时格式**，
//     编辑器那份曲线数据**根本没打进包里**，所以没有「拷出来」这回事。
//     （控制器是同一个道理：包里是运行时格式，工程 `.controller` 要编辑器格式。）
//
//   ⇒ 与 **原版 shader** 完全同一条路子（`WarpforgeShaderLoader`：字节码落不了工程，就运行时从
//      `StreamingAssets` 的小包里取）。这边取的是 **控制器**，它天然带着自己的 clip。
//
// 📌 数据从哪来：`工具/extract_missing_shaders.py --prefabs` 把这两个 prefab + 依赖树重打成
//    `StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle`，并把**控制器自己也登记成容器项**
//    （`controller_names()`），`LoadAsset<RuntimeAnimatorController>(名字)` 才取得到。
//
// 📌 工程里那份 `.controller`（`Assets/WarpforgeVFX/Animators/`）**仍然保留**：它结构是对的
//    （1 层 / 1 状态 / 动作指向同名 clip），只是那个 `.anim` 的**曲线是空的** ——
//    真正播的动画由这里在 `Awake` 时换上。两边名字必须一致（导出器按名字写进 binder）。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WarpforgeVFX
{
    public static class WarpforgeAnimatorBridge
    {
        /// <summary>重打的小包（相对 `StreamingAssets`）。</summary>
        public const string BundleRelPath = "WarpforgeVFX/wf_prefabs_extra.bundle";

        /// <summary>🆕 2026-10-23：**战场布景动画的 9 个小包**（`工具/extract_missing_shaders.py --arena-anims`）。
        ///
        /// 为什么是列表：`repack_tree` 一个产物只能有一个 `src`，而这 15 件分属 9 个源包
        /// （判据 → `资料/普查产出_1022/J-A_战场素材与材质诊断.md` §2·2 + `ARENA_ANIM_GROUPS`）。
        /// ⚠️ 查找顺序 = **主包 → 本列表**；主包行为一个字没变（老调用点零影响）。
        /// ⚠️ 消费方 = `CardPresentation.ArenaSceneAnims.Bind()`（按 `arena_scene_anims.json` 接）。</summary>
        public static readonly string[] ExtraBundleRelPaths =
        {
            "WarpforgeVFX/wf_arena_anims_arena1.bundle",
            "WarpforgeVFX/wf_arena_anims_arena2.bundle",
            "WarpforgeVFX/wf_arena_anims_shared.bundle",
            "WarpforgeVFX/wf_arena_anims_astra.bundle",
            "WarpforgeVFX/wf_arena_anims_blacklegion.bundle",
            "WarpforgeVFX/wf_arena_anims_darkangels.bundle",
            "WarpforgeVFX/wf_arena_anims_emperorschildren.bundle",
            "WarpforgeVFX/wf_arena_anims_genestealers.bundle",
            "WarpforgeVFX/wf_arena_anims_tau.bundle",
        };

        /// <summary>🆕 2026-10-23（真机 C4）：**每只小包的「识别探针」** —— 与
        /// `ExtraBundleRelPaths` **逐项对应**（同一下标 = 同一只包），值是那只包里**必定有**的
        /// 一件资产名（取自 `工具/extract_missing_shaders.py` 的 `ARENA_ANIM_GROUPS`）。
        /// 只用来回答一句：「**这只包是不是已经在进程里了？**」
        ///
        /// 🔴 **为什么需要它**（真机 2026-10-23 14:33 · `_tmp_view/real/battle_arena2.log:338-347`）：
        ///   `WarpforgeShaderLoader.EnsureLoaded()` 用 **`wf_arena_*.bundle` 通配**扫目录
        ///   （`WarpforgeShaderLoader.cs:122`），本意只要 shader 包，但 `wf_arena_anims_*.bundle`
        ///   **也匹配这个通配** ⇒ 9 只动画包先被它整只加载走了（同一份日志 `:47-56` 逐只打出
        ///   「一个 Shader 都没有」）。而 **Unity 一个进程里同一份内容只能加载一次** ⇒ 这里再
        ///   `LoadFromFile` 一律返回 null，引擎逐条报
        ///   `can't be loaded because another AssetBundle with the same files is already loaded`
        ///   ⇒ 旧版把这 9 只全记成「没加载成」（**假失败**：它们其实都在内存里、资产也取得到，
        ///   靠 `TryGet` 末尾那条兜底捡回来才没出事）。
        /// ⇒ 现在**先拿探针去已加载的包里认领**，认到就复用那只包、不再尝试第二次加载。
        ///    （反方向也安全：若本桥先加载了，`ShaderLoader` 那边撞上会走它自己的
        ///    `HarvestFromLoadedBundles`，那条早就存在。）
        ///
        /// ⚠️ 两边的一致性由 `python -I -X utf8 工具/extract_missing_shaders.py --verify-arena-anims`
        ///   对账（改了 `ARENA_ANIM_GROUPS` 没改这张表、或反过来 ⇒ 那条会红）。
        /// ⚠️ 探针要挑**只在这一组里**的名字；同名跨包时认到谁无所谓（资产内容相同），
        ///   但别拿 shader 包/卡包也有的名字。</summary>
        public static readonly string[] ExtraProbeAssets =
        {
            "Cannon 1",                             // arena1（控制器）
            "Pump 1",                               // arena2（控制器）
            "Battle Arena Leviathan",               // shared（控制器）
            "Astra Militarum Scene Animations",     // astra（legacy clip）
            "Black Legion Scene Animation",         // blacklegion（legacy clip）
            "Dark Angels Scene animations",         // darkangels（legacy clip）
            "Emperor's Children Scene Animations",  // emperorschildren（legacy clip）
            "Genestealers Scene Animations",        // genestealers（legacy clip）
            "Railgun BIG",                          // tau（legacy clip）
        };

        /// <summary>最近一次 `EnsureLoaded()` 的读数（自检 / 真机日志断言用）。</summary>
        public static int LastExtrasSelfLoaded;   // 本桥自己 `LoadFromFile` 加载的
        public static int LastExtrasReused;       // 已被别的加载器加载、按探针认领复用的
        public static int LastExtrasMissing;      // 文件不在 / 加载不出来 / 认不到

        static AssetBundle _bundle;
        static readonly List<AssetBundle> _extras = new List<AssetBundle>();
        static bool _tried;
        static readonly Dictionary<string, RuntimeAnimatorController> _byName =
            new Dictionary<string, RuntimeAnimatorController>();
        /// <summary>🆕 2026-10-19：容器别名（`效果名|状态名`）→ 原版片段。</summary>
        static readonly Dictionary<string, AnimationClip> _clipByName =
            new Dictionary<string, AnimationClip>();

        /// <summary>按名字取一份原版控制器。取不到返回 null **并出声**（不许静默失败）。</summary>
        public static RuntimeAnimatorController TryGet(string controllerName)
        {
            if (string.IsNullOrEmpty(controllerName)) return null;
            if (_byName.TryGetValue(controllerName, out var cached)) return cached;

            var rc = FromAll(controllerName);
            if (rc == null)
            {
                EnsureLoaded();
                rc = FromAll(controllerName);
            }
            if (rc == null)
            {
                // 🔴 **同一份内容已在进程里加载过时，`LoadFromFile` 会返回 null**（Unity 限制：
                //    同一个文件不能加载两次）—— 自检/工具先把源 bundle 全量加载时必然撞上。
                //    这里按 `WarpforgeShaderLoader.HarvestFromLoadedBundles` 的老套路，
                //    去**已加载的包**里问一遍谁有这个资产（开发期很常见，运行期一般走不到）。
                foreach (var b in AssetBundle.GetAllLoadedAssetBundles())
                {
                    if (b == null || ReferenceEquals(b, _bundle)) continue;
                    rc = From(b, controllerName);
                    if (rc != null)
                    {
                        Debug.Log($"[WarpforgeAnimatorBridge] 自己的包没加载成，改从**已加载的**包 "
                                + $"`{b.name}` 里捡到控制器 `{controllerName}`");
                        break;
                    }
                }
            }
            if (rc == null)
            {
                Debug.LogError($"[WarpforgeAnimatorBridge] 取不到控制器 `{controllerName}`"
                             + $"（`{BundleRelPath}`）⇒ **这个效果的动画不会播**。"
                             + "排查：`工具/extract_missing_shaders.py --prefabs` 有没有重跑过、"
                             + "包在不在 `StreamingAssets/` 里、跑之前有没有别人先加载过同一个包");
                return null;
            }
            _byName[controllerName] = rc;
            return rc;
        }

        static RuntimeAnimatorController From(AssetBundle b, string name)
        {
            if (b == null) return null;
            try
            {
                var rc = b.LoadAsset<RuntimeAnimatorController>(name);
                if (rc != null) return rc;
                // 兜底：容器里没登记时按类型全捞（同名只有一个）
                foreach (var x in b.LoadAllAssets<RuntimeAnimatorController>())
                    if (x != null && x.name == name) return x;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WarpforgeAnimatorBridge] 在 `{b.name}` 里取 `{name}` 抛了："
                               + $"{e.GetType().Name}: {e.Message}");
            }
            return null;
        }

        /// <summary>主包 + 额外包（战场动画）里按名字找控制器。顺序固定 ⇒ **同名时主包优先**（老行为不变）。</summary>
        static RuntimeAnimatorController FromAll(string name)
        {
            var rc = From(_bundle, name);
            if (rc != null) return rc;
            for (int i = 0; i < _extras.Count; i++)
            {
                rc = From(_extras[i], name);
                if (rc != null) return rc;
            }
            return null;
        }

        /// <summary>同上，找 clip。</summary>
        static AnimationClip FromAllClip(string alias)
        {
            var c = FromClip(_bundle, alias);
            if (c != null) return c;
            for (int i = 0; i < _extras.Count; i++)
            {
                c = FromClip(_extras[i], alias);
                if (c != null) return c;
            }
            return null;
        }

        /// <summary>🆕 2026-10-19（T12 / `A1729`）：按**容器别名**取一份原版 `AnimationClip`。
        ///
        /// 谁在用：`WFModuleAnimation` —— 原版 `AnimFXModuleAnimation.Exit()` 调的是 AssetStore 插件
        ///   `SimpleAnimation.Play("Exit")`，那套组件把「状态名 → clip」存在 prefab 上（我们不导它）。
        ///   ⇒ 打包时把每条 clip 额外登记成 **`&lt;效果名&gt;|&lt;状态名&gt;`** 的容器别名
        ///   （`工具/gen_simple_animation_clips.py` → `extract_missing_shaders.py --prefabs`），
        ///   运行时按别名取 = 一条确定的路，不需要在 C# 里再抄一张映射。
        /// 取不到返回 null **并出声**（与 `TryGet` 同一规矩：不许静默失败）。</summary>
        public static AnimationClip TryGetClip(string alias)
        {
            if (string.IsNullOrEmpty(alias)) return null;
            AnimationClip cached;
            if (_clipByName.TryGetValue(alias, out cached)) return cached;

            var clip = FromAllClip(alias);
            if (clip == null)
            {
                EnsureLoaded();
                clip = FromAllClip(alias);
            }
            if (clip == null)
            {
                // 同 `TryGet`：同一份内容已在进程里加载过时 `LoadFromFile` 返回 null
                foreach (var b in AssetBundle.GetAllLoadedAssetBundles())
                {
                    if (b == null || ReferenceEquals(b, _bundle)) continue;
                    clip = FromClip(b, alias);
                    if (clip != null)
                    {
                        Debug.Log($"[WarpforgeAnimatorBridge] 自己的包没加载成，改从**已加载的**包 "
                                + $"`{b.name}` 里捡到片段 `{alias}`");
                        break;
                    }
                }
            }
            if (clip == null)
            {
                Debug.LogWarning($"[WarpforgeAnimatorBridge] 取不到动画片段 `{alias}`（`{BundleRelPath}`）" +
                                 "—— 该效果的 Exit 动画播不了。排查：`工具/gen_simple_animation_clips.py` → " +
                                 "`工具/extract_missing_shaders.py --prefabs` 有没有重跑过、包在不在 StreamingAssets 里");
                return null;
            }
            _clipByName[alias] = clip;
            return clip;
        }

        static AnimationClip FromClip(AssetBundle b, string alias)
        {
            if (b == null) return null;
            try
            {
                var c = b.LoadAsset<AnimationClip>(alias);
                if (c != null) return c;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WarpforgeAnimatorBridge] 在 `{b.name}` 里取片段 `{alias}` 抛了："
                               + $"{e.GetType().Name}: {e.Message}");
            }
            return null;
        }

        static void EnsureLoaded()
        {
            if (_tried) return;
            _tried = true;
            var path = Path.Combine(Application.streamingAssetsPath, BundleRelPath);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[WarpforgeAnimatorBridge] 找不到 `{path}` —— 先跑一次 "
                               + "`工具/extract_missing_shaders.py --prefabs`");
            }
            else
            {
                try { _bundle = AssetBundle.LoadFromFile(path); }
                catch (Exception e)
                {
                    Debug.LogWarning($"[WarpforgeAnimatorBridge] 加载 `{path}` 抛了：{e.GetType().Name}: {e.Message}");
                }
                if (_bundle == null)
                    Debug.LogWarning($"[WarpforgeAnimatorBridge] `{path}` 加载不出（同一份内容可能已在进程里加载过？）"
                                   + " —— 下面会去已加载的包里再找一遍");
            }

            // 🆕 2026-10-23：战场布景动画的 9 个小包。**主包缺了也照常加载它们**（各自独立）。
            // 🔴 顺序：**先认领、再加载** —— 见 `ExtraProbeAssets` 头注（`WarpforgeShaderLoader`
            //    的 `wf_arena_*` 通配会先把这 9 只扫走，那时 `LoadFromFile` 必然返回 null）。
            _extras.Clear();
            LastExtrasSelfLoaded = LastExtrasReused = LastExtrasMissing = 0;
            var unusable = new List<string>();
            for (int i = 0; i < ExtraBundleRelPaths.Length; i++)
            {
                var rel = ExtraBundleRelPaths[i];
                var ep = Path.Combine(Application.streamingAssetsPath, rel);
                var fileName = Path.GetFileName(rel);
                if (!File.Exists(ep)) { LastExtrasMissing++; unusable.Add(fileName + "（文件不在）"); continue; }

                // ① 已经在进程里了？⇒ 复用，**别再 `LoadFromFile`**（Unity：同一份内容只能加载一次）
                var probe = (i < ExtraProbeAssets.Length) ? ExtraProbeAssets[i] : null;
                var reuse = FindLoadedWith(probe);
                if (reuse != null) { _extras.Add(reuse); LastExtrasReused++; continue; }

                // ② 没人在场 ⇒ 自己加载
                AssetBundle eb = null;
                try { eb = AssetBundle.LoadFromFile(ep); }
                catch (Exception e)
                {
                    Debug.LogWarning($"[WarpforgeAnimatorBridge] 加载 `{ep}` 抛了：{e.GetType().Name}: {e.Message}");
                }
                if (eb != null) { _extras.Add(eb); LastExtrasSelfLoaded++; }
                else { LastExtrasMissing++; unusable.Add(fileName); }
            }
            if (LastExtrasMissing > 0)
                // 🔴 真缺陷（不是「别人先加载了」那种）⇒ **报错并点名**，⛔ 别让它混在噪音里
                Debug.LogError($"[WarpforgeAnimatorBridge] 战场动画小包 {LastExtrasMissing}/{ExtraBundleRelPaths.Length} 个**用不了**："
                             + string.Join(" / ", unusable.ToArray())
                             + " —— 先跑 `工具/extract_missing_shaders.py --arena-anims`；"
                             + "产物对不对用 `python -I -X utf8 工具/extract_missing_shaders.py --verify-arena-anims` 对账");
            else
                Debug.Log($"[WarpforgeAnimatorBridge] 战场动画小包 {ExtraBundleRelPaths.Length}/{ExtraBundleRelPaths.Length} 就绪"
                        + $"（自载 {LastExtrasSelfLoaded} · 复用已加载包 {LastExtrasReused}）");
        }

        /// <summary>在**已加载的**包里按探针找一只（跳过主包 `wf_prefabs_extra` 与已经收进 `_extras` 的）。
        /// 找不到返回 null。判据与原因 → `ExtraProbeAssets` 头注。</summary>
        static AssetBundle FindLoadedWith(string probe)
        {
            if (string.IsNullOrEmpty(probe)) return null;
            foreach (var b in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (b == null || ReferenceEquals(b, _bundle)) continue;
                bool already = false;
                for (int i = 0; i < _extras.Count; i++) if (ReferenceEquals(_extras[i], b)) { already = true; break; }
                if (already) continue;
                if (HasAsset(b, probe)) return b;
            }
            return null;
        }

        /// <summary>这只包里**有没有**这件资产 —— 只问容器/预加载表，**不实例化、不挑类型**。
        ///
        /// 两层判据（都由打包器登记过）：① `Contains(裸名)`（容器键；打包时同时登记了裸名与
        /// `assets/&lt;小写&gt;.&lt;ext&gt;` 两条别名）② `GetAllAssetNames()` 里按小写比一回
        /// （`Contains` 万一对别名不认，这一层兜住）。都不中 ⇒ 当「不在这只包里」。
        /// ⚠️ 不用 `LoadAsset&lt;T&gt;`：类型不对时 Unity 可能额外打一行错，而这里只想知道**在不在**。</summary>
        static bool HasAsset(AssetBundle b, string name)
        {
            try { if (b.Contains(name)) return true; } catch (Exception) { }
            try
            {
                var all = b.GetAllAssetNames();
                if (all != null)
                {
                    var want = name.ToLowerInvariant();
                    var tail = "/" + want;
                    for (int i = 0; i < all.Length; i++)
                    {
                        var a = all[i];
                        if (a == want || a.EndsWith(tail, StringComparison.Ordinal)) return true;
                    }
                }
            }
            catch (Exception) { }
            return false;
        }

        /// <summary>给自检用：清掉缓存，下一问重新加载。</summary>
        public static void Reset()
        {
            _bundle = null;
            _tried = false;
            _extras.Clear();
            _byName.Clear();
            _clipByName.Clear();
            LastExtrasSelfLoaded = LastExtrasReused = LastExtrasMissing = 0;
        }
    }
}
