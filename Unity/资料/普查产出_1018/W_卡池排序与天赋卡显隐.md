# W · 卡组编辑卡池：排序 / 天赋卡显隐 / 全量显隐 —— 落地报告

> 写手件（2026-10-18）。判据正本 = `资料/普查产出_1018/AUD_卡池排序与天赋卡显隐.md`（只读代理已查实原版链）。
> **没跑 Unity / `*.Run` / player**；只跑秒级类型检查（`TMPDIR=/tmp/wf_pool bash Unity/工具/typecheck.sh`，**0/0**，每一轮 `.cs` 改动后都跑过）。
> 没碰 git 写操作、没改正本。

---

## §一 逐条表

| # | 项 | 改了啥 | 文件:行（现读） | 判据（原版，现读 `d:/2/tools/decomp_full/`） | typecheck |
|---|---|---|---|---|---|
| 1 | **卡池排序** | `VisibleCards()` 末尾新增 `SortForDisplay()`：**`cardArmy`(10→130) → 战将整段先 → `manaCost` → 本地化名 → `Id` 兜底**（按目标次序**直接比较**，不靠链式 `OrderBy` 的隐式主序）。本地化名**复用现成那一把尺** `DeckRuntime.CompareDisplayName`（`string.Compare(..., CurrentCulture)`，与左侧卡组列表 `DeckEntries` 同一个比较器）。删掉了旧权宜 `StableGroupByFaction`（「阵营按首次出现」）。 | `CardPresentation/Deck/DeckEditorState.cs:577`（`SortForDisplay`）· `:604`（`ArmyRank`）· `:395`（`VisibleCards`）· `:473-478`（调用点） | `DeckEditingWindow__GetCardCollection.c:106,121,136,151`（4 个稳定 `OrderBy`）· 键选择器 `…b__32_5/6/7/8.c` | 0/0 |
| 2 | **收藏排序多夹 `inventoryOptions`** | 新开关 `SortByInventoryOptions`（**只有收藏窗打开**）⇒ 实际序 **阵营 → 战将 → inventory → 费用 → 名** | `DeckEditorState.cs:106`（字段）· `:591-593`（排序层） | `CardCollectionTab__GetCollection.c:69,84,99,114,129`（5 个稳定 `OrderBy`） | 0/0 |
| 3 | **显示判据（两条链共用）** | `VisibleCards()` 里把原来的 `DeckRules.CannotAddToDeck` **换成**原版卡池链：`PassesShowInCollection`（`show != Hide(0)`；`show==OnlyIfOwned ⇒ owned` 在全解锁下恒过；`!IsHidden()` **判据不在本地 ⇒ 不做，注释写明**）+ `PassesEditModeInventory`（卡组编辑额外那层 `inv != CantAddToDeck(5)`，**防御卡例外**＝用户 2026-09-27 口径）。显示闸由 `WarlordGatedPool || ShowInCollectionOnly` 控制（默认都关 ⇒ 纯逻辑用例不受影响）。 | `DeckEditorState.cs:542`（`PassesShowInCollection`）· `:556`（`PassesEditModeInventory`）· `:412-425`（调用点） | `DeckEditingWindow.__c___GetCardCollection_b__32_2.c:8`（`show != 0`）· `DeckEditingWindow___GetCardCollection_b__32_1.c:28-35`（`inv != 5`，防御卡看 `assignDefensiveCardsInEditMode`）· `…b__32_0.c`（`show==10 ⇒ HasItem`）· `…b__32_3.c`（`!IsHidden`，vtable `0x2A8`） | 0/0 |
| 4 | **收藏窗套显示闸** | 新开关 `ShowInCollectionOnly`；`CollectionWindow.CardsState` 创建时打开（另开 `SortByInventoryOptions`）。**卡背页 / 异画页的状态对象不设**（各走各的） | `CardPresentation/Shell/CollectionWindow.cs:819-828` | `CardCollectionTab.__c___GetCollection_b__8_0.c:19-30` | 0/0 |
| 5 | **「显示 + 变灰」** | 卡池格渲染：`CanAdd != None` 的卡从 `CardHighlightState.Normal` 改成 **`Unplayable`**（整卡 tint `(0.55,0.55,0.58)`）。⚠️ 这是**乘法压暗**、不是原版真去饱和 `GreyScale`——`CardView` 整卡没有 `_GreyScale` 通道（那条只在**徽标**材质上），改真去饱和要动 `Core/CardView.cs`（本笔禁碰）⇒ 见 §四 | `CardPresentation/Deck/DeckRuntime.cs:3005-3016` | `DeckEditorCollectionDisplay__DrawCell.c:75-92` → `CollectionCard__UpdateCardVisuals.c:17` 的 `ToggleGreyScale(param_2 ^ 1)`（`param_2` = `CanAddCard`） | 0/0 |
| 6 | **5 张 DA 秘仪「显示且可加」** | `DeckRules.CannotAddToDeck` 的列判据 `\|\|` → **`&&`**（＝原版 `CardDeck__CanAddCard.c:40` 那个变体）。现读池内差集实测：`inv=5` 的 **39 张全是 defence**（已被卡型路由先返回）⇒ **实际只放开那 5 张 `inv=10 & show=5`**。同时把文档里与新口径矛盾的两段改成「2026-10-18 就地订正」 | `RuleEngine/Core/DeckRules.cs:299`（`return`）· `:242-248`、`:255-260`（注释订正） | `CardDeck__CanAddCard.c:40`（`&&`）+ `:76-77`（`GetOwnedCount==0`——我们**没有**这道闸）· 主对话 2026-10-18 裁定（AUD §五.B.5） | 0/0 |
| 7 | **断言（DeckScene，纯逻辑）** | 新增 `TestPoolSortAndVisibility()` 并在 `Run()` 注册：① 每阵营首张=战将 ①b 战将段连续 ①c 无督军时 13 段、每段首张=战将、段序=ArmyOrder ② 战将段后费用非减 ③ `show==0` 张数=0 ③b 收藏闸 ④ 39 张防御卡一张不少 ⑤ 收藏序首卡=第一个阵营的战将 + inventory 非减 | `CardPresentation/Editor/DeckScene.cs:765`（注册）· `:1532`（方法） | 同 #1/#2/#3 | 0/0 |
| 8 | **断言（DeckScene，渲染/状态）** | 卡池格「显示+变灰」：① 有督军 ⇒ 找一张 `CanAdd != None` 的格 ⇒ `State==Unplayable` + tint 读数 `(0.55,0.55,0.58)`；② **反向控制**：摘督军 ⇒ 池里全可加 ⇒ **一格都不灰** | `DeckScene.cs:3049-3105` | `CollectionCard__UpdateCardVisuals.c:17` | 0/0 |
| 9 | **断言（DeckScene，A1798 更新）** | `DA_Convoke_the_Circle` 期望值「不能进」→「**能进**」；`viaCols` 交叉核 `\|\|`→`&&`；「旧拦新放」由 `==0` 改为「**只许是** `inv=10 & show=5` 那 5 张」（`oldOnly == legit` 且 `>0`）；EC/DA「池里没有不能进卡组的卡」保留（理由改成两条判据） | `DeckScene.cs:1329-1375`、`:1395-1410`、`:1441-1489` | 同 #3/#6 | 0/0 |
| 10 | **夹具修正（排序带出来的必改项）** | 拖拽那一节的「从卡池**前 6 格**里挑一张非 hero/defence」在排序后失效（战将段最多 7 位，占满前几格）⇒ 扫的范围 **6 → 18**（前三行；第 4 行在屏外，⛔ 别去那里起拖） | `DeckScene.cs:2186-2210` | 原版排序 #1 的必然后果（不是新缺陷） | 0/0 |
| 11 | **断言（CollectionScene）** | 新增一节：① 收藏卡牌页 `show==0` = 0；①（交叉核）卡数 = 两列现算；② **天赋卡 = 0**（真值 `talent_cards.json` 92 个去重 id 逐张查）；③ 首卡 = Ultramarines 战将；③b 同阵营同战将档段内 `inventoryOptions` 非减；④ 异画页 7 张照旧、`StylesState`/`CosmoState` 没被套闸、`Pool` 本体没被改小 | `CardPresentation/Editor/CollectionScene.cs:4620-4720` | `CardCollectionTab.__c___GetCollection_b__8_0.c:19-30` · `CardCollectionTab__GetCollection.c:69…129` | 0/0 |

