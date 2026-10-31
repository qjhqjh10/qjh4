// CardDisplayWindowPartial_B111F1E.cs — B-70「选项 b」：把 `ParentConstraint` 等价物的两个接线口
// 接在**同时拥有 `Front` 与 `Level Up Effect` 的那棵树**上（`B111_F1E` · 2026-11-01）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【为什么从 `Shell/CardDetailPopup` 搬到这扇窗（调度台 2026-11-01 裁定 = 选项 b）】
//   ① 原版那支 clip 的**宿主就是展示卡本身**：`MainMenu Warpforge.unity` 里挂着 `Level Up Card` 的
//      `Animation` 组件在 GameObject &88 = `…/Cards/CardUI`（不是 `Upgrade Panel`）
//      ⇒ clip 里 `path: 2DCard/Front` / `2DCard/Level Up Effect` 是**相对这张展示卡**解析的。
//   ② 而 `Level Up Effect` 这个节点在我们工程里**只存在于本窗**（`CardDisplayEffects.Build` 的唯一调用点 =
//      `CardDisplayWindow.cs:646`）。
//   ⇒ 升级弹窗（`CardDetailPopup`）那棵树**天生接不到目标**（`inheritTarget` 恒 null，已如实记在 `B111_F1D`）；
//     真正能接上的只有这里 —— 本文件就是那半个。
//
// 【本文件做什么 / 不做什么】
//   · 做：确保本窗有一个 `CardDisplayLevelUp`（`LevelUpFx`），并把
//         `inheritChild` = **主卡（槽 0）** 的 `2DCard/Front`（`CardView.FaceContentRoot`）、
//         `inheritTarget` = 光效层里的 `Level Up Effect` 绑上去；每次 `BuildFan` 之后重绑一次
//         （卡会被重建 ⇒ `Front` 换引用）。
//   · 不做：**不自动播**（本窗不调 `DoAnim`）。原版这一跳由 clip 曲线驱动，本窗的 `DoAnim` 由**升级流程**决定；
//          「谁来播」是另一件事，见 `报告/B111_F1D.md` §没查清。
//   · ⚠️ **只接槽 0**：原版 5 张 `CardUI` 里只有 1 张（无后缀那张）的 `Front` 有 `ParentConstraint`。
//         本窗槽 0 = 主卡（`Card` 属性就是 `SlotViews[0]`），其余是相关卡 ⇒ 只绑槽 0 与原版一致。
//
// 【判据（2026-11-01 现读）】
//   · `ParentConstraint.Sync()`：`if (inheritPosition) transform.position = target.TransformPoint(childLocalPos)`
//     `if (inheritRotation) transform.rotation = target.rotation * childRotOffset`（`inheritScale = 0` 不走）
//     —— 实现见 `Core/CardDisplayLevelUpPartial_B111F1C.cs`。
//   · clip 两条曲线：`t=0 → 1`、`t=1.45 → 0`（`inSlope/outSlope = Infinity` ⇒ 阶跃），1.45 = `ChangeFrame` 事件时刻。
//   · 节点名：光效层由 `CardDisplayEffects.Build` 建，根名 `"Card Display Effects"`，其中一件叫 `"Level Up Effect"`
//     （`Level Up Effect Down` 是它的子件 ⇒ 名字必须**精确匹配**，⛔ 别用 `StartsWith`/`Contains`）。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace CardPresentation
{
    public partial class CardDisplayWindow
    {
        /// <summary>本窗的升级动画组件（原版 `CardDisplayLevelUpAnimationController` 的合并等价物）。
        /// ⚠️ 本窗**不会自己播**它 —— 只把它和 `Front` / `Level Up Effect` 接上，播不播由升级流程决定。</summary>
        public CardDisplayLevelUp LevelUpFx { get; private set; }

        /// <summary>绑上去的子节点（= 主卡 `CardView.FaceContentRoot`，即 `2DCard/Front`）。</summary>
        public Transform LevelUpInheritChild { get; private set; }

        /// <summary>绑上去的目标（= 光效层里的 `Level Up Effect`）。</summary>
        public Transform LevelUpInheritTarget { get; private set; }

        /// <summary>「目标接不上」只说一次（⛔ 不刷屏，也不静默）。</summary>
        bool _warnedNoLevelUpFxTargetB111F1E;

        /// <summary>确保本窗有一个 `CardDisplayLevelUp` 并把两个接线口绑好（幂等；每次 `BuildFan` 之后调一次）。
        /// <para>⚠️ 调用点只有一处 = `BuildFan()` 末尾（卡重建 ⇒ `Front` 换引用 ⇒ 必须重绑）。</para></summary>
        public void EnsureLevelUpFx()
        {
            if (LevelUpFx == null)
            {
                LevelUpFx = GetComponent<CardDisplayLevelUp>();
                if (LevelUpFx == null) LevelUpFx = gameObject.AddComponent<CardDisplayLevelUp>();
            }
            BindLevelUpInheritTargets();
        }

        /// <summary>把 `inheritChild` / `inheritTarget` 指到本窗真实节点（两个节点都在本窗这棵树里 —— 这正是选项 b 的理由）。</summary>
        public void BindLevelUpInheritTargets()
        {
            var fx = LevelUpFx;
            if (fx == null) return;

            // ① 子节点 = **主卡（槽 0）** 的 `2DCard/Front`。⚠️ 只取槽 0：原版只有一张卡挂约束，其余相关卡没有。
            var v = SlotViews[0];
            LevelUpInheritChild = v != null ? v.FaceContentRoot : null;

            // ② 目标 = 光效层里的 `Level Up Effect`（**精确名**匹配；`…Down` 是它的子件，别误取）
            LevelUpInheritTarget = FindFxNode(_fx, "Level Up Effect");

            fx.inheritChild = LevelUpInheritChild;
            fx.inheritTarget = LevelUpInheritTarget;

            if (LevelUpInheritTarget == null && !_warnedNoLevelUpFxTargetB111F1E)
            {
                _warnedNoLevelUpFxTargetB111F1E = true;
                Debug.LogWarning("[展示窗] 升级动画的 `inheritTarget` 接不上：光效层里没有 `Level Up Effect`"
                               + "（`CardDisplayEffects.Build` 没建成 / `_fx` 为空）⇒ 原版 clip 的"
                               + " `2DCard/Front · inheritPosition/inheritRotation`（0→1.45 s 钉住）**不生效**"
                               + "（⛔ 不是静默）。`inheritChild` = "
                               + (LevelUpInheritChild != null ? LevelUpInheritChild.name : "**空**"));
            }
        }

        /// <summary>在光效层里按**精确名**找一个节点（原版那两件是 `Level Up Effect` 与它的子件 `… Down`）。
        /// ⛔ 别改成 `Find`/`StartsWith`：`Transform.Find` 只查直接子件、而本层允许嵌套（`… Down` 就挂在父件下）。</summary>
        static Transform FindFxNode(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var hit = FindFxNode(root.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }
    }
}
