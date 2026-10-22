# D3 · `DemoDeckInfoSO` 成对表（练习窗「一套两副」）

> 执行代理 D3，2026-10-22。**未跑 Unity、未改运行时/正本**，只读 `d:/2`。
> 产物：`d:/4/Unity/工具/gen_demo_deck_pairs.py` · `d:/4/Unity/数据/游戏数据/demo_deck_pairs.json`
> （md5 `25390a7cad194addb7dd65bbb71d952e` · 141,426 B · LF · `--check` 通过）

---

## 〇、结论（一句话）

`DemoDeckInfoSO` **84 份全解出来**（`menus_assets_all` 82 + `duplicateassetisolationso_assets_all` 2），
其中 **78 份是练习窗的「一对两副」（classic + skirmish），6 份是教程（只有 classic）**。
78 对 = **156 个 deck 引用、156 个互不重复的 deckId**：
**78 个落进我们的 103 副池，78 个落不进** —— 落不进的那 78 副是 `isPracticeDeck == 0` 的**另一批副**，
**不是我们池里那些副的别名**，但**卡表全都在 `数据/游戏数据/decklists.json` 里**（`inDecklists: true`）。

| 计数 | 值 | 判据 |
|---|---|---|
| `DemoDeckInfoSO` 实例 | **84** | 按 `m_Script` 类名筛（不是按字段猜） |
| 其中练习窗一对两副 | **78** | 与 `PracticeModePopup.decks` 数组**逐项相等**（见 §一·4） |
| 其中教程（只有 classic） | **6** | `tutorialIndex` = 0..5 各一个 |
| 练习对 × 两副 = deck 引用 | **156** | 156 个 deckId **互不重复** |
| 落进 `prebuilt_decks.json`（103 副） | **78**（39 classic + 39 skirmish） | **只按 deckId 精确命中** |
| 落不进 | **78**（39 classic + 39 skirmish） | 见 §四 |
| 整对在池 / 半对 / 整对不在池 | **23 / 32 / 23** | 23+32+23 = 78 |
| 教程 6 份的 classic 落点 | **6/6** 落到 `tutorial_decks.json` | `ourSource = "tutorial_decks"` |

---

## 一、怎么找到的（可复跑，全部现场读原版包）

### 1·1 类名对表（先认脚本，再认实例）

`DemoDeckInfoSO` 的 MonoScript 在 **`Waprforge_monoscripts.bundle`**（⚠️ 原包名就是少一个 `r` 的这个拼写），
包内唯一 CAB = **`CAB-17b1b5f865c595045b5fe42b7f8c8967`**，722 个 MonoScript。
本 build 里可用的三个坐标：

| 类 | 包内 MonoScript pathID |
|---|---|
| `DemoDeckInfoSO` | **-8258122028049581671** |
| `PracticeModePopup` | **580221229322215624** |
| `PrebuiltDeck` / `PrebuiltSortedDeck` | 各自可查（`m_Script` → 类名） |

⚠️ 注意 **`assets_full/globalgamemanagers/MonoScript/MonoScript_3631.json` 那个 3631 是「全局表里的序号」，
不是 bundle 里的 pathID** —— 真包里引用的是上面那个 64 位 hash 值。只按 3631 去 grep 会 0 命中。

### 1·2 三个包 / 三个 CAB（PPtr 一律先解 `m_FileID`，⛔ 不按 pathID 全库扫）

| 东西 | bundle | CAB | 对象数 |
|---|---|---|---|
| `DemoDeckInfoSO` | `menus_assets_all.bundle` | `CAB-f67aac7dfa53acf12e1bcb16a2f33904` | **82** |
| `DemoDeckInfoSO` | `duplicateassetisolationso_assets_all.bundle` | （该包自己的 CAB） | **2**（`UM Tutorial` / `Orks Tutorial`） |
| `PrebuiltDeck` / `PrebuiltSortedDeck` | `prebuiltdecks_assets_all.bundle` | **`CAB-dbb6f14c119af496b058533ae98dc790`** | **236**（224 + 12） |
| `armyIcon` 那 17 张 Sprite | `armyicons_assets_all.bundle` | **`CAB-159fa22599e5911bc9d76e52c875bd73`** | 17 |

