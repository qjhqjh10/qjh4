# B94 派活 · R-A 出处补全（只读查证代理 · 第九十四会话 波 1）

> 产出人：R-A（只读）。**未改任何 `.cs` / 数据 / 账 / 正本，未跑 Unity，未动 git。**
> 数据源：`D:/4/Unity/资料/完全复刻/判据/B94_派活.tsv`（381 条 B/C）。
> 第一权威 = `D:/2/tools/decomp_full/`（25,096 个 `类__方法.c`）。

## 〇 · 范围口径（务必先读）

- **任务书原话**：「只做「判据出处」列里不含 `decomp_full/` 的行（约 48 条）」。
- **按字面过滤**（`'decomp_full/' not in 判据出处`）实际得 **216 条**，横跨 **15 个账文件**：
  `日常_奖励 53 · 战斗逻辑驱动 48 · 未分类_其他 38 · 商店 28 · 其他_基础设施 14 · 轮抽 8 · 菜单_外壳 7 · 个人资料_成就_排行 6 · 泛型与启动链_补读 4 · 战斗界面_HUD 2 · 教程_引导 2 · 特效动画 2 · 社交_聊天 2 · 锻造 1 · 音频_语音 1`。
- **`战斗逻辑驱动.tsv` 恰好 48 条** —— 与「约 48」精确吻合，且任务书点名「`PlayerState.cs` 那 7 条最急」（这 7 条全在本文件内）。⇒ **本文交付 = `战斗逻辑驱动.tsv` 的这 48 条**（战斗域 = P0 主场）。
- ⛔ **其余 168 条（14 个账文件）本文未做** —— 请调度台另派 R-A2/R-A3 按账文件切块（每块见上表条数）。这是**真实范围**，不是漏做。

## 〇·二 · 通用判据（本批全部适用）

- `.c` 是 **Ghidra 反编译**：形参常被摊平成 `param_N`，`List[0]` 取队首、`card+0x228` = `cardState`、`card+0x40` = `isPlayer`（0=敌方 / 非0=我方）、`card+0x65`=`isRemnant`、`card+0x66`=`isLeavingRemnant`、`BattleManager+0x470`=`unitsInPlay`、`+0x538`=临时 List、`+0x540/548/550`=三张复用结果 List、`+0xC0`=playerManager、`+0xC8`=enemyManager、`PlayerManager+0x28`=heroCard、`+0x30`=manaManager、`bm+0x168`=ActiveSkillDisplay、`bm+0x220`=EnviromentalEffectCardsSO、`bm+0x100`=ScenarioEnvironmentConditionsManager。
- **`.c` 首行函数名若不是自己 = 文件名与内容不符**（本批命中 1 例：row 11 `DeviceVariables`）。
- **常量值**（`DAT_1834xxxxxx`）在 `.data` 段，本批多处需 VA 读（`工具/read_literal.py` / RVA 反汇编）才能钉死浮点值 —— 凡我写「需 VA」处，即该具体数字**尚未坐实**。

---

## 一 · 逐条（48）

### 1. 账: 战斗逻辑驱动.tsv:94 (F5_表现小文件族, C) — AiScripted.ExecuteAction(BattleManager manager, ScriptedActionCampaignData action)
- **`.c`**: `AiScripted__ExecuteAction.c:1-1281`（主体是一个 86 路大 switch；内层 switch 在 `:1019`，case `0x1e/0x1f/0x23/0x24`）
- **判据结论（逐句）**：按 `action` 的类型字段分派到具体表现。已核实的落点调用：`VoiceLinesController.ShowRadioMessage`(:134) · `VoiceLinesController.DisplayWarlordChatMessage`(:237) · `BattleManager.DisplayBigChat`(:335, :621) · `VoiceLinesController.DisplayChatBox`(:384, :681) · `AiScripted.GetTipPosition`(:307, :1263) · `MinionManager.GetClosestAvailableSlot`(:868，=「部署战将/单位」那支：选最近空位再落子) · `AiScripted.GetChosenCard` · `AiScripted.EnableTutorialHighlight`(:1272) · `ScreenHighlightPosition.Toggle`(:1269)。
- **落点**: `CardPresentation/Battle/CampaignScriptRunner.cs:536`（ExecuteAction）；`:187 ICampaignScriptHost`；`:278 NullCampaignScriptHost`
- **还差什么**：账内「六分支」是**粗分组**；真实的 action-type→分支 表（含 `case 0x1e/0x1f/0x23/0x24` 与部署/高亮 toggle 支）**尚未逐条枚举**。⚠️ **「末尾 `PlaySoundAsset`」在本 `.c` 里 `grep -i sound` 0 命中** —— 要么在状态机/无名分支里，要么账记错，**需 VA 复核**。

### 2. 账: 战斗逻辑驱动.tsv:96 (F4_引擎小文件族, B) — AiScripted.GetActingCard(BattleManager, ScriptedActionData, bool acting)
- **`.c`**: `AiScripted__GetActingCard.c:18-88`
- **判据结论**：`unitType = acting ? actionData+0x14 : actionData+0x20`。映射：`10`→`GetHero(side 1)`(:26-42) · `20`→`GetHero(side 0)`(:31-42) · `30/31/32`→`GetPlayerMinionsAndRemnantInPlay`(:32-37) · `40`→`GetEnemyMinionsAndRemnantInPlay`(:72-80) · `50`→`GetFriendlyHandRef`(:46-50) · `60`→`GetEnemyHandRef`(:65-67) · 其余→`LogWarning` + 返回 0(:56-63)。随后 `GetChosenCard(ai, list, refCard)`；`refCard = acting ? actionData+0x18 : actionData+0x28`（minion 支），或固定 `actionData+0x18`（hand 支）。
- **落点**: `RuleEngine/Core/TutorialScript.cs:1705`（SideOfUnitType；等价=SideOfUnitType(:1553)+FindBoardUnit(:1580)+FindHandIndex(:1624)）
- **还差什么**：须核我们 `unitType` 枚举值是否就是 `10/20/30-32/40/50/60`、以及 refCard 用 `+0x18`（acting）/`+0x28`（target）的对应关系。

### 3. 账: 战斗逻辑驱动.tsv:103 (F3_BattleDriver, B) — AiScripted.GetTipPosition(BattleManager, PositionReference, PositionRelation, RawCardScript referenceUnit)
- **`.c`**: `AiScripted__GetTipPosition.c:44-272`
- **判据结论**：基锚 = `referenceUnit+0x210` 的 transform.position(:70-77)。`fVar9 = cardImage+0x34`（ref∈{11,12,13}∪{21,22,23} 时改 `+0x38`，:47-63）；某全局标志位为真时 `fVar9 *= 2.0`(:67-69)。switch(PositionReference=param_4)：`90(0x5a)`→**写死 (8.1, 0.5)**(:155-165)；`100`→`bm+0x168`(ActiveSkillDisplay) 的 pos，`x -= DAT_1834b3090`(:87-95)；`110(0x6e)`→`referenceUnit+0x140` pos，`x -= DAT_1834b3188`、`y += DAT_1834b2bb4`(:98-120)；`120(0x78)`→`referenceUnit+0x110` pos，`x -= DAT_1834b2bb4`(:124-146)；`70(0x46)`→`bm+0xC8`(enemyManager)`+0x30`(manaManager) pos(:150-151,:171-179)；`80(0x50)`→`bm+0xC0`(playerManager)`+0x30` pos(:168-172)；`10`→`GetHeroPos(side0)`；`11-13`→`GetSpecificHeroPos(side0)`；`20(0x14)`→`GetHeroPos(0x180000001)`；`21-23`→`GetSpecificHeroPos(0x180000001)`；`30(0x1e)`→敌兵+残骸→GetChosenCard→`Get2DPosOfCard`；`40(0x28)`→我兵+残骸→GetChosenCard→`Get2DPosOfCard`；`50(0x32)`→敌手→GetChosenCard→transform；`60(0x3c)`→我手→GetChosenCard→(IsInHand?Get2DPosOfCard:bm+0xd0)(:185-255)。最后按 PositionRelation=param_5 加偏移：`10`→`x -= fVar9`；`20`→`x += fVar9`；`30`→`y += fVar9 * K`(:260-271)。
- **落点**: `CardPresentation/Battle/BattleDriver.cs:16250`（ReticleTargetView）+ `TipAnchor`（:17032-17092）
- **还差什么**：`DAT_1834b3090 / b3188 / b2bb4 / b33f0` 的**浮点真值需 VA 读**（账内「Chat x−0.7 y+0.5」的 0.7/0.5 疑即 `b3188/b2bb4`，**未坐实**）。`70/80 = 两侧 manaManager` 已由代码坐实。

