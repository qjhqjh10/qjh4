# B9 · K1P0 —— P0 四域剩余 C 行施工单（结论与证据）

> 判据代理 `K1P0`（第八十四会话「完全复刻」）· 2026-10-26 · **只读**：未改 `.cs` / 未改资产 / 未跑 Unity（任何 `-executeMethod` / `*Run` / `_run_8_checks.sh`）/ 未动 git / 未改主账表 / 未改正本。
> 产出三件：`判据/B9_P0剩余C_施工单.tsv`（12 列 × 63 行）· `判据/B9_P0剩余C_派活包.md` · 本报告。
> 范围现读：`账/战斗界面_HUD.tsv` **C 25** · `账/卡组编辑.tsv` **C 25** · `账/收藏.tsv` **C 9** · `账/卡牌面_渲染.tsv` **C 4** ⇒ **合计 63 条**（与派活简报的「约 63」一致）。

---

## ① 结论

**条数与等级（现读账）**：63 条 = **P0 6** · **P1 16** · **P2 41**。
- P0 六条：`HUD:92`（CheckToBeAlternate）· `卡组:48`（GetCardTier）· `收藏:188/189/201`（FilterMethods 三行）· `收藏:203`（GameModesIconCollection.Release）。
- P1 十六条：`HUD:682/683/684/686`（ActiveWarlordAbility 四件）· `HUD:744`（ActivateCard）· `卡组:52/54`（开局玩家元数据 / 反序列化装配）· `卡组:194`（AddCardNotDrawnToDeck）· `卡组:285/286`（bot 预组两条）· `卡组:290`（PrebuiltPack.IsValid）· `卡组:299/302/305`（PrebuiltPackUI 三件）· `卡组:325`（ShowError）· `卡面:27`（SetColliderNotInHand）。
- 备注：`收藏:273` 账里**等级为空**，施工单按 P2 记（与 `判据/B8_收藏_卡组_卡牌_C行定级.tsv` 的「建议 P2」一致）。

**可分几包**：**18 包**（按我们的目标文件切，文件互不相交）
- **16 个可派包**：K-01…K-15（并行池）+ K-16（SoftMask，**必须等 K-03** 收工，同抢 `Core/CardView.cs`）。
- **1 个待裁包**：K-17（`收藏:244/247`，B8 定级表建议 **改判 D**）。
- **1 个挂起包**：K-18（`HUD:306`，判据可及但数据源是远端 LiveOps ⇒ 离线恒不可达）。
- **最大瓶颈**：`Battle/BattleDriver.cs` 一个文件装 **20 条**（K-01）—— 五个包（K-02/K-04/K-09/K-15 与 K-03 的反向）对它的改动全部写成「改动请求」交给 K-01，避免撞车。

**最值钱的前 5 条**
1. **`卡组:48` GetCardTier（P0）** —— 开局逐卡档位表缺失（全仓 `GetCardTier` 0 命中）；升级框/卡框档位每局可见，判据（`.c`）只有 4 句、来源（`CardProgress.Level(id)−1`）已备。
2. **`收藏:203` GameModesIconCollection.Release（P0）** —— **机制其实已经存在**（`CardArt.Release`/`PathOf`/`Retain` + `MenuDraw.ReleaseArtUnder`），只差「模式图标那一族被 `IsPerItemArt` 挡掉」的一小段；成本约半小时。
3. **`收藏:188/189/201` FilterMethods 三行（P0）** —— 引擎筛选总口径；地基已有（`TargetCriteriaEval.cs` 自称「全仓唯一那份 36 格判据」+ `CountEval.cs` 文件头已收 `CheckIfMeetsCriteria.c` 的分段）⇒ 补格而不是从零建。
4. **`卡组:285/286` bot 预组（现 P1，建议升 P0）** —— 玩家**每局**都看得见对手牌表，现凑 ≠ 预组；且原版那处「用未过滤长度取随机下标」的 bug 要照抄。
5. **`HUD:744` Codex/Oath 两枚角标（P1）** —— 手牌可打性可视化，每帧可见；`oathOk` 判据我们已有一半（`BattleDriver.cs:27093`）。

> 次值钱但不贵：`HUD:92/93`（战斗侧异画，两行同一件）· `卡面:21`（**建议升 P1**：现在「全部手牌都能弹 tooltip」是可见的错）+ `卡面:27`（进墓地的卡仍可点）。

**多少是 P3 候选**：**4 条**，全部只涉**数据源**，**UI/装配部分不判 D** —— `卡组:52`（elo / alliance / 徽记来自账户数据）· `卡组:49` / `卡组:50`（`SerializedPlayerDeckData` 是发服务器的载荷）· `HUD:306`（教程事件随 PlayFab userInfo 下发）。详见派活包 §5（只给证据 + 时间限定，**未自行判 D**）。

