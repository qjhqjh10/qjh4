# Battle/ + Deck/ 未走 Loc 的玩家可见文案（只读盘点）

口径：只算 `.cs`，只算**非注释**行。① / ② / ③ 三列里，**① 是精确的字符串个数**；② = 含中文的 `Debug.Log*` / `Debug.Assert` 语句数（实测）；③ = 非注释中文串行数 − ① 行数 − ②，**是高估的上界**（多行 `Debug.*` 的续行落这一列）。范围 = `Battle/` 36 件 + `Deck/` 2 件。

## 一、逐文件表

| 文件 | ①处数 | ②处数 | ③处数 | 主要落点（行号: 字面量，最多 3 条） |
|---|---|---|---|---|
| Battle/BattleDriver.cs | 27 | 113 | 213 | `280: "换牌完成，开打"` · `5652: $"{who}打出「{card}」"` · `6540: "你的牌库"` |
| Deck/DeckRuntime.cs | 10 | 22 | 51 | `933: "返回"` · `1255: "把卡拖到这里"` · `3904: "这不是一条合法的卡组串"` |
| Battle/ChatPopupPanel.cs | 6 | 0 | 0 | `76: { "Greet", "Threat", "Well Played", "Taunt", "Sorry", "Oops" }` |
| Battle/EndPanel.cs | 5 | 0 | 0 | `342: "我方投降" / "对方投降"` · `345: $"{rounds} 回合   敌方战将最低生命 …"` · `346: $"{rounds} 回合"` |
| Battle/SettingsPanel.cs | 3 | 8 | 5 | `479: "Settings"` · `569: "AI Difficulty"` · `612: AutoZoomLabelEn("Auto zoom")` |
| Battle/CardDisplayWindow.cs | 1 | 12 | 3 | `201: "再点一下关闭"` |
| Battle/MultiCardDisplay.cs | 1 | 0 | 0 | `152: "点「继续」或再点一下牌堆关闭"` |
| Battle/TutorialOverlay.cs | 1 | 3 | 2 | `417: "Continue"` |
| Deck/DeckEditorState.cs | 1 | 0 | 0 | `133: "新卡组"`（默认卡组名） |
| 其余 Battle/ 29 件（0 ①）：ArenaPrefabData·ArenaSceneState·ArenaRuntimeLoader·AnimFXController·AttackSelector·BattleBackdrop·BattleCameraSreenSize·BattleDoors·BattleLogPanel·BattlePostFx·CardChoicePanel·ChoosePanel·CombatAutoZoom·CombatCameraZoom·EnvironmentApplier·ImageQuad·Label·LookAtConstrainWIP·MulliganPanel·RemnantSfx·ReplayBar·ReplayStore·ScenarioBlendables·SkillPanel·TargetReticle·TouchInputManager·UnitChatPanel·WaitBanner·WfSlider | 0 | 126 | 287 | 全是 `Debug.Log*` / 自检 getter（`"<无>"`）/ Tooltip / 节点名 / 资源名 |
| **合计** | **55** | **284** | **561** | |

## 二、① 类逐条清单

`Loc.cs` 那列：**在** = 键已在表里（可直接换）；**无** = 表里没有（要新增）。