---

## §二 修后现读计数（2026-10-18 现读 `cards_engine.json`，`count = 1127`）

### 2·1 卡池 / 收藏页总数（跨阵营口径）

| 池 | 张数 | 说明 |
|---|---|---|
| 卡表全池 | **1127** | `count`（卡池本体没动，过滤只在 `VisibleCards`） |
| **卡组编辑卡池（跨阵营并集）** | **1005** | `show != 0`（1127 − 122）；`inv=5` 的 39 张全是 defence、按用户口径保留 |
| **收藏卡牌页** | **1005** | 原版收藏链只看 `show != 0`（不看 inventory）⇒ 与卡组编辑并集同数 |
| 被剔掉 | **122** | 92 天赋 + 30 `Z-NC` 效果卡 |

### 2·2 卡池按类型（1005 张）

| 类型 | 张数 | 备注 |
|---|---|---|
| unit | **585** | 剔掉 1 张 `show=0` 的单位（`Z-NC` 族） |
| hero / 战将 | **56** | 全部 `show=5`，一张没动 |
| tactic | **325** | 446 − 121（`show=0` 的战术卡） |
| defence | **39** | 全部 `show=5`、`inv=5` ⇒ 用户口径保留 |
| **其中：效果生成档** | **30 剔 + 5 留** | 剔 = `inv=10 & show=0`（`Z-NC`）；留 = `inv=10 & show=5` 的 5 张暗黑天使秘仪（`Convoke the Circle` / `None Must Know` / `Obscure Ritual` / `Rites of Penance` / `Smothering Decree`，**显示且可加**） |

