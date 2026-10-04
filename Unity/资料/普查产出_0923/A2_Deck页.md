# A2 — `Collection Menu Variant` / **Deck 页**（卡组收藏列表）「层 × 参数」表

> 只读普查，2026-09-23。**未改任何工程文件、未跑 Unity。** 坐标 = 绝对像素 · 1920×1080 · 左上原点 · y 向下（由 `RectTransform` 五元组机械算出）。
> 窗根 pid = **`2716193042033795797`**（`GameObject/Collection Menu Variant.json`，文件无 pid 后缀 ⇒ pid 由它的 RectTransform 反推）。
> ⚠️ 子树的父矩形**必须带 `Content Area` 的 167.17/70.94 内缩**，否则视口会算宽 167.2（本表已按绝对坐标重算）。

---

## 一、Deck 页在窗根下的路径

```
Collection Menu Variant [2716193042033795797]  comps: TabbedWindowComponents, CollectionScreen, MB
└ Content Area [8619238770403107541]  167.2,70.94  1752.8×1009.06
  ├ Background [3567167793339228885]
  ├ Tab Buttons [8952556270700192469]  167.2,158.64 165×921.36   ← 左侧竖排页签条（非横排）
  ├ Tabs [-3296323895330151723]                                   ← 无组件，纯容器
  │ ├ ★ Select Deck Tab [-8946133251602193707]  act=**F**         ← **本页**（Deck 钮的页）
  │ ├ CardsTab [-930198234167581995]            act=F
  │ ├ Cardback Tab [-4885685338377493803]       act=T             ← 出厂激活的是这个，不是 Deck
  │ ├ Alternate Art Tab [-7382280184577334571]  act=F
  │ └ Shared [6273146933576852181]              act=T（Close Button）
  └ Shadow (1) [1029317144425521877]  act=F
```
- **页名就叫 `Select Deck Tab`**（不是 `Deck Tab`/`Decks Tab`）；脚本 = **`SelectDecksTab`**（MB pid `-1218441358004526379`，在 `Select Deck Tab` 上）。
  它是 `CollectionScreen.tabs[0]`；`TabButtons.options` 里 `SelectDecksTab ↔ toggle 4690433759133426389 = Tab Buttons/Deck [GO -2132625767063887147]` ⇒ **Deck 页签 ↔ 本页**，闭环确认。
- `SelectDecksTab` 字段（决定页内 5 处显隐）：
  `unlockButton`=Unlock[7676841223838687957] · `createDeck`=Create[-1054674600977898795] · `importDeck`=Import[-3757846084208041259] ·
  `filterGameObjects`=[Filter Toggle **GO** -7050346372770700587, Header/Filters **GO** 7035875199920104149] ·
  `deckCollectionTab`=Decks Tab · `selectGameModeDeckTab`=Select Game Mode Deck Tab。
- `Setup()` 开场把 `unlockButton` 的 GO `SetActive(false)`；`ToggleSelectDeck()` 再把 `unlock/create/import/filterGameObjects[]` 全部 `SetActive(true)`、`selectGameModeDeckTab` `SetActive(false)`。
  `ChooseTabToOpen()`：**live 的 SelectGameMode 事件 ≥2 个 ⇒ 开 `Select Game Mode Deck Tab`，否则开 `Decks Tab`**（我们无服务器 ⇒ 走 `Decks Tab`）。

### 1·1 页内逐节点表（★ = 本页主链）

