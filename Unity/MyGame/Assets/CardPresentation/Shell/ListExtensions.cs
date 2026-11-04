// ListExtensions.cs — 原版静态工具类 `ListExtensions` 里本仓需要的三个成员
//                   （`GetMax<T,TU>` / `GetMin<T,TU>` / `SpliceWhere<T>`，账 `未分类_其他:398` / `:399` / `:396`）
//
// ============================ 判据（本代理 2026-10-29 现读）============================
//   · 容器体：`d:/2/tools/decomp_full/ListExtensions__GetMax_object__int_.c` 与
//             `d:/2/tools/decomp_full/ListExtensions__GetMin_object__long_.c`
//             —— 两份**首行都是 `FUN_180d10f70`**（同一个 gh id ⇒ 链接器把同形机器码 ICF 折叠了；
//             B94_B6 已记过这条，⛔ 别据此认为「两个方法是同一个」）。
//             体 = `Enumerable.Append(...)` → `Enumerable.Aggregate(...)`（**无种子那一档**）：
//             累加器就是**第一个元素**，逐元素与「比较委托」二选一。
//   · 方向（**只有 `.c` 能读出来**，ILSpy 把那两个 lambda 降级成「两支都 `return this`」）：
//       `ListExtensions.__c__DisplayClass6_0_object__int____GetMax_b__0.c`：
//           `iVar2 = f(param_2).CompareTo(f(param_3));  if (0 < iVar2) param_3 = param_2;  return param_3;`
//           ⇒ **`f(i1) > f(i2)` 取 i1，否则取 i2**（= 同分取**后者**）
//       `ListExtensions.__c__DisplayClass7_0_object__long____GetMin_b__0.c`：
//           `iVar2 = f(param_2).CompareTo(f(param_3));  if (iVar2 < 0) param_3 = param_2;  return param_3;`
//           ⇒ **`f(i1) < f(i2)` 取 i1，否则取 i2**（= 同分取**后者**）
//   · 签名（`cpp2il/out_cpp2il_dev_ilspy/ListExtensions.cs:2527` / `:2603`，**没有 `this`** ⇒ 不是扩展方法）：
//           `public static T GetMax<T, TU>(IEnumerable<T> data, Func<T, TU> f) where TU : IComparable`
//           `public static T GetMin<T, TU>(IEnumerable<T> data, Func<T, TU> f) where TU : IComparable`
//
// 🔴 **⛔ 不是 `data.Select(f).Max()` / `.Min()`** —— 那两个返**回投影值**、而且同分取**第一个**；
//    原版返回的是**元素**、同分取**后者**。账面 B94_B6 记过一次方向搞反（`f(b).CompareTo(f(a))>0?b:a`，
//    那形式同分取前者），B96_MENU 已就地订正，本文件按订正后的 `.c` 语义写。
//
// ============================ 本仓消费点：`GetMax`/`GetMin` 0 个；`SpliceWhere`（2026-11-05 B143-W7 补建）原版 4 个调用点里**只有 1 个**在本仓有对应件 —— `Shell/OfferContainer.cs:1100-1109`（`DrawRewards` 第 ② 拍，现为内联手写版：没有 `count` 上限、用下标 `RemoveAt`），另 3 处 `CarouselOfferPopupDrawer`/`GeneralOfferPopupDrawer`/`DraftEventBase` 本仓**没有同名件** ============================
//   · 原版调用点三处，本地都**没有等价件**：
//       `Everguild.LiveOps.ForgeHandler.cs:321`（`GetMax` 算最高锻造等级）—— 我们 `Shell/ForgeTab.cs`
//         的 `ForgeData.MaxLevel` 是**常量**（不是「逐件取最大」）⇒ 无同形。
//       `MissionChallengeProgress.cs:576`（`GetMax`）—— 本仓 `ChallengeData.Target` 是**单值**。
//       `ArmySelectorRanked.cs:604`（`GetMin` 取最近结束的加分档）—— 本仓 `RankedData.Boosts` 还没有
//         「挑一档」的选择逻辑（`Shell/RankedTab.cs` 归别的写手）。
//   · ⇒ 这两个方法**当前 0 调用方**（`grep -rn "ListExtensions.GetMax\|ListExtensions.GetMin"` = 0）。
//     ⚠️ 这与 2026-10-28 `RankedDeckSelector` 那次「白建一个没人用的类」是**同一类风险**，
//     差别在于：那两个类的**行为**在原版里已有**内联等价实现**，而这里原版的方法体本身**没有本地等价物**
//     （不是「别人替我实现了」，是「本地数据模型是单值 / 常量」）⇒ 建出来是**如实复刻一个通用工具**，
//     由 `Editor/B97_MISC2_Assert.cs` 的 A/B 两段钉住语义，等第一处真实调用点出现时直接可用。
using System;
using System.Collections.Generic;

