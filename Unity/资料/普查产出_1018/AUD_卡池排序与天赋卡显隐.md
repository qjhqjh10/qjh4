# AUD · 卡组编辑卡池：排序规则 / 天赋卡显隐 / 全量显隐分类

> 只读查证（2026-10-18）。**没有改任何 `.cs` / 数据 / 正本**，没跑 Unity、没碰 git。
> 原版判据 = 全量反编译 `d:/2/tools/decomp_full/` + IL2CPP 元数据 `d:/2/tools/il2cpp_out/{dump.cs,script.json}`；
> 我们对侧 = **现读**源码（`DeckRuntime.cs` 正被另一路写手改，引用均注「现读」）。

---

## §一 三问结论（各 3–8 行）

### 1. 卡池排序：**不是按卡牌 ID**；是「阵营 → 战将优先 → 费用 → 卡名」四层稳定排序

- 原版卡组编辑窗的卡池来源 = `DeckEditingWindow.GetCardCollection()`
  （`decomp_full/DeckEditingWindow__GetCardCollection.c:36-152`）：取
  `CollectionManager.Instance.AllCardCollection`（`+0x30`，**未开 Owned 时**）或
  `InventoryManager.GetInventory<RawCardScript>()`（**开 Owned 时**；原版 `Owned` **出厂为开**，
  `DeckEditingWindow__.ctor.c:12` 写 `+0x120 = 1`），再串 **5 个 `Where` + 4 个 `OrderBy`**，最后 `ToList()`。
- 那 4 个 `OrderBy` 的 MethodInfo 用 `script.json` 的 `ScriptMetadataMethod` 逐个落名（地址 + `0x180000000`）：
  `OrderBy<RawCardScript,string>`（`:106`）· `OrderBy<RawCardScript,int>`（`:121`）·
  `OrderBy<RawCardScript,bool>`（`:136`）· `OrderBy<RawCardScript,CardArmy>`（`:151`）。
  四个**都是 `OrderBy`**（不是 `ThenBy`；`ThenBy<RawCardScript,string>` 的 token 在 `0x18427e720`，本处没用）。
- `Enumerable.OrderBy` 是**稳定排序** ⇒ 连串 4 次的**实际主序 = 最后一个键**：
  **`cardArmy`（阵营，枚举值 10→130）→ `cardType != Hero`（战将排前）→ `manaCost` → 本地化卡名**。
  排序键的取值来自 `dump.cs` 的字段偏移：`manaCost` 0x78 · `cardType` 0x94 · `cardArmy`（`ObtainableItem`）· 名 = `GetLocalizedCardName()`。
- **战将不是「单独插一张」**，是 `cardType != Hero` 这个**排序键**（bool 升序 ⇒ `false` 在前）——
  所以是「**同一阵营里所有战将连成一段排最前**」，再按费用/名字。
- 同一条链里**没有** `uniqueId` / `cardShortId` / 稀有度 / 数据索引参与排序。
- 我们对侧：`Deck/DeckEditorState.cs:366 VisibleCards()` **完全不排序**（`:364-365` 自己写着「不排序，原版有没有排序规则没查到」），
  出来的顺序 = `cards_engine.json` 的文件序（**按阵营粗分、但阵营次序是 SaimHann 打头、组内战将在最末、末尾还有 17 张掉队卡**，见 §二）。
- 左侧卡组列表（`DeckListDrawer.Reorder`）：`OrderBy(manaCost)` + `ThenBy(本地化名)`，再把战将塞 sibling 0、防御卡塞 1（`DeckListDrawer__Reorder.c:66,83,141,152,170`）——**我们没有偏离**（`DeckRuntime.cs:4047-4088` 逐条对上）。
- 收藏窗卡牌页（`CardCollectionTab.GetCollection`）多一层：`OrderBy<CardInventoryOptions>` 夹在费用与「战将优先」之间
  ⇒ 实际 **阵营 → 战将 → `inventoryOptions` → 费用 → 名字**（`CardCollectionTab__GetCollection.c:69,84,99,114,129`）。我们同样**没排序**、且用的是同一份 `VisibleCards()`。

### 2. 天赋卡：**原版卡池/收藏都不显示，只在「宿主卡的大图详情」里以 relatedCard 出现**

