// FlyingCardController.cs — 原版 `FlyingCardController`（飞行单位的「抬到空中 + 上下浮动」）的复刻
//
// ─────────────────────────────────────────────────────────────────────────────
// 【原版是谁 · 第一权威】`d:/2/tools/decomp_full/FlyingCardController__{Awake,Toggle,ChangeState,
//   Update,GoToTargetPosition,FlyingState,GetFlyPosition,InTransitionToFlyState,
//   InTransitionToLandState,OnDisable,IsFlying}.c` —— **全篇逐句亲读**（2026-10-19）。
//   ⚠️ `任务/T05_逐卡演出与特效接线.md` §2·4 原来写「`__InTransitionToFlyState` / `__InTransitionToLandState`
//      / `__Toggle` 的方法体【还没读】」—— 🔴 **2026-10-19 就地订正（铁律 5）：它们都在 `decomp_full` 里、
//      已逐句读完**（本文件就是照它们写的）。那句过期记录的理由 = 当时只查了签名桩那一份。
//
// 【四态机】`States { Landed = 0, InTransitionToLand = 1, InTransitionToFly = 2, Flying = 3 }`
//   · `Toggle(option, time)`：`time < 0` ⇒ LogWarning + 归零；`time == 0` ⇒ 直接进 `Flying / Landed`；
//     `time > 0` ⇒ 进 `InTransitionToFly / InTransitionToLand`。
//     🔴 调用点 `BattleCardUI__ToggleFlying.c` 传的是 **`DAT_1834b3158 = 0.4`（4 字节 float，DLL 实读）**
//     ⇒ 生产走的是**过渡态**（0.4 s），⛔ 不是瞬切。
//   · `ChangeState` 的两条早退：`InTransitionToFly` 而当前已经 `Flying` ⇒ 不动；
//     `InTransitionToLand` 而当前已经 `Landed` ⇒ 不动。进入 `Landed` 时**复位 `localPosition`
//     并回调 `OnFlyingToggleFinish(false)`**；进入 `Flying` 时回 `true`。
//   · `InTransitionToLand`：`target = originalLocalPosition`（= 落地）。
//     `InTransitionToFly`：`target = GetFlyPosition()` = 原位置 + `initialHeightOffset`
//     + `flyingHeight × curve(currentTime)`（此刻 `currentTime` 刚清 0 ⇒ ≈ +0.4）。
//   · `GoToTargetPosition`：`currentTime += dt`；`t = clamp01(currentTime / desiredChangeTime)`；
//     `localPosition = lerp(起始位置, target, t)`；**到达判据 = 写之前那个位置与 target 的距离² ≤ 0.0001**
//     （`DAT_1834b2f48 = 0.0001`，4 字节 float，DLL 实读）⇒ 真正到位的**下一帧**才返回 true。
//   · `Flying`：`currentTime += dt × animationSpeed`；
//     `localPosition.y = originalLocalPosition.y + initialHeightOffset + flyingHeight × curve(currentTime)`。
//
// 【参数是【全局一份】、不逐卡】—— `assets_full` 里带 `yMovementCurve` 的 MonoBehaviour 只有 1 份：
//   `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour_2087276408211151808.json`，逐字段实读：
//     `initialHeightOffset = 0.4` · `flyingHeight = 0.1` · `animationSpeed = 0.35`
//     `yMovementCurve` = 4 键：`t 0 → 0.0002288818` · **`t 0.25 → +1.0`** · **`t 0.75 → −1.0`** · `t 1 → 0`
//     切线 `in/outSlope` 见下面 `BuildCurve()` 的注释；`m_PreInfinity = 2`（Loop）· **`m_PostInfinity = 1`（Once）**。
//   ⇒ **一个「先上后下」的往复**：`t = 0.25` 时最高（+0.5 世界单位），`t = 0.75` 最低（+0.3），`t = 1` 回 +0.4。
//   🔴 **`postWrap = Once` 意味着第一个循环走完（`t = 1`，≈ 2.86 s 后）就停在 +0.4、不再摆** ——
//     ⛔ 别自作主张改成永久循环（那与原版不符）。
//   ⚠️ 量纲 = **原版战场世界单位**（卡 prefab 根 `Transform.m_LocalScale = (1,1,1)`、父链 scale 全 1，
//     判据 → `任务/T05` §2·4 的 2026-10-16 那两段）⇒ `0.4` 直接加在**卡根自己的 `localPosition.y`** 上，
//     不需要任何换算。
//
// 【什么时候飞】原版由 **status 动画**驱动（`BattleCardUI__DisplayStatusAnim.c:297-310` /
//   `__RemoveStatusAnim.c:166-172`，逐句读）：
//     · 显示 `DefinedTrait.flying`（0x186 = 390）的徽标、且这张卡**没有** `vanguard`（0x3d4 = 980）⇒ `ToggleFlying(true)`；
//     · 显示 `vanguard` 徽标、且这张卡**有** `flying` ⇒ `ToggleFlying(false)`；
//     · 摘掉任一 status ⇒ `HasCurrentTrait(flying) ? true : false`。
//   ⇒ **判据收成一句：`Has(Flying) && !Has(Vanguard)`**（判据只此一处 = `BattleDriver.SyncUnitFlying`）。
//
// 【我们改的两处（如实记，铁律 3）】
//   ① 原版是 `MonoBehaviour`、靠 `Update` 每帧推；本工程批处理没有帧循环 ⇒ 改成**普通类**、
//      由 `BattleDriver.AdvanceTimeline` 那个唯一的泵推进（本线 `准则与做法.md` §六·6）。
//   ② `Flying` 态里原版每帧把 **x/z 也写回 `originalLocalPosition`**；我们**只写 y**
//      （x/z 保持当前值）—— 因为我们的攻击冲刺 / 让位补间是直接 tween `transform` 的，
//      全量写回会把那些位移每帧抹掉。过渡态（1/2）仍按原版**全向量** lerp。
// ─────────────────────────────────────────────────────────────────────────────