namespace CardPresentation
{
    /// <summary>原版静态工具类 `ListExtensions`（只搬了本账需要的两格：`GetMax` / `GetMin`）。
    /// <para>⛔ 别把它写成扩展方法（原版签名里没有 `this`）；⛔ 别「顺手」加 `Select().Max()` 那种更短的写法
    /// —— 语义不同（见文件头）。</para></summary>
    public static class ListExtensions
    {
        /// <summary>= 原版 `public static T GetMax&lt;T, TU&gt;(IEnumerable&lt;T&gt; data, Func&lt;T, TU&gt; f)`。
        /// <para>语义 = `data.Aggregate((i1, i2) =&gt; f(i1).CompareTo(f(i2)) &gt; 0 ? i1 : i2)`：
        /// 从**第一个元素**起累加，逐元素比较**投影值**，**同分保留后者**。</para>
        /// <para>⚠️ 空序列 ⇒ `InvalidOperationException`（原版 `Aggregate` 无种子那一档同此；⛔ 别悄悄返回 `default`）。</para></summary>
        public static T GetMax<T, TU>(IEnumerable<T> data, Func<T, TU> f) where TU : IComparable
        {
            if (data == null) throw new ArgumentNullException("data");   // 原版这一支是 NRE（`data.Aggregate` 解引用）
            if (f == null) throw new ArgumentNullException("f");
            using (var e = data.GetEnumerator())
            {
                if (!e.MoveNext()) throw new InvalidOperationException("Sequence contains no elements");
                T best = e.Current;
                while (e.MoveNext())
                {
                    T candidate = e.Current;
                    // ★ 原版 lambda：`if (0 < f(i1).CompareTo(f(i2))) 取 i1; else 取 i2;`（同分 ⇒ i2）
                    if (f(best).CompareTo(f(candidate)) > 0) { /* 保留 best */ }
                    else best = candidate;
                }
                return best;
            }
        }

        /// <summary>= 原版 `public static T GetMin&lt;T, TU&gt;(IEnumerable&lt;T&gt; data, Func&lt;T, TU&gt; f)`。
        /// <para>语义 = `data.Aggregate((i1, i2) =&gt; f(i1).CompareTo(f(i2)) &lt; 0 ? i1 : i2)`：
        /// **同分保留后者**（与 `GetMax` 同向，判据见文件头那两份 lambda `.c`）。</para>
        /// <para>⚠️ 空序列 ⇒ `InvalidOperationException`（同 `GetMax`）。</para></summary>
        public static T GetMin<T, TU>(IEnumerable<T> data, Func<T, TU> f) where TU : IComparable
        {
            if (data == null) throw new ArgumentNullException("data");
            if (f == null) throw new ArgumentNullException("f");
            using (var e = data.GetEnumerator())
            {
                if (!e.MoveNext()) throw new InvalidOperationException("Sequence contains no elements");
                T best = e.Current;
                while (e.MoveNext())
                {
                    T candidate = e.Current;
                    // ★ 原版 lambda：`if (f(i1).CompareTo(f(i2)) < 0) 取 i1; else 取 i2;`（同分 ⇒ i2）
                    if (f(best).CompareTo(f(candidate)) < 0) { /* 保留 best */ }
                    else best = candidate;
                }
                return best;
            }
        }

        /// <summary>= 原版 `public static List&lt;T&gt; SpliceWhere&lt;T&gt;(IList&lt;T&gt; list, Func&lt;T, bool&gt; predicate, int count = 0)`。
        /// ✅ 已复刻（2026-11-05 · B143-W7 · `cpp2il/out_cpp2il_dev_ilspy/ListExtensions.cs:2362-2467`）
        /// <para>体（ILSpy 可读的那三句 + 循环尾部）：</para>
        /// <code>
        /// IEnumerable&lt;T&gt; e = Enumerable.Where(list, predicate);      // :2377
        /// if (count &gt; 0) e = Enumerable.Take(e, count);                 // :2378-2385（`count &lt;= 0` 才整表）
        /// List&lt;T&gt; picked = Enumerable.ToList(e);                      // :2386
        /// foreach (T x in picked) list.Remove(x);                       // :2387-2461（接口派发 `IList&lt;T&gt;.Remove`，逐条摘）
        /// return picked;                                                // :2461
        /// </code>
        /// <para>🔴 **会被「摘走」**（这也是它叫 `Splice` 的原因）：返回的是**被挑出来的那批**，
        /// 而 `list` 里对应的元素被 `Remove` 掉（`Remove` 摘**第一处相等**，与 `List&lt;T&gt;.Remove` 同义）。</para>
        /// <para>🔴 **`count` 的语义**：`count &gt; 0` ⇒ 只取**前 `count` 条命中**（`Take` 是**惰性短路**，
        /// 命中够数就停 ⇒ 不会多枚举后面的元素）；`count &lt;= 0` ⇒ 全表。⛔ 别写成 `list.Where(...).Take(count)` 无脑取。</para>
        /// <para>⚠️ 空参：原版 `Enumerable.Where(null, …)` 抛 `ArgumentNullException` —— 这里照抛（⛔ 不悄悄返回空表）。</para>
        /// <para>⚠️ 本仓**消费点 = 1 处待接**：`Shell/OfferContainer.cs:1100-1109`（`DrawRewards` 第 ② 拍）现在是
        /// **内联手写版**（`for` + `RemoveAt`），**没有 `count` 上限**、且用下标 `RemoveAt(i--)` 而不是值相等 `Remove`
        /// ⇒ 等那一处改成调本方法（该文件不在本批白名单，见 `报告/B143_W7_R20九缺口.md`）。</para></summary>
        public static List<T> SpliceWhere<T>(IList<T> list, Func<T, bool> predicate, int count = 0)
        {
            if (list == null) throw new ArgumentNullException("list");         // 原版 `Enumerable.Where(null,…)` 同此
            if (predicate == null) throw new ArgumentNullException("predicate");
            var picked = new List<T>();
            // = 原版 `Where(list, predicate)` → `count > 0 ? Take(count) : 整表` → `ToList()`：
            // 先把**命中且没超过上限**的元素全部物化出来（`Take` 的短路语义 = 够数就停）。
            for (int i = 0; i < list.Count; i++)
            {
                if (!predicate(list[i])) continue;
                picked.Add(list[i]);
                if (count > 0 && picked.Count >= count) break;
            }
            // `foreach (T x in picked) list.Remove(x);` —— 逐条摘（值相等，摘第一处）
            for (int i = 0; i < picked.Count; i++) list.Remove(picked[i]);
            return picked;
        }
    }
}
