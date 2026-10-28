// MainMenuOfferContainer.cs — 主菜单的「报价格容器」（原版 `MainMenuOfferContainer`）· 执行代理 B93_E13（2026-10-28）
//
// ============================ 它是谁（判据 = 第一权威反编译方法体）============================
// 原版 `MainMenuOfferContainer : LiveopMenuContainer`，`OnInitialize(LiveOpsEvent playEvent)` 的体 **四拍**：
//   ① `base.OnInitialize(playEvent)`                      ← `LiveopMenuContainer`（标题 / 计时器 / 背景图三件）
//   ② `var shopOfferEvent = playEvent as ShopOfferEvent;`  ← 判型（不是 ⇒ **null**）
//   ③ `var drawer = GetComponent<GeneralOfferPopupDrawerBase>();`  ← 同 GO 上找抽屉；**没有就 return**
//   ④ `drawer.Initialize(shopOfferEvent.GetBaseData());`  ← 把报价的 `ShopOfferBase` 灌进抽屉
//
// 判据出处（逐份现读）：
//   · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/MainMenuOfferContainer.cs`（**类型名 / 方法名全在这一份**）
//   · `d:/2/tools/decomp_full/MainMenuOfferContainer__OnInitialize.c`（控制流：那三道 null 闸的**顺序**）
//   · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/GeneralOfferPopupDrawerBase.cs`
//     ⇒ 基类 `Initialize(offer)` = `DrawRewards(offer is ShopOfferDataV2 ? offer.Items.Concat(那一串) : offer.Items)`；
//       `ReleaseAssets()` = **TODO 空实现**（只 `CustomDebug.LogWarning("[TODO] Implement itemdrawer release")`）
//   · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/{ShopOfferEvent,ShopOfferEventV2}.cs`
//     ⇒ `GetBaseData()` 返回 `ShopOfferBase`（`Items` 是 `IEnumerable<RewardInfo>`）
//   · 抽屉那一半的**对象行为**（`currentItemDrawers` / `scroll.ManualInit` / 谓词 / SpliceWhere）
//     = `Shell/OfferContainer.cs` 的 `Carousel`（2026-10-28 本批补全，判据写在那一段头上）
//
// ============================ 🔴 我们这一侧的三条边界（⛔ 别当成原版）============================
// ① **`LiveopMenuContainer` 那一半不在这里重写**：主菜单模式卡的文字/图/计时器已经有一套等价实现
//    （`Shell/MainMenuRuntime.cs:3993` 起，判据 `LiveopMenuContainer.cs:35-104`）
//    ⇒ 本类把 ① 拍做成**注入的 `BaseInitializer`**（宿主传它自己的那一段）。两处各写一份 = 迟早不一致。
// ② **`as ShopOfferEvent` 那一拍的 null 支不同**：原版 `MainMenuOfferContainer__OnInitialize.c` 末尾那个
//    `FUN_1803f47a0()` **是不返回的**（= `throw NullReferenceException`）⇒ 原版在「事件不是报价格事件」时**抛空引用**。
//    我们改成**出声 + 早退**（🔴 有意偏离）：本类只会被「报价格事件」建出来（原版同理）⇒ 那一支根本不可达，
//    照抄一个 NRE 只会把崩溃风险搬进来，而本工程红线是「不许静默失败」，出声能看见就行。
// ③ **没有 `GetComponent` 的按类型搜**：本仓的场景是**扁平 quad 树**（没有「同一 GO 上挂一堆组件」那种形态）
//    ⇒ 抽屉的等价物 = 一个**子节点上的 `MainMenuOfferDrawer`**（`GetComponentInChildren`），
//    或宿主显式塞进来的引用（`Drawer`）。⛔ 不为了「像原版」而去建 MonoBehaviour 组件族。
//
// ============================ 今天的可达性（如实记）============================
// · **没有任何调用方**：主菜单的模式卡还没把这件挂上去（`Shell/MainMenuRuntime.cs` 不在本批白名单里）。
//   ⇒ 本文件是**从零新建的组件**，等宿主接线；账里这一条记 **B**（形到、宿主未接）。
// · **数据侧是通的**：报价数据层已在（`Shell/LiveOpsEventFactory.cs:6` 注册了 `ShopOfferEvent` / V2；
//   `Shell/ShopTabPage.cs:809` 是 V2 开容器那条路）⇒ 缺的只有「主菜单那件容器」，与本文件对应。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>= 原版 `ShopOfferBase`（报价的**基数据**：一条报价 = 若干 `RewardInfo`）。
    /// <para>本地对位 = `ShopData` 那一族的数据形状（`ShopOffer.Grants` 就是 `RewardSpec[]`），
    /// ⛔ **不是**「我们编了一份报价表」—— 本类只是搬运口径。</para></summary>
    public sealed class OfferBaseData
    {
        /// <summary>= 原版 `IIdentifiable.Id` / `GetID()`（告警与去重都用它）。</summary>
        public string Id;
        /// <summary>= 原版 `ShopOfferBase.Items`。</summary>
        public CampaignData.RewardSpec[] Items;
        /// <summary>= 原版 `ShopOfferDataV2` **多出来**的那一串（基类 `Initialize` 里 `Items.Concat(它)`）。
        /// **`null` = 不是 V2**（= 原版 `is ShopOfferDataV2` 判假）⇒ 不 Concat。</summary>
        public CampaignData.RewardSpec[] V2Extra;
    }

    /// <summary>= 原版 `LiveOpsEvent&lt;…&gt;`（报价格事件）的最小对位：`Id` + `GetBaseData()`。
    /// <para>⚠️ **只承载本容器用到的两格**（判型 + 基数据），⛔ 不冒充整个 liveops 事件族。</para></summary>
    public sealed class OfferEventData
    {
        public string Id;
        public OfferBaseData Base;
    }

    /// <summary>= 原版 `GeneralOfferPopupDrawerBase`（抽象抽屉）。四个成员一一对位：
    /// <see cref="Initialize"/>（虚，体 = `DrawRewards(...)`）· <see cref="DrawRewards"/> · <see cref="DoReset"/> ·
    /// <see cref="ReleaseAssets"/>（**原版是 TODO 空实现**）。</summary>
    public abstract class MainMenuOfferDrawer : MonoBehaviour
    {
        /// <summary>= 原版 `currentItemDrawers`（自检读口；顺序 = 画出顺序）。</summary>
        public abstract int DrawerCount { get; }

        /// <summary>= 原版 `GeneralOfferPopupDrawerBase.Initialize(ShopOfferBase shopOfferData)`：
        /// `DrawRewards(offer is ShopOfferDataV2 ? offer.Items.Concat(v2 那一串) : offer.Items)`。
        /// <para>⚠️ 原版这里**没有 null 闸**（`shopOfferData.Items` 会直接解引用）⇒ 传 null 就是 NRE；
        /// 我们出声 + 早退（同文件头第 ② 条口径）。</para></summary>
        public virtual void Initialize(OfferBaseData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[MainMenuOffer] 抽屉 `Initialize(null)`：原版 `GeneralOfferPopupDrawerBase.Initialize` "
                               + "在这里会解引用空报价抛 NRE ⇒ 我们出声 + 早退（⛔ 不假装画了东西）。");
                return;
            }
            DrawRewards(data.Items, data.V2Extra);
        }

        public abstract void DrawRewards(IEnumerable<CampaignData.RewardSpec> items,
                                         IEnumerable<CampaignData.RewardSpec> v2Extra);

        /// <summary>= 原版 `DoReset()`（逐抽屉 `ReleaseAssets()` + `Destroy(gameObject)`，然后清表）。</summary>
        public abstract void DoReset();

        /// <summary>= 原版 `ReleaseAssets()`：**TODO 空实现**，体里只有
        /// `foreach (currentItemDrawers) CustomDebug.LogWarning("[TODO] Implement itemdrawer release")`
        /// ⇒ **照抄原版**：只出声、不释放（⛔ 别替原版把它实现出来）。</summary>
        public virtual void ReleaseAssets()
        {
            int n = DrawerCount;
            for (int i = 0; i < n; i++)
                Debug.LogWarning("[MainMenuOffer] `ReleaseAssets()`：原版这一跳是 **TODO 空实现**"
                               + "（只刷 `[TODO] Implement itemdrawer release`）⇒ 照抄：只出声、不释放。");
        }
    }

    /// <summary>= 原版 `CarouselOfferPopupDrawer` 的落地版：行为全在 `OfferContainer.Carousel.State`
    /// （`Initialize` = `base.Initialize` + `scroll.ManualInit`，`DrawRewards` 四拍，`DoReset` 两拍 ——
    /// 判据写在那一段头上，⛔ 不在这里抄第二份）。
    /// <para>本类只做 **Unity 侧的那一层**：把「画一条抽屉」落到 `anchor` 下的节点上（今天的 `Draw` 只建节点，
    /// 奖励图/字走 `ItemDrawer`，与别处同一套）。</para></summary>
    public sealed class MainMenuCarouselDrawer : MainMenuOfferDrawer
    {
        /// <summary>`ItemDrawer.Draw` 的宿主（原版 `contentAnchor`）。</summary>
        public Transform ContentAnchor;
        /// <summary>一屏几槽（= `UI_InfiniteScroll` 从布局量的那个数；那份源码本地没有 ⇒ 由宿主给）。</summary>
        public int Visible = 1;
        /// <summary>把奖励映射到原版 `ObtainableItem` 子类型名（谓词② `x.Item is Currency` 的输入；
        /// 同一份口径 = `OfferContainer.Content.ItemType`）。`null` ⇒ 谓词②判不了（出声一次）。</summary>
        public System.Func<CampaignData.RewardSpec, string> TypeOf;

        /// <summary>行为本体（`OfferContainer.Carousel.State`）。</summary>
        public readonly OfferContainer.Carousel.State State = new OfferContainer.Carousel.State();

        public override int DrawerCount { get { return State.CurrentItemDrawers.Count; } }

        public override void DrawRewards(IEnumerable<CampaignData.RewardSpec> items,
                                         IEnumerable<CampaignData.RewardSpec> v2Extra)
        {
            // ① `base.Initialize` 那一拍（`DrawRewards`） ② `scroll.ManualInit()`
            State.Initialize(items, v2Extra, TypeOf, DrawOne, Visible);
        }

        /// <summary>一格抽屉的落点框。⚠️ **我们挑的**（原版 `ItemDrawer.Draw` 按 `contentAnchor` 的布局自己摆，
        /// 无限滚动那份源码本地没有 ⇒ 宿主给一个显式框；`0` 宽/高 ⇒ 不画并出声）。</summary>
        public PxRect SlotBox = new PxRect(0f, 0f, 200f, 200f);
        /// <summary>渲染队列基档（宿主必须按**它所在的那一层**给，同 `OfferContainer.Build` 的 `qBase`）。
        /// 🔴 `0` = 没给 ⇒ 不画 + 出声（⛔ 不编一个档位：分层错会静默盖住别人）。</summary>
        public int QBase;

        bool DrawOne(CampaignData.RewardSpec r, int qty)
        {
            if (ContentAnchor == null)
            {
                Debug.LogWarning("[MainMenuOffer] 轮播抽屉没有 `ContentAnchor`（原版 `contentAnchor`）⇒ "
                               + "`ItemDrawer.Draw` 无处落，这一条**没画**（原版会直接 NRE）。");
                return false;
            }
            if (QBase <= 0)
            {
                if (!_noQueueSaid)
                {
                    _noQueueSaid = true;
                    Debug.LogWarning("[MainMenuOffer] 轮播抽屉没给 `QBase`（渲染队列基档）⇒ **一格都不画**"
                                   + "（本工程分层靠队列、不靠 z，⛔ 不编一个档位）。");
                }
                return false;
            }
            var it = ItemDrawer.Spec(r.Id, null, r.Id);
            var v = ItemDrawer.DrawView(ContentAnchor, SlotBox, it, qty, DrawerOverride.OfferPopups,
                                        ItemDrawerStyle.Default(QBase, QBase + 1, QBase + 2));
            if (v == null) return false;
            v.ToggleShowInfoOnClick(false);            // 原版 `DrawItemReward` 第三句
            return true;
        }

        bool _noQueueSaid;

        public override void DoReset()
        {
            State.DoReset(e =>
            {
                // 原版 = `Object.Destroy(drawer.gameObject)`；我们这一侧的节点在 `ContentAnchor` 下
                // ⚠️ 不做「按序删节点」的猜测 —— 由 `ItemDrawer` 的句柄回收（同 `GachaEventWindow.ClearRewardContainers` 口径）
            });
        }
    }

    /// <summary>= 原版 `MainMenuOfferContainer`（主菜单那张**报价格卡**：卡片本体 + 里面的报价格抽屉）。
    /// <para>四拍见文件头。**本件不建任何节点**（原版这是挂在 prefab 上的组件）⇒ 宿主先摆好卡，再把本组件挂上。</para></summary>
    public sealed class MainMenuOfferContainer : MonoBehaviour
    {
        /// <summary>= 原版 `GetComponent&lt;GeneralOfferPopupDrawerBase&gt;()` 那个抽屉。
        /// `null` ⇒ 建树时 `GetComponentInChildren` 自己找（同族闸：**找不到就 return**，原版如此）。</summary>
        public MainMenuOfferDrawer Drawer;

        /// <summary>= 原版 `base.OnInitialize(playEvent)` 那一拍（`LiveopMenuContainer`：标题 / 计时器 / 背景图）。
        /// <para>🔴 **宿主注入**（见文件头 ①）：主菜单模式卡那一段已在 `Shell/MainMenuRuntime.cs`，
        /// 本类不重写它。`null` ⇒ 只出声一次（⛔ 不静默跳过）。</para></summary>
        public System.Action<OfferEventData> BaseInitializer;

        /// <summary>最后一次 `OnInitialize` 的实参（自检读口）。</summary>
        public OfferEventData LastEvent { get; private set; }
        /// <summary>`OnInitialize` 被调了几次。</summary>
        public int InitializeCalls { get; private set; }
        /// <summary>抽屉有没有被喂到基数据（`true` = 原版第 ④ 拍真的跑了）。</summary>
        public bool DrawerInitialized { get; private set; }

        bool _noBaseSaid, _noDrawerSaid, _notShopEventSaid;

        /// <summary>= 原版 `MainMenuOfferContainer.OnInitialize(LiveOpsEvent playEvent)`（四拍）。</summary>
        public void OnInitialize(OfferEventData playEvent)
        {
            InitializeCalls++;
            LastEvent = playEvent;
            DrawerInitialized = false;

            // ---- ① `base.OnInitialize(playEvent)` ----
            var b = BaseInitializer;
            if (b != null) b(playEvent);
            else if (!_noBaseSaid)
            {
                _noBaseSaid = true;
                Debug.LogWarning("[MainMenuOffer] `base.OnInitialize` 那一拍没接：原版是 `LiveopMenuContainer.OnInitialize`"
                               + "（标题 / 计时器 / 背景图三件）。主菜单模式卡那一段在 `Shell/MainMenuRuntime.cs`，"
                               + "本类不重写它 ⇒ 宿主必须传 `BaseInitializer`（⛔ 不静默跳过）。");
            }

            // ---- ③ 先找抽屉；**找不到就 return**（原版 `if ((bool)component)` 那道闸在最前面）----
            var drawer = Drawer != null ? Drawer : GetComponentInChildren<MainMenuOfferDrawer>(true);
            if (drawer == null)
            {
                if (!_noDrawerSaid)
                {
                    _noDrawerSaid = true;
                    Debug.LogWarning("[MainMenuOffer] 这张卡上没有报价格抽屉（原版 `GetComponent<GeneralOfferPopupDrawerBase>()` "
                                   + "取不到就 **return**）⇒ 卡建出来了、里面不装东西。");
                }
                return;
            }

            // ---- ② `playEvent as ShopOfferEvent` ----
            //   ⚠️ 原版这一支 = `throw NullReferenceException`（`.c` 末尾 `FUN_1803f47a0` 不返回）；
            //      我们出声 + 早退（文件头 ②，有意偏离）。
            if (playEvent == null)
            {
                if (!_notShopEventSaid)
                {
                    _notShopEventSaid = true;
                    Debug.LogWarning("[MainMenuOffer] 事件不是报价格事件（原版 `playEvent as ShopOfferEvent` 得 null ⇒ "
                                   + "随后 `GetBaseData()` **抛空引用**）⇒ 我们出声 + 早退，⛔ 不假装初始化过。");
                }
                return;
            }

            // ---- ④ `drawer.Initialize(shopOfferEvent.GetBaseData())` ----
            drawer.Initialize(playEvent.Base);
            DrawerInitialized = true;
        }
    }
}
