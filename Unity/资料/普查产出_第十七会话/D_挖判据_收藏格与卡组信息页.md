# D_挖判据_收藏格与卡组信息页（只读诊断代理 `D-挖判据`，2026-10-11）

## 结论（一句话，两条分着写）

- **任务一 `A1503④`**：**两条都挖到判据了，而且结论都指向「不建」** —— ① `Ban Icon` 的触发条件拿到了（`Config` 里 `bannedImage` 的 `SetActive` + 谓词 `PlayEventData.IsCardBanned` + 4 张图的**换材质**转灰 + 名字色常量已按字节读出），但**它的数据源（禁卡表）在远端、本地恒空**；② 空位态 `Create` 的出现条件 = `Config(null)`，而**格数判据是机器码级的：`CollectionDisplay<T>.GetItemCount() = filteredCollection.Count`（无 +1、无 null 插入）** ⇒ 原版这一支**出不来**；③ `Create` 那张图的图名 = **原版本来就是空的**（`m_Sprite = {0,0}`，全库 3319 个 Sprite 无同名/同类图）。
- **任务二 `A1497④`**：**该有内容**。原版「卡组信息」页右侧**照样是卡池**（`Card Display` / `DeckEditorCollectionDisplay`），与「Cards」页**完全一样**；只有「Cosmetics」页才把它换成卡背收藏。判据 = `DeckEditingStateController__ToggleDeckInfo.c` → `DeckEditingWindow__ToggleDisplay(window, 1)`。

---

## 任务一 A1503④

### 目标格 = 收藏窗那份 `Collection Deck`（🔴 先钉死是哪一份）

- 收藏窗（`Collection Menu Variant`）→ `Tabs/Select Deck Tab/Decks Tab/Collection Display/Deck Scroll View`（`RecyclableScrollRect` MB `-8316412282010198741` ⇔ `MonoBehaviour_8316412282010198741.json`）的 **`PrototypeCell = -7203563689306584020`**，对象名 `Collection Deck`，`m_Father = 0`（**prefab 根**）。
- 另一份根 `-1941218197625697718` = **`Collection Deck With Highlight`**，是 `Deck Selection Popup with Tabs` / `Deck Selection Popup` 的 `PrototypeCell`（`MonoBehaviour_-8834611812657285761.json` / `MonoBehaviour_4496232554149549872.json`）。
- ⚠️ **`bundle_menus_assets_all` 里叫 `Collection Deck` 的对象有 4 份**（`menu_dump.py` 自己会出声「命中 4 个，取第一个」）⇒ **按名字取会取错**，必须按 `PrototypeCell` 的 RT pid 认。
- 两份的几何**逐字段相同**（`Game Mode Icon` 84.50×85.75 @(170.5,273.7) · `Ban Icon` 300×377.61 · `Create/Highlight` 298×420 `40k_bt_outline` 九宫 50 · `Text (TMP)` 200×50 fs32）；**只有 `useSelectedHighlight` 不同**（`Collection Deck` = **0** · `With Highlight` = **1**）⇒ **A1503 里「`useSelectedHighlight` 到底画不画金框」那条，先看用哪一份**（收藏窗用的是 =0 那份）。

### Ban Icon —— 触发条件 / 转灰口径 / 图名

**① 谁 SetActive（判据：`d:/2/tools/decomp_full/CollectionDeck__Config.c`，类 `CollectionDeck : CollectionItem<CardDeck>`）**

- 字段偏移（按 `CollectionDeck.cs` 声明序 + 用法逐条对，全部自洽）：`content`=0x68 · `highlight`=0x70 · **`createObject`=0x78** · `cardObject`=0x80 · `gameModeIcon`=0x88 · **`bannedImage`=0x90** · `greyscaleController`=0x98 · `easyMark/normalMark/hardMark`=0xa0/0xa8/0xb0 · `sortMode`=0xb8。
- `Config` 里：`SetActive(get_gameObject(bannedImage /*0x90*/), cVar4)`；同处 `greyscaleController /*0x98*/.ToggleGreyScale(cVar4)`，并把 `deckName /*0x48*/` 的色设成 `cVar4 ? 红 : 白`。
- `cVar4 = Enumerable.Any(deck.GetLibraryInFull(), c => deck.CustomGameModeEvent.IsCardBanned(c))`
  —— 谓词 = `CollectionDeck.__c__DisplayClass16_0___Config_b__0.c`（唯一一句 `PlayEventData__IsCardBanned`）。
