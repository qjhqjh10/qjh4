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
        /// <summary>✅ 已复刻（2026-11-04 · B139-G · 原版 MonoBehaviour `m_Enabled`）—— 条目 13-02 的运行时半边：
        /// 原版这个组件 `m_Enabled == 0` ⇒ `1`（= **不挂**）。
        /// <para>⚠️ 用「`disabled` 为正 ⇒ 跳过」而**不是** `enabled`（默认 1）—— `JsonUtility` 对
        /// **缺字段**给的是「构造后的默认值」，两者的差别只在旧表上；旧表里没有这个键 ⇒ 这里读 0
        /// ⇒ **行为与改前逐字相同**（不会因为表没重生成就把 9 个组件全停掉）。</para>
        /// <para>现读原版 164 个挂载节点上的 266 个 MonoBehaviour 里 **9 个 `m_Enabled = 0`**：
        /// 3 个 `DestroyByTime`（`Blast_Big_Ork_Missiles` 1.5 s · `Necrons Earthquake` ×2 3.0 s）·
        /// 1 个 `SubEmitterManualTrigger`（`Sau_ParticleWhip`）· 5 个嵌套 `AnimFXController`（无行为差）。</para></summary>
        public int disabled;
        /// <summary>🆕 **2026-11-04（条目 13-03）**：`SyncParticleSeeds.particlesSystems` 里每颗 PS 所在
        /// **节点路径**（与 `WFEffectModule.ResolvePath` 同一套 `名字#下标` 约定）。
        /// <para>⚠️ `null`（旧表没有这个键）⇒ 运行时退回「整棵子树」的超集行为并出声；**空数组** = 原版数组确实为空。</para></summary>
        public string[] ps;
        /// <summary>🆕 **2026-11-04（条目 13-04）**：`SubEmitterManualTrigger.subEmitterIndexes` 原文。
        /// <para>⚠️ 同上：`null` = 旧表 ⇒ 退回「全下标」并出声；**空数组** = 原版为空（一次都不触发）。</para></summary>
        public int[] se;
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
        /// <summary>🆕 **2026-11-04（`B139-G` · 条目 13-02 运行时半边）**：原版 `m_Enabled = 0` 因而**不挂**的组件数
        /// （累计；原版现读 9 个，其中 4 个有行为 —— 3 个 `DestroyByTime` + 1 个 `SubEmitterManualTrigger`）。</summary>
        public static int DisabledSkipped { get; private set; }
        /// <summary>🆕 **2026-11-04（`B139-G` · 条目 13-07）**：同一节点上**已经有同语义等价件**、
        /// 因而不重复挂的组件数（`CardPresentation.ParticleEmitOnEnable` 与 `WFExtraParticleEmitOnEnable` 那对）。</summary>
        public static int DuplicateSkipped { get; private set; }
        public static void ResetDiagnostics()
        {
            Attached = 0; Unimplemented = 0; NodeMisses = 0;
            DisabledSkipped = 0; DuplicateSkipped = 0; _warnedUnimpl.Clear();
        }

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

            // 🆕 2026-11-04（`B139_G` · 条目 13-02 运行时半边）：原版 `m_Enabled = 0` ⇒ **不挂**（原版那个组件
            //   在 prefab 上是禁用状态，`Start`/`Update` 都不会跑）。⛔ 别「挂上再 SetEnabled(false)」——
            //   `DestroyByTime` 是在 `Apply()`（我们这一跳）里排销毁的，挂上就已经晚了。
            //   判据：原版 MonoBehaviour JSON 的 `m_Enabled`（现读 9 个 0，其中 4 个有行为）。
            if (e.disabled > 0) { DisabledSkipped++; return; }

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
                    // 🆕 2026-11-04（条目 13-03）：只同步原版 `particlesSystems` 里**点名的那几颗**。
                    host.gameObject.AddComponent<WFExtraSyncParticleSeeds>().Apply(p.transform, e.ps);
                    break;
                case "DestroyByTime":
                    var d = host.gameObject.AddComponent<WFExtraDestroyByTime>();
                    d.lifeTime = e.f0; d.Apply(p);
                    break;
                case "SubEmitterManualTrigger":
                    var s = host.gameObject.AddComponent<WFExtraSubEmitterManualTrigger>();
                    s.triggerOnSecond = e.f0; s.Apply(e.se);   // 🆕 2026-11-04（条目 13-04）：只按原版下标数组触发
                    break;
                case "ParticleFindFloorCollider":
                    host.gameObject.AddComponent<WFExtraParticleFloorCollider>().Apply();
                    break;
                case "ParticleEmitOnEnable":
                    // 🆕 2026-11-04（`B139_G` · 条目 13-07）：同一节点上**已经有同语义的等价件**就不重复挂。
                    //   背景：`CardPresentation/Core/ParticleEmitOnEnable.cs`（`CardView.MountSpawnParticles` 手挂）
                    //   与 `WFExtraParticleEmitOnEnable`（本表挂）**语义逐字相同**（`if (!ps.isPlaying) ps.Play();`），
                    //   而 `Invoke Minion Card Fade Default/Trail#2` 这个节点**两条路都会挂** ⇒ 同节点两个组件。
                    //   铁律「两处写同一条规则 = 迟早不一致」⇒ 这里让路给**先挂上的那个**（手挂的 CardPresentation 那份）。
                    //   ⛔ 判据用**类型名**而不是 `using CardPresentation;` —— 依赖方向是「特效库认识卡牌」，
                    //      但特效库**不把战场表现绑死**（`CardEffects` 那套回调就是这么解耦的）。
                    //   ⚠️ 两个名字都查：`ParticleEmitOnEnable`（CardPresentation 那份）与 `WFExtraParticleEmitOnEnable`
                    //      （本文件这一份）—— 后者让 `AttachAll` **重复调用也幂等**（自检会拿它验这条，见 `B139_G`）。
                    if (HasSameSemanticsComponent(host.gameObject, "ParticleEmitOnEnable")
                        || HasSameSemanticsComponent(host.gameObject, "WFExtraParticleEmitOnEnable"))
                    { DuplicateSkipped++; break; }
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
                    // 🔴 走到这里 = **`effect_extras.json` 里出现了本类还没实现的新 kind**（`Attach` 的 case 已经覆盖
                    //    原版全部 11 种：`ShakeAndSoundObject`/`MaterialFlickerEffect`/`SpriteFlickerEffect` 都在上面）：出声 + 计数，⛔ 不静默。
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

        /// <summary>🆕 2026-11-04（`B139_G` · 条目 13-07）：这个 GameObject 上有没有**同语义的另一份实现**
        /// （按**类型名**判，⛔ 不引 `CardPresentation` 的 `using` —— 见 `ParticleEmitOnEnable` 那一支的注释）。
        /// <para>先查已挂的那些（手挂的 `CardPresentation.ParticleEmitOnEnable` 在
        /// `CardView.MountSpawnParticles` 里先于本表执行）—— 本方法**只查、不挂**。</para></summary>
        public static bool HasSameSemanticsComponent(GameObject go, string typeName)
        {
            if (go == null || string.IsNullOrEmpty(typeName)) return false;
            var all = go.GetComponents<MonoBehaviour>();
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].GetType().Name == typeName) return true;
            return false;
        }
    }

}
