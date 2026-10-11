# Z · 两处悬案（`row_x*` 字色/自适应 · `Deck info Popup` 列表排序）

> 只读诊断代理 `Z-两处悬案`。**没跑 Unity** · **没动 git** · **没改正本** · 除本文件外**没建/改/删任何文件**。
> 读数一律走 `工具/menu_rect.Bundle` + `工具/menu_dump.py` 的函数（**`go_by_rt` 按 RT pid 认人**，不用 pid 索引）；反编译走 `d:/2/tools/decomp_full/`。

## 结论（两句话）

- **悬案 A**：两份读数**不是同一族**（⛔ 不是投票题）——「卡组编辑窗左栏」那一份是 **`Deck Selector Card Info button`**（在 `Deck Editing Menu/Sidebar/Deck Details/Deck List drawer` 那棵树里），它的 `Count` = **色 `(0.9333,0.9333,0.9333,1)` · 自适应**开**`[2.0~32.0]` · 基准 `36.0` · 收敛字号 `32.0` · 对齐 `Right/Midline`**；甲读到的 `(1,1,1,1)`（连「`anchor(1,0)-(1,1)`＋`50 宽`」）**全部来自另一族** `Deck Selector Hero Card Info button`（督军行）。⇒ **乙对，甲读错了对象**；我们现在的 `Color.white` + 固定 32 无自适应**两条都不对**。
- **悬案 B**：`Deck info Popup` 的卡组列表**就是同一套东西** —— 它挂的组件**就是 `DeckListDrawer`**（与左栏同一个类），`DeckInfoPopup.Open` 调 `DeckListDrawer.Initialize`，而 `Initialize`/`AddCardToList` 收尾都调 `Reorder` ⇒ **该按「费用升序 → 本地化卡名」排，与左栏同一条规则**（键1 已由字节码直读坐实 = 费用）。差别只有三个开关：弹窗 **`showWarlord=0` ⇒ 列表里没有督军行**、`checkOwnership=1`、`stackCopies=1`（与左栏相同）。

## A：`Count` 家族逐个表 → 判定

**前提先纠正**：`bundle_menus_assets_all` 里名叫 `Count` 的 TMP 一共 **15 个**，分属 **3 个预制体族**（不是「同族 8 个」）。按 RT pid 认人（`go_by_rt`）：

| 族 | 实例数 | `Count` 色 | 字号 / 基准 / 自适应 | 对齐 |
| --- | --- | --- | --- | --- |
| `Deck Selector Card Info button`(+`_ref`) | **11** | `(0.9333,0.9333,0.9333,1)` | `32` / `36` / **auto `[2~32]`** | `Right/Midline` |
| `Deck Selector Hero Card Info button` | 2 | **`(1,1,1,1)`** | `36` / `36` / **auto=0**（区间 `[18~72]` 是默认值） | `Center/Middle`，文字 = `'2'`（**无 `x` 前缀**） |
| `Deck Selector Defensive Card Slot` | 2 | `(0.9333,…)` | `32` / `36` / auto `[2~32]` | `Right/Midline` |

`Deck Selector Card Info button` 那 11 个（`工具/menu_dump.py … "Deck Selector Card Info button"` 的名字索引只给 8 个；`_ref` 变体有 3 个被它的索引漏掉，**值全同**）：

