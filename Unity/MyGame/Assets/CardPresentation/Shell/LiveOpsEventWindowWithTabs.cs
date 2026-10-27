// LiveOpsEventWindowWithTabs.cs —— **带页签的活动窗基类**（原版 `LiveOpsEventWindowWithTabs` + 它的泛型版）
//
// ============================ 这是什么、判据在哪 ============================
// 判据（唯一来源）：
//   · `资料/完全复刻/判据/泛型与启动链.md` §四 `LiveOpsEventWindowWithTabs<T>`（C 2 条，全 P2）
//   · 账 `资料/完全复刻/账/泛型与启动链_补读.tsv` 同类名下那 2 行
// 原版真身：
//   · `d:/2/tools/decomp_generic_trial/out/LiveOpsEventWindowWithTabs_object___SetupData__1016780.c`
//   · 同目录 `…___InitializeWindow__1016700.c`（两个**方法体都可读、都只有两三句**）
//   · 字段表 = `d:/2/tools/il2cpp_out/dump.cs:52967-53020` 与 `:52979`（`T LiveOp` 是自动属性 +0x0）
//
// 🔴 **这两个方法各只有两三句，别想多了**：
//   · `InitializeWindow(ILiveOps e)` → **`LiveOp = e;`**（就这一句，`sealed override`）。
//   · `SetupData(object data, GameWindowOptions options)` →
//     `base.SetupData(data, options); if (&lt;base 的 data 字段 != null&gt;) LiveOp = (T)data;`。
//   非泛型那个父类的 `InitializeWindow` 是**共享空桩**（RVA `0x4B33B0` = `ret 0`）⇒ 它真的什么都不做。
//
// ⚠️ 与原版的差异：
//   ① 原版 `SetupData(object data, GameWindowOptions options)` 有两个参数；我们的 `GameWindow.SetupData`
//      **只有一个**（`GameWindowOptions` 那一层没搬，判据在 `Shell/GameWindow.cs` 的 `SetupData` 注释里）
//      ⇒ 这里 `protected override void SetupData(object data)`。
//   ② 原版泛型约束是隐式的（`T LiveOp`），我们补 `where T : class, ILiveOps`（原版赋值处就是
//      `isinst ILiveOps`，所以这个约束与原版行为一致，只是把它显式写出来）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `ILiveOpsWindow`（`dump.cs:53033`，**一个方法**）：活动窗要能被「喂一个活动实例」。</summary>
    public interface ILiveOpsWindow
    {
        void InitializeWindow(ILiveOps liveOpsEvent);
    }

    /// <summary>原版 `LiveOpsEventWindowWithTabs : GameWindowWithTabs, ILiveOpsWindow`（`dump.cs:52967`）。
    /// <para>注意它派生自 **`GameWindowWithTabs`**（不是 `LiveOpsEventWindow`）—— 那两族是**平行的**
    /// （一个管「带页签」，一个管「活动窗的层/命中档」）。⛔ 别改成继承 `LiveOpsEventWindow`。</para></summary>
    public class LiveOpsEventWindowWithTabs : GameWindowWithTabs, ILiveOpsWindow
    {
        /// <summary>原版 `public virtual void InitializeWindow(ILiveOps liveopsEvent)` ——
        /// 方法体是**共享空桩**（RVA `0x4B33B0`，机器码 `ret 0`）⇒ **什么都不做**，由泛型子类 `sealed override`。
        /// ⛔ 别在这里加逻辑（原版这一跳真的是空的）。</summary>
        public virtual void InitializeWindow(ILiveOps liveopsEvent) { }
    }

    /// <summary>原版 `LiveOpsEventWindowWithTabs&lt;T&gt; : LiveOpsEventWindowWithTabs`（`dump.cs:52979`）：
    /// 把「这一窗服务哪个活动」存成一个强类型属性 <see cref="LiveOp"/>（`T LiveOp { get; set; }`，自动属性）。
    /// <para>用途（判据 §四）：**奖励页 + 商店页共用一窗**的那种活动窗（扩张通行证 / 双阵营那一类）。</para></summary>
    public class LiveOpsEventWindowWithTabs<T> : LiveOpsEventWindowWithTabs where T : class, ILiveOps
    {
        /// <summary>原版 `public T LiveOp { get; protected set; }`（自动属性，`get_`/`set_` 都是共享桩
        /// ⇒ **纯字段读写，没有副作用**）。</summary>
        public T LiveOp { get; protected set; }

        /// <summary>原版 `protected override void SetupData(object data, GameWindowOptions options)`
        /// （`…___SetupData__1016780.c`，**逐句**）：
        /// <code>
        /// base.SetupData(data, options);                       // ← 先写基类那两个数据字段
        /// if (&lt;base.Data != null&gt;) LiveOp = (T)data;           // ← isinst 强转，失败 throw
        /// </code>
        /// 🔴 **`data == null` 时不碰 `LiveOp`**（不是「清空」）—— 反汇编里那一跳是 `if (this+0x40 != 0)`。
        /// ⚠️ 类型不对时原版 **throw**（`FUN_1803f3780` ⇒ `InvalidCastException`）；
        /// 我们出声 + 保留原值（全工程口径：坏数据不让游戏崩）。
        /// ⚠️ 差异 ①：我们基类 `SetupData` 只有一个参数。</summary>
        protected override void SetupData(object data)
        {
            base.SetupData(data);
            if (data == null) return;                      // ★ 原版：Data 为空就不碰 LiveOp
            var t = data as T;
            if (t == null)
            {
                Debug.LogError("[LiveOpsWindowTabs] SetupData：data 不是 " + typeof(T).Name
                             + "（实际 " + data.GetType().Name + "）；原版这一跳 throw ⇒ 保留原 LiveOp");
                return;
            }
            LiveOp = t;
        }

        /// <summary>原版 `public sealed override void InitializeWindow(ILiveOps liveOpsEvent)`
        /// （`…___InitializeWindow__1016700.c`，**全部内容**）：
        /// `LiveOp = liveOpsEvent as T;`  —— 注意是 **`as`**（轻转），**不是** CT 强转：
        /// 反汇编里是 `thunk_FUN_18042e360`（isinst），失败得 `null` 而**不抛**。
        /// <para>⚠️ 与 `SetupData` 那一跳的差别就在这：`SetupData` **强转会抛**，这里 **`as` 给 null**。照抄。</para></summary>
        public sealed override void InitializeWindow(ILiveOps liveOpsEvent)
        {
            LiveOp = liveOpsEvent as T;
        }
    }
}