**另外三条「不是 P3、但建议改级」**（只在施工单 `建议等级` 列写，最终由调度台裁）—— **调度台已裁其中两条**：
`卡组:285/286` → **P0（已裁，账已改）** · `卡面:21` → **P1（已裁，账已改）** · `收藏:244` → **D（建议，见 §6 三源取证）** · `收藏:247` → **撤销原「建议 D」，保持 C**（补证后查出 3 处活调用，见 §6）。

---

## ② 逐条证据（63 条，一行一条：判据 → 我们现状 → 施工单要点）

> 完整 12 列见 `判据/B9_P0剩余C_施工单.tsv`；这里给「判据出处 + 现状 + 一句话施工」。

**战斗界面_HUD（25）**
| 行 | 判据 | 我们现状 | 一句话施工 |
|---|---|---|---|
| HUD:20 | `ActiveSkillDisplay.cs:115-196`（闸 :160、:172、:188、:193）+ `SupportMethods.cs:1478/1494` | 面板侧三件已齐；缺 ability 级 `targetsAvailableCounter`(+0x40) 列 + `ShowSkillPanel` 读卡级 +0x298 | 生成器补列 → CardDef 加字段 → ShowSkillPanel 改读 ability 级 |
| HUD:92 | `BattleCardManager__CheckToBeAlternate.c:11-58` | 本体 0 直调，同一段内联进活的 `CreateCard`；deck 层无异画副本格 | 并入 HUD:93，不单独立项 |
| HUD:93 | `cpp2il dev BattleCardManager.cs:20-130` | 战斗侧异画缺失；`cards_engine.json` 的 `alternateArts` 0 张有值 + `PlayerDeck.CardIds` 是裸 id | 生成器产 card→altArt[] → PlayerDeck 表示副本 → 建视图设 `artOverride` |
| HUD:101 | `BattleCardUI.BattleCardSetup` + `BattleManager.cs:16244` | 8 处主图已接；缺牌堆底 SDF（CardArt 无 `DeckCardbackSdf`） | CardArt 加 DeckCardbackSdf → 牌堆两层改调 |
| HUD:108 | `BattleCardUI.cs:2899-2911` + `HasToUseLegendarySummonEffect.c` | 只有一份溶解材质（传说也用普通的）；两份资产都在工程里 | rarity==4 && 非伏击 ⇒ 用 `…Dissolve Legendary` |
| HUD:148 | `BattleCardUI.cs:1272-1345` | 抬牌描述无目标后缀；旧落点 `HighlightTargets` 是错的 | CardView 描述写口 + `RuleCore.PlayTargetPool` + `ApplyShowInHand`；⛔ 不与技能面板共用函数 |
| HUD:153 | `BattleCardUI.cs:3046-3051` + `ShouldShowOriginalRemnantCard.c` | 残骸态恒关原卡 3D 卡身 | 三条件写口（show && isRemnant && faction∈{SaimHann,Sautekh}） |
| HUD:189 | `BattleHud.cs:399-407` + 四个实现者 | 日志半边已接（`RefreshBattleLog`）；缺卡面重排字 | CardView.RefreshLocalizedText + 订户扩到卡视图 |
| HUD:306 | `CardDisplayWindow__SetupTutorialObj.c:26-72`（闸 :44-58） | 展示窗无 TutorialObjs 子树；数据源是远端 LiveOps ⇒ 离线恒不可达 | **挂教程线**（不派） |
| HUD:364 | `ChooseCardMenu.AutoProcessDone`（WaitForSeconds(2f)）+ Setup | 回放不弹选牌面板、直接应用录像 | 回放分支弹面板 + 自动选第 1 张 + 2 s 确认 |
| HUD:378 | `ChooseCardMenu.cs:559-660` | 面板本体三件已齐；剩回放自动选 | 并入 HUD:364 |
| HUD:537 | `ManaTypeHolder.cs:186-246`（SetManaSaved）+ .c | manager 层是纯转发（等价）；引擎无 savedMana ⇒ 生产 0 调用 | PlayerState 加「未兑现结转」→ UpdateHud 显示 Energy−saved → 调六步 |
| HUD:548 | `ManaTypeHolder.cs:186-246` + 两份 .c | 六步全落地；剩 ① 引擎无 savedMana ② 两个 AudioCue 0 命中 ③ UpdateHud 每帧重写 | 并入 HUD:537；cue 只记不响 |
| HUD:682 | `ActiveWarlordAbility__OnTouchDown.c`（两句） | 棋盘按下不记时刻、不置标志 | CardHighlight 加 BoardHold 两格 + K-01 调用口 |
| HUD:683 | `__OnTouchExit.c`（一句） | 无标志可清 | 随 682；三条路各清一次 |
| HUD:684 | `__OnTouchUpAsButton.c` 全文 | 轻点 = 开大卡窗；主动技要拖 → 三选一 | 轻点我方先 ShowSkillPanel 再试放主动技 |
| HUD:686 | `__Update.c` 全文（字段默认 0.3f） | 棋盘无长按弹卡；手牌 0.2 s 门是**另一个类** | 按住 > 0.3 s ⇒ 弹卡面（⛔ 别动手牌 0.2 s） |
| HUD:687 | `__OnTouchUpAsButton_b__12_0.c` + `DelayedCall(0.4f)` | 无 PressedBtn 这颗按钮 | 先从 prefab 取节点判据，再建 + 0.4 s 熄灭 |
| HUD:702 | `BattleManager._CompleteBattleFinished_d__389__MoveNext.c:20-95` | 无 `WaitForSecondsRealtime`、无 battleStage、靠玩家点击离房 | 两跳 Realtime（2.0 s + 第二段）→ battleStage=8 → 立刻 LeaveNetRoom |
| HUD:720 | `BattleManager.cs:16354` + .c | 无卡背引用释放口 | 收尾对两座位调 `CardArt.Release`（机制已在） |
| HUD:744 | `CardScript__ActivateCard.c:1-148` | 可打性/描边有；Codex/Oath 两枚角标 0 写点 | RefreshHandPlayable 补两角标（Codex 等值、Oath >=）+ ShowEphemeral |
| HUD:750 | `_DisplayOffensiveCardInCenter_d__264__MoveNext.c` | 固定屏心，无 offensive 偏移 | 先取 globalVars 偏移值，再按 758 同一公式 |
| HUD:755 | `CardScript.cs:17102-17157` + .c | SkillPanel 只取裸键 | 加 needsLoading 两支（词条走官方表） |
| HUD:756 | `CardScript__GetEnemyCardPlayedDisplayPosition.c:1-81` | 固定屏心 | 手牌锚 + 6 个标量公式（**数值未读**） |
| HUD:758 | `__GetOffensiveCardPlayedDisplayPosition.c:1-81` | 同上 | 与 756 同一公式（⛔ 别写两份） |

