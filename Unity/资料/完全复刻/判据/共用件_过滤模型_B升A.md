# 共用件 B 升 A —— 过滤模型（`Core/FilterPanelModel` 那一族）

> **口径**：同 C 的「B 升 A」——逐方法读**原版方法体** → 找我们实现 → 判 **A 已对齐 / C 该做没做（写差什么）/ B 仍待核（写缺什么）**。
> 判据优先级：① `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/**` → ② `D:/2/tools/decomp_full/**.c` → ③ `assets_full` 字段。
> 本文件是**判据记录**，不改别人的账；C 的 `账/收藏.tsv` / `账/卡组编辑.tsv` 里对应行可按本表改判。

## 0. 范围划定（先证「归谁」）

| 原版类 | 我们的对应 | 文件 | 归谁 |
|---|---|---|---|
| `CollectionFilterController<T>` | 模型层 = `Core/FilterPanelModel.cs`（布局/格/点击）+ 视图/控制器 = `Shell/CollectionWindow.cs`（`FilterDrawer` / `ClearFiltersNow` / `RefreshCardsAfterFilter`）+ `Deck/DeckRuntime.cs`（`ApplyDrawerSlide` / `RefreshFilterCells`） | 见右 | **模型层归 E**；视图层归 C/H |
| `ToggleCollectionFilter<T,TOption>` | 同上（每个筛选一格的「值」在 `Deck/DeckEditorState.cs` 的 `DeckFilter`） | 见右 | **模型层归 E**；筛选应用归 C |
| `CollectionFilterToggle` | `FilterPanelModel.Cell`（一格的 rect/图标/文本/开态） | `Core/FilterPanelModel.cs` | E |
| `ArmyItemContainer` | **我们没有这个类**（全仓 `class ArmyItemContainer` 0 命中；战役/锻造页的阵营格是各页自建） | — | **C（P2）「原版有、我们无」**（调度台 2026-10-25 裁定：按 C 记，⛔ 别当 B 挂） |
| `DeckDrawer` / `DeckAndCardbackDrawer` | **我们没有这两个类**（`class DeckDrawer` / `class DeckAndCardbackDrawer` 均 0 命中）；`ItemDrawer` 家族在 **`Shell/ItemDrawer.cs`** | — | **C（P2）「原版有、我们无」**；本判定不归 E（`ItemDrawer` 在 Shell/） |

⇒ 「若 `ArmyItemContainer` / `DeckDrawer` 的**基类**在 `Core/**`」这一条**不成立**：两个类我们都没有；`ItemDrawer` 在 `Shell/`。

## 1. `CollectionFilterController<T>`（原版 `CollectionFilterController.cs`，10 个成员）

| # | 方法（原版行） | 原版行为（方法体要点） | 我们的实现 | 判定 |
|---|---|---|---|---|
| 1 | `Toggle(bool option, bool withAnimation)`（`:154`） | 先判 `activeSelf == option` 早退 → `DOTween.Kill` → 展开：`SetActive(true)` + `DOAnchorPosX(rect, originalAnchorPosition.x, 0.3)`；收起：动画到 `hiddenPosition.x`，完成才 `SetActive(false)`；`withAnimation=false` ⇒ 瞬切 | `CollectionWindow` 的滤镜抽屉（`Slide` 进度 + `TickDrawers`）与 `DeckRuntime.ApplyDrawerSlide`：**只改 localPosition.x、行程 −385px（−550↔−165）、0.3s**，逻辑态 `Open` 与滑动进度分离 | **A**（判据在两端代码注释里已逐条落：`CollectionWindow.cs:480-490` / `DeckRuntime.cs:2597-2605`） |
| 2 | `SetupFilters()`（`:205`） | 隐藏 `filterIconHolder` 的旧图标 → 给 `clearFiltersButton` 绑监听 → 遍历 `filters` 逐个 `SetupFilter` → **缓存 `originalAnchorPosition = rect.anchoredPosition`** | `Build()` 里建整栏 + `Clear filters` 钮绑 `ClearFiltersNow` + 抽屉原位记在 `FltAbs`/`Slide` 那一套常量（视口左缘 + 光圈半径） | **A** |
| 3 | `NotifyFilterChange()`（`:314`） | 先销毁上一批**已选筛选 chip**（`CollectionFilterToggle`）→ 对 `filters` 逐个 `OnFilterChange()` → 用 `FillToggleList(toggleOptions, listLimit)` 重建**可点删除的 chip 行** → `OnFilterChanged?.Invoke()` | 有「筛选变化 → 重算显示」这条（`CollectionWindow.ApplyDeckFilterChanged` / `CollectionData`），**没有 chip 行**：`grep -r "ActiveFilter\|FilterChip\|RemoveFilter"` 全仓 **0 命中**，`FilterPanelModel` 也没有这一族 | **C（P2）**：缺「已选筛选的可删 chip 行」+ 每次变化后的 chip 重排。落点 = `Shell/CollectionWindow.cs`（C 域） |
| 4 | `Filter(List<T> collection)`（`:575`） | 对 `filters` **逐个** `Filter`（跨筛选 **AND**）；任一为空 ⇒ 空集 | `DeckEditorState.VisibleCards()`：**AND** 跨 5 类（Name/Faction/Rarity/Type/Cost）+ Owned/Upgradable 两个独立开关 | **A（含一处差异，见下）**：跨筛选 AND 对齐；**单个筛选内部**原版是「已选集合的并集」，我们是**单值**（第 5 条） |
| 5 | `ClearFilters()`（`:641`） | 遍历 `filters` 清空 + `NotifyFilterChange()` | `ClearFiltersNow()`：清 5 个字段、**不动** `Owned`/`Upgradable`（与原版「清 `filters[]`」同口径） | **A** |
| 6 | `_003CToggle_003Eb__9_0`（`:819`） | 收起动画完成回调：`SetActive(false)` | `CollectionWindow` 抽屉滑动完成时按 `Slide` 归零才关层 | **A**（语义并入 1） |
| 7 | `_003CNotifyFilterChange_003Eb__0(bool b)`（`:130`） | chip 的 `ForceOff()`（点 chip = 取消该筛选） | 不存在（同 3） | **C**（随 3） |
| 8 | `_003C_002Ector_003Eb__14_0`（`:121`） | 空 lambda（委托占位） | — | **D**（编译器产物，无行为） |