- **整个 ban 块被 `if (deck.CustomGameModeEvent != null) {...}` 包着**（`CollectionDeck__Config.c` 那两行 `CardDeck__get_CustomGameModeEvent` 之后的分支）⇒ **没有绑定自定义/活动模式的卡组，这一段一行都不执行**。
- **什么叫 ban**：`Everguild.LiveOps.PlayEventData__IsCardBanned.c` = `该活动对象的 HashSet<…>(+0x60).Contains(card.GetId())`（卡 id 在活动的禁用集合里）。`GetLibraryInFull`（`CardDeck__GetLibraryInFull.c`）= 牌库 + 督军(+0x40) + 0x48 那件，插入到 list 头部 ⇒ **督军也一起判**。
- **颜色常量（按 `工具/read_literal.py` 读 `d:/2/unity_run_ref/GameAssembly.dll`，4 字节 float）**：
  `0x1834b2e50…e5c` = **(1, 1, 1, 1)**（没被 ban）· `0x1834b3210…21c` = **(1, 0.3, 0.3, 1)**（被 ban）—— 与 A1503④ 写的 `(1,0.3,0.3,1)` **逐位吻合**。
- 🔴 **本 build 这一支恒不真**：禁卡表来自 LiveOps 活动数据（远端 CCD），本地没有 ⇒ **永不触发**（与 2026-10-17 `A861`/`D16` 那条结论一致；只读代理只是把「谁 SetActive」这一半挖齐了）。

**② 4 张图转灰的实现口径 = 【换材质】，不是 tint**（判据 = 机器码/反编译，见下）

- `greyscaleController` = `UIImageGreyscaleController`，序列化字段 **`imageMaterialPairs`**（`MonoBehaviour_5378107046444829740.json`），**这一份 prefab 的 4 对**：
  | image（pid → 节点名） | materialGreyScale |
  |---|---|
  | `7280759307390454828` = **`Deck Image`**（卡背） | `-1503182876548073896` |
  | `-7442169067771229140` = **`Frame`** | `-4000214955162235591` |
  | `-3612853215106659284` = **`Faction Icon`** | `-4000214955162235591` |
  | `-1805363592664347604` = **`Game Mode Icon`** | `-4000214955162235591` |
- 两张灰材质的**真名**（`bundle_duplicateassetisolation_assets_all/Material/*.json`）：`Card Image Simple Greyscale`（卡背那张）· **`UI Greyscale`**（另三张）。
- 口径：`ImageMaterialPair__Initialize.c` 缓存 `originalMaterial = image.sharedMaterial`；`SetGreyScaleNonInstanceMaterial.c` / `SetOriginalMaterial.c` 各做 `image.material = materialGreyScale / originalMaterial`（vtable `+0x348`）⇒ **口径是「整体换材质」**，不是 tint（`Image.m_Material` 序列化值是 0）。
- `Banned Text`（`Ban Icon` 的子件）：TMP + **I2 `Localize`，`mTerm = "MenuDeck/Banned"`**（`MonoBehaviour_-1929125017957844.json`），出厂文本 `'Запрещено'`（俄语占位）；`m_fontSize 38.25 / m_fontSizeBase 36 / auto[0.25~72]`、对齐 `Center/Midline`、折行开、色 `(1,1,1,1)`、`m_characterSpacing 0`（`MonoBehaviour_4535457040223734828.json`）。

**③ `Ban Icon` 底图的图名 —— 拿到了**

