# B121-COLL16 · 收藏窗**页头筛选 chip**（四页）—— 判据 + 实现

> 会话 2026-11-03 · 执行代理 `E121-COLL16` · 判据一手源 = `d:/2/tools/decomp_generic_trial/out/`（方法体）+ `d:/2/新解包资源/assets_full/bundle_menus_assets_all/`（prefab 字段）+ `工具/menu_dump.py` 现读。
> 审计里的问题 6 / §五 `?2`（❓）**本条结案**：PPtr 没错、两者也不互斥 —— 是**容器会随 chip 变宽**（下面第 3 问）。

## 一、四问答案（每条带出处）

**问 1：`filterIconHolder` 的 PPtr 到底指向谁？**
→ **就是页头那颗 `Filters`**。四个收藏页各一份 `CollectionFilterController` 派生件，逐份解 PPtr：

| 页 | 控制器（MonoBehaviour pid） | `filterIconHolder` → 父链 | 唯一子件 |
|---|---|---|---|
| Deck | `DeckCollectionFilterController` `-5987181221857729835` | `Filters / Header / Select Deck Tab / …` | `Clear Filter Button` |
| Cards | `CardCollectionFilterController` `-3404574427676351787` | `Filters / Header Filters / CardsTab / …` | `Clear Filter Button` |
| Cosmetics | `CardbackFilterController` `6460081647219630805` | `Filters / Header / Cardback Tab / …` | `Clear Filter Button` |
| Styles | `CardCollectionFilterController` `-8720134135039335723` | `Filters / Header Filters / Alternate Art Tab / …` | `Clear Filter Button` |

（另两份同名控制器在 **`Deck Editing Menu`** 里，`filterIconHolder` 的子件是 `Generic Simplified UI Button_updated` —— 那是**卡组编辑窗**，不在本单范围；它的 chip 同样应搬进页头，见 §五。）
四页的 `Filters` 矩形**逐页相同** = **592.2, 70.9 → 592.2, 155.9**（宽 0 · 高 85；`menu_dump.py … --rt <pid>` 现读）；Cardback 那一份序列化宽是 876.42（见问 3）。

**问 2：chip 的父节点与 rect？**
→ 父节点 = 上面那颗 `Filters`。组件（`MonoBehaviour_-1015512437202754859` / `4664594681446458069`）逐字段：
`GridLayoutGroup`：`m_CellSize = (85,70)` · `m_Spacing = (5,0)` · `m_Padding = 0` · `m_Constraint = 2 (FixedRowCount)` / `m_ConstraintCount = 1` · `m_ChildAlignment = 3 (MiddleLeft)`；`ContentSizeFitter`：`m_HorizontalFit = 1 (MinSize)` · `m_VerticalFit = 0`。
⇒ chip i：**左缘 = 592.17 + 90i**、宽 85、高 70；纵轴 `MiddleLeft` ⇒ **顶边 = 70.94 + (85−70)/2 = 78.44**。
chip 的**长相** = 那一族 option toggle 的 clone（`FillToggleList` 里 `Instantiate(toggle.gameObject, toggle.transform.parent)` + `Initialize(option)`）：
`Army`/`Cost` 的 `Background` **铺满**整格；`Rarity`/`Type` 的 `Background` 是 **50×50 居中**；
`Rarity`/`Type` 另外调 **`HideLabel()`**（只留图标）；`Cost` 不调（那颗 `Label` 就是数字，是 `Background` 的子件、伸锚铺满）。

