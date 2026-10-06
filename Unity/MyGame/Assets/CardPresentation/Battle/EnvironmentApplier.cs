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
//   🆕 2026-10-06 战-A：`ScenarioParticleSpawnerBlender`（4 个实例）**已经补上了**。
//   🆕 2026-10-07 波9离线（A136）：原版 `IScenarioEnvironmentBlendeable` 的**另 4 个实现类**
//      （`FlareScenarioToggler` 6 实例 · `ScenarioAnimationBlend` 2 · `ScenarioGenericMaterialBlend` 1 ·
//       `TauCannonAnimationStopper` 2，**共 11 个、全在场景侧**）也已进旁挂与本文件的解析器
//      ⇒ **9 个实现类全在**（合计 139 条）。⚠️ 其中两类的**资产**还有缺口（clip / `Animation` 组件 /
//      **宿主对象**：`Railgun Turret 1/2` · `Railgun turret` · `Railgun Turret N Target`）—— 逐条见
//      `资料/普查产出_1007/波9离线_A136_A135.md` 与旁挂的 `_missingTargets`，运行时那几处**出声**。
//      🆕 🔴 **2026-10-11（A196）更正**：原来这一格还把 `LookAtConstrainWIP` / `AnimFXController` 两个**类**
//      列在缺口里 —— **那两个类已经移植进工程了**（`Battle/LookAtConstrainWIP.cs` · `Battle/AnimFXController.cs`，
//      工厂里由 `MakeLookAtConstrains` / `MakeAnimFx` 就地建）。🆕 **2026-10-11（A191）宿主对象也补上了** ——
//      最后那一格（`Railgun Turret 1/2` · `Railgun turret` · `Railgun Turret N Target` · `Battle Arena Dark Angels baked`）
//      由 `ArenaBuilder.ApplyGroupNodes` 照原版建场（旁挂 `arenas/<场>/<场>_groups.json`）⇒ 这一族**不再有缺口**。
//   🆕 2026-10-07（A135）：`ScenarioBlendOptions.filterOptions` 三个构造点（实例侧 / 场景侧 / 撤环境）
//      **都灌了值**（`FilterOf`），`ScenarioGenericMaterialBlend` 已按反编译落地。
//   🆕 2026-10-07 波9批二（A137）：**旁挂的 `standalone` 一节**（不被任何 blendable 引用的
//      `ParticleSystemAreaSpawner` / `…Controller`，全库 **24 + 3** 条）也接上了 —— 见 `BuildStandaloneSpawners`。
//      此前**一条都没建**：原版这一族**自己 `OnEnable` 起循环**（`useAutomaticSpawn = 1`，16/24 条）
//      或**被某个 controller 驱动**（另 8 条 `= 0`；controller 直接调它们的 `SpawnParticle()`），
//      **不经过任何 blendable** ⇒ 43 件环境 prefab 里很多件本来就有常驻随机粒子，我们一颗都没有。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;      // 🆕 2026-10-07：`LensFlareComponentSRP`（`FlareScenarioToggler` 的目标）
using UnityEngine.SceneManagement;

namespace CardPresentation
{
    public class EnvironmentApplier : MonoBehaviour
    {
        /// <summary>当前生效的那条 SO 名（原版 `CurrentEnvironment`）。空 = 还没应用过。</summary>
        public string CurrentSO { get; private set; }

        /// <summary>🆕 2026-10-07（A136）：当前生效的那条 SO 的**整条数据** ——
        /// 原版 `ScenarioEnvironmentConditionsManager.CurrentEnvironment`(+0x28) 的对应物。
        /// `ScenarioAnimationBlend.DoScenarioBlend` 会去读它（要 `animationsToChange[]`）。
        /// 🔴 **赋值时机不能提前**：原版是 `DisableEnvironment(旧)` **之后**才 `CurrentEnvironment = 新`
        ///   （`ScenarioEnvironmentConditionsManager__ApplyEnvironment.c`）⇒ 撤环境那一程里它读到的还是**旧 SO**。
        ///   本类 `Apply()` 里那句 `CurrentItem = it` 就摆在 `BeginFadeOut(...)` **后面**，别挪。</summary>
        public EnvironmentConditions.Item CurrentItem { get; private set; }

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
        /// <summary>本场用哪个战场（`battlearena3` 这种键）。空 = 从活动场景名推（`Battle_&lt;场>`）。</summary>
        public string ArenaKey;

        /// <summary>**战场自己**那批 blendable（原版 `cacheBlendeables`），第一次 `Apply` 时建一次。</summary>
        readonly List<ScenarioBlendable> _sceneBlendables = new List<ScenarioBlendable>();
        bool _sceneBuilt;

        /// <summary>正在淡出、还没销毁的旧实例（原版 `DisableEnvironment` 的计数回调那套）。</summary>
        class FadeOut
        {
            public GameObject go; public int pending; public float left; public float timeout; public string so;
            public ScenarioBlendable[] mine;      // 这个实例里的那批（执行器推它们）
            // 🆕 2026-10-07（A137）：这个实例里的 **standalone 生成器**也一起推 —— 原版它们只是
            //   `MonoBehaviour`，实例没销毁就一直跑；不推的话「撤环境那一程」它们会停摆（静默）。
            public List<ParticleSystemAreaSpawner> spawners;
            public List<ParticleSystemAreaSpawnerController> controllers;
        }
        readonly List<FadeOut> _fading = new List<FadeOut>();

        /// <summary>当前实例里挂上的那批（执行器推它们）。</summary>
        ScenarioBlendable[] _currentBlendables;