- `CollectionDeck.bannedImage` 的 Image（MB `-2857002130615858132`）`m_Sprite = {m_FileID: 3, m_PathID: -4138590900053668541}` → 解析名 = **`40k_Cross_icon_cross_big Banned card`**（512×512 · `m_Type = Simple` · 无九宫 · `preserveAspect = 1`）。
- 出处：`d:/2/新解包资源/assets_full/bundle_duplicateassetisolation_assets_all/Sprite/40k_Cross_icon_cross_big Banned card.json`；atlas tag = **`0_GeneralUI Atlas`**（共 139 个 sprite）。

### 空位态 Create —— 出现条件 / 图名

**(a) 出现条件 —— 判据齐了，结论 = 原版出不来**

- **直接条件（反编译级）**：`CollectionDeck__Config.c` 里
  `SetActive(createObject /*0x78*/, deck == null)` · `SetActive(cardObject /*0x80*/, deck != null)`
  ⇒ **「列表里那一格没有卡组」= `Config(null)`**。
- **格数（🔴 机器码级，这次不再靠「方法体缺失」）**：`CollectionDisplay<object>$$GetItemCount`（`script.json` Address `22985296` = RVA `0x15eb4d0` · VA `0x1815eb4d0`）
  ```
  mov rax,[rcx+0x58]      ; filteredCollection
  mov eax,[rax+0x18]      ; List<T>._size
  ret
  ```
  ⇒ **格数 = `filteredCollection.Count`，没有 +1**。
- **`filteredCollection` 怎么来**：`CollectionDisplay<object>$$RefreshCollection`（RVA `0x15ecb70`）
  `filters != null ? filters.Apply(collection) : new List<T>(collection)` → 写入 `this+0x58` ⇒ **也不插 null**（同函数里另有一条旁证：`emptyWarning.SetActive(filteredCollection.Count <= 0)`）。
- **上游有没有塞 null**：`DeckCollectionTab.GetCollection` = `InventoryManager.GetInventory<CardDeck>(…).Where(筛选).ToList()`（`DeckCollectionTab__GetCollection.c`）；`DeckCollectionDisplay` **没覆写 `GetItems()`**（stub 只有 `ctor/OnDisable/SaveDeckOrder`）；选择卡组窗那两条 `DeckSelectionTabController__ShowOwnDecks/ShowPrebuiltDecks` = `Where(...).ToArray()`（**无 null**）⇒ 查得到的上游**一处都没有 null**。
- ⇒ **结论：原版这两扇窗的卡组格都出不来「空位格」** ⇒ 空位态 `Create` **不建**（`prefab` 里作者做出来了，但**默认 `m_IsActive = False` 且没有触发路径**）。
- 旁证（收藏窗「新建卡组」另有出处，不在格子里）：`SelectDecksTab`（`Collection Menu Variant/Tabs/Select Deck Tab`）的序列化字段里有 **`createDeck`**，实体 = `Select Deck Tab/Header/Control Buttons/Create`（Image sprite = `UI_Button_Mulligan`，pid `3819842738995756391`）。

**(b) `Create` 那张图的图名 —— 判据 = 「原版本来就是空的」**

- 该 Image（MB `9012401373763439660`，节点 `Collection Deck/content/Create/Create`）：
  `m_Sprite = {m_FileID: 0, m_PathID: 0}`、`m_Material = {0,0}`、`m_Type = 0(Simple)`、`m_RaycastTarget = 1`、`preserveAspect = 1`、rect 369.3×369.3（**比格子 250×405 还大**）。
- 🔴 **重新核过「全库无人引用」这条老结论**，按**图名/内容**找（不只是按引用找）：
  - `bundle_*/Sprite/*.json` **共 3319 个 sprite**，按 `m_Name` 搜 `create` / `new deck` / `newdeck` / `add deck` / `plus` / `empty` ⇒ **零命中**（只有 `40K_bt_addFriend`、`40k_ranking_icon_trophy Plus` 两个无关的）。
  - `d:/2/Warpforge_tools/data/ui_extract/`（5557 张切图）按文件名搜 `*creat*` ⇒ **零命中**。
  - `d:/2` 与 `d:/4/Unity/素材/` 按文件名搜 `*creat*.png/jpg` ⇒ **零命中**（只剩 Unity 包自己的文档图）。
