# E_信息页卡池（执行代理 `E-信息页卡池`，2026-10-11）

## 结论（一句话）

**「卡组信息」页右侧的卡池接上了** —— 卡池显隐判据从 `_tab == 0` 改成 **`_tab != 2`**，并抽成**唯一出处** `DeckRuntime.CardDisplayPage`；
**同一个原版入参管的第二条**（通配符计数条整条 = `DeckEditingWindow.wildcards`）**一并收口**（它原来恒显示、Cosmetics 页该关）。
类型检查两读数 **0 / 0**。

---

## 状态 → 参数 表（三页 × 卡池/卡背/通配符）

原版判据一句话：**`DeckEditingWindow.ToggleDisplay(param)` 的【一个】入参同时管三件** ——
`cardDisplay(0x78).SetActive(param)` · `cosmeticDisplay(0x80).SetActive(param ^ 1)` · `wildcards(0xd0).SetActive(param)`；
三个调用点里 `DeckEditingStateController__ToggleDeckInfo.c` / `__ToggleDeckList.c` 传 **1**、`__ToggleDeckCosmetic.c` 传 **0**。
（出处：`d:/2/tools/decomp_full/DeckEditingWindow__ToggleDisplay.c` —— **本代理自己重读过一遍**，与简报逐字一致；
两个调用点也是本代理自己 `cat` 的，不是转述。）

| 页签 | `_tab` | 入参 `param` | **卡池** `Card Display` | **卡背** `Cosmetic Display` | **通配符计数条** `wildcards` |
|---|---|---|---|---|---|
| `Cards` | 0 | 1 | **显示**（原版 / 改前我们 = 显示 ✅） | 关（原版 / 我们 ✅） | **显示**（原版 / 我们 ✅） |
| `Deck info` | 1 | **1** | **显示** ← 原版｜**改前我们 = 关 ❌ → 改后 = 显示 ✅** | 关（原版 / 我们 ✅） | 显示（原版 / 我们 ✅） |
| `Cosmetics` | 2 | 0 | 关（原版 / 我们 ✅） | 显示（原版 / 我们 ✅） | **关** ← 原版｜**改前我们 = 显示 ❌ → 改后 = 关 ✅** |

判据出处：
- 入参 → 三件：`DeckEditingWindow__ToggleDisplay.c`（`0x78` / `0x80` / `0xd0`，`param` / `param^1` / `param`）。
- 偏移 ↔ 字段名：`_probe_deckinfo.py bundle_menus_assets_all "Deck Editing Menu" --class DeckEditingWindow` 实读 ——
  `cardDisplay → GO「Card Display」` · `cosmeticDisplay → GO「Cosmetic Display」` · `wildcards → GO「WIldcard Counter」`。
- 三个调用点传值：`DeckEditingStateController__ToggleDeck{Info,List,Cosmetic}.c`（前两个 `…,1,0`、第三个 `…,0,0`）。
- 实体：`menu_dump.py bundle_menus_assets_all "Deck Editing Menu"` ——
  `2 Card Display`（1752.83×1009.06 @(167.2,71)，**act 空 = active**）· `2 Cosmetic Display`（同矩形，**INACT**）·
  `3 WIldcard Counter` 在 `Header` 下（`4 Background` / `4 Counters` / `4 Army Icon`）。
- ⚠️ **只覆盖一种情况说明**：上表是**本 build** 的判据。「切页签时那 0.3 秒滑动放不放」不属于本条（那是抽屉自己的事）。

---

## 改动清单（文件:行号 —— 改了什么 —— 判据出处）

文件只有一个：`d:/4/Unity/MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs`（**CRLF 原样保留**，见文末读数）。