**卡组编辑（25）**
| 行 | 判据 | 我们现状 | 一句话施工 |
|---|---|---|---|
| 卡组:8 | `CardDeck__CanAddCard.c:100-147`（整副满在 :166）+ `CardDeck.cs:745-869` | 整副满排第 3；槽内同卡给 *AlreadySet | 挪到最后 + 错误码 MaxCopiesReached（同批改 DeckScene 夹具） |
| 卡组:48 | `DeckAndWarlordData__GetCardTier.c` + `DeckAndWarlordData.cs:763` | 全仓 `GetCardTier` 0 命中；无 CardTiers 字典 | 开局建 {cardId: Level−1}，未命中 Tier1 |
| 卡组:49 | `__GetSerializedData.c` | 无 DTO | 与 50 同批：客户端侧序列化 + 出声不发 |
| 卡组:50 | `__GetSerializedDeckData.c`（偏移表逐项） | 字段零散（cardback / 头像 / 边框有源） | 按 .c 建 11 组字段（⛔ 别照抄碎掉 ILSpy） |
| 卡组:51 | `__OutputLogContents.c`（四句） | 无四行转储 | 开局末尾打四行（成本最低） |
| 卡组:52 | `__SetPlayerData.c` 全文 | `NetPendingBattle` 只带玩家名 | 扩载荷 + 逐字段灌（缺字段出声） |
| 卡组:54 | `__SetupSerializedData.c` 全文 | 卡组四件有；缺元数据 + CardTiers | 与 48/52 同批反序列化装配 |
| 卡组:71 | `DeckCostQuantityRowDrawer.cs:37-108` + .c | 直赋终值（无 0.2/0.4 补间） | CostCurveDrawer.Build 加补间 |
| 卡组:75 | `DeckDrawer.cs:17-28` + 三源取证（§6） | 抽屉已建；**挂接存在** = `ItemDrawerConfig.asset:201-214` 有 PrebuiltDeck 条目 + 两个 `Deck_cardback_avatar` offer prefab 各挂 1 个 `DeckAndCardbackDrawer`；本地 0 条发放物（时间限定） | 接 OverrideOnClickAction → DeckInfoPopup(View)（已裁：要做，P2） |
| 卡组:131 | `DeckEnergyCostDrawer.cs:99-307` + .c | 归一常数 10 已对；缺补间 | 并入 71 |
| 卡组:134 | `DeckGeneralInfoDemo.cs:51-65` | 4 挂 3，缺 warlordText | 督军名加命中区（padding 口径） |
| 卡组:138 | `DeckGeneralInfoDemo.cs:280-319` | 字段级等价；缺卡背 5 连轮播 | 建 5 层 + Rewind/Play |
| 卡组:148 | `DeckGeneralInfoDemo.cs:417-427` | 只有 Label、无命中区 | 加命中区 → Shell CardDetailPopup(showOptions:false) |
| 卡组:194 | `DeckManager._AddCardNotDrawnToDeck_d__18__MoveNext.c` | 无「未离牌库就回牌库」这条路 | 等 `MoveToDeck` **返回值**时长 → SetupInDeck（⛔ 别写死 0.4） |
| 卡组:284 | `PrebuiltDeckCollection.cs:234-253` | 练习池 + 教程排除已复刻；无 botDecks | 生成期多产 botDecks 分表（285/286 的前置） |
| 卡组:285 | `__GetDeckByDifficulty.c` 全文 | 对手卡组由 ResolveDeck 现凑 | bot 池 + difficulty/playMode 过滤 + **照抄未过滤长度取下标** |
| 卡组:286 | `PrebuiltDeckCollection.cs:67-198` + .c | 同上 | 与 285 共用一条过滤链（四道过滤） |
| 卡组:290 | `PrebuiltPack__IsValid.c` 全文 | 无谓词（现池 882/882 恒 true） | 两条件 + 负例夹具 |
| 卡组:293 | `PrebuiltPackUI.CloseAnim`（:217-224） | 只做缩放 | 加 0.4 s alpha |
| 卡组:299 | `PrebuiltPackUI.OpenAnim`（:253/:268） | 只落缩放 + 0.175 阶梯 | 五件：复位 / alpha / 禁用钮 / appearSound |
| 卡组:300 | `PrebuiltPackUI.cs:114-125` | 预览能开，缺音效 | 走 WindowSounds（cue 名先取） |
| 卡组:302 | `PrebuiltPackUI.SelectAnim`（:321） | 只落缩放 | 影子副本 + 隐藏本体 + SelectAnimDummy |
| 卡组:303 | `PrebuiltPackUI.SelectAnimDummy`（:355） | 整套无 | 7 步（0.3 / 0.2 / 0.4 已读） |
| 卡组:305 | `PrebuiltPackUI.cs:127-133` | 未缓存 startingPos | 先做（299/302/303 前置，须在布局后取） |
| 卡组:325 | `UIMessageController__ShowError.c` + `DeckEditingWindow__OnCardClick.c:29-37` | `DeckRuntime.Say` 只写文本 + Log | ErrorMessageBanner 加无 ctx 入口 → Say 里加一行 |

