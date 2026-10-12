using System;
using UnityEngine;
using UnityEngine.Pool;      // 原版 `ParticleSystemAreaSpawner` 的池子就是 `UnityEngine.Pool.ObjectPool<ParticleSystem>`
                            // （判据 = 反汇编里那句 `new ObjectPool<ParticleSystem>(create, onGet, onRelease, onDestroy, …)`）
// 🆕 2026-10-07：`FlareScenarioToggler` 管的是 `LensFlareComponentSRP`（`Unity.RenderPipelines.Core.Runtime`）。
// ⚠️ **用别名、别 `using UnityEngine.Rendering;`** —— 那个命名空间里**也有一个 `ObjectPool<T>`**，
//    与 `UnityEngine.Pool.ObjectPool<T>` 撞名 ⇒ 上面那行池子会 CS0104「不明确的引用」。
using LensFlareComponentSRP = UnityEngine.Rendering.LensFlareComponentSRP;

namespace CardPresentation
{
    /// <summary>原版 `ParticleSystemAreaSpawnerController`（TypeDefIndex 1107）—— 一组生成器的**加权随机**调度器。
    /// 判据 = `ParticleSystemAreaSpawnerController__{OnEnable,Toggle,SpawnParticle,SelectRandomWeightedItem,
    ///   IsAnyParticlePoolActive,FetchAllParticleSpawners}.c` · `&lt;SpawnParticles>d__6.MoveNext.c` ·
    ///   `…ParticleSpawnDefinition__{TrySpawnParticles,IsAnyPoolItemActive}.c`。
    /// ⚠️ **这一类在我们数据里 0 个实例**（4 条 blender 的 `controllers` 全是空数组；bundle 里那 3 个
    ///   controller 自己 `startOnEnable` 自启、不被任何 blendable 引用）⇒ 移植是为了**完备**（铁律 11），
    ///   运行时目前不会有人建它。它同样改成 `Advance(dt)` 手推。</summary>
    public class ParticleSystemAreaSpawnerController : MonoBehaviour
    {
        /// <summary>原版嵌套类（TypeDefIndex 1105，字段：spawner 0x10 · weight 0x18 · chances 0x1C）。
        /// ⚠️ 它是**普通 `[Serializable]` 类**（dump.cs:49909 里没有基类、字段从 0x10 起）——
        ///   这决定了下面 `FetchAllParticleSpawners` 那条不能照抄，见那儿的注释。</summary>
        [Serializable]
        public class ParticleSpawnDefinition
        {
            public ParticleSystemAreaSpawner particleSystemAreaSpawner;   // 原版 `[SerializeField] private`
            public float weight;                                          // 同上（+0x18）
            public float chances;                                         // 同上（+0x1C，原版带 `[Range(0,1)]`）

            public float Weight { get { return weight; } }

            /// <summary>判据 = `…ParticleSpawnDefinition__TrySpawnParticles.c`：
            /// `if (Random.value > chances) return; spawner.SpawnParticle();`</summary>
            public void TrySpawnParticles()
            {
                if (UnityEngine.Random.value > chances) return;
                if (particleSystemAreaSpawner == null) return;
                particleSystemAreaSpawner.SpawnParticle();
            }

            /// <summary>判据 = `…ParticleSpawnDefinition__IsAnyPoolItemActive.c` = 转调它那颗 spawner 的同名方法。</summary>
            public bool IsAnyPoolItemActive()
            {
                return particleSystemAreaSpawner != null && particleSystemAreaSpawner.IsAnyPoolItemActive();
            }
        }

        public ParticleSpawnDefinition[] particleSystemAreaSpawners;  // 原版 `[SerializeField] private`
        public float spawnRate = 1f;      // ctor 默认 1.0f（`…Controller__.ctor.c` 的 0x28；原版 private）
        public bool startOnEnable = true; // ctor 默认 1（0x2C；原版 private）

        bool _running;
        float _t;

        /// <summary>判据 = `…__OnEnable.c`：`if (startOnEnable) { StopAllCoroutines(); StartCoroutine(SpawnParticles()); }`</summary>
        void OnEnable()
        {
            if (!startOnEnable) return;
            StopAllCoroutines();
            _running = true;
            _t = 0f;
        }

        /// <summary>判据 = `…__Toggle.c` + 反汇编（RVA 0x6742A0）：`StopAllCoroutines(); if (option) StartCoroutine(...)`
        /// —— `false` 那一支**只停**（不启动）。</summary>
        public void Toggle(bool option)
        {
            StopAllCoroutines();
            _running = option;
            _t = 0f;
        }

