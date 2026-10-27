// BattleContextRandom.cs — `BattleContext` 的**随机源**那一半（`partial`，与 `BattleContext.cs` 同一个类）
//
// 🔴 2026-10-27（执行代理 `E1RNG`）新开这个文件的原因：`BattleContext.cs` 被账按 `文件:行号` 引用
//   （23 处落点），往它中间插成员会让下游落点整体下移 ⇒ 新成员放进**同一个类的另一个 partial 文件**，
//   源文件一行不动（只把类声明加上 `partial`）。
//
// ==================================================================
//  原版随机源 = 开局预烧 100 个整数 + 按下标取（不是「每次掷骰」）
// ==================================================================
//
//  判据（逐句，`d:/2/tools/decomp_full/`）：
//   · `MatchData__GenerateRandomNumbers.c`：开局 `new int[100]`（`FUN_1803f37c0(…,100)`），
//     逐格 `= UnityEngine.Random.Range(0, 1000)`，然后把游标 `+0x28` 清 0；
//   · `MatchData__GetRandomInt.c`：
//       数组为空 ⇒ `CustomDebug.LogError` + 返回 0；
//       `maxValueExcl <= minValueIncl + 1` ⇒ 返回 min（**不消耗随机数**）；
//       否则 `idx = 游标 % Count` → `v = 数组[idx]` → 游标++ → 返回 `min + v % (max - min)`。
//   · `BattleManager__GetRandomInt.c` 只是转发到 `MatchData`（同一个表）⇒ 全引擎**一条表**。
//
//  ⇒ 同一局内**完全确定**、每次调用只前进一格。
//  ⚠️ 账 `战斗逻辑驱动.tsv` 的 `MatchData.GetRandomInt` / `BattleManager.GetRandomInt` /
//     `MatchData.GenerateRandomNumbers` / `SupportMethods.GetRandomOrder` 几行说的就是这条链。
//
//  ⚠️ 调用方**一律**用 `ctx.GetRandomInt(minIncl, maxExcl)`（上界**开区间**，与原版同名同义）；
//     ⛔ 别在结算路径上再写 `ctx.Rng.Next` —— 那会绕过这张表（`Rng` 现在只当烧表用的种子流）。
//     ⚠️ `ShowRng`（表现层抽展示候选）与 `AiRng`（AI 掷骰）是**另外两条**，本批不动（见各自 doc）。

namespace RuleEngine
{
    public partial class BattleContext
    {
        /// <summary>开局预烧的随机表（100 个整数，原版 `MatchData.randomNumbers(+0x20)`）。
        /// ⛔ 别手改内容 —— 只在 <see cref="InitRandomNumbers"/> 里填一次。</summary>
        public readonly int[] RandomNumbers = new int[100];

        /// <summary>随机表游标（原版 `MatchData.randomNumbersIndex(+0x28)`）—— <see cref="GetRandomInt"/> 每取一格 ++。</summary>
        public int RandomNumbersIndex;

        /// <summary>烧表：100 格各取一个 `[0,1000)` 的整数，然后游标清 0（原版 `GenerateRandomNumbers`）。
        ///
        /// <para>🔶 自己设计（非原版）：**烧表的数源**用 `System.Random(seed)`（`Rng`），而原版用的是
        /// **全局 `UnityEngine.Random.Range(0,1000)`**（`MatchData__GenerateRandomNumbers.c` 的
        /// `UnityEngine_Random__Range(0,1000,0)`，**不可由种子复现**）。工程红线要求「同一颗种子跑两次
        /// 必须得到同一局」⇒ 只把数源换成种子流；**表/游标/取法三者照原版**（「同局内完全确定」不变）。
        /// 调度台 2026-10-27 裁定。</para></summary>
        void InitRandomNumbers()
        {
            for (int i = 0; i < RandomNumbers.Length; i++) RandomNumbers[i] = Rng.Next(0, 1000);
            RandomNumbersIndex = 0;
        }

        /// <summary>原版 `MatchData.GetRandomInt(int minValueIncl, int maxValueExcl)`
        /// （`d:/2/tools/decomp_full/MatchData__GetRandomInt.c:8-40` 逐句）。
        ///
        /// <para>上界**开区间**（同 `System.Random.Next(min,max)`）；**不掷骰**，只按游标取预烧表的一格。</para>
        ///
        /// <para>三条边界照原版：① 表为空 ⇒ 返回 0（原版还打一条 `CustomDebug.LogError`；我们的表由
        /// <see cref="RandomNumbers"/> 字段初始化器恒非空 ⇒ 这一档**结构上到不了**，故只留返回）；
        /// ② `maxValueExcl &lt;= minValueIncl + 1` ⇒ 返回 `minValueIncl`、**游标不动**；
        /// ③ 游标**只前进**、按 `Count` 取模回绕（原版 `+0x28 % Count`）。</para>
        /// </summary>
        public int GetRandomInt(int minValueIncl, int maxValueExcl)
        {
            if (RandomNumbers == null) return 0;                        // 原版：LogError + 0（结构上到不了，见 doc）
            if (maxValueExcl <= minValueIncl + 1) return minValueIncl;  // 原版：不消耗随机数
            int idx = RandomNumbersIndex % RandomNumbers.Length;
            int v = RandomNumbers[idx];
            RandomNumbersIndex++;
            return minValueIncl + v % (maxValueExcl - minValueIncl);
        }
    }
}
