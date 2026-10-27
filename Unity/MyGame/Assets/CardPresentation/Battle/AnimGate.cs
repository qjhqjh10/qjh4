// AnimGate.cs — 「这张卡还在不在、要不要继续演」的**唯一共用闸**
//
// 对应原版 `CardScript.CanContinueAnim()`（`decomp_full/CardScript__CanContinueAnim.c` 全文 3 句）：
//     return this != null && gameObject != null && gameObject.activeSelf;
// **11 个协程**统一拿它当「这一步还演不演」的闸（`CardScript._*Anim_d__*` 各 state 起手）。
//
// 🔴 我们原来的形状（R24 报告 §15 记的）：
//   · **0 个声明** —— 没有任何共用判据；
//   · `BattleDriver.cs` 里各写 `ViewAt(...) == null`（现读 29 处取自 `ViewAt` 的局部量 + 若干内联），
//     而且**一律不判 `activeSelf`** ⇒ 视图还在场、只是被 `SetActive(false)` 隐藏时，
//     我们**照样演**（原版不演）。
//
// ⚠️ 本文件只提供**判据**。接线按「一个文件一个写手」逐份推进 —— 现状与待接清单见
//   `资料/完全复刻/报告/B88_E5_BattleDriver.md` §「第二批」。
//
// ⚠️ 语义边界（⛔ 别拿它去过宽地替换）：
//   · 它回答的是「**演不演**」；**不是**「引擎里这张卡还在不在」（那看 `UnitState`）。
//   · 只该替换那些「取视图 → 立刻为了播一段动画/补间而早退」的守卫；
//     ⛔ 别拿它去替换「取视图来读坐标 / 读数据」的地方（隐藏的卡坐标照样可以读）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `CardScript.CanContinueAnim()` 的共用等价物。</summary>
    public static class AnimGate
    {
        /// <summary>`this != null && gameObject != null && gameObject.activeSelf`。
        /// <para>⚠️ `Component` 的 `== null` 已经覆盖「Unity 对象被销毁」那一档（伪 null），
        /// 但**不覆盖**「对象在、GameObject 被 `SetActive(false)`」—— 那正是我们缺的那一半。</para></summary>
        public static bool CanContinue(CardView v)
        {
            if (v == null) return false;
            var go = v.gameObject;
            if (go == null) return false;
            return go.activeSelf;
        }

        /// <summary>同 <see cref="CanContinue(CardView)"/>，收任意 `Component`（卡以外的载体也能用）。</summary>
        public static bool CanContinue(Component c)
        {
            if (c == null) return false;
            var go = c.gameObject;
            if (go == null) return false;
            return go.activeSelf;
        }

        /// <summary>自检口：把 `CanContinue` 的判据原样暴露成「(在不在, 激活吗)」两格，
        /// 断言用它可以分辨「null / 隐藏」两种不演的原因。</summary>
        public static void Probe(Component c, out bool alive, out bool active)
        {
            alive = c != null;
            active = alive && c.gameObject != null && c.gameObject.activeSelf;
        }
    }
}