- 原版「天赋」不是卡类型，是**宿主卡上的一条 ability**：`trigger == 46 /*AbilityTrigger.Talent*/`
  （`RawCardScript.CardTraits` 里另挂 `_trait = 845 /*DefinedTrait.talent*/`）；
  `CardScript__GetTalentCard.c` 取该 ability 的 `unitsToSummon[0]` —— **一条对象引用**。
- **天赋卡本体是普通 `RawCardScript`**（当前 build 92 张，全是 `cardType=20 / spellType=0（或 90）` 的 tactic），
  它们的 `showInCollectionOptions = 0 (Hide)` ⇒ 被卡池那条 `Where(showInCollectionOptions != 0)` 剔掉
  （`DeckEditingWindow.__c___GetCardCollection_b__32_2.c:8`），收藏窗同样剔（`CardCollectionTab.__c___GetCollection_b__8_0.c:19-30`）。
- 原版「大图详情」链：`DeckEditingWindow.OpenCardInformation`（左键点卡池任意一张）
  → `CardDisplayWindow.ShowCard` → `InitializeRelatedCards(card)`（`CardDisplayWindow__ShowCard.c` 中段）
  → `card.GetRelatedCards()`（`RawCardScript__GetRelatedCards.c` 逐字读 `+0xE8/+0xF0/+0xF8/+0x100` = `relatedCard1..4`，
  无筛选无匹配）→ 非空的一张一张摆进第 1..4 格。
- 🔴 **实测 96/96：`talent_cards.json` 里每一张「天赋卡」正好等于宿主卡的 `relatedCard1..4` 之一**
  （用 `cards_raw.json` 的 `path_id` 交集验的）⇒ **「点开宿主的大图详情就能看到它的天赋卡」这条假设是坐实的，不是猜的**。
- 我们现状：
  · **卡池**：天赋卡 92/92 都在 `cards_engine.json` 里（引擎生成天赋要用），但 `DeckEditorState.VisibleCards()` 只在
    `WarlordGatedPool == true`（= 卡组编辑）时套 `DeckRules.CannotAddToDeck`，而它要求 `show==5`
    ⇒ **卡组编辑卡池里看不到天赋卡**（与原版一致，**但是靠「卡组准入」判据顺带挡的，不是靠显示判据**）。
  · **收藏窗**：`CollectionWindow.CardsState`（`CollectionWindow.cs:812-817`）的 `WarlordGatedPool` 默认 `false`
    ⇒ **92 张天赋卡 + 30 张 `Z-NC` 效果卡（共 122 张 `show=0`）全都会显示** —— 与原版不符。
  · **大图详情**：`CardDetailPopup.BuildCardStack`（`CardDetailPopup.cs:836-842`）→ `RelatedCards.Find`
    （`Core/RelatedCards.cs:51`；`:67` 起**真值优先**读 `related_cards.json`）——`related_cards.json` **191 条边、96 个天赋宿主全覆盖**，
    且每条都含该宿主的天赋卡 ⇒ **我们的详情窗已经显示天赋卡**（这一半无偏离）。

### 3. 除天赋卡外：谁显示谁不显示 —— 一句话

原版**只有 `showInCollectionOptions != Hide(0)` 的卡**进卡池/收藏（当前 build = **1005 张**），
`Hide(0)` 的 **346 张**（变体/教程/战役 oath/EASY/startinghand/withVanguard/REMOVED/进攻卡/天赋/效果卡）一概不进；
卡组编辑卡池再额外剔掉 `inventoryOptions == CantAddToDeck(5)` 的（= 防御卡，除非该模式 `assignDefensiveCardsInEditMode`，
Classic = false ⇒ **经典模式编辑卡池里没有防御卡**）。我们卡池 = **1127**（比原版多 122 张 `show=0`），
按「全池可显示」口径算：我们 **1000** vs 原版 Classic **966**（差 **+34** = +39 防御卡 −5 张 `show=5/inv=10` 的暗黑天使秘仪）；
⚠️ 这是**跨阵营的总数**，实际一次显示的是「没督军 ⇒ 只列 56 位战将 / 定了督军 ⇒ 该阵营那一段」。详见 §四。

---

## §二 排序规则表