- `CollectionDeck` 类**没有**指向这颗 Image 的序列化字段（字段表里找不到它），`Config` 也从不碰它 ⇒ **它只能是占位/命中区**（`m_RaycastTarget = 1`）。
- ⇒ **复刻口径：照做 = 画一个透明、`raycast` 开着的 Image**（⛔ 别去找一张不存在的图；也**别**把它当成页头那颗 `Create` 钮 —— 那颗用的是 `UI_Button_Mulligan`）。

---

## 任务二 A1497④ —— 该不该有内容

**答案：该有。原版「Info」页右侧【照样是卡池】，与「Cards」页完全一样。**（判据全部来自 `Deck Editing Menu` prefab + 反编译）

1. **页签实体**：`Deck Editing Menu/Content Area/Sidebar/Window Options/Buttons` 下三颗 `EverguildToggle` = **`Cards`（`isOn=1` 默认）· `Info` · `Cosmetics`**（Icon 依次 `40k_collection_bt_cards` / `40k_collection_bt_decks` / `40k_collection_bt_cosmetics`）。
2. **点 `Info` 会做什么**：`DeckEditingStateController__ToggleDeckInfo.c` → `DeckEditingWindow__ToggleDisplay(window, 1)` + `DeckEditingPanel__ToggleDeckInfo(panel)`。
3. **`ToggleDisplay` 的语义**（`DeckEditingWindow__ToggleDisplay.c`）：`cardDisplay(0x78).SetActive(param)` · `cosmeticDisplay(0x80).SetActive(!param)` · `wildcards(0xd0).SetActive(param)`。
   - 偏移表由 `MonoBehaviour_5918047927837519652.json`（`DeckEditingWindow`）的序列化字段序推：`cardDisplay` 起 11 个引用字段到 `wildcards` ⇒ `0x78 + 0x58 = 0xd0`，逐条自洽。
4. **对照**：`ToggleDeckList` 也是 `ToggleDisplay(1)`（**与 Info 相同**）；**只有** `ToggleDeckCosmetic` 是 `ToggleDisplay(0)`（换成卡背收藏）。⇒ **卡池只在「外观」页被换掉**。
5. **右侧内容实体**：`Content Area/Card Display` = `DeckEditorCollectionDisplay` + `EventRelay`，rect **(167.2,71)-(1920,1080) = 1752.83×1009.06**，**prefab 里 `m_IsActive` 默认就是开的**；其 `Scroll View` = `RecyclableScrollRect`，**(330.2,156)-(1920,1080) = 1589.78×924.06**。`Cosmetic Display`（`CardbackCollectionDisplay`）出厂 **INACT**。
6. **卡池里显示什么**：`DeckEditingWindow__FilterToDeck.c` → `CardCollectionFilterController.SetFiltersToDeck(filterCtrl, deck /*+0x118*/)` + `WildcardDisplay.Initialize(...)`；调用点 = `TryOpen` / `RefreshCards` ⇒ **默认过滤到本卡组**（`SetFiltersToDeck` 里逐条把每个 filter 应用到该 `deck`，见 `CardCollectionFilterController__SetFiltersToDeck.c`）。
7. **侧栏（`Deck Details` = `DeckEditingPanel`）在 Info 页显示什么**：`DeckEditingPanel__ToggleDeckInfo.c` ⇒ 隐藏 `deckList(0x28)`、隐藏 `deckCosmeticDrawer(0x30)`、显示 `deckCounterDrawer(0x38)`、显示 `deckCostDrawer(0x40)`（偏移由 `DeckEditingPanel__UpdateDrawers.c` 的 `DeckTypeCounterDrawer__Initialize(0x38)` / `DeckEnergyCostDrawer__Initialize(0x40)` / `DeckCosmeticDrawer__Initialize(0x30)` 钉死）。
   ⇒ 与我们现状**一致**：Info 页左侧 = `费用曲线` + `类型计数`。
