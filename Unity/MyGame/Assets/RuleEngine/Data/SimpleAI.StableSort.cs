// SimpleAI.StableSort.cs — `TweakAvailableActions` 的**稳定降序**排序。
//
// 为什么单开一个分册：`Data/SimpleAI.cs` 被账按 `文件:行号` 引用 41 处 ⇒ 往里加行 = 一片漂移
// （先例 = `SimpleAI.ActiveEntry.cs`，那份账里 0 引用）。本文件同样**账里 0 引用**。
//
// 判据（⛔ 不要重新论证）→ `资料/完全复刻/判据/B97_R4_战斗队列三件.md` 件 2：
//   · 原版 `AI.TweakAvailableActions` = `Enumerable.OrderByDescending(currentActions, a => a.actionScore)`
//     + `Enumerable.ToList(...)` —— LINQ 的 `OrderBy*` 在 .NET 里**保证稳定**（等键保持源序列相对序）。
//     第一权威 `decomp_full/AI__TweakAvailableActions.c:85-87`（thunk + `ToList`）；
//     坐实（`.c` 认不出是 OrderByDescending）→ `cpp2il/out_cpp2il_dev_ilspy/AI.cs:270-276`；
//     第三源 `out_il2csharp/Assembly-CSharp/AI.cs:102` + lambda `:2964`（键 = `actionScore`，float `+0x28`）。
//   · 我们原来 `SimpleAI.cs` 用 `list.Sort(...)` = introsort，**.NET 明确不保证稳定**
//     （>16 元素分区时等键次序会被打乱）。下游 `TweakAvailableActions`（从表尾删最低分）与
//     `SelectBestAction`（严格 `>`、平手留先枚举的）都吃这个次序 ⇒ 同分并列时会选到另一条动作。
//
// ⚠️ 写法与 `RuleCore.StableSortByPlayOrder` 同一条纪律（原始下标做第二排序键）。
// ⛔ 别改回 `list.Sort(...)`（不稳定）；⛔ 别用 `OrderByDescending`（本工程该分册没有、也不加 `using System.Linq`）。
using System;
using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class SimpleAI
    {
        /// <summary>按 <see cref="AiAction.Score"/> **降序、稳定**排序（原版 `AI.TweakAvailableActions`
        /// 的 `OrderByDescending(currentActions, a => a.actionScore)`；判据 → 本文件头）。
        /// <para>实现：原始下标数组做第二排序键 ⇒ 等键（同分）保持传入时的相对次序。</para>
        /// <para>⚠️ 不要用「比较器里 `list.IndexOf`」做第二键 —— 排序过程中 `list` 正处于半交换状态，
        /// 读到的是不可信的值（判据 → `B97_R4` 件 2·3 的告警）。</para></summary>
        static void StableSortByScoreDesc(List<AiAction> list)
        {
            int n = list != null ? list.Count : 0;
            if (n < 2) return;
            var items = new AiAction[n];
            var idx = new int[n];
            for (int i = 0; i < n; i++) { items[i] = list[i]; idx[i] = i; }
            Array.Sort(idx, (a, b) =>
            {
                int c = items[b].Score.CompareTo(items[a].Score);   // 降序
                return c != 0 ? c : a.CompareTo(b);                 // 同分 ⇒ 原始下标序（稳定）
            });
            for (int i = 0; i < n; i++) list[i] = items[idx[i]];
        }
    }
}
