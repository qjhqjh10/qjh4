# C · 收藏 / 卡组编辑 / 商店 —— B 升 A 第 1 轮

> 2026-10-25 · 执行代理 C。范围 = 我名下三本账里的**全部 166 条 B**（`收藏.tsv` 69 · `卡组编辑.tsv` 62 · `商店.tsv` 35）。
> 做法按调度台口径：逐条核**原版方法体** ↔ **我们的实现** → 一致改 `A`（落点精确到文件/符号）、不一致改 `C`、核不动**保持 B 并写清「差什么」**。
> ⛔ 本轮**没有代码改动**（只改账 + 写本报告）；⛔ 未跑 Unity / 未动 git / 未跑 `gen_replica_ledger.py`。

---

## ① 数字

| 账 | 升 A | 降 C | 降 D | 仍 B | 变化后状态分布 |
|---|---:|---:|---:|---:|---|
| `收藏.tsv` | **9** | 0 | **1** | 59 | A113 / B59 / C80 / D99 |
| `卡组编辑.tsv` | **1** | **2** | 0 | 59 | A176 / B59 / C14 / D74 |
| `商店.tsv` | **15** | **9** | **2** | 9 | A114 / B9 / C54 / D165 |
| **合计** | **25** | **11** | **3** | **127** | 三本 B 从 166 → **127** |

三本账仍是 **11 列 / E = 0 / 行尾 LF 未翻**（改完逐本验过：`351·323·342` 行，列数全 11）。

---

## ② 升 A 的 25 条（逐条判据）

### `收藏.tsv`（9）
| 类 · 方法 | 判据 | 我们的落点 |
|---|---|---|
| `WhereToGetCardsSection` × **6**（Awake / Initialize / OnDestroy / Release / Reset / SetVisibility） | 原版 `Awake` 绑 toggle → `SetVisibility`；`Initialize` 建抽屉 + 布局回调；`Reset/Release` 释放 | `Shell/WhereToGetCardsPopup.cs`（折叠箭头 `QToggle` / `ArtToggleOn·Off` / `ToggleX1..X2`；`:54` 折叠说明 + `:160-166` 两态图/色） |
| `CollectionDisplay.GetItems` / `GetItemObject` / `Select(item,button)` | 原版 `listView.Cells` / 按 item 反查 / 点格 → `OnItemSelected` → 开详情 | `Shell/CollectionWindow.cs` `CardsCells` / `CardsCellDef(i)` / `AddHit(… → OpenCardDetail)` |

### `卡组编辑.tsv`（1）
| 类 · 方法 | 判据 | 落点 |
|---|---|---|
| `ImportDeckPopup.TryImportDeck` | **K3 留的那个「第三参是什么门」查清了**：`decomp_full/DeckUtility__ValidateDeck.c:16` = `if (param_3 == 0 || …)` 才跳过**拥有校验** ⇒ 第三参 = `validateOwnership`；我们全解锁 ⇒ 等价 | `Shell/ImportDeckPopup.cs` |

### `商店.tsv`（15）
| 类 · 方法 | 判据 | 落点 |
|---|---|---|
| `PriceDisplay` × 3（FixLayout / Setup(CurrencyValue) / Setup(Price)） | `Setup(CurrencyValue)` 的 ilspy 反编译失败 ⇒ 用 `decomp_full/PriceDisplay__Setup.c` 补读：取货币资产（type 40 无图标）→ 文本=数量、图标=货币图标；`Setup(Price)` 只是 Price→CurrencyValue 转换；`FixLayout` = 图+文本重排（我们固定矩形不需要） | `Shell/ShopTabPage.cs` 价格区（金币图标 + 价格文本） |
| `ContainerOfferEvent.OpenOfferContainer` / `TryPurchaseOffer` | 开容器 → `onComplete(rewards)`；购买后按 `showRewardPopup` 决定弹不弹领奖窗 | `Shell/BoosterPackOpenWindow.cs::Create` + `Shell/ShopWindow.DoBuy` + `Shell/BaseOfferPopup.ProcessPurchase`（`:1050`） |
| `ShopContainer.OnAfterPurchase` / `Open` · `ShopContainerV2.OnAfterPurchase` / `Open` | 同上（V2 只多一档动画开关） | 同上 |
| `InAppOfferContainer` × 3（OnInitialize / Refresh / OnClick）· `PurchaseableOfferContainer` × 3 | `OnInitialize` 填前景/背景图 + 价格钮 + 标题；`Refresh` = `priceDisplayButton.Setup(offer)`；`OnClick` = 按 offer 取 Popup 开窗 / 购买 | `Shell/OfferContainer.cs` + `BaseOfferPopup.Show(prefab, content)` + `ShopWindow.DoBuy` |

---

## ③ 降 C 的 11 条（判据 + 为什么不是 A）

| 类 · 方法 | 差什么 | 判据 |
|---|---|---|
| `DeckUtility.ToShortLocalizedString` · `ToShortRawLocalizationString` | **原版有长/短两档文案**（`MenuDeck/Error/{枚举}` 与 `…/{枚举}/Short`），我们 `DeckRules.Describe` **只有一档** | `DeckUtility.cs:86-148`（短档键 `$"MenuDeck/Error/{arg}/Short"`） |
| `ContainerOfferEvent.GetBaseData` / `GetBaseSave` | LiveOps 容器的**事件数据**与**每活动存档**（`TSave`）我们整层没有 | `ContainerOfferEvent.cs:267,300` |
| `ShopContainer.GetID` / `GetCustomData` · `ShopContainerV2.GetID` | LiveOps 容器数据层的 id / 自定义数据 | `ShopContainer.cs:77,82` · `ShopContainerV2.cs:93` |
| `CarouselOfferPopupDrawer` × 4（Initialize / DrawRewards / DrawItemReward / DoReset） | **「轮播式」奖励抽屉**我们没做 —— 我们的奖励区是**静态一行格** | `CarouselOfferPopupDrawer.cs:109-330` |

