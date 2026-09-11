// WarpforgeShaderLoader.cs — 运行时从 StreamingAssets 加载原版 shader bundle
//
// 为什么必须运行时加载：Unity 拒绝把 AssetBundle 里的 Shader 序列化成工程资产
// （CreateAsset(.asset) 报 "Shader is already an asset at ''"；存 .mat 会让 shader
//   引用变成空 GUID，重导入后是 Hidden/InternalErrorShader；存 .shader 直接被拒）。
// 所以 shader 只能随包带着，进入游戏再取出来用。
//
// 用法：WarpforgeShaderLoader.TryGetShader("Everguild/FX/Extra Color", out var sh)
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WarpforgeVFX
{
    public static class WarpforgeShaderLoader
    {
        /// <summary>主 shader bundle：shaders_assets_all.bundle 的副本，45 个 shader</summary>
        public const string BundleRelPath = "WarpforgeVFX/wf_shaders.bundle";

        /// <summary>补充 shader bundle：33 个「主包里没有」的原版 shader
        /// （Rays For Trail / Multi Ray / Particle Dissolve Mask / Spine/Special/HiddenPass 等，
        ///  影响 212 个效果）。用 工具/extract_missing_shaders.py 从
        ///  battleprefabs_vfxandmisc_assets_all.bundle 里抽出来单独打的包，约 480 KB。</summary>
        public const string ExtraBundleRelPath = "WarpforgeVFX/wf_shaders_extra.bundle";

        static bool _tried;
        static AssetBundle _bundle;
        static readonly Dictionary<string, Shader> _byName = new Dictionary<string, Shader>();

        /// <summary>有 shader 可用即为 ready。来源可能是自己那份 bundle，也可能是
        /// 从进程里已加载的 bundle 里捡回来的（见 EnsureLoaded 的说明）。</summary>
        public static bool Ready { get { EnsureLoaded(); return _byName.Count > 0; } }

        static void EnsureLoaded()
        {
            if (_tried) return;
            _tried = true;
            try
            {
                // 主包 + 补充包。少了补充包，那 33 个原版 shader 在**运行时**解析不到，
                // 相关效果会退回占位材质（编辑器里因为能看到源 bundle，反而是好的
                // —— 这种「编辑器好、进游戏坏」最难查，别只看编辑器）。
                bool mainOk = LoadOne(BundleRelPath, out bool mainCollided);
                LoadOne(ExtraBundleRelPath, out _);

                if (mainCollided)
                {
                    // 同一份内容若已在进程里加载过，LoadFromFile 会返回 null（Unity 限制）。
                    // 编辑器工具（EffectCompare / EffectDiag / EffectIso）会先把源 bundle
                    // 全量加载，其中 shaders_assets_all.bundle 和这份副本**是同一份内容**，
                    // 于是这里必然拿不到。
                    //
                    // ⚠️ 这里绝不能直接放弃 —— 一放弃，所有「只靠 bundle 兜底」的原版 shader
                    // 就全部解析失败，binder 退回占位材质，表现成「导出整个变成纯色块」。
                    // 实测：Environmental Condition Dark Angels Void Combat 那一整块纯白就是这么来的
                    // （占位材质是 URP/Particles/Unlit，_BaseMap 为空 → 白图 + 加法混合）。
                    // 而且它会污染「原版 vs 导出」的比对数字 —— 那一轮 957 组里有 106 个效果中招。
                    int got = HarvestFromLoadedBundles();
                    if (got > 0)
                        Debug.Log($"[WarpforgeVFX] shader bundle 被同内容的包顶掉了，改从已加载的 bundle 里捡回 {got} 个 shader");
                    else
                        Debug.LogWarning($"[WarpforgeVFX] shader bundle 加载失败，且已加载的 bundle 里也没捡到 shader");
                }

                Debug.Log($"[WarpforgeVFX] 共 {_byName.Count} 个原版 shader 可用（主包{(mainOk ? "成功" : "被顶掉")} + 补充包）");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WarpforgeVFX] shader bundle 加载异常: {e.Message}");
            }
        }

        /// <summary>加载一个 shader 包。collided=true 表示「同内容的包已经在进程里」，
        /// 由调用方决定要不要去捡。</summary>
        static bool LoadOne(string relPath, out bool collided)
        {
            collided = false;
            var path = Path.Combine(Application.streamingAssetsPath, relPath);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[WarpforgeVFX] 找不到 shader bundle: {path}");
                return false;
            }
            var b = AssetBundle.LoadFromFile(path);
            if (b == null) { collided = true; return false; }

            if (relPath == BundleRelPath) _bundle = b;
            LoadShadersFrom(b);
            return true;
        }

        /// <summary>把包里的 Shader 全捞出来。
        ///
        /// 用 LoadAllAssets&lt;Shader&gt;() 而不是「遍历 GetAllAssetNames 再挨个 LoadAsset」：
        /// 这些 bundle 的资产名是 **32 位 GUID**（不是路径，见 shaders_assets_all.bundle 的
        /// container），所以按 `.shader` 后缀过滤是筛不出来的；而要挨个试的话，
        /// 84 个源包合计上万个资产，每个都要抛一次类型异常，慢且刷屏。
        /// LoadAllAssets 每个包一次调用就够，而且不依赖 container
        /// ——补充包是从源包里抽出来重打的，container 是空的。</summary>
        static void LoadShadersFrom(AssetBundle b)
        {
            Shader[] shaders = null;
            try { shaders = b.LoadAllAssets<Shader>(); } catch { }
            if (shaders == null) return;
            foreach (var s in shaders)
            {
                if (s == null || string.IsNullOrEmpty(s.name)) continue;
                if (!_byName.ContainsKey(s.name)) _byName[s.name] = s;
            }
        }

        /// <summary>从进程里已加载的 bundle 里把 shader 捡回来。
        ///
        /// 用 LoadAllAssets&lt;Shader&gt;() 而不是「遍历 GetAllAssetNames 再挨个 LoadAsset」：
        /// 这些 bundle 的资产名是 **32 位 GUID**（不是路径，见 shaders_assets_all.bundle 的
        /// container），所以按 `.shader` 后缀过滤是筛不出来的；而要挨个试的话，
        /// 84 个源包合计上万个资产，每个都要抛一次类型异常，慢且刷屏。
        /// LoadAllAssets 每个包一次调用就够。</summary>
        static int HarvestFromLoadedBundles()
        {
            int before = _byName.Count;
            foreach (var b in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (b == null) continue;
                Shader[] shaders = null;
                try { shaders = b.LoadAllAssets<Shader>(); } catch { continue; }
                if (shaders == null) continue;
                foreach (var s in shaders)
                {
                    if (s == null || string.IsNullOrEmpty(s.name)) continue;
                    if (!_byName.ContainsKey(s.name)) _byName[s.name] = s;
                }
            }
            return _byName.Count - before;
        }

        public static bool TryGetShader(string name, out Shader shader)
        {
            EnsureLoaded();
            return _byName.TryGetValue(name, out shader) && shader != null;
        }

        public static IEnumerable<string> ShaderNames
        {
            get { EnsureLoaded(); return _byName.Keys; }
        }
    }
}