`classicDeck` / `skirmishDeck` / `armyIcon` 的 `m_FileID` 是 **1-based** 下标进 `externals`；
脚本对三件事**硬断言**：两个 deck PPtr 必须落到 `CAB-dbb6…`；`armyIcon` 必须落到 `CAB-159f…`；
落地对象必须在目标包里按 pathID 找得到。**84/84 全部通过**（`armyIcon` 无一条为空）。

`DemoDeckInfoSO` 的 pathID 是 hash 型、**散在 int64 全段**：84 个里 38 个负数，
区间 `-9091896766974745539 … 8836832719224497473` —— 所以「按 pathID 排序当顺序」是没有意义的，
产物里的顺序用 §一·4 那个真实 UI 顺序。

### 1·3 `assets_full` 那批是**旁证**、不是源

任务简报说这批 SO 不在 `assets_full` —— **实测是在的**：`bundle_menus_assets_all/MonoBehaviour/` 82 份 JSON
+ `bundle_duplicateassetisolationso_assets_all/MonoBehaviour/` 2 份（2026-10-19 界面线 P05 已经用过）。
本脚本**以真包为准**，只用它做一次「名字集合是否一致」的旁证 ⇒ 产物 `source.assetsFullCrossCheck =
{"soNames": 84, "sameNameSet": true}`。

### 1·4 「78」是**数出来的**，不是抄的

`menus_assets_all.bundle` 里 `PracticeModePopup` 组件（pathID **-5721221448436461764**）的序列化
`decks` 数组**恰好 78 项**、`m_FileID` 全 0（同文件）、78/78 全部命中一个 `DemoDeckInfoSO`。
脚本把它采成 `uiOrder`（练习窗左列的真实顺序），并断言
**「这 78 项 == 两副齐全的那 78 份 SO」**（集合相等，不是数量相等）—— 通过。
⇒ 练习窗那一列就是这 78 份，顺序也拿到了。

---

## 二、78 对：结构长什么样

- **一套两副，不是两套独立卡组**：78 对里 **67 对 classic 与 skirmish 的 `deckName` 完全相同**
  （同一副牌的 30 张/12 张两个版本），11 对名字不同（原版自己就不同，不是我们抄错）。
- 每对都共用一个 `loreLocalizationKey`（原版一个 SO 一条 lore，两副共用）；84 份 SO 的 lore 键
  **84 个互不相同、无一为空**。
- 练习 78 份的 `tutorialIndex` 一律 **-1**；教程 6 份是 **0..5**：

| 教程 SO | tutorialIndex | tutorialIndexToBeUnlocked | classic deckId |
|---|---|---|---|
| Demo DeckInfo UM Tutorial | 0 | -1 | `UMTutorial` |
| Demo DeckInfo Orks Tutorial | 1 | 0 | `OrksTutorial` |
| Demo DeckInfo Sautekh Tutorial | 2 | 1 | `NecronsTutorial` |
| Demo DeckInfo BlackLegion Tutorial | 3 | 1 | `BlackLegionDeckTutorial` |
| Demo DeckInfo Aeldari Tutorial | 4 | 1 | `AeldariDeckTutorial` |
| Demo DeckInfo Leviathan Tutorial | 5 | 1 | `LeviathanDeckTutorial` |

- `armyIcon` 13 张（每阵营一张，`40k_DeckSelection_icon_*`）；`armyColor` **是任意浮点**（不是 8bit/255，
  例如 `35.44/115.47`）⇒ 产物里同时给 `armyColor`（`#RRGGBB`，由浮点四舍五入得来）与
  `armyColorRaw`（原样 4 个 float）。**要精确复刻就用 raw**。

---

## 三、落点对账：78 / 156

- **只按 `deckId` 精确命中**：78 条。命中后逐条核过 **`gameMode` 与所在一侧一致（0/13）、
  `deckName` 与池里那副完全一致** —— 78/78 全对。
- **按名字兜底一律不成立**（实测，不是判断）：
  那 78 条落不进的里，有 30 条能在我们池里找到**同名**候选，**逐条比牌表 30/30 内容不同** ——
  撞的名字都是「同一副牌的另一模式版本」（例：skirmish `UM_SK_3_2` 12 张 ↔ 池里 classic
  `UMDeck5` 30 张）。⇒ 产物里留 `nameCandidates` 作证据，`contentEqual` **全 false**。