### 2·3 被剔掉的 122 张 · 逐族计数

| 阵营 | 张数 | 阵营 | 张数 |
|---|---|---|---|
| Ultramarines | 19 | Sautekh | 7 |
| Goff | 15 | SpaceWolves | 7 |
| BlackLegion | 12 | Leviathan | 7 |
| Genestealers | 11 | AstraMilitarum | 6 |
| SaimHann | 9 | DarkAngels | 6 |
| EmperorsChildren | 9 | Sororitas | 5 |
| TauEmpire | 9 | **合计** | **122** |

构成现读：`show=0` 里 **92 张 = `talent_cards.json` 的去重 `talents` id**（96 宿主 / 92 张去重），另 **30 张 = `Z-NC` 效果卡**（非天赋）。按类型 = 121 tactic + 1 unit。

### 2·4 单阵营卡池（定了该阵营督军时能看到的张数）

| 阵营 | 张 | 阵营 | 张 | 阵营 | 张 |
|---|---|---|---|---|---|
| Ultramarines | 115 | Goff | 104 | SaimHann | 83 |
| DarkAngels | 77 | AstraMilitarum | 72 | BlackLegion | 72 |
| Genestealers | 72 | Sautekh | 72 | Sororitas | 72 |
| TauEmpire | 72 | Leviathan | 72 | EmperorsChildren | 61 |
| SpaceWolves | 61 | | | | |

（没督军 ⇒ 只列 56 位战将，按 `cardArmy` 10→130 分段。）

---

## §三 断言清单（每条都写「灭自证」）

### DeckScene（纯逻辑，`TestPoolSortAndVisibility`）

| 断言 | 灭自证（改坏哪一处会红） |
|---|---|
| ① 每个阵营卡池**第一张是战将** | 去掉 `cardType != Hero` 这一层 / 换成 `ThenBy` 语义 ⇒ 首张变成非战将 |
| ①b 战将段**连续** | 排序键只对「第一张」生效（比如手动把战将挪到 0 号位）⇒ 后面又冒出督军 |
| ①c 无督军：13 段、每段首张战将、段序 = `ArmyOrder` | 换回 `StableGroupByFaction`（首次出现序 = SaimHann 打头）⇒ 段序条红 |
| ② 战将段之后 `manaCost` 非减 | 删掉费用那一层 ⇒ 同段内出现逆序 |
| ③ 卡池 `show==0` 张数 = 0 | 把显示判据退回 `CannotAddToDeck` 或删掉 `showGate` ⇒ 122 张漏进来 |
| ③ 每阵营张数 = 两列现算 | 判据实现与（独立的）两列算式不一致 ⇒ 红 |
| ③b 收藏闸 `show==0` = 0 / 张数 = `show!=0` 现算 | 不打开 `ShowInCollectionOnly` ⇒ 1127 |
| ④ 39 张防御卡一张不少 | 去掉 `PassesEditModeInventory` 的防御卡例外 ⇒ 全被 `inv=5` 剔掉 |
| ⑤ 收藏序首卡 = Ultramarines 战将；inventory 段内非减 | 不排序 ⇒ 首卡是卡表原序第一张；去掉 `SortByInventoryOptions` ⇒ 防御卡（inv=5）按费用混进 inv=0 段 |
| 渲染 ①：加不进的卡 `State == Unplayable` + tint 读数 `(0.55,0.55,0.58)` | 改回 `Normal` ⇒ 本条红 |
| 渲染 ②（**反向控制**）：无督军 ⇒ 一格都不灰 | 恒灰（把 `Playable` 那支也写成 `Unplayable`）⇒ 本条红。两条**同一段 `RefreshPool` 的两个方向**，结构上不可能同时错 |