| 列表 | 原版排序键 + 出处 | 我们现状（文件:行） | 差异 | 建议 |
|---|---|---|---|---|
| **卡组编辑 · 卡池网格** | 4 次稳定 `OrderBy` ⇒ **`cardArmy` → `cardType != Hero`（战将先）→ `manaCost` → 本地化名**。出处：`DeckEditingWindow__GetCardCollection.c:106,121,136,151`；键选择器 = `…b__32_5.c`（名）· `…b__32_6.c`（`+0x78` manaCost）· `…b__32_7.c`（`+0x94 != 10`）· `…b__32_8.c`（`+0x2c` cardArmy）；方法名用 `script.json` 的 `ScriptMetadataMethod` 落定 | `DeckEditorState.cs:366-446` `VisibleCards()`：**不排序**（`:364-365` 自述），顺序 = `cards_engine.json` 文件序 | **全缺排序**。现池文件序 = 24 个阵营段（SaimHann 先、13 段之后还有 11 段零散尾包）；**组内战将在最末**（例：SaimHann 段第 87–91 位才是 5 位战将）；56 位战将里 `EmperorsChildren/Lord Kaphrael` 落在第 1114 位 | 在 `VisibleCards()` 末尾（`StableGroupByFaction` 那一步之前/替代它）加四层排序：`CardArmyOf(c.Faction)` → `c.Type=="hero"`（false 先）→ `c.Cost` → `CompareDisplayName`（**文化相关**，与 `DeckEntries` 同一把尺 `DeckRuntime.cs:4076-4083`）。只有「没督军」那种跨阵营状态才看得出前三层 |
| **卡组编辑 · 左侧卡组列表** | `OrderBy(manaCost)` + `ThenBy(GetLocalizedCardName)`，随后战将→sibling 0、防御卡→1（有战将时）/0。出处：`DeckListDrawer__Reorder.c:66,83`（`OrderBy<UICardInfoItem,int>` / `ThenBy<UICardInfoItem,string>`，token `0x1842703b8` / `0x18427e818`）+ `:141,152,170`；键选择器 `DeckListDrawer.__c___Reorder_b__22_0.c`（`card+0x78` = manaCost）/ `_22_1.c`（本地化名） | `DeckRuntime.cs:4047-4088` `DeckEntries()`：战将、防御卡先入表（`:4050-4051`），其余 `Cost → CompareDisplayName → Id`（`:4076-4083`） | **无差异**（`Id` 那一档是我们补的全序，因为 `List.Sort` 不稳定；原版 `OrderBy` 稳定不需求） | 不动 |
| **收藏窗 · 卡牌页** | `OrderBy(名)` → `OrderBy(manaCost)` → `OrderBy(inventoryOptions)` → `OrderBy(cardType != Hero)` → `OrderBy(cardArmy)` ⇒ **阵营 → 战将 → `inventoryOptions` → 费用 → 名字**。出处：`CardCollectionTab__GetCollection.c:69,84,99,114,129`（token `0x18426fbd8` / `0x18426fae0` / `0x18426f9e0` / `0x18426f7e8` / `0x18426f8e8`） | `CollectionWindow.cs:812-817` 用同一份 `DeckEditorState`，`:1087` 取 `CardsState.VisibleCards()` —— **同样不排序** | 同「卡池」的排序缺失；**另差一层 `inventoryOptions` 键** | 同一条排序函数复用（别写两份）；`inventoryOptions` 那一层加在费用之前 |
| （参考）收藏窗 · 卡组页 | `CollectionDeck.SetSortMode(bool)` 只切拖拽/缩放（`CollectionDeck__SetSortMode.c`），排序用的是 `DeckCollectionDisplay.SaveDeckOrder`（拖拽序） | 我们收藏线用 `DeckLibrary` 序 | 本轮不展开 | —— |

**为什么最后一个 `OrderBy` 是主序**：`Enumerable.OrderBy` 是稳定排序，链式调用等价于「按最后一个键排，前面的键当 tie-breaker」。
⚠️ **不能改写成 `ThenBy`**（语义会反过来：变成「按名字排，阵营只当 tie-breaker」）。

---

## §三 天赋卡

