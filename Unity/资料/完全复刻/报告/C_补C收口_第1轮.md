# C · 补 C 收口（第 2 轮）—— 可做的都做完了，剩下的逐条写了「卡在哪」

> 2026-10-25 · 执行代理 C。本轮把**新建能力顺带解开的 C** 全部核销，并把剩下的每一条**按「卡在谁/卡在哪」分类**。
> 本轮**无代码改动**（只改账）；`typecheck` 上一轮末为 **0/0**（本轮没碰代码）。

---

## ① 本轮核销（+5 A / −5 C）

| 类 · 方法 | 为什么现在能核销 | 落点 |
|---|---|---|
| `ToggleCollectionFilter.FillToggleList` | **chip 行刚落地**（只对已选项建、同一父节点、同一网格下一格位） | `Core/FilterPanelModel.cs` `AppendChips` |
| `CollectionFilterController.NotifyFilterChange` | chip **不单独维护**，随格子重建一并重建（等价原版「先销毁再重建」） | 同上 |
| `CardInventory.Create` / `.Add` / `.UpdateItemCollection` | 背包框架落地后直接对上（`Add` 的 `customData` 形参我们暂无调用点 ⇒ **未建，如实标**） | `Shell/Inventory.cs` + `Inventories.Cards` |

⇒ `收藏.tsv`：A **136 → 141** · C **68 → 63**。

---

## ② 剩下的 C —— **逐条「卡在哪」**（⭐ = 玩家看得见）

### 收藏（63）
| 条数 | 类 | 卡在哪 |
|---:|---|---|
| **7 + 2** | `CollectionItem`（拖拽/排序模式 7）· `CollectionDisplay`（`ReorderCollectionByHierarchy` 等 2） | ⛔ **卡在两处别人的文件**：`RuleEngine/Data/DeckLibrary.UpdateOrder`（代理 A）+ `Shell/PointerLayer.cs` 的「项拖拽」与 `MenuDraw.DeckCell` 的 0.8 缩放（共用件 H）。**规格早已给出**（`报告/C_收藏_第1轮.md` §③ 请求 1） |
| **24** | `InventoryManager` | 🟡 **只差便利口**（`GetOwnedCount/HasItem/HasEnough/Remove/DeserializeInventory`）。**今天没有消费点** ⇒ 建了就是死代码 ⇒ ⛔ 不建（等某个调用方真要用时补那一个） |
| **18** | `UiBadgeNotification`(11) · `UiBadgeNotificationManager`(6) · `BadgeNotificationDefinition`(1) | **域外 = 菜单外壳线（H）**：红点/计数/补间/通知源那一族，本域只记账 |
| **6** | `ArmyItemContainer` | **原版有、我们无**（语义散在 `FilterPanelModel.Cell`；`ToggleGroup`/`SetManualToggle` 没有对应实现）—— E 已定为「类不存在 ⇒ C」 |
| **3** | `CardInventory.UpdateCardTier/CreateAlternateArtInfo`(2) · `AlternateArtInventory`(2) · `WildcardInventory`(1) | **数据面缺**：卡档（tier）与异画/万能卡的**库存条目**我们没有（全解锁 ⇒ 没有「解锁记录」这一层） |
| **1** | `WildcardDisplay.OnEnable`（徽记「闪一下」） | **缺 prefab 参数**：`shinyEffect`（`UIEffectTweener`）那一颗的字段值没读 ⇒ 照猜做就是「我们发明一个闪光」 |

### 卡组编辑（18）
| 条数 | 类 | 卡在哪 |
|---:|---|---|
| **5** | `DeckEnergyCostDrawer`(4) + `DeckCostQuantityRowDrawer`(1) | ⛔ **E 的文件**（`Core/CostCurveDrawer.cs`）—— 请求早已转出（固定 ÷10 归一 + 末行 `8+` + 补间） |
| **9** | `CollectionDeck`(2) · `DeckCollectionDisplay`(2) · `DeckInventory`(1) · `DeckDrawer`(4) | 前 5 = 拖拽排序（同上，A + H）· 后 4 = **原版有、我们无**（`DeckDrawer` 两个子类） |
| **2** | `DeckUtility.ToShortLocalizedString` / `ToShortRawLocalizationString` | ⛔ **卡在别人**：要 `RuleEngine`（A）加 `DescribeShort` + `Shell/SkirmishEventWindow.cs`（H）改调用点 —— 代码已在报告里给全 |
| **1** | `DeckInfoPopup.DuplicateDeck`（后缀 `+`） | ⛔ **A 的 `DeckLibrary`**（请求早已转出） |
| **1** | `DeckAndCardbackDrawer` | **原版有、我们无** |

### 商店（52）
| 条数 | 类 | 卡在哪 |
|---:|---|---|
| **36** | `Gacha*`(26) · `CampaignPremiumPointsPanel`(5) · `ExpansionPassPurchasePremiumPanel`(5) | **活动线暂停**（用户拍板）⇒ 只记账不动工 |
| **4** | `CarouselOfferPopupDrawer` | **布局数学已落**（`OfferContainer.Carousel`）；**UI + 输入**要过 `Shell/PointerLayer.cs`（H）+ 本店每份报价只有 1 条奖励 ⇒ **零可见收益** ⇒ 调度台已裁「先别做」 |
| **5** | `ContainerOfferEvent`(2) · `ShopContainer`(2) · `ShopContainerV2`(1) | **LiveOps 容器数据层**（事件数据 / 每活动 `TSave`）⇒ 缺 G §三 那层 |
| **7** | `CurrencyExtensions`(4) · `ShopContainerInventory`(3) | **背包层的商店侧**（货币库存 / 容器库存）：`Inventories` 只有 `Shop` / `Cards` 两份，货币与容器没有入袋 |

---

## ③ 结论（给调度台）
**C 已经到「我单方面推不动」的地板**：63 / 18 / 52 里，除「活动线 36」与「类不存在 / 数据面缺」两类外，
**每一条都指向一个明确的别人或一条明确缺的判据**（A 的 `DeckLibrary`/`DeckRules` · E 的 `CostCurveDrawer` · H 的 `PointerLayer`/`MenuDraw`/红点 · 缺 `shinyEffect` 的 prefab 字段）。
⇒ 若要继续推，**需要你分配**：① A 落 `UpdateOrder` + `DescribeShort` ② H 落项拖拽 + `DeckCell` 0.8 ③ 谁能读 `shinyEffect` 那一颗的 prefab 字段。

---

## ④ 没查清
1. **`shinyEffect`（`UIEffectTweener`）的字段值** —— 没读（要定位 `WildcardDisplay` MB 的 `shinyEffect` PPtr → 组件 JSON 逐字段）。
2. **货币 / 容器要不要入 `Inventories`** —— 取决于「商店买到的货币要不要进 HUD 钱包」那条口径（`ShopData.Buy` 的 doc 里记着「要不要接 `Wallet.Grant` 留给调度台裁」，至今没裁）。
