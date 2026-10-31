// OfferContainerPartial_B112B.cs —— 执行代理 `B112_B`（2026-11-01）· 账 `B-81`
//
// ============================ 这条活是什么 ============================
//  `OfferContainer.Build` 建的 `Price Display Button` 原来**只有节点 + 一颗字**（`BuildPrice`，`OfferContainer.cs:1601`），
//  没有 `PriceDisplayButton` 组件、也没有**命中区** ⇒ 原版 `ShopOfferContainer.OnOfferTimerEnded` 最后一句
//  `priceDisplayButton.Interactable = false`（`decomp_full/ShopOfferContainer__OnOfferTimerEnded.c` 全文两句）
//  在本仓**没有落点**。本文件把那颗「可交互价签」补上（组件本体 → `Shell/PriceDisplayButtonPartial_B112B.cs`）。
//
// ============================ 判据 ============================
//  · **几何/层级**：`background/name-bg/Price Display Button/Generic UI Button` —— 原版 `PriceDisplayButton` 的
//    `button` 字段指向那颗 `EverguildButton`（实例面 70/70 现读 `button` 非空、`m_OnClick` 空表），
//    而 `EverguildButton` 是 `Selectable` ⇒ **可射线件是 `Generic UI Button` 那一块矩形**。
//    我们 `BuildPrice` 把 `Price Display Button` 与 `Generic UI Button` **建成同一个矩形**（`g.Price`，`OfferContainer.cs:1604-1605`）
//    ⇒ 命中区矩形 = `g.Price`。
//  · **渲染队列必须高于整卡那颗命中区**：整卡 `raycast target` 建在 `QoHit`(=9)（`OfferContainer.cs:1561`），
//    `PointerLayer` 取**队列最高**的那一颗 ⇒ 价签若 ≤ 9 就**永远点不到**（本文件 `QoPriceHit = 11`）。
//    ⚠️ 这条是**结构性**的：不像「值对不对」那样两边一起改就能蒙混（`QoHit` 抬到 11 以上 ⇒ 断言红）。
//  · **点击行为 = 什么都不做**：判据见 `Shell/PriceDisplayButtonPartial_B112B.cs` 文件头（三条）。
//
// ============================ 我们**没有**照抄的部分（如实标）============================
//  · `PriceDisplayButton.Setup(IShopOffer)`（四个重载）没移植 —— 输入全是服务端报价（`GetOwnedQuantity` / `Element.price`
//    / `Store.CanPurchase` / `GetPurchaseCount`）⇒ 服务器相关门类（铁律 18·0），本机无数据源。
//  · `BuildPrice` 的排版一行没动（本文件**不重写**它，只在它建好的节点上补交互面）。

using System;
using UnityEngine;

namespace CardPresentation
{
    public static partial class OfferContainer
    {
        // ⚠️ 新成员追加在类尾（本文件是独立 partial ⇒ 对 `Shell/OfferContainer.cs` 的 19 个账落点**零影响**）。

        /// <summary>价签命中区的渲染队列偏移。**必须 > `QoHit`(=9)** —— 见文件头那条判据；
        /// 也 > `QoWebShopHit`(=10)（两块矩形本来不重叠，真叠上时让后建的价签吃，`PointerLayer` 取最大者）。</summary>
        public const int QoPriceHit = 11;

        /// <summary>`BuildPrice` 里那颗可见底图的名字（`MenuDraw.Rect(box, …, "Button Gfx", …)`，字面量在
        /// `OfferContainer.cs:1606`）。⚠️ 它是**独立字面量**（不是常量）⇒ 改名要两处一起改，否则价签会「有命中区但不换图/不变灰」。</summary>
        public const string NPriceGfx = "Button Gfx";