| 项 | 原版判据 + 出处 | 我们现状（文件:行） | 差异 | 建议 |
|---|---|---|---|---|
| **判定字段** | ①宿主：`CardTraits` 里 `_trait == 845 (DefinedTrait.talent)`（`dump.cs:45804`）+ `cardAbilities[]` 里 `trigger == 46 (AbilityTrigger.Talent)`（`dump.cs:20249`；`CardScript__GetTalentCard.c` 检 `0x2e`）；②天赋卡 = 该 ability 的 `abilityLogic[0].summonCriteria.unitsToSummon[*]` 指向的卡；③同一条引用**一定也在** `relatedCard1..4` 里 | `Assets/RuleEngine/Resources/talent_cards.json`：96 宿主 / 100 条引用 / **92 张去重天赋卡**（生成器 `工具/gen_talent_cards.py`，读 `cards_raw.json` 的 `trigger==46`，源注释见该文件头）；`Core/TalentCardData.cs` 消费 | **一致**（我们另存了一份「宿主→候选」表，因为原版是对象引用、我们是 id） | 不动 |
| **原版卡池怎么排除** | `Where(showInCollectionOptions != 0 /*Hide*/)` —— `DeckEditingWindow.__c___GetCardCollection_b__32_2.c:8`；收藏窗同一条 `CardCollectionTab.__c___GetCollection_b__8_0.c:19-30`（另有 `IsHidden()` 与「非异画」两道） | 卡组编辑：`DeckEditorState.cs:382-390` 的 `DeckRules.CannotAddToDeck(c)`（要求 `show==5`）**顺带**滤掉；收藏窗：**没有任何 `showInCollectionOptions` 判据** ⇒ 全显示 | 卡组编辑：**结果对、判据不对**（显示闸门与「能不能进卡组」混成一条，见 §五）；收藏窗：**122 张 `show=0` 漏进**（92 天赋 + 30 `Z-NC` 效果卡） | 把「显示判据」与「卡组准入判据」拆成两条；收藏/卡池共用同一条显示判据 |
| **原版大图详情链** | 左键点卡 → `DeckEditingWindow.OpenCardInformation`（`…OpenCardInformation.c:44-60`）→ `CardDisplayWindow.ShowCard`（`CardDisplayWindow__ShowCard.c`）→ `InitializeRelatedCards`（`CardDisplayWindow__InitializeRelatedCards.c:26`）→ `GetRelatedCards()` 读 `relatedCard1..4`（`RawCardScript__GetRelatedCards.c:23-96`，`+0xE8/0xF0/0xF8/0x100`） | `DeckRuntime.cs:8030-8040` 开的是菜单版 `CardDetailPopup`；`CardDetailPopup.cs:840-841` `cards.AddRange(RelatedCards.Find(Card, Slots-1))`；`Core/RelatedCards.cs:51,67` **真值优先**读 `CardPresentation/Resources/related_cards.json`（191 条边） | **无差异**：`related_cards.json` 里 96 个天赋宿主全在，且每条都含自己的天赋卡；`talent_cards.json` 与 `related_cards.json` 的 PathID 交集 **96/96 命中** | 不动（`CardFan.Slots = 9` 比原版宽，但真值每卡 ≤4 张，看不出来） |
| **其他入口** | `CardScript.GetTalentCard` 全库只有 `FilterMethods__CheckIfMeetsCriteria.c:133` 一处调用（criteria `0xEB = warlordsTalent`，**战斗内**判定），**没有任何 UI 显示链** | —— | 一致 | 不动 |

**结论**：天赋卡「不在卡池/收藏显示、只在宿主大图详情里看得到」这条原版口径**成立**；
我们**卡组编辑这一半已经对（但判据错位）**，**收藏窗那一半没做**，**大图详情那一半已经对**。

---

## §四 全量显隐分类表（数量 = 2026-10-18 现读）

数据源：`Unity/资料/普查产出_1018/allcards_1350/cards_raw.json`（SO 全量 **1351** 张，字段 `cardType/spellType/showInCollectionOptions/inventoryOptions/m_Name/relatedCard*/alternateArts`）
＋ `MyGame/Assets/RuleEngine/Resources/cards_engine.json`（我们池 **1127** 张）。两集合差 = **224 张**（全部 `show=0`）。

### 4·1 按 `cardType + spellType` 的完美分类