**问 3：与 `Clear Filter Button` 的显隐互斥关系？**
→ **不互斥，是共存**：`NotifyFilterChange` 末句 `clearFiltersButton.gameObject.SetActive(0 < toggleOptions.Count)`（`.c:190`）与 chip 用的是**同一个** `toggleOptions` 计数 ⇒ **有 chip ⇔ 有钮，无 chip ⇔ 两者都没有**。
→ **也不重叠**：那颗钮带 `LayoutElement.m_IgnoreLayout = 1`（prefab 实读）⇒ 既不被网格排、也不计入 CSF；它自己的锚是 `anchorMin = anchorMax = (1,.5)` · `pos = (20,0)` · `pivot = (0,.5)` ⇒ **左缘 = 容器右缘 + 20**。
容器宽由 CSF(MinSize) 决定：`GridLayoutGroup.minWidth = (85+5)·n − 5`（`FixedRowCount` ⇒ 列数 = n）；`n = 0` 时算出来是 **−5**，而 `LayoutUtility.GetMinSize` **忽略负值** ⇒ 宽 **0**（= 实测那颗钮的 612.2 那一档，CSF 根本没被标脏）。
⇒ **n ≥ 1 时容器宽 = 90n − 5，钮被顶到 `592.17 + 90n − 5 + 20`；最后一颗 chip 右缘到钮左缘恒 20px。**
**审计 `?2` 的那个顾虑（592.2 压 612.2）不成立** —— 那是「容器宽恒 0」的读法（旧注释还把它写成了 `CSF(H=Preferred)`，**已就地订正**）。

**问 4：四页各自建几颗？**
→ 一颗 chip = **一个已选 option**，上限 **5**（`FillToggleList(toggleOptions, 5)`；那个 `toggleOptions` 是**跨 filter 累计**的同一个列表，`.c:141` 还有一道 `if (5 < count) break`）。
顺序 = 原版 `filters` 数组序（`optionsFinal` 序在组内）：Cards / Styles = `Name · Army · Rarity · Cost · Type · Upgradable`；Deck = `Name · Army`；Cardback = `Army`。
`CardNameFilter.FillToggleList` 与 `UpgradableCardFilter.FillToggleList` 是**空体** ⇒ 它们一颗都不出（`CardNameFilter__FillToggleList.c` = 一句 `return`）。
⇒ 实际会出 chip 的只有 **阵营 / 稀有度 / 费用 / 卡类** 四类。

## 二、判据成立 ⇒ 已实现

| 落点 | 改了什么 |
|---|---|
| `Core/FilterPanelModel.cs` 类尾 | 新增 `HdrGrpL/HdrGrpT/HdrGrpH/HdrChipW/HdrChipH/HdrChipSpX/HdrChipY/HdrChipMax/HdrIconS` · `HdrClearX(n)` · `BuildHeaderChips(facs, f, cells, armyOnly)` · `HdrChip(...)`（四页共用一份几何/上限/顺序） |
| `Shell/CollectionWindow.cs` 类尾 | 新增 `_hdrFilters[4]` · `_hdrChipNodes[4]` · `RefreshHeaderChips(page)`（重建 chip 节点 + 把 `Clear filters` 三件挪到「最后一颗 chip + 20」）· `HeaderChipCells(page)` · `MoveHeaderNode` · `DestroyNodeNow` |
| 同上（净零行改） | `BuildFilterHeader` 里：容器 `Filters` 的矩形改成原版模板矩形（592.17, 70.94 → 592.17, 155.94）并**存引用**；末尾 `RefreshClearFilter(pageNo); RefreshHeaderChips(pageNo);`（同一物理行） |
| 同上（净零行改） | `RefreshClearFilters()` → 四页各多刷一次 chip（原版 `SetActive(0 < count)` 与 chip 是**同一条判据**，一起刷才不会分叉） |
| 新建 `Editor/B121_COLL16_Assert.cs` | ①数量/顺序/上限 ②选中态 ③与 Clear 钮的共存 + 位移（含灭自证）④无筛选=0 颗 的反证 |

chip 可点（原版 `_003CNotifyFilterChange_003Eb__0` = `toggle.ForceOff()`）：Cards/Styles/Cosmos 走 `Scope(owner, ApplyFilter("$chip:..."))`（`FilterPanelModel.Click` 本来就会剥 `$chip:` 前缀）；Deck 页走它自己的 `ToggleDeckFacFilter(fac)`（该页没有 `FilterPanel.State`）。

## 三、验证