        // 🆕 2026-10-07（A137）：当前实例里那批 **standalone 生成器**（见 `BuildStandaloneSpawners`）
        List<ParticleSystemAreaSpawner> _curSpawners = new List<ParticleSystemAreaSpawner>();
        List<ParticleSystemAreaSpawnerController> _curControllers = new List<ParticleSystemAreaSpawnerController>();

        /// <summary>自检用：场景侧挂上了几个 blendable / 实例侧几个 / 还有几个旧实例在淡出。</summary>
        public int SceneBlendableCount { get { return _sceneBlendables.Count; } }
        public int InstanceBlendableCount { get; private set; }
        public int FadingCount { get { return _fading.Count; } }
        /// <summary>🆕 2026-10-07（A137）：**当前实例**上建出来的 standalone spawner / controller 个数。</summary>
        public int InstanceSpawnerCount { get; private set; }
        public int InstanceControllerCount { get; private set; }
        /// <summary>🆕 2026-10-07（A137）：上一次建 standalone 时**没建出来**的条数 + 逐条名字
        /// （出声用：不许静默少建）。</summary>
        public int StandaloneMissedCount { get; private set; }
        public string StandaloneMissed { get; private set; }

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
                BeginFadeOut(CurrentInstance, oldSo, _currentBlendables, _curSpawners, _curControllers);
                CurrentInstance = null; InstanceBlendableCount = 0; _currentBlendables = null;
                InstanceSpawnerCount = 0; InstanceControllerCount = 0;      // 🆕 A137
                _curSpawners = new List<ParticleSystemAreaSpawner>();
                _curControllers = new List<ParticleSystemAreaSpawnerController>();
            }
            // 🔴 **顺序不能挪**：原版 `ApplyEnvironment` 是「DisableEnvironment(旧) → CurrentEnvironment = 新」
            //   ⇒ 撤环境那一程里 `ScenarioAnimationBlend` 读到的还是**旧 SO**（判据见 `CurrentItem` 的注释）。
            CurrentItem = it;
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
                    _currentBlendables = AttachBlendables(CurrentInstance, it.prefabName, it);
                    InstanceBlendableCount = _currentBlendables.Length;
                    // 🆕 2026-10-07（A137）: **同一棵树里那批 standalone 生成器**（它们不是 blendable，
                    //   旁挂单开了一节）—— 原版它们在 prefab 实例化时就自己 `OnEnable` 起循环 / 被 controller 驱动。
                    BuildStandaloneSpawners(CurrentInstance, it.prefabName);
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
                // 🆕 2026-10-07（A135）：原版 `EnableEnvironment` 把它建的**第二份** options 也带上 `SO.filterOptions`
                //   （`…__EnableEnvironment.c`：`uVar9 = *(param_1 + 0x58)` 灌进 `lVar8`），再由
                //   `SetRegisteredBlendeablesState` 原样转给场景里已登记的那批 ⇒ 这里照做。
                filterOptions = FilterOf(it),
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
            // 🆕 2026-10-07（A137）：当前实例里那批 standalone 生成器（**没有别的驱动者** —— 原版靠帧循环）
            AdvanceSpawners(_curSpawners, _curControllers, dt);

