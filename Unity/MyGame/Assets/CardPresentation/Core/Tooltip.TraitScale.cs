// Tooltip.TraitScale.cs — `B143-W5`（2026-11-05）· 活状态行 `B142-TRAIT-SCALE`
// ============================ 它补的是什么 ============================
//  原版 `Trait Tooltip`（关键词 tooltip 那一版式）**根节点上挂着一颗 `TransformScalerBySmallScreenUI`，
//  `menuScale = 1.25`**；而基础的 `BasicToolTip` **没有这颗组件**。⇒ 小屏档开着时，trait tooltip 整颗放大 1.25，
//  普通 tooltip 一个像素都不动。
//
// ============================ 判据（逐字段，2026-11-05 `B143-R2` 现读）============================
//  判据全文 → `资料/完全复刻/判据/B143_四条待判据.md` §3。要点：
//   · `Trait Tooltip.prefab`：根 `m_LocalScale (1,1,1)`、全部子节点也 (1,1,1) ⇒ **唯一缩放源 = 根上那颗组件**；
//     组件 `menuScale = 1.25`（bundle `MonoBehaviour_5790907548712612281.json` 同值）。
//   · `BasicToolTip.prefab`：**0 处**该组件（两个源都没有）⇒ 基础档恒 (1,1,1)。
//   · `TransformScalerBySmallScreenUI.Initialize`（反编译全文）= `menuScale != 1 && GameStaticData.smallScreenUI`
//     ⇒ **没有**宽/高/dpi 判定；`LateUpdate` = 当前 `localScale × menuScale`（0.0001 守卫 + 回读）。
//   · 组件本体我们**早已逐句复刻**（`Shell/TransformScalerBySmallScreenUI.cs`）⇒ 本件**一个字都不改它**。
//
// ============================ 为什么是 partial 文件（不是加进 Tooltip.cs）============================
//  `账/*.tsv` 有 58 处按行号引用 `Core/Tooltip.cs`，其中三处在 `TipText`（`:1161` / `:1188` / `:1329`，都在
//  `Tooltip` 类**之后**）⇒ 往 `Tooltip` 里插一行，那三条就整体漂。按本仓纪律（「热点文件走新 partial 文件，
//  ⛔ 别插进既有成员之间」）⇒ `Tooltip` 改成 `partial`（那一行**零行数变化**），新成员全放本文件。
//
// ============================ 🔴 与施工单的一处**就地订正**（铁律 5）============================
//  施工单 §3·4 步骤 2 写的是「`Initialize(); Tick();`」**无条件**调 `Tick()`。**不成立**：
//  组件自己的注释与实现都写着「本函数**不自己判 `enabled`**（Unity 只在启用时调 `LateUpdate`）」⇒
//  开关**关着**时无条件 `Tick()` 会把 1.25 乘上去（正是「小屏关着也放大」那个 bug）。
//  ⇒ 这里按原版分支：**`enabled` 才 `Tick()`**，否则把根复位成 (1,1,1)。
using UnityEngine;

namespace CardPresentation
{
    public partial class Tooltip
    {
        /// <summary>原版 `Trait Tooltip.prefab` 根上那颗 `TransformScalerBySmallScreenUI.menuScale`（**序列化值**，两源一致）。
        /// ⛔ 别改成 1（改了 ⇒ 小屏档下 trait tooltip 不再放大，断言 `B143_W5` A1 当场红）。</summary>
        public const float TraitMenuScale = 1.25f;

        /// <summary>根上的缩放器（原版 `Trait Tooltip` 有、`BasicToolTip` 没有 —— 我们两档共用**同一只** tooltip 实例，
        /// 所以组件常挂、靠 `menuScale` 1 / 1.25 切换来区分两档）。`null` = 还没按需挂过。</summary>
        TransformScalerBySmallScreenUI _scaler;

        /// <summary>trait 档 ⇒ 根 ×1.25（且**小屏开关开着**）；其它 ⇒ 回 (1,1,1) 并停用组件。
        /// <para>✅ 已复刻（2026-11-05 · B143-W5 · 判据 = `Trait Tooltip.prefab` 根组件 `menuScale 1.25` +
        /// `TransformScalerBySmallScreenUI__Initialize.c` / `__LateUpdate.c` 两份全文）。</para>
        /// <para>⚠️ **每次进/出 trait 档都重判一次**（幂等）：原版每次弹 tooltip 都是 `Instantiate` 一份新 prefab
        /// ⇒ 每次 `OnEnable → Initialize()` 都会重读开关；我们是**常驻单例**，只在「第一次进 trait 档」做一次的话，
        /// 用户中途改开关会留下过期状态。</para></summary>
        void ApplyTraitScale(bool trait)
        {
            if (_scaler == null)
                _scaler = GetComponent<TransformScalerBySmallScreenUI>() ?? gameObject.AddComponent<TransformScalerBySmallScreenUI>();
            _scaler.menuScale = trait ? TraitMenuScale : 1f;
            _scaler.Initialize();                     // = 原版 `OnEnable → Initialize()`：`menuScale != 1 && smallScreenUI`（裸 `!=`）
            if (_scaler.enabled)
            {
                // = 原版 `LateUpdate` 的那一拍。批处理里**没有帧循环** ⇒ 显式跑一次；
                //   组件自己带 0.0001 守卫 ⇒ 已经是 1.25 时再调是空操作（幂等）。
                _scaler.Tick();
            }
            else if (transform.localScale != Vector3.one)
            {
                // 开关关着 / 回到基础档 ⇒ 原版那颗 prefab 上**根本没有**这颗组件 ⇒ 恒 (1,1,1)。
                transform.localScale = Vector3.one;
            }
        }

        // ==================================================================
        //  自检读口（只读；⛔ 不参与渲染）
        // ==================================================================

        /// <summary>自检读口：tooltip 根节点这一刻的 `localScale.x`（= 原版 prefab 根的 `m_LocalScale` 那一档）。
        /// 没有实例 ⇒ `NaN`（⛔ 别当 1 用：那会让「缩放没生效」的断言假绿）。</summary>
        public static float RootScaleForTest { get { return _inst == null ? float.NaN : _inst.transform.localScale.x; } }

        /// <summary>自检读口：根上那颗缩放器此刻 `enabled` 吗（= 原版 `menuScale != 1 && smallScreenUI`）。
        /// 没有实例 / 还没按需挂 ⇒ `false`。</summary>
        public static bool TraitScalerEnabledForTest { get { return _inst != null && _inst._scaler != null && _inst._scaler.enabled; } }

        /// <summary>自检读口：缩放器上这一刻的 `menuScale`（trait 档 1.25 / 其它 1；没有实例 ⇒ `NaN`）。</summary>
        public static float TraitMenuScaleForTest
        {
            get { return (_inst != null && _inst._scaler != null) ? _inst._scaler.menuScale : float.NaN; }
        }
    }
}
