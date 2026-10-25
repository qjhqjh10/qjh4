# C · chip 行调研（停手待裁）+ 费用多选 + CardServices 错误分支表 · 第 1 轮

> 2026-10-25 · 执行代理 C。
> `TMPDIR=/tmp/wf_c bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（本轮**中途红过一次**，见 §④ 如实记录）。

---

## ① chip 行 —— **调研完了，按你的规矩「停手写报告」**（⛔ 没写代码）

### ①·1 判据（原版怎么放 chip）
`ToggleCollectionFilter.FillToggleList`（`共用件_过滤模型_B升A.md` 第 14 条）：**只对 `IsOn==true` 的选项**，
在 **`option.toggle.transform.parent`** 下 `Instantiate` 一个 chip；`NotifyFilterChange` 每次先销毁上一批再重建；点 chip = `ForceOff()`。

### ①·2 那层父节点**是不是自动布局** —— 是（prefab 现读，`menu_dump.py … "CardsTab" --depth 9 --no-layout`）
| 行 | `Content` 上的组件 | 参数 |
|---|---|---|
| `Army Filter/Content` | **`GridLayoutGroup` + `ContentSizeFitter`** | cell **100×100** · spacing **7/0** · pad **14,0,0,0** · CSF **v:MinSize** |
| `Rarity FIlter/Content` | **`GridLayoutGroup`**（**无 CSF**） | cell 100×100 · spacing 7/0 · pad 14,0,0,0 |
| `Cost Filter/Content` | **`GridLayoutGroup`**（**无 CSF**） | cell **65×65** · spacing **15/20** · pad 15,0,0,0 |
| `Type Filter/Content` | **`HorizontalLayoutGroup`**（无 CSF） | spacing 0 · align 6 · pad 15,0,0,0 · ctrlW/H = 0/0 · expandW/H = 1/1 |
| 外层 `Filters` | **`VerticalLayoutGroup` + `ContentSizeFitter` + `LayoutGroupContentFixer`** | spacing 0 · ctrlW=1 · CSF **v:MinSize** |

⇒ **chip 不是「另起一行」**：它和选项 toggle 是**同一批 cell**，被塞进同一个 `GridLayoutGroup`/`HorizontalLayoutGroup`，
由布局组自动排到**下一个格位**（Army 那一行有 `ContentSizeFitter(MinSize)` ⇒ 行会**长高**；Rarity/Cost/Type **没有 CSF** ⇒ 多出来的 cell 会**溢出那一行的框**，压到下一行标题上）。

### ①·3 为什么停手（按你的规矩）
我们这套筛选抽屉是**固定矩形**（`FilterPanelModel.ComputeLayout` 手算每格 rect），**没有** `GridLayoutGroup` / `VerticalLayoutGroup` / `ContentSizeFitter` 这一层。
两条路：
- **(a) 照原版引入布局组** —— 就是我们**完全没有的组件族**（而且会与「固定矩形 + 手算」那套并存 ⇒ 两份几何）。**按你的规矩：停手，不发明。**
- **(b) 用我们已有的网格算术复刻** —— 我们已经有「按 cellSize/spacing/pad 算第 n 格 rect」的算法（收藏卡池 `CardsCols/PadX` 那一套）⇒ **能算**。但**剩下一个口径要你裁**：Rarity / Cost / Type 三行**原版没有 CSF** ⇒ 多选后多出来的 chip **会溢出那一行**（原版就是这样）。我们要不要**照抄这个溢出**？
  - 照抄 ⇒ 忠实，但屏幕上会出现「chip 压在下一行标题上」。
  - 不照抄（让行长大）⇒ 观感更好，但**偏离原版**，且要改三行的高度 + 下面所有行的 y（我们的 `ComputeLayout` 是**一次性算死**的）。

👉 **请裁**：chip 行走 **(a) 停（等你给布局组件口径）** 还是 **(b) 用网格算术 + 照抄溢出**？我倾向 **(b) 但不照抄溢出**（理由：溢出在原版可能只是「没人同时选两项」的偶然，不是设计意图）—— 但这是**偏离**，要你拍板。

---

## ② 费用筛选多选 —— **做完了**（`Cost..CostMax` 区间 → `HashSet<int>`）

- `DeckFilter` 加 **`HashSet<int> Costs`** + **`CostOk(cost)`**（**集合非空 ⇒ `ClampPoolCost(cost) ∈ 集合`**，与原版一样**夹完再比**；否则走旧区间）+ **`CostSelected(lo)`**（UI 用）。
- `VisibleCards()` 里那一整段「费用区间」**被 `CostOk` 取代**（旧的 40 行注释块随之删除 —— 它记的「夹取」判据已搬进 `CostOk` 的 doc）。
- `FilterPanelModel`：费用格的 `On` → `CostSelected`；`Click` 的 `$cost:` → **toggle `Costs` 成员** + 清掉 `Cost`/`CostMax`（否则区间+集合叠成 AND）。
- **兼容路径**：`Costs` 为空 ⇒ 逐字走旧区间 ⇒ **既有 N10 那一族断言全绿**（`[8,8]` = 夹过 == 8 等）。
- **断言 3 条**（`DeckScene`）：费用选 `{1,8}` 两档 ⇒ 两档的卡**全在** · **两档之外一张不许混进来** · 只设 `Costs` ⇒ `IsEmpty` 为假。

---

## ③ `CardServices.CraftCard` / `UpgradeCard` —— **错误分支表（先给你看，没改代码）**

判据：`decomp_full/CardServices__CraftCard.c`（68 行）· `CardServices__UpgradeCard.c`（43 行）**逐行现读**。

### ③·1 表
| # | 方法 | 分支（条件） | 原版行为 | 我们现在做什么 |
|---|---|---|---|---|
| 1 | `CraftCard(card, onSuccess, onError)` | 入口 | `GetOriginalCard(card)`（**异画 → 原卡**）| 同（`Card.Id` 即原卡 id） |
| 2 | 同上 | **`InventoryManager.TryGetItem(...)` 返回 false**（这张卡**不在库存**里） | **`return`（静默，什么都不发生）** | 我们先判通配符、再扣、再 +1（**更严、且会出声**） |
| 3 | 同上 | 库存条目在 | 组 `DamageSignal(原卡 id, 某值)` → **`PlayfabWrapper.GenericCloudScriptHandler(900, payload, onSuccess, onError)`** | **本地直接成功**（无服务器） |
| 4 | `UpgradeCard(card, onSuccess, onError)` | 入口 | `GetOriginalCard(card)` | 同 |
| 5 | 同上 | **原卡为 null** | 落到方法末尾那个 **`FUN_1803f47a0()`（不返回）** = 崩/断言 | 我们出声 + 返回（**不照抄崩溃**） |
| 6 | 同上 | 原卡在 | 组匿名对象 → **`GenericCloudScriptHandler(0x385 = 901, payload, onSuccess, onError=param_3)`** | **本地直接成功** |

### ③·2 结论（**与 E 的措辞不同，请留意**）
- 原版这两个方法**没有本地业务错误分支** —— **所有判定（够不够通配符 / 货币够不够 / 是否到上限）都在 CloudScript 服务端**；
  本地只有 **2 条**「不走网络」的分支：**CraftCard 库存里没这条 ⇒ 静默 return** · **UpgradeCard 原卡为 null ⇒ 崩**。
- 所以「我们错误分支比原版简」这句的**实际内容**是：**我们把服务端的判定在本地模拟了**（通配符够不够、上限），
  而原版把它们交给服务器 ⇒ **我们不是「少了分支」，是「多做了一层本地校验」**。
- ⇒ **建议**：这一条按「**本机无服务器 ⇒ 用本地校验等价服务端**」判 **A（含如实差异）**，而不是当 C 去补「更多错误分支」。
  ⚠️ 唯一值得照抄的是 **#2 那条静默 return 的反面**：原版「卡不在库存 ⇒ 什么都不做」在我们这边无法发生（全解锁）；**保持我们现在的出声**（红线：不许静默）。
- **请你裁**：这条要不要从 C 改成 A（含差异）？我**没动代码**（按你说的「表先给我看」）。

---

## ④ 如实记录：本轮中途把 `DeckScene.cs` 弄红过一次（已修，现两行都是 0）

改「费用多选」断言时我用了一次**全局字符串替换**（`wantC`→`wantCostTmp` 等），结果**误伤了三个同前缀的既有变量**
（`wantC` → `wantCostTmp` 与既有 `wantCostTmp` 撞名 · `badCostPost`→`badCostTmpost` · `wantCells`→`wantCostTmpells` · `badCntVis`→`badCostTmpntVis`）。
- 症状：`DeckScene.cs(2036)` **CS0136 局部变量遮蔽**；另三处名字被改坏但**编译不报**（最危险）。
- 处置：逐行范围内重命名（**没有删外层变量**）+ 把误伤的三处**改回原名**；`grep CostTmp` 现在 **0 命中**；`typecheck` **运行时 0 / 编辑器 0**。
- 🔴 **教训（值得写进纪律）**：**改断言里的局部变量名，绝不全局替换** —— `wantC` / `badC` 这种短名会被 `wantCost…` / `badCost…` 吃掉。要按**行范围**改，改完 `grep` 旧名残留。

---

## ⑤ 下一步（等你两条裁决）
1. **chip 行**：走 (a) 等布局口径 / 走 (b) 网格算术（+ 溢出照不照抄）。
2. **`CardServices` 两条**：判 A（含「本地校验等价服务端」的差异）还是 C（补更多错误分支）。
3. 之后：`ToggleInvalid`（等「卡组行判无效」）· 继续补 C（收藏 68 / 商店 52 / 卡组编辑 18）。
