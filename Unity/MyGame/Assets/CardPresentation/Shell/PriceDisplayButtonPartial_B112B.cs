// PriceDisplayButtonPartial_B112B.cs —— 执行代理 `B112_B`（2026-11-01）· 账 `B-81`
//
// ============================ 这条活是什么 ============================
//  原版 `PriceDisplayButton`（`cpp2il/out_cpp2il_dev_ilspy/PriceDisplayButton.cs` + `out_il2csharp/Assembly-CSharp/
//  PriceDisplayButton.cs:1-150`，**两个源逐句一致**）是**挂在 prefab 上的一颗组件**，三个序列化字段：
//    `blinkEffect` (+0x20，`BlinkGraphic`) · `button` (+0x28，`EverguildButton`) · `priceDisplay` (+0x30，`PriceDisplay`)
//  两个成员：
//  <code>
//  public Button.ButtonClickedEvent OnClick => this.button.m_OnClick;
//  public bool Interactable {
//      get => this.button.m_Interactable;
//      set { this.button.interactable = value;
//            if (blinkEffect) this.blinkEffect.enabled = value; }   // ← setter 的第二句
//  }
//  </code>
//  ⇒ 本仓原来**整颗没有**（`OfferContainer.Build` 只建节点 + 一颗字）⇒ 原版
//    `ShopOfferContainer.OnOfferTimerEnded` 里 `priceDisplayButton.Interactable = false` 那一跳**没有落点**
//    （`B111_S3` 报告 §五.4 / §六.3 记的**复刻缺口**，不是「原版没有」）。本文件补上这一颗的最小可交互面。
//
// ============================ 判据（逐个字段/成员都对过）============================
//  · `PriceDisplayButton.cs:23-45`（Interactable get/set 全文，ILSpy）· `:10-14`（三个字段）
//  · `decomp_full/PriceDisplayButton__{get_Interactable,set_Interactable,get_OnClick}.c`（三份都现读）
//  · **实例面**（**70 颗** = `bundle_menus_assets_all` 69 + `bundle_scenes_scenes_mainmenuwarpforge` 1，
//    按 `m_Script = -4974499282242864396` 反查）：
//      `blinkEffect` **70/70 = null** ⇒ setter 的那第二句**在原版一次都不生效**（我们照写、字段恒 null，行为逐位相同）；
//      `button.m_Interactable` **68/70 = 1 · 2/70 = 0**。
//  · 🔴 **`PriceDisplayButton.OnClick` 在 `ShopOfferContainer` 这条路里没人监听**（判据三条）：
//      ① 70/70 实例的 `button.m_OnClick.m_PersistentCalls.m_Calls` = **空表**（逐份现读）；
//      ② 全量反编译里 `PriceDisplayButton__get_OnClick` 的调用方只有 9 个**别的**宿主
//         （`AlternateArtPanel` / `CreateAllianceMenu` / `DailyRewardTrackPanel` / `DraftModeSelectPacksStateDemo` /
//          `ExpansionPassRewardItemContainer` / `GachaCratePanel` / `MissionReRollPopup` /
//          `RaidEventRewardItemContainer` / `RaidProgressClaimButtonController`）—— **`ShopOfferContainer` 不在里面**；
//      ③ `ShopOfferContainer__OnClick.c` / `__OnPurchase.c` 都不碰它。
//      ⇒ **原版这一颗点了什么都不做**（但它照样吃射线，所以整卡那颗 `raycast target` 也接不到）——本文件照抄。
//
// ============================ 与原版的**结构**差异（如实标，不是行为差异）============================
//  ① 原版是 `MonoBehaviour`（挂在 prefab 的 `Price Display Button` 节点上）；我们是**纯 C# 类**。
//     理由 = 本仓的 UI 是**代码建树**（不用 prefab），而 `AddComponent<T>()` 要求「类名 == 文件名」的脚本资产，
//     本文件名必须叫 `*Partial_B112B.cs`（白名单）⇒ 挂不上去。同类先例 = `Shell/BaseEventProgressClaimButtonController.cs`
//     文件头差异 ①（那边也是「原版 MonoBehaviour → 我们普通类」）。
//     ⚠️ **这不构成行为差异**：`Interactable` / `OnClick` 两个成员的语义逐字照抄，只是宿主形式不同。
//  ② `OnClick` 只给 getter（原版也只有 getter，且它返回的那个 `ButtonClickedEvent` 我们没有等价类型 ——
//     `WindowButton.onClick` 是 `System.Action`）。要挂监听就 `pb.button.onClick = …`。
//  ③ `Setup(...)` 四个重载**没移植**：它们的输入全是服务端报价（`IShopOffer.GetOwnedQuantity()` /
//     `Element.price` / `Store.CanPurchase` / `GetPurchaseCount`）⇒ 属「服务器相关」门类（铁律 18·0），
//     本机没有这些数据源。**没做，不是「原版没有」**。

