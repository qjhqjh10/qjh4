// WarpforgeParticleForceFieldController.cs — 原版 `ParticleForceForceFieldController` 的对应物。
//
// 语义出处（**反编译方法体 + 字段桩**）：
//   · `d:/2/Warpforge_code/Scripts/Assembly-CSharp/ParticleForceForceFieldController.cs`（签名桩）：
//       [SerializeField] private ParticleSystemForceField forceField;
//       [SerializeField] private float force;
//       private ParticleSystem.MinMaxCurve minMaxCurve;
//   · `d:/2/tools/decomp_full/ParticleForceForceFieldController__Update.c`：
//       （把 `minMaxCurve` 这个结构的两处 float 写成 `force`）
//       `if (forceField != null) forceField.gravity = minMaxCurve;`
//     —— 反编译把两次 float setter 的符号名猜成了 `HableCurve.set_x1` / `Keyframe.set_outWeight`，
//        但两次调用的**实参都是 `force`、第一个参数都是 `minMaxCurve` 的地址**（`param+0x30`），
//        结构随后被整块（32 字节）赋给 `gravity` ⇒ 语义 = **`gravity = MinMaxCurve(force)`**。
//
// 为什么需要它（**这是 `StripMissingScripts` 删掉的第三种组件**）：
//   `Will of Grok` 那条 legacy clip 里有 `attribute: force`、`path: ""`（classID 114）的曲线，
//   原版靠这个组件接收；bundle 里它的 `m_Script` 解析不到 ⇒ 导入时被整条删掉 ⇒ **曲线没有接收者**。
//   本类 + `EffectExporter.AttachForceFieldControllers`（重导时补挂）+ `ImportClip` 的
//   `RemapDeadScriptCurves(..., { "force" }, ...)` 三件合起来才把它接回。
//
// ⚠️ 与 `WFModulePostProcess` 不同：**这不是 AnimFX 模块**，原版就是挂在 prefab 上的普通组件
//   （`Modules` 数据里没有它），所以不进 `WFModuleFactory` 的 kind 表。
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>把一个 `float` 逐帧写进 `ParticleSystemForceField.gravity`（原版 `Update` 的全部内容）。</summary>
    [DisallowMultipleComponent]
    public class WarpforgeParticleForceFieldController : MonoBehaviour
    {
        [Tooltip("原版 `forceField`（原版 `Will Of Gork` 上是 `Force Field` 子物件那个组件）")]
        public ParticleSystemForceField forceField;

        [Tooltip("原版 `force` —— 由 legacy clip 的 `force` 曲线驱动（classID:114, path \"\"）")]
        public float force;

        /// <summary>原版那个 `private ParticleSystem.MinMaxCurve minMaxCurve;`（每帧复用同一份）。</summary>
        ParticleSystem.MinMaxCurve _minMaxCurve;

        void Update()
        {
            if (forceField == null)
            {
                // 原版这里是直接解引用（空就抛 NullReference）；我们出声 + 返回（本项目：不许静默，也不崩对局）
                Debug.LogWarning($"[WarpforgeVFX] `{name}` 的 WarpforgeParticleForceFieldController 没有绑定 "
                               + "`ParticleSystemForceField` ⇒ 这条 `force` 曲线没有效果");
                return;
            }
            _minMaxCurve.mode = ParticleSystemCurveMode.Constant;
            _minMaxCurve.constant = force;          // = 原版第一处 float setter（constant 同时写 min/max）
            _minMaxCurve.curveMultiplier = force;   // = 原版第二处 float setter
            forceField.gravity = _minMaxCurve;
        }
    }
}
