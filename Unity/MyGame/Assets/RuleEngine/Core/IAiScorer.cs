// IAiScorer.cs — `Core` 侧的「AI 评分」钩子（引擎线 `W64 · B-01`，2026-10-23 接线）
//
// 🔴 **为什么是钩子，而不是把评分逻辑搬进 `Core`**（2026-10-23 调度台裁定，见
//   `资料/任务线/引擎线/任务总表.md` 的 `W64·引擎侧` 行）：
//   ① **`Core/` 不许依赖 `Data/`** —— `工具/ruleprobe/ruleprobe.csproj` 只编 `Core/*.cs`
//      （外加显式列出的 `Data/CardDatabase.cs`）。在 Core 里写一句 `SimpleAI.…` 会让
//      **整条探针构建失败**（实测 CS0103 `SimpleAI` / `AiAction`，见
//      `普查产出_1023b/引擎线_W64与T03_执行报告.md` §3·1）。
//   ② 原版那份评分的判据在 `Data/SimpleAI.cs`（`ScoreFromAttack` / `ScoreDamaging` /
//      `CombinedAttackCanKillWarlord`）＋ `Data/AiScoring.cs`（逐卡评分表）＋ `Data/AiTargets.cs`
//      —— 搬进 Core 要连 `AiScoringData` / `AiTargets` 一起搬，是**结构大改**。
//   ⇒ **`Core` 只定义「给一刀打分」这一个动作**，由 `Data/SimpleAI` 实现、由 `Data` 侧注入。
//
// **原版判据（第一权威，逐条现读）**
//   · `d:/2/tools/decomp_full/BattleManager__ResolveEndTurn.c:44-59`
//     —— 回合结束的狂暴强制出手走 `AI.SelectBestAttackAction(ai, bm, 该单位, 0, 0)`。
//   · `AI__SelectBestAttackAction.c:24-65`
//     —— `GetAvailableActions(bm, 该单位.side, …)` → 只留 `actionType ∈ {1 近战, 2 远程}`
//        且 `action.actingCard == 该单位` 的动作 → 逐条 `AI.GetActionScore(…,0,0,0)` 打分 →
//        `AI.SelectBestAction(list, 0)`（严格 `>`，平手留先出现的）。
//   · `AI__GetActionScore.c:45-63`
//     —— `case 1/2` = `AI.ScoreFromAttack(…)`；目标 `cardType == 10`（督军）且
//        `AI.CombinedAttackCanKillWarlord` ⇒ 再 `+= DAT_1834b32f4`（= `10000.0`）。
//   ⇒ 本接口的 `ScoreAttack` **就是**「`GetActionScore` 的攻击支」那一条，语义见实现处
//     （`Data/SimpleAI.cs` 的 `SimpleAiScorer`）。
//
// ⚠️ **探针里恒未安装（`Scorer == null`）** —— `ruleprobe` 不编 `Data/*`，装不上实现
//   ⇒ 调用方必须自带**回退路径**（见 `RuleCore.ResolveBerzerkAttacks`）。这是设计的一部分，
//   ⛔ 别把「未安装」写成「评分 0 分」：那是两种不同的意思，会把「没接上」静默变成「全都一样好」。

namespace RuleEngine
{
    /// <summary>
    /// 「给一刀打分」—— 原版 `AI.GetActionScore` 的**攻击支**（`case 1/2`）。
    ///
    /// 🔴 **接口刻意只暴露这一件事**：多一个参数就要多一条跨层约定。
    ///    `Core` 手里有的东西（`ctx` / 行动方 / 出手格 / 目标格 / 用哪一档打法）已经够复刻原版那一段；
    ///    评分要用的**数据**（卡池评分表、关键词分值…）全在实现侧自己取。
    /// </summary>
    public interface IAiScorer
    {
        /// <summary>
        /// <paramref name="side"/> 方的 <paramref name="slot"/> 格单位，
        /// 用 <paramref name="ranged"/> 那一档打法打 <paramref name="foeSlot"/> 格上的敌方单位，值多少分。
        ///
        /// ⚠️ **返回 0 与「没打分」不是一回事**：0 是合法分值（原版 `GetActionScore` 里
        ///    「打不死的小兵、伤害为 0」就落在 0 附近），调用方**不许**拿 0 当「钩子没装」的哨兵。
        /// </summary>
        /// <param name="side">出手方（0/1）。原版从 `actingCard.side`（`+0x40`）取，不是隐含的「当前行动方」。</param>
        /// <param name="slot">出手单位所在格位。</param>
        /// <param name="foeSlot">目标格位（在 `1 - side` 那一方）。</param>
        /// <param name="ranged">true = 远程档，false = 近战档。</param>
        float ScoreAttack(BattleContext ctx, int side, int slot, int foeSlot, bool ranged);
    }

    /// <summary>
    /// 钩子的挂载点。**只有 `Data` 侧往这里写**（`SimpleAI.InstallAiScorer()` ——
    /// 静态构造 + `[RuntimeInitializeOnLoadMethod]` 两条路都调，幂等）。
    /// ⛔ `Core` 里别的地方**不要**写它；也**不要**在这里缓存分数（评分依赖当场局面）。
    /// </summary>
    public static class AiScorerHook
    {
        /// <summary>当前挂上的评分器；<c>null</c> = 没装（探针 / 只编 Core 的宿主）。</summary>
        public static IAiScorer Scorer;

        /// <summary>装上了没有 —— 调用方用它选「走钩子」还是「走回退」，⛔ 别拿分数 0 判。</summary>
        public static bool Installed { get { return Scorer != null; } }
    }
}