| 路径 | 名字 | pid | rect[x,y,w,h] | sprite / 文字 | 锚点 a/pv/pos/sd | 字号·色 | act | 组件 |
|---|---|---|---|---|---|---|---|---|
| /Tabs/Select Deck Tab | Select Deck Tab | -8946133251602193707 | 167.2,70.9,1752.8,1009.1 | — | (0,0)-(1,1) (.5,.5) (0,0) (0,0) | — | **F** | SelectDecksTab |
| ★../Decks Tab | Decks Tab | 192869081868329685 | 同页 | — | 同上 | — | T | DeckCollectionTab |
| ★../../Collection Display | Collection Display | 4855765916858770133 | 同页 | — | 同上 | — | T | DeckCollectionDisplay + EventRelay |
| ★../../../Deck Scroll View | Deck Scroll View | -6487742763938749739 | **330.9,155.9,1589.1,924.1** | — | (0,0)-(1,1) (.5,.5) (81.8545,-42.5) (-163.707,-85) | — | T | **RecyclableScrollRect** |
| ★../../../../Viewport | Viewport | -5809492718887509291 | 330.9,155.9,1589.1,924.1 | `UIMask` Sliced | (0,0)-(1,1) (0,1) (0,0) (0,0) | **色 a=0** | T | Image + **RectMask2D** |
| ★../../../../../Content | Content | 1826435340917596885 | 330.9,155.9,1589.1,**200** | — | (0,1)-(1,1) (0,1) (0,0) (0,200) | — | T | **GridLayoutGroup**（§三） |
| ../../Empty Collection Warning | Empty Collection Warning | 4562801026176574165 | 165.9,70.9,1804.1,1009.1 | — | (0,0)-(1,1) (.5,.5) (-57.5,42.5) (215,85) | — | **F** | — |
| ...../Warning | Warning | 6881822134541935317 | 同上 | TMP「There are no deck in your collection for the selected filters」 | (0,0)-(1,1) (.5,.5) (0,0) (0,0) | **fs 36**（auto 关，min18/max72）白 · `Localize=MenuCollection/NoDecksFound` | T | TMP + Localize |
| ★../Deck Filters | Deck Filters | 2918321924688569045 | **0.054,155.9,335.5,924.1** | `40k_main_tab_background` Simple | (0,0)-(0,1) (0,.5) (-167.116,-42.5) (335.497,-85) | 白 | T | Image + **DeckCollectionFilterController** |
| ..../Shadow | Shadow | -1024258460384042283 | 0.054,155.9,153,924.1 | `40k_main_tab_shadow` Simple | (0,0)-(1,1) (.5,.5) (-91.2432,0) (-182.486,0) | **col (0,0,0,0.314)** | T | Image |
| ..../Filters | Filters | -8996128322442435883 | 0.054,155.9,335.5,924.1 | — | (0,0)-(1,1) (.5,.5) (0,0) (0,0) | — | T | **VerticalLayoutGroup**（§三） |
| ....../Deck Name Filter | Deck Name Filter | 4745345138025358037 | 0.054,155.9,335.5,**80** | — | (0,1)-(0,1) (.5,.5) (167.749,-40) (335.497,80) | — | T | DeckNameFilter（字段 `inputField`） |
| ......../Input Field | Input Field | -1006242221631020331 | 27.2,175.9,281.3,**40** | `InputFieldBackground` Sliced | (.5,.5)-(.5,.5) (.5,.5) (-0,0) (281.283,40) | **col (0.0627,0,0,1)** | T | Image + **EverguildInputField**（SingleLine / limit 0 / caret 0.85 @1px / selection (0.659,0.808,1,0.753)） |
| ........../Text Area | Text Area | -7927519916597517611 | 37.2,182.9,231.3,27 | — | (0,0)-(1,1) (.5,.5) (-15,-0.5) (-50,-13) | — | T | RectMask2D |
| ............/Placeholder | Placeholder | 5418305286832775893 | 37.2,177.9,231.3,37 | TMP「Search」 | (0,0)-(1,1) (.5,.5) (0,0) (0,0) | fs **auto 10–35** 白 · `Localize=MenuDeck/HUD/SearchFilter` | T | TMP+Localize+`EverguildTextController` |
| ........../Image（放大镜） | Image | 2096359483932009173 | 268.4,180.9,35,30 | **`40k_icon_search`** Simple preserveAspect | (1,0)-(1,1) (1,.5) (-5,0) (35,-10) | 白 | T | Image |
| ....../Army Filter | Army Filter | 6001113345737679573 | 0.054,235.9,335.5,**345** | — | (0,1)-(0,1) (.5,1) (167.749,-80) (335.497,345) | — | T | DeckArmyFilter（13 选项，见 §1·2） |
| ......../Title | Title | -1201852215974960427 | 25.1,240.9,310.5,50 | TMP「Army」 | (0,1)-(1,1) (0,.5) (25,-30) (-25,50) | **fs 36** auto 10–36 白 | T | TMP+Localize+EverguildTextController |
| ......../Content | Content | 4263534123514192597 | 0.054,300.9,335.5,280 | — | (0,0)-(1,1) (.5,.5) (0,-32.5) (0,-65) | — | T | **GridLayoutGroup**（§三） |
| ........../Toggle ×13 | Toggle | -2000671055927188779 等 | 14.05,300.9,**100×100** | 见 §1·2 | (0,1)-(0,1) (.5,.5) (64,-50) (100,100) | — | T | EverguildToggle + CollectionFilterToggle |
| ../../../Select Game Mode Deck Tab | Select Game Mode Deck Tab | 3026728223846884053 | 330.8,155.9,1589.2,924.1 | — | (0,0)-(1,1) (.5,.5) (81.809,-42.5) (-163.618,-85) | — | **T** | SelectGameModeDeckTab（`Title Header` TMP「SELECT GAME MODE DECK」fs 72；`Scroll View` 1589.2×802.3 + Content HLG spacing 50 align 3 + CSF） |
| ★../Header | Header | -7841323304764710187 | 167.2,70.9,1752.8,**85** | — | (0,1)-(1,1) (.5,1) (0,0) (0,85) | — | T | NonDrawingGraphic |
| ..../Filter Toggle | Filter Toggle | -7050346372770700587 | 367.2,88.4,**50×50** | `40k_menu_bt` Sliced | (0,0.5)-(0,0.5) (.5,.5) (225,0) (50,50) | 白 | T | Image + EverguildToggle + MaterialModifier |
| ....../icon detail | icon detail | 2009059056579019477 | 377.2,98.4,30,30 | `40k_bt_icon_search` Simple pAspect | (0,0)-(1,1) (.5,.5) (0,0) (-20,-20) | 白 | T | Image |
| ....../label | label | -5535392421861989675 | 437.2,88.4,150,50 | TMP「Filters」 | (1,0)-(1,1) (1,.5) (170,0) (150,0) | **fs 42** auto 10–42 白 | T | TMP+Localize+EverguildTextController |
| ..../Control Buttons | Control Buttons | 4125381626734568149 | 1180,80.9,**740×60** | — | (1,0)-(1,1) (1,.5) (0,2.5) (740.01,-25) | — | T | **HLG** + CSF(H=Preferred)（§三） |
| ....../Create | Create | -1054674600977898795 | **1661,80.9,245×60** | `UI_Button_Mulligan` Simple | (0,1)-(0,1) (.5,.5) (603.51,-30) (245,60) | 白 | T | Image+EverguildButton | 
| ......../Button Text | Button Text | -2754057212743392555 | 1672.7,82.2,220.8,57.5 | TMP「**Create Deck**」 | (0.0355,0.099)-(0.9613,0.903) (.5,.5) (0,-0.0606) (-6,9.283) | **fs 42** auto 10–42 白 | T | TMP+Localize+**AspectRatioFitter**+EverguildTextController |
| ....../Import | Import | -3757846084208041259 | **1391,80.9,245×60** | 同 Create | (0,1)-(0,1) (.5,.5) (333.51,-30) (245,60) | — | T | 同上 → 文字「**Import Deck**」 |
| ....../Unlock | Unlock | 7676841223838687957 | **1180,80.9,186×60** | `40k_menu_bt_general_bg` Sliced col(0.212,0.094,0.098,1) | (0,1)-(0,1) (.5,.5) (93.005,-30) (186.01,60) | — | T | Image+EverguildButton |
| ......../Button Outline+Text | — | -8849062637360586027 / -220370555535629611 | 1180.5,80.9,185×60 | `40k_menu_bt__general_outline` / TMP「Debug Unlock」fs 35.75 | — | — | T | **三张图的 `m_Enabled` 全 = 0 ⇒ 整件不可见**（见 §四） |
| ..../Separator Line | Separator Line | 7822558757466989269 | 167.2,150.9,1752.8,**10** | `40k_main_line` Sliced | (0,0)-(1,0) (.5,.5) (0,0) (0,10) | 白 | T | Image |
| ..../Filters | Filters | 7035875199920104149 | 592.2,70.9,**0**×85 | — | (0,0)-(0.5,1) (0,.5) (425,0) (-876.415,0) | — | T | **GridLayoutGroup** + CSF(H=Preferred)（§三） |
| ....../Clear Filter Button | Clear Filter Button | -4297683129125510443 | **612.2,83.4,250×60** | `UI_Button_Mulligan` Simple | (1,0.5)-(1,0.5) (0,.5) (20,0) (250,60) | 白 | T | Image+EverguildButton+**LayoutElement(m_IgnoreLayout=1)** → 文字「Clear filters」fs 42 auto 10–42 |

