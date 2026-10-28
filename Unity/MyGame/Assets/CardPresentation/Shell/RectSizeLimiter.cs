// RectSizeLimiter.cs —— 原版 `RectSizeLimiter : UIBehaviour, ILayoutSelfController, ILayoutController`
//（账 `未分类/其他` 第 509 / 510 行：`SetLayoutHorizontal()` / `SetLayoutVertical()`）。
//
// ============================ 判据（逐条现读，第一权威）============================
// · 类声明 / 字段 / 方法序 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/RectSizeLimiter.cs`（全文可读）：
//     public class RectSizeLimiter : UIBehaviour, ILayoutSelfController, ILayoutController
//     public RectTransform rectTransform;      // +0x20
//     protected Vector2 m_maxSize;             // +0x28(x) / +0x2c(y)
//     protected Vector2 m_minSize;             // +0x30(x) / +0x34(y)
//     private DrivenRectTransformTracker m_Tracker;   // +0x38
// · 两个回调的**逐句**控制流 = `d:/2/tools/decomp_full/RectSizeLimiter__SetLayoutHorizontal.c` /
//     `RectSizeLimiter__SetLayoutVertical.c`（35 行 + 35 行，全读）。原文两半**不对称**，⛔ 别「顺手统一」：
//       H-max：`if (maxSize.x > 0 && rect.width >  maxSize.x)                ⇒ SetSize(H, maxSize.x)`
//       H-min：`if (minSize.x > 0 && rect.width <= minSize.x && minSize.x != rect.width) ⇒ SetSize(H, minSize.x)`
//       V-max：`if (maxSize.y > 0 && rect.height <= maxSize.y && maxSize.y != rect.height)⇒ SetSize(V, maxSize.y)`
//       V-min：`if (minSize.y > 0 && rect.height <= minSize.y && minSize.y != rect.height)⇒ SetSize(V, minSize.y)`
//     🔴 **横向 max 那一条没有 `!=`、用的是严格 `>`**，与其余三条**都不一样** —— 照抄，别改写成对称形式。
// · `OnEnable` / `OnDisable` / `SetDirty` = `RectSizeLimiter__OnEnable.c` / `__OnDisable.c` / `__SetDirty.c`：
//     OnEnable  : `base.OnEnable(); if (IsActive()) LayoutRebuilder.MarkLayoutForRebuild(rectTransform);`
//     OnDisable : `m_Tracker.Clear(); MarkLayoutForRebuild(rectTransform); base.OnDisable();`
//     SetDirty  : `if (IsActive()) MarkLayoutForRebuild(rectTransform);`
// · `maxSize` / `minSize` 的 setter（`__set_maxSize.c` / `__set_minSize.c`）：
//     `if (m_maxSize != value) { m_maxSize = value; SetDirty(); }`
//     比较阈值 `DAT_1834b2ba8 = 9.9999994e-11` = `Vector2.kEpsilon²` ⇒ **就是 C# 的 `Vector2 != Vector2`**
//     （`(a-b).sqrMagnitude > kEpsilon * kEpsilon`），所以这里直接用 `!=`，⛔ 别改手写 epsilon。
//
// ============================ 两处「如实记」============================
// ① **`m_Tracker.Add(...)` 的标志位照抄立即数 `0x1000` / `0x2000`**：
//    `.c` 里 `SetSizeWithCurrentAnchors` 之后那一句是
//    `NetworkingPeer__OnMessage(this+0x38, this, rect, 0x1000 /*H*/ , 0)` / `…, 0x2000 /*V*/, 0)`
//    —— 那个函数名是**完全相同代码折叠**的假名（真身 = `DrivenRectTransformTracker.Add`，首参 `this+0x38` = `m_Tracker`），
//    而且它的方法体落在 **RVA `0x4B33B0` = 全局共享空方法桩**上 —— 因为 `DrivenRectTransformTracker`
//    在 **player 构建里是空实现**（`#if UNITY_EDITOR` 包着）。⇒ **这两个立即数对运行时零影响**，
//    所以按原样 `(DrivenRectTransformProperties)0x1000 / 0x2000` 照抄，⛔ 别推断成 `SizeDeltaX/SizeDeltaY`。
// ② **`rectTransform` 字段是「运行时补齐」**：原版由 prefab 序列化赋值（恒非空）；
//    我们是运行时 `AddComponent` ⇒ `OnEnable` 里补一句 `GetComponent<RectTransform>()`。
//    ⚠️ 这不是行为差异：原版那一刻该字段恒非空，补这一句只影响「谁会 NRE」，不影响任何尺寸计算。
//
// ============================ 消费者（本仓用在哪）============================
// `Shell/PracticeModePopup.cs` 的 `Warlord Name`：原版那一颗挂
// `TextMeshProUGUI, ContentSizeFitter, RectSizeLimiter, Button, EverguildTextController`，
// `m_maxSize = (350, 0)` · `m_minSize = (0, 0)`（判据 = `bundle_menus_assets_all` 的两份
// `MonoBehaviour_-6920805963187007684.json` / `MonoBehaviour_-888295288352693547.json`，两份值逐位相同；
// 脚本 pathID = `6305407866017681486`，`m_ClassName = RectSizeLimiter`）。
// ⇒ 那一条「框宽」的**上限 350** 是原版数字，⛔ 不再是我们挑的。
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardPresentation
{
    /// <summary>原版 `RectSizeLimiter`：按 `m_minSize` / `m_maxSize` 夹自身矩形（`ILayoutSelfController`）。
    /// <para>§18·0 口径：这是**复刻**，不是自设计 —— 类名、继承、字段、四个回调的逐句控制流全部有出处（见文件头）。</para></summary>
    public class RectSizeLimiter : UIBehaviour, ILayoutSelfController, ILayoutController
    {
        /// <summary>原版 `public RectTransform rectTransform`（`+0x20`）：被夹的那一格。
        /// 原版由 prefab 序列化赋值；运行时 `AddComponent` 时由 <see cref="OnEnable"/> 补齐（见文件头 ②）。</summary>
        public RectTransform rectTransform;

        /// <summary>原版 `protected Vector2 m_maxSize`（`+0x28` / `+0x2c`）：≤0 的那一轴**不夹**。</summary>
        protected Vector2 m_maxSize;

        /// <summary>原版 `protected Vector2 m_minSize`（`+0x30` / `+0x34`）：≤0 的那一轴**不夹**。</summary>
        protected Vector2 m_minSize;

        /// <summary>原版 `private DrivenRectTransformTracker m_Tracker`（`+0x38`）。player 构建里是空实现。</summary>
        private DrivenRectTransformTracker m_Tracker;

        /// <summary>原版 `public Vector2 maxSize { get; set; }`（`__get_maxSize.c` = 读 +0x28；`__set_maxSize.c` = 变了才 SetDirty）。</summary>
        public Vector2 maxSize
        {
            get { return m_maxSize; }
            set
            {
                if (m_maxSize != value)          // = 原版 `if (kEpsilon² <= Δ²)`（见文件头）
                {
                    m_maxSize = value;
                    SetDirty();
                }
            }
        }

        /// <summary>原版 `public Vector2 minSize { get; set; }`（同上，读 +0x30）。</summary>
        public Vector2 minSize
        {
            get { return m_minSize; }
            set
            {
                if (m_minSize != value)
                {
                    m_minSize = value;
                    SetDirty();
                }
            }
        }

        /// <summary>原版 `protected override void OnEnable()`（`RectSizeLimiter__OnEnable.c`）：
        /// `base.OnEnable(); if (IsActive()) MarkLayoutForRebuild(rectTransform);`
        /// <para>⚠️ 文件头 ② 的那一句补齐就放在这里（原版由 prefab 序列化赋值）。</para></summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();   // 文件头 ②
            if (IsActive()) LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        /// <summary>原版 `protected override void OnDisable()`（`RectSizeLimiter__OnDisable.c`）：
        /// `m_Tracker.Clear(); MarkLayoutForRebuild(rectTransform); base.OnDisable();`</summary>
        protected override void OnDisable()
        {
            m_Tracker.Clear();
            if (rectTransform != null) LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            base.OnDisable();
        }

        /// <summary>原版 `protected void SetDirty()`（`RectSizeLimiter__SetDirty.c`）：
        /// `if (IsActive()) MarkLayoutForRebuild(rectTransform);`</summary>
        protected void SetDirty()
        {
            if (IsActive()) LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        /// <summary>原版 `public void SetLayoutHorizontal()`（`RectSizeLimiter__SetLayoutHorizontal.c`，35 行逐句）。
        /// 🔴 两半**不对称**：max 那半是严格 `>` 且没有 `!=`；min 那半是 `<=` 且带 `!=`。照抄。</summary>
        public void SetLayoutHorizontal()
        {
            if (m_maxSize.x > 0f)
            {
                Rect rect = rectTransform.rect;
                if (rect.width > m_maxSize.x)
                {
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_maxSize.x);
                    // 原版实参 0x1000（见文件头 ①；player 构建里是空实现）
                    m_Tracker.Add(this, rectTransform, (DrivenTransformProperties)0x1000);
                }
            }
            if (m_minSize.x > 0f)
            {
                Rect rect = rectTransform.rect;
                if (rect.width <= m_minSize.x && m_minSize.x != rect.width)
                {
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_minSize.x);
                    m_Tracker.Add(this, rectTransform, (DrivenTransformProperties)0x1000);
                }
            }
        }

        /// <summary>原版 `public void SetLayoutVertical()`（`RectSizeLimiter__SetLayoutVertical.c`，35 行逐句）。
        /// 🔴 这里**两半都用 `<=` 且都带 `!=`**（与横向那半不一样，⛔ 别统一）。</summary>
        public void SetLayoutVertical()
        {
            if (m_maxSize.y > 0f)
            {
                Rect rect = rectTransform.rect;
                if (rect.height <= m_maxSize.y && m_maxSize.y != rect.height)
                {
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, m_maxSize.y);
                    // 原版实参 0x2000（见文件头 ①）
                    m_Tracker.Add(this, rectTransform, (DrivenTransformProperties)0x2000);
                }
            }
            if (m_minSize.y > 0f)
            {
                Rect rect = rectTransform.rect;
                if (rect.height <= m_minSize.y && m_minSize.y != rect.height)
                {
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, m_minSize.y);
                    m_Tracker.Add(this, rectTransform, (DrivenTransformProperties)0x2000);
                }
            }
        }
    }
}
