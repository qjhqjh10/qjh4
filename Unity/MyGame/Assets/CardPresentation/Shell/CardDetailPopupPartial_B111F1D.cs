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
// 🔴 **④ 旧口径「本窗没有 `Level Up Effect` 节点」已作废（2026-11-01 `B-195`/`B-196` 已补 ⇒ 主卡 `2DCard` 下真的建了它）**：
//    `CardDetailPopup.BuildCardStack` 现在调 `CardDisplayEffects.BuildUnder2DCard(SlotViews[0].FaceRoot)`
//    ⇒ 那 4 条图件 + 10 颗粒子直接建在**主卡的 `2DCard`** 下（原版 `CardUI ▸ 2DCard ▸ …`），换算 `s = 1`。
//    ⇒ `inheritTarget = FindNewest("Level Up Effect")` 现在**找得到**；它和 `Front` 是**同一个 `2DCard` 下的
//    同级兄弟**（原版如此，见 `报告/B112W_B149_B195_B196.md`）。
//    ⚠️ 旧口径（「本窗不建、恒 null」）的根因是那一层原来只建在 `Battle/CardDisplayWindow` 的层根下 ——
//      那是**错窗 + 错父件**（`B-195`），两个错已一并修掉。
//    ⛔ 本文件**仍然不伪造**一个 target（例如拿窗根/卡根顶替）—— 那会让「钉住」变成原版没有的行为；
//      目标只能来自**精确名** `FindNewest("Level Up Effect")`。
//    ⚠️ 仍然生效的边界：`FindNewest` 是从**窗根**按「最新那棵子树」往下找，而光效层在主卡内部 ⇒ 能命中；
//      若将来把光效层挪出主卡，这条又会静默变 null（`CardDisplayLevelUp` 会出声一次，⛔ 不是静默）。
//    📌 判据（原版一手）：`MainMenu Warpforge.unity` 的 `!u!111 &622`（`Animation`）在 `GO 88 = …/Cards/CardUI`，其下 `!u!224 &1323`（`2DCard`）→ `!u!224 &1126`（`Level Up Effect`）`m_Father: 1323`。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace CardPresentation
{
    public partial class CardDetailPopup
    {
        /// <summary>本窗那张卡的 `2DCard/Front`（原版 `inheritChild` 那一格；由 <see cref="BindLevelUpInheritTargets"/> 填）。</summary>
        public Transform LevelUpInheritChild { get; private set; }

        /// <summary>原版 `2DCard/Level Up Effect`。✅ **2026-11-01（`B-195`/`B-196`）起本窗真的建了它**（主卡 `2DCard` 下，见文件头 ④）。</summary>
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
                Debug.LogWarning("[CardDetail] 升级动画的 `inheritTarget` **接不上**：主卡 `2DCard` 下没有 `Level Up Effect`"
                        + "（`BuildCardStack` 里那句 `CardDisplayEffects.BuildUnder2DCard` 没建成 / 判据 JSON 或材质缺）"
                        + " ⇒ 原版 clip 的 `2DCard/Front · inheritPosition/inheritRotation`（0→1.45 s 钉住）**不生效**"
                        + "（`CardDisplayLevelUp` 那边也会再出声一次）。`inheritChild` = "
                        + (LevelUpInheritChild != null ? LevelUpInheritChild.name : "**空**"));
            }
        }
    }
}