| # | GO pid / **RT pid** | 父链（根 ← 近） | 挂在哪扇窗 / 用途 | 色 | autoSizing |
| --- | --- | --- | --- | --- | --- |
| 1 | `-8210680612615688412` / **`-5688290593639960796`** | `Deck Editing Menu ◂ Content Area ◂ Sidebar ◂ Deck Details ◂ **Deck List drawer** ◂ Scroll View ◂ Viewport ◂ Content` | **卡组编辑窗左栏 —— 就是这一份** | `(0.9333,…,1)` | **auto `[2~32]` base 36 · 字号 32** |
| 2 | `2499097616895176363` / `3049761815625002667` | 无父链 | 预制体**本体**（模板，不在任何窗里） | 同上 | 同上 |
| 3 | `2994867481076140456` / `-8680500939708855896` | `Deck info Popup ◂ Info Panel ◂ **Deck List** ◂ Content` | **悬案 B 那扇窗的列表** | 同上 | 同上 |
| 4 | `4702626575312321221` / `-6068752977877108027` | `Draft Mode Menu Demo ◂ Draft Mode Deck Info Panel ◂ Cards in deck panel ◂ content` | 轮抽模式（Demo 树） | 同上 | 同上 |
| 5 | `714433798168628658` / `-240534329623654990` | `Draft Mode Deck Info Panel ◂ Cards in deck panel ◂ content` | 轮抽模式（另一份） | 同上 | auto `[2~32]` base 36 · **字号 30** |
| 6 | `7696177630776061732` / `-6168079906896089308` | `Deck Editing Menu ◂ Content Area ◂ Card Display ◂ Card Drag Controller` | **拖拽中的卡牌替身** —— **没有 `Count` 子件**（只有 `Card name`，白色/30px） | — | — |
| 7 | `9000127046654651114` / `5588879667405941482` | `Draft Mode Timed Mode Window ◂ Draft Mode Deck Info Panel ◂ …` | 轮抽·计时模式 | 同上 | 同上 |
| 8 | `-8281892244632657195`(**`_ref`**) / `-8435771671715929387` | `RankedEventWindow ◂ Deck info ◂ Deck List Drawer ◂ Content` | 排位活动窗 | 同上 | auto `[2~32]` **base 22** · 字号 28.1 |

> 另外 3 个 `_ref` 实例（RT `-3032351130120678758` / `3339693772798520098` / `3686555523535884092`，分别在 `Draft Mode Card in deck container Variant` / `Card in deck container` / `Practice Mode Menu ◂ Deck List Drawer`）**色也都是 `(0.9333,…,1)`**。

**判定**：
1. **「卡组编辑窗左栏那一份」= #1**（RT `-5688290593639960796`；它的 `Count` RT = **`-2738939269725491420`**，父 = `Text fill`）。取值：`m_fontColor = (0.9333333969116211, 0.9333333969116211, 0.9333333969116211, 1.0)` · `m_enableAutoSizing = 1` · `m_fontSizeMin = 2.0` · `m_fontSizeMax = 32.0` · `m_fontSizeBase = 36.0` · `m_fontSize = 32.0` · `m_HorizontalAlignment = 4(Right)` · `m_VerticalAlignment = 4096(Midline)`。
2. **甲的两个数来自 `Deck Selector Hero Card Info button ◂ Content ◂ Count`**（RT `6717119346084736804` 在卡组编辑窗里 · `4310701922469194173` 是预制体本体）：`(1,1,1,1)` · `36` · `auto=0` · `anchorMin(1,0)/anchorMax(1,1)/pivot(1,0.5)/sizeDelta.x=50`。⇒ **两个读数各自都真、指向两个不同对象**（铁律 4：按用途取，不投票）。
3. **该改哪儿**：`Deck/DeckRuntime.cs:1451-1457` 那段 doc 把**两族拼在了一起**（「50 宽 + anchor + 字色白」= Hero；「字号 32」= Card Info）；落点 `DeckRuntime.cs:1460-1461` 的 `TxtPx("row_x"+i, …, 32f, Color.white, QText)` ⇒ **色改 `(0.9333,0.9333,0.9333,1)`、开自适应 `min 2 / max 32 / base 36`**（常见情况下仍渲染 32，只有框挤了才缩）。
4. ⚠️ **督军行那颗不显示，所以取 Card Info 那套即对全部可见情形成立**：`UICardInfoItem__SetCount.c:41` `gameObject.SetActive(1 < count)`、督军恒 1 张 ⇒ Hero 那一族永远藏。⛔ 不必照 Hero 的白色/36 去做——**只要不显示就与原版一致**。

## B：`Deck info Popup` 该按什么排 —— **判据够**