        /// <summary>🔴 **原版这个方法很怪，照抄不美化**（`.c` 与反汇编 RVA 0x6754F0 逐条对上）：
        ///   `definitions.Length &lt; 1` ⇒ `false`；否则它**调一次 `Pool.CountInactive` 却把结果丢掉**、
        ///   **无条件 `return true`** ⇒ 可观察行为 = **「有定义就 true」**（与池里有没有粒子无关）。
        ///   那次读取的副作用是 `get_Pool()`（池会被建出来）。</summary>
        public bool IsAnyParticlePoolActive()
        {
            if (particleSystemAreaSpawners == null || particleSystemAreaSpawners.Length < 1) return false;
            var def = particleSystemAreaSpawners[0];
            if (def == null || def.particleSystemAreaSpawner == null) return false;   // 原版这里抛 NRE
            int _ = def.particleSystemAreaSpawner.Pool.CountInactive;
            return true;
        }

        /// <summary>原版那条循环的**一步**（判据 = `&lt;SpawnParticles>d__6.MoveNext.c`：
        /// `while (true) { SelectRandomWeightedItem().TrySpawnParticles(); yield return new WaitForSeconds(1f/spawnRate); }`）。
        /// 与 spawner 那条同理：**一次 `Advance` 最多一颗**（原版一帧只恢复一次协程）。</summary>
        public void Advance(float dt)
        {
            if (!_running || spawnRate <= 0f) return;
            float step = 1f / spawnRate;
            _t += dt;
            if (_t < step) return;
            _t -= step;
            if (_t > step) _t = 0f;
            SpawnParticle();
        }

        /// <summary>判据 = `…__SpawnParticle.c`：`SelectRandomWeightedItem().TrySpawnParticles()`（选完还要再过一次 chances）。</summary>
        void SpawnParticle()
        {
            var def = SelectRandomWeightedItem();
            if (def != null) def.TrySpawnParticles();
        }

        /// <summary>判据 = `…__SelectRandomWeightedItem.c`：按 `Weight` 求总和 → `Random.value * 总和` 逐个减 →
        /// 第一个减到 `&lt;= 0` 的就是它；**一个都没命中时返回第 0 个**（原版如此）。</summary>
        ParticleSpawnDefinition SelectRandomWeightedItem()
        {
            if (particleSystemAreaSpawners == null || particleSystemAreaSpawners.Length == 0) return null;
            float total = 0f;
            for (int i = 0; i < particleSystemAreaSpawners.Length; i++)
                if (particleSystemAreaSpawners[i] != null) total += particleSystemAreaSpawners[i].Weight;
            float r = UnityEngine.Random.value * total;
            for (int i = 0; i < particleSystemAreaSpawners.Length; i++)
            {
                if (particleSystemAreaSpawners[i] == null) continue;
                r -= particleSystemAreaSpawners[i].Weight;
                if (r <= 0f) return particleSystemAreaSpawners[i];
            }
            return particleSystemAreaSpawners[0];
        }

        /// <summary>原版是 NaughtyAttributes 的 `[Button]`（**只在编辑器里手点**，运行时不调）。
        /// 🔴 原版那一句 `GetComponentsInChildren&lt;ParticleSystemAreaSpawnerController.ParticleSpawnDefinition>()`
        ///   **在 C# 里编不过** —— `GetComponentsInChildren&lt;T>` 要求 `T : Component`，而 `ParticleSpawnDefinition`
        ///   是个普通 `[Serializable]` 类（`dump.cs:49909`）⇒ 这里按**意图**落地：把子树上那批
        ///   `ParticleSystemAreaSpawner` 各包成一条定义、引用接上（权重/chances 保持字段默认值）。
        ///   **这一条不是逐句移植**，如实记着。</summary>
        void FetchAllParticleSpawners()
        {
            var found = GetComponentsInChildren<ParticleSystemAreaSpawner>(true);
            var list = new ParticleSpawnDefinition[found.Length];
            for (int i = 0; i < found.Length; i++)
                list[i] = new ParticleSpawnDefinition { particleSystemAreaSpawner = found[i] };
            particleSystemAreaSpawners = list;
            Debug.LogWarning($"[EnvSpawner] `{name}`：`FetchAllParticleSpawners` 是**近似移植**"
                           + $"（原版那句泛型调用在 C# 里编不过，见方法注释）—— 收进 {found.Length} 条");
        }
    }
}
