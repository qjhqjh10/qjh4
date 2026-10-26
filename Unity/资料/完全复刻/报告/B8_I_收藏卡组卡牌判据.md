# B8_I · 收藏 / 卡组编辑 / 卡牌面-渲染 —— B 行判据 + C 行定级

> 2026-10-26 · 只读判据代理 **B8_I** · 对象 = 三个 P0 域主表里全部 **B 行 59 条** + **C 行 36 条**。
> 产物：`判据/B8_收藏_卡组_卡牌_B行判据.tsv`（59 行）· `判据/B8_收藏_卡组_卡牌_C行定级.tsv`（36 行）。
> ⛔ 没跑 Unity / 没动 git / 没改任何 `.cs` / 没动 `账/*.tsv`。只读工具用过 `check_ledger_points.py` 与 `ledger_slice.py`。
> **判据序**：🥇 `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/**`（唯一有真方法体的 cpp2il 产物）→ 🥈 `D:/2/tools/decomp_full/**.c` → 解包 prefab/MB 字段 → `D:/2/tools/il2cpp_out/dump.cs`（签名与嵌套类归属）。
> **不重复查**：`判据/收藏_未读132条_第2轮.md` · `判据/共用件_过滤模型_B升A.md` · `报告/R17_收藏收尾音效与断言_第2轮.md` · `报告/G6_第5轮.md` 的结论**全部照用**，本轮只在它们之上补「还差什么 / 落点真值」。

---

## 〇 两条口径先说清（不读这两行会误用产物）

| # | 事 | 说明 |
|---|---|---|
| 1 | **`账物理行号` = 真·文件行号（表头算第 1 行）** | ⚠️ `工具/ledger_slice.py` 打印的「物理行」**是 真值 − 1**（它 `enumerate(raw.split('\n'))` 从表头的 i=0 起算，文档却写「1 = 表头下一行」）。本产物一律用**真实行号** ⇒ 与 `ledger_slice` 的数字**差 1**。改账/编辑定位请用本表的数字。 |
| 2 | **`D` 的判法** | 方案 §4.1 的 D 定义只列「离线不可用 / 死代码 / 纯编辑器」，但 **D 的规则段**明确允许「② 我们对应用了 X 代替它 + 说明语义覆盖」。同族先例（`ResetRelayTarget` / `AutoRemoveCanvasSorting`）就是按 ② 判的 D ⇒ 本产物沿用。**凡判 D 的行，备注里都写清替代物与语义覆盖**；调度台若要收紧口径，这些行退回 B 即可（不影响其余结论）。 |

---

## 一 结论

### 1.1 一句话

**59 条 B 行里只有 4 条是真缺口（→C），20 条现在就能判 A（全部零代码，只改账），29 条是「原版本身没有 / 我们换了机制」该判 D。** C 行 36 条里 **6 条要改**（2 条其实是零调用 ⇒ 该 D · 1 条等级原本是空的 · 2 条该升 P0 · 1 条该升 P1）。

### 1.2 B 行去向（59 条）

| 域 | B 行数 | → **A** | 留在 **B** | → **C**（真缺口） | → **D** |
|---|---:|---:|---:|---:|---:|
| 收藏 | 33 | **8** | 3 | **2** | **20** |
| 卡组编辑 | 15 | **11** | 2 | 0 | **2** |
| 卡牌面/渲染 | 11 | **1** | 1 | **2** | **7** |
| **合计** | **59** | **20** | **6** | **4** | **29** |

- **A 20 条**全部是「**零代码、只改账**」（改账 = 改 `账/*.tsv` 的 `状态` / `我们的落点` / `判据要点` 三列）：
  · **8 条收藏过滤族** —— E 侧早已核完判 A（`判据/共用件_过滤模型_B升A.md`），账里还挂着「等 E 侧核完」的占位；
  · **11 条卡组编辑** —— 行为已逐句核过一致，只差把落点写成**行号范围** + 一句「这段如何分别满足」；其中 **7 条还须先改正已漂 23–52 行的行号**（真值见 TSV 第 7 列）；
  · **1 条卡牌面** —— `CardCollider2D.Toggle` 的账注记本身写错了（`SetHitEnabled` 已在，逐句等价）。