| 行号（改后） | 改了什么 | 判据出处 |
|---|---|---|
| `:653` | 新增 `readonly List<GameObject> _cardsInfoOnly` —— 「Cards 页 + Deck info 页才显示」的那批件（≠ 已有的 `_infoOnly` / `_cosmOnly`，那两张是**单页**判据） | 与 `_infoOnly`（`:647`）同形状；本件是**两页**判据，塞进任一张单页表必错一页 |
| `:1100-1101` | `hdr_wcbg` 由 `Img(...)` 改成 `var wcBg = Img(...)` + 登记进 `_cardsInfoOnly` | `wildcards(0xd0).SetActive(param)`；原版这一整条在**同一个 GO**（`WIldcard Counter`）底下 |
| `:1109-1110` | 4 颗 `hdr_wc{i}` 同理登记 | 同上（`WIldcard Counter/Counters/*/Icon`） |
| `:1123-1127` | 4 颗 `hdr_wct{i}` 的 `if` 块内加一行登记 | 同上（`…/*/Counter`） |
| `:1171-1172` | `hdr_army` 同理登记 | 同上（`WIldcard Counter/Army Icon`，父链现核） |
| `:1949-1957` | **本活的正主**：`bool on = _tab == 0 && idx < all.Count;` → **`bool on = CardDisplayPage && idx < all.Count;`**（旧记录「卡池只在 Cards 页出现」一并作废） | `cardDisplay(0x78).SetActive(param)`；实体 `Content Area/Card Display` 出厂 active、`Card Display > Scroll View` 视口 (330.2,156)-(1920,1080) |
| `:4472` | 新增属性 **`bool CardDisplayPage { get { return _tab != 2; } }`**（唯一出处，doc 里写全判据 + 明说 `Card Filters` 还没接） | 同上（原版那一个入参） |
| `:4480-4484` | `RefreshTabVisibility()` 里加 `foreach (var go in _cardsInfoOnly) …SetActive(CardDisplayPage);` | 同上；与 `_infoOnly` / `_cosmOnly` **同一处**落显隐 |

**没有改**的：侧栏（`_infoOnly` 仍是费用曲线那一批 9×4 + `curve_bg`，原版 Info 页侧栏 = `DeckEnergyCostDrawer` + `DeckTypeCounterDrawer` 行，见「顺手发现 ①」）；
`Editor/` 下任何文件；`CardDisplay` 的另两个子件（见「没查清 / 顺手发现」）。

### 命中区与显隐同谓词（本仓口径的那道闸）

**卡池不需要额外改**，因为它的画 / 点 / tooltip / 自检读数**本来就共用一个来源**：`RefreshPool` 写 `_poolIndex[vi]` 与 `SetActive`，
其余三处全部**读**它 ——
`:3659`（`HandlePoolClick`：`v == null || !v.gameObject.activeSelf || i >= _poolIndex.Count || _poolIndex[i] < 0` → `continue`）·
`:759`（`TickTooltipAt`：`!v.gameObject.activeSelf` → `continue`）· `:4594` / `:4705`（自检口 `UiPoolCardAt` / `UiPoolCellRect`）。
⛔ **没有第二处写卡池显隐**（全文件 `_poolViews[…].SetActive` 只在 `RefreshPool` 的三处，其中两处是收尾）。
⇒ 改一个谓词，四条路一起对齐。

`KeyLive`（`:6445`）**不涉及**卡池 —— 它只管 `_btns` 里登记的按钮，卡池那批卡**不进 `_btns`**。

---

## 同一判据的所有消费者（逐个列出）

判据 = **原版 `cardDisplay(0x78).SetActive(param)` 这一个入参**（「`Content Area/Card Display` 这一整页在不在」）。
⚠️ 简报让我逐个判的那 4 处 `_tab`，**没有一处是卡池判据**：

| # | 位置 | 它管的是 | 改 / 不改 | 为什么 |
|---|---|---|---|---|
| 1 | `:1957` `RefreshPool` | **卡池**（正主） | **改**（`_tab == 0` → `CardDisplayPage`） | 原版两页同一棵，见上表 |
| 2 | `:4484` `RefreshTabVisibility` | **通配符计数条整条**（新增 `_cardsInfoOnly`） | **改** | 原版 `wildcards(0xd0)` 与 `cardDisplay` **同相**（同一次调用） |
| 3 | `:1100`/`1109`/`1126`/`1171` `BuildHeader` | 上一条那 9 件的**登记点** | **改**（登记） | 我们外层没有那格容器，只能逐个登记 |
| 4 | `:3053`（原简报 `:3026`）`RefreshDeckList` 的 `bool cards = _tab == 0` | **卡组行** | **不改** | 原版 `DeckEditingPanel.ToggleDeckInfo.c`：`deckList(0x28)→0`、`deckCostDrawer(0x38)/deckCounterDrawer(0x40)→1` ⇒ **Info 页卡组列表是关的** |
| 5 | `:3189`（原 `:3162`）`RowUnder`（行悬停） | **卡组行** | **不改** | 同上（行不显示 ⇒ 悬停无意义） |
| 6 | `:3678`（原 `:3651`）`HandleDeckRowClick` | **卡组行** | **不改** | 同上 |
| 7 | `:4601`（原 `:4550`）`UiDeckRowAt` | **卡组行**（自检读数） | **不改** | 同上 |
| 8 | `:5259` `HandleScroll` 的 `else if (_tab == 2)` | 卡背网格的滚轮 | **不改** | 那是 `Cosmetic Display` 那一半，判据是 `param ^ 1` |
| 9 | `:3363` `RefreshHeader` 的 `_tab == 2 ? _cosmoFltOpen : _filtersOpen` | 页头 `Filters` 钮按谁的状态换图 | **不改** | 原版两页的 `filterToggle` 就是**同一颗**（A24 已核），判据不同（逻辑态不是页判据） |
| 10 | `:5189`/`:5197`（`ToggleFilters` 的 `_tab == 2` 分派） | 两套筛选抽屉各自的内容 | **不改** | 同前一格 |