| 类别（cardType/spellType） | SO 张数 | 我们池内 | show=5 / show=0 | inv=0 / inv=5 / inv=10 | 原版·卡组编辑卡池 | 我们·卡组编辑卡池 | 原版·收藏窗 | 我们·收藏窗 | 大图详情 | 样例 id |
|---|---|---|---|---|---|---|---|---|---|---|
| **unit**（0/0） | 605 | 586 | 585 / 20 | 585 / 0 / 20 | 显示 | 显示（同阵营） | 显示 | 显示 | 可开 | `ASH83` Autarch · `33 EmperorsChildren-Heldrake`（GUID）· `6e4f9040…` Tyrnak and Fenrir（`Z-NC`，不显示） |
| **hero/战将**（10/0） | 66 | 56 | 56 / 10 | 56 / 0 / 10 | 显示（`cardType!=Hero` 排序键 ⇒ **同阵营最前**） | 显示（且**没督军时只列战将** ＝ 用户 2026-09-27 规格） | 显示 | 显示 | 可开 | `AM1` Lord Solar Leontus · `SAU37` · `GOF28` |
| **tactic**（20/0） | 508 | 406 | 311 / 197 | 311 / 0 / 197 | 显示（`show=5`） | 显示；**197 张 `show=0` 里 122 张靠「卡组准入」顺带滤掉**；另 5 张 `show=5&inv=10` 被误滤 | 显示（197 张 `show=0` 全被 Hide 判据剔掉） | 显示（多 122 张：92 天赋 + 30 `Z-NC`） | 可开 | `ASH46` Aerial Dominion · `ASH84` Ahnakh-Yth Shrine · `ASH45` Banshee Mask |
| **defence**（20/210） | 39 | 39 | 39 / 0 | 0 / **39** / 0 | **仅当 `assignDefensiveCardsInEditMode == true`**（`DeckEditingWindow___GetCardCollection_b__32_1.c:35-47` → `GameplayVariablesData+0x4A`；`dump.cs`：`assignDefensiveCardsInEditMode`，**Classic=false**，`GameplayVariables.cs:261` 记原版 Classic=false / Skirmish=true） | **一直显示**（`DeckRules.CannotAddToDeck` 对 defence 直接放行） | 显示（收藏不看 `inventoryOptions`） | 显示 | 可开 | `ASH55` Aspect Shrine · `AM60` Timely Reinforcements · `BL56` Throne of the Heretic |
| **effect-only tactic**（20/90,150,180,190,220,230,240,260,265,270） | 42 | 40 | 14 / 28 | 14 / 0 / 28 | `show=5` 的 14 张显示；`show=0` 的 28 张不显示 | 14 张里 5 张（DA 秘仪 `show=5&inv=10`）被误滤；`show=0` 的 26 张被滤 ✓ | 14 张显示 | **40 张全显示**（多 26） | 可开 | `3e99603a…` Convoke the Circle（show5/inv10）· `4a48457c…` Mark of Tzeentch (enchantment)（show0） |
| **offensive / 进攻卡**（20/200） | 91 | **0** | 0 / 91 | 0 / 39 / 52 | 不显示 | 不显示（不在池） | 不显示 | 不显示 | 只能当战斗内效果 | `ASH59` Infinity Circuit Overload · `AM63` Dawn Attack · `8d1fbc34…` Normal Conditions（空进攻卡） |

### 4·2 按「变体后缀 / 特殊标记」