- **不同名但牌表一模一样**：只有 **1 条** —— `SautekhDeck3`「Implacable Conqueror」↔ 池里
  `SautekhDeck8`「Lord of the Storm」（同 30 张、同模式）。产物里记在 `contentTwins`，
  **不自动判等**（是不是原版自己的重复，要单独裁）。
- **交叉验证（与上一轮独立成果一致）**：`工具/gen_prebuilt_decks.py` 的 `DEMO_LORE_KEY`
  （78 条）正好等于本产物里**落进池的那 78 条**，`loreKey` **78/78 逐条相同**（教程 6 条不在那张表里，属预期）。
- **我们池里另外 25 副没有被任何 Demo 对引用**（Demo 之后新增的）：
  `AM_SK_4 · ASH_SK_4 · ASH_SK_5 · AeldariDeck6 · BL_SK_4 · BlackLegionDeck6 · DA_SK_4 ·
  DarkAngelsDeck7 · Deck_UM_Titus · GSC_SK_4 · LEV_Vardenghast · ORK_SK_4 · ORK_SK_5 · ORK_SK_6 ·
  OrksDeck7 · SAU_SK_4 · SOR_SK_4 · SautekhDeck7 · SautekhDeck8 · TAU_SK_4 · TL_SK_4 ·
  TauEmpireDeck7 · UMDeck6 · UM_SK_4 · UM_SK_5`。
- **236 个 PrebuiltDeck 里有 5 个重复 deckId** —— 全部是教程那 12 个对象：
  `UMTutorial` ×4、`NecronsTutorial` ×3 ⇒ 231 个不同 deckId。**练习那 156 个没有任何重复。**

---

## 四、`unresolved` 全表（78 条，全部 `isPracticeDeck = 0`、`inDecklists = true`）

> 结构：`{so, pathID, kind, side, deckId, deckName, gameMode, isPractice, cardCount,
> reason:"not_in_prebuilt_decks", inDecklists, nameCandidates[], contentTwins[]}`。
> 全表也在产物 `demo_deck_pairs.json` 的 `unresolved` 里（机器可读）。