⚠️ **同一入参的另两个消费者【还没接】，如实列出**（本代理**没改**，理由见「没查清 / 顺手发现」②③）：
- **`Card Filters` 抽屉**（`:1702` `_fltSlide`）—— 原版它是 `Card Display` 的**孩子**（`menu_dump` 树：`3 Card Filters` 在 `2 Card Display` 下），
  整棵跟着 `SetActive` 关，**在 Cosmetics 页上是不画出来的**；我们那句 `_fltSlide.PageOn` **从来没被赋过值**（恒 `true`），
  只有 `_cosmoFltSlide.PageOn = (_tab == 2)`（`:5853`）那一半被接了。
- **`Card Drag Controller`**（`CardDraggingController` / 我们挂在 `Root` 下）—— 原版它是 `Card Display` 的孩子（父链现核，见「顺手发现 ③」）。

---

## 现读的版面参数（那份 RSR —— 本代理现读，附出处）

**`Deck Editing Menu > Content Area > Card Display > Scroll View`**
（GO pid `-6923878454973042908` ⇔ 组件在 `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_345129740864909092.json`，
类 `PolyAndCode.UI.RecyclableScrollRect`）：

| 字段 | 值 |
|---|---|
| `_segments` | **4** |
| `_controlSegmentSize` | 1 |
| `_spacingX` / `_spacingY` | **0** / **0** |
| `_useFixedCellSize` | 1 |
| **`_cellWidth`** | **262.5** |
| **`_cellHeight`** | **384** |
| `_mobileSizeScale` | **1.5** |
| `_poolSize` | 0 |
| `IsGrid` / `SelfInitialize` / `Direction` | 1 / 0 / 0 |

**列数不是 `_segments`** —— 照原版那条式子 `_coloums = floor(viewport.rect.width ÷ (_cellWidth + _spacingX))`
（`PolyAndCode.UI.VerticalRecyclingSystem__CreateCellPool.c` L127-167）⇒ `floor(1589.78 ÷ 262.5) = **6**`，
与我们 `Pool.Cols` 现算一致（本文件三处注释 `:2129` / `:2150` / `:4951` 已经写着「**不是** `_segments=4`」）。
⇒ **本读数与我们的实现逐项吻合，不需要动任何版面常量。**

（顺带现读的两份，供对照，**均未使用**：
`Deck Editing Menu > Cosmetic Display > Scroll View` = `_segments 4` · `_cellWidth 250` · `_cellHeight 405` · `_mobileSizeScale 1.2` · spacing 0
—— 与我们 `CosmoCellW/H = 250/405`（`:2149`）吻合；
`Content Area > Card Filters` 的 rect = (2.2,156)-(333.9,1080) = 331.73 宽，与我们 `FltW = 331.7`（`:465`）吻合。）

---

## 没查清的部分

1. **`_segments`（原版私有字段）到底是什么语义** —— **没查清**。已知：它**不是**列数（上面那条式子与三个不同界面的实测都排除了）。
   还差：`PolyAndCode.UI.RecyclableScrollRect` 里读 `_segments` 的那个消费点的**方法体**（`decomp_full` 里没找到 `__*` 产物）。
   ⛔ 本报告**不填**猜测。