### 1·2 左栏「阵营筛选」= 状态 → 图（`DeckArmyFilter.options`，13 条，全部落在一处）

| option | locKey | 图标 sprite |
|---|---|---|
| 10 | Armies/Ultramarines | `40k_DeckSelection_icon_FactionUM` |
| 20 | Armies/Goff | `40k_DeckSelection_icon_FactionOrks` |
| 30 | Armies/SaimHann | `40k_DeckSelection_icon_FactionSaimHann` |
| 40 | Armies/Sautekh | `40k_DeckSelection_icon_FactionSautekh` |
| 50 | Armies/BlackLegion | `40k_DeckSelection_icon_FactionBlackLegion` |
| 60 | Armies/Leviathan | `40k_DeckSelection_icon_FactionLeviathan` |
| 70 | Armies/TauEmpire | `40k_DeckSelection_icon_FactionTauEmpire` |
| 80 | Armies/Sororitas | `40k_DeckSelection_icon_FactionSororitas` |
| 90 | Armies/Genestealers | `40k_DeckSelection_icon_Genestealers` |
| 100 | Armies/AstraMilitarum | `40k_DeckSelection_icon_FactionAstraMilitarum` |
| 110 | Armies/DarkAngels | `40k_DeckSelection_icon_FactionDarkAngels` |
| 120 | Armies/EmperorsChildren | `40k_DeckSelection_icon_FactionEmperorsChildren` |
| 130 | Armies/SpaceWolves | `40k_DeckSelection_icon_SpaceWolves` |

