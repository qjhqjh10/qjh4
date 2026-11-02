# B121-COLL-B · 收藏窗「卡组页」+ 卡组列表/重排 + 卡组规则校验（只读审计）

> 代理：`B121-COLL-B` · 2026-11-03 · 只读（未跑 Unity / 未动 git / 未改源码 · 未派子代理）。
> 范围 = 收藏窗 Deck 页（页头三钮 / 卡组列表格 / 点击与模式 / 拖拽重排 / Deck 筛选抽屉 / 活动过滤 / 模式子页 / 规则校验）。
> 不含：卡组编辑窗 · 卡组详情弹窗内部 · 导入弹窗内部 · 收藏卡池（别的代理在做）。

## ① 结论

**卡组页的「版面」几乎逐位复刻（几何/字号/字距/颜色/九宫/悬停/点击音全对），「交互与规则闸」有 4 处真缺口 + 6 处行为不符**：
最该修的三条 = **① Deck 页 `Clear filters` 钮永远不显示**（`RefreshClearFilter(0)` 读的是 `_deckFltSlide.State == null`）·
**② 点一格卡组就切「当前卡组」并落盘**（原版只在详情弹窗的 Select 钮里改）·
**③ 页头筛选 chip（`Header/Filters` 的 85×70 格、最多 5 个）整套没建**。

覆盖 **71 项**：✅符合原版 **60** · 🟠我们没实现 **4** · ⚠️实现了但参数/行为不符 **6** · ❓判据查不到 **1**。

## ② 主表

