// ShopCarousel.cs — 商店「轮播」的**显示层**（可见槽 → 奖励 的映射 + 拖一格推进）· `B106_W24` · 2026-10-31
//
// ============================ 它补的是哪一层 ============================
//  原版 `CarouselOfferPopupDrawer` 自身只有两句（`out_cpp2il_dev_ilspy/CarouselOfferPopupDrawer.cs:109-113`）：
//      `public override void Initialize(ShopOfferBase shopOfferData) { base.Initialize(shopOfferData); scroll.ManualInit(); }`
//  **「拖一格 ⇒ 槽位换（首尾回绕）」不在它身上，在第三方库 `UnityEngine.UI.Extensions.UI_InfiniteScroll`**：
//    · 真方法体：`d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/UI_InfiniteScroll.cs`
//      `OnScroll(Vector2 pos)`（`// RVA: 0x56DAA0`）逐句：逐个子件把世界坐标转到 ScrollRect 局部，
//      越过 `_disableMargin + _threshold` 就 `anchoredPosition ±= _itemCount * _recordOffset`
//      并 `SetAsFirstSibling()` / `SetAsLastSibling()` —— 也就是说**一列子件首尾相接成环**：
//      拖出去的那一件从另一头接回来，于是每个可见槽上坐的是**下一条**奖励。
//    · ⚠️ assetripper 导出的 `Assets/Plugins/.../UI_InfiniteScroll.cs` 方法体是**空壳**（`ManualInit() { }`），
//      真体只在上面那份 `out_il2csharp` 里（与 CLAUDE 记的「`out_il2csharp` 逐方法看，有的比 ILSpy 干净」一致）。
//
//  ⇒ 我们已有的 `OfferContainer.Carousel.IndexAt(slot, offset, n)` 就是这个环的**离散式**：
//     第 `slot` 个可见槽显示 `rewards[WrapOffset(offset + slot, n)]`。本类把「显示那一层」补上：
//     它记着当前 `Offset`，`Drag(steps)` 推一格（正负都归一到周期），`SlotItem(slot)` 给出该槽**此刻**该显示的奖励。
//
// ============================ 🔶 自己设计（非原版形态）============================
// // 🔶 自己设计（非原版）：把连续像素滚动离散成「一格 = 一个 offset」 · 原版是第三方库按像素移动子件、越界才
// // `SetAsFirst/LastSibling`（没有整数 slot/offset 概念），逐像素搬到无帧循环的自检里既慢又量不出 ·
// // 调度台 2026-10-31（B106_W24）裁定；离散式与它在「拖一个槽距」的读数上等价（拖一格 = 一件换到另一头 = offset ±1 取模）。
//
// ============================ 接线状态 ============================
//  🔴 **今天没接生产**：我们每份报价只有 1 条奖励（`ShopData.Grants` 全长度 1）⇒ 轮播路径不可达
//     （判据 = `Shell/OfferContainer.cs` 的 `Carousel` 段头）。接线口就是 `Drag` / `SlotItem`：
//     真接时由报价弹窗在 `base.Initialize`（`DrawRewards`）之后 `new ShopCarouselView(items, visible)`，
//     拖拽输入调 `Drag(±1)`、每槽 `ItemDrawer` 用 `SlotItem(slot)` 取奖励。
using System.Collections.Generic;

namespace CardPresentation
{
    /// <summary>商店轮播的**显示层**：一条首尾相接的奖励环 + 当前偏移。
    /// <para>语义（判据见文件头）：第 <c>slot</c> 个可见槽显示 <see cref="SlotItem"/>；
    /// <see cref="Drag"/> 是「拖一格」—— 与 `UI_InfiniteScroll.OnScroll` 把越过边界的那件搬到另一头等价。</para>
    /// <para>⚠️ **过滤口走 `OfferContainer.Carousel.Filter`**（原版 `DrawRewards` 那个 `Where`，判据不可及 ⇒ 可注入）：
    /// `SetItems` 每次都现取 <see cref="OfferContainer.Carousel.Apply"/> ⇒ 换 `Filter` 后重建即可生效。</para></summary>
    public sealed class ShopCarouselView
    {
        readonly List<CampaignData.RewardSpec> _items = new List<CampaignData.RewardSpec>();