- 图源包 = `bundle_deckselectionbuttons_assets_all`（与选卡组弹窗同一批图）；**prefab 里 `Toggle/Background` 填的是 option=20 的 `..._FactionOrks`（模板值，运行时代码按 option 换）**。
- `togglePrefab` = MB `5415584153421603541`（`CollectionFilterToggle`）；`DeckCollectionFilterController.hiddenPosition = (-550, 0)`（收起时滑到屏幕左侧外）。
- 页签条：`Tab Buttons` 4 钮各 **165 宽 × 180 高**；`Tab Buttons/Deck` 子件 = `Highlight`（`40k_main_bt_selected BW` Sliced **col(1,0,0,1)** PPU×0.92）/ `Icon`（`40k_collection_bt_decks`）/ `Label`（`40k_main_bt_nametag`，子 TMP「Deck」**fs 36** auto 5–36 col(0.957,0.882,0.675,1)）/ `Badge Highlight`（`40K_notification_number` col 0.736 灰）。

---

## 二、`Collection Deck` 卡组格

### 2·1 先按 pid 分辨同名族（4 个 `Collection Deck` + 1 个 `With Highlight`，**全在 `bundle_menus_assets_all`**）

| 文件 | 真 pid | father | 尺寸 | 它是什么 |
|---|---|---|---|---|
| `Collection Deck.json` | -8249462987040093541 | 有 | 328×496.97 | 打折弹窗 `Deck Drawer/Content` 里的**实例** |
| `Collection Deck_-2024334647938937812.json` | **-2024334647938937812** | **0** | **250×405** | ★ **prefab 根 = 卡组页用的那一个** |
| `Collection Deck_521022471823197654.json` | 521022471823197654 | 有 | 328×496.97 | 报价弹窗 `Deck Drawer/Content` 实例 |
| `Collection Deck_8959905744735839870.json` | 8959905744735839870 | 有 | 328×496.97 | `Deck Drawer/Content` 实例 |
| `Collection Deck With Highlight.json` | -7515975617742763446 | **0** | 250×405 | 另一支 prefab（**兄弟件，不是本页用的**） |