> ⚠️ 这 11 条都**不是**「影响小所以不做」，已经按 C 记进账（`等级 = P2`），等对应线恢复时做。

## ④ 降 D 的 3 条（机制不同，不是缺失）

| 类 · 方法 | 为什么 D |
|---|---|
| `InAppOfferContainer.OnDestroy` · `PurchaseableOfferContainer.OnDestroy` | 方法体只有 `LiveOpsAssetUtility.Unload(ForegroundImage / BackgroundImage)` —— **释放 Addressables 引用**。我们没有 Addressables（全部预导入 `Resources/`），**没有需要手动 Unload 的东西** ⇒ 结构上不存在 |
| `CollectionDisplay.Select(T item)` | 「程序化选中某一项」的入口；本窗 `useSelectedHighlight = 0`、也没有程序化选中调用点 ⇒ **结构上不需要** |

---

## ⑤ 仍 B 的 127 条 —— **差什么、怎么才能核**（⛔ 没有为了好看改成 A）

| 组 | 条数 | 差什么 / 怎么才能核 |
|---|---:|---|
| **共用件不在我名下**：`ItemDrawer`(36) · `CollectionFilterController`(6) · `ToggleCollectionFilter`(8) · `ArmyItemContainer`(6) · `DeckDrawer`+`DeckAndCardbackDrawer`(5) · `CurrencyDrawer`/`PremiumDrawer`/`PremiumIconDrawer`(5) · `PrebuiltDeck`+`PrebuiltDeckCollection`(13) · `DeckGeneralInfoDemo`+`DeckSelectorMenuItemDemo`+`DemoDeckInfoSO`(30) | **109** | 文件归 **H**（`Shell/ItemDrawer`/`PrebuiltDecks`/`PracticeModePopup`）与 **E**（`Core/FilterPanelModel`）。我**读过原版**、但**没有逐方法核我们的实现**（核它需要读 1400~2200 行的共用件）。⇒ **建议把这几族派给它们的 owner 或单独一批「共用件 B 升 A」**；我这一轮只在账里把「差什么」写清楚 |
| **载体不同（我们换了模型）**：`CardDeck.GetID`/`Equals`/`GetHashCode`/`CosmeticsSetup`(4) · `CurrencyValue`(3) · `Price.ToCurrencyAmount`(1) | **8** | 原版有 `deckId` / 多货币 `CurrencyValue`；我们**没有 deckId**（用 Name，A 侧已记风险）、**只有一种资源**（用户拍板不做真实经济）。⇒ **核不成 A**；要么保持 B、要么由你给一条「按 D 处理（用户拍板不做）」的口径 |
| **拖放 / 焦点 / 选中态语义**：`DeckEditingPanel.Drop`×2 · `ToFocus`×3（`CardCollectionTab`/`DeckCollectionTab`/`SelectDecksTab`）· `DeckInfoControls.OnSelect` · `DeckInventory.HasSkirmishDecks` · `DeckUtility.HasDefensiveCards` | **8** | 这些要么**需要真机**（拖放阈值、焦点转移），要么**需要逐值核**（防御卡三态枚举 vs 我们的两态）。⇒ 等自检/真机恢复 |
| **信号层**：`CardCollectionDisplay.OnCollectionUpdate` | **1** | 原版是 `Signal.Register` 驱动的逐格刷新；我们**没有信号总线**，靠按需重建。要判 A 得先有信号层（或确认「可观测结果恒同」需真机） |
| **单档置灰**：`AlternateArtCardCollectionTab.Setup` | **1** | 原版 `styles.Count == 1` ⇒ 两箭头 `interactable = false`；我们代码里有这段，但**没有逐值核**（要跑 `CollectionScene.Run` 看 `interactable`） |

---

## ⑥ 没查清 / 顺带

1. **`PriceDisplay` 那两条 A 的判据是「补读」来的**：`ilspy` 的 `PriceDisplay.Setup(CurrencyValue)` 报 `Stack state not settling`（反编译失败）⇒ 我用 `decomp_full/PriceDisplay__Setup.c` 补读。**如实记**：那一条的「type == 40 不带图标」是条件分支本身，但**为什么 40 这一档要跳过图标**没继续追（不影响 A 的行为判定）。
2. **`CarouselOfferPopupDrawer` 降 C 后要不要做**：它是「报价弹窗里奖励的轮播展示」。我们现在的奖励区是静态一行 —— **功能不缺、表现不同**。如果调度台认为这属于「表现层差异」，可以改判 B（并把「轮播动画」当表现细节）；我按铁律 11 先判 C。
3. **`DeckUtility` 的短档文案**：要补得先确认**哪一处用短档**（原版 `ToShort*` 的调用点我没逐个查）。⇒ 已记在账的 `备注` 里。
4. **本轮没有新增断言**：升 A 的 25 条里，24 条是**既有行为**（之前就已实现、这轮只是把判据补齐），**唯一的新代码路径**（价格三档）**没有数据能触发**（商品表里没有免费件/折扣原价 ⇒ 见 `报告/C_商店_第1轮.md` §④）。⇒ 不加「测不到」的断言；等有真折扣商品时再补版面断言。
5. **文件完整性**：这一轮改账用的是脚本写盘；前后各验了一次（行数 `351/323/342`、列数恒 11、`E = 0`、CRLF 计数 0）。中途有一次写到了仓根的临时文件（路径拼接错了），**已核对内容无误后覆盖回账目录并删掉临时文件** —— 账的最终状态就是 ① 表里那一组数。