### 4. 账: 战斗逻辑驱动.tsv:172 (R-B_无落点, C) — BattleManager.AddTransformCardWithBuff(...8 参)
- **`.c`**: `BattleManager__AddTransformCardWithBuff.c:36-106`
- **判据结论**：`new BattleAction(type=0x1b(27=transformUnit), checkIfCasterInPlay, casterCard, block)`(:38)；填 `targetCard`(:39)、`transformedIntoCard`(:41)、`casterCard+0x28`(:52-54)、`buffEffect`(:56)、`keepEffects`(:58)；`AddAutoActionToQueue(bm, action, priority)`(:106)。
- **落点**: （无落点 —— `transformUnit` 机制未实现，且本 build 池内**零可达**；`RuleEngine/Core/ActionPump.cs:1477` 记录 `jamUnit/transformUnit/removeEffect` 三路零可达）
- **还差什么**：若要接，出队体在 `decomp_full/BattleManager__ResolveTransformUnit.c`（存在）；但先证「有可达生产者」再动手。

### 5. 账: 战斗逻辑驱动.tsv:196 (F3_BattleDriver, B) — BattleManager.ApplyEnvEffect(RawCardScript card, bool isPlayer)
- **`.c`**: `BattleManager__ApplyEnvEffect.c:8-21`
- **判据结论**：取该侧 `PlayerManager`（`!isPlayer`→`bm+0xC8` / `isPlayer`→`bm+0xC0`，:8-13）；若 `pm+0x28`(heroCard)≠null → `heroCard.TriggerOtherCardAbility(card)`(:14-15)；`effect = EnviromentalEffectCardsSO(bm+0x220).GetEnviromentalEffect(card)`(:17-19)；`ScenarioEnvironmentConditionsManager(bm+0x100).ApplyEnvironment(effect, instant=false)`(:20-21)。
- **落点**: `CardPresentation/Battle/BattleDriver.cs:9760`（ApplyOffensiveEnvOnce）· `:37274`（ApplyEnvWarlordAbilityOnce）
- **还差什么**：`instant` 恒 false（第 3 实参写 0）；核我们两处调用是否都传 false、且 heroCard 那一步（TriggerOtherCardAbility）没漏。

### 6. 账: 战斗逻辑驱动.tsv:248 (R-B_无落点, C) — BattleManager.CheckForHeroDeckTransform(DeckAndWarlordData, bool isPlayer)
- **`.c`**: `BattleManager__CheckForHeroDeckTransform.c:23-100`
- **判据结论**：`rawBattleData!=null` ∧ `raw+0x18`(替换卡)≠null ∧ `BoardAnalysis.DeckMeetsCriteria(raw+0x28 牌表, raw+0x18 卡)` 为真(:38-42) ⇒ 在 `PlayerDataManager.fullHeroCollection` 里 `Find` 匹配英雄(:45-56)；命中则 `matchData+0xe0`(敌)/`+0xd8`(我) = 替换卡(:57-70)，`raw+0x18 = 找到的英雄`(:71-74)，玩家侧再 `VoiceLinesController.SetTransformedWarlordNewChats`(:78-82)。
- **落点**: （无落点 —— 督军卡组变身没实现：无 `DeckMeetsCriteria`、无「换卡组」机制）
- **还差什么**：`decomp_full/BoardAnalysis__DeckMeetsCriteria.c` **存在**，需读它拿到「牌表满足替换卡」的判定；再实现「换一副督军卡组」。

### 7. 账: 战斗逻辑驱动.tsv:254 (F2_EffectResolver, B) — BattleManager.ChoiceOfCardEnemy(CardScript, CardScript, int selectedCard)
- **`.c`**: `BattleManager__ChoiceOfCardEnemy.c:18-55`
- **判据结论**：`ActiveSkillDisplay(bm+0x168).FadeOut`(:18-19)；`bVar5 = !IsNetworkedGame() && actingCard!=null && actingCard+0x40==0(敌方) && matchType != 0xa0(160)`(:20-30)；`options = GetChoiceOptions(...,bVar5)`(:31)；若 bVar5 → `sel = fullPool.IndexOf(options[0])`(:32-41)，否则 `sel = 传入 selectedCard`；`chosen = fullPool[sel]`，写 `bm+0x2e8` 并日志，**返回 sel**(:43-53)。
- **落点**: `RuleEngine/Core/EffectResolver.cs:4792`（DoChooseCard；敌方/玩家两档同实现，只有候选侧不同）
- **还差什么**：核 `GetChoiceOptions` / `GetChoiceFullPool` 的候选集差异，以及「非联机敌方 & 非 arena(160) 时自动取 options[0]」这条规则。

### 8. 账: 战斗逻辑驱动.tsv:255 (F2_EffectResolver, B) — BattleManager.ChoiceOfCardPlayer(CardScript, CardScript, int actionValue)
- **`.c`**: `BattleManager__ChoiceOfCardPlayer.c:17-61`
- **判据结论**：`ClockManager(bm+0x118).UnpauseForAnim`、`CancelActionStates`、`bm+0x290=0xe(14)`、`bm+0x298=0`(:17-21)；`VoiceLinesController(bm+0xb0).ForcePopUpClose`；`ActiveSkillDisplay.FadeOut`(:22-25)；`options=GetChoiceOptions`、`fullPool=GetChoiceFullPool`；`chosen=fullPool[actionValue]`、`idx=options.IndexOf(chosen)`(:26-33)；`TouchInputManager.Toggle`；`SetActive(bm+0x1f8=选择卡面板)`(:43-46)；`ChooseCardMenu.Setup(panel, options, actingCard+0x28→+0x38(actingCardId), ClockManager+0x70(timerCounter), idx)`(:50-55)。
- **落点**: `RuleEngine/Core/EffectResolver.cs:4792`（DoChooseCard）
- **还差什么**：与 row 7/9 同源；`Setup` 的四个实参（options / actingCardId / timerCounter / 选中下标）要与我们面板入参对齐。

