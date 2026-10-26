// ItemDrawerLeash.cs —— 原版 `ItemDrawer.OnDestroy` 的等价物：**节点被销毁 ⇒ 句柄自动放掉**
//
// ============================================================ 判据
//  · 🥈 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ItemDrawer.cs:134-141`（`OnDestroy`）：
//      `OnRelease();` + `if (drawerReferences.TryGetValue(this, out ref)) ref.Release(General);`
//    ⇒ 原版 `ItemDrawer` 是**组件**，它的 GameObject 一销毁，`OnDestroy` 就把 `drawerReferences` 里那一笔放掉。
//  · 🥈 `ItemDrawer.cs:36-68`（静态 `Draw` 末段）= `drawerReferences.Add(itemDrawer, ref)` = 我们 `ItemDrawer.Bind`
//    里的 `_handles[v] = res.Drawer`（`Shell/ItemDrawer.cs:1871`）。
//  · 账：`资料/完全复刻/账/收藏.tsv` 的 `ItemDrawer.OnDestroy()` 那一行（原状态 C）。
//
// ============================================================ 为什么需要这颗小件
//  我们的抽屉**不是**组件：`ItemDrawerView` 是纯 C# 句柄，只握着 `Node`（那棵子树的 Transform）。
//  于是「组件随节点销毁」这个事件在本仓**不存在** ⇒ 句柄表 `_handles` 只增不减（**强引用**，被销毁
//  的视图也回收不掉、开窗/画格越多表越大），而 `ItemDrawer.Release(...)` 全仓 **0 个生产调用点**。
//  ⇒ 用一颗极薄的 MonoBehaviour 把「节点没了」这件事接住（**照 `Shell/ShadeHitMark.cs` 的先例**：
//    那里也是「一张只增不减的静态 HashSet」换成「挂在节点自己身上的一颗标记」）。
//
// 🔴 **类名必须与文件名相同**（`A1506`）：否则 Unity 建不出 MonoScript ⇒ 存场景时被烘成内联
//    `!u!115` 存根 ⇒ player 加载即崩。⛔ 别把本类并进 `ItemDrawer.cs`。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>挂在抽屉根节点上，节点销毁时把那一格从 `ItemDrawer._handles` 里放掉。</summary>
    sealed class ItemDrawerLeash : MonoBehaviour
    {
        /// <summary>挂在哪一格上（原版 `OnDestroy` 里的 `this`）。</summary>
        internal ItemDrawerView View;

        /// <summary>把这一格绑到 <paramref name="view"/> 的节点上（同一节点重复绑 ⇒ 只改指向，不叠加组件）。</summary>
        internal static void Attach(ItemDrawerView view)
        {
            if (view == null || view.Node == null) return;      // 没画出来的那一支（`Node == null`）无句柄可放
            var go = view.Node.gameObject;
            var leash = go.GetComponent<ItemDrawerLeash>();
            if (leash == null) leash = go.AddComponent<ItemDrawerLeash>();
            leash.View = view;
        }

        /// <summary>原版 `ItemDrawer.OnDestroy`（`ItemDrawer.cs:134-141`）。
        /// ⚠️ 原版这一跳还会调 `OnRelease()`（它在原版里是**放 Addressables 句柄**）；我们走 `Resources`
        /// 没有加载句柄层（见 `ItemDrawer.cs` 追加区头注 ③）⇒ 净效果等价物 = **解除登记**这一半。</summary>
        void OnDestroy()
        {
            var v = View;
            View = null;                                        // 先清，防重入（Destroy 只会来一次，但别赌）
            if (v != null) ItemDrawer.Release(v);
        }
    }
}