8. ⇒ **缺的只有右侧**：应把**与「Cards」页同一份卡池**画在右侧（`Card Display` 那块 1752.83×1009.06 的整幅区、滚动区从 x=330.2 起），⛔ **不是「这一页什么都不该有」，也不是另做一套内容**。

---

## 搜过哪几个包 / 搜过哪些词（⛔ 报「没有」之前先看这张表）

| 结论 | 搜过哪里 | 搜过什么词 | 结果 |
|---|---|---|---|
| 没有名为 `Create` 的卡面图 | `d:/2/新解包资源/assets_full/bundle_*/Sprite/*.json`（**91 个包 / 3319 个 Sprite 全扫**） | `m_Name` 含 `create` / `new deck` / `newdeck` / `add deck` / `adddeck` / `plus` / `empty` | 0 / 0 / 0 / 0 / 0 / 1（`40k_ranking_icon_trophy Plus`）/ 4（全是 bar_empty 之类） |
| 同上 | `d:/2/Warpforge_tools/data/ui_extract/`（24 个目录 / 5557 张切图） | 文件名 `*creat*` / `*new*deck*` | 0 |
| 同上 | `d:/2` 与 `d:/4/Unity/素材/Warpforge原版/UI图集/` | 文件名 `*creat*.png/jpg/psd` | 0（只剩 Unity 自带包文档图） |
| 空位格触发路径 | `d:/2/tools/decomp_full/`（26282 个方法体） | `CollectionDeck`（9 个文件全读）· `CollectionDisplay`（只导出 `__c` 闭包 + `CollectionDisplay<object>$$*`）· `DeckCollectionTab` / `DeckCollectionDisplay` / `SelectDecksTab` / `DeckSelectionTabController` | 上游**无一处传 null** |
| 格数 | `d:/2/tools/il2cpp_out/script.json`（按 `Address` 查 RVA）+ `GameAssembly.dll`（pefile + capstone 反汇编） | `CollectionDisplay<object>$$GetItemCount` / `$$RefreshCollection` / `$$UpdateCollection` / `$$Initialize` | 机器码级：`Count` 原样返回，无 +1 / 无 null |
| 收藏窗用哪份 cell | `bundle_menus_assets_all` 的 `RecyclableScrollRect` | `PrototypeCell` | `Deck Scroll View`（收藏窗）= `Collection Deck`（5 列）；两个 Deck Selection Popup = `Collection Deck With Highlight`（6 列） |
| 卡组信息页右侧 | `bundle_menus_assets_all` 的 `Deck Editing Menu` 全树 + `DeckEditing*` 反编译 | `ToggleDisplay` / `cardDisplay` / `cosmeticDisplay` / `ToggleDeckInfo` | 判据齐，见上 |

---

## 没查清的部分（「没查清」+ 还差什么，⛔ 不填空）

1. **「没有绑定自定义活动的卡组，`Ban Icon` 会不会露出来」—— 没查清（两条硬事实互相顶住）**：
   - 实测：两份 cell prefab 的 `Ban Icon` GameObject **`m_IsActive = True`**（`GameObject/Ban Icon_5692977593749310508.json` 拥有 `CollectionDeck.bannedImage` 那颗 Image MB `-2857002130615858132`；`With Highlight` 那份同理 = `Ban Icon_417835447678995018.json`）。
   - 实测：`Config` **只在「有 CustomGameModeEvent」那一支里**才 `SetActive(bannedImage, …)`。
   - ⇒ 代码推不出「没活动时会不会被关掉」。**还差什么**：① `CollectionDisplay<CardDeck>` 之外还有没有别的地方关它（本次已把 `CollectionItem` / `CollectionDeck` 全部导出方法读完，只有 `Awake` 关 `deckDifficulty`）；② 或一张**普通卡组**的原版实拍（本 build 的活动数据在远端，实拍也难拿）。
   - 🔴 **给实现的纪律**：⛔ **别按「prefab 默认是开的」去实现**（那会让每张卡组都盖一个红十字）；按 `Config` 的判据实现即可（= 恒不画）。