**收藏（9）**
| 行 | 判据 | 我们现状 | 一句话施工 |
|---|---|---|---|
| 收藏:15 | `ArmyItemContainer.cs:202`（三段） | 只覆盖 Collection 支 | Campaign/Forge 两页各挂 13 颗徽记 |
| 收藏:188 | `FilterMethods__CheckIfMeetsCriteria.c` | `TargetCriteriaEval` 只覆盖部分 36 格 | 建 ta→分支表 → 逐格比 → 补格 + 每格夹具 |
| 收藏:189 | `__CheckIfRawCardMeetsCriteria.c` | 卡级 criteria 未逐字段核 | 落 `CardCriteria` + stat ±1 边界 |
| 收藏:201 | `__RawCardMeetsTraitCriteria.c` | trait 支未核 | traits/excludeTrait + summonRift 门 2×2 |
| 收藏:203 | `GameModesIconCollection__Release.c` 全文 | **机制已在**（Release/Retain/ReleaseArtUnder）；模式图标被 `IsPerItemArt` 挡掉 | 加 `ReleaseModeIcon()` + 一个调用口（⛔ 别扩 PerItemArtPrefix） |
| 收藏:244 | `InventoryManager.cs:878-890` | 无重载；**三源 + C# 调用点全 0** ⇒ 建议 D | 只写账（时间限定 = 本 build） |
| 收藏:247 | `InventoryManager.cs:1481-1501` | 无重载；**有 3 处 C# 活调用**（§6）⇒ 可达 | 建 `LoadableRef<T>` + 一行转发 |
| 收藏:273 | `ItemDrawer.cs:917-951` | 前三步已对；缺 `UI Click 1` wav | 收进 EXTRA_CUES 重跑导入 |
| 收藏:348 | `WildcardDisplay.cs:58-75` + W64 参数全表 | 无 Shiny shader / transition 贴图 | 移植 Shiny（⛔ 不做加法近似）+ externals[14] 取贴图 |

**卡牌面_渲染（4）**
| 行 | 判据 | 我们现状 | 一句话施工 |
|---|---|---|---|
| 卡面:21 | `Card2DController.cs:147` + `EverguildTooltipTrigger__Toggle.c:48-55` | 6 处接 4；`BattleDriver.cs:26476` 把**全部手牌**置 true | 删全量置 true → 交给 ApplyShowInHand；开包/换牌整片关 |
| 卡面:27 | `CardCollider2D.cs:89` + 6 个调用点 | 两支路齐；缺上游调用（进墓地仍可点） | BattleDriver 死亡那一刻 `SetHitEnabled(false)`（残骸除外） |
| 卡面:55 | `CardImageController.cs:251`（死代码）+ `ChangeState(Big)` :286-303/:323-364 | 无 SoftMaskScript、`_textBg` 无软遮罩 | 搬插件 + Big 态接线（单开、与 K-03 串行） |
| 卡面:120 | `TextTooltipController.cs:323` | `ShowAtMouse` 已备；调用点锚在关键词上 | 调用点改「鼠标 x/y + y+10px + TMP 世界 z」 |