## 2. `ToggleCollectionFilter<T,TOption>`（原版 `ToggleCollectionFilter.cs`，8 个成员）

| # | 方法（原版行） | 原版行为（方法体要点） | 我们的实现 | 判定 |
|---|---|---|---|---|
| 9 | `SetupFilter(CollectionFilterController<T>)`（`:147`） | 遍历 `optionsFinal`：实例化 `CollectionFilterToggle`、`Initialize(option)`、绑 `OnValueChanged → controller.NotifyFilterChange`、按当前值设开态 | `FilterPanelModel.Build()` 按每个筛选产出 `Cell`（rect/图标/文本 `On` 初值）+ 宿主把点击接到「写 `DeckFilter` → 刷新」 | **A**（单值口径下行为同构；集合口径见 10/11） |
| 10 | `OnFilterChange()`（`:260`） | 遍历每个选项的 `IsOn` → 写回 `typeFilters[option] = isOn`（**已选集合**） | 没有集合：`DeckFilter` 每类**一个值**（`Faction`/`Rarity`/`Type`/`Cost`），点击=选中/再点=取消 | **C（P2）**：**同类多选（并集）没做**。原版 `ToggleCollectionFilter` 的 `options` 是一组开关、`OnFilterChange` 逐项收集 ⇒ 同一类可同时选多项。落点 = `Deck/DeckEditorState.cs`（`DeckFilter` 要改成集合）+ `Core/FilterPanelModel` 的 `Click` |
| 11 | `Filter(List<T> collection)`（`:330`） | 「没有任何已选」⇒ 原样返回；否则 `Where(FilterItem(item, 任一已选项))`（**并集**） | 每个类单值匹配（`c.Faction == f.Faction` 等） | **C（P2）**（随 10） |
| 12 | `FilterItem(T card)`（`:609`，抽象） | 子类实现「这一项属不属于该选项」；例 `CardCostFilter.FilterItem` 先把卡费夹到 `[1,8]` 再比档位 | `DeckEditorState.VisibleCards()` 里逐类判据（含费用 `ClampPoolCost` —— 判据 = `decomp_full/CardCostFilter__FilterItem.c:15-20`，已逐条对） | **A** |
| 13 | `ClearFilters()`（`:611`） | 所有开关 `SetValue(false, notify:false)` + 集合置 false | `ClearFiltersNow()`（同 5） | **A** |
| 14 | `FillToggleList(List<CollectionFilterToggle>, int listLimit)`（`:671`） | 遍历选项：**只对 `IsOn==true` 的**、且 `currentList.Count < listLimit` ⇒ 在**该开关的父节点下** `Instantiate` 一个 chip 并 `Initialize(option)` ⇒ **已选 chip 行**（配 `NotifyFilterChange` 的销毁重建） | 不存在 | **C（P2）**（随 3） |
| 15 | `_003CFilter_003Eb__7_0/7_1(bool b)`（`:120/:125`） | 两个谓词：`!b` / `b`（「全没选」与「任一选中」） | 单值判据等价物在 `VisibleCards` | **C/B**：谓词本身的语义靠 10/11 的集合实现；今天是单值 ⇒ 随 10 |
| 16 | `_003CSetupFilter_003Eb__5_0(bool b)`（`:803`） | 开关变化回调 → `controller.NotifyFilterChange()` | 宿主点击回调 | **A** |
| 17 | `_003CFilter_003Eb__7_2(T rc)`（`:808`） | 单卡谓词（`FilterItem`） | 同 12 | **A** |

## 3. 小结（给账用）

- **A：10 条**（`Toggle` · `SetupFilters` · `Filter(控制器)` · `ClearFilters`×2 · `SetupFilter` · `FilterItem` · `SetupFilter 回调` · `_b__7_2` · `_b__9_0`）
- **C（P2）：5 条**（`NotifyFilterChange` · `OnFilterChange` 集合口径 · `Filter(单筛选)` 并集 · `FillToggleList` chip 行 · `_b__7_0/7_1`）
  - **共同根因一条**：**「同一类筛选多选（并集）+ 已选 chip 行」这一族没做**（`grep ActiveFilter|FilterChip|RemoveFilter` 全仓 0 命中）。
  - **落点**：`Deck/DeckEditorState.cs` 的 `DeckFilter`（单值 → 集合）+ `Shell/CollectionWindow.cs` / `Deck/DeckRuntime.cs` 的 chip 行（C/H 域；`Core/FilterPanelModel` 的 `Click` 要跟着改）。
- **D：1 条**（空 lambda）。**B：0 条**。
- ⚠️ **`ArmyItemContainer` / `DeckDrawer` / `DeckAndCardbackDrawer`**：我们**没有这三类**（全仓 `class` 声明 0 命中）
  ⇒ 按调度台 2026-10-25 的裁定记 **C（P2）「原版有、我们无」**（⛔ 别挂成 B）；`ItemDrawer` 在 `Shell/ItemDrawer.cs`，本判定不归 E。