- **真缺口 4 条**：`CardCollectionTab.ToFocus`（切页签不重灌）· `ItemDrawer.OnDestroy`（句柄表只增不减）· `CardTextCountersController.HideStat` / `ResetHide`（数值位该印 `?`，我们关层）。
- **留 B 6 条**：`ItemDrawer.Draw` sealed override（11 跳读不到）· `ItemDrawer.Draw(T,int)`（23 个子类）· `ItemDrawer.GetItemName`（23 个子类）· `CardHighlight.Start`（落点 `:4176` 已不在 Build 内）· `DemoDeckInfoSO.GetName` / `PrebuiltDeck.HasHiddenCards`（落点只能落在**多行属性**上，方案明令不许）。

### 1.3 C 行定级（36 条）

| 域 | C 行数 | 保持现级 | 建议改级 |
|---|---:|---:|---|
| 收藏 | 8 | 5 | **3**（L244 / L247 → **D**（零调用，不是待办）；L273 **等级原本是空的** → P2） |
| 卡组编辑 | 24 | 22 | **2**（L285 / L286 P1 → **P0**） |
| 卡牌面/渲染 | 4 | 3 | **1**（L21 P2 → **P1**） |
| **合计** | **36** | **30** | **6** |

分布（现读 → 建议）：P0 **4 → 6** · P1 **10 → 9** · P2 **21 → 19** · 空 **1 → 0** · D **0 → 2**。
工作量（本轮逐条给）：**小 18 · 小-中 2 · 中 11 · 大 3 · 零/不做 2**（大 3 = FilterMethods 两条引擎口径 + `CardImageController.ToggleMask` 搬 SoftMask）。

---

## 二 按域交叉表

### 2.1 收藏（B 33 → A8 / B3 / C2 / D20）

| 桶 | 行（真·行号） | 一句话 |
|---|---|---|
| **A（8）** | 137 · 140 · 141 · 142 · 311 · 316 · 317 · 318 | **过滤族全员**：`判据/共用件_过滤模型_B升A.md` 早判 A（#1/#2/#5/#6/#9/#13/#16/#17），账里还挂着「等 E 侧核完」的占位 ⇒ **纯改账**。⚠️ 同时**订正两句账注记**：①「`withAnimation` 我们恒无动画」不成立（抽屉就是 0.3s 动画）② `ToggleCollectionFilter` 那几条不是「通用件所以不查」，是「已核完」。 |
| **D（20）** | 203 204 205 207（`GameModesIcon*` 4 条：3 条**全库 0 调用**、1 条机制已替代）· 263 266 270 275 277 279 283 285 288 294 297（**抽象声明 11 条，无体**）· 105 108（`CollectionCard` 生命周期 2 条，我们不做 cell 池）· 342 343（`WhereToGetCardsSection`，**那扇根本没有抽屉**）· 32（`OnCollectionUpdate`，整页重建代替） | 详见 TSV 备注；每条都写了替代物或零调用证据。 |
| **C（2）** | **47** `CardCollectionTab.ToFocus` · **272** `ItemDrawer.OnDestroy` | 见 §三 发现 5、6。 |
| **B（3）** | 265（Draw sealed override，11 跳读不到）· 267（泛型抽象 Draw，23 子类）· 269（GetItemName，23 子类） | 都要「读到方法体内部」才能继续，工作量中。 |

### 2.2 卡组编辑（B 15 → A11 / B2 / D2）

| 桶 | 行 | 说明 |
|---|---|---|
| **A（11）** | 137 · 139 · 227 · 228 · 229 · 231 · 232 · 233 · 243 · 275 · 282 | G6 第5轮 G0–G8 已判「行为一致」，**卡住的只是落点形态**（§4.1：共享落点必须给**行号范围** + 说清这段如何分别满足各方法）。其中 **139 / 227 / 228 / 229 / 231 / 232 / 233 的落点行号已漂 23–52 行**（真值见 TSV 第 7 列，已逐条给出）。 |
| **D（2）** | 183 `DeckListDrawer.ConfigPrefab` · 230 `DeckSelectorMenuItemDemo.OnDestroy` | G6 自己的 G2 就写着「**无独立实现行可指**」（原版是「藏行模板 prefab」/「退订按钮监听」，我们没模板、格随重建整棵销毁）⇒ 该判 D。 |
| **B（2）** | 252 `DemoDeckInfoSO.GetName` · 278 `PrebuiltDeck.HasHiddenCards` | 落点只能指到**多行属性**（`PrebuiltDecks.DisplayName:118` / `Tab:337`），而方案 §4.1 明令「纯属性 getter 行不得当方法落点」⇒ 需要一个**一行门面方法**（或在生成期那一段给落点），代价一次。 |