| # | 文件:行号 | 当前字面量（原文照抄） | 建议键名 | 为什么 | 该键在 `Loc.cs` 里在不在 |
|---|---|---|---|---|---|
| 1 | Battle/BattleDriver.cs:280 | `"换牌完成，开打"` | `Battle/Hint/MulliganDone` | 自拟（提示行文案；原版 `Battle/Tips/*` 那 24 条有没有这条未逐条核） | 无 |
| 2 | Battle/BattleDriver.cs:720 | `"已重连并追平"` | `Battle/Hint/Reconnected` | 自拟（联机断线重连） | 无 |
| 3 | Battle/BattleDriver.cs:2687 | `$"卡组存档读不出来（…）—— 本局自动凑了一副"` | `Battle/Hint/DeckLoadFailed` | 自拟（整句带 `{0}`；同族已有 `MenuDeck/Error/SaveFailed` 但语义不同） | 无 |
| 4 | Battle/BattleDriver.cs:3970 | `"换牌中：点牌上的「换」标记要替换的牌，然后点「完成换牌」"` | `Battle/Hint/MulliganPick` | 自拟 | 无 |
| 5 | Battle/BattleDriver.cs:3981 | `"换牌已提交，等主机定序…"` | `Battle/Hint/MulliganSent` | 自拟 | 无 |
| 6 | Battle/BattleDriver.cs:3999 | `$"换掉了 {n} 张"` | `Battle/Hint/ReplacedCount` | 自拟（带 `{0}`） | 无 |
| 7 | Battle/BattleDriver.cs:4086 | `"选择进攻卡"` | `Battle/ChooseCard/Offensive` | 自拟（选卡面板标题；形状照 `Battle/ChooseCard/Instructions`） | 无 |
| 8 | Battle/BattleDriver.cs:4107 | `"选择防御卡"` | `Battle/ChooseCard/Defensive` | 自拟（同上） | 无 |
| 9 | Battle/BattleDriver.cs:5232 | `"选一张牌，然后点「继续」"` / `"选一项，然后点「继续」"` | `Battle/Hint/ChooseCard` / `Battle/Hint/ChooseOption` | 自拟（两条） | 无 |
| 10 | Battle/BattleDriver.cs:5646 | `"我方"` / `"敌方"` | `Battle/Log/SideMe` / `Battle/Log/SideFoe` | 自拟（战斗日志归属；两条） | 无 |
| 11 | Battle/BattleDriver.cs:5652 | `$"{who}打出「{card}」"` | `Battle/Log/Play` | 自拟（带两个占位符） | 无 |
| 12 | Battle/BattleDriver.cs:5654 | `$"{who}「{card}」进入格位 {e.Slot + 1}"` | `Battle/Log/Deploy` | 自拟 | 无 |
| 13 | Battle/BattleDriver.cs:5656 | `"远程"` / `"近战"` | `Battle/Log/Ranged` / `Battle/Log/Melee` | 自拟（日志里的定语；⛔ 别复用 `Battle/Tips/MeleeAttack`，那条值是「近战攻击」，套进去会变「近战攻击攻击」） | 无 |
| 14 | Battle/BattleDriver.cs:5660 | `$"{who}「{card}」回复 {-e.Amount} 点生命"` | `Battle/Log/Heal` | 自拟 | 无 |
| 15 | Battle/BattleDriver.cs:5661 | `$"{who}「{card}」受到 {e.Amount} 点伤害"` | `Battle/Log/Damage` | 自拟 | 无 |
| 16 | Battle/BattleDriver.cs:5663 | `$"{who}「{card}」阵亡"` | `Battle/Log/Death` | 自拟 | 无 |
| 17 | Battle/BattleDriver.cs:5666 | `$"{who}「{card}」离开格位（回手牌或牌库）"` | `Battle/Log/Return` | 自拟 | 无 |
| 18 | Battle/BattleDriver.cs:5668 | `$"{who}「{card}」发动技能"` | `Battle/Log/Ability` | 自拟 | 无 |
| 19 | Battle/BattleDriver.cs:5670 | `$"{who}「{card}」触发「{e.Keyword ?? "效果"}」"` + `"效果"` | `Battle/Log/Trigger` / `Battle/Log/EffectFallback` | 自拟（两条） | 无 |
| 20 | Battle/BattleDriver.cs:5672 | `$"{who}「{card}」"` | `Battle/Log/Bare` | 自拟 | 无 |
| 21 | Battle/BattleDriver.cs:5678 | `$"回合 {e.Turn}　{line}"` | `Battle/Log/TurnPrefix` | 自拟（含全角空格，是格式串） | 无 |
| 22 | Battle/BattleDriver.cs:6133 | `"联机局不能自己重开 —— 对面还在这一局里"` | `Battle/Hint/NoRestartOnline` | 自拟 | 无 |
| 23 | Battle/BattleDriver.cs:6540 | `"你的牌库"` | `Battle/MultiCard/Title` | 自拟（多张牌展示窗标题；原版那格 `Header Text` **无 `Localize`**，见 `MultiCardDisplay.cs` 注释） | 无 |
| 24 | Battle/CardDisplayWindow.cs:201 | `"再点一下关闭"` | `Battle/CardWindow/TapToClose` | 自拟 | 无 |
| 25 | Battle/MultiCardDisplay.cs:152 | `"点「继续」或再点一下牌堆关闭"` | `Battle/MultiCard/TapToClose` | 自拟（**已知缺口**，代码注释自己写着「没有原版键可接」） | 无 |
| 26 | Battle/TutorialOverlay.cs:417 | `"Continue"` | `Battle/Tips/Continue` | **原版键**，已是 `Loc.cs` 里的现成键，直接换即可 | **在** |
| 27 | Battle/SettingsPanel.cs:479 | `"Settings"` | `Battle/Settings/Title` | 自拟（原版这扇窗**没有标题节点**，判据写在 `SettingsPanel.cs:470-478`，已升级为否定证据） | 无 |
| 28 | Battle/SettingsPanel.cs:569 | `"AI Difficulty"` | `Battle/Settings/AiDifficulty` | 自拟（原版没有这一行，是我们自加的） | 无 |
| 29 | Battle/SettingsPanel.cs:612 | `AutoZoomLabelEn` = `"Auto zoom"` | `Settings/Graphics/AutoZoom` | **原版键已在本文件 :254 记着**，只是显示点没走 `Loc.T`（当初理由「先照原版英文」） | **在** |
| 30 | Battle/ChatPopupPanel.cs:76 | `"Greet"` | `Battle/Chat/Greet` | 自拟（原版标签来自远端 I18N，本地不存在，见该行 doc） | 无 |
| 31 | Battle/ChatPopupPanel.cs:76 | `"Threat"` | `Battle/Chat/Threat` | 自拟 | 无 |
| 32 | Battle/ChatPopupPanel.cs:76 | `"Well Played"` | `Battle/Chat/WellPlayed` | 自拟 | 无 |
| 33 | Battle/ChatPopupPanel.cs:76 | `"Taunt"` | `Battle/Chat/Taunt` | 自拟 | 无 |
| 34 | Battle/ChatPopupPanel.cs:76 | `"Sorry"` | `Battle/Chat/Sorry` | 自拟 | 无 |
| 35 | Battle/ChatPopupPanel.cs:76 | `"Oops"` | `Battle/Chat/Oops` | 自拟 | 无 |
| 36 | Battle/EndPanel.cs:342 | `"我方投降"` | `Battle/BattleEnd/ForfeitMe` | 自拟（副标题那一行**原版无对应件**，见 `EndPanel.cs:322-341`） | 无 |
| 37 | Battle/EndPanel.cs:342 | `"对方投降"` | `Battle/BattleEnd/ForfeitFoe` | 自拟 | 无 |
| 38 | Battle/EndPanel.cs:342 | `$"{rounds} 回合   "` | `Battle/BattleEnd/Rounds` | 自拟（带 `{0}`） | 无 |
| 39 | Battle/EndPanel.cs:345 | `$"{rounds} 回合   敌方战将最低生命 {minFoeWarlordHealth}"` | `Battle/BattleEnd/RoundsMinFoeHealth` | 自拟 | 无 |
| 40 | Battle/EndPanel.cs:346 | `$"{rounds} 回合"` | `Battle/BattleEnd/RoundsOnly` | 自拟 | 无 |
| 41 | Deck/DeckRuntime.cs:933 | `"返回"` | `MenuDeck/Button/Back` | 自拟（照 `MenuDeck/Button/Random` 的先例） | 无 |
| 42 | Deck/DeckRuntime.cs:1255 | `"把卡拖到这里"` | `MenuDeck/HUD/DropCardHere` | 自拟（空卡位占位词） | 无 |
| 43 | Deck/DeckRuntime.cs:2145 | `"默认卡背（没选过）"` | `MenuDeck/HUD/DefaultCardback` | 自拟（卡背抽屉名那一格） | 无 |
| 44 | Deck/DeckRuntime.cs:3904 | `"先粘贴卡组串"` | `MenuDeck/Error/ImportEmpty` | 自拟（显示在 `imp_err`，见 :3952） | 无 |
| 45 | Deck/DeckRuntime.cs:3904 | `"这不是一条合法的卡组串"` | `MenuDeck/Error/ImportBadString` | 自拟 | 无 |
| 46 | Deck/DeckRuntime.cs:3928 | `"导入失败：卡组串读出来了，但**没写进存档**——"` + `"（重启就没了）"` | `MenuDeck/Error/ImportNotPersisted` | 自拟（一句两段，建议合成一条带 `{0}`） | 无 |
| 47 | Deck/DeckRuntime.cs:746 | `Library.Create("我的卡组")` | `MenuDeck/DefaultDeckName` | 自拟（空库时替玩家建的那套的**名字**，会被画到页头/列表） | 无 |
| 48 | Deck/DeckRuntime.cs:3430 | `State.SetDeckName("新卡组")` | `MenuDeck/NewDeckName` | 自拟（清空名字后用，会显示） | 无 |
| 49 | Deck/DeckRuntime.cs:6323 | `new PlayerDeck { Name = "复仇者之刃" }` | `MenuDeck/DemoDeckName` | 自拟（演示卡组名，会显示） | 无 |
| 50 | Deck/DeckEditorState.cs:133 | `"新卡组"` | `MenuDeck/NewDeckName` | 自拟（⛔ 与 #48 **同一条**，两处别各起一个键） | 无 |

