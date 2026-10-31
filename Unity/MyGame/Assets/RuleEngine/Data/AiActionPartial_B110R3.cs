// AiActionPartial_B110R3.cs — `AiAction` 的**誓约二选一答案**两格（`partial`，与 `Data/SimpleAI.cs` 同一个类）
//
// 🆕 2026-10-31（`B110_R3` · 账 `N07`）。为什么单开一个文件（与 `BattleContext` 那几份 partial 同一条纪律）：
//   `Data/SimpleAI.cs` 被账按 `文件:行号` 引用 **41 处**（且其中有 `:147` 这种就在 `AiAction` 后面几十行的落点）
//   ⇒ 往 `AiAction` 类体里插成员会让那 41 处**整体漂移**。改 `public class AiAction` → `public partial class AiAction`
//   是 1→1、**本文件零行数变化**，新字段放这里。
//
// 判据（第一权威 = 反编译方法体，2026-10-31 现读）：
//   · 原版的答案是「**选中哪一个变体**」= `choicesDefinition.filterSpecificCards` 的**下标**
//     （池内 4 张 oath 卡都正好 2 项 ⇒ 实际取值 0 / 1；[0] 不带 `oath` trait、[1] 带 —— `cards_truth.json` 逐张现读）；
//   · 存在 `BattleManager.cardChosenInSelection(+0x2E8)`（`out_il2csharp/Assembly-CSharp/BattleManager.cs:126`）；
//   · **跟着动作一起走**：`BattleAction.actionValueTens(+0x54)` →
//     `ChoiceOfCardEnemy(actingCard, targetCard, battleAction.actionValueTens)` → `fullPool[idx]`
//     （`out_il2csharp/…/BattleManager.cs:28810-28816` / `:8951-8965`；编码侧
//      `decomp_full/BattleManager__ParsePlayerAction.c:608` 把它打进 `PlayerAction.pos(+0x20)` 的十位）。
//   ⇒ 我们同形：答案随 `AiAction` → `MsgAction`（`NetProtocol.ToWire` / `RecMsg`）走，⛔ 不另开消息、⛔ 不塞 `picks`。
//
// 🔴 这两个字段为什么必须挂在 `AiAction` 上（不是让 `ToWire` 去读 ctx）：
//   生产路径 `NetBattle.OnLocalAction` 与 `BattleDriver.RecAct` 调 `NetProtocol.ToWire(act, **ctx: null**, …)`
//   ⇒ `ToWire` 里那个 `ctx` 形参拿不到东西。
//
// ⚠️ 与 `AiAction.AltKeyword == "oath"`（**单位主动技**那条路，`RuleCore.UseOathAbility`）**不是一回事**：
//   那是「点技能按钮时付」，不经过「平 / 誓」面板，也没有答案要传。
namespace RuleEngine
{
    public partial class AiAction
    {
        /// <summary>这一手**有没有**「平 / 誓二选一」的答案。`false` = **没记**（老录像 / AI 那一手 / 玩家没答）
        /// —— 必须与「记了、选了平版」**分得开**（`JsonUtility` 把缺键一律填 0；同族先例 =
        /// `MsgDeck.draftPropsVer` / `ReplayVars.varsRecorded`）。</summary>
        public bool OathChoiceRecorded;

        /// <summary><see cref="OathChoiceRecorded"/> 为真时有效：**0 = 平版 · 1 = 誓约版**。
        /// 值 = 原版 `filterSpecificCards` 的下标 —— 与面板摆位同序
        /// （`BattleDriver.ShowAsk` 的 `ChooseOath_Plain_` / `ChooseOath_Oath_`）。</summary>
        public int OathChoice;
    }
}