| 类别 | SO 张数 | 我们池内 | show / inv | 原版卡池+收藏 | 我们卡组编辑 | 我们收藏窗 | 样例 id |
|---|---|---|---|---|---|---|---|
| **附魔 `_enchantment*`** | 115 | **4**（`Mark of Khorne/Slaanesh/Tzeentch/Nurgle (enchantment)`） | 全 `show=0 / inv=10` | 不显示 | 滤掉 ✓ | **4 张显示** ✗ | `4a48457c…` · `b8e81b2f…` · `9a2cadc6…` · `1dfc9bc7…` |
| **教程 `_tutorial/_Tutorial/(tutorial`** | 22 | 0 | 全 `show=0 / inv=10` | 不显示 | 不在池 ✓ | 不在池 ✓ | `0fca8843…` Firstborn (tutorial) · `7a023597…` Witchblade Warlock (tutorial) · `eb815e1e…` Primaris Intercessor |
| **战役 `_oath`** | 10 | 0 | 全 `show=0 / inv=10` | 不显示 | ✓ | ✓ | `57bfef85…` Stormhawk Interception_oath · `6ea114bc…` No Mercy_oath · `fe6fde3d…` Fall Back_oath |
| **EASY** | 6 | 0 | 全 `show=0 / inv=10`（全 heap hero） | 不显示 | ✓ | ✓ | `edb0fd16…` Medreyal Ghaelyn_EASY · `b3a0be3e…` Haarken Worldclaimer_EASY · `bd6baa4e…` Tervigon_EASY |
| **startinghand** | 2 | 0 | `show=0 / inv=10` | 不显示 | ✓ | ✓ | `8f0302f1…` Tyrnak and Fenrir_startinghand · `f3df0cfc…` Abaddon's Chosen_startinghand |
| **withVanguard** | 1 | 0 | `show=0 / inv=10` | 不显示 | ✓ | ✓ | `66486b3f…` Tempestus Scion_withVanguard |
| **REMOVED** | 1 | 0 | `show=0 / inv=10` | 不显示 | ✓ | ✓ | `SAU48` Disintegration Capacitors |
| **其它变体后缀**（`_normal` 10 · `_enana…`/`_enchanatment*` 4 · `_Enchantment_*` 3 · `_enchantment_buff_hero`/`_random_stun` 2） | 19 | 0 | `show=0 / inv=10` | 不显示 | ✓ | ✓ | `cd053fb3…` Fall Back_normal · `7210fc3a…` The Fang_enchanatment1 · `aa3b6aa4…` Crushing Claws |
| **进攻卡（`spellType==200` 的非变体）** | 39 | 0 | `show=0 / inv=5` | 不显示（战斗/环境用） | ✓ | ✓ | `ASH59` · `AM63` · `SW63` Full Moon |
| **空进攻卡 = `Normal Conditions`** | 13（每阵营 1） | 0 | `show=0 / inv=10` | 不显示（默认环境） | ✓ | ✓ | `8d1fbc34…`（Black Legion）· `592d08f2…`（TauEmpire）· `239f4686…`（GSC） |
| **异画 `alternateArts` 非空** | 7（全在池） | 7 | 全 `show=5 / inv=0` | 本体正常显示；**异画实例**（`currentAlternateArt != null`）收藏窗剔（`CardCollectionTab…b__8_0.c:12-17` `IsAlternateArt()`） | 显示 | 显示 | `UM76` Titus · `SW1` Logan Grimnar · `BL34` Abaddon |
| **天赋卡（`talent_cards.json` 的 92 张去重）** | 92 | 92 | 全 `show=0 / inv=10` | 不显示（见 §三） | 滤掉 ✓ | **显示** ✗ | `c06453b3…` Lord Commander · `38871853…` Refuse to Yield · `7bf8ce67…` Diabolic Strength |
| **`Z-NC` 前缀（non-collectible，非天赋）** | 30（全在池） | 30 | `show=0 / inv=10` | 不显示 | 滤掉 ✓ | **显示** ✗ | `0740581c…` Might of Heroes · `68909cf0…` Jammed Communications · `5a8a11b2…` Antrak Silk |

### 4·3 现读总数核对

- SO 全量 **1351** = unit 605 + hero 66 + tactic 508 + defence 39 + effect-only 42 + offensive 91。
- 我们池 **1127** = unit 586 + tactic 446（406 普通 + 40 effect-only）+ hero 56 + defence 39。
- **224 张不在我们池** = 111 `_enchantment` + 10 `_oath` + 22 tutorial + 6 EASY + 2 startinghand + 1 withVanguard + 1 REMOVED + 52 进攻（含 13 空进攻卡）+ 19 其它变体后缀。**224/224 `showInCollectionOptions = 0`**。
- 我们池里 `show=0` 的 **122** = **92 天赋** + **30 `Z-NC` 效果卡**（6 Combat Elixir · 4 Mark of X enchantment · 4 Sabotage · 3 Psychic Power · 3 Rune · 3 Codicil · 7 其它）。
  ⇒ **收藏窗现在比原版多 122 张**（`CollectionWindow.CardsState` 没有这条判据）。
