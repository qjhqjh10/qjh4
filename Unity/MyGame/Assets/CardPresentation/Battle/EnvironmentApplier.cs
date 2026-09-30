// EnvironmentApplier.cs — **战场环境切换的执行器**（§三 第 30 条 · 4 环境的战场侧）
//
// 复刻原版那条链（判据 → `资料/加时与冲突模式_原版规格.md` 的「进攻卡生效时会整场换掉战场环境」那一节）：
//   `ScenarioEnvironmentConditionsManager.ApplyEnvironment(so, instant)`
//     · `CurrentEnvironment == so` ⇒ **直接 return（不重播）**
//     · 否则 `DisableEnvironment(旧)` → `SO.EnableEnvironment(instant)`：
//         ① `ApplyAmbientColor`：`RenderSettings.ambientLight = ambientColor`
//            ＋ **补间** `Shader.SetGlobalFloat("_AmbientColorBlend", ambientBlend)`，时长 `SO.blendTime`
//         ② `ApplyFog`：**补间** `RenderSettings.fogColor / fogDensity`（`ToggleFog` 只切 `fog` 开关）
//         ③ `scenarioObjects` 有值 ⇒ `Object.Instantiate(prefab, Camera.main.position/rotation)`
//     · `Awake` 里先按 **Default** 走一遍（instant:true）
//
// ⚠️ **两条如实记着**：
//   ① 原版在实例化之后还会遍历 `IScenarioEnvironmentBlendeable` **按 filter 分组做混合**
//      （`ScenarioParticleSystemToggler` / `ScenarioMaterialFader` / `ScenarioAnimationBlend` /
//      `FlareScenarioToggler` …）—— **那族组件我们一个都没复刻** ⇒ 现在只有「整份 prefab 一起出现/消失」。
//      那批 prefab 里的粒子是 `playOnAwake: 1`、且挂着**我们自己的** `WarpforgeEffectBinder` ⇒ 实例化即播。
//   ② 原版 `blendTime` 的补间是 DOTween；这里用**手推的线性插值**（批处理下没有帧循环 ⇒
//      必须能 `Advance(dt)` 手动推，自检才验得了）。
using UnityEngine;

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

        void Awake()
        {
            // 原版 `Awake` 里先按 Default 走一遍（instant）——但我们**没有**「本场 Default 是哪条」的运行时来源
            // （那是建场期写进 `ArenaEnvGlobal` / `RenderSettings` 的）⇒ **这里不动**，
            // 免得把 §28 那批已验收的逐场雾/环境光值覆盖掉。第一次 `Apply()` 才接管。
        }

        void Update()
        {
            if (BlendT < 1f) Advance(Time.deltaTime);
        }

        /// <summary>要不要**接管**整场的雾/环境光（`Apply` 之后为 true）。</summary>
        public bool Applied { get; private set; }

        /// <summary>应用一条环境。`instant` = 直接到位（原版 `EnableEnvironment(instant)`）。
        /// 同一条 SO **不重播**（原版那条判据）。传 null / 空 SO = **撤掉当前环境**（回到建场时那套）。</summary>
        public EnvironmentApplier Apply(EnvironmentConditions.Item it, bool instant)
        {
            string want = it != null ? it.so : "";
            if (Applied && CurrentSO == want) return this;      // 同一条不重播
            CurrentSO = want;

            // ---- ③ 物件：先撤旧的，再起新的（原版 `DisableEnvironment` → `Instantiate`）----
            if (CurrentInstance != null) { Destroy(CurrentInstance); CurrentInstance = null; }
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
            return this;
        }

        /// <summary>手动推进一步（**批处理/自检唯一的路** —— 那里没有帧循环）。</summary>
        public EnvironmentApplier Advance(float dt)
        {
            if (BlendT >= 1f) return this;
            Elapsed += dt;
            BlendT = Mathf.Clamp01(BlendDuration <= 0f ? 1f : Elapsed / BlendDuration);
            Push();
            return this;
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