using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `PriceDisplayButton`（结构差异见文件头 ① —— 我们是纯 C# 类，不是 MonoBehaviour）。
    /// <para>本类**只管两件事**：把 `Interactable` 转给那颗真按钮、把 `OnClick` 读出来。
    /// ⛔ 别在这里再写「什么时候该置灰」—— 那是 `ShopOfferContainer.OnOfferTimerEnded` 的规则，
    /// 落点是 `Shell/OfferContainerPartial_B111S3.cs` 的 `OnOfferTimerEnded`（唯一实现）。</para></summary>
    public class PriceDisplayButton
    {
        /// <summary>原版 `blinkEffect`（+0x20）。**70/70 实例是 null** ⇒ setter 那一句在原版从不生效。
        /// 本仓的等价组件 = <see cref="BlinkGraphic"/>（`Shell/BlinkGraphic.cs`）。</summary>
        public BlinkGraphic blinkEffect;

        /// <summary>原版 `button`（+0x28）—— `EverguildButton` 的等价物 = <see cref="WindowButton"/>
        /// （它逐条实现了 `Selectable.interactable` 的三件事：变灰 / 挡点击 / 挡悬停，见它自己的 doc）。</summary>
        public WindowButton button;

        /// <summary>原版 `priceDisplay`（+0x30）—— `PriceDisplay` 组件所在的节点（本仓 = `Price Display` 那一颗）。
        /// ⚠️ `PriceDisplay` 组件本身**没移植**（它只做 `Setup` 的排版，输入同样是远端报价）—— 留给将来的活。</summary>
        public Transform priceDisplay;

        /// <summary>原版 `OnClick => this.button.m_OnClick`（`decomp_full/PriceDisplayButton__get_OnClick.c`）。
        /// ⚠️ 只给 getter（原版也只有 getter）；要挂监听走 <see cref="button"/>。</summary>
        public System.Action OnClick { get { return button != null ? button.onClick : null; } }

        /// <summary>原版 `Interactable`（`decomp_full/PriceDisplayButton__{get,set}_Interactable.c`，逐句）：
        /// <code>
        /// get => this.button.m_Interactable;
        /// set { this.button.interactable = value;
        ///       if (blinkEffect) this.blinkEffect.enabled = value; }
        /// </code>
        /// <para>🔴 **setter 是两句，不是一句** —— 第二句（闪烁件一起关）在本族**恒不生效**（70/70 `blinkEffect = null`），
        /// 但**照抄**：将来谁把闪烁件接上，这一句就该开始生效（⛔ 别按「反正 null」删掉）。</para>
        /// <para>⚠️ `button == null`（命中区没建起来时）⇒ getter 返回 `false`、setter 只出声不崩 —— 原版这里 NRE，
        /// 本仓口径同 `LiveOpsManager.IsTriggeredEvent`（批处理里不抛、改成出声）。</para></summary>
        public bool Interactable
        {
            get { return button != null && button.Interactable; }
            set
            {
                if (button == null)
                {
                    Debug.LogWarning("[PriceDisplayButton] 没有 `button`（命中区没建起来）⇒ `Interactable = " + value
                                     + "` 这一跳**没有落点**（原版这里 NRE；本仓出声、不崩）");
                    return;
                }
                button.Interactable = value;
                if (blinkEffect != null) blinkEffect.enabled = value;   // 原版 setter 的第二句（本族恒 null，照抄）
            }
        }
    }
}