        /// <summary>在 `BuildPrice` 已经建好的 `Price Display Button/Generic UI Button` 上补一颗**可交互价签**：
        /// 命中区（= 原版 `EverguildButton` 的矩形 `g.Price`）+ 组件（= 原版 `PriceDisplayButton`）。
        /// <para>返回的实例由 `Build` 收下并**闭包进到期回调**（`ScheduleTimerEnd(deadline, pb)`）——
        /// 这样 19 颗容器各自持有自己的价签（⛔ 别用「最后建的那一颗」那种静态兜底）。</para>
        /// <para>找不到节点 / 命中区建不出来（整块落在视口外）⇒ **出声 + 返回一个 `button == null` 的实例**
        /// （红线：不许静默失败；`Interactable` 的 setter 遇到 null 也会出声）。</para></summary>
        public static PriceDisplayButton AttachPriceButton(Transform nb, Func<float, float, float, float, PxRect> R,
                                                          Geo g, int qBase)
        {
            var node = nb != null ? nb.Find(NPrice) : null;
            var box = node != null ? node.Find(NPriceBtn) : null;
            if (box == null)
            {
                Debug.LogWarning("[OfferContainer] `AttachPriceButton`：找不到 `" + NPrice + "/" + NPriceBtn
                                 + "` 节点（`BuildPrice` 没跑？）⇒ 这一颗**没有可交互价签**");
                return new PriceDisplayButton();
            }
            var pr = g.Price;
            var hit = MenuDraw.Hit(box, NHitQuad, R(pr.x1, pr.y1, pr.x2, pr.y2), qBase + QoPriceHit, PriceButtonClicked);
            var wb = hit != null ? hit.GetComponent<WindowButton>() : null;
            if (wb == null)
                Debug.LogWarning("[OfferContainer] `" + NPrice + "/" + NPriceBtn + "` 的命中区**没建起来**"
                                 + "（`MenuDraw.Hit` 返回 null = 整块落在视口外）⇒ 这一颗真鼠标点不到、也不会变灰");
            // 🔴 **必须 `Bind` 到那颗可见的 `Button Gfx`**：`WindowButton` 变灰/hover 换图只作用于
            //   「`target` + 本钮子树」。命中区是我们**另建**的一颗（子树里只有那张全透明的命中 quad），
            //   不 `Bind` 的话 `Interactable = false` **一颗像素都不会变**（= 静默失效，本工程踩过同族）。
            //   hover/pressed 由 `Bind` 按 `PriceArt` 自动推（`40K_button → 40K_button_hover / _pressed`，
            //   原版那颗 `EverguildButton` 的 `m_Transition = 2`(SpriteSwap) 走的就是这一对 —— 实读
            //   `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_3290949454593738876.json` 的 `m_SpriteState`）。
            var gfx = box.Find(NPriceGfx);
            if (wb != null) wb.Bind(gfx != null ? gfx.GetComponent<ImageQuad>() : null, PriceArt);
            if (wb != null && gfx == null)
                Debug.LogWarning("[OfferContainer] `" + NPriceBtn + "/" + NPriceGfx + "` 不在 ⇒ 价签没有可见底图可绑"
                                 + "（变灰 / hover 换图都不会生效）");
            return new PriceDisplayButton
            {
                blinkEffect = null,                                  // 原版 70/70 实例都是 null（判据见 PriceDisplayButton 文件头）
                button = wb,
                priceDisplay = box.Find(NPriceBox),
            };
        }

        /// <summary>价签被点 —— **原版什么都不做**（判据三条见 `Shell/PriceDisplayButtonPartial_B112B.cs` 文件头：
        /// 0 个持久化监听 + 全量反编译里 `ShopOfferContainer` 没有 `add_OnClick`）。
        /// <para>⚠️ 但仍然要**有**这颗命中区：原版它是可射线件，会**吃掉**落在价签上的点击（整卡那颗接不到）⇒ 照抄。</para>
        /// <para>出声而不是静默：本仓红线「点了必须有反应 / 不许静默失败」—— 但**不给假反应**（不假装买了）。</para></summary>
        public static void PriceButtonClicked()
        {
            Debug.Log("[OfferContainer] `Price Display Button` 被点了 —— 原版这一颗的 `OnClick` 在 prefab 里**0 个持久化监听**、"
                      + "代码里**没有任何地方 `add_OnClick`**（判据见 `Shell/PriceDisplayButtonPartial_B112B.cs` 文件头）"
                      + "⇒ **原版点了什么都不做**，我们照抄（⛔ 不假装购买；购买是远端 `ILiveOps.Purchase`）");
        }
    }
}