### 9. 账: 战斗逻辑驱动.tsv:256 (F2_EffectResolver, B) — BattleManager.ChooseCardMethod(BattleAction, Action<int> callback)
- **`.c`**: `BattleManager__ChooseCardMethod.c:25-79`
- **判据结论**：构造 `<>f__AnonymousType7<Int32Enum>` 闭包，捕获 `BattleManager`(:28)、`BattleAction` 整份拷贝 0x100 字节 :29-75）、`callback`(:77)；返回闭包。
- **落点**: `RuleEngine/Core/EffectResolver.cs:4639`（ChooseCardCandidates；回调式 ChooseCardMethod 我们改成同步取返回值）
- **还差什么**：原版是**异步/回调闭包**，我们是同步返回；语义等价性需按「候选集 + 选中下标」比对（与 row 7/8 同一套 GetChoiceOptions/FullPool）。

### 10. 账: 战斗逻辑驱动.tsv:279 (F1_RuleCore, B) — BattleManager.DealMultiDamageAndDestroySelf(...)
- **`.c`**: `BattleManager__DealMultiDamageAndDestroySelf.c:37-110`
- **判据结论**：`new BattleAction(type=0x4b(75=doDamageAndDestroySelf), checkIfCasterInPlay, casterCard, block)`(:39)；`targetList` 拷贝进临时 List(:40-47)；**`if casterCard!=null → CardScript.SetAsUnitDestroyed(casterCard)`（先标将死，:49-50）**；`AddAutoActionToQueue(priority)`(:51-98)；**之后**才 `foreach card in targetList → CardScript.RecordPendingDamage(card, damageInfo)`(:99-110)。
- **落点**: `RuleEngine/Core/RuleCore.cs:8453`（ApplyDamage）+ `EffectResolver.cs` 的 DoDeal destroyedSelf 支
- **还差什么**：🔴 **顺序相反** —— 原版「先把施放者标将死(SetAsUnitDestroyed) → 再入队 → 再给目标记待结算伤害」；我们「先伤害后自毁」。账内【B92_B8】已记，**照原版改顺序**，别修成「看起来更合理」的版本。action type = 0x4b(75)。

### 11. 账: 战斗逻辑驱动.tsv:292 (F4_引擎小文件族, B) — BattleManager.DeviceVariables()
- **`.c`**: ⚠️ **`BattleManager__DeviceVariables.c:1-7` 内容不符** —— 首行是 `AlternateArtCard__get_CardImage`（`return *(param_1+0x50)`），**不是本方法**。⇒ 本方法 `.c` **不能当判据**（与账内【B92_B3】结论一致）。
- **判据结论**：以 ILSpy `out_cpp2il_dev_ilspy/BattleManager.cs:47933` 为准 = `return desktopVariables;`（返回 `VarsDevice` 那颗平台变量对象）。序列化真值账内已钉死（`VarsDesktop/VarsMobile.json` 九项）。
- **落点**: `RuleEngine/Core/GameplayVariables.cs:547`（For）+ `CardPresentation/Core/VarsDevice.cs:53`（VarsDevice）
- **还差什么**：`DeviceVariables` 只是「取字段」；**其 .c 被折叠成错文件 ⇒ 仍缺一条 `.c` 级证据**。要坐实需读 `dump.cs` 给 `BattleManager.DeviceVariables` 的 RVA，VA 反汇编确认 `mov rax,[rcx+offset]; ret`。其 9 项消费真值已在账内，无需重查。

### 12. 账: 战斗逻辑驱动.tsv:322 (F4_引擎小文件族, B) — BattleManager.GetAllCardsInPlay(bool includeDying)
- **`.c`**: `BattleManager__GetAllCardsInPlay.c:17-81`
- **判据结论**：清空结果 List(`bm+0x540`)与临时 List(`bm+0x538`)(:17-41)；遍历 `unitsInPlay(bm+0x470)`：`cardType∈{0x14(20 Tactic), 0x28(40 Whispers)}` **跳过**(:48-49，🔴 **不排除督军 0x0A=10**)；`IsInPlay()` 真→收；假时仅当 `includeDying && cardState(+0x228)==5` 才收(:54-71)；末尾 `AddRange` 后返回 `+0x540`(:78-81)。
- **落点**: `RuleEngine/Core/PlayerState.cs:69`（Board；等价 = 读取方逐格判，结构不同）
- **还差什么**：我们由读取方逐格过滤 ⇒ 需保证「排除 20/40、不排除督军 10、includeDying 只收 cardState==5」三档在**每个调用点**都等价，否则易漏督军或多收。

### 13. 账: 战斗逻辑驱动.tsv:344 (R-B_无落点, B) — BattleManager.GetCurrentPlayerId(bool isPlayerTurn)
- **`.c`**: `BattleManager__GetCurrentPlayerId.c:7-31`
- **判据结论**：返回**字符串**：`!isPlayerTurn`→静态 MatchData/PlayerInfo 的 `+0x40`；`isPlayerTurn`→`+0x38`（:15-31）。**唯一调用点只打一句日志**。
- **落点**: （无落点）
- **还差什么**：我们无「playerId 字符串」概念，该值仅用于日志 ⇒ 等价物可只留日志字符串，无需建模。

### 14. 账: 战斗逻辑驱动.tsv:346 (F1_RuleCore, B) — BattleManager.GetDeathType(BattleAction, DamageInfo)
- **`.c`**: `BattleManager__GetDeathType.c:36-311`（决策在 :258-311；另 :145/:254 有提前 return）
- **判据结论**：`damageInfo==null`→LogError(:36-38)；若伤害来源是 spell（`SupportMethods.IsSpellType(cardType)`）→`return 0x1e(30)`(:105-112)；否则 `uVar1 = damageInfo+0x1c`：`uVar1==8`→（攻击者`+0x40` == `IsPlayerTurn()` ? `return 10` : `return 0xf(15)`）(:114-225)；`uVar1∈{3,7,12}`(位掩码 `0x1088`)→`return 0x14(20)`(:227-229)；默认：`uVar16=0`，若 `battleAction[0]==0x2d(45)`→取动作结构里某值(:414-455)，返回 `uVar16`。
- **落点**: `RuleEngine/Core/RuleCore.cs:9481`（DeathTypeOfSource）
- **还差什么**：**枚举名映射未定**（10/15/20/30 各是哪个 DeathType？`damageInfo+0x1c` 是哪种来源枚举？`0x2d(45)` 动作何义？）⇒ 需查 `dump.cs` 的 `DeathType` / 伤害来源枚举定义，再对我们 `DeathTypeOfSource` 逐档对齐。

### 15. 账: 战斗逻辑驱动.tsv:348 (F4_引擎小文件族, B) — BattleManager.GetDeckManager(bool playerHeroY)
- **`.c`**: `BattleManager__GetDeckManager.c:6-9`
- **判据结论**：`if playerHeroY != 0 → return *(bm+0xf0)` 否则 `return *(bm+0x108)`（返回 `DeckManager` 组件引用）。
- **落点**: `RuleEngine/Core/PlayerState.cs:19`（Deck；我们没有 DeckManager 这一层，牌库本身就是 `List<CardInstance>`）
- **还差什么**：结构不同（无 DeckManager）；仅需保证「我方/敌方牌库」两个引用可取到。

