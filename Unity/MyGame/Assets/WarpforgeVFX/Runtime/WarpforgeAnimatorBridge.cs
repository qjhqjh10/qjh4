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

        static AssetBundle _bundle;
        static bool _tried;
        static readonly Dictionary<string, RuntimeAnimatorController> _byName =
            new Dictionary<string, RuntimeAnimatorController>();

        /// <summary>按名字取一份原版控制器。取不到返回 null **并出声**（不许静默失败）。</summary>
        public static RuntimeAnimatorController TryGet(string controllerName)
        {
            if (string.IsNullOrEmpty(controllerName)) return null;
            if (_byName.TryGetValue(controllerName, out var cached)) return cached;

            var rc = From(_bundle, controllerName);
            if (rc == null)
            {
                EnsureLoaded();
                rc = From(_bundle, controllerName);
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

        static void EnsureLoaded()
        {
            if (_tried) return;
            _tried = true;
            var path = Path.Combine(Application.streamingAssetsPath, BundleRelPath);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[WarpforgeAnimatorBridge] 找不到 `{path}` —— 先跑一次 "
                               + "`工具/extract_missing_shaders.py --prefabs`");
                return;
            }
            try { _bundle = AssetBundle.LoadFromFile(path); }
            catch (Exception e)
            {
                Debug.LogWarning($"[WarpforgeAnimatorBridge] 加载 `{path}` 抛了：{e.GetType().Name}: {e.Message}");
            }
            if (_bundle == null)
                Debug.LogWarning($"[WarpforgeAnimatorBridge] `{path}` 加载不出（同一份内容可能已在进程里加载过？）"
                               + " —— 下面会去已加载的包里再找一遍");
        }

        /// <summary>给自检用：清掉缓存，下一问重新加载。</summary>
        public static void Reset() { _bundle = null; _tried = false; _byName.Clear(); }
    }
}
