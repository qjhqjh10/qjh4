// WFModuleMoveParticlesToTarget.cs — 原版 `AnimFxModuleMoveParticlesToTarget` 的对应物
//   ⚠️ **原版类名就是小写的 `AnimFx…`**（不是 `AnimFX…`）—— 我们的 `[WFModuleKind]` 必须与
//      数据里的 kind 逐字一致，`数据/游戏数据/animfx_modules.json` 里就是 `AnimFxModuleMoveParticlesToTarget`。
//   **1 实例**（`Remnant Aeldari Collect particles`：`delay 0.65` / `attractSpeed 2.0` /
//   `attractSpeedByLifetime` = **两个键**`(t=0, v=1.0, 斜率 −3.36e−6)` → `(t=1, v=6.0, 斜率 8.32765)`
//   / `guaranteeFinalPosition 0` / `desiredZPosition -8.0`）
//
// 语义出处：`资料/AnimFX_18类方法体_块2.md` **§5**（6 个方法体：`.ctor` / `Initialize` / `LateUpdate` /
//   `GetTarget` / `SetTarget` / `UpdateTargetPosition`，被两两内联）
//   · `.ctor`：`attractSpeed = 2.0f`、`desiredZPosition = -8.0f`、`particles` = 某静态空数组
//   · `GetTarget(card)`：`card.isPlayer ? BattleManager.playerManager : enemyManager`
//     → **`PlayerManager.GetSpiritStoneManaTransform()`** ⇒ 目标是**那一侧的灵石/法力 UI 锚点**
//     （不是卡、不是格位；效果名 `Remnant Aeldari Collect particles` 正好是灵族收集灵石）
//   · `SetTarget(Transform, Vector3 originalPosition)`：`enabled = true`；存 `target` / `originalPosition`；
//     `targetPosition = CamerasConversionHelper.ConvertPositionBetweenCameras(hudCamera, boardCamera,
//      desiredZPosition - boardCamera.position.z, target.position)`（**HUD 相机 → 棋盘相机的换算**）
//   · `UpdateTargetPosition()`：同一段换算，供目标移动时刷新
//   · `Initialize`：① `particles` 为 null 或长度 < `ps.main.maxParticles` → 重新分配
//     ② `controller.actingCard != null` → `target = GetTarget(actingCard)`，再 `SetTarget(target, ps 自己的位置)`
//   · `LateUpdate`：`curretTime += Time.deltaTime` → **`curretTime < delay` 直接 return**（延时未到不碰粒子）
//     → `n = ps.GetParticles(particles)` → 逐粒子读 `remainingLifetime` / `startLifetime`；
//     `guaranteeFinalPosition == false` 时 `attractSpeedByLifetime.Evaluate(1 - remaining/start)`
//     （`1 -` 里那个 1.0f 是从 DLL 读出来的常量 ⇒ **曲线横轴 = 粒子寿命进度 0→1**）→
//     写回位置 → `ps.SetParticles(particles, n)`
//
// 照方法体还原了什么：
//   · 延时门（`delay`）＋「取粒子 → 逐粒子改位置 → 写回」的整体形状，数组**复用**（不每帧 new）
//   · `attractSpeed` / `attractSpeedByLifetime`（横轴 = 1 − remaining/start）/ `guaranteeFinalPosition` /
//     `desiredZPosition` 四个字段与 `SetTarget` 存 `target` / `originalPosition` 的语义
//   · `SetTarget` 里那句 `enabled = true`（照抄）
//
// 我们定的（显式标出）：
//   · **插值算式是推断，不是读到的** —— 块2 §5 明写「浮点插值算式在反编译里丢了（只剩 Evaluate /
//     Time.deltaTime / 读位置 / 写位置四个调用）」，并按字段名推成
//     「朝 `targetPosition` 以 `attractSpeed × 曲线值 × dt` 靠近」。**本条就是那位会话给的推断**：
//     `p.position = Vector3.MoveTowards(p.position, targetPosition, attractSpeed * 曲线值 * dt)`。
//     要定论得进原版实拍 `Remnant Aeldari Collect particles`（块2 原话："要用就得先实况验一次"）。
//   · **`guaranteeFinalPosition == true` 那条分支也是推断**：方法体里 `if` 只包住 `Evaluate` 那行，
//     「为真时改成什么」丢了。按字段名取最保守的解读 ——「保证最终位置」= 按剩余寿命从 `originalPosition`
//     插值到 `targetPosition`，寿命归零时正好落在目标上（这也解释了为什么要存 `originalPosition`）。
//     ⚠️ 出货数据里唯一实例的 `guaranteeFinalPosition = 0` ⇒ **这条分支从没被用过**。
//     🔴 **2026-09-18 全量反编译复核：上面这条推断要降级，改口嫌「更偏向『为真 = 不吸』」。**
//        实据：`AnimFxModuleMoveParticlesToTarget__LateUpdate.c:47-61` —— 那个 `if (guaranteeFinalPosition)`（字段 `0x58`）
//        **不只包 `Evaluate` 一行**，它**还包住了「读粒子位置 + `Time.deltaTime`」**。
//        ⇒ 「为真」更像是在**跳过整段吸引计算**，而不是「换成另一种插值」。
//        ⚠️ **仍然没改**（`0` 实例、从没被用过）—— 要么重验、要么把这行注释当准；**别照旧的推断写实现**。
//        出处：`资料/AnimFX_实现与接线.md` §十一 的「建议改 ⓒ」。
//   · **目标从哪来**：原版是 `BattleManager → PlayerManager.GetSpiritStoneManaTransform()`，
//     我们这边**没有牌局**（`WarpforgeEffectPlayer` 里也没有 `actingCard`）⇒ 留一个静态钩子
//     `ResolveTarget`（谁装谁负责，与 `WFModuleScreenShake.OnShake` 同款），加两个公开方法
//     `SetTarget()` / `SetTargetAt()`。**没人装钩子时会打警告**（不是静默地吸到原点）。
//   · ~~**没有相机换算**~~ ⇒ 🔴 **2026-10-01 已实现，这句作废**（§三 第 9 条 · ⑤）：
//     方法体在 `d:/2/tools/decomp_full/CamerasConversionHelper__ConvertPositionBetweenCameras.c:17-27`
//     —— **`WorldToViewportPoint` → 换 z → `ViewportToWorldPoint`**；两台相机由 `FromCamera`/`ToCamera`
//     两个静态钩子给（`BattleDriver` 接 HUD 相机 + 棋盘相机）。见 `UpdateTargetPosition`。
//     `desiredZPosition` 现在**参与计算**（= 原版那句里的深度项）。
//     ⚠️ **若那个粒子系统是 Local 模拟空间，两者不在一个坐标系** —— 我们**查不到**这个实例的模拟空间
//     （它的 prefab 没被导出，见下），先按世界坐标做。
//   · **本模块在 `ModuleTick` 里跑**（我们框架只有这一个时机），而原版在 `LateUpdate`：
//     原版是「粒子模拟完之后、渲染之前」改位置，我们在 `Update` 时机改 ⇒ 改动可能被同帧的模拟再推一次。
//     要严格对齐得给播放器加一个 LateUpdate 时机（本次不许动播放器）。
//   · `enabled = true` 照抄，但**门控改用 `HasTarget`**：我们框架的 `ModuleTick` 是**无条件广播**、
//     不看 `enabled`（见 `WFEffectModule` 文件头），没有目标就吸等于把粒子吸到 (0,0,0)。
//
// 🔴 未还原（三条）：
//   ① ~~**曲线本体**：数据里只有一段被截断的 repr 文本~~ ⇒ 🔴 **2026-10-22（`W64` · B13）已解决**：
//      `工具/dump_animfx.py` 不再只导 `repr(v)[:120]`，`数据/游戏数据/animfx_modules.json` 现在带
//      **结构化键** `attractSpeedByLifetime.m_Curve[i].{time,value,inSlope,outSlope,inWeight,outWeight,weightedMode}`
//      （外加 `m_PreInfinity` / `m_PostInfinity`）⇒ 本模块 `Configure` 先读这七个字段逐条建 `Keyframe`
//      （切线/权重都在），**`__repr__` 只作既有数据的兼容退路**（`ParseCurve` / `ParseCurveRepr`）。
//      `truncated` 判据改成「**结构化键在不在**」—— 在 = 曲线完整、**不许**再拿 repr 的末尾去猜。
//      ⚠️ **旧文本已过期**（保留痕迹）：「`工具/dump_animfx.py:250` 写死 `repr(v)[:120]`」——
//      现读那行是 **`:322`**；旧代码 `truncated = !repr.EndsWith("])")` 对完整 repr（结尾是
//      `m_RotationOrder=4)`）**恒为 true** ⇒ 会假报「被截断」并累加 `ApproximatedCurves`。
//   ② **`Remnant Aeldari Collect particles` 这个实例在我们工程里跑不起来** —— 2026-09-18 实测：
//      `WarpforgeVFX/Prefabs/` 下**没有这个 prefab**、`Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset`
//      里**也没有这个效果**（导出侧没搬它）⇒ 本模块现在只能靠手挂 / 将来补导出才会真的跑。
//   ③ 相机换算那一层（理由见上「我们定的」）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    [WFModuleKind("AnimFxModuleMoveParticlesToTarget")]
    public class WFModuleMoveParticlesToTarget : WFEffectModule
    {
        // ---- 数据字段（名字与数据键一致）----
        [Tooltip("原版 `delay`：多久之后才开始吸（延时未到完全不碰粒子）")]
        public float delay = 0f;

        [Tooltip("原版 `attractSpeed`（默认 2.0）")]
        public float attractSpeed = 2f;

        [Tooltip("原版 `attractSpeedByLifetime`：横轴 = 粒子寿命进度 1 − remaining/start。" +
                 "数据走结构化键（七字段逐键），切线/权重都读得到；见文件头 B13")]
        public AnimationCurve attractSpeedByLifetime = AnimationCurve.Constant(0f, 1f, 1f);

        [Tooltip("原版 `guaranteeFinalPosition`：为真时改用「按剩余寿命插值、保证落在目标上」（推断分支，见文件头）")]
        public bool guaranteeFinalPosition;

        [Tooltip("原版 `desiredZPosition`（-8.0）：原版是给相机换算当深度用的。**我们不参与计算**（没有那层换算）")]
        public float desiredZPosition = -8f;

        [Tooltip("原版 `particleSystems`（数据里是**一条** @node 引用；这里用数组兜住数组形式的数据）")]
        public ParticleSystem[] particleSystems = new ParticleSystem[0];

        // ---- 运行期状态（名字对齐原版的字段偏移注释）----
        [Tooltip("原版 `target(+0x60)`")]
        public Transform target;
        [Tooltip("原版 `originalPosition(+0x68)`（= `SetTarget` 那一刻粒子系统的位置）")]
        public Vector3 originalPosition;
        [Tooltip("原版 `targetPosition(+0x84)`")]
        public Vector3 targetPosition;

        /// <summary>原版 `curretTime(+0x80)`。</summary>
        public float Elapsed { get { return _t; } }

        /// <summary>有没有目标（= 原版 `enabled = true` 那条门控的等价物，见文件头）。</summary>
        public bool HasTarget { get { return _hasTarget; } }

        /// <summary>曲线读全了没有（`true` = 只剩 `__repr__` 旧退路且文本被截断；结构化键在时**恒 false**）。
        /// 自检用它 + `ApproximatedCurves` 一起判「B13 真的接上了」。</summary>
        public bool CurveTruncated { get { return _curveTruncated; } }
        /// <summary>`Configure` 实际读到了几个曲线键（本实例应为 **2**）。</summary>
        public int CurveKeys { get { return _curveKeys; } }

        /// <summary>🔴 我们定的钩子：原版走 `BattleManager → PlayerManager.GetSpiritStoneManaTransform()`。
        /// 返回 null = 这一侧没有灵石锚点（会退成「没有目标」，并打一次警告）。
        /// ⚠️ **它给的是 HUD 空间里的那个锚点**（灵石图标），不是棋盘空间里的点 ——
        /// 原版紧跟着就用 `CamerasConversionHelper` 换算过去（见 `UpdateTargetPosition`）。</summary>
        public static Func<Transform> ResolveTarget;

        /// <summary>🆕 2026-10-01（§三 第 9 条 · ⑤）：**相机换算要的那两台相机**（HUD 相机、棋盘相机）。
        /// 原版是 `PostFXController`/`BattleManager` 直接给的两个引用；我们这边由 `BattleDriver` 接。</summary>
        public static Func<Camera> FromCamera, ToCamera;

        /// <summary>做过几次相机换算（自检看它 &gt; 0 = 那座桥真的在用）。</summary>
        public static int ConvertedTargets;
        /// <summary>没接相机、只能原样用 HUD 坐标的次数（&gt; 0 = 换算没生效，目标会吸到错的地方）。</summary>
        public static int MissingCameras;

        /// <summary>因为曲线被截断而只能用近似值的次数（自检用）。</summary>
        public static int ApproximatedCurves;
        /// <summary>没有目标、什么都没吸的次数（>0 = 这条效果在我们这边是空转）。</summary>
        public static int MissingTargets;

        static readonly HashSet<string> _warned = new HashSet<string>();
        public static void ResetDiagnostics()
        { ApproximatedCurves = 0; MissingTargets = 0; ConvertedTargets = 0; MissingCameras = 0; _warned.Clear(); }

        float _t;
        bool _hasTarget;
        bool _curveTruncated;
        int _curveKeys;
        ParticleSystem.Particle[][] _buffers;

        public override void Configure(WFModuleDef def)
        {
            base.Configure(def);
            _def = def;
            if (def == null) return;

            delay                        = def.GetFloat("delay", delay);
            attractSpeed                 = def.GetFloat("attractSpeed", attractSpeed);
            guaranteeFinalPosition       = def.GetBool("guaranteeFinalPosition", guaranteeFinalPosition);
            desiredZPosition             = def.GetFloat("desiredZPosition", desiredZPosition);

            // 🆕 2026-10-22（`W64` · B13）：**先读结构化字段**（七个字段逐条建 `Keyframe`），
            //   读不到才退回 `.__repr__`（既有数据 / 手挂的兼容退路）。判据 → `ParseCurve` 的注释。
            int keys;
            bool trunc;
            var curve = ParseCurve(def, out keys, out trunc);
            if (curve != null)
            {
                attractSpeedByLifetime = curve;
                _curveKeys = keys;
                _curveTruncated = trunc;
            }
        }

        public override void Initialize(WarpforgeEffectPlayer controller)
        {
            base.Initialize(controller);
            ResolveSystems();

            // 原版：`controller.actingCard != null` 才去取目标 —— 我们这边没有 actingCard，改问钩子
            var t = ResolveTarget != null ? ResolveTarget() : null;
            if (t != null) SetTarget(t);
            else
            {
                MissingTargets++;
                WarnOnce($"[WarpforgeVFX] 《{EffectName}》的 AnimFxModuleMoveParticlesToTarget **没有目标** —— " +
                         "原版是 `PlayerManager.GetSpiritStoneManaTransform()`（那一侧的灵石锚点），" +
                         "我们这边没有牌局层 ⇒ 现在它什么都不会吸。" +
                         "要它工作：设 `WFModuleMoveParticlesToTarget.ResolveTarget`，或对实例调 `SetTarget(...)`。");
            }

            if (_curveTruncated)
            {
                ApproximatedCurves++;
                WarnOnce($"🔴 未还原：《{EffectName}》的 `attractSpeedByLifetime` 从 **`__repr__` 旧退路**读到了" +
                         $"一段**被截断**的文本（只读到 {_curveKeys} 个键）⇒ 这条曲线是近似值。" +
                         "修法：重跑 `工具/gen_animfx_modules.py`（`dump_animfx.py` 现在会导出结构化键 " +
                         "`attractSpeedByLifetime.m_Curve[i].*`，本模块优先读那一组）。");
            }
        }

        /// <summary>把 `@node:ParticleSystem:…` 解析成粒子系统。数据里是单数引用，也兜住 `particleSystems[i]` 的数组形式。</summary>
        void ResolveSystems()
        {
            var list = new List<ParticleSystem>();
            if (particleSystems != null)
                foreach (var ps in particleSystems) if (ps != null) list.Add(ps);

            if (list.Count == 0 && _def != null)
            {
                string single = _def.GetString("particleSystems");
                if (!string.IsNullOrEmpty(single))
                {
                    var ps = ResolveNode<ParticleSystem>(transform, single, "AnimFxModuleMoveParticlesToTarget");
                    if (ps != null) list.Add(ps);
                }
                else
                {
                    for (int i = 0; ; i++)
                    {
                        string v = _def.GetString("particleSystems[" + i + "]");
                        if (string.IsNullOrEmpty(v)) break;
                        var ps = ResolveNode<ParticleSystem>(transform, v, "AnimFxModuleMoveParticlesToTarget");
                        if (ps != null) list.Add(ps);
                    }
                }
            }

            if (list.Count == 0)
            {
                // 没解析出粒子系统 = 这个模块没有作用对象。不静默（原版这里会是 NullReference）
                WarnOnce($"[WarpforgeVFX] 《{EffectName}》的 AnimFxModuleMoveParticlesToTarget 没解析出粒子系统" +
                         "（数据键 `particleSystems`）—— 它什么都不会做。");
                MissingTargets++;
                return;
            }

            particleSystems = list.ToArray();
            _buffers = new ParticleSystem.Particle[particleSystems.Length][];
        }

        WFModuleDef _def;

        /// <summary>原版 `SetTarget`：存目标 + 起点、`enabled = true`、算一次 `targetPosition`。</summary>
        public void SetTarget(Transform t)
        {
            if (t == null) return;
            target = t;
            var ps = PrimarySystem();
            originalPosition = ps != null ? ps.transform.position : (transform != null ? transform.position : Vector3.zero);
            enabled = true;                  // 照抄原版（真正的门控是 HasTarget，见文件头）
            _hasTarget = true;
            UpdateTargetPosition();
        }

        /// <summary>给调用方自己算好世界坐标的入口（原版只能传 Transform）。</summary>
        public void SetTargetAt(Vector3 worldPosition)
        {
            var ps = PrimarySystem();
            originalPosition = ps != null ? ps.transform.position : (transform != null ? transform.position : Vector3.zero);
            target = null;
            enabled = true;
            _hasTarget = true;
            targetPosition = worldPosition;
        }

        /// <summary>自检用：把「HUD 空间的一个点」换算到棋盘相机 —— **就是 `UpdateTargetPosition` 里那一句**，
        /// 单独拆出来是为了能在批处理里直接验「换算前后在 HUD 相机上的视口坐标一致」这条定义性质。</summary>
        public static bool TryConvertFromHud(Vector3 hudPos, float depth, out Vector3 boardPos)
        {
            boardPos = hudPos;
            var from = FromCamera != null ? FromCamera() : null;
            var to = ToCamera != null ? ToCamera() : null;
            if (from == null || to == null) return false;
            var v = from.WorldToViewportPoint(hudPos);
            v.z = depth - to.transform.position.z;
            boardPos = to.ViewportToWorldPoint(v);
            return true;
        }

        /// <summary>原版 `UpdateTargetPosition()`：目标移动时刷新。
        /// 🆕 **2026-10-01：相机换算实现了**（原来这里写「原版是相机换算；我们直接取 `target.position`」——
        /// 那句已作废）。判据 = 方法体逐行读过：`CamerasConversionHelper__ConvertPositionBetweenCameras.c:17-27`
        /// ⇒ **在 `from` 相机上做 `WorldToViewportPoint`，把 z 换成给定深度，再在 `to` 相机上
        /// `ViewportToWorldPoint` 回来**；调用点那句是
        /// `ConvertPositionBetweenCameras(hudCamera, boardCamera, desiredZPosition − boardCamera.position.z, target.position)`
        /// （`AnimFxModuleMoveParticlesToTarget__UpdateTargetPosition.c` / `__SetTarget.c` 同源）。
        /// 🔴 **为什么必须有它**：目标是 **HUD 空间**里的灵石图标，粒子在**棋盘相机**的空间里 ——
        ///    不换算就是「往一个别的坐标系里的点吸」。</summary>
        public void UpdateTargetPosition()
        {
            if (target == null) return;
            var from = FromCamera != null ? FromCamera() : null;
            var to = ToCamera != null ? ToCamera() : null;
            if (from != null && to != null)
            {
                var v = from.WorldToViewportPoint(target.position);
                v.z = desiredZPosition - to.transform.position.z;
                targetPosition = to.ViewportToWorldPoint(v);
                ConvertedTargets++;
            }
            else
            {
                // 没接相机 ⇒ 退回原样，并**出声**（别静默地吸到一个错坐标系里的点）
                targetPosition = target.position;
                if (MissingCameras++ == 0)
                    Debug.LogWarning($"[WarpforgeVFX] 《{EffectName}》的 MoveParticlesToTarget **没接相机**" +
                                     "（`FromCamera` / `ToCamera` 是 null）⇒ 只能拿 HUD 空间的坐标直接用，" +
                                     "粒子会吸到错的地方。接法见 `BattleDriver.HookAnimFxCards`。");
            }
        }

        /// <summary>原版 `LateUpdate`。我们框架只有 `ModuleTick`（文件头「我们定的」第 5 条）。</summary>
        public override void ModuleTick(float dt)
        {
            if (!_hasTarget || particleSystems == null || particleSystems.Length == 0) return;

            _t += dt;
            if (_t < delay) return;                 // 原版：延时未到直接 return（一个粒子都不碰）
            // ⚠️ 原版**谁调 `UpdateTargetPosition` 没读到**（方法体在，调用点不在块2 §5 里）
            //    ⇒ 我们每帧刷一次（目标会动，不刷新就会一直吸向旧位置）。这是我们定的。
            if (target != null) UpdateTargetPosition();

            for (int s = 0; s < particleSystems.Length; s++)
            {
                var ps = particleSystems[s];
                if (ps == null) continue;
                var buf = EnsureBuffer(s, ps);
                int n = ps.GetParticles(buf);
                if (n <= 0) continue;

                for (int i = 0; i < n && i < buf.Length; i++)
                {
                    var p = buf[i];
                    float start = p.startLifetime;
                    float progress = start > 0f ? 1f - p.remainingLifetime / start : 1f;   // 原版曲线横轴

                    if (guaranteeFinalPosition)
                    {
                        // 🔴 推断分支（见文件头）：按剩余寿命从 originalPosition 插到 targetPosition
                        p.position = Vector3.Lerp(originalPosition, targetPosition, Mathf.Clamp01(progress));
                    }
                    else
                    {
                        // 🔴 推断算式（见文件头）：朝 targetPosition 以 attractSpeed × 曲线值 × dt 靠近
                        float k = attractSpeedByLifetime != null ? attractSpeedByLifetime.Evaluate(progress) : 1f;
                        p.position = Vector3.MoveTowards(p.position, targetPosition, attractSpeed * k * dt);
                    }
                    buf[i] = p;
                }
                ps.SetParticles(buf, n);
            }
        }

        ParticleSystem PrimarySystem()
        {
            if (particleSystems == null) return null;
            foreach (var ps in particleSystems) if (ps != null) return ps;
            return null;
        }

        /// <summary>原版在 `Initialize` 里分配 `Particle[maxParticles]`；我们按需再补
        /// （粒子数涨了也不越界）。**数组复用**（块2 §5 明写「不能每帧 new」）——
        /// 第一次之后只会因为 `maxParticles` 变大而重分配。</summary>
        ParticleSystem.Particle[] EnsureBuffer(int slot, ParticleSystem ps)
        {
            if (_buffers == null || slot >= _buffers.Length)
            {
                int n = Mathf.Max(slot + 1, particleSystems != null ? particleSystems.Length : 1);
                var grown = new ParticleSystem.Particle[n][];
                if (_buffers != null) Array.Copy(_buffers, grown, _buffers.Length);
                _buffers = grown;
            }
            int need = Mathf.Max(1, ps.main.maxParticles);
            if (_buffers[slot] == null || _buffers[slot].Length < need)
                _buffers[slot] = new ParticleSystem.Particle[need];
            return _buffers[slot];
        }

        void WarnOnce(string msg)
        {
            if (_warned.Contains(msg)) return;
            _warned.Add(msg);
            Debug.LogWarning(msg);
        }

        // ---- 曲线：先读结构化字段，读不到才退回 repr 文本 ----
        //
        // 🔴 **2026-10-22（`W64` · B13）判据**：
        //   原版 `attractSpeedByLifetime` 是完整的 `AnimationCurve`，序列化后就是 `m_Curve[]` 的七元组
        //   （`time/value/inSlope/outSlope/inWeight/outWeight/weightedMode`）+ `m_PreInfinity/m_PostInfinity`。
        //   数据现在按这套逐键导出（`数据/游戏数据/animfx_modules.json`，实例
        //   `Remnant Aeldari Collect particles`：两个键 `(0, 1.0, inSlope −3.359e−06, outWeight 0.07097)`
        //   → `(1.0, 6.0, inSlope/outSlope 8.32765, inWeight 0.02966)`）⇒ **切线/权重一个都不许丢**。
        //   ⚠️ 只读 `time/value` 会把切线填 0 ⇒ 曲线形状变成「折线」，静默错。

        /// <summary>
        /// 从 `WFModuleDef` 取 `attractSpeedByLifetime`。
        /// **先读结构化键**（`attractSpeedByLifetime.m_Curve[i].*` 七个字段逐条建 `Keyframe`）；
        /// 一个结构化键都没有时才退回 `attractSpeedByLifetime.__repr__`（既有数据 / 手挂的兼容退路）。
        /// 返回 `null` = 数据里两样都没有（调用方保持字段上的默认曲线）。
        /// </summary>
        public static AnimationCurve ParseCurve(WFModuleDef def, out int keysRead, out bool truncated)
        {
            keysRead = 0;
            truncated = false;
            if (def == null) return null;

            const string P = "attractSpeedByLifetime.m_Curve";
            int n = def.CountList(P);                     // ⚠️ 用 CountList/点号键，别问 `Has("…[0]")`（见 WFModuleDef 的注释）
            if (n > 0)
            {
                var ks = new List<Keyframe>(n);
                for (int i = 0; i < n; i++)
                {
                    string b = P + "[" + i + "].";
                    float t = def.GetFloat(b + "time", float.NaN);
                    float v = def.GetFloat(b + "value", float.NaN);
                    if (float.IsNaN(t) || float.IsNaN(v)) continue;      // 键不完整 ⇒ 跳过这一条（不猜）
                    var k = new Keyframe(t, v,
                                         def.GetFloat(b + "inSlope", 0f),
                                         def.GetFloat(b + "outSlope", 0f),
                                         def.GetFloat(b + "inWeight", 0f),
                                         def.GetFloat(b + "outWeight", 0f));
                    int wm = def.GetInt(b + "weightedMode", 0);
                    // 🔴 **2026-10-22（`W64` · B13 红）**：**无条件**按数据覆盖 ——
                    //   `Keyframe(t, v, in, out, inW, outW)` 这个 **6 参构造**在 Unity 源码里会把
                    //   `weightedMode` 设成 **`WeightedMode.Both`**（不是默认的 `None`）⇒
                    //   原版数据 `weightedMode = 0` 会被读成 `Both`（`BattleScene` 那条断言当场抓到）。
                    //   ⛔ 别改回 `if (wm != 0)`。
                    k.weightedMode = (WeightedMode)wm;
                    ks.Add(k);
                }
                if (ks.Count > 0)
                {
                    ks.Sort((a, c) => a.time.CompareTo(c.time));
                    var curve = new AnimationCurve(ks.ToArray());
                    ApplyWrap(curve, def);
                    keysRead = ks.Count;
                    // 🔴 `truncated` 的判据 = **结构化键在不在**（在 = 完整）。
                    //    ⛔ 别再用「repr 末尾是不是 `])`」—— 完整 repr 的结尾是 `m_RotationOrder=4)`，
                    //    那条判据对完整数据**恒为 true**（假报「被截断」+ 累加 `ApproximatedCurves`）。
                    truncated = false;
                    return curve;
                }
            }

            string repr = def.GetString("attractSpeedByLifetime.__repr__");
            if (string.IsNullOrEmpty(repr)) return null;
            return ParseCurveRepr(repr, out keysRead, out truncated);
        }

        /// <summary>`m_PreInfinity` / `m_PostInfinity` → `AnimationCurve` 的 wrap。
        /// 序列化值就是 `WrapMode`（`Default 0 / Once 1 / Loop 2 / PingPong 4 / ClampForever 8`）——
        /// 本实例两个都是 **2（Loop）**。值不在那档里就不动（`AnimationCurve` 缺省 wrap）。</summary>
        static void ApplyWrap(AnimationCurve curve, WFModuleDef def)
        {
            int pre = def.GetInt("attractSpeedByLifetime.m_PreInfinity", -1);
            int post = def.GetInt("attractSpeedByLifetime.m_PostInfinity", -1);
            if (IsWrapMode(pre)) curve.preWrapMode = (WrapMode)pre;
            if (IsWrapMode(post)) curve.postWrapMode = (WrapMode)post;
        }

        static bool IsWrapMode(int v) { return v == 0 || v == 1 || v == 2 || v == 4 || v == 8; }

        /// <summary>
        /// **兼容退路**：把 `AnimationCurve(m_Curve=[Keyframe(inSlope=…, outSlope=…, time=0.0, value=1.0, …), …])`
        /// 这种 **repr 文本**拆成曲线（旧数据 / 手挂用；结构化键存在时根本不会走到这里）。
        /// ⚠️ 文本可能是**截断的**（旧版 `工具/dump_animfx.py` 写死 `repr(v)[:120]`，现读 `:322` 已不截断）：
        ///    完整的 `Keyframe(...)` 能读几个算几个；一个都不完整时，尽力读第一个键的 `time=` / `value=`
        ///    当成常值曲线，并置 `truncated = true`（调用方必须报警，不许当成真曲线用）。
        /// 🔴 这里只还原 **time/value**（repr 里的斜率是文本，精度不可靠）—— 所以这条路**一定**比
        ///    结构化字段差；`truncated` 也在这里才有意义。
        /// </summary>
        public static AnimationCurve ParseCurveRepr(string repr, out int keysRead, out bool truncated)
        {
            keysRead = 0;
            truncated = false;
            var keys = new List<Keyframe>();
            if (string.IsNullOrEmpty(repr)) return AnimationCurve.Constant(0f, 1f, 1f);

            foreach (System.Text.RegularExpressions.Match m in
                     System.Text.RegularExpressions.Regex.Matches(repr, @"Keyframe\(([^)]*)\)"))
            {
                float t, v;
                if (!GrabFloat(m.Groups[1].Value, "time=", out t)) continue;
                if (!GrabFloat(m.Groups[1].Value, "value=", out v)) continue;
                keys.Add(new Keyframe(t, v));
            }

            // 完整的 repr 一定带 `m_PostInfinity=` 并以 `)` 收尾（实测 `…, m_RotationOrder=4)`）。
            // ⛔ 旧判据 `!EndsWith("])")` 对**完整** repr 恒为 true（结尾是 `4)`，不是 `])`）。
            truncated = !(repr.IndexOf("m_PostInfinity=", StringComparison.Ordinal) >= 0
                          && repr.TrimEnd().EndsWith(")", StringComparison.Ordinal));

            if (keys.Count == 0)
            {
                // 第一个 Keyframe 也被截断了 ⇒ 尽力读它的 time/value
                float t, v;
                if (GrabFloat(repr, "time=", out t) && GrabFloat(repr, "value=", out v))
                {
                    keys.Add(new Keyframe(t, v));
                    truncated = true;
                }
            }

            if (keys.Count == 0) return AnimationCurve.Constant(0f, 1f, 1f);   // 连一个数都没读到（调用方报警）

            keys.Sort((a, b) => a.time.CompareTo(b.time));
            keysRead = keys.Count;
            return new AnimationCurve(keys.ToArray());
        }

        /// <summary>从 `"… time=0.0, value=1.0, …"` 里取 `名字=` 后面那个数。</summary>
        static bool GrabFloat(string s, string name, out float v)
        {
            v = 0f;
            int i = s.IndexOf(name, StringComparison.Ordinal);
            if (i < 0) return false;
            i += name.Length;
            int j = i;
            while (j < s.Length)
            {
                char c = s[j];
                if (char.IsDigit(c) || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E') j++;
                else break;
            }
            if (j == i) return false;
            return float.TryParse(s.Substring(i, j - i), System.Globalization.NumberStyles.Float,
                                  System.Globalization.CultureInfo.InvariantCulture, out v);
        }
    }
}