### 16. 账: 战斗逻辑驱动.tsv:350 (F4_引擎小文件族, B) — BattleManager.GetEnemyMinionsAndRemnantInPlay()
- **`.c`**: `BattleManager__GetEnemyMinionsAndRemnantInPlay.c:17-73`
- **判据结论**：遍历 `unitsInPlay(bm+0x470)`：`card+0x40==0`(敌方) ∧ `cardType∉{10(督军),0x14(20),0x28(40)}`(:48-51) ∧ `IsInPlay()` 真(:56-57) → 收进 `bm+0x548`；返回 `+0x548`。
- **落点**: `RuleEngine/Core/PlayerState.cs:69`（Board + IsRemnant 判）
- **还差什么**：注意此路 **排除督军(10)**（与 row 12 的 GetAllCardsInPlay **不同**）；确认我们「敌方在场含残骸」一致排除督军。

### 17. 账: 战斗逻辑驱动.tsv:364 (F1_RuleCore, B) — BattleManager.GetMulliganCard(bool isPlayer)
- **`.c`**: `BattleManager__GetMulliganCard.c:18-50`
- **判据结论**：取列表 `bm+0x310`(isPlayer) / `bm+0x318`(敌)(:18-23)；`Count>1` 时把 `Item[1]` 的 GameObject `SetActive(true)`（点亮下一张，:25-31）；`Count<1`→LogError 返回 0(:32-40)；否则取 `Item[0]`、`SetActive(true)`、`RemoveAt(0)` 并返回它(:41-49)。⚠️ **=「从视图列表弹队首卡并预先点亮下一张」**，账内原写「取那张要换的卡」方向对但**不完整**。
- **落点**: `RuleEngine/Core/RuleCore.cs:444`（Mulligan；原版是「取那张」，我们由调用方给下标）
- **还差什么**：核我们换牌是否复刻「弹队首 + 预点亮下一张」这套 view 序；若不做 view，至少语义（谁被换）一致。

### 18. 账: 战斗逻辑驱动.tsv:371 (F4_引擎小文件族, B) — BattleManager.GetPlayerMinionsAndRemnantInPlay()
- **`.c`**: `BattleManager__GetPlayerMinionsAndRemnantInPlay.c:17-73`
- **判据结论**：同 row 16，但 `card+0x40 != 0`(我方)(:48) ∧ `cardType∉{10,20,40}` ∧ `IsInPlay()` → 收进 `bm+0x550`。
- **落点**: `RuleEngine/Core/PlayerState.cs:69`（Board + IsRemnant）
- **还差什么**：同 row 16（排除督军 10）。

### 19. 账: 战斗逻辑驱动.tsv:372 (R-B_无落点, C) — BattleManager.GetPremulliganScriptedData()
- **`.c`**: `BattleManager__GetPremulliganScriptedData.c:12-41`
- **判据结论**：`stage=GetCurrentTutorialStage()`；gate：`stage≠null` ∧ `pma(=stage+0x50)≠null` ∧ `pma+0x18(scriptedTurn)!=0` ∧ `pma+0x20(list)≠null` ∧ `Count>=1`(:16-37) ⇒ 返回 `stage+0x50`(:38-40)。
- **落点**: （无落点 —— 我们只有数据字段 `TutorialStageData.preMulligan`（`RuleEngine/Core/TutorialScript.cs:348`），6 关 `scriptedTurn` 全 false ⇒ 原版这一支恒不跑）
- **还差什么**：无执行代码；若要完全复刻，需补谓词 + 执行体，但**当前数据全 false = 休眠**。

### 20. 账: 战斗逻辑驱动.tsv:381 (F5_表现小文件族, B) — BattleManager.GetTargetInPos(Vector3 worldSpaceMousePosition)
- **`.c`**: `BattleManager__GetTargetInPos.c:51-268`
- **判据结论**：从 **hudCamera(`bm+0xa0`)** 起，用 `Physics2D.GetRayIntersectionNonAlloc`（2D 射线相交，:104）先打；命中则取 `RaycastHit2D` 的 transform；再走 **3D `Physics.RaycastNonAlloc`（`bm+0xa8`/`+0x348`）**(:248)。返回命中的 transform（场上对象）。
- **落点**: `CardPresentation/Battle/TargetReticle.cs:511`（ResolveAim；候选等价 = 屏幕射线与平面求交，但原版从 **hudCamera** 出射线、我们从 **boardCamera 屏幕点** 出 ⇒ 方向源不同）
- **还差什么**：**方向源不同**要改（hudCamera vs boardCamera）；层掩码/距离常量（`DAT_1834b2f44 / b2c70 / b3040 / b3258 / b2bc8`）**需 VA 读**。

### 21. 账: 战斗逻辑驱动.tsv:385 (F4_引擎小文件族, B) — BattleManager.GetUnitsInPlayList(ref List, bool clearList, 8 个 incl 开关)
- **`.c`**: `BattleManager__GetUnitsInPlayList.c:17-33`
- **判据结论**：`clearList` 真→清空 ref List(:17-26)；调 `GetUnitsInPlayPooledList(...)`(:28-29)；`AddRange` 进 ref List(:30-32)。
- **落点**: `RuleEngine/Core/PlayerState.cs:172`（Units；原版 ref List + clearList，我们返回 IEnumerable ⇒ 结构不同）
- **还差什么**：语义等价即可（= row 23 的过滤 + 写进调用方给的表）。

### 22. 账: 战斗逻辑驱动.tsv:386 (F4_引擎小文件族, B) — BattleManager.GetUnitsInPlayNewList(8 个 incl 开关)
- **`.c`**: `BattleManager__GetUnitsInPlayNewList.c:16-30`
- **判据结论**：`new List`(:16-17)；`GetUnitsInPlayPooledList(...)`(:23-24)；`AddRange`(:26)；返回新表(:27)。
- **落点**: `RuleEngine/Core/PlayerState.cs:172`（Units；需要新表时调用方 `.ToList()`）
- **还差什么**：语义等价（= row 23 过滤 + 新表）。

### 23. 账: 战斗逻辑驱动.tsv:387 (F4_引擎小文件族, B) — BattleManager.GetUnitsInPlayPooledList(8 个 incl 开关)
- **`.c`**: `BattleManager__GetUnitsInPlayPooledList.c:21-95`
- **判据结论（核心过滤）**：遍历 `unitsInPlay(bm+0x470)` 收进临时 `+0x538`：`inclPlayer(param_7)`=假则跳过 `card+0x40==0`(敌)(:39-42) · `inclEnemy(param_8)`=假则跳过 `card+0x40==0`(敌)(:45-47) · `inclHero(param_2)`=假则跳过 `cardType==10`(:49-53) · `inclMinions(param_3)`=假则跳过 `cardType==0`(:54-58) · **恒跳过 `cardType==0x14/0x28`**(:60-61) · `inclRemnants(param_6)`=假则跳过 `card+0x65!=0`(残骸)(:62) · `IsInPlay()`/`includeDying && cardState==5`(:67-85)。⚠️ **`inclHeroParts(param_4)` / `inclWeapons(param_5)` 在本 `.c` 体里未见独立判断**（Ghidra 把两 bool 塞进 `undefined8` 形参或优化掉）。
- **落点**: `RuleEngine/Core/PlayerState.cs:172`（Units；原版池化复用，我们不做池化）
- **还差什么**：🔴 **heroParts / weapons 两档的过滤条件在本 `.c` 中读不到** ⇒ 需 VA 反汇编或 `out_il2csharp` 复核（否则「含/不含英灵部件、含/不含武器」会静默错一档）。