| # | 是什么 | 实现了 | 符合 | 原版参数（出处） | 我们参数（文件:行） | 差异 | 档位 |
|---|---|---|---|---|---|---|---|
| A1 | 页头 `Filter Toggle` 图+label | ✅ | ✅ | 50×50 @367.2,88.4；label `Filters` fs42 auto[10,42] base32 Left/Middle @437.2,W150（`menu_dump "Collection Menu Variant"`） | `CollectionWindow.cs:3513`+`:4053` | 无 | ✅ |
| A2 | `Control Buttons` 组 | ✅ | ✅ | 740×60，组矩形 1179.99,80.94→1920.01,140.94，**`m_ReverseArrangement=1`**（视觉序 = Unlock→Import→Create） | `:3354` `CtrlGrp*`；`:4082-4128` | 无（三个 x 逐位对） | ✅ |
| A3 | `Unlock`（Debug Unlock） | 建结构 | ✅ | GO `m_IsActive=1`，但 Image/Button Outline/Text **三个组件全 `m_Enabled=0`** ⇒ 一个像素都不画、占 186.01px 布局位 | `:4166 BuildUnlockPlaceholder`（不建图/不建字） | 无 | ✅ |
| A4 | `Unlock` 显隐时机 | ✅ | ✅ | `Setup()` 先无条件 `SetActive(false)`；`ToggleSelectDeck` 再 `SetActive(true)`（= 列表态开、模式页关） | `:4548 SetDeckModeChrome`：`SetActiveIf(_deckUnlock,!modePage)` | 无 | ✅ |
| A5 | `Create` 钮几何 | ✅ | ✅ | 245×60 @1661.01,80.9；字 `Create Deck` fs42 auto[10,42] | `:3354 CreateX`；`:4083,4099` | 无 | ✅ |
| A6 | `Create` 点击行为 | ✅ | ✅ | `CardDeck.GetEmptyDeck()` → `emptyDeck.gameMode = currentEvent` → `syncedToServer=false` → `OpenWindow<DeckEditingWindow>`（**不入库**）（`SelectDecksTab.cs` il2csharp `CreateDeck`） | `:5639 CreateDeck` → `CollectionData.CreateDeck(mode)` + `GoNewDeck()` | 无（我们也不入库、直接进编辑器） | ✅ |
| A7 | `Create` 的禁用条件 | — | ✅ | **原版无任何 interactable/SetActive 闸**（`Setup()` 只挂监听） | 无闸 | 无 | ✅ |
| A8 | `Import` 钮几何 | ✅ | ✅ | 245×60 @1391.01,80.9；字 `Import Deck` | `:3354 ImportX`；`:4084,4112` | 无 | ✅ |
| A9 | `Import` 点击行为 | ✅ | ✅ | `OpenWindow<ImportDeckPopup>()` | `:4134` → `OpenImportPopup()` `:5426` | 无 | ✅ |
| A10 | **`Import` 的 interactable 闸** | ❌ | — | `importDeck.interactable = Inventory.GetOwnedCount<CardDeck>() < GameStaticData.totalCustomDecks`（`SelectDecksTab.cs` il2csharp `Setup:`；`totalCustomDecks=114` `GameStaticData.cs:2419`） | **没有任何 interactable 赋值**（`grep interactable` 全文件只命中异画页箭钮 `:2457`） | **满 114 套时原版灰掉导入，我们仍可点** | 🟠 |
| A11 | 卡组数上限 114 | ❌ | — | 唯一两处闸：① `Import`（上条）② `duplicateButton.interactable = assets.Count < 114`（`DeckInfoControls.cs:199-207`，属弹窗范围） | 新建/复制均无上限检查 | 与 A10 同源；复制那一半见弹窗代理 | 🟠 |
| A12 | `Clear filters` 钮几何 | ✅ | ✅ | 250×60 @612.2,83.4；字 `Clear filters` fs42 auto[10,42]（Deck 页 Header/Filters 的序列化位） | `:3403 ClearFltX=612.2`；`:3600` | 无 | ✅ |
| A13 | **`Clear filters` 显隐** | ❌ | — | `clearFiltersButton.SetActive(toggleOptions.Count > 0)`（`CollectionFilterController.NotifyFilterChange` 末句，AUD05 §1·6） | `:3734 RefreshClearFilter(0)` 读 `PageDrawer(0).State`，而 `_deckFltSlide = new FilterPanel{…}` **没有 State**（`:4834`；其它三页 `:2768` 才有）⇒ **恒 false ⇒ Deck 页这颗钮永远不显示** | **筛选了却清不掉（只能再点一次阵营格/清空输入框）** | ⚠️ |
| A14 | 页头筛选 **chip** | ❌ | — | `filterIconHolder` = 页头 `Filters` 节点（GO pid `1419059127639990997` 等 5 份）；`Header/Filters` 是 **GridLayoutGroup cellSize=85×70 · spacing 5,0 · constraint=FixedRowCount(1)**，chip 最多 **5** 个 | 页头 `Filters` 只建了 `Clear Filter Button`（`LayoutElement.IgnoreLayout=1`），**没有任何 chip 节点** | 选中筛选后原版在页头挂图标 chip，我们没有 | 🟠 |
| B1 | Deck 视口 | ✅ | ✅ | 1589.29×924（`Deck Scroll View` sizeDelta(−163.707,−85)） | `:943 DeckViewport`(330.9,155.9→1920,1080)=1589.1×924.1 | 宽 −0.19px（不翻列数） | ✅ |
| B2 | 格尺寸 / 间距 | ✅ | ✅ | `_cellWidth 225` `_cellHeight 364.5` `_spacingX 20` `_spacingY 0` `_mobileSizeScale 1.25`（RSR MB `8316412282010198741`） | `:960 DeckCellW/H`、`:963 DeckSpacingX=20`、`:969 DeckSpacingY=0` | 无 | ✅ |
| B3 | 列数 | ✅ | ✅ | `floor(1589.29/(225+20))=6`；小屏 `floor(1589.29/(281.25+20))=5` | `:974 DeckCols` 同一式 ⇒ 6 / 5 | 无 | ✅ |
| B4 | 居中 PadX | ✅ | ✅ | 桌面 69.645 / 小屏 51.52（同式 `((W−(sp+cellW)·cols)+sp)/2`） | `:984 DeckGridOffsetX` ⇒ 69.55 / 51.52 | 桌面 −0.095px（视口宽取 1589.1） | ✅ |
| B5 | 池格数 114 | 实现不同 | ✅ | `_poolSize=114`，池 = `max(min(20,需求),114)`；**只对 < `count` 的格调 `SetCell`** | 只建「视口内 × 现有卡组数」的格（`:5019-5027`） | 无复用池；可观测等价（空池格不配置任何内容） | ✅ |
| B6 | 纵向滚动 | ✅ | ✅ | `m_MovementType=1`(Elastic) · `scrollSensitivity=100` | `:4211 MenuScroll.TopAligned(…,100f)`、`:4212 Elastic=true` | 无 | ✅ |
| B7 | contentH | ✅ | ✅ | `rows·cellH − spacingY`（spacingY=0 ⇒ rows·364.5） | `:4210`、`:4949` 同式 | 无 | ✅ |
| C1 | 格主图 = **卡面插图** | ✅ | ✅ | `deckCardback.sprite = item.deckHero.cardImageReference.Load(General)`（`CollectionDeck.cs` il2csharp `Config`） | `MenuDraw.cs:3417 CardArt.Portrait(info.WarlordId)` | 无（2026-10-18 已改判；方形 `UI_Deck_Warlord_*` 只给详情窗） | ✅ |
| C2 | 主图遮罩/画框 | ✅ | ✅ | 遮罩 `Deck Image(mask)` 228×305.97 @11,26.1；`Deck Image` 347.7×346.35 @−49.4,6.2 | `MenuDraw.cs:3126 DcBack*`、`:3138 DcImg*`；`:3414-3440` 两档画框 | 无 | ✅ |
| C3 | `Frame` 40K_bt_deck | ✅ | ✅ | 245.99×368.01 @2,17 | `:3119 DcFrame*` 246×368 @2,17 | 无 | ✅ |
| C4 | `Deck Name` | ✅ | ✅ | 210×39.69 @20,344.2 · fs32 base30 auto[8,32] · Center/**Capline** · wrap0 · 白 · 字距 **−2** | `:3149 DcName*`；`:3455-3469`（auto + `SetCharSpacing(-2)` + `SetVAlign(Capline)`） | 无 | ✅ |
| C5 | `Faction Icon` | ✅ | ✅ | 84.5×85.75 @−10.5,273.7 · `preserveAspect` | `:3158 DcFac*`(84.5×85.7) · `:3540` | 高 −0.05px | ✅ |
| C6 | `Game Mode Icon` | ✅ | ✅ | 84.5×85.75 @170.5,273.7 · `enabled = (sprite != null)` | `:3163 DcMode*` · `:3548`；`:5308 gameMode: info.GameMode` | 无 | ✅ |
| C7 | `DificultyLevel`（难度标） | 不画 | ✅ | 84.93×84.01 @159.2,15.7，prefab `INACT`；`DeckCollectionDisplay.displayDifficultyLabel = 0`（本窗实例）⇒ **恒不显示** | `:3161` 常量在、`showDifficulty` 缺省 false；`:5017` 不传 | 无 | ✅ |
| C8 | `Highlight`（选中金环） | 不画 | ✅ | `useSelectedHighlight = 0`（本窗那一份 CollectionDeck MB `-594148595981120468`）⇒ `SelectedHighlight` 从不调用 | `:5300` 恒传 `selected=false` | 无 | ✅ |
| C9 | 悬停 | ✅ | ✅ | `HoverHighlight` ⇒ 根 `localScale` 1.0↔**1.05**（直接 set、无补间；`sortMode` 时跳过） | `MenuDraw.cs:3109 DeckCellHoverK` · `:3589-3590` | 无 | ✅ |
| C10 | 点击音 | ✅ | ✅ | `CollectionDeck.onClickSound` pid `605815307224280273` ⇒ cue `DeckClick 1`（两份 prefab 变体同值，MB `-594148595981120468`/`7915729459580635722`） | `MenuDraw.cs:3212 DeckCellClickCue="DeckClick 1"` | 无 | ✅ |
| C11 | 「张数」 | 不画 | ✅ | **原版卡组格没有张数字段/节点**（`CollectionDeck` 字段表 + prefab 树只有 Content/Deck/Ban Icon/Create） | `CollectionData.DeckInfo.Count` 有数据但 `DeckCell` 不画 | **我的任务书写了「张数」，原版没有这一项** | ✅ |
| C12 | `Ban Icon` + `Banned Text` | ❌ | — | `bannedImage.SetActive(Any(GetLibraryInFull(), c => deckGameMode.IsCardBanned(c)))`；`deckName.color = (1,0.3,0.3,1)`；灰度 0.25（`CollectionDeck.Config`） | 未建（`MenuDraw.DeckCell` 无 ban 层；`IsCardBanned` 在 `CollectionWindow.cs:6684`，只服务**卡池格**） | 本地 `DeckRuntime.CurrentEventData == null` ⇒ 原版这一档**不可达**；接上 LiveOps 后必须补 | 🟠 |
| C13 | `Create` 空位格 | 不画 | ✅ | prefab 里 `Create`(createObject) 在 **INACT**；唯一写点是 `Config(null)` ⇒ `createObject.SetActive(true)`；只有 `filteredCollection[index]` 取到的格才 `Config` | 不建 | 用户实拍 `收藏—卡组的参考图.png` 4 格**没有 Create 格** ⇒ 该支路不可达 | ✅ |
| D1 | 点格 = 开 `DeckInfoPopup(Edit)` | ✅ | ✅ | `DeckCollectionTab.OnItemSelected`：`new DeckInfoContext(deck, Edit, …)` → `OpenWindow<DeckInfoPopup>` | `:5342 SelectDeck` → `:5447 OpenDeckInfo` | 无 | ✅ |
| D2 | **点格是否改「当前卡组」** | 多做 | ❌ | `CollectionDisplay.Select` 只写 `SelectedItem` + 调 `OnItemSelected`，**不落任何状态**（`CollectionDisplay_object___Select__15ed9c0.c`）；改当前卡组只在弹窗 `DeckInfoControls.OnSelect` | `:5346 CollectionData.Select(i)` + 失败回滚 + `:5359 OpenDeckInfo` | **我们点一下格就切当前卡组并写盘**（原版必须进弹窗按 Select） | ⚠️ |
| D3 | 排序模式 `sortDeckMode` | 不可达 | ✅ | `DeckCollectionTab.sortDeckMode` 是 **`readonly bool`**、ctor 只调 base ⇒ **恒 false**；`SetSortMode(true)` 那一支**永远走不到**（`DeckCollectionTab__.ctor.c` + il2csharp `:84`） | 生产无入口（`MenuDraw.DeckCell(sortMode)` 只被 `Editor/ShellScene.cs:10708` 自检传 true） | 一致：都没有「进出排序模式」这回事 | ✅ |
| D4 | `SetSortMode` 的 0.8 | 有常量 | ✅ | `DAT_1834b2fa0 = 0.8`（内容根 localScale）·同时压制 hover/选中 | `MenuDraw.cs:3117 DeckCellSortK=0.8`、`:3340` | 仅自检可达（同 D3） | ✅ |
| E1 | 可拖闸 | ✅ | ✅ | `allowItemDrag && collection.Count == filteredCollection.Count`（`CollectionDisplay_object___SetCell__15ee7d0.c`） | `:6123 DeckDragAllowed`（比 `BaseDeckIndices`，**不含**名字/阵营筛；模式筛算进 base，与原版一致） | 无 | ✅ |
| E2 | 落点算法 | ✅ | ✅ | `DragAndDropItem.OnEndDrag` 找**最近格** → `SetSiblingIndex(best+1)` → `ReorderCollectionByHierarchy` 用「前一格的 Item 反查下标」→ `Remove`+`Insert`（`..._ReorderCollectionByHierarchy__15ed470.c`） | `:6196 DeckDropTargetPos`（最近、跳过自己）+ `:6244 MoveDeckInOrder`（Remove/Insert） | 无 | ✅ |
| E3 | 落盘时机 | ✅ | ⚠️ | ① `DeckCollectionTab.OnItemSelected`（普通模式）② `DeckCollectionDisplay.OnDisable()`（关页）；两处都先比 `collection` vs `filteredCollection`，**不同才** `DeckInventory.UpdateOrder` + `PlayerDataManager.OrderDecks` | **松手即落盘** `:6310 CollectionData.ReorderDecks`（`Close()` 里不重复写，`:6359` 已如实标注） | 结果同、时机不同（原版若中途退出不点任何格，顺序不落） | ⚠️ |
| E4 | 拖拽时其它格重排动画 | — | ✅ | 原版 `ReloadData()` + 还原 `normalizedPosition`，**无补间动画** | `:6236 RebuildDeckListNow()` | 无 | ✅ |
| E5 | 拖动中不悬停 | ✅ | ✅ | `CollectionItem.OnPointerEnter` 首句 `if (eventData.pointerDrag != null) return;` | `CollectionWindow.cs:5068 HoverSuppressed`（用「主键按住」等价） | 等价（按下瞬间移入会少亮一次） | ✅ |
| E6 | 有筛选 ⇒ 整页不可拖 | ✅ | ✅ | 同上 E1（两张表不等 ⇒ 不 `Initialize` 拖拽件） | `:6123` | 无 | ✅ |
| F1 | `Deck Filters` 抽屉几何 | ✅ | ✅ | 0.06,155.94→335.56,1080（335.50×924.06）+ 兄弟 `Shadow` 宽 153.01 | `:4696 Dflt*`、`:4720 BuildDeckFilterDrawer` | 无 | ✅ |
| F2 | 抽屉**不可滚** | ✅ | ✅ | Deck/Cardback 的抽屉**没有 Scroll View**（Cards/AltArt 才有，内容高 989.018） | 没建 Scroll View、没 `RegisterScroll` | 无 | ✅ |
| F3 | 开窗默认收起 + 0.3s 滑入滑出 | ✅ | ✅ | `Initialize` 里 `filterController.SetActive(false)`；`Toggle` 用 `DOAnchorPosX(hidden(-550,0), 0.3)` | `:4834` 收起、`:620 StepDrawer`（行程 −385 = 自定，保证整栏出屏） | 行程值自定（等价） | ✅ |
| F4 | `Deck Name Filter` | ✅ | ✅ | 行高 80；InputField 281.28×40 @27.17,175.94；搜索图标 35×30 @268.45,180.94；fs30 auto[10,35] base26 wrap3 | `:4697-4699 D*`；`:4736-4764` | 无 | ✅ |
| F5 | 名称筛选语义 | ✅ | ✅ | `deckName.Contains(input, InvariantCultureIgnoreCase)`；空白串直通 | `:4936 IndexOf(..., OrdinalIgnoreCase)` | 大小写口径 Invariant vs Ordinal（CJK/英文无差） | ✅ |
| F6 | `Army Filter` 几何 | ✅ | ✅ | 行高 345；Title 335.31×50 @25.06,240.94 fs36 auto[10,36] base32 Left；13 格 100×100 gap7 pad14（3/行） | `:4701-4703`；`:4813-4829` | 无 | ✅ |
| F7 | **阵营筛选：单选 vs 多选** | 单选 | ❌ | `DeckArmyFilter : ToggleCollectionFilter<CardDeck,CardArmy>`，内部 `Dictionary<TOption,bool> typeFilters`（`ToggleCollectionFilter.cs:135`），每格 `set_Item(option,isOn)` ⇒ **可同时选多个** | `_deckFacFilter` 是**单个 string**；`:4876 ToggleDeckFacFilter` 换一个就顶掉上一个 | **原版能同时选多个阵营，我们只能选一个** | ⚠️ |
| F8 | 筛选后空态 | ✅ | ✅ | `emptyWarning.SetActive(filteredCollection.Count < 1)` | `:4976 RefreshDeckEmpty`；矩形 165.88,70.94→1970.01,1080 fs36 | 无 | ✅ |
| F9 | 空态文案 | ✅ | ✅ | 官方 I2 `MenuCollection/NoDecksFound` = `There are no decks in your collection for the selected filters **or game mode**` | `Core/Loc.cs:793`（EN 已补 `or game mode`）、`CollectionWindow.cs:4237 Loc.T` | 无 | ✅ |
| G1 | 页内活动/模式过滤 | ✅ | ✅ | `currentEvent != null` ⇒ 只留 `deck.CustomGameModeEvent == currentEvent`；null ⇒ `CustomDebug.Log("No current event defined for deck filtering. Using all")` + 全给（`DeckCollectionTab.GetCollection`） | `:4932 _deckModeFilter >= 0 && d.GameMode != _deckModeFilter` | 语义等价；**null 档我们没有那条日志** | ✅ |
| G2 | 模式子页几何 | ✅ | ✅ | 页 330.8,155.9→1920,1080；Title 426.1,155.9→1824.7,243.5 fs72 base36 auto[18,72] Center/Capline wrap1；Scroll 330.8,260.9→1920,1063.2（802.30）；Content `HorizontalLayoutGroup spacing 50 + CSF` | `:4292-4311 DeckMode*`；`:4373-4432` | 无 | ✅ |
| G3 | 模式卡内容 | 自定 2 张 | ❓ | `SelectGameModeDeckTab.Initialize` 按 **LiveOps `IOwnDeckPlayEvent` 逐颗 `Instantiate(deckMenuContainer)`**；本地 0 事件 ⇒ 原版 `First()` 会抛 | `:4335 DeckModeCards` = 硬编码 Skirmish/Classic 两张（卡尺寸 505.9268×802.6741 来自第六十四会话逐值） | 服务端数据缺失下的离线等价；**未打 `🔶 自己设计` 标记**（见问题③） | ❓ |
| G4 | 进模式页的条件 | ❌ | — | `ChooseTabToOpen`：事件数 **>1** ⇒ 进模式页；**≤1** ⇒ 直接进列表（`SelectDecksTab.cs`/`ChooseTabToOpen.c`） | 恒先显示模式页（`SetDeckModeChrome(true)`） | 本地 0 事件，原版这一档实际会抛 ⇒ 差异不可观测；接 LiveOps 后要按事件数分派 | ⚠️ |
| G5 | 模式页 / 列表页显隐矩阵 | ✅ | ✅ | 模式页：关 `Decks Tab`/`Unlock`/`Create`/`Filter Toggle`/`Filters`，**开 `Import`**；列表页逐颗翻回来 | `:4548 SetDeckModeChrome` | 无 | ✅ |
| G6 | 返回链 | ✅ | ✅ | `BackButtonClicked`：`currentTab == 模式页` ⇒ `ESCPressed()`（关窗）；否则 `currentTab=null` + `ChooseTabToOpen`（回模式页） | `:4640 ShouldReturnToDeckModePage` + `:4661 OnBackClicked` | 无 | ✅ |
| H1 | 总张数 | ✅ | ✅ | 30（未设模式）/ 事件 `gameplayVariables.deckSize`（遭遇 12）（`DeckUtility.ValidateDeck:172` IL_0340 段 + `CardDeck.EmptySlotsCount`） | `DeckRules.cs:452 Validate` ④；`:200 CardCount` | 无 | ✅ |
| H2 | 同名上限 | ✅ | ✅ | `GetMaxCopiesInDeck(rarity,type)`：`cardType==10(hero) ⇒ 1`（在稀有度判断**之前**）；传说 1 / 其余 2 | `DeckRules.cs:165 CopyLimit(rarity,type)` | 无 | ✅ |
| H3 | 整副传说总数 | ✅ | ✅ | `maxLegendaries > 0` 才生效（经典 0 = 无上限，遭遇 4） | `:192 LegendaryTotalLimit`（经典写 `int.MaxValue`） | 无 | ✅ |
| H4 | 阵营限制 | ✅ | ✅ | `card.cardArmy != deck.deckArmy ⇒ InvalidDeck(4)`，**无中立例外**（`DeckUtility__ValidateDeckCards.c:57-61`） | `:492` 逐张 `SameFaction(c.Faction, deckArmy)` | 无 | ✅ |
| H5 | 督军位 | ✅ | ✅ | 必须 `cardType==Hero` 且 `hero.cardArmy==deck.deckArmy`；同一督军不能重复加（`CanAddHero`） | `:471-472`、`:496`；`CopyLimit` 督军恒 1 | 无 | ✅ |
| H6 | 防御卡 | ✅ | ✅ | `ValidateDeck` **不查**防御卡（可 0 张）；带则必须 `spellType==DefensiveCard`（`CanAddDefensive`） | `:481 defensive.Type != "defence"` | 我们多一条「槽位里那张必须是防御卡」—— 如实记（原版不查） | ✅ |
| H7 | 禁卡（事件） | ✅ | ✅ | `IsCardBanned(card) ⇒ DeckError 3`（`ValidateDeckCards`） | `DeckRules`（`CurrentEventData` 有值时才可能命中） | 本地无 LiveOps ⇒ 恒不命中 | ✅ |
| H8 | 持有数闸 | 等效 | ✅ | `CanAddCard`：`CardCopiesInDeck >= GetOwnedCount ⇒ MenuDeck/Error/MissingCopies` | 单机全解锁 ⇒ 恒通过（`DeckRules.cs:265` 已如实标注） | 无 | ✅ |
| H9 | 卡组准入（不能进卡组的卡） | ✅ | ✅ | `inventoryOptions != 0` 或 `showInCollectionOptions != 5 ⇒ InvalidDeck`（`ValidateDeckCards.c:68-77`） | `DeckRules.cs:298 CannotAddToDeck`（`Validate` ⑥b） | 无 | ✅ |
| H10 | 错误码 → 文案 | ✅ | ⚠️ | 原版 `DeckError` 只有 **6** 个值（`None/CardsNotOwned/MissingHero/InvalidDeckBannedCards/InvalidDeck/IncompleteDeck`，`DeckError.cs`） | `DeckRules.cs:44 DeckError` 细分到 ~15 个，全部映射到同一族 I2 键 `MenuDeck/Error/*` | 粒度更细、**文案与判定结果一致**；纯显示层无差，但「合法性」判据仍是同一个 bool | ⚠️ |
| H11 | 收藏页「不合法」的可见标记 | ✅ | ✅ | 只有禁卡档（Ban Icon + 名字变红 + 灰度）；**张数不足 / 缺督军完全不标记** | 我们不标记（同） | 无（`DeckInfoControls.cs:175 practiceButton.interactable = ValidateDeck(...)` 属弹窗） | ✅ |
| H12 | `AreThereInvalidArmyCards` | 不需要 | ✅ | 全反编译树 **0 个调用点**（只有定义 `CardDeck__AreThereInvalidArmyCards.c`） | 未实现 | 死码 | ✅ |

## ③ 问题清单（用户可见优先）

1. 🔴 **Deck 页 `Clear filters` 钮永远不出现**（用户可见）
   - 原版：`clearFiltersButton.SetActive(toggleOptions.Count > 0)`（有任一筛选选中就显示）。
   - 我们：`RefreshClearFilter(0)` 读 `PageDrawer(0).State`，而 `_deckFltSlide` 构造时**没给 State**（`CollectionWindow.cs:4834`）⇒ 恒 `false`；`ClearDeckFilters()`(`:4882`) 与 `ApplyDeckFilterChanged()`(`:4943`) 都不刷那颗钮。
   - 建议：把「Deck 页有没有筛选」做成一个显式谓词（`_deckFacFilter != "" || _deckNameFilter != ""`），在 `RefreshClearFilter` 的 page==0 分支用它；并在 `ToggleDeckFacFilter` / 名称输入回调 / `ClearDeckFilters` 三处各刷一次。**别**去给 `_deckFltSlide` 塞一个假 `State`（那会把卡牌页那套筛选模型混进来）。
2. 🔴 **点一格卡组就改「当前卡组」并落盘**（用户可见）
   - 原版：`CollectionDisplay.Select` 只写 `SelectedItem` 并调 `OnItemSelected`；`DeckCollectionTab.OnItemSelected` 只开详情窗 —— **不写任何持久状态**。改当前卡组只在弹窗 `DeckInfoControls.OnSelect`。
   - 我们：`SelectDeck(i)` 先 `CollectionData.Select(i)`（写盘）再开窗（`:5342-5359`）。
   - 建议：`SelectDeck` 里去掉 `CollectionData.Select`，只留 `OpenDeckInfo`；「当前卡组」的写入只保留在 `DeckInfoPopup:2245` 那一处（否则玩家只是点开看看，战斗用哪套就被悄悄换了）。
3. 🟠 **页头筛选 chip 整套没建**（用户可见）
   - 原版：`CollectionFilterController.filterIconHolder` = 页头 `Header/Filters`（**GridLayoutGroup cellSize 85×70 · spacing 5,0 · FixedRowCount**），每个已选筛选挂一个 chip，**最多 5 个**然后 break。
   - 我们：`Header/Filters` 里只有 `Clear Filter Button`。四条页签都缺，不止 Deck。
   - 建议：先按 13 阵营图标的 85×70 格建 chip（图标 + 可点取消），数量上限 5；`Clear filters` 的 x 可能要跟着 chip 数右移（原版是 `IgnoreLayout` + 序列化位，需再核一次 CSV/MinSize 的运行时行为）。
4. 🟠 **`Import` 缺 interactable 闸（卡组满 114 套）**
   - 原版 `importDeck.interactable = ownedDeckCount < GameStaticData.totalCustomDecks(114)`。
   - 我们无闸。建议在 `BuildDeckHeader` 收尾 + 每次重建列表后按 `CollectionData.DeckCount()` 设 `_deckImportHit` 的 `WindowButton.Interactable`。
5. ⚠️ **阵营筛选只能单选**（原版多选）
   - 原版 `DeckArmyFilter : ToggleCollectionFilter`，`Dictionary<CardArmy,bool>` 可多选同时生效。
   - 我们 `_deckFacFilter` 是单值。建议把该字段换成 `HashSet<string>`（或复用 `DeckEditorState.Filter.Armies` 那套集合口径，见 `DeckEditorState.cs:63` 已按 `typeFilters` 做过一次）。
6. ⚠️ **禁卡档（Ban Icon + 名字变红 + 灰度）未建** —— 本地不可达，但接 LiveOps 后必须补。建议现在就记进活状态表，**别当已做完**。
7. ⚠️ 上述 G3：模式页两张卡是离线等价物，**未打 `🔶 自己设计（非原版）` 标记**（铁律 18 要求机器可 grep）。建议在 `DeckModeCards` 声明处就地改写那条注释补标记（⛔ 别新增行 —— 该文件有账落点）。
8. ⚠️ E3：落盘时机（松手即写 vs 关页才写）——结果同，但原版「拖完不点任何格、直接 Alt+F4」不会落盘。属可接受差异，**已如实记录**，不建议改。
9. ⚠️ G4：进模式页的条件（事件数 >1）我们没有对应分支——本地 0 事件下不可观测，接 LiveOps 时按 `ChooseTabToOpen` 补。
10. ⚠️ H10：`DeckError` 枚举粒度比原版细（6 → ~15）。判定结果（合法/不合法）与原版一致，只是错误文案更具体。**不建议收敛**，但要在账里留这一条，免得后人以为「枚举对不上 = 没复刻」。

## ④ 查不到清单

| # | 问题 | 现在知道什么 | 还差什么（搜过的目录/关键词） |
|---|---|---|---|
| ?1 | 模式卡（`SelectDeckGameModeMenuContainer` 的 prefab 真身） | 尺寸 505.9268×802.6741 + 暗带 + Event Title fs58.8 等来自第六十四会话逐值记录；本窗两张卡是离线等价 | `assets_full/bundle_menus_assets_all/GameObject/` 里**没有**叫 `SelectDeckGameModeMenuContainer` 的 GO（按名搜 `GameObject/` 只出 `Base Game Mode Container*` / `Draft Game Mode Container`）；`deckMenuContainer` 是 `SelectGameModeDeckTab` 的序列化引用，未追到目标 bundle |
| ?2 | 空池格会不会显示 `Create New Deck` | prefab 里 `Create` 节点存在但 INACT；`createObject.SetActive(item==null)` 是唯一写点；`SetCell` 只对 `index < filteredCollection.Count` 调 `Config` ⇒ 推断不可达；用户实拍 4 格无此格 | 未读到 `VerticalRecyclingSystem.OnScroll/UpdateCell` 的**方法体**（`out_cpp2il_dev_ilspy/PolyAndCode.UI/*` 里只有字段与 `InitCoroutine`；`decomp_full` 无 `PolyAndCode.UI.*` 的任何方法）⇒ 「不会越界调 SetCell」是**推断**，不是读出来的 |

## ⑤ 顺手发现（不属本行或值得记账）

1. 🔴 **`Clear Filter Button` 的 x 四页共用一个常量 `612.2`**（`CollectionWindow.cs:3403`），但 prefab 实测：Deck **612.2**、Cards **1488.6**、Cardback **1488.6**、AltArt（未读）。⇒ 卡牌页 / 卡背页的清除钮可能整体挪了 ~876px。**超本行范围**，交给卡池/卡背页的代理复核（`menu_dump` 那一行已含三页的值）。
2. ⚪ `GameStaticData.totalCustomDecks = 114` **同时**是 Deck 页 `_poolSize` 的值（`RecyclableScrollRect` MB `8316412282010198741`）—— 这两个 114 是不是「同一件事被写了两遍」我没有证据，**不下结论**，但值得记一笔。
3. ⚪ `DeckRuntime.CurrentEventData` 目前恒 null（本地零 LiveOps）⇒ 收藏页所有「禁卡」相关档（卡池 Ban 徽标、卡组格 Ban Icon、红名、灰度）都不可达。接 LiveOps 时是**一批**要补的，不是一条。
4. ⚪ `SelectDecksTab.Setup()` 每次 `ToggleSelectDeck` 都会**重新 `AddListener`**（`createDeck.m_OnClick` / `importDeck.m_OnClick`）—— 原版自己的小毛病（UnityEvent 不去重）。我们建一次挂一次，**不建议**照抄。
5. ⚪ 卡组名红名色原版是 `new Color(1.0f, 0.3f, 0.3f, 1.0f)`，配合 `ToggleGreyScale(true, **0.25f**)`（卡组格是**带补间**的 0.25s，与卡池格的「瞬时 0」不同）—— 补 C12 时两个值都要用这一组。

## ⑥ 覆盖边界

- **反编译读透**：`SelectDecksTab.cs`(368) · `DeckCollectionTab.cs`(il2csharp 全) · `CollectionDeck.cs`(全) · `DeckCollectionDisplay.cs`(全) · `CollectionItem.cs`(Config/OnEnable/OnDisable/OnPointerClick/Drag 段) · `CollectionDisplay<T>` 的 `SetCell/ReorderCollectionByHierarchy/Select`（`decomp_generic_trial`）· `DeckNameFilter.cs` · `DeckArmyFilter.cs` · `ToggleCollectionFilter.cs`(字段 + Filter 签名) · `CardDeck.cs`(CanAdd*/CardCount/EmptySlotsCount/MaxDeckSize/AreThereInvalidArmyCards) · `DeckUtility.cs`(ValidateDeck/ValidateDeckCards) · `DeckError.cs` · `DeckInfoControls.cs`(interactable 三条) · `GameStaticData`(totalCustomDecks=114)。
- **prefab**：`Collection Deck`(pid −2024334647938937812) 全树 · `Select Game Mode Deck Tab`(全树) · `Collection Menu Variant` 的 Header/Control Buttons/Filters 三条 grep · 6 份 `filterIconHolder` MB。
- **我们**：`Shell/CollectionWindow.cs`（Deck 页几何 / 页头 / 列表 / 拖拽 / 筛选 / 模式页 / 选择）· `Shell/MenuDraw.cs`(`DeckCell`) · `Shell/CollectionData.cs`(`DeckInfo`) · `RuleEngine/Core/DeckRules.cs`(Validate/RemoveInvalidCards/CopyLimit)。
- **没跑的东西**：Unity / typecheck / 真机（只读代理红线）。
- **未读**：`DeckInfoPopup` 内部（另一代理）· `VerticalRecyclingSystem.OnScroll` 方法体（见 ?2）· `SelectDeckGameModeMenuContainer` 的 prefab 真身（见 ?1）。
- ⚠️ 提醒：**「版面参数对上了」≠「这一页复刻完了」** —— 本表 ✅ 里有一半是几何；chip / 多选筛选 / 禁卡档 / 当前卡组语义这四条才是玩家能感觉到的差。
