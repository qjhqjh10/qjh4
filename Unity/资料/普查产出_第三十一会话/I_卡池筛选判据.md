# I · 卡组编辑卡池筛选 —— 督军默认在不在池里（只读查证 · 第三十一会话）

## 问题1（走不走同一个 `FilterMethods`）
**结论：不走。** 卡组编辑的卡池筛选是一条**独立的 UI 筛选管线**（`CollectionFilter<T>` 家族），
与 `FilterMethods` / `TargetCriteria`（战斗选目标用的）**没有任何调用关系**。
- 链：`DeckEditingWindow.FilterToDeck()` → `CardCollectionFilterController.SetFiltersToDeck(deck)`
  （`DeckEditingWindow__FilterToDeck.c:20` → `CardCollectionFilterController__SetFiltersToDeck.c:36`
  `FUN_180001f90` 对每个 filter 调 `SetFilterToDeck`，接口 = `ICardCollectionFilter`）。
  调用点：`DeckEditingWindow__RefreshCards.c:7` · `DeckEditingWindow__TryOpen.c:134`（**每次刷池都重下发**）。
- `Deck Editing Menu` prefab 的 `CardCollectionFilterController.filters` = **6 个**（`Card Display > Card Filters`）：
  `CardNameFilter` · `CardArmyFilter` · `CardRarityFilter` · `CardCostFilter` · `CardTypeFilter` · `UpgradableCardFilter`
  （判据 = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_<pid>.json` 的 `filters` 六个 PathID，
  `MonoBehaviour_-3311978293714882780.json` = `CardTypeFilter`；实例定位 = 逐级解 `RectTransform.m_Father` 父链回 `GameObject`）。
  **没有一个 filter 碰 `TargetCriteria`。**
- `FilterMethods__*` 的**全部调用点 = 29 个 `.c`**，全是战斗/AI（`AbilityLogic__GetTargets` · `PlayerHand__*` ·
  `CemeteryManager__GetCardsPlayed` · `BoardAnalysis__DeckMeetsCriteria` · `RawCardScript__IsValidTarget` …）
  ⇒ `grep -rl "FilterMethods__" d:/2/tools/decomp_full/` 里**没有一个是 UI/卡池文件**。

## 问题2（缺省档）—— 「状态 → 参数」表
| 卡组状态 | `CardTypeFilter`（卡类） | `CardCostFilter`（费用） | `CardArmyFilter`（阵营） | **池里有没有督军** |
|---|---|---|---|---|
| **没有督军**（`deck.deckHero==null`，`CardDeck+0x40`） | `ShowOnlyHero()`：**强制只勾 `Hero(10)`**，其余关，且**自身 GameObject 隐藏**（`CardTypeFilter__ShowOnlyHero.c:28,53`；`CardTypeFilter__SetFilterToDeck.c:30-31`） | **隐藏**（`SetActive(deckHero!=null)`，`CardCostFilter__SetFilterToDeck.c:21`） | `ClearArmyFilter()` + 显示（`CardArmyFilter__SetFilterToDeck.c:36-39`） | ✅ **只出督军** |
| **有督军**（`deck.deckHero!=null`） | `ShowAll()`：**三个开关全关**（`CardTypeFilter__ShowAll.c:27,67`；`SetFilterToDeck.c:26-27`）⇒ 未勾=不过滤 | **显示** | `FilterToArmy(deck.deckArmy /*+0x50*/)`：钉死卡组阵营 + 自身隐藏（`CardArmyFilter__SetFilterToDeck.c:21-27`） | ✅ **在**（与其它类型混排） |

- 卡类开关**只有三个**：`Hero(10)`（`Card_Race/Warlord`）· `Minion(0)` · `Tactic(20)` ——
  判据 = `CardTypeFilter` 反序列化 `options`（`MonoBehaviour_-3311978293714882780.json` 的 `option: 10/0/20`）
  ⇒ **督军本身就是三个卡类开关之一**，不是被排除的第四类。
- **实况（用户桌面 `warpforge参考图/` 两张原版参考图，与代码互证）**：
  · `卡组编辑里选择督军（阵营）的参考.png`（**无督军**）→ 卡池**清一色战将**（战将 + 25 血）= `ShowOnlyHero`。
  · `卡组编辑界面参考.png`（**已有督军** 安维尔·凯尔托克 · 30/30 · 左侧督军槽已占）→ 卡池第一行就是
    **3 张督军**（伊莱亚克·泽菲列尔 / 安维尔·凯尔托克 / 梅德雷亚尔·盖兰，全「战将」+ 35~40 血），与步兵/计策并排
    ⇒ **有督军时督军照旧进池**。
- 旁证：加督军时**先移除旧的**（`DeckEditingPanel__AddCard`，见 `资料/卡组编辑界面_查证_0920.md:59`）
  ⇒ 原版必须能在池里点另一个督军换帅。
- **没有** `includeHero` 缺省为真的调用点：UI 侧连 `TargetCriteria` 都不出现（它只在战斗/AI 那 29 个文件里）。

## 问题3（我们怎么改）
1. **不要补 `FilterMethods` / `includeHero` 那道卡类门** —— 判据 = 问题1（不同管线，认错源）。
2. **我们现有形状已经等于原版**：`DeckEditorState.cs:356`（`wl==null ⇒ 只列 hero`）= `ShowOnlyHero`；
   `:357`（`wl!=null ⇒ 只该阵营`，注释明写「含其它督军」）= `ShowAll` + `FilterToArmy`；
   `:367`（`f.Type` 空 ⇒ 不限）= 三开关全关。**⇒ 新账 N1 的前提（「督军混进费用档」是缺陷）不成立，
   原版就是这么做的** —— 建议 N1 改判为「已符合原版」，别动。
3. 真要动，只该动 `:375-379` 的**档位口径**：原版 `CardCostFilter__FilterItem.c:15-20` 把 `manaCost`
   **夹到 `[1,8]`**（`<2 ⇒ 1`、`>7 ⇒ 8`），我们是**原值比较 `c.Cost`**。⚠️ 但见「没查清」——
   我们卡表 56 位督军 `cost` 全是 0，这个 0 落进原版不存在的「0 档」，**要不要照原版夹成 1，先定督军真值再说**。

## 没查清
- **督军真实费用有三种读法，没定论**：我们卡表 `cards_engine.json` = **0**（56/56）；
  原版实况卡池左上费用框读作 **2**；PnP 成品图 `d:/2/Warpforge部队卡片/Aeldari/1督军/Warpforge_1_Anvirr-Keltoc.png`
  右上读作 **5**。⇒ 关系到第 3 条该不该夹 `[1,8]`，**单独查**。
- `ToggleCollectionFilter<T,TOption>.Filter / SetupFilter / OnFilterChange` 的**方法体没有落盘**
  （`decomp_full/` 只有它的两个 lambda `ToggleCollectionFilter.__c_object__Int32Enum____Filter_b__7_0/1.c`）。
  ⛔ 我没拿序列化默认值顶替 ——「未勾选 ⇒ 不过滤」这条是**用实况图定的**（有督军那档三开关全关、池里却全部类型都在）。
- **搜过的词**：`FilterMethods` · `TargetCriteria` · `includeHero` · `CardTypeFilter` · `CardCostFilter` ·
  `CardArmyFilter` · `SetFilterToDeck` · `CardCollectionFilterController` · `ToggleCollectionFilter` · `Deck Editing`。
- **搜过的文件/目录**：`d:/2/tools/decomp_full/`（全目录）· `d:/2/Warpforge_code/Scripts/Assembly-CSharp/*.cs` ·
  `d:/2/新解包资源/assets_full/bundle_menus_assets_all/{GameObject,MonoBehaviour,RectTransform}/` ·
  `d:/4/Unity/素材/Warpforge原版/游戏数据/脚本定义/MonoScript/` · `资料/卡组编辑界面_查证_0920.md` ·
  `d:/4/Unity/MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs` · 桌面 `warpforge参考图/`。