            for (int i = _fading.Count - 1; i >= 0; i--)
            {
                var f = _fading[i];
                if (f.go == null) { _fading.RemoveAt(i); continue; }
                f.left += dt;
                if (f.mine != null)
                    for (int k = 0; k < f.mine.Length; k++)
                        if (f.mine[k] != null) f.mine[k].Advance(dt);
                AdvanceSpawners(f.spawners, f.controllers, dt);            // 🆕 A137
                // 原版靠**每个 blendable 的 OnComplete** 计数归零才 Destroy；
                // 这里再加一道**兜底超时**（`blendTime + 1s`）—— 有组件永远不回调时**出声**，不静默挂着。
                if (f.pending <= 0) { FinishFadeOut(f, false); _fading.RemoveAt(i); }
                else if (f.left > Mathf.Max(1f, f.timeout)) { FinishFadeOut(f, true); _fading.RemoveAt(i); }
            }
        }

        /// <summary>🆕 2026-10-07（A137）：推这批 standalone 生成器。
        /// 🔴 **顺序无所谓**（controller 只是「加权选一个 spawner、调它 `SpawnParticle()`」，
        ///   不读 spawner 的 `_t`）—— 这里与 `BuildStandaloneSpawners` 一样先 spawner 后 controller，
        ///   只为把 `Advance` 的调用点集中一处（判据只留一处）。</summary>
        static void AdvanceSpawners(List<ParticleSystemAreaSpawner> sp,
                                   List<ParticleSystemAreaSpawnerController> ct, float dt)
        {
            if (sp != null)
                for (int i = 0; i < sp.Count; i++) if (sp[i] != null) sp[i].Advance(dt);
            if (ct != null)
                for (int i = 0; i < ct.Count; i++) if (ct[i] != null) ct[i].Advance(dt);
        }

        // ---------------- blendable 两半 ----------------

        /// <summary>把旁挂里那几个 blendable 组件挂到 prefab 实例上，并立刻按原版 `lVar7`（direction=true）驱动一次。
        /// prefab 侧的目标按**层级路径**在实例里找（旁挂的 path 从 prefab 根开始 ⇒ 去掉第一段）。</summary>
        ScenarioBlendable[] AttachBlendables(GameObject host, string prefabName, EnvironmentConditions.Item so)
        {
            var items = EnvBlendables.ForPrefab(prefabName);
            var made = new List<ScenarioBlendable>(items.Length);
            var res = new PrefabResolver(host);
            foreach (var it in items)
            {
                var c = ScenarioBlendableFactory.Create(it, PickHost(host, it), res, true);
                if (c == null) continue;
                c.manager = this;          // 🆕 原版这个类自己去问管理器；我们的「管理器」= 执行器
                c.DoScenarioBlend(new ScenarioBlendOptions
                {
                    duration = BlendDuration, direction = true, targetValue = 1f, onComplete = null,
                    // 🆕 2026-10-07（A135）：原版 `EnableEnvironment` 那份 `lVar7` 也带 `SO.filterOptions`
                    filterOptions = FilterOf(so),
                });
                made.Add(c);
            }
            return made.ToArray();
        }

        /// <summary>🆕 2026-10-07（A135）：`SO.filterOptions` → 我们那份（`ScenarioBlendOptions.filterOptions`）。
        /// **只在这里造**（三个构造点共用一处判据）；`it == null` ⇒ 返回 null（原版那种情况本来就会抛 NRE，
        /// 由消费方自己出声 —— 见 `ScenarioGenericMaterialBlend.DoScenarioBlend`）。</summary>
        static ScenarioBlendOptions.BlendFilterOptions FilterOf(EnvironmentConditions.Item it)
        {
            if (it == null) return null;
            return new ScenarioBlendOptions.BlendFilterOptions
            {
                filterCode = it.filterCode ?? "",
                isEnabled = it.filterEnabled != 0,
            };
        }

        // ---------------- 🆕 2026-10-07（A137）：standalone 那批生成器 ----------------
        //
        //  什么是「standalone」：`ParticleSystemAreaSpawner` / `…Controller` 这一族**并不是** blendable
        //  （`IScenarioEnvironmentBlendeable` 9 个实现类里没有它们）—— 它们自己 `OnEnable` 起循环，
        //  或者被一个 `…Controller` 加权随机地驱动。原版全库 **28 + 3** 个实例，被
        //  `ScenarioParticleSpawnerBlender` 引用的只有 **4 个**（2026-10-06 战-A 接的那批），
        //  其余 **24 + 3** 此前**连账都没进**（旁挂里没有 ⇒ 运行时连一句警告都不会打）。
        //  收法 → `工具/gen_env_blendables.py` 的 `collect_standalone`（旁挂 `standalone` 一节）。
        //  🔴 数据形状与 `prefabs` **完全一样**（`EnvBlendables.Group/Item/Target`）——
        //     每个条目只有一个 target = **组件自己那个 GameObject**，`kind` 是 `spawner` / `controller`。

        /// <summary>`EnvBlendables.json` 的 `standalone` 一节。
        /// 🔴 **为什么要在这里读第二遍**：那些条目**不是 blendable**，混进 `EnvBlendables.ForPrefab`
        /// 会让 `ScenarioBlendableFactory.Create` 对它们打「不在工厂的表里」的警告（假警报）；
        /// 而 `Core/EnvBlendables.cs`（`File` 那个类型）不在本件白名单里
        /// ⇒ 在这里只解析这一节，**复用同一套 `EnvBlendables.*` 类型**（不另定义一份 DTO —— 铁律 6）。</summary>
        [Serializable] class StandaloneFile { public EnvBlendables.Group[] standalone; }

        static EnvBlendables.Group[] _standalone;
        static bool _standaloneTried;

        static EnvBlendables.Group[] StandaloneGroups()
        {
            if (_standaloneTried) return _standalone;
            _standaloneTried = true;
            var ta = Resources.Load<TextAsset>("EnvBlendables");
            if (ta == null)
            {
                Debug.LogError("[Env] 取不到 `Resources/EnvBlendables.json` ⇒ 环境 prefab 里那批"
                             + "**常驻生成器**（`ParticleSystemAreaSpawner` / `…Controller`）一颗都建不出来。"
                             + "跑一次 `python 工具/gen_env_blendables.py`。");
                return null;
            }
            var f = JsonUtility.FromJson<StandaloneFile>(ta.text);
            _standalone = (f != null) ? f.standalone : null;
            if (_standalone == null)
                Debug.LogError("[Env] `EnvBlendables.json` 里**没有 `standalone` 一节** —— 环境 prefab 里那批"
                             + "常驻生成器会全部**静默消失**（43 件里很多件本来就有随机粒子）。"
                             + "重跑 `python 工具/gen_env_blendables.py`。");
            return _standalone;
        }

        /// <summary>🆕 2026-10-07（A137）：自检用 —— 旁挂 `standalone` 一节的**总条数**
        /// （`-1` = 那一节没读到 / 解析失败，出错了会在 `StandaloneGroups()` 里报）。
        /// 原版直读实测 = **27**（24 个 `ParticleSystemAreaSpawner` + 3 个 `…Controller`）。</summary>
        public static int StandaloneDataCount()
        {
            var g = StandaloneGroups();
            if (g == null) return -1;
            int n = 0;
            for (int i = 0; i < g.Length; i++)
                if (g[i] != null && g[i].items != null) n += g[i].items.Length;
            return n;
        }

        /// <summary>把这条旁挂条目要挂的那个 GameObject 在**实例**里找出来。
        /// 先按旁挂记的**层级路径**（我们导入的 prefab 保层级），找不到退「这棵子树里按叶子名找」
        /// （与 `FindTemplatePS` 同一条兜底理由）。找不到返回 null（调用方点名，不静默）。</summary>
        static Transform FindStandaloneHost(GameObject root, EnvBlendables.Item it, EnvBlendables.Target t)
        {
            string path = (t != null && !string.IsNullOrEmpty(t.path)) ? t.path : it.owner;
            var tr = FindByPath(root, path);
            if (tr != null) return tr;
            string leaf = (t != null && !string.IsNullOrEmpty(t.leaf)) ? t.leaf : it.ownerLeaf;
            return FindDescendant(root.transform, leaf);
        }

        /// <summary>旁挂那一条的**唯一** target（就是组件自己那个 GameObject）。</summary>
        static EnvBlendables.Target OwnTarget(EnvBlendables.Item it)
        {
            if (it.targets == null) return null;
            for (int i = 0; i < it.targets.Length; i++)
                if (it.targets[i] != null) return it.targets[i];
            return null;
        }

        /// <summary>🆕 2026-10-07（A137）：按旁挂的 `standalone` 一节，把环境 prefab 实例里那批
        /// **常驻生成器**建出来（原版它们在 prefab 实例化时自己 `OnEnable` ⇒ **不需要任何 blendable**）。
        /// 🔴 **顺序：先 spawner 后 controller** —— controller 的 `particleSystemAreaSpawners[]`
        ///    引用的是前者刚建出来的**组件**（旁挂里记的是路径，`MakeController` 按路径回头取）。
        /// 🔴 **不许静默**：建不出来的逐条点名进 `StandaloneMissed`（自检断言直接读它）。</summary>
        void BuildStandaloneSpawners(GameObject host, string prefabName)
        {
            InstanceSpawnerCount = 0; InstanceControllerCount = 0;
            StandaloneMissedCount = 0; StandaloneMissed = "";
            var groups = StandaloneGroups();
            if (groups == null) return;
            EnvBlendables.Item[] items = null;
            for (int i = 0; i < groups.Length; i++)
                if (groups[i] != null && groups[i].root == prefabName) { items = groups[i].items; break; }
            if (items == null || items.Length == 0) return;   // 这件 prefab 本来就没有这一族（多数件如此）

            var missed = new List<string>();
            for (int i = 0; i < items.Length; i++)
            {
                var it = items[i];
                if (it == null || it.cls != "ParticleSystemAreaSpawner") continue;
                var t = OwnTarget(it);
                if (t == null) { missed.Add($"{it.cls}(`{it.ownerLeaf}`):旁挂里没有 target"); continue; }
                var tr = FindStandaloneHost(host, it, t);
                if (tr == null) { missed.Add($"{it.cls}(`{it.ownerLeaf}`)@{it.owner}"); continue; }
                var sp = MakeSpawner(tr, host, t);
                if (sp == null) { missed.Add($"{it.cls}(`{it.ownerLeaf}`):建不出组件"); continue; }
                _curSpawners.Add(sp);
                InstanceSpawnerCount++;
            }
            for (int i = 0; i < items.Length; i++)
            {
                var it = items[i];
                if (it == null || it.cls != "ParticleSystemAreaSpawnerController") continue;
                var t = OwnTarget(it);
                if (t == null) { missed.Add($"{it.cls}(`{it.ownerLeaf}`):旁挂里没有 target"); continue; }
                var tr = FindStandaloneHost(host, it, t);
                if (tr == null) { missed.Add($"{it.cls}(`{it.ownerLeaf}`)@{it.owner}"); continue; }
                var ct = MakeController(tr, host, t);
                if (ct == null) { missed.Add($"{it.cls}(`{it.ownerLeaf}`):建不出组件"); continue; }
                _curControllers.Add(ct);
                InstanceControllerCount++;
            }
            StandaloneMissedCount = missed.Count;
            StandaloneMissed = string.Join("、", missed);
            Debug.Log($"[Env] `{prefabName}` 的常驻生成器：旁挂 {items.Length} 条 → 建出 "
                    + $"spawner {InstanceSpawnerCount} + controller {InstanceControllerCount}"
                    + (missed.Count > 0
                       ? $"；**{missed.Count} 条没建出来**（出声，不静默）：{StandaloneMissed}" : ""));
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
            var res = new SceneResolver(root);
            int made = 0;
            var missed = new List<string>();
            foreach (var it in items)
            {
                if (it == null) continue;
                var host = PickSceneHost(root, it);
                if (host == null)
                {
                    // 🔴 **出声要点名**（原来只数个数）：这 11 个新实例里有一条
                    // （darkangels 的第二颗 `ScenarioAnimationBlend`）**唯一的目标**就是
                    // `Battle Arena Dark Angels baked` 那个分组节点 —— 平铺时代我们没有它 ⇒ 整条挂不上。
                    // 🆕 **2026-10-11（A191）那个分组节点已经照原版建出来了**（`ArenaBuilder.ApplyGroupNodes`）
                    //    ⇒ 这一条**正常情况下不该再出现**；真出现就是那件 arena prefab 没重建。
                    missed.Add($"{it.cls}(owner=`{it.ownerLeaf}`)");
                    continue;
                }
                var c = ScenarioBlendableFactory.Create(it, host, res, true);
                if (c == null) continue;
                c.manager = this;      // 🆕 原版这个类自己去问管理器；我们的「管理器」= 执行器
                _sceneBlendables.Add(c); made++;
            }
            Debug.Log($"[Env] 场景侧 blendable：旁挂 {items.Length} 条 → 挂上 {made} 个"
                    + (missed.Count > 0
                       ? $"；**{missed.Count} 条找不到宿主对象**（出声，不猜）：{string.Join("、", missed)}"
                       : ""));
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
        /// 自检里找战场对象用的也是它）；退一步找 `Warpforge_&lt;场>`。都找不到 ⇒ 返回 null = 全场景搜（并出声）。</summary>
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

        /// <summary>比名字之前的归一化：**Trim**。🔴 原版真有带**尾随空格**的名字
        /// （`'Railgun Turret 1 Target '` · `'Flames '`），不 Trim 会**静默比不上**。
        /// 🆕 2026-10-11（A191）：**从 `private` 放开成 `public`** —— 建场侧（`ArenaBuilder.FindBuilt`）
        /// 也要按「归一化名字 + 最近位置」回头找对象，那份判据**只留这一处**（本仓铁律 6：
        /// 同一条规则别写两遍；同一句在 python 侧还有一份 `工具/a210_a211_gap.py` 的 `norm`，
        /// 那一份多一件事：**去单引号/双引号**（Unity 导出带尾随空格的名字时会写成 `m_Name: 'X '`）。</summary>
        public static string Norm(string s) { return s == null ? "" : s.Trim(); }

        static ParticleSystem FindPSInPrefab(GameObject root, EnvBlendables.Target t)
        { var tr = FindByPath(root, t.path); return tr != null ? tr.GetComponent<ParticleSystem>() : null; }
        static Renderer FindRendererInPrefab(GameObject root, EnvBlendables.Target t)
        { return RendererAt(FindByPath(root, t.path), t, root != null ? root.name : "?"); }
        static GameObject FindGoInPrefab(GameObject root, EnvBlendables.Target t)
        { var tr = FindByPath(root, t.path); return tr != null ? tr.gameObject : null; }

        /// <summary>在某个对象上找渲染器。🔴 **2026-10-07：必须退一步找 `mesh` 子件** ——
        /// `ArenaBuilder` 建网格时是「**holder（名字 = 清单的 `go`）+ 名为 `mesh` 的子件**（MeshFilter/MeshRenderer 在子件上）」
        /// （`ArenaBuilder.cs:1845` 的 `new GameObject(goName)` 与 `:1926` 的 `new GameObject("mesh")`）⇒
        /// 直接 `GetComponent&lt;Renderer>()` 拿到的是 **null**（粒子那种才是挂在对象自己身上：`ParticleSystemRenderer`
        /// 也是 `Renderer`，那条路本来就通）。**修之前**场景侧那 19 条 renderer 目标里凡是网格的一律被静默丢掉
        /// （darkangels 的 `Ship 1..6`、leviathan 的 `Toxic_pool`、arena3 的 `Plane`、GSC 的 `Floor` 都是）。
        /// 找不到就**出声**（本仓红线：不许静默失败），返回 null。</summary>
        static Renderer RendererAt(Transform tr, EnvBlendables.Target t, string where)
        {
            if (tr == null) return null;
            var r = tr.GetComponent<Renderer>();
            if (r != null) return r;
            var mesh = FindChild(tr, "mesh");           // `ArenaBuilder` 给网格子件起的固定名字
            if (mesh != null) { r = mesh.GetComponent<Renderer>(); if (r != null) return r; }
            Debug.LogWarning($"[Env] 目标 `{t.leaf}`（路径 `{t.path}`）上没有渲染器，也没有名为 `mesh` 的子件"
                           + $"（宿主 `{where}`）—— 这一条渲染器目标被跳过（出声，不静默）");
            return null;
        }

        // 🆕 2026-10-07（A136）：另外三种目标 —— `flare` / `animation` / `transform`
        static LensFlareComponentSRP FindFlareInPrefab(GameObject root, EnvBlendables.Target t)
        { var tr = FindByPath(root, t.path); return tr != null ? tr.GetComponent<LensFlareComponentSRP>() : null; }
        static Animation FindAnimationInPrefab(GameObject root, EnvBlendables.Target t)
        { var tr = FindByPath(root, t.path); return tr != null ? tr.GetComponent<Animation>() : null; }
        static Transform FindTransformInPrefab(GameObject root, EnvBlendables.Target t)
        { return FindByPath(root, t.path); }

        // ---- kind = `spawner` / `controller`（2026-10-06 战-A 加）----
        // 原版这两个组件就挂在目标对象上、字段是序列化的；**我们工程里没有任何对应物**
        //（`WarpforgeVFX/Prefabs/*.prefab` grep `AreaSpawner` = 0 命中）⇒ 运行时**就地建一个**、
        // 把旁挂那 6 个字段灌进去（判据与出处 → `Battle/ScenarioBlendables.cs` 末尾那一段）。

        static ParticleSystemAreaSpawner FindSpawnerInPrefab(GameObject root, EnvBlendables.Target t)
        { return MakeSpawner(FindByPath(root, t.path), root, t); }

        static ParticleSystemAreaSpawnerController FindControllerInPrefab(GameObject root, EnvBlendables.Target t)
        { return MakeController(FindByPath(root, t.path), root, t); }

        static ParticleSystemAreaSpawner MakeSpawner(Transform tr, GameObject prefabRoot, EnvBlendables.Target t)
        {
            if (tr == null) return null;
            // 🔴 2026-10-07（A137）：**一律新建，不复用同 object 上已有的**。
            //   原版真的会在**同一个 GameObject 上挂两个** `ParticleSystemAreaSpawner` ——
            //   实测 `EnvironmentalCondition Sororitas Shrine Bombardment/Psychic_Lightning_down`
            //   就有两个（一个被 `ScenarioParticleSpawnerBlender` 引用、一个 standalone，
            //   两条的 `spawnRate`/`chances`/模板都不同）。
            //   写成 `GetComponent() ?? AddComponent()` 的话，后建的那条会 `Configure` 到**先建那个组件**上
            //   ⇒ 先建那条的 6 个字段被**静默覆盖**（看不见、且只在同 object 双组件时现形）。
            //   「同一条 target 只会走一次」由构造保证：blender 那条路每个 target 一次、standalone 每个条目一次。
            var sp = tr.gameObject.AddComponent<ParticleSystemAreaSpawner>();
            if (sp == null) return null;
            string psPath = t.GetS("particleSystemPrefab");
            var template = FindTemplatePS(tr, prefabRoot, psPath);
            if (template == null)
                Debug.LogWarning($"[Env] spawner 目标 `{t.leaf}` 找不到模板粒子（旁挂记的 `particleSystemPrefab` = "
                               + $"`{psPath}`）—— 这一条建不出粒子（出声，不静默）");
            // 缺字段时用原版 ctor 的默认值兜底（实测那几条旁挂**全都有**这 6 个字段）。
            sp.Configure(new Vector3(t.GetF("boxSize.x", 1f), t.GetF("boxSize.y", 1f), t.GetF("boxSize.z", 1f)),
                         template, t.GetI("maxPoolSize", 5), t.GetB("useAutomaticSpawn", true),
                         t.GetF("spawnRate", 1f), t.GetF("chances", 1f));
            return sp;
        }

        /// <summary>模板粒子：原版 `particleSystemPrefab` 是**同一 bundle 里的一条对象引用**。三条路逐级退：
        ///  ① 旁挂记的**层级路径**（相对 prefab 根）—— 实测那 4 条 blender spawner **全是**这种；
        ///  ② 「这棵子树 / 这件 prefab 里按叶子名找」—— 防我们重导时层级有微小出入；
        ///  ③ 🆕 2026-10-07（A137）**工程里另一件独立 prefab**（`WarpforgeEffectLibrary` 按名取）——
        ///     原版那条引用实测有 **10/24** 条落在 **同一个 bundle 里的另一个 prefab 根**上
        ///     （`Orbital 3/4/5 repeat` · `Meteor Angled` · `Rockfall` / `GSC Rockfall` 那种），
        ///     它们**不在**环境 prefab 的子树里 ⇒ 前两条必然落空。走 `MakeTemplateCopy`（见那儿的理由）。
        /// 🔴 **不 Instantiate 原来的第一、二条路**：原版那条引用指的就是实例里的那个对象
        ///  （`OnEnable` 里把它 `SetActive(false)` 当模板，池子按它 `Instantiate`）—— 我们照做。</summary>
        static ParticleSystem FindTemplatePS(Transform spawnerTr, GameObject prefabRoot, string psPath)
        {
            if (spawnerTr == null) return null;
            int i = psPath != null ? psPath.LastIndexOf('/') : -1;
            string leaf = (i >= 0 && i + 1 < psPath.Length) ? psPath.Substring(i + 1) : psPath;
            if (prefabRoot != null && i >= 0)                 // ① 只有在它是**一段路径**时才按路径找
            {                                                 //   （外部模板记的是裸根名，按路径找会原地返回 prefab 根）
                var byPath = FindByPath(prefabRoot, psPath);
                if (byPath != null) { var q = byPath.GetComponent<ParticleSystem>(); if (q != null) return q; }
            }
            if (!string.IsNullOrEmpty(leaf))                  // ② 按叶子名
            {
                var d = FindDescendant(spawnerTr, leaf);
                if (d != null) { var q = d.GetComponent<ParticleSystem>(); if (q != null) return q; }
                if (prefabRoot != null)
                {
                    var d2 = FindDescendant(prefabRoot.transform, leaf);
                    if (d2 != null) { var q2 = d2.GetComponent<ParticleSystem>(); if (q2 != null) return q2; }
                }
                var asset = FindPrefab(Norm(leaf));           // ③ 另一件独立 prefab
                if (asset != null) return MakeTemplateCopy(asset, spawnerTr, Norm(leaf));
            }
            return null;
        }

        /// <summary>🆕 2026-10-07（A137）：外部模板（见 `FindTemplatePS` ③）要在**实例里**有一份。
        /// 🔴 为什么不直接把 `Resources` 取到的那件 prefab 资产当模板：`ParticleSystemAreaSpawner`
        ///   `OnEnable` 会**无条件** `particleSystemPrefab.gameObject.SetActive(false)`（判据 = 反汇编
        ///   `RVA 0x675E10`），而 `Resources.Load` 拿到的是**全进程共享的那份资产**
        ///   ⇒ 直接把共享资产当模板 = **把别的系统也在用的那件 prefab 弄成关闭态**（静默、跨系统）。
        ///   原版没有这个问题：它那条引用是 bundle 里的独立资产、只归这个 spawner 用。
        ///   这里 `Instantiate` 一份**挂在 spawner 下的私有副本**：`SetActive(false)` 只动副本，
        ///   `CreatePooledItem` 的 `Instantiate(prefab, transform)` 与原版等价。
        /// ⚠️ 副本自己**不渲染**（`OnEnableBody` 会关掉它），随环境实例一起销毁。
        /// ⚠️ 有意的偏离：这比原版**多一个场景对象**（原版那份资产不在场景里）。</summary>
        static ParticleSystem MakeTemplateCopy(GameObject asset, Transform parent, string leaf)
        {
            var go = Instantiate(asset, parent);
            if (go == null) return null;
            go.name = "Template_" + leaf;
            var q = go.GetComponent<ParticleSystem>();
            if (q == null)
            {
                var d = FindDescendant(go.transform, leaf);
                if (d != null) q = d.GetComponent<ParticleSystem>();
            }
            if (q == null)
                Debug.LogWarning($"[Env] 外部模板 `{leaf}` 上的 prefab 根/同名子件里没有 `ParticleSystem`"
                               + "（`WarpforgeEffectLibrary` 取到的是这件 prefab）—— 这条建不出粒子（出声）");
            return q;
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

        /// <summary>`ParticleSystemAreaSpawnerController`（TypeDefIndex 1107）—— 一组生成器的**加权随机**调度器。
        /// 🆕 2026-10-07（A137）：**它的主体那一层现在也收了** —— `particleSystemAreaSpawners[]`（每条 =
        /// `{引用, weight, chances}` 的嵌套结构）在旁挂里被摊平成一串带下标的键
        /// （`spawner.&lt;i>` = 落点的层级路径 · `weight.&lt;i>` / `chances.&lt;i>` = 数值；见 gen 脚本的
        /// `CONTROLLER_ARRAY` 那段）。**下标是连着的**：`spawner.i` 与 `weight.i` 一一对应。
        /// 🔴 为什么要收它：全库 3 个 controller 里有 **8 条 spawner** 是 `useAutomaticSpawn = 0`、
        ///    **只**由 controller 驱动（`SpawnParticle()` 直接调，不看那个字段）—— 不收这一层，
        ///    那 8 条永远不出粒子（静默）。
        /// 顺序：`BuildStandaloneSpawners` **先建 spawner 再建 controller** ⇒ 这里按路径回头取到的是
        /// 刚建好的那个组件（不是新建一个）。</summary>
        static ParticleSystemAreaSpawnerController MakeController(Transform tr, GameObject prefabRoot,
                                                                 EnvBlendables.Target t)
        {
            if (tr == null) return null;
            var ct = tr.gameObject.AddComponent<ParticleSystemAreaSpawnerController>();   // 同理：不复用
            if (ct == null) return null;
            ct.spawnRate = t.GetF("spawnRate", 1f);
            ct.startOnEnable = t.GetB("startOnEnable", true);
            var defs = new List<ParticleSystemAreaSpawnerController.ParticleSpawnDefinition>();
            var missing = new List<string>();
            for (int i = 0; i < 64; i++)              // 上界只是防呆（实测最多 3 条）
            {
                bool hasRef = HasField(t, "spawner." + i);
                bool hasW = HasField(t, "weight." + i);
                if (!hasRef && !hasW) break;          // 下标连着 ⇒ 到这儿就是收完了
                string p = t.GetS("spawner." + i);
                var spTr = string.IsNullOrEmpty(p) ? null : FindByPath(prefabRoot, p);
                var sp = spTr != null ? spTr.GetComponent<ParticleSystemAreaSpawner>() : null;
                if (sp == null)
                {
                    missing.Add($"spawner.{i}=`{p}`");  // ⚠️ 不静默；但**下标不能跳**，继续往下读
                    continue;
                }
                defs.Add(new ParticleSystemAreaSpawnerController.ParticleSpawnDefinition
                {
                    particleSystemAreaSpawner = sp,
                    weight = t.GetF("weight." + i, 0f),
                    chances = t.GetF("chances." + i, 1f),
                });
            }
            ct.particleSystemAreaSpawners = defs.ToArray();
            if (missing.Count > 0)
                Debug.LogWarning($"[Env] controller `{t.leaf}` 的 `particleSystemAreaSpawners[]` 有 "
                               + $"{missing.Count} 条引用取不到 spawner 组件：{string.Join("、", missing)}"
                               + " —— 这几条不会生成粒子（出声，不静默）");
            if (defs.Count == 0)
                Debug.LogWarning($"[Env] controller `{t.leaf}` 一条 `particleSystemAreaSpawners` 都没配上"
                               + " —— 它不会生成任何粒子（出声，不静默）");
            return ct;
        }

        /// <summary>旁挂那条 target 的字段里有没有这个键（`GetF` 有默认值、分不出「记了 0」与「没记」）。</summary>
        static bool HasField(EnvBlendables.Target t, string k)
        {
            if (t == null || t.fields == null) return false;
            for (int i = 0; i < t.fields.Length; i++)
                if (t.fields[i] != null && t.fields[i].k == k) return true;
            return false;
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
        { return RendererAt(FindNearest(root, t.leaf, t.pos), t, root != null ? root.name : "?"); }
        static GameObject FindGoInScene(Transform root, EnvBlendables.Target t)
        { var tr = FindNearest(root, t.leaf, t.pos); return tr != null ? tr.gameObject : null; }

        // 🆕 2026-10-07（A136）另外三种（场景侧）
        static LensFlareComponentSRP FindFlareInScene(Transform root, EnvBlendables.Target t)
        { var tr = FindNearest(root, t.leaf, t.pos); return tr != null ? tr.GetComponent<LensFlareComponentSRP>() : null; }
        static Animation FindAnimationInScene(Transform root, EnvBlendables.Target t)
        { var tr = FindNearest(root, t.leaf, t.pos); return tr != null ? tr.GetComponent<Animation>() : null; }
        static Transform FindTransformInScene(Transform root, EnvBlendables.Target t)
        { return FindNearest(root, t.leaf, t.pos); }

        // 场景侧那两个（我们数据里 0 条 kind=spawner/controller 是场景侧的，留着是为了**与 prefab 侧同一套**：
        // 旁挂真收了就有地方接）。模板粒子这里没有 prefab 根 ⇒ 只走「这棵子树里按叶子名找」。
        static ParticleSystemAreaSpawner FindSpawnerInScene(Transform root, EnvBlendables.Target t)
        { return MakeSpawner(FindNearest(root, t.leaf, t.pos), null, t); }

        static ParticleSystemAreaSpawnerController FindControllerInScene(Transform root, EnvBlendables.Target t)
        { return MakeController(FindNearest(root, t.leaf, t.pos), root != null ? root.gameObject : null, t); }

        // ---- 🆕 2026-10-07（A136）：`IEnvTargetResolver` 的两份实现（prefab 侧 / 场景侧）----
        //   为什么要这个接口：目标种类从 5 种涨到 8 种（`flare` / `animation` / `transform` 三种新加），
        //   再往 `ScenarioBlendableFactory.Create` 上摊 lambda 就没人读得动了；而两条路的判据
        //   **本来就不同**（prefab 保层级按路径找 · 我们平铺的战场按名字+最近位置找）⇒ 各包一份、别混。

        /// <summary>prefab 侧：按**层级路径**。`LensFlareComponentSRP` / `Animation` 这两种在**环境 prefab** 里
        /// 一个实例都没有（实测：那一族 9 个类里只有 5 个在 prefab 侧有实例）—— 留着是为了**与场景侧同一套**。</summary>
        class PrefabResolver : IEnvTargetResolver
        {
            readonly GameObject _root;
            public PrefabResolver(GameObject root) { _root = root; }
            public ParticleSystem PsOf(EnvBlendables.Target t) { return FindPSInPrefab(_root, t); }
            public Renderer RendererOf(EnvBlendables.Target t) { return FindRendererInPrefab(_root, t); }
            public GameObject GoOf(EnvBlendables.Target t) { return FindGoInPrefab(_root, t); }
            public ParticleSystemAreaSpawner SpawnerOf(EnvBlendables.Target t) { return FindSpawnerInPrefab(_root, t); }
            public ParticleSystemAreaSpawnerController ControllerOf(EnvBlendables.Target t) { return FindControllerInPrefab(_root, t); }
            public LensFlareComponentSRP FlareOf(EnvBlendables.Target t) { return FindFlareInPrefab(_root, t); }
            public Animation AnimationOf(EnvBlendables.Target t) { return FindAnimationInPrefab(_root, t); }
            public Transform TransformOf(EnvBlendables.Target t) { return FindTransformInPrefab(_root, t); }
        }

        /// <summary>场景侧：**名字 + 最近位置**（我们的战场是 `ArenaBuilder` 按清单平铺建的，没有父链）。
        /// 🔴 `flare` / `animation` / `transform` 这三种**只在这里会真被用到**（11 个新实例**全在场景侧**）。</summary>
        /// <summary>🆕 2026-10-11（A201）：**从 `private` 放开成 `public`** —— 自检要拿**同一个**解析器
        /// 去驱动 `ScenarioBlendableFactory.Create`（A191/A201 那两条断言：宿主在不在、
        /// `clipLoader` 真的被调到没有）。⛔ **不是在 Editor 里另写一份「按名字+最近位置找对象」** ——
        /// 那会把这条判据写第二遍（本仓铁律 6）。它本身是**可reachable 的生产类型**（运行时那一半
        /// 就在用它），放开可见性**不改变任何行为**。</summary>
        public class SceneResolver : IEnvTargetResolver
        {
            readonly Transform _root;
            public SceneResolver(Transform root) { _root = root; }
            public ParticleSystem PsOf(EnvBlendables.Target t) { return FindPSInScene(_root, t); }
            public Renderer RendererOf(EnvBlendables.Target t) { return FindRendererInScene(_root, t); }
            public GameObject GoOf(EnvBlendables.Target t) { return FindGoInScene(_root, t); }
            public ParticleSystemAreaSpawner SpawnerOf(EnvBlendables.Target t) { return FindSpawnerInScene(_root, t); }
            public ParticleSystemAreaSpawnerController ControllerOf(EnvBlendables.Target t) { return FindControllerInScene(_root, t); }
            public LensFlareComponentSRP FlareOf(EnvBlendables.Target t) { return FindFlareInScene(_root, t); }
            public Animation AnimationOf(EnvBlendables.Target t) { return FindAnimationInScene(_root, t); }
            public Transform TransformOf(EnvBlendables.Target t) { return FindTransformInScene(_root, t); }
        }

        /// <summary>场景侧一条旁挂要挂在哪个对象上 —— 原版的宿主是 `Scenario/Particles` 这种**分组节点**，
        /// 而我们的战场是平铺的（那些节点不在清单里）⇒ **改挂到它的第一个目标对象上**
        /// （组件只是个「驱动器」，挂哪儿不影响它管谁）。</summary>
        static GameObject PickSceneHost(Transform root, EnvBlendables.Item it)
        {
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

        void BeginFadeOut(GameObject go, string so, ScenarioBlendable[] mine,
                          List<ParticleSystemAreaSpawner> sp, List<ParticleSystemAreaSpawnerController> ct)
        {
            var f = new FadeOut { go = go, so = so, timeout = BlendDuration, mine = mine,
                                  spawners = sp, controllers = ct };      // 🆕 A137
            f.pending = 0;
            // 🆕 2026-10-07（A135）：原版 `ScenarioEnvironmentConditionSO__DisableEnvironment.c` 建 options 时
            //   第 4 个实参就是 `*(param_1 + 0x58)` = **这条（旧）SO 的 filterOptions**
            //   （`ScenarioBlendOptions___ctor(uVar7, uVar10 /*duration*/, 0 /*direction=false*/, uVar6 /*onComplete*/, uVar1 /*filterOptions*/)`）
            //   ⇒ 撤环境那一程也要灌，否则 `ScenarioGenericMaterialBlend` 会走「FilterOptions == null」那条错分支。
            var filt = FilterOf(EnvironmentConditions.Find(so));
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
                        filterOptions = filt,
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