**判据一（同一套东西，实读）**：`Deck info Popup ◂ Info Panel ◂ Deck List` 的组件 = **`DeckListDrawer`**（MB `-2787211753060071000`），字段表与左栏 `Deck List drawer`（MB `6974456956866981668`）**逐字相同**。

**判据二（谁调排序）**：`DeckInfoPopup__Open.c:91` → `DeckListDrawer__Initialize(listDrawer, deck.GetLibraryInFull(), options)`；`DeckListDrawer__Initialize.c:112` 在 spawn 完全部行之后**无条件** `DeckListDrawer__Reorder`；`DeckListDrawer__AddCardToList.c:46` 也调一次。

**判据三（排什么）**：`DeckListDrawer__Reorder.c:33-40` = `OrderBy(键1).ThenBy(键2)`；
- 键1 = `b__22_0` = `*(int*)(card + 0x78)`；`UICardInfoItem__SetCost.c` 拿**同一个** `card+0x78` 印费用文本、并按其符号位开关费用圆（`>>0x1f ^ 1`）⇒ **键1 = 费用（字节码直读）**。
- 键2 = `b__22_1` = `RawCardScript.GetLocalizedCardName(card)` = **本地化卡名**。
- 之后 `Reorder` 尾部把督军钉 `SetSiblingIndex(0)`、防御卡钉 `1 或 0`。

⇒ **弹窗列表 = 费用升序 → 本地化卡名，与卡组编辑窗左栏同一条规则。**

**和左栏的差别只有三个开关（序列化字段，实读）**：

| 字段 | 编辑器 `Deck List drawer` | `Deck info Popup` 的 `Deck List` |
| --- | --- | --- |
| `stackCopies` | `1` | **`1`（相同）** ⇒ 一卡一行 + `x{n}` |
| `checkOwnership` | `0` | `1`（运行时被 `deck.<bool@0x28>` 覆盖） |
| `showWarlord` | `1` | **`0`** |
| `UIHeroCardInfoItem` / `defensiveCardInfoItem` / `cardCounter` | 有 | **三者全 `null`** |

`showWarlord=0` 的落点：`Initialize.c:77` 拿它决定要不要对卡表套 `Where`，谓词 `__c___Initialize_b__16_0` = `card + 0x94 != 10`（卡种 ≠ 督军）⇒ **弹窗列表里没有督军行**（`UIHeroCardInfoItem=null` 时 `SpawnCardInfo` 会退到 Card Info 族，不改变排序）。

**容器**：`Content` 挂 `GridLayoutGroup`：`cellSize 360×58` · `spacing (11, 4.5)` · `pad (15,0,15,0)` · `m_StartCorner=0(UpperLeft)` · **`m_StartAxis=1(Vertical)`** · `m_Constraint=0(Flexible)` ⇒ **逐列填充**、按 1140 宽算 **3 列**。左栏那边 Content 是 `VerticalLayoutGroup`（单列）。

**实拍**：13 张用户参考图**逐张看过，没有这扇窗**（`更换卡组的参考.png` = 「选择卡组」弹窗 · `收藏—卡组的参考图.png` = 收藏·卡组页 · `卡组编辑界面参考.png` = 编辑器本体 …）⇒ **本件没有实拍可量**，但**也不需要**：判据是预制体 + 反编译，已够。

**我们的偏离**（`Shell/DeckInfoPopup.cs:1080-1132`）：
1. **完全没排序**：`order` = `deck.CardIds` 的出现序（全文件 `Sort`/`OrderBy` **0 命中**）。
2. **多一行督军**：`:1093` 把 `deck.WarlordId` 插到 `order[0]`（截图 `X5_deck.png` 左上那颗 `Anvirr Keltoc`）—— 原版**没有**。
3. **填充轴反了**：`:1097` `c = i % cols, rr = i / cols` = **逐行**；原版 `startAxis=Vertical` = **逐列**。
4. 卡名取 `card.Name`（**英文**）—— 原版键与显示都用 `GetLocalizedCardName`，且我们编辑窗用的是 `CardText.Name(Name, NameZh)` ⇒ 同一副牌**弹窗英文、编辑窗中文**。

## 搜过哪几个包 / 搜过哪些词