### CollectionScene（`Cards 页` 之后新节）

| 断言 | 灭自证 |
|---|---|
| ① 收藏卡牌页 `show==0` = 0 / 卡数 = 两列现算 | 关掉 `ShowInCollectionOnly` ⇒ 回到 1127 |
| ② 天赋卡 = 0（真值表 92 个 id 逐张查） | 与 ① 独立成条（按 id 查，不按 `show` 查）；天赋若被漏进来 ⇒ 红 |
| ③ 首卡 = 第一个阵营的战将 | 不排序 / 排序键次序错 ⇒ 红 |
| ③b 同阵营同战将档段内 `inventoryOptions` 非减 | 去掉 `SortByInventoryOptions` ⇒ 防御卡按费用混进 inv=0 段 ⇒ 红 |
| ④ 异画 7 张照旧 / `StylesState`·`CosmoState` 没被套闸 / `Pool` 没被改小 | 把两个开关写进 `DeckEditorState` 的默认值 ⇒ 异画池被一起过滤 ⇒ 红 |

---

## §四 没查到 / 缺什么

1. **`!IsHidden()` 这一条没做** —— 判据是服务端 `FeatureConfig` 下发的两张隐藏表（`FeatureConfig__IsItemHidden.c:20-40`，`+0x20`/`+0x28`），**本地没有这份配置**（铁律 11 例外①）。代码注释写明「判据不在本地」，**没有静默当成 `false`**、也没有按卡名猜隐藏集。
2. **`show == OnlyIfOwned(10) ⇒ owned` 是边界不是判据** —— 单机全解锁 ⇒ `owned` 恒真；现读池里 `show=10` = **0 张**，今天连边界都用不上。如实注释。
3. **「变灰」不是原版那种去饱和** —— 原版 `UIImageGreyscaleController.ToggleGreyScale` 是真正的 `_GreyScale` 去饱和（走材质/shader）；我们用的是 `CardHighlightState.Unplayable` 的**乘法压暗** `(0.55,0.55,0.58)`。`CardView` 整卡没有 `_GreyScale` 通道（该属性只在**徽标**材质上，见 `CardView.BadgeGreyed`），要做成原版那样得改 `Core/CardView.cs`（本笔**禁碰**）的材质/shader。**缺的口子 = 整卡层一个 `GreyScale` 材质开关（或顶点色去饱和）**。
4. **原版 `b__32_4`（异画 / 「卡组里已有本体 ⇒ 异画可见」那一半）没实现** —— 我们池里只有本体条目（7 张异画的**本体**），没有 `IsAlternateArt` 的实例 ⇒ 这一条今天**无对象**；收藏窗的 `!IsAlternateArt()` 同理。
5. **`assignDefensiveCardsInEditMode` 没按模式切** —— 原版 Classic = false / Skirmish = true；用户 2026-09-27 口径是「防御卡要在编辑卡池里」⇒ 我们**整类保留**（与本笔无关，未改回）。
6. **`CollectionManager.AllCardCollection` 的实际内容**（是否等于 1351 张 SO 全量）静态读不到 —— 本文所有「原版显示多少张」仍按判据推算，不是实机读数。
7. **没跑任何 `.Run` / 真机**（按红线）—— 所有断言都是静态写好的，**没有执行过**；「排序首张是不是战将」这类结论有断言兜着，但要等下一次统一验收才真正跑。

---

## §五 顺手发现（供调度台分流）

