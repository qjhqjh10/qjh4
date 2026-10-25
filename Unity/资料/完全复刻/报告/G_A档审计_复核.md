# 报告 · A 档审计【复核】

> 代理 G · 2026-10-25 · **其它 12 份域账只读**（一个字没改）；**我自己那六份**在复核后做了收口（见 §〇 与 §四）。
> 基线 = `报告/G_A档审计_跨账.md`（报表头写 1,508；**逐条可解析 1,506**，差 2 条是整类行/类名带点·没进表）· 判法 = 三步定位 + 语义近名检测。
> ⚠️ 复核期间 owner 仍在改账（`锻造` 一度冒出 20 条工具失败、几分钟后清零；A 条数也在动）⇒ 本报告是**某一刻的快照**。

## ⚠️ 免责（最重要的一句）

**`check_ledger_points.py` 报「通过」≠ 合格，只等于「落点形态合法」。**
它查不出「这一行是**另一个方法**的声明」——`AddCard` 指到 `CanAddCard`、`GetX` 指到 `SetX`、`Init` 指到 `Reset`。
本报告 §三 的两张「语义近名」表就是补它查不出的那一半；反之**工具报失败 ⇒ 一定不合格**。

## 〇 开工前修掉的 9 条（D 的工具查出、属于我的账）

| 账 | 类.方法 | 旧落点（问题） | 改为 | 依据 |
|---|---|---|---|---|
| 教程_引导 | `BattleTutorialManager.Initialize` | `BattleDriver.cs:4537`（空行） | `Battle/BattleDriver.cs:15992` | `EnsureTutorialOverlay` |
| 教程_引导 | `BattleTutorialManager.Update` | `:13467`（注释） | `Battle/TutorialOverlay.cs:1129` | `TrySkip` |
| 教程_引导 | `FlowTutorialStage.TryExecute` | `MenuTutorialFlow.cs:231`（语句） | `Shell/MenuTutorialFlow.cs:286` | `FirstPlayableStage` |
| 教程_引导 | `TutorialManager.DelayedPLayTutorial` | `:272`（语句） | `Shell/MenuTutorialFlow.cs:223` | `StartFlow` |
| 泛型与启动链 | `ItemDrawer<T>.Draw(T,int)` | `ItemDrawer.cs:1483`（语句） | `Shell/ItemDrawer.cs:840` | 唯一一份 `Draw(...)` 声明 |
| 泛型与启动链 | `ToggleCollectionFilter<T>.Filter` | `DeckEditorState.cs:904`（语句） | `Deck/DeckEditorState.cs:529` | `VisibleCards` |
| 社交_聊天 | `AllianceBadgeDrawer.Draw(GroupBadge)` | `ItemDrawer.cs:1483`（语句） | `Shell/ItemDrawer.cs:840` | 同一份 Draw 承载全部子类 |
| 社交_聊天 | `ChatPreview.Initialize` | `MainMenuRuntime.cs:2246`（语句） | `Shell/MainMenuRuntime.cs:2254` | `BuildChatPreview` |
| 个人资料 | `AvatarDrawer.Draw(PlayerAvatar,int)` | `ItemDrawer.cs:1483`（语句） | `Shell/ItemDrawer.cs:840` | 同上 |

`--fix` 已跑六份账：**漂移 0**。现在这六份的**工具口径失败 = 0**（`其他_基础设施` 剩 2 条属 C 的账）。

## 一 被扫文件清单（先看这张表再看数字）

| 账 | 复核时 A | 工具 A | 工具失败 | 工具漂移 | 基线内？ |
|---|---:|---:|---:|---:|---|
| `战斗逻辑驱动.tsv` | 151 | 151 | 0 | 5 | 是 |
| `特效动画.tsv` | 93 | 93 | 0 | 0 | 是 |
| `菜单_外壳.tsv` | 275 | 275 | 0 | 0 | 是 |
| `战斗界面_HUD.tsv` | 415 | 415 | 0 | 4 | 是 |
| `联机.tsv` | 9 | 9 | 0 | 0 | 是 |
| `卡牌面_渲染.tsv` | 100 | 100 | 0 | 5 | 是 |
| `卡组编辑.tsv` | 176 | 176 | 0 | 0 | 是 |
| `收藏.tsv` | 137 | 137 | 0 | 0 | 是 |
| `商店.tsv` | 112 | 112 | 0 | 0 | 是 |
| `轮抽.tsv` | 98 | 98 | 0 | 6 | 是 |
| `战役.tsv` | 60 | 60 | 0 | 0 | 是 |
| `锻造.tsv` | 54 | 54 | 0 | 0 | 是 |
| `其他_基础设施.tsv` | 11 | 11 | 2 | 0 | 否（我的账） |
| `未分类_其他.tsv` | 31 | 31 | 0 | 0 | 否（我的账） |
| `泛型与启动链_补读.tsv` | 73 | 73 | 0 | 0 | 否（我的账） |
| `社交_聊天.tsv` | 24 | 24 | 0 | 0 | 否（我的账） |
| `个人资料_成就_排行.tsv` | 52 | 52 | 0 | 0 | 否（我的账） |
| `教程_引导.tsv` | 52 | 52 | 0 | 0 | 否（我的账） |

> `--all-A` 共扫 **18 份**（`日常_奖励`/`音频_语音`/`本地化` 无 A）。
> 📌 **「152 条口径差」两个原因都成立**：① **扫漏** —— 基线没扫我自己六份（243 条）；② **期间变更** —— 域账 A 1,784 → 1680（`菜单_外壳` 344→275、`卡牌面` 108→100、`战役` 68→60…）。两个数**不能直接相减**。

## 二 交付物 ①：修好了多少条

| 账 | 基线问题 | 现在硬问题 | 仍不合格 | **修好了** | 新出现 |
|---|---:|---:|---:|---:|---:|
| `战斗逻辑驱动.tsv` | 129 | 2 | 2 | **127** | 0 |
| `特效动画.tsv` | 39 | 3 | 3 | **36** | 0 |
| `菜单_外壳.tsv` | 255 | 5 | 3 | **252** | 2 |
| `战斗界面_HUD.tsv` | 342 | 29 | 28 | **314** | 1 |
| `联机.tsv` | 7 | 0 | 0 | **7** | 0 |
| `卡牌面_渲染.tsv` | 108 | 1 | 1 | **107** | 0 |
| `卡组编辑.tsv` | 177 | 155 | 155 | **22** | 0 |
| `收藏.tsv` | 141 | 100 | 100 | **41** | 0 |
| `商店.tsv` | 113 | 82 | 81 | **32** | 1 |
| `轮抽.tsv` | 99 | 9 | 9 | **90** | 0 |
| `战役.tsv` | 68 | 2 | 2 | **66** | 0 |
| `锻造.tsv` | 28 | 7 | 7 | **21** | 0 |
| `其他_基础设施.tsv` | 0 | 1 | 0 | **0** | 1 |
| `未分类_其他.tsv` | 0 | 0 | 0 | **0** | 0 |
| `泛型与启动链_补读.tsv` | 0 | 0 | 0 | **0** | 0 |
| `社交_聊天.tsv` | 0 | 0 | 0 | **0** | 0 |
| `个人资料_成就_排行.tsv` | 0 | 0 | 0 | **0** | 0 |
| `教程_引导.tsv` | 0 | 0 | 0 | **0** | 0 |
| **合计** | **1506** | **396** | **391** | **1115** | **5** |

> 另：**待确认的软问题 224 条**（语义近名但有注解 / 异名但备注写了等价）—— 见 §三·b、§三·c。
> 我自己六份账基线里没有，其问题不记「新出现」，单列在 §四。

## 三 交付物 ②③：仍不合格 + 新造出来的假精确

### a. 仍不合格（基线就有、现在还没好）—— 逐条

#### 战斗逻辑驱动.tsv（2 条）

| 类.方法 | 落点 | 现在的问题 | 基线问题 | 建议 |
|---|---|---|---|---|
| `BattleManager.ResolveDrawCard(BattleAction battleAction)` | `RuleEngine/Core/RuleCore.cs:2812（Draw） ｜ ` ｜ RuleCore.Draw` | 语义近名指错(无注解) | 仅类.方法(可补行号) | — |
| `CardScript.DrawCard(Vector3 pos, Vector3 newScale, float timeToDraw)` | `RuleEngine/Core/RuleCore.cs:2812（Draw） ｜ ` ｜ RuleCore.Draw` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |

#### 特效动画.tsv（3 条）