- ★ 判定依据：`Deck Scroll View` 的 `PrototypeCell` = RT `-7203563689306584020`，该 RT 的 GO 就是 **-2024334647938937812** ⇒ **本页格子 = 这个 pid**。
- 与 `Collection Deck With Highlight` 的**唯一实质差别**：根上 `useSelectedHighlight` **0 vs 1**（`useHoverHighlight` 两边都是 1），其余子件名字/五元组逐一相同（子 pid 不同，因为各是一份拷贝）。

### 2·2 逐子件表（rect 相对卡面 250×405 左上角；★「显示」= ×0.9 后的实际屏幕尺寸）

| 名字 | pid | 相对 rect[x,y,w,h] | 显示 rect（×0.9） | sprite | 组件 | 出厂 act |
|---|---|---|---|---|---|---|
| **Collection Deck**（根） | -2024334647938937812 | 0,0,250,405 | **225×364.5** | — | CollectionDeck + DragAndDropItem(threshold 50, speed 1, autoInit 0) + UIImageGreyscaleController | T |
| content | 5455182769096854572 | 0,0,250,405 | 同根 | — | 纯 RectTransform（`CollectionDeck.content`，**hover/排序都改它的 localScale**） | T |
| Deck（=`cardObject`） | -7329589175331746772 | 0,0,250,405 | 同根 | — | — | T |
| Deck/Highlight | -1406404812714046420 | **-19.8,-10.9,289.8,427.3** | 260.8×384.6 | **`Highlight Rounded Square`** Sliced **col(1,0.773,0,1)** | Image | **F** |
| Deck/Deck Image(mask) | 8534957316135685164 | 11,26.1,228,306 | 205.2×275.4 | — | **RectMask2D**（无 Image） | T |
| Deck/Deck Image | -2372406319230710740 | -49.4,6.2,347.7,346.4 | 313×311.8 | **sprite=0（运行时代码填卡背）** | Image（`CollectionDeck.deckCardback`） | T |
| Deck/Frame | -2486819420341367764 | 2,17,246,368 | 221.4×331.2 | **`40K_bt_deck`** Simple | Image（`m_Sprite` pid -6797400521842476553） | T |
| Deck/Deck Name | 3187256796294974508 | 20,344.2,210,39.7 | 189×35.7 | TMP「Deck name」 | TMP + **EverguildTextController(max 32/min 8)** | T |
| Deck/Faction Icon | 7325146344951876652 | -10.5,273.7,84.5,85.7 | 76.1×77.1 | sprite=0 preserveAspect | Image（`deckFaction`） | T |
| Deck/Game Mode Icon | -222716274864845780 | 170.5,273.7,84.5,85.7 | 76.1×77.1 | sprite=0 preserveAspect | Image（`gameModeIcon`） | T |
| Deck/DificultyLevel | -6338601443282351060 | 159.2,15.7,84.9,84 | 76.4×75.6 | **`Menu_Icon_Gallons_1`** preserveAspect | Image（`deckDifficulty`） | **F** |
| Ban Icon | 5692977593749310508 | -25,-25.1,300,377.6 | 270×339.8 | **`40k_Cross_icon_cross_big Banned card`** pAspect，raycast=0 | Image（`bannedImage`） | T |
| Ban Icon/Banned Text | -4401652351331365844 | 20,125.9,210,75.5 | 189×68 | TMP「Запрещено」 | TMP + `Localize=MenuDeck/Banned`（`EverguildTextMeshPro`） | T |
| Create（=`createObject`） | -2123338327778685908 | 0,0,250,405 | 225×364.5 | — | — | **F** |
| Create/Highlight | -8398793529741046740 | **-24,-7.5,298,420** | 268.2×378 | **`40k_bt_outline`** Sliced **preserveAspect** | Image | T |
| Create/Create | -1264017207224136660 | -59.6,17.9,369.3,369.3 | 332.4×332.4 | **sprite=0**（prefab 没填，见 §四） | Image preserveAspect | T |
| Create/Text (TMP) | -5512211067309028308 | 25,292.5,200,50 | 180×45 | TMP「**Create\nNew Deck**」（两行） | TMP | T |

