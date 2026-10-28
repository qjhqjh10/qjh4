// RuleCorePartial_B93G1.cs — 第九十三会话 · 文件所有者代理 `B93_G1` 的 `RuleCore` partial 扩展
//
// 🔴 **为什么单开一个文件**：`RuleEngine/Core/RuleCore.cs` 现读 **16,891 行**、被账按 `文件:行号`
//   引用 **365 处**（其中 `>= 15000` 的还有 **44 处**）⇒ 往它中间插成员会让下游落点整体下移、
//   `check_ledger_points` 大批报漂移。做法与 `RuleCorePartial_B93F7.cs` / `RuleCorePartial_B88E14.cs`
//   一致：**源文件只做「零净行数」的就地编辑**，新成员一律落在本文件（它没有任何账落点）。
//
// 本批（B93-G1）落在本文件的东西：
//   · `ShuffleByRandomOrderCore(list, ctx, stratagemList)` ← 账 **1308** `SupportMethods.GetRandomOrder`
//     的 **`stratagemList` 钉位支**（原实现只落了「不钉位」那一条路，账上记作「⚠️ 不实现」）。
//     `RuleCore.ShuffleByRandomOrder` 现在**一行转调**它（⛔ 别再写第二份循环）。
//   · `TryFireRegimentTrigger` 一族**没有**：账 556 的「能力入队那一跳」复查结论是
//     **生产者已经存在**（`EffectResolver.DoTriggerAbility` ← 卡面 `… and trigger their Regiment abilities`
//     解析成 `triggerability` op；池内真卡 = `AM48 Regimental Doctrine`，`chosenAbility [20,411]`），
//     见 `报告/B93_G1.md` §2 —— 那里不改代码，只订正口径。
//
// ⚠️ 判据出处写在方法头上（铁律 2/3：没有出处就写「查不到」）。

using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        /// <summary>原版 `SupportMethods.GetRandomOrder(int numElements, MatchData, List&lt;int&gt; stratagemList)`
        /// 的**完整**实现（`decomp_full/SupportMethods__GetRandomOrder.c:1-98`，本笔逐行现读）——
        /// 原地重排 `list`：`stratagemList` 里那几张**钉在前缀位**，其余位置从剩下的里按原版抽签。
        ///
        /// <para>**逐句对照**（`.c` 行号 → 本方法）：</para>
        /// <list type="number">
        /// <item>`:37-47` 结果表全 `-1`、`used` 全 `false` ⇒ 我们的 `used[n]`（结果表用 `tmp`，
        ///   泛型版不需要 `-1` 哨兵：每个位置都会被写上）。</item>
        /// <item>`:48-60` **钉位**：`stratagemList[i]`（= **元素在 `list` 里的下标**）写到前缀位 `i`、
        ///   并置 `used[该下标] = true`。`:53-56` 对 `used[该下标]` 的越界检查 ⇒ 原版**抛
        ///   `IndexOutOfRangeException`**；我们显式抛 `ArgumentOutOfRangeException`（同类语义、
        ///   但带得出是哪个下标 —— 本项目「不许静默失败」）。</item>
        /// <item>`:61-95` 其余位置（`i` 从 0 数到 `n-1`，`iVar1` 同步 `n → 1` 递减）：
        ///   `uVar9 < iVar11`（= 当前位置落在**钉位前缀**里）⇒ **跳过抽签**、只 `uVar9++ / iVar1--`
        ///   ⇒ **不消耗随机数**；否则 `r = GetRandomInt(0, n - i)`，在 `used` 里数第 `r` 个**未用**
        ///   下标 → 写结果位、置 `used`。
        ///   ⚠️ 注意抽签的**上界是 `n - i`**（含钉位占掉的位），所以钉位会改变后面每一次抽签的
        ///   取模面 —— 这**不是**「先摘掉钉位再洗剩下的」（那样上界会是 `m - i`，牌序与原版不同）。
        ///   ⛔ 别「化简」成后者。</item>
        /// <item>`:92-93` 每一轮都 `uVar9++ / iVar1--`（含跳过抽签的那一轮）—— 所以 `iVar1` 始终 =
        ///   `n - i`，与上面的上界一致。</item>
        /// <item>`GetRandomInt` 的早退（`MatchData__GetRandomInt.c`：`maxValueExcl &lt;= min+1 ⇒ 返回 min、
        ///   游标不动`）⇒ **末位不消耗随机数**，与改前逐字一致。</item>
        /// </list>
        ///
        /// <para>**可达性（如实记）**：池内 `_stratagem` **0 张**（现扫 `cards_engine.json`）⇒
        /// 钉位支今天**零可观测**；铁律 11 照样建（原版有这条代码路径）。真消费者 = 轮抽 / 战术起手
        /// 那条链（`checkStratagem`）；届时**只加参数**，⛔ 别再写第二份算法。</para>
        ///
        /// <para>⚠️ `stratagemList` 的语义 = **下标**（`list` 里的位置），不是卡 id ——
        /// 原版 `param_3` 是 `List&lt;int&gt;` 且直接拿去索引 `used[]`（`.c:53-58`）。</para>
        /// </summary>
        public static void ShuffleByRandomOrderCore<T>(IList<T> list, BattleContext ctx,
                                                       IList<int> stratagemList)
        {
            if (list == null || ctx == null) return;
            int n = list.Count;
            if (n <= 1) return;                       // 0/1 张：同原版（不消耗随机数）
            int pins = stratagemList == null ? 0 : stratagemList.Count;
            if (pins > n)
                throw new System.ArgumentOutOfRangeException(
                    nameof(stratagemList), pins, "原版 GetRandomOrder：钉位数不能超过表长（.c:48-60）");
            var used = new bool[n];
            var tmp = new T[n];
            for (int i = 0; i < pins; i++)
            {
                int idx = stratagemList[i];
                if (idx < 0 || idx >= n)
                    throw new System.ArgumentOutOfRangeException(
                        nameof(stratagemList), idx, "原版 GetRandomOrder .c:53-56：钉位下标越界即抛");
                used[idx] = true;
                tmp[i] = list[idx];
            }
            for (int i = 0; i < n; i++)
            {
                if (i < pins) continue;               // 原版 `.c:64`：`uVar9 < 钉位数` ⇒ 跳过抽签（随机数不消耗）
                int r = ctx.GetRandomInt(0, n - i);   // 上界含钉位占掉的位（⛔ 别改成 n-pins-i）
                int j = 0, seen = 0;
                while (true)
                {
                    while (used[j]) j++;
                    if (seen == r) break;
                    seen++; j++;
                }
                used[j] = true;
                tmp[i] = list[j];
            }
            for (int i = 0; i < n; i++) list[i] = tmp[i];
        }
    }
}
