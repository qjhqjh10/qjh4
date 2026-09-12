// VFXWhiteboard.cs — 白板：把一批效果按节奏一个个铺出来，看「播 → 消失」到底成不成立
//
// 为什么需要它：项目准则 #5「每个改动都要能实拍验证」。958 个 prefab 谈别的之前，
// 先在一块白板上跑通闭环 —— 每个效果确实渲染出内容、播完确实自己消失、反复播不涨对象数。
//
// 两种用法，走的是**同一份逻辑**（所以「工具里过了」和「场景里好看」是同一件事）：
//   · 挂进场景当组件 → play 模式下 Update 自己推（人看）
//   · 由 Editor 工具驱动 → 外面调 Tick(dt)（批处理里跑，不进 play 模式，结果确定可复现）
//
// ⚠️ 实例的 autoTick 会被关掉，由白板统一 Tick。两边都推 = 走两倍速。
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    public class VFXWhiteboard : MonoBehaviour
    {
        [Tooltip("要铺的效果名（留空 = 用 DefaultEffects）")]
        public string[] effects = new string[0];

        [Tooltip("网格列数")]
        public int columns = 5;

        [Tooltip("每格边长（世界单位）。效果按包围盒自动缩放到格子里")]
        public float cellSize = 4f;

        [Tooltip("错峰间隔：每隔这么久起播一个，避免 20 个同时炸成一团")]
        public float stagger = 0.25f;

        [Tooltip("整体缩放，叠在「自动适配格子」之上")]
        public float scale = 1f;

        [Tooltip("自动把每个效果缩放到格子里（关掉就按原尺寸铺）")]
        public bool fitToCell = true;

        [Tooltip("铺完一轮、全清空之后再从头来（看反复播会不会漏）")]
        public bool loop;

        readonly List<WarpforgeEffectPlayer> _spawned = new List<WarpforgeEffectPlayer>();
        readonly List<GameObject> _cells = new List<GameObject>();
        readonly Dictionary<string, float> _radius = new Dictionary<string, float>();
        List<string> _names = new List<string>();
        float _t;
        int _next;
        int _rounds;

        /// <summary>白板自己的存活口径（和全局 WarpforgeEffectPlayer.ActiveCount 分开看）。</summary>
        public int Alive { get { int n = 0; foreach (var p in _spawned) if (p != null) n++; return n; } }

        public int SpawnedTotal { get; private set; }
        public int Rounds { get { return _rounds; } }
        public bool Done { get { return _next >= _names.Count && Alive == 0; } }

        public static readonly string[] DefaultEffects =
        {
            "Antimatter Explosion", "AmbushEffect", "Lightning_Green", "Acid Rain Damage Target",
            "ArmourEffect_Bladeguard_Shield", "EnvironmentalCondition Leviathan Acid Rain",
            "Explosion_Ground", "BulletImpact_Metal", "Psychic_Lightning_down", "Smoke_Medium",
        };

        void Start()
        {
            if (Application.isPlaying) Spawn(ResolveEffects());
        }

        void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>推进：到点起播下一个、推进所有在播的实例。</summary>
        public void Tick(float dt)
        {
            _t += dt;

            while (_next < _names.Count && _t >= _next * stagger) SpawnOne(_names[_next++]);

            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                var p = _spawned[i];
                if (p == null) { _spawned.RemoveAt(i); continue; }
                p.Tick(dt);
            }

            if (loop && Done) { _rounds++; Spawn(_names); }
        }

        /// <summary>重新铺一轮（清空重来）。</summary>
        public void Spawn(IEnumerable<string> names)
        {
            Clear();
            _names = new List<string>(names);
            _t = 0f; _next = 0;
            SpawnedTotal = 0;
        }

        public void Clear()
        {
            foreach (var p in _spawned) if (p != null) p.Kill();
            _spawned.Clear();
            foreach (var c in _cells)
                if (c != null) { if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
            _cells.Clear();
            _names.Clear();
        }

        /// <summary>铺完没有就报出来，别让白板空着还显示「成功」。</summary>
        public List<string> Missing()
        {
            var lib = WarpforgeEffectLibrary.Instance;
            var miss = new List<string>();
            foreach (var n in _names)
            {
                WFEffectEntry e;
                if (lib == null || !lib.TryGet(n, out e)) miss.Add(n);
            }
            return miss;
        }

        void SpawnOne(string n)
        {
            var lib = WarpforgeEffectLibrary.Instance;
            WFEffectEntry e;
            if (lib == null || !lib.TryGet(n, out e))
            {
                Debug.LogWarning($"[WarpforgeVFX] 白板：效果库里没有 '{n}'");
                return;
            }

            int i = SpawnedTotal;
            int col = i % Mathf.Max(1, columns), row = i / Mathf.Max(1, columns);

            // 每格一个空锚点：位置和缩放都加在锚点上，效果自己的根变换一概不碰
            //（原版根节点上带着作者定的旋转/缩放，甚至有的停在场外 x≈100 —— 见
            //  WarpforgeEffectPlayer.Play 的注释。白板只负责把它摆到格子里。）
            // 排布是**垂直于相机的二维墙面**（X 向右、Y 向下），不是沿 Z 往后排 ——
            // 往后排的话越远的格子越小，一行行看过去全是不一样的尺寸。
            var cell = new GameObject($"cell {i} {n}");
            cell.transform.SetParent(transform, false);
            cell.transform.localPosition = new Vector3((col - (columns - 1) * 0.5f) * cellSize,
                                                       -row * cellSize * RowFactor,
                                                       0f);
            cell.transform.localScale = Vector3.one * (fitToCell ? CellRadius / MeasureRadius(e.prefab) : 1f) * scale;
            _cells.Add(cell);

            var p = WarpforgeEffectPlayer.Play(e, cell.transform);
            if (p == null) return;
            p.autoTick = false;             // 白板统一推，见文件头
            _spawned.Add(p);
            SpawnedTotal++;
        }

        float CellRadius { get { return cellSize * 0.42f; } }

        /// <summary>行距相对格宽的比例。留点余量，效果铺开时不至于糊到下一行。</summary>
        public const float RowFactor = 0.95f;

        /// <summary>「全屏 / 环境级叠加」的半径阈值（世界单位）。实测三档分得很开：
        ///   · 普通卡牌特效      半径 3–15
        ///   · 大范围闪电        半径 74–82（Lightning Burst Random add 等）—— 格子缩一下就能看，不算全屏
        ///   · 战场环境叠加     半径 **245.95**（Environmental Condition Black Legion Warp Storm 的
        ///                       Color change overlay / Color change smoke）—— 设计上就是盖满整个战场
        /// 所以取 100：只挡真正盖满屏的那一类。放进格子的话它们的粒子大到把别人的检查全盖掉
        /// （实拍图里那一大团粉色就是它，**不是坏 shader**）。</summary>
        public const float FullscreenRadius = 100f;

        public static bool IsFullscreen(GameObject prefab)
        {
            return prefab != null && MeasureRadiusStatic(prefab, Times) > FullscreenRadius;
        }

        /// <summary>效果在时间轴上的最大包围盒半径。带缓存（量一次要实例化+模拟六个时刻）。</summary>
        public float MeasureRadius(GameObject prefab)
        {
            if (prefab == null) return 1f;
            float r;
            if (prefab != null && _radius.TryGetValue(prefab.name, out r)) return r;
            r = MeasureRadiusStatic(prefab, Times);
            _radius[prefab.name] = r;
            return r;
        }

        // 量半径用的时刻。取到 3.0s 是为了和台账的多时刻扫描同一个窗口 ——
        // 有的效果（战场环境类）到 2.5s 才铺满全屏，只量到 2.0s 会把它当小效果，
        // 缩放一算错就变成「一格效果糊满整屏」。
        static readonly float[] Times = { 0.15f, 0.2f, 0.5f, 1.0f, 2.0f, 3.0f };

        /// <summary>量半径。要在**独立临时实例**上量 —— 挂到白板之后再量会把格子里的位移算进去。</summary>
        public static float MeasureRadiusStatic(GameObject prefab, float[] times)
        {
            var tmp = Instantiate(prefab);
            var binder = tmp.GetComponent<WarpforgeEffectBinder>();
            if (binder != null && !Application.isPlaying) binder.Apply();

            Bounds b = new Bounds(Vector3.zero, Vector3.one);
            bool first = true;
            foreach (var t in times)
            {
                foreach (var ps in tmp.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Simulate(t, withChildren: true, restart: true, fixedTimeStep: false);
                }
                foreach (var r in tmp.GetComponentsInChildren<Renderer>(true))
                {
                    if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
                }
            }
            DestroyImmediate(tmp);
            return Mathf.Max(0.05f, b.extents.magnitude);
        }

        string[] ResolveEffects()
        {
            return (effects != null && effects.Length > 0) ? effects : DefaultEffects;
        }
    }
}
