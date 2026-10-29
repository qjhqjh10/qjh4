// DynamicList.cs —— 原版第三方工具类 `DynamicList` / `DynamicList<T>`（账 `菜单_外壳:142/143/149/150/151/154/155`）
//
// ============================ 这是什么 ============================
// 原版工程里一个**第三方工具类**（Unity 资产 `DynamicList`），作用 = **`[SerializeReference]` 的多态列表包装**：
// 一个 `List<T>`，外加「按子类型取 / 过滤 / 取类型」几个给**自定义 Inspector** 用的口。
//
// 判据（**逐条现读**，⛔ 别再另找）：
//   · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/DynamicList.cs`（647 行，**有真方法体**）——
//     `get_Item:171` · `Count:183` · `GetList:198` · `ListType:203` · `GetIndexType:211` ·
//     `Get<T1>(T):228` · `Get<T1>(int):269` · `GetAll<T1>:321` · `Add:555` · `Inser:577` ·
//     `IndexOf:582` · `Clear:593` · `Remove:605` · `RemoveAt:610` · `.ctor:615` ·
//     基类 `DynamicList`（抽象，两个无体方法）= 同文件 `:642-647`
//   · `d:/2/tools/il2cpp_out/dump.cs:115653-115848`（成员 / 泛型约束 / 字段偏移齐全）
//   · 分诊 → `资料/完全复刻/判据/B96_菜单未分类44_分诊.md` §二·一（成员清单就是照它逐条建的）
//
// 🔴 **拼写照原版**：`Inser(T element, int index)` 的**方法名在二进制里就是拼错的 `Inser`**（不是 `Insert`），
//    而它的体是 `list.Insert(index, element)`（**注意形参次序是反的**：原版叫 `Inser(element, index)`）。
//    ⇒ 照抄拼写 + 照抄实参次序。⛔ 别「顺手改对」：这个类的唯一消费者是**按名字反射**的编辑器抽屉。
//
// ============================ 三条如实标（读不出来 / 我们做了选择）============================
// ① `Get<T1>` / `GetAll<T1>` 的体里有 `Method not found @1803F3750`（IL2CPP 的类型转换助手）⇒
//    「`as` 转换、失败返 null」是**从 `b__10_0` / `b__10_1` 两个 lambda 的形状反推**的
//    （`b__10_0` 返回 `obj != null` ⇒ `is T1`；`b__10_1` 返回那个对象 ⇒ `as T1`），**不是逐句读出**。
// ② `Count` / `IndexOf` / `get_Item` 的 `list == null` 支：ILSpy 渲染成 `return (int)new NullReferenceException()`
//    （原版是 NRE）⇒ 实现保留「null ⇒ 抛」的语义（直接解引用即可），⛔ 别抄那句强转。
// ③ `[SerializeReference]` **照抄**（原版字段就是 `[SerializeReference] [SerializeField] private List<T> list;`）。
//    本工程里这条链**从不走 `JsonUtility`**（`LiveOpsEventSave` 是唯一被 Json 序列化的 LiveOps 类型，
//    见 `Shell/LiveOpsEvent.cs:993`；`EventFilter.Triggers` / `Conditions` 只由代码/自检注入）
//    ⇒ 该特性在这里**是惰性的**，不会打断任何 JSON 装载。
//    ⚠️ 将来若有人拿 `JsonUtility` 去序列化 `LiveOpsEventFilter`，必须回来读这一条（`SerializeReference` 不被它支持）。
//
// ============================ 谁是消费者（⛔ 别让它变成「建了却 0 调用方」）============================
// `LiveOpsEventFilter.Triggers`（原版 `+0x30 DynamicList<EventTrigger>`）与
// `LiveOpsEventFilter.Conditions`（原版 `+0x38 DynamicList<EventCondition>`）**已改成本类型**
// （`Shell/LiveOpsEvent.cs:135/139`）⇒ 真消费者是 `Shell/LiveOpsFilter.cs` 的
// `RegisterTriggers` / `CleanupTriggers` / `Validate`（都在跑，且有自检 P1/P2 断言）。
// 2026-10-28 `RankedDeckSelector` 白建过一次的先例（行为早在别处内联）—— 这里**不存在**那个问题：
// 上面那三处是**原版那两格的真读者**，不是重复件。
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `DynamicList`（**非泛型抽象基类**，`out_cpp2il_dev_ilspy/DynamicList.cs:642-647`）：
    /// 只有两个**无体**的抽象方法（`dump.cs:115833-115845` 明写 `public abstract Type ListType();`，无 RVA）
    /// ⇒ 这两个在账里是 `菜单_外壳.tsv:148/153` 的 **D**（原版本身没有体），⛔ 别因为本文件建了基类就把它们改档。</summary>
    public abstract class DynamicList
    {
        /// <summary>原版 `DynamicList.ListType()`：本条元素声明的类型（`typeof(T)`）。</summary>
        public abstract Type ListType();
        /// <summary>原版 `DynamicList.GetIndexType(int index)`：第 `index` 项的**运行时**类型。</summary>
        public abstract Type GetIndexType(int index);
    }

    /// <summary>原版 `DynamicList&lt;T&gt; : DynamicList`（`[Serializable]`，`where T : class`）。
    /// <para>成员清单与体逐条来自 `out_cpp2il_dev_ilspy/DynamicList.cs`（行号写在各成员的 doc 里）。</para>
    /// <para>⚠️ 原版还有一个 `private sealed class &lt;&gt;c__10&lt;T1&gt;`（`GetAll` 那两个 lambda 的缓存）。
    /// C# 里 `list.Where(e =&gt; e is T1).Select(e =&gt; e as T1)` 的行为**完全相同**（同一表达式树形状）⇒
    /// ⛔ 不必照抄那个缓存类。</para></summary>
    [Serializable]
    public class DynamicList<T> : DynamicList where T : class
    {
        /// <summary>原版 `DynamicList&lt;T&gt;.list`（`dump.cs:115710`）：**两个特性都要**
        /// （`[SerializeReference]` = 多态元素按引用来序列化；`[SerializeField]` = 私有字段也进序列化）。
        /// 见文件头「如实标 ③」。</summary>
        [SerializeReference]
        [SerializeField]
        private List<T> list = new List<T>();

        /// <summary>原版 `DynamicList&lt;T&gt;.get_Item(int)`（`.cs:171`）：`return list[index];`
        /// （`list == null` ⇒ 原版 NRE，我们同样直接解引用；越界由 `List` 自己抛）。</summary>
        public T this[int index]
        {
            get { return list[index]; }
        }

        /// <summary>原版 `DynamicList&lt;T&gt;.Count`（`.cs:183`）：`return list._size;`
        /// （`list == null` ⇒ 原版 NRE）。</summary>
        public int Count
        {
            get { return list.Count; }
        }

        /// <summary>原版 `DynamicList&lt;T&gt;.GetList()`（`.cs:198`）：`return list;`
        /// ⇒ **返回的就是那一张表本身**（不是副本）。</summary>
        public IReadOnlyList<T> GetList()
        {
            return list;
        }

        /// <summary>原版 `DynamicList&lt;T&gt;.ListType()`（`.cs:203`，override）：`return typeof(T);`。</summary>
        public override Type ListType()
        {
            return typeof(T);
        }

        /// <summary>原版 `DynamicList&lt;T&gt;.GetIndexType(int index)`（`.cs:211`，override）：
        /// `var o = list[index]; return o == null ? null : o.GetType();`
        /// （ILSpy 里那两跳是 `list[index]` + `isinst`；元素 null ⇒ null）。</summary>
        public override Type GetIndexType(int index)
        {
            var e = list[index];
            return e == null ? null : e.GetType();
        }

        /// <summary>原版 `DynamicList&lt;T&gt;.Get&lt;T1&gt;(T element)`（`.cs:228`）：`return element as T1;`
        /// （不是 `T1` ⇒ null）。见文件头「如实标 ①」—— 这一句是从两个 lambda 的形状反推的。</summary>
        public T1 Get<T1>(T element) where T1 : class, T
        {
            return element as T1;
        }

        /// <summary>原版 `DynamicList&lt;T&gt;.Get&lt;T1&gt;(int index)`（`.cs:269`）：`return list[index] as T1;`</summary>
        public T1 Get<T1>(int index) where T1 : class, T
        {
            return list[index] as T1;
        }

        /// <summary>原版 `DynamicList&lt;T&gt;.GetAll&lt;T1&gt;()`（`.cs:321`）：
        /// `return list.Where(e =&gt; e is T1).Select(e =&gt; e as T1).ToList();`
        /// <para>判据 = 同方法里那两个 lambda 体（`b__10_0` 的体 = `obj != null`（`is T1`）、
        /// `b__10_1` 的体 = 直接返回该对象（`as T1`））。</para></summary>
        public List<T1> GetAll<T1>() where T1 : class, T
        {
            return list.Where(e => e is T1).Select(e => e as T1).ToList();
        }

        /// <summary>原版 `DynamicList&lt;T&gt;.Add(T element)`（`.cs:555`）：`list.Add(element);`
        /// （ILSpy 把 `List.Add` 内联展开了 —— 那一段 `_items/_size/_version` 就是 `List&lt;T&gt;.Add` 的本体）。</summary>
        public void Add(T element)
        {
            list.Add(element);
        }

        /// <summary>原版 `DynamicList&lt;T&gt;.Inser(T element, int index)`（`.cs:577`）：`list.Insert(index, element);`
        /// 🔴 **方法名与形参次序都照原版**（`Inser` 是原版的拼写，形参是「元素在前、下标在后」）。
        /// ⚠️ 它照抄 `List.Insert` 的行为：`index == Count` ⇒ 追加；越界 ⇒ `ArgumentOutOfRangeException`。</summary>
        public void Inser(T element, int index)
        {
            list.Insert(index, element);
        }

        /// <summary>原版 `DynamicList&lt;T&gt;.IndexOf(T element)`（`.cs:582`）：`return list.IndexOf(element);`</summary>
        public int IndexOf(T element)
        {
            return list.IndexOf(element);
        }

        /// <summary>原版 `DynamicList&lt;T&gt;.Clear()`（`.cs:593`）：`list.Clear();`
        /// （ILSpy 展开成 `_version++ / _size = 0 / Array.Clear` = `List&lt;T&gt;.Clear` 本体；
        /// ⚠️ 原版那段 `if (list._size &gt; 0)` 判的是**清空之后**的 `_size`（恒 0）⇒ 那段 `Array.Clear`
        /// 实际上是**死支**。`List&lt;T&gt;.Clear` 自己会在需要时清引用，所以这里转调即可、⛔ 不去复刻那个死支）。</summary>
        public void Clear()
        {
            list.Clear();
        }

        /// <summary>原版 `DynamicList&lt;T&gt;.Remove(T element)`（`.cs:605`）：`list.Remove(element);`
        /// （ILSpy 里 `bool flag = List.Remove(...)` 的返回值**被丢掉** ⇒ 原版这个方法不返回 bool）。</summary>
        public void Remove(T element)
        {
            list.Remove(element);
        }

        /// <summary>原版 `DynamicList&lt;T&gt;.RemoveAt(int index)`（`.cs:610`）：`list.RemoveAt(index);`</summary>
        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
        }

        // .ctor（`.cs:615`）：`base._ctor(); list = new List<T>();`
        // ⇒ 本类用字段初始化器给出同一初值（原版 ctor 里没有别的动作）。
    }
}