- ⚠️ 记忆里那套「172 = 附魔120 · 教程22 · 战役20 · EASY6 · startinghand2 · withVanguard1 · REMOVED1」是**旧快照口径**；
  当前 build 现读：教程 22 ✅、EASY 6 ✅、startinghand 2 ✅、withVanguard 1 ✅、REMOVED 1 ✅，**附魔 115 / oath 10**（不是 120/20），另有 **19 张拼错的变体后缀**（`_enanchatment` / `_Enchantment_` / `_normal`）——**别按旧数字写死**。

### 4·4 原版的「可见但灰掉」

`DeckEditorCollectionDisplay.DrawCell` 调 `CollectionCard.UpdateCardVisuals(card, canAdd, owned>0, 文本)`
（`DeckEditorCollectionDisplay__DrawCell.c:75-92`），而 `CollectionCard__UpdateCardVisuals.c:17` 是
`ToggleGreyScale(param_2 ^ 1)` ⇒ **原版对「在池子里但加不进」的卡是「显示 + 变灰」，不是隐藏**。
我们目前一律隐藏 ⇒ 这一类「灰卡」状态我们**根本没有**（见 §五）。

---

## §五 要改的清单（只列「改哪」，不写实现细节）

### A. 判据齐（可以直接照做）

1. **`RuleEngine/Core/DeckEditorState.cs` · `VisibleCards()`（`:366`）**
   - ① 加 4 层排序（§二第一行）；② 把「显示判据」从 `DeckRules.CannotAddToDeck` 换回**原版卡池那条链**：
     `showInCollectionOptions != 0` → `inventoryOptions != 5`（防御卡再按 `assignDefensiveCardsInEditMode`）→ `showInCollectionOptions != 10 || owned` → `!IsHidden`（本地无 FeatureConfig ⇒ 如实写「这一条取不到」，不要静默当成 false）。
   - ⚠️ 现有 `DeckScene.cs:1289-1450`（`TestWarlordGatedPool` + `A1798` 那一节）有一批断言**钉在「卡池走 `CannotAddToDeck`」上**
     （例如「皇家药剂一张都不该有」「`DA_Convoke_the_Circle` 不能进卡组」）——**改的是卡池判据、不是 `CannotAddToDeck`**，
     那些断言的**对象是 `DeckRules`**，不应跟着改；但 `s.VisibleCards()` 的期望张数那几条要重算。
2. **`CardPresentation/Shell/CollectionWindow.cs`（`:812-817` 附近的 `CardsState` / 或 `VisibleCards` 的共用显示判据）**
   - 收藏窗必须套 `showInCollectionOptions != Hide`（原版 `CardCollectionTab.__c___GetCollection_b__8_0.c:19-30`）⇒ 现在多出来的 **122 张**（92 天赋 + 30 `Z-NC`）要剔掉。
   - ⚠️ 收藏窗与卡组编辑共用同一个 `DeckEditorState` ⇒ 建议把「显示判据」做成**一个可选开关**（同 `WarlordGatedPool` 的先例），别把两处语义写死在一处。
3. **`CardPresentation/Deck/DeckRuntime.cs`** —— 左侧卡组列表（`:4047-4088`）**无需改**（已对齐原版）。

### B. 待裁（判据已清，但要么和用户既有口径冲突、要么牵动别的线）

4. **防御卡在卡组编辑卡池里显不显示**：原版 `Classic.assignDefensiveCardsInEditMode = false` ⇒ **经典模式不列防御卡**；
   但**用户 2026-09-27 明确要求「防御卡要在卡池里，玩家从这儿挑」**（`DeckScene.cs` 那条断言钉住了）。
   ⇒ 是「照原版按模式切」还是「保留用户口径」，要用户拍板。
5. **`show=5 & inv=10` 的 5 张（暗黑天使秘仪 `Convoke the Circle` / `None Must Know` / `Obscure Ritual` / `Rites of Penance` / `Smothering Decree`）**：
   原版**卡池里看得见**（`show=5` 过闸），点下去 `CardDeck.CanAddCard` 也放行，**但 `GetOwnedCount == 0` 会拦住**（`CardDeck__CanAddCard.c:76-77`）；
   我们单机全解锁 ⇒ 按 `||`（`DeckRules.cs:274`）把它们当「不能进卡组」并**从卡池隐藏**。
   这条 `||` 是 2026-10-14 `A1798` 的**有意选择**（记录在 `DeckRules.cs:225-241`）⇒ 只差「**显示**要不要照原版恢复（显示+灰）」。