### 24. 账: 战斗逻辑驱动.tsv:393 (R-B_无落点, C) — BattleManager.GetWarcry(bool isPlayer)
- **`.c`**: `BattleManager__GetWarcry.c:17-80`
- **判据结论**：取静态 MatchData 的 `+0x50`(敌) / `+0x58`(我) 的 warcry 项 → `PlayerDataManager.GetCosmeticItem(那项+0x60=uniqueId)`(:59-64)；未命中 → `GetTranslation("Chat_Messages/warcry-default")`(:65-70)；命中 → `GetTranslation(Concat(前缀, cosmeticItem+0x38))`(:72-78)（前缀 `DAT_1842d5e30` 疑为 `Chat_Messages/warcry-`）。返回 void（喂 UI）。
- **落点**: `RuleEngine/Core/DeckRules.cs:1057`（WarcryId）· `CardPresentation/Core/Loc.cs:3934`（WarcryKey）· `:3942`（Warcry）
- **还差什么**：`BattleDriver.PlayerBattleMeta.Warcry` 那一格**仍恒空 + 出声**（属 F3 所有权）；取文函数已落地。前缀常量值需确认。

### 25. 账: 战斗逻辑驱动.tsv:422 (R-B_无落点, C) — BattleManager.IsPremulliganScriptAvailable()
- **`.c`**: `BattleManager__IsPremulliganScriptAvailable.c:12-32`
- **判据结论**：`stage≠null` ∧ `pma+0x18(scriptedTurn)!=0` ∧ `pma+0x20!=null` ∧ `Count>=1` ⇒ true，否则 false。
- **落点**: （无落点 —— 只有字段 `TutorialStageData.preMulligan`，无谓词/执行；数据 6 关全 false ⇒ 恒假）
- **还差什么**：补谓词（成本低）；无执行则无害。

### 26. 账: 战斗逻辑驱动.tsv:424 (R-B_无落点, B) — BattleManager.IsScriptedStageAvailable()
- **`.c`**: `BattleManager__IsScriptedStageAvailable.c:6-15`
- **判据结论**：`return IsTutorialStageAvailable() || IsCampaignStageAvailable();`（教程有 → 1；否则返回战役那半）。
- **落点**: （无落点 —— 教程那半有等价物（`DriveTutorialScript` 首行早退），**战役那半我们没实现**）
- **还差什么**：战役 half 未实现；唯一消费者 `UnitOnBoardAttackTypeSelector.cs:206`（账内【B92_B3】）。

### 27. 账: 战斗逻辑驱动.tsv:439 (F3_BattleDriver, C) — BattleManager.LeaveBattle()
- **`.c`**: `BattleManager__LeaveBattle.c:38-90`
- **判据结论**：若 `BattleNetworkManager` 实例的 `+0x4c != 100` → LogWarning + `LeaveBattleRoom()`(:38-47)；`ReleaseCardbacks()`(:48)；若 PlayerDataManager：`FirstTwoTutorialCompleted()` 真 → `EverguildSceneManager.LoadScene(主菜单)` return(:52-60)；假 → `TutorialManager.SetupTutorialMatch` → `MatchMakerManager.StartMatch(...)`(:61-89)。
- **落点**: `CardPresentation/Battle/BattleDriver.cs:12778`（LeaveBattle）
- **还差什么**：**两条支未复刻**：① 联机 `LeaveBattleRoom`；② 「前两关教程未完成 → 重开教程局」分支。目前只做「卡背/解绑/回主菜单」。

### 28. 账: 战斗逻辑驱动.tsv:455 (R-B_无落点, C) — BattleManager.PayCardPlayedCostOath(CardScript card)
- **`.c`**: `BattleManager__PayCardPlayedCostOath.c:10-24`
- **判据结论**：`if card.HasCurrentTrait(0x4fb=1275 oath)`(:11)：取该侧 `PlayerManager`（`card+0x40==0`→`bm+0xC8`(敌) / 否则 `bm+0xC0`(我)，:13-18）；`mana=GetCurrentTraitValue(card,0x4fb)`(:19)；`PlayerManager.UseMana(pm, mana)`(:22)；`manaLeft=BoardAnalysis.GetManaLeft(bm, card+0x40)`(:23)；`CheckIfCodexTriggered(bm, card, manaLeft, ...)`(:24)。
- **落点**: （无 —— 打出链只扣卡费 SpendNormalMana；OathOps 只在棋盘单位的誓约按钮上被消费）
- **还差什么**：**打出时扣誓约费**这条未接；需在打出链补 `HasCurrentTrait(oath) → UseMana(GetCurrentTraitValue(oath))` + Codex 触发。

### 29. 账: 战斗逻辑驱动.tsv:517 (F2_EffectResolver, B) — BattleManager.ResolveFollowAfterResolution(BattleAction)
- **`.c`**: `BattleManager__ResolveFollowAfterResolution.c:85-380`
- **判据结论**：`actingCard(lStack_200)==null` → LogError + `FinishResolvingAction`(:86-137)；否则 log；若第二张卡(`local_1f8`)≠null 再 log；`actingCard.FollowUpResolutionPlayed(local_f8 = battleAction.targetCard)`(:380)；`FinishResolvingAction`(:426)。
- **落点**: `RuleEngine/Core/EffectResolver.cs:7919`（EmitFollowUpResolution）
- **还差什么**：账内【B92_B8】已把「第二个实参 = targetCard」VA 坐实；本 `.c` 与之一致（`FollowUpResolutionPlayed(actingCard, targetCard)`）。无需再查。

### 30. 账: 战斗逻辑驱动.tsv:553 (F1_RuleCore, B) — BattleManager.ResolveTriggerMob(BattleAction, bool isAdditionalTriggerMob)
- **`.c`**: `BattleManager__ResolveTriggerMob.c:29-355`
- **判据结论**：`if action 卡(param_2[2]) != 0`(:29)：若**不在场/不在将死**（`IsInPlayOrDying` 假）→ 直接 `FinishResolvingAction`(:29-80)；否则 `card+0x28`.`RawCardScript.OnTrigger(300, bm, ..., card)`(:217-218)；`if isAdditionalTriggerMob==0` → `BattleManagerSupport.BroadcastUnitMob(card, ...)`(:219-306)；`FinishResolvingAction`。⇒ **trigger id = 300(0x12c)**；BroadcastUnitMob 仅当**非追加**触发时发。
- **落点**: `RuleEngine/Core/RuleCore.cs:12496`（FireTriggerAt(Mob)）
- **还差什么**：核我们 FireTriggerAt(Mob) 用的是 trigger 300、且「不在场/不在将死 → 只 Finish 不触发」「isAdditional 闸住 BroadcastUnitMob」两档一致。

### 31. 账: 战斗逻辑驱动.tsv:556 (F1_RuleCore, B) — BattleManager.ResolveTriggerRegiment(BattleAction)
- **`.c`**: `BattleManager__ResolveTriggerRegiment.c:77-273`
- **判据结论**：`if 被指卡(local_f8)!=0`(:77)：log cardName；若 `IsInPlayOrDying` 真 → `card.TriggerRegiment()`(:225-226，内部 `OnTrigger(301,this,this,this)`)；`FinishResolvingAction`(:273)；若**不在场/不在将死** → 只 `FinishResolvingAction`(:130-176 那支)。
- **落点**: `RuleEngine/Core/RuleCore.cs:12540`（FireTriggerAt）· 原语
- **还差什么**：核「不在场/不在将死 → 不触发」这档；TriggerRegiment 的 trigger id = 301(0x12d)。

