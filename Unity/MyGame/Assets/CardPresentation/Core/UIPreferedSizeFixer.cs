// UIPreferedSizeFixer.cs — 原版 uGUI 件 `UIPreferedSizeFixer`（preferredWidth 跟着「源件 rect 的高」走）
// 🆕 2026-10-29 · 执行代理 `F5` · 账 `未分类_其他:702/703`
//
// ── 判据（三源同读，2026-10-29）──────────────────────────────────────────────────
//  · 🥇 方法体：`decomp_full/UIPreferedSizeFixer__ApplyConfig.c`（全文）
//              `…__CalculateLayoutInputVertical.c`（**空体**）· `…__OnValidate.c`
//              七个 getter：`__get_{minWidth,preferredWidth,flexibleWidth,minHeight,preferredHeight,flexibleHeight,layoutPriority}.c`
//  · 🥈 带字段名的那份：`cpp2il/out_cpp2il_dev_ilspy/UIPreferedSizeFixer.cs:1-30`（四个字段名 + 基类 `UIBehaviour, ILayoutElement`）
//  · 字段偏移互证（`.c`）：`+0x20`=horizontalLayoutGroup · `+0x28`=rectTransform ·
//                        `+0x30`=layoutElement · `+0x38`=preferedWidthFollowHeight
//  🔴 **`__CalculateLayoutInputHorizontal.c` 的内容逐字就是 `ApplyConfig`**（首行 = `UIPreferedSizeFixer__ApplyConfig`）
//     —— 那不是「本方法体读不到」，而是**原版两个方法是同一份体**（ILSpy 两处逐句相同，见 §「它到底算什么」）。
//     ⇒ 两份都按同一段实现，⛔ 别只挑一份做。
//
// ── 它到底算什么（逐句对 `.c` / ILSpy）───────────────────────────────────────────
//  ```
//  if (preferedWidthFollowHeight) {
//      horizontalLayoutGroup.CalculateLayoutInputHorizontal();   // 虚槽 +0x2f8
//      horizontalLayoutGroup.CalculateLayoutInputVertical();     // 虚槽 +0x308
//      horizontalLayoutGroup.SetLayoutHorizontal();              // 虚槽 +0x388
//      horizontalLayoutGroup.SetLayoutVertical();                // 虚槽 +0x398
//      rect = rectTransform.rect;
//      layoutElement.preferredWidth = rect.height;               // 虚槽 +0x378，实参 = rect 的第 4 个 float（= 高）
//  }
//  ```
//  ⚠️ ILSpy 把最后那句打成 `float num = default; layoutElement.preferredWidth = num;`
//     —— 那是**丢实参**（`.c` 里 `get_rect` 四个 float 的**第 4 个** `uStack_c` 被直接喂进 setter）。
//     真值 = **rect.height**，⛔ 别照 ILSpy 写 0。
//  ⇒ 语义：「本件的**首选宽** = **源 `RectTransform` 的当前高**」，且**只在开关开着时**做；
//     开关关着 ⇒ **四步布局与赋值一句都不跑**（不是「跑完不赋值」）。
//
// ── 七个 getter ────────────────────────────────────────────────────────────────
//  全部**只是转发**到 `layoutElement.<同名属性>`（`.c` 各一条虚调用、无分支）。
//  ⚠️ `layoutElement` 为空 ⇒ 原版走 `FUN_1803f47a0`（= **抛 NRE**）⇒ 本实现直接成员访问，照抄（⛔ 不加兜底）。
//
// ── 挂载点（如实记）──────────────────────────────────────────────────────────
//  原版它挂在**远端 LiveOps prefab** 上（与 `ContentSizeFitterMinMax` 同族窗口）。我们今天的窗口
//  **零挂载点**；仍要建（铁律 11）。`OnValidate` 那条自动补引用照抄（它在编辑器里跑）。
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardPresentation
{
    /// <summary>原版 `UIPreferedSizeFixer : UIBehaviour, ILayoutElement` —— 「首选宽 = 源件 rect 的高」那一件。
    /// <para>⚠️ 它**不实现** `ILayoutController`（没有 SetLayoutHorizontal/Vertical）—— 只做
    /// 「跑一遍 HLG 的布局四步 + 把源件的高写成本件的 preferredWidth」，靠 `layoutElement` 让父级布局去用它。</para></summary>
    [AddComponentMenu("Layout/UI Prefered Size Fixer")]
    public class UIPreferedSizeFixer : UIBehaviour, ILayoutElement
    {
        /// <summary>原版 `horizontalLayoutGroup`（`+0x20`）—— 被驱动的那一个 HLG。</summary>
        [SerializeField] private HorizontalLayoutGroup horizontalLayoutGroup;
        /// <summary>原版 `rectTransform`（`+0x28`）—— **源**：它的 `.rect.height` 是首选宽的来源。</summary>
        [SerializeField] private RectTransform rectTransform;
        /// <summary>原版 `layoutElement`（`+0x30`）—— 被写入 `preferredWidth` 的那一件。</summary>
        [SerializeField] private LayoutElement layoutElement;
        /// <summary>原版 `preferedWidthFollowHeight`（`+0x38`，**原版拼写就是 `prefered`**，照抄）。</summary>
        [SerializeField] private bool preferedWidthFollowHeight;

        // ---- ILayoutElement：七个 getter 全部**纯转发**到 layoutElement（判据见文件头）----
        public virtual float minWidth { get { return layoutElement.minWidth; } }
        public virtual float preferredWidth { get { return layoutElement.preferredWidth; } }
        public virtual float flexibleWidth { get { return layoutElement.flexibleWidth; } }
        public virtual float minHeight { get { return layoutElement.minHeight; } }
        public virtual float preferredHeight { get { return layoutElement.preferredHeight; } }
        public virtual float flexibleHeight { get { return layoutElement.flexibleHeight; } }
        public virtual int layoutPriority { get { return layoutElement.layoutPriority; } }

        /// <summary>原版私有 `ApplyConfig()` = 下面 `CalculateLayoutInputHorizontal()` 的**同一份体**
        /// （见文件头 🔴：两份产物逐字相同）。</summary>
        private void ApplyConfig() { RunFix(); }

        /// <summary>原版 `CalculateLayoutInputHorizontal()` —— 与 `ApplyConfig()` 同一份体。
        /// ⚠️ 名字叫 Horizontal，但做的事是**给 HLG 跑四步、再按源件高写 preferredWidth**。</summary>
        public virtual void CalculateLayoutInputHorizontal() { RunFix(); }

        /// <summary>原版 `CalculateLayoutInputVertical()` = **空体**（`.c` 里只有一句 return）。</summary>
        public virtual void CalculateLayoutInputVertical() { }

        /// <summary>两份方法体共用的那一段（判据只此一处；⛔ 别在两处各写一遍）。</summary>
        private void RunFix()
        {
            if (!preferedWidthFollowHeight) return;                 // 开关关着 ⇒ **一句都不跑**
            // 四步布局：顺序照原版（先 Input 两档、再 Set 两档）
            horizontalLayoutGroup.CalculateLayoutInputHorizontal();
            horizontalLayoutGroup.CalculateLayoutInputVertical();
            horizontalLayoutGroup.SetLayoutHorizontal();
            horizontalLayoutGroup.SetLayoutVertical();
            Rect rect = rectTransform.rect;
            layoutElement.preferredWidth = rect.height;             // `.c`：`get_rect` 的第 4 个 float 直喂 set_preferredWidth
        }

        /// <summary>原版 `OnValidate()`：两个引用为空时**自己补上**（`GetComponent` 抓同节点的件）。
        /// ⚠️ 2026-10-31（`B104` · player 构建实测）：**不能写 `override`** —— `UIBehaviour` 没有这个方法，
        ///    编辑器程序集里碰巧编过、**player 构建 `CS0115`**（`PlayerBuild.Run` 日志 :2473）。
        ///    Unity 按名字调消息方法，`protected void OnValidate()` 即可。</summary>
        protected void OnValidate()
        {
            if (!rectTransform) rectTransform = GetComponent<RectTransform>();
            if (!layoutElement) layoutElement = GetComponent<LayoutElement>();
        }

        // ============================================================ 自检口（只读；⛔ 生产路径别调）
        /// <summary>自检用：按原版名摆好三个引用 + 开关（**只在自检里调**）。</summary>
        public void ConfigureForTest(HorizontalLayoutGroup hlg, RectTransform src, LayoutElement le, bool follow)
        {
            horizontalLayoutGroup = hlg; rectTransform = src; layoutElement = le; preferedWidthFollowHeight = follow;
        }

        /// <summary>自检用：开关开着的话跑一次 `CalculateLayoutInputHorizontal()`（= 生产路径那份体）。</summary>
        public void RunFixForTest() { CalculateLayoutInputHorizontal(); }
    }
}