## 三、🔴 已经用 `Loc.T` / `Loc.HasEntry` 接好的消费点清单（= 不用再动的）

- Battle/BattleDriver.cs:2450 → `lib.LastLoadIssueTerm`（动态）· :3584 → `Battle/Tips/HandFull`（过 `Loc.HasEntry` 闸）· :6428 → `Battle/Tips/DragToTarget` · :7168 → `Battle/Tips/UnitNotReady`
- Battle/BattleDriver.cs:7771 → `RuleCodes.TermKey(rc)`（`Loc.HasEntry` 闸 + 兜底 `RuleCodes.Describe`）· :8745-8746 → `Battle/Tips/{MeleeAttack,RangeAttack,HealthPoints,EnergyCost}`
- Battle/BattleDriver.cs:12053 / :12065 → `Battle/Overtime/Title` · :12415 → `CountText(term, n)`（`Battle/HUD/CardsInHand` / `CardsLeft`）
- Battle/CardDisplayWindow.cs:349 → `Battle/HUD/AffectedBy`；:422 → 卡面每行词条（`Battle/Effect/*`）
- Battle/ChoosePanel.cs:56 → `Battle/ChooseCard/Instructions`；:62 → `Battle/Mulligan/ButtonDone`；:86 → `Battle/Prebattle/SelectButton`
- Battle/MulliganPanel.cs:64 → `Battle/Mulligan/ButtonDone`；:133 → `Battle/Mulligan/{Undo,Replace}`；:153/154 → `Battle/Tips/GoFirst` / `Battle/Mulligan/secondTurn`；:235 → `Battle/Mulligan/Instructions`
- Battle/SettingsPanel.cs:528/752 → `Battle/Settings/ResignButton`；:553/759 → `Battle/Settings/SkipTutorial`；:716/773 → `MainMenu/Settings/ButtonLabel/SelectLanguage`；:767/1089 → `SliderNameTerms`（`MainMenu/Settings/SettingLabel/*`）
- Battle/WaitBanner.cs:280 → `Battle/Mulligan/WaitEnemy` · Battle/MultiCardDisplay.cs:144 → `Battle/Mulligan/ButtonDone` · Battle/EndPanel.cs:319 → `Battle/BattleEnd/{Victory,Defeat,Draw}`
- Battle/SkillPanel.cs:301 / :315 → `Battle/HUD/TargetsAvailable`（经 `CardText.TargetsAvailable`）
- Deck/DeckRuntime.cs:962 → `MenuDeck/Filters/Filters` · :981 → `MenuDeck/Filters/ClearFilters` · :1068-1070 → `MenuShop/ShopItemType/{Cards,Cosmetics}` + `MenuDeck/HUD/DeckDescription/DeckInfo`
- Deck/DeckRuntime.cs:1186 → `MenuDeck/HUD/EditDeckName` · :1335 → `MenuDeck/MenuButtons/Done` · :1403 → `MenuDeck/Filters/Army` · :1985 → `MenuCollection/NoCardsFound`
- Deck/DeckRuntime.cs:2575 → `MenuDeck/Share/PasteDeck` · :2583/:3950 → `MenuDeck/HUD/EnterText` · :2630 → `MainMenu/General/Confirm` · :1685 → `FilterPanelModel.TitleText`（内部 `Loc.T`）
- Deck/DeckRuntime.cs:3533 / :3568 → `DeckRules.Describe(e)`（`MenuDeck/Error/*`）· Deck/DeckEditorState.cs:273 → 同上

