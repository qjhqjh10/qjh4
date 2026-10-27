// BackgroundOverDrawController.cs — 原版 `BackgroundOverDrawController`（**113 实例 / 17 个 bundle**）的等价实现：
//   **一条全局栈**，保证「同一时刻只画最上面那一个整屏底」。
//
// ============================ 出处（第一权威 = 反编译，逐句现读）============================
// · `d:/2/tools/decomp_full/BackgroundOverDrawController__OnEnable.c`：
//     若栈（静态 `List<…>`，`DAT_1842c0230 + 0xb8`）非空 ⇒ 取**栈顶**那颗，`Behaviour.set_enabled(top, **0**)`
//     ⇒ 然后 `List.Add(自己)`（`+0x1c` 是 `_size` 的自增）。
// · `..._OnDisable.c`：
//     取栈顶；**只有「栈顶 == 自己」**时才 `set_enabled(新栈顶, **1**)`（`iVar6 = _size - 2` 那一格）
//     ⇒ 再从 List 里删掉自己；末尾把自己也 `enabled = true`。
//   ⚠️ 两个方法体里**一个 `m_Color` 都没写**（只 `Behaviour.set_enabled`）⇒ 它**不动透明度**：
//     原版 `AllianceMemberOptionsPopup` 那颗整屏底 `m_Color = (0,0,0,0)` 仍然透明，它只负责「谁吃射线/谁被画」。
//
// ============================ 🔶 与我们的差（自己设计，非原版）============================
// 🔶 自己设计（非原版）：触发时机从 Unity 生命周期（`OnEnable` / `OnDisable`）改成**显式 `Push` / `Pop`** ·
//   为什么必须自己定：本仓的 UI 件是 `MenuDraw.Rect` 在**编辑模式**下建出来的，而 `AddComponent` 在编辑模式下
//   **不触发** `OnEnable`（本仓踩过的坑，见 `CLAUDE.md` §三「编辑模式下 AddComponent 不触发 Awake/OnEnable」）
//   ⇒ 照生命周期写，自检里会**静默不跑**（比不写更糟）。窗口的生死在本仓由 `GameWindow.Open/Close` 显式驱动
//   ⇒ 就在那两处 Push/Pop，语义与原版逐句等价。调度台 2026-10-27 批（B88-E7c 执行）
// · 另：原版那颗件的 `Image` 在 uGUI 上；我们的件是 `ImageQuad`（`MonoBehaviour`）⇒ `enabled` 是**同一个开关**。
//
// ============================ 已知的边界 ============================
// ⚠️ 今天**只有一扇窗接上**（`Shell/AllianceMemberOptionsPopup.cs` 的整屏底）—— 因为本仓**窗与窗不叠整屏底**
//   （同族 `GenericOptionsPanel` 上那颗控制器在原版里就是**禁用**的）。⇒ 可见差仍然 = 0，
//   接上它只是把「原版有这条规则」这件事变成代码，将来多一扇叠底窗时自动生效。
using System.Collections.Generic;

namespace CardPresentation
{
    /// <summary>整屏底的**全局栈**（原版 `BackgroundOverDrawController` 的等价实现）。见文件头。</summary>
    public static class BackgroundOverDrawController
    {
        static readonly List<ImageQuad> _stack = new List<ImageQuad>();

        /// <summary>栈深（自检读口）。</summary>
        public static int Depth { get { return _stack.Count; } }
        /// <summary>栈顶那颗（没有 ⇒ null）。</summary>
        public static ImageQuad Top { get { return _stack.Count > 0 ? _stack[_stack.Count - 1] : null; } }
        /// <summary>某颗现在在不在栈里。</summary>
        public static bool Contains(ImageQuad backdrop) { return backdrop != null && _stack.Contains(backdrop); }