### 2.3 卡牌面/渲染（B 11 → A1 / B1 / C2 / D7）

| 桶 | 行 | 说明 |
|---|---|---|
| **A（1）** | 29 `CardCollider2D.Toggle(bool)` | `CardView.cs:9340 SetHitEnabled` **逐句等价**（`_hitCollider.enabled = on` + `if(!on) ShowInBigColliderEnabled = false`）。账写「无逐方法对应」是错的。 |
| **B（1）** | 44 `CardHighlight.Start` | 现落点 `CardView.cs:4176` **已不在 Build 里**（`void Build(CardData d)` 声明在 **:4191**）⇒ 先收紧落点。 |
| **C（2）** | **70 `HideStat`** · **72 `ResetHide`** | 见 §三 发现 3。**同一件活**。 |
| **D（7）** | 124 126 127 129 130 131 134（`TextTooltipController` 的 uGUI 事件层） | 我们是全局悬停驱动（`BattleDriver.TickTooltipAt`，判据 = TMP `FindIntersectingLink`）+ 两级开关（`Tooltip.SuppressTooltips` / `CardView.TooltipsEnabled`）⇒ 这一层不建。⚠️ 同族**确有一条真差异** = C 行 L120（tooltip 锚在关键词上、不跟鼠标），别混。 |

---

## 三 最值钱的发现（按价值排）

### 发现 1 🔴 8 条收藏 B 行，E 侧早就核完判 A，账没回填 —— 改账即 +8 A

`判据/共用件_过滤模型_B升A.md` §3 小结原话：「**A：10 条**……**B：0 条**」。而 `账/收藏.tsv` 里对应的 8 行仍写 B + 备注「共用件 ⇒ 保持 B，由 E 侧一次核完」。
⇒ 这是**账与判据不同步**，不是缺实现。**零代码、纯改账**。同类「占位没回填」还会在别的域存在，建议调度台批量扫一遍「备注里写『等 X 侧核完』的 B 行」。

### 发现 2 🔴 卡组编辑 15 条 B 里 9 条落点已漂 23–52 行；而 `check_ledger_points.py` **只查 A 档**

实测（本轮现读 `Shell/PracticeModePopup.cs` 的声明行，并跑过只读闸）：

| 账行 | 账里落点 | 声明**真值** | 漂 |
|---|---|---|---|
| 139 | `:2378（OpenDeckSelection）` | **:2401** | −23 |
| 183 | `:2423（BuildCardRows）` | **:2456** | −33 |
| 227/232/233 | `:2083（BuildDeckCell）` | **:2106**（:2083 现在是 `ShowYourDeckCell` 的 `{`） | −23 |
| 228 | `:2604（PickDeck）` | **:2656** | −52 |
| 229 | `:2693（PickPrebuilt）` | **:2745** | −52 |
| 230 | `:1922（RebuildDeckRows）` | **:1945** | −23 |
| 231 | `:2654（PickPair）` | **:2706** | −52 |

同时跑闸结果（只读）：`收藏 219 通过 / 卡组编辑 231 通过 / 卡牌面 109 通过 · 漂移 0 · 失败 0` —— **这个「0 漂移」只覆盖 A 档**。B/C 档的落点没有任何机检，写成什么样都没人管。
⇒ 建议：把闸扩成 `--all-status B,C`（或在合并账时对 B/C 档也跑一次）。

### 发现 3 🔴 真缺口：敌方埋伏单位的数值位，原版印 **"?"**，我们**直接关层**（空白）