字体/色：`Deck Name` fs 32（auto 8–32）白 **charSpacing -2**；`Banned Text` fs 38.25（auto 0.25–72）白；`Create/Text` fs 32 白。三者的 `m_HorizontalAlignment` 原值都是 **2**（口径见 §四）。

### 2·3 状态差异（**读的是赋值点**：`CollectionDeck.Config / SelectedHighlight / HoverHighlight / SetSortMode`，不是序列化默认值）

| 状态 | 触发 | 多显 | 少显 | 参数（原版值） |
|---|---|---|---|---|
| **有卡组** | `Config(deck)`，`deck != null` | `cardObject`（=Deck 全套） | `createObject` | `Deck Name.text=deck.Name`；`Deck Image.sprite=卡背`（Addressables 载）；`Faction Icon.sprite=ArmyIconsSO.GetArmyIcon(army)`；`Game Mode Icon.sprite=GetGameModeIcon(GameMode)` 且 **`enabled = (icon != null)`**；`highlight.SetActive(false)` |
| **空位 / 新建** | `Config(null)` | `createObject`（Create 全套） | `cardObject` | 文字「Create\nNew Deck」；`Create/Create` 图由别处填（**查不到**） |
| **选中** | `SelectedHighlight(true)` | `Deck/Highlight`（金 `Highlight Rounded Square`） | — | 受 `useSelectedHighlight` 门控（本 prefab = **0** ⇒ 本页不显示选中高亮；`With Highlight` 那支 = 1） |
| **悬停** | `HoverHighlight(true)` | —（**只改缩放**） | — | `content.localScale = 基准 × **1.05**`（常量 `DAT_1834b3254`） |
| **排序/拖拽** | `SetSortMode(true)` | — | —（抑制 hover 与选中高亮：`sortMode` 为真时两个 Highlight 函数直接 return） | `content.localScale = (**0.8, 0.8, 0**)`（常量 `DAT_1834b2fa0` = 0.8） |
| **被 Ban** | 该卡组在某模式被 ban（`GetLibraryInFull().Any(...)`） | `Ban Icon` + `Banned Text`；4 张图**转灰** | — | `UIImageGreyscaleController.ToggleGreyScale(true)`（**pairs 恰好 4 张**：卡背 `Deck Image` / `Frame` / `Faction Icon` / `Game Mode Icon`）；**`Deck Name` 颜色 = (1, 0.3, 0.3, 1)**（正常 = 白 (1,1,1,1)，常量 `0x1834b2e50…` vs `0x1834b3210…`） |
| **难度标** | 仅当 `prebuilt deck` 存在 + 特性开关开（+ `DeckCollectionDisplay.displayDifficultyLabel`） | `DificultyLevel` | — | 图按 `PrebuiltDeck.difficulty`：**0 或 5 → `Menu_Icon_Gallons_1`（easy）· 10 → `_2`（normal）· 15 → `_3`（hard）**，其余为 null |
| **列表空** | 筛选后无卡组 | `Empty Collection Warning`（TMP 见 §1·1，fs 36） | — | `DeckCollectionDisplay.emptyWarning` / `autoSelect=0` / `allowItemDrag=1` / `displayDifficultyLabel=0` |

---

## 三、布局组的原值（这几个节点的子件位置**不是** JSON 值）

