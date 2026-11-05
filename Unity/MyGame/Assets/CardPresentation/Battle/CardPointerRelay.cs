// CardPointerRelay.cs — 原版 `CardCollider`（`D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/CardCollider.cs:7`）的**送达半**转发件
//
// 【判据】原版**卡自己就是 uGUI 事件接收者**：
//   `public abstract class CardCollider : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
//    IDragHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
//    IEndDragHandler, IBeginDragHandler`（**8 个**；`CardCollider2D : CardCollider` 见同目录 `CardCollider2D.cs:10`），
//   每个体的形状都是「① 有 ScrollRect 就转给 ScrollRect；② 转给 `currentCard` 的 `OnTouch*` 方法」。
//   ⇒ **我们只复刻「送达」这一半**：业务逻辑仍在 `CardInteraction` / `BattleDriver` 的既有口里，
//     ⛔ 本类不复制任何业务判定（不判能不能拖、不判目标合法）。
//
// 【两个刻意不做的】
//   ① `sendEventsToScrollRect`（原版把卡上的拖拽转交给所在 ScrollRect）—— 战斗手牌 / 换牌区都不在
//      ScrollRect 里（多卡窗那条滚动是 `MultiCardDisplay` 自己的世界坐标轮询）⇒ 本批不建那个字段；
//   ② `allowClick` 那套「按下 → 移出 → 取消点击」的状态机 —— 我们的 `CardInteraction` 已有 `_pressInside` 同义物。
//
// 【为什么挂在卡根、collider 在子节点也没问题】`Physics2DRaycaster.Raycast` 把 `RaycastResult.gameObject`
//   填成 **collider 所在那个 GameObject**（`_tiltRoot`），而 uGUI 派发时走 `ExecuteEvents.ExecuteHierarchy`
//   **沿父链往上找**第一个实现了该接口的组件 ⇒ 挂在根上的本类照样收到。⛔ 别为此把 collider 挪到根上。
//
// 【共存开关】`BattlePointerRouter.Available` = false（没有射线器 / 批处理 / 兜底 2D 档）⇒ 消费端照旧轮询
//   `CardInteraction.Frontmost` / `BattleDriver.HitSlot`，行为与今天**逐位相同**。⛔ 别在别处再判一次。
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardPresentation
{
    /// <summary>把 uGUI 指针事件转发给 <see cref="BattlePointerRouter"/>（= 原版 `CardCollider` 的送达半）。
    /// <para>⚠️ 一张卡只挂**一只**；`CardView.Create` 建的时候就调 <see cref="Attach"/>（幂等）。
    /// ⚠️ 类名 == 文件名（否则场景存盘会烘成内联 `MonoScript` 存根 `!u!115`，player 加载即崩 —— 见 CLAUDE.md §二）。</para></summary>
    public sealed class CardPointerRelay : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        CardView _card;

        /// <summary>自检读口：这一只转发件挂在哪张卡上（`null` = 还没 `Attach`）。</summary>
        public CardView CardForTest { get { return _card; } }

        /// <summary>累计收到过几个指针事件（自检读口：证明「射线器真的把事件送到了」，不是空挂）。</summary>
        public static int EventsDeliveredForTest { get; private set; }

        /// <summary>自检口径清零（只清计数，不拆组件）。</summary>
        public static void ResetCountersForTest() { EventsDeliveredForTest = 0; }

        /// <summary>建/复用一只转发件并绑到 <paramref name="card"/>（幂等）。</summary>
        public static CardPointerRelay Attach(CardView card)
        {
            if (card == null) return null;
            var r = card.GetComponent<CardPointerRelay>();
            if (r == null) r = card.gameObject.AddComponent<CardPointerRelay>();
            if (r != null) r._card = card;
            return r;
        }

        // 🔴 转发目标 = 一个静态路由（`BattlePointerRouter`），由 `CardInteraction` / `MulliganPanel` 消费。
        //    ⛔ 本类**不调业务方法**（否则一次点击会做两件事：relay 直接干活 + 轮询又干一次）。
        public void OnPointerDown(PointerEventData e) { EventsDeliveredForTest++; BattlePointerRouter.Note(_card, BattlePointerRouter.Edge.Down, e); }
        public void OnPointerUp(PointerEventData e) { EventsDeliveredForTest++; BattlePointerRouter.Note(_card, BattlePointerRouter.Edge.Up, e); }
        public void OnPointerClick(PointerEventData e) { EventsDeliveredForTest++; BattlePointerRouter.Note(_card, BattlePointerRouter.Edge.Click, e); }
        public void OnPointerEnter(PointerEventData e) { EventsDeliveredForTest++; BattlePointerRouter.Note(_card, BattlePointerRouter.Edge.Enter, e); }
        public void OnPointerExit(PointerEventData e) { EventsDeliveredForTest++; BattlePointerRouter.Note(_card, BattlePointerRouter.Edge.Exit, e); }
        public void OnBeginDrag(PointerEventData e) { EventsDeliveredForTest++; BattlePointerRouter.Note(_card, BattlePointerRouter.Edge.BeginDrag, e); }
        public void OnDrag(PointerEventData e) { EventsDeliveredForTest++; BattlePointerRouter.Note(_card, BattlePointerRouter.Edge.Drag, e); }
        public void OnEndDrag(PointerEventData e) { EventsDeliveredForTest++; BattlePointerRouter.Note(_card, BattlePointerRouter.Edge.EndDrag, e); }
    }

    /// <summary>指针事件的**一帧中转站** —— 唯一一处「EventSystem 说了算」的格子。
    /// <para><see cref="Available"/> = false（没有射线器 / 批处理 / 兜底 2D 档）⇒ 消费端照旧轮询
    /// `CardInteraction.Frontmost` / `BattleDriver.HitSlot`，行为与今天**逐位相同**
    /// （这就是共存策略的唯一开关，⛔ 别在别处再判一次）。</para>
    /// <para>⚠️ 只存「哪张卡 + 哪一帧」，**不存 `PointerEventData`**（跨帧存 uGUI 事件对象是坑）。</para></summary>
    public static class BattlePointerRouter
    {
        /// <summary>8 个指针边沿（与 `CardPointerRelay` 实现的 8 个接口一一对应）。</summary>
        public enum Edge { Down, Up, Click, Enter, Exit, BeginDrag, Drag, EndDrag }

        /// <summary>uGUI 这条路可不可用（唯一开关，由 `BattleDriver.Ensure()` 那一侧赋值）。</summary>
        public static bool Available;

        /// <summary>这一帧 uGUI 认为指针压在谁身上（`null` = 谁也不在）。</summary>
        public static CardView HoverCard;

        /// <summary>按下沿 / 松手沿 / 点击沿各自的帧号（`Time.frameCount`；消费端只认「本帧」）。</summary>
        public static int DownFrame = -1, UpFrame = -1, ClickFrame = -1;

        /// <summary>最近一次按下落在哪张卡上。</summary>
        public static CardView DownCard;

        /// <summary>自检/换局收尾：把开关和所有缓存清回「不可用」。</summary>
        public static void Reset()
        {
            Available = false;
            HoverCard = null;
            DownCard = null;
            DownFrame = UpFrame = ClickFrame = -1;
        }

        /// <summary>`CardPointerRelay` 的唯一投递口。</summary>
        public static void Note(CardView c, Edge e, PointerEventData ev)
        {
            int f = Time.frameCount;
            switch (e)
            {
                case Edge.Enter: HoverCard = c; break;
                case Edge.Exit: if (HoverCard == c) HoverCard = null; break;
                case Edge.Down: DownCard = c; DownFrame = f; break;
                case Edge.Up: UpFrame = f; break;
                case Edge.Click: ClickFrame = f; break;
            }
        }
    }
}