| SO | 侧 | deckId | deckName | 张数 |
|---|---|---|---|---|
| Demo DeckInfo Aeldari 3 | classic | `AeldariDeck3` | The Cosmic Serpent | 30 |
| Demo DeckInfo Aeldari 3 | skirmish | `ASH_SK_1_2` | The Cosmic Serpent | 12 |
| Demo DeckInfo Aeldari 5 | classic | `AeldariDeck5` | Air Superiority | 30 |
| Demo DeckInfo Aeldari 5 | skirmish | `ASH_SK_3_2` | Air Superiority | 12 |
| Demo DeckInfo Aeldari 8 | classic | `AeldariDeck8` | Path of the Seer | 30 |
| Demo DeckInfo Aeldari 8 | skirmish | `ASH_SK_2_2` | Path of the Seer | 12 |
| Demo DeckInfo Astra Militarum 2 | classic | `AstraMilitarumDeck2` | Armoured Regiment | 30 |
| Demo DeckInfo Astra Militarum 2 | skirmish | `AM_SK_1_2` | Armoured Regiment | 12 |
| Demo DeckInfo Astra Militarum 4 | classic | `AstraMilitarumDeck4` | Wall of Muscle | 30 |
| Demo DeckInfo Astra Militarum 4 | skirmish | `AM_SK_2_2` | Wall of Muscle | 12 |
| Demo DeckInfo Astra Militarum 5 | skirmish | `AM_SK_3_2` | Combined Arms | 12 |
| Demo DeckInfo Astra Militarum 6 | classic | `AstraMilitarumDeck6` | Emperor's Hammer | 30 |
| Demo DeckInfo BlackLegion 4 | classic | `BlackLegionDeck4` | Bringers of Decay | 30 |
| Demo DeckInfo BlackLegion 4 | skirmish | `BL_SK_1_2` | Bringers of Decay | 12 |
| Demo DeckInfo BlackLegion 5 | skirmish | `BL_SK_3_2` | Chaos Absolute | 12 |
| Demo DeckInfo BlackLegion 8 | classic | `BlackLegionDeck8` | Sin and Ruin | 30 |
| Demo DeckInfo BlackLegion 8 | skirmish | `BL_SK_2_2` | Sin and Ruin | 12 |
| Demo DeckInfo Dark Angels 1 | skirmish | `DA_SK_1_2` | None Must Know | 12 |
| Demo DeckInfo Dark Angels 2 | classic | `DarkAngelsDeck2` | The Hunt | 30 |
| Demo DeckInfo Dark Angels 2 | skirmish | `DA_SK_3_2` | The Hunt | 12 |
| Demo DeckInfo Dark Angels 4 | classic | `DarkAngelsDeck4` | The Inner Circle | 30 |
| Demo DeckInfo Dark Angels 6 | classic | `DarkAngelsDeck6` | Instruments of Vengeance | 30 |
| Demo DeckInfo Dark Angels 6 | skirmish | `DA_SK_2_2` | Instruments of Vengeance | 12 |
| Demo DeckInfo Emperors Children 1 | skirmish | `EC_SK_3_2` | Draught of Despair | 12 |
| Demo DeckInfo Emperors Children 2 | classic | `EmperorsChildrenDeck2` | Symphony of Pain | 30 |
| Demo DeckInfo Emperors Children 2 | skirmish | `EC_SK_1_2` | Symphony of Pain | 12 |
| Demo DeckInfo Emperors Children 4 | classic | `EmperorsChildrenDeck4` | Dark Patronage | 30 |
| Demo DeckInfo Emperors Children 6 | classic | `EmperorsChildrenDeck6` | Feeding the Addiction | 30 |
| Demo DeckInfo Emperors Children 6 | skirmish | `EC_SK_2_2` | Feeding the Addiction | 12 |
| Demo DeckInfo GenestealerCults 1 | classic | `GenestealersDeck1` | Faithful Throng | 30 |
| Demo DeckInfo GenestealerCults 2 | skirmish | `GSC_SK_1_2` | Claws Unsheathed | 12 |
| Demo DeckInfo GenestealerCults 3 | classic | `GenestealersDeck3` | Boundless Horizons | 30 |
| Demo DeckInfo GenestealerCults 4 | skirmish | `GSC_SK_2_2` | Clandestine Operation | 12 |
| Demo DeckInfo GenestealerCults 5 | skirmish | `GSC_SK_3_2` | Unseen Peril | 12 |
| Demo DeckInfo GenestealerCults 6 | classic | `GenestealersDeck6` | The Hidden Threat | 30 |
| Demo DeckInfo Leviathan 2 | classic | `LeviathanDeck2` | Hyper-adaptation | 30 |
| Demo DeckInfo Leviathan 4 | skirmish | `TL_SK_2_2` | Synaptic Control | 12 |
| Demo DeckInfo Leviathan 5 | classic | `LeviathanDeck5` | Predator Instincts | 30 |
| Demo DeckInfo Leviathan 5 | skirmish | `TL_SK_3_2` | Predator Instincts | 12 |
| Demo DeckInfo Leviathan 8 | classic | `LeviathanDeck8` | Spores of Devastation | 30 |
| Demo DeckInfo Leviathan 8 | skirmish | `TL_SK_1_2` | Spores of Devastation | 12 |
| Demo DeckInfo Orks 1 | classic | `OrksDeck1` | Green Tide | 30 |
| Demo DeckInfo Orks 3 | classic | `OrksDeck3` | Goff is Da Best | 30 |
| Demo DeckInfo Orks 3 | skirmish | `ORK_SK_3_2` | Goff is Da Best | 12 |
| Demo DeckInfo Orks 4 | skirmish | `ORK_SK_1_2` | Mekaniak | 12 |
| Demo DeckInfo Orks 8 | classic | `OrksDeck8` | Tempered Engines | 30 |
| Demo DeckInfo Orks 8 | skirmish | `ORK_SK_2_2` | Tempered Engines | 12 |
| Demo DeckInfo Sautekh 3 | classic | `SautekhDeck3` | Implacable Conqueror | 30 |
| Demo DeckInfo Sautekh 3 | skirmish | `SAU_SK_1_2` | Implacable Conqueror | 12 |
| Demo DeckInfo Sautekh 4 | classic | `SautekhDeck4` | Destroyer Cult | 30 |
| Demo DeckInfo Sautekh 5 | classic | `SautekhDeck5` | Scarab Swarm | 30 |
| Demo DeckInfo Sautekh 5 | skirmish | `SAU_SK_3_2` | Scarab Swarm | 12 |
| Demo DeckInfo Sautekh 6 | classic | `SautekhDeck6` | Master of Astromancy | 30 |
| Demo DeckInfo Sautekh 6 | skirmish | `SAU_SK_2_2` | Master of Astromancy | 12 |
| Demo DeckInfo Sororitas 2 | classic | `SororitasDeck2` | Purity of Faith | 30 |
| Demo DeckInfo Sororitas 3 | classic | `SororitasDeck3` | Fiery Oratory | 30 |
| Demo DeckInfo Sororitas 3 | skirmish | `SOR_SK_3_2` | Fiery Oratory | 12 |
| Demo DeckInfo Sororitas 4 | skirmish | `SOR_SK_1_2` | Redemption | 12 |
| Demo DeckInfo Sororitas 5 | skirmish | `SOR_SK_2_2` | Cleansed by Fire | 12 |
| Demo DeckInfo Sororitas 6 | classic | `SororitasDeck6` | Faith and Fury | 30 |
| Demo DeckInfo Space Wolves 2 | classic | `SpaceWolvesDeck2` | Pack Midrange | 30 |
| Demo DeckInfo Space Wolves 2 | skirmish | `SW_SK_1_2` | Terrifying Howls | 12 |
| Demo DeckInfo Space Wolves 4 | classic | `SpaceWolvesDeck4` | Vehicles | 30 |
| Demo DeckInfo Space Wolves 4 | skirmish | `SW_SK_2_2` | Arcane Engines | 12 |
| Demo DeckInfo Space Wolves 6 | classic | `SpaceWolvesDeck6` | Hunt Marks Control | 30 |
| Demo DeckInfo Space Wolves 6 | skirmish | `SW_SK_3_2` | Prowling Predators | 12 |
| Demo DeckInfo TauEmpire 1 | classic | `TauEmpireDeck1` | Target Locked | 30 |
| Demo DeckInfo TauEmpire 1 | skirmish | `TAU_SK_1_2` | Target Locked | 12 |
| Demo DeckInfo TauEmpire 3 | skirmish | `TAU_SK_2_2` | Hero's Mantle | 12 |
| Demo DeckInfo TauEmpire 4 | classic | `TauEmpireDeck4` | Storm of Fire | 30 |
| Demo DeckInfo TauEmpire 5 | classic | `TauEmpireDeck5` | The Huntress | 30 |
| Demo DeckInfo TauEmpire 6 | skirmish | `TAU_SK_3_2` | Patient Hunter | 12 |
| Demo DeckInfo UM 1 | skirmish | `UM_SK_1_2` | 4th Company | 12 |
| Demo DeckInfo UM 2 | classic | `UMDeck2` | Adept of the Codex | 30 |
| Demo DeckInfo UM 3 | classic | `UMDeck3` | Spear of Macragge | 30 |
| Demo DeckInfo UM 4 | skirmish | `UM_SK_2_2` | Champion of the Codex | 12 |
| Demo DeckInfo UM 5 | skirmish | `UM_SK_3_2` | Chapter Command | 12 |
| Demo DeckInfo UM 8 | classic | `UMDeck8` | Master Tactician | 30 |