---

## ③ 没查清的部分（如实记，⛔ 不拿猜测填空）

1. **`HUD:750/756/758` 的偏移数值没读**：公式与偏移位置已从 `.c` 读全（`globalVars +0x50/+0x54/+0x58` 与另一组 `+0x18/+0x1c/+0x20/+0x3c/+0x40/+0x44`），但**具体标量值**要从解包实例（`BattleManager.globalVars` / `GameplayVariables` 那份 SO）读 —— 本代理未取。**还差**：定位那份 SO 实例并读 8 个数（含两档 y）。
2. **`HUD:702` 第二段等待常量 `DAT_1834b2e7c` 的值未读**；`uiState(+0x1c)` 具体是哪一位状态（"uiState 真" 的语义）未逐字段核。
3. **`HUD:687` 的 `PressedBtn` 节点判据（名 / rect / sprite）未取** —— 施工单已写「先读 prefab 再建」。
4. **`卡组:194` 的 `MoveToDeck` 返回时长**（默认常量 `DAT_1834b2dc8`）未读；我们这边是否已有等价补间未核（若没有再建，时长来源就要一起定）。
5. **`卡组:138` 卡背 5 层的 rect / sprite 未取**。
6. **`卡组:286` 的 `HasHiddenCards` 依赖卡池 `isHidden` 列** —— 该列在 `cards_truth.json` 是否存在未核（影响能不能直接做四道过滤）。
7. **`收藏:348` 的 `m_TransitionTex` 仍在 `externals[14]`**（既有裁决时已记），本代理未解；`_TransitionRotation 139°` 是否烘进 `_TransitionTex_ST` 未确认。
8. **`卡面:55` 的 SoftMask 插件来源/版本未选**（搬哪一版、许可证如何，均未定）。
9. **`HUD:20` 的「该拼的 17 张」名单未取**（账有 `17/1128` 数字，但没有卡 id 清单）⇒ 断言用例要现算。
10. **`HUD:684` 的 `TryUsingActiveAbilityOfCard` 在引擎侧的等价入口未逐一核**：面板入口（`ShowSkillPanel`）已定位，「试放主动技」那半只确认到 `RuleCore` 有 CanUse 系列，未确认「返回 bool + 强制选卡」这一档。
11. **`收藏:15` 的徽记 sprite 名 / rect 未取**（只有两窗 `Badge Highlight` 常量线索）。
12. **全部「怎么验」都是待执行读数口**：本代理只读，未跑 `ruleprobe` / 未跑任何宿主 / 未进真机。

---

## ④ 改动清单（只有这三份文件）

| 文件 | 内容 | 规模 |
|---|---|---|
| `d:/4/Unity/资料/完全复刻/判据/B9_P0剩余C_施工单.tsv` | 12 列 × 63 行（首行表头照 `B9_剩余C_施工单.tsv` 原样），UTF-8 无 BOM、LF | 47,372 字符 / 70,337 字节 |
| `d:/4/Unity/资料/完全复刻/判据/B9_P0剩余C_派活包.md` | 包总表 + 18 包明细 + MVP + 并发波次 + P3 候选表 + 冲突清单 | 14,151 字符 |
| `d:/4/Unity/资料/完全复刻/报告/B9_K1P0_P0剩余C施工单.md` | 本报告（原五节 + ⑥ 三源取证 + ⑦ 判据要点替换表；派活简报追加的取证与替换文本都在这两节） | — |

未改：任何 `.cs` / 资产 / `账/*.tsv` 主表 / `项目任务.md` / `CLAUDE.md` / git。

---

## ⑤ 顺手发现（只报不改）