### 32. 账: 战斗逻辑驱动.tsv:573 (R-B_无落点, C) — BattleManager.SetNextCardAsDeckDrawing(bool isPlayer)
- **`.c`**: `BattleManager__SetNextCardAsDeckDrawing.c:14-26`
- **判据结论**：取牌库 `bm+0x310`(我) / `bm+0x318`(敌)(:14-19)；遍历找**第一张 `cardState(+0x228)==0`(inDeck)** 的牌(:22-31) → `card.SetAsDeckDrawing()`(:32-35)（`inDeck → inDeckDrawing(16)`，让视图把「下一张要抽的牌」翻出来）。
- **落点**: （无落点 —— 我们没 `inDeckDrawing` 这个呈现态）
- **还差什么**：缺呈现态 `inDeckDrawing(16)`；若要复刻需补该状态与视图翻转。

### 33. 账: 战斗逻辑驱动.tsv:603 (F1_RuleCore, B) — BattleManager.ShuffleDeck(List<CardScript>, bool checkStratagem)
- **`.c`**: `BattleManager__ShuffleDeck.c:17-64`
- **判据结论**：牌库 null/空 → LogError 返回(:18-21)；否则 `SupportMethods.ShuffleCardList(deck, <静态>, checkStratagem)`(:38)；对返回表 `[0]` `SetActive(true)+SetupInDeck`、`[1..]` `SetActive(false)+SetupInDeck`(:39-65)（= 洗牌 + 只露牌库顶）。
- **落点**: `RuleEngine/Core/RuleCore.cs:1192`（Shuffle；Fisher–Yates + ctx.Rng）
- **还差什么**：**stratum 分档在 `SupportMethods__ShuffleCardList.c`**（本方法只把 `checkStratagem` 透传）⇒ 要复刻分档洗牌须读那个 `.c`；我方是纯 Fisher–Yates（结构不同）。

### 34. 账: 战斗逻辑驱动.tsv:661 (F1_RuleCore, B) — BattleManagerSupport.BroadcastUnitRegiment(...)
- **`.c`**: `BattleManagerSupport__BroadcastUnitRegiment.c:19-64` ⚠️ **签名只有 2 个可见形参**（`manager, 一张卡`），而 ILSpy 是 3 个（manager, unitRegiment, unitRegimentCaster）—— Ghidra 吞了一参。
- **判据结论**：遍历 `unitsInPlay(bm+0x470)`：`IsInPlay()` ∧ `card!=null` → `card.TriggerOtherCardRegiment()`(:24-39)；再 `GetCardsInHandRef(当前行动方)` 每张 → `TriggerOtherCardRegiment()`(:41-49)；再 `GetCardsInHandRef(另一方)` 每张 → `TriggerOtherCardRegiment(casterArg)`(:50-63)。⚠️ **`.c` 体里只做 `IsInPlay && card!=null`，未见与 caster 比较** —— 账内「棋盘 … ∧ != caster」的 caster 排除**可能在 `TriggerOtherCardRegiment` 内**，需回源确认。
- **落点**: `RuleEngine/Core/RuleCore.cs:12540`（FireTriggerAt(Regiment)）+ `RuleEngine/Core/EffectResolver.cs:7266`（case KeywordTable.Regiment → OtherCardRegiment）
- **还差什么**：① 第 3 个形参到底是哪张（caster）—— VA/`out_il2csharp` 确认；② 「!= caster」的排除点在哪一层。

### 35. 账: 战斗逻辑驱动.tsv:804 (R-B_无落点, C) — CardScript.MinionPlayedIntoField(delay, unitInPlayCounter, doTransformAnim, keepStatus, shouldTriggerSummon)
- **`.c`**：工厂 `CardScript__MinionPlayedIntoField.c:13-23`（造闭包，捕获 5 个形参）+ 真体 `CardScript._MinionPlayedIntoField_d__314__MoveNext.c:30-250`（256 行状态机）
- **判据结论**：状态机：`keepStatus(param_1+0x20)` 假 → `CardScript.LandOnGround`(:36-37)；写 `card+0x2e0`/`+0x2c4/+0x2cc` 并 `WaitForSeconds`(:52,:75,:124)；`UpdateMinionInPlayPosition`(:143)；`cardState(+0x228)=2`(:165)；UI`+0xd8` GameObject `SetActive(false)`(:177)；`ActivateTraitsOnSummonOrEnchantment`(:179)；`UpdateSummonSicknessAnims`(:180)；`keepStatus` 假 → LandOnGround + 调召唤消息委托 `(*(lVar9+0x18))(lVar9+0x40, card, lVar9+0x28)`(:209)；随后 `AssetReference.Load` + `Instantiate`「召唤延迟动画」到卡位(:212-245)。
- **落点**: （无）
- **还差什么**：整段是**表现协程**，未实现；若要接需按状态机逐档复刻（含 keepStatus / doTransformAnim / shouldTriggerSummon 三开关）。

### 36. 账: 战斗逻辑驱动.tsv:832 (F2_EffectResolver, B) — CardScript.ReactToUnitJammed(CardScript disenchantedCard)
- **`.c`**: `CardScript__ReactToUnitJammed.c:53-196`
- **判据结论**：**分支一**：`disenchantedCard != this` → `RemoveExtrinsicEffectsFrom(this, disenchantedCard)` + `UpdateWhileInPlay(this)` 返回(:53-58)。**分支二**（自己是被 jam 的）：倒序删光 `activeEffects(+0x108)` 每条 `RemoveEffect(effect,1)`(:59-69)；把 `traits(+0x128)` 的 `+0x18`(trait id) 收集成 List 后逐条 `RemoveTrait`(:72-123)；`this+0x55=0`、`this+0x57=0`(:125-126)；`new CardTraitData(buffType=0x82=130)` 加进 traits(:127-150)；`if cardType!=10 → this+0x220=0`（压主动技能，:151-154）；UI 刷新（StopAnimsInPlay/CleanStatusAnims/UpdateTraitIcons/UpdateHealthText/UpdateAttackText/cost SetText，:155-175）；若 `IsUnit` → `CheckIfDead`(:176-179)；`DisplayTriggerAnim(0x82)`(:181-182)。
- **落点**: `RuleEngine/Core/EffectResolver.cs:16068`（ResolveJamUnit）· 已接 `:16144`（CancelEffectsFromDisappearingUnit 调用）
- **还差什么**：核「挂 buffType 130 的 jam trait」「`+0x55/+0x57` 两标志」「非督军 `+0x220` 压主动技能」三档是否都到位。

### 37. 账: 战斗逻辑驱动.tsv:893 (R-B_无落点, B) — CardScript.SetCardState(CardStateOptions newState)
- **`.c`**: `CardScript__SetCardState.c:17-24`
- **判据结论**：先 log（`<枚举名> <卡名>`，:17-23），再 `*(card+0x228) = newState`(:24)。**18 档状态机唯一写入口**。
- **落点**: 查不到（我们的视图不存 `cardState`：由「在哪张表/哪个布局」直接驱动）
- **还差什么**：结构不同；若要机检「cardState」，需先建这一个人成员。