- `python -I -X utf8 工具/check_ledger_points.py` ⇒ **A 5876 · 通过 5876 · 漂移 0 · 失败 0**（本批中间段的编辑全部做成**净零行**：新增成员只在类尾，`BuildFilterHeader` 的三处改动都落在原物理行上）。
- `TMPDIR=/tmp/wf_c16 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（收工前最后两次跑；中途几次被**别人在飞的文件**挡住：`ArenaTimelinePlayer.cs` → `EnchantmentExecutor_B115.cs` → `ChoiceOptionCards_B116W32.cs` → `EnchantmentPayload_B121D1B.cs` 与 `CardDef` 成员重名 —— 每次报的文件都不一样、全不在本单白名单内；它们收工后本单复跑即 0/0）。
- 断言宿主 **未接线**（`Editor/*Scene.cs` 不在本单白名单）⇒ 调度台在 `CollectionScene.Run` 里加一句 `B121_COLL16_Assert.Run();` 即可跑。

## 四、其它

- 报告里 §一 的 PPtr 解法**沿用 `m_Script` → monoscripts 反查**（类名不在文件名里，按类名搜序列化字节恒 0 命中）。
- `menu_dump.py` 的 `--rt` 直接吃 `filterIconHolder` 的 pid，能把父链与 rect 一次打出来（本单四页都是这么读的）。

## 五、顺手发现（只报不改）

1. 🔴 **同一个 chip 现在会画两次**：原版 `FillToggleList` 在抽屉行里 Instantiate 出副本后，`NotifyFilterChange` **立刻 `SetParent(filterIconHolder)` 搬走**（`.c:154`）；而 2026-10-25「C 域 · chip 行」那批把副本留在了**行内**（`FilterPanelModel.AppendChips`，四个调用点），本批按审计的施工单只补了页头 ⇒ **选一个阵营，抽屉行里一颗、页头又一颗**。
   修它要一起动：`FilterPanelModel.Build`（四个 `AppendChips` + `ComputeLayout(facs.Count + armySel)`）· `BuildTitles`（行高随 Army 行变）· 卡组编辑窗 `DeckRuntime` 的同一套模型 —— **跨了本单白名单与两条既有断言**（`Editor/DeckScene.cs:2070` 的「Army 行长高」、`B121_C1_Assert` 的 `AppendChips` 直测）⇒ 建议**单独一批**做，别顺手夹带。
2. 🔴 **`Deck Editing Menu` 的两份 `CollectionFilterController` 也把 chip 搬进页头**（`filterIconHolder` = `Filters / Header / Content Area / Deck Editing Menu`，子件是 `Generic Simplified UI Button_updated` 而不是 `Clear Filter Button`）⇒ 卡组编辑窗页头同样有 chip，目前一颗都没建（同上，属 `DeckRuntime.cs`，本单白名单外）。
3. ⚠️ **B121-COLL-04 那条「卡背页 `Clear filters` = 1488.59」是【序列化模板值】**：卡背页 `Filters.sizeDelta.x = 0` 只是因为 CSF **从没被标脏**（无 chip）；一旦有 chip，CSF 会把宽压成 `90n−5`，那颗钮落到 `607.17 + 90n`。⇒ 本批在 `n > 0` 时改用 `HdrClearX(n)`、`n == 0` 时保留 `ClearFilterX(page)`（静止态逐位不变，`B121_C1_Assert` 的 COLL-04 四条仍然绿）。
4. ⚠️ 卡背页的**选中态**还有一处既存分叉：`FilterPanelModel.BuildCosmetics` 的 `On` 读**单值** `f.Faction`，而 `Click("$fac:")` 与 `FilteredCosmoNames` 用的是**集合** `f.Factions` ⇒ 卡背抽屉里点阵营**不会亮**。本批的页头 chip 按「真正生效的那条判据」（`FactionSelected`）出，所以 chip 会正常出现；**行内高亮那条留账**（属 `BuildCosmetics` + DeckRuntime 共用件）。