1. **`HUD:686` 账里的「语义相反」是拿两个类比的** —— 手牌 0.2 s 门 = 原版 `CardScript__OnTouchUpAsButton.c:37`（长按**不**开窗，我们已正确实现，`CardInteraction.LongPressNoDisplaySeconds`）；棋盘 0.3 s = `ActiveWarlordAbility.Update`（按住**就**弹）。两者是**不同类、不同对象**。⇒ 施工只补棋盘那一半；若照账的字面去「改手牌方向」，会把已经对的做错。
2. **两个同名 `GetCardTier` 有被混用的风险**：`卡组:48` = `DeckAndWarlordData.GetCardTier`（读开局 `CardTiers` 字典，未命中 `Tier1`）；`调度台日志` 里「#63 GetCardTier → A」讲的是**另一个**（按原卡键取档）。⇒ ⛔ 别拿那条 A 裁定去关 `卡组:48`。
3. **`收藏:203` 的账文已过期（铁律 5 场景）**：账写「我们无句柄层 · `CardArt._cache` 只增不减」，实际 `CardArt.Release` / `PathOf` / `Retain` / `Unretain`（`Core/CardArt.cs:1117-1153`）与 `MenuDraw.ReleaseArtUnder`（`Shell/MenuDraw.cs:3608-3626`）**已在**（工作项 B8_BF，2026-10-26）。剩余缺口只有「模式图标被 `IsPerItemArt` 挡掉」。
4. ✅ **`收藏:244/247` 已结（三源取证，§6）**：`收藏:244` 三源 + C# 调用点**全 0** ⇒ 建议 D（时间限定 = 本 build）；`收藏:247` **有 3 处 C# 活调用** ⇒ **保持 C、撤销原「建议 D」**。账原「两个重载都 0 外部调用点」这句**是错的** —— 只数了 `GetOwnedCount<`，`HasEnough` 那条根本没数。
5. ✅ **`卡组:75`（DeckDrawer）已结**：**挂接存在 = C**（SO 登记 + 两个 offer prefab 里各挂 1 个 `DeckAndCardbackDrawer`）；「0 个 UnityEvent」是**正常**的（点击走 `IPointerClickHandler`），⛔ 不能据此判「没挂接」。时间限定 = 当前 build 0 条 `ObtainableItem` 形式的发放。
6. **`收藏:15` 的跨账分工要写清**：`账/战役.tsv` 把 `ArmySelector.Initialize` 判 A（落点 `CampaignTab.cs:791`），本行要的是「Campaign/Forge 两支的每阵营徽记」⇒ 不是重复行，但合并时若不做说明会被当成重复删掉。
7. **账的行号已在漂**（本轮现读 vs 账）：`CardView.BeginDissolve` **:6986**（账 :6961）· `CardView.SetRemnantBody` **:2537**（账 :2496-2522/:2632）· `SkillPanel.Show` :1535（一致）· `BattleDriver.ShowSkillPanel` 现读 :15281（账写 :15059/:15070）。⇒ 写手一律按**符号名**认。
8. **`RuleEngine/Core/GameplayVariables.cs` 缺 6 个展示偏移**：`HUD:750/756/758` 三行全卡在这同一前置上（原版在 `BattleManager.globalVars`）。这是「一个前置喂三行」的收敛点，值得单独派一次取证。
9. **`CardArt` 的释放家族只差枚举一格**：`PerItemArtPrefix`（`Core/CardArt.cs:1136-1145`）含 6 族，`ui_menu/` 不在内 —— 而原版 `ReleaseGameModeIcon` 只放**默认**模式图标。⇒ 扩前缀是错的、单开一个 `ReleaseModeIcon()` 才对；这条差别容易做反。
10. **`卡组:48` 一族的「判据要点」列填的是类级摘要**：`账/卡组编辑.tsv` 的 `卡组:48/49/50/51/52/54` 六行「判据要点」是同一句「战斗开局的数据装配（洗牌/序列化卡组/玩家战斗数据）」，不是方法级 —— 本施工单第 6/7 列已按 `.c` 逐句覆盖。⇒ 账那一列需要一次就地订正（我没改主表）。
11. **`HUD:537/548` 的两个 AudioCue 本地 0 命中**：即使引擎补上 `savedMana`，那两声也永远不响 ⇒ 需要一条 cue 导入路线（与 `收藏:273` 的 `UI Click 1` 同一类工作，可合批）。
12. **`卡面:21` 的调用点清单其实是 7 处不是 6 处**：`MulliganManager.cs:284` 从未在「6 个调用点」清单里（账已记，但编号仍按 6 走）。施工按 7 处核。
13. **`HUD:744` 的第⑥步卡资源**：`ToggleGlitch`（jam 单位）在既有裁决里是「素材全盘 0 命中」⇒ 本行只能做到 6/7 步，glitch 那一路要另记（⛔ 别用近似图顶）。

---

## ⑥ 三源取证（派活简报追加项 · 2026-10-26）

> 口径照《方案》§4.1：**判 D 的「没人引用」必须查 Unity 侧挂接**，⛔ 只凭「全量 C# 里 0 引用」不够。
> 三处 = ① prefab 的 `m_PersistentCalls` / UnityEvent ② 场景挂接 ③ SO / Addressables 引用。
> **对照组（证明搜法有效）**：`ExportedProject` 里 **245 个 prefab** 有 `m_MethodName`、**14 个 scene** 有它；`assets_full` 里 **860 个 MonoBehaviour JSON** 有 `m_MethodName` ⇒ 下面那些「0 命中」不是搜法失灵。
> ⚠️ 只读：未改任何资产、未跑 Unity。