- 调用链**已现读坐实**：`CardScript.SetAmbush(true)`（`CardScript.cs:14214`）→ `BattleCardUI.ToggleStats(false)`（`BattleCardUI.cs:3065`）→ health/armour/melee/range/cost 各 `HideStat()`；`CardDisplayWindow.ShowBattleCard` 对敌方埋伏卡走同一条（`CardDisplayWindow.cs:674`）。
- `HideStat`（`CardTextCountersController.cs:698-711`）= `hiddenStat = true` + `currentAmount = -1` + **逐个 `textToSet.text = "?"`**；且 `SetText` 头 `if (hiddenStat) return`（:209）⇒ 期间任何数值变化都不改字。`ResetHide`（:726-728）= 只清闸。
- 我们（`Core/CardView.cs:7225-7240 ApplyAmbush`）= `_stats[s].SetActive(false)` 四层 + 关 `_info` / 护甲盾 —— **没有 "?" 这条路径，也没有 hiddenStat 闸**。
- ⇒ 判 **C / P0**（方案 §4.2 的 P0 判据里「**卡面数值**」明列）。工作量 **小**（board 走 `StatTexture` 的 "?" 图、2D 面走 CF-3 那四个 TMP，再加一个 hidden 闸）。

### 发现 4 🔴 静默缺失：`ItemDrawer` 句柄表只增不减（每画一格 +1，全仓 0 个释放调用点）

- 原版 `ItemDrawer.OnDestroy`（`ItemDrawer.cs:134-141`）随 GameObject 销毁自动 `Release`。
- 我们：`Bind()` 里 `_handles[v] = res.Drawer`（`Shell/ItemDrawer.cs:1871`），而 `Release(view)`（:1899）**只从字典移除**，且 `grep "ItemDrawer.Release("` 全仓 **0 个生产调用点**（只有自检与注释）⇒ `Dictionary` 是**强引用**，被销毁的 `ItemDrawerView` 也不会被回收，**开窗/画格次数越多表越大**。
- 只影响内存与 `HandleCount`，不影响画面 ⇒ **C / P2 / 工作量小**（给 `ItemDrawerView` 加 `Dispose()` 挂到 `MenuDraw.ClearChildren` / 窗关闭，退路 = `ConditionalWeakTable`）。

### 发现 5 🔴 判据订正：`CardCollectionTab.ToFocus` 不是 EventSystem 焦点，是「切页签重灌集合」⇒ 我们有一条真差异

- `CardCollectionTab.ToFocus`（`CardCollectionTab.cs:186`）= `base.ToFocus()`（空桩 `@1804B33B0`）+ `[this+0x30].UpdateCollection(GetCollection())`；**同族 `DeckCollectionTab.cs:158` 逐句同形**（也是 base + `[this+0x30].UpdateCollection(...)`）⇒ `[+0x30]` 就是本页的 `CollectionDisplay<T>`。
- 账原写「『焦点』在原版是 EventSystem 的选中对象，我们的壳是自绘命中层、没有这个量」——**不成立**。
- 我们：`Shell/CollectionTabPage.cs:48-51 OnOpen()` 只 `SetupOnce()`（首建后只 SetActive）⇒ **切回来不重灌**。可见例子（待真机）：Cosmetics 页换卡背/异画 → 切回 Decks 页，卡组格缩略图可能仍是旧的 ⇒ **C / P2**。

### 发现 6 🔴 账的**类名**错了 2 条：`Release()` / `TryRelease()` 属于**嵌套私有类 `GameModesIcon`**

`dump.cs` 现读（`119316-119356`）：
- `GameModesIcon`（私有嵌套，TypeDefIndex 3207）有 `TryGetIcon` / **`TryRelease(PlayModes)`** / **`Release()`** / `.ctor`
- `GameModesIconCollection`（3208）只有 `GetIcon` / `ReleaseAll` / `Release(Nullable<PlayModes>)` 三个

⇒ 账里 `GameModesIconCollection.Release()`（L203）与 `GameModesIconCollection.TryRelease(...)`（L207）**类名归属错**；且这两条 + `ReleaseAll()`（L205）**全库 0 调用点**（`ReleaseAll` 体里是直接读字段调 `icon.Release()`，`GetIcon` 内联了自己的循环，都不调它们）⇒ **应判 D（零调用）**，不是「保持 B 等建句柄层」。

