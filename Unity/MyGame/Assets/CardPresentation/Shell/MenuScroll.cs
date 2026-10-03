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
//   · 滚轮一格的手感照**卡组编辑那条已经验过的路**：`DeckRuntime.HandleScroll` 用 `dy * 0.4f`
//     （`Deck/DeckRuntime.cs:1106-1113`）⇒ 这里取同一个系数，**别两处各写一套**。
//
// ============================ 🆕 2026-10-03 惯性 / 回弹 / 拖拽（照 UGUI 源码） ============================
// 🔴 **判据 = Unity 自带的 `ScrollRect` 源码本体** —— 原版游戏的滚动就是它跑的，
//    所以「照抄 UGUI 的算式」= 逐位复刻，**不是我们挑的**。本地就能读：
//    `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/ScrollRect.cs`
//   · **惯性**（`LateUpdate` :863-866）：`v *= Mathf.Pow(m_DecelerationRate, deltaTime)`；
//     `|v| < 1` ⇒ 归零；`position += v * deltaTime`。⚠️ 指数是 **`deltaTime`**、**不是** `deltaTime*60`
//     （旧注释里那句 `dt*60` 是**错的** —— 那会让衰减快 60 倍）。
//   · **速度来源**（:884-888，拖动中每帧）：`newV = (pos − prevPos)/dt`；
//     `v = Vector3.Lerp(v, newV, deltaTime * 10)`。⚠️ 用 **`Time.unscaledDeltaTime`**。
//   · **回弹**（:849-858，`Elastic` 且越界时）：`Mathf.SmoothDamp(pos, pos+offset, ref speed,
//     smoothTime)`，`smoothTime = m_Elasticity`（滚动那一帧 **×3**）；`|speed| < 1` ⇒ 0。
//   · **拖拽**（`OnBeginDrag` :713 / `OnDrag` :779 / `OnEndDrag` :750）：记按下点与按下时的内容位，
//     `position = 按下位 + (当前指针 − 按下指针)`；Clamped ⇒ 夹回；Elastic ⇒ 再减 `RubberDelta`。
//   · **`RubberDelta`**（:1084-1087，橡皮筋阻尼，**照抄**）：
//     `(1 − 1/((|over|·0.55/viewSize) + 1)) · viewSize · sign(over)`
//   ⚠️ **批处理下没有帧循环** ⇒ 惯性/回弹靠 `Tick(dt)` 推；`Update` 里由 `PointerLayer.TickScrolls` 调，
//      自检则直调 `Tick`（同 `Object.Destroy` 不生效、粒子要手动 `Simulate` 那一族的规矩）。
//   ⚠️ **拖拽的「要不要算拖」由 `PointerLayer` 判**（UGUI 是 EventSystem 的 `dragThreshold`，默认 **10px**）——
//      它同时决定「这一下还算不算点击」（拖动超过阈值 ⇒ 手柄抬起时**不点按钮**）。
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
    ///    再 `_win.Clip = _scroll.Viewport;`（画完清掉）—— 逐 quad 的裁切由 **`MenuWindowBase` 那一层的包装**做
    ///    （⚠️ **2026-10-03 就地订正**：这句原来写的是「由 `MenuWindowBase.Rect` 做」—— **已过期**，
    ///     现在 `Nine` / `Text` / `TextBox` / `AddHit` **也都吃 `Clip`**（`Nine` 与 `AddHit` 是 2026-10-03 补的）⇒
    ///     别以为「只有 `Rect` 会裁」；`MenuDraw` 那层的 `Nine` / `Tiled` 同一批也补了 `clip` 形参）。
    /// ③ 注册给指针层：`PointerLayer.RegisterScroll(_scroll)`（滚轮才会找到它）。</summary>
    public class MenuScroll
    {
        /// <summary>一格滚轮对应的系数（**照卡组编辑那条已验过的路**：`dy * 0.4f`；
        /// 新输入系统一格是 ±120 ⇒ 一格滚 48px）。原版 `m_ScrollSensitivity` 逐处不同，我们全壳取一个手感。</summary>
        public const float NotchK = 0.4f;

        // ============================================================ 原版字段（照抄 UGUI，别自己编）
        /// <summary>`m_Elasticity` —— **全库一致 0.1**（102 个 ScrollRect 实测）。回弹的 `SmoothDamp` 时间。</summary>
        public const float Elasticity = 0.1f;
        /// <summary>`m_DecelerationRate` —— **全库一致 0.135**。惯性的每秒衰减底数（`Pow(0.135, dt)`）。</summary>
        public const float DecelerationRate = 0.135f;

        /// <summary>`m_MovementType == 2`（Elastic）。**逐处不同** —— 默认 `false` = Clamped（`1`）。
        /// Elastic 的场次：拖出去会有橡皮筋阻尼、松手回弹。</summary>
        public bool Elastic;
        /// <summary>`m_Inertia` —— **全库一致 1**。关掉它就是「松手立刻停」。</summary>
        public bool Inertia = true;

        /// <summary>当前速度（画布像素 / 秒，沿滚动轴）。拖动中每帧由 `Tick` 更新（照 UGUI 的 `Lerp(v,newV,dt*10)`）。</summary>
        public float Velocity { get { return _vel; } }
        /// <summary>正在被拖吗（`PointerLayer` 派的）。</summary>
        public bool Dragging { get { return _dragging; } }

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

        // ---- 惯性 / 回弹 / 拖拽 的运行时状态（照 UGUI `ScrollRect` 的对应字段）----
        float _vel;                 // m_Velocity（只取滚动轴那一个分量）
        bool _dragging;             // m_Dragging
        float _dragStartAxis;       // 按下时的指针坐标（滚动轴）
        float _dragStartOffset;     // m_ContentStartPosition
        float _prevOffset;          // m_PrevPosition
        bool _scrolling;            // m_Scrolling（滚轮那一帧 ⇒ 回弹的 smoothTime ×3）

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

        /// <summary>夹取下界（**恒 ≤ 0** —— 内容比视口小时两端都收到 0，照 `SetOffset` 原来那条夹法）。</summary>
        public float ClampLo { get { return Mathf.Min(0f, MinOffset); } }
        /// <summary>夹取上界（**恒 ≥ 0**）。</summary>
        public float ClampHi { get { return Mathf.Max(0f, MaxOffset); } }
        /// <summary>视口在滚动轴上的长度（`RubberDelta` 与橡皮筋要用它）。</summary>
        public float ViewSize { get { return Vertical ? Viewport.H : Viewport.W; } }

        /// <summary>内容此刻是不是**越出了**可滚范围（Elastic 下才会发生；画面上就是「拉过头了」）。</summary>
        public bool OutOfRange
        {
            get { return Offset < ClampLo - 0.001f || Offset > ClampHi + 0.001f; }
        }

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
        /// 往下滚（`dy<0`）= 内容左移 = 看右边的东西（照卡组编辑的方向）。
        /// 照 UGUI `OnScroll`（`ScrollRect.cs:648-679`）：**Clamped 才夹回**；Elastic 允许滚出范围、随后回弹；
        /// 滚轮**不产生速度**（惯性只由拖拽产生）。</summary>
        public void Wheel(float dy)
        {
            if (Mathf.Abs(dy) < 1f) return;
            _scrolling = true;                       // 回弹那一帧 smoothTime ×3（UGUI :853-854）
            Apply(Offset - dy * NotchK, !Elastic);   // Clamped ⇒ 夹；Elastic ⇒ 允许越界
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

        // ============================================================ 拖拽 / 惯性 / 回弹（照 UGUI）

        /// <summary>写偏移并通知。`clamp` = 是否**硬夹**回可滚范围
        /// （原版：`MovementType.Clamped` 的每一处写入都夹；`Elastic` 只在松手回弹那一段收）。</summary>
        void Apply(float o, bool clamp)
        {
            float v = clamp ? Mathf.Clamp(o, ClampLo, ClampHi) : o;
            if (Mathf.Abs(v - Offset) < 0.01f) return;
            Offset = v;
            if (OnChanged != null) OnChanged();
        }

        /// <summary>按下：开始拖这一区。`axisPos` = 指针在**滚动轴**上的画布坐标
        /// （横向给 x、纵向给 y）—— 由 `PointerLayer` 从命中点换算好传进来。</summary>
        public void BeginDrag(float axisPos)
        {
            _dragging = true;
            _dragStartAxis = axisPos;
            _dragStartOffset = Offset;
            _prevOffset = Offset;
            _vel = 0f;
        }

        /// <summary>拖动中（照 UGUI `OnDrag` :794-810）：
        /// `raw = 按下时的偏移 − (当前指针 − 按下指针)`；
        /// **Clamped ⇒ 夹回**；**Elastic ⇒ 再叠一层 `RubberDelta`**（橡皮筋阻尼，越拉越沉）。</summary>
        public void DragTo(float axisPos)
        {
            if (!_dragging) return;
            float raw = _dragStartOffset - (axisPos - _dragStartAxis);
            float hard = Mathf.Clamp(raw, ClampLo, ClampHi);
            float over = raw - hard;
            float v = Elastic ? hard + RubberDelta(over, ViewSize) : hard;
            if (Mathf.Abs(v - Offset) < 0.001f) return;
            Offset = v;
            if (OnChanged != null) OnChanged();
        }

        /// <summary>松手（照 UGUI `OnEndDrag` :750-756）：只清拖动标记 —— **速度留着**，
        /// 下一帧 `Tick` 就带着它做惯性（或回弹）。</summary>
        public void EndDrag() { _dragging = false; }

        /// <summary>把这一区停下来（切页/重建时用 —— 否则旧速度会在新内容上继续滚）。</summary>
        public void Stop()
        {
            _dragging = false; _vel = 0f; _prevOffset = Offset; _scrolling = false;
        }

        /// <summary>回弹的橡皮筋阻尼（**逐位照抄** UGUI `ScrollRect.RubberDelta` :1084-1087）。</summary>
        static float RubberDelta(float overStretching, float viewSize)
        {
            if (viewSize <= 0f) return overStretching;
            return (1f - (1f / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1f)))
                   * viewSize * Mathf.Sign(overStretching);
        }

        /// <summary>推进一帧（等价于 UGUI `ScrollRect.LateUpdate` :830-900 里与本区有关的那一段）。
        /// ⚠️ **批处理没有帧循环** ⇒ 真跑由 `PointerLayer.Update` 调，自检**直调**（同 `ParticleSystem.Simulate`）。
        /// 返回这一帧有没有动过（没动就不必重建内容）。</summary>
        public bool Tick(float dt)
        {
            if (dt <= 0f) return false;
            float before = Offset;

            if (_dragging)
            {
                // 照 UGUI :884-888 —— 拖动中把「这一帧走了多远 / dt」混进速度（`Lerp(v,newV,dt*10)`）
                if (Inertia)
                {
                    float newV = (Offset - _prevOffset) / dt;
                    _vel = Mathf.Lerp(_vel, newV, Mathf.Clamp01(dt * 10f));
                }
            }
            else
            {
                float hard = Mathf.Clamp(Offset, ClampLo, ClampHi);
                float over = Offset - hard;                       // UGUI 的 `offset` 取反号，这里用「越出多少」
                if (Elastic && Mathf.Abs(over) > 0.001f)
                {
                    // 照 UGUI :849-858 —— SmoothDamp 回边界；滚轮那一帧 smoothTime ×3
                    float speed = _vel;
                    float smoothTime = Elasticity * (_scrolling ? 3f : 1f);
                    float nv = Mathf.SmoothDamp(Offset, hard, ref speed, smoothTime, Mathf.Infinity, dt);
                    Offset = Mathf.Abs(nv - hard) < 0.01f ? hard : nv;   // 落到边界上就精确吸附（杀浮点残留）
                    if (Mathf.Abs(speed) < 1f) speed = 0f;
                    _vel = speed;
                }
                else if (Inertia && Mathf.Abs(_vel) > 0.0001f)
                {
                    // 照 UGUI :863-866 —— `Pow(0.135, dt)`（**不是 dt*60**）；`|v|<1` 归零；`pos += v*dt`
                    _vel *= Mathf.Pow(DecelerationRate, dt);
                    if (Mathf.Abs(_vel) < 1f) _vel = 0f;
                    Offset += _vel * dt;
                }
                else
                {
                    _vel = 0f;
                    Offset = hard;                                 // Elastic 回弹到位后精确落在边界
                }

                if (!Elastic) Offset = Mathf.Clamp(Offset, ClampLo, ClampHi);
            }

            _prevOffset = Offset;
            _scrolling = false;                                    // UGUI 在 LateUpdate 末尾清（:899）

            bool moved = Mathf.Abs(Offset - before) > 0.001f;
            if (moved && OnChanged != null) OnChanged();
            return moved;
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
