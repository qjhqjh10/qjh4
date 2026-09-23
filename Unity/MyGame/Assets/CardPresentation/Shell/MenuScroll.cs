// MenuScroll.cs — 阶段二「外壳」的**滚动区**（全壳唯一一份滚动实现）
//
// ============================ 为什么要有这个文件 ============================
// 原版每一处能滚的地方都是一个 UGUI `ScrollRect` + `Viewport`(带 `RectMask2D`) + 一个带
// `ContentSizeFitter` 的内容容器（`Rewards Scroll View` / `Forge Army Selector` / 商店的栅格 …）。
// 我们的渲染是**自建 mesh**（`ImageQuad`/`Label`），没有 UGUI 那套组件可用 ——
// 所以「偏移 + 夹取 + 裁切」这三件事得自己有一份、**并且只写这一份**（CLAUDE.md §三）。
//
// 🔴 **2026-09-23 为什么现在才做**：在此之前**整个外壳一处滚动都没有** ⇒
//    · 锻造奖励轨道：格宽 505.9、步长 375.9，第 5 格（level 5）中心在 **2209px**，全在屏幕外
//      ⇒ **领完第 4 格就再也领不动**（可领光效还在闪、按钮却找不到）；
//    · 两条阵营条：13 个条目本来就放不下（见 `项目任务.md` §三 第 15 条 第 21 条）。
//
// ============================ 原版字段（照抄，别自己编） ============================
// 出处：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/` 里 **102 个 ScrollRect**
//       的实测分布（`m_Horizontal`/`m_Vertical`/`m_MovementType`/`m_Elasticity`/`m_Inertia`/
//       `m_DecelerationRate`/`m_ScrollSensitivity`）：
//   · `m_MovementType`：**1 = Clamped** 或 2 = Elastic（两种都有）
//   · `m_Elasticity` **0.1** · `m_Inertia` **1** · `m_DecelerationRate` **0.135**（全库一致）
//   · `m_ScrollSensitivity`：**1 / 10 / 50 / 100**（**逐处不同**，没有统一值）
//   · `Forge Tab/Rewards Scroll View` = **横向**（`m_Horizontal 1 / m_Vertical 0`）
//
// ⇒ 我们的 v1 实现 **Clamped + 滚轮**：
//   ⚠️ **Elastic 回弹与 Inertia 惯性要有【帧循环】才成立**（位移是逐帧插值的），
//      而**批处理下没有帧循环**（同 `Object.Destroy` 不生效、粒子要手动 `Simulate` 那一族）。
//      真 Play 里有帧循环，但这一版**没实现惯性/回弹** —— **明确出声，不是静默**。
//      将来要补：在 `Update` 里对速度做 `v *= Mathf.Pow(0.135f, Time.deltaTime * 60f)`。
//   · 滚轮一格的手感照**卡组编辑那条已经验过的路**：`DeckRuntime.HandleScroll` 用 `dy * 0.4f`
//     （`Deck/DeckRuntime.cs:1106-1113`）⇒ 这里取同一个系数，**别两处各写一套**。
//
// ============================ 两种内容的坐标范围 ============================
// · **左对齐内容**（`Rewards Content`：原版锚在视口左边 + `ContentSizeFitter`）
//   ⇒ 范围 `[0, contentX2 − viewport.x2]`（只能往左滚）。
// · **居中内容**（`Army Content`：原版是「视口中心的一个零宽点」⇒ 对称展开）
//   ⇒ 范围 **两侧都有**：`[contentX1 − viewport.x1, contentX2 − viewport.x2]`
//     （往右滚能看到被左柱盖住的那几个、往左滚能看到越出右边的那几个）。
//   两个范围都由 `contentX1/contentX2` 自动算出来，调用方不用自己定。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>一个滚动区。宿主建一次、内容变了重建；**它自己不知道内容是什么**。
    /// 用法（照 `ForgeTab.BuildRewardCells` / `BuildArmyItems`）：
    /// ① 建区：`_scroll = new MenuScroll(viewportRect, contentX1, contentX2) { Owner = …, OnChanged = 重建 };`
    /// ② 画内容时：`var r = _scroll.Shift(内容矩形); if (!_scroll.Intersects(r)) continue;`
    ///    再 `_win.Clip = _scroll.Viewport;`（画完清掉）—— 逐 quad 的裁切由 `MenuWindowBase.Rect` 做。
    /// ③ 注册给指针层：`PointerLayer.RegisterScroll(_scroll)`（滚轮才会找到它）。</summary>
    public class MenuScroll
    {
        /// <summary>一格滚轮对应的系数（**照卡组编辑那条已验过的路**：`dy * 0.4f`；
        /// 新输入系统一格是 ±120 ⇒ 一格滚 48px）。原版 `m_ScrollSensitivity` 逐处不同，我们全壳取一个手感。</summary>
        public const float NotchK = 0.4f;

        /// <summary>裁切边界（画布像素 · 左上原点）。**等价于原版 `Viewport` + 它的 `RectMask2D`**。</summary>
        public PxRect Viewport;
        /// <summary>内容在**偏移 0 时**的两端（原版 `ContentSizeFitter` 跑完的两端）。
        /// 🔴 **纵向滚动用它当 y 的两端**（`MenuScroll.Axis`）—— 同一套极值算法，只换轴。</summary>
        public float ContentX1, ContentX2;

        /// <summary>滚哪一根轴。`false`（默认）= 横向（锻造轨道/阵营条那种）；
        /// `true` = **纵向**（收藏线的卡组格/卡池那种，原版 `m_Vertical 1`）。
        /// ⚠️ 纵向时 `ContentX1/ContentX2` 装的是**上下两端**（字段名保留是为了不复制一份实现）。</summary>
        public bool Vertical;
        /// <summary>当前偏移（画布像素；正 = 内容左移，能看到右边的东西）。</summary>
        public float Offset;
        /// <summary>偏移变化时回调（宿主在里头重建内容）。</summary>
        public System.Action OnChanged;
        /// <summary>这个区属于哪个节点（窗口 / 页）。`PointerLayer` 命中时跳过**已经关掉 / 切走的**那些
        /// —— 页签切换走的是 `SetActive`，切走那一页里的滚动区**还留在登记表里**。</summary>
        public GameObject Owner;

        public MenuScroll(PxRect viewport, float contentX1, float contentX2)
        {
            Viewport = viewport;
            ContentX1 = contentX1;
            ContentX2 = contentX2;
        }

        /// <summary>左对齐内容的便捷构造（`Rewards Content` 那种：内容从视口左边起排）。</summary>
        public static MenuScroll LeftAligned(PxRect viewport, float contentW)
        {
            return new MenuScroll(viewport, viewport.x1, viewport.x1 + contentW);
        }

        /// <summary>**上对齐**内容的便捷构造（纵向网格那种：内容从视口上边起排）。</summary>
        public static MenuScroll TopAligned(PxRect viewport, float contentH)
        {
            var s = new MenuScroll(viewport, viewport.y1, viewport.y1 + contentH);
            s.Vertical = true;
            return s;
        }

        float ViewLo { get { return Vertical ? Viewport.y1 : Viewport.x1; } }
        float ViewHi { get { return Vertical ? Viewport.y2 : Viewport.x2; } }
        float ViewMid { get { return Vertical ? Viewport.CY : Viewport.CX; } }

        /// <summary>可滚到的左/上极值（负 = 内容还能往右/下推）。</summary>
        public float MinOffset { get { return ContentX1 - ViewLo; } }
        /// <summary>可滚到的右/下极值（正 = 内容还能往左/上推）。</summary>
        public float MaxOffset { get { return ContentX2 - ViewHi; } }

        public void ScrollBy(float dx)
        {
            SetOffset(Offset + dx);
        }

        /// <summary>**绝对**设偏移（夹到范围里）。`FocusOn` 用它 ——
        /// 🔴 2026-09-23 踩过：`FocusOn` 一开始调的 `ScrollBy`（**相对**加）⇒ 连调两次位移翻倍，
        ///    自检当场报出「偏移 2124.82，期望 708」（自检抓 bug 的又一次实例）。</summary>
        public void SetOffset(float o)
        {
            float v = Mathf.Clamp(o, Mathf.Min(0f, MinOffset), Mathf.Max(0f, MaxOffset));
            if (Mathf.Abs(v - Offset) < 0.01f) return;
            Offset = v;
            if (OnChanged != null) OnChanged();
        }

        /// <summary>滚轮一格（`Mouse.current.scroll.ReadValue().y`，Windows 上一格 ±120）。
        /// 往下滚（`dy<0`）= 内容左移 = 看右边的东西（照卡组编辑的方向）。</summary>
        public void Wheel(float dy)
        {
            if (Mathf.Abs(dy) < 1f) return;
            ScrollBy(-dy * NotchK);
        }

        /// <summary>把内容上的一点对到视口中心 —— **照原版 `ScrollViewFocusFunctions.FocusOnItem`**
        /// （`decomp_full/ScrollViewFocusFunctions__CalculateFocusedScrollPosition.c`：取 item 矩形中心变换到
        /// content 空间再算归一化位置 ⇒ 结果就是「item 中心 = 视口中心」）。
        /// ⚠️ 原版谁调它：`ArmySelector.FocusOnArmy`（协程）—— 而**全代码零处调用它**
        /// （`grep -rln FocusOnArmy decomp_full` 只有定义与协程本体，只能由 prefab 侧 UnityEvent 触发）；
        /// 锻造轨道那边有 `ForgeRewardSelector.FinishedSnapping` 一路（说明原版确实会「吸附到某一格」）。
        /// ⇒ 我们用它做**开局定位**：阵营条对到选中的阵营、奖励轨道对到该领的那一格。</summary>
        public void FocusOn(float contentCenter)
        {
            SetOffset(contentCenter - ViewMid);
        }

        /// <summary>内容坐标 → 屏幕坐标（**只做偏移、不裁**）。
        /// 裁切交给 `MenuWindowBase.Clip` —— 因为内容里的每个 quad 都是**按内容坐标摆**的，
        /// 先裁再摆会把子件（文字/图标/条形）一起挪走。</summary>
        public PxRect Shift(PxRect contentRect)
        {
            return Vertical
                ? new PxRect(contentRect.x1, contentRect.y1 - Offset, contentRect.x2, contentRect.y2 - Offset)
                : new PxRect(contentRect.x1 - Offset, contentRect.y1, contentRect.x2 - Offset, contentRect.y2);
        }

        /// <summary>这块（屏幕坐标）与视口有没有交集 —— 没有就**整块别建**
        /// （省下几十个 quad，顺带让它的点击区也消失 = 原版被 `RectMask2D` 裁掉的部分点不到）。</summary>
        public bool Intersects(PxRect onScreen)
        {
            return Vertical
                ? onScreen.y2 > Viewport.y1 + 0.01f && onScreen.y1 < Viewport.y2 - 0.01f
                : onScreen.x2 > Viewport.x1 + 0.01f && onScreen.x1 < Viewport.x2 - 0.01f;
        }

        /// <summary>把内容坐标的矩形搬到屏幕上**并夹到视口**（= 裁切）。
        /// 返回 false = 整块不可见（**别画**）。`uv` 是贴图上要显示的那一段（**必须跟着截**，
        /// 否则图会被压扁 —— 同 `ImageQuad.SetUvRect` 的注释）。
        /// ⚠️ 只在「整块都是同一个 quad」的场合用它；一个容器里有多个子件时用 `Shift` + `MenuWindowBase.Clip`。</summary>
        public bool Place(PxRect contentRect, out PxRect onScreen, out Rect uv)
        {
            float x1 = contentRect.x1 - Offset, x2 = contentRect.x2 - Offset;
            float cx1 = Mathf.Max(x1, Viewport.x1), cx2 = Mathf.Min(x2, Viewport.x2);
            float w = x2 - x1;

            if (cx2 <= cx1 + 0.01f || w <= 0.01f)
            {
                onScreen = new PxRect(0f, contentRect.y1, 0f, contentRect.y2);
                uv = new Rect(0f, 0f, 0f, 0f);
                return false;
            }
            onScreen = new PxRect(cx1, contentRect.y1, cx2, contentRect.y2);
            uv = new Rect((cx1 - x1) / w, 0f, (cx2 - cx1) / w, 1f);
            return true;
        }
    }
}