### 发现 7 🟡 「原版也没有 / 我们的对端根本不存在」的 5 条（该判 D，别当待办）

| 账行 | 事 |
|---|---|
| 收藏 L203 / L205 / L207 | 见发现 6（零调用） |
| 收藏 L342 / L343 | `WhereToGetCardsSection` 的抽屉释放 —— 而 `Shell/WhereToGetCardsPopup.cs`（682 行）**全文件 0 处 ItemDrawer**（只有 2 行注释提到原版那格叫 `Card Drawer`），卡格由 `DrawCardCells:567` 直接画 ⇒ **账里给它挂的「改动请求 2」不该做** |
| 收藏 C 行 L244 / L247 | `InventoryManager` 两个 `LoadableReference<T>` 重载 —— 全库 grep 只有定义与自身转发、**0 个外部调用点**（唯一在用的是无参 `GetOwnedCount<CardDeck>()`，`SelectDecksTab.cs:90`）⇒ 该判 **D（零调用）** 而不是挂 C |

### 发现 8 🟡 零代码就能升 A 的「落点形态」问题（方案 §4.1 自己的例外条款没被用上）

方案 §4.1 在收紧「一个 `文件:行` 覆盖多个方法 ⇒ 只能 B」的**同一段**里留了例外：「若多个原版方法确实由同一段代码实现，落点可以是共享的 —— 条件是 ① 给**行号范围** ② `判据要点` 说清这段如何分别满足各方法 ⇒ **仍算 A**」。
卡组编辑那 11 条**全部满足 ②**（G6 已逐句核过），只缺 ①（现在写的是单行 `:2083`）。⇒ 下一波执行代理**不用改一行代码**，改账即可（本 TSV 第 7 列已给出真值行号，第 8 列已给出该写什么）。

---

## 四 没查清的部分

| # | 没查清 | 还差什么 |
|---:|---|---|
| 1 | `ItemDrawer<T>.Draw(ObtainableItem,int,options)` sealed override 的 **11+ 跳**（quantity / label / image） | 要 VA 反汇编（`decomp_full` 那几跳是 `NoteDecompilerIssue`）。判据清单已在 `收藏_未读132条_第2轮` §二·3 列过 |
| 2 | **23 个抽屉子类**的 `Draw(T,int)` 与 `GetItemName` 各自覆写了什么 | 逐子类读（`CardDrawer` / `WildcardDrawer` / `DeckDrawer` / `CoinDrawer` / …）；本轮只确认账里没有它们的独立行 |
| 3 | `CardHighlight.Start` 那四件事在 `Build` 里的**精确行号范围** | 只确认了 `void Build(CardData)` 在 `:4191`；四件的逐行位置没逐个定位（本轮没为它单独铺开） |
| 4 | 发现 3 的**可见性**：埋伏卡走 3D 卡体时，数值层到底看不看得见 | 真机看一眼（我们的 board 数值层是纹理 quad，`SyncStatLayers` 里 `show = board && StatShownFor`）。看不见 ⇒ 该条降 P2；看得见 ⇒ P0 成立 |
| 5 | 发现 5 的**可见性**：换卡背后切回 Decks 页，卡组格卡背缩略图是不是真的旧 | 真机（或写一条自检读卡组格的贴图名） |
| 6 | 原版 `CollectionFilterController.Toggle(option, withAnimation)` **有没有调用点传 `withAnimation=false`** | 全库 grep 原版调用点（本轮没扫；若无，则我们「没有 withAnimation 形参」不影响 A） |
| 7 | 收藏过滤族判 A 时，`SetupFilters` 那一步「缓存 `originalAnchorPosition`」我们到底用哪个常量承载 | E 侧判 A 时给的是「抽屉原位记在 `FltAbs`/`Slide` 常量」—— 本轮没逐字复核那一处，交 E/C 侧在改账时顺手点明 |
| 8 | `CardTextCountersController.Toggle(bool value)` 的 `ActivationRules`（Normal / SmallScreen / SimplifiedView / NoSimplifiedView）我们有没有对应 | 本轮只读了骨架（`CardTextCountersController.cs:25-`、:731-）；与「?」不是一件活，但同族，留给下一批 |
| 9 | `MulliganManager.cs:284` 那处 `ToggleTooltips` | 由 `报告/R17`/E7 的清单外发现（账 L21 的备注已记）；本轮没接进 C 档定级（属战斗 HUD 域） |