        /// <summary>一屏几槽（原版 `UI_InfiniteScroll.ManualInit()` 从布局量的那一格；我们由调用方给）。</summary>
        public int Visible { get; private set; }
        /// <summary>当前偏移（0..Count−1）。第 <c>slot</c> 槽 = <c>Items[WrapOffset(Offset + slot, Count)]</c>。</summary>
        public int Offset { get; private set; }
        /// <summary>过滤之后的条数（= 环长度）。</summary>
        public int Count { get { return _items.Count; } }
        /// <summary>过滤之后的奖励表（只读视图；⚠️ 别就地改，重建走 <see cref="SetItems"/>）。</summary>
        public IReadOnlyList<CampaignData.RewardSpec> Items { get { return _items; } }

        public ShopCarouselView() { }

        /// <summary>建好（过滤一遍）+ `ManualInit(visible)`。</summary>
        public ShopCarouselView(IEnumerable<CampaignData.RewardSpec> rewards, int visible = 0)
        {
            SetItems(rewards);
            ManualInit(visible);
        }

        /// <summary>= 原版 `DrawRewards` 里那两拍 `Where` 之后的奖励集合（**现取** `Carousel.Filter`）。
        /// <para>⚠️ 偏移在条数变化后归一到新周期（<see cref="OfferContainer.Carousel.WrapOffset"/>）—— 不这么做，
        /// 过滤掉几条之后 `Offset` 可能越界、`IndexAt` 会给出「看起来正常但差一格」的读数。</para></summary>
        public void SetItems(IEnumerable<CampaignData.RewardSpec> rewards)
        {
            _items.Clear();
            List<CampaignData.RewardSpec> src = rewards == null
                ? null : new List<CampaignData.RewardSpec>(rewards);
            var filtered = OfferContainer.Carousel.Apply(src);
            if (filtered != null) _items.AddRange(filtered);
            Offset = _items.Count > 0 ? OfferContainer.Carousel.WrapOffset(Offset, _items.Count) : 0;
        }

        /// <summary>= 原版 `scroll.ManualInit()` 的读数那一半（一屏 N 槽）。</summary>
        public void ManualInit(int visible) { Visible = visible > 0 ? visible : 0; }

        /// <summary>一共几页（= 看完 n 条要滚几次；`visible <= 0` ⇒ 0）。</summary>
        public int Pages { get { return OfferContainer.Carousel.Pages(Count, Visible); } }

        /// <summary>第 <paramref name="slot"/> 槽当前显示第几条（`-1` = 没有槽 / 环为空）。</summary>
        public int SlotIndex(int slot) { return OfferContainer.Carousel.IndexAt(slot, Offset, Count); }

        /// <summary>第 <paramref name="slot"/> 槽当前该显示的奖励（没有 ⇒ <c>default</c>，`Id == null`）。</summary>
        public CampaignData.RewardSpec SlotItem(int slot)
        {
            int i = SlotIndex(slot);
            return i >= 0 && i < _items.Count ? _items[i] : default(CampaignData.RewardSpec);
        }

        /// <summary>整屏（`Visible` 个槽）当前显示的奖励，按槽序。</summary>
        public List<CampaignData.RewardSpec> VisibleItems()
        {
            var list = new List<CampaignData.RewardSpec>();
            for (int s = 0; s < Visible; s++)
            {
                int i = SlotIndex(s);
                if (i >= 0 && i < _items.Count) list.Add(_items[i]);
            }
            return list;
        }

        /// <summary>「拖一格」：把偏移推进 <paramref name="steps"/> 格（负 = 往另一头），归一到周期。
        /// 返回新的 <see cref="Offset"/>。<para>空环 ⇒ 0（⛔ 不除零）。</para></summary>
        public int Drag(int steps)
        {
            Offset = OfferContainer.Carousel.WrapOffset(Offset + steps, Count);
            return Offset;
        }
    }
}