2. **`A1497④` 的画法参数**（列数 / 行高 / 卡片尺寸）：本次只挖「该不该有」，**没挖卡池的版面参数**。还差什么：`Deck Editor Collection Display` 的 `RecyclableScrollRect` 参数（本次只读了收藏窗那份，编辑窗那份未读）——若要照做请另开一次。
3. **页头的 `Create` 钮（`SelectDecksTab.createDeck`）是否与「空位格」互斥**：`SelectDecksTab__CreateDeck.c` 存在但本次没读 ⇒ **没查清**。不影响上面的结论（空位格无触发路径）。

---

## 顺手发现

1. 🔴 **`Collection Deck` 有 4 份同名对象**（`menu_dump.py` 只出声提示「命中 4 个，取第一个」）——**收藏窗那份只有按 `PrototypeCell = -7203563689306584020` 才认得出**。`With Highlight` 那份的 `useSelectedHighlight = 1`、收藏窗那份 = **0**；A1503 里「`useSelectedHighlight` 画不画金框」那条**先确认在说哪一份**。
2. 🔴 **两扇窗的网格列数不同**（`RecyclableScrollRect._segments`）：**收藏窗 `Deck Scroll View` = 5**，两个 `Deck Selection Popup` = **6**；三者 `_cellWidth 225 / _cellHeight 364.5 / _spacingX 20 / _spacingY 0` 一致。⇒ A1503 ⑤/⑥ 若两窗共用一份实现，**列数不能当成同一个常量**。
3. `emptyWarning` 的显隐判据（机器码级，`CollectionDisplay<object>.RefreshCollection` @ VA `0x1815ecc64`）：**`filteredCollection.Count <= 0` ⇒ `SetActive(true)`**（节点 = 收藏窗 `Decks Tab/Collection Display/Empty Collection Warning`）。
4. `Deck Editing Menu` 的出厂态：`Deck Details` 下 `Deck List drawer` 是**唯一默认开的**，`Deck Information cost drawer`(×1.48) / `Deck Counter Drawer` / `Cosmetic Drawer` **都是 INACT**；`Cosmetic Display` 也 INACT ⇒ 与 `DeckEditingStateController__Initialize`（`isOn=true` 的那颗 + `ToggleDeckList`）**逐条吻合**。
5. ⚠️ **交接文档里的路径不存在**：`资料/施工单_卡组编辑*.md` **这一族没有**（`ls` 0 命中）。近名/同内容的是 `资料/普查产出_1017/现核_按文件施工表.md`（G1–G9）· `资料/普查产出_1017/W_G1_卡组编辑窗.md` · `资料/普查产出_1017/对账_卡组编辑部分_差异与待办.md`。⇒ 下次写简报别照抄那个路径。
6. ✅ **可复用的新手段**：`d:/2/tools/il2cpp_out/script.json` 的 `"Address"` 是 **RVA**（VA = `0x180000000 + Address`），配 `pefile`（RVA→文件偏移）+ `capstone`（本机都有）能**直接反汇编那 26282 个方法体之外的方法**。本次靠它把「基类方法体本地缺失」这一格**从「查不到」翻成「机器码级判据」**（`GetItemCount` / `RefreshCollection` / `UpdateCollection` / `Initialize` 全读出来了）。工具落地（脚本）本次没写（红线：只读），下一次若要可照上面三行复现。
7. **「命中 4 个」到底是哪 4 个（照做时别取错）**：`menu_dump.py bundle_menus_assets_all "Collection Deck"` 报的 4 个是 **GameObject**（`GameObject/Collection Deck.json`（无 pid 后缀 = 本包第一份）· `Collection Deck_-2024334647938937812` · `_521022471823197654` · `_8959905744735839870`）；其中**只有两个是 `RectTransform/` 里的 prefab 根**：`-7203563689306584020`（**收藏窗**，`m_GameObject = -2024334647938937812`）与 `-1941218197625697718`（`With Highlight`）。⇒ **要「收藏窗那一份」就用 `--rt -7203563689306584020`**，按名字会取到 `Collection Deck.json` 那一份（那份带 `UIScaleToFit`、`Ban Icon` 还是 INACT）。
