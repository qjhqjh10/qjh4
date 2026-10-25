// TraitEffectController.cs — 原版 `TraitEffectController`（抽象基类，全文 17 行）
//
// 判据正本 → `Unity/资料/完全复刻/判据/TraitEffectController_钩子层.md`（§一 / §三 / §六·1 / §六·2）
// 原版一手出处（**逐字读过全文**）：
//   `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/TraitEffectController.cs`
//     :3    public abstract class TraitEffectController : MonoBehaviour
//     :5    protected int currentValue;
//     :7-9  protected virtual void Start() { }
//     :11   public abstract void Toggle(bool option);
//     :13   public abstract void UpdateValue(int newValue);
//     :15   public abstract void AimingAttack(bool option);
//
// 这一层是什么（判据 §1.1 / §1.2）：
//   · 它**不是 new 出来的**，是 **prefab 上的序列化组件** —— 随 `Instantiate` 一起出生
//     （`BattleManager__CreateAnim.c:62-63`），随状态框被 `Destroy` 一起消失。
//   · 全 build **只有两个实现**：`HuntMarkController` / `MarkerlightController`；
//     这两个脚本**各只挂在 1 个 prefab 上**（判据 §1.3 的全库唯一性自证：84 个 bundle 按
//     `m_Script` pathID 搜，各命中 1 个文件 ⇒ `HuntMark_IdleEffect` / `MarkerlightIdle`）。
//   · 输入 = **当前 trait 总值**（`EntityScript.GetCurrentTraitValueWithModifiers`：基值 + Σmodifier，钳 ≥0）；
//     输出 = **只有 UI**（prefab 上那个 TMP 的 `text` + `DOPunchScale` 弹一下）
//     ⇒ ⛔ 不写引擎状态、不改 trait 值、不发事件、不重算光环（判据 §3.3）。
//
// ⚠️ 两个「按形状保留、⛔ 别赋予语义」的东西（判据 §1.2 / §八·A）：
//   · `currentValue`：本 build **没有任何一处读或写**它（两个子类的 `UpdateValue` 用的都是入参 `newValue`；
//     反编译 `HuntMarkController__UpdateValue.c:13-30` 读的是 `param_2`）⇒ **死字段**。
//   · `Toggle(bool)`：本 build **0 个调用点** —— 三处 `GetComponent<TraitEffectController>` 后续调的都是
//     vtable 槽 `0x198`（= `UpdateValue`）；两个子类的实现**都是空体**；两个 prefab 的 UnityEvent
//     也**没有** `m_MethodName`（§八·A 逐条自证）⇒ 子类写成空方法即可。
//
// ⚠️ 本文件里还有 `TraitEffectControllers`（静态挂载助手，判据 §6.2）：它**不是 MonoBehaviour**，
//    不受「类名 == 文件名」约束（CLAUDE.md A1506/A1508）；放在基类同一个文件里是有意的。
//    ✅ **已接线**：`TraitFrames.Play(...)` 在每张状态框出生时调
//    `TraitEffectControllers.Attach(p.gameObject, prefabName)`（`Core/TraitFrames.cs` 的 `Play()` 里，W0）。
//    🔴 **就地订正（2026-10-25 · R5 · 铁律 5）**：本行原来写「本轮（N15）只**建文件**、⛔ 不接线：没有任何既有文件会调它」——
//    那是 N15 当时的实况，**W0 接线之后就不成立了**。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// 原版 `TraitEffectController`（`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/TraitEffectController.cs`）。
    /// 「某条 trait 的计数器 / 状态框」的抽象钩子：框出生时与再次显示时，把该 trait 的**当前总层数**写上去。
    /// ⛔ 它只管 UI；⛔ 它不是新造的第二条「trait → prefab」绑定路径（那条唯一来源仍
    /// = `Resources/trait_effect_bindings.json`，由 `TraitFrames` 管）。
    /// </summary>
    public abstract class TraitEffectController : MonoBehaviour
    {
        /// <summary>原版 `:5` —— 本 build **无读无写**（死字段），保留形状；⛔ 别拿它当状态。</summary>
        protected int currentValue;

        /// <summary>原版 `:7-9`（空实现的虚方法）—— `MarkerlightController` 覆写了它做状态初始化。</summary>
        protected virtual void Start() { }

        /// <summary>原版 `:11` —— 本 build **0 调用点**（判据 §八·A）；两个子类都是空体。</summary>
        public abstract void Toggle(bool option);

        /// <summary>原版 `:13` —— 本层的全部意义：把「这条 trait 现在是几」写进 prefab 上的计数器。</summary>
        public abstract void UpdateValue(int newValue);

        /// <summary>原版 `:15`。两个实现的处境不同：
        /// `HuntMarkController` **直接 `throw NotImplementedException`**（原版就是这样，照抄）；
        /// `MarkerlightController` 走 `MaterialFader.Toggle(!option, fadeTime)`（`MarkerlightController.cs:81-86`）——
        /// ✅ **已实现**：`Core/MaterialFader.cs` 这个共用件与两个 `MaterialFader` 实例都已建好
        /// （2026-10-25 · R5 · W10）。🔴 **就地订正（铁律 5）**：本段原来写「本工程还没有 `MaterialFader` 共用件
        /// ⇒ 走明确的『未实现』出口」——那是建共用件之前的口径，**已过期**。
        /// ⚠️ 调用点 = `Battle/BattleDriver.cs` 的 `AimAttackingStatusPair`（W10 ③：`PlaySignal` 的
        /// `EvtKind.Attack` 那一拍起手、同拍排一条「攻方动作跑完」收手；原版两拍 =
        /// `CardScript.cs:3649` `hideStatus: true` / `:3637` `hideStatus: false`）。</summary>
        public abstract void AimingAttack(bool option);
    }

    /// <summary>
    /// 「哪个 prefab 上该挂哪个控制器」的**唯一判据**（判据 §6.2）。
    ///
    /// 🔴 **按 prefab 名挂，⛔ 绝不按 trait 名挂**：全 build 只有这两个 prefab 带控制器
    /// （判据 §1.3：84 个 bundle 按两个脚本的 `m_Script` pathID 扫，**各命中 1 个实例**）。
    /// 按 trait 名挂会把组件加到别的状态的框上（判据 §6.4 的「灭自证 2」就是挡这个）。
    ///
    /// 📌 **谁该调它（下一批，本批不接）**：`TraitFrames.Play(prefabName)`（`:964-975`）——
    /// 那是所有状态框出生的**唯一**入口，见判据 §6.2。
    /// </summary>
    public static class TraitEffectControllers
    {
        /// <summary>
        /// 给刚实例化出来的状态框补上控制器组件。
        /// ⚠️ 幂等：实例上**已经有** `TraitEffectController` 就直接返回它
        /// （将来重导出 prefab 若把组件带进来，⛔ 别重复 `AddComponent` 出一份新的）。
        /// </summary>
        /// <param name="go">`WarpforgeEffectPlayer.Play(...)` 返回的那个实例的根 GameObject（可为 null）。</param>
        /// <param name="prefabName">效果库里的 prefab 名（**判据只认这两个字符串**，不是 trait 名）。</param>
        public static TraitEffectController Attach(GameObject go, string prefabName)
        {
            if (go == null || string.IsNullOrEmpty(prefabName)) return null;

            TraitEffectController exist = go.GetComponent<TraitEffectController>();
            if (exist != null) return exist;

            switch (prefabName)
            {
                // 判据 §1.3：`HuntMark_IdleEffect` 的根 = `Transform` + `HuntMarkController`
                case "HuntMark_IdleEffect": return go.AddComponent<HuntMarkController>();
                // 判据 §1.3：`MarkerlightIdle` 的根 = `Transform` + `MarkerlightController`
                case "MarkerlightIdle": return go.AddComponent<MarkerlightController>();
            }
            return null;   // 别的 prefab 本来就不带控制器（全库唯一性）⇒ 不挂、也不出声
        }
    }
}