### ⑥·1 `收藏:244` `GetOwnedCount<T>(LoadableReference<T>)` ⇒ **三源全 0 + C# 调用点 0 ⇒ 建议 D（时间限定 = 本 build）**

| 源 | 查了什么 | 结果 |
|---|---|---|
| ① prefab UnityEvent | `ExportedProject` 全量 `.prefab`（1,389）+ `assets_full` 全量文件的 `GetOwnedCount` 字符串 | **0 命中** |
| ② 场景 | `ExportedProject` 17 个 `.unity` | **0 命中** |
| ③ SO / Addressables | 9,239 个 `.asset`（含 `ItemDrawerConfig.asset`）+ `assets_full` 全量；Addressables 无方法级引用 | **0 命中** |
| ④ 旁证（C# 调用点） | `decomp_full` 全 29 处 `InventoryManager__GetOwnedCount<…>` 逐处判实参 | **无一处传 `LoadableReference`**：1 处无参（`SelectDecksTab__Setup:44`）· 23 处 `(T item)` T=RawCardScript 族（`DAT_18426caf8`）· 3 处 `(T item)` T=Currency（`DAT_18426c900`）· 2 处 `(T item)`（`ShopOfferEvent__GetOwnedQuantity:29`、`RewardWindow__Open:155`，实参都是虚拟 getter 返回的 `ObtainableItem`） |

**结构论证（比「搜不到」更硬）**：这两个重载是**泛型方法**，而 UnityEvent 只能绑定**具体、无泛型参数**的方法，且目标必须是 `UnityEngine.Object` 的组件 —— `InventoryManager` 两者都不满足 ⇒ **prefab/scene 侧结构上不可能绑它**。
**结论**：第三源也是 0；`收藏:244` 可判 **D（时间限定：本 build）**。

### ⑥·2 `收藏:247` `HasEnough<T>(LoadableReference<T>, int)` ⇒ **C# 有 3 处活调用 ⇒ 保持 C（撤销「建议 D」）**

| 调用点 | 实参证据 |
|---|---|
| `decomp_full/CurrencyCondition__Validate.c:14` | `InventoryManager__HasEnough<object>(this, *(param_1 + 0x10), *(param_1 + 0x18), DAT_18426ccf0)` —— 形如「LoadableReference 字段 + 数量」的条件结构 |
| `decomp_full/CardServices__CheckForCardUpgrade.c:88` | 同上形状：`*(lVar7 + 0x10)` + `*(lVar7 + 0x18)`，`DAT_18426ccf0`（与上一处**同一个 methodInfo**） |
| `decomp_full/UpgradePanel__CheckUpgradeRequirements.c:106` | 同上；**同一结构在 `:140` 把这个 `+0x10` 喂给 `LoadableReference.get_Reference`、再当 `Currency` 用** ⇒ 实参确为 `LoadableReference<Currency>` |

> 三处 `HasEnough` 用的是 `DAT_18426ccf0`；另两处（`ChangeNameWindow__Open:71`、`CurrencyValue__HasEnoughCurrency:19`）用 `DAT_18426cdf0` 且实参是**解引用后的 `Currency`**（`get_Reference(...)` / `CurrencyExtensions.ToCurrency(...)`）⇒ 那是 `HasEnough<T>(T item, int)` 那个重载。**两组 methodInfo 不同 = 两个不同重载，可区分。**
> 附：两个泛型体文件 `InventoryManager__HasEnough_object_.c`（具体化形态，直接转发）与 `__HasEnough___Il2CppFullySharedGenericType_.c`（泛型共享形态，内部走间接派发）都在，与「两个重载并存」一致。

**结论**：`收藏:247` **可达 = C**；账原写「两个 LoadableReference 重载 0 外部调用点」**不成立**（错在只数 `GetOwnedCount<`）。
**方法论（建议进准则）**：判「无调用点」必须把**同族签名一起数**（`GetOwnedCount` / `HasEnough` / 命名相邻的那些），并且**按实参形状**判重载，不能只按符号名。

### ⑥·3 `卡组:75` `DeckDrawer.HandlePointerClick` ⇒ **挂接存在 ⇒ C**；并验证「0 条 PrebuiltDeck 发放物」