### 38. 账: 战斗逻辑驱动.tsv:921 (F5_表现小文件族, C) — CardScript.ShowCardBeingDrawn(float stepTime)
- **`.c`**: `CardScript__ShowCardBeingDrawn.c:9-14`
- **判据结论**：`card+0x228 = 9`(beingDrawn)(:9)；若 `UI(+0x290)` → `SetSortingLayer`(:10-11)；`if card+0x40==0`(敌方) → `TurnCardAround(stepTime, stepTime)`(:12-13)。⇒ **时长由调用方传**（这里只透传 stepTime）。
- **落点**: `CardPresentation/Core/CardFeel.cs:3770`（ShowCardBeingDrawn(card,stepTime,isPlayer,…)）；两档常量 `:3544`（弃牌 0.2 / 偷牌 0.4）
- **还差什么**：0.2/0.4 是**调用方**给的（`_ResolveDiscardFromHand` 0.2 · `_ResolveStealFromDeck` 0.4），已确认；只需保证我方两档不统一。

### 39. 账: 战斗逻辑驱动.tsv:996 (R-B_无落点, B) — CardTraitCollection.Init()
- **`.c`**: `CardTraitCollection__Init.c:27-53`
- **判据结论**：`new <集合>`(:27-28) 存进静态单例（`**(DAT_1842d51f8+0xb8)`，:29-30）；若 `this+0x18`(序列化列表)≠null → 遍历每项 `FUN_181750200(单例, item+0x18, item+0x20)`（= 用 `(item+0x18, item+0x20)` 键值对建查找表，:31-53）。
- **落点**: （无落点）
- **还差什么**：`FUN_18173d1a0 / FUN_181750200` 未命名 ⇒ 无法从 `.c` 断定具体是哪几张字典；需 `out_il2csharp` 或 VA 定性。

### 40. 账: 战斗逻辑驱动.tsv:1041 (F4_引擎小文件族, B) — EntityScript.HasCurrentTraitActiveEffect(DefinedTrait trait)
- **`.c`**: `EntityScript__HasCurrentTraitActiveEffect.c:15-38`
- **判据结论**：门 = `card+0x65(isRemnant)==0` **或** `card+0x66(isLeavingRemnant)!=0`（注意是**或**，:15）；否则扫 `activeEffects(+0x108)`：元素 `trait(+0x3c)==参数` ∧ `buffType(+0x38)∈{3(addTrait), 0x5a=90(addTraitRemnant)}` → return true(:24-38)。
- **落点**: `RuleEngine/Core/UnitState.cs:3444`（AnyEffectGrantsTrait；带残骸闸 `IsRemnant && !IsLeavingRemnant ⇒ false`）· 接线 `:3029`（RemoveTraitEffect）
- **还差什么**：核我们的残骸闸是「或」还是「且」（原版 = 或）。

### 41. 账: 战斗逻辑驱动.tsv:1053 (F1_RuleCore, B) — EntityScript.RevealedSecret(RawCardScript enchantment)
- **`.c`**: `EntityScript__RevealedSecret.c:17-60`
- **判据结论**：`if HasSecretOrders(this) < 1 → return`(:17-19)；扫 `activeEffects(+0x108)`：`IsEnchantment(effect)` ∧ `effect+0x88 == enchantment` ∧ `effect+0x90(已揭示?)!=0` → **把 `effect+0x90=0`** 并 return(:40-60)；无匹配 → LogError。
- **落点**: `RuleEngine/Core/RuleCore.cs:8129`（RevealSecretOrders；等价 = 揭示动作本身；⚠️ 我们没建「已揭示」这一位 `+0x90` ⇒ 只到「揭示时顺手清」）
- **还差什么**：缺「每条密令的已揭示位」；若要逐档复刻需加该位。

### 42. 账: 战斗逻辑驱动.tsv:1079 (F3_BattleDriver, B) — MatchData.SetBotOpponent(CardDeck enemyDeck)
- **`.c`**: `MatchData__SetBotOpponent.c:25-161`
- **判据结论**：`matchData+0x40 = 本地玩家信息`(:25)；matchType 重映射 `{10→20, 110→120, 170→180, 200→210}`(:27-41)；`enemyDeck!=null` → `new DeckAndWarlordData(enemyDeck)`→`+0x50`(:42-48)；否则 `GetBotDeck(matchType)`(:50)+`new DeckAndWarlordData(botDeck,pdm)`→`+0x50`，且 `matchType∉{100,140}` 时 `ShuffleCardLibrary`(:68-79)；随后（配置开关）随机 bot 名(`NameGenerator.GenerateName`, :91-105)、`AIBotsConfig.GetRandomAvatar/Title/Cardback`(:120-151)+`SetBotDeckRandomCardLevels`；`mode(+0x18)==7` 时拷 `+0x88`(:155-162)。
- **落点**: `CardPresentation/Battle/BattleDriver.cs:4514`（BeginFromDeckLibrary）+ `BattleContext.BotOpponentName`（BeginFromPendingCore :4033 写 `BotSeat`/`MatchTypes.Effective`）
- **还差什么**：**matchType 重映射表 + AIBotsConfig 随机 头像/头衔/卡背/等级**未复刻（部分属服务器/配置下发，按 §18·0 判定）。

### 43. 账: 战斗逻辑驱动.tsv:1080 (F3_BattleDriver, B) — MatchData.SetBotOpponentDeck(PrebuiltDeck botDeck, PlayerDataManager)
- **`.c`**: `MatchData__SetBotOpponentDeck.c:12-22`
- **判据结论**：`botDeck!=null` → log(:13-14)；`new DeckAndWarlordData(botDeck, pdm)`→`matchData+0x50`(:15-18)；`matchType∉{100,140}` 时 `ShuffleCardLibrary`(:19-22)。
- **落点**: `CardPresentation/Battle/BattleDriver.cs:4514`（BeginFromDeckLibrary）+ `:4744`（BuildTutorialDeck）
- **还差什么**：核我们「预组/tutorial 装对手牌」时，**matchType 100/140 不洗牌**这条是否复刻。

### 44. 账: 战斗逻辑驱动.tsv:1081 (F5_表现小文件族, B) — MatchData.SetMatchRecording(BattleRecordData, PlayerDataManager)
- **`.c`**: `MatchData__SetMatchRecording.c:17-46`
- **判据结论**：🔴 **账内判据要点过时** —— 不是「本局是否录」的 bool 开关，而是**从 `BattleRecordData` 重建整份 MatchData**：拷 `+0x18/+0x20/+0x38/+0x40/+0x48/+0x4c/+0x50/+0x58/+0x28/+0x30/+0x60/+0x68/+0x10`(:18-46)，建两副 `DeckAndWarlordData` 到 `+0x50/+0x58`(:22-31)，再 `GameModes.GetActiveEvent` 取联赛 → `+0xa8`(:50-77)。
- **落点**: `CardPresentation/Battle/ReplayStore.cs:377`（ReplayAvailable）
- **还差什么**：**这是「从录像数据恢复整份对局状态」的路径**，与我方「动作流录像」结构不同；账内「本局是否记录像」的描述**应订正**为「由 BattleRecordData 重建 MatchData」。（若我们要支持「载入一局录像完整回放」，需按此重建。）

### 45. 账: 战斗逻辑驱动.tsv:1165 (F1_RuleCore, B) — PlayerHand.GetMulliganCardsToDiscard()
- **`.c`**: `PlayerHand__GetMulliganCardsToDiscard.c:18-34`
- **判据结论**：`new List`；遍历 `this+0x58`：`cardState(+0x228)==0xe(14 inMulliganDiscarded)` → 收(:18-34)；返回。⚠️ 全树 0 调用点（真在跑的是兄弟 `GetMulliganCardsToDiscardIds`）。
- **落点**: `RuleEngine/Core/RuleCore.cs:444`（Mulligan）+ `MulliganCollectIndices:14184`
- **还差什么**：语义等价（收 cardState==14 那一批）；0 调用点 ⇒ 无需专门复刻。