---

## 五 顺手发现（不在本任务范围，但下一个人需要知道）

1. 🔴 **`工具/ledger_slice.py` 的「物理行」= 真值 − 1**。它 `enumerate(raw.split('\n'))` 从表头 i=0 起算，而文件头注释写「1 = 表头下一行」。**拿它打印的数字去改账 / 定位会整体偏一行**。建议：把表头从计数里排除（`enumerate(raw.split('\n')[1:], 1)`）或改注释；在那之前，**一律以本产物/编辑器显示的真实行号为准**。
2. 🔴 **`工具/check_ledger_points.py` 只机检 A 档**（本轮实测：三域 `A=219/231/109`、漂移 0、失败 0，而 B 档至少 **9 条**已漂 23–52 行）。建议加 `--status` 或默认把 B/C 的落点也过一遍形态检查。
3. 🟡 **`WhereToGetCardsPopup` 全文件 0 处 ItemDrawer**（只有注释提到原版那格是 `Card Drawer`）⇒ 账里给它挂的「建抽屉时存 view 列表 + OnDestroy 转调 Release」是**基于误判的改动请求**，别做。
4. 🟡 **`CardView.Build` 的声明在 `:4191`**，而账里有落点写 `:4176（Build）`（`:4176` 是 `BadgePlateQuad` 那块 Mesh 代码里）。同文件还有 `_baseScale` 的写点在 `:9698` / `:10277`（不在 Build 里）—— 收紧落点时别指错。
5. 🟡 **`GameModesIconCollection` 的三个 Release 方法其实只有一个有真实调用点**（`Release(Nullable<PlayModes>)`，经 `ScriptableObjectsCollectionsUtilities.ReleaseGameModeIcon`，且两个调用点**都传 null**）。做「菜单图标」相关的任何活之前先知道这件事，否则会照账去「建句柄层」（白做）。
6. 🟡 **`ItemDrawer<T>.ToggleClaimedWarning` 不是死代码**（`ItemDrawer.cs:603` 在 `SetConvertedItem` 里活着调），而 `ToggleHighlight` / `ToggleLock` 是死的 —— 这条账里已订正过（L289 备注），本轮复核成立，别再并回去。
7. 🟡 **`DeckRules.DescribeShort`（`:694`）同时服务两个原版方法**（`ToShortLocalizedString` / `ToShortRawLocalizationString`），是**共享落点的正例**（§4.1 例外条款）—— 改账时两个一起写范围，别只改一个。
8. 🟡 **`PracticeModePopup` 自 G6 第5轮以来又长了一截**（B 档落点整体 −23 ~ −52）。它是本域改得最频的文件之一；**任何给它的落点都建议写成「声明名 + 行号范围」**，这样 `--fix` 才能按名字重定位。

---

## 六 给下一波执行代理的三条施工建议

1. **先做「改账」这一批（20 条 → A，零代码）**：8 条收藏过滤族（照 `共用件_过滤模型_B升A.md` 的编号落点）+ 11 条卡组编辑（照本 TSV 第 7 列的真值行号 + 第 8 列的范围写法）+ 1 条 `CardCollider2D.Toggle`。⚠️ 改账时**同一时刻只有一个写手**，且必须先合并补丁再动主表。
2. **再做 4 条真缺口（C）**：`HideStat/ResetHide`（P0，小）· `ToFocus`（P2，小）· `ItemDrawer.OnDestroy`（P2，小）。三条都在本域 `.cs` 里，互不相交，可三路并行。
3. **把 C 档那 6 条判错/空级的先改掉**（收藏 L244 / L247 → D · 收藏 L273 补 P2 · 卡组编辑 L285 / L286 → P0 · 卡牌面 L21 → P1），否则下一批还会按错的优先级排队。