| 节点 | 组 | 原值 |
|---|---|---|
| `Deck Scroll View/Viewport/Content` | **GridLayoutGroup** | `m_CellSize (225, 364.5)` · `m_Spacing (20, 0)` · `Padding L10/R0/T0/B0` · `Constraint = 0 (Flexible)` · `ConstraintCount 2（Flexible 时无效）` · `StartCorner 0(UpperLeft)` · `StartAxis 0(Horizontal)` · `ChildAlignment 0(UpperLeft)` |
| `Deck Scroll View`（同上的滚动器） | **RecyclableScrollRect** | `IsGrid 1` · `_useFixedCellSize 1` · `_cellWidth 225` · `_cellHeight 364.5` · `_spacingX 20` · `_spacingY 0` · `_segments 5` · `_controlSegmentSize 1` · `_poolSize 114` · `_mobileSizeScale 1.25` · `m_MovementType 1` · `m_Elasticity 0.1` · `m_DecelerationRate 0.135` · `m_ScrollSensitivity 100` · `PrototypeCell` → **Collection Deck 根** |
| `Deck Filters/Filters` | **VerticalLayoutGroup** | `spacing 0` · `Padding 全 0` · `ChildAlignment 0(UpperLeft)` · `ChildControlWidth 1` · `ChildForceExpandWidth 1` · `ChildControlHeight 0` · `ChildForceExpandHeight 0` · `ChildScale W/H 0` · `ReverseArrangement 0` |
| `Army Filter/Content` | **GridLayoutGroup** | `m_CellSize (100, 100)` · `m_Spacing (7, 0)` · `Padding L14` · `Constraint 0(Flexible)` · `ChildAlignment 0` |
| `Tab Buttons` | **VerticalLayoutGroup** | `spacing 0` · `Padding Top 30` · `ChildAlignment 1(UpperCenter)` · `ChildControlWidth 1` · `ChildForceExpandWidth 1` · `ChildControlHeight 0` · `ReverseArrangement 0` |
| `Header/Control Buttons` | **HorizontalLayoutGroup** | `spacing 25` · `Padding R14` · `ChildAlignment 5(MiddleRight)` · **`ReverseArrangement 1`** · `ChildControlHeight 1` · `ChildForceExpandW/H 1` + `ContentSizeFitter(H=Preferred, V=Unconstrained)` |
| `Header/Filters` | **GridLayoutGroup** | `m_CellSize (85, 70)` · `m_Spacing (5, 0)` · `Constraint 2(FixedColumnCount)` / `count 1` · `ChildAlignment 3(MiddleLeft)` + `CSF(H=Preferred)` —— ⚠️ 唯一子件 `Clear Filter Button` 带 `LayoutElement.m_IgnoreLayout=1` ⇒ **这一格计算不生效**，按钮按自己的 250×60/anchors 摆 |
| `Header/Filters/Clear Filter Button` | — | 实测落点 **612.2,83.4**（=「Filters」文字条右缘 587.2 + 25）· 250×60 |
| `Content` 的列数 | — | 视口宽 **1589.1**：`6×225+5×20+10 = 1460 ≤ 1589.1`；7 列需 `7×225+6×20+10 = 1695 > 1589.1` ⇒ **GridLayoutGroup 口径 = 6 列**（⚠️ 与 RecyclableScrollRect 的自算算法可能不一致，见 §四） |
| `Army Filter/Content` 列数 | — | 宽 335.5 − pad 14 = 321.5：`3×100+2×7 = 314 ≤ 321.5` ⇒ **3 列** |

---

## 四、查不到的 / 冲突的

