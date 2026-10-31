// CardDisplayLevelUpPartial_B111F1C.cs — 原版 `ParentConstraint`（`2DCard/Front` 粘在 `Level Up Effect` 上）的**等价物**
// （B-70 · 2026-11-01 · `B111_F1C`）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【判据三源（2026-11-01 全部现读）】
//
//  ① clip（`Level Up Card`）：
//     `d:/2/tools/assetripper_out/ExportedProject/Assets/AnimationClip/Level Up Card.anim`
//     （= `bundle_scenes_scenes_mainmenuwarpforge/AnimationClip/AnimationClip_10.json`，同一支）
//     两条 float 矩形曲线，`path: 2DCard/Front` · `classID: 114` · `script guid 33b13aa6e39ef67ef8b281abcbbf1244`：
//        `inheritPosition`：t=0 → **1**，t=1.45 → **0**；`inSlope/outSlope = Infinity`、`inWeight/outWeight = 0`
//        `inheritRotation`：同上（同一对键）
//     ⚠️ 无限斜率 + 零权重 = **阶跃**（先一直保持 1，到 1.45 直接跳 0），**不是**线性淡出 ⇒ ⛔ 别写成 Lerp。
//     ⚠️ 1.45 正是同一 clip 的动画事件 `ChangeFrame` 的时刻（`m_Events`，另一条 3.4833 = `AnimationFinished`）。
//     ⚠️ clip 里**没有** `inheritScale` 的曲线。
//
//  ② 实例（主菜单场景 `MainMenu Warpforge.unity`）：
//     `Card Displayer Menu For Menu/Card Display/Cards/` 下 5 张卡里 **只有 `CardUI`（无后缀那张）** 的
//     `2DCard/Front` 挂了 `ParentConstraint`（`CardUI (1)…(4)` 的 `Front` 只有 RectTransform）：
//        targetParent = `2DCard/Level Up Effect` · `inheritPosition 1` · `inheritRotation 1` · **`inheritScale 0`**
//        · `updateMethod 0`（= **Update**）· `alignAxis 0`（= **X 轴 = right**）· `autoInit 1`
//     （另一处实例在 3D 卡 `CardPrefab/Board Elements/EffectAnchor`，与本条无关。）
//
//  ③ 方法体（`d:/2/tools/decomp_full/ParentConstraint__{Awake,Init,Update,LateUpdate,Sync,ToggleParentingOptions}.c`）：
//     `Init(target)`（`Awake` 里 `autoInit` 就调）：`childLocalPos = target.InverseTransformPoint(transform.position)`；
//        `childRotOffset = FromToRotation(target.<axis>, transform.<axis>)`（`<axis>` 由 `alignAxis` 选 X/Y/Z）；
//        `scaleOffset = transform.localScale / target.localScale`（逐分量）。
//     `Sync()`：`if (inheritPosition) transform.position = target.TransformPoint(childLocalPos);`
//               `if (inheritRotation) transform.rotation = target.rotation * childRotOffset;`
//               `if (!inheritScale) return; transform.localScale = target.localScale * scaleOffset;`
//     `Update()`：`if (!initialized) Init(targetParent); if (updateMethod == 0) Sync();`
//
//  ⇒ **语义 = 「把子节点的世界位置/旋转钉在目标节点的坐标系上，偏移量取 Init 那一刻的相对位姿」**，
//     0 → 1.45 s 生效，1.45 s 起不再写（子节点停在最后一帧的世界位姿）。
//     目标自己动（缩放/位移/旋转）时子节点跟着动；目标不动时是**恒等**（不会跳）。
//
// 【为什么只做位置 + 旋转，不做缩放】
//   本实例 `inheritScale = 0`，而且 clip 根本不驱动它 ⇒ 缩放那一支在原版这条链上**永远不执行**
//   （铁律 11 例外①：原版这条分支在这个实例上不成立）。⛔ 别为了「组件完整」补一条没人走的死支。
//
// 【我们这边的落点与接线】
//   · `Front` 与 `Level Up Effect` 两个节点都**不在本组件手上**（`Front` 由 `Core/CardView` 建、
//     `Level Up Effect` 在那棵展示窗里）⇒ 由接线方赋值，与既有的 `pulseTarget` 同一约定：
//     **没接线 ⇒ 出声一次、不静默、不作用**（⛔ 不是静默跳过）。
//   · 时钟 = 本组件自己的 `_inheritT`（`DoAnim` 起、`Tick` 推），⛔ 不另开 Update —— 与 `StepFade`/`StepShake`/`StepPulse` 同一条口径。
//   · 抓偏移量的时刻：原版在 `Awake`（`autoInit = 1`）；我们在**第一次 `DoAnim`**（那时接线才可能到位，
//     同 `PlayShake` 里 `_shakeBaseOf != shakeTarget` 才重取基准的做法）。两者等价的条件 = 「卡从建出来到开播之间
//     两个节点没被挪过」—— 这对静态 prefab 成立；`Front` 的局部位置是常量（`FrontAposY = 0.08`）。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace CardPresentation
{
    public partial class CardDisplayLevelUp
    {
        // ================================================================== 序列化形状（照原版 `ParentConstraint`）

        [Header("原版 ParentConstraint（2DCard/Front ← Level Up Effect；0 → 1.45 s，位置+旋转，不继承缩放）")]
        /// <summary>原版 `2DCard/Front`（被钉的那一个；`Core/CardView` 的 `Front` 层）。</summary>
        public Transform inheritChild;
        /// <summary>原版 `2DCard/Level Up Effect`（钉到它身上）。</summary>
        public Transform inheritTarget;

        /// <summary>钉住的截止时刻（秒）= clip 里那两条曲线阶跃到 0 的时刻 = `ChangeFrame` 事件时刻。
        /// **判据**：`Level Up Card.anim` 的 `inheritPosition/inheritRotation` 两个键 `t=0 → 1`、`t=1.45 → 0`
        /// （`inSlope/outSlope = Infinity` ⇒ 阶跃），与 `m_Events` 的 `ChangeFrame@1.45` 同刻。</summary>
        public const float InheritEndTime = ClipChangeFrameTime;

        /// <summary>本组件对应的 `alignAxis`（原版场景实例 = 0 = X 轴 = `Transform.right`）。
        /// 原版 `Init` 的 `childRotOffset = FromToRotation(target.<axis>, transform.<axis>)` 用的就是它。</summary>
        public const int InheritAlignAxis = 0;

        // ================================================================== 状态

        Vector3 _inheritLocalPos;                       // 原版 `childLocalPos`
        Quaternion _inheritRotOffset = Quaternion.identity;   // 原版 `childRotOffset`
        Transform _inheritCapturedChild, _inheritCapturedTarget;
        bool _inheritCaptured;
        float _inheritT;
        bool _inheritRunning;
        bool _warnedNoInherit;

        // ================================================================== 读数（自检用；不参与行为）

        /// <summary>现在是不是处于「钉住」窗口（`DoAnim` 起、1.45 s 止）。</summary>
        public bool InheritActive { get { return _inheritRunning; } }
        /// <summary>`ApplyInherit` 真正写过的次数（删掉那一跳 ⇒ 恒 0）。</summary>
        public int InheritAppliedCount { get; private set; }
        /// <summary>`Init` 抓到的那一份偏移（`target` 局部空间的子节点位置）—— 断言用它算期望值。</summary>
        public Vector3 InheritLocalOffset { get { return _inheritLocalPos; } }
        /// <summary>`Init` 抓到的旋转偏移（原版 `Quaternion.FromToRotation(target.right, child.right)`）。</summary>
        public Quaternion InheritRotOffset { get { return _inheritRotOffset; } }
        /// <summary>本窗口已推进的秒数（`Tick` 累加）。</summary>
        public float InheritTime { get { return _inheritT; } }
        /// <summary>偏移量是否已经抓过（诊断用）。</summary>
        public bool InheritCaptured { get { return _inheritCaptured; } }

        // ================================================================== 行为（原版 Init / Sync / Update）

        /// <summary>`DoAnim` 第 ⑦ 跳（原版不在 `DoAnim` 里 —— 它是 **clip 曲线**在 0 时刻把两个 bool 置 1；
        /// 我们没法用 clip（`Animation` 挂在窗根，见类尾那条同族理由）⇒ 在开播同一刻显式开窗）。
        /// 没接线 ⇒ 出声一次并**不开窗**（⛔ 不静默）。</summary>
        public void BeginInherit()
        {
            _inheritT = 0f;
            _inheritRunning = false;
            if (inheritChild == null || inheritTarget == null)
            {
                _warnedNoInherit = WarnOnce(_warnedNoInherit, "[CardDisplayLevelUp] `inheritChild`/`inheritTarget` 没接线 ⇒ 原版 "
                    + "`Level Up Card` clip 的 `2DCard/Front · inheritPosition/inheritRotation`（0 → 1.45 s 把卡面钉在 "
                    + "`Level Up Effect` 上）**不作用**（应传 `CardView` 的 Front 层 + `Level Up Effect` 节点）");
                return;
            }
            EnsureInheritOffset();
            _inheritRunning = true;
            ApplyInherit();          // t=0 立刻钉一帧（原版曲线 t=0 就是 1）
        }

        /// <summary>推一帧（`Tick` 调）。到 `InheritEndTime` 就**停止写**（原版那两个 bool 阶跃到 0）。</summary>
        public void StepInherit(float dt)
        {
            if (!_inheritRunning) return;
            if (dt > 0f) _inheritT += dt;
            if (_inheritT >= InheritEndTime) { _inheritRunning = false; return; }
            ApplyInherit();
        }

        /// <summary>提前收尾（`AnimationFinished` 兜一次；正常路径在 1.45 s 就自己停了）。</summary>
        public void StopInherit() { _inheritRunning = false; }

        /// <summary>原版 `ParentConstraint.Init(target)`：抓「子节点在目标局部空间的位姿」与「绕对齐轴的角度差」。
        /// 引用一变就重抓（同 `PlayShake` 里 `_shakeBaseOf != shakeTarget` 才重取基准）。</summary>
        void EnsureInheritOffset()
        {
            if (_inheritCaptured && _inheritCapturedChild == inheritChild && _inheritCapturedTarget == inheritTarget) return;
            _inheritCapturedChild = inheritChild;
            _inheritCapturedTarget = inheritTarget;
            _inheritLocalPos = inheritTarget.InverseTransformPoint(inheritChild.position);
            _inheritRotOffset = Quaternion.FromToRotation(AxisOf(inheritTarget), AxisOf(inheritChild));
            _inheritCaptured = true;
        }

        /// <summary>原版 `ParentConstraint.Sync()` 的**位置 + 旋转**两支（缩放支本实例不走，见文件头）。</summary>
        void ApplyInherit()
        {
            if (inheritChild == null || inheritTarget == null) return;
            EnsureInheritOffset();
            inheritChild.position = inheritTarget.TransformPoint(_inheritLocalPos);
            inheritChild.rotation = inheritTarget.rotation * _inheritRotOffset;
            InheritAppliedCount++;
        }

        /// <summary>原版 `alignAxis` 选轴（0 = X / 1 = Y / 2 = Z ⇒ right / up / forward）。
        /// ⚠️ 写成三元式而不是 `if`：`InheritAlignAxis` 是 `const`，`if (const == 1) return …` 会被编译器判「不可达语句」（CS0162）。</summary>
        static Vector3 AxisOf(Transform t)
        {
            return InheritAlignAxis == 1 ? t.up : (InheritAlignAxis == 2 ? t.forward : t.right);
        }

        /// <summary>「只出声一次」的小工具（同类警告在本文件已有一串 `_warnedXxx` 字段，形状照抄）。</summary>
        static bool WarnOnce(bool already, string message)
        {
            if (!already) Debug.LogWarning(message);
            return true;
        }
    }
}