2. **`Card Filters` 抽屉在 Cosmetics 页上的行为**要不要一起接 —— **判据齐了**（父链 + 同一个 `SetActive(param)`），
   但**没查**的是「接上之后会不会撞 `Editor/DeckScene.cs` 里那些抽屉断言」（A67 / A24 / 2026-10-10 A224② 那一大片）——
   本代理**不许跑 Unity**、也**不许碰 `Editor/`**，所以**没有复核**。
3. **切页签时原版对筛选抽屉放不放那 0.3 秒动画** —— 沿用文件里既有的「判不出来」结论（`:5844` 那段 A67 的如实标注），本代理没有新证据。

---

## 我建议加的断言（逐条；我自己不写 —— `Editor/` 是主对话独占）

判据一律**不读我们的常量**，读原版那个入参的三条分支。

1. **卡池：Info 页显示、Cosmetics 页关**（正主）
   ```
   _rt.UiSetTab(1);  CheckTrue(_rt.UiPoolCardAt(0) != null, "Deck info 页：卡池第 0 格有卡");
                     CheckTrue(_rt.UiQuadActive("pool_0"), "Deck info 页：卡池那棵树真的亮着");
   _rt.UiSetTab(2);  CheckTrue(_rt.UiPoolCardAt(0) == null, "Cosmetics 页：卡池关");
                     CheckTrue(!_rt.UiQuadActive("pool_0"), "Cosmetics 页：卡池那棵树真的关了");
   _rt.UiSetTab(0);  CheckTrue(_rt.UiQuadActive("pool_0"), "切回 Cards：卡池回来");
   ```
   ⚠️ **两条一起断**（`UiPoolCardAt` 读状态、`UiQuadActive` 读那棵树）—— 与既有的 `row_0` 那一对（`DeckScene.cs:2737`/`2755`）同形，
   理由一样：只断状态的话，「状态对了、树没关」照样绿。
2. **通配符计数条：Cosmetics 页整条关**（同一入参的第二条，**一件都不能漏**）
   ```
   foreach (var k in new[] { "hdr_wcbg", "hdr_army", "hdr_wc0", "hdr_wc3", "hdr_wct0", "hdr_wct3" })
       CheckTrue(!_rt.UiQuadActive(k), $"Cosmetics 页：`{k}` 属于 `WIldcard Counter`，整条一起关");
   _rt.UiSetTab(1); /* 逐件反向断一次：Info 页必须都亮着 */
   ```
   ⚠️ `hdr_wct*` 是 **Label**，`UiQuadActive` 量不到它（`_named` 只装 `ImageQuad`）—— `hdr_wct*` 那四条要用
   `_rt.UiWcCounterRect(i, out …)` 或先 `FindDeep(_rt.Root, "hdr_wct0")` 再 `.activeInHierarchy`。
   **建议用后者**（与「量的是谁的」无关，纯看开关）。
3. **灭自证那条**（防「两边一起改回旧写法」）：上面第 1 条必须**至少有一页是「显示」**、
   **至少有一页是「关」**（三页全断）。只断 Cosmetics 关的话，一个 `_tab == 0` 的旧实现照样全绿
   —— 那正是本活要治的那个 bug。
4. （可选）**Info 页上点得到卡池**：`_rt.UiClickPx(<某格中心>)` 之后 `_rt.State.Deck.CountOf(def)` 变了 —— 钉住「画出来了也点得到」。
   ⚠️ 期望值从 `_rt.UiPoolCardAt(view)` 那一张卡**现算**，⛔ 别写死卡名。

---

## 顺手发现（看见了但**没改**）

① **Info 页侧栏少一件：`DeckTypeCounterDrawer`（原版 `Deck Counter Drawer`，(0.3,761)-(335.6,861) 335.31×100，出厂 INACT）。**
   证据：`menu_dump.py bundle_menus_assets_all "Deck Editing Menu"` 那行；反编译 `DeckEditingPanel__ToggleDeckInfo.c`：
   `deckList(0x28)→0`、`deckCounterDrawer(0x40)→1`、`deckCostDrawer(0x38)→1`、`deckCosmeticDrawer(0x30)→0`。
   我们现状：`_infoOnly` 里**只有费用曲线那 9×4 + `curve_bg`**（`BuildCurve` `:1482`-`:1514`）。
   搜过的词（全工程 `CardPresentation/**/*.cs`）：`DeckTypeCounter` / `DeckCounter` / `CounterDrawer` / `deck_counter` /
   `stratagemsCount` / `troopCount` / `typeCounter` —— **零命中**；`find -iname "*counter*"` 只有三份 `40K_main_deck_card_counter.png`（那是卡池的张数条底图）。
   ⇒ **判定：这件本地没实现**（不是「没找到」）。
   ⛔ 简报说「⛔ 别动侧栏」⇒ **本代理没动**，只报。