## 四、🔴 卡片文本那条线（`Core/CardText.cs`）在 Battle/Deck/ 里被查询/显示的地方

查询点（Battle/）：`BattleDriver.cs:3584`（`Term`）· `:7169/:7185/:7208`（`Phrase("STUNNED")` / `Phrase("THIS UNIT CANNOT ACT")`）· `:7232`（`Phrase("CHOOSE ACTION")`）· `:7387-7389`（`Phrase(what)` / `Phrase("PICK A TARGET")` / `Phrase("NO LEGAL TARGET")`）· `:12574`（`TurnLabel` + `Phrase(who)`）· `:12682`（`Phrase("END TURN")`）· `:12695`（`Phrase(r)`）· `:5700/:7354/:10889/:10968`（`Name`）· `:11043-11119`（`Keyword`）· `:12608/:12610`（`Faction`）；`SkillPanel.cs:300/301/315`（`EffectSentence` / `TargetsAvailable`）；`CardDisplayWindow.cs:383`（`KeywordZh`）；`EndPanel.cs` 头注提到 `Phrase("VICTORY"/"DEFEAT")`。

查询点（Deck/）：`DeckRuntime.cs:2510/2543/2730/3368/5811`、`DeckEditorState.cs:218/400`（`Name` / `Faction`）。

