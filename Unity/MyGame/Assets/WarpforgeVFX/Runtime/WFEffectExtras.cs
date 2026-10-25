// WFEffectExtras.cs — 特效 prefab 上**非 AnimFX 模块**的那些 MonoBehaviour 的运行时等价物
//
// ============================================================================
//  为什么要有这个文件（2026-10-25 · 完全复刻第 1 轮 · D 域普查结论）
// ============================================================================
//  `WarpforgeEffectPlayer.BuildModules` 只按 `animfx_modules.json` 装配 **AnimFX 模块**；
//  原版特效 prefab 上还挂着**别的** MonoBehaviour，那些**一个都不会被携带** ⇒ 静默丢失。
//
//  普查口径（可复跑）：`D:/2/tools/assetripper_out/ExportedProject` 的 963 件特效 prefab
//  （= `数据/游戏数据/effect_index.json` 的 978 条里的 963 件，15 件在导出集里没有），
//  按 `m_Script.guid` → `.cs.meta` 反查类名，统计**非 AnimFX** 的挂载。现读结果（挂载数 / 涉及 prefab 数）：
//
//    | 类 | 挂载 | prefab | 我们怎么办 |
//    |---|---:|---:|---|
//    | `MaterialFlickerEffect` | 51 | 5 | ✅ 复用 `WFMaterialFlicker`（参数从数据灌；算式已用原版实况验过） |
//    | `ScenarioParticleSystemBlender` | 49 | 39 | ✅ 环境链（`EnvironmentApplier` + `ScenarioBlendables`） |
//    | `SyncParticleSeeds` | 40 | 36 | ✅ `WFExtraSyncParticleSeeds` |
//    | `ShakeAndSoundObject` | 35 | 35 | ✅ `WFExtraShakeAndSound`（屏震 preset + 定时音效；见该类注释） |
//    | `ParticleSystemPoolable` / `ParticleSystemAreaSpawner` / `…Controller` | 30/28/3 | env | ✅ 环境链 |
//    | `ScenarioParticleSystemToggler` / `ScenarioMaterialFader` / `ScenarioParticleSpawnerBlender` / `ScenarioGenericObjectToggler` | 19/15/4/1 | env | ✅ 环境链 |
//    | `SubEmitterManualTrigger` | 12 | 3 | ✅ `WFExtraSubEmitterManualTrigger` |
//    | `ParticleFindFloorCollider` | 9 | 5 | ✅ `WFExtraParticleFloorCollider` |
//    | `DestroyByTime` | 9 | 7 | ✅ `WFExtraDestroyByTime` |
//    | `ShaderProperySetter` | 2 | 2 | ✅ `WFExtraShaderPropertyFloat` |
//    | `ParticleEmitOnEnable` | 2 | 2 | ✅ `WFExtraParticleEmitOnEnable` |
//    | `SpriteFlickerEffect` | 2 | 1 | ✅ `WFExtraSpriteFlicker`（同一套四正弦，写 `SpriteRenderer.color`） |
//    | `InitiMaterialWithRandomValue` | 1 | 1 | ✅ `WFExtraRandomMaterialValue` |
//    | `ParticleForceForceFieldController` | 1 | 1 | ✅ 复用 `WarpforgeParticleForceFieldController` |
//    | 卡 prefab 族（`CardScript` / `BattleCardUI` / `Card3DAnimationController` / `NestedFadeGroup*` / …） | 各 1 | 各 1 | ✅ 卡系统另有实现（不是特效链的活） |
//
//  ⚠️ **数据从哪来**：`Resources/WarpforgeVFX/effect_extras.json`（由本轮普查生成，**只含需要它的 88 件**）。
//     ⛔ 不要去改 `WarpforgeEffectLibrary.asset` —— 那会让「重生成效果库」变成必须跑 Unity 的一步。
//
//  ⚠️ **实现纪律**：每个组件的 `Apply()` 由**播放器**在 `Init` 末尾调一次（不是 Unity 的 `Awake`/`Start`），
//     这样批处理（没有帧循环）里也走同一条路；需要逐帧的走 `IWFEffectExtraTick`（播放器 `Tick` 广播），
//     ⛔ 别在这些组件里写 `Update()`（批处理不跑 ⇒ 两条路行为不同）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>逐帧的额外组件（由 `WarpforgeEffectPlayer.Tick` 广播；见文件头「实现纪律」）。</summary>
    public interface IWFEffectExtraTick { void ExtraTick(float dt); }

    /// <summary>一条「这件特效上还挂着什么」的记录（来自 `effect_extras.json`）。</summary>
    [Serializable]
    public class WFEffectExtra
    {
        /// <summary>原版类名（= 普查得到的类名，逐字）。</summary>
        public string kind;
        /// <summary>挂在哪个节点（相对特效根的名字路径；空 = 根自己）。</summary>
        public string node;
        /// <summary>参数 0（按 kind 解释：`DestroyByTime.lifeTime` / `SubEmitterManualTrigger.triggerOnSecond` / `*Flicker.amplitude` / …）。</summary>
        public float f0;
        /// <summary>参数 1（`InitiMaterialWithRandomValue.maxValue` / `*Flicker.frequency` / …）。</summary>
        public float f1;
        /// <summary>参数 2（`*Flicker.fadeInTime`）。</summary>
        public float f2;
        /// <summary>参数 3（`*Flicker.fadeOutTime`）。</summary>
        public float f3;
        /// <summary>参数 4（`*Flicker.randomStart`，0/1）。</summary>
        public float f4;
        /// <summary>字符串参数（`ShaderProperySetter` 的属性名）。</summary>
        public string s0;
        /// <summary>`ShakeAndSoundObject.onEnableShakes[]` → preset 名（`ShakeAndSoundObject` 用）。</summary>
        public string[] presets;
        /// <summary>`ShakeAndSoundObject.sounds[]`（原版 `PlaySoundOnTime` 条目）。</summary>
        public WFSoundSlot[] sounds;
        public override string ToString() { return kind + "@" + node; }
    }

    /// <summary>原版 `PlaySoundOnTime` 的一条（`time` 起、每 `interval` 秒、共 `total` 次）。</summary>
    [Serializable]
    public class WFSoundSlot
    {
        public string cue;
        public float time;
        public float interval;
        public int total;
        public int is2d;
    }

    [Serializable] internal class WFEffectExtraRec { public string effect; public WFEffectExtra[] extras; }
    [Serializable] internal class WFEffectExtraDoc { public string note; public WFEffectExtraRec[] effects; }

    /// <summary>`effect_extras.json` 的运行时入口 + 六个等价组件（见文件头那张表）。</summary>
    public static class WFEffectExtras
    {
        public const string ResourcesPath = "WarpforgeVFX/effect_extras";
        static Dictionary<string, WFEffectExtra[]> _map;

        /// <summary>读表（懒加载一次）。文件不存在 ⇒ 空表 + 出声一次（不静默）。</summary>
        static void Ensure()
        {
            if (_map != null) return;
            _map = new Dictionary<string, WFEffectExtra[]>(StringComparer.Ordinal);
            var ta = Resources.Load<TextAsset>(ResourcesPath);
            if (ta == null)
            {
                Debug.LogWarning($"[WarpforgeVFX] 找不到 `Resources/{ResourcesPath}.json` ⇒ "
                               + "特效 prefab 上那些**非 AnimFX 模块**的组件（`SyncParticleSeeds` / "
                               + "`DestroyByTime` / `SubEmitterManualTrigger` …）这一趟**一个都不会被补上**。"
                               + "重建这张表的脚本在本类文件头（普查口径）。");
                return;
            }
            var doc = JsonUtility.FromJson<WFEffectExtraDoc>(ta.text);
            if (doc != null && doc.effects != null)
                foreach (var r in doc.effects)
                    if (r != null && !string.IsNullOrEmpty(r.effect))
                        _map[r.effect] = r.extras ?? new WFEffectExtra[0];
        }

        /// <summary>这件特效需要补哪些额外组件（没有就空数组）。</summary>
        public static WFEffectExtra[] For(string effectName)
        {
            Ensure();
            WFEffectExtra[] v;
            return (effectName != null && _map.TryGetValue(effectName, out v)) ? v : Empty;
        }
        static readonly WFEffectExtra[] Empty = new WFEffectExtra[0];

        /// <summary>表里一共几件特效（自检用）。</summary>
        public static int EffectCount { get { Ensure(); return _map.Count; } }
        /// <summary>补上的组件数（累计，自检用）。</summary>
        public static int Attached { get; private set; }
        /// <summary>**已知会丢、但这一版还没实现**的挂载数（累计；每条都会出声一次按 kind 去重）。</summary>
        public static int Unimplemented { get; private set; }
        /// <summary>节点解析失败数（累计；出声）。</summary>
        public static int NodeMisses { get; private set; }
        public static void ResetDiagnostics() { Attached = 0; Unimplemented = 0; NodeMisses = 0; _warnedUnimpl.Clear(); }

        static readonly HashSet<string> _warnedUnimpl = new HashSet<string>();

        /// <summary>给一个刚播起来的播放器补挂「非 AnimFX 模块」的组件。由 `WarpforgeEffectPlayer.Init` 调。</summary>
        public static void AttachAll(WarpforgeEffectPlayer p)
        {
            if (p == null || p.Entry == null) return;
            var list = For(p.Entry.name);
            for (int i = 0; i < list.Length; i++) Attach(p, list[i]);
        }

        static void Attach(WarpforgeEffectPlayer p, WFEffectExtra e)
        {
            if (e == null || string.IsNullOrEmpty(e.kind)) return;
            Transform host = p.transform;
            if (!string.IsNullOrEmpty(e.node) && e.node != p.gameObject.name)
            {
                host = WFEffectModule.ResolvePath(p.transform, e.node, "extras/" + e.kind);
                if (host == null)
                {
                    NodeMisses++;
                    Debug.LogWarning($"[WarpforgeVFX] 《{p.EffectName}》的 `{e.kind}` 要挂在节点 `{e.node}` 上，"
                                   + "但那个节点解析不出来 ⇒ **这个组件没有补上**（宁可认输，不猜）。");
                    return;
                }
            }

            switch (e.kind)
            {
                case "SyncParticleSeeds":
                    host.gameObject.AddComponent<WFExtraSyncParticleSeeds>().Apply();
                    break;
                case "DestroyByTime":
                    var d = host.gameObject.AddComponent<WFExtraDestroyByTime>();
                    d.lifeTime = e.f0; d.Apply(p);
                    break;
                case "SubEmitterManualTrigger":
                    var s = host.gameObject.AddComponent<WFExtraSubEmitterManualTrigger>();
                    s.triggerOnSecond = e.f0; s.Apply();
                    break;
                case "ParticleFindFloorCollider":
                    host.gameObject.AddComponent<WFExtraParticleFloorCollider>().Apply();
                    break;
                case "ParticleEmitOnEnable":
                    host.gameObject.AddComponent<WFExtraParticleEmitOnEnable>().Apply();
                    break;
                case "InitiMaterialWithRandomValue":
                    var r = host.gameObject.AddComponent<WFExtraRandomMaterialValue>();
                    r.minValue = e.f0; r.maxValue = e.f1; r.Apply();
                    break;
                case "ShaderProperySetter":
                    var sh = host.gameObject.AddComponent<WFExtraShaderPropertyFloat>();
                    sh.propertyName = e.s0; sh.value = e.f0; sh.Apply();
                    break;
                // ---- 🆕 2026-10-25（本轮补的三种）----
                case "ShakeAndSoundObject":
                {
                    // 原版 `OnEnable` → 逐个 `OverwriteCameraShakePreset.Play()`；`Update` → 逐条 `PlaySoundOnTime`
                    var ss = host.gameObject.AddComponent<WFExtraShakeAndSound>();
                    ss.presets = e.presets;
                    ss.sounds = e.sounds;
                    ss.Apply();
                    break;
                }
                case "MaterialFlickerEffect":
                {
                    // 原版四正弦闪烁；复用 `WFMaterialFlicker`（算式已用原版实况验过），只把参数从数据灌进去。
                    // ⚠️ `AddComponent` 会**立刻**跑 `Awake`（只缓存 Renderer/材质，不读这些参数）⇒ 之后再写安全。
                    var f = host.GetComponent<WFMaterialFlicker>();
                    if (f == null) f = host.gameObject.AddComponent<WFMaterialFlicker>();
                    f.amplitude = e.f0 > 0f ? e.f0 : 1f;
                    f.frequency = e.f1;
                    f.fadeInTime = e.f2;
                    f.fadeOutTime = e.f3;
                    f.randomStart = e.f4 != 0f;
                    // 原版 `randomStart` ⇒ `desync = Random.Range(0,100)`（不可复现）。这里是特效件、不需跨局一致 ⇒ 照原版随机。
                    if (f.randomStart) f.desync = UnityEngine.Random.Range(0f, 100f);
                    break;
                }
                case "SpriteFlickerEffect":
                {
                    var sf = host.gameObject.AddComponent<WFExtraSpriteFlicker>();
                    sf.amplitude = e.f0 > 0f ? e.f0 : 1f;
                    sf.frequency = e.f1;
                    sf.fadeInTime = e.f2;
                    sf.fadeOutTime = e.f3;
                    sf.randomStart = e.f4 != 0f;
                    sf.Apply();
                    break;
                }
                case "ParticleForceForceFieldController":
                    // 我们早就有这一件（`WarpforgeParticleForceFieldController`）——这里只是**把它挂上**
                    if (host.GetComponent<WarpforgeParticleForceFieldController>() == null)
                        host.gameObject.AddComponent<WarpforgeParticleForceFieldController>();
                    break;
                default:
                    // 🔴 **已知会丢**的那几种（`ShakeAndSoundObject` / `MaterialFlickerEffect` /
                    //    `SpriteFlickerEffect` / 未来新增）：**出声 + 计数**，⛔ 不静默。
                    Unimplemented++;
                    if (_warnedUnimpl.Add(e.kind))
                        Debug.LogWarning($"[WarpforgeVFX] 🔴 未实现：《{p.EffectName}》上的 `{e.kind}`"
                                       + $"（节点 `{e.node}`）**没有等价组件** ⇒ 这一件的行为会缺。"
                                       + "已知清单与优先级见 `WFEffectExtras.cs` 文件头那张表。"
                                       + "这条警告按 kind 只报一次。");
                    return;
            }
            Attached++;
        }
    }

    // ======================================================================
    //  六个等价组件（逐句照原版；判据见每类的注释）
    // ======================================================================

    /// <summary>原版 `SyncParticleSeeds`（40 挂载 / 36 件 prefab）。
    /// 语义：`seed = Random.Range(0,10000)`；逐粒子系统 —— `playOnAwake` 就先 `Stop()`，
    /// `useAutoRandomSeed = false`、`randomSeed = seed`，`playOnAwake` 再 `Play()`
    /// （`SyncParticleSeeds.cs:8-40`）。⇒ **同一 prefab 里多层粒子共用同一个随机种子**（层间相位同步）。
    /// ⚠️ 原版字段 `particlesSystems` 是一份**手挑的**列表；我们取「本节点子树里的全部 ParticleSystem」
    /// （超集）。差异：原版只同步它挑的那几个；我们全同步 ⇒ 对**同一 prefab** 而言，多同步的那些本来也是
    /// 同一次实例化的，视觉上只会更同步，不会更差。</summary>
    public class WFExtraSyncParticleSeeds : MonoBehaviour
    {
        public void Apply()
        {
            int seed = UnityEngine.Random.Range(0, 10000);
            var all = GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < all.Length; i++)
            {
                var ps = all[i];
                if (ps == null) continue;
                bool playOnAwake = ps.main.playOnAwake;
                if (playOnAwake) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                // ⚠️ `useAutoRandomSeed` / `randomSeed` 是 **ParticleSystem 自己的属性**，
                //    不在 `MainModule` 里（写进 `main` 编不过 —— 2026-10-25 踩过）。
                ps.useAutoRandomSeed = false;
                ps.randomSeed = (uint)seed;
                if (playOnAwake) ps.Play(true);
            }
        }
    }

    /// <summary>原版 `DestroyByTime`（9 挂载 / 7 件 prefab）：`Start` 里 `Destroy(gameObject, lifeTime)`；
    /// **lifeTime == 0 时 warning + 退回 2 秒**（`DestroyByTime.cs:11-30`）。
    /// ⚠️ 原版是 `Start`（Awake 之后），我们在 `Apply()`（= 播放器 `Init`）里排 —— 同一拍，且批处理也走得到。</summary>
    public class WFExtraDestroyByTime : MonoBehaviour
    {
        public float lifeTime;
        public void Apply(WarpforgeEffectPlayer owner)
        {
            float t = lifeTime;
            if (t == 0f)
            {
                Debug.LogWarning("Animation lifeTime in DestroyByTime set to 0");
                t = 2f;                                  // 原版兜底（逐字）
            }
            // 归播放器管：它已经在管这个对象的一生（`_destroyed` 幂等）⇒ 用它的口，
            // 免得「Destroy 了但 Active 表还挂着」。
            if (owner != null) owner.DestroyAfter(t);
            else Destroy(gameObject, t);
        }
    }

    /// <summary>原版 `SubEmitterManualTrigger`（12 挂载 / 3 件 prefab）：
    /// 粒子在播时累计时间，到 `triggerOnSecond` **一次性**对 `subEmitterIndexes` 逐个 `TriggerSubEmitter`；
    /// 粒子停止时把计时与「已触发」复位（`SubEmitterManualTrigger.cs:17-50`）。
    /// ⚠️ `subEmitterIndexes` 原版是手挑的下标数组，我们取**全部子发射器**（`TriggerSubEmitter(i)` 逐 i）——
    /// 超集；与原版不同只在「原版只触发其中几个」。</summary>
    public class WFExtraSubEmitterManualTrigger : MonoBehaviour, IWFEffectExtraTick
    {
        public float triggerOnSecond = 2f;
        int[] _idx;
        float _t;
        bool _done;
        ParticleSystem _ps;

        public void Apply()
        {
            _ps = GetComponent<ParticleSystem>();
            if (_ps == null) _ps = GetComponentInChildren<ParticleSystem>(true);
            _t = 0f; _done = false;
            int n = _ps != null ? _ps.subEmitters.subEmittersCount : 0;
            _idx = new int[n];
            for (int i = 0; i < n; i++) _idx[i] = i;
        }

        public void ExtraTick(float dt)
        {
            if (_ps == null) return;
            if (_ps.isPlaying)
            {
                _t += dt;
                if (!_done && _t >= triggerOnSecond)
                {
                    for (int i = 0; i < _idx.Length; i++) _ps.TriggerSubEmitter(_idx[i]);
                    _done = true;
                }
            }
            if (_ps.isStopped) { _t = 0f; _done = false; }
        }
    }

    /// <summary>原版 `ParticleFindFloorCollider`（9 挂载 / 5 件 prefab）：
    /// 先 LogError「deprecated, use AnimFxModules instead」；按 tag `FloorPositionReference` 找地面，
    /// 找到就 `ps.collision.SetPlane(0, floor)`，找不到再 LogError
    /// （`ParticleFindFloorCollider.cs:10-35`）。
    /// <para>✅ **2026-10-25 订正**：原来这里写「我们的碰撞平面没打这个 tag」—— 已由代理 A 补上，
    /// 但**只补在唯一那块法线朝上的平面上**（`i == 0`）。原版 `ParticleFindFloorCollider__Start.c:20-45`
    /// 的 `floorCollider` 是**静态缓存**、`FindGameObjectWithTag` 只取**第一个匹配**；另外 6 块法线是
    /// **±Z（竖直的墙）** ⇒ 全打会让碰撞面随机变成一面墙。工程 `TagManager.asset:7` 已注册该 tag。</para>
    /// ⚠️ 取的是 `floor.transform`（**位置**）而不是 `Collider` —— `CollisionModule.SetPlane(int, Transform)` 只吃 Transform。</summary>
    public class WFExtraParticleFloorCollider : MonoBehaviour
    {
        public void Apply()
        {
            var ps = GetComponent<ParticleSystem>();
            if (ps == null) ps = GetComponentInChildren<ParticleSystem>(true);
            if (ps == null) return;
            Debug.LogError("[ERROR] ParticleFindFloorCollider is deprecated, use AnimFxModules instead");
            GameObject floor = null;
            try { floor = GameObject.FindGameObjectWithTag("FloorPositionReference"); }
            catch (UnityException) { /* tag 没在 TagManager 里注册 —— 下面统一报 */ }
            if (floor == null)
            {
                Debug.LogError("[ERROR] ParticleFindFloorCollider: Can't find a GameObject tagged "
                             + "`FloorPositionReference` ⇒ 粒子碰撞平面**没有钉到地面**。"
                             + "（我们的碰撞平面建在 `BattleDriver`，但**没打这个 tag** —— 见 "
                             + "`WFEffectExtras.cs` 该类注释。）");
                return;
            }
            var col = ps.collision;
            col.enabled = true;
            col.SetPlane(0, floor.transform);
        }
    }

    /// <summary>原版 `ParticleEmitOnEnable`（2 挂载 / 2 件 prefab）：`OnEnable` 里
    /// `if (!ps.isPlaying) ps.Play()`（`ParticleEmitOnEnable.cs:7-13`）。</summary>
    public class WFExtraParticleEmitOnEnable : MonoBehaviour
    {
        public void Apply()
        {
            var ps = GetComponent<ParticleSystem>();
            if (ps == null) return;
            if (!ps.isPlaying) ps.Play();
        }
    }

    /// <summary>原版 `InitiMaterialWithRandomValue`（1 挂载）：`Start` 里
    /// `material.SetFloat("_Random", Random.Range(minValue, maxValue))`（`InitiMaterialWithRandomValue.cs:19-30`）。
    /// ⚠️ 用 `.material` 的语义（实例化一份）—— 我们的 binder 已经把材质做成**运行时副本**，
    /// 所以直接写 `sharedMaterial` 不会污染资产。</summary>
    public class WFExtraRandomMaterialValue : MonoBehaviour
    {
        public float minValue = -1f;
        public float maxValue = 1f;
        public void Apply()
        {
            var r = GetComponent<Renderer>();
            if (r == null) r = GetComponentInChildren<Renderer>(true);
            if (r == null) return;
            var m = r.sharedMaterial;
            if (m == null || !m.HasProperty("_Random"))
            {
                Debug.LogWarning("[WarpforgeVFX] `InitiMaterialWithRandomValue`：材质上没有 `_Random` 属性 ⇒ 这一句不生效。");
                return;
            }
            m.SetFloat("_Random", UnityEngine.Random.Range(minValue, maxValue));
        }
    }

    /// <summary>原版 `ShaderProperySetter`（2 挂载 / 2 件 prefab）：`applyOnStart` 时把
    /// `floatsProperties[]` 里的 `propertyName → floatValue` 逐条 `material.SetFloat`
    /// （`ShaderProperySetter.cs` 的 `Start` / `ShaderPropertyDefinitionFloat.ApplyParameter`）。
    /// ⚠️ 原版还支持 `applyOnUpdate`；出货数据里我们这 2 处只有 `applyOnStart`（普查只取了这两个 flag）——
    /// 真出现 `applyOnUpdate` 会在 `Apply` 里出声。</summary>
    public class WFExtraShaderPropertyFloat : MonoBehaviour
    {
        public string propertyName;
        public float value;
        public bool applyOnUpdate;

        public void Apply()
        {
            if (applyOnUpdate)
                Debug.LogWarning("[WarpforgeVFX] `ShaderProperySetter.applyOnUpdate=1` 这一档**未实现**"
                               + "（本工程只见到 `applyOnStart`）⇒ 只会写一次。");
            if (string.IsNullOrEmpty(propertyName)) return;
            var r = GetComponent<Renderer>();
            if (r == null) r = GetComponentInChildren<Renderer>(true);
            if (r == null || r.sharedMaterial == null) return;
            if (!r.sharedMaterial.HasProperty(propertyName))
            {
                Debug.LogWarning($"[WarpforgeVFX] `ShaderProperySetter`：材质 `{r.sharedMaterial.name}` 上没有属性 "
                               + $"`{propertyName}` ⇒ 这一句不生效。");
                return;
            }
            r.sharedMaterial.SetFloat(propertyName, value);
        }
    }

    /// <summary>原版 `ShakeAndSoundObject`（35 挂载 / **35 件 prefab**，全部落在**环境卡**那批）：
    /// · `OnEnable` → 逐个 `onEnableShakes[i].Play()`（= `OverwriteCameraShakePreset` → `CameraShakerManager.DoShake`）
    /// · `Update` → 逐条 `sounds[i].Update(Time.deltaTime, transform)`（= `PlaySoundOnTime`：第 `time` 秒起、
    ///   每 `interval` 秒、共 `n = repeat ? loops : 1` 次）
    /// 判据 → `ShakeAndSoundObject.cs:11-40`。
    /// ⚠️ **屏震**走 `WFModuleScreenShake.PlayPresetByName`（我们唯一的屏震收口，下游 = `CardFeel.ShakeCamera`）；
    ///    **声音**走 `WFSoundBank` + `WFSoundPlayer`（与 `AnimFXController.sounds` 同一套 cue 表）。
    ///    ⚠️ 原版那 7 件带屏震的 prefab 里 `presetSO` 只有两张资产：`Shake Earthquake` / `Shake Hit Medium`。</summary>
    public class WFExtraShakeAndSound : MonoBehaviour, IWFEffectExtraTick
    {
        public string[] presets;
        public WFSoundSlot[] sounds;

        float _t;
        int[] _played;
        static readonly HashSet<string> _badCues = new HashSet<string>();

        public void Apply()
        {
            _t = 0f;                                   // 原版 `OnEnable` 起算
            if (presets != null)
                for (int i = 0; i < presets.Length; i++)
                    WFModuleScreenShake.PlayPresetByName(presets[i], "ShakeAndSoundObject");
            _played = new int[sounds != null ? sounds.Length : 0];
        }

        public void ExtraTick(float dt)
        {
            if (sounds == null || sounds.Length == 0) return;
            _t += dt;
            for (int i = 0; i < sounds.Length; i++)
            {
                var s = sounds[i];
                if (s == null || s.total <= 0 || string.IsNullOrEmpty(s.cue)) continue;
                // `while` 而不是 `if` —— 一帧跨过好几个区间时不丢拍（与 `WarpforgeEffectPlayer.TickSounds` 同口径）
                while (_played[i] < s.total && _t >= s.time + _played[i] * s.interval)
                {
                    _played[i]++;
                    WFSoundCue cue;
                    if (!WFSoundBank.TryGetCue(s.cue, out cue))
                    {
                        if (_badCues.Add(s.cue))
                            Debug.LogWarning($"[WarpforgeVFX] `ShakeAndSoundObject` 的音效 cue `{s.cue}` 在音效表里没有"
                                           + " —— 这一条不会响。跑 `工具/import_original_sfx.py` 重生成表。");
                        continue;
                    }
                    WFSoundPlayer.Play(cue, transform.position, s.is2d != 0);
                }
            }
        }
    }

    /// <summary>原版 `SpriteFlickerEffect`（2 挂载 / 1 件 prefab）：与 `MaterialFlickerEffect` **同一套四正弦算式**，
    /// 只是写 `SpriteRenderer.color` 的 α（不是材质）。判据 → `SpriteFlickerEffect.cs:60-100` 的 `Update`
    /// （算式与 `MaterialFlickerEffect__Update.c` 逐项相同，`WFMaterialFlicker` 文件头有机器码级推导）。
    /// ⚠️ 出货 2 处实例都 `playOnAwake=1`（普查只取了 5 个字段）⇒ 这里按「播放即 `fade=1`」落地；
    ///    若将来出现 `playOnAwake=0` 的，会从 1 起而不是 0（已在 `Apply` 里注释）。</summary>
    public class WFExtraSpriteFlicker : MonoBehaviour, IWFEffectExtraTick
    {
        public float amplitude = 1f, frequency = 1f, fadeInTime = 1f, fadeOutTime = 1f;
        public bool randomStart;

        SpriteRenderer _sr;
        Color _base;
        float _origAlpha = 1f, _fade = 1f, _t, _desync;

        /// <summary>最近一次算出来的 alpha（自检读它）。</summary>
        public float CurrentAlpha { get; private set; }
        /// <summary>材质原本的 alpha（`alpha = (noise+1) * 原α * fade系数` 里那个「原α」）。</summary>
        public float OrigAlpha { get { return _origAlpha; } }

        public void Apply()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = GetComponentInChildren<SpriteRenderer>(true);
            if (_sr == null) return;
            _base = _sr.color;
            _origAlpha = _base.a;
            _fade = 1f;                       // 原版 `Awake`：`playOnAwake ? 1 : 0`（见类注释的限定）
            _t = 0f;
            if (randomStart) _desync = UnityEngine.Random.Range(0f, 100f);
        }

        public void ExtraTick(float dt)
        {
            if (_sr == null) return;
            _t += dt;
            float th = (_t + _desync) * frequency;
            float x = th + 0.25f * Mathf.Sin(0.35f * th);
            float noise = amplitude * (0.35f * Mathf.Sin(1.67f * x + 0.15f)
                                     + 0.59f * Mathf.Sin(x + 0.52f)
                                     + 0.28f * Mathf.Sin(3.24f * x - 0.11f)
                                     + 0.17f * Mathf.Sin(7.02f * x - 0.42f));
            float a = (noise + 1f) * _origAlpha * (_fade - 0.25f) / 0.75f;
            CurrentAlpha = a;
            var c = _base;
            c.a = a;
            _sr.color = c;
        }
    }
}