② **`Card Filters` 抽屉没有页判据**（同一入参的另一个消费者）。
   `_fltSlide.PageOn` **全文件只被读过、从没被赋过值**（`:1763` / `:1773` 读；赋值的只有 `:5853` 那一句 `_cosmoFltSlide.PageOn`）
   ⇒ `_filtersOpen == true` 时切到 Cosmetics，`Card Display` 那一整页（含 `Card Filters`）在原版**不画**，而我们**照画**；
   连带 `FltStripOn`（`:1801`）也不含页判据 ⇒ 那一刻左侧那一条带会被**卡牌筛选**吃掉，而不是卡背筛选。
   本代理**没改**：它落在 `ApplyDrawerSlide` / `RefreshFilters` 这一族上，`Editor/DeckScene.cs` 有一大片抽屉断言（A67 · A24 · A224②），
   我**跑不了自检**、**不能改 `Editor/`** ⇒ 按简报红线「需要改白名单外的判断时停手写进报告」处理（改动本身在白名单内，但**验收**不在）。

③ **一处代码注释与 prefab 事实不符**（`DeckRuntime.cs:840`）：
   现写「（原版：`Content Area > Card Drag Controller` 是 `Card Display` 的**兄弟**，不是它的孩子）」——
   **实测是它的孩子**：`GameObject/Card Drag Controller.json` 的 RectTransform `-3300583987145543900` 的 `m_Father` = `7685107495543959322`，
   后者属于 GO `Card Display`（本包同名只有 1 个实例）。`menu_dump` 的缩进也是 `2 Card Display` → `3 Card Drag Controller`。
   ⇒ 我们把它挂在 `Root` 下（当兄弟）**行为上等价**（卡池/卡组行都在别的页关着，起不了拖），但**注释那句是错的**（铁律 5 该就地改）；
   本代理按简报 ⑨「顺手发现不许自己改」**只报不改**。

④ **Info 页上滚轮滚的是「藏起来的卡组列表」**：`HandleScroll` `:5260` 那句 `else if (px.x < SideBgX + SideBgW)` 不含页判据
   ⇒ Info 页把滚轮放在侧栏那 335px 上，滚的是**不显示的** `_deckScroll`（原版那一刻那个 ScrollRect 是不激活的）。
   后果**不可见**（滚的是隐藏件），是个**惰性缺陷**；⚠️ 注意卡池 x 从 **330.2** 起、侧栏到 **335.6** ⇒ 有 **5.4px** 的重叠带归侧栏那条分支。
   本代理**没改**（不在本活范围内，且碰它会动滚轮分派）。

⑤ **`_segments = 4` 这个「看着像列数」的值已经在三个界面各出现一次**（本窗卡池 4 · 本窗卡背 4 · 收藏窗卡池 4；
   而 `Deck Selection Popup` 与 `Select Deck Tab` 那两份是 6 / 5）—— 我们的代码里已有三处注释钉着「**不是** `_segments`」，
   本报告只是**再确认一遍**这个读数，⛔ 不是新结论。

---

## 类型检查读数

```text
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```text
命令：`TMPDIR=/tmp/wf_e bash d:/4/Unity/工具/typecheck.sh`（**改完即跑**；不算「自检」）。
⚠️ 那一趟**别的代理也在改文件**，但这一次两读数都干净（没有「错在别人文件上」的情况）。

行尾/规模读数（`io.open(p,'rb')`）：
`DeckRuntime.cs` **CRLF 6829 → 6880 行**（`b.count(b'\r\n') == b.count(b'\n') == 6880`，**整篇仍是纯 CRLF**）；
`git diff --numstat` = **58 / 7**（⚠️ 若用 `sed -i` 或文本模式写，这个数会变成整个文件行数）。
`DeckEditorState.cs` **一个字都没动**（本活没用到它 —— 池内容来自 `State.VisibleCards()`，与页签无关）。
**⛔ 没有跑任何 Unity 自检**（红线 1）；**没动 git**（红线 2）。