using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `FlyingCardController` 的复刻（四态机 + 一条往复曲线）。判据见文件头。</summary>
    public sealed class FlyingCardController
    {
        /// <summary>四态（原版 `FlyingCardController.States`，TypeDefIndex 760，逐值照抄）。</summary>
        public enum States
        {
            Landed = 0,
            InTransitionToLand = 1,
            InTransitionToFly = 2,
            Flying = 3,
        }

        // ── 参数（原版序列化值；出处 = `MonoBehaviour_2087276408211151808.json`，见文件头）──
        public const float InitialHeightOffset = 0.4f;
        public const float FlyingHeight = 0.1f;
        public const float AnimationSpeed = 0.35f;
        /// <summary>`BattleCardUI.ToggleFlying` → `Toggle(option, DAT_1834b3158)` 的那个时间常数。
        /// **4 字节 float = 0.4**（`工具/read_literal.py` 实读 `d:/2/unity_run_ref/GameAssembly.dll`）。</summary>
        public const float DefaultTransitionTime = 0.4f;
        /// <summary>到达判据（原版 `DAT_1834b2f48` = **0.0001**，4 字节 float，DLL 实读）= 距离的**平方**阈值。</summary>
        public const float ArriveEpsilonSq = 0.0001f;

        readonly Transform _t;
        Vector3 _home;                     // 原版 `originalLocalPosition` (0x50)
        Vector3 _target;                   // 原版 `targetPosition` (0x44)
        Vector3 _from;                     // 原版 `originalPositionOnChangeStarted` (0x60)
        float _time;                       // 原版 `currentTime` (0x40)
        float _desiredChangeTime = DefaultTransitionTime;   // 原版 `desiredChangeTime` (0x6C)
        States _state = States.Landed;     // 原版 `currentState` (0x5C)

        /// <summary>原版 `OnFlyingToggleFinish`（`Action&lt;bool&gt;`）：过渡收尾时回调（true = 飞起 / false = 落地）。
        /// 🔴 **2026-10-24 就地订正（铁律 5）**：原来这里写「⚠️ 生产今天**没有**消费者（原版那一条挂在
        /// `Card3DAnimationController` 上）；留着是为了将来接动画链」—— **已过期**：那一条**已经接上了**，
        /// `Battle/Card3DAnimationController.cs:254-255`（照原版 `-=` 再 `+=`，防重复挂）与
        /// `:301`（`Detach` 时摘回调）**都在用**它 ⇒ 生产**有**消费者，而且就是原版那一条。</summary>
        public Action<bool> OnFlyingToggleFinish;

        /// <param name="t">要抬的那张卡（= 原版挂 `FlyingCardController` 的那个对象；它自己的 `localPosition`
        /// 被逐帧改写）。判据：原版 `Awake` 缓存的就是**自己的** `localPosition`。</param>
        public FlyingCardController(Transform t)
        {
            _t = t;
            if (_t != null) _home = _t.localPosition;
        }

        public States State { get { return _state; } }
        /// <summary>原版 `IsFlying()`：`currentState != Landed`（含两个过渡态）。</summary>
        public bool IsFlying { get { return _state != States.Landed; } }
        public Vector3 Home { get { return _home; } }
        public float Progress { get { return _time; } }

        /// <summary>更新「原地不动时该在哪儿」（原版是 `Awake` 缓存一次；我们是**每次摆位都更新** ——
        /// 卡换格 / 让位之后浮动要跟到新位置，否则会飘回旧格）。</summary>
        public void SetHome(Vector3 home)
        {
            _home = home;
            if (_state == States.Landed && _t != null) _t.localPosition = home;
        }

        /// <summary>原版 `Toggle(bool option, float time = 0.4)`（逐句见文件头）。</summary>
        public void Toggle(bool option, float time = DefaultTransitionTime)
        {
            if (time < 0f)
            {
                Debug.LogWarning($"[FlyingCardController] `Toggle` 的过渡时长是负数（{time}）⇒ 原版 "
                               + "`CustomDebug.LogWarning` 之后归零（`FlyingCardController__Toggle.c:14-23`）");
                time = 0f;
            }
            _desiredChangeTime = time;
            ChangeState(time == 0f
                ? (option ? States.Flying : States.Landed)
                : (option ? States.InTransitionToFly : States.InTransitionToLand));
        }

        /// <summary>原版 `ChangeState`（逐句见文件头）。</summary>
        void ChangeState(States next)
        {
            if (next == _state) return;

            if (next == States.InTransitionToFly)
            {
                if (_state == States.Flying) return;
                _time = 0f;
                _state = States.InTransitionToFly;
            }
            else
            {
                if (next == States.InTransitionToLand && _state == States.Landed) return;
                _time = 0f;
                _state = next;

                if (next == States.Landed)
                {
                    if (_t != null) _t.localPosition = _home;
                    if (OnFlyingToggleFinish != null) OnFlyingToggleFinish(false);
                    return;
                }
                if (next == States.InTransitionToLand)
                {
                    if (_t != null) _from = _t.localPosition;
                    _target = _home;
                    return;
                }
                if (next != States.Flying)
                    throw new ArgumentOutOfRangeException(nameof(next));

                // States.Flying（`time == 0` 那一路）
                if (OnFlyingToggleFinish != null) OnFlyingToggleFinish(true);
                return;
            }

            // InTransitionToFly 的收尾（原版 state 2 与 state 3 共用这一段）
            if (_t != null)
            {
                _target = FlyPosition();
                _from = _t.localPosition;
            }
        }

        /// <summary>原版 `GetFlyPosition()` = `originalLocalPosition` + (0, `initialHeightOffset`
        /// + `flyingHeight × curve(currentTime)`, 0)。</summary>
        Vector3 FlyPosition()
        {
            var p = _home;
            p.y += InitialHeightOffset + FlyingHeight * EvaluateCurve(_time);
            return p;
        }

        /// <summary>原版 `Update`（逐句见文件头）。由 `BattleDriver.AdvanceTimeline` 每帧推。</summary>
        public void Update(float dt)
        {
            if (_t == null) return;
            switch (_state)
            {
                case States.Landed:
                    return;

                case States.InTransitionToLand:
                    if (GoToTargetPosition(dt) && _state != States.Landed)
                    {
                        _time = 0f;
                        _state = States.Landed;
                        _t.localPosition = _home;
                        if (OnFlyingToggleFinish != null) OnFlyingToggleFinish(false);
                    }
                    return;

                case States.InTransitionToFly:
                    if (GoToTargetPosition(dt) && _state != States.Flying)
                    {
                        _time = 0f;
                        _state = States.Flying;
                        if (OnFlyingToggleFinish != null) OnFlyingToggleFinish(true);
                    }
                    return;

                case States.Flying:
                    _time += dt * AnimationSpeed;
                    {
                        var p = _t.localPosition;
                        // ⚠️ **只写 y**（x/z 留给别的补间）—— 见文件头「我们改的两处」②。
                        p.y = _home.y + InitialHeightOffset + FlyingHeight * EvaluateCurve(_time);
                        _t.localPosition = p;
                    }
                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>原版 `GoToTargetPosition`：推进过渡、写位置、**按写之前那个位置**判到达
        /// （距离² ≤ <see cref="ArriveEpsilonSq"/>）。</summary>
        bool GoToTargetPosition(float dt)
        {
            _time += dt;
            float t01 = _desiredChangeTime <= 0f ? 1f : _time / _desiredChangeTime;
            if (t01 < 0f) t01 = 0f; else if (t01 > 1f) t01 = 1f;

            Vector3 before = _t.localPosition;
            _t.localPosition = Vector3.Lerp(_from, _target, t01);

            float dx = _target.x - before.x;
            float dy = _target.y - before.y;
            float dz = _target.z - before.z;
            if (dx * dx + dy * dy + dz * dz > ArriveEpsilonSq) return false;

            _t.localPosition = _target;
            return true;
        }

        // ── 曲线 ────────────────────────────────────────────────────────────────
        //  原版 `yMovementCurve` 的 4 个键（`MonoBehaviour_2087276408211151808.json` 逐值抄）：
        //    t=0    v=0.0002288818359375  inSlope=outSlope=5.856887340545654
        //    t=0.25 v=1.0                 inSlope=outSlope=0.0
        //    t=0.75 v=-1.0                inSlope=outSlope=0.022920146584510803
        //    t=1    v=0.0                 inSlope=outSlope=5.56168270111084
        //  `weightedMode` 全 0（None）⇒ `inWeight/outWeight` 不参与（原版保存了值但不生效）。
        const float K0V = 0.0002288818359375f;
        const float K0S = 5.856887340545654f;
        const float K1V = 1.0f;
        const float K1S = 0.0f;
        const float K2V = -1.0f;
        const float K2S = 0.022920146584510803f;
        const float K3V = 0.0f;
        const float K3S = 5.56168270111084f;

        static AnimationCurve _curve;
        static readonly object CurveLock = new object();

        /// <summary>原版那条曲线（懒建一次）。**用引擎自己的 `AnimationCurve`** —— 求值语义
        /// （Hermite + 两条 wrap 模式）逐位等于原版，⛔ 别自己手写一份「差不多」的。</summary>
        static AnimationCurve Curve
        {
            get
            {
                if (_curve != null) return _curve;
                lock (CurveLock)
                {
                    if (_curve != null) return _curve;
                    var c = new AnimationCurve(
                        new Keyframe(0f, K0V, K0S, K0S),
                        new Keyframe(0.25f, K1V, K1S, K1S),
                        new Keyframe(0.75f, K2V, K2S, K2S),
                        new Keyframe(1f, K3V, K3S, K3S));
                    c.preWrapMode = WrapMode.Loop;      // 原版 `m_PreInfinity = 2`
                    c.postWrapMode = WrapMode.Once;     // 原版 `m_PostInfinity = 1` ⇒ 第一轮走完停在末值（0）
                    _curve = c;
                }
                return _curve;
            }
        }

        /// <summary>`yMovementCurve.Evaluate(t)`。`t &gt; 1` ⇒ 停在末键值 0（`postWrap = Once`）。</summary>
        public static float EvaluateCurve(float t)
        {
            return Curve.Evaluate(t);
        }

        /// <summary>`Flying` 态在进度 <paramref name="t"/> 处相对 `home.y` 的偏移（自检/诊断用；
        /// = <see cref="InitialHeightOffset"/> + <see cref="FlyingHeight"/> × curve）。</summary>
        public static float HeightOffsetAt(float t)
        {
            return InitialHeightOffset + FlyingHeight * EvaluateCurve(t);
        }
    }
}
