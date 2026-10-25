// RaycastFilter.cs — 原版 `RaycastFilter`（**静态类**：1 个 Type + 1 张允许表 + 7 个方法）
//
// 账行 → `资料/完全复刻/账/未分类_其他.tsv` 第 230 行（`CustomRaycaster.Raycast`）
// 判据正本 → `资料/完全复刻/报告/E6_N10五条+未分类_第2轮.md` §②·5（谓词逐句）· §③·3 请求 1（落点）
//
// 原版一手出处（**逐字读过全文**）：
//   `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/RaycastFilter.cs`（整类 253 行）
//     :60 / :62  两个静态字段（`filteredType` · `allowedObjects`），静态构造里 `allowedObjects = new List<GameObject>()`
//     :64-73     `SetRaycastFilter<T>()` ⇒ `filteredType = typeof(T)`
//     :75-95     `Register(GameObject)`  ⇒ `allowedObjects.Add(obj)`（**不去重**，原版如此）
//     :97-100    `UnRegister(GameObject)` ⇒ `allowedObjects.Remove(obj)`
//     :102-113   `ClearRaycastFilters()` ⇒ `filteredType = null; allowedObjects.Clear();`
//     :115-161   `FilterRaycastResults(List<RaycastResult>)` ⇒ `resultAppendList.RemoveAll(<下面那个谓词>)`
//     :163-220   `RegisterObjectAndChildrenGraphics(GameObject)` ⇒ `Add(obj)` + 对
//                `obj.GetComponentsInChildren<Graphic>(true)` 逐个 `Add(g.gameObject)`
//     :222-246   `UnRegisterObjectAndChildrenGraphics(GameObject)` ⇒ 同形反向（先 Remove 自己、再逐个 Remove 子 Graphic）
//   `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/CustomRaycaster.cs:7-11`（**唯一调用点**）：
//     `base.Raycast(eventData, resultAppendList); RaycastFilter.FilterRaycastResults(resultAppendList);`
//   `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/TutorialManager.cs:1371-1397`（`AddPointerAction`）：
//     `if (!filterRaycast) ClearRaycastFilters(); else { ClearRaycastFilters(); RegisterObjectAndChildrenGraphics(target); }`
//   `TutorialManager.cs:1308-1316 DestroyPointer` · `:1329-1337 DisablePointer` · `TutorialMessageWindowPopUp.cs:133`：
//     三处 `ClearRaycastFilters()`
//
// ── 那个谓词到底说什么（`RemoveAll` 的语义 = **谓词为真 ⇒ 剔除这一条**）──────────────
//   `FilterRaycastResults` 的谓词（cpp2il 把两条 `if` 打散成 `goto IL_00b9`，按 `RemoveAll` 形态还原）：
//     ① `allowedObjects.Count > 0 && !allowedObjects.Contains(r.m_GameObject)` ⇒ **剔除**
//     ② `filteredType != null && r.m_GameObject.GetComponent(filteredType) == null` ⇒ **剔除**
//     其余 ⇒ 保留。
//   ⇒ 本文件的等价写法（<see cref="Allows"/>）：**保留**当且仅当
//     （表空 **或** 表里有它）**且**（`filteredType == null` **或** 它有那个组件）。
//
// 🔴 三条必须照做的真值（写坏会静默把整层菜单点死）：
//   ① **空表 = 不过滤**。教程指针撤掉那一刻原版 `ClearRaycastFilters()`，之后一切照常。
//      ⛔ 别实现成「空表 = 全禁」—— 那会让**整层菜单点不动**，而且没有任何报错。
//   ② `SetRaycastFilter<T>()` 是**死码**：cpp2il 全树 grep **0 调用点**（判据 §②·5 末段）。
//      ⇒ 出货里 `filteredType` **恒 null** ⇒ 谓词②永远不生效。方法按形状保留（账里标「死码」），
//      ⛔ 别给它编一个调用点、也别据此认为过滤会按类型生效。
//   ③ 我们**没有** `RaycastResult` 这条链（指针层自己扫 `WindowButton`，不走 uGUI 射线，
//      判据 → `Shell/PointerLayer.cs` 文件头）⇒ 原版那个 `FilterRaycastResults(List<RaycastResult>)`
//      在我们的等价位置 = **在候选命中表进入排序之前逐条问 <see cref="Allows"/>**
//      （调用点 = `PointerLayer.HitButton` 的 `foreach` 第一句）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// 「教程期只许点某个子树」的那张**全局允许表**（原版 `RaycastFilter`，**静态**、进程级）。
    /// <para>与本工程的分工：原版它是 `CustomRaycaster`（自定义 `GraphicRaycaster`）的后置过滤器；
    /// 我们的指针层不走 uGUI 射线 ⇒ 由 `PointerLayer.HitButton` 在排序前调 <see cref="AllowsHit"/>。</para>
    /// <para>⛔ 它不改引擎状态、不发事件（与 `TraitEffectController` / `MaterialFader` 同一条纪律）。</para>
    /// </summary>
    public static class RaycastFilter
    {
        // ── 原版 2 个静态字段（名字逐字对应 `RaycastFilter.cs:60/62`）────────────────────────
        /// <summary>原版 `:60`（private static）。⚠️ **恒 null**（`SetRaycastFilter` 全库 0 调用点，见文件头 ②）。</summary>
        static System.Type filteredType;
        /// <summary>原版 `:62`（private static；静态构造里 `new List&lt;GameObject&gt;()`）。</summary>
        static readonly List<GameObject> allowedObjects = new List<GameObject>();

        // ── 观测 / 诊断读口（**我们的**，判据 §②·5 的灭自证要用）────────────────────────────
        /// <summary>现在**有没有**在过滤（表非空 或 `filteredType` 非 null）。</summary>
        public static bool Active { get { return filteredType != null || allowedObjects.Count > 0; } }
        /// <summary>允许表里的条数（原版 `allowedObjects.Count`）。</summary>
        public static int AllowedCount { get { return allowedObjects.Count; } }
        /// <summary>`filteredType` 的只读镜像（断言应断 **null** —— 那一半是死码）。</summary>
        public static System.Type FilteredTypeForTest { get { return filteredType; } }

        // ════════════════════════════════════════════════════════════════════════════
        // 原版方法（顺序与 `RaycastFilter.cs` 一致）
        // ════════════════════════════════════════════════════════════════════════════

        /// <summary>原版 `:64-73`。🔴 **死码**（全库 0 调用点，见文件头 ②）：按形状保留，⛔ 别加调用点。</summary>
        public static void SetRaycastFilter<T>() { filteredType = typeof(T); }

        /// <summary>原版 `:75-95`：`allowedObjects.Add(obj)`。⚠️ 原版**不去重**（同一颗注册两次就有两条），照抄。</summary>
        public static void Register(GameObject obj) { allowedObjects.Add(obj); }

        /// <summary>原版 `:97-100`：`allowedObjects.Remove(obj)`（只摘一条 —— `List.Remove` 的语义）。</summary>
        public static void UnRegister(GameObject obj) { allowedObjects.Remove(obj); }

        /// <summary>原版 `:102-113`：`filteredType = null;` + 清表。
        /// 🔴 这是「教程指针撤掉」的唯一出口（`DestroyPointer` / `DisablePointer` / 简报窗那三处）。
        /// ⛔ 别在里面加「顺手保留上一次的表」之类的优化 —— 保留 = 玩家永远点不动别处。</summary>
        public static void ClearRaycastFilters()
        {
            filteredType = null;
            allowedObjects.Clear();
        }

        /// <summary>原版 `:163-220`：`Register(obj)` + 对 `obj` **全子树里每一个 `Graphic`** 逐个注册。
        /// <para>⚠️ **等价替换（我们的）**：原版扫 `Graphic`（uGUI 的射线件）；我们这套「画出来的件」是
        /// `ImageQuad` / `Label`（都不继承 `Graphic`）⇒ 用「**全子树 Transform**」这个 **superset**。
        /// 为什么可以放宽：本文件只在「候选命中表进入排序前」被问，而候选表本身已经是
        /// `WindowButton`（命中区 = 它自己那颗 `ImageQuad`）⇒ 多注册进来的纯容器节点**不会被问到**；
        /// 反过来若只注册 `ImageQuad`/`Label`，会漏掉「按钮根自己也吃指针」的那些件。</para>
        /// <para>⚠️ `includeInactive: true`（原版就是 `includeInactive: true`）——
        /// 非当前页的页签是 `SetActive(false)` 的，教程指针正好可能落在它上面。</para></summary>
        public static void RegisterObjectAndChildrenGraphics(GameObject obj)
        {
            if (obj == null) return;
            Register(obj);
            Transform[] all = obj.GetComponentsInChildren<Transform>(true);   // 见上面那条等价替换
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null) Register(all[i].gameObject);
        }

        /// <summary>原版 `:222-246`：先 `if (Contains(obj)) Remove(obj)`，再对子 `Graphic` 逐个同形。
        /// 等价替换同 <see cref="RegisterObjectAndChildrenGraphics"/>（子树 Transform）。</summary>
        public static void UnRegisterObjectAndChildrenGraphics(GameObject obj)
        {
            if (obj == null) return;
            UnRegister(obj);
            Transform[] all = obj.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null) UnRegister(all[i].gameObject);
        }

        /// <summary>**谓词的等价**（见文件头）：这一条射线结果**该保留吗**。
        /// `(表空 || 含它) &amp;&amp; (filteredType == null || 它有那个组件)`。</summary>
        public static bool Allows(GameObject go)
        {
            if (allowedObjects.Count > 0 && (go == null || !allowedObjects.Contains(go))) return false;
            if (filteredType != null && (go == null || go.GetComponent(filteredType) == null)) return false;
            return true;
        }

        /// <summary>指针层的命中判据（= 原版 `RemoveAll` 在本工程的那一跳）。
        ///
        /// <para>🔴 **为什么要传两个 GameObject**：原版被过滤的是**射线结果**，它的 `m_GameObject`
        /// 是**那颗 `Graphic` 自己**；而我们的候选表里一颗 `WindowButton` 的命中区是**它子树里的
        /// 那颗 `ImageQuad`**（`PointerLayer.HitQuad` 用的是 `GetComponentInChildren`，见那里的注释）
        /// ⇒ 「按钮根」与「真正被射到的那颗图」**常常不是同一个 GameObject**。
        /// 教程的目标可能是**两者之一**（`Resolve()` 返回页签按钮 / 导航按钮 / 任务行 Collect 三种粒度），
        /// 所以两颗任一被登记即放行。⚠️ 这不是放宽：`RegisterObjectAndChildrenGraphics` 本身就把
        /// 「目标 + 它全子树」都登记了 ⇒ 只要目标是这颗按钮或它的祖先，两颗都在表里。</para>
        ///
        /// <para>⛔ 别改成只判 `button`：`MenuDraw.AddHit` 建的命中 quad 是**子件**，
        /// 于是「教程目标 = 按钮里的某颗图」（例如 `Unlock Button` 那样只有图标被指到的件）会**判不中** ⇒
        /// 玩家点不动教程要他点的那一颗（软卡死）。</para></summary>
        public static bool AllowsHit(GameObject button, GameObject hitGraphic)
        {
            return Allows(button) || Allows(hitGraphic);
        }
    }
}
