// EffectResolverPartial_B111E1B.cs —— 第一百一十一会话 · 波 1 · 执行代理 `B111_E1B`（`B-85` + `B-86`）
//
// **掷骰挪进引擎**：原版「平 / 誓二选一」的抽取点只有一个 —— `BattleManager.GetChoiceOptions`
//   （`decomp_full/BattleManager__GetChoiceOptions.c:40-70`），而它被**两条支**各调一次：
//     · `ChoiceOfCardEnemy`（bot 支，`out_il2csharp/Assembly-CSharp/BattleManager.cs:8951-8965`）
//     · `ChoiceOfCardPlayer`（面板支，同文件 `:8941`）—— 那个函数的 `isBotAndNotReplay` 实参**只用于排序**，
//       **抽数那半照跑** ⇒ **玩家选平 / 誓时原版同样消耗 1 格预烧随机数**。
//   ⇒ 所以**玩家手 / AI 手 / 回放落地**三条路都必须走**同一个**掷骰点，各消耗**正好 1 格**。
//   ⚠️ AI 那一手在 `B111_E2` 里是「出牌前在 `SimpleAI` 掷一次」（消耗 1 格），而**回放不走 `SimpleAI`**
//      （AI 的动作从 `MsgAction` 经 `NetApply` 落地）⇒ 回放侧**不消耗** ⇒ 此后每次 `GetRandomInt` **错开 1 格**。
//
// 本文件落的就是「那个唯一的掷骰点」以及它的池序映射（`B111_E2` 写在 `Data/SimpleAIPartial_B111E2.cs`
//   的那一份**原样保留**为门面，只把实现搬到这里来 —— 这样 Core 不依赖 Data 的方向被保住，
//   而 `SimpleAI.OathPoolIndexIsOathVariant` 的既有调用点（含 `B111_E2_Assert.cs`）一个字都不用改）。
//
// ⚠️ 与 `EffectResolver.cs` 的分工：那边 `RuleCore.DecideOathChoice` 是**唯一调用点**
//   （三档闸 + 取答案 + 兜底）；本文件只放「怎么掷」这一件事，⛔ 别在别处再写第二份取数式。
using System;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        /// <summary>
        /// **池下标 → 「这一张是不是誓约变体」**：原版 `GetChoiceFullPool`
        /// （`decomp_full/BattleManager__GetChoiceFullPool.c:55-70`；ILSpy `BattleManager.cs:9138-9154`）
        /// 的池序 = `filterSpecificCards` 过 `uniqueId > 0` 之后**按 `uniqueId` 字符串升序**。
        ///
        /// <para>我们的 `CardDef.ChoicesSpecificCardIds` 是**原版 `filterSpecificCards` 的原序**
        /// ⇒ `[0]` = 平版 · `[1]` = 誓约版（池内 10 张 oath 卡逐张核过：`[1]` 的 `manaCost` 恒更高）。
        /// 于是「誓约变体在池里的下标」= **誓约那个 id 是否排在平版前面**。</para>
        ///
        /// <para>⚠️ 用 `CompareOrdinal`：池里全是等长 32 位十六进制串 ⇒ 与 `OrderBy(string)` 的当前文化比较逐位相同，
        /// 而 ordinal 才能保证跨机器 / 跨文化一致（红线：一局必须可复现）。</para>
        ///
        /// <para>⚠️ 拿不到两个 id ⇒ 退回「`[0]` 平 / `[1]` 誓」的约定（`OathChoiceNeeded` 要求正好 2 项，
        /// 所以这一档只在数据缺列时到）。</para>
        /// </summary>
        public static bool OathPoolIndexIsOathVariant(CardDef card, int poolIndex)
        {
            if (card == null) return false;
            var ids = card.ChoicesSpecificCardIds;
            if (ids == null || ids.Count != 2 || ids[0] == null || ids[1] == null)
                return poolIndex == 1;                       // 约定：[0] = 平版 · [1] = 誓约版
            bool oathFirst = string.CompareOrdinal(ids[1], ids[0]) < 0;
            return oathFirst ? poolIndex == 0 : poolIndex == 1;
        }

        /// <summary>
        /// **这一次「平 / 誓二选一」从池里抽一张** —— 原版 `GetChoiceOptions` 那一格
        /// （`decomp_full/BattleManager__GetChoiceOptions.c:40-70`）在**两条支**里唯一的取数动作。
        ///
        /// <para>**净消耗正好 1 格**：池内 10 张 oath 卡逐张现读 `choicesMaxTargets = 2`、池 2 项
        /// ⇒ 原版循环 2 圈：第 1 圈 `GetRandomInt(0,2)` **消耗 1 格**；第 2 圈 `GetRandomInt(0,1)`
        /// 命中 `maxValueExcl &lt;= minValueIncl + 1 ⇒ 返回 min、**游标不动**`那一档（`Core/BattleContextRandom.cs:65`）
        /// ⇒ 我们这边**一次 `GetRandomInt(0, 2)` 就等价**（⛔ 不是「每圈掷一次」）。</para>
        ///
        /// <para>⚠️ ILSpy `:8993` 把这一句渲染成两次 `GetRandomInt`（`(GetRandomInt(...) &gt;= size) ? 0 : GetRandomInt(...)`）
        /// —— 那是反编译重复求值；`.c:52-59` 只有一次调用 + 一句 `if (Count &lt;= iVar3) iVar3 = 0;`。**以 `.c` 为准**。</para>
        ///
        /// <para>⚠️ 只掷、**不判断要不要掷** —— 三道闸在调用方（`DecideOathChoice` / `SimpleAI.DecideOathForAi`）
        /// 各判一次。⛔ 别把闸塞进来：两条调用点的闸虽然同序同参，但**日志措辞**不同。</para>
        /// </summary>
        /// <returns>这一次抽到的是不是**誓约版**（`true` = 誓约版 · `false` = 平版）。</returns>
        public static bool RollOathPay(BattleContext ctx, CardDef card)
        {
            if (ctx == null || card == null) return false;
            int idx = ctx.GetRandomInt(0, 2);        // 原版第 1 圈：`GetRandomInt(0, fullPool.Count = 2)`
            return OathPoolIndexIsOathVariant(card, idx);
        }
    }
}