- **扫过**（`assets_full` 里带 `RectTransform/` 的 23 个包，我扫了 22 个）：`bundle_menus_assets_all`（全部读数出自这里）· `bundle_mainmenualwaysloaded_assets_all` · `bundle_scenes_scenes_mainmenuwarpforge` · `bundle_generalgamewindows_assets_all` · `bundle_staticgeneralassets_assets_all` · `bundle_duplicateassetisolation_assets_all` · `bundle_boosterpacks_assets_all` · `bundle_battlesharedresources_assets_all` · `bundle_battleprefabs_vfxandmisc_assets_all` · 13 个 `bundle_scenes_scenes_battlearena*`。**除 `menus` 外全部 0 命中**。**未扫**：`bundle_scenes_scenes_simpletransition`。
- **没有 `RectTransform/` 因而逻辑上不可能有 TMP ⇒ 未扫**：`bundle_deckselectionbuttons_assets_all` · `bundle_menusharedresources_assets_all` · `bundle_uiwarlords_assets_all` · `bundle_liveopsmenuimages_assets_all` · `bundle_menumusic_assets_all` · `bundle_prebuiltdecks_assets_all` 等（`deckselectionbuttons` 实测只有 Sprite/图，没有 `GameObject`/`RectTransform` 目录）。
- **GO 名搜过的词**：`Count` · `Card Name` · `Deck Selector Card Info button` · `Deck Selector Hero Card Info button` · `Deck List` / `Deck List drawer` / `Deck List Drawer`。
- **反编译搜过的词**：`DeckListDrawer__Reorder` · `DeckListDrawer__Initialize` · `DeckListDrawer__AddCardToList` · `DeckListDrawer__SpawnCardInfo` · `DeckListDrawer__SetOptions` · `DeckListOptions.*` · `DeckInfoPopup__Open` · `UIHeroCardInfoItem` · `RawCardScript__*`+`0x78`。
- **没用**旧解包 `d:/2/解包整理/` 与 `ui_extract/`（铁律 2：图与字段一律 `assets_full`）。

## 顺手发现（⛔ 一条都没改）

1. 🔴 **「按英文卡名排」不是排序代码，是数据**：`Shell/DeckInfoPopup.cs:1089-1092` 完全没排序，`deck.CardIds` **本身就存成英文名序**（截图逐行 `Anvirr Keltoc / Autarch / Death Spinner…`）⇒ 补上 `Sort(费用 → 显示名)` 就会盖掉这个观感。
2. **`Deck Selector Defensive Card Slot` 也有 `Count`**（`(0.9333…,1)` · auto `[2~32]` · base 36）—— 与 Card Info 族同值，我们防御卡行沿用同一套即可，**不冲突**。
3. ⚠️ **`RowCountW = 50f` 的出处是 Hero 族**（`sizeDelta.x = 50`）；Card Info 族的 `Count` 是 `Content/Text fill`（`HorizontalLayoutGroup`）里的子件，RT `sizeDelta = (0,0)`、靠 `LayoutElement.m_MinWidth = 22.87` 撑开。**但视觉右缘已核过（参考图 317 px vs 我们 ≈318，差 1.5px）⇒ 本轮不算缺陷**，只登记出处（`DeckRuntime.cs:141-146` 那段 doc 写的是 Hero 的锚点）。
4. `DeckListDrawer` 的 `Reorder` 尾部（`DeckListDrawer__Reorder.c:96-140`）显式 `SetSiblingIndex` 钉督军 → 0、防御卡 → 1（有督军）或 0（无督军）——「督军/防御卡在最前、与费用无关」这条**是它钉的**，出处可从 T 报告补成这条字节码。
5. `DeckRuntime.cs:1453` 的注释写「⛔ 别用 `Ink` = `(0.93,0.93,0.95)` —— 那不是原版这颗的数」；实测**原版就是 `0.9333` 灰**，与 `Ink` 几乎同色 ⇒ 那句「别用 Ink」至少**结论方向是误导**（真正该用的是 `0.9333`，不是 `1.0`）。
