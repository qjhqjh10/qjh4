// BattleContextPartial_B111E1B.cs — `BattleContext` 的**「这一次誓约二选一最后用了哪个」**那一格
//   （`partial`，与 `Core/BattleContext.cs` 同一个类；🆕 2026-11-01 · 执行代理 `B111_E1B` · `B-85` / `B-86`）
//
// 为什么需要这一格：掷骰挪进引擎之后（`RuleCore.DecideOathChoice`，见 `EffectResolverPartial_B111E1B.cs` 头注），
//   **AI 不再自己掷** —— 它要写进 `AiAction.OathChoiceRecorded/OathChoice` 的那个值，
//   只能是「引擎这一次**真正用了**的答案」。⇒ 引擎在决定完的那一刻记一格，`SimpleAI.PlayCardAsAi`
//   出牌**之后** peek 一次写进动作（→ `NetProtocol.ToWire` → 线上 / 录像 / 回放）。
//
// 🔒 与相邻两格的分工（⛔ 别混）：
//   · `_oathAnswerSeat/_oathAnswerPay/_oathAnswerHas`（`BattleContext.cs:2051-2053`）= **输入**：
//       面板 / AI / 报文**摆进来**的待用答案，`TakeOathAnswer` **一次一取、取完即清**；
//   · `BattleContextPartial_OathPeek.cs` 的 `TryPeekOathAnswer` = 对上面那格的**只读**视图（取之前看）；
//   · **本文件** = **输出**：引擎结算完「这次到底走平版还是誓约版」的**结论**，**不取不清、可重复读**，
//       直到下一次出牌前被 `ClearOathDecisionRecord()` 清掉。
//
// ⚠️ 为什么不塞进 `EffectResolver.cs` / `BattleContext.cs` 主文件：两个文件都被账按 `文件:行号` 大量引用
//   ⇒ 新成员一律住独立的 partial（先例 = `BattleContextRandom.cs` / `BattleContextPartial_OathPeek.cs`）。
// ⚠️ 不参与 `PlayerHash` / `Fingerprint`：本格是**纯派生记录**（它的输入已经参与过指纹），
//   算进去会让「同一局两端指纹不同」凭空出现一格噪声。
namespace RuleEngine
{
    public partial class BattleContext
    {
        int _oathDecisionSeat = -1;
        bool _oathDecisionPay;
        bool _oathDecisionHas;

        /// <summary>**这一次誓约二选一的结论**（引擎决定完之后由 `RuleCore.DecideOathChoice` 写）。
        /// <para>⚠️ 纯记录：不参与结算、不参与指纹；只给「出牌之后要回填动作」的调用方读。</para></summary>
        public void NoteOathDecision(int seat, bool pay)
        {
            _oathDecisionSeat = seat;
            _oathDecisionPay = pay;
            _oathDecisionHas = true;
        }

        /// <summary>看一眼**上一次**的结论（**不取、不清**）。`false` = 没记过 / 座位对不上。
        /// <para>⚠️ 座位对不上 ⇒ 当作「没有」—— 与 <see cref="TryPeekOathAnswer"/> 同一条口径
        /// （一条答案只属于出它的那个座位）。</para></summary>
        public bool TryPeekLastOathDecision(int seat, out bool pay)
        {
            pay = false;
            if (!_oathDecisionHas || _oathDecisionSeat != seat) return false;
            pay = _oathDecisionPay;
            return true;
        }

        /// <summary>把「上一次的结论」清掉（出牌**之前**调一次，防残答报到下一手 ——
        /// `AiAction` 将来若复用同一个对象，`oathRecorded=true` 会把一次**没发生过**的选择过网）。</summary>
        public void ClearOathDecisionRecord()
        {
            _oathDecisionHas = false;
            _oathDecisionSeat = -1;
            _oathDecisionPay = false;
        }
    }
}
