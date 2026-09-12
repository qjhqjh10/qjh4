// WarpforgeEffectLibrary.cs — 效果名 → prefab 的注册表
//
// 为什么需要它：958 个 prefab 是平铺在 Prefabs/ 里的，没有任何索引。
// 没有索引就只能把 prefab 拖进 Inspector 逐个引用，玩法代码没法「按名字播一个特效」。
//
// 为什么用 ScriptableObject 而不是 Resources.LoadAll：
//   Resources.LoadAll 会把整个目录**无条件全打进包**（958 个 prefab + 1.8 GB 贴图）。
//   SO 的引用列表是「被引用才进包」，而且编辑器与打包后行为一致 —— 不会出现
//   「编辑器里好好的、打包后 Load 不到」这种本项目最难查的 bug。
//
// 资产由 菜单 Tools > Warpforge > 生成效果库（EffectLibraryBuilder）自动生成，
// 不要手改 —— 手改会在下次生成时被覆盖。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    [Serializable]
    public class WFEffectEntry
    {
        [Tooltip("效果名（= 原版 prefab 名 = Prefabs/ 下的文件名，不含扩展名）")]
        public string name;

        [Tooltip("导出的 prefab")]
        public GameObject prefab;

        [Tooltip("原版 AnimFXController.destroyTime：多久后自动销毁。<0 = 原版没有这个组件")]
        public float destroyTime = -1f;

        [Tooltip("原版 AnimFXController.exitDestroyTime：调 Exit() 之后再活多久才销毁")]
        public float exitDestroyTime = -1f;

        [Tooltip("原版 AnimFXController.preventDestroy：原版不让它自己销毁（循环类/由牌局管理的）")]
        public bool preventDestroy;

        [Tooltip("粒子系统自然播完的时长（循环的发射器算不出来，记 -1）。destroyTime 缺失时的兜底")]
        public float natural = -1f;

        [Tooltip("含循环发射器 —— 它自己不会停，靠 destroyTime / 调用方收")]
        public bool loops;

        [Tooltip("台账判定：Z 对得上 / E 亮度密度不对 / C 导出整个丢了 / D 只有导出有 / T 时序错位 / W 两边空")]
        public string verdict = "";

        [Tooltip("台账置信度：高 / 中 / 低。低的说明峰值亮点太少，判定本身不可靠")]
        public string confidence = "";

        [Tooltip("台账亮度比（导出亮度/原版亮度），1.0 = 完全一致")]
        public float ratio = -1f;

        /// <summary>自动销毁时长。显式传参 &gt; 原版 destroyTime &gt; 粒子自然时长 &gt; 安全兜底。</summary>
        public float AutoLifetime(float safety = WarpforgeEffectPlayer.SafeDestroyTime)
        {
            if (destroyTime > 0f) return destroyTime;
            if (natural > 0f) return natural;
            return safety;
        }
    }

    public class WarpforgeEffectLibrary : ScriptableObject
    {
        [Tooltip("由 EffectLibraryBuilder 生成，别手改")]
        public WFEffectEntry[] entries = new WFEffectEntry[0];

        [Tooltip("生成时间 / 来源说明，排查「库和 prefab 对不上」时看这里")]
        public string generated = "";

        static WarpforgeEffectLibrary _instance;
        Dictionary<string, WFEffectEntry> _map;
        List<string> _names;

        /// <summary>运行时取库。放在 Resources/WarpforgeVFX/ 下。</summary>
        public const string ResourcesPath = "WarpforgeVFX/WarpforgeEffectLibrary";

        /// <summary>当前使用的库。可以外部替换（比如白板测试只装一个子集，避免把 1.8 GB 全打进包）。
        /// 赋值会清掉内部索引缓存。</summary>
        public static WarpforgeEffectLibrary Instance
        {
            get
            {
                if (_instance == null) _instance = Resources.Load<WarpforgeEffectLibrary>(ResourcesPath);
                return _instance;
            }
            set { _instance = value; if (value != null) value.Rebuild(); }
        }

        public static bool Available { get { return Instance != null; } }

        /// <summary>建索引。生成器写完资产后会调一次；运行时懒加载也会调。</summary>
        public void Rebuild()
        {
            _map = new Dictionary<string, WFEffectEntry>(entries.Length, StringComparer.Ordinal);
            _names = new List<string>(entries.Length);
            foreach (var e in entries)
            {
                if (e == null || string.IsNullOrEmpty(e.name) || _map.ContainsKey(e.name)) continue;
                _map[e.name] = e;
                _names.Add(e.name);
            }
        }

        void EnsureIndex()
        {
            if (_map == null) Rebuild();
        }

        public int Count { get { EnsureIndex(); return _map.Count; } }

        public IEnumerable<string> Names { get { EnsureIndex(); return _names; } }

        /// <summary>按名字取。找不到返回 false（调用方决定是警告还是静默）。</summary>
        public bool TryGet(string name, out WFEffectEntry entry)
        {
            EnsureIndex();
            return _map.TryGetValue(name, out entry) && entry != null && entry.prefab != null;
        }

        public WFEffectEntry Get(string name)
        {
            WFEffectEntry e;
            return TryGet(name, out e) ? e : null;
        }

        /// <summary>按关键字模糊找（给「我记不清叫什么」用，比如 PlayFuzzy("Explosion")）。
        /// 多个命中时返回最短的那个 —— 名字最短的通常是最基础的那个效果。</summary>
        public List<WFEffectEntry> Find(string keyword)
        {
            EnsureIndex();
            var hit = new List<WFEffectEntry>();
            foreach (var kv in _map)
            {
                if (kv.Key.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (kv.Value == null || kv.Value.prefab == null) continue;   // 断了的引用别往外给
                hit.Add(kv.Value);
            }
            hit.Sort((a, b) => a.name.Length.CompareTo(b.name.Length));
            return hit;
        }
    }
}
