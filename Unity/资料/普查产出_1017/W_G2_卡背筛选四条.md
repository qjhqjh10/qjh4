# W_G2 · 卡背筛选四条（D39 / D40 / D41 / D42）

写手代理产出 · 2026-10-17 · **未跑 Unity**（按施工单「断言写好不跑」）；类型检查两行 0 错。
施工单 = `资料/普查产出_1017/对账_卡组编辑部分_差异与待办.md` §十 那四行。**四条全落地。**

## ① 四条逐条

| # | 项 | 原版 | 我们（改前） | 改成 | 判据出处 |
|---|---|---|---|---|---|
| **D39** | 卡背抽屉 Army 行 `Content` 的 `m_Spacing.y` | **20** ⇒ 行高 50+5×100+4×20 = **630** | 复用卡牌那套（0）⇒ **550** | 卡组编辑显式传 **20**（收藏窗缺省仍 0） | `menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 20 --md` ⇒ `…/Cosmetic Display/Cosmetic FIlter/Filters/Army Filter/Content` = `**【GridLayoutGroup】** cellSize=100×100 spacing={'x': 7.0, 'y': 20.0} pad=14,0,0,0` |
| **D40** | 卡背抽屉 `Owned Toggle` 行顶（绝对） | **813.81** = `FltY` 156 + 15 + **630** + 12.81 | **733.81**（= 156+15+550+12.81） | 由 D39 带出，改一处两处同时对上 | 同 ①（行高是唯一变量；两条算式各自落在 630 / 813.81，互为旁证） |
| **D41** | `Owned Toggle/Image` 宽 | `sd(**80**,0)` ⇒ **80**（228.90→308.90，右缘 = `w−25`） | 锚点式 `0.3w−30` = **69.52**（左缘比原版右 10.48） | 卡组编辑传 **80**（收藏窗缺省仍走锚点式） | `menu_dump.py … "Deck Editing Menu" --depth 20 --md` ⇒ 该行 `锚(1,0)-(1,1) apos(-25,0) sd(80,0)` |
| **D42** | `Owned Toggle/Label` 宽 | `sd(**230**,0)` ⇒ 25..**255** | 锚点式 25..`0.7w` = 25..**232.21**（右缘 −22.79） | 卡组编辑传 **230**（收藏窗缺省仍走锚点式） | 同上 ⇒ 该行 `锚(0,0)-(0,1) apos(25,0) sd(230,0)` |

**收藏窗那份（= 我们现在这套）本来就对，一个字没动** —— `menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 22 --no-sprite`：
`Cardback Tab/…/Cosmetic FIlter/Filters/Army Filter/Content` = `spacing {'x':7.0,'y':0.0}` · `Owned Toggle (1)/Image` 239.9→310.6（宽 70.65 = `0.3×335.5−30`）· `Label` 25.1→234.9（= 25..`0.7×335.5`）—— **两颗都是锚点式**。

**唯一性自证（全包逐实例枚举 `bundle_menus_assets_all`，改动前后都可复跑）**：
- `m_CellSize` 100×100 的 **9 颗**里 `spacing.y = 20` **只有 1 颗** = `MonoBehaviour_7231577424604229412`，父链 = `Content < Army Filter < Filters < Cosmetic FIlter < Cosmetic Display < Content Area < **Deck Editing Menu**`（其余 8 颗 = 7×`(7,0)` + 1×`(5,0)`）。
- `m_SizeDelta = (230,0)` 全包 **只有 1 颗**，父链同上。
- `(80,0)` 有 **3 颗**：卡背抽屉那颗 `Image` + 两棵 `Header/Army Icon` ⇒ **这一条不能只按 `sd` 认，要连父链一起看**。

## ② 改动清单