**该不该换掉：不该整表换。** `CardText` 那三张表（`Phrases` / `Names` / `KeywordNames`）**已经对齐了 `Loc`**，分三层：① `Term(键)` 先过 `Loc.HasEntry` 闸再 `Loc.T`（没中文字体资产时回英文列）；② `Phrase(en)` 先查转发表 `PhraseTerms`（值 = `Loc.cs` 里的原版键）再落到 `Loc.T`，查不到才回本文件兜底表；③ 兜底表只覆盖**原版确实没有词条**的那一族（自研 26 张卡名 / 关键词 / 我们自造的短语）。⇒ 上面这些查询点**一个都不用动**；真正要做的是把「其实有原版词条、却还只躺在 `Phrases` 兜底表里」的那几条迁到 `Loc`（`CardText.cs:339-413` 的注释自己列了已迁的几条，可当迁移模板）。

## 五、没查清的部分

- **原版 `mTerm` 一律未逐条查**（按要求不去解包）。第 11–21 条（战斗日志那 11 句）**疑似**对应二进制里那一族 `Battle/Cemetery/*`（`Core/Loc.cs:615-616` 记「`Battle/Cemetery/*` 19 条」），但**没逐条对过** ⇒ 键名先按自拟，施工前建议核一次（核得到就换成原版键）。
- `Battle/Tips/*` 那 24 条（同上注释）里有没有能直接复用给提示行（第 1–9、22 条）的句子 —— **没核**。搜过：`Core/Loc.cs` 全表 + `Battle/*.cs` 里所有 `Loc.T` 调用点。
- 第 47–50 条（默认/演示卡组名）**算不算 ① 有权衡**：它们是**写进存档的数据**、又被画到屏幕上。这里按「会显示 ⇒ ①」记；若口径是「数据不翻译、显示时再翻」，那这 4 条应改成显示点翻译而不是换常量 —— **需用户裁决**。
- `Deck/DeckRuntime.cs` 的 `Say(...)` 那 20+ 句中文（`:2187 :2504 :3368 :3510 :3517 :3521 :3655 :3853 :3872 :3936 :4768 :4770 :4818-4823 :4831 :4901 :4941 :5800 :5802`）**没进 ①**：`Say` 现在**只写 `Debug.Log` + 一个自检读的状态**，屏幕上那行字已在 2026-10-17（D35）删掉（判据 = `DeckRuntime.cs:3076-3080`）⇒ 按当前代码判 **②**。⚠️ 若哪天把 notice 行加回来，这 20+ 句要补键。
- `DeckRuntime.cs:5214` / `:5427` 用 `Label.Create(..., c.Label, ...)` 直接把 `Core/FilterPanelModel.cs` 的 `"Owned only"` / `"Upgradable only"`（`:508 :833`）与 `RarityNames`（`:348`）/ `TypeLabels`（`:369`）画上屏，**这部分不过 `Loc`**；但字面量在 `Core/`（不在本次范围）⇒ **只记不动**，留给 Core 那一批。
- ② 那一列只数了 `Debug.Log*` / `Debug.Assert` 语句；各件的**自检 getter**（如 `"<无>"`、`AttackSelector.cs:675 "（收起）"`、`:678 "(灰)"`）按定义算「开发串」但**没并进 ② 的数**，落在 ③ 里 ⇒ ③/② 的分界是行级近似，不是逐条判定。
