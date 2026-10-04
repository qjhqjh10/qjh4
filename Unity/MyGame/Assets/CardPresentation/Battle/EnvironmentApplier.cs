// EnvironmentApplier.cs — **战场环境切换的执行器**（§三 第 30 条 · 4 环境的战场侧）
//
// 复刻原版那条链（判据 → `资料/加时与冲突模式_原版规格.md` 的「进攻卡生效时会整场换掉战场环境」那一节；
// 方法体逐句读过：`ScenarioEnvironmentConditionsManager__*.c` / `ScenarioEnvironmentConditionSO__*.c`）：
//   `ApplyEnvironment(so, instant)`：
//     · `CurrentEnvironment == so` ⇒ **直接 return（不重播）**
//     · 否则 **先 `DisableEnvironment(旧)`**，再记下新的，再 `SO.EnableEnvironment(instant)`，
//       最后 `SetRegisteredBlendeablesState(返回值)`
//   `SO.EnableEnvironment(instant)`：
//     ① `ApplyAmbientColor`：`RenderSettings.ambientLight` ＋ **补间** `_AmbientColorBlend`（时长 `SO.blendTime`）
//     ② `ApplyFog`：**补间** `RenderSettings.fogColor / fogDensity`（`ToggleFog` 只切 `fog` 开关）
//     ③ `scenarioObjects` 有值 ⇒ `Instantiate(prefab, Camera.main.pos/rot)` →
//        **对子树里每个 blendable 调 `DoScenarioBlend{duration=blendTime, direction=true, targetValue=1}`**
//     · **返回** 另一份 options：`{direction = targetValue = SO.defaultScenarioObjectsState}`
//   `SO.DisableEnvironment(instant)`：对**旧实例**的每个 blendable 调
//     `DoScenarioBlend{duration, direction=false, onComplete=计数回调}`，**全部回调到齐才 Destroy 那个实例**。
//
// 🔴 2026-09-30 晚补的两半（原来只有「实例化 + 雾/环境光补间」）：
//   ① **实例内的 blendable**（`Battle/ScenarioBlendables.cs` + `Core/EnvBlendables.cs` 的旁挂）
//   ② **13 场战场【自己】挂的那批** —— 它们 `Register` 进管理器的 `cacheBlendeables`，
//      每次换环境都被 `SetRegisteredBlendeablesState` 用 **`SO.defaultScenarioObjectsState`** 刷一遍。
//      实测该值 **38/55 条是 0** ⇒ **放一张进攻卡会把战场自己那批粒子/材质关掉/淡出**
//      （原版 = 战场 FX ↔ 环境 prefab FX 的**交叉换场**）。判据与逐场清单 → `资料/战场场景线_交接.md` §二 第 2 条。
//
// ⚠️ **一条如实记着**：
//   ① 原版 `blendTime` 的补间是 DOTween；这里用**手推的线性插值**（批处理下没有帧循环 ⇒
//      必须能 `Advance(dt)` 手动推，自检才验得了），blendable 那族同理。
//   🆕 2026-10-06 战-A：`ScenarioParticleSpawnerBlender`（4 个实例）**已经补上了** —— 见
//      `ScenarioBlendables.cs` 末尾那段（原版那三个组件 + 旁挂那 6 个字段）。
//      ⚠️ **仍然没接的**：原版 `IScenarioEnvironmentBlendeable` 有 **9 个实现类**，我们只做 5 个；
//      剩下 4 个（`FlareScenarioToggler` 6 实例 · `ScenarioAnimationBlend` 2 · `ScenarioGenericMaterialBlend` 1 ·
//      `TauCannonAnimationStopper` 2）**连旁挂都没收**（`gen_env_blendables.py` 的 `CLASSES` 里没有它们）
//      ⇒ 这一条**不在「已做」里**，判据 → `资料/普查产出_1006/战A_第3_4条.md`。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardPresentation
{
    public class EnvironmentApplier : MonoBehaviour
    {
        /// <summary>当前生效的那条 SO 名（原版 `CurrentEnvironment`）。空 = 还没应用过。</summary>
        public string CurrentSO { get; private set; }

        /// <summary>当前那件环境 prefab 的实例（原版 `scenarioObjects` 的实例）。</summary>
        public GameObject CurrentInstance { get; private set; }

        /// <summary>补间进度 0→1（1 = 已经推完）。自检读它。</summary>
        public float BlendT { get; private set; } = 1f;
        /// <summary>补间总时长（秒）。0 = 立刻到位。</summary>
        public float BlendDuration { get; private set; }
        /// <summary>累计推过的秒数（自检用）。</summary>
        public float Elapsed { get; private set; }

        Color _fog0, _fog1; float _den0, _den1;
        Color _amb0, _amb1; float _blend0, _blend1;

        // ---- 本轮新增：blendable 那两半 ----
        /// <summary>本场用哪个战场（`battlearena3` 这种键）。空 = 从活动场景名推（`Battle_<场>`）。</summary>
        public string ArenaKey;

        /// <summary>**战场自己**那批 blendable（原版 `cacheBlendeables`），第一次 `Apply` 时建一次。</summary>
        readonly List<ScenarioBlendable> _sceneBlendables = new List<ScenarioBlendable>();
        bool _sceneBuilt;

        /// <summary>正在淡出、还没销毁的旧实例（原版 `DisableEnvironment` 的计数回调那套）。</summary>
        class FadeOut
        {
            public GameObject go; public int pending; public float left; public float timeout; public string so;
            public ScenarioBlendable[] mine;      // 这个实例里的那批（执行器推它们）
        }
        readonly List<FadeOut> _fading = new List<FadeOut>();

        /// <summary>当前实例里挂上的那批（执行器推它们）。</summary>
        ScenarioBlendable[] _currentBlendables;

        /// <summary>自检用：场景侧挂上了几个 blendable / 实例侧几个 / 还有几个旧实例在淡出。</summary>
        public int SceneBlendableCount { get { return _sceneBlendables.Count; } }
        public int InstanceBlendableCount { get; private set; }
        public int FadingCount { get { return _fading.Count; } }

        void Awake()
        {
            // 原版 `Awake` 里先按 Default 走一遍（instant）——但我们**没有**「本场 Default 是哪条」的运行时来源
            // （那是建场期写进 `ArenaEnvGlobal` / `RenderSettings` 的）⇒ **这里不动**，
            // 免得把 §28 那批已验收的逐场雾/环境光值覆盖掉。第一次 `Apply()` 才接管。
        }

        void Update()
        {
            if (BlendT < 1f) Advance(Time.deltaTime);
            AdvanceBlendables(Time.deltaTime);
        }

        /// <summary>要不要**接管**整场的雾/环境光（`Apply` 之后为 true）。</summary>
        public bool Applied { get; private set; }

        /// <summary>应用一条环境。`instant` = 直接到位（原版 `EnableEnvironment(instant)`）。
        /// 同一条 SO **不重播**（原版那条判据）。传 null / 空 SO = **撤掉当前环境**（回默认态）。</summary>
        public EnvironmentApplier Apply(EnvironmentConditions.Item it, bool instant)
        {
            string want = it != null ? it.so : "";
            if (Applied && CurrentSO == want) return this;      // 同一条不重播
            string oldSo = CurrentSO;
            CurrentSO = want;

            // ---- ③ 物件：**先撤旧的（淡出 + 全回调到齐才销毁）**，再起新的（原版 `DisableEnvironment` → `Instantiate`）----
            if (CurrentInstance != null)
            {
                BeginFadeOut(CurrentInstance, oldSo, _currentBlendables);
                CurrentInstance = null; InstanceBlendableCount = 0; _currentBlendables = null;
            }
            if (EnvironmentConditions.HasPrefab(it))
            {
                var prefab = FindPrefab(it.prefabName);
                if (prefab == null)
                {
                    Debug.LogWarning($"[Env] 取不到环境 prefab `{it.prefabName}`（SO `{it.so}`）—— "
                                   + "这一条只补间雾/环境光，**不加物件**（不许静默）");
                }
                else
                {
                    var cam = Camera.main;
                    var pos = cam != null ? cam.transform.position : Vector3.zero;
                    var rot = cam != null ? cam.transform.rotation : Quaternion.identity;
                    CurrentInstance = Instantiate(prefab, pos, rot);
                    CurrentInstance.name = "Env_" + it.prefabName;
                    // 原版 `EnableEnvironment` ③：对**新实例子树里每个** blendable 调 `DoScenarioBlend(lVar7)`
                    _currentBlendables = AttachBlendables(CurrentInstance, it.prefabName);
                    InstanceBlendableCount = _currentBlendables.Length;
                }
            }

            // ---- ① / ② 雾与环境光：记起止值，按 `blendTime` 推 ----
            _fog0 = RenderSettings.fogColor; _den0 = RenderSettings.fogDensity;
            _amb0 = RenderSettings.ambientLight; _blend0 = Shader.GetGlobalFloat("_AmbientColorBlend");
            if (it != null)
            {
                _fog1 = ToColor(it.fogColor, _fog0);
                _den1 = it.fogDensity;
                _amb1 = ToColor(it.ambientColor, _amb0);
                _blend1 = it.ambientBlend;
                RenderSettings.fog = true;          // 判据：`ScenarioEnvironmentConditionSO__ToggleFog`（**只切开关**）
            }
            else
            {
                // 撤掉环境 ⇒ 回到「建场时那套」：这里只能把混合量收回 0（原版是 `DisableEnvironment` 走另一套）
                _fog1 = _fog0; _den1 = _den0; _amb1 = _amb0; _blend1 = 0f;
            }
            Applied = true;
            BlendDuration = (it != null) ? Mathf.Max(0f, it.blendTime) : 0f;
            Elapsed = 0f;
            if (instant || BlendDuration <= 0f) { BlendT = 1f; Push(); }
            else { BlendT = 0f; }

            // ---- ④ 战场自己那批（原版 `SetRegisteredBlendeablesState(lVar8)`）----
            //   方向 = `SO.defaultScenarioObjectsState`（**撤环境时回默认 = 1**：13 条 Default 实测全是 1）
            EnsureSceneBlendables();
            bool dir = (it != null) ? (it.defaultScenarioObjectsState != 0) : true;
            var opts = new ScenarioBlendOptions
            {
                duration = BlendDuration, direction = dir, targetValue = dir ? 1f : 0f, onComplete = null,
            };
            for (int i = 0; i < _sceneBlendables.Count; i++)
                if (_sceneBlendables[i] != null) _sceneBlendables[i].DoScenarioBlend(opts);

            return this;
        }

        /// <summary>手动推进一步（**批处理/自检唯一的路** —— 那里没有帧循环）。</summary>
        public EnvironmentApplier Advance(float dt)
        {
            if (BlendT < 1f)
            {
                Elapsed += dt;
                BlendT = Mathf.Clamp01(BlendDuration <= 0f ? 1f : Elapsed / BlendDuration);
                Push();
            }
            return this;
        }

        /// <summary>推进 blendable / 淡出销毁（**唯一的驱动者**：场景侧 + 当前实例 + 正在淡出的旧实例）。
        /// 批处理下没有帧循环 ⇒ 组件自己的 `Update` 不存在（见 `ScenarioBlendable` 的注释），只能靠这里手推。</summary>
        public void AdvanceBlendables(float dt)
        {
            for (int i = 0; i < _sceneBlendables.Count; i++)
                if (_sceneBlendables[i] != null) _sceneBlendables[i].Advance(dt);
            if (_currentBlendables != null)
                for (int i = 0; i < _currentBlendables.Length; i++)
                    if (_currentBlendables[i] != null) _currentBlendables[i].Advance(dt);

            for (int i = _fading.Count - 1; i >= 0; i--)
            {
                var f = _fading[i];
                if (f.go == null) { _fading.RemoveAt(i); continue; }
                f.left += dt;
                if (f.mine != null)
                    for (int k = 0; k < f.mine.Length; k++)
                        if (f.mine[k] != null) f.mine[k].Advance(dt);
                // 原版靠**每个 blendable 的 OnComplete** 计数归零才 Destroy；
                // 这里再加一道**兜底超时**（`blendTime + 1s`）—— 有组件永远不回调时**出声**，不静默挂着。
                if (f.pending <= 0) { FinishFadeOut(f, false); _fading.RemoveAt(i); }
                else if (f.left > Mathf.Max(1f, f.timeout)) { FinishFadeOut(f, true); _fading.RemoveAt(i); }
            }
        }

        // ---------------- blendable 两半 ----------------

        /// <summary>把旁挂里那几个 blendable 组件挂到 prefab 实例上，并立刻按原版 `lVar7`（direction=true）驱动一次。
        /// prefab 侧的目标按**层级路径**在实例里找（旁挂的 path 从 prefab 根开始 ⇒ 去掉第一段）。</summary>
        ScenarioBlendable[] AttachBlendables(GameObject host, string prefabName)
        {
            var items = EnvBlendables.ForPrefab(prefabName);
            var made = new List<ScenarioBlendable>(items.Length);
            foreach (var it in items)
            {
                var c = ScenarioBlendableFactory.Create(it, PickHost(host, it),
                    t => FindPSInPrefab(host, t), t => FindRendererInPrefab(host, t), t => FindGoInPrefab(host, t),
                    t => FindSpawnerInPrefab(host, t), t => FindControllerInPrefab(host, t), true);
                if (c == null) continue;
                c.DoScenarioBlend(new ScenarioBlendOptions
                { duration = BlendDuration, direction = true, targetValue = 1f, onComplete = null });
                made.Add(c);
            }
            return made.ToArray();
        }

        /// <summary>场景侧那批：**只在第一次 `Apply` 时建一次**（原版是组件自己在 `Start`/`Initialize` 里 `Register`，
        /// 之后每次换环境都被刷一遍 ⇒ 我们建一次、放进 `_sceneBlendables` 当那份缓存）。
        /// ⚠️ 我们的战场是 `ArenaBuilder` 按清单**平铺**建的（原版的 `Scenario/Particles/…` 嵌套没有复刻）
        ///   ⇒ 旁挂那边给的是**名字 + 世界位置**，这里**同名取最近**（与 `ArenaBuilder.SameObject` 同一族判据的宽松版：
        ///   那边是「并清单」要求逐字相同，这边是运行时回头找、同名会有多个）。</summary>
        void EnsureSceneBlendables()
        {
            if (_sceneBuilt) return;
            _sceneBuilt = true;
            var items = EnvBlendables.ForArena(ResolveArenaKey());
            if (items.Length == 0)
            {
                Debug.LogWarning($"[Env] 场景侧没有 blendable 旁挂（场 `{ResolveArenaKey()}`）—— "
                               + "「放进攻卡把战场自己那批粒子关掉」这一半不会发生。跑 `python 工具/gen_env_blendables.py`。");
                return;
            }
            var root = ArenaRoot();
            int made = 0, miss = 0;
            foreach (var it in items)
            {
                if (it == null) continue;
                var host = PickSceneHost(root, it, out miss);
                if (host == null) { miss++; continue; }
                var c = ScenarioBlendableFactory.Create(it, host,
                    t => FindPSInScene(root, t), t => FindRendererInScene(root, t), t => FindGoInScene(root, t),
                    t => FindSpawnerInScene(root, t), t => FindControllerInScene(root, t), true);
                if (c != null) { _sceneBlendables.Add(c); made++; }
            }
            Debug.Log($"[Env] 场景侧 blendable：旁挂 {items.Length} 条 → 挂上 {made} 个"
                    + (miss > 0 ? $"；**{miss} 条找不到宿主对象**（出声，不猜）" : ""));
        }

        static string ResolveArenaKey()
        {
            string n = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(n)) return ArenaByArmy.DefaultScene;
            // `Battle_<场>`（一局一战场那条链）⇒ 直接取后缀
            int i = n.LastIndexOf('_');
            if (i >= 0 && n.StartsWith("Battle")) return n.Substring(i + 1);
            // 通用 `Battle.unity`（**自检跑的就是它**，里面烘的是默认那场 = `battlearena1`）
            if (n == "Battle") return ArenaByArmy.DefaultScene;
            return n;
        }

        /// <summary>战场上那些对象挂在哪个根下。战斗场景里是 **`Arena3D`**（`BattleScene.cs` 建的那个容器，
        /// 自检里找战场对象用的也是它）；退一步找 `Warpforge_<场>`。都找不到 ⇒ 返回 null = 全场景搜（并出声）。</summary>
        Transform ArenaRoot()
        {
            var go = GameObject.Find("Arena3D");
            if (go != null) return go.transform;
            string key = ResolveArenaKey();
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.parent == null && t.name == "Warpforge_" + key) return t;
            Debug.Log($"[Env] 没找到战场根（`Arena3D` / `Warpforge_{key}`）⇒ 场景侧 blendable 改在全场景里找"
                    + "（可能变慢/找错）");
            return null;
        }

        // ---- prefab 侧的对象解析（按路径）----
        static Transform FindByPath(GameObject root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path)) return null;
            var segs = path.Split('/');
            var t = root.transform;
            for (int i = 1; i < segs.Length; i++)          // 第 0 段 = prefab 根自己
            {
                t = FindChild(t, segs[i]);
                if (t == null) return null;
            }
            return t;
        }

        /// <summary>子节点按**归一化名字**找（原版真有带尾随空格的名字：`'Flames '` ⇒ 别只比逐字）。</summary>
        static Transform FindChild(Transform p, string name)
        {
            for (int i = 0; i < p.childCount; i++)
                if (Norm(p.GetChild(i).name) == Norm(name)) return p.GetChild(i);
            return null;
        }

        static string Norm(string s) { return s == null ? "" : s.Trim(); }

        static ParticleSystem FindPSInPrefab(GameObject root, EnvBlendables.Target t)
        { var tr = FindByPath(root, t.path); return tr != null ? tr.GetComponent<ParticleSystem>() : null; }
        static Renderer FindRendererInPrefab(GameObject root, EnvBlendables.Target t)
        { var tr = FindByPath(root, t.path); return tr != null ? tr.GetComponent<Renderer>() : null; }
        static GameObject FindGoInPrefab(GameObject root, EnvBlendables.Target t)
        { var tr = FindByPath(root, t.path); return tr != null ? tr.gameObject : null; }

        // ---- kind = `spawner` / `controller`（2026-10-06 战-A 加）----
        // 原版这两个组件就挂在目标对象上、字段是序列化的；**我们工程里没有任何对应物**
        //（`WarpforgeVFX/Prefabs/*.prefab` grep `AreaSpawner` = 0 命中）⇒ 运行时**就地建一个**、
        // 把旁挂那 6 个字段灌进去（判据与出处 → `Battle/ScenarioBlendables.cs` 末尾那一段）。

        static ParticleSystemAreaSpawner FindSpawnerInPrefab(GameObject root, EnvBlendables.Target t)
        { return MakeSpawner(FindByPath(root, t.path), root, t); }

        static ParticleSystemAreaSpawnerController FindControllerInPrefab(GameObject root, EnvBlendables.Target t)
        { return MakeController(FindByPath(root, t.path), t); }

        static ParticleSystemAreaSpawner MakeSpawner(Transform tr, GameObject prefabRoot, EnvBlendables.Target t)
        {
            if (tr == null) return null;
            var sp = tr.GetComponent<ParticleSystemAreaSpawner>();
            if (sp == null) sp = tr.gameObject.AddComponent<ParticleSystemAreaSpawner>();
            if (sp == null) return null;
            string psPath = t.GetS("particleSystemPrefab");
            var template = FindTemplatePS(tr, prefabRoot, psPath);
            if (template == null)
                Debug.LogWarning($"[Env] spawner 目标 `{t.leaf}` 找不到模板粒子（旁挂记的 `particleSystemPrefab` = "
                               + $"`{psPath}`）—— 这一条建不出粒子（出声，不静默）");
            // 缺字段时用原版 ctor 的默认值兜底（实测那 4 条旁挂**全都有**这 6 个字段）。
            sp.Configure(new Vector3(t.GetF("boxSize.x", 1f), t.GetF("boxSize.y", 1f), t.GetF("boxSize.z", 1f)),
                         template, t.GetI("maxPoolSize", 5), t.GetB("useAutomaticSpawn", true),
                         t.GetF("spawnRate", 1f), t.GetF("chances", 1f));
            return sp;
        }

        /// <summary>模板粒子：原版 `particleSystemPrefab` 是**同一件 prefab 里的一条引用**；实测那 4 条
        /// **全是 spawner 那个 GameObject 的直接子物体**（`…/Psychic_Lightning_down/Lightning Main` ×3、
        /// `…/Explosion Right` ×1）⇒ prefab 侧先按旁挂记的**层级路径**找、找不到再退到「这棵子树里按叶子名找」。
        /// 🔴 **不 Instantiate**：原版那条引用指的就是实例里的那个对象（`OnEnable` 里把它 `SetActive(false)`
        /// 当模板，池子按它 `Instantiate`）—— 我们照做。</summary>
        static ParticleSystem FindTemplatePS(Transform spawnerTr, GameObject prefabRoot, string psPath)
        {
            if (spawnerTr == null) return null;
            if (prefabRoot != null && !string.IsNullOrEmpty(psPath))
            {
                var byPath = FindByPath(prefabRoot, psPath);
                if (byPath != null) { var q = byPath.GetComponent<ParticleSystem>(); if (q != null) return q; }
            }
            int i = psPath != null ? psPath.LastIndexOf('/') : -1;
            string leaf = (i >= 0 && i + 1 < psPath.Length) ? psPath.Substring(i + 1) : psPath;
            if (string.IsNullOrEmpty(leaf)) return null;
            var d = FindDescendant(spawnerTr, leaf);
            return d != null ? d.GetComponent<ParticleSystem>() : null;
        }

        static Transform FindDescendant(Transform t, string name)
        {
            if (Norm(t.name) == Norm(name)) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var r = FindDescendant(t.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        /// <summary>⚠️ 只灌 `spawnRate` / `startOnEnable` 两个标量 —— 这个类真正的数据是
        /// `particleSystemAreaSpawners[]`（每条 = 引用 + 权重 + chances），**旁挂里没有这一层**
        /// （实测 0 个实例用到 controller ⇒ 没为它建数据；要收的话见报告「没查清/待办」那一节）。</summary>
        static ParticleSystemAreaSpawnerController MakeController(Transform tr, EnvBlendables.Target t)
        {
            if (tr == null) return null;
            var ct = tr.GetComponent<ParticleSystemAreaSpawnerController>();
            if (ct == null) ct = tr.gameObject.AddComponent<ParticleSystemAreaSpawnerController>();
            if (ct == null) return null;
            ct.spawnRate = t.GetF("spawnRate", 1f);
            ct.startOnEnable = t.GetB("startOnEnable", true);
            return ct;
        }

        // ---- 场景侧的对象解析（名字 + 最近位置）----
        static Transform FindNearest(Transform root, string leaf, float[] pos)
        {
            if (string.IsNullOrEmpty(leaf)) return null;
            string want = Norm(leaf);
            var all = new List<Transform>();
            if (root != null) Collect(root, want, all);
            else
                foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                    if (Norm(t.name) == want) all.Add(t);
            if (all.Count == 0) return null;
            if (all.Count == 1 || pos == null || pos.Length < 3) return all[0];
            Transform best = all[0]; float bd = float.MaxValue;
            for (int i = 0; i < all.Count; i++)
            {
                var p = all[i].position;
                float d = (p.x - pos[0]) * (p.x - pos[0]) + (p.y - pos[1]) * (p.y - pos[1]) + (p.z - pos[2]) * (p.z - pos[2]);
                if (d < bd) { bd = d; best = all[i]; }
            }
            return best;
        }

        static void Collect(Transform t, string want, List<Transform> outL)
        {
            if (Norm(t.name) == want) outL.Add(t);
            for (int i = 0; i < t.childCount; i++) Collect(t.GetChild(i), want, outL);
        }

        static ParticleSystem FindPSInScene(Transform root, EnvBlendables.Target t)
        { var tr = FindNearest(root, t.leaf, t.pos); return tr != null ? tr.GetComponent<ParticleSystem>() : null; }
        static Renderer FindRendererInScene(Transform root, EnvBlendables.Target t)
        { var tr = FindNearest(root, t.leaf, t.pos); return tr != null ? tr.GetComponent<Renderer>() : null; }
        static GameObject FindGoInScene(Transform root, EnvBlendables.Target t)
        { var tr = FindNearest(root, t.leaf, t.pos); return tr != null ? tr.gameObject : null; }

        // 场景侧那两个（我们数据里 0 条 kind=spawner/controller 是场景侧的，留着是为了**与 prefab 侧同一套**：
        // 旁挂真收了就有地方接）。模板粒子这里没有 prefab 根 ⇒ 只走「这棵子树里按叶子名找」。
        static ParticleSystemAreaSpawner FindSpawnerInScene(Transform root, EnvBlendables.Target t)
        { return MakeSpawner(FindNearest(root, t.leaf, t.pos), null, t); }

        static ParticleSystemAreaSpawnerController FindControllerInScene(Transform root, EnvBlendables.Target t)
        { return MakeController(FindNearest(root, t.leaf, t.pos), t); }

        /// <summary>场景侧一条旁挂要挂在哪个对象上 —— 原版的宿主是 `Scenario/Particles` 这种**分组节点**，
        /// 而我们的战场是平铺的（那些节点不在清单里）⇒ **改挂到它的第一个目标对象上**
        /// （组件只是个「驱动器」，挂哪儿不影响它管谁）。</summary>
        static GameObject PickSceneHost(Transform root, EnvBlendables.Item it, out int miss)
        {
            miss = 0;
            if (it.targets != null)
                foreach (var t in it.targets)
                {
                    if (t == null) continue;
                    var tr = FindNearest(root, t.leaf, t.pos);
                    if (tr != null) return tr.gameObject;
                }
            return null;
        }

        /// <summary>prefab 侧一条旁挂的宿主：原版宿主多半就是 prefab 根；找不到就退到第一个目标。</summary>
        static GameObject PickHost(GameObject root, EnvBlendables.Item it)
        {
            var tr = FindByPath(root, it.owner);
            if (tr != null) return tr.gameObject;
            if (it.targets != null)
                foreach (var t in it.targets)
                {
                    if (t == null) continue;
                    var tt = FindByPath(root, t.path);
                    if (tt != null) return tt.gameObject;
                }
            return root;
        }

        void BeginFadeOut(GameObject go, string so, ScenarioBlendable[] mine)
        {
            var f = new FadeOut { go = go, so = so, timeout = BlendDuration, mine = mine };
            f.pending = 0;
            if (mine != null)
                foreach (var b in mine)
                {
                    if (b == null) continue;
                    // ⚠️ **只有会回调的那些参与计数** —— 原版把 `blendables.Length` 当计数，
                    //    而 `ScenarioParticleSystemToggler` **从不回调** ⇒ 原版那个计数永远归不了零、
                    //    旧实例**根本不会被销毁**（视觉上等价：粒子已经关掉了，只是对象留着）。
                    //    我们照「哪些会回调」计数（见 `ScenarioBlendable.NotifiesComplete`）。
                    bool counts = b.NotifiesComplete;
                    if (counts) f.pending++;
                    b.DoScenarioBlend(new ScenarioBlendOptions
                    {
                        duration = BlendDuration, direction = false, targetValue = 0f,
                        onComplete = counts ? (Action)(() => { f.pending--; }) : null,
                    });
                }
            if (f.pending == 0) { FinishFadeOut(f, false); return; }     // 没有会回调的 ⇒ 立刻销毁（原版会挂着）
            _fading.Add(f);
        }

        void FinishFadeOut(FadeOut f, bool timedOut)
        {
            if (f.go != null)
            {
                if (Application.isPlaying) Destroy(f.go); else DestroyImmediate(f.go);
            }
            if (timedOut)
                Debug.LogWarning($"[Env] 旧环境实例 `{f.so}` 的 blendable 在 {f.timeout:0.##}s 内没全部回调"
                               + $"（还剩 {f.pending} 个）—— **按兜底超时销毁**（出声，别当正常）");
        }

        void Push()
        {
            RenderSettings.fogColor = Color.Lerp(_fog0, _fog1, BlendT);
            RenderSettings.fogDensity = Mathf.Lerp(_den0, _den1, BlendT);
            RenderSettings.ambientLight = Color.Lerp(_amb0, _amb1, BlendT);
            Shader.SetGlobalFloat("_AmbientColorBlend", Mathf.Lerp(_blend0, _blend1, BlendT));
        }

        static Color ToColor(float[] a, Color fallback)
            => (a != null && a.Length >= 3) ? new Color(a[0], a[1], a[2], a.Length >= 4 ? a[3] : 1f) : fallback;

        /// <summary>按名字取环境 prefab —— **走工程既有的效果库**（`WarpforgeEffectLibrary` 是 `Resources` 下的
        /// ScriptableObject、持有那批 prefab 的直接引用；原版 43 件都在里面）。
        /// ⚠️ 找不到时**出声**（调用处报）。</summary>
        static GameObject FindPrefab(string name)
        {
            var lib = WarpforgeVFX.WarpforgeEffectLibrary.Instance;
            if (lib != null && lib.TryGet(name, out var e) && e != null && e.prefab != null)
                return e.prefab;
            return null;
        }
    }
}