| 类.方法 | 落点 | 现在的问题 | 基线问题 | 建议 |
|---|---|---|---|---|
| `Card3DAnimationController.Start()` | `CardPresentation/Battle/Card3DAnimationController.cs:162（构` | 指向别的方法/字段 | 指向别的方法/字段 | — |
| `TweenInfoBase.GetTween(CardScript source, CardScript target)` | `CardPresentation/Core/UnitTweenTable.cs:172（`Build`）` | 指向别的方法/字段 | 指向别的方法/字段 | — |
| `UnitTweenSO.BuildSequence(CardScript source, CardScript target, Action` | `CardPresentation/Core/UnitTweenTable.cs:172（`Build`）+ `Cor` | 指向别的方法/字段 | 指向别的方法/字段 | — |

#### 菜单_外壳.tsv（3 条）

| 类.方法 | 落点 | 现在的问题 | 基线问题 | 建议 |
|---|---|---|---|---|
| `ItemInformationPanel.Initialize(ObtainableItem Item, ItemDrawerOptions Options)` | `CardPresentation/Shell/ItemDisplayWindow.cs:208（Create）` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `MainMenuWindow.Initialize()` | `CardPresentation/Shell/MainMenuRuntime.cs:401（MainMenuRunt` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `PopUpGameWindow.ConfigurePopUp(string text, bool localizeTexts, bool close` | `CardPresentation/Shell/PopUpGameWindow.cs:290（Create）` | 指向别的方法/字段 | 文件级(可补行号) | — |

#### 战斗界面_HUD.tsv（28 条）

| 类.方法 | 落点 | 现在的问题 | 基线问题 | 建议 |
|---|---|---|---|---|
| `ActiveSkillDisplay.InitialSetup()` | `CardPresentation/Battle/SkillPanel.cs:680（`Build`）` | 指向别的方法/字段 | 指向别的方法/字段 | — |
| `BattleCardManager.CreateCard(RawCardScript rawCardData, Transform parent, bo` | `CardPresentation/Core/CardView.cs:2136（`Create`）+ :4176（`B` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `BattleCardUI.BattleCardSetup(CardArmy mainArmy, bool isPlayer)` | `CardPresentation/Core/CardView.cs:4176（`Build`）` | 指向别的方法/字段 | 仅类/描述(无方法) | — |
| `BattleCardUI.FinishMulligan()` | `CardPresentation/Battle/MulliganPanel.cs:967（`Close`）` | 指向别的方法/字段 | 文件级 | — |
| `BattleCardUI.GetTraitListForCardIcons(RawCardScript cardToDisplay)` | `CardPresentation/Core/Badges.cs:532（`For`）` | 指向别的方法/字段 | 文件级＋被多方法共用(3) | — |
| `BattleSettingsWindow.Awake()` | `CardPresentation/Battle/SettingsPanel.cs:939（`Build`）` | 指向别的方法/字段 | 数据表项(需确认是否纯查表) | — |
| `BattleSettingsWindow.RegisterToClose(Action closeAction)` | `CardPresentation/Battle/SettingsPanel.cs:887（`Create`：关窗回调` | 指向别的方法/字段 | 文件级 | — |
| `CardDisplayWindow.AddCardsListeners()` | `CardPresentation/Battle/CardDisplayWindow.cs:569（`Build`）` | 指向别的方法/字段 | 文件级 | — |
| `CardDisplayWindow.Open()` | `CardPresentation/Battle/CardDisplayWindow.cs:414（`Create`）` | 指向别的方法/字段 | 指向别的方法/字段 | — |
| `CemeteryManager.Setup()` | `CardPresentation/Battle/BattleLogPanel.cs:775（`Create`）` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `ChooseCardMenu.GetTittleText(string uniqueId)` | `CardPresentation/Battle/ChoosePanel.cs:218（`Open`）` | 指向别的方法/字段 | 指向别的方法/字段 | — |
| `ClockManager.ChangedLanguage()` | `CardPresentation/Battle/BattleDriver.cs:13581（`Update`）` | 指向别的方法/字段 | 注释＋被多方法共用(3) | — |
| `ClockManager.EndCountdown()` | `CardPresentation/Battle/BattleDriver.cs:13581（`Update`）` | 指向别的方法/字段 | 函数中间 | — |
| `ClockManager.EndTurn(bool timeOutFlag)` | `CardPresentation/Battle/BattleDriver.cs:13581（`Update`）` | 指向别的方法/字段 | 注释 | — |
| `ClockManager.SetEndTurnText(bool isPlayer)` | `CardPresentation/Battle/BattleDriver.cs:13581（`Update`）` | 指向别的方法/字段 | 注释＋被多方法共用(3) | — |
| `ClockManager.SetEnemyTurnUI()` | `CardPresentation/Battle/BattleDriver.cs:13581（`Update`）` | 指向别的方法/字段 | 注释＋被多方法共用(3) | — |
| `ClockManager.StartCountdown()` | `CardPresentation/Battle/BattleDriver.cs:13581（`Update`）` | 指向别的方法/字段 | 注释 | — |
| `EverguildTooltipManager.Awake()` | `CardPresentation/Core/Tooltip.cs:252（`Build`）` | 指向别的方法/字段 | 指向别的方法/字段 | — |
| `FlyingCardController.FlyingState()` | `CardPresentation/Battle/FlyingCardController.cs:189（`Updat` | 指向别的方法/字段 | 指向别的方法/字段 | — |
| `MulliganManager.ActivateMulligan(bool goFirst)` | `CardPresentation/Battle/MulliganPanel.cs:960（`Open`）` | 指向别的方法/字段 | 指向别的方法/字段 | — |
| `MulliganManager.ProcessMulliganDone()` | `CardPresentation/Battle/MulliganPanel.cs:960（`Open`）` | 指向别的方法/字段 | 指向别的方法/字段 | — |
| `MulliganManager.Setup()` | `CardPresentation/Battle/MulliganPanel.cs:960（`Open`）` | 指向别的方法/字段 | 文件级 | — |
| `MulliganManager.StopMulligan()` | `CardPresentation/Battle/MulliganPanel.cs:967（`Close`）` | 指向别的方法/字段 | 指向别的方法/字段 | — |
| `PracticeModePopup.LoadSavedDeck(string savedDeck)` | `CardPresentation/Shell/PracticeModePopup.cs:1241（`Open`）` | 指向别的方法/字段 | 注释 | — |
| `PracticeModePopup.OnDestroy()` | `CardPresentation/Shell/PracticeModePopup.cs:1323（`Build`）` | 指向别的方法/字段 | 函数中间 | — |
| `TargetReticleController.AutoMovingCrosshair()` | `CardPresentation/Battle/TargetReticle.cs:563（`Aim`）` | 指向别的方法/字段 | 指向别的方法/字段 | — |
| `TargetReticleController.Initialize()` | `CardPresentation/Battle/TargetReticle.cs:361（`Build`）` | 指向别的方法/字段 | 指向别的方法/字段 | — |
| `TargetReticleController.MoveCrosshair(Vector3 originPositionBoardSpace, Vector3 ta` | `CardPresentation/Battle/TargetReticle.cs:563（`Aim`）` | 指向别的方法/字段 | 指向别的方法/字段 | — |

#### 联机.tsv（0 条）

✅ 基线里的问题已全部处理。

#### 卡牌面_渲染.tsv（1 条）

| 类.方法 | 落点 | 现在的问题 | 基线问题 | 建议 |
|---|---|---|---|---|
| `CardHighlight.Start()` | `CardPresentation/Core/CardView.cs:4176（`Build`：出生路径）` | 指向别的方法/字段 | 仅类/描述(无方法) | — |

#### 卡组编辑.tsv（155 条）

| 类.方法 | 落点 | 现在的问题 | 基线问题 | 建议 |
|---|---|---|---|---|
| `CardDeck.AddCard(RawCardScript card)` | `Deck/DeckEditorState.cs:434` | 指向别的方法/字段 | 仅类.方法(解析不到) | — |
| `CardDeck.AddDefensiveCard(RawCardScript card)` | `Deck/DeckEditorState.cs:434` | 指向别的方法/字段 | 仅类.方法(解析不到) | — |
| `CardDeck.AddHero(RawCardScript card)` | `MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs:453` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardDeck.AreThereInvalidArmyCards()` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:443` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardDeck.CanAddCard(RawCardScript card, out string error)` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:694` | 语义近名指错(无注解) | 仅类.方法(可补行号) | — |
| `CardDeck.CanAddDefensive(RawCardScript card, out string error)` | `MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs:394` | 指向别的方法/字段 | 仅类.方法(解析不到) | — |
| `CardDeck.CanAddHero(RawCardScript card, out string error)` | `Deck/DeckEditorState.cs:394` | 指向别的方法/字段 | 仅类/描述(无方法) | — |
| `CardDeck.CardCopiesInDeck(RawCardScript card)` | `RuleEngine/Core/DeckRules.cs:709` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardDeck.CardCount(bool countHero)` | `Deck/DeckEditorState.cs:385` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardDeck.CopyDeck(CardDeck deckToCopy)` | `MyGame/Assets/RuleEngine/Data/DeckLibrary.cs:335` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardDeck.DeckBasicSetup(string pDeckName, RawCardScript pDeckHero, ` | `RuleEngine/Core/DeckRules.cs:678` | 指向别的方法/字段 | 文件级(可补行号) | — |
| `CardDeck.DeserializeDeckString(string deckString)` | `MyGame/Assets/RuleEngine/Data/DeckLibrary.cs:452` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardDeck.EmptySlotsCount()` | `Deck/DeckEditorState.cs:383` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardDeck.GetDeckCardback()` | `MyGame/Assets/CardPresentation/Core/CardArt.cs:983` | 语义近名指错(无注解) | 仅类.方法(可补行号) | — |
| `CardDeck.GetEmptyDeck()` | `MyGame/Assets/RuleEngine/Data/DeckLibrary.cs:187` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardDeck.GetLibraryInFull()` | `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs:4189` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardDeck.Has(RawCardScript card)` | `RuleEngine/Core/DeckRules.cs:709` | 指向别的方法/字段 | 仅类.方法(解析不到) | — |
| `CardDeck.LegendaryCardsInDeck()` | `RuleEngine/Core/DeckRules.cs:443` | 指向别的方法/字段 | 仅类/描述(无方法) | — |
| `CardDeck.RemoveCard(RawCardScript card)` | `MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs:471` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardDeck.RemoveDefensiveCard()` | `Deck/DeckEditorState.cs:471` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardDeck.RemoveNoValidArmyCards()` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:400` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardDeck.Serialize(CardDeck deck)` | `MyGame/Assets/RuleEngine/Data/DeckLibrary.cs:429` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardDeck.TryAddCard(RawCardScript card)` | `MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs:434` | 指向别的方法/字段 | 仅类.方法(解析不到) | — |
| `CardDeck.TryAddCard(RawCardScript card, out string error)` | `MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs:434` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardDeck.TryAddDefensive(RawCardScript card, out string error)` | `MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs:434` | 指向别的方法/字段 | 仅类.方法(解析不到) | — |
| `CardDeck.TryAddHero(RawCardScript card, out string error)` | `MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs:434` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardDeck._003CGetDeckCardback_003Eb__56_0(CosmeticItem x)` | `MyGame/Assets/CardPresentation/Core/CardArt.cs:983` | 语义近名指错(无注解) | 仅类.方法(可补行号) | — |
| `CollectionDeck.Awake()` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:5` | 指向别的方法/字段 | 文件级＋被多方法共用(3) | — |
| `CollectionDeck.Config(CardDeck item, GameplayVariablesData gameplayVariab` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:5` | 指向别的方法/字段 | 文件级＋被多方法共用(3) | — |
| `CollectionDeck.HoverHighlight(bool state)` | `Shell/CollectionWindow.cs:5025` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CollectionDeck.OnDestroy()` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:5` | 指向别的方法/字段 | 文件级＋被多方法共用(3) | — |
| `DeckArmyFilter.FilterItem(CardDeck deck)` | `Deck/DeckEditorState.cs:97` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckCollectionTab.GetCollection()` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:4` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckCollectionTab.OnItemSelected(CollectionItem<CardDeck> item, PointerEvent` | `Shell/CollectionWindow.cs:5280` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckCollectionTab.OnOpen()` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:3` | 语义近名指错(无注解) | 仅类.方法(可补行号) | — |
| `DeckCollectionTab.RefreshCollection()` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:5` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckCollectionTab.SetEventType(IOwnDeckPlayEvent eventToAssign)` | `Shell/CollectionWindow.cs:4549` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckCollectionTab._003CGetCollection_003Eb__7_0(CardDeck deck)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:4` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckCosmeticDrawer.Initialize(CardDeck deck)` | `Deck/DeckRuntime.cs:3597` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckEditingHeader.SetInstructions(string text)` | `Deck/DeckRuntime.cs:4745` | 指向别的方法/字段 | 文件级 | — |
| `DeckEditingPanel.AddCard(RawCardScript card, bool withAnim)` | `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs:8246` | 语义近名指错(无注解) | 文件级＋被多方法共用(10) | — |
| `DeckEditingPanel.ChangeName(string name)` | `Deck/DeckRuntime.cs:6235` | 指向别的方法/字段 | 文件级＋被多方法共用(10) | — |
| `DeckEditingPanel.CheckCardSlotDrag(PointerEventData eventData, GameObject s` | `Deck/DeckRuntime.cs:5335` | 指向别的方法/字段 | 文件级＋被多方法共用(10) | — |
| `DeckEditingPanel.Initialize(CardDeck deck)` | `Deck/DeckRuntime.cs:1428` | 指向别的方法/字段 | 文件级＋被多方法共用(10) | — |
| `DeckEditingPanel.RemoveCard(RawCardScript card)` | `Deck/DeckEditorState.cs:471` | 指向别的方法/字段 | 文件级＋被多方法共用(10) | — |
| `DeckEditingPanel.ToggleCosmetics()` | `Deck/DeckRuntime.cs:6117` | 指向别的方法/字段 | 文件级＋被多方法共用(10) | — |
| `DeckEditingPanel.ToggleDeckInfo()` | `Deck/DeckRuntime.cs:6117` | 指向别的方法/字段 | 文件级＋被多方法共用(10) | — |
| `DeckEditingPanel.ToggleDeckList()` | `Deck/DeckRuntime.cs:6117` | 指向别的方法/字段 | 文件级＋被多方法共用(10) | — |
| `DeckEditingPanel.UpdateDrawers()` | `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs:1205` | 指向别的方法/字段 | 仅类/描述(无方法) | — |
| `DeckEditingStateController.Initialize(DeckEditingWindow editingWindow)` | `Deck/DeckRuntime.cs:1276` | 指向别的方法/字段 | 仅类/描述(无方法)＋被多方法共用(4) | — |
| `DeckEditingStateController.ToggleDeckCosmetic(bool state)` | `Deck/DeckRuntime.cs:3516` | 指向别的方法/字段 | 仅类/描述(无方法)＋被多方法共用(4) | — |
| `DeckEditingStateController.ToggleDeckInfo(bool state)` | `Deck/DeckRuntime.cs:6117` | 指向别的方法/字段 | 仅类/描述(无方法)＋被多方法共用(4) | — |
| `DeckEditingStateController.ToggleDeckList(bool state)` | `Deck/DeckRuntime.cs:6117` | 指向别的方法/字段 | 仅类/描述(无方法)＋被多方法共用(4) | — |
| `DeckEditingWindow.CheckCardDrag(PointerEventData eventData, GameObject sourc` | `Deck/DeckRuntime.cs:4058` | 语义近名指错(无注解) | 文件级＋被多方法共用(6) | — |
| `DeckEditingWindow.CheckCosmeticDrag(PointerEventData eventData, GameObject s` | `Deck/DeckRuntime.cs:4028` | 语义近名指错(无注解) | 文件级＋被多方法共用(6) | — |
| `DeckEditingWindow.ConfirmDiscard()` | `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs:5670` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckEditingWindow.FilterToDeck()` | `Deck/DeckRuntime.cs:7424` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckEditingWindow.GetCardCollection()` | `MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs:529` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckEditingWindow.GetCosmeticCollection()` | `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs:3516` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckEditingWindow.OnCardClick(CollectionItem<RawCardScript> card, PointerEve` | `Deck/DeckRuntime.cs:5158` | 指向别的方法/字段 | 文件级＋被多方法共用(6) | — |
| `DeckEditingWindow.OnCosmeticClick(CollectionItem<CosmeticItemCardback> cosme` | `Deck/DeckRuntime.cs:3689` | 语义近名指错(无注解) | 文件级＋被多方法共用(6) | — |
| `DeckEditingWindow.OpenCardInformation(RawCardScript card)` | `MyGame/Assets/CardPresentation/Shell/DeckEditingWindow.cs:` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckEditingWindow.Refresh()` | `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs:4136` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckEditingWindow.RefreshCards()` | `Deck/DeckRuntime.cs:2980` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckEditingWindow.ToggleDisplay(bool showCards)` | `Deck/DeckRuntime.cs:7029` | 指向别的方法/字段 | 仅类/描述(无方法) | — |
| `DeckEditingWindow.ToggleShowOwnedCards(bool show)` | `Deck/DeckRuntime.cs:7738` | 指向别的方法/字段 | 文件级＋被多方法共用(6) | — |
| `DeckEditingWindow.ToggleShowOwnedCosmetics(bool show)` | `Deck/DeckRuntime.cs:7904` | 指向别的方法/字段 | 文件级＋被多方法共用(6) | — |
| `DeckEditingWindow.TryOpen(object data, GameWindowOptions options)` | `MyGame/Assets/CardPresentation/Shell/DeckEditingWindow.cs:` | 语义近名指错(无注解) | 仅类.方法(可补行号) | — |
| `DeckEditingWindow.TrySaveDeck()` | `Deck/DeckRuntime.cs:5546` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckEditingWindow.UpdateDoneButton(bool firstTime)` | `Deck/DeckRuntime.cs:4745` | 指向别的方法/字段 | 仅类/描述(无方法) | — |
| `DeckEditingWindow.UploadDeck()` | `Deck/DeckRuntime.cs:8315` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckEditingWindow._003CConfirmDiscard_003Eb__45_1(GameObject GameObject)` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:443` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(4) | — |
| `DeckEditingWindow._003CGetCardCollection_003Eb__32_1(RawCardScript c)` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:443` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(4) | — |
| `DeckEditingWindow._003CGetCardCollection_003Eb__32_4(RawCardScript c)` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:443` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(4) | — |
| `DeckEditingWindow._003CTrySaveDeck_003Eb__42_1(GameObject _)` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:443` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(4) | — |
| `DeckEditorCollectionDisplay.DrawCell(CollectionCard card)` | `Deck/DeckRuntime.cs:2980` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(3) | — |
| `DeckEditorCollectionDisplay.OnListDrag(Vector2 listPos)` | `Deck/DeckRuntime.cs:7089` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckEditorCollectionDisplay.RefreshCells()` | `Deck/DeckRuntime.cs:2980` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(3) | — |
| `DeckEditorCollectionDisplay.RefreshCollection()` | `Deck/DeckRuntime.cs:4745` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckEditorCollectionDisplay.SetCell(ICell cell, int index)` | `Deck/DeckRuntime.cs:3225` | 语义近名指错(无注解) | 仅类.方法(可补行号)＋被多方法共用(3) | — |
| `DeckEditorCollectionDisplay.SetEditingDeck(CardDeck deck)` | `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs:1276` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckInfoControls.Initialize(DeckInfoPopup popup, DeckInfoContext context, b` | `MyGame/Assets/CardPresentation/Shell/DeckInfoPopup.cs:2333` | 指向别的方法/字段 | 文件级(可补行号) | — |
| `DeckInfoControls.OnDisable()` | `MyGame/Assets/CardPresentation/Shell/DeckInfoPopup.cs:2333` | 指向别的方法/字段 | 文件级(可补行号) | — |
| `DeckInfoDrawer.Initialize(CardDeck deck)` | `Shell/DeckInfoPopup.cs:2109` | 指向别的方法/字段 | 文件级 | — |
| `DeckInfoPopup.CardSelected(RawCardScript card)` | `Shell/DeckInfoPopup.cs:1790` | 指向别的方法/字段 | 仅类/描述(无方法) | — |
| `DeckInfoPopup.DeleteDeck()` | `MyGame/Assets/CardPresentation/Shell/DeckInfoPopup.cs:2436` | 语义近名指错(无注解) | 仅类.方法(可补行号) | — |
| `DeckInfoPopup.EditDeck()` | `Shell/CollectionWindow.cs:5448` | 指向别的方法/字段 | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:5409` |
| `DeckInfoPopup.MakeDeckString()` | `MyGame/Assets/RuleEngine/Data/DeckLibrary.cs:429` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckInfoPopup.OnWarlordClick()` | `Shell/DeckInfoPopup.cs:2446` | 指向别的方法/字段 | 仅类/描述(无方法) | — |
| `DeckInfoPopup.Open()` | `Shell/DeckInfoPopup.cs:1214` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Shell/DeckInfoPopup.cs:1234` |
| `DeckInfoPopup.ShareDeckOnChat(Transform optionsMountPoint)` | `MyGame/Assets/CardPresentation/Shell/DeckInfoPopup.cs:2505` | 语义近名指错(无注解) | 文件级＋被多方法共用(4) | — |
| `DeckInfoPopup.ToggleDrawers(bool showList)` | `Shell/DeckInfoPopup.cs:2468` | 指向别的方法/字段 | 仅类.方法(解析不到) | — |
| `DeckInfoPopup._003CDeleteDeck_003Eb__27_2(GameObject _)` | `MyGame/Assets/CardPresentation/Shell/DeckInfoPopup.cs:2436` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(3) | — |
| `DeckInfoPopup._003CDeleteDeck_003Eb__27_3(string _)` | `MyGame/Assets/CardPresentation/Shell/DeckInfoPopup.cs:2436` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(3) | — |
| `DeckInfoPopup._003CDeleteDeck_003Eg__Delete_007C27_0()` | `MyGame/Assets/CardPresentation/Shell/DeckInfoPopup.cs:2436` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(3) | — |
| `DeckInventory.GetItems()` | `RuleEngine/Data/DeckLibrary.cs:141` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckListDrawer.AddCardToList(RawCardScript card, bool reorder, bool withA` | `Deck/DeckRuntime.cs:4288` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(3) | — |
| `DeckListDrawer.CleanPrevious()` | `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs:4288` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(3) | — |
| `DeckListDrawer.ConfigPrefab(UICardInfoItem cardInfo)` | `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs:4288` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(3) | — |
| `DeckListDrawer.Initialize(CardDeck cardDeck, DeckListOptions options)` | `Deck/DeckRuntime.cs:2006` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckListDrawer.Initialize(List<RawCardScript> cardList, DeckListOptions o` | `Deck/DeckRuntime.cs:4189` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckListDrawer.Initialize(PrebuiltDeck cardDeck, DeckListOptions options)` | `Deck/DeckRuntime.cs:4260` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckListDrawer.RemoveCardFromList(RawCardScript card)` | `Deck/DeckRuntime.cs:4288` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(3) | — |
| `DeckListDrawer.Reorder()` | `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs:4189` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckListDrawer.SetCount()` | `Deck/DeckRuntime.cs:4281` | 语义近名指错(无注解) | 仅类/描述(无方法) | — |
| `DeckListDrawer.SetOptions(DeckListOptions options)` | `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs:4288` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(3) | — |
| `DeckListDrawer.ShowEmptySlotsIfNeeded()` | `Deck/DeckRuntime.cs:4260` | 指向别的方法/字段 | 文件级(可补行号) | — |
| `DeckListDrawer.SpawnCardInfo(RawCardScript card, bool withAnim)` | `Deck/DeckRuntime.cs:2006` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(3) | — |
| `DeckListDrawer._003CSpawnCardInfo_003Eb__21_0(RawCardScript c)` | `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs:4189` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckNameFilter.FillToggleList(List<CollectionFilterToggle> currentList, i` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:4` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(3) | — |
| `DeckNameFilter.Filter(List<CardDeck> collection)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:2` | 语义近名指错(无注解) | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:1222` |
| `DeckNameFilter.SetupFilter(CollectionFilterController<CardDeck> controlle` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:4` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(3) | — |
| `DeckNameFilter._003CSetupFilter_003Eb__1_0(string s)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:4` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(3) | — |
| `DeckSelectionPopup.Initialize(IEnumerable<CardDeck> decks, CardDeck selectedD` | `Shell/DeckSelectionPopup.cs:470` | 指向别的方法/字段 | 文件级＋被多方法共用(8) | — |
| `DeckSelectionPopup.Select(CardDeck deck)` | `Shell/DeckSelectionPopup.cs:866` | 指向别的方法/字段 | 文件级＋被多方法共用(8) | — |
| `DeckSelectionPopup.ToggleDecksDifficultyLabels(bool option)` | `Shell/DeckSelectionPopup.cs:804` | 指向别的方法/字段 | 文件级＋被多方法共用(8) | — |
| `DeckSelectionPopup.TryOpen(object data, GameWindowOptions options)` | `MyGame/Assets/CardPresentation/Shell/DeckSelectionPopup.cs` | 语义近名指错(无注解) | 文件级＋被多方法共用(8) | — |
| `DeckSelectionPopup._003CTryOpen_003Eb__10_0(CardDeck d)` | `MyGame/Assets/CardPresentation/Shell/DeckSelectionPopup.cs` | 语义近名指错(无注解) | 文件级＋被多方法共用(8) | — |
| `DeckSelectionTabController.Awake()` | `Shell/DeckSelectionPopup.cs:372` | 指向别的方法/字段 | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController.Filter(IEnumerable<CardDeck> decks)` | `Shell/DeckSelectionPopup.cs:432` | 指向别的方法/字段 | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController.RandomizeDeck()` | `Shell/DeckSelectionPopup.cs:858` | 指向别的方法/字段 | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController.Refresh()` | `Shell/DeckSelectionPopup.cs:883` | 指向别的方法/字段 | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController.ShowOwnDecks(bool state)` | `Shell/DeckSelectionPopup.cs:767` | 指向别的方法/字段 | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController.ShowPrebuiltDecks(bool state)` | `Shell/DeckSelectionPopup.cs:432` | 指向别的方法/字段 | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController.Start()` | `Shell/DeckSelectionPopup.cs:397` | 指向别的方法/字段 | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController._003CAwake_003Eb__6_5(string _)` | `Shell/DeckSelectionPopup.cs:372` | 指向别的方法/字段 | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController._003CFilter_003Eb__12_0(CardDeck d)` | `Shell/DeckSelectionPopup.cs:432` | 指向别的方法/字段 | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController._003CRandomizeDeck_003Eb__7_0(CardDeck d)` | `Shell/DeckSelectionPopup.cs:858` | 指向别的方法/字段 | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController._003CShowOwnDecks_003Eb__10_0(CardDeck d)` | `Shell/DeckSelectionPopup.cs:767` | 指向别的方法/字段 | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController._003CShowPrebuiltDecks_003Eb__11_0(CardDeck d)` | `Shell/DeckSelectionPopup.cs:432` | 指向别的方法/字段 | 文件级＋被多方法共用(12) | — |
| `DeckTypeCounterDrawer.Initialize(CardDeck deck)` | `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs:2463` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckUtility.GetDeckLibrary(CardDeck deck)` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:443` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckUtility.RemoveCardFromDeck(RawCardScript card, CardDeck deck, [In]` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:701` | 语义近名指错(无注解) | 文件级(可补行号)＋被多方法共用(6) | — |
| `DeckUtility.ToLocalizedString(DeckError error)` | `RuleEngine/Core/DeckRules.cs:578` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckUtility.ToRawLocalizationString(DeckError error)` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:578` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `DeckUtility.TryRemoveCardsNotOwned(CardDeck deck, List<RawCardScript> ` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:701` | 语义近名指错(无注解) | 文件级(可补行号)＋被多方法共用(6) | — |
| `DeckUtility.TryRemoveInvalidCards(CardDeck deck, List<RawCardScript> r` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:400` | 语义近名指错(无注解) | 文件级(可补行号)＋被多方法共用(6) | — |
| `DeckUtility.TryRemoveInvalidHero(CardDeck deck, List<RawCardScript> re` | `RuleEngine/Core/DeckRules.cs:400` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(6) | — |
| `DeckUtility.ValidateDeck(CardDeck deck, out DeckError error, bool vali` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:443` | 语义近名指错(无注解) | 仅类.方法(可补行号) | — |
| `DeckUtility.ValidateDeckCards(CardDeck deck, Dictionary<RawCardScript,` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:443` | 语义近名指错(无注解) | 文件级(可补行号)＋被多方法共用(6) | — |
| `DeckUtility.ValidateDeckOwnership(CardDeck deck, Dictionary<RawCardScr` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:443` | 语义近名指错(无注解) | 文件级(可补行号)＋被多方法共用(6) | — |
| `DeckUtility.ValidateDeckWarlord(CardDeck deck, out DeckError error)` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:443` | 语义近名指错(无注解) | 仅类.方法(可补行号) | — |
| `ImportDeckPopup.OnDisable()` | `Shell/ImportDeckPopup.cs:314` | 指向别的方法/字段 | 文件级＋被多方法共用(4) | — |
| `ImportDeckPopup.OnEnable()` | `Shell/ImportDeckPopup.cs:314` | 指向别的方法/字段 | 文件级＋被多方法共用(4) | — |
| `ImportDeckPopup.OnError(string error)` | `Shell/ImportDeckPopup.cs:399` | 语义近名指错(无注解) | 文件级＋被多方法共用(4) | — |
| `ImportDeckPopup.ValidateField(string value)` | `Shell/ImportDeckPopup.cs:367` | 指向别的方法/字段 | 仅类/描述(无方法) | — |
| `SelectDeckGameModeMenuContainer.OnClick()` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:4` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `SelectDecksTab.BackButtonClicked()` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:4` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `SelectDecksTab.ChooseTabToOpen()` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:3` | 语义近名指错(无注解) | 仅类.方法(可补行号) | — |
| `SelectDecksTab.ImportDeck()` | `Shell/CollectionWindow.cs:5358` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `SelectDecksTab.OnClickDeckCollection(IOwnDeckPlayEvent deckPlayEvent)` | `Shell/CollectionWindow.cs:5437` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(3) | — |
| `SelectDecksTab.ReturnFromImport(PlayModes? gameMode)` | `Shell/CollectionWindow.cs:5370` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `SelectDecksTab.Setup(GameWindow window)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:3` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `SelectDecksTab.ToggleSelectDeck(IOwnDeckPlayEvent deckPlayEvent)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:5` | 语义近名指错(无注解) | 文件级(可补行号)＋被多方法共用(3) | — |
| `SelectDecksTab.ToggleSelectGameModeDeck(IEnumerable<IOwnDeckPlayEvent> ga` | `Shell/CollectionWindow.cs:4577` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(3) | — |

#### 收藏.tsv（100 条）

| 类.方法 | 落点 | 现在的问题 | 基线问题 | 建议 |
|---|---|---|---|---|
| `AlternateArtCardCollectionTab.Awake()` | `Shell/CollectionWindow.cs:2318` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(5) | — |
| `AlternateArtCardCollectionTab.ChangeStyleButton(bool isLeft)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:2` | 语义近名指错(无注解) | 仅类.方法(可补行号) | — |
| `AlternateArtCardCollectionTab.ConfigureStyle(int indexStyleToUse, bool updateCollectionD` | `Shell/CollectionWindow.cs:2442` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `AlternateArtCardCollectionTab.GetCollection()` | `Shell/CollectionWindow.cs:2308` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `AlternateArtCardCollectionTab.OnItemSelected(CollectionItem<RawCardScript> item, Pointer` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:5` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `AlternateArtCardCollectionTab.ToggleShowOwned(bool show)` | `CardPresentation/Core/FilterPanelModel.cs:865` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(5) | — |
| `AlternateArtCardCollectionTab._003CAwake_003Eb__7_0()` | `Shell/CollectionWindow.cs:2318` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(5) | — |
| `AlternateArtCardCollectionTab._003CAwake_003Eb__7_1()` | `Shell/CollectionWindow.cs:2318` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(5) | — |
| `AlternateArtCardCollectionTab._003CGetCollection_003Eb__11_0(RawCardScript c)` | `Shell/CollectionWindow.cs:2308` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(5) | — |
| `CardArmyFilter.ClearArmyFilter()` | `CardPresentation/Core/FilterPanelModel.cs:912` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(6) | — |
| `CardArmyFilter.ClearFilters()` | `Shell/CollectionWindow.cs:2821` | 语义近名指错(无注解) | 文件级(可补行号)＋被多方法共用(6) | — |
| `CardArmyFilter.FillToggleList(List<CollectionFilterToggle> currentList, i` | `CardPresentation/Core/FilterPanelModel.cs:718` | 函数中间 | 文件级(可补行号)＋被多方法共用(6) | — |
| `CardArmyFilter.FilterItem(RawCardScript card)` | `CardPresentation/Deck/DeckEditorState.cs:97` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(6) | — |
| `CardArmyFilter.FilterToArmy(CardArmy army)` | `CardPresentation/Core/FilterPanelModel.cs:865` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(6) | — |
| `CardArmyFilter.SetFilterToDeck(CardDeck deck)` | `Shell/CollectionWindow.cs:2843` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(6) | — |
| `CardBackCosmeticArmyFilter.FilterItem(CosmeticItemCardback cosmetic)` | `Shell/CollectionWindow.cs:2068` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardCollectionDisplay.OnListDrag(Vector2 listPos)` | `Shell/CollectionWindow.cs:5098` | 指向别的方法/字段 | 仅类.方法(解析不到) | — |
| `CardCollectionDisplay.RefreshCells()` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:1` | 语义近名指错(无注解) | 仅类.方法(可补行号) | — |
| `CardCollectionDisplay.SetCell(ICell cell, int index)` | `Shell/CollectionWindow.cs:1510` | 语义近名指错(无注解) | 仅类.方法(可补行号) | — |
| `CardCollectionDisplay.UpdateCardVisuals(CollectionCard card)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:1` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardCollectionFilterController.SetFiltersToDeck(CardDeck deck)` | `Shell/CollectionWindow.cs:2843` | 指向别的方法/字段 | 文件级(可补行号) | — |
| `CardCollectionTab.GetCards()` | `Shell/CollectionWindow.cs:1729` | 语义近名指错(无注解) | 仅类.方法(可补行号) | — |
| `CardCollectionTab.GetCollection()` | `CardPresentation/Deck/DeckEditorState.cs:529` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardCollectionTab.OnItemSelected(CollectionItem<RawCardScript> item, Pointer` | `Shell/CollectionWindow.cs:5297` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardCollectionTab.Setup(GameWindow window)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:1` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardCollectionTab.ToggleShowOwned(bool show)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:2` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardCostFilter.FilterItem(RawCardScript card)` | `MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs:509` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardCostFilter.SetFilterToDeck(CardDeck deck)` | `MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs:509` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardNameFilter.CheckInputTextSearch(List<RawCardScript> collection, strin` | `MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs:621` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(8) | — |
| `CardNameFilter.ClearFilters()` | `MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs:621` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(8) | — |
| `CardNameFilter.FillToggleList(List<CollectionFilterToggle> currentList, i` | `MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs:621` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(8) | — |
| `CardNameFilter.Filter(List<RawCardScript> collection)` | `MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs:621` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(8) | `CardPresentation/Deck/DeckEditorState.cs:241` |
| `CardNameFilter.RemoveBetween(string sourceString, string startTag, string` | `MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs:621` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(8) | — |
| `CardNameFilter.SetupFilter(CollectionFilterController<RawCardScript> cont` | `MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs:621` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(8) | — |
| `CardNameFilter.TryMatchName(string query, string comparison)` | `MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs:621` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(8) | — |
| `CardNameFilter._003CSetupFilter_003Eb__1_0(string s)` | `MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs:621` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(8) | — |
| `CardRarityFilter.FillToggleList(List<CollectionFilterToggle> currentList, i` | `CardPresentation/Core/FilterPanelModel.cs:718` | 函数中间 | 仅类.方法(可补行号) | — |
| `CardRarityFilter.FilterItem(RawCardScript card)` | `CardPresentation/Deck/DeckEditorState.cs:108` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CardTypeFilter.ClearFilters()` | `Shell/CollectionWindow.cs:2821` | 语义近名指错(无注解) | 仅类.方法(可补行号)＋被多方法共用(6) | — |
| `CardTypeFilter.FillToggleList(List<CollectionFilterToggle> currentList, i` | `CardPresentation/Core/FilterPanelModel.cs:718` | 函数中间 | 仅类.方法(可补行号)＋被多方法共用(6) | — |
| `CardTypeFilter.FilterItem(RawCardScript card)` | `CardPresentation/Deck/DeckEditorState.cs:120` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(6) | — |
| `CardTypeFilter.SetFilterToDeck(CardDeck deck)` | `Shell/CollectionWindow.cs:2843` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(6) | — |
| `CardTypeFilter.ShowAll()` | `Shell/CollectionWindow.cs:2821` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(6) | — |
| `CardTypeFilter.ShowOnlyHero()` | `CardPresentation/Core/FilterPanelModel.cs:865` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(6) | — |
| `CardbackCollectionDisplay.SetCell(ICell cell, int index)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:1` | 语义近名指错(无注解) | 仅类.方法(可补行号) | — |
| `CardbackCollectionTab.GetCollection()` | `Shell/CollectionWindow.cs:2068` | 指向别的方法/字段 | 仅类/描述(无方法)＋被多方法共用(4) | — |
| `CardbackCollectionTab.OnItemSelected(CollectionItem<CosmeticItemCardback> item, ` | `Shell/CollectionWindow.cs:1874` | 指向别的方法/字段 | 仅类/描述(无方法)＋被多方法共用(4) | — |
| `CardbackCollectionTab.Setup(GameWindow window)` | `Shell/CollectionWindow.cs:1799` | 指向别的方法/字段 | 仅类/描述(无方法)＋被多方法共用(4) | — |
| `CardbackCollectionTab.ToggleShowOwned(bool show)` | `Shell/CollectionWindow.cs:2039` | 指向别的方法/字段 | 仅类/描述(无方法)＋被多方法共用(4) | — |
| `CollectionCard.AnimateIn()` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:1` | 语义近名指错(无注解) | 仅类.方法(可补行号)＋被多方法共用(6) | — |
| `CollectionCard.Config(RawCardScript item, GameplayVariablesData gameplayV` | `Shell/CollectionWindow.cs:1510` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(6) | — |
| `CollectionCard.HoverHighlight(bool state)` | `Shell/CollectionWindow.cs:5025` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(6) | — |
| `CollectionCard.OnRectTransformDimensionsChange()` | `Shell/CollectionWindow.cs:1510` | 指向别的方法/字段 | 文件级(可补行号) | — |
| `CollectionCard.SelectedHighlight(bool state)` | `Shell/CollectionWindow.cs:5171` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(6) | — |
| `CollectionCard.UpdateCardVisuals(bool isValid, bool isOwned, string count` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:1` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CollectionCardCounter.Set(string counterText, bool setGreyscale)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:3` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CollectionCosmetic.Config(CosmeticItemCardback item, GameplayVariablesData ga` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:1` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CollectionCosmetic.HoverHighlight(bool state)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:1` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CollectionFilter.ClearFilters()` | `Shell/CollectionWindow.cs:2821` | 语义近名指错(无注解) | 仅类.方法(可补行号)＋被多方法共用(6) | — |
| `CollectionFilter.FillToggleList(List<CollectionFilterToggle> currentList, i` | `CardPresentation/Core/FilterPanelModel.cs:718` | 函数中间 | 仅类.方法(可补行号)＋被多方法共用(6) | — |
| `CollectionFilter.Filter(List<T> collection)` | `CardPresentation/Deck/DeckEditorState.cs:529` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(6) | `CardPresentation/Deck/DeckEditorState.cs:241` |
| `CollectionFilter.NotifyChange()` | `Shell/CollectionWindow.cs:2881` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(6) | — |
| `CollectionFilter.OnFilterChange()` | `Shell/CollectionWindow.cs:2881` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(6) | — |
| `CollectionFilter.SetupFilter(CollectionFilterController<T> controller)` | `Shell/CollectionWindow.cs:2745` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(6) | — |
| `CollectionFilterController.Filter(List<T> collection)` | `MyGame/Assets/CardPresentation/Deck/DeckEditorState.cs:529` | 指向别的方法/字段 | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckEditorState.cs:241` |
| `CollectionFilterController.NotifyFilterChange()` | `Shell/CollectionWindow.cs:2881` | 指向别的方法/字段 | 文件级 | — |
| `CollectionFilterToggle.ForceOff()` | `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs:7504` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(4) | — |
| `CollectionFilterToggle.HideLabel()` | `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs:7504` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(4) | — |
| `CollectionFilterToggle.Initialize(FilterOptions option)` | `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs:7504` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(4) | — |
| `CollectionFilterToggle.SetValue(bool value, bool notify)` | `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs:7504` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(4) | — |
| `CollectionItem.AnimateIn()` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:1` | 语义近名指错(无注解) | 仅类.方法(可补行号) | — |
| `CollectionItem.Config(T item, CollectionDisplay<T> display, bool animate,` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:1` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CollectionItem.Config(T item, GameplayVariablesData gameplayVariablesData` | `Shell/CollectionWindow.cs:1510` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CollectionItem.HoverHighlight(bool state)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:5` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CollectionItem.OnPointerClick(PointerEventData eventData)` | `MyGame/Assets/CardPresentation/Shell/WindowButton.cs:897` | 语义近名指错(无注解) | 仅类.方法(可补行号) | — |
| `CollectionItem.OnPointerEnter(PointerEventData eventData)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:5` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CollectionItem.OnPointerExit(PointerEventData eventData)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:5` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CollectionItem.SelectedHighlight(bool state)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:5` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |
| `CollectionScreen.BackButtonClick()` | `Shell/CollectionWindow.cs:4632` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(5) | — |
| `CollectionScreen.OnDisable()` | `Shell/MainMenuRuntime.cs:1021` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(5) | — |
| `CollectionScreen.OnEnable()` | `Shell/MainMenuRuntime.cs:1021` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(5) | — |
| `CollectionScreen.ReturnFromImportDeck(PlayModes? gameMode)` | `Shell/CollectionWindow.cs:5358` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(5) | — |
| `CollectionTab.GetCollection()` | `Shell/CollectionTabPage.cs:56` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(5) | — |
| `CollectionTab.GetItems()` | `Shell/CollectionTabPage.cs:44` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(5) | — |
| `CollectionTab.OnItemSelected(CollectionItem<T> item, PointerEventData.In` | `Shell/CollectionWindow.cs:5297` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(5) | — |
| `GameModesIconCollection.GetIcon(PlayModes? playMode)` | `MyGame/Assets/CardPresentation/Shell/MenuDraw.cs:3180` | 语义近名指错(无注解) | 仅类.方法(可补行号)＋被多方法共用(5) | — |
| `GameModesIconCollection.Release()` | `MyGame/Assets/CardPresentation/Shell/MenuDraw.cs:3180` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(5) | — |
| `GameModesIconCollection.Release(PlayModes? playMode)` | `MyGame/Assets/CardPresentation/Shell/MenuDraw.cs:3180` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(5) | — |
| `GameModesIconCollection.ReleaseAll()` | `MyGame/Assets/CardPresentation/Shell/MenuDraw.cs:3180` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(5) | — |
| `GameModesIconCollection.TryGetIcon(PlayModes playModeToCheck, out Sprite iconToRet` | `MyGame/Assets/CardPresentation/Shell/MenuDraw.cs:3180` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(5) | — |
| `GameModesIconCollection.TryRelease(PlayModes playModeToCheck)` | `MyGame/Assets/CardPresentation/Shell/MenuDraw.cs:3180` | 指向别的方法/字段 | 仅类.方法(可补行号)＋被多方法共用(5) | — |
| `Inventory.Get(Func<T, bool> predicate)` | `Shell/Inventory.cs:200` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(11) | `CardPresentation/Shell/Inventory.cs:142` |
| `ToggleCollectionFilter.FillToggleList(List<CollectionFilterToggle> currentList, i` | `CardPresentation/Core/FilterPanelModel.cs:718` | 函数中间 | 文件级(可补行号) | — |
| `ToggleCollectionFilter.Filter(List<T> collection)` | `MyGame/Assets/CardPresentation/Core/FilterPanelModel.cs:86` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(3) | — |
| `ToggleCollectionFilter.FilterItem(T card)` | `MyGame/Assets/CardPresentation/Core/FilterPanelModel.cs:86` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(3) | — |
| `ToggleCollectionFilter.OnFilterChange()` | `MyGame/Assets/CardPresentation/Core/FilterPanelModel.cs:86` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(3) | — |
| `WhereToGetCardsSection.Awake()` | `Shell/WhereToGetCardsPopup.cs:288` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(6) | — |
| `WhereToGetCardsSection.Initialize(WhereToGetExpansionCardsData.ExpansionCardsData` | `Shell/WhereToGetCardsPopup.cs:470` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(6) | — |
| `WhereToGetCardsSection.SetVisibility(bool option)` | `Shell/WhereToGetCardsPopup.cs:597` | 指向别的方法/字段 | 注释＋被多方法共用(6) | — |
| `WildcardDisplay.Initialize(CardArmy army)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:5` | 指向别的方法/字段 | 仅类.方法(可补行号) | — |

#### 商店.tsv（81 条）

| 类.方法 | 落点 | 现在的问题 | 基线问题 | 建议 |
|---|---|---|---|---|
| `BaseOfferPopup.OnDisable()` | `Shell/BaseOfferPopup.cs:679` | 指向别的方法/字段 | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup.OnEnable()` | `Shell/BaseOfferPopup.cs:668` | 指向别的方法/字段 | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup.OnOfferTimerEnded()` | `Shell/BaseOfferPopup.cs:957` | 语义近名指错(无注解) | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup.OverrideVisualRewards(List<RewardInfo> rewards)` | `Shell/BaseOfferPopup.cs:1111` | 指向别的方法/字段 | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup.Refresh()` | `Shell/BaseOfferPopup.cs:725` | 指向别的方法/字段 | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup.SetAvailableText(bool hasLimit, int purchasedCount)` | `Shell/ShopData.cs:397` | 指向别的方法/字段 | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup.SetBadgeText()` | `Shell/BaseOfferPopup.cs:924` | 语义近名指错(无注解) | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup.SetEventData(IShopOffer liveOps)` | `Shell/BaseOfferPopup.cs:707` | 指向别的方法/字段 | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup.SetTimer()` | `Shell/BaseOfferPopup.cs:957` | 语义近名指错(无注解) | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup.ToFocus()` | `Shell/BaseOfferPopup.cs:668` | 指向别的方法/字段 | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup._003CProcessPurchase_003Eg__Callback_007C29_0()` | `MyGame/Assets/CardPresentation/Shell/BaseOfferPopup.cs:105` | 语义近名指错(无注解) | 文件级(可补行号)＋被多方法共用(14) | — |
| `CardShopTab.CheckTimer(out DateTime timer)` | `Shell/ShopTabPage.cs:290` | 指向别的方法/字段 | 文件级 | — |
| `ContainerOfferEvent.OpenOfferContainer(string instanceId, Action<IReadOnlyList` | `Shell/ShopTabPage.cs:827` | 指向别的方法/字段 | 文件级 | — |
| `CosmeticsShopTab.CheckTimer(out DateTime timer)` | `Shell/ShopTabPage.cs:290` | 指向别的方法/字段 | 文件级 | — |
| `GeneralOfferPopupDrawer.DoReset()` | `Shell/ItemDrawer.cs:840` | 指向别的方法/字段 | 文件级＋被多方法共用(3) | — |
| `GeneralOfferPopupDrawer.DrawItemReward(ItemDrawer itemDrawer, ObtainableItem itemT` | `MyGame/Assets/CardPresentation/Shell/ItemDrawer.cs:840` | 指向别的方法/字段 | 文件级＋被多方法共用(3) | — |
| `GeneralOfferPopupDrawer.DrawRewards(IEnumerable<RewardInfo> rewards)` | `MyGame/Assets/CardPresentation/Shell/ItemDrawer.cs:840` | 指向别的方法/字段 | 文件级＋被多方法共用(3) | — |
| `GeneralOfferPopupDrawerBase.DoReset()` | `Shell/BaseOfferPopup.cs:725` | 指向别的方法/字段 | 文件级＋被多方法共用(4) | — |
| `GeneralOfferPopupDrawerBase.DrawRewards(IEnumerable<RewardInfo> rewards)` | `Shell/BaseOfferPopup.cs:1111` | 指向别的方法/字段 | 文件级＋被多方法共用(4) | — |
| `GeneralOfferPopupDrawerBase.Initialize(ShopOfferBase shopOfferData)` | `Shell/BaseOfferPopup.cs:707` | 指向别的方法/字段 | 文件级＋被多方法共用(4) | — |
| `InAppOfferContainer.OnInitialize(IShopOffer offer)` | `MyGame/Assets/CardPresentation/Shell/BaseOfferPopup.cs:679` | 指向别的方法/字段 | 文件级 | — |
| `ItemShopTab.CheckTimer(out DateTime timer)` | `Shell/ShopTabPage.cs:290` | 指向别的方法/字段 | 文件级 | — |
| `ItemShopTab.DoOfferReordering(ref IReadOnlyList<IShopOffer> offers)` | `Shell/OfferContainer.cs:951` | 指向别的方法/字段 | 文件级 | — |
| `PreviewOfferButton.Initialize(IShopOffer offer)` | `Shell/ShopTabPage.cs:484` | 指向别的方法/字段 | 文件级 | — |
| `PriceDisplay.TryInsertStrikeThrough()` | `Shell/ShopTabPage.cs:484` | 指向别的方法/字段 | 文件级＋被多方法共用(4) | — |
| `PuchasePremiumArmyContainer.Click(bool enabledOption)` | `Shell/PurchasePremiumWindow.cs:446` | 指向别的方法/字段 | 文件级＋被多方法共用(5) | — |
| `PuchasePremiumArmyContainer.OnDisable()` | `Shell/PurchasePremiumWindow.cs:459` | 指向别的方法/字段 | 文件级＋被多方法共用(5) | — |
| `PuchasePremiumArmyContainer.OnEnable()` | `Shell/PurchasePremiumWindow.cs:459` | 指向别的方法/字段 | 文件级＋被多方法共用(5) | — |
| `PuchasePremiumArmyContainer.TryHighlight(CardArmy cardArmy)` | `Shell/PurchasePremiumWindow.cs:418` | 指向别的方法/字段 | 文件级＋被多方法共用(5) | — |
| `PuchasePremiumArmyInfo.OnDisable()` | `Shell/PurchasePremiumWindow.cs:459` | 指向别的方法/字段 | 文件级＋被多方法共用(4) | — |
| `PuchasePremiumArmyInfo.OnEnable()` | `Shell/PurchasePremiumWindow.cs:459` | 指向别的方法/字段 | 文件级＋被多方法共用(4) | — |
| `PurchasePremiumWindow.ClearCurrentArmyContainers()` | `Shell/PurchasePremiumWindow.cs:408` | 语义近名指错(无注解) | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow.CloseButtonClick()` | `Shell/GameWindow.cs:714` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow.FetchData()` | `Shell/PurchasePremiumWindow.cs:384` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow.OnDisable()` | `Shell/GameWindow.cs:714` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow.OnEnable()` | `Shell/PurchasePremiumWindow.cs:360` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow.OnPurChaseCompleted(string _)` | `Shell/PurchasePremiumWindow.cs:924` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow.OnPurchasedFailed(string purchaseID)` | `Shell/PurchasePremiumWindow.cs:924` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow.TryOpen(object data, GameWindowOptions options)` | `MyGame/Assets/CardPresentation/Shell/PurchasePremiumWindow` | 语义近名指错(无注解) | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow.TryPurchase(Action<string> onPurchase, Action<string> onFa` | `Shell/ShopData.cs:462` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow.TryPurchase(PremiumArmyPurchaseData purchaseArmyData)` | `Shell/ShopData.cs:462` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow._003COnEnable_003Eb__11_0()` | `Shell/PurchasePremiumWindow.cs:360` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `ShopItemContainer.Initialize(object payload)` | `Shell/ShopTabPage.cs:484` | 指向别的方法/字段 | 文件级＋被多方法共用(8) | — |
| `ShopItemContainer.OnDisable()` | `Shell/OfferContainer.cs:1009` | 指向别的方法/字段 | 文件级＋被多方法共用(8) | — |
| `ShopItemContainer.OnEnable()` | `Shell/OfferContainer.cs:1009` | 指向别的方法/字段 | 文件级＋被多方法共用(8) | — |
| `ShopItemContainer.OnPurchase(bool success)` | `Shell/ShopTabPage.cs:764` | 指向别的方法/字段 | 文件级＋被多方法共用(8) | — |
| `ShopItemContainer.SetRefreshCallback(Action callback)` | `Shell/ShopWindow.cs:254` | 指向别的方法/字段 | 文件级＋被多方法共用(8) | — |
| `ShopItemContainer.TryPurchase()` | `Shell/ShopData.cs:462` | 指向别的方法/字段 | 文件级＋被多方法共用(8) | — |
| `ShopItemContainer._003CTryPurchase_003Eb__2_0()` | `Shell/ShopTabPage.cs:753` | 指向别的方法/字段 | 文件级＋被多方法共用(8) | — |
| `ShopItemContainer._003CTryPurchase_003Eb__2_1()` | `Shell/ShopTabPage.cs:753` | 指向别的方法/字段 | 文件级＋被多方法共用(8) | — |
| `ShopOfferContainer.CallRefreshCallback()` | `Shell/ShopTabPage.cs:764` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.OnClick()` | `Shell/ShopTabPage.cs:714` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.OnInitialize(IShopOffer shopItem)` | `Shell/ShopTabPage.cs:484` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.OnOfferTimerEnded()` | `Shell/ShopTabPage.cs:290` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.OnPurchase(bool success)` | `Shell/ShopTabPage.cs:764` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.OpenContainer()` | `Shell/ShopTabPage.cs:827` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.Refresh()` | `Shell/ShopTabPage.cs:456` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.RefreshExpire()` | `Shell/ShopTabPage.cs:290` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.SetAvailableText(bool hasLimit, int purchasedCount)` | `Shell/ShopData.cs:397` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.SetBadgeText()` | `Shell/ShopData.cs:386` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.SetRefreshCallback(Action callback)` | `Shell/ShopWindow.cs:254` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer._003CRefreshExpire_003Eb__15_0()` | `Shell/ShopTabPage.cs:290` | 指向别的方法/字段 | 文件级＋被多方法共用(13) | — |
| `ShopStore.GetBaseData()` | `Shell/ShopData.cs:238` | 指向别的方法/字段 | 文件级＋被多方法共用(6) | — |
| `ShopStore.GetBaseSave()` | `Shell/ShopData.cs:349` | 指向别的方法/字段 | 文件级＋被多方法共用(6) | — |
| `ShopStore.GetPurchaseCount(string id)` | `Shell/ShopData.cs:386` | 指向别的方法/字段 | 文件级＋被多方法共用(6) | — |
| `ShopStore.Initialize()` | `Shell/ShopData.cs:316` | 指向别的方法/字段 | 文件级＋被多方法共用(6) | — |
| `ShopStore.RemoveFinishedOffer()` | `Shell/ShopData.cs:238` | 指向别的方法/字段 | 文件级＋被多方法共用(6) | — |
| `ShopStore.TryPurchase(IShopOffer offer, Action onComplete, Action on` | `Shell/ShopData.cs:462` | 指向别的方法/字段 | 文件级＋被多方法共用(6) | — |
| `ShopTab.CheckTimer(out DateTime timer)` | `Shell/ShopTabPage.cs:290` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(10) | — |
| `ShopTab.CreateOffers()` | `Shell/ShopTabPage.cs:399` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(10) | — |
| `ShopTab.DoOfferReordering(ref IReadOnlyList<IShopOffer> offers)` | `Shell/OfferContainer.cs:951` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(10) | — |
| `ShopTab.OnOpen()` | `Shell/ShopTabPage.cs:238` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(10) | — |
| `ShopTab.RefreshAfterExpire()` | `Shell/ShopTabPage.cs:456` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(10) | — |
| `ShopTab.RefreshOffers()` | `Shell/ShopTabPage.cs:456` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(10) | — |
| `ShopTab.SetEventTab(ILiveOps liveOps)` | `Shell/ShopTabPage.cs:62` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(10) | — |
| `ShopTab.SetStore()` | `Shell/ShopData.cs:238` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(10) | — |
| `ShopTab.ToFocus()` | `Shell/ShopWindow.cs:254` | 指向别的方法/字段 | 文件级(可补行号)＋被多方法共用(10) | — |
| `ShopWindow.CreateStoreTab(IStore store)` | `MyGame/Assets/CardPresentation/Shell/ShopWindow.cs:118` | 指向别的方法/字段 | 文件级＋被多方法共用(6) | — |
| `ShopWindow.GetStartingTab()` | `Shell/ShopWindow.cs:171` | 指向别的方法/字段 | 文件级＋被多方法共用(6) | — |
| `ShopWindow.OnShopRefresh()` | `Shell/ShopWindow.cs:254` | 指向别的方法/字段 | 文件级＋被多方法共用(6) | — |
| `ShopWindow.SetupTabs()` | `MyGame/Assets/CardPresentation/Shell/ShopTabPage.cs:238` | 指向别的方法/字段 | 文件级＋被多方法共用(6) | — |

#### 轮抽.tsv（9 条）

| 类.方法 | 落点 | 现在的问题 | 基线问题 | 建议 |
|---|---|---|---|---|
| `DraftEventBase.SelectEvent()` | `CardPresentation/Shell/DraftModeWindow.cs:994（`Open`）` | 指向别的方法/字段 | 仅类/描述(无方法) | — |
| `DraftModeMenuDemo.Awake()` | `CardPresentation/Shell/DraftModeWindow.cs:1121（`Build`：窗内各` | 指向别的方法/字段 | 仅类/描述(无方法) | — |
| `DraftModeMenuDemo.CloseButtonOnClick()` | `CardPresentation/Shell/DraftModeWindow.cs:1024（`Close`）` | 指向别的方法/字段 | 仅类/描述(无方法) | — |
| `DraftModeMenuDemo.OnDestroy()` | `CardPresentation/Shell/DraftModeWindow.cs:1024（`Close`：窗销毁` | 指向别的方法/字段 | 仅类/描述(无方法) | — |
| `DraftModeMenuPayStateDemo.OnClose()` | `CardPresentation/Shell/DraftModeWindow.cs:1024（`Close`）` | 语义近名指错(无注解) | 仅类/描述(无方法) | — |
| `DraftModeMenuPayStateDemo.OnDisable()` | `CardPresentation/Shell/DraftModeWindow.cs:1024（`Close`）` | 指向别的方法/字段 | 间接指认 | — |
| `DraftModeSelectPacksStateDemo.OnDestroy()` | `CardPresentation/Shell/DraftModeWindow.cs:1024（`Close`）` | 指向别的方法/字段 | 仅类/描述(无方法) | — |
| `DraftModeWarlordSelectionStateDemo.OnDestroy()` | `CardPresentation/Shell/DraftModeWindow.cs:1024（`Close`）` | 指向别的方法/字段 | 仅类/描述(无方法) | — |
| `DraftRunDisplay.OnDestroy()` | `CardPresentation/Shell/DraftModeWindow.cs:1024（`Close`）` | 指向别的方法/字段 | 仅类/描述(无方法) | — |

#### 战役.tsv（2 条）

| 类.方法 | 落点 | 现在的问题 | 基线问题 | 建议 |
|---|---|---|---|---|
| `CampaignRewardsWindow.ESCPressed()` | `CardPresentation/Shell/CampaignRewardWindow.cs:318（`Create` | 指向别的方法/字段 | 仅类/描述(无方法) | — |
| `CampaignWindowTab.OnSetup()` | `CardPresentation/Shell/CampaignTab.cs:121（`Build`）` | 语义近名指错(无注解) | 仅类/描述(无方法) | — |

#### 锻造.tsv（7 条）

| 类.方法 | 落点 | 现在的问题 | 基线问题 | 建议 |
|---|---|---|---|---|
| `BoosterPackOpenWindow.BoosterPackOpen()` | `Shell/BoosterPackOpenWindow.cs:397 Open / :413 Show / :787` | 语义近名指错(无注解) | 指向别的方法/字段 | — |
| `ForgeRewardItemContainer.ConfigurePoints(int currentPoints, int previousNeededPoint` | `Shell/ForgeTab.cs:918 PointsText + ForgeData.Bar01` | 函数中间 | 函数中间 | — |
| `ForgeRewardSelector.Initialize(Forge forgeEvent)` | `CardPresentation/Shell/ForgeTab.cs:764（Refresh 重建）` | 指向别的方法/字段 | 指向别的方法/字段 | — |
| `ForgeRewardSelector.RewardCollected(RewardInfo[] rewards)` | `CardPresentation/Shell/ForgeTab.cs:764（Refresh 重建）` | 指向别的方法/字段 | 文件级 | — |
| `ForgeSelectedArmyInfo.RefreshLevel(int currentLevel, int maxLevel)` | `CardPresentation/Shell/ForgeTab.cs:764（Refresh 写 LevelText` | 指向别的方法/字段 | 函数中间 | — |
| `ForgeWindowTab.Awake()` | `CardPresentation/Shell/ForgeTab.cs:278（Setup → Build）` | 指向别的方法/字段 | 指向别的方法/字段 | — |
| `ForgeWindowTab.RefreshLevel()` | `CardPresentation/Shell/ForgeTab.cs:764（Refresh 写 LevelText` | 指向别的方法/字段 | 函数中间 | — |

#### 其他_基础设施.tsv（0 条）

✅ 基线里的问题已全部处理。

#### 未分类_其他.tsv（0 条）

✅ 基线里的问题已全部处理。

#### 泛型与启动链_补读.tsv（0 条）

✅ 基线里的问题已全部处理。

#### 社交_聊天.tsv（0 条）

✅ 基线里的问题已全部处理。

#### 个人资料_成就_排行.tsv（0 条）

✅ 基线里的问题已全部处理。

#### 教程_引导.tsv（0 条）

✅ 基线里的问题已全部处理。

### b. 语义近名指错（**无注解 = 硬伤**，机检抓不到）

| 账 | 类.方法 | 落点 | 那一行声明的其实是 | 相似关系 |
|---|---|---|---|---|
| `战斗逻辑驱动.tsv` | `BattleManager.ResolveDrawCard(BattleAction battleAction)` | `RuleEngine/Core/RuleCore.cs:2812（Draw） ｜ ` ｜ RuleCore.Dr` | `Draw` | ResolveDrawCard vs Draw（词干互含） |
| `菜单_外壳.tsv` | `DraggableController._003COnEndDrag_003Eb__6_2(IDropHandler<T> drop` | `Core/DraggableController.cs:202 的循环体（判空位置照原版在循环内）` | `OnEndDrag` | _003COnEndDrag_003Eb__6_2 vs OnEndDrag（词干互含） |
| `卡组编辑.tsv` | `CardDeck.CanAddCard(RawCardScript card, out string erro` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:694` | `AddCard` | CanAddCard vs AddCard（词干互含） |
| `卡组编辑.tsv` | `CardDeck.GetDeckCardback()` | `MyGame/Assets/CardPresentation/Core/CardArt.cs:983` | `DeckCardback` | GetDeckCardback vs DeckCardback（同词干） |
| `卡组编辑.tsv` | `CardDeck._003CGetDeckCardback_003Eb__56_0(CosmeticItem ` | `MyGame/Assets/CardPresentation/Core/CardArt.cs:983` | `DeckCardback` | _003CGetDeckCardback_003Eb__56_0 vs DeckCardback（词干互含） |
| `卡组编辑.tsv` | `DeckCollectionTab.OnOpen()` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs` | `Open` | OnOpen vs Open（同词干） |
| `卡组编辑.tsv` | `DeckEditingPanel.AddCard(RawCardScript card, bool withAnim)` | `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs:8246` | `TryAddCard` | AddCard vs TryAddCard（词干互含） |
| `卡组编辑.tsv` | `DeckEditingWindow.CheckCardDrag(PointerEventData eventData, Game` | `Deck/DeckRuntime.cs:4058` | `BeginCardDragAt` | CheckCardDrag vs BeginCardDragAt（词干互含） |
| `卡组编辑.tsv` | `DeckEditingWindow.CheckCosmeticDrag(PointerEventData eventData, ` | `Deck/DeckRuntime.cs:4028` | `BeginCosmeticDragAt` | CheckCosmeticDrag vs BeginCosmeticDragAt（词干互含） |
| `卡组编辑.tsv` | `DeckEditingWindow.OnCosmeticClick(CollectionItem<CosmeticItemCar` | `Deck/DeckRuntime.cs:3689` | `HandleCosmeticClick` | OnCosmeticClick vs HandleCosmeticClick（词干互含） |
| `卡组编辑.tsv` | `DeckEditingWindow.TryOpen(object data, GameWindowOptions options` | `MyGame/Assets/CardPresentation/Shell/DeckEditingWindow.c` | `Open` | TryOpen vs Open（同词干） |
| `卡组编辑.tsv` | `DeckEditorCollectionDisplay.SetCell(ICell cell, int index)` | `Deck/DeckRuntime.cs:3225` | `PoolCellAt` | SetCell vs PoolCellAt（词干互含） |
| `卡组编辑.tsv` | `DeckInfoPopup.DeleteDeck()` | `MyGame/Assets/CardPresentation/Shell/DeckInfoPopup.cs:24` | `DeleteDeckNow` | DeleteDeck vs DeleteDeckNow（词干互含） |
| `卡组编辑.tsv` | `DeckInfoPopup.ShareDeckOnChat(Transform optionsMountPoint)` | `MyGame/Assets/CardPresentation/Shell/DeckInfoPopup.cs:25` | `ShareDeck` | ShareDeckOnChat vs ShareDeck（词干互含） |
| `卡组编辑.tsv` | `DeckListDrawer.SetCount()` | `Deck/DeckRuntime.cs:4281` | `DeckRowCardCount` | SetCount vs DeckRowCardCount（词干互含） |
| `卡组编辑.tsv` | `DeckNameFilter.Filter(List<CardDeck> collection)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs` | `ApplyFilter` | Filter vs ApplyFilter（同词干） |
| `卡组编辑.tsv` | `DeckSelectionPopup.TryOpen(object data, GameWindowOptions options` | `MyGame/Assets/CardPresentation/Shell/DeckSelectionPopup.` | `Open` | TryOpen vs Open（同词干） |
| `卡组编辑.tsv` | `DeckSelectionPopup._003CTryOpen_003Eb__10_0(CardDeck d)` | `MyGame/Assets/CardPresentation/Shell/DeckSelectionPopup.` | `Open` | _003CTryOpen_003Eb__10_0 vs Open（词干互含） |
| `卡组编辑.tsv` | `DeckUtility.RemoveCardFromDeck(RawCardScript card, CardDec` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:701` | `RemoveCard` | RemoveCardFromDeck vs RemoveCard（词干互含） |
| `卡组编辑.tsv` | `DeckUtility.TryRemoveCardsNotOwned(CardDeck deck, List<Raw` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:701` | `RemoveCard` | TryRemoveCardsNotOwned vs RemoveCard（词干互含） |
| `卡组编辑.tsv` | `DeckUtility.TryRemoveInvalidCards(CardDeck deck, List<RawC` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:400` | `RemoveInvalidCards` | TryRemoveInvalidCards vs RemoveInvalidCards（词干互含） |
| `卡组编辑.tsv` | `DeckUtility.ValidateDeck(CardDeck deck, out DeckError erro` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:443` | `Validate` | ValidateDeck vs Validate（词干互含） |
| `卡组编辑.tsv` | `DeckUtility.ValidateDeckCards(CardDeck deck, Dictionary<Ra` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:443` | `Validate` | ValidateDeckCards vs Validate（词干互含） |
| `卡组编辑.tsv` | `DeckUtility.ValidateDeckOwnership(CardDeck deck, Dictionar` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:443` | `Validate` | ValidateDeckOwnership vs Validate（词干互含） |
| `卡组编辑.tsv` | `DeckUtility.ValidateDeckWarlord(CardDeck deck, out DeckErr` | `MyGame/Assets/RuleEngine/Core/DeckRules.cs:443` | `Validate` | ValidateDeckWarlord vs Validate（词干互含） |
| `卡组编辑.tsv` | `ImportDeckPopup.OnError(string error)` | `Shell/ImportDeckPopup.cs:399` | `RebuildErrorLine` | OnError vs RebuildErrorLine（词干互含） |
| `卡组编辑.tsv` | `SelectDecksTab.ChooseTabToOpen()` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs` | `Open` | ChooseTabToOpen vs Open（词干互含） |
| `卡组编辑.tsv` | `SelectDecksTab.ToggleSelectDeck(IOwnDeckPlayEvent deckPlayEve` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs` | `SelectDeck` | ToggleSelectDeck vs SelectDeck（同词干） |
| `收藏.tsv` | `AlternateArtCardCollectionTab.ChangeStyleButton(bool isLeft)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs` | `ChangeStyle` | ChangeStyleButton vs ChangeStyle（词干互含） |
| `收藏.tsv` | `CardArmyFilter.ClearFilters()` | `Shell/CollectionWindow.cs:2821` | `ClearFiltersNow` | ClearFilters vs ClearFiltersNow（词干互含） |
| `收藏.tsv` | `CardCollectionDisplay.RefreshCells()` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs` | `RebuildCardsCells` | RefreshCells vs RebuildCardsCells（词干互含） |
| `收藏.tsv` | `CardCollectionDisplay.SetCell(ICell cell, int index)` | `Shell/CollectionWindow.cs:1510` | `RebuildCardsCells` | SetCell vs RebuildCardsCells（词干互含） |
| `收藏.tsv` | `CardCollectionTab.GetCards()` | `Shell/CollectionWindow.cs:1729` | `CardsVisibleCount` | GetCards vs CardsVisibleCount（词干互含） |
| `收藏.tsv` | `CardTypeFilter.ClearFilters()` | `Shell/CollectionWindow.cs:2821` | `ClearFiltersNow` | ClearFilters vs ClearFiltersNow（词干互含） |
| `收藏.tsv` | `CardbackCollectionDisplay.SetCell(ICell cell, int index)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs` | `RebuildCosmoCells` | SetCell vs RebuildCosmoCells（词干互含） |
| `收藏.tsv` | `CollectionCard.AnimateIn()` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs` | `BuildCardAnimateInTween` | AnimateIn vs BuildCardAnimateInTween（词干互含） |
| `收藏.tsv` | `CollectionFilter.ClearFilters()` | `Shell/CollectionWindow.cs:2821` | `ClearFiltersNow` | ClearFilters vs ClearFiltersNow（词干互含） |
| `收藏.tsv` | `CollectionItem.AnimateIn()` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs` | `BuildCardAnimateInTween` | AnimateIn vs BuildCardAnimateInTween（词干互含） |
| `收藏.tsv` | `CollectionItem.OnPointerClick(PointerEventData eventData)` | `MyGame/Assets/CardPresentation/Shell/WindowButton.cs:897` | `Click` | OnPointerClick vs Click（词干互含） |
| `收藏.tsv` | `GameModesIconCollection.GetIcon(PlayModes? playMode)` | `MyGame/Assets/CardPresentation/Shell/MenuDraw.cs:3180` | `GameModeIconFile` | GetIcon vs GameModeIconFile（词干互含） |
| `商店.tsv` | `BaseOfferPopup.OnOfferTimerEnded()` | `Shell/BaseOfferPopup.cs:957` | `BuildTimer` | OnOfferTimerEnded vs BuildTimer（词干互含） |
| `商店.tsv` | `BaseOfferPopup.SetBadgeText()` | `Shell/BaseOfferPopup.cs:924` | `BuildBadge` | SetBadgeText vs BuildBadge（词干互含） |
| `商店.tsv` | `BaseOfferPopup.SetTimer()` | `Shell/BaseOfferPopup.cs:957` | `BuildTimer` | SetTimer vs BuildTimer（同词干） |
| `商店.tsv` | `BaseOfferPopup._003CProcessPurchase_003Eg__Callback_007C29_0(` | `MyGame/Assets/CardPresentation/Shell/BaseOfferPopup.cs:1` | `ProcessPurchase` | _003CProcessPurchase_003Eg__Callback_007C29_0 vs ProcessPurchase（词干互含） |
| `商店.tsv` | `PurchasePremiumWindow.ClearCurrentArmyContainers()` | `Shell/PurchasePremiumWindow.cs:408` | `ClearArmyContainers` | ClearCurrentArmyContainers vs ClearArmyContainers（词干互含） |
| `商店.tsv` | `PurchasePremiumWindow.TryOpen(object data, GameWindowOptions options` | `MyGame/Assets/CardPresentation/Shell/PurchasePremiumWind` | `Open` | TryOpen vs Open（同词干） |
| `轮抽.tsv` | `DraftModeMenuPayStateDemo.OnClose()` | `CardPresentation/Shell/DraftModeWindow.cs:1024（`Close`）` | `Close` | OnClose vs Close（同词干） |
| `战役.tsv` | `CampaignWindowTab.OnSetup()` | `CardPresentation/Shell/CampaignTab.cs:121（`Build`）` | `Setup` | OnSetup vs Setup（同词干） |
| `锻造.tsv` | `BoosterPackOpenWindow.BoosterPackOpen()` | `Shell/BoosterPackOpenWindow.cs:397 Open / :413 Show / :7` | `Open` | BoosterPackOpen vs Open（词干互含） |

### c. 语义近名（**有注解** ⇒ 注解本身可能就是错的，待 owner 确认）

| 账 | 类.方法 | 落点（注解名） | 那一行声明的是 | 相似关系 |
|---|---|---|---|---|
| `战斗逻辑驱动.tsv` | `BattleManager.CheckIfAttackKillsTarget(CardScript attacker, ` | `RuleEngine/Core/RuleCore.cs:5429（AttackKillsTarget） ｜ ` ` | `AttackKillsTarget` | CheckIfAttackKillsTarget vs AttackKillsTarget（词干互含） |
| `战斗逻辑驱动.tsv` | `BattleManager.GetWinnerAfterBattleEnd(bool isPlayerDying, Ba` | `RuleEngine/Core/RuleCore.cs:13304（CheckWinner） ｜ `RuleEn` | `CheckWinner` | GetWinnerAfterBattleEnd vs CheckWinner（词干互含） |
| `战斗逻辑驱动.tsv` | `BattleManager.IsDuringMulligan()` | `RuleEngine/Core/RuleCore.cs:440（Mulligan） ｜ `RuleEngine/` | `Mulligan` | IsDuringMulligan vs Mulligan（词干互含） |
| `战斗逻辑驱动.tsv` | `CardScript.CanUseAbilityNow()` | `RuleEngine/Core/RuleCore.cs:10659（CanUseAbility） ｜ ` ｜ R` | `CanUseAbility` | CanUseAbilityNow vs CanUseAbility（词干互含） |
| `战斗逻辑驱动.tsv` | `CardScript.IsInPlay()` | `RuleEngine/Core/RuleCore.cs:10302（IsUnitInPlay） ｜ ` ｜ Ru` | `IsUnitInPlay` | IsInPlay vs IsUnitInPlay（词干互含） |
| `战斗逻辑驱动.tsv` | `CardScript.IsInPlayOrDying()` | `RuleEngine/Core/RuleCore.cs:10326（IsUnitInPlayOrDying） ｜` | `IsUnitInPlayOrDying` | IsInPlayOrDying vs IsUnitInPlayOrDying（词干互含） |
| `战斗逻辑驱动.tsv` | `CardScript.RemoveDropPod(bool isDestroyed)` | `RuleEngine/Core/RuleCore.cs:7521（OpenDropPod） ｜ ` ｜ Rule` | `OpenDropPod` | RemoveDropPod vs OpenDropPod（同词干） |
| `战斗逻辑驱动.tsv` | `CardScript.TriggerSpentAllMana(CardScript actingCard)` | `RuleEngine/Core/RuleCore.cs:12566（FireSpentAllMana） ｜ ` ` | `FireSpentAllMana` | TriggerSpentAllMana vs FireSpentAllMana（词干互含） |
| `战斗逻辑驱动.tsv` | `MinionManager.InsertMinion(CardScript card, int slotNum)` | `RuleEngine/Core/BoardSlots.cs:307（Insert） ｜ BoardSlots.I` | `Insert` | InsertMinion vs Insert（词干互含） |
| `战斗逻辑驱动.tsv` | `RawCardScript.HasPlayedThirdCardInTurn()` | `RuleEngine/Core/PlayedThirdCardInTurn.cs:55（FirePlayedTh` | `FirePlayedThirdCardInTurn` | HasPlayedThirdCardInTurn vs FirePlayedThirdCardInTurn（同词干） |
| `战斗逻辑驱动.tsv` | `RawCardScript.IsTargetedCard()` | `RuleEngine/Core/CardDef.cs:324（Targeted） ｜ ` ｜ CardDef.T` | `Targeted` | IsTargetedCard vs Targeted（词干互含） |
| `战斗逻辑驱动.tsv` | `RawCardScript.OnTrigger(AbilityTrigger trigger, BattleManage` | `RuleEngine/Core/EffectResolver.cs:7148（TriggerNameOfWhen` | `TriggerNameOfWhenKind` | OnTrigger vs TriggerNameOfWhenKind（词干互含） |
| `战斗逻辑驱动.tsv` | `RawCardScript.OnTrigger(AbilityTrigger trigger, BattleManage` | `RuleEngine/Core/EffectResolver.cs:7148（TriggerNameOfWhen` | `TriggerNameOfWhenKind` | OnTrigger vs TriggerNameOfWhenKind（词干互含） |
| `战斗逻辑驱动.tsv` | `RawCardScript.UpdateWhileInPlay(BattleManager manager, CardS` | `RuleEngine/Core/EffectResolver.cs:7736（EmitWhileInPlayPa` | `EmitWhileInPlayPass` | UpdateWhileInPlay vs EmitWhileInPlayPass（词干互含） |
| `特效动画.tsv` | `AnimFXController.PlaySound(AudioCue audioCue)` | `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:801（`PlayO` | `PlayOneSound` | PlaySound vs PlayOneSound（词干互含） |
| `特效动画.tsv` | `AnimFXModuleEvent.TryFireEvent(ActionStart actionStart)` | `WarpforgeVFX/Runtime/WFModuleEvent.cs:240（`Fire`）` | `Fire` | TryFireEvent vs Fire（词干互含） |
| `特效动画.tsv` | `BlinkGraphic.Start()` | `CardPresentation/Shell/BlinkGraphic.cs:166（`Restart`）` | `Restart` | Start vs Restart（词干互含） |
| `特效动画.tsv` | `DelayTween.GetTween()` | `CardPresentation/Core/UnitTweenTable.cs:218（`MakeTween` ` | `MakeTween` | GetTween vs MakeTween（同词干） |
| `特效动画.tsv` | `LUTBlender.DoBlend(float blendValue)` | `CardPresentation/Battle/BattlePostFx.cs:342（`BlendTo`）` | `BlendTo` | DoBlend vs BlendTo（词干互含） |
| `特效动画.tsv` | `ResetBodyTween.GetTween()` | `CardPresentation/Core/UnitTweenTable.cs:218（`MakeTween`）` | `MakeTween` | GetTween vs MakeTween（同词干） |
| `特效动画.tsv` | `ScaleTween.GetTween()` | `CardPresentation/Core/UnitTweenTable.cs:218（`MakeTween`）` | `MakeTween` | GetTween vs MakeTween（同词干） |
| `特效动画.tsv` | `ShakeTween.GetTween()` | `CardPresentation/Core/UnitTweenTable.cs:218（`MakeTween`）` | `MakeTween` | GetTween vs MakeTween（同词干） |
| `特效动画.tsv` | `TweenInfoAnimationBase.GetTween(CardScript source, CardScript target)` | `CardPresentation/Core/UnitTweenTable.cs:218（`MakeTween`）` | `MakeTween` | GetTween vs MakeTween（同词干） |
| `菜单_外壳.tsv` | `AttackTypesButtonsController.HighlightSelectedAttackTypeButtons(AttackTypes` | `Battle/AttackSelector.cs:1262 SetSelected` | `SetSelected` | HighlightSelectedAttackTypeButtons vs SetSelected（词干互含） |
| `菜单_外壳.tsv` | `AttackTypesButtonsController.ToggleAttackTypesSelectorJustOne(bool option, ` | `Battle/AttackSelector.cs:1230 ShowJustOne` | `ShowJustOne` | ToggleAttackTypesSelectorJustOne vs ShowJustOne（词干互含） |
| `菜单_外壳.tsv` | `CardsHorizontalLayout.GetPosition(int cardIndex, int totalCards, int` | `Hand/HandLayout.cs:597 SlotPosition（选中态由视图单独处理）` | `SlotPosition` | GetPosition vs SlotPosition（词干互含） |
| `菜单_外壳.tsv` | `CardsHorizontalLayout.GetRotation(Vector3 cardPosition, int totalCar` | `Hand/HandLayout.cs:625 RotationAt` | `RotationAt` | GetRotation vs RotationAt（词干互含） |
| `菜单_外壳.tsv` | `EverguildButton.OnPointerClick(PointerEventData eventData)` | `CardPresentation/Shell/WindowButton.cs:897（Click）` | `Click` | OnPointerClick vs Click（词干互含） |
| `菜单_外壳.tsv` | `EverguildButton.OnPointerEnter(PointerEventData eventData)` | `Shell/WindowButton.cs:485 Enter` | `Enter` | OnPointerEnter vs Enter（词干互含） |
| `菜单_外壳.tsv` | `EverguildButton.OnPointerExit(PointerEventData eventData)` | `Shell/WindowButton.cs:495 Exit` | `Exit` | OnPointerExit vs Exit（词干互含） |
| `菜单_外壳.tsv` | `EverguildButton.SoftDisable(bool disable)` | `CardPresentation/Shell/WindowButton.cs:725（SetSoftDisabl` | `SetSoftDisabled` | SoftDisable vs SetSoftDisabled（词干互含） |
| `菜单_外壳.tsv` | `EverguildButtonStateFollower.Pressed()` | `Shell/WindowButton.cs:505 Press + ApplyTint` | `Press` | Pressed vs Press（词干互含） |
| `菜单_外壳.tsv` | `EverguildButtonStateFollower.Selected()` | `Shell/WindowButton.cs:128 SetSelected + ApplyTint` | `SetSelected` | Selected vs SetSelected（同词干） |
| `菜单_外壳.tsv` | `EverguildToggle.OnPointerClick(PointerEventData eventData)` | `CardPresentation/Shell/WindowButton.cs:897（Click）` | `Click` | OnPointerClick vs Click（词干互含） |
| `菜单_外壳.tsv` | `EverguildToggle.OnPointerEnter(PointerEventData eventData)` | `Shell/WindowButton.cs:485 Enter` | `Enter` | OnPointerEnter vs Enter（词干互含） |
| `菜单_外壳.tsv` | `GenericOptionsPanel.SetButtons(IEnumerable<Button> buttons, bool l` | `CardPresentation/Shell/GenericOptionsPanel.cs:456（Rebuil` | `RebuildButtons` | SetButtons vs RebuildButtons（词干互含） |
| `菜单_外壳.tsv` | `NavigationPanelController.GetToggle(NavigationPanelToggleType type)` | `CardPresentation/Shell/MainMenuRuntime.cs:876（LightNavTo` | `LightNavToggle` | GetToggle vs LightNavToggle（词干互含） |
| `菜单_外壳.tsv` | `NavigationPanelController.SetToggle(NavigationPanelToggleType type)` | `CardPresentation/Shell/MainMenuRuntime.cs:863（SetNavTogg` | `SetNavToggle` | SetToggle vs SetNavToggle（词干互含） |
| `菜单_外壳.tsv` | `OpenWindowButton.CloseWindow()` | `CardPresentation/Shell/WindowsManager.cs:642（CloseAllWin` | `CloseAllWindows` | CloseWindow vs CloseAllWindows（词干互含） |
| `菜单_外壳.tsv` | `PopUpGameWindow.PopUpWindowButtonPressed()` | `CardPresentation/Shell/WindowsManager.cs:1333（ShowPopUp）` | `ShowPopUp` | PopUpWindowButtonPressed vs ShowPopUp（词干互含） |
| `菜单_外壳.tsv` | `PopUpGameWindow.SetText(string text, bool localizeTexts)` | `CardPresentation/Shell/PopUpGameWindow.cs:389（SetLabelTe` | `SetLabelText` | SetText vs SetLabelText（词干互含） |
| `菜单_外壳.tsv` | `ScrollViewFocusFunctions.FocusOnItem(ScrollRect scrollView, RectTransfo` | `CardPresentation/Shell/MenuScroll.cs:321（FocusOn）` | `FocusOn` | FocusOnItem vs FocusOn（词干互含） |
| `菜单_外壳.tsv` | `Shade.SwitchShadeToAlpha(bool switchOn, bool useAnim` | `CardPresentation/Shell/Shade.cs:40（SetAlpha）` | `SetAlpha` | SwitchShadeToAlpha vs SetAlpha（词干互含） |
| `菜单_外壳.tsv` | `TitleTab.SetTitle(CosmeticItemTitle item)` | `CardPresentation/Shell/ProfileData.cs:263（Titles 称号表；Set` | `Titles` | SetTitle vs Titles（词干互含） |
| `战斗界面_HUD.tsv` | `ActiveSkillDisplay.PointerExit()` | `CardPresentation/Battle/SkillPanel.cs:925（`ExtinguishPre` | `OnSkillPointerExit` | PointerExit vs OnSkillPointerExit（词干互含） |
| `战斗界面_HUD.tsv` | `ActiveSkillDisplay.ShowActingLight()` | `CardPresentation/Battle/SkillPanel.cs:1547（`SetActing`）` | `SetActing` | ShowActingLight vs SetActing（词干互含） |
| `战斗界面_HUD.tsv` | `BasicCardUI.SetCardAlpha(float newAlpha)` | `CardPresentation/Core/CardView.cs:7845（`SetAlpha`）` | `SetAlpha` | SetCardAlpha vs SetAlpha（词干互含） |
| `战斗界面_HUD.tsv` | `BasicCardUI.SetCardImage()` | `CardPresentation/Core/CardView.cs:7045（`SetCardImageTo3D` | `SetCardImageTo3DBase` | SetCardImage vs SetCardImageTo3DBase（词干互含） |
| `战斗界面_HUD.tsv` | `BasicCardUI.SetCardImage(Sprite sprite)` | `CardPresentation/Core/CardView.cs:7045（`SetCardImageTo3D` | `SetCardImageTo3DBase` | SetCardImage vs SetCardImageTo3DBase（词干互含） |
| `战斗界面_HUD.tsv` | `BasicCardUI.SetRawCardData(RawCardScript card, CardTier? t` | `CardPresentation/Core/CardView.cs:3266（`SetData`）` | `SetData` | SetRawCardData vs SetData（词干互含） |
| `战斗界面_HUD.tsv` | `BasicCardUI.SetupCreatedByText(string newText)` | `CardPresentation/Core/CardView.cs:4825（`FillCreatedBy`）` | `FillCreatedBy` | SetupCreatedByText vs FillCreatedBy（词干互含） |
| `战斗界面_HUD.tsv` | `BasicCardUI.TintCard(Color newColor, float time)` | `CardPresentation/Core/CardView.cs:8032（`SetTint`）` | `SetTint` | TintCard vs SetTint（词干互含） |
| `战斗界面_HUD.tsv` | `BattleCardManager.AddCardToDeck(CardScript card, List<CardScript` | `RuleEngine/Core/PlayerState.cs:19（`Deck` 列表 = 牌库）` | `Deck` | AddCardToDeck vs Deck（词干互含） |
| `战斗界面_HUD.tsv` | `BattleCardUI.CardHandToBoardAnimationFinished()` | `CardPresentation/Core/CardFeel.cs:1996（`HandToBoard`）` | `HandToBoard` | CardHandToBoardAnimationFinished vs HandToBoard（词干互含） |
| `战斗界面_HUD.tsv` | `BattleCardUI.ChangeHighlightToSelectColor()` | `CardPresentation/Core/CardView.cs:8159（`SetHighlight`）` | `SetHighlight` | ChangeHighlightToSelectColor vs SetHighlight（词干互含） |
| `战斗界面_HUD.tsv` | `BattleCardUI.ChangeToHighlightColor()` | `CardPresentation/Core/CardView.cs:8159（`SetHighlight`）` | `SetHighlight` | ChangeToHighlightColor vs SetHighlight（词干互含） |
| `战斗界面_HUD.tsv` | `BattleCardUI.CleanStatusAnims(bool clearAll)` | `CardPresentation/Core/TraitFrames.cs:929（`Clean`）` | `Clean` | CleanStatusAnims vs Clean（词干互含） |
| `战斗界面_HUD.tsv` | `BattleCardUI.CreateRemnantBody()` | `CardPresentation/Core/CardView.cs:2524（`SetRemnantBody`）` | `SetRemnantBody` | CreateRemnantBody vs SetRemnantBody（同词干） |
| `战斗界面_HUD.tsv` | `BattleCardUI.DestroyRemnantBody()` | `CardPresentation/Core/CardView.cs:2524（`SetRemnantBody`）` | `SetRemnantBody` | DestroyRemnantBody vs SetRemnantBody（词干互含） |
| `战斗界面_HUD.tsv` | `BattleCardUI.DisplayCreatedByText(CardStateOptions newState` | `CardPresentation/Core/CardView.cs:4825（`FillCreatedBy`）` | `FillCreatedBy` | DisplayCreatedByText vs FillCreatedBy（词干互含） |
| `战斗界面_HUD.tsv` | `BattleCardUI.DisplayStatusAnim(DefinedTrait trait, bool pla` | `CardPresentation/Core/TraitFrames.cs:790（`DisplayStatus`` | `DisplayStatus` | DisplayStatusAnim vs DisplayStatus（词干互含） |
| `战斗界面_HUD.tsv` | `BattleCardUI.DisplayTriggerAnim(DefinedTrait trait, bool pl` | `CardPresentation/Core/TraitFrames.cs:883（`DisplayTrigger` | `DisplayTrigger` | DisplayTriggerAnim vs DisplayTrigger（词干互含） |
| `战斗界面_HUD.tsv` | `BattleCardUI.SetAmbushTexts()` | `CardPresentation/Core/CardView.cs:7187（`ApplyAmbush`）` | `ApplyAmbush` | SetAmbushTexts vs ApplyAmbush（词干互含） |
| `战斗界面_HUD.tsv` | `BattleCardUI.SetRawCardData(CardScript cardScript)` | `CardPresentation/Core/CardView.cs:3266（`SetData`）` | `SetData` | SetRawCardData vs SetData（词干互含） |
| `战斗界面_HUD.tsv` | `BattleCardUI.SetSortingLayer(string layerName)` | `CardPresentation/Core/CardView.cs:2248（`SetLayer`）` | `SetLayer` | SetSortingLayer vs SetLayer（词干互含） |
| `战斗界面_HUD.tsv` | `BattleCardUI.ShowPlaceholderSprite()` | `CardPresentation/Core/CardView.cs:113（`Placeholder` 占位图）` | `Placeholder` | ShowPlaceholderSprite vs Placeholder（词干互含） |
| `战斗界面_HUD.tsv` | `BattleCardUI.ToggleBody3D(bool optionBody, bool? optionEffe` | `CardPresentation/Core/CardView.cs:7672（`BuildBody3D`）` | `BuildBody3D` | ToggleBody3D vs BuildBody3D（同词干） |
| `战斗界面_HUD.tsv` | `BattleCardUI.ToggleFlying(bool option)` | `CardPresentation/Battle/FlyingCardController.cs:99（`Flyi` | `FlyingCardController` | ToggleFlying vs FlyingCardController（词干互含） |
| `战斗界面_HUD.tsv` | `CardDisplayAttackTypeButton.SetupActiveAbilityTooltip(RawCardScript cardWi` | `CardPresentation/Battle/AttackSelector.cs:1769（`RefreshA` | `RefreshAbilityTooltip` | SetupActiveAbilityTooltip vs RefreshAbilityTooltip（词干互含） |
| `战斗界面_HUD.tsv` | `CardDisplayWindow.SetCardVoiceOver(BasicCardUI card)` | `CardPresentation/Battle/CardDisplayWindow.cs:1152（`PlayV` | `PlayVoice` | SetCardVoiceOver vs PlayVoice（词干互含） |
| `战斗界面_HUD.tsv` | `FlyingCardController.IsFlying()` | `CardPresentation/Battle/FlyingCardController.cs:99（`Flyi` | `FlyingCardController` | IsFlying vs FlyingCardController（词干互含） |
| `战斗界面_HUD.tsv` | `PracticeModePopup.GameModeValueChange(bool option)` | `CardPresentation/Shell/PracticeModePopup.cs:1523（`SetGam` | `SetGameMode` | GameModeValueChange vs SetGameMode（词干互含） |
| `战斗界面_HUD.tsv` | `TargetReticleController.SetAttackType(AttackTypes currentAttackType)` | `CardPresentation/Battle/TargetReticle.cs:647（`ApplyAttac` | `ApplyAttackType` | SetAttackType vs ApplyAttackType（同词干） |
| `卡牌面_渲染.tsv` | `BoardTraitIcon.UpdateCounter(int counter)` | `CardPresentation/Core/CardView.cs:3889（`PlaceBadgeCounte` | `PlaceBadgeCounter` | UpdateCounter vs PlaceBadgeCounter（词干互含） |
| `卡牌面_渲染.tsv` | `Card2DController.ChangeShadowColor(Color color, bool isCardback` | `CardPresentation/Core/CardView.cs:6180（`ApplyShadowColor` | `ApplyShadowColor` | ChangeShadowColor vs ApplyShadowColor（词干互含） |
| `卡牌面_渲染.tsv` | `Card2DController.SetFrame(Sprite frameSprite, Sprite highlightS` | `CardPresentation/Core/CardArt.cs:40（`Frame`）+ :109（`Sdf`` | `Frame` | SetFrame vs Frame（同词干） |
| `卡牌面_渲染.tsv` | `Card2DController.ToggleHighlight(bool option, bool isCardback, ` | `CardPresentation/Core/CardView.cs:8229（`SetHoverHighligh` | `SetHoverHighlight` | ToggleHighlight vs SetHoverHighlight（词干互含） |
| `卡牌面_渲染.tsv` | `CardImageController.SetImage(Sprite sprite, CardTypeOptions cardTy` | `CardPresentation/Core/CardView.cs:7045（`SetCardImageTo3D` | `SetCardImageTo3DBase` | SetImage vs SetCardImageTo3DBase（词干互含） |
| `卡牌面_渲染.tsv` | `CardImageController.SetOriginalMaterial()` | `CardPresentation/Core/CardView.cs:7033（`RestoreOriginalM` | `RestoreOriginalMaterial` | SetOriginalMaterial vs RestoreOriginalMaterial（词干互含） |
| `卡牌面_渲染.tsv` | `CardServices.CheckForCardUpgrade(RawCardScript card, out Ca` | `CardPresentation/Shell/CardDetailPopup.cs:323（`CanUpgrad` | `CanUpgrade` | CheckForCardUpgrade vs CanUpgrade（词干互含） |
| `卡牌面_渲染.tsv` | `CardTextCountersController.HideStat()` | `CardPresentation/Core/CardView.cs:12949（`StatShownFor`）` | `StatShownFor` | HideStat vs StatShownFor（词干互含） |
| `卡牌面_渲染.tsv` | `CardTextCountersController.SetText(int newAmount)` | `CardPresentation/Core/CardView.cs:13151（`SyncFaceStatTex` | `SyncFaceStatTexts` | SetText vs SyncFaceStatTexts（词干互含） |
| `卡牌面_渲染.tsv` | `TextTooltipController.GetTraitTooltip(string linkId)` | `CardPresentation/Core/Tooltip.cs:1188（`TipText.Trait`）` | `Trait` | GetTraitTooltip vs Trait（词干互含） |
| `卡牌面_渲染.tsv` | `TextTooltipController.InitializeTooltips()` | `CardPresentation/Core/Tooltip.cs:1020（`LoadTips`）` | `LoadTips` | InitializeTooltips vs LoadTips（词干互含） |
| `卡牌面_渲染.tsv` | `UICardInfoItem.OnDestroy()` | `CardPresentation/Deck/DeckRuntime.cs:8690（`DestroySafe`：` | `DestroySafe` | OnDestroy vs DestroySafe（词干互含） |
| `卡组编辑.tsv` | `ImportDeckPopup.TryImportDeck(CardDeck deck, Action onImport)` | `MyGame/Assets/CardPresentation/Shell/ImportDeckPopup.cs:` | `TryImport` | TryImportDeck vs TryImport（词干互含） |
| `收藏.tsv` | `CollectionDisplay.RefreshCells()` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs` | `RebuildCardsCells` | RefreshCells vs RebuildCardsCells（词干互含） |
| `收藏.tsv` | `CollectionDisplay.Select(CollectionItem<T> collectable, PointerE` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs` | `SelectDeck` | Select vs SelectDeck（词干互含） |
| `收藏.tsv` | `CollectionDisplay.SetCell(ICell cell, int index)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs` | `RebuildCardsCells` | SetCell vs RebuildCardsCells（词干互含） |
| `收藏.tsv` | `CollectionDisplay.ToggleFilters(bool state)` | `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs` | `ToggleFiltersNow` | ToggleFilters vs ToggleFiltersNow（词干互含） |
| `轮抽.tsv` | `DraftModeMenuDemo.GoToCurrentState()` | `CardPresentation/Shell/DraftModeWindow.cs:2190（`UpdateSt` | `UpdateState` | GoToCurrentState vs UpdateState（词干互含） |
| `轮抽.tsv` | `DraftModeMenuDemo.ToggleDeckInfo(bool state)` | `CardPresentation/Shell/DraftModeWindow.cs:1818（`BuildDec` | `BuildDeckInfoPanel` | ToggleDeckInfo vs BuildDeckInfoPanel（词干互含） |
| `轮抽.tsv` | `DraftModeMenuOngoingState._003CResignButtonClick_003Eb__16_0(GameObject ` | `CardPresentation/Shell/DraftModeWindow.cs:3211（`Resign` ` | `Resign` | _003CResignButtonClick_003Eb__16_0 vs Resign（词干互含） |
| `轮抽.tsv` | `DraftModeSelectPacksStateDemo.PackPreview(PrebuiltPack selectedPack)` | `CardPresentation/Shell/DraftModeWindow.cs:1978（`ShowPack` | `ShowPackPreview` | PackPreview vs ShowPackPreview（同词干） |
| `战役.tsv` | `CampaignHandler.GetCampaign(CardArmy army)` | `CardPresentation/Shell/CampaignData.cs:146（`CampaignData` | `CampaignData` | GetCampaign vs CampaignData（词干互含） |
| `战役.tsv` | `CampaignHandler.SetSelectedArmy(CardArmy army)` | `CardPresentation/Shell/CampaignData.cs:304（`Select`）` | `Select` | SetSelectedArmy vs Select（词干互含） |
| `战役.tsv` | `CampaignNode.SetLineColors(Color lineColor)` | `CardPresentation/Shell/CampaignTab.cs:692（`BuildLine` 末句` | `BuildLine` | SetLineColors vs BuildLine（词干互含） |
| `战役.tsv` | `CampaignNode.Unlock()` | `CardPresentation/Shell/CampaignData.cs:163（`IsUnlocked`，` | `IsUnlocked` | Unlock vs IsUnlocked（词干互含） |
| `战役.tsv` | `CampaignTrack.ClearTrack()` | `CardPresentation/Shell/CampaignTab.cs:512（`ClearTrackCon` | `ClearTrackContent` | ClearTrack vs ClearTrackContent（词干互含） |
| `战役.tsv` | `CampaignTrack.GetNode(int index)` | `CardPresentation/Shell/CampaignTab.cs:567（`NodeRect`）` | `NodeRect` | GetNode vs NodeRect（词干互含） |
| `战役.tsv` | `CampaignTrack.GetNodePosition(CampaignNodeInfo data, Vector2` | `CardPresentation/Shell/CampaignTab.cs:552（`NodePos`）` | `NodePos` | GetNodePosition vs NodePos（词干互含） |
| `战役.tsv` | `CampaignWindowTab.LockArmySelectorByArmy(CardArmy selectedArmy)` | `CardPresentation/Shell/CampaignTab.cs:199（`LockArmySelec` | `LockArmySelector` | LockArmySelectorByArmy vs LockArmySelector（词干互含） |
| `锻造.tsv` | `CardInBoosterPack.ResetContentAnimation()` | `Shell/BoosterPackOpenWindow.cs:875 ResetContent` | `ResetContent` | ResetContentAnimation vs ResetContent（词干互含） |
| `锻造.tsv` | `Forge.GetRewardsToCollect()` | `Shell/ForgeData.cs:162 HasToCollect + StateAt==ToCollect` | `HasToCollect` | GetRewardsToCollect vs HasToCollect（词干互含） |
| `锻造.tsv` | `Forge.HasRewardToCollect()` | `Shell/ForgeData.cs:162 HasToCollect` | `HasToCollect` | HasRewardToCollect vs HasToCollect（词干互含） |
| `锻造.tsv` | `ForgeHandler.HasAnyArmyRewardsToCollect()` | `Shell/ForgeData.cs:162 HasToCollect 任一 army` | `HasToCollect` | HasAnyArmyRewardsToCollect vs HasToCollect（词干互含） |
| `锻造.tsv` | `ForgeHandler.HasRewardToCollect(CardArmy army)` | `CardPresentation/Shell/ForgeData.cs:162（HasToCollect 方法）` | `HasToCollect` | HasRewardToCollect vs HasToCollect（词干互含） |
| `其他_基础设施.tsv` | `ArmyUtilities.GetDefaultCardback(CardArmy army)` | `CardPresentation/Core/CardArt.cs:399` | `CardBack` | GetDefaultCardback vs CardBack（词干互含） |
| `其他_基础设施.tsv` | `GameStaticData.IsNeutralArmy(CardArmy army)` | `CardPresentation/Core/ArmyIcons.cs:54` | `Neutral` | IsNeutralArmy vs Neutral（词干互含） |
| `未分类_其他.tsv` | `ScriptedActionData.CheckIfMatchesActionData(BattleActionType batt` | `RuleEngine/Core/TutorialScript.cs:1204（Matches，逐字照抄的分支表）` | `Matches` | CheckIfMatchesActionData vs Matches（词干互含） |
| `泛型与启动链_补读.tsv` | `CollectionFilter<T>.ClearFilters()` | `Shell/CollectionWindow.cs:2054` | `ClearCosmoFilters` | ClearFilters vs ClearCosmoFilters（词干互含） |
| `泛型与启动链_补读.tsv` | `ItemDrawer<T>.SetConvertedItem(RewardInfo convertedTo)` | `Shell/ItemDrawer.cs:961` | `SetConverted` | SetConvertedItem vs SetConverted（词干互含） |
| `泛型与启动链_补读.tsv` | `ItemDrawer<T>.SetEphemeralDisplay(EphemeralState state)` | `Shell/ItemDrawer.cs:933` | `SetEphemeral` | SetEphemeralDisplay vs SetEphemeral（词干互含） |
| `泛型与启动链_补读.tsv` | `PlayEvent<TSave,.ValidateEventDeck(CardDeck deck, bool validate` | `RuleEngine/Core/DeckRules.cs:443` | `Validate` | ValidateEventDeck vs Validate（词干互含） |
| `泛型与启动链_补读.tsv` | `DraftEventBase<TS,.ReRollPack(Action onSuccess, Action onFail)` | `Shell/DraftModeWindow.cs:2971 ReRollPacks` | `ReRollPacks` | ReRollPack vs ReRollPacks（词干互含） |
| `社交_聊天.tsv` | `AllianceScoreBarLine.ShowChestOpen()` | `Shell/AllianceEventScoreInfo.cs:204` | `SwapChestOpen` | ShowChestOpen vs SwapChestOpen（词干互含） |
| `社交_聊天.tsv` | `AllianceTrophy._003Cget_CurrentTier_003Eb__16_0(ChallengeMile` | `Shell/AllianceTrophyModel.cs:76` | `CurrentTier` | _003Cget_CurrentTier_003Eb__16_0 vs CurrentTier（词干互含） |
| `个人资料_成就_排行.tsv` | `LeaderboardManager.GetCachedLeaderboard(string key, long defaultV` | `Shell/LeaderboardData.cs:184` | `GetCached` | GetCachedLeaderboard vs GetCached（词干互含） |
| `个人资料_成就_排行.tsv` | `LeaderboardManager.InvalidateCache(string key)` | `Shell/LeaderboardData.cs:198` | `Invalidate` | InvalidateCache vs Invalidate（词干互含） |
| `个人资料_成就_排行.tsv` | `ProfileRankingSection.HasPlayerReachedLegendaryDivision(int highestE` | `Shell/RankedData.cs:503` | `HasReachedLegendaryDivision` | HasPlayerReachedLegendaryDivision vs HasReachedLegendaryDivision（词干互含） |
| `个人资料_成就_排行.tsv` | `RankedEventBase.GetArmyState(CardArmy army, IReadOnlyList<Rank` | `Shell/RankedData.cs:461` | `ArmyState` | GetArmyState vs ArmyState（同词干） |
| `个人资料_成就_排行.tsv` | `RankedScoreBoostEvent.HasBoostsLeft(CardArmy army)` | `CardPresentation/Shell/RankedData.cs:100` | `BoostsLeftFor` | HasBoostsLeft vs BoostsLeftFor（词干互含） |
| `教程_引导.tsv` | `FlowTutorialStage.CheckForCompletion()` | `Shell/MenuTutorialFlow.cs:543` | `CheckCompletion` | CheckForCompletion vs CheckCompletion（词干互含） |
| `教程_引导.tsv` | `FlowTutorialStage.Reset()` | `Shell/MenuTutorialFlow.cs:384` | `ResetProgress` | Reset vs ResetProgress（词干互含） |
| `教程_引导.tsv` | `TutorialManager.AddPointer(GameObject target, Vector2 offset, ` | `Shell/MenuTutorialFlow.cs:653` | `RefreshPointer` | AddPointer vs RefreshPointer（同词干） |
| `教程_引导.tsv` | `TutorialManager.ShowTutorialMessageWindow(TutorialMessageData ` | `Shell/MenuTutorialFlow.cs:787` | `ShowMessage` | ShowTutorialMessageWindow vs ShowMessage（词干互含） |
| `教程_引导.tsv` | `TutorialStageClaimForgeFirstLevel.HasNoForgeableRewards()` | `Shell/MenuTutorialFlow.cs:644` | `NoForgeableRewards` | HasNoForgeableRewards vs NoForgeableRewards（同词干） |
| `教程_引导.tsv` | `TutorialTipScript.FadeElements(float targetAlpha)` | `CardPresentation/Battle/TutorialOverlay.cs:1158` | `ApplyFade` | FadeElements vs ApplyFade（词干互含） |
| `教程_引导.tsv` | `TutorialTipScript.ShowTip(SmallTipData smallTipData)` | `Battle/TutorialOverlay.cs:925 SetTip` | `SetTip` | ShowTip vs SetTip（同词干） |

### d. 其余「新出现」的硬问题（基线没有、现在不合格；不含我的账）

| 账 | 类.方法 | 落点 | 问题类型 | 建议 |
|---|---|---|---|---|
| `菜单_外壳.tsv` | `AttackTypesButtonsController.OnEnable()` | `Battle/AttackSelector.cs:817 Awake → Build` | 指向别的方法/字段 | — |
| `菜单_外壳.tsv` | `DraggableController._003COnEndDrag_003Eb__6_2(IDropHandler<T> droppable)` | `Core/DraggableController.cs:202 的循环体（判空位置照原版在循环内）` | 语义近名指错(无注解) | — |
| `战斗界面_HUD.tsv` | `CombatCameraZoom.OnDestroy()` | `CardPresentation/Battle/CombatCameraZoom.cs:329（`Awake` | 指向别的方法/字段 | `CardPresentation/Battle/CombatCameraZoom.cs:340` |
| `商店.tsv` | `ShopContainer.OnAfterPurchase(IShopOffer offer, IReadOnlyList<Reward` | `MyGame/Assets/CardPresentation/Shell/BaseOfferPopup.cs` | 指向别的方法/字段 | — |

## 四 我自己六份账的现状（基线未含 ⇒ 不算「新出现」）

| 账 | A | OK | 硬问题 | 软问题（待确认） |
|---|---:|---:|---:|---:|
| `其他_基础设施.tsv` | 11 | 0 | 1 | 10 |
| `未分类_其他.tsv` | 31 | 23 | 0 | 8 |
| `泛型与启动链_补读.tsv` | 73 | 52 | 0 | 21 |
| `社交_聊天.tsv` | 24 | 20 | 0 | 4 |
| `个人资料_成就_排行.tsv` | 52 | 36 | 0 | 16 |
| `教程_引导.tsv` | 52 | 26 | 0 | 26 |

逐条清单：

**其他_基础设施.tsv（1 条）**

| 类.方法 | 落点 | 问题类型 | 建议 |
|---|---|---|---|
| `GameStaticData.CardRarityToString(int rarity)` | `CardPresentation/Core/FilterPanelModel.cs:738` | 函数中间 | — |

**未分类_其他.tsv（0 条）**


**泛型与启动链_补读.tsv（0 条）**


**社交_聊天.tsv（0 条）**


**个人资料_成就_排行.tsv（0 条）**


**教程_引导.tsv（0 条）**


## 五 口径说明

- **硬问题**：注释 / 函数中间 / 指向别的方法 / 文件级 / 仅类描述 / 整类行 / 行号越界 / 文件不存在 / 语义近名(无注解)。
- **软问题（待确认，不计入不合格）**：语义近名但有注解（注解里点名了那一行的方法）/ 异名但备注写了「等价/对应/同一份」。
- 语义近名检测只抓「词干相同或互含」的对（`AddX`↔`CanAddX`、`GetX`↔`SetX`、`Init`↔`Reset`、`Open`↔`Close`…）；
  名字**完全不同**的（`GetArmyIcon`→`Texture`）不算近名 —— 它要么在备注里写了等价，要么已落进「指向别的方法」。
- 我自己六份账的 2 条工具失败（`其他_基础设施`）属 C 的账，未动。

## 六 结论（三个数）

1. **修好了 1115 条**（基线逐条 1506 → 现在仍不合格 391，占 74% 已处理）；另有 **5 条新出现**（域账）。
2. **语义近名指错**：**49 条无注解（硬）** + **129 条有注解（软，待 owner 确认）** —— 这批是工具永远抓不到的。
3. **工具口径**：全账 A **1,923** · 通过 **1,901** · 漂移 **20** · 失败 **2**（两条都在 `其他_基础设施`，属 C 的账）。
   漂移 20 条集中在 `卡牌面`(5)/`战斗逻辑驱动`(5)/`轮抽`(6)/`战斗界面_HUD`(4) —— 那是「声明的行号挪了」，跑 `--fix` 即可。