1. 🔴 **`RecyclableScrollRect.Initialize()`（0 参）的方法体在本机反编译产物里缺失** —— `decomp_full/PolyAndCode.UI.RecyclableScrollRect__Initialize.c` 是**1 参**那版（内部再调 `Initialize(...,0)`），**0 参版本与之撞名被覆盖**。⇒ **卡组格的列数算法读不到**；本表只能给「GridLayoutGroup 口径 = 6 列」并说明它可能不是最终裁决者。**还差什么**：重导该方法（真包 IL2CPP）或跑实况量。**没有拿序列化默认值顶替**。
2. ⚠️ **9 个 `RecyclableScrollRect` 里只有本页的 `Content` 挂了 GridLayoutGroup**（其余 8 个：Cards/Cardback/Alternate Art/Open Alliances 等都没有）⇒ 这条 GridLayoutGroup 是不是真在跑，**静态判不出**。
3. **`Create/Create` 的图标 sprite 在 prefab 里是 `m_Sprite: 0`**，全 bundle 里也**没有任何 MB 引用它** ⇒ 由代码按 Addressables 名加载，**具体图名查不到**（`Create/Highlight` 用的是 `40k_bt_outline`，那条是确定的）。
4. **`Unlock`（Debug Unlock）按钮**：`m_IsActive = True`，但**它自己 + Button Outline 的 Image + Text 的 TMP 三张图的 `m_Enabled` 全 = 0** ⇒ **出厂不可见**；什么时候变可见**查不到**（`SelectDecksTab.DEBUGQAUnlock` 只解锁、不改可见性）。别照抄成「看得见」。
5. ⚠️ **`m_HorizontalAlignment` 的读数口径有冲突**：本表 **照抄 JSON 原值**（`2`）。`menu_dump.py` 会印 `Right`，但 TMP 的 `HorizontalAlignmentOptions` 是**位标志**（Left=1 / Center=2 / Right=4 / Justified=8 / Flush=16）⇒ 原值 2 的语义应是 **Center**。本会话**只读普查、没跑 Unity**，没到实况核 ⇒ 标为冲突项，施工前建议用实况/截图定一次（本页涉及：Deck Name / Banned Text / Create 文字 / Create·Import·Clear 三个按钮文字 / Warning / 页签 Label）。
6. **`Deck Filters` 侧栏的显隐时机**：`DeckCollectionFilterController.hiddenPosition = (-550,0)` 说明有滑出/滑入动画，但**哪一帧在哪个状态**（出厂是否展开、`Filter Toggle` 按下后动的是哪几个节点）**只有这一个字段做证据，没查到触发点**。
   - ✅ **2026-10-06 订正（铁律 5）：触发点已经查到了** —— **`SetupFilters`（VA `0x1815F0740`）**那条链：它把抽屉摆到 `hiddenPosition` 那一头 ⇒ **原版起手是【收起】的**（不是「我们挑的」）。判据 → `资料/已知的坑.md` 2026-10-05 那节 · 落地 → `项目任务.md` §三 第 29 条 **A101**。
7. `SelectDecksTab.Setup` 里 `importDeck` 的可交互性由 `InventoryManager.GetOwnedCount(...)` 与某个 SO 的阈值比较决定（`Selectable.set_interactable`），**那个 SO 的阈值没查**（不影响版面）。
8. ⚠️ **`menu_dump.py` 的 HLG/VLG 标签是猜的**（它按「有 `m_Spacing` + `m_ChildControlWidth`」判 HLG）：实测 **`Tab Buttons` 是 `VerticalLayoutGroup`、`Deck Scroll View/Content` 与 `Army Filter/Content` 是 `GridLayoutGroup`** —— 一律以本表第二节/§三的原始 JSON 为准。
9. 本页**没有**原版实况截图/运行时 dump 做交叉验证（本次是只读普查，未跑 Unity）。

---

## 五、跑过的命令（全部只读）

```
# sprite pid → 名字（读真包，缓存已存在）
python -c "json.load(open('d:/4/_tmp_view/sprite_pids_ALL.json'))"      # 用现成缓存反查

# 结构与参数（pid = 真 pid，绕开「同名取第一个」）
D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/menu_rect.py bundle_menus_assets_all 2716193042033795797 --depth 5
D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all -2024334647938937812 --depth 7
D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all 2716193042033795797 --depth 3
# ⚠️ menu_rect 直接喂子树会把 Content Area 的 167.17/70.94 内缩漏掉 ⇒ 本表的 rect 全部用
#    「从窗根整棵走 + menu_rect.rect_of」重算过（inline python heredoc，未落盘脚本）

# 组件类型（MonoScript pid → 类名）
d:/2/新解包资源/assets_full/bundle_Waprforge_monoscripts/MonoScript/*.json   （722 个）
# CollectionDeck / SelectDecksTab / DeckCollectionTab / DeckCollectionDisplay / DeckArmyFilter 字段
d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_*.json
# 状态与赋值点
d:/2/tools/decomp_full/CollectionDeck__{Config,Setup…}.c 等（见 §2·3）
# 常量
D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/read_literal.py d:/2/unity_run_ref/GameAssembly.dll \
   0x1834b3254 0x1834b2fa0 0x1834b2e50 0x1834b3210      # 1.05 / 0.8 / 白 / (1,0.3,0.3,1)
```