6. **「显示 + 变灰」这一档**（§4·4）：原版对池内不可加的卡是灰显；我们要不要补，取决于 4/5 怎么定。

### C. 无差异 / 确认不用动

7. `Core/RelatedCards.cs` + `CardDetailPopup` 的天赋卡展示（§三）；`talent_cards.json` / `related_cards.json` 数据；左侧卡组列表排序；
   异画（7 张本体正常显示）。

---

## §六 没查清（缺哪个判据 + 搜过什么）

1. **`CollectionDisplay<T>.UpdateCollection / RefreshCollection` 的泛型方法体没有单独落盘**
   （`decomp_full/` 里只有 `CollectionDisplay<object>__UpdateCollection` 的**调用点**，没有本体）。
   我据调用点判断它「只存表 + 套 filter、不再排序」，**没有逐字读到**。
   搜过：`decomp_full/` 全目录 grep `UpdateCollection` / `CollectionDisplay`；`dump.cs` 的 `CollectionDisplay` 类。
2. **`PlayerItem.IsHidden()` 的运行时结果取不到** —— 它读 `FeatureConfig.IsItemHidden(item)`（服务端配置 `+0x20`/`+0x28` 两张隐藏表，
   `FeatureConfig__IsItemHidden.c:20-40`）。本地没有那份配置 ⇒ **原版可能还额外隐藏了哪些卡，查不到**；
   我只能证明「卡池/收藏两条链都调它」，不能给出具体集合。我们侧**根本不存在**这一层（如实记，别假装等价于 false）。
3. **`CollectionManager.AllCardCollection` 的实际内容**（是否等于 1351 张 SO 全量）—— 静态读不到，需要跑原版；
   本文所有「原版显示多少张」是按**当前 build SO 全量 + 判据**推的，不是实机读数。
4. **`CardArmyFilter.SetFilterToDeck` 的「有战将但 0 张卡」那一支**（`CardArmyFilter__SetFilterToDeck.c:24-36`：
   `ClearArmyFilter()` 并显示筛选条）**与用户 2026-09-27 的「没督军只列督军」口径不是同一件事** ——
   原版**从不按「只列战将」过滤**（那一支是「清空阵营筛选」）。我们那条是**用户口径的自我设计**，本文只如实列出，不判对错。
5. **`CardDisplayWindow` 相关卡数组上限**（`cardUIs` 有几格）未逐字确认；但 `relatedCard1..4` 每卡 ≤4 张，`CardFan.Slots = 9` 不会溢出，**对结果无影响**。
6. **`OrderBy` 稳定性的实测**：按 .NET 文档定性（`Enumerable.OrderBy` 为稳定排序），**没有在 IL2CPP 运行时里实测**；
   若要 100% 坐实，需要跑一次原版把这些卡的显示序 dump 出来（本地关服，进不到卡组编辑窗）。

**搜过的词 / 文件（便于下一位复核）**：
`DeckEditingWindow{,_Start,_TryOpen,_OpenCardInformation,_GetCardCollection,_FilterToDeck,_ToggleShowOwnedCards}` ·
`DeckEditorCollectionDisplay{,_DrawCell,_RefreshCells,_CheckFocusedArmy}` · `CardCollectionTab` / `AlternateArtCardCollectionTab` ·
`DeckListDrawer{,_Reorder,_Initialize}` · `CardDisplayWindow{,_ShowCard,_InitializeRelatedCards,_GetEffectsToDisplay}` ·
`RawCardScript{,_GetRelatedCards,_GetOriginalId,_IsUnit,_IsHeroEquivalent,_IsAlternateArt,_HasAlternativeArtStyles}` ·
`CardScript__GetTalentCard` · `CardDeck{,_CanAddCard,_CardCount,_GetLibraryInFull}` · `DeckUtility__ValidateDeckCards` ·
`CardArmyFilter{,_SetFilterToDeck,_FilterToArmy}` · `CardCollectionFilterController__SetFiltersToDeck` · `FeatureConfig__IsItemHidden` ·
`CollectionCard__UpdateCardVisuals` · `CollectionManager` / `GameplayVariablesData` / `CardDeck` 的 `dump.cs` 字段与 Slot；
`script.json` 的 `ScriptMetadataMethod`（用它把 `DAT_1842xxxxx` 全部落成方法名）。
