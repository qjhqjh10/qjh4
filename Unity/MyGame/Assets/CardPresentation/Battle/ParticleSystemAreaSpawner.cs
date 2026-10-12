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
    /// <summary>原版 `ParticleSystemAreaSpawner`（TypeDefIndex 1104）—— 「在 boxSize 盒子里按速率随机生粒子」。
    /// 6 个序列化字段照原版（名字/类型/默认值全是 `dump.cs` 的字段表 + `.c` 里 ctor 的默认值：
    /// `boxSize (1,1,1)` · `maxPoolSize 5` · `useAutomaticSpawn true` · `spawnRate 1` · `chances 1`）。
    /// 判据 = `ParticleSystemAreaSpawner__{OnEnable,Toggle,SpawnParticle,SpawnParticles,IsAnyPoolItemActive,get_Pool,
    ///   CreatePooledItem,GetPoolObject,OnReturnedToPool,OnDestroyPoolObject,GetRandomPositionInsideBox,OnDrawGizmosSelected}.c`
    ///   ＋ `&lt;SpawnParticles>d__14.MoveNext.c`（那条循环）。
    /// ⚠️ 三个偏离，都是**为了在批处理里能推**（原版靠 Unity 的帧循环/协程）：
    ///   ① 循环不做成协程，改 `Advance(dt)`（由 `ScenarioParticleSpawnerBlender.Advance` 推）；
    ///   ② `OnEnable` 那两句挪进 `Configure(...)` 并由 `_ready` 挡住「`AddComponent` 当场触发的那一次」
    ///      （原版是反序列化完才 OnEnable —— 那时字段已经有值，而我们是运行时挂组件、字段还没填）；
    ///   ③ `Start()` 里 `StopAllCoroutines` 保留（我们没协程，留着只为逐句对齐）。
    /// 🔴 `Toggle(false)` **不恢复**模板粒子的 active（原版只在 `OnEnable` 里关它一次 —— 别自作聪明加回来）。</summary>
    public class ParticleSystemAreaSpawner : MonoBehaviour
    {
        // 原版这 6 个都是 `[SerializeField] private`（`spawnRate`/`chances` 还带 `[ShowIf("useAutomaticSpawn")]`、
        // `chances` 带 `[Range(0,1)]`）—— 我们这里**放成 public**：与本文件其它几个类同一套口径
        //（运行时由旁挂灌值、自检读回来核对），且不引 NaughtyAttributes。
        public Vector3 boxSize = new Vector3(1f, 1f, 1f);   // 原版 ctor 默认 (1,1,1)
        public ParticleSystem particleSystemPrefab;
        public int maxPoolSize = 5;
        public bool useAutomaticSpawn = true;
        public float spawnRate = 1f;
        public float chances = 1f;

        int totalPoolItems;                                 // 原版 +0x48（CreatePooledItem 里 +1 · OnDestroyPoolObject 里 −1）
        IObjectPool<ParticleSystem> m_Pool;                 // 原版 +0x50
        readonly bool collectionChecks = true;              // 原版 +0x58（ctor 里 = 1）
        bool _ready;                                        // 旁挂把 6 个字段填好了没有（见类注释 ②）
        bool _running;                                      // 原版 = 「那条协程在不在跑」
        float _t;                                           // 原版 = 距下一颗的累计秒（`WaitForSeconds(1/spawnRate)`）

        /// <summary>原版 `get_Pool`（判据 = `…__get_Pool.c`）：第一次访问时建池 ——
        /// `new ObjectPool&lt;ParticleSystem>(CreatePooledItem, GetPoolObject, OnReturnedToPool, OnDestroyPoolObject,
        ///  collectionChecks, defaultCapacity: 10, maxSize: maxPoolSize)`，并**先把 `totalPoolItems` 清零**。
        /// 那 4 个回调各是哪一个，是拿反汇编里的 `Method$…` 名逐个核过的（不是按参数位置猜的）。</summary>
        public IObjectPool<ParticleSystem> Pool
        {
            get
            {
                if (m_Pool == null)
                {
                    totalPoolItems = 0;
                    m_Pool = new ObjectPool<ParticleSystem>(
                        CreatePooledItem, GetPoolObject, OnReturnedToPool, OnDestroyPoolObject,
                        collectionChecks, 10, maxPoolSize);
                }
                return m_Pool;
            }
        }

        /// <summary>旁挂填字段 + 起循环（**等价于原版的 `OnEnable`** —— 见类注释 ②：
        /// `particleSystemPrefab.gameObject.SetActive(false)` 然后 `if (useAutomaticSpawn)` 起循环）。</summary>
        public void Configure(Vector3 box, ParticleSystem template, int maxPool, bool autoSpawn,
                              float rate, float chance)
        {
            boxSize = box;
            particleSystemPrefab = template;
            maxPoolSize = maxPool;
            useAutomaticSpawn = autoSpawn;
            spawnRate = rate;
            chances = chance;
            _ready = true;
            OnEnableBody();
        }

        void OnEnable()
        {
            // `AddComponent` 当场就会调到这里 —— 那时旁挂还没填字段（`_ready == false`）⇒ 什么都不做。
            // 手动挂在 prefab 上的（`_ready` 已置）再被启用时，走的就是原版那条 OnEnable。
            if (!_ready) return;
            OnEnableBody();
        }

        void OnEnableBody()
        {
            if (particleSystemPrefab == null)
            {
                Debug.LogError($"[EnvSpawner] `{name}` 的 `particleSystemPrefab` 是空的 —— "
                             + "原版这里会抛 NullReferenceException；我们出声并停手（不许静默）");
                _running = false;
                return;
            }
            // 判据 = 反汇编（RVA 0x675E10）：`prefab.gameObject.SetActive(false)` 在**起循环之前**，且**无条件**。
            particleSystemPrefab.gameObject.SetActive(false);
            _running = useAutomaticSpawn;
            _t = 0f;
        }

        /// <summary>判据 = `…__Toggle.c` + 反汇编（RVA 0x676310）：`StopAllCoroutines(); if (option) StartCoroutine(SpawnParticles());`
        /// —— **开与关都先停**。我们的循环不是协程（`Advance` 手推），`StopAllCoroutines` 留着只为逐句对齐。</summary>
        public void Toggle(bool option)
        {
            StopAllCoroutines();
            _running = option;
            _t = 0f;
        }

        /// <summary>判据 = `…__IsAnyPoolItemActive.c`：`Pool.CountInactive != totalPoolItems`
        /// ⇒ 真 = **有池里的东西被借出去了**（= 场上还有活着的粒子）。</summary>
        public bool IsAnyPoolItemActive() { return Pool.CountInactive != totalPoolItems; }

        /// <summary>原版那条循环的**一步**（判据 = `&lt;SpawnParticles>d__14.MoveNext.c`：
        /// `while (true) { if (Random.value &lt;= chances) SpawnParticle(); yield return new WaitForSeconds(1f / spawnRate); }`）。
        /// 🔴 用的是 `UnityEngine.Random`（**原版就是它** —— 这是表现层，不进引擎的确定性那条链）。</summary>
        public void Advance(float dt)
        {
            if (!_ready || !_running) return;
            if (spawnRate <= 0f) return;                    // 原版会 `WaitForSeconds(∞)` ⇒ 永不生成（等价）
            float step = 1f / spawnRate;
            _t += dt;
            if (_t < step) return;
            _t -= step;
            // 🔴 **一次 `Advance` 最多生成一颗** —— 原版那条协程带 `yield`，**一帧只被恢复一次**
            //（哪怕这一帧耗时 600 s）。自检里 `AdvanceBlendables(600f)` 那种大 dt 就靠这一条挡住：
            // 不这么写，600 s 会一次补出「每 7.7 s 一颗 × 78 拍」一串 `Instantiate`。
            if (_t > step) _t = 0f;                         // 落后太多 ⇒ 别把下一拍也提前（重新起算）
            if (UnityEngine.Random.value <= chances) SpawnParticle();
        }

        /// <summary>判据 = `…__SpawnParticle.c`：池里取一颗 → 摆到 `transform.position ± boxSize/2` 的随机点 →
        /// `Play(true)`（withChildren）。取到 null（`GetPoolObject` 报过错了）时原版**把池丢掉重建**再取一次。
        /// 🔴 位置是**世界轴**加减（`.c` 里直接拿 `transform.position` 加三个 `Random.Range`）。</summary>
        public void SpawnParticle()
        {
            Vector3 pos = GetRandomPositionInsideBox();
            ParticleSystem ps = Pool.Get();
            if (ps == null)
            {
                Debug.LogError($"[EnvSpawner] `{name}`：池里取不到粒子（`particleSystemPrefab` 那条链断了）"
                             + " —— 照原版把池丢掉重建后再取一次");
                m_Pool = null;
                ps = Pool.Get();
            }
            if (ps == null) return;
            ps.transform.position = pos;
            ps.Play(true);
        }

        /// <summary>判据 = `…__GetRandomPositionInsideBox.c` + 常量：`transform.position + (每轴
        /// `Random.Range(-box/2, +box/2)` )`。</summary>
        Vector3 GetRandomPositionInsideBox()
        {
            Vector3 p = transform.position;
            return new Vector3(
                p.x + UnityEngine.Random.Range(-boxSize.x * 0.5f, boxSize.x * 0.5f),
                p.y + UnityEngine.Random.Range(-boxSize.y * 0.5f, boxSize.y * 0.5f),
                p.z + UnityEngine.Random.Range(-boxSize.z * 0.5f, boxSize.z * 0.5f));
        }

        /// <summary>判据 = `…__CreatePooledItem.c`：`Instantiate(particleSystemPrefab, transform)` →
        /// `GetComponent&lt;ParticleSystemPoolable>()` → 没有就 `LogError` + `AddComponent` + 接上粒子引用 →
        /// `poolable.pool = Pool` → `totalPoolItems++`。
        /// （那两个泛型实参是从反汇编的 `Method$UnityEngine.Component.GetComponent&lt;ParticleSystemPoolable>()`
        ///  与 `AddComponent&lt;ParticleSystemPoolable>()` 读出来的，不是猜的。）</summary>
        ParticleSystem CreatePooledItem()
        {
            ParticleSystem ps = Instantiate(particleSystemPrefab, transform);
            if (ps == null) return null;
            var poolable = ps.GetComponent<ParticleSystemPoolable>();
            if (poolable == null)
            {
                Debug.LogError($"[EnvSpawner] `{name}`：粒子 prefab 上没有 `ParticleSystemPoolable`"
                             + "（回收那一跳靠它）—— 照原版就地补一个");
                poolable = ps.gameObject.AddComponent<ParticleSystemPoolable>();
                if (poolable != null) poolable.AssignParticleSystemReference(ps);
            }
            if (poolable == null) return null;
            poolable.pool = Pool;
            totalPoolItems++;
            return ps;
        }

        /// <summary>判据 = `…__GetPoolObject.c`（= `ObjectPool` 的 `actionOnGet`）：
        /// `system == null || system.gameObject == null` ⇒ `LogError` 后**原样返回**（池就把 null 交出去，
        /// 由 `SpawnParticle` 兜）；否则 `SetActive(true)`。</summary>
        void GetPoolObject(ParticleSystem system)
        {
            if (system == null || system.gameObject == null)
            {
                Debug.LogError($"[EnvSpawner] `{name}`：池里的粒子已经被销毁（`ObjectPool` 交出了 null）");
                return;
            }
            system.gameObject.SetActive(true);
        }

        /// <summary>= `actionOnRelease`。判据 = `…__OnReturnedToPool.c`：`system.gameObject.SetActive(false)`。</summary>
        void OnReturnedToPool(ParticleSystem system)
        {
            if (system != null && system.gameObject != null) system.gameObject.SetActive(false);
        }

        /// <summary>= `actionOnDestroy`。判据 = `…__OnDestroyPoolObject.c`：`totalPoolItems--`
        /// （反汇编 RVA 0x674790 就一句 `dec [rcx+0x48]` —— 池满时 `Release` 会走销毁这一跳）。</summary>
        void OnDestroyPoolObject(ParticleSystem system) { totalPoolItems--; }

        /// <summary>判据 = `…__OnDrawGizmosSelected.c`：`Gizmos.DrawWireCube(transform.position, boxSize)`。</summary>
        void OnDrawGizmosSelected() { Gizmos.DrawWireCube(transform.position, boxSize); }
    }
}
