// BattleContextTurnCounter.cs — `BattleContext` 的「本回合动作计数」那一格（`partial`，与 `BattleContext.cs` 同一个类）
//
// 🔴 2026-10-27（执行代理 B9_W68ENG3 · 引擎真缺口第一批 G06①）新开这个文件的原因
//   （与 `BattleContextRandom.cs` 同一条纪律）：`BattleContext.cs` 被账按 `文件:行号` 引用
//   （引用到 `MatchTypes.UsesDefencePool` 那一行），往它中间插字段会让下游落点整体下移
//   ⇒ 新字段放进**同一个类的另一个 partial 文件**，源文件一行不动。
//
namespace RuleEngine
{
    public partial class BattleContext
    {
        /// <summary>
        /// 原版 `BattleManager.thisTurnActionCounter`（`int @+0x3bc`，`ObscuredInt`）——
        /// **本回合累计的「玩家动作」条数**（⚠️ 不是攻击次数、也不是打出的牌数）。
        ///
        /// <para>**判据（第一权威 · 现读）**：
        /// · 写点 = `decomp_full/BattleManager__RegisterPlayerAction.c:19-34`（对 `bm+0x3bc` 只做 `op_Increment`，
        ///   **无其他副作用**）；`BattleManager__RegisterPlayerActionCancelled.c:19-34`（同一格 `op_Decrement`）。
        /// · 唯一读者 = `decomp_full/EndTurnClick.c:135-152`：`bm+0x3bc == 0` ⇒ 置 `matchData.shouldCountReduced(+0x4DD)`。
        /// · 全目录 `grep 0x3bc` = 8 个文件、**唯一读者是 `EndTurnClick`**
        ///   （判据 → `资料/普查产出_第三十会话/乙档六笔_判据查证.md` §A1258）。</para>
        ///
        /// <para>🔴 **本批只落【字段】**（纯增量、零行为改动）：写点（`RuleCore` 的「入队成功 / 取消」两处 `++`/`--`）、
        /// 回合归零点（`RuleCore.BeginTurn`）与消费端（`BattleDriver.ShouldReduceNextTurn` 改读本格）
        /// **都落在 W54/W57 的文件里**，⛔ 本批不接 —— 今天**没有任何写点** ⇒ 恒为 0，行为零变化。</para>
        ///
        /// <para>⚠️ 别拿它和表现层的 `BattleDriver._actionsThisTurn`（只数本地玩家动作）或
        /// `ctx.CardsPlayedInTurn`（只数打出的牌）当等价 —— 三者口径不同（判据见施工判据 §二 G06②）。</para>
        /// </summary>
        public int ThisTurnActionCounter;
    }
}
