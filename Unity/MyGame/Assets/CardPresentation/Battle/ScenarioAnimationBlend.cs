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
    /// <summary>`ScenarioAnimationBlend`（原版 **2 个实例**，都在 darkangels 场：`Directional Light` 与
    /// `Battle Arena Dark Angels baked`）。判据 = `ScenarioAnimationBlend__{DoScenarioBlend,AddAnimation,Initialize}.c`：
    ///   · **只在 `direction == false`** 那一程干活（`if (options.ScenarioBlendDirection) return;`）
    ///   · 取**当前环境 SO** 的 `animationsToChange[]`（原版 `Manager.CurrentEnvironment`(+0x28) → SO(+0x60)），
    ///     逐条 `if (atc.filterCode == this.filterCode)` ⇒ `clip = atc.clip.Load()` ⇒
    ///     `myAnimation.AddClip(clip, clip.name)`；`autoPlayWhenChange` 时再 `myAnimation.CrossFade(name, blendTime)`
    ///   · 命中 **> 1 条** ⇒ `CustomDebug.LogWarning`（原版那句是 `Concat(...)` 拼出来的告警）
    ///   · `AddAnimation(clip, alsoPlay = true)` 是公开方法，走的是同一条路
    /// 🔴 **这是【另一套 filter】，别与 `ScenarioBlendOptions.FilterOptions` 混**：这套比的是
    ///   **组件自己的 `filterCode`(+0x28)** ↔ **SO 里 `animationsToChange[]` 每条的 `filterCode`**。
    /// 🔴 **两处数据缺口（如实记着，不是「已做」；两处都会出声）**：
    ///   ① ✅ **2026-10-07（A192）已补上**：那两个 clip（`AssetReferenceTyped&lt;AnimationClip>`，**按 GUID 取**）
    ///      现在由 `工具/extract_missing_shaders.py --prefabs` 收进 `wf_prefabs_extra.bundle`、
    ///      并按 **assetGUID** 登记容器别名（原版源包的容器键就是 GUID）⇒
    ///      `ScenarioBlendableFactory.AnimationClipByGuid` 按 GUID 取原件；取不到仍然出声。
    ///      ✅ **2026-10-11（A201）那一跳已经有一条真跑过的自检**（`Editor/BattleScene.cs` 的 A201 一节：
    ///      GUID → `LoadAsset&lt;AnimationClip>` → `Animation.AddClip` 走一遍）。
    ///      ⛔ **仍然有一条不会播，而那是照抄原版数据的结果，别去「修」**：
    ///        · `Directional Light` 那颗的 `filterCode` = `LightAnimationOrbital`，SO 那条写的是
    ///          `LightAnimationOrbit`（差一个 `al`）⇒ **原版自己这一对永远配不上**。
    ///   ② ✅ **2026-10-11（A191）已补**（这一格原来写「我们 13 件 arena prefab 里一个 `Animation` 组件都没有」+
    ///      「`Battle Arena Dark Angels baked` 那个分组节点我们工程里没有」—— **两句都已不成立**）：
    ///      那个分组节点已照原版建成**真分组节点**（连同它的整棵原子树，`ArenaBuilder.ApplyGroupNodes`），
    ///      它和 `Directional Light` 两颗的 `Animation` 组件**都按原版补上了**（旁挂 `animation` 标记）
    ///      ⇒ `myAnimation` **不再是 null**；建场那一步零条没对上（自检盯着）。
    /// ⚠️ **顺带一条实测**：`Directional Light` 那颗的 `filterCode = "LightAnimationOrbital"`，而 SO
    ///   `Dark Angels Orbiting` 里那条是 `"LightAnimationOrbit"`（**差一个 `al`**）⇒ **原版自己这一对永远匹配不上**。
    ///   我们**照抄**（不做模糊匹配）—— 不替原版「修正」数据。</summary>
    public class ScenarioAnimationBlend : ScenarioBlendable
    {
        public string filterCode;
        public Animation myAnimation;
        public float blendTime = 0.3f;          // 原版 ctor 默认 0.3（`…__.ctor.c` 里的 0x3e99999a）
        public bool autoPlayWhenChange = true;  // 原版 ctor 默认 true

        /// <summary>GUID → `AnimationClip`。**原版是 `AssetReferenceTyped&lt;AnimationClip>.Load()`**；
        /// 由工厂注入 `ScenarioBlendableFactory.AnimationClipByGuid` —— 从重打的
        /// `wf_prefabs_extra.bundle` 里**按 assetGUID** 取原件；**拿不到仍返回 null 并出声**。</summary>
        public Func<string, AnimationClip> clipLoader;

        bool _warnedNoAnim, _warnedNoClip;

        /// <summary>**不回调**（判据：原版整个方法体里没有 `OnComplete`）。</summary>
        public override bool NotifiesComplete { get { return false; } }

        public override void DoScenarioBlend(ScenarioBlendOptions o)
        {
            if (o.direction) return;                                  // 原版第一句：只在「撤环境」那一程干活
            if (string.IsNullOrEmpty(filterCode)) return;             // 原版：filterCode 空 ⇒ 整段不进
            var so = (manager != null) ? manager.CurrentItem : null;
            var list = (so != null) ? so.animationsToChange : null;
            if (list == null || list.Length == 0) return;             // SO 里没有这条 ⇒ 原版也就没事可做
            if (myAnimation == null)
            {
                if (!_warnedNoAnim)
                {
                    _warnedNoAnim = true;
                    Debug.LogError($"[EnvBlend] `ScenarioAnimationBlend`({name}) 的 `myAnimation` 是空的 —— "
                                 + "我们 13 件 arena prefab 里没有 `Animation` 组件（旁挂 `_missingTargets` 里记着）"
                                 + " ⇒ 这一条不生效（出声，不静默）");
                }
                return;
            }
            int n = 0;
            for (int i = 0; i < list.Length; i++)
            {
                var atc = list[i];
                if (atc == null || atc.filterCode != filterCode) continue;
                var clip = (clipLoader != null) ? clipLoader(atc.clip) : null;
                if (clip == null)
                {
                    if (!_warnedNoClip)
                    {
                        _warnedNoClip = true;
                        Debug.LogError($"[EnvBlend] `ScenarioAnimationBlend`({name}) 命中了 SO `{so.so}` 的一条 "
                                     + $"`animationsToChange`（filterCode=`{atc.filterCode}`），但它要的 clip "
                                     + $"（assetGUID `{atc.clip}`）**取不到** ⇒ 这一条不生效（出声，不静默；"
                                     + "取不到的原因见上面 `AnimationClipByGuid` 那条报错）");
                    }
                    continue;
                }
                AddAnimation(clip, autoPlayWhenChange);
                n++;
            }
            if (n > 1)
                Debug.LogWarning($"[EnvBlend] `ScenarioAnimationBlend`({name})：SO `{so.so}` 里有 {n} 条 "
                               + $"`animationsToChange` 都命中 `filterCode={filterCode}` —— 原版也告警（同名 clip 会被重复 AddClip）");
        }

        /// <summary>判据 = `…__AddAnimation.c`：`AddClip(clip, clip.name)`；`alsoPlay` 时再 `CrossFade(clip.name, blendTime)`。</summary>
        public void AddAnimation(AnimationClip clipToAdd, bool alsoPlay = true)
        {
            if (clipToAdd == null || myAnimation == null) return;
            myAnimation.AddClip(clipToAdd, clipToAdd.name);
            if (alsoPlay) myAnimation.CrossFade(clipToAdd.name, blendTime);
        }
    }
}
