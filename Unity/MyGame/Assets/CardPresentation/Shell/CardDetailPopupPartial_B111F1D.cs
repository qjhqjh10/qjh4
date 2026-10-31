// CardDetailPopupPartial_B111F1D.cs — 给 `CardDisplayLevelUp` 的 `inheritChild` / `inheritTarget` 接线（B-70 最后一跳）
// （B111_F1D · 2026-11-01）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【这一步要接什么】
//   原版 `Level Up Card` clip 用两条**阶跃**曲线（`inheritPosition`/`inheritRotation`，`t=0→1`、`t=1.45→0`）
//   把 `2DCard/Front` 钉在 `2DCard/Level Up Effect` 上。等价物在
//   `Core/CardDisplayLevelUpPartial_B111F1C.cs`（`inheritChild` / `inheritTarget` 两个字段），
//   本文件负责**把这两个字段指到本窗真实存在的节点**。
//
// 【判据（2026-11-01 现读，全部自己核过）】
//   ① 原版动画的宿主：`MainMenu Warpforge.unity` 里挂着 `Level Up Card` 的 `Animation` 组件在
//      **`…/Card Displayer Menu For Menu/Card Display/Cards/CardUI`**（GameObject &88）——
//      也就是**那张展示卡本身**，不是 `Upgrade Panel`。⇒ clip 里的 `2DCard/Front` 是相对这张卡解析的。
//   ② 原版挂约束的那张卡：同一批 `Cards/` 下 5 张 `CardUI` 里，**只有 `CardUI`（无后缀）** 的 `2DCard/Front`
//      有 `ParentConstraint`（target = 同卡 `2DCard/Level Up Effect`）；`CardUI (1..4)` 只有 RectTransform。
//      ⇒ **「只给一张挂」** 在原版就是硬事实，不是我们的取舍。
//   ③ 我们这边：本窗（`CardDetailPopup`）**只有一张卡**（槽 0，见 `RewireLevelUpTargets` 那条 doc）
//      ⇒ 天然满足「只有一张」。`inheritChild` = `SlotView(0).FaceContentRoot`（= `CardView` 的 `2DCard/Front`）。
//
// 🔴 **④ 本窗没有 `Level Up Effect` 节点（结构性缺口，如实记）**：
//    `Level Up Effect` / `…Down` 只在 `Shell/CardDisplayEffects.cs` 建的那一层里，而那一层**只有一个调用点**——
//    `Battle/CardDisplayWindow.cs:646`（`_fx = CardDisplayEffects.Build(_cards, …)`，那扇「卡片展示窗」）。
//    本窗（`CardDetailPopup`）**不建**它（同族事实：`RewireLevelUpTargets` 里 `levelUpEffect1/2` 也一直是空，
//    文件自己的注释写着「我们这棵树上没有那三格」）。
//    ⇒ `inheritTarget = FindNewest("Level Up Effect")` **今天只能是 null**，`CardDisplayLevelUp` 会**出声一次**
//      并**不开窗**（⛔ 不是静默）。要让它真生效，得满足二者之一（**都不在本代理白名单**）：
//        a) 本窗也建那一层光效（`Shell/CardDisplayEffects.cs` 一族的活），或
//        b) 把这一跳搬到**真有那个节点的那扇窗**上跑（`Battle/CardDisplayWindow` 有 `Front` 也有 `Level Up Effect`）；
//      并注意原版里两者是**同一个 `2DCard` 下的同级兄弟**，而我们这边 `Front` 在 `CardView` 内部、
//      `Level Up Effect` 在 `Card Display Effects` 层根下 —— **不同父**（见 `报告/B111_F1C.md` §五·2）。
//    ⛔ 本文件**不伪造**一个 target（例如拿窗根/卡根顶替）—— 那会让「钉住」变成原版没有的行为。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace CardPresentation
{
    public partial class CardDetailPopup
    {
        /// <summary>本窗那张卡的 `2DCard/Front`（原版 `inheritChild` 那一格；由 <see cref="BindLevelUpInheritTargets"/> 填）。</summary>
        public Transform LevelUpInheritChild { get; private set; }

        /// <summary>原版 `2DCard/Level Up Effect`。⚠️ **本窗不建这个节点** ⇒ 今天恒 `null`（见文件头 ④）。</summary>
        public Transform LevelUpInheritTarget { get; private set; }

        /// <summary>`inheritTarget` 为空这件事只解释一次（⛔ 不刷屏，也不静默）。</summary>
        bool _warnedNoInheritTargetB111F1D;

        /// <summary>把 `CardDisplayLevelUp` 的两个接线字段指到本窗真实节点。
        /// <para>⚠️ 调用面只有一处 = `RewireLevelUpTargets()` —— 它同时覆盖「`DoAnim` 之前」与
        /// 「`ChangeFrame`(1.45 s) → `RebuildKeepingState()` **之后**」两条路（重建会换掉 `Front`）。
        /// ⛔ 别在别处再调一次：两次赋值之间如果卡被重建，旧引用会被写回去。</para></summary>
        public void BindLevelUpInheritTargets()
        {
            var fx = LevelUpEffect;
            if (fx == null) return;

            // ① 子节点 = **槽 0** 那张卡的 `2DCard/Front`
            //    （本窗 `Card` 永远建在槽 0；⛔ 别写成遍历全部槽 —— 原版只有一张卡做这件事）
            var v = SlotView(0);
            LevelUpInheritChild = v != null ? v.FaceContentRoot : null;

            // ② 目标 = **最新那棵**子树里名为 `Level Up Effect` 的节点（`FindNewest` 是精确名匹配 ⇒ 不会误取 `… Down`）
            LevelUpInheritTarget = FindNewest("Level Up Effect");

            fx.inheritChild = LevelUpInheritChild;
            fx.inheritTarget = LevelUpInheritTarget;

            if (LevelUpInheritTarget == null && !_warnedNoInheritTargetB111F1D)
            {
                _warnedNoInheritTargetB111F1D = true;
                Debug.Log("[CardDetail] 升级动画的 `inheritTarget` **接不上**：本窗不建 `Level Up Effect` 节点"
                        + "（那一层只在 `Battle/CardDisplayWindow` 里由 `CardDisplayEffects.Build` 建）"
                        + " ⇒ 原版 clip 的 `2DCard/Front · inheritPosition/inheritRotation`（0→1.45 s 钉住）**不生效**"
                        + "（`CardDisplayLevelUp` 那边也会再出声一次）。`inheritChild` = "
                        + (LevelUpInheritChild != null ? LevelUpInheritChild.name : "**空**"));
            }
        }
    }
}