⚠️ 注意最后几条：`UM_SK_1_2` 与池里的 `UM_SK_1` 是**不同 deckId、不同牌表**（这里只是名字像）。
**别拿 `_2` 当作「同副」的缩写** —— 名字那 30 条已逐条比过，内容全不同。

---

## 五、下一步接运行时该怎么改（**只写建议，本轮不改代码**）

1. **练习窗左列换数据源**（`Shell/PracticeModePopup.cs`）：读 `demo_deck_pairs.json` 的
   `pairs` 里 `kind == "practice"`，按 `uiOrder`（0..77）排；`Game mode` 取 `Classic(0)` 时用 `classic`、
   否则用 `skirmish`（= 原版 `DemoDeckInfoSO.GetDeck`）；默认 `Game mode` 照原版 ctor = **Skirmish(13)**。
2. **补数据缺口**：这 78 对里有 **78 个 deck 不在 `prebuilt_decks.json`**。建议**新增一份**产物
   （如 `prebuilt_decks_demo.json`，源 = `decklists.json` 里这 78 个 deckId，`isPractice == 0`），
   **不要动现有 103 副那份**（`gen_prebuilt_decks.py` 头注释已声明它必须逐字节不变）。
   或者在 `PrebuiltDecks.Deck` 之外新增一个「按 deckId 查」的补充表，两者合并成「demo 池」。