        /// <summary>= 原版 `OnEnable`：**先把当前栈顶那颗关掉**，再把自己入栈（自己保持开着）。
        /// <para>幂等：已经在栈里 ⇒ 先摘掉再压栈（相当于「再开一次」），⛔ 不会重复入栈。</para></summary>
        public static void Push(ImageQuad backdrop)
        {
            PurgeDestroyed();
            if (backdrop == null) return;
            var top = Top;
            if (top != null && top != backdrop) top.enabled = false;
            _stack.Remove(backdrop);
            _stack.Add(backdrop);
            backdrop.enabled = true;
        }

        /// <summary>= 原版 `OnDisable`：出栈；**只有自己曾是栈顶**才把新的栈顶恢复成 `enabled = true`；
        /// 自己无论如何都恢复成 `enabled = true`（原版末尾那一句）。不在栈里 ⇒ 什么都不做。</summary>
        public static void Pop(ImageQuad backdrop)
        {
            if (ReferenceEquals(backdrop, null)) return;
            // 🔴 **2026-10-28（B88_F2）修崩**：`Pop` 抛 `MissingReferenceException: The object of type
            //   'CardPresentation.ImageQuad' has been destroyed …`（`ShellScene.Run` 在
            //   `WindowsManager.CloseAllWindows` → `AllianceMemberOptionsPopup.Close` 时炸整轮）。
            //   真因 = 栈里**留着已经被销毁的件**：`AllianceMemberOptionsPopup.Build()` 每次都
            //   `MenuDraw.ClearChildren` 重建整屏底（旧的那颗就此销毁），而当时**没有任何一处把旧件摘出栈**
            //   ⇒ 下一次 `Pop` 走到「恢复新栈顶」那一句、对死件设 `enabled` ⇒ 抛。
            //   语义不变（⛔ 没有改成「什么都恢复」）：原版那条规则一字未动，只是把「已销毁」这一档
            //   当成「已经不在栈上」（它对 `set_enabled` 无从谈起，也不可能再是「谁画在上面」）。
            bool alive = backdrop != null;      // Unity 的 `==` 重载：已销毁 ⇒ false
            int i = IndexOfRef(backdrop);
            if (i < 0) { if (alive) backdrop.enabled = true; return; }
            bool wasTop = i == _stack.Count - 1;
            _stack.RemoveAt(i);
            PurgeDestroyed();
            if (alive) backdrop.enabled = true;
            if (wasTop && _stack.Count > 0) _stack[_stack.Count - 1].enabled = true;
        }

        /// <summary>把**已经销毁**的件从栈里摘掉。原版没有这一步 —— 那边栈项跟着 `GameObject` 的
        /// `OnDisable` 自己走，不会在栈里留下死件；我们的件由 `MenuDraw.ClearChildren` 直接销毁，
        /// 必须在这里兜住（否则下一次 `Push`/`Pop` 会对死件设 `enabled` 抛 `MissingReferenceException`）。
        /// 摘掉不改变语义：已销毁的件既不能再被 `set_enabled`，也不再是「谁画在上面」。</summary>
        static void PurgeDestroyed()
        {
            for (int i = _stack.Count - 1; i >= 0; i--) if (_stack[i] == null) _stack.RemoveAt(i);
        }

        /// <summary>按**引用**找下标。⛔ 不用 `List.IndexOf` —— 它对已销毁的 `UnityEngine.Object`
        /// 会走 `Object.Equals`（比 `GetInstanceID`），死件那一路不可靠；这里是同一条链上的崩点。</summary>
        static int IndexOfRef(ImageQuad q)
        {
            if (ReferenceEquals(q, null)) return -1;
            for (int i = 0; i < _stack.Count; i++) if (ReferenceEquals(_stack[i], q)) return i;
            return -1;
        }

        /// <summary>清空并**把所有件恢复成开着**（换场景 / 自检收尾用 —— 原版没有这一步，
        /// 但原版的栈跟着场景走、我们的是静态字段 ⇒ 不清会指向上一个场景的死件）。</summary>
        public static void ResetAll()
        {
            for (int i = 0; i < _stack.Count; i++) if (_stack[i] != null) _stack[i].enabled = true;
            _stack.Clear();
        }
    }
}
