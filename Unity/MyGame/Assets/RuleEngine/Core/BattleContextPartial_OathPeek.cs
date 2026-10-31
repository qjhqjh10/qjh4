// BattleContextPartial_OathPeek.cs — `BattleContext.TryPeekOathAnswer(...)`（`partial`，与 `Core/BattleContext.cs` 同一个类）
//
// 🆕 2026-10-31（`B110_R3` · 账 `N07`）：给「表现层 / 录像侧」一个**只读**口，而不必 `TakeOathAnswer`
//   （那个取完即清，而引擎自己还要取）。
// 判据：主文件那四格 `_oathAnswerSeat` / `_oathAnswerPay` / `_oathAnswerHas` 与 `SetOathAnswer` /
//   `TakeOathAnswer` / `HasOathAnswer` / `ClearOathAnswer`（`RuleEngine/Core/BattleContext.cs:2048-2080`，2026-10-31 现读）。
// 为什么单开一个文件：`BattleContext.cs` 被账按 `文件:行号` 引用 40+ 处 ⇒ **主文件一行不动**（先例 =
//   `BattleContextPartial_B88E14.cs` / `BattleContextRandom.cs` / `BattleContextTurnCounter.cs`）。
// 谁用：`CardPresentation/Battle/BattleDriver.cs` 的 `LocalAct`（出牌落地**之前**把答案抓进 `AiAction`，
//   让它跟着动作过网 / 进录像）。⛔ 别拿它去替引擎「取」答案 —— 那是 `TakeOathAnswer` 的活。
namespace RuleEngine
{
    public partial class BattleContext
    {
        /// <summary>看一眼那一格誓约答案（**不取、不清**）。`false` = 没有 / 座位对不上。
        /// <para>语义与 <see cref="TakeOathAnswer"/> **前半段**逐字相同（座位不符 ⇒ 不算有答案），
        /// 区别只在**不清**。</para></summary>
        public bool TryPeekOathAnswer(int seat, out bool pay)
        {
            pay = false;
            if (!_oathAnswerHas || _oathAnswerSeat != seat) return false;
            pay = _oathAnswerPay;
            return true;
        }
    }
}
