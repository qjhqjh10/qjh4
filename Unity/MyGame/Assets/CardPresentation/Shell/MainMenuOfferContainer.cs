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
// · ✅ **已接线**（2026-11-03 `B128` 就地订正：原写「没有任何调用方 / 主菜单还没挂」**已过期**）—— 真挂点在
//   `Shell/MainMenuRuntime.cs` 的 `BindMainMenuOfferContainer`（由 `BuildModeCardForCard` 调用）；**真原因**是它喂的 `OfferEventData.Base = null`（报价内容在远端）。
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
        /// <summary>一屏几槽（= `UI_InfiniteScroll` 从布局量的那个数 ⇒ 由宿主给）。⚠️ **2026-11-01（`B115_W19`）就地订正（铁律 5）**：原写「那份源码本地没有」——**错**，真体在 `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/UI_InfiniteScroll.cs`（448 行，`Init` :186 从 `content.childCount` 量）；`Shell/ShopCarousel.cs`（2026-10-31）已据它建成离散等价，本类仍由宿主给。</summary>
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
            // 🆕 2026-11-03（B128 · 工具-39）：① **布局量** —— 原版 `SetItems` 的 `_itemCount` + `DisableGridComponents` 的两把尺（`.c` 判据见类尾）
            Ring.SetItems(State.CurrentItemDrawers.Count, SlotBox.W + SlotGap); Ring.AutoScroll = State.AutoScroll;
        }

        /// <summary>一格抽屉的落点框。⚠️ **我们挑的**（原版 `ItemDrawer.Draw` 按 `contentAnchor` 的布局自己摆，
        /// ⇒ 宿主给一个显式框；`0` 宽/高 ⇒ 不画并出声）。🔴 2026-11-03（`B128`）就地订正：原写「无限滚动那份源码本地没有」——**错**（真体 `out_il2csharp/…/UI_InfiniteScroll.cs` 448 行）。</summary>
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
            Ring.SetItems(0, 0f);                       // 🆕 2026-11-03（B128）：`DoReset` = 抽屉全销毁 ⇒ 环也清空（原版 `Destroy` 之后 `childCount` 归 0）
        }

        // ================================================================================================
        //  🆕 2026-11-03（`B128` · 旧任务 `工具-39`）：**`UI_InfiniteScroll` 第 2 实例的 5 格**
        // ================================================================================================
        //  判据（第一权威 = 反编译方法体，2026-11-03 现读）：
        //    · `d:/2/tools/decomp_full/UnityEngine.UI.Extensions.UI_InfiniteScroll__Init.c` —— 记 `onValueChanged` →
        //      `OnScroll` · `_scrollRect.enabled = AllowManualScroll` · `initialized = true` → `SetItems()`
        //    · `…__SetItems.c` —— `_itemCount (+0x84) = content.childCount`
        //    · `…__DisableGridComponents.c` —— `_recordOffsetX (+0x88) = |item[1].anchoredPosition.x − item[0].x|` ·
        //      `_disableMarginX (+0x60) = _itemCount × _recordOffsetX × 0.5`（Y 那一对同形）
        //    · `…__OnScroll.c` —— 逐件 `InverseTransformPoint` ⇒ 越界 `anchoredPosition ±= _itemCount × _recordOffsetX`
        //      + `SetAsFirstSibling`/`SetAsLastSibling`；⚠️ **正侧比 `_disableMarginX + _threshold`、负侧只比
        //      `−_disableMarginX`** —— 原版自己就不对称，**照抄，⛔ 别「修正」**
        //    · `…__Update.c` —— `initialized && AutoScroll` ⇒ `content.anchoredPosition += AutoScrollSpeed × dt` → `OnScroll()`
        //    · `…__SetNewItems.c` —— 清 `items` → 逐子件 `DestroyImmediate` → 新件 `SetParent(content)` → `SetItems()`
        //  原版 prefab 实读（`Main Menu Offer Container*`）：`AutoScroll = 1` · `AutoScrollSpeed = (-50, 0)` · `_threshold = 100`。
        //
        //  ⚠️ **我们这一层没有 `ScrollRect` / `LayoutGroup`**（扁平 quad 树，见文件头 ③）⇒ 这 5 格落在
        //    `InfiniteScrollRing`（**纯数据、不碰 Unity 对象** ⇒ 离线可断言、不依赖帧循环；推进由宿主喂 `Tick(dt)`，
        //    与 `MenuScroll.Tick` 同一条口径）。今天的**可见效果 = 0**：每份报价只有 1 条奖励、且
        //    `MainMenuRuntime.BindMainMenuOfferContainer` 喂的 `OfferEventData.Base` 是 `null`（报价内容在远端）
        //    ⇒ 抽屉一条都不画（那条既有告警会出声）。补齐的是**语义与读数**，接远端数据那天直接生效。
        //  🔶 自己设计（非原版）：**槽距**取 `SlotBox.W + SlotGap`（原版由布局组量，那份布局组被它自己在运行时
        //    `DisableGridComponents` 关掉、本地只有 prefab 的 `_threshold/AutoScrollSpeed` 三个值）· 出厂 `SlotGap = 0`
        //    · 调度台 2026-11-03 指派（B128）。

        /// <summary>= 原版 `CarouselOfferPopupDrawer.scroll`（这一份 `UI_InfiniteScroll` 实例）。</summary>
        public readonly InfiniteScrollRing Ring = new InfiniteScrollRing();

        /// <summary>槽距的附加间隙。<c>0</c> ⇒ 槽距 = `SlotBox.W`。⚠️ **我们挑的**（见本节头「🔶 自己设计」）。</summary>
        public float SlotGap;

        /// <summary>③ = 原版 `Update()`。宿主每 tick 喂 <c>dt</c>（秒）—— 批处理下没有帧循环，⛔ 不自己开 `Update`。</summary>
        public void Tick(float dt) { Ring.Tick(dt); }

        /// <summary>⑤ = 原版 `SetNewItems(ref List&lt;Transform&gt;)`：**换数据**（清空 + 重建 + 重新量尺）。
        /// <para>原版逐子件 `DestroyImmediate` 再 `SetParent` 新件；我们走 <see cref="State.DoReset"/> 销毁句柄 + `Initialize` 重画。</para></summary>
        public void SetNewItems(IEnumerable<CampaignData.RewardSpec> items,
                                IEnumerable<CampaignData.RewardSpec> v2Extra = null)
        {
            DoReset();
            DrawRewards(items, v2Extra);
        }
    }

    /// <summary>= 原版第三方库 `UnityEngine.UI.Extensions.UI_InfiniteScroll` 的**数据等价物**（第 2 实例那 5 格，
    /// 判据与差异见 `MainMenuCarouselDrawer` 类尾那一段）。
    /// <para>纯数据：不持有任何 Unity 对象 ⇒ 自检可离线驱动（`Tick` / `ScrollTo` 都是显式调用，不依赖帧循环）。</para></summary>
    public sealed class InfiniteScrollRing
    {
        /// <summary>① = 原版 `_itemCount`（`content.childCount`）。</summary>
        public int ItemCount { get; private set; }
        /// <summary>① = 原版 `_recordOffsetX`（相邻两件槽距的**绝对值**）。</summary>
        public float RecordOffsetX { get; private set; }
        /// <summary>① = 原版 `_disableMarginX`（`_itemCount × _recordOffsetX × 0.5`）。</summary>
        public float DisableMarginX { get; private set; }
        /// <summary>④ = 原版 `_threshold`（`…__DisableGridComponents` 不碰它；prefab 实读 **100**）。</summary>
        public float Threshold = 100f;
        /// <summary>③ = 原版 `AutoScroll`（prefab 实读 **1**；`DrawRewards` 末尾在条数 < 4 时会置 false）。</summary>
        public bool AutoScroll = true;
        /// <summary>③ = 原版 `AutoScrollSpeed`（prefab 实读 **(-50, 0)**）。本环只用 x（原版横向才取 x、纵向取 y）。</summary>
        public Vector2 AutoScrollSpeed = new Vector2(-50f, 0f);
        /// <summary>③ = 原版 `content.anchoredPosition.x` 的等价物（`Tick` 推进它、`ScrollTo` 直接给）。</summary>
        public float ContentX { get; private set; }
        /// <summary>② 搬件次数（自检读口 = 原版 `SetAsFirstSibling` / `SetAsLastSibling` 的调用次数）。</summary>
        public int WrapCount { get; private set; }
        /// <summary>② `OnScroll` 被调过几次（= 原版 `onValueChanged` 那条链的次数）。</summary>
        public int ScrollCount { get; private set; }

        readonly List<float> _x = new List<float>();

        /// <summary>= 原版 `SetItems()` + `DisableGridComponents()` 那两把尺：建 <paramref name="count"/> 件、
        /// 按 <paramref name="pitch"/> 依次摆（<c>x_i = i × pitch</c>），`_disableMarginX = count × pitch × 0.5`。</summary>
        public void SetItems(int count, float pitch)
        {
            _x.Clear();
            RecordOffsetX = pitch > 0f ? pitch : 0f;
            ItemCount = count > 0 ? count : 0;
            for (int i = 0; i < ItemCount; i++) _x.Add(i * RecordOffsetX);
            DisableMarginX = ItemCount * RecordOffsetX * 0.5f;
            ContentX = 0f;
            WrapCount = 0;
            ScrollCount = 0;
        }

        /// <summary>第 <paramref name="i"/> 件的局部 x（= 原版 `items[i].anchoredPosition.x`）。</summary>
        public float PositionOf(int i) { return i >= 0 && i < _x.Count ? _x[i] : 0f; }

        /// <summary>第 <paramref name="i"/> 件相对滚动视口的坐标（= 原版 `InverseTransformPoint(...).x`）。</summary>
        public float LocalOf(int i) { return PositionOf(i) - ContentX; }

        /// <summary>② = 原版 `OnScroll(Vector2)`：逐件判越界 ⇒ 把那一件搬**一整圈**到另一头。
        /// <para>⚠️ **不对称照抄原版**：正侧比 `_disableMarginX + _threshold`，负侧只比 `−_disableMarginX`
        /// （`…__OnScroll.c` 第二支那个 XOR 只翻了 `_disableMarginX` 的符号位、**没加 `_threshold`**）。</para></summary>
        public void OnScroll()
        {
            ScrollCount++;
            if (ItemCount <= 0 || RecordOffsetX <= 0f) return;
            float span = ItemCount * RecordOffsetX;
            for (int i = 0; i < _x.Count; i++)
            {
                float local = _x[i] - ContentX;
                if (DisableMarginX + Threshold < local) { _x[i] -= span; WrapCount++; }     // 原版：SetAsFirstSibling()
                else if (local < -DisableMarginX) { _x[i] += span; WrapCount++; }           // 原版：SetAsLastSibling()
            }
        }

        /// <summary>③ = 原版 `Update()`：`content.anchoredPosition += AutoScrollSpeed × dt` ⇒ `OnScroll()`。</summary>
        public void Tick(float dt)
        {
            if (!AutoScroll || ItemCount <= 0 || dt <= 0f) return;
            ContentX += AutoScrollSpeed.x * dt;
            OnScroll();
        }

        /// <summary>手动滚动口（= 原版 `ScrollRect.onValueChanged` 的等价物）：直接给内容偏移再判越界。</summary>
        public void ScrollTo(float contentX) { ContentX = contentX; OnScroll(); }
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