| 源 | 查了什么 | 结果 |
|---|---|---|
| ① prefab UnityEvent | `Assets/Deck Drawer.prefab` | `m_MethodName` / `m_OnClick` / `m_Delegates` **各 0 处** —— ⚠️ **这是正常的**：`DeckDrawer : ItemDrawer<PrebuiltDeck>`，点击走 **`IPointerClickHandler`**，不需要持久调用 |
| ① prefab 组件挂接 | 两个 offer prefab 的 script guid → 类名映射 | `General Basic Offer Container Variant Deck_cardback_avatar.prefab` 与 `… Popup …` **各含 1 个 `DeckAndCardbackDrawer`**（`guid d8b1b549…`，= `DeckDrawer` 的子类）⇒ **抽屉实例确实被挂在 offer 槽里** |
| ② 场景 | 17 个 `.unity` 查 `DeckDrawer` / `HandlePointerClick` / Deck Drawer prefab guid | **0 命中**；唯一 `PrebuiltDeck` 的场景引用 = `Intro.unity:7792 starterDeck: {guid 7e1c9a5a…}`（新手起始卡组，**不是** ItemDrawer 那条路） |
| ③ SO / Addressables | `Assets/MonoBehaviour/ItemDrawerConfig.asset` | **有 `PrebuiltDeck, Assembly-CSharp` 条目**（`:201-214`）→ `drawerReference.m_AssetGUID: 7ea5632eb059b5b4493c5047a53e1706`（该 guid 在本机导出里**没有**对应资产文件；`Deck Drawer.prefab` 的 guid 是 `05db7dc0…`，不是这一条） |

**「0 条发放物」核实**：`PrebuiltDeck` 实例 **225 个**（`PrebuiltDeck.cs.meta` guid `a4d04ecb…`）；抽样 4 个实例的 guid 反查引用：**只有 2 个被 Demo `DeckInfo*.asset` 引用、1 个被 `Intro.unity` 的 `starterDeck` 引用**，**没有任何商店 / 奖励 / offer SO 引用**。我们自己的数据侧同样 0 条（`数据/游戏数据/` 里 `PrebuiltDeck` 只出现在 `prebuilt_decks_extra.json` / `prebuilt_deck_lists.json` / 本地化，均非发放）。
⇒ **「本地 0 条 `ObtainableItem` 形式的发放」成立**，但**挂接存在** ⇒ 按裁定：**要做，P2**，时间限定 = 「做完今天看不见真实效果」。

---

## ⑦ `账/卡组编辑.tsv` 六行「判据要点」的方法级替换文本（请调度台合并，未改主表）

> 现状：`卡组:48/49/50/51/52/54` 六行的「判据要点」是**同一句类级摘要**「战斗开局的数据装配（洗牌/序列化卡组/玩家战斗数据）」，不是方法级。下面按施工单第 5–6 列（原版逐条步骤 / 我们缺什么）各写一句可直接替换的文本。

| 账行号 | 现文（六行相同） | 建议替换文 | 依据 |
|---|---|---|---|
| 卡组:48 `GetCardTier` | 战斗开局的数据装配（洗牌/序列化卡组/玩家战斗数据） | 开局逐卡档位：`CardTiers` 字典 `TryGetValue(card)` 命中返回该档、未命中返回 `CardTier.Tier1`；字典为 null 时原版抛 NRE（不兜）。**我们缺这张表**（全仓 `GetCardTier` 0 命中），来源 = `CardProgress.Level(id) − 1` | `decomp_full/DeckAndWarlordData__GetCardTier.c` 全文 · ILSpy `DeckAndWarlordData.cs:763-778` |
| 卡组:49 `GetSerializedData` | 同上 | `GetSerializedDeckData()` 之后 `JsonWrapper.SerializeObject` 成字符串（**原版这份载荷是发给服务器的**；我们只做客户端侧序列化 + 出声说明不发） | `__GetSerializedData.c` 全文 · ILSpy `:780-784` |
| 卡组:50 `GetSerializedDeckData` | 同上 | 组 11 组字段：deckName · deckWarlord{id, CardTier} · defensiveCard id · shuffledCardLibrary→列表 · eloRating · cardback/warcry/allianceName · allianceRating · allianceBadge{featuredTrophy.TargetId, level} · currentDeckId · avatar/border/title · draftVictories | `__GetSerializedDeckData.c` 偏移表逐项（`.c` 为准；ILSpy 那份 body 是碎的） |
| 卡组:51 `OutputLogContents` | 同上 | 四行 `CustomDebug.Log`：`deckName: ` · `deckWarlord: <本地化名>` · `cardback: ` · 卡数（`shuffledCardLibrary.Count`）—— 纯日志，零行为 | `__OutputLogContents.c` 全文 · ILSpy `:131-145` |
| 卡组:52 `SetPlayerData` | 同上 | 从 `PlayerBattleData` 逐格灌：deckName · allianceName · eloRating · allianceRating · allianceBadge（`[9]` 非空才 new `GroupBadge(LoadableReference, [10])`）· currentDeckId · cardback · warcry · avatar · avatarBorder · title | `__SetPlayerData.c` 全文（ILSpy `:680-747` 偏移是碎的） |
| 卡组:54 `SetupSerializedData` | 同上 | 反序列化装配：deckName · `GetHeroCard(deckWarlord.id)` · `allCardCollection` 按 `CardIds` 逐个 Find · 建 `CardTiers`（逐 SerializedCard + 督军那张）· 再灌 11 组玩家元数据 | `__SetupSerializedData.c` 全文 |