### 46. 账: 战斗逻辑驱动.tsv:1279 (R-B_无落点, B) — RawCardScript.PreloadAssets()
- **`.c`**: `RawCardScript__PreloadAssets.c:52-130`
- **判据结论**：① `this+0xd0`(AssetReference) 若 `RuntimeKeyIsValid` → `Load` → `AnimInfo.PreloadAssets`(:52-61)；② 若 `this+0x290`(cardAbilities)≠null → 每个 ability 的 `+0x50`(abilityLogic 列表) 每条 logic 的动画 AssetReference 若 valid → `Load` → `AnimInfo.PreloadAssets`(:62-137)。⇒ 三类引用（自身 + 每条 ability 的 logic 动画）。
- **落点**: （无对应物 —— 本仓运行时 0 处 Addressables；接入后按 `.c` 三类逐条照抄）
- **还差什么**：无 Addressables ⇒ 当前不该有落点。

### 47. 账: 战斗逻辑驱动.tsv:1280 (F5_表现小文件族, C) — RawCardScript.ReleaseFullHeroSprite()
- **`.c`**: `RawCardScript__ReleaseFullHeroSprite.c:15-43`
- **判据结论**：取 `this+0x110`（或另一路 `this+0x210`）的 AssetReference(:21-38)；若 `RuntimeKeyIsValid`（虚调用 `+0x248`）→ `AssetReferenceTyped.Release(1)`(:42-43)。= **释放督军立绘的 Addressable**。
- **落点**: `CardPresentation/Core/CardArt.cs:1341`（ReleaseFullHeroSprite —— 旧记 1348 已漂，现读 1341）
- **还差什么**：我方是**缓存驱逐**（非 Addressables 释放）；结构不同。两候选字段（`+0x110`/`+0x210`）的语义需按 `RawCardScript` 字段表确认。

### 48. 账: 战斗逻辑驱动.tsv:1367 (S4_其它小域, C) — WarlordMasteryContainer.OnInitialize(PlayerInfo context)
- **`.c`**: `WarlordMasteryContainer__OnInitialize.c:38-129`
- **判据结论**：标题 Text(`this+0x40+0x20`) = `GetTranslation(PlayerProfile/HighestWarlordMastery)`(:41-49)；`victoryCounters = PlayerInfo.GetVictoryCounters(context)`(:41)；惰性建 comparator(:53-62)；`Enumerable.FirstOrDefault(context.Logs(+0x80), predicate)`(:74-77) → 命中取该条 `+0x40`(card id)，否则 `VictoryCounter.HighestCardId(vc)`(:78-80)；`AssetLocator.GetAsset(cardId)`→RawCardScript，缺则 fallback `UM47`(:82-85)；`count = Math.Max(vc+0x10, 1)`(:87-91)；有 asset → `image.sprite = fullHeroSprite`、`name = GetLocalizedCardName`(:115-128)；等级文本 = `Format(PlayerProfile/VictoriesDisplay, count)`(:97-109)。
- **落点**: `CardPresentation/Shell/SaveCenter.cs:156/263`（Get/Set）+ `Shell/SaveKeys.cs:193/289`（VictoryCounters=数据 key 44）· 页面格 `Shell/ProfileTab.cs:1219`（现只摆结构留空）
- **还差什么**：页面格**只建了结构、留空** ⇒ 需按上面顺序接线（标题 / 最高项 / 立绘 / 名字 / 等级文本 / UM47 fallback）。

---

## 二 · 「可直接转给写手」汇总（48 条）

| 写手组 | 条数 | 账行号（战斗逻辑驱动.tsv） |
|---|---|---|
| **F1_RuleCore** | **9** | 279, 346, 364, 553, 556, 603, 661, 1053, 1165 |
| **F4_引擎小文件族** | **10** | 96, 292, 322, 348, 350, 371, 385, 386, 387, 1041 |
| **F5_表现小文件族** | **5** | 94, 381, 921, 1081, 1280 |
| **F3_BattleDriver** | **5** | 103, 196, 439, 1079, 1080 |
| **F2_EffectResolver** | **5** | 254, 255, 256, 517, 832 |
| **S4_其它小域** | **1** | 1367 |
| **R-B_无落点** | **13** | 172, 248, 344, 372, 393, 422, 424, 455, 573, 804, 893, 996, 1279 |
| **合计** | **48** | —— |

### 二·一 · 最急 / 最需先动的 5 条（按影响面）

1. **row 10（:279 DealMultiDamageAndDestroySelf）** —— 顺序相反（原版先 `SetAsUnitDestroyed` 后记伤害）；引擎级，改要小心并配断言。
2. **row 23（:387 GetUnitsInPlayPooledList）** —— `inclHeroParts/inclWeapons` 两档在 `.c` 读不到，**有静默漏档风险**，需 VA/`out_il2csharp` 补。
3. **row 34（:661 BroadcastUnitRegiment）** —— 第 3 形参被 Ghidra 吞、`!= caster` 排除点未定位，容易做错 caster。
4. **row 14（:346 GetDeathType）** —— 死因枚举 10/15/20/30 未映射到我们的 `DeathType`，需先查 `dump.cs` 枚举。
5. **row 44（:1081 SetMatchRecording）** —— **账内判据过时**（不是 bool 开关，而是「由 BattleRecordData 重建 MatchData」），账要订正。

### 二·二 · 需要「回源再读一个 `.c`」的挂起项（不是做不到，是差一层）

- row 6 → `BoardAnalysis__DeckMeetsCriteria.c`（存在）
- row 33 → `SupportMethods__ShuffleCardList.c`（stratum 分档）
- row 3 / row 20 → `DAT_1834xxxxxx` 浮点常量值（VA 读）
- row 4 → `BattleManager__ResolveTransformUnit.c`（存在，出队体）
- row 11 → `dump.cs` 的 `BattleManager.DeviceVariables` RVA（`.c` 被折叠成错文件）
- row 23 / row 34 → `inclHeroParts/inclWeapons` 与第 3 形参（VA 或 `out_il2csharp`）
- row 39 → `FUN_18173d1a0 / FUN_181750200` 定性（VA 或 `out_il2csharp`）

---

## 三 · 本批确认的「账记错 / 需订正」（铁律 5）

1. **row 44** `SetMatchRecording` 判据要点写「本局是否录（bool）」→ **实为「由 BattleRecordData 重建整份 MatchData」**。
2. **row 17** `GetMulliganCard` 写「取那张要换的卡」→ 实为「**弹队首卡(RemoveAt(0)) + 预先点亮下一张**」的 view 序。
3. **row 34** 写「棋盘（IsInPlay ∧ != caster）」→ **`.c` 体里只有 `IsInPlay ∧ card!=null`，未见 caster 比较**（排除点存疑，待回源）。
4. **row 1** 写「末尾 PlaySoundAsset」→ **本 `.c` grep 不到 sound**（存疑）。
5. **row 11** `DeviceVariables.c` 内容不符（已知，扩证一次）。

---

## 四 · 红线声明

- 只读：未改任何 `.cs`/数据/账/正本；未跑 Unity；未动 git。
- 凡写「需 VA / 查不到 / 结构不同」处，均为**如实**，未用猜测填空。
- 本文只覆盖 `战斗逻辑驱动.tsv` 的 48 条；其余 14 个账文件的 168 条**请另派**。