1. `Core/FilterPanelModel.cs`（LF，+78/−14）
   - `ArmyRowH(int, float spY = 0f)`：末尾加 `(rows−1)*spY`（`rows ≤ 1` 不加）。**缺省 0 ⇒ 原行为逐位不变**（卡牌筛选那七行的 `ComputeLayout` 走缺省）。
   - 新增三个「卡组编辑档」常量：`CosmoArmySpYDeckEdit = 20` · `CosmoIconWDeckEdit = 80` · `CosmoLabWDeckEdit = 230`，并入 Cosmo 那段长注释（两棵树对照 + 三条判据）。
   - `CosmoArmyRowH` / `CosmoOwnedTop` / `CosmoContentH` 各加可选 `armySpY = 0f`。
   - `ToggleRowRects(..., float iconW = 0f, float labW = 0f)`：`> 0` 用固定宽，否则走原锚点式（右缘 `w−25` / 左缘 25 两种算法相同）。
   - `BuildCosmetics(..., float labelAutoMin = 18, float armySpY = 0f, float iconW = 0f, float labW = 0f)`；Army 格行距改 `(ArmyCell + armySpY)`。
2. `Deck/DeckRuntime.cs`（CRLF）—— **只在调用点**加那三个实参（**具名实参**：四个形参全是 `float`，调换编译器不报）（`labelAutoMin:` / `armySpY:` / `iconW:` / `labW:`），并把 `RefreshCosmoFilters` 头注释里过期的「全高 ≈ 628」改成 **707.8**（= 15+630+12.81+50，铁律 5）。
3. `Editor/DeckScene.cs`（CRLF，本节 +85 行）—— **只加断言，一节**：卡背抽屉那节（`Shoot("deck_cosmo_filters.png")` 之后、`// ---- 真的筛了没有` 之前）。
   - **实拍 2 条**（走真实调用点，量**建出来**那两行 ⇒ 顺带管住「实参有没有传进去」，模型层探针管不到那一格）：最后一行 Army 格中心 `(66.2, **751.0**)` 100×100 · `$owned` 行 `(168.05, **838.81**)` 331.7×50。（`m_Spacing.y` 退回 0 时 = 671.0 / 758.81。）
   - **模型层 10 条**：两组探针（缺省档 / 卡组编辑档，四个原版字面量 `26/20/80/230` 显式传，共用 `w=331.7`）⇒ 收藏窗 577.81·69.51·306.7·25·232.19；卡组编辑 657.81·80·306.7·25·**255**。
   - **★ 判别式 4 条**（把卡组编辑那套改回缺省 ⇒ 四条差全变 0 ⇒ 必红）：行顶差 **80** · `Image` 左缘差 **10.49** · `Label` 宽差 **22.81** · Army 行高差 **80**。
   - 期望值一律**原版字面量**，不从新常量读。两个 `else Check(true,false,…)` 是「量不到就出声」的兜底（与本节既有风格一致）。

## ③ 没查清 / 如实标注的部分

- **「两棵树这颗的锚法不同」只落到矩形上**：`Cell` 里只有矩形，所以 D41/D42 表达成「右缘仍 `w−25` + 固定宽 80/230」。原版那两颗的 `anchorMin/Max`、`pivot` **没有**搬进模型。⚠️ 若将来面板宽可变：卡组编辑那颗 `Image` 的右缘该是「固定 308.9 绝对」还是「`w−25`」，**两棵树在 `w=331.73` 处重合、分不开**（未查）。
- `(80,0)` 那三颗里另两颗（两棵 `Header/Army Icon`）**只核了父链与本单无关**，没有再看它们的锚点。
- **断言没验过红绿**（施工单：不跑 Unity）。「改回缺省必红」是按算式推的，不是跑出来的。
- 顺带发现（**未改，越界**）：`UiCosmoFilterCell` / `RefreshCosmoFilters` 的绝对 y 走 `FltAbs`，而它减的是**卡牌抽屉**那个 `_fltScroll`；卡背抽屉自己没有滚动区 ⇒ 若哪天 `_fltScroll ≠ 0`（现在自检每段收尾都归 0），卡背那两行会被「别人的滚动量」推走。本单只改几何、没碰它。

## ④ 类型检查结果（原样贴）

```
$ TMPDIR=/tmp/wf_g2 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

`git diff --numstat`（改完即核，**行尾未翻**；⚠️ 这三个文件的 numstat 总数含**别的代理**未提交的改动 —— 本次只动上述几处）：`FilterPanelModel.cs` 纯 LF（753→772 行）· `DeckRuntime.cs` 5289/5289 全 CRLF · `DeckScene.cs` 4736/4736 全 CRLF，bare LF 均 0）。