1. **`DeckRules.cs` 有另一路数据写手在并发改注释**（本轮两次收到「文件已被磁盘修改」）。我的 `||`→`&&` 落进同一函数，两人的改动**已共存**：对方把「落地的效果」写成了 `A2052` 口径（127 张 / 真值缺口 0 张），我在其上补了「改口径后 = 拦 122 张」；另外把上面一段与 `&&` 直接矛盾的「必须按 `||` 那一版做」加了 **2026-10-18 就地订正**。收口时建议再肉眼过一遍这个文件的文档段。
2. **`DeckScene` 拖拽夹具的「前 6 格」前提**在排序落地后**必然失效**（战将段最多 7 位、占满前几格）⇒ 已改 18 并写明理由。这不是功能缺陷，是排序的必然结果；以后谁再改排序键，要回来看这一节。
3. **`CollectionScene:4592` 的 `CardsVisibleCount > 1000` 现在只剩 5 张余量**（1005）。将来若有卡被 `show` 收紧（或再补真值），这条会最先红 —— 建议下次把它改成「= 两列现算」而不是写死阈值（本笔没动它）。
4. **现读池里 `InventoryOptions` 已无 `-1`**（0/5/10 三档全覆盖）——`A2052` 说的「真值接不上 171 张」在现卡表上已不成立；`PassesEditModeInventory` 的 `-1` 分支今天是**死分支**（保留以防数据回退，注释已写）。
5. **`DeckScene.TestFilters`「不设筛选命中全部」仍走 1127** —— 说明两个新开关的默认值（false）没把纯逻辑用例带偏；以后**别把 `ShowInCollectionOnly` 默认改成 true**（会把那条打红，而且收藏/编辑两条链本就该各带自己的开关）。
6. **性能**：`VisibleCards()` 现在每次多一次排序（≤1127 项，名字预先取一次再排）。`CardsCellDef(i)` / `UiPoolCardAt(i)` 这类**逐格调用**会重复排序（`CollectionScene` 自己的注释早写过「逐张取会白跑 1000+ 次」）——本轮**没有加缓存**（怕引入陈旧态）；若真机/批处理觉得慢，建议在 `DeckEditorState` 里按 `(Filter, WarlordId, 两个开关)` 做一次结果缓存，而不是在调用点各加一份。

---

## §六 收口补丁（2026-10-18 · 调度台点名）

**问题**：`CollectionScene` 里按卡数写的**硬阈值**在「卡池 1127 → 1005」之后余量过小（`> 1000` 只剩 5 张），下次数据一变就**假红**。按调度台口径全部改成「**两列 / 卡表现算**」，⛔ 不再写死数字。

| # | 位置（现读 `CardPresentation/Editor/CollectionScene.cs`） | 改前 | 改后 | 说明 |
|---|---|---|---|---|
| ① | `:4592-4600`（Cards 页「卡池读到 N 张」） | `win.CardsVisibleCount > 1000` | `Check(win.CardsVisibleCount, wantCardsShown)`，`wantCardsShown` = `CollectionWindow.CardsState.Pool` 里 **`showInCollectionOptions != 0`** 的张数 | 期望值 = 收藏卡牌页显示判据的两列现算（`CardCollectionTab.__c___GetCollection_b__8_0.c:19-30`）；注释写明「1127→1005 后原阈值只剩 5 张余量」 |
| ② | `:4716-4727`（本报告 §一 #11 那一节 ④ 的「卡池本体没被改小」） | `cs.Pool.Count > 1000` | `Check(cs.Pool.Count, wantPoolAll)`，`wantPoolAll` = `CardDatabase.Load()` 的**去重非空 id 数** | `Pool` 是**未过滤**本体 ⇒ 期望值恒等于卡表去重 id 数 |
| ③ | `:10481-10495`（A1132 那一节「编辑器内容真建在窗里」） | `deW3.Runtime.State.PoolCount > 1000` | `PoolCount == wantPoolN`，`wantPoolN` = 同一份「卡表去重 id 现算」 | 原先的 `> 1000` 同样改成现算；读数写进断言文案 |

**复核**：改完该文件再扫一遍 `1127 / 1126 / 1351 / > 1000 / >= 1000` —— 剩下的命中**全在注释里**（`1127 → 1005` 的说明文字），**没有第二条硬阈值**。

**typecheck**：`TMPDIR=/tmp/wf_pool bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0（0/0）**。
**行尾**：`CollectionScene.cs` 仍是纯 LF（0 CRLF / 10637 LF），没翻。
**没跑 Unity / `.Run` / player，没碰 git，只改了 `CollectionScene.cs` + 本报告。**