3. **这 78 副的元数据照 `gen_prebuilt_decks.py` 同一套解析**（hero 判定 / 卡背 / 防御卡补齐口径），
   别写第二份规则（「两处写同一条规则 = 迟早不一致」）。
4. **`loreKey` 直接取本产物**：现有 `prebuilt_decks.json` 只覆盖落池那 78 副；
   练习窗改成 Demo 对之后，**每对统一用 `pairs[i].loreKey`**（两副共用一条，原版语义）。
5. **左列的两颗视觉件**（`armyIcon` / `armyColor`）本产物已给全（84/84 有图标）；
   ⚠️ `armyColor` 是任意浮点，**要精确复刻就用 `armyColorRaw`**，`#RRGGBB` 只是便利用值。

---

## 六、边界 / 没查清（⛔ 不猜）

1. **`armyColor` 的语义**：原版存的是任意浮点（不是 8bit/255）。产物给 raw + 派生 hex；
   运行时到底该按 sRGB 直接用还是要走一次色彩空间转换，**没查**（没有实拍对照，不猜）。
2. **`SautekhDeck3` ↔ `SautekhDeck8` 同牌表不同名**：只标 `contentTwins`，**未裁定**是否同一副。
3. **`PrebuiltSortedDeck` 与 `PrebuiltDeck` 的差异**（教程那 12 个是前者）：类签名有，但本轮只用了公共字段
   （`deckId/deckName/gameMode/isPracticeDeck/cardLibraryIds`）；它是否另有排序字段**没查**。
4. **`PracticeModePopup` 之外的入口**：本轮只证了练习窗那个 78 项数组；
   教程窗（`TutorialModePopup`）是否另有一份 SO 数组、是否就是这 6 份，**没查**。
5. `buttonImageReference`（`AssetReferenceTyped<Sprite>` 的 GUID）**没解**：schema 没要求，
   而且 `guid_map.tsv` 只收卡牌资产；要用得另外按 GUID 反查包。

---

## 附：产物形状（`demo_deck_pairs.json`）

```
{ "format": 1, "generated": "2026-10-22",
  "source": { bundleDir, bundles{demo[],decks,icons,monoscripts}, cabs{...},
              demoScript{className,monoScriptPathID}, practicePopup{bundle,className,pathID,deckCount},
              poolFile, poolDeckCount, assetsFullCrossCheck{soNames,sameNameSet} },
  "summary": { soTotal:84, practicePairs:78, tutorialSo:6, deckRefs:162,
               practiceDeckRefs:156, tutorialDeckRefs:6,
               refsInPool:78, refsInTutorial:6, refsUnresolved:78,
               pairsFullyInPool:23, pairsPartiallyInPool:32, pairsNoneInPool:23 },
  "pairs": [ { so, pathID, bundle, kind, uiOrder, loreKey, armyColor, armyColorRaw,
               armyIcon, armyIconPathID, tutorialIndex, tutorialIndexToBeUnlocked,
               classic|skirmish: { deckId, deckName, gameMode, isPractice, cardCount,
                                   difficulty, heroId, ourDeckId, ourSource,
                                   ourNameZh, tutorialSlots } } ],
  "unresolved": [ { so, pathID, kind, side, deckId, deckName, gameMode, isPractice,
                    cardCount, reason, inDecklists, nameCandidates[], contentTwins[] } ] }
```

工具：`D:/2/Warpforge_tools/py312/python.exe -I -X utf8 d:/4/Unity/工具/gen_demo_deck_pairs.py [--check]`
—— 连续两次跑 md5 相同；`--check` 只重算不写盘。
