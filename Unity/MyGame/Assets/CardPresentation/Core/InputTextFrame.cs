// InputTextFrame.cs — 执行写手 `B144-A`（2026-11-05）· `INPUT-BACKEND-MIGRATE` 第一拍 · 打字输入的唯一入口
//
// ✅ 已复刻（2026-11-05 · B144-A · 原版形状 = 每帧读 `Input.inputString`；调度台 2026-11-05 裁定「按单一每帧消费」）
//
// ============================ 为什么要有它 ============================
// 迁到旧 Input Manager 之后，打字**没有事件**了：`Keyboard.current.onTextInput` 那个回调不存在，
// 旧输入只有 `Input.inputString`（**每帧一个串**）。于是有三处要改（`PointerLayer` / `DeckRuntime` / `TextInputField`），
// 而它们**各自每帧都会读一次** —— 如果三处都直接读 `Input.inputString`，同一帧按下的那个字符会被**三处各收一遍**
// （症状：文本框里一个字变成三个，且**没有任何报错**）。所以取字只能**一处取、一次取**。
//
// ============================ 契约（三条，缺一即静默双打）============================
//   ① **同一帧只取一次**：本帧第一次 `Take` 之后缓冲就空了，第二次起返回空串；
//   ② **只有「当前焦点」能取**：`Claim(owner)` 的那个对象才拿得到字，其余**一个字符都不给**（不是「也拿一份」）；
//   ③ **换焦点那一帧重新取**：`Claim` 会把帧号复位 ⇒ 新焦点从下一帧起正常收字（旧焦点已被 `Release`）。
//
// ============================ 灭自证 ============================
//   `B144_A_PhysLayer_Assert` 的 F 组：`Claim(A); Take(A)` 得到字、`Take(B)` 必须空 ⇒ 删掉 owner 判定就红；
//   `Claim(A); Take(A); Take(A)` 第二次必须空 ⇒ 删掉「取完清空」就红。
//
// ⚠️ 覆盖边界：`Input.inputString` 在**批处理 / 无键盘**下恒为空串（不是 null）⇒ 本类把它归一成 `""`，
//    自检要喂字仍然走各自的 `FeedText` / `SetTypedForTest` 口，⛔ 不要靠伪造键盘。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>旧输入下**唯一**的「这一帧打进来的字」入口。三处输入框共用，谁 `Claim` 谁取。</summary>
    public static class InputTextFrame
    {
        static object _owner;
        static int _frame = -1;
        static string _buf;

        /// <summary>把**当前焦点**交给 <paramref name="owner"/>（同一时刻只有一个）。会复位帧号 ⇒ 本帧重新取。</summary>
        public static void Claim(object owner)
        {
            _owner = owner;
            _frame = -1;
            _buf = null;
        }

        /// <summary>交还焦点（只有持有者调用才生效 —— 别人 `Release` 不会把当前焦点清掉）。</summary>
        public static void Release(object owner)
        {
            if (ReferenceEquals(_owner, owner)) { _owner = null; _frame = -1; _buf = null; }
        }

        /// <summary>当前焦点是不是 <paramref name="owner"/>（自检读口 / 需要判断「该不该处理按键」时用）。</summary>
        public static bool IsOwner(object owner) { return ReferenceEquals(_owner, owner); }

        /// <summary>自检口：把「本帧打进来的字」钉成给定值（批处理/自检里没有真键盘，`Input.inputString` 恒空串）。
        /// 生产路径**从不调用**。⚠️ 它只替换本帧的缓冲，**不改变** `Claim`/`Take` 的任何边界。</summary>
        public static void SeedForTest(string s)
        {
            _frame = Time.frameCount;
            _buf = s ?? "";
        }

        /// <summary>取走**本帧**打进来的字。非焦点 ⇒ 空串；同一帧第二次取 ⇒ 空串（**这是「不会双打」的机器保证**）。</summary>
        public static string Take(object owner)
        {
            if (!ReferenceEquals(_owner, owner)) return "";
            int f = Time.frameCount;
            if (f != _frame) { _frame = f; _buf = Input.inputString ?? ""; }
            string s = _buf;
            _buf = "";                       // 取完清空 ⇒ 本帧后续调用拿空
            return s;
        }
    }
}
