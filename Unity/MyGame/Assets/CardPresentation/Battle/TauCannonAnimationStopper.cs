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
    /// <summary>`TauCannonAnimationStopper`（原版 **2 个实例**，都在 tauviorla 场，挂在
    /// `…/Railgun Turret 1(2)/Railgun Turret Base.00N/Cylinder.00N` 这一族上）。
    /// 判据 = `TauCannonAnimationStopper__{DoScenarioBlend,Toggle,Initialize}.c` ＋ 反汇编
    ///   （`DoScenarioBlend` RVA 0x631B20 · `Toggle` RVA 0x631C00；每个调用目标都拿 `script.json` 的
    ///    `ScriptMethod` 核过：`Behaviour.set_enabled` / `ParticleSystem.Play` / `ParticleSystem.Stop` /
    ///    `Quaternion.Internal_FromEulerRad` / `DOLocalRotateQuaternion` / `SetEase`）：
    ///   `DoScenarioBlend(o)`：`if (o == null) 抛 NRE; if (o.ScenarioBlendDirection) return; Toggle(false);`
    ///   `Toggle(option)`：
    ///     ① `foreach (c in lookAtConstrains) c.enabled = option`
    ///     ② `foreach (ps in particleSystems) { option ? ps.Play(false) : ps.Stop(false); }`
    ///        （⚠️ 反汇编里两次调用的 `withChildren` 实参**都是 0** = `Play(false)`/`Stop(false)`；
    ///         `.c` 把这两个实参印丢了 —— 不是我们挑的）
    ///     ③ `if (animFXController != null) { animFXController.enabled = option;
    ///          if (!option) { animationComponent.enabled = false;
    ///            cannon.DOLocalRotateQuaternion(Quaternion.Euler(finalRotation * Deg2Rad), 0.8f)
    ///                 .SetEase(Ease.OutBounce); } }` —— **这一整支被 `animFXController != null` 挡着**
    ///        （常量：`Deg2Rad` = `DAT_1834b2dc0` = 0.0174532924；时长 = `DAT_1834b2fa0` = **0.8**；
    ///         缓动 = `SetEase(0x1e)`，`DG.Tweening.Ease`（TypeDefIndex 19111）第 30 项 = **OutBounce**）
    /// 🔴 **缺的是哪几件（2026-10-11 更新）**：原版那三类资产里，
    ///   ① ✅ **`LookAtConstrainWIP` / `AnimFXController` 两个类** —— **2026-10-11（A196）已经移植**
    ///      （`Battle/LookAtConstrainWIP.cs` · `Battle/AnimFXController.cs`，
    ///       类名/字段/方法体逐句照 `d:/2/tools/decomp_full/`）；
    ///   ② ✅ **宿主对象（A191）—— 2026-10-11 照原版建出来了**（原来这里写「仍然没有」）：
    ///      `Railgun Turret 1/2`（带 `Animation` 的父节点）· `Railgun turret`（`AnimFXController` 的宿主）·
    ///      `Railgun Turret N Target`（`LookAtConstrainWIP.target` 指的对象）四条宿主 + 两条 Target。
    ///      建场 = `ArenaBuilder.ApplyGroupNodes`（旁挂 `arenas/battlearenatauviorla/battlearenatauviorla_groups.json`，
    ///      由 `工具/gen_arena_groups.py` 直读原版场景包产出；那两件 Target **不在 `_missingTargets` 里** —— 那张表
    ///      只查 blendable 自己的 target，判据 → `资料/普查产出_1011/W9_A196_A210_A211.md` §五·4）。
    ///   ⇒ `lookAtConstrains` / `animFXController` / `animationComponent` 三处**现在解析得到**了；
    ///      真解析不到时工厂仍然「出声、**不建**」（见 `MakeLookAtConstrains` / `MakeAnimFx`）。
    ///   ⚠️ **但原版第 ③ 支仍然不执行** —— 那**不是**因为宿主缺，而是**原版自己**就把它挡在
    ///      `animFXController != null` 后面、而它自己那些实例的 `animFXController` 字段**有值**
    ///      （实测那两个 `TauCannonAnimationStopper` 各 1 个、**2/2 都指到了 `Railgun turret` 上那颗**
    ///        = MB 4767 / 5247）⇒ 原版**是会回位的**。
    ///      我们这条是否真的回位**要等一次 Unity 实跑**（`Toggle(false)` 里那条出声会告诉你走没走到）。
    /// ⚠️ 有意偏离：原版 ③ 用 DOTween；本仓口径是手推 `Advance(dt)`（缓动仍照 `SetEase(0x1e)` = OutBounce 实现）。</summary>
    public class TauCannonAnimationStopper : ScenarioBlendable
    {
        /// <summary>原版 `LookAtConstrainWIP[]`。🆕 2026-10-11（A196）：**类型从 `Behaviour[]` 换成真类**
        /// —— 由 `MakeLookAtConstrains` 就地建（宿主对象在不在见类注释 ②）。空数组 = 一条都没建出来。</summary>
        public LookAtConstrainWIP[] lookAtConstrains;
        public Animation animationComponent;
        public Transform cannon;
        public Vector3 finalRotation;
        public ParticleSystem[] particleSystems;
        /// <summary>原版 `AnimFXController`。🆕 2026-10-11（A196）：**类型从 `Behaviour` 换成真类**
        /// —— 由 `MakeAnimFx` 就地建。null = 宿主对象不在（今天就是，见类注释 ②）。</summary>
        public AnimFXController animFXController;

        /// <summary>原版那条 `DOLocalRotateQuaternion(..., 0.8f)` 的时长（`DAT_1834b2fa0` = 0.800000012）。</summary>
        public const float CannonReturnTime = 0.8f;

        /// <summary>**不回调**（判据：原版 `DoScenarioBlend` 只有 `Toggle(false)` 一句）。</summary>
        public override bool NotifiesComplete { get { return false; } }

        Quaternion _rotFrom, _rotTo;
        float _rotLeft;
        bool _warnedLookAt, _warnedAnimFx;

        public override void DoScenarioBlend(ScenarioBlendOptions o)
        {
            if (!o.direction) Toggle(false);
        }

        /// <summary>判据见类注释（① ② ③ 三步；③ 整支被 `animFXController != null` 挡着）。</summary>
        public void Toggle(bool option)
        {
            if (lookAtConstrains != null)
                for (int i = 0; i < lookAtConstrains.Length; i++)
                    if (lookAtConstrains[i] != null) lookAtConstrains[i].enabled = option;

            if (particleSystems != null)
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    var ps = particleSystems[i];
                    if (ps == null) continue;
                    if (option) ps.Play(false); else ps.Stop(false);     // withChildren = false（反汇编 edx=0）
                }

            if (animFXController != null)
            {
                animFXController.enabled = option;
                if (!option)
                {
                    if (animationComponent == null)
                    {
                        Debug.LogError($"[EnvBlend] `TauCannonAnimationStopper`({name})：`animationComponent` 是空的 —— "
                                     + "原版这一句会抛 NRE；我们出声并停手（不许静默）");
                        return;
                    }
                    animationComponent.enabled = false;
                    BeginCannonReturn();
                }
            }
            else if (!option && !_warnedAnimFx)
            {
                // 忠实照抄 ⇒ 没有 `animFXController` 就不回位。**出声**说清是「**宿主对象**没有」而不是「我们没做」
                //（2026-10-11 A196 之前这句说的是「那个类我们工程里没有」—— 类早就不缺了；
                //  🆕 2026-10-11 A191 之后**宿主也补齐了** ⇒ **这一条正常情况下不该再出现**；
                //  真出现说明那件 arena prefab 还是旧的：跑 `-executeMethod ArenaBuilder.BuildArenaPrefabs`。）
                _warnedAnimFx = true;
                Debug.LogWarning($"[EnvBlend] `TauCannonAnimationStopper`({name})：`animFXController` 解析不到"
                               + "（它的宿主 `Railgun turret` 应当已由 A191 建场建出 —— 见 `ArenaBuilder.ApplyGroupNodes`；"
                               + " 若仍出现 ⇒ 那件 arena prefab 没重建，跑 `ArenaBuilder.BuildArenaPrefabs`）"
                               + " ⇒ 原版第 ③ 支（关 `animationComponent` + 炮塔转回 `finalRotation`）不执行"
                               + "（出声，不静默）");
            }
            if (!option && (lookAtConstrains == null || lookAtConstrains.Length == 0) && !_warnedLookAt)
            {
                _warnedLookAt = true;
                Debug.LogWarning($"[EnvBlend] `TauCannonAnimationStopper`({name})：`lookAtConstrains` 一条都没建出来"
                               + "（`LookAtConstrainWIP` 的宿主或它自己的 `target` 应当已由 A191 建场建出 —— 见 "
                               + "`ArenaBuilder.ApplyGroupNodes`；若仍出现 ⇒ 那件 arena prefab 没重建，"
                               + "跑 `ArenaBuilder.BuildArenaPrefabs`）"
                               + " ⇒ 原版第 ① 步不生效（出声，不静默）");
            }
        }

        /// <summary>炮塔转回 `finalRotation`（原版 = `DOLocalRotateQuaternion(Euler(finalRotation * Deg2Rad), 0.8s)`
        /// `.SetEase(Ease.OutBounce)`）。我们手推 —— 缓动照 `SetEase(0x1e)` = **OutBounce**（Penner 那条）。</summary>
        void BeginCannonReturn()
        {
            if (cannon == null)
            {
                Debug.LogError($"[EnvBlend] `TauCannonAnimationStopper`({name})：`cannon` 是空的 —— 炮塔转不回去（出声）");
                return;
            }
            _rotFrom = cannon.localRotation;
            _rotTo = Quaternion.Euler(finalRotation * Mathf.Deg2Rad);   // ⚠️ `Internal_FromEulerRad` 收的是**弧度**
            _rotLeft = CannonReturnTime;
        }

        public override void Advance(float dt)
        {
            if (_rotLeft <= 0f || cannon == null) return;
            _rotLeft -= dt;
            float t = Mathf.Clamp01(1f - Mathf.Max(0f, _rotLeft) / CannonReturnTime);
            cannon.localRotation = Quaternion.Slerp(_rotFrom, _rotTo, OutBounce(t));
            if (_rotLeft <= 0f) cannon.localRotation = _rotTo;
        }

        /// <summary>DOTween `Ease.OutBounce`（= `DG.Tweening.Ease` 第 30 项，反汇编里 `SetEase(0x1e)`）。
        /// 算式 = Penner 的经典 OutBounce（DOTween 用的就是它），`t ∈ [0,1]`、`b=0` / `c=1`。
        /// ⚠️ **2026-10-12（A420）订正**：原来这里写「这一支今天走不到（`animFXController` 我们工程里没有）
        /// ⇒ 真验它得等那件资产补齐」—— **那句已经过期**（本文件 `TauCannonAnimationStopper` 的类注释
        /// ③ 就写着「`animFXController` 现在解析得到了、2/2 实例都指到了 `Railgun turret` 上那颗」，
        /// 两处打架）。⇒ 这一支**跑得到**；`OutBounce` 自己**没有直接断言**（自检断到的是那一层的
        /// `sounds`，见 `Editor/BattleScene.cs` 的 `Run` 里那条 `sounds` 层断言）⇒ 留给「画面/手感」那一档（真 Play 清单）。</summary>
        public static float OutBounce(float t)
        {
            const float n1 = 7.5625f, d1 = 2.75f;
            if (t < 1f / d1) { return n1 * t * t; }
            if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
            if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
            t -= 2.625f / d1; return n1 * t * t + 0.984375f;
        }
    }
}
